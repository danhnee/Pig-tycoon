using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PigTycoon.Candidates.EvaluatedExport;
using PigTycoon.Core;
using PigTycoon.OriginalWorld.EditorTools;
using PigTycoon.PlayableDemo3D;
using PigTycoon.PlayableDemo3D.Verification;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PigTycoon.PlayableDemo3D.EditorTools
{
    /// <summary>
    /// Integrates the actual pinned Blender world (post-nature.blend, 1,050,009 instances, 234 materials)
    /// with persisted walkable/obstacle MeshColliders, calibrated Golden-Hour lighting, non-uncanny characters/pigs,
    /// and the full PlayableDemoBootstrap + PlayableFarmController + PlayableFarmRaidView + AcceptanceRoute loop.
    /// </summary>
    public static class OriginalWorldPlayableIntegrator
    {
        public const string ScenePath = OriginalBlenderWorldAssembler.ScenePath;
        public const string DefaultPlayerExePath = "Builds/OriginalBlenderWorldDemo/PigTycoonOriginalWorld.exe";
        private const string TransferJsonPath = "Assets/OriginalBlenderWorld/post_nature_234_material_transfer.json";
        private const string MaterialsRoot = "Assets/OriginalBlenderWorld/Materials";
        private const string ColliderMeshesRoot = "Assets/OriginalBlenderWorld/ColliderMeshes";
        private const string VerificationOutDir = ".collab/artifacts/original-map-unity-20261026/unity_verification_v1";

        [Serializable]
        private sealed class TransferColor
        {
            public string property;
            public float[] rgba;
        }

        [Serializable]
        private sealed class TransferFloat
        {
            public string property;
            public float value;
        }

        [Serializable]
        private sealed class MaterialTransferEntry
        {
            public int sourceMaterialId;
            public string sourceMaterialName;
            public string unityMaterialAssetPath;
            public string mappingStatus;
            public string shaderName;
            public string surfaceMode;
            public bool alphaClip;
            public float cutoff;
            public bool doubleSided;
            public float[] baseColor;
            public float smoothness;
            public float metallic;
            public float bumpScale;
            public string baseMapAssetPath;
            public string normalMapAssetPath;
            public string roughnessMapAssetPath;
            public string secondaryMapAssetPath;
            public TransferFloat[] extraFloats;
            public TransferColor[] extraColors;
        }

        [Serializable]
        private sealed class MaterialTransferDocument
        {
            public string bundleDirectory;
            public string expectedRunId;
            public string expectedSourceSha256;
            public string expectedManifestSha256;
            public bool allowExplicitUnverifiedMaterialCandidates;
            public bool addReviewCamera;
            public Vector3 reviewCameraPosition;
            public Vector3 reviewCameraEuler;
            public MaterialTransferEntry[] materialBindings;
        }

        [Serializable]
        private sealed class VerificationReceipt
        {
            public string scenePath;
            public string unityVersion;
            public string sourceBlendSha256;
            public string exportRunId;
            public long loadedInstances;
            public int totalBatches;
            public int configuredMaterials;
            public int groundColliders;
            public int obstacleColliders;
            public float playerSpawnGroundY;
            public float manSpawnGroundY;
            public float feedStationGroundY;
            public float waterStationGroundY;
            public float mainHousePorchGroundY;
        }

        public static void PrepareOriginalWorldAssets()
        {
            if (Application.unityVersion != "6000.6.3f1")
                throw new InvalidOperationException("Expected Unity 6000.6.3f1, got " + Application.unityVersion);

            StageLatestArtCandidates();
            ConfigureAllTextureImporters();
            int configuredMaterials = BuildOriginalWorld234Materials();
            BuildPlayableMaterialsAndPrefabs();
            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();
            Debug.Log($"[OriginalWorldPlayableIntegrator] Prepared {configuredMaterials} world materials and all playable character/pig/prop prefabs.");
        }

        public static void BuildAndVerifyOriginalWorld()
        {
            if (Application.unityVersion != "6000.6.3f1")
                throw new InvalidOperationException("Expected Unity 6000.6.3f1, got " + Application.unityVersion);

            PrepareOriginalWorldAssets();
            int configuredMaterials = 234;

            var doc = JsonUtility.FromJson<MaterialTransferDocument>(File.ReadAllText(TransferJsonPath));
            if (doc == null || doc.materialBindings == null || doc.materialBindings.Length != 234)
                throw new InvalidDataException("Invalid post_nature_234_material_transfer.json");

            var request = new OriginalBlenderWorldAssembler.Request
            {
                bundleDirectory = doc.bundleDirectory,
                expectedRunId = doc.expectedRunId,
                expectedSourceSha256 = doc.expectedSourceSha256,
                expectedManifestSha256 = doc.expectedManifestSha256,
                allowExplicitUnverifiedMaterialCandidates = true,
                addReviewCamera = false,
                materialBindings = doc.materialBindings.Select(b => new OriginalBlenderWorldAssembler.MaterialBinding
                {
                    sourceMaterialId = b.sourceMaterialId,
                    sourceMaterialName = b.sourceMaterialName,
                    unityMaterialAssetPath = b.unityMaterialAssetPath,
                    mappingStatus = b.mappingStatus
                }).ToArray()
            };

            GameObject worldRoot = OriginalBlenderWorldAssembler.Build(request);
            var viewer = worldRoot.GetComponent<EvaluatedMapViewer>();
            if (viewer == null || viewer.loadedInstances != 1050009)
                throw new InvalidOperationException("OriginalBlenderWorld did not load 1,050,009 instances.");

            BuildWorldPhysicsColliders(worldRoot, viewer, out Collider mainGroundCollider, out Collider[] navObstacleColliders,
                out Collider wallTestCollider, out int groundColliderCount, out int totalObstacleCount);

            ConfigureLightingAndAtmosphere();

            PopulatePlayableFarmOnOriginalWorld(mainGroundCollider, navObstacleColliders, wallTestCollider,
                out PlayableFarmController farmCtrl, out PlayableDemoBootstrap bootstrap);

            var receipt = new VerificationReceipt
            {
                scenePath = ScenePath,
                unityVersion = Application.unityVersion,
                sourceBlendSha256 = doc.expectedSourceSha256,
                exportRunId = doc.expectedRunId,
                loadedInstances = viewer.loadedInstances,
                totalBatches = viewer.totalBatches,
                configuredMaterials = configuredMaterials,
                groundColliders = groundColliderCount,
                obstacleColliders = totalObstacleCount,
                playerSpawnGroundY = farmCtrl.GroundHeight(0f, -15f),
                manSpawnGroundY = farmCtrl.GroundHeight(-4.8f, -15f),
                feedStationGroundY = farmCtrl.GroundHeight(-2.8f, -11.2f),
                waterStationGroundY = farmCtrl.GroundHeight(2.8f, -11.2f),
                mainHousePorchGroundY = farmCtrl.GroundHeight(-47f, 21f)
            };

            // Release non-serialized runtime mesh/matrix buffers before saving scene to minimize peak RAM
            viewer.ReleaseRuntimeBuffers();
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Failed to save " + ScenePath);
            AssetDatabase.SaveAssets();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();

            Directory.CreateDirectory(VerificationOutDir);
            string receiptPath = Path.Combine(VerificationOutDir, "verification_receipt.json");
            File.WriteAllText(receiptPath, JsonUtility.ToJson(receipt, true));
            Debug.Log($"[OriginalWorldPlayableIntegrator] Scene assembled & saved: {ScenePath} ({receipt.loadedInstances} instances, {receipt.totalBatches} batches, groundY={receipt.playerSpawnGroundY:F3}m)");
        }

        public static void BuildStandalonePlayer()
        {
            if (Application.unityVersion != "6000.6.3f1")
                throw new BuildFailedException("Expected Unity 6000.6.3f1, got " + Application.unityVersion);
            if (!File.Exists(ScenePath))
                throw new BuildFailedException("Scene not found: " + ScenePath);

            var doc = JsonUtility.FromJson<MaterialTransferDocument>(File.ReadAllText(TransferJsonPath));
            if (doc == null)
                throw new BuildFailedException("Missing transfer JSON: " + TransferJsonPath);

            // Switch active editor scene to an empty scene before building player so EvaluatedMapViewer isn't loaded twice in RAM
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();

            string compatShadow = Path.GetFullPath(".agent_scratch/playable-demo/il2cpp-compat-shadow-v2");
            if (Directory.Exists(compatShadow))
            {
                Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", compatShadow);
            }

            string outputExe = Path.GetFullPath(DefaultPlayerExePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputExe));

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputExe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"OriginalBlenderWorld build failed: {report.summary.result} ({report.summary.totalErrors} errors)");

            string stagedBundle = OriginalBlenderWorldAssembler.CopyBundleForWindows(
                doc.bundleDirectory, outputExe, doc.expectedManifestSha256);

            Debug.Log($"[OriginalWorldPlayableIntegrator] Built standalone player: {outputExe} and staged bundle to {stagedBundle}");
        }

        public static void BuildAllOriginalWorldAndStandalonePlayer()
        {
            BuildAndVerifyOriginalWorld();
            BuildStandalonePlayer();
        }

        private static void StageLatestArtCandidates()
        {
            Directory.CreateDirectory("Assets/PlayableDemo/Art/Characters");
            Directory.CreateDirectory("Assets/PlayableDemo/Art/Environment");
            Directory.CreateDirectory("Assets/PlayableDemo/Art/Textures");
            Directory.CreateDirectory("Assets/PlayableDemo/Art/Materials");
            Directory.CreateDirectory("Assets/PlayableDemo/Art/Animators");
            Directory.CreateDirectory("Assets/PlayableDemo/Art/Prefabs");

            // 1. Khoa Urban v3 (Anatomical posture, indigo denim jacket, cream henley, drawn Dao Rua in Attack)
            string khoaDir = ".collab/artifacts/playable-demo-20261026/argon/candidates_v3/khoa_urban";
            string khoaSrc = File.Exists(Path.Combine(khoaDir, "Khoa_Candidate.fbx"))
                ? Path.Combine(khoaDir, "Khoa_Candidate.fbx")
                : Path.Combine(khoaDir, "Khoa_Urban.fbx");
            CopyIfPresent(khoaSrc, "Assets/PlayableDemo/Art/Characters/Khoa_Urban.fbx");
            foreach (string clip in new[] { "Khoa@Idle.fbx", "Khoa@Walk.fbx", "Khoa@Run.fbx", "Khoa@Interact.fbx", "Khoa@Attack.fbx", "Khoa@Whistle.fbx" })
                CopyIfPresent(Path.Combine(khoaDir, clip), "Assets/PlayableDemo/Art/Characters/" + clip);
            CopyDirectoryPngs(Path.Combine(khoaDir, "Textures"), "Assets/PlayableDemo/Art/Textures");

            // 2. Man v3 (Natural posture & open umbrella)
            string manDir = ".collab/artifacts/playable-demo-20261026/argon/candidates_v3/man";
            CopyIfPresent(Path.Combine(manDir, "Man_Candidate.fbx"), "Assets/PlayableDemo/Art/Characters/Man_Candidate.fbx");
            foreach (string clip in new[] { "Man@Idle.fbx", "Man@Walk.fbx", "Man@Interact.fbx" })
                CopyIfPresent(Path.Combine(manDir, clip), "Assets/PlayableDemo/Art/Characters/" + clip);
            CopyIfPresent(Path.Combine(manDir, "Man_Umbrella_Open.fbx"), "Assets/PlayableDemo/Art/Environment/Man_Umbrella_Open.fbx");
            CopyDirectoryPngs(Path.Combine(manDir, "Textures"), "Assets/PlayableDemo/Art/Textures");

            // 3. Pig v3 (Hong Dien & WhiteFoot with semi-lop forward-drooping porcine ears & quadruped gait)
            string pigDir = ".collab/artifacts/playable-demo-20261026/argon/candidates_v3/pig";
            CopyIfPresent(Path.Combine(pigDir, "Pig_HongDien.fbx"), "Assets/PlayableDemo/Art/Characters/Pig_HongDien.fbx");
            CopyIfPresent(Path.Combine(pigDir, "Pig_WhiteFoot.fbx"), "Assets/PlayableDemo/Art/Characters/Pig_WhiteFoot.fbx");
            foreach (string clip in new[] { "Pig@Idle.fbx", "Pig@Walk.fbx", "Pig@Eat.fbx", "Pig@Sleep.fbx" })
                CopyIfPresent(Path.Combine(pigDir, clip), "Assets/PlayableDemo/Art/Characters/" + clip);
            CopyDirectoryPngs(Path.Combine(pigDir, "Textures"), "Assets/PlayableDemo/Art/Textures");

            // 4. Compact Interactive Care Props (Rich weathered wood, slate roof tiles, stone trim)
            string careDir = ".collab/artifacts/original-map-unity-20261026/care_props_v1";
            foreach (string prop in new[]
            {
                "Prop_FeederCrate.fbx", "Prop_WaterBasin.fbx", "Prop_MudWallow.fbx", "Prop_CarePost.fbx",
                "Bldg_OpenShelter.fbx", "Prop_CapacityBoard.fbx", "Prop_FenceSegment4m.fbx",
                "Prop_FenceBreachDamaged.fbx", "Prop_FenceTempRepair.fbx", "Bldg_NoXuyenVanTower.fbx"
            })
            {
                CopyIfPresent(Path.Combine(careDir, prop), "Assets/PlayableDemo/Art/Environment/" + prop);
            }
            CopyDirectoryPngs(Path.Combine(careDir, "Textures"), "Assets/PlayableDemo/Art/Textures");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CopyIfPresent(string src, string dst)
        {
            if (!File.Exists(src))
                throw new FileNotFoundException("Missing required candidate asset: " + src);
            if (File.Exists(dst) && FilesEqual(src, dst)) return;
            File.Copy(src, dst, true);
        }

        private static void CopyDirectoryPngs(string srcDir, string dstDir)
        {
            if (!Directory.Exists(srcDir)) return;
            foreach (string file in Directory.GetFiles(srcDir, "*.png"))
            {
                string dst = Path.Combine(dstDir, Path.GetFileName(file));
                if (File.Exists(dst) && FilesEqual(file, dst)) continue;
                File.Copy(file, dst, true);
            }
        }

        private static bool FilesEqual(string a, string b)
        {
            var fa = new FileInfo(a);
            var fb = new FileInfo(b);
            if (fa.Length != fb.Length) return false;
            byte[] ba = File.ReadAllBytes(a);
            byte[] bb = File.ReadAllBytes(b);
            for (int i = 0; i < ba.Length; i++)
                if (ba[i] != bb[i]) return false;
            return true;
        }

        private static void ConfigureAllTextureImporters()
        {
            HashSet<string> normalFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "00003.png", "00006.png", "00010.png"
            };
            // Leaf textures 00004.png, 00008.png, 00011.png must stay sRGB with alphaIsTransparency=true
            HashSet<string> linearFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string dir in new[] { "Assets/OriginalBlenderWorld/Textures", "Assets/PlayableDemo/Art/Textures" })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (string file in Directory.GetFiles(dir, "*.png"))
                {
                    string assetPath = file.Replace('\\', '/');
                    string fileName = Path.GetFileName(assetPath);
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null) continue;

                    bool isNormal = normalFiles.Contains(fileName) || fileName.EndsWith("_Normal.png", StringComparison.OrdinalIgnoreCase);
                    bool isLinear = isNormal || linearFiles.Contains(fileName) || fileName.EndsWith("_Roughness.png", StringComparison.OrdinalIgnoreCase);

                    bool dirty = false;
                    var desiredType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    if (importer.textureType != desiredType) { importer.textureType = desiredType; dirty = true; }
                    if (importer.sRGBTexture == isLinear) { importer.sRGBTexture = !isLinear; dirty = true; }
                    if (!isNormal && !isLinear && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                    if (importer.maxTextureSize < 2048) { importer.maxTextureSize = 2048; dirty = true; }

                    if (dirty) importer.SaveAndReimport();
                }
            }
        }

        private static int BuildOriginalWorld234Materials()
        {
            Directory.CreateDirectory(MaterialsRoot);
            var doc = JsonUtility.FromJson<MaterialTransferDocument>(File.ReadAllText(TransferJsonPath));
            if (doc?.materialBindings == null)
                throw new InvalidDataException("Could not parse material bindings from " + TransferJsonPath);

            Shader surfaceLit3D = Shader.Find("PigTycoon/OriginalWorld/SurfaceLit3D");
            if (surfaceLit3D == null) surfaceLit3D = Shader.Find("Universal Render Pipeline/Lit");
            if (surfaceLit3D == null)
                throw new InvalidOperationException("Cannot find PigTycoon/OriginalWorld/SurfaceLit3D shader.");

            Shader meadowEarthShader = Shader.Find("PigTycoon/URP/MeadowEarthTerrain");
            Shader elasticGrassShader = Shader.Find("PigTycoon/URP/ElasticMeadowGrass");

            int count = 0;
            foreach (var entry in doc.materialBindings)
            {
                string targetShaderName = entry.shaderName == "Universal Render Pipeline/Lit"
                    ? "PigTycoon/OriginalWorld/SurfaceLit3D"
                    : entry.shaderName;
                if (entry.sourceMaterialId == 93 && meadowEarthShader != null)
                {
                    targetShaderName = "PigTycoon/URP/MeadowEarthTerrain";
                }
                else if ((entry.sourceMaterialId == 225 || entry.sourceMaterialId == 226 || entry.sourceMaterialId == 227) && elasticGrassShader != null)
                {
                    targetShaderName = "PigTycoon/URP/ElasticMeadowGrass";
                }

                Shader shader = Shader.Find(targetShaderName);
                if (shader == null) shader = surfaceLit3D;
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(entry.unityMaterialAssetPath);
                if (mat == null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, entry.unityMaterialAssetPath);
                }
                else if (mat.shader != shader)
                {
                    mat.shader = shader;
                }

                mat.name = Path.GetFileNameWithoutExtension(entry.unityMaterialAssetPath);
                mat.enableInstancing = true;

                Color baseCol = ToColor(entry.baseColor, Color.white);
                SetColorIfPresent(mat, "_BaseColor", baseCol);
                SetColorIfPresent(mat, "_Color", baseCol);
                SetFloatIfPresent(mat, "_Smoothness", entry.smoothness);
                SetFloatIfPresent(mat, "_Metallic", entry.metallic);
                SetFloatIfPresent(mat, "_BumpScale", entry.bumpScale);
                SetFloatIfPresent(mat, "_Cutoff", entry.cutoff);
                SetFloatIfPresent(mat, "_Cull", entry.doubleSided ? 0f : 2f);
                SetFloatIfPresent(mat, "_SubsurfaceWrap", 0.08f);

                if (entry.alphaClip || string.Equals(entry.surfaceMode, "AlphaClip", StringComparison.OrdinalIgnoreCase))
                {
                    SetFloatIfPresent(mat, "_AlphaClip", 1f);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.renderQueue = (int)RenderQueue.AlphaTest;
                }
                else if (string.Equals(entry.surfaceMode, "Transparent", StringComparison.OrdinalIgnoreCase))
                {
                    SetFloatIfPresent(mat, "_Surface", 1f);
                    SetFloatIfPresent(mat, "_Blend", 0f);
                    SetFloatIfPresent(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
                    SetFloatIfPresent(mat, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    SetFloatIfPresent(mat, "_ZWrite", 0f);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                }
                else
                {
                    SetFloatIfPresent(mat, "_Surface", 0f);
                    SetFloatIfPresent(mat, "_AlphaClip", 0f);
                    SetFloatIfPresent(mat, "_SrcBlend", (float)BlendMode.One);
                    SetFloatIfPresent(mat, "_DstBlend", (float)BlendMode.Zero);
                    SetFloatIfPresent(mat, "_ZWrite", 1f);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = -1;
                }

                if (!string.IsNullOrWhiteSpace(entry.baseMapAssetPath))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.baseMapAssetPath);
                    if (tex != null)
                    {
                        SetTextureIfPresent(mat, "_BaseMap", tex);
                        SetTextureIfPresent(mat, "_MainTex", tex);
                    }
                }
                bool hasNorm = false;
                if (!string.IsNullOrWhiteSpace(entry.normalMapAssetPath))
                {
                    var norm = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.normalMapAssetPath);
                    if (norm != null)
                    {
                        SetTextureIfPresent(mat, "_BumpMap", norm);
                        mat.EnableKeyword("_NORMALMAP");
                        hasNorm = true;
                    }
                }
                SetFloatIfPresent(mat, "_HasNormalMap", hasNorm ? 1f : 0f);

                bool hasRough = false;
                if (!string.IsNullOrWhiteSpace(entry.roughnessMapAssetPath))
                {
                    var rough = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.roughnessMapAssetPath);
                    if (rough != null)
                    {
                        SetTextureIfPresent(mat, "_RoughnessMap", rough);
                        hasRough = true;
                    }
                }
                SetFloatIfPresent(mat, "_HasRoughnessMap", hasRough ? 1f : 0f);

                bool hasSec = false;
                if (!string.IsNullOrWhiteSpace(entry.secondaryMapAssetPath) && entry.sourceMaterialId != 0)
                {
                    var sec = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.secondaryMapAssetPath);
                    if (sec != null)
                    {
                        SetTextureIfPresent(mat, "_SecondaryMap", sec);
                        hasSec = true;
                    }
                }
                SetFloatIfPresent(mat, "_HasSecondaryMap", hasSec ? 1f : 0f);

                if (entry.extraFloats != null)
                {
                    foreach (var ef in entry.extraFloats)
                        if (ef != null && !string.IsNullOrWhiteSpace(ef.property))
                            SetFloatIfPresent(mat, ef.property, ef.value);
                }
                if (entry.extraColors != null)
                {
                    foreach (var ec in entry.extraColors)
                        if (ec != null && !string.IsNullOrWhiteSpace(ec.property))
                            SetColorIfPresent(mat, ec.property, ToColor(ec.rgba, Color.white));
                }

                // Restore 183 Golden Oak canopies on Mat 50 (NS7_ExistingBroadleafAtlas) where 00000.png had max alpha 0.145
                if (entry.sourceMaterialId == 50)
                {
                    var mapleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OriginalBlenderWorld/Textures/00011.png");
                    if (mapleTex != null)
                    {
                        SetTextureIfPresent(mat, "_BaseMap", mapleTex);
                        SetTextureIfPresent(mat, "_MainTex", mapleTex);
                    }
                    Color goldenOak = new Color(0.94f, 0.72f, 0.24f, 1f);
                    SetColorIfPresent(mat, "_BaseColor", goldenOak);
                    SetColorIfPresent(mat, "_Color", goldenOak);
                    SetFloatIfPresent(mat, "_Cutoff", 0.22f);
                    SetFloatIfPresent(mat, "_AlphaClip", 1f);
                    SetFloatIfPresent(mat, "_Cull", 0f);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.renderQueue = (int)RenderQueue.AlphaTest;
                }

                // Warm NS7_Stone* ruins (Mats 163, 164, 165) to match Image 5's weathered golden-buff limestone
                if (entry.sourceMaterialId == 163 || entry.sourceMaterialId == 164 || entry.sourceMaterialId == 165)
                {
                    SetColorIfPresent(mat, "_BaseColor", new Color(0.68f, 0.60f, 0.47f, 1f));
                    SetColorIfPresent(mat, "_RampColor0", new Color(0.46f, 0.39f, 0.30f, 1f));
                    SetColorIfPresent(mat, "_RampColor1", new Color(0.65f, 0.57f, 0.45f, 1f));
                    SetColorIfPresent(mat, "_RampColor2", new Color(0.80f, 0.72f, 0.58f, 1f));
                }

                EditorUtility.SetDirty(mat);
                count++;
            }

            AssetDatabase.SaveAssets();
            return count;
        }

        private static void BuildPlayableMaterialsAndPrefabs()
        {
            Shader lit = Shader.Find("PigTycoon/OriginalWorld/SurfaceLit3D");
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");

            // Anti-uncanny calibrated skin & outfit materials: low smoothness (0.14-0.22), warm natural skin tint & subsurface wrap
            Material khoaSkinMat = UpsertLitMaterial("Assets/PlayableDemo/Art/Materials/KhoaSkin_Mat.mat", lit,
                "Assets/PlayableDemo/Art/Textures/KhoaSkin_Albedo.png",
                "Assets/PlayableDemo/Art/Textures/KhoaSkin_Normal.png",
                new Color(1.0f, 0.97f, 0.95f, 1.0f), smoothness: 0.14f, bumpScale: 0.22f, subsurfaceWrap: 0.28f);

            Material khoaMat = UpsertLitMaterial("Assets/PlayableDemo/Art/Materials/Khoa_Mat.mat", lit,
                "Assets/PlayableDemo/Art/Textures/Khoa_Albedo.png",
                "Assets/PlayableDemo/Art/Textures/Khoa_Normal.png",
                Color.white, smoothness: 0.20f, bumpScale: 0.35f, subsurfaceWrap: 0.10f);

            Material manSkinMat = UpsertLitMaterial("Assets/PlayableDemo/Art/Materials/ManSkin_Mat.mat", lit,
                "Assets/PlayableDemo/Art/Textures/ManSkin_Albedo.png",
                "Assets/PlayableDemo/Art/Textures/ManSkin_Normal.png",
                new Color(1.0f, 0.97f, 0.95f, 1.0f), smoothness: 0.14f, bumpScale: 0.22f, subsurfaceWrap: 0.28f);

            Material manMat = UpsertLitMaterial("Assets/PlayableDemo/Art/Materials/Man_Mat.mat", lit,
                "Assets/PlayableDemo/Art/Textures/Man_Albedo.png",
                "Assets/PlayableDemo/Art/Textures/Man_Normal.png",
                Color.white, smoothness: 0.20f, bumpScale: 0.35f, subsurfaceWrap: 0.10f);

            Material pigHongDienMat = UpsertLitMaterial("Assets/PlayableDemo/Art/Materials/Pig_HongDien_Mat.mat", lit,
                "Assets/PlayableDemo/Art/Textures/Pig_HongDien_Albedo.png",
                "Assets/PlayableDemo/Art/Textures/Pig_HongDien_Normal.png",
                Color.white, smoothness: 0.20f, bumpScale: 0.28f, subsurfaceWrap: 0.26f);

            Material pigWhiteFootMat = UpsertLitMaterial("Assets/PlayableDemo/Art/Materials/Pig_WhiteFoot_Mat.mat", lit,
                "Assets/PlayableDemo/Art/Textures/Pig_WhiteFoot_Albedo.png",
                "Assets/PlayableDemo/Art/Textures/Pig_WhiteFoot_Normal.png",
                Color.white, smoothness: 0.20f, bumpScale: 0.28f, subsurfaceWrap: 0.26f);

            UpsertLitMaterial("Assets/PlayableDemo/Art/Materials/FarmProps_Mat.mat", lit,
                "Assets/PlayableDemo/Art/Textures/FarmProps_Albedo.png",
                "Assets/PlayableDemo/Art/Textures/FarmProps_Normal.png",
                Color.white, smoothness: 0.22f, bumpScale: 0.40f, subsurfaceWrap: 0.08f);

            if (Directory.Exists("Assets/PlayableDemo/Art/Materials"))
            {
                foreach (string matFile in Directory.GetFiles("Assets/PlayableDemo/Art/Materials", "*.mat"))
                {
                    string assetPath = matFile.Replace('\\', '/');
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (m != null && m.shader != lit)
                    {
                        m.shader = lit;
                        m.enableInstancing = true;
                        SetFloatIfPresent(m, "_HasNormalMap", m.GetTexture("_BumpMap") != null ? 1f : 0f);
                        SetFloatIfPresent(m, "_SubsurfaceWrap", 0.12f);
                        EditorUtility.SetDirty(m);
                    }
                }
            }

            // Configure Generic Rig & Clips for Khoa, Man, and Pigs
            ConfigureGenericRig("Assets/PlayableDemo/Art/Characters/Khoa_Urban.fbx", false);
            foreach (var clipSpec in new[]
            {
                ("Assets/PlayableDemo/Art/Characters/Khoa@Idle.fbx", true),
                ("Assets/PlayableDemo/Art/Characters/Khoa@Walk.fbx", true),
                ("Assets/PlayableDemo/Art/Characters/Khoa@Run.fbx", true),
                ("Assets/PlayableDemo/Art/Characters/Khoa@Interact.fbx", false),
                ("Assets/PlayableDemo/Art/Characters/Khoa@Attack.fbx", false),
                ("Assets/PlayableDemo/Art/Characters/Khoa@Whistle.fbx", false)
            })
            {
                ConfigureGenericClip(clipSpec.Item1, clipSpec.Item2);
            }

            ConfigureGenericRig("Assets/PlayableDemo/Art/Characters/Man_Candidate.fbx", false);
            foreach (var clipSpec in new[]
            {
                ("Assets/PlayableDemo/Art/Characters/Man@Idle.fbx", true),
                ("Assets/PlayableDemo/Art/Characters/Man@Walk.fbx", true),
                ("Assets/PlayableDemo/Art/Characters/Man@Interact.fbx", false)
            })
            {
                ConfigureGenericClip(clipSpec.Item1, clipSpec.Item2);
            }

            ConfigureGenericRig("Assets/PlayableDemo/Art/Characters/Pig_HongDien.fbx", false);
            ConfigureGenericRig("Assets/PlayableDemo/Art/Characters/Pig_WhiteFoot.fbx", false);
            foreach (var clipSpec in new[]
            {
                ("Assets/PlayableDemo/Art/Characters/Pig@Idle.fbx", true),
                ("Assets/PlayableDemo/Art/Characters/Pig@Walk.fbx", true),
                ("Assets/PlayableDemo/Art/Characters/Pig@Eat.fbx", false),
                ("Assets/PlayableDemo/Art/Characters/Pig@Sleep.fbx", true)
            })
            {
                ConfigureGenericClip(clipSpec.Item1, clipSpec.Item2);
            }

            AnimatorController khoaCtrl = BuildKhoaAnimatorController("Assets/PlayableDemo/Art/Animators/KhoaAnimator.controller");
            AnimatorController manCtrl = BuildManAnimatorController("Assets/PlayableDemo/Art/Animators/ManAnimator.controller");
            AnimatorController pigCtrl = BuildPigAnimatorController("Assets/PlayableDemo/Art/Animators/PigAnimator.controller");

            Material anMat = UpsertLitMaterial("Assets/PlayableDemo/Art/Materials/An_Mat.mat", lit,
                "Assets/PlayableDemo/Art/Textures/Khoa_Albedo.png",
                "Assets/PlayableDemo/Art/Textures/Khoa_Normal.png",
                new Color(0.76f, 0.90f, 1.00f, 1.0f), smoothness: 0.22f, bumpScale: 0.35f, subsurfaceWrap: 0.12f);

            BuildCharacterPrefab(
                "Assets/PlayableDemo/Art/Characters/Khoa_Urban.fbx",
                "Assets/PlayableDemo/Art/Prefabs/KhoaPrefab.prefab",
                khoaCtrl, khoaMat, khoaSkinMat, addCapsuleCollider: false);

            BuildCharacterPrefab(
                "Assets/PlayableDemo/Art/Characters/Khoa_Urban.fbx",
                "Assets/PlayableDemo/Art/Prefabs/AnPrefab.prefab",
                khoaCtrl, anMat, khoaSkinMat, addCapsuleCollider: false);

            BuildCharacterPrefab(
                "Assets/PlayableDemo/Art/Characters/Man_Candidate.fbx",
                "Assets/PlayableDemo/Art/Prefabs/ManPrefab.prefab",
                manCtrl, manMat, manSkinMat, addCapsuleCollider: true,
                umbrellaFbxPath: null);

            BuildPigPrefab(
                "Assets/PlayableDemo/Art/Characters/Pig_HongDien.fbx",
                "Assets/PlayableDemo/Art/Prefabs/PigPrefab.prefab",
                pigCtrl, pigHongDienMat);

            BuildPigPrefab(
                "Assets/PlayableDemo/Art/Characters/Pig_WhiteFoot.fbx",
                "Assets/PlayableDemo/Art/Prefabs/PigWhiteFootPrefab.prefab",
                pigCtrl, pigWhiteFootMat);

            AssetDatabase.SaveAssets();
        }

        private static Material UpsertLitMaterial(string assetPath, Shader shader, string albedoPath, string normalPath,
            Color baseColor, float smoothness, float bumpScale, float subsurfaceWrap = 0.10f)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            mat.enableInstancing = true;
            SetColorIfPresent(mat, "_BaseColor", baseColor);
            SetColorIfPresent(mat, "_Color", baseColor);
            SetFloatIfPresent(mat, "_Smoothness", smoothness);
            SetFloatIfPresent(mat, "_Metallic", 0f);
            SetFloatIfPresent(mat, "_BumpScale", bumpScale);
            SetFloatIfPresent(mat, "_SubsurfaceWrap", subsurfaceWrap);
            SetFloatIfPresent(mat, "_Cull", 0f);

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            if (albedo != null)
            {
                SetTextureIfPresent(mat, "_BaseMap", albedo);
                SetTextureIfPresent(mat, "_MainTex", albedo);
            }
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal != null)
            {
                SetTextureIfPresent(mat, "_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
                SetFloatIfPresent(mat, "_HasNormalMap", 1f);
            }
            else
            {
                SetFloatIfPresent(mat, "_HasNormalMap", 0f);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureGenericRig(string assetPath, bool importAnim)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) return;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = importAnim;
            importer.importCameras = false;
            importer.importLights = false;
            if (assetPath.IndexOf("Pig_", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                importer.importNormals = ModelImporterNormals.Calculate;
                importer.normalSmoothingAngle = 82f;
            }
            importer.SaveAndReimport();
        }

        private static void ConfigureGenericClip(string assetPath, bool loop)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) return;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            var clips = importer.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = loop;
                    clips[i].loopPose = loop;
                }
                importer.clipAnimations = clips;
            }
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadFirstClip(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        }

        private static AnimatorController BuildKhoaAnimatorController(string path)
        {
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("WalkPlaybackRate", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("RunPlaybackRate", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Whistle", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle");
            idle.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Khoa@Idle.fbx");
            sm.defaultState = idle;

            var walk = sm.AddState("Walk");
            walk.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Khoa@Walk.fbx");
            walk.speedParameterActive = true;
            walk.speedParameter = "WalkPlaybackRate";

            var run = sm.AddState("Run");
            run.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Khoa@Run.fbx");
            run.speedParameterActive = true;
            run.speedParameter = "RunPlaybackRate";

            var interact = sm.AddState("Interact");
            interact.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Khoa@Interact.fbx");

            var attack = sm.AddState("Attack");
            attack.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Khoa@Attack.fbx");

            var whistle = sm.AddState("Whistle");
            whistle.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Khoa@Whistle.fbx");

            var toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false; toWalk.duration = 0.12f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var walkToIdle = walk.AddTransition(idle);
            walkToIdle.hasExitTime = false; walkToIdle.duration = 0.12f;
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            var walkToRun = walk.AddTransition(run);
            walkToRun.hasExitTime = false; walkToRun.duration = 0.1f;
            walkToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            var runToWalk = run.AddTransition(walk);
            runToWalk.hasExitTime = false; runToWalk.duration = 0.1f;
            runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");

            var runToIdle = run.AddTransition(idle);
            runToIdle.hasExitTime = false; runToIdle.duration = 0.12f;
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            foreach (var pair in new[] { (interact, "Interact"), (attack, "Attack"), (whistle, "Whistle") })
            {
                var anyTo = sm.AddAnyStateTransition(pair.Item1);
                anyTo.hasExitTime = false; anyTo.duration = 0.08f;
                anyTo.AddCondition(AnimatorConditionMode.If, 0f, pair.Item2);
                var back = pair.Item1.AddTransition(idle);
                back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.1f;
            }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static AnimatorController BuildManAnimatorController(string path)
        {
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Talk", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Interact", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle");
            idle.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Man@Idle.fbx");
            sm.defaultState = idle;

            var talk = sm.AddState("Talk");
            talk.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Man@Interact.fbx");
            var toTalk = sm.AddAnyStateTransition(talk);
            toTalk.hasExitTime = false; toTalk.duration = 0.1f;
            toTalk.AddCondition(AnimatorConditionMode.If, 0f, "Talk");
            var back = talk.AddTransition(idle);
            back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.1f;

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static AnimatorController BuildPigAnimatorController(string path)
        {
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Eat", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Interact", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle");
            idle.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Pig@Idle.fbx");
            sm.defaultState = idle;

            var walk = sm.AddState("Walk");
            walk.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Pig@Walk.fbx");
            var toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false; toWalk.duration = 0.1f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.08f, "Speed");
            var toIdle = walk.AddTransition(idle);
            toIdle.hasExitTime = false; toIdle.duration = 0.1f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");

            var eat = sm.AddState("Eat");
            eat.motion = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Pig@Eat.fbx");
            var anyEat = sm.AddAnyStateTransition(eat);
            anyEat.hasExitTime = false; anyEat.duration = 0.1f;
            anyEat.AddCondition(AnimatorConditionMode.If, 0f, "Eat");
            var eatBack = eat.AddTransition(idle);
            eatBack.hasExitTime = true; eatBack.exitTime = 0.9f; eatBack.duration = 0.1f;

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static void BuildCharacterPrefab(string fbxPath, string prefabPath, AnimatorController controller,
            Material outfitMat, Material skinMat, bool addCapsuleCollider, string umbrellaFbxPath = null)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (modelAsset == null)
                throw new InvalidOperationException("Missing character FBX at " + fbxPath);

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            instance.name = Path.GetFileNameWithoutExtension(prefabPath);

            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                var shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    string matName = shared[i] != null ? shared[i].name : r.name;
                    bool isSkin = matName.IndexOf("Skin", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  r.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0;
                    shared[i] = isSkin ? skinMat : outfitMat;
                }
                r.sharedMaterials = shared;
            }

            if (!string.IsNullOrEmpty(umbrellaFbxPath))
            {
                GameObject umbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(umbrellaFbxPath);
                if (umbAsset != null)
                {
                    GameObject umb = (GameObject)PrefabUtility.InstantiatePrefab(umbAsset, instance.transform);
                    umb.name = "Man_Umbrella_Accessory";
                    umb.transform.localPosition = new Vector3(-0.26f, 1.05f, 0.08f);
                    umb.transform.localEulerAngles = new Vector3(-10f, 0f, 12f);
                    foreach (var r in umb.GetComponentsInChildren<Renderer>(true))
                        r.sharedMaterial = outfitMat;
                }
            }

            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            if (avatar != null) animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var actor = instance.GetComponent<PlayableFarmActor>();
            if (actor == null) actor = instance.AddComponent<PlayableFarmActor>();
            actor.Animator = animator;
            actor.WalkReferenceMetersPerSecond = 2.8f;
            actor.RunReferenceMetersPerSecond = 4.8f;

            if (addCapsuleCollider && instance.GetComponent<Collider>() == null)
            {
                var cap = instance.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.85f, 0f);
                cap.radius = 0.32f;
                cap.height = 1.70f;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void BuildPigPrefab(string fbxPath, string prefabPath, AnimatorController controller, Material pigMat)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (modelAsset == null)
                throw new InvalidOperationException("Missing pig FBX at " + fbxPath);

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            instance.name = Path.GetFileNameWithoutExtension(prefabPath);

            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                var shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++) shared[i] = pigMat;
                r.sharedMaterials = shared;
            }

            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            if (avatar != null) animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var actor = instance.GetComponent<PlayableFarmActor>();
            if (actor == null) actor = instance.AddComponent<PlayableFarmActor>();
            actor.Animator = animator;
            actor.WalkReferenceMetersPerSecond = 1.8f;
            actor.RunReferenceMetersPerSecond = 3.2f;

            if (instance.GetComponent<Collider>() == null)
            {
                var box = instance.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.36f, 0f);
                box.size = new Vector3(0.55f, 0.68f, 1.05f);
                box.isTrigger = true;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void BuildWorldPhysicsColliders(GameObject worldRoot, EvaluatedMapViewer viewer,
            out Collider mainGroundCollider, out Collider[] navObstacleColliders, out Collider wallTestCollider,
            out int groundColliderCount, out int totalObstacleCount)
        {
            Directory.CreateDirectory(ColliderMeshesRoot);

            var physicsRoot = new GameObject("OriginalWorld_PhysicsColliders");
            physicsRoot.transform.SetParent(worldRoot.transform, false);

            var groundRoot = new GameObject("WalkableGroundColliders");
            groundRoot.transform.SetParent(physicsRoot.transform, false);

            var obstacleRoot = new GameObject("ObstacleColliders");
            obstacleRoot.transform.SetParent(physicsRoot.transform, false);

            int[] walkableMeshIds =
            {
                362, // TOPO_Foreground (Main playable farm & pasture plateau)
                4540, 4541, 4542, 4543, // Medical Terrace
                1067, // River Landing Causeway
                4494, // Coupled Bridge Deck
                1080, 1081, 1083, 1084, 1085, 1086, 1087, 1088, 1089, 1090, 1091, 1092 // Harbor Town streets/plaza/stairs
            };

            mainGroundCollider = null;
            groundColliderCount = 0;
            foreach (int meshId in walkableMeshIds)
            {
                // Layer 8 = Walkable Ground so ObstacleMask ~(1 << 8) never hits terrain during actor/tower raycasts
                var col = AttachBundleMeshCollider(viewer, groundRoot.transform, meshId, "Ground_Mesh_" + meshId, layer: 8);
                if (col != null)
                {
                    groundColliderCount++;
                    if (meshId == 362) mainGroundCollider = col;
                }
            }
            if (mainGroundCollider == null)
                throw new InvalidOperationException("Failed to attach TOPO_Foreground (mesh 362) ground MeshCollider.");

            Physics.SyncTransforms();

            (int id, string label)[] obstacleSpecs =
            {
                (13, "Obstacle_Approved_MainHouse_HF2"),
                (28, "Obstacle_Approved_ResearchLaboratory"),
                (35, "Obstacle_Approved_MedicalTent"),
                (1030, "Obstacle_FM21_MeadowPond_Basin"),
                (4495, "Obstacle_Bridge_Balustrade_4495"),
                (4496, "Obstacle_Bridge_Balustrade_4496"),
                (4497, "Obstacle_Bridge_Balustrade_4497"),
                (4498, "Obstacle_Bridge_Balustrade_4498"),
                (4499, "Obstacle_Bridge_Balustrade_4499"),
                (4500, "Obstacle_Bridge_Balustrade_4500")
            };

            totalObstacleCount = 0;
            foreach (var spec in obstacleSpecs)
            {
                var col = AttachBundleMeshCollider(viewer, obstacleRoot.transform, spec.id, spec.label, layer: 6);
                if (col != null) totalObstacleCount++;
            }

            // Central sacred rock & tree shrine grove collider at (1.0, 7.2, 1.0) so actors/pigs path around the grove
            GameObject groveGo = new GameObject("Obstacle_Central_SacredGrove");
            groveGo.layer = 6;
            groveGo.transform.SetParent(obstacleRoot.transform, false);
            groveGo.transform.position = new Vector3(1.0f, 7.2f, 1.0f);
            var groveBox = groveGo.AddComponent<BoxCollider>();
            groveBox.center = new Vector3(0f, 2.0f, 0f);
            groveBox.size = new Vector3(12.0f, 4.5f, 12.0f);
            groveBox.isTrigger = false;
            totalObstacleCount++;

            // South-East Rock & Pine Cluster (X in [4.5..12.5], Z in [-24.2..-17.8]) so actors/raiders route around the rocks
            float seRockY = RaycastGroundY(mainGroundCollider, 8.5f, -21.0f);
            GameObject seRockGo = new GameObject("Obstacle_SouthEast_RockPineCluster");
            seRockGo.layer = 6;
            seRockGo.transform.SetParent(obstacleRoot.transform, false);
            seRockGo.transform.position = new Vector3(8.5f, seRockY, -21.0f);
            var seRockBox = seRockGo.AddComponent<BoxCollider>();
            seRockBox.center = new Vector3(0f, 2.5f, 0f);
            seRockBox.size = new Vector3(8.0f, 6.0f, 6.4f);
            seRockBox.isTrigger = false;
            totalObstacleCount++;

            // Solid pasture stone/timber boundary wall segment near (-4.5, -18.5) in the South-Central Open Meadow
            float wallGroundY = RaycastGroundY(mainGroundCollider, -4.5f, -18.5f);
            GameObject wallGo = new GameObject("Pasture_SolidFenceWall");
            wallGo.transform.SetParent(obstacleRoot.transform, false);
            wallGo.transform.position = new Vector3(-4.5f, wallGroundY, -18.5f);
            wallGo.transform.rotation = Quaternion.identity;
            GameObject fenceAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlayableDemo/Art/Environment/Prop_FenceSegment4m.fbx");
            if (fenceAsset != null)
            {
                var wallVisual = (GameObject)PrefabUtility.InstantiatePrefab(fenceAsset, wallGo.transform);
                wallVisual.name = "Visual";
                wallVisual.transform.localPosition = Vector3.zero;
                wallVisual.transform.localRotation = fenceAsset.transform.localRotation;
            }
            SetLayerRecursively(wallGo, 6);
            Material propsMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableDemo/Art/Materials/FarmProps_Mat.mat");
            if (propsMat != null)
                foreach (var r in wallGo.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = propsMat;

            var wallBox = wallGo.GetComponent<BoxCollider>();
            if (wallBox == null) wallBox = wallGo.AddComponent<BoxCollider>();
            wallBox.center = new Vector3(0f, 0.9f, 0f);
            wallBox.size = new Vector3(3.8f, 1.8f, 0.65f);
            wallBox.isTrigger = false;
            totalObstacleCount++;
            wallTestCollider = wallBox;

            Physics.SyncTransforms();
            navObstacleColliders = new Collider[] { wallBox, groveBox, seRockBox };
        }

        private static MeshCollider AttachBundleMeshCollider(EvaluatedMapViewer viewer, Transform parent, int meshIndex, string name, int layer)
        {
            Mesh loadedMesh = viewer.LoadColliderMesh(meshIndex, out Matrix4x4 matrix);
            if (loadedMesh == null) return null;

            string meshAssetPath = $"{ColliderMeshesRoot}/ColliderMesh_{meshIndex:D4}.asset";
            Mesh persistedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            if (persistedMesh == null)
            {
                loadedMesh.name = $"ColliderMesh_{meshIndex:D4}";
                AssetDatabase.CreateAsset(loadedMesh, meshAssetPath);
                persistedMesh = loadedMesh;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(loadedMesh);
            }

            var go = new GameObject(name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            ApplyMatrixToTransform(go.transform, matrix);
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = persistedMesh;
            mc.convex = false;
            return mc;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static void ApplyMatrixToTransform(Transform tr, Matrix4x4 m)
        {
            tr.localPosition = new Vector3(m.m03, m.m13, m.m23);
            Vector3 col0 = new Vector3(m.m00, m.m10, m.m20);
            Vector3 col1 = new Vector3(m.m01, m.m11, m.m21);
            Vector3 col2 = new Vector3(m.m02, m.m12, m.m22);
            Vector3 scale = new Vector3(col0.magnitude, col1.magnitude, col2.magnitude);
            if (scale.x > 1e-5f && scale.y > 1e-5f && scale.z > 1e-5f)
            {
                Matrix4x4 rotMat = Matrix4x4.identity;
                rotMat.SetColumn(0, col0 / scale.x);
                rotMat.SetColumn(1, col1 / scale.y);
                rotMat.SetColumn(2, col2 / scale.z);
                tr.localRotation = rotMat.rotation;
            }
            tr.localScale = scale;
        }

        private static float RaycastGroundY(Collider groundCollider, float x, float z)
        {
            Ray ray = new Ray(new Vector3(x, 60f, z), Vector3.down);
            if (groundCollider != null && groundCollider.Raycast(ray, out RaycastHit hit, 120f))
                return hit.point.y;
            if (Physics.Raycast(ray, out RaycastHit anyHit, 120f))
                return anyHit.point.y;
            return 7.28f;
        }

        private static void ConfigureLightingAndAtmosphere()
        {
            var sunGo = new GameObject("OriginalWorld_DirectionalSun");
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            // Warm Golden-Hour sunlight at 1.05 intensity so skin/structures never blow out to chalk-white
            light.color = new Color(1.0f, 0.95f, 0.86f, 1.0f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;
            sunGo.transform.rotation = Quaternion.Euler(38f, -42f, 0f);

            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.74f, 0.86f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.56f, 0.52f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.34f, 0.29f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.84f, 0.90f, 1f);
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 420f;
        }

        private static void PopulatePlayableFarmOnOriginalWorld(
            Collider mainGroundCollider,
            Collider[] navObstacleColliders,
            Collider wallTestCollider,
            out PlayableFarmController farmCtrl,
            out PlayableDemoBootstrap bootstrap)
        {
            Material propsMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableDemo/Art/Materials/FarmProps_Mat.mat");
            GameObject khoaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlayableDemo/Art/Prefabs/KhoaPrefab.prefab");
            GameObject anPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlayableDemo/Art/Prefabs/AnPrefab.prefab");
            GameObject manPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlayableDemo/Art/Prefabs/ManPrefab.prefab");
            GameObject pigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlayableDemo/Art/Prefabs/PigPrefab.prefab");
            GameObject thiefPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlayableDemo/Art/Prefabs/ThiefCandidatePrefab.prefab");
            GameObject crawlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlayableDemo/Art/Prefabs/CrawlerCandidatePrefab.prefab");

            var gameplayRoot = new GameObject("OriginalWorld_PlayableGameplay");

            // Seamless Coastal Bay & Horizon Ocean Water Plane (eliminating rectangular cutoff around Harbor Town, matching Image 2)
            Material oceanWaterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/OriginalBlenderWorld/Materials/Mat_140_FM21_Ocean_And_River_Water_Mat.mat");
            if (oceanWaterMat != null)
            {
                var bayWaterGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
                bayWaterGo.name = "CoastalBayOceanWater";
                var bayCol = bayWaterGo.GetComponent<Collider>();
                if (bayCol != null) UnityEngine.Object.DestroyImmediate(bayCol);
                bayWaterGo.transform.SetParent(gameplayRoot.transform, false);
                bayWaterGo.transform.position = new Vector3(240f, -0.04f, 95f);
                bayWaterGo.transform.localScale = new Vector3(55f, 1f, 55f); // 550m x 550m ocean surface
                var bayRenderer = bayWaterGo.GetComponent<Renderer>();
                if (bayRenderer != null)
                {
                    bayRenderer.sharedMaterial = oceanWaterMat;
                    bayRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            // Gameplay Camera
            var camGo = new GameObject("GameplayCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(gameplayRoot.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1500f;
            cam.fieldOfView = 46f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.74f, 0.83f, 0.92f, 1f);
            if (camGo.GetComponent<UniversalAdditionalCameraData>() == null)
                camGo.AddComponent<UniversalAdditionalCameraData>();

            // ControlledPlayer Root in the clear South-Central Open Meadow (0.0, -15.0)
            float playerY = RaycastGroundY(mainGroundCollider, 0f, -15f);
            var playerRoot = new GameObject("ControlledPlayer");
            playerRoot.transform.SetParent(gameplayRoot.transform, false);
            playerRoot.transform.position = new Vector3(0f, playerY, -15f);
            var motor = playerRoot.AddComponent<CharacterController>();
            motor.height = 1.8f;
            motor.radius = 0.35f;
            motor.center = new Vector3(0f, 0.9f, 0f);
            motor.stepOffset = 0.45f;
            motor.slopeLimit = 50f;

            var pigRoot = new GameObject("CorePigViews");
            pigRoot.transform.SetParent(gameplayRoot.transform, false);

            // Stations Container in the 100% open golden meadow (X in [-6..+13], Z in [-19..-9.5])
            var stationsRoot = new GameObject("InteractiveStations");
            stationsRoot.transform.SetParent(gameplayRoot.transform, false);

            // 1. Feed Station (-2.8, -11.2)
            PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Prop_FeederCrate.fbx",
                "Station_Feed_Main", "demo.feed", "Máng ăn chính",
                FarmInteractionKind.Feed, -2.8f, -11.2f, reach: 2.5f);

            // 2. Water Station (2.8, -11.2)
            PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Prop_WaterBasin.fbx",
                "Station_Water_Main", "demo.water", "Máng nước sạch",
                FarmInteractionKind.Water, 2.8f, -11.2f, reach: 2.5f);

            // 3. Shelter & Cooling Station (5.4, -13.2)
            var shelterStation = PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Bldg_OpenShelter.fbx",
                "Station_Shelter_Main", "demo.shelter", "Mái che đàn heo",
                FarmInteractionKind.Capacity, 5.4f, -13.2f, reach: 2.8f);
            shelterStation.ProvidesHerdShelter = true;
            shelterStation.ProvidesCooling = true;

            // Decorative mud wallow near (-5.6, -11.2)
            PlaceDecorativeProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Prop_MudWallow.fbx",
                "Prop_CoolingMudWallow", -5.6f, -11.2f);

            // 4. Care Brush Post (-5.6, -13.2)
            PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Prop_CarePost.fbx",
                "Station_Care_Brush", "care_post_01", "Trụ chải lông & chăm heo",
                FarmInteractionKind.Care, -5.6f, -13.2f, reach: 2.5f);

            // 5. Capacity Board (-4.2, -13.2) — offset west so it never photobombs behind Khoa at (0.0, -15.0)
            PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Prop_CapacityBoard.fbx",
                "Station_Capacity_Board", "capacity_board", "Bảng sức chứa trại",
                FarmInteractionKind.Capacity, -4.2f, -13.2f, reach: 2.5f);

            // 6a. Built Pasture Fence Segment (2.0, -10.2) on the North border of the open meadow yard
            var builtFenceStation = PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Prop_FenceSegment4m.fbx",
                "Station_Fence_North_Built", "fence_north_built_01", "Rào gỗ chắn hướng Bắc",
                FarmInteractionKind.Construction, 2.0f, -10.2f, reach: 2.5f);
            Transform builtFenceVisualTr = builtFenceStation.transform.Find("Visual");
            var builtFenceVisual = builtFenceVisualTr != null ? builtFenceVisualTr.gameObject : builtFenceStation.gameObject;
            var builtFenceBox = builtFenceStation.gameObject.AddComponent<BoxCollider>();
            builtFenceBox.center = new Vector3(0f, 0.6f, 0f);
            builtFenceBox.size = new Vector3(3.6f, 1.2f, 0.45f);
            builtFenceBox.enabled = true;
            builtFenceStation.CompletedFenceVisual = builtFenceVisual;
            builtFenceStation.CompletedFenceCollider = builtFenceBox;
            builtFenceStation.InitiallyBuilt = true;

            // 6b. Temporary Fence Repair Station (12.2, -9.5)
            var fenceStation = PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Prop_FenceBreachDamaged.fbx",
                "Station_Fence_Temp", "temp_fence_east_01", "Điểm vá rào tạm hướng Đông",
                FarmInteractionKind.Construction, 12.2f, -9.5f, reach: 2.6f);
            var repairAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlayableDemo/Art/Environment/Prop_FenceTempRepair.fbx");
            var repairGo = repairAsset != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(repairAsset, fenceStation.transform)
                : new GameObject("CompletedFenceVisual");
            repairGo.name = "CompletedFenceVisual";
            repairGo.transform.localPosition = Vector3.zero;
            repairGo.transform.localRotation = repairAsset != null ? repairAsset.transform.localRotation : Quaternion.identity;
            if (propsMat != null)
                foreach (var r in repairGo.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = propsMat;
            var fenceBox = fenceStation.gameObject.AddComponent<BoxCollider>();
            fenceBox.center = new Vector3(0f, 0.6f, 0f);
            fenceBox.size = new Vector3(3.6f, 1.2f, 0.45f);
            fenceBox.enabled = false;
            repairGo.SetActive(false);
            fenceStation.CompletedFenceVisual = repairGo;
            fenceStation.CompletedFenceCollider = fenceBox;
            fenceStation.InitiallyBuilt = false;

            // 7. Day Market Counter (12.2, -12.0) — offset & scale visual behind counter point so Khoa & pig stand in front
            var marketStation = PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Bldg_EuropeanMarketCounter.fbx",
                "Station_Market_Day", "day_market_counter", "Quầy Chợ Ngày",
                FarmInteractionKind.Market, 12.2f, -12.0f, reach: 3.2f);
            Transform marketVis = marketStation.transform.Find("Visual");
            if (marketVis != null)
            {
                marketVis.localPosition = new Vector3(1.6f, 0f, 1.4f);
                marketVis.localScale = Vector3.one * 0.42f;
            }

            // 8. Save / Load / Sleep Stations at actual Blender Approved_MainHouse_HF2 porch (-47.0, 21.0)
            PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Prop_CapacityBoard.fbx",
                "Station_Save_Book", "farmhouse_save", "Sổ lưu ngày trại (Nhà Chính)",
                FarmInteractionKind.Save, -47.0f, 21.0f, reach: 2.8f);
            PlaceStationProp(stationsRoot.transform, mainGroundCollider, null,
                null, "Station_Load_Book", "farmhouse_load", "Khôi phục ngày trại",
                FarmInteractionKind.Load, -45.5f, 21.0f, reach: 2.8f);
            PlaceStationProp(stationsRoot.transform, mainGroundCollider, null,
                null, "Station_Sleep_Bed", "farmhouse_sleep", "Nghỉ qua đêm (Nhà Chính)",
                FarmInteractionKind.Sleep, -48.5f, 21.0f, reach: 2.8f);

            // 8b. Interactive 3D Building Doorway & Inspection Stations on the actual Blender 3D landmarks
            PlaceBuildingDoorwayStation(stationsRoot.transform, mainGroundCollider,
                "Station_Building_MainHouse", "building.mainhouse", "Đại Sảnh Nhà Chính",
                FarmInteractionKind.MainHouseHub, -44.2f, 19.5f, reach: 4.5f,
                new Vector3(-34.0f, 15.2f, 9.5f), new Vector3(-49.0f, 11.2f, 24.5f),
                "Trung tâm chỉ huy nông trại, nâng cấp cấp độ đại sảnh Lv1→5, mở khóa giới hạn đàn heo.");

            PlaceBuildingDoorwayStation(stationsRoot.transform, mainGroundCollider,
                "Station_Building_Warehouse", "building.warehouse", "Nhà Kho Nông Sản & Vật Tư (36 Ô)",
                FarmInteractionKind.WarehouseTerminal, -33.0f, 18.0f, reach: 4.5f,
                new Vector3(-23.0f, 14.0f, 8.0f), new Vector3(-34.5f, 10.2f, 19.8f),
                "Kho lưu trữ 36 ô vật tư, nâng cấp sức chứa cám (+100kg/cấp) và bảo quản nông sản.");

            PlaceBuildingDoorwayStation(stationsRoot.transform, mainGroundCollider,
                "Station_Building_Workshop", "building.workshop", "Xưởng Cơ Khí & Cường Hóa Vũ Khí",
                FarmInteractionKind.WorkshopTerminal, -36.5f, 21.0f, reach: 4.5f,
                new Vector3(-26.5f, 14.2f, 10.8f), new Vector3(-37.8f, 10.5f, 22.8f),
                "Rèn & cường hóa Dao Rựa, Búa Tạ, Giáo Tre; chế tạo kiện gỗ/đá gia cố phòng tuyến.");

            PlaceBuildingDoorwayStation(stationsRoot.transform, mainGroundCollider,
                "Station_Building_ResearchLab", "building.researchlab", "Phòng Nghiên Cứu Sinh Học & Lai Tạo Gen",
                FarmInteractionKind.ResearchLab, -21.4f, 34.8f, reach: 4.8f,
                new Vector3(-10.5f, 14.5f, 23.5f), new Vector3(-22.2f, 10.8f, 36.8f),
                "Giám định 4 kênh Gen (Ngoại quan, Gia phả, Thuốc thử, Giải trình tự 8h) & Phối giống F1/F2.");

            PlaceBuildingDoorwayStation(stationsRoot.transform, mainGroundCollider,
                "Station_Building_MedicalTent", "building.medicaltent", "Lều Quân Y & Thú Y (5 Động Từ)",
                FarmInteractionKind.MedicalTent, -10.0f, 10.2f, reach: 4.2f,
                new Vector3(-1.5f, 13.0f, 1.8f), new Vector3(-10.9f, 9.8f, 11.2f),
                "Thực hành 5 động từ thú y: Chẩn đoán, Cách ly, Điều trị, Tiêm phòng, Tẩm bổ đàn heo.");

            PlaceBuildingDoorwayStation(stationsRoot.transform, mainGroundCollider,
                "Station_Building_Observatory", "building.observatory", "Đài Thiên Văn & Trại Thông Tin",
                FarmInteractionKind.ObservatoryIntel, 32.8f, 28.0f, reach: 5.5f,
                new Vector3(17.5f, 18.2f, 13.5f), new Vector3(35.5f, 14.5f, 31.5f),
                "Quét Tinh Cầu Thiên Văn mái xanh viền vàng, dự báo sớm đợt quái & biến động thời tiết.");

            PlaceBuildingDoorwayStation(stationsRoot.transform, mainGroundCollider,
                "Station_Building_HarborTown", "building.harbortown", "Thương Hội Thị Trấn Cảng Châu Âu Cổ",
                FarmInteractionKind.HarborTownHub, 145.0f, 96.7f, reach: 6.5f,
                new Vector3(116.0f, 22.0f, 72.0f), new Vector3(152.0f, 12.0f, 102.0f),
                "Trung tâm giao thương hàng hải, nhập lô cám lớn, tên thép, thuốc quý và khế ước mở rộng đất.");

            // 9. Defense Station (No Xuyen Van Tower at 4.5, -15.5) — field ballista scale so Khoa remains clearly visible
            var defenseStation = PlaceStationProp(stationsRoot.transform, mainGroundCollider, propsMat,
                "Assets/PlayableDemo/Art/Environment/Bldg_NoXuyenVanTower.fbx",
                "Station_Defense_NoXuyenVan", "defense_tower_01", "Nỏ Xuyên Vân",
                FarmInteractionKind.Defense, 4.5f, -15.5f, reach: 2.8f);
            Transform defVis = defenseStation.transform.Find("Visual");
            if (defVis != null)
                defVis.localScale = Vector3.one * 0.45f;
            defenseStation.CoreInstanceId = DemoScenarioFactory.OwnedTowerId;
            var projOrigin = new GameObject("ProjectileOrigin").transform;
            projOrigin.SetParent(defenseStation.transform, false);
            projOrigin.localPosition = new Vector3(0f, 1.15f, 0f);
            defenseStation.ProjectileOrigin = projOrigin;

            // 10. Authored NPC Man with open umbrella at (-4.8, -15.0)
            float manY = RaycastGroundY(mainGroundCollider, -4.8f, -15.0f);
            GameObject manGo = (GameObject)PrefabUtility.InstantiatePrefab(manPrefab, gameplayRoot.transform);
            manGo.name = "NPC_Man_GuideCandidate";
            manGo.transform.position = new Vector3(-4.8f, manY, -15.0f);
            manGo.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
            var npcStation = manGo.GetComponent<PlayableFarmStation>();
            if (npcStation == null) npcStation = manGo.AddComponent<PlayableFarmStation>();
            npcStation.StationId = "npc.man";
            npcStation.DisplayName = "Mận";
            npcStation.Kind = FarmInteractionKind.Dialogue;
            npcStation.ReachMeters = 2.6f;
            var npcView = manGo.GetComponent<PlayableFarmNpcView>();
            if (npcView == null) npcView = manGo.AddComponent<PlayableFarmNpcView>();
            npcView.NpcId = "Man";
            npcView.Interaction = npcStation;

            // Controller & Bootstrap Host
            var hostGo = new GameObject("PlayableFarmSystem");
            hostGo.transform.SetParent(gameplayRoot.transform, false);

            farmCtrl = hostGo.AddComponent<PlayableFarmController>();
            farmCtrl.UseExistingCore = false;
            farmCtrl.OwnSimulationClock = true;
            farmCtrl.LoadDemoSaveOnStart = true;
            farmCtrl.FailOnInvalidExistingSave = true;
            farmCtrl.ShowHud = true;
            farmCtrl.UseConfiguredMap = false;
            farmCtrl.PlayerRoot = playerRoot.transform;
            farmCtrl.PlayerMotor = motor;
            farmCtrl.KhoaPrefab = khoaPrefab;
            farmCtrl.AnPrefab = anPrefab;
            farmCtrl.PigPrefab = pigPrefab;
            farmCtrl.PigRoot = pigRoot.transform;
            farmCtrl.GameplayCamera = cam;
            farmCtrl.PlayerSpawn = new Vector3(0f, playerY, -15f);
            farmCtrl.GroundCollider = mainGroundCollider;
            farmCtrl.GroundMask = (1 << 8) | (1 << 0);
            farmCtrl.ObstacleMask = ~(1 << 8);
            farmCtrl.NavigationObstacles = navObstacleColliders;
            farmCtrl.GroundProbeHeight = 60f;
            farmCtrl.PigFootOffset = 0f;
            farmCtrl.CameraDistance = 13.5f;
            farmCtrl.CameraPitch = 38f;
            farmCtrl.CameraYaw = 0f;

            npcView.Farm = farmCtrl;

            var layout = DemoScenarioFactory.DefaultLayout();
            layout.Map.PastureX = -20;
            layout.Map.PastureY = -22;
            layout.Map.PastureWidth = 40;
            layout.Map.PastureHeight = 36;
            layout.Map.RefugeX = 0;
            layout.Map.RefugeY = -13;
            layout.Map.MonsterEntries.Clear();
            layout.Map.MonsterEntries.Add(new FarmApproachPoint { X = 2.0f, Y = -42.5f, Sector = ApproachSector.South });
            layout.Map.MonsterEntries.Add(new FarmApproachPoint { X = -15f, Y = 40f, Sector = ApproachSector.North });
            layout.Map.MonsterEntries.Add(new FarmApproachPoint { X = 15f, Y = 40f, Sector = ApproachSector.North });
            layout.Map.MonsterEntries.Add(new FarmApproachPoint { X = 55f, Y = -10f, Sector = ApproachSector.East });
            layout.Map.MonsterEntries.Add(new FarmApproachPoint { X = 55f, Y = 10f, Sector = ApproachSector.East });
            layout.Map.MonsterEntries.Add(new FarmApproachPoint { X = -55f, Y = -10f, Sector = ApproachSector.West });
            layout.Map.MonsterEntries.Add(new FarmApproachPoint { X = -55f, Y = 10f, Sector = ApproachSector.West });
            layout.PlayerX = 0f;
            layout.PlayerY = -15f;
            layout.ManX = -4.8f;
            layout.ManY = -15f;
            layout.DefenseX = 4.5f;
            layout.DefenseY = -15.5f;
            layout.Map.ServicePoints.Clear();
            layout.Map.ServicePoints.Add(new FarmServicePoint { Id = "demo.feed", X = -2.8f, Y = -11.2f, Feed = true });
            layout.Map.ServicePoints.Add(new FarmServicePoint { Id = "demo.water", X = 2.8f, Y = -11.2f, Water = true });
            layout.Map.ServicePoints.Add(new FarmServicePoint { Id = "demo.shelter", X = 5.4f, Y = -13.2f, Shelter = true, Cooling = true });
            layout.Pigs.Clear();
            layout.Pigs.Add(new DemoPigSeed { Suffix = "01", Name = "Hồng Điền 01", X = -2.0f, Y = -11.8f, AgeDays = 14, WeightKg = 100f, Hunger = 60, GeneLine = GeneLineId.HongDien });
            layout.Pigs.Add(new DemoPigSeed { Suffix = "02", Name = "Hồng Điền 02", X = 2.0f, Y = -11.8f, AgeDays = 14, WeightKg = 100f, Hunger = 60, GeneLine = GeneLineId.HongDien });

            bootstrap = hostGo.AddComponent<PlayableDemoBootstrap>();
            bootstrap.Farm = farmCtrl;
            bootstrap.Scenario = DemoScenarioKind.Care;
            bootstrap.Layout = layout;
            bootstrap.DefenseStation = defenseStation;
            bootstrap.DefenseVisualRoot = defenseStation.gameObject;
            bootstrap.LoadProfileSave = false;

            var raidView = hostGo.AddComponent<PlayableFarmRaidView>();
            raidView.Farm = farmCtrl;
            raidView.ThiefPrefab = thiefPrefab;
            raidView.CrawlerPrefab = crawlerPrefab;
            raidView.ShowCombatHud = true;

            // AcceptanceRoute for deterministic Playable3DAcceptance verification
            var routeGo = new GameObject("AcceptanceRoute");
            routeGo.transform.SetParent(gameplayRoot.transform, false);
            var route = routeGo.AddComponent<AcceptanceRoute>();
            route.WallCollider = wallTestCollider;
            route.WallApproach = MakeWaypoint(routeGo.transform, mainGroundCollider, "WallApproach", -4.5f, -16.8f);
            route.WallBeyond = MakeWaypoint(routeGo.transform, mainGroundCollider, "WallBeyond", -4.5f, -20.2f);
            route.WallRoute = new[]
            {
                MakeWaypoint(routeGo.transform, mainGroundCollider, "WallStep0", -4.5f, -15.8f)
            };
            route.CareRoute = new[]
            {
                MakeWaypoint(routeGo.transform, mainGroundCollider, "Care0_Feed", -2.4f, -11.8f),
                MakeWaypoint(routeGo.transform, mainGroundCollider, "Care1_Water", 2.4f, -11.8f),
                MakeWaypoint(routeGo.transform, mainGroundCollider, "Care2_Man", -4.0f, -14.4f),
                MakeWaypoint(routeGo.transform, mainGroundCollider, "Care3_MidMarket", 5.5f, -12.0f),
                MakeWaypoint(routeGo.transform, mainGroundCollider, "Care4_Market", 12.1f, -12.0f)
            };
            route.FeedStop = 0;
            route.WaterStop = 1;
            route.NpcStop = 2;
            route.MarketStop = 4;
            route.RaidRoute = new[]
            {
                MakeWaypoint(routeGo.transform, mainGroundCollider, "Raid0_Defense", 4.5f, -14.2f)
            };
            route.DefenseStop = 0;

            Quaternion camRot = Quaternion.Euler(farmCtrl.CameraPitch, farmCtrl.CameraYaw, 0f);
            Vector3 target = playerRoot.transform.position + Vector3.up;
            cam.transform.SetPositionAndRotation(target - camRot * Vector3.forward * farmCtrl.CameraDistance, camRot);
        }

        private static PlayableFarmStation PlaceBuildingDoorwayStation(
            Transform parent, Collider ground, string goName, string stationId, string displayName,
            FarmInteractionKind kind, float x, float z, float reach,
            Vector3 inspectionCamPos, Vector3 inspectionLookAt, string summary)
        {
            float y = RaycastGroundY(ground, x, z);
            var stationGo = new GameObject(goName);
            stationGo.transform.SetParent(parent, false);
            stationGo.transform.position = new Vector3(x, y, z);

            var station = stationGo.AddComponent<PlayableFarmStation>();
            station.StationId = stationId;
            station.DisplayName = displayName;
            station.Kind = kind;
            station.ReachMeters = reach;
            station.HasInteriorView = true;
            station.InspectionCameraPosition = inspectionCamPos;
            station.InspectionCameraLookAt = inspectionLookAt;
            station.BuildingSummary = summary;

            // Warm interior doorway light so buildings glow welcomingly at dusk & night
            var lightGo = new GameObject("DoorwayWarmLight");
            lightGo.transform.SetParent(stationGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var ptLight = lightGo.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = new Color(1.0f, 0.82f, 0.46f, 1f);
            ptLight.intensity = 2.4f;
            ptLight.range = 11.5f;
            ptLight.shadows = LightShadows.None;

            return station;
        }

        private static void CaptureComprehensiveVisualShowcase(EvaluatedMapViewer viewer, PlayableFarmController farmCtrl)
        {
            string outDir = ".collab/artifacts/original-map-unity-20261026/unity_verification_v2";
            Directory.CreateDirectory(outDir);

            Camera cam = farmCtrl.GameplayCamera;
            if (cam == null) return;

            Vector3 savedCamPos = cam.transform.position;
            Quaternion savedCamRot = cam.transform.rotation;
            float savedFov = cam.fieldOfView;

            // Instantiate temporary preview actors at canonical spawn points so Editor-time captures show Khoa, Pigs & Elastic Grass
            float playerY = farmCtrl.GroundHeight(0f, -15f);
            GameObject tempKhoa = null;
            if (farmCtrl.KhoaPrefab != null)
            {
                tempKhoa = (GameObject)PrefabUtility.InstantiatePrefab(farmCtrl.KhoaPrefab);
                tempKhoa.transform.position = new Vector3(0f, playerY, -15f);
                tempKhoa.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
                var idleClip = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Khoa@Idle.fbx");
                if (idleClip != null) idleClip.SampleAnimation(tempKhoa, 0.1f);
            }

            float pig1Y = farmCtrl.GroundHeight(-1.8f, -11.8f);
            float pig2Y = farmCtrl.GroundHeight(1.8f, -11.8f);
            GameObject tempPig1 = null, tempPig2 = null;
            GameObject whiteFootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlayableDemo/Art/Prefabs/PigWhiteFootPrefab.prefab");
            var pigIdleClip = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Pig@Idle.fbx");
            if (farmCtrl.PigPrefab != null)
            {
                tempPig1 = (GameObject)PrefabUtility.InstantiatePrefab(farmCtrl.PigPrefab);
                tempPig1.transform.position = new Vector3(-1.8f, pig1Y, -11.8f);
                tempPig1.transform.rotation = Quaternion.Euler(0f, 135f, 0f);
                if (pigIdleClip != null) pigIdleClip.SampleAnimation(tempPig1, 0.2f);
            }
            if (whiteFootPrefab != null || farmCtrl.PigPrefab != null)
            {
                tempPig2 = (GameObject)PrefabUtility.InstantiatePrefab(whiteFootPrefab != null ? whiteFootPrefab : farmCtrl.PigPrefab);
                tempPig2.transform.position = new Vector3(1.8f, pig2Y, -11.8f);
                tempPig2.transform.rotation = Quaternion.Euler(0f, -140f, 0f);
                if (pigIdleClip != null) pigIdleClip.SampleAnimation(tempPig2, 0.5f);
            }

            // Push elastic grass interactor positions so grass tufts visibly bend and cushion around Khoa & the pigs
            Vector4[] posArr = new Vector4[16];
            Vector4[] bendArr = new Vector4[16];
            posArr[0] = new Vector4(0f, playerY, -15f, 1.45f);
            bendArr[0] = new Vector4(0.35f, 0f, 0.45f, 0.85f);
            posArr[1] = new Vector4(-1.8f, pig1Y, -11.8f, 1.15f);
            bendArr[1] = new Vector4(0.28f, 0f, -0.32f, 0.82f);
            posArr[2] = new Vector4(1.8f, pig2Y, -11.8f, 1.15f);
            bendArr[2] = new Vector4(-0.28f, 0f, -0.30f, 0.82f);
            Shader.SetGlobalInt("_GrassInteractorCount", 3);
            Shader.SetGlobalVectorArray("_GrassInteractorPos", posArr);
            Shader.SetGlobalVectorArray("_GrassInteractorBend", bendArr);

            // 1. World Overview matching Image 5 (warm earth + selective golden grass tufts, clear dirt paths, 3 ponds, golden oaks, observatory)
            RenderCameraToPng(cam, new Vector3(-4.5f, 36.5f, -48.0f), Quaternion.Euler(27.5f, 4.5f, 0f), 44f,
                Path.Combine(outDir, "v2_01_world_overview.png"));

            // 2. Pasture Gameplay Overview
            RenderCameraToPng(cam, new Vector3(0.0f, playerY + 9.2f, -26.8f), Quaternion.Euler(32f, 0f, 0f), 45f,
                Path.Combine(outDir, "v2_02_pasture_gameplay.png"));

            // 3. Khoa Close-Up (Three-quarter front view showing indigo denim jacket, cream henley, natural posture & face)
            Vector3 khoaCamPos = new Vector3(0.55f, playerY + 1.32f, -13.05f);
            Vector3 khoaLook = new Vector3(0.0f, playerY + 1.05f, -15.0f);
            RenderCameraToPng(cam, khoaCamPos, Quaternion.LookRotation((khoaLook - khoaCamPos).normalized, Vector3.up), 36f,
                Path.Combine(outDir, "v2_03_khoa_closeup.png"));

            // 4. Man Close-Up (Three-quarter front view showing Mận's natural face, posture & open umbrella)
            float manY = farmCtrl.GroundHeight(-4.8f, -15.0f);
            Vector3 manCamPos = new Vector3(-3.65f, manY + 1.26f, -13.45f);
            Vector3 manLook = new Vector3(-4.8f, manY + 1.00f, -15.0f);
            RenderCameraToPng(cam, manCamPos, Quaternion.LookRotation((manLook - manCamPos).normalized, Vector3.up), 36f,
                Path.Combine(outDir, "v2_04_man_closeup.png"));

            // 5. Pigs & Elastic Grass Close-Up (Showing semi-lop porcine ears, warm pink snout, and elastic grass tufts)
            Vector3 pigCamPos = new Vector3(-0.35f, pig1Y + 0.92f, -9.95f);
            Vector3 pigLook = new Vector3(-1.35f, pig1Y + 0.36f, -11.65f);
            RenderCameraToPng(cam, pigCamPos, Quaternion.LookRotation((pigLook - pigCamPos).normalized, Vector3.up), 38f,
                Path.Combine(outDir, "v2_05_pigs_elastic_grass.png"));

            // 6. Khoa Combat Slash Close-Up (Sample Khoa@Attack.fbx at t=0.25s where Dao Rua blade is drawn in right hand)
            if (tempKhoa != null)
            {
                var atkClip = LoadFirstClip("Assets/PlayableDemo/Art/Characters/Khoa@Attack.fbx");
                if (atkClip != null) atkClip.SampleAnimation(tempKhoa, 0.25f);
            }
            Vector3 atkCamPos = new Vector3(0.85f, playerY + 1.35f, -12.85f);
            Vector3 atkLook = new Vector3(0.05f, playerY + 1.02f, -15.0f);
            RenderCameraToPng(cam, atkCamPos, Quaternion.LookRotation((atkLook - atkCamPos).normalized, Vector3.up), 38f,
                Path.Combine(outDir, "v2_06_khoa_combat_slash.png"));

            // 7. Observatory & Research Lab Landmark Inspection View
            Vector3 obsCamPos = new Vector3(17.5f, 18.2f, 13.5f);
            Vector3 obsLook = new Vector3(35.5f, 14.5f, 31.5f);
            RenderCameraToPng(cam, obsCamPos, Quaternion.LookRotation((obsLook - obsCamPos).normalized, Vector3.up), 42f,
                Path.Combine(outDir, "v2_07_observatory_landmark.png"));

            if (tempKhoa != null) UnityEngine.Object.DestroyImmediate(tempKhoa);
            if (tempPig1 != null) UnityEngine.Object.DestroyImmediate(tempPig1);
            if (tempPig2 != null) UnityEngine.Object.DestroyImmediate(tempPig2);

            cam.transform.SetPositionAndRotation(savedCamPos, savedCamRot);
            cam.fieldOfView = savedFov;
        }

        private static void RenderCameraToPng(Camera cam, Vector3 pos, Quaternion rot, float fov, string outPath)
        {
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = fov;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
        }

        private static PlayableFarmStation PlaceStationProp(Transform parent, Collider ground, Material mat,
            string fbxPath, string goName, string stationId, string displayName,
            FarmInteractionKind kind, float x, float z, float reach)
        {
            float y = RaycastGroundY(ground, x, z);
            var stationGo = new GameObject(goName);
            stationGo.transform.SetParent(parent, false);
            stationGo.transform.position = new Vector3(x, y, z);

            if (!string.IsNullOrEmpty(fbxPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (asset != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(asset, stationGo.transform);
                    visual.name = "Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = asset.transform.localRotation;
                    if (mat != null)
                        foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
                            r.sharedMaterial = mat;
                }
            }

            var station = stationGo.AddComponent<PlayableFarmStation>();
            station.StationId = stationId;
            station.DisplayName = displayName;
            station.Kind = kind;
            station.ReachMeters = reach;
            return station;
        }

        private static void PlaceDecorativeProp(Transform parent, Collider ground, Material mat, string fbxPath, string name, float x, float z)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (asset == null) return;
            float y = RaycastGroundY(ground, x, z);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.name = name;
            go.transform.position = new Vector3(x, y, z);
            if (mat != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = mat;
        }

        private static Transform MakeWaypoint(Transform parent, Collider ground, string name, float x, float z)
        {
            float y = RaycastGroundY(ground, x, z);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, z);
            return go.transform;
        }

        private static Color ToColor(float[] rgba, Color fallback)
        {
            if (rgba == null || rgba.Length < 3) return fallback;
            float a = rgba.Length >= 4 ? rgba[3] : 1f;
            return new Color(rgba[0], rgba[1], rgba[2], a);
        }

        private static void SetColorIfPresent(Material m, string prop, Color c)
        {
            if (m.HasProperty(prop)) m.SetColor(prop, c);
        }

        private static void SetFloatIfPresent(Material m, string prop, float v)
        {
            if (m.HasProperty(prop)) m.SetFloat(prop, v);
        }

        private static void SetTextureIfPresent(Material m, string prop, Texture t)
        {
            if (m.HasProperty(prop)) m.SetTexture(prop, t);
        }
    }
}
