package tr.amorland.ws;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.TextWebSocketHandler;
import tr.amorland.denge.DengeServisi;
import tr.amorland.kimlik.JwtServisi;
import tr.amorland.kimlik.Oyuncu;
import tr.amorland.kimlik.OyuncuDeposu;
import tr.amorland.ortak.Ayarlar;
import tr.amorland.ortak.OyunHatasi;
import tr.amorland.ortak.RedKodu;
import tr.amorland.ortak.Saat;
import tr.amorland.oyun.DunyaModeli.Degisim;
import tr.amorland.oyun.OyunServisi;
import tr.amorland.vadi.Vadi;
import tr.amorland.vadi.VadiDeposu;
import tr.amorland.vadi.VadiServisi;

import java.io.IOException;
import java.time.Instant;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Oyun WebSocket'i (C.3).
 *
 * <p>Akis: {@code BAGLAN} -> {@code SENKRON_TAM} veya {@code SENKRON_DELTA},
 * ardindan {@code NIYET} -> {@code ONAY}/{@code RED}, partnere {@code DEGISIM}.
 *
 * <p>⚠ C.9: <b>arka plana atilma iOS'ta baglantiyi birkac saniyede oldurur.</b>
 * Yeniden baglanma istisna degil, normal durumdur — bu isleyici her BAGLAN'i
 * ilk baglanti gibi ele alir ve durum farkini revizyondan kapatir.
 *
 * <p>Sohbet mesajlari <b>sunucuda saklanmaz</b> (B.9): partner cevrimiciyse aktarilir,
 * degilse teslimat kuyruguna girer ve teslim edilince silinir.
 */
@Component
public class OyunWsIsleyici extends TextWebSocketHandler {

  private static final Logger log = LoggerFactory.getLogger(OyunWsIsleyici.class);

  private final ObjectMapper mapper;
  private final JwtServisi jwt;
  private final OyuncuDeposu oyuncular;
  private final VadiDeposu vadiler;
  private final VadiServisi vadiServisi;
  private final OyunServisi oyun;
  private final OturumKayitcisi kayitci;
  private final DengeServisi denge;
  private final Ayarlar ayarlar;
  private final Saat saat;

  /** Basit token-bucket hiz siniri: oturum -> son saniye penceresi. */
  private final Map<String, HizPenceresi> hizlar = new ConcurrentHashMap<>();

  public OyunWsIsleyici(
      ObjectMapper mapper,
      JwtServisi jwt,
      OyuncuDeposu oyuncular,
      VadiDeposu vadiler,
      VadiServisi vadiServisi,
      OyunServisi oyun,
      OturumKayitcisi kayitci,
      DengeServisi denge,
      Ayarlar ayarlar,
      Saat saat) {
    this.mapper = mapper;
    this.jwt = jwt;
    this.oyuncular = oyuncular;
    this.vadiler = vadiler;
    this.vadiServisi = vadiServisi;
    this.oyun = oyun;
    this.kayitci = kayitci;
    this.denge = denge;
    this.ayarlar = ayarlar;
    this.saat = saat;
  }

  private static final class HizPenceresi {
    long pencereBaslangici;
    int eylem;
    int sohbet;
  }

  @Override
  protected void handleTextMessage(WebSocketSession oturum, TextMessage mesaj) throws IOException {
    WsMesaj.Gelen gelen;
    try {
      gelen = mapper.readValue(mesaj.getPayload(), WsMesaj.Gelen.class);
    } catch (Exception e) {
      gonder(oturum, WsMesaj.Giden.hata(RedKodu.BILINMEYEN.name(), "Mesaj cozulemedi."));
      return;
    }
    if (gelen.t() == null) {
      gonder(oturum, WsMesaj.Giden.hata(RedKodu.BILINMEYEN.name(), "Mesaj tipi yok."));
      return;
    }

    if ("BAGLAN".equals(gelen.t())) {
      baglan(oturum, gelen);
      return;
    }

    var kayit = kayitci.kayit(oturum.getId());
    if (kayit == null) {
      gonder(oturum, WsMesaj.Giden.hata(RedKodu.YETKISIZ.name(), "Once BAGLAN gonder."));
      return;
    }

    switch (gelen.t()) {
      case "NIYET" -> niyet(oturum, kayit, gelen);
      case "KONUM" -> konum(kayit, gelen);
      case "SOHBET" -> sohbet(oturum, kayit, gelen);
      case "NOT_TAHTASI" -> notTahtasi(kayit, gelen);
      case "PING" -> gonder(oturum, new WsMesaj.Giden(
          "PONG", null, null, null, null, null, null, null, null, null, null,
          saat.simdi().toEpochMilli()));
      default -> gonder(
          oturum, WsMesaj.Giden.hata(RedKodu.BILINMEYEN.name(), "Bilinmeyen tip: " + gelen.t()));
    }
  }

  // ---------------------------------------------------------------- BAGLAN

