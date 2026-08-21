using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Amorland.Ag;
using Amorland.Durum;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Sunucu parsel durumunun 2.5D vadi ustune cizimi.
    ///
    /// ⚠ Bu sinif eski 3D "tarla kutusunun" yerini alir: dunya artik VADIDIR
    /// (<see cref="VadiHaritasi"/>, oyunAlani modunda) ve bu gorunum yalniz
    /// SUNUCUYA BAGLI dinamikleri cizer — parsel zeminleri, ekin evreleri,
    /// yabani kaynak dugumleri, kuyu ve dokunma vurgusu. Statik dekor (evler,
    /// citler, agaclar) vadinin isidir; burada tekrar cizilmez.
    ///
    /// Koordinat sozlesmesi: Oyun.SahneKur bu kokun konumunu (-0.5,-0.5) yapar;
    /// boylece tilemap hucresi (x,y)'nin MERKEZI dunya (x,y) olur ve butun
    /// siniflar SUNUCU karo koordinatiyla konusmaya devam eder. Vadi koku da
    /// ayni sozlesmeyle (-OyunCapa-0.5) kaydirilir; iki izgara ust uste oturur.
    ///
    /// Ekin evreleri 3 gorseldir: filiz -> genc -> olgun. Sunucunun N asamasi
    /// bu uc gorsele katlanir; miktar/sure hesabi YOKTUR (mimari kural 7),
    /// yalniz gorunum secilir.
    /// </summary>
    public class TarlaGorunumu : MonoBehaviour
    {
        /// <summary>Kuyu karosu (sunucu uzayi, z=0). Kova doldurma hedefi.</summary>
        public static readonly Vector3 KuyuKonumu = new Vector3(-4f, 1f, 0f);

        private const int ParselEn = 6;
        private const int ParselBoy = 4;
        /// <summary>Partner parseli gorsel olarak +8 kaydirilir (B.7).</summary>
        private const int PartnerKaydirma = 8;

        private DunyaDurumu _durum;
        private DengeVerisi _denge;
        private Tilemap _zemin;   // parsel topragi — vadi Dekor katmaniyla ayni sirada
        private Tilemap _bitki;   // ekin/dugum — BIREYSEL, karakterle Y-sortlanir
        private Transform _vurgu;

        private static readonly Dictionary<int, Tile> _karoOnbellek = new Dictionary<int, Tile>();
        private static Sprite _cerceve;

        public void Kur(DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;

            var grid = gameObject.GetComponent<Grid>();
            if (grid == null) grid = gameObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            _zemin = Katman("ParselZemin", 1, bireysel: false);
            _bitki = Katman("ParselBitki", 2, bireysel: true);

            KuyuKur();
            VurguKur();

            // Sunucudan her degisim geldiginde (ve iyimser tahmin geri sarildiginda)
            // tum parseller yeniden cizilir: ~60 hucre, fark takibi gerektirmez.
            _durum.DurumDegisti += _ => Tazele();
            _durum.GeriSarildi += _ => Tazele();
            Tazele();
        }

        // ------------------------------------------------------------------ cizim

        public void Tazele()
        {
            if (_durum == null || _bitki == null) return;

            _bitki.ClearAllTiles();
            PedBas(0);
            PedBas(PartnerKaydirma);

            foreach (var karo in _durum.BenimKarolarim) KaroCiz(karo, 0);
            foreach (var karo in _durum.PartnerKarolari) KaroCiz(karo, PartnerKaydirma);
            foreach (var dugum in _durum.Dugumler.Values) DugumCiz(dugum);
        }

        /// <summary>
        /// Bos parsel pedi: KURU SURULU karik. "Burasi benim tarlam" bilgisi
        /// sunucudan karo gelmeden de gorunur; silinen karonun altindan taban geri
        /// cikar. ⚠ Duz toprak KULLANILMAZ: vadinin toprak yolu parselin hemen
        /// yanindan gectigi icin ped yola karisiyor ve parsel siniri kayboluyordu —
        /// karik deseni parseli yoldan tek bakista ayirir.
        /// </summary>
        private void PedBas(int kaydirma)
        {
            for (int x = 0; x < ParselEn; x++)
            {
                for (int y = 0; y < ParselBoy; y++)
                {
                    _zemin.SetTile(
                        new Vector3Int(x + kaydirma, y, 0), K(Sayfa64.Ek, Karo64.Surulu));
                }
            }
        }

        private void KaroCiz(Karo karo, int kaydirma)
        {
            var h = new Vector3Int(karo.X + kaydirma, karo.Y, 0);
            // Islak/kuru farki desenden degil tondan okunur (surulu_islak turevi).
            _zemin.SetTile(h, K(Sayfa64.Ek,
                karo.SulandiAt != null ? Karo64.SuruluIslak : Karo64.Surulu));
            if (karo.Bos) return;

            int g = UrunGorseli(karo.UrunId);
            int n = Mathf.Max(1, _denge.BuyumeAsamaSayisi);
            int indeks;
            if (Olgun(karo)) indeks = Karo64.UrunOlgun[g];
            else if (karo.SulandiAt != null && BuyumeAsamasi(karo) * 2 >= n) indeks = Karo64.UrunGenc[g];
            else indeks = Karo64.Filiz;   // yeni ekilmis / sulanmamis: iyimser aninda filiz
            _bitki.SetTile(h, K(Sayfa64.Ek, indeks));
        }

        private void DugumCiz(Dugum dugum)
        {
            string tur = dugum.Tur ?? "";
            int indeks;
            if (tur.Contains("dal") || tur.Contains("odun") || tur.Contains("kutuk"))
                indeks = Karo64.DevrikKutuk;
            else if (tur.Contains("cevher") || tur.Contains("maden")) indeks = Karo64.Cevher;
            else if (tur.Contains("tas") || tur.Contains("kaya")) indeks = Karo64.TasYigini;
            else indeks = Karo64.CaliKucukKoyu;   // yem_otu ve digerleri
            _bitki.SetTile(new Vector3Int(dugum.X, dugum.Y, 0), K(Sayfa64.Ek, indeks));
        }

        /// <summary>
        /// 10 sunucu urununu 4 gorsel aileye esler (bugday / kirmizi yumru /
        /// turuncu yumru / havuc). Gorsel yaklasiktir ama TUTARLIDIR: ayni urun
        /// her zaman ayni sprite ile cizilir.
        /// </summary>
        private static int UrunGorseli(string urunId)
        {
            switch (urunId)
            {
                case "bugday": case "misir": case "aycicegi": return 0;
                case "turp": case "domates": case "cilek": return 1;
                case "kabak": case "patates": return 2;
                case "havuc": case "marul": return 3;
                default:
                    int h = 0;
                    if (!string.IsNullOrEmpty(urunId)) foreach (char c in urunId) h = h * 31 + c;
                    return ((h % 4) + 4) % 4;
            }
        }

        // ------------------------------------------------------------------ buyume (okuma aninda hesap, kural 2)

        public int BuyumeAsamasi(Karo karo)
        {
            if (karo.Bos) return 0;
            if (karo.SulandiAt == null) return 0;

            var urun = _denge.Urun(karo.UrunId);
            if (urun == null) return 0;

            long sureSn = karo.HasatSayaci == 0
                ? urun.BuyumeSn
                : (urun.YenidenVerir ? urun.YenidenVerirAraSn : urun.BuyumeSn);
            if (sureSn <= 0) return _denge.BuyumeAsamaSayisi - 1;

            double gecen = (_durum.SunucuZamani - karo.SulandiAt.Value).TotalSeconds;
            int n = _denge.BuyumeAsamaSayisi;
            int asama = (int)Math.Floor(gecen * n / sureSn);
            return Mathf.Clamp(asama, 0, n - 1);
        }

        public bool Olgun(Karo karo) =>
            !karo.Bos && karo.SulandiAt != null && BuyumeAsamasi(karo) >= _denge.BuyumeAsamaSayisi - 1;

        /// <summary>Kalan sure (saniye). Negatifse olgun.</summary>
        public double KalanSn(Karo karo)
        {
            if (karo.Bos || karo.SulandiAt == null) return double.MaxValue;
            var urun = _denge.Urun(karo.UrunId);
            if (urun == null) return double.MaxValue;
            long sureSn = karo.HasatSayaci == 0
                ? urun.BuyumeSn
                : (urun.YenidenVerir ? urun.YenidenVerirAraSn : urun.BuyumeSn);
            return sureSn - (_durum.SunucuZamani - karo.SulandiAt.Value).TotalSeconds;
        }

        // ------------------------------------------------------------------ kuyu + vurgu

        private void KuyuKur()
        {
            var golge = KuyuNesnesi("KuyuGolge", Karo64.Al(Sayfa64.Ek, Karo64.Golge), 1);
            golge.localScale = new Vector3(1.1f, 0.9f, 1f);
            KuyuNesnesi("Kuyu", Karo64.Al(Sayfa64.Ek, Karo64.Kuyu), 2);
        }

        private Transform KuyuNesnesi(string ad, Sprite sprite, int sira)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(transform, false);
            // Hucre merkezi: karo + (0.5, 0.5) — kok zaten -0.5 kaydirilmis.
            go.transform.localPosition =
                new Vector3(KuyuKonumu.x + 0.5f, KuyuKonumu.y + 0.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sira;
            return go.transform;
        }

        private void VurguKur()
        {
            var go = new GameObject("Vurgu");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CerceveSprite();
            sr.sortingOrder = 3;
            _vurgu = go.transform;
            go.SetActive(false);
        }

        /// <summary>Hedef karoyu altin cerceveyle isaretler (x,y = sunucu karosu).</summary>
        public void VurguGoster(Vector3 karo)
        {
            if (_vurgu == null) return;
            _vurgu.localPosition = new Vector3(karo.x + 0.5f, karo.y + 0.5f, 0f);
            _vurgu.gameObject.SetActive(true);
        }

        public void VurguGizle()
        {
            if (_vurgu != null) _vurgu.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ altyapi

        private Tilemap Katman(string ad, int sira, bool bireysel)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(transform, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = sira;
            tr.mode = bireysel ? TilemapRenderer.Mode.Individual : TilemapRenderer.Mode.Chunk;
            tr.sortOrder = TilemapRenderer.SortOrder.TopLeft;
            return tm;
        }

        private static Tile K(Sayfa64 sayfa, int indeks)
        {
            int anahtar = ((int)sayfa << 16) | indeks;
            if (!_karoOnbellek.TryGetValue(anahtar, out Tile t) || t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = Karo64.Al(sayfa, indeks);
                _karoOnbellek[anahtar] = t;
            }
            return t;
        }

        private static Sprite CerceveSprite()
        {
            if (_cerceve != null) return _cerceve;
            const int B = 32;
            const int Kalinlik = 3;
            var doku = new Texture2D(B, B, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            Color bos = new Color(0f, 0f, 0f, 0f);
            Color renk = Palet.UiVurgu;
            for (int y = 0; y < B; y++)
            {
                for (int x = 0; x < B; x++)
                {
                    bool kenar = x < Kalinlik || y < Kalinlik
                                 || x >= B - Kalinlik || y >= B - Kalinlik;
                    doku.SetPixel(x, y, kenar ? renk : bos);
                }
            }
            doku.Apply();
            _cerceve = Sprite.Create(doku, new Rect(0, 0, B, B), new Vector2(0.5f, 0.5f), B);
            return _cerceve;
        }
    }
}
