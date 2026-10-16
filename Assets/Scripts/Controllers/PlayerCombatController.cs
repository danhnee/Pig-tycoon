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
                return charData != null && !charData.IsFainted && charData.CurrentGuard > 0f && charData.Stamina.CurrentStamina > 0f;
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
            if (AttackButtonRoot != null) AttackButtonRoot.SetActive(isArmed);
            if (SecondaryButtonRoot != null) SecondaryButtonRoot.SetActive(isArmed);
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

                regenUiSyncTimer += Time.deltaTime;
                if (regenUiSyncTimer >= 0.25f &&
                    (Mathf.Abs(engine.Character.Stamina.CurrentStamina - prevSta) > 0.01f ||
                     Mathf.Abs(engine.Character.CurrentGuard - prevGuard) > 0.01f))
                {
                    regenUiSyncTimer = 0f;
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                }
            }

            if (isArmed)
            {
                // Phím bấm PC: Tấn công chính (Chuột Trái, Space, J, K, F)
                bool leftClick = Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
                if (leftClick || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.F))
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

            PerformAttackCycle(aimDir, damageMultiplier: 1.0f);
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
            if (Time.time < nextSecondaryTime) return;
            if (EquippedWeapon == null) return;

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

                PerformHitDetection(aimDir, damageMultiplier: 0.55f);
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
                        damageable.TakeDamage(finalDamage, aimDir, knockback);

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

            // 2. Xử lý qua hệ thống Đỡ Chủ Động (Active Parry) hoặc Đỡ Mặc Định (Auto-Guard) + Thanh Chống Chịu & Thể Lực
            var hitRes = charData.ProcessIncomingAttack(amount, isParrying, isHoldingGuardStance);

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
                string modeLabel = isHoldingGuardStance ? "THẾ THỦ ĐỠ ĐÒN" : "ĐỠ TỰ ĐỘNG";
                textMesh.text = $"{modeLabel}\n-{hitRes.StaminaDrained:0.0} Thể Lực | -{hitRes.GuardDrained:0.0} Chống Chịu";
                textMesh.color = new Color(0.98f, 0.72f, 0.22f, 1f); // Vàng hổ phách cảnh báo hao Chống Chịu & Thể Lực
            }
            textMesh.fontSize = 4.2f;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 820;

            StartCoroutine(FloatingTextRoutine(popupObj, textMesh));
        }

        private void SpawnParryPopup(CombatHitResolution hitRes)
        {
            GameObject popupObj = new GameObject("ParryPopup");
            popupObj.transform.position = transform.position + new Vector3(0f, 0.90f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            string staNote = hitRes.StaminaDrained < 0f ? $"+{Mathf.Abs(hitRes.StaminaDrained):0} STA" : $"-{hitRes.StaminaDrained:0} STA";
            textMesh.text = $"ĐỠ CHỦ ĐỘNG! (PHẢN {hitRes.CounterDamage:0} ST)\n-{hitRes.GuardDrained:0.0} Chống Chịu ({staNote})";
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
