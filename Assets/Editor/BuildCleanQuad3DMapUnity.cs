using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildCleanQuad3DMapUnity
{
    private static readonly string[] ModularZoneNames = new string[]
    {
        "Zone01_Terrain_GoldenMeadow_And_Paths",
        "Zone02_Diorama_Cutaway_RockRim_And_Base",
        "Zone03_Water_River_Waterfalls_And_3Ponds",
        "Zone04_Building_KhuChuongTrai_Farmstead",
        "Zone05_Building_NhaNghienCuu_ResearchObservatory",
        "Zone06_Building_NhaCuongHoa_And_KhuNPC",
        "Zone07_Landmark_TramThongTin_InfoBeacon",
        "Zone08_Vista_StoneBridge_And_HarborTown",
        "Zone09_Vista_TwilightCastle_And_WaterfallCliff",
        "Zone10_Vista_AlpineMountains_And_ForestRidge",
        "Zone11_Props_AncientRuins_And_GoldenFlora"
    };

    [MenuItem("PigTycoon/Build Verified True 3D Orthogonal Map")]
    public static void BuildAndRenderAll()
    {
        string texDir = "Assets/Textures/FarmMain3D";
        string modelDir = "Assets/Models/FarmMain3D";
        string matDir = "Assets/Materials/FarmMain3D";

        if (!Directory.Exists(matDir)) Directory.CreateDirectory(matDir);

        AssetDatabase.Refresh();

        string[] texFiles = new string[]
        {
            texDir + "/FarmMain_3D_Albedo_4K.png",
            texDir + "/FarmMain_3D_Normal_4K.png",
            texDir + "/FarmMain_3D_Emission_4K.png",
            texDir + "/FarmMain_Sunset_Sky_Backdrop.png",
            texDir + "/FarmMain_Sunset_Skybox_360_4K.png"
        };

        foreach (string tf in texFiles)
        {
            TextureImporter ti = AssetImporter.GetAtPath(tf) as TextureImporter;
            if (ti != null)
            {
                bool isRepeat = tf.Contains("Skybox_360");
                bool isNormal = tf.Contains("Normal_4K");
                TextureWrapMode expectedWrap = isRepeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                TextureImporterType expectedType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                bool expectedSrgb = !isNormal;

                if (ti.maxTextureSize != 4096 ||
                    ti.textureCompression != TextureImporterCompression.Uncompressed ||
                    ti.wrapMode != expectedWrap ||
                    ti.textureType != expectedType ||
                    ti.sRGBTexture != expectedSrgb)
                {
                    ti.maxTextureSize = 4096;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.filterMode = FilterMode.Trilinear;
                    ti.anisoLevel = 16;
                    ti.wrapMode = expectedWrap;
                    ti.textureType = expectedType;
                    ti.sRGBTexture = expectedSrgb;
                    ti.SaveAndReimport();
                }
            }
        }

        Texture2D texMaster = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/FarmMain_3D_Albedo_4K.png");
        Texture2D texNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/FarmMain_3D_Normal_4K.png");
        Texture2D texEmission = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/FarmMain_3D_Emission_4K.png");
        Texture2D texSky = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/FarmMain_Sunset_Sky_Backdrop.png");
        Texture2D texSkybox360 = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/FarmMain_Sunset_Skybox_360_4K.png");
        if (texSkybox360 == null) texSkybox360 = texSky;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        Shader litShader = urpLit != null ? urpLit : (urpUnlit != null ? urpUnlit : Shader.Find("Standard"));
        Shader skyShader = urpUnlit != null ? urpUnlit : litShader;

        Material matM1 = CreateOrUpdateMaterial(
            matDir + "/Mat_FarmMain_GLB_360_Mode1_4K.mat",
            litShader, texMaster, texNormal, texEmission, 0.16f, 0.55f, 1.85f);
        Material matM2 = CreateOrUpdateMaterial(
            matDir + "/Mat_FarmMain_GLB_360_Mode2_4K.mat",
            litShader, texMaster, texNormal, texEmission, 0.18f, 0.55f, 1.95f);

        Material matTerrain = CreateOrUpdateMaterial(
            matDir + "/Mat_True3D_Terrain.mat",
            litShader, texMaster, texNormal, texEmission, 0.14f, 0.55f, 1.85f);
        Material matAtlas = CreateOrUpdateMaterial(
            matDir + "/Mat_True3D_Atlas.mat",
            litShader, texMaster, texNormal, texEmission, 0.18f, 0.55f, 1.95f);
        Material matWater = CreateOrUpdateMaterial(
            matDir + "/Mat_True3D_Water.mat",
            litShader, texMaster, texNormal, texEmission, 0.82f, 0.35f, 1.85f);
        Material matSky = CreateOrUpdateMaterial(
            matDir + "/Mat_True3D_Sky.mat",
            skyShader, texSky, null, texSky, 0.0f, 0.0f, 1.0f);
        Material matSkyDome360 = CreateOrUpdateMaterial(
            matDir + "/Mat_True3D_SkyDome360.mat",
            skyShader, texSkybox360, null, texSkybox360, 0.0f, 0.0f, 1.0f);

        Shader panoShader = Shader.Find("Skybox/Panoramic");
        Material matSkybox360 = null;
        if (panoShader != null && texSkybox360 != null)
        {
            string skyboxMatPath = matDir + "/Mat_FarmMain_Sunset_Skybox_360.mat";
            matSkybox360 = AssetDatabase.LoadAssetAtPath<Material>(skyboxMatPath);
            if (matSkybox360 == null)
            {
                matSkybox360 = new Material(panoShader);
                AssetDatabase.CreateAsset(matSkybox360, skyboxMatPath);
            }
            else
            {
                matSkybox360.shader = panoShader;
            }
            if (matSkybox360.HasProperty("_MainTex")) matSkybox360.SetTexture("_MainTex", texSkybox360);
            if (matSkybox360.HasProperty("_Exposure")) matSkybox360.SetFloat("_Exposure", 1.02f);
            if (matSkybox360.HasProperty("_Mapping")) matSkybox360.SetFloat("_Mapping", 1f);
            if (matSkybox360.HasProperty("_Rotation")) matSkybox360.SetFloat("_Rotation", 90f);
            EditorUtility.SetDirty(matSkybox360);
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("Farm_Main_3D_World_Root");

        GameObject modRoot = new GameObject("01_Modular_3D_Zones_Editable");
        modRoot.transform.SetParent(root.transform, false);

        int totalTris = 0;
        foreach (string zName in ModularZoneNames)
        {
            string binFile = Path.Combine(Application.dataPath, "Models/FarmMain3D/" + zName + ".meshbin");
            if (File.Exists(binFile))
            {
                Mesh zMesh = LoadMeshFromBin(binFile, zName + "_Mesh", modelDir + "/" + zName + "_Mesh.asset", forceRebuild: true);
                totalTris += zMesh.triangles.Length / 3;
                GameObject zGo = new GameObject(zName);
                zGo.transform.SetParent(modRoot.transform, false);
                var zMf = zGo.AddComponent<MeshFilter>();
                zMf.sharedMesh = zMesh;
                var zMr = zGo.AddComponent<MeshRenderer>();
                if (zName.StartsWith("Zone01"))
                    zMr.sharedMaterial = matTerrain;
                else if (zName.StartsWith("Zone03"))
                    zMr.sharedMaterial = matWater;
                else
                    zMr.sharedMaterial = matAtlas;
                zMr.shadowCastingMode = ShadowCastingMode.On;
                zMr.receiveShadows = true;
                var zMc = zGo.AddComponent<MeshCollider>();
                zMc.sharedMesh = zMesh;
            }
        }

        // Mode 2 Sunset Sky Backdrop (`Mode2_Sunset_Atmosphere.meshbin`)
        string skyBin = Path.Combine(Application.dataPath, "Models/FarmMain3D/Mode2_Sunset_Atmosphere.meshbin");
        GameObject goSky = null;
        if (File.Exists(skyBin))
        {
            Mesh skyMesh = LoadMeshFromBin(skyBin, "Mode2_Sunset_Atmosphere_Mesh", modelDir + "/Mode2_Sunset_Atmosphere_Mesh.asset", forceRebuild: true);
            goSky = new GameObject("Mode2_Sunset_Atmosphere");
            goSky.transform.SetParent(root.transform, false);
            goSky.AddComponent<MeshFilter>().sharedMesh = skyMesh;
            var mrSky = goSky.AddComponent<MeshRenderer>();
            mrSky.sharedMaterial = matSky;
            mrSky.shadowCastingMode = ShadowCastingMode.Off;
            mrSky.receiveShadows = false;
            goSky.SetActive(true);
        }

        // Master Isometric Camera matching Blender 5.3.0 `(0.0, -68.5, 42.5)` -> `(0.0, -0.5, 3.1)` (`lens = 42.5mm` on `36x27mm` 4:3 sensor -> vertical FOV = 35.24°)
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        Vector3 camPos = new Vector3(0.0f, 42.5f, -68.5f);
        Vector3 camTarget = new Vector3(0.0f, 3.1f, -0.5f);
        float camFov = 35.24f;

        cam.transform.position = camPos;
        cam.transform.LookAt(camTarget, Vector3.up);
        cam.fieldOfView = camFov;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 750.0f;
        if (matSkybox360 != null)
        {
            RenderSettings.skybox = matSkybox360;
            cam.clearFlags = CameraClearFlags.Skybox;
        }
        else
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
        cam.backgroundColor = new Color(0.95f, 0.68f, 0.42f);

        // Golden Hour Directional Sun Light + Warm Fill Light matching Blender 5.3.0 PBR setup
        GameObject sunGo = new GameObject("Sun_GoldenHour_DirectionalLight");
        sunGo.transform.SetParent(root.transform, false);
        Light sunLight = sunGo.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.90f, 0.72f);
        sunLight.intensity = 1.50f;
        sunLight.shadows = LightShadows.Soft;
        sunGo.transform.rotation = Quaternion.Euler(44f, -32f, 0f);

        GameObject fillGo = new GameObject("Fill_WarmSky_DirectionalLight");
        fillGo.transform.SetParent(root.transform, false);
        Light fillLight = fillGo.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.color = new Color(0.94f, 0.95f, 1.0f);
        fillLight.intensity = 0.72f;
        fillGo.transform.rotation = Quaternion.Euler(48f, 145f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.76f, 0.70f, 0.62f);

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        string scenePath1 = "Assets/Scenes/Farm_Main_3D_Map.unity";
        string scenePath2 = "Assets/Scenes/DongCoThaoNguyen.unity";
        EditorSceneManager.SaveScene(scene, scenePath2);
        EditorSceneManager.SaveScene(scene, scenePath1);

        AssetDatabase.SaveAssets();
        Debug.Log($"[BuildCleanQuad3DMapUnity] Built True 3D Orthogonal Map in Unity 6 ({totalTris} total tris across 11 zones, 4K Albedo + Normal + Selective Emission active)!");
    }

    private static Material CreateOrUpdateMaterial(
        string assetPath,
        Shader shader,
        Texture2D baseTex,
        Texture2D normalTex,
        Texture2D emissionTex,
        float smoothness,
        float normalScale,
        float emissionStrength)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        else
        {
            mat.shader = shader;
        }
        if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_BumpMap"))
        {
            if (normalTex != null && normalScale > 0.001f)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normalTex);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", normalScale);
            }
            else
            {
                mat.DisableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", null);
            }
        }
        if (mat.HasProperty("_DetailMap")) mat.SetTexture("_DetailMap", null);
        if (mat.HasProperty("_DetailStrength")) mat.SetFloat("_DetailStrength", 0f);
        if (mat.HasProperty("_EmissionMap") && emissionTex != null)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", emissionTex);
            mat.SetColor("_EmissionColor", Color.white * emissionStrength);
        }
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Mesh LoadMeshFromBin(string binPath, string meshName, string assetPath, bool forceRebuild = true)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (!forceRebuild && existing != null && existing.vertexCount > 50)
        {
            return existing;
        }
        byte[] bytes = File.ReadAllBytes(binPath);
        int nv = BitConverter.ToInt32(bytes, 0);
        int nIndices = BitConverter.ToInt32(bytes, 4);
        int offset = 8;

        bool hasExplicitNormals = (bytes.Length >= 8 + nv * 32 + nIndices * 4);

        Vector3[] verts = new Vector3[nv];
        for (int i = 0; i < nv; i++)
        {
            float x = BitConverter.ToSingle(bytes, offset);
            float y = BitConverter.ToSingle(bytes, offset + 4);
            float z = BitConverter.ToSingle(bytes, offset + 8);
            verts[i] = new Vector3(x, y, z);
            offset += 12;
        }

        Vector3[] norms = null;
        if (hasExplicitNormals)
        {
            norms = new Vector3[nv];
            for (int i = 0; i < nv; i++)
            {
                float nx = BitConverter.ToSingle(bytes, offset);
                float ny = BitConverter.ToSingle(bytes, offset + 4);
                float nz = BitConverter.ToSingle(bytes, offset + 8);
                norms[i] = new Vector3(nx, ny, nz);
                offset += 12;
            }
        }

        Vector2[] uvs = new Vector2[nv];
        for (int i = 0; i < nv; i++)
        {
            float u = BitConverter.ToSingle(bytes, offset);
            float v = BitConverter.ToSingle(bytes, offset + 4);
            uvs[i] = new Vector2(u, v);
            offset += 8;
        }

        int[] tris = new int[nIndices];
        Buffer.BlockCopy(bytes, offset, tris, 0, nIndices * 4);

        Mesh m = new Mesh();
        m.name = meshName;
        m.indexFormat = IndexFormat.UInt32;
        m.vertices = verts;
        m.uv = uvs;
        m.triangles = tris;
        if (norms != null)
            m.normals = norms;
        else
            m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();

        if (existing != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }
        AssetDatabase.CreateAsset(m, assetPath);
        return m;
    }
}
