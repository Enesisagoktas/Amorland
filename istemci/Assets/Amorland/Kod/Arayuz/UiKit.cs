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
    ///
    /// ⚠ Butun yuzeyler <see cref="Sekil"/> uzerinden YUVARLAK KOSELI sprite kullanir.
    /// Sprite'siz <c>Image</c> keskin dikdortgen cizer; arayuzun ucuz gorunmesinin
    /// sebebi tek basina buydu. Yeni yuzey eklerken <see cref="Kart"/> kullan,
    /// <see cref="Panel"/> yalnizca gorunmez kapsayicilar icindir.
    /// </summary>
    public static class UiKit
    {
        private static Font _yaziTipi;

        // ---------------------------------------------------------------- olcek
        //
        // Tek bir yazi olcegi. Ara boy uydurmak yerine buradan sec: tutarli hiyerarsi
        // "tasarlanmis" hissinin yarisidir.

        public const int YaziDev = 54;
        public const int YaziBuyuk = 30;
        public const int YaziBaslik = 23;
        public const int YaziGovde = 18;
        public const int YaziKucuk = 15;
        public const int YaziMini = 13;

        /// <summary>Standart kose yaricapi. Panel/kart.</summary>
        public const int Yaricap = 16;

        /// <summary>Kucuk ogeler: buton, slot, giris kutusu.</summary>
        public const int YaricapKucuk = 12;

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

        /// <summary>
        /// Ciplak dikdortgen yuzey. Gorunmez kapsayicilar ve tam ekran perdeler icin.
        /// Gorunur bir yuzey kuruyorsan <see cref="Kart"/> kullan.
        /// </summary>
        public static RectTransform Panel(Transform ebeveyn, string ad, Color renk)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(ebeveyn, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = renk;
            // Gorunmez kapsayici tiklamalari yutmasin.
            if (renk.a <= 0.001f) img.raycastTarget = false;
            return rt;
        }

        /// <summary>Yuvarlak koseli yuzey. Panel, slot, kutu — arayuzdeki her gorunur zemin.</summary>
        public static RectTransform Kart(Transform ebeveyn, string ad, Color renk, int yaricap = Yaricap)
        {
            var rt = Panel(ebeveyn, ad, renk);
            var img = rt.gameObject.GetComponent<Image>();
            img.sprite = Sekil.Yuvarlak(yaricap);
            img.type = Image.Type.Sliced;
            return rt;
        }

        /// <summary>
        /// Hedefin arkasina yumusak golge koyar. Kardes olarak eklenir — cocuk olsaydi
        /// hedefin ustune cizilirdi.
        ///
        /// ⚠ Hedefin rect'i AYARLANDIKTAN sonra cagir: golge o anki olculeri kopyalar.
        /// </summary>
        public static Image GolgeEkle(
            RectTransform hedef, int yaricap = Yaricap, int bulanik = 16, float dusus = 5f)
        {
            var go = new GameObject("Golge");
            go.transform.SetParent(hedef.parent, false);
            go.transform.SetSiblingIndex(hedef.GetSiblingIndex());

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = hedef.anchorMin;
            rt.anchorMax = hedef.anchorMax;
            rt.pivot = hedef.pivot;
            rt.anchoredPosition = hedef.anchoredPosition + new Vector2(0f, -dusus);
            rt.sizeDelta = hedef.sizeDelta + new Vector2(bulanik * 2f, bulanik * 2f);

            var img = go.AddComponent<Image>();
            img.sprite = Sekil.Golge(yaricap, bulanik);
            img.type = Image.Type.Sliced;
            img.color = Palet.Golge;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Yuvarlak koseli ince cerceve. Secili slotu isaretler.</summary>
        public static Image Cerceve(Transform ebeveyn, string ad, Color renk, int yaricap, int kalinlik = 3)
        {
            var rt = Panel(ebeveyn, ad, renk);
            Doldur(rt);
            var img = rt.gameObject.GetComponent<Image>();
            img.sprite = Sekil.Halka(yaricap, kalinlik);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Ince ayirac cizgi. Liste ogeleri arasinda.</summary>
        public static RectTransform Ayirac(Transform ebeveyn, float yukseklik = 1f)
        {
            var rt = Panel(ebeveyn, "Ayirac", Palet.UiCizgi);
            rt.gameObject.GetComponent<Image>().raycastTarget = false;
            Boyut(rt.gameObject, 0f, yukseklik);
            return rt;
        }

        public static Text Yazi(
            Transform ebeveyn, string ad, string metin, int boyut, Color renk, TextAnchor hiza)
        {
            return Yazi(ebeveyn, ad, metin, boyut, renk, hiza, false);
        }

        public static Text Yazi(
            Transform ebeveyn, string ad, string metin, int boyut, Color renk, TextAnchor hiza, bool kalin)
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
            yazi.fontStyle = kalin ? FontStyle.Bold : FontStyle.Normal;
            // Varsayilan 1.0 satir araligi sik duruyor; 1.2 uzun aciklamalari nefes aldirir.
            yazi.lineSpacing = 1.15f;
            yazi.horizontalOverflow = HorizontalWrapMode.Wrap;
            yazi.verticalOverflow = VerticalWrapMode.Overflow;
            yazi.raycastTarget = false;
            return yazi;
        }

        /// <summary>Basliklar icin: kalin, acik renk, iri.</summary>
        public static Text Baslik(Transform ebeveyn, string ad, string metin, int boyut = YaziBaslik)
        {
            return Yazi(ebeveyn, ad, metin, boyut, Palet.UiYazi, TextAnchor.UpperLeft, true);
        }

        // ------------------------------------------------------------------ butonlar

        /// <summary>Ikincil buton: koyu yuzey, acik yazi. Varsayilan.</summary>
        public static Button Buton(Transform ebeveyn, string ad, string metin, Action tiklama)
        {
            return ButonKur(ebeveyn, ad, metin, tiklama, Palet.UiYuzey, Palet.UiYazi, YaricapKucuk, false);
        }

        /// <summary>Ana eylem butonu: vurgu rengi zemin, koyu yazi. Ekranda tek olmali.</summary>
        public static Button AnaButon(Transform ebeveyn, string ad, string metin, Action tiklama)
        {
            return ButonKur(
                ebeveyn, ad, metin, tiklama, Palet.UiVurgu, Palet.UiVurguUstuYazi, YaricapKucuk, true);
        }

        /// <summary>Yikici/uyarici eylem.</summary>
        public static Button UyariButon(Transform ebeveyn, string ad, string metin, Action tiklama)
        {
            return ButonKur(ebeveyn, ad, metin, tiklama, Palet.UiUyari, Palet.UiYazi, YaricapKucuk, true);
        }

        /// <summary>Zeminsiz buton: yalnizca yazi. Iptal, "sonra", kapatma gibi ikincil cikislar.</summary>
        public static Button DuzButon(Transform ebeveyn, string ad, string metin, Action tiklama)
        {
            var buton = ButonKur(
                ebeveyn, ad, metin, tiklama, new Color(1f, 1f, 1f, 0.06f), Palet.UiYaziSolgun,
                YaricapKucuk, false);
            return buton;
        }

        /// <summary>Yuvarlak ikon butonu. Kapatma (X) gibi tek karakterli eylemler icin.</summary>
        public static Button DaireButon(Transform ebeveyn, string ad, string metin, Action tiklama)
        {
            var rt = Panel(ebeveyn, ad, Palet.UiYuzey);
            var img = rt.gameObject.GetComponent<Image>();
            img.sprite = Sekil.Daire();
            img.type = Image.Type.Simple;

            var buton = rt.gameObject.AddComponent<Button>();
            buton.targetGraphic = img;
            buton.colors = ButonRenkleri();
            if (tiklama != null) buton.onClick.AddListener(() => tiklama());

            var yazi = Yazi(rt, "Yazi", metin, YaziGovde, Palet.UiYaziSolgun, TextAnchor.MiddleCenter, true);
            Doldur(yazi.rectTransform);
            return buton;
        }

        private static Button ButonKur(
            Transform ebeveyn, string ad, string metin, Action tiklama,
            Color zemin, Color yaziRengi, int yaricap, bool kalin)
        {
            var rt = Kart(ebeveyn, ad, zemin, yaricap);
            var buton = rt.gameObject.AddComponent<Button>();
            buton.targetGraphic = rt.gameObject.GetComponent<Image>();
            buton.colors = ButonRenkleri();

            // ⚠ UnityEvent.AddListener UnityAction ister. System.Action degiskeni
            // dogrudan gecirilemez (CS1503); lambda ile sarmalanir.
            if (tiklama != null) buton.onClick.AddListener(() => tiklama());

            var yazi = Yazi(rt, "Yazi", metin, YaziGovde, yaziRengi, TextAnchor.MiddleCenter, kalin);
            Doldur(yazi.rectTransform, 8f);
            return buton;
        }

        private static ColorBlock ButonRenkleri()
        {
            var renkler = ColorBlock.defaultColorBlock;
            renkler.normalColor = Color.white;
            renkler.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            renkler.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            renkler.selectedColor = Color.white;
            renkler.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.5f);
            renkler.fadeDuration = 0.08f;
            return renkler;
        }

        // ------------------------------------------------------------------ giris

        /// <summary>Yuvarlak koseli metin giris kutusu. Ipucu yazisi bos alanda soluk gorunur.</summary>
        public static InputField Giris(
            Transform ebeveyn, string ad, string ipucu, int karakterSiniri,
            TextAnchor hiza = TextAnchor.MiddleLeft)
        {
            var rt = Kart(ebeveyn, ad, Palet.UiYuzeyKoyu, YaricapKucuk);
            var giris = rt.gameObject.AddComponent<InputField>();
            giris.targetGraphic = rt.gameObject.GetComponent<Image>();

            var yazi = Yazi(rt, "Yazi", "", YaziBaslik, Palet.UiYazi, hiza);
            Doldur(yazi.rectTransform, 14f);
            yazi.supportRichText = false;
            giris.textComponent = yazi;

            var ipucuYazi = Yazi(rt, "Ipucu", ipucu, YaziBaslik, Palet.UiYaziSolgun, hiza);
            Doldur(ipucuYazi.rectTransform, 14f);
            giris.placeholder = ipucuYazi;

            giris.characterLimit = karakterSiniri;
            giris.caretColor = Palet.UiVurgu;
            giris.customCaretColor = true;
            giris.selectionColor = new Color(Palet.UiVurgu.r, Palet.UiVurgu.g, Palet.UiVurgu.b, 0.35f);
            return giris;
        }

        // ------------------------------------------------------------------ yerlesim

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
            DikeyDizi(icerik, 8f, 4);
            var sigdir = icerik.gameObject.AddComponent<ContentSizeFitter>();
            sigdir.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            kaydirma.content = icerik;

            return icerik;
        }

        /// <summary>Kucuk renk rozeti. Depoda "kim koydu" gostergesi (B.10). Daire — kare degil.</summary>
        public static Image Rozet(Transform ebeveyn, Color renk, float boyut = 14f)
        {
            var rt = Panel(ebeveyn, "Rozet", renk);
            var img = rt.gameObject.GetComponent<Image>();
            img.sprite = Sekil.Daire();
            img.raycastTarget = false;
            rt.sizeDelta = new Vector2(boyut, boyut);
            Boyut(rt.gameObject, boyut, boyut);
            return img;
        }

        /// <summary>Ilerleme cubugu. XP, depo dolulugu, alet dayanikliligi. Uclari yuvarlak.</summary>
        public static Image Cubuk(Transform ebeveyn, string ad, Color arka, Color on, out Image dolgu)
        {
            var arkaPanel = Kart(ebeveyn, ad, arka, 5);
            arkaPanel.gameObject.GetComponent<Image>().raycastTarget = false;

            var dolguRt = Kart(arkaPanel, "Dolgu", on, 5);
            dolguRt.anchorMin = Vector2.zero;
            dolguRt.anchorMax = new Vector2(0f, 1f);
            dolguRt.pivot = new Vector2(0f, 0.5f);
            dolguRt.offsetMin = Vector2.zero;
            dolguRt.offsetMax = Vector2.zero;
            dolgu = dolguRt.gameObject.GetComponent<Image>();
            dolgu.raycastTarget = false;
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
