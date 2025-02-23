using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kardx.Editor
{
    public class WebPImporterPlugin : AssetPostprocessor
    {
        private static readonly string[] WebPExtensions = { ".webp", ".WEBP" };

        private void OnPreprocessTexture()
        {
            if (!WebPExtensions.Any(ext => assetPath.EndsWith(ext)))
                return;

            TextureImporter textureImporter = assetImporter as TextureImporter;
            if (textureImporter == null)
                return;

            Debug.Log($"[WebPImporter] Processing WebP image: {assetPath}");

            // Configure texture importer settings for WebP
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.mipmapEnabled = false;
            textureImporter.isReadable = true;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.textureCompression = TextureImporterCompression.Compressed;
            textureImporter.alphaIsTransparency = true;
            textureImporter.npotScale = TextureImporterNPOTScale.None;
            textureImporter.sRGBTexture = true;

            // Configure platform-specific settings
            var platformSettings = textureImporter.GetDefaultPlatformTextureSettings();
            platformSettings.overridden = true;
            platformSettings.maxTextureSize = 2048;
            platformSettings.format = TextureImporterFormat.Automatic;
            platformSettings.textureCompression = TextureImporterCompression.Compressed;
            platformSettings.compressionQuality = 100;
            platformSettings.allowsAlphaSplitting = false;
            platformSettings.androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit;
            textureImporter.SetPlatformTextureSettings(platformSettings);

            Debug.Log($"[WebPImporter] Successfully configured import settings for: {assetPath}");
        }
    }

    public class WebPImporterMenu
    {
        [MenuItem("Assets/Reimport WebP Images")]
        private static void ReimportWebPImages()
        {
            var webpFiles = AssetDatabase
                .FindAssets("t:Texture")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => path.EndsWith(".webp", System.StringComparison.OrdinalIgnoreCase));

            foreach (string file in webpFiles)
            {
                Debug.Log($"[WebPImporter] Reimporting: {file}");
                AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
