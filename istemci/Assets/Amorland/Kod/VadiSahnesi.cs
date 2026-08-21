using UnityEngine;
using UnityEngine.UI;
using Amorland.Arayuz;
using Amorland.Gorsel;

namespace Amorland
{
    /// <summary>
    /// Vadi haritasi sahnesi — oyunun acilisi.
    ///
    /// Haritayi kurar, gezinme kamerasini takar ve bolgelerin uzerine adlarini yazar.
    /// Sunucu GEREKTIRMEZ: <see cref="Oyun"/> acilista sunucuya baglanir ve baglanti
    /// yoksa hicbir sey gorunmez; vadi tek basina calisir, boylece harita her zaman
    /// gezilebilir.
    ///
    /// Spec B.4: vadide 7 bolge var ve bolgeye dokununca o sahneye girilir. Su an
    /// dokunma bagli DEGIL — once haritanin kendisi isteniyordu. Bolge kayitlari
    /// (<see cref="VadiHaritasi.Bolgeler"/>) hazir; tiklama eklendiginde buradan
    /// <c>Oyun.BolgeyeGit</c> cagrilacak.
    /// </summary>
    public class VadiSahnesi : MonoBehaviour
    {
        /// <summary>Acilista kameranin baktigi yer: ciftin evi.</summary>
        private const string BaslangicBolgesi = "ev";
        private const float BaslangicYakinlik = 11f;

        private VadiHaritasi _harita;
        private HaritaGezgini _gezgin;
        private Camera _kamera;
        private Text _bolgeYazisi;

        private void Start() => Kur();

        /// <summary>
        /// Sahneyi kurar. Start ayri degil: editor dogrulama araci Play moduna girmeden
        /// AYNI yolu calistirabilsin diye disari acildi — yoksa arac sahnenin bir
        /// kopyasini kurar ve gercekte kirik olani yesil gosterir.
        /// </summary>
        public void Kur()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.AutoRotation;

            HaritayiKur();
            KamerayiKur();
            EtiketleriKur();
            ArayuzuKur();
        }

        private void HaritayiKur()
        {
            var go = new GameObject("VadiHaritasi");
            go.transform.SetParent(transform, false);
            _harita = go.AddComponent<VadiHaritasi>();
            _harita.Kur();
        }

        private void KamerayiKur()
        {
            var go = new GameObject("Kamera");
            go.transform.SetParent(transform, false);
            _kamera = go.AddComponent<Camera>();
            _kamera.orthographic = true;
            _kamera.nearClipPlane = -50f;
            _kamera.farClipPlane = 50f;
            _kamera.clearFlags = CameraClearFlags.SolidColor;
            // Harita disinda kalan alan: koyu yesil, vadinin disi.
            _kamera.backgroundColor = new Color(0.13f, 0.20f, 0.14f, 1f);

            _gezgin = go.AddComponent<HaritaGezgini>();

            Vector2 baslangic = new Vector2(VadiHaritasi.En / 2f, VadiHaritasi.Boy / 2f);
            foreach (var b in _harita.Bolgeler)
                if (b.Ad == BaslangicBolgesi) baslangic = b.Merkez;
            _gezgin.Odakla(baslangic, BaslangicYakinlik);
        }

        /// <summary>
        /// Bolge adlarini haritanin uzerine yazar.
        ///
        /// ⚠ Dunya uzayinda kanvas: etiketler haritayla birlikte kayar ve yakinlasinca
        /// buyur. Ekran uzayinda sabit dursalardi kamera kaydikca hangi etiketin hangi
        /// bolgeye ait oldugu kaybolurdu.
        /// </summary>
        private void EtiketleriKur()
        {
            var go = new GameObject("BolgeEtiketleri");
            go.transform.SetParent(transform, false);
            var kanvas = go.AddComponent<Canvas>();
            kanvas.renderMode = RenderMode.WorldSpace;
            kanvas.sortingOrder = 5;
            var rt = kanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(VadiHaritasi.En, VadiHaritasi.Boy);
            rt.position = new Vector3(VadiHaritasi.En / 2f, VadiHaritasi.Boy / 2f, -1f);

            foreach (var b in _harita.Bolgeler)
            {
                // Etiket, bolgenin GUNEY kenarinin altina oturur: bolgenin uzerini
                // kapatmaz, girisin yaninda durur.
                var kart = UiKit.Kart(rt, "Etiket_" + b.Ad, new Color(0.13f, 0.10f, 0.08f, 0.82f), 10);
                kart.sizeDelta = new Vector2(26f, 7f);
                kart.localScale = Vector3.one * 0.32f;
                kart.localPosition = new Vector3(b.Merkez.x - VadiHaritasi.En / 2f,
                                                 b.Alan.yMin - VadiHaritasi.Boy / 2f - 4f, 0f);
                kart.gameObject.GetComponent<Image>().raycastTarget = false;

                var yazi = UiKit.Yazi(kart, "Yazi", b.Baslik, 22, Palet.UiYazi,
                                      TextAnchor.MiddleCenter, true);
                UiKit.Doldur(yazi.rectTransform, 1f);
            }
        }

