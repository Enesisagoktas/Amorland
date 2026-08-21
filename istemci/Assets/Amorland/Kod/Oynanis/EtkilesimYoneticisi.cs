using System;
using System.Collections;
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
        private EfektYoneticisi _efekt;

        /// <summary>Tohum secici (4 slot). Bos ise ekim yapilmaz, sadece sula/hasat.</summary>
        public string SeciliTohum;

        private readonly List<Vector2Int> _kuyruk = new List<Vector2Int>();
        private readonly List<GameObject> _kuyrukIsaretleri = new List<GameObject>();
        private Transform _isaretKok;
        private Sprite _isaretSprite;

        private Coroutine _eylemCoroutine;
        private Eylem _kuyrukEylemi;
        private bool _boyamaModu;
        private Vector2 _basmaNoktasi;
        private float _basmaAni;
        private bool _uzunBasTetiklendi;
        private Vector2Int _sonBoyananKaro;
        private bool _kuyrukIsleniyor;

        /// <summary>Joystick aktifken karo etkilesimi calismaz: tek dokunus iki is yapmasin.</summary>
        public System.Func<bool> JoystickAktif;

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
            SesYoneticisi ses,
            EfektYoneticisi efekt)
        {
            _efekt = efekt;
            _durum = durum;
            _denge = denge;
            _baglanti = baglanti;
            _kamera = kamera;
            _karakter = karakter;
            _tarla = tarla;
            _ses = ses;

            _isaretKok = new GameObject("KuyrukIsaretleri").transform;
            _isaretKok.SetParent(transform, false);
            _isaretSprite = IsaretSprite();

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

            // Sol ucte bir joystick bolgesi. Orada baslayan dokunus yurume girdisidir,
            // karo etkilesimi degil — ikisi ayni parmakla cakismamali.
            if (JoystickAktif != null && JoystickAktif()) return;
            if (t.Basladi && Arayuz.Joystick.JoystickBolgesi(t.Konum)) return;

            if (t.Basladi)
            {
                _basmaNoktasi = t.Konum;
                _basmaAni = Time.unscaledTime;
                _boyamaModu = false;
                _uzunBasTetiklendi = false;

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
                if (!_boyamaModu && !_uzunBasTetiklendi
                    && Time.unscaledTime - _basmaAni > UzunBasSn && kayma < BoyamaEsigiPiksel)
                {
                    if (KaroBul(t.Konum, out Vector2Int karo))
                    {
                        _uzunBasTetiklendi = true;
                        UzunBasildi?.Invoke(karo);
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
                // Uzun bas menusu acildiysa birakma ayrica bir eylem tetiklemez.
                if (_uzunBasTetiklendi) return;
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
            EylemIptal();
            // Partnerin parseli GORUNUR ama dokunulamaz (B.7). Sessizce hicbir sey
            // yapmamak "oyun beni dinlemiyor" hissi verir; sebebi soylenir.
            if (PartnerParseliMi(karo))
            {
                Bildirim?.Invoke("Bu parsel partnerin. Dokunamazsın.");
                return;
            }

            Eylem eylem = EylemBelirle(karo);
            if (eylem == Eylem.Yok)
            {
                // Eylem yoksa yine de yuru: harita gezmek de bir eylem.
                // Buyuyen karoysa kalan sure ucar (B.18 "Sayac" ani).
                BuyumeBilgisiGoster(karo);
                _hedefBekliyor = false;
                _karakter.Yuru(karo);
                return;
            }
            _bekleyenHedef = karo;
            _hedefBekliyor = true;
            _tarla.VurguGoster(new Vector3(karo.x, karo.y, 0f));
            _karakter.Yuru(karo);
        }

        private void KaroyaVarildi(Vector2Int varilan)
        {
            _tarla.VurguGizle();

            if (_kuyrukIsleniyor)
            {
                _eylemCoroutine = StartCoroutine(KuyrukAdimi());
                return;
            }
            if (!_hedefBekliyor) return;
            _hedefBekliyor = false;
            _eylemCoroutine = StartCoroutine(GecikmeliEylem(_bekleyenHedef));
        }

        /// <summary>
        /// B.6 eylem suresi: karakter karoya varinca eylem denge suresi kadar
        /// "yapilir" (hop + bekleme), niyet SONRA gider. Yeni dokunus coroutine'i
        /// iptal eder — animasyon kilidi yoktur, oyuncu kapana kisilmaz.
        /// Sifir sureli eylem mitralyoz hissi veriyordu (denetim).
        /// </summary>
        private IEnumerator GecikmeliEylem(Vector2Int hedef)
        {
            _karakter.EylemHopu();
            yield return new WaitForSeconds(EylemSuresi(EylemBelirle(hedef)));
            EylemiUygula(hedef, kuyruktaMi: false);
        }

        private float EylemSuresi(Eylem eylem)
        {
            switch (eylem)
            {
                case Eylem.Ek: return _denge.EkmeSn;
                case Eylem.Sula: return _denge.SulamaSn;
                case Eylem.Hasat: return _denge.HasatSn;
                case Eylem.Topla: return _denge.ToplamaSn;
                case Eylem.KovaDoldur: return _denge.KovaDoldurmaSn;
                default: return 0f;
            }
        }

        private void EylemIptal()
        {
            if (_eylemCoroutine != null)
            {
                StopCoroutine(_eylemCoroutine);
                _eylemCoroutine = null;
            }
        }

        // ------------------------------------------------------------------ baglam kurali

        /// <summary>
        /// Karo durumundan eylemi turetir (B.6 tablosu).
        /// Bu tek fonksiyon "alet secimi yok" tasariminin tamamidir.
        /// </summary>
        public Eylem EylemBelirle(Vector2Int hedef)
        {
            // Kuyu: kova doldurma
            if (Vector2.Distance(
                    new Vector2(hedef.x, hedef.y),
                    new Vector2(TarlaGorunumu.KuyuKonumu.x, TarlaGorunumu.KuyuKonumu.y)) < 1.2f)
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
                    _efekt.ToprakPufu(hedef);
                    _karakter.EylemHopu();
                    break;

                case Eylem.Sula:
                    niyet = Niyet.Olustur("sula", NiyetHedef.Karo(hedef.x, hedef.y));
                    IyimserSulama(niyet.ClientActionId, hedef);
                    _ses.Cal(SesEtiketi.Sulama, kuyruktaMi);
                    _efekt.SuSicramasi(hedef);
                    _karakter.EylemHopu();
                    break;

                case Eylem.Hasat:
                    niyet = Niyet.Olustur("hasat", NiyetHedef.Karo(hedef.x, hedef.y));
                    IyimserHasat(niyet.ClientActionId, hedef);
                    _ses.Cal(SesEtiketi.Hasat, kuyruktaMi);
                    if (!kuyruktaMi) Haptik.Haptik.Calistir(Haptik.Haptik.Desen.CokHafif);
                    Karo hasatKaro = _durum.Karo(hedef.x, hedef.y);
                    if (hasatKaro != null && !hasatKaro.Bos)
                    {
                        _efekt.UrunZiplamasi(hedef, Karo64.Al(Sayfa64.Ek,
                            Karo64.UrunOlgun[TarlaGorunumu.UrunGorseli(hasatKaro.UrunId)]));
                        _efekt.UcanYazi(hedef, "+" + _denge.EsyaAdi(hasatKaro.UrunId),
                            Palet.UiAltin);
                    }
                    _karakter.EylemHopu();
                    break;

                case Eylem.Topla:
                    var dugum = DugumBul(hedef);
                    if (dugum == null) return;
                    niyet = Niyet.Olustur("topla", NiyetHedef.Dugum(dugum.Bolge, dugum.X, dugum.Y));
                    _ses.Cal(SesEtiketi.Toplama, kuyruktaMi);
                    if (!kuyruktaMi) Haptik.Haptik.Calistir(Haptik.Haptik.Desen.CokHafif);
                    _efekt.UrunZiplamasi(hedef, Karo64.Al(Sayfa64.Ek, Karo64.DevrikKutuk));
                    _karakter.EylemHopu();
                    break;

                case Eylem.KovaDoldur:
                    niyet = Niyet.Olustur("kova_doldur");
                    _ses.Cal(SesEtiketi.Kova, kuyruktaMi);
                    _efekt.SuSicramasi(hedef);
                    _efekt.UcanYazi(hedef, "Kova doldu", new Color(0.55f, 0.8f, 0.9f));
                    _karakter.EylemHopu();
                    break;

                default:
                    return;
            }

            _baglanti.NiyetGonder(niyet);
        }

        /// <summary>
        /// Karo partnerin parselinde mi? Partner parseli dunyada +8 kaydirilmis cizilir
        /// (bkz. <see cref="TarlaGorunumu.KaroKonumu"/>), o yuzden kaydirma geri alinir.
        /// </summary>
        private bool PartnerParseliMi(Vector2Int hedef)
        {
            var partner = _durum.Partner;
            if (partner == null) return false;
            const int Kaydirma = 8;
            if (hedef.x < Kaydirma) return false;
            return _durum.Karo(hedef.x - Kaydirma, hedef.y, partner.Id) != null;
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

        /// <summary>
        /// Hasat gorseli ANINDA bosalir (kural 7): efekt oynarken bitkinin dikili
        /// kalmasi "cift urun" gorunumu veriyordu (denetim). Yeniden veren urunun
        /// ara evresini sunucu Degisim'le duzeltir; red gelirse 0.2 sn'de geri sarar.
        /// </summary>
        private void IyimserHasat(string clientActionId, Vector2Int hedef)
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
                    UrunId = null,
                    EkildiAt = null,
                    SulandiAt = null,
                    HasatSayaci = 0
                });
        }

        private void BuyumeBilgisiGoster(Vector2Int hedef)
        {
            Karo k = _durum.Karo(hedef.x, hedef.y);
            if (k == null || k.Bos || k.SulandiAt == null || _tarla.Olgun(k)) return;
            double kalan = _tarla.KalanSn(k);
            if (kalan <= 0 || kalan >= double.MaxValue) return;
            _efekt.UcanYazi(hedef, Arayuz.UiKit.SureMetni(kalan),
                new Color(0.72f, 0.9f, 0.5f));
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

            Eylem e = EylemBelirle(karo);
            if (e == Eylem.Yok) return;
            // B.6: boyama AYNI eylemi kuyruklar — ilk gecerli karo eylemi kilitler.
            // Karisik kuyruk, sularken istemeden tohum ekiyordu (denetim).
            if (_kuyruk.Count == 0) _kuyrukEylemi = e;
            else if (e != _kuyrukEylemi) return;
            if (_kuyruk.Contains(karo)) return;

            _kuyruk.Add(karo);
            var isaret = new GameObject("KuyrukNoktasi");
            isaret.transform.SetParent(_isaretKok, false);
            isaret.transform.localPosition = new Vector3(karo.x, karo.y, 0f);
            var isr = isaret.AddComponent<SpriteRenderer>();
            isr.sprite = _isaretSprite;
            isr.sortingOrder = 3;
            _kuyrukIsaretleri.Add(isaret);
        }

        private void KuyrugoBaslat()
        {
            if (_kuyruk.Count == 0) return;
            _kuyrukIsleniyor = true;
            // Haptik yalniz ILK ve SON eylemde (B.23).
            Haptik.Haptik.KuyrukIlk(Haptik.Haptik.Desen.CokHafif);
            _karakter.Yuru(_kuyruk[0]);
        }

        private IEnumerator KuyrukAdimi()
        {
            if (_kuyruk.Count == 0)
            {
                KuyrukBitir();
                yield break;
            }

            Vector2Int hedef = _kuyruk[0];
            _kuyruk.RemoveAt(0);
            if (_kuyrukIsaretleri.Count > 0)
            {
                UnityEngine.Object.Destroy(_kuyrukIsaretleri[0]);
                _kuyrukIsaretleri.RemoveAt(0);
            }

            // Durum bu arada degistiyse (partner sulamis olabilir) karo atlanir:
            // kuyruk yalnizca kilitlenen eylemi uygular (B.6).
            if (EylemBelirle(hedef) == _kuyrukEylemi)
            {
                _karakter.EylemHopu();
                yield return new WaitForSeconds(EylemSuresi(_kuyrukEylemi));
                // Kuyruk eyleminde ses %75, haptik SADECE ilk ve son (B.23).
                EylemiUygula(hedef, kuyruktaMi: true);
            }

            if (_kuyruk.Count == 0)
            {
                KuyrukBitir();
                yield break;
            }
            _karakter.Yuru(_kuyruk[0]);
        }

        private void KuyrukBitir()
        {
            _kuyrukIsleniyor = false;
            Haptik.Haptik.KuyrukSon(Haptik.Haptik.Desen.CokHafif);
        }

        private void KuyrugoTemizle()
        {
            EylemIptal();
            _kuyruk.Clear();
            foreach (var g in _kuyrukIsaretleri) UnityEngine.Object.Destroy(g);
            _kuyrukIsaretleri.Clear();
            _kuyrukIsleniyor = false;
            _karakter.Dur();
        }

        public int KuyruktakiEylem => _kuyruk.Count;

        // ------------------------------------------------------------------ isaret sprite'i

        private static Sprite _isaretOrnegi;

        /// <summary>Kuyruk noktasi: yumusak kenarli altin disk (calisma zamaninda uretilir).</summary>
        private static Sprite IsaretSprite()
        {
            if (_isaretOrnegi != null) return _isaretOrnegi;
            const int B = 16;
            var doku = new Texture2D(B, B, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            Color renk = Palet.UiVurgu;
            for (int y = 0; y < B; y++)
            {
                for (int x = 0; x < B; x++)
                {
                    float m = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f), new Vector2(B / 2f, B / 2f)) / (B / 2f);
                    Color c = renk;
                    c.a = Mathf.Clamp01(1.05f - m * m * 1.6f);
                    doku.SetPixel(x, y, c);
                }
            }
            doku.Apply();
            _isaretOrnegi = Sprite.Create(
                doku, new Rect(0, 0, B, B), new Vector2(0.5f, 0.5f), B * 2.5f);
            return _isaretOrnegi;
        }
    }
}
