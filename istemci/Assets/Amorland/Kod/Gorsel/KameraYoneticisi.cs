using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Ortografik 2.5D takip kamerasi (B.5).
    ///
    /// ⚠ Dunya artik vadi duzleminde (XY): kamera -Z'den bakar, donmez.
    /// Eski XZ/pitch kamerasinin yerine gecti; <see cref="Pitch"/> alani eski
    /// cagiranlar (denge verisi atamasi) kirilmasin diye durur ama 2.5D'de
    /// egim YOKTUR — derinlik Y-sort'tan gelir, kameradan degil.
    ///
    /// Olu bolge kutusu korunur: karakter kutunun icinde kaldigi surece kamera
    /// KIMILDAMAZ. Surekli takip eden kamera mobilde bas dondurur.
    /// </summary>
    public class KameraYoneticisi : MonoBehaviour
    {
        public Transform Hedef;
        /// <summary>2.5D'de kullanilmaz; denge verisi uyumu icin durur.</summary>
        public float Pitch = 50f;
        public float YumusatmaSn = 0.15f;
        public Vector2 OluBolge = new Vector2(3f, 2f);
        public float ZoomYakin = 5f;
        public float ZoomUzak = 8f;

        private Camera _kamera;
        private Vector2 _merkez;
        private bool _uzakMi;

        private const float Uzaklik = 10f;

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
            // 2.5D Y-sort: bireysel karolar ve karakterler alt kenara gore siralanir.
            _kamera.transparencySortMode = TransparencySortMode.CustomAxis;
            _kamera.transparencySortAxis = Vector3.up;
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
            float dy = h.y - _merkez.y;
            float yariX = OluBolge.x / 2f;
            float yariY = OluBolge.y / 2f;

            if (dx > yariX) _merkez.x += dx - yariX;
            else if (dx < -yariX) _merkez.x += dx + yariX;
            if (dy > yariY) _merkez.y += dy - yariY;
            else if (dy < -yariY) _merkez.y += dy + yariY;

            Yerlestir(anlik: false);
        }

        private void Yerlestir(bool anlik)
        {
            Vector3 hedefKonum = new Vector3(_merkez.x, _merkez.y, -Uzaklik);

            if (anlik)
            {
                _kamera.transform.position = hedefKonum;
            }
            else
            {
                // Kritik sonumlemeye yakin basit yumusatma; kare bagimsiz.
                float t = 1f - Mathf.Pow(0.001f, Time.deltaTime / Mathf.Max(0.01f, YumusatmaSn));
                _kamera.transform.position =
                    Vector3.Lerp(_kamera.transform.position, hedefKonum, t);
            }

            // Kamera hicbir kod yolunda donmez.
            _kamera.transform.rotation = Quaternion.identity;

            float hedefBoyut = _uzakMi ? ZoomUzak : ZoomYakin;
            _kamera.orthographicSize =
                anlik
                    ? hedefBoyut
                    : Mathf.Lerp(_kamera.orthographicSize, hedefBoyut, Time.deltaTime * 6f);
        }

        /// <summary>Ekran noktasini oyun duzlemine (z = 0) yansitir.</summary>
        public bool ZemineYansit(Vector2 ekranNoktasi, out Vector3 dunyaNoktasi)
        {
            dunyaNoktasi = Vector3.zero;
            if (_kamera == null) return false;
            Vector3 d = _kamera.ScreenToWorldPoint(
                new Vector3(ekranNoktasi.x, ekranNoktasi.y, Uzaklik));
            dunyaNoktasi = new Vector3(d.x, d.y, 0f);
            return true;
        }

        /// <summary>Dunya noktasindan karo koordinatina. 1 karo = 1 Unity birimi (B.5).</summary>
        public static Vector2Int KaroyaCevir(Vector3 dunya) =>
            new Vector2Int(Mathf.RoundToInt(dunya.x), Mathf.RoundToInt(dunya.y));

        public static Vector3 KarodanDunyaya(int x, int y) => new Vector3(x, y, 0f);
    }
}
