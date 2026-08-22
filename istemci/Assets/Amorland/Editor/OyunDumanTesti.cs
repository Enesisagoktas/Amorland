using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Amorland.Durum;
using Amorland.Oynanis;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// OTOMATIK OYNANIS TESTI: oyunu gercekten oynar.
    ///
    /// Derleme, protokol testi ve render'lar tek tek yesilken bile "oyun acilir mi,
    /// ilk bes dakikasi oynanir mi" sorusu CEVAPSIZDI. Bu test o cevaptir:
    /// canli sunucuda misafir hesap acar, yenileme jetonunu birakir, Play moduna
    /// girer, karakter secer, oyuna gecer ve GERCEK etkilesim yolundan (dokunma
    /// hedefi -> yuru -> gecikmeli eylem -> niyet -> sunucu onayi) ek/sula/kova
    /// dongusunu oynar. Her adim sunucu onayiyla dogrulanir.
    ///
    /// Calistirma (klonda, sunucu acikken):
    ///   Unity -batchmode -projectPath &lt;klon&gt; \
    ///         -executeMethod Amorland.EditorAraclari.OyunDumanTesti.Calistir \
    ///         -cikti &lt;klasor&gt; -logFile ...      (⚠ -quit YOK: testi surucu kapatir)
    ///
    /// ⚠ Play'e giris domain reload yapar; surucu SessionState bayragiyla reload
    /// sonrasi kancadan devam eder. PlayerPrefs'teki gercek oturum saklanip
    /// cikista GERI KONUR — test kullanicinin kaydini ezmez.
    /// </summary>
    public static class OyunDumanTesti
    {
        private const string BayrakAnahtari = "amorland.duman";
        private const string CiktiAnahtari = "amorland.dumanCikti";
        private const string EskiOturumAnahtari = "amorland.dumanEskiOturum";
        private const string YenilemeAnahtari = "amorland.yenileme";
        private const string EskiOgreticiAnahtari = "amorland.dumanEskiOgretici";
        private const string OgreticiAnahtari = "amorland.ogretici.bitti";
        private const string EskiMektupAnahtari = "amorland.dumanEskiMektup";
        private const string MektupAnahtari = "amorland.mektup.okundu";

        // ------------------------------------------------------------- giris

        public static void Calistir()
        {
            string cikti = Arguman("-cikti");
            if (string.IsNullOrEmpty(cikti)) cikti = Path.Combine(Path.GetTempPath(), "amorland-duman");
            Directory.CreateDirectory(cikti);

            try
            {
                string yenileme = MisafirHesapAc();
                SessionState.SetString(EskiOturumAnahtari, PlayerPrefs.GetString(YenilemeAnahtari, ""));
                PlayerPrefs.SetString(YenilemeAnahtari, yenileme);
                // Test GERCEK yeni-oyuncu deneyimini oynar: zorunlu ogretici ve
                // acilis mektubu acik olmali. Kullanicinin degerleri saklanir,
                // Bitir'de geri konur.
                SessionState.SetInt(EskiOgreticiAnahtari, PlayerPrefs.GetInt(OgreticiAnahtari, 0));
                SessionState.SetInt(EskiMektupAnahtari, PlayerPrefs.GetInt(MektupAnahtari, 0));
                PlayerPrefs.DeleteKey(OgreticiAnahtari);
                PlayerPrefs.DeleteKey(MektupAnahtari);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(cikti, "duman-rapor.txt"),
                    "BASARISIZ: misafir hesap acilamadi (sunucu acik mi?): " + e.Message);
                EditorApplication.Exit(1);
                return;
            }

            SessionState.SetBool(BayrakAnahtari, true);
            SessionState.SetString(CiktiAnahtari, cikti);
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Canli sunucuda taze misafir + yas kapisi; yenileme jetonu doner.</summary>
        private static string MisafirHesapAc()
        {
            using (var http = new HttpClient { BaseAddress = new Uri("http://localhost:8080") })
            {
                // cihazId zorunlu ve benzersiz: her kosu taze misafir hesap acar.
                string govde = "{\"cihazId\":\"duman-" + Guid.NewGuid().ToString("N")
                               + "\",\"ad\":\"DumanBot\",\"renk\":1}";
                var giris = http.PostAsync("/api/oturum/misafir",
                    new StringContent(govde, Encoding.UTF8, "application/json")).Result;
                var paket = JObject.Parse(giris.Content.ReadAsStringAsync().Result);
                string erisim = (string)paket["erisimJetonu"];
                string yenileme = (string)paket["yenilemeJetonu"];
                if (string.IsNullOrEmpty(yenileme)) throw new Exception("yenileme jetonu yok: " + paket);

                var yas = new HttpRequestMessage(HttpMethod.Post, "/api/oturum/yas")
                {
                    Content = new StringContent("{\"dogumYili\":1995}", Encoding.UTF8, "application/json")
                };
                yas.Headers.Add("Authorization", "Bearer " + erisim);
                http.SendAsync(yas).Wait();
                return yenileme;
            }
        }

        // ------------------------------------------------------------- surucu

        private static readonly List<string> _rapor = new List<string>();
        private static readonly List<string> _hataLoglari = new List<string>();
        private static int _adim;
        private static double _adimBaslangici;
        private static double _sonrakiEylemZamani;
        private static bool _kancali;
        private static int _tohumBaslangic = -1;

        [InitializeOnLoadMethod]
        private static void KancaTak()
        {
            if (!SessionState.GetBool(BayrakAnahtari, false)) return;
            if (_kancali) return;
            _kancali = true;
            _adim = 0;
            _adimBaslangici = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += LogYakala;
            EditorApplication.update += Surucu;
        }

        private static void LogYakala(string mesaj, string iz, LogType tip)
        {
            if (tip != LogType.Exception && tip != LogType.Error) return;
            // ⚠ Editorun kendi ic istisnalari (SearchDatabase indeksleme vb.) oyun
            // hatasi DEGILDIR: taze Library'li klonda ilk koSuda cikabiliyor ve
            // testi bosuna dusuruyordu. Oyun istisnalarinin izinde UnityEditor olmaz.
            if (iz != null && iz.Contains("UnityEditor.")) return;
            _hataLoglari.Add(tip + ": " + mesaj);
        }

        private static void Surucu()
        {
            try
            {
                SurucuAdimi();
            }
            catch (Exception e)
            {
                Bitir(false, "surucu istisnasi: " + e);
            }
        }

        private static void SurucuAdimi()
        {
            if (!EditorApplication.isPlaying) return;
            double gecen = EditorApplication.timeSinceStartup - _adimBaslangici;
            if (gecen > 60.0)
            {
                Bitir(false, "adim " + _adim + " zaman asimi (60 sn)");
                return;
            }

            // Oynanista istisna cikti mi? (Ses klibi eksikligi gibi Error'lar
            // rapora yazilir ama testi dusurmez; Exception dusurur.)
            foreach (string h in _hataLoglari)
            {
                if (h.StartsWith("Exception"))
                {
                    Bitir(false, "oynanis istisnasi: " + h);
                    return;
                }
            }

            switch (_adim)
            {
                case 0: AdimVadi(); break;
                case 1: AdimOyunaGec(); break;
                case 2: AdimDunyaBekle(); break;
                case 3: AdimEk(); break;
                case 4: AdimEkOnayi(); break;
                case 5: AdimSula(); break;
                case 6: AdimSulaOnayi(); break;
                // Zorunlu ogretici sirasi: kuyu, toplamadan ONCE gelir.
                case 7: AdimKuyu(); break;
                case 8: AdimKovaOnayi(); break;
                case 9: AdimTopla(); break;
                case 10: AdimToplaOnayi(); break;
                case 11: AdimHasatBekle(); break;
                case 12: AdimHasatOnayi(); break;
                case 13: AdimSeviye2(); break;
                case 14: AdimDepoyaGec(); break;
                case 15: AdimDepoBosalt(); break;
                case 16: AdimDepoOnayi(); break;
            }
        }

        private static void AdimGec(string mesaj)
        {
            _rapor.Add("OK adim " + _adim + ": " + mesaj
                       + " (" + (EditorApplication.timeSinceStartup - _adimBaslangici).ToString("0.0") + " sn)");
            _adim++;
            _adimBaslangici = EditorApplication.timeSinceStartup;
        }

        private static void AdimVadi()
        {
            var vadi = UnityEngine.Object.FindObjectOfType<VadiSahnesi>();
            if (vadi == null) return;
            // ⚠ SecimUygula'yi dogrudan cagirmak EventSystem eksigini KACIRDI
            // (oyuncu bulgusu: secim ekraninda hicbir buton tiklanamiyordu).
            // Test artik gercek UI yolundan gecer: EventSystem var mi + butona
            // gercek pointer tiklamasi isliyor mu.
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                Bitir(false, "EventSystem yok — UI tiklanamaz (secim ekrani olu)");
                return;
            }
            var buton = GameObject.Find("Sec_Çiftçi");
            if (buton == null) return;
            UnityEngine.EventSystems.ExecuteEvents.Execute(
                buton,
                new UnityEngine.EventSystems.PointerEventData(
                    UnityEngine.EventSystems.EventSystem.current),
                UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            AdimGec("vadi acildi, secim BUTONUNA tiklandi (gercek UI yolu)");
        }

        private static void AdimOyunaGec()
        {
            Onyukleyici.OyunaGec();
            AdimGec("oyuna gecildi");
        }

        private static void AdimDunyaBekle()
        {
            var etkilesim = UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>();
            var durum = Durum(etkilesim);
            if (durum == null || durum.Ben == null) return;
            if (string.IsNullOrEmpty(etkilesim.SeciliTohum)) return;   // HUD ilk tohumu secmis olmali
            // Acilis mektubu ilk giriste acilir — gercek UI tiklamasiyla kapatilir
            // (hikaye kapisinin calistigi da boylece test edilir).
            var mektup = GameObject.Find("MektupEkrani");
            if (mektup != null)
            {
                var devam = mektup.transform.Find("Mektup/Ic/Devam");
                if (devam != null)
                {
                    UnityEngine.EventSystems.ExecuteEvents.Execute(
                        devam.gameObject,
                        new UnityEngine.EventSystems.PointerEventData(
                            UnityEngine.EventSystems.EventSystem.current),
                        UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                    _rapor.Add("   mektup acildi ve DEVAM tiklamasiyla kapatildi");
                }
            }
            _tohumBaslangic = durum.EnvanterAdet(etkilesim.SeciliTohum);
            durum.Red += (kod, mesaj) => _rapor.Add("   RED: " + kod + " — " + mesaj);
            AdimGec("dunya kuruldu, oturum ve senkron geldi; secili tohum "
                    + etkilesim.SeciliTohum + " x" + _tohumBaslangic
                    + "; kova " + durum.Ben.KovaKalan + "/" + durum.Ben.KovaKapasite);
        }

        private static void AdimEk()
        {
            // Once ZORUNLU ogretici kilidini yokla: gorev disi karoya dokunus
            // ISLENMEMELI. Islenseydi tohum (1,1)'e ekilir, (0,0) bos kalir ve
            // ekim onayi adimi zaman asimiyla duserdi — kilit boylece olculur.
            Dokun(new Vector2Int(1, 1));
            Dokun(new Vector2Int(0, 0));
            AdimGec("(1,1) kilit yoklamasi + (0,0) karosuna ekim dokunusu");
        }

        private static void AdimEkOnayi()
        {
            var durum = Durum(UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>());
            var karo = durum.Karo(0, 0);
            // Sunucu onayi: tohum adedi Degisim'le duser (iyimser cizim dusurmez).
            var etkilesim = UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>();
            if (karo == null || karo.Bos) return;
            if (durum.EnvanterAdet(etkilesim.SeciliTohum) >= _tohumBaslangic) return;
            EkranGoruntusuAl("duman-1-ekim.png");
            AdimGec("SUNUCU ONAYLI ekim: " + karo.UrunId);
        }

        private static void AdimSula()
        {
            Dokun(new Vector2Int(0, 0));
            AdimGec("ayni karoya sulama dokunusu");
        }

        private static void AdimSulaOnayi()
        {
            var durum = Durum(UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>());
            var karo = durum.Karo(0, 0);
            if (karo == null || karo.SulandiAt == null) return;
            if (durum.Ben.KovaKalan >= durum.Ben.KovaKapasite) return;   // sunucu kovayi dusurdu mu
            EkranGoruntusuAl("duman-3-sulama.png");
            AdimGec("SUNUCU ONAYLI sulama; kova " + durum.Ben.KovaKalan
                    + "/" + durum.Ben.KovaKapasite);
        }

        private static void AdimTopla()
        {
            Dokun(new Vector2Int(-2, 1));
            AdimGec("tarla kenarindaki dala toplama dokunusu");
        }

        private static void AdimToplaOnayi()
        {
            // Oyuncu bulgusu dogrulamasi: "odun elime gelmiyor" — sunucu onayli
            // envanter artisi olculur.
            var durum = Durum(UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>());
            if (durum.EnvanterAdet("odun") <= 0) return;
            AdimGec("SUNUCU ONAYLI toplama: odun x" + durum.EnvanterAdet("odun"));
        }

        private static void AdimKuyu()
        {
            Dokun(new Vector2Int(-4, 1));
            AdimGec("kuyuya kova doldurma dokunusu");
        }

        private static void AdimKovaOnayi()
        {
            var durum = Durum(UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>());
            if (durum.Ben.KovaKapasite <= 0) return;
            if (durum.Ben.KovaKalan < durum.Ben.KovaKapasite) return;
            EkranGoruntusuAl("duman-2-kuyu.png");
            AdimGec("SUNUCU ONAYLI kova dolumu " + durum.Ben.KovaKalan + "/" + durum.Ben.KovaKapasite);
        }

        private static double _sonHasatDenemesi;

        private static void AdimHasatBekle()
        {
            // Bugday 15 sn'de olgunlasir (oyuncu istegiyle hizlandi) — test artik
            // TAM donguyu oynayabiliyor: olgunlasinca gercek dokunusla hasat.
            var tarla = UnityEngine.Object.FindObjectOfType<Amorland.Gorsel.TarlaGorunumu>();
            var durum = Durum(UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>());
            var karo = durum.Karo(0, 0);
            if (karo == null || karo.Bos) { Bitir(false, "hasat oncesi karo bos kaldi"); return; }
            if (!tarla.Olgun(karo)) return;
            if (EditorApplication.timeSinceStartup - _sonHasatDenemesi < 3.0) return;
            _sonHasatDenemesi = EditorApplication.timeSinceStartup;
            Dokun(new Vector2Int(0, 0));
            AdimGec("bugday OLGUNLASTI, hasat dokunusu yapildi");
        }

        private static void AdimHasatOnayi()
        {
            var durum = Durum(UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>());
            var karo = durum.Karo(0, 0);
            if (karo != null && !karo.Bos) return;
            if (durum.EnvanterAdet("bugday") <= 0) return;
            EkranGoruntusuAl("duman-4-hasat.png");
            AdimGec("SUNUCU ONAYLI hasat: bugday x" + durum.EnvanterAdet("bugday")
                    + " — ek/sula/topla/kuyu/HASAT dongusu TAM");
        }

        private static double _sonDonguEylemi;

        /// <summary>
        /// Sv2'ye kadar gercek dongu: ek -> sula (gerekirse kuyu) -> hasat.
        /// Ilk hasat ilk_kez(25) + defter sayfasi(75) getirdigi icin cogu kosuda
        /// tek cevrim yeter; yetmezse dongu doner. Her GERCEK eylem zaman asimi
        /// sayacini tazeler — bu bekleyis mesrudur, kilitlenme degildir.
        /// Sv2'de acilisTohumOdulu (turp tohumu x4) SUNUCU ONAYLI dogrulanir:
        /// "acilan urun ekilebilir olmali" kuralinin ucuna kadar testi.
        /// </summary>
        private static void AdimSeviye2()
        {
            var durum = Durum(UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>());
            if (durum.Ben.Seviye >= 2)
            {
                int turp = durum.EnvanterAdet("turp_tohum");
                if (turp < 4) return; // odul satiri ayni Degisim'de gelmek uzere
                EkranGoruntusuAl("duman-5-seviye2.png");
                AdimGec("SUNUCU ONAYLI Sv" + durum.Ben.Seviye
                        + " + acilis odulu turp_tohum x" + turp);
                return;
            }

            if (EditorApplication.timeSinceStartup - _sonDonguEylemi < 2.0) return;
            var tarla = UnityEngine.Object.FindObjectOfType<Amorland.Gorsel.TarlaGorunumu>();
            var karo = durum.Karo(0, 0);
            bool eylem = false;
            if (karo == null || karo.Bos)
            {
                if (durum.EnvanterAdet("bugday_tohum") <= 0)
                {
                    Bitir(false, "Sv2 dongusunde tohum bitti — tohum geri verme kurali bozuk olabilir");
                    return;
                }
                Dokun(new Vector2Int(0, 0));
                eylem = true;
            }
            else if (karo.SulandiAt == null)
            {
                if (durum.Ben.KovaKalan > 0) Dokun(new Vector2Int(0, 0));
                else Dokun(new Vector2Int(-4, 1));
                eylem = true;
            }
            else if (tarla.Olgun(karo))
            {
                Dokun(new Vector2Int(0, 0));
                eylem = true;
            }
            if (eylem)
            {
                _sonDonguEylemi = EditorApplication.timeSinceStartup;
                _adimBaslangici = EditorApplication.timeSinceStartup;
            }
        }

        private static void AdimDepoyaGec()
        {
            // Bolge gecisinin gercek yolu Oyun.BolgeyeGit — harita tiklamasi da
            // ayni metoda iner; reflection arac, tasarim degil.
            var oyun = UnityEngine.Object.FindObjectOfType<Amorland.Oyun>();
            typeof(Amorland.Oyun)
                .GetMethod("BolgeyeGit", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(oyun, new object[] { "depo" });
            AdimGec("depo bolgesine gecis istendi");
        }

        private static void AdimDepoBosalt()
        {
            // Bolge gecisi 0.6 sn kararmayla acilir; panel gelene kadar beklenir.
            var panel = GameObject.Find("DepoEkrani");
            if (panel == null || !panel.activeInHierarchy) return;
            var bosalt = BulDerin(panel.transform, "Bosalt");
            if (bosalt == null) { Bitir(false, "depo panelinde Bosalt butonu yok"); return; }
            UnityEngine.EventSystems.ExecuteEvents.Execute(
                bosalt.gameObject,
                new UnityEngine.EventSystems.PointerEventData(
                    UnityEngine.EventSystems.EventSystem.current),
                UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            AdimGec("depo paneli acildi, Bosalt GERCEK tiklamayla basildi");
        }

        private static void AdimDepoOnayi()
        {
            var durum = Durum(UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>());
            if (durum.Vadi == null || durum.Vadi.DepoDolu <= 0) return;
            if (durum.EnvanterAdet("bugday") > 0) return; // urunler depoya tasinmali
            EkranGoruntusuAl("duman-6-depo.png");
            Bitir(true, "SUNUCU ONAYLI depo bosaltma: depoDolu=" + durum.Vadi.DepoDolu
                        + "/" + durum.Vadi.DepoKapasitesi
                        + " — tam dongu + Sv2 odulu + depo zinciri calisiyor");
        }

        private static Transform BulDerin(Transform kok, string ad)
        {
            if (kok.name == ad) return kok;
            for (int i = 0; i < kok.childCount; i++)
            {
                var sonuc = BulDerin(kok.GetChild(i), ad);
                if (sonuc != null) return sonuc;
            }
            return null;
        }

        // ------------------------------------------------------------- yardimcilar

        /// <summary>
        /// Gercek dokunma akisi: DokunmaHedefi private'tir ama testin butun degeri
        /// AYNI yoldan gecmekte — reflection burada arac, tasarim degil.
        /// </summary>
        private static void Dokun(Vector2Int karo)
        {
            var etkilesim = UnityEngine.Object.FindObjectOfType<EtkilesimYoneticisi>();
            typeof(EtkilesimYoneticisi)
                .GetMethod("DokunmaHedefi", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(etkilesim, new object[] { karo });
        }

        private static DunyaDurumu Durum(EtkilesimYoneticisi etkilesim)
        {
            if (etkilesim == null) return null;
            return (DunyaDurumu)typeof(EtkilesimYoneticisi)
                .GetField("_durum", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(etkilesim);
        }

        private static void EkranGoruntusuAl(string dosya)
        {
            var kamera = UnityEngine.Object.FindObjectOfType<Camera>();
            if (kamera == null) return;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            var onceki = kamera.targetTexture;
            kamera.targetTexture = rt;
            kamera.Render();
            kamera.targetTexture = onceki;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(
                Path.Combine(SessionState.GetString(CiktiAnahtari, Path.GetTempPath()), dosya),
                tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }

        private static void Bitir(bool basarili, string mesaj)
        {
            EditorApplication.update -= Surucu;
            Application.logMessageReceived -= LogYakala;
            SessionState.SetBool(BayrakAnahtari, false);

            _rapor.Add((basarili ? "SONUC: GECTI — " : "SONUC: KALDI — ") + mesaj);
            if (_hataLoglari.Count > 0)
            {
                _rapor.Add("--- oynanis sirasindaki hata loglari (" + _hataLoglari.Count + ") ---");
                _rapor.AddRange(_hataLoglari);
            }

            // Kullanicinin gercek oturumu geri konur.
            PlayerPrefs.SetString(YenilemeAnahtari, SessionState.GetString(EskiOturumAnahtari, ""));
            PlayerPrefs.SetInt(OgreticiAnahtari, SessionState.GetInt(EskiOgreticiAnahtari, 0));
            PlayerPrefs.SetInt(MektupAnahtari, SessionState.GetInt(EskiMektupAnahtari, 0));
            PlayerPrefs.Save();

            string cikti = SessionState.GetString(CiktiAnahtari, Path.GetTempPath());
            File.WriteAllLines(Path.Combine(cikti, "duman-rapor.txt"), _rapor);
            Debug.Log("[Amorland] Duman testi: " + (basarili ? "GECTI" : "KALDI") + " — " + mesaj);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(basarili ? 0 : 1);
            }
            else
            {
                EditorApplication.ExitPlaymode();
            }
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
