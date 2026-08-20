using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Amorland.Ag;

namespace Amorland.Durum
{
    /// <summary>
    /// Istemcinin dunya kopyasi.
    ///
    /// ⚠ Mimari kural 7 (iyimser istemci): istemci animasyonu ANINDA oynatir ve
    /// durumu <b>tahmin eder</b>; sunucu onaylar veya geri sardirir. Ama istemci
    /// <b>hesaplamaz</b> — tahmin yalnizca gorsel bir on izlemedir, odul/miktar
    /// her zaman sunucudan gelir.
    ///
    /// ⚠ Mimari kural 5: burada <c>DateTime.Now</c> KULLANILMAZ. Butun zaman
    /// karsilastirmalari <see cref="SunucuZamani"/> uzerinden yapilir; oyuncu telefon
    /// saatini ileri alsa bile bugday erken olgunlasmaz.
    /// </summary>
    public class DunyaDurumu
    {
        public long Revizyon { get; private set; } = -1;

        public readonly Dictionary<string, Karo> Karolar = new Dictionary<string, Karo>();
        public readonly Dictionary<string, EnvanterSatiri> Envanter = new Dictionary<string, EnvanterSatiri>();
        public readonly Dictionary<string, DepoSatiri> Depo = new Dictionary<string, DepoSatiri>();
        public readonly Dictionary<string, Dugum> Dugumler = new Dictionary<string, Dugum>();
        public readonly Dictionary<string, AletDurumu> Aletler = new Dictionary<string, AletDurumu>();
        public readonly Dictionary<string, OyuncuOzeti> Oyuncular = new Dictionary<string, OyuncuOzeti>();
        public readonly HashSet<string> Isaretler = new HashSet<string>();

        public VadiOzeti Vadi;
        public string BenimId;

        /// <summary>Partner konumu; 5 Hz gelir, ara degerlerle yumusatilir (C.3).</summary>
        public readonly Dictionary<string, WsKonum> PartnerKonumlari = new Dictionary<string, WsKonum>();

        // ------------------------------------------------------------------ olaylar

        public event Action<Degisim> DurumDegisti;
        public event Action<Olay> OyunOlayi;
        public event Action<string, string> SohbetMesaji;
        public event Action<string, string, string> NotTahtasi;
        public event Action<string, string> Red;

        // ------------------------------------------------------------------ sunucu saati

        private double _saatFarkiSn;
        private bool _saatKuruldu;

        /// <summary>
        /// Sunucu saatiyle yerel saat arasindaki fark. Istemci saatine ASLA guvenilmez
        /// (mimari kural 5), ama buyume gostergesini her karede sunucuya sormak da
        /// olmaz — o yuzden fark bir kez olculur ve yerel saate eklenir.
        /// </summary>
        public void SunucuSaatiniAyarla(long? sunucuZamaniMs)
        {
            if (sunucuZamaniMs == null) return;
            var sunucu = DateTimeOffset.FromUnixTimeMilliseconds(sunucuZamaniMs.Value).UtcDateTime;
            _saatFarkiSn = (sunucu - DateTime.UtcNow).TotalSeconds;
            _saatKuruldu = true;
        }

        public DateTime SunucuZamani =>
            _saatKuruldu ? DateTime.UtcNow.AddSeconds(_saatFarkiSn) : DateTime.UtcNow;

        // ------------------------------------------------------------------ uygulama

        /// <summary>
        /// Degisimi uygular. Tam anlik goruntu ile delta ayni kod yolundan gecer:
        /// <c>Tam</c> true ise once yerel durum atilir, sonra ayni birlestirme yapilir.
        /// </summary>
        public void Uygula(Degisim d)
        {
            if (d == null) return;

            if (d.Tam)
            {
                Karolar.Clear();
                Envanter.Clear();
                Depo.Clear();
                Dugumler.Clear();
                Aletler.Clear();
                Oyuncular.Clear();
                Isaretler.Clear();
                _tahminler.Clear();
            }

            SunucuSaatiniAyarlaDogrudan(d.SunucuZamani);

            if (d.Karolar != null)
                foreach (var k in d.Karolar) Karolar[k.Anahtar] = k;

            if (d.Envanter != null)
                foreach (var e in d.Envanter)
                {
                    string anahtar = e.Sahip + ":" + e.EsyaId;
                    if (e.Adet <= 0) Envanter.Remove(anahtar);
                    else Envanter[anahtar] = e;
                }

            if (d.Depo != null)
                foreach (var s in d.Depo)
                {
                    if (s.Adet <= 0) Depo.Remove(s.EsyaId);
                    else Depo[s.EsyaId] = s;
                }

            if (d.Dugumler != null)
                foreach (var n in d.Dugumler) Dugumler[n.Anahtar] = n;

            if (d.Aletler != null)
                foreach (var a in d.Aletler)
                {
                    if (a.Dayaniklilik <= 0 && a.MaksDayaniklilik > 0) Aletler.Remove(a.Anahtar);
                    else Aletler[a.Anahtar] = a;
                }

            if (d.Oyuncular != null)
                foreach (var o in d.Oyuncular) Oyuncular[o.Id] = o;

            if (d.Vadi != null) Vadi = d.Vadi;

            if (d.Isaretler != null)
                foreach (var i in d.Isaretler) Isaretler.Add(i);

            if (d.Revision > Revizyon) Revizyon = d.Revision;

            DurumDegisti?.Invoke(d);

            if (d.Olaylar != null)
                foreach (var o in d.Olaylar) OyunOlayi?.Invoke(o);
        }

