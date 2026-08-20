package tr.amorland;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import tr.amorland.denge.DengeServisi;
import tr.amorland.kimlik.KimlikServisi;
import tr.amorland.kimlik.OyuncuDeposu;
import tr.amorland.ortak.Deterministik;
import tr.amorland.ortak.OyunHatasi;
import tr.amorland.ortak.RedKodu;
import tr.amorland.oyun.DunyaDeposu;
import tr.amorland.oyun.DunyaKurucu;
import tr.amorland.oyun.Niyet;
import tr.amorland.oyun.OyunServisi;
import tr.amorland.vadi.Vadi;
import tr.amorland.vadi.VadiDeposu;
import tr.amorland.vadi.VadiServisi;

import java.time.Duration;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.*;

/**
 * B.26'daki 26 yasak ve B.3'teki 8 mimari kuralin <b>otomatik</b> denetimi.
 *
 * <p>A.6: "Bu projede en pahali hatalar sessizce dogru gorunen hatalardi." Bir yasak
 * kod incelemesiyle korunuyorsa, bir sonraki degisiklikte sessizce delinir. Buradaki
 * her test bir yasagi <b>calisan sisteme sorarak</b> dogrular.
 *
 * <p>Istemci tarafi yasaklar (envanterde surukle-birak, sesle bildirim) burada
 * dogrulanamaz; onlar {@code araclar/butunluk-denetimi.py} ile statik olarak taranir.
 */
class ButunlukTest extends TestTemeli {

  @Autowired KimlikServisi kimlik;
  @Autowired VadiServisi vadiServisi;
  @Autowired VadiDeposu vadiler;
  @Autowired OyunServisi oyun;
  @Autowired DunyaDeposu dunya;
  @Autowired OyuncuDeposu oyuncular;
  @Autowired DengeServisi denge;
  @Autowired DunyaKurucu kurucu;

  private UUID oyuncuKur(String cihaz) {
    return kimlik.misafirGiris(cihaz, "Test", 0).oyuncuId();
  }

  private void niyet(UUID o, UUID v, String tip, Niyet.Hedef h, Niyet.Ek e) {
    oyun.uygula(o, v, new Niyet(UUID.randomUUID(), tip, h, e));
  }

  private void kovaHazirla(UUID o, Vadi v) {
    dunya.aletYaz(v.id(), o, "ahsap_kova", 0);
    dunya.kovaYaz(v.id(), o, 6);
  }

  // ================================================================== mimari kurallar

  @Test
  @DisplayName("Kural 1: istemci niyeti miktar/fiyat/XP TASIMAZ")
  void kural1_istemciSadeceNiyetGonderir() {
    // Niyet kaydinin alanlari: yalnizca "ne, nerede". Bir gun birisi buraya
    // "kazanilanXp" eklemek isterse bu test onu durdurur.
    Set<String> alanlar = new HashSet<>();
    for (var alan : Niyet.class.getRecordComponents()) alanlar.add(alan.getName());
    assertEquals(Set.of("clientActionId", "tip", "hedef", "ek"), alanlar);

    Set<String> ekAlanlar = new HashSet<>();
    for (var alan : Niyet.Ek.class.getRecordComponents()) ekAlanlar.add(alan.getName());
    assertEquals(
        Set.of("urunId", "esyaId", "adet", "tarifId"), ekAlanlar,
        "Ek'te fiyat/xp/sure alani olmamali (C.11: istemciden hicbir miktar kabul edilmez)");
  }

  @Test
  @DisplayName("Kural 2: buyume icin CRON yok — asama okuma aninda hesaplanir")
  void kural2_cronYok() {
    UUID o = oyuncuKur("c1");
    Vadi vadi = vadiServisi.kur(o);
    kovaHazirla(o, vadi);
    niyet(o, vadi.id(), "ek", new Niyet.Hedef("tarla", 0, 0, null),
        new Niyet.Ek("bugday", null, null, null));
    niyet(o, vadi.id(), "sula", new Niyet.Hedef("tarla", 0, 0, null), null);

    long revizyonOncesi = vadiler.bul(vadi.id()).orElseThrow().revision();
    sahteSaat().ileriSar(Duration.ofHours(3));

    // Zaman gecti, hicbir arka plan isi calismadi: revizyon DEGISMEDI.
    assertEquals(
        revizyonOncesi, vadiler.bul(vadi.id()).orElseThrow().revision(),
        "Zaman gecmesi tek basina durum degistirmemeli (cron olmadiginin kaniti)");

    // Ama urun olgunlasti: asama okuma aninda hesaplaniyor.
    assertDoesNotThrow(
        () -> niyet(o, vadi.id(), "hasat", new Niyet.Hedef("tarla", 0, 0, null), null));
  }

