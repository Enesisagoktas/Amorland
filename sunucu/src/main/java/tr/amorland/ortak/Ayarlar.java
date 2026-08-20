package tr.amorland.ortak;

import org.springframework.boot.context.properties.ConfigurationProperties;

/**
 * {@code application.yml} icindeki {@code amorland.*} ayarlari.
 *
 * <p>Burada <b>oyun dengesi yoktur</b> (mimari kural 8) — yalnizca isletim ayarlari:
 * jeton omru, hiz siniri, protokol esikleri.
 */
@ConfigurationProperties(prefix = "amorland")
public record Ayarlar(Jwt jwt, HizSiniri hizSiniri, Revizyon revizyon, int eylemKaydiTtlSaat) {

  public record Jwt(String gizliAnahtar, int erisimOmruDk, int yenilemeOmruGun) {}

  public record HizSiniri(int eylemSaniyede, int sohbetSaniyede, int jetonGunde) {}

  public record Revizyon(int deltaEsigi, int kayitTutma) {}
}
