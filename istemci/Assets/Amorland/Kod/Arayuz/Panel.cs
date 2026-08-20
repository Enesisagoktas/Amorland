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

            Kok = UiKit.Panel(kanvas, ad, new Color(0f, 0f, 0f, 0.55f));
            UiKit.Doldur(Kok);

            // Disariya dokunmak kapatir: mobilde geri tusundan daha guvenilir.
            var arkaButon = Kok.gameObject.AddComponent<Button>();
            arkaButon.targetGraphic = Kok.gameObject.GetComponent<Image>();
            arkaButon.onClick.AddListener(Kapat);

            var cerceve = UiKit.Panel(Kok, "Cerceve", Palet.UiPanel);
            UiKit.Sabitle(cerceve, 0.5f, 0.5f, 0f, 0f, genislik, yukseklik);
            // Cerceveye dokunmak kapatmasin diye kendi butonu var ama hicbir sey yapmaz.
            var yutucu = cerceve.gameObject.AddComponent<Button>();
            yutucu.targetGraphic = cerceve.gameObject.GetComponent<Image>();

            Baslik = UiKit.Yazi(cerceve, "Baslik", baslik, 26, Palet.UiYazi, TextAnchor.UpperLeft);
            UiKit.Sabitle(Baslik.rectTransform, 0f, 1f, 20f, -16f, genislik - 100f, 34f);

            var kapat = UiKit.Buton(cerceve, "Kapat", "X", Kapat);
            UiKit.Sabitle(kapat.GetComponent<RectTransform>(), 1f, 1f, -16f, -14f, 44f, 40f);
            kapat.GetComponent<RectTransform>().pivot = new Vector2(1f, 1f);

            Icerik = UiKit.Panel(cerceve, "Icerik", Color.clear);
            Icerik.anchorMin = Vector2.zero;
            Icerik.anchorMax = Vector2.one;
            Icerik.offsetMin = new Vector2(16f, 16f);
            Icerik.offsetMax = new Vector2(-16f, -60f);

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
