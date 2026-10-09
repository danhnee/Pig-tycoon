using System;
using System.Collections.Generic;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public struct WeaponSocketData
    {
        public Vector2 PixelPosition;   // Tọa độ pixel (0..64) trên canvas
        public float RotationZ;         // Góc xoay của vũ khí (độ)
        public int SortingOffset;       // Vẽ trước (+1) hay sau (-1) cơ thể

        public WeaponSocketData(int x, int y, float rotationZ, int sortingOffset = 1)
        {
            PixelPosition = new Vector2(x, y);
            RotationZ = rotationZ;
            SortingOffset = sortingOffset;
        }

        public Vector3 ToLocalPosition(float pixelsPerUnit = 64f)
        {
            // Canvas 64x64, tâm (32, 32)
            float lx = (PixelPosition.x - 32f) / pixelsPerUnit;
            float ly = (32f - PixelPosition.y) / pixelsPerUnit;
            return new Vector3(lx, ly, 0f);
        }
    }

    /// <summary>
    /// Quản lý điểm neo bàn tay (Hand Socket) và cơ chế đè hình vũ khí theo thời gian thực (Modular Weapon Overlay).
    /// Khi không tấn công: Vũ khí hoàn toàn vô hình.
    /// Khi tấn công: Vũ khí xuất hiện tại bàn tay nắm hờ (grip pose), xoay và di chuyển bám sát chuyển động tay.
    /// </summary>
    [RequireComponent(typeof(CharacterSpriteAnimator))]
    public class WeaponSocketController : MonoBehaviour
    {
        public static WeaponSocketController Instance { get; private set; }

        [Header("Weapon Visual Object")]
        [SerializeField] private GameObject weaponHolder;
        [SerializeField] private SpriteRenderer weaponRenderer;

        private CharacterSpriteAnimator characterAnimator;
        private SpriteRenderer characterRenderer;

        // Bảng tra cứu Socket 4 hướng cho Slash và Thrust (chuẩn 3 frames theo mẫu gốc của nhân vật)
        private static readonly Dictionary<string, WeaponSocketData[]> Sockets = new Dictionary<string, WeaponSocketData[]>
        {
            // SLASH: 3 frames (Wind-up -> Active Strike -> Recovery)
            // Down: Trên vai trái (màn hình) quạt xuống bụng
            { "slash_down", new WeaponSocketData[] {
                new WeaponSocketData(21, 12, 75f, 1),
                new WeaponSocketData(37, 42, -145f, 1),
                new WeaponSocketData(22, 42, -80f, 1)
            }},
            // Up: Lưng quay lại, tay phải giơ qua vai quạt qua đầu
            { "slash_up", new WeaponSocketData[] {
                new WeaponSocketData(42, 14, 105f, -1),
                new WeaponSocketData(44, 12, 170f, -1),
                new WeaponSocketData(40, 44, -90f, -1)
            }},
            // Left: Sau lưng vung ra trước mặt trái
            { "slash_left", new WeaponSocketData[] {
                new WeaponSocketData(48, 26, 10f, -1),
                new WeaponSocketData(15, 29, 165f, 1),
                new WeaponSocketData(27, 30, -135f, 1)
            }},
            // Right: Sau lưng vung ra trước mặt phải
            { "slash_right", new WeaponSocketData[] {
                new WeaponSocketData(16, 26, 170f, -1),
                new WeaponSocketData(49, 29, 15f, 1),
                new WeaponSocketData(37, 30, -45f, 1)
            }},

            // THRUST: 3 frames (Co ngực -> Đẩy tịnh tiến -> Thu tay)
            // Down: Co ngực -> Đẩy thẳng xuống đất
            { "thrust_down", new WeaponSocketData[] {
                new WeaponSocketData(30, 29, -90f, 1),
                new WeaponSocketData(31, 36, -90f, 1),
                new WeaponSocketData(30, 31, -90f, 1)
            }},
            // Up: Co ngực -> Đẩy thẳng lên qua đầu
            { "thrust_up", new WeaponSocketData[] {
                new WeaponSocketData(40, 32, 90f, -1),
                new WeaponSocketData(34, 22, 90f, -1),
                new WeaponSocketData(40, 34, 90f, -1)
            }},
            // Left: Co ngực -> Đẩy tịnh tiến sang trái
            { "thrust_left", new WeaponSocketData[] {
                new WeaponSocketData(30, 28, 180f, 1),
                new WeaponSocketData(20, 31, 180f, 1),
                new WeaponSocketData(28, 29, 180f, 1)
            }},
            // Right: Co ngực -> Đẩy tịnh tiến sang phải
            { "thrust_right", new WeaponSocketData[] {
                new WeaponSocketData(34, 28, 0f, 1),
                new WeaponSocketData(44, 31, 0f, 1),
                new WeaponSocketData(36, 29, 0f, 1)
            }}
        };

        private void Awake()
        {
            Instance = this;
            characterAnimator = GetComponent<CharacterSpriteAnimator>();
            characterRenderer = GetComponent<SpriteRenderer>();

            EnsureWeaponHolder();
            HideWeapon();
        }

        private void EnsureWeaponHolder()
        {
            if (weaponHolder == null)
            {
                Transform child = transform.Find("WeaponSocketHolder");
                if (child != null)
                {
                    weaponHolder = child.gameObject;
                    weaponRenderer = weaponHolder.GetComponent<SpriteRenderer>();
                }
                else
                {
                    weaponHolder = new GameObject("WeaponSocketHolder");
                    weaponHolder.transform.SetParent(transform, false);
                    weaponRenderer = weaponHolder.AddComponent<SpriteRenderer>();
                }
            }

            if (weaponRenderer != null)
            {
                weaponRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Ẩn vũ khí hoàn toàn khi nhân vật đứng yên hoặc di chuyển
        /// </summary>
        public void HideWeapon()
        {
            if (weaponHolder != null)
            {
                weaponHolder.SetActive(false);
            }
            if (weaponRenderer != null)
            {
                weaponRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Đè hình vũ khí lên vị trí bàn tay nắm hờ theo frame chuyển động
        /// </summary>
        public void UpdateWeaponSocket(CombatActionType actionType, CharacterFacing facing, int frameIndex, string weaponId)
        {
            if (weaponHolder == null || weaponRenderer == null) EnsureWeaponHolder();

            string dirKey = facing.ToString().ToLower();
            if (dirKey == "side") dirKey = "right";

            string actionKey = actionType == CombatActionType.Thrust ? "thrust" : "slash";
            string key = $"{actionKey}_{dirKey}";

            if (!Sockets.TryGetValue(key, out var socketArray) || socketArray == null || socketArray.Length == 0)
            {
                HideWeapon();
                return;
            }

            int safeIndex = Mathf.Clamp(frameIndex, 0, socketArray.Length - 1);
            WeaponSocketData data = socketArray[safeIndex];

            // Tải sprite vũ khí 24x24 px với PPU 64 và pivot tại chuôi kiếm (handle grip)
            Sprite weaponSprite = UISpriteLoader.LoadWeaponSprite(weaponId);

            weaponRenderer.sprite = weaponSprite;
            weaponHolder.transform.localPosition = data.ToLocalPosition();
            weaponHolder.transform.localRotation = Quaternion.Euler(0f, 0f, data.RotationZ - 90f);

            if (characterRenderer != null)
            {
                weaponRenderer.sortingOrder = characterRenderer.sortingOrder + data.SortingOffset;
            }

            weaponHolder.SetActive(true);
            weaponRenderer.enabled = true;
        }

        /// <summary>
        /// Lấy thông tin điểm neo (Hand Socket) của một frame cụ thể
        /// </summary>
        public static bool TryGetSocket(string actionKey, string dirKey, int frameIndex, out WeaponSocketData socketData)
        {
            string key = $"{actionKey.ToLower()}_{dirKey.ToLower()}";
            if (Sockets.TryGetValue(key, out var arr) && arr != null && arr.Length > 0)
            {
                socketData = arr[Mathf.Clamp(frameIndex, 0, arr.Length - 1)];
                return true;
            }
            socketData = default;
            return false;
        }
    }
}
