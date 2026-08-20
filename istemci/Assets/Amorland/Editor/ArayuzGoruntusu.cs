using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Ag;
using Amorland.Arayuz;
using Amorland.Durum;
using Amorland.Gorsel;
using Amorland.Ses;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// Arayuzun PNG goruntulerini BASSIZ uretir — her ekran icin bir kare.
    ///
    /// Neden: Play moduna girmeden arayuzun nasil gorundugunu kimse goremiyor.
    /// Ekran kilitliyse, uzaktan calisiliyorsa ya da degisiklik bir baskasina
    /// gosterilecekse tek yol bu. Ekranlar GERCEK sinif kurularak cizilir
    /// (<see cref="CantaEkrani"/>, <see cref="Hud"/> ...) — elle cizilmis taklit degil.
    ///
    /// Dunya durumu da uydurma degildir: <c>araclar/durum-dokumu.js</c> gercek
    /// sunucuda oynayip anlik goruntuyu JSON'a yazar, burada o okunur. Uydurma veri
    /// gercekte olmayan bir ekrani guzel gosterebilirdi.
    ///
    /// Calistirma:
    ///   node araclar/durum-dokumu.js durum.json
    ///   Unity -batchmode -projectPath &lt;proje&gt; \
    ///         -executeMethod Amorland.EditorAraclari.ArayuzGoruntusu.Uret \
    ///         -dengeYolu &lt;denge.json&gt; -durumYolu &lt;durum.json&gt; -cikti &lt;klasor&gt; -quit
    ///
    /// ⚠ <c>-nographics</c> KULLANMA: render icin grafik aygiti gerekir.
    /// </summary>
    public static class ArayuzGoruntusu
    {
        private const int Genislik = 1280;
        private const int Yukseklik = 720;

        /// <summary>Modal ekranlarin arkasinda duran zemin: cim tonu, oyun alanini taklit eder.</summary>
        private static readonly Color OyunZemini = new Color(0.29f, 0.38f, 0.24f, 1f);

        private static readonly Color GirisZemini = new Color(0.085f, 0.075f, 0.065f, 1f);

        private static readonly List<string> _hatalar = new List<string>();
        private static int _uretilen;

        [MenuItem("Amorland/Arayuz Goruntusu Uret")]
        public static void Uret()
        {
            _hatalar.Clear();
            _uretilen = 0;

            string cikti = Arguman("-cikti");
            if (string.IsNullOrEmpty(cikti)) cikti = Path.Combine(Path.GetTempPath(), "amorland-goruntu");
            Directory.CreateDirectory(cikti);

            var denge = DengeYukle(Arguman("-dengeYolu"));
            var durum = DurumYukle(Arguman("-durumYolu"));

            Kare(cikti, "01-giris.png", GirisZemini, (k) =>
            {
                new GirisEkrani(k, denge).Ac();
            });

            Kare(cikti, "02-hud.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                var joy = YeniBilesen<Joystick>("Joystick");
                joy.Kur(k.GetComponent<Canvas>());
            });

            Kare(cikti, "03-canta.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                new CantaEkrani(k, durum, denge).Ac();
            });

            Kare(cikti, "04-depo.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                new DepoEkrani(k, durum, denge).Ac();
            });

            Kare(cikti, "05-tezgah.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                new TezgahEkrani(k, durum, denge).Ac();
            });

            Kare(cikti, "06-defter.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                new DefterEkrani(k, durum).Ac();
            });

            Kare(cikti, "07-harita.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                new HaritaEkrani(k, durum, denge).Ac();
            });

            Kare(cikti, "08-davet.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                new DavetEkrani(k).Ac();
            });

            Kare(cikti, "09-ayarlar.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                var ses = YeniBilesen<SesYoneticisi>("Ses");
                new AyarlarEkrani(k, ses).Ac();
            });

            Kare(cikti, "10-bildirimler.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                var b = YeniBilesen<Bildirimler>("Bildirimler");
                b.Kur(k);
                b.Goster("Buğday ekildi.");
                b.Goster("Kova boş. Kuyudan doldur.", true);
                var perde = YeniBilesen<BaglantiPerdesi>("Perde");
                perde.Kur(k);
                perde.Goster(2);
            });

            Kare(cikti, "11-seviye.png", OyunZemini, (k) =>
            {
                HudKur(k, durum, denge);
                var s = YeniBilesen<SeviyeAtlamaEkrani>("Seviye");
                s.Kur(k);
                s.Goster(2, "Depo açıldı");
            });

            Kare(cikti, "12-bilesenler.png", GirisZemini, BilesenSayfasi);
            Kare(cikti, "13-parlak-oneri.png", OyunZemini, ParlakOneri);
            Kare(cikti, "14-referans-hud.png", OyunZemini, ReferansHud);

            Debug.Log("[Amorland] " + _uretilen + " kare uretildi, " + _hatalar.Count + " hata. Klasor: " + cikti);
            foreach (string h in _hatalar) Debug.LogError("[Amorland] KARE HATASI: " + h);

            if (Application.isBatchMode) EditorApplication.Exit(_hatalar.Count == 0 ? 0 : 1);
        }

        // ------------------------------------------------------------------ kare

        /// <summary>
        /// Tek bir kare cizer. Her kare kendi try/catch'inde: bir ekran patlarsa
        /// digerleri yine uretilir, boylece tek kosuda butun tabloyu gorursun.
        /// </summary>
        private static void Kare(string klasor, string dosya, Color arka, Action<Transform> kur)
        {
            Camera cam = null;
            RenderTexture rt = null;
            Canvas kanvas = null;
            var cop = new List<GameObject>();
            _copKovasi = cop;

            try
            {
                SahneKur(out cam, out rt, out kanvas, arka);
                kur(kanvas.transform);
                Yakala(cam, rt, Path.Combine(klasor, dosya));
                _uretilen++;
            }
            catch (Exception e)
            {
                _hatalar.Add(dosya + ": " + e.GetType().Name + " — " + e.Message);
                Debug.LogWarning("[Amorland] " + dosya + " cizilemedi: " + e);
            }
            finally
            {
                foreach (var go in cop) if (go != null) UnityEngine.Object.DestroyImmediate(go);
                _copKovasi = null;
                Temizle(cam, rt, kanvas);
            }
        }

        private static List<GameObject> _copKovasi;

        /// <summary>MonoBehaviour ekranlar icin tasiyici nesne. Kare bitince yok edilir.</summary>
        private static T YeniBilesen<T>(string ad) where T : MonoBehaviour
        {
            var go = new GameObject(ad);
            if (_copKovasi != null) _copKovasi.Add(go);
            return go.AddComponent<T>();
        }

        private static void HudKur(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            var hud = YeniBilesen<Hud>("HUD");
            hud.Kur(kanvas, durum, denge);
        }

        // ------------------------------------------------------------------ bilesen sayfasi

        /// <summary>
        /// UiKit'in urettigi her parca yan yana. Bu bir oyun ekrani DEGILDIR —
        /// tasarim dilinin ne oldugunu tek karede gostermek icindir.
        /// </summary>
        private static void BilesenSayfasi(Transform kanvas)
        {
            var baslik = UiKit.Yazi(
                kanvas, "Baslik", "Arayüz bileşenleri", UiKit.YaziBuyuk,
                Palet.UiVurgu, TextAnchor.UpperLeft, true);
            UiKit.Sabitle(baslik.rectTransform, 0f, 1f, 40f, -32f, 600f, 40f);

            var solKart = UiKit.Kart(kanvas, "Butonlar", Palet.UiPanel, 20);
            UiKit.Sabitle(solKart, 0f, 1f, 40f, -92f, 380f, 340f);
            UiKit.GolgeEkle(solKart, 20, 22, 8f);
            var solDizi = UiKit.DikeyDizi(solKart, 12f, 22);
            solDizi.childForceExpandHeight = false;

            EtiketEkle(solKart, "Butonlar");
            UiKit.Boyut(UiKit.AnaButon(solKart, "A", "Vadiye gir", null).gameObject, 0f, 52f);
            UiKit.Boyut(UiKit.Buton(solKart, "B", "İkincil eylem", null).gameObject, 0f, 48f);
            UiKit.Boyut(UiKit.UyariButon(solKart, "C", "Yığını bırak", null).gameObject, 0f, 48f);
            UiKit.Boyut(UiKit.DuzButon(solKart, "D", "Sonra", null).gameObject, 0f, 44f);

            var ortaKart = UiKit.Kart(kanvas, "Girdiler", Palet.UiPanel, 20);
            UiKit.Sabitle(ortaKart, 0f, 1f, 450f, -92f, 380f, 340f);
            UiKit.GolgeEkle(ortaKart, 20, 22, 8f);
            var ortaDizi = UiKit.DikeyDizi(ortaKart, 12f, 22);
            ortaDizi.childForceExpandHeight = false;

            EtiketEkle(ortaKart, "Giriş ve ilerleme");
            UiKit.Boyut(UiKit.Giris(ortaKart, "G", "adını yaz", 16).gameObject, 0f, 52f);
            CubukEkle(ortaKart, "XP", 0.62f, Palet.UiVurgu);
            CubukEkle(ortaKart, "Depo", 0.35f, Palet.UiOnay);
            CubukEkle(ortaKart, "Depo dolu", 1f, Palet.UiUyari);

            var sagKart = UiKit.Kart(kanvas, "Slotlar", Palet.UiPanel, 20);
            UiKit.Sabitle(sagKart, 0f, 1f, 860f, -92f, 380f, 340f);
            UiKit.GolgeEkle(sagKart, 20, 22, 8f);
            EtiketEkle2(sagKart, "Tohum slotları ve rozetler", 22f, -22f);

            SlotEkle(sagKart, 22f, -70f, "Buğday", "12", true);
            SlotEkle(sagKart, 116f, -70f, "Havuç", "3", false);
            SlotEkle(sagKart, 210f, -70f, "", "", false);

            var rozetSatir = UiKit.Panel(sagKart, "Rozetler", Color.clear);
            UiKit.Sabitle(rozetSatir, 0f, 1f, 22f, -186f, 340f, 40f);
            var rozetDizi = UiKit.YatayDizi(rozetSatir, 12f, 0);
            rozetDizi.childForceExpandHeight = false;
            rozetDizi.childAlignment = TextAnchor.MiddleLeft;
            foreach (string hex in new[] { "#D9694A", "#5E8C55", "#4A7FB5", "#D9A441" })
            {
                Color c;
                ColorUtility.TryParseHtmlString(hex, out c);
                UiKit.Rozet(rozetSatir, c, 28f);
            }

            var not = UiKit.Yazi(
                sagKart, "Not", "Seçili slot ince halkayla işaretlenir; boş slot çukurda durur.",
                UiKit.YaziMini, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Sabitle(not.rectTransform, 0f, 1f, 22f, -238f, 336f, 50f);

            var altKart = UiKit.Kart(kanvas, "HudSerit", Palet.UiZemin);
            UiKit.Sabitle(altKart, 0.5f, 0f, 0f, 56f, 330f, 68f);
            UiKit.GolgeEkle(altKart, UiKit.Yaricap, 16, 5f);
            UiKit.YatayDizi(altKart, 6f, 8);
            foreach (string ad in new[] { "Çanta", "Harita", "Defter", "Ayar" })
            {
                var b = UiKit.Buton(altKart, ad, ad, null);
                UiKit.Boyut(b.gameObject, 74f, 52f);
                b.GetComponentInChildren<Text>().fontSize = UiKit.YaziKucuk;
            }

            var altNot = UiKit.Yazi(
                kanvas, "AltNot", "HUD düğme şeridi — panel yüksekliği artık düğme yüksekliği",
                UiKit.YaziMini, Palet.UiYaziSolgun, TextAnchor.UpperCenter);
            UiKit.Sabitle(altNot.rectTransform, 0.5f, 0f, 0f, 28f, 700f, 24f);
        }

        // ------------------------------------------------------------------ parlak oneri

        /// <summary>
        /// YENI YON onerisi: kalin kahve cerceve + krem govde + doygun yesil onay.
        ///
        /// ⚠ Bu gercek ekranlarin ciktisi DEGIL, yon onerisidir. Butun ekranlari
        /// donusturmeden once yonun dogru olup olmadigi tek karede gorulsun diye var.
        /// Yon onaylanirsa ayni bilesenler gercek ekranlara uygulanir.
        /// </summary>
        private static void ParlakOneri(Transform k)
        {
            // --- sol ust: seviye + XP
            RectTransform ic;
            var solUst = UiKit.CerceveliKart(k, "SolUst", Palet.UiKagit, Palet.UiCerceve, out ic, 24, 6f);
            UiKit.Sabitle(solUst, 0f, 1f, 16f, -16f, 250f, 74f);
            UiKit.GolgeEkle(solUst, 24, 14, 5f);

            var sv = UiKit.KonturluYazi(
                ic, "Sv", "Sv 4", UiKit.YaziBaslik, Palet.UiKoyuYazi, TextAnchor.UpperLeft, true, 0f);
            UiKit.Sabitle(sv.rectTransform, 0f, 1f, 12f, -8f, 100f, 26f);

            RectTransform xpIc;
            var xpDis = UiKit.CerceveliKart(
                ic, "Xp", Palet.UiKagitCukur, Palet.UiCerceve, out xpIc, 9, 3f);
            UiKit.Sabitle(xpDis, 0f, 1f, 12f, -38f, 200f, 18f);
            var xpDolgu = UiKit.Kart(xpIc, "Dolgu", Palet.UiAltin, 6);
            xpDolgu.anchorMin = Vector2.zero;
            xpDolgu.anchorMax = new Vector2(0f, 1f);
            xpDolgu.pivot = new Vector2(0f, 0.5f);
            xpDolgu.offsetMin = Vector2.zero;
            xpDolgu.offsetMax = Vector2.zero;
            xpDolgu.sizeDelta = new Vector2(130f, 0f);

            // --- sag ust: altin hapi
            RectTransform altinIc;
            var altin = UiKit.Hap(k, "Altin", Palet.UiKagit, Palet.UiCerceve, out altinIc);
            UiKit.Sabitle(altin, 1f, 1f, -16f, -16f, 168f, 56f);
            UiKit.GolgeEkle(altin, 22, 12, 4f);
            var para = UiKit.Kart(altinIc, "Sikke", Palet.UiAltin, 16);
            para.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
            UiKit.Sabitle(para, 0f, 0.5f, 8f, 0f, 30f, 30f);
            var altinYazi = UiKit.KonturluYazi(
                altinIc, "Yazi", "1.240", UiKit.YaziBaslik, Palet.UiKoyuYazi, TextAnchor.MiddleLeft, true, 0f);
            UiKit.Sabitle(altinYazi.rectTransform, 0f, 0.5f, 46f, 0f, 110f, 30f);

            // --- alt orta: tohum secici
            RectTransform serit;
            var seritDis = UiKit.CerceveliKart(k, "Tohumlar", Palet.UiKagit, Palet.UiCerceve, out serit, 24, 6f);
            UiKit.Sabitle(seritDis, 0.5f, 0f, 0f, 16f, 392f, 96f);
            UiKit.GolgeEkle(seritDis, 24, 16, 6f);
            string[] adlar = { "Buğday", "Havuç", "Marul", "" };
            string[] adetler = { "12", "6", "3", "" };
            for (int i = 0; i < 4; i++)
            {
                bool dolu = adlar[i].Length > 0;
                RectTransform slotIc;
                var slot = UiKit.CerceveliKart(
                    serit, "Slot" + i, dolu ? Palet.UiKagitKoyu : Palet.UiKagitCukur,
                    i == 0 ? Palet.UiAltinKoyu : Palet.UiCerceve, out slotIc, 16, i == 0 ? 5f : 3f);
                UiKit.Sabitle(slot, 0f, 0.5f, 8f + i * 92f, 0f, 84f, 76f);
                slot.pivot = new Vector2(0f, 0.5f);
                if (!dolu) continue;

                var ad = UiKit.Yazi(
                    slotIc, "Ad", adlar[i], UiKit.YaziMini, Palet.UiKoyuYazi, TextAnchor.UpperCenter, true);
                UiKit.Sabitle(ad.rectTransform, 0.5f, 1f, 0f, -6f, 74f, 30f);
                var adet = UiKit.KonturluYazi(
                    slotIc, "Adet", adetler[i], UiKit.YaziKucuk, Color.white, TextAnchor.LowerRight, true, 1.6f);
                UiKit.Sabitle(adet.rectTransform, 1f, 0f, -6f, 5f, 40f, 20f);
            }

            // --- sag alt: ahsap dugmeler
            foreach (var (ad, i) in new[] { ("Çanta", 0), ("Harita", 1), ("Defter", 2), ("Ayar", 3) })
            {
                var b = UiKit.AhsapButon(k, ad, ad, null);
                var rt = b.GetComponent<RectTransform>();
                UiKit.Sabitle(rt, 1f, 0f, -16f - (3 - i) * 84f, 16f, 78f, 66f);
                b.GetComponentInChildren<Text>().fontSize = UiKit.YaziKucuk;
            }

            // --- orta: modal
            RectTransform modal;
            var modalDis = UiKit.CerceveliKart(k, "Modal", Palet.UiKagit, Palet.UiCerceve, out modal, 28, 7f);
            UiKit.Sabitle(modalDis, 0.5f, 0.5f, 0f, 10f, 520f, 300f);
            UiKit.GolgeEkle(modalDis, 28, 26, 10f);

            var baslik = UiKit.KonturluYazi(
                modal, "Baslik", "Depo", UiKit.YaziBuyuk, Palet.UiKoyuYazi, TextAnchor.MiddleCenter, true, 0f);
            UiKit.Sabitle(baslik.rectTransform, 0.5f, 1f, 0f, -10f, 300f, 40f);

            for (int i = 0; i < 2; i++)
            {
                RectTransform satirIc;
                var satir = UiKit.CerceveliKart(
                    modal, "Satir" + i, Palet.UiKagitKoyu, Palet.UiCerceve, out satirIc, 14, 3f);
                UiKit.Sabitle(satir, 0.5f, 1f, 0f, -62f - i * 56f, 460f, 48f);

                var rozet = UiKit.Rozet(satirIc, i == 0 ? Palet.UiYesil : Palet.UiAltin, 22f);
                UiKit.Sabitle(rozet.rectTransform, 0f, 0.5f, 12f, 0f, 22f, 22f);

                var ad = UiKit.Yazi(
                    satirIc, "Ad", i == 0 ? "Buğday · sen bıraktın" : "Havuç · Bora bıraktı",
                    UiKit.YaziGovde, Palet.UiKoyuYazi, TextAnchor.MiddleLeft, true);
                UiKit.Sabitle(ad.rectTransform, 0f, 0.5f, 44f, 0f, 300f, 24f);

                var adet = UiKit.Yazi(
                    satirIc, "Adet", i == 0 ? "24" : "9", UiKit.YaziGovde,
                    Palet.UiKoyuYaziSolgun, TextAnchor.MiddleRight, true);
                UiKit.Sabitle(adet.rectTransform, 1f, 0.5f, -14f, 0f, 60f, 24f);
            }

            var onay = UiKit.YesilButon(modal, "Onay", "Çantayı boşalt", null);
            UiKit.Sabitle(onay.GetComponent<RectTransform>(), 0.5f, 0f, -92f, 16f, 240f, 60f);
            var iptal = UiKit.KirmiziButon(modal, "Iptal", "Kapat", null);
            UiKit.Sabitle(iptal.GetComponent<RectTransform>(), 0.5f, 0f, 128f, 16f, 150f, 60f);

            var not = UiKit.Yazi(
                k, "Not", "YENİ YÖN ÖNERİSİ — kalın çerçeve · krem gövde · doygun yeşil onay",
                UiKit.YaziMini, Color.white, TextAnchor.UpperCenter);
            not.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);
            UiKit.Sabitle(not.rectTransform, 0.5f, 1f, 0f, -6f, 800f, 22f);
        }

        // ------------------------------------------------------------------ referans HUD

        /// <summary>
        /// Referans gorseldeki HUD YAPISI, kod ile kurulmus hali.
        ///
        /// ⚠ Ikonlar YER TUTUCU siluettir; referanstaki elle cizilmis dokulu ikonlar
        /// varlik isidir, kod ile uretilemez. Burada dogrulanan sey yerlesim ve kabuk:
        /// koyu kapsul + acik cerceve, avatar halkasi, ustte kaynak seridi, sagda
        /// parsomen gorev karti, altta parsomen alet cubugu, secili slotta altin cerceve.
        /// Gercek ikonlar geldiginde yalnizca sprite degisir.
        /// </summary>
        private static void ReferansHud(Transform k)
        {
            // ---------------------------------------------------------- sol ust: oyuncu
            RectTransform oyuncuIc;
            var oyuncu = UiKit.CerceveliKart(
                k, "Oyuncu", Palet.KapsulZemin, Palet.KapsulCerceve, out oyuncuIc, 26, 3f);
            UiKit.Sabitle(oyuncu, 0f, 1f, 14f, -12f, 296f, 84f);
            UiKit.GolgeEkle(oyuncu, 26, 14, 4f);

            var ad = UiKit.KonturluYazi(
                oyuncuIc, "Ad", "Enes & Melisa", UiKit.YaziGovde, Color.white, TextAnchor.UpperLeft, true, 1.4f);
            UiKit.Sabitle(ad.rectTransform, 0f, 1f, 96f, -8f, 160f, 24f);

            var kalp = UiKit.Panel(oyuncuIc, "Kalp", Palet.UiKirmizi);
            kalp.gameObject.GetComponent<Image>().sprite = Sekil.Kalp();
            UiKit.Sabitle(kalp, 0f, 1f, 258f, -10f, 20f, 20f);

            var yildiz = UiKit.Panel(oyuncuIc, "Yildiz", Palet.KapsulAltin);
            yildiz.gameObject.GetComponent<Image>().sprite = Sekil.Yildiz();
            UiKit.Sabitle(yildiz, 0f, 1f, 96f, -34f, 18f, 18f);

            var seviye = UiKit.Yazi(
                oyuncuIc, "Seviye", "Seviye 12", UiKit.YaziKucuk, Palet.KapsulAltin,
                TextAnchor.UpperLeft, true);
            UiKit.Sabitle(seviye.rectTransform, 0f, 1f, 120f, -34f, 130f, 20f);

            RectTransform xpIc;
            var xp = UiKit.CerceveliKart(
                oyuncuIc, "Xp", new Color(0f, 0f, 0f, 0.55f), Palet.KapsulCerceve, out xpIc, 10, 2f);
            UiKit.Sabitle(xp, 0f, 1f, 96f, -58f, 186f, 18f);
            var xpDolgu = UiKit.Kart(xpIc, "Dolgu", Palet.UiAltin, 7);
            xpDolgu.anchorMin = Vector2.zero;
            xpDolgu.anchorMax = new Vector2(0f, 1f);
            xpDolgu.pivot = new Vector2(0f, 0.5f);
            xpDolgu.offsetMin = Vector2.zero;
            xpDolgu.offsetMax = Vector2.zero;
            xpDolgu.sizeDelta = new Vector2(112f, 0f);
            var xpYazi = UiKit.KonturluYazi(
                xpIc, "XpYazi", "1.250 / 2.000", UiKit.YaziMini, Color.white, TextAnchor.MiddleCenter, true, 1.2f);
            UiKit.Doldur(xpYazi.rectTransform);

            // Avatar halkasi kapsulun disina tasar: referansta da oyle duruyor.
            var halka = UiKit.Panel(k, "AvatarHalka", Palet.KapsulCerceve);
            halka.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
            UiKit.Sabitle(halka, 0f, 1f, 10f, -8f, 92f, 92f);
            var avatar = UiKit.Panel(halka, "Avatar", new Color(0.42f, 0.55f, 0.68f, 1f));
            avatar.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
            UiKit.Doldur(avatar, 5f);
            var avatarNot = UiKit.Yazi(
                avatar, "Not", "çift\nportresi", UiKit.YaziMini, new Color(1f, 1f, 1f, 0.85f),
                TextAnchor.MiddleCenter);
            UiKit.Doldur(avatarNot.rectTransform);

            // ---------------------------------------------------------- ust orta: kaynaklar
            var kaynaklar = new[]
            {
                new object[] { "Enerji", Sekil.Simsek(), Palet.UiAltin, "120/120", 150f },
                new object[] { "Altin", Sekil.Daire(), Palet.UiAltin, "8.450", 140f },
                new object[] { "Elmas", Sekil.Elmas(), new Color(0.90f, 0.42f, 0.66f), "320", 120f },
            };
            float kx = 330f;
            foreach (var kay in kaynaklar)
            {
                float genislik = (float)kay[4];
                RectTransform hapIc;
                var hap = UiKit.CerceveliKart(
                    k, (string)kay[0], Palet.KapsulZemin, Palet.KapsulCerceve, out hapIc, 20, 3f);
                UiKit.Sabitle(hap, 0f, 1f, kx, -14f, genislik, 46f);
                UiKit.GolgeEkle(hap, 20, 10, 3f);

                var ikon = UiKit.Panel(hapIc, "Ikon", (Color)kay[2]);
                ikon.gameObject.GetComponent<Image>().sprite = (Sprite)kay[1];
                UiKit.Sabitle(ikon, 0f, 0.5f, 8f, 0f, 24f, 24f);

                var deger = UiKit.KonturluYazi(
                    hapIc, "Deger", (string)kay[3], UiKit.YaziGovde, Color.white,
                    TextAnchor.MiddleLeft, true, 1.3f);
                UiKit.Sabitle(deger.rectTransform, 0f, 0.5f, 38f, 0f, genislik - 82f, 24f);

                // Yesil "+" — referansta her kaynagin sagindaki kucuk kare buton.
                RectTransform artiIc;
                var arti = UiKit.CerceveliKart(
                    hapIc, "Arti", Palet.ArtiYesil, Palet.ArtiYesilKoyu, out artiIc, 10, 2f);
                UiKit.Sabitle(arti, 1f, 0.5f, -6f, 0f, 30f, 30f);
                var artiIkon = UiKit.Panel(artiIc, "Sekil", Color.white);
                artiIkon.gameObject.GetComponent<Image>().sprite = Sekil.Arti();
                UiKit.Doldur(artiIkon, 6f);

                kx += genislik + 10f;
            }

            // ---------------------------------------------------------- sag ust: ikon butonlar
            for (int i = 0; i < 4; i++)
            {
                RectTransform bIc;
                var b = UiKit.CerceveliKart(
                    k, "Ikon" + i, Palet.KapsulZemin, Palet.KapsulCerceve, out bIc, 16, 3f);
                UiKit.Sabitle(b, 1f, 1f, -14f - (3 - i) * 62f, -14f, 54f, 54f);
                UiKit.GolgeEkle(b, 16, 10, 3f);
                // ⚠ Emoji glifleri isletim sistemi yazi tipinden gelmiyor ve bos
                // kutu olarak ciziliyordu. Yer tutucu olarak cizilen sekil kullanilir.
                var g = UiKit.Panel(bIc, "G", new Color(1f, 0.95f, 0.85f, 0.85f));
                g.gameObject.GetComponent<Image>().sprite =
                    i == 0 ? Sekil.Kalp() : i == 1 ? Sekil.Yildiz()
                    : i == 2 ? Sekil.Elmas() : Sekil.Simsek();
                UiKit.Doldur(g, 13f);
            }

            // ---------------------------------------------------------- sag: gorev karti
            RectTransform gorevIc;
            var gorev = UiKit.CerceveliKart(
                k, "Gorev", Palet.Parsomen, Palet.UiCerceve, out gorevIc, 20, 4f);
            UiKit.Sabitle(gorev, 1f, 1f, -14f, -84f, 300f, 92f);
            UiKit.GolgeEkle(gorev, 20, 14, 5f);

            var gBaslik = UiKit.Yazi(
                gorevIc, "Baslik", "Günlük Görev", UiKit.YaziGovde, Palet.UiKoyuYazi,
                TextAnchor.UpperLeft, true);
            UiKit.Sabitle(gBaslik.rectTransform, 0f, 1f, 44f, -8f, 200f, 24f);
            var gMetin = UiKit.Yazi(
                gorevIc, "Metin", "10 domates hasat et", UiKit.YaziKucuk, Palet.UiKoyuYaziSolgun,
                TextAnchor.UpperLeft);
            UiKit.Sabitle(gMetin.rectTransform, 0f, 1f, 44f, -32f, 220f, 20f);

            var gIkon = UiKit.Panel(gorevIc, "Ikon", Palet.UiYesil);
            gIkon.gameObject.GetComponent<Image>().sprite = Sekil.Damla();
            UiKit.Sabitle(gIkon, 0f, 1f, 10f, -10f, 28f, 28f);

            RectTransform gcIc;
            var gc = UiKit.CerceveliKart(
                gorevIc, "Cubuk", Palet.ParsomenKoyu, Palet.UiCerceve, out gcIc, 9, 2f);
            UiKit.Sabitle(gc, 0f, 1f, 10f, -58f, 210f, 18f);
            var gcDolgu = UiKit.Kart(gcIc, "Dolgu", Palet.UiYesil, 7);
            gcDolgu.anchorMin = Vector2.zero;
            gcDolgu.anchorMax = new Vector2(0f, 1f);
            gcDolgu.pivot = new Vector2(0f, 0.5f);
            gcDolgu.offsetMin = Vector2.zero;
            gcDolgu.offsetMax = Vector2.zero;
            gcDolgu.sizeDelta = new Vector2(126f, 0f);
            var gcYazi = UiKit.Yazi(
                gorevIc, "Oran", "6/10", UiKit.YaziKucuk, Palet.UiKoyuYazi, TextAnchor.MiddleRight, true);
            UiKit.Sabitle(gcYazi.rectTransform, 1f, 1f, -10f, -58f, 50f, 18f);

            // ---------------------------------------------------------- sol kenar: dairesel
            string[] solAdlar = { "Harita", "Çanta", "Defter" };
            for (int i = 0; i < solAdlar.Length; i++)
            {
                var cember = UiKit.Panel(k, "Sol" + i, Palet.KapsulCerceve);
                cember.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
                UiKit.Sabitle(cember, 0f, 0.5f, 16f, 88f - i * 106f, 62f, 62f);
                var icC = UiKit.Panel(cember, "Ic", Palet.KapsulZemin);
                icC.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
                UiKit.Doldur(icC, 3f);

                var ikonC = UiKit.Panel(icC, "Ikon", new Color(1f, 0.95f, 0.85f, 0.85f));
                ikonC.gameObject.GetComponent<Image>().sprite =
                    i == 0 ? Sekil.Elmas() : i == 1 ? Sekil.Yildiz() : Sekil.Kalp();
                UiKit.Doldur(ikonC, 16f);

                var etiket = UiKit.KonturluYazi(
                    k, "SolAd" + i, solAdlar[i], UiKit.YaziMini, Color.white,
                    TextAnchor.UpperCenter, true, 1.4f);
                UiKit.Sabitle(etiket.rectTransform, 0f, 0.5f, 16f, 88f - i * 106f - 66f, 62f, 18f);
            }

            // ---------------------------------------------------------- alt orta: alet cubugu
            RectTransform aletIc;
            var alet = UiKit.CerceveliKart(
                k, "AletCubugu", Palet.Parsomen, Palet.UiCerceve, out aletIc, 20, 4f);
            UiKit.Sabitle(alet, 0.5f, 0f, 0f, 14f, 470f, 104f);
            UiKit.GolgeEkle(alet, 20, 16, 5f);

            string[] aletAdlari = { "Sulama", "Çapa", "Tohum", "Gübre", "Orak" };
            string[] sayilar = { "", "", "12", "8", "" };
            for (int i = 0; i < 5; i++)
            {
                bool secili = i == 0;
                RectTransform slotIc;
                var slot = UiKit.CerceveliKart(
                    aletIc, "Slot" + i, Palet.ParsomenKoyu,
                    secili ? Palet.KapsulAltin : Palet.UiCerceve, out slotIc, 14, secili ? 4f : 2f);
                UiKit.Sabitle(slot, 0f, 1f, 6f + i * 90f, -6f, 84f, 62f);

                var ikon = UiKit.Panel(slotIc, "Ikon", Palet.UiCerceve);
                ikon.gameObject.GetComponent<Image>().sprite =
                    i == 0 ? Sekil.Damla() : i == 2 ? Sekil.Yildiz() : Sekil.Elmas();
                UiKit.Sabitle(ikon, 0.5f, 0.5f, 0f, 0f, 34f, 34f);

                if (sayilar[i].Length > 0)
                {
                    var rozet = UiKit.Panel(slotIc, "Sayi", Palet.KapsulZemin);
                    rozet.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
                    UiKit.Sabitle(rozet, 1f, 0f, -2f, 2f, 24f, 24f);
                    var sYazi = UiKit.Yazi(
                        rozet, "Y", sayilar[i], UiKit.YaziMini, Color.white, TextAnchor.MiddleCenter, true);
                    UiKit.Doldur(sYazi.rectTransform);
                }

                var adYazi = UiKit.Yazi(
                    aletIc, "Ad" + i, aletAdlari[i], UiKit.YaziMini, Palet.UiKoyuYazi,
                    TextAnchor.UpperCenter, true);
                UiKit.Sabitle(adYazi.rectTransform, 0f, 1f, 6f + i * 90f, -70f, 84f, 20f);
            }

            // ---------------------------------------------------------- sag alt: zaman
            RectTransform zamanIc;
            var zaman = UiKit.CerceveliKart(
                k, "Zaman", Palet.KapsulZemin, Palet.KapsulCerceve, out zamanIc, 20, 3f);
            UiKit.Sabitle(zaman, 1f, 0f, -14f, 14f, 220f, 62f);
            UiKit.GolgeEkle(zaman, 20, 12, 4f);
            var gunes = UiKit.Panel(zamanIc, "Gunes", Palet.UiAltin);
            gunes.gameObject.GetComponent<Image>().sprite = Sekil.Yildiz();
            UiKit.Sabitle(gunes, 0f, 0.5f, 10f, 0f, 30f, 30f);
            var saat = UiKit.KonturluYazi(
                zamanIc, "Saat", "09:45", UiKit.YaziBaslik, Color.white, TextAnchor.UpperLeft, true, 1.3f);
            UiKit.Sabitle(saat.rectTransform, 0f, 1f, 48f, -6f, 120f, 26f);
            var gun = UiKit.Yazi(
                zamanIc, "Gun", "İlkbahar 18. Gün", UiKit.YaziMini, new Color(1f, 0.95f, 0.85f, 0.75f),
                TextAnchor.UpperLeft);
            UiKit.Sabitle(gun.rectTransform, 0f, 1f, 48f, -32f, 160f, 20f);

            // ---------------------------------------------------------- not
            var not = UiKit.KonturluYazi(
                k, "Not",
                "REFERANS YAPISI — kabuk kod ile kuruldu · ikonlar YER TUTUCU (gerçek ikonlar varlık işi)",
                UiKit.YaziMini, Color.white, TextAnchor.LowerCenter, false, 1.4f);
            UiKit.Sabitle(not.rectTransform, 0.5f, 1f, 0f, -160f, 900f, 20f);
        }

        private static void EtiketEkle(Transform ebeveyn, string metin)
        {
            var e = UiKit.Yazi(
                ebeveyn, "Etiket", metin, UiKit.YaziKucuk, Palet.UiYaziSolgun, TextAnchor.MiddleLeft, true);
            UiKit.Boyut(e.gameObject, 0f, 22f);
        }

        private static void EtiketEkle2(Transform ebeveyn, string metin, float x, float y)
        {
            var e = UiKit.Yazi(
                ebeveyn, "Etiket", metin, UiKit.YaziKucuk, Palet.UiYaziSolgun, TextAnchor.UpperLeft, true);
            UiKit.Sabitle(e.rectTransform, 0f, 1f, x, y, 340f, 22f);
        }

        private static void CubukEkle(Transform ebeveyn, string ad, float oran, Color renk)
        {
            var satir = UiKit.Panel(ebeveyn, ad + "Satir", Color.clear);
            UiKit.Boyut(satir.gameObject, 0f, 34f);

            var etiket = UiKit.Yazi(satir, "Ad", ad, UiKit.YaziMini, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Sabitle(etiket.rectTransform, 0f, 1f, 0f, 0f, 120f, 18f);

            Image dolgu;
            var arka = UiKit.Cubuk(satir, "Cubuk", Palet.UiYuzeyKoyu, renk, out dolgu);
            UiKit.Sabitle(arka.rectTransform, 0f, 0f, 0f, 2f, 336f, 12f);
            UiKit.CubukAyarla(dolgu, oran, 336f);
        }

        private static void SlotEkle(Transform ebeveyn, float x, float y, string ad, string adet, bool secili)
        {
            var slot = UiKit.Kart(
                ebeveyn, "Slot" + ad, string.IsNullOrEmpty(ad) ? Palet.UiYuzeyKoyu : Palet.UiYuzey,
                UiKit.YaricapKucuk);
            UiKit.Sabitle(slot, 0f, 1f, x, y, 84f, 76f);

            var adYazi = UiKit.Yazi(slot, "Ad", ad, UiKit.YaziMini, Palet.UiYazi, TextAnchor.UpperCenter);
            UiKit.Sabitle(adYazi.rectTransform, 0.5f, 1f, 0f, -8f, 76f, 34f);

            var adetYazi = UiKit.Yazi(
                slot, "Adet", adet, UiKit.YaziKucuk, Palet.UiVurgu, TextAnchor.LowerRight, true);
            UiKit.Sabitle(adetYazi.rectTransform, 1f, 0f, -8f, 6f, 40f, 20f);

            var cerceve = UiKit.Cerceve(slot, "Cerceve", Color.clear, UiKit.YaricapKucuk, 3);
            cerceve.color = secili ? Palet.UiVurgu : Color.clear;
        }

        // ------------------------------------------------------------------ altyapi

        private static void SahneKur(out Camera cam, out RenderTexture rt, out Canvas kanvas, Color arka)
        {
            rt = new RenderTexture(Genislik, Yukseklik, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            rt.Create();

            var camGo = new GameObject("GoruntuKamera");
            cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = arka;
            cam.targetTexture = rt;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            kanvas = UiKit.Kanvas("GoruntuKanvas", 0);
            kanvas.renderMode = RenderMode.ScreenSpaceCamera;
            kanvas.worldCamera = cam;
            kanvas.planeDistance = 10f;

            // ⚠ ScaleWithScreenSize batchmode'da Screen olculerine bakar ve orada ekran
            // yoktur. Goruntu icin olcek sabitlenir: 1 birim = 1 piksel.
            var olcek = kanvas.GetComponent<CanvasScaler>();
            olcek.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            olcek.scaleFactor = 1f;

            kanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(Genislik, Yukseklik);
        }

        private static void Yakala(Camera cam, RenderTexture rt, string yol)
        {
            // Yerlesim ve yazi atlasi bir kare bekler; zorla guncellenmezse bos cikar.
            Canvas.ForceUpdateCanvases();
            cam.Render();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture onceki = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Genislik, Yukseklik, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0f, 0f, Genislik, Yukseklik), 0, 0);
            tex.Apply();
            RenderTexture.active = onceki;

            File.WriteAllBytes(yol, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void Temizle(Camera cam, RenderTexture rt, Canvas kanvas)
        {
            if (kanvas != null) UnityEngine.Object.DestroyImmediate(kanvas.gameObject);
            if (cam != null) UnityEngine.Object.DestroyImmediate(cam.gameObject);
            if (rt != null)
            {
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            Sekil.Unut();
        }

        private static DengeVerisi DengeYukle(string yol)
        {
            var denge = new DengeVerisi();
            if (!string.IsNullOrEmpty(yol) && File.Exists(yol))
            {
                denge.Yukle(File.ReadAllText(yol));
            }
            else
            {
                Debug.LogWarning("[Amorland] Denge dosyasi yok (" + yol + "), varsayilanlar kullanilacak.");
            }
            return denge;
        }

        /// <summary>
        /// Gercek sunucudan alinmis anlik goruntuyu yukler.
        /// Dosya yoksa BOS durum dondurulur — ekranlar yine cizilir ama "bos durum"
        /// gorunumu verir; bu da denetlenmeye deger bir haldir.
        /// </summary>
        private static DunyaDurumu DurumYukle(string yol)
        {
            var durum = new DunyaDurumu();
            if (string.IsNullOrEmpty(yol) || !File.Exists(yol))
            {
                Debug.LogWarning("[Amorland] Durum dokumu yok (" + yol
                                 + "). Once: node araclar/durum-dokumu.js <dosya>");
                return durum;
            }

            var degisim = JsonConvert.DeserializeObject<Degisim>(File.ReadAllText(yol));
            if (degisim == null)
            {
                Debug.LogWarning("[Amorland] Durum dokumu okunamadi.");
                return durum;
            }

            // ⚠ BenimId Uygula'dan ONCE atanir: HUD ve canta "ben kimim" bilmeden
            // bos gorunur ve denetim yanlis sonuc verir.
            if (degisim.Oyuncular != null && degisim.Oyuncular.Count > 0)
            {
                durum.BenimId = degisim.Oyuncular[0].Id;
            }
            durum.Uygula(degisim);
            Debug.Log("[Amorland] Durum yuklendi: " + yol);
            return durum;
        }

        private static string Arguman(string ad)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ad) return args[i + 1];
            }
            return null;
        }
    }
}
