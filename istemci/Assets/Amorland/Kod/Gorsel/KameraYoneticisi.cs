using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Ortografik takip kamerasi (B.5).
    ///
    /// ⚠ <b>Yaw KILITLI (0°).</b> Bu bir tercih degil, asset butcesinin en buyuk kaldiraci:
    /// modellerin arkasi hic gorunmez, poligon ve doku yariya iner. Kullaniciya dondurme
    /// jesti verilirse bu kazanc aninda kaybolur.
    ///
    /// Olu bolge kutusu: karakter 3x2 karoluk kutunun icinde kaldigi surece kamera
    /// KIMILDAMAZ. Surekli takip eden kamera mobilde bas dondurur.
    /// </summary>
    public class KameraYoneticisi : MonoBehaviour
    {
        public Transform Hedef;
        public float Pitch = 50f;
        public float YumusatmaSn = 0.15f;
        public Vector2 OluBolge = new Vector2(3f, 2f);
        public float ZoomYakin = 5f;
        public float ZoomUzak = 8f;

        private Camera _kamera;
        private Vector3 _merkez;
        private Vector3 _hiz;
        private bool _uzakMi;

        private const float Mesafe = 20f;

        public void Kur(Camera kamera, Transform hedef)
        {
            _kamera = kamera;
            Hedef = hedef;
            _kamera.orthographic = true;
            _kamera.orthographicSize = ZoomYakin;
            _kamera.nearClipPlane = 0.1f;
            _kamera.farClipPlane = 100f;
            _kamera.clearFlags = CameraClearFlags.SolidColor;
            _kamera.backgroundColor = Palet.Gokyuzu;
            if (hedef != null) _merkez = hedef.position;
            Yerlestir(anlik: true);
        }

        /// <summary>Cimdik veya buton: iki kademe. SERBEST ZOOM YOK (B.5).</summary>
        public void ZoomDegistir()
        {
            _uzakMi = !_uzakMi;
        }

        public bool UzakMi => _uzakMi;

        private void LateUpdate()
        {
            if (_kamera == null || Hedef == null) return;

            // Olu bolge: hedef kutunun disina cikarsa merkez o kadar kayar.
            Vector3 h = Hedef.position;
            float dx = h.x - _merkez.x;
            float dz = h.z - _merkez.z;
            float yariX = OluBolge.x / 2f;
            float yariZ = OluBolge.y / 2f;

            if (dx > yariX) _merkez.x += dx - yariX;
            else if (dx < -yariX) _merkez.x += dx + yariX;
            if (dz > yariZ) _merkez.z += dz - yariZ;
            else if (dz < -yariZ) _merkez.z += dz + yariZ;
            _merkez.y = 0f;

            Yerlestir(anlik: false);
        }

        private void Yerlestir(bool anlik)
        {
            float radyan = Pitch * Mathf.Deg2Rad;
            Vector3 yon = new Vector3(0f, Mathf.Sin(radyan), -Mathf.Cos(radyan));
            Vector3 hedefKonum = _merkez + yon * Mesafe;

            if (anlik)
            {
                _kamera.transform.position = hedefKonum;
            }
            else
            {
                // Kritik sonumlemeye yakin basit yumusatma; SmoothDamp'in kare bagimsizligi
                // 30 ve 60 FPS'te ayni hissi verir.
                float t = 1f - Mathf.Pow(0.001f, Time.deltaTime / Mathf.Max(0.01f, YumusatmaSn));
                _kamera.transform.position = Vector3.Lerp(_kamera.transform.position, hedefKonum, t);
            }

            // Yaw her karede sifirlanir: hicbir kod yolu kamerayi donduremesin.
            _kamera.transform.rotation = Quaternion.Euler(Pitch, 0f, 0f);

            float hedefBoyut = _uzakMi ? ZoomUzak : ZoomYakin;
            _kamera.orthographicSize =
                anlik
                    ? hedefBoyut
                    : Mathf.Lerp(_kamera.orthographicSize, hedefBoyut, Time.deltaTime * 6f);
        }

        /// <summary>Ekran noktasini zemin duzlemine (y = 0) yansitir.</summary>
        public bool ZemineYansit(Vector2 ekranNoktasi, out Vector3 dunyaNoktasi)
        {
            dunyaNoktasi = Vector3.zero;
            if (_kamera == null) return false;
            Ray isin = _kamera.ScreenPointToRay(new Vector3(ekranNoktasi.x, ekranNoktasi.y, 0f));
            var zemin = new Plane(Vector3.up, Vector3.zero);
            if (!zemin.Raycast(isin, out float mesafe)) return false;
            dunyaNoktasi = isin.GetPoint(mesafe);
            return true;
        }

        /// <summary>Dunya noktasindan karo koordinatina. 1 karo = 1 Unity birimi (B.5).</summary>
        public static Vector2Int KaroyaCevir(Vector3 dunya) =>
            new Vector2Int(Mathf.RoundToInt(dunya.x), Mathf.RoundToInt(dunya.z));

        public static Vector3 KarodanDunyaya(int x, int y) => new Vector3(x, 0f, y);
    }
}
