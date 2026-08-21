# -*- coding: utf-8 -*-
"""Amorland 64px ek karo sayfasi ureticisi.

rpg-base (Kenney CC0) omurga secildi ama sette ekin, hayvan, koylu, kopru,
kaldirim ve ray YOK. Bu betik eksikleri cizer ve tek sayfaya (ek64.png) dizer.

⚠ Renkler ELLE SECILMEZ — dogrudan RPGpack_sheet.png'nin bilinen karolarindan
ornekleme yapilir. Boylece cizilen her sey rpg-base'le ayni paletten cikar ve
"iki ayri oyundan gelmis" gorunumu olusmaz. rpg-base stili: kontursuz, yumusak
govde golgesi, ustten isik. Cizimler de oyle: duz sekil + altta koyu ton.

Cikti: istemci/Assets/Amorland/Resources/harita2d64/ek64.png (12x3 karo, 768x192)
Ayrica gozle kontrol icin 4x buyutulmus kontak tablosu: ek64-kontak.png
"""
import os
import sys

from PIL import Image, ImageDraw

KOK = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
KAYNAK = os.path.join(KOK, "varliklar", "kenney", "paket", "rpg-base", "Spritesheet", "RPGpack_sheet.png")
CIKTI = os.path.join(KOK, "istemci", "Assets", "Amorland", "Resources", "harita2d64", "ek64.png")
KONTAK = os.path.join(KOK, "araclar", "karo-indeks", "ek64-kontak.png")

T = 64
SUT, SAT = 12, 3


