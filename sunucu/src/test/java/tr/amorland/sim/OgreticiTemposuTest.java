package tr.amorland.sim;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import tr.amorland.TestTemeli;
import tr.amorland.denge.DengeServisi;
import tr.amorland.kimlik.KimlikServisi;
import tr.amorland.kimlik.OyuncuDeposu;
import tr.amorland.ortak.OyunHatasi;
import tr.amorland.oyun.DunyaDeposu;
import tr.amorland.oyun.Niyet;
import tr.amorland.oyun.OyunServisi;
import tr.amorland.vadi.Vadi;
import tr.amorland.vadi.VadiServisi;

import java.time.Duration;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.*;

/**
 * A.6 dogrulama kurali: "Bir sayiyi veya kurali degistirdiginde, etkisini OLC. Tahmin etme."
 *
 * <p>Bu test B.18'deki ogretici akisini gercek sunucu kurallariyla yurutur ve
 * hangi dakikada hangi seviyeye gelindigini <b>olcerek</b> raporlar. Tasarim
 * hedefleriyle karsilastirir; sapma varsa ciktiya yazar.
 *
 * <p>Ciktiyi gormek icin: {@code ./mvnw test -Dtest=OgreticiTemposuTest}
 */
class OgreticiTemposuTest extends TestTemeli {

  @Autowired KimlikServisi kimlik;
  @Autowired VadiServisi vadiServisi;
  @Autowired OyunServisi oyun;
  @Autowired DunyaDeposu dunya;
  @Autowired OyuncuDeposu oyuncular;
  @Autowired DengeServisi denge;

  private final List<String> rapor = new ArrayList<>();
  /** Seviye -> o seviyeye ulasilan dakika. Sapmayi tahmin degil OLCUM soylesin. */
  private final Map<Integer, Double> seviyeAnlari = new LinkedHashMap<>();
  private int sonSeviye = 1;

  private void olc(String adim, UUID oyuncuId, double dakika) {
    var o = oyuncular.bul(oyuncuId).orElseThrow();
    while (sonSeviye < o.seviye()) {
      sonSeviye++;
      seviyeAnlari.put(sonSeviye, dakika);
    }
    rapor.add(
        String.format(
            "  %-38s dk %5.2f   Sv %d   XP %4d / %d",
            adim, dakika, o.seviye(), o.xp(), denge.seviyeEsigi(o.seviye())));
  }

  private void niyet(UUID o, UUID v, String tip, Niyet.Hedef h, Niyet.Ek e) {
    oyun.uygula(o, v, new Niyet(UUID.randomUUID(), tip, h, e));
  }

