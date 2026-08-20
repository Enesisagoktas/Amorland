package tr.amorland.ortak;

import java.security.MessageDigest;
import java.security.SecureRandom;
import java.nio.charset.StandardCharsets;
import java.util.HexFormat;
import java.util.Locale;

/**
 * Insan okunur kod uretimi: 6 haneli davet kodu, 12 haneli kurtarma kodu.
 *
 * <p>Alfabe kasitli olarak <b>karistirilabilir harfleri icermez</b>: 0/O, 1/I/L, 5/S, 2/Z yok.
 * Kod telefonla okunacak ("kodum A3F7K9"), yanlis okuma dogrudan destek talebi demektir.
 *
 * <p>Buyuk/kucuk harf donusumunde her zaman {@link Locale#ROOT} kullanilir:
 * gelistirme makinesi tr_TR locale'inde ve orada {@code "I".toLowerCase()} -> {@code "ı"} olur.
 */
public final class Kodlar {

  /** 0/O, 1/I/L, 2/Z, 5/S cikarildi. 21 harf + 6 rakam = 27 karakter. */
  private static final char[] ALFABE = "ABCDEFGHJKMNPQRTUVWXY346789".toCharArray();

  private static final SecureRandom RASTGELE = new SecureRandom();

  private Kodlar() {}

  public static String uret(int hane) {
    StringBuilder sb = new StringBuilder(hane);
    for (int i = 0; i < hane; i++) {
      sb.append(ALFABE[RASTGELE.nextInt(ALFABE.length)]);
    }
    return sb.toString();
  }

  /** Kullanicinin yazdigi kodu normalize eder: bosluk/tire atilir, buyuk harfe cevrilir. */
  public static String normalize(String kod) {
    if (kod == null) return null;
    return kod.replaceAll("[\\s\\-_]", "").toUpperCase(Locale.ROOT);
  }

  /** Kurtarma kodu ve yenileme jetonu duz metin saklanmaz. */
  public static String ozetle(String duzMetin) {
    try {
      MessageDigest md = MessageDigest.getInstance("SHA-256");
      return HexFormat.of().formatHex(md.digest(duzMetin.getBytes(StandardCharsets.UTF_8)));
    } catch (Exception e) {
      throw new IllegalStateException("SHA-256 yok", e);
    }
  }

  /** Vadi tohumu: deterministik fiyat/pencere uretiminin girdisi. Istemciye GONDERILMEZ (C.11). */
  public static long vadiTohumu() {
    return RASTGELE.nextLong();
  }
}
