package tr.amorland.oyun;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import tr.amorland.TestTemeli;
import tr.amorland.denge.DengeServisi;
import tr.amorland.kimlik.KimlikServisi;
import tr.amorland.kimlik.OyuncuDeposu;
import tr.amorland.ortak.OyunHatasi;
import tr.amorland.ortak.RedKodu;
import tr.amorland.oyun.DunyaModeli.Degisim;
import tr.amorland.vadi.Vadi;
import tr.amorland.vadi.VadiDeposu;
import tr.amorland.vadi.VadiServisi;

import java.time.Duration;
import java.util.List;
import java.util.Set;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.*;

/**
 * Dikey dilim: ek -> sula -> hasat, iki oyuncu bagli, durum kalici.
 *
 * <p>A.6: "Calisiyor demeden once calistir." Bu sinif tam olarak bunu yapar.
 */
class DikeyDilimTest extends TestTemeli {

  @Autowired KimlikServisi kimlik;
  @Autowired VadiServisi vadiServisi;
  @Autowired VadiDeposu vadiler;
  @Autowired OyunServisi oyun;
  @Autowired DunyaDeposu dunya;
  @Autowired OyuncuDeposu oyuncular;
  @Autowired DengeServisi denge;
  @Autowired DunyaKurucu kurucu;

  private UUID oyuncuKur(String cihaz, String ad) {
    return kimlik.misafirGiris(cihaz, ad, 0).oyuncuId();
  }

  private Degisim niyet(UUID oyuncuId, UUID vadiId, String tip, Niyet.Hedef hedef, Niyet.Ek ek) {
    return oyun.uygula(oyuncuId, vadiId, new Niyet(UUID.randomUUID(), tip, hedef, ek));
  }

  private Niyet.Hedef karo(int x, int y) {
    return new Niyet.Hedef("tarla", x, y, null);
  }

  // ================================================================== kurulum

