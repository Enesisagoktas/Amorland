#!/usr/bin/env python3
"""
Amorland — butunluk denetimi (statik).

B.26'daki yasaklarin bir kismi calisan sisteme sorularak dogrulanir
(sunucu/src/test/java/tr/amorland/ButunlukTest.java). Geri kalani —ozellikle
istemci tarafi olanlar— ancak kaynak kodu taranarak denetlenebilir. Bu script
onu yapar.

Calistir:  python araclar/butunluk-denetimi.py
Cikis:     0 = temiz, 1 = ihlal var
"""

import os
import re
import sys

KOK = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SUNUCU = os.path.join(KOK, "sunucu", "src", "main", "java")
ISTEMCI = os.path.join(KOK, "istemci", "Assets", "Amorland")

bulgular = []
gecenler = []


def dosyalar(kok, uzanti):
    for dizin, _, adlar in os.walk(kok):
        for ad in adlar:
            if ad.endswith(uzanti):
                yield os.path.join(dizin, ad)


def oku(yol):
    with open(yol, encoding="utf-8") as f:
        return f.read()


def gecti(ad, aciklama):
    gecenler.append(f"  OK   {ad}: {aciklama}")


def ihlal(ad, aciklama, yerler):
    bulgular.append((ad, aciklama, yerler))


def ara(kok, uzanti, desen, haric_dosyalar=()):
    """Desenle eslesen (dosya, satirNo, satir) listesi dondurur."""
    sonuc = []
    kalip = re.compile(desen)
    for yol in dosyalar(kok, uzanti):
        if os.path.basename(yol) in haric_dosyalar:
            continue
        for i, satir in enumerate(oku(yol).splitlines(), 1):
            if satir.strip().startswith(("//", "*", "/*", "#")):
                continue
            if kalip.search(satir):
                sonuc.append((os.path.relpath(yol, KOK), i, satir.strip()))
    return sonuc


# ---------------------------------------------------------------- sunucu denetimleri

def sunucu_kural5_saat():
    """Mimari kural 5: tum zaman SUNUCU saatiyle, tek kaynaktan."""
    hits = ara(SUNUCU, ".java", r"\bInstant\.now\(\)|\bLocalDate\.now\(\)|\bSystem\.currentTimeMillis\(\)",
               haric_dosyalar=("Saat.java",))
    if hits:
        ihlal("Kural 5 (sunucu saati)",
              "Zaman dogrudan okunuyor; Saat servisi kullanilmali (testte ileri sarilamaz)",
              hits)
    else:
        gecti("Kural 5", "zaman yalnizca Saat servisinden okunuyor")


def sunucu_kural8_denge():
    """Mimari kural 8: denge JSON'lari yalnizca DengeServisi tarafindan okunur."""
    hits = ara(SUNUCU, ".java", r'"denge/|denge/urunler|denge/uretim|denge/ilerleme',
               haric_dosyalar=("DengeServisi.java",))
    if hits:
        ihlal("Kural 8 (denge tek kapidan)",
              "Denge dosyalari DengeServisi disindan okunuyor",
              hits)
    else:
        gecti("Kural 8", "denge dosyalari yalnizca DengeServisi'nden okunuyor")


def sunucu_kural3_rastgele():
    """Yasak 8: tren fiyati saf rastgele olamaz; oyun sonuclarinda Math.random yok."""
    hits = ara(SUNUCU, ".java", r"Math\.random\(\)|new Random\(\)")
    # SecureRandom kod/tohum uretimi icin mesru; Random oyun sonucunda degil.
    hits = [h for h in hits if "Kodlar.java" not in h[0]]
    if hits:
        ihlal("Yasak 8 (deterministik uretim)",
              "Oyun sonucunda saf rastgelelik; Deterministik.hash01 kullanilmali",
              hits)
    else:
        gecti("Yasak 8", "oyun sonuclari deterministik uretiliyor")


def sunucu_yasak9():
    """Yasak 9: mevsim, enerji bari, premium para yok."""
    hits = ara(SUNUCU, ".java", r"\bmevsim\b|\benerji\b|\bpremium\b", )
    hits += ara(os.path.join(KOK, "sunucu", "src", "main", "resources", "denge"), ".json",
                r'"mevsim"|"enerji"|"premium"')
    if hits:
        ihlal("Yasak 9", "Mevsim / enerji bari / premium para izi", hits)
    else:
        gecti("Yasak 9", "mevsim, enerji bari, premium para yok")


