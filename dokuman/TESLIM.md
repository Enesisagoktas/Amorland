# Hafta 1 teslim raporu

**Hedef (A.4 #1):** Ev + Tarla, tek ürün, ek/sula/biç, iki oyuncu bağlı, durum kalıcı,
ses altyapısı + yer tutucu ses seti.

**Durum:** dikey dilim çalışıyor ve doğrulandı. Unity 6000.3.22f1 kuruldu, proje açıldı,
**istemci gerçek Unity'de 0 hatayla derlendi.**

---

## 1. Ne çalışıyor

### Sunucu

| Alan | Durum |
|---|---|
| Denge katmanı — 4 JSON, açılışta 10 tutarlılık denetimi | ✅ |
| Şema — C.2'deki tüm tablolar, Flyway | ✅ |
| Misafir hesap, JWT + yenileme, 12 haneli kurtarma kodu, yaş kapısı | ✅ |
| Vadi kurma, 6 haneli davet, katılma, dünya seviyesi = max(iki oyuncu) | ✅ |
| Revizyon protokolü — delta/tam senkron, `client_action_id` ile idempotency | ✅ |
| ek / sula / hasat / topla / craft / kova_doldur | ✅ |
| Depo — koy/al/boşalt/kilitle, koyan rozeti, kapasite, eşzamanlılık | ✅ |
| Çanta (çeşit) ve depo (miktar) kısıtları, 16 red kodu | ✅ |
| XP, seviye, defter sayfaları, "ilk kez" anları, yetişme bonusu | ✅ |
| WebSocket — BAGLAN/SENKRON_*/NIYET/ONAY/RED/DEGISIM/KONUM/SOHBET/NOT_TAHTASI | ✅ |
| Küslük — sadece manuel, ekonomik etkisi yok, bildirim göndermez | ✅ |

### İstemci (Unity)

| Alan | Durum |
|---|---|
| Protokol DTO'ları — sunucudaki `DunyaModeli` ile birebir | ✅ derleniyor |
| REST + WebSocket, yeniden bağlanma merdiveni (1,2,4,8,15,30 sn) | ✅ derleniyor |
| Çevrimdışı kuyruk — 20 eylem / 60 sn, sadece kendi parselin | ✅ derleniyor |
| İyimser tahmin + 0.2 sn yumuşak geri sarma | ✅ derleniyor |
| Ortografik kamera — 50° pitch, **yaw kilitli**, ölü bölge, iki zoom | ✅ derleniyor |
| Prosedürel low-poly mesh, sert normal, 16 renkli palet, arka yüz yok | ✅ derleniyor |
| Izgara A*, dokun-yürü, dinamik joystick, partner avatarı (5 Hz interpolasyon) | ✅ derleniyor |
| Bağlam duyarlı eylem (alet seçimi yok) + sürükle-boyama kuyruğu | ✅ derleniyor |
| Kod ile UI — HUD, çanta, depo, tezgâh, defter, harita, davet, ayarlar, giriş | ✅ derleniyor |
| Ses — 16'lık havuz, ±%8 pitch, 3 varyasyon, etiket başına 3 kanal, kuyrukta %75 | ✅ derleniyor |
| Haptik — Android `VibrationEffect`, iOS `UIFeedbackGenerator`, kapatılabilir | ✅ derleniyor |
| Öğretici — parlayan hedef + tek satır, modal yok | ✅ derleniyor |
| **Vadi haritası** — 180×130 karo, 3 Tilemap katmanı, 7 bölge | ✅ **çalışıyor, görüntüsü var** |
| **Haritada gezinme** — sürükle / WASD / tekerlek / çimdik, sınır kenetli | ✅ **çalışıyor** |

---

## 1b. Vadi haritası (64 px, ikinci sürüm)

Açılıştaki gezilebilir vadi. İlk sürüm 16 px Tiny-* setiyle kurulmuştu; kullanıcıya
fazla basit geldi. 212 Kenney paketinin tamamı tarandı (8 paralel ajan, envanter
`araclar/karo-indeks/` altında) ve omurga **Kenney RPG Base**'e (64 px, vektör
çizim) taşındı. Harita da büyütüldü.

