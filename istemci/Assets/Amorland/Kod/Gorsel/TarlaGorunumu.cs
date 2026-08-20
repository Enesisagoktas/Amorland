using System;
using System.Collections.Generic;
using UnityEngine;
using Amorland.Ag;
using Amorland.Durum;

namespace Amorland.Gorsel
{
    /// <summary>
    /// Tarla sahnesinin gorsel katmani: karolar, urunler, yabani kaynaklar, kuyu.
    ///
    /// ⚠ Bu sinif OYUN KURALI ICERMEZ. Yalnizca <see cref="DunyaDurumu"/>'nu cizer.
    /// Buyume asamasi burada hesaplanir ama bu bir <b>gosterim</b> hesabidir; olgunluk
    /// karari sunucuya aittir ve istemci "olgun" gorunse bile hasat reddedilebilir.
    /// Sunucu saati kullanilir (mimari kural 5) — telefon saati ileri alinirsa
    /// urun erken olgun GORUNMEZ.
    /// </summary>
    public class TarlaGorunumu : MonoBehaviour
    {
        private DunyaDurumu _durum;
        private DengeVerisi _denge;

        private Transform _kok;
        private Transform _karoKok;
        private Transform _dugumKok;

        private readonly Dictionary<string, KaroGorseli> _karolar = new Dictionary<string, KaroGorseli>();
        private readonly Dictionary<string, DugumGorseli> _dugumler = new Dictionary<string, DugumGorseli>();
        private readonly Dictionary<int, Mesh> _urunMeshOnbellegi = new Dictionary<int, Mesh>();

        private GameObject _vurgu;
        private float _pitch = 50f;
        private Transform _parselKok;
        private string _cizilenParselSiniri;
        private Mesh _karoMeshKuru;
        private Mesh _karoMeshSulu;
        private Mesh _karoMeshBos;

        private class KaroGorseli
        {
            public GameObject Zemin;
            public GameObject Urun;
            public int CizilenAsama = -1;
            public string CizilenUrun;
            public bool CizilenSulu;
            public float GeriSarmaBitis;
        }

        private class DugumGorseli
        {
            public GameObject Nesne;
            public bool CizilenHazir;
        }

        public void Kur(DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;
            _pitch = denge != null && denge.Yuklendi ? denge.KameraPitch : 50f;

            _kok = new GameObject("Tarla").transform;
            _kok.SetParent(transform, false);
            _karoKok = new GameObject("Karolar").transform;
            _karoKok.SetParent(_kok, false);
            _dugumKok = new GameObject("Dugumler").transform;
            _dugumKok.SetParent(_kok, false);

            // Karo tonlari pedin UZERINDE okunmali: bos > kuru > sulu sirasiyla koyulasir
            // ama ucu de pedden acik kalir. Sulanmis karo "yanmis" degil "nemli" gorunur.
            _karoMeshBos = MeshUretici.TarlaKarosu(Color.Lerp(Palet.CimKoyu, Palet.Cim, 0.35f));
            _karoMeshKuru = MeshUretici.TarlaKarosu(Color.Lerp(Palet.Toprak, new Color(0.78f, 0.62f, 0.44f), 0.5f));
            _karoMeshSulu = MeshUretici.TarlaKarosu(Color.Lerp(Palet.Toprak, Palet.Su, 0.22f));

            ZeminKur();
            KuyuKur();

            _vurgu = MateryalDeposu.Nesne(
                "Vurgu", MeshUretici.Cerceve(1f, 0.06f, Palet.UiVurgu), _kok, Vector3.zero);
            _vurgu.SetActive(false);

            _durum.DurumDegisti += _ => Tazele();
            _durum.GeriSarildi += GeriSarmaBaslat;
            Tazele();
        }

        /// <summary>Bir parselin en fazla buyuyecegi olcu (D: tarla.karoKademeleri son kademe 24 karo).</summary>
        private const int ParselEn = 6;
        private const int ParselBoy = 4;

        /// <summary>Partner parseli bu kadar saga kayar (KaroKonumu ile ayni deger).</summary>
        private const float ParselAraligi = 8f;

