using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class CombatArenaController : MonoBehaviour
    {
        public static CombatArenaController Instance { get; private set; }

        [Header("Arena Location")]
        public Vector2 ArenaCenter = new Vector2(28f, 0f);
        public Vector2 ArenaSize = new Vector2(16f, 12f);

        [Header("Runtime References")]
        private readonly List<EnemyAgentView> arenaEnemies = new List<EnemyAgentView>();
        private GameObject arenaMiniHUD;
        private bool isPlayerInArena = false;

        public bool IsPlayerInArena => isPlayerInArena;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildArenaEnvironment();
            CreateArenaMiniHUD();
        }

        private void Update()
        {
            CheckPlayerProximity();
        }

        private void CheckPlayerProximity()
        {
            var player = PlayerCombatController.Instance;
            if (player == null) return;

            Vector2 pPos = player.Position;
            bool inside = (pPos.x >= ArenaCenter.x - ArenaSize.x * 0.5f - 1f &&
                           pPos.x <= ArenaCenter.x + ArenaSize.x * 0.5f + 1f &&
                           pPos.y >= ArenaCenter.y - ArenaSize.y * 0.5f - 1f &&
                           pPos.y <= ArenaCenter.y + ArenaSize.y * 0.5f + 1f);

            if (inside != isPlayerInArena)
            {
                isPlayerInArena = inside;
                if (arenaMiniHUD != null)
                {
                    arenaMiniHUD.SetActive(isPlayerInArena);
                }

                if (isPlayerInArena)
                {
                    PlayerInteractionController.Instance?.ShowFeedback("Đã bước vào VÕ TRƯỜNG THẢO NGUYÊN (Khu vực Test Combat)!");
                }
            }
        }

        private void BuildArenaEnvironment()
        {
            // 1. Tạo GameObject gốc chứa Võ Trường
            var arenaRoot = new GameObject("Combat_Training_Arena");
            arenaRoot.transform.position = new Vector3(ArenaCenter.x, ArenaCenter.y, 0f);

            // 2. Dựng hàng rào bao quanh sân tập (16m x 12m)
            BuildArenaFences(arenaRoot.transform);

            // 3. Biển hiệu Võ Trường tại cổng vào
            BuildArenaSignboard(arenaRoot.transform);

            // 4. Bố trí các Bù Nhìn Tập Đánh (Practice Dummies)
            BuildDummyTargets(arenaRoot.transform);

            // 5. Bố trí Tháp Nỏ Xuyên Vân thử nghiệm
            BuildTrainingDefenseTower(arenaRoot.transform);

            // 6. Bố trí Giá Vũ Khí trực quan
            BuildWeaponRackVisuals(arenaRoot.transform);
        }

        private void BuildArenaFences(Transform parent)
        {
            float halfW = ArenaSize.x * 0.5f;
            float halfH = ArenaSize.y * 0.5f;

            // Đặt các cọc rào bao quanh (bỏ trống 1 khoảng ở phía Tây làm lối vào x = -halfW, y từ -1.5 đến 1.5)
            for (float x = -halfW; x <= halfW; x += 1.2f)
            {
                // Biên Bắc & Biên Nam
                CreateFencePost(parent, new Vector2(ArenaCenter.x + x, ArenaCenter.y + halfH));
                CreateFencePost(parent, new Vector2(ArenaCenter.x + x, ArenaCenter.y - halfH));
            }

            for (float y = -halfH; y <= halfH; y += 1.2f)
            {
                // Biên Đông
                CreateFencePost(parent, new Vector2(ArenaCenter.x + halfW, ArenaCenter.y + y));

                // Biên Tây (Lối vào ở giữa y: -1.2 đến 1.2 không đặt rào)
                if (Mathf.Abs(y) > 1.4f)
                {
                    CreateFencePost(parent, new Vector2(ArenaCenter.x - halfW, ArenaCenter.y + y));
                }
            }
        }

        private void CreateFencePost(Transform parent, Vector2 pos)
        {
            var post = new GameObject("Arena_FencePost");
            post.transform.SetParent(parent, false);
            post.transform.position = new Vector3(pos.x, pos.y, 0f);

            var sr = post.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFenceSprite(false, true, false, true) ?? UISpriteLoader.LoadSprite("Art/Sprites/Environment/fence_v.png", 32);
            sr.color = new Color(0.85f, 0.75f, 0.6f, 1f);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            var col = post.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.7f, 0.7f);
            col.isTrigger = false;
        }

        private void BuildArenaSignboard(Transform parent)
        {
            var signObj = new GameObject("Arena_Signboard");
            signObj.transform.SetParent(parent, false);
            signObj.transform.position = new Vector3(ArenaCenter.x - ArenaSize.x * 0.5f, ArenaCenter.y + 2.2f, 0f);

            var sr = signObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFrameWoodPanel();
            sr.color = new Color(0.75f, 0.55f, 0.35f, 1f);
            sr.sortingOrder = 50;
            signObj.transform.localScale = new Vector3(0.8f, 0.5f, 1f);

            var textObj = new GameObject("SignText");
            textObj.transform.SetParent(signObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = "<b>VÕ TRƯỜNG THẢO NGUYÊN</b>\n<size=70%>[KHU VỰC TEST COMBAT]</size>";
            tmp.fontSize = 3.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.95f, 0.4f, 1f);
            tmp.sortingOrder = 200;
        }

        private void BuildDummyTargets(Transform parent)
        {
            // 1. Bù nhìn Tiêu Chuẩn (200 HP)
            CreateDummy(parent, new Vector2(ArenaCenter.x - 1f, ArenaCenter.y + 2.5f), "Bù Nhìn Tiêu Chuẩn", 200f, new Color(0.95f, 0.8f, 0.4f, 1f));

            // 2. Bù nhìn Thiết Giáp (500 HP, test Búa Tạ Smash & Knockback)
            CreateDummy(parent, new Vector2(ArenaCenter.x - 1f, ArenaCenter.y - 2.5f), "Bù Nhìn Thiết Giáp", 500f, new Color(0.6f, 0.65f, 0.75f, 1f));

            // 3. Nhóm 3 bù nhìn thẳng hàng (test quét Slash 120° & đâm xuyên Thrust 2.0m)
            CreateDummy(parent, new Vector2(ArenaCenter.x + 3.2f, ArenaCenter.y + 2.0f), "Bù Nhìn Hàng 1", 150f, new Color(0.9f, 0.7f, 0.5f, 1f));
            CreateDummy(parent, new Vector2(ArenaCenter.x + 3.2f, ArenaCenter.y), "Bù Nhìn Hàng 2", 150f, new Color(0.9f, 0.7f, 0.5f, 1f));
            CreateDummy(parent, new Vector2(ArenaCenter.x + 3.2f, ArenaCenter.y - 2.0f), "Bù Nhìn Hàng 3", 150f, new Color(0.9f, 0.7f, 0.5f, 1f));
        }

        private void CreateDummy(Transform parent, Vector2 pos, string name, float hp, Color tint)
        {
            var dummyObj = new GameObject($"Dummy_{name}");
            dummyObj.transform.SetParent(parent, false);
            dummyObj.transform.position = new Vector3(pos.x, pos.y, 0f);

            var sr = dummyObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_action.png", 32);
            sr.color = tint;
            sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100);

            var col = dummyObj.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.8f, 1.0f);
            col.isTrigger = false;

            var rb = dummyObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = 8f;
            rb.freezeRotation = true;

            var dummy = dummyObj.AddComponent<DummyPracticeTarget>();
            dummy.MaxHealth = hp;
            dummy.CurrentHealth = hp;

            // Nhãn tên phía trên bù nhìn
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dummyObj.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = $"<size=80%>{name}</size>\n<color=#55ff55>{hp} HP</color>";
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 250;
        }

        private void BuildTrainingDefenseTower(Transform parent)
        {
            var towerObj = new GameObject("Arena_DefenseTower");
            towerObj.transform.SetParent(parent, false);
            Vector2 towerPos = new Vector2(ArenaCenter.x - 4.5f, ArenaCenter.y - 3.5f);
            towerObj.transform.position = new Vector3(towerPos.x, towerPos.y, 0f);

            var sr = towerObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.LoadSprite("Art/Sprites/Environment/rock_boulder.png", 32);
            sr.color = new Color(0.85f, 0.65f, 0.45f, 1f);
            sr.sortingOrder = Mathf.RoundToInt(-towerPos.y * 100);

            var col = towerObj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = false;

            var tower = towerObj.AddComponent<DefenseTower2DView>();
            tower.RangeRadius = 7.5f;

            var bp = new DefenseBuildingInstance
            {
                InstanceId = "arena_tower_1",
                BlueprintId = "NoXuyenVan",
                Name = "Nỏ Xuyên Vân Thử Nghiệm",
                CurrentHp = 1500,
                MaxHp = 1500,
                AmmoCurrent = 999,
                AmmoCapacity = 999,
                CooldownSeconds = 1.0f,
                AmmoCostPerShot = 1
            };
            tower.Bind(bp);

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(towerObj.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = "<color=#ffd700>Nỏ Xuyên Vân (7.5m)</color>";
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 250;
        }

        private void BuildWeaponRackVisuals(Transform parent)
        {
            var rackObj = new GameObject("Arena_WeaponRack");
            rackObj.transform.SetParent(parent, false);
            Vector2 rackPos = new Vector2(ArenaCenter.x - 4.5f, ArenaCenter.y + 3.5f);
            rackObj.transform.position = new Vector3(rackPos.x, rackPos.y, 0f);

            var sr = rackObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetFrameWoodPanel();
            sr.color = new Color(0.6f, 0.4f, 0.25f, 1f);
            sr.sortingOrder = Mathf.RoundToInt(-rackPos.y * 100);
            rackObj.transform.localScale = new Vector3(1.2f, 0.6f, 1f);

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rackObj.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = "<color=#ffcc00>GIÁ VŨ KHÍ TẬP VÕ</color>";
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 250;
        }

        #region Arena Mini-HUD in Top-Left when inside Arena
        private void CreateArenaMiniHUD()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            arenaMiniHUD = new GameObject("Arena_Testing_Panel");
            arenaMiniHUD.transform.SetParent(canvas.transform, false);

            var rt = arenaMiniHUD.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(15f, -70f);
            rt.sizeDelta = new Vector2(330f, 230f);

            var bgImg = arenaMiniHUD.AddComponent<Image>();
            bgImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.12f, 0.14f, 0.18f, 0.92f);

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(arenaMiniHUD.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -6);
            tRt.sizeDelta = new Vector2(0, 24);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<color=#ffd700><b>⚔ BẢNG ĐIỀU KHIỂN VÕ TRƯỜNG</b></color>";
            titleTmp.fontSize = 13.5f;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Row 1: Đổi 4 vũ khí
            CreateArenaButton(arenaMiniHUD.transform, "Đao Gỗ", () => EquipWeapon("dao_go"), new Vector2(10, -36), new Vector2(72, 28));
            CreateArenaButton(arenaMiniHUD.transform, "Kiếm Sắt", () => EquipWeapon("kiem_sat"), new Vector2(88, -36), new Vector2(72, 28));
            CreateArenaButton(arenaMiniHUD.transform, "Búa Tạ", () => EquipWeapon("bua_ta"), new Vector2(166, -36), new Vector2(72, 28));
            CreateArenaButton(arenaMiniHUD.transform, "Giáo Tre", () => EquipWeapon("giao_tre"), new Vector2(244, -36), new Vector2(72, 28));

            // Row 2: Triệu hồi quái vật thử nghiệm
            CreateArenaButton(arenaMiniHUD.transform, "+ Kẻ Bắt Heo", () => SpawnArenaEnemy(EnemyTypeId.KeBatHeo), new Vector2(10, -72), new Vector2(98, 28));
            CreateArenaButton(arenaMiniHUD.transform, "+ 3 Chuột Cám", () => SpawnMultiplePests(3), new Vector2(114, -72), new Vector2(98, 28));
            CreateArenaButton(arenaMiniHUD.transform, "+ Kẻ Cào Phá", () => SpawnArenaEnemy(EnemyTypeId.KeCaoPha), new Vector2(218, -72), new Vector2(98, 28));

            // Row 3: Hành động chiến đấu
            CreateArenaButton(arenaMiniHUD.transform, "Lướt Chém x1.5", ExecuteDashSlash, new Vector2(10, -108), new Vector2(148, 28));
            CreateArenaButton(arenaMiniHUD.transform, "Nộ Ba Chấn Địa", ExecuteRageShockwave, new Vector2(168, -108), new Vector2(148, 28));

            // Row 4: Tiện ích
            CreateArenaButton(arenaMiniHUD.transform, "Hồi 100 HP & Nộ", RefillCombatStats, new Vector2(10, -144), new Vector2(148, 28));
            CreateArenaButton(arenaMiniHUD.transform, "<color=#ff5555>Dọn Sạch Quái</color>", ClearArenaEnemies, new Vector2(168, -144), new Vector2(148, 28));

            // Row 5: Dịch chuyển về tâm trang trại
            CreateArenaButton(arenaMiniHUD.transform, "Quay Về Tâm Nông Trại", TeleportToFarmCenter, new Vector2(10, -180), new Vector2(306, 28));

            arenaMiniHUD.SetActive(false); // Ẩn khi chưa ở trong võ trường
        }

        private void CreateArenaButton(Transform parent, string label, Action onClick, Vector2 pos, Vector2 size)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);

            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = btnObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameSlotNormal();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.2f, 0.12f, 0.05f);
            tmp.alignment = TextAlignmentOptions.Center;
        }
        #endregion

        #region Public Arena Operations
        public void TeleportPlayerToArena()
        {
            var player = PlayerCombatController.Instance;
            if (player != null)
            {
                player.transform.position = new Vector3(ArenaCenter.x - 5f, ArenaCenter.y, 0f);
                PlayerInteractionController.Instance?.ShowFeedback("Đã dịch chuyển tức thì đến VÕ TRƯỜNG THẢO NGUYÊN!");
            }
        }

        public void TeleportToFarmCenter()
        {
            var player = PlayerCombatController.Instance;
            if (player != null)
            {
                player.transform.position = Vector3.zero;
                PlayerInteractionController.Instance?.ShowFeedback("Đã quay về trung tâm nông trại!");
            }
        }

        public void EquipWeapon(string weaponId)
        {
            if (PlayerCombatController.Instance != null)
            {
                PlayerCombatController.Instance.EquipWeaponById(weaponId);
                var def = WeaponDatabase.GetWeapon(weaponId);
                PlayerInteractionController.Instance?.ShowFeedback($"Đã trang bị {def.DisplayName} ({def.BaseDamage} Dmg | {def.ActionType})!");
            }
        }

        public void SpawnArenaEnemy(EnemyTypeId type)
        {
            var spawner = EnemyWaveSpawner.Instance;
            if (spawner != null)
            {
                Vector2 spawnPos = ArenaCenter + new Vector2(4f, UnityEngine.Random.Range(-2.5f, 2.5f));
                var enemy = spawner.SpawnEnemyAt(type, spawnPos);
                if (enemy != null)
                {
                    arenaEnemies.Add(enemy);
                    enemy.OnDefeated += e => arenaEnemies.Remove(e);
                }
                PlayerInteractionController.Instance?.ShowFeedback($"Đã sinh 1 {type} trong Võ Trường!");
            }
        }

        public void SpawnMultiplePests(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnArenaEnemy(EnemyTypeId.ChuotGamKho);
            }
        }

        public void ClearArenaEnemies()
        {
            for (int i = arenaEnemies.Count - 1; i >= 0; i--)
            {
                if (arenaEnemies[i] != null)
                {
                    Destroy(arenaEnemies[i].gameObject);
                }
            }
            arenaEnemies.Clear();
            PlayerInteractionController.Instance?.ShowFeedback("Đã dọn sạch toàn bộ quái vật trong Võ Trường!");
        }

        public void ExecuteDashSlash()
        {
            PlayerCombatController.Instance?.ExecuteDashSlash();
            PlayerInteractionController.Instance?.ShowFeedback("Đoạn Ảnh Trảm: Lướt tới chém gây x1.5 sát thương!");
        }

        public void ExecuteRageShockwave()
        {
            var player = FindAnyObjectByType<PlayerMobileController>();
            if (player != null)
            {
                player.ExecuteMartialSkill(CombatSlotType.TheBien);
            }
        }

        public void RefillCombatStats()
        {
            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData != null)
            {
                charData.Hp = charData.MaxHp;
                charData.Rage = 100;
                charData.Stamina.CurrentStamina = charData.Stamina.MaxStamina;
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                PlayerInteractionController.Instance?.ShowFeedback("Đã hồi phục 100 HP, 100 Nộ Khí & Đầy Thể Lực!");
            }
        }
        #endregion
    }
}
