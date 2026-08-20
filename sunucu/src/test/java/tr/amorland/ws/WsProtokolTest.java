package tr.amorland.ws;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketHandler;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.client.standard.StandardWebSocketClient;
import org.springframework.web.socket.handler.TextWebSocketHandler;
import tr.amorland.TestTemeli;
import tr.amorland.kimlik.KimlikServisi;
import tr.amorland.oyun.DunyaDeposu;
import tr.amorland.vadi.Vadi;
import tr.amorland.vadi.VadiServisi;

import java.net.URI;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;

import static org.junit.jupiter.api.Assertions.*;

/**
 * WebSocket protokolunun uctan uca testi (C.3).
 *
 * <p>Iki gercek istemci baglanir, biri eker, digerinin ekranina o degisim duser.
 * Dikey dilimin "iki oyuncu bagli" sarti tam olarak burada dogrulanir.
 */
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
class WsProtokolTest extends TestTemeli {

  @LocalServerPort int port;

  @Autowired KimlikServisi kimlik;
  @Autowired VadiServisi vadiServisi;
  @Autowired DunyaDeposu dunya;
  @Autowired ObjectMapper mapper;

  /** Mesajlari kuyruga alan basit test istemcisi. */
  private static final class Istemci extends TextWebSocketHandler implements AutoCloseable {
    final BlockingQueue<JsonNode> gelenler = new LinkedBlockingQueue<>();
    final List<JsonNode> tumu = new ArrayList<>();
    WebSocketSession oturum;
    final ObjectMapper mapper;

    Istemci(ObjectMapper mapper) {
      this.mapper = mapper;
    }

    @Override
    public void handleTextMessage(WebSocketSession o, TextMessage m) throws Exception {
      JsonNode d = mapper.readTree(m.getPayload());
      tumu.add(d);
      gelenler.put(d);
    }

    void gonder(Object mesaj) throws Exception {
      oturum.sendMessage(new TextMessage(mapper.writeValueAsString(mesaj)));
    }

    /** Belirtilen tipte ilk mesaji bekler. Digerlerini yutar. */
    JsonNode bekle(String tip, long saniye) throws InterruptedException {
      long bitis = System.nanoTime() + TimeUnit.SECONDS.toNanos(saniye);
      while (System.nanoTime() < bitis) {
        JsonNode d = gelenler.poll(saniye, TimeUnit.SECONDS);
        if (d == null) break;
        if (tip.equals(d.path("t").asText())) return d;
      }
      throw new AssertionError(
          tip + " mesaji gelmedi. Gelenler: "
              + tumu.stream().map(x -> x.path("t").asText()).toList());
    }

    @Override
    public void close() throws Exception {
      if (oturum != null && oturum.isOpen()) oturum.close(CloseStatus.NORMAL);
    }
  }

  private Istemci bagla(String jwt, Long sonRevision) throws Exception {
    Istemci istemci = new Istemci(mapper);
    WebSocketHandler h = istemci;
    istemci.oturum =
        new StandardWebSocketClient()
            .execute(h, null, URI.create("ws://localhost:" + port + "/ws"))
            .get(10, TimeUnit.SECONDS);
    istemci.gonder(
        new java.util.LinkedHashMap<String, Object>() {
          {
            put("t", "BAGLAN");
            put("jwt", jwt);
            if (sonRevision != null) put("sonRevision", sonRevision);
            put("istemciSurum", "test-0.1");
          }
        });
    return istemci;
  }

  @Test
  @DisplayName("BAGLAN -> SENKRON_TAM: tam dunya durumu ve sunucu saati gelir")
  void ilkBaglanti() throws Exception {
    var oturum = kimlik.misafirGiris("cihaz-a", "Ada", 0);
    vadiServisi.kur(oturum.oyuncuId());

    try (Istemci a = bagla(oturum.erisimJetonu(), null)) {
      JsonNode tam = a.bekle("SENKRON_TAM", 10);
      assertTrue(tam.path("degisim").path("tam").asBoolean());
      assertEquals(4, tam.path("degisim").path("karolar").size(), "Sv1 parseli 4 karo");
      assertTrue(tam.path("sunucuZamaniMs").asLong() > 0, "Istemci saat farkini buradan olcer");
      assertTrue(tam.path("degisim").path("vadi").path("tohum").isMissingNode(),
          "Vadi tohumu istemciye GONDERILMEMELI (C.11)");
    }
  }

