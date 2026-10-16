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
            cam.orthographicSize = 6.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.22f, 0.38f, 0.24f); // Màu xanh thảo nguyên dịu mắt
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var camFollow = camObj.AddComponent<CameraFollow2D>();
            camFollow.TargetOrthoSize = 6.2f;
            camFollow.SmoothSpeed = 8.0f;
            camFollow.ClampToFarmBounds = false;

            camObj.AddComponent<CameraShake2D>();

            // 2. Global Light 2D (Ánh sáng đồng cỏ ấm áp)
            var lightObj = new GameObject("Arena Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(1.0f, 0.98f, 0.92f);
            light2D.intensity = 1.15f;

            // 3. Game Controller & Spawner
            var gameCtrlObj = new GameObject("GameController");
            gameCtrlObj.AddComponent<MobileGameController>();

            var waveSpawnerObj = new GameObject("EnemyWaveSpawner");
            waveSpawnerObj.AddComponent<EnemyWaveSpawner>();

            // 4. Môi trường Đồng Cỏ Thảo Nguyên (Pasture Meadow: 33m x 23m)
            var arenaRoot = new GameObject("Arena_Environment");

            var floorRoot = new GameObject("Pasture_Floor_Tiles");
            floorRoot.transform.SetParent(arenaRoot.transform, false);

            for (int x = -16; x <= 16; x++)
            {
                for (int y = -11; y <= 11; y++)
                {
                    var tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.SetParent(floorRoot.transform, false);
                    tileObj.transform.position = new Vector3(x, y, 0);

                    var sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -10000;

                    // Đường đất mòn nối từ bìa rừng phía Tây vào cổng chuồng heo
                    bool isDirtRoad = (y == 0 && x >= -16 && x <= -7) || (x == -7 && y >= -1 && y <= 1);
                    // Sàn đá cho góc luyện võ thao trường
                    bool isTrainingStone = (x >= 6 && x <= 11 && y >= -4 && y <= 4);

                    if (isTrainingStone)
                    {
                        sr.sprite = stoneSp;
                        sr.color = new Color(0.92f, 0.92f, 0.96f, 1f);
                    }
                    else if (isDirtRoad)
                    {
                        sr.sprite = dirtSp;
                        sr.color = new Color(0.9f, 0.85f, 0.78f, 1f);
                    }
                    else
                    {
                        // Đồng cỏ với hoa cỏ và cỏ cao tự nhiên
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

            // 5. Viền Bìa Rừng (Cây cối, đá tảng và ranh giới map)
            var forestRoot = new GameObject("Forest_Border");
            forestRoot.transform.SetParent(arenaRoot.transform, false);
            CreateForestPerimeter(forestRoot.transform, treeSp, rockSp, 16.5f, 11.5f);

            // 6. Khu Chuồng Heo Đồng Cỏ (Pasture Pig Pen: x in [-7, 1], y in [-4, 3])
            var penRoot = new GameObject("Pasture_Pig_Pen");
            penRoot.transform.SetParent(arenaRoot.transform, false);
            CreatePasturePen(penRoot.transform, fenceHSp, fenceVSp, fenceCrackSp, fenceBrokenSp);

            // Các công trình chuồng trại bên trong chuồng
            CreateProp(penRoot.transform, "ShelterBarn", new Vector3(-5.2f, 1.5f, 0f), barnSp, new Vector2(2.5f, 1.8f), new Vector2(0f, -0.2f), 10);
            CreateProp(penRoot.transform, "FeedSilo", new Vector3(-2.8f, 1.8f, 0f), siloSp, new Vector2(1.2f, 1.5f), Vector2.zero, 15);
            CreateProp(penRoot.transform, "FeederTrough", new Vector3(-4.5f, -1.5f, 0f), feederSp, new Vector2(1.2f, 0.6f), Vector2.zero, 5);
            CreateProp(penRoot.transform, "WaterTrough", new Vector3(-1.5f, -1.5f, 0f), waterTroughSp, new Vector2(1.2f, 0.6f), Vector2.zero, 5);

            // 7. Bầy Heo Đồng Cỏ (4 Chú Heo để kiểm thử cướp heo)
            var pigsRoot = new GameObject("Pasture_Pigs");
            pigsRoot.transform.SetParent(arenaRoot.transform, false);

            CreatePasturePig(pigsRoot.transform, "pig_hong_dien", "Ỉn Béo", GeneLineId.HongDien, new Vector3(-3.8f, 0.2f, 0f), pigHongDien);
            CreatePasturePig(pigsRoot.transform, "pig_lam_khe", "Lam Lam", GeneLineId.LamKhe, new Vector3(-1.8f, 0.6f, 0f), pigLamKhe);
            CreatePasturePig(pigsRoot.transform, "pig_kim_tho", "Kim Kim", GeneLineId.KimTho, new Vector3(-2.8f, -2.6f, 0f), pigKimTho);
            CreatePasturePig(pigsRoot.transform, "pig_thao_nguyen", "Heo Thảo Nguyên", GeneLineId.HongDien, new Vector3(3.5f, -1.8f, 0f), pigHongDien);

            // 8. Khu Thao Trường Võ Học (3 Bù nhìn + Tháp canh + Máy bắn đạn)
            var trainingRoot = new GameObject("Combat_Training_Corner");
            trainingRoot.transform.SetParent(arenaRoot.transform, false);

            CreateDummy(trainingRoot.transform, "Bù Nhìn Rơm (0 Giáp)", new Vector3(8.5f, 0f, 0f), 0, Color.white);
            CreateDummy(trainingRoot.transform, "Bù Nhìn Sắt (15 Giáp)", new Vector3(8.5f, 2.8f, 0f), 15, Color.white);
            CreateDummy(trainingRoot.transform, "Bù Nhìn Thép (30 Giáp)", new Vector3(8.5f, -2.8f, 0f), 30, Color.white);

            // Tháp Canh Phòng Thủ
            var towerObj = new GameObject("Defense_Tower_BachVan");
            towerObj.transform.SetParent(trainingRoot.transform, false);
            towerObj.transform.position = new Vector3(5.5f, 3.8f, 0f);
            var towerView = towerObj.AddComponent<DefenseTower2DView>();
            towerView.MaxHp = 500f;
            towerView.CurrentHp = 500f;
            var towerSr = towerObj.AddComponent<SpriteRenderer>();
            towerSr.sprite = towerSp;
            towerSr.sortingOrder = Mathf.RoundToInt(-3.8f * 100);
            towerObj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            var towerCol = towerObj.AddComponent<CircleCollider2D>();
            towerCol.radius = 0.5f;

            // Turret Bắn Đạn Luyện Phản Đòn
            var turretStationObj = new GameObject("Test_Turret_Station");
            turretStationObj.transform.SetParent(trainingRoot.transform, false);
            turretStationObj.transform.position = new Vector3(8.5f, 5.5f, 0f);
            var turretSr = turretStationObj.AddComponent<SpriteRenderer>();
            turretSr.sprite = towerSp;
            turretSr.sortingOrder = 200;
            turretStationObj.transform.localScale = new Vector3(1.4f, 1.6f, 1f);

            var turretLabel = new GameObject("Turret_Label");
            turretLabel.transform.SetParent(turretStationObj.transform, false);
            turretLabel.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var labelMesh = turretLabel.AddComponent<TextMeshPro>();
            labelMesh.text = "🎯 MÁY BẮN ĐẠN (DEFLECT TEST)";
            labelMesh.fontSize = 3.2f;
            labelMesh.color = new Color(1f, 0.85f, 0.3f, 1f);
            labelMesh.alignment = TextAlignmentOptions.Center;

            // 9. Player (Khoa - Trang bị Đầy Đủ Vũ Khí & Combat)
            var playerObj = new GameObject("Player (Khoa)");
            playerObj.transform.position = new Vector3(3.0f, 0.5f, 0f);
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
            var combatCtrl = playerObj.AddComponent<PlayerCombatController>();
            playerObj.AddComponent<PlayerInteractionController>();

            camFollow.Target = playerObj.transform;

            // 10. Canvas UI Đấu Trường
            var canvasObj = new GameObject("Combat Arena Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 10.1. Dev Mode Popup
            var devMode = canvasObj.AddComponent<DevModePopup>();
            devMode.EnsureUIExists();

            // 10.2. EventSystem cho tương tác UI
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

            // 11. Combat Arena Manager & Dashboard Controller
            var arenaManagerObj = new GameObject("CombatArenaManager");
            var arenaCtrl = arenaManagerObj.AddComponent<CombatArenaController>();
            arenaCtrl.PlayerSpawnPoint = playerObj.transform;
            arenaCtrl.DummySpawnRoot = trainingRoot.transform;
            arenaCtrl.ProjectileTurretPoint = turretStationObj.transform;

            // Lưu Scene vào Assets/Scenes/CombatTestArena2D.unity
            EditorSceneManager.SaveScene(scene, ArenaScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[PigTycoon] ĐÃ TẠO THÀNH CÔNG SCENE ĐỒNG CỎ TEST COMBAT TẠI: {ArenaScenePath}</color>");
        }

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
                if (Mathf.Abs(y) < 0.2f) continue; // Cổng chuồng mở cho heo và người đi lại
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

            // DamageCrackOverlay child
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

            // Nhãn Thought Bubble trên đầu
            var thoughtObj = new GameObject("ThoughtBubble");
            thoughtObj.transform.SetParent(pigObj.transform, false);
            thoughtObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            var tm = thoughtObj.AddComponent<TextMeshPro>();
            tm.fontSize = 3.5f;
            tm.alignment = TextAlignmentOptions.Center;
            thoughtObj.SetActive(false);
            pigView.ThoughtBubbleText = tm;

            // Model Heo
            var pigModel = Pig.CreateDefault(id, name, gene, GeneRarity.Thuong);
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

            // Nhãn hiển thị tên và giáp trên đầu
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dummyObj.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            var tm = labelObj.AddComponent<TextMeshPro>();
            tm.text = $"{name}\n<color=#67e8f9>[Giáp: {resilience}]</color>";
            tm.fontSize = 2.8f;
            tm.color = Color.white;
            tm.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateForestPerimeter(Transform parent, Sprite treeSp, Sprite rockSp, float halfW, float halfH)
        {
            // Viền cây phía trên và dưới
            for (float x = -halfW; x <= halfW; x += 1.8f)
            {
                CreateProp(parent, "Tree_Top", new Vector3(x, halfH, 0), treeSp, new Vector2(1.2f, 0.8f), new Vector2(0f, -0.5f));
                CreateProp(parent, "Tree_Bottom", new Vector3(x, -halfH, 0), treeSp, new Vector2(1.2f, 0.8f), new Vector2(0f, -0.5f));
            }
            // Viền cây phía trái và phải
            for (float y = -halfH; y <= halfH; y += 1.8f)
            {
                CreateProp(parent, "Tree_Left", new Vector3(-halfW, y, 0), treeSp, new Vector2(1.2f, 0.8f), new Vector2(0f, -0.5f));
                CreateProp(parent, "Tree_Right", new Vector3(halfW, y, 0), treeSp, new Vector2(1.2f, 0.8f), new Vector2(0f, -0.5f));
            }

            // Điểm xuyết tảng đá ở 4 góc bìa rừng
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
    }
}
