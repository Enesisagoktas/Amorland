using UnityEngine;
using UnityEngine.UI;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Sanal joystick (B.6 ikincil girdi).
    ///
    /// ⚠ <b>Dinamik konumlu</b>: parmagini koydugun yerde belirir. Sabit konumlu joystick
    /// mobilde parmagin nereye basacagini ezberlemeyi gerektirir; ekranin sol ucte biri
    /// icinde nereye basarsan orada olusur.
    ///
    /// Birincil girdi karoya dokunmaktir; joystick yalnizca gezinmek isteyene.
    ///
    /// ⚠ Butun parcalari <see cref="Sekil.Daire"/> ile cizilir. Sprite'siz <c>Image</c>
    /// keskin kare verir; oyun alaninin ortasinda duran kare bir joystick arayuzun
    /// tamamini ucuzlatiyordu.
    ///
    /// ⚠ Renk sozlesmesi (B.22): taban KOYU yari saydam govde + KREM ince halka; topuz
    /// bunun TERSI, KREM dolgu + KOYU kenar. HUD kapsulleriyle ayni fikir: tek tonlu bir
    /// daire tarlanin ya acik ya koyu bolgesinde mutlaka kayboluyordu — iki zit ton her
    /// zeminde birbirini kurtarir. Taban yine de yari saydam kalir: joystick oyunun
    /// UZERINDE durur, altindaki karolari kapatmamali.
    /// </summary>
    public class Joystick : MonoBehaviour
    {
        private RectTransform _kok;
        private RectTransform _taban;
        private RectTransform _sap;
        private Canvas _kanvas;

        private bool _aktif;
        private int _parmakId = -1;
        private Vector2 _merkez;

        private const float Yaricap = 90f;
        private const float OlanBolge = 0.12f;

        /// <summary>Basparmak topuzunun capi. Tabanin yaklasik ucte biri: yon okunur, taban kapanmaz.</summary>
        private const float SapCap = 68f;

        /// <summary>
        /// Taban halkasinin kalinligi. 180 px'lik dairede hala "ince" durur ama cim
        /// dokusunun uzerinde erimeyecek kadar vardir — joystick'in okunurlugunu tek
        /// basina bu hat tasiyor, govde degil.
        /// </summary>
        private const float HalkaKalinlik = 4f;

        /// <summary>Topuzun koyu kenar kalinligi. Krem dolguyu her zeminden ayirir.</summary>
        private const float SapKenar = 5f;

        /// <summary>-1..1 araliginda yon. Sifir ise girdi yok.</summary>
        public Vector2 Yon { get; private set; }

        public bool Aktif => _aktif;

        public void Kur(Canvas kanvas)
        {
            _kanvas = kanvas;
            _kok = UiKit.Panel(kanvas.transform, "Joystick", Color.clear);
            UiKit.Doldur(_kok);
            _kok.gameObject.GetComponent<Image>().raycastTarget = false;

            // Taban ic ice iki daire: alttaki KREM halka, ustteki iceri cekilmis koyu
            // yari saydam govde. Eski hali iki katmani da soluk kremdi ve joystick acik
            // cim uzerinde neredeyse gorunmuyordu; okunurlugu veren sey kapsul degil,
            // etrafindaki acik hat (HUD kapsulleriyle ayni dil).
            //
            // ⚠ Sekil.Halka yuvarlak KOSE uretir ve 180 px'e gerildiginde daire kalmaz;
            // gercek dairesel halka ancak ic ice iki dolu daireyle kuruluyor.
            _taban = Daire(_kok, "Taban", Saydam(Palet.KapsulCerceve, 0.88f), Yaricap * 2f);
            var tabanIc = Daire(_taban, "TabanIc", Saydam(Palet.KapsulZemin, 0.52f), Yaricap * 2f);
            UiKit.Doldur(tabanIc, HalkaKalinlik);

            // ⚠ Sap TABANIN cocugu: Suruklendi anchoredPosition'i tabanin merkezine gore
            // yaziyor. Baska bir ebeveyne alinirsa yon dogru, konum yanlis olur.
            //
            // Topuz tabanin tersi: kahve kenar + krem dolgu. Hareket eden parca ekrandaki
            // en parlak oge olmali ki parmagin altinda kaybolmasin; koyu kenar da onu hem
            // tabanin koyu icinden hem de acik cim tonlarindan ayirir.
            _sap = Daire(_taban, "Sap", Saydam(Palet.UiCerceve, 0.96f), SapCap);
            var sapIc = Daire(_sap, "SapIc", Saydam(Palet.Parsomen, 0.95f), SapCap);
            UiKit.Doldur(sapIc, SapKenar);

            // Ust pah: topuza fiziksel dugme hissi verir — arayuzdeki pahli butonlarla
            // ayni ucuz numara. Daire sprite'i yassi gerilir, yani elips olur.
            var pah = UiKit.Panel(sapIc, "Pah", Palet.UiPah);
            var pahImg = pah.gameObject.GetComponent<Image>();
            pahImg.sprite = Sekil.Daire();
            pahImg.type = Image.Type.Simple;
            // ⚠ UiKit.Panel raycast'i yalnizca TAM saydam zeminlerde kapatir; UiPah
            // saydam degil. Elle kapatilmazsa topuz karoya dokunmayi yutar.
            pahImg.raycastTarget = false;
            UiKit.Sabitle(pah, 0.5f, 1f, 0f, -6f, SapCap * 0.46f, SapCap * 0.20f);

            // Golge yok: taban her dokunusta .position ile tasiniyor, UiKit.GolgeEkle ise
            // golgeyi kardes olarak birakip o anki konumu kopyaliyor — golge yerinde kalirdi.
            // Derinligi bunun yerine halka ile pah tasiyor.

            _taban.gameObject.SetActive(false);
        }

        /// <summary>Ekranin sol ucte biri joystick bolgesidir.</summary>
        public static bool JoystickBolgesi(Vector2 ekranNoktasi) =>
            ekranNoktasi.x < Screen.width / 3f;

        public void Basildi(int parmakId, Vector2 ekranNoktasi)
        {
            if (_aktif) return;
            if (!JoystickBolgesi(ekranNoktasi)) return;

            _aktif = true;
            _parmakId = parmakId;
            _merkez = ekranNoktasi;
            _taban.gameObject.SetActive(true);
            _taban.position = new Vector3(ekranNoktasi.x, ekranNoktasi.y, 0f);
            _sap.anchoredPosition = Vector2.zero;
            Yon = Vector2.zero;
        }

        public void Suruklendi(int parmakId, Vector2 ekranNoktasi)
        {
            if (!_aktif || parmakId != _parmakId) return;

            Vector2 fark = ekranNoktasi - _merkez;
            float uzunluk = fark.magnitude;
            Vector2 sinirli = uzunluk > Yaricap ? fark.normalized * Yaricap : fark;
            _sap.anchoredPosition = sinirli;

            Vector2 ham = sinirli / Yaricap;
            Yon = ham.magnitude < OlanBolge ? Vector2.zero : ham;
        }

        public void Birakildi(int parmakId)
        {
            if (!_aktif || parmakId != _parmakId) return;
            _aktif = false;
            _parmakId = -1;
            Yon = Vector2.zero;
            _taban.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ gorunum

        /// <summary>Dolu daire katmani. Joystick'in her parcasi daire, hicbiri kare degil.</summary>
        private static RectTransform Daire(Transform ebeveyn, string ad, Color renk, float cap)
        {
            var rt = UiKit.Panel(ebeveyn, ad, renk);
            var img = rt.gameObject.GetComponent<Image>();
            img.sprite = Sekil.Daire();
            img.type = Image.Type.Simple;
            // Joystick yalnizca Dokunma'dan beslenir; karoya dokunmayi yutmamali.
            img.raycastTarget = false;
            rt.sizeDelta = new Vector2(cap, cap);
            return rt;
        }

        /// <summary>
        /// Palet renginin saydam hali. Ham renk uydurmamak icin: ton hep paletten gelir,
        /// yalnizca alfa degisir — joystick oyun alaninin uzerinde yuzdugu icin opak olamaz.
        /// </summary>
        private static Color Saydam(Color renk, float alfa) =>
            new Color(renk.r, renk.g, renk.b, alfa);
    }
}