def sunucu_kural1_otorite():
    """Mimari kural 1: istemciden gelen govdede miktar/fiyat/xp alani olmamali."""
    hits = ara(SUNUCU, ".java", r"record\s+\w*(Istek|Niyet)\w*\([^)]*\b(fiyat|xp|altin|odul)\b",
               )
    if hits:
        ihlal("Kural 1 (otorite sunucuda)",
              "Istemci istegi miktar/fiyat/XP alani tasiyor",
              hits)
    else:
        gecti("Kural 1", "istemci istekleri yalnizca niyet tasiyor")


# ---------------------------------------------------------------- istemci denetimleri

def istemci_kural5_saat():
    """Istemci saatine guvenilmez: DateTime.Now yalnizca sunucu farkini olcerken."""
    hits = ara(ISTEMCI, ".cs", r"DateTime\.(Now|UtcNow)",
               haric_dosyalar=("DunyaDurumu.cs",))
    if hits:
        ihlal("Kural 5 (istemci saati)",
              "Yerel saat dogrudan kullaniliyor; DunyaDurumu.SunucuZamani kullanilmali",
              hits)
    else:
        gecti("Kural 5 (istemci)", "zaman karsilastirmalari sunucu saatiyle")


def istemci_kural1_hesaplamaz():
    """Mimari kural 1 ve 7: istemci odul/miktar HESAPLAMAZ."""
    # Istemci kodunda urun miktari veya XP hesabina benzeyen ifade aranir.
    hits = ara(ISTEMCI, ".cs", r"\b(kazanilanXp|odulHesapla|verimHesapla|fiyatHesapla)\b")
    if hits:
        ihlal("Kural 1/7 (istemci hesaplamaz)", "Istemcide odul/miktar hesabi", hits)
    else:
        gecti("Kural 1/7", "istemcide odul veya miktar hesabi yok")


def istemci_yasak5_suruklebirak():
    """Yasak 5: envanterde surukle-birak YASAK (parmak esyayi orter)."""
    envanter_dosyalari = ("CantaEkrani.cs", "DepoEkrani.cs")
    hits = []
    for yol in dosyalar(ISTEMCI, ".cs"):
        if os.path.basename(yol) not in envanter_dosyalari:
            continue
        icerik = oku(yol)
        for desen in (r"IDragHandler", r"IBeginDragHandler", r"OnDrag\s*\("):
            if re.search(desen, icerik):
                hits.append((os.path.relpath(yol, KOK), 0, desen))
    if hits:
        ihlal("Yasak 5", "Envanter ekraninda surukle-birak", hits)
    else:
        gecti("Yasak 5", "envanterde dokun-sec, surukle-birak yok")


def istemci_yasak18_ses_varyasyonu():
    """Yasak 18: perde ve ornek varyasyonu olmadan tekrar eden ses YASAK."""
    yol = os.path.join(ISTEMCI, "Kod", "Ses", "SesYoneticisi.cs")
    if not os.path.exists(yol):
        ihlal("Yasak 18", "SesYoneticisi bulunamadi", [])
        return
    icerik = oku(yol)
    eksik = []
    if "PerdeVaryasyonu" not in icerik:
        eksik.append("perde varyasyonu")
    if "VaryasyonSayisi" not in icerik:
        eksik.append("ornek varyasyonu")
    if "EtiketBasinaMaksKanal" not in icerik:
        eksik.append("eszamanlilik siniri")
    if "KuyrukSesSeviyesi" not in icerik:
        eksik.append("kuyrukta ses alcalmasi")
    if eksik:
        ihlal("Yasak 18", "Eksik onlem: " + ", ".join(eksik), [(os.path.relpath(yol, KOK), 0, "")])
    else:
        gecti("Yasak 18", "perde + ornek varyasyonu + kanal siniri + kuyruk alcalmasi var")


