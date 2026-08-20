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
            _kok = UiKit.Panel(kanvas, "GirisEkrani", new Color(0.085f, 0.075f, 0.065f, 1f));
            UiKit.Doldur(_kok);

            // Basligin arkasinda genis, cok soluk bir sicaklik lekesi. Duz zemin
            // ekranı ucuz gosteriyordu; bu tek leke derinlik hissini veriyor.
            var isik = UiKit.Panel(_kok, "Isik", new Color(Palet.UiVurgu.r, Palet.UiVurgu.g, Palet.UiVurgu.b, 0.07f));
            var isikImg = isik.gameObject.GetComponent<Image>();
            isikImg.sprite = Sekil.Daire();
            isikImg.raycastTarget = false;
            UiKit.Sabitle(isik, 0.5f, 1f, 0f, 120f, 900f, 900f);

            var baslik = UiKit.Yazi(
                _kok, "Baslik", "AMORLAND", UiKit.YaziDev, Palet.UiVurgu, TextAnchor.UpperCenter, true);
            UiKit.Sabitle(baslik.rectTransform, 0.5f, 1f, 0f, -52f, 700f, 66f);

            var altBaslik = UiKit.Yazi(
                _kok, "AltBaslik", "Bu vadide hicbir is tek basina gorulmez.",
                UiKit.YaziGovde, Palet.UiYaziSolgun, TextAnchor.UpperCenter);
            UiKit.Sabitle(altBaslik.rectTransform, 0.5f, 1f, 0f, -118f, 700f, 30f);

            IsimAdimiKur();
            YasAdimiKur();

            _hataYazi = UiKit.Yazi(
                _kok, "Hata", "", UiKit.YaziKucuk, Palet.UiUyari, TextAnchor.UpperCenter);
            UiKit.Sabitle(_hataYazi.rectTransform, 0.5f, 0f, 0f, 44f, 700f, 40f);

            _yasAdim.gameObject.SetActive(false);
        }

        /// <summary>Ortada duran, golgeli form karti. Iki adim da ayni kabugu kullanir.</summary>
        private RectTransform KartKur(string ad, float yukseklik)
        {
            var kart = UiKit.Kart(_kok, ad, Palet.UiPanel, 20);
            UiKit.Sabitle(kart, 0.5f, 0.5f, 0f, -20f, KartGenislik, yukseklik);
            UiKit.GolgeEkle(kart, 20, 26, 10f);
            var dizi = UiKit.DikeyDizi(kart, 14f, 28);
            dizi.childAlignment = TextAnchor.UpperCenter;
            return kart;
        }

        private void IsimAdimiKur()
        {
            _isimAdim = KartKur("IsimAdim", 372f);

            var etiket = UiKit.Yazi(
                _isimAdim, "Etiket", "Adin ne?", UiKit.YaziBaslik, Palet.UiYazi, TextAnchor.MiddleLeft, true);
            UiKit.Boyut(etiket.gameObject, 0f, 28f);

            _isimGiris = UiKit.Giris(_isimAdim, "Giris", "adini yaz", 16);
            UiKit.Boyut(_isimGiris.gameObject, 0f, 56f);

            var renkEtiket = UiKit.Yazi(
                _isimAdim, "RenkEtiket", "Rengini sec", UiKit.YaziBaslik, Palet.UiYazi,
                TextAnchor.MiddleLeft, true);
            UiKit.Boyut(renkEtiket.gameObject, 0f, 28f);

            var renkSatir = UiKit.Panel(_isimAdim, "Renkler", Color.clear);
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

            var basla = UiKit.AnaButon(_isimAdim, "Basla", "Vadiye gir", () =>
            {
                string isim = _isimGiris.text != null ? _isimGiris.text.Trim() : "";
                if (isim.Length < 2)
                {
                    HataGoster("Ad en az 2 karakter olmali.");
                    return;
                }
                Basla?.Invoke(isim, _seciliRenk);
            });
            UiKit.Boyut(basla.gameObject, 0f, 58f);
        }

        /// <summary>
        /// Renk secimi kare degil DAIRE. Kare renk kutulari "renk paleti aracı" gibi
        /// duruyordu; daire bir kimlik secimi gibi duruyor. Secili olan halkayla isaretlenir.
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

            var halkaRt = UiKit.Panel(yuva, "Halka", Color.clear);
            UiKit.Doldur(halkaRt);
            var halkaImg = halkaRt.gameObject.GetComponent<Image>();
            halkaImg.sprite = Sekil.Halka(30, 3);
            halkaImg.type = Image.Type.Sliced;
            halkaImg.raycastTarget = false;

            _renkKutulari.Add(kutuImg);
            _renkHalkalari.Add(halkaImg);
        }

        private void YasAdimiKur()
        {
            _yasAdim = KartKur("YasAdim", 330f);

            var etiket = UiKit.Yazi(
                _yasAdim, "Etiket", "Dogum yilin?", UiKit.YaziBaslik, Palet.UiYazi,
                TextAnchor.MiddleLeft, true);
            UiKit.Boyut(etiket.gameObject, 0f, 30f);

            var aciklama = UiKit.Yazi(
                _yasAdim, "Aciklama",
                _denge.YasKapisiEsigi + " yasin altindaysan sohbet ve partner daveti kapali olur. "
                + "Oyun tek kisilik oynanabilir.",
                UiKit.YaziKucuk, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(aciklama.gameObject, 0f, 52f);

            _yilGiris = UiKit.Giris(_yasAdim, "Giris", "1998", 4, TextAnchor.MiddleCenter);
            _yilGiris.contentType = InputField.ContentType.IntegerNumber;
            UiKit.Boyut(_yilGiris.gameObject, 0f, 56f);

            var devam = UiKit.AnaButon(_yasAdim, "Devam", "Devam", () =>
            {
                if (!int.TryParse(_yilGiris.text, out int yil) || yil < 1900 || yil > 2100)
                {
                    HataGoster("Gecerli bir yil gir.");
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
                    _renkHalkalari[i].color = secili ? Palet.UiYazi : Color.clear;
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
            _hataYazi.text = "";
        }

        public void Kapat() => _kok.gameObject.SetActive(false);

        public void HataGoster(string mesaj) => _hataYazi.text = mesaj;

        public bool Acik => _kok.gameObject.activeSelf;
    }
}
