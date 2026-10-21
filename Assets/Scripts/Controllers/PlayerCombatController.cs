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

        [Header("Dodge Roll / Dash UI")]
        public GameObject DodgeButtonRoot;
        public Button DodgeButton;
        public Image DodgeButtonIcon;
        public Image DodgeCooldownRadialFill;
        public TextMeshProUGUI DodgeCooldownText;

        private PlayerMobileController mobileController;
        private CharacterSpriteAnimator spriteAnimator;
        private Rigidbody2D rb;

        // Timers & Combat State
        private float nextAttackTime = 0f;
        private float nextSecondaryTime = 0f;
        private float nextDodgeTime = 0f;
        private float invincibilityTimer = 0f;
        private bool isArmed = false;
        private bool bufferedAttack = false;
        private bool isAttacking = false;
        private bool isParrying = false;
        private bool isDodging = false;
        private Coroutine attackRoutine;
        private Coroutine parryRoutine;
        private Coroutine dodgeRoutine;
        private Coroutine invincibilityBlinkRoutine;

        // Combo 3 nhịp (GDD 12 / KY_THUAT P1.08)
        private int currentComboStep = 0; // 0: Nhịp 1, 1: Nhịp 2, 2: Nhịp 3 (Finisher)
        private float comboWindowTimer = 0f; // Cửa sổ 0.65s để kích hoạt nhịp tiếp theo

        // Thể lực 2 lớp & Trạng thái Combat Tracker (GDD 9.4)
        private float lastCombatActionTime = -999f;
        private bool isInCombat = false;

        public bool IsArmed => isArmed;
        public bool IsAttacking => isAttacking;
        public bool IsParrying => isParrying;
        public bool IsDodging => isDodging;
        public int CurrentComboStep => currentComboStep;
        public bool IsInCombat => isInCombat;
        public WeaponDefinitionSO CurrentWeapon => EquippedWeapon;

        public bool IsAlive
        {
            get
            {
                var charData = MobileGameController.Instance?.Engine?.Character;
                return charData != null && charData.Hp > 0;
            }
        }

        public Vector2 Position => transform.position;
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

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

            if (DodgeButton != null)
            {
                DodgeButton.onClick.RemoveAllListeners();
                DodgeButton.onClick.AddListener(ExecuteDodgeRoll);
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

            // 3. Dodge Roll / Dash Button (Nút Lướt Né - đặt phía trên nút Tấn Công Chính)
            if (DodgeButtonRoot == null)
            {
                var existingDodge = canvas.transform.Find("Mobile_Dodge_Button");
                if (existingDodge != null)
                {
                    DodgeButtonRoot = existingDodge.gameObject;
                    DodgeButton = existingDodge.GetComponent<Button>();
                    DodgeButtonIcon = existingDodge.Find("Icon")?.GetComponent<Image>();
                    DodgeCooldownRadialFill = existingDodge.Find("CooldownFill")?.GetComponent<Image>();
                    DodgeCooldownText = existingDodge.Find("CooldownText")?.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    DodgeButtonRoot = new GameObject("Mobile_Dodge_Button");
                    DodgeButtonRoot.transform.SetParent(canvas.transform, false);

                    var rt = DodgeButtonRoot.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(1f, 0f);
                    rt.anchoredPosition = new Vector2(-28f, 218f); // Đặt ngay phía trên nút đánh chính
                    rt.sizeDelta = new Vector2(68f, 68f);

                    var bgImg = DodgeButtonRoot.AddComponent<Image>();
                    bgImg.sprite = UISpriteLoader.GetFrameActionButton();
                    bgImg.type = Image.Type.Sliced;
                    bgImg.color = new Color(0.2f, 0.75f, 0.45f, 1f); // Xanh lục ngọc lướt gió

                    DodgeButton = DodgeButtonRoot.AddComponent<Button>();

                    // Icon Lướt né
                    var iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(DodgeButtonRoot.transform, false);
                    var iconRt = iconObj.AddComponent<RectTransform>();
                    iconRt.anchorMin = Vector2.zero;
                    iconRt.anchorMax = Vector2.one;
                    iconRt.sizeDelta = new Vector2(-16f, -16f);
                    DodgeButtonIcon = iconObj.AddComponent<Image>();
                    DodgeButtonIcon.sprite = UISpriteLoader.GetIconEnergyBolt() ?? UISpriteLoader.GetFrameSlotHighlight();
                    DodgeButtonIcon.preserveAspect = true;
                    DodgeButtonIcon.raycastTarget = false;

                    // Cooldown Radial Fill
                    var cdObj = new GameObject("CooldownFill");
                    cdObj.transform.SetParent(DodgeButtonRoot.transform, false);
                    var cdRt = cdObj.AddComponent<RectTransform>();
                    cdRt.anchorMin = Vector2.zero;
                    cdRt.anchorMax = Vector2.one;
                    cdRt.sizeDelta = Vector2.zero;
                    DodgeCooldownRadialFill = cdObj.AddComponent<Image>();
                    DodgeCooldownRadialFill.sprite = UISpriteLoader.GetFrameActionButton();
                    DodgeCooldownRadialFill.type = Image.Type.Filled;
                    DodgeCooldownRadialFill.fillMethod = Image.FillMethod.Radial360;
                    DodgeCooldownRadialFill.fillOrigin = (int)Image.Origin360.Top;
                    DodgeCooldownRadialFill.fillClockwise = false;
                    DodgeCooldownRadialFill.color = new Color(0f, 0f, 0f, 0.65f);
                    DodgeCooldownRadialFill.raycastTarget = false;
                    DodgeCooldownRadialFill.fillAmount = 0f;

                    // Cooldown Text
                    var txtObj = new GameObject("CooldownText");
                    txtObj.transform.SetParent(DodgeButtonRoot.transform, false);
                    var txtRt = txtObj.AddComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;
                    DodgeCooldownText = txtObj.AddComponent<TextMeshProUGUI>();
                    DodgeCooldownText.fontSize = 18f;
                    DodgeCooldownText.fontStyle = FontStyles.Bold;
                    DodgeCooldownText.alignment = TextAlignmentOptions.Center;
                    DodgeCooldownText.color = Color.white;
                    DodgeCooldownText.raycastTarget = false;
                    DodgeCooldownText.gameObject.SetActive(false);
                }
            }

            UpdateUIVisibility();
        }

        private void UpdateUIVisibility()
        {
            if (AttackButtonRoot != null) AttackButtonRoot.SetActive(isArmed);
            if (SecondaryButtonRoot != null) SecondaryButtonRoot.SetActive(isArmed);
            if (DodgeButtonRoot != null) DodgeButtonRoot.SetActive(isArmed);
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

        private void Update()
        {
            // Đếm ngược i-Frames của người chơi
            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.deltaTime;
            }

            // Đếm ngược cửa sổ Combo 3 nhịp: Quá 0.65s sau đòn đánh -> reset về Nhịp 1
            if (currentComboStep > 0 && !isAttacking && Time.time > comboWindowTimer)
            {
                currentComboStep = 0;
            }

            // Hồi phục Thể lực tự nhiên theo GDD 9.4
            HandleStaminaRegeneration();

            if (PlayerInteractionController.IsAnyModalUIOpen())
            {
                UpdateCooldownUI();
                return;
            }

            if (isArmed)
            {
                // Phím bấm PC: Lướt Né (Space, LeftShift, C) - Độc lập hoàn toàn khỏi phím đánh
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.C))
                {
                    ExecuteDodgeRoll();
                }

                // Phím bấm PC: Tấn công chính (Chuột Trái, J, K, F)
                bool leftClick = Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
                if (leftClick || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.F))
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

                // Phím bấm PC: Kỹ năng phụ / Đỡ đòn (Chuột Phải, Phím U, Phím L)
                bool rightClick = Input.GetMouseButtonDown(1) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
                if (rightClick || Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.L))
                {
                    ExecuteSecondarySkill();
                }
            }

            UpdateCooldownUI();
        }

        /// <summary>
        /// Hồi phục Thể lực tự nhiên theo GDD 9.4:
        /// - Đang combat: Sau 1.5s không hành động hồi 2.0/s
        /// - Ngoài combat >= 3s: Hồi 10.0/s
        /// - Chỉ hồi đến trần EffectiveMaxStamina (Tải ngày không tự hồi)
        /// </summary>
        private void HandleStaminaRegeneration()
        {
            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData == null || charData.Stamina == null) return;

            float timeSinceCombat = Time.time - lastCombatActionTime;
            bool nearbyEnemies = HasEnemiesInRange(8.0f);
            isInCombat = nearbyEnemies || (timeSinceCombat < 3.0f);

            if (isInCombat)
            {
                if (timeSinceCombat >= 1.5f)
                {
                    charData.Stamina.RegenerateStamina(2.0f * Time.deltaTime);
                }
            }
            else
            {
                charData.Stamina.RegenerateStamina(10.0f * Time.deltaTime);
            }
        }

        private bool HasEnemiesInRange(float range)
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, range);
            foreach (var c in cols)
            {
                if (c.gameObject == gameObject) continue;
                var enemy = c.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive) return true;
            }
            return false;
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

            // 3. Dodge Roll Cooldown (0.65s chuẩn)
            float dodgeRemaining = nextDodgeTime - Time.time;
            if (dodgeRemaining > 0f)
            {
                if (DodgeCooldownRadialFill != null)
                {
                    DodgeCooldownRadialFill.fillAmount = dodgeRemaining / 0.65f;
                }
                if (DodgeCooldownText != null)
                {
                    DodgeCooldownText.text = dodgeRemaining.ToString("F1");
                    DodgeCooldownText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (DodgeCooldownRadialFill != null) DodgeCooldownRadialFill.fillAmount = 0f;
                if (DodgeCooldownText != null) DodgeCooldownText.gameObject.SetActive(false);
            }
        }

        #region Primary Attack & 3-Hit Combo Execution (GDD 12 & P1.08)
        /// <summary>
        /// Kích hoạt đòn tấn công với Combo 3 nhịp (Light -> Follow-up -> Heavy Finisher)
        /// Smart Auto-Targeting & Input Buffering & Tiêu hao Thể lực 2 lớp
        /// </summary>
        public void ExecuteAttack()
        {
            if (isAttacking)
            {
                bufferedAttack = true; // Lưu cú nhấn tiếp theo để combo mượt mà
                return;
            }
            if (Time.time < nextAttackTime) return;
            if (EquippedWeapon == null) return;

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

            TriggerComboAttack(aimDir);
        }

        /// <summary>
        /// Tấn công chỉ định hướng theo vị trí click chuột
        /// </summary>
        public void ExecuteAttackTowards(Vector2 targetWorldPos)
        {
            if (isAttacking)
            {
                bufferedAttack = true;
                return;
            }
            if (Time.time < nextAttackTime) return;
            if (EquippedWeapon == null) return;

            Vector2 toTarget = targetWorldPos - (Vector2)transform.position;
            if (toTarget.sqrMagnitude < 0.01f) toTarget = Vector2.down;
            Vector2 aimDir = toTarget.normalized;

            if (mobileController != null)
            {
                mobileController.SetFacingDirection(aimDir);
            }

            TriggerComboAttack(aimDir);
        }

        private void TriggerComboAttack(Vector2 aimDir)
        {
            var charData = MobileGameController.Instance?.Engine?.Character;

            // Kiểm tra cửa sổ combo (0.65s): Quá thời gian thì reset về Nhịp 1
            if (currentComboStep > 0 && Time.time > comboWindowTimer)
            {
                currentComboStep = 0;
            }

            var (staminaCost, dailyRatio, dmgMultiplier) = charData != null
                ? charData.GetComboStepData(currentComboStep)
                : (2.5f, 0.30f, 1.0f);

            // Kiểm tra Thể Lực tức thời
            if (charData != null && !charData.Stamina.CanAfford(staminaCost))
            {
                if (PlayerInteractionController.Instance != null)
                {
                    PlayerInteractionController.Instance.ShowFeedback("Hụt hơi! Không đủ thể lực để ra đòn!");
                }
                return;
            }

            charData?.Stamina.TryConsumeStamina(staminaCost, dailyRatio);
            lastCombatActionTime = Time.time;

            int executingStep = currentComboStep;

            // Cooldown ra đòn theo nhịp combo
            float baseCd = EquippedWeapon.Cooldown;
            if (executingStep == 1)
            {
                baseCd *= 0.85f; // Nhịp 2 ra đòn nhanh hơn 15%
            }
            // GDD 9.4: Vùng mệt (< 20%): đòn nặng / ra đòn chậm thêm 20%
            if (charData != null && charData.Stamina.IsFatigued)
            {
                baseCd *= 1.20f;
            }

            nextAttackTime = Time.time + baseCd;

            // Chuyển sang bước tiếp theo của combo 3 nhịp
            currentComboStep = (currentComboStep + 1) % 3;

            PerformAttackCycle(aimDir, dmgMultiplier, executingStep);
        }

        private void PerformAttackCycle(Vector2 aimDir, float damageMultiplier, int comboStep = 0)
        {
            isAttacking = true;

            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = StartCoroutine(AttackCycleRoutine(aimDir, damageMultiplier, comboStep));
        }

        private IEnumerator AttackCycleRoutine(Vector2 aimDir, float damageMultiplier, int comboStep)
        {
            // 1. Attack Commit: Hãm tốc độ di chuyển còn 25% trong lúc vung đòn (chống lướt trượt băng)
            if (mobileController != null)
            {
                mobileController.SpeedMultiplier = 0.25f;
            }

            // 2. Micro-Lunge: Nhích nhẹ theo hướng đánh (0.18 - 0.22m), Nhịp 3 nhích mạnh hơn (0.28m)
            float lungeForce = (comboStep == 2) ? 3.8f : 2.8f;
            if (rb != null)
            {
                rb.AddForce(aimDir.normalized * lungeForce, ForceMode2D.Impulse);
            }

            // Tốc độ hoạt họa theo nhịp combo & vùng mệt
            float animSpeed = 1.0f;
            if (comboStep == 1) animSpeed = 1.15f;
            else if (comboStep == 2) animSpeed = 0.95f;

            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData != null && charData.Stamina.IsFatigued)
            {
                animSpeed *= 0.80f;
            }

            // 3. Kích hoạt hoạt họa nhân vật & điểm va chạm
            if (spriteAnimator != null)
            {
                spriteAnimator.PlayCombatAction(
                    EquippedWeapon.ActionType,
                    aimDir,
                    animSpeed,
                    onHitFrame: () => PerformHitDetection(aimDir, damageMultiplier, comboStep),
                    onComplete: OnAttackCycleCompleted,
                    weaponId: EquippedWeapon != null ? EquippedWeapon.Id : "dao_go"
                );
            }
            else
            {
                yield return new WaitForSeconds(0.06f / animSpeed);
                PerformHitDetection(aimDir, damageMultiplier, comboStep);
                yield return new WaitForSeconds(0.12f / animSpeed);
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

            // Đặt cửa sổ kích hoạt nhịp combo tiếp theo (0.65s)
            comboWindowTimer = Time.time + 0.65f;

            // Xử lý hàng đợi lệnh Input Buffering
            if (bufferedAttack)
            {
                bufferedAttack = false;
                ExecuteAttack();
            }
        }
        #endregion

        #region Dodge Roll / Dash Mechanics (GDD 9.2 & 12.7)
        /// <summary>
        /// Kích hoạt Kỹ năng Lướt Né (Dodge Roll / Dash) có i-Frames:
        /// - Tiêu hao: 9.0 STA (30% Tải ngày; giảm 15% nếu AGI >= 35, giảm 20% nếu Khinh Thân, tăng 25% nếu Trọng Giáp).
        /// - i-Frames: 0.25s miễn nhiễm sát thương tuyệt đối.
        /// - Thời lượng: 0.22s, vận tốc lướt 16m/s (~4.2m).
        /// - Cooldown: 0.65s.
        /// </summary>
        public void ExecuteDodgeRoll()
        {
            if (isDodging) return;
            if (Time.time < nextDodgeTime) return;

            var charData = MobileGameController.Instance?.Engine?.Character;
            float dodgeCost = charData != null ? charData.CalculateDodgeCost() : 9.0f;

            if (charData != null && !charData.Stamina.CanAfford(dodgeCost))
            {
                if (PlayerInteractionController.Instance != null)
                {
                    PlayerInteractionController.Instance.ShowFeedback("Không đủ Thể Lực để lướt né!");
                }
                return;
            }

            charData?.Stamina.TryConsumeStamina(dodgeCost, dailyLoadRatio: 0.30f);
            lastCombatActionTime = Time.time;
            nextDodgeTime = Time.time + 0.65f;

            // Xác định hướng lướt né
            Vector2 dodgeDir = Vector2.zero;
            if (mobileController != null && mobileController.MoveInput.sqrMagnitude > 0.05f)
            {
                dodgeDir = mobileController.MoveInput.normalized;
            }
            else if (mobileController != null)
            {
                dodgeDir = mobileController.FacingDirection.normalized;
            }

            if (dodgeDir.sqrMagnitude < 0.01f) dodgeDir = Vector2.down;

            if (dodgeRoutine != null) StopCoroutine(dodgeRoutine);
            dodgeRoutine = StartCoroutine(DodgeRollRoutine(dodgeDir));
        }

        private IEnumerator DodgeRollRoutine(Vector2 dodgeDir)
        {
            isDodging = true;

            // 1. Kích hoạt 0.25s i-Frames (Bất tử hoàn toàn trong cú lướt)
            invincibilityTimer = 0.25f;

            // 2. Tạm khóa điều khiển thông thường để Coroutine gia tốc
            if (mobileController != null)
            {
                mobileController.CanMove = false;
            }

            // 3. VFX bụi lướt gió
            if (SlashVFXController.Instance != null)
            {
                SlashVFXController.Instance.SpawnDodgeDustVFX(transform.position, dodgeDir);
            }

            // 4. Lướt nhích nhanh trong 0.22s (~4.2m)
            float dodgeDuration = 0.22f;
            float elapsed = 0f;
            float dodgeSpeed = 16.0f;

            var sr = GetComponentInChildren<SpriteRenderer>();
            Color originalColor = sr != null ? sr.color : Color.white;
            if (sr != null)
            {
                sr.color = new Color(0.7f, 0.9f, 1f, 0.75f); // Hiệu ứng bóng mờ lướt gió
            }

            while (elapsed < dodgeDuration)
            {
                elapsed += Time.deltaTime;
                if (rb != null)
                {
                    rb.linearVelocity = dodgeDir * dodgeSpeed;
                }
                yield return null;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (sr != null)
            {
                sr.color = originalColor;
            }

            if (mobileController != null)
            {
                mobileController.CanMove = true;
            }

            isDodging = false;
            dodgeRoutine = null;
        }
        #endregion

        #region Secondary Skill / Parry Mechanics
        /// <summary>
        /// Kích hoạt Kỹ năng Phụ: Đỡ đòn & Phản kích (Parry) / Nện đất (Slam) / Đâm liên hoàn (Flurry)
        /// Tiêu hao Thể lực theo GDD 9.2 (Parry: 5 STA, Slam: 8 STA, Flurry: 10 STA)
        /// </summary>
        public void ExecuteSecondarySkill()
        {
            if (Time.time < nextSecondaryTime) return;
            if (EquippedWeapon == null) return;

            float staminaCost = 5.0f;
            switch (EquippedWeapon.SpecialSkill)
            {
                case WeaponSpecialSkill.Slam:
                    staminaCost = 8.0f;
                    break;
                case WeaponSpecialSkill.Flurry:
                    staminaCost = 10.0f;
                    break;
                case WeaponSpecialSkill.Parry:
                default:
                    staminaCost = 5.0f;
                    break;
            }

            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData != null && !charData.Stamina.CanAfford(staminaCost))
            {
                if (PlayerInteractionController.Instance != null)
                {
                    PlayerInteractionController.Instance.ShowFeedback("Không đủ Thể Lực để dùng Kỹ năng Phụ!");
                }
                return;
            }

            charData?.Stamina.TryConsumeStamina(staminaCost, dailyLoadRatio: 0.30f);
            lastCombatActionTime = Time.time;

            nextSecondaryTime = Time.time + EquippedWeapon.SecondaryCooldown;

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

                var dmg = col.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    Vector2 kDir = (dmg.Position - playerPos).normalized;
                    dmg.TakeDamage(EquippedWeapon.BaseDamage * 1.5f, kDir, 6.0f);
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

                PerformHitDetection(aimDir, damageMultiplier: 0.55f, comboStep: 0);
                yield return new WaitForSeconds(0.08f);
            }
        }

        /// <summary>
        /// Kỹ năng Thế Công (Martial Skill): Lướt tới chém nhanh gây 1.5x sát thương
        /// </summary>
        public void ExecuteDashSlash()
        {
            if (EquippedWeapon == null) return;

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

            PerformAttackCycle(aimDir, damageMultiplier: 1.5f, comboStep: 0);
        }
        #endregion

        #region Hit Detection, Crits & Projectile Deflection
        private void PerformHitDetection(Vector2 aimDir, float damageMultiplier, int comboStep = 0)
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

            // 3. Lấy chỉ số nhân vật (Str & Luck)
            int charStr = 10;
            int luckLevel = 0;
            var engine = MobileGameController.Instance?.Engine;
            if (engine?.Character?.BaseStats != null)
            {
                int bonusStr = engine.Character.BonusStats != null ? engine.Character.BonusStats.Str : 0;
                int bonusLuk = engine.Character.BonusStats != null ? engine.Character.BonusStats.Luk : 0;
                charStr = engine.Character.BaseStats.Str + bonusStr;
                luckLevel = engine.Character.BaseStats.Luk + bonusLuk;
            }

            // 4. Tính toán Sát thương & Tỉ lệ Chí mạng (Stardew Valley 1.6 Formula)
            float critChance = CombatFormulas.CalculateCriticalChance(
                EquippedWeapon.CritChance,
                luckLevel: luckLevel,
                isScout: false,
                isDagger: EquippedWeapon.ActionType == CombatActionType.Thrust
            );

            bool isCrit = UnityEngine.Random.value < critChance;

            // Roll sát thương giữa MinDamage và MaxDamage
            int baseRoll = UnityEngine.Random.Range(EquippedWeapon.MinDamage, EquippedWeapon.MaxDamage + 1);

            int finalDamage = CombatFormulas.CalculateStardewDamage(
                EquippedWeapon.MinDamage,
                EquippedWeapon.MaxDamage,
                attackerAttack: Mathf.RoundToInt(charStr / 3f),
                professionMultiplier: damageMultiplier,
                defenderResilience: 0,
                isCrit: isCrit,
                critMultiplier: EquippedWeapon.CritMultiplier,
                randomRoll: baseRoll
            );

            // 5. Quét va chạm Hitbox chuẩn xác theo nhân vật 64x64
            HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
            Collider2D[] colliders = Physics2D.OverlapCircleAll(playerPos, EquippedWeapon.AttackRange + 0.35f);

            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;

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
                        if (comboStep == 2)
                        {
                            knockback *= 1.6f; // GDD 12: Nhịp 3 Finisher knockback x1.6
                        }

                        damageable.TakeDamage(finalDamage, aimDir, knockback);

                        // Phản hồi va chạm: Đòn chí mạng hoặc Finisher có hiệu ứng ấn tượng
                        if (isCrit || comboStep == 2)
                        {
                            if (SlashVFXController.Instance != null)
                            {
                                SlashVFXController.Instance.SpawnCritSparkVFX(damageable.Position);
                            }
                            if (CameraShake2D.Instance != null)
                            {
                                float intensity = (comboStep == 2) ? 0.14f : 0.08f;
                                CameraShake2D.Instance.Shake(intensity, intensity + 0.05f);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Quét và phản đòn các tia đạn quái vật nằm trong tầm chém
        /// </summary>
        private void DeflectIncomingProjectiles(Vector2 playerPos, Vector2 aimDir)
        {
            var projectiles = FindObjectsByType<CombatProjectile2D>();
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
            // 1. Kiểm tra Invincibility Frames (Miễn nhiễm 1.0s sau khi dính đòn)
            if (invincibilityTimer > 0f)
            {
                return;
            }

            // 2. Kiểm tra Đỡ đòn & Phản kích (Parry Stance)
            if (isParrying)
            {
                HandleParrySuccess(amount, knockbackDirection);
                return;
            }

            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData == null) return;

            lastCombatActionTime = Time.time;

            int res = charData.BaseStats.Res + charData.BonusStats.Res;
            float mitigated = CombatFormulas.CalculateDamage(amount, attackerStr: 10, defenderRes: res);

            charData.Hp = Mathf.Max(0, charData.Hp - (int)mitigated);

            // Kích hoạt Nộ Khí khi nhận sát thương
            int rageGain = CombatFormulas.CalculateRageGainOnDamage(mitigated);
            charData.Rage = Mathf.Min(100, charData.Rage + rageGain);

            // Kích hoạt 1.0s i-Frames
            invincibilityTimer = 1.0f;
            if (invincibilityBlinkRoutine != null) StopCoroutine(invincibilityBlinkRoutine);
            invincibilityBlinkRoutine = StartCoroutine(InvincibilityBlinkRoutine(1.0f));

            // Bật lùi (Knockback)
            if (rb != null && knockbackForce > 0f)
            {
                rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
            }

            // Popup sát thương
            SpawnPlayerDamagePopup(mitigated);

            MobileGameController.Instance?.OnStateUpdated?.Invoke();

            if (charData.Hp <= 0)
            {
                HandlePlayerFainted();
            }
        }

        /// <summary>
        /// Xử lý Phản đòn khi người chơi đỡ thành công trong tư thế Parry
        /// </summary>
        private void HandleParrySuccess(float incomingDamage, Vector2 incomingDir)
        {
            lastCombatActionTime = Time.time;

            // 100% Chặn sát thương người chơi!
            // Phản sát thương = incomingDamage / 2 (Stardew Monster.handleParried)
            int counterDamage = CombatFormulas.CalculateParryCounterDamage((int)incomingDamage);

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
            Collider2D[] attackers = Physics2D.OverlapCircleAll(transform.position, 1.8f);
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

            // Hiển thị chữ nổi ĐỠ ĐÒN!
            SpawnParryPopup();
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
                sr.color = isTransparent ? new Color(1f, 0.4f, 0.4f, 0.45f) : Color.white;
                yield return new WaitForSeconds(blinkInterval);
            }

            sr.color = Color.white;
            invincibilityBlinkRoutine = null;
        }

        private void SpawnPlayerDamagePopup(float damage)
        {
            GameObject popupObj = new GameObject("PlayerDamagePopup");
            popupObj.transform.position = transform.position + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0.8f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            textMesh.text = $"-{Mathf.RoundToInt(damage)} HP";
            textMesh.fontSize = 5f;
            textMesh.color = new Color(1f, 0.25f, 0.25f, 1f);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 800;
            textMesh.rectTransform.sizeDelta = new Vector2(2.0f, 0.8f);

            StartCoroutine(FloatingTextRoutine(popupObj, textMesh));
        }

        private void SpawnParryPopup()
        {
            GameObject popupObj = new GameObject("ParryPopup");
            popupObj.transform.position = transform.position + new Vector3(0f, 0.85f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            textMesh.text = "ĐỠ ĐÒN! (PARRY)";
            textMesh.fontSize = 5.2f;
            textMesh.color = new Color(0.25f, 0.9f, 1f, 1f); // Xanh lam ngọc
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 850;
            textMesh.rectTransform.sizeDelta = new Vector2(3.0f, 0.8f);

            StartCoroutine(FloatingTextRoutine(popupObj, textMesh));
        }

        private IEnumerator FloatingTextRoutine(GameObject obj, TextMeshPro textMesh)
        {
            float duration = 0.6f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Color startColor = textMesh.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                obj.transform.position = startPos + new Vector3(0f, progress * 0.6f, 0f);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }

        private void HandlePlayerFainted()
        {
            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData != null)
            {
                charData.Hp = 50; // Hồi phục 50% HP
            }

            transform.position = Vector3.zero;

            if (PlayerInteractionController.Instance != null)
            {
                PlayerInteractionController.Instance.ShowFeedback("Khoa đã kiệt sức sau giao tranh! Được hồi sức tại hiên nhà.");
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
