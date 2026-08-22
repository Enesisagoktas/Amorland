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

        /// <summary>
        /// Duz su. ⚠ 13/14/33 KULLANILMIYOR: iclerindeki turuncu benekler genis su
        /// yuzeyinde gurultu gibi okundu (inceleme bulgusu). Duz 31 + dekor katmaninda
        /// seyrek Dalga + derin bolgede DerinSu.
        /// </summary>
        public static readonly int[] Su = { 31 };

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

        // ⚠ KAHVE CATI KULLANILMIYOR: iki ayri denemede (ahir, market bati evi)
        // kahve kiremit toprak avluyla ton tona kayboldu ve bina "catisiz" gorundu.
        // Yapi zeminleri hep toprak/kaldirim oldugu icin kahve catinin okundugu yer
        // kalmadi; hepsi arduvaz. Tanim ileride cim-komsulu yapi olursa diye duruyor.
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
        /// <summary>Saman yuklu vagon.</summary>
        public const int Vagon = 10;
        /// <summary>Sandik yuklu vagon — trene cesitlilik.</summary>
        public const int VagonKasa = 11;

        // -------------------------------------------------- ince detay (Ek, satir 3-4)

        /// <summary>
        /// Cayir golgelendirme karolari: rpg-base ciminin merkezi koyulastirilmis
        /// halleri. Kenar halkasi orijinal cimle AYNI — dikissiz birlesir.
        /// </summary>
        public const int CimGolgeHafif = 36;
        public const int CimGolgeKoyu = 37;
        /// <summary>Cicek obekleri: beyaz papatya, sari, pembe.</summary>
        public static readonly int[] Cicekler = { 38, 39, 40 };
        public const int Saz = 41;
        public const int Nilufer = 42;
        public const int Cakiltasi = 43;
        /// <summary>Yumusak elips golge — agac/hayvan/esya altina, Dekor katmanina.</summary>
        public const int Golge = 44;
        public const int Kuyu = 45;
        public const int Fener = 46;
        public const int Baca = 47;
        public const int Duman = 48;
        public const int DumanKucuk = 49;
        public const int Sandal = 50;
        public const int CicekTarhi = 51;
        public const int TasYigini = 52;
        public const int Cevher = 53;
        public const int KelebekAcik = 54;
        public const int KelebekKapali = 55;
        /// <summary>Sacak golgesi: kaide pedinin ustune binen solan serit.</summary>
        public const int GolgeSerit = 56;
        public const int DevrikKutuk = 57;
        /// <summary>Tenteli pazar tezgahi.</summary>
        public const int Tezgah = 58;
        public const int AsiliTabela = 59;
        public const int Kumes = 60;
        public const int Yalak = 61;
        public const int MadenVagonu = 62;
        /// <summary>Acik su dalga vurgusu (dekor).</summary>
        public const int Dalga = 63;
        /// <summary>Derin su: duz su karosunun tamami koyu.</summary>
        public const int DerinSu = 64;
        /// <summary>Orman tabani: cimin tamami koyu — genis alanda dikissiz.</summary>
        public const int OrmanTabani = 65;
        /// <summary>Sonbahar zemini: sicak ton — leke ici (66) ve kenari (67).</summary>
        public const int SonbaharIci = 66;
        public const int SonbaharKenari = 67;
        public const int KelebekMaviAcik = 68;
        public const int KelebekMaviKapali = 69;
        /// <summary>Kesilmis odun istifi — oduncu acikligi.</summary>
        public const int OdunIstifi = 70;
        /// <summary>
        /// ⚠ Yesil agaclarin KOYU turevleri — rpg-base yesilleri cimle ayni tonda
        /// ve koyu orman tabaninda tamamen kayboluyorlardi (olcumle yakalandi).
        /// Yesil agac icin HEP bunlar kullanilir; 200/220/221 emeklidir.
        /// </summary>
        public const int KavakKoyu = 71;
        public const int TopKoyu = 72;
        public const int TopInceKoyu = 73;
        /// <summary>
        /// 2 karo boyunda agaclar (alt = govde, ust = kanopi). 2.5D derinligin
        /// tasiyicisi: karakter govdenin arkasindan gecerken Y-sort orter,
        /// kanopi ayri ust katmanda her zaman onde.
        /// </summary>
        public const int UzunKavakAlt = 74;
        public const int UzunKavakUst = 75;
        public const int UzunCamAlt = 76;
        public const int UzunCamUst = 77;
        public const int UzunTuruncuAlt = 78;
        public const int UzunTuruncuUst = 79;
        /// <summary>Cam vitrin — rpg-base cam karolari duvarla ayni renkti.</summary>
        public const int Vitrin = 80;
        /// <summary>Gercek saman yigini — 248 aslinda krem comlekmis.</summary>
        public const int SamanYigini = 81;
        /// <summary>Yesil calilarin koyu turevleri (hayalet cali duzeltmesi).</summary>
        public const int CaliBuyukKoyu = 82;
        public const int CaliKucukKoyu = 83;
        /// <summary>Kontrastli sandik — rpg kasasi toprakta kayboluyordu.</summary>
        public const int SandikKontrast = 84;
        /// <summary>Cift rayli dikey cit karosu.</summary>
        public const int CitDikeyKaro = 85;

        /// <summary>
        /// Cit kose karolari: yatay rpg rayini dikey citle birlestirir (oyuncu
        /// bulgusu: "citlerin kosesi kose seklinde baglanmamis"). Adlandirma cit
        /// dikdortgeninin kosesine gore: GB = guneybati kosesi (raylar doguya +
        /// kuzeye cikar) vb.
        /// </summary>
        public const int CitKoseGB = 91;
        public const int CitKoseGD = 92;
        public const int CitKoseKB = 93;
        public const int CitKoseKD = 94;
        /// <summary>Kopru dosemesi + celik ray birlesimi.</summary>
        public const int KopruRay = 86;
        /// <summary>Ikinci tezgah cesidi.</summary>
        public const int TezgahB = 87;
        /// <summary>Oyun parseli evreleri: ekim aninda filiz, sulaninca islak zemin.</summary>
        public const int Filiz = 88;
        public const int SuruluIslak = 89;
        /// <summary>Hazir/olgun piriltisi (B.18: hazir kaynak PARLAR).</summary>
        public const int Parilti = 90;

        /// <summary>
        /// Oyuncu koyluleri (satir 8-9): 4 cift rengi x (erkek|kadin) x 3 kare.
        /// Kiyafet rengi SPRITE'IN ICINDE — SpriteRenderer tint'i yuzu de boyadigi
        /// icin kaldirildi; renk secimi tulumu/elbiseyi, sapka bandini ve fiyongu boyar.
        /// Kare: 0 durus, 1-2 yuruyus adimlari (flipX ile tam dongu).
        /// </summary>
        public const int OyuncuTaban = 96;

        public static int Oyuncu(bool kadin, int renk, int kare) =>
            OyuncuTaban + Mathf.Clamp(renk, 0, 3) * 6 + (kadin ? 3 : 0) + Mathf.Clamp(kare, 0, 2);

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

        private static Sprite[] _uzunAgaclar;

        /// <summary>
        /// Uzun agac TEK PARCA sprite'i (64x128; 0 kavak, 1 cam, 2 turuncu).
        ///
        /// ⚠ Iki karoya bolunmus surum EMEKLI (74-79): bolme, motorda yatay dikis
        /// ve derinlik hatasi uretiyordu (oyuncu bulgusu — "agaclar ortadan kesik").
        /// Agaclar artik tilemap'e degil ayak-pivotlu GameObject'e gider; kamera
        /// Y-sort'u derinligi dogal cozer. Pivot (0.5, 0.11) = govde tabani.
        /// </summary>
        public static Sprite UzunAgacSprite(int tur)
        {
            if (_uzunAgaclar == null)
            {
                var doku = Resources.Load<Texture2D>("harita2d64/agaclar64");
                if (doku == null)
                {
                    Debug.LogError("[Amorland] Agac sayfasi yok: Resources/harita2d64/agaclar64");
                    return null;
                }
                _uzunAgaclar = new Sprite[3];
                for (int i = 0; i < 3; i++)
                {
                    _uzunAgaclar[i] = Sprite.Create(
                        doku, new Rect(i * KaroPiksel, 0, KaroPiksel, KaroPiksel * 2),
                        new Vector2(0.5f, 0.11f), PikselBirim, 0, SpriteMeshType.FullRect);
                    _uzunAgaclar[i].name = "uzun_agac_" + i;
                }
            }
            return _uzunAgaclar[Mathf.Clamp(tur, 0, 2)];
        }

        private static Sprite _buyukKuyu;

        /// <summary>Buyuk kuyu (64x128, agac sayfasi 4. sutun; ayak pivotlu).
        /// Oyuncu bulgusu: tek karoluk kuyu ana etkilesim nesnesi icin kucuktu.</summary>
        public static Sprite BuyukKuyuSprite()
        {
            if (_buyukKuyu == null)
            {
                var doku = Resources.Load<Texture2D>("harita2d64/agaclar64");
                if (doku == null) return null;
                _buyukKuyu = Sprite.Create(
                    doku, new Rect(3 * KaroPiksel, 0, KaroPiksel, KaroPiksel * 2),
                    new Vector2(0.5f, 0.06f), PikselBirim, 0, SpriteMeshType.FullRect);
                _buyukKuyu.name = "buyuk_kuyu";
            }
            return _buyukKuyu;
        }

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
