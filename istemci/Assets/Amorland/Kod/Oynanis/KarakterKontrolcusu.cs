using System;
using System.Collections.Generic;
using UnityEngine;
using Amorland.Girdi;
using Amorland.Gorsel;

namespace Amorland.Oynanis
{
    /// <summary>
    /// Karakter hareketi ve yurume animasyonu.
    ///
    /// Yurume hizi 4 karo/saniye (B.6). Hiz denge dosyasindan gelir; burada sabit yok.
    ///
    /// ⚠ Animasyon kilidi IPTAL EDILEBILIR (B.6): oyuncu bir eylemin ortasinda baska
    /// yere dokunursa karakter hemen oraya yurur. "Oyuncu asla kapana kisilmaz" kurali
    /// mobilde en cok hissedilen kaliteyi belirler.
    /// </summary>
    public class KarakterKontrolcusu : MonoBehaviour
    {
        public float KaroBasinaSn = 0.25f;   // 4 karo/sn
        public Izgara Izgara;

        private readonly List<Vector2Int> _yol = new List<Vector2Int>();
        private Vector3 _hedefKonum;
        private bool _yuruyor;
        private float _adimIlerleme;
        private Vector3 _adimBaslangic;

        private Transform _gorsel;
        private SpriteRenderer _sr;
        private float _gorselTabanY;
        private float _sallanmaFazi;

        /// <summary>Yol bitince cagrilir: bekleyen baglam eylemi burada tetiklenir.</summary>
        public event Action<Vector2Int> HedefeVarildi;

        public Vector2Int Karo => new Vector2Int(
            Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

        public bool Yuruyor => _yuruyor;

        public void Kur(Transform gorsel, Izgara izgara, Vector3 baslangic)
        {
            _gorsel = gorsel;
            _sr = gorsel != null ? gorsel.GetComponentInChildren<SpriteRenderer>() : null;
            _gorselTabanY = gorsel != null ? gorsel.localPosition.y : 0f;
            Izgara = izgara;
            transform.position = baslangic;
            _hedefKonum = baslangic;
        }

        /// <summary>
        /// Hedefe yol kurar. Yeni bir hedef eskisini IPTAL EDER — bu bir hata degil,
        /// tasarim: parmagin son dokundugu yer her zaman kazanir.
        /// </summary>
        public void Yuru(Vector2Int hedef)
        {
            _yol.Clear();
            var yol = Izgara != null ? Izgara.Yol(Karo, hedef) : new List<Vector2Int>();
            _yol.AddRange(yol);
            if (_yol.Count == 0)
            {
                _yuruyor = false;
                HedefeVarildi?.Invoke(Karo);
                return;
            }
            SonrakiAdim();
        }

        public void Dur()
        {
            _yol.Clear();
            _yuruyor = false;
        }

        private void SonrakiAdim()
        {
            if (_yol.Count == 0)
            {
                _yuruyor = false;
                HedefeVarildi?.Invoke(Karo);
                return;
            }
            Vector2Int sonraki = _yol[0];
            _yol.RemoveAt(0);
            _adimBaslangic = transform.position;
            _hedefKonum = new Vector3(sonraki.x, sonraki.y, 0f);
            _adimIlerleme = 0f;
            _yuruyor = true;

            // Yon: 2.5D'de karakter donmez, sprite yatay cevrilir.
            Vector3 yon = _hedefKonum - _adimBaslangic;
            if (Mathf.Abs(yon.x) > 0.05f && _sr != null) _sr.flipX = yon.x < 0f;
        }

        private void Update()
        {
            if (!_yuruyor) return;

            // Capraz adim daha uzun: sure mesafeyle olceklenir, yoksa capraz kosmus olur.
            float mesafe = Vector3.Distance(_adimBaslangic, _hedefKonum);
            float adimSuresi = Mathf.Max(0.01f, KaroBasinaSn * Mathf.Max(1f, mesafe));

            _adimIlerleme += Time.deltaTime / adimSuresi;
            if (_adimIlerleme >= 1f)
            {
                transform.position = _hedefKonum;
                SonrakiAdim();
                return;
            }
            transform.position = Vector3.Lerp(_adimBaslangic, _hedefKonum, _adimIlerleme);

            // Yurume zipirtisi: poncik his — minik hoplama + hafif yalpalama.
            if (_gorsel != null)
            {
                _sallanmaFazi += Time.deltaTime * 11f;
                float y = Mathf.Abs(Mathf.Sin(_sallanmaFazi)) * 0.07f;
                _gorsel.localPosition = new Vector3(0f, _gorselTabanY + y, 0f);
                _gorsel.localRotation =
                    Quaternion.Euler(0f, 0f, Mathf.Sin(_sallanmaFazi) * 4f);
            }
        }
    }
}
