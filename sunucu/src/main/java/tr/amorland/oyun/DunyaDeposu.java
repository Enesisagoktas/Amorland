package tr.amorland.oyun;

import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Repository;
import tr.amorland.oyun.DunyaModeli.*;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Timestamp;
import java.time.Instant;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;

/**
 * Dunya durumu tablolari (tarla, envanter, depo, dugum, alet, kasa, kova, isaret).
 *
 * <p>Butun yazma islemleri {@code UPSERT} bicimindedir: ayni eylem iki kez uygulanirsa
 * (tekrar gonderim) satir cogalmaz. Idempotency'nin ikinci savunma hatti budur;
 * birincisi {@code eylem_kaydi} tablosudur.
 */
@Repository
public class DunyaDeposu {

  private final NamedParameterJdbcTemplate jdbc;

  public DunyaDeposu(NamedParameterJdbcTemplate jdbc) {
    this.jdbc = jdbc;
  }

  private static Instant zaman(ResultSet rs, String alan) throws SQLException {
    Timestamp ts = rs.getTimestamp(alan);
    return ts == null ? null : ts.toInstant();
  }

  private static Timestamp ts(Instant i) {
    return i == null ? null : Timestamp.from(i);
  }

  // ------------------------------------------------------------------ tarla

  private static final RowMapper<Karo> KARO =
      (rs, i) ->
          new Karo(
              rs.getObject("parsel_sahibi", UUID.class),
              rs.getShort("x"),
              rs.getShort("y"),
              rs.getString("urun_id"),
              zaman(rs, "ekildi_at"),
              zaman(rs, "sulandi_at"),
              rs.getShort("hasat_sayaci"),
              zaman(rs, "son_hasat_at"),
              rs.getBoolean("gubreli"));

  public List<Karo> karolar(UUID vadiId) {
    return jdbc.query(
        "SELECT * FROM tarla_karo WHERE vadi_id = :v ORDER BY parsel_sahibi, y, x",
        Map.of("v", vadiId),
        KARO);
  }

  public Optional<Karo> karo(UUID vadiId, UUID parsel, int x, int y) {
    return jdbc
        .query(
            "SELECT * FROM tarla_karo WHERE vadi_id = :v AND parsel_sahibi = :p AND x = :x AND y = :y",
            Map.of("v", vadiId, "p", parsel, "x", x, "y", y),
            KARO)
        .stream()
        .findFirst();
  }

  /** Karoyu satir kilidiyle okur. Ayni karoya es zamanli iki eylem gelirse siraya girer. */
  public Optional<Karo> karoKilitli(UUID vadiId, UUID parsel, int x, int y) {
    return jdbc
        .query(
            "SELECT * FROM tarla_karo WHERE vadi_id = :v AND parsel_sahibi = :p AND x = :x AND y = :y "
                + "FOR UPDATE",
            Map.of("v", vadiId, "p", parsel, "x", x, "y", y),
            KARO)
        .stream()
        .findFirst();
  }

  public void karoOlustur(UUID vadiId, UUID parsel, int x, int y) {
    jdbc.update(
        "INSERT INTO tarla_karo (vadi_id, parsel_sahibi, x, y) VALUES (:v, :p, :x, :y) "
            + "ON CONFLICT DO NOTHING",
        Map.of("v", vadiId, "p", parsel, "x", x, "y", y));
  }

  public void karoYaz(UUID vadiId, Karo k) {
    jdbc.update(
        "UPDATE tarla_karo SET urun_id = :u, ekildi_at = :e, sulandi_at = :s, "
            + "hasat_sayaci = :hs, son_hasat_at = :sh, gubreli = :g "
            + "WHERE vadi_id = :v AND parsel_sahibi = :p AND x = :x AND y = :y",
        new MapSqlParameterSource()
            .addValue("v", vadiId)
            .addValue("p", k.parselSahibi())
            .addValue("x", k.x())
            .addValue("y", k.y())
            .addValue("u", k.urunId())
            .addValue("e", ts(k.ekildiAt()))
            .addValue("s", ts(k.sulandiAt()))
            .addValue("hs", k.hasatSayaci())
            .addValue("sh", ts(k.sonHasatAt()))
            .addValue("g", k.gubreli()));
  }

  public int karoSayisi(UUID vadiId, UUID parsel) {
    Integer n =
        jdbc.queryForObject(
            "SELECT count(*) FROM tarla_karo WHERE vadi_id = :v AND parsel_sahibi = :p",
            Map.of("v", vadiId, "p", parsel),
            Integer.class);
    return n == null ? 0 : n;
  }

