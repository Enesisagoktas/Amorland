# Amorland — Sunucu Veri Modeli ve Protokol

> Master prompt'un bilerek ertelediği doküman. `client_action_id` tek başına dayanıklılığı çözmüyor — bu dosya eksik yarısını tanımlıyor.
> Sürüm 1.0 · 2026-08-20

---

## 1. Temel kavram: dünya revizyonu

Tek `client_action_id` yeterli değildi çünkü **"aynı eylemi iki kez uygulama"yı** çözüyor ama **"istemci hangi durumu doğru sayacak"** sorusunu çözmüyor.

> Her vadinin monoton artan bir `revision` (bigint) sayacı vardır. Durumu değiştiren her eylem revizyonu bir artırır. İstemci en son bildiği revizyonu saklar.

Bu tek kavram üç problemi birden çözüyor: yeniden bağlanma, çakışma çözümü, ve iki istemcinin senkron kalması.

```
İstemci A: revision 1042  ──┐
                             ├─→ Sunucu: revision 1044
İstemci B: revision 1044  ──┘

A bağlanınca "1042'deyim" der → sunucu 1043 ve 1044'ü gönderir.
Fark çok büyükse (>200) tam anlık görüntü gönderir.
```

---

## 2. Veri modeli (PostgreSQL)

Tam DDL değil, **şema iskeleti**. Tip ve kısıt seçimleri kasıtlı.

### Oyuncu ve vadi

```sql
oyuncu (
  id            uuid PK,
  cihaz_id      text UNIQUE,           -- misafir hesap
  baglanti_tipi text,                  -- null | eposta | google | apple
  baglanti_ref  text,
  kurtarma_kodu_hash text,             -- 12 hane, hash'lenmiş
  ad            text,
  renk          smallint,              -- 0-3, partner ayırt etme
  seviye        smallint,
  xp            int,
  dogum_yili    smallint,              -- yaş kapısı (13 altı: sohbet kapalı)
  olusturuldu   timestamptz
)

vadi (
  id            uuid PK,
  kuran_id      uuid FK oyuncu,        -- ayrılıkta ev/depo buna kalır
  partner_id    uuid FK oyuncu NULL,
  revision      bigint NOT NULL,       -- ← protokolün kalbi
  tohum         bigint,                -- deterministik fiyat/pencere üretimi
  dunya_seviyesi smallint,             -- max(iki oyuncunun seviyesi)
  tek_imza_modu boolean,               -- partner 7 gündür yoksa true
  olusturuldu   timestamptz
)
```

### Dünya durumu

```sql
tarla_karo (
  vadi_id    uuid, parsel_sahibi uuid, x smallint, y smallint,
  urun_id    text NULL,
  ekildi_at  timestamptz NULL,         -- büyüme buradan HESAPLANIR, cron yok
  sulandi_at timestamptz NULL,
  hasat_sayaci smallint,               -- yeniden veren ürünler için
  son_hasat_at timestamptz NULL,       -- yeniden verme sayacı buradan
  gubreli    boolean,
  PRIMARY KEY (vadi_id, parsel_sahibi, x, y)
)

dugum (                                -- maden + orman + yabani
  vadi_id uuid, bolge text, x smallint, y smallint,
  tur text, saglik smallint, maks_saglik smallint,
  kirildi_at timestamptz NULL,         -- yenilenme buradan hesaplanır
  katki jsonb,                         -- {oyuncu_id: vuruş_sayısı}
  PRIMARY KEY (vadi_id, bolge, x, y)
)

envanter (vadi_id uuid, sahip uuid, esya_id text, adet int, PRIMARY KEY (vadi_id, sahip, esya_id))
depo     (vadi_id uuid, esya_id text, adet int, koyan uuid, kilitli boolean, PRIMARY KEY (vadi_id, esya_id))
alet     (vadi_id uuid, sahip uuid, alet_id text, dayaniklilik smallint)
hayvan   (vadi_id uuid, yapi text, sira smallint, tur text, beslendi_at timestamptz, birikmis smallint)
ortak_proje (vadi_id uuid, proje_id text, katki jsonb, tamamlandi_at timestamptz NULL)
```