        private void SunucuSaatiniAyarlaDogrudan(DateTime sunucu)
        {
            if (sunucu == default) return;
            _saatFarkiSn = (sunucu.ToUniversalTime() - DateTime.UtcNow).TotalSeconds;
            _saatKuruldu = true;
        }

        // ------------------------------------------------------------------ iyimser tahmin

        private class Tahmin
        {
            public string KaroAnahtar;
            public Karo Onceki;
            public float Zaman;
        }

        private readonly Dictionary<string, Tahmin> _tahminler = new Dictionary<string, Tahmin>();

        /// <summary>Geri sarma suresi (B.3 kural 7): 0.2 sn yumusak, sert ziplama yok.</summary>
        public const float GeriSarmaSn = 0.2f;

        public event Action<string> GeriSarildi;

        /// <summary>
        /// Karoyu gorsel olarak ONCEDEN degistirir. Sunucu onaylarsa gercek degisim
        /// zaten uzerine yazar; reddederse <see cref="RediIsle"/> eski hali geri koyar.
        /// </summary>
        public void KaroTahmini(string clientActionId, Karo tahminEdilen)
        {
            string anahtar = tahminEdilen.Anahtar;
            Karolar.TryGetValue(anahtar, out Karo onceki);
            _tahminler[clientActionId] = new Tahmin
            {
                KaroAnahtar = anahtar,
                Onceki = onceki,
                Zaman = Time.unscaledTime
            };
            Karolar[anahtar] = tahminEdilen;
            DurumDegisti?.Invoke(null);
        }

        public void RediIsle(string clientActionId, string kod, string mesaj)
        {
            if (clientActionId != null && _tahminler.TryGetValue(clientActionId, out Tahmin t))
            {
                if (t.Onceki != null) Karolar[t.KaroAnahtar] = t.Onceki;
                else Karolar.Remove(t.KaroAnahtar);
                _tahminler.Remove(clientActionId);
                GeriSarildi?.Invoke(t.KaroAnahtar);
                DurumDegisti?.Invoke(null);
            }
            Red?.Invoke(kod, mesaj);
        }

        public void TahminOnaylandi(string clientActionId)
        {
            _tahminler.Remove(clientActionId);
        }

        // ------------------------------------------------------------------ sorgular

        public OyuncuOzeti Ben => BenimId != null && Oyuncular.TryGetValue(BenimId, out var o) ? o : null;

        public OyuncuOzeti Partner
        {
            get
            {
                if (Vadi == null || BenimId == null) return null;
                string digerId = Vadi.KuranId == BenimId ? Vadi.PartnerId : Vadi.KuranId;
                if (string.IsNullOrEmpty(digerId) || digerId == BenimId) return null;
                return Oyuncular.TryGetValue(digerId, out var o) ? o : null;
            }
        }

        public int EnvanterAdet(string esyaId)
        {
            if (BenimId == null) return 0;
            return Envanter.TryGetValue(BenimId + ":" + esyaId, out var e) ? e.Adet : 0;
        }

        public IEnumerable<EnvanterSatiri> BenimEnvanterim =>
            Envanter.Values.Where(e => e.Sahip == BenimId).OrderBy(e => e.EsyaId);

        public IEnumerable<Karo> BenimKarolarim =>
            Karolar.Values.Where(k => k.ParselSahibi == BenimId).OrderBy(k => k.Y).ThenBy(k => k.X);

        public IEnumerable<Karo> PartnerKarolari =>
            Karolar.Values.Where(k => k.ParselSahibi != BenimId).OrderBy(k => k.Y).ThenBy(k => k.X);

        public Karo Karo(int x, int y, string parsel = null)
        {
            string p = parsel ?? BenimId;
            return Karolar.TryGetValue(p + ":" + x + ":" + y, out var k) ? k : null;
        }

        public bool AletVar(string aletId) =>
            BenimId != null && Aletler.ContainsKey(BenimId + ":" + aletId);

        public bool IsaretVar(string isaret) =>
            BenimId != null && Isaretler.Contains(BenimId + "|" + isaret);

        public int DefterSayfasi
        {
            get
            {
                int enBuyuk = 0;
                foreach (string i in Isaretler)
                {
                    int ayrac = i.IndexOf("|defter:", StringComparison.Ordinal);
                    if (ayrac < 0) continue;
                    if (!i.StartsWith(BenimId ?? "", StringComparison.Ordinal)) continue;
                    if (int.TryParse(i.Substring(ayrac + 8), out int no)) enBuyuk = Mathf.Max(enBuyuk, no);
                }
                return enBuyuk;
            }
        }

        // ------------------------------------------------------------------ disaridan gelen olaylar

        public void PartnerKonumu(string oyuncuId, WsKonum konum)
        {
            if (oyuncuId == null || konum == null) return;
            PartnerKonumlari[oyuncuId] = konum;
        }

        public void PartnerVarligi(string oyuncuId, bool cevrimici)
        {
            if (oyuncuId != null && Oyuncular.TryGetValue(oyuncuId, out var o)) o.Cevrimici = cevrimici;
            DurumDegisti?.Invoke(null);
        }

        public void SohbetGeldi(string gonderen, string metin) => SohbetMesaji?.Invoke(gonderen, metin);

        public void NotTahtasiGeldi(string gonderen, string sablonId, string nesneId) =>
            NotTahtasi?.Invoke(gonderen, sablonId, nesneId);

        public void Sifirla()
        {
            Revizyon = -1;
            Karolar.Clear();
            Envanter.Clear();
            Depo.Clear();
            Dugumler.Clear();
            Aletler.Clear();
            Oyuncular.Clear();
            Isaretler.Clear();
            PartnerKonumlari.Clear();
            _tahminler.Clear();
            Vadi = null;
        }
    }
}
