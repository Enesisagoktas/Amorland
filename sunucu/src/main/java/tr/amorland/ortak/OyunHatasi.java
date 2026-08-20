package tr.amorland.ortak;

/**
 * Bir niyetin reddedilme sebebi. Istemciye {@code RED} mesaji olarak doner.
 *
 * <p>Bu bir "beklenmeyen hata" degildir: oyunun normal akisinin parcasidir
 * (depo dolu, partner once aldi). Yigin izi tutulmaz, cunku saniyede onlarcasi olabilir.
 */
public class OyunHatasi extends RuntimeException {

  private final RedKodu kod;

  public OyunHatasi(RedKodu kod, String mesaj) {
    super(mesaj, null, false, false);
    this.kod = kod;
  }

  public OyunHatasi(RedKodu kod) {
    this(kod, kod.name());
  }

  public RedKodu kod() {
    return kod;
  }

  public static OyunHatasi depoDolu(int kapasite, int dolu) {
    return new OyunHatasi(
        RedKodu.DEPO_DOLU, "Depo dolu: " + dolu + "/" + kapasite + ". Urun tarlada bekliyor.");
  }

  public static OyunHatasi cantaDolu(int slot) {
    return new OyunHatasi(RedKodu.CANTA_DOLU, "Cantada bos slot yok (" + slot + " slot dolu).");
  }

  public static OyunHatasi seviyeYetersiz(String ne, int gereken) {
    return new OyunHatasi(RedKodu.SEVIYE_YETERSIZ, ne + " icin Sv " + gereken + " gerekli.");
  }
}
