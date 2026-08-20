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

        // Ust serit ve alt bar sabit; liste aradaki bosluk kadar. Sayilari tek yerde
        // tutmak, ikisinden birini degistirince listenin altina/ustune binmesini onler.
        private const float SeritYukseklik = 40f;
        private const float AltBarYukseklik = 48f;

        // Satir yuksekligi = ic oge yuksekligi + kenar bosluk (2x8). Fazlasi satiri
        // sunger gibi sisirir ve liste birkac satirda tukenir.
        private const float SatirYukseklik = 52f;
        private const float SatirIcYukseklik = 32f;

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
            var serit = UiKit.Kart(_panel.Icerik, "Serit", Palet.UiYuzeyKoyu, UiKit.YaricapKucuk);
            serit.anchorMin = new Vector2(0f, 1f);
            serit.anchorMax = new Vector2(1f, 1f);
            serit.pivot = new Vector2(0.5f, 1f);
            serit.anchoredPosition = Vector2.zero;
            serit.sizeDelta = new Vector2(0f, SeritYukseklik);

            _ozet = UiKit.Yazi(
                serit, "Ozet", "", UiKit.YaziKucuk, Palet.UiYaziSolgun, TextAnchor.MiddleLeft, true);
            var ozetRt = _ozet.rectTransform;
            ozetRt.anchorMin = new Vector2(0f, 0f);
            ozetRt.anchorMax = new Vector2(0.5f, 1f);
            ozetRt.offsetMin = new Vector2(14f, 0f);
            ozetRt.offsetMax = Vector2.zero;

            _secimYazi = UiKit.Yazi(
                serit, "Secim", "", UiKit.YaziKucuk, Palet.UiVurgu, TextAnchor.MiddleRight, true);
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

            var bosalt = UiKit.AnaButon(
                altBar, "DepoyaBosalt", "Depoya boşalt", () => DepoyaGonder?.Invoke(null));
            UiKit.Boyut(bosalt.gameObject, 200f, 42f);

            // ⚠ "Depodan al" TOPLU butonu YOKTUR (B.10). Kolaylik tek yonludur:
            // kazara bosaltma geri alinabilir, kazara toplu alma depoyu bosaltir.
            var not = UiKit.Yazi(altBar, "Not", "Depodan toplu alma yok — tek tek seçilir.",
                UiKit.YaziKucuk, Palet.UiYaziSolgun, TextAnchor.MiddleLeft);
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
            if (ben == null) return;

            int yigin = _denge.YiginBoyutu;
            int kullanilan = 0;
            var satirlar = new List<EnvanterSatiri>(_durum.BenimEnvanterim);
            foreach (var e in satirlar) kullanilan += Mathf.CeilToInt(e.Adet / (float)yigin);

            _ozet.text = kullanilan + " / " + ben.CantaSlot + " slot   (yığın " + yigin + ")";
            _ozet.color = kullanilan >= ben.CantaSlot ? Palet.UiUyari : Palet.UiYaziSolgun;
            _secimYazi.text = SecilenEsya != null
                ? "Seçili: " + _denge.EsyaAdi(SecilenEsya) + " — hedefe dokun"
                : "";

            for (int i = _liste.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_liste.GetChild(i).gameObject);
            }

            // Bos liste sessiz kalmamali: oyuncu ekranin yuklenmedigini sanmasin.
            if (satirlar.Count == 0)
            {
                var bos = UiKit.Yazi(
                    _liste, "Bos", "Çanta boş.", UiKit.YaziKucuk, Palet.UiYaziSolgun, TextAnchor.MiddleCenter);
                UiKit.Boyut(bos.gameObject, 0f, 60f);
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
                case "islenmis": return Palet.Bugday;
                case "sarf": return Palet.Su;
                case "alet": return Palet.TasKoyu;
                default: return Palet.UiYaziSolgun;
            }
        }

        private void SatirEkle(EnvanterSatiri e)
        {
            string esyaId = e.EsyaId;
            string kategori = _denge.EsyaKategorisi(esyaId);

            var satir = UiKit.Kart(_liste, "Satir_" + esyaId, Palet.UiYuzey, UiKit.YaricapKucuk);
            UiKit.Boyut(satir.gameObject, 0f, SatirYukseklik);

            var dizi = UiKit.YatayDizi(satir, 10f, 8);
            // ⚠ Gerilme kapali: acik kalirsa rozet satir yuksekligine cekilip elips olur.
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;

            var buton = satir.gameObject.AddComponent<Button>();
            buton.targetGraphic = satir.gameObject.GetComponent<Image>();
            buton.onClick.AddListener(() => Sec(esyaId));

            UiKit.Rozet(satir, KategoriRengi(kategori), 16f);

            var ad = UiKit.Yazi(
                satir, "Ad", _denge.EsyaAdi(esyaId), UiKit.YaziGovde, Palet.UiYazi, TextAnchor.MiddleLeft, true);
            UiKit.Boyut(ad.gameObject, 240f, SatirIcYukseklik).flexibleWidth = 1f;

            var kat = UiKit.Yazi(
                satir, "Kategori", kategori, UiKit.YaziMini, Palet.UiYaziSolgun, TextAnchor.MiddleLeft);
            UiKit.Boyut(kat.gameObject, 96f, SatirIcYukseklik);

            // Adet satirin tek sayisal bilgisi; vurgu rengi ve kalinlik onu goz taramasinda one alir.
            var adet = UiKit.Yazi(
                satir, "Adet", e.Adet.ToString(), UiKit.YaziGovde, Palet.UiVurgu, TextAnchor.MiddleRight, true);
            UiKit.Boyut(adet.gameObject, 72f, SatirIcYukseklik);

            var eylem = kategori == "tohum"
                ? UiKit.Buton(satir, "Sec", "Çubuğa al", () => TohumSec?.Invoke(esyaId))
                : UiKit.Buton(satir, "Depoya", "Depoya", () => DepoyaGonder?.Invoke(esyaId));
            UiKit.Boyut(eylem.gameObject, 118f, 34f);
            eylem.GetComponentInChildren<Text>().fontSize = UiKit.YaziKucuk;

            if (esyaId == SecilenEsya)
            {
                // Dolu bir vurgu katmani satirin yazisini bulandiriyordu; ince halka
                // secimi Hud tohum slotlariyla ayni dilde anlatir.
                var cerceve = UiKit.Cerceve(satir, "Cerceve", Palet.UiVurgu, UiKit.YaricapKucuk, 3);
                // Halka satirin tamamini sarar, yatay dizinin bir ogesi degildir.
                cerceve.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
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
