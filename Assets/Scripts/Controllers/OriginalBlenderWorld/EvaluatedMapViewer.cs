using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

namespace PigTycoon.Candidates.EvaluatedExport
{
    [ExecuteAlways]
    [DefaultExecutionOrder(500)]
    public sealed class EvaluatedMapViewer : MonoBehaviour
    {
        [Serializable] public sealed class MeshRecord { public string file; public string sha256; public int[] submesh_slots; }
        [Serializable] public sealed class Binding { public int[] material_ids; }
        [Serializable] public sealed class MaterialRecord { public string name; public string status; }
        [Serializable] public sealed class Manifest
        {
            public string schema, run_id, source_sha256, instances_file, instances_sha256;
            public MeshRecord[] meshes;
            public Binding[] bindings;
            public MaterialRecord[] materials;
        }

        private sealed class Batch
        {
            public Mesh mesh;
            public Material[] materials;
            public List<Matrix4x4> builderMatrices = new List<Matrix4x4>(4);
            public Matrix4x4[] matrices;
            public int count;
            public Bounds worldBounds;
            public bool hasBounds;
            public bool isMicroScatter;
            public ShadowCastingMode shadowMode = ShadowCastingMode.On;

            public void Encapsulate(in Matrix4x4 m, in Bounds localBounds)
            {
                Vector3 center = m.MultiplyPoint3x4(localBounds.center);
                Vector3 ext = localBounds.extents;
                Vector3 axisX = new Vector3(Mathf.Abs(m.m00), Mathf.Abs(m.m10), Mathf.Abs(m.m20)) * ext.x;
                Vector3 axisY = new Vector3(Mathf.Abs(m.m01), Mathf.Abs(m.m11), Mathf.Abs(m.m21)) * ext.y;
                Vector3 axisZ = new Vector3(Mathf.Abs(m.m02), Mathf.Abs(m.m12), Mathf.Abs(m.m22)) * ext.z;
                Bounds transformed = new Bounds(center, (axisX + axisY + axisZ) * 2f);
                if (!hasBounds)
                {
                    worldBounds = transformed;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(transformed);
                }
            }
        }

        // Exact stepping-stone path spline segments from Mesh 1039 for path-clearing & earth-grass balance (Image 5)
        private static readonly Vector2[][] DirtPathPolylines =
        {
            new[]
            {
                new Vector2(-56.0f, 27.8f), new Vector2(-49.3f, 26.2f), new Vector2(-40.2f, 25.2f),
                new Vector2(-24.3f, 22.4f), new Vector2(-16.3f, 23.6f), new Vector2(-9.3f, 19.9f),
                new Vector2(-2.9f, 20.3f), new Vector2(4.1f, 20.0f), new Vector2(11.8f, 20.8f),
                new Vector2(20.2f, 21.1f), new Vector2(28.7f, 26.5f), new Vector2(39.2f, 26.6f),
                new Vector2(49.0f, 31.0f), new Vector2(58.1f, 35.7f), new Vector2(68.3f, 37.8f)
            },
            new[]
            {
                new Vector2(-9.3f, 19.9f), new Vector2(-8.8f, 36.1f), new Vector2(-9.1f, 42.1f),
                new Vector2(-18.6f, 39.5f)
            },
            new[]
            {
                new Vector2(-9.3f, 19.9f), new Vector2(-10.7f, 11.9f), new Vector2(-7.3f, 4.7f),
                new Vector2(-8.7f, -2.8f), new Vector2(-4.2f, -9.1f), new Vector2(-5.2f, -16.1f),
                new Vector2(-1.7f, -24.0f), new Vector2(-3.3f, -32.3f)
            },
            new[]
            {
                new Vector2(20.2f, 21.1f), new Vector2(20.5f, 17.7f), new Vector2(13.9f, 4.9f),
                new Vector2(7.4f, -6.4f), new Vector2(3.2f, -10.8f), new Vector2(-1.9f, -12.9f),
                new Vector2(-5.2f, -16.1f)
            }
        };

