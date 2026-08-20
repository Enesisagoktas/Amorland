using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// Tek tikla proje kurulumu.
    ///
    /// A.3'te yazdigi gibi editor isi (sahne, prefab, inspector) bu projede en kirilgan
    /// katman. Bu script o isin <b>otomatiklestirilebilir</b> kismini yapar: sahne olusturur,
    /// yapim ayarlarina ekler, oyuncu ayarlarini yatay + dogru girdi + dogru ses politikasina
    /// getirir ve URP varsa boru hattini kurar.
    ///
    /// URP'ye <b>reflection ile</b> dokunulur: paket yoksa bu dosya yine de derlenir.
    /// Sert bagimlilik koyulsaydi URP'siz bir projede editor script'i patlar ve
    /// gelistirici hicbir menuyu goremezdi.
    ///
    /// Menu: <c>Amorland &gt; Kurulumu Calistir</c>
    /// </summary>
    public static class AmorlandKurulum
    {
        private const string SahneYolu = "Assets/Amorland/Sahneler/Amorland.unity";
        private const string UrpKlasoru = "Assets/Amorland/Render";

        [MenuItem("Amorland/Kurulumu Calistir", false, 1)]
        public static void Calistir()
        {
            try
            {
                SahneKur();
                OyuncuAyarlari();
                KaliteAyarlari();
                UrpKur();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    "[Amorland] Kurulum tamam.\n"
                    + "- Sahne: " + SahneYolu + " (yapim ayarlarina eklendi)\n"
                    + "- Yon: yatay, uyku kapali\n"
                    + "- Girdi: hem yeni hem eski sistem etkin\n"
                    + "- iOS 'Mute Other Audio Sources' KAPALI (oyuncunun muzigi kesilmez)\n"
                    + "- Gercek zamanli golge kapali\n"
                    + "Simdi Play'e basabilirsin. Sunucu calisiyor olmali.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Amorland] Kurulum basarisiz: " + e);
            }
        }

        [MenuItem("Amorland/Ogreticiyi Sifirla", false, 20)]
        public static void OgreticiSifirla()
        {
            PlayerPrefs.DeleteKey("amorland.ogretici.bitti");
            PlayerPrefs.Save();
            Debug.Log("[Amorland] Ogretici sifirlandi.");
        }

        [MenuItem("Amorland/Oturumu Sifirla (yeni misafir hesap)", false, 21)]
        public static void OturumSifirla()
        {
            PlayerPrefs.DeleteKey("amorland.yenileme");
            PlayerPrefs.DeleteKey("amorland.cihaz.id");
            PlayerPrefs.DeleteKey("amorland.ogretici.bitti");
            PlayerPrefs.Save();
            Debug.Log("[Amorland] Oturum sifirlandi. Sonraki Play yeni misafir hesap acar.");
        }

        [MenuItem("Amorland/Ikinci Oyuncu Kimligi Uret", false, 22)]
        public static void IkinciOyuncu()
        {
            // Ayni makinede iki oyuncu test etmek icin: cihaz kimligini degistirir.
            string yeni = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("amorland.cihaz.id", yeni);
            PlayerPrefs.DeleteKey("amorland.yenileme");
            PlayerPrefs.Save();
            Debug.Log("[Amorland] Yeni cihaz kimligi: " + yeni + "\nSonraki Play ikinci oyuncu olarak girer.");
        }

        // ------------------------------------------------------------------ sahne

        private static void SahneKur()
        {
            string klasor = Path.GetDirectoryName(SahneYolu);
            if (!string.IsNullOrEmpty(klasor) && !Directory.Exists(klasor))
            {
                Directory.CreateDirectory(klasor);
                AssetDatabase.Refresh();
            }

            if (!File.Exists(SahneYolu))
            {
                // Bos sahne yeter: her sey Onyukleyici tarafindan kodla kurulur.
                Scene sahne = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(sahne, SahneYolu);
            }

            var mevcut = EditorBuildSettings.scenes;
            foreach (var s in mevcut)
            {
                if (s.path == SahneYolu) return;
            }
            var yeni = new EditorBuildSettingsScene[mevcut.Length + 1];
            yeni[0] = new EditorBuildSettingsScene(SahneYolu, true);
            Array.Copy(mevcut, 0, yeni, 1, mevcut.Length);
            EditorBuildSettings.scenes = yeni;
        }

        // ------------------------------------------------------------------ oyuncu ayarlari

        private static void OyuncuAyarlari()
        {
            PlayerSettings.companyName = "Amorland";
            PlayerSettings.productName = "Amorland";

            // B.0: yatay oyun.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // ⚠ B.26 yasak 16: oyuncunun muzigini/podcast'ini KESME.
            // Bu bayrak acik olursa iOS'ta oyun basladiginda arka plandaki ses susar.
            PlayerSettings.muteOtherAudioSources = false;

            // B.2: yeni Input System. "Both" secilir ki eski girdi kolu da calisir
            // ve paket kaldirilirsa oyun aninda kirilmasin.
            AktifGirdiSistemiAyarla(2);

            // Low-poly duz renk icin dogrusal renk uzayi daha tutarli.
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        /// <summary>
        /// <c>activeInputHandler</c> genel API'de yok; ProjectSettings varligindan
        /// SerializedObject ile yazilir. 0 = eski, 1 = yeni, 2 = ikisi.
        /// </summary>
        private static void AktifGirdiSistemiAyarla(int deger)
        {
            var varliklar = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (varliklar == null || varliklar.Length == 0) return;

            var so = new SerializedObject(varliklar[0]);
            SerializedProperty prop = so.FindProperty("activeInputHandler");
            if (prop == null) return;
            if (prop.intValue == deger) return;
            prop.intValue = deger;
            so.ApplyModifiedProperties();
            Debug.Log("[Amorland] Girdi sistemi 'Both' yapildi. Unity yeniden baslatma isteyebilir.");
        }

        private static void KaliteAyarlari()
        {
            // B.2: gercek zamanli golge YOK — sahte blob golge kullanilir.
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowCascades = 0;
                QualitySettings.antiAliasing = 0;   // B.22: kenar yumusatma yok
                QualitySettings.vSyncCount = 0;     // hedef kare hizi kodda ayarlanir
            }
        }

        // ------------------------------------------------------------------ URP

        /// <summary>
        /// URP varliklarini olusturur ve grafik ayarlarina baglar.
        /// Paket yoksa sessizce atlanir; oyun yerlesik boru hattinda da calisir
        /// (materyaller shader'i kademeli olarak arar).
        /// </summary>
        private static void UrpKur()
        {
            Type urpVarlikTipi = TipBul("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset");
            Type rendererTipi = TipBul("UnityEngine.Rendering.Universal.UniversalRendererData");

            if (urpVarlikTipi == null)
            {
                Debug.Log(
                    "[Amorland] URP paketi bulunamadi. Oyun yerlesik boru hattinda calisir. "
                    + "URP istiyorsan Package Manager'dan 'Universal RP' ekleyip kurulumu tekrar calistir.");
                return;
            }

            if (!Directory.Exists(UrpKlasoru))
            {
                Directory.CreateDirectory(UrpKlasoru);
                AssetDatabase.Refresh();
            }

            string varlikYolu = UrpKlasoru + "/AmorlandUrp.asset";
            var mevcut = AssetDatabase.LoadAssetAtPath<ScriptableObject>(varlikYolu);
            if (mevcut != null)
            {
                GrafikAyarinaBagla(mevcut);
                return;
            }

            ScriptableObject rendererData = null;
            if (rendererTipi != null)
            {
                rendererData = ScriptableObject.CreateInstance(rendererTipi);
                AssetDatabase.CreateAsset(rendererData, UrpKlasoru + "/AmorlandRenderer.asset");
            }

            ScriptableObject urp = null;
            MethodInfo olustur = urpVarlikTipi.GetMethod(
                "Create", BindingFlags.Public | BindingFlags.Static);
            if (olustur != null && rendererData != null)
            {
                try
                {
                    urp = olustur.Invoke(null, new object[] { rendererData, false }) as ScriptableObject;
                }
                catch (Exception)
                {
                    urp = null;
                }
            }
            if (urp == null) urp = ScriptableObject.CreateInstance(urpVarlikTipi);

            AssetDatabase.CreateAsset(urp, varlikYolu);
            GrafikAyarinaBagla(urp);
        }

        private static void GrafikAyarinaBagla(ScriptableObject urp)
        {
            var grafik = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (grafik == null || grafik.Length == 0) return;

            var so = new SerializedObject(grafik[0]);
            SerializedProperty prop = so.FindProperty("m_CustomRenderPipeline");
            if (prop != null)
            {
                prop.objectReferenceValue = urp;
                so.ApplyModifiedProperties();
            }

            var kalite = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (kalite != null && kalite.Length > 0)
            {
                var qso = new SerializedObject(kalite[0]);
                SerializedProperty diziler = qso.FindProperty("m_QualitySettings");
                if (diziler != null)
                {
                    for (int i = 0; i < diziler.arraySize; i++)
                    {
                        SerializedProperty boru =
                            diziler.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline");
                        if (boru != null) boru.objectReferenceValue = urp;
                    }
                    qso.ApplyModifiedProperties();
                }
            }
        }

        private static Type TipBul(string tamAd)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = a.GetType(tamAd, false);
                if (t != null) return t;
            }
            return null;
        }
    }
}
