package tr.amorland.kimlik;

import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Repository;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.Instant;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;

/**
 * Oyuncu tablosuna erisim. JPA yok: sorgular acik, indeks kullanimi gorunur.
 */
@Repository
public class OyuncuDeposu {

  private static final String ALANLAR =
      "id, cihaz_id, baglanti_tipi, baglanti_ref, kurtarma_kodu_hash, ad, renk, seviye, xp, "
          + "dogum_yili, bagli_kabul_teklif, aktif_oturum, son_gorulme, olusturuldu";

  private static final RowMapper<Oyuncu> ESLEYICI =
      (ResultSet rs, int satir) ->
          new Oyuncu(
              rs.getObject("id", UUID.class),
              rs.getString("cihaz_id"),
              rs.getString("baglanti_tipi"),
              rs.getString("baglanti_ref"),
              rs.getString("kurtarma_kodu_hash"),
              rs.getString("ad"),
              rs.getInt("renk"),
              rs.getInt("seviye"),
              rs.getInt("xp"),
              (Integer) rs.getObject("dogum_yili"),
              rs.getInt("bagli_kabul_teklif"),
              rs.getObject("aktif_oturum", UUID.class),
              zaman(rs, "son_gorulme"),
              zaman(rs, "olusturuldu"));

  private static Instant zaman(ResultSet rs, String alan) throws SQLException {
    var ts = rs.getTimestamp(alan);
    return ts == null ? null : ts.toInstant();
  }

  private final NamedParameterJdbcTemplate jdbc;

  public OyuncuDeposu(NamedParameterJdbcTemplate jdbc) {
    this.jdbc = jdbc;
  }

  public Optional<Oyuncu> cihazIle(String cihazId) {
    return jdbc
        .query("SELECT " + ALANLAR + " FROM oyuncu WHERE cihaz_id = :c", Map.of("c", cihazId), ESLEYICI)
        .stream()
        .findFirst();
  }

  public Optional<Oyuncu> bul(UUID id) {
    return jdbc
        .query("SELECT " + ALANLAR + " FROM oyuncu WHERE id = :id", Map.of("id", id), ESLEYICI)
        .stream()
        .findFirst();
  }

  public List<Oyuncu> bul(List<UUID> idler) {
    if (idler.isEmpty()) return List.of();
    return jdbc.query(
        "SELECT " + ALANLAR + " FROM oyuncu WHERE id IN (:idler)", Map.of("idler", idler), ESLEYICI);
  }

  public Optional<Oyuncu> kurtarmaKoduIle(String kodHash) {
    return jdbc
        .query(
            "SELECT " + ALANLAR + " FROM oyuncu WHERE kurtarma_kodu_hash = :h",
            Map.of("h", kodHash),
            ESLEYICI)
        .stream()
        .findFirst();
  }

  public Oyuncu olustur(String cihazId, String ad, int renk, Instant simdi) {
    UUID id = UUID.randomUUID();
    jdbc.update(
        "INSERT INTO oyuncu (id, cihaz_id, ad, renk, seviye, xp, son_gorulme, olusturuldu) "
            + "VALUES (:id, :c, :ad, :renk, 1, 0, :simdi, :simdi)",
        new MapSqlParameterSource()
            .addValue("id", id)
            .addValue("c", cihazId)
            .addValue("ad", ad)
            .addValue("renk", renk)
            .addValue("simdi", java.sql.Timestamp.from(simdi)));
    return bul(id).orElseThrow();
  }

  public void adVeRenkGuncelle(UUID id, String ad, int renk) {
    jdbc.update(
        "UPDATE oyuncu SET ad = :ad, renk = :renk WHERE id = :id",
        Map.of("id", id, "ad", ad, "renk", renk));
  }

  public void dogumYiliAyarla(UUID id, int dogumYili) {
    jdbc.update(
        "UPDATE oyuncu SET dogum_yili = :y WHERE id = :id", Map.of("id", id, "y", dogumYili));
  }

  /** "Son baglanan kazanir" (C.5): yeni oturum kimligi yazilir, eskisi gecersizlesir. */
  public void oturumAyarla(UUID id, UUID oturumId, Instant simdi) {
    jdbc.update(
        "UPDATE oyuncu SET aktif_oturum = :o, son_gorulme = :s WHERE id = :id",
        new MapSqlParameterSource()
            .addValue("id", id)
            .addValue("o", oturumId)
            .addValue("s", java.sql.Timestamp.from(simdi)));
  }

  public void sonGorulmeGuncelle(UUID id, Instant simdi) {
    jdbc.update(
        "UPDATE oyuncu SET son_gorulme = :s WHERE id = :id",
        new MapSqlParameterSource().addValue("id", id).addValue("s", java.sql.Timestamp.from(simdi)));
  }

  public void hesapBagla(UUID id, String tip, String ref) {
    jdbc.update(
        "UPDATE oyuncu SET baglanti_tipi = :t, baglanti_ref = :r WHERE id = :id",
        Map.of("id", id, "t", tip, "r", ref));
  }

  public void kurtarmaKoduAyarla(UUID id, String kodHash) {
    jdbc.update(
        "UPDATE oyuncu SET kurtarma_kodu_hash = :h WHERE id = :id", Map.of("id", id, "h", kodHash));
  }

  /** Kurtarmada cihaz kimligi yeni telefona tasinir. */
  public void cihazDevret(UUID id, String yeniCihazId) {
    jdbc.update(
        "UPDATE oyuncu SET cihaz_id = :c WHERE id = :id", Map.of("id", id, "c", yeniCihazId));
  }

  public void seviyeVeXpYaz(UUID id, int seviye, int xp) {
    jdbc.update(
        "UPDATE oyuncu SET seviye = :s, xp = :x WHERE id = :id",
        Map.of("id", id, "s", seviye, "x", xp));
  }

  public void baglamaTeklifiArtir(UUID id) {
    jdbc.update(
        "UPDATE oyuncu SET bagli_kabul_teklif = bagli_kabul_teklif + 1 WHERE id = :id",
        Map.of("id", id));
  }
}
