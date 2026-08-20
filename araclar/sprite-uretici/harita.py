"""
Amorland vadi haritasi — izometrik diorama, katman katman PNG.

Spec B.4: "Ustten bakisli bir vadi haritasi. Bolgeye dokunursun, o sahneye
girersin." 7 bolge: Ev, Tarla (Sv1), Depo (Sv2), Tren gari (Sv5), Market (Sv6),
Orman (Sv7), Maden ocagi (Sv9).

⚠ Harita STATIKTIR: uzerinde yurunmez. Bolgeye dokununca o bolgeye yaklasilir ve
hareket ORADA olur. Bu bir tasarim tercihi degil performans karari — tek karede
butun vadiyi canli tutmak mobilde kasar. Bu yuzden harita, Unity'de tek tek
sprite olarak dizilen ONCEDEN CIZILMIS katmanlardir.

Her bolge AYRI katman olarak yazilir: Unity kilitli bolgeyi soluklastirabilsin,
secileni parlatabilsin diye. Tek buyuk PNG olsaydi bunlarin hicbiri yapilamazdi.

Cikti: istemci/Assets/Amorland/Resources/harita/*.png + harita.json
"""

from __future__ import annotations

import json
import math
import os

import numpy as np
from PIL import Image, ImageFilter

from uslup import SS, Tuval, karistir, yaprak_kumesi

KOK = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CIKTI = os.path.join(KOK, "istemci", "Assets", "Amorland", "Resources", "harita")

# ---------------------------------------------------------------- izdusum

#: Izometrik karo olculeri (piksel). 2:1 klasik dimetrik.
TW, TH = 66.0, 33.0
#: Bir birim yukseklik kac piksel yukari.
HH = 30.0

#: Harita tuvali. Referans 2000x1200 civari; 2x cizip kucultmuyoruz (Tuval zaten SS yapar).
EN, BOY = 1920, 1080
#: Izgara merkezinin ekrandaki yeri.
#
# ⚠ OY, adanin DIKEY ORTASI olacak sekilde secilir. Ilk denemede 300'du ve ada
# tuvalin ust yarisinda kaliyor, sag alt yari bombos duruyordu. Ada -13..13
# araliginda; iso'da dikey yayilim (x+z)*TH/2 = +-429 px, artı kalinlik payi.
OX, OY = EN / 2, 512.0


def iso(x: float, z: float, h: float = 0.0) -> tuple[float, float]:
    """Izgara koordinatini ekrana yansitir."""
    return (OX + (x - z) * TW / 2.0, OY + (x + z) * TH / 2.0 - h * HH)


_kayit: dict[str, dict] = {}


def yaz(ad: str, img: Image.Image, oncelik: int = 0, bolge: str | None = None,
        tiklama: list | None = None) -> None:
    os.makedirs(CIKTI, exist_ok=True)
    img.save(os.path.join(CIKTI, ad + ".png"))
    _kayit[ad] = {"en": img.width, "boy": img.height, "oncelik": oncelik}
    if bolge:
        _kayit[ad]["bolge"] = bolge
    if tiklama:
        _kayit[ad]["tiklama"] = tiklama


# ---------------------------------------------------------------- parcalar


def plato(t: Tuval, x0, z0, x1, z1, h, ust, yan_sol=None, yan_sag=None, kalinlik=None):
    """
    Yukseltilmis arazi parcasi: ust yuz + iki yan kesit.

    ⚠ Yan kesit haritanin belkemigi: referansta arazinin "maket" hissi,
    toprak kesitinin GORUNUR olmasindan geliyor. Kesitsiz plato, zemine
    boyanmis bir leke gibi durur.
    """
    # ⚠ Kesit yuksekligi platonun KENDI yuksekligine esit olmali. Sabit bir
    # kalinlik verilince ucurum zeminin ALTINA iniyor ve kesit, havada duran
    # bir cubuk gibi gorunuyor — ilk uretimde haritanin en bozuk yani buydu.
    if kalinlik is None:
        kalinlik = max(0.35, h)

    # ⚠ Plato USTU zeminden ACIK olmali. Ilk uretimde ikisi de ayni yesildi ve
    # yukseklik yalnizca koyu bir serit olarak okunuyordu — kesitler havada duran
    # cubuklar gibi gorunuyordu. Yukseklik arttikca ust yuz isiga yaklasir.
    ust = karistir(ust, (238, 246, 214), min(0.34, 0.10 + h * 0.13))
    yan_sol = yan_sol or karistir(ust, (90, 66, 44), 0.58)
    yan_sag = yan_sag or karistir(ust, (72, 52, 34), 0.72)

    a = iso(x0, z0, h)
    b = iso(x1, z0, h)
    c = iso(x1, z1, h)
    d = iso(x0, z1, h)
    dk = kalinlik * HH

    # sol yan (x0 kenari) ve on yan (z1 kenari)
    t.cokgen([d, c, (c[0], c[1] + dk), (d[0], d[1] + dk)], yan_sag)
    t.cokgen([a, d, (d[0], d[1] + dk), (a[0], a[1] + dk)], yan_sol)
    # ust
    t.cokgen([a, b, c, d], ust)


