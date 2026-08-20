using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Amorland.Girdi
{
    /// <summary>
    /// Girdi soyutlamasi.
    ///
    /// B.2 yeni Input System'i sart kosuyor; ama proje "Both" modunda kurulabilir ve
    /// bazi kurulumlarda paket bulunmayabilir. Iki yol da derlenir: <c>ENABLE_INPUT_SYSTEM</c>
    /// tanimliysa yeni sistem, degilse eski <c>Input</c> kullanilir. Oyun kodu farki gormez.
    ///
    /// ⚠ Buradaki tek is "parmak nerede, ne yapti" sorusuna cevap vermek. Jest ANLAMI
    /// (dokun-yuru, surukle-boyama, uzun bas) <see cref="Amorland.Oynanis"/> tarafinda.
    /// </summary>
    public static class Girdi
    {
        public struct Temas
        {
            public int Id;
            public Vector2 Konum;
            public bool Basladi;
            public bool Bitti;
            public bool Suruyor;
        }

        private static readonly Temas[] _bos = new Temas[0];
        private static Temas[] _tampon = new Temas[8];

        /// <summary>Bu karedeki aktif temaslar. Fare tek temas olarak gorunur.</summary>
        public static Temas[] Temaslar(out int adet)
        {
            adet = 0;

#if ENABLE_INPUT_SYSTEM
            var ekran = Touchscreen.current;
            if (ekran != null)
            {
                foreach (var t in ekran.touches)
                {
                    if (adet >= _tampon.Length) break;
                    var faz = t.phase.ReadValue();
                    bool basladi = faz == UnityEngine.InputSystem.TouchPhase.Began;
                    bool bitti = faz == UnityEngine.InputSystem.TouchPhase.Ended
                                 || faz == UnityEngine.InputSystem.TouchPhase.Canceled;
                    bool suruyor = faz == UnityEngine.InputSystem.TouchPhase.Moved
                                   || faz == UnityEngine.InputSystem.TouchPhase.Stationary;
                    if (!basladi && !bitti && !suruyor) continue;

                    _tampon[adet++] = new Temas
                    {
                        Id = t.touchId.ReadValue(),
                        Konum = t.position.ReadValue(),
                        Basladi = basladi,
                        Bitti = bitti,
                        Suruyor = suruyor
                    };
                }
                if (adet > 0) return _tampon;
            }

            var fare = Mouse.current;
            if (fare != null)
            {
                bool basladi = fare.leftButton.wasPressedThisFrame;
                bool bitti = fare.leftButton.wasReleasedThisFrame;
                bool suruyor = fare.leftButton.isPressed && !basladi;
                if (basladi || bitti || suruyor)
                {
                    _tampon[adet++] = new Temas
                    {
                        Id = 0,
                        Konum = fare.position.ReadValue(),
                        Basladi = basladi,
                        Bitti = bitti,
                        Suruyor = suruyor
                    };
                }
            }
            return adet > 0 ? _tampon : _bos;
#else
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount && adet < _tampon.Length; i++)
                {
                    Touch t = Input.GetTouch(i);
                    _tampon[adet++] = new Temas
                    {
                        Id = t.fingerId,
                        Konum = t.position,
                        Basladi = t.phase == TouchPhase.Began,
                        Bitti = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled,
                        Suruyor = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary
                    };
                }
                return _tampon;
            }

            bool bas = Input.GetMouseButtonDown(0);
            bool birak = Input.GetMouseButtonUp(0);
            bool basili = Input.GetMouseButton(0) && !bas;
            if (bas || birak || basili)
            {
                _tampon[adet++] = new Temas
                {
                    Id = 0,
                    Konum = Input.mousePosition,
                    Basladi = bas,
                    Bitti = birak,
                    Suruyor = basili
                };
                return _tampon;
            }
            return _bos;
#endif
        }

        /// <summary>Cimdik mesafesi degisimi. Iki parmak yoksa 0.</summary>
        public static float CimdikDegisimi()
        {
            Temas[] temaslar = Temaslar(out int adet);
            if (adet < 2) { _oncekiCimdik = -1f; return 0f; }

            float mesafe = Vector2.Distance(temaslar[0].Konum, temaslar[1].Konum);
            if (_oncekiCimdik < 0f) { _oncekiCimdik = mesafe; return 0f; }
            float fark = mesafe - _oncekiCimdik;
            _oncekiCimdik = mesafe;
            return fark;
        }

        private static float _oncekiCimdik = -1f;

        /// <summary>UI uzerindeyse dunya girdisi islenmemeli.</summary>
        public static bool UiUzerinde(Vector2 ekranNoktasi)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return false;
            return es.IsPointerOverGameObject();
        }
    }
}
