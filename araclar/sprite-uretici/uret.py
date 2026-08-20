"""
Amorland sprite seti — TEK OTURUMDA uretir.

Spec B.22 kaynak stratejisi 3: "Saf Python (Pillow/SVG) — atlas paketleme, palet
normalize, asama varyanti turetme."

⚠ Bu dosya bilerek TEK PARCA. Spec'in uyarisi: "AI uretimi her calistirmada farkli
uslup, oran ve palet verir. Onlem: (a) tek ve sabit uslup tarifi, (b) her set TEK
OTURUMDA uretilir, (c) ortak palete normalize edilir, (d) seriler ayni taban
gorselden turetilir." Sprite'lari birden fazla yere bolmek veya parca parca
uretmek bu onlemi bozar.

Cikti: istemci/Assets/Amorland/Resources/sprite/*.png + sprite.json

Unity tarafi bunlari Resources.Load<Texture2D> ile okuyup Sprite.Create ile
kurar — editor kurulumu GEREKMEZ (mimari kural: her sey kodla kurulur).
"""

from __future__ import annotations

import json
import math
import os

import numpy as np
from PIL import Image

from uslup import PALET, SS, Tuval, golge_sprite, karistir, yaprak_kumesi

# ---------------------------------------------------------------- cikti

KOK = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CIKTI = os.path.join(KOK, "istemci", "Assets", "Amorland", "Resources", "sprite")

#: Bir dunya karosunun ekrandaki genisligi (piksel). Butun olcekler buna baglidir.
KARO_EN = 128
KARO_BOY = 74
KARO_KALINLIK = 16

_kayit: dict[str, dict] = {}


def yaz(ad: str, img: Image.Image, pivot=(0.5, 0.5)) -> None:
    """PNG'yi ve Unity import ayarlarini yazar."""
    os.makedirs(CIKTI, exist_ok=True)
    yol = os.path.join(CIKTI, ad + ".png")
    img.save(yol)
    _kayit[ad] = {"en": img.width, "boy": img.height, "pivot": list(pivot)}


# ---------------------------------------------------------------- karolar


def _karo(taban, ic, izler=False, tohum=0, kalinlik=KARO_KALINLIK) -> Image.Image:
    """
    Kalinlikli 2.5D karo: ust elmas + iki yan yuz.

    ⚠ Kalinlik sus degil: kalinliksiz elmas zemine yapistirilmis bir cikartma gibi
    duruyor. Yan yuzler karoyu hacim yapar, referanstaki plato hissi buradan gelir.
    """
    en, boy = KARO_EN, KARO_BOY + kalinlik
    t = Tuval(en, boy)
    uy, oy = 2, KARO_BOY - 2
    sol, sag, ort = 2, en - 2, en / 2
    ory = (uy + oy) / 2

    ust = [(ort, uy), (sag, ory), (ort, oy), (sol, ory)]

    t.cokgen([(sol, ory), (ort, oy), (ort, oy + kalinlik), (sol, ory + kalinlik)],
             karistir(taban, "toprak_koyu", 0.55))
    t.cokgen([(ort, oy), (sag, ory), (sag, ory + kalinlik), (ort, oy + kalinlik)],
             karistir(taban, "toprak_koyu", 0.72))

    t.cokgen(ust, taban)
    t.cokgen([(ort, uy + 10), (sag - 12, ory), (ort, oy - 10), (sol + 12, ory)], ic)

    if izler:
        # ⚠ Ayri tuvale cizilip ust elmasa kirpilir; ayni tuvale cizilirse
        # cizgi kalinligi karonun disina tasar.
        iz = Tuval(en, boy)
        for i in range(-4, 5):
            k = i * 10
            iz.cizgi([(ort - 40 + k, ory + 20), (ort + 40 + k, ory - 20)],
                     karistir(taban, "toprak_koyu", 0.45), kalinlik=1.5)
        iz.kirp(ust)
        t.yapistir(iz.img, 0, 0)

    return t.bitir(isik=0.34, kenar=0.2, tohum=tohum)


