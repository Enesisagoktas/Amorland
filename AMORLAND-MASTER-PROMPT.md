# AMORLAND — Tam Oyun Spesifikasyonu (Master Prompt)

> Bu dosya oyunun **tek doğruluk kaynağıdır.** Kendi kendine yeter — başka dokümana bakmadan oyun buradan inşa edilebilir.
> Sürüm 1.1 · 2026-08-19 · v0.2 + v0.3 + v0.4 + v0.5 (ses) birleştirilmiş hali
> Sayısal tablolar `docs/denge/*.json` ile birebir aynıdır. Çakışma olursa **JSON geçerlidir.**

---

## 0. Görev

**Amorland**, çiftler için tasarlanmış, üstten bakışlı 2.5D mobil çiftlik oyunudur. Android + iOS, yatay, ücretsiz.

İki kişilik ortak oynanış ana odaktır; tek kişilik de oynanabilir ama **daha zordur ve bu bir denge ayarı değil, dünyanın kuralıdır.**

- ~15-20 saat oynanış (yarısı gerçek zamanlı bekleme), 20 seviye, ~16 gün takvim süresi
- Oturum modeli: günde 3-4 kez, 5-10 dakika
- Gelir modeli: sadece ödüllü reklam. Zorunlu reklam yok, banner yok, premium para birimi yok.

---

## 1. Hikâye

Amorland, vadinin eski adı. Buranın tek bir kuralı vardı: **hiçbir iş tek başına görülmez.**

- Tren manifestosu iki imza ister
- Madende ikinci bir el olmadan derine inilmez
- Tarlada biri eker, biri sular

Vadi boşaldı çünkü son çift ayrıldı ve manifestoyu bir daha kimse ikinci kez imzalamadı.

**Anlatım aracı:** Evde yarım kalmış bir "birlikte yapılanlar defteri". 20 sayfa, her biri bir olayla açılır.
**Ara sahne yok. Diyalog ağacı yok. Seslendirme yok.** Anlatım tamamen toplanabilir metin sayfalarıyla yürür.

**Final:** Amorland tren tarifesine geri döner, defterin son sayfası oyuncuların adıyla imzalanır.

---

## 2. Teknik yığın

| Katman | Seçim |
|---|---|
| İstemci | **Unity 6.3 LTS (6000.3.x, en güncel yama)** + URP + New Input System + Addressables |
| Sunucu | Spring Boot, Windows makinede, önünde Caddy |
| Veri | **PostgreSQL** (dünya, tarla, envanter, depo, oyuncu, eşleşme, ortak proje) |
| Önbellek | Redis (oturum, presence, sohbet hız sınırı, teslimat kuyruğu) — *zorunlu değil, Postgres'te de çözülebilir* |
| Kanallar | REST + JWT (meta), WebSocket (anlık senkron + metin sohbeti) |
| Ses sohbeti | **v2.** Photon Voice 2 veya Agora. Kendi WebRTC yazılmayacak. Bas-konuş. |

⚠️ Unity **6000.0 LTS kullanılmaz** — desteği Ekim 2026'da biter.

### Performans hedefleri
- Orta segment 60 FPS, alt segment 30 FPS geri düşüş
- Doku atlası + GPU instancing + statik batching
- Gerçek zamanlı gölge yok — sahte gölge (blob decal)
- Bölge sahneleri Addressables ile asenkron
- İndirme boyutu **150 MB altı**

---

## 3. Değişmez mimari kuralları

Bu sekiz kural asla çiğnenmez:

1. **Otorite tamamen sunucuda.** İstemci niyet gönderir (`{"action":"harvest","x":12,"y":7}`), sunucu sonucu hesaplar. İstemci asla "500 altın kazandım" demez.
2. **Ürün büyütmek için cron yok.** `planted_at` saklanır, büyüme aşaması okuma anında hesaplanır.
3. **Tren fiyatları deterministik üretilir** (tohum + sefer numarası). Veritabanına satır yazılmaz.
4. **Her komut `client_action_id` (UUID) taşır.** Sunucu tekrarı yutar. Mobilde bağlantı sürekli kopar.
5. **Tüm zaman hesapları sunucu saatiyle.** İstemci saatine asla güvenilmez.
6. **Sohbet içeriği sunucuda saklanmaz.** Mesajlar cihazlarda durur. Engelleme listesi sunucuda.
7. **İyimser istemci.** İstemci animasyonu anında oynatır ve durumu *tahmin eder*; sunucu onaylar veya geri sardırır (0.2 sn yumuşak). İstemci asla hesaplamaz.
8. **Denge sayıları koda gömülmez.** JSON yapılandırma olarak sunucuda yüklenir. Denge ayarı yeniden derleme ve mağaza güncellemesi gerektirmez.

---

## 4. Dünya yapısı — 7 bölge

Üstten bakışlı bir **vadi haritası**. Bölgeye dokunursun, o sahneye girersin. Her bölge ayrı bir Unity sahnesi; aynı anda tek sahne yüklü.

| Bölge | İşlev | Açılış |
|---|---|---|
| **Ev** | Üs, tezgâh, defter, kümes, ahır, değirmen, kozmetik | Sv 1 |
| **Tarla** | Ekim, sulama, hasat, kuyu | Sv 1 |
| **Depo** | Ortak sandık — partner değişiminin yeri | Sv 2 |
| **Tren garı** | Satış, manifesto, fiyat tablosu | Sv 5 |
| **Market** | Tohum, alet, gübre, çanta/depo yükseltmesi | Sv 6 |
| **Orman** | Odun, yem, yabani toplama | Sv 7 |
| **Maden ocağı** | Cevher, 4 kat, defter sayfaları | Sv 9 |

Bölge geçişinde 0.5-1 sn kararma + asenkron yükleme. Gizlemeye çalışma.

⚠️ **Depo'nun özel rolü:** çiftin fiziksel buluşma noktası. Partner çevrimdışıyken bile "sana demir bıraktım" iletişimi burada olur. Asenkron ortak oynanışın kalbi — basit bir envanter ekranı gibi tasarlanmaz.

---

## 5. Kamera ve ölçek

| Parametre | Değer |
|---|---|
| İzdüşüm | **Ortografik** |
| Eğim (pitch) | **50°** |
| Dönüş (yaw) | **0°, KİLİTLİ** |
| Takip | Oyuncuyu takip, 3×2 karoluk ölü bölge kutusu, 0.15 sn yumuşatma |
| Zoom | 2 kademe — yakın: orto 5 · uzak: orto 8. Çimdik. Serbest zoom yok. |
| **Ölçek** | **1 karo = 1 Unity birimi.** Karakter 1.6 birim. Ağaç 2.5-4 birim. |

⚠️ **Yaw kilidi asset bütçesinin en büyük kaldıracıdır:** modellerin arkası hiç görünmez (poligon/doku yarıya iner) ve gerekirse aynı modellerden ortografik sprite render edilebilir.

