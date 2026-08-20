package tr.amorland.denge;

import com.fasterxml.jackson.databind.JsonNode;
import org.springframework.http.CacheControl;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.time.Duration;

/**
 * {@code GET /api/denge} — denge JSON'lari, surum etiketli ve onbelleklenebilir.
 *
 * <p>Mimari kural 8: denge ayari yeniden derleme ve magaza guncellemesi gerektirmez.
 * Istemci acilista bu ucu cagirir, ETag ile onbellege alir, degistiyse yeniden indirir.
 *
 * <p>Kimlik dogrulama gerektirmez: icinde oyuncuya ozel hicbir sey yoktur.
 * Dikkat, {@code vadi.tohum} burada YOKTUR (C.11: deterministik fiyat tohumu istemciye gonderilmez).
 */
@RestController
@RequestMapping("/api/denge")
public class DengeUcu {

  private final DengeServisi denge;

  public DengeUcu(DengeServisi denge) {
    this.denge = denge;
  }

  @GetMapping
  public ResponseEntity<JsonNode> getir(
      @RequestHeader(value = "If-None-Match", required = false) String istemciEtag) {

    String etag = "\"" + denge.surumEtiketi() + "\"";
    if (etag.equals(istemciEtag)) {
      return ResponseEntity.status(304).eTag(etag).build();
    }
    return ResponseEntity.ok()
        .eTag(etag)
        .cacheControl(CacheControl.maxAge(Duration.ofHours(1)).cachePublic())
        .body(denge.hamPaket());
  }
}
