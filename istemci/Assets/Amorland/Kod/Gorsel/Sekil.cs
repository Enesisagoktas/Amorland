using System.Collections.Generic;
using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>
    /// UI icin kod ile uretilen sprite'lar: yuvarlak kose, daire, halka, yumusak golge.
    ///
    /// Neden kod: projede sanatci ve .png varligi yok (A.7). Unity'nin ciplak
    /// <c>Image</c> bileseni sprite'siz kullanildiginda keskin koseli dikdortgen cizer —
    /// arayuzun "ucuz" gorunmesinin tek sebebi buydu. Burada uretilen 9-dilim
    /// sprite'lar her boyutta bozulmadan olceklenir, tek seferlik uretilip onbellege
    /// alinir ve dosya sistemine hicbir sey yazmaz.
    ///
    /// ⚠ Sprite'lar statik onbellekte tutulur. Play modu bitince Unity texture'lari yok
    /// ettigi icin onbellek <see cref="Unut"/> ile temizlenir; aksi halde ikinci Play'de
    /// yok edilmis texture referansi kullanilir.
    /// </summary>
    public static class Sekil
    {
        private static readonly Dictionary<int, Sprite> _yuvarlak = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> _golge = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> _halka = new Dictionary<int, Sprite>();
        private static Sprite _daire;
        private static Sprite _hale;

        /// <summary>Yuvarlak koseli dolu dikdortgen. 9-dilim: her boyutta kose yaricapi sabit kalir.</summary>
        public static Sprite Yuvarlak(int yaricap)
        {
            yaricap = Mathf.Clamp(yaricap, 1, 64);
            if (_yuvarlak.TryGetValue(yaricap, out var mevcut) && mevcut != null) return mevcut;

            int boy = yaricap * 2 + 4;
            var tex = YeniTexture(boy);
            var px = new Color32[boy * boy];

            for (int y = 0; y < boy; y++)
            {
                for (int x = 0; x < boy; x++)
                {
                    float a = DolguAlfa(x + 0.5f, y + 0.5f, boy, yaricap);
                    px[y * boy + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);

            var sprite = Sprite.Create(
                tex, new Rect(0f, 0f, boy, boy), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(yaricap, yaricap, yaricap, yaricap));
            sprite.name = "Yuvarlak" + yaricap;
            _yuvarlak[yaricap] = sprite;
            return sprite;
        }

        /// <summary>
        /// Yumusak golge. Panelin arkasina biraz buyuk ve asagi kaydirilmis olarak konur.
        /// Derinlik hissi, arayuzu "duz kagit" olmaktan cikaran en ucuz tek numaradir.
        /// </summary>
        public static Sprite Golge(int yaricap, int bulanik)
        {
            yaricap = Mathf.Clamp(yaricap, 0, 48);
            bulanik = Mathf.Clamp(bulanik, 2, 48);
            int anahtar = yaricap * 100 + bulanik;
            if (_golge.TryGetValue(anahtar, out var mevcut) && mevcut != null) return mevcut;

            int kenar = yaricap + bulanik;
            int boy = kenar * 2 + 4;
            var tex = YeniTexture(boy);
            var px = new Color32[boy * boy];

            for (int y = 0; y < boy; y++)
            {
                for (int x = 0; x < boy; x++)
                {
                    float d = IcSekleUzaklik(x + 0.5f, y + 0.5f, boy, bulanik, yaricap);
                    float a;
                    if (d <= 0f)
                    {
                        a = 1f;
                    }
                    else
                    {
                        float t = Mathf.Clamp01(d / bulanik);
                        // Karesel sonum: dogrusal sonum kenarda halka gibi bir cizgi birakiyor.
                        a = (1f - t) * (1f - t);
                    }
                    px[y * boy + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);

            var sprite = Sprite.Create(
                tex, new Rect(0f, 0f, boy, boy), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(kenar, kenar, kenar, kenar));
            sprite.name = "Golge" + yaricap + "_" + bulanik;
            _golge[anahtar] = sprite;
            return sprite;
        }

        /// <summary>Dolu daire. Renk secimi, rozet ve yuvarlak butonlar icin.</summary>
        public static Sprite Daire()
        {
            if (_daire != null) return _daire;

            const int boy = 128;
            var tex = YeniTexture(boy);
            var px = new Color32[boy * boy];
            const float merkez = boy / 2f;
            const float r = merkez - 1f;

            for (int y = 0; y < boy; y++)
            {
                for (int x = 0; x < boy; x++)
                {
                    float dx = x + 0.5f - merkez;
                    float dy = y + 0.5f - merkez;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    px[y * boy + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            _daire = Sprite.Create(tex, new Rect(0f, 0f, boy, boy), new Vector2(0.5f, 0.5f));
            _daire.name = "Daire";
            return _daire;
        }

        /// <summary>
        /// Merkezden kenara yumusak sonen daire — arka plan parlaklik lekesi.
        ///
        /// ⚠ <see cref="Daire"/> ile yapilamaz: dolu dairenin kenari KESKINDIR ve
        /// dusuk alfada bile koyu zeminde "dev kahverengi disk" gibi gorunur.
        /// Buradaki sonum karesel, yani merkeze yakin yerde hizli duser ve
        /// kenarda hicbir cizgi birakmaz.
        /// </summary>
        public static Sprite Hale()
        {
            if (_hale != null) return _hale;

            const int boy = 256;
            var tex = YeniTexture(boy);
            var px = new Color32[boy * boy];
            const float merkez = boy / 2f;

            for (int y = 0; y < boy; y++)
            {
                for (int x = 0; x < boy; x++)
                {
                    float dx = (x + 0.5f - merkez) / merkez;
                    float dy = (y + 0.5f - merkez) / merkez;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = d >= 1f ? 0f : (1f - d) * (1f - d);
                    px[y * boy + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            _hale = Sprite.Create(tex, new Rect(0f, 0f, boy, boy), new Vector2(0.5f, 0.5f));
            _hale.name = "Hale";
            return _hale;
        }

        /// <summary>Yuvarlak koseli cerceve (ici bos). Secili slotu isaretlemek icin.</summary>
        public static Sprite Halka(int yaricap, int kalinlik)
        {
            yaricap = Mathf.Clamp(yaricap, 2, 64);
            kalinlik = Mathf.Clamp(kalinlik, 1, 12);
            int anahtar = yaricap * 100 + kalinlik;
            if (_halka.TryGetValue(anahtar, out var mevcut) && mevcut != null) return mevcut;

            int boy = yaricap * 2 + 4;
            var tex = YeniTexture(boy);
            var px = new Color32[boy * boy];

            for (int y = 0; y < boy; y++)
            {
                for (int x = 0; x < boy; x++)
                {
                    float dis = DolguAlfa(x + 0.5f, y + 0.5f, boy, yaricap);
                    float ic = IcDolguAlfa(x + 0.5f, y + 0.5f, boy, yaricap, kalinlik);
                    float a = Mathf.Clamp01(dis - ic);
                    px[y * boy + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);

            var sprite = Sprite.Create(
                tex, new Rect(0f, 0f, boy, boy), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(yaricap, yaricap, yaricap, yaricap));
            sprite.name = "Halka" + yaricap + "_" + kalinlik;
            _halka[anahtar] = sprite;
            return sprite;
        }

        // ------------------------------------------------------------------ ikonlar
        //
        // ⚠ Bunlar YER TUTUCUDUR. Referans gorseldeki gibi elle cizilmis, dokulu
        // ikonlar kod ile uretilemez — onlar varlik isidir. Buradaki basit siluetler
        // arayuzun YAPISINI dogru kurmak icindir: nereye ikon gelecegi, hangi
        // olcude ve hangi renkte oldugu. Gercek ikonlar geldiginde yalnizca
        // sprite degisir, yerlesim aynen kalir.

        private static readonly Dictionary<string, Sprite> _ikon = new Dictionary<string, Sprite>();

        /// <summary>Bes koseli yildiz — seviye rozeti.</summary>
        public static Sprite Yildiz() => Poligon("yildiz", YildizNoktalari(5, 0.46f));

        /// <summary>Elmas — premium para.</summary>
        public static Sprite Elmas() => Poligon("elmas", new[]
        {
            new Vector2(0.5f, 0.06f), new Vector2(0.94f, 0.42f),
            new Vector2(0.5f, 0.94f), new Vector2(0.06f, 0.42f),
        });

        /// <summary>Arti — "ekle" dugmesi.</summary>
        public static Sprite Arti() => Poligon("arti", new[]
        {
            new Vector2(0.40f, 0.10f), new Vector2(0.60f, 0.10f), new Vector2(0.60f, 0.40f),
            new Vector2(0.90f, 0.40f), new Vector2(0.90f, 0.60f), new Vector2(0.60f, 0.60f),
            new Vector2(0.60f, 0.90f), new Vector2(0.40f, 0.90f), new Vector2(0.40f, 0.60f),
            new Vector2(0.10f, 0.60f), new Vector2(0.10f, 0.40f), new Vector2(0.40f, 0.40f),
        });

        /// <summary>Simsek — enerji.</summary>
        public static Sprite Simsek() => Poligon("simsek", new[]
        {
            new Vector2(0.58f, 0.04f), new Vector2(0.24f, 0.54f), new Vector2(0.46f, 0.54f),
            new Vector2(0.38f, 0.96f), new Vector2(0.78f, 0.44f), new Vector2(0.54f, 0.44f),
        });

        /// <summary>Su damlasi — sulama.</summary>
        public static Sprite Damla()
        {
            var n = new List<Vector2> { new Vector2(0.5f, 0.02f) };
            // Govde: alt yariyi cember, ust yariyi sivri uc olarak birlestir.
            for (int i = 0; i <= 24; i++)
            {
                float a = Mathf.PI * (0.5f + i / 24f * 1.5f) + Mathf.PI * 0.25f;
                n.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.36f, 0.62f + Mathf.Sin(a) * 0.36f));
            }
            return Poligon("damla", n.ToArray());
        }

        /// <summary>Kalp — partner gostergesi.</summary>
        public static Sprite Kalp()
        {
            var n = new List<Vector2>();
            for (int i = 0; i <= 48; i++)
            {
                float t = i / 48f * Mathf.PI * 2f;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t)
                          - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                n.Add(new Vector2(0.5f + x / 40f, 0.5f - y / 36f));
            }
            return Poligon("kalp", n.ToArray());
        }

        private static Vector2[] YildizNoktalari(int kol, float disYaricap)
        {
            var n = new Vector2[kol * 2];
            float ic = disYaricap * 0.44f;
            for (int i = 0; i < kol * 2; i++)
            {
                float r = (i % 2 == 0) ? disYaricap : ic;
                float a = -Mathf.PI / 2f + i * Mathf.PI / kol;
                n[i] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r);
            }
            return n;
        }

        /// <summary>
        /// Birim karedeki (0..1) poligonu sprite'a rasterize eder.
        /// Kenar yumusatma icin piksel basina 3x3 ornekleme yapilir — tek ornekte
        /// egik kenarlar merdiven gibi ciktigi icin ikonlar ucuz gorunuyordu.
        /// </summary>
        public static Sprite Poligon(string ad, Vector2[] noktalar, int boy = 64)
        {
            if (_ikon.TryGetValue(ad, out var mevcut) && mevcut != null) return mevcut;

            var tex = YeniTexture(boy);
            var px = new Color32[boy * boy];
            const int orn = 3;

            for (int y = 0; y < boy; y++)
            {
                for (int x = 0; x < boy; x++)
                {
                    int icerde = 0;
                    for (int sy = 0; sy < orn; sy++)
                    {
                        for (int sx = 0; sx < orn; sx++)
                        {
                            float px01 = (x + (sx + 0.5f) / orn) / boy;
                            float py01 = 1f - (y + (sy + 0.5f) / orn) / boy;
                            if (Icinde(noktalar, px01, py01)) icerde++;
                        }
                    }
                    float a = icerde / (float)(orn * orn);
                    px[y * boy + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, boy, boy), new Vector2(0.5f, 0.5f));
            sprite.name = "Ikon_" + ad;
            _ikon[ad] = sprite;
            return sprite;
        }

        /// <summary>Isin atma yontemi: noktadan saga cizilen isin kenarlari tek sayida keserse icerdedir.</summary>
        private static bool Icinde(Vector2[] p, float x, float y)
        {
            bool ic = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            {
                if ((p[i].y > y) != (p[j].y > y) &&
                    x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                {
                    ic = !ic;
                }
            }
            return ic;
        }

        /// <summary>Play modu bitiminde onbellegi bosaltir (yok edilmis texture kullanmamak icin).</summary>
        public static void Unut()
        {
            _yuvarlak.Clear();
            _golge.Clear();
            _halka.Clear();
            _daire = null;
            _hale = null;
            _ikon.Clear();
        }

        // ------------------------------------------------------------------ ic hesaplar

        private static Texture2D YeniTexture(int boy)
        {
            return new Texture2D(boy, boy, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        /// <summary>Yuvarlak koseli dikdortgenin (x,y) noktasindaki dolulugu. Kenarda 1 px yumusama.</summary>
        private static float DolguAlfa(float x, float y, int boy, float yaricap)
        {
            float cx = Mathf.Clamp(x, yaricap, boy - yaricap);
            float cy = Mathf.Clamp(y, yaricap, boy - yaricap);
            float dx = x - cx;
            float dy = y - cy;
            return Mathf.Clamp01(yaricap - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
        }

        /// <summary>Halkanin ic boslugu icin, <paramref name="kalinlik"/> kadar iceri cekilmis dolgu.</summary>
        private static float IcDolguAlfa(float x, float y, int boy, float yaricap, float kalinlik)
        {
            float icYaricap = Mathf.Max(0.5f, yaricap - kalinlik);
            float enAz = kalinlik;
            float enCok = boy - kalinlik;
            if (x < enAz || x > enCok || y < enAz || y > enCok) return 0f;

            float cx = Mathf.Clamp(x, enAz + icYaricap, enCok - icYaricap);
            float cy = Mathf.Clamp(y, enAz + icYaricap, enCok - icYaricap);
            float dx = x - cx;
            float dy = y - cy;
            return Mathf.Clamp01(icYaricap - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
        }

        /// <summary>Golge icin: iceri cekilmis yuvarlak dikdortgenin kenarina isaretli uzaklik.</summary>
        private static float IcSekleUzaklik(float x, float y, int boy, float kenarBosluk, float yaricap)
        {
            float enAz = kenarBosluk;
            float enCok = boy - kenarBosluk;
            float cx = Mathf.Clamp(x, enAz + yaricap, enCok - yaricap);
            float cy = Mathf.Clamp(y, enAz + yaricap, enCok - yaricap);
            float dx = x - cx;
            float dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - yaricap;
        }
    }
}
