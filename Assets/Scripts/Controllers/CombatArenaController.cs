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
    /// Điều phối Đấu Trường Luyện Võ & Triển Lãm Chiến Đấu (Combat Test Arena Showcase).
    /// Hỗ trợ kiểm thử toàn diện:
    /// 1. Triển lãm & Trình diễn hoạt ảnh 20 Tháp Phòng Thủ (4 Factions: Bạch Vân, Alien Tech, Bandit Scrap, Bio & Mystic).
    /// 2. Triển lãm & Sinh 13 Loại Quái Vật & 5 Đợt Đột Kích (Raids, Ambushes, Boss Siege, Mothership).
    /// 3. Thao trường Thử Vũ Khí (Slash, Smash, Thrust), Đỡ Đòn (Parry), Phản Xạ Đạn (Deflect), Thể Lực 2 Tầng.
    /// 4. Triển lãm Vật Phẩm Rơi (Loot Drops & Cột Sáng Rarity Beams) & Bản Vẽ Công Thức (Blueprints).
    /// 5. Hệ thống Cướp Heo & Giải Cứu Đàn Heo (Tractor Beam & Pig Snatching).
    /// </summary>
    [ExecuteAlways]
    public class CombatArenaController : MonoBehaviour
    {
        public static CombatArenaController Instance { get; private set; }

        [Header("Arena Configuration")]
        public Transform PlayerSpawnPoint;
        public Transform DummySpawnRoot;
        public Transform EnemySpawnRoot;
        public Transform ProjectileTurretPoint;
        public Transform TowersRoot;

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

        // UI Tabs Management
        private int currentTab = 0; // 0: Quái Vật, 1: 20 Tháp, 2: Vũ Khí & Tiện Ích, 3: Thống Kê
        private GameObject[] tabPanels;
        private Image[] tabTabButtons;
        private int cleanlinessFrames = 0;

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                SafeDestroy(gameObject);
                return;
            }
            Instance = this;
            EnforceArenaLayoutCleanliness();
        }

        private void OnEnable()
        {
            EnforceArenaLayoutCleanliness();
        }

        private void Start()
        {
            EnforceArenaLayoutCleanliness();

            if (!Application.isPlaying) return;

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
            EquipWeapon("kiem_sat");
        }

        private void Update()
        {
            if (cleanlinessFrames < 5)
            {
                cleanlinessFrames++;
                EnforceArenaLayoutCleanliness();
            }

            // Auto Turret bắn đạn định kỳ để luyện Parry / Deflection
            if (autoShootTurret)
            {
                turretTimer -= Time.deltaTime;
                if (turretTimer <= 0f)
                {
                    turretTimer = 2.2f;
                    FireTestProjectile();
                }
            }

            // Phím tắt vũ khí
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) EquipWeapon("kiem_sat");
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) EquipWeapon("bua_ta");
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) EquipWeapon("giao_tre");
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) EquipWeapon("dao_go");

            // Phím tắt đột kích (Raid Escapes)
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) EnemyWaveSpawner.Instance?.TriggerBanditAmbush3(Vector2.zero);
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) EnemyWaveSpawner.Instance?.TriggerBanditBossSiege();
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) EnemyWaveSpawner.Instance?.TriggerAlienDrop3(Vector2.zero);
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) EnemyWaveSpawner.Instance?.TriggerWave("Binh Đoàn Alien UFO", 4, RaidType.AlienRaid);
            if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) EnemyWaveSpawner.Instance?.TriggerMothershipInvasion();

            // Phím tắt gọi quái
            if (Input.GetKeyDown(KeyCode.T)) SpawnEnemy(EnemyTypeId.KeBatHeo);
            if (Input.GetKeyDown(KeyCode.Y)) SpawnEnemy(EnemyTypeId.KeCaoPha);
            if (Input.GetKeyDown(KeyCode.U)) SpawnEnemy(EnemyTypeId.ChuotGamKho);
            if (Input.GetKeyDown(KeyCode.I)) SpawnEnemy(EnemyTypeId.UfoTrinhSat);
            if (Input.GetKeyDown(KeyCode.O)) SpawnEnemy(EnemyTypeId.UfoThuHoach);
            if (Input.GetKeyDown(KeyCode.G)) SpawnEnemy(EnemyTypeId.AlienGrunt);
            if (Input.GetKeyDown(KeyCode.E)) SpawnEnemy(EnemyTypeId.AlienElite);
            if (Input.GetKeyDown(KeyCode.J)) SpawnEnemy(EnemyTypeId.DauLinhTrom);
            if (Input.GetKeyDown(KeyCode.K)) SpawnEnemy(EnemyTypeId.ChuaTromRiuSat);
            if (Input.GetKeyDown(KeyCode.L)) SpawnEnemy(EnemyTypeId.BossAlienWarlord);
            if (Input.GetKeyDown(KeyCode.M)) SpawnEnemy(EnemyTypeId.UfoMothership);
            if (Input.GetKeyDown(KeyCode.N)) SpawnEnemy(EnemyTypeId.ChoSuong);
            if (Input.GetKeyDown(KeyCode.B)) SpawnEnemy(EnemyTypeId.BongHuyen);

            // Phím tắt tính năng showcase
            if (Input.GetKeyDown(KeyCode.F)) FireAll20Towers();
            if (Input.GetKeyDown(KeyCode.H)) SpawnTestPig();
            if (Input.GetKeyDown(KeyCode.P)) FireTestProjectile();
            if (Input.GetKeyDown(KeyCode.C)) ClearAllEnemies();
            if (Input.GetKeyDown(KeyCode.F8)) UnityEngine.SceneManagement.SceneManager.LoadScene("MainFarm2D");

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
                        WeaponSpecialSkill.Flurry => "Đâm Liên Hoàn [Flurry]",
                        _ => def.SpecialSkill.ToString()
                    };
                    weaponStatusText.text = $"Vũ khí: <color=#fcd34d><b>{def.DisplayName}</b></color> ({def.MinDamage}-{def.MaxDamage} DMG | {skillDesc})";
                }
            }
        }

        public void SpawnEnemy(EnemyTypeId type)
        {
            Vector2 playerPos = PlayerCombatController.Instance != null ? PlayerCombatController.Instance.Position : Vector2.zero;
            Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(3.5f, 5.5f);
            Vector2 spawnPos = playerPos + offset;

            if (EnemyWaveSpawner.Instance != null)
            {
                var enemy = EnemyWaveSpawner.Instance.SpawnEnemyAt(type, spawnPos);
                if (enemy != null)
                {
                    spawnedEnemies.Add(enemy);
                    enemy.OnDefeated += (e) => spawnedEnemies.Remove(e);
                    PlayerInteractionController.Instance?.ShowFeedback($"Đã sinh quái vật: {enemy.Model?.Name ?? type.ToString()}");
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
            sr.color = new Color(1f, 0.3f, 0.2f, 1f);
            sr.sortingOrder = 550;

            var proj = projObj.AddComponent<CombatProjectile2D>();
            proj.Initialize(spawnOrigin, fireDir, damage: 15f, speed: 4.8f, isPlayer: false);
        }

        public void ClearAllEnemies()
        {
            foreach (var enemy in FindObjectsByType<EnemyAgentView>())
            {
                if (enemy != null) SafeDestroy(enemy.gameObject);
            }
            foreach (var proj in FindObjectsByType<CombatProjectile2D>())
            {
                if (proj != null) SafeDestroy(proj.gameObject);
            }
            foreach (var loot in FindObjectsByType<LootDrop2DView>())
            {
                if (loot != null) SafeDestroy(loot.gameObject);
            }
            spawnedEnemies.Clear();
            PlayerInteractionController.Instance?.ShowFeedback("Đã dọn dẹp sạch toàn bộ kẻ địch, đạn và vật phẩm trên sân!");
        }

        private int testPigCounter = 0;

        public void SpawnTestPig()
        {
            Vector2 playerPos = PlayerCombatController.Instance != null ? PlayerCombatController.Instance.Position : Vector2.zero;
            Vector2 spawnPos = playerPos + (Vector2)UnityEngine.Random.insideUnitCircle * 2.2f;

            testPigCounter++;
            GeneLineId gene = GeneLineId.HongDien;
            GeneRarity rarity = GeneRarity.Thuong;
            string pigName = "Ỉn Hồng Điền";
            string spritePath = "Art/Sprites/Pigs/hong_dien/idle.png";

            if (testPigCounter % 4 == 2)
            {
                gene = GeneLineId.LamKhe;
                rarity = GeneRarity.Thuong;
                pigName = "Ỉn Lam Khê";
                spritePath = "Art/Sprites/Pigs/lam_khe/idle.png";
            }
            else if (testPigCounter % 4 == 3)
            {
                gene = GeneLineId.KimTho;
                rarity = GeneRarity.Hiem;
                pigName = "Ỉn Kim Thọ [Hiếm]";
                spritePath = "Art/Sprites/Pigs/kim_tho/idle.png";
            }
            else if (testPigCounter % 4 == 0)
            {
                gene = GeneLineId.HuThe;
                rarity = GeneRarity.DiBien;
                pigName = "Ỉn Hư Thể [Dị Biến]";
                spritePath = "Art/Sprites/Pigs/hu_the/idle.png";
            }

            var pigObj = new GameObject($"Pig_{gene}_{UnityEngine.Random.Range(100, 999)}");
            pigObj.transform.position = spawnPos;

            var sr = pigObj.AddComponent<SpriteRenderer>();
            sr.sprite = UISpriteLoader.LoadSprite(spritePath, 32) ?? UISpriteLoader.LoadSprite("Art/Sprites/Pigs/hong_dien/idle.png", 32);
            sr.sortingOrder = Mathf.RoundToInt(-spawnPos.y * 100);

            var rb = pigObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = pigObj.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            var pigAgent = pigObj.AddComponent<PigAgentView>();
            pigAgent.SpriteRenderer = sr;

            var engine = MobileGameController.Instance?.Engine;
            var pigModel = Pig.CreateDefault($"pig_{Guid.NewGuid().ToString().Substring(0, 6)}", pigName, gene, rarity);
            pigModel.WeightKg = (rarity == GeneRarity.DiBien) ? 120f : ((rarity == GeneRarity.Hiem) ? 75f : 35f);
            engine?.Pigs.Add(pigModel);
            pigAgent.Bind(pigModel);

            PlayerInteractionController.Instance?.ShowFeedback($"Đã thả thêm 1 chú {pigName} vào đồng cỏ!");
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
                    PlayerInteractionController.Instance.ShowFeedback("Đã hồi phục 100% Máu, Thể Lực (2 Tầng) và Nộ Khí!");
                }
            }
        }
        #endregion

        #region Tower Showcase Operations
        public void FireAll20Towers()
        {
            var towers = FindObjectsByType<DefenseTower2DView>();
            int fired = 0;
            foreach (var t in towers)
            {
                if (t != null && t.gameObject.activeInHierarchy)
                {
                    t.EnsureBuildingModelInitialized();
                    Vector3 forwardTarget = t.transform.position + Vector3.down * 3.5f;
                    t.TestFireAt(forwardTarget);
                    fired++;
                }
            }
            PlayerInteractionController.Instance?.ShowFeedback($"⚡ KHAI HỎA ĐỒNG LOẠT {fired} THÁP PHÒNG THỦ!");
        }

        public void FireFactionTowers(string faction)
        {
            var towers = FindObjectsByType<DefenseTower2DView>();
            int fired = 0;
            foreach (var t in towers)
            {
                if (t != null)
                {
                    t.EnsureBuildingModelInitialized();
                    if (t.BuildingModel != null && t.BuildingModel.Faction == faction)
                    {
                        Vector3 forwardTarget = t.transform.position + Vector3.down * 3.5f;
                        t.TestFireAt(forwardTarget);
                        fired++;
                    }
                }
            }
            PlayerInteractionController.Instance?.ShowFeedback($"Khai hỏa {fired} tháp phe {faction}!");
        }

        public void TriggerBachVanMegaLightning()
        {
            var towers = FindObjectsByType<DefenseTower2DView>();
            foreach (var t in towers)
            {
                if (t != null)
                {
                    t.EnsureBuildingModelInitialized();
                    if (t.BuildingModel != null && t.BuildingModel.IsBachVan)
                    {
                        t.ExecuteBachVanMegaLightning();
                    }
                }
            }
            PlayerInteractionController.Instance?.ShowFeedback("⚡ BẠCH VÂN · CỬU TIÊU LÔI PHÁO GIÁNG SẤM TOÀN MAP!");
        }

        public void RefillAllTowersAmmo()
        {
            var towers = FindObjectsByType<DefenseTower2DView>();
            foreach (var t in towers)
            {
                if (t != null)
                {
                    t.EnsureBuildingModelInitialized();
                    if (t.BuildingModel != null)
                    {
                        t.BuildingModel.AmmoCurrent = t.BuildingModel.AmmoCapacity;
                        t.BuildingModel.CurrentHp = t.BuildingModel.MaxHp;
                        t.BuildingModel.IsBroken = false;
                        t.Bind(t.BuildingModel);
                    }
                }
            }
            PlayerInteractionController.Instance?.ShowFeedback("Đã nạp đầy đạn và phục hồi 100% độ bền cho toàn bộ 20 tháp!");
        }

        public void SetAllTowersAmmoRatio(float ratio)
        {
            var towers = FindObjectsByType<DefenseTower2DView>();
            foreach (var t in towers)
            {
                if (t != null)
                {
                    t.EnsureBuildingModelInitialized();
                    if (t.BuildingModel != null)
                    {
                        t.BuildingModel.AmmoCurrent = Mathf.RoundToInt(t.BuildingModel.AmmoCapacity * Mathf.Clamp01(ratio));
                    }
                }
            }
            string stateLabel = ratio <= 0f ? "Cạn Đạn (0% - Gục nòng & Tắt đèn)" : (ratio < 0.25f ? "Sắp Hết Đạn (<25% - Chớp giật & Tàn tro)" : "Đầy Đạn (100% - Rực sáng)");
            PlayerInteractionController.Instance?.ShowFeedback($"Đã chuyển 20 tháp sang trạng thái: {stateLabel}!");
        }

        public void SpawnAllLootDropsShowcase()
        {
            var player = PlayerCombatController.Instance;
            Vector2 basePos = player != null ? player.Position + new Vector2(3f, 0f) : Vector2.zero;

            // 15 Bản vẽ tháp
            string[] blueprintItems = new string[]
            {
                "item_blueprint_plasma_beam", "item_blueprint_quantum_mortar", "item_blueprint_graviton_tractor",
                "item_blueprint_emp_sentry", "item_blueprint_antimatter_spire",
                "item_blueprint_gatling_crossbow", "item_blueprint_saw_catapult", "item_blueprint_tar_flamethrower",
                "item_blueprint_harpoon_ballista", "item_blueprint_warlord_howitzer",
                "item_blueprint_spine_ejector", "item_blueprint_spore_emitter", "item_blueprint_corrosive_spitter",
                "item_blueprint_leech_obelisk", "item_blueprint_bone_spire"
            };

            // Vật liệu quái rớt
            string[] materials = new string[]
            {
                "item_plasma_core", "item_quantum_fiber", "item_quantum_chip", "item_alloy_scrap",
                "item_broken_post", "item_rusty_nail", "item_rope", "item_rat_tail",
                "item_fertilizer", "item_mutant_corn", "icon_coin_gold"
            };

            int i = 0;
            foreach (var bp in blueprintItems)
            {
                Vector2 pos = basePos + new Vector2((i % 5) * 1.4f - 2.8f, (i / 5) * 1.5f + 1.2f);
                var loot = LootDatabase.CreateLootItem(bp, 1);
                if (loot != null) LootDrop2DView.Spawn(loot, pos);
                i++;
            }

            i = 0;
            foreach (var mat in materials)
            {
                Vector2 pos = basePos + new Vector2((i % 4) * 1.4f - 2.1f, -((i / 4) * 1.5f + 1.2f));
                var loot = LootDatabase.CreateLootItem(mat, UnityEngine.Random.Range(2, 6));
                if (loot != null) LootDrop2DView.Spawn(loot, pos);
                i++;
            }

            PlayerInteractionController.Instance?.ShowFeedback("Đã rải 15 Bản Vẽ Tháp và Vật Liệu Quái Rơi kèm Cột Sáng Rarity!");
        }
        #endregion

        #region Combat Dashboard UI
        private void BuildCombatDashboardUI()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("CombatTestDashboard");
            if (existing != null) SafeDestroy(existing.gameObject);

            // Panel Dashboard ở góc trên bên trái
            var dashboardObj = new GameObject("CombatTestDashboard", typeof(RectTransform));
            dashboardObj.transform.SetParent(canvas.transform, false);

            var rt = dashboardObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -65f);
            rt.sizeDelta = new Vector2(360f, 560f);

            var bg = dashboardObj.AddComponent<Image>();
            bg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);
            bg.raycastTarget = true;

            // Tiêu đề
            var titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(dashboardObj.transform, false);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -8f);
            titleRt.sizeDelta = new Vector2(0f, 26f);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "🏛️ ĐẤU TRƯỜNG & TRIỂN LÃM COMBAT";
            titleTxt.fontSize = 13.5f;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(1f, 0.85f, 0.3f, 1f);

            // Nút Về Nông Trại
            var returnBtnObj = new GameObject("Btn_ReturnFarm", typeof(RectTransform));
            returnBtnObj.transform.SetParent(dashboardObj.transform, false);
            var returnRt = returnBtnObj.GetComponent<RectTransform>();
            returnRt.anchorMin = new Vector2(1f, 1f);
            returnRt.anchorMax = new Vector2(1f, 1f);
            returnRt.pivot = new Vector2(1f, 1f);
            returnRt.anchoredPosition = new Vector2(-8f, -6f);
            returnRt.sizeDelta = new Vector2(120f, 24f);

            var returnImg = returnBtnObj.AddComponent<Image>();
            returnImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            returnImg.type = Image.Type.Sliced;
            returnImg.color = new Color(0.18f, 0.28f, 0.38f, 0.95f);

            var returnBtn = returnBtnObj.AddComponent<Button>();
            returnBtn.targetGraphic = returnImg;
            returnBtn.onClick.AddListener(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainFarm2D");
            });

            var returnTxtObj = new GameObject("Text", typeof(RectTransform));
            returnTxtObj.transform.SetParent(returnBtnObj.transform, false);
            var returnTxtRt = returnTxtObj.GetComponent<RectTransform>();
            returnTxtRt.anchorMin = Vector2.zero;
            returnTxtRt.anchorMax = Vector2.one;
            returnTxtRt.sizeDelta = Vector2.zero;

            var returnTxt = returnTxtObj.AddComponent<TextMeshProUGUI>();
            returnTxt.text = "🏡 Về Nông Trại";
            returnTxt.fontSize = 11f;
            returnTxt.fontStyle = FontStyles.Bold;
            returnTxt.alignment = TextAlignmentOptions.Center;
            returnTxt.color = Color.white;

            // Hàng 4 Tabs: [Quái Vật], [20 Tháp], [Vũ Khí], [Thống Kê]
            var tabRowObj = new GameObject("TabHeaderRow", typeof(RectTransform));
            tabRowObj.transform.SetParent(dashboardObj.transform, false);
            var tabRowRt = tabRowObj.GetComponent<RectTransform>();
            tabRowRt.anchorMin = new Vector2(0f, 1f);
            tabRowRt.anchorMax = new Vector2(1f, 1f);
            tabRowRt.pivot = new Vector2(0.5f, 1f);
            tabRowRt.anchoredPosition = new Vector2(0f, -34f);
            tabRowRt.sizeDelta = new Vector2(-20f, 28f);

            var tabHlg = tabRowObj.AddComponent<HorizontalLayoutGroup>();
            tabHlg.spacing = 4f;
            tabHlg.childControlWidth = true;
            tabHlg.childForceExpandWidth = true;
            tabHlg.childControlHeight = true;
            tabHlg.childForceExpandHeight = true;

            string[] tabNames = new string[] { "👹 Quái Vật", "🏰 20 Tháp", "⚔️ Vũ Khí", "📊 Thống Kê" };
            tabTabButtons = new Image[4];
            tabPanels = new GameObject[4];

            for (int i = 0; i < 4; i++)
            {
                int tabIdx = i;
                var tabBtnObj = new GameObject($"TabBtn_{i}", typeof(RectTransform));
                tabBtnObj.transform.SetParent(tabRowObj.transform, false);
                var btnImg = tabBtnObj.AddComponent<Image>();
                btnImg.sprite = UISpriteLoader.GetFrameActionButton();
                btnImg.type = Image.Type.Sliced;
                btnImg.color = (i == 0) ? new Color(0.95f, 0.72f, 0.15f, 1f) : new Color(0.2f, 0.3f, 0.45f, 1f);
                tabTabButtons[i] = btnImg;

                var btn = tabBtnObj.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => SwitchTab(tabIdx));

                var txtObj = new GameObject("Text", typeof(RectTransform));
                txtObj.transform.SetParent(tabBtnObj.transform, false);
                var txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
                var txt = txtObj.AddComponent<TextMeshProUGUI>();
                txt.text = tabNames[i];
                txt.fontSize = 10.5f;
                txt.fontStyle = (i == 0) ? FontStyles.Bold : FontStyles.Normal;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = (i == 0) ? new Color(0.1f, 0.1f, 0.1f, 1f) : Color.white;
            }

            // Container Panel chung
            var containerObj = new GameObject("TabContainer", typeof(RectTransform));
            containerObj.transform.SetParent(dashboardObj.transform, false);
            var containerRt = containerObj.GetComponent<RectTransform>();
            containerRt.anchorMin = Vector2.zero;
            containerRt.anchorMax = Vector2.one;
            containerRt.offsetMin = new Vector2(12f, 12f);
            containerRt.offsetMax = new Vector2(-12f, -66f);

            // Tab 0: Quái Vật & Raids
            tabPanels[0] = CreateTabPanel(containerObj.transform, "Panel_Monsters");
            BuildMonstersTab(tabPanels[0].transform);

            // Tab 1: 20 Tháp Phòng Thủ
            tabPanels[1] = CreateTabPanel(containerObj.transform, "Panel_Towers");
            BuildTowersTab(tabPanels[1].transform);

            // Tab 2: Vũ Khí & Tiện Ích
            tabPanels[2] = CreateTabPanel(containerObj.transform, "Panel_Weapons");
            BuildWeaponsTab(tabPanels[2].transform);

            // Tab 3: Thống Kê DPS & Chiến Thuật
            tabPanels[3] = CreateTabPanel(containerObj.transform, "Panel_Stats");
            BuildStatsTab(tabPanels[3].transform);

            SwitchTab(0);
        }

        private GameObject CreateTabPanel(Transform parent, string name)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5f;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            return panel;
        }

        private void SwitchTab(int tabIndex)
        {
            currentTab = tabIndex;
            for (int i = 0; i < 4; i++)
            {
                if (tabPanels[i] != null) tabPanels[i].SetActive(i == tabIndex);
                if (tabTabButtons[i] != null)
                {
                    bool isSel = (i == tabIndex);
                    tabTabButtons[i].color = isSel ? new Color(0.95f, 0.72f, 0.15f, 1f) : new Color(0.2f, 0.3f, 0.45f, 1f);
                    var txt = tabTabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.fontStyle = isSel ? FontStyles.Bold : FontStyles.Normal;
                        txt.color = isSel ? new Color(0.1f, 0.1f, 0.1f, 1f) : Color.white;
                    }
                }
            }
        }

        private void BuildMonstersTab(Transform parent)
        {
            CreateSectionLabel(parent, "👹 BĂNG CƯỚP ĐẠO TẶC (Bandit Faction):");
            var rowBandit = CreateButtonRow(parent, 4);
            CreateButton(rowBandit, "Kẻ Bắt Heo (T)", () => SpawnEnemy(EnemyTypeId.KeBatHeo));
            CreateButton(rowBandit, "Kẻ Cào Phá (Y)", () => SpawnEnemy(EnemyTypeId.KeCaoPha));
            CreateButton(rowBandit, "Đầu Lĩnh (J)", () => SpawnEnemy(EnemyTypeId.DauLinhTrom));
            CreateButton(rowBandit, "Chúa Trộm (K)", () => SpawnEnemy(EnemyTypeId.ChuaTromRiuSat));

            CreateSectionLabel(parent, "🛸 BINH ĐOÀN NGOÀI HÀNH TINH (Alien Tech):");
            var rowAlien1 = CreateButtonRow(parent, 3);
            CreateButton(rowAlien1, "UFO Trinh Sát (I)", () => SpawnEnemy(EnemyTypeId.UfoTrinhSat));
            CreateButton(rowAlien1, "UFO Thu Hoạch (O)", () => SpawnEnemy(EnemyTypeId.UfoThuHoach));
            CreateButton(rowAlien1, "Lính Alien (G)", () => SpawnEnemy(EnemyTypeId.AlienGrunt));

            var rowAlien2 = CreateButtonRow(parent, 3);
            CreateButton(rowAlien2, "Alien Elite (E)", () => SpawnEnemy(EnemyTypeId.AlienElite));
            CreateButton(rowAlien2, "Boss Warlord (L)", () => SpawnEnemy(EnemyTypeId.BossAlienWarlord));
            CreateButton(rowAlien2, "Tàu Mẹ UFO (M)", () => SpawnEnemy(EnemyTypeId.UfoMothership));

            CreateSectionLabel(parent, "🌿 DỊCH HỌA & HUYỀN BÍ (Bio & Mystic):");
            var rowBio = CreateButtonRow(parent, 3);
            CreateButton(rowBio, "Chuột Kho (U)", () => SpawnEnemy(EnemyTypeId.ChuotGamKho));
            CreateButton(rowBio, "Chó Sương (N)", () => SpawnEnemy(EnemyTypeId.ChoSuong));
            CreateButton(rowBio, "Bóng Huyền (B)", () => SpawnEnemy(EnemyTypeId.BongHuyen));

            CreateSectionLabel(parent, "🚨 KÍCH HOẠT CÁC ĐỢT ĐỘT KÍCH (RAIDS):");
            var rowRaid1 = CreateButtonRow(parent, 3);
            CreateButton(rowRaid1, "Đạo Tặc Raid (R)", () => EnemyWaveSpawner.Instance?.TriggerWave("Đạo Tặc Thảo Nguyên", 3, RaidType.BanditRaid));
            CreateButton(rowRaid1, "Phục Kích 3 (5)", () => EnemyWaveSpawner.Instance?.TriggerBanditAmbush3(Vector2.zero));
            CreateButton(rowRaid1, "Trại Cướp (6)", () => EnemyWaveSpawner.Instance?.TriggerBanditBossSiege());

            var rowRaid2 = CreateButtonRow(parent, 3);
            CreateButton(rowRaid2, "Alien Thả (7)", () => EnemyWaveSpawner.Instance?.TriggerAlienDrop3(Vector2.zero));
            CreateButton(rowRaid2, "Binh Đoàn UFO (8)", () => EnemyWaveSpawner.Instance?.TriggerWave("Binh Đoàn Alien UFO", 4, RaidType.AlienRaid));
            CreateButton(rowRaid2, "Tàu Mẹ Đổ Bộ (9)", () => EnemyWaveSpawner.Instance?.TriggerMothershipInvasion());
        }

        private void BuildTowersTab(Transform parent)
        {
            CreateSectionLabel(parent, "⚡ KHAI HỎA TRÌNH DIỄN 20 THÁP PHÒNG THỦ:");
            var fireAllRow = CreateButtonRow(parent, 1);
            var fireAllBtn = CreateButton(fireAllRow, "⚡ KHAI HỎA TOÀN BỘ 20 THÁP CÙNG LÚC (Phím F)", FireAll20Towers);
            var fireImg = fireAllBtn.GetComponent<Image>();
            if (fireImg != null) fireImg.color = new Color(0.85f, 0.25f, 0.15f, 1f);

            CreateSectionLabel(parent, "🎯 BẮN TRÌNH DIỄN THEO PHE (FACTIONS):");
            var factionRow = CreateButtonRow(parent, 3);
            CreateButton(factionRow, "5 Tháp Alien", () => FireFactionTowers("Alien"));
            CreateButton(factionRow, "5 Tháp Bandit", () => FireFactionTowers("Bandit"));
            CreateButton(factionRow, "5 Tháp Bio/Mystic", () => FireFactionTowers("BioMystic"));

            var bachVanRow = CreateButtonRow(parent, 2);
            CreateButton(bachVanRow, "Bạch Vân · Cửu Tiêu Lôi Pháo", TriggerBachVanMegaLightning);
            CreateButton(bachVanRow, "Nạp Đầy Đạn & Sửa Tháp", RefillAllTowersAmmo);

            CreateSectionLabel(parent, "💡 THỬ NGHIỆM ÁNH SÁNG DIEGETIC (AMMO LIGHTING):");
            var ammoTestRow = CreateButtonRow(parent, 3);
            CreateButton(ammoTestRow, "Đầy Đạn (100%)", () => SetAllTowersAmmoRatio(1f));
            CreateButton(ammoTestRow, "Đạn Yếu (<25%)", () => SetAllTowersAmmoRatio(0.15f));
            CreateButton(ammoTestRow, "Hết Đạn (0%)", () => SetAllTowersAmmoRatio(0f));

            CreateSectionLabel(parent, "📋 DANH MỤC CÔNG NGHỆ 20 THÁP TRIỂN LÃM:");
            var descBox = new GameObject("TowerDescBox", typeof(RectTransform));
            descBox.transform.SetParent(parent, false);
            var descRt = descBox.GetComponent<RectTransform>();
            descRt.sizeDelta = new Vector2(336f, 180f);
            var descLe = descBox.AddComponent<LayoutElement>();
            descLe.minHeight = 180f;
            descLe.preferredHeight = 180f;

            var descBg = descBox.AddComponent<Image>();
            descBg.sprite = UISpriteLoader.GetFrameSlotNormal();
            descBg.color = new Color(0f, 0f, 0f, 0.65f);

            var descTxtObj = new GameObject("Text", typeof(RectTransform));
            descTxtObj.transform.SetParent(descBox.transform, false);
            var txtRt = descTxtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(8f, 6f);
            txtRt.offsetMax = new Vector2(-8f, -6f);

            var descTxt = descTxtObj.AddComponent<TextMeshProUGUI>();
            descTxt.fontSize = 10.5f;
            descTxt.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            descTxt.text = 
                "• <color=#38bdf8><b>Alien Tech (5):</b></color> Laser Hội Tụ, Pháo Cối Lượng Tử, Phản Trọng Lực, EMP Choáng, Mega Beam.\n" +
                "• <color=#fb923c><b>Bandit Scrap (5):</b></color> Nỏ Liên Châu, Máy Bắn Rìu Cưa, Tháp Phun Lửa, Lao Móc Xích, Đại Bác Chúa Trộm.\n" +
                "• <color=#a78bfa><b>Bio & Mystic (5):</b></color> Ụ Gai Độc 360°, Trụ Nấm Bào Tử Mù, Ống Bơm Axit, Cột Tổ Hút Hồn, Cột Dị Cốt Sương.\n" +
                "• <color=#fcd34d><b>Cơ Bản & Bạch Vân (5):</b></color> Nỏ Xuyên Vân, Bẫy Chông Gỗ, Cửu Tiêu Lôi Pháo, Thiên Nhãn Đài, Vạn Linh Trận.";
        }

        private void BuildWeaponsTab(Transform parent)
        {
            CreateSectionLabel(parent, "⚔️ CHỌN VŨ KHÍ KHOA (Phím 1, 2, 3, 4 hoặc Click):");
            var wepRow = CreateButtonRow(parent, 4);
            CreateWeaponButton(wepRow, "kiem_sat", "[1] Kiếm", () => EquipWeapon("kiem_sat"));
            CreateWeaponButton(wepRow, "bua_ta", "[2] Búa", () => EquipWeapon("bua_ta"));
            CreateWeaponButton(wepRow, "giao_tre", "[3] Giáo", () => EquipWeapon("giao_tre"));
            CreateWeaponButton(wepRow, "dao_go", "[4] Đao", () => EquipWeapon("dao_go"));

            var wepStatusObj = new GameObject("WeaponStatusLabel", typeof(RectTransform));
            wepStatusObj.transform.SetParent(parent, false);
            var wepStatusRt = wepStatusObj.GetComponent<RectTransform>();
            wepStatusRt.sizeDelta = new Vector2(336f, 18f);
            var wepStatusLe = wepStatusObj.AddComponent<LayoutElement>();
            wepStatusLe.minHeight = 18f;
            wepStatusLe.preferredHeight = 18f;
            weaponStatusText = wepStatusObj.AddComponent<TextMeshProUGUI>();
            weaponStatusText.fontSize = 11f;
            weaponStatusText.color = new Color(0.95f, 0.9f, 0.75f, 1f);

            CreateSectionLabel(parent, "🛠️ THAO TRƯỜNG & LUYỆN PHẢN ĐÒN (PARRY/DEFLECT):");
            var trainRow = CreateButtonRow(parent, 3);
            CreateButton(trainRow, "Bắn Đạn (P)", FireTestProjectile);
            toggleTurretBtn = CreateButton(trainRow, "Auto: TẮT", ToggleAutoTurret);
            CreateButton(trainRow, "Thả Heo (H)", SpawnTestPig);

            var utilRow = CreateButtonRow(parent, 2);
            CreateButton(utilRow, "Hồi Máu & Nộ & Thể Lực", RefillPlayer);
            CreateButton(utilRow, "Dọn Sạch Đấu Trường (C)", ClearAllEnemies);

            CreateSectionLabel(parent, "💎 TRIỂN LÃM VẬT PHẨM & BẢN VẼ RƠI (LOOT BEAMS):");
            var lootRow = CreateButtonRow(parent, 1);
            var lootBtn = CreateButton(lootRow, "✨ Rải 15 Bản Vẽ + Vật Liệu Quái Rơi Cột Sáng", SpawnAllLootDropsShowcase);
            var lootImg = lootBtn.GetComponent<Image>();
            if (lootImg != null) lootImg.color = new Color(0.2f, 0.6f, 0.5f, 1f);
        }

        private void BuildStatsTab(Transform parent)
        {
            CreateSectionLabel(parent, "📊 BẢNG THEO DÕI DPS & CHỈ SỐ THỰC CHIẾN:");

            var statsBox = new GameObject("StatsBox", typeof(RectTransform));
            statsBox.transform.SetParent(parent, false);
            var statsRt = statsBox.GetComponent<RectTransform>();
            statsRt.sizeDelta = new Vector2(336f, 220f);
            var statsLe = statsBox.AddComponent<LayoutElement>();
            statsLe.minHeight = 220f;
            statsLe.preferredHeight = 220f;

            var statsBg = statsBox.AddComponent<Image>();
            statsBg.sprite = UISpriteLoader.GetFrameSlotNormal();
            statsBg.color = new Color(0f, 0f, 0f, 0.65f);

            var statsTxtObj = new GameObject("StatsText", typeof(RectTransform));
            statsTxtObj.transform.SetParent(statsBox.transform, false);
            var statsTxtRt = statsTxtObj.GetComponent<RectTransform>();
            statsTxtRt.anchorMin = Vector2.zero;
            statsTxtRt.anchorMax = Vector2.one;
            statsTxtRt.offsetMin = new Vector2(10f, 8f);
            statsTxtRt.offsetMax = new Vector2(-10f, -8f);

            statsText = statsTxtObj.AddComponent<TextMeshProUGUI>();
            statsText.fontSize = 11.5f;
            statsText.color = Color.white;
            statsText.alignment = TextAlignmentOptions.TopLeft;

            var btnRow = CreateButtonRow(parent, 2);
            CreateButton(btnRow, "Reset Thống Kê", ResetStats);
            CreateButton(btnRow, "Hồi Phục Player", RefillPlayer);

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
            foreach (var p in FindObjectsByType<PigAgentView>())
            {
                if (p != null && p.PigModel != null && p.PigModel.IsAlive)
                {
                    totalPigs++;
                    if (p.IsCaptured) capturedPigs++;
                }
            }

            int aliveEnemies = 0;
            foreach (var e in FindObjectsByType<EnemyAgentView>())
            {
                if (e != null && e.IsAlive) aliveEnemies++;
            }

            int towersCount = 0;
            foreach (var t in FindObjectsByType<DefenseTower2DView>())
            {
                if (t != null) towersCount++;
            }

            statsText.text = 
                $"• <b>Trang bị Khoa:</b> <color=#fcd34d><b>{wepName}</b></color> [{wepInfo}]\n" +
                $"• <b>Đòn đánh cuối:</b> <color=#fcd34d>{Mathf.RoundToInt(LastHitDamage)}</color> DMG\n" +
                $"• <b>Tổng sát thương gây ra:</b> <color=#38bdf8>{Mathf.RoundToInt(TotalDamageDealt)}</color> ({TotalHits} đòn)\n" +
                $"• <b>Đỡ đòn hoàn hảo (Parry):</b> <color=#a7f3d0>{TotalParries}</color> lần\n" +
                $"• <b>Tia đạn phản xạ (Deflect):</b> <color=#67e8f9>{TotalDeflections}</color> tia\n" +
                $"• <b>Đàn heo đồng cỏ:</b> <color=#f472b6><b>{totalPigs}</b> chú</color> (Bị cướp: <color=#ef4444><b>{capturedPigs}</b></color>)\n" +
                $"• <b>Quái trên sân đấu:</b> <color=#fb7185><b>{aliveEnemies}</b> tên</color>\n" +
                $"• <b>Tháp phòng thủ triển lãm:</b> <color=#4ade80><b>{towersCount}</b> tháp</color>";
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
            rt.sizeDelta = new Vector2(336f, 16f);
            var le = obj.AddComponent<LayoutElement>();
            le.minHeight = 16f;
            le.preferredHeight = 16f;

            var txt = obj.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = 11f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(0.75f, 0.88f, 0.98f, 0.95f);
        }

        private Transform CreateButtonRow(Transform parent, int count)
        {
            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(336f, 30f);

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 30f;
            le.preferredHeight = 30f;
            le.flexibleWidth = 1f;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
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
            le.minHeight = 28f;
            le.preferredHeight = 30f;
            le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameActionButton();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.22f, 0.38f, 0.58f, 1f);
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
            txt.fontSize = 10.5f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;

            return btn;
        }

        private static readonly Dictionary<string, Vector3> ShowcaseTowerCoordinates = new Dictionary<string, Vector3>
        {
            // Bạch Vân & Cơ Bản (Y = 15.5)
            { "Tower_NoXuyenVan", new Vector3(-22f, 15.5f, 0f) },
            { "Tower_BayChongGo", new Vector3(-19f, 15.5f, 0f) },
            { "Tower_CuuTieuLoiPhao", new Vector3(-16f, 15.5f, 0f) },
            { "Tower_ThienNhanVongDai", new Vector3(-13f, 15.5f, 0f) },
            { "Tower_VanLinhHoiNguyenTran", new Vector3(-10f, 15.5f, 0f) },
            // Alien Tech (Y = 15.5)
            { "Tower_TruPlasmaHoiTu", new Vector3(10f, 15.5f, 0f) },
            { "Tower_PhaoCoiLuongTu", new Vector3(13f, 15.5f, 0f) },
            { "Tower_ThapPhanTrongLuc", new Vector3(16f, 15.5f, 0f) },
            { "Tower_TruXungDienEMP", new Vector3(19f, 15.5f, 0f) },
            { "Tower_PhaoDaiPhanVatChat", new Vector3(22f, 15.5f, 0f) },
            // Bandit Scrap (Y = -14.0)
            { "Tower_NoLienChauThep", new Vector3(-8f, -14.0f, 0f) },
            { "Tower_MayBanRiuCua", new Vector3(-4f, -14.0f, 0f) },
            { "Tower_ThapPhunLuaHacIn", new Vector3(0f, -14.0f, 0f) },
            { "Tower_PhaoLaoMocXich", new Vector3(4f, -14.0f, 0f) },
            { "Tower_DaiBacChuaTrom", new Vector3(8f, -14.0f, 0f) },
            // Bio & Mystic (Y = -9.5)
            { "Tower_UGaiDocGamNhom", new Vector3(-22f, -9.5f, 0f) },
            { "Tower_TruNamBaoTuMu", new Vector3(-19f, -9.5f, 0f) },
            { "Tower_OngBomDichAxit", new Vector3(-16f, -9.5f, 0f) },
            { "Tower_CotToHutHon", new Vector3(-13f, -9.5f, 0f) },
            { "Tower_CotDiCotSuongHuyen", new Vector3(-10f, -9.5f, 0f) }
        };

        /// <summary>
        /// Tự động dọn sạch sàn đấu trung tâm và định vị chính xác toàn bộ 7 Banner, 20 Tháp, Signboard, nhãn Dummy
        /// Hoạt động cả ở Edit Mode lẫn Play Mode, đảm bảo không bao giờ bị dồn cục tại (0, 0).
        /// </summary>
        public void EnforceArenaLayoutCleanliness()
        {
            // 1. Dời toàn bộ 7 Banners về đúng tọa độ phân khu của chúng
            RepositionBannerByKeyword("ROYAL COMBAT COLOSSEUM", new Vector3(0f, 7.5f, 0f));
            RepositionBannerByKeyword("SÀN ĐẤU GIÁC ĐẤU", new Vector3(0f, 7.5f, 0f));
            RepositionBannerByKeyword("BẠCH VÂN THẦN KHÍ", new Vector3(-16f, 18.8f, 0f));
            RepositionBannerByKeyword("ALIEN TECH", new Vector3(16f, 18.8f, 0f));
            RepositionBannerByKeyword("BANDIT SCRAP", new Vector3(0f, -11.0f, 0f));
            RepositionBannerByKeyword("TẠP PHẾ BĂNG CƯỚP", new Vector3(0f, -11.0f, 0f));
            RepositionBannerByKeyword("BIO & MYSTIC", new Vector3(-16f, -6.8f, 0f));
            RepositionBannerByKeyword("DỊCH HỌA & HUYỀN BÍ", new Vector3(-16f, -6.8f, 0f));
            RepositionBannerByKeyword("CHIẾN HẠM ALIEN", new Vector3(20.5f, 7.5f, 0f));
            RepositionBannerByKeyword("SINH VẬT & CHIẾN HẠM", new Vector3(20.5f, 7.5f, 0f));
            RepositionBannerByKeyword("DOANH TRẠI BĂNG CƯỚP", new Vector3(20.5f, -9.0f, 0f));
            RepositionBannerByKeyword("BĂNG CƯỚP ĐẠO TẶC", new Vector3(20.5f, -9.0f, 0f));

            // 2. Định vị chính xác toàn bộ 20 tháp triển lãm và Signboard
            foreach (var tower in FindObjectsByType<DefenseTower2DView>())
            {
                if (tower == null) continue;
                tower.EnsureBuildingModelInitialized();
                string tName = tower.gameObject.name;
                if (ShowcaseTowerCoordinates.TryGetValue(tName, out Vector3 targetPos))
                {
                    // Nếu tháp bị trôi về (0,0) hoặc lệch khỏi vị trí triển lãm
                    if (Vector3.Distance(tower.transform.localPosition, targetPos) > 0.05f)
                    {
                        tower.SetAnchorPosition(targetPos);
                    }
                }

                // Đồng bộ sprite hiển thị chuẩn theo Blueprint ID (kể cả Bio & Mystic)
                string bpId = tower.BuildingModel?.BlueprintId ?? tName.Replace("Tower_", "").Trim();
                int paren = bpId.IndexOf('(');
                if (paren >= 0) bpId = bpId.Substring(0, paren).Trim();
                var expectedSprite = UISpriteLoader.GetTowerSprite(bpId);
                if (expectedSprite != null && tower.BaseRenderer != null && tower.BaseRenderer.sprite != expectedSprite)
                {
                    tower.BaseRenderer.sprite = expectedSprite;
                }
                if (tower.GlowMaskRenderer != null && tower.BaseRenderer != null && tower.GlowMaskRenderer.sprite != tower.BaseRenderer.sprite)
                {
                    tower.GlowMaskRenderer.sprite = tower.BaseRenderer.sprite;
                }

                // Loại bỏ hoàn toàn nhãn chữ/tên nổi trên đầu tháp phòng thủ
                var sign = tower.transform.Find("Signboard");
                if (sign != null)
                {
                    SafeDestroy(sign.gameObject);
                }

                // Loại bỏ ngọn đuốc chiếu sáng thừa thãi trên Tháp Phun Lửa Hắc Ín
                if (tName.Contains("ThapPhunLua"))
                {
                    var torch = tower.transform.Find("DiegeticTorch");
                    if (torch != null)
                    {
                        SafeDestroy(torch.gameObject);
                    }
                    tower.TorchRenderer = null;
                }
            }

            // 3. Định vị chính xác toàn bộ Dummy và Label tương ứng với từng tháp
            foreach (var dummy in FindObjectsByType<TrainingDummy2DView>())
            {
                if (dummy == null) continue;
                string dName = dummy.gameObject.name;
                string matchingTowerName = "Tower_" + dName.Replace("Dummy_", "");
                if (ShowcaseTowerCoordinates.TryGetValue(matchingTowerName, out Vector3 towerPos))
                {
                    float dummyY = (towerPos.y > 0) ? (towerPos.y - 2.8f) : (towerPos.y + 2.8f);
                    Vector3 expectedDummyPos = new Vector3(towerPos.x, dummyY, 0f);
                    if (Vector3.Distance(dummy.transform.localPosition, expectedDummyPos) > 0.05f)
                    {
                        dummy.transform.localPosition = expectedDummyPos;
                    }
                }

                var label = dummy.transform.Find("Label");
                if (label != null)
                {
                    label.localPosition = new Vector3(0f, 0.85f, 0f);
                    var rt = label.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(0f, 0.85f);
                        rt.localPosition = new Vector3(0f, 0.85f, 0f);
                    }
                }
            }

            // 4. Sắp xếp lại nhãn của máy bắn đạn Deflect Trainer
            var turretStation = GameObject.Find("Test_Turret_Station");
            if (turretStation != null)
            {
                var label = turretStation.transform.Find("Turret_Label");
                if (label != null)
                {
                    label.localPosition = new Vector3(0f, 1.2f, 0f);
                    var rt = label.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(0f, 1.2f);
                        rt.localPosition = new Vector3(0f, 1.2f, 0f);
                    }
                }
            }

            // 5. Sắp xếp lại nhãn của các kệ vũ khí Dojo
            var pedestalsRoot = GameObject.Find("Weapon_Display_Pedestals");
            if (pedestalsRoot != null)
            {
                for (int i = 0; i < pedestalsRoot.transform.childCount; i++)
                {
                    var child = pedestalsRoot.transform.GetChild(i);
                    var label = child.Find("Label");
                    if (label != null)
                    {
                        label.localPosition = new Vector3(1.8f, 0f, 0f);
                        var rt = label.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchoredPosition = new Vector2(1.8f, 0f);
                            rt.localPosition = new Vector3(1.8f, 0f, 0f);
                        }
                    }
                }
            }

            // 6. Sắp xếp lại nhãn khu trưng bày Loot Drops
            var lootShowcase = GameObject.Find("Loot_Showcase_Gallery");
            if (lootShowcase != null)
            {
                var label = lootShowcase.transform.Find("Label");
                if (label != null)
                {
                    label.localPosition = new Vector3(0f, 1.2f, 0f);
                    var rt = label.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(0f, 1.2f);
                        rt.localPosition = new Vector3(0f, 1.2f, 0f);
                    }
                }
            }

            // 7. Sắp xếp lại biển hiệu ở các chuồng quái
            var sanctuaries = GameObject.Find("Monster_Exhibition_Sanctuaries");
            if (sanctuaries != null)
            {
                foreach (var piece in sanctuaries.GetComponentsInChildren<Transform>(true))
                {
                    if (piece.name.StartsWith("Piece_") || piece.name.StartsWith("Exhibit_"))
                    {
                        var sign = piece.Find("Sign");
                        if (sign != null)
                        {
                            sign.localPosition = new Vector3(0f, 1.1f, 0f);
                            var rt = sign.GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                rt.anchoredPosition = new Vector2(0f, 1.1f);
                                rt.localPosition = new Vector3(0f, 1.1f, 0f);
                            }
                        }
                    }
                }
            }

            // 8. Đảm bảo Dev_Mode_Popup không hiển thị đè lên màn hình khi khởi động
            var devPopup = GameObject.Find("Dev_Mode_Popup");
            if (devPopup != null)
            {
                var devComp = DevModePopup.Instance ?? FindAnyObjectByType<DevModePopup>();
                if (devComp == null || !devComp.IsOpen)
                {
                    devPopup.SetActive(false);
                }
            }

            // 9. Quét sạch toàn bộ khu vực sàn đấu giác đấu trung tâm (|x| < 7.5, |y| < 5.5)
            // Bảo đảm tuyệt đối không còn bất kỳ banner, chữ hay tháp nào chắn sàn đấu của Player
            foreach (var tmp in FindObjectsByType<TextMeshPro>())
            {
                if (tmp == null || tmp.GetComponentInParent<Canvas>() != null) continue;
                Vector3 p = tmp.transform.position;
                if (Mathf.Abs(p.x) < 7.5f && Mathf.Abs(p.y) < 5.5f)
                {
                    if (tmp.name.StartsWith("Banner_") || (tmp.text != null && tmp.text.Contains("KHU")))
                    {
                        // Đẩy lên Colosseum banner
                        tmp.transform.position = new Vector3(0f, 7.5f, 0f);
                        if (tmp.rectTransform != null)
                        {
                            tmp.rectTransform.position = new Vector3(0f, 7.5f, 0f);
                            tmp.rectTransform.localPosition = new Vector3(0f, 7.5f, 0f);
                            tmp.rectTransform.anchoredPosition = new Vector2(0f, 7.5f);
                        }
                    }
                }
            }
        }

        private void RepositionBannerByKeyword(string keyword, Vector3 targetPos)
        {
            var allTMPs = FindObjectsByType<TextMeshPro>();
            string kw = keyword.ToUpperInvariant();
            foreach (var tmp in allTMPs)
            {
                if (tmp == null) continue;
                string goName = tmp.gameObject.name.ToUpperInvariant();
                string text = tmp.text != null ? tmp.text.ToUpperInvariant() : "";
                if (goName.Contains(kw) || text.Contains(kw))
                {
                    tmp.transform.position = targetPos;
                    if (tmp.rectTransform != null)
                    {
                        tmp.rectTransform.position = targetPos;
                        tmp.rectTransform.localPosition = targetPos;
                        tmp.rectTransform.anchoredPosition = new Vector2(targetPos.x, targetPos.y);
                    }
                }
            }
        }
        #endregion
    }
}
