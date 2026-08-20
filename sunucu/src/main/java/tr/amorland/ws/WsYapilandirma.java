package tr.amorland.ws;

import org.springframework.context.annotation.Configuration;
import org.springframework.web.socket.config.annotation.EnableWebSocket;
import org.springframework.web.socket.config.annotation.WebSocketConfigurer;
import org.springframework.web.socket.config.annotation.WebSocketHandlerRegistry;
import org.springframework.web.socket.server.standard.ServletServerContainerFactoryBean;

/**
 * WebSocket ucunu kaydeder.
 *
 * <p>STOMP kullanilmiyor: protokol zaten kendi zarfimiz ({@code {"t": ...}}) ve
 * STOMP'un abonelik modeli iki kisilik bir vadide fazladan katman olurdu.
 * "Kendi WebSocket kanalin" (B.9) tam olarak bu.
 */
@Configuration
@EnableWebSocket
public class WsYapilandirma implements WebSocketConfigurer {

  private final OyunWsIsleyici isleyici;

  public WsYapilandirma(OyunWsIsleyici isleyici) {
    this.isleyici = isleyici;
  }

  @Override
  public void registerWebSocketHandlers(WebSocketHandlerRegistry kayit) {
    // Kimlik BAGLAN mesajindaki JWT ile kurulur, handshake basliginda degil:
    // mobil WebSocket istemcileri ozel baslik gonderemeyebiliyor.
    kayit.addHandler(isleyici, "/ws").setAllowedOriginPatterns("*");
  }

  @org.springframework.context.annotation.Bean
  public ServletServerContainerFactoryBean wsKapsayici() {
    var fabrika = new ServletServerContainerFactoryBean();
    fabrika.setMaxTextMessageBufferSize(256 * 1024); // SENKRON_TAM buyuk olabilir
    fabrika.setMaxSessionIdleTimeout(120_000L);
    return fabrika;
  }
}
