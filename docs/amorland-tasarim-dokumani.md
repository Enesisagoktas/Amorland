# Amorland — Oyun Tasarım Dokümanı

> Sürüm 0.2 — `son-durak-tasarim-dokumani.md` bu dosyayla değiştirildi.
> ✅ karar verildi · ❓ tartışmaya açık · ⚠️ risk

---

## 1. Tek cümlelik özet

Bir çift, ancak iki kişiyle iş görülen bir vadiye taşınır ve evden ormana, madenden tren garına kadar yedi bölgeyi birlikte yeniden ayağa kaldırır.

**Tür:** Kozy çiftlik + kaynak yönetimi
**Platform:** Android + iOS, yatay, üstten bakış 2.5D
**Oyuncu:** 2 kişilik ortak oynanış (çift odaklı), tek kişilik de mümkün
**Süre:** ~15-20 saat, yarısı gerçek zamanlı bekleme
**Oturum:** Günde 3-4 kez, 5-10 dakika

---

## 2. Hikâye

### Neden değişti

İlk taslaktaki hikâye terk edilmiş bir demiryolu kasabası ve ölmüş bir istasyon şefiydi. "Amorland" ismi, küslük ve ayrılık mekanikleri ile birlikte oyunun konusu **iki oyuncu** değil **bir çift** oldu. Melankolik hayalet kasaba anlatısı bu markanın altında yanlış duruyor. Yeni hikâye aynı üretim ucuzluğunu koruyor ama temaya oturuyor.

### Kurulum

Amorland, vadinin eski adı. Buranın tek bir kuralı vardı: **hiçbir iş tek başına görülmez.**

- Tren manifestosu **iki imza** ister
- Madende ikinci bir el olmadan derine inilmez
- Tarlada biri eker, biri sular

Vadi boşaldı çünkü son çift ayrıldı ve manifestoyu bir daha kimse ikinci kez imzalamadı.

### Anlatım aracı

Evde yarım kalmış bir **"birlikte yapılanlar defteri"** var. Önceki sakinlerin bıraktığı, tamamlanmamış bir kayıt. Her bölge açıldığında, her ortak proje bittiğinde bir sayfası tamamlanır.

Ara sahne yok. Diyalog ağacı yok. Seslendirme yok. Anlatım tamamen **toplanabilir metin sayfaları** üzerinden yürüyor — tek kişilik geliştirmede bütçeyi bitiren üç şey bunlar değil.

### Final

Amorland tren tarifesine geri döner ve defterin son sayfası oyuncuların kendi adlarıyla imzalanır.

### Mekanik = hikâye ✅

Tek kişilik modun neden daha zor olduğu artık bir denge ayarı değil, **dünyanın kuralı**: tek imzalı manifesto trende daha az karşılık bulur. Oyuncu bunu bir ceza değil, bir hikâye gerçeği olarak yaşar.

---

## 3. Dünya yapısı — 7 bölge ✅

Üstten bakışlı bir **vadi haritası** var. Bölgeye dokunursun, o sahneye girersin, karakterini orada kontrol edersin.

| Bölge | İşlev | Açılış |
|---|---|---|
| **Ev** | Üs, craft tezgâhı, defter, kümes, kozmetik | Sv 1 |
| **Tarla** | Ekim, sulama, hasat | Sv 1 |
| **Depo** | Ortak sandık — partner değişiminin yeri | Sv 2 |
| **Tren garı** | Satış, manifesto, fiyat tablosu | Sv 5 |
| **Market** | Tohum, alet, çanta yükseltmesi alımı | Sv 6 |
| **Orman** | Odun, yem, mevsimlik toplama | Sv 7 |
| **Maden ocağı** | Cevher, katlar, defter sayfaları | Sv 9 |

### Bu yapının teknik değeri ⚠️ (önemli)

Hub-and-spoke tasarımı seçmen mobil için doğru karardı ve muhtemelen farkında olmadığın üç şey kazandırıyor:

1. **Açık dünya yapmıyorsun.** Her bölge ayrı bir sahne. Kapsam otomatik olarak sınırlı kalıyor — çoğu tek kişilik projeyi öldüren şey açık dünyaydı.
2. **Bellek kontrol altında.** Aynı anda tek sahne yüklü. Alt segment Android cihazlarda hayat kurtarır.
3. **Paralel üretim.** Orman bitmeden madene başlayabilirsin, birbirine bulaşmazlar.

Bölge geçişinde kısa bir yükleme ekranı olacak. Bunu gizlemeye çalışma — Addressables ile sahneyi asenkron yükle, geçişe 0.5-1 sn'lik bir kararma koy, yeterli.

**Depo'nun özel rolü:** Ortak sandık, çiftin fiziksel buluşma noktası. Partner çevrimdışıyken bile "sana demir bıraktım" iletişimi burada gerçekleşiyor. Asenkron ortak oynanışın kalbi bu bölge, sakın basit bir envanter ekranı gibi tasarlama.

