package tr.amorland.vadi;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import tr.amorland.denge.DengeServisi;
import tr.amorland.kimlik.Oyuncu;
import tr.amorland.kimlik.OyuncuDeposu;
import tr.amorland.ortak.Kodlar;
import tr.amorland.ortak.OyunHatasi;
import tr.amorland.ortak.RedKodu;
import tr.amorland.ortak.Saat;
import tr.amorland.oyun.DunyaKurucu;

import java.time.Duration;
import java.time.Instant;
import java.util.UUID;

/**
 * Vadi kurma, partner daveti ve katilma (B.7).
 *
 * <p>Rastgele eslesme <b>yoktur</b> (B.26 yasak 13): kalici ortak dunyada yabanci,
 * tasinamaz bir moderasyon yuku ve partner kaybinda oyunun olumu demektir.
 * Tek giris yolu 6 haneli davet kodudur.
 */
@Service
public class VadiServisi {

  private final VadiDeposu vadiler;
  private final OyuncuDeposu oyuncular;
  private final DunyaKurucu kurucu;
  private final DengeServisi denge;
  private final Saat saat;

  public VadiServisi(
      VadiDeposu vadiler,
      OyuncuDeposu oyuncular,
      DunyaKurucu kurucu,
      DengeServisi denge,
      Saat saat) {
    this.vadiler = vadiler;
    this.oyuncular = oyuncular;
    this.kurucu = kurucu;
    this.denge = denge;
    this.saat = saat;
  }

  /** Vadi kurar. Oyuncunun vadisi zaten varsa onu doner (idempotent). */
  @Transactional
  public Vadi kur(UUID oyuncuId) {
    var mevcut = vadiler.oyuncununVadisi(oyuncuId);
    if (mevcut.isPresent()) return mevcut.get();

    Instant simdi = saat.simdi();
    Oyuncu oyuncu = oyuncular.bul(oyuncuId).orElseThrow();
    Vadi vadi = vadiler.olustur(oyuncuId, Kodlar.vadiTohumu(), simdi);

    kurucu.oyuncuyuYerlestir(vadi.id(), oyuncuId, oyuncu.seviye(), simdi);
    kurucu.yabaniKaynaklariKur(vadi.id(), "tarla_kenari");
    vadiler.dunyaSeviyesiYaz(vadi.id(), oyuncu.seviye());

    return vadiler.bul(vadi.id()).orElseThrow();
  }

