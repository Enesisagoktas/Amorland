package tr.amorland.oyun;

import org.springframework.stereotype.Service;
import tr.amorland.denge.DengeModelleri;
import tr.amorland.denge.DengeServisi;
import tr.amorland.kimlik.Oyuncu;
import tr.amorland.kimlik.OyuncuDeposu;
import tr.amorland.ortak.Saat;
import tr.amorland.oyun.DunyaModeli.*;
import tr.amorland.vadi.Vadi;

import java.time.Duration;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;

/**
 * Dunya durumunu okur ve istemciye gonderilecek sekle cevirir.
 *
 * <p>Mimari kural 2 ve 3: burada <b>hicbir sey saklanmaz</b> — dugum hazirligi,
 * buyume asamasi ve kalan sure hep {@code simdi} ile hesaplanir.
 */
@Service
public class DurumOkuyucu {

  private final DengeServisi denge;
  private final DunyaDeposu dunya;
  private final OyuncuDeposu oyuncular;
  private final Saat saat;

  public DurumOkuyucu(
      DengeServisi denge, DunyaDeposu dunya, OyuncuDeposu oyuncular, Saat saat) {
    this.denge = denge;
    this.dunya = dunya;
    this.oyuncular = oyuncular;
    this.saat = saat;
  }

  /** Tam anlik goruntu (C.3 SENKRON_TAM). */
  public Degisim anlikGoruntu(Vadi vadi, Set<UUID> cevrimiciler) {
    Instant simdi = saat.simdi();
    List<Oyuncu> uyeler = oyuncular.bul(vadi.oyuncular());

    List<OyuncuOzeti> ozetler = new ArrayList<>();
    for (Oyuncu o : uyeler) {
      ozetler.add(oyuncuOzeti(vadi, o, cevrimiciler.contains(o.id())));
    }

    List<Alet> aletler =
        dunya.aletler(vadi.id()).stream()
            .map(a -> new Alet(a.sahip(), a.aletId(), a.dayaniklilik(), maksDayaniklilik(a.aletId())))
            .toList();

    List<String> isaretler = new ArrayList<>();
    for (Oyuncu o : uyeler) {
      for (String i : dunya.isaretler(vadi.id(), o.id())) {
        isaretler.add(o.id() + "|" + i);
      }
    }

    return new Degisim(
        vadi.revision(),
        simdi,
        true,
        dunya.karolar(vadi.id()),
        dunya.envanter(vadi.id()),
        dunya.depo(vadi.id()),
        dugumler(vadi, simdi),
        aletler,
        ozetler,
        vadiOzeti(vadi),
        isaretler,
        List.of());
  }

  public OyuncuOzeti oyuncuOzeti(Vadi vadi, Oyuncu o, boolean cevrimici) {
    int kovaKap = kovaKapasitesi(vadi.id(), o.id());
    return new OyuncuOzeti(
        o.id(),
        o.ad(),
        o.renk(),
        o.seviye(),
        o.xp(),
        denge.seviyeEsigi(o.seviye()),
        // Para Sv 5'te girer (B.15). Oncesinde HUD'da gostergesi bile yok; -1 "gizle" demek.
        o.seviye() >= denge.ilerlemeDosyasi().paraninGirisi().ilkSeviye()
            ? dunya.altin(vadi.id(), o.id())
            : -1,
        denge.cantaSlot(o.seviye()),
        dunya.kovaKalan(vadi.id(), o.id()),
        kovaKap,
        cevrimici,
        o.sonGorulme());
  }

  public VadiOzeti vadiOzeti(Vadi vadi) {
    var kuslukAyar = denge.ilerlemeDosyasi();
    Optional<UUID> kusluk = dunya.kuslukBaslatan(vadi.id());
    return new VadiOzeti(
        vadi.id(),
        vadi.kuranId(),
        vadi.partnerId(),
        vadi.dunyaSeviyesi(),
        vadi.tekImzaModu(),
        denge.depoKapasitesi(vadi.dunyaSeviyesi()),
        dunya.depoDolu(vadi.id()),
        kusluk.isPresent(),
        kusluk.orElse(null));
  }

  public List<Dugum> dugumler(Vadi vadi, Instant simdi) {
    List<Dugum> sonuc = new ArrayList<>();
    for (var s : dunya.dugumler(vadi.id())) {
      sonuc.add(dugumeCevir(s));
    }
    return sonuc;
  }

  public Dugum dugumeCevir(DunyaDeposu.DugumSatiri s) {
    Instant hazir = null;
    if (s.kirildiAt() != null) {
      long yenilenme = yenilenmeSn(s.tur(), s.sinif());
      hazir = s.kirildiAt().plus(Duration.ofSeconds(yenilenme));
    }
    return new Dugum(
        s.bolge(), s.x(), s.y(), s.tur(), s.sinif(), s.saglik(), s.maksSaglik(), s.kirildiAt(), hazir);
  }

  /** Yenilenme suresi: yabani kaynak kendi tablosundan, cevher kat tablosundan. */
  public long yenilenmeSn(String tur, String sinif) {
    if ("yabani".equals(sinif)) {
      return denge.yabaniKaynak(tur).map(DengeModelleri.YabaniKaynak::yenilenmeSn).orElse(1800L);
    }
    if ("cevher".equals(sinif)) {
      return denge.uretimDosyasi().maden().katlar().stream()
          .filter(k -> k.dugumler().stream().anyMatch(d -> d.id().equals(tur)))
          .mapToLong(DengeModelleri.MadenKat::yenilenmeSn)
          .min()
          .orElse(1200L);
    }
    // Agac: kesilince karo bosalir, kutuk kalmaz (B.12). Yeniden dikilir.
    return denge.uretimDosyasi().agaclar().stream()
        .filter(a -> a.id().equals(tur))
        .mapToLong(DengeModelleri.Agac::buyumeSn)
        .findFirst()
        .orElse(28800L);
  }

  public int maksDayaniklilik(String aletId) {
    return denge.alet(aletId).map(a -> a.dayaniklilik() == null ? 0 : a.dayaniklilik()).orElse(0);
  }

  /** Oyuncunun sahip oldugu en iyi kovanin kapasitesi. Kova yoksa 0. */
  public int kovaKapasitesi(UUID vadiId, UUID oyuncuId) {
    int enIyi = 0;
    for (var a : dunya.aletler(vadiId, oyuncuId)) {
      var tanim = denge.alet(a.aletId());
      if (tanim.isPresent()
          && "kova".equals(tanim.get().tur())
          && tanim.get().kapasiteKaro() != null) {
        enIyi = Math.max(enIyi, tanim.get().kapasiteKaro());
      }
    }
    return enIyi;
  }

  /**
   * Bir turdeki en iyi aleti secer (B.6: alet secimi yok, otomatik secilir).
   * Kirik/olmayan alet icin bos doner.
   */
  public Optional<DunyaDeposu.AletSatiri> enIyiAlet(UUID vadiId, UUID oyuncuId, String tur) {
    DunyaDeposu.AletSatiri enIyi = null;
    int enIyiKademe = -1;
    for (var a : dunya.aletler(vadiId, oyuncuId)) {
      var tanim = denge.alet(a.aletId());
      if (tanim.isEmpty() || !tur.equals(tanim.get().tur())) continue;
      if (a.dayaniklilik() <= 0) continue;
      if (tanim.get().kademe() > enIyiKademe) {
        enIyiKademe = tanim.get().kademe();
        enIyi = a;
      }
    }
    return Optional.ofNullable(enIyi);
  }
}
