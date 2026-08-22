package tr.amorland.oyun;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import tr.amorland.denge.DengeModelleri.Alet;
import tr.amorland.denge.DengeModelleri.Urun;
import tr.amorland.denge.DengeModelleri.YabaniKaynak;
import tr.amorland.denge.DengeServisi;
import tr.amorland.kimlik.Oyuncu;
import tr.amorland.kimlik.OyuncuDeposu;
import tr.amorland.ortak.Deterministik;
import tr.amorland.ortak.OyunHatasi;
import tr.amorland.ortak.RedKodu;
import tr.amorland.ortak.Saat;
import tr.amorland.oyun.DunyaModeli.*;
import tr.amorland.vadi.Vadi;
import tr.amorland.vadi.VadiDeposu;

import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * Oyunun kural motoru. Butun otorite burada (mimari kural 1).
 *
 * <p>Her niyet tek bir islemde su sirayla islenir:
 * <ol>
 *   <li>Idempotency: {@code clientActionId} daha once gorulduyse ayni sonuc doner
 *   <li>Vadi satiri kilitlenir, kurallar dogrulanir
 *   <li>Durum yazilir, revizyon artirilir, {@code revizyon_kaydi}'na islenir
 *   <li>Sonuc {@code eylem_kaydi}'na yazilir
 * </ol>
 *
 * <p>⚠ B.26 yasak 4: <b>esya asla yok edilmez, yere dusmez, otomatik satilmaz.</b>
 * Kapasite yetmiyorsa eylem <i>reddedilir</i>; urun karoda bekler.
 */
@Service
public class OyunServisi {

  private final DengeServisi denge;
  private final DunyaDeposu dunya;
  private final DunyaKurucu kurucu;
  private final DurumOkuyucu okuyucu;
  private final IlerlemeServisi ilerleme;
  private final RevizyonServisi revizyon;
  private final VadiDeposu vadiler;
  private final OyuncuDeposu oyuncular;
  private final ObjectMapper mapper;
  private final Saat saat;

  private final TrenServisi tren;

  public OyunServisi(
      DengeServisi denge,
      DunyaDeposu dunya,
      DunyaKurucu kurucu,
      DurumOkuyucu okuyucu,
      IlerlemeServisi ilerleme,
      RevizyonServisi revizyon,
      VadiDeposu vadiler,
      OyuncuDeposu oyuncular,
      ObjectMapper mapper,
      Saat saat,
      TrenServisi tren) {
    this.tren = tren;
    this.denge = denge;
    this.dunya = dunya;
    this.kurucu = kurucu;
    this.okuyucu = okuyucu;
    this.ilerleme = ilerleme;
    this.revizyon = revizyon;
    this.vadiler = vadiler;
    this.oyuncular = oyuncular;
    this.mapper = mapper;
    this.saat = saat;
  }

  // ================================================================== giris noktasi

  @Transactional
  public Degisim uygula(UUID oyuncuId, UUID vadiId, Niyet niyet) {
    if (niyet.clientActionId() == null) {
      throw new OyunHatasi(RedKodu.BILINMEYEN, "client_action_id zorunlu (mimari kural 4).");
    }
    Optional<Degisim> onceki = revizyon.oncekiSonuc(niyet.clientActionId());
    if (onceki.isPresent()) return onceki.get();

    Vadi vadi =
        vadiler
            .bulKilitli(vadiId)
            .orElseThrow(() -> new OyunHatasi(RedKodu.BILINMEYEN, "Vadi yok."));
    // Kilit ALTINDA ikinci bakis (mimari kural 4): ayni client_action_id ile
    // yarisan iki istekte ikincisi ilkinin commit'inden sonra kilidi alir ve
    // sonucu burada gorur — kilitsiz ilk bakis yalniz hizli yoldur, guvence bu.
    onceki = revizyon.oncekiSonuc(niyet.clientActionId());
    if (onceki.isPresent()) return onceki.get();

    if (!vadi.uyeMi(oyuncuId)) {
      throw new OyunHatasi(RedKodu.UYE_DEGIL, "Bu vadinin uyesi degilsin.");
    }
    Oyuncu oyuncu = oyuncular.bul(oyuncuId).orElseThrow();
    Instant simdi = saat.simdi();
    DegisimYazici yazici = new DegisimYazici();

    int xp =
        switch (niyet.tip()) {
          case "ek" -> ek(vadi, oyuncu, niyet, simdi, yazici);
          case "sula" -> sula(vadi, oyuncu, niyet, simdi, yazici);
          case "hasat" -> hasat(vadi, oyuncu, niyet, simdi, yazici);
          case "topla" -> topla(vadi, oyuncu, niyet, simdi, yazici);
          case "kova_doldur" -> kovaDoldur(vadi, oyuncu, yazici);
          case "depoya_koy" -> depoyaKoy(vadi, oyuncu, niyet, yazici);
          case "depodan_al" -> depodanAl(vadi, oyuncu, niyet, yazici);
          case "depoya_bosalt" -> depoyaBosalt(vadi, oyuncu, yazici);
          case "depo_kilit" -> depoKilit(vadi, oyuncu, niyet, yazici);
          case "craft" -> craft(vadi, oyuncu, niyet, yazici);
          case "bolgeye_gir" -> bolgeyeGir(vadi, oyuncu, niyet, yazici);
          case "kusluk_baslat" -> kuslukBaslat(vadi, oyuncu, simdi, yazici);
          case "kusluk_bitir" -> kuslukBitir(vadi, yazici);
          case "tren_karsila" -> trenKarsila(vadi, oyuncu, niyet, simdi, yazici);
          case "sat" -> sat(vadi, oyuncu, niyet, yazici);
          case "satin_al" -> satinAl(vadi, oyuncu, niyet, yazici);
          default -> throw new OyunHatasi(RedKodu.BILINMEYEN, "Bilinmeyen niyet: " + niyet.tip());
        };

    if (xp > 0) {
      oyuncu = ilerleme.xpEkle(vadi, oyuncu, xp, yazici);
    }

    // Oyuncu ve vadi ozetini her zaman tazele: seviye/canta/depo dolulugu degismis olabilir.
    Vadi guncelVadi = vadiler.bul(vadi.id()).orElseThrow();
    yazici.oyuncu(okuyucu.oyuncuOzeti(guncelVadi, oyuncu, true));
    yazici.vadi(okuyucu.vadiOzeti(guncelVadi));

    long yeniRevizyon = vadiler.revizyonArtir(vadi.id());
    Degisim degisim = yazici.kapat(yeniRevizyon, simdi);
    revizyon.kaydet(vadi.id(), degisim);
    revizyon.eylemKaydet(niyet.clientActionId(), oyuncuId, vadi.id(), degisim);
    return degisim;
  }

