using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Imports the reviewed, separate art scenes without replacing the playable Prototype.
public static class BuildDocumentWorldCandidates
{
    [Serializable] public sealed class SceneEntry
    {
        public string id = string.Empty;
        public string model = string.Empty;
        public float width;
        public float depth;
    }
    [Serializable] public sealed class Manifest { public SceneEntry[] scenes = Array.Empty<SceneEntry>(); }
    [Serializable] public sealed class SceneResult
    {
        public string id;
        public string scene;
        public int meshCount;
        public int triangles;
        public int colliders;
        public string screenshot;
    }
    [Serializable] public sealed class Report
    {
        public string unity;
        public bool gameplayValidated = false;
        public SceneResult[] scenes;
    }

    private const string Folder = "Assets/Art/DocumentWorld";
    private const string Scenes = "Assets/Scenes/DocumentWorld";
    private static readonly Dictionary<Material, Material> Materials = new Dictionary<Material, Material>();

    public static void Build()
    {
        if (Application.unityVersion != "6000.6.3f1")
            throw new InvalidOperationException("Document world requires Unity 6000.6.3f1.");
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder + "/import-manifest.json"));
        if (manifest?.scenes == null || manifest.scenes.Length == 0)
            throw new InvalidDataException("No reviewed scene exports supplied.");
        Directory.CreateDirectory(Scenes);
        Directory.CreateDirectory(Folder + "/Materials");
        Directory.CreateDirectory(".collab/artifacts/document-world-unity");
        AssetDatabase.Refresh();
        Materials.Clear();
        var results = new List<SceneResult>();
        var ids = new HashSet<string>();
        foreach (var entry in manifest.scenes)
        {
            if (!ids.Add(entry.id) || !IsSceneId(entry.id) || entry.width <= 0 || entry.depth <= 0)
                throw new InvalidDataException("Invalid or duplicate scene identity: " + entry.id);
            if (entry.model != Folder + "/" + entry.id + ".fbx")
                throw new InvalidDataException("Unexpected model path: " + entry.model);
            var importer = AssetImporter.GetAtPath(entry.model) as ModelImporter;
            if (importer == null) throw new InvalidDataException("Missing FBX importer: " + entry.model);
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.model);
            if (prefab == null) throw new InvalidDataException("FBX failed to import: " + entry.model);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            root.name = entry.id + "_DocumentWorld";
            var result = new SceneResult { id = entry.id, scene = Scenes + "/" + entry.id + ".unity" };
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; ++i)
                    materials[i] = ConvertMaterial(materials[i], entry.id);
                renderer.sharedMaterials = materials;
            }
            var bounds = new Bounds();
            bool first = true;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || mesh.vertexCount == 0) throw new InvalidDataException("Empty scene mesh.");
                foreach (var vertex in mesh.vertices)
                    if (!Finite(vertex.x) || !Finite(vertex.y) || !Finite(vertex.z))
                        throw new InvalidDataException("Non-finite scene geometry.");
                result.meshCount++;
                result.triangles += mesh.triangles.Length / 3;
                var renderer = filter.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (first) { bounds = renderer.bounds; first = false; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (!HasAncestorPrefix(filter.transform, "Vista_"))
                {
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
                    result.colliders++;
                }
            }
            if (first || result.triangles == 0) throw new InvalidDataException("Scene has no geometry.");
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name.StartsWith("Night_", StringComparison.Ordinal)) child.gameObject.SetActive(false);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .9f, .74f);
            sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42, -35, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f, .55f, .65f);
            var camera = new GameObject("Art Review Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 58;
            camera.farClipPlane = 1000;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.56f, .65f, .72f);
            var target = bounds.center;
            float distance = Mathf.Max(bounds.size.x / 1.7f, bounds.size.z) * 1.35f + bounds.size.y;
            camera.transform.position = target + new Vector3(.2f, .8f, -1f).normalized * distance;
            camera.transform.LookAt(target);
            Physics.SyncTransforms();
            EditorSceneManager.SaveScene(scene, result.scene);
            result.screenshot = ".collab/artifacts/document-world-unity/" + entry.id + ".png";
            Capture(camera, result.screenshot);
            results.Add(result);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(".collab/artifacts/document-world-unity/verification.json", JsonUtility.ToJson(
            new Report { unity = Application.unityVersion, scenes = results.ToArray() }, true));
        Debug.Log("DOCUMENT_WORLD_IMPORTED " + results.Count + " separate candidate scenes; gameplay not validated.");
    }

    private static bool IsSceneId(string id) => id == "Farm_Main" || id == "Farm_Core" ||
        id == "Hub_Village" || id == "Market_Town" || id == "Forest_Primeval";
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool HasAncestorPrefix(Transform node, string prefix)
    {
        for (; node != null; node = node.parent)
            if (node.name.StartsWith(prefix, StringComparison.Ordinal)) return true;
        return false;
    }
    private static Material ConvertMaterial(Material source, string sceneId)
    {
        if (source == null) throw new InvalidDataException("Missing source material.");
        if (Materials.TryGetValue(source, out var cached)) return cached;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit unavailable.");
        var material = new Material(shader) { name = source.name, enableInstancing = true };
        material.SetColor("_BaseColor", source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color);
        material.SetTexture("_BaseMap", source.mainTexture);
        material.SetFloat("_Smoothness", source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : .25f);
        material.SetFloat("_Metallic", source.HasProperty("_Metallic") ? source.GetFloat("_Metallic") : 0);
        var key = AssetDatabase.GetAssetPath(source) + ":" + source.name;
        var path = Folder + "/Materials/" + sceneId + "_" + Hash128.Compute(key) + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing == null) AssetDatabase.CreateAsset(material, path);
        else { EditorUtility.CopySerialized(material, existing); UnityEngine.Object.DestroyImmediate(material); material = existing; EditorUtility.SetDirty(material); }
        Materials[source] = material;
        return material;
    }
    private static void Capture(Camera camera, string path)
    {
        var previous = RenderTexture.active;
        var target = new RenderTexture(1600, 1000, 24);
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