---

## 4. Ortak oynanış

### Temel kural ✅ — Ortak dünya, ayrı tarlalar

Depo, maden, orman, gar, market **ortak**. Tarla iki parsele bölünür, her oyuncunun kendi parseli vardır.

| Problem | Çözüm |
|---|---|
| Partner ekinleri mahvedebilir | Kimse diğerinin parseline dokunamaz |
| Partner bırakırsa ilerleme ölür | Ortak bölgeler devam eder, sadece yavaşlar |
| Tek kişilik ayrı tasarım ister | Tek kişilik = aynı dünya, iki parsel de sende |

**Tek kişilik mod ayrı bir mod değil.** Sıfır ekstra içerik üretiyorsun.

### Tek kişilik denge ✅ — %70

Sen bana bıraktın, %70 diyorum. Gerekçe: %65, Sv 11 ve Sv 15'teki ortak projelerde duvara dönüşüyor; %70 gerçek bir baskı yaratıyor ama bitirilebilir kalıyor. Ayrıca **testte oranı aşağı çekmek yukarı çekmekten kolaydır** — yukarı çekmek "oyun kolaylaştı" şikâyeti üretir, aşağı çekmek fark edilmez.

Ne ölçeklenir: tren manifesto kontenjanı, ortak proje malzeme maliyeti, maden asansörü katkı eşiği.
Ne ölçeklenmez: ürün büyüme süresi, fiyatlar, XP. Bunlara dokunursan iki mod arasında ekonomi ayrışır ve dengeyi iki kez ayarlamak zorunda kalırsın.

### Eşleşme akışı ✅

```
İlk açılış → misafir hesap (cihaz ID), kayıt ekranı yok
   ↓
"Amorland'e nasıl gireceksin?"
   ├─ Tek başıma → oyun başlar, partner sonra davet edilebilir
   └─ Eşimle
         ↓
      6 haneli davet kodu + paylaş bağlantısı (deep link)
         ↓
      Partner kodu girer → aynı vadiye düşer
```

- **Rastgele eşleşme yok.** Kalıcı ortak dünyada yabancıyla eşleşmek, partner kaybolduğunda oyunu öldürür. Ayrıca sesli sohbetle birleşince taşınamaz bir moderasyon yükü doğurur.
- Partner daveti **Sv 3'te** açılır — oyuncu önce temel döngüyü öğrensin.
- Tek başına başlayan sonradan partner davet edebilir, ilerleme korunur.

---

## 5. Küslük ve ayrılık ✅

İki ayrı özellik, iki ayrı ağırlık.

### Küslük — geçici, geri dönülebilir

| Özellik | Karar |
|---|---|
| Nasıl tetiklenir | **Sadece manuel.** Bir oyuncu "küstüm" der. |
| Etkisi | Ortak manifesto bonusu düşer (1.8x → 1.3x). Avatar üstünde küçük gösterge. |
| Nasıl biter | Depoya hediye bırakma **veya** ifade tekerleğinden "barışalım" → karşı taraf kabul eder → anında normale döner. |
| Süre sınırı | 48 saat sonra kendiliğinden düşer. |

### ⚠️ Kırmızı çizgi — bunu tasarımda kaybetme

**Küslük asla hareketsizlikle tetiklenmemeli.** "Partnerin 3 gündür girmedi, küs duruma geçtiniz" gibi bir mekanik, gerçek bir ilişkide gerçek bir suçluluk üretir. Oyun, birinin sınavı olduğu için giremediği bir haftayı ilişki sorununa çevirmemeli.

Aynı sebeple: küslük durumu partnere **bildirim göndermemeli**. Oyuncu oyuna girdiğinde görür, telefonu titremez. Bu ayrım küçük görünüyor ama oyunun çift üzerinde baskı kurup kurmadığını belirleyen şey tam olarak bu.

Küslük, çiftin oynadığı bir **rol** olmalı — oyunun çifte uyguladığı bir yaptırım değil.

### Ayrılık — kalıcı

- Tek taraflı istek → karşı tarafa bildirim → **48 saat** bekleme (dürtüsel ayrılığı engeller)
- Çift taraflı onay varsa bekleme yok
- Ayrılan oyuncu **seviyesini ve sırt çantasını koruyarak** yeni bir vadi kurar
- Ev ve depo, vadiyi kuran oyuncuda kalır
- **7 gün** yeni eşleşme yapılamaz
- Defter ilerlemesi ikisinde de kalır (hikâyeyi kimseden geri alma)

---

## 6. Sohbet — metin + sesli ✅

Serbest sohbet kararını verdin. Bu, ilk taslaktaki önerimin tersi — ama **rastgele eşleşme olmadığı için** savunulabilir. Yabancıları birbirine bağlamıyorsun, davet koduyla eşleşmiş iki kişiyi bağlıyorsun.

### Metin sohbeti — v1 ✅

