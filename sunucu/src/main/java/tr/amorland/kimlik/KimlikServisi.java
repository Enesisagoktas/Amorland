package tr.amorland.kimlik;

import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import tr.amorland.denge.DengeServisi;
import tr.amorland.ortak.Ayarlar;
import tr.amorland.ortak.Kodlar;
import tr.amorland.ortak.OyunHatasi;
import tr.amorland.ortak.RedKodu;
import tr.amorland.ortak.Saat;

import java.time.Duration;
import java.time.Instant;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;

/**
 * Misafir hesap, hesap baglama ve kurtarma (B.19).
 *
 * <p>Ilk acilista kayit ekrani yoktur: cihaz kimligiyle misafir hesap acilir.
 * Bu <b>surtunmesiz giris</b> tasariminin geregi. Bedeli su: telefon degisirse
 * ilerleme gider. O yuzden Sv 5/8/12'de baglama teklifi, partner davetinde ise
 * <b>zorunlu</b> baglama vardir — artik baskasinin ilerlemesi de buna baglidir.
 */
@Service
public class KimlikServisi {

  private final OyuncuDeposu oyuncular;
  private final JwtServisi jwt;
  private final NamedParameterJdbcTemplate jdbc;
  private final DengeServisi denge;
  private final Ayarlar ayarlar;
  private final Saat saat;

  public KimlikServisi(
      OyuncuDeposu oyuncular,
      JwtServisi jwt,
      NamedParameterJdbcTemplate jdbc,
      DengeServisi denge,
      Ayarlar ayarlar,
      Saat saat) {
    this.oyuncular = oyuncular;
    this.jwt = jwt;
    this.jdbc = jdbc;
    this.denge = denge;
    this.ayarlar = ayarlar;
    this.saat = saat;
  }

  /** Oturum acildiginda istemciye donen paket. */
  public record OturumPaketi(
      UUID oyuncuId,
      String ad,
      int renk,
      int seviye,
      int xp,
      boolean bagli,
      Integer dogumYili,
      String erisimJetonu,
      long erisimOmruSn,
      String yenilemeJetonu,
      UUID vadiId) {}

  /**
   * Misafir giris. Cihaz daha once gorulduyse ayni hesap doner (yeni hesap acilmaz).
   *
   * <p>Her giris yeni bir oturum kimligi uretir: "son baglanan kazanir" (C.5) kurali
   * bunun uzerinden isler.
   */
  @Transactional
  public OturumPaketi misafirGiris(String cihazId, String istenenAd, Integer istenenRenk) {
    Instant simdi = saat.simdi();
    var hesapAyar = denge.turetilmisDosyasi().misafirHesap();

    Oyuncu oyuncu =
        oyuncular
            .cihazIle(cihazId)
            .orElseGet(
                () -> {
                  String ad = adDogrula(istenenAd, hesapAyar.adMinUzunluk(), hesapAyar.adMaksUzunluk());
                  int renk = renkDogrula(istenenRenk, hesapAyar.renkSayisi());
                  return oyuncular.olustur(cihazId, ad, renk, simdi);
                });

    // Mevcut hesapta ad/renk degistirilmek isteniyorsa uygula (isim ekrani tekrar acilabilir).
    if (istenenAd != null && !istenenAd.isBlank() && !istenenAd.equals(oyuncu.ad())) {
      String ad = adDogrula(istenenAd, hesapAyar.adMinUzunluk(), hesapAyar.adMaksUzunluk());
      int renk = istenenRenk != null ? renkDogrula(istenenRenk, hesapAyar.renkSayisi()) : oyuncu.renk();
      oyuncular.adVeRenkGuncelle(oyuncu.id(), ad, renk);
      oyuncu = oyuncular.bul(oyuncu.id()).orElseThrow();
    }

    return oturumAc(oyuncu, simdi);
  }

  @Transactional
  public OturumPaketi oturumAc(Oyuncu oyuncu, Instant simdi) {
    UUID oturumId = UUID.randomUUID();
    oyuncular.oturumAyarla(oyuncu.id(), oturumId, simdi);

    String yenileme = Kodlar.uret(32);
    Duration omur = Duration.ofDays(ayarlar.jwt().yenilemeOmruGun());
    jdbc.update(
        "INSERT INTO yenileme_jetonu (jeton_hash, oyuncu_id, gecerli_bit, olusturuldu) "
            + "VALUES (:h, :o, :g, :s)",
        new MapSqlParameterSource()
            .addValue("h", Kodlar.ozetle(yenileme))
            .addValue("o", oyuncu.id())
            .addValue("g", java.sql.Timestamp.from(simdi.plus(omur)))
            .addValue("s", java.sql.Timestamp.from(simdi)));

    UUID vadiId =
        jdbc
            .query(
                "SELECT vadi_id FROM oyuncu_vadi WHERE oyuncu_id = :o",
                Map.of("o", oyuncu.id()),
                (rs, i) -> rs.getObject("vadi_id", UUID.class))
            .stream()
            .findFirst()
            .orElse(null);

    return new OturumPaketi(
        oyuncu.id(),
        oyuncu.ad(),
        oyuncu.renk(),
        oyuncu.seviye(),
        oyuncu.xp(),
        oyuncu.bagliMi(),
        oyuncu.dogumYili(),
        jwt.erisimJetonu(oyuncu.id(), oturumId),
        jwt.erisimOmruSn(),
        yenileme,
        vadiId);
  }