def karolar() -> None:
    yaz("karo_cim", _karo("cim", karistir("cim", "cim_acik", 0.5), tohum=3))
    yaz("karo_cim_koyu", _karo("cim_koyu", karistir("cim_koyu", "cim", 0.5), tohum=4))
    yaz("karo_toprak", _karo("toprak", karistir("toprak", "toprak_acik", 0.42), izler=True, tohum=5))
    # Sulanmis: acikca daha koyu ve DOYGUN — "yanmis" degil "islak" okunmali.
    # ⚠ Islak toprak KOYU ama OLU degil: toprak_islak'a dogrudan gidince
    # yanmis gibi okunuyordu. Tabana dogru geri karistirilip hafif maviye
    # cekiliyor — "nemli" bilgisi rengin doygunlugundan gelir, koyulugundan degil.
    islak = karistir(karistir("toprak_islak", "toprak", 0.42), "su", 0.14)
    yaz("karo_sulu", _karo(islak, karistir(islak, "toprak_acik", 0.30), izler=True, tohum=6))
    yaz("karo_yol", _karo(karistir("toprak_acik", "tas", 0.35),
                          karistir("toprak_acik", "tas_acik", 0.4), tohum=7))


# ---------------------------------------------------------------- urunler


#: Urun basina siluet ve renk tarifi. AYNI taban cizimden turetilir (spec kurali d).
URUNLER = {
    "bugday":   {"yaprak": "yaprak",      "meyve": "bugday",           "tip": "basak"},
    "havuc":    {"yaprak": "yaprak_acik", "meyve": (226, 132, 62),     "tip": "kok"},
    "marul":    {"yaprak": "yaprak_acik", "meyve": (140, 190, 96),     "tip": "yumru"},
    "domates":  {"yaprak": "yaprak",      "meyve": (206, 76, 60),      "tip": "meyve"},
    "patates":  {"yaprak": "yaprak_koyu", "meyve": (198, 164, 106),    "tip": "kok"},
    "misir":    {"yaprak": "yaprak",      "meyve": (232, 198, 96),     "tip": "basak"},
    "cilek":    {"yaprak": "yaprak_acik", "meyve": (208, 68, 74),      "tip": "meyve"},
    "kabak":    {"yaprak": "yaprak_koyu", "meyve": (222, 148, 60),     "tip": "yumru"},
}


