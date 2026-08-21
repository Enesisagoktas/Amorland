# -*- coding: utf-8 -*-
"""Karo sayfasi olcum araci.

KaroSeti.cs'e indeks secmeden ONCE kosulur. Iki kez yasandi: gozle dogrulanmamis
indeks secimi haritayi bastan basa bozdu (kontur izgarasi, puantiye meydan).
Bu arac uc sey uretir:

  1. Etiketli indeks tablosu PNG'si  -> gozle bakilir
  2. Dikissiz zemin karolari listesi -> tamamen opak + kontursuz + kenar uyumlu
  3. Kenar siniflandirmasi           -> autotile bloklarini OLCEREK cikarir

Kullanim:
  python olc.py <sayfa.png> <karo> [bosluk] [--tablo cikti.png] [--sinif r,g,b=AD ...]

Ornek (roguelike sheet, 16px karo, 1px bosluk):
  python olc.py Spritesheet/roguelikeSheet_transparent.png 16 1 --tablo tablo.png
"""
import sys

from PIL import Image, ImageDraw


def ayristir(argv):
    konum, secenek = [], {"tablo": None, "sinif": []}
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--tablo":
            secenek["tablo"] = argv[i + 1]; i += 2
        elif a == "--sinif":
            secenek["sinif"].append(argv[i + 1]); i += 2
        else:
            konum.append(a); i += 1
    return konum, secenek


def main():
    konum, secenek = ayristir(sys.argv[1:])
    if len(konum) < 2:
        print(__doc__); return 2

    yol, karo = konum[0], int(konum[1])
    bosluk = int(konum[2]) if len(konum) > 2 else 0

    im = Image.open(yol).convert("RGBA")
    adim = karo + bosluk
    # Kenney sheetlerinde bosluk karolarin ARASINDA olur, dis kenarda olmaz:
    # sutun sayisi (genislik + bosluk) / (karo + bosluk) ile cikar.
    sut = (im.width + bosluk) // adim
    sat = (im.height + bosluk) // adim
    print(f"{yol}: {im.width}x{im.height}, {karo}px karo, {bosluk}px bosluk -> {sut}x{sat} = {sut*sat} karo")
    if (im.width + bosluk) % adim or (im.height + bosluk) % adim:
        print("!! UYARI: boyutlar izgaraya tam oturmuyor — karo/bosluk yanlis olabilir")

    # --sinif r,g,b=AD ciftleri: kenar pikselleri bu renklere gore siniflanir.
    siniflar = []
    for tanim in secenek["sinif"]:
        renk, ad = tanim.split("=")
        r, g, b = (int(x) for x in renk.split(","))
        siniflar.append(((r, g, b), ad))

    def kes(i):
        x, y = (i % sut) * adim, (i // sut) * adim
        return im.crop((x, y, x + karo, y + karo))

    def kenar_sinifi(px_dizi):
        if not siniflar:
            return "?"
        sayim = {}
        for p in px_dizi:
            if p[3] < 200:
                continue
            for renk, ad in siniflar:
                if abs(p[0]-renk[0]) + abs(p[1]-renk[1]) + abs(p[2]-renk[2]) <= 24:
                    sayim[ad] = sayim.get(ad, 0) + 1
                    break
        if not sayim:
            return "?"
        return max(sayim, key=sayim.get)

    dikissiz = []
    for i in range(sut * sat):
        k = kes(i)
        px = list(k.getdata())
        if any(p[3] < 250 for p in px):
            continue
        renkler = {}
        for p in px:
            renkler[p[:3]] = renkler.get(p[:3], 0) + 1
        baskin, adet = max(renkler.items(), key=lambda kv: kv[1])
        dikissiz.append((i, baskin, adet, len(renkler)))

    print(f"\nTamamen opak karo: {len(dikissiz)}")
    for i, c, n, cesit in dikissiz[:80]:
        satirlar = [str(p) for p in k_kenarlar(kes(i), kenar_sinifi)] if siniflar else []
        ek = f"  kenar(U,A,S,D)={'/'.join(satirlar)}" if satirlar else ""
        print(f"  {i:4d}  baskin rgb{c} x{n}/{karo*karo}, {cesit} renk{ek}")
    if len(dikissiz) > 80:
        print(f"  ... +{len(dikissiz)-80} daha")

    if secenek["tablo"]:
        O = max(2, 640 // (sut * karo)) if sut * karo < 640 else 2
        O = min(O, 8)
        f = Image.new("RGB", (sut * karo * O, sat * karo * O), (40, 40, 55))
        d = ImageDraw.Draw(f)
        for i in range(sut * sat):
            k = kes(i).resize((karo * O, karo * O), Image.NEAREST)
            px_, py_ = (i % sut) * karo * O, (i // sut) * karo * O
            tmp = Image.new("RGBA", k.size, (40, 40, 55, 255)); tmp.alpha_composite(k)
            f.paste(tmp.convert("RGB"), (px_, py_))
            d.rectangle([px_, py_, px_ + karo * O - 1, py_ + karo * O - 1], outline=(255, 0, 255))
            d.text((px_ + 2, py_ + 1), str(i), fill=(255, 255, 0))
        f.save(secenek["tablo"])
        print(f"\nTablo yazildi: {secenek['tablo']}")
    return 0


def k_kenarlar(k, sinifla):
    px = list(k.getdata())
    n = k.width
    ust = px[:n]
    alt = px[-n:]
    sol = px[::n]
    sag = px[n - 1::n]
    return [sinifla(ust), sinifla(alt), sinifla(sol), sinifla(sag)]


if __name__ == "__main__":
    sys.exit(main())
