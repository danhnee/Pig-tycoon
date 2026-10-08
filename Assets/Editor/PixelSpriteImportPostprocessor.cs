using UnityEditor;
using UnityEngine;

namespace PigTycoon.EditorTools
{
    /// <summary>
    /// P0.05 — khóa filter pixel cho art mới khi import.
    /// Không đổi spritePixelsToUnits: art Prototype đang là 32 PPU, ép 16 sẽ nhân đôi kích thước thế giới.
    /// </summary>
    public class PixelSpriteImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath == null || !assetPath.StartsWith("Assets/Art/"))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
        }
    }
}
