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
        public float MoveSpeed = MovementSpec.WalkMetersPerSecond;
        public float SpeedMultiplier { get; set; } = 1.0f;
        public bool CanMove { get; set; } = true;
        public bool DevForceRun;
        [Tooltip("Joystick tilt at or above this runs (touch has no Shift key). Below it the walk speed follows the tilt.")]
        public float RunJoystickThreshold = 0.85f;
        [Tooltip("Seconds to reach full speed / to stop. Short ramps read as weight without feeling slow (UX default).")]
        public float AccelerateSeconds = 0.08f;
        public float DecelerateSeconds = 0.06f;
        /// <summary>True while the locomotion step is a run (Shift on PC, full joystick tilt on touch).</summary>
        public bool IsRunning { get; private set; }
        /// <summary>Speed actually travelled this physics step (m/s); the animator scales its stride rate by this.</summary>
        public float ActualSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
        private bool joystickDriven;
        private float analogScale = 1f;
        public float CarriedMassKg { get; set; } = 0f;

        [Header("2D Visual & Sorting")]
        public SpriteRenderer SpriteRenderer;
        public bool AutoDynamicSortingOrder = true;
        public int SortingPrecision = 100;

        private Rigidbody2D rb;
        private CharacterData characterData;
        private Vector2 movementInput;
        public Vector2 MoveInput => movementInput;
        /// <summary>
        /// Demo/recording hook (PixelMyth DemoAutopilot, flag -pigtycoon-scenario=video). When non-null it replaces
        /// joystick/keyboard input; null in normal play so behavior is unchanged.
        /// </summary>
        public Vector2? ScriptedMoveInput { get; set; }
        public Vector2 FacingDirection { get; set; } = Vector2.down;

        public void SetFacingDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                FacingDirection = direction.normalized;
            }
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

            if (GetComponent<MartialSkillRunner>() == null)
            {
                gameObject.AddComponent<MartialSkillRunner>(); // võ kỹ + túi nhanh F5–F8
            }

            if (GetComponent<PlayerWeatherStatusDriver>() == null)
            {
                gameObject.AddComponent<PlayerWeatherStatusDriver>(); // GDD 7.2 trạng thái thời tiết lên người
            }

            if (SlashVFXController.Instance == null)
            {
                var vfxManager = new GameObject("SlashVFXManager");
                vfxManager.AddComponent<SlashVFXController>();
            }
        }

        private void Start()
        {
            if (MobileGameController.Instance != null)
            {
                characterData = MobileGameController.Instance.Engine.Character;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!CanMove || BackpackPopup.InputBlockedThisFrame)
            {
                movementInput = Vector2.zero;
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }

            float h = 0f;
            float v = 0f;

            joystickDriven = false;
            if (Joystick != null && (Mathf.Abs(Joystick.Horizontal) > 0.01f || Mathf.Abs(Joystick.Vertical) > 0.01f))
            {
                h = Joystick.Horizontal;
                v = Joystick.Vertical;
                joystickDriven = true;
            }
            else
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
            }

            if (ScriptedMoveInput.HasValue) { h = ScriptedMoveInput.Value.x; v = ScriptedMoveInput.Value.y; joystickDriven = false; }
            // The tool wheel owns joystick/keyboard direction while open; the player stands still (resumes on close).
            if (RadialToolWheel.Instance != null && RadialToolWheel.Instance.IsOpen) { h = 0f; v = 0f; }
            movementInput = new Vector2(h, v);
            if (movementInput.sqrMagnitude > 1f)
            {
                movementInput.Normalize();
            }

            if (movementInput.sqrMagnitude > CardinalFacing.MoveEpsilonSqr)
            {
                FacingDirection = movementInput.normalized;
            }

            if (AutoDynamicSortingOrder && SpriteRenderer != null)
            {
                SpriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * SortingPrecision);
            }

            ApplyLocomotion();
        }

        private void ApplyLocomotion()
        {
            bool moving = movementInput.sqrMagnitude > CardinalFacing.MoveEpsilonSqr;
            if (!moving || characterData == null)
            {
                MoveSpeed = MovementSpec.WalkMetersPerSecond;
                IsRunning = false;
                analogScale = 1f;
                return;
            }

            float tilt = Mathf.Clamp01(movementInput.magnitude);
            bool wantsRun = DevForceRun || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                || (joystickDriven && tilt >= RunJoystickThreshold);
            // Touch: a light tilt walks slower (down to 45%), so small corrections do not sprint across the pen.
            analogScale = joystickDriven && !wantsRun ? Mathf.Lerp(0.45f, 1f, Mathf.InverseLerp(0.15f, RunJoystickThreshold, tilt)) : 1f;
            LocomotionStep step = PlayerLocomotion.Advance(
                wantsRun,
                characterData.BaseStats.Agi,
                characterData.Stamina.CurrentStamina,
                Time.deltaTime,
                KhoRatExtraRunStamina()); // GDD 7.2 Khô Rát

            float carryPenalty = CarryCapacity.StaminaMultiplier(CarriedMassKg, characterData.Stamina.Condition);
            MoveSpeed = step.MetersPerSecond * MartialProgression.StyleSpeedMultiplier(characterData); // GDD 12.2 lối chơi
            IsRunning = step.MetersPerSecond > MovementSpec.WalkMetersPerSecond + 0.01f;
            characterData.Stamina.CurrentStamina = Mathf.Max(0f, characterData.Stamina.CurrentStamina + step.StaminaDelta * carryPenalty);
            characterData.Stamina.DailyLoad += step.DailyLoadDelta * carryPenalty;
        }

        private static float KhoRatExtraRunStamina()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            return engine != null ? engine.WeatherStatus.ExtraSprintStaminaPerSecond(engine) : 0f;
        }

        private void FixedUpdate()
        {
            if (rb == null) return;

            if (!CanMove || BackpackPopup.InputBlockedThisFrame)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 target = movementInput.sqrMagnitude > CardinalFacing.MoveEpsilonSqr
                ? movementInput.normalized * (MoveSpeed * SpeedMultiplier * analogScale)
                : Vector2.zero;
            // Short ramp instead of an instant start/stop, so the sprite does not snap into full stride.
            float ramp = target.sqrMagnitude > rb.linearVelocity.sqrMagnitude ? AccelerateSeconds : DecelerateSeconds;
            float maxDelta = ramp <= 0f ? float.MaxValue : Mathf.Max(MoveSpeed, MovementSpec.WalkMetersPerSecond) / ramp * Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, maxDelta);
        }

        #region 3 Ô Võ Kỹ (GDD 12: kỹ năng đã học, lắp theo ô, mở bởi cặp trang bị)
        public void ExecuteMartialSkill(CombatSlotType slot)
        {
            if (characterData == null) return;
            var runner = GetComponent<MartialSkillRunner>() ?? gameObject.AddComponent<MartialSkillRunner>();
            runner.Cast(slot);
        }
        #endregion
    }
}
