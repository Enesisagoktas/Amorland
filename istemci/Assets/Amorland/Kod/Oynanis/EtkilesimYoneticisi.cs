using System;
using System.Collections.Generic;
using UnityEngine;
using Amorland.Ag;
using Amorland.Durum;
using Amorland.Girdi;
using Amorland.Gorsel;
using Amorland.Ses;

namespace Amorland.Oynanis
{
    /// <summary>
    /// Baglam duyarli etkilesim (B.6).
    ///
    /// ⚠ <b>ALET SECIMI YOKTUR.</b> Oyuncu karoya dokunur, karakter yurur ve o karonun
    /// durumuna gore ne yapilacaksa onu yapar. Ekrandaki 4 slotluk cubuk bir ALET cubugu
    /// degil, TOHUM/SARF secicisidir; aletler envanterde durur ve otomatik secilir.
    ///
    /// ⚠ Surukle-boyama (basili tut + surukle) parmagin gectigi karolara ayni eylemi
    /// KUYRUGA alir. Envanterdeki surukle-birak YASAK (B.26 yasak 5) ama tarladaki
    /// boyama farkli bir jesttir ve celismez.
    /// </summary>
    public class EtkilesimYoneticisi : MonoBehaviour
    {
        public enum Eylem { Yok, Ek, Sula, Hasat, Topla, KovaDoldur }

        private DunyaDurumu _durum;
        private DengeVerisi _denge;
        private BaglantiYoneticisi _baglanti;
        private KameraYoneticisi _kamera;
        private KarakterKontrolcusu _karakter;
        private TarlaGorunumu _tarla;
        private SesYoneticisi _ses;

        /// <summary>Tohum secici (4 slot). Bos ise ekim yapilmaz, sadece sula/hasat.</summary>
        public string SeciliTohum;

        private readonly List<Vector2Int> _kuyruk = new List<Vector2Int>();
        private readonly List<GameObject> _kuyrukIsaretleri = new List<GameObject>();
        private Transform _isaretKok;
        private Mesh _isaretMesh;

        private bool _boyamaModu;
        private Vector2 _basmaNoktasi;
        private float _basmaAni;
        private Vector2Int _sonBoyananKaro;
        private bool _kuyrukIsleniyor;

        private const float BoyamaEsigiPiksel = 24f;
        private const float UzunBasSn = 0.5f;

        public event Action<string> Bildirim;
        public event Action<Vector2Int> UzunBasildi;

        public void Kur(
            DunyaDurumu durum,
            DengeVerisi denge,
            BaglantiYoneticisi baglanti,
            KameraYoneticisi kamera,
            KarakterKontrolcusu karakter,
            TarlaGorunumu tarla,
            SesYoneticisi ses)
        {
            _durum = durum;
            _denge = denge;
            _baglanti = baglanti;
            _kamera = kamera;
            _karakter = karakter;
            _tarla = tarla;
            _ses = ses;

            _isaretKok = new GameObject("KuyrukIsaretleri").transform;
            _isaretKok.SetParent(transform, false);
            _isaretMesh = MeshUretici.Disk(0.12f, 8, Palet.UiVurgu);

            _karakter.HedefeVarildi += KaroyaVarildi;
        }

        // ------------------------------------------------------------------ girdi dongusu

        private void Update()
        {
            if (_durum == null || _kamera == null) return;

            Girdi.Girdi.Temas[] temaslar = Girdi.Girdi.Temaslar(out int adet);
            if (adet == 0) return;

            // Iki parmak: cimdik zoom. Karo etkilesimi calismaz.
            if (adet >= 2)
            {
                float fark = Girdi.Girdi.CimdikDegisimi();
                if (Mathf.Abs(fark) > 40f) _kamera.ZoomDegistir();
                return;
            }

            var t = temaslar[0];
            if (Girdi.Girdi.UiUzerinde(t.Konum)) return;

            if (t.Basladi)
            {
                _basmaNoktasi = t.Konum;
                _basmaAni = Time.unscaledTime;
                _boyamaModu = false;

                // Kuyrukta is varken herhangi bir yere dokunmak IPTAL eder (B.6).
                if (_kuyruk.Count > 0)
                {
                    KuyrugoTemizle();
                    Bildirim?.Invoke("Kuyruk iptal edildi.");
                    return;
                }
            }
            else if (t.Suruyor)
            {
                float kayma = Vector2.Distance(t.Konum, _basmaNoktasi);
                if (!_boyamaModu && kayma > BoyamaEsigiPiksel && Time.unscaledTime - _basmaAni > 0.08f)
                {
                    _boyamaModu = true;
                    _sonBoyananKaro = new Vector2Int(int.MinValue, int.MinValue);
                }
                if (_boyamaModu) BoyamaSurukle(t.Konum);

                // Uzun bas: belirsiz karolarda dairesel menu (B.6).
                if (!_boyamaModu && Time.unscaledTime - _basmaAni > UzunBasSn && kayma < BoyamaEsigiPiksel)
                {
                    if (KaroBul(t.Konum, out Vector2Int karo))
                    {
                        UzunBasildi?.Invoke(karo);
                        _basmaAni = float.MaxValue; // bir kez tetiklensin
                    }
                }
            }
            else if (t.Bitti)
            {
                if (_boyamaModu)
                {
                    _boyamaModu = false;
                    KuyrugoBaslat();
                    return;
                }
                if (Time.unscaledTime == float.MaxValue) return;
                if (KaroBul(t.Konum, out Vector2Int karo)) DokunmaHedefi(karo);
            }
        }