def agac_ciz(t: Tuval, x, z, h=0.0, olcek=1.0, tohum=0, koyu=False):
    sx, sy = iso(x, z, h)
    govde = 12 * olcek
    t.cokgen([(sx - 2.5 * olcek, sy), (sx + 2.5 * olcek, sy),
              (sx + 2 * olcek, sy - govde), (sx - 2 * olcek, sy - govde)], (96, 70, 46))
    ic = "yaprak_koyu" if koyu else "yaprak_acik"
    dis = (52, 92, 44) if koyu else "yaprak"
    yaprak_kumesi(t, sx, sy - govde - 9 * olcek, 13 * olcek, ic, dis, tohum, adet=6)


def cam_ciz(t: Tuval, x, z, h=0.0, olcek=1.0):
    """Ibreli agac: orman bolgesinin silueti yapraklidan AYRI okunmali."""
    sx, sy = iso(x, z, h)
    t.cokgen([(sx - 2, sy), (sx + 2, sy), (sx + 2, sy - 8 * olcek), (sx - 2, sy - 8 * olcek)],
             (88, 62, 40))
    for i, (w, yb, yt) in enumerate([(13, 6, 26), (10, 18, 36), (7, 29, 45)]):
        t.cokgen([(sx - w * olcek, sy - yb * olcek), (sx + w * olcek, sy - yb * olcek),
                  (sx, sy - yt * olcek)],
                 karistir((58, 100, 52), (86, 138, 74), 0.25 + i * 0.28))


def kaya_ciz(t: Tuval, x, z, h=0.0, olcek=1.0, tohum=0):
    sx, sy = iso(x, z, h)
    rng = np.random.default_rng(400 + tohum)
    n = []
    for i in range(6):
        a = (i / 6) * math.tau
        r = 13 * olcek * rng.uniform(0.7, 1.0)
        n.append((sx + math.cos(a) * r, sy + math.sin(a) * r * 0.55 - 5 * olcek))
    t.cokgen(n, (150, 150, 148))
    t.cokgen([(sx - 5 * olcek, sy - 11 * olcek), (sx + 3 * olcek, sy - 13 * olcek),
              (sx + 7 * olcek, sy - 7 * olcek), (sx - 1 * olcek, sy - 5 * olcek)], (184, 184, 180))


def bina_ciz(t: Tuval, x, z, h, en, boy, yuk, duvar, cati, kapi=True):
    """Izometrik ev: iki duvar yuzu + iki cati egimi."""
    a = iso(x, z, h)
    b = iso(x + en, z, h)
    c = iso(x + en, z + boy, h)
    d = iso(x, z + boy, h)
    dy = yuk * HH

    ua = (a[0], a[1] - dy)
    ub = (b[0], b[1] - dy)
    uc = (c[0], c[1] - dy)
    ud = (d[0], d[1] - dy)

    t.cokgen([d, c, (c[0], c[1] - dy), (d[0], d[1] - dy)], karistir(duvar, (255, 255, 255), 0.14))
    t.cokgen([a, d, ud, ua], karistir(duvar, (0, 0, 0), 0.22))

    tepe = ((ua[0] + uc[0]) / 2, min(ua[1], uc[1]) - yuk * HH * 0.55)
    t.cokgen([ud, uc, tepe], karistir(cati, (255, 255, 255), 0.12))
    t.cokgen([ua, ud, tepe], karistir(cati, (0, 0, 0), 0.18))
    t.cokgen([ub, uc, tepe], karistir(cati, (0, 0, 0), 0.30))

    if kapi:
        kx = (d[0] + c[0]) / 2
        ky = (d[1] + c[1]) / 2
        t.dikdortgen([kx - 5, ky - dy * 0.62, kx + 5, ky - 2], (78, 54, 34), yaricap=2)
        t.dikdortgen([kx - 20, ky - dy * 0.86, kx - 11, ky - dy * 0.58], (232, 194, 108), yaricap=2)


# ---------------------------------------------------------------- katmanlar


