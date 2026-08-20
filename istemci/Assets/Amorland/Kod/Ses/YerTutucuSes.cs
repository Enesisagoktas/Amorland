using UnityEngine;

namespace Amorland.Ses
{
    /// <summary>
    /// Yer tutucu ses uretimi.
    ///
    /// ⚠ B.23 acikca soyluyor: <b>organik sesler (su, toprak, ahsap) sentezle iyi cikmaz.</b>
    /// Buradaki sesler "guzel" olmayi hedeflemiyor; hedefleri tek: <b>ses altyapisinin
    /// dikey dilimde CALISTIGINI kanitlamak</b> (B.26 yasak 19 — altyapiyi sonraya birakmak
    /// 92 sesin hepsini yeniden elden gecirmek demek).
    ///
    /// Gercek sesler telefonla kaydedilecek (A.5): sulama = kovadan legene su dok,
    /// balta = tahtaya tahta vur, her sesin 5-6 tekrari.
    ///
    /// Uslup sozlesmesine uyulur: eylem sesi 300 ms altinda, keskin transient yok
    /// (kozy = yumusak atak), alcak frekans agirlikli.
    /// </summary>
    public static class YerTutucuSes
    {
        private const int OrneklemeHizi = 22050;

        /// <summary>Bir etiket icin varyasyon uretir. Varyasyon indeksi tohum olur.</summary>
        public static AudioClip Uret(SesEtiketi etiket, int varyasyon)
        {
            switch (etiket)
            {
                case SesEtiketi.Ekme:
                case SesEtiketi.Toplama:
                    return Gurultu("ses_" + etiket + varyasyon, 0.11f, 900f, 0.45f, varyasyon);

                case SesEtiketi.Sulama:
                case SesEtiketi.Kova:
                    return Gurultu("ses_" + etiket + varyasyon, 0.22f, 1600f, 0.35f, varyasyon);

                case SesEtiketi.Hasat:
                    return Gurultu("ses_" + etiket + varyasyon, 0.14f, 1200f, 0.5f, varyasyon);

                case SesEtiketi.Kazma:
                case SesEtiketi.KazmaTas:
                case SesEtiketi.Balta:
                case SesEtiketi.DugumKirilma:
                    return Vurus("ses_" + etiket + varyasyon, 0.16f, 180f, varyasyon);

                case SesEtiketi.AletKirilma:
                    return Vurus("ses_" + etiket + varyasyon, 0.28f, 120f, varyasyon);

                case SesEtiketi.AyakToprak:
                case SesEtiketi.AyakCim:
                case SesEtiketi.AyakTahta:
                case SesEtiketi.AyakTas:
                    return Gurultu("ses_" + etiket + varyasyon, 0.07f, 700f, 0.30f, varyasyon);

                case SesEtiketi.SeviyeAtlama:
                    return Ezgi("ses_seviye" + varyasyon, new[] { 523.25f, 659.25f, 783.99f }, 0.16f);

                case SesEtiketi.DefterSayfasi:
                    return Gurultu("ses_defter" + varyasyon, 0.30f, 2200f, 0.28f, varyasyon);

                case SesEtiketi.OrtakProjeTamam:
                    return Ezgi("ses_ortak" + varyasyon, new[] { 392f, 523.25f, 659.25f, 783.99f }, 0.22f);

                case SesEtiketi.TrenGelisi:
                    return Ezgi("ses_tren" + varyasyon, new[] { 174.61f, 220f, 174.61f, 146.83f }, 0.9f);

                case SesEtiketi.UiDokunma:
                    return Ton("ses_ui_dokun" + varyasyon, 0.05f, 660f, 0.25f);

                case SesEtiketi.UiAcilis:
                    return Ton("ses_ui_ac" + varyasyon, 0.09f, 520f, 0.25f);

                case SesEtiketi.UiKapanis:
                    return Ton("ses_ui_kapa" + varyasyon, 0.09f, 390f, 0.25f);

                case SesEtiketi.UiHata:
                    return Ton("ses_ui_hata" + varyasyon, 0.12f, 196f, 0.35f);

                case SesEtiketi.UiOnay:
                    return Ezgi("ses_ui_onay" + varyasyon, new[] { 587.33f, 880f }, 0.08f);

                case SesEtiketi.OrtamTarla:
                case SesEtiketi.OrtamEv:
                    return OrtamDongusu("ses_" + etiket, 6f, varyasyon);

                default:
                    return Ton("ses_" + etiket + varyasyon, 0.08f, 440f, 0.2f);
            }
        }

        // ------------------------------------------------------------------ uretecler

