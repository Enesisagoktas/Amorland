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
    /// Canta ekrani (B.10).
    ///
    /// ⚠ B.26 yasak 5: <b>envanterde surukle-birak YASAK.</b> Parmak esyayi orter ve
    /// yanlis slota duser. Etkilesim modeli: <b>dokun-sec -> dokun-hedef</b>.
    /// Tarladaki surukle-boyama farkli bir jesttir ve bununla celismez.
    ///
    /// ⚠ Canta kisiti esya CESIDIDIR (slot), miktar degil. Yigin 99'da dolar.
    /// </summary>
    public class CantaEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;
        private RectTransform _liste;
        private Text _ozet;
        private Text _secimYazi;

        /// <summary>Dokun-sec ile secilen esya. Ikinci dokunus hedefi belirler.</summary>
        public string SecilenEsya { get; private set; }

        public event Action<string> DepoyaGonder;
        public event Action<string> TohumSec;

        public CantaEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "CantaEkrani", "Canta", () => SecilenEsya = null);
            Kur();
            _durum.DurumDegisti += _ => { if (_panel.Acik) Tazele(); };
        }

        private void Kur()
        {
            _ozet = UiKit.Yazi(_panel.Icerik, "Ozet", "", 16, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Sabitle(_ozet.rectTransform, 0f, 1f, 0f, 0f, 640f, 24f);

            _secimYazi = UiKit.Yazi(_panel.Icerik, "Secim", "", 16, Palet.UiVurgu, TextAnchor.UpperRight);
            UiKit.Sabitle(_secimYazi.rectTransform, 1f, 1f, 0f, 0f, 400f, 24f);
            _secimYazi.rectTransform.pivot = new Vector2(1f, 1f);

            var listeKok = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            _liste = listeKok;
            var disRt = listeKok.parent.parent as RectTransform;
            if (disRt != null)
            {
                disRt.anchorMin = Vector2.zero;
                disRt.anchorMax = Vector2.one;
                disRt.offsetMin = new Vector2(0f, 56f);
                disRt.offsetMax = new Vector2(0f, -32f);
            }

            var altBar = UiKit.Panel(_panel.Icerik, "AltBar", Color.clear);
            UiKit.Sabitle(altBar, 0.5f, 0f, 0f, 0f, 660f, 48f);
            UiKit.YatayDizi(altBar, 10f, 0);

            var bosalt = UiKit.Buton(altBar, "DepoyaBosalt", "Depoya bosalt", () => DepoyaGonder?.Invoke(null));
            UiKit.Boyut(bosalt.gameObject, 200f, 44f);

            // ⚠ "Depodan al" TOPLU butonu YOKTUR (B.10). Kolaylik tek yonludur:
            // kazara bosaltma geri alinabilir, kazara toplu alma depoyu bosaltir.
            var not = UiKit.Yazi(altBar, "Not", "Depodan toplu alma yok — tek tek secilir.",
                14, Palet.UiYaziSolgun, TextAnchor.MiddleLeft);
            UiKit.Boyut(not.gameObject, 380f, 44f);
        }

        public void Ac()
        {
            _panel.Ac();
            Tazele();
        }

        public void Kapat() => _panel.Kapat();

        public bool Acik => _panel.Acik;

        public void Tazele()
        {
            var ben = _durum.Ben;
            if (ben == null) return;

            int yigin = _denge.YiginBoyutu;
            int kullanilan = 0;
            var satirlar = new List<EnvanterSatiri>(_durum.BenimEnvanterim);
            foreach (var e in satirlar) kullanilan += Mathf.CeilToInt(e.Adet / (float)yigin);

            _ozet.text = kullanilan + " / " + ben.CantaSlot + " slot   (yigin " + yigin + ")";
            _ozet.color = kullanilan >= ben.CantaSlot ? Palet.UiUyari : Palet.UiYaziSolgun;
            _secimYazi.text = SecilenEsya != null
                ? "Secili: " + _denge.EsyaAdi(SecilenEsya) + " — hedefe dokun"
                : "";

            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }

            // Kategoriye gore sirala (B.10): tohum / urun / cevher / odun / islenmis
            satirlar.Sort((a, b) =>
            {
                int ka = KategoriSirasi(_denge.EsyaKategorisi(a.EsyaId));
                int kb = KategoriSirasi(_denge.EsyaKategorisi(b.EsyaId));
                return ka != kb ? ka.CompareTo(kb)
                    : string.Compare(_denge.EsyaAdi(a.EsyaId), _denge.EsyaAdi(b.EsyaId), StringComparison.Ordinal);
            });

            foreach (var e in satirlar) SatirEkle(e);
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
                case "sarf": return 5;
                case "alet": return 6;
                default: return 7;
            }
        }

        private void SatirEkle(EnvanterSatiri e)
        {
            var satir = UiKit.Panel(_liste, "Satir_" + e.EsyaId, Palet.UiZemin);
            UiKit.Boyut(satir.gameObject, 0f, 46f);
            UiKit.YatayDizi(satir, 8f, 8);

            var buton = satir.gameObject.AddComponent<Button>();
            buton.targetGraphic = satir.gameObject.GetComponent<Image>();
            string esyaId = e.EsyaId;
            buton.onClick.AddListener(() => Sec(esyaId));

            string kategori = _denge.EsyaKategorisi(esyaId);
            var ad = UiKit.Yazi(satir, "Ad", _denge.EsyaAdi(esyaId), 18, Palet.UiYazi, TextAnchor.MiddleLeft);
            UiKit.Boyut(ad.gameObject, 300f, 30f);

            var kat = UiKit.Yazi(satir, "Kategori", kategori, 14, Palet.UiYaziSolgun, TextAnchor.MiddleLeft);
            UiKit.Boyut(kat.gameObject, 110f, 30f);

            var adet = UiKit.Yazi(satir, "Adet", e.Adet.ToString(), 18, Palet.UiVurgu, TextAnchor.MiddleRight);
            UiKit.Boyut(adet.gameObject, 70f, 30f);

            if (kategori == "tohum")
            {
                var sec = UiKit.Buton(satir, "Sec", "Cubuga al", () => TohumSec?.Invoke(esyaId));
                UiKit.Boyut(sec.gameObject, 110f, 34f);
            }
            else
            {
                var bosalt = UiKit.Buton(satir, "Depoya", "Depoya", () => DepoyaGonder?.Invoke(esyaId));
                UiKit.Boyut(bosalt.gameObject, 110f, 34f);
            }

            if (esyaId == SecilenEsya)
            {
                satir.gameObject.GetComponent<Image>().color = new Color(
                    Palet.UiVurgu.r, Palet.UiVurgu.g, Palet.UiVurgu.b, 0.22f);
            }
        }

        /// <summary>Dokun-sec: ilk dokunus secer, ikincisi ayni esyaya dokunursa secimi birakir.</summary>
        private void Sec(string esyaId)
        {
            SecilenEsya = SecilenEsya == esyaId ? null : esyaId;
            Tazele();
        }
    }
}
