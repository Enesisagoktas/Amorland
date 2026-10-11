# AMORLAND — Tasarım ve Yapım Belgesi

> Oyunun tasarımı, sunucu protokolü ve tüm denge sayıları bu belgededir.
> Sürüm 1.0 · 2026-08-20

---

# BÖLÜM A — Kapsam ve çalışma kuralları

## A.1 Proje

Amorland, çiftler için tasarlanmış üstten bakışlı 2.5D mobil çiftlik oyunu. Android + iOS, ücretsiz, Unity istemci + Spring Boot sunucu.

Tasarım bitmiştir. Bölüm B, C ve D'deki kararlar simülasyonla doğrulanmış ve dış incelemeden geçmiştir. Amaç tasarımı yeniden tartışmak değil, inşa etmektir.

## A.2 Kısıtlar

Tek kişilik ekip: sanatçı, ses tasarımcısı ve test ekibi yok. Asset bütçesi 0 TL.

## A.3 ⚠️ Kapsam — dürüst tahmin

Bu 20 seviye, 7 bölge, 26 ekran, çok oyunculu, sunucu + iki platform demek. Tek kişilik **6-18 aylık** bir iş. Tek oturumda bitirilemez.

| Katman | Durum | Not |
|---|---|---|
| Sunucu (Spring Boot, Postgres, WS, auth, oyun mantığı) | ✅ tamamı | Geliştiricinin de alanı, en hızlı ilerleyen yer |
| Unity C# kodu (oynanış, ağ, UI mantığı, durum) | ✅ tamamı | |
| Unity Editor işi (sahne, prefab, inspector, animator, canvas) | ⚠️ kısmen | Editörde tıklanamaz. Editor scripting API ile programatik kurulabilir ama kırılgan. **Bu geliştiricinin işi.** |
| Asset | ⚠️ işlevsel | Üreteç script'i yazılır. Sonuç temiz low-poly olur, sevimli olmaz. |
| His ayarı (yürüyüş hızı, dokunma hedefi, sıkıcılık) | ❌ | Oynanarak bulunur |
| Cihaz testi, mağaza gönderimi, iOS/Mac derlemesi | ❌ | |

## A.4 Yapım sırası — bu sırayı bozma

1. **DİKEY DİLİM** ← her zaman buradan başla
   Ev + Tarla, tek ürün, ek/sula/biç, **iki oyuncu bağlı**, durum kalıcı.
   Ayrıca: ses altyapısı (mixer + havuz + varyasyon) + yer tutucu ses seti.
   *Bu çalışmıyorsa gerisi konuşulmaz.*
2. **Perde 1 tam (Sv 1-5)** — oyunun vitrini. Eylem sesleri + haptik + tarla/ev ortamı.
3. Market + Orman (Sv 6-7)
4. Maden + craft (Sv 9-10)
5. İlk ortak proje (Sv 11) — sosyal tasarımın gerçek testi
6. Perde 3 içeriği
7. Cila: UI sesleri, müzik, karışım, bildirim, mağaza materyali
8. **(v2)** Sesli sohbet

> ⚠️ **Perde 1 bitmeden Perde 3'e tek satır içerik üretme.**

## A.5 Asset stratejisi (bütçe 0 TL)

1. **CC0 paketler** (Kenney, Quaternius, KayKit, Poly Pizza) — karakter, yapı, prop. İhtiyacın ~%70'i.
2. **Parametrik Blender script'i** — 16 ürün × 4 aşama = 64 model gibi *tutarlılık gerektiren* setler. Tek script, garantili aynı stil, CPU yeter.
3. **Saf Python** (Pillow/SVG) — UI ikonları, zemin dokuları, ortografik sprite render.

⚠️ **AI 3D üretimi bu proje için yanlış araç** — 4090 olsa bile. Her çalıştırmada farklı siluet, oran, palet ve bozuk topoloji üretir; "aynı havucun %40 büyümüşü" diyemezsin. Script diyebilir.

**Ses:** en iyi 0 TL kaynak geliştiricinin telefonu. Sulama = kovadan leğene su dök. Balta = tahtaya tahta vur. Her sesin 5-6 tekrarını kaydet, Audacity ile normalize et. Kalanı CC0 (Kenney Audio, Freesound **CC0 filtresiyle**).

## A.6 Doğrulama kuralı ⚠️

Bu projede en pahalı hatalar **sessizce doğru görünen** hatalardı. Üç örnek — üçü de gerçekten oldu:

- Depo baskısı mekaniği hiç tetiklenmiyordu (yığın 99 iken depo asla dolmuyordu)
- Ortak düğüm kuralı toplamda **4× cevher** üretiyordu, kimse fark etmemişti
- Saatlik getiri tablosu gerçek oturum modelinde **14-80 kat** sapıyordu

Bu yüzden:

> **Bir sayıyı veya kuralı değiştirdiğinde, etkisini ölç. Tahmin etme.**
> Denge değişikliklerinden sonra ekonomi simülasyonunu tekrar koştur.
> "Çalışıyor" demeden önce çalıştır ve çıktıyı göster.

Ve şunu unutma: **görmediğin şeyi simüle edemezsin.** Bu dokümandaki her sayı tutarlı, ama hiçbiri oynanarak test edilmedi.

## A.7 Bilinen riskler — bunları akılda tut

| Risk | Neden |
|---|---|
| **Depo kapasitesi** | Dikey dilimde ölçülecek 1 numaralı sayı. Cömert yaparsan tüm ekonomi tasarımı boşa gider. |
| **Sv 5 terk oranı** | Oyunun kaderi. İlk 15 dakika ile ilk tren durağı arası en cilalı kısım olmalı. |
| **Alet kilitlenmesi** | Hiçbir alet, o aleti yapmak için gereken kaynağın tek edinme yolu olmamalı. |
| **Unity + Git** | `.gitignore` ve Git LFS şart. `.unity` ve `.prefab` çakışmaları araçla çözülmez. |
| **Arka planda WebSocket kopar** | iOS birkaç saniyede öldürür. Yeniden bağlanma istisna değil, normal durum. |
| **iOS derlemesi Mac gerektirir** | Sona bırakma, yayın tarihi kayar. |
| **Ortak proje geçilebilirliği** | Sv 11 ve 15 tek kişide %70 maliyetle gerçekten geçilebiliyor mu — sadece oyun testi söyler. |

## A.8 Kapsam dışı — bunları yazmaya çalışma

| Konu | Ne zaman |
|---|---|
| 20 defter sayfasının gerçek metni | Perde perde, yazarlık işi |
| Ses dosyaları, müzik besteleri | Üretim işi |
| İsim/marka müsaitliği, alan adı | Yayın kararından önce. **Sıra: mağaza araması → marka → paket adı → domain.** Paket adı bir kez alınınca değişmez. |
| Yerelleştirme içeriği | Altyapı baştan (string tablosu), metin cilada |

---

# BÖLÜM B — Oyun spesifikasyonu

## B.0 Görev

**Amorland**, çiftler için tasarlanmış, üstten bakışlı 2.5D mobil çiftlik oyunudur. Android + iOS, yatay, ücretsiz.

İki kişilik ortak oynanış ana odaktır; tek kişilik de oynanabilir ama **daha zordur ve bu bir denge ayarı değil, dünyanın kuralıdır.**

- ~15-20 saat oynanış (yarısı gerçek zamanlı bekleme), 20 seviye, ~16 gün takvim süresi
- Oturum modeli: günde 3-4 kez, 5-10 dakika
- Gelir modeli: sadece ödüllü reklam. Zorunlu reklam yok, banner yok, premium para birimi yok.

---

## B.1. Hikâye

Amorland, vadinin eski adı. Buranın tek bir kuralı vardı: **hiçbir iş tek başına görülmez.**

- Tren manifestosu iki imza ister
- Madende ikinci bir el olmadan derine inilmez
- Tarlada biri eker, biri sular

Vadi boşaldı çünkü son çift ayrıldı ve manifestoyu bir daha kimse ikinci kez imzalamadı.

**Anlatım aracı:** Evde yarım kalmış bir "birlikte yapılanlar defteri". 20 sayfa, her biri bir olayla açılır.
**Ara sahne yok. Diyalog ağacı yok. Seslendirme yok.** Anlatım tamamen toplanabilir metin sayfalarıyla yürür.

**Final:** Amorland tren tarifesine geri döner, defterin son sayfası oyuncuların adıyla imzalanır.

---

## B.2. Teknik yığın

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

## B.3. Değişmez mimari kuralları

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

## B.4. Dünya yapısı — 7 bölge

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

## B.5. Kamera ve ölçek

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

## B.6. Etkileşim modeli

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

## B.7. Ortak oynanış

### Temel kural — ortak dünya, ayrı tarlalar
Depo, maden, orman, gar, market **ortak**. Tarla iki parsele bölünür, **kimse diğerinin parseline dokunamaz.**

Tek kişilik ayrı bir mod değildir: aynı dünya, iki parsel de sende. **Sıfır ekstra içerik.**

### İki seviye kavramı
| Kavram | Belirler | Hesap |
|---|---|---|
| **Dünya seviyesi** | Hangi bölge/kat açık | İki oyuncunun **yükseği** |
| **Oyuncu seviyesi** | Kendi parseli, çantası, tohumları | Kişisel XP |

**Yetişme bonusu:** Oyuncu partnerinden 3+ seviye gerideyse XP'si **×1.5**. Fark 2'ye düşünce veya **14 gün sonra** kapanır (bilerek geri kalıp biriktirmeyi engeller).

### Tek kişilik: daha yavaş değil, daha yalın

Ölçüm: **solo 15.3 gün · duo 14.1 gün → süre farkı sadece 1.08×.** %70 ölçekleme ilerleme süresine neredeyse etki etmiyor, çünkü XP'nin çoğu kendi parselinden geliyor.

> Fark **bolluk** farkıdır, duvar değil: solo %19 daha az altın, %25 daha az cevher.