def yorumsuz(icerik):
    """Yorum satirlarini atar. Yorumdaki 'Instantiate YOK' ifadesi ihlal degildir."""
    satirlar = []
    for satir in icerik.splitlines():
        s = satir.strip()
        if s.startswith(("//", "*", "/*", "///")):
            continue
        satirlar.append(satir.split("//")[0])
    return "\n".join(satirlar)


def istemci_ses_havuzu():
    """B.23: ses basina Instantiate YOK, 16 kaynakli havuz."""
    yol = os.path.join(ISTEMCI, "Kod", "Ses", "SesYoneticisi.cs")
    icerik = yorumsuz(oku(yol)) if os.path.exists(yol) else ""
    if "Instantiate" in icerik:
        ihlal("B.23 ses havuzu", "Ses calarken Instantiate kullaniliyor",
              [(os.path.relpath(yol, KOK), 0, "")])
    elif "HavuzBoyutu" in icerik:
        gecti("B.23 ses havuzu", "sabit havuz, ses basina Instantiate yok")
    else:
        ihlal("B.23 ses havuzu", "Havuz bulunamadi", [(os.path.relpath(yol, KOK), 0, "")])


def istemci_yasak17_sadece_ses_degil():
    """Yasak 17: hicbir mekanik yalnizca sesle bildirilmez."""
    yol = os.path.join(ISTEMCI, "Kod", "Oyun.cs")
    icerik = oku(yol) if os.path.exists(yol) else ""
    kritik = ["alet_kirildi", "depo_doldu", "seviye_atlama"]
    eksik = []
    for olay in kritik:
        # Olayin islendigi blokta hem ses hem gorsel geri bildirim olmali.
        blok = re.search(r'case\s+"' + olay + r'"\s*:(.{0,400}?)break;', icerik, re.S)
        if not blok:
            eksik.append(olay + " (islenmiyor)")
            continue
        govde = blok.group(1)
        gorsel = ("Bildirimler" in govde or "_bildirimler" in govde
                  or "_seviyeEkrani" in govde or "Goster" in govde)
        if not gorsel:
            eksik.append(olay + " (gorsel karsiligi yok)")
    if eksik:
        ihlal("Yasak 17", "Yalnizca sesle bildirilen mekanik: " + ", ".join(eksik),
              [(os.path.relpath(yol, KOK), 0, "")])
    else:
        gecti("Yasak 17", "kritik olaylarin hepsinde gorsel karsilik var")


def istemci_yasak16_muzik_kesme():
    """Yasak 16: oyuncunun muzigini/podcast'ini kesme."""
    yol = os.path.join(ISTEMCI, "Editor", "AmorlandKurulum.cs")
    icerik = oku(yol) if os.path.exists(yol) else ""
    if "muteOtherAudioSources = false" in icerik:
        gecti("Yasak 16", "iOS 'Mute Other Audio Sources' kapatiliyor")
    else:
        ihlal("Yasak 16", "muteOtherAudioSources = false ayarlanmiyor",
              [(os.path.relpath(yol, KOK), 0, "")])


def istemci_haptik():
    """B.23: Handheld.Vibrate tek basina yetmez."""
    hits = ara(ISTEMCI, ".cs", r"Handheld\.Vibrate\(\)")
    yol = os.path.join(ISTEMCI, "Kod", "Haptik", "Haptik.cs")
    icerik = oku(yol) if os.path.exists(yol) else ""
    desen_var = "VibrationEffect" in icerik and "_AmorlandHaptik" in icerik
    if hits and not desen_var:
        ihlal("B.23 haptik", "Yalnizca Handheld.Vibrate kullaniliyor, desen uretilemez", hits)
    elif desen_var:
        gecti("B.23 haptik", "Android VibrationEffect + iOS UIFeedbackGenerator")
    else:
        ihlal("B.23 haptik", "Platform haptigi bulunamadi", [(os.path.relpath(yol, KOK), 0, "")])


def istemci_yaw_kilidi():
    """B.5: yaw KILITLI. Kamerayi donduren tek satir bile kaldiraci yok eder."""
    yol = os.path.join(ISTEMCI, "Kod", "Gorsel", "KameraYoneticisi.cs")
    icerik = oku(yol) if os.path.exists(yol) else ""
    if "Quaternion.Euler(Pitch, 0f, 0f)" in icerik:
        gecti("B.5 yaw kilidi", "kamera rotasyonu her karede yaw=0'a sabitleniyor")
    else:
        ihlal("B.5 yaw kilidi", "Kamera yaw'i sabitlenmiyor", [(os.path.relpath(yol, KOK), 0, "")])


