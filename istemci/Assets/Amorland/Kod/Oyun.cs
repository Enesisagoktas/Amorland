using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Amorland.Ag;
using Amorland.Arayuz;
using Amorland.Durum;
using Amorland.Girdi;
using Amorland.Gorsel;
using Amorland.Ogretici;
using Amorland.Oynanis;
using Amorland.Ses;

namespace Amorland
{
    /// <summary>
    /// Oyunun kok bileseni: butun katmanlari kurar ve birbirine baglar.
    ///
    /// Katman sirasi bilincli:
    ///   denge -> kimlik -> vadi -> durum -> gorsel -> girdi -> UI -> ogretici
    /// Her katman yalnizca altindakini bilir. Ust katman (UI) oyun kurali icermez;
    /// alt katman (durum) UI'i tanimaz.
    ///
    /// ⚠ Mimari kural 1: bu dosyada tek bir odul/miktar hesabi yoktur. Istemci
    /// niyet gonderir, sunucu sonucu hesaplar, istemci gelen degisimi cizer.
    /// </summary>
    public class Oyun : MonoBehaviour
    {
        // --- katmanlar
        private RestIstemci _rest;
        private WsIstemci _ws;
        private BaglantiYoneticisi _baglanti;
        private readonly DunyaDurumu _durum = new DunyaDurumu();
        private readonly DengeVerisi _denge = new DengeVerisi();

        // --- sahne
        private Camera _kamera;
        private KameraYoneticisi _kameraYoneticisi;
        private VadiHaritasi _vadi;
        private TarlaGorunumu _tarla;
        private KarakterKontrolcusu _karakter;
        private Transform _partnerGorsel;
        private Animator _karakterAnim;
        private Izgara _izgara;
        private EtkilesimYoneticisi _etkilesim;
        private SesYoneticisi _ses;
        private EfektYoneticisi _efekt;

        // --- arayuz
        private Canvas _oyunKanvasi;
        private Canvas _ustKanvas;
        private Hud _hud;
        private Joystick _joystick;
        private CantaEkrani _canta;
        private DepoEkrani _depo;
        private TezgahEkrani _tezgah;
        private DefterEkrani _defter;
        private HaritaEkrani _harita;
        private DavetEkrani _davet;
        private AyarlarEkrani _ayarlar;
        private SohbetEkrani _sohbet;
        private TrenEkrani _tren;
        private MarketEkrani _market;
        private NotTahtasiEkrani _notTahtasi;
        private GirisEkrani _giris;
        private Bildirimler _bildirimler;
        private BaglantiPerdesi _perde;
        private SeviyeAtlamaEkrani _seviyeEkrani;
        private YuklemeEkrani _yukleme;
        private OgreticiAkisi _ogretici;

        private string _yenilemeJetonu;
        private int _benimRenk;
        private int _partnerRenk = 1;
        private string _aktifBolge = "tarla";
        private float _sonrakiKonumYayini;

        private const string YenilemeAnahtari = "amorland.yenileme";

        // ================================================================== yasam dongusu

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // Yatay oyun (B.0)
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.AutoRotation;
            // Gercek zamanli golge YOK (B.2): sahte blob golge kullanilir.
            QualitySettings.shadows = ShadowQuality.Disable;

            SahneKur();
            ArayuzKur();
            _yukleme.Goster("Sunucuya bağlanılıyor...");
            StartCoroutine(Acilis());
        }

        private void Update()
        {
            _baglanti?.Guncelle();

            if (_baglanti != null)
            {
                if (_baglanti.PerdeIndi) _perde.Goster(_baglanti.KuyruktakiEylem);
                else if (_perde.Acik && _baglanti.Bagli) _perde.Gizle();
            }

            KonumYayinla();
            JoystickGuncelle();
            _tren?.SureTazele();
            _market?.Guncelle();
        }

        private void OnDestroy() => _baglanti?.Durdur();

        // ================================================================== kurulum

        private void SahneKur()
        {
            var kameraGo = new GameObject("Kamera");
            kameraGo.transform.SetParent(transform, false);
            _kamera = kameraGo.AddComponent<Camera>();
            _kameraYoneticisi = kameraGo.AddComponent<KameraYoneticisi>();

            // Dunya = VADI (2.5D). Oyun ayri bir "tarla kutusu" kurmaz: vadinin
            // tarla bolgesinin ici oynanabilir alandir. Kok, sunucu karo (0,0)
            // vadideki OyunCapa'ya denk gelecek sekilde kaydirilir; boylece tum
            // oyun mantigi sunucu koordinatinda degismeden kalir (isik yok:
            // sprite'lar isiksiz cizilir, derinlik Y-sort'tan gelir).
            var vadiGo = new GameObject("Vadi");
            vadiGo.transform.SetParent(transform, false);
            vadiGo.transform.localPosition = new Vector3(
                -(VadiHaritasi.OyunCapaX + 0.5f), -(VadiHaritasi.OyunCapaY + 0.5f), 0f);
            _vadi = vadiGo.AddComponent<VadiHaritasi>();
            _vadi.Kur(oyunAlani: true);

            var sesGo = new GameObject("Ses");
            sesGo.transform.SetParent(transform, false);
            _ses = sesGo.AddComponent<SesYoneticisi>();
            _ses.Kur();
            _ses.SeviyeleriYukle();

            var tarlaGo = new GameObject("TarlaGorunumu");
            tarlaGo.transform.SetParent(transform, false);
            // Hucre-merkez sozlesmesi: karo (x,y)'nin merkezi dunya (x,y).
            tarlaGo.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);
            _tarla = tarlaGo.AddComponent<TarlaGorunumu>();

