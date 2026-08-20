package tr.amorland;

import io.zonky.test.db.postgres.embedded.EmbeddedPostgres;
import org.junit.jupiter.api.Disabled;
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;

import java.io.IOException;

/**
 * <b>Docker olmadan</b> sunucuyu calistirmanin yolu.
 *
 * <p>Normal yol {@code docker compose up -d} + {@code ./mvnw spring-boot:run}'dur.
 * Ama Docker Desktop kapaliysa veya kurulu degilse bu sinif ayni sunucuyu
 * <b>gomulu PostgreSQL</b> ile 8080'de ayaga kaldirir ve Ctrl+C'ye kadar bekler.
 *
 * <p>Calistir:
 * <pre>
 * ./mvnw test -Dtest=YerelSunucu -DfailIfNoTests=false "-Djunit.jupiter.conditions.deactivate=*"
 * </pre>
 *
 * <p>⚠ Bu bir test degildir. {@link TestTemeli}'nden TUREMEZ — turerse sahte saat
 * devreye girer (urunler hic buyumez) ve her testten once tablolar bosaltilir.
 * Burada gercek saat ve kalici oturum gerekir.
 *
 * <p>⚠ Gomulu veritabani <b>gecicidir</b>: sunucu kapaninca veri gider.
 * Kalici test icin docker compose kullan.
 */
@SpringBootTest(
    webEnvironment = SpringBootTest.WebEnvironment.DEFINED_PORT,
    properties = {"server.port=8080"})
@ActiveProfiles("test")
@Disabled("Elle calistirilir; testlerle birlikte kosmaz.")
class YerelSunucu {

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
      } catch (IOException e) {
        throw new IllegalStateException("Gomulu Postgres baslatilamadi", e);
      }
    }
    return postgres.getJdbcUrl("postgres", "postgres");
  }

  @Test
  void calistir() throws InterruptedException {
    System.out.println();
    System.out.println("==================================================");
    System.out.println(" Amorland sunucusu calisiyor: http://localhost:8080");
    System.out.println(" Veritabani: gomulu PostgreSQL (GECICI - kapaninca silinir)");
    System.out.println(" Saglik ucu: http://localhost:8080/api/saglik");
    System.out.println(" Durdurmak icin Ctrl+C");
    System.out.println("==================================================");
    System.out.println();
    Thread.currentThread().join();
  }
}
