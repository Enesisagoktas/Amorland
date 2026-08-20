# Amorland — Hafta 1 Faz Planı

**Hedef:** A.4 #1 **Dikey Dilim** — Ev + Tarla, ek/sula/biç, iki oyuncu bağlı, durum kalıcı,
ses altyapısı + yer tutucu ses seti. Çalışır ve test edilebilir.

**Kapsam içi (Sv 1-3):** misafir hesap, vadi kurma, 6 haneli davet, iki oyuncu senkron,
tarla parselleri, ekme/sulama/hasat, büyüme hesabı, yabani kaynak, çanta, **depo**,
basit tezgâh (taş kazma/balta/ahşap kova), XP/seviye, defter 1-3, öğretici, ses + haptik.

**Kapsam dışı (Hafta 2+):** market, tren garı, orman, maden, hayvan, gübre, ortak proje,
reklam, bildirim, sesli sohbet, ayrılık akışı, kozmetik.
Sunucu tarafında bunların **veri modeli ve denge yüklemesi hazır**, mantığı yazılmadı.

---

## Sunucu fazları

| Faz | Ad | Çıktı |
|---|---|---|
| S1 | Proje iskeleti | Maven wrapper, Spring Boot 3.5/Java 21, docker-compose, .gitignore |
| S2 | Denge katmanı | 3 JSON ayrı dosya, kayıt modelleri, `DengeServisi`, `GET /api/denge` |
| S3 | Şema | Flyway V1 — C.2'deki tüm tablolar |
| S4 | Kimlik | Misafir hesap, JWT + yenileme, 12 haneli kurtarma kodu, filtre, hız sınırı |
| S5 | Vadi | Kurma, 6 haneli davet, katılma, dünya seviyesi, tek imza modu |
| S6 | Revizyon çekirdeği | revision, revizyon_kaydi, delta/tam anlık görüntü, eylem_kaydi (idempotency) |
| S7 | Oyun kuralları | Büyüme, ekme/sulama/hasat, tohum iadesi, yeniden verme, çanta/depo kısıtı, red kodları |
| S8 | Depo | koy/al/boşalt/kilitle, koyan rozeti, kapasite, eşzamanlılık |
| S9 | Yabani kaynak | Düğüm yenilenme, toplama, ortak düğüm x1.5 altyapısı |
| S10 | Craft + alet | Basit tezgâh, dayanıklılık, otomatik alet seçimi, %20 uyarısı |
| S11 | WebSocket | BAGLAN/SENKRON_*/NIYET/ONAY/RED/DEGISIM/KONUM, yayın, BASKA_CIHAZ |
| S12 | Defter + ödüller | Sayfa tetikleyicileri, "ilk kez" anları, seviye ödülleri |
| S13 | Ekonomi simülasyonu | A.6 doğrulama aracı — depo baskısı ve tempo ölçümü |

## İstemci (Unity) fazları

| Faz | Ad | Çıktı |
|---|---|---|
| U1 | Proje iskeleti | manifest.json, ProjectSettings, asmdef, .gitattributes (LFS) |
| U2 | Kurulum otomasyonu | Editor script: URP asset, kalite, build settings + runtime bootstrap |
| U3 | Ağ katmanı | REST + WebSocket, protokol DTO, yeniden bağlanma merdiveni, çevrimdışı kuyruk |
| U4 | Durum katmanı | DunyaDurumu, revizyon uygulama, iyimser tahmin + 0.2 sn geri sarma |
| U5 | Görselleştirme | Prosedürel mesh/materyal, ortografik kamera (50°, yaw kilit), karo ızgarası, 4 aşama |
| U6 | Karakter + hareket | Izgara A*, dokun-yürü, dinamik joystick, partner avatarı |
| U7 | Etkileşim | Bağlam duyarlı eylem, uzun bas menü, sürükle-boyama kuyruğu |
| U8 | UI (kod ile) | HUD, çanta, depo, tohum seçici, defter, isim/renk, yaş kapısı, davet, bağlantı perdesi |
| U9 | Ses | AudioMixer, 16'lık havuz, ±%8 pitch, 3 varyasyon, kanal sınırı, ambient kategori |
| U10 | Haptik | iOS/Android desenleri, ayarlardan kapatma |
| U11 | Öğretici | B.18 akışı, parlayan hedef + tek satır |

## Doğrulama fazları

| Faz | Ad | Çıktı |
|---|---|---|
| D1 | Bütünlük kontrolü | 26 yasak + 8 mimari kural denetimi, rapor |
| D2 | Sunucu testleri | JUnit, gömülü Postgres, koşum çıktısı |
| D3 | Simülasyon koşumu | Denge çıktısı, sapma raporu |
| D4 | C# derleme doğrulaması | Unity stub'larına karşı gerçek derleme |
| D5 | Teslim | README, kurulum adımları, el ile test senaryoları |

---

## Değişmez kurallar (her fazda denetlenir)

1. Otorite sunucuda — istemci sadece niyet gönderir
2. Cron yok — büyüme okuma anında hesaplanır
3. Tren fiyatı deterministik üretilir
4. Her komut `client_action_id` taşır
5. Tüm zaman sunucu saatiyle
6. Sohbet içeriği sunucuda saklanmaz
7. İyimser istemci — tahmin eder, hesaplamaz
8. Denge sayıları koda gömülmez
