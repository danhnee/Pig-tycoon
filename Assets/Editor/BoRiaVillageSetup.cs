using System;
using System.IO;
using System.Collections.Generic;
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
    public static class BoRiaVillageSetup
    {
        public const string VillageScenePath = "Assets/Scenes/BoRiaVillage2D.unity";
        public const string MainFarmScenePath = "Assets/Scenes/MainFarm2D.unity";
        public const string CombatArenaScenePath = "Assets/Scenes/CombatTestArena2D.unity";

        [MenuItem("PigTycoon/4. Tạo & Mở Map Làng Bờ Rìa (BoRiaVillage2D)")]
        public static void CreateAndOpenVillageScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "PigTycoon - Cảnh Báo",
                    "Unity đang ở chế độ Play Mode.\nVui lòng tắt nút Play trước khi tạo hoặc nạp Scene Làng Bờ Rìa!",
                    "Đã hiểu"
                );
                return;
            }

            Universal2DSetup.SetupUniversal2D();

            string dir = Path.GetDirectoryName(VillageScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite defaultSquare = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                Texture2D.whiteTexture.width
            );
            Sprite defaultKnob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // Nạp toàn bộ Sprite Pixel Art Chuyên Biệt Mới của Làng Bờ Rìa
            // 1. Công Trình (Buildings)
            Sprite bldgBaSau = LoadSprite("Assets/Art/Sprites/Environment/Village/building_ba_sau_hut.png", defaultSquare);
            Sprite bldgTamVa = LoadSprite("Assets/Art/Sprites/Environment/Village/building_tam_va_workshop.png", defaultSquare);
            Sprite bldgCoDung = LoadSprite("Assets/Art/Sprites/Environment/Village/building_co_dung_apothecary.png", defaultSquare);
            Sprite bldgScale = LoadSprite("Assets/Art/Sprites/Environment/Village/building_scale_station.png", defaultSquare);
            Sprite treeBanyan = LoadSprite("Assets/Art/Sprites/Environment/Village/nature_ancient_banyan_tree.png", defaultSquare);

            // 2. Đồ Đạc Trang Trí Chuyên Dụng (Props)
            Sprite propTeaTable = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_tea_table_set.png", defaultSquare);
            Sprite propInvertedBowl = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_inverted_bowl.png", defaultSquare);
            Sprite propWorkbench = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_carpenter_workbench.png", defaultSquare);
            Sprite propLumber = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_lumber_stack.png", defaultSquare);
            Sprite propDryingRack = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_herbal_drying_rack.png", defaultSquare);
            Sprite propMortar = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_herbal_mortar.png", defaultSquare);
            Sprite propScalePlatform = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_mechanical_scale_platform.png", defaultSquare);
            Sprite propForbiddenDial = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_forbidden_scale_dial.png", defaultSquare);
            Sprite propBlackLedgerDesk = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_black_ledger_desk.png", defaultSquare);
            Sprite propStoneShrine = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_stone_shrine.png", defaultSquare);
            Sprite propRunicMegalith = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_runic_megalith.png", defaultSquare);
            Sprite propNoticeBoard = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_village_notice_board.png", defaultSquare);
            Sprite propLanternPost = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_village_lantern_post.png", defaultSquare);
            Sprite propMudPuddle = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_mud_puddle.png", defaultSquare);
            Sprite tileStone = LoadSprite("Assets/Art/Sprites/Environment/Village/tile_village_stone.png", defaultSquare);

            // 3. Nhân Vật Dân Làng (Dedicated NPC Sprites)
            Sprite npcBaSau = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_ba_sau.png", defaultKnob);
            Sprite npcTamVa = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_tam_va.png", defaultKnob);
            Sprite npcCoDung = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_co_dung.png", defaultKnob);
            Sprite npcAnhCa = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_anh_ca.png", defaultKnob);
            Sprite npcOngTon = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_ong_ton.png", defaultKnob);
            Sprite npcBiNhan = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_bi_nhan.png", defaultKnob);

            // 4. Môi trường chung & Nhân vật người chơi Khoa
            Sprite treeSp = LoadSprite("Assets/Art/Sprites/Environment/Nature/tree_prairie.png", defaultKnob);
            Sprite rockSp = LoadSprite("Assets/Art/Sprites/Environment/Nature/rock_boulder.png", defaultKnob);
            Sprite grassBaseSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_base.png", defaultSquare);
            Sprite grassSteppeSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_steppe.png", grassBaseSp);
            Sprite grassTallSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_tall.png", grassBaseSp);
            Sprite dirtTrailSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail.png", defaultSquare);
            Sprite dummySp = LoadSprite("Assets/Art/Sprites/Environment/Training/training_dummy_straw.png", defaultKnob);
            Sprite wellSp = LoadSprite("Assets/Art/Sprites/Environment/Buildings/water_well.png", defaultSquare);
            Sprite fencePostSp = LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_post.png", defaultSquare);
            Sprite fenceBrokenSp = LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_broken.png", defaultSquare);
            Sprite vfxTractorBeamSp = LoadSprite("Assets/Art/Sprites/VFX/Telegraphs/vfx_tractor_beam.png", defaultKnob);

            Sprite pDownIdle = LoadSprite("Assets/Art/Sprites/Characters/Player/player_down_idle.png", defaultKnob);
            Sprite pDownWalk1 = LoadSprite("Assets/Art/Sprites/Characters/Player/player_down_walk1.png", pDownIdle);
            Sprite pDownWalk2 = LoadSprite("Assets/Art/Sprites/Characters/Player/player_down_walk2.png", pDownIdle);
            Sprite pUpIdle = LoadSprite("Assets/Art/Sprites/Characters/Player/player_up_idle.png", pDownIdle);
            Sprite pUpWalk1 = LoadSprite("Assets/Art/Sprites/Characters/Player/player_up_walk1.png", pUpIdle);
            Sprite pUpWalk2 = LoadSprite("Assets/Art/Sprites/Characters/Player/player_up_walk2.png", pUpIdle);
            Sprite pSideIdle = LoadSprite("Assets/Art/Sprites/Characters/Player/player_side_idle.png", pDownIdle);
            Sprite pSideWalk1 = LoadSprite("Assets/Art/Sprites/Characters/Player/player_side_walk1.png", pSideIdle);
            Sprite pSideWalk2 = LoadSprite("Assets/Art/Sprites/Characters/Player/player_side_walk2.png", pSideIdle);
            Sprite pAction = LoadSprite("Assets/Art/Sprites/Characters/Player/player_action.png", pDownIdle);

            // ==========================================
            // 1. MAIN CAMERA 2D (Sắc Thái Sương Lạnh Làng Bờ Rìa)
            // ==========================================
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.18f); // Sắc thái xanh xám sương lạnh
            camObj.transform.position = new Vector3(-12f, 0f, -10f);
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

            // ==========================================
            // 2. GLOBAL LIGHT 2D (Ánh Sáng U Ám Hoàng Hôn Sương Mù)
            // ==========================================
            var lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.68f, 0.74f, 0.85f); // Lam xám lạnh mờ sương
            light2D.intensity = 0.85f;

            // ==========================================
            // 3. GAME CONTROLLER (Singleton giữ dữ liệu tiền/heo/túi đồ)
            // ==========================================
            var gameControllerObj = new GameObject("GameController");
            gameControllerObj.AddComponent<MobileGameController>();

            // ==========================================
            // 4. MÔI TRƯỜNG LÀNG BỜ RÌA (Village_Environment_2D)
            // ==========================================
            var villageEnvRoot = new GameObject("Village_Environment_2D");

            // A. Nền Đất & Đường Lát Đá (Cobblestone Paths & Steppe Grass Ground)
            var groundRoot = new GameObject("Village_Ground_Grid");
            groundRoot.transform.SetParent(villageEnvRoot.transform, false);

            for (int x = -18; x <= 18; x++)
            {
                for (int y = -12; y <= 12; y++)
                {
                    var tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.SetParent(groundRoot.transform, false);
                    tileObj.transform.position = new Vector3(x, y, 0);

                    var sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -50;

                    // Mạng lưới đường đá cổ làng Bờ Rìa
                    bool isEastWestMainRoad = (y == 0 && x >= -14 && x <= 14);
                    bool isBaSauBranch = (x == -6 && y >= 0 && y <= 3);
                    bool isTamVaBranch = (x == 5 && y >= 0 && y <= 4);
                    bool isCoDungBranch = (x == -6 && y >= -5 && y <= 0);
                    bool isScaleBranch = (x == 5 && y >= -4 && y <= 0);
                    bool isCentralSquare = (Mathf.Abs(x) <= 2 && Mathf.Abs(y) <= 2);

                    if (isEastWestMainRoad || isBaSauBranch || isTamVaBranch || isCoDungBranch || isScaleBranch || isCentralSquare)
                    {
                        sr.sprite = tileStone;
                        sr.color = new Color(0.85f, 0.88f, 0.90f, 1f);
                    }
                    else
                    {
                        // Đồng đất sương lạnh Bờ Rìa
                        int pattern = Mathf.Abs((x * 31) ^ (y * 17)) % 100;
                        if (pattern < 20) sr.sprite = grassTallSp;
                        else if (pattern < 45) sr.sprite = grassSteppeSp;
                        else sr.sprite = grassBaseSp;

                        sr.color = new Color(0.70f, 0.76f, 0.70f, 1f); // Xám cỏ lạnh
                    }
                }
            }

            // Vũng bùn sình lầy sau mưa phùn
            CreateProp(villageEnvRoot.transform, "MudPuddle_1", propMudPuddle, new Vector3(-10f, -0.6f, 0f), Vector2.zero, Vector2.zero, -45);
            CreateProp(villageEnvRoot.transform, "MudPuddle_2", propMudPuddle, new Vector3(1.5f, -1.2f, 0f), Vector2.zero, Vector2.zero, -45);
            CreateProp(villageEnvRoot.transform, "MudPuddle_3", propMudPuddle, new Vector3(9f, 0.6f, 0f), Vector2.zero, Vector2.zero, -45);

            // B. Rừng Cây & Rào Dậu Bao Quanh Làng (Village Perimeter Enclosure)
            var forestRoot = new GameObject("Village_Perimeter_Forest");
            forestRoot.transform.SetParent(villageEnvRoot.transform, false);
            CreateVillagePerimeter(forestRoot.transform, treeSp, rockSp, fenceBrokenSp);

            // C. Cổng Tây Làng Bờ Rìa & Điểm Trở Về Nông Trại
            var gatewayRoot = new GameObject("Village_West_Gateway");
            gatewayRoot.transform.SetParent(villageEnvRoot.transform, false);
            CreateWestGateway(gatewayRoot.transform, fencePostSp, fenceBrokenSp, propLanternPost);

            // D. Quảng Trường Trung Tâm & Bảng Hương Ước
            var squareRoot = new GameObject("Village_Central_Square");
            squareRoot.transform.SetParent(villageEnvRoot.transform, false);
            CreateCentralSquare(squareRoot.transform, propNoticeBoard, propLanternPost);

            // ==========================================
            // 5. NĂM GIAN HÀNG / TRẠM DÂN LÀNG RIÊNG BIỆT (ZERO ĐỒ TẠM)
            // ==========================================
            var stationsRoot = new GameObject("Village_Stations");
            stationsRoot.transform.SetParent(villageEnvRoot.transform, false);

            // 1. QUÁN BÀ SÁU (Tây Bắc: x = -6, y = 4)
            CreateStationBaSau(stationsRoot.transform, new Vector3(-6f, 4f, 0f),
                bldgBaSau, propTeaTable, propInvertedBowl, propLanternPost, npcBaSau);

            // 2. XƯỞNG MỘC TÁM VÁ (Đông Bắc: x = 5, y = 5)
            CreateStationTamVa(stationsRoot.transform, new Vector3(5f, 5f, 0f),
                bldgTamVa, propWorkbench, propLumber, dummySp, npcTamVa);

            // 3. TIỆM THUỐC THÚ Y CÔ DUNG (Tây Nam: x = -6, y = -5)
            CreateStationCoDung(stationsRoot.transform, new Vector3(-6f, -5f, 0f),
                bldgCoDung, propDryingRack, propMortar, wellSp, npcCoDung);

            // 4. TRẠM CÂN 6+1 (Đông Nam: x = 5, y = -4)
            CreateStationAnhCa(stationsRoot.transform, new Vector3(5f, -4f, 0f),
                bldgScale, propScalePlatform, propForbiddenDial, propBlackLedgerDesk, npcAnhCa, npcOngTon);

            // 5. GỐC ĐA RANH GIỚI - BÍ NHÂN (Cực Đông: x = 13, y = 0)
            CreateStationBiNhan(stationsRoot.transform, new Vector3(13f, 0f, 0f),
                treeBanyan, propStoneShrine, propRunicMegalith, vfxTractorBeamSp, npcBiNhan);

            // ==========================================
            // 6. NHÂN VẬT NGƯỜI CHƠI KHOA (Player Spawn Point tại Cổng Tây)
            // ==========================================
            var playerObj = new GameObject("Player_Khoa");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(-12f, 0f, 0f);

            var playerSprite = playerObj.AddComponent<SpriteRenderer>();
            playerSprite.sprite = pDownIdle;
            playerSprite.sortingOrder = 0;

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var playerCol = playerObj.AddComponent<CircleCollider2D>();
            playerCol.offset = new Vector2(0f, -0.35f);
            playerCol.radius = 0.25f;

            var playerCtrl = playerObj.AddComponent<PlayerMobileController>();
            playerCtrl.SpriteRenderer = playerSprite;

            var charAnim = playerObj.AddComponent<CharacterSpriteAnimator>();
            charAnim.DownIdle = pDownIdle;
            charAnim.DownWalk = new[] { pDownWalk1, pDownIdle, pDownWalk2 };
            charAnim.UpIdle = pUpIdle;
            charAnim.UpWalk = new[] { pUpWalk1, pUpIdle, pUpWalk2 };
            charAnim.SideIdle = pSideIdle;
            charAnim.SideWalk = new[] { pSideWalk1, pSideIdle, pSideWalk2 };
            charAnim.ActionSprite = pAction;

            playerObj.AddComponent<WeaponSocketController>();
            playerObj.AddComponent<PlayerCombatController>();
            playerObj.AddComponent<PlayerInteractionController>();

            camFollow.Target = playerObj.transform;

            // ==========================================
            // 7. CANVAS UI LÀNG & HỆ THỐNG GIAO DỊCH (UI Canvas)
            // ==========================================
            var canvasObj = new GameObject("Village_UI_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Hệ thống Barter Giao dịch Dân Làng
            var barterPopup = canvasObj.AddComponent<VillageBarterPopup>();
            barterPopup.EnsureUIExists();

            // Hệ thống Chuyển Cảnh (Transition Controller)
            var transObj = new GameObject("TransitionZoneController");
            var transCtrl = transObj.AddComponent<TransitionZoneController>();
            transCtrl.EnsureUIExists();

            // Dev Mode Popup tiện kiểm thử
            var devMode = canvasObj.AddComponent<DevModePopup>();
            devMode.EnsureUIExists();

            // EventSystem cho UI
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // ==========================================
            // 8. LƯU SCENE & CẬP NHẬT EDITOR BUILD SETTINGS
            // ==========================================
            EditorSceneManager.SaveScene(scene, VillageScenePath);
            AssetDatabase.SaveAssets();

            // Đăng ký cả 3 scene vào Build Settings
            UpdateBuildSettings();

            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[PigTycoon] ĐÃ TẠO THÀNH CÔNG SCENE LÀNG BỜ RÌA ĐỘC LẬP TẠI: {VillageScenePath} VỚI 100% ART MỚI!</color>");
        }

        public static void UpdateBuildSettings()
        {
            var sceneList = new List<EditorBuildSettingsScene>();
            if (File.Exists(MainFarmScenePath))
                sceneList.Add(new EditorBuildSettingsScene(MainFarmScenePath, true));
            if (File.Exists(VillageScenePath))
                sceneList.Add(new EditorBuildSettingsScene(VillageScenePath, true));
            if (File.Exists(CombatArenaScenePath))
                sceneList.Add(new EditorBuildSettingsScene(CombatArenaScenePath, true));

            EditorBuildSettings.scenes = sceneList.ToArray();
            Debug.Log($"[PigTycoon] Đã đăng ký {sceneList.Count} Scene vào EditorBuildSettings: MainFarm2D, BoRiaVillage2D, CombatTestArena2D.");
        }

        // --- CÁC HÀM HỖ TRỢ DỰNG MÔI TRƯỜNG & TRẠM ---

        private static void CreateVillagePerimeter(Transform parent, Sprite treeSp, Sprite rockSp, Sprite brokenFenceSp)
        {
            Color treeTint = new Color(0.48f, 0.55f, 0.48f); // Tông xám lạnh sương mù Bờ Rìa
            Color rockTint = new Color(0.45f, 0.48f, 0.52f);

            // Bờ Bắc (y = 11.5)
            for (float x = -18f; x <= 18f; x += 3.2f)
            {
                CreateProp(parent, $"Tree_North_{x}", treeSp, new Vector3(x, 11.5f, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-11.5f * 100), treeTint, 1.4f);
            }

            // Bờ Nam (y = -11.5)
            for (float x = -18f; x <= 18f; x += 3.2f)
            {
                CreateProp(parent, $"Tree_South_{x}", treeSp, new Vector3(x, -11.5f, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt(11.5f * 100), treeTint, 1.4f);
            }

            // Bờ Đông (x = 18.0)
            for (float y = -10f; y <= 10f; y += 3.0f)
            {
                CreateProp(parent, $"Tree_East_{y}", treeSp, new Vector3(18f, y, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-y * 100), treeTint, 1.4f);
            }

            // Bờ Tây (x = -18.0, chừa cổng tại y in [-2, 2])
            for (float y = -10f; y <= 10f; y += 3.0f)
            {
                if (Mathf.Abs(y) < 2.5f) continue;
                CreateProp(parent, $"Tree_West_{y}", treeSp, new Vector3(-18f, y, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-y * 100), treeTint, 1.4f);
            }

            // Tảng đá rêu góc ranh giới
            CreateProp(parent, "Rock_NW", rockSp, new Vector3(-16.5f, 9.8f, 0f), new Vector2(1.2f, 0.8f), Vector2.zero, -980, rockTint, 1.3f);
            CreateProp(parent, "Rock_SW", rockSp, new Vector3(-16.5f, -9.8f, 0f), new Vector2(1.2f, 0.8f), Vector2.zero, 980, rockTint, 1.3f);
            CreateProp(parent, "Rock_NE", rockSp, new Vector3(16.5f, 9.8f, 0f), new Vector2(1.2f, 0.8f), Vector2.zero, -980, rockTint, 1.3f);
            CreateProp(parent, "Rock_SE", rockSp, new Vector3(16.5f, -9.8f, 0f), new Vector2(1.2f, 0.8f), Vector2.zero, 980, rockTint, 1.3f);
        }

        private static void CreateWestGateway(Transform parent, Sprite postSp, Sprite brokenFenceSp, Sprite lanternSp)
        {
            var tv = new GameObject("Trigger_Village_To_Farm");
            tv.transform.SetParent(parent, false);
            tv.transform.position = new Vector3(-14f, 0f, 0f);

            var colV = tv.AddComponent<BoxCollider2D>();
            colV.isTrigger = true;
            colV.size = new Vector2(4.5f, 3.2f);

            var lV = tv.AddComponent<TransitionTriggerListener>();
            lV.IsTriggerToVillage = false;

            // Cột cổng gỗ hai bên
            CreateProp(tv.transform, "Post_L", postSp, new Vector3(0f, 1.8f, 0f), Vector2.zero, Vector2.zero, 180, Color.white, 1.2f);
            CreateProp(tv.transform, "Post_R", postSp, new Vector3(0f, -1.8f, 0f), Vector2.zero, Vector2.zero, -180, Color.white, 1.2f);

            // Rào gãy cạnh cổng
            CreateProp(tv.transform, "Broken_L", brokenFenceSp, new Vector3(-1.2f, 2.5f, 0f), Vector2.zero, Vector2.zero, 250, new Color(0.7f, 0.65f, 0.6f), 1f);
            CreateProp(tv.transform, "Broken_R", brokenFenceSp, new Vector3(-1.2f, -2.5f, 0f), Vector2.zero, Vector2.zero, -250, new Color(0.7f, 0.65f, 0.6f), 1f);

            // Đèn lồng treo cổng
            CreateProp(tv.transform, "Gate_Lantern", lanternSp, new Vector3(-0.5f, 2.2f, 0f), Vector2.zero, Vector2.zero, 260, Color.white, 1f);

            // Biển gỗ chỉ dẫn
            CreateFloatingSignboard(tv.transform, "Sign_To_Farm", new Vector3(0f, 2.6f, 0f),
                "<b>ĐƯỜNG VỀ TRANG TRẠI</b>\n<size=70%><color=#d1d5db>Dốc Đất Đỏ Sương Mù</color></size>", new Color(0.65f, 0.50f, 0.38f));
        }

        private static void CreateCentralSquare(Transform parent, Sprite noticeBoardSp, Sprite lanternSp)
        {
            var squareObj = new GameObject("Village_Notice_Board");
            squareObj.transform.SetParent(parent, false);
            squareObj.transform.position = new Vector3(0f, 1.2f, 0f);

            CreateProp(squareObj.transform, "Board_Sprite", noticeBoardSp, Vector3.zero, new Vector2(1.4f, 0.8f), new Vector2(0f, -0.4f), -120, Color.white, 1.2f);

            CreateFloatingSignboard(squareObj.transform, "Sign_Rules", new Vector3(0f, 1.8f, 0f),
                "<b>HƯƠNG ƯỚC LÀNG BỜ RÌA</b>\n<color=#fca5a5><size=65%>1. CẤM SỐ 7\n2. BÁT ÚP CHỚ LẬT\n3. TUYỆT ĐỐI KHÔNG NHÌN LÊN TRỜI</size></color>",
                new Color(0.65f, 0.45f, 0.35f), 3.4f, 1.5f);

            // Trụ đèn lồng bão cạnh ngã tư quảng trường
            CreateProp(parent, "Crossroads_Lantern", lanternSp, new Vector3(-1.8f, -0.6f, 0f), Vector2.zero, Vector2.zero, 60, Color.white, 1.1f);
        }

        // --- 5 TRẠM DÂN LÀNG ---

        private static void CreateStationBaSau(Transform parent, Vector3 pos,
            Sprite bldgSp, Sprite tableSp, Sprite bowlSp, Sprite lanternSp, Sprite npcSp)
        {
            var stationObj = new GameObject("Station_BaSau");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Gian nhà tranh quán nước
            CreateProp(stationObj.transform, "Building_Hut", bldgSp, Vector3.zero, new Vector2(2.8f, 1.5f), new Vector2(0f, -0.3f), Mathf.RoundToInt(-pos.y * 100), Color.white, 1f);

            // Bàn trà mộc kèm ấm chén sứ
            var tableObj = CreateProp(stationObj.transform, "Table_TeaSet", tableSp, new Vector3(0f, -1.6f, 0f), new Vector2(1.5f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 1.6f) * 100), Color.white, 1.1f);

            // Chiếc Bát Sứt Úp Ngược (Taboo #2) đặt trang trọng trên bàn
            CreateProp(tableObj.transform, "Prop_Inverted_Bowl", bowlSp, new Vector3(0.4f, 0.2f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.6f) * 100) + 10, Color.white, 0.9f);

            // Trụ đèn lồng bão treo hiên quán
            CreateProp(stationObj.transform, "Lantern_Post", lanternSp, new Vector3(-2.2f, -0.8f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 0.8f) * 100) + 5, Color.white, 1f);

            // NPC Bà Sáu
            CreateProp(stationObj.transform, "NPC_BaSau", npcSp, new Vector3(-1.2f, -1.5f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.5f) * 100), Color.white, 1f);

            // Biển hiệu
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.3f, 0f),
                "<b>Quán Bà Sáu</b>\n<size=70%><color=#fbbf24>Nước Chè & Cháo Nóng</color></size>", new Color(0.72f, 0.55f, 0.38f));

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(4.5f, 3.8f);
            trigCol.offset = new Vector2(0f, -0.8f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.BaSau;
            trig.StationTitle = "Quán Bà Sáu";
        }

        private static void CreateStationTamVa(Transform parent, Vector3 pos,
            Sprite bldgSp, Sprite benchSp, Sprite lumberSp, Sprite dummySp, Sprite npcSp)
        {
            var stationObj = new GameObject("Station_TamVa");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Xưởng mộc
            CreateProp(stationObj.transform, "Building_Workshop", bldgSp, Vector3.zero, new Vector2(3.0f, 1.5f), new Vector2(0f, -0.3f), Mathf.RoundToInt(-pos.y * 100), Color.white, 1f);

            // Bàn thợ mộc cưa xẻ gỗ
            CreateProp(stationObj.transform, "Workbench", benchSp, new Vector3(0f, -1.6f, 0f), new Vector2(1.5f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 1.6f) * 100), Color.white, 1.1f);

            // Đống gỗ xẻ & cọc rào gia cố
            CreateProp(stationObj.transform, "Lumber_Stack", lumberSp, new Vector3(2.6f, -0.5f, 0f), new Vector2(1.4f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 0.5f) * 100), Color.white, 1.1f);

            // Bù nhìn thử nghiệm độ bền rào
            CreateProp(stationObj.transform, "Dummy", dummySp, new Vector3(-2.2f, -1.2f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.2f) * 100), Color.white, 1f);

            // NPC Tám Vá
            CreateProp(stationObj.transform, "NPC_TamVa", npcSp, new Vector3(-1.1f, -1.5f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.5f) * 100), Color.white, 1f);

            // Biển hiệu
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.3f, 0f),
                "<b>Xưởng Mộc Tám Vá</b>\n<size=70%><color=#93c5fd>Vá Rào Chống Quái (Gỗ Xẻ)</color></size>", new Color(0.60f, 0.45f, 0.32f));

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(4.8f, 3.8f);
            trigCol.offset = new Vector2(0f, -0.8f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.TamVa;
            trig.StationTitle = "Xưởng Mộc Tám Vá";
        }

        private static void CreateStationCoDung(Transform parent, Vector3 pos,
            Sprite bldgSp, Sprite dryingRackSp, Sprite mortarSp, Sprite wellSp, Sprite npcSp)
        {
            var stationObj = new GameObject("Station_CoDung");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Gian tiệm thuốc
            CreateProp(stationObj.transform, "Building_Apothecary", bldgSp, Vector3.zero, new Vector2(2.8f, 1.5f), new Vector2(0f, -0.3f), Mathf.RoundToInt(-pos.y * 100), Color.white, 1f);

            // Giàn phơi nia thuốc thảo dược
            CreateProp(stationObj.transform, "Drying_Rack", dryingRackSp, new Vector3(-2.4f, -0.6f, 0f), new Vector2(1.4f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 0.6f) * 100), Color.white, 1.1f);

            // Cối đá giã thuốc thú y
            CreateProp(stationObj.transform, "Herbal_Mortar", mortarSp, new Vector3(1.8f, -1.2f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.2f) * 100), Color.white, 1f);

            // Giếng đá rửa dược liệu
            CreateProp(stationObj.transform, "Well", wellSp, new Vector3(-2.5f, 1.4f, 0f), new Vector2(1.1f, 0.7f), new Vector2(0f, -0.3f), Mathf.RoundToInt((-pos.y - 1.4f) * 100), Color.white, 1.1f);

            // NPC Cô Dung
            CreateProp(stationObj.transform, "NPC_CoDung", npcSp, new Vector3(0.2f, -1.3f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.3f) * 100), Color.white, 1f);

            // Biển hiệu
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.3f, 0f),
                "<b>Tiệm Thuốc Cô Dung</b>\n<size=70%><color=#86efac>Bột Kháng Dịch & An Thần</color></size>", new Color(0.45f, 0.55f, 0.42f));

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(4.8f, 3.8f);
            trigCol.offset = new Vector2(0f, -0.8f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.CoDung;
            trig.StationTitle = "Tiệm Thuốc Cô Dung";
        }

        private static void CreateStationAnhCa(Transform parent, Vector3 pos,
            Sprite bldgSp, Sprite platformSp, Sprite dialSp, Sprite deskSp, Sprite npcAnhCa, Sprite npcOngTon)
        {
            var stationObj = new GameObject("Station_AnhCa");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Cổng trạm cân gỗ & chuồng nhốt heo
            CreateProp(stationObj.transform, "Building_ScaleGate", bldgSp, Vector3.zero, new Vector2(3.0f, 1.5f), new Vector2(0f, -0.3f), Mathf.RoundToInt(-pos.y * 100), Color.white, 1f);

            // Bàn cân cơ học sàn sắt cho heo
            CreateProp(stationObj.transform, "Scale_Platform", platformSp, new Vector3(-0.2f, -1.6f, 0f), new Vector2(1.5f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 1.6f) * 100), Color.white, 1.1f);

            // Bảng đồng hồ vạch cân cấm số 7 rùng rợn
            CreateProp(stationObj.transform, "Forbidden_Dial", dialSp, new Vector3(-1.8f, -1.3f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.3f) * 100) + 10, Color.white, 1f);

            // Bàn gỗ ghi chép Sổ Đen của Ông Tồn
            CreateProp(stationObj.transform, "Ledger_Desk", deskSp, new Vector3(2.2f, -1.5f, 0f), new Vector2(1.4f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 1.5f) * 100), Color.white, 1.1f);

            // NPC Anh Ca (Lực lưỡng)
            CreateProp(stationObj.transform, "NPC_AnhCa", npcAnhCa, new Vector3(-1.8f, -1.8f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.8f) * 100), Color.white, 1f);

            // NPC Ông Tồn (Lão làng giữ sổ)
            CreateProp(stationObj.transform, "NPC_OngTon", npcOngTon, new Vector3(2.4f, -1.9f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.9f) * 100), Color.white, 1f);

            // Biển hiệu
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.3f, 0f),
                "<b>Trạm Cân 6+1</b>\n<size=70%><color=#f87171>Anh Ca & Ông Tồn [CẤM SỐ 7]</color></size>", new Color(0.62f, 0.45f, 0.35f));

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(5.2f, 4.0f);
            trigCol.offset = new Vector2(0f, -0.8f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.AnhCa;
            trig.StationTitle = "Trạm Cân 6+1";
        }

        private static void CreateStationBiNhan(Transform parent, Vector3 pos,
            Sprite treeSp, Sprite shrineSp, Sprite megalithSp, Sprite auraSp, Sprite npcSp)
        {
            var stationObj = new GameObject("Station_BiNhan");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Cây Đa Cổ Thụ Ranh Giới Khổng Lồ
            var treeObj = CreateProp(stationObj.transform, "Ancient_Banyan_Tree", treeSp, Vector3.zero, Vector2.zero, Vector2.zero, Mathf.RoundToInt(-pos.y * 100), Color.white, 1.3f);
            var treeCol = treeObj.AddComponent<CircleCollider2D>();
            treeCol.radius = 0.65f;
            treeCol.offset = new Vector2(0f, -1.1f);

            // Miếu Thờ Đá Rêu Phong
            CreateProp(stationObj.transform, "Stone_Shrine", shrineSp, new Vector3(-2.2f, -1.5f, 0f), new Vector2(1.2f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 1.5f) * 100), Color.white, 1.1f);

            // Bia Cự Thạch Khắc Phù Chú Ranh Giới
            CreateProp(stationObj.transform, "Runic_Megalith", megalithSp, new Vector3(2.2f, 0.5f, 0f), new Vector2(1.0f, 1.2f), Vector2.zero, Mathf.RoundToInt((-pos.y - 0.5f) * 100), Color.white, 1.1f);

            // Luồng Hào Quang Dị Biến Tím Huyền Bí
            CreateProp(stationObj.transform, "Mystic_Aura", auraSp, new Vector3(0f, -0.5f, 0f), Vector2.zero, Vector2.zero, -20, new Color(0.65f, 0.25f, 0.95f, 0.35f), 1.8f);

            // NPC Bí Nhân
            CreateProp(stationObj.transform, "NPC_BiNhan", npcSp, new Vector3(-0.4f, -1.5f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.5f) * 100), Color.white, 1f);

            // Biển hiệu
            CreateFloatingSignboard(stationObj.transform, "Signboard", new Vector3(0f, 2.5f, 0f),
                "<b>Gốc Đa Ranh Giới</b>\n<size=70%><color=#c084fc>Bí Nhân [Vùng Cấm 25m]</color></size>", new Color(0.40f, 0.30f, 0.50f));

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(5.5f, 4.2f);
            trigCol.offset = new Vector2(0f, -0.8f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.BiNhan;
            trig.StationTitle = "Gốc Đa Ranh Giới";
        }

        private static GameObject CreateProp(Transform parent, string name, Sprite sprite, Vector3 localPos,
            Vector2 colSize, Vector2 colOffset, int sortOrder, Color tint = default, float scale = 1f)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = Vector3.one * scale;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortOrder;
            sr.color = (tint == default) ? Color.white : tint;

            if (colSize.sqrMagnitude > 0.001f)
            {
                var col = obj.AddComponent<BoxCollider2D>();
                col.size = colSize;
                col.offset = colOffset;
            }

            return obj;
        }

        private static void CreateFloatingSignboard(Transform parent, string name, Vector3 localPos, string text, Color frameColor, float width = 2.8f, float height = 1.2f)
        {
            var signObj = new GameObject(name);
            signObj.transform.SetParent(parent, false);
            signObj.transform.localPosition = localPos;

            var bg = signObj.AddComponent<SpriteRenderer>();
            bg.sprite = LoadSprite("Assets/Art/UI/Frames/frame_wood_panel.png", null);
            bg.color = frameColor;
            bg.sortingOrder = 200;
            signObj.transform.localScale = new Vector3(width * 0.35f, height * 0.45f, 1f);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(signObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.05f);

            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 3.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 205;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(width * 2.8f, height * 2.2f);
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
                var dyn = UISpriteLoader.LoadSprite(path);
                if (dyn != null) return dyn;
                return fallback;
            }
            return sp;
        }
    }
}
