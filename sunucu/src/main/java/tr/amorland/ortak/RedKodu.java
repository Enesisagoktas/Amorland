package tr.amorland.ortak;

/**
 * Sunucunun bir niyeti reddederken kullandigi kodlar (C.3).
 *
 * <p>Istemci her koda <b>ayri</b> geri bildirim gosterir; hepsi "bir hata olustu"ya
 * dusmez. Kod adlari protokolun parcasidir, degistirilirse istemci de degisir.
 */
public enum RedKodu {
  /** Hasat engellendi, urun karoda bekliyor. Esya YOK EDILMEZ (B.26 yasak 4). */
  DEPO_DOLU,
  /** Cantada bos slot yok. Kisit esya CESIDI uzerindendir, miktar degil. */
  CANTA_DOLU,
  /** Partner once aldi. */
  ESYA_YOK,
  /** Partner son vurusu yapti. */
  DUGUM_TUKENDI,
  /** Tren siparisi karsilandi. */
  SIPARIS_DOLDU,
  /** Gereken kademede alet yok. */
  ALET_YOK,
  /** Kat/bolge kapali. */
  SEVIYE_YETERSIZ,
  /** Istemci saati ileri alinmis olabilir (C.11: tekrarlanirsa kayda al). */
  HENUZ_OLGUN_DEGIL,
  /** Partner o yigini kilitlemis. */
  KILITLI,
  /** Karoda yapilacak bir sey yok. */
  GECERSIZ_HEDEF,
  /** Partnerin parseline dokunulamaz (B.7). */
  BASKA_PARSEL,
  /** Craft veya satin alma icin malzeme/altin yetmiyor. */
  KAYNAK_YETERSIZ,
  /** Hiz siniri asildi (C.11). */
  HIZ_SINIRI,
  /** Hesap baska cihazda acildi, bu oturum dusuruldu (C.5). */
  BASKA_CIHAZ,
  /** JWT gecersiz veya suresi dolmus. */
  YETKISIZ,
  /** Siniflandirilamayan hata. */
  BILINMEYEN
}