⚠️ **Hiçbir tabloda büyüme aşaması, fiyat veya kalan süre saklanmaz.** Hepsi okuma anında `now()` ile hesaplanır. Mimari kural 2 ve 3.

### Dayanıklılık tabloları

```sql
eylem_kaydi (                          -- idempotency
  client_action_id uuid PK,
  oyuncu_id  uuid,
  vadi_id    uuid,
  revision   bigint,                   -- eylemin ürettiği revizyon
  sonuc      jsonb,                    -- tekrar gelirse AYNI sonuç döner
  olusturuldu timestamptz
)
-- TTL 24 saat. Mobil yeniden bağlanma günlerce sürmez.

revizyon_kaydi (                       -- delta gönderimi için
  vadi_id uuid, revision bigint, degisim jsonb, olusturuldu timestamptz,
  PRIMARY KEY (vadi_id, revision)
)
-- Son 200 revizyon tutulur. Daha eskisi için tam anlık görüntü gönderilir.

reklam_jetonu (                        -- ödül atomikliği
  jeton uuid PK, oyuncu_id uuid, yerlesim text,
  baglam jsonb,                        -- hangi hasat/sandık
  verildi_at timestamptz,
  dogrulandi_at timestamptz NULL,      -- reklam ağının SSV callback'i
  uygulandi_at timestamptz NULL        -- ödül verildi
)

mesaj_kuyrugu (                        -- teslim edilince SİLİNİR, arşiv değil
  id uuid PK, vadi_id uuid, gonderen uuid, sifreli_govde bytea,
  olusturuldu timestamptz              -- 7 gün sonra otomatik silme
)
```

---

## 3. WebSocket protokolü

### Bağlantı kurulumu

```
İstemci → BAGLAN { jwt, son_revision, istemci_surum }
Sunucu  → SENKRON_TAM   { revision, dunya }        (fark > 200 veya ilk giriş)
       veya
          SENKRON_DELTA { revision, degisimler[] } (fark ≤ 200)
```

### Eylem akışı

```
İstemci → NIYET {
  client_action_id: uuid,
  tip: "ek" | "sula" | "hasat" | "vur" | "topla" | "depoya_koy" | ...
  hedef: { bolge, x, y },
  ek: { urun_id? , esya_id?, adet? }
}

Sunucu  → ONAY { client_action_id, revision, degisim }
       veya
          RED  { client_action_id, kod, mesaj }
```

**Red kodları** (istemci her birine ayrı geri bildirim gösterir):

| Kod | Anlam |
|---|---|
| `DEPO_DOLU` | Hasat engellendi, ürün karoda bekliyor |
| `CANTA_DOLU` | Çeşit slotu yetersiz |
| `ESYA_YOK` | Partner önce aldı |
| `DUGUM_TUKENDI` | Partner son vuruşu yaptı |
| `SIPARIS_DOLDU` | Tren siparişi karşılandı |
| `ALET_YOK` | Gereken kademede alet yok |
| `SEVIYE_YETERSIZ` | Kat/bölge kapalı |
| `HENUZ_OLGUN_DEGIL` | İstemci saati ileri alınmış |
| `KILITLI` | Partner o yığını kilitlemiş |

### Yayın (broadcast)
Partner çevrimiçiyse her `ONAY` ona da `DEGISIM { revision, degisim }` olarak gider. Konum güncellemesi ayrı ve seyrek: **5 Hz**, ara değerlerle yumuşatılır, revizyon üretmez.

---

## 4. Çevrimdışı sınırı — "zarif kopuş"

⚠️ Master prompt'ta bu tanımsızdı ve içeride çelişki vardı: bir yerde "eylemler yerel kuyruğa alınır" (yani çevrimdışı oynanır), başka yerde "bağlantı gerektiği baştan söylenir" (yani oynanmaz).

**Karar: sınırlı çevrimdışı, ilkeli bir sınırla.**

> Kendi parselinde başladığın işi bitirebilirsin. Paylaşılan duruma dokunan hiçbir şeyi yapamazsın.

Gerekçe: kendi parselindeki eylemler **hiç kimseyle çakışmaz**, bu yüzden sonradan uygulanmaları güvenlidir. Depo, tren, market, maden ise ortak durumu değiştirir — çevrimdışı yapılırsa partner ile çakışır ve toplu red yağmuru olur.