Bir çift oyununda tek oyuncuyu süre olarak cezalandırmak yanlış olurdu — partner bulamamak kusur değil. Bolluk farkı hikâyeyi ("tek imzalı manifesto daha az karşılık bulur") koruyor, ilerlemeyi tıkamıyor.

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
İki oyuncu aynı cevher/ağaca vurursa: **paylaşımlı sağlık.** Vuruşlar aynı havuzdan iner. **Katkısı %25 üstünde olan HER oyuncu tam getiriyi alır** (bölünmez).

⚠️ **Ortak vurulan düğümün sağlığı ×1.5 olur.** Bu olmadan ölçüm oyuncu başına 2×, toplamda **4× cevher** gösterdi — ekonomi tablosu tek oyuncu varsayımıyla kurulmuştu. ×1.5 ile avantaj oyuncu başına 1.33×'e iner ve kural korunur.

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

## B.8. Küslük ve ayrılık

### Küslük — geçici
| Özellik | Karar |
|---|---|
| Tetikleme | **Sadece manuel.** Bir oyuncu "küstüm" der. |
| Etki | **Ekonomik etki YOK.** Sadece avatar üstünde küçük gösterge + barışma ritüeli açılır. |
| Bitiş | Depoya hediye **veya** "barışalım" → karşı taraf kabul → anında normal |
| Süre sınırı | 48 saat sonra kendiliğinden düşer |

### ⚠️ KIRMIZI ÇİZGİ — bunu asla kaybetme
**Küslük asla hareketsizlikle tetiklenmez ve asla bildirim göndermez.**

"Partnerin 3 gündür girmedi, küs duruma geçtiniz" gibi bir mekanik gerçek bir ilişkide gerçek bir suçluluk üretir. Oyun, birinin sınavı olduğu için giremediği bir haftayı ilişki sorununa çevirmemeli.

> Küslük, çiftin oynadığı bir **rol** olmalı — oyunun çifte uyguladığı bir yaptırım değil.

⚠️ **v0.6 düzeltmesi:** Küslük eskiden manifesto bonusunu 1.8× → 1.3× düşürüyordu. Bu, bir oyuncunun partnerini **ekonomik olarak cezalandırabildiği bir düğmeydi** — bir rolün para cezası olmasına gerek yok, ve para cezası olduğu anda rol olmaktan çıkıp silaha dönüşüyordu. Ekonomik etki tamamen kaldırıldı. Geriye değerli olan kısım kalıyor: "kırıldım" diyebilmek için biçimsel bir kanal ve bir barışma ritüeli.

### Ayrılık — kalıcı
- Tek taraflı istek → bildirim → **48 saat** bekleme. Çift taraflı onayda bekleme yok.
- Ayrılan oyuncu **seviyesini ve çantasını koruyarak** yeni vadi kurar
- Ev ve depo vadiyi kuranda kalır
- **7 gün** yeni eşleşme yok
- Defter ilerlemesi ikisinde de kalır
- **Bir oyuncu aynı anda tek vadide olabilir**

### Ayrılıkta sahiplik — tam tablo

| Varlık | Kim alır |
|---|---|
| Seviye, XP, çanta, aletler | Kendinde |
| **Depo içeriği** | Vadiyi kurana — ama **ayrılan %50'sini "yol azığı" olarak alır** |
| Kendi parselindeki ekili ürünler | Kaybolur; **tohum değeri altın olarak iade** |
| Ev, depo yapısı ve kapasitesi, hayvanlar, vadi anıtı | Vadiyi kurana |
| **Craft tarif açılımları** | **İkisinde de** — öğrenilmiş bilgi geri alınmaz |
| Kozmetikler, çanta yükseltmeleri | İkisinde de (kişisel) |
| Defter ilerlemesi | İkisinde de |
| Dünya seviyesi, bölge/kat açılışları | Yeni vadide **kendi seviyesine göre** yeniden hesaplanır |

⚠️ **%50 depo payı kasıtlı:** onsuz vadiyi kuran oyuncu partnerini hiçbir şeysiz bırakabilirdi.

---

## B.9. Sohbet, gizlilik, mağaza uyumu

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

## B.10. Çanta ve depo — iki FARKLI kısıt

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
| 460 | 18 | 100.000 | 1.7 |

⚠️ **Depo kapasitesi üretimden YAVAŞ büyümeli.** Büyüme doğrusal (+60/+80/+100/+120), maliyet üstel. Sefer karşılığı 2.5→1.7 düşer: **baskı ilerledikçe artar.** Cömert yaparsan tüm ekonomi tasarımı boşa gider.

### Ortak kullanım UX'i
| Konu | Karar |
|---|---|
| Kim koydu | Her yığında **koyanın renk rozeti** |
| Partner alabilir mi | **Evet** — "sana demir bıraktım" bunun üzerine kurulu |
| Kilitleme | Oyuncu **en fazla 3 yığın** kilitleyebilir. Sınır kasıtlı: yoksa depo ikiye bölünür. |
| Kazara boşaltma | **"Depoya boşalt" var, "depodan al" toplu butonu YOK.** Tek yönlü kolaylık. |
| Eşzamanlı alma | Sunucuya ilk ulaşan kazanır, diğeri `ESYA_YOK` |

### Zorunlu kolaylıklar
- **Otomatik yığınlama**
- **"Depoya boşalt" tek butonu** — bu buton olmadan oyun mobilde işkence
- **Depo dolu:** hasat **engellenir**, ürün tarlada bekler, net uyarı
- Kategoriye göre sıralama (tohum / ürün / cevher / odun / işlenmiş)

> ⚠️ **Eşya asla yok edilmez, asla yere düşmez, asla otomatik satılmaz.** Mobilde kaybolan eşya = silinen oyun.

---

## B.11. Ürünler — 16 ürün

**Saatlik getiri kademesi** (taban fiyat türetme aracı):
5 dk: 24 · 30 dk: 30 · 2 sa: 36 · 6 sa: 42 · 12 sa: 48 altın/saat/karo

⚠️ **Bu kademe sadece büyüme süresi ≥ oturum arası (6.9 sa) olan ürünler için geçerlidir.** Günde 3.5 oturumda kısa ürün olgunlaştıktan sonra karoyu boş bekletir; gerçekleşen getirisi kademenin 14-80 katı altında kalır.

> **Uzun ürün oyuncu yokken çalışır. Kısa ürün oyuncu oradayken çalışır.**
> 5-30 dakikalık ürünler **aktif oturum dolgusudur** — oyuncu zaten oyundayken bir hasat daha verir. Saatlik getiri onların ölçüsü değildir.

Kademe içi **±%10 sapma kasıtlıdır** — 4 ürün birebir aynı fiyatta olsaydı seçim anlamsız olurdu.

| # | Ürün | Açılış | Süre | Fiyat | Tohum | Özel |
|---|---|---|---|---|---|---|
| 1 | Buğday | Sv 1 ödül | 5 dk | 2 | 1 | Öğretici + un girdisi |
| 2 | Turp | Sv 2 ödül | 30 dk | 15 | 5 | |
| 3 | Havuç | Sv 3 ödül | 30 dk | 16 | 5 | |
| 4 | Mısır | Sv 4 ödül | 2 sa | 72 | 22 | İlk gerçek karar |
| 5 | Patates | Sv 5 ödül | 2 sa | 68 | 20 | |
| 6 | Domates | Sv 6 market | 2 sa | 85 | 24 | **Yeniden verir ×4** (6 sa) |
| 7 | Marul | Sv 7 market | 30 dk | 14 | 4 | |
| 8 | Kabak | Sv 8 market | 6 sa | 252 | 76 | |
| 9 | Çilek | Sv 10 market | 6 sa | 230 | 70 | **Yeniden verir ×4** (3 sa) |
| 10 | Ayçiçeği | Sv 11 market | 6 sa | 245 | 74 | |
| 11 | Lahana | Sv 12 market | 6 sa | 260 | 78 | |
| 12 | Karpuz | Sv 14 market | 12 sa | 576 | 175 | |
| 13 | Üzüm | Sv 15 market | 12 sa | 540 | 165 | **Yeniden verir ×3** (12 sa) |
| 14 | Balkabağı | Sv 16 market | 12 sa | 600 | 180 | |
| 15 | Kavun | Sv 18 market | 12 sa | 590 | 178 | **Tohum penceresi** |
| 16 | Safran | Sv 19 market | 12 sa | 900 | 420 | Nadir, **tohum penceresi**, kıt tohum |

### ⚠️ Başlangıç ürünü kuralı (kalıcı, çok önemli)
> **Buğday, turp, havuç, mısır, patates** hasatta **1 ürün + 1 tohum** verir. Market ürünleri (Sv 6+) vermez.

Üç işi birden yapar: Perde 1 kendi kendine yeter · başlangıç ürünleri geç oyunda **bedava ama düşük getirili güvenlik ağı** kalır · market ürünleri gerçek yatırım kararı olur.

### Yeniden veren ürünler (domates, çilek, üzüm)
| Kural | Karar |
|---|---|
| `×N` ne sayar | **Toplam** hasat (ilk dahil). ×3 = 1 ilk + 2 yeniden. |
| Tohum | Sadece ekimde bir kez |
| Son hasattan sonra | Karo boşalır |
| Gübre | **Sadece bir sonraki hasata** +1, tüm döngüye değil |
| Depo doluysa | Hasat engellenir, **yeniden verme sayacı işlemez** |
| Fiyat | Kademesinin **%15-20 üstünde** — karoyu 3-4 kat uzun kilitlemenin karşılığı |

### Tohum penceresi (kavun, safran)
Bu ürünlerin **tohumu** markette sadece belirli pencerelerde satılır (günde 3 kez, 2 saat, deterministik). **Büyüme ve hasat normaldir, hasat penceresi yoktur** — ürün tarlada bekleyebilir, çürümez. Fiyat primi bu arz kısıtından gelir: ölçekli ekilemez.

⚠️ Bu, Sv 16 ormandaki **kısa pencereli toplama**dan farklı bir sistemdir, karıştırma.

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

## B.12. Orman

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

## B.13. Maden

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

