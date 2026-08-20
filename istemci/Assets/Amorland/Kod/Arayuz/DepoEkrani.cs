using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Ag;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Depo ekrani (B.4, B.10).
    ///
    /// ⚠ Depo <b>basit bir envanter ekrani gibi tasarlanmaz.</b> Ciftin fiziksel bulusma
    /// noktasidir: partner cevrimdisiyken bile "sana demir biraktim" iletisimi burada olur.
    /// Asenkron ortak oynanisin kalbi.
    ///
    /// Bu yuzden her yiginda <b>koyanin renk rozeti</b> vardir ve kim ne biraktigi gorunur.
    ///
    /// ⚠ Depo kisiti toplam MIKTARDIR, slot degil (B.10). Cubuk bunu gosterir; doluluk
    /// arttikca renk degisir. Dolu depo hasati engeller — bu SESSIZ kalamaz.
    /// </summary>
    public class DepoEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;

        private RectTransform _liste;
        private Text _ozet;
        private Image _dolulukDolgu;
        private const float CubukGenislik = 640f;

        public event Action<string, int> DepodanAl;
        public event Action<string> KilitDegistir;
        public event Action DepoyaBosalt;

        public DepoEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "DepoEkrani", "Depo");
            Kur();
            _durum.DurumDegisti += _ => { if (_panel.Acik) Tazele(); };
        }

        private void Kur()
        {
            _ozet = UiKit.Yazi(_panel.Icerik, "Ozet", "", 16, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Sabitle(_ozet.rectTransform, 0f, 1f, 0f, 0f, 640f, 22f);

            var arka = UiKit.Cubuk(
                _panel.Icerik, "Doluluk", new Color(1f, 1f, 1f, 0.10f), Palet.UiOnay, out _dolulukDolgu);
            UiKit.Sabitle(arka.rectTransform, 0f, 1f, 0f, -26f, CubukGenislik, 10f);

            _liste = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            var disRt = _liste.parent.parent as RectTransform;
            if (disRt != null)
            {
                disRt.anchorMin = Vector2.zero;
                disRt.anchorMax = Vector2.one;
                disRt.offsetMin = new Vector2(0f, 56f);
                disRt.offsetMax = new Vector2(0f, -44f);
            }

            var altBar = UiKit.Panel(_panel.Icerik, "AltBar", Color.clear);
            UiKit.Sabitle(altBar, 0.5f, 0f, 0f, 0f, 660f, 48f);
            UiKit.YatayDizi(altBar, 10f, 0);

            // ⚠ "Depoya bosalt" tek butonu ZORUNLU (B.10): bu buton olmadan oyun mobilde iskence.
            var bosalt = UiKit.Buton(altBar, "Bosalt", "Cantayi depoya bosalt", () => DepoyaBosalt?.Invoke());
            UiKit.Boyut(bosalt.gameObject, 260f, 44f);

            var not = UiKit.Yazi(altBar, "Not",
                "Kilit: en fazla 3 yigin. Kilitli yigini partner alamaz.",
                14, Palet.UiYaziSolgun, TextAnchor.MiddleLeft);
            UiKit.Boyut(not.gameObject, 380f, 44f);
        }

        public void Ac()
        {
            _panel.Ac();
            Tazele();
        }

        public bool Acik => _panel.Acik;

        public void Tazele()
        {
            var vadi = _durum.Vadi;
            if (vadi == null) return;

            if (vadi.DepoKapasitesi <= 0)
            {
                _ozet.text = "Depo Sv 2'de acilir.";
                _ozet.color = Palet.UiYaziSolgun;
                UiKit.CubukAyarla(_dolulukDolgu, 0f, CubukGenislik);
                return;
            }

            float oran = vadi.DepoDolu / (float)vadi.DepoKapasitesi;
            _ozet.text = vadi.DepoDolu + " / " + vadi.DepoKapasitesi + " birim";
            _ozet.color = oran >= 1f ? Palet.UiUyari : Palet.UiYaziSolgun;
            UiKit.CubukAyarla(_dolulukDolgu, oran, CubukGenislik);
            _dolulukDolgu.color = oran >= 1f ? Palet.UiUyari : oran > 0.85f ? Palet.UiVurgu : Palet.UiOnay;

            if (oran >= 1f)
            {
                _ozet.text += "  —  DOLU: hasat engellenir, urun tarlada bekler.";
            }

            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }

            var satirlar = new List<DepoSatiri>(_durum.Depo.Values);
            satirlar.Sort((a, b) =>
            {
                int ka = KategoriSirasi(_denge.EsyaKategorisi(a.EsyaId));
                int kb = KategoriSirasi(_denge.EsyaKategorisi(b.EsyaId));
                return ka != kb ? ka.CompareTo(kb)
                    : string.Compare(_denge.EsyaAdi(a.EsyaId), _denge.EsyaAdi(b.EsyaId), StringComparison.Ordinal);
            });

            foreach (var s in satirlar) SatirEkle(s);
        }

        private static int KategoriSirasi(string kategori)
        {
            switch (kategori)
            {
                case "tohum": return 0;
                case "urun": return 1;
                case "cevher": return 2;
                case "odun": return 3;
                case "islenmis": return 4;
                default: return 5;
            }
        }

        private void SatirEkle(DepoSatiri s)
        {
            var satir = UiKit.Panel(_liste, "Satir_" + s.EsyaId, Palet.UiZemin);
            UiKit.Boyut(satir.gameObject, 0f, 46f);
            UiKit.YatayDizi(satir, 8f, 8);

            // Koyanin renk rozeti: "kim biraktı" bilgisi depo UX'inin merkezinde (B.10).
            Color rozetRengi = Palet.UiYaziSolgun;
            if (s.Koyan != null && _durum.Oyuncular.TryGetValue(s.Koyan, out OyuncuOzeti koyan))
            {
                rozetRengi = _denge.OyuncuRengi(koyan.Renk);
            }
            UiKit.Rozet(satir, rozetRengi);

            var ad = UiKit.Yazi(satir, "Ad", _denge.EsyaAdi(s.EsyaId), 18, Palet.UiYazi, TextAnchor.MiddleLeft);
            UiKit.Boyut(ad.gameObject, 260f, 30f);

            var adet = UiKit.Yazi(satir, "Adet", s.Adet.ToString(), 18, Palet.UiVurgu, TextAnchor.MiddleRight);
            UiKit.Boyut(adet.gameObject, 70f, 30f);

            string esyaId = s.EsyaId;
            bool benimKilidim = s.Kilitli && s.Kilitleyen == _durum.BenimId;
            bool baskasininKilidi = s.Kilitli && !benimKilidim;

            var al = UiKit.Buton(satir, "Al", baskasininKilidi ? "Kilitli" : "Al", () =>
            {
                if (!baskasininKilidi) DepodanAl?.Invoke(esyaId, s.Adet);
            });
            al.interactable = !baskasininKilidi;
            UiKit.Boyut(al.gameObject, 80f, 34f);

            var kilit = UiKit.Buton(satir, "Kilit", s.Kilitli ? "Ac" : "Kilitle",
                () => KilitDegistir?.Invoke(esyaId));
            kilit.interactable = !baskasininKilidi;
            UiKit.Boyut(kilit.gameObject, 90f, 34f);

            if (s.Kilitli)
            {
                satir.gameObject.GetComponent<Image>().color = new Color(
                    Palet.UiUyari.r, Palet.UiUyari.g, Palet.UiUyari.b, 0.14f);
            }
        }
    }
}
