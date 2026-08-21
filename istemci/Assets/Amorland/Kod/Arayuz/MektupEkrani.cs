using System;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Acilis mektubu: oyuncunun NEDEN burada oldugunu soyleyen tek ekran
    /// (oyuncu istegi: "biz niye durduk yere bugday ekiyoruz?").
    ///
    /// B.1 cercevesinde: Amorland'in kurali, ayrilan son cift, imzasiz kalan
    /// manifesto, durmayan tren — ve davet. Ara sahne degildir: bir kez
    /// gosterilir, kapatilir, defter devralir (anlatim toplanabilir sayfalarda).
    /// </summary>
    public class MektupEkrani
    {
        private const string OkunduAnahtari = "amorland.mektup.okundu";

        private readonly RectTransform _kok;

        public event Action Kapandi;

        private const string Metin =
            "Bu mektup eline geçtiyse, vadinin anahtarı da geçmiştir.\n\n"
            + "Amorland'ın tek bir kuralı vardı: hiçbir iş tek başına görülmez. "
            + "Tren manifestosu iki imza ister; tarlada biri eker, biri sular.\n\n"
            + "Son çift ayrıldığında manifesto imzasız kaldı. Tren o gün bu gündür "
            + "burada durmuyor. Ben her sabah perona çıkıyorum; tarifede Amorland'ın "
            + "üstü çizili.\n\n"
            + "Evde yarım kalmış bir defter bulacaksın: birlikte yapılanlar defteri. "
            + "Doldurun onu. Tarla canlanır, depo dolar, manifesto imza bulursa — "
            + "tren geri döner.\n\n"
            + "Vadi seni bekliyordu. Belki ikinizi.";

        public static bool Okundu => PlayerPrefs.GetInt(OkunduAnahtari, 0) == 1;

        public MektupEkrani(Transform kanvas)
        {
            _kok = UiKit.Panel(kanvas, "MektupEkrani", new Color(0.04f, 0.035f, 0.03f, 0.72f));
            UiKit.Doldur(_kok);

            RectTransform ic;
            var kart = UiKit.CerceveliKart(
                _kok, "Mektup", Palet.Parsomen, Palet.UiCerceve, out ic, 26, 6f);
            UiKit.Sabitle(kart, 0.5f, 0.5f, 0f, 0f, 720f, 560f);
            UiKit.GolgeEkle(kart, 26, 28, 10f);

            var baslik = UiKit.Yazi(
                ic, "Baslik", "Amorland İstasyonu — Şef Saffet'ten", UiKit.YaziBaslik,
                Palet.UiKoyuYazi, TextAnchor.MiddleCenter, true);
            UiKit.Sabitle(baslik.rectTransform, 0.5f, 1f, 0f, -18f, 640f, 32f);

            var cizgi = UiKit.Panel(ic, "Cizgi", Palet.UiCerceve);
            UiKit.Sabitle(cizgi, 0.5f, 1f, 0f, -58f, 600f, 2f);

            var govde = UiKit.Yazi(
                ic, "Govde", Metin, UiKit.YaziGovde, Palet.UiKoyuYazi, TextAnchor.UpperLeft);
            UiKit.Sabitle(govde.rectTransform, 0.5f, 1f, 0f, -74f, 620f, 360f);

            var imza = UiKit.Yazi(
                ic, "Imza", "— S.", UiKit.YaziBaslik, Palet.UiKoyuYaziSolgun,
                TextAnchor.MiddleRight, true);
            UiKit.Sabitle(imza.rectTransform, 1f, 0f, -36f, 84f, 120f, 30f);

            var devam = UiKit.YesilButon(ic, "Devam", "Vadiye adım at", Kapat);
            UiKit.Sabitle(devam.GetComponent<RectTransform>(), 0.5f, 0f, 0f, 18f, 260f, 52f);

            _kok.SetAsLastSibling();
        }

        private void Kapat()
        {
            PlayerPrefs.SetInt(OkunduAnahtari, 1);
            PlayerPrefs.Save();
            UnityEngine.Object.Destroy(_kok.gameObject);
            Kapandi?.Invoke();
        }
    }
}