def ortalama(sheet, indeks, kutu=(16, 16, 48, 48)):
    """Bilinen bir rpg-base karosunun orta bolgesinin ortalama rengi."""
    x, y = (indeks % 20) * T, (indeks // 20) * T
    k = sheet.crop((x + kutu[0], y + kutu[1], x + kutu[2], y + kutu[3]))
    px = [p for p in k.getdata() if p[3] > 200]
    n = max(1, len(px))
    return tuple(sum(c[i] for c in px) // n for i in range(3))


def ton(renk, carpan):
    return tuple(max(0, min(255, int(c * carpan))) for c in renk)


class Cizer:
    def __init__(self):
        self.im = Image.new("RGBA", (SUT * T, SAT * T), (0, 0, 0, 0))
        sheet = Image.open(KAYNAK).convert("RGBA")
        # Palet: rpg-base'in kendisinden.
        self.CIM = ortalama(sheet, 43)          # duz cim
        self.TOPRAK = ortalama(sheet, 26)       # duz toprak
        self.AHSAP = ortalama(sheet, 246)       # cit ahsabi
        self.KREM = ortalama(sheet, 64)         # krem duvar
        self.TAS = ortalama(sheet, 91)          # gri tas duvar
        self.sheet = sheet

    def kanvas(self, i):
        return (i % SUT) * T, (i // SUT) * T

    def zemin(self, i, renk):
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        d.rectangle([x, y, x + T - 1, y + T - 1], fill=renk + (255,))
        return x, y, d

    def toprak_zemin(self, i):
        """rpg-base duz toprak karosunu (26) temel alir — birebir ayni doku."""
        x, y = self.kanvas(i)
        sx, sy = (26 % 20) * T, (26 // 20) * T
        self.im.paste(self.sheet.crop((sx, sy, sx + T, sy + T)), (x, y))
        return x, y, ImageDraw.Draw(self.im)

    # ------------------------------------------------------------ zeminler

    def kaldirim(self, i, tohum):
        """Koy meydani doseme tasi.

        ⚠ Ilk surum 16 px'lik minik taslardi; oyun yakinliginda meydan titresimli
        bir hali desenine donustu ve ekrani yedi. Simdi karo basina 2x2 BUYUK plaka:
        sakin, dusuk kontrast, krem-bej (koyun sicak paletine uyumlu, maviye kacmaz).
        """
        harc = ton(self.KREM, 0.66)
        x, y, d = self.zemin(i, harc)
        import random
        r = random.Random(tohum)
        for sy in range(2):
            for sx in range(2):
                px = x + sx * 32 + 1
                py = y + sy * 32 + 1
                taban = ton(self.KREM, 0.86 + r.random() * 0.10)
                d.rounded_rectangle([px, py, px + 29, py + 29], 6, fill=taban + (255,))
                d.rounded_rectangle([px + 2, py + 2, px + 27, py + 13], 5,
                                    fill=ton(taban, 1.05) + (255,))

    def surulu(self, i):
        """Surulmus tarla: toprak uzerine koyu saban izleri."""
        x, y, d = self.toprak_zemin(i)
        koyu = ton(self.TOPRAK, 0.72)
        acik = ton(self.TOPRAK, 1.10)
        for s in range(4):
            yy = y + 6 + s * 16
            d.rounded_rectangle([x + 2, yy, x + T - 3, yy + 7], 3, fill=koyu + (255,))
            d.rounded_rectangle([x + 2, yy + 8, x + T - 3, yy + 10], 2, fill=acik + (200,))

    def ray(self, i, dikey):
        """Tren rayi: toprak ustune traversler + iki celik ray."""
        x, y, d = self.toprak_zemin(i)
        travers = ton(self.AHSAP, 0.72)
        celik = ton(self.TAS, 1.12)
        celik_koyu = ton(self.TAS, 0.78)
        if not dikey:
            for s in range(4):
                xx = x + 4 + s * 16
                d.rounded_rectangle([xx, y + 10, xx + 8, y + T - 11], 2, fill=travers + (255,))
            for yy in (y + 18, y + T - 24):
                d.rectangle([x, yy + 4, x + T - 1, yy + 6], fill=celik_koyu + (255,))
                d.rectangle([x, yy, x + T - 1, yy + 4], fill=celik + (255,))
        else:
            for s in range(4):
                yy = y + 4 + s * 16
                d.rounded_rectangle([x + 10, yy, x + T - 11, yy + 8], 2, fill=travers + (255,))
            for xx in (x + 18, x + T - 24):
                d.rectangle([xx + 4, y, xx + 6, y + T - 1], fill=celik_koyu + (255,))
                d.rectangle([xx, y, xx + 4, y + T - 1], fill=celik + (255,))

    def kalas(self, i, dikey):
        """Kopru/iskele dosemesi: enine kalaslar."""
        x, y, d = self.zemin(i, ton(self.AHSAP, 0.80))
        for s in range(8):
            if not dikey:
                yy = y + s * 8
                renk = ton(self.AHSAP, 0.92 + (s % 3) * 0.07)
                d.rectangle([x, yy, x + T - 1, yy + 6], fill=renk + (255,))
            else:
                xx = x + s * 8
                renk = ton(self.AHSAP, 0.92 + (s % 3) * 0.07)
                d.rectangle([xx, y, xx + 6, y + T - 1], fill=renk + (255,))

    # ------------------------------------------------------------ ekinler

    def bugday(self, i, olgun):
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        renk = (222, 184, 92) if olgun else ton(self.CIM, 1.05)
        koyu = ton(renk, 0.80)
        import random
        r = random.Random(7 + i)
        boy = 34 if olgun else 18
        for s in range(7):
            xx = x + 7 + s * 8 + r.randint(-2, 2)
            taban = y + 56
            d.line([xx, taban, xx, taban - boy], fill=koyu + (255,), width=3)
            if olgun:
                d.rounded_rectangle([xx - 3, taban - boy - 8, xx + 3, taban - boy + 4],
                                    3, fill=renk + (255,))

    def yumru(self, i, olgun, meyve, yaprak=None):
        """Domates/balkabagi tipi: yesil govde + renkli meyveler."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        yesil = yaprak or ton(self.CIM, 0.88)
        import random
        r = random.Random(11 + i)
        for s in range(4):
            cx = x + 12 + (s % 2) * 28 + r.randint(-3, 3)
            cy = y + 22 + (s // 2) * 24 + r.randint(-2, 2)
            if olgun:
                d.ellipse([cx - 9, cy - 7, cx + 9, cy + 9], fill=ton(yesil, 0.9) + (255,))
                d.ellipse([cx - 7, cy - 2, cx + 7, cy + 11], fill=meyve + (255,))
                d.ellipse([cx - 5, cy, cx - 1, cy + 4], fill=ton(meyve, 1.25) + (255,))
            else:
                d.ellipse([cx - 6, cy - 2, cx + 6, cy + 8], fill=yesil + (255,))

    def havuc(self, i, olgun):
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        import random
        r = random.Random(23 + i)
        for s in range(6):
            cx = x + 9 + (s % 3) * 20 + r.randint(-2, 2)
            cy = y + 20 + (s // 3) * 26
            sacak = ton(self.CIM, 0.92)
            for k in (-4, 0, 4):
                d.line([cx, cy + 8, cx + k, cy - 6], fill=sacak + (255,), width=3)
            if olgun:
                d.polygon([(cx - 5, cy + 8), (cx + 5, cy + 8), (cx, cy + 18)],
                          fill=(232, 133, 58, 255))

    # ------------------------------------------------------------ canlilar

    def inek(self, i):
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        gv = (246, 240, 228)
        d.rounded_rectangle([x + 10, y + 22, x + 54, y + 50], 12, fill=gv + (255,))
        d.ellipse([x + 16, y + 26, x + 30, y + 40], fill=(151, 106, 74, 255))
        d.ellipse([x + 38, y + 36, x + 50, y + 46], fill=(151, 106, 74, 255))
        d.rounded_rectangle([x + 22, y + 12, x + 42, y + 30], 9, fill=gv + (255,))
        d.ellipse([x + 18, y + 10, x + 26, y + 18], fill=ton(gv, 0.92) + (255,))
        d.ellipse([x + 38, y + 10, x + 46, y + 18], fill=ton(gv, 0.92) + (255,))
        d.rounded_rectangle([x + 26, y + 22, x + 38, y + 30], 4, fill=(238, 190, 178, 255))
        d.ellipse([x + 26, y + 16, x + 30, y + 20], fill=(60, 48, 42, 255))
        d.ellipse([x + 34, y + 16, x + 38, y + 20], fill=(60, 48, 42, 255))
        for k in (14, 26, 38, 48):
            d.rectangle([x + k, y + 48, x + k + 5, y + 56], fill=ton(gv, 0.85) + (255,))

    def koyun(self, i):
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        yun = (240, 235, 220)
        import random
        r = random.Random(5)
        for s in range(9):
            cx = x + 16 + (s % 3) * 14 + r.randint(-2, 2)
            cy = y + 24 + (s // 3) * 9
            d.ellipse([cx - 9, cy - 8, cx + 9, cy + 8], fill=yun + (255,))
        d.rounded_rectangle([x + 24, y + 14, x + 40, y + 28], 7, fill=(88, 74, 66, 255))
        d.ellipse([x + 27, y + 18, x + 30, y + 21], fill=(240, 235, 220, 255))
        d.ellipse([x + 34, y + 18, x + 37, y + 21], fill=(240, 235, 220, 255))
        for k in (20, 40):
            d.rectangle([x + k, y + 48, x + k + 4, y + 56], fill=(88, 74, 66, 255))

    def tavuk(self, i):
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        gv = (246, 242, 232)
        d.ellipse([x + 20, y + 26, x + 44, y + 50], fill=gv + (255,))
        d.ellipse([x + 26, y + 14, x + 40, y + 30], fill=gv + (255,))
        d.polygon([(x + 29, y + 14), (x + 33, y + 8), (x + 37, y + 14)], fill=(216, 74, 60, 255))
        d.polygon([(x + 40, y + 20), (x + 47, y + 22), (x + 40, y + 25)], fill=(232, 160, 62, 255))
        d.ellipse([x + 35, y + 18, x + 39, y + 22], fill=(60, 48, 42, 255))
        d.ellipse([x + 22, y + 32, x + 34, y + 44], fill=ton(gv, 0.90) + (255,))
        for k in (26, 34):
            d.rectangle([x + k, y + 48, x + k + 3, y + 55], fill=(232, 160, 62, 255))

    def koylu(self, i, kadin):
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        ten = (238, 200, 164)
        sac = (122, 84, 56) if not kadin else (86, 58, 40)
        ust = (86, 124, 168) if not kadin else (176, 88, 96)
        alt = ton(self.AHSAP, 0.68)
        d.rounded_rectangle([x + 24, y + 30, x + 40, y + 48], 6, fill=ust + (255,))
        d.rectangle([x + 26, y + 46, x + 31, y + 58], fill=alt + (255,))
        d.rectangle([x + 33, y + 46, x + 38, y + 58], fill=alt + (255,))
        d.ellipse([x + 23, y + 12, x + 41, y + 32], fill=ten + (255,))
        if kadin:
            d.pieslice([x + 21, y + 8, x + 43, y + 34], 150, 390, fill=sac + (255,))
            d.ellipse([x + 23, y + 16, x + 41, y + 34], fill=ten + (255,))
        else:
            d.pieslice([x + 22, y + 9, x + 42, y + 29], 180, 360, fill=sac + (255,))
        d.ellipse([x + 28, y + 21, x + 31, y + 24], fill=(60, 48, 42, 255))
        d.ellipse([x + 33, y + 21, x + 36, y + 24], fill=(60, 48, 42, 255))

    def direk(self, i):
        """Tek cit diregi: dikey cit kenarlari icin (rpg-base yalniz yatay cit verir)."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        ahsap = ton(self.AHSAP, 0.88)
        d.ellipse([x + 25, y + 50, x + 39, y + 58], fill=ton(ahsap, 0.70) + (255,))
        d.rounded_rectangle([x + 27, y + 14, x + 37, y + 54], 4, fill=ahsap + (255,))
        d.rounded_rectangle([x + 27, y + 14, x + 37, y + 30], 4, fill=ton(ahsap, 1.12) + (255,))

    def lokomotif(self, i):
        """Yandan bakisli kucuk buharli lokomotif — doguya (saga) gider."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        govde = ton(self.TAS, 0.52)
        acik = ton(self.TAS, 0.72)
        # kazan
        d.rounded_rectangle([x + 4, y + 26, x + 42, y + 46], 7, fill=govde + (255,))
        d.rounded_rectangle([x + 4, y + 26, x + 42, y + 34], 7, fill=acik + (255,))
        # kabin (sagda)
        d.rounded_rectangle([x + 40, y + 14, x + 58, y + 46], 4, fill=govde + (255,))
        d.rectangle([x + 44, y + 20, x + 54, y + 30], fill=(140, 190, 205, 255))
        # baca + isik
        d.rectangle([x + 10, y + 14, x + 18, y + 28], fill=govde + (255,))
        d.rounded_rectangle([x + 8, y + 10, x + 20, y + 16], 3, fill=acik + (255,))
        # on tampon
        d.polygon([(x + 2, y + 46), (x + 10, y + 38), (x + 10, y + 46)], fill=ton(self.AHSAP, 0.8) + (255,))
        # tekerler
        for cx in (14, 30, 48):
            d.ellipse([x + cx - 7, y + 42, x + cx + 7, y + 56], fill=(52, 44, 46, 255))
            d.ellipse([x + cx - 3, y + 46, x + cx + 3, y + 52], fill=acik + (255,))

    def vagon(self, i):
        """Yuk vagonu: ahsap sandik govde + iki teker."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        ahsap = ton(self.AHSAP, 0.9)
        d.rounded_rectangle([x + 6, y + 24, x + 58, y + 48], 4, fill=ton(ahsap, 0.8) + (255,))
        for k in range(4):
            d.rectangle([x + 9 + k * 13, y + 27, x + 18 + k * 13, y + 45], fill=ahsap + (255,))
        d.rectangle([x + 6, y + 24, x + 58, y + 29], fill=ton(ahsap, 1.1) + (255,))
        for cx in (18, 46):
            d.ellipse([x + cx - 6, y + 44, x + cx + 6, y + 56], fill=(52, 44, 46, 255))
            d.ellipse([x + cx - 2, y + 48, x + cx + 2, y + 52], fill=ton(self.TAS, 0.72) + (255,))

    def isaret(self, i):
        """Yon tabelasi."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        d.rectangle([x + 29, y + 20, x + 35, y + 56], fill=ton(self.AHSAP, 0.75) + (255,))
        d.rounded_rectangle([x + 12, y + 12, x + 52, y + 30], 5, fill=self.AHSAP + (255,))
        d.rounded_rectangle([x + 12, y + 12, x + 52, y + 20], 5, fill=ton(self.AHSAP, 1.12) + (255,))


def main():
    c = Cizer()
    # Satir 0: zeminler
    c.kaldirim(0, 1)
    c.kaldirim(1, 2)
    c.surulu(2)
    c.ray(3, dikey=False)
    c.ray(4, dikey=True)
    c.kalas(5, dikey=False)
    c.kalas(6, dikey=True)
    c.isaret(7)
    c.direk(8)
    c.lokomotif(9)
    c.vagon(10)
    # Satir 1: ekinler (genc 12-15, olgun 16-19)
    c.bugday(12, olgun=False)
    c.yumru(13, olgun=False, meyve=(214, 70, 56))
    c.yumru(14, olgun=False, meyve=(230, 140, 50))
    c.havuc(15, olgun=False)
    c.bugday(16, olgun=True)
    c.yumru(17, olgun=True, meyve=(214, 70, 56))
    c.yumru(18, olgun=True, meyve=(230, 140, 50))
    c.havuc(19, olgun=True)
    # Satir 2: canlilar
    c.inek(24)
    c.koyun(25)
    c.tavuk(26)
    c.koylu(27, kadin=False)
    c.koylu(28, kadin=True)

    os.makedirs(os.path.dirname(CIKTI), exist_ok=True)
    c.im.save(CIKTI)

    # Kontak tablosu: 4x buyutulmus, koyu zeminde, indeks etiketli.
    O = 4
    kontak = Image.new("RGB", (SUT * T * O, SAT * T * O), (40, 40, 55))
    b = c.im.resize((SUT * T * O, SAT * T * O), Image.NEAREST)
    kontak.paste(Image.alpha_composite(
        Image.new("RGBA", b.size, (40, 40, 55, 255)), b).convert("RGB"), (0, 0))
    d = ImageDraw.Draw(kontak)
    for i in range(SUT * SAT):
        px, py = (i % SUT) * T * O, (i // SUT) * T * O
        d.rectangle([px, py, px + T * O - 1, py + T * O - 1], outline=(255, 0, 255))
        d.text((px + 4, py + 3), str(i), fill=(255, 255, 0))
    kontak.save(KONTAK)
    print("yazildi:", CIKTI)
    print("kontak :", KONTAK)


if __name__ == "__main__":
    sys.exit(main())
