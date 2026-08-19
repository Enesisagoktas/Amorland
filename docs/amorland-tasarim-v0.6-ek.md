# Amorland — Tasarım Eki v0.6

> Dış inceleme (ChatGPT) + ölçüm sonucu bulunan hataların kapanışı.
> Geçerlilik: **v0.6 > v0.5 > v0.4 > v0.3 > v0.2** · Protokol ayrı dosyada: `amorland-sunucu-protokol.md`
>
> Tarih: 2026-08-20

---

## 1. ⚠️ En büyük bulgu — saatlik getiri kademesi gerçek oyunda geçersizdi

Dokümanın ekonomik temeli şu tabloydu:

> 5 dk: 24 · 30 dk: 30 · 2 sa: 36 · 6 sa: 42 · 12 sa: 48 altın/saat/karo
> "Uzun ürün saatte ~%25 daha kârlı."

**Bu ölçüldüğünde tutmuyor.** Sebep: oyuncu günde 3.5 oturum açıyor, yani **oturum arası 6.86 saat.** Bundan kısa büyüyen ürün olgunlaştıktan sonra karoyu boş bekletiyor.

Gerçekleşen değerler:

| Ürün | Kademe iddiası | Gerçekleşen | Fark |
|---|---|---|---|
| Buğday (5 dk) | 24 altın/sa | **0.3** | 80× |
| Turp (30 dk) | 30 altın/sa | **2.2** | 14× |
| Mısır (2 sa) | 36 altın/sa | 10.5 | 3.4× |
| Kabak (6 sa) | 42 altın/sa | 25.7 | 1.6× |
| Karpuz (12 sa) | 48 altın/sa | 33.4 | 1.4× |

### Karar: kısa ürünlerin rolü yeniden tanımlanıyor

Kısa ürünler saatlik getiride yarışamaz ve yarışmamalı. **Farklı bir işleri var:**

> **Uzun ürün oyuncu yokken çalışır. Kısa ürün oyuncu oradayken çalışır.**
>
> 5-30 dakikalık ürünler **aktif oturum dolgusudur**: oyuncu zaten oyundayken bir hasat daha verir, bekleme süresini doldurur, oturumu "dolu" hissettirir. Saatlik getiri onların ölçüsü değildir.

🔧 **Düzeltme:** "Uzun ürün saatte ~%25 daha kârlı" ifadesi yanlış, kaldırılıyor. Saatlik getiri kademesi **sadece büyüme süresi ≥ oturum arası olan ürünler için** (2 saat ve üzeri) geçerlidir; o bantta gerçekleşen fark 1.4-3.4×.

---

## 2. Yeniden veren ürünler — tanım + denge

Önceden `{"adet": 3, "araSn": 7200}` yazıyordu ama **"3" ne demek belli değildi.** Bu, ekonomi simülasyonunu doğrudan geçersiz kılıyordu.

| Soru | Karar |
|---|---|
| `adet` ne sayıyor | **Toplam** hasat (ilk dahil). 3 = 1 ilk + 2 yeniden. |
| Tohum | Sadece ekimde bir kez. Yeniden hasatlar bedava. |
| Son hasattan sonra | Karo boşalır, yeniden ekilir. |
| Gübre | **Sadece bir sonraki hasata** +1. Tüm döngüye olsaydı gübre+yenidenveren otomatik en iyi seçenek olurdu. |
| Depo doluysa | Hasat engellenir, ürün karoda bekler, **yeniden verme sayacı işlemez**. |
| Fiyat | Kademesinin **%15-20 üstünde** — karoyu 3-4 kat uzun kilitlemenin karşılığı. Kademe içi ±%10 kuralının bilinçli istisnası. |

### Düzeltilen sayılar

| Ürün | Değişiklik | Sebep |
|---|---|---|
| **Domates** | ara 2sa→**6sa**, adet 3→**4**, fiyat 75→**85** | 2 saatlik ara oturum arasının (6.9 sa) altındaydı — yeniden verme hiç işe yaramıyordu, ürün normal muadilinden **kötüydü** |
| **Üzüm** | ara 6sa→**12sa** | Kademesinin %69 üstündeydi, Sv 15'ten sonra tek seçenek haline geliyordu |
| **Safran** | tohum 300→**420** | Kademenin %43 üstündeydi. Fiyatı korunuyor ama riski artıyor — kıt tohumlu prestij ürünü. |

