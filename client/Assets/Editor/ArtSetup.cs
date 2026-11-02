using System.IO;
using UnityEditor;
using UnityEngine;

namespace IdleBlade.Editor
{
    [InitializeOnLoad]
    public static class ArtSetup
    {
        static ArtSetup() { EditorApplication.delayCall += Ensure; }
        static void Ensure()
        {
            const string path = "Assets/Resources/Art/DefaultTheme.asset";
            if (File.Exists(path)) return;
            Directory.CreateDirectory("Assets/Resources/Art");
            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ArtTheme>(), path);
            AssetDatabase.SaveAssets();
        }
    }
    public class ArtImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/") && !assetPath.StartsWith("Assets/Resources/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            if(assetPath.StartsWith("Assets/Resources/Art/Rigs/")) {
                importer.maxTextureSize=2048;
                importer.npotScale=TextureImporterNPOTScale.None;
            }
            if(assetPath.StartsWith("Assets/Resources/Art/Main/")) {
                // Keep generation originals intact; bound runtime texture memory during import.
                string id=Path.GetFileNameWithoutExtension(assetPath);
                importer.maxTextureSize=id.StartsWith("bg-")||id.StartsWith("cover-")?2048:
                    (id.StartsWith("hero-")||id.StartsWith("boss-")||id.StartsWith("panel-"))?512:
                    (id.StartsWith("mob-")||id.StartsWith("axe-"))?256:128;
                importer.npotScale=TextureImporterNPOTScale.None;
            }
        }
    }
}
