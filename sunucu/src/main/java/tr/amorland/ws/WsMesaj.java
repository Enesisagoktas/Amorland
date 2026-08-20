package tr.amorland.ws;

import com.fasterxml.jackson.annotation.JsonInclude;
import tr.amorland.oyun.DunyaModeli.Degisim;
import tr.amorland.oyun.Niyet;

import java.util.List;
import java.util.UUID;

/**
 * WebSocket protokolu (C.3).
 *
 * <p>Tek bir zarf: {@code {"t": "<tip>", ...}}. Tip alani protokolun parcasidir.
 * Gelen ve giden mesajlar ayri kayitlar olarak tanimli — istemci gonderebilecegi
 * mesajlarla sunucunun gonderdiklerini karistiramaz.
 */
public final class WsMesaj {

  private WsMesaj() {}

  // ---------------------------------------------------------------- istemciden gelen

  /** Istemcinin gonderebilecegi butun mesajlar tek kayitta; kullanilmayan alanlar null. */
  @JsonInclude(JsonInclude.Include.NON_NULL)
  public record Gelen(
      String t,
      String jwt,
      Long sonRevision,
      String istemciSurum,
      Niyet niyet,
      Konum konum,
      String metin,
      String sablonId,
      String nesneId) {}

  /** Konum guncellemesi 5 Hz, revizyon URETMEZ (C.3). */
  public record Konum(String bolge, double x, double y, double yon) {}

  // ---------------------------------------------------------------- sunucudan giden

  @JsonInclude(JsonInclude.Include.NON_NULL)
  public record Giden(
      String t,
      Long revision,
      Degisim degisim,
      List<Degisim> degisimler,
      UUID clientActionId,
      String kod,
      String mesaj,
      UUID oyuncuId,
      Konum konum,
      String sablonId,
      String nesneId,
      Long sunucuZamaniMs) {

    public static Giden senkronTam(Degisim tam, long sunucuZamaniMs) {
      return new Giden(
          "SENKRON_TAM", tam.revision(), tam, null, null, null, null, null, null, null, null,
          sunucuZamaniMs);
    }

    public static Giden senkronDelta(List<Degisim> deltalar, long revision, long sunucuZamaniMs) {
      return new Giden(
          "SENKRON_DELTA", revision, null, deltalar, null, null, null, null, null, null, null,
          sunucuZamaniMs);
    }

    public static Giden onay(UUID clientActionId, Degisim degisim) {
      return new Giden(
          "ONAY", degisim.revision(), degisim, null, clientActionId, null, null, null, null, null,
          null, degisim.sunucuZamani().toEpochMilli());
    }

    public static Giden red(UUID clientActionId, String kod, String mesaj) {
      return new Giden(
          "RED", null, null, null, clientActionId, kod, mesaj, null, null, null, null, null);
    }

    public static Giden degisim(Degisim degisim) {
      return new Giden(
          "DEGISIM", degisim.revision(), degisim, null, null, null, null, null, null, null, null,
          degisim.sunucuZamani().toEpochMilli());
    }

    public static Giden konum(UUID oyuncuId, Konum konum) {
      return new Giden(
          "KONUM", null, null, null, null, null, null, oyuncuId, konum, null, null, null);
    }

    public static Giden sohbet(UUID gonderen, String metin) {
      return new Giden(
          "SOHBET", null, null, null, null, null, metin, gonderen, null, null, null, null);
    }

    public static Giden notTahtasi(UUID gonderen, String sablonId, String nesneId) {
      return new Giden(
          "NOT_TAHTASI", null, null, null, null, null, null, gonderen, null, sablonId, nesneId, null);
    }

    public static Giden hata(String kod, String mesaj) {
      return new Giden("HATA", null, null, null, null, kod, mesaj, null, null, null, null, null);
    }

    /** Partnerin cevrimici/cevrimdisi durumu degisti. */
    public static Giden varlik(UUID oyuncuId, boolean cevrimici) {
      return new Giden(
          cevrimici ? "PARTNER_GIRDI" : "PARTNER_CIKTI",
          null, null, null, null, null, null, oyuncuId, null, null, null, null);
    }
  }
}