## B.14. Aletler ve craft

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
| | Tavuk | İnek |
|---|---|---|
| Açılış | Sv 8 (kümes) | Sv 14 (ahır) |
| Satın alma | 500 | 4.000 |
| Ürün (fiyat) | Yumurta (40) | Süt (180) |
| Periyot | 24 sa | 24 sa |
| Yem / periyot | 1 | 2 |
| XP | 3 | 8 |

**Kapasite:** Kümes 4 (Sv 8) → 8 (Sv 12, 6.000) · Ahır 2 (Sv 14) → 4 (Sv 17, 40.000)

**Yem:** ormandan yem otu (45 dk yenilenme) veya marketten 12 altın. Craftlanmaz — 25 tarif sınırı dolu.

**Besleme: TEK DOKUNUŞLA yapıdaki tüm hayvanlar.** 8 tavuğu tek tek beslemek mobilde işkence; bağlam duyarlı etkileşim felsefesiyle tutarlı.

**Toplama:** ürün hayvanın yanında belirir, dokunarak **çantaya** alınır — otomatik depoya gitmez, depo kısıtı korunmalı.

**Birikme limiti: 3 periyot.** 3 günden fazla birikmez, üretim durur. Sınırsız çevrimdışı birikim yok.

**Yem yoksa üretmez, ÖLMEZ.** Mutluluk, hastalık, ölüm, üreme **yok** — kozy oyun, kapsam dışı.

---

## B.15. Tren garı ve ekonomi

Oyunun ekonomik kalbi.

### Tarife
- **6 saatte bir sefer** → 00:00 / 06:00 / 12:00 / 18:00 **UTC** (sunucu saati; iki oyuncu farklı saat diliminde olabilir)
- Her seferde **3 sipariş kartı**
- Karşılanan sipariş: **1.6-2.0×** fiyat + 60 XP + defter ilerlemesi
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

## B.16. XP ve seviye

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

## B.17. Seviye seviye ilerleyiş

**Tasarım kuralı: Her seviye tek bir yeni ETKİLEŞİM açar.**

Pasif açılımlar sayılmaz — ürün çeşidi, kapasite artışı (çanta/depo/tarla), yeni craft tarifi, ekonomik gösterge.

| Sayılır (etkileşim) | Sayılmaz (pasif) |
|---|---|
| Yeni bölge · yeni yapı · yeni iletişim kanalı · ortak proje · maden katı | Yeni ürün · kapasite yükseltmesi · yeni tarif · altın göstergesinin belirmesi |

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

## B.18. Öğretici — ilk 15 dakika

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

## B.19. Hesap ve kurtarma

| Aşama | Kural |
|---|---|
| İlk açılış | Misafir hesap (cihaz ID), **sürtünmesiz** |
| Sv 5 / 8 / 12 | Hesap bağlama teklifi (e-posta / Google / Apple), atlanabilir, en fazla 3 kez sorar |
| **Partner davetinde** | **ZORUNLU** — artık başkasının ilerlemesi sana bağlı, gerekçesi ekranda yazar |
| Bağlamak istemeyen | **12 haneli kurtarma kodu**, oyuncu üretir, kendi saklar |

⚠️ Bu olmadan: telefon değişti → 20 saat gitti → **ve partnerin dünyası da bozuldu.**

---

## B.20. Ekran envanteri — 26 ekran + 1 kalıcı HUD

**HUD:** seviye+XP · altın (Sv 5+) · çanta · tohum seçici (4 slot) · harita · sohbet · partner göstergesi (çevrimiçi/çevrimdışı/küs)

Açılış+isim · yaş kapısı · vadi haritası · **7 bölge sahnesi** · çanta · depo · craft · tren garı · market · defter · sohbet · partner daveti · partner paneli · ortak proje tahtası · ayarlar · rapor akışı · reklam onayı · seviye atlama · günlük sandık · yükleme ekranı

**Perde 1 için gereken: 19 ekran.**

---

## B.21. Defter — 20 sayfa

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

## B.22. Sanat yönü

**Elle çizilmiş 2B illüstrasyon sprite, ortografik 2.5D yerleşim.**

> ⚠️ **Bu bölüm 20.08.2026'da değişti.** Önceki karar "low-poly 3D, düz renk, gradyan yok"
> idi. Referans görseller üzerinden yön değiştirildi: hedef, dokulu ve gradyanlı el çizimi
> illüstrasyon görünümü. Kamera kararı (2.5D ortografik, yaw kilidi) **aynen geçerli**;
> değişen yalnızca varlıkların nasıl üretildiği ve nasıl göründüğü.
>
> Eski karara ait "düz renk / gradyan yok / 12-16 renk / üçgen bütçesi" maddeleri
> **geçersizdir.** Kodda `Gorsel/Palet.cs` hâlâ 16 renkli dünya paletini taşıyor; o palet
> artık *zorunlu sözleşme* değil, sprite'lar gelene kadarki yer tutucudur.

### Stil sözleşmesi
- **2B sprite**, ortografik 2.5D ızgaraya oturur. Derinlik sıralaması y eksenine göre.
- Dokulu ve gradyanlı — el çizimi hissi hedeflenir, düz renk kısıtı **yoktur**
- Sıcak, doygun, masalsı ton. Kozy = keskin kontrast yok, ama **soluk da değil**
- Aynı ürünün 4 büyüme aşaması **aynı siluet ailesinden** olmalı (tutarlılık riski burada)
- Sahte gölge sprite'ı her nesnenin altında

### Arayüz stil sözleşmesi
Arayüz kabuğu iki dilden oluşur — ikisi karışmaz:

| Katman | Görünüm | Nerede |
|---|---|---|
| **HUD kapsülü** | Koyu yarı saydam gövde + **açık krem çerçeve** (2-3 px) | Seviye, kaynak sayaçları, ikon butonları, zaman |
| **Parşömen panel** | Krem gövde + **kalın kahve çerçeve** (4-7 px) | Modal ekranlar, görev kartı, alet çubuğu, liste satırları |

- Seçili öge **altın çerçeve** alır (kalınlık artar, renk değişir)
- Koyu zeminde yazı **beyaz + kontur**; krem zeminde **koyu kahve**, kontursuz
- Ana eylem doygun **yeşil**, ikincil **ahşap**, yıkıcı **kırmızı** — hepsi pahlı ve alt tabanlı
- Her sayaç ve her alet **ikon taşır**; yalnızca yazı etiketi yetmez

⚠️ Koyu, çerçevesiz, düz panel **yasaktır.** Zengin bir dünyanın üzerinde kaybolur ve
arayüzü "sistem menüsü" gibi gösterir. Okunurluğu sağlayan şey kapsülün kendisi değil,
etrafındaki açık hattır.

### Kaynak stratejisi (bütçe 0 TL)
1. **AI 2B sprite üretimi** — dünya nesneleri, ürünler, ikonlar. Tek üslup tarifiyle toplu
   üretim, sonra elle rötuş ve palet birleştirme.
2. **CC0 2B paketler** (Kenney, Game-icons.net) — arayüz ikonları ve doldurucu prop.
3. **Saf Python** (Pillow/SVG) — atlas paketleme, palet normalize, aşama varyantı türetme.

⚠️ **Tutarlılık asıl risktir.** AI üretimi her çalıştırmada farklı üslup, oran ve palet
verir. Önlem: (a) tek ve sabit üslup tarifi, (b) her set **tek oturumda** üretilir,
(c) üretim sonrası ortak palete normalize edilir, (d) "aynı havucun %40 büyüğü" gibi
seriler **aynı taban görselden türetilir**, sıfırdan üretilmez.

⚠️ AI **3B** üretimi hâlâ yanlış araçtır (bozuk topoloji, tutarsız siluet). Bu bölümdeki
karar 2B içindir; ikisi karıştırılmamalı.

---

## B.23. Ses ve haptik

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

## B.24. Reklam ve mağaza

### Reklam — sadece ödüllü
| Yer | Ödül |
|---|---|
| Hasat sonrası | Bu hasadı 2× yap |
| Maden girişi | Fener dayanıklılığını tazele |
| Günlük sandık | İki katına çıkar |

**Günde en fazla 5 · ilk 30 dakikada yok · çanta doluysa reklam BAŞLATILMAZ**

⚠️ **Ödül atomikliği zorunlu.** İstemci ödülü asla kendisi vermez: sunucudan jeton alınır → reklam gösterilir → reklam ağı **sunucuya** SSV callback yapar → sunucu ödülü atomik uygular. Günlük 5 hakkını sunucu sayar. Tam akış ve kenar durumlar: **BÖLÜM C.6**.

**"2× hasat" tam anlamı:** sadece hasat edilen ürün katlanır · başlangıç ürünlerinin geri verdiği **tohum katlanmaz** · yeniden veren üründe **sadece o hasat** · gübreyle birleşince `(1 + gübre) × 2`.

**ASLA:** ilerlemeyi kilitleyen · süre kısaltma satan · çift arasında dengesizlik yaratan · kozmetik veren reklam

### Bildirim — öncelik sırası
**Günde en fazla 2.** Hangi 2'si olduğu önceliğe göre:

1. Partner mesajı / not tahtası
2. Ortak proje tamamlandı
3. Tren manifestosu bitiyor (son 1 saat **ve** karşılanabilir sipariş varsa)
4. Günlük sandık hazır
5. Hayvan ürünü birikme sınırına yaklaştı

**Asla:** ürün büyüdü (çok sık) · depo doldu (dırdır) · **küslük** · **partner terk**

Aralarında en az 4 saat · **yerel saatle 23:00-09:00 arası hiç yok** · oyuncu o gün girdiyse 4 ve 5 gönderilmez.

### Günlük sandık
Sıfırlama **son alımdan 24 saat sonra** (UTC gece yarısı değil — saat dilimi adaletsizliği ve çift alma açığı doğuruyor). İçerik: 3-5 tohum (açık ürünlerden) + `seviye × 50` altın. **Seri/streak yok** — kozy oyun, kaçırdığın gün cezalandırılmaz. Reklamla 2× hem altını hem tohum adedini katlar.

