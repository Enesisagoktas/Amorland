using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Amorland.Ag
{
    /// <summary>
    /// REST uclari (C.10): oturum, vadi, denge.
    ///
    /// Oyun eylemleri buradan GECMEZ — onlar WebSocket'ten gider. REST yalnizca
    /// meta islerdir. Bu ayrim protokolun parcasi: eylemler siralanabilir ve
    /// yayinlanabilir olmali, HTTP istekleri ise bagimsiz.
    /// </summary>
    public class RestIstemci
    {
        private readonly MonoBehaviour _kosucu;
        public string ErisimJetonu;

        public RestIstemci(MonoBehaviour kosucu)
        {
            _kosucu = kosucu;
        }

        public class Sonuc<T>
        {
            public T Veri;
            public bool Basarili;
            public string HataKodu;
            public string HataMesaji;
            public long HttpKodu;

            public static Sonuc<T> Ok(T veri) => new Sonuc<T> { Veri = veri, Basarili = true };

            public static Sonuc<T> Hata(string kod, string mesaj, long http) =>
                new Sonuc<T> { Basarili = false, HataKodu = kod, HataMesaji = mesaj, HttpKodu = http };
        }

        public Coroutine Gonder<T>(
            string yol, string yontem, object govde, Action<Sonuc<T>> geriCagri, bool jetonGerekli = true)
        {
            return _kosucu.StartCoroutine(Istek(yol, yontem, govde, geriCagri, jetonGerekli));
        }

        private IEnumerator Istek<T>(
            string yol, string yontem, object govde, Action<Sonuc<T>> geriCagri, bool jetonGerekli)
        {
            string url = SunucuAyari.Temel + yol;
            using (var istek = new UnityWebRequest(url, yontem))
            {
                if (govde != null)
                {
                    byte[] veri = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(govde));
                    istek.uploadHandler = new UploadHandlerRaw(veri);
                    istek.SetRequestHeader("Content-Type", "application/json");
                }
                istek.downloadHandler = new DownloadHandlerBuffer();
                if (jetonGerekli && !string.IsNullOrEmpty(ErisimJetonu))
                {
                    istek.SetRequestHeader("Authorization", "Bearer " + ErisimJetonu);
                }
                istek.timeout = 15;

                yield return istek.SendWebRequest();

                string metin = istek.downloadHandler != null ? istek.downloadHandler.text : "";
                long http = istek.responseCode;

                if (istek.result != UnityWebRequest.Result.Success)
                {
                    string kod = "AG_HATASI";
                    string mesaj = istek.error;
                    if (!string.IsNullOrEmpty(metin))
                    {
                        try
                        {
                            var h = JsonConvert.DeserializeObject<SunucuHatasi>(metin);
                            if (h != null && !string.IsNullOrEmpty(h.Kod))
                            {
                                kod = h.Kod;
                                mesaj = h.Mesaj;
                            }
                        }
                        catch (Exception)
                        {
                            // Sunucu JSON dondurmediyse ham hata mesajiyla devam et.
                        }
                    }
                    geriCagri(Sonuc<T>.Hata(kod, mesaj, http));
                    yield break;
                }

                try
                {
                    T veri = typeof(T) == typeof(string)
                        ? (T)(object)metin
                        : JsonConvert.DeserializeObject<T>(metin);
                    geriCagri(Sonuc<T>.Ok(veri));
                }
                catch (Exception e)
                {
                    geriCagri(Sonuc<T>.Hata("COZUMLEME", e.Message, http));
                }
            }
        }

        // ------------------------------------------------------------------ uclar

        public Coroutine MisafirGiris(string ad, int renk, Action<Sonuc<OturumPaketi>> geriCagri)
        {
            var govde = new { cihazId = SunucuAyari.CihazKimligi(), ad, renk };
            return Gonder("/api/oturum/misafir", "POST", govde, geriCagri, jetonGerekli: false);
        }

        public Coroutine JetonYenile(string yenilemeJetonu, Action<Sonuc<OturumPaketi>> geriCagri)
        {
            return Gonder(
                "/api/oturum/yenile", "POST", new { yenilemeJetonu }, geriCagri, jetonGerekli: false);
        }

        public Coroutine YasKapisi(int dogumYili, Action<Sonuc<string>> geriCagri)
        {
            return Gonder("/api/oturum/yas", "POST", new { dogumYili }, geriCagri);
        }

        public Coroutine HesapBagla(string tip, string referans, Action<Sonuc<string>> geriCagri)
        {
            return Gonder("/api/oturum/bagla", "POST", new { tip, @ref = referans }, geriCagri);
        }

        public Coroutine KurtarmaKodu(Action<Sonuc<string>> geriCagri)
        {
            return Gonder("/api/oturum/kurtarma-kodu", "POST", new { }, geriCagri);
        }

        public Coroutine VadiKur(Action<Sonuc<VadiYaniti>> geriCagri)
        {
            return Gonder("/api/vadi", "POST", new { }, geriCagri);
        }

        public Coroutine VadimiAl(Action<Sonuc<VadiYaniti>> geriCagri)
        {
            return Gonder("/api/vadi/benim", "GET", null, geriCagri);
        }

        public Coroutine DavetKodu(Action<Sonuc<string>> geriCagri)
        {
            return Gonder("/api/vadi/davet", "POST", new { }, geriCagri);
        }

        public Coroutine Katil(string kod, Action<Sonuc<VadiYaniti>> geriCagri)
        {
            return Gonder("/api/vadi/katil", "POST", new { kod }, geriCagri);
        }

        public Coroutine AnlikGoruntu(string vadiId, Action<Sonuc<Degisim>> geriCagri)
        {
            return Gonder("/api/vadi/" + vadiId + "/anlik", "GET", null, geriCagri);
        }

        /// <summary>Tren panosu (B.15): sefer, geri sayim, 3 siparis karti.</summary>
        public Coroutine TrenDurumu(string vadiId, Action<Sonuc<TrenCevabi>> geriCagri)
        {
            return Gonder("/api/vadi/" + vadiId + "/tren", "GET", null, geriCagri);
        }

        /// <summary>Denge JSON'lari (mimari kural 8). Kimlik gerekmez.</summary>
        public Coroutine Denge(Action<Sonuc<string>> geriCagri)
        {
            return Gonder("/api/denge", "GET", null, geriCagri, jetonGerekli: false);
        }

        public Coroutine Saglik(Action<Sonuc<string>> geriCagri)
        {
            return Gonder("/api/saglik", "GET", null, geriCagri, jetonGerekli: false);
        }
    }
}