            _izgara = new Izgara();

            var karakterGo = new GameObject("Karakter");
            karakterGo.transform.SetParent(transform, false);
            _karakter = karakterGo.AddComponent<KarakterKontrolcusu>();

            var etkilesimGo = new GameObject("Etkilesim");
            etkilesimGo.transform.SetParent(transform, false);
            _etkilesim = etkilesimGo.AddComponent<EtkilesimYoneticisi>();

            _efekt = etkilesimGo.AddComponent<EfektYoneticisi>();
        }

        private void ArayuzKur()
        {
            if (EventSystem.current == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(transform, false);
                esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                // ⚠ InputSystemUIInputModule UnityEngine.EventSystems'te DEGIL,
                // UnityEngine.InputSystem.UI ad alanindadir.
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esGo.AddComponent<StandaloneInputModule>();
#endif
            }

            _oyunKanvasi = UiKit.Kanvas("OyunKanvasi", 10);
            _oyunKanvasi.transform.SetParent(transform, false);
            _ustKanvas = UiKit.Kanvas("UstKanvas", 50);
            _ustKanvas.transform.SetParent(transform, false);

            var bildirimGo = new GameObject("BildirimKatmani");
            bildirimGo.transform.SetParent(transform, false);
            _bildirimler = bildirimGo.AddComponent<Bildirimler>();
            _bildirimler.Kur(_ustKanvas.transform);

            _perde = bildirimGo.AddComponent<BaglantiPerdesi>();
            _perde.Kur(_ustKanvas.transform);

            _seviyeEkrani = bildirimGo.AddComponent<SeviyeAtlamaEkrani>();
            _seviyeEkrani.Kur(_ustKanvas.transform);

            _yukleme = bildirimGo.AddComponent<YuklemeEkrani>();
            _yukleme.Kur(_ustKanvas.transform);
        }

        // ================================================================== acilis akisi

        private IEnumerator Acilis()
        {
            _rest = new RestIstemci(this);

            bool dengeGeldi = false;
            bool dengeBasarili = false;
            _rest.Denge(sonuc =>
            {
                dengeGeldi = true;
                dengeBasarili = sonuc.Basarili;
                if (sonuc.Basarili)
                {
                    try
                    {
                        _denge.Yukle(sonuc.Veri);
                    }
                    catch (Exception e)
                    {
                        dengeBasarili = false;
                        Debug.LogError("[Amorland] Denge cozulemedi: " + e.Message);
                    }
                }
            });
            while (!dengeGeldi) yield return null;

            if (!dengeBasarili)
            {
                _yukleme.Goster(
                    "Sunucuya ulaşılamadı.\n" + SunucuAyari.Temel
                    + "\n\nSunucu çalışıyor mu? Ayarlardan adresi değiştirebilirsin.");
                yield break;
            }

            EkranlariKur();

            // Kayitli oturum varsa sessizce gir; yoksa isim ekrani.
            _yenilemeJetonu = PlayerPrefs.GetString(YenilemeAnahtari, "");
            if (!string.IsNullOrEmpty(_yenilemeJetonu))
            {
                bool geldi = false;
                _rest.JetonYenile(_yenilemeJetonu, sonuc =>
                {
                    geldi = true;
                    if (sonuc.Basarili) OturumKuruldu(sonuc.Veri);
                    else
                    {
                        PlayerPrefs.DeleteKey(YenilemeAnahtari);
                        _yukleme.Gizle();
                        _giris.Ac();
                    }
                });
                while (!geldi) yield return null;
                yield break;
            }

            _yukleme.Gizle();
            _giris.Ac();
        }

        private void EkranlariKur()
        {
            _giris = new GirisEkrani(_ustKanvas.transform, _denge);
            _giris.Basla += (isim, renk) =>
            {
                _yukleme.Goster("Vadi hazırlanıyor...");
                _rest.MisafirGiris(isim, renk, sonuc =>
                {
                    if (!sonuc.Basarili)
                    {
                        _yukleme.Gizle();
                        _giris.HataGoster(sonuc.HataMesaji ?? "Bağlantı kurulamadı.");
                        return;
                    }
                    // Yas kapisi bir kez sorulur (B.18 0:15).
                    if (sonuc.Veri.DogumYili == null)
                    {
                        _bekleyenOturum = sonuc.Veri;
                        _yukleme.Gizle();
                        _giris.YasAdimiGoster();
                        return;
                    }
                    OturumKuruldu(sonuc.Veri);
                });
            };
            _giris.YasVerildi += yil =>
            {
                if (_bekleyenOturum == null) return;
                _rest.ErisimJetonu = _bekleyenOturum.ErisimJetonu;
                _rest.YasKapisi(yil, _ =>
                {
                    var paket = _bekleyenOturum;
                    _bekleyenOturum = null;
                    OturumKuruldu(paket);
                });
            };
        }

        private OturumPaketi _bekleyenOturum;

        private void OturumKuruldu(OturumPaketi paket)
        {
            _rest.ErisimJetonu = paket.ErisimJetonu;
            _yenilemeJetonu = paket.YenilemeJetonu;
            PlayerPrefs.SetString(YenilemeAnahtari, paket.YenilemeJetonu);
            PlayerPrefs.Save();

            _durum.BenimId = paket.OyuncuId;
            _benimRenk = paket.Renk;
            _giris.Kapat();
            _yukleme.Goster("Vadi açılıyor...");

            _rest.VadiKur(sonuc =>
            {
                if (!sonuc.Basarili)
                {
                    _yukleme.Goster("Vadi kurulamadı: " + sonuc.HataMesaji);
                    return;
                }
                DunyayiKur();
                _baglanti.Baslat(paket.ErisimJetonu);
                _yukleme.Gizle();

                // Vadi haritasindan bir bolgeye dokunularak gelindiyse oraya gec.
                if (!string.IsNullOrEmpty(Onyukleyici.IstenenBolge))
                {
                    string bolge = Onyukleyici.IstenenBolge;
                    Onyukleyici.IstenenBolge = null;
                    BolgeyeGit(bolge);
                }
            });
        }

        // ================================================================== dunya

        private void DunyayiKur()
        {
            _ws = new WsIstemci();
            _baglanti = new BaglantiYoneticisi(this, _ws, _durum)
            {
                MaksKuyrukEylem = _denge.MaksKuyrukEylem,
                MaksKopusSn = _denge.MaksKopusSn
            };
            _baglanti.Bildirim += mesaj => _bildirimler.Goster(mesaj, uyari: true);
            _baglanti.BaglantiDegisti += bagli =>
            {
                if (bagli) _perde.Gizle();
                else _perde.DurumYaz("Yeniden bağlanıyor...");
            };

            _durum.Red += (kod, mesaj) =>
            {
                _bildirimler.Goster(_denge.RedMetni(kod, mesaj), uyari: true);
                _ses.Cal(SesEtiketi.UiHata);
                Haptik.Haptik.Calistir(Haptik.Haptik.Desen.Hata);
            };
            _durum.OyunOlayi += OlayIsle;
            _durum.SohbetMesaji += (gonderen, metin) =>
            {
                // Ifade tekerlegi ayni kanaldan "ifade:" onekiyle gelir (B.9):
                // ayri mesaj tipi acmak sunucuya icerik bilgisi sizdirirdi.
                if (metin.StartsWith("ifade:"))
                {
                    IfadeBalonu(benim: false, metin.Substring("ifade:".Length));
                    return;
                }
                // Gonderen adiyla goster: kimden geldigi belirsiz mesaj, cift
                // oyununda sohbet degil sistem bildirimi gibi okunur.
                string ad = _durum.Oyuncular.TryGetValue(gonderen, out var o) ? o.Ad : "Eşin";
                _bildirimler.Goster(ad + ": " + metin);
            };
            _durum.NotTahtasi += (gonderen, sablon, nesne) =>
            {
                string kimden = _durum.Oyuncular.TryGetValue(gonderen, out var k) ? k.Ad : "Eşin";
                _bildirimler.Goster(
                    kimden + " tahtaya yazdı: " + NotTahtasiEkrani.Metin(sablon, NesneAdi(nesne)),
                    false, 8f);
                _ses.Cal(SesEtiketi.UiOnay);
            };

            _tarla.Kur(_durum, _denge);

            // Karakter: vadi acilisinda secilen poncik koylu (2.5D sprite).
            // Cift rengi SPRITE'A islenmis (tulum/elbise, sapka bandi, fiyonk) —
            // eski tum-govde tint'i yuzu de boyuyordu, kaldirildi (B.7 ayrim korunur).
            bool benKadin = PlayerPrefs.GetString("vadi_karakter", "erkek") == "kadin";
            Transform gorsel = KarakterSpriteKur(_karakter.transform, benKadin, _benimRenk);

            IzgarayiKur();
            _karakter.KaroBasinaSn = _denge.YurumeKaroSn;
            _karakter.Kur(gorsel, _izgara, new Vector3(1f, -2f, 0f));
            _karakter.YurumeKareleri(
                Karo64.Al(Sayfa64.Ek, Karo64.Oyuncu(benKadin, _benimRenk, 0)),
                Karo64.Al(Sayfa64.Ek, Karo64.Oyuncu(benKadin, _benimRenk, 1)),
                Karo64.Al(Sayfa64.Ek, Karo64.Oyuncu(benKadin, _benimRenk, 2)));

            _kameraYoneticisi.Pitch = _denge.KameraPitch;
            _kameraYoneticisi.YumusatmaSn = _denge.KameraYumusatma;
            _kameraYoneticisi.OluBolge = new Vector2(_denge.OluBolgeX, _denge.OluBolgeY);
            _kameraYoneticisi.ZoomYakin = _denge.ZoomYakin;
            _kameraYoneticisi.ZoomUzak = _denge.ZoomUzak;
            _kameraYoneticisi.Kur(_kamera, _karakter.transform);

            PartnerGorseliKur();
            HudKur();

            _efekt.Kur(_kamera, _oyunKanvasi);
            _etkilesim.Kur(
                _durum, _denge, _baglanti, _kameraYoneticisi, _karakter, _tarla, _ses, _efekt);
            _etkilesim.JoystickAktif = () => _joystick != null && _joystick.Aktif;
            _etkilesim.Bildirim += mesaj => _bildirimler.Goster(mesaj);
            _etkilesim.UzunBasildi += DaireselMenu;

            _ses.OrtamBaslat(SesEtiketi.OrtamTarla);

            _ogretici = gameObject.AddComponent<OgreticiAkisi>();
            _ogretici.Kur(_durum, _denge, _ustKanvas.transform, _hud, _efekt);

            // Hikayenin kapisi: ilk giriste Saffet'in mektubu (B.1). Bir kez
            // gosterilir; sonrasini defter sayfalari anlatir.
            if (!MektupEkrani.Okundu)
            {
                new MektupEkrani(_ustKanvas.transform);
            }
        }

        private void IzgarayiKur()
        {
            _izgara.Temizle();
            // Sinirlar TUM VADI: karakter tarladan cikip vadiyi gezebilir.
            _izgara.MinX = -VadiHaritasi.OyunCapaX;
            _izgara.MaksX = VadiHaritasi.En - VadiHaritasi.OyunCapaX - 1;
            _izgara.MinY = -VadiHaritasi.OyunCapaY;
            _izgara.MaksY = VadiHaritasi.Boy - VadiHaritasi.OyunCapaY - 1;
            // Vadi engelleri (su, bina, govde) geri cagriyla: kume taramasi yok.
            _izgara.DisEngel = (x, y) => !_vadi.Gecilebilir(
                x + VadiHaritasi.OyunCapaX, y + VadiHaritasi.OyunCapaY);
            // Kuyu gecilemez: uzerinden yurunmez, yanina gidilir.
            Vector3 kuyu = TarlaGorunumu.KuyuKonumu;
            _izgara.EngelKoy(Mathf.RoundToInt(kuyu.x), Mathf.RoundToInt(kuyu.y));
        }

        /// <summary>
        /// Ayak-pivot sozlesmesi (2.5D): kok karo merkezindedir, sprite yarim karo
        /// yukarida bir cocuktur. Y-sort karsilastirmasi ayaga gore yapilir —
        /// karakter agac govdesinin ustundeyken arkasinda, altindayken onunde cizilir.
        /// </summary>
        private Transform KarakterSpriteKur(Transform kok, bool kadin, int renk)
        {
            var golgeGo = new GameObject("Golge");
            golgeGo.transform.SetParent(kok, false);
            // ⚠ Golge elipsi DOKUNUN ALT YARISINDA cizili (merkez -0.22 birim,
            // olculdu). Cocuk +0.20'de durur ki elips ayak cizgisine otursun;
            // -0.06'da karakter havada suzuluyordu (denetim).
            golgeGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            golgeGo.transform.localScale = new Vector3(0.95f, 0.6f, 1f);
            var gsr = golgeGo.AddComponent<SpriteRenderer>();
            gsr.sprite = Karo64.Al(Sayfa64.Ek, Karo64.Golge);
            gsr.sortingOrder = 1;

            var gorselGo = new GameObject("KarakterGorsel");
            gorselGo.transform.SetParent(kok, false);
            gorselGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var sr = gorselGo.AddComponent<SpriteRenderer>();
            sr.sprite = Karo64.Al(Sayfa64.Ek, Karo64.Oyuncu(kadin, renk, 0));
            sr.sortingOrder = 2;
            return gorselGo.transform;
        }

        private void PartnerGorseliKur()
        {
            var go = new GameObject("PartnerGorsel");
            go.transform.SetParent(transform, false);
            _partnerGorsel = go.transform;
            _partnerGovde = go;
            PartnerGorseliniBoya(_partnerRenk);
            go.SetActive(false);
        }

        private GameObject _partnerGovde;

        /// <summary>
        /// Partner avatarini kendi rengiyle cizer. Renk yalnizca partner katildiginda
        /// bilinir; iki oyuncunun ayirt edilebilirligi tamamen renge bagli (B.7).
        /// </summary>
        private void PartnerGorseliniBoya(int renk)
        {
            if (_partnerGovde == null) return;
            for (int i = _partnerGovde.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_partnerGovde.transform.GetChild(i).gameObject);
            }
            // Partnerin cinsiyet secimi istemcide bilinmez (sunucu saklamaz);
            // gorsel ayrim icin benim secimimin TERSI cizilir, kiyafeti KENDI rengi (B.7).
            bool pKadin = PlayerPrefs.GetString("vadi_karakter", "erkek") != "kadin";
            Transform pGorsel = KarakterSpriteKur(_partnerGovde.transform, pKadin, renk);
            // Partner konumu Lerp ile disaridan suruldugu icin adim animasyonunu
            // SpriteYurutucu olcerek oynatir.
            var yurutucu = _partnerGovde.GetComponent<SpriteYurutucu>();
            if (yurutucu == null) yurutucu = _partnerGovde.AddComponent<SpriteYurutucu>();
            yurutucu.Kur(
                pGorsel.GetComponent<SpriteRenderer>(), pGorsel,
                Karo64.Al(Sayfa64.Ek, Karo64.Oyuncu(pKadin, renk, 0)),
                Karo64.Al(Sayfa64.Ek, Karo64.Oyuncu(pKadin, renk, 1)),
                Karo64.Al(Sayfa64.Ek, Karo64.Oyuncu(pKadin, renk, 2)));
            _partnerRenk = renk;
        }

        private void HudKur()
        {
            var hudGo = new GameObject("Hud");
            hudGo.transform.SetParent(transform, false);
            _hud = hudGo.AddComponent<Hud>();
            _hud.Kur(_oyunKanvasi.transform, _durum, _denge);
            _hud.TohumSecildi += tohum => _etkilesim.SeciliTohum = tohum;
            _etkilesim.SeciliTohum = _hud.SeciliTohum;

            _joystick = hudGo.AddComponent<Joystick>();
            _joystick.Kur(_oyunKanvasi);

            _canta = new CantaEkrani(_ustKanvas.transform, _durum, _denge);
            _depo = new DepoEkrani(_ustKanvas.transform, _durum, _denge);
            _tezgah = new TezgahEkrani(_ustKanvas.transform, _durum, _denge);
            _defter = new DefterEkrani(_ustKanvas.transform, _durum);
            _harita = new HaritaEkrani(_ustKanvas.transform, _durum, _denge);
            _davet = new DavetEkrani(_ustKanvas.transform);
            _ayarlar = new AyarlarEkrani(_ustKanvas.transform, _ses);

            _tren = new TrenEkrani(_ustKanvas.transform, _durum, _denge);
            // Niyet PANODAKI sefere baglanir: tren tam sinirda kalkarsa sunucu
            // bayat panoyu ACIKCA reddeder, oyuncu gormedigini onaylamaz.
            _tren.Karsila += kart => Gonder(
                "tren_karsila",
                NiyetHedef.Dugum("gar", kart, 0),
                new NiyetEk { Sefer = _tren.AcikSefer });
            _tren.Yenile += () => TrenTazele(ac: false);
            _market = new MarketEkrani(_ustKanvas.transform, _durum, _denge);
            _market.SatIstegi += (esya, adet) =>
                Gonder("sat", null, new NiyetEk { EsyaId = esya, Adet = adet });
            _market.AlIstegi += (esya, adet) =>
                Gonder("satin_al", null, new NiyetEk { EsyaId = esya, Adet = adet });

            _notTahtasi = new NotTahtasiEkrani(_ustKanvas.transform, _durum, _denge);
            _notTahtasi.Gonder += (sablon, nesne) =>
            {
                // WS kapaliyken "asildi" demek yalan olurdu: not yayin, kuyruga girmez.
                if (_baglanti == null || !_baglanti.Bagli)
                {
                    _bildirimler.Goster("Bağlantı yok — not asılamadı.", uyari: true);
                    return;
                }
                _baglanti.NotTahtasi(sablon, nesne);
                // Kendi notumun da onayini gorurum: "asildi mi?" belirsiz kalmaz.
                _bildirimler.Goster("Tahtaya asıldı: " + NotTahtasiEkrani.Metin(sablon, NesneAdi(nesne)));
            };
            _sohbet = new SohbetEkrani(_ustKanvas.transform);
            _sohbet.Gonder += metin => _baglanti.SohbetGonder(metin);
            _sohbet.NotTahtasi += () => _notTahtasi.Ac();
            _sohbet.Ifade += ifadeId =>
            {
                if (_baglanti == null || !_baglanti.Bagli)
                {
                    _bildirimler.Goster("Bağlantı yok — ifade gönderilemedi.", uyari: true);
                    return;
                }
                _baglanti.SohbetGonder("ifade:" + ifadeId);
                // Kendi balonum aninda ucar (iyimser istemci); partnerinki
                // SohbetMesaji uzerinden gelir.
                IfadeBalonu(benim: true, ifadeId);
            };
            // Kusluk manuel bir ILETISIM jestidir (B.8): sohbet panelinden yonetilir.
            _sohbet.KuslukDegistir += () =>
            {
                bool aktif = _durum.Vadi != null && _durum.Vadi.KuslukAktif;
                Gonder(aktif ? "kusluk_bitir" : "kusluk_baslat");
            };

            _hud.PartnerTiklandi += () =>
            {
                if (_durum.Partner != null)
                {
                    _sohbet.Ac(_durum.Vadi != null && _durum.Vadi.KuslukAktif, true);
                }
                else if (_durum.Ben != null && _durum.Ben.Seviye >= 3)
                {
                    _davet.Ac();
                }
                else
                {
                    _bildirimler.Goster("Partner daveti Seviye 3'te açılır.");
                }
                _ses.Cal(SesEtiketi.UiAcilis);
            };
            _hud.SohbetAcildi += () =>
            {
                _sohbet.Ac(_durum.Vadi != null && _durum.Vadi.KuslukAktif, _durum.Partner != null);
                _ses.Cal(SesEtiketi.UiAcilis);
            };
            _hud.CantaAcildi += () => { _canta.Ac(); _ses.Cal(SesEtiketi.UiAcilis); };
            _hud.HaritaAcildi += () => { _harita.Ac(); _ses.Cal(SesEtiketi.UiAcilis); };
            _hud.DefterAcildi += () => { _defter.Ac(); _ses.Cal(SesEtiketi.UiAcilis); };
            _hud.AyarlarAcildi += () => { _ayarlar.Ac(); _ses.Cal(SesEtiketi.UiAcilis); };

            _canta.DepoyaGonder += esyaId =>
            {
                if (esyaId == null) Gonder("depoya_bosalt");
                else Gonder("depoya_koy", ek: new NiyetEk { EsyaId = esyaId });
            };
            _canta.TohumSec += tohum => _etkilesim.SeciliTohum = tohum;

            _depo.DepodanAl += (esyaId, adet) =>
                Gonder("depodan_al", ek: new NiyetEk { EsyaId = esyaId, Adet = adet });
            _depo.KilitDegistir += esyaId =>
                Gonder("depo_kilit", ek: new NiyetEk { EsyaId = esyaId });
            _depo.DepoyaBosalt += () => Gonder("depoya_bosalt");

            _tezgah.Craftla += tarifId => Gonder("craft", ek: new NiyetEk { TarifId = tarifId });

            _harita.BolgeSecildi += BolgeyeGit;
            // Harita paneline vadinin kus bakisi dokusu (bos parsomen kalmasin).
            _harita.MinimapVer(_vadi.MinimapPikselleri(4, out int mw, out int mh), mw, mh);

            _davet.KodIste += () => _rest.DavetKodu(sonuc =>
            {
                if (sonuc.Basarili)
                {
                    try
                    {
                        var nesne = Newtonsoft.Json.Linq.JObject.Parse(sonuc.Veri);
                        _davet.KodGoster((string)nesne["kod"]);
                    }
                    catch (Exception)
                    {
                        _davet.HataGoster("Kod okunamadı.");
                    }
                }
                else _davet.HataGoster(sonuc.HataMesaji ?? "Kod üretilemedi.");
            });
            _davet.Katil += kod => _rest.Katil(kod, sonuc =>
            {
                if (sonuc.Basarili)
                {
                    _bildirimler.Goster("Vadiye katıldın! Ortak vadi yükleniyor...");
                    // Eski soket ESKI vadiye kayitli (sunucu uyeligi BAGLAN aninda
                    // sabitler): kapatilmadan yeni vadiye gecilmez. Durum da
                    // sifirlanir ki eski revizyon yeni vadiye delta tabani olmasin.
                    _baglanti.Durdur();
                    _durum.Sifirla();
                    _baglanti.Baslat(_rest.ErisimJetonu);
                }
                else _davet.HataGoster(sonuc.HataMesaji ?? "Katılamadı.");
            });
            _davet.HesapBagla += () =>
            {
                // Dikey dilimde gercek OAuth yok: kurtarma kodu ureterek ayni sarti karsilar (B.19).
                _rest.KurtarmaKodu(sonuc =>
                {
                    if (!sonuc.Basarili)
                    {
                        _davet.HataGoster(sonuc.HataMesaji ?? "Kod alınamadı.");
                        return;
                    }
                    try
                    {
                        var nesne = Newtonsoft.Json.Linq.JObject.Parse(sonuc.Veri);
                        _bildirimler.Goster("Kurtarma kodun: " + (string)nesne["kod"] + " — bir yere yaz!", false, 12f);
                        _davet.KodGoster(null);
                    }
                    catch (Exception)
                    {
                        _davet.HataGoster("Kod okunamadı.");
                    }
                });
            };

            _ayarlar.AdresDegisti += adres =>
            {
                SunucuAyari.AdresYaz(adres);
                _bildirimler.Goster("Sunucu adresi kaydedildi. Oyunu yeniden başlat.");
            };
        }

        // ================================================================== eylemler

        private void Gonder(string tip, NiyetHedef hedef = null, NiyetEk ek = null)
        {
            if (_baglanti == null) return;
            _baglanti.NiyetGonder(Niyet.Olustur(tip, hedef, ek));
        }

        /// <summary>
        /// Tren panosunu sunucudan ceker (mimari kural 7: istemci fiyat HESAPLAMAZ,
        /// yalnizca gosterir). Basarisizlik sessiz kalmaz.
        /// </summary>
        private void TrenTazele(bool ac = true)
        {
            string vadiId = _durum.Vadi != null ? _durum.Vadi.Id : null;
            if (vadiId == null) return;
            _rest.TrenDurumu(vadiId, s =>
            {
                if (s.Basarili) _tren.Goster(s.Veri, ac);
                else _bildirimler.Goster("Tren panosu alınamadı: " + s.HataMesaji, uyari: true);
            });
        }

        /// <summary>
        /// Vadi haritasindan dogrudan gar'a gelindiginde ilk senkron henuz
        /// gelmemis olabilir — pano SESSIZCE acilmamak yerine senkronu bekler.
        /// </summary>
        private IEnumerator GarPanosunuAc()
        {
            for (int i = 0; i < 16 && _durum.Vadi == null; i++)
            {
                yield return new WaitForSecondsRealtime(0.5f);
            }
            if (_durum.Vadi == null)
            {
                _bildirimler.Goster("Tren panosu için bağlantı bekleniyor...", uyari: true);
                yield break;
            }
            TrenTazele(ac: true);
        }

        /// <summary>Not tahtasi nesnesi esya DA bolge DE olabilir — adi dogru sozlukten cozulur.</summary>
        private string NesneAdi(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _denge.BolgeAcilisSeviyesi(id) < 99 ? _denge.BolgeAdi(id) : _denge.EsyaAdi(id);
        }

        /// <summary>
        /// Ifade balonu: konusanin BASININ USTUNDE ucar (B.9 tekerlek). Gorsel
        /// sahnede yoksa (partner baska bolgede) bildirime duser — ifade kaybolmaz.
        /// </summary>
        private void IfadeBalonu(bool benim, string ifadeId)
        {
            string yazi = SohbetEkrani.IfadeAdi(ifadeId);
            Transform kok = benim
                ? (_karakter != null ? _karakter.transform : null)
                : _partnerGorsel;
            if (kok == null || !kok.gameObject.activeInHierarchy)
            {
                if (!benim)
                {
                    var p = _durum.Partner;
                    _bildirimler.Goster((p != null ? p.Ad : "Eşin") + ": " + yazi);
                }
                return;
            }
            _efekt.UcanYazi((Vector2)kok.position + new Vector2(0f, 1.35f), yazi, Palet.KapsulAltin);
            if (!benim) _ses.Cal(SesEtiketi.UiOnay);
        }

        private void BolgeyeGit(string bolgeId)
        {
            _aktifBolge = bolgeId;
            Gonder("bolgeye_gir", NiyetHedef.Bolgeye(bolgeId));

            // Bolge gecisinde 0.5-1 sn kararma (B.4). Gizlemeye calisilmaz.
            StartCoroutine(BolgeGecisi(bolgeId));
        }

        private IEnumerator BolgeGecisi(string bolgeId)
        {
            _yukleme.Goster(_denge.BolgeAdi(bolgeId) + " yükleniyor...");
            yield return new WaitForSecondsRealtime(0.6f);
            _yukleme.Gizle();

            // Dikey dilimde yalnizca tarla ve ev cizilir; digerleri panel olarak acilir.
            switch (bolgeId)
            {
                case "depo":
                    _depo.Ac();
                    break;
                case "ev":
                    _tezgah.Ac();
                    _ses.OrtamBaslat(SesEtiketi.OrtamEv);
                    break;
                case "gar":
                    StartCoroutine(GarPanosunuAc());
                    _ses.OrtamBaslat(SesEtiketi.OrtamTarla);
                    break;
                case "market":
                    _market.Ac();
                    _ses.OrtamBaslat(SesEtiketi.OrtamTarla);
                    break;
                default:
                    _ses.OrtamBaslat(SesEtiketi.OrtamTarla);
                    break;
            }
        }

        /// <summary>
        /// Uzun bas -> karo inceleme. Ham enum adi sizdiriyordu (denetim);
        /// simdi urun adi + durum + kalan sure Turkce soylenir.
        /// </summary>
        private void DaireselMenu(Vector2Int karo)
        {
            var k = _durum.Karo(karo.x, karo.y);
            if (k != null && !k.Bos)
            {
                string ad = _denge.EsyaAdi(k.UrunId);
                if (_tarla.Olgun(k)) _bildirimler.Goster(ad + " — olgun, hasat et!");
                else if (k.SulandiAt == null) _bildirimler.Goster(ad + " — susuz, sula.");
                else _bildirimler.Goster(ad + " — " + UiKit.SureMetni(_tarla.KalanSn(k)) + " kaldı.");
                return;
            }
            switch (_etkilesim.EylemBelirle(karo))
            {
                case EtkilesimYoneticisi.Eylem.Ek: _bildirimler.Goster("Boş karo — seçili tohumu ekebilirsin."); break;
                case EtkilesimYoneticisi.Eylem.KovaDoldur: _bildirimler.Goster("Kuyu — kovayı doldur."); break;
                case EtkilesimYoneticisi.Eylem.Topla: _bildirimler.Goster("Yabani kaynak — toplanabilir."); break;
                default: _bildirimler.Goster("Burada yapılacak bir şey yok."); break;
            }
        }

        private void OlayIsle(Ag.Olay olay)
        {
            switch (olay.Tip)
            {
                case "seviye_atlama":
                    int seviye = olay.Sayi ?? 0;
                    _seviyeEkrani.Goster(seviye, SeviyeAcilimi(seviye));
                    _ses.Cal(SesEtiketi.SeviyeAtlama);
                    Haptik.Haptik.Calistir(Haptik.Haptik.Desen.UcluYukselen);
                    // B.18 ~10:00: ogreticinin vardigi yer partner davetidir —
                    // Sv 3 kutlamasindan sonra davet KENDILIGINDEN acilir
                    // (kapatilabilir; sonra partner kapsulunden geri donulur).
                    if (seviye == 3 && _durum.Partner == null)
                    {
                        StartCoroutine(DavetiGecikmeliAc());
                    }
                    break;

                case "defter_sayfasi":
                    _bildirimler.Goster("Defterde yeni sayfa: " + olay.Sayi);
                    _ses.Cal(SesEtiketi.DefterSayfasi);
                    break;

                case "bolge_acildi":
                    _bildirimler.Goster(_denge.BolgeAdi(olay.Deger) + " açıldı.");
                    break;

                case "alet_kirildi":
                    // ⚠ Sessizce kirilan alet ofke uretir (B.14). Cift darbe + gorsel + yazi.
                    _bildirimler.Goster(_denge.EsyaAdi(olay.Deger) + " kırıldı!", uyari: true, sure: 5f);
                    _ses.Cal(SesEtiketi.AletKirilma);
                    Haptik.Haptik.Calistir(Haptik.Haptik.Desen.CiftDarbe);
                    break;

                case "alet_dayaniklilik_dustu":
                    _bildirimler.Goster(
                        _denge.EsyaAdi(olay.Deger) + " yıpranıyor (%20 altı).", uyari: true, sure: 5f);
                    break;

                case "depo_doldu":
                    _bildirimler.Goster("Depo dolu — hasat engellendi. Ürün tarlada bekliyor.", true, 5f);
                    break;

                case "tren_siparis_karsilandi":
                    // B.1: "hicbir is tek basina gorulmez" — partnerin teslimati da
                    // KENDI adiyla kutlanir, "sen teslim ettin" yalani soylenmez.
                    bool benimTeslimatim = olay.Deger == null || olay.Deger == _durum.BenimId;
                    if (benimTeslimatim)
                    {
                        _bildirimler.Goster("Sipariş teslim edildi — tren düdüğü senin için çaldı!");
                        Haptik.Haptik.Calistir(Haptik.Haptik.Desen.UcluYukselen);
                    }
                    else
                    {
                        string teslimEden =
                            _durum.Oyuncular.TryGetValue(olay.Deger, out var te) ? te.Ad : "Eşin";
                        _bildirimler.Goster(teslimEden + " bir siparişi teslim etti — manifesto ilerliyor!");
                    }
                    _ses.Cal(SesEtiketi.UiOnay);
                    if (_tren.Acik) TrenTazele(ac: false);
                    break;

                case "manifesto_tamam":
                    // B.1: "hicbir is tek basina gorulmez" — manifesto IKI imzanin isi,
                    // kutlama da ikisine birden gider (sunucu iki tarafa da XP yazar).
                    _bildirimler.Goster(
                        "Manifesto 3/3 tamam! Bonus XP ikinize de yazıldı.", false, 8f);
                    _ses.Cal(SesEtiketi.SeviyeAtlama);
                    Haptik.Haptik.Calistir(Haptik.Haptik.Desen.UcluYukselen);
                    break;

                case "ilk_kez":
                    // B.26 yasak 17: yalniz sesle bildirim olmaz — gorsel es art.
                    _ses.Cal(SesEtiketi.UiOnay);
                    _bildirimler.Goster("İlk kez! " + IlkKezMetni(olay.Deger));
                    Haptik.Haptik.Calistir(Haptik.Haptik.Desen.CokHafif);
                    break;
            }
        }

        private IEnumerator DavetiGecikmeliAc()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (_durum.Partner == null) _davet.Ac();
        }

        private static string IlkKezMetni(string isaret)
        {
            switch (isaret)
            {
                case "ilk_ekim": return "İlk tohumun toprakta.";
                case "ilk_sulama": return "İlk sulaman.";
                case "ilk_hasat": return "İlk hasadın!";
                case "ilk_toplama": return "İlk yabani toplaman.";
                case "ilk_craft": return "İlk aletin!";
                case "ilk_depo": return "Depoya ilk katkın.";
                default: return "Yeni bir şey denedin, XP kazandın.";
            }
        }

        private string SeviyeAcilimi(int seviye)
        {
            switch (seviye)
            {
                case 2: return "Depo açıldı · Turp";
                case 3: return "Partner daveti · Havuç";
                case 4: return "Mısır · Çanta 12 slot";
                case 5: return "Tren garı · Patates · Para oyuna girdi";
                default: return "";
            }
        }

        // ================================================================== her kare

        /// <summary>Konum 5 Hz yayinlanir ve revizyon URETMEZ (C.3).</summary>
        private void KonumYayinla()
        {
            if (_baglanti == null || !_baglanti.Bagli || _karakter == null) return;
            if (Time.unscaledTime < _sonrakiKonumYayini) return;
            _sonrakiKonumYayini = Time.unscaledTime + 1f / Mathf.Max(1, _denge.KonumYayinHz);

            Vector3 p = _karakter.transform.position;
            _baglanti.KonumGonder(_aktifBolge, new Vector2(p.x, p.y), 0f);

            PartnerKonumunuUygula();
        }

        private void PartnerKonumunuUygula()
        {
            var partner = _durum.Partner;
            if (partner != null && partner.Renk != _partnerRenk) PartnerGorseliniBoya(partner.Renk);
            if (partner == null || _partnerGorsel == null)
            {
                if (_partnerGorsel != null) _partnerGorsel.gameObject.SetActive(false);
                return;
            }
            if (!_durum.PartnerKonumlari.TryGetValue(partner.Id, out WsKonum konum))
            {
                _partnerGorsel.gameObject.SetActive(false);
                return;
            }
            // Partner baska bolgedeyse ham koordinati burada cizmek "hayalet
            // avatar" uretir (denetim) — gizlenir.
            if (!string.IsNullOrEmpty(konum.Bolge) && konum.Bolge != _aktifBolge)
            {
                _partnerGorsel.gameObject.SetActive(false);
                return;
            }
            _partnerGorsel.gameObject.SetActive(true);
            // 5 Hz gelen konum ARA DEGERLERLE yumusatilir; yoksa partner ziplayarak yurur.
            Vector3 hedef = new Vector3(konum.X, konum.Y, 0f);
            _partnerGorsel.position =
                Vector3.Lerp(_partnerGorsel.position, hedef, Time.deltaTime * 8f);
        }

        private void JoystickGuncelle()
        {
            if (_joystick == null || _karakter == null) return;

            Girdi.Girdi.Temas[] temaslar = Girdi.Girdi.Temaslar(out int adet);
            for (int i = 0; i < adet; i++)
            {
                var t = temaslar[i];
                if (t.Basladi) _joystick.Basildi(t.Id, t.Konum);
                else if (t.Suruyor) _joystick.Suruklendi(t.Id, t.Konum);
                else if (t.Bitti) _joystick.Birakildi(t.Id);
            }

            if (!_joystick.Aktif || _joystick.Yon == Vector2.zero) return;

            // Joystick karo karo yurutmez, dogrudan iter: gezinmek icin daha akici.
            // ⚠ Carpisma karo bazli ve KAYDIRMALI: hedef hucre kapaliysa once
            // yalniz X, sonra yalniz Y denenir — karakter engellere yapismaz.
            float hiz = 1f / Mathf.Max(0.01f, _denge.YurumeKaroSn);
            _karakter.Dur();
            Vector2 adim = _joystick.Yon * (hiz * Time.deltaTime);
            Vector2 simdiki = _karakter.transform.position;
            Vector2 aday = simdiki + adim;
            if (!KaroGecer(aday))
            {
                Vector2 sadeceX = simdiki + new Vector2(adim.x, 0f);
                Vector2 sadeceY = simdiki + new Vector2(0f, adim.y);
                if (KaroGecer(sadeceX)) aday = sadeceX;
                else if (KaroGecer(sadeceY)) aday = sadeceY;
                else return;
            }
            _karakter.transform.position = new Vector3(aday.x, aday.y, 0f);
            _karakter.DisYuruyus();
        }

        private bool KaroGecer(Vector2 konum) =>
            _izgara.Gecilebilir(Mathf.RoundToInt(konum.x), Mathf.RoundToInt(konum.y));

        // ================================================================== disaridan

        public void DavetEkraniniAc() => _davet.Ac();

        public void DepoAc() => _depo.Ac();

        public void TezgahAc() => _tezgah.Ac();
    }
}
