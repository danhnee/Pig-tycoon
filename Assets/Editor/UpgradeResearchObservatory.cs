using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Apply only the reviewed Zone05 asset and verify its real Unity import.</summary>
public static class UpgradeResearchObservatory
{
    private const string Zone = "Zone05_Building_NhaNghienCuu_ResearchObservatory";
    private const string ModelDirectory = "Assets/Models/FarmMain3D/";
    private const string OutputDirectory = ".collab/artifacts/zone05-20261018";
    private const string CandidateDirectory = "ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/";

    [MenuItem("PigTycoon/Apply Reviewed Research Observatory")]
    public static void Run()
    {
        try
        {
            if (Application.unityVersion != "6000.6.3f1")
                throw new InvalidOperationException("AGENTS.md requires Unity 6000.6.3f1.");
            var candidate = JsonUtility.FromJson<Candidate>(File.ReadAllText(CandidateDirectory + "validation.json"));
            using (var sha = SHA256.Create())
            {
                string digest = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(CandidateDirectory + Zone + ".meshbin")))
                    .Replace("-", "").ToLowerInvariant();
                if (candidate == null || digest != candidate.meshbin_sha256)
                    throw new InvalidDataException("Candidate mesh differs from its Blender validation report.");
            }
            string assetPath = ModelDirectory + Zone + "_Mesh.asset";
            string originalGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(originalGuid))
                throw new InvalidOperationException("Existing Zone05 mesh is required for a GUID-preserving update.");

            Mesh mesh = BuildCleanQuad3DMapUnity.LoadMeshFromBin(
                CandidateDirectory + Zone + ".meshbin", Zone + "_Mesh", assetPath);
            if (AssetDatabase.AssetPathToGUID(assetPath) != originalGuid)
                throw new InvalidOperationException("Zone05 mesh GUID changed.");
            if (mesh.vertexCount == 0 || mesh.triangles.Length == 0 || mesh.normals.Length != mesh.vertexCount)
                throw new InvalidOperationException("Zone05 geometry or explicit normals are missing.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/FarmMain3D/Mat_True3D_Atlas.mat");
            if (material == null)
                throw new InvalidOperationException("The existing reviewed observatory atlas material is missing.");
            if (!material.shader.isSupported)
                throw new InvalidOperationException("Observatory shader is unsupported on this device.");

            foreach (string scenePath in new[] { "Assets/Scenes/Farm_Main_3D_Map.unity", "Assets/Scenes/DongCoThaoNguyen.unity" })
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject zone = GameObject.Find(Zone);
                if (zone == null || zone.GetComponent<MeshRenderer>() == null || zone.GetComponent<MeshFilter>() == null)
                    throw new InvalidOperationException("Missing Zone05 renderer in " + scenePath);
                zone.GetComponent<MeshFilter>().sharedMesh = mesh;
                zone.GetComponent<MeshRenderer>().sharedMaterial = material;
                var collider = zone.GetComponent<MeshCollider>();
                if (collider != null) collider.sharedMesh = mesh;
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException("Could not save " + scenePath);
            }
            AssetDatabase.SaveAssets();
            File.Copy(CandidateDirectory + Zone + ".meshbin", ModelDirectory + Zone + ".meshbin", true);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject proof = new GameObject("Observatory_Import_Proof");
            proof.AddComponent<MeshFilter>().sharedMesh = mesh;
            proof.AddComponent<MeshRenderer>().sharedMaterial = material;
            var proofCollider = proof.AddComponent<MeshCollider>();
            proofCollider.sharedMesh = mesh;
            Physics.SyncTransforms();
            if (!proofCollider.Raycast(new Ray(new Vector3(-7.8f, 15f, 6.83f), Vector3.down), out RaycastHit hit, 20f))
                throw new InvalidOperationException("Imported mesh collider did not answer the roof raycast.");

            Camera camera = new GameObject("ProofCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.21f, 0.23f, 0.27f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            Vector3 origin = new Vector3(-7.8f, 3.3232934f, 6.4f);
            camera.transform.position = origin + new Vector3(6f, 6f, -9f);
            camera.transform.LookAt(origin + Vector3.up * 1.65f);
            Light sun = new GameObject("ProofSun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.46f, 0.50f);
            Directory.CreateDirectory(OutputDirectory);
            Capture(camera, OutputDirectory + "/08-unity-front.png", 1000, 1000);
            camera.transform.position = origin + new Vector3(6f, 6f, 9f);
            camera.transform.LookAt(origin + Vector3.up * 1.65f);
            Capture(camera, OutputDirectory + "/09-unity-rear.png", 1000, 1000);

            EditorSceneManager.OpenScene("Assets/Scenes/Farm_Main_3D_Map.unity", OpenSceneMode.Single);
            Camera mapCamera = Camera.main;
            if (mapCamera == null) throw new InvalidOperationException("Map camera is missing.");
            Capture(mapCamera, OutputDirectory + "/10-unity-world.png", 1280, 960);
            string report = JsonUtility.ToJson(new Verification
            {
                unityVersion = Application.unityVersion, meshGuid = originalGuid,
                vertices = mesh.vertexCount, triangles = mesh.triangles.Length / 3,
                roofRayHitY = hit.point.y, shader = material.shader.name, bothScenesSaved = true
            }, true);
            File.WriteAllText(OutputDirectory + "/unity-validation.json", report);
            Debug.Log("OBSERVATORY_UNITY_VERIFIED " + report);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    [Serializable]
    private sealed class Candidate
    {
        public string meshbin_sha256 = string.Empty;
    }

    [Serializable]
    private sealed class Verification
    {
        public string unityVersion;
        public string meshGuid;
        public int vertices;
        public int triangles;
        public float roofRayHitY;
        public string shader;
        public bool bothScenesSaved;
    }

    private static void Capture(Camera camera, string path, int width, int height)
    {
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        Texture2D image = null;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
