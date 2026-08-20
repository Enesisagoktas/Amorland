using UnityEngine;

namespace Amorland.Ag
{
    /// <summary>
    /// Sunucu adresi ve istemci surumu.
    ///
    /// Editorde ve cihazda farkli adresler gerekir: Android emulatorunde
    /// <c>localhost</c> cihazin kendisidir, gelistirme makinesi degil.
    /// O yuzden adres <see cref="PlayerPrefs"/> ile de gecilebilir — APK yeniden
    /// derlemeden test sunucusu degistirilebilsin.
    /// </summary>
    public static class SunucuAyari
    {
        public const string IstemciSurum = "0.1.0";

        private const string AnahtarAdres = "amorland.sunucu.adres";

        /// <summary>Android emulatorunden ana makineye ulasmanin yolu.</summary>
        private const string AndroidEmulatorAdresi = "http://10.0.2.2:8080";

        private const string VarsayilanAdres = "http://localhost:8080";

        public static string Temel
        {
            get
            {
                string kayitli = PlayerPrefs.GetString(AnahtarAdres, "");
                if (!string.IsNullOrEmpty(kayitli)) return kayitli.TrimEnd('/');
#if UNITY_ANDROID && !UNITY_EDITOR
                return AndroidEmulatorAdresi;
#else
                return VarsayilanAdres;
#endif
            }
        }

        public static void AdresYaz(string adres)
        {
            PlayerPrefs.SetString(AnahtarAdres, string.IsNullOrEmpty(adres) ? "" : adres.TrimEnd('/'));
            PlayerPrefs.Save();
        }

        public static string WsAdresi
        {
            get
            {
                string t = Temel;
                if (t.StartsWith("https://")) return "wss://" + t.Substring(8) + "/ws";
                if (t.StartsWith("http://")) return "ws://" + t.Substring(7) + "/ws";
                return "ws://" + t + "/ws";
            }
        }

        /// <summary>
        /// Cihaz kimligi. Misafir hesabin anahtaridir (B.19).
        /// <c>SystemInfo.deviceUniqueIdentifier</c> bazi platformlarda degisebiliyor,
        /// o yuzden ilk uretilen deger PlayerPrefs'e yazilir ve bir daha degistirilmez.
        /// </summary>
        public static string CihazKimligi()
        {
            const string anahtar = "amorland.cihaz.id";
            string kayitli = PlayerPrefs.GetString(anahtar, "");
            if (!string.IsNullOrEmpty(kayitli)) return kayitli;

            string yeni = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(yeni) || yeni == SystemInfo.unsupportedIdentifier)
            {
                yeni = System.Guid.NewGuid().ToString();
            }
            PlayerPrefs.SetString(anahtar, yeni);
            PlayerPrefs.Save();
            return yeni;
        }
    }
}
