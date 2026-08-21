using UnityEngine;
using Amorland.Girdi;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Vadi haritasinda gezinme kamerasi.
    ///
    /// Parmakla surukle, iki parmakla cimdikle; masaustunde WASD/ok tuslari ve fare
    /// tekerlegi. Kamera her zaman harita sinirlarinin ICINDE kalir — kenardan disari
    /// cikip bos ekrana bakmak mumkun degil.
    ///
    /// ⚠ Surukleme YONU ters: parmak saga giderse harita saga kayar, yani kamera SOLA
    /// gider. Haritayi tutup cekiyormus hissi bu; kamerayi parmakla ayni yone
    /// suruklemek her denemede yanlis hissettiriyor.
    ///
    /// ⚠ Sinir hesabi zoom'a BAGLI: uzaklastikca ekrana daha cok karo girer ve kameranin
    /// gidebilecegi alan daralir. Sabit sinir kullanilirsa uzak zoom'da harita disi
    /// gorunur. Harita ekrandan kucukse eksen ortalanir.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class HaritaGezgini : MonoBehaviour
    {
        /// <summary>Ekran yuksekliginin YARISI, dunya biriminde. Kucuk = yakin.</summary>
        public float EnYakin = 6f;
        public float EnUzak = 26f;
        /// <summary>Klavyeyle gezinme hizi: saniyede karo.</summary>
        public float TusHizi = 26f;
        public float TekerlekHizi = 4f;
        /// <summary>Cimdikte 1 piksel = kac birim zoom.</summary>
        public float CimdikHizi = 0.045f;

        /// <summary>
        /// Takip hedefi (vadi karakteri). Doluyken kamera onu izler; surukleme
        /// gecici bakinmadir, parmak kalkinca kamera karaktere geri suzulur.
        /// WASD da karaktere gider — kamera tus kontrolu devre disi kalir.
        /// </summary>
        public Transform Takip;

        private Camera _kamera;
        private float _ortoHedef = -1f;
        private bool _suruklerken;
        private Vector3 _tutulanDunya;   // surukleme basladiginda parmagin altindaki nokta
        private Vector3 _hedef;

        /// <summary>
        /// Kamera TEMBEL cozulur.
        ///
        /// ⚠ Awake yerine burada: Unity, Editor Play modu disinda AddComponent yapilan
        /// bilesenlerde Awake CAGIRMAZ. Editor dogrulama araci sahneyi Play moduna
        /// girmeden kurar; alan Awake ile doldurulsaydi arac NullReference ile patlardi
        /// ve gercek yolu sinayamazdik.
        /// </summary>
        private Camera Kamera
        {
            get
            {
                if (_kamera != null) return _kamera;
                _kamera = GetComponent<Camera>();
                if (_kamera != null)
                {
                    _kamera.orthographic = true;
                    // 2B karo haritasi: kamera tam karsidan bakar.
                    transform.rotation = Quaternion.identity;
                    _hedef = transform.position;
                }
                return _kamera;
            }
        }

        private void Awake()
        {
            _ = Kamera;
        }

        /// <summary>Kamerayi bir noktaya ANINDA oturtur (acilis).</summary>
        public void Odakla(Vector2 dunyaNoktasi, float ortoBoy)
        {
            _ortoHedef = Mathf.Clamp(ortoBoy, EnYakin, EnUzak);
            Kamera.orthographicSize = _ortoHedef;
            _hedef = new Vector3(dunyaNoktasi.x, dunyaNoktasi.y, transform.position.z);
            Sinirla();
            transform.position = _hedef;
        }

        /// <summary>
        /// Kamerayi bir noktaya YUMUSAK tasir (bolgeye dokununca). Konum zaten
        /// yumusatiliyordu; zoom icin ayrica hedef tutulur ve Update yaklastirir.
        /// </summary>
        public void YumusakOdakla(Vector2 dunyaNoktasi, float ortoBoy)
        {
            _ortoHedef = Mathf.Clamp(ortoBoy, EnYakin, EnUzak);
            _hedef = new Vector3(dunyaNoktasi.x, dunyaNoktasi.y, transform.position.z);
        }

        private void Update()
        {
            // Yumusak zoom hedefi: elle zoom yapilinca hedef aninda esitlenir ki
            // kullanicinin tekerlegiyle kavga etmesin.
            if (_ortoHedef > 0f && !Mathf.Approximately(Kamera.orthographicSize, _ortoHedef))
            {
                Kamera.orthographicSize = Mathf.Lerp(Kamera.orthographicSize, _ortoHedef,
                                                     1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            }

            Zoom();
            Surukle();
            if (Takip == null) Tuslar();
            else if (!_suruklerken)
                _hedef = new Vector3(Takip.position.x, Takip.position.y + 0.5f,
                                     transform.position.z);
            Sinirla();

            // ⚠ Yumusatma: karo haritasinda sert takip titrek gorunur. 18 katsayisi
            // takibi gecikmeli hissettirmeden titremeyi aliyor.
            transform.position = Vector3.Lerp(transform.position, _hedef,
                                              1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        }

        private void Zoom()
        {
            float degisim = 0f;

            float cimdik = Girdi.Girdi.CimdikDegisimi();
            if (Mathf.Abs(cimdik) > 0.01f) degisim -= cimdik * CimdikHizi;
            else degisim -= Girdi.Girdi.Tekerlek() * TekerlekHizi * 0.01f;

            if (Mathf.Approximately(degisim, 0f)) return;
            Kamera.orthographicSize = Mathf.Clamp(Kamera.orthographicSize + degisim, EnYakin, EnUzak);
            _ortoHedef = Kamera.orthographicSize;   // elle zoom yumusak hedefi iptal eder
        }

        private void Surukle()
        {
            Girdi.Girdi.Temas[] t = Girdi.Girdi.Temaslar(out int adet);

            // Iki parmak = cimdik; surukleme devreye girmemeli.
            if (adet != 1) { _suruklerken = false; return; }

            Girdi.Girdi.Temas p = t[0];
            if (Girdi.Girdi.UiUzerinde(p.Konum)) { _suruklerken = false; return; }

            if (p.Basladi)
            {
                _tutulanDunya = Kamera.ScreenToWorldPoint(p.Konum);
                _suruklerken = true;
                return;
            }
            if (p.Bitti) { _suruklerken = false; return; }
            if (!_suruklerken || !p.Suruyor) return;

            // Parmagin altindaki dunya noktasi AYNI kalsin: fark kadar kamerayi kaydir.
            Vector3 simdi = Kamera.ScreenToWorldPoint(p.Konum);
            Vector3 fark = _tutulanDunya - simdi;
            _hedef += new Vector3(fark.x, fark.y, 0f);
            // Anlik uygula, yoksa yumusatma yuzunden parmak noktadan kayar.
            transform.position = new Vector3(_hedef.x, _hedef.y, transform.position.z);
        }

        private void Tuslar()
        {
            Vector2 y = Girdi.Girdi.Yon();
            if (y == Vector2.zero) return;
            // Uzaklasinca ayni tus daha cok karo gecsin — yoksa uzak zoom'da surunur.
            float olcek = Kamera.orthographicSize / 9f;
            _hedef += (Vector3)(y * TusHizi * olcek * Time.unscaledDeltaTime);
        }

        /// <summary>Kamerayi harita dikdortgeninin icinde tutar.</summary>
        private void Sinirla()
        {
            float yariBoy = Kamera.orthographicSize;
            float yariEn = yariBoy * Kamera.aspect;

            _hedef.x = Kenetle(_hedef.x, yariEn, VadiHaritasi.En);
            _hedef.y = Kenetle(_hedef.y, yariBoy, VadiHaritasi.Boy);
        }

        /// <summary>Harita bu eksende ekrandan kucukse ortala, degilse kenetle.</summary>
        private static float Kenetle(float deger, float yari, int uzunluk)
        {
            if (yari * 2f >= uzunluk) return uzunluk / 2f;
            return Mathf.Clamp(deger, yari, uzunluk - yari);
        }
    }
}