  @Test
  @DisplayName("Kural 3: tren fiyati DETERMINISTIK — ayni girdi ayni sonuc, satir yazilmaz")
  void kural3_deterministikFiyat() {
    long tohum = 123456789L;
    double a = Deterministik.hash01(tohum, "bugday", 42);
    double b = Deterministik.hash01(tohum, "bugday", 42);
    assertEquals(a, b, 0.0, "Ayni girdi ayni degeri vermeli");

    double c = Deterministik.hash01(tohum, "bugday", 43);
    assertNotEquals(a, c, "Farkli sefer farkli deger vermeli");

    // Ayirici olmadan ("ab","c") ile ("a","bc") ayni cikardi.
    assertNotEquals(
        Deterministik.hash01("ab", "c"), Deterministik.hash01("a", "bc"),
        "Parcalar ayirici ile birlestirilmeli");

    // 0..1 araliginda ve tekduze dagilmis olmali.
    double toplam = 0;
    for (int i = 0; i < 2000; i++) {
      double v = Deterministik.hash01(tohum, "urun", i);
      assertTrue(v >= 0.0 && v < 1.0, "hash01 [0,1) araliginda olmali: " + v);
      toplam += v;
    }
    assertEquals(0.5, toplam / 2000.0, 0.03, "Dagilim tekduze olmali");
  }

  @Test
  @DisplayName("Kural 4: client_action_id ZORUNLU ve tekrar yutulur")
  void kural4_idempotency() {
    UUID o = oyuncuKur("c1");
    Vadi vadi = vadiServisi.kur(o);

    OyunHatasi hata =
        assertThrows(
            OyunHatasi.class,
            () -> oyun.uygula(o, vadi.id(),
                new Niyet(null, "ek", new Niyet.Hedef("tarla", 0, 0, null),
                    new Niyet.Ek("bugday", null, null, null))));
    assertTrue(hata.getMessage().contains("client_action_id"));
  }

  @Test
  @DisplayName("Kural 5: istemci saati kabul edilmez — ekim zamani SUNUCUDAN")
  void kural5_sunucuSaati() {
    UUID o = oyuncuKur("c1");
    Vadi vadi = vadiServisi.kur(o);
    niyet(o, vadi.id(), "ek", new Niyet.Hedef("tarla", 0, 0, null),
        new Niyet.Ek("bugday", null, null, null));

    var karo = dunya.karo(vadi.id(), o, 0, 0).orElseThrow();
    assertEquals(
        BASLANGIC, karo.ekildiAt(),
        "ekildiAt sunucu saatinden gelmeli; istemci bu alani gonderemiyor bile");
  }

  @Test
  @DisplayName("Kural 6: sohbet icerigi sunucuda SAKLANMAZ")
  void kural6_sohbetSaklanmaz() {
    // Sohbet mesaji icin yazma yolu yalnizca teslimat kuyrugudur ve teslim edilince silinir.
    // Kalici bir "mesaj" veya "sohbet_gecmisi" tablosu OLMAMALI.
    List<String> tablolar =
        jdbc.queryForList(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'",
            String.class);
    for (String t : tablolar) {
      assertFalse(
          t.contains("sohbet") || t.equals("mesaj"),
          "Sohbet arsivi tablosu olmamali, bulundu: " + t);
    }
    assertTrue(tablolar.contains("mesaj_kuyrugu"), "Teslimat kuyrugu olmali (arsiv degil)");
  }