def zemin_katmani() -> Image.Image:
    """Ana ada: cim ustu + toprak kesiti + gol."""
    t = Tuval(EN, BOY)

    # Ada govdesi: duzensiz kenarli buyuk plato
    plato(t, -13, -11, 13, 11, 0, (126, 168, 92), kalinlik=2.4)

    # Uzak yariyi hafif koyulastir: derinlik
    a = iso(-13, -11, 0); b = iso(13, -11, 0); c = iso(13, 0, 0); d = iso(-13, 0, 0)
    t.cokgen([a, b, c, d], (116, 156, 86))

    # Nehir: adayi ikiye bolerek onden denize akar
    nehir = []
    for i in range(24):
        p = i / 23.0
        x = -6 + p * 9 + math.sin(p * 5.2) * 1.6
        z = -11 + p * 22
        nehir.append(iso(x, z, 0.02))
    geri = []
    for i in range(23, -1, -1):
        p = i / 23.0
        x = -6 + p * 9 + math.sin(p * 5.2) * 1.6 + 1.9
        z = -11 + p * 22
        geri.append(iso(x, z, 0.02))
    t.cokgen(nehir + geri, (110, 168, 196))
    t.cokgen([(px, py + 3) for px, py in nehir] + [(px, py + 3) for px, py in geri],
             (138, 190, 214))

    return t.bitir(isik=0.16, kenar=0.10, doku=0.05, tohum=1)


def orman_katmani() -> Image.Image:
    """ORMAN (Sv 7) — sol tarafta yukseltilmis camlik."""
    t = Tuval(EN, BOY)
    plato(t, -12.5, -9, -4.5, 2, 0.9, (110, 152, 84))
    rng = np.random.default_rng(11)
    for i in range(46):
        x = -12.2 + rng.uniform(0, 7.4)
        z = -8.7 + rng.uniform(0, 10.4)
        cam_ciz(t, x, z, 0.9, rng.uniform(0.75, 1.25))
    for i in range(8):
        kaya_ciz(t, -12 + rng.uniform(0, 7), -8.5 + rng.uniform(0, 10), 0.9, rng.uniform(0.6, 1.0), i)
    # oduncu kulubesi: bolgenin ISLEVI okunmali
    bina_ciz(t, -8.6, -1.4, 0.9, 2.0, 1.8, 1.1, (150, 111, 74), (140, 92, 62))
    return t.bitir(isik=0.30, kenar=0.14, doku=0.05, tohum=2)


def maden_katmani() -> Image.Image:
    """MADEN OCAGI (Sv 9) — sag arkada dag ve tunel agzi."""
    t = Tuval(EN, BOY)
    plato(t, 5, -10, 12.5, -2, 1.7, (118, 158, 88))

    # dag kutleleri
    for (bx, bz, o) in [(6.6, -8.6, 1.35), (9.0, -9.4, 1.0), (10.6, -7.0, 1.15)]:
        sx, sy = iso(bx, bz, 1.7)
        w, hh = 74 * o, 118 * o
        t.cokgen([(sx - w, sy), (sx, sy - hh), (sx + w, sy)], (150, 150, 148))
        t.cokgen([(sx, sy - hh), (sx + w, sy), (sx + w * 0.42, sy - hh * 0.22)], (112, 112, 110))
        t.cokgen([(sx, sy - hh), (sx - w * 0.28, sy - hh * 0.44), (sx + w * 0.22, sy - hh * 0.5)],
                 (226, 232, 236))

    # tunel agzi
    sx, sy = iso(8.4, -3.4, 1.7)
    t.cokgen([(sx - 30, sy), (sx + 30, sy), (sx + 26, sy - 40), (sx - 26, sy - 40)], (150, 150, 148))
    t.elips([sx - 20, sy - 46, sx + 20, sy - 6], (44, 36, 30))
    t.dikdortgen([sx - 26, sy - 46, sx - 17, sy], (128, 92, 60))
    t.dikdortgen([sx + 17, sy - 46, sx + 26, sy], (108, 76, 50))
    t.dikdortgen([sx - 30, sy - 54, sx + 30, sy - 44], (128, 92, 60), yaricap=3)
    return t.bitir(isik=0.34, kenar=0.14, doku=0.05, tohum=3)