---

## 6. Etkileşim modeli

### Hareket
| Girdi | Davranış |
|---|---|
| **Karoya dokun** (birincil) | Karakter otomatik yürür + bağlam eylemini yapar |
| **Sanal joystick** (ikincil) | Sol üçte bir, **dinamik konumlu** (parmağı koyduğun yerde belirir) |
| Yürüme hızı | 4 karo/saniye |
| Yol bulma | Izgara A* |

### Eylem — bağlam duyarlı, ALET SEÇİMİ YOK

| Karo durumu | Eylem |
|---|---|
| Boş toprak + tohum seçili | Çapala + ek (**tek eylem**, ayrı çapa yok) |
| Ekili + susuz | Sula |
| Olgun | Hasat et |
| Ağaç | Kes (en iyi balta otomatik) |
| Cevher damarı | Kır (en iyi kazma otomatik) |
| Yabani kaynak | Topla |
| Kuyu | Kovayı doldur |

Belirsiz karolarda (meyveli ağaç) **uzun bas → dairesel menü**.

**Ekrandaki 4 slotluk çubuk = tohum/sarf seçici**, alet çubuğu değil. Aletler envanterde durur, otomatik seçilir, kırılınca alt kademeye düşer.

### Toplu eylem
**Basılı tut + sürükle = boyama.** Parmağın geçtiği karolara aynı eylemi kuyruğa alır. Kuyruk noktalarla gösterilir, herhangi bir yere dokununca iptal.

⚠️ **Envanterde sürükle-bırak YASAK** (parmak eşyayı örter). Envanterde **dokun-seç → dokun-hedef**. Tarladaki boyama farklı bir jesttir, çelişmez.

### Eylem süreleri (saniye)
| Eylem | Süre |
|---|---|
| Ekme / sulama | 0.4 |
| Hasat | 0.6 |
| Yabani toplama | 0.5 |
| Vuruş başına (ağaç/cevher) | 0.7 |
| Kova doldurma | 1.0 |

Animasyon kilidi **iptal edilebilir**. Oyuncu asla kapana kısılmaz.

---

## 7. Ortak oynanış

### Temel kural — ortak dünya, ayrı tarlalar
Depo, maden, orman, gar, market **ortak**. Tarla iki parsele bölünür, **kimse diğerinin parseline dokunamaz.**

Tek kişilik ayrı bir mod değildir: aynı dünya, iki parsel de sende. **Sıfır ekstra içerik.**

### İki seviye kavramı
| Kavram | Belirler | Hesap |
|---|---|---|
| **Dünya seviyesi** | Hangi bölge/kat açık | İki oyuncunun **yükseği** |
| **Oyuncu seviyesi** | Kendi parseli, çantası, tohumları | Kişisel XP |

**Yetişme bonusu:** Oyuncu partnerinden 3+ seviye gerideyse XP'si **×1.5**. Fark 2'ye düşünce kapanır.

### Tek kişilik denge — %70
| | |
|---|---|
| **Ölçeklenir** | Tren manifesto kontenjanı, ortak proje malzeme maliyeti, maden asansörü katkı eşiği |
| **Ölçeklenmez** | Ürün büyüme süresi, fiyatlar, XP, düğüm sağlığı, alet dayanıklılığı |

### Eşleşme
```
İlk açılış → misafir hesap (cihaz ID), kayıt ekranı yok, isim + 4 renkten biri
   ↓
"Amorland'e nasıl gireceksin?"
   ├─ Tek başıma → oyun başlar, partner sonra davet edilebilir
   └─ Eşimle → 6 haneli davet kodu + deep link → partner aynı vadiye düşer
```
- **Rastgele eşleşme YOK.** Kalıcı ortak dünyada yabancı = oyunun ölümü + taşınamaz moderasyon yükü.
- Partner daveti **Sv 3'te** açılır.
- Partner davet ederken **hesap bağlama zorunlu** (artık başkasının ilerlemesi sana bağlı).

### Ortak düğüm çakışması
İki oyuncu aynı cevher/ağaca vurursa: **paylaşımlı sağlık.** Vuruşlar aynı havuzdan iner, düğüm daha hızlı kırılır. **Katkısı %25 üstünde olan HER oyuncu tam getiriyi alır** (bölünmez). Yenilenme süresi değişmez.

> Birlikte çalışmak asla tek başına çalışmaktan kötü olmamalı.

### Ortak projeler (Sv 11 asansör, Sv 15 köprü)
Katkı tahtası: malzeme kalemleri + iki oyuncunun katkı çubuğu.

| Kural | Karar |
|---|---|
| Katkı geri alınabilir mi | **Hayır** (onay diyaloğu) |
| İki taraflı katkı | **Her oyuncu toplamın en az %25'ini yapmalı.** Bar dolsa bile tek imza varsa: "ikinci imza eksik" |
| Süre kilidi | **Yok** |
| Tek kişilik | Maliyet %70, %25 kuralı uygulanmaz |
| Ödül | Bölge/kat + defter sayfası + 300 XP |

### Partner bırakırsa
| Süre | Ne olur |
|---|---|
| 0-7 gün | Hiçbir şey |
| 7 gün | Dünya **"tek imza" moduna** geçer (%70 ölçekleme) |
| Partner dönerse | **Anında** normale döner |
| 30 gün | Sessiz kapı: "tek başına sürdür" / "yeni eş davet et" |

⚠️ **Bildirim gitmez.**

---

## 8. Küslük ve ayrılık

### Küslük — geçici
| Özellik | Karar |
|---|---|
| Tetikleme | **Sadece manuel.** Bir oyuncu "küstüm" der. |
| Etki | Manifesto bonusu 1.8× → **1.3×**. Avatar üstünde küçük gösterge. |
| Bitiş | Depoya hediye **veya** "barışalım" → karşı taraf kabul → anında normal |
| Süre sınırı | 48 saat sonra kendiliğinden düşer |

### ⚠️ KIRMIZI ÇİZGİ — bunu asla kaybetme
**Küslük asla hareketsizlikle tetiklenmez ve asla bildirim göndermez.**

"Partnerin 3 gündür girmedi, küs duruma geçtiniz" gibi bir mekanik gerçek bir ilişkide gerçek bir suçluluk üretir. Oyun, birinin sınavı olduğu için giremediği bir haftayı ilişki sorununa çevirmemeli.

> Küslük, çiftin oynadığı bir **rol** olmalı — oyunun çifte uyguladığı bir yaptırım değil.

### Ayrılık — kalıcı
- Tek taraflı istek → bildirim → **48 saat** bekleme. Çift taraflı onayda bekleme yok.
- Ayrılan oyuncu **seviyesini ve çantasını koruyarak** yeni vadi kurar
- Ev ve depo vadiyi kuranda kalır
- **7 gün** yeni eşleşme yok
- Defter ilerlemesi ikisinde de kalır
- **Bir oyuncu aynı anda tek vadide olabilir**

