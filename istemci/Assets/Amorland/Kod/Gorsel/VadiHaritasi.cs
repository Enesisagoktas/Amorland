using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Amorland vadisinin buyuk karo haritasi — 180x130 karo (23.400 hucre).
    ///
    /// Uc Tilemap katmani:
    ///   Zemin -> cim / su / yol. Bosluk BIRAKMAZ, her hucre dolu.
    ///   Dekor -> agac, cali, cicek, ekin, cit. Zeminin ustune biner.
    ///   Nesne -> ahir, ev, maden agzi, tezgah, karakter. En ustte.
    ///
    /// ⚠ Neden Tilemap ve neden Chunk modu: 23.400 hucre x 3 katman ayri
    /// SpriteRenderer olsaydi 70 binden fazla GameObject ederdi — mobilde acilmaz.
    /// Tilemap hucreleri parca parca tek mesh'e batchler ve ekran disini eler.
    /// Spec B.2 performans hedefi bu sayede tutuyor.
    ///
    /// ⚠ Harita DETERMINISTIK: <see cref="Tohum"/> sabit oldugu surece ayni vadi
    /// cikar. Aksi halde her acilista baska bir dunya olurdu ve "evim neredeydi"
    /// sorusunun cevabi kalmazdi.
    ///
    /// Spec B.4: 7 bolge — Ev, Tarla (Sv1), Depo (Sv2), Tren gari (Sv5), Market (Sv6),
    /// Orman (Sv7), Maden ocagi (Sv9).
    /// </summary>
    public class VadiHaritasi : MonoBehaviour
    {
        public const int En = 180;
        public const int Boy = 130;
        /// <summary>Sabit tohum: vadi her calistirmada AYNI cikar.</summary>
        public const int Tohum = 20260821;

        /// <summary>Haritadaki bir bolge. Spec B.4: dokununca o sahneye girilir.</summary>
        public struct Bolge
        {
            public string Ad;
            public string Baslik;
            public int Seviye;
            public RectInt Alan;
            public Vector2 Merkez => new Vector2(Alan.x + Alan.width / 2f, Alan.y + Alan.height / 2f);
        }

        public readonly List<Bolge> Bolgeler = new List<Bolge>();

        private Tilemap _zemin, _dekor, _nesne;
        private System.Random _r;

        private bool[,] _su;      // nehir
        private bool[,] _yol;     // toprak yol
        private bool[,] _kopru;   // yolun suyu gectigi yer
        private bool[,] _meydan;  // bolge avlusu — yol gibi doseNir ama uzerine esya konur
        private bool[,] _dolu;    // dekor/nesne kaplamis — ustune bir sey konmaz

        private static readonly Dictionary<int, Tile> _onbellek = new Dictionary<int, Tile>();

        // ------------------------------------------------------------------ kurulum

        public void Kur()
        {
            _r = new System.Random(Tohum);
            _su = new bool[En, Boy];
            _yol = new bool[En, Boy];
            _kopru = new bool[En, Boy];
            _meydan = new bool[En, Boy];
            _dolu = new bool[En, Boy];

            KatmanlariKur();
            BolgeleriTanimla();

            NehirCiz();
            YolAgiCiz();
            MeydanlariCiz();

            ZeminDose();

            Orman();
            MadenOcagi();
            EvVeTarla();
            Depo();
            TrenGari();
            Market();

            Serpistir();

            _zemin.RefreshAllTiles();
            _dekor.RefreshAllTiles();
            _nesne.RefreshAllTiles();
        }

        private void KatmanlariKur()
        {
            var grid = gameObject.GetComponent<Grid>();
            if (grid == null) grid = gameObject.AddComponent<Grid>();
            // Bir karo = 1 dunya birimi. KaroSeti.PikselBirim ile tutarli olmali.
            grid.cellSize = new Vector3(1f, 1f, 0f);

            _zemin = Katman("Zemin", 0);
            _dekor = Katman("Dekor", 1);
            _nesne = Katman("Nesne", 2);
        }

        private Tilemap Katman(string ad, int sira)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(transform, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sira;
            // ⚠ Chunk: hucreleri tek mesh'e batchler. Individual modu her karoyu ayri
            // cizer ve 23 bin hucrede kare hizini yere serer.
            tr.mode = TilemapRenderer.Mode.Chunk;
            // Ekranda asagida kalan karo ONDE cizilsin — tepeden bakista dogru ortusme.
            tr.sortOrder = TilemapRenderer.SortOrder.TopLeft;
            return tm;
        }

        private void BolgeleriTanimla()
        {
            Bolgeler.Clear();
            Bolgeler.Add(new Bolge { Ad = "orman",  Baslik = "Orman",       Seviye = 7, Alan = new RectInt(6, 76, 52, 48) });
            Bolgeler.Add(new Bolge { Ad = "tren",   Baslik = "Tren Gari",   Seviye = 5, Alan = new RectInt(118, 92, 54, 34) });
            Bolgeler.Add(new Bolge { Ad = "ev",     Baslik = "Ev",          Seviye = 1, Alan = new RectInt(60, 58, 24, 22) });
            Bolgeler.Add(new Bolge { Ad = "tarla",  Baslik = "Tarla",       Seviye = 1, Alan = new RectInt(60, 30, 52, 24) });
            Bolgeler.Add(new Bolge { Ad = "depo",   Baslik = "Depo",        Seviye = 2, Alan = new RectInt(124, 56, 40, 24) });
            Bolgeler.Add(new Bolge { Ad = "maden",  Baslik = "Maden Ocagi", Seviye = 9, Alan = new RectInt(8, 6, 44, 32) });
            Bolgeler.Add(new Bolge { Ad = "market", Baslik = "Market",      Seviye = 6, Alan = new RectInt(112, 6, 56, 34) });
        }

        // ------------------------------------------------------------------ arazi

        /// <summary>
        /// Nehri kuzeybatidan guneydoguya akitir.
        ///
        /// ⚠ Genislik 3 karonun ALTINA dusmemeli: 3x3 autotile setinde "hem batisi hem
        /// dogusu kara" karosu yok. Ince nehir bozuk kiyi karolari uretir.
        /// </summary>
        private void NehirCiz()
        {
            Vector2[] yatak =
            {
                new Vector2(-4f, 118f), new Vector2(30f, 108f), new Vector2(62f, 100f),
                new Vector2(94f, 90f),  new Vector2(116f, 76f), new Vector2(140f, 50f),
                new Vector2(162f, 30f), new Vector2(184f, 18f),
            };

            for (int i = 0; i < yatak.Length - 1; i++)
            {
                Vector2 a = yatak[i], b = yatak[i + 1];
                int adim = Mathf.CeilToInt(Vector2.Distance(a, b) * 3f);
                for (int s = 0; s <= adim; s++)
                {
                    float t = s / (float)adim;
                    Vector2 p = Vector2.Lerp(a, b, t);
                    // Genislik dalgalansin: duz kanal degil, dere gorunsun.
                    float yaricap = 3.2f + 0.9f * Mathf.Sin((i + t) * 2.3f);
                    Disk(_su, p, yaricap);
                }
            }
        }

        /// <summary>
        /// Yol agi: ev meydani merkez, her bolgeye bir kol.
        ///
        /// Yol 3 karo genisliginde — autotile setinin alt siniri (bkz. NehirCiz).
        /// </summary>
        private void YolAgiCiz()
        {
            Vector2 meydan = new Vector2(74f, 56f);

            foreach (var b in Bolgeler)
            {
                if (b.Ad == "ev") continue;
                Kol(meydan, b.Merkez);
            }

            // Vadinin bir ucundan otekine gecen ana yol — haritayi baglar.
            Kol(new Vector2(2f, 62f), meydan);
            Kol(meydan, new Vector2(176f, 70f));

            // Yolun suya girdigi her yer koprudur.
            for (int x = 0; x < En; x++)
                for (int y = 0; y < Boy; y++)
                    if (_yol[x, y] && _su[x, y]) _kopru[x, y] = true;
        }

        /// <summary>
        /// Bolge avlularini (market meydani, depo avlusu, gar onu) toprak olarak
        /// isaretler.
        ///
        /// ⚠ Neden yol maskesiyle AYNI malzeme ama AYRI dizi: avlular yol autotile'iyla
        /// dosenmeli — o zaman cime yuvarlak kenarla baglanirlar. Ilk denemede avluyu
        /// duz tas karoyla doseyince koca bir gri beton levha cikti, kenari da bicakla
        /// kesilmis gibiydi. Ama yol maskesine yazsaydim uzerine tezgah konamazdi:
        /// <see cref="Musait"/> yolu kapali sayar. Ayri dizi ikisini de cozuyor.
        /// </summary>
        private void MeydanlariCiz()
        {
            Avlu(Alan("market"), 6, 4, 6, 12);
            Avlu(Alan("depo"), 3, 2, 3, 9);
            Avlu(Alan("ev"), 2, 2, 2, 10);

            // ⚠ Gar avlusu DAR: ilk denemede bolgenin tamamiydi ve ekranin tamami
            // ciplak toprak cikti. Peronun ve ray yatağinin cevresiyle sinirli.
            RectInt g = Alan("tren");
            Avlu(new RectInt(g.xMin + 4, PeronY(g) - 3, g.width - 8, 14), 0, 0, 0, 0);

            // Maden avlusu: ocak agzinin onundeki calisma alani.
            Avlu(Alan("maden"), 6, 4, 6, 8);
        }

        /// <summary>
        /// Peronun y konumu. Hem avlu cizimi hem gar kurulumu buna bakar; iki yerde
        /// ayri sayi tutulursa avlu peronu kacirir.
        /// </summary>
        private static int PeronY(RectInt trenAlani) => trenAlani.yMin + 10;

        /// <summary>Bolgenin ic dikdortgenini avlu yapar; kenari gurultuyle kirilir.</summary>
        private void Avlu(RectInt a, int solPay, int altPay, int sagPay, int ustPay)
        {
            int x0 = a.xMin + solPay, x1 = a.xMax - sagPay;
            int y0 = a.yMin + altPay, y1 = a.yMax - ustPay;
            for (int x = x0; x < x1; x++)
            {
                for (int y = y0; y < y1; y++)
                {
                    if (x < 0 || y < 0 || x >= En || y >= Boy || _su[x, y]) continue;
                    // Kenardan 2 karo ice kadar gurultuyle kemir: dikdortgen okunmasin.
                    float kenar = Mathf.Min(Mathf.Min(x - x0, x1 - 1 - x),
                                            Mathf.Min(y - y0, y1 - 1 - y));
                    if (kenar < 2f && Gurultu(x, y, 5, 9) < 0.42f) continue;
                    _meydan[x, y] = true;
                }
            }
        }

        /// <summary>Iki nokta arasina L kirikli yol: once yatay, sonra dikey.</summary>
        private void Kol(Vector2 a, Vector2 b)
        {
            Vector2 kose = new Vector2(b.x, a.y);
            Serit(a, kose);
            Serit(kose, b);
        }

        private void Serit(Vector2 a, Vector2 b)
        {
            int adim = Mathf.CeilToInt(Vector2.Distance(a, b) * 3f);
            if (adim == 0) return;
            for (int s = 0; s <= adim; s++)
            {
                Vector2 p = Vector2.Lerp(a, b, s / (float)adim);
                Disk(_yol, p, 1.7f);
            }
        }

        private static void Disk(bool[,] maske, Vector2 merkez, float yaricap)
        {
            int r = Mathf.CeilToInt(yaricap);
            int cx = Mathf.RoundToInt(merkez.x), cy = Mathf.RoundToInt(merkez.y);
            for (int x = cx - r; x <= cx + r; x++)
            {
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (x < 0 || y < 0 || x >= En || y >= Boy) continue;
                    float dx = x - merkez.x, dy = y - merkez.y;
                    if (dx * dx + dy * dy <= yaricap * yaricap) maske[x, y] = true;
                }
            }
        }

        /// <summary>
        /// Zemin katmanini doser: once kopru, sonra su, sonra yol, kalani cim.
        ///
        /// ⚠ Cim cesitlemesi AGIRLIKLI: duz cim %78. Cicekli karo esit dagitilirsa
        /// vadi cayirdan cok puantiyeli kumasa benziyor — seyrek serpilmeli.
        /// </summary>
        private void ZeminDose()
        {
            for (int x = 0; x < En; x++)
            {
                for (int y = 0; y < Boy; y++)
                {
                    if (_kopru[x, y])
                    {
                        // Kopru govdesi: dolu toprak. Korkuluklari Serpistir koyar.
                        Koy(_zemin, x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sec(KaroSeti.ToprakDolu, _r)));
                        continue;
                    }

                    if (_su[x, y])
                    {
                        int i = KaroSeti.Dokuz(KaroSeti.SuBlok,
                                               Ayni(_su, x, y + 1), Ayni(_su, x, y - 1),
                                               Ayni(_su, x - 1, y), Ayni(_su, x + 1, y));
                        // Acik suda kopuk/nilufer cesitlemesi.
                        if (i == KaroSeti.SuBlok[1, 1] && _r.Next(100) < 12)
                            i = KaroSeti.Sec(KaroSeti.SuAcik, _r);
                        Koy(_zemin, x, y, new KaroKimlik(Sayfa.Su, i));
                        continue;
                    }

                    if (_yol[x, y] || _meydan[x, y])
                    {
                        int i = KaroSeti.Dokuz(KaroSeti.YolBlok,
                                               YolGibi(x, y + 1), YolGibi(x, y - 1),
                                               YolGibi(x - 1, y), YolGibi(x + 1, y));
                        if (i == KaroSeti.YolBlok[1, 1])
                            i = KaroSeti.Sec(KaroSeti.ToprakDolu, _r);
                        Koy(_zemin, x, y, new KaroKimlik(Sayfa.Kasaba, i));
                        continue;
                    }

                    // ⚠ Cim cesitlemesi GURULTUYE bagli, tek tek zar atmaya degil. Her
                    // karoya bagimsiz zar atinca cicekler haritanin her yerine esit
                    // dagilmis konfeti gibi cikiyordu; boyle cicek TARHLARI olusuyor.
                    float cayir = Gurultu(x, y, 11, 1);
                    int z = _r.Next(100);
                    KaroKimlik cim;
                    if (cayir > 0.66f) cim = z < 40 ? KaroSeti.CimCicek : (z < 70 ? KaroSeti.CimOt : KaroSeti.CimDuz);
                    else if (cayir > 0.44f) cim = z < 24 ? KaroSeti.CimOt : KaroSeti.CimDuz;
                    else cim = z < 5 ? KaroSeti.CimOt : KaroSeti.CimDuz;
                    Koy(_zemin, x, y, cim);
                }
            }
        }

        /// <summary>Harita disi AYNI sayilir — kenarda yapay kiyi cizgisi olusmasin.</summary>
        private static bool Ayni(bool[,] maske, int x, int y)
        {
            if (x < 0 || y < 0 || x >= En || y >= Boy) return true;
            return maske[x, y];
        }

        /// <summary>
        /// Toprak sayilan her sey: yol, kopru ve avlu. Autotile kenar secimi buna bakar;
        /// yoksa avlunun yola degdigi yerde ve koprunun iki ucunda yapay cim seridi cikar.
        /// </summary>
        private bool YolGibi(int x, int y)
        {
            if (x < 0 || y < 0 || x >= En || y >= Boy) return true;
            return _yol[x, y] || _kopru[x, y] || _meydan[x, y];
        }

        // ------------------------------------------------------------------ bolgeler

        private void Orman()
        {
            RectInt a = Alan("orman");
            for (int x = a.xMin; x < a.xMax; x++)
            {
                for (int y = a.yMin; y < a.yMax; y++)
                {
                    if (!Musait(x, y)) continue;

                    // Kenara dogru seyrelt: orman bicakla kesilmis gibi bitmesin.
                    float kenar = Mathf.Min(Mathf.Min(x - a.xMin, a.xMax - 1 - x),
                                            Mathf.Min(y - a.yMin, a.yMax - 1 - y));
                    float kenarPay = Mathf.Clamp01(kenar / 8f);

                    // ⚠ Ic taraf da GURULTUYLE delinir: tek tip yogunluktaki orman
                    // dumduz yesil bir leke gibi duruyordu. Boyle icinde acikliklar,
                    // patikalar ve sik obekler olusuyor.
                    float sikilik = Gurultu(x, y, 13, 5);
                    float sans = Mathf.Lerp(8f, 78f, kenarPay) * Mathf.Clamp01(sikilik * 1.5f);
                    if (_r.Next(100) >= sans) continue;

                    int t = _r.Next(100);
                    if (t < 58) Nesne(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sec(KaroSeti.Camlar, _r)));
                    else if (t < 82) Nesne(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sec(KaroSeti.AgacObek, _r)));
                    else if (t < 91) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Cali));
                    else if (t < 97) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Mantar));
                    else Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Kutuk));
                }
            }
            Tabela(a.xMin + a.width / 2, a.yMin - 2);
        }

        private void MadenOcagi()
        {
            RectInt a = Alan("maden");

            int mx = a.xMin + a.width / 2 - 1, my = a.yMax - 11;

            // Ocak agzinin arkasindaki KAYA YUZU. ⚠ Kucuk tutuluyor: saf tas karosunun
            // (109) cime baglayan gecis karosu YOK, kenari merdiven gibi cikiyor. Genis
            // alana doseyince gri kagit kesigi gibi duruyordu; ocak agzinin cevresiyle
            // sinirli kalinca kaya yuzu olarak okunuyor. Zemin toprak avludur.
            for (int x = mx - 4; x <= mx + 6; x++)
                for (int y = my - 1; y <= my + 4; y++)
                    if (Musait(x, y)) Koy(_zemin, x, y, KaroSeti.TasZemin);
            Blok(_nesne, KaroSeti.MadenAgzi, Sayfa.Kasaba, mx, my);

            // Ocak agzinin onunde calisma alani: kazma, varil, kasa, isci.
            Nesne(mx - 4, my, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Kazma));
            Nesne(mx - 3, my - 2, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Varil));
            Nesne(mx + 4, my, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Kasa, _r)));
            Nesne(mx + 5, my - 2, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sepet));
            Nesne(mx + 1, my - 4, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Koylu));

            // Kaya yuzunun kenarina tas yigini: sert gecisi kirar.
            for (int x = mx - 5; x <= mx + 7; x++)
            {
                if (Musait(x, my - 2) && _r.Next(100) < 62)
                    Dekor(x, my - 2, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Kaya, _r)));
                if (Musait(x, my + 5) && _r.Next(100) < 48)
                    Dekor(x, my + 5, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Kaya, _r)));
            }

            // Kayalar gurultuyle kumelenir; arasi acik kalir.
            for (int x = a.xMin; x < a.xMax; x++)
            {
                for (int y = a.yMin; y < a.yMax; y++)
                {
                    if (!Musait(x, y)) continue;
                    float kayalik = Gurultu(x, y, 6, 4);
                    int t = _r.Next(100);
                    if (kayalik > 0.58f)
                    {
                        if (t < 34) Dekor(x, y, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Kaya, _r)));
                        else if (t < 42) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Kutuk));
                    }
                    else if (t < 7) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.OtTutam));
                }
            }
            Tabela(a.xMin + a.width / 2, a.yMax + 2);
        }

        private void EvVeTarla()
        {
            RectInt e = Alan("ev");


            // Ahir: 3 sutun x 4 satir, avlunun batisinda.
            Blok(_nesne, KaroSeti.Ahir, Sayfa.Ciftlik, e.xMin + 3, e.yMin + 8);
            // Ciftin evi: ahirin dogusunda.
            Blok(_nesne, KaroSeti.EvMavi, Sayfa.Kasaba, e.xMin + 12, e.yMin + 10);

            Nesne(e.xMin + 2, e.yMin + 6, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.SamanBalya));
            Nesne(e.xMin + 8, e.yMin + 7, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.KovaDolu));
            Nesne(e.xMin + 17, e.yMin + 8, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Varil));
            Nesne(e.xMin + 10, e.yMin + 12, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Kovan));

            // Cift: evin onunde yan yana.
            Nesne(e.xMin + 13, e.yMin + 7, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Ciftci));
            Nesne(e.xMin + 15, e.yMin + 7, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.CiftciKadin));

            // Hayvanlar avlunun guneyinde otluyor.
            for (int i = 0; i < 5; i++)
                Nesne(e.xMin + 4 + i * 3, e.yMin + 4, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Hayvan, _r)));

            RectInt t = Alan("tarla");
            CitCevir(t);

            // Ekin parselleri: 4 karo genis, 3 karo yuksek bloklar, aralarinda yurume payi.
            for (int px = t.xMin + 2; px + 4 <= t.xMax - 2; px += 6)
            {
                for (int py = t.yMin + 2; py + 3 <= t.yMax - 2; py += 5)
                {
                    int urun = _r.Next(KaroSeti.UrunOlgun.Length);
                    bool olgun = _r.Next(100) < 55;
                    for (int x = px; x < px + 4; x++)
                    {
                        for (int y = py; y < py + 3; y++)
                        {
                            if (!Musait(x, y)) continue;
                            Koy(_zemin, x, y, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Parsel, _r)));
                            Dekor(x, y, new KaroKimlik(Sayfa.Ciftlik,
                                  olgun ? KaroSeti.UrunOlgun[urun] : KaroSeti.UrunGenc[urun]));
                        }
                    }
                }
            }
            Tabela(t.xMin + t.width / 2, t.yMax + 2);
        }

        private void Depo()
        {
            RectInt a = Alan("depo");


            Blok(_nesne, KaroSeti.EvAhsap, Sayfa.Kasaba, a.xMin + 5, a.yMax - 7);
            Blok(_nesne, KaroSeti.EvTas, Sayfa.Kasaba, a.xMin + 24, a.yMax - 7);

            // ⚠ Cuval ve kasalar DUZENLI siralar halinde: depo istiflenmis mal
            // demektir. Rastgele serpme ilk denemede cop alani gibi cikti.
            for (int sira = 0; sira < 3; sira++)
            {
                for (int i = 0; i < 10; i++)
                {
                    int x = a.xMin + 5 + i * 3, y = a.yMin + 3 + sira * 3;
                    if (!Musait(x, y)) continue;
                    Nesne(x, y, new KaroKimlik(Sayfa.Ciftlik,
                          (sira + i) % 2 == 0 ? KaroSeti.Sec(KaroSeti.Cuval, _r)
                                              : KaroSeti.Sec(KaroSeti.Kasa, _r)));
                }
            }
            Nesne(a.xMin + 3, a.yMin + 10, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sepet));
            Nesne(a.xMin + 5, a.yMin + 12, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Varil));
            Nesne(a.xMin + 20, a.yMin + 11, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Ciftci));
            Tabela(a.xMin + a.width / 2, a.yMin - 2);
        }

        private void TrenGari()
        {
            RectInt a = Alan("tren");
            int py = PeronY(a);

            // Peron: saf tas. Duz kenari burada DOGRU — peron insa edilmis bir yapidir,
            // arazi degil. Zemininin geri kalani MeydanlariCiz tarafindan toprak dosendi.
            TasDose(new RectInt(a.xMin + 6, py, a.width - 12, 3));

            // Ray: traversler toprak yatagin uzerinde. Yatak avlunun parcasi.
            for (int x = a.xMin + 5; x < a.xMax - 5; x += 2)
                Dekor(x, py + 5, new KaroKimlik(Sayfa.Kasaba, KaroSeti.CitYatay));

            // Gar binasi: tas govde + tas cepheli ek bina.
            Blok(_nesne, KaroSeti.TasDuvar, Sayfa.Kasaba, a.xMin + 6, py + 8);
            Blok(_nesne, KaroSeti.EvTas, Sayfa.Kasaba, a.xMin + 16, py + 8);
            Nesne(a.xMin + 5, py + 8, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Bayrak));

            // Peronda bekleyen yuk ve yolcu.
            for (int i = 0; i < 9; i++)
                Nesne(a.xMin + 24 + i * 3, py + 1, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Kasa, _r)));
            Nesne(a.xMin + 22, py + 2, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Koylu));
            Nesne(a.xMin + 30, py + 3, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.CiftciKadin));
            Tabela(a.xMin + a.width / 2, a.yMin - 2);
        }

        private void Market()
        {
            RectInt a = Alan("market");

            // Meydan zemini MeydanlariCiz tarafindan toprak olarak dosendi.
            var meydan = new RectInt(a.xMin + 7, a.yMin + 5, a.width - 14, a.height - 15);

            // Uc dukkan meydanin kuzey kenarinda sirali.
            Blok(_nesne, KaroSeti.EvKirmizi, Sayfa.Kasaba, a.xMin + 7, a.yMax - 7);
            Blok(_nesne, KaroSeti.EvTas, Sayfa.Kasaba, a.xMin + 23, a.yMax - 7);
            Blok(_nesne, KaroSeti.EvAhsap, Sayfa.Kasaba, a.xMin + 39, a.yMax - 7);

            // Tezgah SIRALARI: pazar dagilmis esya degil, yan yana dizilmis tezgahtir.
            // Ilk denemede 9 karo arayla serpildiler ve meydan bos otopark gibi cikti.
            for (int sira = 0; sira < 3; sira++)
            {
                int y = meydan.yMin + 2 + sira * 5;
                for (int i = 0; i < 9; i++)
                {
                    int x = meydan.xMin + 2 + i * 4;
                    if (x + 2 >= meydan.xMax) break;
                    Nesne(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sepet));
                    Nesne(x + 1, y, new KaroKimlik(Sayfa.Kasaba,
                          (sira + i) % 3 == 0 ? KaroSeti.Ekmek : KaroSeti.Kovan));
                    Nesne(x + 2, y, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Cuval, _r)));
                    if (i % 2 == 0)
                        Nesne(x, y + 1, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Kasa, _r)));
                }
            }

            // Meydanda insan olsun: bos meydan pazar degil, otopark gibi duruyor.
            Nesne(meydan.xMin + 6, meydan.yMin + 4, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Koylu));
            Nesne(meydan.xMax - 7, meydan.yMin + 5, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.CiftciKadin));
            Nesne(meydan.xMin + 15, meydan.yMax - 2, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Ciftci));
            Nesne(meydan.xMin + 20, meydan.yMax - 3, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Bayrak));
            Tabela(a.xMin + a.width / 2, a.yMin - 2);
        }

        /// <summary>Dikdortgeni saf tasla doser (meydan, peron).</summary>
        private void TasDose(RectInt r)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    if (Musait(x, y)) Koy(_zemin, x, y, KaroSeti.TasZemin);
        }

        // ------------------------------------------------------------------ serpme

        /// <summary>
        /// Bolgelerin disinda kalan cayiri KUMELER halinde doldurur.
        ///
        /// ⚠ Ilk surumde her karoya bagimsiz zar atiliyordu; sonuc 23 bin karo boyunca
        /// ayni yogunlukta serpilmis tek tuk agacti ve vadi bastan basa ayni goruntuye
        /// dusuyordu. Gurultuyle koruluk / cayir / acikli alan ayrimi cikiyor, gezerken
        /// manzara degisiyor.
        /// </summary>
        private void Serpistir()
        {
            for (int x = 0; x < En; x++)
            {
                for (int y = 0; y < Boy; y++)
                {
                    if (_kopru[x, y])
                    {
                        // Korkuluk: koprunun kuzey/guney kenari suyun uzerindeyse cit koy.
                        bool ucKuzey = y + 1 < Boy && _su[x, y + 1] && !_kopru[x, y + 1];
                        bool ucGuney = y - 1 >= 0 && _su[x, y - 1] && !_kopru[x, y - 1];
                        if (ucKuzey || ucGuney) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.CitYatay));
                        continue;
                    }
                    if (!Musait(x, y) || BolgedeMi(x, y)) continue;

                    float bitki = Gurultu(x, y, 15, 2);
                    int t = _r.Next(1000);

                    if (bitki > 0.68f)
                    {
                        // Koruluk: agac obekleri, altinda cali ve mantar.
                        if (t < 250) Nesne(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sec(KaroSeti.Camlar, _r)));
                        else if (t < 350) Nesne(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sec(KaroSeti.AgacObek, _r)));
                        else if (t < 420) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Cali));
                        else if (t < 440) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Mantar));
                    }
                    else if (bitki > 0.50f)
                    {
                        // Cayir: ot, bugday, tek tuk agac.
                        if (t < 95) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.OtTutam));
                        else if (t < 140) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sec(KaroSeti.Bugday, _r)));
                        else if (t < 158) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Cali));
                        else if (t < 166) Nesne(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Sec(KaroSeti.Camlar, _r)));
                    }
                    else
                    {
                        // Acik arazi: neredeyse bos, arada tasli yama.
                        if (t < 20) Dekor(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.OtTutam));
                        else if (t < 30) Koy(_zemin, x, y, KaroSeti.CakilYama);
                        else if (t < 34) Dekor(x, y, new KaroKimlik(Sayfa.Ciftlik, KaroSeti.Sec(KaroSeti.Kaya, _r)));
                    }
                }
            }
        }

        // ------------------------------------------------------------------ gurultu

        /// <summary>
        /// Deterministik deger gurultusu — bitkiyi ve dokuyu KUMELEYEN sey.
        ///
        /// Kaba bir izgaranin (olcek karo) dugumlerine sabit rastgele deger konur,
        /// aradaki degerler yumusatilmis interpolasyonla bulunur. Dizi tutmaz, her
        /// cagrida ayni sonucu verir — harita determinizmi bozulmaz.
        /// </summary>
        private static float Gurultu(int x, int y, int olcek, int tuz)
        {
            int gx = Mathf.FloorToInt(x / (float)olcek);
            int gy = Mathf.FloorToInt(y / (float)olcek);
            float fx = x / (float)olcek - gx;
            float fy = y / (float)olcek - gy;
            // ⚠ Yumusatma egrisi sart: duz dogrusal interpolasyon kaba izgarayi
            // gorunur birakir, haritada kare kare gecisler belli olur.
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Dugum(gx, gy, tuz), b = Dugum(gx + 1, gy, tuz);
            float c = Dugum(gx, gy + 1, tuz), d = Dugum(gx + 1, gy + 1, tuz);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Izgara dugumunun sabit rastgele degeri (hash).</summary>
        private static float Dugum(int x, int y, int tuz)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + tuz * 1274126177 + Tohum);
                h ^= h >> 13;
                h *= 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>Cevresine cit ceker; guney kenarinin ortasina kapi birakir.</summary>
        private void CitCevir(RectInt a)
        {
            int kapi = a.xMin + a.width / 2;
            for (int x = a.xMin; x < a.xMax; x++)
            {
                if (Musait(x, a.yMin))
                    Dekor(x, a.yMin, new KaroKimlik(Sayfa.Kasaba,
                          Mathf.Abs(x - kapi) > 1 ? KaroSeti.CitYatay : KaroSeti.CitKapi));
                if (Musait(x, a.yMax - 1))
                    Dekor(x, a.yMax - 1, new KaroKimlik(Sayfa.Kasaba, KaroSeti.CitYatay));
            }
            for (int y = a.yMin; y < a.yMax; y++)
            {
                if (Musait(a.xMin, y)) Dekor(a.xMin, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.CitDikey));
                if (Musait(a.xMax - 1, y)) Dekor(a.xMax - 1, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.CitDikey));
            }
        }

        private void Tabela(int x, int y)
        {
            if (x < 0 || y < 0 || x >= En || y >= Boy) return;
            Nesne(x, y, new KaroKimlik(Sayfa.Kasaba, KaroSeti.Tabela));
        }

        // ------------------------------------------------------------------ yerlestirme

        public RectInt Alan(string ad)
        {
            foreach (var b in Bolgeler) if (b.Ad == ad) return b.Alan;
            return new RectInt(0, 0, 0, 0);
        }

        private bool BolgedeMi(int x, int y)
        {
            var p = new Vector2Int(x, y);
            foreach (var b in Bolgeler) if (b.Alan.Contains(p)) return true;
            return false;
        }

        /// <summary>
        /// Bu hucreye bir sey konabilir mi: su/yol/dolu degilse evet.
        /// ⚠ Avlu (<c>_meydan</c>) engel SAYILMAZ — tezgahlar ve kasalar oraya konur.
        /// </summary>
        private bool Musait(int x, int y)
        {
            if (x < 0 || y < 0 || x >= En || y >= Boy) return false;
            return !_su[x, y] && !_yol[x, y] && !_dolu[x, y];
        }

        private void Dekor(int x, int y, KaroKimlik k)
        {
            if (x < 0 || y < 0 || x >= En || y >= Boy) return;
            Koy(_dekor, x, y, k);
            _dolu[x, y] = true;
        }

        private void Nesne(int x, int y, KaroKimlik k)
        {
            if (x < 0 || y < 0 || x >= En || y >= Boy) return;
            Koy(_nesne, x, y, k);
            _dolu[x, y] = true;
        }

        /// <summary>
        /// Cok karolu bir yapiyi yerlestirir. Tablo [satir, sutun] ve satir 0 sayfada
        /// USTTEKI sira; dunyada ust = buyuk y, o yuzden satir ters cevrilir.
        /// </summary>
        private void Blok(Tilemap tm, int[,] tablo, Sayfa sayfa, int x0, int y0)
        {
            int sat = tablo.GetLength(0), sut = tablo.GetLength(1);
            for (int s = 0; s < sat; s++)
            {
                for (int u = 0; u < sut; u++)
                {
                    int x = x0 + u, y = y0 + (sat - 1 - s);
                    if (x < 0 || y < 0 || x >= En || y >= Boy) continue;
                    Koy(tm, x, y, new KaroKimlik(sayfa, tablo[s, u]));
                    _dolu[x, y] = true;
                }
            }
        }

        private static void Koy(Tilemap tm, int x, int y, KaroKimlik karo)
        {
            Tile t = Onbellek(karo);
            if (t != null) tm.SetTile(new Vector3Int(x, y, 0), t);
        }

        /// <summary>
        /// Tile nesnesi onbellegi.
        ///
        /// ⚠ Her hucre icin ayri Tile uretmek 23 bin ScriptableObject demektir. Ayni
        /// karo tekrar tekrar kullanildigi icin sayfa+indeks anahtariyla paylasilir;
        /// tum harita birkac yuz Tile ile doner.
        /// </summary>
        private static Tile Onbellek(KaroKimlik karo)
        {
            int anahtar = (int)karo.Sayfa * 10000 + karo.Indeks;
            if (_onbellek.TryGetValue(anahtar, out Tile t) && t != null) return t;

            Sprite s = KaroSeti.Al(karo);
            if (s == null) return null;

            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.colliderType = Tile.ColliderType.None;
            t.name = s.name;
            _onbellek[anahtar] = t;
            return t;
        }

        /// <summary>Onbellegi bosaltir — editor araclari her uretimde temiz baslar.</summary>
        public static void OnbellegiTemizle() => _onbellek.Clear();
    }
}