        private bool KaroBul(Vector2 ekranNoktasi, out Vector2Int karo)
        {
            karo = default;
            if (!_kamera.ZemineYansit(ekranNoktasi, out Vector3 dunya)) return false;
            karo = KameraYoneticisi.KaroyaCevir(dunya);
            return true;
        }

        // ------------------------------------------------------------------ tek dokunus

        private Vector2Int _bekleyenHedef;
        private bool _hedefBekliyor;

        private void DokunmaHedefi(Vector2Int karo)
        {
            Eylem eylem = EylemBelirle(karo);
            if (eylem == Eylem.Yok)
            {
                // Eylem yoksa yine de yuru: harita gezmek de bir eylem.
                _hedefBekliyor = false;
                _karakter.Yuru(karo);
                return;
            }
            _bekleyenHedef = karo;
            _hedefBekliyor = true;
            _tarla.VurguGoster(new Vector3(karo.x, 0f, karo.y));
            _karakter.Yuru(karo);
        }

        private void KaroyaVarildi(Vector2Int varilan)
        {
            _tarla.VurguGizle();

            if (_kuyrukIsleniyor)
            {
                KuyrugoIsle();
                return;
            }
            if (!_hedefBekliyor) return;
            _hedefBekliyor = false;
            EylemiUygula(_bekleyenHedef, kuyruktaMi: false);
        }

        // ------------------------------------------------------------------ baglam kurali

        /// <summary>
        /// Karo durumundan eylemi turetir (B.6 tablosu).
        /// Bu tek fonksiyon "alet secimi yok" tasariminin tamamidir.
        /// </summary>
        public Eylem EylemBelirle(Vector2Int hedef)
        {
            // Kuyu: kova doldurma
            if (Vector3.Distance(
                    new Vector3(hedef.x, 0f, hedef.y), TarlaGorunumu.KuyuKonumu) < 1.2f)
            {
                return Eylem.KovaDoldur;
            }

            // Yabani kaynak: alet gerektirmez (B.14 guvenlik agi)
            foreach (var d in _durum.Dugumler.Values)
            {
                if (d.X != hedef.x || d.Y != hedef.y) continue;
                if (d.Sinif != "yabani") continue;
                return d.Hazir(_durum.SunucuZamani) ? Eylem.Topla : Eylem.Yok;
            }

            Karo karo = _durum.Karo(hedef.x, hedef.y);
            if (karo == null) return Eylem.Yok;

            if (karo.Bos) return string.IsNullOrEmpty(SeciliTohum) ? Eylem.Yok : Eylem.Ek;
            if (karo.SulandiAt == null) return Eylem.Sula;
            if (_tarla.Olgun(karo)) return Eylem.Hasat;
            return Eylem.Yok;
        }

        private void EylemiUygula(Vector2Int hedef, bool kuyruktaMi)
        {
            Eylem eylem = EylemBelirle(hedef);
            if (eylem == Eylem.Yok) return;

            Niyet niyet;
            switch (eylem)
            {
                case Eylem.Ek:
                    var urun = _denge.TohumdanUrun(SeciliTohum);
                    if (urun == null) return;
                    niyet = Niyet.Olustur(
                        "ek", NiyetHedef.Karo(hedef.x, hedef.y), new NiyetEk { UrunId = urun.Id });
                    IyimserEkim(niyet.ClientActionId, hedef, urun.Id);
                    _ses.Cal(SesEtiketi.Ekme, kuyruktaMi);
                    break;

                case Eylem.Sula:
                    niyet = Niyet.Olustur("sula", NiyetHedef.Karo(hedef.x, hedef.y));
                    IyimserSulama(niyet.ClientActionId, hedef);
                    _ses.Cal(SesEtiketi.Sulama, kuyruktaMi);
                    break;

                case Eylem.Hasat:
                    niyet = Niyet.Olustur("hasat", NiyetHedef.Karo(hedef.x, hedef.y));
                    _ses.Cal(SesEtiketi.Hasat, kuyruktaMi);
                    break;

                case Eylem.Topla:
                    var dugum = DugumBul(hedef);
                    if (dugum == null) return;
                    niyet = Niyet.Olustur("topla", NiyetHedef.Dugum(dugum.Bolge, dugum.X, dugum.Y));
                    _ses.Cal(SesEtiketi.Toplama, kuyruktaMi);
                    break;

                case Eylem.KovaDoldur:
                    niyet = Niyet.Olustur("kova_doldur");
                    _ses.Cal(SesEtiketi.Kova, kuyruktaMi);
                    break;

                default:
                    return;
            }

            _baglanti.NiyetGonder(niyet);
        }

