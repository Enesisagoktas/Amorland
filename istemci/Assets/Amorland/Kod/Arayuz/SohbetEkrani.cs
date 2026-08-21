using System;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Esine mesaj paneli (B.7 cift iletisimi + B.8 kusluk).
    ///
    /// Sohbet icerigi sunucuda SAKLANMAZ (mimari kural 6) — yalnizca iletilir.
    /// Hazir mesajlar one cikar: mobilde yazmak yavas, ciftlik sohbetinin
    /// %90'i ayni bes cumledir. Serbest metin altta yine de durur.
    ///
    /// Kusluk dugmesi de burada: kusluk bir ILETISIM jestidir (B.8 — ekonomik
    /// etkisi yok, bildirim gondermez), ayri bir ayar ekranina gomulmez.
    /// </summary>
    public class SohbetEkrani
    {
        private readonly Panel _panel;
        private readonly InputField _giris;
        private readonly Button _kuslukButon;

        public event Action<string> Gonder;
        public event Action KuslukDegistir;

        /// <summary>Ifade tekerlegi secimi (B.9): parametre ifade kimligi.</summary>
        public event Action<string> Ifade;

        /// <summary>Not tahtasini ac.</summary>
        public event Action NotTahtasi;

        // Spec B.9 "Ifade tekerlegi (8 — fazlasi parmakla iskalanir)" — birebir.
        private static readonly (string Id, string Ad)[] Ifadeler =
        {
            ("selam", "Selam!"),
            ("tesekkur", "Teşekkür!"),
            ("evet", "Evet"),
            ("hayir", "Hayır"),
            ("yardim", "Yardım!"),
            ("bekle", "Bekle"),
            ("harika", "Harika!"),
            ("barisalim", "Barışalım"),
        };

        /// <summary>Ifade kimligini gorunen yaziya cevirir (balon iki tarafta ayni).</summary>
        public static string IfadeAdi(string id)
        {
            foreach (var i in Ifadeler)
            {
                if (i.Id == id) return i.Ad;
            }
            return id;
        }

        private static readonly string[] HazirMesajlar =
        {
            "Sulamayı ben alırım.",
            "Hasat hazır, gel!",
            "Depoya bir bak.",
            "Mola verelim mi?",
        };

        public SohbetEkrani(Transform kanvas)
        {
            _panel = new Panel(kanvas, "SohbetEkrani", "Eşine mesaj", null, 560f, 470f);

            var dikey = UiKit.Panel(_panel.Icerik, "Dikey", Color.clear);
            UiKit.Doldur(dikey);
            UiKit.DikeyDizi(dikey, 8f, 12);

            // Ifade tekerlegi: 2x4, tek dokunus — panel kapanir, balon ucar.
            for (int i = 0; i < Ifadeler.Length; i += 4)
            {
                var ifadeSatir = UiKit.Panel(dikey, "Ifade" + i, Color.clear);
                UiKit.Boyut(ifadeSatir.gameObject, 0f, 42f);
                var ifadeDizi = UiKit.YatayDizi(ifadeSatir, 8f, 0);
                ifadeDizi.childForceExpandWidth = true;

                for (int j = i; j < Mathf.Min(i + 4, Ifadeler.Length); j++)
                {
                    string ifadeId = Ifadeler[j].Id;
                    var ib = UiKit.DuzButon(ifadeSatir, "I" + j, Ifadeler[j].Ad, () =>
                    {
                        Ifade?.Invoke(ifadeId);
                        _panel.Kapat();
                    });
                    UiKit.Boyut(ib.gameObject, 0f, 40f).flexibleWidth = 1f;
                }
            }

            EkranParcasi.Ayirac(dikey);

            // Hazir mesajlar iki sutunlu satirlar halinde
            for (int i = 0; i < HazirMesajlar.Length; i += 2)
            {
                var satir = UiKit.Panel(dikey, "Satir" + i, Color.clear);
                UiKit.Boyut(satir.gameObject, 0f, 46f);
                var dizi = UiKit.YatayDizi(satir, 8f, 0);
                dizi.childForceExpandWidth = true;

                for (int j = i; j < Mathf.Min(i + 2, HazirMesajlar.Length); j++)
                {
                    string m = HazirMesajlar[j];
                    var b = UiKit.AhsapButon(satir, "Hazir" + j, m, () => MesajGonder(m));
                    UiKit.Boyut(b.gameObject, 0f, 44f).flexibleWidth = 1f;
                }
            }

            EkranParcasi.Ayirac(dikey);

            // Serbest metin + gonder
            var yazSatir = UiKit.Panel(dikey, "YazSatir", Color.clear);
            UiKit.Boyut(yazSatir.gameObject, 0f, 52f);
            var yazDizi = UiKit.YatayDizi(yazSatir, 8f, 0);
            yazDizi.childForceExpandHeight = false;
            yazDizi.childAlignment = TextAnchor.MiddleLeft;

            RectTransform girisIc;
            var girisKutu = EkranParcasi.Satir(yazSatir, "GirisKutu", Palet.UiKagitCukur, 52f, out girisIc);
            EkranParcasi.Esnet(girisKutu.gameObject, 280f);
            _giris = UiKit.Giris(girisIc, "Giris", "kendi cümlen...", 60);
            UiKit.Doldur(_giris.GetComponent<RectTransform>());
            EkranParcasi.KagitGiris(_giris);
            _giris.textComponent.fontSize = UiKit.YaziGovde;
            var ipucu = _giris.placeholder as Text;
            if (ipucu != null) ipucu.fontSize = UiKit.YaziGovde;

            var gonderButon = UiKit.YesilButon(yazSatir, "Gonder", "Gönder",
                () => MesajGonder(_giris.text));
            UiKit.Boyut(gonderButon.gameObject, 130f, 50f);

            EkranParcasi.Ayirac(dikey);

            var notButon = UiKit.AhsapButon(dikey, "NotTahtasi", "Not Tahtası", () =>
            {
                NotTahtasi?.Invoke();
                _panel.Kapat();
            });
            UiKit.Boyut(notButon.gameObject, 0f, 44f);

            // Kusluk: iletisim jesti — buton yazisi Ac()'ta duruma gore ayarlanir.
            _kuslukButon = UiKit.AhsapButon(dikey, "Kusluk", "", () =>
            {
                KuslukDegistir?.Invoke();
                _panel.Kapat();
            });
            UiKit.Boyut(_kuslukButon.gameObject, 0f, 44f);
        }

        public void Ac(bool kuslukAktif, bool partnerVar)
        {
            var yazi = _kuslukButon.GetComponentInChildren<Text>();
            if (yazi != null)
            {
                yazi.text = kuslukAktif ? "Küslüğü bitir" : "Küslük başlat";
            }
            _kuslukButon.gameObject.SetActive(partnerVar);
            _panel.Ac();
        }

        public bool Acik => _panel.Acik;

        private float _sonGonderim;

        private void MesajGonder(string metin)
        {
            if (string.IsNullOrWhiteSpace(metin)) return;
            // B.9: istemci tarafi gaz kelebegi — saniyede en fazla 1 mesaj.
            if (Time.unscaledTime - _sonGonderim < 1f) return;
            _sonGonderim = Time.unscaledTime;
            Gonder?.Invoke(metin.Trim());
            _giris.text = "";
            _panel.Kapat();
        }
    }
}