| Ne | Değer |
|---|---|
| Boyut | **512 × 320 karo = 163.840 hücre** (ilk sürümün ~7 katı), 4 katman |
| Karo seti | RPG Base (CC0) + `ek64.png` — eksikler RPG Base paletinden örneklenerek Python'la çizildi |
| Bölge | 7 spec bölgesi + meyve bahçesi, iskele, sonbahar kuşağı |
| Arazi | Göl + ada, iki nehir, korkuluklu kalas köprüler, yol ağı, tren hattı |
| Binalar | Karodan kompoze: 3 duvar malzemesi × 2 çatı, kapı/pencere/vitrin kaplamaları |
| Üretim | Deterministik (`Tohum = 20260821`) |

**Teknik çekirdek.** RPG Base piksel sanatı değil; Bilinear + mipmap şart. Atlasta
komşu karolar Bilinear'da birbirine sızdığı için karolar yüklenirken kenarları
taşırılarak (extrude) 8 px oluklu bir çalışma-anı atlasına kopyalanır
(`Karo64.cs`). Kapı/pencereler duvar karolarının üstüne binen saydam kaplamalar
olduğundan dördüncü bir Tilemap katmanı (`NesneUst`) eklendi.

**Ölçüm disiplini sürdü.** Beş yanlış tahmin görüntü ölçümüyle yakalandı: 162
saçak sağı değil üçgen alınlık yarısıydı (çatının ucu çapraz kesiliyordu); 111
duvar ortasıydı, sağ kenar 112; mavi gövdenin kendi kaidesi yok, taş kaideyi
paylaşıyor; kıvrımlı çatı panelleri (140/142) gövdede dikiş üretiyor, gövde tek
karodan döşenir; kahve çatı toprak avluyla ton tona kaybolur — toprak zeminde
çatı arduvaz olur. Hepsi `araclar/karo-indeks/` görüntüleriyle belgeli.

**Render klonu.** Kullanıcının Unity editörü açıkken başsız render alınamıyor
(tek örnek kilidi). Doğrulama render'ları `%TEMP%\amorland-render-klon` altındaki
klon projede koşuyor; `robocopy -MIR` ile eşitleniyor.

Bilinen eksik: bölgeye dokunma bağlı değil (kayıtlar hazır, `Oyun.BolgeyeGit`
bekliyor); tren yok, hat ve gar var.

---

## 2. Doğrulama — çalıştırıldı, çıktısı var

| Ne | Sonuç |
|---|---|
| Sunucu testleri | **56/56 geçti** (gerçek PostgreSQL, Docker gerektirmiyor) |
| Bütünlük — çalışma zamanı | **22/22 geçti** (26 yasak + 8 mimari kural) |
| Bütünlük — statik | **16/16 geçti** |
| Protokol — canlı sunucu | **32/32 geçti** (Unity dışından, gerçek WebSocket) |
| Unity C# derlemesi — **gerçek Unity DLL'leri** | **0 hata** (iki girdi kolunda da) |
| Unity projesi açılışı + paket çözümü | **0 hata** (URP 17.3.0, Input System 1.20.0, Newtonsoft 3.2.2) |
| `Amorland > Kurulumu Çalıştır` | **başarılı** — sahne, yön, girdi, ses politikası, URP |
| Vadi haritası — başsız render | **9 PNG** (tam vadi + 7 bölge + köprü) |
| Play ekranı — gerçek `VadiSahnesi` yolu | **1 PNG**, kamera/etiket/arayüz dahil |

Hepsini tekrar koşmak için:

```bash
cd sunucu && ./mvnw test && cd .. && python araclar/butunluk-denetimi.py
araclar/csharp-dogrulama/dogrula.sh
```

---

## 3. ⚠ Bulgular — karar senin

### 3.1 Sulamanın mekanik etkisi spec'te TANIMSIZDI

B.1 "Tarlada biri eker, biri sular" diyor, B.6 sulamayı eylem listesine koyuyor,
B.11 kova kapasitesini ve kuyuyu tarif ediyor — ama **sulamanın ürüne ne yaptığı
hiçbir yerde yazmıyor.**

