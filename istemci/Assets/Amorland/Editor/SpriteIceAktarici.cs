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

        private void OnPreprocessTexture()
        {
            if (assetPath == null || assetPath.Replace('\\', '/').IndexOf(Klasor) < 0) return;

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
    }
}
