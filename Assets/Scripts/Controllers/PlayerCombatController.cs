using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(PlayerMobileController))]
    public class PlayerCombatController : MonoBehaviour, IDamageable
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
        private Rigidbody2D rb;
        private float nextAttackTime = 0f;
        private bool isArmed = false;
        private bool bufferedAttack = false; // Input Buffering tương tự anotherClick của Stardew Valley
        private Coroutine flashCoroutine;

        public bool IsArmed => isArmed;
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

            // Khởi tạo vũ khí mặc định nếu chưa có
            if (EquippedWeapon == null)
            {
                EquippedWeapon = WeaponDatabase.GetWeapon("dao_go");
            }
        }

        private void Start()
        {
            EnsureAttackButtonUI();

            if (AttackButton != null)
            {
                AttackButton.onClick.RemoveAllListeners();
                AttackButton.onClick.AddListener(ExecuteAttack);
            }

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

        private void EnsureAttackButtonUI()
        {
            if (AttackButtonRoot != null && AttackButton != null) return;

            // Tìm Canvas chính để sinh nút Tấn Công nổi nếu chưa gắn sẵn trong Scene
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("Mobile_Primary_Attack_Button");
            if (existing != null)
            {
                AttackButtonRoot = existing.gameObject;
                AttackButton = existing.GetComponent<Button>();
                AttackButtonIcon = existing.Find("Icon")?.GetComponent<Image>();
                CooldownRadialFill = existing.Find("CooldownFill")?.GetComponent<Image>();
                CooldownText = existing.Find("CooldownText")?.GetComponent<TextMeshProUGUI>();
                return;
            }

            // Tự động tạo nút Combat nổi ở góc dưới bên phải màn hình
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

            // Icon thanh gươm / đao
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(AttackButtonRoot.transform, false);
            var iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.sizeDelta = new Vector2(-18f, -18f);
            AttackButtonIcon = iconObj.AddComponent<Image>();
            AttackButtonIcon.sprite = UISpriteLoader.LoadSprite("Art/Sprites/Weapons/weapon_dao_go.png", 24);
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

            AttackButtonRoot.SetActive(isArmed);
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
            if (AttackButtonIcon != null && newWeapon.Icon != null)
            {
                AttackButtonIcon.sprite = newWeapon.Icon;
            }
        }

        public void EquipWeaponById(string weaponId)
        {
            var def = WeaponDatabase.GetWeapon(weaponId);
            if (def != null) SwitchWeapon(def);
        }

        private void Update()
        {
            // Bàn phím: Phím J, K, F hoặc Space (khi cầm vũ khí) để tấn công
            if (isArmed)
            {
                if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.F))
                {
                    ExecuteAttack();
                }
            }

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

        /// <summary>
        /// Tấn công có hỗ trợ Smart Auto-Targeting: Nếu có quái trong bán kính 3.5m, tự động khóa hướng và vung đòn về phía đối thủ.
        /// </summary>
        /// <summary>
        /// Tấn công có hỗ trợ Smart Auto-Targeting & Input Buffering (anotherClick chuẩn Stardew Valley)
        /// </summary>
        public void ExecuteAttack()
        {
            if (spriteAnimator != null && spriteAnimator.IsAttacking)
            {
                bufferedAttack = true; // Buffer cú click kế tiếp để combo mượt mà
                return;
            }
            if (Time.time < nextAttackTime) return;
            if (EquippedWeapon == null) return;

            Vector2 aimDir = mobileController != null ? mobileController.FacingDirection : Vector2.down;

            // Smart Auto-Targeting: Tìm kẻ địch hoặc dummy gần nhất
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
        /// Tấn công chỉ định hướng theo tọa độ click chuột trên thế giới 2D
        /// </summary>
        public void ExecuteAttackTowards(Vector2 targetWorldPos)
        {
            if (spriteAnimator != null && spriteAnimator.IsAttacking)
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

        /// <summary>
        /// Kỹ năng Thế Công (Martial Skill): Lướt tới chém nhanh gây 1.5x sát thương
        /// </summary>
        public void ExecuteDashSlash()
        {
            if (EquippedWeapon == null) return;

            Vector2 aimDir = mobileController != null ? mobileController.FacingDirection : Vector2.down;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.down;

            // Smart Auto-Lock
            IDamageable target = FindNearestTarget(4.0f);
            if (target != null)
            {
                aimDir = (target.Position - (Vector2)transform.position).normalized;
            }

            // Lướt tới trước
            if (rb != null)
            {
                rb.AddForce(aimDir.normalized * 5f, ForceMode2D.Impulse);
            }

            PerformAttackCycle(aimDir, damageMultiplier: 1.5f);
        }

        private void PerformAttackCycle(Vector2 aimDir, float damageMultiplier)
        {
            nextAttackTime = Time.time + EquippedWeapon.Cooldown;

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
                PerformHitDetection(aimDir, damageMultiplier);
                OnAttackCycleCompleted();
            }
        }

        private void OnAttackCycleCompleted()
        {
            if (bufferedAttack)
            {
                bufferedAttack = false;
                ExecuteAttack();
            }
        }

        private IDamageable FindNearestTarget(float maxDistance)
        {
            Vector2 playerPos = transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, maxDistance);
            float nearestDistSqr = float.MaxValue;
            IDamageable best = null;

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;

                var dmg = col.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    float dSqr = (dmg.Position - playerPos).sqrMagnitude;
                    if (dSqr < nearestDistSqr)
                    {
                        nearestDistSqr = dSqr;
                        best = dmg;
                    }
                }
            }
            return best;
        }

        private void PerformHitDetection(Vector2 aimDir, float damageMultiplier)
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

            // 2. Tính toán sát thương theo chỉ số Str của Khoa
            int charStr = 10;
            var engine = MobileGameController.Instance?.Engine;
            if (engine?.Character?.BaseStats != null)
            {
                charStr = engine.Character.BaseStats.Str + engine.Character.BonusStats.Str;
            }

            float finalDamage = CombatFormulas.CalculateDamage(EquippedWeapon.BaseDamage * damageMultiplier, attackerStr: charStr);

            // 3. Quét va chạm Hitbox chuẩn xác
            HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
            Collider2D[] colliders = Physics2D.OverlapCircleAll(playerPos, EquippedWeapon.AttackRange + 0.35f);

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
                            float angleToTargetThrust = Vector2.Angle(aimDir, targetDir);
                            isInHitbox = (dist <= EquippedWeapon.AttackRange) && (angleToTargetThrust <= 35f);
                            break;

                        case CombatActionType.Smash:
                            Vector2 smashCenter = playerPos + (aimDir.normalized * (EquippedWeapon.AttackRange * 0.7f));
                            isInHitbox = Vector2.Distance(smashCenter, damageable.Position) <= 0.85f;
                            break;

                        case CombatActionType.Slash:
                        default:
                            float angleToTargetSlash = Vector2.Angle(aimDir, targetDir);
                            isInHitbox = (dist <= EquippedWeapon.AttackRange) && (angleToTargetSlash <= EquippedWeapon.ArcAngle * 0.5f);
                            break;
                    }

                    if (isInHitbox && hitTargets.Add(damageable))
                    {
                        damageable.TakeDamage(finalDamage, aimDir, EquippedWeapon.KnockbackForce);
                    }
                }
            }
        }

        #region IDamageable Implementation for Player Khoa
        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData == null) return;

            int res = charData.BaseStats.Res + charData.BonusStats.Res;
            float mitigated = CombatFormulas.CalculateDamage(amount, attackerStr: 10, defenderRes: res);

            charData.Hp = Mathf.Max(0, charData.Hp - (int)mitigated);

            // Tích tụ Nộ Khí khi nhận sát thương
            int rageGain = CombatFormulas.CalculateRageGainOnDamage(mitigated);
            charData.Rage = Mathf.Min(100, charData.Rage + rageGain);

            // 1. Nhấp nháy đỏ nhân vật
            if (spriteAnimator != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(PlayerFlashRoutine());
            }

            // 2. Bật lùi (Knockback)
            if (rb != null && knockbackForce > 0f)
            {
                rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
            }

            // 3. Popup số sát thương màu đỏ
            SpawnPlayerDamagePopup(mitigated);

            MobileGameController.Instance?.OnStateUpdated?.Invoke();

            // 4. Nếu HP về 0: Ngất xỉu & hồi sinh tại cửa nhà
            if (charData.Hp <= 0)
            {
                HandlePlayerFainted();
            }
        }

        private IEnumerator PlayerFlashRoutine()
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1f, 0.3f, 0.3f, 1f);
                yield return new WaitForSeconds(0.12f);
                sr.color = Color.white;
            }
        }

        private void SpawnPlayerDamagePopup(float damage)
        {
            GameObject popupObj = new GameObject("PlayerDamagePopup");
            popupObj.transform.position = transform.position + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0.8f, 0f);

            var textMesh = popupObj.AddComponent<TextMeshPro>();
            textMesh.text = $"-{Mathf.RoundToInt(damage)} HP";
            textMesh.fontSize = 5f;
            textMesh.color = new Color(1f, 0.2f, 0.2f, 1f); // Đỏ tươi
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 800;

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

            transform.position = Vector3.zero; // Quay về vị trí trung tâm nông trại an toàn

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
