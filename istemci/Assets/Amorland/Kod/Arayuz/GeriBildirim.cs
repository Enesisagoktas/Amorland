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
    /// zaman koyudur; uyari, ince bir halka ve yazi rengiyle ayrisir.
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
            // HUD'un sol/sag ust seritleri -14'ten baslayip 66 px iniyor; -92 onlarin
            // hemen altina denk gelir ve ustlerine binmez.
            UiKit.Sabitle(_kok, 0.5f, 1f, 0f, -92f, SeritGenislik, 240f);
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

            var kart = UiKit.Kart(kabuk, "Bildirim", Palet.UiZemin, UiKit.YaricapKucuk);
            UiKit.Doldur(kart);
            // Toast dokunmayi yutarsa altindaki tarlaya basilamaz.
            kart.gameObject.GetComponent<Image>().raycastTarget = false;
            UiKit.GolgeEkle(kart, UiKit.YaricapKucuk, 14, 4f);

            // Uyari dolu kirmizi zemin degil ince halka: goze carpar ama bagirmaz.
            if (uyari) UiKit.Cerceve(kart, "UyariHalkasi", Palet.UiUyari, UiKit.YaricapKucuk, 2);

            var yazi = UiKit.Yazi(
                kart, "Yazi", mesaj, UiKit.YaziGovde,
                uyari ? Palet.UiUyari : Palet.UiYazi, TextAnchor.MiddleCenter, true);
            UiKit.Doldur(yazi.rectTransform, 18f);

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
            var kart = UiKit.Kart(_kok, "Kart", Palet.UiPanel, 20);
            UiKit.Sabitle(kart, 0.5f, 0.5f, 0f, 0f, KartGenislik, KartYukseklik);
            UiKit.GolgeEkle(kart, 20, 26, 10f);

            var baslik = UiKit.Yazi(
                kart, "Baslik", "Bağlantı yok", UiKit.YaziBuyuk, Palet.UiYazi, TextAnchor.MiddleCenter, true);
            UiKit.Sabitle(baslik.rectTransform, 0.5f, 1f, 0f, -24f, KartGenislik - 56f, 38f);

            var cizgi = UiKit.Ayirac(kart);
            UiKit.Sabitle(cizgi, 0.5f, 1f, 0f, -72f, KartGenislik - 56f, 1f);

            _durumYazi = UiKit.Yazi(
                kart, "Durum", "Yeniden bağlanıyor...", UiKit.YaziGovde, Palet.UiYaziSolgun,
                TextAnchor.MiddleCenter);
            UiKit.Sabitle(_durumYazi.rectTransform, 0.5f, 1f, 0f, -88f, KartGenislik - 56f, 30f);

            // Kuyruk sayisi vurgu renginde: "islerin kayboldu" korkusunu bu satir dagitir.
            _kuyrukYazi = UiKit.Yazi(
                kart, "Kuyruk", "", UiKit.YaziKucuk, Palet.UiVurgu, TextAnchor.MiddleCenter);
            UiKit.Sabitle(_kuyrukYazi.rectTransform, 0.5f, 1f, 0f, -126f, KartGenislik - 56f, 44f);

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

            var kart = UiKit.Kart(_kok, "Kart", Palet.UiPanel, 20);
            UiKit.Sabitle(kart, 0.5f, 0.5f, 0f, 0f, KartGenislik, KartYukseklik);
            UiKit.GolgeEkle(kart, 20, 28, 10f);
            kart.gameObject.GetComponent<Image>().raycastTarget = false;

            var etiket = UiKit.Yazi(
                kart, "Etiket", "seviye atladın", UiKit.YaziKucuk, Palet.UiYaziSolgun,
                TextAnchor.MiddleCenter);
            UiKit.Sabitle(etiket.rectTransform, 0.5f, 1f, 0f, -22f, KartGenislik - 56f, 24f);

            // Sayinin kendisi odak: kutlamanin tek iri ogesi bu satir olmali.
            _yazi = UiKit.Yazi(
                kart, "Yazi", "", UiKit.YaziDev, Palet.UiVurgu, TextAnchor.MiddleCenter, true);
            UiKit.Sabitle(_yazi.rectTransform, 0.5f, 1f, 0f, -48f, KartGenislik - 56f, 66f);

            var cizgi = UiKit.Ayirac(kart);
            UiKit.Sabitle(cizgi, 0.5f, 1f, 0f, -126f, KartGenislik - 96f, 1f);

            _acilanYazi = UiKit.Yazi(
                kart, "Acilan", "", UiKit.YaziGovde, Palet.UiYazi, TextAnchor.UpperCenter);
            UiKit.Sabitle(_acilanYazi.rectTransform, 0.5f, 1f, 0f, -142f, KartGenislik - 56f, 58f);

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
            // zemin ona bir yer veriyor.
            var hap = UiKit.Kart(_kok, "Hap", Palet.UiZemin, UiKit.YaricapKucuk);
            UiKit.Sabitle(hap, 0.5f, 0.5f, 0f, 0f, 420f, 64f);
            UiKit.GolgeEkle(hap, UiKit.YaricapKucuk, 18, 6f);

            _yazi = UiKit.Yazi(
                hap, "Yazi", "Yükleniyor...", UiKit.YaziGovde, Palet.UiYazi, TextAnchor.MiddleCenter, true);
            UiKit.Doldur(_yazi.rectTransform, 18f);
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
