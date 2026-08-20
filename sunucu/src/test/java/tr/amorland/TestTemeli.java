package tr.amorland;

import io.zonky.test.db.postgres.embedded.EmbeddedPostgres;
import org.junit.jupiter.api.BeforeEach;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Primary;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.test.context.ContextConfiguration;
import tr.amorland.ortak.Saat;

import java.io.IOException;
import java.time.Instant;

/**
 * Butun tumlestirme testlerinin temeli.
 *
 * <p>Gercek PostgreSQL kullanir ama <b>Docker gerektirmez</b>: zonky gomulu Postgres
 * ikili dosyalari indirir ve gecici bir ornek baslatir. Boylece jsonb, {@code FOR UPDATE}
 * ve {@code ON CONFLICT} gibi Postgres'e ozgu davranislar gercekten test edilir —
 * H2 ile bunlarin hicbiri dogrulanamazdi.
 *
 * <p>Saat sahtelenir: buyume ve yenilenme testleri gercek zamani beklemez, ileri sarar.
 */
@SpringBootTest
@ActiveProfiles("test")
@ContextConfiguration(classes = {AmorlandUygulamasi.class, TestTemeli.TestAyarlari.class})
public abstract class TestTemeli {

  protected static final Instant BASLANGIC = Instant.parse("2026-08-20T09:00:00Z");

  private static EmbeddedPostgres postgres;

  @DynamicPropertySource
  static void veritabani(DynamicPropertyRegistry kayit) {
    kayit.add("spring.datasource.url", () -> jdbcUrl());
    kayit.add("spring.datasource.username", () -> "postgres");
    kayit.add("spring.datasource.password", () -> "postgres");
  }

  private static synchronized String jdbcUrl() {
    if (postgres == null) {
      try {
        postgres = EmbeddedPostgres.builder().start();
        Runtime.getRuntime()
            .addShutdownHook(
                new Thread(
                    () -> {
                      try {
                        postgres.close();
                      } catch (IOException yoksay) {
                        // kapanista onemli degil
                      }
                    }));
      } catch (IOException e) {
        throw new IllegalStateException("Gomulu Postgres baslatilamadi", e);
      }
    }
    return postgres.getJdbcUrl("postgres", "postgres");
  }

  @TestConfiguration
  static class TestAyarlari {
    @Bean
    @Primary
    public Saat testSaati() {
      return new Saat.Sahte(BASLANGIC);
    }
  }

  @Autowired protected Saat saat;
  @Autowired protected JdbcTemplate jdbc;

  protected Saat.Sahte sahteSaat() {
    return (Saat.Sahte) saat;
  }

  /** Her testten once tablolari bosaltir ve saati basa alir. Sema Flyway'den gelir. */
  @BeforeEach
  void temizle() {
    sahteSaat().ayarla(BASLANGIC);
    jdbc.execute(
        "TRUNCATE oyuncu, vadi, oyuncu_vadi, davet_kodu, yenileme_jetonu, tarla_karo, dugum, "
            + "envanter, depo, alet, hayvan, ortak_proje, ilerleme_isareti, kasa, kova_durumu, "
            + "eylem_kaydi, revizyon_kaydi, reklam_jetonu, mesaj_kuyrugu, engelleme, "
            + "gunluk_sandik, kusluk RESTART IDENTITY CASCADE");
  }
}
