using UnityEditor;
using UnityEngine;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Impone los ajustes de importación de los atlas de plasticina (<c>Assets/Resources/Plasticina/**.png</c>, los escribe <c>tools/juegos/empaqueta.py</c>) en cada importación, por si el <c>.meta</c> se rompe o se borra:
    /// textura normal (no sprite: <c>PlasticArt</c> corta cada sprite con las medidas del manifiesto), sin mipmaps (se dibujan a su tamaño), SIN reescalar (no son potencias de 2) y SIN compresión que ensucie el degradado de la plasticina.
    /// El borde del nine-slice y los pivotes NO van en los ajustes de importación: están en el manifiesto de cada grupo.
    /// </summary>
    public sealed class PlasticinaImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/Plasticina/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;       // Unity rellena el color de los píxeles transparentes del borde: sin halos oscuros al filtrar
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 4096;
            android.format = TextureImporterFormat.RGBA32;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
