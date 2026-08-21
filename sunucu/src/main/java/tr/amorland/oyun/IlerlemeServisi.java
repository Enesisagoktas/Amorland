package tr.amorland.oyun;

import org.springframework.stereotype.Service;
import tr.amorland.denge.DengeServisi;
import tr.amorland.kimlik.Oyuncu;
import tr.amorland.kimlik.OyuncuDeposu;
import tr.amorland.ortak.Saat;
import tr.amorland.oyun.DunyaModeli.Olay;
import tr.amorland.vadi.Vadi;
import tr.amorland.vadi.VadiDeposu;

import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.UUID;

/**
 * XP, seviye, defter sayfalari ve "ilk kez" anlari (B.16, B.17, B.21).
 *
 * <p>⚠ B.26 yasak 12: <b>tek seferlik XP odullerini kismak Perde 1'i gunlerce suren
 * angaryaya cevirir.</b> Perde 1'in temposu ciftcilikle degil KESIFLE yurur:
 * ciftlikten gelen hiz gunde 14-28 XP iken, tek seferlik oduller 765 XP tasir.
 * Bu yuzden defter/bolge/ilk-kez odulleri burada ayni onemde ele alinir.
 */
@Service
public class IlerlemeServisi {

  private final DengeServisi denge;
  private final DunyaDeposu dunya;
  private final OyuncuDeposu oyuncular;
  private final VadiDeposu vadiler;
  private final DunyaKurucu kurucu;
  private final Saat saat;

  public IlerlemeServisi(
      DengeServisi denge,
      DunyaDeposu dunya,
      OyuncuDeposu oyuncular,
      VadiDeposu vadiler,
      DunyaKurucu kurucu,
      Saat saat) {
    this.denge = denge;
    this.dunya = dunya;
    this.oyuncular = oyuncular;
    this.vadiler = vadiler;
    this.kurucu = kurucu;
    this.saat = saat;
  }

  /**
   * XP ekler, gerekirse seviye atlatir ve acilimlari uygular.
   *
   * @return guncel oyuncu kaydi
   */
  public Oyuncu xpEkle(Vadi vadi, Oyuncu oyuncu, int hamXp, DegisimYazici yazici) {
    if (hamXp <= 0) return oyuncu;

    int xp = (int) Math.round(hamXp * yetismeBonusu(vadi, oyuncu));
    yazici.olay(Olay.xpKazandi(xp));

    int seviye = oyuncu.seviye();
    int birikim = oyuncu.xp() + xp;
    boolean seviyeAtladi = false;

    while (seviye < denge.maksSeviye()) {
      int esik = denge.seviyeEsigi(seviye);
      if (esik <= 0 || birikim < esik) break;
      birikim -= esik;
      seviye++;
      seviyeAtladi = true;
      yazici.olay(Olay.seviyeAtlama(seviye));
      seviyeOdulleriniUygula(vadi, oyuncu.id(), seviye, yazici);
    }
    // Ust seviyede XP birikmeye devam etmez.
    if (seviye >= denge.maksSeviye()) birikim = 0;

    oyuncular.seviyeVeXpYaz(oyuncu.id(), seviye, birikim);
    Oyuncu guncel = oyuncular.bul(oyuncu.id()).orElseThrow();

    if (seviyeAtladi) {
      dunyaSeviyesiniTazele(vadi);
    }
    return guncel;
  }

  /**
   * Yetisme bonusu (B.7): partnerinden 3+ seviye gerideysen XP x1.5.
   * 14 gun sonra fark kapanmasa bile duser — bilerek geri kalip biriktirmeyi engeller.
   */
  private double yetismeBonusu(Vadi vadi, Oyuncu oyuncu) {
    var yb = denge.ilerlemeDosyasi().xpKaynaklari().yetismeBonusu();
    if (!vadi.ciftMi()) return 1.0;

    UUID digerId = vadi.digerOyuncu(oyuncu.id());
    if (digerId == null) return 1.0;
    var diger = oyuncular.bul(digerId);
    if (diger.isEmpty()) return 1.0;

    int fark = diger.get().seviye() - oyuncu.seviye();
    if (fark < yb.seviyeFarkiEsigi()) return 1.0;

    Instant sinir = vadi.olusturuldu().plus(Duration.ofDays(yb.maksSureGun()));
    if (saat.simdi().isAfter(sinir)) return 1.0;

    return yb.carpan();
  }

  /** Dunya seviyesi iki oyuncunun yuksegidir (B.7). Bolge/kat erisimini bu belirler. */
  public void dunyaSeviyesiniTazele(Vadi vadi) {
    int enYuksek =
        oyuncular.bul(vadi.oyuncular()).stream().mapToInt(Oyuncu::seviye).max().orElse(1);
    if (enYuksek != vadi.dunyaSeviyesi()) {
      vadiler.dunyaSeviyesiYaz(vadi.id(), enYuksek);
    }
  }

