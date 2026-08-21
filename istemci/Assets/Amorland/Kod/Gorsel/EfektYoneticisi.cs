using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Arayuz;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Tek seferlik oynanis efektleri: ekim pufu, su sicramasi, hasat ziplamasi,
    /// ucan yazi. "Dokundum ama bir sey olmadi" hissinin panzehiri (B.22/B.23
    /// ailesi: her eylemin gorsel karsiligi olur; ses + haptik zaten var, bu
    /// sinif GOZ icin olanini tamamlar).
    ///
    /// ⚠ Parcacik sistemi KULLANILMAZ: URP parcacik materyali + doku kurulumu
    /// kod-kurulumlu projede gereksiz agirlik. Iki-uc sprite'in coroutine ile
    /// ucusmasi ayni hissi bedavaya verir ve stil (poncik, keskin) korunur.
    ///
    /// Ucan yazi HUD kanvasina cizilir (dunya konumundan ekrana yansitilir):
    /// dunya-uzayi metin daha once "dev bulanik harfler" dersini yasatmisti.
    /// </summary>
    public class EfektYoneticisi : MonoBehaviour
    {
        private Camera _kamera;
        private Transform _hudKok;

        private static Sprite _nokta;

        public void Kur(Camera kamera, Canvas hudKanvas)
        {
            _kamera = kamera;
            var kok = new GameObject("UcanYazilar");
            kok.transform.SetParent(hudKanvas.transform, false);
            var rt = kok.AddComponent<RectTransform>();
            UiKit.Doldur(rt);
            _hudKok = kok.transform;
        }

        // ------------------------------------------------------------------ efektler

        /// <summary>Ekim: karodan sacilan uc kahve toprak zerresi.</summary>
        public void ToprakPufu(Vector2 dunya)
        {
            Sacilim(dunya, new Color(0.62f, 0.45f, 0.28f), 3, 0.35f, 0.9f);
        }

        /// <summary>Sulama: yukari sicrayip dusen uc mavi damla.</summary>
        public void SuSicramasi(Vector2 dunya)
        {
            Sacilim(dunya, new Color(0.45f, 0.76f, 0.86f), 3, 0.45f, 1.6f);
        }

        /// <summary>Hasat/toplama: urun ikonu ziplar, buyur ve sonup kaybolur.</summary>
        public void UrunZiplamasi(Vector2 dunya, Sprite urun)
        {
            if (urun == null) return;
            var go = new GameObject("HasatEfekti");
            go.transform.position = new Vector3(dunya.x, dunya.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = urun;
            sr.sortingOrder = 5;
            go.transform.localScale = Vector3.one * 0.7f;
            StartCoroutine(ZiplaVeSon(go, sr));
        }

        /// <summary>Dunya konumundan yukari suzulen konturlu HUD yazisi ("+1", "+15 XP").</summary>
        public void UcanYazi(Vector2 dunya, string metin, Color renk)
        {
            if (_kamera == null || _hudKok == null) return;
            var yazi = UiKit.KonturluYazi(
                _hudKok, "UcanYazi", metin, UiKit.YaziBaslik, renk,
                TextAnchor.MiddleCenter, true, 1.6f);
            var rt = yazi.rectTransform;
            rt.sizeDelta = new Vector2(220f, 34f);
            rt.position = _kamera.WorldToScreenPoint(new Vector3(dunya.x, dunya.y + 0.6f, 0f));
            StartCoroutine(YaziyiUcur(yazi));
        }

        // ------------------------------------------------------------------ ic

        private void Sacilim(Vector2 merkez, Color renk, int adet, float sure, float hiz)
        {
            for (int i = 0; i < adet; i++)
            {
                var go = new GameObject("Zerre");
                go.transform.position = new Vector3(merkez.x, merkez.y + 0.15f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = NoktaSprite();
                sr.color = renk;
                sr.sortingOrder = 5;
                float aci = Mathf.PI * (0.25f + 0.5f * i / Mathf.Max(1, adet - 1));
                var v = new Vector2(Mathf.Cos(aci), Mathf.Sin(aci)) * hiz;
                StartCoroutine(ZerreUcur(go, sr, v, sure));
            }
        }

        private IEnumerator ZerreUcur(GameObject go, SpriteRenderer sr, Vector2 hiz, float sure)
        {
            float t = 0f;
            Vector3 baslangic = go.transform.position;
            Color c = sr.color;
            while (t < sure)
            {
                t += Time.deltaTime;
                float o = t / sure;
                // Basit balistik: yukari firlar, yercekimiyle duser.
                go.transform.position = baslangic
                    + new Vector3(hiz.x * t, hiz.y * t - 3.5f * t * t, 0f);
                c.a = 1f - o * o;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        private IEnumerator ZiplaVeSon(GameObject go, SpriteRenderer sr)
        {
            const float Sure = 0.55f;
            float t = 0f;
            Vector3 baslangic = go.transform.position;
            Color c = sr.color;
            while (t < Sure)
            {
                t += Time.deltaTime;
                float o = t / Sure;
                go.transform.position = baslangic + new Vector3(0f, 1.1f * o, 0f);
                go.transform.localScale = Vector3.one * (0.7f + 0.35f * o);
                c.a = 1f - o * o;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        private IEnumerator YaziyiUcur(Text yazi)
        {
            const float Sure = 0.9f;
            float t = 0f;
            var rt = yazi.rectTransform;
            Vector3 baslangic = rt.position;
            Color c = yazi.color;
            while (t < Sure && yazi != null)
            {
                t += Time.deltaTime;
                float o = t / Sure;
                rt.position = baslangic + new Vector3(0f, 46f * o, 0f);
                c.a = 1f - o * o;
                yazi.color = c;
                yield return null;
            }
            if (yazi != null) Destroy(yazi.gameObject);
        }

        private static Sprite NoktaSprite()
        {
            if (_nokta != null) return _nokta;
            const int B = 8;
            var doku = new Texture2D(B, B, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < B; y++)
            {
                for (int x = 0; x < B; x++)
                {
                    float m = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f), new Vector2(B / 2f, B / 2f)) / (B / 2f);
                    doku.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1.1f - m * 1.4f)));
                }
            }
            doku.Apply();
            _nokta = Sprite.Create(doku, new Rect(0, 0, B, B), new Vector2(0.5f, 0.5f), B * 5f);
            return _nokta;
        }
    }
}