        private void ZeminKur()
        {
            // ⚠ Tek dev yesil dikdortgen YETMEZ. Oyuncu "neredeyim, sinirim nerede"
            // sorusuna gorsel cevap alamiyordu: ekran bostu, olculek yoktu, parselin
            // nerede bittigi belli degildi. Zemin artik katmanli kuruluyor.
            var zemin = MateryalDeposu.Nesne(
                "Zemin", MeshUretici.Yama(70f, 70f, Palet.Cim), _kok, new Vector3(4f, -0.03f, 2f));
            zemin.transform.localPosition = new Vector3(4f, -0.03f, 2f);

            CimYamalariKur();
            // ⚠ Ped rengi CIM TONU OLAMAZ: cimden ayirt edilemiyordu ve "burasi benim
            // tarlam" bilgisi gorunmuyordu. Islenmis toprak tonu, ekilmemis karolarla da
            // ayni ailede kalir ama zeminden net ayrilir.
            // ⚠ Ped, cizim sprite'lariyla AYNI doygunlukta olmali. Ilk deneme cim ile
            // toprak arasinda yikanmis bir bej veriyordu ve zengin sprite'larin yaninda
            // "bos alan" gibi duruyordu. Toprak tarafina cekildi.
            _parselKok = new GameObject("Parseller").transform;
            _parselKok.SetParent(_kok, false);
            EvKur();
            DekorKur();
        }

        /// <summary>
        /// Zemine renk cesitlemesi. Tek ton cim, ortografik kamerada olu bir yuzey
        /// gibi duruyor ve mesafe hissi vermiyordu; hafif ton farklari yuzeyi okunur yapar.
        /// </summary>
        private void CimYamalariKur()
        {
            var rastgele = new System.Random(20260820);
            for (int i = 0; i < 22; i++)
            {
                float x = -14f + (float)rastgele.NextDouble() * 40f;
                float z = -10f + (float)rastgele.NextDouble() * 26f;
                // Parsellerin uzerine yama atma: karo okunurlugunu bozar.
                if (ParseleYakin(x, z)) continue;

                // ⚠ Ne dikdortgen ne de keskin kenarli disk: ikisi de ortografik
                // kamerada "doku" degil "hata" gibi okunuyor. Boyali yama sprite'i
                // kenari sonerek biter, zemine karisir.
                var konum = new Vector3(x, -0.02f, z);
                if (SpriteDeposu.SetVar)
                {
                    float olcek = 2.2f + (float)rastgele.NextDouble() * 2.6f;
                    SpriteDeposu.Yatik("zemin_yama_" + (i % 4), _kok, konum, null, olcek);
                }
                else
                {
                    float yaricap = 1.8f + (float)rastgele.NextDouble() * 3.2f;
                    float ton = 0.97f + (float)rastgele.NextDouble() * 0.05f;
                    MateryalDeposu.Nesne(
                        "CimYama" + i, MeshUretici.Disk(yaricap, 12, Palet.Cim * ton), _kok, konum);
                }
            }
        }

        private static bool ParseleYakin(float x, float z)
        {
            bool zAralikta = z > -2f && z < ParselBoy + 1f;
            bool benim = x > -2f && x < ParselEn + 1f;
            bool partner = x > ParselAraligi - 2f && x < ParselAraligi + ParselEn + 1f;
            return zAralikta && (benim || partner);
        }

        /// <summary>
        /// Parsel pedi + cit. Cit dekor degil OKUNABILIRLIK araci: "burasi benim tarlam,
        /// surasi partnerin" bilgisi baska turlu gorunmuyordu (B.7 dokunulamazlik kurali
        /// gorsel bir karsilik olmadan sadece bir red mesajidir).
        /// </summary>
        private void ParselPediKur(string ad, float x0, float x1, float z0, float z1, Color renk)
        {
            float merkezX = (x0 + x1) / 2f;
            float merkezZ = (z0 + z1) / 2f;
            float en = (x1 - x0) + 1.7f;
            float boy = (z1 - z0) + 1.7f;

            var ped = MeshUretici.Yama(en, boy, renk);
            MateryalDeposu.Nesne(ad + "Ped", ped, _parselKok, new Vector3(merkezX, -0.01f, merkezZ));

            CitSirasi(ad + "CitOn", new Vector3(merkezX, 0f, merkezZ - boy / 2f), en, false);
            CitSirasi(ad + "CitArka", new Vector3(merkezX, 0f, merkezZ + boy / 2f), en, false);
            CitSirasi(ad + "CitSol", new Vector3(merkezX - en / 2f, 0f, merkezZ), boy, true);
            CitSirasi(ad + "CitSag", new Vector3(merkezX + en / 2f, 0f, merkezZ), boy, true);
        }

