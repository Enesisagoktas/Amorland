package tr.amorland.kimlik;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.List;

/**
 * {@code Authorization: Bearer ...} basligini cozer ve {@link IstekBaglami}'na yazar.
 *
 * <p>Jetonun gecerli olmasi yetmez: jetondaki oturum kimligi oyuncunun
 * <b>aktif</b> oturumuyla ayni olmali. Boylece "son baglanan kazanir" (C.5)
 * kurali REST tarafinda da isler; eski cihazin jetonu suresi dolmadan gecersizlesir.
 */
@Component
@Order(1)
public class JwtFiltresi extends OncePerRequestFilter {

  /** Kimlik gerektirmeyen uclar. */
  private static final List<String> ACIK_YOLLAR =
      List.of("/api/denge", "/api/oturum/misafir", "/api/oturum/yenile", "/api/oturum/kurtar",
              "/api/saglik", "/ws");

  private final JwtServisi jwt;
  private final OyuncuDeposu oyuncular;

  public JwtFiltresi(JwtServisi jwt, OyuncuDeposu oyuncular) {
    this.jwt = jwt;
    this.oyuncular = oyuncular;
  }

  @Override
  protected boolean shouldNotFilter(HttpServletRequest istek) {
    String yol = istek.getRequestURI();
    return ACIK_YOLLAR.stream().anyMatch(yol::startsWith);
  }

  @Override
  protected void doFilterInternal(
      HttpServletRequest istek, HttpServletResponse yanit, FilterChain zincir)
      throws ServletException, IOException {

    String baslik = istek.getHeader("Authorization");
    if (baslik == null || !baslik.startsWith("Bearer ")) {
      yanit.sendError(HttpServletResponse.SC_UNAUTHORIZED, "YETKISIZ");
      return;
    }

    var icerik = jwt.coz(baslik.substring(7));
    if (icerik.isEmpty()) {
      yanit.sendError(HttpServletResponse.SC_UNAUTHORIZED, "YETKISIZ");
      return;
    }

    var oyuncu = oyuncular.bul(icerik.get().oyuncuId());
    if (oyuncu.isEmpty()) {
      yanit.sendError(HttpServletResponse.SC_UNAUTHORIZED, "YETKISIZ");
      return;
    }
    if (!icerik.get().oturumId().equals(oyuncu.get().aktifOturum())) {
      yanit.sendError(HttpServletResponse.SC_UNAUTHORIZED, "BASKA_CIHAZ");
      return;
    }

    try {
      IstekBaglami.ayarla(new IstekBaglami.Kimlik(icerik.get().oyuncuId(), icerik.get().oturumId()));
      zincir.doFilter(istek, yanit);
    } finally {
      IstekBaglami.temizle();
    }
  }
}