**Kararım:** sulama zorunlu. Büyüme saati `ekildiAt` değil **`sulandiAt`**'ten başlar;
sulanmayan ürün aşama 0'da kalır.

**Gerekçe:** Yağmurlama craft'ı (Sv 14) "4 komşu karoyu **her büyüme çevriminde**
otomatik sular" diyor. Sulama kozmetik olsaydı bu craft'ın ve 4 kademeli kova
tablosunun varlık sebebi olmazdı. Ayrıca hikâyenin merkezindeki iş bölümü
mekanik karşılığını buradan alıyor.

**Etkisi:** Yeniden veren ürünlerde her çevrim yeniden sulanır. Cron gerektirmez:
`olgunlukAnı = sulandiAt + (hasatSayaci == 0 ? büyümeSn : yenidenVerir.araSn)`.

Gerekçe `sunucu/src/main/resources/denge/turetilmis.json` → `sulama` içinde yazılı.
**Farklı istiyorsan tek yerden değişir.**

---

### 3.2 Depo kısıtı öğreticiyi kilitliyordu

B.10 "Depo dolu: hasat **engellenir**" diyor. Ama depo Sv 2'de açılıyor ve
**Sv 1'de kapasite 0.** Kural harfiyen uygulansaydı `0 >= 0` olur ve
**öğreticideki ilk hasat engellenirdi.**

**Kararım:** kısıt yalnızca depo açıkken (kapasite > 0) uygulanır.
`turetilmis.json` → `depoKisitiKurali`. Test: `DikeyDilimTest.depoSv1deHasatiEngellemez`.

---

### 3.3 Bölge XP'si: ev sayılmıyor

B.16 "Perde 1 tek seferlik XP: 5 defter (375) + **3 bölge (150)** + 3 craft (90) +
6 ilk kez (150) = **765**".

Perde 1'de açılan bölgeler: tarla, depo, gar = 3. **Ev sayılmıyor** çünkü oyuncu
orada başlıyor, "ilk açma" eylemi yok. Ev dahil edilseydi toplam 815 olurdu ve
765 tutmazdı.

Bu **türetim, tahmin değil** — 765 sayısı ancak böyle tutuyor.
`turetilmis.json` → `bolgeXpIstisnasi`. Test: `ButunlukTest.yasak12_tekSeferlikOduller`.

---

### 3.4 Sv 19→20 eşiği formülle 1 XP sapıyor

`ilerleme.json` tablosu: `19 → 2663`
Formül `round(100 × 1.2^18)` = **2662**

Tablo kendi içinde tutarlı (eşikler toplamı tam 15.476), sapma yalnızca formül
açıklamasında. **Tablo geçerli sayıldı**, sunucu açılışta uyarı basıyor:

```
DENGE UYARISI: Seviye esigi 19 -> 20 = 2663, formul 2662 diyor (yuvarlama sapmasi)
```

Etkisi 1 XP. Düzeltmek istersen `ilerleme.json` → `seviye.esikler."19"`.

---

### 3.5 Öğretici temposu: Sv 2 tasarımdan ~1.5 dk erken

Ölçüm (`./mvnw test -Dtest=OgreticiTemposuTest`):

```
  0:45 ev + defter sayfa 1               Sv 1   XP    0 / 100
  1:20 tarlaya gir (+50 XP)              Sv 1   XP   50 / 100
  1:30 ilk ekim (+25 XP)                 Sv 1   XP   75 / 100
  1:50 ilk sulama (+25 XP)               Sv 2   XP    0 / 120   ← tasarım 3:30 diyor
  ilk hasat (+1 XP, defter sayfa 2)      Sv 2   XP  104 / 120
```

Sv 3'e (partner daveti) kalan 16 XP — tasarımın ~10:00 hedefiyle aynı büyüklük sırasında.

**Etkisi küçük:** öğreticinin 3:30 adımı depoyu zaten *öğretiyor*, *açmıyor*.
**B.26 yasak 12** bu ödülleri kısmayı yasaklıyor; ayar gerekirse defter sayfa 1
tetikleyicisi kaydırılır, XP değeri düşürülmez.