        private void ArayuzuKur()
        {
            var kanvas = UiKit.Kanvas("VadiArayuzu", 40);
            kanvas.transform.SetParent(transform, false);

            // Ust seride vadi adi ve o an bakilan bolge.
            var ust = UiKit.Kart(kanvas.transform, "Baslik", new Color(0.13f, 0.10f, 0.08f, 0.85f), 14);
            ust.anchorMin = new Vector2(0.5f, 1f);
            ust.anchorMax = new Vector2(0.5f, 1f);
            ust.pivot = new Vector2(0.5f, 1f);
            ust.anchoredPosition = new Vector2(0f, -14f);
            ust.sizeDelta = new Vector2(340f, 46f);

            _bolgeYazisi = UiKit.Yazi(ust, "Yazi", "Amorland Vadisi", UiKit.YaziBaslik,
                                      Palet.UiYazi, TextAnchor.MiddleCenter, true);
            UiKit.Doldur(_bolgeYazisi.rectTransform, 8f);

            // Alt seride kontrol ipucu.
            // ⚠ Kapsul ustunde: ilk denemede ciplak yaziydi ve acik yesil cimin
            // uzerinde solgun krem renk neredeyse okunmuyordu. Ustelik ekranin en
            // dibine yapisikti; harita her karede degistigi icin sabit bir zemin sart.
            var ipucuKart = UiKit.Kart(kanvas.transform, "Ipucu",
                                       new Color(0.13f, 0.10f, 0.08f, 0.72f), 12);
            ipucuKart.anchorMin = new Vector2(0.5f, 0f);
            ipucuKart.anchorMax = new Vector2(0.5f, 0f);
            ipucuKart.pivot = new Vector2(0.5f, 0f);
            ipucuKart.anchoredPosition = new Vector2(0f, 26f);
            ipucuKart.sizeDelta = new Vector2(560f, 34f);
            ipucuKart.gameObject.GetComponent<Image>().raycastTarget = false;

            var ipucu = UiKit.Yazi(ipucuKart, "Yazi",
                                   "Sürükle veya WASD ile gez  ·  tekerlek / çimdik ile yakınlaş",
                                   UiKit.YaziKucuk, Palet.UiYazi, TextAnchor.MiddleCenter);
            UiKit.Doldur(ipucu.rectTransform, 6f);

            var buton = UiKit.YesilButon(kanvas.transform, "OyunaGir", "Oyuna gir", OyunaGec);
            var brt = buton.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.pivot = new Vector2(1f, 0f);
            brt.anchoredPosition = new Vector2(-16f, 16f);
            brt.sizeDelta = new Vector2(168f, 54f);
        }

        private void Update()
        {
            if (_bolgeYazisi == null || _kamera == null) return;

            // Kameranin ortasi hangi bolgenin uzerinde? Baslik onu soyler.
            Vector3 p = _kamera.transform.position;
            var nokta = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
            string ad = "Amorland Vadisi";
            foreach (var b in _harita.Bolgeler)
            {
                if (!b.Alan.Contains(nokta)) continue;
                ad = b.Baslik;
                break;
            }
            if (_bolgeYazisi.text != ad) _bolgeYazisi.text = ad;
        }

        private void OyunaGec() => Onyukleyici.OyunaGec();
    }
}