  // ------------------------------------------------------------------ envanter

  private static final RowMapper<EnvanterSatiri> ENVANTER =
      (rs, i) ->
          new EnvanterSatiri(
              rs.getObject("sahip", UUID.class), rs.getString("esya_id"), rs.getInt("adet"));

  public List<EnvanterSatiri> envanter(UUID vadiId) {
    return jdbc.query(
        "SELECT * FROM envanter WHERE vadi_id = :v ORDER BY sahip, esya_id",
        Map.of("v", vadiId),
        ENVANTER);
  }

  public int envanterAdet(UUID vadiId, UUID sahip, String esyaId) {
    Integer n =
        jdbc.queryForObject(
            "SELECT coalesce(sum(adet), 0) FROM envanter "
                + "WHERE vadi_id = :v AND sahip = :s AND esya_id = :e",
            Map.of("v", vadiId, "s", sahip, "e", esyaId),
            Integer.class);
    return n == null ? 0 : n;
  }

  /**
   * Kullanilan slot sayisi. Canta kisiti esya CESIDIDIR, miktar degil (B.10) — ama
   * yigin 99'da dolar, 150 bugday iki slot tutar. O yuzden {@code ceil(adet / yigin)} toplanir.
   */
  public int envanterSlotSayisi(UUID vadiId, UUID sahip, int yiginBoyutu) {
    Integer n =
        jdbc.queryForObject(
            "SELECT coalesce(sum(ceil(adet::numeric / :y)), 0) FROM envanter "
                + "WHERE vadi_id = :v AND sahip = :s",
            Map.of("v", vadiId, "s", sahip, "y", yiginBoyutu),
            Integer.class);
    return n == null ? 0 : n;
  }

  /**
   * Envantere ekler veya cikarir. Sonuc 0 ise satir silinir; negatif olamaz.
   *
   * @return islemden sonraki adet
   */
  public int envanterDegistir(UUID vadiId, UUID sahip, String esyaId, int delta) {
    if (delta == 0) return envanterAdet(vadiId, sahip, esyaId);
    if (delta > 0) {
      jdbc.update(
          "INSERT INTO envanter (vadi_id, sahip, esya_id, adet) VALUES (:v, :s, :e, :d) "
              + "ON CONFLICT (vadi_id, sahip, esya_id) DO UPDATE SET adet = envanter.adet + :d",
          Map.of("v", vadiId, "s", sahip, "e", esyaId, "d", delta));
    } else {
      // Once sifira dusecek satiri SIL, sonra kalani guncelle.
      // Ters sirada CHECK (adet > 0) kisiti UPDATE aninda patlar.
      jdbc.update(
          "DELETE FROM envanter WHERE vadi_id = :v AND sahip = :s AND esya_id = :e AND adet + :d <= 0",
          Map.of("v", vadiId, "s", sahip, "e", esyaId, "d", delta));
      jdbc.update(
          "UPDATE envanter SET adet = adet + :d "
              + "WHERE vadi_id = :v AND sahip = :s AND esya_id = :e AND adet + :d > 0",
          Map.of("v", vadiId, "s", sahip, "e", esyaId, "d", delta));
    }
    return envanterAdet(vadiId, sahip, esyaId);
  }

  // ------------------------------------------------------------------ depo

  private static final RowMapper<DepoSatiri> DEPO =
      (rs, i) ->
          new DepoSatiri(
              rs.getString("esya_id"),
              rs.getInt("adet"),
              rs.getObject("koyan", UUID.class),
              rs.getBoolean("kilitli"),
              rs.getObject("kilitleyen", UUID.class));

  public List<DepoSatiri> depo(UUID vadiId) {
    return jdbc.query(
        "SELECT * FROM depo WHERE vadi_id = :v ORDER BY esya_id", Map.of("v", vadiId), DEPO);
  }

  public Optional<DepoSatiri> depoSatiri(UUID vadiId, String esyaId) {
    return jdbc
        .query(
            "SELECT * FROM depo WHERE vadi_id = :v AND esya_id = :e",
            Map.of("v", vadiId, "e", esyaId),
            DEPO)
        .stream()
        .findFirst();
  }

  /** Depo TOPLAM MIKTAR kisitidir, slot degil (B.10). */
  public int depoDolu(UUID vadiId) {
    Integer n =
        jdbc.queryForObject(
            "SELECT coalesce(sum(adet), 0) FROM depo WHERE vadi_id = :v",
            Map.of("v", vadiId),
            Integer.class);
    return n == null ? 0 : n;
  }

