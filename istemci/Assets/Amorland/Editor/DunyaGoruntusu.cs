using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Amorland.Ag;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// 3B dunyanin PNG goruntusunu BASSIZ uretir.
    ///
    /// Neden ayri bir arac: <see cref="ArayuzGoruntusu"/> yalnizca kanvasi cizer, dunyayi
    /// cizmez. Dunyanin gorunup gorunmedigi ise bu projede gercek bir soru oldu — yukari
    /// bakan butun yuzler ters sarilmisti ve oyun "calisiyor" ama ekran bostu. Play moduna
    /// girmeden bunu gorebilmek gerekiyor.
    ///
    /// Kamera oyunun kendi matematigini birebir tekrarlar (<see cref="KameraYoneticisi"/>):
    /// ortografik, yaw kilitli, pitch denge dosyasindan, mesafe 20.
    ///
    /// Calistirma:
    ///   node araclar/durum-dokumu.js durum.json
    ///   Unity -batchmode -projectPath &lt;proje&gt; \
    ///         -executeMethod Amorland.EditorAraclari.DunyaGoruntusu.Uret \
    ///         -dengeYolu &lt;denge.json&gt; -durumYolu &lt;durum.json&gt; -cikti &lt;klasor&gt; -quit
    ///
    /// ⚠ <c>-nographics</c> KULLANMA: render icin grafik aygiti gerekir.
    /// </summary>
    public static class DunyaGoruntusu
    {
        private const int Genislik = 1280;
        private const int Yukseklik = 720;
        private const float Mesafe = 20f;

        private static readonly List<string> _hatalar = new List<string>();
        private static int _uretilen;

        [MenuItem("Amorland/Dunya Goruntusu Uret")]
        public static void Uret()
        {
            _hatalar.Clear();
            _uretilen = 0;

            string cikti = Arguman("-cikti");
            if (string.IsNullOrEmpty(cikti)) cikti = Path.Combine(Path.GetTempPath(), "amorland-dunya");
            Directory.CreateDirectory(cikti);

            var denge = DengeYukle(Arguman("-dengeYolu"));
            var durum = DurumYukle(Arguman("-durumYolu"));

            float pitch = denge.Yuklendi ? denge.KameraPitch : 50f;
            float yakin = denge.Yuklendi ? denge.ZoomYakin : 5f;
            float uzak = denge.Yuklendi ? denge.ZoomUzak : 8f;

            // Karakterin durdugu yer (Oyun.cs ile ayni baslangic konumu).
            Vector3 oyuncu = new Vector3(1f, 0f, -2f);

            Kare(cikti, "01-dunya-yakin.png", durum, denge, oyuncu, pitch, yakin);
            Kare(cikti, "02-dunya-uzak.png", durum, denge, oyuncu, pitch, uzak);
            // Genis kus bakisi: zeminin ve parselin tamami gorunsun.
            Kare(cikti, "03-dunya-genis.png", durum, denge, new Vector3(2f, 0f, 0f), pitch, 14f);

            Debug.Log("[Amorland] " + _uretilen + " dunya karesi uretildi, " + _hatalar.Count
                      + " hata. Klasor: " + cikti);
            foreach (string h in _hatalar) Debug.LogError("[Amorland] KARE HATASI: " + h);

            if (Application.isBatchMode) EditorApplication.Exit(_hatalar.Count == 0 ? 0 : 1);
        }

        // ------------------------------------------------------------------ kare

        private static void Kare(
            string klasor, string dosya, DunyaDurumu durum, DengeVerisi denge,
            Vector3 merkez, float pitch, float zoom)
        {
            Camera cam = null;
            RenderTexture rt = null;
            var cop = new List<GameObject>();

            try
            {
                rt = new RenderTexture(Genislik, Yukseklik, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 8
                };
                rt.Create();

                var camGo = new GameObject("DunyaKamera");
                cop.Add(camGo);
                cam = camGo.AddComponent<Camera>();

                // KameraYoneticisi.Yerlestir ile AYNI matematik. Farkli olsaydi goruntu
                // oyunda gorunenden baska bir sey gosterir, denetim degeri kalmazdi.
                cam.orthographic = true;
                cam.orthographicSize = zoom;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Palet.Gokyuzu;
                cam.targetTexture = rt;

                // ⚠ ISIK SART. Prosedurel mesh'ler ve sprite'lar Unlit'ti, isiksiz de
                // dogru gorunuyordu. Kenney modelleri LIT shader kullaniyor: isiksiz
                // sahnede yalnizca ortam rengiyle, turkuaza calan bir tonda cikiyorlar.
                // Oyunun kendi isigiyla ayni acilar (Oyun.SahneKur).
                var isikGo = new GameObject("Isik");
                cop.Add(isikGo);
                var isik = isikGo.AddComponent<Light>();
                isik.type = LightType.Directional;
                isik.color = new Color(1f, 0.97f, 0.90f);
                isik.intensity = 1.15f;
                isik.shadows = LightShadows.None;
                isikGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.52f, 0.54f, 0.50f);

                float radyan = pitch * Mathf.Deg2Rad;
                Vector3 yon = new Vector3(0f, Mathf.Sin(radyan), -Mathf.Cos(radyan));
                camGo.transform.position = merkez + yon * Mesafe;
                camGo.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);

                // Gercek tarla gorunumu: zemin, kuyu, karolar, dugumler.
                var tarlaGo = new GameObject("TarlaGorunumu");
                cop.Add(tarlaGo);
                var tarla = tarlaGo.AddComponent<TarlaGorunumu>();
                tarla.Kur(durum, denge);

                KarakterKur(cop, denge, new Vector3(1f, 0f, -2f));

                cam.Render();

                RenderTexture onceki = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(Genislik, Yukseklik, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, Genislik, Yukseklik), 0, 0);
                tex.Apply();
                RenderTexture.active = onceki;

                File.WriteAllBytes(Path.Combine(klasor, dosya), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                _uretilen++;
                Debug.Log("[Amorland] Yazildi: " + dosya);
            }
            catch (Exception e)
            {
                _hatalar.Add(dosya + ": " + e.GetType().Name + " — " + e.Message);
                Debug.LogWarning("[Amorland] " + dosya + " cizilemedi: " + e);
            }
            finally
            {
                foreach (var go in cop) if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (cam != null) cam.targetTexture = null;
                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }
            }
        }

        /// <summary>Oyuncu gorseli: Oyun.cs'teki kurulumun aynisi (govde + sahte golge).</summary>
        private static void KarakterKur(List<GameObject> cop, DengeVerisi denge, Vector3 konum)
        {
            var kok = new GameObject("Karakter");
            cop.Add(kok);
            kok.transform.position = konum;

            float pitch = denge.Yuklendi ? denge.KameraPitch : 50f;
            if (VarlikKatalogu.SetVar
                && VarlikKatalogu.KarakterKur(kok.transform, Vector3.zero, out Animator anim) != null)
            {
                // ⚠ BURADA ANIMATOR TIKLATILMIYOR — bilerek.
                // Denendi ve BOZDU: Kenney'in ayri animasyon FBX'leri (anim_idle/run)
                // model FBX'inin iskeletiyle birebir uyusmuyor. Avatar CopyFromOther ile
                // baglansa bile kemikler yanlis donusum aliyor ve skinned mesh ekran
                // boyunca geriliyor; karakter kayboluyor. Tik atilmayinca model bind
                // pozunda (T-pozu) kalir — dogru degil ama DURUST: animasyonun henuz
                // calismadigini gosterir. Cozum retarget isi ve ayri ele alinacak.
                if (anim != null) { }
                SpriteDeposu.Yatik("golge", kok.transform, new Vector3(0f, 0.05f, 0f), null, 0.62f);
                return;
            }
            if (SpriteDeposu.SetVar)
            {
                SpriteDeposu.Yatik("golge", kok.transform, new Vector3(0f, 0.05f, 0f), null, 0.62f);
                SpriteDeposu.Nesne("karakter_0", kok.transform, Vector3.zero, pitch, 0.05f);
                return;
            }
            Color renk = denge.Yuklendi ? denge.OyuncuRengi(0) : Palet.Cati;
            MateryalDeposu.Nesne(
                "Govde", MeshUretici.Karakter(renk, new Color(0.88f, 0.76f, 0.64f)),
                kok.transform, Vector3.zero);
            MateryalDeposu.Nesne(
                "Golge", MeshUretici.Disk(0.45f, 10, Palet.Golge),
                kok.transform, new Vector3(0f, 0.01f, 0f), saydam: true);
        }

        // ------------------------------------------------------------------ veri

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
                Debug.LogWarning("[Amorland] Denge dosyasi yok (" + yol + "), varsayilanlar kullanilacak.");
            }
            return denge;
        }

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
