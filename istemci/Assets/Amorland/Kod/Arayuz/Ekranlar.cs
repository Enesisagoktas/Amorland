using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Ag;
using Amorland.Durum;
using Amorland.Gorsel;
using Amorland.Ses;

namespace Amorland.Arayuz
{
    /// <summary>Tezgah (craft) ekrani. Sv 1'de tas kazma, tas balta, ahsap kova (B.17).</summary>
    public class TezgahEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;
        private RectTransform _liste;

        public event Action<string> Craftla;

        public TezgahEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "TezgahEkrani", "Basit tezgah");
            _liste = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            var dis = _liste.parent.parent as RectTransform;
            if (dis != null) UiKit.Doldur(dis);
            _durum.DurumDegisti += _ => { if (_panel.Acik) Tazele(); };
        }

        public void Ac()
        {
            _panel.Ac();
            Tazele();
        }

        public bool Acik => _panel.Acik;

        public void Tazele()
        {
            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }

            var ben = _durum.Ben;
            if (ben == null) return;

            foreach (string tarifId in _denge.TezgahTarifleri("basit_tezgah"))
            {
                var satir = UiKit.Panel(_liste, "Tarif_" + tarifId, Palet.UiZemin);
                UiKit.Boyut(satir.gameObject, 0f, 54f);
                UiKit.YatayDizi(satir, 8f, 8);

                var ad = UiKit.Yazi(satir, "Ad", _denge.EsyaAdi(tarifId), 18, Palet.UiYazi, TextAnchor.MiddleLeft);
                UiKit.Boyut(ad.gameObject, 180f, 36f);

                var malzemeler = _denge.AletMalzemesi(tarifId);
                bool yeter = true;
                var parcalar = new List<string>();
                foreach (var m in malzemeler)
                {
                    int elde = _durum.EnvanterAdet(m.EsyaId);
                    if (elde < m.Adet) yeter = false;
                    parcalar.Add(elde + "/" + m.Adet + " " + _denge.EsyaAdi(m.EsyaId));
                }

                var malzemeYazi = UiKit.Yazi(
                    satir, "Malzeme", string.Join("  ·  ", parcalar), 15,
                    yeter ? Palet.UiYazi : Palet.UiUyari, TextAnchor.MiddleLeft);
                UiKit.Boyut(malzemeYazi.gameObject, 320f, 36f);

                bool sahip = _durum.AletVar(tarifId);
                string tid = tarifId;
                var buton = UiKit.Buton(satir, "Yap", sahip ? "Yenile" : "Yap", () => Craftla?.Invoke(tid));
                buton.interactable = yeter && ben.Seviye >= _denge.AletCraftSeviyesi(tarifId);
                UiKit.Boyut(buton.gameObject, 100f, 40f);
            }

            var not = UiKit.Yazi(
                _liste, "Not",
                "Tas kazma ve tas balta Sv 1'den yapilabilir; dal ve tas parcasi elle toplanir. "
                + "Boylece alet kirilsa bile oyun kilitlenmez.",
                14, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(not.gameObject, 0f, 46f);
        }
    }

    /// <summary>Defter (B.21). Anlatim tamamen toplanabilir metin sayfalariyla yurur.</summary>
    public class DefterEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private RectTransform _liste;

        /// <summary>Dikey dilimde ilk 5 sayfanin metni. Gerisi perde perde yazilir (A.8).</summary>
        private static readonly Dictionary<int, string> Sayfalar = new Dictionary<int, string>
        {
            { 1, "Bu vadide hicbir is tek basina gorulmez.\n\nTren manifestosu iki imza ister. "
                 + "Madende ikinci bir el olmadan derine inilmez. Tarlada biri eker, biri sular." },
            { 2, "Ilk hasat.\n\nToprak verdigini geri aldigin gun, vadi seni tanimaya baslar." },
            { 3, "Depo acildi.\n\nBurada biraktiklarin ona ulasir. O da sana birakabilir." },
            { 4, "Ikinci imza.\n\nManifesto iki isim ister. Cagirdigin kisi ayni vadiye duser." },
            { 5, "Tren yillar sonra ilk kez durdu.\n\nVadinin disariyla baglantisi geri geldi." }
        };

        public DefterEkrani(Transform kanvas, DunyaDurumu durum)
        {
            _durum = durum;
            _panel = new Panel(kanvas, "DefterEkrani", "Birlikte yapilanlar defteri");
            _liste = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            var dis = _liste.parent.parent as RectTransform;
            if (dis != null) UiKit.Doldur(dis);
        }

        public void Ac()
        {
            _panel.Ac();
            Tazele();
        }

        public bool Acik => _panel.Acik;

        public void Tazele()
        {
            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }

            int acik = _durum.DefterSayfasi;
            var baslik = UiKit.Yazi(
                _liste, "Ilerleme", acik + " / 20 sayfa", 16, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(baslik.gameObject, 0f, 26f);

            for (int no = 1; no <= 20; no++)
            {
                bool acikMi = _durum.IsaretVar("defter:" + no);
                var kart = UiKit.Panel(_liste, "Sayfa" + no, Palet.UiZemin);
                UiKit.Boyut(kart.gameObject, 0f, acikMi ? 96f : 40f);
                UiKit.DikeyDizi(kart, 4f, 10);

                var no_ = UiKit.Yazi(
                    kart, "No", "Sayfa " + no, 15,
                    acikMi ? Palet.UiVurgu : Palet.UiYaziSolgun, TextAnchor.UpperLeft);
                UiKit.Boyut(no_.gameObject, 0f, 20f);

                string metin = acikMi
                    ? (Sayfalar.TryGetValue(no, out string m) ? m : "(bu sayfanin metni henuz yazilmadi)")
                    : "Henuz acilmadi.";
                var govde = UiKit.Yazi(
                    kart, "Metin", metin, 15,
                    acikMi ? Palet.UiYazi : Palet.UiYaziSolgun, TextAnchor.UpperLeft);
                UiKit.Boyut(govde.gameObject, 0f, acikMi ? 64f : 0f);
            }
        }
    }

    /// <summary>Vadi haritasi (B.4). Bolgeye dokunursun, o sahneye girersin.</summary>
    public class HaritaEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;
        private RectTransform _liste;

        public event Action<string> BolgeSecildi;

        public HaritaEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "HaritaEkrani", "Vadi haritasi", null, 660f, 460f);
            _liste = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            var dis = _liste.parent.parent as RectTransform;
            if (dis != null) UiKit.Doldur(dis);
        }

        public void Ac()
        {
            _panel.Ac();
            Tazele();
        }

        public bool Acik => _panel.Acik;

        public void Tazele()
        {
            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }

            int dunyaSeviyesi = _durum.Vadi != null ? _durum.Vadi.DunyaSeviyesi : 1;

            foreach (string bolgeId in _denge.BolgeIdleri())
            {
                int gereken = _denge.BolgeAcilisSeviyesi(bolgeId);
                bool acik = dunyaSeviyesi >= gereken;

                var satir = UiKit.Panel(_liste, "Bolge_" + bolgeId, Palet.UiZemin);
                UiKit.Boyut(satir.gameObject, 0f, 52f);
                UiKit.YatayDizi(satir, 8f, 10);

                var ad = UiKit.Yazi(
                    satir, "Ad", _denge.BolgeAdi(bolgeId), 19,
                    // Kapali bolgeler SILIK gorunur ama GORUNUR (B.18 0:30): oyuncu
                    // vadinin buyuklugunu ilk dakikada gorsun.
                    acik ? Palet.UiYazi : Palet.UiYaziSolgun, TextAnchor.MiddleLeft);
                UiKit.Boyut(ad.gameObject, 260f, 34f);

                var durum = UiKit.Yazi(
                    satir, "Durum", acik ? "" : "Sv " + gereken, 15,
                    Palet.UiYaziSolgun, TextAnchor.MiddleRight);
                UiKit.Boyut(durum.gameObject, 90f, 34f);

                string bid = bolgeId;
                var git = UiKit.Buton(satir, "Git", "Git", () => BolgeSecildi?.Invoke(bid));
                git.interactable = acik;
                UiKit.Boyut(git.gameObject, 90f, 38f);
            }
        }
    }

    /// <summary>Partner daveti (B.7, B.19). Sv 3'te acilir, hesap baglama ZORUNLUDUR.</summary>
    public class DavetEkrani
    {
        private readonly Panel _panel;
        private Text _kodYazi;
        private Text _durumYazi;
        private InputField _kodGiris;

        public event Action KodIste;
        public event Action<string> Katil;
        public event Action HesapBagla;

        public DavetEkrani(Transform kanvas)
        {
            _panel = new Panel(kanvas, "DavetEkrani", "Esini vadiye cagir", null, 660f, 420f);

            var dikey = UiKit.Panel(_panel.Icerik, "Dikey", Color.clear);
            UiKit.Doldur(dikey);
            UiKit.DikeyDizi(dikey, 10f, 8);

            var aciklama = UiKit.Yazi(
                dikey, "Aciklama",
                "Bu vadide hicbir is tek basina gorulmez. Kodu esine gonder; ayni vadiye duser.\n"
                + "Rastgele eslesme yoktur — yalnizca cagirdigin kisi gelir.",
                16, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(aciklama.gameObject, 0f, 60f);

            _kodYazi = UiKit.Yazi(dikey, "Kod", "— — — — — —", 44, Palet.UiVurgu, TextAnchor.MiddleCenter);
            UiKit.Boyut(_kodYazi.gameObject, 0f, 70f);

            var uret = UiKit.Buton(dikey, "Uret", "Davet kodu uret", () => KodIste?.Invoke());
            UiKit.Boyut(uret.gameObject, 0f, 48f);

            _durumYazi = UiKit.Yazi(dikey, "Durum", "", 15, Palet.UiUyari, TextAnchor.UpperLeft);
            UiKit.Boyut(_durumYazi.gameObject, 0f, 46f);

            var bagla = UiKit.Buton(dikey, "Bagla", "Hesabimi bagla (e-posta)", () => HesapBagla?.Invoke());
            UiKit.Boyut(bagla.gameObject, 0f, 44f);

            var ayrac = UiKit.Yazi(dikey, "Ayrac", "— veya kodla katil —", 14, Palet.UiYaziSolgun,
                TextAnchor.MiddleCenter);
            UiKit.Boyut(ayrac.gameObject, 0f, 24f);

            var girisRt = UiKit.Panel(dikey, "KodGiris", Palet.UiZemin);
            UiKit.Boyut(girisRt.gameObject, 0f, 44f);
            _kodGiris = girisRt.gameObject.AddComponent<InputField>();
            var girisYazi = UiKit.Yazi(girisRt, "Yazi", "", 22, Palet.UiYazi, TextAnchor.MiddleCenter);
            UiKit.Doldur(girisYazi.rectTransform, 6f);
            _kodGiris.textComponent = girisYazi;
            _kodGiris.characterLimit = 8;

            var katilButon = UiKit.Buton(dikey, "Katil", "Kodla katil",
                () => Katil?.Invoke(_kodGiris.text));
            UiKit.Boyut(katilButon.gameObject, 0f, 44f);
        }

        public void Ac() => _panel.Ac();

        public bool Acik => _panel.Acik;

        public void KodGoster(string kod)
        {
            _kodYazi.text = string.IsNullOrEmpty(kod) ? "— — — — — —" : kod;
            _durumYazi.text = string.IsNullOrEmpty(kod) ? "" : "Kod 24 saat gecerli.";
            _durumYazi.color = Palet.UiOnay;
        }

        public void HataGoster(string mesaj)
        {
            _durumYazi.text = mesaj;
            _durumYazi.color = Palet.UiUyari;
        }
    }

    /// <summary>Ayarlar: ses kanallari, haptik, sunucu adresi.</summary>
    public class AyarlarEkrani
    {
        private readonly Panel _panel;
        private readonly SesYoneticisi _ses;
        private InputField _adresGiris;

        public event Action<string> AdresDegisti;

        public AyarlarEkrani(Transform kanvas, SesYoneticisi ses)
        {
            _ses = ses;
            _panel = new Panel(kanvas, "AyarlarEkrani", "Ayarlar", null, 620f, 480f);

            var dikey = UiKit.Panel(_panel.Icerik, "Dikey", Color.clear);
            UiKit.Doldur(dikey);
            UiKit.DikeyDizi(dikey, 8f, 8);

            foreach (SesKanali kanal in Enum.GetValues(typeof(SesKanali)))
            {
                SesKanali k = kanal;
                var satir = UiKit.Panel(dikey, "Ses_" + kanal, Palet.UiZemin);
                UiKit.Boyut(satir.gameObject, 0f, 44f);
                UiKit.YatayDizi(satir, 8f, 8);

                var ad = UiKit.Yazi(satir, "Ad", kanal.ToString(), 17, Palet.UiYazi, TextAnchor.MiddleLeft);
                UiKit.Boyut(ad.gameObject, 140f, 30f);

                var deger = UiKit.Yazi(
                    satir, "Deger", Mathf.RoundToInt(_ses.KanalSeviyesi(k) * 100f) + "%",
                    16, Palet.UiVurgu, TextAnchor.MiddleRight);
                UiKit.Boyut(deger.gameObject, 70f, 30f);

                var azalt = UiKit.Buton(satir, "Az", "-", () =>
                {
                    _ses.KanalSeviyesiAyarla(k, _ses.KanalSeviyesi(k) - 0.1f);
                    deger.text = Mathf.RoundToInt(_ses.KanalSeviyesi(k) * 100f) + "%";
                });
                UiKit.Boyut(azalt.gameObject, 54f, 36f);

                var artir = UiKit.Buton(satir, "Cok", "+", () =>
                {
                    _ses.KanalSeviyesiAyarla(k, _ses.KanalSeviyesi(k) + 0.1f);
                    deger.text = Mathf.RoundToInt(_ses.KanalSeviyesi(k) * 100f) + "%";
                });
                UiKit.Boyut(artir.gameObject, 54f, 36f);
            }

            // Haptik kapatilabilir olmak ZORUNDA (B.23).
            var haptikSatir = UiKit.Panel(dikey, "Haptik", Palet.UiZemin);
            UiKit.Boyut(haptikSatir.gameObject, 0f, 44f);
            UiKit.YatayDizi(haptikSatir, 8f, 8);
            var haptikAd = UiKit.Yazi(haptikSatir, "Ad", "Titresim", 17, Palet.UiYazi, TextAnchor.MiddleLeft);
            UiKit.Boyut(haptikAd.gameObject, 200f, 30f);
            Button haptikButon = null;
            haptikButon = UiKit.Buton(haptikSatir, "Degistir",
                Haptik.Haptik.Acik ? "Acik" : "Kapali",
                () =>
                {
                    Haptik.Haptik.Acik = !Haptik.Haptik.Acik;
                    var y = haptikButon.GetComponentInChildren<Text>();
                    if (y != null) y.text = Haptik.Haptik.Acik ? "Acik" : "Kapali";
                });
            UiKit.Boyut(haptikButon.gameObject, 120f, 36f);

            var adresSatir = UiKit.Panel(dikey, "Adres", Palet.UiZemin);
            UiKit.Boyut(adresSatir.gameObject, 0f, 44f);
            UiKit.YatayDizi(adresSatir, 8f, 8);
            var adresAd = UiKit.Yazi(adresSatir, "Ad", "Sunucu", 17, Palet.UiYazi, TextAnchor.MiddleLeft);
            UiKit.Boyut(adresAd.gameObject, 100f, 30f);

            var girisRt = UiKit.Panel(adresSatir, "Giris", Palet.UiPanel);
            UiKit.Boyut(girisRt.gameObject, 280f, 36f);
            _adresGiris = girisRt.gameObject.AddComponent<InputField>();
            var girisYazi = UiKit.Yazi(girisRt, "Yazi", SunucuAyari.Temel, 15, Palet.UiYazi, TextAnchor.MiddleLeft);
            UiKit.Doldur(girisYazi.rectTransform, 6f);
            _adresGiris.textComponent = girisYazi;
            _adresGiris.text = SunucuAyari.Temel;

            var kaydet = UiKit.Buton(adresSatir, "Kaydet", "Kaydet",
                () => AdresDegisti?.Invoke(_adresGiris.text));
            UiKit.Boyut(kaydet.gameObject, 100f, 36f);

            var not = UiKit.Yazi(
                dikey, "Not",
                "Sesi kapali oynayabilirsin: hicbir mekanik yalnizca sesle bildirilmez.",
                14, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(not.gameObject, 0f, 40f);
        }

        public void Ac() => _panel.Ac();

        public bool Acik => _panel.Acik;
    }
}
