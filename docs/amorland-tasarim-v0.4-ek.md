# Amorland — Tasarım Eki v0.4

> v0.3'ün simülasyonla bulunan ekonomi hatalarını ve kalan kural boşluklarını kapatır.
> Çakışma olursa geçerlilik sırası: **v0.4 > v0.3 > v0.2**
>
> Tarih: 2026-08-19

---

## 1. Perde 1 ekonomisi ✅ (v0.3'ün en ciddi hatası)

Simülasyon: tren Sv 5, market Sv 6 → **Sv 1-4 arası satış kanalı yok.** Buna rağmen Sv 4 çanta yükseltmesi 400 altın istiyordu ve tohum kaynağı tanımsızdı.

### Karar: Perde 1'de para yok

| Kural | Karar |
|---|---|
| Altın | **Sv 1-4'te oyunda yoktur.** HUD'da altın göstergesi bile yok; Sv 5'te tren gelince belirir. |
| Sv 4 çanta yükseltmesi | **Bedava, seviye ödülü.** Sonraki yükseltmeler (Sv 8, 14) parayla. |
| Tohum | **Başlangıç ürünleri hasatta tohumunu geri verir.** |

**Başlangıç ürünleri kuralı (kalıcı):** Buğday, turp, havuç, mısır, patates → hasat 1 ürün **+ 1 tohum** verir. Market ürünleri (Sv 6+) vermez.

Bu tek kural üç işi birden yapıyor:
1. Perde 1 kendi kendine yetiyor, tohum sorunu bitiyor
2. Başlangıç ürünleri geç oyunda da anlamlı kalıyor: **bedava ama düşük getirili** güvenlik ağı
3. Market ürünleri gerçek bir yatırım kararına dönüşüyor — tohum parası batabilir

**Hikâye kazancı:** Para oyuna tam olarak trenin geldiği anda giriyor. "Tren yıllar sonra ilk kez durur" perde kapanışı artık mekanik olarak da doğru.

---

## 2. Perde 1 tempo motoru ✅

Simülasyon: Perde 1'in 537 XP'sinin **%96'sı tek seferlik ödüllerden** geliyordu — kazara. Çiftlikten gelen hız günde 14-28 XP, tek başına 19 gün ederdi.

**Karar: bu kasıtlı hale getiriliyor ve yazıya geçiyor.**

> Perde 1'in temposu çiftçilikle değil **keşifle** yürür. Tek seferlik ödüller Perde 1'in motorudur, süsü değil. Bunları kısan biri Perde 1'i sessizce günlerce süren bir angaryaya çevirir.

### Tek seferlik XP ödülleri (yeni tablo)

| Ödül | XP | Perde 1'de kaç kez |
|---|---|---|
| Defter sayfası | 75 | 5 |
| Bölge ilk açma | 50 | 3 (ev, tarla, depo) |
| Craft ilk kez | 30 | 3 |
| **"İlk kez" anları** | **25** | **6** |

**"İlk kez" anları:** ilk ekim · ilk sulama · ilk hasat · ilk depoya boşaltma · ilk craft · ilk partner mesajı

Toplam Perde 1 tek seferlik XP: **375 + 150 + 90 + 150 = 765** · gereken 537 · **pay %42**

Pay bilinçli: oyuncu bazılarını atlarsa bile Perde 1 tıkanmıyor.

---

## 3. Düzeltilen sayılar ✅

| Ne | v0.3 | v0.4 | Sebep |
|---|---|---|---|
| Depo IV maliyeti (Sv 18) | 180.000 | **120.000** | Simülasyonda kasa -32.805'e düşüyordu *(v0.6'da 100.000'e indi)* |
| Çanta I maliyeti (Sv 4) | 400 | **0** (seviye ödülü) | Sv 4'te para kazanma yolu yok |
| Vadi anıtı | "Sv 17'de açılan geç oyun çıkışı" | **"Sv 20 sonrası hedef"** | 820.000 altın, Sv 17-20 arası ~3.5 gün var. Oyun içinde bitmesi mümkün değil, öyle sunulmamalı. |

---

## 4. Hesap kurtarma ✅ (v0.3'te hiç yoktu)

Misafir hesap + cihaz ID demek: **telefon değişti → 20 saat gitti.** Çift oyununda daha kötüsü var — bir kişinin hesabı ölünce iki kişinin dünyası ölüyor.

