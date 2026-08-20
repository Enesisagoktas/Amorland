using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Arayuz;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// Arayuzun PNG goruntusunu BASSIZ uretir.
    ///
    /// Neden gerekli: Play moduna girmeden arayuzun nasil gorundugunu kimse goremiyor.
    /// Ekran kilitliyse, uzaktan calisiliyorsa ya da degisikligi bir baskasina
    /// gostermek gerekiyorsa tek yol bu. Goruntu GERCEK <see cref="GirisEkrani"/>
    /// sinifi kurularak alinir — elle cizilmis bir taklit degil.
    ///
    /// Calistirma:
    ///   Unity -batchmode -projectPath &lt;proje&gt; \
    ///         -executeMethod Amorland.EditorAraclari.ArayuzGoruntusu.Uret \
    ///         -dengeYolu &lt;denge.json&gt; -cikti &lt;klasor&gt; -quit
    ///
    /// ⚠ <c>-nographics</c> KULLANMA: render icin grafik aygiti gerekir.
    /// </summary>
    public static class ArayuzGoruntusu
    {
        private const int Genislik = 1280;
        private const int Yukseklik = 720;

        [MenuItem("Amorland/Arayuz Goruntusu Uret")]
        public static void Uret()
        {
            string dengeYolu = Arguman("-dengeYolu");
            string cikti = Arguman("-cikti");
            if (string.IsNullOrEmpty(cikti)) cikti = Path.Combine(Path.GetTempPath(), "amorland-goruntu");
            Directory.CreateDirectory(cikti);

            int hata = 0;
            try
            {
                var denge = DengeYukle(dengeYolu);

                GirisEkraniniCiz(Path.Combine(cikti, "1-giris-ekrani.png"), denge);
                BilesenSayfasiCiz(Path.Combine(cikti, "2-bilesenler.png"));

                Debug.Log("[Amorland] Goruntuler uretildi: " + cikti);
            }
            catch (Exception e)
            {
                Debug.LogError("[Amorland] Goruntu uretilemedi: " + e.GetType().Name + " — " + e.Message
                               + "\n" + e.StackTrace);
                hata = 1;
            }

            if (Application.isBatchMode) EditorApplication.Exit(hata);
        }

        // ------------------------------------------------------------------ sahneler

        private static void GirisEkraniniCiz(string yol, DengeVerisi denge)
        {
            Camera cam;
            RenderTexture rt;
            Canvas kanvas;
            SahneKur(out cam, out rt, out kanvas, new Color(0.085f, 0.075f, 0.065f, 1f));

            try
            {
                // Gercek sinif. Taklit degil — ekranda ne varsa oyunda da o var.
                var giris = new GirisEkrani(kanvas.transform, denge);
                giris.Ac();
                Yakala(cam, rt, yol);
            }
            finally
            {
                Temizle(cam, rt, kanvas);
            }
        }

        /// <summary>
        /// Bilesen sayfasi: UiKit'in urettigi her parca yan yana. Bu bir oyun ekrani
        /// DEGILDIR — tasarim dilinin ne oldugunu tek karede gostermek icindir.
        /// </summary>
        private static void BilesenSayfasiCiz(string yol)
        {
            Camera cam;
            RenderTexture rt;
            Canvas kanvas;
            SahneKur(out cam, out rt, out kanvas, new Color(0.085f, 0.075f, 0.065f, 1f));

            try
            {
                var baslik = UiKit.Yazi(
                    kanvas.transform, "Baslik", "Arayüz bileşenleri", UiKit.YaziBuyuk,
                    Palet.UiVurgu, TextAnchor.UpperLeft, true);
                UiKit.Sabitle(baslik.rectTransform, 0f, 1f, 40f, -32f, 600f, 40f);

                // --- sol sutun: butonlar
                var solKart = UiKit.Kart(kanvas.transform, "Butonlar", Palet.UiPanel, 20);
                UiKit.Sabitle(solKart, 0f, 1f, 40f, -92f, 380f, 340f);
                UiKit.GolgeEkle(solKart, 20, 22, 8f);
                var solDizi = UiKit.DikeyDizi(solKart, 12f, 22);
                solDizi.childForceExpandHeight = false;

                EtiketEkle(solKart, "Butonlar");
                UiKit.Boyut(UiKit.AnaButon(solKart, "A", "Vadiye gir", null).gameObject, 0f, 52f);
                UiKit.Boyut(UiKit.Buton(solKart, "B", "İkincil eylem", null).gameObject, 0f, 48f);
                UiKit.Boyut(UiKit.UyariButon(solKart, "C", "Yığını bırak", null).gameObject, 0f, 48f);
                UiKit.Boyut(UiKit.DuzButon(solKart, "D", "Sonra", null).gameObject, 0f, 44f);

                // --- orta sutun: giris ve cubuklar
                var ortaKart = UiKit.Kart(kanvas.transform, "Girdiler", Palet.UiPanel, 20);
                UiKit.Sabitle(ortaKart, 0f, 1f, 450f, -92f, 380f, 340f);
                UiKit.GolgeEkle(ortaKart, 20, 22, 8f);
                var ortaDizi = UiKit.DikeyDizi(ortaKart, 12f, 22);
                ortaDizi.childForceExpandHeight = false;

                EtiketEkle(ortaKart, "Giriş ve ilerleme");
                UiKit.Boyut(UiKit.Giris(ortaKart, "G", "adını yaz", 16).gameObject, 0f, 52f);

                CubukEkle(ortaKart, "XP", 0.62f, Palet.UiVurgu);
                CubukEkle(ortaKart, "Depo", 0.35f, Palet.UiOnay);
                CubukEkle(ortaKart, "Depo dolu", 1f, Palet.UiUyari);

                // --- sag sutun: rozet ve slot
                var sagKart = UiKit.Kart(kanvas.transform, "Slotlar", Palet.UiPanel, 20);
                UiKit.Sabitle(sagKart, 0f, 1f, 860f, -92f, 380f, 340f);
                UiKit.GolgeEkle(sagKart, 20, 22, 8f);
                EtiketEkle2(sagKart, "Tohum slotları ve rozetler", 22f, -22f);

                // Secili ve bos slot yan yana: fark gorunsun.
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

                // --- alt serit: HUD dugme serisi
                var altKart = UiKit.Kart(kanvas.transform, "HudSerit", Palet.UiZemin);
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
                    kanvas.transform, "AltNot", "HUD düğme şeridi — panel yüksekliği artık düğme yüksekliği",
                    UiKit.YaziMini, Palet.UiYaziSolgun, TextAnchor.UpperCenter);
                UiKit.Sabitle(altNot.rectTransform, 0.5f, 0f, 0f, 28f, 700f, 24f);

                Yakala(cam, rt, yol);
            }
            finally
            {
                Temizle(cam, rt, kanvas);
            }
        }

        // ------------------------------------------------------------------ parcalar

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
            rt = new RenderTexture(Genislik, Yukseklik, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8
            };
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

            // ⚠ ScaleWithScreenSize batchmode'da Screen olculerine bakar ve orada
            // ekran yoktur. Goruntu icin olcek sabitlenir: 1 birim = 1 piksel.
            var olcek = kanvas.GetComponent<CanvasScaler>();
            olcek.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            olcek.scaleFactor = 1f;

            var kanvasRt = kanvas.GetComponent<RectTransform>();
            kanvasRt.sizeDelta = new Vector2(Genislik, Yukseklik);
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
            Debug.Log("[Amorland] Yazildi: " + yol);
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
                Debug.Log("[Amorland] Denge yuklendi: " + yol);
            }
            else
            {
                Debug.LogWarning("[Amorland] Denge dosyasi yok (" + yol
                                 + "), varsayilanlarla cizilecek — renkler beyaz gorunur.");
            }
            return denge;
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
