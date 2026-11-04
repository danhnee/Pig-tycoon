using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace PigTycoon.EditorTools.PixelMyth
{
    /// <summary>
    /// Imports the generated pixel art under Assets/Art/PixelMyth: point filter, 32 PPU (ADR-0005),
    /// uncompressed, and grid-slices every sheet that has a sibling JSON written by tools/pixelart/pixelize.py
    /// ({"cell":[w,h],"rows":[...],"frames_per_row":n,"pivot_px_from_bottom_left":[x,y]}).
    /// Sprite names: &lt;sheet&gt;_&lt;row&gt;_&lt;frame&gt;, e.g. khoa_walk_left_03.
    /// Run headless: -executeMethod PigTycoon.EditorTools.PixelMyth.PixelMythSpriteImport.ImportAll
    /// </summary>
    public static class PixelMythSpriteImport
    {
        public const string Root = "Assets/Art/PixelMyth";
        public const int PixelsPerUnit = 32;

        [Serializable]
        private sealed class SheetMeta
        {
            public int[] cell;
            public string[] rows;
            public int frames_per_row;
            public int[] pivot_px_from_bottom_left;
            public float fps;
        }

        [Serializable]
        private sealed class ListEntry
        {
            public string file;
            public int[] size;
            public int[] pivot_px;
        }

        [Serializable]
        private sealed class ListManifest
        {
            public ListEntry[] sprites;
        }

        // pivots from folder manifests such as Decor/decor.json: {"sprites":[{"file","size","pivot_px" (top-left, y down)}]}
        private static readonly Dictionary<string, Vector2> listPivots = new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);

        private static void LoadListPivots()
        {
            listPivots.Clear();
            foreach (var json in Directory.GetFiles(Root, "*.json", SearchOption.AllDirectories))
            {
                var dir = Path.GetDirectoryName(json).Replace('\\', '/');
                ListManifest m = null;
                try { m = JsonUtility.FromJson<ListManifest>(File.ReadAllText(json)); }
                catch { continue; }
                if (m?.sprites == null) continue;
                foreach (var e in m.sprites)
                {
                    if (e?.file == null || e.size == null || e.size.Length < 2 || e.pivot_px == null || e.pivot_px.Length < 2) continue;
                    listPivots[$"{dir}/{e.file}"] = new Vector2(e.pivot_px[0] / (float)e.size[0], 1f - e.pivot_px[1] / (float)e.size[1]);
                }
            }
        }

        [MenuItem("Pig Tycoon/Pixel Myth/Import All Sprites")]
        public static void ImportAll()
        {
            var pngs = Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => !Path.GetFileName(p).StartsWith("preview", StringComparison.OrdinalIgnoreCase))
                .ToList();
            LoadListPivots();
            int sliced = 0, single = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var png in pngs)
                {
                    if (Configure(png)) sliced++; else single++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
            Debug.Log($"[PixelMyth] imported {pngs.Count} textures: {sliced} sliced sheets, {single} single sprites");
        }

        private static bool Configure(string png)
        {
            var importer = AssetImporter.GetAtPath(png) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(png) as TextureImporter;
                if (importer == null) return false;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.isReadable = false;

            var jsonPath = Path.ChangeExtension(png, ".json");
            SheetMeta meta = null;
            if (File.Exists(jsonPath))
            {
                try { meta = JsonUtility.FromJson<SheetMeta>(File.ReadAllText(jsonPath)); }
                catch (Exception e) { Debug.LogWarning($"[PixelMyth] bad sheet json {jsonPath}: {e.Message}"); }
            }

            bool isSheet = meta != null && meta.cell != null && meta.cell.Length == 2 && meta.rows != null && meta.frames_per_row > 0;
            importer.spriteImportMode = isSheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            if (!isSheet && listPivots.TryGetValue(png, out var listPivot))
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = listPivot;
                importer.SetTextureSettings(settings);
            }
            importer.SaveAndReimport();
            if (!isSheet) return false;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
            int w = meta.cell[0], h = meta.cell[1];
            var pivot = meta.pivot_px_from_bottom_left != null && meta.pivot_px_from_bottom_left.Length == 2
                ? new Vector2(meta.pivot_px_from_bottom_left[0] / (float)w, meta.pivot_px_from_bottom_left[1] / (float)h)
                : new Vector2(0.5f, 0f);
            var stem = Path.GetFileNameWithoutExtension(png);
            var rects = new List<SpriteRect>();
            for (int r = 0; r < meta.rows.Length; r++)
            {
                for (int c = 0; c < meta.frames_per_row; c++)
                {
                    rects.Add(new SpriteRect
                    {
                        name = $"{stem}_{meta.rows[r]}_{c:00}",
                        rect = new Rect(c * w, tex.height - (r + 1) * h, w, h),
                        alignment = SpriteAlignment.Custom,
                        pivot = pivot,
                        spriteID = GUID.Generate(),
                    });
                }
            }
            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            return true;
        }
    }
}