- Kendi WebSocket kanalın üzerinde. Yeni altyapı gerekmiyor.
- Sadece eşleşmiş partner arasında
- Hız sınırı (saniyede 1 mesaj), mesaj uzunluk sınırı
- İfade tekerleği ve not tahtası **kalıyor** — mobilde klavye açmak yavaş, hızlı iletişim için hâlâ en iyi yol

### Sesli sohbet — v2 ✅

Takvim kararı, tasarım kararı değil: sesli sohbet tek başına bir aylık iş.

- **WebRTC'yi kendin yazma.** Photon Voice 2 veya Agora — ikisinin de küçük projeler için ücretsiz katmanı var.
- **Bas-konuş.** Sürekli açık mikrofon mobilde pil ve mahremiyet felaketi.
- iOS'ta `NSMicrophoneUsageDescription` zorunlu, izin metnini şimdiden yaz.

---

## 6b. Veri saklama politikası ✅

**Ne ses ne mesaj içeriği sunucuda saklanmaz.**

| Veri | Nerede durur |
|---|---|
| Ses | Hiçbir yerde. Canlı akış, kayıt yok. |
| Mesaj geçmişi | **Sadece iki cihazda** (yerel SQLite). Sunucuda arşiv yok. |
| Engelleme listesi | Sunucuda (zorunlu — istemci tarafı engelleme işe yaramaz) |
| Not tahtası | Sunucuda, ama **şablon kimliği + nesne kimliği** olarak. Serbest metin değil, mahremiyet yükü sıfır. |

### ⚠️ Arşiv ≠ kuyruk — bu ayrımı kaçırma

"Hiçbir şey saklanmayacak" teknik olarak mümkün değil, çünkü **partner çevrimdışıyken gönderilen mesaj bir yere düşmek zorunda.** İki seçenek var:

- **A) Telsiz modeli:** İkisi de çevrimiçi değilse mesaj gitmez, kaybolur. Sıfır sunucu verisi. Ama bir çift oyununda "yazdım, gitmemiş" hissi kötü.
- **B) Teslimat kuyruğu:** Teslim edilmemiş mesaj sunucuda bekler, **teslim edilir edilmez silinir**, teslim edilmezse azami 7 gün sonra otomatik silinir. Bu bir arşiv değil, bir kuyruk.

**Önerim B.** Arşiv tutmuyorsun, geçmiş sorgulanamıyor, ama mesaj kaybolmuyor. ❓ Senin kararın.

### ⚠️ Rapor butonu — mağaza uyumu

Apple ve Google, kullanıcıların birbirine mesaj gönderebildiği uygulamalarda **rapor etme, engelleme ve müdahale mekanizması** istiyor. Hiçbir şey saklamazsan rapor butonu boşa basılan bir düğme olur ve bu bir **mağaza reddi sebebi**.

Veri saklamadan uyumlu kalmanın yolu:

> **İstemci taraflı rapor paketi.** Mesajlar sadece cihazlarda duruyor. Kullanıcı "rapor et" dediğinde, **istemci kendi yerel geçmişinden** son N mesajı rapora ekleyip gönderir. Sunucu bu paketi sadece rapor açıldığında alır, inceleme bitince siler.

Sonuç: rutin saklama sıfır, kanıt yalnızca biri yardım istediğinde oluşuyor, mağaza kuralına da uyuyorsun.

Sesli sohbet için kayıt beklenmiyor — **kullanıcıyı** rapor etme + engelleme yeterli.

### Mağaza etkisi ⚠️

Serbest sohbet + sesli sohbet, her iki mağazada da:
- Yaş derecelendirmesini yükseltir ("Kullanıcılar etkileşimde bulunur" beyanı)
- Gizlilik etiketlerinde veri toplama beyanı gerektirir
- **Yaş kapısı** koymanı gerektirir — 13 yaş altı kullanıcıya sohbet açarsan COPPA sorunu doğar

Bu üçünü v1'de bile hazırla, sesli sohbeti sonra eklesen de altyapı hazır olsun.

---

## 7. Sırt çantası ✅

Minecraft mantığı ama mobil için sadeleştirilmiş.

| Özellik | Karar |
|---|---|
| Başlangıç | **8 slot** |
| Yükseltme | 12 → 16 → 20 (marketten satın alınır, Sv 4 / 8 / 14) |
| Yığın | Slot başına 99 |
| Alet çubuğu | Ayrı **4 slot**, ekranın altında sabit |

### Mobil etkileşim ⚠️

**Sürükle-bırak yapma.** Küçük ekranda parmak eşyayı örter, hedefi göremezsin, yanlış slota düşer.

Bunun yerine **dokun-seç → dokun-hedef**: eşyaya dokun (seçili hale gelir, hafif büyür), hedef slota dokun. İki dokunuş, sıfır hata.

### Zorunlu kolaylıklar