  @Test
  @DisplayName("Iki oyuncu bagli: birinin eylemi digerine DEGISIM olarak duser")
  void ikiOyuncuSenkron() throws Exception {
    var a = kimlik.misafirGiris("cihaz-a", "Ada", 0);
    var b = kimlik.misafirGiris("cihaz-b", "Bora", 1);
    Vadi vadi = vadiServisi.kur(a.oyuncuId());
    jdbc.update("UPDATE oyuncu SET seviye = 3 WHERE id = ?", a.oyuncuId());
    jdbc.update("UPDATE vadi SET dunya_seviyesi = 3 WHERE id = ?", vadi.id());
    kimlik.bagla(a.oyuncuId(), "eposta", "ada@ornek.tr");
    vadiServisi.katil(b.oyuncuId(), vadiServisi.davetKoduUret(a.oyuncuId()));

    try (Istemci ia = bagla(a.erisimJetonu(), null);
        Istemci ib = bagla(b.erisimJetonu(), null)) {

      ia.bekle("SENKRON_TAM", 10);
      ib.bekle("SENKRON_TAM", 10);

      UUID eylemId = UUID.randomUUID();
      ia.gonder(
          java.util.Map.of(
              "t", "NIYET",
              "niyet",
                  java.util.Map.of(
                      "clientActionId", eylemId.toString(),
                      "tip", "ek",
                      "hedef", java.util.Map.of("bolge", "tarla", "x", 0, "y", 0),
                      "ek", java.util.Map.of("urunId", "bugday"))));

      JsonNode onay = ia.bekle("ONAY", 10);
      assertEquals(eylemId.toString(), onay.path("clientActionId").asText());
      assertEquals(1, onay.path("degisim").path("karolar").size());

      JsonNode yayin = ib.bekle("DEGISIM", 10);
      assertEquals(onay.path("revision").asLong(), yayin.path("revision").asLong(),
          "Partner ayni revizyonu gormeli");
      assertEquals(
          "bugday", yayin.path("degisim").path("karolar").get(0).path("urunId").asText());
    }
  }

  @Test
  @DisplayName("Gecersiz niyet RED doner ve kod tasir; baglanti kopmaz")
  void redAkisi() throws Exception {
    var oturum = kimlik.misafirGiris("cihaz-a", "Ada", 0);
    vadiServisi.kur(oturum.oyuncuId());

    try (Istemci a = bagla(oturum.erisimJetonu(), null)) {
      a.bekle("SENKRON_TAM", 10);
      a.gonder(
          java.util.Map.of(
              "t", "NIYET",
              "niyet",
                  java.util.Map.of(
                      "clientActionId", UUID.randomUUID().toString(),
                      "tip", "hasat",
                      "hedef", java.util.Map.of("bolge", "tarla", "x", 0, "y", 0))));

      JsonNode red = a.bekle("RED", 10);
      assertEquals("GECERSIZ_HEDEF", red.path("kod").asText());
      assertTrue(a.oturum.isOpen(), "Red baglantiyi kopartmamali");
    }
  }