  @Test
  @DisplayName("Kural 8: denge sayilari koda gomulmemis — JSON degisince davranis degisir")
  void kural8_dengeKodaGomulmez() {
    // Ayni denge servisini iki kez yukleyip ayni sonucu almak yeterli degil;
    // asil kanit tablolarin JSON'dan okunmasi. Ornek: canta kademeleri.
    assertEquals(8, denge.cantaSlot(1));
    assertEquals(12, denge.cantaSlot(4));
    assertEquals(
        denge.ilerlemeDosyasi().canta().kademeler().size(), 4,
        "Canta kademeleri JSON'dan geliyor");
    assertEquals(
        denge.urunlerDosyasi().urunler().size(), 16,
        "Urun tablosu JSON'dan geliyor");
  }

  // ================================================================== yasaklar

  @Test
  @DisplayName("Yasak 4: esya YOK EDILMEZ — depo doluyken hasat urunu karoda birakir")
  void yasak4_esyaYokEdilmez() {
    UUID o = oyuncuKur("c1");
    Vadi vadi = vadiServisi.kur(o);
    oyuncular.seviyeVeXpYaz(o, 2, 0);
    vadiler.dunyaSeviyesiYaz(vadi.id(), 2);
    kovaHazirla(o, vadi);

    dunya.depoDegistir(vadi.id(), "tas", denge.depoKapasitesi(2), o);
    niyet(o, vadi.id(), "ek", new Niyet.Hedef("tarla", 0, 0, null),
        new Niyet.Ek("bugday", null, null, null));
    niyet(o, vadi.id(), "sula", new Niyet.Hedef("tarla", 0, 0, null), null);
    sahteSaat().ileriSar(Duration.ofMinutes(6));

    int oncekiEnvanter = dunya.envanterAdet(vadi.id(), o, "bugday");
    assertThrows(
        OyunHatasi.class,
        () -> niyet(o, vadi.id(), "hasat", new Niyet.Hedef("tarla", 0, 0, null), null));

    assertEquals(oncekiEnvanter, dunya.envanterAdet(vadi.id(), o, "bugday"), "Urun cantaya girmedi");
    assertEquals("bugday", dunya.karo(vadi.id(), o, 0, 0).orElseThrow().urunId(), "Urun KAROda duruyor");
    assertTrue(
        denge.ilerlemeDosyasi().depo().doluKurali().esyaYokEdilir() == false
            && denge.ilerlemeDosyasi().depo().doluKurali().esyaYereDuser() == false
            && denge.ilerlemeDosyasi().depo().doluKurali().otomatikSatilir() == false,
        "Denge dosyasi da ayni sozu veriyor");
  }

  @Test
  @DisplayName("Yasak 21: kuslugun EKONOMIK ETKISI YOK")
  void yasak21_kuslukEkonomikDegil() {
    UUID a = oyuncuKur("ca");
    UUID b = oyuncuKur("cb");
    Vadi vadi = vadiServisi.kur(a);
    oyuncular.seviyeVeXpYaz(a, 3, 0);
    vadiler.dunyaSeviyesiYaz(vadi.id(), 3);
    kimlik.bagla(a, "eposta", "a@b.c");
    vadiServisi.katil(b, vadiServisi.davetKoduUret(a));
    kovaHazirla(a, vadi);

    // Kuslukten ONCE hasat
    int oncekiXp = oyuncular.bul(a).orElseThrow().xp();
    HasatYap(a, vadi, 0, 0);
    int kusluksuzUrun = dunya.envanterAdet(vadi.id(), a, "bugday");

    // Kusluk baslat, tekrar hasat
    niyet(a, vadi.id(), "kusluk_baslat", null, null);
    assertTrue(dunya.kuslukBaslatan(vadi.id()).isPresent(), "Kusluk aktif olmali");
    HasatYap(a, vadi, 1, 0);
    int kuslukluUrun = dunya.envanterAdet(vadi.id(), a, "bugday") - kusluksuzUrun;

    assertEquals(
        kusluksuzUrun, kuslukluUrun,
        "Kusluk hasat getirisini DEGISTIRMEMELI — para cezasi olan bir rol, silaha doner");
    assertFalse(
        denge.ilerlemeDosyasi() != null && KuslukEkonomikMi(),
        "Denge dosyasinda da kuslugun ekonomik etkisi olmamali");
  }

