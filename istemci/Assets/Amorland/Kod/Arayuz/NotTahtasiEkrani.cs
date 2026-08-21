using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Not tahtasi (B.9) — asenkron cift iletisiminin omurgasi: partner cevrimdisi
    /// olsa da niyetini tahtaya asarsin.
    ///
    /// ⚠ SERBEST METIN DEGILDIR: sunucuya yalnizca SABLON KIMLIGI + NESNE KIMLIGI
    /// gider (B.9 — icerik saklanmaz, moderasyon yuku dogmaz). 12 sablon spec'ten
    /// birebir; metin her iki istemcide de AYNI tablodan cozulur.
    ///
    /// Parametreli sablonlar ({nesne}/{bolge}) iki adimda calisir: once sablon,
    /// sonra secim listesi. Mobilde yazmadan, iki dokunusla anlamli mesaj.
    /// </summary>
    public class NotTahtasiEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;
        private RectTransform _liste;
        private Text _baslikNotu;

        /// <summary>Secili sablon; parametre adimi bunun uzerinden calisir.</summary>
        private string _bekleyenSablon;

        /// <summary>(sablonId, nesneId?) — tahtaya asma istegi.</summary>
        public event Action<string, string> Gonder;

        private enum Parametre { Yok, Nesne, Bolge }

        private struct Sablon
        {
            public string Id;
            public string Kalip;
            public Parametre Parametre;
        }

        // Spec B.9 "Not tahtasi sablonlari (12)" — sira ve icerik birebir.
        private static readonly Sablon[] Sablonlar =
        {
            new Sablon { Id = "birakti", Kalip = "Sana {0} bıraktım", Parametre = Parametre.Nesne },
            new Sablon { Id = "lazim", Kalip = "{0} lazım", Parametre = Parametre.Nesne },
            new Sablon { Id = "gidiyorum", Kalip = "{0} tarafına gidiyorum", Parametre = Parametre.Bolge },
            new Sablon { Id = "yardim", Kalip = "{0} tarafında yardım lazım", Parametre = Parametre.Bolge },
            new Sablon { Id = "tren", Kalip = "Trene {0} yetiştirelim", Parametre = Parametre.Nesne },
            new Sablon { Id = "satma", Kalip = "{0} satma, lazım olacak", Parametre = Parametre.Nesne },
            new Sablon { Id = "depo_doldu", Kalip = "Depo doldu", Parametre = Parametre.Yok },
            new Sablon { Id = "giremeyecegim", Kalip = "Bugün giremeyeceğim", Parametre = Parametre.Yok },
            new Sablon { Id = "sira_sende", Kalip = "Sıra sende", Parametre = Parametre.Yok },
            new Sablon { Id = "tesekkur", Kalip = "Teşekkürler", Parametre = Parametre.Yok },
            new Sablon { Id = "ozledim", Kalip = "Seni özledim", Parametre = Parametre.Yok },
            new Sablon { Id = "barisalim", Kalip = "Barışalım", Parametre = Parametre.Yok },
        };

        /// <summary>
        /// Sablon + nesneyi okunur cumleye cevirir. Alici ve gonderici AYNI metni
        /// gorur — cozum istemcide, icerik sunucudan hic gecmez.
        /// </summary>
        public static string Metin(string sablonId, string nesneAdi)
        {
            foreach (var s in Sablonlar)
            {
                if (s.Id != sablonId) continue;
                return s.Parametre == Parametre.Yok || string.IsNullOrEmpty(nesneAdi)
                    ? s.Kalip.Replace(" {0}", "").Replace("{0} ", "").Replace("{0}", "")
                    : string.Format(s.Kalip, nesneAdi);
            }
            return sablonId;
        }

        public NotTahtasiEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "NotTahtasiEkrani", "Not Tahtası", null, 560f, 520f);

            _baslikNotu = UiKit.Yazi(
                _panel.Icerik, "Ust", "", UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun,
                TextAnchor.MiddleCenter, true);
            UiKit.Sabitle(_baslikNotu.rectTransform, 0.5f, 1f, 0f, -2f, 500f, 26f);

            _liste = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            var disRt = _liste.parent.parent as RectTransform;
            if (disRt != null)
            {
                disRt.anchorMin = Vector2.zero;
                disRt.anchorMax = Vector2.one;
                disRt.offsetMin = new Vector2(0f, 8f);
                disRt.offsetMax = new Vector2(0f, -34f);
            }
        }

        public bool Acik => _panel.Acik;

        public void Ac()
        {
            _bekleyenSablon = null;
            _panel.Ac();
            SablonlariCiz();
        }

        private void Temizle()
        {
            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }
        }

        private void SablonlariCiz()
        {
            Temizle();
            _baslikNotu.text = "Bir not seç — eşin girince görür.";

            foreach (var s in Sablonlar)
            {
                var sablon = s;
                string onizleme = sablon.Parametre == Parametre.Yok
                    ? sablon.Kalip
                    : string.Format(sablon.Kalip, sablon.Parametre == Parametre.Nesne ? "…" : "…");
                var b = UiKit.AhsapButon(_liste, "S_" + sablon.Id, onizleme, () => SablonSecildi(sablon));
                UiKit.Boyut(b.gameObject, 0f, 46f);
            }
        }

        private void SablonSecildi(Sablon s)
        {
            if (s.Parametre == Parametre.Yok)
            {
                Gonder?.Invoke(s.Id, null);
                _panel.Kapat();
                return;
            }
            _bekleyenSablon = s.Id;
            if (s.Parametre == Parametre.Nesne) NesneleriCiz(s);
            else BolgeleriCiz(s);
        }

        private void NesneleriCiz(Sablon s)
        {
            Temizle();
            _baslikNotu.text = "“" + string.Format(s.Kalip, "…") + "” — hangi eşya?";

            GeriButonu();

            // Nesne havuzu = cantamdakiler: "sana X biraktim" ancak elde olanla
            // soylenebilir. Bos cantada da bir sey secilebilsin diye acilmis
            // urunler eklenir (ornegin "tren icin bugday yetistirelim").
            var adaylar = _durum.BenimEnvanterim.Select(e => e.EsyaId).ToList();
            var ben = _durum.Ben;
            int seviye = ben != null ? ben.Seviye : 1;
            foreach (var u in _denge.Urunler.Where(u => u.AcilisSeviye <= seviye))
            {
                if (!adaylar.Contains(u.Id)) adaylar.Add(u.Id);
            }

            foreach (var esyaId in adaylar)
            {
                string id = esyaId;
                var b = UiKit.AhsapButon(_liste, "N_" + id, _denge.EsyaAdi(id), () => ParametreSecildi(id));
                UiKit.Boyut(b.gameObject, 0f, 44f);
            }
        }

        private void BolgeleriCiz(Sablon s)
        {
            Temizle();
            _baslikNotu.text = "“" + string.Format(s.Kalip, "…") + "” — hangi bölge?";

            GeriButonu();

            var ben = _durum.Ben;
            int seviye = ben != null ? ben.Seviye : 1;
            foreach (var bolgeId in _denge.BolgeIdleri())
            {
                if (_denge.BolgeAcilisSeviyesi(bolgeId) > seviye) continue;
                string id = bolgeId;
                var b = UiKit.AhsapButon(_liste, "B_" + id, _denge.BolgeAdi(id), () => ParametreSecildi(id));
                UiKit.Boyut(b.gameObject, 0f, 44f);
            }
        }

        private void GeriButonu()
        {
            var geri = UiKit.DuzButon(_liste, "Geri", "← Şablonlara dön", SablonlariCiz);
            UiKit.Boyut(geri.gameObject, 0f, 40f);
        }

        private void ParametreSecildi(string nesneId)
        {
            if (_bekleyenSablon == null) return;
            Gonder?.Invoke(_bekleyenSablon, nesneId);
            _bekleyenSablon = null;
            _panel.Kapat();
        }
    }
}