---

## 9. Sohbet, gizlilik, mağaza uyumu

### Metin sohbeti (v1)
Kendi WebSocket kanalın. Sadece eşleşmiş partner arası. Saniyede 1 mesaj, uzunluk sınırı.

### Veri saklama
| Veri | Nerede |
|---|---|
| Ses | Hiçbir yerde (canlı akış, kayıt yok) |
| Mesaj geçmişi | **Sadece iki cihazda** (yerel SQLite) |
| Çevrimdışı mesaj | **Teslimat kuyruğu** — teslim edilince anında silinir, edilmezse 7 gün sonra otomatik. Arşiv değil, kuyruk. |
| Engelleme listesi | Sunucuda (zorunlu) |
| Not tahtası | Sunucuda **şablon kimliği + nesne kimliği** olarak. Serbest metin değil. |

### Rapor butonu — mağaza reddi sebebi
Apple ve Google, mesajlaşma olan uygulamalarda rapor/engelle/müdahale ister. Hiçbir şey saklamazsan rapor butonu boşa basılır.

> **İstemci taraflı rapor paketi:** Kullanıcı "rapor et" dediğinde istemci **kendi yerel geçmişinden** son N mesajı pakete ekleyip gönderir. Sunucu paketi sadece rapor açıldığında alır, inceleme bitince siler.

Rutin saklama sıfır, kanıt yalnızca biri yardım istediğinde oluşur, mağaza kuralına uyulur.

### Yaş kapısı
İlk açılışta sorulur. **13 yaşın altında: sohbet ve partner daveti kapalı, oyun tek kişilik oynanır.** Engelleme değil, kısıtlama.

### Not tahtası şablonları (12)
"Sana {nesne} bıraktım" · "{nesne} lazım" · "{bölge}'ye gidiyorum" · "{bölge}'de yardım lazım" · "Trene {nesne} yetiştirelim" · "Bunu satma, lazım olacak" · "Depo doldu" · "Bugün giremeyeceğim" · "Sıra sende" · "Teşekkürler" · "Seni özledim" · "Barışalım"

### İfade tekerleği (8 — fazlası parmakla ıskalanır)
Selam · Teşekkür · Evet · Hayır · Yardım · Bekle · Harika · Barışalım

---

## 10. Çanta ve depo — iki FARKLI kısıt

⚠️ Bu ayrım oyunun ekonomik gerilimini taşır. Yığın 99 + depo slot cinsindense **depo baskısı hiç tetiklenmez.**

| Kısıt | Ölçtüğü | Zorladığı | Nerede ısırır |
|---|---|---|---|
| **Çanta** | Eşya **çeşidi** (slot, yığın 99) | Hareket | Madende (taş/kömür/demir/altın/elmas) |
| **Depo** | Toplam **miktar** (birim) | Karar | Hasat sonrası |

```
Tarla → Çanta (çeşit) → Depo (miktar) → Tren
        küçük kısıt      büyük kısıt
```

### Çanta
| Slot | Seviye | Maliyet |
|---|---|---|
| 8 | 1 | — |
| 12 | 4 | **0 (seviye ödülü)** |
| 16 | 8 | 3.500 |
| 20 | 14 | 28.000 |

### Depo
| Kapasite | Seviye | Maliyet | ≈ sefer karşılığı |
|---|---|---|---|
| 100 | 2 | — | 2.5 |
| 160 | 6 | 1.200 | 2.2 |
| 240 | 11 | 9.000 | 2.0 |
| 340 | 15 | 45.000 | 1.8 |
| 460 | 18 | 120.000 | 1.7 |

⚠️ **Depo kapasitesi üretimden YAVAŞ büyümeli.** Büyüme doğrusal (+60/+80/+100/+120), maliyet üstel. Sefer karşılığı 2.5→1.7 düşer: **baskı ilerledikçe artar.** Cömert yaparsan tüm ekonomi tasarımı boşa gider.

### Zorunlu kolaylıklar
- **Otomatik yığınlama**
- **"Depoya boşalt" tek butonu** — bu buton olmadan oyun mobilde işkence
- **Depo dolu:** hasat **engellenir**, ürün tarlada bekler, net uyarı
- Kategoriye göre sıralama (tohum / ürün / cevher / odun / işlenmiş)

> ⚠️ **Eşya asla yok edilmez, asla yere düşmez, asla otomatik satılmaz.** Mobilde kaybolan eşya = silinen oyun.

---

## 11. Ürünler — 16 ürün

**Saatlik getiri kademesi** (uzun ürün daha kârlı, azalan artışla — 12 sa otomatik en iyi olmamalı):
5 dk: 24 · 30 dk: 30 · 2 sa: 36 · 6 sa: 42 · 12 sa: 48 altın/saat/karo

| # | Ürün | Açılış | Süre | Fiyat | Tohum | Özel |
|---|---|---|---|---|---|---|
| 1 | Buğday | Sv 1 ödül | 5 dk | 2 | 1 | Öğretici + un girdisi |
| 2 | Turp | Sv 2 ödül | 30 dk | 15 | 5 | |
| 3 | Havuç | Sv 3 ödül | 30 dk | 16 | 5 | |
| 4 | Mısır | Sv 4 ödül | 2 sa | 72 | 22 | İlk gerçek karar |
| 5 | Patates | Sv 5 ödül | 2 sa | 68 | 20 | |
| 6 | Domates | Sv 6 market | 2 sa | 75 | 24 | **Yeniden verir ×3** (2 sa) |
| 7 | Marul | Sv 7 market | 30 dk | 14 | 4 | |
| 8 | Kabak | Sv 8 market | 6 sa | 252 | 76 | |
| 9 | Çilek | Sv 10 market | 6 sa | 230 | 70 | **Yeniden verir ×4** (3 sa) |
| 10 | Ayçiçeği | Sv 11 market | 6 sa | 245 | 74 | |
| 11 | Lahana | Sv 12 market | 6 sa | 260 | 78 | |
| 12 | Karpuz | Sv 14 market | 12 sa | 576 | 175 | |
| 13 | Üzüm | Sv 15 market | 12 sa | 540 | 165 | **Yeniden verir ×3** (6 sa) |
| 14 | Balkabağı | Sv 16 market | 12 sa | 600 | 180 | |
| 15 | Kavun | Sv 18 market | 12 sa | 590 | 178 | Kısa pencere |
| 16 | Safran | Sv 19 market | 12 sa | 900 | 300 | Nadir, kısa pencere, +%56 prim |

### ⚠️ Başlangıç ürünü kuralı (kalıcı, çok önemli)
> **Buğday, turp, havuç, mısır, patates** hasatta **1 ürün + 1 tohum** verir. Market ürünleri (Sv 6+) vermez.

Üç işi birden yapar: Perde 1 kendi kendine yeter · başlangıç ürünleri geç oyunda **bedava ama düşük getirili güvenlik ağı** kalır · market ürünleri gerçek yatırım kararı olur.

