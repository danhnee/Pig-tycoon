using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PigTycoon.EditorTools
{
    /// <summary>Builds an explicit demo scene without changing the project's build settings.</summary>
    public static class PlayableDemoBuild
    {
        public static void BuildWindows()
        {
            if (Application.unityVersion != "6000.6.3f1")
                throw new BuildFailedException("Pig Tycoon requires Unity 6000.6.3f1.");

            string scene = Argument("-demoScene", "Assets/Scenes/Farm_Main.unity");
            string output = Path.GetFullPath(Argument("-demoOutput", "Builds/PlayableDemo/PigTycoonDemo.exe"));
            bool clean = Environment.GetCommandLineArgs().Contains("-demoClean");
            if (!File.Exists(scene) || !scene.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Demo scene does not exist: " + scene);
            if (!output.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Windows demo output must end in .exe: " + output);

            Directory.CreateDirectory(Path.GetDirectoryName(output));
            // Initial import can retain editor-only textures and sample assets. Release them
            // before loading the player scene, so import and player-build peaks don't overlap.
            long beforeUnload = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();
            Debug.Log($"[PlayableDemoBuild] Editor allocations before/after unload: {beforeUnload / 1048576} / {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576} MiB");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | (clean ? BuildOptions.CleanBuildCache : BuildOptions.None)
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Demo build failed: {report.summary.result}, {report.summary.totalErrors} errors.");

            Debug.Log($"[PlayableDemoBuild] {output}; {report.summary.totalSize} bytes; scene={scene}");
        }

        private static string Argument(string name, string fallback)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, name);
            if (index < 0) return fallback;
            if (index + 1 >= arguments.Length || arguments[index + 1].StartsWith("-", StringComparison.Ordinal))
                throw new BuildFailedException("Missing value for " + name);
            return arguments[index + 1];
        }
    }

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
