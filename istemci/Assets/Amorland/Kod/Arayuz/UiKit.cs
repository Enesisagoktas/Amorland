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
    /// sebebi tek basina buydu.
    ///
    /// ⚠ Gorunur yuzey icin <see cref="Kart"/> ARTIK YETMEZ (B.22): cercevesizdir ve
    /// zengin dunyanin uzerinde kaybolur. Yeni yuzey <see cref="CerceveliKart"/> ile
    /// kurulur; <see cref="Kart"/> yalnizca cubuk dolgusu gibi cerceve ISTEMEYEN
    /// parcalar icin, <see cref="Panel"/> ise yalnizca gorunmez kapsayicilar ve
    /// sprite tasiyan ikon/daire katmanlari icindir.
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

        /// <summary>
        /// Sahnede EventSystem yoksa kurar. UI tiklamasinin ON KOSULU: vadi
        /// sahnesi uc kanvas kurup bunu unutunca butun butonlar olu kalmisti
        /// (oyuncu bulgusu — secim ekraninda hicbir sey tiklanamiyordu; klavye
        /// calisiyordu cunku Girdi EventSystem'den gecmez). Kanvas kuran herkes
        /// bunu bedavaya alir; kok nesneye baglanmaz ki sahne yikimlarinda yasasin.
        /// </summary>
        public static void EventSystemGarantile()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            // ⚠ InputSystemUIInputModule UnityEngine.EventSystems'te DEGIL,
            // UnityEngine.InputSystem.UI ad alanindadir.
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        public static Canvas Kanvas(string ad, int sira)
        {
            EventSystemGarantile();
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

        /// <summary>
        /// Basliklar icin: kalin, acik renk, iri.
        ///
        /// ⚠ KOYU zemin varsayar (<see cref="Palet.UiYazi"/> acik kremdir). Krem
        /// parsomen uzerinde gorunmez kalir — orada <see cref="Yazi"/> ile
        /// <see cref="Palet.UiKoyuYazi"/> kullan.
        /// </summary>
        public static Text Baslik(Transform ebeveyn, string ad, string metin, int boyut = YaziBaslik)
        {
            return Yazi(ebeveyn, ad, metin, boyut, Palet.UiYazi, TextAnchor.UpperLeft, true);
        }

        // ------------------------------------------------------------------ butonlar
        //
        // ⚠ ESKI KOYU PANEL DILI. Bu bes buton (Buton/AnaButon/UyariButon/DuzButon/
        // DaireButon) koyu yuzey + acik yazi varsayar ve modal govdesi KREM PARSOMEN'e
        // donunce (B.22) hepsi krem uzerinde krem kalir. Ekranlarin hicbiri artik
        // bunlari kullanmiyor; yeni buton icin asagidaki YesilButon / AhsapButon /
        // KirmiziButon uclusu kullanilir. Imzalar kaldirilmadi ki dis kod kirilmasin.

        /// <summary>Ikincil buton: koyu yuzey, acik yazi. ⚠ Yalnizca KOYU zeminde.</summary>
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
            // ⚠ Zemin TAMAMEN saydam. Onceki %6 beyaz dolgu, koyu kart uzerinde
            // ikincil butondan daha parlak cikiyor ve hiyerarsiyi tersine ceviriyordu:
            // "sonra" baglantisi ana eylemden once goze carpiyordu.
            return ButonKur(
                ebeveyn, ad, metin, tiklama, new Color(1f, 1f, 1f, 0f), Palet.UiYaziSolgun,
                YaricapKucuk, false);
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
            var img = rt.gameObject.GetComponent<Image>();

            // ⚠ Panel, saydam zeminli ogelerin raycastTarget'ini kapatir (gorunmez
            // kapsayici tiklamayi yutmasin diye). Buton icin bu YANLIS: zemini saydam
            // olan DuzButon tiklanamaz hale gelir. Butonda her zaman acik olmali.
            img.raycastTarget = true;

            var buton = rt.gameObject.AddComponent<Button>();
            buton.targetGraphic = img;
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

        // ------------------------------------------------------------------ parlak kabuk
        //
        // Kozy ciftlik arayuzunun imzasi KALIN CERCEVE + PAH'tir. Ince cizgili duz
        // yuzey "uygulama" gibi durur; kalin kahve cerceveli krem panel "oyuncak"
        // gibi durur. Fark tek basina bu iki seyden geliyor.

        /// <summary>
        /// Kalin cerceveli panel. Icerik <paramref name="ic"/> yuzeyine eklenir —
        /// dis cerceveye eklenirse cercevenin uzerine tasar.
        /// </summary>
        public static RectTransform CerceveliKart(
            Transform ebeveyn, string ad, Color dolgu, Color cerceve, out RectTransform ic,
            int yaricap = 24, float kalinlik = 6f)
        {
            var dis = Kart(ebeveyn, ad, cerceve, yaricap);
            ic = Kart(dis, "Ic", dolgu, Mathf.Max(2, yaricap - Mathf.RoundToInt(kalinlik)));
            Doldur(ic, kalinlik);
            ic.gameObject.GetComponent<Image>().raycastTarget = false;
            return dis;
        }

        /// <summary>
        /// Kalin, pahli buton: altta koyu serit (kalinlik hissi), ustte acik pah (isik).
        /// Yazi konturludur — doygun zemin uzerinde beyaz yazi kontursuz erir.
        /// </summary>
        public static Button KalinButon(
            Transform ebeveyn, string ad, string metin, Action tiklama,
            Color yuz, Color taban, Color yaziRengi, int boyut = YaziGovde)
        {
            var dis = Kart(ebeveyn, ad, taban, 16);
            var img = dis.gameObject.GetComponent<Image>();
            img.raycastTarget = true;

            var buton = dis.gameObject.AddComponent<Button>();
            buton.targetGraphic = img;
            buton.colors = ButonRenkleri();
            if (tiklama != null) buton.onClick.AddListener(() => tiklama());

            // Yuz, tabandan 6 px yukarida durur: alttaki koyu serit butonun kalinligidir.
            var yuzRt = Kart(dis, "Yuz", yuz, 14);
            yuzRt.anchorMin = Vector2.zero;
            yuzRt.anchorMax = Vector2.one;
            yuzRt.offsetMin = new Vector2(3f, 7f);
            yuzRt.offsetMax = new Vector2(-3f, -3f);
            yuzRt.gameObject.GetComponent<Image>().raycastTarget = false;

            // ⚠ Pah rengi ham yazilmaz: joystick topuzu da ayni isigi kullanir ve iki
            // yerde farkli alfa tutulunca pahli ogeler ayni ekranda ayri malzemeden
            // gorunuyordu. Tek kaynak Palet.UiPah.
            // ⚠ Pah ust %35'te kalir: %55'i kaplayan parlama, yaziyi acik/koyu
            // sinirina oturtuyor ve ayri bir blok gibi okunuyordu (UI denetimi).
            var pah = Kart(yuzRt, "Pah", Palet.UiPah, 10);
            pah.anchorMin = new Vector2(0f, 0.66f);
            pah.anchorMax = new Vector2(1f, 1f);
            pah.offsetMin = new Vector2(6f, 0f);
            pah.offsetMax = new Vector2(-6f, -5f);
            pah.gameObject.GetComponent<Image>().raycastTarget = false;

            var yazi = KonturluYazi(yuzRt, "Yazi", metin, boyut, yaziRengi, TextAnchor.MiddleCenter);
            Doldur(yazi.rectTransform, 8f);
            return buton;
        }

        /// <summary>Onay/ana eylem: doygun yesil.</summary>
        public static Button YesilButon(Transform ebeveyn, string ad, string metin, Action tiklama) =>
            KalinButon(ebeveyn, ad, metin, tiklama, Palet.UiYesil, Palet.UiYesilKoyu, Color.white);

        /// <summary>Ikincil eylem: ahsap.</summary>
        public static Button AhsapButon(Transform ebeveyn, string ad, string metin, Action tiklama) =>
            KalinButon(ebeveyn, ad, metin, tiklama, Palet.UiAhsapButon, Palet.UiAhsapButonKoyu, Color.white);

        /// <summary>Iptal/yikici eylem: kirmizi.</summary>
        public static Button KirmiziButon(Transform ebeveyn, string ad, string metin, Action tiklama) =>
            KalinButon(ebeveyn, ad, metin, tiklama, Palet.UiKirmizi, Palet.UiKirmiziKoyu, Color.white);

        /// <summary>
        /// Modal kapatma butonu: kirmizi daire + krem halka + ortalanmis carpi.
        ///
        /// ⚠ KalinButon ile YAPILMAZ: 46 px'lik kare butonda pah seridi carpiyi
        /// sol-alta itiyor ve parlama ayri bir blok gibi okunuyordu (UI denetimi).
        /// Kapatma icin pahsiz, tam merkezli daire gerekir.
        /// </summary>
        public static Button KapatButonu(Transform ebeveyn, Action tiklama)
        {
            var rt = Panel(ebeveyn, "Kapat", Palet.UiKirmizi);
            var img = rt.gameObject.GetComponent<Image>();
            img.sprite = Sekil.Daire();
            img.raycastTarget = true;

            var buton = rt.gameObject.AddComponent<Button>();
            buton.targetGraphic = img;
            buton.colors = ButonRenkleri();
            if (tiklama != null) buton.onClick.AddListener(() => tiklama());

            var halka = Cerceve(rt, "Halka", Palet.KapsulCerceve, 23, 3);
            halka.sprite = Sekil.Halka(23, 3);

            var yazi = KonturluYazi(rt, "Yazi", "×", YaziBuyuk, Color.white, TextAnchor.MiddleCenter);
            Doldur(yazi.rectTransform);
            // Carpi glifi taban cizgisiyle yukari kacar; minik dusey duzeltme merkezler.
            yazi.rectTransform.offsetMin = new Vector2(0f, 2f);
            return buton;
        }

        /// <summary>
        /// Konturlu yazi. Doygun renk ustunde okunurlugu tasiyan sey konturdur;
        /// golge tek basina yetmez cunku ayni tonda zeminde kaybolur.
        /// </summary>
        public static Text KonturluYazi(
            Transform ebeveyn, string ad, string metin, int boyut, Color renk, TextAnchor hiza,
            bool kalin = true, float konturKalinlik = 2f)
        {
            var yazi = Yazi(ebeveyn, ad, metin, boyut, renk, hiza, kalin);
            var kontur = yazi.gameObject.AddComponent<Outline>();
            kontur.effectColor = Palet.UiKontur;
            kontur.effectDistance = new Vector2(konturKalinlik, -konturKalinlik);
            kontur.useGraphicAlpha = false;
            return yazi;
        }

        /// <summary>Yuvarlak, cerceveli rozet — sayac ve para gostergeleri icin.</summary>
        public static RectTransform Hap(
            Transform ebeveyn, string ad, Color dolgu, Color cerceve, out RectTransform ic)
        {
            return CerceveliKart(ebeveyn, ad, dolgu, cerceve, out ic, 22, 4f);
        }

        // ------------------------------------------------------------------ giris

        /// <summary>
        /// Yuvarlak koseli metin giris kutusu. Ipucu yazisi bos alanda soluk gorunur.
        ///
        /// ⚠ KOYU panel icin kuruludur: koyu kutu, acik yazi, acik altin imlec. Krem
        /// parsomen uzerinde kutu kara bir leke, yazi ve imlec ise gorunmez olur.
        /// Parsomen kart icinde kurulduktan SONRA renkleri cevrilmeli (bkz.
        /// <c>EkranParcasi.KagitGiris</c> ve <c>GirisEkrani.GirisiParsomeneUyarla</c>).
        /// </summary>
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
            if (saniye <= 0) return "hazır";
            int sn = Mathf.CeilToInt((float)saniye);
            if (sn < 60) return sn + " sn";
            int dk = sn / 60;
            if (dk < 60) return dk + " dk";
            int sa = dk / 60;
            int kalanDk = dk % 60;
            if (sa < 24) return kalanDk > 0 ? sa + " sa " + kalanDk + " dk" : sa + " sa";
            int gun = sa / 24;
            return gun + " gün " + (sa % 24) + " sa";
        }
    }
}
