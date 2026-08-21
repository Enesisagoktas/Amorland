package tr.amorland.denge;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

import java.util.List;
import java.util.Map;

/**
 * Denge JSON dosyalarinin Java karsiligi.
 *
 * <p>Mimari kural 8: denge sayilari koda gomulmez. Bu dosyada tek bir denge sayisi
 * yoktur; yalnizca JSON'un seklini tarif eden kayitlar vardir. Butun degerler
 * {@code resources/denge/*.json} dosyalarindan gelir.
 *
 * <p>Bilinmeyen alanlar yok sayilir: spec JSON'lari aciklama alanlari tasir ve
 * ileride yeni alan eklendiginde sunucu patlamamalidir.
 */
public final class DengeModelleri {

  private DengeModelleri() {}

  // ---------------------------------------------------------------- urunler.json

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record AcilisTohumOdulu(int adet) {}

  public record Acilis(String tip, int seviye) {
    public boolean seviyeOduluMu() {
      return "seviye_odulu".equals(tip);
    }
  }

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record YenidenVerir(int adet, long araSn) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Urun(
      String id,
      String ad,
      Acilis acilis,
      long buyumeSn,
      int tabanFiyat,
      int tohumFiyat,
      int verim,
      int xp,
      Boolean tohumGeriVerir,
      YenidenVerir yenidenVerir,
      Boolean tohumPenceresi) {

    /** Tohum esyasinin kimligi. Urun kimliginden turetilir, ayri tablo yok. */
    public String tohumEsyaId() {
      return id + "_tohum";
    }

    public boolean tohumGeriVerirMi() {
      return Boolean.TRUE.equals(tohumGeriVerir);
    }

    public boolean yenidenVerirMi() {
      return yenidenVerir != null && yenidenVerir.adet() > 1;
    }

    /** Toplam hasat sayisi. yenidenVerir.adet ILK hasati da sayar (D: yenidenVerirKurali). */
    public int toplamHasatSayisi() {
      return yenidenVerirMi() ? yenidenVerir.adet() : 1;
    }
  }

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record BuyumeAsamasi(int asamaSayisi) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record GubreEtki(int verimEk) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Gubre(int acilisSeviye, GubreEtki etki, double fiyatOraniTabanFiyata, boolean craftlanabilir) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record MarketAlisOrani(double min, double max) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record TrenFiyat(
      double bandMin,
      double bandMax,
      double baslangic,
      double ortalamayaDonusKatsayisi,
      double gurultuGenligi,
      long seferAraligiSn,
      String epochUtc,
      int ileriSarmaPenceresi) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Manifesto(
      int siparisKartiSayisi,
      double carpanMin,
      double carpanMax,
      int tamDoldurmaXpBonus,
      int siparisXp,
      double tekKisilikKontenjanOrani,
      boolean kacirmaCezasi) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record BaslangicUrunKurali(List<String> urunler) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record UrunlerDosyasi(
      String surum,
      Map<String, Integer> saatlikGetiriKademeleri,
      List<Urun> urunler,
      BuyumeAsamasi buyumeAsamasi,
      Gubre gubre,
      MarketAlisOrani marketAlisOrani,
      TrenFiyat trenFiyat,
      Manifesto manifesto,
      AcilisTohumOdulu acilisTohumOdulu,
      BaslangicUrunKurali baslangicUrunKurali) {}