        [Tooltip("Absolute path to a NEW bundle exported by export_evaluated.py")]
        public string bundleDirectory;
        [Tooltip("Windows delivery places the same source-bound bundle below StreamingAssets.")]
        public string playerBundleRelativePath = "OriginalBlenderWorld/source-bundle";
        public bool useStreamingAssetsInPlayer = true;
        [Tooltip("Release duplicated CPU mesh buffers after upload; source PMB files remain authoritative for queries/colliders.")]
        public bool releaseCpuMeshDataAfterLoad = true;
        [Tooltip("Bind this viewer to the intended native export run; stale runs fail closed.")]
        public string expectedRunId;
        public string expectedSourceSha256;
        [Tooltip("Assign reviewed Unity materials in manifest.materials order. Every missing slot blocks loading.")]
        public Material[] reviewedMaterials;

        [Header("Culling & Shadow Optimization")]
        public bool enableFrustumCulling = true;
        public float microScatterCullDistance = 280f;
        public float shadowCasterMaxRadiusFromFarm = 145f;

        [Header("Elastic Grass Physics (Hooke Spring-Damper)")]
        public float springFrequency = 11.5f;
        public float dampingRatio = 0.34f;
        public float playerRadius = 1.45f;
        public float pigRadius = 1.05f;
        public float bendResponse = 1.15f;

        public long loadedInstances { get; private set; }
        public string loadedRunId { get; private set; }
        public int totalBatches => batches.Count;
        public int lastRenderedBatches { get; private set; }

        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly List<Batch> batches = new List<Batch>();
        private readonly Plane[] frustumPlanes = new Plane[6];
        private int lastSubmittedFrame = -1;

        // Hooke Spring-Damper Interactor State for Elastic Grass Shader
        private const int MaxGrassInteractors = 16;
        private static readonly int PropInteractorCount = Shader.PropertyToID("_GrassInteractorCount");
        private static readonly int PropInteractorPos = Shader.PropertyToID("_GrassInteractorPos");
        private static readonly int PropInteractorBend = Shader.PropertyToID("_GrassInteractorBend");

        private readonly Vector4[] interactorPos = new Vector4[MaxGrassInteractors];
        private readonly Vector4[] interactorBend = new Vector4[MaxGrassInteractors];
        private readonly Vector3[] prevActorPos = new Vector3[MaxGrassInteractors];
        private readonly Vector3[] springDisp = new Vector3[MaxGrassInteractors];
        private readonly Vector3[] springVel = new Vector3[MaxGrassInteractors];
        private readonly bool[] hasPrevActor = new bool[MaxGrassInteractors];
        private readonly List<Transform> trackedTransforms = new List<Transform>(MaxGrassInteractors);
        private readonly List<float> trackedRadii = new List<float>(MaxGrassInteractors);
        private float nextActorScanTime;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            if ((Application.isPlaying || !Application.isBatchMode) && !string.IsNullOrWhiteSpace(bundleDirectory))
                try { Reload(); }
                catch (Exception error) { Clear(); Debug.LogException(error, this); }
        }

        public string ResolveBundleRoot()
        {
            return Application.isEditor || !useStreamingAssetsInPlayer
                ? Path.GetFullPath(bundleDirectory)
                : SafePath(Application.streamingAssetsPath, playerBundleRelativePath);
        }