- **Otomatik yığınlama** — aynı eşya boş slot yerine mevcut yığına gider
- **"Depoya boşalt" tek butonu** — depodaki mevcut eşyalarla eşleşenleri toplu aktarır. Bu buton olmadan oyun mobilde işkenceye döner.
- **Çanta doluyken hasat:** uyarı ver, hasadı yapma. **Yere eşya düşürme yok.** Mobilde kaybolan eşya = silinen oyun.
- Ürünler otomatik olarak alfabetik değil, **kategoriye göre** sıralansın (tohum / ürün / cevher / odun / işlenmiş)

---

## 7b. Üretim sistemleri

### Tarla ✅

Karo sayısı ve ürün çeşidi seviyeyle birlikte artar.

| Seviye | Karo | Açılan ürün |
|---|---|---|
| 1 | 4 | Başlangıç seti başlar |
| 2-5 | 8 | Başlangıç setinin kalanı (toplam **5 ürün**) |
| 6-9 | 12 | +3 ürün |
| 10-13 | 16 | +3 ürün |
| 14-17 | 20 | +3 ürün |
| 18-20 | 24 | +2 ürün (nadir, kısa mevsim) |

**Toplam ~16 ürün.** Bu sayı keyfi değil — aşağıdaki asset uyarısına bak.

**Uzun ürün = pahalı kuralı ✅:** Saat başına getiri uzun üründe ~%25 daha yüksek, ama esneklik kaybettirir. 12 saatlik ürün ektiysen o karo 12 saat kilitli; tren üç kez gelip geçer, sen bakamazsın. Karar burada doğuyor.

❓ **Öneri:** 5 başlangıç ürününü Sv 1'de birden açma. Sv 1'de 1 ürün, sonra her seviyede +1 → Sv 5'te beşi de açık. İlk 10 dakikada beş seçenek sunmak, oyuncuyu öğretmek yerine felç eder.

### Market ✅

| İşlem | Kural |
|---|---|
| Alım | Tohum, alet, çanta ve depo yükseltmesi |
| Satım | **Beklemeden**, anında — ama düşük |
| Market alış fiyatı | Taban fiyatın **%50-60'ı**, sabit (dalgalanmaz) |
| Tohum maliyeti | Beklenen hasat değerinin **%25-35'i** |

**Neden sabit fiyat:** Market dalgalanırsa oyuncunun takip etmesi gereken iki fiyat sistemi olur. Market "acil nakit musluğu" olmalı, ikinci bir strateji katmanı değil.

**Oran kontrolü:** Market %50-60 · tren ortalama %100 · manifesto 1.6-2x. Bu aralık korunduğu sürece market trenle rekabet etmez, sadece nakit sıkıştığında kullanılır. ⚠️ Marketi cömert yaparsan tren garı ölür ve oyunun ekonomik kalbi durur.

### Tren tohum getiriyor ✅

Tren sadece almaz, getirir de.

- Her seferde **2-3 çeşit tohum**, marketten **%30-40 ucuz**
- **Sınırlı adet** — bu şart. Sınırsız olursa market anlamsızlaşır.
- Çeşit rastgele (deterministik: tohum + sefer no)

Bu, her tren varışına ikinci bir sebep katıyor: sadece "ne satayım" değil, "bak bakalım ne getirmiş".

### Maden ✅

Kazma zorunlu. Dayanıklılık var, kazma kırılır.

| Kat | Kazma seviyesi | Çıktı |
|---|---|---|
| Kat 1 | Taş kazma | Taş, kömür |
| Kat 2 | Demir kazma | Kömür, demir |
| Kat 3 | Çelik kazma | Demir, altın |
| Dip | Elmas uçlu kazma | Altın, elmas |

Kazma seviyesi = kat erişimi. Madencilik ilerlemesinin omurgası bu tablo.

### ⚠️ Kazma kilitlenmesi — bunu çözmeden madeni yayınlama

Şöyle bir zincir var: **kazma kırılır → yenisi için demir lazım → demir madenden çıkar → madene girmek için kazma lazım.**

Oyuncu son kazmasını kırar, envanterinde demir yoktur, cebinde para yoktur. **Oyun kilitlenir.** Bu teorik bir risk değil, bu tasarımda kesinlikle olacak bir şey.

Üç katmanlı çözüm — üçü de olmalı:

1. **Taş kazma bedava craftlanabilir** — sadece odun + taş. Orman ve maden girişi hep bunu verir.
2. **Market her zaman taş kazma satar**, sabit ve çok ucuz fiyata.
3. **Kırılmadan önce uyarı:** dayanıklılık %20'nin altına düşünce net bir bildirim. Sessizce kırılan alet mobilde öfke üretir.

Kural: **oyuncunun elinde hiçbir zaman "kazma edinme yolu yok" durumu oluşmamalı.**

### Cevherlerin işlevi ✅

"İşlevsiz ürün istemiyoruz" dedin ve haklısın. Dört cevherin de görevi var:

| Cevher | İşlev |
|---|---|
| **Kömür** | Yakıt — eritme, işleme tezgâhı, maden feneri |
| **Demir** | Alet craft'ı: kazma, kürek, balta, kova |
| **Altın** | Ev eşyası craft'ı + yüksek değerli satış |
| **Elmas** | Elmas uçlu kazma + en değerli ev eşyaları + en yüksek satış |

Elmas hem satılıyor hem de en üst kademe alet ve eşyaya giriyor. Böylece "sadece satılan taş" olmaktan çıkıyor.

### Craft ✅

İki ayrı hat, aynı tezgâh (evde):

- **Alet hattı:** demir + odun → kazma, balta, kürek, kova · yükseltmeler
- **Ev hattı:** altın + elmas + odun → mobilya, dekorasyon, çiftin ortak eşyaları

Craft basit kalacak: **tarif sayısı 25'i geçmesin**, malzeme derinliği en fazla 2 kademe (cevher → külçe → eşya).

### Orman ✅

Ağaç türleri var, ekilir ve kesilir. Büyüme süresi ürünlerden çok daha uzun.

| Tür | Büyüme | Verim |
|---|---|---|
| Çam | 8 saat | Odun (temel) |
| Meşe | 24 saat | Sert odun (craft) |
| Ceviz | 48 saat | Değerli odun + ceviz (satış) |
| Nadir tür | 72 saat | Sv 16'da açılır |

### ⚠️ Ormanın ölü zaman problemi

Ağaçlar 8-72 saatte büyüyorsa, oyuncu ormana girer, diker, çıkar — ve bölge iki gün boyunca ziyaret edilmeyen boş bir sahne olur. Koca bir bölgeyi bunun için üretmeye değmez.

Çözüm: ormanda **iki ayrı ritim** olsun.

- **Uzun ritim:** ekilen ağaçlar (8-72 saat) — planlama katmanı
- **Kısa ritim:** kendiliğinden yenilenen yabani kaynaklar (30 dk - 2 saat) — dal, mantar, yaban meyvesi, yem

Kısa ritim oyuncuyu her girişte bir şeyle ödüllendirir, uzun ritim ona sebep verir. Kısa ritim olmadan orman ölü bölge olur.

❓ **Karar gerekiyor:** Ağaç kesilince ne kalıyor? Kütük mü (tekrar dikmek için temizlenmeli), yoksa boş karo mu? Kütük daha gerçekçi ama bir adım daha ekliyor.

### ⚠️ Asset gerçekliği — bu sayıyı şimdi sabitle

16 ürün × 4 büyüme aşaması = **64 model.** Üstüne 4 ağaç türü × 3 aşama = 12. Üstüne cevherler, aletler, ev eşyaları.

CC0 paketlerde bu kadar çeşidi **tek stilde** bulmak zor. Sırayla yap:

1. Önce Kenney / Quaternius paketlerinde **gerçekte kaç ürün modeli var** onu say
2. Ürün sayısını o sayıya göre kes
3. Sonra seviye tablosunu doldur

Ters sırada yaparsan Sv 14'te elinde modeli olmayan üç ürün kalır.

---

## 8. Tren garı ve fiyat sistemi ✅

Oyunun ekonomik kalbi.

### Tarife

- **6 saatte bir sefer** → günde 4 sefer (00:00 / 06:00 / 12:00 / 18:00 oyuncunun yerel saatine göre değil, **sunucu saatine göre** — iki oyuncu farklı saat diliminde olabilir)
- Her seferde **3 sipariş kartı**: belirli üründen belirli miktar
- Karşılanan sipariş: 1.6-2x fiyat + XP + defter ilerlemesi
- **Kaçırma cezası yok.** Kozy oyunda ceza itici olur; fırsat kaybı yeterli baskıyı zaten yaratıyor.

### Dinamik fiyatlandırma

Her seferde tüm ürün fiyatları yeniden belirlenir.

| Kural | Değer |
|---|---|
| Dalgalanma bandı | Taban fiyatın **%70 - %130**'u |
| Üretim | Sunucuda **deterministik** (tohum + sefer numarası). Veritabanına satır yazılmaz, iki istemci de aynı fiyatı görür. |
| Gösterim | Her ürünün yanında ↑ ↓ oku + son 3 seferin mini grafiği |

### Satış baskısı: depo doluluğu ✅

Ürün **asla değer kaybetmez**. Oyuncu istediği kadar yüksek fiyatı bekleyebilir. Onu satmaya zorlayan tek şey **yer sıkıntısı**: beklerken depo dolar, hasat yapamaz hale gelir.

Karar şuna dönüşür: *"%90 fiyata şimdi satıp yer mi açayım, yoksa %130'u bekleyip bu turu hasatsız mı geçireyim?"*