def urun(ad: str, asama: int, asama_sayisi: int = 4) -> Image.Image:
    """
    Tek taban cizimden turetilen buyume asamasi.

    ⚠ Asamalar sifirdan cizilmez: boy, yaprak sayisi ve meyve gorunurlugu tek
    orandan turetilir. Boylece "ayni bitkinin buyugu" olur, dort ayri bitki degil.
    """
    tarif = URUNLER[ad]
    en, boy = 64, 96
    t = Tuval(en, boy)
    ox = en / 2
    oran = asama / max(1, asama_sayisi - 1)
    yukseklik = 26 + 62 * oran
    genislik = 7 + 9 * oran
    dip = boy - 6

    # sap
    t.cokgen(
        [(ox - genislik * 0.4, dip), (ox + genislik * 0.4, dip),
         (ox + genislik * 0.25, dip - yukseklik), (ox - genislik * 0.25, dip - yukseklik)],
        "sap",
    )

    # Yapraklar. ⚠ Siluet tipi basina acilma acisi ve sayisi DEGISIR: ayni
    # cizimden turetilirken bile bugday ile marul uzaktan ayirt edilebilmeli.
    # Ilk sette hepsi ayni acidaydi ve dort urun ayni bitki gibi duruyordu.
    tip = tarif["tip"]
    yaprak_sayisi = {"basak": 2 + asama, "kok": 3 + asama * 2,
                     "yumru": 4 + asama * 2, "meyve": 2 + asama * 2}[tip]
    acilma = {"basak": 0.55, "kok": 1.35, "yumru": 1.6, "meyve": 1.0}[tip]
    for i in range(yaprak_sayisi):
        yon = -1 if i % 2 else 1
        h = dip - 6 - yukseklik * (0.30 + 0.62 * (i / max(1, yaprak_sayisi - 1)))
        uc = (ox + yon * (10 + 12 * oran) * acilma, h - (12 + 9 * oran) / acilma)
        t.cokgen([(ox, h + 5), (ox, h - 5), uc],
                 karistir(tarif["yaprak"], "yaprak_acik", min(0.9, 0.22 + 0.15 * i)))

    # meyve/urun: yalnizca son iki asamada, olgunda tam
    if asama >= asama_sayisi - 2:
        tam = asama >= asama_sayisi - 1
        m = tarif["meyve"]
        tepe = dip - yukseklik
        if tarif["tip"] == "basak":
            n = 9 if tam else 5
            for i in range(n):
                y = tepe + i * 5
                g = (13 if tam else 8) - i * 0.75
                t.elips([ox - g, y - 5, ox + g, y + 6], karistir(m, "bugday_acik", 1 - i / n))
        elif tarif["tip"] == "meyve":
            n = 3 if tam else 1
            for i in range(n):
                a = -0.6 + i * 0.6
                cx = ox + math.sin(a) * 12
                cy = tepe + 16 + math.cos(a) * 6
                r = 8 if tam else 5
                t.elips([cx - r, cy - r, cx + r, cy + r], m)
                t.elips([cx - r * 0.45, cy - r * 0.6, cx + r * 0.1, cy - r * 0.05],
                        karistir(m, (255, 255, 255), 0.35))
        elif tarif["tip"] == "yumru":
            r = 20 if tam else 13
            t.elips([ox - r, dip - r * 1.5, ox + r, dip + r * 0.35], m)
            t.elips([ox - r * 0.5, dip - r * 1.35, ox + r * 0.15, dip - r * 0.6],
                    karistir(m, (255, 255, 255), 0.28))
        else:  # kok — topragin ustunde omuz gosterir
            r = 12 if tam else 8
            t.elips([ox - r, dip - r * 0.7, ox + r, dip + r * 0.5], m)

    return t.bitir(isik=0.5, tohum=asama * 13 + len(ad), merkez=(ox, dip - yukseklik * 0.7))


def urunler() -> None:
    for ad in URUNLER:
        for a in range(4):
            yaz(f"urun_{ad}_{a}", urun(ad, a), pivot=(0.5, 0.06))


# ---------------------------------------------------------------- dogal ogeler


def agac(tohum=0, olcek=1.0) -> Image.Image:
    en, boy = 96, 128
    t = Tuval(en, boy)
    ox = en / 2
    t.cokgen([(ox - 5, boy - 18), (ox + 5, boy - 18), (ox + 8, boy - 4), (ox - 8, boy - 4)], "ahsap_koyu")
    t.cokgen([(ox - 4, boy - 46), (ox + 4, boy - 46), (ox + 5, boy - 16), (ox - 5, boy - 16)], "ahsap")
    yaprak_kumesi(t, ox, boy - 62, 30 * olcek, "yaprak_acik", "yaprak_koyu", tohum, adet=8)
    yaprak_kumesi(t, ox + 2, boy - 86, 22 * olcek, "yaprak_acik", "yaprak", tohum + 7, adet=6)
    return t.bitir(isik=0.55, tohum=tohum, merkez=(ox, boy - 70))


def cali(tohum=0) -> Image.Image:
    en, boy = 72, 56
    t = Tuval(en, boy)
    yaprak_kumesi(t, en / 2, boy - 20, 26, "yaprak_acik", "yaprak_koyu", tohum, adet=7)
    return t.bitir(isik=0.5, tohum=tohum, merkez=(en / 2, boy - 26))