  /** Yenileme jetonuyla yeni erisim jetonu. Jeton tek kullanimlik: eskisi iptal edilir. */
  @Transactional
  public OturumPaketi yenile(String yenilemeJetonu) {
    Instant simdi = saat.simdi();
    String hash = Kodlar.ozetle(yenilemeJetonu);

    UUID oyuncuId =
        jdbc
            .query(
                "SELECT oyuncu_id FROM yenileme_jetonu "
                    + "WHERE jeton_hash = :h AND iptal_at IS NULL AND gecerli_bit > :s",
                new MapSqlParameterSource().addValue("h", hash).addValue("s", java.sql.Timestamp.from(simdi)),
                (rs, i) -> rs.getObject("oyuncu_id", UUID.class))
            .stream()
            .findFirst()
            .orElseThrow(() -> new OyunHatasi(RedKodu.YETKISIZ, "Yenileme jetonu gecersiz."));

    jdbc.update(
        "UPDATE yenileme_jetonu SET iptal_at = :s WHERE jeton_hash = :h",
        new MapSqlParameterSource().addValue("h", hash).addValue("s", java.sql.Timestamp.from(simdi)));

    Oyuncu oyuncu = oyuncular.bul(oyuncuId).orElseThrow();
    return oturumAc(oyuncu, simdi);
  }

  /** Hesap baglama (e-posta / Google / Apple). Partner davetinde ZORUNLUDUR (B.19). */
  @Transactional
  public void bagla(UUID oyuncuId, String tip, String ref) {
    if (!java.util.List.of("eposta", "google", "apple").contains(tip)) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "Bilinmeyen baglanti tipi: " + tip);
    }
    oyuncular.hesapBagla(oyuncuId, tip, ref);
  }

  /**
   * Baglamak istemeyen oyuncu icin 12 haneli kurtarma kodu.
   * Kod duz metin saklanmaz; oyuncu kaybederse yenisi uretilir ve eskisi gecersizlesir.
   */
  @Transactional
  public String kurtarmaKoduUret(UUID oyuncuId) {
    int hane = denge.turetilmisDosyasi().misafirHesap().kurtarmaKoduHane();
    String kod = Kodlar.uret(hane);
    oyuncular.kurtarmaKoduAyarla(oyuncuId, Kodlar.ozetle(kod));
    return kod;
  }

  /** Kurtarma: kod dogruysa cihaz kimligi yeni telefona devredilir. */
  @Transactional
  public OturumPaketi kurtar(String kod, String yeniCihazId) {
    String normal = Kodlar.normalize(kod);
    Oyuncu oyuncu =
        oyuncular
            .kurtarmaKoduIle(Kodlar.ozetle(normal))
            .orElseThrow(() -> new OyunHatasi(RedKodu.YETKISIZ, "Kurtarma kodu gecersiz."));

    // Yeni cihaz baska bir misafir hesaba bagliysa cakisma olur; o hesabin cihazini serbest birak.
    Optional<Oyuncu> cakisan = oyuncular.cihazIle(yeniCihazId);
    if (cakisan.isPresent() && !cakisan.get().id().equals(oyuncu.id())) {
      oyuncular.cihazDevret(cakisan.get().id(), "terk:" + cakisan.get().id());
    }
    oyuncular.cihazDevret(oyuncu.id(), yeniCihazId);

    return oturumAc(oyuncular.bul(oyuncu.id()).orElseThrow(), saat.simdi());
  }

  /** Yas kapisi (B.9). Bir kez sorulur; 13 altinda sohbet ve partner daveti kapanir. */
  @Transactional
  public void yasKapisi(UUID oyuncuId, int dogumYili) {
    int buYil = java.time.LocalDate.ofInstant(saat.simdi(), java.time.ZoneOffset.UTC).getYear();
    if (dogumYili < buYil - 120 || dogumYili > buYil) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "Dogum yili gecersiz.");
    }
    oyuncular.dogumYiliAyarla(oyuncuId, dogumYili);
  }

  public boolean sohbetAcikMi(Oyuncu oyuncu) {
    var yk = denge.ilerlemeDosyasi().yasKapisi();
    return !oyuncu.yasKapisiAltindaMi(yk.esik(), saat.simdi());
  }

  private static String adDogrula(String ad, int min, int maks) {
    String temiz = ad == null ? "" : ad.trim();
    if (temiz.length() < min || temiz.length() > maks) {
      throw new OyunHatasi(
          RedKodu.BILINMEYEN, "Ad " + min + "-" + maks + " karakter olmali.");
    }
    return temiz;
  }

  private static int renkDogrula(Integer renk, int adet) {
    int r = renk == null ? 0 : renk;
    if (r < 0 || r >= adet) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "Renk 0-" + (adet - 1) + " arasinda olmali.");
    }
    return r;
  }
}
