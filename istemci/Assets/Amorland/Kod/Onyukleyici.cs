using UnityEngine;

namespace Amorland
{
    /// <summary>
    /// Oyunun giris noktasi.
    ///
    /// ⚠ Kasitli olarak <b>sahne kurulumu gerektirmez.</b> Bos bir sahne yeter; kamera,
    /// isik, kanvas ve butun nesneler kodla kurulur. Sebep A.3'te yazili: Unity Editor
    /// isi (sahne, prefab, inspector) bu projede en kirilgan katman ve <c>.unity</c>
    /// catismalari araclarla cozulmuyor (A.7). Kodla kurulan dunya git'te temiz birlesir.
    ///
    /// Cila asamasinda prefab'a tasinabilir; o zamana kadar tek gercek kaynak koddur.
    /// </summary>
    public static class Onyukleyici
    {
        /// <summary>Acilista ne kurulur.</summary>
        public enum Mod
        {
            /// <summary>Vadi haritasi. Sunucu gerektirmez, her zaman calisir.</summary>
            Vadi,
            /// <summary>Oyunun kendisi. Acilista sunucuya baglanir.</summary>
            Oyun,
        }

        /// <summary>
        /// Acilis modu.
        ///
        /// ⚠ Varsayilan <see cref="Mod.Vadi"/>: <see cref="Amorland.Oyun"/> acilista
        /// sunucuya baglanir ve sunucu ayakta degilse ekranda yukleme yazisindan baska
        /// bir sey gorunmez. Vadi haritasi tek basina calistigi icin Play tusuna
        /// basildiginda her kosulda gezilebilir bir dunya cikiyor. Vadideki
        /// "Oyuna gir" butonu <see cref="OyunaGec"/> ile oyuna gecer.
        /// </summary>
        public static Mod AcilisModu = Mod.Vadi;

        private static GameObject _kok;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Baslat()
        {
            if (_kok != null) return;
            OnbellekleriBosalt();

            _kok = Kok();
            if (AcilisModu == Mod.Vadi) _kok.AddComponent<VadiSahnesi>();
            else _kok.AddComponent<Oyun>();
        }

        /// <summary>
        /// Vadi haritasinda secilen bolge. Oyun, oturum kurulunca bunu okur ve
        /// <c>BolgeyeGit</c> cagirir (spec B.4: bolgeye dokun, o sahneye gir).
        /// Statik cunku vadi kokU yikilip oyun kokU kurulurken tasiyacak nesne yok.
        /// </summary>
        public static string IstenenBolge;

        /// <summary>Vadiden oyuna gecer: mevcut koku atar, yerine oyunu kurar.</summary>
        public static void OyunaGec(string bolgeId = null)
        {
            IstenenBolge = bolgeId;
            if (_kok != null) Object.Destroy(_kok);
            OnbellekleriBosalt();

            _kok = Kok();
            _kok.AddComponent<Oyun>();
        }

        private static GameObject Kok()
        {
            var go = new GameObject("Amorland");
            Object.DontDestroyOnLoad(go);
            return go;
        }

        /// <summary>
        /// ⚠ UI sprite'lari ve karo sayfalari statik onbellekte tutuluyor. Play modu
        /// bittiginde Unity texture'lari yok eder ama Editor'de domain reload kapaliysa
        /// statik alanlar hayatta kalir — ikinci Play yok edilmis texture'a bakar ve
        /// arayuz kaybolur. Ayni sey mod degisiminde de gecerli.
        /// </summary>
        private static void OnbellekleriBosalt()
        {
            Gorsel.Sekil.Unut();
            Gorsel.SpriteDeposu.Unut();
            Gorsel.Karo64.Unut();
            Gorsel.VadiHaritasi.OnbellegiTemizle();
        }
    }
}