Bu, bozulma mekaniğinden daha iyi bir karar — çünkü oyuncu çevrimdışıyken hiçbir şey kaybetmiyor, aktif bir seçim yapıyor.

### ⚠️ Bunu çalıştıran tek denge kuralı

**Depo kapasitesi, üretim hızından yavaş büyümeli.** Aksi halde mekanik sessizce ölür: Sv 14'te depo 10 sefer yetecek kadar büyükse "baskı" diye bir şey kalmaz ve fiyat sistemi anlamsızlaşır.

| Kural | Değer |
|---|---|
| Depo kapasitesi | Azami üretimin **1.5 - 2 tren seferi** kadarı |
| Depo büyümesi | **Doğrusal** — her yükseltme sabit miktar ekler |
| Üretim büyümesi | Doğrusalın üstünde — yeni parsel + işleme zincirleri birleşince katlanır |

Bu üç satır sistemin can damarı. Depo yükseltmelerini "cömert" yaparsan tüm ekonomi tasarımı boşa gider.

### İki kademeli baskı

```
Tarla → Sırt çantası (8-20 slot) → Depo → Tren
         küçük kısıt              büyük kısıt
         hareketi zorlar          kararı zorlar
```

Çanta seni depoya gitmeye zorlar, depo seni trene gitmeye zorlar. Tek bir kısıt yerine iki kademe kullanmak, oyuncuya sürekli küçük kararlar verdirir.

### Dolu depo kuralı ⚠️

Hasat **engellenir**, net bir uyarı verilir. Ürün tarlada bekler.

**Eşya asla yok edilmez, asla yere düşmez, asla otomatik satılmaz.** Mobilde kaybolan eşya = silinen oyun.

---

## 9. Seviye seviye ilerleyiş

**Tasarım kuralı:** Her seviye **tek bir yeni şey** açar. Asla iki.

### Perde 1 — Ev kurulur (Sv 1-5) · ~1.5-2 saat

| Sv | Açılan | Hikâye |
|---|---|---|
| 1 | Ev + Tarla. Buğday, el sulama, 4 karo. Çanta 8 slot. | Vadiye varış. Defter bulunur, ilk sayfa okunur. |
| 2 | **Depo** açılır | Ortak sandık. "Burada bıraktıkların ona ulaşır." |
| 3 | **Partner daveti + ifade tekerleği + metin sohbeti** | "Bu vadide hiçbir iş tek başına görülmez." |
| 4 | Mısır (uzun süre, yüksek getiri) + çanta 12 slot | İlk gerçek karar: hızlı mı, kârlı mı? |
| 5 | **Tren garı** — ilk manifesto, 6 saatlik tarife | Perde kapanışı. Tren yıllar sonra ilk kez durur. |

⚠️ **Sv 5 oyunun kaderi.** Terk oranı en yüksek burada olacak. İlk 15 dakika ile ilk tren durağı arası, oyunun en çok cilalanmış kısmı olmalı.

### Perde 2 — Vadi açılır (Sv 6-12) · ~5-7 saat

| Sv | Açılan | Hikâye |
|---|---|---|
| 6 | **Market** — tohum, alet, çanta yükseltmesi | Vadiye ilk dışarıdan gelen. |
| 7 | **Orman** — balta, odun, yem | "Ev genişleyecekse odun lazım." |
| 8 | Kümes (evin yanı) — günlük yumurta + çanta 16 slot | Vadiye ilk canlı döner. |
| 9 | **Maden ocağı** — kazma, demir, Kat 1 | Çökük giriş açılır. İlk defter sayfası. |
| 10 | Craft tezgâhı (evde) — demir + odun → alet yükseltme | Artık ürettiğini geliştiriyorsun. |
| 11 | **Ortak proje: maden asansörü** | İki katkı zorunlu. Tek kişilikte %70 maliyet, daha uzun sürer. |
| 12 | Maden Kat 2 — kömür, fener dayanıklılığı | Derine inmenin bedeli var. |

### Perde 3 — Defter tamamlanır (Sv 13-20) · ~7-9 saat

| Sv | Açılan | Hikâye |
|---|---|---|
| 13 | Değirmen: buğday → un → ekmek | Ham satmak artık aptalca. |
| 14 | İnek, süt → peynir + çanta 20 slot | Vadi kendine yetiyor. |
| 15 | **Ortak proje: köprü** → Maden Kat 3 (bakır, gümüş) | En büyük kaynak talebi. |
| 16 | Orman derinliği — kısa mevsimli nadir toplama | Zamanlama baskısı zirvede. |
| 17 | Ev genişletme + kozmetikler | Nefes alma seviyesi. Ödül var, ilerleme yok. |
| 18 | Maden dibi | Defterin son sayfaları. Son çiftin neden ayrıldığı ortaya çıkar. |
| 19 | Büyük manifesto hazırlığı | Oyunun tek en büyük siparişi. |
| 20 | **Amorland tarifeye döner** | Final. Defter oyuncuların adıyla imzalanır. |

