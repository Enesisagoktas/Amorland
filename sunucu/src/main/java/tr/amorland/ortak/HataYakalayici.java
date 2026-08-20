package tr.amorland.ortak;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

import java.util.Map;

/**
 * REST tarafinda hatalari istemcinin anlayacagi {@code {kod, mesaj}} bicimine cevirir.
 *
 * <p>Istemci koda gore ayri geri bildirim gosterir (C.3); serbest metne bakmaz.
 */
@RestControllerAdvice
public class HataYakalayici {

  private static final Logger log = LoggerFactory.getLogger(HataYakalayici.class);

  @ExceptionHandler(OyunHatasi.class)
  public ResponseEntity<Map<String, String>> oyunHatasi(OyunHatasi e) {
    HttpStatus durum =
        switch (e.kod()) {
          case YETKISIZ, BASKA_CIHAZ -> HttpStatus.UNAUTHORIZED;
          case HIZ_SINIRI -> HttpStatus.TOO_MANY_REQUESTS;
          case SEVIYE_YETERSIZ, BASKA_PARSEL, KILITLI -> HttpStatus.FORBIDDEN;
          default -> HttpStatus.CONFLICT;
        };
    return ResponseEntity.status(durum).body(Map.of("kod", e.kod().name(), "mesaj", e.getMessage()));
  }

  @ExceptionHandler(MethodArgumentNotValidException.class)
  public ResponseEntity<Map<String, String>> dogrulama(MethodArgumentNotValidException e) {
    String mesaj =
        e.getBindingResult().getFieldErrors().stream()
            .map(h -> h.getField() + ": " + h.getDefaultMessage())
            .findFirst()
            .orElse("Gecersiz istek");
    return ResponseEntity.badRequest().body(Map.of("kod", RedKodu.BILINMEYEN.name(), "mesaj", mesaj));
  }

  @ExceptionHandler(Exception.class)
  public ResponseEntity<Map<String, String>> beklenmeyen(Exception e) {
    log.error("Beklenmeyen hata", e);
    return ResponseEntity.internalServerError()
        .body(Map.of("kod", RedKodu.BILINMEYEN.name(), "mesaj", "Sunucu hatasi"));
  }
}