### Mağaza
- Serbest sohbet → yaş derecelendirmesi yükselir, "kullanıcılar etkileşimde bulunur" beyanı
- Gizlilik etiketlerinde veri toplama beyanı
- Rapor + engelleme mekanizması zorunlu
- Google Play 25 $ tek seferlik · Apple Developer 99 $/yıl · **iOS derlemesi Mac gerektirir, sona bırakma**

---

## B.25. Yapım sırası

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

## B.26. Yasaklar — bunları yaparsan oyun bozulur

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
| 21 | Küslüğe ekonomik diş takmak | Rol olmaktan çıkıp silaha dönüşür |
| 22 | Ödülü istemcinin vermesi (reklam dahil) | Sunucu otoritesi delinir |
| 23 | Paylaşılan duruma çevrimdışı dokunmak | Bağlantı dönünce toplu red yağmuru |
| 24 | Ortak düğüm sağlığını ×1.5 yapmamak | Toplamda 4× cevher patlaması |
| 25 | Kısa ürünleri saatlik getiriyle dengelemek | Oturum kapısı yüzünden 14-80 kat sapıyor |

---

## B.27. Bilerek kapsam dışı bırakılanlar

| Konu | Ne zaman |
|---|---|
| İsim müsaitliği (marka, paket adı, alan adı, mağaza araması) | Yayın kararından önce. **Sıra: mağaza araması → marka → paket adı → domain** |
| Yerelleştirme | Altyapı baştan (string tablosu), içerik cilada |
| 20 defter sayfasının gerçek metni | Perde perde yazılır |

> ⚠️ **Buradaki her sayı başlangıç noktasıdır.** Simülasyonla tutarlı hale getirildi (16 gün, 7.5 sa aktif, kasa hiç eksiye düşmüyor) ama gerçek değerler oyun testinden çıkacak. Özellikle **depo kapasitesi** dikey dilimde ölçülecek 1 numaralı sayıdır.

---

# BÖLÜM C — Sunucu veri modeli ve protokol

## C.1 Temel kavram: dünya revizyonu

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

## C.2. Veri modeli (PostgreSQL)

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

## C.3. WebSocket protokolü

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

## C.4. Çevrimdışı sınırı — "zarif kopuş"

⚠️ Önceki taslakta bu tanımsızdı ve içeride çelişki vardı: bir yerde "eylemler yerel kuyruğa alınır" (yani çevrimdışı oynanır), başka yerde "bağlantı gerektiği baştan söylenir" (yani oynanmaz).

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

## C.5. Çakışma çözümü

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

## C.6. Reklam ödül atomikliği

⚠️ Önceki taslak oyun eylemleri için idempotency zorunlu kılmıştı ama reklam ödülü için hiçbir şey söylemiyordu. Bu bir tutarsızlıktı.

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

## C.7. Bildirim önceliği

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

## C.8. Günlük sandık

| Kural | Karar |
|---|---|
| Sıfırlama | **Son alımdan 24 saat sonra.** UTC gece yarısı değil — saat dilimi adaletsizliği ve gece yarısı çift alma açığı doğuruyor |
| İçerik | 3-5 tohum (açık ürünlerden, rastgele) + `seviye × 50` altın |
| Seri (streak) | **Yok.** Kozy oyun; kaçırdığın gün cezalandırılmaz |
| Süre dolması | Yok, birikmez de — bir tane bekler |
| Reklamla 2× | Hem altını hem tohum adedini katlar, günlük 5 hakkın 1'i |

---

## C.9. Yeniden bağlanma — tam akış

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

## C.10. REST uçları (meta, WebSocket dışı)

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

## C.11. Güvenlik notları

- JWT kısa ömürlü (1 saat) + yenileme jetonu
- **İstemciden gelen hiçbir miktar, fiyat, XP veya süre kabul edilmez.** Sadece niyet: ne, nerede.
- Hız sınırı: eylem 10/sn, sohbet 1/sn, jeton isteği 5/gün
- `HENUZ_OLGUN_DEGIL` reddi tekrarlanıyorsa istemci saati oynanıyor demektir — kayda al, engelleme
- Deterministik fiyat tohumu (`vadi.tohum`) **istemciye gönderilmez**; istemci fiyatları sunucudan alır

---

# BÖLÜM D — Denge verileri
> Bu üç dosya sayısal **doğruluk kaynağıdır.** Bölüm B'deki tablolarla çakışırsa **bu bölüm geçerlidir.**
> Sunucuda yapılandırma olarak yüklenir, koda gömülmez (mimari kural 8). `GET /api/denge` ile sürüm etiketli sunulur.

## D — `urunler.json`