| Bağlantı yokken | Durum |
|---|---|
| Mevcut sahnede kalma, gezinme, envanter görme | ✅ |
| Kendi parselinde ekme / sulama / hasat | ✅ kuyruğa alınır, **en fazla 20 eylem** |
| Yabani kaynak toplama | ✅ (kendi çantana gider) |
| Depo, tren, market, ortak proje | ❌ kapalı |
| Maden ve ormanda düğüm vurma | ❌ kapalı (ortak düğüm) |
| Sahne değiştirme | ❌ kapalı |
| Sohbet | ❌ (mesaj yerel taslakta bekler) |

**20 eylem dolduğunda veya 60 saniye geçtiğinde** "bağlantı yok" perdesi iner, oyun durur.

Bağlantı dönünce kuyruk **sırayla** gönderilir. Herhangi biri reddedilirse kalanlar yine de denenir (biri geçersizse diğerleri geçerli olabilir), her red ayrı bildirim üretmez — tek bir özet gösterilir: *"3 işlem uygulanamadı."*

---

## 5. Çakışma çözümü

| Durum | Kural |
|---|---|
| Kendi parselinde eylem | Uygulama anında geçerliyse **her zaman kabul** |
| Depodan alma, ikisi aynı anda | **Sunucuya ilk ulaşan kazanır.** Kaybeden `ESYA_YOK` |
| Aynı düğüme vurma | Çakışma değil, **tasarım**. İkisinin vuruşu aynı sağlıktan iner, düğüm sağlığı ×1.5 olur, katkısı %25 üstündeki her oyuncu tam getiri alır |
| Aynı tren siparişi | İlk ulaşan kazanır, diğeri `SIPARIS_DOLDU` |
| Ortak proje katkısı | Toplanır, çakışma yok |
| İki cihazdan aynı hesap | **Son bağlanan kazanır**, önceki oturum `BASKA_CIHAZ` ile düşürülür |

İstemci reddi **0.2 sn yumuşak geri sarma** ile gösterir, sert zıplama yok.

---

## 6. Reklam ödül atomikliği

⚠️ Master prompt oyun eylemleri için idempotency zorunlu kılmıştı ama reklam ödülü için hiçbir şey söylemiyordu. Bu bir tutarsızlıktı.

**İstemci ödülü asla kendisi vermez.** Akış:

```
1. İstemci → JETON_ISTE { yerlesim, baglam }
   Sunucu günlük 5 hakkı KENDİ sayar, jeton üretir → { jeton }
   (hak dolduysa RED — istemci sayacına güvenilmez)

2. İstemci reklamı gösterir

3. Reklam ağı → SUNUCU (SSV callback, istemciden bağımsız)
   Sunucu: dogrulandi_at = now()

4. Sunucu ödülü atomik uygular, uygulandi_at yazar, revizyon üretir
5. Sunucu → İstemci: DEGISIM
```

| Kenar durum | Davranış |
|---|---|
| İstemci reklam ortasında kapandı | SSV zaten geldi → ödül uygulanır, oyuncu bir sonraki girişte görür |
| SSV hiç gelmedi | Jeton 1 saat sonra süresi dolar, hak **iade edilir** |
| İki kez kullanma denemesi | `jeton` birincil anahtar + `uygulandi_at` kontrolü → ikinci istek yok sayılır |
| Çanta/depo dolu | **Jeton hiç verilmez**, buton pasif ve sebebi yazar |

### "2× hasat" tam anlamı
- Sadece **hasat edilen ürün** ikiye katlanır
- Başlangıç ürünlerinin **geri verdiği tohum katlanmaz**
- Yeniden veren üründe **sadece o hasat** için geçerli, döngünün tamamı için değil
- Gübre ile birleşince: gübre önce uygulanır, reklam **sonucu** ikiye katlar → `(1 + gübre) × 2`
- Günlük 5 reklam hakkının **1'ini** harcar

---

## 7. Bildirim önceliği

Günde en fazla 2 bildirim var ama hangi 2'si olduğu tanımsızdı.

