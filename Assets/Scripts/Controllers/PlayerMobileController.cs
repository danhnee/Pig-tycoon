using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMobileController : MonoBehaviour
    {
        [Header("2D Movement")]
        public MobileJoystick Joystick;
        public float MoveSpeed = 4.5f;

        [Header("2D Visual & Sorting")]
        public SpriteRenderer SpriteRenderer;
        public bool AutoDynamicSortingOrder = true;
        public int SortingPrecision = 100;

        private Rigidbody2D rb;
        private CharacterData characterData;
        private Vector2 movementInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // Top-down 2D không trọng lực
            rb.freezeRotation = true;

            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
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
            if (Joystick == null) return;

            movementInput = new Vector2(Joystick.Horizontal, Joystick.Vertical);

            // 1. Flip Sprite theo hướng trái / phải
            if (SpriteRenderer != null && Mathf.Abs(movementInput.x) > 0.05f)
            {
                SpriteRenderer.flipX = movementInput.x < 0f;
            }

            // 2. Y-Sorting cho Top-down 2D (Đứng trước che đứng sau)
            if (AutoDynamicSortingOrder && SpriteRenderer != null)
            {
                SpriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * SortingPrecision);
            }

            // 3. Tiêu hao Stamina chạy khi di chuyển (GDD mục 9.2: 1.3 STA/giây)
            if (movementInput.sqrMagnitude > 0.01f && characterData != null)
            {
                characterData.Stamina.CurrentStamina = Mathf.Max(0f, characterData.Stamina.CurrentStamina - (1.3f * Time.deltaTime));
            }
        }

        private void FixedUpdate()
        {
            if (rb == null) return;

            if (movementInput.sqrMagnitude > 0.01f)
            {
                rb.velocity = movementInput.normalized * MoveSpeed;
            }
            else
            {
                rb.velocity = Vector2.zero;
            }
        }

        #region 3 Ô Võ Kỹ (Universal 2D Mobile)
        public void ExecuteMartialSkill(CombatSlotType slot)
        {
            if (characterData == null) return;

            switch (slot)
            {
                case CombatSlotType.TheCong:
                    // Thế Công: Stamina (ví dụ: Lướt / Đoạn Ảnh Trảm)
                    if (characterData.Stamina.CurrentStamina >= 8f)
                    {
                        characterData.Stamina.CurrentStamina -= 8f;
                        Debug.Log("[Combat 2D] Kích hoạt Thế Công!");
                    }
                    break;

                case CombatSlotType.TheThu:
                    // Thế Thủ: Stamina hoặc HP (Huyết Tế)
                    if (characterData.Stamina.CurrentStamina >= 10f)
                    {
                        characterData.Stamina.CurrentStamina -= 10f;
                        Debug.Log("[Combat 2D] Kích hoạt Thế Thủ (Đỡ đòn / Hộ thân)!");
                    }
                    break;

                case CombatSlotType.TheBien:
                    // Thế Biến: Nộ Khí (ví dụ: Thiên La / Dựng Chốt / Xoay Bầy)
                    if (characterData.Rage >= 30)
                    {
                        characterData.Rage -= 30;
                        Debug.Log("[Combat 2D] Kích hoạt Thế Biến (Khống chế diện rộng)!");
                    }
                    break;
            }
        }
        #endregion
    }
}