def kaya(tohum=0, olcek=1.0) -> Image.Image:
    en, boy = 72, 56
    t = Tuval(en, boy)
    rng = np.random.default_rng(200 + tohum)
    ox, oy = en / 2, boy - 12
    noktalar = []
    for i in range(7):
        a = (i / 7) * math.tau
        r = 32 * olcek * rng.uniform(0.74, 1.0)
        noktalar.append((ox + math.cos(a) * r, oy + math.sin(a) * r * 0.62))
    t.cokgen(noktalar, "tas")
    t.cokgen([(ox - 10, oy - 14), (ox + 6, oy - 18), (ox + 14, oy - 8), (ox - 2, oy - 4)], "tas_acik")
    return t.bitir(isik=0.6, tohum=tohum, merkez=(ox + 6, oy - 14))


def ot(tohum=0) -> Image.Image:
    en, boy = 48, 40
    t = Tuval(en, boy)
    rng = np.random.default_rng(300 + tohum)
    ox, dip = en / 2, boy - 4
    for i in range(5):
        yon = rng.uniform(-1, 1)
        h = rng.uniform(14, 26)
        t.cokgen([(ox - 2 + i * 1.4, dip), (ox + 2 + i * 1.4, dip),
                  (ox + yon * 9 + i * 1.4, dip - h)],
                 karistir("yaprak", "yaprak_acik", rng.uniform(0.2, 0.8)))
    return t.bitir(isik=0.45, kenar=0.18, tohum=tohum)


def dogal() -> None:
    for i in range(3):
        yaz(f"agac_{i}", agac(i + 1, 1.0 - i * 0.09), pivot=(0.5, 0.04))
    yaz("cali", cali(3), pivot=(0.5, 0.08))
    for i in range(2):
        yaz(f"kaya_{i}", kaya(i + 2, 1.0 - i * 0.22), pivot=(0.5, 0.12))
    for i in range(3):
        yaz(f"ot_{i}", ot(i), pivot=(0.5, 0.08))


# ---------------------------------------------------------------- yapilar


def _cati(t, ox, tabanY, en, yuk, renk):
    t.cokgen([(ox - en / 2 - 6, tabanY), (ox, tabanY - yuk), (ox + en / 2 + 6, tabanY)], renk)
    t.cokgen([(ox, tabanY - yuk), (ox + en / 2 + 6, tabanY), (ox + en / 2 + 6, tabanY + 4),
              (ox, tabanY - yuk + 4)], karistir(renk, "cati_koyu", 0.6))


def ev() -> Image.Image:
    en, boy = 192, 176
    t = Tuval(en, boy)
    ox, dip = en / 2, boy - 8
    t.dikdortgen([ox - 56, dip - 74, ox + 56, dip], "ahsap")
    t.dikdortgen([ox - 56, dip - 74, ox - 10, dip], karistir("ahsap", "ahsap_acik", 0.5))
    _cati(t, ox, dip - 74, 112, 52, "cati")
    # kapi
    t.dikdortgen([ox - 16, dip - 40, ox + 14, dip], "ahsap_koyu", yaricap=6)
    t.elips([ox + 6, dip - 22, ox + 11, dip - 17], "bugday_acik")
    # pencereler: ic isik sicakligi
    for px in (ox - 40, ox + 30):
        t.dikdortgen([px, dip - 60, px + 24, dip - 38], karistir("bugday", "bugday_acik", 0.5), yaricap=4)
        t.cizgi([(px + 12, dip - 60), (px + 12, dip - 38)], "ahsap_koyu", kalinlik=1.5)
    # baca
    t.dikdortgen([ox + 24, dip - 132, ox + 40, dip - 100], karistir("cati", "cati_koyu", 0.4))
    return t.bitir(isik=0.42, tohum=41, merkez=(ox - 20, dip - 90))


