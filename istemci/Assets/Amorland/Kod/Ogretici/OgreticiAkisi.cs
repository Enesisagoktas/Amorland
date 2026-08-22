using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Arayuz;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.Ogretici
{
    /// <summary>
    /// Ogretici akisi (B.18).
    ///
    /// ⚠ Ilk 15 dakikanin isi <b>Sv 5'e ulasmak degil</b>, oyuncuyu 10. dakikada
    /// PARTNER DAVETINE getirmek. Ogreticinin kendisi 4 dakikayi gecmemeli.
    ///
    /// ⚠ Yontem: <b>parlayan hedef + tek satir</b>. Modal diyalog YOK, zorla el tutma YOK.
    /// Oyuncu adimi atlarsa oyun devam eder; ipucu ustte durur ve kosul saglaninca duser.
    ///
    /// ⚠ Ilk 30 dakikada reklam YOK (B.24) — bu akista hicbir reklam noktasi yoktur.
    /// </summary>
    public class OgreticiAkisi : MonoBehaviour
    {
        private DunyaDurumu _durum;
        private DengeVerisi _denge;
        private Hud _hud;
        private EfektYoneticisi _efekt;

        private RectTransform _serit;
        private Text _yazi;
        private int _adim = -1;

        // Surumlu anahtar: hedef zinciri eklenince eski "bitti" bayragi
        // gecersizdir — isaret tabanli kosullar dogru adima kendiliginden oturur.
        private const string TamamlandiAnahtari = "amorland.ogretici.bitti.v2";

        /// <summary>
        /// Ipucu seridinin olculeri. Genislik 760'tan 620'ye indi: 760 px'lik serit
        /// sol ust oyuncu kapsulunun (x 14..310) uzerine biniyordu.
        /// </summary>
        private const float SeritGenislik = 620f;

        private const float SeritYukseklik = 48f;

        private class Adim
        {
            public string Metin;
            public Func<DunyaDurumu, bool> Kosul;

            /// <summary>Dunyada gosterilecek hedef (ziplayan altin ok); yoksa null.</summary>
            public Vector2? Hedef;

            /// <summary>
            /// ZORUNLU mod (oyuncu istegi): bu adimda hangi dunya dokunusuna izin
            /// var. null = adim dunya dokunusu istemiyorsa Serbest karar verir.
            /// </summary>
            public Func<DunyaDurumu, Vector2Int, bool> DokunusIzni;

            /// <summary>Dinamik ok hedefi (dal gibi); null ise sabit Hedef kullanilir.</summary>
            public Func<DunyaDurumu, Vector2?> OkHedefi;

            /// <summary>Adim basinda oyuncu depoya tasinir (bolge gecisi).</summary>
            public bool DepoyaGotur;

            /// <summary>true = dunya dokunuslari serbesttir (son adim: serbest oyun).</summary>
            public bool Serbest;
        }

        /// <summary>Adim basinda karakter hedefe ZORLA yurutulur (dunya karosu).</summary>
        public event Action<Vector2Int> HedefeYurut;

        /// <summary>Depo adimi basinda oyuncu depoya tasinir.</summary>
        public event Action DepoyaGit;

        /// <summary>Aktif adimin yonergesi — kilit reddinde oyuncuya tekrar soylenir.</summary>
        public string AktifMetin =>
            _adim >= 0 && _adim < _adimlar.Count ? _adimlar[_adim].Metin : "";

        /// <summary>
        /// ZORUNLU ogretici kilidi: aktif adimin hedefi disindaki dunya dokunusu
        /// islenmez. Ogretici bittiyse ya da adim serbestse her dokunus gecer.
        /// </summary>
        public bool DokunusUygun(Vector2Int karo)
        {
            if (_adim < 0 || _adim >= _adimlar.Count) return true;
            var a = _adimlar[_adim];
            if (a.Serbest) return true;
            if (a.DokunusIzni != null) return a.DokunusIzni(_durum, karo);
            // Dunya dokunusu istemeyen adim (depo paneli): dunya kilitli.
            return !a.DepoyaGotur;
        }

        /// <summary>Kilit reddinde serit nefes alir — oyuncunun gozu yonergeye doner.</summary>
        public void Vurgula()
        {
            if (_adim < 0 || !_serit.gameObject.activeSelf) return;
            StopAllCoroutines();
            StartCoroutine(SeritPulse());
        }

        /// <summary>Ilk hazir yabani dugum (dal onceligi) — ok ve yurutme hedefi.</summary>
        private Vector2Int? HazirYabani()
        {
            Vector2Int? aday = null;
            foreach (var d in _durum.Dugumler.Values)
            {
                if (!"yabani".Equals(d.Sinif) || !d.Hazir(_durum.SunucuZamani)) continue;
                var k = new Vector2Int(d.X, d.Y);
                if ((d.Tur ?? "").Contains("dal")) return k;
                if (aday == null) aday = k;
            }
            return aday;
        }

        private bool HazirYabaniMi(Vector2Int karo)
        {
            foreach (var d in _durum.Dugumler.Values)
            {
                if (d.X == karo.x && d.Y == karo.y)
                    return "yabani".Equals(d.Sinif) && d.Hazir(_durum.SunucuZamani);
            }
            return false;
        }

        private readonly List<Adim> _adimlar = new List<Adim>();

        public void Kur(
            DunyaDurumu durum, DengeVerisi denge, Transform kanvas, Hud hud, EfektYoneticisi efekt)
        {
            _durum = durum;
            _denge = denge;
            _hud = hud;
            _efekt = efekt;

            SeritKur(kanvas);
            AdimlariKur();

            if (PlayerPrefs.GetInt(TamamlandiAnahtari, 0) == 1)
            {
                _serit.gameObject.SetActive(false);
                return;
            }
            _adim = 0;
            AdimGoster();
        }

        private void SeritKur(Transform kanvas)
        {
            // ⚠ Ipucu dunyanin UZERINDE yuzer, oyuncunun actigi bir modal degildir:
            // dolayisiyla parsomen degil HUD KAPSUL dilini konusur (B.22) — koyu govde,
            // acik krem cerceve, beyaz konturlu yazi. Cercevesiz duz serit tarlanin koyu
            // bolgelerinde kayboluyordu; okunurlugu tasiyan sey govde degil, o acik hat.
            //
            // ⚠ Kapsul GORUNMEZ bir kabugun icinde durur ve gizlenen sey kabuktur:
            // GolgeEkle golgeyi KARDES olarak birakir, yani kapsulu tek basina kapatmak
            // ogretici bittikten sonra ekranda asili bir kara leke birakirdi.
            //
            // ⚠ Serit ust HUD siranin ALTINA konur: -16'da 760 px genisliginde dururken
            // ust orta altin kapsulunu (0.5, 1, -14, 150x46) tamamen ortuyordu ve
            // ogretici kanvasi (sira 50) HUD'un (sira 10) uzerine cizildigi icin sayac
            // gorunmuyordu. -70 altin kapsulun alt kenarinin (-60) altinda kalir.
            _serit = UiKit.Panel(kanvas, "OgreticiSeritKabuk", Color.clear);
            UiKit.Sabitle(_serit, 0.5f, 1f, 0f, -70f, SeritGenislik, SeritYukseklik);

            RectTransform ic;
            var kapsul = UiKit.CerceveliKart(
                _serit, "OgreticiSerit", Palet.KapsulZemin, Palet.KapsulCerceve, out ic, 20, 3f);
            UiKit.Doldur(kapsul);
            UiKit.GolgeEkle(kapsul, 20, 14, 4f);
            // Ipucu tiklamayi yutarsa altindaki karoya basilamaz.
            kapsul.gameObject.GetComponent<Image>().raycastTarget = false;

            // Her kapsul bir ikon tasir (B.22). ⚠ YER TUTUCU sekil — gercek ikon 2B
            // varlik olarak sonra gelecek.
            var ikon = UiKit.Panel(ic, "Ikon", Palet.UiAltin);
            var ikonImg = ikon.gameObject.GetComponent<Image>();
            ikonImg.sprite = Sekil.Yildiz();
            ikonImg.raycastTarget = false;
            UiKit.Sabitle(ikon, 0f, 0.5f, 12f, 0f, 24f, 24f);

            // Koyu kapsulde yazi BEYAZ ve KONTURLU: kontursuz acik yazi kapsulun yari
            // saydam govdesinden gecen parlak cim uzerinde kenarini kaybediyordu.
            _yazi = UiKit.KonturluYazi(
                ic, "Yazi", "", UiKit.YaziGovde, Color.white, TextAnchor.MiddleCenter, true, 1.3f);
            var yaziRt = _yazi.rectTransform;
            yaziRt.anchorMin = Vector2.zero;
            yaziRt.anchorMax = Vector2.one;
            // Soldaki bosluk ikonun yeri; esit kenarli Doldur yaziyi ikonun ustune bindirirdi.
            yaziRt.offsetMin = new Vector2(44f, 6f);
            yaziRt.offsetMax = new Vector2(-16f, -6f);
        }

        private void AdimlariKur()
        {
            // Tek satir, emir kipi + DUNYADA ZIPLAYAN OK (oyuncu bulgusu: "nasil
            // ekecegim, nasil su dolduracagim belli degil" — metin tek basina
            // yetmiyor, hedef dunyada gosterilir). Akis kova-dolu baslangica gore:
            // ek -> sula -> kuyu -> dal -> hasat -> depo -> partner. Eski ilk iki
            // adim (defter/bolge) oyuncunun elinde olmayan sunucu isaretlerine
            // bagliydi; kaldirildi.
            _adimlar.Add(new Adim
            {
                Metin = "Altın okun gösterdiği boş karoya dokun — karakterin yürür ve tohum eker.",
                Kosul = d => d.IsaretVar("ilk:ilk_ekim"),
                Hedef = new Vector2(0f, 0f),
                DokunusIzni = (d, k) => k == new Vector2Int(0, 0)
            });
            _adimlar.Add(new Adim
            {
                Metin = "Ektiğin karoya BİR DAHA dokun — sular. Islak toprak koyulaşır.",
                Kosul = d => d.IsaretVar("ilk:ilk_sulama"),
                Hedef = new Vector2(0f, 0f),
                DokunusIzni = (d, k) => k == new Vector2Int(0, 0)
            });
            _adimlar.Add(new Adim
            {
                Metin = "Kovan azaldı. Kuyuya dokun — karakterin doldurur.",
                // ⚠ Kovasiz hesap (eski kayit) bu adimda KILITLENIYORDU ve akis
                // "sirasiz" gorunuyordu (oyuncu bulgusu) — kovasizsa adim atlanir;
                // sunucu iyilestirmesi zaten girista kova verir.
                // Kova durumu KALICI degil: sonradan bosalan kova, yeniden
                // giriste bu adimi yanlis yere geri getiriyordu. Hasat izi varsa
                // kuyu dersi coktan alinmistir (ayni sinif hata: dal adimi).
                Kosul = d => d.IsaretVar("ilk:ilk_hasat")
                             || (d.Ben != null && (d.Ben.KovaKapasite <= 0
                                 || d.Ben.KovaKalan >= d.Ben.KovaKapasite)),
                Hedef = new Vector2(TarlaGorunumu.KuyuKonumu.x, TarlaGorunumu.KuyuKonumu.y),
                // Kuyu 2 karo boyunda tek parca: cevresine 1 karo tolerans.
                DokunusIzni = (d, k) =>
                    Mathf.Abs(k.x - (int)TarlaGorunumu.KuyuKonumu.x) <= 1
                    && Mathf.Abs(k.y - (int)TarlaGorunumu.KuyuKonumu.y) <= 1
            });
            _adimlar.Add(new Adim
            {
                Metin = "Parlayan dallara dokun — odun topla, çantana girer.",
                // ⚠ Kosul KALICI olmali: envantere bakmak, odun depoya/craft'a
                // gidince adimi yeniden aciyordu ve dallar tukenmisken oyuncu
                // "odun topla" karsisinda kilitleniyordu (oyuncu bulgusu).
                // Isaret sunucudan gelir; eski kayitlar icin canta/depo kaniti da
                // kabul edilir (isaret gecmise donuk yazilamaz).
                Kosul = d => d.IsaretVar("ilk:ilk_toplama")
                             || d.EnvanterAdet("odun") > 0
                             || d.Depo.ContainsKey("odun"),
                Hedef = new Vector2(-2f, 1f),
                // HERHANGI hazir yabani kabul: tek dala kilitlemek, o dal
                // toplanmissa oyuncuyu acmaza sokardi. Hic hazir dugum yoksa
                // kilit uygulanmaz (30 dk yenilenme emniyeti).
                DokunusIzni = (d, k) => HazirYabani() == null || HazirYabaniMi(k),
                OkHedefi = d =>
                {
                    var h = HazirYabani();
                    return h.HasValue ? new Vector2(h.Value.x, h.Value.y) : (Vector2?)null;
                }
            });
            _adimlar.Add(new Adim
            {
                Metin = "Buğday 5 dakikada büyür; PARLAYINCA dokun — hasat.",
                Kosul = d => d.IsaretVar("ilk:ilk_hasat"),
                Hedef = new Vector2(0f, 0f),
                DokunusIzni = (d, k) => k == new Vector2Int(0, 0)
            });
            _adimlar.Add(new Adim
            {
                Metin = "Hasadı depoya taşı: açılan panelde 'Çantayı depoya boşalt'a bas.",
                Kosul = d => d.IsaretVar("ilk:ilk_depoya_bosaltma"),
                DepoyaGotur = true
            });
            // ⚠ Kilit tasi (B.18 ~10:00): ogreticinin GERCEK hedefi bu satir.
            _adimlar.Add(new Adim
            {
                Metin = "Bu vadide hiçbir iş tek başına görülmez. Sv 3'te eşin için sol üst kapsüle dokun.",
                Kosul = d => d.Ben != null && d.Ben.Seviye >= 3,
                Serbest = true   // ogretici gorevleri bitti: dunya artik oyuncunun
            });

            // ---- HEDEF ZINCIRI (oyuncu bulgusu: "2 dakikalik gameplay") ----
            // Ogretici bitince oyuncuya kimse "simdi ne yap" demiyordu; Sv2-6
            // arasi bos hissediliyordu. Zincir spec'in kendi ilerlemesini takip
            // eder: craft -> Sv5 tren -> teslimat -> market. Hepsi SERBEST —
            // kilit yok, yalniz yol gosterir; kosullar KALICI izlere bagli.
            _adimlar.Add(new Adim
            {
                Metin = "Tarla kenarından taş topla, evdeki tezgahta TAŞ KAZMA yap.",
                Kosul = d => d.IsaretVar("craft:tas_kazma"),
                Serbest = true
            });
            _adimlar.Add(new Adim
            {
                Metin = "Bir de TAŞ BALTA yap — orman Sv 7'de seni bekliyor.",
                Kosul = d => d.IsaretVar("craft:tas_balta"),
                Serbest = true
            });
            _adimlar.Add(new Adim
            {
                Metin = "Ek, sula, biç — Sv 5'te TREN GARI açılır ve para oyuna girer.",
                Kosul = d => d.Ben != null && d.Ben.Seviye >= 5,
                Serbest = true
            });
            _adimlar.Add(new Adim
            {
                Metin = "Haritadan GAR'a git — trene bir sipariş teslim et, ilk altınını kazan.",
                Kosul = d => d.IsaretVar("ilk:ilk_tren_teslimi"),
                Serbest = true
            });
            _adimlar.Add(new Adim
            {
                Metin = "Sv 6'da MARKET açılır: fazla ürünü sat, yeni tohumlar al.",
                Kosul = d => d.IsaretVar("bolge:market"),
                Serbest = true
            });
        }

        private void Update()
        {
            if (_adim < 0 || _durum == null) return;
            if (_adim >= _adimlar.Count)
            {
                Bitir();
                return;
            }
            // WHILE, IF degil: tam senkron birden cok adimin kosulunu ayni anda
            // getirebilir (isaretler kalicidir). Tek tek ilerleseydi ogretici
            // coktan yapilmis islerde takili gorunurdu (oyuncu bulgusu).
            bool ilerledi = false;
            while (_adim < _adimlar.Count && _adimlar[_adim].Kosul(_durum))
            {
                _adim++;
                ilerledi = true;
            }
            if (_adim >= _adimlar.Count) Bitir();
            else if (ilerledi) AdimGoster();
        }

        private void AdimGoster()
        {
            var a = _adimlar[_adim];
            _serit.gameObject.SetActive(true);
            _yazi.text = (_adim + 1) + "/" + _adimlar.Count + "  ·  " + a.Metin;

            Vector2? ok = a.OkHedefi != null ? (a.OkHedefi(_durum) ?? a.Hedef) : a.Hedef;
            if (_efekt != null) _efekt.HedefOku(ok);

            // ZORLA GONDERME (oyuncu istegi): adim baslar baslamaz karakter
            // hedefe kendisi yurur — oyuncu yalnizca dokunusu yapar. Depo adimi
            // oyuncuyu bolgeye tasir; oyun acilisinda yarim kalan adim neyse
            // oyuncu dogrudan oraya goturulur.
            if (a.DepoyaGotur) DepoyaGit?.Invoke();
            else if (ok.HasValue)
                HedefeYurut?.Invoke(new Vector2Int(
                    Mathf.RoundToInt(ok.Value.x), Mathf.RoundToInt(ok.Value.y)));

            StopAllCoroutines();
            StartCoroutine(SeritPulse());
        }

        /// <summary>Yeni adim dikkat cekmeli: serit kisa bir nefes alir.</summary>
        private System.Collections.IEnumerator SeritPulse()
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float o = 1f + 0.08f * Mathf.Sin(t / 0.35f * Mathf.PI);
                _serit.localScale = new Vector3(o, o, 1f);
                yield return null;
            }
            _serit.localScale = Vector3.one;
        }

        private void Bitir()
        {
            _adim = -1;
            _serit.gameObject.SetActive(false);
            if (_efekt != null) _efekt.HedefOku(null);
            PlayerPrefs.SetInt(TamamlandiAnahtari, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Testte ogreticiyi bastan baslatmak icin.</summary>
        public static void Sifirla()
        {
            PlayerPrefs.DeleteKey(TamamlandiAnahtari);
            PlayerPrefs.Save();
        }
    }
}
