using System;
using System.Collections.Generic;
using UnityEngine;
using Amorland.Durum;

namespace Amorland.Ag
{
    /// <summary>
    /// Baglanti yasam dongusu: yeniden baglanma merdiveni (C.9) ve
    /// cevrimdisi kuyruk (C.4 "zarif kopus").
    ///
    /// ⚠ C.9: <b>arka plana atilma iOS'ta baglantiyi birkac saniyede oldurur.</b>
    /// Yeniden baglanma istisna degil, NORMAL durumdur. Bu sinif her uygulama one
    /// geldiginde bastan baglanir ve farki revizyondan kapatir.
    ///
    /// Cevrimdisi ilkesi tek cumle: <b>kendi parselinde basladigin isi bitirebilirsin,
    /// paylasilan duruma dokunan hicbir seyi yapamazsin.</b> Kendi parselindeki eylemler
    /// hic kimseyle cakismaz; depo/tren/maden ise partnerle cakisir ve baglanti donunce
    /// toplu red yagmuru olur.
    /// </summary>
    public class BaglantiYoneticisi
    {
        private readonly WsIstemci _ws;
        private readonly DunyaDurumu _durum;
        private readonly MonoBehaviour _kosucu;

        private readonly List<Niyet> _kuyruk = new List<Niyet>();
        private readonly HashSet<string> _bekleyenOnaylar = new HashSet<string>();

        private float _sonrakiDenemeAn;
        private int _merdivenAdimi;
        private float _kopusAni = -1f;
        private bool _elleKapatildi;

        /// <summary>Bu eylemler cevrimdisi kuyruga alinabilir (turetilmis.json: cevrimdisi).</summary>
        private static readonly HashSet<string> IzinliCevrimdisi =
            new HashSet<string> { "ek", "sula", "hasat", "topla" };

        private static readonly float[] MerdivenSn = { 1f, 2f, 4f, 8f, 15f, 30f };

        public int MaksKuyrukEylem = 20;
        public float MaksKopusSn = 60f;

        public string Jwt;

        /// <summary>Kuyruk doldu veya 60 sn gecti: "baglanti yok" perdesi iner, oyun durur.</summary>
        public bool PerdeIndi { get; private set; }

        public event Action<string> Bildirim;
        public event Action<bool> BaglantiDegisti;

        public BaglantiYoneticisi(MonoBehaviour kosucu, WsIstemci ws, DunyaDurumu durum)
        {
            _kosucu = kosucu;
            _ws = ws;
            _durum = durum;

            _ws.Acildi += SoketAcildi;
            _ws.Kapandi += SoketKapandi;
            _ws.MesajGeldi += MesajGeldi;
        }

        public bool Bagli => _ws.Bagli;
        public int KuyruktakiEylem => _kuyruk.Count;

        public void Baslat(string jwt)
        {
            Jwt = jwt;
            _elleKapatildi = false;
            _merdivenAdimi = 0;
            _sonrakiDenemeAn = 0f;
        }

        public void Durdur()
        {
            _elleKapatildi = true;
            _ws.Kapat("uygulama kapaniyor");
        }

        // ------------------------------------------------------------------ dongu

        public void Guncelle()
        {
            _ws.Isle();

            if (_elleKapatildi || string.IsNullOrEmpty(Jwt)) return;

            if (!_ws.Bagli && _ws.AnlikDurum != WsIstemci.Durum.Baglaniyor)
            {
                if (Time.unscaledTime >= _sonrakiDenemeAn)
                {
                    _ws.Bagla(SunucuAyari.WsAdresi);
                    float gecikme = MerdivenSn[Mathf.Min(_merdivenAdimi, MerdivenSn.Length - 1)];
                    _sonrakiDenemeAn = Time.unscaledTime + gecikme;
                    _merdivenAdimi++;
                }
            }

            // Perde kosulu: kuyruk doldu VEYA kopustan beri 60 sn gecti (C.4).
            if (!_ws.Bagli && _kopusAni > 0f)
            {
                bool sureDoldu = Time.unscaledTime - _kopusAni >= MaksKopusSn;
                bool kuyrukDoldu = _kuyruk.Count >= MaksKuyrukEylem;
                if ((sureDoldu || kuyrukDoldu) && !PerdeIndi)
                {
                    PerdeIndi = true;
                    BaglantiDegisti?.Invoke(false);
                }
            }
        }

        // ------------------------------------------------------------------ niyet gonderme

        /// <summary>
        /// Niyeti gonderir; baglanti yoksa kurallara uygunsa kuyruga alir.
        /// </summary>
        /// <returns>false ise eylem cevrimdisi yapilamaz ve istemci kullaniciya soyler.</returns>
        public bool NiyetGonder(Niyet niyet)
        {
            if (_ws.Bagli)
            {
                _bekleyenOnaylar.Add(niyet.ClientActionId);
                _ws.Gonder(new WsGiden { T = Protokol.TipNiyet, Niyet = niyet });
                return true;
            }

            if (!IzinliCevrimdisi.Contains(niyet.Tip))
            {
                Bildirim?.Invoke("Bu işlem bağlantı gerektirir.");
                return false;
            }
            if (_kuyruk.Count >= MaksKuyrukEylem)
            {
                Bildirim?.Invoke("Bağlantı yok. Bekleyen işlem sınırı doldu.");
                return false;
            }
            _kuyruk.Add(niyet);
            return true;
        }

