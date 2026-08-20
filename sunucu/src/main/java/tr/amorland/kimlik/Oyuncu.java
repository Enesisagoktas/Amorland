package tr.amorland.kimlik;

import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.UUID;

/**
 * Oyuncu kaydi. Misafir hesapla baslar (cihaz kimligi), sonradan baglanabilir (B.19).
 */
public record Oyuncu(
    UUID id,
    String cihazId,
    String baglantiTipi,
    String baglantiRef,
    String kurtarmaKoduHash,
    String ad,
    int renk,
    int seviye,
    int xp,
    Integer dogumYili,
    int bagliKabulTeklif,
    UUID aktifOturum,
    Instant sonGorulme,
    Instant olusturuldu) {

  public boolean bagliMi() {
    return baglantiTipi != null;
  }

  public boolean kurtarmaKoduVarMi() {
    return kurtarmaKoduHash != null;
  }

  /**
   * Yas kapisi (B.9): esik altindaysa sohbet ve partner daveti kapalidir.
   * Engelleme degil, kisitlama — oyun tek kisilik oynanabilir.
   *
   * @param esik ilerleme.json yasKapisi.esik
   * @param simdi sunucu saati (mimari kural 5)
   */
  public boolean yasKapisiAltindaMi(int esik, Instant simdi) {
    if (dogumYili == null) return false; // henuz sorulmadi
    int buYil = LocalDate.ofInstant(simdi, ZoneOffset.UTC).getYear();
    return (buYil - dogumYili) < esik;
  }
}
