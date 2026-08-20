package tr.amorland.oyun;

import tr.amorland.oyun.DunyaModeli.*;

import java.time.Instant;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

/**
 * Bir eylem sirasinda biriken degisiklikleri toplar.
 *
 * <p>Ayni karo/esya bir eylemde birden fazla kez degisebilir (ornek: hasat karoyu
 * bosaltir, tohum iadesi envanteri iki kez gunceller). Anahtarli haritalar
 * kullanilir ki istemciye <b>her varlik icin tek ve son</b> hali gitsin.
 */
public final class DegisimYazici {

  private final Map<String, Karo> karolar = new LinkedHashMap<>();
  private final Map<String, EnvanterSatiri> envanter = new LinkedHashMap<>();
  private final Map<String, DepoSatiri> depo = new LinkedHashMap<>();
  private final Map<String, Dugum> dugumler = new LinkedHashMap<>();
  private final Map<String, Alet> aletler = new LinkedHashMap<>();
  private final Map<UUID, OyuncuOzeti> oyuncular = new LinkedHashMap<>();
  private final List<String> isaretler = new ArrayList<>();
  private final List<Olay> olaylar = new ArrayList<>();
  private VadiOzeti vadi;

  public DegisimYazici karo(Karo k) {
    karolar.put(k.parselSahibi() + ":" + k.x() + ":" + k.y(), k);
    return this;
  }

  public DegisimYazici envanter(EnvanterSatiri e) {
    envanter.put(e.sahip() + ":" + e.esyaId(), e);
    return this;
  }

  public DegisimYazici depo(DepoSatiri d) {
    depo.put(d.esyaId(), d);
    return this;
  }

  public DegisimYazici dugum(Dugum d) {
    dugumler.put(d.bolge() + ":" + d.x() + ":" + d.y(), d);
    return this;
  }

  public DegisimYazici alet(Alet a) {
    aletler.put(a.sahip() + ":" + a.aletId(), a);
    return this;
  }

  public DegisimYazici oyuncu(OyuncuOzeti o) {
    oyuncular.put(o.id(), o);
    return this;
  }

  public DegisimYazici vadi(VadiOzeti v) {
    this.vadi = v;
    return this;
  }

  public DegisimYazici isaret(String i) {
    if (!isaretler.contains(i)) isaretler.add(i);
    return this;
  }

  public DegisimYazici olay(Olay o) {
    olaylar.add(o);
    return this;
  }

  public boolean bosMu() {
    return karolar.isEmpty()
        && envanter.isEmpty()
        && depo.isEmpty()
        && dugumler.isEmpty()
        && aletler.isEmpty()
        && oyuncular.isEmpty()
        && isaretler.isEmpty()
        && olaylar.isEmpty()
        && vadi == null;
  }

  public Degisim kapat(long revision, Instant simdi) {
    return new Degisim(
        revision,
        simdi,
        false,
        List.copyOf(karolar.values()),
        List.copyOf(envanter.values()),
        List.copyOf(depo.values()),
        List.copyOf(dugumler.values()),
        List.copyOf(aletler.values()),
        List.copyOf(oyuncular.values()),
        vadi,
        List.copyOf(isaretler),
        List.copyOf(olaylar));
  }
}
