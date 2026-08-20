using UnityEditor;
using UnityEngine;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// <c>Resources/varlik/</c> altindaki Kenney modellerinin import ayarlarini zorlar.
    ///
    /// Varliklar CC0 (Kenney.nl) — spec B.22 kaynak stratejisi 2: "CC0 2B/3B paketler".
    /// Lisans metni <c>Resources/varlik/KENNEY-LICENSE.txt</c> icinde.
    ///
    /// ⚠ En kritik ayar <see cref="ModelImporter.materialSearch"/>. Kenney kitlerinde
    /// materyaller ISIMLE tanimli ("wood", "leafsDark", "roof") ve ayni isim onlarca
    /// modelde tekrar eder. Varsayilan import her FBX icin AYRI materyal uretir:
    /// 186 model x ~4 materyal = yuzlerce materyal ve o kadar cizim cagrisi. Everywhere
    /// + BasedOnMaterialName ile ayni isimli materyal TEK dosyaya baglanir; mobil
    /// hedefte bu ayar performansin kendisidir, kozmetik degil.
    ///
    /// ⚠ Editor kurulumu gerektirmez (mimari kural: her sey kodla kurulur) — bu dosya
    /// import aninda calisir, kimsenin Inspector'da bir sey isaretlemesi gerekmez.
    /// </summary>
    public class ModelIceAktarici : AssetPostprocessor
    {
        private const string Klasor = "/Resources/varlik/";
        private const string KarakterKlasoru = "/Resources/varlik/karakter/";

        private void OnPreprocessModel()
        {
            string yol = assetPath == null ? null : assetPath.Replace('\\', '/');
            if (yol == null || yol.IndexOf(Klasor) < 0) return;

            var mi = (ModelImporter)assetImporter;

            // Kenney kitlerinde 1 birim = 1 karo. Oyunun izgarasi da 1 karo = 1 birim (B.5).
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importVisibility = false;
            mi.isReadable = false;

            // Materyal paylasimi — bu sinifin varlik sebebi.
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialLocation = ModelImporterMaterialLocation.External;
            mi.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            mi.materialSearch = ModelImporterMaterialSearch.Everywhere;

            bool karakter = yol.IndexOf(KarakterKlasoru) >= 0;
            if (karakter)
            {
                // Karakter animasyonlu (idle/run/jump ayri FBX). Generic rig yeterli:
                // Humanoid retarget'i bu tek modelde kazanc getirmez, kurulum maliyeti getirir.
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.importAnimation = true;
                mi.importNormals = ModelImporterNormals.Import;
                // ⚠ Avatar uretimi acikca istenmeli. Yalnizca animationType vermek
                // yetmiyor: avatar olmadan animasyon klipleri modelin iskeletiyle
                // konusamaz ve skinned mesh deforme olur.
                if (yol.EndsWith("/karakter.fbx"))
                {
                    mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                }
            }
            else
            {
                // Prop'larda animasyon yok: rig import etmek sahneye bos Animator ve
                // gereksiz kemik hiyerarsisi ekler.
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
                // ⚠ Sert normal SART: Kenney low-poly modelleri yumusak normalle
                // "eriyik plastik" gibi gorunur. Kenar acisi 0 = her yuz kendi normali.
                mi.importNormals = ModelImporterNormals.Calculate;
                mi.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
                mi.importTangents = ModelImporterTangents.None;
            }
        }

        /// <summary>
        /// Materyali URP shader'ina cevirir ve rengini AMORLAND PALETINE normalize eder.
        ///
        /// ⚠ Iki ayri sorun cozuluyor:
        /// (a) FBX diffuse rengi TASIMIYOR — Unity'nin urettigi materyal bembeyaz kalir.
        ///     Renkler kitin .mtl/.glb dosyalarinda; buraya <c>palet-esleme.json</c>
        ///     olarak cikarildi.
        /// (b) nature-kit'in KENDI paleti turkuaz/turuncudur (kitin Sample.png'sine bak).
        ///     Tutarli ama Amorland'in sicak yesil/kahve dunyasina ait degil. Spec B.22:
        ///     "uretim sonrasi ortak palete normalize edilir."
        ///
        /// Materyaller ada gore PAYLASILDIGI icin 26 satirlik eslesme butun modelleri
        /// birden boyar — model basina is yok.
        /// </summary>
        private Material OnAssignMaterialModel(Material materyal, Renderer isleyici)
        {
            string yol = assetPath == null ? null : assetPath.Replace('\\', '/');
            if (yol == null || yol.IndexOf(Klasor) < 0 || materyal == null) return null;

            Shader s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s != null && materyal.shader != s) materyal.shader = s;

            if (PaletRengi(materyal.name, out Color renk))
            {
                if (materyal.HasProperty("_BaseColor")) materyal.SetColor("_BaseColor", renk);
                if (materyal.HasProperty("_Color")) materyal.SetColor("_Color", renk);
                // Kozy low-poly: parlaklik yok. Speculer birakilirsa plastik gorunur.
                if (materyal.HasProperty("_Smoothness")) materyal.SetFloat("_Smoothness", 0f);
                if (materyal.HasProperty("_SpecularHighlights")) materyal.SetFloat("_SpecularHighlights", 0f);
            }
            return null;
        }

        private static System.Collections.Generic.Dictionary<string, Color> _palet;

        private static bool PaletRengi(string ad, out Color renk)
        {
            renk = Color.white;
            if (string.IsNullOrEmpty(ad)) return false;

            if (_palet == null)
            {
                _palet = new System.Collections.Generic.Dictionary<string, Color>();
                string p = "Assets/Amorland/Resources/varlik/palet-esleme.json";
                if (System.IO.File.Exists(p))
                {
                    // Kucuk ve sabit bir dosya; tam JSON ayristiricisi getirmek yerine
                    // "ad": [r, g, b] satirlari okunur.
                    foreach (string satir in System.IO.File.ReadAllLines(p))
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(
                            satir, "\"([A-Za-z_][A-Za-z0-9_]*)\"\\s*:\\s*\\[\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*\\]");
                        if (!m.Success) continue;
                        _palet[m.Groups[1].Value] = new Color(
                            int.Parse(m.Groups[2].Value) / 255f,
                            int.Parse(m.Groups[3].Value) / 255f,
                            int.Parse(m.Groups[4].Value) / 255f, 1f);
                    }
                }
                if (_palet.Count == 0)
                {
                    Debug.LogWarning("[Amorland] palet-esleme.json okunamadi — Kenney kitleri "
                                     + "kendi turkuaz paletiyle kalacak.");
                }
            }

            return _palet.TryGetValue(ad, out renk);
        }
    }
}
