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

        private static readonly string[] HazirMesajlar =
        {
            "Selam!",
            "Sulamayı ben alırım.",
            "Hasat hazır, gel!",
            "Depoya bir bak.",
            "Kuyudayım.",
            "Mola verelim mi?",
        };

        public SohbetEkrani(Transform kanvas)
        {
            _panel = new Panel(kanvas, "SohbetEkrani", "Eşine mesaj", null, 560f, 470f);

            var dikey = UiKit.Panel(_panel.Icerik, "Dikey", Color.clear);
            UiKit.Doldur(dikey);
            UiKit.DikeyDizi(dikey, 8f, 12);

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