        /// <summary>
        /// Parsel pedini ve citini GERCEK karolara gore kurar.
        ///
        /// ⚠ Once ped sabit 6x4 (en fazla buyuyecek olcu) ciziliyordu. Sv 1'de oyuncunun
        /// 4 karosu var; geri kalan alan kocaman bos bir kahverengi dikdortgen olarak
        /// duruyor ve parsel "yarim kalmis" gorunuyordu. Ped artik karolarla birlikte
        /// buyur — tarla acildikca cit de disari kayar, bu da ilerlemenin gorsel karsiligi.
        /// </summary>
        private void ParselleriTazele()
        {
            if (_durum == null || _parselKok == null) return;

            bool benimVar = false, partnerVar = false;
            float bx0 = 0, bx1 = 0, bz0 = 0, bz1 = 0;
            float px0 = 0, px1 = 0, pz0 = 0, pz1 = 0;

            foreach (var karo in _durum.Karolar.Values)
            {
                bool benim = karo.ParselSahibi == _durum.BenimId;
                Vector3 k = KaroKonumu(karo, benim);
                if (benim)
                {
                    if (!benimVar) { bx0 = bx1 = k.x; bz0 = bz1 = k.z; benimVar = true; }
                    bx0 = Mathf.Min(bx0, k.x); bx1 = Mathf.Max(bx1, k.x);
                    bz0 = Mathf.Min(bz0, k.z); bz1 = Mathf.Max(bz1, k.z);
                }
                else
                {
                    if (!partnerVar) { px0 = px1 = k.x; pz0 = pz1 = k.z; partnerVar = true; }
                    px0 = Mathf.Min(px0, k.x); px1 = Mathf.Max(px1, k.x);
                    pz0 = Mathf.Min(pz0, k.z); pz1 = Mathf.Max(pz1, k.z);
                }
            }
            if (!benimVar) return;

            string imza = $"{bx0},{bx1},{bz0},{bz1}|{partnerVar},{px0},{px1},{pz0},{pz1}";
            if (imza == _cizilenParselSiniri) return;
            _cizilenParselSiniri = imza;

            for (int i = _parselKok.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_parselKok.GetChild(i).gameObject);
            }

            // Ped karolardan KOYU: karo, pedin uzerinde yukselen acik bir yama gibi okunur.
            ParselPediKur("BenimParsel", bx0, bx1, bz0, bz1,
                          Color.Lerp(Palet.TopraKoyu, Palet.TopraIslak, 0.45f));
            if (partnerVar)
            {
                ParselPediKur("PartnerParsel", px0, px1, pz0, pz1,
                              Color.Lerp(Palet.TopraIslak, Palet.TopraKoyu, 0.25f));
            }
        }

