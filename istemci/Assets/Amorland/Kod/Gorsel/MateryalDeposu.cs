using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode burada

namespace Amorland.Gorsel
{
    /// <summary>
    /// Materyal uretimi ve paylasimi.
    ///
    /// ⚠ Boru hattindan bagimsiz: URP varsa URP shader'i, yoksa yerlesik shader kullanilir.
    /// Sebep pratik — proje URP ile kurulmus olmayabilir ve <c>Shader.Find</c> null donerse
    /// her sey pembe olur. Bu geri dusus, "acilinca calisiyor" ile "acilinca pembe" arasindaki
    /// fark demek.
    ///
    /// ⚠ Tek materyal + kose rengi (vertex color) kullanilir: her nesneye ayri materyal
    /// vermek batch'lemeyi oldururdu. Renkler mesh'in icindedir (B.22: tek atlas mantigi).
    /// </summary>
    public static class MateryalDeposu
    {
        private static Material _koseRenkli;
        private static Material _ui;
        private static readonly Dictionary<string, Material> _duzRenkler = new Dictionary<string, Material>();

        /// <summary>
        /// Kose rengini kullanan tek materyal. Butun dunya nesneleri bunu paylasir.
        /// </summary>
        public static Material KoseRenkli
        {
            get
            {
                if (_koseRenkli != null) return _koseRenkli;
                Shader s = ShaderBul(
                    "Universal Render Pipeline/Particles/Unlit",
                    "Universal Render Pipeline/Unlit",
                    "Unlit/Color",
                    "Sprites/Default");
                _koseRenkli = new Material(s) { name = "amorland_kose_renkli" };
                if (_koseRenkli.HasProperty("_BaseColor")) _koseRenkli.SetColor("_BaseColor", Color.white);
                _koseRenkli.color = Color.white;
                return _koseRenkli;
            }
        }

        /// <summary>
        /// Kose rengi desteklemeyen shader'a dusersek renkler kaybolur. O yuzden
        /// duz renkli materyaller de tutulur; mesh basina birer tane, ad ile onbellenir.
        /// </summary>
        public static Material DuzRenk(string ad, Color renk)
        {
            if (_duzRenkler.TryGetValue(ad, out Material m) && m != null) return m;
            Shader s = ShaderBul(
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Sprites/Default");
            m = new Material(s) { name = "amorland_" + ad };
            m.color = renk;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", renk);
            _duzRenkler[ad] = m;
            return m;
        }

        private static Material _saydam;

        /// <summary>
        /// Saydamlik destekleyen kose renkli materyal. Sahte golge ve solgun vurgular icin.
        ///
        /// ⚠ Ayri bir materyal SART: <see cref="KoseRenkli"/> OPAKTIR ve alfayi yok sayar.
        /// Sahte golge %38 siyah olarak yazilmisti ama opak materyalde KATI SIYAH bir leke
        /// olarak ciziliyordu — karakterin altinda kocaman bir delik gibi duruyordu.
        /// Butun dunyayi saydam kuyruga almak ise siralama sorunu ve performans bedeli
        /// getirir; o yuzden yalnizca gerekli nesneler bunu kullanir.
        /// </summary>
        public static Material Saydam
        {
            get
            {
                if (_saydam != null) return _saydam;
                Shader s = ShaderBul(
                    "Universal Render Pipeline/Particles/Unlit",
                    "Universal Render Pipeline/Unlit",
                    "Sprites/Default");
                _saydam = new Material(s) { name = "amorland_saydam" };
                SaydamYap(_saydam);
                return _saydam;
            }
        }

        /// <summary>
        /// Materyali alfa harmanlamaya gecirir. URP'de bu tek bir bayrak degil:
        /// yuzey tipi, harman modu, derinlik yazimi ve kuyruk birlikte ayarlanmali.
        /// </summary>
        private static void SaydamYap(Material m)
        {
            if (m == null) return;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);   // 1 = Transparent
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);       // 0 = Alpha
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        public static Material Ui
        {
            get
            {
                if (_ui != null) return _ui;
                Shader s = ShaderBul("UI/Default", "Sprites/Default", "Unlit/Color");
                _ui = new Material(s) { name = "amorland_ui" };
                return _ui;
            }
        }

        private static Shader ShaderBul(params string[] adaylar)
        {
            foreach (string ad in adaylar)
            {
                Shader s = Shader.Find(ad);
                if (s != null) return s;
            }
            Debug.LogWarning(
                "[Amorland] Hicbir shader bulunamadi. Materyaller pembe gorunecek. "
                + "Proje URP ile mi kuruldu?");
            return null;
        }

        /// <summary>
        /// Verilen mesh'i cizen bir GameObject kurar.
        /// Gercek zamanli golge YOK (B.2 performans hedefi) — sahte blob golge kullanilir.
        /// </summary>
        public static GameObject Nesne(
            string ad, Mesh mesh, Transform ebeveyn, Vector3 konum, bool saydam = false)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(ebeveyn, false);
            go.transform.localPosition = konum;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = saydam ? Saydam : KoseRenkli;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }
    }
}