        /// <summary>Loads an individual prototype mesh with CPU readability retained for static MeshCollider cooking.</summary>
        public Mesh LoadColliderMesh(int meshIndex, out Matrix4x4 firstMatrix)
        {
            firstMatrix = Matrix4x4.identity;
            string root = ResolveBundleRoot();
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root, "manifest.json")));
            if (manifest?.meshes == null || meshIndex < 0 || meshIndex >= manifest.meshes.Length)
                throw new ArgumentOutOfRangeException(nameof(meshIndex));
            string meshPath = SafePath(root, manifest.meshes[meshIndex].file);
            VerifyHash(meshPath, manifest.meshes[meshIndex].sha256);
            Mesh mesh = ReadMesh(meshPath, out _, computeTangentsIfUv: false);

            string instancePath = SafePath(root, manifest.instances_file);
            using (var stream = File.OpenRead(instancePath))
            using (var reader = new BinaryReader(stream))
            {
                RequireMagic(reader, "PMI3");
                ulong count = reader.ReadUInt64();
                for (ulong i = 0; i < count; i++)
                {
                    uint m = reader.ReadUInt32();
                    reader.ReadUInt32();
                    if (m == (uint)meshIndex)
                    {
                        var mat = new Matrix4x4();
                        for (int r = 0; r < 4; r++)
                            for (int c = 0; c < 4; c++)
                                mat[r, c] = Finite(reader.ReadSingle());
                        firstMatrix = mat;
                        break;
                    }
                    else
                    {
                        stream.Seek(64, SeekOrigin.Current);
                    }
                }
            }
            return mesh;
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            float t = lenSq > 1e-5f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }

        private static float MinDirtPathDistance(Vector2 xz)
        {
            float minD = 999f;
            for (int p = 0; p < DirtPathPolylines.Length; p++)
            {
                var line = DirtPathPolylines[p];
                for (int i = 0; i < line.Length - 1; i++)
                {
                    float d = DistToSegment(xz, line[i], line[i + 1]);
                    if (d < minD) minD = d;
                }
            }
            return minD;
        }

        private static bool ShouldKeepGrassOrFlowerInstance(uint meshId, ulong instanceIndex, ref Matrix4x4 m)
        {
            // Grass clumps: 4526..4529; Wildflowers: 4531..4538
            bool isGrass = meshId >= 4526 && meshId <= 4529;
            bool isFlower = meshId >= 4531 && meshId <= 4538;
            if (!isGrass && !isFlower) return true;

            Vector2 xz = new Vector2(m.m03, m.m23);

            // 1. Clear winding dirt footpaths cleanly (1.35m core trail matching Image 3)
            float pathDist = MinDirtPathDistance(xz);
            if (pathDist < 1.35f) return false;

            // 2. Clear the 3 water pond interiors so water surfaces reflect cleanly (Image 3)
            if (Vector2.Distance(xz, new Vector2(-22.0f, -15.0f)) < 6.8f) return false;
            if (Vector2.Distance(xz, new Vector2(-14.5f, -10.5f)) < 4.9f) return false;
            if (Vector2.Distance(xz, new Vector2(-27.5f, -19.2f)) < 4.5f) return false;
            if (Vector2.Distance(xz, new Vector2(1.0f, 1.0f)) < 4.2f) return false; // Sacred grove inner rock plinth

            // 3. Organic macro-tuft distribution across the meadow & pasture (Image 3 lush golden sward)
            float macro = Mathf.PerlinNoise(xz.x * 0.075f + 17.3f, xz.y * 0.075f + 41.7f) * 0.65f
                        + Mathf.PerlinNoise(xz.x * 0.19f - 9.4f, xz.y * 0.19f + 13.2f) * 0.35f;

            if (pathDist < 2.4f)
                macro -= (1.0f - (pathDist - 1.35f) / 1.05f) * 0.22f;

            if (macro < 0.22f) return false;

            // 4. Deterministic hash to keep a rich, plush golden grass & wildflower sward without GPU overload
            uint h = (uint)(instanceIndex * 747796405UL + 2891336453UL);
            h = ((h >> (int)((h >> 28) + 4)) ^ h) * 277803737U;
            float rand01 = ((h >> 8) & 0xFFFFFF) / (float)0x1000000;

            bool inActivePasture = xz.x >= -10.0f && xz.x <= 15.0f && xz.y >= -21.0f && xz.y <= -7.5f;
            float keepProb = isGrass
                ? (inActivePasture ? 0.34f : Mathf.Lerp(0.28f, 0.50f, Mathf.Clamp01((macro - 0.22f) * 1.8f)))
                : (inActivePasture ? 0.36f : 0.45f);

            if (rand01 > keepProb) return false;

            if (isGrass)
            {
                // Widen & soften surviving grass tufts so they form plush golden-olive meadow cushions (Image 3)
                float widthBoost = Mathf.Lerp(1.28f, 1.65f, Mathf.Clamp01((macro - 0.22f) * 1.8f));
                float heightBoost = Mathf.Lerp(0.95f, 1.26f, Mathf.Clamp01((macro - 0.22f) * 1.8f));
                m.m00 *= widthBoost; m.m10 *= widthBoost; m.m20 *= widthBoost;
                m.m01 *= heightBoost; m.m11 *= heightBoost; m.m21 *= heightBoost;
                m.m02 *= widthBoost; m.m12 *= widthBoost; m.m22 *= widthBoost;
            }
            return true;
        }

        [ContextMenu("Reload verified binary bundle")]
        public void Reload()
        {
            Clear();
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && !SystemInfo.supportsInstancing)
                throw new NotSupportedException("GPU instancing unavailable");
            string root = ResolveBundleRoot();
            if (File.Exists(Path.Combine(root, "FAILED.json"))) throw new InvalidDataException("Failed export bundle");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root, "manifest.json")));
            if (manifest == null || manifest.schema != "pigtycoon-evaluated-map-v1" || string.IsNullOrEmpty(manifest.run_id))
                throw new InvalidDataException("Unsupported/unbound export manifest");
            if (string.IsNullOrEmpty(expectedRunId) || expectedRunId != manifest.run_id || string.IsNullOrEmpty(expectedSourceSha256) ||
                !string.Equals(expectedSourceSha256, manifest.source_sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Export run/source does not match the requested candidate");
            if (manifest.meshes == null || manifest.bindings == null || manifest.materials == null)
                throw new InvalidDataException("Incomplete manifest arrays");
            if (reviewedMaterials == null || reviewedMaterials.Length != manifest.materials.Length)
                throw new InvalidDataException("Explicit reviewed material mapping required; Blender shaders remain NOT_VERIFIED");
            foreach (var material in reviewedMaterials)
                if (material == null || !material.enableInstancing)
                    throw new InvalidDataException("Missing Unity material or GPU instancing disabled");
            try
            {
                bool isNullGfx = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
                var meshes = new Mesh[manifest.meshes.Length];
                var localBounds = new Bounds[meshes.Length];
                var isMicroMesh = new bool[meshes.Length];
                var slots = new int[meshes.Length][];
                for (int i = 0; i < meshes.Length; i++)
                {
                    string path = SafePath(root, manifest.meshes[i].file);
                    VerifyHash(path, manifest.meshes[i].sha256);
                    meshes[i] = ReadMesh(path, out slots[i], computeTangentsIfUv: !isNullGfx, headerOnly: isNullGfx);
                    if (meshes[i] != null)
                    {
                        localBounds[i] = meshes[i].bounds;
                        isMicroMesh[i] = (i >= 4526 && i <= 4538) || localBounds[i].extents.magnitude < 0.55f;
                        ownedMeshes.Add(meshes[i]);
                        if (releaseCpuMeshDataAfterLoad) meshes[i].UploadMeshData(true);
                    }
                    else
                    {
                        localBounds[i] = new Bounds(Vector3.zero, Vector3.one);
                        isMicroMesh[i] = i >= 4526 && i <= 4538;
                    }
                    if (!isNullGfx && (i & 255) == 255) GC.Collect();
                    if (manifest.meshes[i].submesh_slots == null || slots[i].Length != manifest.meshes[i].submesh_slots.Length)
                        throw new InvalidDataException("Submesh manifest mismatch");
                    for (int s = 0; s < slots[i].Length; s++)
                        if (slots[i][s] != manifest.meshes[i].submesh_slots[s]) throw new InvalidDataException("Material slot mismatch");
                }
                string instancePath = SafePath(root, manifest.instances_file);
                VerifyHash(instancePath, manifest.instances_sha256);
                var current = new Dictionary<ulong, Batch>();
                using (var stream = File.OpenRead(instancePath))
                using (var reader = new BinaryReader(stream))
                {
                    RequireMagic(reader, "PMI3");
                    ulong count = reader.ReadUInt64();
                    if (count > 10000000 || (ulong)stream.Length != 12UL + count * 72UL) throw new InvalidDataException("Instance count/size mismatch");
                    for (ulong i = 0; i < count; i++)
                    {
                        uint mesh = reader.ReadUInt32(), binding = reader.ReadUInt32();
                        if (mesh >= meshes.Length || binding >= manifest.bindings.Length) throw new InvalidDataException("Invalid instance reference");
                        var matrix = new Matrix4x4();
                        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) matrix[r, c] = Finite(reader.ReadSingle());
                        if (Mathf.Abs(matrix[3, 0]) + Mathf.Abs(matrix[3, 1]) + Mathf.Abs(matrix[3, 2]) + Mathf.Abs(matrix[3, 3] - 1) > 1e-5f || matrix.determinant <= 0)
                            throw new InvalidDataException("Non-affine/singular/unsplit mirrored matrix");

                        // Always count every verified source instance record (1,050,009)
                        loadedInstances++;

                        // Hide Mesh 1071 (3.3km beige slab), 1069/1070 (rectangular ocean box border lines), and 4493 (flat brown box under Harbor Bay)
                        if (mesh == 1071 || mesh == 1069 || mesh == 1070 || mesh == 4493) continue;

                        // Filter over-dense yellow grass carpet into Image-5-authentic soft golden tufts with exposed earth & clear paths
                        if (!ShouldKeepGrassOrFlowerInstance(mesh, i, ref matrix))
                            continue;

                        bool micro = isMicroMesh[mesh];
                        ulong cellHash = 0UL;
                        if (micro)
                        {
                            int cx = Mathf.FloorToInt(matrix.m03 / 20f) + 512;
                            int cz = Mathf.FloorToInt(matrix.m23 / 20f) + 512;
                            cellHash = ((ulong)(uint)(cx & 0x3FF) << 10) | (uint)(cz & 0x3FF);
                        }
                        ulong key = ((ulong)mesh << 36) | ((ulong)binding << 20) | cellHash;
                        if (!current.TryGetValue(key, out var batch) || batch.count == 1023)
                        {
                            var ids = manifest.bindings[binding].material_ids;
                            var mats = new Material[slots[mesh].Length];
                            for (int s = 0; s < mats.Length; s++)
                            {
                                int slot = slots[mesh][s];
                                if (ids == null || slot < 0 || slot >= ids.Length || ids[slot] < 0 || ids[slot] >= reviewedMaterials.Length)
                                    throw new InvalidDataException("Unbound source material slot");
                                mats[s] = reviewedMaterials[ids[slot]];
                            }
                            batch = new Batch
                            {
                                mesh = meshes[mesh],
                                materials = mats,
                                isMicroScatter = micro
                            };
                            batches.Add(batch);
                            current[key] = batch;
                        }
                        batch.builderMatrices.Add(matrix);
                        batch.count++;
                        batch.Encapsulate(in matrix, in localBounds[mesh]);
                    }
                }
                float maxShadowRadiusSqr = shadowCasterMaxRadiusFromFarm * shadowCasterMaxRadiusFromFarm;
                foreach (var batch in batches)
                {
                    batch.matrices = batch.builderMatrices.ToArray();
                    batch.builderMatrices = null;
                    if (batch.isMicroScatter)
                    {
                        batch.shadowMode = ShadowCastingMode.Off;
                    }
                    else
                    {
                        Vector2 flatCenter = new Vector2(batch.worldBounds.center.x, batch.worldBounds.center.z);
                        batch.shadowMode = flatCenter.sqrMagnitude > maxShadowRadiusSqr && batch.worldBounds.size.magnitude < 45f
                            ? ShadowCastingMode.Off
                            : ShadowCastingMode.On;
                    }
                }
                loadedRunId = manifest.run_id;
                Debug.Log($"Loaded {loadedInstances} instances across {batches.Count} spatial batches from {loadedRunId}", this);
            }
            catch { Clear(); throw; }
        }

        private void UpdateElasticGrassInteractors()
        {
            float dt = Application.isPlaying ? Mathf.Clamp(Time.deltaTime, 0.001f, 0.05f) : 0.016f;
            if (!Application.isPlaying || Time.unscaledTime >= nextActorScanTime || trackedTransforms.Count == 0)
            {
                nextActorScanTime = Time.unscaledTime + 0.45f;
                trackedTransforms.Clear();
                trackedRadii.Clear();

                var controllers = FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
                for (int i = 0; i < controllers.Length && trackedTransforms.Count < MaxGrassInteractors; i++)
                {
                    var cc = controllers[i];
                    if (cc == null || !cc.gameObject.activeInHierarchy) continue;
                    trackedTransforms.Add(cc.transform);
                    trackedRadii.Add(playerRadius);
                }
                var animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
                for (int i = 0; i < animators.Length && trackedTransforms.Count < MaxGrassInteractors; i++)
                {
                    var anim = animators[i];
                    if (anim == null || !anim.gameObject.activeInHierarchy) continue;
                    Transform rootTr = anim.transform.parent != null && anim.transform.parent.name.IndexOf("Pig", StringComparison.OrdinalIgnoreCase) >= 0
                        ? anim.transform.parent
                        : anim.transform;
                    if (trackedTransforms.Contains(rootTr)) continue;
                    bool isPig = rootTr.name.IndexOf("Pig", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 anim.name.IndexOf("Pig", StringComparison.OrdinalIgnoreCase) >= 0;
                    trackedTransforms.Add(rootTr);
                    trackedRadii.Add(isPig ? pigRadius : playerRadius);
                }
            }

            int count = Mathf.Min(trackedTransforms.Count, MaxGrassInteractors);
            float omega = springFrequency;
            float zeta = dampingRatio;

            for (int i = 0; i < count; i++)
            {
                Transform tr = trackedTransforms[i];
                if (tr == null)
                {
                    hasPrevActor[i] = false;
                    interactorPos[i] = Vector4.zero;
                    interactorBend[i] = Vector4.zero;
                    continue;
                }

                Vector3 pos = tr.position;
                Vector3 rawVel = Vector3.zero;
                if (hasPrevActor[i] && (pos - prevActorPos[i]).sqrMagnitude < 25f)
                {
                    rawVel = (pos - prevActorPos[i]) / dt;
                }
                prevActorPos[i] = pos;
                hasPrevActor[i] = true;

                Vector3 horizVel = new Vector3(rawVel.x, 0f, rawVel.z);
                float speed = Mathf.Clamp(horizVel.magnitude, 0f, 7.5f);
                Vector3 targetBend = speed > 0.05f ? horizVel.normalized * Mathf.Min(1.0f, speed * 0.24f) : Vector3.zero;

                // Second-order Hooke spring-damper ODE: x'' = omega^2 * (target - x) - 2 * zeta * omega * x'
                Vector3 accel = (omega * omega) * (targetBend - springDisp[i]) - (2f * zeta * omega) * springVel[i];
                springVel[i] += accel * dt;
                springDisp[i] += springVel[i] * dt;

                interactorPos[i] = new Vector4(pos.x, pos.y, pos.z, trackedRadii[i]);
                Vector3 b = springDisp[i] * bendResponse;
                interactorBend[i] = new Vector4(b.x, b.y, b.z, Mathf.Clamp01(0.58f + speed * 0.14f));
            }

            for (int i = count; i < MaxGrassInteractors; i++)
            {
                hasPrevActor[i] = false;
                interactorPos[i] = Vector4.zero;
                interactorBend[i] = Vector4.zero;
            }

            Shader.SetGlobalInt(PropInteractorCount, count);
            Shader.SetGlobalVectorArray(PropInteractorPos, interactorPos);
            Shader.SetGlobalVectorArray(PropInteractorBend, interactorBend);
        }

        private void Update()
        {
            UpdateElasticGrassInteractors();
        }

        private void LateUpdate()
        {
            if (Application.isPlaying)
            {
                lastSubmittedFrame = Time.frameCount;
                SubmitBatches(Camera.main, null);
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam == null || cam.name == "__VerificationRenderCamera") return;
            if (!Application.isPlaying)
            {
                UpdateElasticGrassInteractors();
            }
            if (!Application.isPlaying || lastSubmittedFrame != Time.frameCount)
            {
                SubmitBatches(cam, cam);
            }
        }

        public void SubmitBatches(Camera cullingCamera, Camera targetCamera)
        {
            if (batches.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(bundleDirectory) && loadedInstances == 0)
                {
                    try { Reload(); }
                    catch (Exception error) { Debug.LogException(error, this); return; }
                }
                if (batches.Count == 0) return;
            }

            bool hasFrustum = enableFrustumCulling && cullingCamera != null;
            Vector3 camPos = Vector3.zero;
            float microDistSqr = microScatterCullDistance * microScatterCullDistance;
            if (hasFrustum)
            {
                GeometryUtility.CalculateFrustumPlanes(cullingCamera, frustumPlanes);
                camPos = cullingCamera.transform.position;
            }

            int rendered = 0;
            int layer = gameObject.layer;
            foreach (var batch in batches)
            {
                if (batch.mesh == null) continue;
                if (hasFrustum && batch.hasBounds)
                {
                    if (batch.isMicroScatter && batch.worldBounds.SqrDistance(camPos) > microDistSqr)
                        continue;
                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, batch.worldBounds))
                        continue;
                }
                rendered++;
                for (int s = 0; s < batch.materials.Length; s++)
                {
                    Graphics.DrawMeshInstanced(batch.mesh, s, batch.materials[s], batch.matrices, batch.count, null,
                        batch.shadowMode, true, layer, targetCamera, LightProbeUsage.Off);
                }
            }
            lastRenderedBatches = rendered;
        }

        private static Mesh ReadMesh(string path, out int[] slots, bool computeTangentsIfUv, bool headerOnly = false)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                RequireMagic(reader, "PMB3");
                int nv = CheckedCount(reader.ReadUInt32(), 8000000), ni = CheckedCount(reader.ReadUInt32(), 24000000), ns = CheckedCount(reader.ReadUInt32(), 100000);
                if (ni % 3 != 0 || ns > ni / 3 || stream.Length != 16L + 12L * ns + 32L * nv + 4L * ni) throw new InvalidDataException("Mesh count/size mismatch");
                slots = new int[ns]; var starts = new int[ns]; var counts = new int[ns]; long cursor = 0;
                for (int i = 0; i < ns; i++)
                {
                    slots[i] = CheckedCount(reader.ReadUInt32(), int.MaxValue);
                    starts[i] = CheckedCount(reader.ReadUInt32(), ni); counts[i] = CheckedCount(reader.ReadUInt32(), ni);
                    if (starts[i] != cursor || counts[i] % 3 != 0) throw new InvalidDataException("Invalid submesh ranges");
                    cursor += counts[i];
                }
                if (cursor != ni) throw new InvalidDataException("Incomplete submesh ranges");
                if (headerOnly) return null;
                var positions = new Vector3[nv]; var normals = new Vector3[nv]; var uv = new Vector2[nv];
                for (int i = 0; i < nv; i++) positions[i] = ReadVector(reader);
                for (int i = 0; i < nv; i++) normals[i] = ReadVector(reader);
                bool hasNonZeroUv = false;
                for (int i = 0; i < nv; i++)
                {
                    float u = Finite(reader.ReadSingle()), v = Finite(reader.ReadSingle());
                    uv[i] = new Vector2(u, v);
                    if (!hasNonZeroUv && (Mathf.Abs(u) > 1e-6f || Mathf.Abs(v) > 1e-6f)) hasNonZeroUv = true;
                }
                var indices = new int[ni];
                for (int i = 0; i < ni; i++)
                {
                    indices[i] = CheckedCount(reader.ReadUInt32(), nv - 1);
                }
                var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path), indexFormat = nv > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                try
                {
                    mesh.vertices = positions; mesh.normals = normals; mesh.uv = uv; mesh.subMeshCount = ns;
                    for (int i = 0; i < ns; i++) mesh.SetTriangles(indices, starts[i], counts[i], i, false, 0);
                    mesh.RecalculateBounds();
                    if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && computeTangentsIfUv && hasNonZeroUv && nv <= 24000)
                        mesh.RecalculateTangents();
                    return mesh;
                }
                catch { DestroyImmediate(mesh); throw; }
            }
        }
        private static int CheckedCount(uint value, int maximum)
        { if (maximum < 0 || value > maximum) throw new InvalidDataException("Unsafe binary count/index"); return (int)value; }
        private static Vector3 ReadVector(BinaryReader reader)
        { return new Vector3(Finite(reader.ReadSingle()), Finite(reader.ReadSingle()), Finite(reader.ReadSingle())); }
        private static float Finite(float value)
        { if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Nonfinite binary data"); return value; }
        private static void RequireMagic(BinaryReader reader, string expected)
        { if (new string(reader.ReadChars(4)) != expected) throw new InvalidDataException("Wrong binary version"); }
        private static string SafePath(string root, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("Invalid relative path");
            string normalizedRoot = Path.GetFullPath(root);
            string path = Path.GetFullPath(Path.Combine(normalizedRoot, relative));
            string prefix = normalizedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Path escapes bundle");
            return path;
        }
        private static void VerifyHash(string path, string expected)
        {
            using (var algorithm = SHA256.Create()) using (var stream = File.OpenRead(path))
                if (!string.Equals(BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", ""), expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA256 mismatch: " + path);
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Clear();
        }
        public void ReleaseRuntimeBuffers() => Clear();
        private void Clear()
        {
            batches.Clear(); loadedInstances = 0; loadedRunId = null; lastRenderedBatches = 0;
            foreach (var mesh in ownedMeshes) if (mesh != null) DestroyImmediate(mesh);
            ownedMeshes.Clear();
        }
    }
}
