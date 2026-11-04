using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PigTycoon.EditorTools
{
    /// <summary>Includes PNGs that presentation code loads from disk rather than serialized Unity references.</summary>
    public sealed class PackageRuntimeArt : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows64) return;
            string output = Path.GetFullPath(report.summary.outputPath);
            string dataRoot = Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "_Data");
            string packageRoot = Path.Combine(dataRoot, "PigTycoonArt");
            string sourceRoot = Path.Combine(Application.dataPath, "Art");
            string[] files = Directory.GetFiles(sourceRoot, "*.png", SearchOption.AllDirectories);
            if (files.Length == 0) throw new BuildFailedException("No runtime PNG assets found in " + sourceRoot);
            // Start clean: art removed from Assets (e.g. held-back, unapproved pictures) must not survive in the build
            // from an earlier build into the same folder.
            if (Directory.Exists(packageRoot)) Directory.Delete(packageRoot, true);

            foreach (string source in files.OrderBy(path => path, StringComparer.Ordinal))
            {
                string relative = source.Substring(Application.dataPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string destination = Path.Combine(packageRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination, true);
            }

            Debug.Log($"[PlayableDemoBuild] Packaged {files.Length} runtime PNGs in {packageRoot}");
        }
    }
}
