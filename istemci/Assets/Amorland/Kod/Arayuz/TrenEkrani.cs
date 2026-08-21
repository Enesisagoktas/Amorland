using System;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Ag;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Tren panosu (B.15) — oyunun ekonomik kalbi ve hikayenin nabzi: tren her
    /// 6 saatte bir gelir, 3 siparis karti asar, kacan sefer geri gelmez.
    ///
    /// ⚠ Kartlar SUNUCUDAN okunur (GET /tren) ve istemci hicbir fiyat HESAPLAMAZ
    /// (mimari kural 7): pano tamamen sunucunun soyledigidir, istemci yalnizca
    /// "elimde yeterli var mi" tahminini renkle soyler.
    ///
    /// ⚠ "Ilk ulasan kazanir" (C.5) burada gorunur olmali: karsilanan kartta
    /// KIMIN karsiladigi yazar — cift ayni karta kosarsa kaybeden bunu ekranda
    /// gorur, sessiz bir red yasamaz.
    ///
    /// Kabuk B.22 stil sozlesmesi: krem parsomen govde, KOYU yazi, kalin kahve
    /// cerceve; karsilanmis kart ALTIN cerceve (tamamlanmislik = altin dili).
    /// </summary>
    public class TrenEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;

        private RectTransform _liste;
        private Text _seferYazi;
        private Text _sayacYazi;

        private TrenCevabi _cevap;
        private double _cevapAni;
        private bool _yenilemeIstendi;

        private const float PanelGenislik = 720f;
        private const float PanelYukseklik = 520f;
        private const float KartGenislik = PanelGenislik - 60f;
        private const float SiparisKartYuksekligi = 96f;

        /// <summary>Karsila butonuna basildi; parametre kart sirasi (0-2).</summary>
        public event Action<int> Karsila;

        /// <summary>Pano bayatladi (sefer degisti) — sahibi yeniden GET atmali.</summary>
        public event Action Yenile;

        public TrenEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "TrenEkrani", "Gar — Tren Panosu", null, PanelGenislik, PanelYukseklik);
            Kur();
            // Envanter degisince "elinde X/Y" satirlari eskir; pano ayni kalir,
            // yalnizca yerel cizim tazelenir — sunucuya GEREKSIZ GET atilmaz.
            _durum.DurumDegisti += _ => { if (_panel.Acik && _cevap != null) Ciz(); };
        }

        private void Kur()
        {
            RectTransform ozetIc;
            var ozet = UiKit.CerceveliKart(
                _panel.Icerik, "Ozet", Palet.ParsomenKoyu, Palet.UiCerceve, out ozetIc, 20, 4f);
            UiKit.Sabitle(ozet, 0.5f, 1f, 0f, 0f, KartGenislik, 72f);
            UiKit.GolgeEkle(ozet, 20, 14, 4f);

            _seferYazi = UiKit.Yazi(
                ozetIc, "Sefer", "", UiKit.YaziBaslik, Palet.UiKoyuYazi, TextAnchor.MiddleLeft, true);
            UiKit.Sabitle(_seferYazi.rectTransform, 0f, 0.5f, 16f, 0f, 320f, 40f);

            // Geri sayim panonun en canli parcasi: tren KACIRILABILIR (B.15),
            // bu his sayacin surekli akmasindan gelir.
            _sayacYazi = UiKit.Yazi(
                ozetIc, "Sayac", "", UiKit.YaziBaslik, Palet.UiAltinKoyu, TextAnchor.MiddleRight, true);
            UiKit.Sabitle(_sayacYazi.rectTransform, 1f, 0.5f, -16f, 0f, 300f, 40f);

            _liste = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            var disRt = _liste.parent.parent as RectTransform;
            if (disRt != null)
            {
                disRt.anchorMin = Vector2.zero;
                disRt.anchorMax = Vector2.one;
                disRt.offsetMin = new Vector2(0f, 46f);
                disRt.offsetMax = new Vector2(0f, -84f);
            }

            var not = UiKit.Yazi(
                _panel.Icerik, "Not",
                "Manifesto 3/3 dolarsa ikiniz de bonus XP alırsınız — kartlar ortak, ödül ortak.",
                UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleCenter);
            UiKit.Sabitle(not.rectTransform, 0.5f, 0f, 0f, 8f, KartGenislik, 30f);
        }

        public bool Acik => _panel.Acik;

        /// <summary>Sunucudan gelen panoyu acar/tazeler. Sayac bu andan itibaren yerel akar.</summary>
        public void Goster(TrenCevabi cevap)
        {
            _cevap = cevap;
            _cevapAni = Time.realtimeSinceStartupAsDouble;
            _yenilemeIstendi = false;
            _panel.Ac();
            Ciz();
            SureTazele();
        }

        /// <summary>
        /// Her karede cagrilir (sahibi surer). Sayac YEREL saatle akar; sefer
        /// bitince TEK bir Yenile istegi atilir — kendi kendine GET yagmuru olmaz.
        /// </summary>
        public void SureTazele()
        {
            if (!_panel.Acik || _cevap == null) return;
            double kalan = _cevap.KalanSn - (Time.realtimeSinceStartupAsDouble - _cevapAni);
            if (kalan <= 0)
            {
                _sayacYazi.text = "Tren geldi!";
                if (!_yenilemeIstendi)
                {
                    _yenilemeIstendi = true;
                    Yenile?.Invoke();
                }
                return;
            }
            _sayacYazi.text = "Kalkışa " + UiKit.SureMetni(kalan);
        }

        private void Ciz()
        {
            _seferYazi.text = "Sefer #" + _cevap.SeferNo;

            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }

            if (_cevap.Siparisler == null || _cevap.Siparisler.Count == 0)
            {
                RectTransform bosIc;
                var bos = UiKit.CerceveliKart(
                    _liste, "Bos", Palet.UiKagitCukur, Palet.UiCerceve, out bosIc, 14, 3f);
                UiKit.Boyut(bos.gameObject, 0f, 104f);
                var yazi = UiKit.Yazi(
                    bosIc, "Yazi", "Bu sefer sipariş yok — tren yalnızca selam verip geçiyor.",
                    UiKit.YaziGovde, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleCenter);
                UiKit.Doldur(yazi.rectTransform, 18f);
                return;
            }

            foreach (var s in _cevap.Siparisler) KartEkle(s);
        }

        private void KartEkle(TrenKarti s)
        {
            int elde = _durum.EnvanterAdet(s.UrunId);
            bool yeterli = elde >= s.Miktar;

            // Karsilanan kart ALTIN cerceveyle muhurlenir (tamamlanmislik dili);
            // karsilanabilir kart YESIL cerceve alir — "kosabilirsin" cagrisi.
            Color cerceve = s.Karsilandi ? Palet.KapsulAltin
                : yeterli ? Palet.UiYesil
                : Palet.UiCerceve;
            Color dolgu = s.Karsilandi ? Palet.UiKagitCukur : Palet.ParsomenKoyu;

            RectTransform ic;
            var kart = UiKit.CerceveliKart(
                _liste, "Kart_" + s.Kart, dolgu, cerceve, out ic, 14, s.Karsilandi ? 4f : 3f);
            UiKit.Boyut(kart.gameObject, 0f, SiparisKartYuksekligi);

            var ikon = UiKit.Panel(ic, "Ikon", Palet.Bugday);
            var ikonImg = ikon.gameObject.GetComponent<Image>();
            ikonImg.sprite = Sekil.Yildiz();
            ikonImg.raycastTarget = false;
            UiKit.Sabitle(ikon, 0f, 0.5f, 16f, 0f, 34f, 34f);

            var ad = UiKit.Yazi(
                ic, "Ad", s.Miktar + " × " + _denge.EsyaAdi(s.UrunId),
                UiKit.YaziBaslik, Palet.UiKoyuYazi, TextAnchor.UpperLeft, true);
            UiKit.Sabitle(ad.rectTransform, 0f, 1f, 62f, -12f, 300f, 30f);

            // "Elinde X/Y" istemci TAHMINI: renk soyler, karar sunucunundur.
            var durumYazi = UiKit.Yazi(
                ic, "Durum",
                s.Karsilandi ? KarsilayanMetni(s) : "elinde " + elde + " / " + s.Miktar,
                UiKit.YaziKucuk,
                s.Karsilandi ? Palet.UiAltinKoyu : yeterli ? Palet.UiYesilKoyu : Palet.UiKoyuYaziSolgun,
                TextAnchor.LowerLeft, true);
            UiKit.Sabitle(durumYazi.rectTransform, 0f, 0f, 62f, 12f, 300f, 24f);

            var odul = UiKit.Yazi(
                ic, "Odul", s.Altin + " altın · +" + s.Xp + " XP",
                UiKit.YaziGovde, Palet.UiAltinKoyu, TextAnchor.MiddleRight, true);
            UiKit.Sabitle(odul.rectTransform, 1f, 0.5f, -160f, 16f, 220f, 28f);

            if (s.Karsilandi)
            {
                var muhur = UiKit.Yazi(
                    ic, "Muhur", "✓ Karşılandı", UiKit.YaziGovde, Palet.UiAltinKoyu,
                    TextAnchor.MiddleRight, true);
                UiKit.Sabitle(muhur.rectTransform, 1f, 0.5f, -16f, -14f, 140f, 28f);
                return;
            }

            int kartNo = s.Kart;
            Button buton = yeterli
                ? UiKit.YesilButon(ic, "Karsila", "Karşıla", () => Karsila?.Invoke(kartNo))
                : UiKit.AhsapButon(ic, "Karsila", "Eksik", () => Karsila?.Invoke(kartNo));
            // Eksikken buton YINE tiklanabilir: sunucu reddeder ve NE eksigini
            // soyler — istemci kullaniciyi sessizce kilitlemez (iyimser istemci).
            var brt = (RectTransform)buton.transform;
            UiKit.Sabitle(brt, 1f, 0.5f, -16f, -14f, 132f, 44f);
        }

        private string KarsilayanMetni(TrenKarti s)
        {
            if (s.Karsilayan == null) return "karşılandı";
            if (s.Karsilayan == _durum.BenimId) return "sen karşıladın";
            return _durum.Oyuncular.TryGetValue(s.Karsilayan, out var o)
                ? o.Ad + " karşıladı"
                : "partnerin karşıladı";
        }
    }
}
