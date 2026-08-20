using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Amorland.Gorsel;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// Dunya mesh'lerinin BASSIZ denetimi — yuz yonu (winding) dogru mu?
    ///
    /// Neden gerekli: Unity arka yuzleri kirpar. Yukari bakmasi gereken bir yuzeyin
    /// ucgenleri ters sarilmissa nesne SESSIZCE gorunmez olur — hata yok, uyari yok,
    /// sadece bos ekran. Zemin, tarla karosu ve ogretici vurgusu tam olarak bu yuzden
    /// gorunmuyordu; oyun "calisiyor" ama ortada dunya yoktu.
    ///
    /// Denetim her mesh'i uretir, ucgen normallerini alan agirlikli olarak toplar ve
    /// beklenen yone bakip bakmadigini soyler.
    ///
    /// Calistirma:
    ///   Unity -batchmode -nographics -projectPath &lt;proje&gt; \
    ///         -executeMethod Amorland.EditorAraclari.DunyaDenetimi.Denetle -quit
    ///
    /// Cikis kodu 0 temiz, 1 hatali.
    /// </summary>
    public static class DunyaDenetimi
    {
        private static readonly List<string> _hatalar = new List<string>();
        private static int _gecen;

        /// <summary>Yatay bir yuzeyin "yukari bakiyor" sayilmasi icin gereken en az oran.</summary>
        private const float UstYuzEsigi = 0.30f;

        [MenuItem("Amorland/Dunya Denetimi")]
        public static void Denetle()
        {
            _hatalar.Clear();
            _gecen = 0;

            Debug.Log("[Amorland] Dunya mesh denetimi basliyor.");

            // Zemin, karo ve vurgu YUKARI bakan yuzeyi olan nesnelerdir. Oyuncu bunlara
            // tepeden bakar; ust yuzleri gorunmuyorsa dunya bos gorunur.
            UstYuzuVar("Zemin", MeshUretici.Zemin(40f, 40f, Palet.Cim));
            UstYuzuVar("TarlaKarosu", MeshUretici.TarlaKarosu(Palet.Toprak));
            UstYuzuVar("Cerceve", MeshUretici.Cerceve(1f, 0.06f, Palet.UiVurgu));
            UstYuzuVar("Disk", MeshUretici.Disk(0.45f, 10, Palet.Golge));
            UstYuzuVar("Kutu", MeshUretici.Kutu(new Vector3(1f, 1f, 1f), Palet.Ahsap));
            UstYuzuVar("Kuyu", MeshUretici.Kuyu());
            UstYuzuVar("Ev", MeshUretici.Ev());
            UstYuzuVar("Karakter", MeshUretici.Karakter(Palet.Cati, Color.white));
            UstYuzuVar("Koni", MeshUretici.Koni(0.5f, 1f, 8, Palet.Yaprak));
            UstYuzuVar("Silindir", MeshUretici.Silindir(0.3f, 1f, 8, Palet.Ahsap));

            for (int asama = 0; asama < 4; asama++)
            {
                UstYuzuVar("Urun(asama " + asama + ")", MeshUretici.Urun(asama, 4, Palet.Yaprak));
            }
            foreach (string tur in new[] { "cali", "tas", "agac", "ot" })
            {
                var m = MeshUretici.YabaniKaynak(tur);
                if (m == null) { Ekle("YabaniKaynak(" + tur + ") null dondu"); continue; }
                UstYuzuVar("YabaniKaynak(" + tur + ")", m);
            }

            Debug.Log("[Amorland] " + _gecen + " denetim gecti, " + _hatalar.Count + " hata.");
            foreach (string h in _hatalar) Debug.LogError("[Amorland] HATA: " + h);

            if (Application.isBatchMode) EditorApplication.Exit(_hatalar.Count == 0 ? 0 : 1);
        }

        /// <summary>
        /// Mesh'in kameraya donuk (yukari bakan) yuzey alani var mi?
        ///
        /// Alan agirlikli bakilir: tek bir minik ucgenin yukari bakmasi yeterli degil,
        /// nesnenin ust yuzu gercekten gorunur buyuklukte olmali.
        /// </summary>
        private static void UstYuzuVar(string ad, Mesh mesh)
        {
            if (mesh == null) { Ekle(ad + " mesh null"); return; }

            Vector3[] k = mesh.vertices;
            int[] u = mesh.triangles;
            if (u.Length < 3) { Ekle(ad + " ucgensiz"); return; }

            float yukari = 0f, asagi = 0f, toplam = 0f;

            for (int i = 0; i < u.Length; i += 3)
            {
                Vector3 a = k[u[i]], b = k[u[i + 1]], c = k[u[i + 2]];
                // Unity'nin on yuz normali: cross(b-a, c-a).
                Vector3 n = Vector3.Cross(b - a, c - a);
                float alan = n.magnitude * 0.5f;
                if (alan <= 1e-7f) continue;
                toplam += alan;
                float egim = n.normalized.y;
                if (egim > 0.5f) yukari += alan;
                else if (egim < -0.5f) asagi += alan;
            }

            if (toplam <= 0f) { Ekle(ad + " butun ucgenleri bozuk (sifir alan)"); return; }

            float yukariOran = yukari / toplam;
            float asagiOran = asagi / toplam;

            if (yukariOran < UstYuzEsigi && asagiOran > yukariOran)
            {
                Ekle(ad + " ust yuzu TERS sarilmis — yukari bakan alan %"
                     + Mathf.RoundToInt(yukariOran * 100f) + ", asagi bakan %"
                     + Mathf.RoundToInt(asagiOran * 100f)
                     + ". Unity arka yuzu kirptigi icin nesne tepeden GORUNMEZ.");
                return;
            }

            _gecen++;
        }

        private static void Ekle(string mesaj) => _hatalar.Add(mesaj);

        /// <summary>
        /// Tani amacli tam dokum: her mesh icin yukari/asagi/yan alan yuzdeleri.
        /// Duzeltmeden once neyin ters oldugunu gormek icin.
        /// </summary>
        [MenuItem("Amorland/Dunya Normal Dokumu")]
        public static void Dokum()
        {
            var sb = new StringBuilder();
            sb.AppendLine("mesh | yukari% | asagi% | yan% | hacim");

            void Satir(string ad, Mesh mesh)
            {
                if (mesh == null) { sb.AppendLine(ad + " | NULL"); return; }
                Vector3[] k = mesh.vertices;
                int[] u = mesh.triangles;
                float yukari = 0f, asagi = 0f, yan = 0f, toplam = 0f, hacim = 0f;
                for (int i = 0; i < u.Length; i += 3)
                {
                    Vector3 a = k[u[i]], b = k[u[i + 1]], c = k[u[i + 2]];
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    // Isaretli hacim (diverjans teoremi). Kapali bir mesh'te yuzler DISA
                    // bakiyorsa pozitif, ICE bakiyorsa negatif cikar. Yan yuzlerin yonunu
                    // olcmenin tek guvenilir yolu bu; normalin y bileseni yan yuzde susar.
                    hacim += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
                    float alan = n.magnitude * 0.5f;
                    if (alan <= 1e-7f) continue;
                    toplam += alan;
                    float e = n.normalized.y;
                    if (e > 0.5f) yukari += alan;
                    else if (e < -0.5f) asagi += alan;
                    else yan += alan;
                }
                if (toplam <= 0f) { sb.AppendLine(ad + " | SIFIR ALAN"); return; }
                sb.AppendLine(ad + " | " + Mathf.RoundToInt(yukari / toplam * 100f)
                              + " | " + Mathf.RoundToInt(asagi / toplam * 100f)
                              + " | " + Mathf.RoundToInt(yan / toplam * 100f)
                              + " | " + hacim.ToString("F3"));
            }

            Satir("Zemin", MeshUretici.Zemin(40f, 40f, Palet.Cim));
            Satir("TarlaKarosu", MeshUretici.TarlaKarosu(Palet.Toprak));
            Satir("Cerceve", MeshUretici.Cerceve(1f, 0.06f, Palet.UiVurgu));
            Satir("Disk", MeshUretici.Disk(0.45f, 10, Palet.Golge));
            Satir("Kutu", MeshUretici.Kutu(new Vector3(1f, 1f, 1f), Palet.Ahsap));
            Satir("Kuyu", MeshUretici.Kuyu());
            Satir("Ev", MeshUretici.Ev());
            Satir("Karakter", MeshUretici.Karakter(Palet.Cati, Color.white));
            Satir("Koni", MeshUretici.Koni(0.5f, 1f, 8, Palet.Yaprak));
            Satir("Silindir", MeshUretici.Silindir(0.3f, 1f, 8, Palet.Ahsap));
            for (int a = 0; a < 4; a++) Satir("Urun" + a, MeshUretici.Urun(a, 4, Palet.Yaprak));
            foreach (string t in new[] { "cali", "tas", "agac", "ot" })
                Satir("Yabani_" + t, MeshUretici.YabaniKaynak(t));

            Debug.Log("[Amorland] NORMAL DOKUMU\n" + sb);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
