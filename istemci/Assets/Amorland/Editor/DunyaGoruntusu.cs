using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Amorland.Ag;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// 2.5D oyun dunyasinin PNG goruntusunu BASSIZ uretir.
    ///
    /// Dunya artik VADIDIR: Oyun.SahneKur ile ayni kurulum tekrarlanir —
    /// VadiHaritasi (oyunAlani modunda, OyunCapa kaydirmasiyla), TarlaGorunumu
    /// (hucre-merkez sozlesmesi, -0.5 kaydirma), poncik koylu sprite'lari ve
    /// Y-sort kamerasi. Sunucu yokken parseller SAHTE DEMO DURUMLA doldurulur:
    /// filiz/genc/olgun evreleri, islak/kuru zemin, yabani dugumler ve partner
    /// parseli tek karede denetlenebilir.
    ///
    /// Calistirma:
    ///   Unity -batchmode -projectPath &lt;proje&gt; \
    ///         -executeMethod Amorland.EditorAraclari.DunyaGoruntusu.Uret \
    ///         -dengeDizin &lt;repo&gt;/sunucu/src/main/resources/denge -cikti &lt;klasor&gt; -quit
    ///
    /// ⚠ <c>-nographics</c> KULLANMA: render icin grafik aygiti gerekir.
    /// ⚠ Denge dizini yoksa uyari verir ve buyume hesaplanamadigi icin tum
    ///   ekinler filiz cizilir — kare yine uretilir, denetim degeri dusuktur.
    /// </summary>
    public static class DunyaGoruntusu
    {
        private const int Genislik = 1280;
        private const int Yukseklik = 720;

        private static readonly List<string> _hatalar = new List<string>();
        private static int _uretilen;

        [MenuItem("Amorland/Dunya Goruntusu Uret")]
        public static void Uret()
        {
            _hatalar.Clear();
            _uretilen = 0;

            string cikti = Arguman("-cikti");
            if (string.IsNullOrEmpty(cikti)) cikti = Path.Combine(Path.GetTempPath(), "amorland-dunya");
            Directory.CreateDirectory(cikti);

            DengeVerisi denge = DengeYukle();
            DunyaDurumu durum = DemoDurum(denge);

            float yakin = denge.Yuklendi ? denge.ZoomYakin : 5f;
            float uzak = denge.Yuklendi ? denge.ZoomUzak : 8f;

            // Karakterin durdugu yer (Oyun.cs ile ayni baslangic konumu).
            Vector3 oyuncu = new Vector3(1f, -2f, 0f);

            Kare(cikti, "01-oyun-yakin.png", durum, denge, oyuncu, yakin);
            Kare(cikti, "02-oyun-uzak.png", durum, denge, oyuncu, uzak);
            // Genis bakis: iki parsel, kuyu ve dugumler tek karede.
            Kare(cikti, "03-oyun-genis.png", durum, denge, new Vector3(4f, 1f, 0f), 12f);
            // Vadi baglami: temizlenmis oyun alaninin vadiye oturusu.
            Kare(cikti, "04-oyun-vadi-baglam.png", durum, denge, new Vector3(4f, 2f, 0f), 26f);
            // Y-sort kaniti: kuyunun kuzeyindeki karakter govdesi kuyu ARKASINDA.
            Kare(cikti, "06-ysort-kuyu.png", durum, denge, new Vector3(-4f, 1.5f, 0f), 3f);
            // Cit kose kaniti: tarla bolgesinin guneybati kosesi (oyuncu bulgusu
            // "citlerin kosesi kose seklinde baglanmamis" dogrulamasi).
            Kare(cikti, "05-cit-kose.png", durum, denge, new Vector3(-42f, -20f, 0f), 5f);
            // Orman kesim sahasi: dekordan arinmis aciklik + oynanabilir agaclar.
            Kare(cikti, "07-orman-kesim.png", durum, denge, new Vector3(-52f, 101f, 0f), 9f);
            // Bolge denetim kareleri (oyuncu talebi: "haritanin diger yerlerini
            // elden gecir") — her bolge kendi kadrajinda taranir.
            Kare(cikti, "08-maden.png", durum, denge, new Vector3(-131f, -16f, 0f), 14f);
            Kare(cikti, "09-ev.png", durum, denge, new Vector3(15f, 85f, 0f), 10f);
            Kare(cikti, "10-depo.png", durum, denge, new Vector3(135f, 83f, 0f), 12f);
            Kare(cikti, "11-market.png", durum, denge, new Vector3(231f, 45f, 0f), 14f);
            Kare(cikti, "12-gar.png", durum, denge, new Vector3(152f, -48f, 0f), 12f);
            Kare(cikti, "13-gol.png", durum, denge, new Vector3(108f, 152f, 0f), 18f);
            Kare(cikti, "14-vadi-tam.png", durum, denge, new Vector3(42f, 72f, 0f), 162f);

            Debug.Log("[Amorland] " + _uretilen + " dunya karesi uretildi, " + _hatalar.Count
                      + " hata. Klasor: " + cikti);
            foreach (string h in _hatalar) Debug.LogError("[Amorland] KARE HATASI: " + h);

            if (Application.isBatchMode) EditorApplication.Exit(_hatalar.Count == 0 ? 0 : 1);
        }

        // ------------------------------------------------------------------ kare

        private static void Kare(
            string klasor, string dosya, DunyaDurumu durum, DengeVerisi denge,
            Vector3 merkez, float zoom)
        {
            Camera cam = null;
            RenderTexture rt = null;
            var cop = new List<GameObject>();

            try
            {
                rt = new RenderTexture(Genislik, Yukseklik, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 8
                };
                rt.Create();

                var camGo = new GameObject("DunyaKamera");
                cop.Add(camGo);
                cam = camGo.AddComponent<Camera>();

                // KameraYoneticisi.Kur ile AYNI kurulum. Farkli olsaydi goruntu
                // oyunda gorunenden baska bir sey gosterir, denetim degeri kalmazdi.
                cam.orthographic = true;
                cam.orthographicSize = zoom;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Palet.Gokyuzu;
                cam.transparencySortMode = TransparencySortMode.CustomAxis;
                cam.transparencySortAxis = Vector3.up;
                cam.targetTexture = rt;
                camGo.transform.position = new Vector3(merkez.x, merkez.y, -10f);

                // Vadi: Oyun.SahneKur ile ayni kaydirma sozlesmesi.
                var vadiGo = new GameObject("Vadi");
                cop.Add(vadiGo);
                vadiGo.transform.position = new Vector3(
                    -(VadiHaritasi.OyunCapaX + 0.5f), -(VadiHaritasi.OyunCapaY + 0.5f), 0f);
                var vadi = vadiGo.AddComponent<VadiHaritasi>();
                vadi.Kur(oyunAlani: true);

                var tarlaGo = new GameObject("TarlaGorunumu");
                cop.Add(tarlaGo);
                tarlaGo.transform.position = new Vector3(-0.5f, -0.5f, 0f);
                var tarla = tarlaGo.AddComponent<TarlaGorunumu>();
                tarla.Kur(durum, denge);

                int kesAgac = 0;
                foreach (var t in tarlaGo.GetComponentsInChildren<Transform>())
                {
                    if (t.name.StartsWith("KesAgac_")) kesAgac++;
                }
                Debug.Log("[Amorland] " + dosya + " KesAgac sayisi: " + kesAgac);

                KarakterKur(cop, kadin: false, renk: 0, kare: 0, new Vector3(1f, -2f, 0f));
                // Partner temsili: kendi rengi (gokyuzu), durus karesinde —
                // hareketsiz karakterin adim ortasinda donmasi dogal durmuyordu.
                KarakterKur(cop, kadin: true, renk: 2, kare: 0, new Vector3(10f, 5f, 0f));
                // Y-sort dogrulamasi: kuyunun (-4,1) hemen kuzeyindeki karakter
                // govdesi kuyu tarafindan kismen ortulmeli (05. kare bunu buyutur).
                // Yarim karo yaklastirilir: tam karo mesafede sprite'lar bitisir
                // ama ortusmez, Y-sort kaniti ancak ortusmede gorunur.
                KarakterKur(cop, kadin: false, renk: 1, kare: 0, new Vector3(-4f, 1.05f, 0f));

                cam.Render();

                RenderTexture onceki = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(Genislik, Yukseklik, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, Genislik, Yukseklik), 0, 0);
                tex.Apply();
                RenderTexture.active = onceki;

                File.WriteAllBytes(Path.Combine(klasor, dosya), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                _uretilen++;
                Debug.Log("[Amorland] Yazildi: " + dosya);
            }
            catch (Exception e)
            {
                _hatalar.Add(dosya + ": " + e.GetType().Name + " — " + e.Message);
                Debug.LogWarning("[Amorland] " + dosya + " cizilemedi: " + e);
            }
            finally
            {
                foreach (var go in cop) if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (cam != null) cam.targetTexture = null;
                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }
            }
        }

        /// <summary>
        /// Karakter defilesi: tum oyuncu varyantlari (4 renk x 2 cinsiyet x 3 kare)
        /// + koy NPC ciftini tek karede, cim zemin uzerinde 4x buyuklukte cizer.
        /// Sprite uretecindeki bir gerileme en hizli burada yakalanir.
        /// </summary>
        [MenuItem("Amorland/Karakter Defilesi Uret")]
        public static void KarakterDefilesi()
        {
            _hatalar.Clear();
            _uretilen = 0;
            string cikti = Arguman("-cikti");
            if (string.IsNullOrEmpty(cikti)) cikti = Path.Combine(Path.GetTempPath(), "amorland-dunya");
            Directory.CreateDirectory(cikti);

            Camera cam = null;
            RenderTexture rt = null;
            var cop = new List<GameObject>();
            try
            {
                rt = new RenderTexture(Genislik, Yukseklik, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 8
                };
                rt.Create();
                var camGo = new GameObject("DefileKamera");
                cop.Add(camGo);
                cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 4.4f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 50f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.42f, 0.60f, 0.31f);
                cam.targetTexture = rt;
                camGo.transform.position = new Vector3(0f, 0f, -10f);

                // 5 satir: 4 cift rengi + NPC cifti. 6 sutun: erkek d/A/B + kadin d/A/B.
                for (int renk = 0; renk < 4; renk++)
                {
                    for (int c = 0; c < 6; c++)
                    {
                        bool kadin = c >= 3;
                        int kare = c % 3;
                        var konum = new Vector3((c - 2.5f) * 1.5f, (1.5f - renk) * 1.75f, 0f);
                        KarakterKur(cop, kadin, renk, kare, konum);
                    }
                }
                NpcKur(cop, kadin: false, new Vector3(-0.75f, -3.6f, 0f));
                NpcKur(cop, kadin: true, new Vector3(0.75f, -3.6f, 0f));

                cam.Render();
                RenderTexture onceki = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(Genislik, Yukseklik, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, Genislik, Yukseklik), 0, 0);
                tex.Apply();
                RenderTexture.active = onceki;
                File.WriteAllBytes(Path.Combine(cikti, "05-karakter-defilesi.png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                _uretilen++;
                Debug.Log("[Amorland] Yazildi: 05-karakter-defilesi.png");
            }
            catch (Exception e)
            {
                _hatalar.Add("defile: " + e.GetType().Name + " — " + e.Message);
                Debug.LogWarning("[Amorland] Defile cizilemedi: " + e);
            }
            finally
            {
                foreach (var go in cop) if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (cam != null) cam.targetTexture = null;
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            }

            if (Application.isBatchMode) EditorApplication.Exit(_hatalar.Count == 0 ? 0 : 1);
        }

        private static void NpcKur(List<GameObject> cop, bool kadin, Vector3 konum)
        {
            SpriteNesnesi(cop, Karo64.Al(
                Sayfa64.Ek, kadin ? Karo64.KoyluKadin : Karo64.KoyluErkek), konum);
        }

        /// <summary>Oyuncu gorseli: Oyun.KarakterSpriteKur'un aynisi (golge + koylu).</summary>
        private static void KarakterKur(List<GameObject> cop, bool kadin, int renk, int kare, Vector3 konum)
        {
            SpriteNesnesi(cop, Karo64.Al(Sayfa64.Ek, Karo64.Oyuncu(kadin, renk, kare)), konum);
        }

        private static void SpriteNesnesi(List<GameObject> cop, Sprite sprite, Vector3 konum)
        {
            var kok = new GameObject("Karakter");
            cop.Add(kok);
            kok.transform.position = konum;

            var golgeGo = new GameObject("Golge");
            golgeGo.transform.SetParent(kok.transform, false);
            // Golge elipsi dokunun alt yarisinda: +0.20 cocuk = ayak cizgisi (Oyun ile ayni).
            golgeGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            golgeGo.transform.localScale = new Vector3(0.95f, 0.6f, 1f);
            var gsr = golgeGo.AddComponent<SpriteRenderer>();
            gsr.sprite = Karo64.Al(Sayfa64.Ek, Karo64.Golge);
            gsr.sortingOrder = 1;

            var gorselGo = new GameObject("KarakterGorsel");
            gorselGo.transform.SetParent(kok.transform, false);
            gorselGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var sr = gorselGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;
        }

        // ------------------------------------------------------------------ veri

        /// <summary>
        /// Dengeyi sunucunun sundugu bicimde yerelden derler: /denge cevabi
        /// { dosyaAdi: dosyaIcerigi } birlesimidir (DengeVerisi.Sayi yollari
        /// "ilerleme/kamera/..." seklinde dosya adiyla baslar).
        /// </summary>
        private static DengeVerisi DengeYukle()
        {
            var denge = new DengeVerisi();
            string dizin = Arguman("-dengeDizin");
            if (string.IsNullOrEmpty(dizin))
            {
                dizin = Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", "..", "sunucu", "src", "main", "resources", "denge"));
            }
            if (!Directory.Exists(dizin))
            {
                Debug.LogWarning("[Amorland] Denge dizini yok (" + dizin
                                 + ") — buyume hesaplanamaz, ekinler filiz cizilir.");
                return denge;
            }

            var kok = new JObject();
            foreach (string yol in Directory.GetFiles(dizin, "*.json"))
            {
                kok[Path.GetFileNameWithoutExtension(yol)] = JToken.Parse(File.ReadAllText(yol));
            }
            denge.Yukle(kok.ToString());
            Debug.Log("[Amorland] Denge yerelden derlendi: " + dizin);
            return denge;
        }

        /// <summary>
        /// Sahte demo durum: her buyume evresi + islak/kuru zemin + partner parseli
        /// + 3 yabani dugum. Sunucu gerektirmez; gorsel denetim icindir.
        /// </summary>
        private static DunyaDurumu DemoDurum(DengeVerisi denge)
        {
            var durum = new DunyaDurumu { BenimId = "demo-ben" };
            DateTime simdi = durum.SunucuZamani;

            long Buyume(string urunId)
            {
                var u = denge.Urun(urunId);
                return u != null && u.BuyumeSn > 0 ? u.BuyumeSn : 600;
            }

            void KaroEkle(string sahip, int x, int y, string urun, double sulanmaOrani)
            {
                var k = new Karo
                {
                    ParselSahibi = sahip,
                    X = x,
                    Y = y,
                    UrunId = urun,
                    EkildiAt = simdi.AddMinutes(-30),
                    SulandiAt = sulanmaOrani < 0
                        ? (DateTime?)null
                        : simdi.AddSeconds(-Buyume(urun) * sulanmaOrani),
                    HasatSayaci = 0
                };
                durum.Karolar[k.Anahtar] = k;
            }

            // Orman kesim sahasi agaclari (sunucu yerlesiminin aynasi:
            // turetilmis.json ormanKesimSahasi x0=-62 y0=96, 4 karo ara).
            for (int i = 0; i < 10; i++)
            {
                var a = new Amorland.Ag.Dugum
                {
                    Bolge = "orman",
                    X = -62 + (i % 6) * 4,
                    Y = 96 + (i / 6) * 4,
                    Tur = "cam",
                    Sinif = "agac",
                    Saglik = 3,
                    MaksSaglik = 3
                };
                durum.Dugumler[a.Anahtar] = a;
            }

            // Benim parsel: filiz (kuru) / filiz (yeni islak) / genc / olgun.
            KaroEkle("demo-ben", 0, 0, "bugday", -1);
            KaroEkle("demo-ben", 1, 0, "domates", 0.05);
            KaroEkle("demo-ben", 2, 0, "havuc", 0.6);
            KaroEkle("demo-ben", 3, 0, "kabak", 1.5);
            KaroEkle("demo-ben", 4, 1, "turp", 1.5);
            KaroEkle("demo-ben", 0, 2, "misir", 0.6);

            // Partner parseli (+8 kaydirmayla cizilir).
            KaroEkle("demo-es", 0, 0, "domates", 1.5);
            KaroEkle("demo-es", 1, 1, "bugday", 0.6);

            // Yabani kaynaklar: tarla kenari (dal x2 + yem otu, sunucu yerlesimi).
            for (int i = 0; i < 3; i++)
            {
                var d = new Dugum
                {
                    Bolge = "tarla_kenari",
                    X = -2,
                    Y = i,
                    Tur = i == 2 ? "yem_otu" : "dal",
                    Sinif = "yabani"
                };
                durum.Dugumler[d.Anahtar] = d;
            }
            return durum;
        }

        private static string Arguman(string ad)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ad) return args[i + 1];
            }
            return null;
        }
    }
}
