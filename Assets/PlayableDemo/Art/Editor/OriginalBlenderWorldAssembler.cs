using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PigTycoon.Candidates.EvaluatedExport;

namespace PigTycoon.OriginalWorld.EditorTools
{
    /// <summary>Assembles only the actual pinned source bundle. No replacement terrain/props/anchors.</summary>
    public static class OriginalBlenderWorldAssembler
    {
        public const string ScenePath = "Assets/Scenes/OriginalBlenderWorld.unity";

        [Serializable] public sealed class MaterialBinding
        {
            public int sourceMaterialId;
            public string sourceMaterialName, unityMaterialAssetPath, mappingStatus;
        }
        [Serializable] public sealed class Request
        {
            public string bundleDirectory, expectedRunId, expectedSourceSha256, expectedManifestSha256;
            public MaterialBinding[] materialBindings;
            public bool allowExplicitUnverifiedMaterialCandidates;
            public bool addReviewCamera;
            public Vector3 reviewCameraPosition, reviewCameraEuler;
        }
        [Serializable] private sealed class SourceManifest
        {
            public string source, source_sha256, run_id;
            public Counter counters;
            public MaterialRecord[] materials;
        }
        [Serializable] private sealed class Counter { public long exported; }
        [Serializable] private sealed class MaterialRecord { public string name, status; }

        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-originalWorldImportRequest");
            if (index < 0 || index + 1 >= args.Length)
                throw new ArgumentException("Pass -originalWorldImportRequest with a concrete pinned request JSON.");
            Build(JsonUtility.FromJson<Request>(File.ReadAllText(args[index + 1])));
        }

        public static GameObject Build(Request request)
        {
            if (Application.unityVersion != "6000.6.3f1" || !Application.isBatchMode)
                throw new InvalidOperationException("Use the pinned headless Unity assembly job; no interactive scene overwrite.");
            if (request == null) throw new ArgumentNullException(nameof(request));
            string bundle = Path.GetFullPath(request.bundleDirectory);
            if (File.Exists(Path.Combine(bundle, "FAILED.json"))) throw new InvalidDataException("Failed source export bundle.");
            string manifestPath = Path.Combine(bundle, "manifest.json");
            VerifyHash(manifestPath, request.expectedManifestSha256);
            var source = JsonUtility.FromJson<SourceManifest>(File.ReadAllText(manifestPath));
            if (source == null || source.run_id != request.expectedRunId || source.source_sha256 != request.expectedSourceSha256)
                throw new InvalidDataException("Wrong original-world source/run identity.");
            if (File.Exists(source.source))
                VerifyHash(source.source, request.expectedSourceSha256);
            if (source.counters == null || source.counters.exported <= 0 || source.materials == null ||
                request.materialBindings == null || request.materialBindings.Length != source.materials.Length)
                throw new InvalidDataException("Complete per-source material mapping and source counts are required.");
            Material[] materials = new Material[source.materials.Length];
            bool anyUnverified = false;
            foreach (var entry in request.materialBindings)
            {
                if (entry.sourceMaterialId < 0 || entry.sourceMaterialId >= materials.Length || materials[entry.sourceMaterialId] != null ||
                    source.materials[entry.sourceMaterialId].name != entry.sourceMaterialName || string.IsNullOrWhiteSpace(entry.mappingStatus))
                    throw new InvalidDataException("Invalid/duplicate/misidentified source material mapping.");
                bool unverified = entry.mappingStatus != "NATIVE_REVIEWED_SOURCE_MAPPING";
                if (unverified && !request.allowExplicitUnverifiedMaterialCandidates)
                    throw new InvalidDataException("Unverified material candidate was not explicitly declared: " + entry.sourceMaterialName);
                var material = AssetDatabase.LoadAssetAtPath<Material>(entry.unityMaterialAssetPath);
                if (material == null || !material.enableInstancing)
                    throw new InvalidDataException("Missing material or disabled instancing: " + entry.unityMaterialAssetPath);
                materials[entry.sourceMaterialId] = material;
                anyUnverified |= unverified;
            }
            if (materials.Any(material => material == null)) throw new InvalidDataException("Unbound source material ID.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var world = new GameObject("OriginalBlenderWorld_SourceGeometry");
            var identity = world.AddComponent<OriginalWorldSourceBinding>();
            identity.SourceBlendPath = source.source;
            identity.SourceSha256 = request.expectedSourceSha256;
            identity.ExportRunId = request.expectedRunId;
            identity.ManifestSha256 = request.expectedManifestSha256;
            identity.ExpectedInstanceRecords = source.counters.exported;
            identity.MaterialMappingStatus = anyUnverified ? "EXPLICIT_CANDIDATES_NATIVE_APPEARANCE_NOT_VERIFIED" : "NATIVE_REVIEWED_SOURCE_MAPPING";
            var viewer = world.AddComponent<EvaluatedMapViewer>();
            identity.Viewer = viewer;
            viewer.bundleDirectory = bundle;
            viewer.expectedRunId = request.expectedRunId;
            viewer.expectedSourceSha256 = request.expectedSourceSha256;
            viewer.reviewedMaterials = materials;
            viewer.Reload();
            if (viewer.loadedRunId != request.expectedRunId || viewer.loadedInstances != source.counters.exported)
                throw new InvalidDataException("The original world did not load every exported instance record.");
            if (request.addReviewCamera)
            {
                var camera = new GameObject("OriginalWorld_ExplicitReviewCamera").AddComponent<Camera>();
                camera.transform.position = request.reviewCameraPosition;
                camera.transform.eulerAngles = request.reviewCameraEuler;
                camera.nearClipPlane = 0.05f; camera.farClipPlane = 2000f;
                camera.tag = "MainCamera";
            }
            Directory.CreateDirectory("Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save original-world scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("Original Blender source world assembled: " + viewer.loadedInstances +
                " records; material/physics/semantic/gameplay acceptance remain separate. " + ScenePath);
            return world;
        }

        /// <summary>Build owner copies the exact bundle after Windows build; no scene/geometry reconstruction.</summary>
        public static string CopyBundleForWindows(string bundleDirectory, string builtExePath, string expectedManifestSha256)
        {
            string source = Path.GetFullPath(bundleDirectory);
            VerifyHash(Path.Combine(source, "manifest.json"), expectedManifestSha256);
            string executable = Path.GetFullPath(builtExePath);
            if (!File.Exists(executable)) throw new FileNotFoundException("Build the Windows player before staging its source bundle.", executable);
            string data = Path.Combine(Path.GetDirectoryName(executable), Path.GetFileNameWithoutExtension(executable) + "_Data");
            if (!Directory.Exists(data)) throw new DirectoryNotFoundException(data);
            string destination = Path.Combine(data, "StreamingAssets/OriginalBlenderWorld/source-bundle");
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(source, file);
                string target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
            VerifyHash(Path.Combine(destination, "manifest.json"), expectedManifestSha256);
            return destination;
        }

        private static void VerifyHash(string path, string expected)
        {
            if (string.IsNullOrWhiteSpace(expected) || expected.Length != 64 || !File.Exists(path))
                throw new InvalidDataException("Missing source file or reviewed SHA256: " + path);
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
                if (!string.Equals(BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", ""), expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Source SHA256 mismatch: " + path);
        }
    }
}
