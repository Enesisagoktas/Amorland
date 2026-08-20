package tr.amorland.oyun;

import org.springframework.stereotype.Service;
import tr.amorland.denge.DengeServisi;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.UUID;

/**
 * Yeni vadiyi ve yeni katilan oyuncuyu dunyaya yerlestirir.
 *
 * <p>Karo yerlesimi <b>deterministiktir</b>: ayni karo sayisi her zaman ayni izgarayi
 * verir. Boylece seviye atlayip karo eklendiginde eski karolar yerinde kalir, oyuncunun
 * ektigi urun yer degistirmez.
 */
@Service
public class DunyaKurucu {

  private final DengeServisi denge;
  private final DunyaDeposu dunya;

  public DunyaKurucu(DengeServisi denge, DunyaDeposu dunya) {
    this.denge = denge;
    this.dunya = dunya;
  }

  /** Parsel izgarasi: karo sayisina gore genislik x yukseklik. */
  public record Izgara(int genislik, int yukseklik) {}

  /**
   * Karo sayisindan izgara boyutu. Kademeler B.11'den: 4, 8, 12, 16, 20, 24.
   *
   * <p>Yukseklik once buyur, sonra genislik: dar ve derin bir parsel ortografik
   * kamerada daha az yatay kaydirma gerektirir.
   */
  public static Izgara izgara(int karoSayisi) {
    return switch (karoSayisi) {
      case 4 -> new Izgara(2, 2);
      case 8 -> new Izgara(4, 2);
      case 12 -> new Izgara(4, 3);
      case 16 -> new Izgara(4, 4);
      case 20 -> new Izgara(5, 4);
      case 24 -> new Izgara(6, 4);
      default -> {
        int g = (int) Math.ceil(Math.sqrt(karoSayisi));
        yield new Izgara(g, (int) Math.ceil(karoSayisi / (double) g));
      }
    };
  }

  /** Karo koordinatlari, satir satir. Ilk N karo her kademede ayni yerde kalir. */
  public static List<int[]> karoKoordinatlari(int karoSayisi) {
    Izgara iz = izgara(karoSayisi);
    List<int[]> liste = new ArrayList<>(karoSayisi);
    for (int y = 0; y < iz.yukseklik(); y++) {
      for (int x = 0; x < iz.genislik(); x++) {
        if (liste.size() >= karoSayisi) break;
        liste.add(new int[] {x, y});
      }
    }
    return liste;
  }

  /** Oyuncunun parselini seviyesine gore acar. Var olan karolara dokunmaz. */
  public int parseliAc(UUID vadiId, UUID oyuncuId, int seviye) {
    int hedef = denge.tarlaKaroSayisi(seviye);
    for (int[] k : karoKoordinatlari(hedef)) {
      dunya.karoOlustur(vadiId, oyuncuId, k[0], k[1]);
    }
    return hedef;
  }

  /**
   * Yeni oyuncuyu vadiye yerlestirir: parsel, baslangic envanteri, kova durumu, kasa.
   *
   * <p>Baslangic envanteri {@code turetilmis.json}'dan gelir (B.18 1:20 - 4 bugday tohumu).
   */
  public void oyuncuyuYerlestir(UUID vadiId, UUID oyuncuId, int seviye, Instant simdi) {
    parseliAc(vadiId, oyuncuId, seviye);
    for (var e : denge.turetilmisDosyasi().baslangicDurumu().envanter()) {
      if (dunya.envanterAdet(vadiId, oyuncuId, e.esyaId()) == 0) {
        dunya.envanterDegistir(vadiId, oyuncuId, e.esyaId(), e.adet());
      }
    }
    dunya.kovaYaz(vadiId, oyuncuId, 0);
    dunya.altinDegistir(vadiId, oyuncuId, 0);
  }

  /**
   * Bolgedeki yabani kaynaklari kurar.
   *
   * <p>Bunlar alet kilitlenmesine karsi guvenlik agidir (B.14): hicbiri alet gerektirmez,
   * 30 dakikada yenilenir ve uc bolgede birden bulunur.
   */
  public void yabaniKaynaklariKur(UUID vadiId, String bolge) {
    Map<String, Integer> yerlesim =
        denge.turetilmisDosyasi().yabaniKaynakYerlesimi().getOrDefault(bolge, Map.of());
    int sira = 0;
    for (var giris : yerlesim.entrySet()) {
      String kaynakId = giris.getKey();
      denge.yabaniKaynak(kaynakId).orElseThrow(
          () -> new IllegalStateException("Bilinmeyen yabani kaynak: " + kaynakId));
      for (int i = 0; i < giris.getValue(); i++) {
        // Kenar seridi: x = -2 sabit, y artan. Tarla izgarasinin soluna dizilir.
        dunya.dugumOlustur(vadiId, bolge, -2, sira, kaynakId, "yabani", 1);
        sira++;
      }
    }
  }
}
