package tr.amorland;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.ConfigurationPropertiesScan;

import java.util.Locale;
import java.util.TimeZone;

/**
 * Amorland oyun sunucusu.
 *
 * <p>Mimari kural 5: <b>tum zaman hesaplari sunucu saatiyle ve UTC</b> yapilir.
 * JVM varsayilan saat dilimini burada sabitliyoruz ki makine ayari degisse bile
 * tren seferi / buyume hesaplari kaymasin.
 *
 * <p>Ayrica varsayilan Locale ROOT'a sabitlenir: gelistirme makinesi tr_TR
 * locale'inde ve Java'da {@code "ID".toLowerCase()} Turkce locale'de {@code "ıd"}
 * uretir. Esya ve bolge kimlikleri ASCII'dir, bu tuzaga dusmemeliyiz.
 */
@SpringBootApplication
@ConfigurationPropertiesScan
public class AmorlandUygulamasi {

  public static void main(String[] args) {
    TimeZone.setDefault(TimeZone.getTimeZone("UTC"));
    Locale.setDefault(Locale.ROOT);
    SpringApplication.run(AmorlandUygulamasi.class, args);
  }
}
