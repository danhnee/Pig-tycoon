using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Điều khiển Hệ thống Chiến đấu Chuẩn hóa cho Nhân vật 64x64 pixel (Stardew Valley 1.6 Authentic Mechanics).
    /// Các cơ chế cốt lõi:
    /// 1. Micro-Lunge (Bước nhích khi ra đòn) & Attack Commit (Hãm tốc độ di chuyển trong lúc vung đòn).
    /// 2. Input Buffering (Hàng đợi lệnh anotherClick).
    /// 3. Đỡ đòn & Phản kích (Parry & Counter Stance) cho Kiếm/Đao; Dập đất (Slam) cho Búa; Đâm liên hoàn (Flurry) cho Giáo.
    /// 4. Phá / Chặn đạn bay (Melee Projectile Deflection).
    /// 5. Bất tử tạm thời (i-Frames: 1.0s cho người chơi, 0.15s cho quái vật).
    /// 6. Công thức sát thương & chí mạng chuẩn Stardew Valley 1.6 kết hợp chỉ số nhân vật.
    /// </summary>
    [RequireComponent(typeof(PlayerMobileController))]
    public class PlayerCombatController : MonoBehaviour, IDamageable
    {
        public static PlayerCombatController Instance { get; private set; }

        [Header("Weapon Configuration")]
        public WeaponDefinitionSO EquippedWeapon;

        [Header("Primary Attack UI")]
        public GameObject AttackButtonRoot;
        public Button AttackButton;
        public Image AttackButtonIcon;
        public Image CooldownRadialFill;
        public TextMeshProUGUI CooldownText;

        [Header("Secondary Skill / Parry UI")]
        public GameObject SecondaryButtonRoot;
        public Button SecondaryButton;
        public Image SecondaryButtonIcon;
        public Image SecondaryCooldownRadialFill;
        public TextMeshProUGUI SecondaryCooldownText;

        private PlayerMobileController mobileController;
        private CharacterSpriteAnimator spriteAnimator;
        private Rigidbody2D rb;

        // Timers & Combat State
        private float nextAttackTime = 0f;
        private float nextSecondaryTime = 0f;
        private float invincibilityTimer = 0f;
        private float secondsSinceLastCombatAction = 5.0f;
        private float regenUiSyncTimer = 0f;
        private bool isArmed = false;
        private bool bufferedAttack = false;
        private bool isAttacking = false;
        private bool isParrying = false;
        private bool isHoldingGuardStance = false;
        private Coroutine attackRoutine;
        private Coroutine parryRoutine;
        private Coroutine invincibilityBlinkRoutine;

        public bool IsArmed => isArmed;
        public bool IsAttacking => isAttacking;
        public bool IsParrying => isParrying;
        public bool IsHoldingGuardStance => isHoldingGuardStance;
        public WeaponDefinitionSO CurrentWeapon => EquippedWeapon;

        public bool IsAlive
        {
            get
            {
                var charData = MobileGameController.Instance?.Engine?.Character;
                return charData != null && !charData.IsFainted && charData.CurrentGuard > 0f; // thanh sống còn = Chống Chịu
            }
        }

        public Vector2 Position => transform.position;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            mobileController = GetComponent<PlayerMobileController>();
            spriteAnimator = GetComponent<CharacterSpriteAnimator>();
            rb = GetComponent<Rigidbody2D>();

            // Khởi tạo vũ khí mặc định nếu chưa gán
            if (EquippedWeapon == null)
            {
                EquippedWeapon = WeaponDatabase.GetWeapon("dao_go");
            }
        }

        /// <summary>The weapon the Core character wears (GDD 12.3); null = bare hands.</summary>
        EquipmentItem CoreWeapon => MobileGameController.Instance?.Engine?.Character is CharacterData c ? MartialProgression.Weapon(c) : null;

        string syncedWeaponId = "";
        int arrowsDay = -1;
        /// <summary>Nỏ Tay bolts carried (GDD 12.3: tối đa 40 tên); refilled each morning (ADR-0017).</summary>
        public int Arrows { get; private set; } = 40;
        bool wasWaveActive;

        /// <summary>Rebuilds the swing profile when the worn weapon changes: reach, arc, interval and VFX follow the archetype.</summary>
        void SyncWeaponFromEquipment()
        {
            var item = CoreWeapon;
            string id = item != null ? item.Id : "";
            if (id == syncedWeaponId && EquippedWeapon != null) return;
            syncedWeaponId = id;
            var w = ScriptableObject.CreateInstance<WeaponDefinitionSO>();
            w.Id = id.Length > 0 ? id : "tay_khong";
            w.DisplayName = item != null ? item.Name : "Tay không";
            var arch = item != null ? item.Archetype : WeaponArchetype.None;
            w.ActionType = arch == WeaponArchetype.Smash ? CombatActionType.Smash : arch == WeaponArchetype.Thrust || arch == WeaponArchetype.Ranged ? CombatActionType.Thrust : CombatActionType.Slash;
            w.SpecialSkill = arch == WeaponArchetype.Smash ? WeaponSpecialSkill.Slam : arch == WeaponArchetype.Thrust ? WeaponSpecialSkill.Flurry : WeaponSpecialSkill.Parry;
            w.Cooldown = item != null && item.AttackInterval > 0f ? item.AttackInterval : 0.5f;
            w.SecondaryCooldown = Mathf.Max(1.2f, w.Cooldown * 2f);
            // Reach/arc per archetype (ADR-0017): knife sweeps close, spear reaches, hammer lands one spot, crossbow 12 m.
            switch (arch)
            {
                case WeaponArchetype.Thrust: w.AttackRange = 2.1f; w.ArcAngle = 40f; w.KnockbackForce = 3.5f; break;
                case WeaponArchetype.Smash: w.AttackRange = 1.3f; w.ArcAngle = 90f; w.KnockbackForce = 5.5f; break;
                case WeaponArchetype.Ranged: w.AttackRange = 12f; w.ArcAngle = 10f; w.KnockbackForce = 1.5f; break;
                case WeaponArchetype.Slash: w.AttackRange = 1.4f; w.ArcAngle = 130f; w.KnockbackForce = 2.5f; break;
                default: w.AttackRange = 1.0f; w.ArcAngle = 90f; w.KnockbackForce = 1.5f; break;
            }
            w.CritMultiplier = 2.0f;
            string icon = arch == WeaponArchetype.Smash ? "weapon_bua_ta" : arch == WeaponArchetype.Thrust ? "weapon_giao_tre" : arch == WeaponArchetype.Slash ? "weapon_dao_go" : "weapon_kiem_sat";
            w.Icon = UISpriteLoader.LoadSprite("Art/Sprites/Weapons/" + icon + ".png", 32);
            SwitchWeapon(w);
            if (HotbarController.Instance != null) OnHotbarToolChanged(HotbarController.Instance.CurrentTool);
        }

        private void Start()
        {
            EnsureCombatUI();

            if (AttackButton != null)
            {
                AttackButton.onClick.RemoveAllListeners();
                AttackButton.onClick.AddListener(ExecuteAttack);
            }

            if (SecondaryButton != null)
            {
                SecondaryButton.onClick.RemoveAllListeners();
                SecondaryButton.onClick.AddListener(ExecuteSecondarySkill);
            }

            if (HotbarController.Instance != null)
            {
                HotbarController.Instance.OnToolChanged += OnHotbarToolChanged;
                OnHotbarToolChanged(HotbarController.Instance.CurrentTool);
            }
            else
            {
                isArmed = true;
                UpdateUIVisibility();
            }
        }

        private void OnDestroy()
        {
            if (HotbarController.Instance != null)
            {
                HotbarController.Instance.OnToolChanged -= OnHotbarToolChanged;
            }
        }

        #region UI Generation & Setup
        private void EnsureCombatUI()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Primary Attack Button (Góc dưới bên phải)
            if (AttackButtonRoot == null)
            {
                var existingPrimary = canvas.transform.Find("Mobile_Primary_Attack_Button");
                if (existingPrimary != null)
                {
                    AttackButtonRoot = existingPrimary.gameObject;
                    AttackButton = existingPrimary.GetComponent<Button>();
                    AttackButtonIcon = existingPrimary.Find("Icon")?.GetComponent<Image>();
                    CooldownRadialFill = existingPrimary.Find("CooldownFill")?.GetComponent<Image>();
                    CooldownText = existingPrimary.Find("CooldownText")?.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    AttackButtonRoot = new GameObject("Mobile_Primary_Attack_Button");
                    AttackButtonRoot.transform.SetParent(canvas.transform, false);

                    var rt = AttackButtonRoot.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(1f, 0f);
                    rt.anchoredPosition = new Vector2(-28f, 120f);
                    rt.sizeDelta = new Vector2(85f, 85f);

                    var bgImg = AttackButtonRoot.AddComponent<Image>();
                    bgImg.sprite = UISpriteLoader.GetFrameActionButton();
                    bgImg.type = Image.Type.Sliced;
                    bgImg.color = new Color(0.95f, 0.45f, 0.25f, 1f);

                    AttackButton = AttackButtonRoot.AddComponent<Button>();

                    // Icon vũ khí
                    var iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(AttackButtonRoot.transform, false);
                    var iconRt = iconObj.AddComponent<RectTransform>();
                    iconRt.anchorMin = Vector2.zero;
                    iconRt.anchorMax = Vector2.one;
                    iconRt.sizeDelta = new Vector2(-18f, -18f);
                    AttackButtonIcon = iconObj.AddComponent<Image>();
                    AttackButtonIcon.sprite = UISpriteLoader.LoadSprite("Art/Sprites/Weapons/weapon_dao_go.png", 32);
                    AttackButtonIcon.preserveAspect = true;
                    AttackButtonIcon.raycastTarget = false;

                    // Cooldown Radial Fill
                    var cdObj = new GameObject("CooldownFill");
                    cdObj.transform.SetParent(AttackButtonRoot.transform, false);
                    var cdRt = cdObj.AddComponent<RectTransform>();
                    cdRt.anchorMin = Vector2.zero;
                    cdRt.anchorMax = Vector2.one;
                    cdRt.sizeDelta = Vector2.zero;
                    CooldownRadialFill = cdObj.AddComponent<Image>();
                    CooldownRadialFill.sprite = UISpriteLoader.GetFrameActionButton();
                    CooldownRadialFill.type = Image.Type.Filled;
                    CooldownRadialFill.fillMethod = Image.FillMethod.Radial360;
                    CooldownRadialFill.fillOrigin = (int)Image.Origin360.Top;
                    CooldownRadialFill.fillClockwise = false;
                    CooldownRadialFill.color = new Color(0f, 0f, 0f, 0.65f);
                    CooldownRadialFill.raycastTarget = false;
                    CooldownRadialFill.fillAmount = 0f;

                    // Cooldown Text
                    var txtObj = new GameObject("CooldownText");
                    txtObj.transform.SetParent(AttackButtonRoot.transform, false);
                    var txtRt = txtObj.AddComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;
                    CooldownText = txtObj.AddComponent<TextMeshProUGUI>();
                    CooldownText.fontSize = 22f;
                    CooldownText.fontStyle = FontStyles.Bold;
                    CooldownText.alignment = TextAlignmentOptions.Center;
                    CooldownText.color = Color.white;
                    CooldownText.raycastTarget = false;
                    CooldownText.gameObject.SetActive(false);
                }
            }

            // 2. Secondary Skill / Parry Button (Kế bên Primary Attack Button)
            if (SecondaryButtonRoot == null)
            {
                var existingSecondary = canvas.transform.Find("Mobile_Secondary_Skill_Button");
                if (existingSecondary != null)
                {
                    SecondaryButtonRoot = existingSecondary.gameObject;
                    SecondaryButton = existingSecondary.GetComponent<Button>();
                    SecondaryButtonIcon = existingSecondary.Find("Icon")?.GetComponent<Image>();
                    SecondaryCooldownRadialFill = existingSecondary.Find("CooldownFill")?.GetComponent<Image>();
                    SecondaryCooldownText = existingSecondary.Find("CooldownText")?.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    SecondaryButtonRoot = new GameObject("Mobile_Secondary_Skill_Button");
                    SecondaryButtonRoot.transform.SetParent(canvas.transform, false);

                    var rt = SecondaryButtonRoot.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(1f, 0f);
                    rt.anchoredPosition = new Vector2(-125f, 120f);
                    rt.sizeDelta = new Vector2(68f, 68f);

                    var bgImg = SecondaryButtonRoot.AddComponent<Image>();
                    bgImg.sprite = UISpriteLoader.GetFrameActionButton();
                    bgImg.type = Image.Type.Sliced;
                    bgImg.color = new Color(0.25f, 0.6f, 0.95f, 1f); // Xanh dương thủ hộ

                    SecondaryButton = SecondaryButtonRoot.AddComponent<Button>();

                    // Icon kỹ năng
                    var iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(SecondaryButtonRoot.transform, false);
                    var iconRt = iconObj.AddComponent<RectTransform>();
                    iconRt.anchorMin = Vector2.zero;
                    iconRt.anchorMax = Vector2.one;
                    iconRt.sizeDelta = new Vector2(-14f, -14f);
                    SecondaryButtonIcon = iconObj.AddComponent<Image>();
                    SecondaryButtonIcon.sprite = UISpriteLoader.GetFrameSlotSelected();
                    SecondaryButtonIcon.preserveAspect = true;
                    SecondaryButtonIcon.raycastTarget = false;

                    // Cooldown Radial Fill
                    var cdObj = new GameObject("CooldownFill");
                    cdObj.transform.SetParent(SecondaryButtonRoot.transform, false);
                    var cdRt = cdObj.AddComponent<RectTransform>();
                    cdRt.anchorMin = Vector2.zero;
                    cdRt.anchorMax = Vector2.one;
                    cdRt.sizeDelta = Vector2.zero;
                    SecondaryCooldownRadialFill = cdObj.AddComponent<Image>();
                    SecondaryCooldownRadialFill.sprite = UISpriteLoader.GetFrameActionButton();
                    SecondaryCooldownRadialFill.type = Image.Type.Filled;
                    SecondaryCooldownRadialFill.fillMethod = Image.FillMethod.Radial360;
                    SecondaryCooldownRadialFill.fillOrigin = (int)Image.Origin360.Top;
                    SecondaryCooldownRadialFill.fillClockwise = false;
                    SecondaryCooldownRadialFill.color = new Color(0f, 0f, 0f, 0.65f);
                    SecondaryCooldownRadialFill.raycastTarget = false;
                    SecondaryCooldownRadialFill.fillAmount = 0f;

                    // Cooldown Text
                    var txtObj = new GameObject("CooldownText");
                    txtObj.transform.SetParent(SecondaryButtonRoot.transform, false);
                    var txtRt = txtObj.AddComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;
                    SecondaryCooldownText = txtObj.AddComponent<TextMeshProUGUI>();
                    SecondaryCooldownText.fontSize = 18f;
                    SecondaryCooldownText.fontStyle = FontStyles.Bold;
                    SecondaryCooldownText.alignment = TextAlignmentOptions.Center;
                    SecondaryCooldownText.color = Color.white;
                    SecondaryCooldownText.raycastTarget = false;
                    SecondaryCooldownText.gameObject.SetActive(false);
                }
            }

            UpdateUIVisibility();
        }

        private void UpdateUIVisibility()
        {
            // MythHud draws its own icon combat buttons (attack/defend/burst); the legacy squares stay hidden.
            bool show = isArmed && !MythHudController.LegacyActionButtonsHidden;
            if (AttackButtonRoot != null) AttackButtonRoot.SetActive(show);
            if (SecondaryButtonRoot != null) SecondaryButtonRoot.SetActive(show);
        }
        #endregion

        private void OnHotbarToolChanged(StardewToolType tool)
        {
            isArmed = (tool == StardewToolType.DaoGo);
            UpdateUIVisibility();
        }

        public void SwitchWeapon(WeaponDefinitionSO newWeapon)
        {
            if (newWeapon == null) return;
            EquippedWeapon = newWeapon;
            isArmed = true;
            UpdateUIVisibility();

            if (AttackButtonIcon != null && newWeapon.Icon != null)
            {
                AttackButtonIcon.sprite = newWeapon.Icon;
            }

            if (SecondaryButtonIcon != null)
            {
                switch (newWeapon.SpecialSkill)
                {
                    case WeaponSpecialSkill.Parry:
                        SecondaryButtonIcon.sprite = UISpriteLoader.GetIconEnergyBolt() ?? UISpriteLoader.GetFrameSlotHighlight();
                        break;
                    case WeaponSpecialSkill.Slam:
                        SecondaryButtonIcon.sprite = UISpriteLoader.GetFrameWoodPanel();
                        break;
                    case WeaponSpecialSkill.Flurry:
                        SecondaryButtonIcon.sprite = UISpriteLoader.GetIconCloseCross();
                        break;
                }
            }

            CombatArenaController.Instance?.HighlightWeaponButton(newWeapon.Id);
            CombatArenaController.Instance?.UpdateStatsUI();
        }

        public void EquipWeaponById(string weaponId)
        {
            var def = WeaponDatabase.GetWeapon(weaponId);
            if (def != null) SwitchWeapon(def);
        }

        private bool heldByFaint;

        private void Update()
        {
            var eng = MobileGameController.Instance?.Engine;
            // GDD 8.2: knocked out during a wave → lies at the main house until the wave ends (engine.WakeAfterWave)
            bool knockedOut = eng?.Character != null && eng.Character.IsFainted;
            if (mobileController != null && knockedOut != heldByFaint)
            {
                mobileController.CanMove = !knockedOut;
                heldByFaint = knockedOut;
                if (knockedOut) spriteAnimator?.PlayChain(AnimationClips.Faint());
            }
            if (knockedOut) return;
            if (eng?.Character != null)
            {
                var ch = eng.Character;
                ch.Now = Time.time;
                SyncWeaponFromEquipment();
                bool wave = eng.Clock.IsDuringCombatWave;
                MartialProgression.TickRage(ch, Time.deltaTime, secondsSinceLastCombatAction, wave);
                if (Time.time >= nextWhistleCalmScan)
                {
                    nextWhistleCalmScan = Time.time + 1f;
                    var near = new List<Pig>();
                    foreach (var pv in FindObjectsByType<PigAgentView>(FindObjectsSortMode.None))
                        if (pv != null && pv.PigModel != null && Vector2.Distance(pv.transform.position, transform.position) <= 10f) near.Add(pv.PigModel);
                    int calmed = MartialProgression.TryCalmWithHerdingWhistle(ch, near, Time.time, ref whistleCalmReadyAt);
                    if (calmed > 0) PlayerInteractionController.Instance?.ShowFeedback($"Còi Chăn Heo: {calmed} heo hoảng loạn dịu lại (+10 Tinh thần).");
                }
                if (wasWaveActive && !wave) MartialProgression.OnWaveEnded(ch, eng.Clock.CycleNumber);
                wasWaveActive = wave;
                if (arrowsDay != eng.Clock.CurrentDay) { arrowsDay = eng.Clock.CurrentDay; Arrows = 40; }
            }

            // Đếm ngược i-Frames của người chơi
            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.deltaTime;
            }

            secondsSinceLastCombatAction += Time.deltaTime;
            isHoldingGuardStance = isParrying || Input.GetMouseButton(1) || Input.GetKey(KeyCode.L) || Input.GetKey(KeyCode.U) || Input.GetKey(KeyCode.LeftShift);

            // Hồi phục Thanh Thể Lực & Thanh Chống Chịu theo thời gian thực (GDD 9.4)
            var engine = MobileGameController.Instance?.Engine;
            if (engine?.Character != null && !engine.Character.IsFainted)
            {
                bool inCombat = engine.Clock.IsDuringCombatWave || secondsSinceLastCombatAction < 3.0f;
                int healingStationLevel = engine.Defense.Buildings.Exists(b => b.BlueprintId == "VanLinhHoiNguyenTran" && !b.IsBroken) ? 3 : 0;
                float prevSta = engine.Character.Stamina.CurrentStamina;
                float prevGuard = engine.Character.CurrentGuard;

                engine.Character.TickCombatRegen(Time.deltaTime, inCombat, secondsSinceLastCombatAction, healingStationLevel);
                // Hồi Khí (bị động, GDD 12.7): rời combat 5 giây thì hồi Stamina +20%
                if (!inCombat && secondsSinceLastCombatAction >= 5f && MartialProgression.HasPassive(engine.Character, "pig.skill.hoi_khi"))
                {
                    float gained = engine.Character.Stamina.CurrentStamina - prevSta;
                    if (gained > 0f)
                        engine.Character.Stamina.CurrentStamina = Mathf.Min(engine.Character.GetEffectiveMaxStamina(), engine.Character.Stamina.CurrentStamina + gained * 0.20f);
                }

                regenUiSyncTimer += Time.deltaTime;
                if (regenUiSyncTimer >= 0.25f &&
                    (Mathf.Abs(engine.Character.Stamina.CurrentStamina - prevSta) > 0.01f ||
                     Mathf.Abs(engine.Character.CurrentGuard - prevGuard) > 0.01f))
                {
                    regenUiSyncTimer = 0f;
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                }
            }

            // Keys and clicks belong to an open popup or dialogue, not to combat (3 Nov audit #4).
            if (UISpriteLoader.IsAnyModalOrDialogueOpen()) { UpdateCooldownUI(); return; }

            // Lướt né (GDD 9.2 / HANDBOOK P1.08): Left Ctrl — có i-frame, chỉ tránh được đòn cơ bản
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)) ExecuteDodge();
            // Phản kích (Q): chỉ dùng cho đòn đặc biệt, tỉ lệ theo kỹ năng thuần của người chơi
            if (Input.GetKeyDown(KeyCode.Q) && !RadialToolWheelOpen()) PressCounter();

            if (isArmed)
            {
                // Phím bấm PC: Tấn công chính (Chuột Trái, Space, J, K). F is the farm overview, not an attack.
                // Space attacks only with nothing in reach; next to a pig/trough it is PlayerInteractionController's interact.
                bool leftClick = Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
                bool spaceAttack = Input.GetKeyDown(KeyCode.Space) && (PlayerInteractionController.Instance == null || PlayerInteractionController.Instance.CurrentTarget == null);
                if (leftClick || spaceAttack || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K))
                {
                    if (leftClick)
                    {
                        Vector3 mouseWorld = Camera.main != null ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
                        ExecuteAttackTowards(mouseWorld);
                    }
                    else
                    {
                        ExecuteAttack();
                    }
                }

                // Phím bấm PC: Kỹ năng phụ / Đỡ đòn chủ động (Chuột Phải, Phím U, Phím L)
                bool rightClick = Input.GetMouseButtonDown(1) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
                if (rightClick || Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.L))
                {
                    ExecuteSecondarySkill();
                }
            }

            UpdateCooldownUI();
        }

        private void UpdateCooldownUI()
        {
            if (EquippedWeapon == null) return;

            // 1. Primary Attack Cooldown
            float remaining = nextAttackTime - Time.time;
            if (remaining > 0f)
            {
                if (CooldownRadialFill != null)
                {
                    CooldownRadialFill.fillAmount = remaining / EquippedWeapon.Cooldown;
                }
                if (CooldownText != null)
                {
                    CooldownText.text = remaining.ToString("F1");
                    CooldownText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (CooldownRadialFill != null) CooldownRadialFill.fillAmount = 0f;
                if (CooldownText != null) CooldownText.gameObject.SetActive(false);
            }

            // 2. Secondary Skill Cooldown
            float secRemaining = nextSecondaryTime - Time.time;
            if (secRemaining > 0f)
            {
                if (SecondaryCooldownRadialFill != null)
                {
                    SecondaryCooldownRadialFill.fillAmount = secRemaining / EquippedWeapon.SecondaryCooldown;
                }
                if (SecondaryCooldownText != null)
                {
                    SecondaryCooldownText.text = secRemaining.ToString("F1");
                    SecondaryCooldownText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (SecondaryCooldownRadialFill != null) SecondaryCooldownRadialFill.fillAmount = 0f;
                if (SecondaryCooldownText != null) SecondaryCooldownText.gameObject.SetActive(false);
            }
        }

        #region Primary Attack Execution
        /// <summary>
        /// Kích hoạt đòn tấn công cơ bản với Smart Auto-Targeting & Input Buffering (anotherClick)
        /// </summary>
        public void ExecuteAttack()
        {
            if (BackpackPopup.InputBlockedThisFrame) return;
            if (isAttacking)
            {
                bufferedAttack = true; // Lưu cú nhấn tiếp theo để combo mượt mà
                return;
            }
            if (Time.time < nextAttackTime) return;
            if (EquippedWeapon == null) { NoWeaponFeedback(); return; }

            Vector2 aimDir = mobileController != null ? mobileController.FacingDirection : Vector2.down;

            // Smart Auto-Targeting: Tìm quái vật gần nhất trong bán kính 3.5m
            IDamageable nearestTarget = FindNearestTarget(3.5f);
            if (nearestTarget != null)
            {
                aimDir = (nearestTarget.Position - (Vector2)transform.position).normalized;
                if (mobileController != null)
                {
                    mobileController.SetFacingDirection(aimDir);
                }
            }

            if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.down;

            PerformAttackCycle(aimDir, damageMultiplier: 1.0f);
        }

        /// <summary>
        /// Tấn công chỉ định hướng theo vị trí click chuột
        /// </summary>
        public void ExecuteAttackTowards(Vector2 targetWorldPos)
        {
            if (BackpackPopup.InputBlockedThisFrame) return;
            if (isAttacking)
            {
                bufferedAttack = true;
                return;
            }
            if (Time.time < nextAttackTime) return;
            if (EquippedWeapon == null) { NoWeaponFeedback(); return; }

            Vector2 toTarget = targetWorldPos - (Vector2)transform.position;
            if (toTarget.sqrMagnitude < 0.01f) toTarget = Vector2.down;
            Vector2 aimDir = toTarget.normalized;

            if (mobileController != null)
            {
                mobileController.SetFacingDirection(aimDir);
            }

            PerformAttackCycle(aimDir, damageMultiplier: 1.0f);
        }

        private void PerformAttackCycle(Vector2 aimDir, float damageMultiplier)
        {
            secondsSinceLastCombatAction = 0f;

            // Tiêu hao Thể Lực đòn đánh theo GDD 9.2 (Đánh thường 2.5 STA, Đòn mạnh 7.0 STA)
            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData != null)
            {
                float staCost = damageMultiplier > 1.2f ? 7.0f : 2.5f;
                float loadRatio = damageMultiplier > 1.2f ? 0.40f : 0.30f;
                bool stillConscious = charData.TryConsumeCombatStamina(staCost, loadRatio, out _);
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                if (!stillConscious)
                {
                    HandlePlayerFainted(charData.LastFaintReason);
                    return;
                }
            }

            float fatigueCooldownMult = (charData != null && charData.IsFatigued()) ? 1.20f : 1.0f;
            isAttacking = true;
            nextAttackTime = Time.time + EquippedWeapon.Cooldown * fatigueCooldownMult;

            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = StartCoroutine(AttackCycleRoutine(aimDir, damageMultiplier));
        }

        private IEnumerator AttackCycleRoutine(Vector2 aimDir, float damageMultiplier)
        {
            // 1. Attack Commit: Hãm tốc độ di chuyển còn 25% trong lúc vung đòn (chống lướt trượt băng)
            if (mobileController != null)
            {
                mobileController.SpeedMultiplier = 0.25f;
            }

            // 2. Micro-Lunge: Nhích nhẹ theo hướng đánh (0.18 - 0.22m) tạo lực đòn đánh kinetic
            if (rb != null)
            {
                rb.AddForce(aimDir.normalized * 2.8f, ForceMode2D.Impulse);
            }

            // 3. Kích hoạt hoạt họa nhân vật & điểm va chạm
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayCombatAction(
                    EquippedWeapon.ActionType,
                    aimDir,
                    1.0f,
                    onHitFrame: () => PerformHitDetection(aimDir, damageMultiplier),
                    onComplete: OnAttackCycleCompleted,
                    weaponId: EquippedWeapon != null ? EquippedWeapon.Id : "dao_go"
                );
            }
            else
            {
                yield return new WaitForSeconds(0.06f);
                PerformHitDetection(aimDir, damageMultiplier);
                yield return new WaitForSeconds(0.12f);
                OnAttackCycleCompleted();
            }
        }

        private void OnAttackCycleCompleted()
        {
            // Khôi phục tốc độ di chuyển bình thường
            if (mobileController != null)
            {
                mobileController.SpeedMultiplier = 1.0f;
            }

            isAttacking = false;
            attackRoutine = null;

            // Xử lý hàng đợi lệnh Input Buffering
            if (bufferedAttack)
            {
                bufferedAttack = false;
                ExecuteAttack();
            }
        }
        #endregion

        #region Secondary Skill / Parry Mechanics
        /// <summary>
        /// Kích hoạt Kỹ năng Phụ: Đỡ đòn & Phản kích (Parry) / Nện đất (Slam) / Đâm liên hoàn (Flurry)
        /// </summary>
        public void ExecuteSecondarySkill()
        {
            if (BackpackPopup.InputBlockedThisFrame) return;
            if (Time.time < nextSecondaryTime) return;
            if (EquippedWeapon == null) { NoWeaponFeedback(); return; }

            secondsSinceLastCombatAction = 0f;
            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData != null && EquippedWeapon.SpecialSkill != WeaponSpecialSkill.Parry)
            {
                bool stillConscious = charData.TryConsumeCombatStamina(7.0f, 0.40f, out _);
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                if (!stillConscious)
                {
                    HandlePlayerFainted(charData.LastFaintReason);
                    return;
                }
            }

            nextSecondaryTime = Time.time + EquippedWeapon.SecondaryCooldown;

            var arch = CoreWeapon != null ? CoreWeapon.Archetype : WeaponArchetype.None;
            if (arch == WeaponArchetype.Thrust) { ExecuteHookPull(); return; }

            switch (EquippedWeapon.SpecialSkill)
            {
                case WeaponSpecialSkill.Slam:
                    ExecuteHammerSlam();
                    break;

                case WeaponSpecialSkill.Flurry:
                    ExecuteSpearFlurry();
                    break;

                case WeaponSpecialSkill.Parry:
                default:
                    ExecuteSwordParry();
                    break;
            }
        }

        /// <summary>
        /// Đỡ Đòn Chuẩn Stardew Valley 1.6 (Monster.handleParried): 
        /// Giữ thế thủ 0.4s. Khi bị tấn công: 100% chặn sát thương, phản lại damage / 2 và đẩy lùi quái x3.5.
        /// </summary>
        private void ExecuteSwordParry()
        {
            if (parryRoutine != null) StopCoroutine(parryRoutine);
            parryRoutine = StartCoroutine(ParryStanceRoutine());
            // the body takes the guard stance too (the parry used to be a VFX ring only: X looked dead, 3 Nov UI probe)
            spriteAnimator?.PlayChain(AnimationClips.Counter(CoreWeapon != null ? CoreWeapon.Archetype : WeaponArchetype.None));
        }

        private IEnumerator ParryStanceRoutine()
        {
            isParrying = true;

            // Hào quang thủ hộ lam ngọc
            if (SlashVFXController.Instance != null)
            {
                SlashVFXController.Instance.SpawnParryVFX(transform.position, false);
            }

            if (mobileController != null)
            {
                mobileController.SpeedMultiplier = 0.5f; // Chậm lại khi giữ thế thủ
            }

            yield return new WaitForSeconds(0.40f); // Thời gian mở khung đỡ đòn 400ms

            isParrying = false;
            parryRoutine = null;

            if (mobileController != null)
            {
                mobileController.SpeedMultiplier = 1.0f;
            }
        }

        /// <summary>
        /// Kỹ năng Nện Đất (Búa Tạ): Gây sóng chấn động AoE bán kính 2.5m hất văng mọi quái xung quanh
        /// </summary>
        private void ExecuteHammerSlam()
        {
            Vector2 playerPos = transform.position;

            // Rung màn hình uy lực
            if (CameraShake2D.Instance != null)
            {
                CameraShake2D.Instance.Shake(0.18f, 0.22f);
            }

            // VFX nện đất
            if (SlashVFXController.Instance != null)
            {
                SlashVFXController.Instance.SpawnVFX(CombatActionType.Smash, playerPos, Vector2.down, CharacterFacing.Down);
            }

            // Quét AoE bán kính 2.5m
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, 2.5f);
            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;

                if (IsFriendly(col)) continue;
                var dmg = col.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    Vector2 kDir = (dmg.Position - playerPos).normalized;
                    var c = MobileGameController.Instance?.Engine?.Character;
                    float hit = c != null ? MartialProgression.WeaponHitDamage(c, heavyStrike: true) : 20f;
                    dmg.TakeDamage(hit, kDir, 6.0f);
                    col.GetComponent<EnemyAgentView>()?.ApplySlow(0f, 0.3f); // Chấn: choáng 0,3 giây
                }
            }
        }

        /// <summary>
        /// Kỹ năng Đâm Liên Hoàn (Giáo Tre / Dao Găm): 4 cú đâm tốc biến gây dồn sát thương
        /// </summary>
        private void ExecuteSpearFlurry()
        {
            StartCoroutine(SpearFlurryRoutine());
        }

        private IEnumerator SpearFlurryRoutine()
        {
            Vector2 aimDir = mobileController != null ? mobileController.FacingDirection : Vector2.down;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.down;

            for (int i = 0; i < 4; i++)
            {
                // Micro-lunge cho mỗi cú đâm
                if (rb != null)
                {
                    rb.AddForce(aimDir.normalized * 2.0f, ForceMode2D.Impulse);
                }

                PerformHitDetection(aimDir, damageMultiplier: 0.55f);
                yield return new WaitForSeconds(0.08f);
            }
        }

        /// <summary>
        /// Kỹ năng Thế Công (Martial Skill): Lướt tới chém nhanh gây 1.5x sát thương
        /// </summary>
        public void ExecuteDashSlash()
        {
            if (BackpackPopup.InputBlockedThisFrame) return;
            if (EquippedWeapon == null) { NoWeaponFeedback(); return; }

            Vector2 aimDir = mobileController != null ? mobileController.FacingDirection : Vector2.down;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.down;

            IDamageable target = FindNearestTarget(4.0f);
            if (target != null)
            {
                aimDir = (target.Position - (Vector2)transform.position).normalized;
            }

            if (rb != null)
            {
                rb.AddForce(aimDir.normalized * 5.5f, ForceMode2D.Impulse);
            }

            PerformAttackCycle(aimDir, damageMultiplier: 1.5f);
        }
        #endregion

        #region Hit Detection, Crits & Projectile Deflection
        private void PerformHitDetection(Vector2 aimDir, float damageMultiplier)
        {
            Vector2 playerPos = transform.position;

            // 1. Sinh hiệu ứng VFX theo đúng hướng và tỉ lệ 64x64
            if (SlashVFXController.Instance != null && spriteAnimator != null)
            {
                SlashVFXController.Instance.SpawnVFX(
                    EquippedWeapon.ActionType,
                    playerPos,
                    aimDir,
                    spriteAnimator.CurrentFacing
                );
            }

            // 2. Cơ chế Phá / Chặn đạn bay (Melee Projectile Deflection)
            DeflectIncomingProjectiles(playerPos, aimDir);

            // 3–4. Sát thương theo GDD 8.2 + 12.3: gốc vũ khí × (1 + 1%/điểm chỉ số của vũ khí); chí mạng CTRL × 0,2%
            var engine = MobileGameController.Instance?.Engine;
            var chData = engine?.Character;
            float raw = chData != null ? MartialProgression.WeaponHitDamage(chData, heavyStrike: damageMultiplier > 1.2f) : 5f;
            bool forceCrit = pendingGuaranteedCrit;
            pendingGuaranteedCrit = false;
            bool isCrit = forceCrit || (chData != null && UnityEngine.Random.value < MartialProgression.CritChance(chData));
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(raw * damageMultiplier * (isCrit ? EquippedWeapon.CritMultiplier : 1f)));

            if (CoreWeapon != null && CoreWeapon.Archetype == WeaponArchetype.Ranged)
            {
                FireBolt(playerPos, aimDir, finalDamage, isCrit);
                return;
            }

            // 5. Quét va chạm Hitbox chuẩn xác theo nhân vật 64x64
            HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
            Collider2D[] colliders = Physics2D.OverlapCircleAll(playerPos, EquippedWeapon.AttackRange + 0.35f);

            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;
                if (IsFriendly(col)) continue;

                var damageable = col.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    Vector2 targetDir = damageable.Position - playerPos;
                    float dist = targetDir.magnitude;
                    bool isInHitbox = false;

                    switch (EquippedWeapon.ActionType)
                    {
                        case CombatActionType.Thrust:
                            float angleThrust = Vector2.Angle(aimDir, targetDir);
                            isInHitbox = (dist <= EquippedWeapon.AttackRange) && (angleThrust <= 35f);
                            break;

                        case CombatActionType.Smash:
                            Vector2 smashCenter = playerPos + (aimDir.normalized * (EquippedWeapon.AttackRange * 0.7f));
                            isInHitbox = Vector2.Distance(smashCenter, damageable.Position) <= 0.85f;
                            break;

                        case CombatActionType.Slash:
                        default:
                            float angleSlash = Vector2.Angle(aimDir, targetDir);
                            isInHitbox = (dist <= EquippedWeapon.AttackRange) && (angleSlash <= EquippedWeapon.ArcAngle * 0.5f);
                            break;
                    }

                    if (isInHitbox && hitTargets.Add(damageable))
                    {
                        float knockback = EquippedWeapon.KnockbackForce * (isCrit ? 1.4f : 1.0f);
                        damageable.TakeDamage(finalDamage, aimDir, knockback);
                        if (chData != null)
                        {
                            // Trọng Giáp (GDD 12.2): đòn nặng gây choáng ngắn; Phù Nộ Hỏa: +1 Nộ mỗi đòn trúng
                            if (damageMultiplier > 1.2f && chData.GetPlaystyle() == "TrọngGiáp")
                                col.GetComponent<EnemyAgentView>()?.ApplySlow(0f, MartialProgression.ControlSeconds(chData, 0.3f));
                            if (chData.EngravedRunes.Contains("PhuNoHoa")) chData.Rage = Mathf.Min(100, chData.Rage + 1);
                        }

                        // Phản hồi va chạm: Đòn chí mạng có tia lửa và rung màn hình
                        if (isCrit)
                        {
                            if (SlashVFXController.Instance != null)
                            {
                                SlashVFXController.Instance.SpawnCritSparkVFX(damageable.Position);
                            }
                            if (CameraShake2D.Instance != null)
                            {
                                CameraShake2D.Instance.Shake(0.08f, 0.12f);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Pigs, fences, structures and NPCs never take the player's swings.</summary>
        public static bool IsFriendly(Collider2D col) =>
            col.GetComponent<PigAgentView>() != null || col.GetComponent<Fence2DView>() != null ||
            col.GetComponent<DefenseTower2DView>() != null || col.GetComponent<PixelFenceSegment>() != null ||
            col.GetComponent<PlayerCombatController>() != null;

        /// <summary>Skills that grant brief invulnerability (Bộ Pháp Lướt 0,2 giây).</summary>
        public void GrantInvincibility(float seconds) { invincibilityTimer = Mathf.Max(invincibilityTimer, seconds); }
        /// <summary>Vô Ảnh Bộ: hits that are dodged for free.</summary>
        [System.NonSerialized] public int FreeDodges;
        public float SecondsSinceCombatAction => secondsSinceLastCombatAction;
        public void MarkCombatAction() { secondsSinceLastCombatAction = 0f; }

        bool dodging;

        /// <summary>
        /// Lướt né 3 m theo hướng di chuyển (ADR-0017 quãng 3 m, i-frame 0,25 giây). Giá do Core tính (9 STA, AGI ≥ 35,
        /// lối chơi, Guốc Gió). Nồm Ẩm: 10% trượt ngã 1 giây trừ khi mang Giày Đinh. Phù Truy Phong: để lại vệt chậm.
        /// </summary>
        public void ExecuteDodge()
        {
            if (dodging || BackpackPopup.InputBlockedThisFrame) return;
            var engine = MobileGameController.Instance?.Engine;
            var c = engine?.Character;
            if (c == null || c.IsFainted) return;
            float paid = MartialProgression.TryDodge(c, Time.time);
            if (paid < 0f) { PlayerInteractionController.Instance?.ShowFeedback("Không đủ Thể Lực để lướt."); return; }
            secondsSinceLastCombatAction = 0f;
            Vector2 dir = mobileController != null ? mobileController.FacingDirection : Vector2.down;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;
            bool slip = engine.CurrentWeather == WeatherType.NomAm && !MartialProgression.Wears(c, "MienTronTruot") && UnityEngine.Random.value < 0.10f;
            StartCoroutine(DodgeRoutine(MartialSkillRunner.Snap8(dir), slip, c.EngravedRunes.Contains("PhuTruyPhong"), c));
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
        }

        IEnumerator DodgeRoutine(Vector2 dir, bool slip, bool trail, CharacterData c)
        {
            dodging = true;
            var dodgeWeapon = MartialProgression.Weapon(c);
            spriteAnimator?.PlayChain(AnimationClips.Dodge(dodgeWeapon != null ? dodgeWeapon.Archetype : WeaponArchetype.None));
            GrantInvincibility(0.25f);
            if (mobileController != null) mobileController.SpeedMultiplier = 0f;
            Vector2 start = transform.position;
            float dist = 3f;
            foreach (var h in Physics2D.RaycastAll(start, dir, dist))
            {
                if (h.collider == null || h.collider.isTrigger || h.collider.gameObject == gameObject) continue;
                if (h.collider.GetComponent<EnemyAgentView>() != null || h.collider.GetComponent<PigAgentView>() != null) continue;
                dist = Mathf.Min(dist, Mathf.Max(0f, h.distance - 0.4f));
            }
            float t = 0f;
            while (t < dist)
            {
                t = Mathf.Min(dist, t + 22f * Time.deltaTime);
                Vector2 p = start + dir * t;
                if (rb != null) rb.position = p; else transform.position = p;
                if (trail)
                    foreach (var col in Physics2D.OverlapCircleAll(p, 1f))
                        col.GetComponent<EnemyAgentView>()?.ApplySlow(0.8f, MartialProgression.ControlSeconds(c, 3f));
                yield return null;
            }
            if (slip)
            {
                PlayerInteractionController.Instance?.ShowFeedback("Trơn Trượt! Ngã mất 1 giây (Nồm Ẩm).");
                yield return new WaitForSeconds(1f);
            }
            if (mobileController != null) mobileController.SpeedMultiplier = 1f;
            dodging = false;
        }

        // ------------------------------------------------------------------ đòn cơ bản / đòn đặc biệt / phản kích
        readonly List<float> counterPresses = new List<float>();
        EnemyAgentView incomingEnemy;
        MonsterMove incomingMove;
        float incomingStart = -99f, incomingImpact = -99f;
        int counterStreak;

        static bool RadialToolWheelOpen() => UnityEngine.Object.FindAnyObjectByType<RadialToolWheel>() is RadialToolWheel w && w.IsOpen;

        /// <summary>Bấm Q: ghi lại khoảnh khắc để chấm điểm căn nhịp khi đòn đặc biệt chạm.</summary>
        float nextNoWeaponToast;

        /// <summary>GDD 8.1: no innate attack, so with no weapon in hand the press is refused, but out loud: the touch
        /// action buttons used to do nothing at all (3 Nov UI probe: Space/X/context medallions DEAD).</summary>
        void NoWeaponFeedback()
        {
            if (Time.unscaledTime < nextNoWeaponToast) return;
            nextNoWeaponToast = Time.unscaledTime + 2f;
            PlayerInteractionController.Instance?.ShowFeedback("Tay không có vũ khí: lắp vũ khí ở ô Trang bị [C] hoặc chọn vũ khí trên vòng công cụ [Tab].");
        }

        float nextCounterHint;

        public void PressCounter()
        {
            // Nothing to counter: say what the button is for instead of a silent parry pose.
            if (incomingMove == null && Time.unscaledTime >= nextCounterHint)
            {
                nextCounterHint = Time.unscaledTime + 3f;
                PlayerInteractionController.Instance?.ShowFeedback("Phản kích [Q]: bấm đúng lúc quái vung đòn đặc biệt (đòn có vòng cảnh báo).");
            }
            counterPresses.Add(Time.time);
            if (counterPresses.Count > 8) counterPresses.RemoveAt(0);
            var cw = MobileGameController.Instance?.Engine?.Character is CharacterData cc ? MartialProgression.Weapon(cc) : null;
            spriteAnimator?.PlayChain(AnimationClips.Counter(cw != null ? cw.Archetype : WeaponArchetype.None));
            SlashVFXController.Instance?.SpawnParryVFX(transform.position, false);
        }

        /// <summary>A monster starts winding up a move at the player (called by EnemyAgentView).</summary>
        public void NotifyIncoming(EnemyAgentView enemy, MonsterMove move, float impactAt)
        {
            if (move == null || move.Kind != MonsterMoveKind.Special) return;
            incomingEnemy = enemy;
            incomingMove = move;
            incomingStart = Time.time;
            incomingImpact = impactAt;
        }

        /// <summary>
        /// Resolves a monster move on the player. Đòn cơ bản: lướt né (i-frame) tránh được, đỡ chủ động giảm hao.
        /// Đòn đặc biệt: không né được; chỉ phản kích được với tỉ lệ <see cref="CounterResolver.Chance"/> (gốc 5%).
        /// </summary>
        public void ResolveEnemyMove(EnemyAgentView enemy, MonsterMove move, float damage, Vector2 dir, float knock)
        {
            var c = MobileGameController.Instance?.Engine?.Character;
            if (c == null || move == null) { TakeDamage(damage, dir, knock); return; }
            if (move.Kind == MonsterMoveKind.Basic)
            {
                if (invincibilityTimer > 0f) { ShowCombatText("Né!", new Color(0.6f, 0.9f, 1f)); return; }
                pendingDrainRatio = move.StaminaDrainRatio;
                TakeDamage(damage, dir, knock);
                pendingDrainRatio = 0f;
                return;
            }

            // đòn đặc biệt
            float now = Time.time;
            float start = enemy == incomingEnemy ? incomingStart : now - move.TelegraphSeconds;
            bool pressedEarly = false, pressedAfter = false;
            int presses = 0;
            float best = 99f;
            foreach (var p in counterPresses)
            {
                if (p < start - 0.35f || p > now + 0.05f) continue;
                if (p < start) { pressedEarly = true; continue; }
                pressedAfter = true;
                presses++;
                best = Mathf.Min(best, Mathf.Abs(now - p));
            }
            counterPresses.Clear();
            incomingEnemy = null;
            float chance = pressedAfter && !pressedEarly
                ? CounterResolver.Chance(best, true, presses, counterStreak, now < c.CounterUntil)
                : CounterResolver.BaseChance;
            bool success = pressedAfter && UnityEngine.Random.value < chance;
            if (success)
            {
                counterStreak++;
                c.Rage = Mathf.Min(100, c.Rage + 1);
                float back = damage * 1.5f; // phản 150% như Phản Chấn (ADR-0024)
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(back, -dir, 5f);
                    enemy.ApplySlow(0f, MartialProgression.ControlSeconds(c, 0.5f));
                }
                SlashVFXController.Instance?.SpawnParryVFX(transform.position, true);
                CameraShake2D.Instance?.Shake(0.12f, 0.18f);
                PlayerInteractionController.Instance?.ShowFeedback($"PHẢN KÍCH {move.Name}! ({chance * 100f:0}%) · chuỗi {counterStreak}");
                CounterRhythmIndicator.ReportCounterResult(true, chance, counterStreak);
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                return;
            }
            counterStreak = 0;
            if (pressedAfter) PlayerInteractionController.Instance?.ShowFeedback($"Phản kích hụt ({chance * 100f:0}%) — {move.Name}");
            if (pressedAfter || pressedEarly) CounterRhythmIndicator.ReportCounterResult(false, chance, 0);
            else CounterRhythmIndicator.HideTelegraph(enemy != null ? enemy.transform : null);
            // không né được: bỏ qua i-frame và lượt né miễn phí
            float savedIframe = invincibilityTimer; int savedDodges = FreeDodges;
            invincibilityTimer = 0f; FreeDodges = 0;
            pendingDrainRatio = move.StaminaDrainRatio;
            TakeDamage(damage, dir, knock);
            pendingDrainRatio = 0f;
            FreeDodges = savedDodges;
            invincibilityTimer = Mathf.Max(invincibilityTimer, savedIframe);
        }

        float pendingDrainRatio;
        float nextWhistleCalmScan;
        double whistleCalmReadyAt;

        /// <summary>Tỉ lệ phản kích sẽ đạt nếu bấm Q đúng khung chạm (dùng cho chỉ báo nhịp); spam = đã bấm sớm hoặc bấm loạn.</summary>
        public float PreviewCounterChance(EnemyAgentView enemy, out bool spam, out int streak)
        {
            var c = MobileGameController.Instance?.Engine?.Character;
            streak = counterStreak;
            float start = enemy == incomingEnemy ? incomingStart : Time.time;
            int after = 0; bool early = false;
            foreach (var p in counterPresses)
            {
                if (p < start - 0.35f) continue;
                if (p < start) early = true; else after++;
            }
            spam = early || after > 1;
            if (spam) return CounterResolver.BaseChance;
            bool phanChan = c != null && Time.time < c.CounterUntil;
            return CounterResolver.Chance(0f, true, 1, counterStreak, phanChan);
        }

        void ShowCombatText(string text, Color color)
        {
            var go = new GameObject("CombatText");
            go.transform.position = transform.position + new Vector3(0f, 0.9f, 0f);
            var t = go.AddComponent<TextMeshPro>();
            t.text = text; t.fontSize = 4.2f; t.alignment = TextAlignmentOptions.Center; t.color = color; t.sortingOrder = 820;
            StartCoroutine(FloatingTextRoutine(go, t));
        }

        /// <summary>Thủ Chuyển Công (hợp kích): the next finishing hit is a sure crit.</summary>
        [System.NonSerialized] public bool pendingGuaranteedCrit;

        /// <summary>Nỏ Tay (GDD 12.3): one bolt along the aim, 12 m (ADR-0017); the hit target is marked 5 s for the towers.</summary>
        void FireBolt(Vector2 from, Vector2 dir, int damage, bool crit)
        {
            if (Arrows <= 0) { PlayerInteractionController.Instance?.ShowFeedback("Hết tên nỏ (mỗi sáng được nạp lại 40 tên)."); return; }
            Arrows--;
            dir = dir.normalized;
            EnemyAgentView best = null;
            float bestAlong = float.MaxValue;
            foreach (var col in Physics2D.OverlapCircleAll(from + dir * 6f, 6.2f))
            {
                var e = col.GetComponent<EnemyAgentView>();
                if (e == null || !e.IsAlive) continue;
                Vector2 to = (Vector2)e.transform.position - from;
                float along = Vector2.Dot(to, dir);
                if (along < 0f || along > 12f) continue;
                if ((to - dir * along).magnitude > 0.7f) continue;
                if (along < bestAlong) { bestAlong = along; best = e; }
            }
            var pool = CombatObjectPool.Instance;
            var proj = pool != null ? pool.SpawnProjectile() : null;
            if (proj != null) proj.Initialize(from + dir * 0.6f, dir, 0f, 18f, isPlayer: true);
            if (best == null) return;
            best.TakeDamage(damage, dir, EquippedWeapon.KnockbackForce);
            CombatMarks.Mark(best, 5f);
            if (crit && SlashVFXController.Instance != null) SlashVFXController.Instance.SpawnCritSparkVFX(best.transform.position);
        }

        /// <summary>Giáo Móc đòn nặng (GDD 12.3): hits the spear line and drags small monsters 4 m toward the player.</summary>
        void ExecuteHookPull()
        {
            Vector2 aimDir = mobileController != null ? mobileController.FacingDirection : Vector2.down;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.down;
            PerformAttackCycle(aimDir, damageMultiplier: 1.5f);
            Vector2 me = transform.position;
            foreach (var col in Physics2D.OverlapCircleAll(me, 5f))
            {
                var e = col.GetComponent<EnemyAgentView>();
                if (e == null || !e.IsAlive || !MartialSkillRunner.IsSmall(e)) continue;
                Vector2 to = (Vector2)e.transform.position - me;
                if (Vector2.Angle(aimDir, to) > 30f) continue;
                MartialSkillRunner.Drag(e, -to.normalized, Mathf.Min(4f, Mathf.Max(0f, to.magnitude - 0.8f)));
            }
        }

        /// <summary>
        /// Quét và phản đòn các tia đạn quái vật nằm trong tầm chém
        /// </summary>
        private void DeflectIncomingProjectiles(Vector2 playerPos, Vector2 aimDir)
        {
            var projectiles = FindObjectsByType<CombatProjectile2D>(FindObjectsSortMode.None);
            foreach (var proj in projectiles)
            {
                if (proj != null && !proj.IsFromPlayer)
                {
                    Vector2 toProj = (Vector2)proj.transform.position - playerPos;
                    float dist = toProj.magnitude;
                    float angle = Vector2.Angle(aimDir, toProj);

                    if (dist <= EquippedWeapon.AttackRange + 0.35f && angle <= 75f)
                    {
                        proj.Deflect(aimDir);
                        if (SlashVFXController.Instance != null)
                        {
                            SlashVFXController.Instance.SpawnCritSparkVFX(proj.transform.position);
                        }
                    }
                }
            }
        }

        private IDamageable FindNearestTarget(float maxDistance)
        {
            Vector2 playerPos = transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, maxDistance);
            float nearestEnemyDistSqr = float.MaxValue;
            float nearestDummyDistSqr = float.MaxValue;
            IDamageable bestEnemy = null;
            IDamageable bestDummy = null;

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;
                if (col.GetComponent<Fence2DView>() != null) continue; // Không tự động khóa hàng rào
                if (col.GetComponent<DefenseTower2DView>() != null) continue; // Không tự động khóa công trình

                var dmg = col.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    float dSqr = (dmg.Position - playerPos).sqrMagnitude;
                    if (col.GetComponent<EnemyAgentView>() != null)
                    {
                        if (dSqr < nearestEnemyDistSqr)
                        {
                            nearestEnemyDistSqr = dSqr;
                            bestEnemy = dmg;
                        }
                    }
                    else if (col.GetComponent<TrainingDummy2DView>() != null)
                    {
                        if (dSqr < nearestDummyDistSqr)
                        {
                            nearestDummyDistSqr = dSqr;
                            bestDummy = dmg;
                        }
                    }
                }
            }
            // Luôn ưu tiên khóa quái vật chiến đấu; nếu không có quái mới khóa bù nhìn thao trường để test chiêu
            return bestEnemy ?? bestDummy;
        }
        #endregion

        #region IDamageable Implementation & Invincibility Frames
        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            // 1. Kiểm tra Invincibility Frames (Miễn nhiễm sau khi nhận đòn)
            if (invincibilityTimer > 0f)
            {
                return;
            }

            var engine = MobileGameController.Instance?.Engine;
            var charData = engine?.Character;
            if (charData == null) return;

            secondsSinceLastCombatAction = 0f;
            charData.Now = Time.time;
            if (FreeDodges > 0) { FreeDodges--; PlayerInteractionController.Instance?.ShowFeedback($"Vô Ảnh Bộ né! (còn {FreeDodges})"); return; }

            // Phản Chấn: đỡ trong cửa sổ 0,3 giây → phản 150% vào quái gần nhất
            if (MartialProgression.TryCounter(charData, Time.time, amount, out float reflected))
            {
                foreach (var col in Physics2D.OverlapCircleAll(transform.position, 3f))
                {
                    var e = col.GetComponent<EnemyAgentView>();
                    if (e != null && e.IsAlive) { e.TakeDamage(reflected, ((Vector2)e.transform.position - (Vector2)transform.position).normalized, 4f); break; }
                }
                SlashVFXController.Instance?.SpawnParryVFX(transform.position, true);
                PlayerInteractionController.Instance?.ShowFeedback($"Phản Chấn! Phản {reflected:0} sát thương.");
                return;
            }
            // Hộ Thân: khiên hút sát thương trước
            amount = charData.AbsorbWithShield(amount);
            if (amount <= 0.01f) { SlashVFXController.Instance?.SpawnParryVFX(transform.position, false); return; }
            // Trụ Bộ / Giáp Lưới Thép / Trọng Giáp: không bị đẩy lùi
            if (MartialProgression.IsUnstoppable(charData, Time.time) || MartialProgression.Wears(charData, "KhongBiKeoDay") || charData.GetPlaystyle() == "TrọngGiáp")
                knockbackForce = 0f;

            // 2. Xử lý qua hệ thống Đỡ Chủ Động (Active Parry) hoặc Đỡ Mặc Định (Auto-Guard) + Thanh Chống Chịu & Thể Lực
            var hitRes = charData.ProcessIncomingAttack(amount, isParrying, isHoldingGuardStance, pendingDrainRatio);

            if (hitRes.WasActiveParry)
            {
                HandleParrySuccess(amount, knockbackDirection, hitRes);
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                if (hitRes.CausedFaint)
                {
                    HandlePlayerFainted(hitRes.FaintReason);
                }
                return;
            }

            // 3. Mặc định nhân vật luôn tự gồng vũ khí đỡ đòn (Auto-Guard):
            // Hao cả Thể Lực lẫn điểm Thanh Chống Chịu. Khi 1 trong 2 về 0 -> Ngất và kéo về trại hồi sức!
            if (SlashVFXController.Instance != null)
            {
                SlashVFXController.Instance.SpawnParryVFX(transform.position, false);
            }

            invincibilityTimer = hitRes.TriggeredAnBatKhuat ? 2.0f : 0.70f;
            if (invincibilityBlinkRoutine != null) StopCoroutine(invincibilityBlinkRoutine);
            invincibilityBlinkRoutine = StartCoroutine(InvincibilityBlinkRoutine(invincibilityTimer));

            // Bật lùi nhẹ khi đỡ đòn tự động
            if (rb != null && knockbackForce > 0f)
            {
                float guardKnockFactor = isHoldingGuardStance ? 0.35f : 0.60f;
                rb.AddForce(knockbackDirection.normalized * (knockbackForce * guardKnockFactor), ForceMode2D.Impulse);
            }

            SpawnAutoGuardPopup(hitRes);
            MobileGameController.Instance?.OnStateUpdated?.Invoke();

            // GDD 8: a hit drains guard; a heavy one (≥ 25% of the guard bar) staggers
            if (!hitRes.CausedFaint)
                spriteAnimator?.PlayChain(AnimationClips.Hit(hitRes.GuardDrained >= charData.MaxGuard * 0.25f));
            if (hitRes.CausedFaint)
            {
                HandlePlayerFainted(hitRes.FaintReason);
            }
        }

        /// <summary>
        /// Xử lý Phản đòn khi người chơi Đỡ Chủ Động (Active Parry) đúng nhịp
        /// </summary>
        private void HandleParrySuccess(float incomingDamage, Vector2 incomingDir, CombatHitResolution hitRes)
        {
            int counterDamage = Mathf.Max(1, Mathf.RoundToInt(hitRes.CounterDamage));

            // Hiệu ứng hào quang hoàng kim
            if (SlashVFXController.Instance != null)
            {
                SlashVFXController.Instance.SpawnParryVFX(transform.position, true);
            }

            if (CameraShake2D.Instance != null)
            {
                CameraShake2D.Instance.Shake(0.1f, 0.15f);
            }

            // Phản lực lại kẻ địch xung quanh
            Collider2D[] attackers = Physics2D.OverlapCircleAll(transform.position, 2.0f);
            foreach (var col in attackers)
            {
                if (col.gameObject == gameObject) continue;

                var dmg = col.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    Vector2 reflectDir = -incomingDir.normalized;
                    float counterKnockback = 3.5f * 2.5f; // Hệ số phản hồi x3.5
                    dmg.TakeDamage(counterDamage, reflectDir, counterKnockback);
                }
            }

            SpawnParryPopup(hitRes);
        }

        private IEnumerator InvincibilityBlinkRoutine(float duration)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr == null) yield break;

            float elapsed = 0f;
            float blinkInterval = 0.1f;
            bool isTransparent = false;

            while (elapsed < duration)
            {
                elapsed += blinkInterval;
                isTransparent = !isTransparent;
                sr.color = isTransparent ? new Color(0.55f, 0.85f, 1f, 0.55f) : Color.white;
                yield return new WaitForSeconds(blinkInterval);
            }

            sr.color = Color.white;
            invincibilityBlinkRoutine = null;
        }

        private void SpawnAutoGuardPopup(CombatHitResolution hitRes)
        {
            // MythHud is icon-first: guard/stamina drain shows on the HUD bars and the sprite blink, not as a text slab
            if (MythHudController.LegacyActionButtonsHidden) return;
            GameObject popupObj = new GameObject("AutoGuardPopup");
            popupObj.transform.position = transform.position + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0.85f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            if (hitRes.TriggeredAnBatKhuat)
            {
                textMesh.text = "ẤN BẤT KHUẤT! (BẤT TỬ 2S)";
                textMesh.color = new Color(1f, 0.85f, 0.2f, 1f);
            }
            else
            {
                textMesh.text = hitRes.StaminaDrained > 0f ? $"-{hitRes.GuardDrained:0} Chống Chịu | -{hitRes.StaminaDrained:0} Thể Lực" : $"-{hitRes.GuardDrained:0} Chống Chịu";
                textMesh.color = new Color(0.98f, 0.72f, 0.22f, 1f); // Vàng hổ phách cảnh báo hao Chống Chịu & Thể Lực
            }
            textMesh.fontSize = 4.2f;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 820;

            StartCoroutine(FloatingTextRoutine(popupObj, textMesh));
        }

        private void SpawnParryPopup(CombatHitResolution hitRes)
        {
            // MythHud is icon-first: guard/stamina drain shows on the HUD bars and the sprite blink, not as a text slab
            if (MythHudController.LegacyActionButtonsHidden) return;
            GameObject popupObj = new GameObject("ParryPopup");
            popupObj.transform.position = transform.position + new Vector3(0f, 0.90f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            string staNote = hitRes.StaminaDrained < 0f ? $"+{Mathf.Abs(hitRes.StaminaDrained):0} STA" : $"-{hitRes.StaminaDrained:0} STA";
            textMesh.text = $"ĐỠ CHỦ ĐỘNG! −{hitRes.GuardDrained:0.0} Chống Chịu ({staNote}), +1 Nộ";
            textMesh.fontSize = 4.6f;
            textMesh.color = new Color(0.25f, 0.95f, 1f, 1f); // Xanh lam ngọc
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 850;

            StartCoroutine(FloatingTextRoutine(popupObj, textMesh));
        }

        private IEnumerator FloatingTextRoutine(GameObject obj, TextMeshPro textMesh)
        {
            float duration = 0.75f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = textMesh.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                obj.transform.position = startPos + new Vector3(0f, progress * 0.65f, 0f);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        private void HandlePlayerFainted(string faintReason = "")
        {
            spriteAnimator?.PlayChain(AnimationClips.Faint());
            var engine = MobileGameController.Instance?.Engine;
            string rescueMessage = "Nhân vật đã ngất do vỡ chống chịu / cạn thể lực và được kéo về Hiên Nhà Chính hồi sức!";

            if (engine != null)
            {
                var rescue = engine.ExecutePlayerFaintRescue(faintReason);
                rescueMessage = rescue.rescueMessage;
            }

            // Đưa nhân vật về Trại Hồi Sức / Hiên Nhà Chính (Farmhouse Porch ở góc Tây Bắc [-16, 10] hoặc [0, 0] nếu ở Sandbox)
            bool isArena = CombatArenaController.Instance != null;
            transform.position = isArena ? Vector3.zero : new Vector3(-16f, 10f, 0f);
            if (rb != null) rb.linearVelocity = Vector2.zero;

            invincibilityTimer = 2.0f;
            MobileGameController.Instance?.OnStateUpdated?.Invoke();

            if (PlayerInteractionController.Instance != null)
            {
                PlayerInteractionController.Instance.ShowFeedback($"<color=#f87171>{rescueMessage}</color>");
            }
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            if (EquippedWeapon == null) return;

            Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f);
            Vector2 aim = mobileController != null ? mobileController.FacingDirection : Vector2.down;
            Gizmos.DrawWireSphere(transform.position, EquippedWeapon.AttackRange);
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(aim.normalized * EquippedWeapon.AttackRange));
        }
    }
}
