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
        private Text _hataYazi;
        private RectTransform _isimAdim;
        private RectTransform _yasAdim;
        private InputField _yilGiris;

        public event Action<string, int> Basla;
        public event Action<int> YasVerildi;

        public GirisEkrani(Transform kanvas, DengeVerisi denge)
        {
            _denge = denge;
            _kok = UiKit.Panel(kanvas, "GirisEkrani", new Color(0.08f, 0.09f, 0.10f, 1f));
            UiKit.Doldur(_kok);

            var baslik = UiKit.Yazi(_kok, "Baslik", "AMORLAND", 52, Palet.UiVurgu, TextAnchor.UpperCenter);
            UiKit.Sabitle(baslik.rectTransform, 0.5f, 1f, 0f, -60f, 600f, 60f);

            var altBaslik = UiKit.Yazi(
                _kok, "AltBaslik", "Bu vadide hicbir is tek basina gorulmez.",
                18, Palet.UiYaziSolgun, TextAnchor.UpperCenter);
            UiKit.Sabitle(altBaslik.rectTransform, 0.5f, 1f, 0f, -120f, 700f, 30f);

            IsimAdimiKur();
            YasAdimiKur();

            _hataYazi = UiKit.Yazi(_kok, "Hata", "", 16, Palet.UiUyari, TextAnchor.UpperCenter);
            UiKit.Sabitle(_hataYazi.rectTransform, 0.5f, 0f, 0f, 40f, 700f, 40f);

            _yasAdim.gameObject.SetActive(false);
        }

        private void IsimAdimiKur()
        {
            _isimAdim = UiKit.Panel(_kok, "IsimAdim", Color.clear);
            UiKit.Sabitle(_isimAdim, 0.5f, 0.5f, 0f, 0f, 520f, 300f);
            UiKit.DikeyDizi(_isimAdim, 12f, 0);

            var etiket = UiKit.Yazi(_isimAdim, "Etiket", "Adin ne?", 20, Palet.UiYazi, TextAnchor.MiddleLeft);
            UiKit.Boyut(etiket.gameObject, 0f, 28f);

            var girisRt = UiKit.Panel(_isimAdim, "Giris", Palet.UiPanel);
            UiKit.Boyut(girisRt.gameObject, 0f, 52f);
            _isimGiris = girisRt.gameObject.AddComponent<InputField>();
            var girisYazi = UiKit.Yazi(girisRt, "Yazi", "", 24, Palet.UiYazi, TextAnchor.MiddleLeft);
            UiKit.Doldur(girisYazi.rectTransform, 10f);
            _isimGiris.textComponent = girisYazi;
            _isimGiris.characterLimit = 16;

            var renkEtiket = UiKit.Yazi(
                _isimAdim, "RenkEtiket", "Rengini sec", 20, Palet.UiYazi, TextAnchor.MiddleLeft);
            UiKit.Boyut(renkEtiket.gameObject, 0f, 28f);

            var renkSatir = UiKit.Panel(_isimAdim, "Renkler", Color.clear);
            UiKit.Boyut(renkSatir.gameObject, 0f, 64f);
            UiKit.YatayDizi(renkSatir, 12f, 0);

            for (int i = 0; i < _denge.RenkSayisi; i++)
            {
                int indeks = i;
                var kutuRt = UiKit.Panel(renkSatir, "Renk" + i, _denge.OyuncuRengi(i));
                UiKit.Boyut(kutuRt.gameObject, 60f, 60f);
                var buton = kutuRt.gameObject.AddComponent<Button>();
                buton.targetGraphic = kutuRt.gameObject.GetComponent<Image>();
                buton.onClick.AddListener(() => RenkSec(indeks));
                _renkKutulari.Add(kutuRt.gameObject.GetComponent<Image>());
            }
            RenkSec(0);

            var basla = UiKit.Buton(_isimAdim, "Basla", "Vadiye gir", () =>
            {
                string isim = _isimGiris.text != null ? _isimGiris.text.Trim() : "";
                if (isim.Length < 2)
                {
                    HataGoster("Ad en az 2 karakter olmali.");
                    return;
                }
                Basla?.Invoke(isim, _seciliRenk);
            });
            UiKit.Boyut(basla.gameObject, 0f, 54f);
        }

        private void YasAdimiKur()
        {
            _yasAdim = UiKit.Panel(_kok, "YasAdim", Color.clear);
            UiKit.Sabitle(_yasAdim, 0.5f, 0.5f, 0f, 0f, 520f, 240f);
            UiKit.DikeyDizi(_yasAdim, 12f, 0);

            var etiket = UiKit.Yazi(
                _yasAdim, "Etiket", "Dogum yilin?", 22, Palet.UiYazi, TextAnchor.MiddleLeft);
            UiKit.Boyut(etiket.gameObject, 0f, 30f);

            var aciklama = UiKit.Yazi(
                _yasAdim, "Aciklama",
                _denge.YasKapisiEsigi + " yasin altindaysan sohbet ve partner daveti kapali olur. "
                + "Oyun tek kisilik oynanabilir.",
                15, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(aciklama.gameObject, 0f, 46f);

            var girisRt = UiKit.Panel(_yasAdim, "Giris", Palet.UiPanel);
            UiKit.Boyut(girisRt.gameObject, 0f, 52f);
            _yilGiris = girisRt.gameObject.AddComponent<InputField>();
            var girisYazi = UiKit.Yazi(girisRt, "Yazi", "", 24, Palet.UiYazi, TextAnchor.MiddleCenter);
            UiKit.Doldur(girisYazi.rectTransform, 10f);
            _yilGiris.textComponent = girisYazi;
            _yilGiris.characterLimit = 4;
            _yilGiris.contentType = InputField.ContentType.IntegerNumber;

            var devam = UiKit.Buton(_yasAdim, "Devam", "Devam", () =>
            {
                if (!int.TryParse(_yilGiris.text, out int yil) || yil < 1900 || yil > 2100)
                {
                    HataGoster("Gecerli bir yil gir.");
                    return;
                }
                YasVerildi?.Invoke(yil);
            });
            UiKit.Boyut(devam.gameObject, 0f, 54f);
        }

        private void RenkSec(int indeks)
        {
            _seciliRenk = indeks;
            for (int i = 0; i < _renkKutulari.Count; i++)
            {
                Color renk = _denge.OyuncuRengi(i);
                _renkKutulari[i].color = i == indeks ? renk : new Color(renk.r, renk.g, renk.b, 0.35f);
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