  /** Davet kodu uretir. Sv 3'te acilir ve hesap baglama/kurtarma kodu ZORUNLUDUR (B.19). */
  @Transactional
  public String davetKoduUret(UUID oyuncuId) {
    Instant simdi = saat.simdi();
    Oyuncu oyuncu = oyuncular.bul(oyuncuId).orElseThrow();
    Vadi vadi = vadisiniAl(oyuncuId);

    if (vadi.ciftMi()) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "Bu vadide zaten bir partner var.");
    }
    if (!vadi.kuranId().equals(oyuncuId)) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "Daveti vadiyi kuran gonderir.");
    }

    int davetSeviyesi = 3; // B.17 Perde 1: Sv 3'te partner daveti acilir
    if (oyuncu.seviye() < davetSeviyesi) {
      throw OyunHatasi.seviyeYetersiz("Partner daveti", davetSeviyesi);
    }

    var yk = denge.ilerlemeDosyasi().yasKapisi();
    if (oyuncu.yasKapisiAltindaMi(yk.esik(), simdi)) {
      throw new OyunHatasi(
          RedKodu.SEVIYE_YETERSIZ,
          yk.esik() + " yasin altinda partner daveti kapalidir. Oyun tek kisilik oynanir.");
    }

    // B.19: partner davetinde hesap baglama zorunlu. Baglamak istemeyen icin kurtarma kodu.
    if (!oyuncu.bagliMi() && !oyuncu.kurtarmaKoduVarMi()) {
      throw new OyunHatasi(
          RedKodu.BILINMEYEN,
          "Once hesabini bagla veya kurtarma kodu al: artik partnerinin ilerlemesi de sana bagli.");
    }

    var ayar = denge.turetilmisDosyasi().misafirHesap();
    vadiler.eskiDavetleriIptal(vadi.id(), simdi);

    String kod;
    int deneme = 0;
    do {
      kod = Kodlar.uret(ayar.davetKoduHane());
      deneme++;
      if (deneme > 20) throw new IllegalStateException("Davet kodu uretilemedi");
    } while (vadiler.davetBul(kod).isPresent());

    vadiler.davetKoduYaz(
        kod, vadi.id(), oyuncuId, simdi.plus(Duration.ofSeconds(ayar.davetKoduOmruSn())), simdi);
    return kod;
  }

  /** Kodla katilma. Partner ayni vadiye duser; dunya seviyesi ikisinin yuksegi olur (B.7). */
  @Transactional
  public Vadi katil(UUID oyuncuId, String hamKod) {
    Instant simdi = saat.simdi();
    String kod = Kodlar.normalize(hamKod);

    var kayit =
        vadiler
            .davetBul(kod)
            .orElseThrow(() -> new OyunHatasi(RedKodu.BILINMEYEN, "Davet kodu bulunamadi."));
    if (kayit.kullanildiAt() != null) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "Bu davet kodu kullanilmis.");
    }
    if (simdi.isAfter(kayit.gecerliBit())) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "Davet kodunun suresi dolmus.");
    }
    if (kayit.uretenId().equals(oyuncuId)) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "Kendi davetine katilamazsin.");
    }

    Vadi vadi =
        vadiler
            .bulKilitli(kayit.vadiId())
            .orElseThrow(() -> new OyunHatasi(RedKodu.BILINMEYEN, "Vadi bulunamadi."));
    if (vadi.ciftMi()) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "Bu vadide zaten bir partner var.");
    }

    Oyuncu katilan = oyuncular.bul(oyuncuId).orElseThrow();
    var yk = denge.ilerlemeDosyasi().yasKapisi();
    if (katilan.yasKapisiAltindaMi(yk.esik(), simdi)) {
      throw new OyunHatasi(
          RedKodu.SEVIYE_YETERSIZ, yk.esik() + " yasin altinda partner daveti kapalidir.");
    }

    vadiler.partnerAta(vadi.id(), oyuncuId);
    vadiler.uyeYap(vadi.id(), oyuncuId, simdi);
    vadiler.davetKullanildi(kod, simdi);

    kurucu.oyuncuyuYerlestir(vadi.id(), oyuncuId, katilan.seviye(), simdi);

    // Dunya seviyesi iki oyuncunun yuksegidir (B.7 "iki seviye kavrami").
    Oyuncu kuran = oyuncular.bul(vadi.kuranId()).orElseThrow();
    vadiler.dunyaSeviyesiYaz(vadi.id(), Math.max(kuran.seviye(), katilan.seviye()));
    vadiler.partnerSonGorulmeYaz(vadi.id(), simdi);

    return vadiler.bul(vadi.id()).orElseThrow();
  }

  public Vadi vadisiniAl(UUID oyuncuId) {
    return vadiler
        .oyuncununVadisi(oyuncuId)
        .orElseThrow(() -> new OyunHatasi(RedKodu.BILINMEYEN, "Once vadi kur."));
  }

  /**
   * Partner 7 gundur girmediyse dunya "tek imza" moduna gecer (%70 olcekleme).
   * Partner donerse ANINDA normale doner. <b>Bildirim gitmez</b> (B.7 kirmizi cizgi).
   */
  @Transactional
  /** Giris aninda eksik baslangic ekipmanini tamamlar (bkz. DunyaKurucu). */
  public void baslangicSetiniTamamla(java.util.UUID vadiId, java.util.UUID oyuncuId) {
    kurucu.baslangicSetiniTamamla(vadiId, oyuncuId);
  }

  public boolean tekImzaModunuTazele(Vadi vadi) {
    if (!vadi.ciftMi()) return true;
    var pt = denge.ilerlemeDosyasi().partnerTerk();
    Instant esik = saat.simdi().minus(Duration.ofDays(pt.tekImzaModuGun()));
    boolean tekImza = vadi.partnerSonGorulme() != null && vadi.partnerSonGorulme().isBefore(esik);
    if (tekImza != vadi.tekImzaModu()) {
      vadiler.tekImzaModuYaz(vadi.id(), tekImza);
    }
    return tekImza;
  }
}