**Sv 11 ve 15'in rolü:** Bunlar duvar değil **buluşma noktası** — çiftin oturup plan yapmak zorunda kaldığı tek yer. Oyunun sosyal doruk noktaları.

### Sv 20 sonrası

Sonsuz haftalık manifestolar · kozmetik koleksiyon · yeni bir eşle yeni vadi (seviye korunur)

---

## 10. Geri dönme sebepleri

1. **Tren tarifesi** — 6 saatte bir, ceza yok
2. **Yumurta / süt** — günlük 30 saniyelik toplama
3. **Not tahtası + sohbet** — "eşin sana not bıraktı" en güçlü bildirim
4. **Günlük sandık** — küçük, tahmin edilebilir

**Bildirim kuralı:** Günde en fazla 2. Fazlası kaldırılma sebebi.
**Bildirim yasağı:** Küslük durumu asla bildirim göndermez (bkz. Bölüm 5).

---

## 11. Ekonomi çatısı

| Kural | Değer |
|---|---|
| Seviye başına XP artışı | ~%20 bileşik |
| Büyüme basamakları | 5 dk / 30 dk / 2 sa / 6 sa / 12 sa |
| Uzun ürün avantajı | Saat başına ~%25 daha kârlı, esneklik kaybettirir |
| İşlenmiş / ham fiyat | 2.5x - 3x |
| Manifesto / normal satış | 1.6x - 2x (küslükte 1.3x) |

**6 ve 12 saatlik ürünler neden önemli:** 6 saat tren tarifesiyle hizalanıyor, 12 saat oyuncunun yatmadan ekip sabah dönmesini sağlıyor. Günlük ritmin çapası bu ikisi.

### ⚠️ Para çıkışları — çiftlik oyunlarının en sık öldüğü yer

Gelir tarafını tasarladık, gider tarafını tasarlamadık. Sv 15'te altın ve elmas akmaya başlayınca, harcayacak yer yoksa **para anlamsızlaşır** ve oyunun tüm ekonomik gerilimi çöker. Oyuncu artık hiçbir karar vermiyordur.

Çıkışlar ve ölçekleri:

| Çıkış | Ölçek |
|---|---|
| Tohum | Sürekli, gelirle birlikte büyür |
| Alet yükseltme | Kademeli, her kademe öncekinin ~2.5 katı |
| Tarla karosu genişletme | Kademeli, artan maliyet |
| Depo ve çanta yükseltmesi | Kademeli, **kasıtlı olarak pahalı** (bkz. Bölüm 8 denge kuralı) |
| Ev eşyası craft'ı | Sonsuz — altın/elmasın ana kuyusu |
| **Vadi anıtı** ❓ | Geç oyun prestij çıkışı |

**Vadi anıtı fikri:** Sv 17 sonrası açılan, çiftin birlikte inşa ettiği tek bir büyük yapı. Altın ve elmasla kademe kademe büyür, hiçbir işlevsel fayda vermez — sadece haritada görünür ve defterde kaydı olur.

İki işi birden yapıyor: geç oyunun para kuyusunu kapatıyor ve çift temasına doğrudan bağlanıyor. "Birlikte yaptığımız şey" oyunun kendi hikâyesi zaten.

---

## 12. Reklam ❓

Sadece **ödüllü reklam**. Zorunlu geçiş reklamı yok, banner yok.

| Yer | Ödül |
|---|---|
| Hasat sonrası | Bu hasadı 2x yap |
| Maden girişi | Fener dayanıklılığını tazele |
| Günlük sandık | İki katına çıkar |

**Asla:** İlerlemeyi kilitleyen, süre kısaltmayı satan, ya da çift arasında dengesizlik yaratan reklam.

---

## 13. Android + iOS ⚠️

### Bütçe gerçeği

| Kalem | Maliyet |
|---|---|
| Google Play geliştirici hesabı | 25 $ tek seferlik |
| Apple Developer Program | 99 $ / yıl |
| **iOS derlemesi için Mac** | Zorunlu. Ya Mac mini ya bulut derleme (Codemagic / Unity Cloud Build). |

Mac konusunu erken planla — iOS'u sona bırakırsan yayın tarihin kayar.

### Teknik hedefler

