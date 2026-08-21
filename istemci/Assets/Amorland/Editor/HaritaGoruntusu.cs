using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Amorland.Gorsel;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// Vadi haritasinin PNG goruntusunu BASSIZ uretir.
    ///
    /// Play moduna girmeden haritanin nasil dizildigini gormenin tek yolu. Harita
    /// 180x130 karo — Play'de ekrana ancak bir kosesi sigar; burada tamami tek karede
    /// cikar ve yerlesim hatalari (ust uste binen bolge, bos kalan alan, bozuk kiyi
    /// karosu) gorunur olur.
    ///
    /// Calistirma:
    ///   Unity -batchmode -projectPath &lt;proje&gt; \
    ///         -executeMethod Amorland.EditorAraclari.HaritaGoruntusu.Uret \
    ///         -cikti &lt;klasor&gt; -quit
    ///
    /// ⚠ <c>-nographics</c> KULLANMA: render icin grafik aygiti gerekir.
    /// </summary>
    public static class HaritaGoruntusu
    {
        /// <summary>
        /// Uc karo sayfasinin indeks tablosunu UNITY'NIN kendi dilimlemesiyle cizer.
        ///
        /// ⚠ Neden gerekli: indeksleri once Python'da urettigim tablodan okumustum ve
        /// karolarin kenarlarindaki koyu konturu FARK ETMEMISTIM — cim sandigim karolar
        /// tek basina duran yama karolariydi ve doseyince harita bastan basa koyu izgara
        /// cikti. Sayfa uzerinde ne oldugunu gozle gormeden indeks secilmiyor.
        /// </summary>
        public static void KaroTablosu()
        {
            string cikti = CiktiKlasoru();
            var cop = new List<GameObject>();
            try
            {
                Karo64.Unut();
                foreach (Sayfa64 sayfa in System.Enum.GetValues(typeof(Sayfa64)))
                {
                    cop.Clear();
                    int sut = sayfa == Sayfa64.RpgBase ? 20 : 12;
                    int sat = sayfa == Sayfa64.RpgBase ? 13 : 3;
                    const int aralik = 2;

                    for (int i = 0; i < sut * sat; i++)
                    {
                        Sprite s = Karo64.Al(sayfa, i);
                        if (s == null) continue;
                        var go = new GameObject("k" + i);
                        cop.Add(go);
                        go.transform.position = new Vector3((i % sut) * aralik, -(i / sut) * aralik, 0f);
                        go.AddComponent<SpriteRenderer>().sprite = s;
                    }

                    Kare(cikti, "00-karo-" + sayfa.ToString().ToLowerInvariant() + ".png", cop,
                         new Vector2((sut - 1) * aralik / 2f, -(sat - 1) * aralik / 2f),
                         sat * aralik / 2f + 1f, sut * 90, sat * 90 + 90);

                    foreach (var go in cop) if (go != null) UnityEngine.Object.DestroyImmediate(go);
                }
                Debug.Log("[Amorland] Karo tablolari yazildi.");
            }
            finally
            {
                foreach (var go in cop) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Oyuncunun Play tusuna bastiginda GORECEGI kareyi uretir.
        ///
        /// ⚠ Neden ayri: <see cref="Uret"/> haritayi kendi kurdugu bir kamerayla cekiyor
        /// — yani haritanin dogru oldugunu gosterir ama SAHNENIN calistigini gostermez.
        /// Bu metot gercek <see cref="VadiSahnesi"/> bilesenini kurar; kamera, gezgin,
        /// bolge etiketleri ve arayuz hangi yoldan geliyorsa o yoldan gelir. Derleme
        /// yesil oldugu halde sahnenin patladigi bir durum burada yakalanir.
        ///
        /// ⚠ Ekran uzayi kanvas RenderTexture'a cizilmez; kanvaslar gecici olarak
        /// kameraya baglanir, yoksa arayuz karede hic gorunmez ve "arayuz yok" sanilir.
        /// </summary>
        public static void Sahne()
        {
            string cikti = CiktiKlasoru();
            GameObject kok = null;
            RenderTexture rt = null;
            int hata = 0;

            try
            {
                Karo64.Unut();
                VadiHaritasi.OnbellegiTemizle();

                kok = new GameObject("VadiSahnesi");
                var sahne = kok.AddComponent<Amorland.VadiSahnesi>();
                sahne.Kur();

                Camera cam = kok.GetComponentInChildren<Camera>();
                if (cam == null) throw new Exception("VadiSahnesi kamera kurmadi.");

                const int en = 1280, boy = 720;
                rt = new RenderTexture(en, boy, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
                rt.Create();
                cam.targetTexture = rt;

                foreach (var k in kok.GetComponentsInChildren<Canvas>(true))
                {
                    if (k.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    k.renderMode = RenderMode.ScreenSpaceCamera;
                    k.worldCamera = cam;
                    k.planeDistance = 1f;
                }
                Canvas.ForceUpdateCanvases();

                cam.Render();

                RenderTexture onceki = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(en, boy, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, en, boy), 0, 0);
                tex.Apply();
                RenderTexture.active = onceki;

                File.WriteAllBytes(Path.Combine(cikti, "00-play-ekrani.png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                cam.targetTexture = null;

                Debug.Log("[Amorland] Play ekrani uretildi: " + cikti);
            }
            catch (Exception e)
            {
                Debug.LogError("[Amorland] Vadi sahnesi kurulamadi: "
                               + e.GetType().Name + " — " + e.Message + "\n" + e.StackTrace);
                hata = 1;
            }
            finally
            {
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                if (kok != null) UnityEngine.Object.DestroyImmediate(kok);
            }

            if (Application.isBatchMode) EditorApplication.Exit(hata);
        }

        public static void Uret()
        {
            string cikti = CiktiKlasoru();
            var cop = new List<GameObject>();
            int hata = 0;

            try
            {
                Karo64.Unut();
                VadiHaritasi.OnbellegiTemizle();

                var kokGo = new GameObject("VadiHaritasi");
                cop.Add(kokGo);
                var harita = kokGo.AddComponent<VadiHaritasi>();
                harita.Kur();

                // Tum vadi tek karede: yerlesim, nehir yatagi ve yol agi burada okunur.
                Kare(cikti, "01-vadi-tam.png", cop,
                     new Vector2(VadiHaritasi.En / 2f, VadiHaritasi.Boy / 2f),
                     VadiHaritasi.Boy / 2f + 2f, 1860, 1360);

                // Oyun yakinligi: ekranda ~32 karo. "Gezerken boyle gorunur."
                foreach (var b in harita.Bolgeler)
                    Kare(cikti, "02-" + b.Ad + ".png", cop, b.Merkez, 9f, 1280, 720);

                // Koprunun oldugu yer: nehrin ana yolu kestigi nokta.
                Kare(cikti, "03-kopru.png", cop, new Vector2(296f, 132f), 11f, 1280, 720);

                // Meyve bahcesi ve iskele resmi bolge degil; ayrica cekilir.
                Kare(cikti, "04-bahce-iskele.png", cop, new Vector2(280f, 232f), 26f, 1280, 720);

                Debug.Log("[Amorland] Harita goruntuleri uretildi: " + cikti);
            }
            catch (Exception e)
            {
                Debug.LogError("[Amorland] Harita goruntusu uretilemedi: "
                               + e.GetType().Name + " — " + e.Message + "\n" + e.StackTrace);
                hata = 1;
            }
            finally
            {
                foreach (var go in cop) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            if (Application.isBatchMode) EditorApplication.Exit(hata);
        }

        private static string CiktiKlasoru()
        {
            string c = Arguman("-cikti");
            if (string.IsNullOrEmpty(c)) c = Path.Combine(Path.GetTempPath(), "amorland-harita");
            Directory.CreateDirectory(c);
            return c;
        }

        private static void Kare(string klasor, string dosya, List<GameObject> cop,
                                 Vector2 merkez, float ortoBoy, int en, int boy)
        {
            RenderTexture rt = null;
            GameObject camGo = null;
            try
            {
                rt = new RenderTexture(en, boy, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
                rt.Create();

                camGo = new GameObject("HaritaKamera");
                cop.Add(camGo);
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = ortoBoy;
                cam.nearClipPlane = -100f;
                cam.farClipPlane = 100f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                // Harita disinda kalan alan: koyu, harita sinirini belli etsin.
                cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
                cam.targetTexture = rt;
                // 2B harita: kamera duz karsidan bakar (izometrik degil, tepeden).
                camGo.transform.position = new Vector3(merkez.x, merkez.y, -10f);
                camGo.transform.rotation = Quaternion.identity;

                cam.Render();

                RenderTexture onceki = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(en, boy, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, en, boy), 0, 0);
                tex.Apply();
                RenderTexture.active = onceki;

                File.WriteAllBytes(Path.Combine(klasor, dosya), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Debug.Log("[Amorland] Yazildi: " + dosya);
            }
            finally
            {
                if (camGo != null)
                {
                    var c = camGo.GetComponent<Camera>();
                    if (c != null) c.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(camGo);
                    cop.Remove(camGo);
                }
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            }
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