---

### 3.6 Stub'ların gizlediği 3 Unity API hatası

Unity kurulunca istemci kodunu **gerçek Unity assembly'lerine** karşı derledim
(lisans gerektirmiyor). Stub'ların yanlış taklit ettiği 3 hata çıktı — **üçü de
Unity'de derlemeyi kırardı:**

| Hata | Doğrusu |
|---|---|
| `ShadowCastingMode` | `UnityEngine`'de değil, **`UnityEngine.Rendering`**'de |
| `Button.onClick.AddListener(Action)` | `UnityAction` ister; `System.Action` değişkeni geçilemez (CS1503) |
| `InputSystemUIInputModule` | `EventSystems`'te değil, **`UnityEngine.InputSystem.UI`**'da |

Üçü düzeltildi, stub'lar gerçeğe uyduruldu. `dogrula.sh` artık 4 varyant koşuyor:
stub/gerçek × eski/yeni girdi. Ayrıca MSBuild önbelleği bayat "0 hata" gösterebiliyordu —
her varyant ayrı çıktı klasörüne derleniyor.

**Ders:** stub tabanlı doğrulama iyi bir ilk savunma ama tek başına yeterli değil.
Unity kuruluysa her zaman `GercekUnity` katmanı da koşturulmalı.

---

### 3.7 `UYE_DEGIL` red kodu eklendi

Başkasının vadisini okumaya çalışmak önce `YETKISIZ` (401) dönüyordu. İstemciye
otomatik jeton yenileme eklendiği gün bu **sonsuz döngü** üretirdi: jeton geçerli,
yenilemek bir şey değiştirmiyor.

Ayrı kod eklendi, 403 dönüyor. `turetilmis.json` → `redKodlari.UYE_DEGIL`.

---

## 4. Bilerek yapılmayanlar

| Karar | Sebep |
|---|---|
| **AudioMixer varlığı yok** | AudioMixer bir asset'tir, çalışma zamanında oluşturulamaz. 4 kanal (Müzik/Ortam/Eylem/UI) kodda ayrı seviyelerle uygulandı; `SesYoneticisi.Mixer` alanına editörde bir mixer atanırsa otomatik bağlanır. Davranış aynı, cila aşamasında gerçek mixer'a taşınır. |
| **asmdef yok** | asmdef, paket assembly'lerini açıkça referans etmek zorunda. Input System kurulu değilse **hiçbir script derlenmez** ve teşhisi zor. Gerekçe: `dokuman/asmdef-karari.md` |
| **TextMeshPro yerine legacy Text** | TMP "Essential Resources" import adımı gerektiriyor. Dinamik OS yazı tipi sıfır asset ile çalışıyor. Cila aşamasında TMP'ye geçilir. |
| **ProjectSettings elle yazılmadı** | Yanlış YAML projeyi açılmaz yapar. Unity kendi üretti, `Amorland > Kurulumu Çalıştır` gerekli ayarları uyguladı ✅ |
| **Gerçek OAuth yok** | Dikey dilimde hesap bağlama şartı 12 haneli kurtarma koduyla karşılanıyor (B.19 zaten bu alternatifi tanımlıyor). |
| **Ses dosyaları sentetik** | B.23: organik sesler sentezle iyi çıkmaz, oraya girme. Bunlar yer tutucu; gerçekleri telefonla kaydedilecek (A.5). |

---

## 5. Elle test senaryoları

Sunucu + Unity açıkken sırayla:

