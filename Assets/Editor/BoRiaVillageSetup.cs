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
            Sprite propMudPuddle = LoadSprite("Assets/Art/Sprites/Environment/Village/prop_mud_puddle.png", defaultSquare);
            Sprite tileStone = LoadSprite("Assets/Art/Sprites/Environment/Village/tile_village_stone.png", defaultSquare);
            Sprite stonePathSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path.png", tileStone);
            Sprite dirtTrailSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail.png", defaultSquare);
            Sprite waterTileSp = LoadSprite("Assets/Art/Tiles/Sprites/water_tile.png", defaultSquare);
            Sprite bridgeBaseSp = LoadSprite("Assets/Art/Sprites/Environment/Village/bridge_village_wood_stone.png", defaultSquare);
            Sprite bridgeRailSp = LoadSprite("Assets/Art/Sprites/Environment/Village/bridge_village_wood_stone_rail.png", defaultSquare);

            // C. Nhân Vật Dân Làng (NPCs)
            Sprite npcBaSau = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_ba_sau.png", defaultKnob);
            Sprite npcTamVa = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_tam_va.png", defaultKnob);
            Sprite npcCoDung = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_co_dung.png", defaultKnob);
            Sprite npcAnhCa = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_anh_ca.png", defaultKnob);
            Sprite npcOngTon = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_ong_ton.png", defaultKnob);
            Sprite npcBiNhan = LoadSprite("Assets/Art/Sprites/Characters/NPCs/npc_bi_nhan.png", defaultKnob);

            // D. Môi Trường Tự Nhiên & Rào
            Sprite treeSp = LoadSprite("Assets/Art/Sprites/Environment/Nature/tree_prairie.png", defaultKnob);
            Sprite rockSp = LoadSprite("Assets/Art/Sprites/Environment/Nature/rock_boulder.png", defaultKnob);
            Sprite grassBaseSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_base.png", defaultSquare);
            Sprite grassSteppeSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_steppe.png", grassBaseSp);
            Sprite grassTallSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_tall.png", grassBaseSp);
            Sprite grassFlower1Sp = LoadSprite("Assets/Art/Tiles/Sprites/grass_flower_1.png", grassBaseSp);
            Sprite grassFlower2Sp = LoadSprite("Assets/Art/Tiles/Sprites/grass_flower_2.png", grassBaseSp);
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
            // 5. MÔI TRƯỜNG LÀNG BỜ RÌA MỞ RỘNG (81 x 53 Ô GẠCH)
            // ==========================================
            var villageEnvRoot = new GameObject("Village_Environment_2D");

            var groundRoot = new GameObject("Village_Ground_Grid");
            groundRoot.transform.SetParent(villageEnvRoot.transform, false);

            var waterCollidersRoot = new GameObject("Village_Water_Colliders");
            waterCollidersRoot.transform.SetParent(villageEnvRoot.transform, false);

            // Sinh lưới gạch bản đồ mở rộng từ x: -40..40, y: -26..26
            for (int x = -40; x <= 40; x++)
            {
                for (int y = -26; y <= 26; y++)
                {
                    var tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.SetParent(groundRoot.transform, false);
                    tileObj.transform.position = new Vector3(x, y, 0);

                    var sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -100;

                    // 1. DÒNG SÔNG THÔN GIÁP (Rộng 6.5 - 7 ô gạch, uốn lượn tự nhiên)
                    float riverCenter = -24.0f + Mathf.Sin(y * 0.13f) * 2.5f;
                    float distRiver = Mathf.Abs(x - riverCenter);

                    // Khu vực Cầu Gỗ Đá Thôn Giáp (x: -28.5..-19.5, y: -1.3..1.3)
                    bool isBridgeDeck = (Mathf.Abs(y) <= 1.3f && Mathf.Abs(x - (-24.0f)) <= 4.4f);

                    if (distRiver <= 3.3f)
                    {
                        if (isBridgeDeck)
                        {
                            // Dưới sàn cầu: Nước sông sâu đổ bóng tối, KHÔNG gắn BoxCollider để người chơi bước qua cầu mượt mà
                            sr.sprite = waterTileSp;
                            sr.color = new Color(0.18f, 0.30f, 0.45f, 1f);
                            continue;
                        }
                        else
                        {
                            // Lòng sông rộng: Nước sâu ở giữa, nước cạn ven bờ
                            sr.sprite = waterTileSp;
                            if (distRiver <= 2.2f)
                                sr.color = new Color(0.24f, 0.48f, 0.82f, 1f); // Nước sông sâu
                            else
                                sr.color = new Color(0.36f, 0.62f, 0.88f, 0.95f); // Nước cạn ven bờ

                            // BoxCollider cản người chơi không rơi xuống sông
                            var wCol = tileObj.AddComponent<BoxCollider2D>();
                            wCol.size = Vector2.one;
                            tileObj.transform.SetParent(waterCollidersRoot.transform, false);
                            continue;
                        }
                    }
                    else if (distRiver <= 4.4f && !isBridgeDeck)
                    {
                        // Bờ sông sỏi cát tự nhiên (Riverbank shoreline)
                        sr.sprite = dirtTrailSp;
                        sr.color = new Color(0.76f, 0.68f, 0.56f, 1f);
                    }
                    else
                    {
                        // 2. MẠNG LƯỚI ĐƯỜNG THÔN DÃ (Thực tế, mềm mại, không nan hoa, không mảng bê tông)

                        // A. Đường vào làng phía Tây (từ Cổng Tây x = -36 đến đầu cầu x = -28.5)
                        float distWestEntry = 999f;
                        if (x >= -38 && x <= -20)
                            distWestEntry = Mathf.Abs(y);

                        // B. Đại lộ chính Thôn Bờ Rìa (từ đầu cầu x = -19.5 qua Quảng trường x = 0 đến x = 8)
                        float distMainStreet = 999f;
                        if (x >= -20 && x <= 8)
                        {
                            float streetY = Mathf.Sin((x - (-19.0f)) * 0.18f) * 0.6f;
                            distMainStreet = Mathf.Abs(y - streetY);
                        }

                        // C. Trục giao thông Thôn Tây (tại x = -16, nối Quán Bà Sáu y = 14 và Tiệm Thuốc Cô Dung y = -14)
                        float distWestCross = 999f;
                        if (y >= -15 && y <= 15 && x >= -21 && x <= -11)
                        {
                            float laneX = -16.0f + Mathf.Sin(y * 0.22f) * 0.8f;
                            distWestCross = Mathf.Abs(x - laneX);
                        }

                        // D. Vòng đai lát đá quanh Giếng Làng Quảng Trường (x = 0, y = -0.5)
                        float distWell = Mathf.Sqrt((x * 0.95f) * (x * 0.95f) + (y - (-0.5f)) * (y - (-0.5f)));
                        float distSquareLoop = (distWell >= 1.4f && distWell <= 4.2f) ? Mathf.Abs(distWell - 2.8f) : 999f;

                        // E. Lối Bắc - Nam nối Quảng trường tới Nhà Trưởng Thôn (y = 16) và Nhà Dân Cư (y = -16)
                        float distNorthAve = 999f;
                        if (y >= 2 && y <= 16 && x >= -4 && x <= 4)
                        {
                            float laneX = Mathf.Sin((y - 2.0f) * 0.25f) * 0.7f;
                            distNorthAve = Mathf.Abs(x - laneX);
                        }

                        float distSouthAve = 999f;
                        if (y >= -16 && y <= -2 && x >= -4 && x <= 4)
                        {
                            float laneX = Mathf.Sin((-y - 2.0f) * 0.25f) * 0.7f;
                            distSouthAve = Mathf.Abs(x - laneX);
                        }

                        // F. Nhánh đường Thôn Đông lên Xưởng Mộc Tám Vá (x = 18, y = 14)
                        float distNELane = 999f;
                        if (y >= 0 && y <= 14 && x >= 6 && x <= 22)
                        {
                            float t = y / 14.0f;
                            float laneX = 7.0f * (1f - t) + 18.0f * t + Mathf.Sin(t * Mathf.PI) * 1.5f;
                            distNELane = Mathf.Abs(x - laneX);
                        }

                        // G. Nhánh đường xe kéo xuống Trạm Cân 6+1 (x = 18, y = -14)
                        float distSELane = 999f;
                        if (y >= -14 && y <= 0 && x >= 6 && x <= 22)
                        {
                            float t = (-y) / 14.0f;
                            float laneX = 7.0f * (1f - t) + 18.0f * t + Mathf.Sin(t * Mathf.PI) * 1.5f;
                            distSELane = Mathf.Abs(x - laneX);
                        }

                        // H. Lối mòn Rừng Cấm sang Gốc Đa Bí Nhân (x = 8..34, y = 0)
                        float distForestTrail = 999f;
                        if (x >= 8 && x <= 34 && Mathf.Abs(y) <= 4)
                        {
                            float trailY = Mathf.Sin((x - 8.0f) * 0.22f) * 1.4f;
                            distForestTrail = Mathf.Abs(y - trailY);
                        }

                        // I. Sân vườn mộc mạc và bậc thềm đá trước cửa nhà (Không phải mảng bê tông)
                        bool isHouseYard = (
                            (Mathf.Abs(x - (-16f)) <= 2.6f && Mathf.Abs(y - 12.0f) <= 1.4f) || // Sân Bà Sáu
                            (Mathf.Abs(x - (-16f)) <= 2.6f && Mathf.Abs(y - (-16.0f)) <= 1.4f) || // Sân Cô Dung
                            (Mathf.Abs(x - 18f) <= 2.6f && Mathf.Abs(y - 12.0f) <= 1.4f) || // Sân Tám Vá
                            (Mathf.Abs(x - 18f) <= 2.6f && Mathf.Abs(y - (-16.0f)) <= 1.4f) || // Sân Anh Cả
                            (Mathf.Abs(x) <= 2.6f && Mathf.Abs(y - 14.0f) <= 1.4f) || // Sân Trưởng Thôn
                            (Mathf.Abs(x) <= 2.6f && Mathf.Abs(y - (-14.0f)) <= 1.4f) // Sân Nhà Dân
                        );

                        bool isDoorstep = (
                            (Mathf.Abs(x - (-15.5f)) <= 0.9f && Mathf.Abs(y - 12.2f) <= 0.8f) ||
                            (Mathf.Abs(x - (-15.5f)) <= 0.9f && Mathf.Abs(y - (-15.8f)) <= 0.8f) ||
                            (Mathf.Abs(x - 18.5f) <= 0.9f && Mathf.Abs(y - 12.2f) <= 0.8f) ||
                            (Mathf.Abs(x - 18.5f) <= 0.9f && Mathf.Abs(y - (-15.8f)) <= 0.8f) ||
                            (Mathf.Abs(x - 0.5f) <= 0.9f && Mathf.Abs(y - 14.2f) <= 0.8f) ||
                            (Mathf.Abs(x - 0.5f) <= 0.9f && Mathf.Abs(y - (-14.2f)) <= 0.8f)
                        );

                        float minPathDist = Mathf.Min(
                            distWestEntry,
                            Mathf.Min(distMainStreet,
                            Mathf.Min(distWestCross,
                            Mathf.Min(distSquareLoop,
                            Mathf.Min(distNorthAve,
                            Mathf.Min(distSouthAve,
                            Mathf.Min(distNELane,
                            Mathf.Min(distSELane, distForestTrail)))))))
                        );

                        // Nhiễu hạt tự nhiên cho viền đường mềm mại
                        float edgeNoise = ((Mathf.Sin(x * 1.7f + y * 2.3f) + Mathf.Cos(x * 2.1f - y * 1.5f)) * 0.5f) * 0.35f;

                        bool isStoneCore = (isDoorstep ||
                                           (distSquareLoop < 0.6f) ||
                                           (minPathDist <= (0.75f + edgeNoise)));

                        bool isDirtEdge = (isHouseYard ||
                                          (distSquareLoop < 1.4f) ||
                                          (minPathDist <= (1.65f + edgeNoise)));

                        if (isStoneCore)
                        {
                            sr.sprite = tileStone;
                            sr.color = new Color(0.88f, 0.90f, 0.92f, 1f);
                        }
                        else if (isDirtEdge)
                        {
                            sr.sprite = dirtTrailSp;
                            sr.color = new Color(0.82f, 0.72f, 0.60f, 1f);
                        }
                        else
                        {
                            // Đồng cỏ thảo nguyên và hoa dại làng Bờ Rìa
                            int pattern = Mathf.Abs((x * 37) ^ (y * 23)) % 100;
                            if (pattern < 15)
                            {
                                sr.sprite = grassFlower1Sp;
                                sr.color = new Color(0.95f, 0.90f, 0.90f, 1f);
                            }
                            else if (pattern < 25)
                            {
                                sr.sprite = grassFlower2Sp;
                                sr.color = new Color(0.90f, 0.95f, 0.90f, 1f);
                            }
                            else if (pattern < 45)
                            {
                                sr.sprite = grassTallSp;
                                sr.color = new Color(0.72f, 0.78f, 0.72f, 1f);
                            }
                            else if (pattern < 70)
                            {
                                sr.sprite = grassSteppeSp;
                                sr.color = new Color(0.70f, 0.76f, 0.70f, 1f);
                            }
                            else
                            {
                                sr.sprite = grassBaseSp;
                                sr.color = new Color(0.68f, 0.74f, 0.68f, 1f);
                            }
                        }
                    }
                }
            }

            // ==========================================
            // 6. CẦU GỖ ĐÁ THÔN GIÁP & VŨNG BÙN TỰ NHIÊN
            // ==========================================
            // Cầu Gỗ Đá Thôn Giáp (Dedicated Pixel-Art Bridge 9x3 Ô Gạch, bắc qua sông 7 ô)
            var bridgeRoot = new GameObject("Village_Main_Bridge");
            bridgeRoot.transform.SetParent(villageEnvRoot.transform, false);
            bridgeRoot.transform.position = new Vector3(-24f, 0f, 0f);

            // Thân cầu, mố đá và lan can Bắc (Sorting Order: -20, nằm sau người chơi)
            CreateProp(bridgeRoot.transform, "Bridge_Deck_Supports", bridgeBaseSp, Vector3.zero, Vector2.zero, Vector2.zero, -20, Color.white, 1f);

            // Lan can Nam Mặt Trước (Sorting Order: 20, nằm trước người chơi - chiều sâu 2.5D chân thực)
            CreateProp(bridgeRoot.transform, "Bridge_Front_Railing", bridgeRailSp, Vector3.zero, Vector2.zero, Vector2.zero, 20, Color.white, 1f);

            // BoxCollider chặn 2 bên mép lan can không cho người chơi rơi xuống lòng sông
            var railN = bridgeRoot.AddComponent<BoxCollider2D>();
            railN.size = new Vector2(9.0f, 0.7f);
            railN.offset = new Vector2(0f, 1.05f);

            var railS = bridgeRoot.AddComponent<BoxCollider2D>();
            railS.size = new Vector2(9.0f, 0.7f);
            railS.offset = new Vector2(0f, -1.05f);

            // Đèn lồng bão ở hai đầu mố cầu đá
            CreateProp(bridgeRoot.transform, "Bridge_Lantern_West", propLanternPost, new Vector3(-4.2f, 1.6f, 0f), Vector2.zero, Vector2.zero, 120, Color.white, 1.1f);
            CreateProp(bridgeRoot.transform, "Bridge_Lantern_East", propLanternPost, new Vector3(4.2f, 1.6f, 0f), Vector2.zero, Vector2.zero, 120, Color.white, 1.1f);

            // Vũng bùn sình lầy sau mưa dọc các lối mòn đất sỏi
            CreateProp(villageEnvRoot.transform, "MudPuddle_1", propMudPuddle, new Vector3(-30f, -0.6f, 0f), Vector2.zero, Vector2.zero, -45);
            CreateProp(villageEnvRoot.transform, "MudPuddle_2", propMudPuddle, new Vector3(-12f, 0.8f, 0f), Vector2.zero, Vector2.zero, -45);
            CreateProp(villageEnvRoot.transform, "MudPuddle_3", propMudPuddle, new Vector3(10f, -0.8f, 0f), Vector2.zero, Vector2.zero, -45);
            CreateProp(villageEnvRoot.transform, "MudPuddle_4", propMudPuddle, new Vector3(24f, 0.5f, 0f), Vector2.zero, Vector2.zero, -45);

            // ==========================================
            // 7. RỪNG CÂY & RÀO DẬU BAO QUANH LÀNG (Perimeter)
            // ==========================================
            var forestRoot = new GameObject("Village_Perimeter_Forest");
            forestRoot.transform.SetParent(villageEnvRoot.transform, false);
            CreateExpandedVillagePerimeter(forestRoot.transform, treeSp, rockSp, fenceBrokenSp);

            // ==========================================
            // 8. CỔNG TÂY LÀNG BỜ RÌA & ĐIỂM TRỞ VỀ NÔNG TRẠI
            // ==========================================
            var gatewayRoot = new GameObject("Village_West_Gateway");
            gatewayRoot.transform.SetParent(villageEnvRoot.transform, false);
            CreateWestGateway(gatewayRoot.transform, new Vector3(-35f, 0f, 0f), fencePostSp, fenceBrokenSp, propLanternPost);

            // ==========================================
            // 9. QUẢNG TRƯỜNG TRUNG TÂM (Central Square)
            // ==========================================
            var squareRoot = new GameObject("Village_Central_Square");
            squareRoot.transform.SetParent(villageEnvRoot.transform, false);
            CreateCentralSquare(squareRoot.transform, Vector3.zero, propNoticeBoard, propLanternPost, wellSp, rockSp);

            // ==========================================
            // 10. NĂM GIAN HÀNG / TRẠM DÂN LÀNG RIÊNG BIỆT (MẪU MÁI RƠM ĐÁ CUỘI MỚI)
            // ==========================================
            var stationsRoot = new GameObject("Village_Stations");
            stationsRoot.transform.SetParent(villageEnvRoot.transform, false);

            // 1. QUÁN NƯỚC BÀ SÁU (Thôn Tây Bắc: x = -16, y = 14)
            CreateStationBaSau(stationsRoot.transform, new Vector3(-16f, 14f, 0f),
                bldgBaSau, propTeaTable, propInvertedBowl, propLanternPost, npcBaSau, fenceHSp);

            // 2. XƯỞNG MỘC TÁM VÁ (Thôn Đông Bắc: x = 18, y = 14)
            CreateStationTamVa(stationsRoot.transform, new Vector3(18f, 14f, 0f),
                bldgTamVa, propWorkbench, propLumber, dummySp, npcTamVa, fenceHSp);

            // 3. TIỆM THUỐC THÚ Y CÔ DUNG (Thôn Tây Nam: x = -16, y = -14)
            CreateStationCoDung(stationsRoot.transform, new Vector3(-16f, -14f, 0f),
                bldgCoDung, propDryingRack, propMortar, wellSp, npcCoDung, fenceHSp);

            // 4. TRẠM CÂN 6+1 (Thôn Đông Nam: x = 18, y = -14)
            CreateStationAnhCa(stationsRoot.transform, new Vector3(18f, -14f, 0f),
                bldgScale, propScalePlatform, propForbiddenDial, propBlackLedgerDesk, npcAnhCa, npcOngTon, fenceHSp);

            // 5. GỐC ĐA RANH GIỚI - BÍ NHÂN (Cực Đông Rừng Cấm: x = 34, y = 0)
            CreateStationBiNhan(stationsRoot.transform, new Vector3(34f, 0f, 0f),
                treeBanyan, propStoneShrine, propRunicMegalith, vfxTractorBeamSp, npcBiNhan, treeSp);

            // 6. NHÀ DÂN LÀNG PHỤ (North Cottage & South Cottage)
            CreateVillageCottage(stationsRoot.transform, "Elder_Cottage", new Vector3(0f, 16f, 0f),
                bldgCottage, "Nhà Trưởng Thôn Bờ Rìa", "Gõ cửa nhà Trưởng thôn", fenceHSp);
            CreateVillageCottage(stationsRoot.transform, "Villager_Cottage", new Vector3(0f, -16f, 0f),
                bldgCottage, "Nhà Dân Làng Bờ Rìa", "Xem nhà dân xóm", fenceHSp);

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

        // --- CÁC HÀM HỖ TRỢ DỰNG MÔI TRƯỜNG & TRẠM ---

        private static void CreateExpandedVillagePerimeter(Transform parent, Sprite treeSp, Sprite rockSp, Sprite brokenFenceSp)
        {
            Color treeTint = new Color(0.48f, 0.55f, 0.48f); // Tông xám lạnh sương mù Bờ Rìa
            Color rockTint = new Color(0.45f, 0.48f, 0.52f);

            // Bờ Bắc (y = 26)
            for (float x = -40f; x <= 40f; x += 3.5f)
            {
                CreateProp(parent, $"Tree_North_{x}", treeSp, new Vector3(x, 26f, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-26f * 100), treeTint, 1.4f);
            }

            // Bờ Nam (y = -26)
            for (float x = -40f; x <= 40f; x += 3.5f)
            {
                CreateProp(parent, $"Tree_South_{x}", treeSp, new Vector3(x, -26f, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt(26f * 100), treeTint, 1.4f);
            }

            // Bờ Đông Rừng Cấm (x = 40)
            for (float y = -26f; y <= 26f; y += 3.2f)
            {
                CreateProp(parent, $"Tree_East_{y}", treeSp, new Vector3(40f, y, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-y * 100), treeTint, 1.4f);
            }

            // Bờ Tây (x = -40) chừa cổng ở giữa (y trong khoảng -3 đến 3)
            for (float y = -26f; y <= 26f; y += 3.2f)
            {
                if (Mathf.Abs(y) < 3.2f) continue; // Cổng làng Cổng Tây
                CreateProp(parent, $"Tree_West_{y}", treeSp, new Vector3(-40f, y, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-y * 100), treeTint, 1.4f);
            }

            // Bổ sung đá cuội phong rêu rải rác bờ rìa rừng
            for (int i = 0; i < 20; i++)
            {
                float rx = UnityEngine.Random.Range(-38f, 38f);
                float ry = UnityEngine.Random.Range(20f, 25f);
                if (i % 2 == 1) ry = UnityEngine.Random.Range(-25f, -20f);
                CreateProp(parent, $"Perimeter_Rock_{i}", rockSp, new Vector3(rx, ry, 0f), new Vector2(1.0f, 0.6f), new Vector2(0f, -0.2f), Mathf.RoundToInt(-ry * 100), rockTint, 1.1f);
            }
        }

        private static void CreateWestGateway(Transform parent, Vector3 pos, Sprite postSp, Sprite brokenFenceSp, Sprite lanternSp)
        {
            var tv = new GameObject("Trigger_Village_To_Farm");
            tv.transform.SetParent(parent, false);
            tv.transform.position = pos;

            var colV = tv.AddComponent<BoxCollider2D>();
            colV.isTrigger = true;
            colV.size = new Vector2(4.5f, 3.5f);

            var lV = tv.AddComponent<TransitionTriggerListener>();
            lV.IsTriggerToVillage = false;

            // Cột cổng gỗ hai bên
            CreateProp(tv.transform, "Post_L", postSp, new Vector3(0f, 2.0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, 180, Color.white, 1.3f);
            CreateProp(tv.transform, "Post_R", postSp, new Vector3(0f, -2.0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, -180, Color.white, 1.3f);

            // Rào dậu cạnh cổng
            CreateProp(tv.transform, "Broken_L", brokenFenceSp, new Vector3(-1.2f, 2.8f, 0f), new Vector2(1.2f, 0.5f), Vector2.zero, 250, new Color(0.7f, 0.65f, 0.6f), 1.1f);
            CreateProp(tv.transform, "Broken_R", brokenFenceSp, new Vector3(-1.2f, -2.8f, 0f), new Vector2(1.2f, 0.5f), Vector2.zero, -250, new Color(0.7f, 0.65f, 0.6f), 1.1f);

            // Đèn lồng treo cổng
            CreateProp(tv.transform, "Gate_Lantern", lanternSp, new Vector3(-0.5f, 2.4f, 0f), Vector2.zero, Vector2.zero, 260, Color.white, 1.1f);
        }

        private static void CreateCentralSquare(Transform parent, Vector3 pos, Sprite noticeBoardSp, Sprite lanternSp, Sprite wellSp, Sprite benchSp)
        {
            var squareObj = new GameObject("Central_Square_Elements");
            squareObj.transform.SetParent(parent, false);
            squareObj.transform.position = pos;

            // 1. Giếng Làng Cổ Kính ở chính giữa Quảng Trường (x = 0, y = -0.5)
            var wellObj = CreateProp(squareObj.transform, "Ancient_Village_Well", wellSp, new Vector3(0f, -0.5f, 0f), new Vector2(1.4f, 0.9f), new Vector2(0f, -0.4f), 50, Color.white, 1.25f);
            wellObj.AddComponent<WaterWell2DView>();

            // 2. Bảng Hương Ước Làng Bờ Rìa (Phía Bắc Giếng Làng: x = 0, y = 3.2)
            var boardParent = new GameObject("Village_Notice_Board");
            boardParent.transform.SetParent(squareObj.transform, false);
            boardParent.transform.position = new Vector3(0f, 3.2f, 0f);

            var boardObj = CreateProp(boardParent.transform, "Board_Sprite", noticeBoardSp, Vector3.zero, new Vector2(1.5f, 0.9f), new Vector2(0f, -0.4f), -120, Color.white, 1.25f);
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

            // 3. Bốn Cột Đèn Lồng Bão tại 4 góc quảng trường
            CreateProp(squareObj.transform, "Square_Lantern_NW", lanternSp, new Vector3(-4.5f, 4.5f, 0f), new Vector2(0.3f, 0.3f), Vector2.zero, 120, Color.white, 1.15f);
            CreateProp(squareObj.transform, "Square_Lantern_NE", lanternSp, new Vector3(4.5f, 4.5f, 0f), new Vector2(0.3f, 0.3f), Vector2.zero, 120, Color.white, 1.15f);
            CreateProp(squareObj.transform, "Square_Lantern_SW", lanternSp, new Vector3(-4.5f, -4.5f, 0f), new Vector2(0.3f, 0.3f), Vector2.zero, -120, Color.white, 1.15f);
            CreateProp(squareObj.transform, "Square_Lantern_SE", lanternSp, new Vector3(4.5f, -4.5f, 0f), new Vector2(0.3f, 0.3f), Vector2.zero, -120, Color.white, 1.15f);

            // 4. Ghế đá nghỉ chân hai bên
            CreateProp(squareObj.transform, "Bench_West", benchSp, new Vector3(-3.8f, 0f, 0f), new Vector2(1.2f, 0.6f), Vector2.zero, 10, new Color(0.6f, 0.62f, 0.65f), 1f);
            CreateProp(squareObj.transform, "Bench_East", benchSp, new Vector3(3.8f, 0f, 0f), new Vector2(1.2f, 0.6f), Vector2.zero, 10, new Color(0.6f, 0.62f, 0.65f), 1f);
        }

        // --- 5 TRẠM DÂN LÀNG VỚI MẪU NHÀ MÁI RƠM ĐÁ CUỘI ---

        private static void CreateStationBaSau(Transform parent, Vector3 pos,
            Sprite bldgSp, Sprite tableSp, Sprite bowlSp, Sprite lanternSp, Sprite npcSp, Sprite fenceSp)
        {
            var stationObj = new GameObject("Station_BaSau");
            stationObj.transform.SetParent(parent, false);
            stationObj.transform.position = pos;

            // Gian nhà tranh mái rơm đá cuội Quán Bà Sáu
            var hutObj = CreateProp(stationObj.transform, "Building_Hut", bldgSp, Vector3.zero, new Vector2(4.6f, 2.2f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-pos.y * 100), Color.white, 1f);
            var cbHut = hutObj.AddComponent<CustomBuildingController>();
            cbHut.BuildingId = "station_ba_sau";
            cbHut.BuildingDisplayName = "Quán Nước Bà Sáu";
            cbHut.BuildingRenderer = hutObj.GetComponent<SpriteRenderer>();
            cbHut.DoorLocalOffset = new Vector2(0.5f, -1.8f);
            cbHut.InteractionRadius = 2.4f;
            cbHut.ActionPromptText = "Gặp Bà Sáu";
            cbHut.AssignedFunction = BuildingInteractionFunction.GiaoDichDanLang;
            cbHut.TargetVillager = VillagerId.BaSau;

            // Hàng rào sân chè
            CreateProp(stationObj.transform, "Fence_Front_L", fenceSp, new Vector3(-3.2f, -3.2f, 0f), new Vector2(2f, 0.4f), Vector2.zero, Mathf.RoundToInt((-pos.y + 3.2f) * 100), Color.white, 1f);
            CreateProp(stationObj.transform, "Fence_Front_R", fenceSp, new Vector3(3.2f, -3.2f, 0f), new Vector2(2f, 0.4f), Vector2.zero, Mathf.RoundToInt((-pos.y + 3.2f) * 100), Color.white, 1f);

            // Bàn trà mộc kèm ấm chén sứ ngoài hiên
            var tableObj = CreateProp(stationObj.transform, "Table_TeaSet", tableSp, new Vector3(-1.8f, -2.4f, 0f), new Vector2(1.5f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 2.4f) * 100), Color.white, 1.15f);

            // Chiếc Bát Sứt Úp Ngược (Taboo #2) đặt trang trọng trên bàn
            CreateProp(tableObj.transform, "Prop_Inverted_Bowl", bowlSp, new Vector3(0.4f, 0.2f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 2.4f) * 100) + 10, Color.white, 0.9f);

            // Trụ đèn lồng bão cạnh cổng sân
            CreateProp(stationObj.transform, "Lantern_Post", lanternSp, new Vector3(-1.2f, -3.3f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 3.3f) * 100) + 5, Color.white, 1.1f);

            // NPC Bà Sáu đứng đón khách
            CreateProp(stationObj.transform, "NPC_BaSau", npcSp, new Vector3(0.5f, -2.1f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 2.1f) * 100), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(5.5f, 4.5f);
            trigCol.offset = new Vector2(0f, -1.2f);

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
            var wsObj = CreateProp(stationObj.transform, "Building_Workshop", bldgSp, Vector3.zero, new Vector2(4.6f, 2.2f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-pos.y * 100), Color.white, 1f);
            var cbWs = wsObj.AddComponent<CustomBuildingController>();
            cbWs.BuildingId = "station_tam_va";
            cbWs.BuildingDisplayName = "Xưởng Mộc Tám Vá";
            cbWs.BuildingRenderer = wsObj.GetComponent<SpriteRenderer>();
            cbWs.DoorLocalOffset = new Vector2(0.5f, -1.8f);
            cbWs.InteractionRadius = 2.4f;
            cbWs.ActionPromptText = "Gặp Tám Vá";
            cbWs.AssignedFunction = BuildingInteractionFunction.GiaoDichDanLang;
            cbWs.TargetVillager = VillagerId.TamVa;

            // Bàn thợ mộc cưa xẻ gỗ ngoài sân
            CreateProp(stationObj.transform, "Workbench", benchSp, new Vector3(1.8f, -2.4f, 0f), new Vector2(1.5f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 2.4f) * 100), Color.white, 1.15f);

            // Đống gỗ xẻ & cọc rào gia cố bên hông
            CreateProp(stationObj.transform, "Lumber_Stack", lumberSp, new Vector3(3.6f, -0.8f, 0f), new Vector2(1.5f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 0.8f) * 100), Color.white, 1.2f);

            // Bù nhìn thử nghiệm độ bền rào
            CreateProp(stationObj.transform, "Dummy", dummySp, new Vector3(-3.2f, -2.2f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 2.2f) * 100), Color.white, 1.1f);

            // NPC Tám Vá
            CreateProp(stationObj.transform, "NPC_TamVa", npcSp, new Vector3(0.5f, -2.1f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 2.1f) * 100), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(5.5f, 4.5f);
            trigCol.offset = new Vector2(0f, -1.2f);

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

            // Gian tiệm thuốc mái rơm đá cuội Cô Dung
            var apoObj = CreateProp(stationObj.transform, "Building_Apothecary", bldgSp, Vector3.zero, new Vector2(4.6f, 2.2f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-pos.y * 100), Color.white, 1f);
            var cbApo = apoObj.AddComponent<CustomBuildingController>();
            cbApo.BuildingId = "station_co_dung";
            cbApo.BuildingDisplayName = "Tiệm Thuốc Cô Dung";
            cbApo.BuildingRenderer = apoObj.GetComponent<SpriteRenderer>();
            cbApo.DoorLocalOffset = new Vector2(0.5f, -1.8f);
            cbApo.InteractionRadius = 2.4f;
            cbApo.ActionPromptText = "Mua Thuốc Cô Dung";
            cbApo.AssignedFunction = BuildingInteractionFunction.GiaoDichDanLang;
            cbApo.TargetVillager = VillagerId.CoDung;

            // Giàn phơi nia thuốc thảo dược ngoài sân
            CreateProp(stationObj.transform, "Drying_Rack", dryingRackSp, new Vector3(-2.8f, -1.2f, 0f), new Vector2(1.5f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 1.2f) * 100), Color.white, 1.15f);

            // Cối đá giã thuốc thú y
            CreateProp(stationObj.transform, "Herbal_Mortar", mortarSp, new Vector3(2.6f, -2.2f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 2.2f) * 100), Color.white, 1.1f);

            // Giếng đá rửa dược liệu
            CreateProp(stationObj.transform, "Well", wellSp, new Vector3(-3.2f, 1.5f, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.3f), Mathf.RoundToInt((-pos.y - 1.5f) * 100), Color.white, 1.15f);

            // NPC Cô Dung
            CreateProp(stationObj.transform, "NPC_CoDung", npcSp, new Vector3(0.5f, -2.1f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 2.1f) * 100), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(5.5f, 4.5f);
            trigCol.offset = new Vector2(0f, -1.2f);

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

            // Nhà trạm cân mái rơm đá cuội vững chãi (Anh Cả & Ông Tôn)
            var scaleObj = CreateProp(stationObj.transform, "Building_ScaleGate", bldgSp, Vector3.zero, new Vector2(4.6f, 2.2f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-pos.y * 100), Color.white, 1f);
            var cbScale = scaleObj.AddComponent<CustomBuildingController>();
            cbScale.BuildingId = "station_anh_ca";
            cbScale.BuildingDisplayName = "Trạm Cân 6+1";
            cbScale.BuildingRenderer = scaleObj.GetComponent<SpriteRenderer>();
            cbScale.DoorLocalOffset = new Vector2(0.5f, -1.8f);
            cbScale.InteractionRadius = 2.4f;
            cbScale.ActionPromptText = "Gặp Anh Ca";
            cbScale.AssignedFunction = BuildingInteractionFunction.GiaoDichDanLang;
            cbScale.TargetVillager = VillagerId.AnhCa;

            // Bàn cân cơ học sàn sắt cho heo
            CreateProp(stationObj.transform, "Scale_Platform", platformSp, new Vector3(-0.4f, -2.4f, 0f), new Vector2(1.6f, 0.9f), Vector2.zero, Mathf.RoundToInt((-pos.y + 2.4f) * 100), Color.white, 1.15f);

            // Bảng đồng hồ vạch cân cấm số 7
            CreateProp(stationObj.transform, "Forbidden_Dial", dialSp, new Vector3(-2.2f, -1.8f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.8f) * 100) + 10, Color.white, 1.1f);

            // Bàn gỗ ghi chép Sổ Đen của Ông Tồn
            CreateProp(stationObj.transform, "Ledger_Desk", deskSp, new Vector3(2.6f, -2.2f, 0f), new Vector2(1.5f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 2.2f) * 100), Color.white, 1.15f);

            // NPC Anh Cả (Lực lưỡng)
            CreateProp(stationObj.transform, "NPC_AnhCa", npcAnhCa, new Vector3(-1.8f, -2.6f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 2.6f) * 100), Color.white, 1f);

            // NPC Ông Tôn (Lão làng giữ sổ)
            CreateProp(stationObj.transform, "NPC_OngTon", npcOngTon, new Vector3(2.8f, -2.6f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 2.6f) * 100), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(5.8f, 4.8f);
            trigCol.offset = new Vector2(0f, -1.2f);

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

            // Cây Đa Cổ Thụ Ranh Giới Khổng Lồ
            var treeObj = CreateProp(stationObj.transform, "Ancient_Banyan_Tree", treeSp, Vector3.zero, Vector2.zero, Vector2.zero, Mathf.RoundToInt(-pos.y * 100), Color.white, 1.4f);
            var treeCol = treeObj.AddComponent<CircleCollider2D>();
            treeCol.radius = 0.75f;
            treeCol.offset = new Vector2(0f, -1.2f);

            // Miếu Thờ Đá Rêu Phong
            CreateProp(stationObj.transform, "Stone_Shrine", shrineSp, new Vector3(-2.6f, -1.8f, 0f), new Vector2(1.2f, 0.8f), Vector2.zero, Mathf.RoundToInt((-pos.y + 1.8f) * 100), Color.white, 1.15f);

            // Bia Cự Thạch Khắc Phù Chú Ranh Giới
            CreateProp(stationObj.transform, "Runic_Megalith", megalithSp, new Vector3(2.6f, 0.8f, 0f), new Vector2(1.0f, 1.2f), Vector2.zero, Mathf.RoundToInt((-pos.y - 0.8f) * 100), Color.white, 1.15f);

            // Luồng Hào Quang Dị Biến Tím Huyền Bí
            CreateProp(stationObj.transform, "Mystic_Aura", auraSp, new Vector3(0f, -0.6f, 0f), Vector2.zero, Vector2.zero, -20, new Color(0.65f, 0.25f, 0.95f, 0.35f), 2.2f);

            // Cụm cây cổ thụ bao quanh bảo vệ miếu
            CreateProp(stationObj.transform, "Forest_Tree_L", forestTreeSp, new Vector3(-4.0f, 1.2f, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt((-pos.y - 1.2f) * 100), new Color(0.40f, 0.45f, 0.42f), 1.3f);
            CreateProp(stationObj.transform, "Forest_Tree_R", forestTreeSp, new Vector3(3.8f, -1.8f, 0f), new Vector2(1.2f, 0.8f), new Vector2(0f, -0.6f), Mathf.RoundToInt((-pos.y + 1.8f) * 100), new Color(0.40f, 0.45f, 0.42f), 1.3f);

            // NPC Bí Nhân
            CreateProp(stationObj.transform, "NPC_BiNhan", npcSp, new Vector3(-0.4f, -1.8f, 0f), Vector2.zero, Vector2.zero, Mathf.RoundToInt((-pos.y + 1.8f) * 100), Color.white, 1f);

            // Trigger tương tác
            var trigCol = stationObj.AddComponent<BoxCollider2D>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector2(6.0f, 4.8f);
            trigCol.offset = new Vector2(0f, -1.0f);

            var trig = stationObj.AddComponent<VillageInteractableTrigger>();
            trig.TargetVillagerId = VillagerId.BiNhan;
            trig.StationTitle = "Gốc Đa Ranh Giới";
        }

        private static void CreateVillageCottage(Transform parent, string name, Vector3 pos,
            Sprite cottageSp, string displayName, string actionPrompt, Sprite fenceSp)
        {
            var cottageObj = new GameObject(name);
            cottageObj.transform.SetParent(parent, false);
            cottageObj.transform.position = pos;

            var bldgObj = CreateProp(cottageObj.transform, "Building_Cottage", cottageSp, Vector3.zero, new Vector2(4.6f, 2.2f), new Vector2(0f, -0.6f), Mathf.RoundToInt(-pos.y * 100), Color.white, 1f);
            var cb = bldgObj.AddComponent<CustomBuildingController>();
            cb.BuildingId = name.ToLowerInvariant();
            cb.BuildingDisplayName = displayName;
            cb.BuildingRenderer = bldgObj.GetComponent<SpriteRenderer>();
            cb.DoorLocalOffset = new Vector2(0.5f, -1.8f);
            cb.InteractionRadius = 2.4f;
            cb.ActionPromptText = actionPrompt;
            cb.AssignedFunction = BuildingInteractionFunction.NghiNgoiGiuong; // Giường nghỉ ngơi hoặc ghé thăm

            // Hàng rào sân trước
            CreateProp(cottageObj.transform, "Fence_L", fenceSp, new Vector3(-3.2f, -2.8f, 0f), new Vector2(2f, 0.4f), Vector2.zero, Mathf.RoundToInt((-pos.y + 2.8f) * 100), Color.white, 1f);
            CreateProp(cottageObj.transform, "Fence_R", fenceSp, new Vector3(3.2f, -2.8f, 0f), new Vector2(2f, 0.4f), Vector2.zero, Mathf.RoundToInt((-pos.y + 2.8f) * 100), Color.white, 1f);
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
