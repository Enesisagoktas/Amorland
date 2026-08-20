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
    /// Rozet HUD'dakinden iri, adin altinda da yazili: "kim koydu" burada bir detay degil,
    /// satirin kimligidir.
    ///
    /// ⚠ Depo kisiti toplam MIKTARDIR, slot degil (B.10). Cubuk bunu gosterir; doluluk
    /// arttikca renk degisir. Dolu depo hasati engeller — bu SESSIZ kalamaz, o yuzden
    /// doluluk kendi golgeli kartinda ekranin en ustunde durur.
    /// </summary>
    public class DepoEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;

        private RectTransform _liste;
        private Text _ozet;
        private Text _uyari;
        private Image _dolulukDolgu;

        // Depo ciftin en cok actigi ekran: varsayilan modalden genis, cunku 46'lik
        // satirlara sikistirilmis liste "kim ne birakti" bilgisini okunmaz yapiyordu.
        private const float PanelGenislik = 860f;
        private const float PanelYukseklik = 560f;
        private const float KartGenislik = PanelGenislik - 40f;
        private const float CubukGenislik = KartGenislik - 36f;
        private const float SatirYuksekligi = 58f;

        public event Action<string, int> DepodanAl;
        public event Action<string> KilitDegistir;
        public event Action DepoyaBosalt;

        public DepoEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "DepoEkrani", "Depo", null, PanelGenislik, PanelYukseklik);
            Kur();
            _durum.DurumDegisti += _ => { if (_panel.Acik) Tazele(); };
        }

        private void Kur()
        {
            // Doluluk kendi yuzeyinde ve golgeli: hasat engellenmesinin tek gorsel
            // uyarisi bu kart, listenin arasinda kaybolmamali.
            var ozetKart = UiKit.Kart(_panel.Icerik, "OzetKart", Palet.UiYuzey);
            UiKit.Sabitle(ozetKart, 0.5f, 1f, 0f, 0f, KartGenislik, 86f);
            UiKit.GolgeEkle(ozetKart, UiKit.Yaricap, 14, 4f);

            _ozet = UiKit.Yazi(
                ozetKart, "Ozet", "", UiKit.YaziBaslik, Palet.UiYazi, TextAnchor.UpperLeft, true);
            UiKit.Sabitle(_ozet.rectTransform, 0f, 1f, 18f, -14f, 380f, 28f);

            _uyari = UiKit.Yazi(
                ozetKart, "Uyari", "", UiKit.YaziKucuk, Palet.UiYaziSolgun, TextAnchor.UpperRight, true);
            UiKit.Sabitle(_uyari.rectTransform, 1f, 1f, -18f, -16f, 380f, 24f);

            // Cubuk zemini cukur ton: dolgunun nerede bittigi ancak boyle okunuyor.
            var arka = UiKit.Cubuk(ozetKart, "Doluluk", Palet.UiYuzeyKoyu, Palet.UiOnay, out _dolulukDolgu);
            UiKit.Sabitle(arka.rectTransform, 0f, 1f, 18f, -52f, CubukGenislik, 14f);

            _liste = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            var disRt = _liste.parent.parent as RectTransform;
            if (disRt != null)
            {
                disRt.anchorMin = Vector2.zero;
                disRt.anchorMax = Vector2.one;
                // Ust: ozet karti (86) + nefes payi. Alt: alt serit (52) + nefes payi.
                disRt.offsetMin = new Vector2(0f, 68f);
                disRt.offsetMax = new Vector2(0f, -98f);
            }

            var altBar = UiKit.Panel(_panel.Icerik, "AltBar", Color.clear);
            UiKit.Sabitle(altBar, 0.5f, 0f, 0f, 0f, KartGenislik, 52f);
            var altDizi = UiKit.YatayDizi(altBar, 12f, 0);
            altDizi.childForceExpandHeight = false;
            altDizi.childAlignment = TextAnchor.MiddleLeft;

            // ⚠ "Depoya bosalt" tek butonu ZORUNLU (B.10): bu buton olmadan oyun mobilde
            // iskence. Ekranin ana eylemi bu — vurgu zemini yalnizca o hak ediyor.
            var bosalt = UiKit.AnaButon(altBar, "Bosalt", "Çantayı depoya boşalt", () => DepoyaBosalt?.Invoke());
            UiKit.Boyut(bosalt.gameObject, 268f, 48f);

            var not = UiKit.Yazi(altBar, "Not",
                "Kilitli yığını partner alamaz · en fazla 3 yığın",
                UiKit.YaziKucuk, Palet.UiYaziSolgun, TextAnchor.MiddleRight);
            UiKit.Boyut(not.gameObject, 380f, 44f).flexibleWidth = 1f;
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
                _ozet.text = "Depo Sv 2'de açılır.";
                _ozet.color = Palet.UiYaziSolgun;
                _uyari.text = "";
                UiKit.CubukAyarla(_dolulukDolgu, 0f, CubukGenislik);
                return;
            }

            float oran = vadi.DepoDolu / (float)vadi.DepoKapasitesi;
            _ozet.text = vadi.DepoDolu + " / " + vadi.DepoKapasitesi + " birim";
            _ozet.color = oran >= 1f ? Palet.UiUyari : Palet.UiYazi;
            UiKit.CubukAyarla(_dolulukDolgu, oran, CubukGenislik);

            // Kritik doluluk uc kademede okunur: renk, yazi ve cubuk ayni anda doner.
            if (oran >= 1f)
            {
                _dolulukDolgu.color = Palet.UiUyari;
                _uyari.color = Palet.UiUyari;
                _uyari.text = "DOLU · hasat engellenir, ürün tarlada bekler";
            }
            else if (oran > 0.85f)
            {
                _dolulukDolgu.color = Palet.UiVurgu;
                _uyari.color = Palet.UiVurgu;
                _uyari.text = "dolmak üzere · %" + Mathf.RoundToInt(oran * 100f);
            }
            else
            {
                _dolulukDolgu.color = Palet.UiOnay;
                _uyari.color = Palet.UiYaziSolgun;
                _uyari.text = "%" + Mathf.RoundToInt(oran * 100f);
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

            if (satirlar.Count == 0)
            {
                BosDurumEkle();
                return;
            }

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

        /// <summary>Bos depo, bos kutu gibi gorunmemeli: ne yapilacagini soyleyen tek satir.</summary>
        private void BosDurumEkle()
        {
            var yazi = UiKit.Yazi(
                _liste, "Bos", "Depo boş. Çantandaki fazlalığı buraya bırak — partnerin de alabilir.",
                UiKit.YaziGovde, Palet.UiYaziSolgun, TextAnchor.MiddleCenter);
            UiKit.Boyut(yazi.gameObject, 0f, 96f);
        }

        private void SatirEkle(DepoSatiri s)
        {
            bool benimKilidim = s.Kilitli && s.Kilitleyen == _durum.BenimId;
            bool baskasininKilidi = s.Kilitli && !benimKilidim;

            // Baskasinin kilidi = dokunulamaz yigin. Cukur yuzey bunu tiklamadan once soyler.
            var satir = UiKit.Kart(
                _liste, "Satir_" + s.EsyaId,
                baskasininKilidi ? Palet.UiYuzeyKoyu : Palet.UiYuzey, UiKit.YaricapKucuk);
            UiKit.Boyut(satir.gameObject, 0f, SatirYuksekligi);

            var dizi = UiKit.YatayDizi(satir, 10f, 10);
            // ⚠ Varsayilan childForceExpandHeight butonlari satir boyuna gerer — sunger
            // butonlar arayuzun en cirkin parcasiydi (bkz. Hud SagAlt).
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;

            Color rozetRengi = Palet.UiYaziSolgun;
            string koyanMetni = "kaynağı bilinmiyor";
            if (s.Koyan != null && _durum.Oyuncular.TryGetValue(s.Koyan, out OyuncuOzeti koyan))
            {
                rozetRengi = _denge.OyuncuRengi(koyan.Renk);
                koyanMetni = s.Koyan == _durum.BenimId ? "sen bıraktın" : koyan.Ad + " bıraktı";
            }

            // Rozet HUD'daki 14'ten iri: depoda "kim koydu" satirin kimligi (B.10).
            UiKit.Rozet(satir, rozetRengi, 18f);

            var adKutu = UiKit.Panel(satir, "AdKutu", Color.clear);
            UiKit.Boyut(adKutu.gameObject, 240f, 38f).flexibleWidth = 1f;
            var adDizi = UiKit.DikeyDizi(adKutu, 0f, 0);
            adDizi.childAlignment = TextAnchor.MiddleLeft;

            var ad = UiKit.Yazi(
                adKutu, "Ad", _denge.EsyaAdi(s.EsyaId), UiKit.YaziGovde,
                Palet.UiYazi, TextAnchor.LowerLeft, true);
            UiKit.Boyut(ad.gameObject, 0f, 22f);

            // Rozet rengi tek basina hangi imzanin kim oldugunu ogretmiyor; ad da yazili.
            var koyanYazi = UiKit.Yazi(
                adKutu, "Koyan", koyanMetni, UiKit.YaziMini, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(koyanYazi.gameObject, 0f, 16f);

            var adet = UiKit.Yazi(
                satir, "Adet", s.Adet.ToString(), UiKit.YaziBaslik,
                Palet.UiVurgu, TextAnchor.MiddleRight, true);
            UiKit.Boyut(adet.gameObject, 80f, 30f);

            string esyaId = s.EsyaId;

            var al = UiKit.Buton(satir, "Al", baskasininKilidi ? "Kilitli" : "Al", () =>
            {
                if (!baskasininKilidi) DepodanAl?.Invoke(esyaId, s.Adet);
            });
            al.interactable = !baskasininKilidi;
            UiKit.Boyut(al.gameObject, 84f, 36f);
            al.GetComponentInChildren<Text>().fontSize = UiKit.YaziKucuk;

            var kilit = UiKit.Buton(satir, "Kilit", s.Kilitli ? "Aç" : "Kilitle",
                () => KilitDegistir?.Invoke(esyaId));
            kilit.interactable = !baskasininKilidi;
            UiKit.Boyut(kilit.gameObject, 96f, 36f);
            kilit.GetComponentInChildren<Text>().fontSize = UiKit.YaziKucuk;

            if (s.Kilitli)
            {
                // Kilit satirin ZEMININI degil cercevesini boyar: renkli dolgu altindaki
                // yaziyi bulandiriyordu (ayni ders Hud tohum slotunda alindi).
                var halka = UiKit.Cerceve(
                    satir, "KilitCerceve", benimKilidim ? Palet.UiVurgu : Palet.UiUyari,
                    UiKit.YaricapKucuk, 2);
                // Cerceve satirin tamamini kaplar; yerlesim onu bir sutun sanmamali.
                halka.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
        }
    }
}
