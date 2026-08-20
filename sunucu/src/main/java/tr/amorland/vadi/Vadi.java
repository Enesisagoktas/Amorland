package tr.amorland.vadi;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/**
 * Bir cift (veya tek oyuncu) icin kalici dunya.
 *
 * <p>{@code tohum} deterministik fiyat ve pencere uretiminin girdisidir ve
 * <b>istemciye asla gonderilmez</b> (C.11) — gonderilse oyuncu gelecek fiyatlari
 * onceden hesaplardi.
 */
public record Vadi(
    UUID id,
    UUID kuranId,
    UUID partnerId,
    long revision,
    long tohum,
    int dunyaSeviyesi,
    boolean tekImzaModu,
    Instant partnerSonGorulme,
    Instant olusturuldu) {

  public boolean ciftMi() {
    return partnerId != null;
  }

  public List<UUID> oyuncular() {
    return partnerId == null ? List.of(kuranId) : List.of(kuranId, partnerId);
  }

  public boolean uyeMi(UUID oyuncuId) {
    return kuranId.equals(oyuncuId) || oyuncuId.equals(partnerId);
  }

  public UUID digerOyuncu(UUID oyuncuId) {
    if (partnerId == null) return null;
    return kuranId.equals(oyuncuId) ? partnerId : kuranId;
  }
}
