# Amorland

Çiftler için 2.5D mobil çiftlik oyunu. Unity istemci + Spring Boot/PostgreSQL sunucu.

**Tek doğruluk kaynağı:** [AMORLAND-TASARIM.md](AMORLAND-TASARIM.md)

---

## Şu an nerede — Hafta 1 sonu

**A.4 #1 dikey dilim çalışıyor:** Ev + Tarla, ek/sula/biç, iki oyuncu bağlı, durum kalıcı,
ses altyapısı + yer tutucu ses seti.

| | Durum |
|---|---|
| Sunucu (Spring Boot + PostgreSQL + WebSocket) | ✅ çalışıyor, 56 test geçiyor |
| Protokol (C.3 tamamı) | ✅ 32 uçtan uca denetim geçiyor |
| Unity istemci kodu | ✅ **gerçek Unity 6000.3.22f1'de 0 hatayla derlendi**, proje kuruldu |
| Denge katmanı (mimari kural 8) | ✅ JSON'dan yükleniyor, 10 tutarlılık denetimi |
| Bütünlük (26 yasak + 8 kural) | ✅ 22 çalışma zamanı + 16 statik denetim |

**Kapsam dışı (Hafta 2+):** market, tren garı, orman, maden, hayvan, gübre, ortak proje,
reklam, bildirim, sesli sohbet, ayrılık akışı. Veri modeli ve denge yüklemesi hazır,
mantığı yazılmadı.

📄 **Önce oku:** [dokuman/TESLIM.md](dokuman/TESLIM.md) — ne çalışıyor, ne bulundu, nasıl test edilir.

---

## Hızlı başlangıç

### 1. Sunucuyu çalıştır

Docker ile (kalıcı veri):

```bash
docker compose up -d && cd sunucu && ./mvnw spring-boot:run
```

> Postgres **5433**'te (5432'de başka bir projenin container'ı vardı).

Docker olmadan (geçici veri, gömülü PostgreSQL):

```bash
cd sunucu && ./mvnw test -Dtest=YerelSunucu -DfailIfNoTests=false "-Djunit.jupiter.conditions.deactivate=*"
```

Kontrol: <http://localhost:8080/api/saglik>

### 2. Sunucuyu doğrula (Unity gerekmez)

```bash
node araclar/protokol-testi.js
```

### 3. Unity istemcisini aç

[dokuman/KURULUM.md](dokuman/KURULUM.md) adım adım anlatıyor. Özeti:
Unity Hub > `istemci/` klasörü zaten kurulu ve ayarlı → **Play**.

---

## Klasör yapısı

```
sunucu/       Spring Boot — oyunun tüm otoritesi burada (mimari kural 1)
  denge/      Denge JSON'ları: spec'ten birebir + türetilmiş kurallar
istemci/      Unity projesi — sahne kurulumu gerektirmez, her şey kodla kurulur
araclar/      Doğrulama araçları (protokol testi, bütünlük denetimi, C# derleme)
dokuman/      Teslim raporu, kurulum, faz planı
```

---

## Doğrulama komutları

| Ne | Komut |
|---|---|
| Sunucu testleri (56) | `cd sunucu && ./mvnw test` |
| Bütünlük denetimi (statik) | `python araclar/butunluk-denetimi.py` |
| Protokol testi (sunucu açıkken) | `node araclar/protokol-testi.js` |
| Unity C# derlemesi (4 varyant) | `araclar/csharp-dogrulama/dogrula.sh` |
| Öğretici temposu ölçümü | `cd sunucu && ./mvnw test -Dtest=OgreticiTemposuTest` |

---

## Proje kuralları

- **A.6:** bir sayıyı veya kuralı değiştirdiğinde etkisini **ölç**, tahmin etme
- **B.26:** 26 yasak — `python araclar/butunluk-denetimi.py` bunları denetler
- **A.4:** Perde 1 bitmeden Perde 3'e tek satır içerik üretme
