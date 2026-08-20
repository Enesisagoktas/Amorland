package tr.amorland.ortak;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;

/**
 * Deterministik sozde-rastgelelik.
 *
 * <p>Mimari kural 3: tren fiyatlari ve pencereler <b>uretilir, saklanmaz.</b> Ayni
 * girdilerle her zaman ayni sonuc cikar; veritabanina satir yazilmaz ve iki sunucu
 * ornegi ayni degeri hesaplar.
 *
 * <p>Yabani kaynak getirisinde de saf {@link java.util.Random} yerine bu kullanilir:
 * ayni eylem tekrar gelirse (yeniden baglanma) ayni getiri cikar, oyuncu
 * "cevrimi kacirdim, az verdi" hissi yasamaz.
 */
public final class Deterministik {

  private Deterministik() {}

  /**
   * SHA-256(parcalar) ilk 8 bayti / 2^64 -> [0, 1) araliginda cift duyarlikli sayi.
   * C.3'teki {@code hash01} tanimiyla ayni.
   */
  public static double hash01(Object... parcalar) {
    try {
      MessageDigest md = MessageDigest.getInstance("SHA-256");
      for (Object p : parcalar) {
        md.update(String.valueOf(p).getBytes(StandardCharsets.UTF_8));
        md.update((byte) 0x1F); // ayirici: ("ab","c") ile ("a","bc") ayni olmasin
      }
      byte[] ozet = md.digest();
      long v = 0;
      for (int i = 0; i < 8; i++) {
        v = (v << 8) | (ozet[i] & 0xFFL);
      }
      // Isaretsiz yoruma cevir
      return (v >>> 11) / (double) (1L << 53);
    } catch (Exception e) {
      throw new IllegalStateException("SHA-256 yok", e);
    }
  }

  /** [enAz, enCok] araliginda deterministik tam sayi. */
  public static int aralik(int enAz, int enCok, Object... parcalar) {
    if (enCok <= enAz) return enAz;
    double h = hash01(parcalar);
    return enAz + (int) Math.floor(h * (enCok - enAz + 1));
  }

  /** Agirlikli secim. agirliklar toplami 0 ise ilk secenek doner. */
  public static int agirlikliSec(int[] agirliklar, Object... parcalar) {
    int toplam = 0;
    for (int a : agirliklar) toplam += a;
    if (toplam <= 0) return 0;
    double h = hash01(parcalar) * toplam;
    int birikim = 0;
    for (int i = 0; i < agirliklar.length; i++) {
      birikim += agirliklar[i];
      if (h < birikim) return i;
    }
    return agirliklar.length - 1;
  }
}