### Büyüme aşaması
4 görsel aşama. `aşama = min(3, floor(geçenSüre / büyümeSn × 4))`. Sunucu okuma anında hesaplar, **cron yok.**

### Gübre
Tek tip, kalite kademesi yok. **+1 verim** (süreyi değiştirmez — süre değişseydi altın/saat şişerdi). Fiyat = taban fiyatın **%70'i**. Marketten, Sv 12. Craft yok.

> Oran bilinçli: otomatik kârlı değil. **Tren fiyatı %100 üstündeyken** kullanılır — dinamik fiyat sistemine bağlanan üçüncü karar katmanı.

### Sulama
Kova kapasitesi var, kuyu **tarla sahnesinin içinde** (sahne değiştirmek yok). Doldurma 1 sn. Kovalar kırılmaz.

| Kova | Kapasite | Malzeme |
|---|---|---|
| Ahşap | 6 karo | 8 odun |
| Demir | 12 karo | 6 demir külçesi |
| Çelik | 20 karo | 8 çelik külçesi |
| Elmas | 40 karo | 6 çelik + 2 elmas |

**Yağmurlama** (Sv 14 craft): 4 komşu karoyu her büyüme çevriminde otomatik sular. 24 karoyu elle sulamak dayanılmaz olur.

### Tarla karoları
4 (Sv1) → 8 (Sv2) → 12 (Sv6) → 16 (Sv10) → 20 (Sv14) → 24 (Sv18). Her oyuncu için ayrı parsel.

---

## 12. Orman

### Ağaçlar — uzun ritim
| Tür | Açılış | Büyüme | Vuruş | Getiri | Fidan |
|---|---|---|---|---|---|
| Çam | Sv 7 | 8 sa | 3 | 4 odun | 30 |
| Meşe | Sv 10 | 24 sa | 4 | 3 sert odun | 120 |
| Ceviz | Sv 13 | 48 sa | 5 | 2 değerli odun + 3 ceviz | 400 |
| Gümüş kavak | Sv 16 | 72 sa | 6 | 2 nadir odun | 1.200 |

**Kütük yok** — ağaç kesilince karo anında boşalır. Mobilde her fazladan dokunuş maliyettir.

### Yabani kaynaklar — kısa ritim
⚠️ Bu sistem iki iş yapar: ormanın ölü bölge olmasını engeller **ve alet kilitlenmesine karşı güvenlik ağıdır** (hiçbiri alet gerektirmez).

| Kaynak | Yenilenme | Getiri | Nerede |
|---|---|---|---|
| Dal | 30 dk | 1-2 odun | Orman, tarla kenarı, maden girişi |
| Taş parçası | 30 dk | 1-2 taş | Orman, maden girişi |
| Mantar | 90 dk | 1 mantar | Orman |
| Yaban meyvesi | 2 sa | 1-2 meyve | Orman |
| Yem otu | 45 dk | 1 yem | Orman, tarla kenarı |

**Kısa pencereli toplama** (Sv 16): günde 3 kez, 2 saatlik pencerelerde beliren nadir kaynaklar. Sunucu deterministik hesaplar. **Oyun içi takvim/mevsim YOK.**

---

## 13. Maden

| Kat | Kazma | Düğümler | Vuruş | Getiri | Yenilenme | Açılış |
|---|---|---|---|---|---|---|
| Kat 1 | Taş | Taş, kömür | 3 | 2-3 | 20 dk | Sv 9 |
| Kat 2 | Demir | Kömür, demir | 4 | 2-3 | 30 dk | Sv 12 |
| Kat 3 | Çelik | Demir, altın | 5 | 1-2 | 45 dk | Sv 15 |
| Dip | Elmas uçlu | Altın, elmas | 6 | 1-2 | 60 dk | Sv 18 |

**Sadece 4 cevher vardır: kömür, demir, altın, elmas.** Bakır/gümüş YOK.

| Cevher | İşlev |
|---|---|
| Kömür | Yakıt — eritme, tezgâh, maden feneri |
| Demir | Alet craft'ı |
| Altın | Ev eşyası craft'ı + yüksek değerli satış |
| Elmas | Elmas uçlu kazma + en değerli eşyalar + en yüksek satış |

---

## 14. Aletler ve craft

### Aletler
| Alet | Kademe | Malzeme | Dayanıklılık | Craft Sv |
|---|---|---|---|---|
| Taş kazma | 1 | 5 odun + 10 taş | 40 | **1** |
| Demir kazma | 2 | 5 odun + 8 demir külçesi | 120 | 10 |
| Çelik kazma | 3 | 5 sert odun + 10 çelik külçesi | 300 | 12 |
| Elmas uçlu kazma | 4 | 5 değerli odun + 8 çelik + 3 elmas | 800 | 15 |
| Taş balta | 1 | 5 odun + 8 taş | 120 | **1** |
| Demir balta | 2 | 5 odun + 6 demir külçesi | 360 | 10 |
| Çelik balta | 3 | 5 sert odun + 8 çelik külçesi | 900 | 12 |
| Elmas balta | 4 | 5 değerli odun + 6 çelik + 2 elmas | 2.400 | 15 |

Balta kırılır ama dayanıklılığı kazmanın **3 katı**. Dayanıklılık %20 altına düşünce **net bildirim** — sessizce kırılan alet öfke üretir.

### ⚠️ KİLİTLENME DEĞİŞMEZİ — bunu çözmeden yayınlama

Zincir: *kazma kırılır → demir lazım → demir madende → madene kazma lazım.* Aynı tuzak baltada da var.

> **Değişmez kural: Hiçbir alet, o aleti yapmak için gereken kaynağın tek edinme yolu olmamalı.**

Dördü de zorunlu:
1. **Dal ve taş parçası elle toplanır** — alet gerekmez, 30 dk'da yenilenir, üç bölgede birden var
2. **Taş kazma ve taş balta Sv 1'den evdeki tezgâhta craftlanabilir**
3. **Market ikisini de her zaman ucuz satar** (Sv 6+)
4. **%20 dayanıklılık uyarısı**

### Craft — TAM 25 tarif (sınır dolu)
| Grup | Adet |
|---|---|
| Aletler (4 kazma + 4 balta + 4 kova) | 12 |
| Ara malzeme (demir/çelik/altın külçesi) | 3 |
| İşleme (un, ekmek, peynir) | 3 |
| Yapı (yağmurlama) | 1 |
| Ev eşyası | 6 |

Malzeme derinliği en fazla **2 kademe** (cevher → külçe → eşya).

| Ara malzeme | Girdi |
|---|---|
| Demir külçesi | 2 demir + 1 kömür |
| Çelik külçesi | 2 demir külçesi + 2 kömür |
| Altın külçesi | 2 altın + 1 kömür |

