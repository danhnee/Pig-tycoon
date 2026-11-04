using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PigTycoon.EditorTools
{
    /// <summary>
    /// Appends every Assets/Art/PixelMyth/UI/**/*.png to the Resources/PigTycoonArt byte package, art-index.json and the
    /// package manifest, in the same format as tools/package_ui_sprite_resources.py (exact PNG bytes as *.bytes TextAssets,
    /// uuid5 .meta GUIDs). Built players read these through UISpriteLoader; the Editor reads Assets/Art directly.
    /// Only appends: existing entries are kept. Run from Pig Tycoon > Package PixelMyth UI sprites, or headless via
    /// -executeMethod PigTycoon.EditorTools.MythHudPackaging.Package (through tools/headless_runner.py).
    /// </summary>
    public static class MythHudPackaging
    {
        const string SourceFolder = "Assets/Art/PixelMyth/UI";
        const string OutputFolder = "Assets/Resources/PigTycoonArt";
        const string ManifestPath = "Docs/plans/2026-10-30-ui-sprite-package.json";
        const string Namespace = "5017d341-d6e2-4596-8d38-561f727446d9"; // same as the python packager

        [Serializable] class Catalog { public string[] paths = new string[0]; }
        [Serializable] class Entry { public string source; public string package; public string sha256; public int bytes; }
        [Serializable] class Manifest { public string purpose; public string excludes; public Entry[] files = new Entry[0]; }

        [MenuItem("Pig Tycoon/Package PixelMyth UI sprites")]
        public static void Package()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string srcRoot = Path.Combine(root, SourceFolder);
            if (!Directory.Exists(srcRoot)) throw new DirectoryNotFoundException(srcRoot);
            PackageFiles(root, new List<string>(Directory.GetFiles(srcRoot, "*.png", SearchOption.AllDirectories)), "PixelMyth UI");
        }

        /// <summary>
        /// Android: packs the HUD/UI folders plus every art file the Windows player actually read during the full
        /// playthrough (.agent_scratch/art_usage.txt, written with -pigtycoon-artlog) — built players on Android can only
        /// read Resources, so anything UISpriteLoader loads from disk must be here. Run before AndroidBuildPipeline.
        /// </summary>
        [MenuItem("Pig Tycoon/Package art used at runtime (Android)")]
        public static void PackageRuntimeArt()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            var files = new List<string>();
            foreach (string folder in new[] { "Assets/Art/PixelMyth/UI", "Assets/Art/PixelMyth/FX", "Assets/Art/UI", "Assets/Art/Sprites", "Assets/Art/Tiles" }) // các thư mục UISpriteLoader gọi theo đường dẫn
            {
                string dir = Path.Combine(root, folder);
                if (Directory.Exists(dir)) files.AddRange(Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories));
            }
            // chân dung / biểu tượng nhân vật lấy ô đầu của sheet idle (LoadCharacterCellZero) — ~5 MB cho cả 165 sheet
            string chars = Path.Combine(root, "Assets/Art/PixelMyth/Characters");
            if (Directory.Exists(chars)) files.AddRange(Directory.GetFiles(chars, "*_idle.png", SearchOption.AllDirectories));
            string usage = Path.Combine(root, ".agent_scratch", "art_usage.txt");
            int fromLog = 0;
            if (File.Exists(usage))
                foreach (string line in File.ReadAllLines(usage))
                {
                    string rel = line.Trim();
                    if (rel.Length == 0) continue;
                    string full = Path.Combine(root, "Assets", rel.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(full)) { files.Add(full); fromLog++; }
                }
            else Debug.LogWarning("MythHudPackaging: no .agent_scratch/art_usage.txt — only the UI folders are packed. Run the video playthrough first.");
            var unique = new List<string>(new HashSet<string>(files, StringComparer.OrdinalIgnoreCase));
            PackageFiles(root, unique, $"runtime art ({fromLog} from the playthrough log)");
        }

        static void PackageFiles(string root, List<string> sources, string what)
        {
            string outRoot = Path.Combine(root, OutputFolder);
            Directory.CreateDirectory(outRoot);
            sources.Sort(StringComparer.Ordinal);

            var entries = new List<Entry>();
            var newPaths = new List<string>();
            foreach (string file in sources)
            {
                string projectRel = Rel(root, file);                    // Assets/Art/PixelMyth/UI/Kit/x.png
                string key = projectRel.Substring("Assets/".Length);    // Art/PixelMyth/UI/Kit/x.png
                string package = Path.Combine(outRoot, Path.ChangeExtension(key, ".bytes").Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(package));
                File.Copy(file, package, true);
                WriteMeta(root, package, false);
                entries.Add(new Entry
                {
                    source = projectRel,
                    package = Rel(root, package),
                    sha256 = Sha256(file),
                    bytes = (int)new FileInfo(file).Length
                });
                newPaths.Add(key);
            }
            // Folder metas for every new directory between the output root and each file.
            foreach (string dir in Directory.GetDirectories(outRoot, "*", SearchOption.AllDirectories))
                WriteMeta(root, dir, true);

            MergeCatalog(Path.Combine(outRoot, "art-index.json"), newPaths, root);
            MergeManifest(Path.Combine(root, ManifestPath), entries);
            AssetDatabase.Refresh();
            Debug.Log("MythHudPackaging: packaged " + entries.Count + " PNGs (" + what + ") into " + OutputFolder);
        }

        static void MergeCatalog(string catalogPath, List<string> added, string root)
        {
            var paths = new List<string>();
            if (File.Exists(catalogPath))
            {
                var existing = JsonUtility.FromJson<Catalog>(File.ReadAllText(catalogPath, Encoding.UTF8));
                if (existing != null && existing.paths != null) paths.AddRange(existing.paths);
            }
            var set = new HashSet<string>(paths, StringComparer.Ordinal);
            foreach (string p in added) if (set.Add(p)) paths.Add(p);
            paths.Sort(CompareSegments); // python sorts Path objects (segment-wise, case-insensitive on Windows)
            var sb = new StringBuilder();
            sb.Append("{\n  \"paths\": [\n");
            for (int i = 0; i < paths.Count; i++)
                sb.Append("    \"").Append(paths[i].Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"').Append(i + 1 < paths.Count ? ",\n" : "\n");
            sb.Append("  ]\n}");
            File.WriteAllText(catalogPath, sb.ToString(), new UTF8Encoding(false));
            WriteMeta(root, catalogPath, false, "TextScriptImporter");
        }

        static void MergeManifest(string manifestPath, List<Entry> added)
        {
            if (!File.Exists(manifestPath)) return; // nothing to keep in sync
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath, Encoding.UTF8)) ?? new Manifest();
            var byPackage = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in manifest.files) byPackage[e.package] = e;
            foreach (var e in added) byPackage[e.package] = e;
            var list = new List<Entry>(byPackage.Values);
            list.Sort((a, b) => CompareSegments(a.source, b.source));
            manifest.files = list.ToArray();
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true), new UTF8Encoding(false));
        }

        static int CompareSegments(string a, string b)
        {
            string[] x = a.Split('/'), y = b.Split('/');
            for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
            {
                int c = string.Compare(x[i], y[i], StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
            }
            return x.Length.CompareTo(y.Length);
        }

        static string Rel(string root, string full) =>
            full.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');

        static string Sha256(string file)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(File.ReadAllBytes(file));
                var sb = new StringBuilder();
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // uuid5(NS, project-relative posix path) -- identical to the python packager, so GUIDs never churn.
        static string Guid5(string name)
        {
            byte[] ns = new byte[16];
            string hex = Namespace.Replace("-", string.Empty);
            for (int i = 0; i < 16; i++) ns[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            byte[] data = new byte[16 + nameBytes.Length];
            Buffer.BlockCopy(ns, 0, data, 0, 16);
            Buffer.BlockCopy(nameBytes, 0, data, 16, nameBytes.Length);
            byte[] h;
            using (var sha1 = SHA1.Create()) h = sha1.ComputeHash(data);
            h[6] = (byte)((h[6] & 0x0f) | 0x50);
            h[8] = (byte)((h[8] & 0x3f) | 0x80);
            var sb = new StringBuilder();
            for (int i = 0; i < 16; i++) sb.Append(h[i].ToString("x2"));
            return sb.ToString();
        }

        static void WriteMeta(string root, string path, bool folder, string importer = "TextScriptImporter")
        {
            string meta = path + ".meta";
            if (File.Exists(meta)) return; // keep GUIDs of anything already imported
            string tail = folder ? "folderAsset: yes\nDefaultImporter:" : importer + ":";
            File.WriteAllText(meta,
                "fileFormatVersion: 2\nguid: " + Guid5(Rel(root, path)) + "\n" + tail +
                "\n  externalObjects: {}\n  userData: Original PNG byte package for UISpriteLoader\n  assetBundleName:\n  assetBundleVariant:\n",
                new UTF8Encoding(false));
        }
    }
}
