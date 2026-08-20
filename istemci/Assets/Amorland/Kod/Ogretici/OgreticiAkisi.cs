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
            _serit = UiKit.Panel(kanvas, "OgreticiSerit", Palet.UiZemin);
            UiKit.Sabitle(_serit, 0.5f, 1f, 0f, -16f, 760f, 48f);
            _serit.gameObject.GetComponent<Image>().raycastTarget = false;

            _yazi = UiKit.Yazi(_serit, "Yazi", "", 19, Palet.UiYazi, TextAnchor.MiddleCenter);
            UiKit.Doldur(_yazi.rectTransform, 10f);
        }

        private void AdimlariKur()
        {
            // Tek satir, emir kipi, aciklama yok. "Dokun, ekilir."
            _adimlar.Add(new Adim
            {
                Metin = "Evdeki defteri ac: Defter butonuna dokun.",
                Kosul = d => d.IsaretVar("defter:1")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Tarlaya git: Harita > Tarla.",
                Kosul = d => d.IsaretVar("bolge:tarla")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Bos karoya dokun. Karakterin yurur, capalar ve eker.",
                Kosul = d => d.IsaretVar("ilk:ilk_ekim")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Ahsap kova yap: Harita > Ev > Basit tezgah (8 odun). "
                        + "Odun icin tarla kenarindaki dallara dokun.",
                Kosul = d => d.AletVar("ahsap_kova")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Kuyuya dokun, kovayi doldur.",
                Kosul = d => d.Ben != null && d.Ben.KovaKalan > 0
            });
            _adimlar.Add(new Adim
            {
                Metin = "Ektigin karoyu sula. Sulanmayan urun buyumez.",
                Kosul = d => d.IsaretVar("ilk:ilk_sulama")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Bugday 5 dakikada buyur. Bu arada dallari topla — bekleme bos gecmez.",
                Kosul = d => d.IsaretVar("ilk:ilk_hasat")
            });
            _adimlar.Add(new Adim
            {
                Metin = "Ilk hasat tamam. Depoya bosalt: Canta > Depoya bosalt.",
                Kosul = d => d.IsaretVar("ilk:ilk_depoya_bosaltma")
            });
            // ⚠ Kilit tasi (B.18 ~10:00): ogreticinin GERCEK hedefi bu satir.
            _adimlar.Add(new Adim
            {
                Metin = "Bu vadide hicbir is tek basina gorulmez. Sv 3'te esini cagirabilirsin.",
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