| İşlenmiş | Girdi | Sv | Fiyat çarpanı |
|---|---|---|---|
| Un | 3 buğday | 13 | 2.5× |
| Ekmek | 2 un + 1 odun | 13 | 3× |
| Peynir | 2 süt | 14 | 2.8× |

⚠️ 25 dolu. **Yeni tarif eklemek isteyen bir tarif çıkarmak zorunda.**

### Hayvanlar
| Hayvan | Yapı | Sv | Ürün | Periyot | Fiyat |
|---|---|---|---|---|---|
| Tavuk | Kümes | 8 | Yumurta | 24 sa | 40 |
| İnek | Ahır | 14 | Süt | 24 sa | 180 |

**Yem yoksa üretmez, ÖLMEZ.** Kozy oyunda hayvan ölmez.

---

## 15. Tren garı ve ekonomi

Oyunun ekonomik kalbi.

### Tarife
- **6 saatte bir sefer** → 00:00 / 06:00 / 12:00 / 18:00 **UTC** (sunucu saati; iki oyuncu farklı saat diliminde olabilir)
- Her seferde **3 sipariş kartı**
- Karşılanan sipariş: **1.6-2.0×** fiyat + 60 XP + defter ilerlemesi (küslükte 1.3×)
- Manifesto tam doldurma: **+100 XP** bonus
- **Kaçırma cezası yok.** Fırsat kaybı yeterli baskı.
- **Kısmi yatırma YOK** — sipariş ancak tam miktar elindeyken karşılanır. Sıfır kenar durumu.

### Dinamik fiyatlandırma — deterministik

⚠️ **Saf rastgele KULLANMA.** Yanında "son 3 seferin mini grafiği" gösterilecek; saf rastgelede okunacak eğilim olmaz, grafik gürültü olur. Fiyatın **hafızası** olmalı.

```
seferNo = floor((sunucuZamanı - 2026-01-01T00:00:00Z) / 6 saat)

p[0] = 1.0
p[n] = clamp(0.70, 1.30,
             p[n-1] + (1.0 - p[n-1]) × 0.30                        // ortalamaya dönüş
                    + (hash01(tohum, ürünId, n) - 0.5) × 0.24)     // gürültü

fiyat = tabanFiyat × p[n]
```
- `hash01` = SHA-256(tohum ‖ ürünId ‖ n) ilk 8 bayt / 2^64
- **Durumsuz:** son 50 seferden ileri sararak hesaplanır, veritabanına satır yazılmaz
- Her ürün kendi bağımsız yürüyüşünü yapar

### Tren tohum getirir
Her seferde **2-3 çeşit tohum**, marketten %30-40 ucuz, **sınırlı adet** (sınırsız olursa market anlamsızlaşır). Çeşit deterministik.

### Market
| İşlem | Kural |
|---|---|
| Alım | Tohum, alet, gübre, çanta/depo yükseltmesi |
| Satım | Beklemeden, anında — ama düşük |
| Market alış fiyatı | Taban fiyatın **%50-60'ı, SABİT** (dalgalanmaz) |

**Neden sabit:** dalgalanırsa oyuncunun takip edeceği iki fiyat sistemi olur. Market "acil nakit musluğu" olmalı, ikinci strateji katmanı değil.

**Oran kontrolü:** market %50-60 · tren ort. %100 · manifesto 1.6-2×. ⚠️ Marketi cömert yaparsan **tren garı ölür ve oyunun ekonomik kalbi durur.**

### Satış baskısı
Ürün **asla değer kaybetmez.** Oyuncuyu satmaya zorlayan tek şey **yer sıkıntısı**.

> Karar: *"%90 fiyata şimdi satıp yer mi açayım, yoksa %130'u bekleyip bu turu hasatsız mı geçireyim?"*

Bozulma mekaniğinden iyidir — oyuncu çevrimdışıyken hiçbir şey kaybetmez, aktif bir seçim yapar.

### ⚠️ Para oyuna Sv 5'te girer
**Sv 1-4'te altın YOKTUR.** HUD'da göstergesi bile yok; tren gelince belirir. Perde 1 tohumla kendi kendine döner (başlangıç ürünü kuralı).

Hikâye kazancı: "Tren yıllar sonra ilk kez durur" perde kapanışı mekanik olarak da doğru.

### Para çıkışları — çiftlik oyunlarının en sık öldüğü yer
Gelir tasarlanıp gider tasarlanmazsa Sv 15'te para anlamsızlaşır ve tüm ekonomik gerilim çöker.

| Çıkış | Ölçek |
|---|---|
| Tohum | Sürekli, gelirle büyür |
| Alet yükseltme | Her kademe öncekinin ~2.5 katı |
| Tarla karosu | Artan maliyet |
| Depo ve çanta | **Kasıtlı olarak pahalı** |
| Ev eşyası craft'ı | Sonsuz — altın/elmasın ana kuyusu |
| **Vadi anıtı** | **Sv 20 SONRASI hedef** — 5 kademe, toplam 820.000 altın + 160 elmas, işlevsel fayda yok |

---

## 16. XP ve seviye

`Sv n → n+1 = round(100 × 1.2^(n-1))` · Toplam **15.476 XP**

| Sv | XP | Sv | XP | Sv | XP | Sv | XP |
|---|---|---|---|---|---|---|---|
| 1→2 | 100 | 6→7 | 249 | 11→12 | 619 | 16→17 | 1.541 |
| 2→3 | 120 | 7→8 | 299 | 12→13 | 743 | 17→18 | 1.849 |
| 3→4 | 144 | 8→9 | 358 | 13→14 | 892 | 18→19 | 2.219 |
| 4→5 | 173 | 9→10 | 430 | 14→15 | 1.070 | 19→20 | 2.663 |
| 5→6 | 207 | 10→11 | 516 | 15→16 | 1.284 | | |

### XP kaynakları
| Kaynak | XP |
|---|---|
| Hasat | `max(1, round(büyümeSaati × 2))` |
| **Tren siparişi** | **60** (baskın kaynak olmalı) |
| Manifesto tam doldurma | +100 |
| Cevher düğümü | 3 |
| Ağaç | 10 / 20 / 40 / 60 |
| Yabani toplama | 1 |
| Craft ilk kez / tekrar | 30 / 5 |
| Defter sayfası | 75 |
| Bölge ilk açma | 50 |
| **"İlk kez" anları** | **25** ×6 |
| Ortak proje | 300 |

### ⚠️ Perde 1'in tempo motoru
> Perde 1'in temposu çiftçilikle değil **KEŞİFLE** yürür. Tek seferlik ödüller Perde 1'in motorudur, süsü değil.

Perde 1 tek seferlik XP: 5 defter (375) + 3 bölge (150) + 3 craft (90) + 6 "ilk kez" (150) = **765**, gereken 537, pay **%42**.

Çiftlikten gelen hız günde sadece 14-28 XP. **Bu ödülleri kısan biri Perde 1'i günlerce süren angaryaya çevirir ve kimse nedenini anlamaz.**

