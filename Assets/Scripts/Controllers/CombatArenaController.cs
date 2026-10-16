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
        private readonly Dictionary<string, Image> weaponBtnImages = new Dictionary<string, Image>();
        private TextMeshProUGUI weaponStatusText;
        private string currentEquippedWeaponId = "kiem_sat";
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
                var dev = DevModePopup.Instance ?? FindAnyObjectByType<DevModePopup>();
                if (dev == null)
                {
                    dev = canvas.GetComponent<DevModePopup>() ?? canvas.gameObject.AddComponent<DevModePopup>();
                }
                dev.EnsureUIExists();
            }

            BuildCombatDashboardUI();

            // Mặc định trang bị Kiếm Thép ngay từ đầu
            EquipWeapon("kiem_sat");
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

            // Phím tắt nhanh trên bàn phím PC (cả phím số trên và Numpad)
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) EquipWeapon("kiem_sat");
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) EquipWeapon("bua_ta");
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) EquipWeapon("giao_tre");
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) EquipWeapon("dao_go");

            // Phím tắt leo thang chiến đấu ngoài hành tinh
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) EnemyWaveSpawner.Instance?.TriggerAlienDrop3(Vector2.zero);
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) EnemyWaveSpawner.Instance?.TriggerWave("Binh Đoàn Alien UFO", 4, RaidType.AlienRaid);
            if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) EnemyWaveSpawner.Instance?.TriggerMothershipInvasion();

            if (Input.GetKeyDown(KeyCode.T)) SpawnEnemy(EnemyTypeId.KeBatHeo);
            if (Input.GetKeyDown(KeyCode.Y)) SpawnEnemy(EnemyTypeId.KeCaoPha);
            if (Input.GetKeyDown(KeyCode.U)) SpawnEnemy(EnemyTypeId.ChuotGamKho);
            if (Input.GetKeyDown(KeyCode.I)) SpawnEnemy(EnemyTypeId.UfoTrinhSat);
            if (Input.GetKeyDown(KeyCode.H)) SpawnTestPig();
            if (Input.GetKeyDown(KeyCode.R)) EnemyWaveSpawner.Instance?.TriggerWave("Đạo Tặc Thảo Nguyên", 3, RaidType.BanditRaid);
            if (Input.GetKeyDown(KeyCode.O)) EnemyWaveSpawner.Instance?.TriggerWave("Binh Đoàn Alien UFO", 4, RaidType.AlienRaid);
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
            currentEquippedWeaponId = weaponId;
            var player = PlayerCombatController.Instance ?? FindAnyObjectByType<PlayerCombatController>();
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

            HighlightWeaponButton(weaponId);
            UpdateStatsUI();
        }

        public void HighlightWeaponButton(string weaponId)
        {
            currentEquippedWeaponId = weaponId;
            foreach (var kvp in weaponBtnImages)
            {
                bool isSelected = (kvp.Key == weaponId);
                if (kvp.Value != null)
                {
                    kvp.Value.color = isSelected 
                        ? new Color(0.95f, 0.72f, 0.15f, 1f) 
                        : new Color(0.20f, 0.28f, 0.38f, 1f);
                    
                    var txt = kvp.Value.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                        txt.color = isSelected ? new Color(0.12f, 0.08f, 0.02f) : Color.white;
                    }
                }
            }

            if (weaponStatusText != null)
            {
                var def = WeaponDatabase.GetWeapon(weaponId);
                if (def != null)
                {
                    string skillDesc = def.SpecialSkill switch
                    {
                        WeaponSpecialSkill.Parry => "Đỡ Đòn [Parry]",
                        WeaponSpecialSkill.Slam => "Dập Đất [Slam]",
                        WeaponSpecialSkill.Flurry => "Đâm Liền Hoàn [Flurry]",
                        _ => def.SpecialSkill.ToString()
                    };
                    weaponStatusText.text = $"- Đang chọn: <color=#fcd34d><b>{def.DisplayName}</b></color> ({def.MinDamage}-{def.MaxDamage} DMG | {skillDesc})";
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

        public void SpawnTestPig()
        {
            Vector2 playerPos = PlayerCombatController.Instance != null ? PlayerCombatController.Instance.Position : Vector2.zero;
            Vector2 spawnPos = playerPos + (Vector2)UnityEngine.Random.insideUnitCircle * 2.2f;

            var pigObj = new GameObject($"Pig_DongCo_{UnityEngine.Random.Range(100, 999)}");
            pigObj.transform.position = spawnPos;

            var sr = pigObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.LoadSprite("Art/Sprites/Pigs/hong_dien/idle.png", 32);
            sr.sortingOrder = Mathf.RoundToInt(-spawnPos.y * 100);

            var rb = pigObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = pigObj.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            var pigAgent = pigObj.AddComponent<PigAgentView>();
            pigAgent.SpriteRenderer = sr;

            var engine = MobileGameController.Instance?.Engine;
            var pigModel = Pig.CreateDefault($"pig_{Guid.NewGuid().ToString().Substring(0, 6)}", "Ỉn Đồng Cỏ", GeneLineId.HongDien, GeneRarity.Thuong);
            engine?.Pigs.Add(pigModel);
            pigAgent.Bind(pigModel);

            PlayerInteractionController.Instance?.ShowFeedback("Đã thả thêm 1 chú Heo vào đồng cỏ!");
            UpdateStatsUI();
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

            // Panel Dashboard ở góc trên bên trái (dưới nút DevMode)
            var dashboardObj = new GameObject("CombatTestDashboard", typeof(RectTransform));
            dashboardObj.transform.SetParent(canvas.transform, false);

            var rt = dashboardObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -70f);
            rt.sizeDelta = new Vector2(340f, 500f);

            var bg = dashboardObj.AddComponent<Image>();
            bg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.94f);
            bg.raycastTarget = true;

            // Tiêu đề
            var titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(dashboardObj.transform, false);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);
            titleRt.sizeDelta = new Vector2(0f, 28f);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "[ARENA] ĐỒNG CỎ & TEST CƯỚP HEO";
            titleTxt.fontSize = 14f;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(1f, 0.85f, 0.3f, 1f);

            // Container chứa các nút bấm
            var contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(dashboardObj.transform, false);
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(14f, 14f);
            contentRt.offsetMax = new Vector2(-14f, -44f);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlWidth = true;

            // 1. Nhóm đổi vũ khí
            CreateSectionLabel(contentObj.transform, "CHỌN VŨ KHÍ (Phím 1, 2, 3, 4 hoặc Click):");
            var wepRow = CreateButtonRow(contentObj.transform, 4);
            CreateWeaponButton(wepRow, "kiem_sat", "[1] Kiếm", () => EquipWeapon("kiem_sat"));
            CreateWeaponButton(wepRow, "bua_ta", "[2] Búa", () => EquipWeapon("bua_ta"));
            CreateWeaponButton(wepRow, "giao_tre", "[3] Giáo", () => EquipWeapon("giao_tre"));
            CreateWeaponButton(wepRow, "dao_go", "[4] Đao", () => EquipWeapon("dao_go"));

            var wepStatusObj = new GameObject("WeaponStatusLabel", typeof(RectTransform));
            wepStatusObj.transform.SetParent(contentObj.transform, false);
            var wepStatusRt = wepStatusObj.GetComponent<RectTransform>();
            wepStatusRt.sizeDelta = new Vector2(312f, 18f);
            var wepStatusLe = wepStatusObj.AddComponent<LayoutElement>();
            wepStatusLe.minHeight = 18f;
            wepStatusLe.preferredHeight = 18f;
            weaponStatusText = wepStatusObj.AddComponent<TextMeshProUGUI>();
            weaponStatusText.fontSize = 11f;
            weaponStatusText.color = new Color(0.95f, 0.9f, 0.75f, 1f);
            weaponStatusText.alignment = TextAlignmentOptions.Left;

            // 2. Nhóm gọi quái vật & Thử nghiệm Raid
            CreateSectionLabel(contentObj.transform, "GỌI KẺ ĐỊCH & RAID (T, Y, U, I, R, 7, 8, 9):");
            var enemyRow = CreateButtonRow(contentObj.transform, 4);
            CreateButton(enemyRow, "Kẻ Bắt Heo", () => SpawnEnemy(EnemyTypeId.KeBatHeo));
            CreateButton(enemyRow, "Kẻ Cào Phá", () => SpawnEnemy(EnemyTypeId.KeCaoPha));
            CreateButton(enemyRow, "Chuột Gặm", () => SpawnEnemy(EnemyTypeId.ChuotGamKho));
            CreateButton(enemyRow, "UFO (I)", () => SpawnEnemy(EnemyTypeId.UfoTrinhSat));

            var raidRow = CreateButtonRow(contentObj.transform, 2);
            CreateButton(raidRow, "Raid Đạo Tặc (R)", () => EnemyWaveSpawner.Instance?.TriggerWave("Đạo Tặc Thảo Nguyên", 3, RaidType.BanditRaid));
            CreateButton(raidRow, "Thả 3 Alien (7)", () => EnemyWaveSpawner.Instance?.TriggerAlienDrop3(Vector2.zero));

            var alienRow = CreateButtonRow(contentObj.transform, 2);
            CreateButton(alienRow, "Alien Raid (8/O)", () => EnemyWaveSpawner.Instance?.TriggerWave("Binh Đoàn Alien UFO", 4, RaidType.AlienRaid));
            CreateButton(alienRow, "Tàu Mẹ & Boss (9)", () => EnemyWaveSpawner.Instance?.TriggerMothershipInvasion());

            // 3. Nhóm thử nghiệm phản đạn & tiện ích
            var utilRow = CreateButtonRow(contentObj.transform, 4);
            CreateButton(utilRow, "Thả Heo (H)", SpawnTestPig);
            CreateButton(utilRow, "Bắn Đạn (P)", FireTestProjectile);
            toggleTurretBtn = CreateButton(utilRow, "Auto: TẮT", ToggleAutoTurret);
            CreateButton(utilRow, "Dọn Quái (C)", ClearAllEnemies);

            var healRow = CreateButtonRow(contentObj.transform, 2);
            CreateButton(healRow, "Hồi Máu & Nộ", RefillPlayer);
            CreateButton(healRow, "Reset Thống Kê", ResetStats);

            // 4. Bảng thống kê DPS & Chiến thuật
            var statsBox = new GameObject("StatsBox", typeof(RectTransform));
            statsBox.transform.SetParent(contentObj.transform, false);
            var statsRt = statsBox.GetComponent<RectTransform>();
            statsRt.sizeDelta = new Vector2(312f, 115f);
            var statsLe = statsBox.AddComponent<LayoutElement>();
            statsLe.minHeight = 110f;
            statsLe.preferredHeight = 115f;

            var statsBg = statsBox.AddComponent<Image>();
            statsBg.sprite = UISpriteLoader.GetFrameSlotNormal();
            statsBg.color = new Color(0f, 0f, 0f, 0.55f);

            var statsTxtObj = new GameObject("StatsText", typeof(RectTransform));
            statsTxtObj.transform.SetParent(statsBox.transform, false);
            var statsTxtRt = statsTxtObj.GetComponent<RectTransform>();
            statsTxtRt.anchorMin = Vector2.zero;
            statsTxtRt.anchorMax = Vector2.one;
            statsTxtRt.offsetMin = new Vector2(8f, 6f);
            statsTxtRt.offsetMax = new Vector2(-8f, -6f);

            statsText = statsTxtObj.AddComponent<TextMeshProUGUI>();
            statsText.fontSize = 11.5f;
            statsText.color = Color.white;
            statsText.alignment = TextAlignmentOptions.TopLeft;

            UpdateStatsUI();
            HighlightWeaponButton(currentEquippedWeaponId);
        }

        private void ToggleAutoTurret()
        {
            autoShootTurret = !autoShootTurret;
            if (toggleTurretBtn != null)
            {
                var txt = toggleTurretBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = autoShootTurret ? "Auto: BẬT" : "Auto: TẮT";
                    txt.color = autoShootTurret ? new Color(0.2f, 1f, 0.4f) : Color.white;
                }
            }
        }

        public void UpdateStatsUI()
        {
            if (statsText == null) return;

            var player = PlayerCombatController.Instance ?? FindAnyObjectByType<PlayerCombatController>();
            var wep = player?.CurrentWeapon ?? WeaponDatabase.GetWeapon(currentEquippedWeaponId);
            string wepName = wep != null ? wep.DisplayName : "Chưa chọn";
            string wepInfo = wep != null ? $"{wep.MinDamage}-{wep.MaxDamage} DMG ({wep.ActionType})" : "";

            int totalPigs = 0;
            int capturedPigs = 0;
            foreach (var p in FindObjectsByType<PigAgentView>(FindObjectsSortMode.None))
            {
                if (p != null && p.PigModel != null && p.PigModel.IsAlive)
                {
                    totalPigs++;
                    if (p.IsCaptured) capturedPigs++;
                }
            }

            statsText.text = 
                $"- Đàn heo đồng cỏ: <color=#f472b6><b>{totalPigs}</b> chú</color> (Bị cướp: <color=#ef4444><b>{capturedPigs}</b></color>)\n" +
                $"- Vũ khí: <color=#fcd34d><b>{wepName}</b></color> [{wepInfo}]\n" +
                $"- Đòn đánh cuối: <color=#fcd34d>{Mathf.RoundToInt(LastHitDamage)}</color> DMG\n" +
                $"- Tổng sát thương: <color=#38bdf8>{Mathf.RoundToInt(TotalDamageDealt)}</color> ({TotalHits} đòn)\n" +
                $"- Số lần Đỡ đòn (Parry): <color=#a7f3d0>{TotalParries}</color>\n" +
                $"- Tia đạn đã Phản xạ: <color=#67e8f9>{TotalDeflections}</color>";
        }

        private Button CreateWeaponButton(Transform parent, string weaponId, string text, Action onClick)
        {
            var btn = CreateButton(parent, text, onClick);
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                weaponBtnImages[weaponId] = img;
            }
            return btn;
        }

        private void CreateSectionLabel(Transform parent, string text)
        {
            var obj = new GameObject("Label", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(312f, 18f);
            var le = obj.AddComponent<LayoutElement>();
            le.minHeight = 18f;
            le.preferredHeight = 18f;

            var txt = obj.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 11.5f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(0.75f, 0.85f, 0.95f, 0.9f);
        }

        private Transform CreateButtonRow(Transform parent, int count)
        {
            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(312f, 32f);

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 32f;
            le.preferredHeight = 32f;
            le.flexibleWidth = 1f;

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
            var btnObj = new GameObject($"Btn_{text}", typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);

            var le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = 30f;
            le.preferredHeight = 32f;
            le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameActionButton();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.45f, 0.7f, 1f);
            img.raycastTarget = true;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 11.5f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;

            return btn;
        }
        #endregion
    }
}
