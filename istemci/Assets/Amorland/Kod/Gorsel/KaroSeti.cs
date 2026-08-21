using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>Karo sayfalari. Ucu de Kenney CC0, ucu de 16x16 ve AYNI palet.</summary>
    public enum Sayfa
    {
        /// <summary>Tiny Farm — ekin, hayvan, ciftlik esyasi.</summary>
        Ciftlik,
        /// <summary>Tiny Town — cim/yol arazisi, agac, cit, yapi, maden agzi.</summary>
        Kasaba,
        /// <summary>Tiny Battle — su ve kiyi autotile karolari.</summary>
        Su,
    }

    /// <summary>Bir karonun kimligi: hangi sayfanin kacinci karosu.</summary>
    public readonly struct KaroKimlik
    {
        public readonly Sayfa Sayfa;
        public readonly int Indeks;
        public KaroKimlik(Sayfa sayfa, int indeks) { Sayfa = sayfa; Indeks = indeks; }
        public bool Bos => Indeks < 0;
        public static readonly KaroKimlik Yok = new KaroKimlik(Sayfa.Kasaba, -1);
    }

    /// <summary>
    /// Uc Kenney karo sayfasini yukler, dilimler ve karolara ISIM verir.
    ///
    /// ⚠ Neden uc sayfa: Tiny Farm kendi arka planini cim olarak cizer — icinde
    /// dikissiz dosenebilecek TEK bir zemin karosu yoktur. Cim sandigim 105/107/117/119
    /// karolarinin dordunde de cepecevre koyu kontur (rgb 63,38,49) var; doseyince
    /// harita bastan basa koyu izgara cikti. Olcum: sayfada saf cim 16x16 pencere
    /// sayisi = 0.
    ///
    /// Cozum ayni sanatcinin diger iki setinden geldi:
    ///   Tiny Town   -> karo 0 = %100 saf cim, 12..38 arasi tam bir 3x3 yol autotile
    ///   Tiny Battle -> 18..56 arasi tam bir 3x3 su/kiyi autotile
    /// Uc sayfada da cim rengi TAM OLARAK rgb(132,198,105). Bu yuzden renk
    /// normalizasyonu gerekmez; sayfalar dikissiz birlesir.
    ///
    /// ⚠ Ham indeks numarasi oyun kodunda GECMEZ. "karo 103" alti ay sonra hicbir sey
    /// ifade etmez; asagidaki adlar eder. Set degisirse yalnizca bu dosya degisir.
    ///
    /// Indeks tablolari araclar/karo-indeks/ altindaki etiketli goruntulerden OKUNDU,
    /// tahmin edilmedi. O klasordeki OKU.md hangi tahminin neden tuttugunu yazar.
    /// </summary>
    public static class KaroSeti
    {
        public const int KaroPiksel = 16;
        /// <summary>Bir karo = 1 dunya birimi. PPU karo boyutuna esit olmali.</summary>
        public const float PikselBirim = 16f;

        private static readonly string[] Yollar =
        {
            "harita2d/tinyfarm",   // Sayfa.Ciftlik
            "harita2d/kasaba",     // Sayfa.Kasaba
            "harita2d/su",         // Sayfa.Su
        };

        private static readonly Sprite[][] _karolar = new Sprite[3][];

        // ------------------------------------------------------------ arazi (Kasaba)

        public static readonly KaroKimlik CimDuz   = new KaroKimlik(Sayfa.Kasaba, 0);
        public static readonly KaroKimlik CimOt    = new KaroKimlik(Sayfa.Kasaba, 1);
        public static readonly KaroKimlik CimCicek = new KaroKimlik(Sayfa.Kasaba, 2);
        /// <summary>
        /// ⚠ Cakil bir YAMA karosudur: cim zemin uzerine serpilmis tas. Genis alana
        /// doseyince duzenli puantiye deseni cikar (meydan tamamen duvar kagidi gibi
        /// gorundu). Tek tuk serpilir, dolgu olarak KULLANILMAZ.
        /// </summary>
        public static readonly KaroKimlik CakilYama = new KaroKimlik(Sayfa.Kasaba, 43);

        /// <summary>Saf acik tas — meydan, peron, maden zemini. %100 tek renk, dikissiz.</summary>
        public static readonly KaroKimlik TasZemin = new KaroKimlik(Sayfa.Kasaba, 109);

        /// <summary>Dolu toprak cesitlemesi — yolun icini tekduzelikten kurtarir.</summary>
        public static readonly int[] ToprakDolu = { 25, 39, 40, 41, 42 };

        /// <summary>
        /// Yol 3x3 autotile blogu (Tiny Town). [satir, sutun]:
        /// satir 0 = cim KUZEYDE, satir 2 = cim GUNEYDE, sutun 0 = cim BATIDA.
        /// Olcum: her karonun dort kenari cim/toprak diye tek tek sayildi.
        /// </summary>
        public static readonly int[,] YolBlok =
        {
            { 12, 13, 14 },
            { 24, 25, 26 },
            { 36, 37, 38 },
        };

        /// <summary>Su 3x3 autotile blogu (Tiny Battle), YolBlok ile ayni duzen.</summary>
        public static readonly int[,] SuBlok =
        {
            { 18, 19, 20 },
            { 36, 37, 38 },
            { 54, 55, 56 },
        };

        /// <summary>Acik su cesitlemesi: kopuk, nilufer.</summary>
        public static readonly int[] SuAcik = { 37, 39, 72, 74, 75, 90, 91, 92, 93 };

        // ------------------------------------------------------------ bitki (Kasaba)

        public static readonly int[] Camlar   = { 4, 16, 28 };
        public static readonly int[] AgacObek = { 6, 7, 8, 19, 20, 30, 31, 32 };
        public static readonly int[] Bugday   = { 3, 15, 27 };
        public const int Cali    = 5;
        public const int OtTutam = 17;
        public const int Mantar  = 29;

        // ------------------------------------------------------------ cit (Kasaba)

        /// <summary>Dikey direk + korkuluk.</summary>
        public const int CitDikey = 56;
        /// <summary>Duz yatay korkuluk. ⚠ 68 sanmistim; 68 KOSE parcasi cikti.</summary>
        public const int CitYatay = 81;
        /// <summary>Ust-sag kose.</summary>
        public const int CitKose  = 68;
        /// <summary>Iki direk arasi korkuluk — cit boyunca aralikli serpilir.</summary>
        public const int CitDirek = 69;
        /// <summary>Fenerli tabela — kapi yerine gecer.</summary>
        public const int CitKapi  = 57;
        /// <summary>Ayakli yon tabelasi — bolge girislerine konur.</summary>
        public const int Tabela   = 83;

        // ------------------------------------------------------------ yapi (Kasaba)

        /// <summary>Mavi catili ev: [satir, sutun], satir 0 ustteki (catili) sira.</summary>
        public static readonly int[,] EvMavi     = { { 48, 49, 50, 51 }, { 60, 61, 62, 63 } };
        public static readonly int[,] EvKirmizi  = { { 52, 53, 54, 55 }, { 64, 65, 66, 67 } };
        public static readonly int[,] EvAhsap    = { { 72, 73, 74, 75 }, { 84, 85, 86, 87 } };
        public static readonly int[,] EvTas      = { { 76, 77, 78, 79 }, { 88, 89, 90, 91 } };

        /// <summary>Maden agzi: 2x2 kemerli kara giris.</summary>
        public static readonly int[,] MadenAgzi = { { 111, 112 }, { 123, 124 } };
        /// <summary>Tas duvar 3x3 — tren garinin govdesi.</summary>
        public static readonly int[,] TasDuvar =
        {
            {  96,  97,  98 },
            { 108, 109, 110 },
            { 120, 121, 122 },
        };

        /// <summary>⚠ 92 tezgah DEGIL, toprak yigini. Buyuk basilip bakildi.</summary>
        public const int ToprakYigini = 92;
        public const int Ekmek  = 93;
        public const int Kovan  = 94;
        public const int Bayrak = 95;
        public const int Kutuk  = 106;
        public const int Sepet  = 107;
        public const int Kazma  = 115;
        public const int Yaba   = 116;
        /// <summary>Miğferli kasabali — bolgelerde canlilik icin.</summary>
        public const int Koylu  = 104;

        // ------------------------------------------------------------ ciftlik (Ciftlik)

        /// <summary>Ekin sutunlari: her deger bir urun, genc/olgun ayri dizide.</summary>
        public static readonly int[] UrunGenc  = { 5, 17, 29, 41, 53, 65 };
        public static readonly int[] UrunOlgun = { 6, 18, 30, 42, 54, 66 };
        /// <summary>Surulmus parsel — Tiny Farm kendi cim zeminiyle getirir.</summary>
        public static readonly int[] Parsel = { 48, 49, 50, 51, 60, 61, 62, 63 };
        public static readonly int[] Cuval  = { 9, 21, 33, 45, 57, 69 };
        public static readonly int[] Kasa   = { 11, 23, 35, 47, 59, 71 };
        public static readonly int[] Hayvan = { 120, 121, 122, 123 };
        public static readonly int[] Kaya   = { 77, 89 };
        public const int Ciftci      = 109;
        public const int CiftciKadin = 108;
        public const int SamanBalya  = 96;
        public const int KovaDolu    = 73;
        public const int Varil       = 124;

        /// <summary>Ahir 4 satir x 3 sutun.</summary>
        public static readonly int[,] Ahir =
        {
            {  90,  91,  92 },
            { 102, 103, 104 },
            { 114, 115, 116 },
            { 126, 127, 128 },
        };

        // ------------------------------------------------------------ yukleme

        public static bool SetVar => Sprites(Sayfa.Kasaba) != null;

        private static Sprite[] Sprites(Sayfa sayfa)
        {
            int s = (int)sayfa;
            if (_karolar[s] != null) return _karolar[s];

            var doku = Resources.Load<Texture2D>(Yollar[s]);
            if (doku == null)
            {
                Debug.LogWarning("[Amorland] Karo sayfasi yok: Resources/" + Yollar[s]);
                return null;
            }

            // ⚠ Nokta filtre SART: 16 px piksel sanati bilineer filtreyle bulaniklasir.
            doku.filterMode = FilterMode.Point;
            doku.wrapMode = TextureWrapMode.Clamp;

            int sut = doku.width / KaroPiksel;
            int sat = doku.height / KaroPiksel;
            var dizi = new Sprite[sut * sat];

            for (int y = 0; y < sat; y++)
            {
                for (int x = 0; x < sut; x++)
                {
                    // ⚠ Sayfa ustten asagi dizilir ama Unity dokuyu ALTTAN sayar.
                    // Bu cevirme yapilmazsa harita dikey olarak ters cikar.
                    int py = doku.height - (y + 1) * KaroPiksel;
                    int i = y * sut + x;
                    dizi[i] = Sprite.Create(
                        doku, new Rect(x * KaroPiksel, py, KaroPiksel, KaroPiksel),
                        new Vector2(0.5f, 0.5f), PikselBirim, 0, SpriteMeshType.FullRect);
                    dizi[i].name = sayfa + "_" + i;
                }
            }

            _karolar[s] = dizi;
            return dizi;
        }

        public static Sprite Al(KaroKimlik karo)
        {
            if (karo.Bos) return null;
            Sprite[] d = Sprites(karo.Sayfa);
            if (d == null || karo.Indeks >= d.Length) return null;
            return d[karo.Indeks];
        }

        public static Sprite Al(Sayfa sayfa, int indeks) => Al(new KaroKimlik(sayfa, indeks));

        // ------------------------------------------------------------ autotile

        /// <summary>
        /// 3x3 autotile secimi. Komsu bayraklari "ayni malzeme mi" sorusunun cevabi.
        ///
        /// ⚠ Bu set ic kose ve tek karo genisliginde serit karosu ICERMEZ. Yol ve nehir
        /// en az 2-3 karo genisliginde cizilmeli; 1 karo genislikte hem bati hem dogu
        /// farkli olur ve set bunu karsilayamaz.
        /// </summary>
        public static int Dokuz(int[,] blok, bool kuzey, bool guney, bool bati, bool dogu)
        {
            int satir = !kuzey ? 0 : (!guney ? 2 : 1);
            int sutun = !bati ? 0 : (!dogu ? 2 : 1);
            return blok[satir, sutun];
        }

        /// <summary>Diziden rastgele indeks — cesitleme icin.</summary>
        public static int Sec(int[] secenek, System.Random r) => secenek[r.Next(secenek.Length)];

        public static void Unut()
        {
            for (int i = 0; i < _karolar.Length; i++) _karolar[i] = null;
        }
    }
}
