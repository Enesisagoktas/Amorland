using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Vadide yuruyen karakter.
    ///
    /// Haritayi artik kamera degil KARAKTER gezer: WASD/ok tuslari dogrudan
    /// yurutur, bos zemine dokunmak yuruyus hedefi koyar. Kamera karakteri
    /// takip eder (HaritaGezgini.Takip).
    ///
    /// ⚠ 2.5D duzeni: GameObject'in konumu karakterin AYAK noktasidir; gorsel
    /// (sprite) yarim karo yukarida bir cocuktur. Boylece Y-sort karsilastirmasi
    /// ayaga gore yapilir — karakter bir agacin govde hucresinin ustundeyken
    /// govdenin ARKASINDA, altindayken ONUNDE cizilir.
    ///
    /// ⚠ Carpisma karo bazlidir ve KAYDIRMALIDIR: hedef hucre kapaliysa once
    /// yalniz X, sonra yalniz Y denenir. Bu kucuk numara olmadan karakter
    /// engellere yapisip kaliyor, kose donmek eziyete donusuyordu.
    /// </summary>
    public class VadiKarakteri : MonoBehaviour
    {
        public const float Hiz = 5.5f;

        private VadiHaritasi _harita;
        private Transform _gorsel;
        private SpriteRenderer _sr;
        private Vector2? _hedef;
        private float _bobZamani;
        private Sprite _kareDur, _kareA, _kareB;

        public Vector2 Konum => transform.position;

        public void Kur(VadiHaritasi harita, Sprite sprite, Vector2 baslangic)
        {
            _harita = harita;
            transform.position = new Vector3(baslangic.x, baslangic.y, 0f);

            var g = new GameObject("Gorsel");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            _gorsel = g.transform;
            _sr = g.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            _sr.sortingOrder = 2;   // Nesne katmaniyla ayni — Y-sort karar verir

            var golge = new GameObject("Golge");
            golge.transform.SetParent(transform, false);
            // ⚠ 0.42'de elips (dokunun alt yarisinda) govdenin ARKASINA denk
            // gelip hic gorunmuyordu — "vadide golge yok" bulgusunun koku.
            // +0.20 = elips merkezi ayak cizgisinde.
            golge.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            golge.transform.localScale = new Vector3(0.95f, 0.6f, 1f);
            var gsr = golge.AddComponent<SpriteRenderer>();
            gsr.sprite = Karo64.Al(Sayfa64.Ek, Karo64.Golge);
            gsr.sortingOrder = 1;
        }

        /// <summary>Dokunulan noktaya yuru.</summary>
        public void HedefKoy(Vector2 dunya) => _hedef = dunya;

        /// <summary>Yuruyus kareleri: dur / adim A / adim B.</summary>
        public void YurumeKareleri(Sprite dur, Sprite a, Sprite b)
        {
            _kareDur = dur;
            _kareA = a;
            _kareB = b;
        }

        private void Update()
        {
            if (_harita == null) return;

            Vector2 yon = Girdi.Girdi.Yon();
            if (yon != Vector2.zero)
            {
                _hedef = null;                       // tus, dokunma hedefini iptal eder
            }
            else if (_hedef.HasValue)
            {
                Vector2 fark = _hedef.Value - Konum;
                if (fark.magnitude < 0.15f) { _hedef = null; }
                else yon = fark.normalized;
            }

            if (yon == Vector2.zero) { Dinlen(); return; }

            Vector2 adim = yon * Hiz * Time.deltaTime;
            Vector2 simdiki = Konum;
            Vector2 aday = simdiki + adim;

            if (!Gecer(aday))
            {
                // Eksen kaydirma: once yalniz X, sonra yalniz Y.
                Vector2 sadeceX = simdiki + new Vector2(adim.x, 0f);
                Vector2 sadeceY = simdiki + new Vector2(0f, adim.y);
                if (Gecer(sadeceX)) aday = sadeceX;
                else if (Gecer(sadeceY)) aday = sadeceY;
                else { _hedef = null; Dinlen(); return; }
            }

            transform.position = new Vector3(aday.x, aday.y, 0f);

            // Yuz yonu + yurume zipirtisi (poncik his: minik hoplama)
            if (Mathf.Abs(yon.x) > 0.05f) _sr.flipX = yon.x < 0f;
            _bobZamani += Time.deltaTime * 11f;
            _gorsel.localPosition = new Vector3(0f, 0.5f + Mathf.Abs(Mathf.Sin(_bobZamani)) * 0.07f, 0f);
            if (_kareA != null)
            {
                _sr.sprite = Mathf.FloorToInt(_bobZamani / Mathf.PI) % 2 == 0 ? _kareA : _kareB;
            }
        }

        private void Dinlen()
        {
            _bobZamani = 0f;
            if (_gorsel != null) _gorsel.localPosition = new Vector3(0f, 0.5f, 0f);
            if (_sr != null && _kareDur != null) _sr.sprite = _kareDur;
        }

        private bool Gecer(Vector2 p)
        {
            return _harita.Gecilebilir(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        }
    }
}
