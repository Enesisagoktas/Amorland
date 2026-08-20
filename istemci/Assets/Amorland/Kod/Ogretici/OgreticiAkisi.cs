using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Arayuz;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.Ogretici
{
    /// <summary>
    /// Ogretici akisi (B.18).
    ///
    /// ⚠ Ilk 15 dakikanin isi <b>Sv 5'e ulasmak degil</b>, oyuncuyu 10. dakikada
    /// PARTNER DAVETINE getirmek. Ogreticinin kendisi 4 dakikayi gecmemeli.
    ///
    /// ⚠ Yontem: <b>parlayan hedef + tek satir</b>. Modal diyalog YOK, zorla el tutma YOK.
    /// Oyuncu adimi atlarsa oyun devam eder; ipucu ustte durur ve kosul saglaninca duser.
    ///
    /// ⚠ Ilk 30 dakikada reklam YOK (B.24) — bu akista hicbir reklam noktasi yoktur.
    /// </summary>
    public class OgreticiAkisi : MonoBehaviour
    {
        private DunyaDurumu _durum;
        private DengeVerisi _denge;
        private Hud _hud;

        private RectTransform _serit;
        private Text _yazi;
        private int _adim = -1;

        private const string TamamlandiAnahtari = "amorland.ogretici.bitti";

        /// <summary>
        /// Ipucu seridinin olculeri. Genislik 760'tan 620'ye indi: 760 px'lik serit
        /// sol ust oyuncu kapsulunun (x 14..310) uzerine biniyordu.
        /// </summary>
        private const float SeritGenislik = 620f;

        private const float SeritYukseklik = 48f;

        private class Adim
        {
            public string Metin;
            public Func<DunyaDurumu, bool> Kosul;
        }

        private readonly List<Adim> _adimlar = new List<Adim>();

        public void Kur(DunyaDurumu durum, DengeVerisi denge, Transform kanvas, Hud hud)
        {
            _durum = durum;
            _denge = denge;
            _hud = hud;

            SeritKur(kanvas);
            AdimlariKur();

            if (PlayerPrefs.GetInt(TamamlandiAnahtari, 0) == 1)
            {
                _serit.gameObject.SetActive(false);
                return;
            }
            _adim = 0;
            AdimGoster();
        }

        private void SeritKur(Transform kanvas)
        {
            // ⚠ Ipucu dunyanin UZERINDE yuzer, oyuncunun actigi bir modal degildir:
            // dolayisiyla parsomen degil HUD KAPSUL dilini konusur (B.22) — koyu govde,
            // acik krem cerceve, beyaz konturlu yazi. Cercevesiz duz serit tarlanin koyu
            // bolgelerinde kayboluyordu; okunurlugu tasiyan sey govde degil, o acik hat.
            //
            // ⚠ Kapsul GORUNMEZ bir kabugun icinde durur ve gizlenen sey kabuktur:
            // GolgeEkle golgeyi KARDES olarak birakir, yani kapsulu tek basina kapatmak
            // ogretici bittikten sonra ekranda asili bir kara leke birakirdi.
            //
            // ⚠ Serit ust HUD siranin ALTINA konur: -16'da 760 px genisliginde dururken
            // ust orta altin kapsulunu (0.5, 1, -14, 150x46) tamamen ortuyordu ve
            // ogretici kanvasi (sira 50) HUD'un (sira 10) uzerine cizildigi icin sayac
            // gorunmuyordu. -70 altin kapsulun alt kenarinin (-60) altinda kalir.
            _serit = UiKit.Panel(kanvas, "OgreticiSeritKabuk", Color.clear);
            UiKit.Sabitle(_serit, 0.5f, 1f, 0f, -70f, SeritGenislik, SeritYukseklik);

            RectTransform ic;
            var kapsul = UiKit.CerceveliKart(
                _serit, "OgreticiSerit", Palet.KapsulZemin, Palet.KapsulCerceve, out ic, 20, 3f);
            UiKit.Doldur(kapsul);
            UiKit.GolgeEkle(kapsul, 20, 14, 4f);
            // Ipucu tiklamayi yutarsa altindaki karoya basilamaz.
            kapsul.gameObject.GetComponent<Image>().raycastTarget = false;

            // Her kapsul bir ikon tasir (B.22). ⚠ YER TUTUCU sekil — gercek ikon 2B
            // varlik olarak sonra gelecek.
            var ikon = UiKit.Panel(ic, "Ikon", Palet.UiAltin);
            var ikonImg = ikon.gameObject.GetComponent<Image>();
            ikonImg.sprite = Sekil.Yildiz();
            ikonImg.raycastTarget = false;
            UiKit.Sabitle(ikon, 0f, 0.5f, 12f, 0f, 24f, 24f);

            // Koyu kapsulde yazi BEYAZ ve KONTURLU: kontursuz acik yazi kapsulun yari
            // saydam govdesinden gecen parlak cim uzerinde kenarini kaybediyordu.
            _yazi = UiKit.KonturluYazi(
                ic, "Yazi", "", UiKit.YaziGovde, Color.white, TextAnchor.MiddleCenter, true, 1.3f);
            var yaziRt = _yazi.rectTransform;
            yaziRt.anchorMin = Vector2.zero;
            yaziRt.anchorMax = Vector2.one;
            // Soldaki bosluk ikonun yeri; esit kenarli Doldur yaziyi ikonun ustune bindirirdi.
            yaziRt.offsetMin = new Vector2(44f, 6f);
            yaziRt.offsetMax = new Vector2(-16f, -6f);
        }

        private void AdimlariKur()
        {
            // Tek satir, emir kipi, aciklama yok. "Dokun, ekilir."
            _adimlar.Add(new Adim
            {
                Metin = "Evdeki defteri aç: Defter butonuna dokun.",
                Kosul = d => d.IsaretVar("defter:1")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Tarlaya git: Harita > Tarla.",
                Kosul = d => d.IsaretVar("bolge:tarla")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Boş karoya dokun. Karakterin yürür, çapalar ve eker.",
                Kosul = d => d.IsaretVar("ilk:ilk_ekim")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Ahşap kova yap: Harita > Ev > Basit tezgâh (8 odun). "
                        + "Odun için tarla kenarındaki dallara dokun.",
                Kosul = d => d.AletVar("ahsap_kova")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Kuyuya dokun, kovayı doldur.",
                Kosul = d => d.Ben != null && d.Ben.KovaKalan > 0
            });
            _adimlar.Add(new Adim
            {
                Metin = "Ektiğin karoyu sula. Sulanmayan ürün büyümez.",
                Kosul = d => d.IsaretVar("ilk:ilk_sulama")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Buğday 5 dakikada büyür. Bu arada dalları topla — bekleme boş geçmez.",
                Kosul = d => d.IsaretVar("ilk:ilk_hasat")
            });
            _adimlar.Add(new Adim
            {
                Metin = "İlk hasat tamam. Depoya boşalt: Çanta > Depoya boşalt.",
                Kosul = d => d.IsaretVar("ilk:ilk_depoya_bosaltma")
            });
            // ⚠ Kilit tasi (B.18 ~10:00): ogreticinin GERCEK hedefi bu satir.
            _adimlar.Add(new Adim
            {
                Metin = "Bu vadide hiçbir iş tek başına görülmez. Sv 3'te eşini çağırabilirsin.",
                Kosul = d => d.Ben != null && d.Ben.Seviye >= 3
            });
        }

        private void Update()
        {
            if (_adim < 0 || _durum == null) return;
            if (_adim >= _adimlar.Count)
            {
                Bitir();
                return;
            }
            if (_adimlar[_adim].Kosul(_durum))
            {
                _adim++;
                if (_adim >= _adimlar.Count) Bitir();
                else AdimGoster();
            }
        }

        private void AdimGoster()
        {
            _serit.gameObject.SetActive(true);
            _yazi.text = _adimlar[_adim].Metin;
        }

        private void Bitir()
        {
            _adim = -1;
            _serit.gameObject.SetActive(false);
            PlayerPrefs.SetInt(TamamlandiAnahtari, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Testte ogreticiyi bastan baslatmak icin.</summary>
        public static void Sifirla()
        {
            PlayerPrefs.DeleteKey(TamamlandiAnahtari);
            PlayerPrefs.Save();
        }
    }
}
