using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PigTycoon.Presentation;
using UnityEditor;
using UnityEngine;

namespace PigTycoon.EditorTools.PixelMyth
{
    /// <summary>
    /// Builds one PixelMythSpriteLibrary per folder under Assets/Art/PixelMyth/Characters/&lt;family&gt;
    /// from the sheets PixelMythSpriteImport sliced (sprite names &lt;family&gt;_&lt;clip&gt;_&lt;dir&gt;_&lt;nn&gt;).
    /// Output: Assets/Resources/PixelMythLibs/&lt;family&gt;.asset (loaded per family at runtime, see PixelMythRegistry)
    /// Headless: -executeMethod PigTycoon.EditorTools.PixelMyth.PixelMythLibraryBuilder.ImportAndBuild
    /// </summary>
    public static class PixelMythLibraryBuilder
    {
        public const string CharactersRoot = "Assets/Art/PixelMyth/Characters";
        public const string LibrariesRoot = "Assets/Resources/" + PixelMythRegistry.LibrariesResourceFolder;
        private const string OldLibrariesRoot = "Assets/Art/PixelMyth/Libraries";

        // One-shot clips and the frame their effect lands on (hit, pour, whistle note), as a fraction of length.
        private static readonly Dictionary<string, float> OneShots = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            { "attack", 0.5f }, { "interact", 0.5f }, { "whistle", 0.4f }, { "telegraph", 0.99f },
            { "hit", 0.0f }, { "death", 0.99f }, { "bind", 0.6f }, { "drag", 0.5f },
        };

        [Serializable]
        private sealed class SheetMeta { public float fps; public float stride_m_per_cycle; public float stride_m; }

        [MenuItem("Pig Tycoon/Pixel Myth/Import Sprites + Build Libraries")]
        public static void ImportAndBuild()
        {
            PixelMythSpriteImport.ImportAll();
            BuildAll();
        }

        [MenuItem("Pig Tycoon/Pixel Myth/Build Libraries")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(LibrariesRoot))
            {
                AssetDatabase.CreateFolder("Assets/Resources", PixelMythRegistry.LibrariesResourceFolder);
            }
            // Move (not recreate) the old libraries so scene/prefab references keep their GUIDs.
            if (AssetDatabase.IsValidFolder(OldLibrariesRoot))
            {
                foreach (var g in AssetDatabase.FindAssets("t:PixelMythSpriteLibrary", new[] { OldLibrariesRoot }))
                {
                    var from = AssetDatabase.GUIDToAssetPath(g);
                    var err = AssetDatabase.MoveAsset(from, $"{LibrariesRoot}/{Path.GetFileName(from)}");
                    if (!string.IsNullOrEmpty(err)) Debug.LogError($"[PixelMyth] move {from}: {err}");
                }
            }
            foreach (var dir in Directory.GetDirectories(CharactersRoot))
            {
                var family = Path.GetFileName(dir);
                var lib = AssetDatabase.LoadAssetAtPath<PixelMythSpriteLibrary>($"{LibrariesRoot}/{family}.asset");
                bool created = lib == null;
                if (created) lib = ScriptableObject.CreateInstance<PixelMythSpriteLibrary>();
                lib.Family = family;
                lib.Clips.Clear();
                foreach (var png in Directory.GetFiles(dir, "*.png").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
                {
                    var stem = Path.GetFileNameWithoutExtension(png);
                    if (!stem.StartsWith(family + "_", StringComparison.OrdinalIgnoreCase)) continue;
                    var clipName = stem.Substring(family.Length + 1);
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(png).OfType<Sprite>().ToList();
                    if (sprites.Count == 0) continue;
                    float fps = 10f, stride = 0f;
                    var json = Path.ChangeExtension(png, ".json");
                    if (File.Exists(json))
                    {
                        var m = JsonUtility.FromJson<SheetMeta>(File.ReadAllText(json));
                        if (m != null && m.fps > 0f) fps = m.fps;
                        if (m != null) stride = m.stride_m_per_cycle > 0f ? m.stride_m_per_cycle : m.stride_m;
                    }
                    var clip = new PixelMythSpriteLibrary.Clip
                    {
                        Name = clipName,
                        Fps = fps,
                        StrideMetersPerCycle = stride,
                        Down = Row(sprites, stem, "down"),
                        Left = Row(sprites, stem, "left"),
                        Up = Row(sprites, stem, "up"),
                        Right = Row(sprites, stem, "right"),
                    };
                    if (OneShots.TryGetValue(clipName, out var at))
                    {
                        clip.Loop = false;
                        clip.EffectFrame = Mathf.Clamp(Mathf.RoundToInt(at * (clip.Down.Length - 1)), 0, Mathf.Max(0, clip.Down.Length - 1));
                    }
                    if (clipName == "death") clip.Loop = false;
                    lib.Clips.Add(clip);
                }
                if (created) AssetDatabase.CreateAsset(lib, $"{LibrariesRoot}/{family}.asset");
                EditorUtility.SetDirty(lib);
                Debug.Log($"[PixelMyth] library {family}: {string.Join(", ", lib.Clips.Select(c => $"{c.Name}({c.Down.Length})"))}");
            }
            SyncRegistry();
            AssetDatabase.SaveAssets();
        }

        private static void SyncRegistry()
        {
            const string path = "Assets/Resources/PixelMythRegistry.asset";
            var reg = AssetDatabase.LoadAssetAtPath<PixelMythRegistry>(path);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<PixelMythRegistry>();
                AssetDatabase.CreateAsset(reg, path);
            }
            // Names only: a reference list would make Unity load every library's textures with the registry.
            reg.Families = AssetDatabase.FindAssets("t:PixelMythSpriteLibrary", new[] { LibrariesRoot })
                .Select(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(f => f, StringComparer.Ordinal).ToList();
            EditorUtility.SetDirty(reg);
        }

        private static Sprite[] Row(List<Sprite> sprites, string stem, string dir)
        {
            var prefix = $"{stem}_{dir}_";
            return sprites.Where(s => s.name.StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
        }
    }
}
