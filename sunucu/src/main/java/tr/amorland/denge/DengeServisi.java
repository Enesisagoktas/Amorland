package tr.amorland.denge;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.JsonNodeFactory;
import com.fasterxml.jackson.databind.node.ObjectNode;
import jakarta.annotation.PostConstruct;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.io.ClassPathResource;
import org.springframework.stereotype.Service;
import tr.amorland.denge.DengeModelleri.*;

import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.HexFormat;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;

/**
 * Denge yapilandirmasini yukler, dogrular ve sorgulanabilir hale getirir.
 *
 * <p>Mimari kural 8: denge sayilari koda gomulmez. Sunucu acilirken JSON'lar okunur;
 * bir tutarsizlik varsa sunucu <b>acilmaz</b>. A.6 dogrulama kurali: sessizce dogru
 * gorunen hata en pahali hatadir, o yuzden acilista olculur.
 */
@Service
public class DengeServisi {

  private static final Logger log = LoggerFactory.getLogger(DengeServisi.class);

  private static final String DIZIN = "denge/";
  private static final List<String> DOSYALAR =
      List.of("urunler.json", "uretim.json", "ilerleme.json", "turetilmis.json");

  private final ObjectMapper mapper;

  private UrunlerDosyasi urunler;
  private UretimDosyasi uretim;
  private IlerlemeDosyasi ilerleme;
  private TuretilmisDosyasi turetilmis;

  /** Istemciye aynen gonderilen ham JSON agaci. */
  private JsonNode hamPaket;
  /** Ham paketin SHA-256'sinin ilk 16 hanesi. ETag ve surum etiketi olarak kullanilir. */
  private String surumEtiketi;

  private Map<String, Urun> urunIndeksi = Map.of();
  private Map<String, Urun> tohumIndeksi = Map.of();
  private Map<String, Alet> aletIndeksi = Map.of();
  private Map<String, Bolge> bolgeIndeksi = Map.of();
  private Map<String, EvYapisi> evYapiIndeksi = Map.of();
  private Map<String, YabaniKaynak> yabaniIndeksi = Map.of();
  private Map<String, String> esyaKategorileri = Map.of();
  private Map<String, String> esyaAdlari = Map.of();

  public DengeServisi(ObjectMapper mapper) {
    this.mapper = mapper;
  }

  @PostConstruct
  public void yukle() {
    try {
      Map<String, JsonNode> agaclar = new LinkedHashMap<>();
      for (String dosya : DOSYALAR) {
        agaclar.put(dosya.replace(".json", ""), oku(dosya));
      }
      // Belgeleme anahtarlari ("$aciklama", "$kaynak", "not") Map<String,Integer> gibi
      // alanlarda deserializasyonu bozar. Tipli okuma icin temizlenmis kopya kullanilir;
      // istemciye giden ham paket dokunulmadan kalir.
      Map<String, JsonNode> temiz = new LinkedHashMap<>();
      agaclar.forEach((ad, agac) -> temiz.put(ad, belgeAnahtarlariniAt(agac)));

      this.urunler = mapper.treeToValue(temiz.get("urunler"), UrunlerDosyasi.class);
      this.uretim = mapper.treeToValue(temiz.get("uretim"), UretimDosyasi.class);
      this.ilerleme = mapper.treeToValue(temiz.get("ilerleme"), IlerlemeDosyasi.class);
      this.turetilmis = mapper.treeToValue(temiz.get("turetilmis"), TuretilmisDosyasi.class);

      this.hamPaket = mapper.valueToTree(agaclar);
      this.surumEtiketi = ozet(mapper.writeValueAsBytes(this.hamPaket));

      indeksle();
      DenetimSonucu sonuc = denetle();
      for (String u : sonuc.uyarilar()) {
        log.warn("DENGE UYARISI: {}", u);
      }
      if (!sonuc.hatalar().isEmpty()) {
        throw new IllegalStateException(
            "Denge dosyalari tutarsiz (" + sonuc.hatalar().size() + " hata):\n  - "
                + String.join("\n  - ", sonuc.hatalar()));
      }
      log.info(
          "Denge yuklendi: {} urun, {} alet, {} bolge, {} yabani kaynak. Surum etiketi {}",
          urunIndeksi.size(), aletIndeksi.size(), bolgeIndeksi.size(), yabaniIndeksi.size(),
          surumEtiketi);
    } catch (IOException e) {
      throw new IllegalStateException("Denge dosyalari okunamadi", e);
    }
  }

