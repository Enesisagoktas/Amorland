package tr.amorland.oyun;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/**
 * Istemciye giden dunya durumu ve degisim paketleri.
 *
 * <p>Tasarim: <b>tam anlik goruntu ile delta ayni sekle sahiptir.</b> Istemci tek bir
 * uygulama koduna sahip olur; delta'da alan yoksa dokunulmaz, varsa uzerine yazilir.
 * Bu, C.1'deki "fark 200'den buyukse tam goruntu" dallanmasini istemci tarafinda
 * ikinci bir kod yolu olmaktan cikarir.
 *
 * <p>Mimari kural 2 ve 3: burada <b>buyume asamasi, kalan sure veya fiyat YOKTUR</b>.
 * {@code ekildiAt} gonderilir, istemci gorsel asamayi {@code sunucuZamani} farkina
 * gore kendisi cizer. Odul ve miktar hesabi istemcide asla yapilmaz (kural 7).
 */
public final class DunyaModeli {

  private DunyaModeli() {}

  /** Bir tarla karosu. Parsel sahibi haric kimse dokunamaz (B.7). */
  public record Karo(
      UUID parselSahibi,
      int x,
      int y,
      String urunId,
      Instant ekildiAt,
      Instant sulandiAt,
      int hasatSayaci,
      Instant sonHasatAt,
      boolean gubreli) {

    public boolean bosMu() {
      return urunId == null;
    }
  }

  /** Envanter satiri. adet 0 ise istemci satiri siler. */
  public record EnvanterSatiri(UUID sahip, String esyaId, int adet) {}

  /** Depo yigini. koyan alani renk rozetini besler (B.10). */
  public record DepoSatiri(String esyaId, int adet, UUID koyan, boolean kilitli, UUID kilitleyen) {}

  /** Yabani kaynak / agac / cevher dugumu. kirildiAt null ise dugum hazir. */
  public record Dugum(
      String bolge,
      int x,
      int y,
      String tur,
      String sinif,
      int saglik,
      int maksSaglik,
      Instant kirildiAt,
      Instant hazirOlacakAt) {

    public boolean hazirMi(Instant simdi) {
      return hazirOlacakAt == null || !simdi.isBefore(hazirOlacakAt);
    }
  }

  /** Alet ve kalan dayanikliligi. */
  public record Alet(UUID sahip, String aletId, int dayaniklilik, int maksDayaniklilik) {}

  /** Oyuncu ozeti. Partnerin de gorunur alanlari buradan gelir. */
  public record OyuncuOzeti(
      UUID id,
      String ad,
      int renk,
      int seviye,
      int xp,
      int sonrakiSeviyeXp,
      long altin,
      int cantaSlot,
      int kovaKalan,
      int kovaKapasite,
      boolean cevrimici,
      Instant sonGorulme) {}

  /** Vadi ozeti. tohum burada YOKTUR (C.11). */
  public record VadiOzeti(
      UUID id,
      UUID kuranId,
      UUID partnerId,
      int dunyaSeviyesi,
      boolean tekImzaModu,
      int depoKapasitesi,
      int depoDolu,
      boolean kuslukAktif,
      UUID kuslukBaslatan) {}

  /**
   * Tek seferlik olaylar. Durum degil, <b>bildirim</b>: istemci bunlari oynatir
   * (seviye atlama animasyonu, defter sayfasi acilisi) ve saklamaz.
   */
  public record Olay(String tip, String deger, Integer sayi) {

    public static Olay seviyeAtlama(int seviye) {
      return new Olay("seviye_atlama", null, seviye);
    }

    public static Olay defterSayfasi(int no) {
      return new Olay("defter_sayfasi", null, no);
    }

    public static Olay bolgeAcildi(String bolgeId) {
      return new Olay("bolge_acildi", bolgeId, null);
    }

    public static Olay ilkKez(String an) {
      return new Olay("ilk_kez", an, null);
    }

    public static Olay xpKazandi(int xp) {
      return new Olay("xp", null, xp);
    }

    public static Olay aletUyarisi(String aletId) {
      return new Olay("alet_dayaniklilik_dustu", aletId, null);
    }

    public static Olay aletKirildi(String aletId) {
      return new Olay("alet_kirildi", aletId, null);
    }

    public static Olay depoDoldu() {
      return new Olay("depo_doldu", null, null);
    }
  }

  /**
   * Durum degisimi. Bos listeler "degismedi" demektir.
   *
   * @param tam true ise istemci yerel durumunu atip bunun yerine koyar (SENKRON_TAM)
   */
  public record Degisim(
      long revision,
      Instant sunucuZamani,
      boolean tam,
      List<Karo> karolar,
      List<EnvanterSatiri> envanter,
      List<DepoSatiri> depo,
      List<Dugum> dugumler,
      List<Alet> aletler,
      List<OyuncuOzeti> oyuncular,
      VadiOzeti vadi,
      List<String> isaretler,
      List<Olay> olaylar) {

    public static Degisim bos(long revision, Instant simdi) {
      return new Degisim(
          revision, simdi, false,
          List.of(), List.of(), List.of(), List.of(), List.of(), List.of(),
          null, List.of(), List.of());
    }
  }

  /** Bir eylemin sonucu: yeni revizyon + o eylemin urettigi degisim. */
  public record EylemSonucu(long revision, Degisim degisim) {}
}