---

## 17. Seviye seviye ilerleyiş

**Tasarım kuralı: Her seviye TEK bir yeni şey açar. Asla iki.**

### Perde 1 — Ev kurulur (Sv 1-5) · ~1.5-2 saat · para YOK

| Sv | Açılan | Hikâye |
|---|---|---|
| 1 | Ev + Tarla (4 karo) + buğday + **basit tezgâh (taş kazma, taş balta, ahşap kova)** + çanta 8 slot + kuyu | Vadiye varış. Defter bulunur. |
| 2 | **Depo** (100 birim) + turp | "Burada bıraktıkların ona ulaşır." |
| 3 | **Partner daveti + ifade tekerleği + metin sohbeti** + havuç | "Bu vadide hiçbir iş tek başına görülmez." |
| 4 | Mısır (2 sa) + çanta 12 slot **(bedava)** | İlk gerçek karar: hızlı mı, kârlı mı? |
| 5 | **Tren garı** + patates + **para oyuna girer** + hesap bağlama teklifi | Tren yıllar sonra ilk kez durur. |

⚠️ **Sv 5 oyunun kaderidir.** Terk oranı en yüksek burada olacak. İlk 15 dakika ile ilk tren durağı arası, oyunun en çok cilalanmış kısmı olmalı.

### Perde 2 — Vadi açılır (Sv 6-12) · ~5-7 saat

| Sv | Açılan | Hikâye |
|---|---|---|
| 6 | **Market** + depo 160 | Vadiye ilk dışarıdan gelen. |
| 7 | **Orman** — balta, çam, yabani kaynaklar | "Ev genişleyecekse odun lazım." |
| 8 | Kümes + çanta 16 slot | Vadiye ilk canlı döner. |
| 9 | **Maden ocağı** — Kat 1 | Çökük giriş açılır. |
| 10 | **Tezgâh yükseltmesi** — metal hattı + meşe | Artık ürettiğini geliştiriyorsun. |
| 11 | **ORTAK PROJE: maden asansörü** + depo 240 | İki katkı zorunlu. |
| 12 | Maden Kat 2 + gübre | Derine inmenin bedeli var. |

### Perde 3 — Defter tamamlanır (Sv 13-20) · ~7-9 saat

| Sv | Açılan | Hikâye |
|---|---|---|
| 13 | Değirmen: buğday → un → ekmek + ceviz | Ham satmak artık aptalca. |
| 14 | İnek/ahır + çanta 20 slot + yağmurlama | Vadi kendine yetiyor. |
| 15 | **ORTAK PROJE: köprü** → Maden Kat 3 (altın) + depo 340 | En büyük kaynak talebi. |
| 16 | Orman derinliği — gümüş kavak + kısa pencereli toplama | Zamanlama baskısı zirvede. |
| 17 | Ev genişletme + kozmetikler + **vadi anıtı açılır** | Nefes alma seviyesi. |
| 18 | Maden dibi (elmas) + depo 460 | **Son çiftin neden ayrıldığı ortaya çıkar.** |
| 19 | Büyük manifesto hazırlığı + safran | Oyunun tek en büyük siparişi. |
| 20 | **Amorland tarifeye döner** | Final. Defter imzalanır. |

**Sv 11 ve 15'in rolü:** Bunlar duvar değil **buluşma noktası** — çiftin oturup plan yapmak zorunda kaldığı tek yer. Oyunun sosyal doruk noktaları.

**Sv 20 sonrası:** Sonsuz haftalık manifestolar · kozmetik koleksiyon · **vadi anıtı** (asıl uzun hedef)

---

## 18. Öğretici — ilk 15 dakika

⚠️ İlk 15 dakikanın işi **Sv 5'e ulaşmak değil**, oyuncuyu **10. dakikada partner davetine** getirmek.

| Dk | Olay |
|---|---|
| 0:00 | Misafir hesap, kayıt yok. İsim + 4 renkten biri. |
| 0:15 | Yaş kapısı (bir kez) |
| 0:30 | Vadi haritası — sadece **Ev** aktif, diğer 6 silik |
| 0:45 | Eve gir. Masada parlayan defter → **1. sayfa:** "Bu vadide hiçbir iş tek başına görülmez." |
| 1:10 | Defter görev verir: "Tarlaya git." |
| 1:20 | Tarla, 4 karo, çantada 4 buğday tohumu |
| 1:30 | Karo parlar → dokun → yürür, çapalar + eker. **"Dokun, ekilir."** |
| 1:50 | Kuyu parlar → kova dolar → karolara dokun → sulanır. **Sürükle-boyama ipucu.** |
| 2:10 | "Buğday 5 dakikada büyür." Sayaç. |
| 2:20 | Tarla kenarındaki 3 yabani kaynak parlar → topla. **Bekleme boş geçmiyor.** |
| 3:00 | İlk hasat (1 ürün + 1 tohum). XP çubuğu ilk kez dolar. |
| 3:30 | **Sv 2 → Depo.** "Depoya boşalt" öğretilir. |
| 4:00 | Defter 2. sayfa. **Öğretici biter.** |
| 4-10 | Serbest oyun, turp, Sv 3'e yaklaşma |
| ~10:00 | **Sv 3 → Partner daveti.** Tam ekran, kaçırılamaz, atlanabilir ("sonra"). |

**Kurallar:** atlanabilir olmamalı ama 4 dakikayı geçmemeli · zorla el tutma yok (parlayan hedef + tek satır, modal diyalog değil) · **ilk 30 dakikada reklam yok** · bağlantı gerektiği baştan söylenir.

---

## 19. Hesap ve kurtarma

| Aşama | Kural |
|---|---|
| İlk açılış | Misafir hesap (cihaz ID), **sürtünmesiz** |
| Sv 5 / 8 / 12 | Hesap bağlama teklifi (e-posta / Google / Apple), atlanabilir, en fazla 3 kez sorar |
| **Partner davetinde** | **ZORUNLU** — artık başkasının ilerlemesi sana bağlı, gerekçesi ekranda yazar |
| Bağlamak istemeyen | **12 haneli kurtarma kodu**, oyuncu üretir, kendi saklar |

⚠️ Bu olmadan: telefon değişti → 20 saat gitti → **ve partnerin dünyası da bozuldu.**

---

## 20. Ekran envanteri — 26 ekran + 1 kalıcı HUD

**HUD:** seviye+XP · altın (Sv 5+) · çanta · tohum seçici (4 slot) · harita · sohbet · partner göstergesi (çevrimiçi/çevrimdışı/küs)

Açılış+isim · yaş kapısı · vadi haritası · **7 bölge sahnesi** · çanta · depo · craft · tren garı · market · defter · sohbet · partner daveti · partner paneli · ortak proje tahtası · ayarlar · rapor akışı · reklam onayı · seviye atlama · günlük sandık · yükleme ekranı

