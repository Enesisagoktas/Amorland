# Amorland — Tasarım Eki v0.5: Ses

> Ses tasarımı v0.2'de "yapım sırası adım 7"ye ertelenmişti, hiç açılmamıştı.
> Geçerlilik sırası: **v0.5 > v0.4 > v0.3 > v0.2**
>
> Tarih: 2026-08-19 · Bütçe: **0 TL** · Ses tasarımcısı: **yok**

---

## 1. Bu oyuna özel iki gerçek

Plan yazmadan önce bunlar bilinmeli, çünkü ikisi de sıradan ses planını geçersiz kılıyor.

### ⚠️ Gerçek 1 — Oyuncularının çoğu sesi kapalı oynayacak

Mobil kozy oyunlar **podcast/müzik dinlerken** oynanır. Günde 3-4 kez, 5-10 dakika, çoğu zaman toplu taşımada veya yatakta.

İki sonucu var:

1. **Hiçbir mekanik sese bağlı olamaz.** Alet kırılma uyarısı, depo dolu uyarısı, tren gelişi — hepsinin görsel karşılığı olmak zorunda. Ses sadece **ikinci kanal**.
2. **Oyuncunun sesini çalma.** iOS'ta `AVAudioSessionCategoryAmbient` kullanılacak; oyun açılınca kullanıcının çalan podcast'i durmayacak. Android'de `AudioManager` odak isteği `AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK` olacak.

> Birinin podcast'ini kesen kozy oyun kaldırılır. Bu bir tercih değil, gereklilik.

### ⚠️ Gerçek 2 — Sürükle-boyama sesi katleder

v0.3'te "basılı tut + sürükle = 24 karoyu kuyruğa al" kararı verildi. Bu şu demek: **aynı sulama sesi arka arkaya 24 kez çalacak.**

Hiçbir önlem alınmazsa sonuç makineli tüfek sesi olur ve oyuncu ilk gün sesi kapatır.

Üç önlem, üçü de zorunlu:

| Önlem | Değer |
|---|---|
| **Perde varyasyonu** | Her tekrarda ±%8 rastgele pitch |
| **Örnek varyasyonu** | Sık tekrar eden her eylemin **en az 3 farklı kaydı** |
| **Eşzamanlılık sınırı** | Aynı ses etiketinden aynı anda en fazla **3** kanal; 4. istek en eskiyi keser |

Ayrıca kuyruk eylemlerinde ses **hafif alçalarak** çalar (1. eylem %100, sonrakiler %75) — ritim duyulur, gürültü olmaz.

---

## 2. Neyi önce yapmalı — yatırım sırası

Ses bütçesi (zaman) sınırlı. Sırayı içgüdüye bırakma; kozy mobil oyunda getiri sırası şu:

| Sıra | Katman | Neden |
|---|---|---|
| **1** | **Eylem sesleri** | Oyunun dokusu. Bir oturumda yüzlerce kez duyulur. Sesi açık oyuncunun oyunu bunlarla hisseder. |
| **2** | **Ödül anları** (seviye, tren gelişi, ortak proje) | Az sayıda, yüksek etkili. Ucuz ve unutulmaz. |
| **3** | **Ortam döngüleri** | Kozy tonu bunlar kurar. 7 tane, her biri kısa. |
| **4** | **Haptik** | Sessizken **tek çalışan** geri bildirim kanalı. Bkz. bölüm 6. |
| **5** | UI sesleri | Fark edilmez. Sonra. |
| **6** | **Müzik** | **En pahalı, en az fark edilen.** Çoğu oyuncu zaten kendi müziğini dinliyor. En sona. |

> ⚠️ Ters sırada gidip önce müzikle uğraşmak, tek kişilik ses bütçesini bitiren klasik hatadır.

---

## 3. Ses envanteri — 52 benzersiz ses

### Eylem (14) — 1. öncelik, her biri **×3 varyasyon**
| # | Ses | Not |
|---|---|---|
| 1 | Ekme | Toprak, kısa, yumuşak |
| 2 | Sulama | Su dökme — en çok duyulan ses, en iyi kayıt buraya |
| 3 | Hasat (bitki) | Kopma, hışırtı |
| 4 | Hasat (kök) | Havuç/patates — toprakla, farklı |
| 5 | Kazma vuruşu (taş) | |
| 6 | Kazma vuruşu (metal) | |
| 7 | Düğüm kırılma | Vuruş dizisinin ödülü, belirgin olmalı |
| 8 | Balta vuruşu | |
| 9 | Ağaç devrilme | |
| 10 | Yabani toplama | |
| 11 | Kova doldurma | Kuyu, su akışı |
| 12 | Craft başlangıç | |
| 13 | Craft bitiş | |
| 14 | **Alet kırılma** | Kaybı hissettirmeli ama cezalandırıcı olmamalı. Kozy sınırı burada. |

