using System.Collections.Generic;
using UnityEngine;

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
        public static GameObject Nesne(string ad, Mesh mesh, Transform ebeveyn, Vector3 konum)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(ebeveyn, false);
            go.transform.localPosition = konum;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = KoseRenkli;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }
    }
}