  // ================================================================== tren + market (B.15)

  private void paraKapisi(Oyuncu oyuncu) {
    int esik = denge.ilerlemeDosyasi().paraninGirisi().ilkSeviye();
    if (oyuncu.seviye() < esik) {
      throw new OyunHatasi(
          RedKodu.SEVIYE_YETERSIZ, "Para Sv " + esik + "'te oyuna girer. Tren gelince gorusuruz.");
    }
  }

  /**
   * Tren siparisi karsilama (B.15). Kismi yatirma YOK: tam miktar elde olmali.
   * Ayni karta ikinci gelen SIPARIS_DOLDU alir (C.5 — birincil anahtar zorlar).
   * Manifesto 3/3 dolarsa IKI oyuncu da bonus alir — "iki imza" mekanigi.
   */
  private int trenKarsila(Vadi vadi, Oyuncu oyuncu, Niyet n, Instant simdi, DegisimYazici y) {
    paraKapisi(oyuncu);
    long sefer = tren.seferNo(simdi);
    // Niyet PANODAKI sefere baglidir: istemci sefer N'in kartini gorup basmisken
    // sunucu N+1'i hesaplarsa oyuncu hic gormedigi bir takasi onaylamis olurdu.
    // Sefer uyusmazligi sessiz kabul degil ACIK reddir.
    Long panoSeferi = n.ek() == null ? null : n.ek().sefer();
    if (panoSeferi != null && panoSeferi != sefer) {
      throw new OyunHatasi(
          RedKodu.GECERSIZ_HEDEF, "Tren kalkti — pano yenilendi, yeni seferi kontrol et.");
    }
    var siparisler = tren.siparisler(vadi, vadi.dunyaSeviyesi(), sefer);
    int kart = n.hedef() == null || n.hedef().x() == null ? -1 : n.hedef().x();
    if (kart < 0 || kart >= siparisler.size()) {
      throw new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Boyle bir siparis karti yok.");
    }
    TrenServisi.Siparis siparis = siparisler.get(kart);

    int elde = dunya.envanterAdet(vadi.id(), oyuncu.id(), siparis.urunId());
    if (elde < siparis.miktar()) {
      throw new OyunHatasi(
          RedKodu.KAYNAK_YETERSIZ,
          denge.esyaAdi(siparis.urunId()) + " " + elde + "/" + siparis.miktar()
              + " — siparis tam miktar ister, kismi yatirma yok.");
    }
    if (!dunya.trenKarsilamaYaz(vadi.id(), sefer, kart, oyuncu.id(), simdi)) {
      throw new OyunHatasi(RedKodu.SIPARIS_DOLDU, "Bu siparisi partnerin kapmis bile.");
    }

    envanterYaz(vadi, oyuncu.id(), siparis.urunId(), -siparis.miktar(), y);
    dunya.altinDegistir(vadi.id(), oyuncu.id(), siparis.altinOdul());
    y.olay(DunyaModeli.Olay.trenSiparisi(oyuncu.id(), kart));

    int xp = siparis.xp();
    xp += ilerleme.defterTetikle(vadi, oyuncu.id(), "bolge_acildi:gar", y);

    // Manifesto tamam mi? (3/3) — tam doldurma bonusu IKI imzaya da yazilir.
    if (dunya.trenKarsilananlar(vadi.id(), sefer).size()
        >= denge.urunlerDosyasi().manifesto().siparisKartiSayisi()) {
      int bonus = denge.urunlerDosyasi().manifesto().tamDoldurmaXpBonus();
      xp += bonus;
      y.olay(DunyaModeli.Olay.manifestoTamam());
      UUID digerId = vadi.digerOyuncu(oyuncu.id());
      if (digerId != null) {
        oyuncular.bul(digerId).ifPresent(p -> ilerleme.xpEkle(vadi, p, bonus, y));
      }
    }
    return xp;
  }

