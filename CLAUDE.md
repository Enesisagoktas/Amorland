# Amorland

Çiftler için 2.5D mobil çiftlik oyunu. Unity istemci + Spring Boot/PostgreSQL sunucu.

**Tasarımın tek doğruluk kaynağı: [AMORLAND-YAPIM-PROMPTU.md](AMORLAND-YAPIM-PROMPTU.md)**

Bu dosya kendi kendine yeter — oyun tasarımı, sunucu protokolü ve tüm denge sayıları içinde.
Çalışmaya başlamadan önce oku.

- **Bölüm A** — çalışma kuralları, yapım sırası, doğrulama kuralı, riskler
- **Bölüm B** — oyun spesifikasyonu
- **Bölüm C** — sunucu veri modeli ve protokol
- **Bölüm D** — denge verileri (sayısal doğruluk kaynağı)

**Projenin şu anki durumu: [dokuman/TESLIM.md](dokuman/TESLIM.md)**

## Kod düzeni

| Klasör | İçerik |
|---|---|
| `sunucu/` | Spring Boot. Oyunun **tüm otoritesi** burada (mimari kural 1). |
| `sunucu/src/main/resources/denge/` | Denge JSON'ları. `urunler/uretim/ilerleme` spec'ten **birebir**; `turetilmis.json` B/C'den türetilen kurallar. |
| `istemci/Assets/Amorland/Kod/` | Unity C#. Sahne kurulumu gerektirmez; `Onyukleyici` her şeyi kodla kurar. |
| `araclar/` | Doğrulama: protokol testi, bütünlük denetimi, Unity'siz C# derlemesi. |

## Değişmez kurallar

1. Otorite sunucuda — istemci sadece niyet gönderir
2. Cron yok — büyüme okuma anında hesaplanır
3. Tren fiyatı deterministik üretilir, saklanmaz
4. Her komut `client_action_id` taşır
5. Tüm zaman sunucu saatiyle (`Saat` servisi; `Instant.now()` yasak)
6. Sohbet içeriği sunucuda saklanmaz
7. İyimser istemci — tahmin eder, hesaplamaz
8. Denge sayıları koda gömülmez

## Bir şey değiştirdikten sonra

**A.6: tahmin etme, ölç.**

```bash
cd sunucu && ./mvnw test          # 56 test
python araclar/butunluk-denetimi.py  # 26 yasak + 8 kural, statik
node araclar/protokol-testi.js       # sunucu açıkken, 32 denetim
araclar/csharp-dogrulama/dogrula.sh  # Unity C# derlemesi
```

Denge sayısı değiştirdiysen `./mvnw test -Dtest=OgreticiTemposuTest` ile tempoyu tekrar ölç.

Yanıtlar Türkçe, sade ve kısa. Git komutlarını uygularken ne yaptığını kısaca açıkla.
