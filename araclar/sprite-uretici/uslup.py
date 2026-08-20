"""
Amorland sprite uslup motoru.

B.22 (20.08.2026 revizyonu): hedef, dokulu ve gradyanli el cizimi illustrasyon.
Duz renk kisiti YOK. Palet.cs artik zorunlu sozlesme degil, yer tutucu.

⚠ Spec'in en buyuk uyarisi TUTARLILIK: "AI uretimi her calistirmada farkli uslup,
oran ve palet verir." Onlem burada mimariye gomuldu:
  - tek ve sabit uslup tarifi  -> bu dosyadaki sabitler ve Isik/Doku parametreleri
  - her set tek oturumda       -> uret.py tek calistirmada butun seti yazar
  - ortak palete normalize     -> butun renkler PALET'ten turetilir
  - seriler ayni tabandan      -> buyume asamalari tek taban sekilden olceklenir

Teknik: her sey ORNEKLEME KATI (SS) katinda cizilir, sonra LANCZOS ile kucultulur.
Pillow'un cizim fonksiyonlari kenar yumusatma yapmaz; tek pratik yol budur.
"""

from __future__ import annotations

import math
from dataclasses import dataclass

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# ---------------------------------------------------------------- uslup sabitleri

#: Ornekleme kati. 4 yeterli; 8 dosya boyutunu degil sureyi buyutur.
SS = 4

#: Isik yonu (ekran uzayinda, birim vektor). Sag ust.
#: Butun sprite'lar ayni yonden isik alir — tutarliligin gorunur kismi budur.
ISIK = (0.55, -0.83)

#: Doku gurultusunun siddeti (0 = duz renk, 1 = kum kagidi).
DOKU = 0.055

#: Kenar karartma miktari. Kontur cizgisi DEGIL — siluetin ic kenarinda
#: koyulasma. Sert siyah kontur "sticker" gorunumu veriyor, bu vermiyor.
KENAR = 0.28


PALET = {
    # zemin
    "cim":         (126, 168, 92),
    "cim_acik":    (152, 190, 112),
    "cim_koyu":    (94, 130, 70),
    "toprak":      (146, 110, 76),
    "toprak_acik": (172, 134, 96),
    "toprak_koyu": (104, 76, 52),
    "toprak_islak": (88, 65, 45),
    # bitki
    "yaprak":      (96, 152, 74),
    "yaprak_acik": (128, 182, 96),
    "yaprak_koyu": (58, 100, 48),
    "sap":         (128, 168, 84),
    "bugday":      (222, 186, 96),
    "bugday_acik": (244, 214, 130),
    # ahsap / tas
    "ahsap":       (150, 111, 74),
    "ahsap_acik":  (180, 138, 96),
    "ahsap_koyu":  (98, 70, 46),
    "tas":         (150, 150, 148),
    "tas_acik":    (184, 184, 180),
    "tas_koyu":    (104, 104, 102),
    "cati":        (186, 96, 78),
    "cati_acik":   (214, 126, 102),
    "cati_koyu":   (134, 64, 52),
    # su / gok
    "su":          (94, 148, 178),
    "su_acik":     (138, 190, 214),
    # ten
    "ten":         (238, 205, 174),
    "ten_koyu":    (206, 170, 138),
}