| Aşama | Kural |
|---|---|
| İlk açılış | Misafir hesap, kayıt yok. **Sürtünmesiz giriş korunuyor.** |
| Sv 5 (ilk tren) | Hesap bağlama teklifi: e-posta / Google / Apple. **Atlanabilir.** |
| Sv 8, Sv 12 | Bağlanmadıysa tekrar sorulur (en fazla 3 kez, sonra susar) |
| **Partner davet ederken** | **Zorunlu.** Artık başkasının ilerlemesi de sana bağlı. Gerekçesi ekranda açıkça yazar. |
| Bağlamak istemeyenler | **Kurtarma kodu** — 12 haneli, oyuncu üretir, kendi saklar. Tek seferlik. |

---

## 5. Partner seviye farkı ✅ (v0.3'te hiç yoktu)

Sv 15 oyuncu Sv 1 arkadaşını davet ederse ne olur? Davet akışının en sık yaşanacak hali buydu ve tanımsızdı.

### Karar: iki ayrı seviye kavramı

| Kavram | Ne belirler | Nasıl hesaplanır |
|---|---|---|
| **Dünya seviyesi** | Hangi bölgeler ve katlar açık | İki oyuncunun **yükseği** |
| **Oyuncu seviyesi** | Kendi tarla parseli, kendi çantası, kendi tohumları | Kişisel XP |

Yani yeni gelen açık madene girebilir (dünya oradan geçmiş), ama **kendi parseli 4 karodan başlar** ve çantası 8 slottur. Dünyanın ilerlemesi ortak, kapasite kişisel.

### Yetişme bonusu

> Oyuncu seviyesi partnerinden **3 veya daha fazla** geride ise kazandığı XP **%50 fazla** olur.

Bonus fark 2'ye düşünce kapanır. Bu olmadan geriden gelen asla yetişemez ve çift ayrışır.

---

## 6. Partner bırakırsa ✅ (v0.3'te hiç yoktu)

Çift oyununun 1 numaralı başarısızlık modu.

| Süre | Ne olur |
|---|---|
| 0-7 gün | Hiçbir şey. Depo, tarla, dünya olduğu gibi durur. |
| **7 gün** | Dünya **"tek imza" moduna** geçer: manifesto kontenjanı ve ortak proje maliyeti %70'e iner (tek kişilik ölçekleme). |
| Partner dönerse | **Anında** normale döner. Ceza yok, iz yok. |
| 30 gün | Oyuncuya sessiz bir kapı sunulur: "vadiyi tek başına sürdür" veya "yeni eş davet et". Ayrılık akışı değil, zorlama değil. |

⚠️ **Bildirim gitmez.** Küslüğün kırmızı çizgisiyle aynı mantık: oyun, birinin giremediği bir haftayı suçluluğa çevirmemeli. Oyuncu girdiğinde görür.

---

## 7. Yaş kapısı — 13 yaş altı ✅

Engellemek yerine kısıtlamak:

> 13 yaşın altındaki oyuncuda **sohbet ve partner daveti kapalı**, oyun tek kişilik oynanır.

COPPA'ya uyar, oyuncuyu kaybetmezsin, veri toplama beyanı sadeleşir.

---

## 8. Kalan kural boşlukları ✅

| Konu | Karar |
|---|---|
| **Karakter ayırt edilebilirliği** | Sv 1'de isimle birlikte **4 hazır renk** seçimi (gömlek). Partner üstünde isim etiketi her zaman görünür. Kozmetikler bunun üstüne gelir. |
| **Kısmi sipariş** | **Kısmi yatırma yok.** Sipariş ancak tam miktar elindeyken karşılanır. Tren gidince kaybedilecek bir şey oluşmaz — sıfır kenar durumu. |
| **Hayvan yemi** | Yem yoksa **üretmez, ölmez.** Yem verilince bir sonraki periyotta üretir. Kozy oyunda hayvan ölmez. |
| **Çanta dolu + 2x reklam ödülü** | Reklam **başlatılmaz**, buton pasif ve sebebi yazar. İzlenmiş reklamın ödülünün kaybolması kabul edilemez. |
| **Depodan alma yetkisi** | Partner ortak depodan her şeyi alabilir. Bu bir hata değil, tasarım — "sana demir bıraktım" bunun üzerine kurulu. |
| **Günlük sandık** | Küçük ve tahmin edilebilir: 1 tohum paketi (açık ürünlerden) + oyuncu seviyesine ölçekli az miktarda altın. Nadir madde yok — sürpriz değil, ritim. |

---

## 9. Defter — 20 sayfanın haritası ✅

Hikâyenin tek anlatım aracı bu ve haritası yoktu.

