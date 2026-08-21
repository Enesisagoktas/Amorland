using System;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Tam ekran modal panel temeli. Basligi, kapatma butonu ve icerik alani vardir.
    ///
    /// ⚠ Ogretici modal diyalog KULLANMAZ (B.18: "zorla el tutma yok, parlayan hedef +
    /// tek satir"). Bu sinif oyuncunun KENDI actigi ekranlar icindir — canta, depo, defter.
    /// </summary>
    public class Panel
    {
        public readonly RectTransform Kok;
        public readonly RectTransform Icerik;
        public readonly Text Baslik;

        private readonly Action _kapaninca;

        public bool Acik => Kok.gameObject.activeSelf;

        public Panel(Transform kanvas, string ad, string baslik, Action kapaninca = null, float genislik = 720f,
            float yukseklik = 520f)
        {
            _kapaninca = kapaninca;

            // Perde: arkadaki oyunu karartir. Modalin "uzerinde durdugu" hissi buradan gelir.
            Kok = UiKit.Panel(kanvas, ad, new Color(0.04f, 0.035f, 0.03f, 0.62f));
            UiKit.Doldur(Kok);

            // Disariya dokunmak kapatir: mobilde geri tusundan daha guvenilir.
            var arkaButon = Kok.gameObject.AddComponent<Button>();
            arkaButon.targetGraphic = Kok.gameObject.GetComponent<Image>();
            arkaButon.transition = Selectable.Transition.None;
            arkaButon.onClick.AddListener(Kapat);

            // Parsomen govde + KALIN kahve cerceve (B.22 arayuz stil sozlesmesi).
            // Koyu cercevesiz panel zengin dunyanin uzerinde kaybolur ve arayuzu
            // "sistem menusu" gibi gosterir; okunurlugu saglayan sey kalin hattir.
            RectTransform govde;
            var cerceve = UiKit.CerceveliKart(Kok, "Cerceve", Palet.Parsomen, Palet.UiCerceve,
                out govde, 26, 6f);
            UiKit.Sabitle(cerceve, 0.5f, 0.5f, 0f, 0f, genislik, yukseklik);
            UiKit.GolgeEkle(cerceve, 26, 28, 10f);
            // Cerceveye dokunmak kapatmasin diye kendi butonu var ama hicbir sey yapmaz.
            var yutucu = cerceve.gameObject.AddComponent<Button>();
            yutucu.targetGraphic = cerceve.gameObject.GetComponent<Image>();
            yutucu.transition = Selectable.Transition.None;

            // Baslik seridi: govdenin ustunde koyu bir bant. Basligi icerikten ayirir
            // ve modale "levha" gorunumu verir.
            RectTransform seritIc;
            var serit = UiKit.CerceveliKart(govde, "BaslikSeridi", Palet.ParsomenKoyu,
                Palet.UiCerceve, out seritIc, 18, 3f);
            UiKit.Sabitle(serit, 0.5f, 1f, 0f, -8f, genislik - 40f, 52f);

            Baslik = UiKit.Yazi(
                seritIc, "Baslik", baslik, UiKit.YaziBuyuk, Palet.UiKoyuYazi, TextAnchor.MiddleLeft, true);
            UiKit.Sabitle(Baslik.rectTransform, 0f, 0.5f, 18f, 0f, genislik - 150f, 34f);

            // Kapatma: kirmizi yuvarlak — referansta da kapatma tek kirmizi ogedir.
            // Baslik seridinin dikey merkezine oturur; seritten tasmaz.
            var kapat = UiKit.KapatButonu(govde, Kapat);
            var kapatRt = kapat.GetComponent<RectTransform>();
            UiKit.Sabitle(kapatRt, 1f, 1f, -12f, -11f, 46f, 46f);

            Icerik = UiKit.Panel(govde, "Icerik", Color.clear);
            Icerik.anchorMin = Vector2.zero;
            Icerik.anchorMax = Vector2.one;
            Icerik.offsetMin = new Vector2(16f, 16f);
            Icerik.offsetMax = new Vector2(-16f, -70f);

            Kok.gameObject.SetActive(false);
        }

        public void Ac()
        {
            Kok.gameObject.SetActive(true);
            Kok.SetAsLastSibling();
        }

        public void Kapat()
        {
            Kok.gameObject.SetActive(false);
            _kapaninca?.Invoke();
        }

        public void Temizle()
        {
            for (int i = Icerik.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(Icerik.GetChild(i).gameObject);
            }
        }
    }
}