  public int depoKilitliSayisi(UUID vadiId, UUID oyuncuId) {
    Integer n =
        jdbc.queryForObject(
            "SELECT count(*) FROM depo WHERE vadi_id = :v AND kilitli AND kilitleyen = :o",
            Map.of("v", vadiId, "o", oyuncuId),
            Integer.class);
    return n == null ? 0 : n;
  }

  public int depoDegistir(UUID vadiId, String esyaId, int delta, UUID koyan) {
    if (delta > 0) {
      jdbc.update(
          "INSERT INTO depo (vadi_id, esya_id, adet, koyan) VALUES (:v, :e, :d, :k) "
              + "ON CONFLICT (vadi_id, esya_id) DO UPDATE SET adet = depo.adet + :d, koyan = :k",
          new MapSqlParameterSource()
              .addValue("v", vadiId)
              .addValue("e", esyaId)
              .addValue("d", delta)
              .addValue("k", koyan));
    } else if (delta < 0) {
      jdbc.update(
          "DELETE FROM depo WHERE vadi_id = :v AND esya_id = :e AND adet + :d <= 0",
          Map.of("v", vadiId, "e", esyaId, "d", delta));
      jdbc.update(
          "UPDATE depo SET adet = adet + :d WHERE vadi_id = :v AND esya_id = :e AND adet + :d > 0",
          Map.of("v", vadiId, "e", esyaId, "d", delta));
    }
    return depoSatiri(vadiId, esyaId).map(DepoSatiri::adet).orElse(0);
  }

  /** Es zamanli almada "sunucuya ilk ulasan kazanir" (C.5) icin satir kilidi. */
  public Optional<DepoSatiri> depoSatiriKilitli(UUID vadiId, String esyaId) {
    return jdbc
        .query(
            "SELECT * FROM depo WHERE vadi_id = :v AND esya_id = :e FOR UPDATE",
            Map.of("v", vadiId, "e", esyaId),
            DEPO)
        .stream()
        .findFirst();
  }

  public void depoKilit(UUID vadiId, String esyaId, boolean kilitli, UUID kilitleyen) {
    jdbc.update(
        "UPDATE depo SET kilitli = :k, kilitleyen = :kl WHERE vadi_id = :v AND esya_id = :e",
        new MapSqlParameterSource()
            .addValue("v", vadiId)
            .addValue("e", esyaId)
            .addValue("k", kilitli)
            .addValue("kl", kilitli ? kilitleyen : null));
  }

  // ------------------------------------------------------------------ dugum

  private static final RowMapper<DugumSatiri> DUGUM_SATIRI =
      (rs, i) ->
          new DugumSatiri(
              rs.getString("bolge"),
              rs.getShort("x"),
              rs.getShort("y"),
              rs.getString("tur"),
              rs.getString("sinif"),
              rs.getShort("saglik"),
              rs.getShort("maks_saglik"),
              zaman(rs, "kirildi_at"),
              rs.getString("katki"));

  /** Ham dugum satiri; katki JSON metni olarak tasinir. */
  public record DugumSatiri(
      String bolge,
      int x,
      int y,
      String tur,
      String sinif,
      int saglik,
      int maksSaglik,
      Instant kirildiAt,
      String katkiJson) {}

  public List<DugumSatiri> dugumler(UUID vadiId) {
    return jdbc.query(
        "SELECT * FROM dugum WHERE vadi_id = :v ORDER BY bolge, y, x", Map.of("v", vadiId), DUGUM_SATIRI);
  }

  public Optional<DugumSatiri> dugumKilitli(UUID vadiId, String bolge, int x, int y) {
    return jdbc
        .query(
            "SELECT * FROM dugum WHERE vadi_id = :v AND bolge = :b AND x = :x AND y = :y FOR UPDATE",
            Map.of("v", vadiId, "b", bolge, "x", x, "y", y),
            DUGUM_SATIRI)
        .stream()
        .findFirst();
  }

  public void dugumOlustur(
      UUID vadiId, String bolge, int x, int y, String tur, String sinif, int maksSaglik) {
    jdbc.update(
        "INSERT INTO dugum (vadi_id, bolge, x, y, tur, sinif, saglik, maks_saglik) "
            + "VALUES (:v, :b, :x, :y, :t, :s, :m, :m) ON CONFLICT DO NOTHING",
        new MapSqlParameterSource()
            .addValue("v", vadiId)
            .addValue("b", bolge)
            .addValue("x", x)
            .addValue("y", y)
            .addValue("t", tur)
            .addValue("s", sinif)
            .addValue("m", maksSaglik));
  }