### Ayak sesi (4 yüzey) — her biri **×5 varyasyon**
Toprak · çim · tahta (ev) · taş (maden)

### Ödül ve olay (8) — 2. öncelik
| # | Ses | Süre |
|---|---|---|
| 19 | Seviye atlama | ~2 sn jingle |
| 20 | **Tren gelişi** | ~4 sn, oyunun en büyük olayı |
| 21 | Tren kalkışı | |
| 22 | Manifesto tamamlandı | |
| 23 | Defter sayfası | Kâğıt, hikâye anı |
| 24 | Ortak proje katkı | Her yatırmada, ilerleme hissi |
| 25 | **Ortak proje tamamlandı** | Oyunun sosyal doruk noktası, en büyük ses |
| 26 | Günlük sandık | |

### Ortam döngüsü (7) — 3. öncelik, bölge başına 30-45 sn döngü
Ev · Tarla · Depo · Gar · Market · Orman · Maden

Maden dışında hepsi sakin. Maden alçak, basınçlı, hafif tedirgin — tek ton kırılması orada.

### Hayvan (3)
Tavuk · inek · yumurta toplama

### UI (12) — 5. öncelik
Buton · sekme · envanter seç · envanter yerleştir · hata · onay · satın al · sat · ekran aç · ekran kapat · partner mesajı · partner çevrimiçi

### Müzik (4) — 6. öncelik, en son
| Parça | Kullanım |
|---|---|
| Ana tema | Menü + vadi haritası |
| Genel döngü | Oyun içi, çok sakin, öne çıkmayan |
| Maden döngüsü | Farklı ton |
| Final | Sv 20 |

**Toplam: 52 benzersiz ses · varyasyonlarla ~92 dosya**

---

## 4. Kaynak — 0 TL, ses tasarımcısı yok

Üç katman, asset stratejisinin birebir aynısı:

### Katman 1 — Kendi kaydın (en iyi kaynak, en ucuz)

⚠️ **Çiftlik oyununda en iyi 0 TL ses kaynağı senin telefonun.** Foley tam olarak budur ve bir öğleden sonra sürer.

| Ses | Nasıl kaydedilir |
|---|---|
| Sulama | Kovadan/şişeden leğene su dök |
| Ekme, toprak | Saksı toprağını avuçla, sık |
| Hasat | Marul/maydanoz kopar, kâğıt buruştur |
| Balta | Tahtaya tahta vur |
| Kazma | Taşa taş vur, çakıl karıştır |
| Ayak sesi | Toprakta/tahtada/çakılda yürü |
| Kova doldurma | Musluk + kova |
| Defter sayfası | Kalın kâğıt çevir |

Kayıt kuralları: sessiz oda, telefon 20-30 cm mesafede, her sesin **5-6 tekrarı** (varyasyon buradan çıkacak), sonra Audacity'de (ücretsiz) kırp + gürültü azalt + normalize.

Bu yolla eylem seslerinin (1. öncelik) **tamamı** çıkar ve kütüphane sesinden daha iyi olur — çünkü kendi oyununa özel.

### Katman 2 — CC0 kütüphaneler
| Kaynak | Ne için | Lisans |
|---|---|---|
| **Kenney Audio** | UI, impact, jingle | CC0, oyuna hazır |
| **Freesound** (CC0 filtresi) | Ortam, hayvan, tren | CC0 — **filtreyi mutlaka CC0'a al**, varsayılan değil |
| **Sonniss GDC bundle** | Ortam, foley | Telifsiz, yıllık ücretsiz |
| **Pixabay Audio** | Genel | Telifsiz |
| **OpenGameArt** | Jingle, müzik | Karışık — lisansı tek tek kontrol et |

⚠️ **Lisans kaydı tut.** Her dosya için kaynak + lisans + indirme tarihi bir CSV'de dursun. Mağaza sorarsa veya bir sesin lisansı değişirse tek tek aramak zorunda kalmazsın.

