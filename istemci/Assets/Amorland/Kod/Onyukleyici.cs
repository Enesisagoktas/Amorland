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
        private static GameObject _kok;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Baslat()
        {
            if (_kok != null) return;

            _kok = new GameObject("Amorland");
            Object.DontDestroyOnLoad(_kok);
            _kok.AddComponent<Oyun>();
        }
    }
}