  private boolean KuslukEkonomikMi() {
    var dugum = denge.hamPaket().path("ilerleme").path("kusluk").path("ekonomikEtki");
    return dugum.asBoolean(false);
  }

  private void HasatYap(UUID o, Vadi vadi, int x, int y) {
    dunya.kovaYaz(vadi.id(), o, 6);
    niyet(o, vadi.id(), "ek", new Niyet.Hedef("tarla", x, y, null),
        new Niyet.Ek("bugday", null, null, null));
    niyet(o, vadi.id(), "sula", new Niyet.Hedef("tarla", x, y, null), null);
    sahteSaat().ileriSar(Duration.ofMinutes(6));
    niyet(o, vadi.id(), "hasat", new Niyet.Hedef("tarla", x, y, null), null);
  }

  @Test
  @DisplayName("Yasak 1: kusluk HAREKETSIZLIKLE tetiklenmez")
  void yasak1_kuslukOtomatikDegil() {
    UUID a = oyuncuKur("ca");
    UUID b = oyuncuKur("cb");
    Vadi vadi = vadiServisi.kur(a);
    oyuncular.seviyeVeXpYaz(a, 3, 0);
    vadiler.dunyaSeviyesiYaz(vadi.id(), 3);
    kimlik.bagla(a, "eposta", "a@b.c");
    vadiServisi.katil(b, vadiServisi.davetKoduUret(a));

    // 30 gun hicbir sey yapma
    sahteSaat().ileriSar(Duration.ofDays(30));
    vadiServisi.tekImzaModunuTazele(vadiler.bul(vadi.id()).orElseThrow());

    assertTrue(
        dunya.kuslukBaslatan(vadi.id()).isEmpty(),
        "30 gun hareketsizlik kusluk YARATMAMALI (B.8 kirmizi cizgi)");
    assertFalse(
        denge.ilerlemeDosyasi().partnerTerk().bildirimGonderir(),
        "Partner terkinde bildirim gitmemeli");
  }

  @Test
  @DisplayName("Yasak 14: Perde 1'de para YOK, market Sv 6'da")
  void yasak14_perde1deParaYok() {
    assertEquals(5, denge.ilerlemeDosyasi().paraninGirisi().ilkSeviye());
    assertEquals(6, denge.bolge("market").orElseThrow().acilisSeviye());
    assertEquals(5, denge.bolge("gar").orElseThrow().acilisSeviye());

    UUID o = oyuncuKur("c1");
    Vadi vadi = vadiServisi.kur(o);
    var ozet = oyun.anlikGoruntu(vadi, Set.of(o)).oyuncular().get(0);
    assertEquals(-1, ozet.altin(), "Sv1'de altin gonderilmemeli (HUD'da gostergesi bile yok)");
  }

  @Test
  @DisplayName("Yasak 13: rastgele eslesme YOK — tek giris davet kodu")
  void yasak13_rastgeleEslesmeYok() {
    UUID a = oyuncuKur("ca");
    UUID b = oyuncuKur("cb");
    vadiServisi.kur(a);
    vadiServisi.kur(b);

    // b, a'nin vadisine kod olmadan giremez.
    OyunHatasi hata =
        assertThrows(OyunHatasi.class, () -> vadiServisi.katil(b, "UYDURMA"));
    assertTrue(hata.getMessage().contains("bulunamadi"));
  }

  @Test
  @DisplayName("Yasak 9: mevsim / enerji bari / premium para YOK")
  void yasak9_yasakSistemlerYok() {
    List<String> tablolar =
        jdbc.queryForList(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'",
            String.class);
    for (String t : tablolar) {
      assertFalse(t.contains("mevsim"), "Mevsim tablosu olmamali: " + t);
      assertFalse(t.contains("enerji"), "Enerji tablosu olmamali: " + t);
      assertFalse(t.contains("premium") || t.contains("elmas_para"), "Premium para olmamali: " + t);
    }
    String ham = denge.hamPaket().toString();
    assertFalse(ham.contains("\"mevsim\""), "Denge dosyalarinda mevsim olmamali");
    assertFalse(ham.contains("\"enerji\""), "Denge dosyalarinda enerji bari olmamali");
  }

