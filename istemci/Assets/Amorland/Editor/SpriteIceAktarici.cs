using UnityEditor;
using UnityEngine;

namespace Amorland.EditorAraclari
{
    /// <summary>
    /// <c>Resources/sprite/</c> altindaki PNG'lerin import ayarlarini zorlar.
    ///
    /// ⚠ Neden gerekli: Unity'nin varsayilan texture import'u mipmap uretir ve
    /// sikistirma uygular. Cizim sprite'lari boyle alininca gorunur sekilde
    /// BULANIKLASIYOR — ilk denemede otlar ve urunler yagli boya lekesi gibi ciktu.
    /// Ayar elle degil kodla verilir: mimari kural geregi projede editor kurulumu
    /// gerektiren adim birakilmaz, ve .meta dosyasini elle yazmak surum kirilgan.
    /// </summary>
    public class SpriteIceAktarici : AssetPostprocessor
    {
        private const string Klasor = "/Resources/sprite/";
        private const string KaroKlasoru = "/Resources/harita2d/";
        private const string Karo64Klasoru = "/Resources/harita2d64/";

        private void OnPreprocessTexture()
        {
            string y = assetPath == null ? null : assetPath.Replace('\\', '/');
            if (y == null) return;

            if (y.IndexOf(Karo64Klasoru) >= 0)
            {
                Karo64Sayfasi();
                return;
            }
            if (y.IndexOf(KaroKlasoru) >= 0)
            {
                KaroSayfasi();
                return;
            }
            if (y.IndexOf(Klasor) < 0) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 64f;          // SpriteDeposu.PikselBirim ile ayni
            ti.mipmapEnabled = false;              // 2B sprite'ta mipmap = bulanik
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 512;
            ti.sRGBTexture = true;
        }

        /// <summary>
        /// Karo sayfasi (Tiny Farm tilemap) ayarlari.
        ///
        /// ⚠ <see cref="TextureImporter.npotScale"/> BURADAKI HER SEYIN ANAHTARI.
        /// Sayfa 192x176 — ikinin kuvveti degil. Unity varsayilani onu en yakin
        /// kuvvete OLCEKLER; 16 px'lik karo izgarasi kayar ve harita tamamen yanlis
        /// karolarla dosenir. Ilk denemede butun zemin ahir duvari cikti.
        ///
        /// Nokta filtre ve sikistirmasiz doku de sart: piksel sanati bilineer
        /// filtreyle bulanir, sikistirma ise karo kenarlarinda kirli piksel birakir.
        /// </summary>
        /// <summary>
        /// 64px sayfalar (rpg-base ailesi) icin KAYNAK doku ayarlari.
        ///
        /// ⚠ Bu sayfalar dogrudan cizilmez: Karo64 calisma aninda karolari extrude
        /// atlasa kopyalar. O yuzden buradaki tek kritik ayar isReadable — kapali
        /// olursa GetPixels32 patlar. Mipmap/filtre atlasta uretilir; kaynakta gereksiz.
        /// npotScale yine None: 1280x832 olceklenirse 64 px izgara kayar.
        /// </summary>
        private void Karo64Sayfasi()
        {
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.isReadable = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
            ti.sRGBTexture = true;
        }

        private void KaroSayfasi()
        {
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16f;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Point;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
            ti.sRGBTexture = true;
        }
    }
}
