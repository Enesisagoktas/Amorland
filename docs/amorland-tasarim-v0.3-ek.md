# Amorland — Tasarım Eki v0.3

> Bu dosya `amorland-tasarim-dokumani.md` (v0.2) ile birlikte okunur. v0.2'yi geçersiz kılmaz, **eksiklerini kapatır ve çelişkilerini çözer.**
> ✅ karar verildi · ⚠️ risk · 🔧 v0.2'de düzeltme
>
> Tarih: 2026-08-19

---

## 0. Bu ek ne kapatıyor

v0.2 sistem ve ekonomi tasarımında güçlü, **anlık etkileşim tasarımında boştu.** Ayrıca 19 açık madde ve içeride 7 çelişki vardı. Bu ek üçünü de kapatıyor:

| Bölüm | Kapattığı |
|---|---|
| 1 | Kamera ve sahne ölçeği |
| 2 | Hareket ve etkileşim modeli |
| 3 | Eylem süreleri + iyimser istemci |
| 4 | Ekran envanteri (UI iş yükü) |
| 5 | XP kaynakları ve seviye tablosu |
| 6 | Ürün tablosu (16 ürün, mutlak sayılar) |
| 7 | Ağaç, cevher, alet, craft tabloları |
| 8 | Çanta ve depo kapasiteleri |
| 9 | Ortak proje mekaniği (Sv 11 / 15) |
| 10 | Öğretici — ilk 15 dakika |
| 11 | Ev'in Sv 1-7 boşluğu |
| 12 | Tren fiyat üretimi (deterministik formül) |
| 13 | v0.2'deki 7 çelişkinin çözümü |
| 14 | 19 açık maddenin kapanışı |
| 15 | Bu ekte **bilerek** kapatılmayanlar |

---

## 1. Kamera ve sahne ölçeği ✅

Bu, dokümanda hiç yoktu ve **asset üretiminden önce kilitlenmesi gereken ilk karar.** Model ölçeği, silüet okunabilirliği ve arka yüz gereksinimi buradan çıkıyor.

| Parametre | Karar | Gerekçe |
|---|---|---|
| İzdüşüm | **Ortografik** | Izgara üzerinde karo boyutu ekranın her yerinde eşit. Perspektifte uzak karolar küçülür, dokunma hedefi bozulur. Ayrıca 3D→2D sprite render köprüsünü açık tutar. |
| Eğim (pitch) | **50°** | 90° (tam tepeden) nesnelerin yan yüzünü göstermez, ürün büyüme aşaması okunamaz. 30° fazla örtüşme yaratır. 50° hem zemini hem yan yüzü verir. |
| Dönüş (yaw) | **0°, kilitli** | Kamera dönmüyorsa modellerin arka yüzü hiç görünmez → **poligon ve doku bütçesi yarıya iner**, sprite ön-render mümkün olur. Bu tek karar asset bütçesinin en büyük kaldıracı. |
| Takip | Oyuncuyu takip, **ölü bölge** kutusu var | Ekran merkezinde ~3×2 karoluk kutu; oyuncu kutunun içinde gezerken kamera durur. Titremeyi engeller. |
| Yumuşatma | 0.15 sn gecikme | |
| Zoom | **2 kademe** (yakın / uzak), çimdik | Serbest zoom asset LOD varsayımlarını bozar ve oyuncunun işe yaramaz bir uzaklığa çıkmasına izin verir. |
| Ortografik boyut | Yakın: 5 · Uzak: 8 | Yakın ~10 karo, uzak ~16 karo dikey görüş. |

**Ölçek sözleşmesi:** 1 karo = **1 Unity birimi**. Karakter boyu ~1.6 birim. Ağaç 2.5-4 birim. Tüm asset üretimi bu ölçeğe göre yapılır.

⚠️ **Kamera kilitli olduğu için modellerin arkası boş olabilir, ama gölge alan yüzeyleri boş olamaz.** Sahte gölge (blob decal) kullanılacağı için gerçek gölge dökümü yok — sorun değil.

---

## 2. Hareket ve etkileşim ✅

Oyunun en çok kullanılan mekaniği. v0.2'de tek satır yoktu.

### 2.1 Hareket

| Girdi | Davranış |
|---|---|
| **Karoya dokun** (birincil) | Karakter oraya otomatik yürür ve bağlam eylemini yapar. Tek dokunuş = tek iş. |
| **Sanal joystick** (ikincil) | Ekranın sol üçte birinde **dinamik konumlu** — parmağı nereye koyarsan orada belirir. Sabit joystick farklı telefon boyutlarında elin altına gelmez. |
| Yürüme hızı | **4 karo/saniye** |
| Yol bulma | Basit ızgara A*. Sahneler küçük, maliyet önemsiz. |

### 2.2 Eylem modeli — bağlam duyarlı ✅

**Alet seçimi yok.** Oyuncu karoya dokunur, oyun ne yapılacağını kendi anlar:

| Karo durumu | Eylem |
|---|---|
| Boş toprak + tohum seçili | Çapala + ek (**tek eylem**, ayrı çapa adımı yok) |
| Ekili + susuz | Sula |
| Olgun | Hasat et |
| Ağaç | Kes (envanterdeki en iyi balta otomatik seçilir) |
| Cevher damarı | Kır (en iyi kazma otomatik) |
| Yabani kaynak | Topla |
| Kuyu | Kovayı doldur |

🔧 **v0.2 düzeltmesi:** Bölüm 7'deki "alet çubuğu 4 slot" artık **alet** çubuğu değil, **tohum/sarf seçici**. Aletler envanterde durur ve otomatik seçilir. Kırılan alet varsa bir alt kademeye otomatik düşer.

**Belirsizlik durumu:** Nadiren bir karoda iki eylem mümkün olur (meyveli ağaç: kes mi, topla mı). Çözüm: **uzun bas → 2-3 seçenekli dairesel menü.** Sadece belirsiz karolarda çıkar.

### 2.3 Toplu eylem — sürükle-boyama ✅

24 karoluk tarlada karo karo dokunmak işkence. Çözüm:

> **Basılı tut + sürükle** = parmağın geçtiği tüm karolara aynı eylemi **kuyruğa** al. Karakter sırayla yapar. Kuyruk ekranda küçük noktalarla gösterilir, herhangi bir yere dokununca iptal olur.

🔧 **v0.2 ile çelişmiyor:** Bölüm 7'deki "sürükle-bırak yapma" yasağı **envanterde eşyayı slota taşımak** içindi (parmak eşyayı örter). Tarladaki ızgara boyaması farklı bir jest, hedef parmağın altında değil önünde. İkisi bir arada olabilir.

### 2.4 Sulama modeli ✅ (açık madde #12)

**Kova kapasitesi var, kuyu tarla girişinde.** Sınırsız sulama, sulamayı anlamsız bir dokunuşa çevirirdi.

| Kova | Kapasite | Malzeme |
|---|---|---|
| Ahşap | 6 karo | 8 odun |
| Demir | 12 karo | 6 demir külçesi |
| Çelik | 20 karo | 8 çelik külçesi |
| Elmas | 40 karo | 6 çelik + 2 elmas |

- Kuyu **tarla sahnesinin içinde**, sahne değiştirmek gerekmiyor
- Doldurma: 1 saniye
- Kovalar **kırılmaz** (kap, alet değil)
- **Yağmurlama** (Sv 14 craft): kurulduğu karonun 4 komşusunu her büyüme çevriminde otomatik sular. Geç oyunda 24 karo elle sulamak dayanılmaz olur; bu onu çözer ve aynı zamanda çelik/elmas çıkışı.