  public void dugumYaz(
      UUID vadiId, String bolge, int x, int y, int saglik, int maksSaglik,
      Instant kirildiAt, String katkiJson) {
    jdbc.update(
        "UPDATE dugum SET saglik = :sg, maks_saglik = :ms, kirildi_at = :k, katki = CAST(:kt AS jsonb) "
            + "WHERE vadi_id = :v AND bolge = :b AND x = :x AND y = :y",
        new MapSqlParameterSource()
            .addValue("v", vadiId)
            .addValue("b", bolge)
            .addValue("x", x)
            .addValue("y", y)
            .addValue("sg", saglik)
            .addValue("ms", maksSaglik)
            .addValue("k", ts(kirildiAt))
            .addValue("kt", katkiJson));
  }

  // ------------------------------------------------------------------ alet

  public record AletSatiri(UUID sahip, String aletId, int dayaniklilik) {}

  private static final RowMapper<AletSatiri> ALET =
      (rs, i) ->
          new AletSatiri(
              rs.getObject("sahip", UUID.class), rs.getString("alet_id"), rs.getShort("dayaniklilik"));

  public List<AletSatiri> aletler(UUID vadiId) {
    return jdbc.query(
        "SELECT * FROM alet WHERE vadi_id = :v ORDER BY sahip, alet_id", Map.of("v", vadiId), ALET);
  }

  public List<AletSatiri> aletler(UUID vadiId, UUID sahip) {
    return jdbc.query(
        "SELECT * FROM alet WHERE vadi_id = :v AND sahip = :s ORDER BY alet_id",
        Map.of("v", vadiId, "s", sahip),
        ALET);
  }

  public void aletYaz(UUID vadiId, UUID sahip, String aletId, int dayaniklilik) {
    jdbc.update(
        "INSERT INTO alet (vadi_id, sahip, alet_id, dayaniklilik) VALUES (:v, :s, :a, :d) "
            + "ON CONFLICT (vadi_id, sahip, alet_id) DO UPDATE SET dayaniklilik = :d",
        Map.of("v", vadiId, "s", sahip, "a", aletId, "d", dayaniklilik));
  }

  public void aletSil(UUID vadiId, UUID sahip, String aletId) {
    jdbc.update(
        "DELETE FROM alet WHERE vadi_id = :v AND sahip = :s AND alet_id = :a",
        Map.of("v", vadiId, "s", sahip, "a", aletId));
  }

  // ------------------------------------------------------------------ kasa ve kova

  public long altin(UUID vadiId, UUID oyuncuId) {
    Long v =
        jdbc.queryForObject(
            "SELECT coalesce(max(altin), 0) FROM kasa WHERE vadi_id = :v AND oyuncu_id = :o",
            Map.of("v", vadiId, "o", oyuncuId),
            Long.class);
    return v == null ? 0 : v;
  }

  public long altinDegistir(UUID vadiId, UUID oyuncuId, long delta) {
    jdbc.update(
        "INSERT INTO kasa (vadi_id, oyuncu_id, altin) VALUES (:v, :o, GREATEST(:d, 0)) "
            + "ON CONFLICT (vadi_id, oyuncu_id) DO UPDATE SET altin = kasa.altin + :d",
        Map.of("v", vadiId, "o", oyuncuId, "d", delta));
    return altin(vadiId, oyuncuId);
  }

  public int kovaKalan(UUID vadiId, UUID oyuncuId) {
    Integer v =
        jdbc.queryForObject(
            "SELECT coalesce(max(kalan_karo), 0) FROM kova_durumu WHERE vadi_id = :v AND oyuncu_id = :o",
            Map.of("v", vadiId, "o", oyuncuId),
            Integer.class);
    return v == null ? 0 : v;
  }

  public void kovaYaz(UUID vadiId, UUID oyuncuId, int kalan) {
    jdbc.update(
        "INSERT INTO kova_durumu (vadi_id, oyuncu_id, kalan_karo) VALUES (:v, :o, :k) "
            + "ON CONFLICT (vadi_id, oyuncu_id) DO UPDATE SET kalan_karo = :k",
        Map.of("v", vadiId, "o", oyuncuId, "k", kalan));
  }

  // ------------------------------------------------------------------ tren

  public record TrenKarsilama(int kart, UUID oyuncuId) {}

