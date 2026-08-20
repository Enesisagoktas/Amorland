package tr.amorland.kimlik;

import tr.amorland.ortak.OyunHatasi;
import tr.amorland.ortak.RedKodu;

import java.util.UUID;

/**
 * O anki istegin kimligi. {@link JwtFiltresi} doldurur, uclar okur.
 *
 * <p>Spring Security kullanmiyoruz: tek bir kimlik kaynagi (JWT) ve iki yetki durumu
 * (var / yok) var. Tam bir guvenlik cercevesi burada kazanctan cok yapilandirma getirir.
 */
public final class IstekBaglami {

  private static final ThreadLocal<Kimlik> AKTIF = new ThreadLocal<>();

  private IstekBaglami() {}

  public record Kimlik(UUID oyuncuId, UUID oturumId) {}

  public static void ayarla(Kimlik kimlik) {
    AKTIF.set(kimlik);
  }

  public static void temizle() {
    AKTIF.remove();
  }

  public static Kimlik kimlik() {
    Kimlik k = AKTIF.get();
    if (k == null) {
      throw new OyunHatasi(RedKodu.YETKISIZ, "Oturum yok.");
    }
    return k;
  }

  public static UUID oyuncuId() {
    return kimlik().oyuncuId();
  }

  public static boolean varMi() {
    return AKTIF.get() != null;
  }
}
