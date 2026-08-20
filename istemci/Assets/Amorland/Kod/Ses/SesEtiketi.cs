namespace Amorland.Ses
{
    /// <summary>
    /// Ses envanteri (B.23): 52 benzersiz ses, ~92 dosya.
    /// Dikey dilimde yalnizca yer tutucular calar; gercek kayitlar Perde 1'de gelir.
    ///
    /// ⚠ B.26 yasak 17: <b>hicbir mekanik yalnizca sesle bildirilemez.</b> Oyuncularin
    /// cogu sesi kapali oynayacak (mobil kozy oyunlar podcast dinlerken oynanir).
    /// Buradaki her etiketin bir GORSEL karsiligi olmak zorunda; ses ikinci kanaldir.
    /// </summary>
    public enum SesKanali
    {
        Muzik,
        Ortam,
        Eylem,
        Ui
    }

    public enum SesEtiketi
    {
        // Eylem (B.23: 14 eylem sesi, her biri x3 varyasyon)
        Ekme,
        Sulama,
        Hasat,
        HasatAgac,
        Kazma,
        KazmaTas,
        DugumKirilma,
        Balta,
        AgacDusme,
        Toplama,
        Kova,
        Craft,
        CraftBitti,
        AletKirilma,

        // Ayak sesi (4 yuzey x5 varyasyon)
        AyakToprak,
        AyakCim,
        AyakTahta,
        AyakTas,

        // Odul / olay (8)
        SeviyeAtlama,
        TrenGelisi,
        TrenKalkis,
        Manifesto,
        DefterSayfasi,
        OrtakProjeKatki,
        OrtakProjeTamam,
        GunlukSandik,

        // UI (12'nin dikey dilimde gerekenleri)
        UiDokunma,
        UiAcilis,
        UiKapanis,
        UiHata,
        UiOnay,

        // Ortam
        OrtamTarla,
        OrtamEv
    }
}
