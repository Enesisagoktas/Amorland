"""
Uslup dogrulamasi: sete yayilmadan once tek karede birkac sprite uretir.

Amac guzel bir tablo degil, SORUYA cevap: bu boru hatti "dokulu, gradyanli el
cizimi" hissini veriyor mu, yoksa hala duz renk lekesi mi? Cevap hayirsa
uslup.py degisir, uret.py yazilmaz.
"""

from __future__ import annotations

import math
import os

from PIL import Image

from uslup import PALET, Tuval, golge_sprite, karistir, yaprak_kumesi


def agac(tohum=0, olcek=1.0) -> Image.Image:
    en, boy = 96, 128
    t = Tuval(en, boy)
    ox = en / 2

    # govde: alta dogru genisler, hafif egri
    t.cokgen(
        [(ox - 5, boy - 18), (ox + 5, boy - 18), (ox + 8, boy - 4), (ox - 8, boy - 4)],
        "ahsap_koyu",
    )
    t.cokgen([(ox - 4, boy - 46), (ox + 4, boy - 46), (ox + 5, boy - 16), (ox - 5, boy - 16)], "ahsap")

    # yaprak: iki kademe kume
    yaprak_kumesi(t, ox, boy - 62, 30 * olcek, "yaprak_acik", "yaprak_koyu", tohum, adet=8)
    yaprak_kumesi(t, ox + 2, boy - 86, 22 * olcek, "yaprak_acik", "yaprak", tohum + 7, adet=6)

    return t.bitir(isik=0.55, tohum=tohum, merkez=(ox, boy - 70))


def kaya(tohum=0) -> Image.Image:
    en, boy = 72, 56
    t = Tuval(en, boy)
    rng = __import__("numpy").random.default_rng(200 + tohum)
    ox, oy = en / 2, boy - 12
    n = 7
    noktalar = []
    for i in range(n):
        a = (i / n) * math.tau
        r = 26 * rng.uniform(0.72, 1.0)
        noktalar.append((ox + math.cos(a) * r, oy + math.sin(a) * r * 0.62))
    t.cokgen(noktalar, "tas")
    # ust yuzey: isik alan kucuk faseta
    t.cokgen([(ox - 10, oy - 14), (ox + 6, oy - 18), (ox + 14, oy - 8), (ox - 2, oy - 4)], "tas_acik")
    return t.bitir(isik=0.6, tohum=tohum, merkez=(ox + 6, oy - 14))


def bugday(asama: int, asama_sayisi: int = 4) -> Image.Image:
    """
    Buyume asamasi. ⚠ Spec kurali: seriler AYNI TABAN gorselden turetilir,
    sifirdan uretilmez — yoksa 4 asama 4 farkli bitki gibi durur.
    """
    en, boy = 64, 96
    t = Tuval(en, boy)
    ox = en / 2
    oran = asama / max(1, asama_sayisi - 1)
    yukseklik = 20 + 54 * oran
    genislik = 5 + 7 * oran

    # sap
    t.cokgen(
        [
            (ox - genislik * 0.4, boy - 6),
            (ox + genislik * 0.4, boy - 6),
            (ox + genislik * 0.25, boy - 6 - yukseklik),
            (ox - genislik * 0.25, boy - 6 - yukseklik),
        ],
        "sap",
    )
    # yapraklar: asamayla sayisi artar
    for i in range(2 + asama * 2):
        yon = -1 if i % 2 else 1
        h = boy - 12 - yukseklik * (0.35 + 0.16 * i)
        uc = (ox + yon * (10 + 9 * oran), h - 10 - 7 * oran)
        t.cokgen([(ox, h + 5), (ox, h - 5), uc], karistir("yaprak", "yaprak_acik", min(0.9, 0.25 + 0.16 * i)))
    # basak: yalnizca olgunda
    if asama >= asama_sayisi - 1:
        tepe = boy - 6 - yukseklik
        for i in range(7):
            y = tepe + i * 5
            g = 11 - i * 0.7
            t.elips([ox - g, y - 5, ox + g, y + 6], karistir("bugday", "bugday_acik", 1 - i / 7))
    return t.bitir(isik=0.5, tohum=asama, merkez=(ox, boy - 40))