  private JsonNode oku(String ad) throws IOException {
    ClassPathResource kaynak = new ClassPathResource(DIZIN + ad);
    try (InputStream in = kaynak.getInputStream()) {
      return mapper.readTree(in);
    }
  }

  /**
   * Belgeleme anahtarlarini ("$" ile baslayanlar ve "not") agactan siler.
   * Spec JSON'lari bu alanlari her seviyede tasir; tipli okumada gurultu yaparlar.
   */
  private static JsonNode belgeAnahtarlariniAt(JsonNode dugum) {
    if (dugum.isObject()) {
      ObjectNode kopya = JsonNodeFactory.instance.objectNode();
      Iterator<Map.Entry<String, JsonNode>> it = dugum.fields();
      while (it.hasNext()) {
        Map.Entry<String, JsonNode> alan = it.next();
        String ad = alan.getKey();
        if (ad.startsWith("$") || "not".equals(ad)) continue;
        kopya.set(ad, belgeAnahtarlariniAt(alan.getValue()));
      }
      return kopya;
    }
    if (dugum.isArray()) {
      ArrayNode kopya = JsonNodeFactory.instance.arrayNode();
      dugum.forEach(e -> kopya.add(belgeAnahtarlariniAt(e)));
      return kopya;
    }
    return dugum;
  }

  private static String ozet(byte[] veri) {
    try {
      MessageDigest md = MessageDigest.getInstance("SHA-256");
      return HexFormat.of().formatHex(md.digest(veri)).substring(0, 16);
    } catch (Exception e) {
      throw new IllegalStateException(e);
    }
  }

  private void indeksle() {
    Map<String, Urun> u = new LinkedHashMap<>();
    Map<String, Urun> t = new LinkedHashMap<>();
    for (Urun urun : urunler.urunler()) {
      u.put(urun.id(), urun);
      t.put(urun.tohumEsyaId(), urun);
    }
    urunIndeksi = Collections.unmodifiableMap(u);
    tohumIndeksi = Collections.unmodifiableMap(t);

    Map<String, Alet> a = new LinkedHashMap<>();
    for (Alet alet : uretim.aletler()) a.put(alet.id(), alet);
    aletIndeksi = Collections.unmodifiableMap(a);

    Map<String, Bolge> b = new LinkedHashMap<>();
    for (Bolge bolge : ilerleme.bolgeler()) b.put(bolge.id(), bolge);
    bolgeIndeksi = Collections.unmodifiableMap(b);

    Map<String, EvYapisi> y = new LinkedHashMap<>();
    for (EvYapisi yapi : ilerleme.evYapilari()) y.put(yapi.id(), yapi);
    evYapiIndeksi = Collections.unmodifiableMap(y);

    Map<String, YabaniKaynak> yk = new LinkedHashMap<>();
    for (YabaniKaynak k : uretim.yabaniKaynaklar().kaynaklar()) yk.put(k.id(), k);
    yabaniIndeksi = Collections.unmodifiableMap(yk);

    Map<String, String> kat = new LinkedHashMap<>();
    Map<String, String> ad = new LinkedHashMap<>();
    for (Urun urun : urunler.urunler()) {
      kat.put(urun.id(), "urun");
      ad.put(urun.id(), urun.ad());
      kat.put(urun.tohumEsyaId(), "tohum");
      ad.put(urun.tohumEsyaId(), urun.ad() + " tohumu");
    }
    for (EsyaTanimi e : turetilmis.esyalar().hamMadde()) {
      kat.put(e.id(), e.kategori());
      ad.put(e.id(), e.ad());
    }
    for (EsyaTanimi e : turetilmis.esyalar().islenmis()) {
      kat.put(e.id(), e.kategori());
      ad.put(e.id(), e.ad());
    }
    for (Alet alet : uretim.aletler()) {
      kat.put(alet.id(), "alet");
      ad.put(alet.id(), alet.id());
    }
    esyaKategorileri = Collections.unmodifiableMap(kat);
    esyaAdlari = Collections.unmodifiableMap(ad);
  }

  // ------------------------------------------------------------------ dogrulama

  /** Denetim ciktisi: hatalar sunucuyu acmaz, uyarilar sadece loglanir. */
  public record DenetimSonucu(List<String> hatalar, List<String> uyarilar) {}

