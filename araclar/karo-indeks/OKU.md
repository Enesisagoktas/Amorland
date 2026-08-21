# Karo indeks tabloları

`KaroSeti.cs` içindeki her indeks **bu görüntülerden okundu**. Silme.

| Dosya | Ne |
|---|---|
| `ciftlik-indeks.png` | Tiny Farm — ekin, hayvan, çiftlik eşyası |
| `kasaba-ust-satirlar.png` | Tiny Town 0–56 — çim, yol autotile, ağaç, çit |
| `kasaba-alt-satirlar.png` | Tiny Town 48–131 — yapı, kapı, maden ağzı, alet |
| `su-autotile.png` | Tiny Battle 36–93 — su ve kıyı |
| `supheli-karolar.png` | Yanlış tahmin ettiğim karolar, 24× büyütülmüş |

## Neden bu klasör var

İndeksleri gözle görmeden seçmek **iki kez** haritayı bozdu:

1. **Tiny Farm'da çim yok.** 105/107/117/119'u çim sandım. Dördünde de çepeçevre koyu
   kontur (rgb 63,38,49) var — bunlar tek başına duran yama karoları. Döşeyince harita
   baştan başa koyu ızgara çıktı. Ölçüm: sayfada saf çim 16×16 pencere sayısı **sıfır**.

2. **Çakıl (43) zemin karosu değil.** Çim üstüne serpilmiş taş yaması. Market meydanını
   onunla döşeyince düzenli puantiyeli duvar kâğıdı oldu. Gerçek dikişsiz taş **109**
   (%100 tek renk).

Ayrıca `68`'i düz yatay çit sandım; köşe parçasıymış, düz olan **81**. `92`'yi market
tezgâhı sandım; toprak yığınıymış.

## Yeniden üretmek

`tilemap_packed.png` sayfaları `varliklar/kenney/paket/<paket>/Tilemap/` altında.
Etiketli tabloyu Pillow ile 10× `Image.NEAREST` büyüterek ve hücre başına indeks
yazarak üret. Unity'nin kendi dilimlemesiyle karşılaştırmak için:

```bash
Unity.exe -batchmode -projectPath istemci -executeMethod Amorland.EditorAraclari.HaritaGoruntusu.KaroTablosu -cikti <klasor> -quit
```

Bu, Python'un gördüğü sıra ile Unity'nin ürettiği sıranın aynı olduğunu doğrular.