  @Test
  @DisplayName("Yeni vadi: 4 karo, 4 bugday tohumu, tarla kenarinda 3 yabani kaynak")
  void vadiKurulumu() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);

    assertEquals(4, dunya.karoSayisi(vadi.id(), o), "Sv1 parseli 4 karo (B.11)");
    assertEquals(4, dunya.envanterAdet(vadi.id(), o, "bugday_tohum"), "Ogretici 4 tohumla baslar");
    assertEquals(3, dunya.dugumler(vadi.id()).size(), "Tarla kenarinda 3 yabani kaynak (B.18 2:20)");
    assertEquals(1, vadi.dunyaSeviyesi());
    assertTrue(vadi.tekImzaModu(), "Partner yokken tek imza modu");
    assertEquals(0, denge.depoKapasitesi(1), "Sv1'de depo yok");
  }

  // ================================================================== ek / sula / hasat

  @Test
  @DisplayName("Ekme tohumu harcar, karoyu doldurur ve ilk_ekim XP'si verir")
  void ekme() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);

    Degisim d = niyet(o, vadi.id(), "ek", karo(0, 0), new Niyet.Ek("bugday", null, null, null));

    assertEquals(3, dunya.envanterAdet(vadi.id(), o, "bugday_tohum"));
    var k = dunya.karo(vadi.id(), o, 0, 0).orElseThrow();
    assertEquals("bugday", k.urunId());
    assertNotNull(k.ekildiAt());
    assertNull(k.sulandiAt(), "Ekilen karo henuz sulanmamis");
    assertTrue(d.olaylar().stream().anyMatch(x -> "ilk_kez".equals(x.tip()) && "ilk_ekim".equals(x.deger())));
    assertEquals(1, d.revision(), "Her eylem revizyonu bir artirir");
  }

  @Test
  @DisplayName("Sulanmamis urun BUYUMEZ: sure gecse bile hasat reddedilir")
  void sulanmayanUrunBuyumez() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    niyet(o, vadi.id(), "ek", karo(0, 0), new Niyet.Ek("bugday", null, null, null));

    sahteSaat().ileriSar(Duration.ofHours(5)); // bugday 5 dakikada buyur

    OyunHatasi hata =
        assertThrows(
            OyunHatasi.class, () -> niyet(o, vadi.id(), "hasat", karo(0, 0), null));
    assertEquals(RedKodu.HENUZ_OLGUN_DEGIL, hata.kod());
    assertTrue(hata.getMessage().contains("sula"), "Mesaj sebebi soylemeli: " + hata.getMessage());
  }

  @Test
  @DisplayName("Kova bossa sulama reddedilir; kuyu doldurunca calisir")
  void kovaZinciri() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    niyet(o, vadi.id(), "ek", karo(0, 0), new Niyet.Ek("bugday", null, null, null));

    // Baslangic kovasi artik DOLU verilir (ogretici varsayimi gercek kuruluma
    // tasindi — dal matematigi 8 odunluk craft'a izin vermiyordu ve yeni oyuncu
    // sulamaya ulasamiyordu). Bos-kova senaryosu elle kurulur.
    dunya.kovaYaz(vadi.id(), o, 0);

    OyunHatasi kovaBos =
        assertThrows(OyunHatasi.class, () -> niyet(o, vadi.id(), "sula", karo(0, 0), null));
    assertEquals(RedKodu.KAYNAK_YETERSIZ, kovaBos.kod());

    // Kova aleti bastan var: kuyu calisir, sulama acilir.
    niyet(o, vadi.id(), "kova_doldur", null, null);
    niyet(o, vadi.id(), "sula", karo(0, 0), null);
  }

  @Test
  @DisplayName("Tam dongu: odun topla -> kova craft -> kuyu -> sula -> hasat (1 urun + 1 tohum)")
  void tamDongu() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);

    // 8 odun icin dal topla: dal 1-2 odun verir, 30 dk'da yenilenir.
    int odun = 0;
    int tur = 0;
    while (odun < 8 && tur < 40) {
      for (var d : dunya.dugumler(vadi.id())) {
        if (!"dal".equals(d.tur())) continue;
        try {
          niyet(o, vadi.id(), "topla", new Niyet.Hedef(d.bolge(), d.x(), d.y(), null), null);
        } catch (OyunHatasi yoksay) {
          // henuz yenilenmedi
        }
      }
      odun = dunya.envanterAdet(vadi.id(), o, "odun");
      sahteSaat().ileriSar(Duration.ofMinutes(31));
      tur++;
    }
    assertTrue(odun >= 8, "Dal toplayarak 8 odun toplanabilmeli, toplanan: " + odun);
    // Ogretici izi KALICI: ilk toplama isareti yazilmis olmali (XP'siz —
    // B.16'nin 6 ilk-kez odulu sabit kalir, bu yalnizca "yapildi" kaydidir).
    assertTrue(
        dunya.isaretler(vadi.id(), o).contains("ilk:ilk_toplama"),
        "Ilk toplama isareti kalici yazilmali (ogretici kilitlenmesi)");

    Degisim craft =
        niyet(o, vadi.id(), "craft", null, new Niyet.Ek(null, null, null, "ahsap_kova"));
    assertTrue(
        craft.aletler().stream().anyMatch(a -> a.aletId().equals("ahsap_kova")),
        "Ahsap kova craftlanmali");
    niyet(o, vadi.id(), "kova_doldur", null, null);
    assertEquals(6, dunya.kovaKalan(vadi.id(), o), "Ahsap kova 6 karo (B.11)");

    niyet(o, vadi.id(), "ek", karo(0, 0), new Niyet.Ek("bugday", null, null, null));
    niyet(o, vadi.id(), "sula", karo(0, 0), null);
    assertEquals(5, dunya.kovaKalan(vadi.id(), o), "Sulama kovadan 1 karo harcar");

    sahteSaat().ileriSar(Duration.ofMinutes(6)); // bugday 5 dk
    Degisim hasat = niyet(o, vadi.id(), "hasat", karo(0, 0), null);

    assertEquals(1, dunya.envanterAdet(vadi.id(), o, "bugday"), "1 urun");
    var k = dunya.karo(vadi.id(), o, 0, 0).orElseThrow();
    assertTrue(k.bosMu(), "Bugday yeniden vermez, karo bosalir");
    assertTrue(
        hasat.olaylar().stream().anyMatch(x -> "defter_sayfasi".equals(x.tip()) && x.sayi() == 2),
        "Ilk hasat defterin 2. sayfasini acar (B.21)");
  }

  @Test
  @DisplayName("Baslangic urunu hasatta 1 tohum geri verir, market urunu vermez")
  void tohumGeriVerme() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    kovaHazirla(o, vadi);

    int oncekiTohum = dunya.envanterAdet(vadi.id(), o, "bugday_tohum");
    niyet(o, vadi.id(), "ek", karo(0, 0), new Niyet.Ek("bugday", null, null, null));
    niyet(o, vadi.id(), "sula", karo(0, 0), null);
    sahteSaat().ileriSar(Duration.ofMinutes(6));
    niyet(o, vadi.id(), "hasat", karo(0, 0), null);

    assertEquals(
        oncekiTohum, dunya.envanterAdet(vadi.id(), o, "bugday_tohum"),
        "Bugday: 1 tohum harcanir, 1 tohum geri gelir -> Perde 1 kendi kendine yeter (B.11)");
  }

  // ================================================================== canta / depo

  @Test
  @DisplayName("Canta slotu dolunca yeni CESIT alinamaz (kisit cesit, miktar degil)")
  void cantaKisiti() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    assertEquals(8, denge.cantaSlot(1));

    // Bugday tohumu zaten 1 slot tutuyor; 7 dolgu ile tam 8 slot dolar.
    for (int i = 0; i < 7; i++) {
      dunya.envanterDegistir(vadi.id(), o, "dolgu_" + i, 1);
    }
    assertEquals(8, dunya.envanterSlotSayisi(vadi.id(), o, denge.yiginBoyutu()));

    // Canta tam dolu -> yeni CESIT girmemeli (miktar degil, cesit kisiti)
    OyunHatasi hata =
        assertThrows(
            OyunHatasi.class,
            () -> niyet(o, vadi.id(), "topla", ilkDal(vadi), null));
    assertEquals(RedKodu.CANTA_DOLU, hata.kod());
  }

  @Test
  @DisplayName("Depo Sv2'de acilir; Sv1'de depo kisiti hasati ENGELLEMEZ")
  void depoSv1deHasatiEngellemez() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    kovaHazirla(o, vadi);

    OyunHatasi hata =
        assertThrows(
            OyunHatasi.class,
            () -> niyet(o, vadi.id(), "depoya_bosalt", null, null));
    assertEquals(RedKodu.SEVIYE_YETERSIZ, hata.kod(), "Sv1'de depo yok");

    niyet(o, vadi.id(), "ek", karo(0, 0), new Niyet.Ek("bugday", null, null, null));
    niyet(o, vadi.id(), "sula", karo(0, 0), null);
    sahteSaat().ileriSar(Duration.ofMinutes(6));
    assertDoesNotThrow(
        () -> niyet(o, vadi.id(), "hasat", karo(0, 0), null),
        "Depo kapasitesi 0 iken hasat engellenmemeli (yoksa ogretici kilitlenir)");
  }

  @Test
  @DisplayName("Ilk depoya bosaltma isareti 'oyuncuId|ilk:...' bicimiyle DELTADA gider")
  void isaretFormati() {
    // Oyuncu bulgusu: delta ciplak isaret tasiyordu, istemci yalnizca
    // "oyuncuId|isaret" bicimini aradigi icin ogreticinin depo adimi hic
    // dusmuyordu. Tam senkron ve delta artik AYNI bicimi tasimali.
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    seviyeAyarla(o, vadi, 2);
    dunya.envanterDegistir(vadi.id(), o, "bugday", 3);

    Degisim d = niyet(o, vadi.id(), "depoya_bosalt", null, null);

    assertTrue(
        d.isaretler().contains(o + "|ilk:ilk_depoya_bosaltma"),
        "Delta isaretleri oyuncu onekiyle gitmeli; gelenler: " + d.isaretler());
  }

  @Test
  @DisplayName("Depo dolunca hasat engellenir, urun karoda BEKLER (yok edilmez)")
  void depoDoluHasatiEngeller() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    seviyeAyarla(o, vadi, 2);
    kovaHazirla(o, vadi);

    int kapasite = denge.depoKapasitesi(2);
    dunya.depoDegistir(vadi.id(), "tas", kapasite, o);
    assertEquals(kapasite, dunya.depoDolu(vadi.id()));

    niyet(o, vadi.id(), "ek", karo(0, 0), new Niyet.Ek("bugday", null, null, null));
    niyet(o, vadi.id(), "sula", karo(0, 0), null);
    sahteSaat().ileriSar(Duration.ofMinutes(6));

    OyunHatasi hata =
        assertThrows(OyunHatasi.class, () -> niyet(o, vadi.id(), "hasat", karo(0, 0), null));
    assertEquals(RedKodu.DEPO_DOLU, hata.kod());

    var k = dunya.karo(vadi.id(), o, 0, 0).orElseThrow();
    assertEquals("bugday", k.urunId(), "Urun karoda BEKLIYOR, yok edilmedi (B.26 yasak 4)");
  }

  // ================================================================== iki oyuncu

  @Test
  @DisplayName("Davet + katil: iki oyuncu ayni vadide, dunya seviyesi ikisinin yuksegi")
  void ikiOyuncuBagli() {
    UUID a = oyuncuKur("cihaz-a", "Ada");
    UUID b = oyuncuKur("cihaz-b", "Bora");
    Vadi vadi = vadiServisi.kur(a);

    // Sv 3 ve hesap baglama zorunlu (B.19)
    seviyeAyarla(a, vadi, 3);
    kimlik.bagla(a, "eposta", "ada@ornek.tr");

    String kod = vadiServisi.davetKoduUret(a);
    assertEquals(6, kod.length());

    Vadi ciftVadi = vadiServisi.katil(b, kod);
    assertEquals(vadi.id(), ciftVadi.id());
    assertEquals(b, ciftVadi.partnerId());
    assertFalse(ciftVadi.tekImzaModu(), "Partner katilinca tek imza modu kapanir");
    assertEquals(3, ciftVadi.dunyaSeviyesi(), "Dunya seviyesi = max(3, 1)");
    assertEquals(4, dunya.karoSayisi(vadi.id(), b), "Partnerin kendi parseli acilir");
  }

  @Test
  @DisplayName("Partnerin parseline dokunulamaz")
  void baskaParselYasak() {
    UUID a = oyuncuKur("cihaz-a", "Ada");
    UUID b = oyuncuKur("cihaz-b", "Bora");
    Vadi vadi = vadiServisi.kur(a);
    seviyeAyarla(a, vadi, 3);
    kimlik.bagla(a, "eposta", "ada@ornek.tr");
    vadiServisi.katil(b, vadiServisi.davetKoduUret(a));

    OyunHatasi hata =
        assertThrows(
            OyunHatasi.class,
            () ->
                niyet(
                    a, vadi.id(), "ek",
                    new Niyet.Hedef("tarla", 0, 0, b),
                    new Niyet.Ek("bugday", null, null, null)));
    assertEquals(RedKodu.BASKA_PARSEL, hata.kod());
  }

  @Test
  @DisplayName("Depo ortak: biri koyar, digeri alir (asenkron ortak oynanisin kalbi)")
  void depoOrtak() {
    UUID a = oyuncuKur("cihaz-a", "Ada");
    UUID b = oyuncuKur("cihaz-b", "Bora");
    Vadi vadi = vadiServisi.kur(a);
    seviyeAyarla(a, vadi, 3);
    kimlik.bagla(a, "eposta", "ada@ornek.tr");
    vadiServisi.katil(b, vadiServisi.davetKoduUret(a));

    dunya.envanterDegistir(vadi.id(), a, "odun", 10);
    niyet(a, vadi.id(), "depoya_koy", null, new Niyet.Ek(null, "odun", 10, null));

    var yigin = dunya.depoSatiri(vadi.id(), "odun").orElseThrow();
    assertEquals(10, yigin.adet());
    assertEquals(a, yigin.koyan(), "Yiginda koyanin renk rozeti (B.10)");

    niyet(b, vadi.id(), "depodan_al", null, new Niyet.Ek(null, "odun", 4, null));
    assertEquals(4, dunya.envanterAdet(vadi.id(), b, "odun"), "Partner alabilmeli");
    assertEquals(6, dunya.depoSatiri(vadi.id(), "odun").orElseThrow().adet());
  }

  @Test
  @DisplayName("Kilitli yigini partner alamaz; kilit siniri 3")
  void depoKilit() {
    UUID a = oyuncuKur("cihaz-a", "Ada");
    UUID b = oyuncuKur("cihaz-b", "Bora");
    Vadi vadi = vadiServisi.kur(a);
    seviyeAyarla(a, vadi, 3);
    kimlik.bagla(a, "eposta", "ada@ornek.tr");
    vadiServisi.katil(b, vadiServisi.davetKoduUret(a));

    for (String e : List.of("odun", "tas", "komur", "demir")) {
      dunya.depoDegistir(vadi.id(), e, 5, a);
    }
    for (String e : List.of("odun", "tas", "komur")) {
      niyet(a, vadi.id(), "depo_kilit", null, new Niyet.Ek(null, e, null, null));
    }
    OyunHatasi dorduncu =
        assertThrows(
            OyunHatasi.class,
            () -> niyet(a, vadi.id(), "depo_kilit", null, new Niyet.Ek(null, "demir", null, null)));
    assertEquals(RedKodu.KILITLI, dorduncu.kod());

    OyunHatasi partner =
        assertThrows(
            OyunHatasi.class,
            () -> niyet(b, vadi.id(), "depodan_al", null, new Niyet.Ek(null, "odun", 1, null)));
    assertEquals(RedKodu.KILITLI, partner.kod());
  }

  // ================================================================== protokol

  @Test
  @DisplayName("Ayni client_action_id ikinci kez gelirse ayni sonuc doner, durum degismez")
  void idempotency() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);

    Niyet n = new Niyet(UUID.randomUUID(), "ek", karo(0, 0), new Niyet.Ek("bugday", null, null, null));
    Degisim ilk = oyun.uygula(o, vadi.id(), n);
    Degisim tekrar = oyun.uygula(o, vadi.id(), n);

    assertEquals(ilk.revision(), tekrar.revision(), "Tekrar ayni revizyonu dondurmeli");
    assertEquals(3, dunya.envanterAdet(vadi.id(), o, "bugday_tohum"), "Tohum bir kez harcanmali");
    assertEquals(1, vadiler.bul(vadi.id()).orElseThrow().revision(), "Revizyon iki kez artmamali");
  }

  @Test
  @DisplayName("Revizyon delta'lari sirayla gelir; anlik goruntu ile ayni sekle sahip")
  void revizyonAkisi() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);

    niyet(o, vadi.id(), "ek", karo(0, 0), new Niyet.Ek("bugday", null, null, null));
    niyet(o, vadi.id(), "ek", karo(1, 0), new Niyet.Ek("bugday", null, null, null));
    Vadi guncel = vadiler.bul(vadi.id()).orElseThrow();
    assertEquals(2, guncel.revision());

    List<Degisim> deltalar = oyun.deltalarVeyaTam(guncel, 0, Set.of());
    assertEquals(2, deltalar.size());
    assertFalse(deltalar.get(0).tam());
    assertEquals(1, deltalar.get(0).revision());
    assertEquals(2, deltalar.get(1).revision());

    Degisim tam = oyun.anlikGoruntu(guncel, Set.of(o));
    assertTrue(tam.tam());
    assertEquals(4, tam.karolar().size());
    assertEquals(1, tam.oyuncular().size());
    assertNotNull(tam.vadi());
  }

  @Test
  @DisplayName("Altin Sv 5'e kadar gizli (-1): HUD'da gostergesi bile yok")
  void paraSv5teGirer() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);

    Degisim tam = oyun.anlikGoruntu(vadi, Set.of(o));
    assertEquals(-1, tam.oyuncular().get(0).altin(), "Sv1-4'te altin gizli (B.15)");

    seviyeAyarla(o, vadi, 5);
    Vadi guncel = vadiler.bul(vadi.id()).orElseThrow();
    assertEquals(0, oyun.anlikGoruntu(guncel, Set.of(o)).oyuncular().get(0).altin());
  }

  // ================================================================== yardimcilar

  private Niyet.Hedef ilkDal(Vadi vadi) {
    var d =
        dunya.dugumler(vadi.id()).stream()
            .filter(x -> "dal".equals(x.tur()))
            .findFirst()
            .orElseThrow();
    return new Niyet.Hedef(d.bolge(), d.x(), d.y(), null);
  }

  /** Testte seviyeyi dogrudan ayarlar ve parseli o seviyeye acar. */
  private void seviyeAyarla(UUID oyuncuId, Vadi vadi, int seviye) {
    oyuncular.seviyeVeXpYaz(oyuncuId, seviye, 0);
    vadiler.dunyaSeviyesiYaz(vadi.id(), seviye);
    kurucu.parseliAc(vadi.id(), oyuncuId, seviye);
  }

  /** Testte kova zincirini kisa yoldan hazirlar. */
  private void kovaHazirla(UUID oyuncuId, Vadi vadi) {
    dunya.aletYaz(vadi.id(), oyuncuId, "ahsap_kova", 0);
    dunya.kovaYaz(vadi.id(), oyuncuId, 6);
  }

  // ================================================================== orman (B.14)

  @Test
  @DisplayName("Agac kesme: baltasiz red, cok vurus, yipranma, devrilme, yeniden buyume")
  void agacKesme() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    kurucu.ormaniKur(vadi.id());
    oyuncular.seviyeVeXpYaz(o, 7, 0);

    var cam =
        dunya.dugumler(vadi.id()).stream()
            .filter(d -> "agac".equals(d.sinif()) && "cam".equals(d.tur()))
            .findFirst()
            .orElseThrow();
    var hedef = new Niyet.Hedef(cam.bolge(), cam.x(), cam.y(), null);

    OyunHatasi baltasiz =
        assertThrows(OyunHatasi.class, () -> niyet(o, vadi.id(), "kes", hedef, null));
    assertEquals(RedKodu.ALET_YOK, baltasiz.kod(), "Baltasiz kesim reddedilir (B.14)");

    dunya.aletYaz(vadi.id(), o, "tas_balta", 40);

    niyet(o, vadi.id(), "kes", hedef, null);
    niyet(o, vadi.id(), "kes", hedef, null);
    assertEquals(0, dunya.envanterAdet(vadi.id(), o, "odun"), "Devrilmeden getiri yok");

    Degisim son = niyet(o, vadi.id(), "kes", hedef, null); // cam: 3 vurus

    assertEquals(4, dunya.envanterAdet(vadi.id(), o, "odun"), "Cam 4 odun verir (D)");
    int dayaniklilik =
        dunya.aletler(vadi.id(), o).stream()
            .filter(a -> "tas_balta".equals(a.aletId()))
            .findFirst()
            .orElseThrow()
            .dayaniklilik();
    assertEquals(37, dayaniklilik, "3 vurus baltayi 3 yipratir");
    assertTrue(
        son.isaretler().contains(o + "|ilk:ilk_agac_kesimi"),
        "Ilk kesim kalici iz birakir (hedef zinciri)");

    OyunHatasi buyuyor =
        assertThrows(OyunHatasi.class, () -> niyet(o, vadi.id(), "kes", hedef, null));
    assertEquals(RedKodu.DUGUM_TUKENDI, buyuyor.kod(), "Devrik agac hemen kesilmez");

    sahteSaat().ileriSar(Duration.ofSeconds(28801)); // cam buyumeSn
    assertDoesNotThrow(
        () -> niyet(o, vadi.id(), "kes", hedef, null), "Buyuyen agac yeniden kesilir");
  }

  // ================================================================== tren + market (B.15)

  @Autowired TrenServisi tren;

  @Test
  @DisplayName("Tren: Sv5 alti red; siparis karsilanir, altin yatar; ayni kart ikinciye SIPARIS_DOLDU")
  void trenKarsilamaVeMarket() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);

    OyunHatasi erken =
        assertThrows(
            OyunHatasi.class,
            () -> niyet(o, vadi.id(), "tren_karsila", new Niyet.Hedef("gar", 0, 0, null), null));
    assertEquals(RedKodu.SEVIYE_YETERSIZ, erken.kod(), "Para Sv5'te oyuna girer (B.15)");

    oyuncular.seviyeVeXpYaz(o, 5, 0);
    long sefer = tren.seferNo(sahteSaat().simdi());
    var siparis = tren.siparisler(vadi, vadi.dunyaSeviyesi(), sefer).get(0);
    dunya.envanterDegistir(vadi.id(), o, siparis.urunId(), siparis.miktar());

    // Bayat pano reddi: istemci sefer N-1'in kartina basmisken tren kalkmissa
    // sunucu N'in kartini SESSIZCE esleyemez — oyuncu gormedigini onaylayamaz.
    OyunHatasi bayat =
        assertThrows(
            OyunHatasi.class,
            () ->
                niyet(
                    o, vadi.id(), "tren_karsila",
                    new Niyet.Hedef("gar", 0, 0, null),
                    new Niyet.Ek(null, null, null, null, sefer - 1)));
    assertEquals(RedKodu.GECERSIZ_HEDEF, bayat.kod(), "Sefer uyusmazligi acik red");

    Degisim d =
        niyet(
            o, vadi.id(), "tren_karsila",
            new Niyet.Hedef("gar", 0, 0, null),
            new Niyet.Ek(null, null, null, null, sefer));

    assertEquals(siparis.altinOdul(), dunya.altin(vadi.id(), o), "Odul tek seferde tam yatar");
    assertTrue(
        d.isaretler().contains(o + "|ilk:ilk_tren_teslimi"),
        "Ilk tren teslimi kalici iz birakmali (hedef zinciri)");
    assertEquals(0, dunya.envanterAdet(vadi.id(), o, siparis.urunId()), "Kismi yatirma yok, tam teslim");
    assertTrue(d.olaylar().stream().anyMatch(x -> "tren_siparis_karsilandi".equals(x.tip())));

    // C.5: ayni karta ikinci gelen kaybeder — birincil anahtar zorlar.
    dunya.envanterDegistir(vadi.id(), o, siparis.urunId(), siparis.miktar());
    OyunHatasi dolu =
        assertThrows(
            OyunHatasi.class,
            () -> niyet(o, vadi.id(), "tren_karsila", new Niyet.Hedef("gar", 0, 0, null), null));
    assertEquals(RedKodu.SIPARIS_DOLDU, dolu.kod());
  }

  @Test
  @DisplayName("Market: sat sabit dusuk oranla aninda oder; satin_al tohum fiyatini duser")
  void marketSatVeAl() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    oyuncular.seviyeVeXpYaz(o, 5, 0);
    dunya.envanterDegistir(vadi.id(), o, "bugday", 10);

    niyet(o, vadi.id(), "sat", null, new Niyet.Ek(null, "bugday", 10, null));

    var oran = denge.urunlerDosyasi().marketAlisOrani();
    long satisGeliri =
        Math.max(
            1,
            (long)
                Math.floor(
                    denge.urun("bugday").orElseThrow().tabanFiyat()
                        * ((oran.min() + oran.max()) / 2.0)
                        * 10));
    assertEquals(satisGeliri, dunya.altin(vadi.id(), o), "B.15: market dusuk ve SABIT oranla alir");
    assertEquals(0, dunya.envanterAdet(vadi.id(), o, "bugday"));

    int tohumOnce = dunya.envanterAdet(vadi.id(), o, "bugday_tohum");
    niyet(o, vadi.id(), "satin_al", null, new Niyet.Ek(null, "bugday_tohum", 2, null));
    assertEquals(tohumOnce + 2, dunya.envanterAdet(vadi.id(), o, "bugday_tohum"));
    long fiyat = 2L * denge.urun("bugday").orElseThrow().tohumFiyat();
    assertEquals(satisGeliri - fiyat, dunya.altin(vadi.id(), o), "Tohum bedeli kasadan duser");

    OyunHatasi kapali =
        assertThrows(
            OyunHatasi.class,
            () -> niyet(o, vadi.id(), "satin_al", null, new Niyet.Ek(null, "domates_tohum", 1, null)));
    assertEquals(RedKodu.SEVIYE_YETERSIZ, kapali.kod(), "Acilmamis urunun tohumu satilmaz");
  }

  @Test
  @DisplayName("Tohum penceresi (B.11): safran tohumu yalnizca acik pencerede satilir")
  void tohumPenceresi() {
    UUID o = oyuncuKur("cihaz-1", "Ada");
    Vadi vadi = vadiServisi.kur(o);
    oyuncular.seviyeVeXpYaz(o, 19, 0);
    dunya.altinDegistir(vadi.id(), o, 1_000_000);
    var safran = denge.urun("safran").orElseThrow();
    assertTrue(safran.tohumPenceresi(), "safran pencereli olmali (denge)");

    // Pencere deterministik: 15 dk adimlarla 24 saat taranir, iki durum da bulunur.
    java.time.Instant acikAn = null;
    java.time.Instant kapaliAn = null;
    for (int i = 0; i < 96 && (acikAn == null || kapaliAn == null); i++) {
      java.time.Instant aday = BASLANGIC.plusSeconds(i * 900L);
      if (oyun.tohumPenceresiAcikMi(vadi, safran, aday)) {
        if (acikAn == null) acikAn = aday;
      } else if (kapaliAn == null) {
        kapaliAn = aday;
      }
    }
    assertNotNull(acikAn, "24 saatte en az bir acik pencere olmali (gunde 3 x 2 saat)");
    assertNotNull(kapaliAn, "pencereler gunun tamamini kaplamamali");

    sahteSaat().ayarla(kapaliAn);
    OyunHatasi kapali =
        assertThrows(
            OyunHatasi.class,
            () -> niyet(o, vadi.id(), "satin_al", null, new Niyet.Ek(null, "safran_tohum", 1, null)));
    assertEquals(RedKodu.ESYA_YOK, kapali.kod(), "Pencere disinda tezgahta yok");

    sahteSaat().ayarla(acikAn);
    niyet(o, vadi.id(), "satin_al", null, new Niyet.Ek(null, "safran_tohum", 1, null));
    assertEquals(1, dunya.envanterAdet(vadi.id(), o, "safran_tohum"), "Pencerede satis calisir");
  }
}
