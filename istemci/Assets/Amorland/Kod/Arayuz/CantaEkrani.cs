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
    ///
    /// ⚠ Kabuk B.22 arayuz stil sozlesmesine uyar: <see cref="Panel"/> govdesi KREM
    /// PARSOMEN oldugu icin buradaki her yazi KOYU olmak zorunda. Acik yazi krem
    /// zeminde gorunmez — bu ekranin en sik kirilan kurali budur.
    /// </summary>
    public class CantaEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;
        private RectTransform _liste;
        private Text _ozet;
        private Text _secimYazi;

        // Ust serit ve alt bar sabit; liste aradaki bosluk kadar. Sayilari tek yerde
        // tutmak, ikisinden birini degistirince listenin altina/ustune binmesini onler.
        private const float SeritYukseklik = 40f;
        private const float AltBarYukseklik = 48f;

        // Satir yuksekligi = cerceve (2x3) + kenar bosluk (2x8) + ic oge yuksekligi.
        // Fazlasi satiri sunger gibi sisirir ve liste birkac satirda tukenir.
        // ⚠ Cerceveli karta gecince 52 px yetmedi: 3 px'lik kahve hat iki yandan
        // yiyor ve rozet ile buton ic alana sigmiyordu.
        private const float SatirYukseklik = 64f;
        private const float SatirIcYukseklik = 34f;

        /// <summary>Kategori ve adet rozetlerinin capi. Ikisi ayni boy — satir simetrik durur.</summary>
        private const float RozetBoyut = 36f;

        /// <summary>Dokun-sec ile secilen esya. Ikinci dokunus hedefi belirler.</summary>
        public string SecilenEsya { get; private set; }

        public event Action<string> DepoyaGonder;
        public event Action<string> TohumSec;

        public CantaEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "CantaEkrani", "Çanta", () => SecilenEsya = null);
            Kur();
            _durum.DurumDegisti += _ => { if (_panel.Acik) Tazele(); };
        }

        private void Kur()
        {
            // Doluluk ve secim ipucu ayni ice cokuk seritte durur: ikisi de listeye ait
            // degil, listenin DURUMUNU anlatir. Ayri zemin bu farki yaziya gerek kalmadan soyler.
            // ⚠ Parsomen govdenin uzerinde "cukur" KAGIT CUKUR tonuyla anlatilir (B.22);
            // koyu yuzey burada krem levhada delik gibi durur.
            RectTransform seritIc;
            var serit = UiKit.CerceveliKart(
                _panel.Icerik, "Serit", Palet.UiKagitCukur, Palet.UiCerceve, out seritIc, 14, 3f);
            serit.anchorMin = new Vector2(0f, 1f);
            serit.anchorMax = new Vector2(1f, 1f);
            serit.pivot = new Vector2(0.5f, 1f);
            serit.anchoredPosition = Vector2.zero;
            serit.sizeDelta = new Vector2(0f, SeritYukseklik);

            // Sayacin ikonu. ⚠ YER TUTUCU: gercek canta ikonu 2B varlik olarak sonra gelecek.
            var seritIkon = UiKit.Panel(seritIc, "Ikon", Palet.UiCerceve).gameObject.GetComponent<Image>();
            seritIkon.sprite = Sekil.Elmas();
            seritIkon.raycastTarget = false;
            UiKit.Sabitle(seritIkon.rectTransform, 0f, 0.5f, 12f, 0f, 20f, 20f);

            _ozet = UiKit.Yazi(
                seritIc, "Ozet", "", UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleLeft, true);
            var ozetRt = _ozet.rectTransform;
            ozetRt.anchorMin = new Vector2(0f, 0f);
            ozetRt.anchorMax = new Vector2(0.5f, 1f);
            ozetRt.offsetMin = new Vector2(40f, 0f);
            ozetRt.offsetMax = Vector2.zero;

            // Secim ipucu altin ama KOYU altin: acik altin (UiVurgu) krem seritte erir.
            _secimYazi = UiKit.Yazi(
                seritIc, "Secim", "", UiKit.YaziKucuk, Palet.UiAltinKoyu, TextAnchor.MiddleRight, true);
            var secimRt = _secimYazi.rectTransform;
            secimRt.anchorMin = new Vector2(0.38f, 0f);
            secimRt.anchorMax = new Vector2(1f, 1f);
            secimRt.offsetMin = Vector2.zero;
            secimRt.offsetMax = new Vector2(-14f, 0f);

            var listeKok = UiKit.KaydirmaListesi(_panel.Icerik, "Liste", out _);
            _liste = listeKok;
            var disRt = listeKok.parent.parent as RectTransform;
            if (disRt != null)
            {
                disRt.anchorMin = Vector2.zero;
                disRt.anchorMax = Vector2.one;
                disRt.offsetMin = new Vector2(0f, AltBarYukseklik + 10f);
                disRt.offsetMax = new Vector2(0f, -(SeritYukseklik + 10f));
            }

            // Alt bar gorunmez kapsayici: butonlar zaten yuvarlak koseli, altlarina
            // ikinci bir zemin koymak modali katman katman agirlastiriyordu.
            var altBar = UiKit.Panel(_panel.Icerik, "AltBar", Color.clear);
            altBar.anchorMin = new Vector2(0f, 0f);
            altBar.anchorMax = new Vector2(1f, 0f);
            altBar.pivot = new Vector2(0.5f, 0f);
            altBar.anchoredPosition = Vector2.zero;
            altBar.sizeDelta = new Vector2(0f, AltBarYukseklik);

            var altDizi = UiKit.YatayDizi(altBar, 12f, 0);
            // Gerilmis buton, bar yuksekligi kadar sisip yaziyi ortada kaybediyor.
            altDizi.childForceExpandHeight = false;
            altDizi.childAlignment = TextAnchor.MiddleLeft;

            // Ekranin TEK yesil butonu bu: toplu bosaltma cantanin ana eylemi (B.22).
            // Satirlardaki tekil eylemler ahsap kalir, yoksa yesil anlamini yitirir.
            var bosalt = UiKit.YesilButon(
                altBar, "DepoyaBosalt", "Depoya boşalt", () => DepoyaGonder?.Invoke(null));
            UiKit.Boyut(bosalt.gameObject, 210f, 46f);

            // ⚠ "Depodan al" TOPLU butonu YOKTUR (B.10). Kolaylik tek yonludur:
            // kazara bosaltma geri alinabilir, kazara toplu alma depoyu bosaltir.
            var not = UiKit.Yazi(altBar, "Not", "Depodan toplu alma yok — tek tek seçilir.",
                UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleLeft);
            UiKit.Boyut(not.gameObject, 300f, 42f).flexibleWidth = 1f;
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
            if (ben == null)
            {
                EkranParcasi.BosDurum(_liste, "Çanta sunucudan geliyor...");
                return;
            }

            int yigin = _denge.YiginBoyutu;
            int kullanilan = 0;
            var satirlar = new List<EnvanterSatiri>(_durum.BenimEnvanterim);
            foreach (var e in satirlar) kullanilan += Mathf.CeilToInt(e.Adet / (float)yigin);
            if (satirlar.Count == 0)
            {
                EkranParcasi.BosDurum(_liste, "Çantan boş — tarladan ürün topla.");
            }

            _ozet.text = kullanilan + " / " + ben.CantaSlot + " slot   (yığın " + yigin + ")";
            // Dolu canta uyarisi KOYU kirmizi: acik kirmizi krem seritte solgun kaliyor
            // ve "dikkat" tonunu kaybediyordu.
            _ozet.color = kullanilan >= ben.CantaSlot ? Palet.UiKirmiziKoyu : Palet.UiKoyuYaziSolgun;
            _secimYazi.text = SecilenEsya != null
                ? "Seçili: " + _denge.EsyaAdi(SecilenEsya)
                : "";

            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }

            // Bos liste sessiz kalmamali: oyuncu ekranin yuklenmedigini sanmasin.
            if (satirlar.Count == 0)
            {
                // Bos durum da cukur bir kart: ciplak yazi parsomen govdede "yuklenmemis"
                // gibi duruyordu, cukur yuzey ise "burasi bos" der.
                RectTransform bosIc;
                var bosKart = UiKit.CerceveliKart(
                    _liste, "Bos", Palet.UiKagitCukur, Palet.UiCerceve, out bosIc, 14, 3f);
                UiKit.Boyut(bosKart.gameObject, 0f, 72f);
                var bos = UiKit.Yazi(
                    bosIc, "Yazi", "Çanta boş.", UiKit.YaziGovde, Palet.UiKoyuYaziSolgun,
                    TextAnchor.MiddleCenter);
                UiKit.Doldur(bos.rectTransform);
                return;
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

        /// <summary>
        /// Rozet rengi. Liste uzadikca kategori yazisini okumak yavaslar; renk noktasi
        /// esyayi ad okumadan once ayirt ettirir.
        /// </summary>
        private static Color KategoriRengi(string kategori)
        {
            switch (kategori)
            {
                case "tohum": return Palet.Sap;
                case "urun": return Palet.Yaprak;
                case "cevher": return Palet.Tas;
                case "odun": return Palet.Ahsap;
                // ⚠ Bugday degil KOYU ALTIN: rozetin icindeki krem sekil acik bugday
                // zeminde kayboluyordu. Ton ayni aileden, okunurluk kazaniliyor.
                case "islenmis": return Palet.UiAltinKoyu;
                case "sarf": return Palet.Su;
                case "alet": return Palet.TasKoyu;
                // ⚠ Bilinmeyen kategori de GORUNUR kalmali: acik gri rozet krem
                // satirda tamamen kayboluyordu.
                default: return Palet.UiKoyuYaziSolgun;
            }
        }

        /// <summary>
        /// Kategori simgesi — rozetin icindeki sekil. Renk tek basina yetmiyor:
        /// yan yana iki yesil ton (tohum/urun) goz taramasinda ayirt edilmiyor.
        ///
        /// ⚠ YER TUTUCU: gercek esya ikonlari 2B varlik olarak sonra gelecek.
        /// </summary>
        private static Sprite KategoriSekli(string kategori)
        {
            switch (kategori)
            {
                case "tohum": return Sekil.Damla();
                case "urun": return Sekil.Daire();
                case "cevher": return Sekil.Elmas();
                // Yuvarlak kose = kutuk/tahta; tek dikdortgen sekil bu ailede o.
                case "odun": return Sekil.Yuvarlak(8);
                case "islenmis": return Sekil.Yildiz();
                case "sarf": return Sekil.Simsek();
                case "alet": return Sekil.Arti();
                default: return Sekil.Daire();
            }
        }

        private void SatirEkle(EnvanterSatiri e)
        {
            string esyaId = e.EsyaId;
            string kategori = _denge.EsyaKategorisi(esyaId);
            bool secili = esyaId == SecilenEsya;

            // Satir = kahve cerceveli parsomen kart. Secim DOLU KATMANLA degil, dis
            // cercevenin altina donmesi ve kalinlasmasiyla anlatilir (B.22): dolu vurgu
            // satirin yazisini bulandiriyordu, altin cerceve ise Hud tohum slotuyla
            // ayni dili konusuyor.
            RectTransform ic;
            var satir = UiKit.CerceveliKart(
                _liste, "Satir_" + esyaId, Palet.ParsomenKoyu,
                secili ? Palet.KapsulAltin : Palet.UiCerceve, out ic, 14, secili ? 5f : 3f);
            UiKit.Boyut(satir.gameObject, 0f, SatirYukseklik);

            var disImg = satir.gameObject.GetComponent<Image>();
            var buton = satir.gameObject.AddComponent<Button>();
            buton.targetGraphic = disImg;
            buton.onClick.AddListener(() => Sec(esyaId));

            // ⚠ Yatay dizi IC yuzeye kurulur. Dis cerceveye kurulursa ogeler kahve
            // hattin uzerine tasar ve cerceve kesilmis gorunur.
            var dizi = UiKit.YatayDizi(ic, 10f, 8);
            // ⚠ Gerilme kapali: acik kalirsa rozetler satir yuksekligine cekilip elips olur.
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;

            // Kategori rozeti: kahve halka + kategori renginde daire + icinde sekil.
            // Halka olmadan renkli daire krem satirda "boya lekesi" gibi duruyordu.
            var rozet = UiKit.Rozet(ic, Palet.UiCerceve, RozetBoyut);
            var rozetIc = UiKit.Panel(rozet.rectTransform, "Ic", KategoriRengi(kategori))
                .gameObject.GetComponent<Image>();
            rozetIc.sprite = Sekil.Daire();
            rozetIc.raycastTarget = false;
            UiKit.Doldur(rozetIc.rectTransform, 3f);

            var ikon = UiKit.Panel(rozetIc.rectTransform, "Ikon", Palet.Parsomen)
                .gameObject.GetComponent<Image>();
            ikon.sprite = KategoriSekli(kategori);
            ikon.raycastTarget = false;
            UiKit.Doldur(ikon.rectTransform, 7f);

            var ad = UiKit.Yazi(
                ic, "Ad", _denge.EsyaAdi(esyaId), UiKit.YaziGovde, Palet.UiKoyuYazi,
                TextAnchor.MiddleLeft, true);
            UiKit.Boyut(ad.gameObject, 240f, SatirIcYukseklik).flexibleWidth = 1f;

            var kat = UiKit.Yazi(
                ic, "Kategori", kategori, UiKit.YaziMini, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleLeft);
            UiKit.Boyut(kat.gameObject, 96f, SatirIcYukseklik);

            // Adet satirin tek sayisal bilgisi: KOYU DAIRE rozetinde, beyaz yaziyla.
            // Krem satirdaki tek koyu leke odur ve goz taramasinda onu one alir —
            // Hud'daki adet rozetiyle ayni dil.
            var adetRozet = UiKit.Rozet(ic, Palet.KapsulZemin, RozetBoyut);
            var adet = UiKit.Yazi(
                adetRozet.rectTransform, "Adet", e.Adet.ToString(), UiKit.YaziKucuk, Color.white,
                TextAnchor.MiddleCenter, true);
            UiKit.Doldur(adet.rectTransform);
            // ⚠ Uc haneli adet daireye sigmaz; sarma satiri ikiye bolup rozetten tasar,
            // tasma ise sayiyi tek satirda ortali tutar.
            adet.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Satir eylemi AHSAP: yesil ekranda tek olmali ve o alt bardaki toplu
            // bosaltmadir (B.22).
            var eylem = kategori == "tohum"
                ? UiKit.AhsapButon(ic, "Sec", "Çubuğa al", () => TohumSec?.Invoke(esyaId))
                : UiKit.AhsapButon(ic, "Depoya", "Depoya", () => DepoyaGonder?.Invoke(esyaId));
            UiKit.Boyut(eylem.gameObject, 118f, 40f);
            eylem.GetComponentInChildren<Text>().fontSize = UiKit.YaziKucuk;
        }

        /// <summary>Dokun-sec: ilk dokunus secer, ikincisi ayni esyaya dokunursa secimi birakir.</summary>
        private void Sec(string esyaId)
        {
            SecilenEsya = SecilenEsya == esyaId ? null : esyaId;
            Tazele();
        }
    }
}