  /**
   * Siparis karsilamayi ATOMIK yazar: birincil anahtar (vadi, sefer, kart)
   * oldugu icin ayni karta ikinci INSERT 0 satir doner — C.5 "ilk ulasan
   * kazanir" kurali veritabaninda zorlanir, yaris kosulu kalmaz.
   */
  public boolean trenKarsilamaYaz(
      UUID vadiId, long seferNo, int kart, UUID oyuncuId, java.time.Instant simdi) {
    int satir =
        jdbc.update(
            "INSERT INTO tren_karsilama (vadi_id, sefer_no, kart, oyuncu_id, karsilandi) "
                + "VALUES (:v, :s, :k, :o, :t) ON CONFLICT (vadi_id, sefer_no, kart) DO NOTHING",
            new MapSqlParameterSource()
                .addValue("v", vadiId)
                .addValue("s", seferNo)
                .addValue("k", kart)
                .addValue("o", oyuncuId)
                .addValue("t", java.sql.Timestamp.from(simdi)));
    return satir > 0;
  }

  public List<TrenKarsilama> trenKarsilananlar(UUID vadiId, long seferNo) {
    return jdbc.query(
        "SELECT kart, oyuncu_id FROM tren_karsilama WHERE vadi_id = :v AND sefer_no = :s",
        Map.of("v", vadiId, "s", seferNo),
        (rs, i) -> new TrenKarsilama(rs.getInt("kart"), rs.getObject("oyuncu_id", UUID.class)));
  }

  // ------------------------------------------------------------------ isaretler

  public List<String> isaretler(UUID vadiId, UUID oyuncuId) {
    return jdbc.queryForList(
        "SELECT isaret FROM ilerleme_isareti WHERE vadi_id = :v AND oyuncu_id = :o",
        Map.of("v", vadiId, "o", oyuncuId),
        String.class);
  }

  /**
   * Isareti kaydeder.
   *
   * @return true ise isaret ILK KEZ konuldu (tek seferlik odul burada verilir)
   */
  public boolean isaretKoy(UUID vadiId, UUID oyuncuId, String isaret, Instant simdi) {
    int n =
        jdbc.update(
            "INSERT INTO ilerleme_isareti (vadi_id, oyuncu_id, isaret, isaret_at) "
                + "VALUES (:v, :o, :i, :s) ON CONFLICT DO NOTHING",
            new MapSqlParameterSource()
                .addValue("v", vadiId)
                .addValue("o", oyuncuId)
                .addValue("i", isaret)
                .addValue("s", ts(simdi)));
    return n > 0;
  }

  // ------------------------------------------------------------------ kusluk

  public Optional<UUID> kuslukBaslatan(UUID vadiId) {
    return jdbc
        .query(
            "SELECT baslatan_id FROM kusluk WHERE vadi_id = :v",
            Map.of("v", vadiId),
            (rs, i) -> rs.getObject("baslatan_id", UUID.class))
        .stream()
        .findFirst();
  }

  public void kuslukBaslat(UUID vadiId, UUID baslatan, Instant simdi) {
    jdbc.update(
        "INSERT INTO kusluk (vadi_id, baslatan_id, baslangic) VALUES (:v, :b, :s) "
            + "ON CONFLICT (vadi_id) DO UPDATE SET baslatan_id = :b, baslangic = :s",
        new MapSqlParameterSource().addValue("v", vadiId).addValue("b", baslatan).addValue("s", ts(simdi)));
  }

  public void kuslukBitir(UUID vadiId) {
    jdbc.update("DELETE FROM kusluk WHERE vadi_id = :v", Map.of("v", vadiId));
  }

  /** 48 saat sonra kendiliginden duser (B.8). Okuma aninda hesaplanir, cron yok. */
  public void kuslukSuresiDoldursa(UUID vadiId, Instant simdi, long otomatikDusmeSn) {
    jdbc.update(
        "DELETE FROM kusluk WHERE vadi_id = :v AND baslangic < :esik",
        new MapSqlParameterSource()
            .addValue("v", vadiId)
            .addValue("esik", ts(simdi.minusSeconds(otomatikDusmeSn))));
  }

  // ------------------------------------------------------------------ toplu okuma yardimcisi

  public Map<String, Integer> envanterHaritasi(UUID vadiId, UUID sahip) {
    Map<String, Integer> harita = new HashMap<>();
    jdbc.query(
        "SELECT esya_id, adet FROM envanter WHERE vadi_id = :v AND sahip = :s",
        Map.of("v", vadiId, "s", sahip),
        rs -> {
          harita.put(rs.getString("esya_id"), rs.getInt("adet"));
        });
    return harita;
  }
}
