using System;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Kod ile UI kurma yardimcilari.
    ///
    /// Neden kod: bu asamada prefab yok, sanatci yok ve <c>.prefab</c> catismalari
    /// araclarla cozulmuyor (A.7). Kodla kurulan UI git'te duzgun birlesir ve tek
    /// kisilik ekipte en az surtunmeyi uretir. Cila asamasinda prefab'a tasinabilir.
    ///
    /// Yazi tipi isletim sisteminden dinamik yuklenir: TextMeshPro "Essential Resources"
    /// import adimi gerektiriyor ve Unity'ye yeni birinin ilk 10 dakikasini yiyor.
    /// </summary>
    public static class UiKit
    {
        private static Font _yaziTipi;

        public static Font YaziTipi
        {
            get
            {
                if (_yaziTipi != null) return _yaziTipi;
                _yaziTipi = Font.CreateDynamicFontFromOSFont(
                    new[] { "Segoe UI", "Roboto", "Noto Sans", "Arial", "Helvetica", "DejaVu Sans" }, 24);
                if (_yaziTipi == null)
                {
                    Debug.LogWarning("[Amorland] Isletim sistemi yazi tipi bulunamadi, UI yazisiz kalabilir.");
                }
                return _yaziTipi;
            }
        }

        // ------------------------------------------------------------------ temel

        public static Canvas Kanvas(string ad, int sira)
        {
            var go = new GameObject(ad);
            var kanvas = go.AddComponent<Canvas>();
            kanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            kanvas.sortingOrder = sira;

            var olcek = go.AddComponent<CanvasScaler>();
            olcek.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Yatay oyun (B.0). Referans cozunurluk 16:9 telefon.
            olcek.referenceResolution = new Vector2(1280f, 720f);
            olcek.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            olcek.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return kanvas;
        }

        public static RectTransform Panel(Transform ebeveyn, string ad, Color renk)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(ebeveyn, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = renk;
            return rt;
        }

        public static Text Yazi(
            Transform ebeveyn, string ad, string metin, int boyut, Color renk, TextAnchor hiza)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(ebeveyn, false);
            go.AddComponent<RectTransform>();
            var yazi = go.AddComponent<Text>();
            yazi.font = YaziTipi;
            yazi.text = metin;
            yazi.fontSize = boyut;
            yazi.color = renk;
            yazi.alignment = hiza;
            yazi.horizontalOverflow = HorizontalWrapMode.Wrap;
            yazi.verticalOverflow = VerticalWrapMode.Overflow;
            yazi.raycastTarget = false;
            return yazi;
        }

        public static Button Buton(Transform ebeveyn, string ad, string metin, Action tiklama)
        {
            var rt = Panel(ebeveyn, ad, Palet.UiPanel);
            var buton = rt.gameObject.AddComponent<Button>();
            buton.targetGraphic = rt.gameObject.GetComponent<Image>();

            var renkler = ColorBlock.defaultColorBlock;
            renkler.normalColor = Color.white;
            renkler.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            renkler.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            renkler.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            renkler.fadeDuration = 0.06f;
            buton.colors = renkler;

            // ⚠ UnityEvent.AddListener UnityAction ister. System.Action degiskeni
            // dogrudan gecirilemez (CS1503); lambda ile sarmalanir.
            if (tiklama != null) buton.onClick.AddListener(() => tiklama());

            var yazi = Yazi(rt, "Yazi", metin, 22, Palet.UiYazi, TextAnchor.MiddleCenter);
            Doldur(yazi.rectTransform, 8f);
            return buton;
        }

        /// <summary>Ebeveyni tamamen kaplar.</summary>
        public static void Doldur(RectTransform rt, float kenarBosluk = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(kenarBosluk, kenarBosluk);
            rt.offsetMax = new Vector2(-kenarBosluk, -kenarBosluk);
        }

        /// <summary>Koseye sabitler. Ornek: <c>Sabitle(rt, 0, 1, 16, -16, 200, 60)</c> sol ust.</summary>
        public static void Sabitle(
            RectTransform rt, float ankraX, float ankraY, float x, float y, float genislik, float yukseklik)
        {
            rt.anchorMin = new Vector2(ankraX, ankraY);
            rt.anchorMax = new Vector2(ankraX, ankraY);
            rt.pivot = new Vector2(ankraX, ankraY);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(genislik, yukseklik);
        }

        public static VerticalLayoutGroup DikeyDizi(RectTransform rt, float aralik, int kenar = 12)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = aralik;
            g.padding = new RectOffset(kenar, kenar, kenar, kenar);
            g.childForceExpandHeight = false;
            g.childForceExpandWidth = true;
            g.childControlWidth = true;
            g.childControlHeight = true;
            return g;
        }

        public static HorizontalLayoutGroup YatayDizi(RectTransform rt, float aralik, int kenar = 8)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = aralik;
            g.padding = new RectOffset(kenar, kenar, kenar, kenar);
            g.childForceExpandHeight = true;
            g.childForceExpandWidth = false;
            g.childControlWidth = true;
            g.childControlHeight = true;
            return g;
        }

        public static LayoutElement Boyut(GameObject go, float genislik, float yukseklik)
        {
            var el = go.AddComponent<LayoutElement>();
            el.preferredWidth = genislik;
            el.preferredHeight = yukseklik;
            el.minHeight = yukseklik;
            return el;
        }

        /// <summary>Kaydirilabilir liste. Envanter ve depo icin.</summary>
        public static RectTransform KaydirmaListesi(Transform ebeveyn, string ad, out ScrollRect kaydirma)
        {
            var dis = Panel(ebeveyn, ad, Color.clear);
            kaydirma = dis.gameObject.AddComponent<ScrollRect>();
            kaydirma.horizontal = false;
            kaydirma.vertical = true;
            kaydirma.movementType = ScrollRect.MovementType.Clamped;
            kaydirma.scrollSensitivity = 24f;

            var gorunum = Panel(dis, "Gorunum", Color.clear);
            Doldur(gorunum);
            gorunum.gameObject.AddComponent<RectMask2D>();
            kaydirma.viewport = gorunum;

            var icerik = Panel(gorunum, "Icerik", Color.clear);
            icerik.anchorMin = new Vector2(0f, 1f);
            icerik.anchorMax = new Vector2(1f, 1f);
            icerik.pivot = new Vector2(0.5f, 1f);
            icerik.sizeDelta = new Vector2(0f, 0f);
            DikeyDizi(icerik, 6f, 6);
            var sigdir = icerik.gameObject.AddComponent<ContentSizeFitter>();
            sigdir.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            kaydirma.content = icerik;

            return icerik;
        }

        /// <summary>Kucuk renk rozeti. Depoda "kim koydu" gostergesi (B.10).</summary>
        public static Image Rozet(Transform ebeveyn, Color renk, float boyut = 14f)
        {
            var rt = Panel(ebeveyn, "Rozet", renk);
            rt.sizeDelta = new Vector2(boyut, boyut);
            Boyut(rt.gameObject, boyut, boyut);
            return rt.gameObject.GetComponent<Image>();
        }

        /// <summary>Ilerleme cubugu. XP, depo dolulugu, alet dayanikliligi.</summary>
        public static Image Cubuk(Transform ebeveyn, string ad, Color arka, Color on, out Image dolgu)
        {
            var arkaPanel = Panel(ebeveyn, ad, arka);
            var dolguRt = Panel(arkaPanel, "Dolgu", on);
            dolguRt.anchorMin = Vector2.zero;
            dolguRt.anchorMax = new Vector2(0f, 1f);
            dolguRt.pivot = new Vector2(0f, 0.5f);
            dolguRt.offsetMin = Vector2.zero;
            dolguRt.offsetMax = Vector2.zero;
            dolgu = dolguRt.gameObject.GetComponent<Image>();
            return arkaPanel.gameObject.GetComponent<Image>();
        }

        public static void CubukAyarla(Image dolgu, float oran, float tamGenislik)
        {
            var rt = dolgu.rectTransform;
            rt.sizeDelta = new Vector2(tamGenislik * Mathf.Clamp01(oran), rt.sizeDelta.y);
        }

        /// <summary>Sureyi insan okunur yazar: "4 sa 12 dk", "38 sn".</summary>
        public static string SureMetni(double saniye)
        {
            if (saniye <= 0) return "hazir";
            int sn = Mathf.CeilToInt((float)saniye);
            if (sn < 60) return sn + " sn";
            int dk = sn / 60;
            if (dk < 60) return dk + " dk";
            int sa = dk / 60;
            int kalanDk = dk % 60;
            if (sa < 24) return kalanDk > 0 ? sa + " sa " + kalanDk + " dk" : sa + " sa";
            int gun = sa / 24;
            return gun + " gun " + (sa % 24) + " sa";
        }
    }
}
