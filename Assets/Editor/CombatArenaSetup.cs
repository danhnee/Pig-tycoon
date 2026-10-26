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
    public static class CombatArenaSetup
    {
        private const string ArenaScenePath = "Assets/Scenes/CombatTestArena2D.unity";

        [MenuItem("PigTycoon/3. Tạo & Mở Map Test Combat (Đồng Cỏ 2D)")]
        public static void CreateAndOpenCombatArenaScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "PigTycoon - Cảnh Báo",
                    "Unity đang ở chế độ Play Mode.\nVui lòng tắt nút Play trước khi tạo hoặc nạp Scene Đấu Trường Đồng Cỏ!",
                    "Đã hiểu"
                );
                return;
            }

            Universal2DSetup.SetupUniversal2D();

            string dir = Path.GetDirectoryName(ArenaScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite defaultSquare = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                Texture2D.whiteTexture.width
            );
            Sprite defaultKnob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // Nạp Sprite môi trường, đồng cỏ, chuồng trại & nhân vật
            Sprite grassSp = LoadSprite("Assets/Art/Tiles/Sprites/grass_base.png", defaultSquare);
            Sprite grassFlower1 = LoadSprite("Assets/Art/Tiles/Sprites/grass_flower_1.png", grassSp);
            Sprite grassFlower2 = LoadSprite("Assets/Art/Tiles/Sprites/grass_flower_2.png", grassSp);
            Sprite grassFlower3 = LoadSprite("Assets/Art/Tiles/Sprites/grass_flower_3.png", grassSp);
            Sprite grassTall = LoadSprite("Assets/Art/Tiles/Sprites/grass_tall.png", grassSp);
            Sprite dirtSp = LoadSprite("Assets/Art/Tiles/Sprites/dirt_trail.png", defaultSquare);
            Sprite stoneSp = LoadSprite("Assets/Art/Tiles/Sprites/stone_path.png", defaultSquare);

            Sprite fenceHSp = LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_h.png", defaultSquare);
            Sprite fenceVSp = LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_v.png", fenceHSp);
            Sprite fenceCrackSp = LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_damaged_overlay.png", null);
            Sprite fenceBrokenSp = LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_broken.png", null);

            Sprite treeSp = LoadSprite("Assets/Art/Sprites/Environment/Nature/tree_prairie.png", defaultKnob);
            Sprite rockSp = LoadSprite("Assets/Art/Sprites/Environment/Nature/rock_boulder.png", defaultKnob);

            Sprite barnSp = LoadSprite("Assets/Art/Sprites/Environment/Buildings/shelter_barn.png", defaultKnob);
            Sprite siloSp = LoadSprite("Assets/Art/Sprites/Environment/Buildings/feed_silo.png", defaultKnob);
            Sprite feederSp = LoadSprite("Assets/Art/Sprites/Environment/Buildings/feeder_trough.png", defaultKnob);
            Sprite waterTroughSp = LoadSprite("Assets/Art/Sprites/Environment/Buildings/water_trough.png", defaultKnob);
            Sprite towerSp = LoadSprite("Assets/Art/Sprites/Environment/Buildings/defense_tower.png", defaultKnob);

            Sprite pigHongDien = LoadSprite("Assets/Art/Sprites/Pigs/hong_dien/idle.png", defaultKnob);
            Sprite pigLamKhe = LoadSprite("Assets/Art/Sprites/Pigs/lam_khe/idle.png", defaultKnob);
            Sprite pigKimTho = LoadSprite("Assets/Art/Sprites/Pigs/kim_tho/idle.png", defaultKnob);
            Sprite pigHuThe = LoadSprite("Assets/Art/Sprites/Pigs/hu_the/idle.png", defaultKnob);

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

            // 1. Main Camera 2D
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.32f, 0.20f);
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var camFollow = camObj.AddComponent<CameraFollow2D>();
            camFollow.TargetOrthoSize = 6.5f;
            camFollow.SmoothSpeed = 8.0f;
            camFollow.ClampToFarmBounds = false;

            camObj.AddComponent<CameraShake2D>();

            // 2. Global Light 2D
            var lightObj = new GameObject("Arena Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(1.0f, 0.98f, 0.94f);
            light2D.intensity = 1.15f;

            // 3. Game Controller & Spawner
            var gameCtrlObj = new GameObject("GameController");
            gameCtrlObj.AddComponent<MobileGameController>();

            var waveSpawnerObj = new GameObject("EnemyWaveSpawner");
            waveSpawnerObj.AddComponent<EnemyWaveSpawner>();

            // 4. Môi trường Đấu Trường & Triển Lãm Toàn Diện (57m x 43m)
            var arenaRoot = new GameObject("Arena_Environment");

            var floorRoot = new GameObject("Arena_Floor_Tiles");
            floorRoot.transform.SetParent(arenaRoot.transform, false);

            for (int x = -28; x <= 28; x++)
            {
                for (int y = -21; y <= 21; y++)
                {
                    var tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.SetParent(floorRoot.transform, false);
                    tileObj.transform.position = new Vector3(x, y, 0);

                    var sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -10000;

                    // Mạng lưới đường đi & quảng trường:
                    // 1. Đại Lộ Tháp Bắc (North Boulevard: y in [12, 17], x in [-25, 25])
                    bool isNorthPromenade = (y >= 13 && y <= 16 && x >= -25 && x <= 25);
                    // 2. Đại Lộ Tháp Nam (South Promenade: y in [-16, -12], x in [-25, 25])
                    bool isSouthPromenade = (y >= -16 && y <= -13 && x >= -25 && x <= 25);
                    // 3. Thao Trường Võ Học (Combat Dojo: x in [-26, -15], y in [-5, 5])
                    bool isDojoFloor = (x >= -26 && x <= -15 && y >= -5 && y <= 5);
                    // 4. Khu Triển Lãm Quái Vật Alien (East Alien Zoo: x in [15, 26], y in [-6, 6])
                    bool isAlienFloor = (x >= 15 && x <= 26 && y >= -6 && y <= 6);
                    // 5. Đường nối trung tâm chữ thập
                    bool isCrossRoad = (x >= -2 && x <= 2 && y >= -20 && y <= 20) || (y >= -1 && y <= 1 && x >= -25 && x <= 25);
                    // 6. Sân đấu trung tâm (Royal Colosseum Arena Ring)
                    bool isCenterPit = (x >= -9 && x <= 9 && y >= -6 && y <= 6);

                    if (isNorthPromenade || isSouthPromenade || isDojoFloor || isCenterPit)
                    {
                        sr.sprite = stoneSp;
                        sr.color = new Color(0.92f, 0.94f, 0.98f, 1f);
                    }
                    else if (isAlienFloor)
                    {
                        sr.sprite = stoneSp;
                        sr.color = new Color(0.85f, 0.88f, 0.95f, 1f);
                    }
                    else if (isCrossRoad)
                    {
                        sr.sprite = dirtSp;
                        sr.color = new Color(0.92f, 0.86f, 0.78f, 1f);
                    }
                    else
                    {
                        // Đồng cỏ tự nhiên hoa lá
                        int pattern = Mathf.Abs((x * 37) ^ (y * 19)) % 100;
                        if (pattern < 8) sr.sprite = grassFlower1;
                        else if (pattern < 15) sr.sprite = grassFlower2;
                        else if (pattern < 22) sr.sprite = grassFlower3;
                        else if (pattern < 32) sr.sprite = grassTall;
                        else sr.sprite = grassSp;

                        sr.color = new Color(0.95f, 1f, 0.95f, 1f);
                    }
                }
            }

            // 5. Viền Bìa Rừng Rậm & Hàng Rào Biên Giới
            var forestRoot = new GameObject("Forest_Border");
            forestRoot.transform.SetParent(arenaRoot.transform, false);
            CreateForestPerimeter(forestRoot.transform, treeSp, rockSp, 28.5f, 21.5f);

            // 6. KHU VỰC 1: TRIỂN LÃM TOÀN BỘ 20 THÁP PHÒNG THỦ (4 Factions)
            var towersRoot = new GameObject("Grand_Towers_Showcase_Gallery");
            towersRoot.transform.SetParent(arenaRoot.transform, false);

            var defenseMgr = new DefenseManager();
            CreateAll20ShowcaseTowers(towersRoot.transform, defenseMgr, towerSp);

            // 7. KHU VỰC 2: TRIỂN LÃM QUÁI VẬT (3 Faction Enclosures)
            var exhibitsRoot = new GameObject("Monster_Exhibition_Sanctuaries");
            exhibitsRoot.transform.SetParent(arenaRoot.transform, false);
            CreateMonsterExhibits(exhibitsRoot.transform, fenceHSp, fenceVSp);

            // 8. KHU VỰC 3: ĐẤU TRƯỜNG GIÁC ĐẤU HOÀNG GIA (ROYAL COMBAT COLOSSEUM)
            var colosseumRoot = new GameObject("Grand_Colosseum_Arena_Ring");
            colosseumRoot.transform.SetParent(arenaRoot.transform, false);
            CreatePavilionBanner(colosseumRoot.transform, "SÀN ĐẤU GIÁC ĐẤU HOÀNG GIA (ROYAL COMBAT COLOSSEUM)", new Vector3(0f, 7.5f, 0f), "#38bdf8");

            // 9. KHU VỰC 4: THAO TRƯỜNG VÕ HỌC DOJO, BÙ NHÌN & DEFLECT TRAINER
            var trainingRoot = new GameObject("Combat_Training_Dojo");
            trainingRoot.transform.SetParent(arenaRoot.transform, false);

            CreateDummy(trainingRoot.transform, "Bù Nhìn Rơm (0 Giáp)", new Vector3(-18.5f, 3.2f, 0f), 0, Color.white);
            CreateDummy(trainingRoot.transform, "Bù Nhìn Sắt (15 Giáp)", new Vector3(-18.5f, 0f, 0f), 15, Color.white);
            CreateDummy(trainingRoot.transform, "Bù Nhìn Thép (30 Giáp)", new Vector3(-18.5f, -3.2f, 0f), 30, Color.white);

            // Turret Bắn Đạn Luyện Phản Đòn
            var turretStationObj = new GameObject("Test_Turret_Station");
            turretStationObj.transform.SetParent(trainingRoot.transform, false);
            turretStationObj.transform.position = new Vector3(-23.5f, 0f, 0f);
            var turretSr = turretStationObj.AddComponent<SpriteRenderer>();
            turretSr.sprite = towerSp;
            turretSr.sortingOrder = 200;
            turretStationObj.transform.localScale = new Vector3(1.4f, 1.6f, 1f);

            var turretLabel = new GameObject("Turret_Label");
            turretLabel.transform.SetParent(turretStationObj.transform, false);
            turretLabel.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var labelMesh = turretLabel.AddComponent<TextMeshPro>();
            labelMesh.rectTransform.localPosition = new Vector3(0f, 1.2f, 0f);
            labelMesh.rectTransform.anchoredPosition = new Vector2(0f, 1.2f);
            labelMesh.text = "MÁY BẮN ĐẠN\n<color=#fcd34d>(DEFLECT TRAINER)</color>";
            labelMesh.fontSize = 2.4f;
            labelMesh.color = Color.white;
            labelMesh.alignment = TextAlignmentOptions.Center;
            labelMesh.rectTransform.sizeDelta = new Vector2(3.6f, 0.8f);

            // Bàn Trưng Bày 4 Vũ Khí
            CreateWeaponDisplayPedestals(trainingRoot.transform, new Vector3(-15.0f, 0f, 0f));

            // Bàn Trưng Bày Bản Vẽ & Vật Phẩm Rơi (Loot Drops)
            CreateLootShowcase(trainingRoot.transform, new Vector3(-15.0f, -4.5f, 0f));

            // 10. Player (Khoa)
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

            // 11. Canvas UI Đấu Trường
            var canvasObj = new GameObject("Combat Arena Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Dev Mode Popup
            var devMode = canvasObj.AddComponent<DevModePopup>();
            devMode.EnsureUIExists();
            devMode.Hide();

            // EventSystem
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Virtual Joystick
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

            // 12. Combat Arena Controller & Manager
            var arenaManagerObj = new GameObject("CombatArenaManager");
            var arenaCtrl = arenaManagerObj.AddComponent<CombatArenaController>();
            arenaCtrl.PlayerSpawnPoint = playerObj.transform;
            arenaCtrl.DummySpawnRoot = trainingRoot.transform;
            arenaCtrl.ProjectileTurretPoint = turretStationObj.transform;
            arenaCtrl.TowersRoot = towersRoot.transform;

            // Lưu Scene vào Assets/Scenes/CombatTestArena2D.unity
            EditorSceneManager.SaveScene(scene, ArenaScenePath);
            BoRiaVillageSetup.UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[PigTycoon] ĐÃ TẠO THÀNH CÔNG ĐẤU TRƯỜNG & TRIỂN LÃM COMBAT TOÀN DIỆN TẠI: {ArenaScenePath}</color>");
        }

        #region Pre-placing All 20 Defense Towers
        private static void CreateAll20ShowcaseTowers(Transform parent, DefenseManager mgr, Sprite fallbackSp)
        {
            // =========================================================================
            // ĐẠI LỘ THÁP BẮC (y = 15.5f): 10 Tháp Công Nghệ Cao (Bạch Vân & Alien Tech)
            // =========================================================================

            // Pavilion 1: 5 Tháp Cơ Bản & Bạch Vân
            string[] bachVanIds = new string[] { "NoXuyenVan", "BayChongGo", "CuuTieuLoiPhao", "ThienNhanVongDai", "VanLinhHoiNguyenTran" };
            float[] bachVanX = new float[] { -22f, -19f, -16f, -13f, -10f };

            CreatePavilionBanner(parent, "KHU TRIỂN LÃM: CƠ BẢN & BẠCH VÂN THẦN KHÍ", new Vector3(-16f, 18.8f, 0f), "#fcd34d");
            for (int i = 0; i < bachVanIds.Length; i++)
            {
                var bp = mgr.GetBlueprint(bachVanIds[i]);
                if (bp != null)
                {
                    CreateShowcaseTower(parent, bp, new Vector3(bachVanX[i], 15.5f, 0f), fallbackSp);
                }
            }

            // Pavilion 2: 5 Tháp Alien Tech
            string[] alienIds = new string[] { "TruPlasmaHoiTu", "PhaoCoiLuongTu", "ThapPhanTrongLuc", "TruXungDienEMP", "PhaoDaiPhanVatChat" };
            float[] alienX = new float[] { 10f, 13f, 16f, 19f, 22f };

            CreatePavilionBanner(parent, "KHU TRIỂN LÃM: CÔNG NGHỆ NGOÀI HÀNH TINH (ALIEN TECH)", new Vector3(16f, 18.8f, 0f), "#38bdf8");
            for (int i = 0; i < alienIds.Length; i++)
            {
                var bp = mgr.GetBlueprint(alienIds[i]);
                if (bp != null)
                {
                    CreateShowcaseTower(parent, bp, new Vector3(alienX[i], 15.5f, 0f), fallbackSp);
                }
            }

            // =========================================================================
            // ĐẠI LỘ THÁP NAM (y = -14.0f): 10 Tháp Phế Liệu & Sinh Học (Bandit Scrap & Bio/Mystic)
            // =========================================================================

            // Pavilion 3: 5 Tháp Bandit Scrap
            string[] banditIds = new string[] { "NoLienChauThep", "MayBanRiuCua", "ThapPhunLuaHacIn", "PhaoLaoMocXich", "DaiBacChuaTrom" };
            float[] banditX = new float[] { -8f, -4f, 0f, 4f, 8f };

            CreatePavilionBanner(parent, "KHU TRIỂN LÃM: TẠP PHẾ BĂNG CƯỚP (BANDIT SCRAP)", new Vector3(0f, -11.0f, 0f), "#fb923c");
            for (int i = 0; i < banditIds.Length; i++)
            {
                var bp = mgr.GetBlueprint(banditIds[i]);
                if (bp != null)
                {
                    CreateShowcaseTower(parent, bp, new Vector3(banditX[i], -14.0f, 0f), fallbackSp);
                }
            }

            // Pavilion 4: 5 Tháp Bio & Mystic
            string[] bioIds = new string[] { "UGaiDocGamNhom", "TruNamBaoTuMu", "OngBomDichAxit", "CotToHutHon", "CotDiCotSuongHuyen" };
            float[] bioX = new float[] { -22f, -19f, -16f, -13f, -10f };

            CreatePavilionBanner(parent, "KHU TRIỂN LÃM: DỊCH HỌA & HUYỀN BÍ (BIO & MYSTIC)", new Vector3(-16f, -6.8f, 0f), "#c084fc");
            for (int i = 0; i < bioIds.Length; i++)
            {
                var bp = mgr.GetBlueprint(bioIds[i]);
                if (bp != null)
                {
                    CreateShowcaseTower(parent, bp, new Vector3(bioX[i], -9.5f, 0f), fallbackSp);
                }
            }
        }

        private static void CreateShowcaseTower(Transform parent, DefenseBlueprint bp, Vector3 pos, Sprite fallbackSp)
        {
            var towerObj = new GameObject($"Tower_{bp.Id}");
            towerObj.transform.SetParent(parent, false);
            towerObj.transform.position = pos;

            var sr = towerObj.AddComponent<SpriteRenderer>();
            var chosenSprite = UISpriteLoader.GetTowerSprite(bp.Id) ?? fallbackSp;
            sr.sprite = chosenSprite;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            // Tùy biến màu sắc & hiệu ứng phe phái đặc trưng
            Color factionTint = bp.Faction switch
            {
                "Alien" => new Color(0.70f, 0.94f, 1.0f, 1f), // Ánh xanh Plasma công nghệ cao
                "Bandit" => new Color(0.95f, 0.82f, 0.68f, 1f), // Tạp phế sắt gỉ & gỗ cháy xém
                "BioMystic" => (bp.Category == BuildingCategory.Bay || bp.Id.Contains("Doc") || bp.Id.Contains("Axit"))
                    ? new Color(0.72f, 0.98f, 0.65f, 1f) // Độc tố sinh học xanh lục
                    : new Color(0.88f, 0.70f, 1.0f, 1f), // Huyền bí tà thuật tím sương
                _ => new Color(1.0f, 0.95f, 0.75f, 1f) // Bạch Vân thần khí hoàng kim
            };
            sr.color = factionTint;

            // Tùy biến tỷ lệ scale trực quan
            if (bp.Category == BuildingCategory.Bay)
            {
                towerObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            }
            else if (bp.Id == "PhaoDaiPhanVatChat")
            {
                towerObj.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            }
            else
            {
                towerObj.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            }

            var col = towerObj.AddComponent<CircleCollider2D>();
            col.radius = 0.55f;

            var view = towerObj.AddComponent<DefenseTower2DView>();
            view.BaseSprite = sr;
            view.InfiniteAmmoInTest = true;
            view.TargetDummies = true;
            view.ShowFloatingBars = false; // Triển lãm gọn gàng, thanh HP chỉ xuất hiện khi chịu sát thương
            view.OnlyShowBarsWhenDamaged = true;

            // Không nhân bản toàn bộ tháp canh làm Turret (loại bỏ hoàn toàn lỗi 2 tháp gỗ chồng lên nhau)
            view.TurretSprite = null;

            var instance = CreateTowerInstance(bp, (Vector2)pos);
            view.Bind(instance);

            // Không gắn nhãn tên trên đầu tháp phòng thủ (giữ tháp sạch sẽ, nguyên bản)

            // Target Dummy tương ứng đặt phía trước tháp để thử nghiệm bắn
            float dummyY = (pos.y > 0) ? (pos.y - 2.8f) : (pos.y + 2.8f);
            CreateDummy(parent, $"Dummy_{bp.Id}", new Vector3(pos.x, dummyY, 0f), 10, Color.white);
        }

        private static DefenseBuildingInstance CreateTowerInstance(DefenseBlueprint bp, Vector2 pos)
        {
            return new DefenseBuildingInstance
            {
                InstanceId = $"tower_{bp.Id}_{Guid.NewGuid():N}",
                BlueprintId = bp.Id,
                Name = bp.Name,
                Category = bp.Category,
                IsBachVan = bp.IsBachVan,
                Faction = bp.Faction,
                Tier = bp.Tier,
                AttackType = bp.AttackType,
                SpecialValue = bp.SpecialValue,
                Durability = bp.Durability,
                MaxDurability = bp.Durability,
                MaxHp = bp.MaxHp,
                CurrentHp = bp.MaxHp,
                AmmoCurrent = bp.AmmoCapacity,
                AmmoCapacity = bp.AmmoCapacity,
                AmmoName = bp.AmmoName,
                AmmoCostPerShot = bp.IsBachVan ? 50 : 1,
                CooldownSeconds = bp.CooldownSeconds,
                Damage = bp.Damage,
                AttackRange = bp.AttackRange,
                WorldX = pos.x,
                WorldY = pos.y
            };
        }

        private static void CreatePavilionBanner(Transform parent, string title, Vector3 pos, string hexColor)
        {
            var bannerObj = new GameObject($"Banner_{title}");
            bannerObj.transform.SetParent(parent, false);
            bannerObj.transform.localPosition = pos;

            var tm = bannerObj.AddComponent<TextMeshPro>();
            tm.rectTransform.localPosition = pos;
            tm.rectTransform.anchoredPosition = new Vector2(pos.x, pos.y);
            tm.text = $"<color={hexColor}><b>=== {title} ===</b></color>";
            tm.fontSize = 2.8f;
            tm.alignment = TextAlignmentOptions.Center;
            tm.sortingOrder = 500;
            tm.rectTransform.sizeDelta = new Vector2(18f, 1.4f);
        }
        #endregion

        #region Pre-placing Monster Faction Enclosures
        private static void CreateMonsterExhibits(Transform parent, Sprite fenceH, Sprite fenceV)
        {
            // =========================================================================
            // KHU TRƯNG BÀY 1: ALIEN FACTION (Phía Đông: x in [16, 26], y in [-6, 6])
            // =========================================================================
            var alienRoot = new GameObject("Enclosure_Alien_Faction");
            alienRoot.transform.SetParent(parent, false);
            CreatePavilionBanner(alienRoot.transform, "KHU TRƯNG BÀY: SINH VẬT & CHIẾN HẠM ALIEN", new Vector3(20.5f, 7.5f, 0f), "#38bdf8");

            CreateMonsterExhibitPiece(alienRoot.transform, "UFO Trinh Sát", EnemyTypeId.UfoTrinhSat,
                UISpriteLoader.GetUfoScoutSprite(), new Vector3(18f, 5.0f, 0f), new Vector3(1.8f, 1.8f, 1f),
                "HP 160 | Tốc độ cao\nBay trên không định vị đàn heo");

            CreateMonsterExhibitPiece(alienRoot.transform, "UFO Thu Hoạch", EnemyTypeId.UfoThuHoach,
                UISpriteLoader.GetUfoScoutSprite(), new Vector3(23f, 5.0f, 0f), new Vector3(2.2f, 2.2f, 1f),
                "HP 650 | Tractor Beam\nChiếu chùm sáng hút heo lên không gian");

            CreateMonsterExhibitPiece(alienRoot.transform, "Lính Alien Grunt", EnemyTypeId.AlienGrunt,
                UISpriteLoader.GetAlienGruntSprite(), new Vector3(18f, 0.5f, 0f), new Vector3(1.55f, 1.55f, 1f),
                "HP 180 | Cận chiến hung hãn\nThả sau khi UFO thất bại, dồn bắt heo");

            CreateMonsterExhibitPiece(alienRoot.transform, "Alien Elite Tinh Nhuệ", EnemyTypeId.AlienElite,
                UISpriteLoader.GetAlienEliteSprite(), new Vector3(23f, 0.5f, 0f), new Vector3(1.95f, 1.95f, 1f),
                "HP 450 | Trượng Năng Lượng\nKhiên Plasma tự hồi phục & Tia điện");

            CreateMonsterExhibitPiece(alienRoot.transform, "Boss Alien Warlord", EnemyTypeId.BossAlienWarlord,
                UISpriteLoader.GetBossAlienWarlordSprite(), new Vector3(18f, -4.5f, 0f), new Vector3(2.85f, 2.85f, 1f),
                "HP 8000 | Sóng Xung Kích\nBoss Chiến Tranh Hủy Diệt Tối Thượng");

            CreateMonsterExhibitPiece(alienRoot.transform, "Tàu Mẹ UFO Đổ Bộ", EnemyTypeId.UfoMothership,
                UISpriteLoader.GetUfoMothershipSprite(), new Vector3(23.5f, -4.5f, 0f), new Vector3(3.8f, 3.8f, 1f),
                "HP 8000 | Pháo Đài Bay\nĐại chiến xâm lược toàn bản đồ");

            // =========================================================================
            // KHU TRƯNG BÀY 2: BANDIT FACTION (Phía Nam - Tây: x in [16, 25], y in [-17, -10])
            // =========================================================================
            var banditRoot = new GameObject("Enclosure_Bandit_Faction");
            banditRoot.transform.SetParent(parent, false);
            CreatePavilionBanner(banditRoot.transform, "DOANH TRẠI BĂNG CƯỚP ĐẠO TẶC", new Vector3(20.5f, -9.0f, 0f), "#fb923c");

            CreateMonsterExhibitPiece(banditRoot.transform, "Kẻ Bắt Heo", EnemyTypeId.KeBatHeo,
                UISpriteLoader.GetThiefSprite(), new Vector3(18f, -11.5f, 0f), new Vector3(1.35f, 1.35f, 1f),
                "HP 150 | Bao Tải Trộm\nLén vào chuồng trói heo kéo đi");

            CreateMonsterExhibitPiece(banditRoot.transform, "Kẻ Cào Phá", EnemyTypeId.KeCaoPha,
                UISpriteLoader.GetBreakerSprite(), new Vector3(23f, -11.5f, 0f), new Vector3(1.5f, 1.5f, 1f),
                "HP 120 | Vuốt Sắt Phá Rào\nCào phá rào chắn mở đường cướp");

            CreateMonsterExhibitPiece(banditRoot.transform, "Đầu Lĩnh Trộm", EnemyTypeId.DauLinhTrom,
                UISpriteLoader.GetDauLinhTromSprite(), new Vector3(18f, -15.5f, 0f), new Vector3(1.85f, 1.85f, 1f),
                "HP 500 | Tiếng Hú Chỉ Huy\nBuff tốc độ chạy cho đồng bọn");

            CreateMonsterExhibitPiece(banditRoot.transform, "Chúa Trộm Rìu Sắt", EnemyTypeId.ChuaTromRiuSat,
                UISpriteLoader.GetBossChuaTromSprite(), new Vector3(23f, -15.5f, 0f), new Vector3(2.45f, 2.45f, 1f),
                "HP 12000 | Boss Chu Kỳ 5\nĐại Rìu Sắt bổ nát mọi công trình");
        }

        private static void CreateMonsterExhibitPiece(Transform parent, string name, EnemyTypeId type, Sprite sprite, Vector3 pos, Vector3 scale, string description)
        {
            var pieceObj = new GameObject($"Exhibit_{name}");
            pieceObj.transform.SetParent(parent, false);
            pieceObj.transform.position = pos;

            var sr = pieceObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite ?? UISpriteLoader.GetThiefSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);
            pieceObj.transform.localScale = scale;

            var col = pieceObj.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;

            // Bảng biển tên & chỉ số
            var signObj = new GameObject("Sign");
            signObj.transform.SetParent(pieceObj.transform, false);
            var tm = signObj.AddComponent<TextMeshPro>();
            tm.rectTransform.localPosition = new Vector3(0f, 1.1f, 0f);
            tm.rectTransform.anchoredPosition = new Vector2(0f, 1.1f);
            tm.text = $"<b>{name}</b>\n<size=70%>{description}</size>";
            tm.fontSize = 2.0f;
            tm.color = Color.white;
            tm.alignment = TextAlignmentOptions.Center;
            tm.sortingOrder = sr.sortingOrder + 20;
            tm.rectTransform.sizeDelta = new Vector2(3.5f, 0.8f);
        }
        #endregion

        #region Dojo Pedestals & Showcase
        private static void CreateWeaponDisplayPedestals(Transform parent, Vector3 basePos)
        {
            var root = new GameObject("Weapon_Display_Pedestals");
            root.transform.SetParent(parent, false);
            root.transform.position = basePos;

            string[] weaponIds = new string[] { "kiem_sat", "bua_ta", "giao_tre", "dao_go" };
            float[] offsets = new float[] { 2.4f, 0.8f, -0.8f, -2.4f };

            for (int i = 0; i < weaponIds.Length; i++)
            {
                var wep = WeaponDatabase.GetWeapon(weaponIds[i]);
                if (wep == null) continue;

                Vector3 pos = basePos + new Vector3(0f, offsets[i], 0f);
                var obj = new GameObject($"Pedestal_{wep.Id}");
                obj.transform.SetParent(root.transform, false);
                obj.transform.position = pos;

                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = wep.Icon ?? UISpriteLoader.GetFrameActionButton();
                sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);
                obj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

                var sign = new GameObject("Label");
                sign.transform.SetParent(obj.transform, false);
                var tm = sign.AddComponent<TextMeshPro>();
                tm.rectTransform.localPosition = new Vector3(1.8f, 0f, 0f);
                tm.rectTransform.anchoredPosition = new Vector2(1.8f, 0f);
                tm.text = $"<color=#fcd34d><b>{wep.DisplayName}</b></color>\n<size=75%>{wep.ActionType} | {wep.SpecialSkill}\n{wep.MinDamage}-{wep.MaxDamage} DMG</size>";
                tm.fontSize = 1.9f;
                tm.alignment = TextAlignmentOptions.Left;
                tm.sortingOrder = sr.sortingOrder + 10;
                tm.rectTransform.sizeDelta = new Vector2(3.2f, 0.8f);
            }
        }

        private static void CreateLootShowcase(Transform parent, Vector3 basePos)
        {
            var root = new GameObject("Loot_Showcase_Gallery");
            root.transform.SetParent(parent, false);
            root.transform.position = basePos;

            // Rải các mẫu vật phẩm với cột sáng lấp lánh
            string[] showItems = new string[]
            {
                "item_blueprint_plasma_beam", "item_blueprint_antimatter_spire",
                "item_plasma_core", "item_quantum_fiber", "item_rusty_nail"
            };

            for (int i = 0; i < showItems.Length; i++)
            {
                Vector3 p = basePos + new Vector3(i * 1.6f - 3.2f, 0f, 0f);
                var loot = LootDatabase.CreateLootItem(showItems[i], 1);
                if (loot != null)
                {
                    var view = LootDrop2DView.Spawn(loot, (Vector2)p);
                    view.transform.SetParent(root.transform, true);
                }
            }

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(root.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var tm = labelObj.AddComponent<TextMeshPro>();
            tm.rectTransform.localPosition = new Vector3(0f, 1.2f, 0f);
            tm.rectTransform.anchoredPosition = new Vector2(0f, 1.2f);
            tm.text = "TRIỂN LÃM VẬT PHẨM & BẢN VẼ RƠI TỪ QUÁI (LOOT BEAMS)";
            tm.fontSize = 2.1f;
            tm.color = new Color(0.4f, 0.95f, 1f);
            tm.alignment = TextAlignmentOptions.Center;
            tm.sortingOrder = 500;
            tm.rectTransform.sizeDelta = new Vector2(8f, 0.6f);
        }
        #endregion

        #region Pen & Perimeter Helpers
        private static void CreatePasturePen(Transform parent, Sprite fenceH, Sprite fenceV, Sprite crackOverlay, Sprite brokenSp)
        {
            // Cạnh trên (y = 3, x từ -7 đến 1)
            for (float x = -7f; x <= 1f; x += 1.0f)
            {
                CreatePenFencePiece(parent, new Vector3(x, 3f, 0f), fenceH, crackOverlay, brokenSp);
            }
            // Cạnh dưới (y = -4, x từ -7 đến 1)
            for (float x = -7f; x <= 1f; x += 1.0f)
            {
                CreatePenFencePiece(parent, new Vector3(x, -4f, 0f), fenceH, crackOverlay, brokenSp);
            }
            // Cạnh phải (x = 1, y từ -3 đến 2)
            for (float y = -3f; y <= 2f; y += 1.0f)
            {
                CreatePenFencePiece(parent, new Vector3(1f, y, 0f), fenceV, crackOverlay, brokenSp);
            }
            // Cạnh trái (x = -7, y từ -3 đến 2, chừa y = 0 làm cổng chuồng)
            for (float y = -3f; y <= 2f; y += 1.0f)
            {
                if (Mathf.Abs(y) < 0.2f) continue;
                CreatePenFencePiece(parent, new Vector3(-7f, y, 0f), fenceV, crackOverlay, brokenSp);
            }
        }

        private static void CreatePenFencePiece(Transform parent, Vector3 pos, Sprite sprite, Sprite crackOverlay, Sprite brokenSp)
        {
            var fenceObj = new GameObject($"Fence_{pos.x:F0}_{pos.y:F0}");
            fenceObj.transform.SetParent(parent, false);
            fenceObj.transform.position = pos;

            var sr = fenceObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            var col = fenceObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);

            var fence = fenceObj.AddComponent<Fence2DView>();
            fence.MaxHp = 120f;
            fence.CurrentHp = 120f;
            fence.BrokenSprite = brokenSp;

            if (crackOverlay != null)
            {
                var crackObj = new GameObject("DamageCrackOverlay");
                crackObj.transform.SetParent(fenceObj.transform, false);
                crackObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                var crackSr = crackObj.AddComponent<SpriteRenderer>();
                crackSr.sprite = crackOverlay;
                crackSr.sortingOrder = sr.sortingOrder + 1;
                crackObj.SetActive(false);
                fence.DamageCrackOverlay = crackObj;
            }
        }

        private static void CreatePasturePig(Transform parent, string id, string name, GeneLineId gene, Vector3 pos, Sprite idleSprite)
        {
            var pigObj = new GameObject($"Pig_{name}");
            pigObj.transform.SetParent(parent, false);
            pigObj.transform.position = pos;

            var sr = pigObj.AddComponent<SpriteRenderer>();
            sr.sprite = idleSprite;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            var rb = pigObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = pigObj.AddComponent<CircleCollider2D>();
            col.offset = new Vector2(0f, -0.12f);
            col.radius = 0.32f;

            var pigView = pigObj.AddComponent<PigAgentView>();
            pigView.SpriteRenderer = sr;
            pigView.EnsureThoughtBubble();

            var pigModel = Pig.CreateDefault(id, name, gene, (gene == GeneLineId.HuThe) ? GeneRarity.DiBien : ((gene == GeneLineId.KimTho) ? GeneRarity.Hiem : GeneRarity.Thuong));
            pigView.Bind(pigModel);
        }

        private static void CreateDummy(Transform parent, string name, Vector3 pos, int resilience, Color color)
        {
            var dummyObj = new GameObject(name);
            dummyObj.transform.SetParent(parent, false);
            dummyObj.transform.position = pos;

            string spritePath = resilience >= 30 
                ? "Assets/Art/Sprites/Environment/Training/training_dummy_steel.png" 
                : (resilience >= 15 ? "Assets/Art/Sprites/Environment/Training/training_dummy_iron.png" : "Assets/Art/Sprites/Environment/Training/training_dummy_straw.png");

            var sr = dummyObj.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spritePath, null) ?? LoadSprite("Assets/Art/Sprites/Environment/Fences/fence_post.png", null);
            sr.color = Color.white;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);
            dummyObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            var col = dummyObj.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.6f, 1.0f);

            var rb = dummyObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = 8f;
            rb.freezeRotation = true;

            var dummy = dummyObj.AddComponent<TrainingDummy2DView>();
            dummy.DummyName = name;
            dummy.Resilience = resilience;
            dummy.SpriteRenderer = sr;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dummyObj.transform, false);
            var tm = labelObj.AddComponent<TextMeshPro>();
            tm.rectTransform.localPosition = new Vector3(0f, 0.85f, 0f);
            tm.rectTransform.anchoredPosition = new Vector2(0f, 0.85f);
            tm.text = $"{name}\n<color=#67e8f9>[Giáp: {resilience}]</color>";
            tm.fontSize = 2.0f;
            tm.color = Color.white;
            tm.alignment = TextAlignmentOptions.Center;
            tm.rectTransform.sizeDelta = new Vector2(2.5f, 0.8f);
        }

        private static void CreateForestPerimeter(Transform parent, Sprite treeSp, Sprite rockSp, float halfW, float halfH)
        {
            for (float x = -halfW; x <= halfW; x += 1.8f)
            {
                CreateProp(parent, "Tree_Top", new Vector3(x, halfH, 0), treeSp, new Vector2(1.2f, 0.8f), new Vector2(0f, -0.5f));
                CreateProp(parent, "Tree_Bottom", new Vector3(x, -halfH, 0), treeSp, new Vector2(1.2f, 0.8f), new Vector2(0f, -0.5f));
            }
            for (float y = -halfH; y <= halfH; y += 1.8f)
            {
                CreateProp(parent, "Tree_Left", new Vector3(-halfW, y, 0), treeSp, new Vector2(1.2f, 0.8f), new Vector2(0f, -0.5f));
                CreateProp(parent, "Tree_Right", new Vector3(halfW, y, 0), treeSp, new Vector2(1.2f, 0.8f), new Vector2(0f, -0.5f));
            }

            CreateProp(parent, "Rock_NW", new Vector3(-halfW + 1.5f, halfH - 1.5f, 0), rockSp, new Vector2(1.0f, 0.8f), Vector2.zero);
            CreateProp(parent, "Rock_NE", new Vector3(halfW - 1.5f, halfH - 1.5f, 0), rockSp, new Vector2(1.0f, 0.8f), Vector2.zero);
            CreateProp(parent, "Rock_SW", new Vector3(-halfW + 1.5f, -halfH + 1.5f, 0), rockSp, new Vector2(1.0f, 0.8f), Vector2.zero);
            CreateProp(parent, "Rock_SE", new Vector3(halfW - 1.5f, -halfH + 1.5f, 0), rockSp, new Vector2(1.0f, 0.8f), Vector2.zero);
        }

        private static void CreateProp(Transform parent, string name, Vector3 pos, Sprite sprite, Vector2 colSize, Vector2 colOffset, int sortOffset = 0)
        {
            if (sprite == null) return;
            var propObj = new GameObject(name);
            propObj.transform.SetParent(parent, false);
            propObj.transform.position = pos;

            var sr = propObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100) + sortOffset;

            var col = propObj.AddComponent<BoxCollider2D>();
            col.size = colSize;
            col.offset = colOffset;
        }

        private static Sprite LoadSprite(string path, Sprite fallback)
        {
            if (string.IsNullOrEmpty(path)) return fallback;
            if (!path.StartsWith("Assets/")) path = "Assets/" + path;

            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in allAssets)
            {
                if (a is Sprite subSprite) return subSprite;
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32);
            }

            var dynSprite = UISpriteLoader.LoadSprite(path);
            if (dynSprite != null) return dynSprite;

            return fallback;
        }
        #endregion
    }
}
