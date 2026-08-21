using System.Collections.Generic;
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
    /// Spec B.4: bolgeye dokununca o sahneye girilir. Dokunma BAGLI: bolgeye
    /// tiklaninca kamera yumusakca yaklasir ve bolge karti acilir; "Bolgeye gir"
    /// istegi <see cref="Onyukleyici.IstenenBolge"/> uzerinden Oyun'a tasinir,
    /// oturum kurulunca <c>Oyun.BolgeyeGit</c> calisir.
    ///
    /// ⚠ Dokunma ile surukleme ayrimi PIKSELLE yapilir: parmak 14 px'ten az
    /// oynadiysa dokunmadir. Sure sartina bakilmaz — haritaya bakarken parmagini
    /// basili tutan biri hala "dokunmus" sayilmali.
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

        // Bolge karti
        private RectTransform _kart;
        private Text _kartBaslik;
        private Text _kartBilgi;
        private string _seciliBolge;

        // Dokunma takibi
        private bool _temasVar;
        private Vector2 _temasBaslangic;
        private bool _surukleOldu;

        // Tren
        private Transform _tren;
        private const float TrenHizi = 5f;

        /// <summary>
        /// Trenin dikey konumu — GOZLE DEGIL PIKSELLE hizalandi.
        ///
        /// Ray karosu dunya y [36,37) araligini kaplar; goruntu satiri r, dunya
        /// y = 37 - r/64'e denk gelir. Olcum: alcak (yakin) rayin parlak sirti
        /// 45-46. satirlarda, tum araclarin teker alti 60. satirda. Teker raya
        /// ~2 px binerek otursun diye temas satiri 42 secildi:
        ///   y_ray   = 37 - 42/64          = 36.344
        ///   y_merkez= y_ray + (60-32)/64  = 36.78   (pivot sprite ortasi)
        /// 36.5 (karo ortasi) kullanilinca tekerler rayin ALTINDA yuzuyordu.
        /// </summary>
        private const float TrenY = 36.78f;

        // Kelebekler: cicekli cayirlarda gezinen minik canlilik.
        private readonly List<Transform> _kelebekler = new List<Transform>();
        private readonly List<Vector2> _kelebekYuva = new List<Vector2>();
        private readonly List<Sprite> _kanatAcikListe = new List<Sprite>();
        private readonly List<Sprite> _kanatKapaliListe = new List<Sprite>();
        private float _kanatZamani;

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
            TrenKur();
            KelebekleriKur();
        }

        /// <summary>
        /// Dort kelebek, cicekli cayir noktalarina yuvalanir; Lissajous egrisiyle
        /// yuva cevresinde suzulur, kanatlar iki karelik cirpma ile animasyonlanir.
        /// </summary>
        private void KelebekleriKur()
        {
            Sprite tAcik = Karo64.Al(Sayfa64.Ek, Karo64.KelebekAcik);
            Sprite tKapali = Karo64.Al(Sayfa64.Ek, Karo64.KelebekKapali);
            Sprite mAcik = Karo64.Al(Sayfa64.Ek, Karo64.KelebekMaviAcik);
            Sprite mKapali = Karo64.Al(Sayfa64.Ek, Karo64.KelebekMaviKapali);

            Vector2[] yuvalar =
            {
                new Vector2(250f, 195f), new Vector2(215f, 118f),
                new Vector2(430f, 152f), new Vector2(298f, 104f),
            };
            var kok = new GameObject("Kelebekler");
            kok.transform.SetParent(transform, false);
            for (int i = 0; i < yuvalar.Length; i++)
            {
                var go = new GameObject("Kelebek" + i);
                go.transform.SetParent(kok.transform, false);
                go.transform.position = new Vector3(yuvalar[i].x, yuvalar[i].y, 0f);
                go.transform.localScale = Vector3.one * 0.8f;
                var sr = go.AddComponent<SpriteRenderer>();
                bool mavi = i % 2 == 1;
                sr.sprite = mavi ? mAcik : tAcik;
                sr.sortingOrder = 4;
                _kelebekler.Add(go.transform);
                _kelebekYuva.Add(yuvalar[i]);
                _kanatAcikListe.Add(mavi ? mAcik : tAcik);
                _kanatKapaliListe.Add(mavi ? mKapali : tKapali);
            }
        }

        /// <summary>
        /// Hatta gezen tren: lokomotif + iki vagon. Salt dekor — vadiye hayat verir.
        /// Karo degil serbest sprite: karolar hucreye kilitli, tren akmali.
        /// </summary>
        private void TrenKur()
        {
            var kok = new GameObject("Tren");
            kok.transform.SetParent(transform, false);
            _tren = kok.transform;

            int[] parcalar = { Karo64.Lokomotif, Karo64.Vagon, Karo64.VagonKasa };
            Sprite golge = Karo64.Al(Sayfa64.Ek, Karo64.Golge);
            for (int i = 0; i < parcalar.Length; i++)
            {
                var go = new GameObject(i == 0 ? "Lokomotif" : "Vagon" + i);
                go.transform.SetParent(kok.transform, false);
                // Lokomotif onde (sagda), vagonlar arkasinda.
                go.transform.localPosition = new Vector3(-i * 1.02f, 0f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Karo64.Al(Sayfa64.Ek, parcalar[i]);
                // Zemin katmani 0, dekor 1 — tren nesnelerle ayni duzlemde (2).
                sr.sortingOrder = 2;

                var g = new GameObject("Golge");
                g.transform.SetParent(go.transform, false);
                g.transform.localPosition = new Vector3(0f, -0.12f, 0f);
                var gsr = g.AddComponent<SpriteRenderer>();
                gsr.sprite = golge;
                gsr.sortingOrder = 1;
            }
            // Bacadan duman: lokomotifin ustunde iki puf, geride kalir.
            var d1 = new GameObject("Duman1");
            d1.transform.SetParent(kok.transform, false);
            d1.transform.localPosition = new Vector3(0.34f, 0.9f, 0f);
            d1.AddComponent<SpriteRenderer>().sprite = Karo64.Al(Sayfa64.Ek, Karo64.DumanKucuk);
            d1.GetComponent<SpriteRenderer>().sortingOrder = 3;
            var d2 = new GameObject("Duman2");
            d2.transform.SetParent(kok.transform, false);
            d2.transform.localPosition = new Vector3(-0.35f, 1.4f, 0f);
            var d2sr = d2.AddComponent<SpriteRenderer>();
            d2sr.sprite = Karo64.Al(Sayfa64.Ek, Karo64.Duman);
            d2sr.color = new Color(1f, 1f, 1f, 0.7f);
            d2sr.sortingOrder = 3;
            // Gar yakininda baslar; bassiz kare de treni peronda gosterir.
            _tren.position = new Vector3(352f, TrenY, 0f);
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
                //
                // ⚠ Olcek/punto dengesi: kart PIKSEL boyutunda tutulur (300x72) ve
                // localScale ile dunya birimine indirilir. Ilk surum karti dogrudan
                // dunya biriminde (26x7) acmisti; 30 puntoluk yazi 7 birimlik karta
                // sigmayip tasti ve ekranda dev bulanik harfler yuzuyordu.
                var dis = UiKit.Kart(rt, "Etiket_" + b.Ad, Palet.UiYazi, 26);
                dis.sizeDelta = new Vector2(300f, 74f);
                dis.localScale = Vector3.one * 0.028f;
                dis.localPosition = new Vector3(b.Merkez.x - VadiHaritasi.En / 2f,
                                                b.Alan.yMin - VadiHaritasi.Boy / 2f - 3f, 0f);
                dis.gameObject.GetComponent<Image>().raycastTarget = false;
                var kart = UiKit.Kart(dis, "Ic", new Color(0.13f, 0.10f, 0.08f, 0.92f), 22);
                kart.anchorMin = Vector2.zero; kart.anchorMax = Vector2.one;
                kart.offsetMin = new Vector2(4f, 4f); kart.offsetMax = new Vector2(-4f, -4f);
                kart.gameObject.GetComponent<Image>().raycastTarget = false;

                var yazi = UiKit.Yazi(kart, "Yazi", b.Baslik, 34, Palet.UiYazi,
                                      TextAnchor.MiddleCenter, true);
                UiKit.Doldur(yazi.rectTransform, 4f);
            }
        }

        private void ArayuzuKur()
        {
            var kanvas = UiKit.Kanvas("VadiArayuzu", 40);
            kanvas.transform.SetParent(transform, false);

            // Ust seride vadi adi ve o an bakilan bolge.
            // HUD dili: koyu kapsul + KREM CERCEVE (proje arayuz kurali).
            var ustDis = UiKit.Kart(kanvas.transform, "Baslik", Palet.UiYazi, 16);
            ustDis.anchorMin = new Vector2(0.5f, 1f);
            ustDis.anchorMax = new Vector2(0.5f, 1f);
            ustDis.pivot = new Vector2(0.5f, 1f);
            ustDis.anchoredPosition = new Vector2(0f, -14f);
            ustDis.sizeDelta = new Vector2(340f, 48f);
            ustDis.gameObject.GetComponent<Image>().raycastTarget = false;
            var ust = UiKit.Kart(ustDis, "Ic", new Color(0.13f, 0.10f, 0.08f, 0.96f), 13);
            ust.anchorMin = Vector2.zero; ust.anchorMax = Vector2.one;
            ust.offsetMin = new Vector2(3f, 3f); ust.offsetMax = new Vector2(-3f, -3f);
            ust.gameObject.GetComponent<Image>().raycastTarget = false;

            _bolgeYazisi = UiKit.Yazi(ust, "Yazi", "Amorland Vadisi", UiKit.YaziBaslik,
                                      Palet.UiYazi, TextAnchor.MiddleCenter, true);
            UiKit.Doldur(_bolgeYazisi.rectTransform, 6f);

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

            var buton = UiKit.YesilButon(kanvas.transform, "OyunaGir", "Oyuna gir", () => OyunaGec(null));
            var brt = buton.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.pivot = new Vector2(1f, 0f);
            brt.anchoredPosition = new Vector2(-16f, 16f);
            brt.sizeDelta = new Vector2(168f, 54f);

            BolgeKartiKur(kanvas.transform);
        }

        /// <summary>
        /// Bolge karti: dokunulan bolgenin adi, seviyesi ve giris dugmesi.
        /// HUD dili: koyu kapsul + krem yazi (bkz. arayuz dili notu).
        /// </summary>
        private void BolgeKartiKur(Transform ebeveyn)
        {
            var kartDis = UiKit.Kart(ebeveyn, "BolgeKarti", Palet.UiYazi, 18);
            kartDis.anchorMin = new Vector2(0.5f, 0f);
            kartDis.anchorMax = new Vector2(0.5f, 0f);
            kartDis.pivot = new Vector2(0.5f, 0f);
            kartDis.anchoredPosition = new Vector2(0f, 70f);
            kartDis.sizeDelta = new Vector2(366f, 134f);
            _kart = UiKit.Kart(kartDis, "Ic", new Color(0.13f, 0.10f, 0.08f, 0.96f), 15);
            _kart.anchorMin = Vector2.zero; _kart.anchorMax = Vector2.one;
            _kart.offsetMin = new Vector2(3f, 3f); _kart.offsetMax = new Vector2(-3f, -3f);

            _kartBaslik = UiKit.Yazi(_kart, "Baslik", "", UiKit.YaziBaslik,
                                     Palet.UiYazi, TextAnchor.UpperCenter, true);
            var bas = _kartBaslik.rectTransform;
            bas.anchorMin = new Vector2(0f, 1f); bas.anchorMax = new Vector2(1f, 1f);
            bas.pivot = new Vector2(0.5f, 1f);
            bas.anchoredPosition = new Vector2(0f, -10f);
            bas.sizeDelta = new Vector2(0f, 30f);

            _kartBilgi = UiKit.Yazi(_kart, "Bilgi", "", UiKit.YaziKucuk,
                                    Palet.UiYaziSolgun, TextAnchor.UpperCenter);
            var blg = _kartBilgi.rectTransform;
            blg.anchorMin = new Vector2(0f, 1f); blg.anchorMax = new Vector2(1f, 1f);
            blg.pivot = new Vector2(0.5f, 1f);
            blg.anchoredPosition = new Vector2(0f, -40f);
            blg.sizeDelta = new Vector2(0f, 22f);

            var gir = UiKit.YesilButon(_kart, "Gir", "Bölgeye gir", () => OyunaGec(_seciliBolge));
            var grt = gir.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0f, 0f); grt.anchorMax = new Vector2(0f, 0f);
            grt.pivot = new Vector2(0f, 0f);
            grt.anchoredPosition = new Vector2(14f, 12f);
            grt.sizeDelta = new Vector2(196f, 50f);

            var kapat = UiKit.DuzButon(_kart, "Kapat", "Kapat", KartKapat);
            var krt = kapat.GetComponent<RectTransform>();
            krt.anchorMin = new Vector2(1f, 0f); krt.anchorMax = new Vector2(1f, 0f);
            krt.pivot = new Vector2(1f, 0f);
            krt.anchoredPosition = new Vector2(-14f, 12f);
            krt.sizeDelta = new Vector2(110f, 50f);

            _kart.parent.gameObject.SetActive(false);
        }

        /// <summary>
        /// Adiyla bolge kartini acar. Iki kullanici var: bassiz dogrulama araci
        /// (dokunma similasyonu yerine) ve ileride bildirimden derin baglanti.
        /// </summary>
        public void BolgeyiGoster(string ad)
        {
            foreach (var b in _harita.Bolgeler)
            {
                if (b.Ad != ad) continue;
                BolgeSec(b);
                _gezgin.Odakla(b.Merkez, 10f);   // bassiz karede animasyon yok, aninda otur
                return;
            }
        }

        private void KartKapat()
        {
            _seciliBolge = null;
            if (_kart != null) _kart.parent.gameObject.SetActive(false);
        }

        private void BolgeSec(VadiHaritasi.Bolge b)
        {
            _seciliBolge = b.Ad;
            _kartBaslik.text = b.Baslik;
            _kartBilgi.text = "Seviye " + b.Seviye + " bölgesi";
            _kart.parent.gameObject.SetActive(true);
            // Kamera bolgeye yumusakca yaklasir; kadraj icerigin uzerine oturur.
            _gezgin.YumusakOdakla(b.Merkez, 10f);
        }

        private void Update()
        {
            TrenGuncelle();
            KelebekGuncelle();
            DokunmaIsle();

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

        private void TrenGuncelle()
        {
            if (_tren == null) return;
            Vector3 k = _tren.position;
            k.x += TrenHizi * Time.deltaTime;
            // Hattan cikinca bati ucundan tekrar girer (vagon payi 12 karo).
            if (k.x > VadiHaritasi.En + 12f) k.x = -12f;
            _tren.position = k;
        }

        private void KelebekGuncelle()
        {
            if (_kelebekler.Count == 0) return;
            _kanatZamani += Time.deltaTime;
            bool acik = Mathf.FloorToInt(_kanatZamani / 0.16f) % 2 == 0;
            float t = Time.time;
            for (int i = 0; i < _kelebekler.Count; i++)
            {
                Transform tr = _kelebekler[i];
                Vector2 yuva = _kelebekYuva[i];
                float f = 0.55f + i * 0.13f;
                // Lissajous: yuva cevresinde 8 cizerek suzulme.
                tr.position = new Vector3(
                    yuva.x + Mathf.Sin(t * f) * 3.2f,
                    yuva.y + Mathf.Sin(t * f * 1.7f + i) * 2.1f, 0f);
                var sr = tr.GetComponent<SpriteRenderer>();
                sr.sprite = acik ? _kanatAcikListe[i] : _kanatKapaliListe[i];
            }
        }

        /// <summary>
        /// Dokunmayi (surukleme olmadan basip birakma) bolge secimine cevirir.
        /// </summary>
        private void DokunmaIsle()
        {
            Girdi.Girdi.Temas[] t = Girdi.Girdi.Temaslar(out int adet);
            if (adet != 1) { _temasVar = false; return; }

            Girdi.Girdi.Temas p = t[0];
            if (p.Basladi)
            {
                if (Girdi.Girdi.UiUzerinde(p.Konum)) { _temasVar = false; return; }
                _temasVar = true;
                _surukleOldu = false;
                _temasBaslangic = p.Konum;
                return;
            }
            if (!_temasVar) return;

            if (p.Suruyor && Vector2.Distance(p.Konum, _temasBaslangic) > 14f)
                _surukleOldu = true;

            if (!p.Bitti) return;
            _temasVar = false;
            if (_surukleOldu) return;

            Vector3 dunya = _kamera.ScreenToWorldPoint(p.Konum);
            var nokta = new Vector2Int(Mathf.FloorToInt(dunya.x), Mathf.FloorToInt(dunya.y));
            foreach (var b in _harita.Bolgeler)
            {
                if (!b.Alan.Contains(nokta)) continue;
                BolgeSec(b);
                return;
            }
            // Bolge disina dokunmak karti kapatir.
            KartKapat();
        }

        private void OyunaGec(string bolgeId) => Onyukleyici.OyunaGec(bolgeId);
    }
}
