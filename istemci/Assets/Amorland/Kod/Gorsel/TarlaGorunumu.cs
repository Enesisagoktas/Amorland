using System;
using System.Collections.Generic;
using UnityEngine;
using Amorland.Ag;
using Amorland.Durum;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Tarla sahnesinin gorsel katmani: karolar, urunler, yabani kaynaklar, kuyu.
    ///
    /// ⚠ Bu sinif OYUN KURALI ICERMEZ. Yalnizca <see cref="DunyaDurumu"/>'nu cizer.
    /// Buyume asamasi burada hesaplanir ama bu bir <b>gosterim</b> hesabidir; olgunluk
    /// karari sunucuya aittir ve istemci "olgun" gorunse bile hasat reddedilebilir.
    /// Sunucu saati kullanilir (mimari kural 5) — telefon saati ileri alinirsa
    /// urun erken olgun GORUNMEZ.
    /// </summary>
    public class TarlaGorunumu : MonoBehaviour
    {
        private DunyaDurumu _durum;
        private DengeVerisi _denge;

        private Transform _kok;
        private Transform _karoKok;
        private Transform _dugumKok;

        private readonly Dictionary<string, KaroGorseli> _karolar = new Dictionary<string, KaroGorseli>();
        private readonly Dictionary<string, DugumGorseli> _dugumler = new Dictionary<string, DugumGorseli>();
        private readonly Dictionary<int, Mesh> _urunMeshOnbellegi = new Dictionary<int, Mesh>();

        private GameObject _vurgu;
        private Mesh _karoMeshKuru;
        private Mesh _karoMeshSulu;
        private Mesh _karoMeshBos;

        private class KaroGorseli
        {
            public GameObject Zemin;
            public GameObject Urun;
            public int CizilenAsama = -1;
            public string CizilenUrun;
            public bool CizilenSulu;
            public float GeriSarmaBitis;
        }

        private class DugumGorseli
        {
            public GameObject Nesne;
            public bool CizilenHazir;
        }

        public void Kur(DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;

            _kok = new GameObject("Tarla").transform;
            _kok.SetParent(transform, false);
            _karoKok = new GameObject("Karolar").transform;
            _karoKok.SetParent(_kok, false);
            _dugumKok = new GameObject("Dugumler").transform;
            _dugumKok.SetParent(_kok, false);

            _karoMeshBos = MeshUretici.TarlaKarosu(Palet.CimKoyu);
            _karoMeshKuru = MeshUretici.TarlaKarosu(Palet.Toprak);
            _karoMeshSulu = MeshUretici.TarlaKarosu(Palet.TopraIslak);

            ZeminKur();
            KuyuKur();

            _vurgu = MateryalDeposu.Nesne(
                "Vurgu", MeshUretici.Cerceve(1f, 0.06f, Palet.UiVurgu), _kok, Vector3.zero);
            _vurgu.SetActive(false);

            _durum.DurumDegisti += _ => Tazele();
            _durum.GeriSarildi += GeriSarmaBaslat;
            Tazele();
        }

        private void ZeminKur()
        {
            var zemin = MateryalDeposu.Nesne(
                "Zemin", MeshUretici.Zemin(40f, 40f, Palet.Cim), _kok, new Vector3(4f, -0.01f, 2f));
            zemin.transform.localPosition = new Vector3(4f, -0.01f, 2f);
        }

        private void KuyuKur()
        {
            // Kuyu tarla sahnesinin ICINDE (B.11): su almak icin sahne degistirmek yok.
            MateryalDeposu.Nesne("Kuyu", MeshUretici.Kuyu(), _kok, KuyuKonumu);
        }

        /// <summary>Kuyu, parselin soluna yerlestirilir; yabani kaynak seridiyle cakismaz.</summary>
        public static readonly Vector3 KuyuKonumu = new Vector3(-4f, 0f, 1f);

        // ------------------------------------------------------------------ tazeleme

        public void Tazele()
        {
            if (_durum == null) return;

            foreach (var karo in _durum.Karolar.Values)
            {
                KaroCiz(karo);
            }
            foreach (var dugum in _durum.Dugumler.Values)
            {
                DugumCiz(dugum);
            }
        }

        private void KaroCiz(Karo karo)
        {
            if (!_karolar.TryGetValue(karo.Anahtar, out KaroGorseli g))
            {
                g = new KaroGorseli();
                bool benim = karo.ParselSahibi == _durum.BenimId;
                Vector3 konum = KaroKonumu(karo, benim);
                g.Zemin = MateryalDeposu.Nesne("Karo_" + karo.X + "_" + karo.Y, _karoMeshBos, _karoKok, konum);
                _karolar[karo.Anahtar] = g;
            }

            bool sulu = karo.SulandiAt != null;
            Mesh hedefMesh = karo.Bos ? _karoMeshBos : (sulu ? _karoMeshSulu : _karoMeshKuru);
            if (g.CizilenSulu != sulu || g.CizilenUrun != karo.UrunId)
            {
                var mf = g.Zemin.GetComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = hedefMesh;
                g.CizilenSulu = sulu;
            }

            int asama = BuyumeAsamasi(karo);
            if (g.CizilenUrun != karo.UrunId || g.CizilenAsama != asama)
            {
                if (g.Urun != null)
                {
                    UnityEngine.Object.Destroy(g.Urun);
                    g.Urun = null;
                }
                if (!karo.Bos)
                {
                    var urunBilgi = _denge.Urun(karo.UrunId);
                    Color renk = Palet.UrunRengi(asama, _denge.BuyumeAsamaSayisi);
                    if (urunBilgi != null && urunBilgi.Id == "bugday") renk = Palet.Bugday;
                    Mesh mesh = MeshUretici.Urun(asama, _denge.BuyumeAsamaSayisi, renk);
                    bool benim = karo.ParselSahibi == _durum.BenimId;
                    g.Urun = MateryalDeposu.Nesne(
                        "Urun", mesh, _karoKok, KaroKonumu(karo, benim) + Vector3.up * 0.06f);
                }
                g.CizilenUrun = karo.UrunId;
                g.CizilenAsama = asama;
            }
        }

        private void DugumCiz(Dugum dugum)
        {
            bool hazir = dugum.Hazir(_durum.SunucuZamani);
            if (!_dugumler.TryGetValue(dugum.Anahtar, out DugumGorseli g))
            {
                g = new DugumGorseli();
                g.Nesne = MateryalDeposu.Nesne(
                    "Dugum_" + dugum.Tur,
                    MeshUretici.YabaniKaynak(dugum.Tur),
                    _dugumKok,
                    new Vector3(dugum.X, 0f, dugum.Y));
                _dugumler[dugum.Anahtar] = g;
                g.CizilenHazir = !hazir;
            }
            if (g.CizilenHazir != hazir)
            {
                // Toplanmis kaynak KAYBOLMAZ, sadece silinir: oyuncu nerede oldugunu unutmasin.
                g.Nesne.transform.localScale = hazir ? Vector3.one : Vector3.one * 0.35f;
                g.CizilenHazir = hazir;
            }
        }

        // ------------------------------------------------------------------ hesaplar

        /// <summary>
        /// Gorsel buyume asamasi. Sulanmamis karo asama 0'da kalir — "biri eker, biri sular"
        /// kurali oyuncuya SESSIZCE degil, gorselde soylenir.
        /// </summary>
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

        /// <summary>
        /// Karo dunya konumu. Iki parsel yan yana dizilir; partnerin parseline
        /// DOKUNULAMAZ (B.7) ama GORUNUR — birlikte oynandigi hissi buradan gelir.
        /// </summary>
        public static Vector3 KaroKonumu(Karo karo, bool benimParselim)
        {
            float xKaydirma = benimParselim ? 0f : 8f;
            return new Vector3(karo.X + xKaydirma, 0f, karo.Y);
        }

        public Vector3 KaroKonumu(int x, int y) => new Vector3(x, 0f, y);

        // ------------------------------------------------------------------ vurgu ve geri sarma

        public void VurguGoster(Vector3 konum)
        {
            _vurgu.SetActive(true);
            _vurgu.transform.localPosition = konum + Vector3.up * 0.02f;
        }

        public void VurguGizle() => _vurgu.SetActive(false);

        /// <summary>
        /// Sunucu reddettiginde 0.2 sn yumusak geri sarma (B.3 kural 7).
        /// Sert ziplama YASAK: oyuncu ne oldugunu anlamaz ve oyunu bozuk sanir.
        /// </summary>
        private void GeriSarmaBaslat(string karoAnahtar)
        {
            if (_karolar.TryGetValue(karoAnahtar, out KaroGorseli g))
            {
                g.GeriSarmaBitis = Time.unscaledTime + _denge.GeriSarmaSn;
            }
        }

        private void Update()
        {
            if (_durum == null) return;

            foreach (var g in _karolar.Values)
            {
                if (g.GeriSarmaBitis <= 0f) continue;
                float kalan = g.GeriSarmaBitis - Time.unscaledTime;
                if (kalan <= 0f)
                {
                    g.GeriSarmaBitis = 0f;
                    g.Zemin.transform.localScale = Vector3.one;
                    continue;
                }
                float t = kalan / Mathf.Max(0.01f, _denge.GeriSarmaSn);
                g.Zemin.transform.localScale = Vector3.one * (1f + 0.08f * t);
            }
        }
    }
}