Sonuç — kademe içi dağılım artık en fazla %20:

```
 2 saat: Domates 11.5 · Mısır 10.5 (-9%) · Patates 9.9 (-14%)
 6 saat: Çilek 31.0 · Lahana 26.5 (-14%) · Kabak 25.7 (-17%) · Ayçiçeği 24.9 (-20%)
12 saat: Üzüm 40.4 · Safran 40.0 (-1%) · Balkabağı 35.0 (-13%) · Kavun 34.3 (-15%) · Karpuz 33.4 (-17%)
```

---

## 3. "Kısa pencere" iki farklı şeye takılmıştı

Aynı etiket hem Sv 16 yabani kaynaklar (tanımlı) hem kavun/safran (tanımsız) için kullanılıyordu.

🔧 Ürün tarafı **`tohumPenceresi`** olarak ayrıldı:

> Bu ürünlerin **tohumu** markette sadece belirli pencerelerde satılır (günde 3 kez, 2 saat, deterministik). **Büyüme ve hasat normaldir.**

- Hasat penceresi **yok** — ürün tarlada bekleyebilir, çürümez. "Eşya asla yok edilmez" kuralı geçerli.
- Fiyat primi tam olarak bu arz kısıtından geliyor: ölçekli ekilemiyor.

---

## 4. Ortak düğüm — ölçülen 4× kaynak patlaması

Kural şuydu: *"katkısı %25 üstünde olan her oyuncu tam getiriyi alır."*

Ölçüm sonucu: iki oyuncu aynı düğüme vurunca N vuruş yarılanıyor **ve** ikisi de tam loot alıyor →
**oyuncu başına 2×, toplamda 4× cevher.** Ekonomi tablosu tek oyuncu varsayımıyla kurulmuştu.

🔧 **Düzeltme: ortak vurulan düğümün sağlığı ×1.5 olur.**

| | Solo | Duo (düzeltme öncesi) | Duo (v0.6) |
|---|---|---|---|
| Oyuncu başına düğüm/gün | 13.3 | 26.7 (2.00×) | 17.8 (**1.33×**) |

"Birlikte çalışmak asla tek başına çalışmaktan kötü olmamalı" kuralı korunuyor (hâlâ 1.33× avantaj), kaynak patlaması kapanıyor.

---

## 5. Solo vs Duo — hedef yanlıştı, sayılar değil

Tam ilerleme simülasyonu (oyuncu başına):

```
Sv 20'ye:  SOLO 15.3 gün (7.1 sa aktif)  ·  DUO 14.1 gün (6.6 sa)
Süre oranı        1.08×
Gelir oranı (duo) 1.24×
Maden oranı (duo) 1.33×
```

**%70 ölçekleme ilerleme süresine neredeyse hiç etki etmiyor.** Çünkü XP'nin çoğu kendi parselinden geliyor ve o iki modda da aynı.

🔧 **Karar: sayıları zorlamak yerine hedef yeniden yazılıyor.**

> Tek kişilik oyun **daha yavaş değil, daha yalın.** Aynı sürede bitirilir ama %19 daha az altın ve %25 daha az cevherle. Fark bolluk farkıdır, duvar değil.

Gerekçe: bir çift oyununda tek oyuncuyu süre olarak cezalandırmak yanlış olurdu — partner bulamamak bir kusur değil. Bolluk farkı hikâyeyi ("tek imzalı manifesto daha az karşılık bulur") koruyor, ilerlemeyi tıkamıyor.

⚠️ Doğrulanması gereken: Sv 11 ve Sv 15 ortak projeleri %70 maliyetle tek kişide gerçekten geçilebiliyor mu — bu **oyun testi** işi, simülasyon değil.

---

## 6. "Her seviye tek yeni şey" kuralı — gerçekten çiğneniyordu

v0.3'te Sv 3 ihlalini fark edip "tek özellik kümesi sayılır" deyip geçmiştim. El sallamaydı. Sv 5 (4 şey) ve Sv 14 (3 şey) hiç fark edilmemişti.

🔧 **Kural yeniden yazılıyor:**