  /**
   * A.6: tahmin etme, olc. Sunucu acilirken denge dosyalarinin ic tutarliligi denetlenir.
   *
   * <p>Ayrim kasitli: <b>oynanisi bozan</b> tutarsizlik hatadir ve sunucuyu acmaz;
   * yuvarlama sapmasi gibi <b>etkisiz</b> farklar uyaridir ve sadece loglanir.
   * Yoksa 1 XP'lik bir sapma tum sunucuyu durdururdu.
   */
  public DenetimSonucu denetle() {
    List<String> hata = new ArrayList<>();
    List<String> uyari = new ArrayList<>();

    // 1) Seviye esikleri formulle uyusmali: round(100 * 1.2^(n-1))
    //    +-1 sapma yuvarlama farkidir, uyari. Daha buyugu hatadir.
    Map<String, Integer> esikler = ilerleme.seviye().esikler();
    int toplam = 0;
    for (int n = 1; n < ilerleme.seviye().maks(); n++) {
      int beklenen = (int) Math.round(100 * Math.pow(1.2, n - 1));
      Integer gercek = esikler.get(String.valueOf(n));
      if (gercek == null) {
        hata.add("Seviye esigi eksik: " + n);
        continue;
      }
      if (gercek != beklenen) {
        String mesaj =
            "Seviye esigi " + n + " -> " + (n + 1) + " = " + gercek + ", formul " + beklenen + " diyor";
        if (Math.abs(gercek - beklenen) <= 1) uyari.add(mesaj + " (yuvarlama sapmasi, oynanisa etkisi yok)");
        else hata.add(mesaj);
      }
      toplam += gercek;
    }
    if (toplam != ilerleme.seviye().toplamXp()) {
      uyari.add(
          "Toplam XP " + ilerleme.seviye().toplamXp() + " yazili ama esikler toplami " + toplam
              + " (fark " + (toplam - ilerleme.seviye().toplamXp()) + ")");
    }

    // 2) Craft tarif sayisi siniri (B.26 yasak 10)
    CraftTarifleri c = uretim.craftTarifleri();
    int sayilan =
        uretim.aletler().size()
            + c.araMalzeme().size()
            + c.isleme().size()
            + c.yapi().size()
            + 6; // ev esyasi: adet olarak tanimli, kalem listesi yok
    if (sayilan != c.sinir()) {
      hata.add("Craft tarifi sayisi " + sayilan + ", sinir " + c.sinir() + " (B.26 yasak 10)");
    }

    // 3) Alet malzemeleri bilinen esya olmali
    for (Alet alet : uretim.aletler()) {
      for (Malzeme m : alet.malzeme()) {
        if (!esyaKategorileri.containsKey(m.id())) {
          hata.add("Alet " + alet.id() + " bilinmeyen malzeme istiyor: " + m.id());
        }
      }
    }

    // 4) Kilitlenme degismezi (B.14): kademe 1 kazma ve balta Sv1'de craftlanabilir olmali
    //    ve malzemeleri alet gerektirmeyen yabani kaynaklardan gelmeli.
    List<String> yabaniCiktilar = new ArrayList<>();
    for (YabaniKaynak k : uretim.yabaniKaynaklar().kaynaklar()) {
      for (Getiri g : k.getiri()) yabaniCiktilar.add(g.id());
    }
    for (Alet alet : uretim.aletler()) {
      if (alet.kademe() != 1) continue;
      if (alet.craftSeviye() != 1) {
        hata.add("Kilitlenme riski: " + alet.id() + " kademe 1 ama craftSeviye " + alet.craftSeviye());
      }
      for (Malzeme m : alet.malzeme()) {
        if (!yabaniCiktilar.contains(m.id())) {
          hata.add(
              "Kilitlenme riski: " + alet.id() + " malzemesi " + m.id()
                  + " elle toplanabilir yabani kaynaklardan gelmiyor (B.14 degismezi)");
        }
      }
    }

    // 5) Baslangic envanteri bilinen esya olmali
    for (BaslangicEsya e : turetilmis.baslangicDurumu().envanter()) {
      if (!esyaKategorileri.containsKey(e.esyaId())) {
        hata.add("Baslangic envanterinde bilinmeyen esya: " + e.esyaId());
      }
    }

    // 6) Depo kapasitesi dogrusal buyumeli, maliyet ustel artmali (B.10)
    List<DepoKademe> dk = ilerleme.depo().kademeler();
    for (int i = 1; i < dk.size(); i++) {
      if (dk.get(i).kapasite() <= dk.get(i - 1).kapasite()) {
        hata.add("Depo kapasitesi artmiyor: kademe " + i);
      }
      if (dk.get(i).maliyet() <= dk.get(i - 1).maliyet()) {
        hata.add("Depo maliyeti artmiyor: kademe " + i);
      }
      if (dk.get(i).seferKarsiligi() > dk.get(i - 1).seferKarsiligi()) {
        hata.add(
            "Depo baskisi gevsiyor: kademe " + i + " sefer karsiligi "
                + dk.get(i).seferKarsiligi() + " > " + dk.get(i - 1).seferKarsiligi()
                + " (B.10: baski ilerledikce ARTMALI)");
      }
    }

    // 7) Bolge XP istisnasi gecerli bir bolgeyi gostermeli
    for (String b : turetilmis.bolgeXpIstisnasi().haricBolgeler()) {
      if (!bolgeIndeksi.containsKey(b)) {
        hata.add("bolgeXpIstisnasi bilinmeyen bolge gosteriyor: " + b);
      }
    }

    // 8) Defter sayfa numaralari 1..20, tekrarsiz
    List<Integer> nolar = turetilmis.defter().sayfalar().stream().map(DefterSayfasi::no).toList();
    for (int i = 1; i <= 20; i++) {
      if (Collections.frequency(nolar, i) != 1) {
        hata.add("Defter sayfa " + i + " tam bir kez tanimli degil");
      }
    }

    // 9) Ortak dugum saglik carpani 1.5 olmali (B.26 yasak 24 - 4x cevher patlamasi)
    if (Math.abs(uretim.ortakDugum().ortakVurustaSaglikCarpani() - 1.5) > 1e-9) {
      hata.add(
          "Ortak dugum saglik carpani " + uretim.ortakDugum().ortakVurustaSaglikCarpani()
              + ", 1.5 olmali (B.26 yasak 24)");
    }

    // 10) Baslangic urunleri gercekten tohum geri vermeli (B.11 baslangic urun kurali)
    for (String id : urunler.baslangicUrunKurali().urunler()) {
      Urun urun = urunIndeksi.get(id);
      if (urun == null) {
        hata.add("baslangicUrunKurali bilinmeyen urun gosteriyor: " + id);
      } else if (!urun.tohumGeriVerirMi()) {
        hata.add("Baslangic urunu " + id + " tohum geri vermiyor (B.11)");
      }
    }
    for (Urun urun : urunler.urunler()) {
      boolean baslangic = urunler.baslangicUrunKurali().urunler().contains(urun.id());
      if (!baslangic && urun.tohumGeriVerirMi()) {
        hata.add("Market urunu " + urun.id() + " tohum geri veriyor (B.11 yasak)");
      }
    }

    return new DenetimSonucu(hata, uyari);
  }

