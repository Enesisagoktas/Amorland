package tr.amorland.denge;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import tr.amorland.denge.DengeModelleri.Urun;

import java.util.List;

import static org.junit.jupiter.api.Assertions.*;

/**
 * A.6 dogrulama kurali: denge dosyalari acilista denetlenir.
 * Bu test Spring baglami olmadan calisir, saniyeler surer.
 */
class DengeServisiTest {

  private static DengeServisi denge;

  @BeforeAll
  static void kur() {
    denge = new DengeServisi(new ObjectMapper());
    denge.yukle();
  }

  @Test
  @DisplayName("Denge dosyalari ic tutarli (10 denetim)")
  void tutarlilik() {
    var sonuc = denge.denetle();
    assertTrue(
        sonuc.hatalar().isEmpty(),
        () -> "Denge hatalari:\n  - " + String.join("\n  - ", sonuc.hatalar()));
  }

  @Test
  @DisplayName("Bilinen tek denge uyarisi: Sv 19->20 esigi 1 XP sapiyor")
  void bilinenUyari() {
    var uyarilar = denge.denetle().uyarilar();
    assertEquals(
        1, uyarilar.size(),
        () -> "Beklenmeyen uyari kumesi:\n  - " + String.join("\n  - ", uyarilar));
    assertTrue(uyarilar.get(0).contains("Seviye esigi 19 -> 20 = 2663"));
    // Tablo kendi icinde tutarli: esikler toplami tam 15476. Sapma sadece formul aciklamasinda.
    assertEquals(
        15476,
        denge.ilerlemeDosyasi().seviye().esikler().values().stream().mapToInt(Integer::intValue).sum());
  }

  @Test
  @DisplayName("16 urun yuklendi ve tohum kimlikleri turetildi")
  void urunler() {
    assertEquals(16, denge.tumUrunler().size());
    Urun bugday = denge.urun("bugday").orElseThrow();
    assertEquals("bugday_tohum", bugday.tohumEsyaId());
    assertTrue(bugday.tohumGeriVerirMi(), "Bugday baslangic urunu, tohum geri vermeli");
    assertEquals(bugday, denge.tohumdanUrun("bugday_tohum").orElseThrow());
  }

  @Test
  @DisplayName("Kapasite tablolari seviyeye gore dogru okunuyor")
  void kapasiteler() {
    assertEquals(8, denge.cantaSlot(1));
    assertEquals(8, denge.cantaSlot(3));
    assertEquals(12, denge.cantaSlot(4));
    assertEquals(20, denge.cantaSlot(20));

    assertEquals(0, denge.depoKapasitesi(1), "Sv1'de depo yok");
    assertEquals(100, denge.depoKapasitesi(2));
    assertEquals(460, denge.depoKapasitesi(20));

    assertEquals(4, denge.tarlaKaroSayisi(1));
    assertEquals(8, denge.tarlaKaroSayisi(2));
    assertEquals(24, denge.tarlaKaroSayisi(18));
  }

  @Test
  @DisplayName("Bolge XP: ev haric, digerleri 50")
  void bolgeXp() {
    assertEquals(0, denge.bolgeIlkAcmaXp("ev"), "Oyuncu evde BASLIYOR, ilk acma yok");
    assertEquals(50, denge.bolgeIlkAcmaXp("tarla"));
    assertEquals(50, denge.bolgeIlkAcmaXp("depo"));
    assertEquals(50, denge.bolgeIlkAcmaXp("gar"));
  }

  @Test
  @DisplayName("Buyume asamasi 0..3 arasinda kaliyor ve cron gerektirmiyor")
  void buyumeAsamasi() {
    long buyume = 300; // bugday
    assertEquals(0, denge.buyumeAsamasi(0, buyume));
    assertEquals(0, denge.buyumeAsamasi(74, buyume));
    assertEquals(1, denge.buyumeAsamasi(75, buyume));
    assertEquals(2, denge.buyumeAsamasi(150, buyume));
    assertEquals(3, denge.buyumeAsamasi(225, buyume));
    assertEquals(3, denge.buyumeAsamasi(300, buyume));
    assertEquals(3, denge.buyumeAsamasi(999_999, buyume), "Olgunlastiktan sonra asama artmaz");
  }

  @Test
  @DisplayName("Hasat XP formulu: max(1, round(buyumeSaati * 2))")
  void hasatXp() {
    assertEquals(1, denge.hasatXp(denge.urun("bugday").orElseThrow()));   // 5 dk
    assertEquals(1, denge.hasatXp(denge.urun("turp").orElseThrow()));     // 30 dk
    assertEquals(4, denge.hasatXp(denge.urun("misir").orElseThrow()));    // 2 sa
    assertEquals(12, denge.hasatXp(denge.urun("kabak").orElseThrow()));   // 6 sa
    assertEquals(24, denge.hasatXp(denge.urun("karpuz").orElseThrow()));  // 12 sa
  }

  @Test
  @DisplayName("Bolge acilis seviyeleri B.4 ile uyusuyor")
  void bolgeler() {
    assertTrue(denge.bolgeAcikMi("ev", 1));
    assertTrue(denge.bolgeAcikMi("tarla", 1));
    assertFalse(denge.bolgeAcikMi("depo", 1));
    assertTrue(denge.bolgeAcikMi("depo", 2));
    assertFalse(denge.bolgeAcikMi("gar", 4));
    assertTrue(denge.bolgeAcikMi("gar", 5));
    assertFalse(denge.bolgeAcikMi("maden", 8));
    assertTrue(denge.bolgeAcikMi("maden", 9));
  }

  @Test
  @DisplayName("Basit tezgah Sv1'de acik ve tas aletleri veriyor (kilitlenme degismezi)")
  void kilitlenmeDegismezi() {
    var tezgah = denge.evYapisi("basit_tezgah").orElseThrow();
    assertEquals(1, tezgah.acilisSeviye());
    assertTrue(tezgah.tarifler().containsAll(List.of("tas_kazma", "tas_balta", "ahsap_kova")));
  }

  @Test
  @DisplayName("Esya kategorileri urun/tohum/cevher/odun ayrimini yapiyor")
  void esyaKategorileri() {
    assertEquals("urun", denge.esyaKategorisi("bugday"));
    assertEquals("tohum", denge.esyaKategorisi("bugday_tohum"));
    assertEquals("odun", denge.esyaKategorisi("odun"));
    assertEquals("cevher", denge.esyaKategorisi("demir"));
    assertEquals("alet", denge.esyaKategorisi("tas_kazma"));
    assertFalse(denge.esyaVarMi("uydurma_esya"));
  }

  @Test
  @DisplayName("Surum etiketi kararli ve 16 hane")
  void surumEtiketi() {
    assertEquals(16, denge.surumEtiketi().length());
    DengeServisi ikinci = new DengeServisi(new ObjectMapper());
    ikinci.yukle();
    assertEquals(denge.surumEtiketi(), ikinci.surumEtiketi(), "Ayni dosyalar ayni etiketi vermeli");
  }
}
