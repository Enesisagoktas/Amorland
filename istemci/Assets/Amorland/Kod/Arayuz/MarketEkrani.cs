using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Ag;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Market (B.15) — bilerek SIKICI: satis tabanFiyat × sabit oran, alis
    /// tohumFiyat, pazarlik yok, dalgalanma yok. Heyecan trende; market yalnizca
    /// acil nakit muslugu ve tohum tezgahi. Bu ekran o sikiciligi SAKLAMAZ,
    /// "fiyatlar sabittir" diye acikca soyler — oyuncu treni beklemeyi ogrenir.
    ///
    /// ⚠ Gosterilen fiyatlar istemci TAHMINIDIR (denge JSON'undan, mimari kural
    /// 7-8): gercek hesap sunucuda. Tahmin ile sunucu ayni denge dosyasini
    /// okudugu icin sapma olmaz; olursa otorite sunucudur.
    ///
    /// Kabuk B.22: krem parsomen, koyu yazi, kalin kahve cerceve.
    /// </summary>
    public class MarketEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;

        private RectTransform _liste;
        private Text _bakiye;

        private const float PanelGenislik = 720f;
        private const float PanelYukseklik = 520f;
        private const float KartGenislik = PanelGenislik - 60f;
        private const float SatirYuksekligi = 58f;

        /// <summary>(esyaId, adet) — markete satis niyeti.</summary>
        public event Action<string, int> SatIstegi;

        /// <summary>(esyaId, adet) — marketten tohum alma niyeti.</summary>
        public event Action<string, int> AlIstegi;

        public MarketEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "MarketEkrani", "Market", null, PanelGenislik, PanelYukseklik);
            Kur();
            _durum.DurumDegisti += _ => { if (_panel.Acik) Tazele(); };
        }

        private void Kur()
        {
            RectTransform ozetIc;
            var ozet = UiKit.CerceveliKart(
                _panel.Icerik, "Ozet", Palet.ParsomenKoyu, Palet.UiCerceve, out ozetIc, 20, 4f);
            UiKit.Sabitle(ozet, 0.5f, 1f, 0f, 0f, KartGenislik, 64f);
            UiKit.GolgeEkle(ozet, 20, 14, 4f);

            var ikon = UiKit.Panel(ozetIc, "Ikon", Palet.KapsulAltin);
            var ikonImg = ikon.gameObject.GetComponent<Image>();
            ikonImg.sprite = Sekil.Daire();
            ikonImg.raycastTarget = false;
            UiKit.Sabitle(ikon, 0f, 0.5f, 16f, 0f, 28f, 28f);

            _bakiye = UiKit.Yazi(
                ozetIc, "Bakiye", "", UiKit.YaziBaslik, Palet.UiKoyuYazi, TextAnchor.MiddleLeft, true);
            UiKit.Sabitle(_bakiye.rectTransform, 0f, 0.5f, 56f, 0f, 260f, 36f);

            var not = UiKit.Yazi(
                ozetIc, "Not", "Fiyatlar sabittir — iyi para trende.",
                UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleRight, true);
            UiKit.Sabitle(not.rectTransform, 1f, 0.5f, -16f, 0f, 300f, 30f);

            _liste = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            var disRt = _liste.parent.parent as RectTransform;
            if (disRt != null)
            {
                disRt.anchorMin = Vector2.zero;
                disRt.anchorMax = Vector2.one;
                disRt.offsetMin = new Vector2(0f, 16f);
                disRt.offsetMax = new Vector2(0f, -76f);
            }
        }

        public bool Acik => _panel.Acik;

        public void Ac()
        {
            _panel.Ac();
            Tazele();
        }

        public void Tazele()
        {
            var ben = _durum.Ben;
            if (ben == null) return;

            _bakiye.text = ben.Altin + " altın";

            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }

            // ---- SATIS: cantadaki urunler. Tohum ve islenmisler SATILMAZ (B.15
            // market yalnizca urun alir) — liste bunu hic gostermeyerek soyler.
            BolumBasligi("Sat — ürünler, anında altın");
            float oran = _denge.MarketAlisOraniOrt;
            var satilabilir = _durum.BenimEnvanterim
                .Where(e => _denge.Urun(e.EsyaId) != null)
                .ToList();
            if (satilabilir.Count == 0)
            {
                BilgiSatiri("Çantanda satılık ürün yok — önce hasat.");
            }
            foreach (var e in satilabilir)
            {
                var urun = _denge.Urun(e.EsyaId);
                int birim = Mathf.Max(1, Mathf.FloorToInt(urun.TabanFiyat * oran));
                string esyaId = e.EsyaId;
                int adet = e.Adet;
                SatirEkle(
                    _denge.EsyaAdi(esyaId) + " × " + adet,
                    "adedi ~" + birim + " altın",
                    Palet.Bugday, Sekil.Yildiz(),
                    ("1 Sat", () => SatIstegi?.Invoke(esyaId, 1)),
                    ("Tümünü Sat", () => SatIstegi?.Invoke(esyaId, adet)));
            }

            // ---- ALIS: acilmis urunlerin tohumlari. Kapali urunler LISTELENMEZ:
            // "alamayacagin sey vitrinde" alay olurdu (ayni ilke trende de var).
            BolumBasligi("Al — tohumlar");
            long bakiye = ben.Altin;
            foreach (var u in _denge.Urunler
                         .Where(u => u.AcilisSeviye <= ben.Seviye)
                         .OrderBy(u => u.AcilisSeviye))
            {
                if (u.TohumFiyat <= 0) continue;
                string tohumId = u.TohumEsyaId;
                int fiyat = u.TohumFiyat;
                SatirEkle(
                    _denge.EsyaAdi(u.Id) + " tohumu",
                    "tanesi " + fiyat + " altın",
                    Palet.Yaprak, Sekil.Damla(),
                    ("1 Al", () => AlIstegi?.Invoke(tohumId, 1), bakiye >= fiyat),
                    ("5 Al", () => AlIstegi?.Invoke(tohumId, 5), bakiye >= fiyat * 5L));
            }
        }

        private void BolumBasligi(string metin)
        {
            var kutu = UiKit.Panel(_liste, "Baslik_" + metin.GetHashCode(), Color.clear);
            UiKit.Boyut(kutu.gameObject, 0f, 36f);
            var yazi = UiKit.Yazi(
                kutu, "Yazi", metin, UiKit.YaziGovde, Palet.UiKoyuYazi, TextAnchor.LowerLeft, true);
            UiKit.Doldur(yazi.rectTransform, 6f);
        }

        private void BilgiSatiri(string metin)
        {
            RectTransform ic;
            var kart = UiKit.CerceveliKart(
                _liste, "Bilgi", Palet.UiKagitCukur, Palet.UiCerceve, out ic, 14, 3f);
            UiKit.Boyut(kart.gameObject, 0f, 64f);
            var yazi = UiKit.Yazi(
                ic, "Yazi", metin, UiKit.YaziGovde, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleCenter);
            UiKit.Doldur(yazi.rectTransform, 12f);
        }

        private void SatirEkle(
            string ad, string altYazi, Color ikonRenk, Sprite ikonSekil,
            params (string etiket, Action tiklama, bool aktif)[] butonlar)
        {
            RectTransform ic;
            var satir = UiKit.CerceveliKart(
                _liste, "Satir_" + ad, Palet.ParsomenKoyu, Palet.UiCerceve, out ic, 14, 3f);
            UiKit.Boyut(satir.gameObject, 0f, SatirYuksekligi);

            var ikon = UiKit.Panel(ic, "Ikon", ikonRenk);
            var ikonImg = ikon.gameObject.GetComponent<Image>();
            ikonImg.sprite = ikonSekil;
            ikonImg.raycastTarget = false;
            UiKit.Sabitle(ikon, 0f, 0.5f, 14f, 0f, 28f, 28f);

            var adYazi = UiKit.Yazi(
                ic, "Ad", ad, UiKit.YaziGovde, Palet.UiKoyuYazi, TextAnchor.UpperLeft, true);
            UiKit.Sabitle(adYazi.rectTransform, 0f, 1f, 54f, -8f, 280f, 26f);

            var alt = UiKit.Yazi(
                ic, "Alt", altYazi, UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun, TextAnchor.LowerLeft, true);
            UiKit.Sabitle(alt.rectTransform, 0f, 0f, 54f, 8f, 280f, 20f);

            float sagKenar = -14f;
            for (int i = butonlar.Length - 1; i >= 0; i--)
            {
                var (etiket, tiklama, aktif) = butonlar[i];
                var buton = i == butonlar.Length - 1
                    ? UiKit.YesilButon(ic, "B" + i, etiket, () => tiklama())
                    : UiKit.AhsapButon(ic, "B" + i, etiket, () => tiklama());
                buton.interactable = aktif;
                var brt = (RectTransform)buton.transform;
                float genislik = etiket.Length > 6 ? 132f : 78f;
                UiKit.Sabitle(brt, 1f, 0.5f, sagKenar, 0f, genislik, 42f);
                sagKenar -= genislik + 10f;
            }
        }

        private void SatirEkle(
            string ad, string altYazi, Color ikonRenk, Sprite ikonSekil,
            (string etiket, Action tiklama) buton1, (string etiket, Action tiklama) buton2)
        {
            SatirEkle(ad, altYazi, ikonRenk, ikonSekil,
                (buton1.etiket, buton1.tiklama, true), (buton2.etiket, buton2.tiklama, true));
        }
    }
}
