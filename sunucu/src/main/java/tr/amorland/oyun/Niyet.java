package tr.amorland.oyun;

import java.util.UUID;

/**
 * Istemcinin gonderdigi niyet (C.3).
 *
 * <p>Mimari kural 1: istemci <b>niyet</b> gonderir, sonuc gondermez. Bu kayitta
 * miktar, fiyat, XP veya sure alani <b>yoktur</b> — olsaydi istemci "500 altin kazandim"
 * diyebilirdi. Yalnizca "ne, nerede" tasinir.
 *
 * <p>Mimari kural 4: her komut {@code clientActionId} tasir. Sunucu tekrari yutar.
 */
public record Niyet(
    UUID clientActionId,
    String tip,
    Hedef hedef,
    Ek ek) {

  /** Hedef karo veya dugum. Parsel null ise oyuncunun kendi parseli varsayilir. */
  public record Hedef(String bolge, Integer x, Integer y, UUID parsel) {}

  /**
   * Eyleme ozgu ek bilgi. Buradaki {@code adet} bile <b>talep</b>tir, sonuc degil:
   * sunucu kapasiteye ve stoka gore kirpar.
   */
  public record Ek(String urunId, String esyaId, Integer adet, String tarifId, Long sefer) {

    /** Sefersiz kisa bicim — tren disindaki niyetler sefer tasimaz. */
    public Ek(String urunId, String esyaId, Integer adet, String tarifId) {
      this(urunId, esyaId, adet, tarifId, null);
    }
  }

  public String bolge() {
    return hedef == null ? null : hedef.bolge();
  }

  public int x() {
    return hedef == null || hedef.x() == null ? 0 : hedef.x();
  }

  public int y() {
    return hedef == null || hedef.y() == null ? 0 : hedef.y();
  }
}