> **Her seviye tek bir yeni ETKİLEŞİM açar.**
> Pasif açılımlar sayılmaz: ürün çeşidi, kapasite artışı (çanta/depo/tarla), yeni craft tarifi, ekonomik gösterge.

| Sayılır (etkileşim) | Sayılmaz (pasif) |
|---|---|
| Yeni bölge | Yeni ürün |
| Yeni yapı (kümes, ahır, değirmen) | Çanta/depo/tarla yükseltmesi |
| Yeni iletişim kanalı | Yeni craft tarifi |
| Ortak proje | Altın göstergesinin belirmesi |
| Maden katı | Hesap bağlama teklifi |

Yeni kuralla denetim:

| Sv | İçerik | Etkileşim sayısı |
|---|---|---|
| 3 | Partner daveti + ifade tekerleği + sohbet (**tek küme: iletişim**) + havuç (pasif) | 1 ✅ |
| 5 | Tren garı + patates (pasif) + para (pasif) + hesap teklifi (pasif) | 1 ✅ |
| 14 | İnek/ahır + çanta (pasif) + yağmurlama (**tarif = pasif**) | 1 ✅ |

---

## 7. Küslük — ekonomik etkisi kaldırılıyor ⚠️

Küslük manifesto bonusunu 1.8× → 1.3× düşürüyordu. Dış inceleme haklı olarak şunu söyledi:

> *Bu hâlâ bir oyuncunun partnerini **ekonomik olarak cezalandırabildiği** bir düğme.*

Doğru. Savunmam "bu çiftin oynadığı bir rol" idi — ama **bir rolün para cezası olmasına gerek yok.** Para cezası olduğu anda rol olmaktan çıkıp silaha dönüşüyor. Kötü niyetli senaryo gerçek: partner küser, çiftin geliri düşer, diğeri bunu düzeltemez.

🔧 **Karar: küslüğün ekonomik etkisi yok.**

| | v0.2-v0.5 | **v0.6** |
|---|---|---|
| Manifesto bonusu | 1.8× → 1.3× | **değişmez** |
| Gösterge | avatar üstünde | avatar üstünde (aynı) |
| Barışma ritüeli | hediye / "barışalım" | aynı |
| Bildirim | yok | yok |
| 48 saatte düşme | var | var |

Geriye kalan tam olarak değerli olan kısım: çiftin "kırıldım" diyebildiği **biçimsel bir kanal** ve bir **barışma ritüeli**. Ekonomik diş olmadan bu bir ifade aracı; dişle birlikte bir yaptırım aracıydı.

---

## 8. Hayvan sistemi — başlık değil, sistem

Önceden 2 tablo satırı + 1 cümleydi ("yem yoksa üretmez, ölmez"). Tam döngü:

| | Tavuk | İnek |
|---|---|---|
| Açılış | Sv 8 (kümes) | Sv 14 (ahır) |
| Satın alma | 500 | 4.000 |
| Ürün | Yumurta (40) | Süt (180) |
| Periyot | 24 sa | 24 sa |
| Yem / periyot | 1 | 2 |
| XP | 3 | 8 |

**Kapasite:** Kümes 4 (Sv 8) → 8 (Sv 12, 6.000 altın) · Ahır 2 (Sv 14) → 4 (Sv 17, 40.000 altın)

**Yem:** ormandan yem otu (45 dk yenilenme) veya marketten 12 altın. **Craftlanmaz** — 25 tarif sınırı dolu.

**Besleme: tek dokunuşla yapıdaki tüm hayvanlar.** 8 tavuğu tek tek beslemek mobilde işkence; bağlam duyarlı etkileşim felsefesiyle tutarlı.

**Toplama:** ürün hayvanın yanında belirir, dokunarak **çantaya** alınır (otomatik depoya gitmez — depo kısıtı korunmalı).

**Birikme limiti: 3 periyot.** 3 günden fazla birikmez, üretim durur. Sınırsız çevrimdışı birikim yok.

**Yok:** mutluluk, hastalık, ölüm, üreme. Kozy oyun, kapsam dışı.

---

## 9. Depo — sosyal UX

Ekonomik tasarım netti, ortak kullanım UX'i yoktu.

