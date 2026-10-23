using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;
using PigTycoon.Presentation;

namespace PigTycoon.EditorTools
{
    /// <summary>
    /// Công cụ Dựng & Mở Bản Đồ Làng Bờ Rìa Mở Rộng (BoRiaVillage2D Setup):
    /// - Nhà dân làng thiết kế lại 100% theo phong cách nhà mái rơm đá cuội Stardew Valley.
    /// - Bản đồ mở rộng gấp hơn 4.6 lần (81x53 ô gạch, x: -40..40, y: -26..26).
    /// - Hệ thống đường di chuyển uốn lượn hữu cơ, kết hợp đá cuội, lối mòn đất sỏi và hoa dại.
    /// - Dòng suối Thôn Giáp uốn lượn với 3 cây cầu đá/gỗ bắc qua làng.
    /// - Quảng trường trung tâm rộng lớn với Giếng Làng cổ, Bảng Hương Ước và cột đèn bão.
    /// </summary>
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

            // ==========================================
            // 1. TẢI TÀI NGUYÊN NGHỆ THUẬT PIXEL ART
            // ==========================================
            // A. Công Trình Mái Rơm Đá Cuội Mới Thiết Kế
            Sprite bldgBaSau = LoadSprite("Assets/Art/Sprites/Environment/Village/building_ba_sau_hut.png", defaultSquare);
            Sprite bldgTamVa = LoadSprite("Assets/Art/Sprites/Environment/Village/building_tam_va_workshop.png", defaultSquare);
            Sprite bldgCoDung = LoadSprite("Assets/Art/Sprites/Environment/Village/building_co_dung_apothecary.png", defaultSquare);
            Sprite bldgScale = LoadSprite("Assets/Art/Sprites/Environment/Village/building_scale_station.png", defaultSquare);
            Sprite bldgCottage = LoadSprite("Assets/Art/Sprites/Environment/Village/building_village_cottage.png", defaultSquare);
            Sprite treeBanyan = LoadSprite("Assets/Art/Sprites/Environment/Village/nature_ancient_banyan_tree.png", defaultSquare);

            // B. Đồ Đạc Trang Trí Chuyên Dụng (Props)
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
            Sprite propBenchSp = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_village_bench.png", defaultSquare);

            // C. Bộ Block Tilemap Đồng Cỏ (Grass Tiles)
            Sprite grassBaseSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_base.png", defaultSquare);
            Sprite grassSteppeSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_steppe.png", grassBaseSp);
            Sprite grassTallSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_tall.png", grassBaseSp);
            Sprite flower1Sp = LoadSprite("Assets/Art/Tiles/Sprites/grass_flower_1.png", grassBaseSp);
            Sprite flower2Sp = LoadSprite("Assets/Art/Tiles/Sprites/grass_flower_2.png", grassBaseSp);
            Sprite flower3Sp = LoadSprite("Assets/Art/Tiles/Sprites/grass_flower_3.png", grassBaseSp);

            // D. Bộ Block Sông & Bờ Nước (Water & Riverbank Tiles)
            Sprite waterDeepSp = LoadSprite("Assets/Art/Tiles/Sprites/water_deep.png", defaultSquare);
            Sprite waterShallowSp = LoadSprite("Assets/Art/Tiles/Sprites/water_shallow.png", waterDeepSp);
            Sprite waterBankWSp = LoadSprite("Assets/Art/Tiles/Sprites/water_bank_w.png", waterDeepSp);
            Sprite waterBankESp = LoadSprite("Assets/Art/Tiles/Sprites/water_bank_e.png", waterDeepSp);
            Sprite waterBankNSp = LoadSprite("Assets/Art/Tiles/Sprites/water_bank_n.png", waterDeepSp);
            Sprite waterBankSSp = LoadSprite("Assets/Art/Tiles/Sprites/water_bank_s.png", waterDeepSp);

            // E. Bộ Block Đường Đá Cuội Sỏi (Stone Path Autotiles)
            Sprite stoneHSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_h.png", defaultSquare);
            Sprite stoneVSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_v.png", stoneHSp);
            Sprite stoneCrossSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_cross.png", stoneHSp);
            Sprite stoneTNSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_t_north.png", stoneHSp);
            Sprite stoneTSSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_t_south.png", stoneHSp);
            Sprite stoneTWSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_t_west.png", stoneHSp);
            Sprite stoneTESp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_t_east.png", stoneHSp);
            Sprite stoneCNWSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_corner_nw.png", stoneHSp);
            Sprite stoneCNESp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_corner_ne.png", stoneHSp);
            Sprite stoneCSWSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_corner_sw.png", stoneHSp);
            Sprite stoneCSESp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_corner_se.png", stoneHSp);
            Sprite stoneCenterSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_center.png", stoneHSp);
            Sprite stoneEdgeNSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_edge_n.png", stoneHSp);
            Sprite stoneEdgeSSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_edge_s.png", stoneHSp);
            Sprite stoneEdgeWSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_edge_w.png", stoneHSp);
            Sprite stoneEdgeESp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_edge_e.png", stoneHSp);
            Sprite stoneEdgeNOpenSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_edge_n_open.png", stoneHSp);
            Sprite stoneEdgeSOpenSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_edge_s_open.png", stoneHSp);
            Sprite stoneEdgeWOpenSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_edge_w_open.png", stoneHSp);
            Sprite stoneEdgeEOpenSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_edge_e_open.png", stoneHSp);

            // F. Bộ Block Đường Mòn Đất Sỏi (Dirt Trail Autotiles)
            Sprite dirtHSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_h.png", defaultSquare);
            Sprite dirtVSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_v.png", dirtHSp);
            Sprite dirtCrossSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_cross.png", dirtHSp);
            Sprite dirtTNSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_t_north.png", dirtHSp);
            Sprite dirtTSSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_t_south.png", dirtHSp);
            Sprite dirtTWSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_t_west.png", dirtHSp);
            Sprite dirtTESp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_t_east.png", dirtHSp);
            Sprite dirtCNWSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_corner_nw.png", dirtHSp);
            Sprite dirtCNESp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_corner_ne.png", dirtHSp);
            Sprite dirtCSWSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_corner_sw.png", dirtHSp);
            Sprite dirtCSESp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_corner_se.png", dirtHSp);
            Sprite dirtCenterSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_center.png", dirtHSp);
            Sprite dirtEdgeNSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_edge_n.png", dirtHSp);
            Sprite dirtEdgeSSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_edge_s.png", dirtHSp);
            Sprite dirtEdgeWSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_edge_w.png", dirtHSp);
            Sprite dirtEdgeESp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_edge_e.png", dirtHSp);

            // G. Bộ Block Chéo Liền Mạch (Diagonal Road Blocks 45 độ - Không bậc thang)
            Sprite dirtDiagNETopSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_diag_ne_top.png", dirtHSp);
            Sprite dirtDiagNEBotSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_diag_ne_bot.png", dirtHSp);
            Sprite dirtDiagNESp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_diag_ne.png", dirtHSp);
            Sprite dirtDiagNWTopSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_diag_nw_top.png", dirtHSp);
            Sprite dirtDiagNWBotSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_diag_nw_bot.png", dirtHSp);
            Sprite dirtDiagNWSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail_diag_nw.png", dirtHSp);

            Sprite stoneDiagNETopSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_diag_ne_top.png", stoneHSp);
            Sprite stoneDiagNEBotSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_diag_ne_bot.png", stoneHSp);
            Sprite stoneDiagNESp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_diag_ne.png", stoneHSp);
            Sprite stoneDiagNWTopSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_diag_nw_top.png", stoneHSp);
            Sprite stoneDiagNWBotSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_diag_nw_bot.png", stoneHSp);
            Sprite stoneDiagNWSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path_diag_nw.png", stoneHSp);

            // G. Cầu Gỗ Đá Thôn Giáp (Bridge Sprites)
            Sprite bridgeVillageFullSp = LoadSprite("Assets/Art/Sprites/Environment/Village/bridge_village_wood_stone.png", defaultSquare);
            Sprite bridgeVillageRailSp = LoadSprite("Assets/Art/Sprites/Environment/Village/bridge_village_wood_stone_rail.png", defaultSquare);
            Sprite bridgeDeckMidSp = LoadSprite("Assets/Art/Tiles/Sprites/bridge_deck_mid.png", defaultSquare);
            Sprite bridgeAbutWSp = LoadSprite("Assets/Art/Tiles/Sprites/bridge_abutment_w.png", defaultSquare);
            Sprite bridgeAbutESp = LoadSprite("Assets/Art/Tiles/Sprites/bridge_abutment_e.png", defaultSquare);
            Sprite bridgeRailBackSp = LoadSprite("Assets/Art/Tiles/Sprites/bridge_rail_back.png", defaultSquare);
            Sprite bridgeRailFrontSp = LoadSprite("Assets/Art/Tiles/Sprites/bridge_rail_front.png", defaultSquare);

            // C. Nhân Vật Dân Làng (NPCs)
            Sprite npcBaSau = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_ba_sau.png", defaultKnob);
            Sprite npcTamVa = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_tam_va.png", defaultKnob);
            Sprite npcCoDung = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_co_dung.png", defaultKnob);
            Sprite npcAnhCa = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_anh_ca.png", defaultKnob);
            Sprite npcOngTon = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_ong_ton.png", defaultKnob);
            Sprite npcBiNhan = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_bi_nhan.png", defaultKnob);

            // D. Môi Trường Tự Nhiên, Cây Cối & Đạo Cụ Phong Phú
            Sprite treeSp = LoadSprite("Assets/Art/Sprites/Environment/Nature/tree_prairie.png", defaultKnob);
            Sprite treeWillowSp = LoadSprite("Assets/Art/Sprites/Environment/Village/tree_willow_river.png", treeSp);
            Sprite treeFruitSp = LoadSprite("Assets/Art/Sprites/Environment/Village/tree_village_fruit.png", treeSp);
            Sprite treePineSp = LoadSprite("Assets/Art/Sprites/Environment/Village/tree_pine_conifer.png", treeSp);
            Sprite propHayBaleSp = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_hay_bale.png", defaultSquare);
            Sprite propBarrelSp = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_wooden_barrel.png", defaultSquare);
            Sprite propFlowerBushSp = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_flower_bush.png", flower1Sp);
            Sprite flowerPatchSp = LoadSprite("Assets/Art/Sprites/Environment/Nature/flower_patch.png", defaultSquare);
            Sprite rockSp = LoadSprite("Assets/Art/Sprites/Environment/Nature/rock_boulder.png", defaultKnob);
            Sprite dummySp = LoadSprite("Assets/Art/Sprites/Environment/Training/training_dummy_straw.png", defaultKnob);
            Sprite wellSp = LoadSprite("Assets/Art/Sprites/Environment/Buildings/water_well.png", defaultSquare);
            Sprite fencePostSp = LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_post.png", defaultSquare);
            Sprite fenceBrokenSp = LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_broken.png", defaultSquare);
            Sprite fenceHSp = LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_h.png", defaultSquare);
            Sprite vfxTractorBeamSp = LoadSprite("Assets/Art/Sprites/VFX/Telegraphs/vfx_tractor_beam.png", defaultKnob);

