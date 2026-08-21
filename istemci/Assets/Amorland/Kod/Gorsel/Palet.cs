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

        // ---------------------------------------------------------------- parlak arayuz
        //
        // Kozy ciftlik arayuzu KOYU DEGILDIR. Koyu panel, ekrandaki sicak cim/toprak
        // paletinin uzerinde "sistem menusu" gibi durur ve oyundan kopar. Buradaki
        // tonlar kagit/ahsap ailesinden: kalin kahve cerceve, krem govde, doygun
        // yesil onay. Yazi krem uzerinde KOYU olur — beyaz yazi okunmaz.

        /// <summary>Panel govdesi: krem kagit.</summary>
        public static readonly Color UiKagit = H(252, 238, 208);

        /// <summary>Panelin icine gomulu yuzey: liste satiri, slot, giris kutusu.</summary>
        public static readonly Color UiKagitKoyu = H(236, 214, 172);

        /// <summary>Daha da cukur yuzey: bos slot, cubuk zemini.</summary>
        public static readonly Color UiKagitCukur = H(214, 188, 146);

        /// <summary>Kalin dis cerceve — Hay Day benzeri arayuzun imzasi budur.</summary>
        public static readonly Color UiCerceve = H(120, 82, 50);

        /// <summary>Cerceve uzerindeki acik pah (ust kenar isigi).</summary>
        // ⚠ 0.35 opak pah butonun ust yarisini ayri bir "bos bar" gibi gosteriyordu
        // (UI denetimi): parlama isik hissi vermeli, katman hissi degil.
        public static readonly Color UiPah = new Color(1f, 1f, 1f, 0.22f);

        /// <summary>Krem zemin uzerine yazi.</summary>
        public static readonly Color UiKoyuYazi = H(78, 52, 32);
        // ⚠ 0.62 alfa krem zeminde ~2.2:1 kontrast veriyordu (UI denetimi, WCAG
        // siniri 4.5:1). Solgunluk hiyerarsi icin yeter, okunmazlik icin degil.
        public static readonly Color UiKoyuYaziSolgun = new Color(78f / 255f, 52f / 255f, 32f / 255f, 0.85f);

        /// <summary>Onay/ana eylem butonu.</summary>
        public static readonly Color UiYesil = H(126, 184, 68);
        public static readonly Color UiYesilKoyu = H(88, 140, 46);

        /// <summary>Iptal/yikici eylem.</summary>
        public static readonly Color UiKirmizi = H(220, 94, 76);
        public static readonly Color UiKirmiziKoyu = H(172, 64, 50);

        /// <summary>Ikincil ahsap buton.</summary>
        public static readonly Color UiAhsapButon = H(178, 130, 84);
        public static readonly Color UiAhsapButonKoyu = H(134, 94, 58);

        /// <summary>Altin/para ve XP vurgusu.</summary>
        public static readonly Color UiAltin = H(248, 194, 66);
        public static readonly Color UiAltinKoyu = H(198, 146, 40);

        /// <summary>Konturlu yazinin kontur rengi. Butonlarda beyaz yaziyi zeminden ayirir.</summary>
        public static readonly Color UiKontur = new Color(0.24f, 0.15f, 0.08f, 0.9f);

        // ---------------------------------------------------------------- HUD kapsulleri
        //
        // Referans arayuzun tasiyici fikri: oyun dunyasi zengin ve dolu oldugu icin
        // HUD ogeleri KOYU kapsullerde durur ve etrafina ACIK bir cerceve alir.
        // Cerceve olmadan koyu kapsul dunyanin koyu bolgelerinde kaybolur; asil
        // okunurlugu saglayan sey kapsulun kendisi degil, o ince acik hattir.

        /// <summary>HUD kapsul govdesi — dunyanin uzerinde yari saydam koyu.</summary>
        public static readonly Color KapsulZemin = new Color(0.125f, 0.105f, 0.092f, 0.93f);

        /// <summary>Kapsul cercevesi: krem. Koyu kapsulu her zeminden ayirir.</summary>
        public static readonly Color KapsulCerceve = H(232, 216, 184);

        /// <summary>Secili/onemli kapsul cercevesi: altin.</summary>
        public static readonly Color KapsulAltin = H(214, 170, 76);

        /// <summary>Parsomen panel — gorev, alet cubugu, liste.</summary>
        public static readonly Color Parsomen = H(246, 236, 214);
        public static readonly Color ParsomenKoyu = H(226, 210, 180);

        /// <summary>"Ekle" dugmesi.</summary>
        public static readonly Color ArtiYesil = H(108, 190, 72);
        public static readonly Color ArtiYesilKoyu = H(76, 148, 48);

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