def _karo(taban, ic, izler=False, tohum=0, kalinlik=16) -> Image.Image:
    """
    Kalinlikli 2.5D karo: ust elmas + iki yan yuz.

    ⚠ Kalinlik sus degil. Kalinliksiz elmas, zemine yapistirilmis bir cikartma
    gibi duruyor; yan yuzler karoyu bir HACIM yapiyor ve referanstaki plato
    hissi buradan geliyor.
    """
    en, boy = 128, 74 + kalinlik
    t = Tuval(en, boy)
    uy = 2                      # ust elmasin tepe y'si
    oy = 74 - 2                 # ust elmasin dip y'si
    sol, sag = 2, en - 2
    ort = en / 2

    ust = [(ort, uy), (sag, (uy + oy) / 2), (ort, oy), (sol, (uy + oy) / 2)]

    # yan yuzler once: ust yuzun altinda kalsinlar
    t.cokgen([(sol, (uy + oy) / 2), (ort, oy), (ort, oy + kalinlik), (sol, (uy + oy) / 2 + kalinlik)],
             karistir(taban, "toprak_koyu", 0.55))
    t.cokgen([(ort, oy), (sag, (uy + oy) / 2), (sag, (uy + oy) / 2 + kalinlik), (ort, oy + kalinlik)],
             karistir(taban, "toprak_koyu", 0.72))

    # ust yuz
    t.cokgen(ust, taban)
    t.cokgen([(ort, uy + 10), (sag - 12, (uy + oy) / 2), (ort, oy - 10), (sol + 12, (uy + oy) / 2)], ic)

    if izler:
        # ⚠ Izler AYRI tuvale cizilip ust elmasa kirpilir. Ayni tuvale cizilseydi
        # cizgi kalinligi yuzunden karonun disina tasardi — ilk denemede tam bunu
        # yapti ve karolar kirik gorundu.
        iz = Tuval(en, boy)
        for i in range(-4, 5):
            k = i * 10
            iz.cizgi([(ort - 40 + k, (uy + oy) / 2 + 20), (ort + 40 + k, (uy + oy) / 2 - 20)],
                     karistir(taban, "toprak_koyu", 0.5), kalinlik=1.5)
        iz.kirp(ust)
        t.yapistir(iz.img, 0, 0)

    return t.bitir(isik=0.34, kenar=0.2, tohum=tohum)


def cim_karo() -> Image.Image:
    return _karo("cim", karistir("cim", "cim_acik", 0.5), tohum=3)


def toprak_karo(sulu=False) -> Image.Image:
    taban = "toprak_islak" if sulu else "toprak"
    ic = karistir(taban, "toprak_acik", 0.18 if sulu else 0.42)
    return _karo(taban, ic, izler=True, tohum=5 + int(sulu))


def main() -> None:
    cikti = os.path.join(os.path.dirname(__file__), "deneme.png")

    parcalar = [
        ("cim karo", cim_karo()),
        ("toprak", toprak_karo(False)),
        ("sulanmis", toprak_karo(True)),
        ("agac 1", agac(1)),
        ("agac 2", agac(5, 0.86)),
        ("kaya", kaya(2)),
        ("bugday 0", bugday(0)),
        ("bugday 1", bugday(1)),
        ("bugday 2", bugday(2)),
        ("bugday 3", bugday(3)),
        ("golge", golge_sprite(64, 30)),
    ]

    hucre_en, hucre_boy = 150, 150
    sutun = 6
    satir = (len(parcalar) + sutun - 1) // sutun
    sayfa = Image.new("RGBA", (sutun * hucre_en, satir * hucre_boy), (232, 214, 188, 255))

    for i, (_, img) in enumerate(parcalar):
        sx = (i % sutun) * hucre_en + (hucre_en - img.width) // 2
        sy = (i // sutun) * hucre_boy + (hucre_boy - img.height) // 2
        sayfa.alpha_composite(img, (sx, sy))

    sayfa.save(cikti)
    print("yazildi:", cikti, sayfa.size)


if __name__ == "__main__":
    main()
