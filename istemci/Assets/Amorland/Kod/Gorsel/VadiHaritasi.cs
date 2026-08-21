using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Amorland vadisinin buyuk karo haritasi — 512x320 karo (163.840 hucre).
    ///
    /// Karo seti 64 px Kenney RPG Base (+ ayni paletten cizilmis ek sayfa).
    /// Onceki 16 px tiny-* seti kullanicinin gozune fazla basit geldi; rpg-base
    /// ayni mimariye oturan en zengin tepeden bakisli set (212 paketin taramasi
    /// araclar/karo-indeks/ altinda).
    ///
    /// Dort Tilemap katmani:
    ///   Zemin    -> cim / su / toprak / kaldirim / ray / kalas. Bosluk birakmaz.
    ///   Dekor    -> cit, cali, ekin, saban izi ustu bitki. Zemine biner.
    ///   Nesne    -> agac, duvar, cati, kasa, hayvan, koylu.
    ///   NesneUst -> kapi ve pencereler. ⚠ Ayri katman SART: kapi saydam kenarli bir
    ///               kaplamadir ve duvar karosunun USTUNE binmek zorundadir; ayni
    ///               katmanda ikisinden biri kaybolur.
    ///
    /// ⚠ Harita DETERMINISTIK: <see cref="Tohum"/> sabit oldugu surece ayni vadi.
    ///
    /// ⚠ Su kenarlari CIM varsayar: SuBlok karolarinin kiyisinda cim gomulu.
    /// Toprak tipi zemin suya degdiginde sahte cim seridi cikar — o yuzden toprak
    /// maskeleri suyun 1 karo yakinina yazilmaz; su gecisleri KALAS koprudur.
    ///
    /// Spec B.4: 7 bolge — Ev, Tarla (Sv1), Depo (Sv2), Tren gari (Sv5), Market (Sv6),
    /// Orman (Sv7), Maden ocagi (Sv9).
    /// </summary>
    public class VadiHaritasi : MonoBehaviour
    {
        public const int En = 512;
        public const int Boy = 320;
        public const int Tohum = 20260821;

        public struct Bolge
        {
            public string Ad;
            public string Baslik;
            public int Seviye;
            public RectInt Alan;
            public Vector2 Merkez => new Vector2(Alan.x + Alan.width / 2f, Alan.y + Alan.height / 2f);
        }

        public readonly List<Bolge> Bolgeler = new List<Bolge>();

        private Tilemap _zemin, _dekor, _nesne, _nesneUst;
        private System.Random _r;

        private bool[,] _su;        // gol + nehirler
        private bool[,] _suYakin;   // suyun 1 karo genislemis hali
        private bool[,] _toprak;    // yol + avlu + tarla tabani (toprak blok autotile)
        private bool[,] _kaldirim;  // arnavut kaldirimi (meydan, peron)
        private bool[,] _surulu;    // saban izli tarla parseli
        private bool[,] _ray;       // tren rayi
        private bool[,] _kalas;     // kopru / iskele dosemesi
        private bool[,] _dolu;      // dekor/nesne kaplamis

        private static readonly Dictionary<long, Tile> _onbellek = new Dictionary<long, Tile>();

        // ------------------------------------------------------------------ kurulum

        public void Kur()
        {
            _r = new System.Random(Tohum);
            _su = new bool[En, Boy];
            _suYakin = new bool[En, Boy];
            _toprak = new bool[En, Boy];
            _kaldirim = new bool[En, Boy];
            _surulu = new bool[En, Boy];
            _ray = new bool[En, Boy];
            _kalas = new bool[En, Boy];
            _dolu = new bool[En, Boy];

            KatmanlariKur();
            BolgeleriTanimla();

            NehirVeGol();
            YolAgi();
            TrenHatti();
            Avlular();
            SuTamponu();

            ZeminDose();

            Orman();
            MadenOcagi();
            EvVeCiftlik();
            Tarlalar();
            Depo();
            TrenGari();
            Market();
            MeyveBahcesi();
            Iskele();

            Serpistir();

            _zemin.RefreshAllTiles();
            _dekor.RefreshAllTiles();
            _nesne.RefreshAllTiles();
            _nesneUst.RefreshAllTiles();
        }

        private void KatmanlariKur()
        {
            var grid = gameObject.GetComponent<Grid>();
            if (grid == null) grid = gameObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            _zemin = Katman("Zemin", 0);
            _dekor = Katman("Dekor", 1);
            _nesne = Katman("Nesne", 2);
            _nesneUst = Katman("NesneUst", 3);
        }

        private Tilemap Katman(string ad, int sira)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(transform, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sira;
            // ⚠ Chunk: 163 bin hucre tek tek cizilirse kare hizi olur. Chunk mesh'ler.
            tr.mode = TilemapRenderer.Mode.Chunk;
            tr.sortOrder = TilemapRenderer.SortOrder.TopLeft;
            return tm;
        }

        private void BolgeleriTanimla()
        {
            Bolgeler.Clear();
            Bolgeler.Add(new Bolge { Ad = "orman",  Baslik = "Orman",       Seviye = 7, Alan = new RectInt(16, 180, 168, 118) });
            Bolgeler.Add(new Bolge { Ad = "maden",  Baslik = "Maden Ocagi", Seviye = 9, Alan = new RectInt(24, 30, 118, 84) });
            Bolgeler.Add(new Bolge { Ad = "ev",     Baslik = "Ev",          Seviye = 1, Alan = new RectInt(198, 150, 62, 46) });
            Bolgeler.Add(new Bolge { Ad = "tarla",  Baslik = "Tarla",       Seviye = 1, Alan = new RectInt(170, 66, 122, 58) });
            Bolgeler.Add(new Bolge { Ad = "depo",   Baslik = "Depo",        Seviye = 2, Alan = new RectInt(310, 146, 78, 50) });
            Bolgeler.Add(new Bolge { Ad = "market", Baslik = "Market",      Seviye = 6, Alan = new RectInt(392, 96, 106, 74) });
            Bolgeler.Add(new Bolge { Ad = "tren",   Baslik = "Tren Gari",   Seviye = 5, Alan = new RectInt(322, 14, 88, 46) });
        }

        // ------------------------------------------------------------------ arazi

        /// <summary>
        /// Buyuk gol (kuzeydogu) + ona baglanan iki nehir. Nehirler 5-7 karo genis:
        /// 3x3 autotile ic kose icermedigi icin dar kanal bozuk kiyi uretir.
        /// </summary>
        private void NehirVeGol()
        {
            // Gol: dalgali kenarli buyuk blob + ortada ada.
            Vector2 merkez = new Vector2(322f, 240f);
            for (int x = (int)merkez.x - 60; x <= (int)merkez.x + 60; x++)
            {
                for (int y = (int)merkez.y - 46; y <= (int)merkez.y + 46; y++)
                {
                    if (x < 0 || y < 0 || x >= En || y >= Boy) continue;
                    float dx = (x - merkez.x) / 52f, dy = (y - merkez.y) / 38f;
                    float kiyi = 1f + 0.22f * (Gurultu(x, y, 17, 31) - 0.5f) * 2f;
                    if (dx * dx + dy * dy <= kiyi * kiyi * 0.86f) _su[x, y] = true;
                }
            }
            // Ada
            for (int x = 306; x <= 330; x++)
                for (int y = 232; y <= 252; y++)
                {
                    float dx = (x - 318f) / 11f, dy = (y - 242f) / 9f;
                    if (dx * dx + dy * dy <= 1f) _su[x, y] = false;
                }

            // Kuzey nehri: ust kenardan gole.
            Serit(_su, new[]
            {
                new Vector2(236f, 324f), new Vector2(244f, 296f), new Vector2(262f, 274f),
                new Vector2(282f, 262f), new Vector2(295f, 252f),
            }, 3.4f, 0.8f);

            // Guney nehri: golden guney kenara. ⚠ Ana yolun (y=140) civarinda nehir
            // DIKEY tutulur: ilk surumde kavis yatay yolla dar acili kesisti ve yol
            // bandi nehir boyunca metrelerce kalas dosedi — kopru degil sal cikti.
            Serit(_su, new[]
            {
                new Vector2(300f, 212f), new Vector2(291f, 188f), new Vector2(290f, 156f),
                new Vector2(290f, 124f), new Vector2(296f, 92f), new Vector2(300f, 60f),
                new Vector2(292f, 28f),  new Vector2(290f, -4f),
            }, 3.2f, 0.9f);
        }

        private void YolAgi()
        {
            Vector2 kavsak = new Vector2(272f, 140f);   // vadinin gobegi

            foreach (var b in Bolgeler)
            {
                if (b.Ad == "orman") { Kol(kavsak, new Vector2(150f, 200f)); continue; }
                Kol(kavsak, b.Merkez);
            }
            // Dogu-bati ana yolu.
            Kol(new Vector2(4f, 128f), kavsak);
            Kol(kavsak, new Vector2(508f, 132f));
        }

        private void TrenHatti()
        {
            // Hattin toprak yatagi 3 karo, ortasinda ray.
            for (int x = 0; x < En; x++)
            {
                for (int y = 34; y <= 38; y++)
                {
                    if (y >= 35 && y <= 37) _toprak[x, y] = true;
                    if (y == 36) _ray[x, y] = true;
                }
            }
        }

        /// <summary>
        /// Avlular binalarin CEVRESI kadardir, bolgenin tamami degil.
        ///
        /// ⚠ Ilk surumde avlu = bolge ici ve 512x320 haritada bolgeler devasa; oyun
        /// yakinliginda ekran dolusu ciplak toprak cikti. Avlu, kadraj merkezinin
        /// (bolge merkezi) cevresinde iceriğe yetecek kadar tutulur.
        /// </summary>
        private void Avlular()
        {
            Avlu(Merkezli("market", 30, 22), 0, 0, 0, 0);
            Avlu(Merkezli("depo", 26, 18), 0, 0, 0, 0);
            Avlu(Merkezli("ev", 28, 18), 0, 0, 0, 0);
            Avlu(Merkezli("maden", 30, 20), 0, 0, 0, 0);
            RectInt g = Alan("tren");
            Avlu(new RectInt(g.xMin + 4, 30, g.width - 8, 22), 0, 0, 0, 0);

            // Kaldirim: market meydani (kucuk!) + gar peronu.
            KaldirimDose(Merkezli("market", 18, 10));
            KaldirimDose(new RectInt(g.xMin + 12, 40, g.width - 24, 5));
        }

        /// <summary>Bolge merkezine oturan en x boy dikdortgen.</summary>
        private RectInt Merkezli(string ad, int en, int boy)
        {
            Vector2 m = default;
            foreach (var b in Bolgeler) if (b.Ad == ad) m = b.Merkez;
            return new RectInt(Mathf.RoundToInt(m.x) - en / 2, Mathf.RoundToInt(m.y) - boy / 2, en, boy);
        }

        /// <summary>Bolgenin ic dikdortgenini toprak avlu yapar; kenari gurultuyle kirilir.</summary>
        private void Avlu(RectInt a, int sol, int alt, int sag, int ust)
        {
            int x0 = a.xMin + sol, x1 = a.xMax - sag;
            int y0 = a.yMin + alt, y1 = a.yMax - ust;
            for (int x = x0; x < x1; x++)
            {
                for (int y = y0; y < y1; y++)
                {
                    if (x < 0 || y < 0 || x >= En || y >= Boy || _su[x, y]) continue;
                    float kenar = Mathf.Min(Mathf.Min(x - x0, x1 - 1 - x),
                                            Mathf.Min(y - y0, y1 - 1 - y));
                    if (kenar < 3f && Gurultu(x, y, 6, 9) < 0.44f) continue;
                    _toprak[x, y] = true;
                }
            }
        }

        private void KaldirimDose(RectInt a)
        {
            for (int x = a.xMin; x < a.xMax; x++)
                for (int y = a.yMin; y < a.yMax; y++)
                    if (Icerde(x, y) && !_su[x, y]) { _kaldirim[x, y] = true; _toprak[x, y] = true; }
        }

        /// <summary>
        /// Su tamponu: toprak tipi maskeler suyun 1 karo yakinindan silinir, yol/ray
        /// suyu kestigi yerde KALAS olur (uzerine cit korkuluk Serpistir'de gelir).
        /// </summary>
        private void SuTamponu()
        {
            for (int x = 0; x < En; x++)
                for (int y = 0; y < Boy; y++)
                    if (_su[x, y])
                        for (int dx = -1; dx <= 1; dx++)
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx >= 0 && ny >= 0 && nx < En && ny < Boy) _suYakin[nx, ny] = true;
                            }

            for (int x = 0; x < En; x++)
            {
                for (int y = 0; y < Boy; y++)
                {
                    if (!_suYakin[x, y]) continue;
                    bool gecis = _toprak[x, y] || _ray[x, y];
                    if (_su[x, y] && gecis)
                    {
                        _kalas[x, y] = true;      // kopru dosemesi
                        _su[x, y] = false;
                    }
                    else if (gecis && !_su[x, y])
                    {
                        // Kiyida toprak birakma: SuBlok kiyisi cim gomulu.
                        if (!_kalas[x, y]) { _toprak[x, y] = false; _kaldirim[x, y] = false; }
                    }
                }
            }

            // ⚠ Kopru KIYIYA OTURSUN: kalas yalnizca yol∩su ile kalirsa iki ucta
            // (toprak silinen kiyi seridinde) cim boslugu kaliyor ve kopru nehre
            // degmeyen bir sal gibi gorunuyor. Kalas, kiyi seridindeki kalas-komsu
            // hucrelere bir adim genisletilir.
            var ek = new List<Vector2Int>();
            for (int x = 0; x < En; x++)
                for (int y = 0; y < Boy; y++)
                {
                    if (_kalas[x, y] || _su[x, y] || !_suYakin[x, y]) continue;
                    bool kalasKomsu =
                        (x > 0 && _kalas[x - 1, y]) || (x < En - 1 && _kalas[x + 1, y]) ||
                        (y > 0 && _kalas[x, y - 1]) || (y < Boy - 1 && _kalas[x, y + 1]);
                    if (kalasKomsu) ek.Add(new Vector2Int(x, y));
                }
            foreach (var pkt in ek) { _kalas[pkt.x, pkt.y] = true; _toprak[pkt.x, pkt.y] = false; }
        }

        private void Kol(Vector2 a, Vector2 b)
        {
            Vector2 kose = new Vector2(b.x, a.y);
            Serit(_toprak, new[] { a, kose, b }, 2.2f, 0f);
        }

        private void Serit(bool[,] maske, Vector2[] nokta, float yaricap, float dalga)
        {
            for (int i = 0; i < nokta.Length - 1; i++)
            {
                Vector2 a = nokta[i], b = nokta[i + 1];
                int adim = Mathf.CeilToInt(Vector2.Distance(a, b) * 2.5f);
                if (adim == 0) continue;
                for (int s = 0; s <= adim; s++)
                {
                    float t = s / (float)adim;
                    Vector2 p = Vector2.Lerp(a, b, t);
                    float r = yaricap + dalga * Mathf.Sin((i + t) * 2.1f);
                    Disk(maske, p, r);
                }
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

        // ------------------------------------------------------------------ zemin

        private void ZeminDose()
        {
            for (int x = 0; x < En; x++)
            {
                for (int y = 0; y < Boy; y++)
                {
                    if (_kalas[x, y])
                    {
                        // Yatay mi dikey mi: koprunun akis yonu komsusundan okunur.
                        bool yatay = KalasGibi(x - 1, y) || KalasGibi(x + 1, y);
                        Koy(_zemin, x, y, Ek(yatay ? Karo64.KalasYatay : Karo64.KalasDikey));
                        continue;
                    }
                    if (_su[x, y])
                    {
                        int i = Karo64.Dokuz(Karo64.SuBlok,
                                             Ayni(_su, x, y + 1), Ayni(_su, x, y - 1),
                                             Ayni(_su, x - 1, y), Ayni(_su, x + 1, y));
                        if (i == Karo64.SuBlok[1, 1])
                        {
                            i = _r.Next(100) < 4 ? Karo64.SuAdacik : Karo64.Sec(Karo64.Su, _r);
                        }
                        Koy(_zemin, x, y, Rpg(i));
                        continue;
                    }
                    if (_ray[x, y])
                    {
                        Koy(_zemin, x, y, Ek(Karo64.RayYatay));
                        continue;
                    }
                    if (_kaldirim[x, y])
                    {
                        Koy(_zemin, x, y, Ek(Karo64.Sec(Karo64.Kaldirim, _r)));
                        continue;
                    }
                    if (_surulu[x, y])
                    {
                        Koy(_zemin, x, y, Ek(Karo64.Surulu));
                        continue;
                    }
                    if (_toprak[x, y])
                    {
                        int i = Karo64.Dokuz(Karo64.ToprakBlok,
                                             ToprakGibi(x, y + 1), ToprakGibi(x, y - 1),
                                             ToprakGibi(x - 1, y), ToprakGibi(x + 1, y));
                        if (i == Karo64.ToprakBlok[1, 1])
                            i = Karo64.Sec(Karo64.Toprak, _r);
                        Koy(_zemin, x, y, Rpg(i));
                        continue;
                    }

                    // Cim: cayirlik bolgelerde tutamli/detayli, duzluklerde sade.
                    float cayir = Gurultu(x, y, 19, 1);
                    int c;
                    if (cayir > 0.62f && _r.Next(100) < 30) c = Karo64.Sec(Karo64.CimDetay, _r);
                    else c = Karo64.Sec(Karo64.Cim, _r);
                    Koy(_zemin, x, y, Rpg(c));
                }
            }
        }

        private static bool Ayni(bool[,] maske, int x, int y)
        {
            if (x < 0 || y < 0 || x >= En || y >= Boy) return true;
            return maske[x, y];
        }

        /// <summary>Toprak sayilan her sey — autotile kenarlarini kapatir.</summary>
        private bool ToprakGibi(int x, int y)
        {
            if (x < 0 || y < 0 || x >= En || y >= Boy) return true;
            return _toprak[x, y] || _surulu[x, y] || _kaldirim[x, y] || _ray[x, y] || _kalas[x, y];
        }

        private bool KalasGibi(int x, int y)
        {
            if (x < 0 || y < 0 || x >= En || y >= Boy) return false;
            return _kalas[x, y];
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
                    float kenar = Mathf.Min(Mathf.Min(x - a.xMin, a.xMax - 1 - x),
                                            Mathf.Min(y - a.yMin, a.yMax - 1 - y));
                    float sikilik = Gurultu(x, y, 15, 5);
                    float sans = Mathf.Lerp(6f, 64f, Mathf.Clamp01(kenar / 10f))
                                 * Mathf.Clamp01(sikilik * 1.6f);
                    if (_r.Next(100) >= sans) continue;

                    int t = _r.Next(100);
                    if (t < 46) Nesne(x, y, Rpg(Karo64.KavakYesil));
                    else if (t < 72) Nesne(x, y, Rpg(Karo64.KavakCam));
                    else if (t < 82) Nesne(x, y, Rpg(Karo64.TopYesil));
                    else if (t < 92) Dekor(x, y, Rpg(Karo64.Sec(Karo64.CaliBuyuk, _r)));
                    else Dekor(x, y, Rpg(Karo64.Sec(Karo64.Fidan, _r)));
                }
            }
            Nesne(a.xMin + a.width / 2, a.yMin - 2, Ek(Karo64.YonTabelasi));
        }

        private void MadenOcagi()
        {
            RectInt a = Alan("maden");
            int mx = a.xMin + a.width / 2, my = a.yMin + a.height / 2;

            // Ocak binasi kadraj merkezinin hemen ustunde: gri tas, kemerli agiz.
            EvInsa(mx - 3, my + 2, 6, 2, Karo64.DuvarGri, Karo64.CatiArduvaz, kemer: true);

            // Bina onunde islik: kasa/varil kumeleri + merdiven + isci.
            Nesne(mx - 6, my - 1, Rpg(Karo64.KasaBuyuk));
            Nesne(mx - 5, my - 3, Rpg(Karo64.GriVaril));
            Nesne(mx - 3, my - 2, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            Nesne(mx + 4, my - 1, Rpg(Karo64.KasaBuyuk));
            Nesne(mx + 5, my - 3, Rpg(Karo64.KasaKucuk));
            Nesne(mx + 6, my - 1, Rpg(Karo64.GriVaril));
            Nesne(mx - 7, my + 3, Rpg(Karo64.Merdiven));
            Nesne(mx + 1, my - 4, Ek(Karo64.KoyluErkek));
            Nesne(mx - 1, my - 6, Rpg(Karo64.Sec(Karo64.Comlek, _r)));

            // Bolgenin geri kalani kirac: seyrek cali + varil/comlek.
            for (int x = a.xMin; x < a.xMax; x++)
            {
                for (int y = a.yMin; y < a.yMax; y++)
                {
                    if (!Musait(x, y) || _r.Next(1000) >= 20) continue;
                    int t = _r.Next(100);
                    if (t < 45) Dekor(x, y, Rpg(Karo64.Sec(Karo64.CaliKucuk, _r)));
                    else if (t < 70) Nesne(x, y, Rpg(Karo64.GriVaril));
                    else Dekor(x, y, Rpg(Karo64.Sec(Karo64.Comlek, _r)));
                }
            }
            Nesne(a.xMin + a.width / 2, a.yMin - 2, Ek(Karo64.YonTabelasi));
        }

        private void EvVeCiftlik()
        {
            RectInt a = Alan("ev");
            int mx = a.xMin + a.width / 2, my = a.yMin + a.height / 2;

            // Ciftin evi kadraj merkezinin ustunde, ahir yaninda.
            // ⚠ KURAL (olculdu): kahve cati TOPRAK avluda kayboluyor — 4x buyutmede
            // ahirin catisi yerli yerindeydi ama toprakla ton tona oldugu icin "cati
            // yok" gorunuyordu. Toprak zeminli bolgelerde cati hep ARDUVAZ; kahve
            // cati yalniz cim/kaldirim komsulu yerlerde (market) kullanilir.
            EvInsa(mx - 8, my + 1, 6, 2, Karo64.DuvarKrem, Karo64.CatiArduvaz);
            EvInsa(mx + 1, my, 4, 1, Karo64.DuvarGri, Karo64.CatiArduvaz, kemer: true);

            // Ev onunde yasam: cift, saman, varil, comlek.
            Nesne(mx - 6, my - 2, Ek(Karo64.KoyluErkek));
            Nesne(mx - 4, my - 2, Ek(Karo64.KoyluKadin));
            Nesne(mx + 6, my - 2, Rpg(Karo64.SamanYigin));
            Nesne(mx + 7, my, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            Nesne(mx - 9, my - 1, Rpg(Karo64.Sec(Karo64.Comlek, _r)));

            // Citli otlak: avlunun guneyi, hayvanlar icinde.
            var otlak = new RectInt(mx - 12, my - 12, 24, 9);
            CitCevir(otlak);
            Nesne(mx - 9, my - 6, Ek(Karo64.Inek));
            Nesne(mx - 5, my - 8, Ek(Karo64.Inek));
            Nesne(mx - 1, my - 6, Ek(Karo64.Koyun));
            Nesne(mx + 3, my - 9, Ek(Karo64.Koyun));
            Nesne(mx + 6, my - 6, Ek(Karo64.Koyun));
            Nesne(mx + 8, my - 8, Ek(Karo64.Tavuk));
            Nesne(mx + 9, my - 6, Ek(Karo64.Tavuk));
            Nesne(mx - 3, my - 10, Ek(Karo64.Tavuk));
        }

        private void Tarlalar()
        {
            RectInt a = Alan("tarla");
            CitCevir(a);

            // Parseller: 6x4 saban izli bloklar, aralarinda yurume payi.
            int urunSayisi = Karo64.UrunOlgun.Length;
            for (int px = a.xMin + 3; px + 6 <= a.xMax - 3; px += 9)
            {
                for (int py = a.yMin + 3; py + 4 <= a.yMax - 3; py += 7)
                {
                    int urun = _r.Next(urunSayisi);
                    bool olgun = _r.Next(100) < 60;
                    for (int x = px; x < px + 6; x++)
                    {
                        for (int y = py; y < py + 4; y++)
                        {
                            // Yol/avlu hucresine parsel yazma: yol tarlanin icinden
                            // kesintisiz gecmeli, parsel yolu deliyordu.
                            if (!Icerde(x, y) || _su[x, y] || _suYakin[x, y] || _toprak[x, y]) continue;
                            _surulu[x, y] = true;
                            Koy(_zemin, x, y, Ek(Karo64.Surulu));
                            Dekor(x, y, Ek(olgun ? Karo64.UrunOlgun[urun] : Karo64.UrunGenc[urun]));
                        }
                    }
                }
            }
            Nesne(a.xMin + a.width / 2, a.yMax + 2, Ek(Karo64.YonTabelasi));
        }

        private void Depo()
        {
            RectInt a = Alan("depo");
            int mx = a.xMin + a.width / 2, my = a.yMin + a.height / 2;

            EvInsa(mx - 11, my + 1, 5, 2, Karo64.DuvarGri, Karo64.CatiArduvaz, kemer: true);
            EvInsa(mx + 2, my + 1, 5, 2, Karo64.DuvarGri, Karo64.CatiArduvaz, kemer: true);

            // Istif: binalarin onunde duzenli kasa/varil siralari.
            for (int sira = 0; sira < 2; sira++)
            {
                for (int i = 0; i < 8; i++)
                {
                    int x = mx - 10 + i * 3, y = my - 3 - sira * 3;
                    if (!Musait(x, y)) continue;
                    Nesne(x, y, Rpg((sira + i) % 2 == 0 ? Karo64.KasaBuyuk : Karo64.Sec(Karo64.Varil, _r)));
                }
            }
            Nesne(mx - 12, my - 4, Ek(Karo64.KoyluErkek));
            Nesne(mx + 11, my - 2, Rpg(Karo64.KasaKucuk));
            Nesne(mx, my - 7, Rpg(Karo64.SamanYigin));
            Nesne(a.xMin + a.width / 2, a.yMin - 2, Ek(Karo64.YonTabelasi));
        }

        private void TrenGari()
        {
            RectInt a = Alan("tren");
            int mx = a.xMin + a.width / 2;

            // Gar binasi peronun hemen kuzeyinde, vitrinli tas cephe.
            EvInsa(mx - 3, 46, 6, 1, Karo64.DuvarGri, Karo64.CatiArduvaz, vitrin: true);

            // Peronda bekleyen yuk ve yolcular.
            for (int i = 0; i < 5; i++)
                Nesne(mx + 5 + i * 2, 41 + (i % 2), Rpg(Karo64.KasaBuyuk));
            Nesne(mx - 6, 42, Ek(Karo64.KoyluKadin));
            Nesne(mx - 9, 41, Ek(Karo64.KoyluErkek));
            Nesne(mx - 12, 42, Rpg(Karo64.SamanYigin));
            Nesne(mx - 14, 41, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            Nesne(mx, a.yMax + 2, Ek(Karo64.YonTabelasi));
        }

        private void Market()
        {
            RectInt a = Alan("market");
            int mx = a.xMin + a.width / 2, my = a.yMin + a.height / 2;

            // Meydanin cevresi dukkan: kuzeyde iki vitrinli, dogu-batida birer ev.
            EvInsa(mx - 9, my + 6, 4, 1, Karo64.DuvarKrem, Karo64.CatiKahve, vitrin: true);
            EvInsa(mx - 2, my + 6, 3, 2, Karo64.DuvarGri, Karo64.CatiArduvaz);
            EvInsa(mx + 3, my + 6, 4, 1, Karo64.DuvarMavi, Karo64.CatiKahve, vitrin: true);
            EvInsa(mx - 14, my - 2, 4, 2, Karo64.DuvarGri, Karo64.CatiKahve);
            EvInsa(mx + 10, my - 2, 4, 2, Karo64.DuvarKrem, Karo64.CatiArduvaz);

            // Meydanda tezgahlar: kasa + comlek + saman, aralarinda koyluler.
            for (int i = 0; i < 4; i++)
            {
                int x = mx - 7 + i * 4, y = my + 1;
                Nesne(x, y, Rpg(Karo64.KasaBuyuk));
                Nesne(x + 1, y, Rpg(Karo64.Sec(Karo64.Comlek, _r)));
                if (i % 2 == 0) Nesne(x, y - 1, Rpg(Karo64.SamanYigin));
            }
            for (int i = 0; i < 3; i++)
            {
                int x = mx - 5 + i * 4, y = my - 3;
                Nesne(x, y, Rpg(Karo64.Sec(Karo64.Varil, _r)));
                Nesne(x + 1, y, Rpg(Karo64.KasaKucuk));
            }
            Nesne(mx - 2, my - 1, Ek(Karo64.KoyluKadin));
            Nesne(mx + 4, my + 2, Ek(Karo64.KoyluErkek));
            Nesne(mx - 8, my - 4, Ek(Karo64.KoyluKadin));
            Nesne(mx + 8, my - 5, Ek(Karo64.KoyluErkek));
            Nesne(mx, a.yMin - 2, Ek(Karo64.YonTabelasi));
        }

        /// <summary>Meyve bahcesi: golun batisinda, cit icinde duzenli top agac siralari.</summary>
        private void MeyveBahcesi()
        {
            var a = new RectInt(196, 232, 64, 52);
            CitCevir(a);
            for (int x = a.xMin + 3; x < a.xMax - 3; x += 4)
            {
                for (int y = a.yMin + 3; y < a.yMax - 3; y += 4)
                {
                    if (!Musait(x, y)) continue;
                    int t = (x / 4 + y / 4) % 3;
                    Nesne(x, y, Rpg(t == 0 ? Karo64.TopYesil : (t == 1 ? Karo64.TopCam : Karo64.TopYesilInce)));
                }
            }
        }

        /// <summary>Golun guney kiyisina kalas iskele + variller.</summary>
        private void Iskele()
        {
            int x0 = 316;
            int y = 205;
            // Kiyidan suya dogru uzanan iskele.
            for (int i = 0; i < 9; i++)
            {
                int yy = y + i;
                if (yy >= Boy) break;
                _kalas[x0, yy] = true; _kalas[x0 + 1, yy] = true;
                _su[x0, yy] = false; _su[x0 + 1, yy] = false;
                Koy(_zemin, x0, yy, Ek(Karo64.KalasDikey));
                Koy(_zemin, x0 + 1, yy, Ek(Karo64.KalasDikey));
            }
            Nesne(x0, y + 1, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            Nesne(x0 + 1, y + 7, Rpg(Karo64.KasaKucuk));
        }

        // ------------------------------------------------------------------ yapi

        /// <summary>
        /// Cok karolu ev kompozisyonu.
        ///
        /// Sema (asagidan yukari): duvar kati (sol/orta/sag) + kapi/pencere kaplamasi,
        /// ustune sacak sirasi, ustune <paramref name="catiKat"/> sira kiremit.
        /// Parcalar araclar/karo-indeks/ev-deneme.png ile GOZLE dogrulandi — mahya ve
        /// alinlik karolari (125/126) denendi ve celisik cikti, bilerek kullanilmiyor.
        /// </summary>
        private void EvInsa(int x0, int y0, int genislik, int catiKat,
                            Karo64.Duvar duvar, Karo64.Cati cati,
                            bool kemer = false, bool vitrin = false)
        {
            // Taban pedi: kapi onune ve binanin yanlarina bir sira kaldirim.
            // Bina ciplak topraga oturunca zeminle kaynasip "yapistirilmis" duruyor;
            // krem ped hem kontur veriyor hem girisi isaretliyor.
            for (int x = -1; x <= genislik; x++)
            {
                int px = x0 + x;
                if (!Icerde(px, y0 - 1) || _su[px, y0 - 1]) continue;
                _kaldirim[px, y0 - 1] = true; _toprak[px, y0 - 1] = true;
                Koy(_zemin, px, y0 - 1, Ek(Karo64.Sec(Karo64.Kaldirim, _r)));
            }

            // Duvar kati
            for (int x = 0; x < genislik; x++)
            {
                int i = x == 0 ? duvar.AltSol : (x == genislik - 1 ? duvar.AltSag : duvar.AltOrta);
                NesneKoy(x0 + x, y0, Rpg(i));
            }
            // Kapi ve pencereler ust katmana.
            int orta = x0 + genislik / 2;
            if (vitrin)
            {
                bool krem = duvar.AltOrta == Karo64.DuvarKrem.AltOrta;
                Koy(_nesneUst, orta, y0, Rpg(krem ? Karo64.DukkanCamKrem : Karo64.DukkanCamGri));
                if (genislik >= 3) Koy(_nesneUst, orta - 1, y0, Rpg(Karo64.KapiBuyukKahve));
            }
            else
            {
                Koy(_nesneUst, orta, y0, Rpg(kemer ? Karo64.KapiKemerKahve : Karo64.KapiBuyukKahve));
                if (genislik >= 3) Koy(_nesneUst, orta - 1, y0, Rpg(Karo64.PencereKahve));
                if (genislik >= 4) Koy(_nesneUst, orta + 1, y0, Rpg(Karo64.PencereKucuk));
            }
            // Sacak + kiremit katlari
            for (int x = 0; x < genislik; x++)
            {
                int e = x == 0 ? cati.EtekSol : (x == genislik - 1 ? cati.EtekSag : cati.EtekOrta);
                NesneKoy(x0 + x, y0 + 1, Rpg(e));
                for (int k = 0; k < catiKat; k++)
                {
                    int m = x == 0 ? cati.OrtaSol : (x == genislik - 1 ? cati.OrtaSag : cati.OrtaOrta);
                    NesneKoy(x0 + x, y0 + 2 + k, Rpg(m));
                }
            }
        }

        /// <summary>Cit: dikdortgenin cevresi, guney ortasinda kapi boslugu.</summary>
        private void CitCevir(RectInt a)
        {
            int kapi = a.xMin + a.width / 2;
            for (int x = a.xMin; x < a.xMax; x++)
            {
                if (Mathf.Abs(x - kapi) > 1 && Musait(x, a.yMin))
                    Dekor(x, a.yMin, Rpg(x == a.xMin ? Karo64.CitSolBas
                                        : (x == a.xMax - 1 ? Karo64.CitSagBas
                                        : Karo64.Sec(Karo64.CitOrta, _r))));
                if (Musait(x, a.yMax - 1))
                    Dekor(x, a.yMax - 1, Rpg(x == a.xMin ? Karo64.CitSolBas
                                            : (x == a.xMax - 1 ? Karo64.CitSagBas
                                            : Karo64.Sec(Karo64.CitOrta, _r))));
            }
            // ⚠ rpg-base'de dikey cit karosu YOK; yatay parcalar dikey dizilince
            // kopuk kopuk gorundu. Dikey kenarlar tek direk sirasi.
            for (int y = a.yMin + 1; y < a.yMax - 1; y++)
            {
                if (Musait(a.xMin, y)) Dekor(a.xMin, y, Ek(Karo64.CitDirek));
                if (Musait(a.xMax - 1, y)) Dekor(a.xMax - 1, y, Ek(Karo64.CitDirek));
            }
        }

        // ------------------------------------------------------------------ serpme

        /// <summary>
        /// Vadi geneli: gurultuyle kumelenen koruluk / cayir / acik arazi. Sonbahar
        /// kusagi golun kuzeydogusu — turuncu agaclar orada yogunlasir.
        /// </summary>
        private void Serpistir()
        {
            for (int x = 0; x < En; x++)
            {
                for (int y = 0; y < Boy; y++)
                {
                    if (_kalas[x, y])
                    {
                        // Korkuluk: koprunun su goren HER kenarina. Yatay kenara cit
                        // parcasi, dikey kenara direk — nehir dikey akarken koprunun
                        // dogu/bati kenarlari su gorur, ilk surum yalniz kuzey/guneye
                        // bakiyordu ve koprude hic korkuluk cikmamisti.
                        bool kuzey = y + 1 < Boy && !_kalas[x, y + 1] && _su[x, y + 1];
                        bool guney = y - 1 >= 0 && !_kalas[x, y - 1] && _su[x, y - 1];
                        bool bati = x > 0 && !_kalas[x - 1, y] && _su[x - 1, y];
                        bool dogu = x + 1 < En && !_kalas[x + 1, y] && _su[x + 1, y];
                        if (kuzey || guney) Dekor(x, y, Rpg(Karo64.Sec(Karo64.CitOrta, _r)));
                        else if (bati || dogu) Dekor(x, y, Ek(Karo64.CitDirek));
                        continue;
                    }
                    if (!Musait(x, y) || BolgedeMi(x, y)) continue;

                    bool sonbahar = x > 330 && y > 200;
                    float bitki = Gurultu(x, y, 21, 2);
                    int t = _r.Next(1000);

                    if (bitki > 0.66f)
                    {
                        if (t < 200)
                            Nesne(x, y, Rpg(sonbahar ? Karo64.KavakTuruncu
                                           : (t % 2 == 0 ? Karo64.KavakYesil : Karo64.KavakCam)));
                        else if (t < 280)
                            Nesne(x, y, Rpg(sonbahar ? Karo64.TopTuruncu : Karo64.TopYesil));
                        else if (t < 340) Dekor(x, y, Rpg(Karo64.Sec(Karo64.CaliBuyuk, _r)));
                    }
                    else if (bitki > 0.48f)
                    {
                        if (t < 60) Dekor(x, y, Rpg(Karo64.Sec(Karo64.CaliKucuk, _r)));
                        else if (t < 84) Dekor(x, y, Rpg(Karo64.Sec(Karo64.Fidan, _r)));
                        else if (t < 96)
                            Nesne(x, y, Rpg(sonbahar ? Karo64.TopTuruncuInce : Karo64.TopYesilInce));
                    }
                    else if (t < 12)
                    {
                        Dekor(x, y, Rpg(Karo64.Sec(Karo64.CaliKucuk, _r)));
                    }
                }
            }
        }

        // ------------------------------------------------------------------ gurultu

        /// <summary>Deterministik deger gurultusu — kumelenmenin kaynagi.</summary>
        private static float Gurultu(int x, int y, int olcek, int tuz)
        {
            int gx = Mathf.FloorToInt(x / (float)olcek);
            int gy = Mathf.FloorToInt(y / (float)olcek);
            float fx = x / (float)olcek - gx;
            float fy = y / (float)olcek - gy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Dugum(gx, gy, tuz), b = Dugum(gx + 1, gy, tuz);
            float c = Dugum(gx, gy + 1, tuz), d = Dugum(gx + 1, gy + 1, tuz);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

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

        private static bool Icerde(int x, int y) => x >= 0 && y >= 0 && x < En && y < Boy;

        private bool Musait(int x, int y)
        {
            if (!Icerde(x, y)) return false;
            return !_su[x, y] && !_ray[x, y] && !_kalas[x, y] && !_surulu[x, y] && !_dolu[x, y];
        }

        private static Karo64Kimlik Rpg(int i) => new Karo64Kimlik(Sayfa64.RpgBase, i);
        private static Karo64Kimlik Ek(int i) => new Karo64Kimlik(Sayfa64.Ek, i);

        private void Dekor(int x, int y, Karo64Kimlik k)
        {
            if (!Icerde(x, y)) return;
            Koy(_dekor, x, y, k);
            _dolu[x, y] = true;
        }

        private void Nesne(int x, int y, Karo64Kimlik k)
        {
            if (!Icerde(x, y)) return;
            Koy(_nesne, x, y, k);
            _dolu[x, y] = true;
        }

        /// <summary>Nesne katmanina koyar ve hucreyi kapatir (ev parcalari).</summary>
        private void NesneKoy(int x, int y, Karo64Kimlik k)
        {
            if (!Icerde(x, y)) return;
            Koy(_nesne, x, y, k);
            _dolu[x, y] = true;
        }

        private static void Koy(Tilemap tm, int x, int y, Karo64Kimlik karo)
        {
            Tile t = Onbellek(karo);
            if (t != null) tm.SetTile(new Vector3Int(x, y, 0), t);
        }

        /// <summary>Tile onbellegi: ayni karo tek ScriptableObject.</summary>
        private static Tile Onbellek(Karo64Kimlik karo)
        {
            long anahtar = (long)karo.Sayfa * 100000 + karo.Indeks;
            if (_onbellek.TryGetValue(anahtar, out Tile t) && t != null) return t;

            Sprite s = Karo64.Al(karo);
            if (s == null) return null;

            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.colliderType = Tile.ColliderType.None;
            t.name = s.name;
            _onbellek[anahtar] = t;
            return t;
        }

        public static void OnbellegiTemizle() => _onbellek.Clear();
    }
}
