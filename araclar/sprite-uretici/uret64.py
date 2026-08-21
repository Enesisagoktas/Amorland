# -*- coding: utf-8 -*-
"""Amorland 64px ek karo sayfasi ureticisi.

rpg-base (Kenney CC0) omurga secildi ama sette ekin, hayvan, koylu, kopru,
kaldirim ve ray YOK. Bu betik eksikleri cizer ve tek sayfaya (ek64.png) dizer.

⚠ Renkler ELLE SECILMEZ — dogrudan RPGpack_sheet.png'nin bilinen karolarindan
ornekleme yapilir. Boylece cizilen her sey rpg-base'le ayni paletten cikar ve
"iki ayri oyundan gelmis" gorunumu olusmaz. rpg-base stili: kontursuz, yumusak
govde golgesi, ustten isik. Cizimler de oyle: duz sekil + altta koyu ton.

Cikti: istemci/Assets/Amorland/Resources/harita2d64/ek64.png (12x8 karo, 768x512)
Ayrica gozle kontrol icin 4x buyutulmus kontak tablosu: ek64-kontak.png
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

KOK = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
KAYNAK = os.path.join(KOK, "varliklar", "kenney", "paket", "rpg-base", "Spritesheet", "RPGpack_sheet.png")
CIKTI = os.path.join(KOK, "istemci", "Assets", "Amorland", "Resources", "harita2d64", "ek64.png")
KONTAK = os.path.join(KOK, "araclar", "karo-indeks", "ek64-kontak.png")

T = 64
# ⚠ SAT artmadan yeni indeks EKLENMEZ: 12x6=72 karoyken koyu agaclar 71/72/73'e
# cizildi ve 72-73 tuvalin DISINA dustu — PIL sessizce kirpti, Unity'de sprite
# null donunce golge konup agac atlandi ("govdesiz golge" hatasi).
SUT, SAT = 12, 8


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

    def filiz(self, i):
        """Yeni ekilmis karo ustu: minik cift yaprakli filiz + tohum cukuru.

        Oyun parselinde 3 evre vardir: filiz -> genc urun -> olgun urun.
        Genc/olgun karolar (12-19) zaten var; bu, ekimin ANINDA gorunen ilk
        evresidir (iyimser istemci: dokundugun an bir sey degismeli)."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        yesil = (106, 178, 70)
        koyu = (74, 138, 52)
        d.ellipse([x + 22, y + 40, x + 42, y + 52], fill=ton(self.TOPRAK, 0.64) + (200,))
        d.rectangle([x + 30, y + 28, x + 33, y + 46], fill=koyu + (255,))
        d.ellipse([x + 17, y + 21, x + 31, y + 34], fill=yesil + (255,))
        d.ellipse([x + 33, y + 21, x + 47, y + 34], fill=yesil + (255,))
        d.ellipse([x + 20, y + 23, x + 27, y + 29], fill=(150, 210, 110, 255))
        d.ellipse([x + 36, y + 23, x + 43, y + 29], fill=(150, 210, 110, 255))

    def surulu_islak(self, i, kaynak=2):
        """Sulanmis surulu karo: kaynak karonun koyu-nemli TUREVI.

        ⚠ Yeniden cizim degil turev: karik deseni birebir ayni kalir, yalniz
        parlaklik duser ve hafif maviye kayar — kuru/islak farki desenden
        degil tondan okunur (ayni ders: cim_koyu_duz ailesi)."""
        import numpy as np
        sx, sy = self.kanvas(kaynak)
        k = self.im.crop((sx, sy, sx + T, sy + T))
        a = np.asarray(k).astype(np.float32)
        a[..., 0] *= 0.60
        a[..., 1] *= 0.65
        a[..., 2] *= 0.80
        x, y = self.kanvas(i)
        self.im.paste(Image.fromarray(a.clip(0, 255).astype("uint8"), "RGBA"), (x, y))

    def surulu(self, i):
        """Surulmus tarla — USTTEN bakis.

        ⚠ Ilk surum kalin yuvarlak barlardi; parseller cime yatirilmis ahsap palet
        gibi okundu (inceleme bulgusu). Ustten karik: zemin hafif koyu toprak, ince
        acik cizgiler + tek tuk topak. Derinlik cizgiden degil dokudan gelir.
        """
        x, y, d = self.toprak_zemin(i)
        d.rectangle([x, y, x + T - 1, y + T - 1], fill=ton(self.TOPRAK, 0.86) + (235,))
        koyu = ton(self.TOPRAK, 0.70)
        acik = ton(self.TOPRAK, 1.08)
        for s in range(4):
            yy = y + 8 + s * 16
            d.line([x, yy, x + T - 1, yy], fill=koyu + (255,), width=3)
            d.line([x, yy + 4, x + T - 1, yy + 4], fill=ton(acik, 0.96) + (255,), width=2)
        import random
        r = random.Random(i * 7 + 3)
        for s in range(5):
            cx, cy = x + r.randint(4, 58), y + r.randint(2, 60)
            d.ellipse([cx, cy, cx + 3, cy + 2], fill=ton(self.TOPRAK, 0.62) + (255,))

    def ray(self, i, dikey):
        """Tren rayi. Denetim: balast entegre — traverslerin altinda mat cakil
        bandi, ray artik ciplak toprakta yuzmuyor."""
        x, y, d = self.toprak_zemin(i)
        import random
        r = random.Random(3 + i)
        travers = ton(self.AHSAP, 0.72)
        celik = ton(self.TAS, 1.12)
        celik_koyu = ton(self.TAS, 0.78)
        # balast bandi
        for s_ in range(26):
            cx = x + r.randint(0, 60)
            cy = (y + 12 + r.randint(0, 40)) if not dikey else (y + r.randint(0, 60))
            bx = (x + 12 + r.randint(0, 40)) if dikey else cx
            w = 3 + r.randint(0, 3)
            taban = ton((176, 158, 138), 0.8 + r.random() * 0.2)
            if dikey:
                by = y + r.randint(0, 58)
                d.ellipse([bx, by, bx + w, by + w - 1], fill=taban + (255,))
            else:
                d.ellipse([cx, cy, cx + w, cy + w - 1], fill=taban + (255,))
        if not dikey:
            for s_ in range(4):
                xx = x + 4 + s_ * 16
                d.rounded_rectangle([xx, y + 10, xx + 8, y + T - 11], 2, fill=travers + (255,))
            for yy in (y + 18, y + T - 24):
                d.rectangle([x, yy + 4, x + T - 1, yy + 6], fill=celik_koyu + (255,))
                d.rectangle([x, yy, x + T - 1, yy + 4], fill=celik + (255,))
        else:
            for s_ in range(4):
                yy = y + 4 + s_ * 16
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
        """Bugday: genc = cift yaprak filizler; olgun = dolgun sari basak demetleri."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        import random
        r = random.Random(7 + i)
        if not olgun:
            filiz = ton(self.CIM, 0.8)
            for s_ in range(8):
                cx = x + 8 + (s_ % 4) * 15 + r.randint(-2, 2)
                cy = y + 24 + (s_ // 4) * 26
                d.ellipse([cx - 6, cy - 2, cx - 1, cy + 4], fill=filiz + (255,))
                d.ellipse([cx + 1, cy - 2, cx + 6, cy + 4], fill=ton(filiz, 1.15) + (255,))
                d.line([cx, cy + 6, cx, cy - 1], fill=ton(filiz, 0.8) + (255,), width=2)
            return
        sari = (228, 190, 96)
        sap = ton(sari, 0.72)
        for s_ in range(8):
            cx = x + 8 + (s_ % 4) * 15 + r.randint(-2, 2)
            taban = y + 30 + (s_ // 4) * 28
            for ax in (-5, 0, 5):
                d.line([cx, taban, cx + ax, taban - 15], fill=sap + (255,), width=2)
                # dolgun basak: ust uste minik daireler
                for k in range(3):
                    d.ellipse([cx + ax - 3, taban - 22 + k * 4, cx + ax + 3, taban - 15 + k * 4],
                              fill=(sari if k % 2 == 0 else ton(sari, 1.1)) + (255,))
    def yumru(self, i, olgun, meyve, yaprak=None, kabak=False):
        """Domates/balkabagi: olgun meyveler isiltili ve calyx'li; kabaksa dilimli."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        yesil = yaprak or ton(self.CIM, 0.82)
        import random
        r = random.Random(11 + i)
        for s_ in range(4):
            cx = x + 14 + (s_ % 2) * 28 + r.randint(-3, 3)
            cy = y + 20 + (s_ // 2) * 26 + r.randint(-2, 2)
            if not olgun:
                # cift yaprakli filiz
                d.ellipse([cx - 7, cy - 1, cx - 1, cy + 6], fill=yesil + (255,))
                d.ellipse([cx + 1, cy - 1, cx + 7, cy + 6], fill=ton(yesil, 1.15) + (255,))
                d.line([cx, cy + 8, cx, cy], fill=ton(yesil, 0.8) + (255,), width=2)
                continue
            if kabak:
                d.ellipse([cx - 10, cy - 6, cx + 10, cy + 11], fill=meyve + (255,))
                d.ellipse([cx - 6, cy - 7, cx + 6, cy + 10], fill=ton(meyve, 1.1) + (255,))
                for ax in (-5, 5):
                    d.arc([cx + ax - 4, cy - 6, cx + ax + 4, cy + 10], 250, 470,
                          fill=ton(meyve, 0.82) + (255,), width=2)
                d.rounded_rectangle([cx - 2, cy - 10, cx + 2, cy - 4], 2,
                                    fill=ton(self.AHSAP, 0.7) + (255,))
            else:
                d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], fill=ton(yesil, 0.9) + (255,))
                d.ellipse([cx - 7, cy - 5, cx + 7, cy + 10], fill=meyve + (255,))
                d.ellipse([cx - 5, cy - 3, cx - 1, cy + 1], fill=ton(meyve, 1.3) + (255,))
                # calyx: tepede yildiz yaprak
                for ax, ay in ((-4, -6), (0, -8), (4, -6)):
                    d.ellipse([cx + ax - 2, cy + ay - 2, cx + ax + 2, cy + ay + 2],
                              fill=ton(yesil, 0.85) + (255,))
    def havuc(self, i, olgun):
        """Havuc: tombul turuncu koni + isilti + pofuduk sacaklar."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        import random
        r = random.Random(23 + i)
        for s_ in range(6):
            cx = x + 10 + (s_ % 3) * 20 + r.randint(-2, 2)
            cy = y + 18 + (s_ // 3) * 28
            sacak = ton(self.CIM, 0.88)
            for ax in (-5, 0, 5):
                d.ellipse([cx + ax - 3, cy - 9, cx + ax + 3, cy - 1],
                          fill=(sacak if ax else ton(sacak, 1.15)) + (255,))
            if olgun:
                d.polygon([(cx - 6, cy + 2), (cx + 6, cy + 2), (cx, cy + 18)],
                          fill=(236, 138, 60, 255))
                d.polygon([(cx - 3, cy + 3), (cx, cy + 3), (cx - 1, cy + 12)],
                          fill=(246, 172, 96, 255))
    def _goz(self, d, cx, cy, w=7, h=8):
        """Iri koyu goz + sol ustte beyaz isilti."""
        d.ellipse([cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2],
                  fill=(58, 48, 44, 255))
        d.ellipse([cx - w // 2 + 1, cy - h // 2 + 1, cx - w // 2 + 4, cy - h // 2 + 4],
                  fill=(255, 255, 255, 235))

    def _yanak(self, d, cx, cy, taban, w=8, h=5, guc=0.55):
        """⚠ Opak karisim: ImageDraw alfa karistirmaz, yari saydam pikseli oldugu
        gibi yazar — yanaklar haritada altindaki zemini gosteren delige donerdi.
        Pembe, zemin rengiyle onceden karistirilip opak basilir."""
        pembe = (240, 142, 150)
        renk = tuple(int(taban[k] * (1 - guc) + pembe[k] * guc) for k in range(3))
        d.ellipse([cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2],
                  fill=renk + (255,))

    def _gulus(self, d, cx, cy, w=8):
        d.arc([cx - w // 2, cy - 4, cx + w // 2, cy + 3], 25, 155,
              fill=(122, 82, 70, 255), width=2)

    # ------------------------------------------------------------ canlilar

    def inek(self, i):
        """Poncik inek: koca kafa, pembe burun, minik boynuz, benekli tombul govde."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        gv = (250, 244, 234)
        benek = (158, 112, 80)
        # tombul govde
        d.rounded_rectangle([x + 10, y + 32, x + 54, y + 56], 13, fill=gv + (255,))
        d.ellipse([x + 38, y + 38, x + 52, y + 52], fill=benek + (255,))
        d.ellipse([x + 12, y + 44, x + 22, y + 54], fill=benek + (255,))
        # bacaklar: kisa tiknaz + toynak
        for k in (16, 27, 38, 48):
            d.rounded_rectangle([x + k, y + 50, x + k + 6, y + 60], 3, fill=gv + (255,))
            d.rounded_rectangle([x + k, y + 57, x + k + 6, y + 61], 2, fill=(96, 78, 66, 255))
        # koca kafa
        d.ellipse([x + 14, y + 2, x + 50, y + 38], fill=gv + (255,))
        # kulaklar + minik boynuzlar
        d.ellipse([x + 8, y + 14, x + 20, y + 24], fill=gv + (255,))
        d.ellipse([x + 44, y + 14, x + 56, y + 24], fill=gv + (255,))
        d.ellipse([x + 11, y + 16, x + 17, y + 21], fill=(238, 190, 184, 255))
        d.ellipse([x + 47, y + 16, x + 53, y + 21], fill=(238, 190, 184, 255))
        d.rounded_rectangle([x + 19, y + 1, x + 25, y + 8], 3, fill=(232, 214, 178, 255))
        d.rounded_rectangle([x + 39, y + 1, x + 45, y + 8], 3, fill=(232, 214, 178, 255))
        # kafa benegi
        d.ellipse([x + 36, y + 4, x + 50, y + 16], fill=benek + (255,))
        # pembe burun + delikler
        d.rounded_rectangle([x + 22, y + 24, x + 42, y + 36], 8, fill=(240, 196, 188, 255))
        d.ellipse([x + 27, y + 28, x + 30, y + 32], fill=(196, 138, 130, 255))
        d.ellipse([x + 34, y + 28, x + 37, y + 32], fill=(196, 138, 130, 255))
        # gozler + yanaklar
        self._goz(d, x + 24, y + 18)
        self._goz(d, x + 40, y + 18)
        self._yanak(d, x + 16, y + 24, gv)
        self._yanak(d, x + 48, y + 24, gv)

    def koyun(self, i):
        """Poncik koyun: bulut yunu, tepede pofuduk, koyu sirin yuz."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        yun = (246, 242, 230)
        import random
        r = random.Random(5)
        # bacaklar önce (yun ustune tasmasin)
        for k in (20, 40):
            d.rounded_rectangle([x + k, y + 48, x + k + 6, y + 60], 3, fill=(88, 72, 62, 255))
        # bulut govde: cevre pofler + dolgu
        for cx, cy, rr in ((18, 36, 11), (32, 32, 13), (46, 36, 11),
                           (16, 46, 10), (32, 48, 12), (47, 46, 10), (24, 40, 12), (40, 41, 12)):
            d.ellipse([x + cx - rr, y + cy - rr, x + cx + rr, y + cy + rr], fill=yun + (255,))
        # koyu yuz — govdenin ust ortasinda
        d.ellipse([x + 21, y + 10, x + 43, y + 34], fill=(112, 92, 80, 255))
        # kulaklar
        d.ellipse([x + 14, y + 18, x + 24, y + 26], fill=(112, 92, 80, 255))
        d.ellipse([x + 40, y + 18, x + 50, y + 26], fill=(112, 92, 80, 255))
        # tepe pofudugu
        d.ellipse([x + 22, y + 2, x + 34, y + 13], fill=yun + (255,))
        d.ellipse([x + 30, y + 1, x + 42, y + 12], fill=yun + (255,))
        # gozler: koyu yuzde beyaz cerceveli
        for gx in (27, 37):
            d.ellipse([x + gx - 4, y + 18, x + gx + 4, y + 27], fill=(246, 242, 230, 255))
            d.ellipse([x + gx - 2, y + 20, x + gx + 2, y + 25], fill=(58, 48, 44, 255))
            d.ellipse([x + gx - 2, y + 20, x + gx, y + 22], fill=(255, 255, 255, 235))
        self._yanak(d, x + 21, y + 27, (112, 92, 80), guc=0.6)
        self._yanak(d, x + 43, y + 27, (112, 92, 80), guc=0.6)
        self._gulus(d, x + 32, y + 30, w=6)

    def tavuk(self, i):
        """Poncik tavuk: top govde, ibik, minik gaga, kanat cikintilari."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        gv = (250, 246, 238)
        # ayaklar
        for k in (25, 34):
            d.rounded_rectangle([x + k, y + 50, x + k + 4, y + 59], 2, fill=(234, 164, 66, 255))
        # top govde
        d.ellipse([x + 14, y + 12, x + 50, y + 52], fill=gv + (255,))
        # kanat cikintilari
        d.ellipse([x + 10, y + 26, x + 22, y + 42], fill=ton(gv, 0.92) + (255,))
        d.ellipse([x + 42, y + 26, x + 54, y + 42], fill=ton(gv, 0.92) + (255,))
        # ibik: uc pembe-kirmizi pof
        for cx in (24, 31, 38):
            d.ellipse([x + cx - 4, y + 5, x + cx + 4, y + 15], fill=(224, 88, 74, 255))
        # gozler
        self._goz(d, x + 26, y + 26, w=6, h=7)
        self._goz(d, x + 38, y + 26, w=6, h=7)
        # gaga + gerdan
        d.polygon([(x + 29, y + 30), (x + 35, y + 30), (x + 32, y + 36)],
                  fill=(238, 172, 70, 255))
        d.ellipse([x + 30, y + 35, x + 34, y + 41], fill=(224, 88, 74, 255))
        self._yanak(d, x + 20, y + 31, gv, w=6, h=4)
        self._yanak(d, x + 44, y + 31, gv, w=6, h=4)

    def koylu(self, i, kadin):
        """Poncik koylu: koca kafa, minik govde; erkekte hasir sapka, kadinda cicek."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        ten = (248, 218, 186)
        sac = (124, 86, 56) if not kadin else (92, 60, 42)
        ust = (98, 142, 188) if not kadin else (216, 112, 122)
        hasir = (228, 196, 120)

        # govde: tombul, kucuk
        if kadin:
            d.polygon([(x + 24, y + 40), (x + 40, y + 40), (x + 44, y + 56), (x + 20, y + 56)],
                      fill=ust + (255,))
        else:
            d.rounded_rectangle([x + 22, y + 40, x + 42, y + 56], 8, fill=ust + (255,))
        # kol nublari
        d.ellipse([x + 17, y + 42, x + 25, y + 50], fill=ust + (255,))
        d.ellipse([x + 39, y + 42, x + 47, y + 50], fill=ust + (255,))
        d.ellipse([x + 18, y + 46, x + 23, y + 51], fill=ten + (255,))
        d.ellipse([x + 41, y + 46, x + 46, y + 51], fill=ten + (255,))
        # ayaklar
        d.rounded_rectangle([x + 24, y + 54, x + 31, y + 60], 3, fill=(96, 74, 58, 255))
        d.rounded_rectangle([x + 33, y + 54, x + 40, y + 60], 3, fill=(96, 74, 58, 255))

        # koca kafa
        d.ellipse([x + 15, y + 6, x + 49, y + 40], fill=ten + (255,))

        if kadin:
            # sac: tepe + iki yan topuz
            d.pieslice([x + 14, y + 4, x + 50, y + 34], 180, 360, fill=sac + (255,))
            d.ellipse([x + 10, y + 18, x + 22, y + 30], fill=sac + (255,))
            d.ellipse([x + 42, y + 18, x + 54, y + 30], fill=sac + (255,))
            # topuzda cicek
            d.ellipse([x + 44, y + 17, x + 52, y + 25], fill=(240, 142, 150, 255))
            d.ellipse([x + 47, y + 20, x + 49, y + 22], fill=(246, 220, 130, 255))
        else:
            # kisa sac + hasir sapka
            d.pieslice([x + 16, y + 6, x + 48, y + 32], 180, 360, fill=sac + (255,))
            d.ellipse([x + 10, y + 8, x + 54, y + 20], fill=ton(hasir, 0.88) + (255,))
            d.ellipse([x + 19, y + 0, x + 45, y + 16], fill=hasir + (255,))
            d.ellipse([x + 22, y + 1, x + 42, y + 10], fill=ton(hasir, 1.1) + (255,))

        # yuz: iri gozler + yanak + gulus
        self._goz(d, x + 26, y + 25)
        self._goz(d, x + 38, y + 25)
        self._yanak(d, x + 19, y + 31, ten)
        self._yanak(d, x + 45, y + 31, ten)
        self._gulus(d, x + 32, y + 32)

    def direk(self, i):
        """Tek cit diregi: dikey cit kenarlari icin (rpg-base yalniz yatay cit verir)."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        ahsap = ton(self.AHSAP, 0.88)
        d.ellipse([x + 25, y + 50, x + 39, y + 58], fill=ton(ahsap, 0.70) + (255,))
        d.rounded_rectangle([x + 27, y + 14, x + 37, y + 54], 4, fill=ahsap + (255,))
        d.rounded_rectangle([x + 27, y + 14, x + 37, y + 30], 4, fill=ton(ahsap, 1.12) + (255,))

    def lokomotif(self, i):
        """Klasik sevimli buharli lokomotif — DOGUYA (saga) gider.

        ⚠ Onceki iki deneme de dagildi: baca kazanin ortasinda mantar gibi
        duruyordu, on izgara govdeden kopuktu, sasi yoktu. Bu surum arketipe
        sadik: arkada sacakli kabin, kirmizi kazan + pirinc bantlar, onde koyu
        duman haznesi ve ustunde huni baca, altta sasi cubugu, baglanti kollu
        tekerler, one entegre izgara.
        """
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        kirmizi = (204, 70, 58)
        kkoyu = ton(kirmizi, 0.76)
        komur = (56, 48, 50)
        kacik = (96, 88, 92)
        pirinc = (228, 190, 96)
        cam = (168, 214, 226)

        # kabin (arkada): kirmizi govde + sacakli koyu cati
        d.rounded_rectangle([x + 4, y + 16, x + 24, y + 50], 4, fill=kirmizi + (255,))
        d.rectangle([x + 4, y + 34, x + 24, y + 50], fill=kkoyu + (255,))
        d.rounded_rectangle([x + 1, y + 11, x + 27, y + 18], 3, fill=komur + (255,))
        # pencere
        d.rounded_rectangle([x + 8, y + 21, x + 20, y + 33], 3, fill=komur + (255,))
        d.rounded_rectangle([x + 10, y + 23, x + 18, y + 31], 2, fill=cam + (255,))
        d.rectangle([x + 10, y + 23, x + 18, y + 26], fill=ton(cam, 1.15) + (255,))

        # kazan: kirmizi silindir + ust isilti + pirinc bantlar
        d.rounded_rectangle([x + 22, y + 28, x + 52, y + 50], 6, fill=kirmizi + (255,))
        d.rounded_rectangle([x + 22, y + 28, x + 52, y + 36], 6, fill=ton(kirmizi, 1.14) + (255,))
        d.rectangle([x + 30, y + 28, x + 33, y + 50], fill=pirinc + (255,))
        d.rectangle([x + 42, y + 28, x + 45, y + 50], fill=pirinc + (255,))
        # buhar kubbesi
        d.pieslice([x + 33, y + 20, x + 43, y + 32], 180, 360, fill=kkoyu + (255,))
        d.pieslice([x + 35, y + 22, x + 41, y + 30], 180, 360, fill=kirmizi + (255,))

        # duman haznesi (onde, koyu) + on kapak + lamba
        d.rounded_rectangle([x + 48, y + 26, x + 62, y + 50], 6, fill=komur + (255,))
        d.rounded_rectangle([x + 48, y + 26, x + 62, y + 33], 6, fill=kacik + (255,))
        d.ellipse([x + 57, y + 34, x + 63, y + 42], fill=kacik + (255,))
        d.ellipse([x + 59, y + 36, x + 61, y + 40], fill=pirinc + (255,))

        # huni baca: haznenin ustunde, govdeli + genis agiz
        d.rectangle([x + 51, y + 14, x + 57, y + 28], fill=komur + (255,))
        d.polygon([(x + 48, y + 8), (x + 60, y + 8), (x + 58, y + 16), (x + 50, y + 16)],
                  fill=komur + (255,))
        d.rectangle([x + 49, y + 8, x + 59, y + 11], fill=kacik + (255,))

        # sasi + one entegre izgara (suvari)
        d.rounded_rectangle([x + 2, y + 48, x + 60, y + 54], 3, fill=komur + (255,))
        d.polygon([(x + 56, y + 48), (x + 63, y + 60), (x + 52, y + 54)],
                  fill=pirinc + (255,))
        d.polygon([(x + 56, y + 48), (x + 60, y + 55), (x + 54, y + 52)],
                  fill=ton(pirinc, 0.8) + (255,))

        # tekerler: iki buyuk + bir kucuk, pirinc gobek + baglanti kolu
        for cx, rr in ((14, 9), (34, 9), (51, 6)):
            d.ellipse([x + cx - rr, y + 60 - rr * 2, x + cx + rr, y + 60],
                      fill=komur + (255,))
            d.ellipse([x + cx - rr + 3, y + 60 - rr * 2 + 3, x + cx + rr - 3, y + 60 - 3],
                      fill=kacik + (255,))
            d.ellipse([x + cx - 2, y + 60 - rr - 2, x + cx + 2, y + 60 - rr + 2],
                      fill=pirinc + (255,))
        d.line([x + 14, y + 51, x + 51, y + 54], fill=pirinc + (255,), width=3)
    def vagon(self, i, yuk="saman"):
        """Yuk vagonu: cerceveli ahsap kasa + sasi + pirinc gobekli tekerler.

        ⚠ Onceki surumde citalar kasanin ustunden tasip mazgal gibi gorunuyordu
        ve tekerler bosluga asiliydi. Citalar artik cercevenin ICINDE, altta
        sasi cubugu var. yuk="saman" pofuduk tepe, "kasa" sandik yigini koyar.
        """
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        ahsap = ton(self.AHSAP, 0.9)
        cerceve = ton(self.AHSAP, 0.66)
        komur = (56, 48, 50)
        pirinc = (228, 190, 96)

        # yuk (kasanin arkasindan yukselir)
        if yuk == "saman":
            saman = (232, 198, 110)
            d.ellipse([x + 8, y + 12, x + 40, y + 34], fill=saman + (255,))
            d.ellipse([x + 26, y + 10, x + 56, y + 32], fill=ton(saman, 1.08) + (255,))
            d.ellipse([x + 14, y + 14, x + 30, y + 24], fill=ton(saman, 1.16) + (255,))
            for sx, sy in ((10, 14), (48, 12), (30, 8)):
                d.line([x + sx, y + sy, x + sx + 5, y + sy - 4],
                       fill=ton(saman, 0.8) + (255,), width=2)
        else:
            for kx, ky, w in ((10, 14, 20), (32, 16, 18)):
                d.rounded_rectangle([x + kx, y + ky, x + kx + w, y + ky + 16], 3,
                                    fill=ahsap + (255,))
                d.rounded_rectangle([x + kx + 2, y + ky + 2, x + kx + w - 2, y + ky + 7], 2,
                                    fill=ton(ahsap, 1.15) + (255,))
                d.rectangle([x + kx, y + ky + 7, x + kx + w, y + ky + 9],
                            fill=cerceve + (255,))

        # kasa: koyu cerceve + ICERIDE dikey citalar
        d.rounded_rectangle([x + 4, y + 26, x + 60, y + 50], 5, fill=cerceve + (255,))
        for k in range(4):
            d.rounded_rectangle([x + 9 + k * 13, y + 30, x + 18 + k * 13, y + 46], 2,
                                fill=ahsap + (255,))
            d.rectangle([x + 9 + k * 13, y + 30, x + 18 + k * 13, y + 34],
                        fill=ton(ahsap, 1.12) + (255,))

        # sasi + tekerler
        d.rounded_rectangle([x + 6, y + 48, x + 58, y + 54], 3, fill=komur + (255,))
        for cx in (18, 46):
            d.ellipse([x + cx - 8, y + 44, x + cx + 8, y + 60], fill=komur + (255,))
            d.ellipse([x + cx - 5, y + 47, x + cx + 5, y + 57], fill=(96, 88, 92, 255))
            d.ellipse([x + cx - 2, y + 50, x + cx + 2, y + 54], fill=pirinc + (255,))
    def yapistir(self, i, katman):
        """Ayri katmanda cizilen (blur'lu) sprite'i karoya oturtur."""
        x, y = self.kanvas(i)
        self.im.alpha_composite(katman, (x, y))

    def karo_kopya(self, kaynak_indeks):
        """rpg-base karosunun piksellerini dondurur (64x64 RGBA)."""
        sx, sy = (kaynak_indeks % 20) * T, (kaynak_indeks // 20) * T
        return self.sheet.crop((sx, sy, sx + T, sy + T)).copy()

    # ------------------------------------------------------------ ince detay

    def cim_koyu_duz(self, i, guc):
        """Orman tabani: cimin TAMAMI koyu.

        ⚠ Merkez-koyu karo (36/37) yogun ormanda her hucrede tekrarlaninca dama
        deseni verdi — ayni hata derin suda da yasandi. Genis bitisik alanlar duz
        varyant ister; merkez-koyu olanlar yalniz SEYREK cayir lekelerinde kalir.
        """
        import numpy as np
        k = self.karo_kopya(43)
        a = np.asarray(k).astype(np.float32)
        a[:, :, :3] *= (1.0 - guc)
        x, y = self.kanvas(i)
        self.im.paste(Image.fromarray(a.astype("uint8"), "RGBA"), (x, y))

    def cim_koyu(self, i, guc):
        """Cayir golgelendirmesi: rpg-base cimini MERKEZI koyulastirarak turetir.

        ⚠ Kenarlar ELLENMEZ: kenar halkasi orijinal cimle ayni kaldigi icin karo,
        normal cim komsulariyla dikissiz birlesir. Duz koyu karo kullanilsaydi
        cayir lekeleri sert kareli sinirlarla gorunurdu.
        """
        import numpy as np
        k = self.karo_kopya(43)
        a = np.asarray(k).astype(np.float32)
        yy, xx = np.mgrid[0:T, 0:T]
        mesafe = np.sqrt(((xx - 31.5) / 30.0) ** 2 + ((yy - 31.5) / 30.0) ** 2)
        maske = np.clip(1.0 - mesafe, 0.0, 1.0) ** 0.7          # merkezde 1, kenarda 0
        carpan = 1.0 - guc * maske
        a[:, :, :3] *= carpan[:, :, None]
        x, y = self.kanvas(i)
        self.im.paste(Image.fromarray(a.astype("uint8"), "RGBA"), (x, y))

    def cicek(self, i, renk):
        """Cicek obegi: 4-5 minik cicek + yaprak. Cimin ustune dekor."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        import random
        r = random.Random(i * 31)
        yesil = ton(self.CIM, 0.82)
        for s_ in range(5):
            cx = x + 10 + r.randint(0, 44)
            cy = y + 10 + r.randint(0, 44)
            d.ellipse([cx - 5, cy + 2, cx + 1, cy + 6], fill=yesil + (255,))
            for ax, ay in ((-3, 0), (3, 0), (0, -3), (0, 3)):
                d.ellipse([cx + ax - 2, cy + ay - 2, cx + ax + 2, cy + ay + 2],
                          fill=renk + (255,))
            d.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=(246, 220, 130, 255))

    def saz(self, i):
        """Kiyi sazi: ince uzun yapraklar + hasir kahvesi basaklar."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        import random
        r = random.Random(77)
        yesil = ton(self.CIM, 0.62)
        acik = ton(self.CIM, 0.85)
        for s_ in range(7):
            bx = x + 8 + s_ * 8 + r.randint(-2, 2)
            egim = r.randint(-6, 6)
            boy = 30 + r.randint(0, 12)
            d.line([bx, y + 58, bx + egim, y + 58 - boy],
                   fill=(yesil if s_ % 2 else acik) + (255,), width=3)
            if s_ % 3 == 0:
                tx, ty = bx + egim, y + 58 - boy
                d.rounded_rectangle([tx - 3, ty - 10, tx + 3, ty + 2], 3,
                                    fill=ton(self.AHSAP, 0.62) + (255,))

    def nilufer(self, i):
        """Nilufer. Denetim: cicek yapraktan kopuktu; artik buyuk yapragin
        uzerinde oturuyor."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        yesil = ton(self.CIM, 0.72)
        d.pieslice([x + 14, y + 18, x + 40, y + 40], 25, 335, fill=yesil + (255,))
        d.pieslice([x + 14, y + 18, x + 40, y + 40], 25, 140, fill=ton(yesil, 1.15) + (255,))
        d.pieslice([x + 40, y + 40, x + 58, y + 55], 25, 335, fill=yesil + (255,))
        # cicek: buyuk yapragin ustunde
        d.ellipse([x + 22, y + 22, x + 32, y + 31], fill=(238, 140, 158, 255))
        d.ellipse([x + 25, y + 25, x + 29, y + 28], fill=(246, 220, 130, 255))
    def cakiltasi(self, i):
        """Cakil. Denetim: parlak gumus kureler 'celik bilye' gibi okunuyordu;
        simdi mat, sicak gri-kahve, farkli boy, parlamasiz."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        import random
        r = random.Random(i * 13)
        for s_ in range(7):
            cx = x + 6 + r.randint(0, 50)
            cy = y + 8 + r.randint(0, 48)
            w = 3 + r.randint(0, 5)
            h = max(2, w - r.randint(0, 2))
            gri = 0.78 + r.random() * 0.2
            taban = ton((176, 158, 138), gri)
            d.ellipse([cx, cy, cx + w, cy + h], fill=taban + (255,))
    def golge(self, i):
        """Yumusak elips golge — agac/hayvan/esya altina, Dekor katmanina.

        ⚠ Ayri katmanda cizilip Gauss ile yumusatilir; keskin elips 'yapistirma
        cikartma' gibi duruyor, yumusagi nesneyi zemine oturtuyor.
        """
        katman = Image.new("RGBA", (T, T), (0, 0, 0, 0))
        d = ImageDraw.Draw(katman)
        d.ellipse([10, 40, 54, 60], fill=(30, 24, 20, 120))
        katman = katman.filter(ImageFilter.GaussianBlur(5))
        self.yapistir(i, katman)

    def kuyu(self, i):
        """Koy kuyusu. Denetim bulgulari islendi: bilezik artik TAM elips (ust-arka
        yay dahil), direk tabanlari bilezigin DIS kenarina basiyor."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        tas = ton(self.TAS, 0.94)
        cati = ton(self.AHSAP, 0.78)
        # tam bilezik: dis elips + ic karanlik + on yuz kalinligi
        d.ellipse([x + 12, y + 28, x + 52, y + 52], fill=ton(tas, 0.72) + (255,))
        d.ellipse([x + 12, y + 24, x + 52, y + 48], fill=tas + (255,))
        d.ellipse([x + 17, y + 28, x + 47, y + 44], fill=(46, 38, 40, 255))
        # tas derzleri
        for ax in (16, 28, 40):
            d.arc([x + 12, y + 24, x + 52, y + 48], 150 + ax * 3, 165 + ax * 3,
                  fill=ton(tas, 0.7) + (255,), width=2)
        # direkler bilezigin disinda
        d.rectangle([x + 9, y + 12, x + 14, y + 42], fill=cati + (255,))
        d.rectangle([x + 50, y + 12, x + 55, y + 42], fill=cati + (255,))
        # cati
        d.polygon([(x + 32, y + 0), (x + 4, y + 16), (x + 60, y + 16)],
                  fill=ton(cati, 0.85) + (255,))
        d.polygon([(x + 32, y + 4), (x + 12, y + 16), (x + 52, y + 16)],
                  fill=ton(cati, 1.05) + (255,))
        # ip + kova
        d.line([x + 32, y + 16, x + 32, y + 30], fill=(60, 50, 44, 255), width=2)
        d.rounded_rectangle([x + 27, y + 28, x + 37, y + 36], 3,
                            fill=ton(self.TAS, 0.7) + (255,))
    def fener(self, i):
        """Sokak feneri: koyu direk + sicak isikli cam kutu + hale."""
        # hale ayri katmanda, blur'lu
        katman = Image.new("RGBA", (T, T), (0, 0, 0, 0))
        dh = ImageDraw.Draw(katman)
        dh.ellipse([14, 2, 50, 32], fill=(255, 214, 120, 70))
        katman = katman.filter(ImageFilter.GaussianBlur(6))
        self.yapistir(i, katman)

        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        demir = (66, 58, 60)
        d.rectangle([x + 30, y + 20, x + 34, y + 54], fill=demir + (255,))
        d.polygon([(x + 24, y + 58), (x + 40, y + 58), (x + 36, y + 52), (x + 28, y + 52)],
                  fill=demir + (255,))
        d.rounded_rectangle([x + 24, y + 6, x + 40, y + 22], 3, fill=demir + (255,))
        d.rectangle([x + 27, y + 9, x + 37, y + 19], fill=(255, 214, 120, 255))
        d.rectangle([x + 27, y + 9, x + 37, y + 12], fill=(255, 238, 180, 255))

    def baca(self, i):
        """Cati bacasi: yuvarlak koseli govde + tastan sapka + koyu agiz."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        tas = ton(self.TAS, 0.9)
        d.rounded_rectangle([x + 24, y + 20, x + 40, y + 52], 4, fill=tas + (255,))
        d.rounded_rectangle([x + 24, y + 20, x + 40, y + 28], 4, fill=ton(tas, 1.12) + (255,))
        d.rounded_rectangle([x + 20, y + 12, x + 44, y + 22], 4, fill=ton(tas, 0.72) + (255,))
        d.ellipse([x + 26, y + 14, x + 38, y + 20], fill=(46, 38, 40, 255))
    def duman(self, i, kucuk):
        """Baca/lokomotif dumani: yumusak puf."""
        katman = Image.new("RGBA", (T, T), (0, 0, 0, 0))
        d = ImageDraw.Draw(katman)
        if kucuk:
            d.ellipse([22, 30, 42, 48], fill=(246, 244, 240, 205))
            d.ellipse([32, 22, 50, 40], fill=(246, 244, 240, 170))
        else:
            d.ellipse([10, 30, 38, 54], fill=(246, 244, 240, 210))
            d.ellipse([26, 20, 54, 46], fill=(246, 244, 240, 185))
            d.ellipse([20, 12, 44, 34], fill=(244, 242, 238, 110))
        katman = katman.filter(ImageFilter.GaussianBlur(3))
        self.yapistir(i, katman)

    def sandal(self, i):
        """Kayik: yumusak govde, isiltili ic, burun halkasi."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        govde = ton(self.AHSAP, 0.72)
        ic = ton(self.AHSAP, 1.02)
        d.polygon([(x + 6, y + 26), (x + 42, y + 20), (x + 56, y + 32),
                   (x + 42, y + 44), (x + 6, y + 38)], fill=govde + (255,))
        d.polygon([(x + 11, y + 28), (x + 41, y + 24), (x + 50, y + 32),
                   (x + 41, y + 40), (x + 11, y + 36)], fill=ic + (255,))
        d.polygon([(x + 12, y + 29), (x + 40, y + 25), (x + 44, y + 29),
                   (x + 13, y + 32)], fill=ton(ic, 1.12) + (255,))
        for k in (20, 31):
            d.rounded_rectangle([x + k, y + 26, x + k + 4, y + 38], 2, fill=govde + (255,))
        d.ellipse([x + 53, y + 29, x + 59, y + 35], fill=ton(govde, 0.8) + (255,))
        d.ellipse([x + 55, y + 31, x + 57, y + 33], fill=(120, 196, 208, 255))
    def cicek_tarhi(self, i):
        """Ahsap cerceveli cicek tarhi — meydan susu."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        cerceve = ton(self.AHSAP, 0.82)
        d.rounded_rectangle([x + 6, y + 12, x + 58, y + 52], 6, fill=cerceve + (255,))
        d.rounded_rectangle([x + 10, y + 16, x + 54, y + 48], 4,
                            fill=ton(self.TOPRAK, 0.72) + (255,))
        import random
        r = random.Random(5)
        renkler = [(238, 130, 148), (246, 220, 130), (240, 240, 236)]
        for s_ in range(6):
            cx = x + 15 + (s_ % 3) * 15 + r.randint(-2, 2)
            cy = y + 22 + (s_ // 3) * 14 + r.randint(-1, 1)
            d.ellipse([cx - 4, cy + 2, cx + 4, cy + 7], fill=ton(self.CIM, 0.85) + (255,))
            for ax, ay in ((-3, 0), (3, 0), (0, -3), (0, 3)):
                d.ellipse([cx + ax - 2, cy + ay - 2, cx + ax + 2, cy + ay + 2],
                          fill=renkler[s_ % 3] + (255,))
            d.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=(160, 120, 60, 255))

    def tas_yigini(self, i):
        """Maden isligi icin kaya yigini."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        import random
        r = random.Random(3)
        for cx, cy, w in ((18, 40, 16), (36, 38, 18), (28, 28, 15), (44, 46, 12), (12, 48, 11)):
            taban = ton(self.TAS, 0.8 + r.random() * 0.25)
            d.ellipse([x + cx - w // 2, y + cy - w // 2 + 2, x + cx + w // 2, y + cy + w // 2 + 6],
                      fill=taban + (255,))
            d.ellipse([x + cx - w // 3, y + cy - w // 2 + 3, x + cx + w // 3, y + cy],
                      fill=ton(taban, 1.14) + (255,))

    def cevher(self, i):
        """Icinde altin parlamalari olan kaya — maden hissi."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        taban = ton(self.TAS, 0.7)
        d.rounded_rectangle([x + 14, y + 22, x + 50, y + 54], 12, fill=taban + (255,))
        d.rounded_rectangle([x + 18, y + 24, x + 46, y + 38], 10, fill=ton(taban, 1.15) + (255,))
        for cx, cy in ((26, 34), (38, 42), (32, 48), (44, 32)):
            d.polygon([(x + cx, y + cy - 4), (x + cx + 4, y + cy),
                       (x + cx, y + cy + 4), (x + cx - 4, y + cy)],
                      fill=(226, 186, 96, 255))
            d.ellipse([x + cx - 1, y + cy - 2, x + cx + 1, y + cy], fill=(255, 244, 210, 255))

    def kelebek(self, i, acik, renk=(234, 152, 64)):
        """Kelebek: benekli kanatlar, iki kare cirpma. renk ile turuncu/mavi."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        if acik:
            d.ellipse([x + 19, y + 24, x + 31, y + 37], fill=renk + (255,))
            d.ellipse([x + 33, y + 24, x + 45, y + 37], fill=renk + (255,))
            d.ellipse([x + 22, y + 33, x + 30, y + 41], fill=ton(renk, 1.2) + (255,))
            d.ellipse([x + 34, y + 33, x + 42, y + 41], fill=ton(renk, 1.2) + (255,))
            d.ellipse([x + 23, y + 27, x + 27, y + 31], fill=(250, 240, 220, 255))
            d.ellipse([x + 37, y + 27, x + 41, y + 31], fill=(250, 240, 220, 255))
        else:
            d.ellipse([x + 25, y + 23, x + 32, y + 39], fill=renk + (255,))
            d.ellipse([x + 32, y + 23, x + 39, y + 39], fill=ton(renk, 1.15) + (255,))
        d.rounded_rectangle([x + 30, y + 26, x + 34, y + 40], 2, fill=(70, 54, 46, 255))
    def golge_serit(self, i):
        """Sacak golgesi: ustten asagi solan yari saydam serit — kaide pedinin
        ustune biner, bina zemine 'oturur'. Inceleme bulgusu."""
        katman = Image.new("RGBA", (T, T), (0, 0, 0, 0))
        d = ImageDraw.Draw(katman)
        for r_ in range(22):
            a = int(70 * (1 - r_ / 22.0))
            d.line([0, r_, T - 1, r_], fill=(30, 24, 20, a))
        self.yapistir(i, katman)

    def devrik_kutuk(self, i):
        """Yatay devrilmis kutuk + iki kesik odun — orman isi izi."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        govde = ton(self.AHSAP, 0.68)
        d.rounded_rectangle([x + 8, y + 26, x + 52, y + 40], 7, fill=govde + (255,))
        d.rounded_rectangle([x + 8, y + 26, x + 52, y + 32], 7, fill=ton(govde, 1.15) + (255,))
        d.ellipse([x + 46, y + 26, x + 58, y + 40], fill=ton(self.KREM, 0.9) + (255,))
        d.ellipse([x + 49, y + 29, x + 55, y + 37], fill=ton(self.AHSAP, 1.05) + (255,))
        for cx, cy in ((16, 48), (28, 50)):
            d.ellipse([x + cx, y + cy, x + cx + 10, y + cy + 8], fill=govde + (255,))
            d.ellipse([x + cx + 2, y + cy + 2, x + cx + 8, y + cy + 6],
                      fill=ton(self.KREM, 0.85) + (255,))

    def tezgah(self, i, cesit=0):
        """Pazar tezgahi. Denetim: tente dilimleri artik 54px'e TAM bolunuyor
        (6 dilim x 9px, fisto ayni hizada) ve urun dizilimi cesitleniyor."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        masa = ton(self.AHSAP, 0.9)
        d.rectangle([x + 10, y + 30, x + 14, y + 54], fill=ton(masa, 0.75) + (255,))
        d.rectangle([x + 50, y + 30, x + 54, y + 54], fill=ton(masa, 0.75) + (255,))
        d.rounded_rectangle([x + 5, y + 34, x + 59, y + 44], 3, fill=masa + (255,))
        # urunler: ceside gore
        urunler = [
            [((216, 76, 60), (240, 130, 110)), ((246, 208, 90), (252, 234, 160)),
             ((150, 106, 74), (186, 140, 104))],
            [((236, 138, 60), (246, 172, 96)), ((216, 76, 60), (240, 130, 110)),
             ((132, 176, 60), (168, 205, 96))],
        ][cesit % 2]
        for k, (ana, isik) in enumerate(urunler):
            cx = x + 14 + k * 13
            d.ellipse([cx, y + 26, cx + 10, y + 36], fill=ana + (255,))
            d.ellipse([cx + 2, y + 28, cx + 5, y + 31], fill=isik + (255,))
        # tente: 6 dilim x 9 = 54, fisto ayni izgarada
        for k in range(6):
            renk = (214, 70, 56) if k % 2 == 0 else (246, 240, 228)
            d.rectangle([x + 5 + k * 9, y + 8, x + 14 + k * 9, y + 20], fill=renk + (255,))
        for k in range(6):
            renk = (214, 70, 56) if k % 2 == 0 else (246, 240, 228)
            d.pieslice([x + 5 + k * 9, y + 15, x + 14 + k * 9, y + 25], 0, 180,
                       fill=renk + (255,))
        d.rectangle([x + 3, y + 6, x + 61, y + 10], fill=ton(masa, 0.8) + (255,))
    def asili_tabela(self, i):
        """Dukkan tabelasi. Denetim: kirmizi daire 'bildirim rozeti' gibi okunuyordu;
        simdi koyu cerceveli pano + elma ikonu + zincir baglantilari."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        for zx in (24, 40):
            d.line([x + zx, y + 38, x + zx, y + 46], fill=(88, 76, 66, 255), width=2)
            d.ellipse([x + zx - 2, y + 38, x + zx + 2, y + 42], fill=(88, 76, 66, 255))
        d.rounded_rectangle([x + 16, y + 44, x + 48, y + 60], 4,
                            fill=ton(self.AHSAP, 0.62) + (255,))
        d.rounded_rectangle([x + 18, y + 46, x + 46, y + 58], 3,
                            fill=ton(self.AHSAP, 1.0) + (255,))
        # elma ikonu
        d.ellipse([x + 27, y + 49, x + 37, y + 58], fill=(214, 70, 56, 255))
        d.ellipse([x + 29, y + 50, x + 32, y + 53], fill=(240, 130, 110, 255))
        d.line([x + 32, y + 49, x + 33, y + 46], fill=(88, 76, 66, 255), width=2)
        d.ellipse([x + 33, y + 46, x + 37, y + 49], fill=ton(self.CIM, 0.8) + (255,))
    def kumes(self, i):
        """Tavuk kumesi: yuvarlak catili kirmizi kulube + yuvarlak kapi + pencere."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        gv = (176, 88, 68)
        cati = ton(self.AHSAP, 0.75)
        d.rounded_rectangle([x + 12, y + 22, x + 52, y + 50], 6, fill=gv + (255,))
        d.pieslice([x + 8, y + 4, x + 56, y + 40], 180, 360, fill=cati + (255,))
        d.pieslice([x + 14, y + 8, x + 50, y + 36], 180, 360, fill=ton(cati, 1.12) + (255,))
        # yuvarlak kapi + esik rampasi
        d.ellipse([x + 25, y + 28, x + 39, y + 44], fill=(46, 38, 40, 255))
        d.polygon([(x + 28, y + 44), (x + 44, y + 58), (x + 34, y + 58)],
                  fill=ton(self.AHSAP, 0.95) + (255,))
        for k in range(3):
            d.line([x + 31 + k * 4, y + 47 + k * 4, x + 38 + k * 4, y + 47 + k * 4],
                   fill=ton(self.AHSAP, 0.7) + (255,), width=2)
        # minik yuvarlak pencere
        d.ellipse([x + 43, y + 27, x + 49, y + 33], fill=(246, 220, 130, 255))
    def yalak(self, i):
        """Su yalagi: ahsap tekne + su."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        d.rounded_rectangle([x + 8, y + 30, x + 56, y + 50], 6,
                            fill=ton(self.AHSAP, 0.7) + (255,))
        d.rounded_rectangle([x + 12, y + 34, x + 52, y + 46], 4, fill=(120, 196, 208, 255))
        d.ellipse([x + 18, y + 36, x + 30, y + 41], fill=(168, 224, 232, 255))

    def maden_vagonu(self, i):
        """Maden el vagonu: yumusak kova, parlak cevher tepesi, gobekli teker."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        govde = ton(self.TAS, 0.55)
        # cevher tepesi
        for cx, cy, rr in ((24, 22, 8), (34, 18, 9), (43, 23, 7)):
            d.ellipse([x + cx - rr, y + cy - rr, x + cx + rr, y + cy + rr],
                      fill=ton(self.TAS, 0.8) + (255,))
        d.ellipse([x + 31, y + 12, x + 39, y + 19], fill=(226, 186, 96, 255))
        d.ellipse([x + 22, y + 18, x + 27, y + 23], fill=(226, 186, 96, 255))
        # kova: yuvarlak koseli, ust seridi acik
        d.rounded_rectangle([x + 12, y + 24, x + 52, y + 48], 7, fill=govde + (255,))
        d.rounded_rectangle([x + 12, y + 24, x + 52, y + 31], 7, fill=ton(govde, 1.28) + (255,))
        d.rounded_rectangle([x + 16, y + 34, x + 48, y + 44], 5, fill=ton(govde, 0.85) + (255,))
        # gobekli tekerler
        for cx in (22, 42):
            d.ellipse([x + cx - 6, y + 45, x + cx + 6, y + 57], fill=(52, 44, 46, 255))
            d.ellipse([x + cx - 2, y + 49, x + cx + 2, y + 53], fill=(214, 70, 56, 255))
    def dalga(self, i):
        """Acik su dalga vurgusu: iki kisa beyaz yay. Turuncu benekli rpg-base
        su cesitlemesinin yerine geciyor (inceleme bulgusu)."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        for cx, cy, w in ((16, 20, 22), (34, 42, 18)):
            d.arc([x + cx, y + cy, x + cx + w, y + cy + 10], 200, 340,
                  fill=(230, 248, 252, 210), width=3)

    def derin_su(self, i):
        """Derin su: duz su karosunun TAMAMI %7 koyu.

        ⚠ Cimde ise yarayan merkez-koyu hilesi burada ters tepti: puruzssuz su
        yuzeyinde her karonun merkez lekesi ayri okundu ve gol puantiyeli cikti.
        Derin bolge kendi icinde dikissiz olsun diye ton, karonun tamamina esit
        uygulanir; derin/sig siniri tek kademelik yumusak bir bant olarak kalir.
        """
        import numpy as np
        k = self.karo_kopya(31)
        a = np.asarray(k).astype(np.float32)
        a[:, :, :3] *= 0.93
        x, y = self.kanvas(i)
        self.im.paste(Image.fromarray(a.astype("uint8"), "RGBA"), (x, y))

    def cim_sicak(self, i, kenarli):
        """Sonbahar zemin tonu: cimin sicak (sari-turuncu kacik) hali.

        Ayni leke mimarisi: kenarli=False leke ICI (tam karo sicak),
        kenarli=True leke KENARI (yalniz merkez sicak, kenar halkasi orijinal).
        Sonbahar agaclari yesil zeminde igreti duruyordu; zemin de donsun.
        """
        import numpy as np
        k = self.karo_kopya(43)
        a = np.asarray(k).astype(np.float32)
        if kenarli:
            yy, xx = np.mgrid[0:T, 0:T]
            mesafe = np.sqrt(((xx - 31.5) / 30.0) ** 2 + ((yy - 31.5) / 30.0) ** 2)
            maske = np.clip(1.0 - mesafe, 0.0, 1.0) ** 0.7
        else:
            maske = np.ones((T, T), dtype=np.float32)
        a[:, :, 0] *= 1.0 + 0.10 * maske
        a[:, :, 1] *= 1.0 - 0.015 * maske
        a[:, :, 2] *= 1.0 - 0.16 * maske
        x, y = self.kanvas(i)
        self.im.paste(Image.fromarray(np.clip(a, 0, 255).astype("uint8"), "RGBA"), (x, y))

    def odun_istifi(self, i):
        """Kesilmis odun istifi — oduncu acikligi icin. Halkali kutuk uclari one bakar."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        govde = ton(self.AHSAP, 0.72)
        halka = ton(self.KREM, 0.9)
        ic = ton(self.AHSAP, 1.05)
        yerler = [(14, 44), (28, 44), (42, 44), (21, 33), (35, 33), (28, 22)]
        for cx, cy in yerler:
            d.ellipse([x + cx - 8, y + cy - 8, x + cx + 8, y + cy + 8], fill=govde + (255,))
            d.ellipse([x + cx - 6, y + cy - 6, x + cx + 6, y + cy + 6], fill=halka + (255,))
            d.ellipse([x + cx - 3, y + cy - 3, x + cx + 3, y + cy + 3], fill=ic + (255,))
        # alt destek kalasi
        d.rounded_rectangle([x + 4, y + 50, x + 52, y + 55], 2, fill=ton(govde, 0.85) + (255,))

    def agac_koyu(self, i, kaynak, guc=0.84):
        """rpg-base agacinin koyulastirilmis turevi.

        ⚠ Olcum: KavakYesil'in (200) ana yesili (129,181,45), koyulastirilmis
        orman zeminiyle (131,182,49) neredeyse birebir ayniydi — yesil agaclar
        kendi zeminlerinde GORUNMEZ oluyordu. Koyu turev hem normal cimde hem
        koyu tabanda kontrastli; kanopinin cimden koyu olmasi dogal da okunur.
        """
        import numpy as np
        k = self.karo_kopya(kaynak)
        a = np.asarray(k).astype(np.float32)
        a[:, :, :3] *= guc
        x, y = self.kanvas(i)
        self.im.paste(Image.fromarray(np.clip(a, 0, 255).astype("uint8"), "RGBA"), (x, y))

    def uzun_agac(self, alt_i, ust_i, kaynak, koyu):
        """2 karo boyunda agac — TEK tuval kurgusu.

        ⚠ Onceki kurgu (ust karo = agacin tamami, alt karo = ayri cizilmis uzun
        govde) "lolipop" uretti: kanopi govdeden kopuk duruyor, govde bir karo
        boyunca ciplak direk gibi uzuyordu. Simdi 64x128 tek tuvalde once KISA
        ve KALIN govde cizilir, kanopi ustune BINDIRILIR (kaynagin kendi mini
        govdesini de orter), tuval sonra iki karoya bolunur — dikissiz, dogru
        oranli (kanopi ~2/3, govde ~1/3).
        """
        import numpy as np
        k = self.karo_kopya(kaynak)
        if koyu:
            a = np.asarray(k).astype(np.float32)
            a[:, :, :3] *= 0.84
            k = Image.fromarray(np.clip(a, 0, 255).astype("uint8"), "RGBA")

        kanvas = Image.new("RGBA", (T, 2 * T), (0, 0, 0, 0))
        d = ImageDraw.Draw(kanvas)
        govde = ton(self.AHSAP, 0.82)
        koyu_g = ton(govde, 0.78)
        acik_g = ton(govde, 1.14)
        # kisa-kalin govde: kanopinin altindan cikar (satir 70..114)
        d.polygon([(25, 70), (39, 70), (41, 106), (23, 106)], fill=govde + (255,))
        d.polygon([(23, 104), (15, 113), (28, 109)], fill=govde + (255,))
        d.polygon([(41, 104), (49, 113), (36, 109)], fill=govde + (255,))
        d.rounded_rectangle([18, 106, 46, 113], 3, fill=ton(govde, 0.9) + (255,))
        d.line([28, 74, 27, 104], fill=koyu_g + (255,), width=2)
        d.line([36, 76, 37, 102], fill=koyu_g + (255,), width=2)
        d.line([32, 72, 32, 106], fill=acik_g + (255,), width=1)
        # kanopi govdenin ustune biner (satir 20..84) — kaynagin mini govdesi
        # ve benim govdemin tepesi kanopinin altinda kalir
        kanvas.alpha_composite(k, (0, 20))

        x, y = self.kanvas(ust_i)
        self.im.alpha_composite(kanvas.crop((0, 0, T, T)), (x, y))
        x, y = self.kanvas(alt_i)
        self.im.alpha_composite(kanvas.crop((0, T, T, 2 * T)), (x, y))
    def vitrin(self, i):
        """Dukkan vitrini. Denetim: rpg-base cam karolari duvarla ayni renkti;
        gok mavisi cam + capraz parilti + krem cerceve."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        cer = ton(self.KREM, 0.9)
        d.rounded_rectangle([x + 8, y + 10, x + 56, y + 52], 5, fill=ton(cer, 0.72) + (255,))
        d.rounded_rectangle([x + 11, y + 13, x + 53, y + 49], 4, fill=cer + (255,))
        d.rounded_rectangle([x + 15, y + 17, x + 49, y + 45], 3, fill=(184, 216, 230, 255))
        d.polygon([(x + 18, y + 45), (x + 30, y + 17), (x + 38, y + 17), (x + 22, y + 45)],
                  fill=(214, 236, 246, 255))
        d.rectangle([x + 30, y + 17, x + 34, y + 45], fill=cer + (255,))
        d.rectangle([x + 15, y + 29, x + 49, y + 33], fill=cer + (255,))
        d.rounded_rectangle([x + 6, y + 48, x + 58, y + 56], 3, fill=ton(cer, 0.8) + (255,))

    def saman_yigini(self, i):
        """Gercek saman yigini. Denetim: SamanYigin diye kullanilan rpg karosu
        aslinda krem comlekti — denetci haritada saman bulamadi, hakliydi."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        saman = (232, 198, 110)
        d.ellipse([x + 8, y + 28, x + 56, y + 56], fill=ton(saman, 0.88) + (255,))
        d.ellipse([x + 12, y + 16, x + 52, y + 48], fill=saman + (255,))
        d.ellipse([x + 18, y + 18, x + 40, y + 34], fill=ton(saman, 1.12) + (255,))
        import random
        r = random.Random(9)
        for s_ in range(8):
            sx = x + 10 + r.randint(0, 44)
            sy = y + 18 + r.randint(0, 30)
            d.line([sx, sy, sx + 6, sy - 3], fill=ton(saman, 0.72) + (255,), width=2)
        d.line([x + 30, y + 16, x + 36, y + 6], fill=ton(saman, 0.72) + (255,), width=2)
        d.line([x + 36, y + 6, x + 42, y + 12], fill=ton(saman, 0.72) + (255,), width=2)

    def sandik(self, i):
        """Kontrastli sandik. Denetim: rpg kasasi toprakla ayni tondaydi."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        cer = (118, 78, 48)
        panel = (222, 178, 120)
        d.rounded_rectangle([x + 10, y + 14, x + 54, y + 54], 5, fill=cer + (255,))
        d.rounded_rectangle([x + 14, y + 18, x + 50, y + 32], 3, fill=panel + (255,))
        d.rounded_rectangle([x + 14, y + 36, x + 50, y + 50], 3, fill=ton(panel, 0.9) + (255,))
        d.rectangle([x + 14, y + 18, x + 50, y + 23], fill=ton(panel, 1.12) + (255,))
        d.line([x + 14, y + 36, x + 50, y + 50], fill=cer + (255,), width=3)
        d.line([x + 50, y + 36, x + 14, y + 50], fill=cer + (255,), width=3)

    def cit_dikey(self, i):
        """Dikey cit karosu. Denetim: dikey kenarlar tek kaziktan ibaretti,
        raysiz cit cit gibi okunmuyordu — iki dikey ray + kazik."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        ahsap = ton(self.AHSAP, 0.88)
        for rx in (24, 36):
            d.rectangle([x + rx, y, x + rx + 4, y + 63], fill=ton(ahsap, 0.92) + (255,))
            d.rectangle([x + rx, y, x + rx + 2, y + 63], fill=ton(ahsap, 1.08) + (255,))
        d.rounded_rectangle([x + 28, y + 22, x + 36, y + 58], 3, fill=ahsap + (255,))
        d.rounded_rectangle([x + 28, y + 22, x + 36, y + 34], 3, fill=ton(ahsap, 1.12) + (255,))
        d.ellipse([x + 27, y + 56, x + 37, y + 61], fill=ton(ahsap, 0.7) + (255,))

    def kopru_ray(self, i):
        """Kopru + ray birlesimi: kalas doseme uzerinde celik raylar.
        Denetim: tren koprusunde ray cizgisi kayboluyordu."""
        self.kalas(i, dikey=False)
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        celik = ton(self.TAS, 1.12)
        celik_koyu = ton(self.TAS, 0.78)
        for yy in (y + 18, y + T - 24):
            d.rectangle([x, yy + 4, x + T - 1, yy + 6], fill=celik_koyu + (255,))
            d.rectangle([x, yy, x + T - 1, yy + 4], fill=celik + (255,))

    def isaret(self, i):
        """Yon tabelasi: saga isaret eden yuvarlak uclu ok levha. Ilk surum kaba
        bir T seklindeydi; simdi civili, iki tonlu, sivri uclu gercek bir tabela."""
        x, y = self.kanvas(i)
        d = ImageDraw.Draw(self.im)
        direk = ton(self.AHSAP, 0.72)
        levha = ton(self.AHSAP, 1.0)
        d.rounded_rectangle([x + 28, y + 20, x + 36, y + 58], 4, fill=direk + (255,))
        d.rounded_rectangle([x + 28, y + 20, x + 36, y + 30], 4, fill=ton(direk, 1.12) + (255,))
        # ok levha: govde + sivri uc
        d.rounded_rectangle([x + 8, y + 12, x + 46, y + 30], 6, fill=levha + (255,))
        d.polygon([(x + 44, y + 12), (x + 58, y + 21), (x + 44, y + 30)], fill=levha + (255,))
        d.rounded_rectangle([x + 8, y + 12, x + 46, y + 20], 6, fill=ton(levha, 1.12) + (255,))
        d.polygon([(x + 44, y + 12), (x + 55, y + 19), (x + 44, y + 20)],
                  fill=ton(levha, 1.12) + (255,))
        # civiler
        d.ellipse([x + 13, y + 19, x + 17, y + 23], fill=ton(direk, 0.8) + (255,))
        d.ellipse([x + 38, y + 19, x + 42, y + 23], fill=ton(direk, 0.8) + (255,))
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
    c.vagon(10, yuk="saman")
    c.vagon(11, yuk="kasa")
    # Satir 3: ince detay — zemin ve dogal dekor
    c.cim_koyu(36, 0.055)
    c.cim_koyu(37, 0.10)
    c.cicek(38, (240, 240, 236))
    c.cicek(39, (246, 208, 90))
    c.cicek(40, (238, 130, 148))
    c.saz(41)
    c.nilufer(42)
    c.cakiltasi(43)
    c.golge(44)
    c.kuyu(45)
    c.fener(46)
    c.baca(47)
    # Satir 4: yasam ve is
    c.duman(48, kucuk=False)
    c.duman(49, kucuk=True)
    c.sandal(50)
    c.cicek_tarhi(51)
    c.tas_yigini(52)
    c.cevher(53)
    c.kelebek(54, acik=True)
    c.kelebek(55, acik=False)
    c.golge_serit(56)
    c.devrik_kutuk(57)
    c.tezgah(58, cesit=0)
    c.tezgah(87, cesit=1)
    c.asili_tabela(59)
    c.kumes(60)
    c.yalak(61)
    c.maden_vagonu(62)
    c.dalga(63)
    c.derin_su(64)
    c.cim_koyu_duz(65, 0.07)
    c.cim_sicak(66, kenarli=False)
    c.cim_sicak(67, kenarli=True)
    c.kelebek(68, acik=True, renk=(108, 168, 205))
    c.kelebek(69, acik=False, renk=(108, 168, 205))
    c.odun_istifi(70)
    c.agac_koyu(71, 200)   # KavakYesil koyu
    c.agac_koyu(72, 220)   # TopYesil koyu
    c.agac_koyu(73, 221)   # TopYesilInce koyu
    # Uzun (2 karo) agaclar — 2.5D derinlik icin
    c.uzun_agac(74, 75, 200, koyu=True)    # koyu kavak uzun (alt, ust)
    c.uzun_agac(76, 77, 204, koyu=False)   # teal cam uzun
    c.uzun_agac(78, 79, 202, koyu=False)   # turuncu kavak uzun (sonbahar)
    c.vitrin(80)
    c.saman_yigini(81)
    c.agac_koyu(82, 180, guc=0.8)   # buyuk yesil cali koyu (hayalet cali duzeltmesi)
    c.agac_koyu(83, 181, guc=0.8)   # kucuk yesil cali koyu
    c.sandik(84)
    c.cit_dikey(85)
    c.kopru_ray(86)
    # Satir 1: ekinler (genc 12-15, olgun 16-19)
    c.bugday(12, olgun=False)
    c.yumru(13, olgun=False, meyve=(214, 70, 56))
    c.yumru(14, olgun=False, meyve=(230, 140, 50), kabak=True)
    c.havuc(15, olgun=False)
    c.bugday(16, olgun=True)
    c.yumru(17, olgun=True, meyve=(214, 70, 56))
    c.yumru(18, olgun=True, meyve=(230, 140, 50), kabak=True)
    c.havuc(19, olgun=True)
    # Satir 2: canlilar
    c.inek(24)
    c.koyun(25)
    c.tavuk(26)
    c.koylu(27, kadin=False)
    c.koylu(28, kadin=True)
    # Oyun parseli evre karolari
    c.filiz(88)
    c.surulu_islak(89, kaynak=2)

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
