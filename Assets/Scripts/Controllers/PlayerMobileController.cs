using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMobileController : MonoBehaviour
    {
        [Header("Movement")]
        public MobileJoystick Joystick;
        public float MoveSpeed = 5.0f;
        public float SprintMultiplier = 1.4f;

        private CharacterController characterController;
        private CharacterData characterData;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
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
            if (Joystick == null || characterController == null) return;

            Vector3 moveDir = new Vector3(Joystick.Horizontal, 0f, Joystick.Vertical);
            if (moveDir.magnitude > 0.05f)
            {
                // Xoay mặt theo hướng di chuyển
                transform.forward = Vector3.Slerp(transform.forward, moveDir.normalized, Time.deltaTime * 12f);

                // Di chuyển
                characterController.Move(moveDir * (MoveSpeed * Time.deltaTime));

                // Tiêu hao Stamina chạy (1.3/s theo GDD mục 9.2)
                if (characterData != null)
                {
                    characterData.Stamina.CurrentStamina = Mathf.Max(0f, characterData.Stamina.CurrentStamina - (1.3f * Time.deltaTime));
                }
            }
        }

        #region 3 Võ Kỹ (Combat Slots)
        public void ExecuteMartialSkill(CombatSlotType slot)
        {
            if (characterData == null) return;

            switch (slot)
            {
                case CombatSlotType.TheCong:
                    // Dùng Stamina (Ví dụ 8 Stamina)
                    if (characterData.Stamina.CurrentStamina >= 8f)
                    {
                        characterData.Stamina.CurrentStamina -= 8f;
                        Debug.Log("[Combat] Kích hoạt Thế Công!");
                    }
                    break;

                case CombatSlotType.TheThu:
                    // Dùng Stamina hoặc HP (Huyết Tế)
                    if (characterData.Stamina.CurrentStamina >= 10f)
                    {
                        characterData.Stamina.CurrentStamina -= 10f;
                        Debug.Log("[Combat] Kích hoạt Thế Thủ (Đỡ đòn / Tạo khiên)!");
                    }
                    break;

                case CombatSlotType.TheBien:
                    // Dùng Nộ Khí (Ví dụ 30 Nộ)
                    if (characterData.Rage >= 30)
                    {
                        characterData.Rage -= 30;
                        Debug.Log("[Combat] Kích hoạt Thế Biến (Tuyệt kỹ / Khống chế bầy)!");
                    }
                    break;
            }
        }
        #endregion
    }
}
