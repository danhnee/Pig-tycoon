using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Tilemaps;
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
            cam.orthographicSize = 6.5f; // Zoom gần ấm cúng chuẩn Stardew Valley (thay vì 13.5)
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.14f, 0.28f, 0.12f); // Rìa ngoài nông trại
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var camFollow = camObj.AddComponent<CameraFollow2D>();
            camFollow.TargetOrthoSize = 6.5f;
            camFollow.SmoothSpeed = 6.0f;
            camFollow.ClampToFarmBounds = true;

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

            // Nạp toàn bộ Sprite Pixel Art & Tileset
            Sprite grassBaseSp = LoadSprite("Assets/Art/Tiles/grass_base.png", defaultSquare);
            Sprite flower1Sp = LoadSprite("Assets/Art/Tiles/grass_flower_1.png", grassBaseSp);
            Sprite flower2Sp = LoadSprite("Assets/Art/Tiles/grass_flower_2.png", grassBaseSp);
            Sprite flower3Sp = LoadSprite("Assets/Art/Tiles/grass_flower_3.png", grassBaseSp);
            Sprite stonePathSp = LoadSprite("Assets/Art/Tiles/stone_path.png", defaultSquare);
            Sprite dirtPatchSp = LoadSprite("Assets/Art/Tiles/dirt_patch.png", defaultSquare);
            Sprite mudTileSp = LoadSprite("Assets/Art/Tiles/mud_tile.png", defaultSquare);

            Sprite fenceHSp = LoadSprite("Assets/Art/Sprites/Environment/fence_h.png", defaultSquare);
            Sprite feederSp = LoadSprite("Assets/Art/Sprites/Environment/feeder_trough.png", defaultSquare);
            Sprite waterTroughSp = LoadSprite("Assets/Art/Sprites/Environment/water_trough.png", defaultSquare);
            Sprite barnSp = LoadSprite("Assets/Art/Sprites/Environment/shelter_barn.png", defaultSquare);
            Sprite towerSp = LoadSprite("Assets/Art/Sprites/Environment/defense_tower.png", defaultKnob);
            Sprite corpseLotSp = LoadSprite("Assets/Art/Sprites/Environment/corpse_lot.png", defaultSquare);

            // Sprite nhân vật Khoa
            Sprite pDownIdle = LoadSprite("Assets/Art/Sprites/Characters/player_down_idle.png", defaultKnob);
            Sprite pDownWalk1 = LoadSprite("Assets/Art/Sprites/Characters/player_down_walk1.png", pDownIdle);
            Sprite pDownWalk2 = LoadSprite("Assets/Art/Sprites/Characters/player_down_walk2.png", pDownIdle);
            Sprite pUpIdle = LoadSprite("Assets/Art/Sprites/Characters/player_up_idle.png", pDownIdle);
            Sprite pUpWalk1 = LoadSprite("Assets/Art/Sprites/Characters/player_up_walk1.png", pUpIdle);
            Sprite pUpWalk2 = LoadSprite("Assets/Art/Sprites/Characters/player_up_walk2.png", pUpIdle);
            Sprite pSideIdle = LoadSprite("Assets/Art/Sprites/Characters/player_side_idle.png", pDownIdle);
            Sprite pSideWalk1 = LoadSprite("Assets/Art/Sprites/Characters/player_side_walk1.png", pSideIdle);
            Sprite pSideWalk2 = LoadSprite("Assets/Art/Sprites/Characters/player_side_walk2.png", pSideIdle);
            Sprite pAction = LoadSprite("Assets/Art/Sprites/Characters/player_action.png", pDownIdle);

            // 4.5. Hệ thống Tilemap 2D Nông Trại (Cỏ xanh điểm hoa dại, Đường lát đá sỏi, Bãi bùn)
            var gridObj = new GameObject("Farm_Grid");
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            var groundTilemapObj = new GameObject("Tilemap_Ground");
            groundTilemapObj.transform.SetParent(gridObj.transform, false);
            var groundTilemap = groundTilemapObj.AddComponent<Tilemap>();
            var groundRenderer = groundTilemapObj.AddComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = -100;

            var pathTilemapObj = new GameObject("Tilemap_Paths");
            pathTilemapObj.transform.SetParent(gridObj.transform, false);
            var pathTilemap = pathTilemapObj.AddComponent<Tilemap>();
            var pathRenderer = pathTilemapObj.AddComponent<TilemapRenderer>();
            pathRenderer.sortingOrder = -90;

            var mudTilemapObj = new GameObject("Tilemap_MudPit");
            mudTilemapObj.transform.SetParent(gridObj.transform, false);
            var mudTilemap = mudTilemapObj.AddComponent<Tilemap>();
            var mudRenderer = mudTilemapObj.AddComponent<TilemapRenderer>();
            mudRenderer.sortingOrder = -80;

            Tile grassTile = ScriptableObject.CreateInstance<Tile>(); grassTile.sprite = grassBaseSp;
            Tile flowerTile1 = ScriptableObject.CreateInstance<Tile>(); flowerTile1.sprite = flower1Sp;
            Tile flowerTile2 = ScriptableObject.CreateInstance<Tile>(); flowerTile2.sprite = flower2Sp;
            Tile flowerTile3 = ScriptableObject.CreateInstance<Tile>(); flowerTile3.sprite = flower3Sp;
            Tile stoneTile = ScriptableObject.CreateInstance<Tile>(); stoneTile.sprite = stonePathSp;
            Tile mudTile = ScriptableObject.CreateInstance<Tile>(); mudTile.sprite = mudTileSp;

            // Tô nền cỏ nông trại với các bụi hoa dại ngẫu nhiên
            for (int x = -16; x <= 16; x++)
            {
                for (int y = -11; y <= 11; y++)
                {
                    float r = UnityEngine.Random.value;
                    Tile t = grassTile;
                    if (r < 0.035f) t = flowerTile1;
                    else if (r < 0.07f) t = flowerTile2;
                    else if (r < 0.10f) t = flowerTile3;
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), t);
                }
            }

            // Vẽ lối đi lát đá sỏi (Cobblestone Paths)
            for (int y = -10; y <= 0; y++) pathTilemap.SetTile(new Vector3Int(7, y, 0), stoneTile);
            for (int x = 0; x <= 7; x++) pathTilemap.SetTile(new Vector3Int(x, 0, 0), stoneTile);
            for (int x = -9; x <= 0; x++) pathTilemap.SetTile(new Vector3Int(x, 3, 0), stoneTile);
            for (int y = 0; y <= 5; y++) pathTilemap.SetTile(new Vector3Int(-9, y, 0), stoneTile);
            for (int x = 4; x <= 9; x++) pathTilemap.SetTile(new Vector3Int(x, -2, 0), stoneTile);

            // Vẽ bãi bùn tắm
            for (int mx = 6; mx <= 12; mx++)
            {
                for (int my = -6; my <= -2; my++)
                {
                    mudTilemap.SetTile(new Vector3Int(mx, my, 0), mudTile);
                }
            }

            // 5. Boundary Fences (Hàng rào gỗ 300 HP bao quanh kín, cổng mở 5m phía dưới)
            var fencesGroup = new GameObject("Boundary_Fences");
            CreateFenceSegment(fencesGroup.transform, fenceHSp, new Vector3(0, 10f, 0), new Vector3(30.8f, 0.8f, 1f), "Fence_Top");
            CreateFenceSegment(fencesGroup.transform, fenceHSp, new Vector3(-5f, -10f, 0), new Vector3(20.8f, 0.8f, 1f), "Fence_Bottom_Left");
            CreateFenceSegment(fencesGroup.transform, fenceHSp, new Vector3(12.5f, -10f, 0), new Vector3(5.8f, 0.8f, 1f), "Fence_Bottom_Right"); // Cổng 5m từ x=5 đến x=10
            CreateFenceSegment(fencesGroup.transform, fenceHSp, new Vector3(-15f, 0, 0), new Vector3(0.8f, 20.8f, 1f), "Fence_Left");
            CreateFenceSegment(fencesGroup.transform, fenceHSp, new Vector3(15f, 0, 0), new Vector3(0.8f, 20.8f, 1f), "Fence_Right");

            // 6. Mái Trú (Shelter 2D - Nhà kho nông trại góc trên bên trái)
            var shelterObj = new GameObject("Shelter_Zone");
            shelterObj.transform.position = new Vector3(-9.5f, 5.5f, 0);
            var shelterSprite = shelterObj.AddComponent<SpriteRenderer>();
            shelterSprite.sprite = barnSp;
            shelterSprite.color = Color.white;
            shelterObj.transform.localScale = new Vector3(6f, 4.5f, 1f);
            var shelterCol = shelterObj.AddComponent<BoxCollider2D>();
            shelterCol.size = new Vector2(5.5f, 4.0f);
            shelterCol.isTrigger = true;
            shelterObj.AddComponent<Shelter2DView>();

            // 7. Máng Ăn (Feeders 2D)
            CreateFeeder(feederSp, new Vector3(-4f, 2.5f, 0), "Feeder_1");
            CreateFeeder(feederSp, new Vector3(4f, 2.5f, 0), "Feeder_2");

            // 8. Bồn Nước (Water Troughs 2D)
            CreateWaterTrough(waterTroughSp, new Vector3(-4f, -2.5f, 0), "WaterTrough_1");
            CreateWaterTrough(waterTroughSp, new Vector3(4f, -2.5f, 0), "WaterTrough_2");

            // 9. Bãi Bùn Làm Mát (Mud Pit 2D - Góc phải)
            var mudObj = new GameObject("MudPit_Zone");
            mudObj.transform.position = new Vector3(9f, -4f, 0);
            var mudCol = mudObj.AddComponent<BoxCollider2D>();
            mudCol.size = new Vector2(7f, 5f);
            mudCol.isTrigger = true;
            mudObj.AddComponent<MudPit2DView>();

            // 10. Khu Xử Lý Xác (Corpse Lot 2D - Góc dưới bên trái)
            var corpseLotObj = new GameObject("CorpseLot_Zone");
            corpseLotObj.transform.position = new Vector3(-10f, -6f, 0);
            var corpseLotSprite = corpseLotObj.AddComponent<SpriteRenderer>();
            corpseLotSprite.sprite = corpseLotSp;
            corpseLotSprite.color = Color.white;
            corpseLotObj.transform.localScale = new Vector3(5f, 3.5f, 1f);
            corpseLotObj.AddComponent<CorpseLot2DView>();

            // 11. Tháp Phòng Thủ (Defense Tower: Nỏ Xuyên Vân)
            var towerObj = new GameObject("DefenseTower (NoXuyenVan)");
            towerObj.transform.position = new Vector3(13f, 8f, 0);
            var towerSprite = towerObj.AddComponent<SpriteRenderer>();
            towerSprite.sprite = towerSp;
            towerSprite.color = Color.white;
            towerObj.transform.localScale = new Vector3(2.2f, 3.0f, 1f);
            var towerCol = towerObj.AddComponent<CircleCollider2D>();
            towerCol.radius = 0.5f;
            towerObj.AddComponent<DefenseTower2DView>();

            // 12. Player (Khoa - 2D Top-down Pixel Art)
            var playerObj = new GameObject("Player (Khoa)");
            playerObj.transform.position = Vector3.zero;
            playerObj.tag = "Player";

            var playerSprite = playerObj.AddComponent<SpriteRenderer>();
            playerSprite.sprite = pDownIdle;
            playerSprite.color = Color.white;
            playerObj.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            var playerRb = playerObj.AddComponent<Rigidbody2D>();
            playerRb.gravityScale = 0f;
            playerRb.freezeRotation = true;

            var playerCol = playerObj.AddComponent<CircleCollider2D>();
            playerCol.radius = 0.4f;

            var playerCtrl = playerObj.AddComponent<PlayerMobileController>();
            playerCtrl.SpriteRenderer = playerSprite;
            var playerInteract = playerObj.AddComponent<PlayerInteractionController>();

            var charAnim = playerObj.AddComponent<CharacterSpriteAnimator>();
            charAnim.DownIdle = pDownIdle;
            charAnim.DownWalk = new[] { pDownWalk1, pDownIdle, pDownWalk2 };
            charAnim.UpIdle = pUpIdle;
            charAnim.UpWalk = new[] { pUpWalk1, pUpIdle, pUpWalk2 };
            charAnim.SideIdle = pSideIdle;
            charAnim.SideWalk = new[] { pSideWalk1, pSideIdle, pSideWalk2 };
            charAnim.ActionSprite = pAction;

            camFollow.Target = playerObj.transform;

            // 13. Đàn heo mẫu 2D (4 giống heo với Sprite Pixel Art & Animation riêng biệt)
            string[] pigNames = { "Hồng Điền #1", "Lam Khê #1", "Kim Thọ #1 (Huyền thoại)", "Hư Thể #1 (Dị biến)" };
            string[] breedKeys = { "hong_dien", "lam_khe", "kim_tho", "hu_the" };
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

                string bKey = breedKeys[i];
                Sprite pIdle = LoadSprite($"Assets/Art/Sprites/Pigs/{bKey}/idle.png", defaultKnob);
                Sprite pWalk1 = LoadSprite($"Assets/Art/Sprites/Pigs/{bKey}/walk1.png", pIdle);
                Sprite pWalk2 = LoadSprite($"Assets/Art/Sprites/Pigs/{bKey}/walk2.png", pIdle);
                Sprite pEat = LoadSprite($"Assets/Art/Sprites/Pigs/{bKey}/eat.png", pIdle);
                Sprite pSleep = LoadSprite($"Assets/Art/Sprites/Pigs/{bKey}/sleep.png", pIdle);

                var sRender = pigObj.AddComponent<SpriteRenderer>();
                sRender.sprite = pIdle;
                sRender.color = Color.white;
                pigObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                var pRb = pigObj.AddComponent<Rigidbody2D>();
                pRb.gravityScale = 0f;
                pRb.freezeRotation = true;

                var pCol = pigObj.AddComponent<CircleCollider2D>();
                pCol.radius = 0.45f;

                var agent = pigObj.AddComponent<PigAgentView>();
                agent.SpriteRenderer = sRender;

                var pigAnim = pigObj.AddComponent<PigSpriteAnimator>();
                pigAnim.SetBreedSprites(pIdle, new[] { pWalk1, pWalk2 }, pEat, pSleep);

                var pigModel = Pig.CreateDefault($"pig_{i+1}", pigNames[i], geneLines[i], GeneRarity.Thuong);
                agent.Bind(pigModel, playerObj.transform);
            }

            // 14. UI Canvas Phong Cách Stardew Valley
            var canvasObj = new GameObject("Mobile Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 14.1. Virtual Joystick UI (Góc dưới bên trái)
            var joystickBg = new GameObject("Joystick_Background");
            joystickBg.transform.SetParent(canvasObj.transform, false);
            var joyBgImg = joystickBg.AddComponent<Image>();
            joyBgImg.color = new Color(1f, 1f, 1f, 0.22f);
            var joyBgRect = joystickBg.GetComponent<RectTransform>();
            joyBgRect.anchorMin = new Vector2(0f, 0f);
            joyBgRect.anchorMax = new Vector2(0f, 0f);
            joyBgRect.pivot = new Vector2(0.5f, 0.5f);
            joyBgRect.anchoredPosition = new Vector2(160f, 160f);
            joyBgRect.sizeDelta = new Vector2(160f, 160f);

            var joystickHandle = new GameObject("Joystick_Handle");
            joystickHandle.transform.SetParent(joystickBg.transform, false);
            var joyHandleImg = joystickHandle.AddComponent<Image>();
            joyHandleImg.color = new Color(1f, 1f, 1f, 0.75f);
            var joyHandleRect = joystickHandle.GetComponent<RectTransform>();
            joyHandleRect.sizeDelta = new Vector2(65f, 65f);

            var joystick = joystickBg.AddComponent<MobileJoystick>();
            joystick.BackgroundRect = joyBgRect;
            joystick.HandleRect = joyHandleRect;
            playerCtrl.Joystick = joystick;

            // 14.2. Stardew Valley Top-Right Clock & Calendar Panel (Bảng gỗ góc trên bên phải)
            var clockPanel = CreateUIPanel(canvasObj.transform, "Stardew_Clock_Panel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-25, -25), new Vector2(280, 135), new Color(0.86f, 0.68f, 0.42f));
            AddUIOutline(clockPanel, new Color(0.35f, 0.18f, 0.05f), new Vector2(3, -3));

            var dateText = CreateUIText(clockPanel.transform, "DateWeatherText", "Ngày 1 (Sáng) ☀️", 20, FontStyles.Bold, new Color(0.28f, 0.14f, 0.04f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(-20, 26), TextAlignmentOptions.Center);
            var timeText = CreateUIText(clockPanel.transform, "TimeText", "06:00 AM", 28, FontStyles.Bold, new Color(0.22f, 0.10f, 0.02f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(-20, 32), TextAlignmentOptions.Center);

            var goldBox = CreateUIPanel(clockPanel.transform, "GoldBox", new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.76f, 0.55f, 0.28f));
            AddUIOutline(goldBox, new Color(0.28f, 0.14f, 0.04f), new Vector2(1.5f, -1.5f));
            var goldText = CreateUIText(goldBox.transform, "GoldText", "🪙 2,500g", 21, FontStyles.Bold, new Color(1f, 0.95f, 0.7f), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);

            var farmStatusText = CreateUIText(clockPanel.transform, "FarmStatusText", "Heo: 4/12 | SC: 75% | ÁLSK: 10", 12, FontStyles.Normal, new Color(0.32f, 0.16f, 0.05f), new Vector2(0, 0), new Vector2(1, 0.22f), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(0, 20), TextAlignmentOptions.Center);

            // 14.3. Stardew Valley Bottom-Right Energy Bar (Thanh năng lượng 'E' phong cách Stardew)
            var energyRoot = CreateUIPanel(canvasObj.transform, "Stardew_Energy_Bar", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 25), new Vector2(34, 180), new Color(0.24f, 0.12f, 0.04f));
            AddUIOutline(energyRoot, new Color(0.12f, 0.06f, 0.02f), new Vector2(2, -2));

            var energyFillObj = new GameObject("Energy_Fill");
            energyFillObj.transform.SetParent(energyRoot.transform, false);
            var fillImg = energyFillObj.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Vertical;
            fillImg.fillOrigin = 0;
            fillImg.color = new Color(0.35f, 0.85f, 0.38f);
            var fillRect = energyFillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.15f, 0.05f);
            fillRect.anchorMax = new Vector2(0.85f, 0.82f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var badgeObj = CreateUIPanel(energyRoot.transform, "E_Badge", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(32, 32), new Color(0.85f, 0.22f, 0.18f));
            AddUIOutline(badgeObj, new Color(0.35f, 0.08f, 0.06f), new Vector2(2, -2));
            CreateUIText(badgeObj.transform, "E_Text", "E", 18, FontStyles.Bold, new Color(1f, 0.92f, 0.45f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);

            var energyText = CreateUIText(energyRoot.transform, "EnergyText", "100", 11, FontStyles.Bold, Color.white, new Vector2(0, 0), new Vector2(1, 0.15f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);

            // 14.4. Stardew Valley Bottom Tool Hotbar (Thanh công cụ 8 ô đáy màn hình)
            var hotbarRoot = CreateUIPanel(canvasObj.transform, "Stardew_Hotbar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 15), new Vector2(540, 75), new Color(0.65f, 0.43f, 0.22f));
            AddUIOutline(hotbarRoot, new Color(0.32f, 0.16f, 0.05f), new Vector2(3, -3));

            var toolNameText = CreateUIText(hotbarRoot.transform, "ToolNameText", "[ Bao Cám ]", 18, FontStyles.Bold, new Color(1f, 0.95f, 0.75f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(0, 24), TextAlignmentOptions.Center);
            AddUIOutline(toolNameText.gameObject, new Color(0.2f, 0.1f, 0.02f), new Vector2(1.5f, -1.5f));

            var slotsContainer = new GameObject("Slots_Container");
            slotsContainer.transform.SetParent(hotbarRoot.transform, false);
            var slotsRect = slotsContainer.AddComponent<RectTransform>();
            slotsRect.anchorMin = Vector2.zero;
            slotsRect.anchorMax = Vector2.one;
            slotsRect.offsetMin = new Vector2(8, 6);
            slotsRect.offsetMax = new Vector2(-8, -6);

            string[] toolEmojis = { "🌾", "💧", "🔨", "❤️", "🔍", "⚔️", "🧪", "☁️" };
            for (int i = 0; i < 8; i++)
            {
                float posX = -228f + (i * 65f);
                var slotObj = CreateUIPanel(slotsContainer.transform, $"Slot_{i}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(posX, 0), new Vector2(56, 56), new Color(0.82f, 0.62f, 0.38f));
                slotObj.AddComponent<Button>();
                var outline = slotObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.35f, 0.18f, 0.05f);
                outline.effectDistance = new Vector2(2, -2);

                CreateUIText(slotObj.transform, "Emoji", toolEmojis[i], 28, FontStyles.Normal, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            }

            var hotbarCtrl = hotbarRoot.AddComponent<HotbarController>();
            hotbarCtrl.SlotsContainer = slotsContainer.transform;
            hotbarCtrl.ToolNameText = toolNameText;

            // 14.5. Context Action Button (Nút tương tác ngữ cảnh nổi)
            var actionBtnObj = CreateUIPanel(canvasObj.transform, "Context_Action_Button", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80, 80), new Vector2(240, 60), new Color(0.84f, 0.58f, 0.28f));
            AddUIOutline(actionBtnObj, new Color(0.35f, 0.16f, 0.05f), new Vector2(3, -3));
            var actionBtn = actionBtnObj.AddComponent<Button>();
            var actionBtnText = CreateUIText(actionBtnObj.transform, "ActionText", "🌾 Đổ Cám (20kg)", 17, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            AddUIOutline(actionBtnText.gameObject, new Color(0.2f, 0.08f, 0.02f), new Vector2(2, -2));

            var feedbackText = CreateUIText(actionBtnObj.transform, "FeedbackText", "", 18, FontStyles.Bold, new Color(1f, 0.95f, 0.35f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0), new Vector2(0, 15), new Vector2(120, 35), TextAlignmentOptions.Center);
            AddUIOutline(feedbackText.gameObject, Color.black, new Vector2(2, -2));
            feedbackText.gameObject.SetActive(false);

            playerInteract.ActionButtonRoot = actionBtnObj;
            playerInteract.ActionButton = actionBtn;
            playerInteract.ActionButtonText = actionBtnText;
            playerInteract.FeedbackFloatingText = feedbackText;

            // 14.6. Stardew Valley Pig Inspect Popup (Hộp thoại giám định heo khung gỗ)
            var popupRoot = CreateUIPanel(canvasObj.transform, "Pig_Inspect_Popup", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480, 390), new Color(0.68f, 0.46f, 0.24f));
            AddUIOutline(popupRoot, new Color(0.28f, 0.12f, 0.03f), new Vector2(4, -4));

            var parchment = CreateUIPanel(popupRoot.transform, "Parchment", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24, -24), new Color(0.98f, 0.93f, 0.82f));
            AddUIOutline(parchment, new Color(0.5f, 0.35f, 0.18f), new Vector2(1.5f, -1.5f));

            var pTitleText = CreateUIText(parchment.transform, "TitleText", "Hồng Điền #1", 22, FontStyles.Bold, new Color(0.28f, 0.12f, 0.03f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(-30, 32), TextAlignmentOptions.Center);
            var pGeneText = CreateUIText(parchment.transform, "GeneText", "Dòng: Hồng Điền [Thường] | Giới tính: Đực", 15, FontStyles.Normal, new Color(0.38f, 0.20f, 0.08f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(-30, 24), TextAlignmentOptions.Center);

            var avatarObj = CreateUIPanel(parchment.transform, "Avatar", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(25, -75), new Vector2(65, 65), new Color(1f, 0.72f, 0.78f));
            avatarObj.GetComponent<Image>().sprite = defaultKnob;
            AddUIOutline(avatarObj, new Color(0.35f, 0.15f, 0.05f), new Vector2(2, -2));

            var pHeartsText = CreateUIText(parchment.transform, "HeartsText", "Thân thiết: ❤️ ❤️ ❤️ 🖤 🖤 (60%)", 16, FontStyles.Bold, new Color(0.85f, 0.15f, 0.25f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(105, -75), new Vector2(-120, 25), TextAlignmentOptions.Left);
            var pMoodText = CreateUIText(parchment.transform, "MoodText", "Tâm trạng: 😊 Bình Ổn (70/100)", 15, FontStyles.Normal, new Color(0.2f, 0.45f, 0.2f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(105, -102), new Vector2(-120, 25), TextAlignmentOptions.Left);

            var pWeightText = CreateUIText(parchment.transform, "WeightText", "Cân nặng: 100.0 kg", 15, FontStyles.Normal, new Color(0.28f, 0.14f, 0.04f), new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(25, -155), new Vector2(-30, 24), TextAlignmentOptions.Left);
            var pStageText = CreateUIText(parchment.transform, "StageText", "Giai đoạn: Trưởng Thành (14 ngày)", 15, FontStyles.Normal, new Color(0.28f, 0.14f, 0.04f), new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -155), new Vector2(-20, 24), TextAlignmentOptions.Left);
            var pMeatQualityText = CreateUIText(parchment.transform, "MeatQualityText", "Phẩm chất thịt: Hạng B", 15, FontStyles.Bold, new Color(0.8f, 0.45f, 0.05f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(25, -185), new Vector2(-50, 24), TextAlignmentOptions.Left);
            var pTraitsText = CreateUIText(parchment.transform, "TraitsText", "Đặc tính: Thuần chủng, không có dị biến.", 14, FontStyles.Italic, new Color(0.35f, 0.20f, 0.10f), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1), new Vector2(25, -215), new Vector2(-50, 80), TextAlignmentOptions.TopLeft);

            var closeBtnObj = CreateUIPanel(popupRoot.transform, "CloseButton", new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(6, 6), new Vector2(36, 36), new Color(0.85f, 0.25f, 0.20f));
            AddUIOutline(closeBtnObj, new Color(0.35f, 0.08f, 0.06f), new Vector2(2, -2));
            var closeBtn = closeBtnObj.AddComponent<Button>();
            CreateUIText(closeBtnObj.transform, "X", "✕", 20, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);

            var inspectPopup = popupRoot.AddComponent<PigInspectPopup>();
            inspectPopup.ContentPanel = popupRoot;
            inspectPopup.TitleText = pTitleText;
            inspectPopup.GeneText = pGeneText;
            inspectPopup.PigAvatarImage = avatarObj.GetComponent<Image>();
            inspectPopup.BondingHeartsText = pHeartsText;
            inspectPopup.MoodText = pMoodText;
            inspectPopup.WeightText = pWeightText;
            inspectPopup.StageText = pStageText;
            inspectPopup.MeatQualityText = pMeatQualityText;
            inspectPopup.TraitsText = pTraitsText;
            inspectPopup.CloseButton = closeBtn;

            // 14.7. Stardew HUD Manager
            var stardewHUD = canvasObj.AddComponent<StardewHUDController>();
            stardewHUD.TimeText = timeText;
            stardewHUD.DayPartWeatherText = dateText;
            stardewHUD.GoldText = goldText;
            stardewHUD.FarmStatusText = farmStatusText;
            stardewHUD.EnergyFillBar = fillImg;
            stardewHUD.EnergyText = energyText;
            stardewHUD.PlayerController = playerCtrl;

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

        private static Sprite LoadSprite(string path, Sprite fallback)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sp != null ? sp : fallback;
        }

        private static void CreateFenceSegment(Transform parent, Sprite sprite, Vector3 pos, Vector3 scale, string name)
        {
            var fence = new GameObject(name);
            fence.transform.SetParent(parent);
            fence.transform.position = pos;
            fence.transform.localScale = scale;

            var sr = fence.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;

            fence.AddComponent<BoxCollider2D>();
            fence.AddComponent<Fence2DView>();
        }

        private static void CreateFeeder(Sprite sprite, Vector3 pos, string name)
        {
            var feeder = new GameObject(name);
            feeder.transform.position = pos;
            feeder.transform.localScale = new Vector3(2.5f, 1.4f, 1f);

            var sr = feeder.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;

            var col = feeder.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            feeder.AddComponent<Feeder2DView>();
        }

        private static void CreateWaterTrough(Sprite sprite, Vector3 pos, string name)
        {
            var trough = new GameObject(name);
            trough.transform.position = pos;
            trough.transform.localScale = new Vector3(2.5f, 1.4f, 1f);

            var sr = trough.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;

            var col = trough.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            trough.AddComponent<WaterTrough2DView>();
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            var img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static TextMeshProUGUI CreateUIText(Transform parent, string name, string text, float fontSize, FontStyles fontStyle, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, TextAlignmentOptions alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = alignment;
            return tmp;
        }

        private static Outline AddUIOutline(GameObject obj, Color outlineColor, Vector2 distance)
        {
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = distance;
            return outline;
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