**Mevsim/hava sistemi yok** (bkz. bölüm 14, madde #3/#18).

---

## 3. Eylem süreleri ve iyimser istemci ✅

v0.2'de büyüme süreleri vardı, **eylem** süreleri yoktu.

| Eylem | Süre | Not |
|---|---|---|
| Ekme | 0.4 sn | |
| Sulama | 0.4 sn | |
| Hasat | 0.6 sn | |
| Yabani toplama | 0.5 sn | |
| Ağaç kesme | vuruş × 0.7 sn | Vuruş sayısı: ağaç türü / balta kademesi |
| Cevher kırma | vuruş × 0.7 sn | Vuruş sayısı: kat / kazma kademesi |
| Kova doldurma | 1.0 sn | |

**Animasyon kilidi iptal edilebilir.** Oyuncu eylem ortasında başka yere dokunursa mevcut eylem biter, kuyruk değişir. Oyuncu asla kapana kısılmaz.

### İyimser istemci + sunucu mutabakatı ⚠️ (kritik)

Mobil gecikme 100-400 ms. Her eylem için sunucuyu beklersen oyun hissi ölür.

```
Oyuncu dokunur
   ↓
İstemci animasyonu ANINDA oynatır + yerel durumu geçici günceller
   ↓ (paralel)
Sunucuya niyet gider: {action, x, y, client_action_id}
   ↓
Sunucu doğrular → onay veya red
   ├─ onay → yerel durum kalıcılaşır
   └─ red  → geri sarılır + kısa bildirim ("Depo dolu")
```

- Geri sarma **görsel olarak yumuşak** olmalı (0.2 sn), sert zıplama değil
- `client_action_id` (UUID) her niyette zorunlu — v0.2 mimari kuralı 4
- İstemci **asla** sonucu hesaplamaz, sadece **tahmin eder**. Altın, XP, envanter miktarı her zaman sunucudan gelir.
- Bağlantı kopukken: eylemler yerel kuyruğa alınır, bağlantı dönünce sırayla gönderilir. Kuyruk 50 eylemle sınırlı, aşınca oyuncuya "bağlantı yok" ekranı gösterilir.

---

## 4. Ekran envanteri ✅

UI iş yükü bu listeyle ölçülür. **26 ekran + 1 kalıcı HUD.**

### Kalıcı HUD (bölge sahnelerinde)
Seviye + XP çubuğu · altın · çanta butonu · tohum seçici (4 slot) · harita butonu · sohbet balonu · partner göstergesi (çevrimiçi / çevrimdışı / küs)

### Ekranlar

| # | Ekran | Perde |
|---|---|---|
| 1 | Açılış + isim girişi (misafir hesap) | 1 |
| 2 | Yaş kapısı (mağaza uyumu) | 1 |
| 3 | Vadi haritası (7 bölge) | 1 |
| 4-10 | Bölge sahneleri × 7 | 1-2 |
| 11 | Çanta | 1 |
| 12 | Depo (+ "depoya boşalt") | 1 |
| 13 | Craft tezgâhı | 1 |
| 14 | Tren garı — manifesto kartları + fiyat tablosu + mini grafik | 1 |
| 15 | Market — al / sat sekmeleri | 2 |
| 16 | Defter (hikâye sayfaları) | 1 |
| 17 | Sohbet (metin + ifade tekerleği + not tahtası) | 1 |
| 18 | Partner daveti / kod girişi | 1 |
| 19 | Partner paneli (küslük, barışma, ayrılık) | 1 |
| 20 | Ortak proje katkı tahtası | 2 |
| 21 | Ayarlar (ses, dil, hesap, engelleme, gizlilik) | 1 |
| 22 | Rapor et akışı | 1 |
| 23 | Ödüllü reklam onayı | 2 |
| 24 | Seviye atlama kutlaması | 1 |
| 25 | Günlük sandık | 2 |
| 26 | Yükleme ekranı (bölge geçişi) | 1 |

**Perde 1 için gereken: 19 ekran.** Bu, v0.2'nin "Perde 1 oyunun vitrini" iddiasının gerçek maliyeti.

---

## 5. XP ve seviye tablosu ✅

v0.2 XP'yi sadece tren manifestosunda anıyordu. Tek kaynak olsaydı günde ~12 XP olayı olurdu ve 20 seviye 15-20 saate sığmazdı.

### 5.1 Seviye eşikleri

`Sv n → n+1 gereken XP = 100 × 1.2^(n-1)` (v0.2'nin "%20 bileşik" kuralı)

| Sv | XP | Sv | XP | Sv | XP | Sv | XP |
|---|---|---|---|---|---|---|---|
| 1→2 | 100 | 6→7 | 249 | 11→12 | 619 | 16→17 | 1.541 |
| 2→3 | 120 | 7→8 | 299 | 12→13 | 743 | 17→18 | 1.849 |
| 3→4 | 144 | 8→9 | 358 | 13→14 | 892 | 18→19 | 2.219 |
| 4→5 | 173 | 9→10 | 430 | 14→15 | 1.070 | 19→20 | 2.663 |
| 5→6 | 207 | 10→11 | 516 | 15→16 | 1.284 | | |

**Toplam: 15.476 XP.** ~35 günlük oynanışa yayılırsa günde ~440 XP.

### 5.2 XP kaynakları

| Kaynak | XP |
|---|---|
| Hasat | `büyüme_saati × 2`, en az 1 (5 dk→1, 2 sa→4, 6 sa→12, 12 sa→24) |
| Tren siparişi karşılama | **60** |
| Manifesto tam doldurma (3/3) | +100 bonus |
| Cevher düğümü | 3 |
| Ağaç kesme | 10 / 20 / 40 / 60 (çam / meşe / ceviz / kavak) |
| Yabani toplama | 1 |
| Craft — ilk kez | 30 |
| Craft — tekrar | 5 |
| Defter sayfası | 75 |
| Ortak proje tamamlama | 300 |
| Bölge ilk kez açma | 50 |

**Tren XP'nin baskın kaynağı olmalı** — v0.2'nin "tren ekonominin kalbi" tasarımını XP tarafında da doğruluyor.

⚠️ Bu tablo **sunucuda yapılandırma dosyası** olarak durur, koda gömülmez. Denge ayarı yeniden derleme gerektirmemeli.

---

## 6. Ürün tablosu — 16 ürün ✅

Saatlik getiri kademesi (v0.2'nin "uzun ürün ~%25 daha kârlı" kuralı, azalan artışla):

| Süre | altın / saat / karo |
|---|---|
| 5 dk | 24 |
| 30 dk | 30 |
| 2 sa | 36 |
| 6 sa | 42 |
| 12 sa | 48 |

Azalan artış bilinçli: 12 saatlik ürün otomatik en iyi seçenek olmamalı, yoksa karar ölür.

| # | Ürün | Açılış | Süre | Taban fiyat | Tohum | Verim | Özel |
|---|---|---|---|---|---|---|---|
| 1 | Buğday | Sv 1 (ödül) | 5 dk | 2 | 1 | 1 | Öğretici + un zinciri girdisi |
| 2 | Turp | Sv 2 (ödül) | 30 dk | 15 | 5 | 1 | |
| 3 | Havuç | Sv 3 (ödül) | 30 dk | 16 | 5 | 1 | |
| 4 | Mısır | Sv 4 (ödül) | 2 sa | 72 | 22 | 1 | İlk gerçek karar |
| 5 | Patates | Sv 5 (ödül) | 2 sa | 68 | 20 | 1 | |
| 6 | Domates | Sv 6 (market) | 2 sa | 75 | 24 | 1 | **Yeniden verir ×3** (2 sa ara) |
| 7 | Marul | Sv 7 (market) | 30 dk | 14 | 4 | 1 | |
| 8 | Kabak | Sv 8 (market) | 6 sa | 252 | 76 | 1 | |
| 9 | Çilek | Sv 10 (market) | 6 sa | 230 | 70 | 1 | **Yeniden verir ×4** (3 sa ara) |
| 10 | Ayçiçeği | Sv 11 (market) | 6 sa | 245 | 74 | 1 | |
| 11 | Lahana | Sv 12 (market) | 6 sa | 260 | 78 | 1 | |
| 12 | Karpuz | Sv 14 (market) | 12 sa | 576 | 175 | 1 | |
| 13 | Üzüm | Sv 15 (market) | 12 sa | 540 | 165 | 1 | **Yeniden verir ×3** (6 sa ara) |
| 14 | Balkabağı | Sv 16 (market) | 12 sa | 600 | 180 | 1 | |
| 15 | Kavun | Sv 18 (market) | 12 sa | 590 | 178 | 1 | Kısa pencere tohumu |
| 16 | Safran | Sv 19 (market) | 12 sa | 900 | 300 | 1 | Nadir, kısa pencere |

**Yeniden veren ürünler** (domates, çilek, üzüm) tek modelle gerçek strateji çeşitliliği sağlıyor — ek asset maliyeti sıfır.

**Buğday 2 altın:** kasıtlı. Görevi ekonomi değil, ilk 3 dakikada döngüyü öğretmek. Sonra değirmen zincirinin (un → ekmek) hammaddesi olarak geri döner.

🔧 **v0.2 düzeltmesi — ürünler seviyeyle açılmıyor:** v0.2'nin Bölüm 7b tablosu ürünleri seviyeye bağlıyordu, ama Bölüm 9'da Sv 6-20 arası **her seviye zaten başka bir şey açıyor.** "Her seviye tek bir yeni şey" kuralı çiğneniyordu. Çözüm: **Sv 1-5 ürünleri seviye ödülü, Sv 6+ tohumları market envanterine seviye eşiğiyle girer ve satın alınarak açılır.** Seviye tek şey açmaya devam eder, ürün çeşitliliği oyuncunun kararı olur, market de anlam kazanır.

### Gübre ✅ (açık madde #19)

- **Tek tip.** Kalite kademesi yok (Stardew tarzı kalite sistemi 4× UI ve asset demek).
- Etki: **+1 birim verim** (süreyi değiştirmez — süre değiştirseydi altın/saat ekonomisi şişerdi)
- Maliyet: ürünün taban fiyatının **~%70'i**, marketten alınır, craft yok
- Sv 12'de market envanterine girer

Bu oran bilinçli: gübre otomatik kârlı değil. **Tren fiyatı %100'ün üstündeyken** kullanılır. Yani gübre dinamik fiyat sistemine bağlanan üçüncü karar katmanı oluyor.

**Çapa yok** — bağlam duyarlı etkileşimde çapalama ekmenin içinde (bkz. 2.2).

---

## 7. Ağaç, cevher, alet, craft ✅

### 7.1 Ağaçlar (uzun ritim)

| Tür | Açılış | Büyüme | Vuruş | Getiri | Fidan |
|---|---|---|---|---|---|
| Çam | Sv 7 | 8 sa | 3 | 4 odun | 30 |
| Meşe | Sv 10 | 24 sa | 4 | 3 sert odun | 120 |
| Ceviz | Sv 13 | 48 sa | 5 | 2 değerli odun + 3 ceviz | 400 |
| Gümüş kavak | Sv 16 | 72 sa | 6 | 2 nadir odun | 1.200 |

**Kütük yok** ✅ (açık madde #10) — ağaç kesilince karo anında boşalır. Gerekçe: mobilde her fazladan dokunuş maliyet, ve ormanın ölü zaman problemi zaten kısa ritimle çözülüyor. "Bir adım daha" eklemeye gerek yok.

### 7.2 Yabani kaynaklar (kısa ritim)

Ormanın ölü bölge olmasını engelleyen sistem. Ayrıca **alet kilitlenmesine karşı güvenlik ağı** (bkz. 7.4).

| Kaynak | Yenilenme | Getiri | Nerede |
|---|---|---|---|
| Dal | 30 dk | 1-2 odun | Orman + tarla kenarı + maden girişi |
| Taş parçası | 30 dk | 1-2 taş | Orman + maden girişi |
| Mantar | 90 dk | 1 mantar (satış) | Orman |
| Yaban meyvesi | 2 sa | 1-2 meyve (satış + yem) | Orman |
| Yem otu | 45 dk | 1 yem | Orman + tarla kenarı |

### 7.3 Maden

| Kat | Gereken kazma | Düğümler | Vuruş | Getiri | Yenilenme |
|---|---|---|---|---|---|
| Kat 1 | Taş | Taş, kömür | 3 | 2-3 | 20 dk |
| Kat 2 | Demir | Kömür, demir | 4 | 2-3 | 30 dk |
| Kat 3 | Çelik | Demir, altın | 5 | 1-2 | 45 dk |
| Dip | Elmas uçlu | Altın, elmas | 6 | 1-2 | 60 dk |

🔧 **v0.2 çelişkisi çözüldü:** Bölüm 9'da "Sv 15 → Maden Kat 3 (**bakır, gümüş**)" yazıyordu, ama Bölüm 7b'nin hem maden tablosu hem "cevherlerin işlevi" tablosu sadece **4 cevher** tanımlıyor (kömür, demir, altın, elmas) ve Kat 3'ü "demir, altın" diyor. **Bakır ve gümüş yok.** Sv 15 girişi "Maden Kat 3 — altın" olarak düzeltiliyor. Beşinci ve altıncı cevher, işlevi olmayan cevher demekti — v0.2'nin kendi "işlevsiz ürün istemiyoruz" kuralına aykırıydı.

### 7.4 Aletler

| Alet | Kademe | Malzeme | Dayanıklılık | Açar |
|---|---|---|---|---|
| Taş kazma | 1 | 5 odun + 10 taş | 40 vuruş | Kat 1 |
| Demir kazma | 2 | 5 odun + 8 demir külçesi | 120 | Kat 2 |
| Çelik kazma | 3 | 5 sert odun + 10 çelik külçesi | 300 | Kat 3 |
| Elmas uçlu kazma | 4 | 5 değerli odun + 8 çelik + 3 elmas | 800 | Dip |
| Taş balta | 1 | 5 odun + 8 taş | 120 vuruş | Çam |
| Demir balta | 2 | 5 odun + 6 demir külçesi | 360 | Meşe |
| Çelik balta | 3 | 5 sert odun + 8 çelik külçesi | 900 | Ceviz |
| Elmas balta | 4 | 5 değerli odun + 6 çelik + 2 elmas | 2.400 | Kavak |

**Balta da kırılır** ✅ (açık madde #9) — ama dayanıklılığı kazmanın **3 katı**. Gerekçe: tutarlılık (kazma kırılıp balta kırılmazsa keyfi durur), ama odun taştan yumuşak ve orman düşük riskli bölge.

### ⚠️ Kilitlenme değişmezi — genel kural

v0.2 kazma kilitlenmesini çözmüştü. Balta eklenince aynı tuzak orada da açılıyor: **balta yok → odun yok → balta yapılamaz.**

> **Değişmez kural: Hiçbir alet, o aleti yapmak için gereken kaynağın tek edinme yolu olmamalı.**

Uygulaması, üçü de zorunlu:
1. **Yabani dal ve taş parçası elle toplanır** — hiçbir alet gerektirmez, 30 dakikada yenilenir, orman + tarla kenarı + maden girişinde her zaman var
2. **Taş kazma ve taş balta Sv 1'den itibaren evdeki tezgâhta craftlanabilir** (bkz. bölüm 11)
3. **Market ikisini de sabit ve çok ucuz satar** (Sv 6'dan sonra)
4. Dayanıklılık **%20'nin altına düşünce net bildirim** — sessizce kırılan alet öfke üretir

### 7.5 Craft tarifleri — tam liste (25/25) ✅

v0.2 sınırı: 25 tarif, en fazla 2 kademe derinlik. Liste tam dolu:

**Aletler (12):** 4 kazma · 4 balta · 4 kova (yukarıdaki tablolar)

**Ara malzeme (3):**
| Ürün | Girdi |
|---|---|
| Demir külçesi | 2 demir + 1 kömür |
| Çelik külçesi | 2 demir külçesi + 2 kömür |
| Altın külçesi | 2 altın + 1 kömür |

**İşleme (3):**
| Ürün | Girdi | Açılış | Fiyat çarpanı |
|---|---|---|---|
| Un | 3 buğday | Sv 13 (değirmen) | 2.5× |
| Ekmek | 2 un + 1 odun | Sv 13 | 3× |
| Peynir | 2 süt | Sv 14 | 2.8× |

**Yapı (1):**
| Ürün | Girdi | Açılış |
|---|---|---|
| Yağmurlama | 4 çelik külçesi + 1 elmas | Sv 14 |

**Ev eşyası (6):** Altın külçesi + değerli odun + elmas kombinasyonları. İşlevsel fayda yok, kozmetik + defter kaydı. Geç oyunun ana altın/elmas kuyusu.

⚠️ 25 dolu. Yeni bir tarif eklemek isteyen **bir tarif çıkarmak zorunda.** Bu sınır v0.2'de konmuştu, korunuyor.

---

## 8. Çanta ve depo ✅ (açık madde #8)

### 8.1 İki kısıt gerçekten farklı olmalı

🔧 **v0.2'de sessiz bir hata vardı:** Çanta yığını 99 ve depo da slot cinsindense, "depo doluluğu satış baskısı yaratır" mekaniği **asla tetiklenmez** — 8 slot × 99 = 792 birim, hiçbir üretim hızı bunu doldurmaz. Bölüm 8'in tüm ekonomik gerilimi buna dayanıyordu.

Çözüm — iki kısıt **farklı boyutları** ölçer:

| Kısıt | Ölçtüğü | Zorladığı |
|---|---|---|
| **Çanta** | Eşya **çeşidi** (slot sayısı, yığın 99) | Hareket — 9. çeşidi alınca depoya dönmelisin |
| **Depo** | Toplam **miktar** (birim) | Karar — dolunca satmalısın |

Çanta madende ısırır (taş, kömür, demir, altın, elmas + craft malzemesi = çeşit patlaması), depo hasat sonrası ısırır. v0.2'nin "küçük kısıt hareketi zorlar, büyük kısıt kararı zorlar" tasarımı ancak böyle çalışıyor.

### 8.2 Çanta

| Kademe | Slot | Açılış | Maliyet |
|---|---|---|---|
| Başlangıç | 8 | Sv 1 | — |
| I | 12 | Sv 4 | 400 |
| II | 16 | Sv 8 | 3.500 |
| III | 20 | Sv 14 | 28.000 |

Alet çubuğu ayrı 4 slot (tohum/sarf seçici, bkz. 2.2).

### 8.3 Depo

| Kademe | Kapasite (birim) | Açılış | Maliyet | ≈ tren seferi karşılığı |
|---|---|---|---|---|
| Başlangıç | 100 | Sv 2 | — | ~2.5 |
| I | 160 | Sv 6 | 1.200 | ~2.2 |
| II | 240 | Sv 11 | 9.000 | ~2.0 |
| III | 340 | Sv 15 | 45.000 | ~1.8 |
| IV | 460 | Sv 18 | 180.000 | ~1.7 |

- Büyüme **doğrusal** (+60, +80, +100, +120), maliyet **üstel**. v0.2 Bölüm 8 kuralı.
- Sefer karşılığı 2.5 → 1.7'ye düşüyor: **baskı oyun ilerledikçe artıyor.** Kasıtlı.
- UI: slot ızgarası değil, **doluluk çubuğu** + kategori filtresi

⚠️ **Bu tablo dikey dilimde ölçülecek 1 numaralı sayı.** v0.2 zaten bunu söylüyordu (#8). Buradaki değerler başlangıç noktası, gerçek sayı oyun testinden çıkacak.

**Depo dolu kuralı** (v0.2'den, değişmiyor): hasat **engellenir**, ürün tarlada bekler, net uyarı verilir. Eşya asla yok edilmez, yere düşmez, otomatik satılmaz.

---

## 9. Ortak proje mekaniği ✅ (Sv 11 asansör, Sv 15 köprü)

v0.2 bunları "oyunun sosyal doruk noktası" ilan ediyordu ama mekaniği tanımlamıyordu.

### Katkı tahtası

Proje = birkaç malzeme kalemi. Her oyuncu çantasından veya depodan yatırır.

```
MADEN ASANSÖRÜ                     Sv 11
─────────────────────────────────────────
Odun          ████████░░  160/200
Demir külçesi ██████░░░░  72/120
Taş           ██████████  80/80  ✓
Altın         ███░░░░░░░  1.500/5.000

    Ayşe ▓▓▓▓▓▓ %58      Mehmet ▓▓▓▓ %42
             ✓ iki imza da geçerli
```

| Kural | Karar |
|---|---|
| Katkı geri alınabilir mi | **Hayır.** Yatırmadan önce onay diyaloğu. |
| İki taraflı katkı | **Her oyuncu toplam katkının en az %25'ini yapmalı.** Bar dolsa bile tek imza varsa proje tamamlanmaz: "ikinci imza eksik". |
| Süre kilidi | **Yok.** Malzeme tamamlanınca biter. İki haftalık malzeme toplamanın üstüne bir de sayaç koymak ceza olur. |
| Tek kişilik | Malzeme maliyeti **%70**, %25 kuralı uygulanmaz. Tren manifestosunda karşılığı daha az (v0.2 hikâye kuralı). |
| Ödül | Bölge/kat açılır + defter sayfası + 300 XP |

**%25 kuralı doğrudan hikâyeden geliyor:** "Tren manifestosu iki imza ister." Mekanik = hikâye.

Partnerin katkı çubuğunu görmek, bu ekranın asıl sosyal ödülü. Çevrimdışıyken bile "o da uğraşıyor" hissi buradan geliyor.

---

## 10. Öğretici — ilk 15 dakika ✅

v0.2: "Sv 5 oyunun kaderi, ilk 15 dakika en cilalı kısım olmalı." Ama o kısım hiç tasarlanmamıştı.

⚠️ **Netleştirme:** İlk 15 dakika **Sv 5'e ulaşmak değildir.** Perde 1 ~1.5-2 saat. İlk 15 dakikanın işi oyuncuyu **partner davetine** kadar getirmek.

| Dakika | Olay |
|---|---|
| 0:00 | Açılış. Misafir hesap (cihaz ID), kayıt yok. Tek alan: isim. |
| 0:15 | Yaş kapısı (mağaza uyumu, bir kez) |
| 0:30 | Vadi haritası. Sadece **Ev** aktif, diğer 6 bölge silik. |
| 0:45 | Eve gir. Masada parlayan defter. Dokun → **1. sayfa:** "Bu vadide hiçbir iş tek başına görülmez." |
| 1:10 | Defter görev verir: "Tarlaya git." |
| 1:20 | Tarla. 4 karo. Çantada 4 buğday tohumu. |
| 1:30 | Bir karo parlar. Dokun → karakter yürür, çapalar + eker. **"Dokun, ekilir."** |
| 1:50 | Kuyu parlar → kova dolar. Karolara dokun → sulanır. **Sürükle-boyama ipucu burada.** |
| 2:10 | "Buğday 5 dakikada büyür." Sayaç görünür. |
| 2:20 | Tarla kenarındaki 3 yabani kaynak parlar → topla. **Bekleme süresi boş geçmiyor.** |
| 3:00 | İlk hasat. XP çubuğu ilk kez dolar. |
| 3:30 | **Sv 2 → Depo açılır.** "Depoya boşalt" butonu öğretilir. |
| 4:00 | Defter 2. sayfa. Öğretici biter, serbest oyun. |
| 4:00-10:00 | Serbest: 4 karo döngüsü, turp açılır, seviye 3'e yaklaşır |
| ~10:00 | **Sv 3 → Partner daveti.** Tam ekran, kaçırılamaz, ama atlanabilir ("sonra"). Oyunun en önemli dönüşüm anı. |
| 10:00-15:00 | İfade tekerleği + metin sohbeti tanıtılır (partner varsa) |

**Kurallar:**
- Öğretici **atlanabilir olmamalı** ama 4 dakikadan uzun sürmemeli
- Zorla el tutma yok — parlayan hedef + tek satır metin, modal diyalog değil
- **İlk 30 dakikada reklam yok**
- Bağlantı gerektiği baştan söylenir (sunucu otoritesi, çevrimdışı oynanmaz)

---

## 11. Ev — Sv 1-7 boşluğu ✅

v0.2'de ev Sv 1'de açılıyordu ama craft tezgâhı Sv 10, kümes Sv 8'di. Yani **7 seviye boyunca ev = defterin durduğu boş oda.**

Ayrıca gizli bir tutarsızlık: **maden Sv 9'da açılıyor, tezgâh Sv 10'da.** v0.2'nin kilitlenme çözümü "taş kazma her zaman bedava craftlanabilir olmalı" diyordu ama Sv 9'da craftlanacak tezgâh yoktu.

🔧 **Düzeltme — tezgâh Sv 1'e alınıyor:**

| Sv | Evde ne var |
|---|---|
| **1** | Defter + **basit tezgâh (3 tarif: taş kazma, taş balta, ahşap kova)** |
| 2 | Depo (evin yanı) |
| 8 | Kümes |
| **10** | **Tezgâh yükseltmesi** — metal hattı açılır (külçe, demir/çelik aletler) |
| 13 | Değirmen (evin yanı) |
| 14 | Ahır (inek) |
| 17 | Ev genişletme + kozmetik yerleşim |

Böylece: ev hiç boş kalmıyor, kilitlenme çözümü baştan çalışıyor, Sv 10 hâlâ "tek yeni şey" açıyor (tezgâhın kendisi değil, metal hattı).

---

## 12. Tren fiyat üretimi ✅

v0.2: "deterministik (tohum + sefer numarası), veritabanına yazılmaz, banda %70-130." Formül yoktu.

### Sefer numarası
```
seferNo = floor((sunucuZamanı - EPOCH) / 6 saat)
EPOCH = 2026-01-01T00:00:00Z
```
Seferler 00:00 / 06:00 / 12:00 / 18:00 **UTC**. İki oyuncu farklı saat diliminde olabilir.

### Fiyat çarpanı — ortalamaya dönen rastgele yürüyüş

⚠️ **Saf rastgele kullanma.** v0.2 "son 3 seferin mini grafiği" gösterilecek diyor — ama saf rastgelede okunacak bir eğilim yoktur, grafik gürültü olur. Oyuncunun "yükseliyor, bekleyeyim" diyebilmesi için fiyatın **hafızası** olmalı.

```
p[0] = 1.0
p[n] = clamp(0.70, 1.30,
             p[n-1] + (1.0 - p[n-1]) × 0.30      // ortalamaya dönüş
                    + (hash01(tohum, ürünId, n) - 0.5) × 0.24 )   // gürültü

fiyat = tabanFiyat × p[n]
```

- `hash01` = SHA-256(tohum ‖ ürünId ‖ n) ilk 8 bayt / 2^64 → [0,1)
- Tamamen **durumsuz ve deterministik**: `p[n]` son 50 seferden ileri sararak hesaplanır, veritabanına satır yazılmaz
- Ortalamaya dönüş katsayısı 0.30 → fiyat 2-3 seferde bandın ortasına çekilir, ama gürültü eğilim üretmeye yeter
- Her ürün kendi bağımsız yürüyüşünü yapar

### Sipariş kartları ve tohum

Aynı deterministik kaynaktan:
- Her seferde **3 sipariş kartı**: ürün + miktar, oyuncunun seviyesinde açık ürünlerden seçilir
- Her seferde **2-3 çeşit tohum**, marketten %30-40 ucuz, **sınırlı adet** (v0.2 kuralı)
- Manifesto çarpanı: 1.6-2.0× · küslükte 1.3× · tek kişilikte kontenjan %70

---

## 13. v0.2'de bulunan çelişkiler ve çözümleri 🔧

| # | Çelişki | Çözüm | Bölüm |
|---|---|---|---|
| 1 | Kat 3 cevheri: Bölüm 7b "demir, altın" · Bölüm 9 "bakır, gümüş" | Bakır/gümüş yok. 4 cevher kalıyor. Sv 15 → "Kat 3, altın". | 7.3 |
| 2 | Ürünler seviyeyle açılıyor ama her seviye zaten başka bir şey açıyor ("tek yeni şey" kuralı çiğneniyor) | Sv 1-5 seviye ödülü, Sv 6+ market envanterinden satın alma | 6 |
| 3 | Maden Sv 9, craft tezgâhı Sv 10 → Sv 9'da "her zaman craftlanabilir taş kazma" yok | Tezgâh Sv 1'e, 3 tarifle. Sv 10 = tezgâh yükseltmesi. | 11 |
| 4 | Çanta yığını 99 + depo slot cinsinden → "depo doluluğu baskısı" hiç tetiklenmez | Çanta = çeşit kısıtı, depo = miktar kısıtı | 8.1 |
| 5 | Alet çubuğu 4 slot ama etkileşim modeli tanımsız | Bağlam duyarlı eylem; 4 slot = tohum/sarf seçici, aletler otomatik | 2.2 |
| 6 | "Sürükle-bırak yapma" vs 24 karolu tarlada toplu ekim | Yasak envantere ait; tarlada sürükle-**boyama** farklı jest | 2.3 |
| 7 | Sv 3 üç şey açıyor (davet + ifade tekerleği + sohbet), "tek yeni şey" kuralına aykırı | Kabul: bunlar tek bir özellik kümesi (iletişim). Kural korunuyor. | — |

---

## 14. 19 açık maddenin kapanışı ✅

| # | Konu | Karar |
|---|---|---|
| 1 | "Amorland" isim müsaitliği | ⏳ **Açık kalıyor** — bu bir hukuk/araştırma işi, tasarım kararı değil. Marka, `com.*.amorland` paket adı, alan adı ve iki mağaza araması yapılmalı. |
| 2 | Reklam yerleşimi | ✅ v0.2 taslağı onaylandı + **günde en fazla 5 ödüllü reklam**, **ilk 30 dakikada reklam yok** |
| 3 | Mevsim sistemi | ✅ **Yok.** 4 mevsim = 4 zemin paleti + her ürüne mevsim etiketi + mevsim dışı ürün kuralları. 0 TL bütçede karşılanamaz. Ayrıca gerçek zamanlı büyümeyle çelişir (12 saatlik ürün mevsim sınırını aşarsa ne olur?). |
| 4 | Kozmetik kaynağı | ✅ **Sadece oyun içi** (craft + defter kilometre taşları). Reklamla kozmetik verirsen, reklam izleyen partner ile izlemeyen arasında görsel eşitsizlik doğar — çift oyununda kabul edilemez. |
| 5 | Ev genişletme kapsamı | ✅ **Görsel + kozmetik yerleşim alanı. Depo kapasitesine dokunmaz.** Depo kapasitesi ekonominin can damarı; oraya ikinci bir büyüme kanalı açmak Bölüm 8'in tüm dengesini bozar. |
| 6 | Küslük mekaniği detayı | ✅ v0.2 Bölüm 5 yeterli. Kırmızı çizgi korunuyor: **hareketsizlik asla tetiklemez, bildirim asla gitmez.** |
| 7 | Çevrimdışı mesaj | ✅ **B — teslimat kuyruğu.** Teslim edilince anında silinir, edilmezse 7 gün sonra otomatik silinir. Arşiv değil, kuyruk. |
| 8 | Depo kapasite sayıları | ✅ Tablo verildi (8.3). Dikey dilimde ölçülecek **1 numaralı sayı**. |
| 9 | Balta dayanıklılığı | ✅ **Kırılır**, kazmanın 3 katı dayanıklılıkta. Kilitlenme değişmezi (7.4) baltayı da kapsıyor. |
| 10 | Ağaç kesildikten sonra | ✅ **Kütük yok**, karo anında boşalır. Mobilde her fazladan dokunuş maliyet. |
| 11 | Ortak düğüm çakışması | ✅ **Paylaşımlı düğüm sağlığı.** İki oyuncunun vuruşu aynı havuzdan iner, düğüm daha hızlı kırılır. **Katkısı %25'in üstünde olan her oyuncu tam getiriyi alır** (bölünmez — birlikte çalışmak asla tek başına çalışmaktan kötü olmamalı). Yenilenme süresi değişmez. Sunucu otoritesi: her vuruş bir niyet, sunucu sağlığı düşürür ve yayınlar. |
| 12 | Sulama sınırı | ✅ **Kova kapasitesi var, kuyu tarla içinde.** 4 kademe kova + Sv 14 yağmurlama. (2.4) |
| 13 | Başlangıç ürünleri | ✅ v0.2'nin kendi önerisi kabul: **Sv 1'de 1 ürün, her seviyede +1, Sv 5'te beşi açık.** |
| 14 | Vadi anıtı | ✅ **Eklendi.** Sv 17'de açılır, 5 kademe, işlevsel fayda yok. Tek model + malzeme/ölçek değişimi (ucuz asset). Maliyet: 10k / 30k / 80k / 200k / 500k altın + artan elmas. Geç oyunun para kuyusu. |
| **15** | **Sanat yönü** | ✅ **Low-poly 3D + ortografik kilitli kamera.** Gerekçe aşağıda. |
| 16 | Enerji barı | ✅ **Yok.** Oyunda zaten üç kısıt var (çanta, depo, zaman). Dördüncüsü kaçınılmaz olarak reklam kapısına dönüşür — v0.2 Bölüm 12 bunu açıkça yasaklıyor. Ayrıca enerji barı, çift oyununun tam olarak teşvik etmek istediği şeyi (birlikte uzun oturum) cezalandırır. |
| 17 | Premium para birimi | ✅ **Yok.** Elmas madenden çıkan bir kaynak; satın alınabilir yapmak hem madenciliği hem ekonomiyi çürütür. Oyun ücretsiz + sadece ödüllü reklam. |
| 18 | Mevsim / oyun içi gün | ✅ **Oyun içi takvim yok.** Sv 16'daki "kısa mevsimli toplama" → **"kısa pencereli toplama"**: sunucunun deterministik hesapladığı, günde 2-3 kez açılan 2 saatlik pencerelerde beliren nadir kaynaklar. Aynı zamanlama baskısı, sıfır takvim altyapısı. |
| 19 | Çapa ve gübre | ✅ **Çapa yok** (ekmenin içinde). **Gübre var**, tek tip, +1 verim, marketten, Sv 12. (6) |

### #15 Sanat yönü — gerekçe ⚠️

**Low-poly 3D, ortografik kilitli kamera.** Dört sebep:

1. **CC0 kaynak gerçekliği.** Kenney, Quaternius, KayKit, Poly Pizza bu ölçekte low-poly 3D çiftlik/doğa varlığı veriyor. Aynı kapsamda, tek stilde, ücretsiz **pixel art** çiftlik seti yok.
2. **Parametrik üretim sadece 3D'de çalışıyor.** 16 ürün × 4 aşama = 64 model, tek Blender script'iyle garantili tutarlı üretilebilir. Pixel art'ın çekiciliği sanatçının elidir — burada sanatçı eli yok, ve AI pixel art tutarsız.
3. **Kamera kilitli olduğu için 2D'ye dönüş yolu açık.** Fikrin değişirse aynı modelleri ortografik render edip sprite sheet çıkarırsın. Tek boru hattı, iki sonuç.
4. **Low-poly tutarsızlığı affeder.** Pixel art'ta iki farklı elin çizdiği iki nesne anında belli olur; düz renkli low-poly'de aynı palet ve aynı poligon yoğunluğu kullanıldığı sürece belli olmaz.

**Stil sözleşmesi** (asset üretiminin kuralı olacak):
- Düz renk, doku yok veya tek atlas (gradyan yok, PBR yok)
- 12-16 renkli sabit palet, tüm varlıklar bu paletten
- Kenar yumuşatma yok, sert normal
- Ürün modeli 150-400 üçgen, ağaç 400-900, karakter 1.200-2.000
- Sahte gölge (blob decal), gerçek gölge dökümü yok

---

## 15. Bu ekte bilerek kapatılmayanlar

Dürüst kapsam beyanı — aşağıdakiler **eksik değil, ertelenmiş**:

| Konu | Neden ertelendi | Ne zaman |
|---|---|---|
| **Sunucu veri modeli + WebSocket protokolü** | Kendi başına bir doküman. Tablo şemaları, mesaj tipleri, yeniden bağlanma/senkron akışı, JWT ve oturum yönetimi. | Dikey dilimden hemen önce, ayrı doküman |
| **İsim müsaitliği (#1)** | Tasarım kararı değil, hukuk/araştırma işi | Yayın kararından önce |
| **Ses tasarımı** | v0.2 yapım sırası adım 7'ye koymuş, doğru | Perde 3 sonrası |
| **Yerelleştirme** | v1 Türkçe + İngilizce, altyapı baştan (string tablosu), içerik sonra | Cila aşaması |
| **Hikâye metinleri** | 20 defter sayfasının gerçek metni. Tasarım değil, yazarlık işi. | Perde perde |
| **Kesin denge sayıları** | Buradaki tüm sayılar **başlangıç noktası.** Gerçek değerler oyun testinden çıkacak. | Dikey dilim + Perde 1 testi |

⚠️ **Sayılar hakkında:** Bu ekteki her tablo sunucuda **yapılandırma dosyası** olarak durmalı, koda gömülmemeli. Denge ayarı yeniden derleme ve mağaza güncellemesi gerektirmemeli. `docs/denge/*.json` bunun için var.

---

## 16. Güncellenen seviye tablosu

v0.2 Bölüm 9'un bu ekle uyumlu hali. Değişenler **kalın**.

### Perde 1 — Ev kurulur (Sv 1-5) · ~1.5-2 saat

| Sv | Açılan |
|---|---|
| 1 | Ev + Tarla (4 karo) + buğday + **basit tezgâh (3 tarif)** + çanta 8 slot |
| 2 | Depo (100 birim) + turp |
| 3 | Partner daveti + ifade tekerleği + metin sohbeti + havuç |
| 4 | Mısır + çanta 12 slot |
| 5 | **Tren garı** — ilk manifesto, 6 saatlik tarife + patates |

### Perde 2 — Vadi açılır (Sv 6-12) · ~5-7 saat

| Sv | Açılan |
|---|---|
| 6 | **Market** (tohum, alet, çanta/depo yükseltmesi) + depo 160 |
| 7 | **Orman** — balta, çam, yabani kaynaklar |
| 8 | Kümes (günlük yumurta) + çanta 16 slot |
| 9 | **Maden ocağı** — Kat 1, taş kazma, ilk defter sayfası |
| 10 | **Tezgâh yükseltmesi** — metal hattı (külçe, demir aletler) + meşe |
| 11 | **Ortak proje: maden asansörü** + depo 240 |
| 12 | Maden Kat 2 — demir, fener dayanıklılığı + gübre marketle |

### Perde 3 — Defter tamamlanır (Sv 13-20) · ~7-9 saat

| Sv | Açılan |
|---|---|
| 13 | Değirmen: buğday → un → ekmek + ceviz |
| 14 | İnek, süt → peynir + çanta 20 slot + yağmurlama |
| 15 | **Ortak proje: köprü** → Maden **Kat 3 (altın)** + depo 340 |
| 16 | Orman derinliği — gümüş kavak + **kısa pencereli** nadir toplama |
| 17 | Ev genişletme + kozmetikler + **vadi anıtı** açılır |
| 18 | Maden dibi (elmas) + depo 460 |
| 19 | Büyük manifesto hazırlığı |
| 20 | **Amorland tarifeye döner** — final, defter imzalanır |

---

## 17. Değişen teknik kararlar

v0.2 Bölüm 15'e ekleme:

| Konu | Karar |
|---|---|
| **Unity sürümü** | **6000.3.x LTS (Unity 6.3)** — en güncel yama. ⚠️ 6000.0 LTS desteği **Ekim 2026'da bitiyor**, kullanılmayacak. |
| Kamera | Ortografik, 50° pitch, 0° yaw kilitli |
| Ölçek | 1 karo = 1 Unity birimi |
| Etkileşim | Bağlam duyarlı, iyimser istemci + sunucu mutabakatı |
| Denge verisi | Sunucuda JSON yapılandırma, koda gömülmez |
| Sanat | Low-poly 3D, düz renk, 12-16 renkli sabit palet |
