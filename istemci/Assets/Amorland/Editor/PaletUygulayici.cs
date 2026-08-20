using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// Kenney kit materyallerini Amorland paletine boyar.
    ///
    /// Spec B.22 kaynak stratejisi: "uretim sonrasi ortak palete <b>normalize edilir</b>."
    /// nature-kit'in kendi paleti turkuaz/turuncudur (kitin Sample.png'sine bak) —
    /// tutarli ama Amorland'in sicak yesil/kahve dunyasina ait degil.
    ///
    /// ⚠ Neden ayri bir komut, neden AssetPostprocessor degil:
    /// <c>materialLocation = External</c> ile materyaller BAGIMSIZ .mat varliklari olarak
    /// uretiliyor ve bu akista <c>OnAssignMaterialModel</c> cagrilmiyor. Ilk denemede
    /// postprocessor sessizce hicbir sey yapmadi, modeller turkuaz kaldi. Materyali
    /// dogrudan yazmak tek kesin yol.
    ///
    /// Modeller ada gore materyal PAYLASTIGI icin 26 satirlik eslesme butun kiti boyar.
    ///
    /// Calistirma:
    ///   Unity -batchmode -projectPath &lt;proje&gt; \
    ///         -executeMethod Amorland.EditorAraclari.PaletUygulayici.Uygula -quit
    /// </summary>
    public static class PaletUygulayici
    {
        private const string VarlikKok = "Assets/Amorland/Resources/varlik";
        private const string EslesmeYolu = VarlikKok + "/palet-esleme.json";

        [MenuItem("Amorland/Paleti Modellere Uygula")]
        public static void Uygula()
        {
            Dictionary<string, Color> palet = EslemeOku();
            if (palet.Count == 0)
            {
                Debug.LogError("[Amorland] palet-esleme.json okunamadi: " + EslesmeYolu);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            string[] kimlikler = AssetDatabase.FindAssets("t:Material", new[] { VarlikKok });
            int boyanan = 0, atlanan = 0;
            var bilinmeyen = new SortedSet<string>();

            foreach (string kimlik in kimlikler)
            {
                string yol = AssetDatabase.GUIDToAssetPath(kimlik);
                var m = AssetDatabase.LoadAssetAtPath<Material>(yol);
                if (m == null) continue;

                if (!palet.TryGetValue(m.name, out Color renk))
                {
                    bilinmeyen.Add(m.name);
                    atlanan++;
                    continue;
                }

                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", renk);
                if (m.HasProperty("_Color")) m.SetColor("_Color", renk);
                // Kozy low-poly'de parlaklik yok: speculer birakilirsa plastik gorunur.
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0f);
                if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", 0f);
                EditorUtility.SetDirty(m);
                boyanan++;
            }

            KarakterKaplamasi();

            AssetDatabase.SaveAssets();
            Debug.Log($"[Amorland] Palet uygulandi: {boyanan} materyal boyandi, {atlanan} atlandi.");
            if (bilinmeyen.Count > 0)
            {
                Debug.LogWarning("[Amorland] Eslesmede olmayan materyal adlari: "
                                 + string.Join(", ", bilinmeyen));
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Karakterin "skin" materyaline kaplama dokusunu baglar.
        ///
        /// ⚠ Bu materyal palete girmez: karakter DUZ RENK degil, dokulu. Palet
        /// eslesmesinde olmadigi icin ilk kosuda atlandi ve karakter bembeyaz kaldi.
        /// </summary>
        private static void KarakterKaplamasi()
        {
            string[] adaylar =
            {
                VarlikKok + "/karakter/skaterMaleA.png",
                VarlikKok + "/karakter/criminalMaleA.png",
                VarlikKok + "/karakter/skaterFemaleA.png",
                VarlikKok + "/karakter/cyborgFemaleA.png",
            };

            Texture2D doku = null;
            foreach (string y in adaylar)
            {
                doku = AssetDatabase.LoadAssetAtPath<Texture2D>(y);
                if (doku != null) break;
            }
            if (doku == null)
            {
                Debug.LogWarning("[Amorland] Karakter kaplamasi bulunamadi, model duz kalacak.");
                return;
            }

            foreach (string kimlik in AssetDatabase.FindAssets("t:Material", new[] { VarlikKok }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(kimlik));
                if (m == null || m.name != "skin") continue;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", doku);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", doku);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
                EditorUtility.SetDirty(m);
                Debug.Log("[Amorland] Karakter kaplamasi baglandi: " + doku.name);
            }
        }

        private static Dictionary<string, Color> EslemeOku()
        {
            var p = new Dictionary<string, Color>();
            if (!File.Exists(EslesmeYolu)) return p;

            // Dosya kucuk ve sabit sekilli; tam JSON ayristiricisi getirmeye deger degil.
            foreach (string satir in File.ReadAllLines(EslesmeYolu))
            {
                Match m = Regex.Match(
                    satir,
                    "\"([A-Za-z_][A-Za-z0-9_]*)\"\\s*:\\s*\\[\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*\\]");
                if (!m.Success) continue;
                p[m.Groups[1].Value] = new Color(
                    int.Parse(m.Groups[2].Value) / 255f,
                    int.Parse(m.Groups[3].Value) / 255f,
                    int.Parse(m.Groups[4].Value) / 255f, 1f);
            }
            return p;
        }
    }
}
