using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Arayuz;
using Amorland.Gorsel;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// Arayuz katmaninin BASSIZ denetimi.
    ///
    /// Neden gerekli: <see cref="Sekil"/> sprite'lari calisma aninda <c>Texture2D</c>
    /// uretiyor. Derleme bunu dogrulamaz — 9-dilim kenar degeri texture'dan buyukse
    /// veya piksel dongusu tasarsa hata ancak oyun calisirken gorunur. Ekran kilitliyken
    /// ya da CI'da Play moduna girilemedigi icin denetim batchmode'da kosar.
    ///
    /// Calistirma:
    ///   Unity -batchmode -nographics -projectPath &lt;proje&gt; \
    ///         -executeMethod Amorland.EditorAraclari.ArayuzDenetimi.Denetle -quit
    ///
    /// Cikis kodu 0 temiz, 1 hatali.
    ///
    /// ⚠ Bu bir EDITOR aracidir, oyuna girmez. Yalnizca <c>Assets/Amorland/Editor</c>
    /// altinda durdugu icin oyun derlemesine dahil edilmez.
    /// </summary>
    public static class ArayuzDenetimi
    {
        private static readonly List<string> _hatalar = new List<string>();
        private static int _gecen;

        [MenuItem("Amorland/Arayuz Denetimi")]
        public static void Denetle()
        {
            _hatalar.Clear();
            _gecen = 0;

            Debug.Log("[Amorland] Arayuz denetimi basliyor.");

            SekilDenetle();
            UiKitDenetle();
            ModalDenetle();

            Debug.Log("[Amorland] " + _gecen + " denetim gecti, " + _hatalar.Count + " hata.");
            foreach (string h in _hatalar) Debug.LogError("[Amorland] HATA: " + h);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(_hatalar.Count == 0 ? 0 : 1);
            }
        }

        // ------------------------------------------------------------------ sekil

        /// <summary>
        /// Uretilen sprite'lar gercekten yuvarlak mi? Kose saydam, merkez opak olmali.
        /// Sadece "null degil" demek yetmez — duz bir dikdortgen de null degildir.
        /// </summary>
        private static void SekilDenetle()
        {
            // UI'da fiilen kullanilan yaricaplar.
            foreach (int r in new[] { 5, 12, 16, 20, 22, 30 })
            {
                var s = Sekil.Yuvarlak(r);
                if (!SpriteGecerli(s, "Yuvarlak(" + r + ")")) continue;

                int boy = r * 2 + 4;
                if (s.texture.width != boy)
                {
                    Ekle("Yuvarlak(" + r + ") texture genisligi " + s.texture.width + ", beklenen " + boy);
                    continue;
                }
                if (!KenarGecerli(s, boy, "Yuvarlak(" + r + ")")) continue;

                // Merkez dolu olmali.
                byte merkez = s.texture.GetPixel(boy / 2, boy / 2).a > 0.99f ? (byte)255 : (byte)0;
                if (merkez != 255)
                {
                    Ekle("Yuvarlak(" + r + ") merkezi opak degil");
                    continue;
                }
                // Kose bosalmali — yuvarlatmanin kaniti bu.
                float kose = s.texture.GetPixel(0, 0).a;
                if (kose > 0.02f)
                {
                    Ekle("Yuvarlak(" + r + ") kosesi saydam degil (alfa " + kose.ToString("F3")
                         + ") — kose yuvarlanmamis");
                    continue;
                }
                Gecti();
            }

            foreach (var ikili in new[] { new[] { 16, 14 }, new[] { 16, 16 }, new[] { 16, 18 },
                                          new[] { 20, 26 }, new[] { 22, 28 } })
            {
                int r = ikili[0], b = ikili[1];
                var s = Sekil.Golge(r, b);
                if (!SpriteGecerli(s, "Golge(" + r + "," + b + ")")) continue;

                int boy = (r + b) * 2 + 4;
                if (s.texture.width != boy)
                {
                    Ekle("Golge(" + r + "," + b + ") boyu " + s.texture.width + ", beklenen " + boy);
                    continue;
                }
                if (!KenarGecerli(s, boy, "Golge(" + r + "," + b + ")")) continue;

                // Golgenin ici dolu, disi sonumlu olmali.
                if (s.texture.GetPixel(boy / 2, boy / 2).a < 0.99f)
                {
                    Ekle("Golge(" + r + "," + b + ") merkezi dolu degil");
                    continue;
                }
                if (s.texture.GetPixel(0, 0).a > 0.02f)
                {
                    Ekle("Golge(" + r + "," + b + ") kosesi sonumlenmemis");
                    continue;
                }
                Gecti();
            }

            foreach (var ikili in new[] { new[] { 12, 3 }, new[] { 30, 3 } })
            {
                int r = ikili[0], k = ikili[1];
                var s = Sekil.Halka(r, k);
                if (!SpriteGecerli(s, "Halka(" + r + "," + k + ")")) continue;

                int boy = r * 2 + 4;
                if (!KenarGecerli(s, boy, "Halka(" + r + "," + k + ")")) continue;

                // Halkanin ICI bos olmali; dolu olsaydi altindaki yaziyi kapatirdi.
                float ic = s.texture.GetPixel(boy / 2, boy / 2).a;
                if (ic > 0.02f)
                {
                    Ekle("Halka(" + r + "," + k + ") ici dolu (alfa " + ic.ToString("F3") + ")");
                    continue;
                }
                // Kenarinda cizgi olmali.
                float kenar = s.texture.GetPixel(boy / 2, k / 2 + 1).a;
                if (kenar < 0.5f)
                {
                    Ekle("Halka(" + r + "," + k + ") kenarinda cizgi yok (alfa " + kenar.ToString("F3") + ")");
                    continue;
                }
                Gecti();
            }

            var daire = Sekil.Daire();
            if (SpriteGecerli(daire, "Daire()"))
            {
                int b = daire.texture.width;
                if (daire.texture.GetPixel(b / 2, b / 2).a < 0.99f) Ekle("Daire() merkezi bos");
                else if (daire.texture.GetPixel(1, 1).a > 0.02f) Ekle("Daire() kosesi dolu — daire degil kare");
                else Gecti();
            }

            // Hale: merkezde parlak, kenarda TAMAMEN sonmus olmali. Kenarda deger
            // kalirsa koyu zeminde keskin bir disk gorunur — duzeltilen kusur buydu.
            var hale = Sekil.Hale();
            if (SpriteGecerli(hale, "Hale()"))
            {
                int b = hale.texture.width;
                // ⚠ Esik 0.99 DEGIL: hale duz dolgu degil gradyan. Cift boyutlu bir
                // texture'da merkez piksel yarim piksel kacik orneklenir, tepe deger
                // (1 - 0.0055)^2 ≈ 0.989'u gecemez. 0.99 istemek ulasilamaz bir sart.
                if (hale.texture.GetPixel(b / 2, b / 2).a < 0.95f) Ekle("Hale() merkezi sonuk");
                else if (hale.texture.GetPixel(b / 2, 1).a > 0.02f)
                    Ekle("Hale() kenari sonmemis — keskin disk gibi gorunur");
                else if (hale.texture.GetPixel(b / 2, (int)(b * 0.75f)).a > 0.30f)
                    Ekle("Hale() cok yavas sonuyor, yine disk gibi durur");
                else Gecti();
            }

            // Onbellek ayni ornegi dondurmeli: her cagride texture uretirse UI kasar.
            if (!ReferenceEquals(Sekil.Yuvarlak(16), Sekil.Yuvarlak(16)))
            {
                Ekle("Sekil.Yuvarlak onbellege almiyor — her cagride yeni texture uretiyor");
            }
            else Gecti();
        }

        private static bool SpriteGecerli(Sprite s, string ad)
        {
            if (s == null) { Ekle(ad + " null dondu"); return false; }
            if (s.texture == null) { Ekle(ad + " texture'i null"); return false; }
            return true;
        }

        /// <summary>9-dilim kenari texture'dan buyukse Unity sprite'i bozuk cizer.</summary>
        private static bool KenarGecerli(Sprite s, int boy, string ad)
        {
            Vector4 k = s.border;
            if (k.x + k.z >= boy || k.y + k.w >= boy)
            {
                Ekle(ad + " 9-dilim kenari texture'dan buyuk: " + k + " boy " + boy);
                return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ uikit

        /// <summary>UiKit'in her uretecini gercekten kurar. Null referans burada patlar.</summary>
        private static void UiKitDenetle()
        {
            GameObject kok = null;
            try
            {
                var kanvas = UiKit.Kanvas("DenetimKanvas", 0);
                kok = kanvas.gameObject;

                var kart = UiKit.Kart(kanvas.transform, "Kart", Palet.UiPanel);
                UiKit.Sabitle(kart, 0.5f, 0.5f, 0f, 0f, 400f, 300f);
                DogrulaSprite(kart, "Kart");

                // Golge KARDES olarak eklenmeli, cocuk olarak degil — cocuk olsaydi ustune cizerdi.
                var golge = UiKit.GolgeEkle(kart, 16, 16, 5f);
                if (golge == null) Ekle("GolgeEkle null dondu");
                else if (golge.transform.parent != kart.parent) Ekle("Golge kardes degil");
                else if (golge.transform.GetSiblingIndex() > kart.GetSiblingIndex())
                    Ekle("Golge kartin ONUNDE ciziliyor — arkasinda olmali");
                else Gecti();

                var ikincil = UiKit.Buton(kart, "Ikincil", "Ikincil", null);
                DogrulaButon(ikincil, "Buton");

                var ana = UiKit.AnaButon(kart, "Ana", "Ana", null);
                DogrulaButon(ana, "AnaButon");
                // Vurgu zemin uzerine ACIK yazi okunmaz; koyu olmali.
                var anaYazi = ana.GetComponentInChildren<Text>();
                if (anaYazi != null && anaYazi.color.grayscale > 0.5f)
                    Ekle("AnaButon yazisi vurgu zemin uzerinde acik renk — okunmaz");
                else Gecti();

                DogrulaButon(UiKit.UyariButon(kart, "Uyari", "Sil", null), "UyariButon");
                DogrulaButon(UiKit.DuzButon(kart, "Duz", "Iptal", null), "DuzButon");
                DogrulaButon(UiKit.DaireButon(kart, "Daire", "×", null), "DaireButon");

                var giris = UiKit.Giris(kart, "Giris", "ipucu", 16);
                if (giris == null) Ekle("Giris null");
                else if (giris.textComponent == null) Ekle("Giris textComponent bagli degil");
                else if (giris.placeholder == null) Ekle("Giris placeholder bagli degil");
                else Gecti();

                Image dolgu;
                var cubuk = UiKit.Cubuk(kart, "Cubuk", Palet.UiYuzeyKoyu, Palet.UiVurgu, out dolgu);
                if (cubuk == null || dolgu == null) Ekle("Cubuk kurulmadi");
                else if (cubuk.sprite == null) Ekle("Cubuk zemini sprite'siz — keskin kose");
                else if (dolgu.sprite == null) Ekle("Cubuk dolgusu sprite'siz");
                else { UiKit.CubukAyarla(dolgu, 0.5f, 100f); Gecti(); }

                var rozet = UiKit.Rozet(kart, Palet.UiOnay, 16f);
                if (rozet == null || rozet.sprite == null) Ekle("Rozet daire sprite'i almamis");
                else Gecti();

                var cerceve = UiKit.Cerceve(kart, "Cerceve", Color.clear, 12, 3);
                if (cerceve == null || cerceve.sprite == null) Ekle("Cerceve halka sprite'i almamis");
                else if (cerceve.raycastTarget) Ekle("Cerceve tiklamalari yutuyor");
                else Gecti();

                var ayirac = UiKit.Ayirac(kart);
                if (ayirac == null) Ekle("Ayirac kurulmadi"); else Gecti();

                // Gorunmez kapsayici tiklama yutmamali.
                var bos = UiKit.Panel(kart, "Bos", Color.clear);
                var bosImg = bos.GetComponent<Image>();
                if (bosImg.raycastTarget) Ekle("Saydam kapsayici hala raycastTarget — altindaki dunya tiklanmaz");
                else Gecti();

                ScrollRect kaydirma;
                var liste = UiKit.KaydirmaListesi(kart, "Liste", out kaydirma);
                if (liste == null || kaydirma == null || kaydirma.content == null)
                    Ekle("KaydirmaListesi eksik kuruldu");
                else Gecti();

                // Yazi olceginin artan sirali oldugu burada denetlenmez: boyutlar const,
                // derleyici karsilastirmayi sabit katliyor ve denetim olu koda donuyor.

                if (UiKit.SureMetni(0) != "hazır") Ekle("SureMetni(0) 'hazir' donmedi");
                else if (UiKit.SureMetni(90) != "1 dk") Ekle("SureMetni(90) yanlis: " + UiKit.SureMetni(90));
                else Gecti();
            }
            catch (Exception e)
            {
                Ekle("UiKit kurulumu istisna atti: " + e.GetType().Name + " — " + e.Message);
            }
            finally
            {
                if (kok != null) UnityEngine.Object.DestroyImmediate(kok);
            }
        }

        private static void DogrulaSprite(RectTransform rt, string ad)
        {
            var img = rt.GetComponent<Image>();
            if (img == null) { Ekle(ad + " Image bileseni yok"); return; }
            if (img.sprite == null) { Ekle(ad + " sprite'siz — KESKIN KOSE cizer"); return; }
            if (img.type != Image.Type.Sliced) { Ekle(ad + " 9-dilim degil, kose orani bozulur"); return; }
            Gecti();
        }

        private static void DogrulaButon(Button b, string ad)
        {
            if (b == null) { Ekle(ad + " null"); return; }
            var img = b.GetComponent<Image>();
            if (img == null || img.sprite == null) { Ekle(ad + " sprite'siz — keskin kose"); return; }
            if (b.targetGraphic == null) { Ekle(ad + " targetGraphic bagli degil, basma efekti yok"); return; }
            if (b.GetComponentInChildren<Text>() == null) { Ekle(ad + " yazisi yok"); return; }
            // ⚠ Saydam zeminli buton (DuzButon) icin gercek bir tuzak: UiKit.Panel
            // saydam ogelerin raycast'ini kapatiyor ve buton sessizce tiklanamaz oluyor.
            if (!img.raycastTarget) { Ekle(ad + " raycastTarget kapali — TIKLANAMAZ"); return; }
            Gecti();
        }

        // ------------------------------------------------------------------ modal

        /// <summary>Modal panel kabugu: baslik, kapatma, icerik alani.</summary>
        private static void ModalDenetle()
        {
            GameObject kok = null;
            try
            {
                var kanvas = UiKit.Kanvas("ModalKanvas", 0);
                kok = kanvas.gameObject;

                bool kapandi = false;
                var panel = new Arayuz.Panel(kanvas.transform, "Test", "Baslik", () => kapandi = true);

                if (panel.Acik) Ekle("Panel kurulur kurulmaz acik — kapali baslamali");
                else Gecti();

                panel.Ac();
                if (!panel.Acik) Ekle("Panel.Ac() acmadi"); else Gecti();

                if (panel.Icerik == null) Ekle("Panel.Icerik yok"); else Gecti();
                if (panel.Baslik == null || panel.Baslik.text != "Baslik") Ekle("Panel basligi yanlis");
                else Gecti();

                panel.Kapat();
                if (panel.Acik) Ekle("Panel.Kapat() kapatmadi");
                else if (!kapandi) Ekle("Panel kapaninca geri cagri calismadi");
                else Gecti();
            }
            catch (Exception e)
            {
                Ekle("Modal panel istisna atti: " + e.GetType().Name + " — " + e.Message);
            }
            finally
            {
                if (kok != null) UnityEngine.Object.DestroyImmediate(kok);
            }
        }

        // ------------------------------------------------------------------ kayit

        private static void Gecti() => _gecen++;

        private static void Ekle(string mesaj) => _hatalar.Add(mesaj);
    }
}
