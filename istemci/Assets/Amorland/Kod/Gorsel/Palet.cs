using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Sabit 16 renkli palet (B.22 stil sozlesmesi).
    ///
    /// ⚠ Tum varliklar YALNIZCA bu paletten renk alir. Gradyan yok, doku yok, PBR yok.
    /// Palet disina cikan tek bir renk bile low-poly gorunumu bozar; asil tutarlilik
    /// kaynagi modeller degil, sinirli palettir.
    ///
    /// Renkler yumusak ve dusuk doygunlukta: kozy oyun keskin kontrast sevmez.
    /// </summary>
    public static class Palet
    {
        private static Color H(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        // Zemin ve toprak
        public static readonly Color Cim = H(126, 168, 92);
        public static readonly Color CimKoyu = H(104, 143, 76);
        public static readonly Color Toprak = H(138, 105, 74);
        public static readonly Color TopraKoyu = H(112, 84, 58);
        public static readonly Color TopraIslak = H(88, 65, 45);

        // Bitki
        public static readonly Color Yaprak = H(96, 152, 74);
        public static readonly Color YaprakKoyu = H(70, 118, 56);
        public static readonly Color Sap = H(120, 160, 80);
        public static readonly Color Bugday = H(214, 180, 92);

        // Yapi ve ahsap
        public static readonly Color Ahsap = H(150, 111, 74);
        public static readonly Color AhsapKoyu = H(112, 80, 52);
        public static readonly Color Tas = H(150, 150, 148);
        public static readonly Color TasKoyu = H(112, 112, 110);
        public static readonly Color Cati = H(178, 96, 78);

        // Su ve gokyuzu
        public static readonly Color Su = H(94, 148, 178);
        public static readonly Color Gokyuzu = H(178, 206, 220);

        // UI
        //
        // ⚠ Notr gri degil, ILIK koyu tonlar. Kozy bir ciftlik oyununda notr gri panel
        // ekrandaki sicak yesil/toprak paletiyle catisir ve arayuzu oyundan kopuk,
        // "sistem menusu" gibi gosterir. Tonlar hafif kahverengiye cekilmistir.
        public static readonly Color UiZemin = new Color(0.115f, 0.105f, 0.095f, 0.90f);
        public static readonly Color UiPanel = new Color(0.175f, 0.160f, 0.145f, 0.97f);

        /// <summary>Panelin uzerine binen yuzey: slot, giris kutusu, ikincil buton.</summary>
        public static readonly Color UiYuzey = new Color(0.245f, 0.222f, 0.196f, 1f);

        /// <summary>Basili/secili yuzey.</summary>
        public static readonly Color UiYuzeyKoyu = new Color(0.145f, 0.132f, 0.120f, 1f);

        public static readonly Color UiCizgi = new Color(1f, 0.96f, 0.90f, 0.10f);
        public static readonly Color UiYazi = new Color(0.960f, 0.945f, 0.915f, 1f);
        public static readonly Color UiYaziSolgun = new Color(0.960f, 0.945f, 0.915f, 0.58f);
        public static readonly Color UiVurgu = H(224, 188, 96);
        public static readonly Color UiVurguKoyu = H(186, 152, 72);

        /// <summary>Vurgu zemin uzerine yazi. Acik zeminde beyaz yazi okunmaz.</summary>
        public static readonly Color UiVurguUstuYazi = new Color(0.16f, 0.13f, 0.08f, 1f);

        public static readonly Color UiUyari = H(212, 116, 92);
        public static readonly Color UiOnay = H(132, 176, 96);
        public static readonly Color Golge = new Color(0f, 0f, 0f, 0.38f);

        /// <summary>Buyume asamasina gore bitki rengi. Asama ilerledikce koyulasir ve doyar.</summary>
        public static Color UrunRengi(int asama, int asamaSayisi)
        {
            float t = asamaSayisi <= 1 ? 1f : asama / (float)(asamaSayisi - 1);
            return Color.Lerp(Sap, YaprakKoyu, t);
        }

        /// <summary>Sulanmis toprak daha koyudur — sulama durumu SESSIZ degil, GORUNUR olmali.</summary>
        public static Color ToprakRengi(bool sulu) => sulu ? TopraIslak : Toprak;
    }
}
