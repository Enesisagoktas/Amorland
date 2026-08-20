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
    /// </summary>
    public class Bildirimler : MonoBehaviour
    {
        private RectTransform _kok;
        private readonly List<GameObject> _aktifler = new List<GameObject>();
        private const int MaksAyniAnda = 3;

        public void Kur(Transform kanvas)
        {
            _kok = UiKit.Panel(kanvas, "Bildirimler", Color.clear);
            UiKit.Sabitle(_kok, 0.5f, 1f, 0f, -90f, 700f, 200f);
            UiKit.DikeyDizi(_kok, 6f, 0);
            _kok.gameObject.GetComponent<Image>().raycastTarget = false;
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

            var kart = UiKit.Panel(_kok, "Bildirim", uyari ? Palet.UiUyari : Palet.UiZemin);
            UiKit.Boyut(kart.gameObject, 0f, 44f);
            kart.gameObject.GetComponent<Image>().raycastTarget = false;

            var yazi = UiKit.Yazi(kart, "Yazi", mesaj, 17, Palet.UiYazi, TextAnchor.MiddleCenter);
            UiKit.Doldur(yazi.rectTransform, 10f);

            _aktifler.Add(kart.gameObject);
            StartCoroutine(Soldur(kart.gameObject, sure));
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

        public void Kur(Transform kanvas)
        {
            _kok = UiKit.Panel(kanvas, "BaglantiPerdesi", new Color(0f, 0f, 0f, 0.82f));
            UiKit.Doldur(_kok);

            var baslik = UiKit.Yazi(
                _kok, "Baslik", "Baglanti yok", 34, Palet.UiYazi, TextAnchor.MiddleCenter);
            UiKit.Sabitle(baslik.rectTransform, 0.5f, 0.5f, 0f, 40f, 600f, 50f);

            _durumYazi = UiKit.Yazi(
                _kok, "Durum", "Yeniden baglaniyor...", 18, Palet.UiYaziSolgun, TextAnchor.MiddleCenter);
            UiKit.Sabitle(_durumYazi.rectTransform, 0.5f, 0.5f, 0f, -6f, 700f, 40f);

            _kuyrukYazi = UiKit.Yazi(
                _kok, "Kuyruk", "", 16, Palet.UiVurgu, TextAnchor.MiddleCenter);
            UiKit.Sabitle(_kuyrukYazi.rectTransform, 0.5f, 0.5f, 0f, -44f, 700f, 30f);

            _kok.gameObject.SetActive(false);
        }

        public void Goster(int kuyruktakiEylem)
        {
            _kok.gameObject.SetActive(true);
            _kok.SetAsLastSibling();
            _kuyrukYazi.text = kuyruktakiEylem > 0
                ? kuyruktakiEylem + " islem baglanti donunce gonderilecek."
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

        public void Kur(Transform kanvas)
        {
            _kok = UiKit.Panel(kanvas, "SeviyeAtlama", new Color(0f, 0f, 0f, 0.6f));
            UiKit.Doldur(_kok);
            _kok.gameObject.GetComponent<Image>().raycastTarget = false;

            _yazi = UiKit.Yazi(_kok, "Yazi", "", 46, Palet.UiVurgu, TextAnchor.MiddleCenter);
            UiKit.Sabitle(_yazi.rectTransform, 0.5f, 0.5f, 0f, 20f, 700f, 60f);

            _acilanYazi = UiKit.Yazi(_kok, "Acilan", "", 20, Palet.UiYazi, TextAnchor.MiddleCenter);
            UiKit.Sabitle(_acilanYazi.rectTransform, 0.5f, 0.5f, 0f, -30f, 700f, 40f);

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
            _kok = UiKit.Panel(kanvas, "Yukleme", new Color(0.06f, 0.07f, 0.08f, 1f));
            UiKit.Doldur(_kok);
            _yazi = UiKit.Yazi(_kok, "Yazi", "Yukleniyor...", 22, Palet.UiYaziSolgun, TextAnchor.MiddleCenter);
            UiKit.Doldur(_yazi.rectTransform);
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
