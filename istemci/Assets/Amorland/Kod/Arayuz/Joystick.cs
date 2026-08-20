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

        /// <summary>-1..1 araliginda yon. Sifir ise girdi yok.</summary>
        public Vector2 Yon { get; private set; }

        public bool Aktif => _aktif;

        public void Kur(Canvas kanvas)
        {
            _kanvas = kanvas;
            _kok = UiKit.Panel(kanvas.transform, "Joystick", Color.clear);
            UiKit.Doldur(_kok);
            _kok.gameObject.GetComponent<Image>().raycastTarget = false;

            _taban = UiKit.Panel(_kok, "Taban", new Color(1f, 1f, 1f, 0.10f));
            _taban.sizeDelta = new Vector2(Yaricap * 2f, Yaricap * 2f);
            _taban.gameObject.GetComponent<Image>().raycastTarget = false;

            _sap = UiKit.Panel(_taban, "Sap", new Color(1f, 1f, 1f, 0.28f));
            _sap.sizeDelta = new Vector2(64f, 64f);
            _sap.gameObject.GetComponent<Image>().raycastTarget = false;

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
    }
}