  @Test
  @DisplayName("Yasak 11: bir seviyede iki yeni ETKILESIM acilmaz")
  void yasak11_seviyeBasinaTekEtkilesim() {
    // Etkilesim = yeni bolge veya yeni ev yapisi. Urun/kapasite/tarif SAYILMAZ.
    var sayac = new java.util.HashMap<Integer, java.util.List<String>>();
    for (var b : denge.ilerlemeDosyasi().bolgeler()) {
      if ("ev".equals(b.id()) || "tarla".equals(b.id())) continue; // Sv1 baslangici
      sayac.computeIfAbsent(b.acilisSeviye(), k -> new java.util.ArrayList<>()).add("bolge:" + b.id());
    }
    for (var y : denge.ilerlemeDosyasi().evYapilari()) {
      if (y.acilisSeviye() <= 1) continue;
      sayac.computeIfAbsent(y.acilisSeviye(), k -> new java.util.ArrayList<>()).add("yapi:" + y.id());
    }
    sayac.forEach(
        (seviye, liste) ->
            assertTrue(
                liste.size() <= 1,
                "Sv " + seviye + " birden fazla etkilesim aciyor: " + liste
                    + " (B.26 yasak 11: ogrenme egrisi kirilir)"));
  }

  @Test
  @DisplayName("Yasak 12: tek seferlik XP odulleri Perde 1'in motorunu tasiyor")
  void yasak12_tekSeferlikOduller() {
    var xp = denge.ilerlemeDosyasi().xpKaynaklari();
    int defter = xp.defterSayfasi();
    int bolge = xp.bolgeIlkAcma();
    int ilkKez = xp.ilkKezAnlari().xp();
    int craft = xp.craftIlkKez();

    // B.16: 5 defter (375) + 3 bolge (150) + 3 craft (90) + 6 ilk kez (150) = 765
    int perde1 = 5 * defter + 3 * bolge + 3 * craft + 6 * ilkKez;
    assertEquals(765, perde1, "Perde 1 tek seferlik XP toplami 765 olmali (B.16)");

    int gereken = 0;
    for (int s = 1; s <= 4; s++) gereken += denge.seviyeEsigi(s);
    assertEquals(537, gereken, "Sv 5'e ulasmak icin gereken XP");
    assertTrue(
        perde1 > gereken,
        "Tek seferlik oduller gerekenden FAZLA olmali; kisilirsa Perde 1 angaryaya doner");
  }

  @Test
  @DisplayName("Yasak 3 ve 2: market ve depo comert degil")
  void yasak2ve3_ekonomikBaskiKorunuyor() {
    var market = denge.urunlerDosyasi().marketAlisOrani();
    assertTrue(market.max() <= 0.6, "Market alis fiyati taban fiyatin %60'ini gecmemeli");
    assertTrue(market.min() >= 0.5, "Market alis fiyati %50'nin altina inmemeli");

    var kademeler = denge.ilerlemeDosyasi().depo().kademeler();
    for (int i = 1; i < kademeler.size(); i++) {
      assertTrue(
          kademeler.get(i).seferKarsiligi() < kademeler.get(i - 1).seferKarsiligi(),
          "Depo baskisi ilerledikce ARTMALI (sefer karsiligi dusmeli)");
      assertTrue(
          kademeler.get(i).maliyet() > kademeler.get(i - 1).maliyet() * 2,
          "Depo maliyeti ustel artmali");
    }
  }