def kuyu() -> Image.Image:
    en, boy = 112, 128
    t = Tuval(en, boy)
    ox, dip = en / 2, boy - 8
    t.elips([ox - 34, dip - 44, ox + 34, dip], "tas_koyu")
    t.elips([ox - 34, dip - 56, ox + 34, dip - 22], "tas")
    t.elips([ox - 26, dip - 51, ox + 26, dip - 29], karistir("su", "su_acik", 0.4))
    for px, renk in ((ox - 28, "ahsap"), (ox + 22, "ahsap_koyu")):
        t.dikdortgen([px, dip - 108, px + 8, dip - 44], renk)
    _cati(t, ox, dip - 100, 76, 34, "cati")
    t.dikdortgen([ox - 8, dip - 84, ox + 8, dip - 74], "ahsap_koyu", yaricap=3)
    return t.bitir(isik=0.5, tohum=42, merkez=(ox + 10, dip - 70))


def depo() -> Image.Image:
    en, boy = 128, 112
    t = Tuval(en, boy)
    ox, dip = en / 2, boy - 8
    t.dikdortgen([ox - 44, dip - 52, ox + 44, dip], "ahsap", yaricap=4)
    t.dikdortgen([ox - 44, dip - 52, ox - 4, dip], karistir("ahsap", "ahsap_acik", 0.45), yaricap=4)
    _cati(t, ox, dip - 52, 88, 36, "yaprak_koyu")
    # iki renkli kilit: ciftin ortak sandigi (B.10)
    t.dikdortgen([ox - 20, dip - 34, ox - 2, dip - 8], (217, 105, 74), yaricap=3)
    t.dikdortgen([ox + 2, dip - 34, ox + 20, dip - 8], (74, 127, 181), yaricap=3)
    return t.bitir(isik=0.45, tohum=43, merkez=(ox - 14, dip - 40))


def cit_parcasi() -> Image.Image:
    en, boy = 128, 64
    t = Tuval(en, boy)
    dip = boy - 6
    for px in (6, en / 2 - 5, en - 18):
        t.dikdortgen([px, dip - 42, px + 12, dip], "ahsap", yaricap=3)
    t.dikdortgen([0, dip - 34, en, dip - 26], "ahsap_koyu", yaricap=2)
    t.dikdortgen([0, dip - 20, en, dip - 12], "ahsap_koyu", yaricap=2)
    return t.bitir(isik=0.4, tohum=44)


def yapilar() -> None:
    yaz("ev", ev(), pivot=(0.5, 0.05))
    yaz("kuyu", kuyu(), pivot=(0.5, 0.06))
    yaz("depo", depo(), pivot=(0.5, 0.07))
    yaz("cit", cit_parcasi(), pivot=(0.5, 0.09))


# ---------------------------------------------------------------- karakter


#: Misafir hesabin 4 rengi (turetilmis.json ile ayni).
OYUNCU_RENKLERI = [(217, 105, 74), (94, 140, 85), (74, 127, 181), (217, 164, 65)]


def karakter(renk, tohum=0) -> Image.Image:
    en, boy = 72, 112
    t = Tuval(en, boy)
    ox, dip = en / 2, boy - 6

    # bacaklar
    t.dikdortgen([ox - 13, dip - 22, ox - 2, dip], "ahsap_koyu", yaricap=4)
    t.dikdortgen([ox + 2, dip - 22, ox + 13, dip], "ahsap_koyu", yaricap=4)
    # govde
    t.dikdortgen([ox - 18, dip - 56, ox + 18, dip - 18], renk, yaricap=8)
    t.dikdortgen([ox - 18, dip - 56, ox - 2, dip - 18], karistir(renk, (255, 255, 255), 0.18), yaricap=8)
    # kollar
    t.dikdortgen([ox - 24, dip - 52, ox - 15, dip - 28], karistir(renk, (0, 0, 0), 0.15), yaricap=4)
    t.dikdortgen([ox + 15, dip - 52, ox + 24, dip - 28], karistir(renk, (0, 0, 0), 0.15), yaricap=4)
    # kafa
    t.elips([ox - 16, dip - 84, ox + 16, dip - 52], "ten")
    # sapka: siluetin okunmasini saglayan sey
    t.elips([ox - 22, dip - 92, ox + 22, dip - 74], karistir("bugday", "ahsap", 0.35))
    t.elips([ox - 13, dip - 98, ox + 13, dip - 80], karistir("bugday", "bugday_acik", 0.4))
    return t.bitir(isik=0.5, tohum=tohum, merkez=(ox - 6, dip - 66))