        /// <summary>
        /// Cit siras. ⚠ Sprite kolu YATAY kenarlarda kullanilir; dikey kenarda bilboard
        /// sprite yandan bakildiginda kagit gibi incelir, orada mesh kalir. Yaw kilitli
        /// oldugu icin bu ayrim sabittir ve karede tutarsizlik uretmez.
        /// </summary>
        private void CitSirasi(string ad, Vector3 merkez, float uzunluk, bool dikey)
        {
            if (VarlikKatalogu.SetVar)
            {
                // Cit parcasi 1 birim; kenar boyunca dizilir. Dikey kenarda 90 derece doner.
                int adet = Mathf.Max(1, Mathf.RoundToInt(uzunluk));
                float adim = uzunluk / adet;
                for (int i = 0; i < adet; i++)
                {
                    float t = -uzunluk / 2f + adim * (i + 0.5f);
                    Vector3 k = dikey
                        ? new Vector3(merkez.x, 0f, merkez.z + t)
                        : new Vector3(merkez.x + t, 0f, merkez.z);
                    VarlikKatalogu.Kur(VarlikKatalogu.CitDuz, _kok, k, adim,
                                       dikey ? 90f : 0f, ad + i);
                }
                return;
            }
            if (SpriteDeposu.SetVar && !dikey)
            {
                // Cit parcasi 2 birim genis; kenari dolduracak kadar tekrarlanir.
                int adet = Mathf.Max(1, Mathf.RoundToInt(uzunluk / 2f));
                float adim = uzunluk / adet;
                for (int i = 0; i < adet; i++)
                {
                    float x = merkez.x - uzunluk / 2f + adim * (i + 0.5f);
                    SpriteDeposu.Nesne(
                        "cit", _kok, new Vector3(x, 0f, merkez.z), _pitch, 0.09f, null, adim / 2f);
                }
                return;
            }

            var mesh = MeshUretici.CitParcasi(uzunluk);
            var go = MateryalDeposu.Nesne(ad, mesh, _kok, merkez);
            if (dikey) go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }

        /// <summary>
        /// Ev. ⚠ Mesh zaten yaziliydi ama sahneye HIC konmamisti — olu koddu. Ev,
        /// defterin ve ogreticinin gonderdigi yer; gorunmeyince "eve git" yonergesi
        /// anlamsiz kaliyordu.
        /// </summary>
        private void EvKur()
        {
            var konum = new Vector3(2.5f, 0f, 7.2f);
            if (SpriteDeposu.SetVar)
            {
                SpriteDeposu.Yatik("golge", _kok, konum + new Vector3(0f, 0.02f, -0.1f), null, 2.4f);
                SpriteDeposu.Nesne("ev", _kok, konum, _pitch, 0.05f, null, 2.2f);
                return;
            }
            MateryalDeposu.Nesne("Ev", MeshUretici.Ev(), _kok, konum);
        }