def tren_katmani() -> Image.Image:
    """TREN GARI (Sv 5) — arkada peron ve adadan cikan ray."""
    t = Tuval(EN, BOY)
    plato(t, -3, -10.5, 4.5, -5.5, 0.7, (124, 164, 90))

    # ray: adanin disina uzanir
    for i in range(30):
        p = i / 29.0
        x = -3.2 + p * 15
        z = -9.4 + p * 1.0
        sx, sy = iso(x, z, 0.7)
        t.dikdortgen([sx - 16, sy - 3, sx + 16, sy + 3], (96, 84, 74))
        if i % 2 == 0:
            t.dikdortgen([sx - 5, sy - 8, sx + 5, sy + 8], (104, 76, 50), yaricap=1)

    bina_ciz(t, -1.6, -8.2, 0.7, 2.6, 2.0, 1.35, (168, 128, 88), (186, 96, 78))
    # vagon
    sx, sy = iso(2.6, -8.9, 0.7)
    t.dikdortgen([sx - 34, sy - 34, sx + 34, sy - 4], (94, 140, 85), yaricap=4)
    t.dikdortgen([sx - 34, sy - 34, sx + 6, sy - 4], (112, 160, 100), yaricap=4)
    return t.bitir(isik=0.32, kenar=0.14, doku=0.05, tohum=4)


def market_katmani() -> Image.Image:
    """MARKET (Sv 6) — sag onde kasaba kumesi."""
    t = Tuval(EN, BOY)
    plato(t, 4.5, 1.5, 12, 8.5, 0.5, (128, 170, 94))

    binalar = [
        (5.4, 2.4, 2.1, 1.7, 1.25, (170, 130, 90), (186, 96, 78)),
        (8.0, 2.0, 1.7, 1.5, 1.05, (150, 111, 74), (140, 92, 62)),
        (6.2, 5.0, 1.9, 1.6, 1.15, (178, 142, 100), (166, 88, 72)),
        (9.2, 4.6, 2.3, 1.8, 1.35, (160, 120, 82), (196, 108, 84)),
        (10.2, 7.0, 1.6, 1.4, 0.95, (172, 134, 92), (150, 84, 66)),
    ]
    for (x, z, en, boy, yuk, d, c) in binalar:
        bina_ciz(t, x, z, 0.5, en, boy, yuk, d, c)

    # pazar tezgahi: "market" oldugu buradan okunur
    sx, sy = iso(7.6, 6.6, 0.5)
    t.dikdortgen([sx - 40, sy - 10, sx + 40, sy - 2], (140, 100, 66), yaricap=2)
    for i, cx in enumerate((-28, -4, 20)):
        t.cokgen([(sx + cx - 14, sy - 10), (sx + cx + 14, sy - 10), (sx + cx, sy - 40)],
                 (217, 105, 74) if i % 2 == 0 else (238, 226, 206))
    rng = np.random.default_rng(21)
    for i in range(9):
        agac_ciz(t, 4.9 + rng.uniform(0, 7), 1.8 + rng.uniform(0, 6.4), 0.5,
                 rng.uniform(0.7, 1.0), 60 + i)
    return t.bitir(isik=0.32, kenar=0.14, doku=0.05, tohum=5)


def ev_katmani() -> Image.Image:
    """EV (Sv 1) — vadinin kalbi, en buyuk ve en sicak yapi."""
    t = Tuval(EN, BOY)
    plato(t, -3.5, 2.0, 2.5, 7.5, 0.45, (132, 174, 96))

    bina_ciz(t, -2.4, 3.2, 0.45, 3.0, 2.4, 1.7, (186, 146, 102), (196, 104, 84))
    # baca
    sx, sy = iso(0.2, 3.6, 0.45)
    t.dikdortgen([sx - 8, sy - 116, sx + 8, sy - 74], (168, 96, 76), yaricap=2)
    # degirmen carki: evin islevlerinden (B.4)
    sx, sy = iso(1.9, 6.2, 0.45)
    t.elips([sx - 26, sy - 52, sx + 26, sy], (0, 0, 0, 0))
    t.cizgi([(sx - 24, sy - 26), (sx + 24, sy - 26)], (110, 78, 50), kalinlik=3)
    t.cizgi([(sx, sy - 50), (sx, sy - 2)], (110, 78, 50), kalinlik=3)
    for a in (0.8, 2.4, 3.9, 5.5):
        t.cizgi([(sx, sy - 26), (sx + math.cos(a) * 24, sy - 26 + math.sin(a) * 24)],
                (128, 92, 60), kalinlik=3)
    rng = np.random.default_rng(31)
    for i in range(6):
        agac_ciz(t, -3.2 + rng.uniform(0, 5.4), 2.4 + rng.uniform(0, 4.6), 0.45,
                 rng.uniform(0.75, 1.05), 80 + i)
    return t.bitir(isik=0.34, kenar=0.14, doku=0.05, tohum=6)


