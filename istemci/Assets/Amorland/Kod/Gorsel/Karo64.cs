using System.Collections.Generic;
using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>64px karo sayfalari. Ikisi de ayni paletten (rpg-base + ondan turetilen ek).</summary>
    public enum Sayfa64
    {
        /// <summary>Kenney RPG Base (CC0): arazi, agac, cit, bina, cati, kapi/pencere.</summary>
        RpgBase,
        /// <summary>uret64.py ciktisi: ekin, hayvan, koylu, ray, kalas, kaldirim.
        /// Renkleri rpg-base sayfasindan ORNEKLENEREK cizildi — palet ayni.</summary>
        Ek,
    }

    /// <summary>Bir 64px karonun kimligi: sayfa + indeks.</summary>
    public readonly struct Karo64Kimlik
    {
        public readonly Sayfa64 Sayfa;
        public readonly int Indeks;
        public Karo64Kimlik(Sayfa64 sayfa, int indeks) { Sayfa = sayfa; Indeks = indeks; }
        public bool Bos => Indeks < 0;
    }

    /// <summary>
    /// 64px karo setinin yukleyicisi ve adlari.
    ///
    /// ⚠ Bu set PIKSEL SANATI DEGIL — rpg-base yumusak gecisli vektor cizimdir.
    /// Nokta filtre burada kirli gorunur; Bilinear + mipmap sart. Ama atlasta yan yana
    /// duran karolar Bilinear'da birbirine SIZAR (cimin kenarindan toprak gorunur).
    /// Cozum: sayfa yuklenirken her karo, kenar pikselleri disari TASIRILARAK
    /// (extrude) 8 px oluklu yeni bir atlasa kopyalanir. Mip zinciri 4 seviyeyle
    /// sinirli — 8 px oluk ancak o kadarini guvenle tasir.
    ///
    /// ⚠ Ham indeks oyun kodunda GECMEZ; asagidaki adlar gecer. Indeksler
    /// araclar/karo-indeks/rpgbase-tablo.png ve ek64-kontak.png uzerinden OKUNDU.
    /// </summary>
    public static class Karo64
    {
        public const int KaroPiksel = 64;
        /// <summary>Bir karo = 1 dunya birimi.</summary>
        public const float PikselBirim = 64f;

        private const int Oluk = 8;

        // ------------------------------------------------------------ arazi (RpgBase)

        /// <summary>Duz cim cesitleri. Cogunluk sade (43), tutamli olanlar serpilir.</summary>
        public static readonly int[] Cim = { 43, 43, 43, 3, 23, 44 };
        public static readonly int[] CimDetay = { 4, 24 };

        /// <summary>Duz toprak cesitleri (yol/avlu ici).</summary>
        public static readonly int[] Toprak = { 8, 9, 28, 29 };

        /// <summary>Duz su cesitleri (golun ortasi).</summary>
        public static readonly int[] Su = { 13, 14, 33 };
        public const int SuAdacik = 34;

        /// <summary>
        /// Toprak 3x3 blogu: toprak adasi, kenarlarinda CIM zemin gomulu.
        /// [satir 0] = cim kuzeyde. Yol, avlu, tarla tabani hep bu bloktan.
        /// </summary>
        public static readonly int[,] ToprakBlok =
        {
            {  5,  6,  7 },
            { 25, 26, 27 },
            { 45, 46, 47 },
        };

        /// <summary>Su 3x3 blogu: su adasi, kiyi seridi gomulu, cim zeminde.</summary>
        public static readonly int[,] SuBlok =
        {
            { 10, 11, 12 },
            { 30, 31, 32 },
            { 50, 51, 52 },
        };

        // ------------------------------------------------------------ bitki (RpgBase)

        /// <summary>Sivri kavak tipi agaclar: yesil / turuncu / cam yesili.</summary>
        public const int KavakYesil = 200;
        public const int KavakTuruncu = 202;
        public const int KavakCam = 204;
        /// <summary>Yuvarlak tepeli agaclar.</summary>
        public const int TopYesil = 220;
        public const int TopYesilInce = 221;
        public const int TopTuruncu = 222;
        public const int TopTuruncuInce = 223;
        public const int TopCam = 224;
        public const int TopCamInce = 225;
        /// <summary>Kucuk fidanlar.</summary>
        public static readonly int[] Fidan = { 201, 203, 205 };
        /// <summary>Calilar: buyuk 180/182/184, kucuk 181/183/185.</summary>
        public static readonly int[] CaliBuyuk = { 180, 182, 184 };
        public static readonly int[] CaliKucuk = { 181, 183, 185 };

        // ------------------------------------------------------------ prop (RpgBase)

        public const int CitSolBas = 206;
        public const int CitSagBas = 207;
        public static readonly int[] CitOrta = { 226, 227 };
        public const int CitTek = 246;
        public const int CitTek2 = 247;
        public const int Merdiven = 186;
        public const int KasaBuyuk = 188;
        public const int KasaKucuk = 189;
        public static readonly int[] Varil = { 208, 228 };
        public static readonly int[] Comlek = { 209, 229 };
        public const int SamanYigin = 248;
        public const int GriVaril = 249;

        // ------------------------------------------------------------ yapi (RpgBase)
        //
        // Uc malzeme (krem / gri tas / mavi tas), uc cati (kahve / arduvaz / acik tas).
        // Ev kompozisyonu Karo64Yapi.EvInsa icinde; buradaki adlar ham parcalar.

        public struct Duvar
        {
            public int UstSol, UstOrta, UstSag;
            public int AltSol, AltOrta, AltSag;
            public int Kemer;
        }

        public struct Cati
        {
            public int TepeSol, TepeOrta, TepeSag;
            public int OrtaSol, OrtaOrta, OrtaSag;
            public int EtekSol, EtekOrta, EtekSag;
        }

        // ⚠ Alt siralar OLCULDU (araclar/karo-indeks/duvar-alt-sira.png): 100/101/102
        // krem kaide; 109/110/112 tas kaide (111 ORTA, 112 sag kenar — ilk tahmin
        // 111'i sag sanmisti); 113-115 kemerli arkad, kaide DEGIL. Mavi govdenin
        // kendine ait kaidesi yok — tas kaideyi paylasir.
        public static readonly Duvar DuvarKrem = new Duvar
        { UstSol = 60, UstOrta = 61, UstSag = 62, AltSol = 100, AltOrta = 101, AltSag = 102, Kemer = 104 };
        public static readonly Duvar DuvarGri = new Duvar
        { UstSol = 69, UstOrta = 70, UstSag = 71, AltSol = 109, AltOrta = 110, AltSag = 112, Kemer = 113 };
        public static readonly Duvar DuvarMavi = new Duvar
        { UstSol = 73, UstOrta = 74, UstSag = 75, AltSol = 109, AltOrta = 110, AltSag = 112, Kemer = 113 };

        // ⚠ Iki olcumden ders: (a) govde TEK karodan (145/154) doser — kivrimli
        // paneller (140/142) kenarda dikey dikis uretti; (b) etek 164/165/166 —
        // 162'yi etek sagi sanmistim, 3x buyutmede ucgen ALINLIK yarisi cikti ve
        // catinin sag ucu capraz kesiliyordu (araclar/karo-indeks/kiremit-detay.png).
        public static readonly Cati CatiKahve = new Cati
        {
            TepeSol = 145, TepeOrta = 145, TepeSag = 145,
            OrtaSol = 145, OrtaOrta = 145, OrtaSag = 145,
            EtekSol = 164, EtekOrta = 165, EtekSag = 166,
        };
        public static readonly Cati CatiArduvaz = new Cati
        {
            TepeSol = 154, TepeOrta = 154, TepeSag = 154,
            OrtaSol = 154, OrtaOrta = 154, OrtaSag = 154,
            EtekSol = 173, EtekOrta = 174, EtekSag = 175,
        };

        /// <summary>Kapi ve pencereler (koyu bosluk uzerine cizilmis, saydam kenarli).</summary>
        public const int KapiBuyukKahve = 214;
        public const int KapiKemerKahve = 234;
        public const int PencereKahve = 215;
        public const int PencereCiftKahve = 210;
        public const int PencereKucuk = 213;
        public const int DukkanCamKrem = 167;
        public const int DukkanCamGri = 176;

        // ------------------------------------------------------------ ek sayfa

        public static readonly int[] Kaldirim = { 0, 1 };
        public const int Surulu = 2;
        public const int RayYatay = 3;
        public const int RayDikey = 4;
        public const int KalasYatay = 5;
        public const int KalasDikey = 6;
        public const int YonTabelasi = 7;
        /// <summary>Ekinler: bugday, domates, balkabagi, havuc. Ayni sirayla genc/olgun.</summary>
        public static readonly int[] UrunGenc = { 12, 13, 14, 15 };
        public static readonly int[] UrunOlgun = { 16, 17, 18, 19 };
        public const int Inek = 24;
        public const int Koyun = 25;
        public const int Tavuk = 26;
        public const int KoyluErkek = 27;
        public const int KoyluKadin = 28;
        /// <summary>Tek ahsap direk — rpg-base'de DIKEY cit yok, dikey kenarlar bununla.</summary>
        public const int CitDirek = 8;
        /// <summary>Yandan bakisli lokomotif ve yuk vagonu — hatta gezen tren.</summary>
        public const int Lokomotif = 9;
        public const int Vagon = 10;

        // ------------------------------------------------------------ yukleme

        private static readonly string[] Yollar = { "harita2d64/rpgbase", "harita2d64/ek64" };
        private static readonly Sprite[][] _karolar = new Sprite[2][];
        private static readonly Texture2D[] _atlaslar = new Texture2D[2];
        private static readonly bool[] _uyarildi = new bool[2];

        public static bool SetVar => Sprites(Sayfa64.RpgBase) != null;

        private static Sprite[] Sprites(Sayfa64 sayfa)
        {
            int s = (int)sayfa;
            if (_karolar[s] != null) return _karolar[s];

            var kaynak = Resources.Load<Texture2D>(Yollar[s]);
            if (kaynak == null)
            {
                // ⚠ TEK uyari: bu yol her karo istendiginde denenir; sayfa eksikken
                // 160 binden fazla ayni satir loglanip render'i surundurdu.
                if (!_uyarildi[s])
                {
                    _uyarildi[s] = true;
                    Debug.LogError("[Amorland] 64px karo sayfasi yok: Resources/" + Yollar[s]);
                }
                return null;
            }

            int sut = kaynak.width / KaroPiksel;
            int sat = kaynak.height / KaroPiksel;
            int adim = KaroPiksel + Oluk * 2;

            // ⚠ Extrude atlasi: her karo kendi hucresine, kenar pikselleri oluklara
            // KOPYALANARAK yazilir. Olukssuz atlasta Bilinear ornekleme komsu karodan
            // renk tasir — cim karosunun kenarinda toprak cizgisi belirir.
            var atlas = new Texture2D(sut * adim, sat * adim, TextureFormat.RGBA32, 4, false);
            atlas.name = Yollar[s] + "-extrude";
            atlas.filterMode = FilterMode.Bilinear;
            atlas.wrapMode = TextureWrapMode.Clamp;

            Color32[] kpx = kaynak.GetPixels32();
            var apx = new Color32[atlas.width * atlas.height];

            for (int ty = 0; ty < sat; ty++)
            {
                for (int tx = 0; tx < sut; tx++)
                {
                    int kx0 = tx * KaroPiksel, ky0 = ty * KaroPiksel;
                    int ax0 = tx * adim, ay0 = ty * adim;
                    for (int y = -Oluk; y < KaroPiksel + Oluk; y++)
                    {
                        int sy = Mathf.Clamp(y, 0, KaroPiksel - 1) + ky0;
                        int hedefSatir = (ay0 + y + Oluk) * atlas.width;
                        int kaynakSatir = sy * kaynak.width;
                        for (int x = -Oluk; x < KaroPiksel + Oluk; x++)
                        {
                            int sx = Mathf.Clamp(x, 0, KaroPiksel - 1) + kx0;
                            apx[hedefSatir + ax0 + x + Oluk] = kpx[kaynakSatir + sx];
                        }
                    }
                }
            }

            atlas.SetPixels32(apx);
            atlas.Apply(true, true);
            _atlaslar[s] = atlas;

            var dizi = new Sprite[sut * sat];
            for (int y = 0; y < sat; y++)
            {
                for (int x = 0; x < sut; x++)
                {
                    // Kaynak sayfa ustten sayilir, Unity dokuyu alttan sayar — cevir.
                    int py = atlas.height - (y + 1) * adim + Oluk;
                    int i = y * sut + x;
                    dizi[i] = Sprite.Create(
                        atlas, new Rect(x * adim + Oluk, py, KaroPiksel, KaroPiksel),
                        new Vector2(0.5f, 0.5f), PikselBirim, 0, SpriteMeshType.FullRect);
                    dizi[i].name = sayfa + "_" + i;
                }
            }

            _karolar[s] = dizi;
            return dizi;
        }

        public static Sprite Al(Karo64Kimlik karo)
        {
            if (karo.Bos) return null;
            Sprite[] d = Sprites(karo.Sayfa);
            if (d == null || karo.Indeks >= d.Length) return null;
            return d[karo.Indeks];
        }

        public static Sprite Al(Sayfa64 sayfa, int indeks) => Al(new Karo64Kimlik(sayfa, indeks));

        /// <summary>3x3 blok autotile secimi. Bayrak = "komsu ayni malzeme mi".</summary>
        public static int Dokuz(int[,] blok, bool kuzey, bool guney, bool bati, bool dogu)
        {
            int satir = !kuzey ? 0 : (!guney ? 2 : 1);
            int sutun = !bati ? 0 : (!dogu ? 2 : 1);
            return blok[satir, sutun];
        }

        public static int Sec(int[] secenek, System.Random r) => secenek[r.Next(secenek.Length)];

        public static void Unut()
        {
            for (int i = 0; i < _karolar.Length; i++)
            {
                _karolar[i] = null;
                _uyarildi[i] = false;
                if (_atlaslar[i] != null)
                {
                    // ⚠ Editor araclari Play modu disinda cagirir; orada Destroy yasak.
                    if (Application.isPlaying) Object.Destroy(_atlaslar[i]);
                    else Object.DestroyImmediate(_atlaslar[i]);
                    _atlaslar[i] = null;
                }
            }
        }
    }
}