  // ---------------------------------------------------------------- uretim.json

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Malzeme(String id, int adet) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Alet(
      String id,
      String tur,
      int kademe,
      Integer dayaniklilik,
      Integer kapasiteKaro,
      List<Malzeme> malzeme,
      int craftSeviye,
      Integer marketFiyat,
      Boolean herZamanMarkette) {

    public boolean kirilirMi() {
      return dayaniklilik != null && dayaniklilik > 0;
    }
  }

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record AletKurallari(double dayaniklilikUyariEsigi, boolean kirilincaAltKademeyeDus) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Getiri(String id, Integer adet, Integer adetMin, Integer adetMax) {
    public int enAz() {
      if (adetMin != null) return adetMin;
      return adet != null ? adet : 0;
    }

    public int enCok() {
      if (adetMax != null) return adetMax;
      return adet != null ? adet : 0;
    }
  }

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record YabaniKaynak(
      String id,
      String ad,
      long yenilenmeSn,
      List<Getiri> getiri,
      List<String> bolgeler,
      int xp) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record YabaniKaynaklar(List<YabaniKaynak> kaynaklar) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Agac(
      String id,
      String ad,
      int acilisSeviye,
      long buyumeSn,
      int vurus,
      int fidanFiyat,
      List<Getiri> getiri,
      int xp) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record MadenDugum(String id, int adetMin, int adetMax, int agirlik) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record MadenKat(
      int kat,
      String ad,
      int gerekenKazmaKademe,
      int acilisSeviye,
      int vurus,
      long yenilenmeSn,
      List<MadenDugum> dugumler) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Maden(List<MadenKat> katlar, int dugumXp) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record OrtakDugum(
      String model,
      boolean getiriBolusulur,
      double getiriIcinMinKatkiOrani,
      double ortakVurustaSaglikCarpani) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record AraMalzeme(String id, String ad, List<Malzeme> girdi, int craftSeviye) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Isleme(
      String id,
      String ad,
      List<Malzeme> girdi,
      int acilisSeviye,
      double fiyatCarpani,
      String yapi) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record YapiTarifi(String id, String ad, List<Malzeme> girdi, int acilisSeviye, String etki) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record CraftXp(int ilkKez, int tekrar) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record CraftTarifleri(
      int sinir,
      int kullanilan,
      List<AraMalzeme> araMalzeme,
      List<Isleme> isleme,
      List<YapiTarifi> yapi,
      CraftXp craftXp) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record UretimDosyasi(
      String surum,
      List<Agac> agaclar,
      boolean kutukKalir,
      YabaniKaynaklar yabaniKaynaklar,
      Maden maden,
      OrtakDugum ortakDugum,
      List<Alet> aletler,
      AletKurallari aletKurallari,
      CraftTarifleri craftTarifleri) {}

  // ---------------------------------------------------------------- ilerleme.json

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Seviye(String formul, int maks, int toplamXp, Map<String, Integer> esikler) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record HasatXp(String formul) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record IlkKezAnlari(int xp, List<String> anlar) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record YetismeBonusu(int seviyeFarkiEsigi, double carpan, int kapanmaFarki, int maksSureGun) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record XpKaynaklari(
      HasatXp hasat,
      int trenSiparisi,
      int manifestoTamDoldurma,
      int cevherDugumu,
      Map<String, Integer> agacKesme,
      int yabaniToplama,
      int craftIlkKez,
      int craftTekrar,
      int defterSayfasi,
      int ortakProjeTamamlama,
      int bolgeIlkAcma,
      IlkKezAnlari ilkKezAnlari,
      YetismeBonusu yetismeBonusu) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record CantaKademe(int slot, int acilisSeviye, int maliyet) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Canta(String kisitTuru, int yiginBoyutu, List<CantaKademe> kademeler, int aletCubuguSlot) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record DepoKademe(int kapasite, int acilisSeviye, int maliyet, double seferKarsiligi) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record DepoDoluKurali(
      boolean hasatEngellenir,
      boolean urunTarladaBekler,
      boolean esyaYokEdilir,
      boolean esyaYereDuser,
      boolean otomatikSatilir) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Depo(String kisitTuru, List<DepoKademe> kademeler, DepoDoluKurali doluKurali) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record TarlaKademe(int karo, int seviye) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Tarla(List<TarlaKademe> karoKademeleri, int parselSayisi) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Bolge(String id, String ad, int acilisSeviye) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record EvYapisi(String id, int acilisSeviye, List<String> tarifler, String acar) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record EylemSureleri(
      double yurumeKaroSn,
      double ekme,
      double sulama,
      double hasat,
      double yabaniToplama,
      double vurusBasina,
      double kovaDoldurma,
      boolean animasyonKilidiIptalEdilebilir) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record ZoomKademe(String ad, double ortoBoyut) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record OluBolge(int x, int y) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Kamera(
      String izdusum,
      double pitchDerece,
      double yawDerece,
      boolean yawKilitli,
      OluBolge oluBolgeKaro,
      double yumusatmaSn,
      List<ZoomKademe> zoomKademeleri,
      double karoBirimi,
      double karakterBoyBirim) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Ogretici(int sureSn, boolean atlanabilir, boolean modalDiyalog, String yontem) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record ParaninGirisi(int ilkSeviye, String tetikleyici) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record TekKisilikOlcekleme(double oran, List<String> olceklenir, List<String> olceklenmez) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record YasKapisi(int esik, List<String> altindaKapali, boolean altindaOynanabilir) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record PartnerTerk(
      int tekImzaModuGun,
      int sessizKapiGun,
      boolean donusteAnindaNormal,
      boolean bildirimGonderir) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record IlerlemeDosyasi(
      String surum,
      Seviye seviye,
      XpKaynaklari xpKaynaklari,
      Canta canta,
      Depo depo,
      Tarla tarla,
      List<Bolge> bolgeler,
      List<EvYapisi> evYapilari,
      EylemSureleri eylemSureleri,
      Kamera kamera,
      Ogretici ogretici,
      ParaninGirisi paraninGirisi,
      TekKisilikOlcekleme tekKisilikOlcekleme,
      YasKapisi yasKapisi,
      PartnerTerk partnerTerk) {}

