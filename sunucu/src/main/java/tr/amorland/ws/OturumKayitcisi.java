package tr.amorland.ws;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.WebSocketSession;

import java.io.IOException;
import java.util.Collections;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Canli WebSocket oturumlari: kim hangi vadide, kim cevrimici.
 *
 * <p>Tek sunucu ornegi varsayimiyla bellekte tutulur. Coklu ornege gecilirse burasi
 * Redis pub/sub ile degistirilir; disari acilan yuzey (yayin, cevrimici sorgusu)
 * ayni kalir diye ayri bir sinif.
 */
@Component
public class OturumKayitcisi {

  private static final Logger log = LoggerFactory.getLogger(OturumKayitcisi.class);

  /** oyuncuId -> canli oturum. Oyuncu basina TEK oturum: son baglanan kazanir (C.5). */
  private final Map<UUID, Kayit> oyuncuOturumlari = new ConcurrentHashMap<>();

  /** vadiId -> o vadideki oyuncular. */
  private final Map<UUID, Set<UUID>> vadiUyeleri = new ConcurrentHashMap<>();

  public record Kayit(WebSocketSession oturum, UUID oyuncuId, UUID vadiId, UUID oturumId) {}

  /**
   * Oturumu kaydeder.
   *
   * @return varsa dusurulmesi gereken onceki oturum
   */
  public Kayit kaydet(WebSocketSession oturum, UUID oyuncuId, UUID vadiId, UUID oturumId) {
    Kayit yeni = new Kayit(oturum, oyuncuId, vadiId, oturumId);
    Kayit eski = oyuncuOturumlari.put(oyuncuId, yeni);
    vadiUyeleri.computeIfAbsent(vadiId, k -> ConcurrentHashMap.newKeySet()).add(oyuncuId);
    return eski;
  }

  public void cikar(String wsOturumId) {
    oyuncuOturumlari.entrySet().removeIf(
        e -> {
          if (!e.getValue().oturum().getId().equals(wsOturumId)) return false;
          Set<UUID> uyeler = vadiUyeleri.get(e.getValue().vadiId());
          if (uyeler != null) {
            uyeler.remove(e.getKey());
            if (uyeler.isEmpty()) vadiUyeleri.remove(e.getValue().vadiId());
          }
          return true;
        });
  }

  public Kayit kayit(String wsOturumId) {
    return oyuncuOturumlari.values().stream()
        .filter(k -> k.oturum().getId().equals(wsOturumId))
        .findFirst()
        .orElse(null);
  }

  public Set<UUID> cevrimiciler(UUID vadiId) {
    return Collections.unmodifiableSet(
        vadiUyeleri.getOrDefault(vadiId, Collections.emptySet()));
  }

  public boolean cevrimiciMi(UUID oyuncuId) {
    return oyuncuOturumlari.containsKey(oyuncuId);
  }

  public Kayit oyuncununOturumu(UUID oyuncuId) {
    return oyuncuOturumlari.get(oyuncuId);
  }

  /** Vadideki digerlerine gonderir. Gonderen haric. */
  public void yayinla(UUID vadiId, UUID haricOyuncu, java.util.function.Consumer<Kayit> gonder) {
    for (UUID uye : cevrimiciler(vadiId)) {
      if (uye.equals(haricOyuncu)) continue;
      Kayit k = oyuncuOturumlari.get(uye);
      if (k == null || !k.oturum().isOpen()) continue;
      try {
        gonder.accept(k);
      } catch (Exception e) {
        log.warn("Yayin basarisiz, oyuncu {}: {}", uye, e.toString());
      }
    }
  }

  public void kapat(Kayit kayit, String sebep) {
    try {
      kayit.oturum().close(new org.springframework.web.socket.CloseStatus(4001, sebep));
    } catch (IOException e) {
      log.debug("Oturum kapatilamadi: {}", e.toString());
    }
  }
}