**Perde 1 için gereken: 19 ekran.**

---

## 21. Defter — 20 sayfa

| Sayfa | Tetikleyici |
|---|---|
| 1 | Başlangıç |
| 2 | İlk hasat |
| 3-5 | Depo (Sv2) · Partner daveti (Sv3) · Tren garı (Sv5) |
| 6-9 | Market · Orman · Kümes · Maden girişi |
| 10-12 | İlk metal alet · Maden asansörü · Kat 2 |
| 13-16 | Değirmen · İnek · Köprü · Orman derinliği |
| 17-19 | Ev genişletme · **Maden dibi (son çiftin neden ayrıldığı)** · Büyük manifesto |
| 20 | Final |

**Tek kişilik final:** Son sayfa oyuncunun adıyla ve **boş bırakılmış bir ikinci imza satırıyla** kapanır:

> *"İkinci imza için bir yer her zaman ayrılmıştır."*

Kayıp değil, davet. Sonradan eş davet edilirse o satır dolar.

---

## 22. Sanat yönü

**Low-poly 3D, ortografik kilitli kamera.**

### Stil sözleşmesi
- Düz renk, doku yok veya tek atlas. **Gradyan yok, PBR yok.**
- **12-16 renkli sabit palet**, tüm varlıklar bu paletten
- Kenar yumuşatma yok, sert normal
- Ürün 150-400 üçgen · ağaç 400-900 · karakter 1.200-2.000
- Sahte gölge (blob decal)

### Kaynak stratejisi (bütçe 0 TL)
1. **CC0 paketler** (Kenney, Quaternius, KayKit, Poly Pizza) — karakter, yapı, prop. İhtiyacın ~%70'i.
2. **Parametrik Blender script'i** — 16 ürün × 4 aşama = 64 model gibi *tutarlılık gerektiren* setler. Tek script, garantili aynı stil.
3. **Saf Python** (Pillow/SVG) — UI ikonları, zemin dokuları, ortografik sprite render.

⚠️ **AI 3D üretimi bu proje için yanlış araçtır.** Her çalıştırmada farklı siluet, oran, palet ve bozuk topoloji üretir; "aynı havucun %40 büyümüşü" diyemezsin. Script diyebilir.

---

## 23. Ses ve haptik

### ⚠️ İki gerçek — sıradan ses planını geçersiz kılar

**1. Oyuncularının çoğu sesi kapalı oynayacak.** Mobil kozy oyunlar podcast/müzik dinlerken oynanır.
- **Hiçbir mekanik sese bağlı olamaz.** Alet kırılma, depo dolu, tren gelişi — hepsinin görsel karşılığı zorunlu. Ses ikinci kanal.
- **Oyuncunun sesini çalma.** iOS `AVAudioSessionCategoryAmbient`, Android `AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK`. Birinin podcast'ini kesen kozy oyun kaldırılır.

**2. Sürükle-boyama sesi katleder.** 24 karo kuyruğa alınınca aynı ses 24 kez çalar → makineli tüfek. Üçü de zorunlu:

| Önlem | Değer |
|---|---|
| Perde varyasyonu | Her tekrarda **±%8** rastgele pitch |
| Örnek varyasyonu | Sık tekrar eden her eylemin **en az 3 kaydı** |
| Eşzamanlılık sınırı | Etiket başına aynı anda en fazla **3** kanal |

Kuyruk eylemlerinde ses hafif alçalır (1. eylem %100, sonrakiler %75).

### Yatırım sırası — içgüdüye bırakma

**1)** Eylem sesleri · **2)** Ödül anları · **3)** Ortam döngüleri · **4)** Haptik · **5)** UI · **6)** Müzik

> ⚠️ Müzik **en pahalı, en az fark edilen** katman — çoğu oyuncu kendi müziğini dinliyor. Önce müzikle uğraşmak tek kişilik ses bütçesini bitiren klasik hatadır.

### Envanter — 52 benzersiz ses, ~92 dosya

| Grup | Adet | Not |
|---|---|---|
| **Eylem** | 14 (**×3 varyasyon**) | Ekme, sulama, hasat ×2, kazma ×2, düğüm kırılma, balta, ağaç, toplama, kova, craft ×2, **alet kırılma** |
| **Ayak sesi** | 4 yüzey (**×5 varyasyon**) | Toprak, çim, tahta, taş |
| **Ödül/olay** | 8 | Seviye, **tren gelişi (~4 sn, oyunun en büyük olayı)**, tren kalkış, manifesto, defter sayfası, ortak proje katkı, **ortak proje tamamlandı**, günlük sandık |
| **Ortam döngüsü** | 7 | Bölge başına 30-45 sn. Maden dışında hepsi sakin. |
| **Hayvan** | 3 | Tavuk, inek, yumurta |
| **UI** | 12 | |
| **Müzik** | 4 | Ana tema, genel döngü, maden, final |

### Kaynak — 0 TL, ses tasarımcısı yok

⚠️ **Çiftlik oyununda en iyi 0 TL ses kaynağı senin telefonun.** Foley tam olarak budur, bir öğleden sonra sürer ve kütüphane sesinden iyi çıkar.

| Ses | Nasıl kaydedilir |
|---|---|
| Sulama | Kovadan leğene su dök *(en çok duyulan ses — en iyi kayıt buraya)* |
| Ekme / toprak | Saksı toprağını avuçla, sık |
| Hasat | Marul kopar, kâğıt buruştur |
| Balta | Tahtaya tahta vur |
| Kazma | Taşa taş vur, çakıl karıştır |
| Ayak sesi | Toprakta / tahtada / çakılda yürü |
| Defter sayfası | Kalın kâğıt çevir |

Her sesin **5-6 tekrarını** kaydet (varyasyon buradan çıkar), Audacity ile kırp + normalize.

**Kütüphane** (kalan için): Kenney Audio (CC0) · Freesound (**CC0 filtresini mutlaka aç**) · Sonniss GDC bundle · Pixabay Audio
⚠️ **Lisans kaydı tut:** her dosya için kaynak + lisans + tarih bir CSV'de.

**Üretilen:** UI sesleri Python ile sentezlenebilir. Organik sesler (su, toprak, ahşap) sentezle iyi çıkmaz — oraya girme.

### Stil sözleşmesi
| Kural | Değer |
|---|---|
| Format | OGG Vorbis (kaynak 48 kHz WAV) |
| Ses yüksekliği | Tüm SFX **-16 LUFS**, tepe -1 dBFS |
| Eylem sesi | **300 ms altı** |
| Ödül sesi | 1-4 sn |
| Ortam döngüsü | 30-45 sn, sıfır geçişli kusursuz döngü |
| Ton | Yumuşak, alçak frekanslı. **Kozy = keskin transient yok.** |

### Haptik — sessizken çalışan TEK kanal

Oyuncuların çoğu sesi kapalı oynayacaksa dokunsal geri bildirim sesten önemli. Ve bedava.

