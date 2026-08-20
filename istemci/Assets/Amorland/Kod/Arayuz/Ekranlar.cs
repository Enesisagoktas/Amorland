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
    /// <summary>Tezgah (craft) ekrani. Sv 1'de tas kazma, tas balta, ahsap kova (B.17).</summary>
    public class TezgahEkrani
    {
        private readonly Panel _panel;
        private readonly DunyaDurumu _durum;
        private readonly DengeVerisi _denge;
        private RectTransform _liste;

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
                // Satir panelin UZERINE binen bir yuzeydir: UiZemin degil UiYuzey.
                // UiZemin kullanildiginda satirlar panel zeminine karisip liste
                // duz bir metin blogu gibi gorunuyordu.
                var satir = UiKit.Kart(_liste, "Tarif_" + tarifId, Palet.UiYuzey, UiKit.YaricapKucuk);
                UiKit.Boyut(satir.gameObject, 0f, 58f);
                // ⚠ Kenar 10: buton (36) + 2x10 = 56 ve satira sigar. 12 kenarla
                // buton satirin zemininden tasip alt satira giriyordu.
                var dizi = UiKit.YatayDizi(satir, 10f, 10);
                // ⚠ Kapatilmazsa buton satir yuksekligine gerilir (sunger buton).
                dizi.childForceExpandHeight = false;
                dizi.childAlignment = TextAnchor.MiddleLeft;

                var ad = UiKit.Yazi(
                    satir, "Ad", _denge.EsyaAdi(tarifId), UiKit.YaziGovde, Palet.UiYazi,
                    TextAnchor.MiddleLeft, true);
                UiKit.Boyut(ad.gameObject, 168f, 34f);

                var malzemeler = _denge.AletMalzemesi(tarifId);
                bool yeter = true;
                var parcalar = new List<string>();
                foreach (var m in malzemeler)
                {
                    int elde = _durum.EnvanterAdet(m.EsyaId);
                    if (elde < m.Adet) yeter = false;
                    parcalar.Add(elde + "/" + m.Adet + " " + _denge.EsyaAdi(m.EsyaId));
                }

                var malzemeYazi = UiKit.Yazi(
                    satir, "Malzeme", string.Join("  ·  ", parcalar), UiKit.YaziKucuk,
                    yeter ? Palet.UiYazi : Palet.UiUyari, TextAnchor.MiddleLeft);
                // Esneyen sutun malzeme listesi olsun: buton hep sagda hizali kalir.
                UiKit.Boyut(malzemeYazi.gameObject, 240f, 34f).flexibleWidth = 1f;

                bool sahip = _durum.AletVar(tarifId);
                string tid = tarifId;
                // Listede N tane satir var; hicbiri ana buton olamaz (ekranda tek ana buton).
                var buton = UiKit.Buton(satir, "Yap", sahip ? "Yenile" : "Yap", () => Craftla?.Invoke(tid));
                buton.interactable = yeter && ben.Seviye >= _denge.AletCraftSeviyesi(tarifId);
                UiKit.Boyut(buton.gameObject, 104f, 36f);
            }

            var not = UiKit.Yazi(
                _liste, "Not",
                "Taş kazma ve taş balta Sv 1'den yapılabilir; dal ve taş parçası elle toplanır. "
                + "Böylece alet kırılsa bile oyun kilitlenmez.",
                UiKit.YaziMini, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
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

        /// <summary>Listenin basindaki sayac. Ice cokuk yuzey: sayfa kartlarindan ayrilsin.</summary>
        private void IlerlemeSeridi(int acik)
        {
            var serit = UiKit.Kart(_liste, "Ilerleme", Palet.UiYuzeyKoyu, UiKit.YaricapKucuk);
            // Yukseklik = en uzun oge (sayac 28) + 2x12 kenar. Onceki 46/14 ikilisi
            // sayaci seridin disina tasiriyordu.
            UiKit.Boyut(serit.gameObject, 0f, 52f);
            var dizi = UiKit.YatayDizi(serit, 8f, 12);
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;

            var etiket = UiKit.Yazi(
                serit, "Etiket", "Açılan sayfalar", UiKit.YaziKucuk, Palet.UiYaziSolgun,
                TextAnchor.MiddleLeft);
            UiKit.Boyut(etiket.gameObject, 180f, 24f).flexibleWidth = 1f;

            var sayac = UiKit.Yazi(
                serit, "Sayac", acik + " / " + ToplamSayfa, UiKit.YaziBaslik, Palet.UiVurgu,
                TextAnchor.MiddleRight, true);
            UiKit.Boyut(sayac.gameObject, 110f, 28f);
        }

        private void AcikSayfa(int no)
        {
            var kart = UiKit.Kart(_liste, "Sayfa" + no, Palet.UiYuzey, UiKit.YaricapKucuk);
            // 2x14 kenar + baslik 22 + 6 aralik + govde 64 = 120. Kart bundan alcak
            // olursa govde metni kendi zemininin disina tasar.
            UiKit.Boyut(kart.gameObject, 0f, 120f);
            UiKit.DikeyDizi(kart, 6f, 14);

            var basli = UiKit.Yazi(
                kart, "No", "Sayfa " + no, UiKit.YaziKucuk, Palet.UiVurgu, TextAnchor.UpperLeft, true);
            UiKit.Boyut(basli.gameObject, 0f, 22f);

            string metin = Sayfalar.TryGetValue(no, out string m)
                ? m
                : "(bu sayfanın metni henüz yazılmadı)";
            var govde = UiKit.Yazi(
                kart, "Metin", metin, UiKit.YaziKucuk, Palet.UiYazi, TextAnchor.UpperLeft);
            UiKit.Boyut(govde.gameObject, 0f, 64f);
        }

        /// <summary>
        /// Kapali sayfa TEK SATIR ve ice cokuk. Onceden acik sayfayla ayni kutu kabugunu
        /// paylasiyor, govde yazisi 0 yukseklikli bir rect'ten tasiyordu.
        /// </summary>
        private void KapaliSayfa(int no)
        {
            var kart = UiKit.Kart(_liste, "Sayfa" + no, Palet.UiYuzeyKoyu, UiKit.YaricapKucuk);
            UiKit.Boyut(kart.gameObject, 0f, 44f);
            // 22'lik yazi + 2x10 kenar = 42; 14 kenarla satir kendi zeminini asiyordu.
            var dizi = UiKit.YatayDizi(kart, 8f, 10);
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;

            var basli = UiKit.Yazi(
                kart, "No", "Sayfa " + no, UiKit.YaziKucuk, Palet.UiYaziSolgun, TextAnchor.MiddleLeft);
            UiKit.Boyut(basli.gameObject, 120f, 22f).flexibleWidth = 1f;

            var kilit = UiKit.Yazi(
                kart, "Kilit", "henüz açılmadı", UiKit.YaziMini, Palet.UiYaziSolgun,
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

                // Kapali bolge ice cokuk yuzeyde durur: kilitli oldugu renkten de anlasilsin,
                // yalnizca soluk yazidan degil.
                var satir = UiKit.Kart(
                    _liste, "Bolge_" + bolgeId, acik ? Palet.UiYuzey : Palet.UiYuzeyKoyu,
                    UiKit.YaricapKucuk);
                UiKit.Boyut(satir.gameObject, 0f, 58f);
                // Tezgah satiriyla ayni olcu: 36'lik buton + 2x10 kenar = 56.
                var dizi = UiKit.YatayDizi(satir, 10f, 10);
                dizi.childForceExpandHeight = false;
                dizi.childAlignment = TextAnchor.MiddleLeft;

                var ad = UiKit.Yazi(
                    satir, "Ad", _denge.BolgeAdi(bolgeId), UiKit.YaziGovde,
                    // Kapali bolgeler SILIK gorunur ama GORUNUR (B.18 0:30): oyuncu
                    // vadinin buyuklugunu ilk dakikada gorsun.
                    acik ? Palet.UiYazi : Palet.UiYaziSolgun, TextAnchor.MiddleLeft, true);
                UiKit.Boyut(ad.gameObject, 240f, 34f).flexibleWidth = 1f;

                var durum = UiKit.Yazi(
                    satir, "Durum", acik ? "" : "Sv " + gereken, UiKit.YaziKucuk,
                    Palet.UiYaziSolgun, TextAnchor.MiddleRight);
                UiKit.Boyut(durum.gameObject, 84f, 34f);

                string bid = bolgeId;
                var git = UiKit.Buton(satir, "Git", "Git", () => BolgeSecildi?.Invoke(bid));
                git.interactable = acik;
                UiKit.Boyut(git.gameObject, 96f, 36f);
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
            // dikey diziyi bosluga yayip butonlari sisiriyor.
            _panel = new Panel(kanvas, "DavetEkrani", "Eşini vadiye çağır", null, 660f, 520f);

            var dikey = UiKit.Panel(_panel.Icerik, "Dikey", Color.clear);
            UiKit.Doldur(dikey);
            UiKit.DikeyDizi(dikey, 10f, 16);

            var aciklama = UiKit.Yazi(
                dikey, "Aciklama",
                "Bu vadide hiçbir iş tek başına görülmez. Kodu eşine gönder; aynı vadiye düşer.\n"
                + "Rastgele eşleşme yoktur — yalnızca çağırdığın kişi gelir.",
                UiKit.YaziGovde, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(aciklama.gameObject, 0f, 50f);

            // Kod ice cokuk bir kutuda durur: kopyalanacak bir DEGER oldugu okunsun,
            // ekrandaki baska bir baslik gibi gorunmesin.
            var kodKart = UiKit.Kart(dikey, "KodKart", Palet.UiYuzeyKoyu, UiKit.Yaricap);
            UiKit.Boyut(kodKart.gameObject, 0f, 70f);
            _kodYazi = UiKit.Yazi(
                kodKart, "Kod", BosKod, UiKit.YaziDev, Palet.UiVurgu, TextAnchor.MiddleCenter, true);
            UiKit.Doldur(_kodYazi.rectTransform, 10f);

            // Ekranin tek ana eylemi: kod uretmek. Katilma ve baglama ikincil kalir.
            var uret = UiKit.AnaButon(dikey, "Uret", "Davet kodu üret", () => KodIste?.Invoke());
            UiKit.Boyut(uret.gameObject, 0f, 52f);

            _durumYazi = UiKit.Yazi(
                dikey, "Durum", "", UiKit.YaziKucuk, Palet.UiUyari, TextAnchor.MiddleCenter);
            UiKit.Boyut(_durumYazi.gameObject, 0f, 28f);

            UiKit.Ayirac(dikey);

            var ayracEtiket = UiKit.Yazi(
                dikey, "AyracEtiket", "veya elindeki kodla katıl", UiKit.YaziMini,
                Palet.UiYaziSolgun, TextAnchor.MiddleCenter);
            UiKit.Boyut(ayracEtiket.gameObject, 0f, 20f);

            var katilSatir = UiKit.Panel(dikey, "KatilSatir", Color.clear);
            UiKit.Boyut(katilSatir.gameObject, 0f, 52f);
            var katilDizi = UiKit.YatayDizi(katilSatir, 10f, 0);
            katilDizi.childForceExpandHeight = false;
            katilDizi.childAlignment = TextAnchor.MiddleLeft;

            _kodGiris = UiKit.Giris(katilSatir, "KodGiris", "8 haneli kod", 8, TextAnchor.MiddleCenter);
            UiKit.Boyut(_kodGiris.gameObject, 300f, 52f).flexibleWidth = 1f;

            var katilButon = UiKit.Buton(katilSatir, "Katil", "Katıl", () => Katil?.Invoke(_kodGiris.text));
            UiKit.Boyut(katilButon.gameObject, 170f, 52f);

            var bagla = UiKit.Buton(dikey, "Bagla", "Hesabımı bağla (e-posta)", () => HesapBagla?.Invoke());
            UiKit.Boyut(bagla.gameObject, 0f, 44f);
        }

        public void Ac() => _panel.Ac();

        public bool Acik => _panel.Acik;

        public void KodGoster(string kod)
        {
            _kodYazi.text = string.IsNullOrEmpty(kod) ? BosKod : kod;
            _durumYazi.text = string.IsNullOrEmpty(kod) ? "" : "Kod 24 saat geçerli.";
            _durumYazi.color = Palet.UiOnay;
        }

        public void HataGoster(string mesaj)
        {
            _durumYazi.text = mesaj;
            _durumYazi.color = Palet.UiUyari;
        }
    }

    /// <summary>Ayarlar: ses kanallari, haptik, sunucu adresi.</summary>
    public class AyarlarEkrani
    {
        private readonly Panel _panel;
        private readonly SesYoneticisi _ses;
        private InputField _adresGiris;

        /// <summary>Ses cubugunun sabit genisligi. <c>CubukAyarla</c> tam genislik ister.</summary>
        private const float SesCubukGenislik = 150f;

        public event Action<string> AdresDegisti;

        public AyarlarEkrani(Transform kanvas, SesYoneticisi ses)
        {
            _ses = ses;
            // ⚠ Yukseklik icerigin toplamindan (4 ses satiri + haptik + adres + not)
            // hesaplandi; daha alcak panel son satiri modalin disinda birakiyordu.
            _panel = new Panel(kanvas, "AyarlarEkrani", "Ayarlar", null, 640f, 580f);

            var dikey = UiKit.Panel(_panel.Icerik, "Dikey", Color.clear);
            UiKit.Doldur(dikey);
            UiKit.DikeyDizi(dikey, 8f, 16);

            foreach (SesKanali kanal in Enum.GetValues(typeof(SesKanali)))
            {
                SesKanaliSatiri(dikey, kanal);
            }

            UiKit.Ayirac(dikey);
            HaptikSatiri(dikey);
            AdresSatiri(dikey);
            UiKit.Ayirac(dikey);

            var not = UiKit.Yazi(
                dikey, "Not",
                "Sesi kapalı oynayabilirsin: hiçbir mekanik yalnızca sesle bildirilmez.",
                UiKit.YaziMini, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Boyut(not.gameObject, 0f, 40f);
        }

        /// <summary>Ortak satir kabugu: panel uzerine binen yuvarlak koseli yuzey.</summary>
        private static RectTransform Satir(Transform ebeveyn, string ad, float yukseklik)
        {
            var satir = UiKit.Kart(ebeveyn, ad, Palet.UiYuzey, UiKit.YaricapKucuk);
            UiKit.Boyut(satir.gameObject, 0f, yukseklik);
            // Kenar 10: 56'lik satirda 36'lik daire buton ve butonlar tam oturur.
            var dizi = UiKit.YatayDizi(satir, 10f, 10);
            // ⚠ Kapatilmazsa cubuk ve daire butonlar satir yuksekligine gerilir;
            // daire butonlar elipse doner.
            dizi.childForceExpandHeight = false;
            dizi.childAlignment = TextAnchor.MiddleLeft;
            return satir;
        }

        /// <summary>Sag bloku saga iten gorunmez esneme. Yatay dizide bosluk yaratmanin yolu.</summary>
        private static void Esneme(Transform ebeveyn)
        {
            var bosluk = UiKit.Panel(ebeveyn, "Bosluk", Color.clear);
            bosluk.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void SesKanaliSatiri(Transform ebeveyn, SesKanali kanal)
        {
            SesKanali k = kanal;
            var satir = Satir(ebeveyn, "Ses_" + kanal, 56f);

            var ad = UiKit.Yazi(
                satir, "Ad", kanal.ToString(), UiKit.YaziGovde, Palet.UiYazi, TextAnchor.MiddleLeft, true);
            UiKit.Boyut(ad.gameObject, 96f, 28f);

            // Sayi tek basina seviyeyi HISSETTIRMIYOR; cubuk bir bakista okunur.
            Image dolgu;
            var cubuk = UiKit.Cubuk(satir, "Cubuk", Palet.UiYuzeyKoyu, Palet.UiVurgu, out dolgu);
            UiKit.Boyut(cubuk.gameObject, SesCubukGenislik, 8f);

            var deger = UiKit.Yazi(
                satir, "Deger", "", UiKit.YaziKucuk, Palet.UiVurgu, TextAnchor.MiddleRight, true);
            UiKit.Boyut(deger.gameObject, 58f, 28f);

            Esneme(satir);

            Action tazele = () =>
            {
                float seviye = _ses.KanalSeviyesi(k);
                deger.text = Mathf.RoundToInt(seviye * 100f) + "%";
                UiKit.CubukAyarla(dolgu, seviye, SesCubukGenislik);
            };
            tazele();

            var azalt = UiKit.DaireButon(satir, "Az", "−", () =>
            {
                _ses.KanalSeviyesiAyarla(k, _ses.KanalSeviyesi(k) - 0.1f);
                tazele();
            });
            UiKit.Boyut(azalt.gameObject, 36f, 36f);

            var artir = UiKit.DaireButon(satir, "Cok", "+", () =>
            {
                _ses.KanalSeviyesiAyarla(k, _ses.KanalSeviyesi(k) + 0.1f);
                tazele();
            });
            UiKit.Boyut(artir.gameObject, 36f, 36f);
        }

        // Haptik kapatilabilir olmak ZORUNDA (B.23).
        private void HaptikSatiri(Transform ebeveyn)
        {
            var satir = Satir(ebeveyn, "Haptik", 56f);

            var ad = UiKit.Yazi(
                satir, "Ad", "Titreşim", UiKit.YaziGovde, Palet.UiYazi, TextAnchor.MiddleLeft, true);
            UiKit.Boyut(ad.gameObject, 180f, 28f);

            Esneme(satir);

            Button haptikButon = null;
            haptikButon = UiKit.Buton(satir, "Degistir", Haptik.Haptik.Acik ? "Açık" : "Kapalı", () =>
            {
                Haptik.Haptik.Acik = !Haptik.Haptik.Acik;
                var y = haptikButon.GetComponentInChildren<Text>();
                if (y == null) return;
                y.text = Haptik.Haptik.Acik ? "Açık" : "Kapalı";
                // Acik/kapali farki renkten de okunsun: yalniz yazi degisimi gozden kaciyor.
                y.color = Haptik.Haptik.Acik ? Palet.UiOnay : Palet.UiYaziSolgun;
            });
            UiKit.Boyut(haptikButon.gameObject, 124f, 36f);

            var etiket = haptikButon.GetComponentInChildren<Text>();
            if (etiket != null) etiket.color = Haptik.Haptik.Acik ? Palet.UiOnay : Palet.UiYaziSolgun;
        }

        private void AdresSatiri(Transform ebeveyn)
        {
            var satir = Satir(ebeveyn, "Adres", 56f);

            var ad = UiKit.Yazi(
                satir, "Ad", "Sunucu", UiKit.YaziGovde, Palet.UiYazi, TextAnchor.MiddleLeft, true);
            UiKit.Boyut(ad.gameObject, 88f, 28f);

            _adresGiris = UiKit.Giris(satir, "Giris", "http://...", 120);
            UiKit.Boyut(_adresGiris.gameObject, 240f, 36f).flexibleWidth = 1f;
            // ⚠ Giris varsayilan yazi boyutu baslik olcegidir; bir URL bu
            // yukseklikteki kutuya o boyutla sigmiyor. Kucuk olcege indiriliyor.
            _adresGiris.textComponent.fontSize = UiKit.YaziKucuk;
            var ipucu = _adresGiris.placeholder as Text;
            if (ipucu != null) ipucu.fontSize = UiKit.YaziKucuk;
            _adresGiris.text = SunucuAyari.Temel;

            var kaydet = UiKit.Buton(satir, "Kaydet", "Kaydet", () => AdresDegisti?.Invoke(_adresGiris.text));
            UiKit.Boyut(kaydet.gameObject, 110f, 36f);
        }

        public void Ac() => _panel.Ac();

        public bool Acik => _panel.Acik;
    }
}
