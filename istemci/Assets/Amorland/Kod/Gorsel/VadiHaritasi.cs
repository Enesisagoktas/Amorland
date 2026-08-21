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

        /// <summary>
        /// Oyun capasi: sunucu karo (0,0)'in vadi hucre karsiligi. Oyun sahnesi
        /// vadi kokunu (-CapaX-0.5, -CapaY-0.5) konumuna koyar; boylece sunucu
        /// karo (x,y)'nin MERKEZI dunya (x,y) olur ve TUM oyun mantigi (izgara,
        /// niyetler, konum yayini) sunucu koordinatinda degismeden kalir.
        /// Tarla bolgesi (170,66,122,58) icinde secildi: parseller, kuyu (-4,1)
        /// ve izgara sinirlari (-12..24, -8..16) bolgeye rahatca sigar.
        /// </summary>
        public const int OyunCapaX = 214;
        public const int OyunCapaY = 88;

        public struct Bolge
        {
            public string Ad;
            public string Baslik;
            public int Seviye;
            public RectInt Alan;
            public Vector2 Merkez => new Vector2(Alan.x + Alan.width / 2f, Alan.y + Alan.height / 2f);
        }

        public readonly List<Bolge> Bolgeler = new List<Bolge>();

        private Tilemap _zemin, _dekor, _nesne, _nesneUst, _nesneTepe;
        private System.Random _r;

        private bool[,] _su;        // gol + nehirler
        private bool[,] _suYakin;   // suyun 1 karo genislemis hali
        private bool[,] _suDerin;   // kiyidan 2+ karo uzak su — koyu ton
        private bool[,] _toprak;    // yol + avlu + tarla tabani (toprak blok autotile)
        private bool[,] _kaldirim;  // arnavut kaldirimi (meydan, peron)
        private bool[,] _surulu;    // saban izli tarla parseli
        private bool[,] _ray;       // tren rayi
        private bool[,] _kalas;     // kopru / iskele dosemesi
        private bool[,] _dolu;      // dekor/nesne kaplamis
        private bool[,] _engel;     // karakterin GIREMEYECEGI hucre (bina, govde, kuyu)

        private static readonly Dictionary<long, Tile> _onbellek = new Dictionary<long, Tile>();

        // ------------------------------------------------------------------ kurulum

        public void Kur(bool oyunAlani = false)
        {
            _r = new System.Random(Tohum);
            _su = new bool[En, Boy];
            _suYakin = new bool[En, Boy];
            _suDerin = new bool[En, Boy];
            _toprak = new bool[En, Boy];
            _kaldirim = new bool[En, Boy];
            _surulu = new bool[En, Boy];
            _ray = new bool[En, Boy];
            _kalas = new bool[En, Boy];
            _dolu = new bool[En, Boy];
            _engel = new bool[En, Boy];

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
            Ada();

            Serpistir();
            SuDetayi();
            Balast();
            KoprulerSer();

            if (oyunAlani) OyunAlaniAc();

            _zemin.RefreshAllTiles();
            _dekor.RefreshAllTiles();
            _nesne.RefreshAllTiles();
            _nesneUst.RefreshAllTiles();
            // ⚠ NesneTepe listeye SONRADAN eklendi ve refresh'i unutulmustu:
            // bassiz renderda chunk mesh'lerinin yarisi guncellenmeden ciziliyor,
            // uzun agac tepelerinin ~%51'i gorunmuyordu (denetim bulgusu; veri
            // tamdi, KesikAgacDokumu 5435/5435). Katman ekleyen REFRESH de ekler.
            _nesneTepe.RefreshAllTiles();
        }

        private void KatmanlariKur()
        {
            var grid = gameObject.GetComponent<Grid>();
            if (grid == null) grid = gameObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            _zemin = Katman("Zemin", 0);
            _dekor = Katman("Dekor", 1);
            // ⚠ 2.5D: Nesne ve NesneUst BIREYSEL modda ve AYNI sirada (2) —
            // kamera Y ekseni sort'uyla karakter agacin/binanin arkasina gecince
            // ortulur. Kapi/pencere ayri katman ama ayni sirada: duvarla ayni
            // hucrede oldugundan Y-sort duzeni bozmaz, karakter kapi onunde
            // dururken (bir alt hucre) dogru sekilde onde cizilir.
            _nesne = Katman("Nesne", 2, bireysel: true);
            _nesneUst = Katman("NesneUst", 2, bireysel: true);
            // Kanopi tepeleri: her zaman ONDE — karakter govdenin arkasindayken
            // kanopinin altindan gecmis gibi gorunur.
            _nesneTepe = Katman("NesneTepe", 4);
        }

        private Tilemap Katman(string ad, int sira, bool bireysel = false)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(transform, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sira;
            // ⚠ Zemin/dekor CHUNK (163 bin hucre tek tek cizilmez); nesne
            // katmanlari BIREYSEL — Y-sort ancak karo basina calisir. Bireysel
            // katmanlarda yalniz nesneler var (~4-5 bin karo), ekranda ayni anda
            // birkac yuz gorunur; B.2 butcesine sigar.
            tr.mode = bireysel ? TilemapRenderer.Mode.Individual : TilemapRenderer.Mode.Chunk;
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
            // ⚠ Gar dikdortgeni guneye 4 karo genis: kadraj bolge merkezine bakar ve
            // ilk halinde gar binasi kadrajin ustunde kaliyordu (inceleme bulgusu).
            Bolgeler.Add(new Bolge { Ad = "tren",   Baslik = "Tren Gari",   Seviye = 5, Alan = new RectInt(322, 18, 88, 44) });
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

            // Orman yolunun ucunu meyve bahcesinin guney kapisina bagla — cikmaz yol
            // ve haritadan kopuk bahce tek hamlede cozulur (inceleme bulgusu).
            Kol(new Vector2(150f, 200f), new Vector2(228f, 226f));
            Serit(_toprak, new[] { new Vector2(228f, 226f), new Vector2(228f, 233f) }, 2.0f, 0f);
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

        /// <summary>Ray yataginin iki yanina seyrek balast cakili — hattin zemini
        /// baglanir (inceleme bulgusu: ray toprakta yuzuyordu).</summary>
        private void Balast()
        {
            for (int x = 0; x < En; x += 1)
            {
                if (_r.Next(100) < 40 && Icerde(x, 35) && _toprak[x, 35] && !_kalas[x, 35])
                    Dekor(x, 35, Ek(Karo64.Cakiltasi));
                if (_r.Next(100) < 40 && Icerde(x, 37) && _toprak[x, 37] && !_kalas[x, 37])
                    Dekor(x, 37, Ek(Karo64.Cakiltasi));
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
                        // ⚠ SU SILINMEZ: kalas artik Dekor katmaninda suyun USTUNE
                        // serilir. Ilk kurguda su siliniyordu ve nehir koprunun iki
                        // yaninda kapak karosuyla bitiyordu — kopru suyun degil
                        // cimin ustunde duruyor gibiydi (denetim bulgusu).
                        _kalas[x, y] = true;
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

            // Derin su: 5x5 komsulugun tamami su olan hucreler. Iki tonlu su golun
            // ortasina derinlik hissi verir (inceleme bulgusu: tek duz camgobegi).
            for (int x = 0; x < En; x++)
            {
                for (int y = 0; y < Boy; y++)
                {
                    if (!_su[x, y]) continue;
                    // ⚠ 7x7 erozyon: 5x5 nehirde jilet kenarli dikey bant uretti
                    // (nehir ~7 genis, cekirdek tek sutun kaliyordu). 7x7 ile derin
                    // bolge yalniz golde olusur; kenari asagida titretilir.
                    bool derin = true;
                    for (int dx = -3; dx <= 3 && derin; dx++)
                        for (int dy = -3; dy <= 3 && derin; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= En || ny >= Boy) continue;
                            if (!_su[nx, ny]) derin = false;
                        }
                    if (derin && Gurultu(x, y, 5, 13) < 0.35f) derin = false;
                    _suDerin[x, y] = derin;
                }
            }
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
                    if (_su[x, y])
                    {
                        int i = Karo64.Dokuz(Karo64.SuBlok,
                                             Ayni(_su, x, y + 1), Ayni(_su, x, y - 1),
                                             Ayni(_su, x - 1, y), Ayni(_su, x + 1, y));
                        if (i == Karo64.SuBlok[1, 1])
                        {
                            if (_suDerin[x, y]) { Koy(_zemin, x, y, Ek(Karo64.DerinSu)); continue; }
                            i = Karo64.Sec(Karo64.Su, _r);
                        }
                        Koy(_zemin, x, y, Rpg(i));
                        continue;
                    }
                    if (_ray[x, y] && !_kalas[x, y])
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

                    // Cim: iki ayri gurultu katmani.
                    // ⚠ Golgelendirme (36/37) buyuk olcekli gurultuyle gelir — cayira
                    // derinlik veren sey bu. Karolarin kenar halkasi orijinal cimle
                    // ayni oldugu icin leke sinirlari dikissiz.
                    float golgelik = Gurultu(x, y, 27, 7);
                    float cayir = Gurultu(x, y, 19, 1);
                    // ⚠ Cayir lekeleri ORMANDA calismaz: kenar karolari ormanin
                    // koyu tabani icinde parlak kare cukurlar birakiyordu — iki
                    // ayri leke sistemi ayni zemin icin yarisamaz.
                    bool ormanIci = Alan("orman").Contains(new Vector2Int(x, y));
                    if (golgelik < 0.34f && !ormanIci)
                    {
                        // ⚠ Leke ICI duz koyu (65), leke KENARI merkez-koyu gecis
                        // karosu (36). Merkez-koyu karo genis bitisik alanda dama
                        // deseni uretiyor (derin su dersi); kenarindaki halkasi
                        // orijinal cim oldugu icin gecisi o tasir.
                        bool kenar = Gurultu(x, y + 1, 27, 7) >= 0.34f
                                     || Gurultu(x, y - 1, 27, 7) >= 0.34f
                                     || Gurultu(x - 1, y, 27, 7) >= 0.34f
                                     || Gurultu(x + 1, y, 27, 7) >= 0.34f;
                        // Sonbahar kusaginda lekeler sicak tona doner — turuncu
                        // agaclar yesil zeminde igreti duruyordu.
                        bool sonbaharZemin = x > 330 && y > 200;
                        int ic = sonbaharZemin ? Karo64.SonbaharIci : Karo64.OrmanTabani;
                        int kn = sonbaharZemin ? Karo64.SonbaharKenari : Karo64.CimGolgeHafif;
                        Koy(_zemin, x, y, Ek(kenar ? kn : ic));
                    }
                    else
                    {
                        int c = (cayir > 0.62f && _r.Next(100) < 30)
                                ? Karo64.Sec(Karo64.CimDetay, _r)
                                : Karo64.Sec(Karo64.Cim, _r);
                        Koy(_zemin, x, y, Rpg(c));
                    }
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

            // Oduncu patikasi: yol agzindan ormanin icine kivrilan cignenmis iz.
            // Toprak autotile derdi yok — koyu cim seridi + seyrek cakil "iz" okunur.
            Vector2[] patika =
            {
                new Vector2(150f, 200f), new Vector2(132f, 222f), new Vector2(110f, 238f),
                new Vector2(92f, 252f), new Vector2(78f, 262f),
            };
            var iz = new bool[En, Boy];
            Serit(iz, patika, 1.4f, 0f);
            for (int x = 0; x < En; x++)
                for (int y = 0; y < Boy; y++)
                    if (iz[x, y] && Musait(x, y))
                    {
                        Koy(_zemin, x, y, Ek(Karo64.OrmanTabani));
                        _dolu[x, y] = true;   // agac gelmesin
                        if (_r.Next(100) < 18) Koy(_dekor, x, y, Ek(Karo64.Cakiltasi));
                    }

            // Aciklik: patikanin ucunda agacsiz alan + odun istifi ve devrik kutukler.
            Vector2 aciklik = new Vector2(78f, 262f);
            for (int x = (int)aciklik.x - 6; x <= (int)aciklik.x + 6; x++)
                for (int y = (int)aciklik.y - 5; y <= (int)aciklik.y + 5; y++)
                {
                    if (!Icerde(x, y)) continue;
                    float dx = x - aciklik.x, dy = y - aciklik.y;
                    if (dx * dx + dy * dy <= 27f) _dolu[x, y] = true;
                }
            GolgeliNesne((int)aciklik.x - 2, (int)aciklik.y + 1, Ek(Karo64.OdunIstifi));
            GolgeliNesne((int)aciklik.x + 2, (int)aciklik.y - 1, Ek(Karo64.OdunIstifi));
            Koy(_dekor, (int)aciklik.x + 1, (int)aciklik.y + 3, Ek(Karo64.DevrikKutuk));
            Koy(_dekor, (int)aciklik.x - 3, (int)aciklik.y - 2, Ek(Karo64.DevrikKutuk));

            for (int x = a.xMin; x < a.xMax; x++)
            {
                for (int y = a.yMin; y < a.yMax; y++)
                {
                    float kenar = Mathf.Min(Mathf.Min(x - a.xMin, a.xMax - 1 - x),
                                            Mathf.Min(y - a.yMin, a.yMax - 1 - y));
                    float sikilik = Gurultu(x, y, 15, 5);
                    // ⚠ Taban basimi MUSAIT KONTROLUNDEN ONCE: taban zemin isidir,
                    // hucrenin dolu olmasi onu ilgilendirmez. Eski sirada agac
                    // kanopisinin isgal ettigi hucreye taban basilmiyor ve koyu
                    // denizde agac basina bir ACIK KARE kaliyordu ("renk bozuklugu").
                    if (sikilik > 0.50f && !_toprak[x, y] && !_su[x, y])
                    {
                        bool sinir = Gurultu(x, y + 1, 15, 5) <= 0.50f
                                     || Gurultu(x, y - 1, 15, 5) <= 0.50f
                                     || Gurultu(x - 1, y, 15, 5) <= 0.50f
                                     || Gurultu(x + 1, y, 15, 5) <= 0.50f;
                        Koy(_zemin, x, y, Ek(sinir ? Karo64.CimGolgeHafif : Karo64.OrmanTabani));
                    }
                    if (!Musait(x, y)) continue;
                    float sans = Mathf.Lerp(8f, 74f, Mathf.Clamp01(kenar / 10f))
                                 * Mathf.Clamp01(sikilik * 1.6f);
                    if (_r.Next(100) >= sans) continue;

                    int t = _r.Next(100);
                    // Siluet cesitliligi: ucgen %72'ydi, yuvarlak %10 (denetim) —
                    // top agaclarin payi artti, ince top ormana girdi.
                    if (t < 26) UzunAgac(x, y, 0);
                    else if (t < 40) GolgeliNesne(x, y, Ek(Karo64.KavakKoyu));
                    else if (t < 54) UzunAgac(x, y, 1);
                    else if (t < 62) GolgeliNesne(x, y, Rpg(Karo64.KavakCam));
                    else if (t < 76) GolgeliNesne(x, y, Ek(Karo64.TopKoyu));
                    else if (t < 84) GolgeliNesne(x, y, Ek(Karo64.TopInceKoyu));
                    else if (t < 90) Dekor(x, y, BuyukCali());
                    else if (t < 96) Dekor(x, y, Rpg(Karo64.Sec(Karo64.Fidan, _r)));
                    else Dekor(x, y, Ek(Karo64.DevrikKutuk));
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

            // Bina arkasi kaya yamaci: ocak bir tepeye oyulmus okunsun.
            for (int rx = mx - 6; rx <= mx + 8; rx++)
            {
                int ry = my + 6 + ((rx * 7) % 3);
                if (Musait(rx, ry) && _r.Next(100) < 78)
                    GolgeliNesne(rx, ry, Ek(Karo64.TasYigini));
            }

            // Agizdan cikan kisa ray + uzerinde maden vagonu — mekanin kimligi
            // (inceleme bulgusu: 'burasi neden maden, okunmuyor').
            for (int ry = my - 3; ry <= my + 1; ry++)
                if (Icerde(mx, ry)) Koy(_zemin, mx, ry, Ek(Karo64.RayDikey));
            // Vagon RAYDA ORTALI durmali: GolgeliNesne'nin konum jitteri onu
            // ray ekseninden 5 px kaydiriyordu (denetim) — varyantsiz yerlesir.
            if (_dekor.GetTile(new Vector3Int(mx, my - 1, 0)) == null)
                Koy(_dekor, mx, my - 1, Ek(Karo64.Golge));
            Koy(_nesne, mx, my - 1, Ek(Karo64.MadenVagonu));
            _dolu[mx, my - 1] = true;

            // Bina onunde islik: kasa/varil kumeleri + merdiven + isci.
            GolgeliNesne(mx - 6, my - 1, Rpg(Karo64.KasaBuyuk));
            GolgeliNesne(mx - 5, my - 3, Rpg(Karo64.GriVaril));
            GolgeliNesne(mx - 3, my - 2, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            GolgeliNesne(mx + 4, my - 1, Rpg(Karo64.KasaBuyuk));
            GolgeliNesne(mx + 5, my - 3, Rpg(Karo64.KasaKucuk));
            GolgeliNesne(mx + 6, my - 1, Rpg(Karo64.GriVaril));
            GolgeliNesne(mx + 1, my - 4, Ek(Karo64.KoyluErkek));
            Nesne(mx - 1, my - 6, Rpg(Karo64.Sec(Karo64.Comlek, _r)));

            // Bolgenin geri kalani kirac: seyrek cali + varil/comlek.
            for (int x = a.xMin; x < a.xMax; x++)
            {
                for (int y = a.yMin; y < a.yMax; y++)
                {
                    if (!Musait(x, y) || _r.Next(1000) >= 26) continue;
                    int t = _r.Next(100);
                    if (t < 26) Dekor(x, y, KucukCali());
                    else if (t < 44) GolgeliNesne(x, y, Ek(Karo64.TasYigini));
                    else if (t < 56) GolgeliNesne(x, y, Ek(Karo64.Cevher));
                    else if (t < 74) Dekor(x, y, Ek(Karo64.Cakiltasi));
                    else if (t < 88) Nesne(x, y, Rpg(Karo64.GriVaril));
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
            EvInsa(mx - 8, my + 1, 6, 2, Karo64.DuvarKrem, Karo64.CatiArduvaz, duman: true);
            EvInsa(mx + 1, my, 4, 1, Karo64.DuvarGri, Karo64.CatiArduvaz, kemer: true);

            // Ev onunde yasam: cift, saman, varil, comlek.
            GolgeliNesne(mx - 6, my - 2, Ek(Karo64.KoyluErkek));
            GolgeliNesne(mx - 4, my - 2, Ek(Karo64.KoyluKadin));
            GolgeliNesne(mx + 6, my - 2, Ek(Karo64.SamanYigini));
            Nesne(mx + 7, my, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            Nesne(mx - 9, my - 1, Rpg(Karo64.Sec(Karo64.Comlek, _r)));

            // Citli otlak: avlunun guneyi, hayvanlar icinde.
            var otlak = new RectInt(mx - 12, my - 12, 24, 9);
            CitCevir(otlak);
            GolgeliNesne(mx - 9, my - 6, Ek(Karo64.Inek));
            GolgeliNesne(mx - 5, my - 8, Ek(Karo64.Inek));
            GolgeliNesne(mx - 1, my - 6, Ek(Karo64.Koyun));
            GolgeliNesne(mx + 3, my - 9, Ek(Karo64.Koyun));
            GolgeliNesne(mx + 6, my - 6, Ek(Karo64.Koyun));
            GolgeliNesne(mx + 8, my - 8, Ek(Karo64.Tavuk));
            GolgeliNesne(mx + 9, my - 6, Ek(Karo64.Tavuk));
            GolgeliNesne(mx - 3, my - 10, Ek(Karo64.Tavuk));

            // Bakim izleri: kumes, su yalagi, saman — hayvanlar sahipsiz durmasin
            // (inceleme bulgusu: 'kimse onlara bakmiyor hissi').
            GolgeliNesne(mx + 10, my - 10, Ek(Karo64.Kumes));
            GolgeliNesne(mx - 7, my - 5, Ek(Karo64.Yalak));
            GolgeliNesne(mx - 11, my - 9, Ek(Karo64.SamanYigini));
            GolgeliNesne(mx + 1, my - 8, Ek(Karo64.SamanYigini));
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
            // Istif SIKI: aralikli dama 'dagilmis esya' okunuyordu (denetim).
            for (int sira = 0; sira < 2; sira++)
            {
                for (int i = 0; i < 10; i++)
                {
                    int x = mx - 10 + i * 2, y = my - 3 - sira * 2;
                    if (!Musait(x, y)) continue;
                    GolgeliNesne(x, y, (sira + i) % 3 == 2
                                 ? Rpg(Karo64.Sec(Karo64.Varil, _r))
                                 : Ek(Karo64.SandikKontrast));
                }
            }
            GolgeliNesne(mx - 12, my - 4, Ek(Karo64.KoyluErkek));
            Nesne(mx + 11, my - 2, Rpg(Karo64.KasaKucuk));
            Nesne(mx, my - 7, Ek(Karo64.SamanYigini));
            Nesne(a.xMin + a.width / 2, a.yMin - 2, Ek(Karo64.YonTabelasi));
        }

        private void TrenGari()
        {
            RectInt a = Alan("tren");
            int mx = a.xMin + a.width / 2;

            // Gar binasi peronun hemen kuzeyinde, vitrinli tas cephe.
            EvInsa(mx - 3, 46, 6, 1, Karo64.DuvarGri, Karo64.CatiArduvaz, vitrin: true, duman: true);

            // Peron dekoru: ~20 karoluk platform 6 sandikla bombostu (denetim).
            for (int pi = 0; pi < 3; pi++)
            {
                if (Icerde(mx - 8 + pi * 3, 42))
                    GolgeliNesne(mx - 8 + pi * 3, 42, Ek(Karo64.SandikKontrast));
            }
            if (Icerde(mx + 8, 42)) GolgeliNesne(mx + 8, 42, Ek(Karo64.Fener));

            // Peronda bekleyen yuk ve yolcular.
            for (int i = 0; i < 5; i++)
                GolgeliNesne(mx + 5 + i * 2, 41 + (i % 2), Rpg(Karo64.KasaBuyuk));
            GolgeliNesne(mx - 6, 42, Ek(Karo64.KoyluKadin));
            GolgeliNesne(mx - 9, 41, Ek(Karo64.KoyluErkek));
            Nesne(mx - 16, 41, Ek(Karo64.Fener));
            Nesne(mx + 20, 41, Ek(Karo64.Fener));
            GolgeliNesne(mx - 12, 42, Ek(Karo64.SamanYigini));
            GolgeliNesne(mx - 14, 41, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            Nesne(mx, a.yMax + 2, Ek(Karo64.YonTabelasi));
        }

        private void Market()
        {
            RectInt a = Alan("market");
            int mx = a.xMin + a.width / 2, my = a.yMin + a.height / 2;

            // Meydanin cevresi dukkan: kuzeyde iki vitrinli, dogu-batida birer ev.
            EvInsa(mx - 9, my + 6, 4, 1, Karo64.DuvarKrem, Karo64.CatiArduvaz, vitrin: true, duman: true);
            EvInsa(mx - 2, my + 6, 3, 2, Karo64.DuvarGri, Karo64.CatiArduvaz);
            EvInsa(mx + 3, my + 6, 4, 1, Karo64.DuvarMavi, Karo64.CatiArduvaz, vitrin: true, duman: true);
            EvInsa(mx - 14, my - 2, 4, 2, Karo64.DuvarGri, Karo64.CatiArduvaz);
            EvInsa(mx + 10, my - 2, 4, 2, Karo64.DuvarKrem, Karo64.CatiArduvaz);

            // Pazar: tenteli tezgahlar onde, mal yiginlari arkalarinda kumeli.
            for (int sira = 0; sira < 2; sira++)
            {
                for (int i = 0; i < 4; i++)
                {
                    int x = mx - 7 + i * 4 + (sira % 2) * 2, y = my + 1 - sira * 4;
                    if (!Icerde(x, y)) continue;
                    GolgeliNesne(x, y, Ek((sira + i) % 2 == 0 ? Karo64.Tezgah : Karo64.TezgahB));
                    if (i % 2 == sira % 2)
                        Nesne(x + 1, y - 1, Rpg(Karo64.Sec(Karo64.Comlek, _r)));
                    else if (_r.Next(2) == 0)
                        Nesne(x - 1, y - 1, Rpg(Karo64.KasaKucuk));
                }
            }
            GolgeliNesne(mx - 10, my - 3, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            GolgeliNesne(mx + 10, my - 4, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            GolgeliNesne(mx - 2, my - 1, Ek(Karo64.KoyluKadin));
            GolgeliNesne(mx + 4, my + 2, Ek(Karo64.KoyluErkek));
            GolgeliNesne(mx - 8, my - 4, Ek(Karo64.KoyluKadin));
            GolgeliNesne(mx + 8, my - 5, Ek(Karo64.KoyluErkek));
            Nesne(mx, a.yMin - 2, Ek(Karo64.YonTabelasi));

            // Meydanin kalbi: kuyu. Koseler fenerli, girisler cicek tarhli.
            GolgeliNesne(mx, my - 1, Ek(Karo64.Kuyu));
            _engel[mx, my - 1] = true;
            Nesne(mx - 8, my + 3, Ek(Karo64.Fener));
            Nesne(mx + 8, my + 3, Ek(Karo64.Fener));
            Nesne(mx - 8, my - 6, Ek(Karo64.Fener));
            Nesne(mx + 8, my - 6, Ek(Karo64.Fener));
            Nesne(mx - 5, my + 4, Ek(Karo64.CicekTarhi));
            Nesne(mx + 5, my + 4, Ek(Karo64.CicekTarhi));
            // ⚠ Yan tarhlar (mx±12) dogu/bati evlerinin CATISINA denk geliyordu —
            // ayni katmana SetTile kiremidi silip altindan cim gosterdi. Kuyu yanina.
            Nesne(mx - 3, my - 1, Ek(Karo64.CicekTarhi));
            Nesne(mx + 3, my - 1, Ek(Karo64.CicekTarhi));
        }

        /// <summary>Meyve bahcesi: golun batisinda, cit icinde duzenli top agac siralari.</summary>
        private RectInt _bahce;

        private void MeyveBahcesi()
        {
            var a = new RectInt(196, 232, 64, 52);
            _bahce = a;   // Serpistir bahceyi dislar — yabani dekor icine tasmasin
            CitCevir(a);
            for (int x = a.xMin + 3; x < a.xMax - 3; x += 4)
            {
                for (int y = a.yMin + 3; y < a.yMax - 3; y += 4)
                {
                    if (!Musait(x, y)) continue;
                    int t = (x / 4 + y / 4) % 3;
                    GolgeliNesne(x, y, t == 0 ? Ek(Karo64.TopKoyu) : (t == 1 ? Rpg(Karo64.TopCam) : Ek(Karo64.TopInceKoyu)));
                }
            }
        }

        /// <summary>Gol adasi: iki agac, kaya, cicekler — bos yesil disk olmasin.</summary>
        private void Ada()
        {
            GolgeliNesne(315, 244, Ek(Karo64.TopKoyu));
            GolgeliNesne(321, 240, Rpg(Karo64.TopTuruncuInce));
            GolgeliNesne(318, 238, Ek(Karo64.TasYigini));
            if (Icerde(313, 239)) Koy(_dekor, 313, 239, Ek(Karo64.Cicekler[0]));
            if (Icerde(322, 244)) Koy(_dekor, 322, 244, Ek(Karo64.Cicekler[2]));
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
                // Kalas isareti yeter — KoprulerSer dosemeyi Dekor'a serer,
                // su iskelenin altindan gorunur.
                _kalas[x0, yy] = true; _kalas[x0 + 1, yy] = true;
            }
            GolgeliNesne(x0, y + 1, Rpg(Karo64.Sec(Karo64.Varil, _r)));
            GolgeliNesne(x0 + 1, y + 7, Rpg(Karo64.KasaKucuk));
            // Iskeleye bagli sandal — suyun ustunde, Dekor katmaninda.
            if (Icerde(x0 + 3, y + 6) && _su[x0 + 3, y + 6])
                Dekor(x0 + 3, y + 6, Ek(Karo64.Sandal));
            if (Icerde(x0 - 2, y + 4) && _su[x0 - 2, y + 4])
                Dekor(x0 - 2, y + 4, Ek(Karo64.Sandal));
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
                            bool kemer = false, bool vitrin = false, bool duman = false)
        {
            // Sacak golgesi: kaide pedinin ustune binen solan serit — bina zemine
            // oturur (inceleme bulgusu: binalar 'yapistirilmis kagit' gibiydi).
            for (int x = 0; x < genislik; x++)
                if (Icerde(x0 + x, y0 - 1))
                    Koy(_dekor, x0 + x, y0 - 1, Ek(Karo64.GolgeSerit));

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
                // Cam vitrin (denetim: rpg cam karolari duvarla ayni renkteydi).
                Koy(_nesneUst, orta, y0, Ek(Karo64.Vitrin));
                if (genislik >= 3) Koy(_nesneUst, orta - 1, y0, Rpg(Karo64.KapiBuyukKahve));
                // Dukkan tabelasi sacaktan sarkar.
                // Tabela cizimi karonun USTUNDEN sarkar (denetim); hucre bir ust
                // satira cikti ki pano cephe hizasina, askilar sacaga otursun.
                if (Icerde(orta, y0 + 2)) Koy(_nesneUst, orta, y0 + 2, Ek(Karo64.AsiliTabela));
            }
            else if (kemer)
            {
                // Maden agzi: kapi kaplamasi degil duvarin KENDI kemer karosu —
                // 234 overlay'i ortadan ikiye bolunmus gorunuyordu (denetim).
                NesneKoy(orta, y0, Rpg(duvar.Kemer));
                if (genislik >= 3) Koy(_nesneUst, orta - 1, y0, Rpg(Karo64.PencereKucuk));
                if (genislik >= 4) Koy(_nesneUst, orta + 1, y0, Rpg(Karo64.PencereKucuk));
            }
            else
            {
                Koy(_nesneUst, orta, y0, Rpg(Karo64.KapiBuyukKahve));
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

            // Baca: en ust kiremit sirasinin solunda. Duman istenirse bir ust hucrede
            // (orasi catinin ustu — arkadaki cimin uzerine puf).
            int bacaX = x0 + 1, bacaY = y0 + 1 + catiKat;
            Koy(_nesneUst, bacaX, bacaY, Ek(Karo64.Baca));
            if (duman && Icerde(bacaX, bacaY + 1))
                Koy(_nesneUst, bacaX, bacaY + 1, Ek(Karo64.DumanKucuk));
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
            // Dikey kenarlar cift rayli dikey cit karosuyla. ⚠ Musait degil
            // Icerde kontrolu: cicek/ot dekoru hucreyi 'dolu' yapinca cit
            // atlaniyor ve kenarda bosluklar kaliyordu (denetim bulgusu) —
            // cit dekoru ezer.
            for (int y = a.yMin + 1; y < a.yMax - 1; y++)
            {
                if (Icerde(a.xMin, y) && !_su[a.xMin, y] && !_engel[a.xMin, y])
                    Dekor(a.xMin, y, Ek(Karo64.CitDikeyKaro));
                if (Icerde(a.xMax - 1, y) && !_su[a.xMax - 1, y] && !_engel[a.xMax - 1, y])
                    Dekor(a.xMax - 1, y, Ek(Karo64.CitDikeyKaro));
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
                        // Korkuluk NESNE katmaninda (kalas artik Dekor'u kaplıyor;
                        // ayrica Y-sort sayesinde karakter korkulugun online/arkasina
                        // dogru gecer). Yatay kenara cit, dikey kenara dikey cit karosu.
                        // suYakin da sayilir: kiyi ucundaki kalas hucreleri
                        // korkuluksuz kaliyordu, kopru 'yarim' okunuyordu (denetim).
                        bool kuzey = y + 1 < Boy && !_kalas[x, y + 1]
                                     && (_su[x, y + 1] || _suYakin[x, y + 1]);
                        bool guney = y - 1 >= 0 && !_kalas[x, y - 1]
                                     && (_su[x, y - 1] || _suYakin[x, y - 1]);
                        bool bati = x > 0 && !_kalas[x - 1, y]
                                    && (_su[x - 1, y] || _suYakin[x - 1, y]);
                        bool dogu = x + 1 < En && !_kalas[x + 1, y]
                                    && (_su[x + 1, y] || _suYakin[x + 1, y]);
                        if (kuzey || guney) Koy(_nesne, x, y, Rpg(Karo64.Sec(Karo64.CitOrta, _r)));
                        else if (bati || dogu) Koy(_nesne, x, y, Ek(Karo64.CitDikeyKaro));
                        continue;
                    }
                    if (!Musait(x, y) || BolgedeMi(x, y)) continue;
                    if (_bahce.Contains(new Vector2Int(x, y))) continue;

                    bool sonbahar = x > 330 && y > 200;
                    float bitki = Gurultu(x, y, 21, 2);
                    int t = _r.Next(1000);

                    if (bitki > 0.66f)
                    {
                        if (t < 110)
                            UzunAgac(x, y, sonbahar ? 2 : (t % 2 == 0 ? 0 : 1));
                        else if (t < 200)
                            GolgeliNesne(x, y, sonbahar ? Rpg(Karo64.KavakTuruncu)
                                           : (t % 2 == 0 ? Ek(Karo64.KavakKoyu) : Rpg(Karo64.KavakCam)));
                        else if (t < 280)
                            GolgeliNesne(x, y, sonbahar ? Rpg(Karo64.TopTuruncu) : Ek(Karo64.TopKoyu));
                        else if (t < 340) Dekor(x, y, BuyukCali());
                    }
                    else if (bitki > 0.48f)
                    {
                        if (t < 60) Dekor(x, y, KucukCali());
                        else if (t < 84) Dekor(x, y, Rpg(Karo64.Sec(Karo64.Fidan, _r)));
                        else if (t < 96)
                            GolgeliNesne(x, y, sonbahar ? Rpg(Karo64.TopTuruncuInce) : Ek(Karo64.TopInceKoyu));
                    }
                    else if (t < 12)
                    {
                        Dekor(x, y, KucukCali());
                    }
                    else if (t < 30 && Gurultu(x, y, 27, 7) > 0.6f)
                    {
                        // Cicekler acik cayirin AYDINLIK lekelerinde toplanir.
                        Dekor(x, y, Ek(Karo64.Sec(Karo64.Cicekler, _r)));
                    }
                }
            }
        }

        /// <summary>
        /// Su detaylari: kiyi sazlari + acik suda niluferler.
        /// Serpistir'den AYRI: kiyi hucresi tanimi su komsuluguna bakar.
        /// </summary>
        private void SuDetayi()
        {
            for (int x = 0; x < En; x++)
            {
                for (int y = 0; y < Boy; y++)
                {
                    if (_su[x, y])
                    {
                        // Nilufer: yalnizca acik suda (dort komsusu da su) ve golde.
                        bool acikSu = Ayni(_su, x, y + 1) && Ayni(_su, x, y - 1)
                                      && Ayni(_su, x - 1, y) && Ayni(_su, x + 1, y);
                        if (acikSu && y > 200 && _r.Next(1000) < 14)
                            Dekor(x, y, Ek(Karo64.Nilufer));
                        else if (acikSu && _r.Next(1000) < 30)
                            Dekor(x, y, Ek(Karo64.Dalga));
                        continue;
                    }
                    if (!_suYakin[x, y] || !Musait(x, y) || BolgedeMi(x, y)) continue;
                    if (_r.Next(100) < 34) Dekor(x, y, Ek(Karo64.Saz));
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

        /// <summary>
        /// Kopru/iskele dosemesini Dekor katmanina serer — altindaki su gorunur
        /// kalir, nehir koprunun ALTINDAN akar. Ray hattiyla kesisen kalaslara
        /// ray birlesim karosu gelir (tren koprusunde cizgi kopmasin).
        /// </summary>
        private void KoprulerSer()
        {
            for (int x = 0; x < En; x++)
            {
                for (int y = 0; y < Boy; y++)
                {
                    if (!_kalas[x, y]) continue;
                    bool yatay = KalasGibi(x - 1, y) || KalasGibi(x + 1, y);
                    int karo = _ray[x, y] ? Karo64.KopruRay
                              : (yatay ? Karo64.KalasYatay : Karo64.KalasDikey);
                    Koy(_dekor, x, y, Ek(karo));
                }
            }
        }

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

        /// <summary>
        /// Oyun modunda tarla bolgesinin ORTASINI oynanabilir alan icin bosaltir:
        /// dekoratif parseller, agaclar ve serpistirilmis nesneler kalkar, zemin
        /// cime doner. Gercek parselleri sunucu verisiyle TarlaGorunumu cizer.
        /// Bolgenin cit cevirisi ve disarida kalan dekor korunur — oyun alani
        /// vadinin icinde bir "temizlenmis tarla" gibi durur, ayri bir kutu gibi degil.
        /// </summary>
        private void OyunAlaniAc()
        {
            // Sunucu uzayinda (-7..17) x (-4..7); ustte +1 satir kanopi payi.
            var a = new RectInt(OyunCapaX - 7, OyunCapaY - 4, 25, 12);
            for (int x = a.xMin; x <= a.xMax; x++)
            {
                for (int y = a.yMin; y <= a.yMax + 1; y++)
                {
                    if (!Icerde(x, y)) continue;
                    var h = new Vector3Int(x, y, 0);
                    _nesne.SetTile(h, null);
                    _nesneUst.SetTile(h, null);
                    _nesneTepe.SetTile(h, null);
                    if (y > a.yMax) continue;   // kanopi satiri: yalniz tepe temizligi
                    _dekor.SetTile(h, null);
                    _dolu[x, y] = false;
                    _engel[x, y] = false;
                    // Dekoratif surulu parsel kalkar, zemin cime doner. Yol (toprak),
                    // kaldirim ve su oldugu gibi kalir — yol tarladan gecmeye devam eder.
                    if (_surulu[x, y])
                    {
                        _surulu[x, y] = false;
                        Koy(_zemin, x, y, Rpg(Karo64.Sec(Karo64.Cim, _r)));
                    }
                }
            }
        }

        private bool Musait(int x, int y)
        {
            if (!Icerde(x, y)) return false;
            return !_su[x, y] && !_ray[x, y] && !_kalas[x, y] && !_surulu[x, y] && !_dolu[x, y];
        }

        /// <summary>
        /// Kesif haritasi pikselleri: her OLCEKxOLCEK blok tek piksel. Oncelik
        /// su > kalas > kaldirim > ray > surulu > toprak > cim; nesne katmani
        /// minimap'te gosterilmez (sis zaten kesfi anlatir).
        /// </summary>
        public Color32[] MinimapPikselleri(int olcek, out int w, out int h)
        {
            w = En / olcek;
            h = Boy / olcek;
            var px = new Color32[w * h];
            for (int by = 0; by < h; by++)
            {
                for (int bx = 0; bx < w; bx++)
                {
                    int su = 0, kalas = 0, kaldirim = 0, ray = 0, surulu = 0, toprak = 0;
                    for (int dx = 0; dx < olcek; dx++)
                        for (int dy = 0; dy < olcek; dy++)
                        {
                            int x = bx * olcek + dx, y = by * olcek + dy;
                            if (_su[x, y]) su++;
                            else if (_kalas[x, y]) kalas++;
                            else if (_kaldirim[x, y]) kaldirim++;
                            else if (_ray[x, y]) ray++;
                            else if (_surulu[x, y]) surulu++;
                            else if (_toprak[x, y]) toprak++;
                        }
                    Color32 r;
                    int esik = olcek * olcek / 4;
                    if (su > esik) r = new Color32(96, 190, 214, 255);
                    else if (kalas > 0) r = new Color32(150, 108, 72, 255);
                    else if (kaldirim > esik) r = new Color32(206, 192, 160, 255);
                    else if (ray > 0) r = new Color32(120, 104, 96, 255);
                    else if (surulu > esik) r = new Color32(142, 96, 60, 255);
                    else if (toprak > esik) r = new Color32(190, 138, 90, 255);
                    else r = new Color32(112, 168, 62, 255);
                    px[by * w + bx] = r;
                }
            }
            return px;
        }

        /// <summary>Karakter bu hucrede yuruyebilir mi.</summary>
        public bool Gecilebilir(int x, int y)
        {
            if (!Icerde(x, y)) return false;
            if (_engel[x, y]) return false;
            if (_su[x, y] && !_kalas[x, y]) return false;
            return true;
        }

        /// <summary>Buyuk cali: yesil secilirse koyu turev (hayalet cali duzeltmesi).</summary>
        private Karo64Kimlik BuyukCali()
        {
            int c = Karo64.Sec(Karo64.CaliBuyuk, _r);
            return c == 180 ? Ek(Karo64.CaliBuyukKoyu) : Rpg(c);
        }

        private Karo64Kimlik KucukCali()
        {
            int c = Karo64.Sec(Karo64.CaliKucuk, _r);
            return c == 181 ? Ek(Karo64.CaliKucukKoyu) : Rpg(c);
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

        /// <summary>
        /// Ayakta duran nesne (agac, hayvan, koylu, varil): altina yumusak golge.
        ///
        /// ⚠ Golgesiz hali "kagittan cikartma" gibi duruyordu — nesneler zeminde
        /// degil zeminin USTUNDE yuzuyordu. Golge Dekor katmanina gider; ayni
        /// hucrede cit/ekin varsa golge atlanir (ustune binmesin).
        /// </summary>
        /// <summary>
        /// 2 karo boyunda agac: golge + govde (Nesne, Y-sortlu, ENGEL) + kanopi
        /// (NesneTepe, hep onde). tur: 0 koyu kavak, 1 cam, 2 turuncu.
        /// </summary>
        private void UzunAgac(int x, int y, int tur)
        {
            if (!Icerde(x, y) || !Icerde(x, y + 1)) return;
            // Kanopi hucresi onceden doluysa (mantar, kutuk...) agac dikilmez:
            // kanopi dekoru gomup 'bosa yerlesim' uretiyordu (denetim).
            if (_dolu[x, y + 1]) return;
            int alt = tur == 0 ? Karo64.UzunKavakAlt : (tur == 1 ? Karo64.UzunCamAlt : Karo64.UzunTuruncuAlt);
            int ust = alt + 1;
            // ⚠ Uzun agacta varyant kaydirma YOK: alt/ust iki ayri karo — kaydirma
            // ikilisinde gorunum sorunlari dogurdu; govdeli agacta izgara hissi
            // zaten dusuk.
            const int varyant = 0;
            if (_dekor.GetTile(new Vector3Int(x, y, 0)) == null)
                Koy(_dekor, x, y, Ek(Karo64.Golge), varyant);
            Koy(_nesne, x, y, Ek(alt), varyant);
            Koy(_nesneTepe, x, y + 1, Ek(ust), varyant);
            _dolu[x, y] = true;
            _dolu[x, y + 1] = true;
            _engel[x, y] = true;
        }

        private void GolgeliNesne(int x, int y, Karo64Kimlik k)
        {
            if (!Icerde(x, y)) return;
            // Konuma bagli deterministik varyant: govde ve golge birlikte kayar.
            int varyant = ((x * 73856093) ^ (y * 19349663)) & 3;
            if (_dekor.GetTile(new Vector3Int(x, y, 0)) == null)
                Koy(_dekor, x, y, Ek(Karo64.Golge), varyant);
            Koy(_nesne, x, y, k, varyant);
            _dolu[x, y] = true;
        }

        /// <summary>Nesne katmanina koyar, hucreyi kapatir ve ENGEL yapar (ev parcalari).</summary>
        private void NesneKoy(int x, int y, Karo64Kimlik k)
        {
            if (!Icerde(x, y)) return;
            Koy(_nesne, x, y, k);
            _dolu[x, y] = true;
            _engel[x, y] = true;
        }

        private static void Koy(Tilemap tm, int x, int y, Karo64Kimlik karo)
        {
            Koy(tm, x, y, karo, 0);
        }

        /// <summary>
        /// varyant 1-3: karoyu hucre icinde bir tutam kaydirir (Tile.transform).
        /// Agaclar hucre merkezine kilitlenince orman ekili misir tarlasi gibi
        /// izgara hissi veriyordu; kucuk kaydirma dogal dagilim yaniltmasi yaratir.
        /// Golge ve govde AYNI varyantla cagrilir ki birlikte kaysinlar.
        /// </summary>
        private static void Koy(Tilemap tm, int x, int y, Karo64Kimlik karo, int varyant)
        {
            Tile t = Onbellek(karo, varyant);
            if (t != null) tm.SetTile(new Vector3Int(x, y, 0), t);
        }

        private static readonly Vector3[] _varyantKaydirma =
        {
            Vector3.zero,
            new Vector3(-0.16f, 0.05f, 0f),
            new Vector3(0.14f, -0.04f, 0f),
            new Vector3(0.05f, 0.12f, 0f),
        };

        /// <summary>Tile onbellegi: ayni karo+varyant tek ScriptableObject.</summary>
        private static Tile Onbellek(Karo64Kimlik karo, int varyant)
        {
            long anahtar = ((long)karo.Sayfa * 100000 + karo.Indeks) * 8 + varyant;
            if (_onbellek.TryGetValue(anahtar, out Tile t) && t != null) return t;

            Sprite s = Karo64.Al(karo);
            if (s == null) return null;

            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.colliderType = Tile.ColliderType.None;
            t.name = s.name + "_v" + varyant;
            if (varyant > 0)
                t.transform = Matrix4x4.Translate(_varyantKaydirma[varyant]);
            _onbellek[anahtar] = t;
            return t;
        }

        public static void OnbellegiTemizle() => _onbellek.Clear();
    }
}