def tarla_katmani() -> Image.Image:
    """TARLA (Sv 1) — surulmus parseller, kuyu."""
    t = Tuval(EN, BOY)
    plato(t, -10, 2.5, -4, 8.5, 0.35, (150, 112, 76))

    # karo izgarasi
    for gx in range(5):
        for gz in range(5):
            x0 = -9.6 + gx * 1.1
            z0 = 3.0 + gz * 1.1
            ust = (172, 132, 92) if (gx + gz) % 2 == 0 else (140, 104, 70)
            a = iso(x0, z0, 0.36); b = iso(x0 + 0.95, z0, 0.36)
            c = iso(x0 + 0.95, z0 + 0.95, 0.36); d = iso(x0, z0 + 0.95, 0.36)
            t.cokgen([a, b, c, d], ust)
            # olgun ekin
            if (gx * 3 + gz) % 4 == 0:
                cx, cy = iso(x0 + 0.48, z0 + 0.48, 0.36)
                t.cokgen([(cx - 2, cy), (cx + 2, cy), (cx + 1, cy - 20), (cx - 1, cy - 20)],
                         (128, 168, 84))
                t.elips([cx - 7, cy - 30, cx + 7, cy - 16], (222, 186, 96))

    # kuyu
    sx, sy = iso(-4.9, 6.6, 0.35)
    t.elips([sx - 22, sy - 20, sx + 22, sy + 4], (120, 120, 118))
    t.elips([sx - 16, sy - 16, sx + 16, sy], (94, 148, 178))
    t.dikdortgen([sx - 18, sy - 62, sx - 12, sy - 14], (150, 111, 74))
    t.dikdortgen([sx + 12, sy - 62, sx + 18, sy - 14], (120, 88, 58))
    t.cokgen([(sx - 26, sy - 60), (sx, sy - 82), (sx + 26, sy - 60)], (196, 104, 84))
    return t.bitir(isik=0.32, kenar=0.14, doku=0.05, tohum=7)


def depo_katmani() -> Image.Image:
    """DEPO (Sv 2) — ciftin bulusma noktasi. Evin hemen yaninda, iki renkli kilit."""
    t = Tuval(EN, BOY)
    plato(t, 2.8, 3.4, 5.2, 6.2, 0.45, (136, 176, 100))
    bina_ciz(t, 3.2, 3.9, 0.45, 1.7, 1.5, 1.1, (150, 111, 74), (94, 140, 85))
    sx, sy = iso(4.05, 5.4, 0.45)
    t.dikdortgen([sx - 16, sy - 26, sx - 2, sy - 4], (217, 105, 74), yaricap=2)
    t.dikdortgen([sx + 2, sy - 26, sx + 16, sy - 4], (74, 127, 181), yaricap=2)
    return t.bitir(isik=0.34, kenar=0.14, doku=0.05, tohum=8)


# ---------------------------------------------------------------- ana


#: Bolge -> (katman fonksiyonu, acilis seviyesi, tiklama merkezi izgara koordinati)
BOLGELER = [
    ("zemin",  zemin_katmani,  0, None),
    ("orman",  orman_katmani,  7, (-8.5, -3.5)),
    ("maden",  maden_katmani,  9, (8.7, -6.0)),
    ("tren",   tren_katmani,   5, (0.7, -8.0)),
    ("market", market_katmani, 6, (8.2, 5.0)),
    ("tarla",  tarla_katmani,  1, (-7.0, 5.5)),
    ("ev",     ev_katmani,     1, (-0.5, 4.8)),
    ("depo",   depo_katmani,   2, (4.0, 4.8)),
]


def main() -> None:
    _kayit.clear()
    for i, (ad, fn, seviye, merkez) in enumerate(BOLGELER):
        img = fn()
        tik = None
        if merkez:
            sx, sy = iso(merkez[0], merkez[1], 0.6)
            tik = [round(sx, 1), round(sy, 1)]
        yaz("harita_" + ad, img, oncelik=i, bolge=ad, tiklama=tik)
        _kayit["harita_" + ad]["seviye"] = seviye
        print(f"  {ad:8s} sv{seviye}")

    os.makedirs(CIKTI, exist_ok=True)
    with open(os.path.join(CIKTI, "harita.json"), "w", encoding="utf-8") as f:
        json.dump({"en": EN, "boy": BOY, "katman": _kayit}, f, ensure_ascii=False, indent=1)
    print(f"{len(_kayit)} katman -> {CIKTI}")


if __name__ == "__main__":
    main()
