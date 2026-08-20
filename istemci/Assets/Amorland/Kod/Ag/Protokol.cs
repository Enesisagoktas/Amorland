using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Amorland.Ag
{
    /// <summary>
    /// Sunucu protokolunun istemci tarafi karsiligi (Bolum C.3).
    ///
    /// Bu dosyadaki kayitlar sunucudaki <c>DunyaModeli</c> ve <c>WsMesaj</c> ile
    /// BIREBIR ayni alan adlarini tasir. Alan adi degistirilirse iki taraf birden
    /// degistirilmelidir; protokol sozlesmesi budur.
    ///
    /// Mimari kural 1: burada miktar/fiyat/XP HESAPLAYAN hicbir sey yoktur.
    /// Istemci yalnizca sunucunun soyledigini tasir ve cizer.
    /// </summary>
    public static class Protokol
    {
        public const string TipBaglan = "BAGLAN";
        public const string TipNiyet = "NIYET";
        public const string TipKonum = "KONUM";
        public const string TipSohbet = "SOHBET";
        public const string TipNotTahtasi = "NOT_TAHTASI";
        public const string TipPing = "PING";

        public const string TipSenkronTam = "SENKRON_TAM";
        public const string TipSenkronDelta = "SENKRON_DELTA";
        public const string TipOnay = "ONAY";
        public const string TipRed = "RED";
        public const string TipDegisim = "DEGISIM";
        public const string TipHata = "HATA";
        public const string TipPong = "PONG";
        public const string TipPartnerGirdi = "PARTNER_GIRDI";
        public const string TipPartnerCikti = "PARTNER_CIKTI";
    }

    // ------------------------------------------------------------------ niyet

    [Serializable]
    public class NiyetHedef
    {
        [JsonProperty("bolge")] public string Bolge;
        [JsonProperty("x")] public int? X;
        [JsonProperty("y")] public int? Y;
        [JsonProperty("parsel")] public string Parsel;

        public static NiyetHedef Karo(int x, int y) =>
            new NiyetHedef { Bolge = "tarla", X = x, Y = y };

        public static NiyetHedef Dugum(string bolge, int x, int y) =>
            new NiyetHedef { Bolge = bolge, X = x, Y = y };

        public static NiyetHedef Bolgeye(string bolge) => new NiyetHedef { Bolge = bolge };
    }

    [Serializable]
    public class NiyetEk
    {
        [JsonProperty("urunId")] public string UrunId;
        [JsonProperty("esyaId")] public string EsyaId;
        [JsonProperty("adet")] public int? Adet;
        [JsonProperty("tarifId")] public string TarifId;
    }

    /// <summary>
    /// Mimari kural 4: her komut <c>clientActionId</c> tasir. Mobilde baglanti
    /// surekli kopar; istemci gonderdigini bilemez ve tekrar gonderir. Sunucu ayni
    /// kimligi ikinci kez gorurse ilk sonucu doner, eylemi tekrar UYGULAMAZ.
    /// </summary>
    [Serializable]
    public class Niyet
    {
        [JsonProperty("clientActionId")] public string ClientActionId;
        [JsonProperty("tip")] public string Tip;
        [JsonProperty("hedef")] public NiyetHedef Hedef;
        [JsonProperty("ek")] public NiyetEk Ek;

        public static Niyet Olustur(string tip, NiyetHedef hedef = null, NiyetEk ek = null) =>
            new Niyet
            {
                ClientActionId = Guid.NewGuid().ToString(),
                Tip = tip,
                Hedef = hedef,
                Ek = ek
            };
    }

    // ------------------------------------------------------------------ dunya durumu

    [Serializable]
    public class Karo
    {
        [JsonProperty("parselSahibi")] public string ParselSahibi;
        [JsonProperty("x")] public int X;
        [JsonProperty("y")] public int Y;
        [JsonProperty("urunId")] public string UrunId;
        [JsonProperty("ekildiAt")] public DateTime? EkildiAt;
        [JsonProperty("sulandiAt")] public DateTime? SulandiAt;
        [JsonProperty("hasatSayaci")] public int HasatSayaci;
        [JsonProperty("sonHasatAt")] public DateTime? SonHasatAt;
        [JsonProperty("gubreli")] public bool Gubreli;

        [JsonIgnore] public bool Bos => string.IsNullOrEmpty(UrunId);
        [JsonIgnore] public bool Susuz => !Bos && SulandiAt == null;
        [JsonIgnore] public string Anahtar => ParselSahibi + ":" + X + ":" + Y;
    }

    [Serializable]
    public class EnvanterSatiri
    {
        [JsonProperty("sahip")] public string Sahip;
        [JsonProperty("esyaId")] public string EsyaId;
        [JsonProperty("adet")] public int Adet;
    }

    [Serializable]
    public class DepoSatiri
    {
        [JsonProperty("esyaId")] public string EsyaId;
        [JsonProperty("adet")] public int Adet;
        [JsonProperty("koyan")] public string Koyan;
        [JsonProperty("kilitli")] public bool Kilitli;
        [JsonProperty("kilitleyen")] public string Kilitleyen;
    }

    [Serializable]
    public class Dugum
    {
        [JsonProperty("bolge")] public string Bolge;
        [JsonProperty("x")] public int X;
        [JsonProperty("y")] public int Y;
        [JsonProperty("tur")] public string Tur;
        [JsonProperty("sinif")] public string Sinif;
        [JsonProperty("saglik")] public int Saglik;
        [JsonProperty("maksSaglik")] public int MaksSaglik;
        [JsonProperty("kirildiAt")] public DateTime? KirildiAt;
        [JsonProperty("hazirOlacakAt")] public DateTime? HazirOlacakAt;

        [JsonIgnore] public string Anahtar => Bolge + ":" + X + ":" + Y;

        public bool Hazir(DateTime sunucuZamani) =>
            HazirOlacakAt == null || sunucuZamani >= HazirOlacakAt.Value;
    }

    [Serializable]
    public class AletDurumu
    {
        [JsonProperty("sahip")] public string Sahip;
        [JsonProperty("aletId")] public string AletId;
        [JsonProperty("dayaniklilik")] public int Dayaniklilik;
        [JsonProperty("maksDayaniklilik")] public int MaksDayaniklilik;

        [JsonIgnore] public string Anahtar => Sahip + ":" + AletId;
    }

    [Serializable]
    public class OyuncuOzeti
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("ad")] public string Ad;
        [JsonProperty("renk")] public int Renk;
        [JsonProperty("seviye")] public int Seviye;
        [JsonProperty("xp")] public int Xp;
        [JsonProperty("sonrakiSeviyeXp")] public int SonrakiSeviyeXp;
        /// <summary>-1 ise altin HENUZ GORUNMEZ. Para Sv 5'te girer (B.15).</summary>
        [JsonProperty("altin")] public long Altin;
        [JsonProperty("cantaSlot")] public int CantaSlot;
        [JsonProperty("kovaKalan")] public int KovaKalan;
        [JsonProperty("kovaKapasite")] public int KovaKapasite;
        [JsonProperty("cevrimici")] public bool Cevrimici;
        [JsonProperty("sonGorulme")] public DateTime? SonGorulme;

        [JsonIgnore] public bool AltinGorunur => Altin >= 0;
    }

    [Serializable]
    public class VadiOzeti
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("kuranId")] public string KuranId;
        [JsonProperty("partnerId")] public string PartnerId;
        [JsonProperty("dunyaSeviyesi")] public int DunyaSeviyesi;
        [JsonProperty("tekImzaModu")] public bool TekImzaModu;
        [JsonProperty("depoKapasitesi")] public int DepoKapasitesi;
        [JsonProperty("depoDolu")] public int DepoDolu;
        [JsonProperty("kuslukAktif")] public bool KuslukAktif;
        [JsonProperty("kuslukBaslatan")] public string KuslukBaslatan;
    }

    [Serializable]
    public class Olay
    {
        [JsonProperty("tip")] public string Tip;
        [JsonProperty("deger")] public string Deger;
        [JsonProperty("sayi")] public int? Sayi;
    }

    /// <summary>
    /// Durum degisimi. Tam anlik goruntu ile delta AYNI sekle sahiptir: istemcide
    /// tek bir uygulama kodu vardir, <c>Tam</c> true ise once yerel durum atilir.
    /// </summary>
    [Serializable]
    public class Degisim
    {
        [JsonProperty("revision")] public long Revision;
        [JsonProperty("sunucuZamani")] public DateTime SunucuZamani;
        [JsonProperty("tam")] public bool Tam;
        [JsonProperty("karolar")] public List<Karo> Karolar;
        [JsonProperty("envanter")] public List<EnvanterSatiri> Envanter;
        [JsonProperty("depo")] public List<DepoSatiri> Depo;
        [JsonProperty("dugumler")] public List<Dugum> Dugumler;
        [JsonProperty("aletler")] public List<AletDurumu> Aletler;
        [JsonProperty("oyuncular")] public List<OyuncuOzeti> Oyuncular;
        [JsonProperty("vadi")] public VadiOzeti Vadi;
        [JsonProperty("isaretler")] public List<string> Isaretler;
        [JsonProperty("olaylar")] public List<Olay> Olaylar;
    }

    // ------------------------------------------------------------------ WebSocket zarflari

    [Serializable]
    public class WsKonum
    {
        [JsonProperty("bolge")] public string Bolge;
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("yon")] public float Yon;
    }

    [Serializable]
    public class WsGiden
    {
        [JsonProperty("t")] public string T;
        [JsonProperty("jwt", NullValueHandling = NullValueHandling.Ignore)] public string Jwt;
        [JsonProperty("sonRevision", NullValueHandling = NullValueHandling.Ignore)] public long? SonRevision;
        [JsonProperty("istemciSurum", NullValueHandling = NullValueHandling.Ignore)] public string IstemciSurum;
        [JsonProperty("niyet", NullValueHandling = NullValueHandling.Ignore)] public Niyet Niyet;
        [JsonProperty("konum", NullValueHandling = NullValueHandling.Ignore)] public WsKonum Konum;
        [JsonProperty("metin", NullValueHandling = NullValueHandling.Ignore)] public string Metin;
        [JsonProperty("sablonId", NullValueHandling = NullValueHandling.Ignore)] public string SablonId;
        [JsonProperty("nesneId", NullValueHandling = NullValueHandling.Ignore)] public string NesneId;
    }

    [Serializable]
    public class WsGelen
    {
        [JsonProperty("t")] public string T;
        [JsonProperty("revision")] public long? Revision;
        [JsonProperty("degisim")] public Degisim Degisim;
        [JsonProperty("degisimler")] public List<Degisim> Degisimler;
        [JsonProperty("clientActionId")] public string ClientActionId;
        [JsonProperty("kod")] public string Kod;
        [JsonProperty("mesaj")] public string Mesaj;
        [JsonProperty("oyuncuId")] public string OyuncuId;
        [JsonProperty("konum")] public WsKonum Konum;
        [JsonProperty("sablonId")] public string SablonId;
        [JsonProperty("nesneId")] public string NesneId;
        [JsonProperty("sunucuZamaniMs")] public long? SunucuZamaniMs;
    }

    // ------------------------------------------------------------------ REST yanitlari

    [Serializable]
    public class OturumPaketi
    {
        [JsonProperty("oyuncuId")] public string OyuncuId;
        [JsonProperty("ad")] public string Ad;
        [JsonProperty("renk")] public int Renk;
        [JsonProperty("seviye")] public int Seviye;
        [JsonProperty("xp")] public int Xp;
        [JsonProperty("bagli")] public bool Bagli;
        [JsonProperty("dogumYili")] public int? DogumYili;
        [JsonProperty("erisimJetonu")] public string ErisimJetonu;
        [JsonProperty("erisimOmruSn")] public long ErisimOmruSn;
        [JsonProperty("yenilemeJetonu")] public string YenilemeJetonu;
        [JsonProperty("vadiId")] public string VadiId;
    }

    [Serializable]
    public class VadiYaniti
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("kuranId")] public string KuranId;
        [JsonProperty("partnerId")] public string PartnerId;
        [JsonProperty("revision")] public long Revision;
        [JsonProperty("dunyaSeviyesi")] public int DunyaSeviyesi;
        [JsonProperty("tekImzaModu")] public bool TekImzaModu;
    }

    [Serializable]
    public class SunucuHatasi
    {
        [JsonProperty("kod")] public string Kod;
        [JsonProperty("mesaj")] public string Mesaj;
    }
}