  /**
   * Seviye odulleri: tarla karosu acilimi.
   *
   * <p>Urun acilimi, canta ve depo kapasitesi <b>tablodan turetilir</b>, ayri kayit
   * tutulmaz — oyuncunun seviyesi degistiginde otomatik gecerli olur.
   */
  private void seviyeOdulleriniUygula(Vadi vadi, UUID oyuncuId, int yeniSeviye, DegisimYazici yazici) {
    // Yeni seviyede acilan urunlerin tohumu verilir: acilis kutlamasi somut
    // olsun — tohumsuz acilim "sadece bugday ekiyoruz" cikmaziydi (oyuncu
    // bulgusu; tohumlarin baska edinme yolu yoktu).
    int tohumOdul = denge.urunlerDosyasi().acilisTohumOdulu().adet();
    for (var urun : denge.urunlerDosyasi().urunler()) {
      if (urun.acilis() == null || urun.acilis().seviye() != yeniSeviye) continue;
      int yeni = dunya.envanterDegistir(vadi.id(), oyuncuId, urun.tohumEsyaId(), tohumOdul);
      yazici.envanter(new DunyaModeli.EnvanterSatiri(oyuncuId, urun.tohumEsyaId(), yeni));
    }

    int oncekiKaro = denge.tarlaKaroSayisi(yeniSeviye - 1);
    int yeniKaro = denge.tarlaKaroSayisi(yeniSeviye);
    if (yeniKaro > oncekiKaro) {
      kurucu.parseliAc(vadi.id(), oyuncuId, yeniSeviye);
      for (int[] k : DunyaKurucu.karoKoordinatlari(yeniKaro)) {
        dunya
            .karo(vadi.id(), oyuncuId, k[0], k[1])
            .ifPresent(yazici::karo);
      }
    }
  }

  // ------------------------------------------------------------------ isaretler

  /**
   * "Ilk kez" ani. Ayni an ikinci kez gelirse XP verilmez.
   *
   * @return verilen XP (0 ise daha once olmus)
   */
  public int ilkKezAni(Vadi vadi, UUID oyuncuId, String an, DegisimYazici yazici) {
    var ik = denge.ilerlemeDosyasi().xpKaynaklari().ilkKezAnlari();
    if (!ik.anlar().contains(an)) return 0;
    if (!dunya.isaretKoy(vadi.id(), oyuncuId, "ilk:" + an, saat.simdi())) return 0;
    yazici.isaret("ilk:" + an);
    yazici.olay(Olay.ilkKez(an));
    return ik.xp();
  }

  /**
   * Bolgeye ilk giris. Ev haric her bolge 50 XP verir; ev baslangic bolgesidir (B.16 turetimi).
   *
   * @return verilen XP
   */
  public int bolgeyeIlkGiris(Vadi vadi, UUID oyuncuId, String bolgeId, DegisimYazici yazici) {
    if (!dunya.isaretKoy(vadi.id(), oyuncuId, "bolge:" + bolgeId, saat.simdi())) return 0;
    yazici.isaret("bolge:" + bolgeId);
    yazici.olay(Olay.bolgeAcildi(bolgeId));
    int xp = denge.bolgeIlkAcmaXp(bolgeId);
    // Bolge acilisi bir defter sayfasini tetikleyebilir.
    defterTetikle(vadi, oyuncuId, "bolge_acildi:" + bolgeId, yazici);
    return xp;
  }

  /**
   * Defter sayfasi tetikler (B.21).
   *
   * @return verilen XP (0 ise sayfa zaten aciktir veya tetik tanimsizdir)
   */
  public int defterTetikle(Vadi vadi, UUID oyuncuId, String tetik, DegisimYazici yazici) {
    var defter = denge.turetilmisDosyasi().defter();
    List<Integer> sayfalar =
        defter.sayfalar().stream()
            .filter(s -> s.tetik().equals(tetik))
            .map(s -> s.no())
            .toList();
    int toplam = 0;
    for (int no : sayfalar) {
      if (dunya.isaretKoy(vadi.id(), oyuncuId, "defter:" + no, saat.simdi())) {
        yazici.isaret("defter:" + no);
        yazici.olay(Olay.defterSayfasi(no));
        toplam += defter.sayfaXp();
      }
    }
    return toplam;
  }

  /** Craft XP'si: ilk kez 30, tekrar 5 (D: craftXp). */
  public int craftXp(Vadi vadi, UUID oyuncuId, String tarifId, DegisimYazici yazici) {
    var cx = denge.uretimDosyasi().craftTarifleri().craftXp();
    boolean ilk = dunya.isaretKoy(vadi.id(), oyuncuId, "craft:" + tarifId, saat.simdi());
    if (ilk) yazici.isaret("craft:" + tarifId);
    return ilk ? cx.ilkKez() : cx.tekrar();
  }
}