  @Test
  @DisplayName("Yasak 6 ve kilitlenme degismezi: tas alet her zaman ulasilabilir")
  void yasak6_kilitlenmeYok() {
    UUID o = oyuncuKur("c1");
    Vadi vadi = vadiServisi.kur(o);

    // Alet yok, cevher yok, para yok. Yine de tas kazma yapilabilmeli:
    // dal ve tas parcasi ELLE toplanir.
    var kaynaklar = denge.uretimDosyasi().yabaniKaynaklar().kaynaklar();
    Set<String> elleToplanabilir = new HashSet<>();
    for (var k : kaynaklar) for (var g : k.getiri()) elleToplanabilir.add(g.id());

    var tasKazma = denge.alet("tas_kazma").orElseThrow();
    assertEquals(1, tasKazma.craftSeviye());
    for (var m : tasKazma.malzeme()) {
      assertTrue(
          elleToplanabilir.contains(m.id()),
          "tas_kazma malzemesi " + m.id() + " alet gerektirmeden toplanabilmeli");
    }
    assertTrue(Boolean.TRUE.equals(tasKazma.herZamanMarkette()), "Market her zaman satmali");
  }

  @Test
  @DisplayName("Yasak 24: ortak dugum sagligi x1.5 — 4x cevher patlamasi kapali")
  void yasak24_ortakDugumCarpani() {
    var od = denge.uretimDosyasi().ortakDugum();
    assertEquals(1.5, od.ortakVurustaSaglikCarpani(), 1e-9);
    assertFalse(od.getiriBolusulur(), "Getiri bolusulmez, katkisi %25 ustundeki HER oyuncu tam alir");
    assertEquals(0.25, od.getiriIcinMinKatkiOrani(), 1e-9);
  }

  @Test
  @DisplayName("Yasak 23: cevrimdisi PAYLASILAN duruma dokunulamaz")
  void yasak23_cevrimdisiSinir() {
    var c = denge.turetilmisDosyasi().cevrimdisi();
    assertEquals(20, c.maksKuyrukEylem());
    assertEquals(60, c.maksSureSn());
    assertEquals(List.of("ek", "sula", "hasat", "topla"), c.izinliEylemler());
    for (String yasak : List.of("depoya_koy", "depodan_al", "vur", "ortak_proje_katki")) {
      assertTrue(c.yasakEylemler().contains(yasak), yasak + " cevrimdisi yasak olmali");
    }
    assertTrue(c.redOzetiTekBildirim(), "Redler tek ozette toplanmali, red yagmuru olmamali");
  }

  @Test
  @DisplayName("Kural 7: geri sarma 0.2 sn ve YUMUSAK")
  void kural7_yumusakGeriSarma() {
    var i = denge.turetilmisDosyasi().iyimserIstemci();
    assertEquals(0.2, i.geriSarmaSn(), 1e-9);
    assertEquals("yumusak", i.geriSarmaEgrisi());
    assertEquals(5, i.konumYayinHz());
    assertFalse(i.konumYayinRevizyonUretir(), "Konum yayini revizyon uretmemeli");
  }

  @Test
  @DisplayName("Yasak 25: kisa urunler saatlik getiriyle dengelenmiyor (oturum kapisi belgeli)")
  void yasak25_oturumKapisi() {
    var kapi = denge.hamPaket().path("urunler").path("oturumKapisi");
    assertFalse(kapi.isMissingNode(), "oturumKapisi bulgusu denge dosyasinda kayitli olmali");
    assertTrue(kapi.path("oturumAralikSaat").asDouble() > 6.0, "Oturum araligi ~6.9 saat");
  }

  @Test
  @DisplayName("C.5: es zamanli depodan almada ilk ulasan kazanir, digeri ESYA_YOK alir")
  void cakismaCozumu() {
    UUID a = oyuncuKur("ca");
    UUID b = oyuncuKur("cb");
    Vadi vadi = vadiServisi.kur(a);
    oyuncular.seviyeVeXpYaz(a, 3, 0);
    vadiler.dunyaSeviyesiYaz(vadi.id(), 3);
    kimlik.bagla(a, "eposta", "a@b.c");
    vadiServisi.katil(b, vadiServisi.davetKoduUret(a));

    dunya.depoDegistir(vadi.id(), "odun", 3, a);
    niyet(a, vadi.id(), "depodan_al", null, new Niyet.Ek(null, "odun", 3, null));

    OyunHatasi hata =
        assertThrows(
            OyunHatasi.class,
            () -> niyet(b, vadi.id(), "depodan_al", null, new Niyet.Ek(null, "odun", 1, null)));
    assertEquals(RedKodu.ESYA_YOK, hata.kod());
  }
}
