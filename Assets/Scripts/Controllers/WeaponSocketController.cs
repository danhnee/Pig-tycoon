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

        // Bảng tra cứu Socket 4 hướng cho Slash và Thrust
        private static readonly Dictionary<string, WeaponSocketData[]> Sockets = new Dictionary<string, WeaponSocketData[]>
        {
            // SLASH: Tay quạt 180 độ
            // Down: Trên vai xuống rốn
            { "slash_down", new WeaponSocketData[] {
                new WeaponSocketData(44, 17, 115f, 1),
                new WeaponSocketData(47, 30, 30f, 1),
                new WeaponSocketData(35, 42, -75f, 1),
                new WeaponSocketData(31, 44, -90f, 1)
            }},
            // Up: Hông phải quạt lên cao qua đầu
            { "slash_up", new WeaponSocketData[] {
                new WeaponSocketData(44, 41, -75f, -1),
                new WeaponSocketData(47, 28, 20f, -1),
                new WeaponSocketData(36, 14, 110f, -1),
                new WeaponSocketData(38, 20, 90f, -1)
            }},
            // Left: Sau lưng vung ra trước mặt trái
            { "slash_left", new WeaponSocketData[] {
                new WeaponSocketData(46, 28, -150f, -1),
                new WeaponSocketData(26, 29, -70f, 1),
                new WeaponSocketData(13, 31, 10f, 1),
                new WeaponSocketData(17, 35, 0f, 1)
            }},
            // Right: Sau lưng vung ra trước mặt phải
            { "slash_right", new WeaponSocketData[] {
                new WeaponSocketData(18, 28, 150f, -1),
                new WeaponSocketData(38, 29, 70f, 1),
                new WeaponSocketData(51, 31, -10f, 1),
                new WeaponSocketData(47, 35, 0f, 1)
            }},

            // THRUST: Co sát ngực rồi đẩy tịnh tiến thẳng ra xa hết cỡ
            // Down: Co ngực -> đẩy thẳng xuống
            { "thrust_down", new WeaponSocketData[] {
                new WeaponSocketData(32, 32, -90f, 1),
                new WeaponSocketData(32, 39, -90f, 1),
                new WeaponSocketData(32, 48, -90f, 1),
                new WeaponSocketData(32, 45, -90f, 1)
            }},
            // Up: Co ngực -> đẩy thẳng lên qua đầu
            { "thrust_up", new WeaponSocketData[] {
                new WeaponSocketData(32, 30, 90f, 1),
                new WeaponSocketData(32, 21, 90f, 1),
                new WeaponSocketData(32, 11, 90f, 1),
                new WeaponSocketData(32, 16, 90f, 1)
            }},
            // Left: Co ngực -> đẩy tịnh tiến sang trái
            { "thrust_left", new WeaponSocketData[] {
                new WeaponSocketData(27, 31, 180f, 1),
                new WeaponSocketData(19, 31, 180f, 1),
                new WeaponSocketData(10, 31, 180f, 1),
                new WeaponSocketData(14, 31, 180f, 1)
            }},
            // Right: Co ngực -> đẩy tịnh tiến sang phải
            { "thrust_right", new WeaponSocketData[] {
                new WeaponSocketData(37, 31, 0f, 1),
                new WeaponSocketData(45, 31, 0f, 1),
                new WeaponSocketData(54, 31, 0f, 1),
                new WeaponSocketData(50, 31, 0f, 1)
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

            // Tải sprite vũ khí
            string spritePath = $"Art/Sprites/Weapons/weapon_{weaponId}.png";
            Sprite weaponSprite = UISpriteLoader.LoadSprite(spritePath);
            if (weaponSprite == null)
            {
                // Fallback default dao_go
                weaponSprite = UISpriteLoader.LoadSprite("Art/Sprites/Weapons/weapon_dao_go.png");
            }

            weaponRenderer.sprite = weaponSprite;
            weaponHolder.transform.localPosition = data.ToLocalPosition();
            weaponHolder.transform.localRotation = Quaternion.Euler(0f, 0f, data.RotationZ);

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