  @Test
  @DisplayName("Yeniden baglanma: sonRevision ile SENKRON_DELTA gelir")
  void yenidenBaglanmaDelta() throws Exception {
    var oturum = kimlik.misafirGiris("cihaz-a", "Ada", 0);
    Vadi vadi = vadiServisi.kur(oturum.oyuncuId());

    try (Istemci a = bagla(oturum.erisimJetonu(), null)) {
      a.bekle("SENKRON_TAM", 10);
      a.gonder(
          java.util.Map.of(
              "t", "NIYET",
              "niyet",
                  java.util.Map.of(
                      "clientActionId", UUID.randomUUID().toString(),
                      "tip", "ek",
                      "hedef", java.util.Map.of("bolge", "tarla", "x", 0, "y", 0),
                      "ek", java.util.Map.of("urunId", "bugday"))));
      a.bekle("ONAY", 10);
    }

    // Kopma sonrasi: istemci 0. revizyonda kaldigini soyler
    try (Istemci a2 = bagla(oturum.erisimJetonu(), 0L)) {
      JsonNode delta = a2.bekle("SENKRON_DELTA", 10);
      assertEquals(1, delta.path("degisimler").size());
      assertEquals(1, delta.path("degisimler").get(0).path("revision").asLong());
      assertFalse(delta.path("degisimler").get(0).path("tam").asBoolean());
    }
  }

  @Test
  @DisplayName("Konum yayini revizyon URETMEZ ve partnere gider")
  void konumYayini() throws Exception {
    var a = kimlik.misafirGiris("cihaz-a", "Ada", 0);
    var b = kimlik.misafirGiris("cihaz-b", "Bora", 1);
    Vadi vadi = vadiServisi.kur(a.oyuncuId());
    jdbc.update("UPDATE oyuncu SET seviye = 3 WHERE id = ?", a.oyuncuId());
    jdbc.update("UPDATE vadi SET dunya_seviyesi = 3 WHERE id = ?", vadi.id());
    kimlik.bagla(a.oyuncuId(), "eposta", "ada@ornek.tr");
    vadiServisi.katil(b.oyuncuId(), vadiServisi.davetKoduUret(a.oyuncuId()));

    try (Istemci ia = bagla(a.erisimJetonu(), null);
        Istemci ib = bagla(b.erisimJetonu(), null)) {
      ia.bekle("SENKRON_TAM", 10);
      ib.bekle("SENKRON_TAM", 10);

      ia.gonder(
          java.util.Map.of(
              "t", "KONUM",
              "konum", java.util.Map.of("bolge", "tarla", "x", 1.5, "y", 2.0, "yon", 90.0)));

      JsonNode konum = ib.bekle("KONUM", 10);
      assertEquals(a.oyuncuId().toString(), konum.path("oyuncuId").asText());
      assertEquals(1.5, konum.path("konum").path("x").asDouble(), 1e-6);
      assertTrue(konum.path("revision").isMissingNode(), "Konum revizyon uretmez (C.3)");
    }
  }

  @Test
  @DisplayName("Sohbet aktarilir ama sunucuda SAKLANMAZ")
  void sohbetSaklanmaz() throws Exception {
    var a = kimlik.misafirGiris("cihaz-a", "Ada", 0);
    var b = kimlik.misafirGiris("cihaz-b", "Bora", 1);
    Vadi vadi = vadiServisi.kur(a.oyuncuId());
    jdbc.update("UPDATE oyuncu SET seviye = 3 WHERE id = ?", a.oyuncuId());
    jdbc.update("UPDATE vadi SET dunya_seviyesi = 3 WHERE id = ?", vadi.id());
    kimlik.bagla(a.oyuncuId(), "eposta", "ada@ornek.tr");
    vadiServisi.katil(b.oyuncuId(), vadiServisi.davetKoduUret(a.oyuncuId()));

    try (Istemci ia = bagla(a.erisimJetonu(), null);
        Istemci ib = bagla(b.erisimJetonu(), null)) {
      ia.bekle("SENKRON_TAM", 10);
      ib.bekle("SENKRON_TAM", 10);

      ia.gonder(java.util.Map.of("t", "SOHBET", "metin", "Depoya demir biraktim"));
      JsonNode sohbet = ib.bekle("SOHBET", 10);
      assertEquals("Depoya demir biraktim", sohbet.path("mesaj").asText());

      Integer kayitli =
          jdbc.queryForObject("SELECT count(*) FROM mesaj_kuyrugu", Integer.class);
      assertEquals(0, kayitli, "Cevrimici teslim edilen mesaj kuyruga bile girmez (B.9)");
    }
  }
}