        private Dugum DugumBul(Vector2Int hedef)
        {
            foreach (var d in _durum.Dugumler.Values)
            {
                if (d.X == hedef.x && d.Y == hedef.y) return d;
            }
            return null;
        }

        // ------------------------------------------------------------------ iyimser tahmin

        /// <summary>
        /// Ekim gorseli ANINDA cizilir (mimari kural 7). Sunucu reddederse
        /// <see cref="DunyaDurumu.RediIsle"/> eski hali 0.2 sn'de geri koyar.
        /// Burada hicbir MIKTAR veya XP hesaplanmaz — sadece karonun gorunumu.
        /// </summary>
        private void IyimserEkim(string clientActionId, Vector2Int hedef, string urunId)
        {
            _durum.KaroTahmini(
                clientActionId,
                new Karo
                {
                    ParselSahibi = _durum.BenimId,
                    X = hedef.x,
                    Y = hedef.y,
                    UrunId = urunId,
                    EkildiAt = _durum.SunucuZamani,
                    SulandiAt = null,
                    HasatSayaci = 0,
                    Gubreli = false
                });
        }

        private void IyimserSulama(string clientActionId, Vector2Int hedef)
        {
            Karo mevcut = _durum.Karo(hedef.x, hedef.y);
            if (mevcut == null) return;
            _durum.KaroTahmini(
                clientActionId,
                new Karo
                {
                    ParselSahibi = mevcut.ParselSahibi,
                    X = mevcut.X,
                    Y = mevcut.Y,
                    UrunId = mevcut.UrunId,
                    EkildiAt = mevcut.EkildiAt,
                    SulandiAt = _durum.SunucuZamani,
                    HasatSayaci = mevcut.HasatSayaci,
                    SonHasatAt = mevcut.SonHasatAt,
                    Gubreli = mevcut.Gubreli
                });
        }

        // ------------------------------------------------------------------ surukle-boyama

        private void BoyamaSurukle(Vector2 ekranNoktasi)
        {
            if (!KaroBul(ekranNoktasi, out Vector2Int karo)) return;
            if (karo == _sonBoyananKaro) return;
            _sonBoyananKaro = karo;

            if (EylemBelirle(karo) == Eylem.Yok) return;
            if (_kuyruk.Contains(karo)) return;

            _kuyruk.Add(karo);
            var isaret = MateryalDeposu.Nesne(
                "KuyrukNoktasi", _isaretMesh, _isaretKok, new Vector3(karo.x, 0.05f, karo.y));
            _kuyrukIsaretleri.Add(isaret);
        }

        private void KuyrugoBaslat()
        {
            if (_kuyruk.Count == 0) return;
            _kuyrukIsleniyor = true;
            _karakter.Yuru(_kuyruk[0]);
        }

        private void KuyrugoIsle()
        {
            if (_kuyruk.Count == 0)
            {
                _kuyrukIsleniyor = false;
                return;
            }

            Vector2Int hedef = _kuyruk[0];
            _kuyruk.RemoveAt(0);
            if (_kuyrukIsaretleri.Count > 0)
            {
                UnityEngine.Object.Destroy(_kuyrukIsaretleri[0]);
                _kuyrukIsaretleri.RemoveAt(0);
            }

            // Kuyruk eyleminde ses %75, haptik SADECE ilk ve son (B.23).
            EylemiUygula(hedef, kuyruktaMi: true);

            if (_kuyruk.Count == 0)
            {
                _kuyrukIsleniyor = false;
                return;
            }
            _karakter.Yuru(_kuyruk[0]);
        }

        private void KuyrugoTemizle()
        {
            _kuyruk.Clear();
            foreach (var g in _kuyrukIsaretleri) UnityEngine.Object.Destroy(g);
            _kuyrukIsaretleri.Clear();
            _kuyrukIsleniyor = false;
            _karakter.Dur();
        }

        public int KuyruktakiEylem => _kuyruk.Count;
    }
}
