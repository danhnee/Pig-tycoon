using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Điều phối Đấu Trường Luyện Võ & Kiểm Thử Chiến Đấu (Combat Test Arena).
    /// Cho phép đổi vũ khí tức thì, sinh quái vật, bắn đạn thử nghiệm (để luyện Parry & Deflection) và theo dõi thông số DPS.
    /// </summary>
    public class CombatArenaController : MonoBehaviour
    {
        public static CombatArenaController Instance { get; private set; }

        [Header("Arena Configuration")]
        public Transform PlayerSpawnPoint;
        public Transform DummySpawnRoot;
        public Transform EnemySpawnRoot;
        public Transform ProjectileTurretPoint;

        [Header("Combat Analytics")]
        public float TotalDamageDealt = 0f;
        public int TotalHits = 0;
        public int TotalCrits = 0;
        public int TotalParries = 0;
        public int TotalDeflections = 0;
        public float LastHitDamage = 0f;

        private readonly List<EnemyAgentView> spawnedEnemies = new List<EnemyAgentView>();
        private bool autoShootTurret = false;
        private float turretTimer = 0f;
        private TextMeshProUGUI statsText;
        private Button toggleTurretBtn;

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
            // Đảm bảo EventSystem và Dev Mode Button luôn sẵn sàng hoạt động
            DevModePopup.EnsureEventSystem();
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var dev = DevModePopup.Instance;
                if (dev == null)
                {
                    dev = canvas.gameObject.AddComponent<DevModePopup>();
                }
                dev.EnsureUIExists();
            }

            BuildCombatDashboardUI();

            // Đảm bảo người chơi cầm sẵn Kiếm Thép khi vào đấu trường
            var player = PlayerCombatController.Instance;
            if (player != null)
            {
                player.EquipWeaponById("kiem_sat");
            }
        }

        private void Update()
        {
            // Bắn đạn định kỳ để người chơi luyện phản đòn / chém phá đạn
            if (autoShootTurret)
            {
                turretTimer -= Time.deltaTime;
                if (turretTimer <= 0f)
                {
                    turretTimer = 2.2f;
                    FireTestProjectile();
                }
            }

            // Phím tắt nhanh trên bàn phím PC
            if (Input.GetKeyDown(KeyCode.Alpha1)) EquipWeapon("kiem_sat");
            if (Input.GetKeyDown(KeyCode.Alpha2)) EquipWeapon("bua_ta");
            if (Input.GetKeyDown(KeyCode.Alpha3)) EquipWeapon("giao_tre");
            if (Input.GetKeyDown(KeyCode.Alpha4)) EquipWeapon("dao_go");

            if (Input.GetKeyDown(KeyCode.T)) SpawnEnemy(EnemyTypeId.KeBatHeo);
            if (Input.GetKeyDown(KeyCode.Y)) SpawnEnemy(EnemyTypeId.KeCaoPha);
            if (Input.GetKeyDown(KeyCode.U)) SpawnEnemy(EnemyTypeId.ChuotGamKho);
            if (Input.GetKeyDown(KeyCode.P)) FireTestProjectile();
            if (Input.GetKeyDown(KeyCode.C)) ClearAllEnemies();

            UpdateStatsUI();
        }

        #region Combat Tracking & Analytics
        public void RecordHit(string targetName, float damage)
        {
            TotalDamageDealt += damage;
            TotalHits++;
            LastHitDamage = damage;
            UpdateStatsUI();
        }

        public void RecordParry()
        {
            TotalParries++;
            UpdateStatsUI();
        }

        public void RecordDeflection()
        {
            TotalDeflections++;
            UpdateStatsUI();
        }

        public void ResetStats()
        {
            TotalDamageDealt = 0f;
            TotalHits = 0;
            TotalCrits = 0;
            TotalParries = 0;
            TotalDeflections = 0;
            LastHitDamage = 0f;
            UpdateStatsUI();
        }
        #endregion

        #region Weapon Switching & Spawning
        public void EquipWeapon(string weaponId)
        {
            var player = PlayerCombatController.Instance;
            if (player != null)
            {
                player.EquipWeaponById(weaponId);
                var weapon = player.CurrentWeapon;
                string name = weapon != null ? weapon.DisplayName : weaponId;
                if (PlayerInteractionController.Instance != null)
                {
                    PlayerInteractionController.Instance.ShowFeedback($"Trang bị: {name} ({weapon?.ActionType} | Kỹ năng: {weapon?.SpecialSkill})");
                }
            }
        }

        public void SpawnEnemy(EnemyTypeId type)
        {
            Vector2 playerPos = PlayerCombatController.Instance != null ? PlayerCombatController.Instance.Position : Vector2.zero;
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(4.5f, 6.0f);
            Vector2 spawnPos = playerPos + offset;

            if (EnemyWaveSpawner.Instance != null)
            {
                var enemy = EnemyWaveSpawner.Instance.SpawnEnemyAt(type, spawnPos);
                if (enemy != null)
                {
                    spawnedEnemies.Add(enemy);
                    enemy.OnDefeated += (e) => spawnedEnemies.Remove(e);
                }
            }
        }

        public void FireTestProjectile()
        {
            var player = PlayerCombatController.Instance;
            if (player == null) return;

            Vector2 playerPos = player.Position;
            Vector2 spawnOrigin = (ProjectileTurretPoint != null) 
                ? (Vector2)ProjectileTurretPoint.position 
                : playerPos + new Vector2(UnityEngine.Random.Range(-5f, 5f), 6.5f);

            Vector2 fireDir = (playerPos - spawnOrigin).normalized;

            var projObj = new GameObject("TestEnemyProjectile");
            var sr = projObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.GetIconEnergyBolt() ?? UISpriteLoader.GetFrameActionButton();
            sr.color = new Color(1f, 0.3f, 0.2f, 1f); // Màu đỏ cam cảnh báo
            sr.sortingOrder = 550;

            var proj = projObj.AddComponent<CombatProjectile2D>();
            proj.Initialize(spawnOrigin, fireDir, damage: 15f, speed: 4.8f, isPlayer: false);
        }

        public void ClearAllEnemies()
        {
            foreach (var enemy in FindObjectsByType<EnemyAgentView>(FindObjectsSortMode.None))
            {
                if (enemy != null) Destroy(enemy.gameObject);
            }
            foreach (var proj in FindObjectsByType<CombatProjectile2D>(FindObjectsSortMode.None))
            {
                if (proj != null) Destroy(proj.gameObject);
            }
            spawnedEnemies.Clear();
        }

        public void RefillPlayer()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine?.Character != null)
            {
                engine.Character.Hp = engine.Character.MaxHp;
                engine.Character.Rage = 100;
                engine.Character.Stamina.CurrentStamina = engine.Character.Stamina.MaxStamina;
                MobileGameController.Instance?.OnStateUpdated?.Invoke();

                if (PlayerInteractionController.Instance != null)
                {
                    PlayerInteractionController.Instance.ShowFeedback("Đã hồi phục toàn bộ Máu, Thể Lực và Nộ Khí!");
                }
            }
        }
        #endregion

        #region Combat Dashboard UI
        private void BuildCombatDashboardUI()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("CombatTestDashboard");
            if (existing != null) Destroy(existing.gameObject);

            // Panel Dashboard ở góc trên bên trái
            var dashboardObj = new GameObject("CombatTestDashboard");
            dashboardObj.transform.SetParent(canvas.transform, false);

            var rt = dashboardObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -70f);
            rt.sizeDelta = new Vector2(340f, 430f);

            var bg = dashboardObj.AddComponent<Image>();
            bg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.92f);

            // Tiêu đề
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(dashboardObj.transform, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -22f);
            titleRt.sizeDelta = new Vector2(0f, 32f);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "⚔️ ĐẤU TRƯỜNG TEST COMBAT";
            titleTxt.fontSize = 17f;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(1f, 0.85f, 0.3f, 1f);

            // Container chứa các nút bấm
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(dashboardObj.transform, false);
            var contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.offsetMin = new Vector2(14f, 14f);
            contentRt.offsetMax = new Vector2(-14f, -48f);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;

            // 1. Nhóm đổi vũ khí
            CreateSectionLabel(contentObj.transform, "CHỌN VŨ KHÍ (Phím 1, 2, 3, 4):");
            var wepRow = CreateButtonRow(contentObj.transform, 4);
            CreateButton(wepRow, "🗡️ Kiếm", () => EquipWeapon("kiem_sat"));
            CreateButton(wepRow, "🪓 Búa", () => EquipWeapon("bua_ta"));
            CreateButton(wepRow, "🔱 Giáo", () => EquipWeapon("giao_tre"));
            CreateButton(wepRow, "🪵 Đao", () => EquipWeapon("dao_go"));

            // 2. Nhóm gọi quái vật
            CreateSectionLabel(contentObj.transform, "GỌI KẺ ĐỊCH (Phím T, Y, U, P):");
            var enemyRow = CreateButtonRow(contentObj.transform, 3);
            CreateButton(enemyRow, "Kẻ Bắt Heo", () => SpawnEnemy(EnemyTypeId.KeBatHeo));
            CreateButton(enemyRow, "Kẻ Cào Phá", () => SpawnEnemy(EnemyTypeId.KeCaoPha));
            CreateButton(enemyRow, "Chuột Gặm", () => SpawnEnemy(EnemyTypeId.ChuotGamKho));

            // 3. Nhóm thử nghiệm phản đạn & tiện ích
            var utilRow = CreateButtonRow(contentObj.transform, 3);
            CreateButton(utilRow, "Bắn Đạn", FireTestProjectile);
            toggleTurretBtn = CreateButton(utilRow, "Auto Bắn: TẮT", ToggleAutoTurret);
            CreateButton(utilRow, "Dọn Quái (C)", ClearAllEnemies);

            var healRow = CreateButtonRow(contentObj.transform, 2);
            CreateButton(healRow, "❤️ Hồi Đầy Máu & Nộ", RefillPlayer);
            CreateButton(healRow, "🔄 Reset Chỉ Số", ResetStats);

            // 4. Bảng thống kê DPS & Chiến thuật
            var statsBox = new GameObject("StatsBox");
            statsBox.transform.SetParent(contentObj.transform, false);
            var statsRt = statsBox.AddComponent<RectTransform>();
            statsRt.sizeDelta = new Vector2(312f, 105f);
            var statsBg = statsBox.AddComponent<Image>();
            statsBg.sprite = UISpriteLoader.GetFrameSlotNormal();
            statsBg.color = new Color(0f, 0f, 0f, 0.45f);

            var statsTxtObj = new GameObject("StatsText");
            statsTxtObj.transform.SetParent(statsBox.transform, false);
            var statsTxtRt = statsTxtObj.AddComponent<RectTransform>();
            statsTxtRt.anchorMin = Vector2.zero;
            statsTxtRt.anchorMax = Vector2.one;
            statsTxtRt.offsetMin = new Vector2(8f, 6f);
            statsTxtRt.offsetMax = new Vector2(-8f, -6f);

            statsText = statsTxtObj.AddComponent<TextMeshProUGUI>();
            statsText.fontSize = 13f;
            statsText.color = Color.white;
            statsText.alignment = TextAlignmentOptions.TopLeft;

            UpdateStatsUI();
        }

        private void ToggleAutoTurret()
        {
            autoShootTurret = !autoShootTurret;
            if (toggleTurretBtn != null)
            {
                var txt = toggleTurretBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = autoShootTurret ? "Auto Bắn: BẬT" : "Auto Bắn: TẮT";
                    txt.color = autoShootTurret ? new Color(0.2f, 1f, 0.4f) : Color.white;
                }
            }
        }

        private void UpdateStatsUI()
        {
            if (statsText == null) return;

            statsText.text = 
                $"• Đòn đánh cuối: <color=#fcd34d>{Mathf.RoundToInt(LastHitDamage)}</color> DMG\n" +
                $"• Tổng sát thương: <color=#38bdf8>{Mathf.RoundToInt(TotalDamageDealt)}</color> ({TotalHits} đòn)\n" +
                $"• Số lần Đỡ đòn (Parry): <color=#a7f3d0>{TotalParries}</color>\n" +
                $"• Số tia đạn đã Phản xạ: <color=#67e8f9>{TotalDeflections}</color>";
        }

        private void CreateSectionLabel(Transform parent, string text)
        {
            var obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(312f, 18f);
            var txt = obj.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 11.5f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(0.75f, 0.85f, 0.95f, 0.9f);
        }

        private Transform CreateButtonRow(Transform parent, int count)
        {
            var row = new GameObject("Row");
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(312f, 32f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5f;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;

            return row.transform;
        }

        private Button CreateButton(Transform parent, string text, Action onClick)
        {
            var btnObj = new GameObject($"Btn_{text}");
            btnObj.transform.SetParent(parent, false);

            var img = btnObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameActionButton();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.45f, 0.7f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;

            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 12f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;

            return btn;
        }
        #endregion
    }
}