        /// <summary>Kenarlari dolduran dekor: agac siniri, tas ve ot tutamlari.</summary>
        /// <summary>
        /// Kenarlari dolduran dekor: agac siniri, kaya ve ot tutamlari.
        ///
        /// B.22 revizyonundan sonra bunlar CIZIM SPRITE'idir; prosedurel mesh yalnizca
        /// sprite seti diskte yoksa yedek olarak kullanilir.
        /// </summary>
        private void DekorKur()
        {
            var rastgele = new System.Random(999);
            bool model = VarlikKatalogu.SetVar;
            bool sprite = SpriteDeposu.SetVar;

            void Agac(string ad, Vector3 konum, float olcek, int cesit)
            {
                if (model)
                {
                    // Kenney CC0 3B kiti (B.22 kaynak stratejisi 2). Rastgele Y donusu
                    // olmazsa ayni agacin kopyalari "kopyala-yapistir" gibi durur.
                    VarlikKatalogu.Kur(VarlikKatalogu.Agac(cesit), _kok, konum,
                                       olcek * 1.15f, (float)rastgele.NextDouble() * 360f, ad);
                    return;
                }
                if (sprite)
                {
                    SpriteDeposu.Yatik("golge", _kok, konum + new Vector3(0f, 0.02f, -0.05f), null, olcek * 0.85f);
                    SpriteDeposu.Nesne("agac_" + cesit, _kok, konum, _pitch, 0.04f, null, olcek * 1.9f);
                }
                else
                {
                    MateryalDeposu.Nesne(ad, MeshUretici.Agac(olcek), _kok, konum);
                }
            }

            // Arka agac siniri: sahneye ust sinir verir, "sonsuz bos tarla" hissini keser.
            for (int i = 0; i < 16; i++)
            {
                float x = -12f + i * 2.2f + (float)rastgele.NextDouble() * 0.8f;
                float z = 10.5f + (float)rastgele.NextDouble() * 2.5f;
                float olcek = 0.85f + (float)rastgele.NextDouble() * 0.5f;
                Agac("Agac" + i, new Vector3(x, 0f, z), olcek, i % 3);
            }

            // Yan agaclar: sol ve sag cerceve.
            for (int i = 0; i < 6; i++)
            {
                float z = -6f + i * 2.8f;
                float olcek = 0.8f + (float)rastgele.NextDouble() * 0.4f;
                Agac("AgacSol" + i, new Vector3(-10f - (float)rastgele.NextDouble() * 2f, 0f, z), olcek, (i + 1) % 3);
                Agac("AgacSag" + i, new Vector3(19f + (float)rastgele.NextDouble() * 2f, 0f, z), olcek, (i + 2) % 3);
            }

            // Ot tutamlari: bos alanlari doldurur, olcek hissi verir.
            for (int i = 0; i < 90; i++)
            {
                float x = -11f + (float)rastgele.NextDouble() * 32f;
                float z = -7f + (float)rastgele.NextDouble() * 18f;
                if (ParseleYakin(x, z)) continue;
                var konum = new Vector3(x, 0f, z);
                if (model)
                {
                    VarlikKatalogu.Kur(VarlikKatalogu.Ot(i), _kok, konum,
                                       0.9f + (float)rastgele.NextDouble() * 0.5f,
                                       (float)rastgele.NextDouble() * 360f, "Ot" + i);
                }
                else if (sprite) SpriteDeposu.Nesne("ot_" + (i % 3), _kok, konum, _pitch, 0.08f, null, 0.7f);
                else MateryalDeposu.Nesne("Ot" + i, MeshUretici.OtTutami(i), _kok, konum);
            }

            // Birkac tas ve cali.
            for (int i = 0; i < 7; i++)
            {
                float x = -9f + (float)rastgele.NextDouble() * 28f;
                float z = -6f + (float)rastgele.NextDouble() * 15f;
                if (ParseleYakin(x, z)) continue;
                var konum = new Vector3(x, 0f, z);
                if (model)
                {
                    string m = i % 3 == 0 ? VarlikKatalogu.Cali(i) : VarlikKatalogu.Kaya(i);
                    VarlikKatalogu.Kur(m, _kok, konum, 0.9f + (float)rastgele.NextDouble() * 0.4f,
                                       (float)rastgele.NextDouble() * 360f, "Kaya" + i);
                }
                else if (sprite)
                {
                    SpriteDeposu.Nesne(i % 3 == 0 ? "cali" : "kaya_" + (i % 2), _kok, konum,
                                       _pitch, 0.1f, null, 0.9f);
                }
                else
                {
                    float b = 0.3f + (float)rastgele.NextDouble() * 0.35f;
                    MateryalDeposu.Nesne(
                        "Tas" + i, MeshUretici.Kutu(new Vector3(b, b * 0.6f, b * 0.85f), Palet.Tas),
                        _kok, konum);
                }
            }
        }

        private void KuyuKur()
        {
            // Kuyu tarla sahnesinin ICINDE (B.11): su almak icin sahne degistirmek yok.
            if (SpriteDeposu.SetVar)
            {
                SpriteDeposu.Yatik("golge", _kok, KuyuKonumu + new Vector3(0f, 0.02f, -0.05f), null, 1.2f);
                SpriteDeposu.Nesne("kuyu", _kok, KuyuKonumu, _pitch, 0.06f, null, 1.3f);
                return;
            }
            MateryalDeposu.Nesne("Kuyu", MeshUretici.Kuyu(), _kok, KuyuKonumu);
        }

        /// <summary>Kuyu, parselin soluna yerlestirilir; yabani kaynak seridiyle cakismaz.</summary>
        public static readonly Vector3 KuyuKonumu = new Vector3(-4f, 0f, 1f);

        // ------------------------------------------------------------------ tazeleme

        public void Tazele()
        {
            if (_durum == null) return;

            ParselleriTazele();

            foreach (var karo in _durum.Karolar.Values)
            {
                KaroCiz(karo);
            }
            foreach (var dugum in _durum.Dugumler.Values)
            {
                DugumCiz(dugum);
            }
        }

