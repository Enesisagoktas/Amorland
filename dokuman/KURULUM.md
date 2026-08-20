# Kurulum

Unity'de deneyimin yoksa bu dosyayı sırayla takip et. Her adımın sonunda ne görmen
gerektiği yazıyor; görmüyorsan devam etme.

---

## 1. Sunucu

### Gerekli
- Java 21 (kurulu: Temurin 21.0.11 ✅)
- Docker Desktop **veya** hiçbir şey (gömülü veritabanı yolu var)

### A) Docker ile — veri kalıcı

```bash
docker compose up -d
```

Sonra:

```bash
cd sunucu && ./mvnw spring-boot:run
```

> ⚠ `docker compose up -d` "cannot connect to the Docker daemon" derse Docker Desktop
> kapalıdır. Başlat ve tekrar dene.

### B) Docker olmadan — veri geçici

Gömülü PostgreSQL indirir ve aynı sunucuyu 8080'de açar:

```bash
cd sunucu && ./mvnw test -Dtest=YerelSunucu -DfailIfNoTests=false "-Djunit.jupiter.conditions.deactivate=*"
```

> ⚠ Sunucu kapanınca **veri gider**. İki oturumu karşılaştıran testler için (A) yolunu kullan.

### Çalıştığını doğrula

```bash
curl http://localhost:8080/api/saglik
```

Beklenen: `{"durum":"acik","sunucuZamaniMs":...,"dengeSurumu":"..."}`

Sonra Unity'ye geçmeden protokolü doğrula:

```bash
node araclar/protokol-testi.js
```

Beklenen: `Temiz: 32 denetimin hepsi gecti.`

---

## 2. Unity

### Gerekli
- **Unity 6.3 LTS (6000.3.x)** — Unity Hub > Installs > Install Editor
  - Modüller: **Android Build Support** (JDK + SDK & NDK Tools dahil)
  - ⚠ Unity **6000.0 LTS kullanma** — desteği Ekim 2026'da bitiyor (B.2)

### Önerilen yol: mevcut klasörü aç

1. Unity Hub > **Add** > `C:\App\Amorland\istemci` klasörünü seç
2. Unity sürüm uyuşmazlığı sorarsa **kendi 6.3 sürümünü seç** — yükseltmeyi kabul et
3. İlk açılış paketleri indirir (URP, Input System, Newtonsoft), **2-5 dakika sürer**
4. Console'da kırmızı hata olmamalı

> Paket sürümü hatası alırsan: `Window > Package Manager` aç, hatalı paketi kaldırıp
> aynı ismi tekrar ekle. Unity kendi sürümüne uygun olanı seçer.

### Alternatif yol: temiz proje (paket hatası alırsan)

1. Unity Hub > **New project** > **Universal 3D** şablonu
2. Proje açılınca `istemci/Assets/Amorland` klasörünü yeni projenin `Assets/` altına kopyala
3. `Window > Package Manager > Unity Registry` > **Input System** ve **Newtonsoft Json** ekle
4. Devam et

### Kurulumu çalıştır

Menüden: **Amorland > Kurulumu Çalıştır**

Yaptıkları:
- `Assets/Amorland/Sahneler/Amorland.unity` (boş sahne) oluşturur, yapım ayarlarına ekler
- Yön: **yatay**, uyku kapalı
- Girdi: **Both** (yeni Input System + eski) — Unity yeniden başlatma isteyebilir, kabul et
- iOS **Mute Other Audio Sources: KAPALI** ← oyuncunun podcast'i kesilmesin (B.26 yasak 16)
- Gerçek zamanlı gölge kapalı, kenar yumuşatma kapalı (B.22)
- URP varsa render asset'i oluşturup bağlar

Console'da şunu görmelisin:

```
[Amorland] Kurulum tamam.
```

### Çalıştır

1. `Assets/Amorland/Sahneler/Amorland.unity` sahnesini aç
2. **Play**

Beklenen: siyah ekranda **AMORLAND** başlığı, isim kutusu ve 4 renk.

> Sunucuya ulaşamıyorsa "Sunucuya ulaşılamadı" yazar. Sunucu çalışıyor mu kontrol et.

---

## 3. İki oyuncu testi

Aynı makinede iki oyuncu test etmek için:

**Yol 1 — Editor + Build**
1. Editor'de Play → 1. oyuncu
2. `File > Build And Run` (Windows) → 2. oyuncu ayrı pencerede

**Yol 2 — Sırayla iki hesap**
1. Editor'de Play, 1. oyuncuyu oluştur, Sv 3'e gel, davet kodu al, kodu not et
2. Play'i durdur
3. Menü: **Amorland > İkinci Oyuncu Kimliği Üret**
4. Play → yeni oyuncu → Davet ekranında kodu gir

**Telefonda test:** `File > Build Settings > Android > Build And Run`.
Telefon sunucuya ulaşamazsa Ayarlar ekranından sunucu adresini bilgisayarının
yerel IP'siyle değiştir (örnek `http://192.168.1.20:8080`).

---

## 4. Faydalı menüler

| Menü | Ne yapar |
|---|---|
| `Amorland > Kurulumu Çalıştır` | Tüm proje ayarlarını uygular |
| `Amorland > Öğreticiyi Sıfırla` | Öğretici şeridini baştan başlatır |
| `Amorland > Oturumu Sıfırla` | Yeni misafir hesap açar |
| `Amorland > İkinci Oyuncu Kimliği Üret` | Aynı makinede ikinci oyuncu |

---

## 5. Sık karşılaşılan sorunlar

| Belirti | Sebep | Çözüm |
|---|---|---|
| Her şey pembe | Shader bulunamadı | `Amorland > Kurulumu Çalıştır`, URP asset'i bağlansın |
| UI'da yazı yok | İşletim sistemi yazı tipi bulunamadı | Console'daki uyarıya bak; nadir |
| "Sunucuya ulaşılamadı" | Sunucu kapalı veya adres yanlış | `curl localhost:8080/api/saglik` |
| Telefonda bağlanmıyor | `localhost` telefonun kendisi | Ayarlar > Sunucu > bilgisayarın yerel IP'si |
| Console: `ENABLE_INPUT_SYSTEM` uyarısı | Girdi modu değişti | Unity'yi yeniden başlat |
| Ürün büyümüyor | **Sulanmamış** | Kova yap → kuyudan doldur → karoyu sula |

> ⚠ **Ürün sulanmadan büyümez.** Spec sulamanın mekanik etkisini yazmıyordu; türetildi
> ve gerekçesi `denge/turetilmis.json` içindeki `sulama` bölümünde yazılı.
