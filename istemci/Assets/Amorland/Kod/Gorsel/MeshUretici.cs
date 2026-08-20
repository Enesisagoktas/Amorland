using System.Collections.Generic;
using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Prosedurel low-poly mesh uretimi (B.22).
    ///
    /// ⚠ Butun mesh'ler <b>sert normalli</b> uretilir: her ucgenin kendi koseleri vardir,
    /// paylasilan kose yoktur. Yumusak normal low-poly gorunumu bozar ve "ucuz mobil oyun"
    /// hissi verir. Bu, uslup sozlesmesinin en ucuz ve en etkili maddesi.
    ///
    /// ⚠ Kamera yaw KILITLI (B.5): modellerin arkasi hic gorunmez. Bu yuzden hicbir sekilde
    /// arka yuz detayi uretilmez — asset butcesinin en buyuk kaldiraci budur.
    /// </summary>
    public static class MeshUretici
    {
        private class Yapici
        {
            public readonly List<Vector3> Koseler = new List<Vector3>();
            public readonly List<int> Ucgenler = new List<int>();
            public readonly List<Color> Renkler = new List<Color>();

            /// <summary>Ucgen ekler. Her cagrida YENI koseler yazilir -> sert normal.</summary>
            public void Ucgen(Vector3 a, Vector3 b, Vector3 c, Color renk)
            {
                int i = Koseler.Count;
                Koseler.Add(a);
                Koseler.Add(b);
                Koseler.Add(c);
                Renkler.Add(renk);
                Renkler.Add(renk);
                Renkler.Add(renk);
                Ucgenler.Add(i);
                Ucgenler.Add(i + 1);
                Ucgenler.Add(i + 2);
            }

            public void DortGen(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color renk)
            {
                Ucgen(a, b, c, renk);
                Ucgen(a, c, d, renk);
            }

            /// <summary>
            /// Ucgeni, verilen DIS yone bakacak sekilde ekler.
            ///
            /// ⚠ Sarim yonunu elle tutturmak bu dosyada zaten bir kez patladi: zemin,
            /// karo, kuyu, ev ve karakterin ust yuzleri ters sariliydi ve Unity arka
            /// yuzu kirptigi icin dunya SESSIZCE gorunmuyordu — hata da uyari da yok,
            /// sadece bos ekran. Burada niyet yazilir ("bu yuz yukari baksin"), sarimi
            /// kod ayarlar. Ayni hata bir daha sessizce olamaz.
            /// </summary>
            public void UcgenYonlu(Vector3 a, Vector3 b, Vector3 c, Vector3 disYon, Color renk)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), disYon) < 0f)
                {
                    Vector3 gecici = b;
                    b = c;
                    c = gecici;
                }
                Ucgen(a, b, c, renk);
            }

            /// <summary>Dortgeni verilen dis yone bakacak sekilde ekler.</summary>
            public void DortGenYonlu(
                Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 disYon, Color renk)
            {
                UcgenYonlu(a, b, c, disYon, renk);
                UcgenYonlu(a, c, d, disYon, renk);
            }

            /// <summary>
            /// Iki yuzlu (cift tarafli) ucgen. Ince yaprak gibi tek duzlemli parcalar
            /// icin: hangi taraftan bakilirsa bakilsin gorunur.
            /// </summary>
            public void UcgenCiftYuz(Vector3 a, Vector3 b, Vector3 c, Color renk)
            {
                Ucgen(a, b, c, renk);
                Ucgen(a, c, b, renk);
            }

            public Mesh Kapat(string ad)
            {
                var mesh = new Mesh { name = ad };
                mesh.vertices = Koseler.ToArray();
                mesh.triangles = Ucgenler.ToArray();
                mesh.colors = Renkler.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        // ------------------------------------------------------------------ temel sekiller

        /// <summary>Merkezi tabanda olan kutu. Ust yuz daha acik, yan yuzler koyu: sahte isik.</summary>
        public static Mesh Kutu(Vector3 boyut, Color renk, string ad = "kutu")
        {
            var y = new Yapici();
            KutuEkle(y, Vector3.zero, boyut, renk);
            return y.Kapat(ad);
        }

        private static void KutuEkle(Yapici y, Vector3 merkez, Vector3 boyut, Color renk)
        {
            float gx = boyut.x / 2f, gz = boyut.z / 2f, yy = boyut.y;
            Vector3 o = merkez;

            Color ust = renk;
            Color yan = renk * 0.86f;
            Color on = renk * 0.94f;

            Vector3 a0 = o + new Vector3(-gx, 0, -gz);
            Vector3 a1 = o + new Vector3(gx, 0, -gz);
            Vector3 a2 = o + new Vector3(gx, 0, gz);
            Vector3 a3 = o + new Vector3(-gx, 0, gz);
            Vector3 u0 = a0 + Vector3.up * yy;
            Vector3 u1 = a1 + Vector3.up * yy;
            Vector3 u2 = a2 + Vector3.up * yy;
            Vector3 u3 = a3 + Vector3.up * yy;

            y.DortGenYonlu(u0, u1, u2, u3, Vector3.up, ust);       // ust
            y.DortGenYonlu(a1, a0, u0, u1, Vector3.back, on);      // on (-z, kameraya bakan)
            y.DortGenYonlu(a0, a3, u3, u0, Vector3.left, yan);     // sol
            y.DortGenYonlu(a2, a1, u1, u2, Vector3.right, yan);    // sag
            // Arka yuz uretilmez: yaw kilitli oldugu icin asla gorunmez.
        }

        /// <summary>Yatay disk. Blob golge ve zemin yamalari icin.</summary>
        public static Mesh Disk(float yaricap, int dilim, Color renk, string ad = "disk")
        {
            var y = new Yapici();
            Vector3 merkez = Vector3.zero;
            for (int i = 0; i < dilim; i++)
            {
                float a0 = (i / (float)dilim) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)dilim) * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * yaricap, 0, Mathf.Sin(a0) * yaricap);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * yaricap, 0, Mathf.Sin(a1) * yaricap);
                y.UcgenYonlu(merkez, p0, p1, Vector3.up, renk);
            }
            return y.Kapat(ad);
        }

        /// <summary>Koni. Agac tepesi ve fidan icin.</summary>
        public static Mesh Koni(float yaricap, float yukseklik, int dilim, Color renk, string ad = "koni")
        {
            var y = new Yapici();
            Vector3 tepe = Vector3.up * yukseklik;
            for (int i = 0; i < dilim; i++)
            {
                float a0 = (i / (float)dilim) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)dilim) * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * yaricap, 0, Mathf.Sin(a0) * yaricap);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * yaricap, 0, Mathf.Sin(a1) * yaricap);
                float orta = (a0 + a1) * 0.5f;
                float golge = 0.86f + 0.14f * Mathf.Abs(Mathf.Cos(orta));
                Vector3 disYon = new Vector3(Mathf.Cos(orta), 0f, Mathf.Sin(orta));
                y.UcgenYonlu(p0, p1, tepe, disYon, renk * golge);
            }
            return y.Kapat(ad);
        }

        public static Mesh Silindir(float yaricap, float yukseklik, int dilim, Color renk, string ad = "silindir")
        {
            var y = new Yapici();
            for (int i = 0; i < dilim; i++)
            {
                float a0 = (i / (float)dilim) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)dilim) * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * yaricap, 0, Mathf.Sin(a0) * yaricap);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * yaricap, 0, Mathf.Sin(a1) * yaricap);
                Vector3 u0 = p0 + Vector3.up * yukseklik;
                Vector3 u1 = p1 + Vector3.up * yukseklik;
                float orta = (a0 + a1) * 0.5f;
                float golge = 0.84f + 0.16f * Mathf.Abs(Mathf.Cos(orta));
                Vector3 disYon = new Vector3(Mathf.Cos(orta), 0f, Mathf.Sin(orta));
                y.DortGenYonlu(p0, p1, u1, u0, disYon, renk * golge);
                y.UcgenYonlu(u0, u1, Vector3.up * yukseklik, Vector3.up, renk);
            }
            return y.Kapat(ad);
        }

        // ------------------------------------------------------------------ oyun nesneleri

        /// <summary>Islenmis tarla karosu: hafif yukseltilmis, kenarlari pahli toprak.</summary>
        public static Mesh TarlaKarosu(Color renk)
        {
            var y = new Yapici();
            const float k = 0.46f;   // karo yarim genisligi (1 birim karo, aralarinda bosluk)
            const float h = 0.06f;   // yukseklik
            const float pah = 0.08f;

            Vector3 u0 = new Vector3(-k + pah, h, -k + pah);
            Vector3 u1 = new Vector3(k - pah, h, -k + pah);
            Vector3 u2 = new Vector3(k - pah, h, k - pah);
            Vector3 u3 = new Vector3(-k + pah, h, k - pah);
            Vector3 a0 = new Vector3(-k, 0, -k);
            Vector3 a1 = new Vector3(k, 0, -k);
            Vector3 a2 = new Vector3(k, 0, k);
            Vector3 a3 = new Vector3(-k, 0, k);

            y.DortGenYonlu(u0, u1, u2, u3, Vector3.up, renk);
            y.DortGenYonlu(a1, a0, u0, u1, Vector3.back, renk * 0.88f);
            y.DortGenYonlu(a0, a3, u3, u0, Vector3.left, renk * 0.82f);
            y.DortGenYonlu(a2, a1, u1, u2, Vector3.right, renk * 0.82f);
            return y.Kapat("tarla_karosu");
        }

        /// <summary>
        /// Urun mesh'i, buyume asamasina gore. 4 asama (D: buyumeAsamasi.asamaSayisi).
        /// Asama 0 filiz, 3 olgun. Silueti asamayla belirgin degistir: oyuncu
        /// uzaktan bakip "olgun mu" diyebilmeli — bu bir OKUNABILIRLIK sarti.
        /// </summary>
        public static Mesh Urun(int asama, int asamaSayisi, Color renk)
        {
            var y = new Yapici();
            float t = asamaSayisi <= 1 ? 1f : Mathf.Clamp01(asama / (float)(asamaSayisi - 1));
            float boy = Mathf.Lerp(0.12f, 0.55f, t);
            float en = Mathf.Lerp(0.06f, 0.20f, t);

            // Sap
            KutuEkle(y, Vector3.zero, new Vector3(en * 0.4f, boy, en * 0.4f), Palet.Sap);

            // Yapraklar: asama arttikca sayilari artar
            int yaprakSayisi = 2 + asama;
            for (int i = 0; i < yaprakSayisi; i++)
            {
                float aci = (i / (float)yaprakSayisi) * Mathf.PI * 2f;
                float yaricap = en * 0.9f;
                Vector3 kok = new Vector3(0, boy * (0.35f + 0.15f * (i % 2)), 0);
                Vector3 uc = kok + new Vector3(Mathf.Cos(aci) * yaricap, boy * 0.18f, Mathf.Sin(aci) * yaricap);
                Vector3 yanA = kok + new Vector3(Mathf.Cos(aci + 0.5f) * yaricap * 0.35f, 0,
                                                 Mathf.Sin(aci + 0.5f) * yaricap * 0.35f);
                Vector3 yanB = kok + new Vector3(Mathf.Cos(aci - 0.5f) * yaricap * 0.35f, 0,
                                                 Mathf.Sin(aci - 0.5f) * yaricap * 0.35f);
                y.UcgenCiftYuz(yanA, yanB, uc, renk);
            }

            // Olgun asamada tepede meyve/basak: "hasada hazir" isareti
            if (asama >= asamaSayisi - 1)
            {
                KutuEkle(y, new Vector3(0, boy, 0), new Vector3(en, en * 0.9f, en), renk);
            }
            return y.Kapat("urun_" + asama);
        }

        /// <summary>Karakter: govde + kafa + iki kol. ~40 ucgen; hedef 1.200-2.000 degil,
        /// cunku bu bir YER TUTUCU (CC0 karakterle degistirilecek, B.22 kaynak stratejisi).</summary>
        public static Mesh Karakter(Color govdeRengi, Color tenRengi)
        {
            var y = new Yapici();
            const float boy = 1.6f; // B.5: karakter 1.6 birim
            KutuEkle(y, Vector3.zero, new Vector3(0.42f, boy * 0.55f, 0.28f), govdeRengi);
            KutuEkle(y, new Vector3(0, boy * 0.55f, 0), new Vector3(0.34f, boy * 0.28f, 0.30f), tenRengi);
            KutuEkle(y, new Vector3(-0.28f, boy * 0.30f, 0), new Vector3(0.12f, boy * 0.25f, 0.12f), govdeRengi * 0.9f);
            KutuEkle(y, new Vector3(0.28f, boy * 0.30f, 0), new Vector3(0.12f, boy * 0.25f, 0.12f), govdeRengi * 0.9f);
            return y.Kapat("karakter");
        }

        /// <summary>Kuyu: tas govde + ahsap cati. Tarla sahnesinin icinde (B.11).</summary>
        public static Mesh Kuyu()
        {
            var y = new Yapici();
            KutuEkle(y, Vector3.zero, new Vector3(0.9f, 0.5f, 0.9f), Palet.Tas);
            KutuEkle(y, new Vector3(0, 0.5f, 0), new Vector3(0.75f, 0.05f, 0.75f), Palet.Su);
            KutuEkle(y, new Vector3(-0.35f, 0.55f, 0), new Vector3(0.1f, 0.8f, 0.1f), Palet.Ahsap);
            KutuEkle(y, new Vector3(0.35f, 0.55f, 0), new Vector3(0.1f, 0.8f, 0.1f), Palet.Ahsap);
            KutuEkle(y, new Vector3(0, 1.35f, 0), new Vector3(1.1f, 0.12f, 1.0f), Palet.Cati);
            return y.Kapat("kuyu");
        }

        /// <summary>Ev: govde + prizma cati.</summary>
        public static Mesh Ev()
        {
            var y = new Yapici();
            KutuEkle(y, Vector3.zero, new Vector3(3f, 1.8f, 2.4f), Palet.Ahsap);

            float gx = 1.6f, gz = 1.3f, tabani = 1.8f, tepe = 1.1f;
            Vector3 o0 = new Vector3(-gx, tabani, -gz);
            Vector3 o1 = new Vector3(gx, tabani, -gz);
            Vector3 a0 = new Vector3(-gx, tabani, gz);
            Vector3 a1 = new Vector3(gx, tabani, gz);
            Vector3 t0 = new Vector3(-gx, tabani + tepe, 0);
            Vector3 t1 = new Vector3(gx, tabani + tepe, 0);

            // Cati egimleri yukari + disa bakar; alinliklar yanlara.
            y.DortGenYonlu(o0, o1, t1, t0, new Vector3(0f, 1f, -1f), Palet.Cati);
            y.DortGenYonlu(a1, a0, t0, t1, new Vector3(0f, 1f, 1f), Palet.Cati * 0.88f);
            y.UcgenYonlu(o0, t0, a0, Vector3.left, Palet.Cati * 0.8f);
            y.UcgenYonlu(o1, a1, t1, Vector3.right, Palet.Cati * 0.8f);

            KutuEkle(y, new Vector3(0, 0, -1.21f), new Vector3(0.7f, 1.2f, 0.05f), Palet.AhsapKoyu);
            return y.Kapat("ev");
        }

        /// <summary>Yabani kaynak: dal / tas parcasi / ot. Hepsi ELLE toplanir, alet gerekmez (B.14).</summary>
        public static Mesh YabaniKaynak(string tur)
        {
            var y = new Yapici();
            switch (tur)
            {
                case "tas_parca":
                    KutuEkle(y, Vector3.zero, new Vector3(0.34f, 0.22f, 0.30f), Palet.Tas);
                    KutuEkle(y, new Vector3(0.16f, 0, 0.10f), new Vector3(0.20f, 0.14f, 0.18f), Palet.TasKoyu);
                    break;
                case "yem_otu":
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 1.25f;
                        KutuEkle(
                            y,
                            new Vector3(Mathf.Cos(a) * 0.12f, 0, Mathf.Sin(a) * 0.12f),
                            new Vector3(0.05f, 0.3f + 0.06f * (i % 3), 0.05f),
                            Palet.Yaprak);
                    }
                    break;
                default: // dal
                    KutuEkle(y, Vector3.zero, new Vector3(0.5f, 0.08f, 0.09f), Palet.AhsapKoyu);
                    KutuEkle(y, new Vector3(0.12f, 0.06f, 0.05f), new Vector3(0.3f, 0.06f, 0.07f), Palet.Ahsap);
                    break;
            }
            return y.Kapat("yabani_" + tur);
        }

        /// <summary>
        /// Agac: govde + iki kademe yaprak konisi.
        ///
        /// Dekor amaclidir, uzerinde yurunmez. Sahnenin kenarlarini doldurup "bos tarla"
        /// hissini kirar; oyuncunun nerede oldugunu anlamasi cerceve olmadan zordur.
        /// </summary>
        public static Mesh Agac(float olcek = 1f)
        {
            var y = new Yapici();
            GovdeEkle(y, olcek);
            KoniEkle(y, new Vector3(0f, 0.9f * olcek, 0f), 0.85f * olcek, 1.1f * olcek, 7, Palet.Yaprak);
            KoniEkle(y, new Vector3(0f, 1.55f * olcek, 0f), 0.6f * olcek, 0.9f * olcek, 7, Palet.YaprakKoyu);
            return y.Kapat("agac");
        }

        private static void GovdeEkle(Yapici y, float olcek)
        {
            SilindirEkle(y, Vector3.zero, 0.11f * olcek, 1.0f * olcek, 6, Palet.AhsapKoyu);
        }

        /// <summary>Cit parcasi: iki direk + iki yatay lata. Parsel sinirini gorunur kilar.</summary>
        public static Mesh CitParcasi(float uzunluk)
        {
            var y = new Yapici();
            // ⚠ Direkler yalnizca ucta olursa tepeden bakan kamerada cit duz bir
            // kahverengi serit gibi okunuyor. Duzenli araliklarla direk konur ve
            // direkler latalardan YUKSEK tutulur: ritim, seridi cit yapan seydir.
            int direkSayisi = Mathf.Max(2, Mathf.RoundToInt(uzunluk / 1.4f) + 1);
            for (int i = 0; i < direkSayisi; i++)
            {
                float t = direkSayisi == 1 ? 0.5f : i / (float)(direkSayisi - 1);
                float x = Mathf.Lerp(-uzunluk / 2f, uzunluk / 2f, t);
                KutuEkle(y, new Vector3(x, 0f, 0f), new Vector3(0.13f, 0.78f, 0.13f), Palet.Ahsap);
            }
            KutuEkle(y, new Vector3(0f, 0.52f, 0f), new Vector3(uzunluk, 0.07f, 0.06f), Palet.AhsapKoyu);
            KutuEkle(y, new Vector3(0f, 0.28f, 0f), new Vector3(uzunluk, 0.07f, 0.06f), Palet.AhsapKoyu);
            return y.Kapat("cit");
        }

        /// <summary>Ot tutami: birkac ince yaprak. Zemine doku hissi verir.</summary>
        public static Mesh OtTutami(int tohum)
        {
            var y = new Yapici();
            var rastgele = new System.Random(tohum);
            int adet = 3 + rastgele.Next(3);
            for (int i = 0; i < adet; i++)
            {
                float aci = (float)(rastgele.NextDouble() * Mathf.PI * 2f);
                float mesafe = (float)rastgele.NextDouble() * 0.18f;
                float boy = 0.16f + (float)rastgele.NextDouble() * 0.16f;
                Vector3 dip = new Vector3(Mathf.Cos(aci) * mesafe, 0f, Mathf.Sin(aci) * mesafe);
                Vector3 uc = dip + new Vector3(Mathf.Cos(aci) * 0.07f, boy, Mathf.Sin(aci) * 0.07f);
                Vector3 genislik = new Vector3(-Mathf.Sin(aci), 0f, Mathf.Cos(aci)) * 0.035f;
                y.UcgenCiftYuz(dip - genislik, dip + genislik, uc, Palet.Yaprak);
            }
            return y.Kapat("ot");
        }

        /// <summary>Duz yatay yama. Zemin renk cesitlemesi icin (tek ton cim olu duruyor).</summary>
        public static Mesh Yama(float genislik, float derinlik, Color renk)
        {
            var y = new Yapici();
            float gx = genislik / 2f, gz = derinlik / 2f;
            y.DortGenYonlu(
                new Vector3(-gx, 0, -gz), new Vector3(gx, 0, -gz),
                new Vector3(gx, 0, gz), new Vector3(-gx, 0, gz),
                Vector3.up, renk);
            return y.Kapat("yama");
        }

        // Koni ve Silindir'i baska bir Yapici'ya eklemek icin (bilesik nesneler).
        private static void KoniEkle(
            Yapici y, Vector3 taban, float yaricap, float yukseklik, int dilim, Color renk)
        {
            Vector3 tepe = taban + Vector3.up * yukseklik;
            for (int i = 0; i < dilim; i++)
            {
                float a0 = (i / (float)dilim) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)dilim) * Mathf.PI * 2f;
                Vector3 p0 = taban + new Vector3(Mathf.Cos(a0) * yaricap, 0, Mathf.Sin(a0) * yaricap);
                Vector3 p1 = taban + new Vector3(Mathf.Cos(a1) * yaricap, 0, Mathf.Sin(a1) * yaricap);
                float orta = (a0 + a1) * 0.5f;
                float golge = 0.86f + 0.14f * Mathf.Abs(Mathf.Cos(orta));
                y.UcgenYonlu(p0, p1, tepe, new Vector3(Mathf.Cos(orta), 0f, Mathf.Sin(orta)), renk * golge);
            }
        }

        private static void SilindirEkle(
            Yapici y, Vector3 taban, float yaricap, float yukseklik, int dilim, Color renk)
        {
            for (int i = 0; i < dilim; i++)
            {
                float a0 = (i / (float)dilim) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)dilim) * Mathf.PI * 2f;
                Vector3 p0 = taban + new Vector3(Mathf.Cos(a0) * yaricap, 0, Mathf.Sin(a0) * yaricap);
                Vector3 p1 = taban + new Vector3(Mathf.Cos(a1) * yaricap, 0, Mathf.Sin(a1) * yaricap);
                Vector3 u0 = p0 + Vector3.up * yukseklik;
                Vector3 u1 = p1 + Vector3.up * yukseklik;
                float orta = (a0 + a1) * 0.5f;
                float golge = 0.84f + 0.16f * Mathf.Abs(Mathf.Cos(orta));
                Vector3 disYon = new Vector3(Mathf.Cos(orta), 0f, Mathf.Sin(orta));
                y.DortGenYonlu(p0, p1, u1, u0, disYon, renk * golge);
                y.UcgenYonlu(u0, u1, taban + Vector3.up * yukseklik, Vector3.up, renk);
            }
        }

        /// <summary>Zemin: tek buyuk dortgen. Karo izgarasi ayri cizilir.</summary>
        public static Mesh Zemin(float genislik, float derinlik, Color renk)
        {
            var y = new Yapici();
            float gx = genislik / 2f, gz = derinlik / 2f;
            y.DortGenYonlu(
                new Vector3(-gx, 0, -gz),
                new Vector3(gx, 0, -gz),
                new Vector3(gx, 0, gz),
                new Vector3(-gx, 0, gz),
                Vector3.up,
                renk);
            return y.Kapat("zemin");
        }

        /// <summary>Karo vurgusu: ince cerceve. Dokunulabilir karoyu parlatmak icin (ogretici).</summary>
        public static Mesh Cerceve(float boyut, float kalinlik, Color renk)
        {
            var y = new Yapici();
            float d = boyut / 2f;
            float i = d - kalinlik;
            const float h = 0.02f;

            void Serit(Vector3 a, Vector3 b, Vector3 c, Vector3 dd) =>
                y.DortGenYonlu(a, b, c, dd, Vector3.up, renk);

            Serit(new Vector3(-d, h, -d), new Vector3(d, h, -d), new Vector3(d, h, -i), new Vector3(-d, h, -i));
            Serit(new Vector3(-d, h, i), new Vector3(d, h, i), new Vector3(d, h, d), new Vector3(-d, h, d));
            Serit(new Vector3(-d, h, -i), new Vector3(-i, h, -i), new Vector3(-i, h, i), new Vector3(-d, h, i));
            Serit(new Vector3(i, h, -i), new Vector3(d, h, -i), new Vector3(d, h, i), new Vector3(i, h, i));
            return y.Kapat("cerceve");
        }
    }
}