| Olay | Titreşim |
|---|---|
| Hasat | Çok hafif, tek darbe |
| Düğüm kırılma | Orta, tek darbe |
| **Alet kırılma** | **Çift darbe** — sesi kapalı oyuncu bunu kaçırmamalı |
| Seviye atlama | Yükselen üçlü desen |
| Ortak proje tamamlandı | Uzun, belirgin |
| Hata | Çok kısa, sert |

- Ayarlarda **kapatılabilir**
- Kuyruk eylemlerinde **sadece ilk ve son** titreşir — 24 titreşim işkence
- iOS `UIFeedbackGenerator`, Android `VibrationEffect`. `Handheld.Vibrate()` tek başına yetmez.

### Unity teknik
| Konu | Karar |
|---|---|
| AudioMixer | Master → Müzik / Ortam / Eylem / UI, her biri ayrı kaydırıcı |
| Kısa SFX | Decompress on Load, ADPCM/PCM |
| Ortam + müzik | Streaming, Vorbis %70, Addressables ile bölgeyle |
| **AudioSource havuzu** | Ses başına `Instantiate` YOK. 16 kaynaklı havuz. |
| Sessiz anahtarı | iOS'ta sessizdeyse ses çalmaz |
| Arka plan | Uygulama arka plana atılınca tüm ses durur |
| Boyut bütçesi | **~20 MB** (150 MB indirme hedefi içinde) |

---

## 24. Reklam ve mağaza

### Reklam — sadece ödüllü
| Yer | Ödül |
|---|---|
| Hasat sonrası | Bu hasadı 2× yap |
| Maden girişi | Fener dayanıklılığını tazele |
| Günlük sandık | İki katına çıkar |

**Günde en fazla 5 · ilk 30 dakikada yok · çanta doluysa reklam BAŞLATILMAZ** (izlenmiş reklamın ödülü kaybolamaz)

**ASLA:** ilerlemeyi kilitleyen · süre kısaltma satan · çift arasında dengesizlik yaratan · kozmetik veren reklam

### Bildirim
**Günde en fazla 2.** Fazlası kaldırılma sebebi.
**Küslük ve partner terk durumu ASLA bildirim göndermez.**

### Mağaza
- Serbest sohbet → yaş derecelendirmesi yükselir, "kullanıcılar etkileşimde bulunur" beyanı
- Gizlilik etiketlerinde veri toplama beyanı
- Rapor + engelleme mekanizması zorunlu
- Google Play 25 $ tek seferlik · Apple Developer 99 $/yıl · **iOS derlemesi Mac gerektirir, sona bırakma**

---

## 25. Yapım sırası

1. **Dikey dilim** — Ev + Tarla, tek ürün, ek/sula/biç, iki oyuncu bağlı, durum kalıcı, **ses altyapısı (mixer + havuz + varyasyon) + yer tutucu ses seti**
2. **Perde 1 tam (Sv 1-5)** — oyunun vitrini. **Eylem sesleri (kendi kaydın) + haptik + tarla/ev ortamı** — vitrin sessiz olamaz.
3. Market + Orman (Sv 6-7)
4. Maden + craft (Sv 9-10)
5. İlk ortak proje (Sv 11) — sosyal tasarımın gerçek testi
6. Perde 3 içeriği
7. Cila, UI sesleri, müzik, karışım dengesi, bildirim, mağaza materyali
8. **(v2)** Sesli sohbet

> ⚠️ **Perde 1 bitmeden Perde 3'e tek satır içerik üretme.**

---

## 26. Yasaklar — bunları yaparsan oyun bozulur

| # | Yasak | Sonuç |
|---|---|---|
| 1 | Küslüğü hareketsizlikle tetiklemek veya bildirim göndermek | Gerçek ilişkide gerçek suçluluk üretir |
| 2 | Depo yükseltmelerini cömert yapmak | Fiyat sistemi anlamsızlaşır, ekonomi ölür |
| 3 | Marketi cömert yapmak | Tren garı ölür, ekonomik kalp durur |
| 4 | Eşyayı yok etmek / yere düşürmek / otomatik satmak | Mobilde kaybolan eşya = silinen oyun |
| 5 | Envanterde sürükle-bırak | Parmak eşyayı örter, yanlış slota düşer |
| 6 | Taş kazma/baltayı ulaşılmaz yapmak | Oyun kilitlenir |
| 7 | İstemci saatine güvenmek | Oyuncu telefon saatini ileri alır |
| 8 | Tren fiyatını saf rastgele üretmek | Mini grafik gürültü olur, strateji ölür |
| 9 | Mevsim, enerji barı veya premium para eklemek | Bütçe, tasarım ve reklam politikasıyla çelişir |
| 10 | 25 craft tarifini aşmak | Karmaşıklık patlar |
| 11 | Bir seviyede iki yeni şey açmak | Öğrenme eğrisi kırılır |
| 12 | Tek seferlik XP ödüllerini kısmak | Perde 1 sessizce günlerce süren angaryaya döner |
| 13 | Rastgele eşleşme eklemek | Taşınamaz moderasyon yükü, partner kaybında oyun ölür |
| 14 | Perde 1'de para/market açmak | Öğretici yükü katlanır, tren kapanışının anlamı gider |
| 15 | AI 3D üretimine güvenmek | Stil tutarsızlığı, bozuk topoloji |
| 16 | Oyuncunun çalan müziğini/podcast'ini kesmek | Kozy oyunun 1 numaralı kaldırılma sebebi |
| 17 | Bir mekanığı sadece sesle bildirmek | Oyuncuların çoğu sesi kapalı oynuyor |
| 18 | Perde/örnek varyasyonu olmadan tekrar eden ses | Sürükle-boyamada makineli tüfek sesi |
| 19 | Ses altyapısını dikey dilimden sonraya bırakmak | 92 sesin hepsi yeniden elden geçer |
| 20 | Müziği önce yapmak | Ses bütçesini bitiren klasik hata |

---

## 27. Bilerek kapsam dışı bırakılanlar

| Konu | Ne zaman |
|---|---|
| **Sunucu veri modeli + WebSocket protokolü** | Ayrı doküman, dikey dilimden hemen önce |
| İsim müsaitliği (marka, paket adı, alan adı, mağaza araması) | Yayın kararından önce. **Sıra: mağaza araması → marka → paket adı → domain** |
| Yerelleştirme | Altyapı baştan (string tablosu), içerik cilada |
| 20 defter sayfasının gerçek metni | Perde perde yazılır |

> ⚠️ **Buradaki her sayı başlangıç noktasıdır.** Simülasyonla tutarlı hale getirildi (16 gün, 7.5 sa aktif, kasa hiç eksiye düşmüyor) ama gerçek değerler oyun testinden çıkacak. Özellikle **depo kapasitesi** dikey dilimde ölçülecek 1 numaralı sayıdır.