  // ------------------------------------------------------------------ erisim

  public UrunlerDosyasi urunlerDosyasi() { return urunler; }

  public UretimDosyasi uretimDosyasi() { return uretim; }

  public IlerlemeDosyasi ilerlemeDosyasi() { return ilerleme; }

  public TuretilmisDosyasi turetilmisDosyasi() { return turetilmis; }

  public JsonNode hamPaket() { return hamPaket; }

  public String surumEtiketi() { return surumEtiketi; }

  public Optional<Urun> urun(String id) { return Optional.ofNullable(urunIndeksi.get(id)); }

  public Collection<Urun> tumUrunler() { return urunIndeksi.values(); }

  /** Tohum esya kimliginden urun bulur. Ornek: {@code bugday_tohum -> bugday}. */
  public Optional<Urun> tohumdanUrun(String tohumEsyaId) {
    return Optional.ofNullable(tohumIndeksi.get(tohumEsyaId));
  }

  public Optional<Alet> alet(String id) { return Optional.ofNullable(aletIndeksi.get(id)); }

  public Collection<Alet> tumAletler() { return aletIndeksi.values(); }

  public Optional<Bolge> bolge(String id) { return Optional.ofNullable(bolgeIndeksi.get(id)); }

  public Optional<EvYapisi> evYapisi(String id) { return Optional.ofNullable(evYapiIndeksi.get(id)); }

