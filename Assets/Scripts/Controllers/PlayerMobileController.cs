using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMobileController : MonoBehaviour
    {
        public static PlayerMobileController Instance { get; private set; }

        [Header("2D Movement")]
        public MobileJoystick Joystick;
        public float MoveSpeed = 4.5f;
        public float SpeedMultiplier { get; set; } = 1.0f;
        public bool CanMove { get; set; } = true;

        [Header("2D Visual & Sorting")]
        public SpriteRenderer SpriteRenderer;
        public bool AutoDynamicSortingOrder = true;
        public int SortingPrecision = 100;
        public float FootYOffset = -0.65f; // Chân nhân vật cách tâm transform 0.65m trong không gian 2D

        private Rigidbody2D rb;
        private CharacterData characterData;
        private Vector2 movementInput;
        public Vector2 MoveInput => movementInput;
        public Vector2 FacingDirection { get; set; } = Vector2.down;

        public void SetFacingDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                FacingDirection = direction.normalized;
            }
        }

        private Coroutine slowCoroutine;

        public void ApplySlow(float multiplier, float duration)
        {
            if (slowCoroutine != null) StopCoroutine(slowCoroutine);
            slowCoroutine = StartCoroutine(SlowRoutine(multiplier, duration));
        }

        private System.Collections.IEnumerator SlowRoutine(float multiplier, float duration)
        {
            SpeedMultiplier = multiplier;
            yield return new WaitForSeconds(duration);
            SpeedMultiplier = 1.0f;
            slowCoroutine = null;
        }

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // Top-down 2D không trọng lực
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.offset = new Vector2(0f, -0.35f); // Đặt trọng tâm va chạm tại bàn chân
                col.radius = 0.25f;
            }

            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (GetComponent<CharacterSpriteAnimator>() == null)
            {
                gameObject.AddComponent<CharacterSpriteAnimator>();
            }

            if (GetComponent<PlayerCombatController>() == null)
            {
                gameObject.AddComponent<PlayerCombatController>();
            }

            if (SlashVFXController.Instance == null)
            {
                var vfxManager = new GameObject("SlashVFXManager");
                vfxManager.AddComponent<SlashVFXController>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (MobileGameController.Instance != null)
            {
                characterData = MobileGameController.Instance.Engine.Character;
            }
        }

        private void Update()
        {
            float h = 0f;
            float v = 0f;

            if (Joystick != null && (Mathf.Abs(Joystick.Horizontal) > 0.01f || Mathf.Abs(Joystick.Vertical) > 0.01f))
            {
                h = Joystick.Horizontal;
                v = Joystick.Vertical;
            }
            else
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
            }

            movementInput = new Vector2(h, v);
            if (movementInput.sqrMagnitude > 1f)
            {
                movementInput.Normalize();
            }

            if (movementInput.sqrMagnitude > 0.05f)
            {
                FacingDirection = movementInput.normalized;
            }

            // Hướng hiển thị được CharacterSpriteAnimator quản lý độc lập 4 hướng (Down, Up, Left, Right), không lật flipX

            // 2. Y-Sorting cho Top-down 2D dựa theo điểm tiếp đất (chân nhân vật)
            if (AutoDynamicSortingOrder && SpriteRenderer != null)
            {
                float footY = transform.position.y + FootYOffset;
                SpriteRenderer.sortingOrder = Mathf.RoundToInt(-footY * SortingPrecision);
            }

            // 3. Tiêu hao Stamina chạy khi di chuyển (GDD mục 9.2: 1.3 STA/giây, 20% thành Tải ngày)
            if (characterData == null && MobileGameController.Instance?.Engine != null)
            {
                characterData = MobileGameController.Instance.Engine.Character;
            }

            if (movementInput.sqrMagnitude > 0.01f && characterData != null && characterData.Stamina != null)
            {
                characterData.Stamina.ForceConsumeStamina(1.3f * Time.deltaTime, dailyLoadRatio: 0.20f);
            }
        }

        private void FixedUpdate()
        {
            if (rb == null) return;

            if (!CanMove || (PlayerCombatController.Instance != null && PlayerCombatController.Instance.IsDodging))
            {
                return;
            }

            if (movementInput.sqrMagnitude > 0.01f)
            {
                float finalSpeed = MoveSpeed * SpeedMultiplier;

                // GDD 9.4: Vùng mệt (< 20%): Chạy -15%
                if (characterData != null && characterData.Stamina != null && characterData.Stamina.IsFatigued)
                {
                    finalSpeed *= 0.85f;
                }

                // GDD 12.2: Hiệu ứng Lối chơi theo Hạng cân
                if (characterData != null)
                {
                    string playstyle = characterData.GetPlaystyle();
                    if (playstyle == "KhinhThân")
                    {
                        finalSpeed *= 1.08f; // Khinh Thân: tốc chạy +8%
                    }
                    else if (playstyle == "TrọngGiáp")
                    {
                        finalSpeed *= 0.90f; // Trọng Giáp: tốc chạy -10%
                    }
                }

                rb.linearVelocity = movementInput.normalized * finalSpeed;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        #region 3 Ô Võ Kỹ (Universal 2D Mobile)
        public void ExecuteMartialSkill(CombatSlotType slot)
        {
            if (characterData == null) return;

            switch (slot)
            {
                case CombatSlotType.TheCong:
                    // Thế Công: Stamina (Đoạn Ảnh Trảm / Lướt chém 1.5x damage)
                    if (characterData.Stamina.CurrentStamina >= 8f)
                    {
                        characterData.Stamina.CurrentStamina -= 8f;
                        PlayerCombatController.Instance?.ExecuteDashSlash();
                    }
                    else
                    {
                        PlayerInteractionController.Instance?.ShowFeedback("Không đủ Thể Lực để dùng Thế Công!");
                    }
                    break;

                case CombatSlotType.TheThu:
                    // Thế Thủ: Hộ Thân (Hồi phục 25 HP)
                    if (characterData.Stamina.CurrentStamina >= 10f)
                    {
                        characterData.Stamina.CurrentStamina -= 10f;
                        characterData.Hp = Mathf.Min(characterData.MaxHp, characterData.Hp + 25);
                        PlayerInteractionController.Instance?.ShowFeedback("Kích hoạt Thế Thủ: Hồi phục 25 HP!");
                        MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    }
                    else
                    {
                        PlayerInteractionController.Instance?.ShowFeedback("Không đủ Thể Lực để dùng Thế Thủ!");
                    }
                    break;

                case CombatSlotType.TheBien:
                    // Thế Biến: Tiêu hao 30 Nộ Khí - Nộ Ba Chấn Địa đẩy lùi quái xung quanh
                    if (characterData.Rage >= 30)
                    {
                        characterData.Rage -= 30;
                        Collider2D[] foes = Physics2D.OverlapCircleAll(transform.position, 4.5f);
                        foreach (var f in foes)
                        {
                            var dmg = f.GetComponent<IDamageable>();
                            if (dmg != null && dmg.IsAlive && f.gameObject != gameObject)
                            {
                                Vector2 kDir = (dmg.Position - (Vector2)transform.position).normalized;
                                dmg.TakeDamage(35f, kDir, 6.0f);
                            }
                        }
                        PlayerInteractionController.Instance?.ShowFeedback("Kích hoạt Thế Biến: Nộ Ba Chấn Địa!");
                        MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    }
                    else
                    {
                        PlayerInteractionController.Instance?.ShowFeedback($"Chưa đủ Nộ Khí ({characterData.Rage}/30) để dùng Thế Biến!");
                    }
                    break;
            }
        }
        #endregion
    }
}