- Unity 6 LTS + URP, tek geçişli forward
- Hedef: orta segmentte 60 FPS, alt segmentte 30 FPS geri düşüş
- Doku atlası + GPU instancing + statik batching
- Gerçek zamanlı gölge minimum — üstten bakışta sahte gölge (blob) yeterli ve bedava
- Bölge sahneleri **Addressables** ile, asenkron yükleme
- Hedef indirme boyutu: 150 MB altı (Play Store'da uyarısız kurulum sınırı)

### ⚠️ En sık atlanan iki mobil sorunu

1. **Arka plana atılınca WebSocket kopar.** iOS uygulamayı arka planda birkaç saniyede dondurur. Yeniden bağlanma + durum senkronizasyonu stratejisini baştan yaz, sonradan eklenmez.
2. **Cihaz saati değişir.** Oyuncu telefon saatini ileri alarak ürün büyütmeye çalışır. Tüm zaman hesapları **sunucu saatiyle** yapılmalı, istemci saatine asla güvenilmemeli.

---

## 14. Yapım sırası

1. **Dikey dilim** — Ev + Tarla, tek ürün, ek/sula/biç, iki oyuncu bağlanıyor, durum kalıcı
2. Perde 1 tam (Sv 1-5) — oyunun vitrini burası
3. Market + Orman (Sv 6-7)
4. Maden + craft (Sv 9-10)
5. İlk ortak proje (Sv 11) — sosyal tasarımın gerçek testi
6. Perde 3 içeriği
7. Cila, ses efektleri, bildirim, mağaza materyali
8. **(v2)** Sesli sohbet

**Perde 1 bitmeden Perde 3'e tek satır içerik üretme.**

---

## 15. Teknik özet

- **İstemci:** Unity 6 LTS + URP + New Input System + Addressables
- **Sunucu:** Spring Boot (Win10), önünde Caddy
- **Veri:** PostgreSQL (dünya, envanter, oyuncu), Redis (oturum, presence, sohbet hız sınırı)
- **Kanallar:** REST + JWT (meta), WebSocket (anlık senkron + metin sohbeti), üçüncü parti SDK (ses)
- **Otorite:** Tamamen sunucuda. İstemci niyet gönderir, sunucu sonucu hesaplar.
- **Büyüme:** Cron yok. `planted_at` saklanır, aşama okuma anında hesaplanır.
- **Fiyatlar:** Deterministik üretim (tohum + sefer no), veritabanına yazılmaz.
- **Dayanıklılık:** Her komut `client_action_id` (UUID) taşır, sunucu tekrarı yutar.
- **Görsel:** CC0 kaynaklar (Kenney, Quaternius, KayKit, Poly Pizza, Mixamo). Tek stile kilitlen.

---

## 16. Açık kalan konular

| # | Konu | Durum |
|---|---|---|
| 1 | "Amorland" isim müsaitliği | Marka, paket adı (`com.x.amorland`), alan adı ve mağaza araması kontrol edilmeli |
| 2 | Reklam yerleşimi | Taslak hazır, onay bekliyor |
| 3 | Mevsim sistemi olacak mı | Sv 16'daki "kısa mevsimli toplama" mevsim gerektiriyor — yoksa basitleştirilmeli |
| 4 | Kozmetik kaynağı | Sadece oyun içi mi, ödüllü reklamla da mı |
| 5 | Ev genişletme kapsamı | Görsel mi, işlevsel mi (depo kapasitesi vb.) |
| 6 | Küslük mekaniği detayı | Ertelendi, birlikte derinleştirilecek. Bölüm 5'teki kırmızı çizgi korunuyor. |
| 7 | Çevrimdışı mesaj | Telsiz modeli mi (kaybolur), teslimat kuyruğu mu (teslimde silinir)? |
| 8 | Depo kapasite sayıları | Sv 2 / 6 / 11 / 15 yükseltmelerinde kaç slot — dikey dilim testinde ölçülecek |
| 9 | Balta dayanıklılığı | Kazma kırılıyorsa balta da kırılmalı mı? Tutarlılık meselesi. |
| 10 | Ağaç kesildikten sonra | Kütük mü kalıyor (temizlenmeli), boş karo mu? |
| 11 | Ortak düğüm çakışması | İki oyuncu aynı cevher damarına / ağaca aynı anda vurursa ne olur? |
| 12 | Sulama sınırı | Kova kapasitesi + kuyuya gidiş olacak mı, yoksa sınırsız sulama mı? |
| 13 | Başlangıç ürünleri | Beşi de Sv 1'de mi, yoksa Perde 1 boyunca dağıtılsın mı? |
| 14 | Vadi anıtı | Geç oyun para çıkışı olarak eklensin mi? |
| **15** | **Sanat yönü** ⚠️ | **Low-poly 3D mü, pixel art 2D mi? Doküman 3D varsayıyor, konsept görseller 2D. Bu karar teknik planın yarısını belirliyor — en öncelikli madde.** |
| 16 | Enerji barı | Konsept görselde var, tasarımda yok. Dördüncü kısıt gerekli mi? Reklam kapısına dönüşme riski var. |
| 17 | Premium para birimi | Konsept görselde satın alınabilir elmas var. Bizim elmasımız madenden çıkıyor. İkisi bir arada olamaz. |
| 18 | Mevsim / oyun içi gün | Konsept görselde "İlkbahar 18. Gün" var. Gerçek zamanlı büyüme kararıyla çelişiyor. |
| 19 | Çapa ve gübre | Konsept görselde alet çubuğunda var, tasarımda yok. Eklenecek mi? |
