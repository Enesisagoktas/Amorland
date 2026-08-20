using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// Karakter animasyon denetleyicisini kurar.
    ///
    /// ⚠ Neden gerekli: Kenney karakteri rigli bir FBX ve animasyonlari AYRI dosyalarda
    /// (idle/run/jump). Animator olmadan model BIND POZUNDA — yani kollari iki yana acik
    /// "T-pozunda" duruyor ve oyun bozuk gorunuyor. Ilk entegrasyonda tam bunu yapti.
    ///
    /// Denetleyici burada KODLA uretilir; kimsenin Animator penceresinde durum
    /// baglamasi gerekmez (mimari kural: her sey kodla kurulur).
    ///
    /// Calistirma:
    ///   Unity -batchmode -projectPath &lt;proje&gt; \
    ///         -executeMethod Amorland.EditorAraclari.KarakterKurulumu.Kur -quit
    /// </summary>
    public static class KarakterKurulumu
    {
        private const string Klasor = "Assets/Amorland/Resources/varlik/karakter";
        // ⚠ Ad FBX'ten FARKLI olmali: ikisi de "karakter" olsaydi Resources.Load ayni
        // yolda iki varlik gorur ve hangisinin dondugu tipe kalirdi — kirilgan.
        private const string DenetleyiciYolu = Klasor + "/karakter-animator.controller";

        [MenuItem("Amorland/Karakter Animasyonunu Kur")]
        public static void Kur()
        {
            AvatarPaylas();

            AnimationClip bosta = Klip("anim_idle");
            AnimationClip kosu = Klip("anim_run");

            if (bosta == null)
            {
                Debug.LogError("[Amorland] anim_idle klibi bulunamadi: " + Klasor);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var dc = AnimatorController.CreateAnimatorControllerAtPath(DenetleyiciYolu);
            var katman = dc.layers[0].stateMachine;

            var bostaDurum = katman.AddState("Bosta");
            bostaDurum.motion = bosta;
            katman.defaultState = bostaDurum;

            if (kosu != null)
            {
                // "Yuruyor" bool'u: KarakterKontrolcusu hareket ederken true yazar.
                dc.AddParameter("Yuruyor", AnimatorControllerParameterType.Bool);

                var kosuDurum = katman.AddState("Kosu");
                kosuDurum.motion = kosu;

                var ileri = bostaDurum.AddTransition(kosuDurum);
                ileri.AddCondition(AnimatorConditionMode.If, 0f, "Yuruyor");
                ileri.duration = 0.12f;
                ileri.hasExitTime = false;

                var geri = kosuDurum.AddTransition(bostaDurum);
                geri.AddCondition(AnimatorConditionMode.IfNot, 0f, "Yuruyor");
                geri.duration = 0.12f;
                geri.hasExitTime = false;
            }

            // Klipler dongusel olmali: tek seferlik oynayip donan bir idle, karakteri
            // bir saniye sonra yine T-pozunda birakir.
            DonguYap("anim_idle");
            DonguYap("anim_run");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Amorland] Karakter denetleyicisi kuruldu: " + DenetleyiciYolu
                      + (kosu != null ? " (bosta + kosu)" : " (yalnizca bosta)"));

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Animasyon FBX'lerini modelin AVATARINA baglar.
        ///
        /// ⚠ Bu adim olmadan karakter deforme oluyor. Kenney'de model ve animasyonlar
        /// AYRI FBX dosyalari; her biri kendi iskeletini ve avatarini uretir. Klip,
        /// modelin iskeletiyle eslesmeyen bir avatar uzerinden calisinca kemikler
        /// anlamsiz donusum aliyor ve skinned mesh ekran boyunca geriliyor — ilk
        /// denemede karakter kaybolup kenarlarda dev parcalar olarak ciktı.
        ///
        /// CopyFromOther + sourceAvatar: butun klipler modelin iskeletiyle konusur.
        /// </summary>
        private static void AvatarPaylas()
        {
            string modelYolu = Klasor + "/karakter.fbx";
            var modelIce = AssetImporter.GetAtPath(modelYolu) as ModelImporter;
            if (modelIce == null)
            {
                Debug.LogWarning("[Amorland] karakter.fbx bulunamadi: " + modelYolu);
                return;
            }

            // ⚠ Kosul "zaten Generic mi" DEGIL "avatari var mi" olmali. Postprocessor
            // animationType'i Generic yapiyor ama avatarSetup'a dokunmuyor; ilk kosuda
            // model Generic oldugu icin bu blok atlandi ve avatar hic uretilmedi.
            if (modelIce.animationType != ModelImporterAnimationType.Generic
                || modelIce.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                modelIce.animationType = ModelImporterAnimationType.Generic;
                modelIce.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modelIce.SaveAndReimport();
            }

            Avatar avatar = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(modelYolu))
            {
                if (o is Avatar a) { avatar = a; break; }
            }
            if (avatar == null)
            {
                Debug.LogWarning("[Amorland] Model avatari uretilemedi; animasyon baglanmayacak.");
                return;
            }

            foreach (string ad in new[] { "anim_idle", "anim_run", "anim_jump" })
            {
                string yol = Klasor + "/" + ad + ".fbx";
                if (!File.Exists(yol)) continue;
                var mi = AssetImporter.GetAtPath(yol) as ModelImporter;
                if (mi == null) continue;

                mi.animationType = ModelImporterAnimationType.Generic;
                mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                mi.sourceAvatar = avatar;
                mi.SaveAndReimport();
            }
            Debug.Log("[Amorland] Animasyonlar modelin avatarina baglandi.");
        }

        private static AnimationClip Klip(string ad)
        {
            string yol = Klasor + "/" + ad + ".fbx";
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(yol))
            {
                if (o is AnimationClip k && !k.name.StartsWith("__preview")) return k;
            }
            return null;
        }

        private static void DonguYap(string ad)
        {
            string yol = Klasor + "/" + ad + ".fbx";
            if (!File.Exists(yol)) return;
            var mi = AssetImporter.GetAtPath(yol) as ModelImporter;
            if (mi == null) return;

            ModelImporterClipAnimation[] klipler = mi.defaultClipAnimations;
            if (klipler == null || klipler.Length == 0) return;
            for (int i = 0; i < klipler.Length; i++) klipler[i].loopTime = true;
            mi.clipAnimations = klipler;
            mi.SaveAndReimport();
        }
    }
}
