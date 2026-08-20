package tr.amorland.vadi;

import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Repository;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Timestamp;
import java.time.Instant;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;

@Repository
public class VadiDeposu {

  private static final RowMapper<Vadi> ESLEYICI =
      (ResultSet rs, int i) ->
          new Vadi(
              rs.getObject("id", UUID.class),
              rs.getObject("kuran_id", UUID.class),
              rs.getObject("partner_id", UUID.class),
              rs.getLong("revision"),
              rs.getLong("tohum"),
              rs.getShort("dunya_seviyesi"),
              rs.getBoolean("tek_imza_modu"),
              zaman(rs, "partner_son_gorulme"),
              zaman(rs, "olusturuldu"));

  private static Instant zaman(ResultSet rs, String alan) throws SQLException {
    Timestamp ts = rs.getTimestamp(alan);
    return ts == null ? null : ts.toInstant();
  }

  private final NamedParameterJdbcTemplate jdbc;

  public VadiDeposu(NamedParameterJdbcTemplate jdbc) {
    this.jdbc = jdbc;
  }

  public Optional<Vadi> bul(UUID id) {
    return jdbc.query("SELECT * FROM vadi WHERE id = :id", Map.of("id", id), ESLEYICI).stream()
        .findFirst();
  }

  /** Vadiyi satir kilidiyle okur. Revizyon artirmadan once cagrilir. */
  public Optional<Vadi> bulKilitli(UUID id) {
    return jdbc
        .query("SELECT * FROM vadi WHERE id = :id FOR UPDATE", Map.of("id", id), ESLEYICI)
        .stream()
        .findFirst();
  }

  public Optional<Vadi> oyuncununVadisi(UUID oyuncuId) {
    return jdbc
        .query(
            "SELECT v.* FROM vadi v JOIN oyuncu_vadi ov ON ov.vadi_id = v.id WHERE ov.oyuncu_id = :o",
            Map.of("o", oyuncuId),
            ESLEYICI)
        .stream()
        .findFirst();
  }

  public Vadi olustur(UUID kuranId, long tohum, Instant simdi) {
    UUID id = UUID.randomUUID();
    jdbc.update(
        "INSERT INTO vadi (id, kuran_id, tohum, dunya_seviyesi, tek_imza_modu, olusturuldu) "
            + "VALUES (:id, :k, :t, 1, true, :s)",
        new MapSqlParameterSource()
            .addValue("id", id)
            .addValue("k", kuranId)
            .addValue("t", tohum)
            .addValue("s", Timestamp.from(simdi)));
    uyeYap(id, kuranId, simdi);
    return bul(id).orElseThrow();
  }

  public void uyeYap(UUID vadiId, UUID oyuncuId, Instant simdi) {
    jdbc.update(
        "INSERT INTO oyuncu_vadi (oyuncu_id, vadi_id, katildi_at) VALUES (:o, :v, :s) "
            + "ON CONFLICT (oyuncu_id) DO UPDATE SET vadi_id = :v, katildi_at = :s",
        new MapSqlParameterSource()
            .addValue("o", oyuncuId)
            .addValue("v", vadiId)
            .addValue("s", Timestamp.from(simdi)));
  }

  public void partnerAta(UUID vadiId, UUID partnerId) {
    jdbc.update(
        "UPDATE vadi SET partner_id = :p, tek_imza_modu = false WHERE id = :v",
        Map.of("v", vadiId, "p", partnerId));
  }

  public void dunyaSeviyesiYaz(UUID vadiId, int seviye) {
    jdbc.update("UPDATE vadi SET dunya_seviyesi = :s WHERE id = :v", Map.of("v", vadiId, "s", seviye));
  }

  public void tekImzaModuYaz(UUID vadiId, boolean tekImza) {
    jdbc.update(
        "UPDATE vadi SET tek_imza_modu = :t WHERE id = :v", Map.of("v", vadiId, "t", tekImza));
  }

  public void partnerSonGorulmeYaz(UUID vadiId, Instant simdi) {
    jdbc.update(
        "UPDATE vadi SET partner_son_gorulme = :s WHERE id = :v",
        new MapSqlParameterSource().addValue("v", vadiId).addValue("s", Timestamp.from(simdi)));
  }

  /**
   * Revizyonu bir artirir ve yeni degeri dondurur.
   * Tek bir UPDATE ... RETURNING: iki es zamanli eylem ayni revizyonu alamaz.
   */
  public long revizyonArtir(UUID vadiId) {
    Long yeni =
        jdbc.queryForObject(
            "UPDATE vadi SET revision = revision + 1 WHERE id = :v RETURNING revision",
            Map.of("v", vadiId),
            Long.class);
    if (yeni == null) throw new IllegalStateException("Vadi bulunamadi: " + vadiId);
    return yeni;
  }

  // ------------------------------------------------------------------ davet kodu

  public void davetKoduYaz(String kod, UUID vadiId, UUID uretenId, Instant gecerliBit, Instant simdi) {
    jdbc.update(
        "INSERT INTO davet_kodu (kod, vadi_id, ureten_id, gecerli_bit, olusturuldu) "
            + "VALUES (:k, :v, :u, :g, :s)",
        new MapSqlParameterSource()
            .addValue("k", kod)
            .addValue("v", vadiId)
            .addValue("u", uretenId)
            .addValue("g", Timestamp.from(gecerliBit))
            .addValue("s", Timestamp.from(simdi)));
  }

  public record DavetKaydi(String kod, UUID vadiId, UUID uretenId, Instant gecerliBit, Instant kullanildiAt) {}

  public Optional<DavetKaydi> davetBul(String kod) {
    return jdbc
        .query(
            "SELECT * FROM davet_kodu WHERE kod = :k FOR UPDATE",
            Map.of("k", kod),
            (rs, i) ->
                new DavetKaydi(
                    rs.getString("kod"),
                    rs.getObject("vadi_id", UUID.class),
                    rs.getObject("ureten_id", UUID.class),
                    zaman(rs, "gecerli_bit"),
                    zaman(rs, "kullanildi_at")))
        .stream()
        .findFirst();
  }

  public void davetKullanildi(String kod, Instant simdi) {
    jdbc.update(
        "UPDATE davet_kodu SET kullanildi_at = :s WHERE kod = :k",
        new MapSqlParameterSource().addValue("k", kod).addValue("s", Timestamp.from(simdi)));
  }

  /** Ayni vadinin bekleyen eski davetlerini iptal eder: tek gecerli kod olsun. */
  public void eskiDavetleriIptal(UUID vadiId, Instant simdi) {
    jdbc.update(
        "UPDATE davet_kodu SET gecerli_bit = :s WHERE vadi_id = :v AND kullanildi_at IS NULL",
        new MapSqlParameterSource().addValue("v", vadiId).addValue("s", Timestamp.from(simdi)));
  }
}