def istemci_cevrimdisi_sinir():
    """Yasak 23: paylasilan duruma cevrimdisi dokunulmaz."""
    yol = os.path.join(ISTEMCI, "Kod", "Ag", "BaglantiYoneticisi.cs")
    icerik = oku(yol) if os.path.exists(yol) else ""
    m = re.search(r"IzinliCevrimdisi\s*=\s*new HashSet<string>\s*\{([^}]*)\}", icerik, re.S)
    if not m:
        ihlal("Yasak 23", "Cevrimdisi izin listesi bulunamadi", [(os.path.relpath(yol, KOK), 0, "")])
        return
    izinli = set(re.findall(r'"(\w+)"', m.group(1)))
    yasak_olmali = {"depoya_koy", "depodan_al", "vur", "craft", "ortak_proje_katki"}
    kacak = izinli & yasak_olmali
    if kacak:
        ihlal("Yasak 23", "Paylasilan duruma dokunan eylem cevrimdisi izinli: " + ", ".join(kacak),
              [(os.path.relpath(yol, KOK), 0, "")])
    elif izinli == {"ek", "sula", "hasat", "topla"}:
        gecti("Yasak 23", "cevrimdisi yalnizca kendi parselindeki eylemler")
    else:
        ihlal("Yasak 23", "Beklenmeyen izin listesi: " + ", ".join(sorted(izinli)),
              [(os.path.relpath(yol, KOK), 0, "")])


def istemci_denge_gomulu_degil():
    """Mimari kural 8: istemcide denge sayisi gomulu olmamali."""
    yol = os.path.join(ISTEMCI, "Kod", "Durum", "DengeVerisi.cs")
    icerik = oku(yol) if os.path.exists(yol) else ""
    # Varsayilan degerler mesru (sunucuya ulasilamadiginda cizim bozulmasin) ama
    # urun fiyati / buyume suresi gibi tablolar gomulu olmamali.
    hits = ara(ISTEMCI, ".cs", r'"bugday".*buyumeSn|tabanFiyat\s*=\s*\d+')
    if hits:
        ihlal("Kural 8 (istemci)", "Istemcide gomulu denge tablosu", hits)
    else:
        gecti("Kural 8 (istemci)", "denge sunucudan geliyor, gomulu tablo yok")


# ---------------------------------------------------------------- calistir

def main():
    print()
    print("=" * 72)
    print("AMORLAND — BUTUNLUK DENETIMI (statik)")
    print("=" * 72)

    for f in (
        sunucu_kural5_saat,
        sunucu_kural8_denge,
        sunucu_kural3_rastgele,
        sunucu_yasak9,
        sunucu_kural1_otorite,
        istemci_kural5_saat,
        istemci_kural1_hesaplamaz,
        istemci_yasak5_suruklebirak,
        istemci_yasak18_ses_varyasyonu,
        istemci_ses_havuzu,
        istemci_yasak17_sadece_ses_degil,
        istemci_yasak16_muzik_kesme,
        istemci_haptik,
        istemci_yaw_kilidi,
        istemci_cevrimdisi_sinir,
        istemci_denge_gomulu_degil,
    ):
        f()

    print()
    for satir in gecenler:
        print(satir)

    if bulgular:
        print()
        print("-" * 72)
        print(f"IHLAL: {len(bulgular)}")
        print("-" * 72)
        for ad, aciklama, yerler in bulgular:
            print(f"\n  ! {ad}: {aciklama}")
            for yol, satirNo, metin in yerler[:6]:
                yer = f"{yol}:{satirNo}" if satirNo else yol
                print(f"      {yer}  {metin[:90]}")
            if len(yerler) > 6:
                print(f"      ... {len(yerler) - 6} yer daha")
        print()
        return 1

    print()
    print(f"Temiz: {len(gecenler)} denetimin hepsi gecti.")
    print("Not: calisan sisteme sorulan denetimler ButunlukTest.java icinde (22 test).")
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
