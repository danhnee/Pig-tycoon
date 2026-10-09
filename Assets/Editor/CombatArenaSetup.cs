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
    public static class CombatArenaSetup
    {
        private const string ArenaScenePath = "Assets/Scenes/CombatTestArena2D.unity";

        [MenuItem("PigTycoon/3. Tạo & Mở Map Test Combat (Arena 2D)")]
        public static void CreateAndOpenCombatArenaScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "PigTycoon - Cảnh Báo",
                    "Unity đang ở chế độ Play Mode.\nVui lòng tắt nút Play trước khi tạo hoặc nạp Scene Đấu Trường Combat!",
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

            // Nạp Sprite môi trường & nhân vật
            Sprite stoneSp = LoadSprite("Assets/Art/Tiles/stone_path.png", defaultSquare);
            Sprite grassSp = LoadSprite("Assets/Art/Tiles/grass_base.png", defaultSquare);
            Sprite dirtSp = LoadSprite("Assets/Art/Tiles/dirt_trail.png", defaultSquare);
            Sprite fenceHSp = LoadSprite("Assets/Art/Sprites/Environment/fence_h.png", defaultSquare);
            Sprite fenceVSp = LoadSprite("Assets/Art/Sprites/Environment/fence_v.png", fenceHSp);
            Sprite towerSp = LoadSprite("Assets/Art/Sprites/Environment/defense_tower.png", defaultKnob);

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

            // 1. Main Camera 2D
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.28f); // Màu nền đấu võ trường đêm huyền bí
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var camFollow = camObj.AddComponent<CameraFollow2D>();
            camFollow.TargetOrthoSize = 5.6f;
            camFollow.SmoothSpeed = 8.0f;
            camFollow.ClampToFarmBounds = false;

            camObj.AddComponent<CameraShake2D>();

            // 2. Global Light 2D (Ánh sáng đấu trường ấm áp)
            var lightObj = new GameObject("Arena Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(1.0f, 0.95f, 0.88f);
            light2D.intensity = 1.15f;

            // 3. Game Controller & Spawner
            var gameCtrlObj = new GameObject("GameController");
            gameCtrlObj.AddComponent<MobileGameController>();

            var waveSpawnerObj = new GameObject("EnemyWaveSpawner");
            waveSpawnerObj.AddComponent<EnemyWaveSpawner>();

            // 4. Arena Floor & Walls
            var arenaRoot = new GameObject("Arena_Environment");

            // Tạo sàn đá đấu trường (18m x 12m)
            var floorRoot = new GameObject("Floor_Tiles");
            floorRoot.transform.SetParent(arenaRoot.transform, false);

            for (int x = -9; x <= 9; x++)
            {
                for (int y = -6; y <= 6; y++)
                {
                    var tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.SetParent(floorRoot.transform, false);
                    tileObj.transform.position = new Vector3(x, y, 0);

                    var sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = (Mathf.Abs(x) <= 3 && Mathf.Abs(y) <= 3) ? stoneSp : dirtSp;
                    sr.sortingOrder = -10000;
                    sr.color = (Mathf.Abs(x) <= 3 && Mathf.Abs(y) <= 3) 
                        ? new Color(0.9f, 0.9f, 0.95f, 1f) 
                        : new Color(0.75f, 0.72f, 0.7f, 1f);
                }
            }

            // Tạo tường rào bao quanh đấu trường có va chạm vật lý (Không cho chạy ra ngoài)
            var fencesRoot = new GameObject("Perimeter_Fences");
            fencesRoot.transform.SetParent(arenaRoot.transform, false);

            CreateFenceBorder(fencesRoot.transform, fenceHSp, fenceVSp, 9.5f, 6.5f);

            // 5. Training Dummies (3 Bù nhìn luyện công với các mức phòng thủ)
            var dummyRoot = new GameObject("Training_Dummies");
            dummyRoot.transform.SetParent(arenaRoot.transform, false);

            CreateDummy(dummyRoot.transform, "Bù Nhìn Rơm (0 Giáp)", new Vector3(4.5f, 0f, 0f), 0, new Color(0.95f, 0.85f, 0.45f));
            CreateDummy(dummyRoot.transform, "Bù Nhìn Sắt (15 Giáp)", new Vector3(4.5f, 3.2f, 0f), 15, new Color(0.6f, 0.7f, 0.85f));
            CreateDummy(dummyRoot.transform, "Bù Nhìn Thép (30 Giáp)", new Vector3(4.5f, -3.2f, 0f), 30, new Color(0.85f, 0.45f, 0.4f));

            // 6. Turret Bắn Đạn Luyện Phản Đòn (Projectile Deflection Station)
            var turretStationObj = new GameObject("Test_Turret_Station");
            turretStationObj.transform.SetParent(arenaRoot.transform, false);
            turretStationObj.transform.position = new Vector3(0f, 6.2f, 0f);

            var turretSr = turretStationObj.AddComponent<SpriteRenderer>();
            turretSr.sprite = towerSp;
            turretSr.sortingOrder = 200;
            turretStationObj.transform.localScale = new Vector3(1.8f, 2.2f, 1f);

            var turretLabel = new GameObject("Turret_Label");
            turretLabel.transform.SetParent(turretStationObj.transform, false);
            turretLabel.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var labelMesh = turretLabel.AddComponent<TextMeshPro>();
            labelMesh.text = "🎯 MÁY BẮN ĐẠN (DEFLECT TEST)";
            labelMesh.fontSize = 3.5f;
            labelMesh.color = new Color(1f, 0.85f, 0.3f, 1f);
            labelMesh.alignment = TextAlignmentOptions.Center;

            // 7. Player (Khoa - Trang bị Đầy Đủ Vũ Khí & Combat)
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
            var combatCtrl = playerObj.AddComponent<PlayerCombatController>();
            playerObj.AddComponent<PlayerInteractionController>();

            camFollow.Target = playerObj.transform;

            // 8. Canvas UI Đấu Trường
            var canvasObj = new GameObject("Combat Arena Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

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

            // 9. Combat Arena Manager & Dashboard Controller
            var arenaManagerObj = new GameObject("CombatArenaManager");
            var arenaCtrl = arenaManagerObj.AddComponent<CombatArenaController>();
            arenaCtrl.PlayerSpawnPoint = playerObj.transform;
            arenaCtrl.DummySpawnRoot = dummyRoot.transform;
            arenaCtrl.ProjectileTurretPoint = turretStationObj.transform;

            // Lưu Scene vào Assets/Scenes/CombatTestArena2D.unity
            EditorSceneManager.SaveScene(scene, ArenaScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[PigTycoon] ĐÃ TẠO THÀNH CÔNG SCENE TEST COMBAT TẠI: {ArenaScenePath}</color>");
        }

        private static void CreateDummy(Transform parent, string name, Vector3 pos, int resilience, Color color)
        {
            var dummyObj = new GameObject(name);
            dummyObj.transform.SetParent(parent, false);
            dummyObj.transform.position = pos;

            var sr = dummyObj.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Art/Sprites/Characters/enemy_thief_down.png", null) 
                        ?? LoadSprite("Art/Sprites/Environment/fence_post.png", null);
            sr.color = color;
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

        private static void CreateFenceBorder(Transform parent, Sprite fenceH, Sprite fenceV, float halfWidth, float halfHeight)
        {
            // Tường trên & dưới (Horizontal)
            for (float x = -halfWidth; x <= halfWidth; x += 1.0f)
            {
                CreateWallPiece(parent, fenceH, new Vector3(x, halfHeight, 0));
                CreateWallPiece(parent, fenceH, new Vector3(x, -halfHeight, 0));
            }

            // Tường trái & phải (Vertical)
            for (float y = -halfHeight; y <= halfHeight; y += 1.0f)
            {
                CreateWallPiece(parent, fenceV, new Vector3(-halfWidth, y, 0));
                CreateWallPiece(parent, fenceV, new Vector3(halfWidth, y, 0));
            }
        }

        private static void CreateWallPiece(Transform parent, Sprite sprite, Vector3 pos)
        {
            var wall = new GameObject("Wall");
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;

            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            var col = wall.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
        }

        private static Sprite LoadSprite(string path, Sprite fallback)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32);
            }
            return fallback;
        }
    }
}