        /// <summary>Alcak geciren filtreli gurultu. Toprak, su, kagit gibi genis bantli sesler.</summary>
        private static AudioClip Gurultu(string ad, float sure, float kesim, float genlik, int tohum)
        {
            int n = Mathf.Max(1, (int)(sure * OrneklemeHizi));
            var veri = new float[n];
            var rnd = new System.Random(1000 + tohum * 7919);
            float onceki = 0f;
            float katsayi = Mathf.Clamp01(kesim / (OrneklemeHizi * 0.5f));

            for (int i = 0; i < n; i++)
            {
                float ham = (float)(rnd.NextDouble() * 2.0 - 1.0);
                onceki += katsayi * (ham - onceki);   // tek kutuplu alcak geciren
                float zarf = Zarf(i / (float)n, 0.15f);
                veri[i] = onceki * zarf * genlik;
            }
            return Klip(ad, veri);
        }

        /// <summary>Sonumlenen dusuk frekansli vurus. Tas, tahta, kirilma.</summary>
        private static AudioClip Vurus(string ad, float sure, float frekans, int tohum)
        {
            int n = Mathf.Max(1, (int)(sure * OrneklemeHizi));
            var veri = new float[n];
            var rnd = new System.Random(2000 + tohum * 104729);
            float sapma = 1f + (float)(rnd.NextDouble() - 0.5) * 0.12f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)OrneklemeHizi;
                float sonum = Mathf.Exp(-t * 26f);
                float ton = Mathf.Sin(2f * Mathf.PI * frekans * sapma * t);
                float carp = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.35f;
                veri[i] = (ton * 0.75f + carp) * sonum * 0.5f;
            }
            return Klip(ad, veri);
        }

        private static AudioClip Ton(string ad, float sure, float frekans, float genlik)
        {
            int n = Mathf.Max(1, (int)(sure * OrneklemeHizi));
            var veri = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)OrneklemeHizi;
                float zarf = Zarf(i / (float)n, 0.2f);
                // Ucuncu harmonik hafif katilir: saf sinus "test tonu" gibi duyuluyor.
                float s = Mathf.Sin(2f * Mathf.PI * frekans * t)
                          + 0.18f * Mathf.Sin(2f * Mathf.PI * frekans * 3f * t);
                veri[i] = s * zarf * genlik * 0.7f;
            }
            return Klip(ad, veri);
        }

        private static AudioClip Ezgi(string ad, float[] notalar, float notaSuresi)
        {
            int notaOrnek = Mathf.Max(1, (int)(notaSuresi * OrneklemeHizi));
            var veri = new float[notaOrnek * notalar.Length];
            for (int k = 0; k < notalar.Length; k++)
            {
                for (int i = 0; i < notaOrnek; i++)
                {
                    float t = i / (float)OrneklemeHizi;
                    float zarf = Zarf(i / (float)notaOrnek, 0.25f);
                    veri[k * notaOrnek + i] =
                        Mathf.Sin(2f * Mathf.PI * notalar[k] * t) * zarf * 0.32f;
                }
            }
            return Klip(ad, veri);
        }

        /// <summary>Ortam dongusu: sifir gecisli kusursuz dongu (B.23 uslup sozlesmesi).</summary>
        private static AudioClip OrtamDongusu(string ad, float sure, int tohum)
        {
            int n = Mathf.Max(1, (int)(sure * OrneklemeHizi));
            var veri = new float[n];
            var rnd = new System.Random(3000 + tohum);
            float onceki = 0f;

            for (int i = 0; i < n; i++)
            {
                float ham = (float)(rnd.NextDouble() * 2.0 - 1.0);
                onceki += 0.004f * (ham - onceki);
                // Dongu sinirinda catirti olmasin diye ilk/son 200 ms carpraz solduruluyor.
                float t = i / (float)n;
                float kenar = Mathf.Min(1f, Mathf.Min(t, 1f - t) * 30f);
                veri[i] = onceki * 6f * kenar * 0.25f;
            }
            veri[n - 1] = 0f;
            return Klip(ad, veri);
        }

        /// <summary>Yumusak atak + sonum. Kozy = keskin transient yok (B.23).</summary>
        private static float Zarf(float t, float atakOrani)
        {
            if (t < atakOrani) return t / atakOrani;
            float kalan = (t - atakOrani) / Mathf.Max(0.0001f, 1f - atakOrani);
            return Mathf.Exp(-kalan * 4f);
        }

        private static AudioClip Klip(string ad, float[] veri)
        {
            var klip = AudioClip.Create(ad, veri.Length, 1, OrneklemeHizi, false);
            klip.SetData(veri, 0);
            return klip;
        }
    }
}
