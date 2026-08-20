package tr.amorland.kimlik;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.JwtException;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;
import org.springframework.stereotype.Service;
import tr.amorland.ortak.Ayarlar;
import tr.amorland.ortak.Saat;

import javax.crypto.SecretKey;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.time.Instant;
import java.util.Date;
import java.util.Optional;
import java.util.UUID;

/**
 * Kisa omurlu erisim jetonu uretir ve dogrular (C.11).
 *
 * <p>Yenileme jetonu JWT degildir: rastgele bir kod uretilir, ozeti veritabaninda
 * saklanir ve iptal edilebilir. JWT iptal edilemez, o yuzden omru kisa tutulur.
 */
@Service
public class JwtServisi {

  private final SecretKey anahtar;
  private final Duration erisimOmru;
  private final Saat saat;

  public JwtServisi(Ayarlar ayarlar, Saat saat) {
    byte[] ham = ayarlar.jwt().gizliAnahtar().getBytes(StandardCharsets.UTF_8);
    if (ham.length < 32) {
      throw new IllegalStateException(
          "amorland.jwt.gizli-anahtar en az 32 bayt olmali (su an " + ham.length + ").");
    }
    this.anahtar = Keys.hmacShaKeyFor(ham);
    this.erisimOmru = Duration.ofMinutes(ayarlar.jwt().erisimOmruDk());
    this.saat = saat;
  }

  /**
   * Erisim jetonu uretir.
   *
   * @param oturumId her girisle degisen kimlik. "Son baglanan kazanir" (C.5) kurali
   *     bunun uzerinden isler: eski jetonun oturumId'si artik gecerli degildir.
   */
  public String erisimJetonu(UUID oyuncuId, UUID oturumId) {
    Instant simdi = saat.simdi();
    return Jwts.builder()
        .subject(oyuncuId.toString())
        .claim("oturum", oturumId.toString())
        .issuedAt(Date.from(simdi))
        .expiration(Date.from(simdi.plus(erisimOmru)))
        .signWith(anahtar)
        .compact();
  }

  public Optional<JwtIcerik> coz(String jeton) {
    try {
      Claims c =
          Jwts.parser()
              .verifyWith(anahtar)
              .clock(() -> Date.from(saat.simdi()))
              .build()
              .parseSignedClaims(jeton)
              .getPayload();
      return Optional.of(
          new JwtIcerik(
              UUID.fromString(c.getSubject()),
              UUID.fromString(c.get("oturum", String.class))));
    } catch (JwtException | IllegalArgumentException e) {
      return Optional.empty();
    }
  }

  public long erisimOmruSn() {
    return erisimOmru.toSeconds();
  }

  public record JwtIcerik(UUID oyuncuId, UUID oturumId) {}
}