def karakterler() -> None:
    for i, r in enumerate(OYUNCU_RENKLERI):
        yaz(f"karakter_{i}", karakter(r, i), pivot=(0.5, 0.05))


# ---------------------------------------------------------------- zemin yamalari


def zemin_yamasi(tohum=0, ton=0.0) -> Image.Image:
    """
    Zemine serilen boyali cim yamasi.

    ⚠ Kenari YUMUSAK olmali. Once mesh disk kullaniliyordu; keskin kenar
    ortografik kamerada "doku" degil "hata" gibi okunuyordu. Alfa kenari
    sonen bir yama, zeminin uzerine boyanmis gibi durur.
    """
    from PIL import ImageFilter
    en = boy = 256
    t = Tuval(en, boy)
    rng = np.random.default_rng(700 + tohum)

    taban = karistir("cim", "cim_acik" if ton >= 0 else "cim_koyu", abs(ton))
    # Duzensiz kume: tek daire "leke" gibi durur, kume organik durur.
    # ⚠ Leke tuvalin KENARINA DEGMEMELI. Ilk denemede m+r en fazla 0.64*en'e
    # ulasiyordu; merkezden sinira 0.5*en var, yani lekeler kirpiliyor ve
    # bulanik kenar kare bir sinir birakiyordu — zeminde saydam dikdortgenler
    # goruldu. Toplam yaricap 0.36*en ile sinirlandi, bulaniga pay birakildi.
    for i in range(9):
        a = (i / 9) * math.tau + rng.uniform(-0.3, 0.3)
        m = rng.uniform(0.06, 0.17) * en
        r = rng.uniform(0.12, 0.19) * en
        cx = en / 2 + math.cos(a) * m
        cy = boy / 2 + math.sin(a) * m * 0.62
        t.elips([cx - r, cy - r * 0.62, cx + r, cy + r * 0.62],
                karistir(taban, "cim_acik", rng.uniform(0.0, 0.30)))

    img = t.bitir(isik=0.22, kenar=0.0, doku=0.075, tohum=tohum)
    # Kenari sondur: yama zemine karissin, uzerine yapistirilmasin.
    a = np.asarray(img).astype(np.float32)
    alfa = Image.fromarray(a[..., 3].astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(radius=20))
    a[..., 3] = (np.asarray(alfa, np.float32) * 0.42).clip(0, 255)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def zemin_yamalari() -> None:
    for i in range(4):
        yaz(f"zemin_yama_{i}", zemin_yamasi(i, -0.5 + i * 0.34), pivot=(0.5, 0.5))


# ---------------------------------------------------------------- ana


def main() -> None:
    _kayit.clear()
    karolar()
    zemin_yamalari()
    urunler()
    dogal()
    yapilar()
    karakterler()
    yaz("golge", golge_sprite(96, 44), pivot=(0.5, 0.5))

    os.makedirs(CIKTI, exist_ok=True)
    with open(os.path.join(CIKTI, "sprite.json"), "w", encoding="utf-8") as f:
        json.dump(
            {"karoEn": KARO_EN, "karoBoy": KARO_BOY, "karoKalinlik": KARO_KALINLIK,
             "sprite": _kayit},
            f, ensure_ascii=False, indent=1,
        )

    print(f"{len(_kayit)} sprite yazildi -> {CIKTI}")


if __name__ == "__main__":
    main()
