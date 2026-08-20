# Neden asmdef yok

Baslangicta `Amorland.asmdef` ve `Amorland.Editor.asmdef` vardi. Kaldirildi.

**Sebep:** asmdef kullanan bir derleme, paket assembly'lerini (ornek `Unity.InputSystem`)
ACIKCA referans etmek zorundadir. O paket projede yoksa asmdef cozulemez ve
**hicbir script derlenmez** — Unity'de yeni olan biri icin teshisi zor bir duvar.

asmdef olmadan scriptler `Assembly-CSharp` icine girer ve mevcut butun paketleri
otomatik referans eder. `Editor/` klasoru zaten Unity tarafindan editor-only
derlemeye alinir, o kural asmdef gerektirmez.

**Ne zaman geri gelmeli:** proje buyuyup derleme suresi rahatsiz edici olunca
(kabaca 300+ dosya). O noktada paketler zaten kurulu ve sabit olur.