        public void KonumGonder(string bolge, Vector2 konum, float yon)
        {
            if (!_ws.Bagli) return;
            _ws.Gonder(
                new WsGiden
                {
                    T = Protokol.TipKonum,
                    Konum = new WsKonum { Bolge = bolge, X = konum.x, Y = konum.y, Yon = yon }
                });
        }

        public void SohbetGonder(string metin)
        {
            if (!_ws.Bagli) return;
            _ws.Gonder(new WsGiden { T = Protokol.TipSohbet, Metin = metin });
        }

        public void NotTahtasi(string sablonId, string nesneId)
        {
            if (!_ws.Bagli) return;
            _ws.Gonder(new WsGiden { T = Protokol.TipNotTahtasi, SablonId = sablonId, NesneId = nesneId });
        }

        // ------------------------------------------------------------------ olaylar

        private void SoketAcildi()
        {
            _merdivenAdimi = 0;
            _kopusAni = -1f;
            PerdeIndi = false;

            long? sonRevision = _durum.Revizyon >= 0 ? _durum.Revizyon : (long?)null;
            _ws.Gonder(
                new WsGiden
                {
                    T = Protokol.TipBaglan,
                    Jwt = Jwt,
                    SonRevision = sonRevision,
                    IstemciSurum = SunucuAyari.IstemciSurum
                });
            BaglantiDegisti?.Invoke(true);
        }

        private void SoketKapandi(string sebep)
        {
            if (_kopusAni < 0f) _kopusAni = Time.unscaledTime;
            BaglantiDegisti?.Invoke(false);
        }

        private void MesajGeldi(WsGelen mesaj)
        {
            switch (mesaj.T)
            {
                case Protokol.TipSenkronTam:
                    _durum.SunucuSaatiniAyarla(mesaj.SunucuZamaniMs);
                    _durum.Uygula(mesaj.Degisim);
                    KuyrugoBosalt();
                    break;

                case Protokol.TipSenkronDelta:
                    _durum.SunucuSaatiniAyarla(mesaj.SunucuZamaniMs);
                    if (mesaj.Degisimler != null)
                    {
                        foreach (var d in mesaj.Degisimler) _durum.Uygula(d);
                    }
                    KuyrugoBosalt();
                    break;

                case Protokol.TipOnay:
                    _bekleyenOnaylar.Remove(mesaj.ClientActionId);
                    _durum.Uygula(mesaj.Degisim);
                    break;

                case Protokol.TipDegisim:
                    _durum.Uygula(mesaj.Degisim);
                    break;

                case Protokol.TipRed:
                    _bekleyenOnaylar.Remove(mesaj.ClientActionId);
                    // Tek bildirim kanali: Red olayi (Oyun'daki abone toast basar).
                    // Buradan ikinci bir Bildirim atmak AYNI reddi ust uste iki
                    // seritle gosteriyordu; kuyruk bosaltilirken ikisi de susar,
                    // ozet mekanizmasi konusur.
                    _durum.RediIsle(
                        mesaj.ClientActionId, mesaj.Kod, mesaj.Mesaj, sessiz: _kuyrukBosaltiliyor);
                    if (_kuyrukBosaltiliyor) _kuyrukRedSayisi++;
                    break;

                case Protokol.TipHata:
                    Bildirim?.Invoke(mesaj.Mesaj);
                    if (mesaj.Kod == "BASKA_CIHAZ")
                    {
                        _elleKapatildi = true;
                        _ws.Kapat("baska cihaz");
                    }
                    break;

                case Protokol.TipKonum:
                    _durum.PartnerKonumu(mesaj.OyuncuId, mesaj.Konum);
                    break;

                case Protokol.TipSohbet:
                    _durum.SohbetGeldi(mesaj.OyuncuId, mesaj.Mesaj);
                    break;

                case Protokol.TipNotTahtasi:
                    _durum.NotTahtasiGeldi(mesaj.OyuncuId, mesaj.SablonId, mesaj.NesneId);
                    break;

                case Protokol.TipPartnerGirdi:
                    _durum.PartnerVarligi(mesaj.OyuncuId, true);
                    break;

                case Protokol.TipPartnerCikti:
                    _durum.PartnerVarligi(mesaj.OyuncuId, false);
                    break;
            }
        }

        // ------------------------------------------------------------------ kuyruk

        private bool _kuyrukBosaltiliyor;
        private int _kuyrukRedSayisi;

        /// <summary>
        /// Kuyruktaki eylemler SIRAYLA gonderilir. Biri reddedilirse kalanlar yine de
        /// denenir (biri gecersizse digerleri gecerli olabilir) ve her red ayri bildirim
        /// uretmez — tek bir ozet gosterilir (C.4).
        /// </summary>
        private void KuyrugoBosalt()
        {
            if (_kuyruk.Count == 0) return;

            _kuyrukBosaltiliyor = true;
            _kuyrukRedSayisi = 0;
            foreach (var niyet in _kuyruk)
            {
                _bekleyenOnaylar.Add(niyet.ClientActionId);
                _ws.Gonder(new WsGiden { T = Protokol.TipNiyet, Niyet = niyet });
            }
            int gonderilen = _kuyruk.Count;
            _kuyruk.Clear();

            // Redler asenkron gelir; kisa bir gecikmeyle ozetlenir.
            _kosucu.StartCoroutine(OzetiGoster(gonderilen));
        }

        private System.Collections.IEnumerator OzetiGoster(int gonderilen)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            _kuyrukBosaltiliyor = false;
            if (_kuyrukRedSayisi > 0)
            {
                Bildirim?.Invoke(_kuyrukRedSayisi + " işlem uygulanamadı.");
            }
            _kuyrukRedSayisi = 0;
        }
    }
}