  /** Markete satis: ANINDA, dusuk ve SABIT oranla (B.15 — acil nakit muslugu). */
  private int sat(Vadi vadi, Oyuncu oyuncu, Niyet n, DegisimYazici y) {
    paraKapisi(oyuncu);
    String esyaId = n.ek() == null ? null : n.ek().esyaId();
    int adet = n.ek() == null || n.ek().adet() == null ? 1 : n.ek().adet();
    if (adet <= 0) throw new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Gecersiz adet.");
    Urun urun =
        denge
            .urun(esyaId)
            .orElseThrow(
                () -> new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Market yalnizca urun alir."));
    int elde = dunya.envanterAdet(vadi.id(), oyuncu.id(), esyaId);
    if (elde < adet) {
      throw new OyunHatasi(RedKodu.ESYA_YOK, denge.esyaAdi(esyaId) + " " + elde + "/" + adet);
    }
    var oran = denge.urunlerDosyasi().marketAlisOrani();
    long kazanc = (long) Math.floor(urun.tabanFiyat() * ((oran.min() + oran.max()) / 2.0) * adet);
    envanterYaz(vadi, oyuncu.id(), esyaId, -adet, y);
    dunya.altinDegistir(vadi.id(), oyuncu.id(), Math.max(1, kazanc));
    return 0;
  }

  /** Marketten tohum alimi. Fiyat SABIT (tohumFiyat); acilmamis urun satilmaz. */
  private int satinAl(Vadi vadi, Oyuncu oyuncu, Niyet n, DegisimYazici y) {
    paraKapisi(oyuncu);
    String esyaId = n.ek() == null ? null : n.ek().esyaId();
    int adet = n.ek() == null || n.ek().adet() == null ? 1 : n.ek().adet();
    if (esyaId == null || !esyaId.endsWith("_tohum") || adet <= 0) {
      throw new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Market simdilik yalnizca tohum satar.");
    }
    String urunId = esyaId.substring(0, esyaId.length() - "_tohum".length());
    Urun urun =
        denge
            .urun(urunId)
            .orElseThrow(() -> new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Bilinmeyen tohum."));
    if (urun.acilis() != null && oyuncu.seviye() < urun.acilis().seviye()) {
      throw new OyunHatasi(
          RedKodu.SEVIYE_YETERSIZ,
          denge.esyaAdi(urunId) + " Sv " + urun.acilis().seviye() + "'te acilir.");
    }
    if (Boolean.TRUE.equals(urun.tohumPenceresi()) && !tohumPenceresiAcikMi(vadi, urun, saat.simdi())) {
      throw new OyunHatasi(
          RedKodu.ESYA_YOK,
          denge.esyaAdi(urunId) + " tohumu su an tezgahta degil — gunun pencerelerinden birini bekle.");
    }
    long fiyat = (long) urun.tohumFiyat() * adet;
    long bakiye = dunya.altin(vadi.id(), oyuncu.id());
    if (bakiye < fiyat) {
      throw new OyunHatasi(
          RedKodu.KAYNAK_YETERSIZ, "Altin yetmiyor: " + bakiye + "/" + fiyat + ".");
    }
    cantayaEkle(vadi, oyuncu, esyaId, adet, y);
    dunya.altinDegistir(vadi.id(), oyuncu.id(), -fiyat);
    return 0;
  }

  /**
   * Tohum penceresi (B.11): pencereli urunlerin tohumu gunde N kez, S saniyelik
   * DETERMINISTIK pencerelerde satilir — safranin fiyat primi bu arz kisitindan
   * gelir. Gun esit dilimlere bolunur; her dilimde pencerenin baslangici
   * vadi.tohum'dan turetilir (C tablosu: tohum "fiyat ve PENCERE uretiminin
   * girdisi"). Cron yok: okuma aninda hesaplanir (mimari kural 2).
   */
  public boolean tohumPenceresiAcikMi(Vadi vadi, Urun urun, Instant simdi) {
    var kural = denge.urunlerDosyasi().tohumPenceresiKurali();
    long gunSn = 86400L;
    long dilimSn = gunSn / kural.gunlukPencereSayisi();
    long gun = simdi.getEpochSecond() / gunSn;
    long gunIci = simdi.getEpochSecond() % gunSn;
    int dilim = (int) (gunIci / dilimSn);
    double kayma =
        Deterministik.hash01(vadi.tohum(), "tohumPencere", urun.id(), gun, dilim);
    long baslangic = dilim * dilimSn + (long) (kayma * (dilimSn - kural.pencereSuresiSn()));
    return gunIci >= baslangic && gunIci < baslangic + kural.pencereSuresiSn();
  }

  // ================================================================== tarla

  /** Bos toprak + tohum secili -> capala + ek. TEK eylem, ayri capa yok (B.6). */
  private int ek(Vadi vadi, Oyuncu oyuncu, Niyet n, Instant simdi, DegisimYazici y) {
    Karo karo = kendiKarosu(vadi, oyuncu, n);
    if (!karo.bosMu()) {
      throw new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Karo bos degil.");
    }
    String urunId = n.ek() == null ? null : n.ek().urunId();
    Urun urun =
        denge
            .urun(urunId)
            .orElseThrow(() -> new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Bilinmeyen urun: " + urunId));
    if (oyuncu.seviye() < urun.acilis().seviye()) {
      throw OyunHatasi.seviyeYetersiz(urun.ad(), urun.acilis().seviye());
    }

    String tohum = urun.tohumEsyaId();
    if (dunya.envanterAdet(vadi.id(), oyuncu.id(), tohum) < 1) {
      throw new OyunHatasi(RedKodu.KAYNAK_YETERSIZ, urun.ad() + " tohumun yok.");
    }
    envanterYaz(vadi, oyuncu.id(), tohum, -1, y);

    Karo yeni =
        new Karo(karo.parselSahibi(), karo.x(), karo.y(), urun.id(), simdi, null, 0, null, false);
    dunya.karoYaz(vadi.id(), yeni);
    y.karo(yeni);

    return ilerleme.ilkKezAni(vadi, oyuncu.id(), "ilk_ekim", y);
  }

  /**
   * Sulama. Buyume sayaci burada baslar (turetilmis.json: sulama).
   * "Tarlada biri eker, biri sular" — mekanik olarak da boyle.
   */
  private int sula(Vadi vadi, Oyuncu oyuncu, Niyet n, Instant simdi, DegisimYazici y) {
    Karo karo = kendiKarosu(vadi, oyuncu, n);
    if (karo.bosMu()) {
      throw new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Karoda ekili urun yok.");
    }
    if (karo.sulandiAt() != null) {
      throw new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Bu karo zaten sulanmis.");
    }
    int kalan = dunya.kovaKalan(vadi.id(), oyuncu.id());
    if (kalan <= 0) {
      throw new OyunHatasi(RedKodu.KAYNAK_YETERSIZ, "Kova bos. Kuyudan doldur.");
    }
    dunya.kovaYaz(vadi.id(), oyuncu.id(), kalan - 1);

    Karo yeni =
        new Karo(
            karo.parselSahibi(), karo.x(), karo.y(), karo.urunId(),
            karo.ekildiAt(), simdi, karo.hasatSayaci(), karo.sonHasatAt(), karo.gubreli());
    dunya.karoYaz(vadi.id(), yeni);
    y.karo(yeni);

    return ilerleme.ilkKezAni(vadi, oyuncu.id(), "ilk_sulama", y);
  }

  private int hasat(Vadi vadi, Oyuncu oyuncu, Niyet n, Instant simdi, DegisimYazici y) {
    Karo karo = kendiKarosu(vadi, oyuncu, n);
    if (karo.bosMu()) {
      throw new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Karoda ekili urun yok.");
    }
    Urun urun = denge.urun(karo.urunId()).orElseThrow();

    Instant olgunluk = olgunlukAni(karo, urun);
    if (olgunluk == null) {
      throw new OyunHatasi(RedKodu.HENUZ_OLGUN_DEGIL, "Once sula: sulanmayan urun buyumez.");
    }
    if (simdi.isBefore(olgunluk)) {
      throw new OyunHatasi(
          RedKodu.HENUZ_OLGUN_DEGIL,
          "Olgunlasmasina " + Duration.between(simdi, olgunluk).toSeconds() + " sn var.");
    }

    // Depo dolu ise hasat ENGELLENIR; urun karoda bekler, YOK EDILMEZ (B.10, B.26 yasak 4).
    depoDoluysaReddet(vadi, y);

    int verim = urun.verim() + (karo.gubreli() ? denge.urunlerDosyasi().gubre().etki().verimEk() : 0);
    cantayaEkle(vadi, oyuncu, urun.id(), verim, y);

    // Baslangic urunleri 1 urun + 1 TOHUM verir (B.11). Market urunleri vermez.
    if (urun.tohumGeriVerirMi()) {
      cantayaEkle(vadi, oyuncu, urun.tohumEsyaId(), 1, y);
    }

    int sayac = karo.hasatSayaci() + 1;
    Karo yeni;
    if (sayac < urun.toplamHasatSayisi()) {
      // Yeniden veren urun: karo dolu kalir, yeni cevrim icin YENIDEN SULANMALI.
      yeni =
          new Karo(
              karo.parselSahibi(), karo.x(), karo.y(), karo.urunId(),
              karo.ekildiAt(), null, sayac, simdi, false);
    } else {
      // Son hasattan sonra karo bosalir (D: yenidenVerirKurali.sonHasattanSonra).
      yeni = new Karo(karo.parselSahibi(), karo.x(), karo.y(), null, null, null, 0, null, false);
    }
    dunya.karoYaz(vadi.id(), yeni);
    y.karo(yeni);

    int xp = denge.hasatXp(urun);
    xp += ilerleme.ilkKezAni(vadi, oyuncu.id(), "ilk_hasat", y);
    xp += ilerleme.defterTetikle(vadi, oyuncu.id(), "ilk_hasat", y);
    return xp;
  }

  /**
   * Olgunlasma ani. Sulanmamis karo icin {@code null} (buyume duruyor).
   *
   * <p>Cron yok (mimari kural 2): tek bir toplama islemi, okuma aninda.
   */
  public Instant olgunlukAni(Karo karo, Urun urun) {
    if (karo.sulandiAt() == null) return null;
    long sure =
        karo.hasatSayaci() == 0
            ? urun.buyumeSn()
            : (urun.yenidenVerirMi() ? urun.yenidenVerir().araSn() : urun.buyumeSn());
    return karo.sulandiAt().plusSeconds(sure);
  }

  // ================================================================== yabani kaynak

  /**
   * Yabani kaynak toplama. Alet gerektirmez — bu, alet kilitlenmesine karsi
   * guvenlik aginin birinci ayagidir (B.14 degismezi).
   */
  private int topla(Vadi vadi, Oyuncu oyuncu, Niyet n, Instant simdi, DegisimYazici y) {
    var satir =
        dunya
            .dugumKilitli(vadi.id(), n.bolge(), n.x(), n.y())
            .orElseThrow(() -> new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Burada kaynak yok."));
    if (!"yabani".equals(satir.sinif())) {
      throw new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Bu kaynak elle toplanmaz.");
    }
    YabaniKaynak kaynak =
        denge
            .yabaniKaynak(satir.tur())
            .orElseThrow(() -> new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Bilinmeyen kaynak."));

    if (satir.kirildiAt() != null) {
      Instant hazir = satir.kirildiAt().plusSeconds(kaynak.yenilenmeSn());
      if (simdi.isBefore(hazir)) {
        throw new OyunHatasi(RedKodu.DUGUM_TUKENDI, "Bu kaynak henuz yenilenmedi.");
      }
    }

    // Cevrim numarasi getiriyi deterministik yapar: tekrar gonderimde ayni sonuc.
    long cevrim = satir.kirildiAt() == null ? 0 : satir.kirildiAt().getEpochSecond();
    for (var g : kaynak.getiri()) {
      int adet =
          Deterministik.aralik(
              g.enAz(), g.enCok(), vadi.tohum(), satir.bolge(), satir.x(), satir.y(), cevrim, g.id());
      cantayaEkle(vadi, oyuncu, g.id(), adet, y);
    }

    dunya.dugumYaz(
        vadi.id(), satir.bolge(), satir.x(), satir.y(), 0, satir.maksSaglik(), simdi, satir.katkiJson());
    dunya
        .dugumKilitli(vadi.id(), satir.bolge(), satir.x(), satir.y())
        .ifPresent(s -> y.dugum(okuyucu.dugumeCevir(s)));

    return kaynak.xp();
  }

  // ================================================================== kuyu ve kova

  /** Kuyu tarla sahnesinin icindedir; sahne degistirmek yok (B.11). */
  private int kovaDoldur(Vadi vadi, Oyuncu oyuncu, DegisimYazici y) {
    int kapasite = okuyucu.kovaKapasitesi(vadi.id(), oyuncu.id());
    if (kapasite <= 0) {
      throw new OyunHatasi(RedKodu.ALET_YOK, "Once tezgahta ahsap kova yap (8 odun).");
    }
    dunya.kovaYaz(vadi.id(), oyuncu.id(), kapasite);
    return 0;
  }

  // ================================================================== depo

  /**
   * Depoya koyma. Kapasite TOPLAM MIKTARDIR (B.10); yigin sayisi degil.
   * Kismi kabul edilir: 30 birim yer varken 50 koymaya calisirsan 30'u girer.
   */
  private int depoyaKoy(Vadi vadi, Oyuncu oyuncu, Niyet n, DegisimYazici y) {
    depoAcikMi(vadi);
    String esyaId = gerekliEsya(n);
    int istenen = n.ek().adet() == null ? Integer.MAX_VALUE : n.ek().adet();
    int elde = dunya.envanterAdet(vadi.id(), oyuncu.id(), esyaId);
    if (elde <= 0) throw new OyunHatasi(RedKodu.ESYA_YOK, "Cantanda bu esyadan yok.");

    int bosluk = denge.depoKapasitesi(vadi.dunyaSeviyesi()) - dunya.depoDolu(vadi.id());
    if (bosluk <= 0) throw OyunHatasi.depoDolu(denge.depoKapasitesi(vadi.dunyaSeviyesi()), dunya.depoDolu(vadi.id()));

    int adet = Math.min(Math.min(istenen, elde), bosluk);
    envanterYaz(vadi, oyuncu.id(), esyaId, -adet, y);
    dunya.depoDegistir(vadi.id(), esyaId, adet, oyuncu.id());
    y.depo(depoSatiriOku(vadi, esyaId));

    return ilerleme.ilkKezAni(vadi, oyuncu.id(), "ilk_depoya_bosaltma", y);
  }

  /** "Depoya bosalt" tek butonu — bu buton olmadan oyun mobilde iskence (B.10). */
  private int depoyaBosalt(Vadi vadi, Oyuncu oyuncu, DegisimYazici y) {
    depoAcikMi(vadi);
    int kapasite = denge.depoKapasitesi(vadi.dunyaSeviyesi());
    int bosluk = kapasite - dunya.depoDolu(vadi.id());
    if (bosluk <= 0) throw OyunHatasi.depoDolu(kapasite, dunya.depoDolu(vadi.id()));

    // Tohum ve alet depoya gitmez: oyuncunun ekmeye devam edebilmesi icin cantada kalir.
    var envanter = dunya.envanterHaritasi(vadi.id(), oyuncu.id());
    boolean birSeyGitti = false;
    for (var giris : envanter.entrySet()) {
      if (bosluk <= 0) break;
      String esyaId = giris.getKey();
      String kategori = denge.esyaKategorisi(esyaId);
      if ("tohum".equals(kategori) || "alet".equals(kategori)) continue;

      int adet = Math.min(giris.getValue(), bosluk);
      if (adet <= 0) continue;
      envanterYaz(vadi, oyuncu.id(), esyaId, -adet, y);
      dunya.depoDegistir(vadi.id(), esyaId, adet, oyuncu.id());
      y.depo(depoSatiriOku(vadi, esyaId));
      bosluk -= adet;
      birSeyGitti = true;
    }
    if (!birSeyGitti) throw new OyunHatasi(RedKodu.ESYA_YOK, "Depoya gidecek bir sey yok.");
    return ilerleme.ilkKezAni(vadi, oyuncu.id(), "ilk_depoya_bosaltma", y);
  }

  /**
   * Depodan alma. Es zamanli almada "sunucuya ilk ulasan kazanir", digeri
   * {@code ESYA_YOK} alir (C.5). Satir kilidi bunu garanti eder.
   *
   * <p>Toplu "depodan al" butonu YOKTUR (B.10): kolaylik tek yonludur.
   */
  private int depodanAl(Vadi vadi, Oyuncu oyuncu, Niyet n, DegisimYazici y) {
    depoAcikMi(vadi);
    String esyaId = gerekliEsya(n);
    var satir =
        dunya
            .depoSatiriKilitli(vadi.id(), esyaId)
            .orElseThrow(() -> new OyunHatasi(RedKodu.ESYA_YOK, "Depoda bu esyadan yok."));

    if (satir.kilitli() && !oyuncu.id().equals(satir.kilitleyen())) {
      throw new OyunHatasi(RedKodu.KILITLI, "Partner bu yigini kilitlemis.");
    }
    int istenen = n.ek().adet() == null ? satir.adet() : Math.min(n.ek().adet(), satir.adet());
    if (istenen <= 0) throw new OyunHatasi(RedKodu.ESYA_YOK, "Depoda bu esyadan yok.");

    cantayaEkle(vadi, oyuncu, esyaId, istenen, y);
    dunya.depoDegistir(vadi.id(), esyaId, -istenen, satir.koyan());
    y.depo(depoSatiriOku(vadi, esyaId));
    return 0;
  }

  /** Oyuncu en fazla 3 yigin kilitleyebilir. Sinir kasitli: yoksa depo ikiye bolunur (B.10). */
  private int depoKilit(Vadi vadi, Oyuncu oyuncu, Niyet n, DegisimYazici y) {
    depoAcikMi(vadi);
    String esyaId = gerekliEsya(n);
    var satir =
        dunya
            .depoSatiri(vadi.id(), esyaId)
            .orElseThrow(() -> new OyunHatasi(RedKodu.ESYA_YOK, "Depoda bu esyadan yok."));

    boolean kilitle = !satir.kilitli();
    if (satir.kilitli() && !oyuncu.id().equals(satir.kilitleyen())) {
      throw new OyunHatasi(RedKodu.KILITLI, "Bu kilidi partnerin koydu.");
    }
    int maks = denge.turetilmisDosyasi().depoSiralama().maksKilitliYigin();
    if (kilitle && dunya.depoKilitliSayisi(vadi.id(), oyuncu.id()) >= maks) {
      throw new OyunHatasi(RedKodu.KILITLI, "En fazla " + maks + " yigin kilitleyebilirsin.");
    }
    dunya.depoKilit(vadi.id(), esyaId, kilitle, oyuncu.id());
    y.depo(depoSatiriOku(vadi, esyaId));
    return 0;
  }

  // ================================================================== craft

  /**
   * Tezgahta uretim. Sv 1'de basit tezgah tas kazma, tas balta ve ahsap kova verir —
   * alet kilitlenmesine karsi guvenlik aginin ikinci ayagi (B.14).
   */
  private int craft(Vadi vadi, Oyuncu oyuncu, Niyet n, DegisimYazici y) {
    String tarifId = n.ek() == null ? null : n.ek().tarifId();
    Alet alet =
        denge
            .alet(tarifId)
            .orElseThrow(
                () -> new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Bu tarif henuz yok: " + tarifId));

    if (oyuncu.seviye() < alet.craftSeviye()) {
      throw OyunHatasi.seviyeYetersiz(alet.id(), alet.craftSeviye());
    }
    String yapi = tarifiVerenYapi(alet);
    if (!denge.evYapisiAcikMi(yapi, vadi.dunyaSeviyesi())) {
      throw new OyunHatasi(RedKodu.SEVIYE_YETERSIZ, yapi + " henuz acilmadi.");
    }

    for (var m : alet.malzeme()) {
      if (dunya.envanterAdet(vadi.id(), oyuncu.id(), m.id()) < m.adet()) {
        throw new OyunHatasi(
            RedKodu.KAYNAK_YETERSIZ,
            m.adet() + " " + denge.esyaAdi(m.id()) + " gerekli.");
      }
    }
    for (var m : alet.malzeme()) {
      envanterYaz(vadi, oyuncu.id(), m.id(), -m.adet(), y);
    }

    int dayaniklilik = alet.dayaniklilik() == null ? 0 : alet.dayaniklilik();
    dunya.aletYaz(vadi.id(), oyuncu.id(), alet.id(), dayaniklilik);
    y.alet(new DunyaModeli.Alet(oyuncu.id(), alet.id(), dayaniklilik, dayaniklilik));

    int xp = ilerleme.craftXp(vadi, oyuncu.id(), alet.id(), y);
    xp += ilerleme.ilkKezAni(vadi, oyuncu.id(), "ilk_craft", y);
    return xp;
  }

  private String tarifiVerenYapi(Alet alet) {
    for (var yapi : denge.ilerlemeDosyasi().evYapilari()) {
      if (yapi.tarifler() != null && yapi.tarifler().contains(alet.id())) return yapi.id();
    }
    return "tezgah_yukseltme";
  }

  // ================================================================== bolge

  /** Bolgeye giris. Ilk giriste 50 XP (ev haric) ve defter sayfasi tetiklenebilir. */
  private int bolgeyeGir(Vadi vadi, Oyuncu oyuncu, Niyet n, DegisimYazici y) {
    String bolgeId = n.bolge();
    var bolge =
        denge
            .bolge(bolgeId)
            .orElseThrow(() -> new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Bilinmeyen bolge."));
    if (vadi.dunyaSeviyesi() < bolge.acilisSeviye()) {
      throw OyunHatasi.seviyeYetersiz(bolge.ad(), bolge.acilisSeviye());
    }
    return ilerleme.bolgeyeIlkGiris(vadi, oyuncu.id(), bolgeId, y);
  }

  // ================================================================== kusluk

  /**
   * Kusluk (B.8) — SADECE manuel, ekonomik etkisi YOK, bildirim GONDERMEZ.
   *
   * <p>⚠ Kirmizi cizgi: hareketsizlik asla kusluk tetiklemez. "Partnerin 3 gundur
   * girmedi, kus duruma gectiniz" gercek bir iliskide gercek suculuk uretir.
   * Kusluk ciftin oynadigi bir ROL olmali, oyunun uyguladigi bir yaptirim degil.
   */
  private int kuslukBaslat(Vadi vadi, Oyuncu oyuncu, Instant simdi, DegisimYazici y) {
    if (!vadi.ciftMi()) throw new OyunHatasi(RedKodu.BILINMEYEN, "Partner yok.");
    dunya.kuslukBaslat(vadi.id(), oyuncu.id(), simdi);
    return 0;
  }

  private int kuslukBitir(Vadi vadi, DegisimYazici y) {
    dunya.kuslukBitir(vadi.id());
    return 0;
  }

  // ================================================================== yardimcilar

  /** Hedef karoyu getirir ve parsel sahipligini dogrular (B.7: kimse digerine dokunamaz). */
  private Karo kendiKarosu(Vadi vadi, Oyuncu oyuncu, Niyet n) {
    UUID parsel = n.hedef() != null && n.hedef().parsel() != null ? n.hedef().parsel() : oyuncu.id();
    if (!parsel.equals(oyuncu.id())) {
      throw new OyunHatasi(RedKodu.BASKA_PARSEL, "Bu parsel partnerin. Dokunamazsin.");
    }
    return dunya
        .karoKilitli(vadi.id(), parsel, n.x(), n.y())
        .orElseThrow(() -> new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Boyle bir karo yok."));
  }

  private String gerekliEsya(Niyet n) {
    String esyaId = n.ek() == null ? null : n.ek().esyaId();
    if (esyaId == null || !denge.esyaVarMi(esyaId)) {
      throw new OyunHatasi(RedKodu.GECERSIZ_HEDEF, "Bilinmeyen esya: " + esyaId);
    }
    return esyaId;
  }

  private void depoAcikMi(Vadi vadi) {
    if (!denge.bolgeAcikMi("depo", vadi.dunyaSeviyesi())) {
      throw OyunHatasi.seviyeYetersiz("Depo", denge.bolge("depo").orElseThrow().acilisSeviye());
    }
  }

  /**
   * Depo doluysa hasati reddeder.
   *
   * <p>⚠ Depo Sv 2'de acilir; Sv 1'de kapasite 0'dir. Kural harfiyen uygulansaydi
   * 0 >= 0 olur ve <b>ogreticideki ilk hasat engellenirdi.</b> O yuzden kisit
   * yalnizca depo acikken calisir (turetilmis.json: depoKisitiKurali).
   */
  private void depoDoluysaReddet(Vadi vadi, DegisimYazici y) {
    int kapasite = denge.depoKapasitesi(vadi.dunyaSeviyesi());
    if (kapasite <= 0) return;
    if (!denge.ilerlemeDosyasi().depo().doluKurali().hasatEngellenir()) return;
    int dolu = dunya.depoDolu(vadi.id());
    if (dolu >= kapasite) {
      y.olay(Olay.depoDoldu());
      throw OyunHatasi.depoDolu(kapasite, dolu);
    }
  }

  /** Cantaya ekler. Slot yetmezse CANTA_DOLU; esya asla yok edilmez (B.26 yasak 4). */
  private void cantayaEkle(Vadi vadi, Oyuncu oyuncu, String esyaId, int adet, DegisimYazici y) {
    if (adet <= 0) return;
    int yigin = denge.yiginBoyutu();
    int mevcut = dunya.envanterAdet(vadi.id(), oyuncu.id(), esyaId);
    int simdikiSlot = dunya.envanterSlotSayisi(vadi.id(), oyuncu.id(), yigin);
    int buEsyaninSlotu = (int) Math.ceil(mevcut / (double) yigin);
    int yeniSlot = (int) Math.ceil((mevcut + adet) / (double) yigin);
    int gereken = simdikiSlot - buEsyaninSlotu + yeniSlot;

    if (gereken > denge.cantaSlot(oyuncu.seviye())) {
      throw OyunHatasi.cantaDolu(denge.cantaSlot(oyuncu.seviye()));
    }
    envanterYaz(vadi, oyuncu.id(), esyaId, adet, y);
  }

  private void envanterYaz(Vadi vadi, UUID sahip, String esyaId, int delta, DegisimYazici y) {
    int yeni = dunya.envanterDegistir(vadi.id(), sahip, esyaId, delta);
    y.envanter(new EnvanterSatiri(sahip, esyaId, yeni));
  }

  private DepoSatiri depoSatiriOku(Vadi vadi, String esyaId) {
    return dunya
        .depoSatiri(vadi.id(), esyaId)
        .orElse(new DepoSatiri(esyaId, 0, null, false, null));
  }

  // ================================================================== okuma

  public Degisim anlikGoruntu(Vadi vadi, java.util.Set<UUID> cevrimiciler) {
    return okuyucu.anlikGoruntu(vadi, cevrimiciler);
  }

  public List<Degisim> deltalarVeyaTam(
      Vadi vadi, long sonBilinen, java.util.Set<UUID> cevrimiciler) {
    var deltalar = revizyon.deltalar(vadi.id(), sonBilinen, vadi.revision());
    if (deltalar.isEmpty()) {
      return List.of(anlikGoruntu(vadi, cevrimiciler));
    }
    return deltalar.get();
  }
}