        private void KaroCiz(Karo karo)
        {
            if (!_karolar.TryGetValue(karo.Anahtar, out KaroGorseli g))
            {
                g = new KaroGorseli();
                bool benim = karo.ParselSahibi == _durum.BenimId;
                Vector3 konum = KaroKonumu(karo, benim);
                g.Zemin = MateryalDeposu.Nesne("Karo_" + karo.X + "_" + karo.Y, _karoMeshBos, _karoKok, konum);
                _karolar[karo.Anahtar] = g;
            }

            bool sulu = karo.SulandiAt != null;
            Mesh hedefMesh = karo.Bos ? _karoMeshBos : (sulu ? _karoMeshSulu : _karoMeshKuru);
            if (g.CizilenSulu != sulu || g.CizilenUrun != karo.UrunId)
            {
                var mf = g.Zemin.GetComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = hedefMesh;
                g.CizilenSulu = sulu;
            }

            int asama = BuyumeAsamasi(karo);
            if (g.CizilenUrun != karo.UrunId || g.CizilenAsama != asama)
            {
                if (g.Urun != null)
                {
                    UnityEngine.Object.Destroy(g.Urun);
                    g.Urun = null;
                }
                if (!karo.Bos)
                {
                    bool benim = karo.ParselSahibi == _durum.BenimId;
                    Vector3 yer = KaroKonumu(karo, benim) + Vector3.up * 0.06f;

                    // B.22 revizyonu: urun artik cizim sprite'i. Sprite yoksa (uret.py
                    // hic calismadiysa) eski prosedurel mesh'e duser — oyun her halde acilir.
                    if (VarlikKatalogu.SetVar)
                    {
                        string mYol = VarlikKatalogu.Urun(karo.UrunId, asama, _denge.BuyumeAsamaSayisi);
                        g.Urun = VarlikKatalogu.Kur(mYol, _karoKok, yer, 1f, 0f, "Urun");
                        if (g.Urun != null)
                        {
                            g.CizilenUrun = karo.UrunId;
                            g.CizilenAsama = asama;
                            return;
                        }
                    }

                    string spriteAdi = "urun_" + karo.UrunId + "_" + asama;
                    if (SpriteDeposu.SetVar && SpriteDeposu.Al(spriteAdi) != null)
                    {
                        // ⚠ Olcek 1.35: 64x96 px sprite tam boyutta karonun yaninda
                        // kayboluyordu. Urun, uzerinde durdugu karodan uzun gorunmeli.
                        g.Urun = SpriteDeposu.Nesne(spriteAdi, _karoKok, yer, _pitch, 0.06f, null, 1.35f);
                    }
                    else
                    {
                        var urunBilgi = _denge.Urun(karo.UrunId);
                        Color renk = Palet.UrunRengi(asama, _denge.BuyumeAsamaSayisi);
                        if (urunBilgi != null && urunBilgi.Id == "bugday") renk = Palet.Bugday;
                        Mesh mesh = MeshUretici.Urun(asama, _denge.BuyumeAsamaSayisi, renk);
                        g.Urun = MateryalDeposu.Nesne("Urun", mesh, _karoKok, yer);
                    }
                }
                g.CizilenUrun = karo.UrunId;
                g.CizilenAsama = asama;
            }
        }

        private void DugumCiz(Dugum dugum)
        {
            bool hazir = dugum.Hazir(_durum.SunucuZamani);
            if (!_dugumler.TryGetValue(dugum.Anahtar, out DugumGorseli g))
            {
                g = new DugumGorseli();
                g.Nesne = MateryalDeposu.Nesne(
                    "Dugum_" + dugum.Tur,
                    MeshUretici.YabaniKaynak(dugum.Tur),
                    _dugumKok,
                    new Vector3(dugum.X, 0f, dugum.Y));
                _dugumler[dugum.Anahtar] = g;
                g.CizilenHazir = !hazir;
            }
            if (g.CizilenHazir != hazir)
            {
                // Toplanmis kaynak KAYBOLMAZ, sadece silinir: oyuncu nerede oldugunu unutmasin.
                g.Nesne.transform.localScale = hazir ? Vector3.one : Vector3.one * 0.35f;
                g.CizilenHazir = hazir;
            }
        }

