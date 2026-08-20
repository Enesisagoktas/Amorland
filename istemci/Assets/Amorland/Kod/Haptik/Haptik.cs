using UnityEngine;

namespace Amorland.Haptik
{
    /// <summary>
    /// Dokunsal geri bildirim (B.23).
    ///
    /// ⚠ <b>Haptik, ses kapaliyken calisan TEK kanaldir.</b> Oyuncularin cogu sesi kapali
    /// oynayacaksa dokunsal geri bildirim sesten onemlidir — ve bedavadir.
    ///
    /// ⚠ <c>Handheld.Vibrate()</c> tek basina yetmez: Android'de sabit ~400 ms titresim
    /// verir, desen olusturulamaz ve iOS'ta hic calismaz. Bu yuzden Android'de
    /// <c>VibrationEffect</c>, iOS'ta <c>UIFeedbackGenerator</c> kullanilir.
    ///
    /// ⚠ Kuyruk eylemlerinde SADECE ilk ve son titresir. 24 karo boyandiginda 24 titresim
    /// iskencedir; bu kurali cagiran taraf uygular (<see cref="KuyrukIlk"/> / <see cref="KuyrukSon"/>).
    /// </summary>
    public static class Haptik
    {
        public enum Desen
        {
            /// <summary>Hasat: cok hafif, tek darbe.</summary>
            CokHafif,
            /// <summary>Dugum kirilma: orta, tek darbe.</summary>
            Orta,
            /// <summary>Alet kirilma: CIFT darbe — sesi kapali oyuncu bunu kacirmamali.</summary>
            CiftDarbe,
            /// <summary>Seviye atlama: yukselen uclu desen.</summary>
            UcluYukselen,
            /// <summary>Ortak proje tamamlandi: uzun, belirgin.</summary>
            Uzun,
            /// <summary>Hata: cok kisa, sert.</summary>
            Hata
        }

        private const string Anahtar = "amorland.haptik.acik";

        public static bool Acik
        {
            get => PlayerPrefs.GetInt(Anahtar, 1) == 1;
            set { PlayerPrefs.SetInt(Anahtar, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void Calistir(Desen desen)
        {
            if (!Acik) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidTitret(desen);
#elif UNITY_IOS && !UNITY_EDITOR
            IosTitret(desen);
#else
            // Editor ve masaustunde titresim donanimi yok; sessizce gecilir.
#endif
        }

        /// <summary>Kuyruk basi: titresir.</summary>
        public static void KuyrukIlk(Desen desen) => Calistir(desen);

        /// <summary>Kuyruk sonu: titresir. Aradakiler TITREMEZ.</summary>
        public static void KuyrukSon(Desen desen) => Calistir(desen);

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _titresim;
        private static int _apiSeviyesi;

        private static void AndroidHazirla()
        {
            if (_titresim != null) return;
            using (var birim = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var etkinlik = birim.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                _titresim = etkinlik.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            using (var surum = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                _apiSeviyesi = surum.GetStatic<int>("SDK_INT");
            }
        }

        private static void AndroidTitret(Desen desen)
        {
            AndroidHazirla();
            if (_titresim == null) return;

            long[] desenDizisi;
            int[] siddet;
            switch (desen)
            {
                case Desen.CokHafif:     desenDizisi = new long[] { 0, 12 };            siddet = new[] { 0, 60 }; break;
                case Desen.Orta:         desenDizisi = new long[] { 0, 28 };            siddet = new[] { 0, 150 }; break;
                case Desen.CiftDarbe:    desenDizisi = new long[] { 0, 30, 70, 30 };    siddet = new[] { 0, 200, 0, 200 }; break;
                case Desen.UcluYukselen: desenDizisi = new long[] { 0, 20, 60, 26, 60, 34 }; siddet = new[] { 0, 90, 0, 150, 0, 220 }; break;
                case Desen.Uzun:         desenDizisi = new long[] { 0, 220 };           siddet = new[] { 0, 200 }; break;
                default:                 desenDizisi = new long[] { 0, 10 };            siddet = new[] { 0, 255 }; break;
            }

            if (_apiSeviyesi >= 26)
            {
                using (var efektSinifi = new AndroidJavaClass("android.os.VibrationEffect"))
                {
                    AndroidJavaObject efekt =
                        efektSinifi.CallStatic<AndroidJavaObject>("createWaveform", desenDizisi, siddet, -1);
                    _titresim.Call("vibrate", efekt);
                }
            }
            else
            {
                _titresim.Call("vibrate", desenDizisi, -1);
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _AmorlandHaptik(int desen);

        private static void IosTitret(Desen desen)
        {
            // iOS tarafi Plugins/iOS/AmorlandHaptik.mm icinde UIFeedbackGenerator kullanir.
            _AmorlandHaptik((int)desen);
        }
#endif
    }
}
