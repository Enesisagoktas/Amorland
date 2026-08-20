-- Amorland — ilk sema (Bolum C.2)
--
-- Kasitli kararlar:
--  * Hicbir tabloda buyume asamasi, fiyat veya kalan sure SAKLANMAZ (mimari kural 2 ve 3).
--    Hepsi okuma aninda now() ile hesaplanir.
--  * Tum zaman damgalari timestamptz ve UTC (mimari kural 5).
--  * Kimlikler (esya_id, bolge, urun_id) ASCII text. Turkce karakter yok:
--    tr_TR locale'inde buyuk/kucuk harf donusumu bozar.

-- gen_random_uuid() PostgreSQL 13+ ile cekirdekte gelir; pgcrypto eklentisi gerekmez.

-- ============================================================ oyuncu ve vadi

CREATE TABLE oyuncu (
  id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  cihaz_id            text UNIQUE NOT NULL,
  baglanti_tipi       text,                      -- null | eposta | google | apple
  baglanti_ref        text,
  kurtarma_kodu_hash  text,                      -- 12 hane, hash'lenmis
  ad                  text NOT NULL,
  renk                smallint NOT NULL DEFAULT 0 CHECK (renk BETWEEN 0 AND 3),
  seviye              smallint NOT NULL DEFAULT 1 CHECK (seviye BETWEEN 1 AND 20),
  xp                  integer  NOT NULL DEFAULT 0 CHECK (xp >= 0),
  dogum_yili          smallint,                  -- yas kapisi; 13 alti sohbet kapali
  bagli_kabul_teklif  smallint NOT NULL DEFAULT 0, -- hesap baglama kac kez teklif edildi (maks 3)
  aktif_oturum        uuid,                      -- "son baglanan kazanir" (C.5)
  son_gorulme         timestamptz,
  olusturuldu         timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE vadi (
  id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  kuran_id            uuid NOT NULL REFERENCES oyuncu(id),
  partner_id          uuid REFERENCES oyuncu(id),
  revision            bigint NOT NULL DEFAULT 0,  -- protokolun kalbi (C.1)
  tohum               bigint NOT NULL,            -- deterministik fiyat/pencere uretimi; istemciye GONDERILMEZ
  dunya_seviyesi      smallint NOT NULL DEFAULT 1,
  tek_imza_modu       boolean NOT NULL DEFAULT true,
  partner_son_gorulme timestamptz,
  olusturuldu         timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT vadi_partner_farkli CHECK (partner_id IS NULL OR partner_id <> kuran_id)
);

CREATE UNIQUE INDEX vadi_kuran_tek ON vadi(kuran_id);
CREATE UNIQUE INDEX vadi_partner_tek ON vadi(partner_id) WHERE partner_id IS NOT NULL;

-- Bir oyuncu ayni anda tek vadide olabilir (B.8).
CREATE TABLE oyuncu_vadi (
  oyuncu_id  uuid PRIMARY KEY REFERENCES oyuncu(id) ON DELETE CASCADE,
  vadi_id    uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  katildi_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX oyuncu_vadi_vadi ON oyuncu_vadi(vadi_id);

CREATE TABLE davet_kodu (
  kod          text PRIMARY KEY,                 -- 6 hane, karistirilamaz alfabe
  vadi_id      uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  ureten_id    uuid NOT NULL REFERENCES oyuncu(id),
  gecerli_bit  timestamptz NOT NULL,
  kullanildi_at timestamptz,
  olusturuldu  timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX davet_kodu_vadi ON davet_kodu(vadi_id);

CREATE TABLE yenileme_jetonu (
  jeton_hash  text PRIMARY KEY,
  oyuncu_id   uuid NOT NULL REFERENCES oyuncu(id) ON DELETE CASCADE,
  gecerli_bit timestamptz NOT NULL,
  iptal_at    timestamptz,
  olusturuldu timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX yenileme_jetonu_oyuncu ON yenileme_jetonu(oyuncu_id);

-- ============================================================ dunya durumu

CREATE TABLE tarla_karo (
  vadi_id        uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  parsel_sahibi  uuid NOT NULL REFERENCES oyuncu(id),
  x              smallint NOT NULL,
  y              smallint NOT NULL,
  urun_id        text,
  ekildi_at      timestamptz,        -- buyume BURADAN hesaplanir, cron yok
  sulandi_at     timestamptz,
  hasat_sayaci   smallint NOT NULL DEFAULT 0,   -- yeniden veren urunler
  son_hasat_at   timestamptz,
  gubreli        boolean NOT NULL DEFAULT false,
  PRIMARY KEY (vadi_id, parsel_sahibi, x, y)
);

CREATE TABLE dugum (
  vadi_id      uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  bolge        text NOT NULL,          -- tarla_kenari | orman | maden_girisi | maden_kat_1 ...
  x            smallint NOT NULL,
  y            smallint NOT NULL,
  tur          text NOT NULL,          -- dal | tas_parca | mantar | cam | tas | komur ...
  sinif        text NOT NULL,          -- yabani | agac | cevher
  saglik       smallint NOT NULL DEFAULT 0,
  maks_saglik  smallint NOT NULL DEFAULT 0,
  kirildi_at   timestamptz,            -- yenilenme BURADAN hesaplanir
  katki        jsonb NOT NULL DEFAULT '{}'::jsonb,   -- {oyuncu_id: vurus_sayisi}
  PRIMARY KEY (vadi_id, bolge, x, y)
);
CREATE INDEX dugum_vadi_bolge ON dugum(vadi_id, bolge);

CREATE TABLE envanter (
  vadi_id  uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  sahip    uuid NOT NULL REFERENCES oyuncu(id),
  esya_id  text NOT NULL,
  adet     integer NOT NULL CHECK (adet > 0),
  PRIMARY KEY (vadi_id, sahip, esya_id)
);

CREATE TABLE depo (
  vadi_id  uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  esya_id  text NOT NULL,
  adet     integer NOT NULL CHECK (adet > 0),
  koyan    uuid REFERENCES oyuncu(id),           -- yigindaki renk rozeti
  kilitli  boolean NOT NULL DEFAULT false,
  kilitleyen uuid REFERENCES oyuncu(id),
  PRIMARY KEY (vadi_id, esya_id)
);

CREATE TABLE alet (
  vadi_id      uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  sahip        uuid NOT NULL REFERENCES oyuncu(id),
  alet_id      text NOT NULL,
  dayaniklilik smallint NOT NULL,
  PRIMARY KEY (vadi_id, sahip, alet_id)
);

CREATE TABLE hayvan (
  vadi_id      uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  yapi         text NOT NULL,        -- kumes | ahir
  sira         smallint NOT NULL,
  tur          text NOT NULL,        -- tavuk | inek
  beslendi_at  timestamptz,
  birikmis     smallint NOT NULL DEFAULT 0,
  PRIMARY KEY (vadi_id, yapi, sira)
);

CREATE TABLE ortak_proje (
  vadi_id        uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  proje_id       text NOT NULL,
  katki          jsonb NOT NULL DEFAULT '{}'::jsonb,  -- {oyuncu_id: {esya_id: adet}}
  tamamlandi_at  timestamptz,
  PRIMARY KEY (vadi_id, proje_id)
);

-- Oyuncuya ozel ilerleme: defter sayfalari, "ilk kez" anlari, gorulen bolgeler.
CREATE TABLE ilerleme_isareti (
  vadi_id    uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  oyuncu_id  uuid NOT NULL REFERENCES oyuncu(id),
  isaret     text NOT NULL,          -- ilk_ekim | defter:3 | bolge:tarla | craft:tas_kazma
  isaret_at  timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (vadi_id, oyuncu_id, isaret)
);

-- Altin: vadi ortak kasasi degil, oyuncuya ozel (B.15 "para Sv 5'te girer").
CREATE TABLE kasa (
  vadi_id   uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  oyuncu_id uuid NOT NULL REFERENCES oyuncu(id),
  altin     bigint NOT NULL DEFAULT 0 CHECK (altin >= 0),
  PRIMARY KEY (vadi_id, oyuncu_id)
);

-- Kova durumu oyuncuya ozel: kapasite alete bagli, dolu karo sayisi burada.
CREATE TABLE kova_durumu (
  vadi_id    uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  oyuncu_id  uuid NOT NULL REFERENCES oyuncu(id),
  kalan_karo smallint NOT NULL DEFAULT 0 CHECK (kalan_karo >= 0),
  PRIMARY KEY (vadi_id, oyuncu_id)
);

-- ============================================================ dayaniklilik

CREATE TABLE eylem_kaydi (
  client_action_id uuid PRIMARY KEY,
  oyuncu_id        uuid NOT NULL REFERENCES oyuncu(id),
  vadi_id          uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  revision         bigint NOT NULL,
  sonuc            jsonb NOT NULL,       -- tekrar gelirse AYNI sonuc doner
  olusturuldu      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX eylem_kaydi_temizlik ON eylem_kaydi(olusturuldu);

CREATE TABLE revizyon_kaydi (
  vadi_id     uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  revision    bigint NOT NULL,
  degisim     jsonb NOT NULL,
  olusturuldu timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (vadi_id, revision)
);

CREATE TABLE reklam_jetonu (
  jeton          uuid PRIMARY KEY,
  oyuncu_id      uuid NOT NULL REFERENCES oyuncu(id),
  vadi_id        uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  yerlesim       text NOT NULL,
  baglam         jsonb NOT NULL DEFAULT '{}'::jsonb,
  verildi_at     timestamptz NOT NULL DEFAULT now(),
  dogrulandi_at  timestamptz,       -- reklam aginin SSV callback'i
  uygulandi_at   timestamptz        -- odul verildi
);
CREATE INDEX reklam_jetonu_oyuncu_gun ON reklam_jetonu(oyuncu_id, verildi_at);

CREATE TABLE mesaj_kuyrugu (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  vadi_id       uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  gonderen      uuid NOT NULL REFERENCES oyuncu(id),
  alici         uuid NOT NULL REFERENCES oyuncu(id),
  sifreli_govde bytea NOT NULL,
  olusturuldu   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX mesaj_kuyrugu_alici ON mesaj_kuyrugu(alici, olusturuldu);

CREATE TABLE engelleme (
  oyuncu_id    uuid NOT NULL REFERENCES oyuncu(id) ON DELETE CASCADE,
  engellenen_id uuid NOT NULL REFERENCES oyuncu(id) ON DELETE CASCADE,
  olusturuldu  timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (oyuncu_id, engellenen_id)
);

CREATE TABLE gunluk_sandik (
  vadi_id    uuid NOT NULL REFERENCES vadi(id) ON DELETE CASCADE,
  oyuncu_id  uuid NOT NULL REFERENCES oyuncu(id),
  son_alim_at timestamptz,
  PRIMARY KEY (vadi_id, oyuncu_id)
);

-- Kusluk: SADECE manuel, ekonomik etkisi yok, bildirim gondermez (B.8 kirmizi cizgi).
CREATE TABLE kusluk (
  vadi_id     uuid PRIMARY KEY REFERENCES vadi(id) ON DELETE CASCADE,
  baslatan_id uuid NOT NULL REFERENCES oyuncu(id),
  baslangic   timestamptz NOT NULL DEFAULT now()
);