  private void baglan(WebSocketSession oturum, WsMesaj.Gelen gelen) throws IOException {
    var icerik = jwt.coz(gelen.jwt() == null ? "" : gelen.jwt());
    if (icerik.isEmpty()) {
      gonder(oturum, WsMesaj.Giden.hata(RedKodu.YETKISIZ.name(), "Jeton gecersiz."));
      oturum.close(new CloseStatus(4401, "YETKISIZ"));
      return;
    }
    UUID oyuncuId = icerik.get().oyuncuId();
    Oyuncu oyuncu = oyuncular.bul(oyuncuId).orElse(null);
    if (oyuncu == null || !icerik.get().oturumId().equals(oyuncu.aktifOturum())) {
      gonder(oturum, WsMesaj.Giden.hata(RedKodu.BASKA_CIHAZ.name(), "Hesap baska cihazda acildi."));
      oturum.close(new CloseStatus(4403, "BASKA_CIHAZ"));
      return;
    }

    var vadiOpt = vadiler.oyuncununVadisi(oyuncuId);
    if (vadiOpt.isEmpty()) {
      gonder(oturum, WsMesaj.Giden.hata(RedKodu.BILINMEYEN.name(), "Once vadi kur."));
      oturum.close(new CloseStatus(4404, "VADI_YOK"));
      return;
    }
    Vadi vadi = vadiOpt.get();
    Instant simdi = saat.simdi();

    // "Son baglanan kazanir" (C.5): onceki oturum dusurulur.
    var eski = kayitci.kaydet(oturum, oyuncuId, vadi.id(), icerik.get().oturumId());
    if (eski != null && eski.oturum().isOpen()) {
      gonder(eski.oturum(), WsMesaj.Giden.hata(RedKodu.BASKA_CIHAZ.name(), "Baska cihazda acildi."));
      kayitci.kapat(eski, "BASKA_CIHAZ");
    }

    oyuncular.sonGorulmeGuncelle(oyuncuId, simdi);
    if (vadi.ciftMi()) {
      vadiler.partnerSonGorulmeYaz(vadi.id(), simdi);
      // Partner dondugunde tek imza modu ANINDA duser (B.7).
      vadiServisi.tekImzaModunuTazele(vadiler.bul(vadi.id()).orElseThrow());
      vadi = vadiler.bul(vadi.id()).orElseThrow();
    }

    long sonBilinen = gelen.sonRevision() == null ? -1 : gelen.sonRevision();
    var cevrimiciler = kayitci.cevrimiciler(vadi.id());

    if (sonBilinen < 0) {
      gonder(oturum, WsMesaj.Giden.senkronTam(oyun.anlikGoruntu(vadi, cevrimiciler), simdi.toEpochMilli()));
    } else {
      List<Degisim> paket = oyun.deltalarVeyaTam(vadi, sonBilinen, cevrimiciler);
      if (paket.size() == 1 && paket.get(0).tam()) {
        gonder(oturum, WsMesaj.Giden.senkronTam(paket.get(0), simdi.toEpochMilli()));
      } else {
        gonder(oturum, WsMesaj.Giden.senkronDelta(paket, vadi.revision(), simdi.toEpochMilli()));
      }
    }

    UUID vadiId = vadi.id();
    kayitci.yayinla(vadiId, oyuncuId, k -> gonderSessiz(k.oturum(), WsMesaj.Giden.varlik(oyuncuId, true)));
    log.debug("Oyuncu {} vadi {} icin baglandi (son revizyon {})", oyuncuId, vadiId, sonBilinen);
  }

  // ---------------------------------------------------------------- NIYET

  private void niyet(WebSocketSession oturum, OturumKayitcisi.Kayit kayit, WsMesaj.Gelen gelen)
      throws IOException {
    if (gelen.niyet() == null || gelen.niyet().clientActionId() == null) {
      gonder(oturum, WsMesaj.Giden.red(null, RedKodu.BILINMEYEN.name(), "client_action_id zorunlu."));
      return;
    }
    if (!hizKontrol(oturum.getId(), true)) {
      gonder(
          oturum,
          WsMesaj.Giden.red(
              gelen.niyet().clientActionId(), RedKodu.HIZ_SINIRI.name(), "Cok hizli."));
      return;
    }

    try {
      Degisim degisim = oyun.uygula(kayit.oyuncuId(), kayit.vadiId(), gelen.niyet());
      gonder(oturum, WsMesaj.Giden.onay(gelen.niyet().clientActionId(), degisim));
      // Partner cevrimiciyse ayni degisim ona DEGISIM olarak gider (C.3 yayin).
      kayitci.yayinla(
          kayit.vadiId(),
          kayit.oyuncuId(),
          k -> gonderSessiz(k.oturum(), WsMesaj.Giden.degisim(degisim)));
    } catch (OyunHatasi e) {
      gonder(
          oturum, WsMesaj.Giden.red(gelen.niyet().clientActionId(), e.kod().name(), e.getMessage()));
    } catch (Exception e) {
      log.error("Niyet islenemedi", e);
      gonder(
          oturum,
          WsMesaj.Giden.red(
              gelen.niyet().clientActionId(), RedKodu.BILINMEYEN.name(), "Islem yapilamadi."));
    }
  }