```json
{
 "$aciklama": "Amorland urun denge tablosu. Sunucuda yapilandirma olarak yuklenir, koda gomulmez. Tum degerler baslangic noktasidir; dikey dilim testinden sonra ayarlanacak.",
 "surum": "0.3",
 "tarih": "2026-08-19",
 "saatlikGetiriKademeleri": {
  "$aciklama": "altin / saat / karo. Uzun urun daha karli ama azalan artisla - 12 saatlik urun otomatik en iyi secenek olmamali.",
  "300": 24,
  "1800": 30,
  "7200": 36,
  "21600": 42,
  "43200": 48
 },
 "urunler": [
  {
   "id": "bugday",
   "ad": "Buğday",
   "acilis": {
    "tip": "seviye_odulu",
    "seviye": 1
   },
   "buyumeSn": 300,
   "tabanFiyat": 2,
   "tohumFiyat": 1,
   "verim": 1,
   "xp": 1,
   "not": "Ogretici + un zinciri girdisi. Tohum/hasat orani %50 - tabanFiyat 2 altinken tamsayi ile %30 tutturulamiyor. Kasitli istisna, ekonomik agirligi yok.",
   "tohumGeriVerir": true
  },
  {
   "id": "turp",
   "ad": "Turp",
   "acilis": {
    "tip": "seviye_odulu",
    "seviye": 2
   },
   "buyumeSn": 1800,
   "tabanFiyat": 15,
   "tohumFiyat": 5,
   "verim": 1,
   "xp": 1,
   "tohumGeriVerir": true
  },
  {
   "id": "havuc",
   "ad": "Havuç",
   "acilis": {
    "tip": "seviye_odulu",
    "seviye": 3
   },
   "buyumeSn": 1800,
   "tabanFiyat": 16,
   "tohumFiyat": 5,
   "verim": 1,
   "xp": 1,
   "tohumGeriVerir": true
  },
  {
   "id": "misir",
   "ad": "Mısır",
   "acilis": {
    "tip": "seviye_odulu",
    "seviye": 4
   },
   "buyumeSn": 7200,
   "tabanFiyat": 72,
   "tohumFiyat": 22,
   "verim": 1,
   "xp": 4,
   "not": "Ilk gercek karar: hizli mi karli mi",
   "tohumGeriVerir": true
  },
  {
   "id": "patates",
   "ad": "Patates",
   "acilis": {
    "tip": "seviye_odulu",
    "seviye": 5
   },
   "buyumeSn": 7200,
   "tabanFiyat": 68,
   "tohumFiyat": 20,
   "verim": 1,
   "xp": 4,
   "tohumGeriVerir": true
  },
  {
   "id": "domates",
   "ad": "Domates",
   "acilis": {
    "tip": "market",
    "seviye": 6
   },
   "buyumeSn": 7200,
   "tabanFiyat": 85,
   "tohumFiyat": 24,
   "verim": 1,
   "xp": 4,
   "yenidenVerir": {
    "adet": 4,
    "araSn": 21600
   },
   "tohumGeriVerir": false,
   "not": "v0.6: ara sure 2sa->6sa, adet 3->4, fiyat 75->85. 2 saatlik ara oturum arasinin (6.9sa) altinda kaliyordu, yeniden verme hic ise yaramiyordu."
  },
  {
   "id": "marul",
   "ad": "Marul",
   "acilis": {
    "tip": "market",
    "seviye": 7
   },
   "buyumeSn": 1800,
   "tabanFiyat": 14,
   "tohumFiyat": 4,
   "verim": 1,
   "xp": 1,
   "tohumGeriVerir": false
  },
  {
   "id": "kabak",
   "ad": "Kabak",
   "acilis": {
    "tip": "market",
    "seviye": 8
   },
   "buyumeSn": 21600,
   "tabanFiyat": 252,
   "tohumFiyat": 76,
   "verim": 1,
   "xp": 12,
   "tohumGeriVerir": false
  },
  {
   "id": "cilek",
   "ad": "Çilek",
   "acilis": {
    "tip": "market",
    "seviye": 10
   },
   "buyumeSn": 21600,
   "tabanFiyat": 230,
   "tohumFiyat": 70,
   "verim": 1,
   "xp": 12,
   "yenidenVerir": {
    "adet": 4,
    "araSn": 10800
   },
   "tohumGeriVerir": false
  },
  {
   "id": "aycicegi",
   "ad": "Ayçiçeği",
   "acilis": {
    "tip": "market",
    "seviye": 11
   },
   "buyumeSn": 21600,
   "tabanFiyat": 245,
   "tohumFiyat": 74,
   "verim": 1,
   "xp": 12,
   "tohumGeriVerir": false
  },
  {
   "id": "lahana",
   "ad": "Lahana",
   "acilis": {
    "tip": "market",
    "seviye": 12
   },
   "buyumeSn": 21600,
   "tabanFiyat": 260,
   "tohumFiyat": 78,
   "verim": 1,
   "xp": 12,
   "tohumGeriVerir": false
  },
  {
   "id": "karpuz",
   "ad": "Karpuz",
   "acilis": {
    "tip": "market",
    "seviye": 14
   },
   "buyumeSn": 43200,
   "tabanFiyat": 576,
   "tohumFiyat": 175,
   "verim": 1,
   "xp": 24,
   "tohumGeriVerir": false
  },
  {
   "id": "uzum",
   "ad": "Üzüm",
   "acilis": {
    "tip": "market",
    "seviye": 15
   },
   "buyumeSn": 43200,
   "tabanFiyat": 540,
   "tohumFiyat": 165,
   "verim": 1,
   "xp": 24,
   "yenidenVerir": {
    "adet": 3,
    "araSn": 43200
   },
   "tohumGeriVerir": false,
   "not": "v0.6: ara sure 6sa->12sa. Onceden kademesinin %69 ustundeydi, gec oyunda tek secenek haline geliyordu."
  },
  {
   "id": "balkabagi",
   "ad": "Balkabağı",
   "acilis": {
    "tip": "market",
    "seviye": 16
   },
   "buyumeSn": 43200,
   "tabanFiyat": 600,
   "tohumFiyat": 180,
   "verim": 1,
   "xp": 24,
   "tohumGeriVerir": false
  },
  {
   "id": "kavun",
   "ad": "Kavun",
   "acilis": {
    "tip": "market",
    "seviye": 18
   },
   "buyumeSn": 43200,
   "tabanFiyat": 590,
   "tohumFiyat": 178,
   "verim": 1,
   "xp": 24,
   "tohumGeriVerir": false,
   "tohumPenceresi": true
  },
  {
   "id": "safran",
   "ad": "Safran",
   "acilis": {
    "tip": "market",
    "seviye": 19
   },
   "buyumeSn": 43200,
   "tabanFiyat": 900,
   "tohumFiyat": 420,
   "verim": 1,
   "xp": 24,
   "not": "v0.6: tohum 300->420. Yuksek fiyati korunuyor ama riski de artiyor - kit tohumlu prestij urunu.",
   "tohumGeriVerir": false,
   "tohumPenceresi": true
  }
 ],
 "buyumeAsamasi": {
  "$aciklama": "Her urun 4 gorsel asamada buyur. Asama = floor(gecenSure / buyumeSn * 4), 3'te sinirlanir. Sunucu okuma aninda hesaplar, cron yok.",
  "asamaSayisi": 4
 },
 "gubre": {
  "$aciklama": "Tek tip. Kalite kademesi yok. Sureyi degistirmez - sure degisseydi altin/saat ekonomisi siserdi.",
  "acilisSeviye": 12,
  "etki": {
   "verimEk": 1
  },
  "fiyatOraniTabanFiyata": 0.7,
  "craftlanabilir": false,
  "not": "Oran bilincli: otomatik karli degil. Tren fiyati %100 ustundeyken kullanilir."
 },
 "marketAlisOrani": {
  "$aciklama": "Marketin oyuncudan alis fiyati. Sabit, dalgalanmaz. Acil nakit muslugu.",
  "min": 0.5,
  "max": 0.6
 },
 "trenFiyat": {
  "$aciklama": "Ortalamaya donen rastgele yuruyus. Saf rastgele KULLANILMAZ - mini grafikte okunacak egilim olmali.",
  "bandMin": 0.7,
  "bandMax": 1.3,
  "baslangic": 1.0,
  "ortalamayaDonusKatsayisi": 0.3,
  "gurultuGenligi": 0.24,
  "seferAraligiSn": 21600,
  "epochUtc": "2026-01-01T00:00:00Z",
  "ileriSarmaPenceresi": 50,
  "formul": "p[n] = clamp(0.70, 1.30, p[n-1] + (1.0 - p[n-1]) * 0.30 + (hash01(tohum, urunId, n) - 0.5) * 0.24)"
 },
 "manifesto": {
  "siparisKartiSayisi": 3,
  "carpanMin": 1.6,
  "carpanMax": 2.0,
  "carpanKus": 1.3,
  "tamDoldurmaXpBonus": 100,
  "siparisXp": 60,
  "tekKisilikKontenjanOrani": 0.7,
  "kacirmaCezasi": false
 },
 "trenTohum": {
  "cesitMin": 2,
  "cesitMax": 3,
  "indirimMin": 0.3,
  "indirimMax": 0.4,
  "sinirliAdet": true,
  "not": "Sinirsiz olursa market anlamsizlasir."
 },
 "baslangicUrunKurali": {
  "$aciklama": "v0.4: Baslangic urunleri hasatta 1 urun + 1 tohum verir. Market urunleri vermez.",
  "urunler": [
   "bugday",
   "havuc",
   "misir",
   "patates",
   "turp"
  ],
  "etki": "Perde 1 kendi kendine yetiyor; baslangic urunleri gec oyunda 'bedava ama dusuk getirili' guvenlik agi olarak kaliyor."
 },
 "yenidenVerirKurali": {
  "$aciklama": "v0.6: 'adet' semantigi tanimlandi. Onceden belirsizdi ve simulasyonu gecersiz kiliyordu.",
  "adetAnlami": "TOPLAM hasat sayisi (ilk hasat dahil). adet=3 -> 1 ilk + 2 yeniden.",
  "tohumHarcamasi": "Sadece ekimde bir kez. Yeniden hasatlar tohum harcamaz.",
  "araSnAnlami": "Bir hasattan sonraki hasada kadar gecen sure.",
  "sonHasattanSonra": "Karo bosalir, yeniden ekilmesi gerekir.",
  "gubreEtkisi": "SADECE bir sonraki hasata +1 verim. Tum donguye degil - yoksa gubre+yenidenveren kombinasyonu otomatik en iyi secenek olurdu.",
  "depoDoluysa": "Hasat engellenir, urun karoda bekler, YENIDEN VERME SAYACI ISLEMEZ. Ancak hasat edilince sonraki sayac baslar.",
  "fiyatPrimi": {
   "$aciklama": "v0.6: yeniden veren urunler kademesinin %15-20 USTUNDE fiyatlanir.",
   "gerekce": "Karoyu 3-4 kat daha uzun kilitliyorlar. Prim bu esneklik kaybinin karsiligi.",
   "not": "Bu, kademe ici +-%10 varyasyon kuralinin bilincli istisnasidir."
  }
 },
 "tohumPenceresiKurali": {
  "$aciklama": "v0.6: 'kisa pencere' etiketi hem yabani kaynak hem urun icin kullaniliyordu, urun tarafi hic tanimli degildi.",
  "anlam": "Bu urunlerin TOHUMU markette sadece belirli pencerelerde satilir. Buyume ve hasat normaldir.",
  "gunlukPencereSayisi": 3,
  "pencereSuresiSn": 7200,
  "deterministik": true,
  "hasatPenceresiYok": "Urun tarlada bekleyebilir, cururmez. 'Esya asla yok edilmez' kurali gecerli.",
  "not": "Fiyat primi (safran +%56) tam olarak bu arz kisitindan geliyor - olcekli ekilemiyor."
 },
 "saatlikGetiriVaryasyonu": {
  "$aciklama": "v0.6: Kademe ici +-%10 sapma KASITLIDIR, hata degil.",
  "bant": 0.1,
  "gerekce": "Bir kademedeki 4 urun birebir ayni fiyatta olsaydi aralarinda secim yapmak anlamsiz olurdu.",
  "istisna": "safran +%56 - tohum penceresi arz kisiti karsiligi."
 },
 "oturumKapisi": {
  "$aciklama": "v0.6 KRITIK BULGU: saatlik getiri kademesi gercek oturum modelinde gecerli DEGIL.",
  "oturumAralikSaat": 6.86,
  "bulgu": "Gunde 3.5 oturumda oturum arasi 6.9 saat. Bundan kisa buyuyen urun, hasat edilene kadar karoyu bos bekletiyor. 30 dakikalik urun kademe tablosunda 30 altin/saat diyor ama gerceklesen deger 2.2 - 14 kat fark.",
  "sonuc": "Kisa urunler saatlik getiride yarisamaz. Rolleri farkli: AKTIF OTURUM DOLGUSU. Oyuncu zaten oyundayken 5-30 dakikalik urun ek bir hasat verir; uzun urun ise oyuncu yokken calisir.",
  "duzeltme": "'Uzun urun saatte ~%25 daha karli' ifadesi YANLIS. Gercek fark 10-20 kat. Saatlik getiri kademesi sadece buyume suresi >= oturum arasi olan urunler icin gecerlidir (2sa ve uzeri)."
 }
}
```

## D — `uretim.json`

