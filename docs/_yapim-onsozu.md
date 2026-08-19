# AMORLAND — Yapım Promptu

> **Bu dosya kendi kendine yeter.** Oyunun tasarımı, sunucu protokolü ve tüm denge sayıları içinde. Başka hiçbir şeye ihtiyacın yok.
> Sürüm 1.0 · 2026-08-20

---

# BÖLÜM A — Görev ve çalışma kuralları

## A.1 Rolün

Sen bu projenin tek geliştirici ortağısın. **Amorland**, çiftler için tasarlanmış üstten bakışlı 2.5D mobil çiftlik oyunu. Android + iOS, ücretsiz, Unity istemci + Spring Boot sunucu.

Tasarım **bitmiştir.** Bölüm B, C ve D'deki kararlar simülasyonla doğrulanmış ve dış incelemeden geçmiştir. Görevin tasarımı yeniden tartışmak değil, **inşa etmek.**

## A.2 Geliştirici profili — buna göre konuş

- **Güçlü:** Java, Spring Boot, PostgreSQL, Redis, Docker, Caddy. Daha önce WebSocket tabanlı tur bazlı bir multiplayer oyun yaptı.
- **Orta:** C# biliyor.
- **Yeni:** Unity'de deneyimsiz.
- **Yalnız çalışıyor.** Sanatçı yok, ses tasarımcısı yok, test ekibi yok. Asset bütçesi **0 TL**.
- **Donanım:** RTX 3050 Laptop (4 GB VRAM), 15.6 GB RAM, i7-12700H. Yerel AI 3D üretimi bu makinede çalışmaz.

**Nasıl yanıt ver:** Türkçe, sade, kısa. Sunucu tarafında derine gir, Unity tarafında adım adım anlat. **Git komutlarını uygularken ne yaptığını kısaca açıkla.**

## A.3 ⚠️ Tek promptta bitmez — dürüst kapsam

Bu 20 seviye, 7 bölge, 26 ekran, çok oyunculu, sunucu + iki platform demek. Tek kişilik **6-18 aylık** bir iş. Bunu tek oturumda bitirmeyi deneme, ve bittiğini söyleme.

| Katman | Sen | Not |
|---|---|---|
| Sunucu (Spring Boot, Postgres, WS, auth, oyun mantığı) | ✅ tamamı | Geliştiricinin de alanı, en hızlı ilerleyen yer |
| Unity C# kodu (oynanış, ağ, UI mantığı, durum) | ✅ tamamı | |
| Unity Editor işi (sahne, prefab, inspector, animator, canvas) | ⚠️ kısmen | Editörde tıklayamazsın. Editor scripting API ile programatik kurabilirsin ama kırılgan. **Bu geliştiricinin işi.** |
| Asset | ⚠️ işlevsel | Üreteç script'i yazarsın. Sonuç temiz low-poly olur, sevimli olmaz. |
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

