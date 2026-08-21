package tr.amorland.vadi;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import org.springframework.web.bind.annotation.*;
import tr.amorland.kimlik.IstekBaglami;
import tr.amorland.ortak.OyunHatasi;
import tr.amorland.ortak.RedKodu;
import tr.amorland.oyun.DunyaModeli.Degisim;
import tr.amorland.ortak.Saat;
import tr.amorland.oyun.DunyaDeposu;
import tr.amorland.oyun.OyunServisi;
import tr.amorland.oyun.TrenServisi;
import tr.amorland.ws.OturumKayitcisi;

import java.util.Map;
import java.util.UUID;

/**
 * {@code /api/vadi/*} — C.10'daki vadi uclari.
 *
 * <p>Oyun eylemleri burada <b>yoktur</b>: onlar WebSocket uzerinden gider (C.3).
 * REST yalnizca meta islerdir — kurma, davet, ilk yukleme icin anlik goruntu.
 */
@RestController
@RequestMapping("/api/vadi")
public class VadiUcu {

  private final VadiServisi vadiServisi;
  private final VadiDeposu vadiler;
  private final OyunServisi oyun;
  private final OturumKayitcisi kayitci;
  private final TrenServisi trenServisi;
  private final DunyaDeposu dunya;
  private final Saat saat;

  public VadiUcu(
      VadiServisi vadiServisi,
      VadiDeposu vadiler,
      OyunServisi oyun,
      OturumKayitcisi kayitci,
      TrenServisi trenServisi,
      DunyaDeposu dunya,
      Saat saat) {
    this.vadiServisi = vadiServisi;
    this.vadiler = vadiler;
    this.oyun = oyun;
    this.kayitci = kayitci;
    this.trenServisi = trenServisi;
    this.dunya = dunya;
    this.saat = saat;
  }

  public record KatilIstek(@NotBlank String kod) {}

  public record VadiYaniti(
      UUID id, UUID kuranId, UUID partnerId, long revision, int dunyaSeviyesi, boolean tekImzaModu) {

    static VadiYaniti of(Vadi v) {
      return new VadiYaniti(
          v.id(), v.kuranId(), v.partnerId(), v.revision(), v.dunyaSeviyesi(), v.tekImzaModu());
    }
  }

  /** Yeni vadi kur. Zaten varsa mevcut vadiyi doner (idempotent). */
  @PostMapping
  public VadiYaniti kur() {
    return VadiYaniti.of(vadiServisi.kur(IstekBaglami.oyuncuId()));
  }

  @GetMapping("/benim")
  public VadiYaniti benim() {
    return VadiYaniti.of(vadiServisi.vadisiniAl(IstekBaglami.oyuncuId()));
  }

  /** 6 haneli davet kodu. Sv 3 + hesap baglama/kurtarma kodu zorunlu (B.19). */
  @PostMapping("/davet")
  public Map<String, Object> davet() {
    return Map.of("kod", vadiServisi.davetKoduUret(IstekBaglami.oyuncuId()));
  }

  @PostMapping("/katil")
  public VadiYaniti katil(@Valid @RequestBody KatilIstek istek) {
    return VadiYaniti.of(vadiServisi.katil(IstekBaglami.oyuncuId(), istek.kod()));
  }

  /** Tam anlik goruntu — WebSocket oncesi ilk yukleme icin (C.10). */
  /** Tren panosu: sefer, geri sayim ve 3 siparis karti (karsilanma durumuyla). */
  public record TrenKarti(
      int kart, String urunId, int miktar, int altin, int xp, boolean karsilandi,
      java.util.UUID karsilayan) {}

  public record TrenCevabi(long seferNo, long kalanSn, java.util.List<TrenKarti> siparisler) {}

  @GetMapping("/{vadiId}/tren")
  public TrenCevabi tren(@PathVariable UUID vadiId) {
    Vadi vadi =
        vadiler.bul(vadiId).orElseThrow(() -> new OyunHatasi(RedKodu.BILINMEYEN, "Vadi yok."));
    if (!vadi.uyeMi(IstekBaglami.oyuncuId())) {
      throw new OyunHatasi(RedKodu.UYE_DEGIL, "Bu vadinin uyesi degilsin.");
    }
    var simdi = saat.simdi();
    long sefer = trenServisi.seferNo(simdi);
    var karsilananlar = dunya.trenKarsilananlar(vadiId, sefer);
    var kartlar = new java.util.ArrayList<TrenKarti>();
    for (var s : trenServisi.siparisler(vadi, vadi.dunyaSeviyesi(), sefer)) {
      java.util.UUID karsilayan = null;
      for (var k : karsilananlar) {
        if (k.kart() == s.kart()) karsilayan = k.oyuncuId();
      }
      kartlar.add(new TrenKarti(
          s.kart(), s.urunId(), s.miktar(), s.altinOdul(), s.xp(),
          karsilayan != null, karsilayan));
    }
    return new TrenCevabi(sefer, trenServisi.kalanSn(simdi), kartlar);
  }

  @GetMapping("/{vadiId}/anlik")
  public Degisim anlik(@PathVariable UUID vadiId) {
    Vadi vadi =
        vadiler.bul(vadiId).orElseThrow(() -> new OyunHatasi(RedKodu.BILINMEYEN, "Vadi yok."));
    if (!vadi.uyeMi(IstekBaglami.oyuncuId())) {
      throw new OyunHatasi(RedKodu.UYE_DEGIL, "Bu vadinin uyesi degilsin.");
    }
    return oyun.anlikGoruntu(vadi, kayitci.cevrimiciler(vadiId));
  }
}
