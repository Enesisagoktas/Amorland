# Amorland — Proje Bağlamı

Bu dosya Claude Code'un projeyi tanıması için.

- Tam tasarım: `docs/amorland-tasarim-dokumani.md` (v0.2)
- **Birleştirilmiş tek doğruluk kaynağı: `AMORLAND-MASTER-PROMPT.md`** — oyunun tamamı burada, kendi kendine yeter
- Ekler (karar günlüğü): `docs/amorland-tasarim-v0.3-ek.md` (etkileşim + sayılar) · `v0.4-ek.md` (ekonomi düzeltmeleri + kural boşlukları) · `v0.5-ses.md` (ses + haptik)
- Çakışma olursa geçerlilik sırası: **JSON > master prompt > v0.5 > v0.4 > v0.3 > v0.2**
- Denge tabloları: `docs/denge/*.json` — sunucuda yapılandırma olarak yüklenir, koda gömülmez

## Proje nedir

**Amorland** — çiftler için tasarlanmış, üstten bakışlı 2.5D mobil çiftlik oyunu.

- Unity ile geliştirilecek, Android + iOS
- 2 kişilik ortak oynanış ana odak, tek kişilik de mümkün (%70 dengeyle)
- Ürün ekme/biçme, madencilik, ormancılık, basit craft
- Tren garına satış, 6 saatte bir dinamik fiyat
- ~15-20 saatlik oynanış, 20 seviye
- Ücretsiz yayınlanacak, az miktarda ödüllü reklam

## Teknik yığın

| Katman | Seçim |
|---|---|
| İstemci | **Unity 6.3 LTS (6000.3.x)** + URP + New Input System + Addressables |
| Sunucu | Spring Boot, Win10 makinede, önünde Caddy |
| Veri | PostgreSQL (dünya, envanter, oyuncu), Redis (oturum, presence) |
| Kanallar | REST + JWT (meta), WebSocket (anlık senkron + metin sohbeti) |
| Ses sohbeti | v2'ye ertelendi. Photon Voice veya Agora, kendi WebRTC yazılmayacak. |

⚠️ **Unity 6000.0 LTS kullanılmayacak** — desteği Ekim 2026'da bitiyor. 6000.3 akışının en güncel yaması alınacak.

## Değişmez mimari kuralları

1. **Otorite tamamen sunucuda.** İstemci niyet gönderir (`{"action":"harvest","x":12,"y":7}`), sunucu sonucu hesaplar. İstemci asla "500 altın kazandım" demez.
2. **Ürün büyütmek için cron yok.** `planted_at` saklanır, büyüme aşaması okuma anında hesaplanır.
3. **Tren fiyatları deterministik üretilir** (tohum + sefer numarası). Veritabanına yazılmaz. Formül: v0.3 ek, bölüm 12.
4. **Her komut `client_action_id` (UUID) taşır.** Sunucu tekrarı yutar. Mobilde bağlantı sürekli kopar.
5. **Tüm zaman hesapları sunucu saatiyle.** İstemci saatine asla güvenilmez (oyuncu telefon saatini ileri alır).
6. **Sohbet içeriği sunucuda saklanmaz.** Mesajlar cihazlarda durur; rapor edilince istemci kendi geçmişini gönderir. Engelleme listesi sunucuda.
7. **İyimser istemci.** İstemci animasyonu anında oynatır ve durumu *tahmin eder*; sunucu onaylar veya geri sardırır. İstemci asla hesaplamaz.
8. **Denge sayıları koda gömülmez.** `docs/denge/*.json` sunucuda yapılandırma olarak yüklenir. Denge ayarı yeniden derleme ve mağaza güncellemesi gerektirmemeli.

## Kilitlenen tasarım kararları (v0.3)