### Katman 3 — Üretilen (sadece UI ve yer tutucu)
UI sesleri (blip, onay, hata) Python ile sentezlenebilir — yumuşak marimba/sine tonlar, kozy tona uyar. Organik sesler (su, toprak, ahşap) sentezle **iyi çıkmaz**, oraya girme.

Ayrıca: **yer tutucu ses seti.** Dikey dilimde her ses kancasının bağlı olduğunu doğrulamak için 52 sesin hepsinin sentetik bir taslağı üretilebilir. Gri kutu asset'in ses karşılığı.

---

## 5. Stil sözleşmesi

Sanat yönünde olduğu gibi ses de tek elden çıkmış gibi durmalı:

| Kural | Değer |
|---|---|
| Format | **OGG Vorbis** (Unity), kaynak dosyalar 48 kHz WAV |
| Ses yüksekliği | Tüm SFX **-16 LUFS**'a normalize, tepe -1 dBFS |
| Eylem sesi uzunluğu | **300 ms altı** — sık tekrar eden ses uzun olamaz |
| Ödül sesi | 1-4 sn |
| Ortam döngüsü | 30-45 sn, kusursuz döngü (sıfır geçişli kesim) |
| Ton | Yumuşak, alçak frekanslı, tiz yok. **Kozy = keskin transient yok.** |
| Reverb | Kaynakta yok. Bölgeye göre Unity'de eklenir. |

---

## 6. Haptik — sessizken çalışan tek kanal

⚠️ Oyuncuların çoğu sesi kapalı oynayacaksa, **dokunsal geri bildirim sesten daha önemli.** Ve bedava.

| Olay | Titreşim |
|---|---|
| Hasat | Çok hafif, tek darbe |
| Düğüm kırılma | Orta, tek darbe |
| **Alet kırılma** | Çift darbe — sesi kapalı oyuncu bunu kaçırmamalı |
| Seviye atlama | Yükselen üçlü desen |
| Ortak proje tamamlandı | Uzun, belirgin |
| Hata / geçersiz eylem | Çok kısa, sert |

- Ayarlarda **kapatılabilir** olmalı (herkes titreşim sevmez, ayrıca pil)
- Kuyruk eylemlerinde (sürükle-boyama) **sadece ilk ve son** titreşir — 24 titreşim işkence
- iOS `UIFeedbackGenerator`, Android `VibrationEffect` (genlik destekli). `Handheld.Vibrate()` çok kaba, tek başına yetmez.

---

## 7. Unity teknik kuralları

| Konu | Karar |
|---|---|
| **AudioMixer grupları** | Master → Müzik / Ortam / Eylem / UI. Her biri ayarlarda ayrı kaydırıcı. |
| Kısa SFX | `Decompress on Load`, ADPCM veya PCM |
| Ortam ve müzik | `Streaming`, Vorbis, kalite %70 |
| **AudioSource havuzu** | Ses başına `Instantiate` YOK. 16 kaynaklı havuz, en eskisini geri dönüştür. |
| Eşzamanlılık | Etiket başına en fazla 3 (bkz. bölüm 1) |
| Perde varyasyonu | Tekrar eden her seste ±%8 |
| **Ses odağı** | iOS `Ambient`, Android `MAY_DUCK`. Oyuncunun müziğini durdurma. |
| Sessiz anahtarı | iOS'ta sessize alınmışsa oyun sesi çalmaz |
| Arka plan | Uygulama arka plana atılınca tüm ses durur |
| Boyut bütçesi | Ses toplamı **~20 MB** (150 MB indirme hedefinin içinde) |
| Bölge yüklemesi | Ortam ve müzik Addressables ile bölgeyle birlikte |

---

## 8. Yapım sırası (v0.2 adım 7'nin yerine)

Ses artık tek bir "cila adımı" değil, üç noktaya yayılıyor:

| Ne zaman | Ne yapılır |
|---|---|
| **Dikey dilim** | Yer tutucu ses seti + AudioMixer + havuz + varyasyon sistemi. **Sesin kendisi değil, altyapısı.** |
| **Perde 1** | Eylem sesleri (kendi kaydın) + haptik + tarla/ev ortamı. Vitrin sessiz olamaz. |
| **Perde 2-3** | Kalan bölge ortamları, ödül sesleri, hayvan |
| **Cila** | UI sesleri, müzik, karışım dengesi |

> ⚠️ Varyasyon ve eşzamanlılık sistemi **dikey dilimde** kurulmalı. Sonradan eklenirse 92 sesin hepsi yeniden elden geçer.