@dataclass
class Tuval:
    """RGBA calisma yuzeyi. Butun olculer SON boyuttadir; SS icerde uygulanir."""

    en: int
    boy: int

    def __post_init__(self) -> None:
        self.img = Image.new("RGBA", (self.en * SS, self.boy * SS), (0, 0, 0, 0))
        self.ciz = ImageDraw.Draw(self.img)

    # -------------------------------------------------------------- sekiller

    def elips(self, kutu, renk):
        self.ciz.ellipse(_ss(kutu), fill=_rgba(renk))

    def dikdortgen(self, kutu, renk, yaricap=0):
        if yaricap:
            self.ciz.rounded_rectangle(_ss(kutu), radius=yaricap * SS, fill=_rgba(renk))
        else:
            self.ciz.rectangle(_ss(kutu), fill=_rgba(renk))

    def cokgen(self, noktalar, renk):
        self.ciz.polygon([(x * SS, y * SS) for x, y in noktalar], fill=_rgba(renk))

    def cizgi(self, noktalar, renk, kalinlik=1):
        self.ciz.line(
            [(x * SS, y * SS) for x, y in noktalar],
            fill=_rgba(renk),
            width=max(1, int(kalinlik * SS)),
            joint="curve",
        )

    def kirp(self, noktalar):
        """
        Cizileni bir cokgenin icine hapseder.

        ⚠ Gerekli: karo yuzeyine cizilen surgu izleri, cizgi kalinligi yuzunden
        karonun disina tasiyordu ve karo "kirik" gorunuyordu. Pillow'da cizim
        sinirlamasi yok; maskeyi elle uygulamak tek yol.
        """
        maske = Image.new("L", self.img.size, 0)
        ImageDraw.Draw(maske).polygon([(x * SS, y * SS) for x, y in noktalar], fill=255)
        a = np.asarray(self.img).copy()
        a[..., 3] = (a[..., 3].astype(np.float32) * (np.asarray(maske, np.float32) / 255.0)).astype(
            np.uint8
        )
        self.img = Image.fromarray(a, "RGBA")
        self.ciz = ImageDraw.Draw(self.img)

    def yapistir(self, img: Image.Image, x, y):
        """Baska bir tuvali (SS katinda) uzerine bindirir."""
        self.img.alpha_composite(img, (int(x * SS), int(y * SS)))
        self.ciz = ImageDraw.Draw(self.img)

    # -------------------------------------------------------------- bitirme

    def isikla(self, guc=0.5, merkez=None):
        """
        Sekle hacim verir: isik yonunde acilir, ters yonde koyulasir.

        Bu, düz renk lekesini "cizilmis" gosteren tek en buyuk adim. Gradyan
        alfa maskesiyle sinirlanir, yani seklin disina tasmaz.
        """
        a = np.asarray(self.img, dtype=np.float32)
        alfa = a[..., 3:4] / 255.0
        h, w = a.shape[:2]

        ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
        cx = (merkez[0] * SS) if merkez else w / 2.0
        cy = (merkez[1] * SS) if merkez else h / 2.0
        # Isik yonundeki izdusum, -1..1 araligina normalize.
        olcek = max(w, h) / 2.0
        t = ((xs - cx) * ISIK[0] + (ys - cy) * ISIK[1]) / olcek
        t = np.clip(t, -1.0, 1.0)[..., None]

        a[..., :3] = np.clip(a[..., :3] * (1.0 + t * guc) + t * guc * 26.0, 0, 255)
        a[..., :3] *= alfa > 0
        self.img = Image.fromarray(a.astype(np.uint8), "RGBA")
        self.ciz = ImageDraw.Draw(self.img)

    def kenarla(self, guc=KENAR):
        """
        Siluetin ic kenarini koyulastirir. Sert kontur yerine bu kullanilir:
        kontur sprite'i "kesilmis sticker" gibi gosteriyor, bu ise formu tutuyor.
        """
        if guc <= 0:
            return
        a = np.asarray(self.img, dtype=np.float32)
        alfa = Image.fromarray(a[..., 3].astype(np.uint8), "L")
        icer = alfa.filter(ImageFilter.MinFilter(_tek(5 * SS // 4)))
        icer = icer.filter(ImageFilter.GaussianBlur(radius=1.6 * SS))
        kenar = (np.asarray(alfa, np.float32) - np.asarray(icer, np.float32)) / 255.0
        kenar = np.clip(kenar, 0, 1)[..., None]
        a[..., :3] = np.clip(a[..., :3] * (1.0 - kenar * guc), 0, 255)
        self.img = Image.fromarray(a.astype(np.uint8), "RGBA")
        self.ciz = ImageDraw.Draw(self.img)

    def dokula(self, siddet=DOKU, tohum=0):
        """Ince gurultu. Duz dijital yuzeyi kagit hissine yaklastirir."""
        if siddet <= 0:
            return
        rng = np.random.default_rng(1000 + tohum)
        a = np.asarray(self.img, dtype=np.float32)
        h, w = a.shape[:2]
        # Dusuk cozunurlukte uret, buyut: piksel gurultusu degil LEKE dokusu.
        kucuk = rng.normal(0.0, 1.0, (max(2, h // (2 * SS)), max(2, w // (2 * SS))))
        n = np.asarray(
            Image.fromarray(((kucuk * 60) + 128).clip(0, 255).astype(np.uint8), "L").resize(
                (w, h), Image.BILINEAR
            ),
            np.float32,
        )
        n = (n - 128.0) / 128.0
        a[..., :3] = np.clip(a[..., :3] * (1.0 + n[..., None] * siddet), 0, 255)
        self.img = Image.fromarray(a.astype(np.uint8), "RGBA")
        self.ciz = ImageDraw.Draw(self.img)

    def bitir(self, isik=0.5, kenar=KENAR, doku=DOKU, tohum=0, merkez=None) -> Image.Image:
        """Uslup zincirini uygular ve son boyuta indirir."""
        self.isikla(isik, merkez)
        self.kenarla(kenar)
        self.dokula(doku, tohum)
        return self.img.resize((self.en, self.boy), Image.LANCZOS)


# ---------------------------------------------------------------- yardimcilar


def _ss(kutu):
    return [int(round(v * SS)) for v in kutu]


def _rgba(renk):
    if isinstance(renk, str):
        renk = PALET[renk]
    if len(renk) == 3:
        return (renk[0], renk[1], renk[2], 255)
    return tuple(renk)


def _tek(n):
    n = int(n)
    return n if n % 2 == 1 else n + 1


def karistir(a, b, t):
    """Iki paleti renk arasi gecis. Seri turetmede ara tonlar icin."""
    ra, rb = (PALET[a] if isinstance(a, str) else a), (PALET[b] if isinstance(b, str) else b)
    return tuple(int(round(ra[i] + (rb[i] - ra[i]) * t)) for i in range(3))


def golge_sprite(en=64, boy=32, koyuluk=0.34) -> Image.Image:
    """
    Sahte gölge (B.22: "Sahte gölge sprite'ı her nesnenin altında").
    Yumusak kenarli elips — kati leke degil.
    """
    img = Image.new("RGBA", (en * SS, boy * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pay = int(en * SS * 0.07)
    d.ellipse([pay, pay, en * SS - pay, boy * SS - pay], fill=(28, 22, 16, int(255 * koyuluk)))
    img = img.filter(ImageFilter.GaussianBlur(radius=en * SS * 0.055))
    return img.resize((en, boy), Image.LANCZOS)


def yaprak_kumesi(t: Tuval, cx, cy, yaricap, renk_ic, renk_dis, tohum, adet=7):
    """
    Ust uste binen dairelerden yaprak kutlesi. Tek buyuk daire "balon" gibi
    duruyor; kume silueti kiriyor ve el cizimi hissi veriyor.
    """
    rng = np.random.default_rng(tohum)
    for i in range(adet):
        aci = (i / adet) * math.tau + rng.uniform(-0.25, 0.25)
        mes = yaricap * rng.uniform(0.32, 0.62)
        r = yaricap * rng.uniform(0.46, 0.68)
        x = cx + math.cos(aci) * mes
        y = cy + math.sin(aci) * mes * 0.72
        # Isiga bakan kumeler acik, ters taraf koyu.
        yon = (math.cos(aci) * ISIK[0] + math.sin(aci) * ISIK[1] + 1) / 2
        t.elips([x - r, y - r * 0.9, x + r, y + r * 0.9], karistir(renk_dis, renk_ic, yon))
    t.elips([cx - yaricap * 0.5, cy - yaricap * 0.5, cx + yaricap * 0.5, cy + yaricap * 0.42], renk_ic)