| Konu | Karar |
|---|---|
| Sanat yönü | **Low-poly 3D**, düz renk, 12-16 renkli sabit palet |
| Kamera | Ortografik, 50° pitch, **0° yaw kilitli**, 1 karo = 1 Unity birimi |
| Etkileşim | **Bağlam duyarlı** — alet seçimi yok, karoya dokun, oyun ne yapılacağını anlar |
| Toplu eylem | Basılı tut + sürükle = boyama (envanterde sürükle-bırak hâlâ yasak) |
| Sulama | Kova kapasitesi + kuyu. 4 kademe kova, Sv 14 yağmurlama. |
| Çanta / depo | Çanta = **çeşit** kısıtı (slot), depo = **miktar** kısıtı (birim) |
| Mevsim / oyun içi gün | **Yok** |
| Enerji barı | **Yok** |
| Premium para birimi | **Yok** |
| Cevherler | 4 tane: kömür, demir, altın, elmas. Bakır/gümüş yok. |
| Perde 1 (Sv 1-4) | **Para yok.** Başlangıç ürünleri hasatta tohumunu geri verir. |
| Ses | Oyuncuların çoğu kapalı oynar — **hiçbir mekanik sese bağlı olamaz.** Haptik birincil kanal. |

## Donanım gerçeği ⚠️

`nvidia-smi` ile ölçüldü (2026-08-19):

| | Değer |
|---|---|
| GPU | RTX 3050 Laptop, **4 GB VRAM** |
| RAM | 15.6 GB |
| CPU | i7-12700H, 14 çekirdek / 20 iş parçacığı |

**Yerel AI 3D üretimi bu makinede çalışmaz.** TRELLIS 2 min 8 GB, Hunyuan3D 2.1 min 6 GB (2GP forku) ister. SDXL de sınırın üstünde. Colab manuel bir çıkış kapısı, sistem değil.

## Asset stratejisi

Bütçe **0 TL**. Üç katman:

1. **CC0 paketler** (Kenney, Quaternius, KayKit, Poly Pizza) — karakter, yapı, prop. İhtiyacın ~%70'i.
2. **Parametrik Blender script'i** — 16 ürün × 4 aşama = 64 model gibi *tutarlılık gerektiren* setler. Tek script, garantili aynı stil, sanat yönü değişirse yeniden üretilir. CPU yeter, GPU gerekmez.
3. **Saf Python** (Pillow / SVG) — UI ikonları, zemin dokuları, ortografik render → sprite sheet.

⚠️ **AI 3D üretimi bu proje için yanlış araç** — 4090 olsa bile. Her çalıştırmada farklı siluet, oran, palet ve bozuk topoloji üretir; "aynı havucun %40 büyümüşü" diyemezsin. Script diyebilir.

Kamera yaw'ı kilitli olduğu için modellerin arkası boş olabilir ve gerekirse aynı modellerden sprite sheet render edilir.

## Geliştirici hakkında

- Java, Spring Boot, PostgreSQL, Redis, Docker, Caddy deneyimi var
- C# biliyor, Unity'de yeni
- Daha önce WebSocket tabanlı bir multiplayer oyun yaptı (tur bazlı)
- **Git komutlarını uygularken ne yaptığını kısaca açıkla**
- Yanıtlar Türkçe, sade ve kısa olsun

## Bilinen tuzaklar

- **Alet kilitlenmesi:** kazma kırılır → demir lazım → demir madende → madene kazma lazım. Değişmez kural: *hiçbir alet, o aleti yapmak için gereken kaynağın tek edinme yolu olmamalı.* Balta için de geçerli.
- **Depo kapasitesi üretimden yavaş büyümeli**, yoksa fiyat sistemi anlamsızlaşır. Bu, dikey dilimde ölçülecek **1 numaralı sayı**.
- **Unity + Git:** `.gitignore` ve Git LFS şart. `.unity` ve `.prefab` çakışmaları araçla çözülmez.
- **iOS derlemesi Mac gerektirir.** Sona bırakma.
- **Arka plana atılınca WebSocket kopar.** Yeniden bağlanma + senkron stratejisi baştan yazılmalı.
