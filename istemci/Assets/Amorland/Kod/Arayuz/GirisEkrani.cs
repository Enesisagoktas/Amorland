using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Ilk acilis: isim + 4 renkten biri, sonra yas kapisi (B.18 0:00 ve 0:15).
    ///
    /// ⚠ <b>Kayit ekrani YOKTUR.</b> Misafir hesap cihaz kimligiyle acilir; surtunmesiz
    /// giris tasarimin geregidir (B.19). Bedeli telefon degisince ilerlemenin gitmesi —
    /// bu yuzden Sv 5/8/12'de baglama teklif edilir, partner davetinde ZORUNLU olur.
    ///
    /// ⚠ Yas kapisi bir kez sorulur. 13 altinda sohbet ve partner daveti kapanir ama
    /// oyun oynanabilir kalir (B.9): engelleme degil, kisitlama.
    ///
    /// ⚠ Kabuk B.22 arayuz stil sozlesmesine uyar: perde KOYU kalir (oyunun disi),
    /// form karti PARSOMEN + KALIN KAHVE CERCEVE olur ve o kartin icindeki her yazi
    /// KOYU'ya doner. Krem zeminde acik yazi hic gorunmez — bu ekranin ilk izlenim
    /// olmasi hatayi bagislanamaz yapar.
    /// </summary>
    public class GirisEkrani
    {
        private readonly RectTransform _kok;
        private readonly DengeVerisi _denge;

        private InputField _isimGiris;
        private int _seciliRenk;
        private readonly List<Image> _renkKutulari = new List<Image>();
        private readonly List<Image> _renkHalkalari = new List<Image>();
        private Text _hataYazi;
        private RectTransform _hataKutu;
        private RectTransform _isimAdim;
        private RectTransform _yasAdim;
        private InputField _yilGiris;

        private const float KartGenislik = 560f;

        public event Action<string, int> Basla;
        public event Action<int> YasVerildi;

        public GirisEkrani(Transform kanvas, DengeVerisi denge)
        {
            _denge = denge;

            // Notr siyah degil ilik koyu kahve: oyunun kendi paletiyle ayni ailede.
            // ⚠ Ham renk, palet disi degil zorunluluk: paletteki koyu tonlarin HEPSI
            // saydam (UiZemin 0.90, KapsulZemin 0.93) ve arkasini tam kapatmasi gereken
            // acilis perdesi icin kullanilamaz. YuklemeEkrani ile ayni ton.
            _kok = UiKit.Panel(kanvas, "GirisEkrani", new Color(0.085f, 0.075f, 0.065f, 1f));
            UiKit.Doldur(_kok);

            // Basligin arkasinda genis, yumusak bir sicaklik lekesi.
            // ⚠ Sekil.Daire ile YAPILMAZ: dolu dairenin keskin kenari koyu zeminde
            // dev bir disk gibi gorunuyordu. Hale merkezden kenara sonerek biter.
            var isik = UiKit.Panel(_kok, "Isik", new Color(Palet.UiAltin.r, Palet.UiAltin.g, Palet.UiAltin.b, 0.16f));
            var isikImg = isik.gameObject.GetComponent<Image>();
            isikImg.sprite = Sekil.Hale();
            isikImg.raycastTarget = false;
            UiKit.Sabitle(isik, 0.5f, 1f, 0f, 120f, 900f, 900f);

            // Perde KOYU oldugu icin buradaki yazilar HUD dilindedir: acik renk + kontur.
            // Kontur olmadan altin baslik kendi halesinin icinde eriyordu.
            var baslik = UiKit.KonturluYazi(
                _kok, "Baslik", "AMORLAND", UiKit.YaziDev, Palet.UiAltin, TextAnchor.UpperCenter, true, 3f);
            UiKit.Sabitle(baslik.rectTransform, 0.5f, 1f, 0f, -52f, 700f, 66f);

            var altBaslik = UiKit.KonturluYazi(
                _kok, "AltBaslik", "Bu vadide hiçbir iş tek başına görülmez.",
                UiKit.YaziGovde, Palet.KapsulCerceve, TextAnchor.UpperCenter, false, 1.4f);
            UiKit.Sabitle(altBaslik.rectTransform, 0.5f, 1f, 0f, -118f, 700f, 30f);

            IsimAdimiKur();
            YasAdimiKur();

            // Hata kendi KAPSULUNDE durur: ciplak kirmizi yazi perdenin uzerinde
            // "cizim hatasi" gibi duruyordu. Kapsul mesaj yokken tamamen gizlenir —
            // bos bir kirmizi serit her acilista ekranda beklemesin.
            RectTransform hataIc;
            _hataKutu = UiKit.CerceveliKart(
                _kok, "Hata", Palet.UiKirmiziKoyu, Palet.UiKirmizi, out hataIc, 18, 3f);
            UiKit.Sabitle(_hataKutu, 0.5f, 0f, 0f, 40f, KartGenislik, 46f);

            _hataYazi = UiKit.KonturluYazi(
                hataIc, "Yazi", "", UiKit.YaziKucuk, Color.white, TextAnchor.MiddleCenter, true, 1.2f);
            UiKit.Doldur(_hataYazi.rectTransform, 10f);
            _hataKutu.gameObject.SetActive(false);

            _yasAdim.gameObject.SetActive(false);
        }

        /// <summary>
        /// Ortada duran, golgeli PARSOMEN form karti. Iki adim da ayni kabugu kullanir.
        ///
        /// ⚠ Donen deger, kartin GORUNMEZ KABUGUDUR — adim gizlenirken o kapatilir.
        /// Kartin kendisi kapatilamaz: <see cref="UiKit.GolgeEkle"/> golgeyi KARDES
        /// olarak birakir, yani karti tek basina gizlemek golgesini ekranda birakirdi.
        /// Iki adim ayni merkezde durdugu icin geride kalan golge, gorunen kartin
        /// altinda kalin bir kara hale gibi duruyordu.
        ///
        /// Icerik <paramref name="ic"/> yuzeyine eklenir; dis cerceveye eklenirse
        /// cercevenin uzerine tasar.
        /// </summary>
        private RectTransform KartKur(string ad, float yukseklik, out RectTransform ic)
        {
            var kabuk = UiKit.Panel(_kok, ad + "Kabuk", Color.clear);
            UiKit.Sabitle(kabuk, 0.5f, 0.5f, 0f, -20f, KartGenislik, yukseklik);

            // Kalin kahve cerceve + krem govde: bu arayuzun imzasi (B.22). Cercevesiz
            // duz panel koyu perdenin uzerinde "sistem diyalogu" gibi duruyordu.
            var kart = UiKit.CerceveliKart(kabuk, ad, Palet.Parsomen, Palet.UiCerceve, out ic, 26, 6f);
            UiKit.Doldur(kart);
            UiKit.GolgeEkle(kart, 26, 26, 10f);
            var dizi = UiKit.DikeyDizi(ic, 14f, 24);
            dizi.childAlignment = TextAnchor.UpperCenter;
            return kabuk;
        }

        /// <summary>
        /// Giris kutusunu parsomene uyarlar: cukur krem yuzey, koyu yazi, kahve hat.
        ///
        /// ⚠ <see cref="UiKit.Giris"/> koyu panel icin kuruludur (koyu yuzey + acik yazi).
        /// Krem kartin uzerinde o kutu kara bir leke gibi durur, yazisi da zeminle ayni
        /// aileden degildir. Renkler UiKit imzasina dokunmadan, kurulumdan SONRA cevrilir.
        /// </summary>
        private static void GirisiParsomeneUyarla(InputField giris)
        {
            giris.GetComponent<Image>().color = Palet.UiKagitCukur;
            giris.textComponent.color = Palet.UiKoyuYazi;

            var ipucu = giris.placeholder as Text;
            if (ipucu != null) ipucu.color = Palet.UiKoyuYaziSolgun;

            // Imlec ve secim de koyu zemin varsayimiyla kuruluydu; krem uzerinde
            // acik altin imlec gorunmuyordu.
            giris.caretColor = Palet.UiCerceve;
            giris.selectionColor = new Color(Palet.UiAltin.r, Palet.UiAltin.g, Palet.UiAltin.b, 0.45f);

            // Hat halkadir, dolu katman degil: yazinin uzerini kapatmaz, yalnizca kenari boyar.
            UiKit.Cerceve(giris.transform, "Hat", Palet.UiCerceve, UiKit.YaricapKucuk, 3);
        }

        private void IsimAdimiKur()
        {
            RectTransform ic;
            _isimAdim = KartKur("IsimAdim", 372f, out ic);

            // ⚠ Krem parsomen uzerinde yazi KOYU. Eski acik ton burada gorunmez kalirdi.
            var etiket = UiKit.Yazi(
                ic, "Etiket", "Adın ne?", UiKit.YaziBaslik, Palet.UiKoyuYazi, TextAnchor.MiddleLeft, true);
            UiKit.Boyut(etiket.gameObject, 0f, 28f);

            _isimGiris = UiKit.Giris(ic, "Giris", "adını yaz", 16);
            UiKit.Boyut(_isimGiris.gameObject, 0f, 56f);
            GirisiParsomeneUyarla(_isimGiris);

            var renkEtiket = UiKit.Yazi(
                ic, "RenkEtiket", "Rengini seç", UiKit.YaziBaslik, Palet.UiKoyuYazi,
                TextAnchor.MiddleLeft, true);
            UiKit.Boyut(renkEtiket.gameObject, 0f, 28f);

            var renkSatir = UiKit.Panel(ic, "Renkler", Color.clear);
            UiKit.Boyut(renkSatir.gameObject, 0f, 68f);
            var renkDizi = UiKit.YatayDizi(renkSatir, 16f, 0);
            renkDizi.childAlignment = TextAnchor.MiddleCenter;
            // ⚠ Kapatilmazsa yuvalar satir yuksekligine gerilir ve daireler elips olur.
            renkDizi.childForceExpandHeight = false;

            for (int i = 0; i < _denge.RenkSayisi; i++)
            {
                int indeks = i;
                RenkKutusuKur(renkSatir, indeks);
            }
            RenkSec(0);

            // Ekranin tek yesili: "Vadiye gir" bu ekrandaki tek ileri yoldur (B.22).
            var basla = UiKit.YesilButon(ic, "Basla", "Vadiye gir", () =>
            {
                string isim = _isimGiris.text != null ? _isimGiris.text.Trim() : "";
                if (isim.Length < 2)
                {
                    HataGoster("Ad en az 2 karakter olmalı.");
                    return;
                }
                Basla?.Invoke(isim, _seciliRenk);
            });
            UiKit.Boyut(basla.gameObject, 0f, 58f);
        }

        /// <summary>
        /// Renk secimi kare degil DAIRE. Kare renk kutulari "renk paleti araci" gibi
        /// duruyordu; daire bir kimlik secimi gibi duruyor. Secili olan ALTIN halka alir.
        /// </summary>
        private void RenkKutusuKur(Transform satir, int indeks)
        {
            var yuva = UiKit.Panel(satir, "Renk" + indeks, Color.clear);
            UiKit.Boyut(yuva.gameObject, 60f, 60f);

            var kutuRt = UiKit.Panel(yuva, "Daire", _denge.OyuncuRengi(indeks));
            UiKit.Doldur(kutuRt, 5f);
            var kutuImg = kutuRt.gameObject.GetComponent<Image>();
            kutuImg.sprite = Sekil.Daire();

            var buton = kutuRt.gameObject.AddComponent<Button>();
            buton.targetGraphic = kutuImg;
            buton.onClick.AddListener(() => RenkSec(indeks));

            // Her dairenin ince kahve hatti var: krem parsomen uzerinde kenarsiz daireler
            // secim degil, uzerine damlamis boya lekesi gibi duruyordu.
            var hatRt = UiKit.Panel(yuva, "Hat", Palet.UiCerceve);
            UiKit.Doldur(hatRt, 5f);
            var hatImg = hatRt.gameObject.GetComponent<Image>();
            hatImg.sprite = Sekil.Halka(25, 2);
            hatImg.type = Image.Type.Sliced;
            hatImg.raycastTarget = false;

            // Secim halkasi ince hattan KALIN: secim rengi degil kalinligiyla da okunur
            // (B.22 — secili oge dolu katmanla degil, kalinlasan altin cerceveyle isaretlenir).
            var halkaRt = UiKit.Panel(yuva, "Halka", Color.clear);
            UiKit.Doldur(halkaRt);
            var halkaImg = halkaRt.gameObject.GetComponent<Image>();
            halkaImg.sprite = Sekil.Halka(30, 5);
            halkaImg.type = Image.Type.Sliced;
            halkaImg.raycastTarget = false;

            _renkKutulari.Add(kutuImg);
            _renkHalkalari.Add(halkaImg);
        }

        private void YasAdimiKur()
        {
            RectTransform ic;
            _yasAdim = KartKur("YasAdim", 330f, out ic);

            var etiket = UiKit.Yazi(
                ic, "Etiket", "Doğum yılın?", UiKit.YaziBaslik, Palet.UiKoyuYazi,
                TextAnchor.MiddleLeft, true);
            UiKit.Boyut(etiket.gameObject, 0f, 30f);

            // Aciklama ikincil bilgidir; krem uzerinde solgun KOYU ton bunu soyler.
            var aciklama = UiKit.Yazi(
                ic, "Aciklama",
                _denge.YasKapisiEsigi + " yaşın altındaysan sohbet ve partner daveti kapalı olur. "
                + "Oyun tek kişilik oynanabilir.",
                UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(aciklama.gameObject, 0f, 52f);

            _yilGiris = UiKit.Giris(ic, "Giris", "1998", 4, TextAnchor.MiddleCenter);
            _yilGiris.contentType = InputField.ContentType.IntegerNumber;
            UiKit.Boyut(_yilGiris.gameObject, 0f, 56f);
            GirisiParsomeneUyarla(_yilGiris);

            // Iki adim ayni anda gorunmez; bu yesil de ekrandaki tek yesildir.
            var devam = UiKit.YesilButon(ic, "Devam", "Devam", () =>
            {
                if (!int.TryParse(_yilGiris.text, out int yil) || yil < 1900 || yil > 2100)
                {
                    HataGoster("Geçerli bir yıl gir.");
                    return;
                }
                YasVerildi?.Invoke(yil);
            });
            UiKit.Boyut(devam.gameObject, 0f, 58f);
        }

        private void RenkSec(int indeks)
        {
            _seciliRenk = indeks;
            for (int i = 0; i < _renkKutulari.Count; i++)
            {
                Color renk = _denge.OyuncuRengi(i);
                bool secili = i == indeks;
                // Secilmeyenler tamamen solmaz — %70 kalir. Cok solunca "kapali" gibi duruyordu.
                _renkKutulari[i].color = secili ? renk : new Color(renk.r, renk.g, renk.b, 0.7f);
                if (i < _renkHalkalari.Count)
                {
                    // Secim ALTIN halkayla isaretlenir (B.22). Krem parsomen uzerinde
                    // eski acik halka zeminle ayni tondaydi ve secim hic okunmuyordu.
                    _renkHalkalari[i].color = secili ? Palet.KapsulAltin : Color.clear;
                }
            }
        }

        public void Ac()
        {
            _kok.gameObject.SetActive(true);
            _isimAdim.gameObject.SetActive(true);
            _yasAdim.gameObject.SetActive(false);
        }

        public void YasAdimiGoster()
        {
            _isimAdim.gameObject.SetActive(false);
            _yasAdim.gameObject.SetActive(true);
            HataGoster("");
        }

        public void Kapat() => _kok.gameObject.SetActive(false);

        public void HataGoster(string mesaj)
        {
            _hataYazi.text = mesaj;
            // Kapsul yalnizca soyleyecek sey varken gorunur; bos kirmizi serit
            // hata yokken de ekranda duruyormus gibi olmasin.
            _hataKutu.gameObject.SetActive(!string.IsNullOrEmpty(mesaj));
        }

        public bool Acik => _kok.gameObject.activeSelf;
    }
}
