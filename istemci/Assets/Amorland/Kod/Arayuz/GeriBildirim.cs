using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Kisa bildirimler (toast) ve red geri bildirimi.
    ///
    /// ⚠ C.3: istemci her red koduna <b>ayri</b> geri bildirim gosterir. "Bir hata olustu"
    /// demek, oyuncunun depoyu bosaltmasi gerektigini anlamamasi demektir.
    ///
    /// ⚠ B.26 yasak 17: hicbir mekanik yalnizca sesle bildirilmez — bu sinif her sesin
    /// gorsel karsiligini uretir.
    ///
    /// ⚠ Bildirim ekranin akisini KESMEZ: kirmizi dolu bir serit oyuncuyu durdurur,
    /// oysa buradaki mesajlarin cogu "sonra bak" seviyesindedir. Bu yuzden zemin her
    /// zaman koyudur; uyari yalnizca CERCEVE ve IKON rengiyle ayrisir.
    ///
    /// ⚠ Toast dunyanin uzerinde yuzer, bu yuzden B.22'nin HUD KAPSUL dilini kullanir:
    /// koyu govde + acik krem cerceve + beyaz konturlu yazi. Parsomen (krem govde)
    /// yalnizca oyuncunun actigi modallar icindir.
    /// </summary>
    public class Bildirimler : MonoBehaviour
    {
        private RectTransform _kok;
        private readonly List<GameObject> _aktifler = new List<GameObject>();
        private const int MaksAyniAnda = 3;

        private const float SeritGenislik = 520f;
        private const float SeritYukseklik = 46f;

        public void Kur(Transform kanvas)
        {
            _kok = UiKit.Panel(kanvas, "Bildirimler", Color.clear);
            // Ust seride sigacak sira: HUD kapsulleri (-12'den 96 px), sonra ogretici
            // ipucu seridi (-70'ten 48 px, alt kenari -118). -140 hepsinin altina
            // duser ve golge payini da (bulanik 14) temiz birakir.
            UiKit.Sabitle(_kok, 0.5f, 1f, 0f, -140f, SeritGenislik, 240f);
            // Aralik golge payindan (bulanik 14) genis olmali, yoksa ust uste binen
            // serit golgeleri listeyi kirli gosterir.
            UiKit.DikeyDizi(_kok, 16f, 0);
        }

        public void Goster(string mesaj, bool uyari = false, float sure = 3f)
        {
            if (string.IsNullOrEmpty(mesaj)) return;

            // Ust uste yigilma: eskiler dusurulur, yoksa ekran kapanir.
            while (_aktifler.Count >= MaksAyniAnda)
            {
                if (_aktifler[0] != null) Destroy(_aktifler[0]);
                _aktifler.RemoveAt(0);
            }

            // ⚠ Golge dizinin cocugu OLAMAZ: GolgeEkle kardes olarak ekler ve dikey
            // dizi onu ayri bir satir sayardi. Her serit kendi gorunmez kabugunda durur;
            // golge o kabugun icinde kalir.
            var kabuk = UiKit.Panel(_kok, "BildirimKabuk", Color.clear);
            UiKit.Boyut(kabuk.gameObject, 0f, SeritYukseklik);

            // Cercevesiz koyu serit, cimin koyu bolgelerinde kayboluyordu: okunurlugu
            // saglayan sey kapsulun kendisi degil, etrafindaki acik hattir.
            // ⚠ Uyari dolu kirmizi zemin degil KIRMIZI CERCEVE: goze carpar ama bagirmaz.
            // ⚠ Icerik "ic" yuzeyine girer; dis cerceveye eklenirse cercevenin uzerine tasar.
            RectTransform ic;
            var kart = UiKit.CerceveliKart(
                kabuk, "Bildirim", Palet.KapsulZemin,
                uyari ? Palet.UiKirmizi : Palet.KapsulCerceve, out ic, 20, uyari ? 4f : 3f);
            UiKit.Doldur(kart);
            // Toast dokunmayi yutarsa altindaki tarlaya basilamaz.
            kart.gameObject.GetComponent<Image>().raycastTarget = false;
            UiKit.GolgeEkle(kart, 20, 14, 4f);

            // Her oge ikon tasir: sekil, rengi ayirt edemeyen oyuncuda da bilgi/uyari
            // ayrimini korur.
            // ⚠ YER TUTUCU sekil — gercek ikonlar 2B varlik olarak sonra gelecek.
            var ikon = UiKit.Panel(ic, "Ikon", uyari ? Palet.UiKirmizi : Palet.UiAltin);
            var ikonImg = ikon.gameObject.GetComponent<Image>();
            ikonImg.sprite = uyari ? Sekil.Simsek() : Sekil.Yildiz();
            ikonImg.raycastTarget = false;
            UiKit.Sabitle(ikon, 0f, 0.5f, 12f, 0f, 22f, 22f);

            // Koyu kapsulde yazi BEYAZ ve KONTURLU: uyari seridi digerlerinden yazi
            // rengiyle degil cerceve ve ikonla ayrisir, boylece her mesaj ayni
            // okunurlukta kalir.
            var yazi = UiKit.KonturluYazi(
                ic, "Yazi", mesaj, UiKit.YaziGovde, Color.white, TextAnchor.MiddleCenter, true, 1.3f);
            var yaziRt = yazi.rectTransform;
            yaziRt.anchorMin = Vector2.zero;
            yaziRt.anchorMax = Vector2.one;
            // Soldaki bosluk ikonun yeridir; Doldur ile esit kenar verilirse yazi ikonun
            // ustune binerdi.
            yaziRt.offsetMin = new Vector2(42f, 6f);
            yaziRt.offsetMax = new Vector2(-16f, -6f);

            _aktifler.Add(kabuk.gameObject);
            StartCoroutine(Soldur(kabuk.gameObject, sure));
        }

        private IEnumerator Soldur(GameObject kart, float sure)
        {
            yield return new WaitForSecondsRealtime(sure);
            if (kart != null)
            {
                _aktifler.Remove(kart);
                Destroy(kart);
            }
        }
    }

    /// <summary>
    /// "Baglanti yok" perdesi (C.4).
    ///
    /// ⚠ Perde <b>20 eylem dolunca veya 60 saniye gecince</b> iner ve oyun durur.
    /// Oncesinde oyuncu kendi parselinde calismaya devam edebilir: "kendi parselinde
    /// basladigin isi bitirebilirsin, paylasilan duruma dokunan hicbir seyi yapamazsin."
    /// </summary>
    public class BaglantiPerdesi : MonoBehaviour
    {
        private RectTransform _kok;
        private Text _durumYazi;
        private Text _kuyrukYazi;

        private const float KartGenislik = 520f;
        private const float KartYukseklik = 196f;

        public void Kur(Transform kanvas)
        {
            // Perde tam ekran karartma — raycast'i YUTAR, oyun gercekten durmalidir.
            _kok = UiKit.Panel(kanvas, "BaglantiPerdesi", new Color(0.04f, 0.035f, 0.03f, 0.82f));
            UiKit.Doldur(_kok);

            // Mesaj bosluga asili yaziyken "oyun coktu" gibi duruyordu; kart onu
            // bilincli bir duraklama haline getiriyor.
            // ⚠ Kart dunyanin uzerinde yuzer -> HUD KAPSUL dili (B.22): koyu govde +
            // krem cerceve. Parsomen degil; parsomen oyuncunun actigi modallarindir.
            // ⚠ Icerik "ic" yuzeyine girer, dis cerceveye degil.
            RectTransform ic;
            var kart = UiKit.CerceveliKart(
                _kok, "Kart", Palet.KapsulZemin, Palet.KapsulCerceve, out ic, 24, 4f);
            UiKit.Sabitle(kart, 0.5f, 0.5f, 0f, 0f, KartGenislik, KartYukseklik);
            UiKit.GolgeEkle(kart, 24, 26, 10f);

            // Rozet kartin ust kenarindan disari tasar — HUD'daki avatar halkasiyla ayni
            // dil; kesilen baglantiyi yazidan once bu isaret anlatir. Bilincli olarak
            // DIS cerceveye eklenir, yoksa tasma kirpilmis gibi durur.
            // ⚠ Simsek YER TUTUCU sekil — gercek ikon 2B varlik olarak sonra gelecek.
            var halka = UiKit.Panel(kart, "IkonHalka", Palet.KapsulCerceve);
            halka.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
            UiKit.Sabitle(halka, 0.5f, 1f, 0f, 34f, 58f, 58f);
            var ikon = UiKit.Panel(halka, "Ikon", Palet.UiKirmizi);
            ikon.gameObject.GetComponent<Image>().sprite = Sekil.Simsek();
            UiKit.Doldur(ikon, 14f);

            var baslik = UiKit.KonturluYazi(
                ic, "Baslik", "Bağlantı yok", UiKit.YaziBuyuk, Color.white, TextAnchor.MiddleCenter);
            UiKit.Sabitle(baslik.rectTransform, 0.5f, 1f, 0f, -22f, KartGenislik - 56f, 38f);

            var cizgi = UiKit.Ayirac(ic);
            UiKit.Sabitle(cizgi, 0.5f, 1f, 0f, -70f, KartGenislik - 56f, 1f);

            _durumYazi = UiKit.Yazi(
                ic, "Durum", "Yeniden bağlanıyor...", UiKit.YaziGovde, Palet.UiYaziSolgun,
                TextAnchor.MiddleCenter);
            UiKit.Sabitle(_durumYazi.rectTransform, 0.5f, 1f, 0f, -86f, KartGenislik - 56f, 30f);

            // Kuyruk sayisi altin: "islerin kayboldu" korkusunu bu satir dagitir. Konturlu,
            // cunku altin yazi koyu kapsulde tek basina kenarini kaybediyor.
            _kuyrukYazi = UiKit.KonturluYazi(
                ic, "Kuyruk", "", UiKit.YaziKucuk, Palet.UiAltin, TextAnchor.MiddleCenter, true, 1.2f);
            UiKit.Sabitle(_kuyrukYazi.rectTransform, 0.5f, 1f, 0f, -124f, KartGenislik - 56f, 44f);

            _kok.gameObject.SetActive(false);
        }

        public void Goster(int kuyruktakiEylem)
        {
            _kok.gameObject.SetActive(true);
            _kok.SetAsLastSibling();
            _kuyrukYazi.text = kuyruktakiEylem > 0
                ? kuyruktakiEylem + " işlem bağlantı dönünce gönderilecek."
                : "";
        }

        public void Gizle() => _kok.gameObject.SetActive(false);

        public void DurumYaz(string metin) => _durumYazi.text = metin;

        public bool Acik => _kok.gameObject.activeSelf;
    }

    /// <summary>
    /// Seviye atlama gosterimi. Ses kapaliyken de gorulmeli (B.26 yasak 17);
    /// haptik de ayrica tetiklenir (yukselen uclu desen).
    /// </summary>
    public class SeviyeAtlamaEkrani : MonoBehaviour
    {
        private RectTransform _kok;
        private Text _yazi;
        private Text _acilanYazi;

        private const float KartGenislik = 560f;
        private const float KartYukseklik = 224f;

        public void Kur(Transform kanvas)
        {
            _kok = UiKit.Panel(kanvas, "SeviyeAtlama", new Color(0.04f, 0.035f, 0.03f, 0.55f));
            UiKit.Doldur(_kok);
            // Kutlama oyunu DURDURMAZ: perde tiklamayi yutmaz, oyuncu calismaya devam eder.
            _kok.gameObject.GetComponent<Image>().raycastTarget = false;

            // Kapsul dili + ALTIN cerceve: altin bu arayuzde "onemli/secili" demektir
            // (B.22). Kutlama kartinin krem cerceveli sayaclardan ayrildigi yer burasi.
            // ⚠ Icerik "ic" yuzeyine girer, dis cerceveye degil.
            RectTransform ic;
            var kart = UiKit.CerceveliKart(
                _kok, "Kart", Palet.KapsulZemin, Palet.KapsulAltin, out ic, 24, 5f);
            UiKit.Sabitle(kart, 0.5f, 0.5f, 0f, 0f, KartGenislik, KartYukseklik);
            UiKit.GolgeEkle(kart, 24, 28, 10f);
            kart.gameObject.GetComponent<Image>().raycastTarget = false;

            // Yildiz rozeti kartin ust kenarindan tasar: kutlamanin ilk okunan isareti
            // sayi degil, bu rozettir.
            // ⚠ YER TUTUCU sekil — gercek ikon 2B varlik olarak sonra gelecek.
            var halka = UiKit.Panel(kart, "YildizHalka", Palet.KapsulAltin);
            var halkaImg = halka.gameObject.GetComponent<Image>();
            halkaImg.sprite = Sekil.Daire();
            halkaImg.raycastTarget = false;
            UiKit.Sabitle(halka, 0.5f, 1f, 0f, 36f, 58f, 58f);
            var yildiz = UiKit.Panel(halka, "Yildiz", Color.white);
            var yildizImg = yildiz.gameObject.GetComponent<Image>();
            yildizImg.sprite = Sekil.Yildiz();
            yildizImg.raycastTarget = false;
            UiKit.Doldur(yildiz, 12f);

            var etiket = UiKit.Yazi(
                ic, "Etiket", "seviye atladın", UiKit.YaziKucuk, Palet.UiYaziSolgun,
                TextAnchor.MiddleCenter);
            UiKit.Sabitle(etiket.rectTransform, 0.5f, 1f, 0f, -20f, KartGenislik - 56f, 24f);

            // Sayinin kendisi odak: kutlamanin tek iri ogesi bu satir olmali. Kontur
            // kalin, cunku 54 px altin yazi koyu zeminde kenarsiz kalinca eriyor.
            _yazi = UiKit.KonturluYazi(
                ic, "Yazi", "", UiKit.YaziDev, Palet.UiAltin, TextAnchor.MiddleCenter, true, 2.5f);
            UiKit.Sabitle(_yazi.rectTransform, 0.5f, 1f, 0f, -44f, KartGenislik - 56f, 66f);

            var cizgi = UiKit.Ayirac(ic);
            UiKit.Sabitle(cizgi, 0.5f, 1f, 0f, -122f, KartGenislik - 96f, 1f);

            _acilanYazi = UiKit.KonturluYazi(
                ic, "Acilan", "", UiKit.YaziGovde, Color.white, TextAnchor.UpperCenter, false, 1.2f);
            UiKit.Sabitle(_acilanYazi.rectTransform, 0.5f, 1f, 0f, -138f, KartGenislik - 56f, 58f);

            _kok.gameObject.SetActive(false);
        }

        public void Goster(int seviye, string acilan)
        {
            _yazi.text = "Seviye " + seviye;
            _acilanYazi.text = acilan;
            _kok.gameObject.SetActive(true);
            _kok.SetAsLastSibling();
            StopAllCoroutines();
            StartCoroutine(Kapat());
        }

        private IEnumerator Kapat()
        {
            yield return new WaitForSecondsRealtime(2.4f);
            _kok.gameObject.SetActive(false);
        }
    }

    /// <summary>Yukleme perdesi. Bolge gecisinde 0.5-1 sn kararma (B.4).</summary>
    public class YuklemeEkrani : MonoBehaviour
    {
        private RectTransform _kok;
        private Text _yazi;

        public void Kur(Transform kanvas)
        {
            // Tam kapatan perde; paletteki en koyu ton bile saydam oldugu icin burada
            // ilik koyu bir zemin acikca yaziliyor (GirisEkrani ile ayni aile).
            _kok = UiKit.Panel(kanvas, "Yukleme", new Color(0.085f, 0.075f, 0.065f, 1f));
            UiKit.Doldur(_kok);

            // Yazi bos ekranda yuzerken yukleme "donmus" gibi duruyordu; hap seklindeki
            // zemin ona bir yer veriyor. Hap, HUD kapsul dilinin kucuk hali: koyu govde +
            // krem cerceve (B.22). Icerik "ic" yuzeyine girer.
            RectTransform ic;
            var hap = UiKit.Hap(_kok, "Hap", Palet.KapsulZemin, Palet.KapsulCerceve, out ic);
            UiKit.Sabitle(hap, 0.5f, 0.5f, 0f, 0f, 420f, 64f);
            UiKit.GolgeEkle(hap, 22, 18, 6f);

            // Ikon hapin solunda sabit durur — donen bir gosterge YOK: bekleme suresi
            // 0.5-1 sn (B.4), donen carkin farkedilecek zamani bile olmuyor.
            // ⚠ YER TUTUCU sekil — gercek ikon 2B varlik olarak sonra gelecek.
            var ikon = UiKit.Panel(ic, "Ikon", Palet.UiAltin);
            var ikonImg = ikon.gameObject.GetComponent<Image>();
            ikonImg.sprite = Sekil.Yildiz();
            ikonImg.raycastTarget = false;
            UiKit.Sabitle(ikon, 0f, 0.5f, 16f, 0f, 26f, 26f);

            _yazi = UiKit.KonturluYazi(
                ic, "Yazi", "Yükleniyor...", UiKit.YaziGovde, Color.white, TextAnchor.MiddleCenter,
                true, 1.3f);
            var yaziRt = _yazi.rectTransform;
            yaziRt.anchorMin = Vector2.zero;
            yaziRt.anchorMax = Vector2.one;
            // Soldaki bosluk ikonun yeri; esit kenarli Doldur yaziyi ikonun ustune bindirirdi.
            yaziRt.offsetMin = new Vector2(50f, 8f);
            yaziRt.offsetMax = new Vector2(-18f, -8f);
        }

        public void Goster(string metin)
        {
            _yazi.text = metin;
            _kok.gameObject.SetActive(true);
            _kok.SetAsLastSibling();
        }

        public void Gizle() => _kok.gameObject.SetActive(false);
    }
}