  @Test
  @DisplayName("Ogretici temposu olcumu: B.18 akisini gercek kurallarla yurut")
  void ogreticiTemposu() {
    UUID o = kimlik.misafirGiris("cihaz-1", "Ada", 0).oyuncuId();
    Vadi vadi = vadiServisi.kur(o);

    // Test kova zincirini kisaltiyor: ogreticide kuyu hazir kabul edilir.
    dunya.aletYaz(vadi.id(), o, "ahsap_kova", 0);
    dunya.kovaYaz(vadi.id(), o, 6);

    double dk = 0;
    rapor.add("");
    rapor.add("=== OGRETICI TEMPOSU (olculdu, tahmin degil) ===");
    olc("0:00 misafir hesap", o, dk);

    // 0:45 Eve gir, defterin 1. sayfasi
    dk = 0.75;
    niyet(o, vadi.id(), "bolgeye_gir", new Niyet.Hedef("ev", null, null, null), null);
    olc("0:45 ev + defter sayfa 1", o, dk);

    // 1:20 Tarlaya gir
    dk = 1.33;
    sahteSaat().ileriSar(Duration.ofSeconds(35));
    niyet(o, vadi.id(), "bolgeye_gir", new Niyet.Hedef("tarla", null, null, null), null);
    olc("1:20 tarlaya gir (+50 XP)", o, dk);

    // 1:30 Ilk ekim
    dk = 1.5;
    sahteSaat().ileriSar(Duration.ofSeconds(10));
    niyet(o, vadi.id(), "ek", new Niyet.Hedef("tarla", 0, 0, null), new Niyet.Ek("bugday", null, null, null));
    olc("1:30 ilk ekim (+25 XP)", o, dk);

    // 1:50 Ilk sulama
    dk = 1.83;
    sahteSaat().ileriSar(Duration.ofSeconds(20));
    niyet(o, vadi.id(), "sula", new Niyet.Hedef("tarla", 0, 0, null), null);
    olc("1:50 ilk sulama (+25 XP)", o, dk);

    // 2:20 Yabani kaynaklar
    dk = 2.33;
    sahteSaat().ileriSar(Duration.ofSeconds(30));
    for (var d : dunya.dugumler(vadi.id())) {
      try {
        niyet(o, vadi.id(), "topla", new Niyet.Hedef(d.bolge(), d.x(), d.y(), null), null);
      } catch (OyunHatasi yoksay) {
        // canta dolabilir
      }
    }
    olc("2:20 3 yabani kaynak (+3 XP)", o, dk);

    // 3:00 Ilk hasat (bugday 5 dk; sulama 1:50'de basladi -> 6:50)
    sahteSaat().ileriSar(Duration.ofMinutes(5));
    dk = 7.33;
    niyet(o, vadi.id(), "hasat", new Niyet.Hedef("tarla", 0, 0, null), null);
    olc("ilk hasat (+1 XP, defter sayfa 2)", o, dk);

    var sonrasi = oyuncular.bul(o).orElseThrow();

    rapor.add("");
    rapor.add("=== TASARIM HEDEFI ILE KARSILASTIRMA (B.18) ===");
    rapor.add("  Tasarim:  3:30 -> Sv 2 (Depo)    ~10:00 -> Sv 3 (Partner daveti)");
    rapor.add(
        String.format(
            "  Olculen:  ilk hasattan sonra Sv %d, XP %d", sonrasi.seviye(), sonrasi.xp()));

    int toplamKazanilan = toplamXp(sonrasi);
    rapor.add(String.format("  Toplam kazanilan XP: %d", toplamKazanilan));
    rapor.add("  Sv 2 esigi: " + denge.seviyeEsigi(1) + "   Sv 3 esigi: " + denge.seviyeEsigi(2));

    rapor.add("");
    rapor.add("=== BULGU ===");
    Double sv2 = seviyeAnlari.get(2);
    if (sv2 == null) {
      rapor.add("  ! Ogretici sonunda hala Sv 1. Tek seferlik oduller beklenenden dusuk.");
    } else {
      rapor.add(String.format("  Sv 2 dk %.2f'de geldi; tasarim 3:30 diyor (B.18).", sv2));
      if (sv2 < 3.0) {
        rapor.add("    SAPMA: Depo tasarlanandan ~1.5 dk ERKEN aciliyor.");
        rapor.add("    Sebep: 'ilk sulama' (+25) ile Sv2 esigi (100) tam doluyor.");
        rapor.add("    Etki KUCUK: ogreticinin 3:30 adimi zaten depoyu OGRETIYOR, aciyor degil.");
        rapor.add("    B.26 yasak 12 tek seferlik odulleri kismayi yasakliyor; dolayisiyla");
        rapor.add("    ayar gerekirse defter sayfa 1 tetikleyicisi (0:45 -> ilk hasat) kaydirilir,");
        rapor.add("    XP degeri DUSURULMEZ.");
      } else {
        rapor.add("    Tasarim hedefiyle uyumlu.");
      }
    }
    int kalanSv3 = denge.seviyeEsigi(2) - sonrasi.xp();
    rapor.add(
        String.format(
            "  Sv 3'e (partner daveti) kalan: %d XP. Bugday hasati basina 1 XP + defter/ilk-kez", 
            Math.max(0, kalanSv3)));
    rapor.add("    odulleri; tasarimin ~10:00 hedefiyle ayni buyukluk sirasinda.");

    System.out.println(String.join("\n", rapor));

    // Kirmizi cizgi: ogreticide oyuncu MUTLAKA ilerlemeli.
    assertTrue(sonrasi.seviye() >= 2, "Ogreticinin sonunda en az Sv 2 olmali (Depo acilmali)");
    assertTrue(
        toplamKazanilan >= 175,
        "Tek seferlik oduller Perde 1'in motoru (B.16); kisilirsa ogretici angaryaya doner");
  }

  private int toplamXp(tr.amorland.kimlik.Oyuncu o) {
    int toplam = o.xp();
    for (int s = 1; s < o.seviye(); s++) toplam += denge.seviyeEsigi(s);
    return toplam;
  }
}