            // E. Nhân Vật Người Chơi Khoa
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
            // 2. MAIN CAMERA 2D (Tầm Nhìn Rộng Bao Quát Làng Bờ Rìa)
            // ==========================================
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.18f); // Sắc thái xanh xám sương mù
            camObj.transform.position = new Vector3(-32f, 0f, -10f);
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var camFollow = camObj.AddComponent<CameraFollow2D>();
            camFollow.TargetOrthoSize = 5.5f;
            camFollow.MinOrthoSize = 3.8f;
            camFollow.MaxOrthoSize = 11.0f;
            camFollow.SmoothSpeed = 6.0f;
            camFollow.ClampToFarmBounds = true;

            // ==========================================
            // 3. GLOBAL LIGHT 2D (Hoàng Hôn Sương Lạnh Lãng Mạn)
            // ==========================================
            var lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.75f, 0.78f, 0.88f); // Ánh sương mù thanh bình
            light2D.intensity = 0.90f;

            // ==========================================
            // 4. GAME CONTROLLER
            // ==========================================
            var gameControllerObj = new GameObject("GameController");
            gameControllerObj.AddComponent<MobileGameController>();

            // ==========================================
            // 5. HỆ THỐNG GRID & TILEMAP LÀNG BỜ RÌA (81 x 53 BLOCK TILES)
            // ==========================================
            var villageEnvRoot = new GameObject("Village_Environment_2D");

            var gridObj = new GameObject("Village_Grid");
            gridObj.transform.SetParent(villageEnvRoot.transform, false);
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            // Layer 1: Đồng Cỏ Nền (Sorting Order: -10000)
            var groundTilemapObj = new GameObject("Tilemap_Ground");
            groundTilemapObj.transform.SetParent(gridObj.transform, false);
            var groundTilemap = groundTilemapObj.AddComponent<Tilemap>();
            var groundRenderer = groundTilemapObj.AddComponent<TilemapRenderer>();
            groundRenderer.sortingOrder = -10000;

            // Layer 2: Dòng Sông Thôn Giáp (Sorting Order: -9800) + TilemapCollider2D
            var waterTilemapObj = new GameObject("Tilemap_Water");
            waterTilemapObj.transform.SetParent(gridObj.transform, false);
            var waterTilemap = waterTilemapObj.AddComponent<Tilemap>();
            var waterRenderer = waterTilemapObj.AddComponent<TilemapRenderer>();
            waterRenderer.sortingOrder = -9800;
            var waterCol = waterTilemapObj.AddComponent<TilemapCollider2D>();

            // Layer 3: Mạng Lưới Đường Đi Đá & Đất (Sorting Order: -9900)
            var pathTilemapObj = new GameObject("Tilemap_Paths");
            pathTilemapObj.transform.SetParent(gridObj.transform, false);
            var pathTilemap = pathTilemapObj.AddComponent<Tilemap>();
            var pathRenderer = pathTilemapObj.AddComponent<TilemapRenderer>();
            pathRenderer.sortingOrder = -9900;

            // Tạo các đối tượng Tile ScriptableObject
            Tile tGrassBase = CreateTile(grassBaseSp);
            Tile tGrassSteppe = CreateTile(grassSteppeSp);
            Tile tGrassTall = CreateTile(grassTallSp);
            Tile tFlower1 = CreateTile(flower1Sp);
            Tile tFlower2 = CreateTile(flower2Sp);
            Tile tFlower3 = CreateTile(flower3Sp);

            Tile tWaterDeep = CreateTile(waterDeepSp, Tile.ColliderType.Grid);
            Tile tWaterShallow = CreateTile(waterShallowSp, Tile.ColliderType.Grid);
            Tile tWaterBankW = CreateTile(waterBankWSp, Tile.ColliderType.Grid);
            Tile tWaterBankE = CreateTile(waterBankESp, Tile.ColliderType.Grid);
            Tile tWaterUnderBridge = CreateTile(waterDeepSp, Tile.ColliderType.None); // Dưới cầu: Không cản bước

            Tile tStoneH = CreateTile(stoneHSp);
            Tile tStoneV = CreateTile(stoneVSp);
            Tile tStoneCross = CreateTile(stoneCrossSp);
            Tile tStoneTN = CreateTile(stoneTNSp);
            Tile tStoneTS = CreateTile(stoneTSSp);
            Tile tStoneTW = CreateTile(stoneTWSp);
            Tile tStoneTE = CreateTile(stoneTESp);
            Tile tStoneCNW = CreateTile(stoneCNWSp);
            Tile tStoneCNE = CreateTile(stoneCNESp);
            Tile tStoneCSW = CreateTile(stoneCSWSp);
            Tile tStoneCSE = CreateTile(stoneCSESp);
            Tile tStoneCenter = CreateTile(stoneCenterSp);
            Tile tStoneEN = CreateTile(stoneEdgeNSp);
            Tile tStoneES = CreateTile(stoneEdgeSSp);
            Tile tStoneEW = CreateTile(stoneEdgeWSp);
            Tile tStoneEE = CreateTile(stoneEdgeESp);
            Tile tStoneENOpen = CreateTile(stoneEdgeNOpenSp);
            Tile tStoneESOpen = CreateTile(stoneEdgeSOpenSp);
            Tile tStoneEWOpen = CreateTile(stoneEdgeWOpenSp);
            Tile tStoneEEOpen = CreateTile(stoneEdgeEOpenSp);

            Tile tDirtH = CreateTile(dirtHSp);
            Tile tDirtV = CreateTile(dirtVSp);
            Tile tDirtCross = CreateTile(dirtCrossSp);
            Tile tDirtTN = CreateTile(dirtTNSp);
            Tile tDirtTS = CreateTile(dirtTSSp);
            Tile tDirtTW = CreateTile(dirtTWSp);
            Tile tDirtTE = CreateTile(dirtTESp);
            Tile tDirtCNW = CreateTile(dirtCNWSp);
            Tile tDirtCNE = CreateTile(dirtCNESp);
            Tile tDirtCSW = CreateTile(dirtCSWSp);
            Tile tDirtCSE = CreateTile(dirtCSESp);
            Tile tDirtCenter = CreateTile(dirtCenterSp);
            Tile tDirtEN = CreateTile(dirtEdgeNSp);
            Tile tDirtES = CreateTile(dirtEdgeSSp);
            Tile tDirtEW = CreateTile(dirtEdgeWSp);
            Tile tDirtEE = CreateTile(dirtEdgeESp);

            Tile tDirtDiagNETop = CreateTile(dirtDiagNETopSp);
            Tile tDirtDiagNEBot = CreateTile(dirtDiagNEBotSp);
            Tile tDirtDiagNE = CreateTile(dirtDiagNESp);
            Tile tDirtDiagNWTop = CreateTile(dirtDiagNWTopSp);
            Tile tDirtDiagNWBot = CreateTile(dirtDiagNWBotSp);
            Tile tDirtDiagNW = CreateTile(dirtDiagNWSp);

            Tile tStoneDiagNETop = CreateTile(stoneDiagNETopSp);
            Tile tStoneDiagNEBot = CreateTile(stoneDiagNEBotSp);
            Tile tStoneDiagNE = CreateTile(stoneDiagNESp);
            Tile tStoneDiagNWTop = CreateTile(stoneDiagNWTopSp);
            Tile tStoneDiagNWBot = CreateTile(stoneDiagNWBotSp);
            Tile tStoneDiagNW = CreateTile(stoneDiagNWSp);

            // 1. Sinh thảm cỏ tự nhiên trọn bản đồ 81x53 ô (Tilemap_Ground)
            for (int x = -40; x <= 40; x++)
            {
                for (int y = -26; y <= 26; y++)
                {
                    int p = Mathf.Abs((x * 37) ^ (y * 23)) % 100;
                    Tile gt = tGrassBase;
                    if (p < 15) gt = tFlower1;
                    else if (p < 25) gt = tFlower2;
                    else if (p < 35) gt = tFlower3;
                    else if (p < 55) gt = tGrassTall;
                    else if (p < 75) gt = tGrassSteppe;
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), gt);
                }
            }

            // 2. Dòng sông Thôn Giáp uốn lượn hữu cơ (Tilemap_Water)
            var waterCells = new HashSet<Vector2Int>();
            for (int y = -26; y <= 26; y++)
            {
                float rCenter = -24.0f + Mathf.Sin(y * 0.13f) * 2.0f;
                int xMin = Mathf.RoundToInt(rCenter - 3f);
                int xMax = Mathf.RoundToInt(rCenter + 3f);
                for (int x = xMin; x <= xMax; x++)
                {
                    waterCells.Add(new Vector2Int(x, y));
                }
            }

            foreach (var pos in waterCells)
            {
                int wx = pos.x;
                int wy = pos.y;
                var cellPos = new Vector3Int(wx, wy, 0);

                // Khu vực Cầu Gỗ bắc qua sông (x: -26..-22, y: -1..1) -> Không cản bước
                if (Mathf.Abs(wy) <= 1 && wx >= -26 && wx <= -22)
                {
                    waterTilemap.SetTile(cellPos, tWaterUnderBridge);
                    continue;
                }

                float rCenter = -24.0f + Mathf.Sin(wy * 0.13f) * 2.0f;
                float dist = Mathf.Abs(wx - rCenter);

                bool isWBank = !waterCells.Contains(new Vector2Int(wx - 1, wy));
                bool isEBank = !waterCells.Contains(new Vector2Int(wx + 1, wy));

                Tile wt = tWaterShallow;
                if (isWBank && !isEBank) wt = tWaterBankW;
                else if (isEBank && !isWBank) wt = tWaterBankE;
                else if (dist <= 1.5f) wt = tWaterDeep;

                waterTilemap.SetTile(cellPos, wt);
            }

            // 3. Khởi tạo danh sách tọa độ Đường Đá và Đường Đất (Hệ thống đường 2-block rộng thoáng)
            var stoneCells = new HashSet<Vector3Int>();
            var dirtCells = new HashSet<Vector3Int>();
            var dirtDiagNETopCells = new HashSet<Vector3Int>();
            var dirtDiagNEBotCells = new HashSet<Vector3Int>();
            var dirtDiagNWTopCells = new HashSet<Vector3Int>();
            var dirtDiagNWBotCells = new HashSet<Vector3Int>();

            // A. Đại lộ chính lát đá: từ đầu cầu x = -20 đến mép quảng trường x = -4 tại y = 0 và y = -1 (rộng 2 block)
            for (int x = -20; x <= -4; x++)
            {
                stoneCells.Add(new Vector3Int(x, 0, 0));
                stoneCells.Add(new Vector3Int(x, -1, 0));
            }

            // B. Quảng trường trung tâm lát đá: x: -3..3, y: -3..3 (rộng 7x7)
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    stoneCells.Add(new Vector3Int(x, y, 0));
                }
            }

            // C. Trục Bắc lát đá lên Nhà Trưởng Thôn: x = -1 và 0, y = 4..15 (rộng 2 block)
            for (int y = 4; y <= 15; y++)
            {
                stoneCells.Add(new Vector3Int(0, y, 0));
                stoneCells.Add(new Vector3Int(-1, y, 0));
            }

            // D. Trục Nam lát đá xuống Nhà Dân Làng: x = -1 và 0, y = -15..-4 (rộng 2 block)
            for (int y = -15; y <= -4; y++)
            {
                stoneCells.Add(new Vector3Int(0, y, 0));
                stoneCells.Add(new Vector3Int(-1, y, 0));
            }

            // E. Đường mòn đất Cổng Tây vào làng: x: -36..-27, y = 0 và y = -1 (rộng 2 block)
            for (int x = -36; x <= -27; x++)
            {
                dirtCells.Add(new Vector3Int(x, 0, 0));
                dirtCells.Add(new Vector3Int(x, -1, 0));
            }

            // F. Trục đường Thôn Tây: x = -17 và -16, y: -15..15 (nối Quán Bà Sáu và Tiệm Cô Dung, rộng 2 block)
            for (int y = -15; y <= 15; y++)
            {
                dirtCells.Add(new Vector3Int(-17, y, 0));
                dirtCells.Add(new Vector3Int(-16, y, 0));
            }

            // G. Đại lộ Thôn Đông sang Gốc Đa Bí Nhân: x = 4..34, y = 0 và y = -1 (rộng 2 block)
            for (int x = 4; x <= 34; x++)
            {
                dirtCells.Add(new Vector3Int(x, 0, 0));
                dirtCells.Add(new Vector3Int(x, -1, 0));
            }

            // H. Trục đường Thôn Đông: x = 17 và 18, y: -15..15 (nối Xưởng Mộc Tám Vá và Trạm Cân Anh Cả, rộng 2 block đối xứng Thôn Tây)
            for (int y = -15; y <= 15; y++)
            {
                dirtCells.Add(new Vector3Int(17, y, 0));
                dirtCells.Add(new Vector3Int(18, y, 0));
            }

            // Sân thềm Quán Nước Bà Sáu (x: -19..-14, y: 12..15)
            for (int x = -19; x <= -14; x++)
            {
                for (int y = 12; y <= 15; y++)
                {
                    dirtCells.Add(new Vector3Int(x, y, 0));
                }
            }

            // Sân thềm Tiệm Thuốc Cô Dung (x: -19..-14, y: -15..-12)
            for (int x = -19; x <= -14; x++)
            {
                for (int y = -15; y <= -12; y++)
                {
                    dirtCells.Add(new Vector3Int(x, y, 0));
                }
            }

            // Sân thềm Xưởng Mộc Tám Vá (x: 15..20, y: 12..15)
            for (int x = 15; x <= 20; x++)
            {
                for (int y = 12; y <= 15; y++)
                {
                    dirtCells.Add(new Vector3Int(x, y, 0));
                }
            }

            // Sân bãi Trạm Cân Anh Cả (x: 15..20, y: -15..-12)
            for (int x = 15; x <= 20; x++)
            {
                for (int y = -15; y <= -12; y++)
                {
                    dirtCells.Add(new Vector3Int(x, y, 0));
                }
            }

            // Sân thềm trước Nhà Trưởng Thôn (x: -2..1, y: 14..16)
            for (int x = -2; x <= 1; x++)
            {
                for (int y = 14; y <= 16; y++)
                {
                    stoneCells.Add(new Vector3Int(x, y, 0));
                }
            }

            // Sân thềm trước Nhà Dân Làng (x: -2..1, y: -16..-14)
            for (int x = -2; x <= 1; x++)
            {
                for (int y = -16; y <= -14; y++)
                {
                    stoneCells.Add(new Vector3Int(x, y, 0));
                }
            }

            // Khuôn viên đền cổ Gốc Đa Ranh Giới (x: 32..36, y: -2..1)
            for (int x = 32; x <= 36; x++)
            {
                for (int y = -2; y <= 1; y++)
                {
                    dirtCells.Add(new Vector3Int(x, y, 0));
                }
            }

            // Tự động kết nối Auto-Tile cho toàn bộ hệ thống đường
            BuildVillageTilemapPaths(pathTilemap, stoneCells, dirtCells,
                dirtDiagNETopCells, dirtDiagNEBotCells, dirtDiagNWTopCells, dirtDiagNWBotCells,
                tStoneH, tStoneV, tStoneCross, tStoneTN, tStoneTS, tStoneTW, tStoneTE,
                tStoneCNW, tStoneCNE, tStoneCSW, tStoneCSE,
                tStoneCenter, tStoneEN, tStoneES, tStoneEW, tStoneEE,
                tStoneENOpen, tStoneESOpen, tStoneEWOpen, tStoneEEOpen,
                tDirtH, tDirtV, tDirtCross, tDirtTN, tDirtTS, tDirtTW, tDirtTE,
                tDirtCNW, tDirtCNE, tDirtCSW, tDirtCSE,
                tDirtCenter, tDirtEN, tDirtES, tDirtEW, tDirtEE,
                tDirtDiagNETop, tDirtDiagNEBot, tDirtDiagNWTop, tDirtDiagNWBot);

            // ==========================================
            // 6. CẦU GỖ ĐÁ THÔN GIÁP (BẮC QUA SÔNG TẠI X = -24, RỘNG 9x3 Ô GẠCH)
            // ==========================================
            var bridgeRoot = new GameObject("Village_Main_Bridge");
            bridgeRoot.transform.SetParent(villageEnvRoot.transform, false);
            bridgeRoot.transform.position = new Vector3(-24f, 0f, 0f);

            // A. Thân cầu gỗ, mặt sàn ván gỗ và mố đá 2 đầu (Sorting Order: -20, nằm sau người chơi)
            CreateProp(bridgeRoot.transform, "Bridge_Main_Structure", bridgeVillageFullSp, Vector3.zero, Vector2.zero, Vector2.zero, -20);

            // B. Lan can phía Nam (Sorting Order: 250, nằm trước người chơi trên mặt cầu tạo chiều sâu 2.5D chính xác)
            CreateProp(bridgeRoot.transform, "Bridge_Front_Railing", bridgeVillageRailSp, Vector3.zero, Vector2.zero, Vector2.zero, 250);

            // C. BoxCollider chặn 2 bên lan can Bắc và Nam (người chơi không rơi xuống nước)
            var bridgeColObj = new GameObject("Bridge_Rail_Colliders");
            bridgeColObj.transform.SetParent(bridgeRoot.transform, false);
            bridgeColObj.transform.position = new Vector3(-24f, 0f, 0f);

            var railN = bridgeColObj.AddComponent<BoxCollider2D>();
            railN.size = new Vector2(7.2f, 0.4f);
            railN.offset = new Vector2(0f, 0.95f);

            var railS = bridgeColObj.AddComponent<BoxCollider2D>();
            railS.size = new Vector2(7.2f, 0.4f);
            railS.offset = new Vector2(0f, -0.95f);

            // Đèn lồng bão ở hai đầu mố cầu đá (đặt chuẩn tâm ô)
            CreateProp(bridgeRoot.transform, "Bridge_Lantern_West", propLanternPost, new Vector3(-4.5f, 1.5f, 0f), new Vector2(0.35f, 0.35f), new Vector2(0f, -0.55f), ComputeYSortingOrder(1.5f, FootOffset_Lantern, 1.0f), Color.white, 1.0f);
            CreateProp(bridgeRoot.transform, "Bridge_Lantern_East", propLanternPost, new Vector3(4.5f, 1.5f, 0f), new Vector2(0.35f, 0.35f), new Vector2(0f, -0.55f), ComputeYSortingOrder(1.5f, FootOffset_Lantern, 1.0f), Color.white, 1.0f);

            // ==========================================
            // 7. RỪNG CÂY & RÀO DẬU BAO QUANH LÀNG (Perimeter)
            // ==========================================
            var forestRoot = new GameObject("Village_Perimeter_Forest");
            forestRoot.transform.SetParent(villageEnvRoot.transform, false);
            CreateExpandedVillagePerimeter(forestRoot.transform, treeSp, rockSp, fenceBrokenSp);

            // ==========================================
            // 7B. HỆ THỐNG CẢNH QUAN, RỪNG CÂY, VƯỜN QUẢ & TIỂU CẢNH TOÀN LÀNG
            // ==========================================
            var landscapeRoot = new GameObject("Village_Rich_Landscape");
            landscapeRoot.transform.SetParent(villageEnvRoot.transform, false);
            DecorateRichVillageLandscape(landscapeRoot.transform,
                treeWillowSp, treeFruitSp, treePineSp, treeSp,
                propFlowerBushSp, flowerPatchSp, rockSp,
                propHayBaleSp, propBarrelSp, propBenchSp, propLanternPost);

            // ==========================================
            // 8. CỔNG TÂY LÀNG BỜ RÌA & ĐIỂM TRỞ VỀ NÔNG TRẠI
            // ==========================================
            var gatewayRoot = new GameObject("Village_West_Gateway");
            gatewayRoot.transform.SetParent(villageEnvRoot.transform, false);
            CreateWestGateway(gatewayRoot.transform, new Vector3(-36.5f, 0.5f, 0f), fencePostSp, fenceBrokenSp, propLanternPost);

            // ==========================================
            // 9. QUẢNG TRƯỜNG TRUNG TÂM (Central Square)
            // ==========================================
            var squareRoot = new GameObject("Village_Central_Square");
            squareRoot.transform.SetParent(villageEnvRoot.transform, false);
            CreateCentralSquare(squareRoot.transform, Vector3.zero, propNoticeBoard, propLanternPost, wellSp, propBenchSp);

            // ==========================================
            // 10. NĂM GIAN HÀNG / TRẠM DÂN LÀNG RIÊNG BIỆT (ĐẶT CHUẨN Ô & ĐỘ SÂU Y-SORT)
            // ==========================================
            var stationsRoot = new GameObject("Village_Stations");
            stationsRoot.transform.SetParent(villageEnvRoot.transform, false);

            // 1. QUÁN NƯỚC BÀ SÁU (Thôn Tây Bắc: Đầu trục đường Thôn Tây, x = -16.5, y = 17.5)
            CreateStationBaSau(stationsRoot.transform, new Vector3(-16.5f, 17.5f, 0f),
                bldgBaSau, propTeaTable, propInvertedBowl, propLanternPost, npcBaSau, fenceHSp);

            // 2. XƯỞNG MỘC TÁM VÁ (Thôn Đông Bắc: Đầu trục đường Thôn Đông, x = 17.5, y = 17.5)
            CreateStationTamVa(stationsRoot.transform, new Vector3(17.5f, 17.5f, 0f),
                bldgTamVa, propWorkbench, propLumber, dummySp, npcTamVa, fenceHSp);

            // 3. TIỆM THUỐC THÚ Y CÔ DUNG (Thôn Tây Nam: Cuối trục đường Thôn Tây, x = -16.5, y = -17.5)
            CreateStationCoDung(stationsRoot.transform, new Vector3(-16.5f, -17.5f, 0f),
                bldgCoDung, propDryingRack, propMortar, wellSp, npcCoDung, fenceHSp);

            // 4. TRẠM CÂN 6+1 (Thôn Đông Nam: Cuối trục đường Thôn Đông, x = 17.5, y = -17.5)
            CreateStationAnhCa(stationsRoot.transform, new Vector3(17.5f, -17.5f, 0f),
                bldgScale, propScalePlatform, propForbiddenDial, propBlackLedgerDesk, npcAnhCa, npcOngTon, fenceHSp);

            // 5. GỐC ĐA RANH GIỚI - BÍ NHÂN (Cực Đông Rừng Cấm: x = 35.5, y = 0.5)
            CreateStationBiNhan(stationsRoot.transform, new Vector3(35.5f, 0.5f, 0f),
                treeBanyan, propStoneShrine, propRunicMegalith, vfxTractorBeamSp, npcBiNhan, treeSp);

            // 6. NHÀ DÂN LÀNG PHỤ (North Cottage & South Cottage - Đặt đầu trục Bắc và Nam)
            CreateVillageCottage(stationsRoot.transform, "Elder_Cottage", new Vector3(-0.5f, 18.5f, 0f),
                bldgCottage, "Nhà Trưởng Thôn Bờ Rìa", "Gõ cửa nhà Trưởng thôn", fenceHSp, faceSouth: true);
            CreateVillageCottage(stationsRoot.transform, "Villager_Cottage", new Vector3(-0.5f, -18.5f, 0f),
                bldgCottage, "Nhà Dân Làng Bờ Rìa", "Xem nhà dân xóm", fenceHSp, faceSouth: false);

            // ==========================================
            // 11. NHÂN VẬT NGƯỜI CHƠI KHOA (Player Spawn Point tại Cổng Tây)
            // ==========================================
            var playerObj = new GameObject("Player_Khoa");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(-32f, 0f, 0f);

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
            // 12. CANVAS UI LÀNG & HỆ THỐNG GIAO DỊCH
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
            // 13. LƯU SCENE & CẬP NHẬT EDITOR BUILD SETTINGS
            // ==========================================
            EditorSceneManager.SaveScene(scene, VillageScenePath);
            AssetDatabase.SaveAssets();

            UpdateBuildSettings();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[PigTycoon] ĐÃ TẠO TOÀN DIỆN MAP LÀNG BỜ RÌA MỞ RỘNG (81x53 Ô GẠCH) TẠI: {VillageScenePath} VỚI MẪU NHÀ MÁI RƠM ĐÁ CUỘI MỚI!</color>");
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

        // --- CÁC HẰNG SỐ ĐỘ SÂU TIẾP ĐẤT (Y-SORTING GROUND FOOT OFFSETS) ---
        // Chuẩn hóa theo trục tiếp đất thực tế của Sprite 32 PPU (tương thích tuyệt đối với PlayerMobileController: footY = y - 0.65f)
        public const float FootOffset_Tree = -1.20f;
        public const float FootOffset_Banyan = -1.94f;
        public const float FootOffset_Building = -2.20f;
        public const float FootOffset_Lantern = -0.75f;
        public const float FootOffset_Bench = -0.50f;
        public const float FootOffset_PropSmall = -0.47f;
        public const float FootOffset_Table = -0.50f;
        public const float FootOffset_Shrine = -0.75f;
        public const float FootOffset_Well = -0.70f;
        public const float FootOffset_NoticeBoard = -0.75f;
        public const float FootOffset_NPC = -0.75f;
        public const float FootOffset_Rock = -0.50f;
        public const float FootOffset_FlowerBush = -0.40f;
        public const float FootOffset_Fence = -0.40f;

        public static int ComputeYSortingOrder(float worldY, float footLocalOffset, float scale = 1f)
        {
            float footWorldY = worldY + (footLocalOffset * scale);
            return Mathf.RoundToInt(-footWorldY * 100);
        }

        public static float ResolveFootOffsetByName(string objName)
        {
            if (string.IsNullOrEmpty(objName)) return FootOffset_PropSmall;
            if (objName.Contains("Tree") || objName.Contains("Pine") || objName.Contains("Willow") || objName.Contains("Fruit")) return FootOffset_Tree;
            if (objName.Contains("Banyan")) return FootOffset_Banyan;
            if (objName.Contains("Lantern") || objName.Contains("Post")) return FootOffset_Lantern;
            if (objName.Contains("Bench")) return FootOffset_Bench;
            if (objName.Contains("Table") || objName.Contains("Workbench") || objName.Contains("Lumber") || objName.Contains("Drying") || objName.Contains("Scale") || objName.Contains("Ledger")) return FootOffset_Table;
            if (objName.Contains("NPC")) return FootOffset_NPC;
            if (objName.Contains("Well")) return FootOffset_Well;
            if (objName.Contains("Notice_Board") || objName.Contains("Board")) return FootOffset_NoticeBoard;
            if (objName.Contains("Shrine") || objName.Contains("Megalith")) return FootOffset_Shrine;
            if (objName.Contains("Rock") || objName.Contains("Boulder")) return FootOffset_Rock;
            if (objName.Contains("Flower") || objName.Contains("Bush")) return FootOffset_FlowerBush;
            if (objName.Contains("Fence") || objName.Contains("Broken")) return FootOffset_Fence;
            if (objName.Contains("Building") || objName.Contains("Hut") || objName.Contains("Cottage")) return FootOffset_Building;
            return FootOffset_PropSmall;
        }

        [MenuItem("PigTycoon/6. Tự Động Căn Lưới & Cập Nhật Sorting Order (Auto-Snap & Sort)", false, 60)]
        public static void AutoSnapAndSortVillageObjects()
        {
            int modifiedCount = 0;
            var villageEnv = GameObject.Find("Village_Environment_2D");
            if (villageEnv == null)
            {
                EditorUtility.DisplayDialog("PigTycoon", "Không tìm thấy GameObject 'Village_Environment_2D' trong Scene hiện tại. Vui lòng mở Scene BoRiaVillage2D!", "Đã hiểu");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(villageEnv, "Auto-Snap & Sort Village");

            // 1. Quản lý các Station và Cottage
            var stationsRoot = villageEnv.transform.Find("Village_Stations");
            if (stationsRoot != null)
            {
                for (int i = 0; i < stationsRoot.childCount; i++)
                {
                    var station = stationsRoot.GetChild(i);
                    Vector3 pos = station.position;
                    station.position = new Vector3(Mathf.Floor(pos.x) + 0.5f, Mathf.Floor(pos.y) + 0.5f, 0f);
                    modifiedCount++;

                    var renderers = station.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var sr in renderers)
                    {
                        var customBuilding = sr.GetComponent<CustomBuildingController>();
                        if (customBuilding != null)
                        {
                            float baseY = customBuilding.transform.position.y + customBuilding.DoorLocalOffset.y;
                            sr.sortingOrder = Mathf.RoundToInt(-baseY * customBuilding.SortingPrecision) + customBuilding.SortingOffset;
                        }
                        else
                        {
                            float offset = ResolveFootOffsetByName(sr.gameObject.name);
                            sr.sortingOrder = ComputeYSortingOrder(sr.transform.position.y, offset, sr.transform.localScale.y);
                        }
                        modifiedCount++;
                    }
                }
            }

            // 2. Quản lý Landscape & Props
            string[] groupNames = { "Village_Rich_Landscape", "Village_Perimeter_Forest", "Village_Central_Square", "Village_West_Gateway" };
            foreach (var gName in groupNames)
            {
                var grp = villageEnv.transform.Find(gName);
                if (grp == null) continue;

                var renderers = grp.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in renderers)
                {
                    Vector3 p = sr.transform.position;
                    sr.transform.position = new Vector3(Mathf.Floor(p.x) + 0.5f, Mathf.Floor(p.y) + 0.5f, 0f);

                    float offset = ResolveFootOffsetByName(sr.gameObject.name);
                    sr.sortingOrder = ComputeYSortingOrder(sr.transform.position.y, offset, sr.transform.localScale.y);
                    modifiedCount++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "PigTycoon - Căn Lưới & Độ Sâu 2.5D",
                $"Đã hoàn tất tự động căn lề theo ô lưới (Grid Snapping) và cập nhật Sorting Order chuẩn xác cho {modifiedCount} đối tượng trên bản đồ Làng Bờ Rìa!",
                "Tuyệt vời"
            );
        }

        // --- CÁC HÀM HỖ TRỢ DỰNG MÔI TRƯỜNG & TRẠM ĐẶT THEO Ô ---

        private static void CreateExpandedVillagePerimeter(Transform parent, Sprite treeSp, Sprite rockSp, Sprite brokenFenceSp)
        {
            Color treeTint = new Color(0.48f, 0.55f, 0.48f); // Tông xám lạnh sương mù Bờ Rìa
            Color rockTint = new Color(0.45f, 0.48f, 0.52f);
            Vector2 treeColSize = new Vector2(0.6f, 0.45f);
            Vector2 treeColOffset = new Vector2(0f, -0.95f);

            // Bờ Bắc (y = 25.5f) - Đặt theo bước 3 ô chuẩn lưới gạch
            for (int x = -39; x <= 39; x += 3)
            {
                Vector3 pos = new Vector3(x + 0.5f, 25.5f, 0f);
                CreateProp(parent, $"Tree_North_{x}", treeSp, pos, treeColSize, treeColOffset,
                    ComputeYSortingOrder(pos.y, FootOffset_Tree, 1.4f), treeTint, 1.4f);
            }

            // Bờ Nam (y = -25.5f) - Đặt theo bước 3 ô chuẩn lưới gạch
            for (int x = -39; x <= 39; x += 3)
            {
                Vector3 pos = new Vector3(x + 0.5f, -25.5f, 0f);
                CreateProp(parent, $"Tree_South_{x}", treeSp, pos, treeColSize, treeColOffset,
                    ComputeYSortingOrder(pos.y, FootOffset_Tree, 1.4f), treeTint, 1.4f);
            }

            // Bờ Đông Rừng Cấm (x = 39.5f)
            for (int y = -25; y <= 25; y += 3)
            {
                Vector3 pos = new Vector3(39.5f, y + 0.5f, 0f);
                CreateProp(parent, $"Tree_East_{y}", treeSp, pos, treeColSize, treeColOffset,
                    ComputeYSortingOrder(pos.y, FootOffset_Tree, 1.4f), treeTint, 1.4f);
            }

            // Bờ Tây (x = -39.5f) - Chừa lối Cổng Tây (y trong khoảng -3 đến 3)
            for (int y = -25; y <= 25; y += 3)
            {
                if (Mathf.Abs(y) < 3) continue;
                Vector3 pos = new Vector3(-39.5f, y + 0.5f, 0f);
                CreateProp(parent, $"Tree_West_{y}", treeSp, pos, treeColSize, treeColOffset,
                    ComputeYSortingOrder(pos.y, FootOffset_Tree, 1.4f), treeTint, 1.4f);
            }

            // Đá cuội phong rêu bờ rìa đặt chuẩn theo ô
            int[] rockX = { -35, -29, -23, -17, -11, -5, 5, 11, 17, 23, 29, 35, -35, -25, -15, 15, 25, 35 };
            int[] rockY = { 23, 24, 23, 24, 23, 24, 24, 23, 24, 23, 24, 23, -23, -24, -23, -23, -24, -23 };
            for (int i = 0; i < rockX.Length; i++)
            {
                Vector3 rPos = new Vector3(rockX[i] + 0.5f, rockY[i] + 0.5f, 0f);
                CreateProp(parent, $"Perimeter_Rock_{i}", rockSp, rPos, new Vector2(1.0f, 0.5f), new Vector2(0f, -0.2f),
                    ComputeYSortingOrder(rPos.y, FootOffset_Rock, 1.1f), rockTint, 1.1f);
            }
        }

        private static void CreateWestGateway(Transform parent, Vector3 pos, Sprite postSp, Sprite brokenFenceSp, Sprite lanternSp)
        {
            var tv = new GameObject("Trigger_Village_To_Farm");
            tv.transform.SetParent(parent, false);
            tv.transform.position = pos;

            var colV = tv.AddComponent<BoxCollider2D>();
            colV.isTrigger = true;
            colV.size = new Vector2(4.0f, 3.0f);

            var lV = tv.AddComponent<TransitionTriggerListener>();
            lV.IsTriggerToVillage = false;

            // Cột cổng gỗ hai bên (đặt chuẩn ô ngoài lề đường y: -1..0)
            CreateProp(tv.transform, "Post_L", postSp, new Vector3(0f, 1.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero,
                ComputeYSortingOrder(pos.y + 1.5f, FootOffset_Fence, 1.3f), Color.white, 1.3f);
            CreateProp(tv.transform, "Post_R", postSp, new Vector3(0f, -1.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero,
                ComputeYSortingOrder(pos.y - 1.5f, FootOffset_Fence, 1.3f), Color.white, 1.3f);

            // Rào dậu cạnh cổng
            CreateProp(tv.transform, "Broken_L", brokenFenceSp, new Vector3(-1.0f, 2.0f, 0f), new Vector2(1.0f, 0.5f), Vector2.zero,
                ComputeYSortingOrder(pos.y + 2.0f, FootOffset_Fence, 1.1f), new Color(0.7f, 0.65f, 0.6f), 1.1f);
            CreateProp(tv.transform, "Broken_R", brokenFenceSp, new Vector3(-1.0f, -2.0f, 0f), new Vector2(1.0f, 0.5f), Vector2.zero,
                ComputeYSortingOrder(pos.y - 2.0f, FootOffset_Fence, 1.1f), new Color(0.7f, 0.65f, 0.6f), 1.1f);

            // Đèn lồng treo cổng
            CreateProp(tv.transform, "Gate_Lantern", lanternSp, new Vector3(0f, 2.0f, 0f), new Vector2(0.35f, 0.35f), new Vector2(0f, -0.55f),
                ComputeYSortingOrder(pos.y + 2.0f, FootOffset_Lantern, 1.1f), Color.white, 1.1f);
        }

        private static void CreateCentralSquare(Transform parent, Vector3 pos, Sprite noticeBoardSp, Sprite lanternSp, Sprite wellSp, Sprite benchSp)
        {
            var squareObj = new GameObject("Central_Square_Elements");
            squareObj.transform.SetParent(parent, false);
            squareObj.transform.position = pos;

            // 1. Giếng Làng Cổ Kính ở chính giữa Quảng Trường (đặt chuẩn tâm ô x: 0.5, y: 0.5)
            Vector3 wellPos = new Vector3(0.5f, 0.5f, 0f);
            var wellObj = CreateProp(squareObj.transform, "Ancient_Village_Well", wellSp, wellPos,
                new Vector2(1.4f, 0.8f), new Vector2(0f, -0.3f),
                ComputeYSortingOrder(wellPos.y, FootOffset_Well, 1.2f), Color.white, 1.2f);
            wellObj.AddComponent<WaterWell2DView>();

            // 2. Bảng Hương Ước Làng Bờ Rìa (Phía Bắc Giếng Làng: x: 0.5, y: 3.5)
            Vector3 boardPos = new Vector3(0.5f, 3.5f, 0f);
            var boardParent = new GameObject("Village_Notice_Board");
            boardParent.transform.SetParent(squareObj.transform, false);
            boardParent.transform.position = boardPos;

            var boardObj = CreateProp(boardParent.transform, "Board_Sprite", noticeBoardSp, Vector3.zero,
                new Vector2(1.4f, 0.8f), new Vector2(0f, -0.3f),
                ComputeYSortingOrder(boardPos.y, FootOffset_NoticeBoard, 1.2f), Color.white, 1.2f);
            var cb = boardObj.AddComponent<CustomBuildingController>();
            cb.BuildingId = "village_notice_board";
            cb.BuildingDisplayName = "Bảng Hương Ước Làng Bờ Rìa";
            cb.BuildingRenderer = boardObj.GetComponent<SpriteRenderer>();
            cb.DoorLocalOffset = new Vector2(0f, -0.7f);
            cb.InteractionRadius = 2.0f;
            cb.ActionPromptText = "Đọc Bảng Hương Ước";
            cb.AssignedFunction = BuildingInteractionFunction.DocBangTinLore;
            cb.LoreTitle = "HƯƠNG ƯỚC LÀNG BỜ RÌA (KHẮC GỖ CỔ)";
            cb.LoreContent = "1. TUYỆT ĐỐI CẤM SỐ 7: Không mua bán, không cân đong số 7.\n2. BÁT ÚP CHỚ LẬT: Bát sứt trên bàn thờ đá phải luôn úp miệng xuống đất để phong ấn khí lạnh.\n3. TRỜI TỐI KHÔNG NHÌN LÊN: Sau canh ba, nếu nghe tiếng rít gió trên nóc nhà, tuyệt đối nhắm mắt nằm im.";

            // 3. Bốn Cột Đèn Lồng Bão tại 4 góc quảng trường (đặt chuẩn tâm ô)
            Vector3 lanternNW = new Vector3(-2.5f, 3.5f, 0f);
            Vector3 lanternNE = new Vector3(3.5f, 3.5f, 0f);
            Vector3 lanternSW = new Vector3(-2.5f, -2.5f, 0f);
            Vector3 lanternSE = new Vector3(3.5f, -2.5f, 0f);

            CreateProp(squareObj.transform, "Square_Lantern_NW", lanternSp, lanternNW,
                new Vector2(0.35f, 0.35f), new Vector2(0f, -0.55f),
                ComputeYSortingOrder(lanternNW.y, FootOffset_Lantern, 1.15f), Color.white, 1.15f);
            CreateProp(squareObj.transform, "Square_Lantern_NE", lanternSp, lanternNE,
                new Vector2(0.35f, 0.35f), new Vector2(0f, -0.55f),
                ComputeYSortingOrder(lanternNE.y, FootOffset_Lantern, 1.15f), Color.white, 1.15f);
            CreateProp(squareObj.transform, "Square_Lantern_SW", lanternSp, lanternSW,
                new Vector2(0.35f, 0.35f), new Vector2(0f, -0.55f),
                ComputeYSortingOrder(lanternSW.y, FootOffset_Lantern, 1.15f), Color.white, 1.15f);
            CreateProp(squareObj.transform, "Square_Lantern_SE", lanternSp, lanternSE,
                new Vector2(0.35f, 0.35f), new Vector2(0f, -0.55f),
                ComputeYSortingOrder(lanternSE.y, FootOffset_Lantern, 1.15f), Color.white, 1.15f);

            // 4. Ghế gỗ công viên nghỉ chân hai bên quảng trường (đặt chuẩn tâm ô)
            Vector3 benchW = new Vector3(-2.5f, 0.5f, 0f);
            Vector3 benchE = new Vector3(3.5f, 0.5f, 0f);
            CreateProp(squareObj.transform, "Bench_West", benchSp, benchW,
                new Vector2(1.4f, 0.5f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(benchW.y, FootOffset_Bench, 1.0f), Color.white, 1.0f);
            CreateProp(squareObj.transform, "Bench_East", benchSp, benchE,
                new Vector2(1.4f, 0.5f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(benchE.y, FootOffset_Bench, 1.0f), Color.white, 1.0f);
        }

        // --- 5 TRẠM DÂN LÀNG VỚI MẪU NHÀ MÁI RƠM ĐÁ CUỘI ĐẶT CHUẨN Ô & ĐỘ SÂU ---

        private static void CreateStationBaSau(Transform parent, Vector3 pos,
            Sprite bldgSp, Sprite tableSp, Sprite bowlSp, Sprite lanternSp, Sprite npcSp, Sprite fenceSp)
        {
            var stationObj = new GameObject("Station_BaSau");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Gian nhà tranh Quán Bà Sáu
            var hutObj = CreateProp(stationObj.transform, "Building_Hut", bldgSp, Vector3.zero,
                new Vector2(4.6f, 2.0f), new Vector2(0f, -0.6f),
                ComputeYSortingOrder(pos.y, -2.2f, 1f), Color.white, 1f);
            var cbHut = hutObj.AddComponent<CustomBuildingController>();
            cbHut.BuildingId = "station_ba_sau";
            cbHut.BuildingDisplayName = "Quán Nước Bà Sáu";
            cbHut.BuildingRenderer = hutObj.GetComponent<SpriteRenderer>();
            cbHut.DoorLocalOffset = new Vector2(0.5f, -2.2f);
            cbHut.InteractionRadius = 2.4f;
            cbHut.ActionPromptText = "Gặp Bà Sáu";
            cbHut.AssignedFunction = BuildingInteractionFunction.GiaoDichDanLang;
            cbHut.TargetVillager = VillagerId.BaSau;

            // Hàng rào sân chè hai bên (đặt chuẩn ô)
            CreateProp(stationObj.transform, "Fence_Front_L", fenceSp, new Vector3(-3.0f, -4.0f, 0f),
                new Vector2(2f, 0.4f), Vector2.zero,
                ComputeYSortingOrder(pos.y - 4.0f, FootOffset_Fence, 1f), Color.white, 1f);
            CreateProp(stationObj.transform, "Fence_Front_R", fenceSp, new Vector3(3.0f, -4.0f, 0f),
                new Vector2(2f, 0.4f), Vector2.zero,
                ComputeYSortingOrder(pos.y - 4.0f, FootOffset_Fence, 1f), Color.white, 1f);

            // Bàn trà mộc kèm ấm chén sứ ngoài hiên (đặt chuẩn tâm ô)
            var tableObj = CreateProp(stationObj.transform, "Table_TeaSet", tableSp, new Vector3(-2.0f, -4.0f, 0f),
                new Vector2(1.4f, 0.6f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(pos.y - 4.0f, FootOffset_Table, 1.15f), Color.white, 1.15f);

            // Chiếc Bát Sứt Úp Ngược (Taboo #2) đặt trang trọng trên bàn
            CreateProp(tableObj.transform, "Prop_Inverted_Bowl", bowlSp, new Vector3(0.3f, 0.2f, 0f),
                Vector2.zero, Vector2.zero,
                ComputeYSortingOrder(pos.y - 4.0f, FootOffset_Table, 1.15f) + 10, Color.white, 0.9f);

            // Trụ đèn lồng bão cạnh cổng sân (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Lantern_Post", lanternSp, new Vector3(2.0f, -5.0f, 0f),
                new Vector2(0.35f, 0.35f), new Vector2(0f, -0.55f),
                ComputeYSortingOrder(pos.y - 5.0f, FootOffset_Lantern, 1.1f), Color.white, 1.1f);

            // NPC Bà Sáu đứng đón khách ngoài sân
            CreateProp(stationObj.transform, "NPC_BaSau", npcSp, new Vector3(0f, -3.5f, 0f),
                Vector2.zero, Vector2.zero,
                ComputeYSortingOrder(pos.y - 3.5f, FootOffset_NPC, 1f), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(6.0f, 4.5f);
            trigCol.offset = new Vector2(0f, -3.0f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.BaSau;
            trig.StationTitle = "Quán Bà Sáu";
        }

        private static void CreateStationTamVa(Transform parent, Vector3 pos,
            Sprite bldgSp, Sprite benchSp, Sprite lumberSp, Sprite dummySp, Sprite npcSp, Sprite fenceSp)
        {
            var stationObj = new GameObject("Station_TamVa");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Xưởng mộc mái rơm đá cuội Tám Vá
            var wsObj = CreateProp(stationObj.transform, "Building_Workshop", bldgSp, Vector3.zero,
                new Vector2(4.6f, 2.0f), new Vector2(0f, -0.6f),
                ComputeYSortingOrder(pos.y, -2.2f, 1f), Color.white, 1f);
            var cbWs = wsObj.AddComponent<CustomBuildingController>();
            cbWs.BuildingId = "station_tam_va";
            cbWs.BuildingDisplayName = "Xưởng Mộc Tám Vá";
            cbWs.BuildingRenderer = wsObj.GetComponent<SpriteRenderer>();
            cbWs.DoorLocalOffset = new Vector2(0.5f, -2.2f);
            cbWs.InteractionRadius = 2.4f;
            cbWs.ActionPromptText = "Gặp Tám Vá";
            cbWs.AssignedFunction = BuildingInteractionFunction.GiaoDichDanLang;
            cbWs.TargetVillager = VillagerId.TamVa;

            // Bàn thợ mộc cưa xẻ gỗ ngoài sân (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Workbench", benchSp, new Vector3(2.0f, -4.0f, 0f),
                new Vector2(1.4f, 0.6f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(pos.y - 4.0f, FootOffset_Table, 1.15f), Color.white, 1.15f);

            // Đống gỗ xẻ bên hông xưởng (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Lumber_Stack", lumberSp, new Vector3(-2.0f, -4.0f, 0f),
                new Vector2(1.4f, 0.6f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(pos.y - 4.0f, FootOffset_Table, 1.2f), Color.white, 1.2f);

            // Bù nhìn thử nghiệm độ bền rào (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Dummy", dummySp, new Vector3(-2.0f, -2.5f, 0f),
                new Vector2(0.6f, 0.6f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(pos.y - 2.5f, FootOffset_PropSmall, 1.1f), Color.white, 1.1f);

            // NPC Tám Vá
            CreateProp(stationObj.transform, "NPC_TamVa", npcSp, new Vector3(0f, -3.5f, 0f),
                Vector2.zero, Vector2.zero,
                ComputeYSortingOrder(pos.y - 3.5f, FootOffset_NPC, 1f), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(6.0f, 4.5f);
            trigCol.offset = new Vector2(0f, -3.0f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.TamVa;
            trig.StationTitle = "Xưởng Mộc Tám Vá";
        }

        private static void CreateStationCoDung(Transform parent, Vector3 pos,
            Sprite bldgSp, Sprite dryingRackSp, Sprite mortarSp, Sprite wellSp, Sprite npcSp, Sprite fenceSp)
        {
            var stationObj = new GameObject("Station_CoDung");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Gian tiệm thuốc Cô Dung (Cửa hướng Bắc lên sân thềm x: -16.5, y: -17.5)
            var apoObj = CreateProp(stationObj.transform, "Building_Apothecary", bldgSp, Vector3.zero,
                new Vector2(4.6f, 2.0f), new Vector2(0f, 0.6f),
                ComputeYSortingOrder(pos.y, 2.2f, 1f), Color.white, 1f);
            var cbApo = apoObj.AddComponent<CustomBuildingController>();
            cbApo.BuildingId = "station_co_dung";
            cbApo.BuildingDisplayName = "Tiệm Thuốc Cô Dung";
            cbApo.BuildingRenderer = apoObj.GetComponent<SpriteRenderer>();
            cbApo.DoorLocalOffset = new Vector2(0.5f, 2.2f);
            cbApo.InteractionRadius = 2.4f;
            cbApo.ActionPromptText = "Mua Thuốc Cô Dung";
            cbApo.AssignedFunction = BuildingInteractionFunction.GiaoDichDanLang;
            cbApo.TargetVillager = VillagerId.CoDung;

            // Giàn phơi nia thuốc ngoài sân (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Drying_Rack", dryingRackSp, new Vector3(-2.0f, 4.0f, 0f),
                new Vector2(1.4f, 0.6f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(pos.y + 4.0f, FootOffset_Table, 1.15f), Color.white, 1.15f);

            // Cối đá giã thuốc thú y (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Herbal_Mortar", mortarSp, new Vector3(2.0f, 4.0f, 0f),
                new Vector2(0.6f, 0.6f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(pos.y + 4.0f, FootOffset_PropSmall, 1.1f), Color.white, 1.1f);

            // Giếng đá rửa dược liệu (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Well", wellSp, new Vector3(-2.0f, 2.5f, 0f),
                new Vector2(1.2f, 0.8f), new Vector2(0f, -0.3f),
                ComputeYSortingOrder(pos.y + 2.5f, FootOffset_Well, 1.15f), Color.white, 1.15f);

            // NPC Cô Dung
            CreateProp(stationObj.transform, "NPC_CoDung", npcSp, new Vector3(0f, 3.5f, 0f),
                Vector2.zero, Vector2.zero,
                ComputeYSortingOrder(pos.y + 3.5f, FootOffset_NPC, 1f), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(6.0f, 4.5f);
            trigCol.offset = new Vector2(0f, 3.0f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.CoDung;
            trig.StationTitle = "Tiệm Thuốc Cô Dung";
        }

        private static void CreateStationAnhCa(Transform parent, Vector3 pos,
            Sprite bldgSp, Sprite platformSp, Sprite dialSp, Sprite deskSp, Sprite npcAnhCa, Sprite npcOngTon, Sprite fenceSp)
        {
            var stationObj = new GameObject("Station_AnhCa");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Nhà trạm cân Anh Cả & Ông Tôn (Cửa hướng Bắc lên sân thềm x: 17.5, y: -17.5)
            var scaleObj = CreateProp(stationObj.transform, "Building_ScaleGate", bldgSp, Vector3.zero,
                new Vector2(4.6f, 2.0f), new Vector2(0f, 0.6f),
                ComputeYSortingOrder(pos.y, 2.2f, 1f), Color.white, 1f);
            var cbScale = scaleObj.AddComponent<CustomBuildingController>();
            cbScale.BuildingId = "station_anh_ca";
            cbScale.BuildingDisplayName = "Trạm Cân 6+1";
            cbScale.BuildingRenderer = scaleObj.GetComponent<SpriteRenderer>();
            cbScale.DoorLocalOffset = new Vector2(0.5f, 2.2f);
            cbScale.InteractionRadius = 2.4f;
            cbScale.ActionPromptText = "Gặp Anh Cả";
            cbScale.AssignedFunction = BuildingInteractionFunction.GiaoDichDanLang;
            cbScale.TargetVillager = VillagerId.AnhCa;

            // Bàn cân cơ học sàn sắt cho heo (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Scale_Platform", platformSp, new Vector3(-1.0f, 4.0f, 0f),
                new Vector2(1.5f, 0.8f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(pos.y + 4.0f, FootOffset_Table, 1.15f), Color.white, 1.15f);

            // Bảng đồng hồ vạch cân cấm số 7
            CreateProp(stationObj.transform, "Forbidden_Dial", dialSp, new Vector3(-2.5f, 5.0f, 0f),
                Vector2.zero, Vector2.zero,
                ComputeYSortingOrder(pos.y + 5.0f, FootOffset_Table, 1.1f) + 10, Color.white, 1.1f);

            // Bàn gỗ ghi chép Sổ Đen của Ông Tồn (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Ledger_Desk", deskSp, new Vector3(2.0f, 4.0f, 0f),
                new Vector2(1.4f, 0.6f), new Vector2(0f, -0.2f),
                ComputeYSortingOrder(pos.y + 4.0f, FootOffset_Table, 1.15f), Color.white, 1.15f);

            // NPC Anh Cả (Lực lưỡng)
            CreateProp(stationObj.transform, "NPC_AnhCa", npcAnhCa, new Vector3(-1.0f, 3.0f, 0f),
                Vector2.zero, Vector2.zero,
                ComputeYSortingOrder(pos.y + 3.0f, FootOffset_NPC, 1f), Color.white, 1f);

            // NPC Ông Tôn (Lão làng giữ sổ)
            CreateProp(stationObj.transform, "NPC_OngTon", npcOngTon, new Vector3(2.0f, 3.0f, 0f),
                Vector2.zero, Vector2.zero,
                ComputeYSortingOrder(pos.y + 3.0f, FootOffset_NPC, 1f), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(6.0f, 4.5f);
            trigCol.offset = new Vector2(0f, 3.0f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.AnhCa;
            trig.StationTitle = "Trạm Cân 6+1";
        }

        private static void CreateStationBiNhan(Transform parent, Vector3 pos,
            Sprite treeSp, Sprite shrineSp, Sprite megalithSp, Sprite auraSp, Sprite npcSp, Sprite forestTreeSp)
        {
            var stationObj = new GameObject("Station_BiNhan");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Cây Đa Cổ Thụ Ranh Giới Khổng Lồ (đặt chuẩn tâm ô x: 35.5, y: 0.5)
            var treeObj = CreateProp(stationObj.transform, "Ancient_Banyan_Tree", treeSp, Vector3.zero,
                Vector2.zero, Vector2.zero,
                ComputeYSortingOrder(pos.y, FootOffset_Banyan, 1.4f), Color.white, 1.4f);
            var treeCol = treeObj.AddComponent<CircleCollider2D>();
            treeCol.radius = 0.8f;
            treeCol.offset = new Vector2(0f, -1.8f);

            // Miếu Thờ Đá Rêu Phong (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Stone_Shrine", shrineSp, new Vector3(-3.0f, 0f, 0f),
                new Vector2(1.2f, 0.8f), new Vector2(0f, -0.3f),
                ComputeYSortingOrder(pos.y, FootOffset_Shrine, 1.15f), Color.white, 1.15f);

            // Bia Cự Thạch Khắc Phù Chú Ranh Giới (đặt chuẩn tâm ô)
            CreateProp(stationObj.transform, "Runic_Megalith", megalithSp, new Vector3(0f, -2.0f, 0f),
                new Vector2(1.0f, 1.0f), new Vector2(0f, -0.5f),
                ComputeYSortingOrder(pos.y - 2.0f, FootOffset_Shrine, 1.15f), Color.white, 1.15f);

            // Luồng Hào Quang Dị Biến Tím Huyền Bí
            CreateProp(stationObj.transform, "Mystic_Aura", auraSp, new Vector3(0f, -0.5f, 0f),
                Vector2.zero, Vector2.zero, -20, new Color(0.65f, 0.25f, 0.95f, 0.35f), 2.2f);

            // Cây cổ thụ hộ vệ hai bên miếu
            CreateProp(stationObj.transform, "Forest_Tree_L", forestTreeSp, new Vector3(-4.0f, 2.0f, 0f),
                new Vector2(0.6f, 0.45f), new Vector2(0f, -0.95f),
                ComputeYSortingOrder(pos.y + 2.0f, FootOffset_Tree, 1.3f), new Color(0.40f, 0.45f, 0.42f), 1.3f);
            CreateProp(stationObj.transform, "Forest_Tree_R", forestTreeSp, new Vector3(2.0f, -2.0f, 0f),
                new Vector2(0.6f, 0.45f), new Vector2(0f, -0.95f),
                ComputeYSortingOrder(pos.y - 2.0f, FootOffset_Tree, 1.3f), new Color(0.40f, 0.45f, 0.42f), 1.3f);

            // NPC Bí Nhân
            CreateProp(stationObj.transform, "NPC_BiNhan", npcSp, new Vector3(-2.0f, -1.0f, 0f),
                Vector2.zero, Vector2.zero,
                ComputeYSortingOrder(pos.y - 1.0f, FootOffset_NPC, 1f), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(6.0f, 5.0f);
            trigCol.offset = new Vector2(-1.0f, 0f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.BiNhan;
            trig.StationTitle = "Gốc Đa Ranh Giới";
        }

        private static void CreateVillageCottage(Transform parent, string name, Vector3 pos,
            Sprite cottageSp, string displayName, string actionPrompt, Sprite fenceSp, bool faceSouth = true)
        {
            var cottageObj = new GameObject(name);
            cottageObj.transform.SetParent(parent, false);
            cottageObj.transform.position = pos;

            float doorY = faceSouth ? -2.2f : 2.2f;
            float wallColY = faceSouth ? -0.6f : 0.6f;

            var bldgObj = CreateProp(cottageObj.transform, "Building_Cottage", cottageSp, Vector3.zero,
                new Vector2(4.6f, 2.0f), new Vector2(0f, wallColY),
                ComputeYSortingOrder(pos.y, doorY, 1f), Color.white, 1f);

            var cb = bldgObj.AddComponent<CustomBuildingController>();
            cb.BuildingId = name.ToLowerInvariant();
            cb.BuildingDisplayName = displayName;
            cb.BuildingRenderer = bldgObj.GetComponent<SpriteRenderer>();
            cb.DoorLocalOffset = new Vector2(0.5f, doorY);
            cb.InteractionRadius = 2.4f;
            cb.ActionPromptText = actionPrompt;
            cb.AssignedFunction = BuildingInteractionFunction.NghiNgoiGiuong;

            // Hàng rào sân trước/sau (đặt chuẩn ô)
            float fenceY = faceSouth ? -3.0f : 3.0f;
            CreateProp(cottageObj.transform, "Fence_L", fenceSp, new Vector3(-3.0f, fenceY, 0f),
                new Vector2(2f, 0.4f), Vector2.zero,
                ComputeYSortingOrder(pos.y + fenceY, FootOffset_Fence, 1f), Color.white, 1f);
            CreateProp(cottageObj.transform, "Fence_R", fenceSp, new Vector3(3.0f, fenceY, 0f),
                new Vector2(2f, 0.4f), Vector2.zero,
                ComputeYSortingOrder(pos.y + fenceY, FootOffset_Fence, 1f), Color.white, 1f);
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

        private static Tile CreateTile(Sprite sp, Tile.ColliderType colType = Tile.ColliderType.None)
        {
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sp;
            t.colliderType = colType;
            return t;
        }

        private static void BuildVillageTilemapPaths(
            Tilemap pathTilemap,
            HashSet<Vector3Int> stoneCells,
            HashSet<Vector3Int> dirtCells,
            HashSet<Vector3Int> dirtDiagNETopCells,
            HashSet<Vector3Int> dirtDiagNEBotCells,
            HashSet<Vector3Int> dirtDiagNWTopCells,
            HashSet<Vector3Int> dirtDiagNWBotCells,
            Tile stoneH, Tile stoneV, Tile stoneCross, Tile stoneTN, Tile stoneTS, Tile stoneTW, Tile stoneTE,
            Tile stoneCNW, Tile stoneCNE, Tile stoneCSW, Tile stoneCSE,
            Tile stoneCenter, Tile stoneEN, Tile stoneES, Tile stoneEW, Tile stoneEE,
            Tile stoneENOpen, Tile stoneESOpen, Tile stoneEWOpen, Tile stoneEEOpen,
            Tile dirtH, Tile dirtV, Tile dirtCross, Tile dirtTN, Tile dirtTS, Tile dirtTW, Tile dirtTE,
            Tile dirtCNW, Tile dirtCNE, Tile dirtCSW, Tile dirtCSE,
            Tile dirtCenter, Tile dirtEN, Tile dirtES, Tile dirtEW, Tile dirtEE,
            Tile dirtDiagNETop, Tile dirtDiagNEBot, Tile dirtDiagNWTop, Tile dirtDiagNWBot)
        {
            // 1. Auto-tile Stone Paths & Central Plaza
            foreach (var p in stoneCells)
            {
                int x = p.x;
                int y = p.y;
                Tile chosen = stoneCenter;

                // Khu vực Quảng Trường (x: -3..3, y: -3..3)
                if (x >= -3 && x <= 3 && y >= -3 && y <= 3)
                {
                    if (x == -3 && y == 3) chosen = stoneCNW;
                    else if (x == 3 && y == 3) chosen = stoneCNE;
                    else if (x == -3 && y == -3) chosen = stoneCSW;
                    else if (x == 3 && y == -3) chosen = stoneCSE;
                    else if (y == 3) chosen = (x == 0 || x == -1) ? stoneCenter : stoneEN;
                    else if (y == -3) chosen = (x == 0 || x == -1) ? stoneCenter : stoneES;
                    else if (x == -3) chosen = (y == 0 || y == -1) ? stoneCenter : stoneEW;
                    else if (x == 3) chosen = (y == 0 || y == -1) ? stoneCenter : stoneEE;
                    else chosen = stoneCenter;
                }
                else
                {
                    bool n = stoneCells.Contains(new Vector3Int(x, y + 1, 0)) || dirtCells.Contains(new Vector3Int(x, y + 1, 0));
                    bool s = stoneCells.Contains(new Vector3Int(x, y - 1, 0)) || dirtCells.Contains(new Vector3Int(x, y - 1, 0));
                    bool e = stoneCells.Contains(new Vector3Int(x + 1, y, 0)) || dirtCells.Contains(new Vector3Int(x + 1, y, 0));
                    bool w = stoneCells.Contains(new Vector3Int(x - 1, y, 0)) || dirtCells.Contains(new Vector3Int(x - 1, y, 0));

                    if (n && s && e && w) chosen = stoneCenter;
                    else if (e && w && s && !n) chosen = stoneEN;
                    else if (e && w && n && !s) chosen = stoneES;
                    else if (n && s && e && !w) chosen = stoneEW;
                    else if (n && s && w && !e) chosen = stoneEE;
                    else if (s && w && !n && !e) chosen = stoneCSW;
                    else if (s && e && !n && !w) chosen = stoneCSE;
                    else if (n && w && !s && !e) chosen = stoneCNW;
                    else if (n && e && !s && !w) chosen = stoneCNE;
                    else if (n && s && !e && !w) chosen = stoneV;
                    else chosen = stoneH;
                }

                pathTilemap.SetTile(p, chosen);
            }

            // 2. Auto-tile Dirt Trails & Diagonal 45-Degree Connectors
            foreach (var p in dirtCells)
            {
                if (stoneCells.Contains(p)) continue;

                // Ưu tiên khối chéo liền mạch cho cung đường 45 độ
                if (dirtDiagNETopCells.Contains(p))
                {
                    pathTilemap.SetTile(p, dirtDiagNETop);
                    continue;
                }
                if (dirtDiagNEBotCells.Contains(p))
                {
                    pathTilemap.SetTile(p, dirtDiagNEBot);
                    continue;
                }
                if (dirtDiagNWTopCells.Contains(p))
                {
                    pathTilemap.SetTile(p, dirtDiagNWTop);
                    continue;
                }
                if (dirtDiagNWBotCells.Contains(p))
                {
                    pathTilemap.SetTile(p, dirtDiagNWBot);
                    continue;
                }

                int x = p.x;
                int y = p.y;

                bool n = dirtCells.Contains(new Vector3Int(x, y + 1, 0)) || stoneCells.Contains(new Vector3Int(x, y + 1, 0));
                bool s = dirtCells.Contains(new Vector3Int(x, y - 1, 0)) || stoneCells.Contains(new Vector3Int(x, y - 1, 0));
                bool e = dirtCells.Contains(new Vector3Int(x + 1, y, 0)) || stoneCells.Contains(new Vector3Int(x + 1, y, 0));
                bool w = dirtCells.Contains(new Vector3Int(x - 1, y, 0)) || stoneCells.Contains(new Vector3Int(x - 1, y, 0));

                Tile chosen = dirtH;
                if (n && s && e && w) chosen = dirtCenter;
                else if (e && w && s && !n) chosen = dirtEN;
                else if (e && w && n && !s) chosen = dirtES;
                else if (n && s && e && !w) chosen = dirtEW;
                else if (n && s && w && !e) chosen = dirtEE;
                else if (s && w && !n && !e) chosen = dirtCSW;
                else if (s && e && !n && !w) chosen = dirtCSE;
                else if (n && w && !s && !e) chosen = dirtCNW;
                else if (n && e && !s && !w) chosen = dirtCNE;
                else if (n && s && !e && !w) chosen = dirtV;
                else chosen = dirtH;

                pathTilemap.SetTile(p, chosen);
            }
        }

        private static void DecorateRichVillageLandscape(Transform parent,
            Sprite treeWillowSp, Sprite treeFruitSp, Sprite treePineSp, Sprite treePrairieSp,
            Sprite flowerBushSp, Sprite flowerPatchSp, Sprite rockSp,
            Sprite hayBaleSp, Sprite barrelSp, Sprite benchSp, Sprite lanternSp)
        {
            Vector2 treeColSize = new Vector2(0.6f, 0.45f);
            Vector2 treeColOffset = new Vector2(0f, -0.95f);
            Vector2 benchColSize = new Vector2(1.4f, 0.5f);
            Vector2 benchColOffset = new Vector2(0f, -0.2f);
            Vector2 barrelColSize = new Vector2(0.6f, 0.4f);
            Vector2 barrelColOffset = new Vector2(0f, -0.2f);
            Vector2 hayColSize = new Vector2(0.8f, 0.45f);
            Vector2 hayColOffset = new Vector2(0f, -0.15f);
            Vector2 rockColSize = new Vector2(1.0f, 0.5f);
            Vector2 rockColOffset = new Vector2(0f, -0.2f);
            Vector2 lanternColSize = new Vector2(0.35f, 0.35f);
            Vector2 lanternColOffset = new Vector2(0f, -0.55f);

            // 1. DẢI CÂY LIỄU & BĂNG GHẾ VEN SÔNG THÔN GIÁP (ĐẶT CHUẨN Ô VEN SÔNG)
            float[] riverWillowY = { 5.5f, 9.5f, 13.5f, 17.5f, 21.5f, -5.5f, -9.5f, -13.5f, -17.5f, -21.5f };
            for (int i = 0; i < riverWillowY.Length; i++)
            {
                float wy = riverWillowY[i];
                Vector3 posW = new Vector3(-28.5f, wy, 0f);
                Vector3 posE = new Vector3(-19.5f, wy, 0f);
                CreateProp(parent, $"Willow_West_{i}", treeWillowSp, posW, treeColSize, treeColOffset,
                    ComputeYSortingOrder(posW.y, FootOffset_Tree, 1.25f), Color.white, 1.25f);
                CreateProp(parent, $"Willow_East_{i}", treeWillowSp, posE, treeColSize, treeColOffset,
                    ComputeYSortingOrder(posE.y, FootOffset_Tree, 1.25f), Color.white, 1.25f);
            }

            // Băng ghế ven bờ sông ngắm dòng nước (đặt chuẩn tâm ô)
            Vector3[] riverBenches = {
                new Vector3(-28.5f, 2.5f, 0f), new Vector3(-28.5f, -2.5f, 0f),
                new Vector3(-19.5f, 2.5f, 0f), new Vector3(-19.5f, -2.5f, 0f)
            };
            for (int i = 0; i < riverBenches.Length; i++)
            {
                CreateProp(parent, $"River_Bench_{i}", benchSp, riverBenches[i], benchColSize, benchColOffset,
                    ComputeYSortingOrder(riverBenches[i].y, FootOffset_Bench, 1.0f), Color.white, 1.0f);
            }

            // Đá cuội rêu phong bờ sông (đặt chuẩn tâm ô)
            Vector3[] riverRocks = {
                new Vector3(-27.5f, 6.5f, 0f), new Vector3(-20.5f, 7.5f, 0f),
                new Vector3(-27.5f, -6.5f, 0f), new Vector3(-20.5f, -11.5f, 0f)
            };
            for (int i = 0; i < riverRocks.Length; i++)
            {
                CreateProp(parent, $"River_Rock_{i}", rockSp, riverRocks[i], rockColSize, rockColOffset,
                    ComputeYSortingOrder(riverRocks[i].y, FootOffset_Rock, 1.1f), new Color(0.55f, 0.6f, 0.65f), 1.1f);
            }

            // 2. KHU RỪNG THẢO NGUYÊN & RỪNG THÔNG BIÊN GIỚI TÂY (WEST FOREST - ĐẶT THEO Ô)
            for (int wx = -38; wx <= -29; wx += 3)
            {
                for (int wy = -22; wy <= 22; wy += 4)
                {
                    if (Mathf.Abs(wy) < 3) continue; // Tránh đường mòn cổng Tây
                    Vector3 pos = new Vector3(wx + 0.5f, wy + 0.5f, 0f);
                    Sprite chosenTree = ((wx + wy) % 2 == 0) ? treePrairieSp : treePineSp;
                    CreateProp(parent, $"West_Tree_{wx}_{wy}", chosenTree, pos, treeColSize, treeColOffset,
                        ComputeYSortingOrder(pos.y, FootOffset_Tree, 1.3f), Color.white, 1.3f);
                }
            }

            // 3. KHU RỪNG CỔ THỤ & RỪNG THÔNG HUYỀN BÍ PHÍA ĐÔNG (EAST MYSTIC WOODS - ĐẶT THEO Ô)
            for (int wx = 25; wx <= 38; wx += 3)
            {
                for (int wy = -22; wy <= 22; wy += 4)
                {
                    if (Mathf.Abs(wy) < 3 && wx > 31) continue; // Tránh Gốc Đa Bí Nhân
                    Vector3 pos = new Vector3(wx + 0.5f, wy + 0.5f, 0f);
                    Sprite chosenTree = ((wx + wy) % 2 == 0) ? treePineSp : treePrairieSp;
                    CreateProp(parent, $"East_Tree_{wx}_{wy}", chosenTree, pos, treeColSize, treeColOffset,
                        ComputeYSortingOrder(pos.y, FootOffset_Tree, 1.3f), Color.white, 1.3f);
                }
            }

            // 4. VƯỜN CÂY ĂN QUẢ QUÁN BÀ SÁU (NORTHWEST FRUIT ORCHARD - ĐẶT THEO Ô)
            Vector3[] baSauTrees = {
                new Vector3(-13.5f, 19.5f, 0f), new Vector3(-10.5f, 19.5f, 0f), new Vector3(-7.5f, 19.5f, 0f),
                new Vector3(-13.5f, 13.5f, 0f), new Vector3(-10.5f, 13.5f, 0f), new Vector3(-7.5f, 13.5f, 0f),
                new Vector3(-13.5f, 9.5f, 0f),  new Vector3(-9.5f, 9.5f, 0f)
            };
            for (int i = 0; i < baSauTrees.Length; i++)
            {
                CreateProp(parent, $"Orchard_Tree_{i}", treeFruitSp, baSauTrees[i], treeColSize, treeColOffset,
                    ComputeYSortingOrder(baSauTrees[i].y, FootOffset_Tree, 1.35f), Color.white, 1.35f);
            }
            CreateProp(parent, "BaSau_Garden_Bench", benchSp, new Vector3(-11.5f, 11.5f, 0f), benchColSize, benchColOffset,
                ComputeYSortingOrder(11.5f, FootOffset_Bench, 1.0f), Color.white, 1.0f);
            CreateProp(parent, "BaSau_Water_Barrel_1", barrelSp, new Vector3(-14.5f, 15.5f, 0f), barrelColSize, barrelColOffset,
                ComputeYSortingOrder(15.5f, FootOffset_PropSmall, 1.1f), Color.white, 1.1f);
            CreateProp(parent, "BaSau_Water_Barrel_2", barrelSp, new Vector3(-13.5f, 15.5f, 0f), barrelColSize, barrelColOffset,
                ComputeYSortingOrder(15.5f, FootOffset_PropSmall, 1.1f), Color.white, 1.1f);

            // 5. KHU VƯỜN DƯỢC LIỆU TIỆM THUỐC CÔ DUNG (SOUTHWEST HERB SANCTUARY - ĐẶT THEO Ô)
            Vector3[] coDungTrees = {
                new Vector3(-13.5f, -19.5f, 0f), new Vector3(-10.5f, -19.5f, 0f), new Vector3(-7.5f, -19.5f, 0f),
                new Vector3(-13.5f, -13.5f, 0f), new Vector3(-10.5f, -13.5f, 0f), new Vector3(-7.5f, -13.5f, 0f),
                new Vector3(-13.5f, -9.5f, 0f),  new Vector3(-9.5f, -9.5f, 0f)
            };
            for (int i = 0; i < coDungTrees.Length; i++)
            {
                CreateProp(parent, $"Herb_Tree_{i}", treePrairieSp, coDungTrees[i], treeColSize, treeColOffset,
                    ComputeYSortingOrder(coDungTrees[i].y, FootOffset_Tree, 1.3f), Color.white, 1.3f);
            }
            CreateProp(parent, "CoDung_Barrel_1", barrelSp, new Vector3(-14.5f, -15.5f, 0f), barrelColSize, barrelColOffset,
                ComputeYSortingOrder(-15.5f, FootOffset_PropSmall, 1.1f), Color.white, 1.1f);
            CreateProp(parent, "CoDung_Barrel_2", barrelSp, new Vector3(-13.5f, -15.5f, 0f), barrelColSize, barrelColOffset,
                ComputeYSortingOrder(-15.5f, FootOffset_PropSmall, 1.1f), Color.white, 1.1f);

            // 6. RỪNG THÔNG & BÃI GỖ XƯỞNG MỘC TÁM VÁ (NORTHEAST TIMBER FOREST - ĐẶT THEO Ô)
            Vector3[] tamVaPines = {
                new Vector3(12.5f, 20.5f, 0f), new Vector3(15.5f, 22.5f, 0f), new Vector3(20.5f, 20.5f, 0f),
                new Vector3(24.5f, 22.5f, 0f), new Vector3(12.5f, 14.5f, 0f), new Vector3(22.5f, 15.5f, 0f), new Vector3(24.5f, 18.5f, 0f)
            };
            for (int i = 0; i < tamVaPines.Length; i++)
            {
                CreateProp(parent, $"TamVa_Pine_{i}", treePineSp, tamVaPines[i], treeColSize, treeColOffset,
                    ComputeYSortingOrder(tamVaPines[i].y, FootOffset_Tree, 1.35f), Color.white, 1.35f);
            }
            CreateProp(parent, "TamVa_Barrel_1", barrelSp, new Vector3(14.5f, 15.5f, 0f), barrelColSize, barrelColOffset,
                ComputeYSortingOrder(15.5f, FootOffset_PropSmall, 1.1f), Color.white, 1.1f);
            CreateProp(parent, "TamVa_Barrel_2", barrelSp, new Vector3(15.5f, 15.5f, 0f), barrelColSize, barrelColOffset,
                ComputeYSortingOrder(15.5f, FootOffset_PropSmall, 1.1f), Color.white, 1.1f);
            CreateProp(parent, "TamVa_Hay_1", hayBaleSp, new Vector3(21.5f, 15.5f, 0f), hayColSize, hayColOffset,
                ComputeYSortingOrder(15.5f, FootOffset_PropSmall, 1.15f), Color.white, 1.15f);

            // 7. BÃI CHĂN THẢ & ĐỤN RƠM TRẠM CÂN ANH CẢ (SOUTHEAST LIVESTOCK CORRAL - ĐẶT THEO Ô)
            Vector3[] anhCaTrees = {
                new Vector3(12.5f, -20.5f, 0f), new Vector3(15.5f, -22.5f, 0f), new Vector3(20.5f, -20.5f, 0f),
                new Vector3(24.5f, -22.5f, 0f), new Vector3(12.5f, -14.5f, 0f), new Vector3(22.5f, -15.5f, 0f), new Vector3(24.5f, -18.5f, 0f)
            };
            for (int i = 0; i < anhCaTrees.Length; i++)
            {
                Sprite chosen = (i % 2 == 0) ? treeFruitSp : treePrairieSp;
                CreateProp(parent, $"AnhCa_Tree_{i}", chosen, anhCaTrees[i], treeColSize, treeColOffset,
                    ComputeYSortingOrder(anhCaTrees[i].y, FootOffset_Tree, 1.3f), Color.white, 1.3f);
            }
            CreateProp(parent, "AnhCa_Hay_1", hayBaleSp, new Vector3(14.5f, -15.5f, 0f), hayColSize, hayColOffset,
                ComputeYSortingOrder(-15.5f, FootOffset_PropSmall, 1.15f), Color.white, 1.15f);
            CreateProp(parent, "AnhCa_Hay_2", hayBaleSp, new Vector3(15.5f, -15.5f, 0f), hayColSize, hayColOffset,
                ComputeYSortingOrder(-15.5f, FootOffset_PropSmall, 1.15f), Color.white, 1.15f);
            CreateProp(parent, "AnhCa_Hay_3", hayBaleSp, new Vector3(21.5f, -16.5f, 0f), hayColSize, hayColOffset,
                ComputeYSortingOrder(-16.5f, FootOffset_PropSmall, 1.15f), Color.white, 1.15f);
            CreateProp(parent, "AnhCa_Barrel_1", barrelSp, new Vector3(21.5f, -14.5f, 0f), barrelColSize, barrelColOffset,
                ComputeYSortingOrder(-14.5f, FootOffset_PropSmall, 1.1f), Color.white, 1.1f);
            CreateProp(parent, "AnhCa_Waiting_Bench", benchSp, new Vector3(13.5f, -13.5f, 0f), benchColSize, benchColOffset,
                ComputeYSortingOrder(-13.5f, FootOffset_Bench, 1.0f), Color.white, 1.0f);

            // 8. CÔNG VIÊN & KHU ĐỒNG CỎ TRUNG TÂM (CENTRAL COMMUNITY PARK - ĐẶT THEO Ô)
            Vector3[] parkTrees = {
                new Vector3(8.5f, 4.5f, 0f), new Vector3(14.5f, 5.5f, 0f), new Vector3(20.5f, 4.5f, 0f),
                new Vector3(8.5f, -4.5f, 0f), new Vector3(14.5f, -5.5f, 0f), new Vector3(20.5f, -4.5f, 0f),
                new Vector3(11.5f, 2.5f, 0f), new Vector3(17.5f, -2.5f, 0f)
            };
            for (int i = 0; i < parkTrees.Length; i++)
            {
                Sprite chosen = (i % 2 == 0) ? treeWillowSp : treeFruitSp;
                CreateProp(parent, $"Park_Tree_{i}", chosen, parkTrees[i], treeColSize, treeColOffset,
                    ComputeYSortingOrder(parkTrees[i].y, FootOffset_Tree, 1.3f), Color.white, 1.3f);
            }
            CreateProp(parent, "Park_Bench_1", benchSp, new Vector3(11.5f, 1.5f, 0f), benchColSize, benchColOffset,
                ComputeYSortingOrder(1.5f, FootOffset_Bench, 1.0f), Color.white, 1.0f);
            CreateProp(parent, "Park_Bench_2", benchSp, new Vector3(17.5f, -1.5f, 0f), benchColSize, benchColOffset,
                ComputeYSortingOrder(-1.5f, FootOffset_Bench, 1.0f), Color.white, 1.0f);

            // 9. CÂY BÓNG MÁT & ĐÈN LỒNG DỌC CÁC TRỤC ĐẠI LỘ (ĐẶT CHUẨN TÂM Ô)
            for (float wy = 6.5f; wy <= 14.5f; wy += 4.0f)
            {
                Vector3 nPosL = new Vector3(-3.5f, wy, 0f);
                Vector3 nPosR = new Vector3(3.5f, wy, 0f);
                Vector3 sPosL = new Vector3(-3.5f, -wy, 0f);
                Vector3 sPosR = new Vector3(3.5f, -wy, 0f);

                CreateProp(parent, $"NorthAvenue_Tree_L_{wy}", treeFruitSp, nPosL, treeColSize, treeColOffset,
                    ComputeYSortingOrder(nPosL.y, FootOffset_Tree, 1.25f), Color.white, 1.25f);
                CreateProp(parent, $"NorthAvenue_Tree_R_{wy}", treeFruitSp, nPosR, treeColSize, treeColOffset,
                    ComputeYSortingOrder(nPosR.y, FootOffset_Tree, 1.25f), Color.white, 1.25f);
                CreateProp(parent, $"SouthAvenue_Tree_L_{-wy}", treePrairieSp, sPosL, treeColSize, treeColOffset,
                    ComputeYSortingOrder(sPosL.y, FootOffset_Tree, 1.25f), Color.white, 1.25f);
                CreateProp(parent, $"SouthAvenue_Tree_R_{-wy}", treePrairieSp, sPosR, treeColSize, treeColOffset,
                    ComputeYSortingOrder(sPosR.y, FootOffset_Tree, 1.25f), Color.white, 1.25f);
            }

            // Đèn lồng dọc các trục đại lộ (đặt chuẩn tâm ô lề đường)
            Vector3[] lanterns = {
                // Đại lộ Tây (giữa cầu và quảng trường, y = 1.5f và -1.5f)
                new Vector3(-18.5f, 1.5f, 0f), new Vector3(-13.5f, 1.5f, 0f), new Vector3(-8.5f, 1.5f, 0f), new Vector3(-4.5f, 1.5f, 0f),
                new Vector3(-18.5f, -1.5f, 0f), new Vector3(-13.5f, -1.5f, 0f), new Vector3(-8.5f, -1.5f, 0f), new Vector3(-4.5f, -1.5f, 0f),
                // Trục Bắc (x = -1.5f và 1.5f)
                new Vector3(-1.5f, 6.5f, 0f), new Vector3(1.5f, 10.5f, 0f), new Vector3(-1.5f, 14.5f, 0f),
                // Trục Nam (x = -1.5f và 1.5f)
                new Vector3(-1.5f, -6.5f, 0f), new Vector3(1.5f, -10.5f, 0f), new Vector3(-1.5f, -14.5f, 0f),
                // Đại lộ Đông (x = 6.5f .. 31.5f, y = 1.5f và -1.5f)
                new Vector3(6.5f, 1.5f, 0f), new Vector3(11.5f, 1.5f, 0f), new Vector3(16.5f, 1.5f, 0f), new Vector3(21.5f, 1.5f, 0f), new Vector3(26.5f, 1.5f, 0f), new Vector3(31.5f, 1.5f, 0f),
                new Vector3(6.5f, -1.5f, 0f), new Vector3(11.5f, -1.5f, 0f), new Vector3(16.5f, -1.5f, 0f), new Vector3(21.5f, -1.5f, 0f), new Vector3(26.5f, -1.5f, 0f), new Vector3(31.5f, -1.5f, 0f)
            };
            for (int i = 0; i < lanterns.Length; i++)
            {
                CreateProp(parent, $"Avenue_Lantern_{i}", lanternSp, lanterns[i], lanternColSize, lanternColOffset,
                    ComputeYSortingOrder(lanterns[i].y, FootOffset_Lantern, 1.05f), Color.white, 1.05f);
            }

            // Băng ghế dọc đại lộ Tây và Đông (đặt chuẩn tâm ô)
            CreateProp(parent, "WestAvenue_Bench_N", benchSp, new Vector3(-11.5f, 1.5f, 0f), benchColSize, benchColOffset,
                ComputeYSortingOrder(1.5f, FootOffset_Bench, 1.0f), Color.white, 1.0f);
            CreateProp(parent, "WestAvenue_Bench_S", benchSp, new Vector3(-11.5f, -1.5f, 0f), benchColSize, benchColOffset,
                ComputeYSortingOrder(-1.5f, FootOffset_Bench, 1.0f), Color.white, 1.0f);
            CreateProp(parent, "EastAvenue_Bench_N", benchSp, new Vector3(13.5f, 1.5f, 0f), benchColSize, benchColOffset,
                ComputeYSortingOrder(1.5f, FootOffset_Bench, 1.0f), Color.white, 1.0f);
            CreateProp(parent, "EastAvenue_Bench_S", benchSp, new Vector3(13.5f, -1.5f, 0f), benchColSize, benchColOffset,
                ComputeYSortingOrder(-1.5f, FootOffset_Bench, 1.0f), Color.white, 1.0f);

            // 10. BỤI HOA DẠI & THẢM HOA ĐIỂM XUYẾT TOÀN BẢN ĐỒ (WALKABLE FOLIAGE - ĐẶT THEO Ô)
            Vector3[] flowerBushes = {
                new Vector3(-28.5f, 3.5f, 0f), new Vector3(-19.5f, 3.5f, 0f),
                new Vector3(-28.5f, -3.5f, 0f), new Vector3(-19.5f, -3.5f, 0f),
                new Vector3(-14.5f, 1.5f, 0f), new Vector3(-6.5f, 1.5f, 0f),
                new Vector3(-12.5f, 16.5f, 0f), new Vector3(-14.5f, 11.5f, 0f),
                new Vector3(-12.5f, -14.5f, 0f), new Vector3(-14.5f, -17.5f, 0f),
                new Vector3(11.5f, -15.5f, 0f), new Vector3(14.5f, -17.5f, 0f),
                new Vector3(14.5f, 3.5f, 0f), new Vector3(16.5f, -3.5f, 0f),
                new Vector3(-33.5f, 2.5f, 0f), new Vector3(-33.5f, -2.5f, 0f)
            };
            for (int i = 0; i < flowerBushes.Length; i++)
            {
                CreateProp(parent, $"Flower_Bush_{i}", flowerBushSp, flowerBushes[i], Vector2.zero, Vector2.zero,
                    ComputeYSortingOrder(flowerBushes[i].y, FootOffset_FlowerBush, 1.1f), Color.white, 1.1f);
            }
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