  public Optional<YabaniKaynak> yabaniKaynak(String id) {
    return Optional.ofNullable(yabaniIndeksi.get(id));
  }

  public String esyaKategorisi(String esyaId) {
    return esyaKategorileri.getOrDefault(esyaId, "bilinmeyen");
  }

  public boolean esyaVarMi(String esyaId) { return esyaKategorileri.containsKey(esyaId); }

  public String esyaAdi(String esyaId) { return esyaAdlari.getOrDefault(esyaId, esyaId); }

  // ------------------------------------------------------------------ turetilmis hesaplar

  /** Sv n -> n+1 icin gereken XP. Ust seviyede 0 doner. */
  public int seviyeEsigi(int seviye) {
    Integer v = ilerleme.seviye().esikler().get(String.valueOf(seviye));
    return v == null ? 0 : v;
  }

  public int maksSeviye() { return ilerleme.seviye().maks(); }

  /** Hasat XP'si: max(1, round(buyumeSaati * 2)). Formul ilerleme.json'da yazili. */
  public int hasatXp(Urun urun) {
    double saat = urun.buyumeSn() / 3600.0;
    return Math.max(1, (int) Math.round(saat * 2));
  }

  /** Verilen oyuncu seviyesinde canta slot sayisi. */
  public int cantaSlot(int seviye) {
    int slot = 0;
    for (CantaKademe k : ilerleme.canta().kademeler()) {
      if (seviye >= k.acilisSeviye()) slot = Math.max(slot, k.slot());
    }
    return slot;
  }

  public int yiginBoyutu() { return ilerleme.canta().yiginBoyutu(); }

  /** Verilen dunya seviyesinde depo kapasitesi (birim). Sv 2 altinda depo yoktur -> 0. */
  public int depoKapasitesi(int dunyaSeviyesi) {
    int kap = 0;
    for (DepoKademe k : ilerleme.depo().kademeler()) {
      if (dunyaSeviyesi >= k.acilisSeviye()) kap = Math.max(kap, k.kapasite());
    }
    return kap;
  }

  /** Verilen oyuncu seviyesinde parseldeki karo sayisi. */
  public int tarlaKaroSayisi(int seviye) {
    int karo = 0;
    for (TarlaKademe k : ilerleme.tarla().karoKademeleri()) {
      if (seviye >= k.seviye()) karo = Math.max(karo, k.karo());
    }
    return karo;
  }

  public boolean bolgeAcikMi(String bolgeId, int dunyaSeviyesi) {
    return bolge(bolgeId).map(b -> dunyaSeviyesi >= b.acilisSeviye()).orElse(false);
  }

  public boolean evYapisiAcikMi(String yapiId, int dunyaSeviyesi) {
    return evYapisi(yapiId).map(y -> dunyaSeviyesi >= y.acilisSeviye()).orElse(false);
  }

  /** Bolgeye ilk giriste verilecek XP. Baslangic bolgesi (ev) icin 0. */
  public int bolgeIlkAcmaXp(String bolgeId) {
    if (turetilmis.bolgeXpIstisnasi().haricBolgeler().contains(bolgeId)) return 0;
    return ilerleme.xpKaynaklari().bolgeIlkAcma();
  }

  /** Oyuncunun seviyesinde ekilebilir urunler (acilis seviyesi <= seviye). */
  public List<Urun> acikUrunler(int seviye) {
    return urunler.urunler().stream().filter(u -> u.acilis().seviye() <= seviye).toList();
  }

  /** Tek kisilik vadide olceklenen maliyetler icin oran; iki oyunculuda 1.0. */
  public double olcekleme(boolean tekImzaModu) {
    return tekImzaModu ? ilerleme.tekKisilikOlcekleme().oran() : 1.0;
  }

  /** Buyume asamasi: floor(gecen / buyumeSn * asamaSayisi), son asamada sinirlanir. */
  public int buyumeAsamasi(long gecenSn, long buyumeSn) {
    int n = urunler.buyumeAsamasi().asamaSayisi();
    if (buyumeSn <= 0) return n - 1;
    long asama = (gecenSn * n) / buyumeSn;
    return (int) Math.max(0, Math.min(n - 1, asama));
  }

  /** ASCII kimlik normalizasyonu. tr_TR locale'inde toLowerCase() I harfini bozar. */
  public static String kimlikNormalize(String s) {
    return s == null ? null : s.trim().toLowerCase(Locale.ROOT);
  }
}