| Sayfa | Tetikleyici | İçerik |
|---|---|---|
| 1 | Oyun başlangıcı | "Bu vadide hiçbir iş tek başına görülmez." |
| 2 | İlk hasat | Vadinin kuralı: tarlada biri eker, biri sular |
| 3 | Depo açılır (Sv 2) | Ortak sandık — "burada bıraktıkların ona ulaşır" |
| 4 | Partner daveti (Sv 3) | İki imza kuralının kökeni |
| 5 | Tren garı (Sv 5) | Manifesto ne demek, neden iki imza ister |
| 6 | Market (Sv 6) | Vadiye ilk dışarıdan gelen |
| 7 | Orman (Sv 7) | Önceki çiftin diktiği ağaçlar |
| 8 | Kümes (Sv 8) | Vadiye ilk canlı döner |
| 9 | Maden girişi (Sv 9) | Çökük giriş — burada bir şey olmuş |
| 10 | İlk metal alet (Sv 10) | Önceki çiftin alet imzası |
| 11 | Maden asansörü (Sv 11) | İki katkı olmadan derine inilmez |
| 12 | Maden Kat 2 (Sv 12) | Derine inmenin bedeli |
| 13 | Değirmen (Sv 13) | Ham satmak artık aptalca |
| 14 | İnek (Sv 14) | Vadi kendine yetiyor |
| 15 | Köprü (Sv 15) | En büyük ortak iş |
| 16 | Orman derinliği (Sv 16) | Kısa pencereli nadir kaynaklar |
| 17 | Ev genişletme (Sv 17) | Nefes — burada yaşanmış |
| 18 | Maden dibi (Sv 18) | **Son çiftin neden ayrıldığı** |
| 19 | Büyük manifesto (Sv 19) | Hazırlık |
| 20 | Final (Sv 20) | Amorland tarifeye döner, defter imzalanır |

### Tek kişilik final ✅

Defterin son sayfası oyuncunun adıyla ve **boş bırakılmış bir ikinci imza satırıyla** kapanır:

> *"İkinci imza için bir yer her zaman ayrılmıştır."*

Kayıp değil, davet. Oyuncu sonradan eş davet ederse o satır dolar.

---

## 10. Not tahtası şablonları ve ifade tekerleği ✅

Gizlilik modelinin tamamı "sunucuda serbest metin değil, **şablon kimliği + nesne kimliği** saklanır" üzerine kuruluydu — ama şablon listesi hiç yoktu.

### Not tahtası şablonları (12)

| # | Şablon | Parametre |
|---|---|---|
| 1 | "Sana {nesne} bıraktım" | eşya |
| 2 | "{nesne} lazım" | eşya |
| 3 | "{bölge}'ye gidiyorum" | bölge |
| 4 | "{bölge}'de yardım lazım" | bölge |
| 5 | "Trene {nesne} yetiştirelim" | eşya |
| 6 | "Bunu satma, lazım olacak" | eşya |
| 7 | "Depo doldu" | — |
| 8 | "Bugün giremeyeceğim" | — |
| 9 | "Sıra sende" | — |
| 10 | "Teşekkürler" | — |
| 11 | "Seni özledim" | — |
| 12 | "Barışalım" | — |

### İfade tekerleği (8)

Selam · Teşekkür · Evet · Hayır · Yardım · Bekle · Harika · Barışalım

Tekerlek 8 ile sınırlı — mobilde 8'den fazla dilim parmakla ıskalanır.

---

## 11. Sv 20 sonrası ve vadi sayısı ✅

| Soru | Karar |
|---|---|
| Oyuncu kaç vadide olabilir | **Tek.** Aynı anda birden fazla vadi yok. |
| Yeni eşle yeni vadi | Eskisinden **ayrılmayı gerektirir** (ayrılık akışı, 48 sa bekleme). |
| Eski vadi | Vadiyi kuran oyuncuda kalır (v0.2 kuralı) |
| Sv 20 sonrası içerik | Haftalık manifestolar · kozmetik koleksiyon · **vadi anıtı** (asıl uzun hedef) |

---

## 12. Güncellenen doğrulama

v0.4 sayılarıyla simülasyon tekrar koşturuldu:

- Perde 1'de para yok → Sv 4 yükseltmesi bedava → **kasa hiç eksiye düşmüyor**
- Depo IV 120.000 → Sv 18'de karşılanabiliyor
- Başlangıç ürünleri tohum verdiği için Perde 1 kendi kendine yetiyor
- Perde 1 tek seferlik XP payı %42

Detay: `docs/denge/*.json` ve doğrulama script'i.
