using System.Collections.Generic;
using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Cizim sprite'larini yukler ve dunyaya yerlestirir.
    ///
    /// B.22 (20.08.2026 revizyonu): sanat yonu "elle cizilmis 2B illustrasyon sprite,
    /// ortografik 2.5D yerlesim" oldu. Duz renkli prosedurel mesh'ler artik yalnizca
    /// ZEMIN icin kullanilir; ayakta duran her sey sprite'tir.
    ///
    /// Sprite'lar <c>araclar/sprite-uretici/uret.py</c> tarafindan uretilir ve
    /// <c>Resources/sprite/</c> altina yazilir. Editor kurulumu GEREKMEZ (mimari
    /// kural: her sey kodla kurulur) — texture'lar calisma aninda Sprite'a cevrilir.
    ///
    /// ⚠ Kamera yaw KILITLI ve pitch SABIT (B.5). Bu yuzden bilboard donusu her karede
    /// hesaplanmaz, nesne kurulurken BIR KEZ verilir. Her karede bakan bir bilboard
    /// bu oyunda bosa is olurdu.
    /// </summary>
    public static class SpriteDeposu
    {
        /// <summary>Kac piksel bir dunya birimi. Karakter 1.6 birim (B.5) -> ~112 px.</summary>
        public const float PikselBirim = 64f;

        private const string Klasor = "sprite/";

        private static readonly Dictionary<string, Sprite> _onbellek = new Dictionary<string, Sprite>();
        private static Material _materyal;
        private static Kayit _kayit;

        [System.Serializable]
        private class SpriteKaydi
        {
            public int en;
            public int boy;
            public float[] pivot;
        }

        [System.Serializable]
        private class Kayit
        {
            public int karoEn;
            public int karoBoy;
            public int karoKalinlik;
        }

        /// <summary>Sprite'lari cizen materyal. Saydam, isiksiz, kose rengi carpani beyaz.</summary>
        public static Material Materyal
        {
            get
            {
                if (_materyal != null) return _materyal;
                Shader s = Shader.Find("Sprites/Default");
                if (s == null) s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (s == null) s = Shader.Find("Unlit/Transparent");
                _materyal = new Material(s) { name = "amorland_sprite" };
                return _materyal;
            }
        }

        /// <summary>
        /// Adiyla sprite dondurur. Bulunamazsa null — cagiran yer tutucuya duser.
        /// Pivot alt-ortadadir: nesne zeminde DURUR, ortasindan asilmaz.
        /// </summary>
        public static Sprite Al(string ad, float pivotY = 0.06f)
        {
            if (_onbellek.TryGetValue(ad, out Sprite mevcut) && mevcut != null) return mevcut;

            var tex = Resources.Load<Texture2D>(Klasor + ad);
            if (tex == null)
            {
                Debug.LogWarning("[Amorland] Sprite bulunamadi: " + ad
                                 + " — once 'python araclar/sprite-uretici/uret.py' calistir.");
                return null;
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var sprite = Sprite.Create(
                tex, new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, pivotY), PikselBirim, 0, SpriteMeshType.FullRect);
            sprite.name = ad;
            _onbellek[ad] = sprite;
            return sprite;
        }

        /// <summary>
        /// Dunyaya dik duran bir sprite koyar (bilboard).
        ///
        /// Pitch, kameranin egimidir; sprite ayni aciyla yatirilir ki ekranda DIK
        /// gorunsun. Yatirilmazsa yere serilmis kagit gibi durur.
        /// </summary>
        public static GameObject Nesne(
            string ad, Transform ebeveyn, Vector3 konum, float pitch, float pivotY = 0.06f,
            Color? renk = null, float olcek = 1f)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(ebeveyn, false);
            go.transform.localPosition = konum;
            go.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            if (!Mathf.Approximately(olcek, 1f)) go.transform.localScale = Vector3.one * olcek;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Al(ad, pivotY);
            sr.sharedMaterial = Materyal;
            if (renk.HasValue) sr.color = renk.Value;
            return go;
        }

        /// <summary>
        /// Zemine YATIK sprite (golge, vurgu halkasi). Bilboard degil: yere serilir.
        /// </summary>
        public static GameObject Yatik(
            string ad, Transform ebeveyn, Vector3 konum, Color? renk = null, float olcek = 1f)
        {
            var go = new GameObject(ad);
            go.transform.SetParent(ebeveyn, false);
            go.transform.localPosition = konum;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (!Mathf.Approximately(olcek, 1f)) go.transform.localScale = Vector3.one * olcek;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Al(ad, 0.5f);
            sr.sharedMaterial = Materyal;
            if (renk.HasValue) sr.color = renk.Value;
            return go;
        }

        /// <summary>Play modu bitiminde onbellegi bosaltir (yok edilmis texture kullanmamak icin).</summary>
        public static void Unut()
        {
            _onbellek.Clear();
            _materyal = null;
            _kayit = null;
        }

        /// <summary>Sprite seti diskte var mi? Yoksa oyun eski mesh gorunumune duser.</summary>
        public static bool SetVar => Resources.Load<Texture2D>(Klasor + "karo_cim") != null;
    }
}