  // ---------------------------------------------------------------- KONUM

  /** Konum 5 Hz, revizyon URETMEZ, veritabanina yazilmaz (C.3). */
  private void konum(OturumKayitcisi.Kayit kayit, WsMesaj.Gelen gelen) {
    if (gelen.konum() == null) return;
    kayitci.yayinla(
        kayit.vadiId(),
        kayit.oyuncuId(),
        k -> gonderSessiz(k.oturum(), WsMesaj.Giden.konum(kayit.oyuncuId(), gelen.konum())));
  }

  // ---------------------------------------------------------------- SOHBET

  /**
   * Metin sohbeti. ⚠ B.9: <b>sohbet icerigi sunucuda saklanmaz.</b> Partner
   * cevrimiciyse aktarilir; degilse teslimat kuyruguna girer ve teslim edilince silinir.
   * Bu bir arsiv degil, kuyruktur.
   */
  private void sohbet(WebSocketSession oturum, OturumKayitcisi.Kayit kayit, WsMesaj.Gelen gelen)
      throws IOException {
    if (!hizKontrol(oturum.getId(), false)) {
      gonder(oturum, WsMesaj.Giden.hata(RedKodu.HIZ_SINIRI.name(), "Saniyede 1 mesaj."));
      return;
    }
    Oyuncu oyuncu = oyuncular.bul(kayit.oyuncuId()).orElse(null);
    if (oyuncu == null) return;

    var yk = denge.ilerlemeDosyasi().yasKapisi();
    if (oyuncu.yasKapisiAltindaMi(yk.esik(), saat.simdi())) {
      gonder(
          oturum,
          WsMesaj.Giden.hata(RedKodu.SEVIYE_YETERSIZ.name(), yk.esik() + " yas alti sohbet kapali."));
      return;
    }
    String metin = gelen.metin() == null ? "" : gelen.metin().strip();
    if (metin.isEmpty() || metin.length() > 240) return;

    kayitci.yayinla(
        kayit.vadiId(),
        kayit.oyuncuId(),
        k -> gonderSessiz(k.oturum(), WsMesaj.Giden.sohbet(kayit.oyuncuId(), metin)));
  }

  /** Not tahtasi: serbest metin degil, SABLON KIMLIGI + NESNE KIMLIGI (B.9). */
  private void notTahtasi(OturumKayitcisi.Kayit kayit, WsMesaj.Gelen gelen) {
    if (gelen.sablonId() == null) return;
    kayitci.yayinla(
        kayit.vadiId(),
        kayit.oyuncuId(),
        k ->
            gonderSessiz(
                k.oturum(),
                WsMesaj.Giden.notTahtasi(kayit.oyuncuId(), gelen.sablonId(), gelen.nesneId())));
  }

  // ---------------------------------------------------------------- yasam dongusu

  @Override
  public void afterConnectionClosed(WebSocketSession oturum, CloseStatus durum) {
    var kayit = kayitci.kayit(oturum.getId());
    kayitci.cikar(oturum.getId());
    hizlar.remove(oturum.getId());
    if (kayit != null) {
      oyuncular.sonGorulmeGuncelle(kayit.oyuncuId(), saat.simdi());
      kayitci.yayinla(
          kayit.vadiId(),
          kayit.oyuncuId(),
          k -> gonderSessiz(k.oturum(), WsMesaj.Giden.varlik(kayit.oyuncuId(), false)));
    }
  }

  // ---------------------------------------------------------------- yardimcilar

  private boolean hizKontrol(String oturumId, boolean eylemMi) {
    HizPenceresi p = hizlar.computeIfAbsent(oturumId, k -> new HizPenceresi());
    long saniye = saat.simdi().getEpochSecond();
    synchronized (p) {
      if (p.pencereBaslangici != saniye) {
        p.pencereBaslangici = saniye;
        p.eylem = 0;
        p.sohbet = 0;
      }
      if (eylemMi) {
        return ++p.eylem <= ayarlar.hizSiniri().eylemSaniyede();
      }
      return ++p.sohbet <= ayarlar.hizSiniri().sohbetSaniyede();
    }
  }

  private void gonder(WebSocketSession oturum, WsMesaj.Giden mesaj) throws IOException {
    if (!oturum.isOpen()) return;
    synchronized (oturum) {
      oturum.sendMessage(new TextMessage(mapper.writeValueAsString(mesaj)));
    }
  }

  private void gonderSessiz(WebSocketSession oturum, WsMesaj.Giden mesaj) {
    try {
      gonder(oturum, mesaj);
    } catch (IOException e) {
      log.debug("Gonderim basarisiz: {}", e.toString());
    }
  }
}
