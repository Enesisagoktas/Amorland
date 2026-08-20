package tr.amorland.kimlik;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import org.springframework.web.bind.annotation.*;
import tr.amorland.kimlik.KimlikServisi.OturumPaketi;

import java.util.Map;

/**
 * {@code /api/oturum/*} — C.10'daki kimlik uclari.
 */
@RestController
@RequestMapping("/api/oturum")
public class OturumUcu {

  private final KimlikServisi kimlik;

  public OturumUcu(KimlikServisi kimlik) {
    this.kimlik = kimlik;
  }

  public record MisafirIstek(
      @NotBlank @Size(max = 128) String cihazId, String ad, Integer renk) {}

  public record YenileIstek(@NotBlank String yenilemeJetonu) {}

  public record BaglaIstek(@NotBlank String tip, @NotBlank String ref) {}

  public record KurtarIstek(@NotBlank String kod, @NotBlank String cihazId) {}

  public record YasIstek(int dogumYili) {}

  /** Ilk acilis: kayit ekrani yok, cihaz kimligiyle misafir hesap (B.18 0:00). */
  @PostMapping("/misafir")
  public OturumPaketi misafir(@Valid @RequestBody MisafirIstek istek) {
    return kimlik.misafirGiris(istek.cihazId(), istek.ad(), istek.renk());
  }

  @PostMapping("/yenile")
  public OturumPaketi yenile(@Valid @RequestBody YenileIstek istek) {
    return kimlik.yenile(istek.yenilemeJetonu());
  }

  @PostMapping("/bagla")
  public Map<String, Object> bagla(@Valid @RequestBody BaglaIstek istek) {
    kimlik.bagla(IstekBaglami.oyuncuId(), istek.tip(), istek.ref());
    return Map.of("bagli", true);
  }

  /** Baglamak istemeyen oyuncu icin 12 haneli kod. Bir kez gosterilir. */
  @PostMapping("/kurtarma-kodu")
  public Map<String, Object> kurtarmaKodu() {
    return Map.of("kod", kimlik.kurtarmaKoduUret(IstekBaglami.oyuncuId()));
  }

  @PostMapping("/kurtar")
  public OturumPaketi kurtar(@Valid @RequestBody KurtarIstek istek) {
    return kimlik.kurtar(istek.kod(), istek.cihazId());
  }

  /** Yas kapisi bir kez sorulur (B.18 0:15). */
  @PostMapping("/yas")
  public Map<String, Object> yas(@RequestBody YasIstek istek) {
    kimlik.yasKapisi(IstekBaglami.oyuncuId(), istek.dogumYili());
    return Map.of("kabul", true);
  }
}
