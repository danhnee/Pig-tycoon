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

        private const string SceneVersionKey = "PigTycoon_SceneVersion_v6";

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
            }

            if (!EditorPrefs.GetBool(SceneVersionKey, false) && !EditorApplication.isPlaying)
            {
                EditorPrefs.SetBool(SceneVersionKey, true);
                Debug.Log("[PigTycoon] Tự động cập nhật Scene MainFarm2D sang phiên bản mới nhất với hệ thống vật lý và bãi rào mới...");
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

        [MenuItem("PigTycoon/2. Tạo & Mở Scene Nông Trại 2D Mẫu", true)]
        public static bool ValidateCreateAndOpen2DScene()
        {
            return !EditorApplication.isPlaying;
        }

        [MenuItem("PigTycoon/2. Tạo & Mở Scene Nông Trại 2D Mẫu")]
        public static void CreateAndOpen2DScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[PigTycoon] Không thể tạo Scene mới trong khi Unity đang ở chế độ Play Mode. Vui lòng tắt nút Play trước!");
                EditorUtility.DisplayDialog(
                    "PigTycoon - Cảnh Báo",
                    "Unity đang ở chế độ Play Mode (đang chạy game).\n\nVui lòng nhấn nút Play trên đỉnh cửa sổ Unity để dừng game trước khi tạo hoặc nạp lại Scene mới!",
                    "Đã hiểu"
                );
                return;
            }

            SetupUniversal2D();
            EnsureFolderExists(ScenesFolderPath);
            TilePaletteBuilder.GenerateTilePalette();

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
            cam.orthographicSize = 5.2f; // Tầm nhìn gần ấm cúng chuẩn Stardew Valley (5.2m)
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.32f, 0.52f, 0.22f); // Chân trời cỏ thảo nguyên
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var camFollow = camObj.AddComponent<CameraFollow2D>();
            camFollow.TargetOrthoSize = 5.2f;
            camFollow.MinOrthoSize = 3.5f;
            camFollow.MaxOrthoSize = 9.5f;
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
            farmEnv.MapBounds = new Rect(-45f, -32f, 90f, 64f); // Toàn bộ thảo nguyên bao la 90m x 64m
            farmEnv.PigPastureBounds = new Rect(-20f, -14f, 40f, 28f); // Khuôn viên rào chăn thả heo 40m x 28m

            // Nạp toàn bộ Sprite Pixel Art & Tileset Thảo Nguyên
            Sprite grassBaseSp = LoadSprite("Assets/Art/Tiles/grass_base.png", defaultSquare);
            Sprite grassSteppeSp = LoadSprite("Assets/Art/Tiles/grass_steppe.png", grassBaseSp);
            Sprite grassTallSp = LoadSprite("Assets/Art/Tiles/grass_tall.png", grassSteppeSp);
            Sprite flower1Sp = LoadSprite("Assets/Art/Tiles/grass_flower_1.png", grassBaseSp);
            Sprite flower2Sp = LoadSprite("Assets/Art/Tiles/grass_flower_2.png", grassBaseSp);
            Sprite flower3Sp = LoadSprite("Assets/Art/Tiles/grass_flower_3.png", grassBaseSp);
            Sprite dirtTrailSp = LoadSprite("Assets/Art/Tiles/dirt_trail.png", defaultSquare);
            Sprite dirtTrailVSp = LoadSprite("Assets/Art/Tiles/dirt_trail_v.png", dirtTrailSp);
            Sprite dirtTrailHSp = LoadSprite("Assets/Art/Tiles/dirt_trail_h.png", dirtTrailSp);
            Sprite dirtTrailCrossSp = LoadSprite("Assets/Art/Tiles/dirt_trail_cross.png", dirtTrailSp);
            Sprite dirtTrailTWestSp = LoadSprite("Assets/Art/Tiles/dirt_trail_t_west.png", dirtTrailSp);
            Sprite dirtTrailTEastSp = LoadSprite("Assets/Art/Tiles/dirt_trail_t_east.png", dirtTrailSp);
            Sprite dirtTrailTNorthSp = LoadSprite("Assets/Art/Tiles/dirt_trail_t_north.png", dirtTrailSp);
            Sprite dirtTrailTSouthSp = LoadSprite("Assets/Art/Tiles/dirt_trail_t_south.png", dirtTrailSp);
            Sprite dirtTrailCornerSWSp = LoadSprite("Assets/Art/Tiles/dirt_trail_corner_sw.png", dirtTrailSp);
            Sprite dirtTrailCornerSESp = LoadSprite("Assets/Art/Tiles/dirt_trail_corner_se.png", dirtTrailSp);
            Sprite dirtTrailCornerNWSp = LoadSprite("Assets/Art/Tiles/dirt_trail_corner_nw.png", dirtTrailSp);
            Sprite dirtTrailCornerNESp = LoadSprite("Assets/Art/Tiles/dirt_trail_corner_ne.png", dirtTrailSp);
            Sprite dirtTrailEndWSp = LoadSprite("Assets/Art/Tiles/dirt_trail_end_w.png", dirtTrailSp);
            Sprite dirtTrailEndESp = LoadSprite("Assets/Art/Tiles/dirt_trail_end_e.png", dirtTrailSp);
            Sprite dirtTrailEndNSp = LoadSprite("Assets/Art/Tiles/dirt_trail_end_n.png", dirtTrailSp);
            Sprite dirtTrailEndSSp = LoadSprite("Assets/Art/Tiles/dirt_trail_end_s.png", dirtTrailSp);
            Sprite stonePathSp = LoadSprite("Assets/Art/Tiles/stone_path.png", defaultSquare);
            Sprite waterTileSp = LoadSprite("Assets/Art/Tiles/water_tile.png", defaultSquare);
            Sprite mudTileSp = LoadSprite("Assets/Art/Tiles/mud_tile.png", defaultSquare);

            Sprite treeSp = LoadSprite("Assets/Art/Sprites/Environment/tree_prairie.png", defaultSquare);
            Sprite rockSp = LoadSprite("Assets/Art/Sprites/Environment/rock_boulder.png", defaultSquare);
            Sprite flowerPatchSp = LoadSprite("Assets/Art/Sprites/Environment/flower_patch.png", defaultSquare);

            Sprite fenceHSp = LoadSprite("Assets/Art/Sprites/Environment/fence_h.png", defaultSquare);
            Sprite fenceVSp = LoadSprite("Assets/Art/Sprites/Environment/fence_v.png", fenceHSp);
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

            // 4.5. Hệ thống Tilemap Đồng Cỏ Thảo Nguyên (Cỏ thảo nguyên, Cỏ cao dập dờn, Hồ nước, Lối mòn)
            var gridObj = new GameObject("Farm_Grid");
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            var groundTilemapObj = new GameObject("Tilemap_Ground");
            groundTilemapObj.transform.SetParent(gridObj.transform, false);
            var groundTilemap = groundTilemapObj.AddComponent<Tilemap>();
            var groundRenderer = groundTilemapObj.AddComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = -10000;

            var waterTilemapObj = new GameObject("Tilemap_Water");
            waterTilemapObj.transform.SetParent(gridObj.transform, false);
            var waterTilemap = waterTilemapObj.AddComponent<Tilemap>();
            var waterRenderer = waterTilemapObj.AddComponent<TilemapRenderer>();
            waterRenderer.sortingOrder = -9800;
            var waterCol = waterTilemapObj.AddComponent<TilemapCollider2D>();

            var mudTilemapObj = new GameObject("Tilemap_MudPit");
            mudTilemapObj.transform.SetParent(gridObj.transform, false);
            var mudTilemap = mudTilemapObj.AddComponent<Tilemap>();
            var mudRenderer = mudTilemapObj.AddComponent<TilemapRenderer>();
            mudRenderer.sortingOrder = -9700;

            var pathTilemapObj = new GameObject("Tilemap_Paths");
            pathTilemapObj.transform.SetParent(gridObj.transform, false);
            var pathTilemap = pathTilemapObj.AddComponent<Tilemap>();
            var pathRenderer = pathTilemapObj.AddComponent<TilemapRenderer>();
            pathRenderer.sortingOrder = -9900;

            Tile grassSteppeTile = ScriptableObject.CreateInstance<Tile>(); grassSteppeTile.sprite = grassSteppeSp;
            Tile grassTallTile = ScriptableObject.CreateInstance<Tile>(); grassTallTile.sprite = grassTallSp;
            Tile grassBaseTile = ScriptableObject.CreateInstance<Tile>(); grassBaseTile.sprite = grassBaseSp;
            Tile flowerTile1 = ScriptableObject.CreateInstance<Tile>(); flowerTile1.sprite = flower1Sp;
            Tile flowerTile2 = ScriptableObject.CreateInstance<Tile>(); flowerTile2.sprite = flower2Sp;
            Tile flowerTile3 = ScriptableObject.CreateInstance<Tile>(); flowerTile3.sprite = flower3Sp;
            Tile dirtTrailTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailTile.sprite = dirtTrailSp;
            Tile dirtTrailVTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailVTile.sprite = dirtTrailVSp;
            Tile dirtTrailHTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailHTile.sprite = dirtTrailHSp;
            Tile dirtTrailCrossTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailCrossTile.sprite = dirtTrailCrossSp;
            Tile dirtTrailTWestTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailTWestTile.sprite = dirtTrailTWestSp;
            Tile dirtTrailTEastTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailTEastTile.sprite = dirtTrailTEastSp;
            Tile dirtTrailTNorthTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailTNorthTile.sprite = dirtTrailTNorthSp;
            Tile dirtTrailTSouthTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailTSouthTile.sprite = dirtTrailTSouthSp;
            Tile dirtTrailCornerSWTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailCornerSWTile.sprite = dirtTrailCornerSWSp;
            Tile dirtTrailCornerSETile = ScriptableObject.CreateInstance<Tile>(); dirtTrailCornerSETile.sprite = dirtTrailCornerSESp;
            Tile dirtTrailCornerNWTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailCornerNWTile.sprite = dirtTrailCornerNWSp;
            Tile dirtTrailCornerNETile = ScriptableObject.CreateInstance<Tile>(); dirtTrailCornerNETile.sprite = dirtTrailCornerNESp;
            Tile dirtTrailEndWTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailEndWTile.sprite = dirtTrailEndWSp;
            Tile dirtTrailEndETile = ScriptableObject.CreateInstance<Tile>(); dirtTrailEndETile.sprite = dirtTrailEndESp;
            Tile dirtTrailEndNTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailEndNTile.sprite = dirtTrailEndNSp;
            Tile dirtTrailEndSTile = ScriptableObject.CreateInstance<Tile>(); dirtTrailEndSTile.sprite = dirtTrailEndSSp;

            farmEnv.SetPathTiles(
                dirtTrailVTile, dirtTrailHTile, dirtTrailCrossTile,
                dirtTrailTWestTile, dirtTrailTEastTile, dirtTrailTNorthTile, dirtTrailTSouthTile,
                dirtTrailCornerSWTile, dirtTrailCornerSETile, dirtTrailCornerNWTile, dirtTrailCornerNETile,
                dirtTrailEndWTile, dirtTrailEndETile, dirtTrailEndNTile, dirtTrailEndSTile
            );
            farmEnv.FenceHSprite = fenceHSp;
            farmEnv.FenceVSprite = fenceVSp;
            Tile waterTile = ScriptableObject.CreateInstance<Tile>(); waterTile.sprite = waterTileSp; waterTile.colliderType = Tile.ColliderType.Grid;
            Tile mudTile = ScriptableObject.CreateInstance<Tile>(); mudTile.sprite = mudTileSp;

            // 1. Phủ kín toàn bộ thảo nguyên bao la 90m x 64m (-48 đến +48, -35 đến +35)
            for (int x = -48; x <= 48; x++)
            {
                for (int y = -35; y <= 35; y++)
                {
                    float r = UnityEngine.Random.value;
                    Tile t = grassSteppeTile;
                    if (r < 0.15f) t = grassTallTile;
                    else if (r < 0.25f) t = grassBaseTile;
                    else if (r < 0.28f) t = flowerTile1;
                    else if (r < 0.31f) t = flowerTile2;
                    else if (r < 0.34f) t = flowerTile3;
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), t);
                }
            }

            // 2. Hồ Nước Thảo Nguyên Tự Nhiên (Prairie Oasis Lake ở vùng thảo nguyên hoang dã phía Đông)
            for (int wx = 23; wx <= 39; wx++)
            {
                for (int wy = -10; wy <= 6; wy++)
                {
                    float dx = (wx - 31f) / 7.5f;
                    float dy = (wy - (-2f)) / 6.0f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        waterTilemap.SetTile(new Vector3Int(wx, wy, 0), waterTile);
                    }
                }
            }

            // 3. Vạt Bùn Tắm Tự Nhiên Nằm Trong Bãi Thả Heo (Phía Đông Nam khu chuồng thả)
            for (int mx = 8; mx <= 16; mx++)
            {
                for (int my = -11; my <= -5; my++)
                {
                    float dx = (mx - 12f) / 3.8f;
                    float dy = (my - (-8f)) / 2.8f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        mudTilemap.SetTile(new Vector3Int(mx, my, 0), mudTile);
                    }
                }
            }

            // 4. Lối mòn đất đỏ thảo nguyên kết nối chuồng chăn thả ra thảo nguyên rộng lớn
            for (int y = -28; y <= 6; y++) pathTilemap.SetTile(new Vector3Int(0, y, 0), dirtTrailVTile);
            for (int x = -13; x <= 0; x++) pathTilemap.SetTile(new Vector3Int(x, 6, 0), dirtTrailHTile);
            for (int x = 0; x <= 7; x++) pathTilemap.SetTile(new Vector3Int(x, -1, 0), dirtTrailHTile);
            for (int x = -30; x <= 25; x++) pathTilemap.SetTile(new Vector3Int(x, -20, 0), dirtTrailHTile);

            FarmEnvironment2D.AutoTilePathMap(
                pathTilemap,
                dirtTrailVTile, dirtTrailHTile, dirtTrailCrossTile,
                dirtTrailTWestTile, dirtTrailTEastTile, dirtTrailTNorthTile, dirtTrailTSouthTile,
                dirtTrailCornerSWTile, dirtTrailCornerSETile, dirtTrailCornerNWTile, dirtTrailCornerNETile,
                dirtTrailEndWTile, dirtTrailEndETile, dirtTrailEndNTile, dirtTrailEndSTile
            );

            // 5. Cảnh quan Thiên Nhiên Thảo Nguyên (Cây Đại Thụ, Tảng Đá Rêu & Vạt Hoa Dã Quỳ)
            var natureGroup = new GameObject("Prairie_Nature");
            // Cây đại thụ che bóng mát trong bãi thả heo (đặt tại y = 11 để không đè lên lòng đường mòn y = 6)
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-5f, 11f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(15f, 9f, 0));

            // Cây đại thụ rải rác ngoài thảo nguyên hoang dã
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-32f, 18f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-28f, -6f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(28f, 18f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(32f, -18f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-10f, -24f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(14f, -24f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(8f, 24f, 0));
            CreatePrairieTree(natureGroup.transform, treeSp, new Vector3(-18f, 25f, 0));

            // Tảng đá rêu phong trong bãi thả & ngoài đồng cỏ
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(-8f, -6f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(5f, -6f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(-34f, 4f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(22f, 10f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(20f, -14f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(-24f, -24f, 0));
            CreatePrairieBoulder(natureGroup.transform, rockSp, new Vector3(34f, -6f, 0));

            // Bụi hoa dại nở rộ
            CreatePrairieFlowerPatch(natureGroup.transform, flowerPatchSp, new Vector3(-3f, 1f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerPatchSp, new Vector3(4f, 1f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerPatchSp, new Vector3(-25f, 10f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerPatchSp, new Vector3(24f, 12f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerPatchSp, new Vector3(-12f, -22f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerPatchSp, new Vector3(8f, -22f, 0));
            CreatePrairieFlowerPatch(natureGroup.transform, flowerPatchSp, new Vector3(0f, 22f, 0));

            // 6. Hàng Rào Chuồng Thả Heo (Bao quanh khu chăn thả 40m x 28m, có cổng rộng 6m phía nam)
            var fencesGroup = new GameObject("Pasture_Fences");
            CreateFenceSegment(fencesGroup.transform, fenceHSp, new Vector3(0, 14f, 0), new Vector3(40.8f, 0.8f, 1f), "Fence_Pasture_Top");
            CreateFenceSegment(fencesGroup.transform, fenceVSp, new Vector3(-20f, 0, 0), new Vector3(0.8f, 28.8f, 1f), "Fence_Pasture_Left");
            CreateFenceSegment(fencesGroup.transform, fenceVSp, new Vector3(20f, 0, 0), new Vector3(0.8f, 28.8f, 1f), "Fence_Pasture_Right");
            // Cổng rộng 6m phía nam (x từ -3 đến +3 để người chơi tự do ra vào thảo nguyên)
            CreateFenceSegment(fencesGroup.transform, fenceHSp, new Vector3(-11.5f, -14f, 0), new Vector3(17.2f, 0.8f, 1f), "Fence_Pasture_Bottom_Left");
            CreateFenceSegment(fencesGroup.transform, fenceHSp, new Vector3(11.5f, -14f, 0), new Vector3(17.2f, 0.8f, 1f), "Fence_Pasture_Bottom_Right");

            // Rào chắn vô hình tại cổng chỉ chặn heo, cho phép người chơi đi qua
            var gateObj = new GameObject("Pasture_Gate_Barrier");
            gateObj.transform.SetParent(fencesGroup.transform);
            gateObj.transform.position = new Vector3(0, -14f, 0);
            var gateCol = gateObj.AddComponent<BoxCollider2D>();
            gateCol.size = new Vector2(6.2f, 0.8f);
            gateCol.offset = Vector2.zero;
            gateObj.AddComponent<PigGateBarrier>();

            // 7. Mái Trú Thảo Nguyên (Nằm trong bãi thả heo góc Tây Bắc)
            var shelterObj = new GameObject("Shelter_Zone");
            shelterObj.transform.position = new Vector3(-13f, 8.5f, 0);
            var shelterSprite = shelterObj.AddComponent<SpriteRenderer>();
            shelterSprite.sprite = barnSp;
            shelterSprite.color = Color.white;
            shelterSprite.sortingOrder = Mathf.RoundToInt(-(shelterObj.transform.position.y - 1.28f) * 100);
            shelterObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            // Chân đế móng nhà 4x2 ô tiếp đất là vật cản cứng (Mái nhà phía trên không cản nhân vật)
            var shelterSolidCol = shelterObj.AddComponent<BoxCollider2D>();
            shelterSolidCol.size = new Vector2(3.8f, 1.0f);
            shelterSolidCol.offset = new Vector2(0f, -0.85f);
            shelterSolidCol.isTrigger = false;

            // Vùng mái hiên trước cửa chuồng (trigger để heo trú mưa / ngủ)
            var shelterTriggerCol = shelterObj.AddComponent<BoxCollider2D>();
            shelterTriggerCol.size = new Vector2(4.0f, 0.8f);
            shelterTriggerCol.offset = new Vector2(0f, -1.35f);
            shelterTriggerCol.isTrigger = true;
            shelterObj.AddComponent<Shelter2DView>();

            // 8. Máng Ăn Thảo Nguyên (Đặt tại bãi cỏ trung tâm trong chuồng thả)
            CreateFeeder(feederSp, new Vector3(-6f, 3f, 0), "Feeder_1");
            CreateFeeder(feederSp, new Vector3(2f, 5f, 0), "Feeder_2");

            // 9. Bến Nước (Water Troughs trong khu chuồng thả)
            CreateWaterTrough(waterTroughSp, new Vector3(7f, -1f, 0), "WaterTrough_1");
            CreateWaterTrough(waterTroughSp, new Vector3(12f, 4f, 0), "WaterTrough_2");

            // 10. Bãi Bùn Tắm Trong Chuồng Thả (Mud Pit Zone)
            var mudObj = new GameObject("MudPit_Zone");
            mudObj.transform.position = new Vector3(12f, -8f, 0);
            var mudCol = mudObj.AddComponent<BoxCollider2D>();
            mudCol.size = new Vector2(8f, 5f);
            mudCol.isTrigger = true;
            mudObj.AddComponent<MudPit2DView>();

            // 11. Khu Xử Lý Cách Ly (Corpse Lot - Góc xa Tây Nam ngoài thảo nguyên hoang dã)
            var corpseLotObj = new GameObject("CorpseLot_Zone");
            corpseLotObj.transform.position = new Vector3(-30f, -20f, 0);
            var corpseLotSprite = corpseLotObj.AddComponent<SpriteRenderer>();
            corpseLotSprite.sprite = corpseLotSp;
            corpseLotSprite.color = Color.white;
            corpseLotSprite.sortingOrder = Mathf.RoundToInt(-corpseLotObj.transform.position.y * 100);
            corpseLotObj.transform.localScale = new Vector3(5f, 3.5f, 1f);
            corpseLotObj.AddComponent<CorpseLot2DView>();

            // 12. Tháp Phòng Thủ (Đồi cao Đông Bắc nhìn bao quát toàn bộ thảo nguyên)
            var towerObj = new GameObject("DefenseTower (NoXuyenVan)");
            towerObj.transform.position = new Vector3(32f, 20f, 0);
            var towerSprite = towerObj.AddComponent<SpriteRenderer>();
            towerSprite.sprite = towerSp;
            towerSprite.color = Color.white;
            towerSprite.sortingOrder = Mathf.RoundToInt(-(towerObj.transform.position.y - 1.12f) * 100);
            towerObj.transform.localScale = new Vector3(2.4f, 3.2f, 1f);
            var towerCol = towerObj.AddComponent<CircleCollider2D>();
            towerCol.offset = new Vector2(0f, -0.35f);
            towerCol.radius = 0.4f;
            towerCol.isTrigger = false;
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
            playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerRb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var playerCol = playerObj.AddComponent<CircleCollider2D>();
            playerCol.offset = new Vector2(0f, -0.35f);
            playerCol.radius = 0.25f;

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
                pRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                pRb.interpolation = RigidbodyInterpolation2D.Interpolate;

                var pCol = pigObj.AddComponent<CircleCollider2D>();
                pCol.offset = new Vector2(0f, -0.15f);
                pCol.radius = 0.32f;

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
            if (sp == null)
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in allAssets)
                {
                    if (a is Sprite sprite)
                    {
                        sp = sprite;
                        break;
                    }
                }
            }
            return sp != null ? sp : fallback;
        }

        private static void CreateFenceSegment(Transform parent, Sprite sprite, Vector3 pos, Vector3 size, string name)
        {
            var fence = new GameObject(name);
            fence.transform.SetParent(parent);
            fence.transform.position = pos;

            var sr = fence.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(size.x, size.y);
            sr.tileMode = SpriteTileMode.Continuous;
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            var col = fence.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size.x, size.y);
            col.offset = Vector2.zero;

            fence.AddComponent<Fence2DView>();
        }

        private static void CreateFeeder(Sprite sprite, Vector3 pos, string name)
        {
            var feeder = new GameObject(name);
            feeder.transform.position = pos;
            feeder.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            var sr = feeder.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            var col = feeder.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 0.7f);
            col.offset = new Vector2(0f, -0.1f);
            col.isTrigger = false; // Vật cản cứng, không cho đi xuyên qua máng ăn

            feeder.AddComponent<Feeder2DView>();
        }

        private static void CreateWaterTrough(Sprite sprite, Vector3 pos, string name)
        {
            var trough = new GameObject(name);
            trough.transform.position = pos;
            trough.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            var sr = trough.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            var col = trough.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 0.7f);
            col.offset = new Vector2(0f, -0.1f);
            col.isTrigger = false; // Vật cản cứng, không cho đi xuyên qua bồn nước

            trough.AddComponent<WaterTrough2DView>();
        }

        private static void CreatePrairieTree(Transform parent, Sprite sprite, Vector3 pos)
        {
            var tree = new GameObject("Prairie_Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = pos;
            tree.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

            var sr = tree.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-(pos.y - 1.53f) * 100);

            var col = tree.AddComponent<CircleCollider2D>();
            col.offset = new Vector2(0f, -0.85f); // Đặt đúng tại gốc cây tiếp xúc mặt đất
            col.radius = 0.22f; // Bán kính thân cây vững chắc
            col.isTrigger = false;
        }

        private static void CreatePrairieBoulder(Transform parent, Sprite sprite, Vector3 pos)
        {
            var rock = new GameObject("Prairie_Rock");
            rock.transform.SetParent(parent);
            rock.transform.position = pos;
            rock.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            var sr = rock.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-(pos.y - 0.14f) * 100);

            var col = rock.AddComponent<CircleCollider2D>();
            col.offset = new Vector2(0f, -0.1f);
            col.radius = 0.35f;
            col.isTrigger = false;
        }

        private static void CreatePrairieFlowerPatch(Transform parent, Sprite sprite, Vector3 pos)
        {
            var patch = new GameObject("Flower_Bush");
            patch.transform.SetParent(parent);
            patch.transform.position = pos;
            patch.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            var sr = patch.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = -9600;
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