### S1 — İlk açılış (2 dk)
1. Play → isim yaz, renk seç → **Vadiye gir**
2. Doğum yılı gir → **Devam**
3. **Beklenen:** tarla görünür, 4 karo, sol üstte "Sv 1", altta tohum çubuğunda "Buğday 4"
4. **Beklenen:** altın göstergesi **YOK** (Sv 5'e kadar gizli)

### S2 — Ek / sula / biç (5 dk)
1. Boş karoya dokun → karakter yürür, eker
2. **Beklenen:** karo koyulaşır, üstünde filiz belirir, tohum 4→3
3. Tekrar aynı karoya dokun → "Kova boş. Kuyudan doldur."
4. Tarla kenarındaki **dallara** dokun → odun topla (30 dk'da yenilenir)
5. Harita > Ev > Basit tezgâh > **Ahşap kova** (8 odun)
6. Kuyuya dokun → **Beklenen:** sağ altta "Kova 6/6"
7. Ekili karoya dokun → sulanır, karo daha da koyulaşır, kova 5/6
8. 5 dakika bekle → karo dolgunlaşır
9. Karoya dokun → hasat. **Beklenen:** 1 buğday + 1 tohum (başlangıç ürünü kuralı)

### S3 — Depo (3 dk)
1. Sv 2'ye gel (S2'yi tamamlayınca gelir)
2. Harita > Depo → **Beklenen:** "0 / 100 birim"
3. Çanta > **Depoya boşalt**
4. **Beklenen:** depoda yığın, yanında **senin renk rozetin**
5. **"Depodan al" toplu butonu OLMAMALI** (B.10 — tek yönlü kolaylık)

### S4 — İki oyuncu (10 dk)
1. Sv 3'e gel → Davet ekranı
2. **Hesabımı bağla** → kurtarma kodu çıkar, not al
3. **Davet kodu üret** → 6 hane
4. `Amorland > İkinci Oyuncu Kimliği Üret` → Play → kodu gir
5. **Beklenen:** ikinci oyuncu aynı vadide, kendi 4 karolu parseli var
6. İkinci oyuncu birinin parseline dokunsun → **"Bu parsel partnerin. Dokunamazsın."**
7. Biri depoya koysun, diğeri alsın → **anında görünmeli**

### S5 — Bağlantı kopması (2 dk)
1. Oyun açıkken sunucuyu **durdur** (Ctrl+C)
2. **Beklenen:** ekranda "Bağlantı yok / Yeniden bağlanıyor..."
3. Kendi karona ekmeyi dene → kuyruğa alınır
4. Depoyu açmayı dene → **"Bu işlem bağlantı gerektirir."**
5. Sunucuyu tekrar başlat
6. **Beklenen:** kuyruk gönderilir, durum senkronlanır

### S6 — Sürükle-boyama ve ses (2 dk)
1. Kovayı doldur, birkaç boş karo olsun
2. Parmağını (veya fareyi) basılı tutup karolar üzerinde sürükle
3. **Beklenen:** karolarda sarı noktalar, karakter sırayla dolaşıp eker
4. **Beklenen:** ses her tekrarda hafif farklı perdede, makineli tüfek değil
5. Herhangi bir yere dokun → kuyruk iptal

### S7 — Sessiz oyna (1 dk)
1. Ayarlar > tüm ses kanallarını %0 yap
2. Hasat et, alet kır, seviye atla
3. **Beklenen:** her olayın **görsel** karşılığı var (B.26 yasak 17)

---

## 6. Sıradaki adım — Hafta 2

Sırayı bozma (A.4):

1. **Perde 1'i tamamla (Sv 1-5)** — oyunun vitrini
   - Tren garı + manifesto + dinamik fiyat (formül hazır: `Deterministik.hash01`)
   - Turp/havuç/mısır/patates zaten denge dosyasında, sadece açılış akışı gerekiyor
   - **Eylem seslerini kendi telefonunla kaydet** (A.5) — sulama en çok duyulan ses
   - Defter sayfa 3-5 metinleri
2. Market + Orman (Sv 6-7)
3. Maden + craft (Sv 9-10)

⚠ **Perde 1 bitmeden Perde 3'e tek satır içerik üretme.**

### İlk ölçülecek sayı

**Depo kapasitesi.** A.7: "Dikey dilimde ölçülecek 1 numaralı sayı. Cömert yaparsan
tüm ekonomi tasarımı boşa gider." Şu an 100 birim (Sv 2). S3 senaryosunu birkaç gün
oynayıp **kaç hasatta dolduğunu** not et, sonra `ilerleme.json` → `depo.kademeler`
üzerinden ayarla ve `./mvnw test` ile denetimlerin hâlâ geçtiğini gör.
