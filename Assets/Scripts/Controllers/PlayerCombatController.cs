using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(PlayerMobileController))]
    public class PlayerCombatController : MonoBehaviour
    {
        public static PlayerCombatController Instance { get; private set; }

        [Header("Weapon Configuration")]
        public WeaponDefinitionSO EquippedWeapon;

        [Header("UI & Controls")]
        public GameObject AttackButtonRoot;
        public Button AttackButton;
        public Image AttackButtonIcon;
        public Image CooldownRadialFill;
        public TextMeshProUGUI CooldownText;

        private PlayerMobileController mobileController;
        private CharacterSpriteAnimator spriteAnimator;
        private float nextAttackTime = 0f;
        private bool isArmed = false;

        public bool IsArmed => isArmed;
        public WeaponDefinitionSO CurrentWeapon => EquippedWeapon;

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

            // Khởi tạo vũ khí mặc định nếu chưa có
            if (EquippedWeapon == null)
            {
                EquippedWeapon = ScriptableObject.CreateInstance<WeaponDefinitionSO>();
                EquippedWeapon.Id = "dao_go";
                EquippedWeapon.DisplayName = "Đao Gỗ";
                EquippedWeapon.ActionType = CombatActionType.Slash;
                EquippedWeapon.BaseDamage = 15f;
                EquippedWeapon.AttackRange = 1.35f;
                EquippedWeapon.ArcAngle = 120f;
                EquippedWeapon.Cooldown = 0.35f;
                EquippedWeapon.KnockbackForce = 2.5f;
            }

            if (AttackButton != null)
            {
                AttackButton.onClick.AddListener(ExecuteAttack);
            }
        }

        private void Start()
        {
            if (HotbarController.Instance != null)
            {
                HotbarController.Instance.OnToolChanged += OnHotbarToolChanged;
                OnHotbarToolChanged(HotbarController.Instance.CurrentTool);
            }
        }

        private void OnDestroy()
        {
            if (HotbarController.Instance != null)
            {
                HotbarController.Instance.OnToolChanged -= OnHotbarToolChanged;
            }
        }

        private void OnHotbarToolChanged(StardewToolType tool)
        {
            isArmed = (tool == StardewToolType.DaoGo);

            if (AttackButtonRoot != null)
            {
                AttackButtonRoot.SetActive(isArmed);
            }
        }

        public void SwitchWeapon(WeaponDefinitionSO newWeapon)
        {
            if (newWeapon == null) return;
            EquippedWeapon = newWeapon;
        }

        private void Update()
        {
            // Bàn phím: Phím J, K hoặc F để tấn công
            if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.F))
            {
                ExecuteAttack();
            }

            // Cooldown UI Update
            UpdateCooldownUI();
        }

        private void UpdateCooldownUI()
        {
            if (EquippedWeapon == null) return;

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
        }

        public void ExecuteAttack()
        {
            if (Time.time < nextAttackTime) return;
            if (spriteAnimator != null && spriteAnimator.IsAttacking) return;
            if (EquippedWeapon == null) return;

            Vector2 aimDir = mobileController != null ? mobileController.FacingDirection : Vector2.down;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.down;

            nextAttackTime = Time.time + EquippedWeapon.Cooldown;

            if (spriteAnimator != null)
            {
                spriteAnimator.PlayCombatAction(
                    EquippedWeapon.ActionType,
                    aimDir,
                    1.0f,
                    onHitFrame: () => PerformHitDetection(aimDir),
                    onComplete: null
                );
            }
            else
            {
                PerformHitDetection(aimDir);
            }
        }

        private void PerformHitDetection(Vector2 aimDir)
        {
            Vector2 playerPos = transform.position;

            // 1. Sinh hiệu ứng VFX vệt chém / bổ / đâm
            if (SlashVFXController.Instance != null && spriteAnimator != null)
            {
                SlashVFXController.Instance.SpawnVFX(
                    EquippedWeapon.ActionType,
                    playerPos,
                    aimDir,
                    spriteAnimator.CurrentFacing
                );
            }

            // 2. Quét va chạm Hitbox chuẩn xác
            HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
            Collider2D[] colliders = Physics2D.OverlapCircleAll(playerPos, EquippedWeapon.AttackRange + 0.3f);

            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue; // Bỏ qua bản thân người chơi

                var damageable = col.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    Vector2 targetDir = damageable.Position - playerPos;
                    float dist = targetDir.magnitude;

                    bool isInHitbox = false;

                    switch (EquippedWeapon.ActionType)
                    {
                        case CombatActionType.Thrust:
                            // Đâm thẳng: Khoảng cách xa hơn, góc hẹp (< 30°)
                            float angleToTargetThrust = Vector2.Angle(aimDir, targetDir);
                            isInHitbox = (dist <= EquippedWeapon.AttackRange) && (angleToTargetThrust <= 35f);
                            break;

                        case CombatActionType.Smash:
                            // Bổ đập: Vùng nén tại điểm chạm đất phía trước
                            Vector2 smashCenter = playerPos + (aimDir.normalized * (EquippedWeapon.AttackRange * 0.7f));
                            isInHitbox = Vector2.Distance(smashCenter, damageable.Position) <= 0.8f;
                            break;

                        case CombatActionType.Slash:
                        default:
                            // Vung chém: Góc quạt hình nón (ArcAngle)
                            float angleToTargetSlash = Vector2.Angle(aimDir, targetDir);
                            isInHitbox = (dist <= EquippedWeapon.AttackRange) && (angleToTargetSlash <= EquippedWeapon.ArcAngle * 0.5f);
                            break;
                    }

                    if (isInHitbox && hitTargets.Add(damageable))
                    {
                        damageable.TakeDamage(EquippedWeapon.BaseDamage, aimDir, EquippedWeapon.KnockbackForce);
                    }
                }
            }
        }

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