        // ------------------------------------------------------------------ hesaplar

        /// <summary>
        /// Gorsel buyume asamasi. Sulanmamis karo asama 0'da kalir — "biri eker, biri sular"
        /// kurali oyuncuya SESSIZCE degil, gorselde soylenir.
        /// </summary>
        public int BuyumeAsamasi(Karo karo)
        {
            if (karo.Bos) return 0;
            if (karo.SulandiAt == null) return 0;

            var urun = _denge.Urun(karo.UrunId);
            if (urun == null) return 0;

            long sureSn = karo.HasatSayaci == 0
                ? urun.BuyumeSn
                : (urun.YenidenVerir ? urun.YenidenVerirAraSn : urun.BuyumeSn);
            if (sureSn <= 0) return _denge.BuyumeAsamaSayisi - 1;

            double gecen = (_durum.SunucuZamani - karo.SulandiAt.Value).TotalSeconds;
            int n = _denge.BuyumeAsamaSayisi;
            int asama = (int)Math.Floor(gecen * n / sureSn);
            return Mathf.Clamp(asama, 0, n - 1);
        }

        public bool Olgun(Karo karo) =>
            !karo.Bos && karo.SulandiAt != null && BuyumeAsamasi(karo) >= _denge.BuyumeAsamaSayisi - 1;

        /// <summary>Kalan sure (saniye). Negatifse olgun.</summary>
        public double KalanSn(Karo karo)
        {
            if (karo.Bos || karo.SulandiAt == null) return double.MaxValue;
            var urun = _denge.Urun(karo.UrunId);
            if (urun == null) return double.MaxValue;
            long sureSn = karo.HasatSayaci == 0
                ? urun.BuyumeSn
                : (urun.YenidenVerir ? urun.YenidenVerirAraSn : urun.BuyumeSn);
            return sureSn - (_durum.SunucuZamani - karo.SulandiAt.Value).TotalSeconds;
        }

        /// <summary>
        /// Karo dunya konumu. Iki parsel yan yana dizilir; partnerin parseline
        /// DOKUNULAMAZ (B.7) ama GORUNUR — birlikte oynandigi hissi buradan gelir.
        /// </summary>
        public static Vector3 KaroKonumu(Karo karo, bool benimParselim)
        {
            float xKaydirma = benimParselim ? 0f : 8f;
            return new Vector3(karo.X + xKaydirma, 0f, karo.Y);
        }

        public Vector3 KaroKonumu(int x, int y) => new Vector3(x, 0f, y);

        // ------------------------------------------------------------------ vurgu ve geri sarma

        public void VurguGoster(Vector3 konum)
        {
            _vurgu.SetActive(true);
            _vurgu.transform.localPosition = konum + Vector3.up * 0.02f;
        }

        public void VurguGizle() => _vurgu.SetActive(false);

        /// <summary>
        /// Sunucu reddettiginde 0.2 sn yumusak geri sarma (B.3 kural 7).
        /// Sert ziplama YASAK: oyuncu ne oldugunu anlamaz ve oyunu bozuk sanir.
        /// </summary>
        private void GeriSarmaBaslat(string karoAnahtar)
        {
            if (_karolar.TryGetValue(karoAnahtar, out KaroGorseli g))
            {
                g.GeriSarmaBitis = Time.unscaledTime + _denge.GeriSarmaSn;
            }
        }

        private void Update()
        {
            if (_durum == null) return;

            foreach (var g in _karolar.Values)
            {
                if (g.GeriSarmaBitis <= 0f) continue;
                float kalan = g.GeriSarmaBitis - Time.unscaledTime;
                if (kalan <= 0f)
                {
                    g.GeriSarmaBitis = 0f;
                    g.Zemin.transform.localScale = Vector3.one;
                    continue;
                }
                float t = kalan / Mathf.Max(0.01f, _denge.GeriSarmaSn);
                g.Zemin.transform.localScale = Vector3.one * (1f + 0.08f * t);
            }
        }
    }
}
