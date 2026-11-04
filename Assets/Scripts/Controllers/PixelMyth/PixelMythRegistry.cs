using System.Collections.Generic;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Runtime lookup of sprite libraries for views that are spawned in code (pigs, enemies, NPCs).
    /// Lives at Resources/PixelMythRegistry.asset; PixelMythLibraryBuilder keeps it in sync.
    /// The registry holds family names only. Each library is its own Resources asset
    /// (Resources/PixelMythLibs/&lt;family&gt;) loaded on first use, so only the sheets a scene really
    /// shows reach memory. A list of direct references made Unity load every sheet at once
    /// (~2 GB of RGBA with the gear and skill clips; the 3 Nov player died at the 3 GB cap).
    /// </summary>
    [CreateAssetMenu(menuName = "Pig Tycoon/Pixel Myth Registry")]
    public sealed class PixelMythRegistry : ScriptableObject
    {
        public const string LibrariesResourceFolder = "PixelMythLibs";

        public List<string> Families = new List<string>();

        private static Dictionary<string, string> exactNames;
        private static readonly Dictionary<string, PixelMythSpriteLibrary> loaded =
            new Dictionary<string, PixelMythSpriteLibrary>(System.StringComparer.OrdinalIgnoreCase);

        public static PixelMythSpriteLibrary Find(string family)
        {
            if (string.IsNullOrEmpty(family)) return null;
            if (loaded.TryGetValue(family, out var cached)) return cached;
            if (exactNames == null)
            {
                exactNames = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
                var reg = Resources.Load<PixelMythRegistry>("PixelMythRegistry");
                if (reg != null)
                {
                    foreach (var f in reg.Families) if (!string.IsNullOrEmpty(f)) exactNames[f] = f;
                    Resources.UnloadAsset(reg);
                }
            }
            PixelMythSpriteLibrary lib = null;
            if (exactNames.TryGetValue(family, out var exact))
                lib = Resources.Load<PixelMythSpriteLibrary>(LibrariesResourceFolder + "/" + exact);
            loaded[family] = lib; // misses are cached too (views probe a family, then a fallback, on every spawn)
            return lib;
        }
    }
}
