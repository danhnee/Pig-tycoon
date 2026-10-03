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

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // 1x1 pure flat white sprite (chuẩn 1 đơn vị thế giới, không bị méo viền 9-slice)
            Sprite defaultSquare = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                Texture2D.whiteTexture.width
            );
            Sprite defaultKnob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // 1. Main Camera (2D Orthographic)
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 13.5f; // Bao quát toàn cảnh nông trại 30x20
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.14f, 0.28f, 0.12f); // Rìa ngoài nông trại
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // 2. Global Light 2D & Controller
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

            // 4. Farm Environment 2D Manager
            var farmEnvObj = new GameObject("FarmEnvironment_2D");
            var farmEnv = farmEnvObj.AddComponent<FarmEnvironment2D>();
            farmEnv.FarmBounds = new Rect(-15f, -10f, 30f, 20f);

            // 4.5. Nền cỏ Nông Trại 2D (Farm Lawn Ground - Rộng 32x22 đơn vị)
            var groundObj = new GameObject("FarmGround_Grass");
            groundObj.transform.position = Vector3.zero;
            var groundSprite = groundObj.AddComponent<SpriteRenderer>();
            groundSprite.sprite = defaultSquare;
            groundSprite.color = new Color(0.30f, 0.58f, 0.26f); // Xanh cỏ nông trại tươi
            groundObj.transform.localScale = new Vector3(32f, 22f, 1f);
            groundSprite.sortingOrder = -1000; // Luôn nằm dưới cùng

            // 5. Boundary Fences (Hàng rào gỗ 300 HP bao quanh kín, cổng mở 5m phía dưới)
            var fencesGroup = new GameObject("Boundary_Fences");
            CreateFenceSegment(fencesGroup.transform, defaultSquare, new Vector3(0, 10f, 0), new Vector3(30.8f, 0.8f, 1f), "Fence_Top");
            CreateFenceSegment(fencesGroup.transform, defaultSquare, new Vector3(-5f, -10f, 0), new Vector3(20.8f, 0.8f, 1f), "Fence_Bottom_Left");
            CreateFenceSegment(fencesGroup.transform, defaultSquare, new Vector3(12.5f, -10f, 0), new Vector3(5.8f, 0.8f, 1f), "Fence_Bottom_Right"); // Cổng 5m từ x=5 đến x=10
            CreateFenceSegment(fencesGroup.transform, defaultSquare, new Vector3(-15f, 0, 0), new Vector3(0.8f, 20.8f, 1f), "Fence_Left");
            CreateFenceSegment(fencesGroup.transform, defaultSquare, new Vector3(15f, 0, 0), new Vector3(0.8f, 20.8f, 1f), "Fence_Right");

            // 6. Mái Trú (Shelter 2D - Góc trên bên trái)
            var shelterObj = new GameObject("Shelter_Zone");
            shelterObj.transform.position = new Vector3(-9.5f, 5.5f, 0);
            var shelterSprite = shelterObj.AddComponent<SpriteRenderer>();
            shelterSprite.sprite = defaultSquare;
            shelterSprite.color = new Color(0.42f, 0.28f, 0.16f, 0.9f); // Mái gỗ nâu sẫm
            shelterObj.transform.localScale = new Vector3(8f, 5.5f, 1f);
            var shelterCol = shelterObj.AddComponent<BoxCollider2D>();
            shelterCol.isTrigger = true;
            shelterObj.AddComponent<Shelter2DView>();

            // 7. Máng Ăn (Feeders 2D)
            CreateFeeder(defaultSquare, new Vector3(-4f, 2.5f, 0), "Feeder_1");
            CreateFeeder(defaultSquare, new Vector3(4f, 2.5f, 0), "Feeder_2");

            // 8. Bồn Nước (Water Troughs 2D)
            CreateWaterTrough(defaultSquare, new Vector3(-4f, -2.5f, 0), "WaterTrough_1");
            CreateWaterTrough(defaultSquare, new Vector3(4f, -2.5f, 0), "WaterTrough_2");

            // 9. Bãi Bùn Làm Mát (Mud Pit 2D - Góc phải)
            var mudObj = new GameObject("MudPit_Zone");
            mudObj.transform.position = new Vector3(9f, -4f, 0);
            var mudSprite = mudObj.AddComponent<SpriteRenderer>();
            mudSprite.sprite = defaultSquare;
            mudSprite.color = new Color(0.48f, 0.35f, 0.20f, 0.95f); // Màu bùn tắm
            mudObj.transform.localScale = new Vector3(8f, 6f, 1f);
            var mudCol = mudObj.AddComponent<BoxCollider2D>();
            mudCol.isTrigger = true;
            mudObj.AddComponent<MudPit2DView>();

            // 10. Khu Xử Lý Xác (Corpse Lot 2D - Góc dưới bên trái)
            var corpseLotObj = new GameObject("CorpseLot_Zone");
            corpseLotObj.transform.position = new Vector3(-10f, -6f, 0);
            var corpseLotSprite = corpseLotObj.AddComponent<SpriteRenderer>();
            corpseLotSprite.sprite = defaultSquare;
            corpseLotSprite.color = new Color(0.32f, 0.35f, 0.38f, 0.85f); // Đá cách ly
            corpseLotObj.transform.localScale = new Vector3(6f, 5f, 1f);
            corpseLotObj.AddComponent<CorpseLot2DView>();

            // 11. Tháp Phòng Thủ (Defense Tower: Nỏ Xuyên Vân)
            var towerObj = new GameObject("DefenseTower (NoXuyenVan)");
            towerObj.transform.position = new Vector3(13f, 8f, 0);
            var towerSprite = towerObj.AddComponent<SpriteRenderer>();
            towerSprite.sprite = defaultKnob;
            towerSprite.color = new Color(0.85f, 0.25f, 0.25f);
            towerObj.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            var towerCol = towerObj.AddComponent<CircleCollider2D>();
            towerCol.radius = 0.5f;
            towerObj.AddComponent<DefenseTower2DView>();

            // 12. Player (2D Top-down)
            var playerObj = new GameObject("Player (Khoa)");
            playerObj.transform.position = Vector3.zero;

            var playerSprite = playerObj.AddComponent<SpriteRenderer>();
            playerSprite.sprite = defaultKnob;
            playerSprite.color = new Color(0.2f, 0.6f, 1.0f); // Xanh dương
            playerObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var playerRb = playerObj.AddComponent<Rigidbody2D>();
            playerRb.gravityScale = 0f;
            playerRb.freezeRotation = true;

            var playerCol = playerObj.AddComponent<CircleCollider2D>();
            playerCol.radius = 0.5f;

            var playerCtrl = playerObj.AddComponent<PlayerMobileController>();
            playerCtrl.SpriteRenderer = playerSprite;

            // 13. Đàn heo mẫu 2D
            string[] pigNames = { "Hồng Điền #1", "Lam Khê #1", "Kim Thọ #1 (Huyền thoại)", "Hư Thể #1 (Dị biến)" };
            Color[] pigColors = { 
                new Color(1f, 0.75f, 0.8f),      // Hồng
                new Color(0.6f, 0.85f, 0.9f),    // Xanh Lam Khê
                new Color(1f, 0.85f, 0.2f),      // Vàng ánh kim
                new Color(0.7f, 0.4f, 0.9f)       // Tím dị biến
            };

            GeneLineId[] geneLines = {
                GeneLineId.HongDien,
                GeneLineId.LamKhe,
                GeneLineId.KimTho,
                GeneLineId.HuThe
            };

            for (int i = 0; i < 4; i++)
            {
                var pigObj = new GameObject($"Pig_{i + 1} ({pigNames[i]})");
                pigObj.transform.position = new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0);

                var sRender = pigObj.AddComponent<SpriteRenderer>();
                sRender.sprite = defaultKnob;
                sRender.color = pigColors[i];
                pigObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

                var pRb = pigObj.AddComponent<Rigidbody2D>();
                pRb.gravityScale = 0f;
                pRb.freezeRotation = true;

                var pCol = pigObj.AddComponent<CircleCollider2D>();
                pCol.radius = 0.45f;

                var agent = pigObj.AddComponent<PigAgentView>();
                agent.SpriteRenderer = sRender;

                var pigModel = Pig.CreateDefault($"pig_{i+1}", pigNames[i], geneLines[i], GeneRarity.Thuong);
                agent.Bind(pigModel, playerObj.transform);
            }

            // 14. UI Canvas (Mobile HUD & Touch Joystick)
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
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Cập nhật các hạ tầng vào môi trường FarmEnvironment2D
            farmEnv.RefreshInfrastructureRegistries();

            // Lưu Scene
            EditorSceneManager.SaveScene(scene, MainScenePath);

            // Thêm Scene vào Build Settings
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            Debug.Log($"<color=green>[PigTycoon] ĐÃ TẠO TOÀN DIỆN BẢN ĐỒ NÔNG TRẠI 2D (HÀNG RÀO, MÁI TRÚ, MÁNG ĂN, BÃI BÙN, KHU XỬ LÝ) TẠI: {MainScenePath}!</color>");
        }

        private static void CreateFenceSegment(Transform parent, Sprite sprite, Vector3 pos, Vector3 scale, string name)
        {
            var fence = new GameObject(name);
            fence.transform.SetParent(parent);
            fence.transform.position = pos;
            fence.transform.localScale = scale;

            var sr = fence.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.55f, 0.38f, 0.22f);

            fence.AddComponent<BoxCollider2D>();
            fence.AddComponent<Fence2DView>();
        }

        private static void CreateFeeder(Sprite sprite, Vector3 pos, string name)
        {
            var feeder = new GameObject(name);
            feeder.transform.position = pos;
            feeder.transform.localScale = new Vector3(3f, 1.4f, 1f);

            var sr = feeder.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.88f, 0.72f, 0.40f);

            var col = feeder.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            feeder.AddComponent<Feeder2DView>();
        }

        private static void CreateWaterTrough(Sprite sprite, Vector3 pos, string name)
        {
            var trough = new GameObject(name);
            trough.transform.position = pos;
            trough.transform.localScale = new Vector3(3f, 1.4f, 1f);

            var sr = trough.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.25f, 0.65f, 0.95f);

            var col = trough.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            trough.AddComponent<WaterTrough2DView>();
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