| Öncelik | Olay | Koşul |
|---|---|---|
| 1 | Partner mesajı / not tahtası | Her zaman |
| 2 | Ortak proje tamamlandı | Her zaman |
| 3 | Tren manifestosu bitiyor | Son 1 saat **ve** oyuncunun karşılayabileceği sipariş varsa |
| 4 | Günlük sandık hazır | 24 sa doldu |
| 5 | Hayvan ürünü birikme sınırına yaklaştı | 3. periyoda girdi |

**Asla bildirim gönderilmeyen:** ürün büyüdü (çok sık) · depo doldu (dırdır) · **küslük** (kırmızı çizgi) · **partner terk** (kırmızı çizgi)

Kurallar: günde en fazla 2 · aralarında en az 4 saat · **yerel saatle 23:00-09:00 arası hiç yok** · oyuncu o gün zaten girdiyse 4 ve 5 gönderilmez.

---

## 8. Günlük sandık

| Kural | Karar |
|---|---|
| Sıfırlama | **Son alımdan 24 saat sonra.** UTC gece yarısı değil — saat dilimi adaletsizliği ve gece yarısı çift alma açığı doğuruyor |
| İçerik | 3-5 tohum (açık ürünlerden, rastgele) + `seviye × 50` altın |
| Seri (streak) | **Yok.** Kozy oyun; kaçırdığın gün cezalandırılmaz |
| Süre dolması | Yok, birikmez de — bir tane bekler |
| Reklamla 2× | Hem altını hem tohum adedini katlar, günlük 5 hakkın 1'i |

---

## 9. Yeniden bağlanma — tam akış

```
Bağlantı koptu
   ↓
İstemci: kuyruk moduna geçer (kendi parseli, en fazla 20 eylem / 60 sn)
   ↓
Tekrar bağlanma denemesi: 1s, 2s, 4s, 8s, 15s, 30s (üst sınır 30s)
   ↓
BAGLAN { son_revision }
   ↓
   ├─ fark ≤ 200  → SENKRON_DELTA, istemci uygular
   └─ fark > 200  → SENKRON_TAM, istemci yerel durumu atar
   ↓
Kuyruktaki eylemler sırayla gönderilir (client_action_id ile)
   ↓
Sunucu tekrarları yutar (eylem_kaydi), yeni olanları uygular
   ↓
Reddedilenler tek özet bildirimde toplanır
```

⚠️ **Arka plana atılma iOS'ta bağlantıyı birkaç saniyede öldürür.** Uygulama öne geldiğinde bu akış her seferinde çalışır — istisna değil, normal durum. Baştan böyle yazılmalı.

---

## 10. REST uçları (meta, WebSocket dışı)

```
POST /api/oturum/misafir      → cihaz_id ile misafir hesap + JWT
POST /api/oturum/bagla        → e-posta/Google/Apple bağlama
POST /api/oturum/kurtar       → 12 haneli kod ile hesap kurtarma
POST /api/vadi                → yeni vadi kur
POST /api/vadi/davet          → 6 haneli kod üret
POST /api/vadi/katil          → kodla katıl
GET  /api/vadi/{id}/anlik     → tam anlık görüntü (WS öncesi ilk yükleme)
POST /api/rapor               → istemci taraflı rapor paketi (son N mesaj)
POST /api/engelle             → engelleme listesi
GET  /api/denge               → denge JSON'ları (sürüm etiketli, önbelleklenir)
```

`GET /api/denge` sayesinde denge ayarı mağaza güncellemesi gerektirmez — mimari kural 8.

---

## 11. Güvenlik notları

- JWT kısa ömürlü (1 saat) + yenileme jetonu
- **İstemciden gelen hiçbir miktar, fiyat, XP veya süre kabul edilmez.** Sadece niyet: ne, nerede.
- Hız sınırı: eylem 10/sn, sohbet 1/sn, jeton isteği 5/gün
- `HENUZ_OLGUN_DEGIL` reddi tekrarlanıyorsa istemci saati oynanıyor demektir — kayda al, engelleme
- Deterministik fiyat tohumu (`vadi.tohum`) **istemciye gönderilmez**; istemci fiyatları sunucudan alır
