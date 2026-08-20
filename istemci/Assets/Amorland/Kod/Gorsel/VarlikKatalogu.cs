using System.Collections.Generic;
using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Oyun kavramlarini hazir 3B modellere baglayan tek yer.
    ///
    /// Varliklar Kenney (kenney.nl) CC0 kitleridir — spec B.22 kaynak stratejisi 2.
    /// Lisans: <c>Resources/varlik/KENNEY-LICENSE.txt</c>. CC0 atif gerektirmez;
    /// yine de kaynak belirtmek dogru olandir.
    ///
    /// ⚠ Oyun kodu ASLA ham yol yazmaz. "tree_default" gecen tek yer burasi; boylece
    /// bir model degistiginde tek dosya degisir ve eksik model tek yerde yakalanir.
    ///
    /// ⚠ Model bulunamazsa null doner ve cagiran taraf prosedurel yedege duser.
    /// Varlik klasoru olmadan da oyun ACILIR — sadece daha ilkel gorunur.
    /// </summary>
    public static class VarlikKatalogu
    {
        private const string Kok = "varlik/";

        private static readonly Dictionary<string, GameObject> _onbellek =
            new Dictionary<string, GameObject>();
        private static readonly HashSet<string> _uyarilan = new HashSet<string>();

        // ------------------------------------------------------------------ temel

        /// <summary>Modeli yukler. Yoksa null — bir kez uyarir, her karede degil.</summary>
        public static GameObject Model(string yol)
        {
            if (string.IsNullOrEmpty(yol)) return null;
            if (_onbellek.TryGetValue(yol, out GameObject mevcut) && mevcut != null) return mevcut;

            var go = Resources.Load<GameObject>(Kok + yol);
            if (go == null)
            {
                if (_uyarilan.Add(yol))
                {
                    Debug.LogWarning("[Amorland] Model yok: " + Kok + yol
                                     + " — prosedurel yedege dusuluyor.");
                }
                return null;
            }
            _onbellek[yol] = go;
            return go;
        }

        /// <summary>Varlik seti diskte var mi? Yoksa oyun eski prosedurel gorunume duser.</summary>
        public static bool SetVar => Model("doga/tree_default") != null;

        /// <summary>
        /// Modeli sahneye koyar. Y ekseninde rastgele donus verilebilir — ayni modelin
        /// yan yana kopyalari aynı acidan bakinca "kopyala yapistir" gibi durur.
        /// </summary>
        public static GameObject Kur(
            string yol, Transform ebeveyn, Vector3 konum, float olcek = 1f,
            float yDonus = 0f, string ad = null)
        {
            GameObject kaynak = Model(yol);
            if (kaynak == null) return null;

            var go = Object.Instantiate(kaynak, ebeveyn);
            go.name = ad ?? kaynak.name;
            go.transform.localPosition = konum;
            go.transform.localRotation = Quaternion.Euler(0f, yDonus, 0f);
            if (!Mathf.Approximately(olcek, 1f)) go.transform.localScale = Vector3.one * olcek;
            return go;
        }

        // ------------------------------------------------------------------ doga

        private static readonly string[] _agaclar =
        {
            "doga/tree_default", "doga/tree_oak", "doga/tree_blocks",
            "doga/tree_fat", "doga/tree_cone", "doga/tree_default_dark",
        };

        private static readonly string[] _cam = { "doga/tree_pineDefaultA", "doga/tree_pineTallA" };

        private static readonly string[] _kayalar =
        {
            "doga/rock_largeA", "doga/rock_largeB", "doga/rock_smallA",
            "doga/rock_smallB", "doga/rock_smallC", "doga/rock_tallA",
        };

        private static readonly string[] _otlar =
        {
            "doga/grass", "doga/grass_large", "doga/grass_leafs",
            "doga/flower_redA", "doga/flower_yellowA", "doga/flower_purpleA",
        };

        private static readonly string[] _calilar =
        {
            "doga/plant_bush", "doga/plant_bushDetailed", "doga/plant_bushLarge",
        };

        public static string Agac(int i) => _agaclar[Mathf.Abs(i) % _agaclar.Length];
        public static string Cam(int i) => _cam[Mathf.Abs(i) % _cam.Length];
        public static string Kaya(int i) => _kayalar[Mathf.Abs(i) % _kayalar.Length];
        public static string Ot(int i) => _otlar[Mathf.Abs(i) % _otlar.Length];
        public static string Cali(int i) => _calilar[Mathf.Abs(i) % _calilar.Length];

        public const string CimZemin = "doga/ground_grass";
        public const string YolDuz = "doga/ground_pathStraight";
        public const string YolDonus = "doga/ground_pathBend";
        public const string UcurumBlok = "doga/cliff_block_rock";
        public const string UcurumUst = "doga/cliff_top_rock";
        public const string Kopru = "doga/bridge_stone";

        // ------------------------------------------------------------------ tarla

        public const string ToprakKaro = "doga/crops_dirtSingle";
        public const string ToprakSira = "doga/crops_dirtRow";
        public const string ToprakSiraUc = "doga/crops_dirtRowEnd";

        public const string CitDuz = "doga/fence_simple";
        public const string CitOrta = "doga/fence_simpleCenter";
        public const string CitKose = "doga/fence_corner";
        public const string CitKapi = "doga/fence_gate";

        /// <summary>
        /// Urunun buyume asamasi modeli.
        ///
        /// ⚠ Kenney kiti her urun icin 4 asama TASIMAZ: misir 4 (A-D), bugday 2 (A-B),
        /// digerleri yok. Bu yuzden erken asamalar ORTAK filiz modelini kullanir
        /// (crops_leafsStage*), gec asamalar urune ozel modele gecer. Sonuc: her urun
        /// dort asamada da bir sey gosterir ve olgunlukta kendi silueti okunur.
        /// </summary>
        public static string Urun(string urunId, int asama, int asamaSayisi)
        {
            int n = Mathf.Max(2, asamaSayisi);
            float oran = Mathf.Clamp01(asama / (float)(n - 1));

            if (urunId == "misir")
            {
                string[] m = { "doga/crops_cornStageA", "doga/crops_cornStageB",
                               "doga/crops_cornStageC", "doga/crops_cornStageD" };
                return m[Mathf.Clamp(Mathf.RoundToInt(oran * 3f), 0, 3)];
            }

            // Erken: ortak filiz. Gec: urune ozel (bugday) ya da yaprak kutlesi.
            if (oran < 0.34f) return "doga/crops_leafsStageA";
            if (oran < 0.67f) return "doga/crops_leafsStageB";

            if (urunId == "bugday") return oran < 0.99f ? "doga/crops_wheatStageA" : "doga/crops_wheatStageB";
            if (urunId == "kabak" || urunId == "karpuz") return "doga/crops_bambooStageB";
            return oran < 0.99f ? "doga/crops_wheatStageA" : "doga/crops_wheatStageB";
        }

        // ------------------------------------------------------------------ yapi

        public const string Karakter = "karakter/karakter";
        public const string KarakterAnimator = "karakter/karakter-animator";

        /// <summary>
        /// Karakteri Animator'uyle birlikte kurar.
        ///
        /// ⚠ Animator olmadan rigli FBX BIND POZUNDA kalir — kollar iki yana acik,
        /// "T-pozu". Denetleyici <c>Amorland/Karakter Animasyonunu Kur</c> ile uretilir.
        /// </summary>
        public static GameObject KarakterKur(Transform ebeveyn, Vector3 konum, out Animator animator)
        {
            animator = null;
            GameObject go = Kur(Karakter, ebeveyn, konum, 1f, 0f, "Govde");
            if (go == null) return null;

            animator = go.GetComponent<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>();

            var dc = Resources.Load<RuntimeAnimatorController>(Kok + KarakterAnimator);
            if (dc != null) animator.runtimeAnimatorController = dc;
            else Debug.LogWarning("[Amorland] Karakter animator denetleyicisi yok — T-pozu gorunur.");

            animator.applyRootMotion = false;
            return go;
        }

        /// <summary>Depo icin sandik/varil: ortak sandigin fiziksel karsiligi (B.10).</summary>
        private static readonly string[] _sandiklar =
        {
            "depo/box-large", "depo/box", "depo/barrel", "depo/crate",
        };

        public static string Sandik(int i) => _sandiklar[Mathf.Abs(i) % _sandiklar.Length];

        // Ev, fantasy-town-kit parcalarindan KODLA kurulur: kitte tam bina yok.
        public const string Duvar = "yapi/wall";
        public const string DuvarKapi = "yapi/wall-door";
        public const string DuvarPencere = "yapi/wall-window";
        public const string Cati = "yapi/roof";
        public const string CatiUc = "yapi/roof-gable-end";
        public const string CatiTepe = "yapi/roof-gable-top";
        public const string Baca = "yapi/chimney";

        public static void Unut()
        {
            _onbellek.Clear();
            _uyarilan.Clear();
        }
    }
}