| Konu | Karar |
|---|---|
| Kim koydu | Her yığında **koyanın renk rozeti** |
| Partner alabilir mi | **Evet** — "sana demir bıraktım" bunun üzerine kurulu, kısıtlanmıyor |
| Kilitleme | Oyuncu **en fazla 3 yığın** kilitleyebilir; partner alamaz. Sınır kasıtlı — yoksa depo ikiye bölünür |
| "Bunu satma" | Not tahtası şablonu ile söylenir; kilit ise mekanik güvence |
| Kazara boşaltma | **"Depoya boşalt" var, "depodan al" toplu butonu YOK.** Tek yönlü kolaylık. |
| Eşzamanlı alma | Sunucuya ilk ulaşan kazanır, diğeri `ESYA_YOK` |

---

## 10. Ayrılıkta sahiplik

"Seviye ve çanta korunur, ev/depo vadide kalır" yetersizdi — geri kalan her şey tanımsızdı.

| Varlık | Kim alır |
|---|---|
| Seviye, XP | Her oyuncu kendininkini korur |
| Çanta içeriği ve aletler | Kendinde |
| **Depo içeriği** | Vadiyi kurana kalır — ama **ayrılan oyuncu %50'sini "yol azığı" olarak alır** |
| Kendi parselindeki ekili ürünler | Kaybolur; **tohum değeri altın olarak iade edilir** |
| Ev, depo yapısı, depo kapasitesi | Vadiyi kurana |
| Hayvanlar | Vadiyi kurana |
| Vadi anıtı ilerlemesi | Vadiyi kurana |
| **Craft tarif açılımları** | **İkisinde de kalır** — öğrenilmiş bilgi geri alınmaz |
| Kozmetikler | İkisinde de (kişisel) |
| Çanta yükseltmeleri | Kişisel, korunur |
| Defter ilerlemesi | İkisinde de (v0.2 kuralı) |
| Dünya seviyesi, bölge/kat açılışları | Yeni vadide **oyuncunun kendi seviyesine göre** yeniden hesaplanır |

⚠️ **%50 depo payı kasıtlı:** onsuz vadiyi kuran oyuncu, partnerini hiçbir şeysiz bırakabilirdi. Tek taraflı ayrılıkta bile taraflardan biri diğerini soyamamalı.

---

## 11. Yetişme bonusu — tavan

Partnerinden 3+ seviye geride olan oyuncu ×1.5 XP alıyordu, süre sınırı yoktu.

🔧 **En fazla 14 gün.** Sonra fark kapanmasa bile bonus düşer. Bilerek geri kalıp biriktirmeyi engeller.

(Sömürü riski zaten düşüktü — bilerek geri kalmak için oynamamak gerekiyor, kaybettiğin XP kazandığından fazla. Yine de tavan ucuz.)

---

## 12. Reddedilen bulgu

**"Ürün fiyatları saatlik getiri kademesiyle örtüşmüyor"** — bu bir hata değil.

Kademe içi ±%10 sapma **kasıtlıdır**: bir kademedeki 4 ürün birebir aynı fiyatta olsaydı aralarında seçim yapmak anlamsız olurdu.

Ama işaret ettiği belge boşluğu gerçekti: **bu varyasyonun kasıtlı olduğu hiçbir yerde yazmıyordu.** Artık `urunler.json` içinde `saatlikGetiriVaryasyonu` olarak yazılı.

---

## 13. Protokol dokümanına giden bulgular

Aşağıdakiler `amorland-sunucu-protokol.md` içinde çözüldü:

| Bulgu | Nerede |
|---|---|
| Reconnect + state resync + revizyon numarası | Bölüm 1, 3, 9 |
| Çevrimdışı sınırı (çelişki vardı) | Bölüm 4 |
| Çakışma çözümü | Bölüm 5 |
| Reklam ödül atomikliği + "2× hasat" tam anlamı | Bölüm 6 |
| Bildirim önceliği | Bölüm 7 |
| Günlük sandık kuralları | Bölüm 8 |
| Veri modeli | Bölüm 2 |

⚠️ Çevrimdışı konusunda master prompt'ta **gerçek bir çelişki** vardı: bir yerde "eylemler yerel kuyruğa alınır", başka yerde "bağlantı gerektiği baştan söylenir". Protokol bölüm 4 bunu ilkeli bir sınırla çözüyor: *kendi parselinde başladığın işi bitirebilirsin, paylaşılan duruma dokunan hiçbir şeyi yapamazsın.*