```json
{
 "$aciklama": "Amorland uretim tablolari: agac, yabani kaynak, maden, alet, craft. Sunucuda yapilandirma olarak yuklenir.",
 "surum": "0.3",
 "tarih": "2026-08-19",
 "agaclar": [
  {
   "id": "cam",
   "ad": "Çam",
   "acilisSeviye": 7,
   "buyumeSn": 28800,
   "vurus": 3,
   "fidanFiyat": 30,
   "getiri": [
    {
     "id": "odun",
     "adet": 4
    }
   ],
   "xp": 10
  },
  {
   "id": "mese",
   "ad": "Meşe",
   "acilisSeviye": 10,
   "buyumeSn": 86400,
   "vurus": 4,
   "fidanFiyat": 120,
   "getiri": [
    {
     "id": "sert_odun",
     "adet": 3
    }
   ],
   "xp": 20
  },
  {
   "id": "ceviz",
   "ad": "Ceviz",
   "acilisSeviye": 13,
   "buyumeSn": 172800,
   "vurus": 5,
   "fidanFiyat": 400,
   "getiri": [
    {
     "id": "degerli_odun",
     "adet": 2
    },
    {
     "id": "ceviz_urun",
     "adet": 3
    }
   ],
   "xp": 40
  },
  {
   "id": "gumus_kavak",
   "ad": "Gümüş kavak",
   "acilisSeviye": 16,
   "buyumeSn": 259200,
   "vurus": 6,
   "fidanFiyat": 1200,
   "getiri": [
    {
     "id": "nadir_odun",
     "adet": 2
    }
   ],
   "xp": 60
  }
 ],
 "kutukKalir": false,
 "yabaniKaynaklar": {
  "$aciklama": "Kisa ritim. Ormanin olu bolge olmasini engeller VE alet kilitlenmesine karsi guvenlik agi - hicbiri alet gerektirmez.",
  "kaynaklar": [
   {
    "id": "dal",
    "ad": "Dal",
    "yenilenmeSn": 1800,
    "getiri": [
     {
      "id": "odun",
      "adetMin": 1,
      "adetMax": 2
     }
    ],
    "bolgeler": [
     "orman",
     "tarla_kenari",
     "maden_girisi"
    ],
    "xp": 1
   },
   {
    "id": "tas_parca",
    "ad": "Tas parcasi",
    "yenilenmeSn": 1800,
    "getiri": [
     {
      "id": "tas",
      "adetMin": 1,
      "adetMax": 2
     }
    ],
    "bolgeler": [
     "orman",
     "maden_girisi"
    ],
    "xp": 1
   },
   {
    "id": "mantar",
    "ad": "Mantar",
    "yenilenmeSn": 5400,
    "getiri": [
     {
      "id": "mantar",
      "adet": 1
     }
    ],
    "bolgeler": [
     "orman"
    ],
    "xp": 1
   },
   {
    "id": "yaban_meyve",
    "ad": "Yaban meyvesi",
    "yenilenmeSn": 7200,
    "getiri": [
     {
      "id": "meyve",
      "adetMin": 1,
      "adetMax": 2
     }
    ],
    "bolgeler": [
     "orman"
    ],
    "xp": 1
   },
   {
    "id": "yem_otu",
    "ad": "Yem otu",
    "yenilenmeSn": 2700,
    "getiri": [
     {
      "id": "yem",
      "adet": 1
     }
    ],
    "bolgeler": [
     "orman",
     "tarla_kenari"
    ],
    "xp": 1
   }
  ]
 },
 "kisaPencereToplama": {
  "$aciklama": "Sv 16 'kisa mevsimli toplama' yerine gecen sistem. Oyun ici takvim gerektirmez.",
  "acilisSeviye": 16,
  "gunlukPencereSayisi": 3,
  "pencereSuresiSn": 7200,
  "deterministik": true,
  "not": "Pencere zamanlari sunucu tohumu + gun numarasindan hesaplanir."
 },
 "maden": {
  "katlar": [
   {
    "kat": 1,
    "ad": "Kat 1",
    "gerekenKazmaKademe": 1,
    "acilisSeviye": 9,
    "vurus": 3,
    "yenilenmeSn": 1200,
    "dugumler": [
     {
      "id": "tas",
      "adetMin": 2,
      "adetMax": 3,
      "agirlik": 60
     },
     {
      "id": "komur",
      "adetMin": 2,
      "adetMax": 3,
      "agirlik": 40
     }
    ]
   },
   {
    "kat": 2,
    "ad": "Kat 2",
    "gerekenKazmaKademe": 2,
    "acilisSeviye": 12,
    "vurus": 4,
    "yenilenmeSn": 1800,
    "dugumler": [
     {
      "id": "komur",
      "adetMin": 2,
      "adetMax": 3,
      "agirlik": 50
     },
     {
      "id": "demir",
      "adetMin": 2,
      "adetMax": 3,
      "agirlik": 50
     }
    ]
   },
   {
    "kat": 3,
    "ad": "Kat 3",
    "gerekenKazmaKademe": 3,
    "acilisSeviye": 15,
    "vurus": 5,
    "yenilenmeSn": 2700,
    "dugumler": [
     {
      "id": "demir",
      "adetMin": 1,
      "adetMax": 2,
      "agirlik": 65
     },
     {
      "id": "altin",
      "adetMin": 1,
      "adetMax": 2,
      "agirlik": 35
     }
    ]
   },
   {
    "kat": 4,
    "ad": "Dip",
    "gerekenKazmaKademe": 4,
    "acilisSeviye": 18,
    "vurus": 6,
    "yenilenmeSn": 3600,
    "dugumler": [
     {
      "id": "altin",
      "adetMin": 1,
      "adetMax": 2,
      "agirlik": 70
     },
     {
      "id": "elmas",
      "adetMin": 1,
      "adetMax": 2,
      "agirlik": 30
     }
    ]
   }
  ],
  "dugumXp": 3,
  "not": "Bakir ve gumus YOK. v0.2 Bolum 9'daki 'Kat 3 bakir gumus' ifadesi Bolum 7b ile celisiyordu; 4 cevher korundu."
 },
 "ortakDugum": {
  "$aciklama": "v0.6 DUZELTME: onceki kural oyuncu basina 2x, toplamda 4x cevher uretiyordu (simulasyonla olculdu).",
  "model": "paylasimli_saglik",
  "getiriBolusulur": false,
  "getiriIcinMinKatkiOrani": 0.25,
  "yenilenmeDegismez": true,
  "not": "Iki oyuncu ayni dugume vurursa dugum sagligi %50 artar. Her ikisi de tam getiriyi alir. Net etki: oyuncu basina 1.33x (2.0x degil). 'Birlikte calismak asla tek basina calismaktan kotu olmamali' kurali korunuyor, ama 4x kaynak patlamasi kapaniyor.",
  "ortakVurustaSaglikCarpani": 1.5
 },
 "aletler": [
  {
   "id": "tas_kazma",
   "tur": "kazma",
   "kademe": 1,
   "dayaniklilik": 40,
   "malzeme": [
    {
     "id": "odun",
     "adet": 5
    },
    {
     "id": "tas",
     "adet": 10
    }
   ],
   "craftSeviye": 1,
   "marketFiyat": 50,
   "herZamanMarkette": true
  },
  {
   "id": "demir_kazma",
   "tur": "kazma",
   "kademe": 2,
   "dayaniklilik": 120,
   "malzeme": [
    {
     "id": "odun",
     "adet": 5
    },
    {
     "id": "demir_kulce",
     "adet": 8
    }
   ],
   "craftSeviye": 10
  },
  {
   "id": "celik_kazma",
   "tur": "kazma",
   "kademe": 3,
   "dayaniklilik": 300,
   "malzeme": [
    {
     "id": "sert_odun",
     "adet": 5
    },
    {
     "id": "celik_kulce",
     "adet": 10
    }
   ],
   "craftSeviye": 12
  },
  {
   "id": "elmas_kazma",
   "tur": "kazma",
   "kademe": 4,
   "dayaniklilik": 800,
   "malzeme": [
    {
     "id": "degerli_odun",
     "adet": 5
    },
    {
     "id": "celik_kulce",
     "adet": 8
    },
    {
     "id": "elmas",
     "adet": 3
    }
   ],
   "craftSeviye": 15
  },
  {
   "id": "tas_balta",
   "tur": "balta",
   "kademe": 1,
   "dayaniklilik": 120,
   "malzeme": [
    {
     "id": "odun",
     "adet": 5
    },
    {
     "id": "tas",
     "adet": 8
    }
   ],
   "craftSeviye": 1,
   "marketFiyat": 45,
   "herZamanMarkette": true
  },
  {
   "id": "demir_balta",
   "tur": "balta",
   "kademe": 2,
   "dayaniklilik": 360,
   "malzeme": [
    {
     "id": "odun",
     "adet": 5
    },
    {
     "id": "demir_kulce",
     "adet": 6
    }
   ],
   "craftSeviye": 10
  },
  {
   "id": "celik_balta",
   "tur": "balta",
   "kademe": 3,
   "dayaniklilik": 900,
   "malzeme": [
    {
     "id": "sert_odun",
     "adet": 5
    },
    {
     "id": "celik_kulce",
     "adet": 8
    }
   ],
   "craftSeviye": 12
  },
  {
   "id": "elmas_balta",
   "tur": "balta",
   "kademe": 4,
   "dayaniklilik": 2400,
   "malzeme": [
    {
     "id": "degerli_odun",
     "adet": 5
    },
    {
     "id": "celik_kulce",
     "adet": 6
    },
    {
     "id": "elmas",
     "adet": 2
    }
   ],
   "craftSeviye": 15
  },
  {
   "id": "ahsap_kova",
   "tur": "kova",
   "kademe": 1,
   "dayaniklilik": null,
   "kapasiteKaro": 6,
   "malzeme": [
    {
     "id": "odun",
     "adet": 8
    }
   ],
   "craftSeviye": 1
  },
  {
   "id": "demir_kova",
   "tur": "kova",
   "kademe": 2,
   "dayaniklilik": null,
   "kapasiteKaro": 12,
   "malzeme": [
    {
     "id": "demir_kulce",
     "adet": 6
    }
   ],
   "craftSeviye": 10
  },
  {
   "id": "celik_kova",
   "tur": "kova",
   "kademe": 3,
   "dayaniklilik": null,
   "kapasiteKaro": 20,
   "malzeme": [
    {
     "id": "celik_kulce",
     "adet": 8
    }
   ],
   "craftSeviye": 12
  },
  {
   "id": "elmas_kova",
   "tur": "kova",
   "kademe": 4,
   "dayaniklilik": null,
   "kapasiteKaro": 40,
   "malzeme": [
    {
     "id": "celik_kulce",
     "adet": 6
    },
    {
     "id": "elmas",
     "adet": 2
    }
   ],
   "craftSeviye": 15
  }
 ],
 "aletKurallari": {
  "dayaniklilikUyariEsigi": 0.2,
  "kirilincaAltKademeyeDus": true,
  "kilitlenmeDegismezi": "Hicbir alet, o aleti yapmak icin gereken kaynagin tek edinme yolu olmamali. Dal ve tas parcasi elle toplanir; tas kazma ve tas balta Sv 1'den craftlanabilir; market ikisini de her zaman ucuz satar."
 },
 "craftTarifleri": {
  "$aciklama": "TAM 25 tarif. v0.2 siniri. Yeni tarif eklemek isteyen bir tarif cikarmak zorunda. Malzeme derinligi en fazla 2 kademe.",
  "sinir": 25,
  "kullanilan": 25,
  "dagilim": {
   "aletler": 12,
   "araMalzeme": 3,
   "isleme": 3,
   "yapi": 1,
   "evEsyasi": 6
  },
  "araMalzeme": [
   {
    "id": "demir_kulce",
    "ad": "Demir kulcesi",
    "girdi": [
     {
      "id": "demir",
      "adet": 2
     },
     {
      "id": "komur",
      "adet": 1
     }
    ],
    "craftSeviye": 10
   },
   {
    "id": "celik_kulce",
    "ad": "Celik kulcesi",
    "girdi": [
     {
      "id": "demir_kulce",
      "adet": 2
     },
     {
      "id": "komur",
      "adet": 2
     }
    ],
    "craftSeviye": 12
   },
   {
    "id": "altin_kulce",
    "ad": "Altin kulcesi",
    "girdi": [
     {
      "id": "altin",
      "adet": 2
     },
     {
      "id": "komur",
      "adet": 1
     }
    ],
    "craftSeviye": 15
   }
  ],
  "isleme": [
   {
    "id": "un",
    "ad": "Un",
    "girdi": [
     {
      "id": "bugday",
      "adet": 3
     }
    ],
    "acilisSeviye": 13,
    "fiyatCarpani": 2.5,
    "yapi": "degirmen"
   },
   {
    "id": "ekmek",
    "ad": "Ekmek",
    "girdi": [
     {
      "id": "un",
      "adet": 2
     },
     {
      "id": "odun",
      "adet": 1
     }
    ],
    "acilisSeviye": 13,
    "fiyatCarpani": 3.0,
    "yapi": "degirmen"
   },
   {
    "id": "peynir",
    "ad": "Peynir",
    "girdi": [
     {
      "id": "sut",
      "adet": 2
     }
    ],
    "acilisSeviye": 14,
    "fiyatCarpani": 2.8,
    "yapi": "ahir"
   }
  ],
  "yapi": [
   {
    "id": "yagmurlama",
    "ad": "Yagmurlama",
    "girdi": [
     {
      "id": "celik_kulce",
      "adet": 4
     },
     {
      "id": "elmas",
      "adet": 1
     }
    ],
    "acilisSeviye": 14,
    "etki": "4 komsu karoyu her buyume cevriminde otomatik sular"
   }
  ],
  "evEsyasi": {
   "adet": 6,
   "girdiTemeli": [
    "altin_kulce",
    "degerli_odun",
    "nadir_odun",
    "elmas"
   ],
   "islevselFayda": false,
   "not": "Gec oyunun ana altin/elmas kuyusu. Kozmetik + defter kaydi."
  },
  "craftXp": {
   "ilkKez": 30,
   "tekrar": 5
  }
 },
 "vadiAniti": {
  "$aciklama": "Acik madde #14. Gec oyun prestij cikisi. Tek model + malzeme/olcek degisimi.",
  "acilisSeviye": 17,
  "islevselFayda": false,
  "kademeler": [
   {
    "kademe": 1,
    "altin": 10000,
    "elmas": 0
   },
   {
    "kademe": 2,
    "altin": 30000,
    "elmas": 5
   },
   {
    "kademe": 3,
    "altin": 80000,
    "elmas": 15
   },
   {
    "kademe": 4,
    "altin": 200000,
    "elmas": 40
   },
   {
    "kademe": 5,
    "altin": 500000,
    "elmas": 100
   }
  ]
 },
 "hayvanlar": {
  "$aciklama": "v0.6: onceden 2 tablo satiri + 1 cumleydi. Tam dongu tanimlandi.",
  "turler": [
   {
    "id": "tavuk",
    "ad": "Tavuk",
    "yapi": "kumes",
    "acilisSeviye": 8,
    "satinAlmaFiyat": 500,
    "urun": "yumurta",
    "urunTabanFiyat": 40,
    "periyotSn": 86400,
    "yemPeriyotBasina": 1,
    "xp": 3
   },
   {
    "id": "inek",
    "ad": "İnek",
    "yapi": "ahir",
    "acilisSeviye": 14,
    "satinAlmaFiyat": 4000,
    "urun": "sut",
    "urunTabanFiyat": 180,
    "periyotSn": 86400,
    "yemPeriyotBasina": 2,
    "xp": 8
   }
  ],
  "kapasite": [
   {
    "yapi": "kumes",
    "acilisSeviye": 8,
    "adet": 4
   },
   {
    "yapi": "kumes",
    "acilisSeviye": 12,
    "adet": 8,
    "yukseltmeMaliyet": 6000
   },
   {
    "yapi": "ahir",
    "acilisSeviye": 14,
    "adet": 2
   },
   {
    "yapi": "ahir",
    "acilisSeviye": 17,
    "adet": 4,
    "yukseltmeMaliyet": 40000
   }
  ],
  "yem": {
   "kaynaklar": [
    "orman: yem otu (45 dk yenilenme)",
    "market: 12 altin/adet"
   ],
   "craftlanmaz": true,
   "not": "Craft siniri 25 dolu. Yem toplanir veya satin alinir."
  },
  "besleme": {
   "yontem": "TEK DOKUNUS ile yapidaki tum hayvanlar beslenir",
   "gerekce": "Baglam duyarli etkilesim felsefesi. 8 tavugu tek tek beslemek mobilde iskence.",
   "yemYoksa": {
    "uretir": false,
    "olur": false,
    "saglikDuser": false
   }
  },
  "toplama": {
   "urunNerede": "Hayvanin yaninda belirir, dokunarak toplanir, CANTAYA girer",
   "birikmeLimiti": 3,
   "birikmeNotu": "En fazla 3 periyot (3 gun) birikir, sonra uretim durur. Sinirsiz bekleme yok.",
   "otomatikDepoyaGitmez": true
  },
  "yok": [
   "mutluluk",
   "hastalik",
   "olum",
   "ureme"
  ],
  "yokGerekce": "Kozy oyun. Hayvan olmez, hastalanmaz. Kapsam disi."
 }
}
```