  // ---------------------------------------------------------------- turetilmis.json

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record BolgeXpIstisnasi(List<String> haricBolgeler) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record DefterSayfasi(int no, String tetik, int perde) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Defter(int sayfaXp, List<DefterSayfasi> sayfalar) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Cevrimdisi(
      String ilke,
      int maksKuyrukEylem,
      int maksSureSn,
      List<String> izinliEylemler,
      List<String> yasakEylemler,
      boolean redOzetiTekBildirim) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record YenidenBaglanma(List<Integer> gecikmelerSn, int ustSinirSn, int deltaEsigi) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Renk(int id, String ad, String hex) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record MisafirHesap(
      int renkSayisi,
      List<Renk> renkler,
      int adMinUzunluk,
      int adMaksUzunluk,
      int davetKoduHane,
      long davetKoduOmruSn,
      int kurtarmaKoduHane) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record BaslangicEsya(String esyaId, int adet) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record BaslangicDurumu(List<BaslangicEsya> envanter, List<String> aletler, int kovaKapasitesi) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Kuyu(String bolge, double doldurmaSn, boolean sahneIcinde) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record EsyaTanimi(String id, String ad, String kategori) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Esyalar(List<EsyaTanimi> hamMadde, List<EsyaTanimi> islenmis, List<String> kategoriSirasi) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record DepoSiralama(int maksKilitliYigin, boolean topluAlButonu, boolean topluBosaltButonu) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record Sulama(
      boolean buyumeSulamayaBagli,
      String buyumeSayaciBaslangici,
      int cevrimBasinaSulama,
      boolean yenidenVerirCevrimindeYenidenSulanir) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record DepoKisitiKurali(boolean kapasiteSifirsaKisitYok) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record IyimserIstemci(
      double geriSarmaSn,
      String geriSarmaEgrisi,
      int konumYayinHz,
      boolean konumYayinRevizyonUretir) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record GunlukSandik(
      long sifirlamaSn,
      String sifirlamaAnlami,
      int tohumMin,
      int tohumMaks,
      String tohumKaynagi,
      int altinSeviyeCarpani,
      boolean seriVar,
      boolean birikir) {}

  @JsonIgnoreProperties(ignoreUnknown = true)
  public record TuretilmisDosyasi(
      String surum,
      BolgeXpIstisnasi bolgeXpIstisnasi,
      GunlukSandik gunlukSandik,
      Defter defter,
      Cevrimdisi cevrimdisi,
      YenidenBaglanma yenidenBaglanma,
      Map<String, String> redKodlari,
      IyimserIstemci iyimserIstemci,
      MisafirHesap misafirHesap,
      BaslangicDurumu baslangicDurumu,
      Kuyu kuyu,
      /** Bolge -> (yabani kaynak id -> adet). Ogreticide tarla kenarinda 3 kaynak parlar. */
      Map<String, Map<String, Integer>> yabaniKaynakYerlesimi,
      Esyalar esyalar,
      DepoSiralama depoSiralama,
      Sulama sulama,
      DepoKisitiKurali depoKisitiKurali) {}
}
