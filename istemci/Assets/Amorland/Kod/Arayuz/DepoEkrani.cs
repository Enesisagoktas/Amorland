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
    /// Rozet HUD'dakinden iri — kahve halka icinde renkli disk, yani bir "muhur" — ve adin
    /// altinda da yazili: "kim koydu" burada bir detay degil, satirin kimligidir.
    ///
    /// ⚠ Depo kisiti toplam MIKTARDIR, slot degil (B.10). Cubuk bunu gosterir; doluluk
    /// arttikca renk degisir. Dolu depo hasati engeller — bu SESSIZ kalamaz, o yuzden
    /// doluluk kendi cerceveli kartinda ekranin en ustunde durur.
    ///
    /// ⚠ Kabuk B.22 arayuz stil sozlesmesine uyar: modal govdesi KREM PARSOMEN oldugu icin
    /// buradaki her yazi KOYU'dur (<see cref="Palet.UiKoyuYazi"/>). Acik yazi krem zeminde
    /// gorunmez — bu ekranin en kolay bozulan yeri budur. Satirlar parsomen + kalin kahve
    /// cerceve, kilitli yigin ise cercevesinden ALTIN (benim) / KIRMIZI (partnerin) okunur.
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

        // ⚠ Panel govdesi cerceve kadar (6 px x 2) daralir, Icerik ayrica 16 px x 2 iceri
        // cekilir. 820 genislikte kartlar govdenin cercevesine biniyordu; 800 nefes birakir.
        private const float KartGenislik = PanelGenislik - 60f;
        private const float CubukGenislik = KartGenislik - 36f;

        /// <summary>Cubuk dolgusunun gercek genisligi: cerceve (3 px x 2) disarida kalir.</summary>
        private const float CubukIcGenislik = CubukGenislik - 6f;

        private const float OzetYukseklik = 92f;
        private const float SatirYuksekligi = 62f;

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
            // Doluluk kendi cerceveli kartinda ve golgeli: hasat engellenmesinin tek gorsel
            // uyarisi bu kart, listenin arasinda kaybolmamali. Govde parsomen oldugu icin
            // kart bir ton KOYU parsomen alir — ayni tonda kart zeminden ayrilmaz.
            RectTransform ozetIc;
            var ozetKart = UiKit.CerceveliKart(
                _panel.Icerik, "OzetKart", Palet.ParsomenKoyu, Palet.UiCerceve, out ozetIc, 20, 4f);
            UiKit.Sabitle(ozetKart, 0.5f, 1f, 0f, 0f, KartGenislik, OzetYukseklik);
            UiKit.GolgeEkle(ozetKart, 20, 14, 4f);

            // Ikon YER TUTUCU: gercek depo ikonu 2B varlik olarak sonra gelecek. Simdilik
            // HUD'daki depo kapsulu ile ayni sekli kullanir — iki gosterge ayni sey demeli.
            var ozetIkon = UiKit.Panel(ozetIc, "Ikon", Palet.UiCerceve);
            var ozetIkonImg = ozetIkon.gameObject.GetComponent<Image>();
            ozetIkonImg.sprite = Sekil.Elmas();
            ozetIkonImg.raycastTarget = false;
            UiKit.Sabitle(ozetIkon, 0f, 1f, 14f, -10f, 28f, 28f);

            _ozet = UiKit.Yazi(
                ozetIc, "Ozet", "", UiKit.YaziBaslik, Palet.UiKoyuYazi, TextAnchor.UpperLeft, true);
            UiKit.Sabitle(_ozet.rectTransform, 0f, 1f, 50f, -10f, 340f, 30f);

            _uyari = UiKit.Yazi(
                ozetIc, "Uyari", "", UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun, TextAnchor.UpperRight, true);
            UiKit.Sabitle(_uyari.rectTransform, 1f, 1f, -16f, -12f, 380f, 26f);

            // Cubuk zemini CUKUR parsomen: dolgunun nerede bittigi ancak boyle okunuyor.
            // Cerceve cubuga da gerekli — cercevesiz cubuk parsomen uzerinde bir leke gibi durur.
            RectTransform cubukIc;
            var cubuk = UiKit.CerceveliKart(
                ozetIc, "Doluluk", Palet.UiKagitCukur, Palet.UiCerceve, out cubukIc, 10, 3f);
            UiKit.Sabitle(cubuk, 0f, 1f, 14f, -48f, CubukGenislik, 20f);

            // ⚠ Dolgu genisligi kodla surulur (pivot 0, anchorMax.x = 0), o yuzden
            // yerlesim grubuna degil ciplak rect'e dayanir.
            var dolgu = UiKit.Kart(cubukIc, "Dolgu", Palet.UiYesil, 7);
            dolgu.anchorMin = Vector2.zero;
            dolgu.anchorMax = new Vector2(0f, 1f);
            dolgu.pivot = new Vector2(0f, 0.5f);
            dolgu.offsetMin = Vector2.zero;
            dolgu.offsetMax = Vector2.zero;
            _dolulukDolgu = dolgu.gameObject.GetComponent<Image>();
            _dolulukDolgu.raycastTarget = false;

            _liste = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            var disRt = _liste.parent.parent as RectTransform;
            if (disRt != null)
            {
                disRt.anchorMin = Vector2.zero;
                disRt.anchorMax = Vector2.one;
                // Ust: ozet karti (92) + nefes payi. Alt: alt serit (52) + nefes payi.
                disRt.offsetMin = new Vector2(0f, 66f);
                disRt.offsetMax = new Vector2(0f, -102f);
            }

            var altBar = UiKit.Panel(_panel.Icerik, "AltBar", Color.clear);
            UiKit.Sabitle(altBar, 0.5f, 0f, 0f, 0f, KartGenislik, 52f);
            var altDizi = UiKit.YatayDizi(altBar, 12f, 0);
            altDizi.childForceExpandHeight = false;
            altDizi.childAlignment = TextAnchor.MiddleLeft;

            // ⚠ "Depoya bosalt" tek butonu ZORUNLU (B.10): bu buton olmadan oyun mobilde
            // iskence. Ekranin ana eylemi bu — ekrandaki TEK yesil buton odur; satir
            // butonlari ahsap kalir ki goz once buraya dussun.
            var bosalt = UiKit.YesilButon(altBar, "Bosalt", "Çantayı depoya boşalt",
                () => DepoyaBosalt?.Invoke());
            UiKit.Boyut(bosalt.gameObject, 288f, 50f);

            var not = UiKit.Yazi(altBar, "Not",
                "Kilitli yığını partner alamaz · en fazla 3 yığın",
                UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleRight);
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
                _ozet.color = Palet.UiKoyuYaziSolgun;
                _uyari.text = "";
                UiKit.CubukAyarla(_dolulukDolgu, 0f, CubukIcGenislik);
                return;
            }

            float oran = vadi.DepoDolu / (float)vadi.DepoKapasitesi;
            _ozet.text = vadi.DepoDolu + " / " + vadi.DepoKapasitesi + " birim";
            _ozet.color = oran >= 1f ? Palet.UiKirmiziKoyu : Palet.UiKoyuYazi;
            UiKit.CubukAyarla(_dolulukDolgu, oran, CubukIcGenislik);

            // Kritik doluluk uc kademede okunur: renk, yazi ve cubuk ayni anda doner.
            // ⚠ Cubuk DOLGUSU doygun renk alir (uzak mesafeden okunan sey odur), yazi ise
            // ayni ailenin KOYU tonunu — krem zeminde acik altin/kirmizi yazi silinir.
            if (oran >= 1f)
            {
                _dolulukDolgu.color = Palet.UiKirmizi;
                _uyari.color = Palet.UiKirmiziKoyu;
                _uyari.text = "DOLU · hasat engellenir, ürün tarlada bekler";
            }
            else if (oran > 0.85f)
            {
                _dolulukDolgu.color = Palet.UiAltin;
                _uyari.color = Palet.UiAltinKoyu;
                _uyari.text = "dolmak üzere · %" + Mathf.RoundToInt(oran * 100f);
            }
            else
            {
                _dolulukDolgu.color = Palet.UiYesil;
                _uyari.color = Palet.UiKoyuYaziSolgun;
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

        /// <summary>
        /// Kategori ikonu — YER TUTUCU. Gercek esya ikonlari 2B varlik olarak sonra gelecek;
        /// simdilik <see cref="Sekil"/> poligonlari yigini kategorisinden ayirmaya yetiyor.
        /// </summary>
        private static Sprite KategoriIkonu(string kategori)
        {
            switch (kategori)
            {
                case "tohum": return Sekil.Damla();
                case "urun": return Sekil.Yildiz();
                case "cevher": return Sekil.Elmas();
                case "islenmis": return Sekil.Simsek();
                default: return Sekil.Daire();
            }
        }

        /// <summary>Ikon rengi de kategoriyi tasir: sekil kucukken once renk okunuyor.</summary>
        private static Color KategoriRengi(string kategori)
        {
            switch (kategori)
            {
                case "tohum": return Palet.Yaprak;
                case "urun": return Palet.Bugday;
                case "cevher": return Palet.Su;
                case "odun": return Palet.Ahsap;
                case "islenmis": return Palet.Cati;
                default: return Palet.UiKoyuYaziSolgun;
            }
        }

        /// <summary>Bos depo, bos kutu gibi gorunmemeli: ne yapilacagini soyleyen tek satir.</summary>
        private void BosDurumEkle()
        {
            // Cukur yuzey = bos slot dili (B.22). Bos depo boyle "henuz doldurulmamis bir
            // sandik" gibi durur; ciplak yazi sadece eksiklik gibi duruyordu.
            RectTransform ic;
            var kart = UiKit.CerceveliKart(
                _liste, "Bos", Palet.UiKagitCukur, Palet.UiCerceve, out ic, 14, 3f);
            UiKit.Boyut(kart.gameObject, 0f, 104f);

            var yazi = UiKit.Yazi(
                ic, "Yazi", "Depo boş. Çantandaki fazlalığı buraya bırak — partnerin de alabilir.",
                UiKit.YaziGovde, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleCenter);
            UiKit.Doldur(yazi.rectTransform, 18f);
        }

        private void SatirEkle(DepoSatiri s)
        {
            bool benimKilidim = s.Kilitli && s.Kilitleyen == _durum.BenimId;
            bool baskasininKilidi = s.Kilitli && !benimKilidim;

            // ⚠ Kilit satirin ZEMININI degil CERCEVESINI boyar: renkli dolgu altindaki
            // yaziyi bulandiriyordu (ayni ders Hud tohum slotunda alindi). Altin halka
            // "benim kilidim", kirmizi halka "partnerin kilidi, dokunamazsin" demektir.
            Color cerceveRengi = benimKilidim ? Palet.KapsulAltin
                : baskasininKilidi ? Palet.UiKirmizi
                : Palet.UiCerceve;
            // Kilitli satirin cercevesi kalinlasir: renk korlugunde de fark edilsin.
            float kalinlik = s.Kilitli ? 4f : 3f;
            // Baskasinin kilidi = dokunulamaz yigin. Cukur yuzey bunu tiklamadan once soyler.
            Color dolguRengi = baskasininKilidi ? Palet.UiKagitCukur : Palet.ParsomenKoyu;

            RectTransform ic;
            var satir = UiKit.CerceveliKart(
                _liste, "Satir_" + s.EsyaId, dolguRengi, cerceveRengi, out ic, 14, kalinlik);
            UiKit.Boyut(satir.gameObject, 0f, SatirYuksekligi);

            // ⚠ Yerlesim IC yuzeye kurulur; dis cerceveye kurulsa icerik cercevenin
            // uzerine tasar ve halkanin okunurlugu biterdi.
            var dizi = UiKit.YatayDizi(ic, 10f, 8);
            // ⚠ Varsayilan childForceExpandHeight butonlari satir boyuna gerer — sunger
            // butonlar arayuzun en cirkin parcasiydi (bkz. Hud SagAlt).
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;

            string kategori = _denge.EsyaKategorisi(s.EsyaId);
            var ikon = UiKit.Panel(ic, "Ikon", KategoriRengi(kategori));
            var ikonImg = ikon.gameObject.GetComponent<Image>();
            ikonImg.sprite = KategoriIkonu(kategori);
            ikonImg.raycastTarget = false;
            UiKit.Boyut(ikon.gameObject, 30f, 30f);

            Color rozetRengi = Palet.UiKoyuYaziSolgun;
            string koyanMetni = "kaynağı bilinmiyor";
            if (s.Koyan != null && _durum.Oyuncular.TryGetValue(s.Koyan, out OyuncuOzeti koyan))
            {
                rozetRengi = _denge.OyuncuRengi(koyan.Renk);
                koyanMetni = s.Koyan == _durum.BenimId ? "sen bıraktın" : koyan.Ad + " bıraktı";
            }

            KoyanRozeti(ic, rozetRengi);

            var adKutu = UiKit.Panel(ic, "AdKutu", Color.clear);
            UiKit.Boyut(adKutu.gameObject, 220f, 38f).flexibleWidth = 1f;
            var adDizi = UiKit.DikeyDizi(adKutu, 0f, 0);
            adDizi.childAlignment = TextAnchor.MiddleLeft;

            var ad = UiKit.Yazi(
                adKutu, "Ad", _denge.EsyaAdi(s.EsyaId), UiKit.YaziGovde,
                Palet.UiKoyuYazi, TextAnchor.LowerLeft, true);
            UiKit.Boyut(ad.gameObject, 0f, 22f);

            // Rozet rengi tek basina hangi imzanin kim oldugunu ogretmiyor; ad da yazili.
            var koyanYazi = UiKit.Yazi(
                adKutu, "Koyan", koyanMetni, UiKit.YaziMini, Palet.UiKoyuYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(koyanYazi.gameObject, 0f, 16f);

            // Adet krem zeminde KOYU altin: acik altin (UiAltin) parsomen uzerinde erir.
            var adet = UiKit.Yazi(
                ic, "Adet", s.Adet.ToString(), UiKit.YaziBaslik,
                Palet.UiAltinKoyu, TextAnchor.MiddleRight, true);
            UiKit.Boyut(adet.gameObject, 74f, 30f);

            string esyaId = s.EsyaId;

            // Satir eylemleri IKINCIL: ahsap. Ekranin tek yesili alttaki "bosalt" butonu.
            var al = UiKit.AhsapButon(ic, "Al", baskasininKilidi ? "Kilitli" : "Al", () =>
            {
                if (!baskasininKilidi) DepodanAl?.Invoke(esyaId, s.Adet);
            });
            al.interactable = !baskasininKilidi;
            UiKit.Boyut(al.gameObject, 84f, 42f);
            al.GetComponentInChildren<Text>().fontSize = UiKit.YaziKucuk;

            var kilit = UiKit.AhsapButon(ic, "Kilit", s.Kilitli ? "Aç" : "Kilitle",
                () => KilitDegistir?.Invoke(esyaId));
            kilit.interactable = !baskasininKilidi;
            UiKit.Boyut(kilit.gameObject, 96f, 42f);
            kilit.GetComponentInChildren<Text>().fontSize = UiKit.YaziKucuk;
        }

        /// <summary>
        /// "Kim koydu" muhru: kahve halka icinde oyuncunun rengi. Ciplak renkli disk
        /// parsomen uzerinde bir leke gibi duruyordu; halka onu bir jetona cevirir ve
        /// depodaki en onemli ikinci bilgiyi (B.10) satirin basinda tutar.
        /// </summary>
        private static void KoyanRozeti(Transform ebeveyn, Color renk)
        {
            var halka = UiKit.Panel(ebeveyn, "KoyanRozet", Palet.UiCerceve);
            var halkaImg = halka.gameObject.GetComponent<Image>();
            halkaImg.sprite = Sekil.Daire();
            halkaImg.raycastTarget = false;
            UiKit.Boyut(halka.gameObject, 26f, 26f);

            var disk = UiKit.Panel(halka, "Renk", renk);
            var diskImg = disk.gameObject.GetComponent<Image>();
            diskImg.sprite = Sekil.Daire();
            diskImg.raycastTarget = false;
            UiKit.Doldur(disk, 3f);
        }
    }
}