## D — `ilerleme.json`

```json
{
 "$aciklama": "Amorland ilerleme tablolari: XP, seviye, canta, depo, bolge acilislari, ortak projeler, eylem sureleri, reklam.",
 "surum": "0.3",
 "tarih": "2026-08-19",
 "seviye": {
  "formul": "Sv n -> n+1 gereken XP = round(100 * 1.2^(n-1))",
  "maks": 20,
  "toplamXp": 15476,
  "esikler": {
   "1": 100,
   "2": 120,
   "3": 144,
   "4": 173,
   "5": 207,
   "6": 249,
   "7": 299,
   "8": 358,
   "9": 430,
   "10": 516,
   "11": 619,
   "12": 743,
   "13": 892,
   "14": 1070,
   "15": 1284,
   "16": 1541,
   "17": 1849,
   "18": 2219,
   "19": 2663
  }
 },
 "xpKaynaklari": {
  "$aciklama": "Tren XP'nin baskin kaynagi olmali - v0.2'nin 'tren ekonominin kalbi' tasarimini XP tarafinda da dogrular.",
  "hasat": {
   "formul": "max(1, round(buyumeSaati * 2))"
  },
  "trenSiparisi": 60,
  "manifestoTamDoldurma": 100,
  "cevherDugumu": 3,
  "agacKesme": {
   "cam": 10,
   "mese": 20,
   "ceviz": 40,
   "gumus_kavak": 60
  },
  "yabaniToplama": 1,
  "craftIlkKez": 30,
  "craftTekrar": 5,
  "defterSayfasi": 75,
  "ortakProjeTamamlama": 300,
  "bolgeIlkAcma": 50,
  "ilkKezAnlari": {
   "xp": 25,
   "anlar": [
    "ilk_ekim",
    "ilk_sulama",
    "ilk_hasat",
    "ilk_depoya_bosaltma",
    "ilk_craft",
    "ilk_partner_mesaji"
   ]
  },
  "yetismeBonusu": {
   "$aciklama": "v0.4: geriden gelen partner yetisebilsin.",
   "seviyeFarkiEsigi": 3,
   "carpan": 1.5,
   "kapanmaFarki": 2,
   "maksSureGun": 14,
   "not": "v0.6: bonus en fazla 14 gun surer, sonra fark kapanmasa bile duser. Bilerek geri kalip biriktirmeyi engeller."
  }
 },
 "canta": {
  "$aciklama": "KISIT TURU: esya CESIDI (slot sayisi). Yigin 99. Madende isirir - tas/komur/demir/altin/elmas cesit patlamasi. Hareketi zorlar.",
  "kisitTuru": "cesit",
  "yiginBoyutu": 99,
  "kademeler": [
   {
    "slot": 8,
    "acilisSeviye": 1,
    "maliyet": 0
   },
   {
    "slot": 12,
    "acilisSeviye": 4,
    "maliyet": 0,
    "not": "v0.4: bedava, seviye odulu. Sv4'te para kazanma yolu yok."
   },
   {
    "slot": 16,
    "acilisSeviye": 8,
    "maliyet": 3500
   },
   {
    "slot": 20,
    "acilisSeviye": 14,
    "maliyet": 28000
   }
  ],
  "aletCubuguSlot": 4,
  "aletCubuguIcerik": "tohum_ve_sarf",
  "not": "Aletler envanterde durur ve baglama gore OTOMATIK secilir. Alet cubugu tohum/sarf secicidir."
 },
 "depo": {
  "$aciklama": "KISIT TURU: toplam MIKTAR (birim). Karari zorlar. Bu tablo dikey dilimde olculecek 1 numarali sayi.",
  "kisitTuru": "miktar",
  "kademeler": [
   {
    "kapasite": 100,
    "acilisSeviye": 2,
    "maliyet": 0,
    "seferKarsiligi": 2.5
   },
   {
    "kapasite": 160,
    "acilisSeviye": 6,
    "maliyet": 1200,
    "seferKarsiligi": 2.2
   },
   {
    "kapasite": 240,
    "acilisSeviye": 11,
    "maliyet": 9000,
    "seferKarsiligi": 2.0
   },
   {
    "kapasite": 340,
    "acilisSeviye": 15,
    "maliyet": 45000,
    "seferKarsiligi": 1.8
   },
   {
    "kapasite": 460,
    "acilisSeviye": 18,
    "maliyet": 100000,
    "seferKarsiligi": 1.7,
    "not": "v0.6: 120.000 -> 100.000. Urun dengesi degisince solo kasa Sv18'de eksiye dusuyordu."
   }
  ],
  "buyumeTipi": "dogrusal",
  "maliyetTipi": "ustel",
  "doluKurali": {
   "hasatEngellenir": true,
   "urunTarladaBekler": true,
   "esyaYokEdilir": false,
   "esyaYereDuser": false,
   "otomatikSatilir": false
  },
  "evGenisletmeKapasiteyeEtkiEder": false,
  "not": "Sefer karsiligi 2.5 -> 1.7'ye dusuyor: baski oyun ilerledikce artiyor. Kasitli."
 },
 "tarla": {
  "karoKademeleri": [
   {
    "karo": 4,
    "seviye": 1
   },
   {
    "karo": 8,
    "seviye": 2
   },
   {
    "karo": 12,
    "seviye": 6
   },
   {
    "karo": 16,
    "seviye": 10
   },
   {
    "karo": 20,
    "seviye": 14
   },
   {
    "karo": 24,
    "seviye": 18
   }
  ],
  "parselSayisi": 2,
  "not": "Her oyuncunun kendi parseli. Kimse digerinin parseline dokunamaz. Tek kisilikte iki parsel de oyuncuda."
 },
 "bolgeler": [
  {
   "id": "ev",
   "ad": "Ev",
   "acilisSeviye": 1
  },
  {
   "id": "tarla",
   "ad": "Tarla",
   "acilisSeviye": 1
  },
  {
   "id": "depo",
   "ad": "Depo",
   "acilisSeviye": 2
  },
  {
   "id": "gar",
   "ad": "Tren gari",
   "acilisSeviye": 5
  },
  {
   "id": "market",
   "ad": "Market",
   "acilisSeviye": 6
  },
  {
   "id": "orman",
   "ad": "Orman",
   "acilisSeviye": 7
  },
  {
   "id": "maden",
   "ad": "Maden ocagi",
   "acilisSeviye": 9
  }
 ],
 "evYapilari": [
  {
   "id": "defter",
   "acilisSeviye": 1
  },
  {
   "id": "basit_tezgah",
   "acilisSeviye": 1,
   "tarifler": [
    "tas_kazma",
    "tas_balta",
    "ahsap_kova"
   ],
   "not": "v0.2'de Sv 10'daydi. Sv 9'da maden aciliyordu ve craftlanacak tezgah yoktu - kilitlenme cozumu calismiyordu."
  },
  {
   "id": "kumes",
   "acilisSeviye": 8
  },
  {
   "id": "tezgah_yukseltme",
   "acilisSeviye": 10,
   "acar": "metal_hatti"
  },
  {
   "id": "degirmen",
   "acilisSeviye": 13
  },
  {
   "id": "ahir",
   "acilisSeviye": 14
  },
  {
   "id": "ev_genisletme",
   "acilisSeviye": 17,
   "islevselFayda": false
  }
 ],
 "ortakProjeler": [
  {
   "id": "maden_asansoru",
   "ad": "Maden asansoru",
   "seviye": 11,
   "acar": "maden_kat_2",
   "malzeme": [
    {
     "id": "odun",
     "adet": 200
    },
    {
     "id": "demir_kulce",
     "adet": 120
    },
    {
     "id": "tas",
     "adet": 80
    },
    {
     "id": "altin_para",
     "adet": 5000
    }
   ]
  },
  {
   "id": "kopru",
   "ad": "Kopru",
   "seviye": 15,
   "acar": "maden_kat_3",
   "malzeme": [
    {
     "id": "sert_odun",
     "adet": 300
    },
    {
     "id": "celik_kulce",
     "adet": 180
    },
    {
     "id": "tas",
     "adet": 250
    },
    {
     "id": "altin_para",
     "adet": 25000
    }
   ]
  }
 ],
 "ortakProjeKurallari": {
  "katkiGeriAlinabilir": false,
  "minKatkiOraniOyuncuBasi": 0.25,
  "sureKilidi": false,
  "tekKisilikMaliyetOrani": 0.7,
  "tekKisilikMinKatkiKuraliUygulanir": false,
  "not": "%25 kurali dogrudan hikayeden: 'Tren manifestosu iki imza ister.' Mekanik = hikaye."
 },
 "tekKisilikOlcekleme": {
  "$aciklama": "v0.2 karari: %70. Olceklenen ve olceklenmeyenler kesin.",
  "oran": 0.7,
  "olceklenir": [
   "tren_manifesto_kontenjani",
   "ortak_proje_malzeme_maliyeti",
   "maden_asansoru_katki_esigi"
  ],
  "olceklenmez": [
   "urun_buyume_suresi",
   "fiyatlar",
   "xp",
   "dugum_sagligi",
   "alet_dayanikliligi"
  ]
 },
 "eylemSureleri": {
  "$aciklama": "Saniye. Istemci tarafi animasyon suresi. Sunucu bunlari umursamaz, sadece eylemin gecerli oldugunu dogrular.",
  "yurumeKaroSn": 0.25,
  "ekme": 0.4,
  "sulama": 0.4,
  "hasat": 0.6,
  "yabaniToplama": 0.5,
  "vurusBasina": 0.7,
  "kovaDoldurma": 1.0,
  "animasyonKilidiIptalEdilebilir": true
 },
 "kamera": {
  "izdusum": "ortografik",
  "pitchDerece": 50,
  "yawDerece": 0,
  "yawKilitli": true,
  "oluBolgeKaro": {
   "x": 3,
   "y": 2
  },
  "yumusatmaSn": 0.15,
  "zoomKademeleri": [
   {
    "ad": "yakin",
    "ortoBoyut": 5
   },
   {
    "ad": "uzak",
    "ortoBoyut": 8
   }
  ],
  "karoBirimi": 1.0,
  "karakterBoyBirim": 1.6
 },
 "ogretici": {
  "sureSn": 240,
  "atlanabilir": false,
  "modalDiyalog": false,
  "yontem": "parlayan_hedef_ve_tek_satir",
  "kilitTasi": {
   "dakika": 10,
   "olay": "partner_daveti",
   "not": "Ilk 15 dakikanin isi Sv 5'e ulasmak degil, oyuncuyu partner davetine getirmek."
  }
 },
 "reklam": {
  "tur": "sadece_odullu",
  "gecisReklami": false,
  "banner": false,
  "gunlukMaksimum": 5,
  "ilkDakikaMuafiyeti": 30,
  "yerlesimler": [
   {
    "id": "hasat_2x",
    "odul": "Bu hasadi 2x yap"
   },
   {
    "id": "fener_tazele",
    "odul": "Fener dayanikliligini tazele"
   },
   {
    "id": "gunluk_sandik_2x",
    "odul": "Gunluk sandigi iki katina cikar"
   }
  ],
  "yasak": [
   "ilerleme_kilidi",
   "sure_kisaltma",
   "cift_arasi_dengesizlik",
   "kozmetik"
  ]
 },
 "bildirim": {
  "gunlukMaksimum": 2,
  "kuslukBildirimi": false,
  "not": "Kuslugun bildirim gondermemesi v0.2 Bolum 5'in kirmizi cizgisi. Oyun cifte baski kurmamali."
 },
 "cevrimdisiMesaj": {
  "model": "teslimat_kuyrugu",
  "teslimEdilinceSilinir": true,
  "maksBeklemeSn": 604800,
  "arsivTutulmaz": true
 },
 "kusluk": {
  "tetikleme": "sadece_manuel",
  "hareketsizlikTetikler": false,
  "otomatikDusmeSn": 172800,
  "bildirimGonderir": false,
  "ekonomikEtki": false,
  "not": "v0.6: manifesto carpani dusurme KALDIRILDI. Bir oyuncunun partnerini ekonomik olarak cezalandirabildigi bir dugmeydi. Kusluk artik sadece gosterge + barisma rituali."
 },
 "ayrilik": {
  "tekTarafliBeklemeSn": 172800,
  "ciftTarafliBekleme": false,
  "seviyeKorunur": true,
  "cantaKorunur": true,
  "evVeDepoVadiyiKuranda": true,
  "yeniEslesmeBeklemeSn": 604800,
  "defterIlerlemesiIkisindeKalir": true
 },
 "paraninGirisi": {
  "$aciklama": "v0.4: Sv1-4'te altin YOKTUR, HUD'da gostergesi bile yok.",
  "ilkSeviye": 5,
  "tetikleyici": "tren_gari"
 },
 "seviyeKavramlari": {
  "$aciklama": "v0.4: partner seviye farki cozumu.",
  "dunyaSeviyesi": {
   "belirler": "bolge_ve_kat_erisimi",
   "hesap": "iki_oyuncunun_yuksegi"
  },
  "oyuncuSeviyesi": {
   "belirler": "tarla_parseli_canta_tohum",
   "hesap": "kisisel_xp"
  }
 },
 "hesapKurtarma": {
  "baslangic": "misafir_cihaz_id",
  "baglamaTeklifSeviyeleri": [
   5,
   8,
   12
  ],
  "maksTeklif": 3,
  "partnerDavetindeZorunlu": true,
  "kurtarmaKoduHane": 12,
  "saglayicilar": [
   "eposta",
   "google",
   "apple"
  ]
 },
 "partnerTerk": {
  "tekImzaModuGun": 7,
  "sessizKapiGun": 30,
  "donusteAnindaNormal": true,
  "bildirimGonderir": false
 },
 "yasKapisi": {
  "esik": 13,
  "altindaKapali": [
   "sohbet",
   "partner_daveti"
  ],
  "altindaOynanabilir": true
 },
 "karakterAyirtEdilebilirlik": {
  "hazirRenkSayisi": 4,
  "partnerIsimEtiketi": "her_zaman_gorunur"
 },
 "siparisKismiYatirma": false,
 "hayvanYemsiz": {
  "uretir": false,
  "olur": false
 },
 "gunlukSandik": {
  "icerik": [
   "1 tohum paketi (acik urunlerden)",
   "seviyeye olcekli az altin"
  ],
  "nadirMadde": false
 }
}
```

---

# Kapanış

Bölüm A çalışma kurallarını, B oyunu, C sunucuyu, D sayıları tanımlar.

**Başlangıç noktası her zaman A.4'teki dikey dilimdir.** Ev + Tarla, tek ürün, iki oyuncu bağlı, durum kalıcı.

Bir şeyi değiştirdiğinde etkisini ölç (A.6). Bittiğini söylemeden önce çalıştır.
