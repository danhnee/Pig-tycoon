using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;
using PigTycoon.Presentation;

namespace PigTycoon.EditorTools
{
    [InitializeOnLoad]
    public static class Universal2DSetup
    {
        private const string SettingsFolderPath = "Assets/Settings";
        private const string ScenesFolderPath = "Assets/Scenes";
        private const string RendererAssetPath = "Assets/Settings/Universal2D_Renderer.asset";
        private const string PipelineAssetPath = "Assets/Settings/Universal2D_PipelineAsset.asset";
        private const string MainScenePath = "Assets/Scenes/MainFarm2D.unity";

        static Universal2DSetup()
        {
            EditorApplication.delayCall += CheckAndAutoSetup;
        }

        private static void CheckAndAutoSetup()
        {
            // Kiểm tra xem đã kích hoạt URP 2D chưa
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                Debug.Log("[PigTycoon] Chưa phát hiện cấu hình Universal 2D. Đang tự động cấu hình...");
                SetupUniversal2D();
                CreateAndOpen2DScene();
            }
        }

        [MenuItem("PigTycoon/1. Setup Universal 2D (Chuyển sang URP 2D)")]
        public static void SetupUniversal2D()
        {
            EnsureFolderExists(SettingsFolderPath);

            // 1. Tạo hoặc lấy Renderer2DData
            Renderer2DData rendererData = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererAssetPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
                Debug.Log($"[PigTycoon] Đã tạo 2D Renderer Asset tại: {RendererAssetPath}");
            }

            // 2. Tạo hoặc lấy UniversalRenderPipelineAsset
            UniversalRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
                Debug.Log($"[PigTycoon] Đã tạo URP 2D Pipeline Asset tại: {PipelineAssetPath}");
            }

            // 3. Gán Pipeline vào GraphicsSettings và QualitySettings
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;

            // 4. Thiết lập Y-sorting cho Top-down 2D
            GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            GraphicsSettings.transparencySortAxis = new Vector3(0, 1, 0);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[PigTycoon] ĐÃ CẤU HÌNH THÀNH CÔNG UNIVERSAL 2D (URP 2D) VÀO DỰ ÁN!</color>");
        }

        [MenuItem("PigTycoon/2. Tạo & Mở Scene Nông Trại 2D Mẫu")]
        public static void CreateAndOpen2DScene()
        {
            SetupUniversal2D();
            EnsureFolderExists(ScenesFolderPath);

            // Tạo Scene 2D mới
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Camera (2D Orthographic)
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.36f, 0.16f); // Xanh cỏ nông trại
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // 2. Global Light 2D (Chiếu sáng ngày/đêm theo 4 buổi)
            var lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(1.0f, 0.95f, 0.85f);
            light2D.intensity = 1.0f;

            var dayNight = lightObj.AddComponent<DayNight2DController>();
            dayNight.GlobalLight2D = light2D;

            // 3. Game Controller
            var gameControllerObj = new GameObject("GameController");
            gameControllerObj.AddComponent<MobileGameController>();

            // 4. Player (2D Top-down)
            var playerObj = new GameObject("Player (Khoa)");
            playerObj.transform.position = Vector3.zero;

            var playerSprite = playerObj.AddComponent<SpriteRenderer>();
            playerSprite.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            playerSprite.color = new Color(0.2f, 0.6f, 1.0f); // Xanh dương
            playerObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var playerRb = playerObj.AddComponent<Rigidbody2D>();
            playerRb.gravityScale = 0f;
            playerRb.freezeRotation = true;

            var playerCol = playerObj.AddComponent<CircleCollider2D>();
            playerCol.radius = 0.5f;

            var playerCtrl = playerObj.AddComponent<PlayerMobileController>();
            playerCtrl.SpriteRenderer = playerSprite;

            // 5. Đàn heo mẫu 2D (4 con đại diện)
            string[] pigNames = { "Hồng Điền #1", "Lam Khê #1", "Kim Thọ #1 (Huyền thoại)", "Hư Thể #1 (Dị biến)" };
            Color[] pigColors = { 
                new Color(1f, 0.75f, 0.8f),      // Hồng
                new Color(0.6f, 0.85f, 0.9f),    // Xanh nhạt
                new Color(1f, 0.85f, 0.2f),      // Vàng ánh kim
                new Color(0.7f, 0.4f, 0.9f)       // Tím dị biến
            };

            for (int i = 0; i < 4; i++)
            {
                var pigObj = new GameObject($"Pig_{i + 1} ({pigNames[i]})");
                pigObj.transform.position = new Vector3(UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(-4f, 4f), 0);

                var sRender = pigObj.AddComponent<SpriteRenderer>();
                sRender.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                sRender.color = pigColors[i];
                pigObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

                var pRb = pigObj.AddComponent<Rigidbody2D>();
                pRb.gravityScale = 0f;
                pRb.freezeRotation = true;

                var pCol = pigObj.AddComponent<CircleCollider2D>();
                pCol.radius = 0.45f;

                var agent = pigObj.AddComponent<PigAgentView>();
                agent.SpriteRenderer = sRender;
            }

            // 6. UI Canvas (Mobile HUD & Touch Joystick)
            var canvasObj = new GameObject("Mobile Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Virtual Joystick UI
            var joystickBg = new GameObject("Joystick_Background");
            joystickBg.transform.SetParent(canvasObj.transform, false);
            var joyBgImg = joystickBg.AddComponent<Image>();
            joyBgImg.color = new Color(1f, 1f, 1f, 0.25f);
            var joyBgRect = joystickBg.GetComponent<RectTransform>();
            joyBgRect.anchorMin = new Vector2(0f, 0f);
            joyBgRect.anchorMax = new Vector2(0f, 0f);
            joyBgRect.pivot = new Vector2(0.5f, 0.5f);
            joyBgRect.anchoredPosition = new Vector2(160f, 160f);
            joyBgRect.sizeDelta = new Vector2(160f, 160f);

            var joystickHandle = new GameObject("Joystick_Handle");
            joystickHandle.transform.SetParent(joystickBg.transform, false);
            var joyHandleImg = joystickHandle.AddComponent<Image>();
            joyHandleImg.color = new Color(1f, 1f, 1f, 0.7f);
            var joyHandleRect = joystickHandle.GetComponent<RectTransform>();
            joyHandleRect.sizeDelta = new Vector2(65f, 65f);

            var joystick = joystickBg.AddComponent<MobileJoystick>();
            joystick.BackgroundRect = joyBgRect;
            joystick.HandleRect = joyHandleRect;
            playerCtrl.Joystick = joystick;

            // HUD Manager
            var hudCtrl = canvasObj.AddComponent<MobileHUDController>();
            hudCtrl.PlayerController = playerCtrl;

            // EventSystem
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Lưu Scene
            EditorSceneManager.SaveScene(scene, MainScenePath);

            // Thêm Scene vào Build Settings
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            Debug.Log($"<color=green>[PigTycoon] ĐÃ TẠO VÀ MỞ SCENE UNIVERSAL 2D TẠI: {MainScenePath}!</color>");
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
