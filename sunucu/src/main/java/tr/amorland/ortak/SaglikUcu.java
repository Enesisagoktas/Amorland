package tr.amorland.ortak;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import tr.amorland.denge.DengeServisi;

import java.util.Map;

/**
 * {@code GET /api/saglik} — istemcinin acilista sunucuya ulasip ulasamadigini
 * ogrenmesi icin. Ayrica <b>sunucu saatini</b> doner: istemci saat farkini burada
 * olcer ve buyume gostergelerini ona gore cizer (mimari kural 5).
 */
@RestController
@RequestMapping("/api/saglik")
public class SaglikUcu {

  private final Saat saat;
  private final DengeServisi denge;

  public SaglikUcu(Saat saat, DengeServisi denge) {
    this.saat = saat;
    this.denge = denge;
  }

  @GetMapping
  public Map<String, Object> saglik() {
    return Map.of(
        "durum", "acik",
        "sunucuZamaniMs", saat.simdi().toEpochMilli(),
        "dengeSurumu", denge.surumEtiketi());
  }
}
