package tr.amorland.ortak;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.time.Clock;
import java.time.Instant;
import java.time.ZoneOffset;

/**
 * Sunucu saati (mimari kural 5).
 *
 * <p>Istemci saatine asla guvenilmez. Butun buyume, yenilenme ve tren hesaplari
 * bu tek kaynaktan okunur. Testlerde ileri sarabilmek icin {@link Clock} ile
 * sarmalanmistir; kodun hicbir yerinde {@code Instant.now()} dogrudan cagrilmaz.
 */
public interface Saat {

  Instant simdi();

  /** Uretim: gercek sistem saati, UTC. */
  @Configuration
  class Yapilandirma {
    @Bean
    public Saat saat() {
      Clock clock = Clock.system(ZoneOffset.UTC);
      return clock::instant;
    }
  }

  /** Test: elle ileri sarilabilir saat. */
  final class Sahte implements Saat {
    private Instant an;

    public Sahte(Instant baslangic) {
      this.an = baslangic;
    }

    @Override
    public Instant simdi() {
      return an;
    }

    public void ayarla(Instant yeni) {
      this.an = yeni;
    }

    public void ileriSar(java.time.Duration sure) {
      this.an = an.plus(sure);
    }
  }
}
