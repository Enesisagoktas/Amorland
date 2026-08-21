package tr.amorland.oyun;

import org.springframework.stereotype.Service;
import tr.amorland.denge.DengeModelleri.Urun;
import tr.amorland.denge.DengeServisi;
import tr.amorland.ortak.Deterministik;
import tr.amorland.ortak.Saat;
import tr.amorland.vadi.Vadi;

import java.time.Duration;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;

/**
 * Tren seferleri ve siparis kartlari (B.15 — oyunun ekonomik kalbi).
 *
 * <p>TAMAMEN DETERMINISTIK ve DURUMSUZ (mimari kural 3): sefer numarasi
 * epoch'tan, fiyat carpani 50 seferlik ileri sarmali ortalamaya-donen
 * yuruyusten, kartlar hash'ten uretilir. Veritabanina yalnizca KARSILAMA
 * kayitlari yazilir — sefer/fiyat/kart satiri ASLA yazilmaz.
 *
 * <p>Saf rastgele KULLANILMAZ: fiyatin hafizasi olmali ki istemcideki
 * "son seferler" egilimi okunabilsin (B.15 dinamik fiyatlandirma).
 */
@Service
public class TrenServisi {

  private final DengeServisi denge;
  private final Saat saat;

  public TrenServisi(DengeServisi denge, Saat saat) {
    this.denge = denge;
    this.saat = saat;
  }

  /** Bir siparis karti. altinOdul = tabanFiyat x fiyatCarpani x miktar x siparisCarpani. */
  public record Siparis(int kart, String urunId, int miktar, int altinOdul, int xp) {}

  public long seferNo() {
    return seferNo(saat.simdi());
  }

  public long seferNo(Instant simdi) {
    Instant epoch = Instant.parse(denge.urunlerDosyasi().trenFiyat().epochUtc());
    long araligSn = denge.urunlerDosyasi().trenFiyat().seferAraligiSn();
    return Duration.between(epoch, simdi).getSeconds() / araligSn;
  }

  /** Bir sonraki sefere kalan saniye. */
  public long kalanSn(Instant simdi) {
    Instant epoch = Instant.parse(denge.urunlerDosyasi().trenFiyat().epochUtc());
    long araligSn = denge.urunlerDosyasi().trenFiyat().seferAraligiSn();
    long gecen = Duration.between(epoch, simdi).getSeconds();
    return araligSn - (gecen % araligSn);
  }

  /**
   * Urunun bu seferki fiyat carpani. Son {@code ileriSarmaPenceresi} seferden
   * ileri sarilarak hesaplanir — durumsuz, her cagiran ayni sonucu bulur.
   */
  public double fiyatCarpani(String urunId, long sefer) {
    var tf = denge.urunlerDosyasi().trenFiyat();
    long baslangic = Math.max(0, sefer - tf.ileriSarmaPenceresi());
    double p = tf.baslangic();
    for (long n = baslangic + 1; n <= sefer; n++) {
      double gurultu = (Deterministik.hash01("trenFiyat", urunId, n) - 0.5) * tf.gurultuGenligi();
      p = p + (1.0 - p) * tf.ortalamayaDonusKatsayisi() + gurultu;
      p = Math.max(tf.bandMin(), Math.min(tf.bandMax(), p));
    }
    return p;
  }

  /**
   * Seferin 3 siparis karti. Urunler dunya seviyesiyle ACILMIS havuzdan secilir
   * (karsilanamayacak siparis 'firsat' degil alay olur); tek kisilik vadide
   * kontenjan %70'e iner (B.7 — olceklenenler listesinde).
   */
  public List<Siparis> siparisler(Vadi vadi, int dunyaSeviyesi, long sefer) {
    var m = denge.urunlerDosyasi().manifesto();
    List<Urun> havuz = new ArrayList<>();
    for (Urun u : denge.urunlerDosyasi().urunler()) {
      if (u.acilis() == null || u.acilis().seviye() <= dunyaSeviyesi) havuz.add(u);
    }
    if (havuz.isEmpty()) return List.of();

    var sonuc = new ArrayList<Siparis>(m.siparisKartiSayisi());
    for (int kart = 0; kart < m.siparisKartiSayisi(); kart++) {
      Urun urun = havuz.get((int) (Deterministik.hash01("trenUrun", sefer, kart) * havuz.size()));
      int miktar = 4 + (int) (Deterministik.hash01("trenAdet", urun.id(), sefer, kart) * 9);
      if (!vadi.ciftMi()) {
        miktar = Math.max(2, (int) Math.round(miktar * m.tekKisilikKontenjanOrani()));
      }
      double siparisCarpani = m.carpanMin()
          + Deterministik.hash01("trenCarpan", urun.id(), sefer, kart)
            * (m.carpanMax() - m.carpanMin());
      int altin = (int) Math.round(
          urun.tabanFiyat() * fiyatCarpani(urun.id(), sefer) * miktar * siparisCarpani);
      sonuc.add(new Siparis(kart, urun.id(), miktar, Math.max(1, altin), m.siparisXp()));
    }
    return sonuc;
  }
}
