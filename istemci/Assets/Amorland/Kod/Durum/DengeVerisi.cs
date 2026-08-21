using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Amorland.Durum
{
    /// <summary>
    /// Sunucudan gelen denge JSON'lari (mimari kural 8).
    ///
    /// ⚠ Bu sinif <b>hicbir denge sayisi icermez</b>. Butun degerler
    /// <c>GET /api/denge</c> ile gelir; boylece denge ayari magaza guncellemesi
    /// gerektirmez. Istemci burayi yalnizca GORSEL amacli okur (isim, sure gostergesi,
    /// ikon rengi) — odul ve miktar hesabi her zaman sunucudadir (kural 1).
    /// </summary>
    public class DengeVerisi
    {
        private JObject _kok;

        public bool Yuklendi => _kok != null;

        public void Yukle(string json)
        {
            _kok = JObject.Parse(json);
            UrunleriIndeksle();
            EsyalariIndeksle();
        }

        // ------------------------------------------------------------------ urunler

        public class UrunBilgisi
        {
            public string Id;
            public string Ad;
            public long BuyumeSn;
            public int AcilisSeviye;
            public int TabanFiyat;
            public bool TohumGeriVerir;
            public int YenidenVerirAdet;
            public long YenidenVerirAraSn;

            public string TohumEsyaId => Id + "_tohum";
            public bool YenidenVerir => YenidenVerirAdet > 1;
        }

        private readonly Dictionary<string, UrunBilgisi> _urunler = new Dictionary<string, UrunBilgisi>();
        public IEnumerable<UrunBilgisi> Urunler => _urunler.Values;

        private void UrunleriIndeksle()
        {
            _urunler.Clear();
            var dizi = _kok?["urunler"]?["urunler"] as JArray;
            if (dizi == null) return;
            foreach (var u in dizi)
            {
                var bilgi = new UrunBilgisi
                {
                    Id = (string)u["id"],
                    Ad = (string)u["ad"],
                    BuyumeSn = (long?)u["buyumeSn"] ?? 0,
                    AcilisSeviye = (int?)u["acilis"]?["seviye"] ?? 1,
                    TabanFiyat = (int?)u["tabanFiyat"] ?? 0,
                    TohumGeriVerir = (bool?)u["tohumGeriVerir"] ?? false,
                    YenidenVerirAdet = (int?)u["yenidenVerir"]?["adet"] ?? 1,
                    YenidenVerirAraSn = (long?)u["yenidenVerir"]?["araSn"] ?? 0
                };
                _urunler[bilgi.Id] = bilgi;
            }
        }

        public UrunBilgisi Urun(string id) =>
            id != null && _urunler.TryGetValue(id, out var u) ? u : null;

        public UrunBilgisi TohumdanUrun(string tohumEsyaId)
        {
            if (tohumEsyaId == null || !tohumEsyaId.EndsWith("_tohum", StringComparison.Ordinal)) return null;
            return Urun(tohumEsyaId.Substring(0, tohumEsyaId.Length - 6));
        }

        // ------------------------------------------------------------------ esya sozlugu

        private readonly Dictionary<string, string> _esyaAdlari = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _esyaKategorileri = new Dictionary<string, string>();

        private void EsyalariIndeksle()
        {
            _esyaAdlari.Clear();
            _esyaKategorileri.Clear();

            foreach (var u in _urunler.Values)
            {
                _esyaAdlari[u.Id] = u.Ad;
                _esyaKategorileri[u.Id] = "urun";
                _esyaAdlari[u.TohumEsyaId] = u.Ad + " tohumu";
                _esyaKategorileri[u.TohumEsyaId] = "tohum";
            }

            foreach (string grup in new[] { "hamMadde", "islenmis" })
            {
                if (!(_kok?["turetilmis"]?["esyalar"]?[grup] is JArray dizi)) continue;
                foreach (var e in dizi)
                {
                    string id = (string)e["id"];
                    if (id == null) continue;
                    _esyaAdlari[id] = (string)e["ad"] ?? id;
                    _esyaKategorileri[id] = (string)e["kategori"] ?? "bilinmeyen";
                }
            }

            if (_kok?["uretim"]?["aletler"] is JArray aletler)
            {
                foreach (var a in aletler)
                {
                    string id = (string)a["id"];
                    if (id == null) continue;
                    _esyaAdlari[id] = AdaCevir(id);
                    _esyaKategorileri[id] = "alet";
                }
            }
        }

        /// <summary>ASCII kimligi okunabilir ada cevirir: <c>tas_kazma</c> -> <c>Tas kazma</c>.</summary>
        private static string AdaCevir(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            string bosluklu = id.Replace('_', ' ');
            return char.ToUpperInvariant(bosluklu[0]) + bosluklu.Substring(1);
        }

        public string EsyaAdi(string esyaId) =>
            esyaId != null && _esyaAdlari.TryGetValue(esyaId, out var ad) ? ad : AdaCevir(esyaId);

        public string EsyaKategorisi(string esyaId) =>
            esyaId != null && _esyaKategorileri.TryGetValue(esyaId, out var k) ? k : "bilinmeyen";

        // ------------------------------------------------------------------ genel erisim

        public JToken Yol(params string[] parcalar)
        {
            JToken t = _kok;
            foreach (string p in parcalar)
            {
                if (t == null) return null;
                t = t[p];
            }
            return t;
        }

        public float Sayi(float varsayilan, params string[] yol)
        {
            var t = Yol(yol);
            return t == null ? varsayilan : (float)t;
        }

        public int TamSayi(int varsayilan, params string[] yol)
        {
            var t = Yol(yol);
            return t == null ? varsayilan : (int)t;
        }

        public string Metin(string varsayilan, params string[] yol)
        {
            var t = Yol(yol);
            return t == null ? varsayilan : (string)t;
        }

        // ------------------------------------------------------------------ sik kullanilanlar

        public float EkmeSn => Sayi(0.4f, "ilerleme", "eylemSureleri", "ekme");
        public float SulamaSn => Sayi(0.4f, "ilerleme", "eylemSureleri", "sulama");
        public float HasatSn => Sayi(0.6f, "ilerleme", "eylemSureleri", "hasat");
        public float ToplamaSn => Sayi(0.5f, "ilerleme", "eylemSureleri", "yabaniToplama");
        public float VurusSn => Sayi(0.7f, "ilerleme", "eylemSureleri", "vurusBasina");
        public float KovaDoldurmaSn => Sayi(1.0f, "ilerleme", "eylemSureleri", "kovaDoldurma");
        public float YurumeKaroSn => Sayi(0.25f, "ilerleme", "eylemSureleri", "yurumeKaroSn");

        public float KameraPitch => Sayi(50f, "ilerleme", "kamera", "pitchDerece");
        public float KameraYumusatma => Sayi(0.15f, "ilerleme", "kamera", "yumusatmaSn");
        public int OluBolgeX => TamSayi(3, "ilerleme", "kamera", "oluBolgeKaro", "x");
        public int OluBolgeY => TamSayi(2, "ilerleme", "kamera", "oluBolgeKaro", "y");

        public float ZoomYakin
        {
            get
            {
                var dizi = Yol("ilerleme", "kamera", "zoomKademeleri") as JArray;
                return dizi != null && dizi.Count > 0 ? (float)dizi[0]["ortoBoyut"] : 5f;
            }
        }

        public float ZoomUzak
        {
            get
            {
                var dizi = Yol("ilerleme", "kamera", "zoomKademeleri") as JArray;
                return dizi != null && dizi.Count > 1 ? (float)dizi[1]["ortoBoyut"] : 8f;
            }
        }

        public int BuyumeAsamaSayisi => TamSayi(4, "urunler", "buyumeAsamasi", "asamaSayisi");
        public int YiginBoyutu => TamSayi(99, "ilerleme", "canta", "yiginBoyutu");
        public int ParaninIlkSeviyesi => TamSayi(5, "ilerleme", "paraninGirisi", "ilkSeviye");
        public float GeriSarmaSn => Sayi(0.2f, "turetilmis", "iyimserIstemci", "geriSarmaSn");
        public int KonumYayinHz => TamSayi(5, "turetilmis", "iyimserIstemci", "konumYayinHz");
        public int MaksKuyrukEylem => TamSayi(20, "turetilmis", "cevrimdisi", "maksKuyrukEylem");
        public int MaksKopusSn => TamSayi(60, "turetilmis", "cevrimdisi", "maksSureSn");

        /// <summary>Red kodunun kullaniciya gosterilecek metni (C.3).</summary>
        public string RedMetni(string kod, string sunucuMesaji)
        {
            string t = Metin(null, "turetilmis", "redKodlari", kod ?? "");
            return !string.IsNullOrEmpty(t) ? t : (sunucuMesaji ?? "İşlem yapılamadı.");
        }

        /// <summary>
        /// Denge gelmeden onceki yedek renkler: mercan, gokyuzu, nane, lavanta.
        /// ⚠ Beyaz DONULMEZ (UI denetimi): giris ekraninda "rengini sec" daireleri
        /// dordu de bembeyaz cikiyor ve ozellik islevsiz gorunuyordu.
        /// </summary>
        private static readonly Color[] _yedekRenkler =
        {
            new Color(0.93f, 0.45f, 0.40f),
            new Color(0.42f, 0.66f, 0.86f),
            new Color(0.45f, 0.78f, 0.55f),
            new Color(0.72f, 0.56f, 0.84f),
        };

        /// <summary>Oyuncu renkleri (4 renk, B.18 0:00).</summary>
        public Color OyuncuRengi(int indeks)
        {
            var dizi = Yol("turetilmis", "misafirHesap", "renkler") as JArray;
            if (dizi == null || indeks < 0 || indeks >= dizi.Count)
                return _yedekRenkler[((indeks % 4) + 4) % 4];
            string hex = (string)dizi[indeks]["hex"];
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }

        public string OyuncuRenkAdi(int indeks)
        {
            var dizi = Yol("turetilmis", "misafirHesap", "renkler") as JArray;
            if (dizi == null || indeks < 0 || indeks >= dizi.Count) return "Renk " + indeks;
            return (string)dizi[indeks]["ad"];
        }

        public int RenkSayisi
        {
            get
            {
                var dizi = Yol("turetilmis", "misafirHesap", "renkler") as JArray;
                return dizi?.Count ?? 4;
            }
        }

        public int YasKapisiEsigi => TamSayi(13, "ilerleme", "yasKapisi", "esik");

        /// <summary>Bolgenin acilis seviyesi. Bilinmiyorsa cok yuksek bir sayi doner.</summary>
        public int BolgeAcilisSeviyesi(string bolgeId)
        {
            if (!(Yol("ilerleme", "bolgeler") is JArray dizi)) return 99;
            foreach (var b in dizi)
            {
                if ((string)b["id"] == bolgeId) return (int?)b["acilisSeviye"] ?? 99;
            }
            return 99;
        }

        public string BolgeAdi(string bolgeId)
        {
            if (!(Yol("ilerleme", "bolgeler") is JArray dizi)) return bolgeId;
            foreach (var b in dizi)
            {
                if ((string)b["id"] == bolgeId) return (string)b["ad"] ?? bolgeId;
            }
            return bolgeId;
        }

        public List<string> BolgeIdleri()
        {
            var sonuc = new List<string>();
            if (Yol("ilerleme", "bolgeler") is JArray dizi)
            {
                foreach (var b in dizi) sonuc.Add((string)b["id"]);
            }
            return sonuc;
        }

        /// <summary>Basit tezgahin verdigi tarifler (B.14 kilitlenme degismezi).</summary>
        public List<string> TezgahTarifleri(string yapiId)
        {
            var sonuc = new List<string>();
            if (!(Yol("ilerleme", "evYapilari") is JArray dizi)) return sonuc;
            foreach (var y in dizi)
            {
                if ((string)y["id"] != yapiId) continue;
                if (y["tarifler"] is JArray t)
                {
                    foreach (var x in t) sonuc.Add((string)x);
                }
            }
            return sonuc;
        }

        /// <summary>Bir aletin malzeme listesi. UI'da "5 odun + 10 tas" yazmak icin.</summary>
        public List<(string EsyaId, int Adet)> AletMalzemesi(string aletId)
        {
            var sonuc = new List<(string, int)>();
            if (!(Yol("uretim", "aletler") is JArray dizi)) return sonuc;
            foreach (var a in dizi)
            {
                if ((string)a["id"] != aletId) continue;
                if (a["malzeme"] is JArray m)
                {
                    foreach (var x in m) sonuc.Add(((string)x["id"], (int)x["adet"]));
                }
            }
            return sonuc;
        }

        public int AletCraftSeviyesi(string aletId)
        {
            if (!(Yol("uretim", "aletler") is JArray dizi)) return 99;
            foreach (var a in dizi)
            {
                if ((string)a["id"] == aletId) return (int?)a["craftSeviye"] ?? 99;
            }
            return 99;
        }

        public long YabaniYenilenmeSn(string kaynakId)
        {
            if (!(Yol("uretim", "yabaniKaynaklar", "kaynaklar") is JArray dizi)) return 1800;
            foreach (var k in dizi)
            {
                if ((string)k["id"] == kaynakId) return (long?)k["yenilenmeSn"] ?? 1800;
            }
            return 1800;
        }

        public string YabaniAdi(string kaynakId)
        {
            if (!(Yol("uretim", "yabaniKaynaklar", "kaynaklar") is JArray dizi)) return AdaCevir(kaynakId);
            foreach (var k in dizi)
            {
                if ((string)k["id"] == kaynakId) return (string)k["ad"] ?? AdaCevir(kaynakId);
            }
            return AdaCevir(kaynakId);
        }
    }
}
