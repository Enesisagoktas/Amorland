using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Konumu DISARIDAN surulen sprite karakterin adim animasyonu (partner, NPC).
    ///
    /// KarakterKontrolcusu kendi yolunu yurur ve karesini kendisi degistirir;
    /// partner ise sunucudan 5 Hz konumla gelir ve Oyun onu Lerp ile tasir.
    /// Bu bilesen yalnizca konum degisimini OLCER: kipirdiyorsa adim karelerini
    /// oynatir, durunca durus karesine doner. Boylece ayni gorsel dil, hareketi
    /// kimin surdugunden bagimsiz calisir.
    /// </summary>
    public class SpriteYurutucu : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Transform _gorsel;
        private Sprite _dur, _a, _b;
        private Vector3 _sonKonum;
        private float _faz;
        private float _tabanY;
        private bool _yuruyordu;

        public void Kur(SpriteRenderer sr, Transform gorsel, Sprite dur, Sprite a, Sprite b)
        {
            _sr = sr;
            _gorsel = gorsel;
            _dur = dur;
            _a = a;
            _b = b;
            _tabanY = gorsel != null ? gorsel.localPosition.y : 0f;
            _sonKonum = transform.position;
        }

        private void Update()
        {
            if (_sr == null) return;

            Vector3 fark = transform.position - _sonKonum;
            _sonKonum = transform.position;
            // ⚠ Histerezisli esik: 5 Hz lerp sinirinda hiz esik cevresinde salinip
            // dur/yuru karesini titretiyordu (denetim). Giris 0.30, cikis 0.18.
            float hiz = Time.deltaTime > 0f ? fark.magnitude / Time.deltaTime : 0f;
            bool yuruyor = _yuruyordu ? hiz > 0.18f : hiz > 0.30f;
            _yuruyordu = yuruyor;

            if (!yuruyor)
            {
                _faz = 0f;
                if (_dur != null) _sr.sprite = _dur;
                if (_gorsel != null)
                {
                    // Yumusak oturus: tek karede yere isinlanma poncik dili bozar.
                    _gorsel.localPosition = Vector3.MoveTowards(
                        _gorsel.localPosition, new Vector3(0f, _tabanY, 0f),
                        0.9f * Time.deltaTime);
                    _gorsel.localRotation = Quaternion.RotateTowards(
                        _gorsel.localRotation, Quaternion.identity, 80f * Time.deltaTime);
                }
                return;
            }

            if (Mathf.Abs(fark.x) > 0.001f)
            {
                bool yeniFlip = fark.x < 0f;
                if (_sr.flipX != yeniFlip)
                {
                    // Flip aninda faz PI kayar: adim dongusu karsi bacakla surer.
                    _sr.flipX = yeniFlip;
                    _faz += Mathf.PI;
                }
            }
            _faz += Time.deltaTime * 11f;
            if (_a != null)
            {
                // 4 fazli dongu: A -> dur -> B -> dur (KarakterKontrolcusu ile ayni).
                int p = Mathf.FloorToInt((_faz + Mathf.PI / 4f) / (Mathf.PI / 2f)) % 4;
                _sr.sprite = p == 0 ? _a : p == 2 ? _b : _dur;
            }
            if (_gorsel != null)
            {
                float y = Mathf.Abs(Mathf.Sin(_faz)) * 0.07f;
                _gorsel.localPosition = new Vector3(0f, _tabanY + y, 0f);
                _gorsel.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_faz) * 4f);
            }
        }
    }
}
