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
    /// <summary>
    /// Bes ekranin ortak gorsel parcalari.
    ///
    /// ⚠ <see cref="Panel"/> govdesi KREM PARSOMEN'dir (B.22). Bu dosyadaki her sey o
    /// zemini varsayar: yazi KOYU, yuzeyler kagit tonlarinda, cerceve kahve. UiKit'in
    /// bazi varsayilanlari hala KOYU panel icin kuruludur (acik yazi, acik altin imlec,
    /// beyaz ayirac cizgisi) — krem uzerinde hepsi gorunmez kalir. Asagidaki
    /// yardimcilar o varsayilanlari cevirir; ekranlar tek tek ugrasmasin.
    /// </summary>
    internal static class EkranParcasi
    {
        /// <summary>
        /// Liste satiri / ayar satiri kabugu: kagit yuzey + kalin kahve cerceve.
        ///
        /// ⚠ Icerik <paramref name="ic"/> yuzeyine eklenir; dis cerceveye eklenirse
        /// cercevenin uzerine tasar (<see cref="UiKit.CerceveliKart"/> sozlesmesi).
        /// Verilen yukseklik DIS olcudur: ic yuzey 2×kalinlik kadar alcaktir.
        /// </summary>
        internal static RectTransform Satir(
            Transform ebeveyn, string ad, Color dolgu, float yukseklik, out RectTransform ic,
            int yaricap = 14, float kalinlik = 3f)
        {
            var dis = UiKit.CerceveliKart(ebeveyn, ad, dolgu, Palet.UiCerceve, out ic, yaricap, kalinlik);
            UiKit.Boyut(dis.gameObject, 0f, yukseklik);
            return dis;
        }

        /// <summary>
        /// Satir basi ikonu.
        ///
        /// ⚠ Buradaki sekiller YER TUTUCUDUR: gercek ikonlar 2B varlik olarak sonra
        /// gelecek. Yine de simdiden konuyorlar, cunku ikonsuz liste satiri "ayar
        /// menusu" gibi duruyor — ikonlu satir oyuna ait hissettiriyor.
        /// </summary>
        internal static Image Ikon(Transform ebeveyn, string ad, Sprite sekil, Color renk, float boyut = 26f)
        {
            var rt = UiKit.Panel(ebeveyn, ad, renk);
            var img = rt.gameObject.GetComponent<Image>();
            img.sprite = sekil;
            img.raycastTarget = false;
            UiKit.Boyut(rt.gameObject, boyut, boyut);
            return img;
        }

        /// <summary>
        /// Ilerleme cubugu: cerceveli cukur zemin + icinde soldan buyuyen altin dolgu.
        /// Dolgu genisligi <see cref="UiKit.CubukAyarla"/> ile verilir; tam genislik
        /// olarak DIS genislik eksi 2×kalinlik gecilmeli.
        /// </summary>
        internal static RectTransform Cubuk(
            Transform ebeveyn, string ad, float genislik, float yukseklik, out Image dolgu)
        {
            RectTransform ic;
            var dis = UiKit.CerceveliKart(
                ebeveyn, ad, Palet.UiKagitCukur, Palet.UiCerceve, out ic, 8, 2f);
            UiKit.Boyut(dis.gameObject, genislik, yukseklik);

            var dolguRt = UiKit.Kart(ic, "Dolgu", Palet.UiAltin, 6);
            dolguRt.anchorMin = Vector2.zero;
            dolguRt.anchorMax = new Vector2(0f, 1f);
            dolguRt.pivot = new Vector2(0f, 0.5f);
            dolguRt.offsetMin = Vector2.zero;
            dolguRt.offsetMax = Vector2.zero;
            dolgu = dolguRt.gameObject.GetComponent<Image>();
            dolgu.raycastTarget = false;
            return dis;
        }

        /// <summary>
        /// Satiri yatay dizide esneyen bir sutuna cevirir.
        ///
        /// ⚠ Ikinci bir <c>LayoutElement</c> EKLEMEZ, var olani duzenler: ayni nesnede
        /// iki LayoutElement bulunursa Unity degerleri sessizce harmanlar ve olcu
        /// nereden geldigi anlasilmaz olur.
        /// </summary>
        internal static void Esnet(GameObject go, float genislik)
        {
            var el = go.GetComponent<LayoutElement>();
            if (el == null) el = go.AddComponent<LayoutElement>();
            el.preferredWidth = genislik;
            el.flexibleWidth = 1f;
        }

        /// <summary>
        /// Kilitli buton gorunumu.
        ///
        /// ⚠ <see cref="UiKit.KalinButon"/> pahli bir butondur ve Unity'nin
        /// <c>disabledColor</c>'i yalnizca DIS tabana isler; ustteki "Yuz" katmani
        /// parlak kaldigi icin kilitli buton basilabilir gorunuyordu. Yuzu kagit tonuna
        /// cekmek kilidi tek bakista okutur.
        /// </summary>
        internal static void KilitGorunumu(Button buton)
        {
            if (buton == null || buton.interactable) return;

            var yuz = buton.transform.Find("Yuz");
            if (yuz != null)
            {
                var yuzImg = yuz.GetComponent<Image>();
                if (yuzImg != null) yuzImg.color = Palet.UiKagitCukur;
            }

            var etiket = buton.GetComponentInChildren<Text>();
            if (etiket == null) return;
            etiket.color = Palet.UiKoyuYaziSolgun;
            // Kontur doygun zemin icindir; solgun koyu yazinin etrafinda camur birakir.
            var kontur = etiket.GetComponent<Outline>();
            if (kontur != null) kontur.enabled = false;
        }

        /// <summary>
        /// Giris kutusunu krem parsomene uyarlar.
        ///
        /// ⚠ <see cref="UiKit.Giris"/> KOYU panel icin kuruludur: koyu kutu, acik yazi,
        /// acik altin imlec. Krem zeminde yazi da imlec de gorunmez olur.
        /// </summary>
        internal static void KagitGiris(InputField giris)
        {
            var zemin = giris.gameObject.GetComponent<Image>();
            if (zemin != null) zemin.color = Palet.UiKagitCukur;
            if (giris.textComponent != null) giris.textComponent.color = Palet.UiKoyuYazi;
            var ipucu = giris.placeholder as Text;
            if (ipucu != null) ipucu.color = Palet.UiKoyuYaziSolgun;
            giris.caretColor = Palet.UiKoyuYazi;
            // Secim vurgusu yazinin ARKASINA cizilir; altin zemin uzerinde koyu yazi
            // okunur kalir ve secim gercekten gorunur.
            giris.selectionColor = Palet.UiAltin;
        }

        /// <summary>
        /// Ayirac cizgisi.
        ///
        /// ⚠ <see cref="UiKit.Ayirac"/> ACIK renkli bir cizgi cizer (koyu panel icin);
        /// krem parsomen uzerinde tamamen kaybolur. Kahve kagit tonuna cevrilir ve
        /// 1 px yerine 2 px'e cikarilir — ince acik cizgi kremde hic okunmuyordu.
        /// </summary>
        internal static void Ayirac(Transform ebeveyn)
        {
            var rt = UiKit.Ayirac(ebeveyn, 2f);
            var img = rt.gameObject.GetComponent<Image>();
            if (img != null) img.color = Palet.UiKagitCukur;
        }
    }

    /// <summary>Tezgah (craft) ekrani. Sv 1'de tas kazma, tas balta, ahsap kova (B.17).</summary>
    public class TezgahEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;
        private RectTransform _liste;

        /// <summary>Dis satir yuksekligi. Ic yuzey 2×3 cerceve kadar alcak kalir.</summary>
        private const float SatirYuksekligi = 64f;

        public event Action<string> Craftla;

        public TezgahEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "TezgahEkrani", "Basit tezgâh");
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
                // Satir, parsomen govdenin UZERINE binen bir yuzeydir: bir ton koyu
                // kagit + kahve cerceve. Cercevesiz satir krem govdeye karisiyor ve
                // liste duz bir metin blogu gibi gorunuyordu.
                RectTransform ic;
                EkranParcasi.Satir(_liste, "Tarif_" + tarifId, Palet.ParsomenKoyu, SatirYuksekligi, out ic);
                // ⚠ Kenar 10: buton (36) + 2×10 = 56 ve 58'lik ic yuzeye sigar. Dis olcu
                // 64 secildi cunku cerceve her kenardan 3 px yiyor.
                var dizi = UiKit.YatayDizi(ic, 10f, 10);
                // ⚠ Kapatilmazsa buton satir yuksekligine gerilir (sunger buton).
                dizi.childForceExpandHeight = false;
                dizi.childAlignment = TextAnchor.MiddleLeft;

                EkranParcasi.Ikon(ic, "Ikon", Sekil.Elmas(), Palet.UiCerceve, 28f);

                var ad = UiKit.Yazi(
                    ic, "Ad", _denge.EsyaAdi(tarifId), UiKit.YaziGovde, Palet.UiKoyuYazi,
                    TextAnchor.MiddleLeft, true);
                UiKit.Boyut(ad.gameObject, 158f, 34f);

                var malzemeler = _denge.AletMalzemesi(tarifId);
                bool yeter = true;
                var parcalar = new List<string>();
                foreach (var m in malzemeler)
                {
                    int elde = _durum.EnvanterAdet(m.EsyaId);
                    if (elde < m.Adet) yeter = false;
                    parcalar.Add(elde + "/" + m.Adet + " " + _denge.EsyaAdi(m.EsyaId));
                }

                // Eksik malzeme kirmiziya doner. ⚠ Acik kirmizi (UiKirmizi) krem kagitta
                // sinyal gibi degil sus gibi duruyor; koyu tonu okunur kaliyor.
                var malzemeYazi = UiKit.Yazi(
                    ic, "Malzeme", string.Join("  ·  ", parcalar), UiKit.YaziKucuk,
                    yeter ? Palet.UiKoyuYazi : Palet.UiKirmiziKoyu, TextAnchor.MiddleLeft);
                // Esneyen sutun malzeme listesi olsun: buton hep sagda hizali kalir.
                UiKit.Boyut(malzemeYazi.gameObject, 240f, 34f).flexibleWidth = 1f;

                bool sahip = _durum.AletVar(tarifId);
                string tid = tarifId;
                // Listede N tane satir var; hicbiri YESIL olamaz — ekranda en fazla bir
                // yesil buton bulunur (B.22). Hepsi ikincil ahsap butondur.
                var buton = UiKit.AhsapButon(ic, "Yap", sahip ? "Yenile" : "Yap", () => Craftla?.Invoke(tid));
                buton.interactable = yeter && ben.Seviye >= _denge.AletCraftSeviyesi(tarifId);
                UiKit.Boyut(buton.gameObject, 104f, 36f);
                EkranParcasi.KilitGorunumu(buton);
            }

            var not = UiKit.Yazi(
                _liste, "Not",
                "Taş kazma ve taş balta Sv 1'den yapılabilir; dal ve taş parçası elle toplanır. "
                + "Böylece alet kırılsa bile oyun kilitlenmez.",
                UiKit.YaziMini, Palet.UiKoyuYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(not.gameObject, 0f, 48f);
        }
    }

    /// <summary>Defter (B.21). Anlatim tamamen toplanabilir metin sayfalariyla yurur.</summary>
    public class DefterEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private RectTransform _liste;

        private const int ToplamSayfa = 20;

        /// <summary>Ilerleme cubugunun dis genisligi. <c>CubukAyarla</c> ic genislik ister.</summary>
        private const float CubukGenislik = 120f;

        /// <summary>Dikey dilimde ilk 5 sayfanin metni. Gerisi perde perde yazilir (A.8).</summary>
        private static readonly Dictionary<int, string> Sayfalar = new Dictionary<int, string>
        {
            { 1, "Bu vadide hiçbir iş tek başına görülmez.\n\nTren manifestosu iki imza ister. "
                 + "Madende ikinci bir el olmadan derine inilmez. Tarlada biri eker, biri sular." },
            { 2, "İlk hasat.\n\nToprak verdiğini geri aldığın gün, vadi seni tanımaya başlar." },
            { 3, "Depo açıldı.\n\nBurada bıraktıkların ona ulaşır. O da sana bırakabilir." },
            { 4, "İkinci imza.\n\nManifesto iki isim ister. Çağırdığın kişi aynı vadiye düşer." },
            { 5, "Tren yıllar sonra ilk kez durdu.\n\nVadinin dışarıyla bağlantısı geri geldi." }
        };

        public DefterEkrani(Transform kanvas, DunyaDurumu durum)
        {
            _durum = durum;
            _panel = new Panel(kanvas, "DefterEkrani", "Birlikte yapılanlar defteri");
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

            IlerlemeSeridi(_durum.DefterSayfasi);

            for (int no = 1; no <= ToplamSayfa; no++)
            {
                if (_durum.IsaretVar("defter:" + no)) AcikSayfa(no);
                else KapaliSayfa(no);
            }
        }

        /// <summary>Listenin basindaki sayac. ICE COKUK kagit: sayfa kartlarindan ayrilsin.</summary>
        private void IlerlemeSeridi(int acik)
        {
            RectTransform ic;
            EkranParcasi.Satir(_liste, "Ilerleme", Palet.UiKagitCukur, 58f, out ic);
            // Yukseklik = en uzun oge (sayac 28) + 2×12 kenar + 2×3 cerceve.
            var dizi = UiKit.YatayDizi(ic, 8f, 12);
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;

            EkranParcasi.Ikon(ic, "Ikon", Sekil.Yildiz(), Palet.UiAltinKoyu, 22f);

            var etiket = UiKit.Yazi(
                ic, "Etiket", "Açılan sayfalar", UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun,
                TextAnchor.MiddleLeft);
            UiKit.Boyut(etiket.gameObject, 150f, 24f).flexibleWidth = 1f;

            // Sayi tek basina ilerlemeyi HISSETTIRMIYOR; cubuk bir bakista okunur.
            Image dolgu;
            EkranParcasi.Cubuk(ic, "Cubuk", CubukGenislik, 16f, out dolgu);
            UiKit.CubukAyarla(dolgu, acik / (float)ToplamSayfa, CubukGenislik - 4f);

            var sayac = UiKit.Yazi(
                ic, "Sayac", acik + " / " + ToplamSayfa, UiKit.YaziBaslik, Palet.UiKoyuYazi,
                TextAnchor.MiddleRight, true);
            UiKit.Boyut(sayac.gameObject, 96f, 28f);
        }

        private void AcikSayfa(int no)
        {
            RectTransform ic;
            EkranParcasi.Satir(_liste, "Sayfa" + no, Palet.ParsomenKoyu, 126f, out ic);
            // 2×14 kenar + baslik 22 + 6 aralik + govde 64 = 120; cerceve 2×3 ile 126.
            // Kart bundan alcak olursa govde metni kendi zemininin disina tasar.
            UiKit.DikeyDizi(ic, 6f, 14);

            // Baslik satiri ayri bir yatay dizi: ikon ile sayfa numarasi yan yana dursun.
            var baslikSatir = UiKit.Panel(ic, "BaslikSatir", Color.clear);
            UiKit.Boyut(baslikSatir.gameObject, 0f, 22f);
            var basDizi = UiKit.YatayDizi(baslikSatir, 6f, 0);
            basDizi.childForceExpandHeight = false;
            basDizi.childAlignment = TextAnchor.MiddleLeft;

            EkranParcasi.Ikon(baslikSatir, "Ikon", Sekil.Yildiz(), Palet.UiAltinKoyu, 18f);

            var basli = UiKit.Yazi(
                baslikSatir, "No", "Sayfa " + no, UiKit.YaziKucuk, Palet.UiCerceve,
                TextAnchor.MiddleLeft, true);
            UiKit.Boyut(basli.gameObject, 120f, 22f).flexibleWidth = 1f;

            string metin = Sayfalar.TryGetValue(no, out string m)
                ? m
                : "(bu sayfanın metni henüz yazılmadı)";
            var govde = UiKit.Yazi(
                ic, "Metin", metin, UiKit.YaziKucuk, Palet.UiKoyuYazi, TextAnchor.UpperLeft);
            UiKit.Boyut(govde.gameObject, 0f, 64f);
        }

        /// <summary>
        /// Kapali sayfa TEK SATIR ve ICE COKUK. Onceden acik sayfayla ayni kutu kabugunu
        /// paylasiyor, govde yazisi 0 yukseklikli bir rect'ten tasiyordu.
        /// </summary>
        private void KapaliSayfa(int no)
        {
            RectTransform ic;
            EkranParcasi.Satir(_liste, "Sayfa" + no, Palet.UiKagitCukur, 50f, out ic);
            // 22'lik yazi + 2×10 kenar = 42; cerceve 2×3 ile 50.
            var dizi = UiKit.YatayDizi(ic, 8f, 10);
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;

            EkranParcasi.Ikon(ic, "Ikon", Sekil.Elmas(), Palet.UiKoyuYaziSolgun, 16f);

            var basli = UiKit.Yazi(
                ic, "No", "Sayfa " + no, UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun, TextAnchor.MiddleLeft);
            UiKit.Boyut(basli.gameObject, 120f, 22f).flexibleWidth = 1f;

            var kilit = UiKit.Yazi(
                ic, "Kilit", "henüz açılmadı", UiKit.YaziMini, Palet.UiKoyuYaziSolgun,
                TextAnchor.MiddleRight);
            UiKit.Boyut(kilit.gameObject, 140f, 22f);
        }
    }

    /// <summary>Vadi haritasi (B.4). Bolgeye dokunursun, o sahneye girersin.</summary>
    public class HaritaEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;
        private RectTransform _liste;

        /// <summary>Tezgah satiriyla ayni olcu: 36'lik buton + 2×10 kenar + 2×3 cerceve.</summary>
        private const float SatirYuksekligi = 64f;

        public event Action<string> BolgeSecildi;

        public HaritaEkrani(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _panel = new Panel(kanvas, "HaritaEkrani", "Vadi haritası", null, 660f, 460f);
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

                // Kapali bolge ICE COKUK kagitta durur: kilitli oldugu renkten de
                // anlasilsin, yalnizca soluk yazidan degil.
                RectTransform ic;
                EkranParcasi.Satir(
                    _liste, "Bolge_" + bolgeId, acik ? Palet.ParsomenKoyu : Palet.UiKagitCukur,
                    SatirYuksekligi, out ic);
                var dizi = UiKit.YatayDizi(ic, 10f, 10);
                dizi.childForceExpandHeight = false;
                dizi.childAlignment = TextAnchor.MiddleLeft;

                // Yer tutucu bolge isareti; gercek bolge ikonlari 2B varlik olarak gelecek.
                EkranParcasi.Ikon(
                    ic, "Ikon", Sekil.Daire(), acik ? Palet.Yaprak : Palet.UiKoyuYaziSolgun, 26f);

                var ad = UiKit.Yazi(
                    ic, "Ad", _denge.BolgeAdi(bolgeId), UiKit.YaziGovde,
                    // Kapali bolgeler SILIK gorunur ama GORUNUR (B.18 0:30): oyuncu
                    // vadinin buyuklugunu ilk dakikada gorsun.
                    acik ? Palet.UiKoyuYazi : Palet.UiKoyuYaziSolgun, TextAnchor.MiddleLeft, true);
                UiKit.Boyut(ad.gameObject, 240f, 34f).flexibleWidth = 1f;

                var durum = UiKit.Yazi(
                    ic, "Durum", acik ? "" : "Sv " + gereken, UiKit.YaziKucuk,
                    Palet.UiKoyuYaziSolgun, TextAnchor.MiddleRight);
                UiKit.Boyut(durum.gameObject, 84f, 34f);

                string bid = bolgeId;
                var git = UiKit.AhsapButon(ic, "Git", "Git", () => BolgeSecildi?.Invoke(bid));
                git.interactable = acik;
                UiKit.Boyut(git.gameObject, 96f, 36f);
                EkranParcasi.KilitGorunumu(git);
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

        private const string BosKod = "— — — — — —";

        public event Action KodIste;
        public event Action<string> Katil;
        public event Action HesapBagla;

        public DavetEkrani(Transform kanvas)
        {
            // ⚠ Panel yuksekligi icerigin toplamina gore secildi. Fazla yuksek panel
            // dikey diziyi bosluga yayip butonlari sisiriyor. Cerceveli kabuklar her
            // satira 6 px ekledigi icin olcu 520'den 550'ye cikti.
            _panel = new Panel(kanvas, "DavetEkrani", "Eşini vadiye çağır", null, 660f, 550f);

            var dikey = UiKit.Panel(_panel.Icerik, "Dikey", Color.clear);
            UiKit.Doldur(dikey);
            UiKit.DikeyDizi(dikey, 10f, 16);

            var aciklama = UiKit.Yazi(
                dikey, "Aciklama",
                "Bu vadide hiçbir iş tek başına görülmez. Kodu eşine gönder; aynı vadiye düşer.\n"
                + "Rastgele eşleşme yoktur — yalnızca çağırdığın kişi gelir.",
                UiKit.YaziGovde, Palet.UiKoyuYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(aciklama.gameObject, 0f, 50f);

            // Kod ICE COKUK, kalin cerceveli bir kutuda durur: kopyalanacak bir DEGER
            // oldugu okunsun, ekrandaki baska bir baslik gibi gorunmesin. Ekranin
            // kahramani oldugu icin cercevesi liste satirlarindan kalin.
            RectTransform kodIc;
            EkranParcasi.Satir(dikey, "KodKart", Palet.UiKagitCukur, 78f, out kodIc, 18, 4f);

            // Kalp: bu kod bir ES cagrisidir (B.7). Yer tutucu ikon.
            var kalp = EkranParcasi.Ikon(kodIc, "Kalp", Sekil.Kalp(), Palet.UiKirmizi, 30f);
            UiKit.Sabitle(kalp.rectTransform, 0f, 0.5f, 18f, 0f, 30f, 30f);

            _kodYazi = UiKit.Yazi(
                kodIc, "Kod", BosKod, UiKit.YaziDev, Palet.UiCerceve, TextAnchor.MiddleCenter, true);
            UiKit.Doldur(_kodYazi.rectTransform, 10f);

            // Ekranin tek ana eylemi: kod uretmek. Ekrandaki TEK yesil buton budur;
            // katilma ve baglama ikincil ahsap kalir.
            var uret = UiKit.YesilButon(dikey, "Uret", "Davet kodu üret", () => KodIste?.Invoke());
            UiKit.Boyut(uret.gameObject, 0f, 56f);

            _durumYazi = UiKit.Yazi(
                dikey, "Durum", "", UiKit.YaziKucuk, Palet.UiKirmiziKoyu, TextAnchor.MiddleCenter);
            UiKit.Boyut(_durumYazi.gameObject, 0f, 28f);

            EkranParcasi.Ayirac(dikey);

            var ayracEtiket = UiKit.Yazi(
                dikey, "AyracEtiket", "veya elindeki kodla katıl", UiKit.YaziMini,
                Palet.UiKoyuYaziSolgun, TextAnchor.MiddleCenter);
            UiKit.Boyut(ayracEtiket.gameObject, 0f, 20f);

            var katilSatir = UiKit.Panel(dikey, "KatilSatir", Color.clear);
            UiKit.Boyut(katilSatir.gameObject, 0f, 56f);
            var katilDizi = UiKit.YatayDizi(katilSatir, 10f, 0);
            katilDizi.childForceExpandHeight = false;
            katilDizi.childAlignment = TextAnchor.MiddleLeft;

            // Giris kutusu kendi kahve cercevesini alir: cercevesiz kutu krem panelde
            // "buraya yazilir" demiyordu, panelin bir parcasi gibi duruyordu.
            RectTransform girisIc;
            var girisKutu = EkranParcasi.Satir(katilSatir, "KodKutu", Palet.UiKagitCukur, 56f, out girisIc);
            EkranParcasi.Esnet(girisKutu.gameObject, 300f);

            _kodGiris = UiKit.Giris(girisIc, "KodGiris", "8 haneli kod", 8, TextAnchor.MiddleCenter);
            UiKit.Doldur(_kodGiris.GetComponent<RectTransform>());
            EkranParcasi.KagitGiris(_kodGiris);

            var katilButon = UiKit.AhsapButon(
                katilSatir, "Katil", "Katıl", () => Katil?.Invoke(_kodGiris.text));
            UiKit.Boyut(katilButon.gameObject, 170f, 56f);

            var bagla = UiKit.AhsapButon(
                dikey, "Bagla", "Hesabımı bağla (e-posta)", () => HesapBagla?.Invoke());
            UiKit.Boyut(bagla.gameObject, 0f, 48f);
        }

        public void Ac() => _panel.Ac();

        public bool Acik => _panel.Acik;

        public void KodGoster(string kod)
        {
            _kodYazi.text = string.IsNullOrEmpty(kod) ? BosKod : kod;
            _durumYazi.text = string.IsNullOrEmpty(kod) ? "" : "Kod 24 saat geçerli.";
            // ⚠ Acik yesil (UiOnay) krem kagitta neredeyse okunmuyor; koyu tonu sinyali korur.
            _durumYazi.color = Palet.UiYesilKoyu;
        }

        public void HataGoster(string mesaj)
        {
            _durumYazi.text = mesaj;
            _durumYazi.color = Palet.UiKirmiziKoyu;
        }
    }

    /// <summary>Ayarlar: ses kanallari, haptik, sunucu adresi.</summary>
    public class AyarlarEkrani
    {
        private readonly Panel _panel;
        private readonly SesYoneticisi _ses;
        private InputField _adresGiris;

        /// <summary>Ses cubugunun sabit DIS genisligi. <c>CubukAyarla</c> ic genislik ister.</summary>
        private const float SesCubukGenislik = 150f;

        /// <summary>Dis satir yuksekligi: 36'lik buton + 2×10 kenar + 2×3 cerceve.</summary>
        private const float SatirYuksekligi = 62f;

        public event Action<string> AdresDegisti;

        public AyarlarEkrani(Transform kanvas, SesYoneticisi ses)
        {
            _ses = ses;
            // ⚠ Yukseklik icerigin toplamindan (4 ses satiri + haptik + adres + not)
            // hesaplandi; daha alcak panel son satiri modalin disinda birakiyordu.
            // Cerceveli satirlar her satira 6 px ekledigi icin olcu 580'den 620'ye cikti.
            _panel = new Panel(kanvas, "AyarlarEkrani", "Ayarlar", null, 640f, 620f);

            var dikey = UiKit.Panel(_panel.Icerik, "Dikey", Color.clear);
            UiKit.Doldur(dikey);
            UiKit.DikeyDizi(dikey, 8f, 16);

            foreach (SesKanali kanal in Enum.GetValues(typeof(SesKanali)))
            {
                SesKanaliSatiri(dikey, kanal);
            }

            EkranParcasi.Ayirac(dikey);
            HaptikSatiri(dikey);
            AdresSatiri(dikey);
            EkranParcasi.Ayirac(dikey);

            var not = UiKit.Yazi(
                dikey, "Not",
                "Sesi kapalı oynayabilirsin: hiçbir mekanik yalnızca sesle bildirilmez.",
                UiKit.YaziMini, Palet.UiKoyuYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(not.gameObject, 0f, 40f);
        }

        /// <summary>
        /// Ortak satir kabugu: bir ton koyu kagit + kalin kahve cerceve.
        ///
        /// ⚠ Donen deger artik IC yuzeydir. Cocuklar dis cerceveye eklenirse cercevenin
        /// uzerine tasar — <see cref="UiKit.CerceveliKart"/> sozlesmesi budur.
        /// </summary>
        private static RectTransform Satir(Transform ebeveyn, string ad, float yukseklik)
        {
            RectTransform ic;
            EkranParcasi.Satir(ebeveyn, ad, Palet.ParsomenKoyu, yukseklik, out ic);
            // Kenar 10: 56'lik ic yuzeyde 36'lik butonlar ve cubuk tam oturur.
            var dizi = UiKit.YatayDizi(ic, 10f, 10);
            // ⚠ Kapatilmazsa cubuk ve butonlar satir yuksekligine gerilir; pahli
            // butonlar sunger gibi sisip alt seritlerini kaybeder.
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;
            return ic;
        }

        /// <summary>Sag bloku saga iten gorunmez esneme. Yatay dizide bosluk yaratmanin yolu.</summary>
        private static void Esneme(Transform ebeveyn)
        {
            var bosluk = UiKit.Panel(ebeveyn, "Bosluk", Color.clear);
            bosluk.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        /// <summary>
        /// Acik/kapali farkini BUTONUN YUZUNDEN okutur: yalniz yazi degisimi gozden
        /// kaciyordu. Acikken altin yuz, kapaliyken ahsap yuz.
        ///
        /// ⚠ Yesil kullanilmaz: ekrandaki tek yesil ana eylem butonuna ayrilmistir (B.22),
        /// bir ayar anahtari o hiyerarsiyi calmamali.
        /// </summary>
        private static void HaptikYuzu(Button buton)
        {
            var yuz = buton.transform.Find("Yuz");
            if (yuz == null) return;
            var img = yuz.GetComponent<Image>();
            if (img == null) return;
            img.color = Haptik.Haptik.Acik ? Palet.UiAltin : Palet.UiAhsapButon;
        }

        /// <summary>
        /// Kanalin ekranda gorunen adi.
        ///
        /// ⚠ <c>kanal.ToString()</c> KULLANILMAZ: enum adi bir kod tanimlayicisidir,
        /// Turkce karakter tasiyamaz ve "Ui" gibi kisaltmalar ekranda anlamsiz durur.
        /// Gorunen ad her zaman ayri tutulur.
        /// </summary>
        private static string KanalAdi(SesKanali kanal)
        {
            switch (kanal)
            {
                case SesKanali.Muzik: return "Müzik";
                case SesKanali.Ortam: return "Ortam";
                case SesKanali.Eylem: return "Eylem";
                case SesKanali.Ui: return "Arayüz";
                default: return kanal.ToString();
            }
        }

        private void SesKanaliSatiri(Transform ebeveyn, SesKanali kanal)
        {
            SesKanali k = kanal;
            var satir = Satir(ebeveyn, "Ses_" + kanal, SatirYuksekligi);

            EkranParcasi.Ikon(satir, "Ikon", Sekil.Daire(), Palet.UiAltinKoyu, 22f);

            var ad = UiKit.Yazi(
                satir, "Ad", KanalAdi(kanal), UiKit.YaziGovde, Palet.UiKoyuYazi,
                TextAnchor.MiddleLeft, true);
            UiKit.Boyut(ad.gameObject, 88f, 28f);

            // Sayi tek basina seviyeyi HISSETTIRMIYOR; cubuk bir bakista okunur.
            Image dolgu;
            EkranParcasi.Cubuk(satir, "Cubuk", SesCubukGenislik, 16f, out dolgu);

            var deger = UiKit.Yazi(
                satir, "Deger", "", UiKit.YaziKucuk, Palet.UiCerceve, TextAnchor.MiddleRight, true);
            UiKit.Boyut(deger.gameObject, 58f, 28f);

            Esneme(satir);

            Action tazele = () =>
            {
                float seviye = _ses.KanalSeviyesi(k);
                deger.text = Mathf.RoundToInt(seviye * 100f) + "%";
                // ⚠ Tam genislik cubugun IC olcusudur: cerceve her kenardan 2 px yer.
                UiKit.CubukAyarla(dolgu, seviye, SesCubukGenislik - 4f);
            };
            tazele();

            var azalt = UiKit.AhsapButon(satir, "Az", "−", () =>
            {
                _ses.KanalSeviyesiAyarla(k, _ses.KanalSeviyesi(k) - 0.1f);
                tazele();
            });
            UiKit.Boyut(azalt.gameObject, 36f, 36f);

            var artir = UiKit.AhsapButon(satir, "Cok", "+", () =>
            {
                _ses.KanalSeviyesiAyarla(k, _ses.KanalSeviyesi(k) + 0.1f);
                tazele();
            });
            UiKit.Boyut(artir.gameObject, 36f, 36f);
        }

        // Haptik kapatilabilir olmak ZORUNDA (B.23).
        private void HaptikSatiri(Transform ebeveyn)
        {
            var satir = Satir(ebeveyn, "Haptik", SatirYuksekligi);

            // Simsek yer tutucu: titresim icin gercek ikon 2B varlik olarak gelecek.
            EkranParcasi.Ikon(satir, "Ikon", Sekil.Simsek(), Palet.UiAltinKoyu, 22f);

            var ad = UiKit.Yazi(
                satir, "Ad", "Titreşim", UiKit.YaziGovde, Palet.UiKoyuYazi, TextAnchor.MiddleLeft, true);
            UiKit.Boyut(ad.gameObject, 160f, 28f);

            Esneme(satir);

            Button haptikButon = null;
            haptikButon = UiKit.AhsapButon(satir, "Degistir", Haptik.Haptik.Acik ? "Açık" : "Kapalı", () =>
            {
                Haptik.Haptik.Acik = !Haptik.Haptik.Acik;
                var y = haptikButon.GetComponentInChildren<Text>();
                if (y == null) return;
                y.text = Haptik.Haptik.Acik ? "Açık" : "Kapalı";
                HaptikYuzu(haptikButon);
            });
            UiKit.Boyut(haptikButon.gameObject, 124f, 36f);

            HaptikYuzu(haptikButon);
        }

        private void AdresSatiri(Transform ebeveyn)
        {
            var satir = Satir(ebeveyn, "Adres", SatirYuksekligi);

            EkranParcasi.Ikon(satir, "Ikon", Sekil.Elmas(), Palet.UiAltinKoyu, 22f);

            var ad = UiKit.Yazi(
                satir, "Ad", "Sunucu", UiKit.YaziGovde, Palet.UiKoyuYazi, TextAnchor.MiddleLeft, true);
            UiKit.Boyut(ad.gameObject, 80f, 28f);

            // Giris kendi cerceveli kutusunda durur: krem panelde cercevesiz kutu
            // yazilabilir bir alan gibi gorunmuyordu.
            RectTransform girisIc;
            var girisKutu = EkranParcasi.Satir(satir, "GirisKutu", Palet.UiKagitCukur, 40f, out girisIc);
            EkranParcasi.Esnet(girisKutu.gameObject, 240f);

            _adresGiris = UiKit.Giris(girisIc, "Giris", "http://...", 120);
            UiKit.Doldur(_adresGiris.GetComponent<RectTransform>());
            EkranParcasi.KagitGiris(_adresGiris);
            // ⚠ Giris varsayilan yazi boyutu baslik olcegidir; bir URL bu
            // yukseklikteki kutuya o boyutla sigmiyor. Kucuk olcege indiriliyor.
            _adresGiris.textComponent.fontSize = UiKit.YaziKucuk;
            var ipucu = _adresGiris.placeholder as Text;
            if (ipucu != null) ipucu.fontSize = UiKit.YaziKucuk;
            _adresGiris.text = SunucuAyari.Temel;

            var kaydet = UiKit.AhsapButon(
                satir, "Kaydet", "Kaydet", () => AdresDegisti?.Invoke(_adresGiris.text));
            UiKit.Boyut(kaydet.gameObject, 110f, 36f);
        }

        public void Ac() => _panel.Ac();

        public bool Acik => _panel.Acik;
    }
}
