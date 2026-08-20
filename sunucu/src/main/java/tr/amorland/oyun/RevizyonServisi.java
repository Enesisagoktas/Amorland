package tr.amorland.oyun;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Service;
import tr.amorland.ortak.Ayarlar;
import tr.amorland.oyun.DunyaModeli.Degisim;

import java.sql.Timestamp;
import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;

/**
 * Dunya revizyonu (C.1) — protokolun kalbi.
 *
 * <p>Her durum degistiren eylem revizyonu bir artirir ve degisimi
 * {@code revizyon_kaydi}'na yazar. Yeniden baglanan istemci "1042'deyim" der,
 * sunucu 1043 ve sonrasini gonderir. Fark esikten buyukse tam anlik goruntu gider.
 *
 * <p>Bu tek kavram uc problemi birden cozer: yeniden baglanma, cakisma cozumu,
 * ve iki istemcinin senkron kalmasi.
 */
@Service
public class RevizyonServisi {

  private final NamedParameterJdbcTemplate jdbc;
  private final ObjectMapper mapper;
  private final Ayarlar ayarlar;

  public RevizyonServisi(NamedParameterJdbcTemplate jdbc, ObjectMapper mapper, Ayarlar ayarlar) {
    this.jdbc = jdbc;
    this.mapper = mapper;
    this.ayarlar = ayarlar;
  }

  public int deltaEsigi() {
    return ayarlar.revizyon().deltaEsigi();
  }

  /** Degisimi kaydeder. Revizyon numarasi cagiran tarafindan zaten artirilmis olmalidir. */
  public void kaydet(UUID vadiId, Degisim degisim) {
    try {
      jdbc.update(
          "INSERT INTO revizyon_kaydi (vadi_id, revision, degisim, olusturuldu) "
              + "VALUES (:v, :r, CAST(:d AS jsonb), :s) ON CONFLICT DO NOTHING",
          new MapSqlParameterSource()
              .addValue("v", vadiId)
              .addValue("r", degisim.revision())
              .addValue("d", mapper.writeValueAsString(degisim))
              .addValue("s", Timestamp.from(degisim.sunucuZamani())));
    } catch (JsonProcessingException e) {
      throw new IllegalStateException("Degisim JSON'a cevrilemedi", e);
    }
    budaAsiriKayit(vadiId, degisim.revision());
  }

  /** Son N revizyon tutulur; oncesi icin tam anlik goruntu gonderilir (C.2). */
  private void budaAsiriKayit(UUID vadiId, long guncelRevizyon) {
    long esik = guncelRevizyon - ayarlar.revizyon().kayitTutma();
    if (esik <= 0) return;
    jdbc.update(
        "DELETE FROM revizyon_kaydi WHERE vadi_id = :v AND revision < :e",
        Map.of("v", vadiId, "e", esik));
  }

  /**
   * Istemcinin bildigi revizyondan sonraki degisimleri getirir.
   *
   * @return bos ise istemci zaten guncel; {@link Optional#empty()} ise kayit eksik
   *     ve tam anlik goruntu gonderilmeli
   */
  public Optional<List<Degisim>> deltalar(UUID vadiId, long sonBilinen, long guncel) {
    if (sonBilinen >= guncel) return Optional.of(List.of());
    if (guncel - sonBilinen > ayarlar.revizyon().deltaEsigi()) return Optional.empty();

    List<String> satirlar =
        jdbc.queryForList(
            "SELECT degisim::text FROM revizyon_kaydi "
                + "WHERE vadi_id = :v AND revision > :r ORDER BY revision",
            Map.of("v", vadiId, "r", sonBilinen),
            String.class);

    // Kayit budanmis olabilir: beklenen sayida satir yoksa tam goruntu sart.
    if (satirlar.size() != guncel - sonBilinen) return Optional.empty();

    List<Degisim> sonuc = new ArrayList<>(satirlar.size());
    for (String s : satirlar) {
      try {
        sonuc.add(mapper.readValue(s, Degisim.class));
      } catch (JsonProcessingException e) {
        return Optional.empty(); // bozuk kayit -> tam goruntu
      }
    }
    return Optional.of(sonuc);
  }

  // ------------------------------------------------------------------ idempotency

  /**
   * Ayni {@code client_action_id} ikinci kez gelirse ilk sonucu doner (mimari kural 4).
   * Mobilde baglanti surekli kopar; istemci gonderdigini bilemez, tekrar gonderir.
   */
  public Optional<Degisim> oncekiSonuc(UUID clientActionId) {
    List<String> satir =
        jdbc.queryForList(
            "SELECT sonuc::text FROM eylem_kaydi WHERE client_action_id = :c",
            Map.of("c", clientActionId),
            String.class);
    if (satir.isEmpty()) return Optional.empty();
    try {
      return Optional.of(mapper.readValue(satir.get(0), Degisim.class));
    } catch (JsonProcessingException e) {
      return Optional.empty();
    }
  }

  public void eylemKaydet(UUID clientActionId, UUID oyuncuId, UUID vadiId, Degisim sonuc) {
    try {
      jdbc.update(
          "INSERT INTO eylem_kaydi (client_action_id, oyuncu_id, vadi_id, revision, sonuc, olusturuldu) "
              + "VALUES (:c, :o, :v, :r, CAST(:s AS jsonb), :z) ON CONFLICT DO NOTHING",
          new MapSqlParameterSource()
              .addValue("c", clientActionId)
              .addValue("o", oyuncuId)
              .addValue("v", vadiId)
              .addValue("r", sonuc.revision())
              .addValue("s", mapper.writeValueAsString(sonuc))
              .addValue("z", Timestamp.from(sonuc.sunucuZamani())));
    } catch (JsonProcessingException e) {
      throw new IllegalStateException("Eylem sonucu JSON'a cevrilemedi", e);
    }
  }

  /** TTL 24 saat (C.2): mobil yeniden baglanma gunlerce surmez. */
  public int eskiEylemKayitlariniTemizle(Instant simdi) {
    return jdbc.update(
        "DELETE FROM eylem_kaydi WHERE olusturuldu < :e",
        new MapSqlParameterSource()
            .addValue("e", Timestamp.from(simdi.minusSeconds(ayarlar.eylemKaydiTtlSaat() * 3600L))));
  }
}
