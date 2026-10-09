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

    public struct SwordSwipeFrame
    {
        public int PixelX;
        public int PixelY;
        public float RotationZ;
        public int SortingOffset;       // -1: sau lưng, +1: trước ngực
        public float DurationSeconds;

        public SwordSwipeFrame(int x, int y, float rotationZ, int sortingOffset, float durationSeconds)
        {
            PixelX = x;
            PixelY = y;
            RotationZ = rotationZ;
            SortingOffset = sortingOffset;
            DurationSeconds = durationSeconds;
        }

        public Vector3 ToLocalPosition(float pixelsPerUnit = 64f)
        {
            float lx = (PixelX - 32f) / pixelsPerUnit;
            float ly = (32f - PixelY) / pixelsPerUnit;
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
        // Bảng tra cứu Socket 4 hướng cho Slash và Thrust (chuẩn 3 frames theo mẫu gốc của nhân vật)
        // Đo đạc trực tiếp từ tọa độ pixel bàn tay thực tế, chuẩn hóa theo cơ chế xoay & phân lớp (Layering) Stardew Valley 1.6
        private static readonly Dictionary<string, WeaponSocketData[]> Sockets = new Dictionary<string, WeaponSocketData[]>
        {
            // SLASH: 3 frames (Wind-up -> Active Strike -> Recovery)
            // Down: Trên vai giương lên (sau lưng) -> chém vòng cung qua ngực/bụng -> buông xuôi cạnh hông
            { "slash_down", new WeaponSocketData[] {
                new WeaponSocketData(33, 21, 60f, -1),
                new WeaponSocketData(37, 42, -145f, 1),
                new WeaponSocketData(40, 42, -90f, 1)
            }},
            // Up: Chuẩn bị ở hông (sau lưng) -> vung cao quá đầu chém lên (sau lưng) -> hạ kiếm hồi thế (sau lưng)
            { "slash_up", new WeaponSocketData[] {
                new WeaponSocketData(20, 43, -90f, -1),
                new WeaponSocketData(24, 22, 90f, -1),
                new WeaponSocketData(40, 44, -90f, -1)
            }},
            // Left: Giương sau lưng bên phải -> chém vung mạnh ra trước bên trái -> hạ thế thu kiếm
            { "slash_left", new WeaponSocketData[] {
                new WeaponSocketData(47, 26, 10f, -1),
                new WeaponSocketData(14, 29, 165f, 1),
                new WeaponSocketData(26, 27, -135f, 1)
            }},
            // Right: Giương sau lưng bên trái -> chém vung mạnh ra trước bên phải -> hạ thế thu kiếm
            { "slash_right", new WeaponSocketData[] {
                new WeaponSocketData(16, 26, 170f, -1),
                new WeaponSocketData(49, 29, 15f, 1),
                new WeaponSocketData(37, 27, -45f, 1)
            }},

            // THRUST: 3 frames (Co ngực -> Đẩy tịnh tiến -> Thu tay)
            // Down: Co ngực -> Đẩy thẳng xuống đất -> Thu tay
            { "thrust_down", new WeaponSocketData[] {
                new WeaponSocketData(30, 25, -90f, 1),
                new WeaponSocketData(31, 36, -90f, 1),
                new WeaponSocketData(30, 30, -90f, 1)
            }},
            // Up: Co ngực -> Đẩy thẳng lên qua đầu -> Thu tay (luôn sau lưng)
            { "thrust_up", new WeaponSocketData[] {
                new WeaponSocketData(30, 34, 90f, -1),
                new WeaponSocketData(33, 24, 90f, -1),
                new WeaponSocketData(32, 32, 90f, -1)
            }},
            // Left: Co ngực -> Đẩy tịnh tiến sang trái -> Thu tay
            { "thrust_left", new WeaponSocketData[] {
                new WeaponSocketData(30, 28, 180f, 1),
                new WeaponSocketData(20, 32, 180f, 1),
                new WeaponSocketData(29, 28, 180f, 1)
            }},
            // Right: Co ngực -> Đẩy tịnh tiến sang phải -> Thu tay
            { "thrust_right", new WeaponSocketData[] {
                new WeaponSocketData(34, 28, 0f, 1),
                new WeaponSocketData(43, 32, 0f, 1),
                new WeaponSocketData(34, 28, 0f, 1)
            }}
        };

        private Coroutine activeSwipeCoroutine;

        // Bảng tra cứu chu kỳ vung kiếm 6 frames chuẩn Stardew Valley (MeleeWeapon.cs & FarmerSprite.cs)
        // Cơ chế: 6 góc quét xoay 180 độ, phân lớp động (SortingOffset -1 sau lưng, +1 trước ngực),
        // căn chỉnh chính xác theo trục chuôi kiếm (11.5, 20.5) và thân nhân vật 64x64.
        private static readonly Dictionary<CharacterFacing, SwordSwipeFrame[]> SwordSwipeSequences = new Dictionary<CharacterFacing, SwordSwipeFrame[]>
        {
            {
                CharacterFacing.Right, new SwordSwipeFrame[]
                {
                    new SwordSwipeFrame(20, 24, 25f, -1, 0.055f),   // Frame 0: Giương kiếm sau vai (sau lưng)
                    new SwordSwipeFrame(32, 18, -25f, -1, 0.045f),  // Frame 1: Vung qua đỉnh đầu
                    new SwordSwipeFrame(42, 26, -80f, 1, 0.035f),   // Frame 2: Chém thẳng ra trước (Đòn đánh + VFX)
                    new SwordSwipeFrame(44, 34, -125f, 1, 0.035f),  // Frame 3: Chém xéo xuống hông
                    new SwordSwipeFrame(38, 40, -160f, 1, 0.035f),  // Frame 4: Quét đà qua đùi
                    new SwordSwipeFrame(32, 36, -140f, 1, 0.075f)   // Frame 5: Hồi kiếm cạnh hông
                }
            },
            {
                CharacterFacing.Left, new SwordSwipeFrame[]
                {
                    new SwordSwipeFrame(44, 24, -25f, -1, 0.055f),  // Frame 0: Giương kiếm sau vai (sau lưng)
                    new SwordSwipeFrame(32, 18, 25f, -1, 0.045f),   // Frame 1: Vung qua đỉnh đầu
                    new SwordSwipeFrame(22, 26, 80f, 1, 0.035f),    // Frame 2: Chém thẳng ra trước (Đòn đánh + VFX)
                    new SwordSwipeFrame(20, 34, 125f, 1, 0.035f),   // Frame 3: Chém xéo xuống hông
                    new SwordSwipeFrame(26, 40, 160f, 1, 0.035f),   // Frame 4: Quét đà qua đùi
                    new SwordSwipeFrame(32, 36, 140f, 1, 0.075f)    // Frame 5: Hồi kiếm cạnh hông
                }
            },
            {
                CharacterFacing.Down, new SwordSwipeFrame[]
                {
                    new SwordSwipeFrame(22, 24, -35f, 1, 0.055f),   // Frame 0: Giương kiếm vai phải
                    new SwordSwipeFrame(26, 28, -75f, 1, 0.045f),   // Frame 1: Bắt đầu chém chéo xuống
                    new SwordSwipeFrame(32, 32, -120f, 1, 0.035f),  // Frame 2: Chém ngang qua ngực (Đòn đánh + VFX)
                    new SwordSwipeFrame(36, 36, -165f, 1, 0.035f),  // Frame 3: Cắt xéo xuống rốn
                    new SwordSwipeFrame(38, 40, 150f, 1, 0.035f),   // Frame 4: Quét đà qua hông trái
                    new SwordSwipeFrame(36, 38, 165f, 1, 0.075f)    // Frame 5: Thu kiếm thế thủ
                }
            },
            {
                CharacterFacing.Up, new SwordSwipeFrame[]
                {
                    new SwordSwipeFrame(22, 38, -150f, -1, 0.055f), // Frame 0: Thấp cạnh hông phải (sau lưng)
                    new SwordSwipeFrame(24, 26, -90f, -1, 0.045f),  // Frame 1: Vung dọc sườn lên
                    new SwordSwipeFrame(28, 14, -20f, -1, 0.035f),  // Frame 2: Chém vút qua đầu (Đòn đánh + VFX)
                    new SwordSwipeFrame(34, 12, 25f, -1, 0.035f),   // Frame 3: Quạt vòng cung trên cao
                    new SwordSwipeFrame(40, 20, 75f, -1, 0.035f),   // Frame 4: Đưa đà sang vai trái
                    new SwordSwipeFrame(36, 26, 45f, -1, 0.075f)    // Frame 5: Hồi thế thủ sau lưng
                }
            }
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
        /// Kích hoạt chu kỳ vung kiếm 6-frame chuẩn Stardew Valley
        /// </summary>
        public void PlaySwordSwipe(CharacterFacing facing, float speedMultiplier, Action onHitFrame, Action onComplete, string weaponId = "kiem_sat")
        {
            if (activeSwipeCoroutine != null)
            {
                StopCoroutine(activeSwipeCoroutine);
            }
            activeSwipeCoroutine = StartCoroutine(SwordSwipeRoutine(facing, speedMultiplier, onHitFrame, onComplete, weaponId));
        }

        private System.Collections.IEnumerator SwordSwipeRoutine(CharacterFacing facing, float speedMultiplier, Action onHitFrame, Action onComplete, string weaponId)
        {
            EnsureWeaponHolder();
            if (weaponHolder == null || weaponRenderer == null)
            {
                onHitFrame?.Invoke();
                onComplete?.Invoke();
                yield break;
            }

            // Gán Sprite vũ khí chính xác
            if (!string.IsNullOrEmpty(weaponId))
            {
                Sprite weaponSp = UISpriteLoader.LoadSprite($"Art/Sprites/Weapons/weapon_{weaponId}.png", 24);
                if (weaponSp != null)
                {
                    weaponRenderer.sprite = weaponSp;
                }
            }

            if (weaponRenderer.sprite == null)
            {
                weaponRenderer.sprite = UISpriteLoader.LoadSprite("Art/Sprites/Weapons/weapon_kiem_sat.png", 24);
            }

            weaponHolder.SetActive(true);
            weaponRenderer.enabled = true;

            // Xác định chuỗi frame theo hướng
            CharacterFacing keyFacing = facing;
            if (keyFacing == CharacterFacing.Side) keyFacing = CharacterFacing.Right;

            if (!SwordSwipeSequences.TryGetValue(keyFacing, out var frames) || frames == null)
            {
                SwordSwipeSequences.TryGetValue(CharacterFacing.Down, out frames);
            }

            int baseSorting = characterRenderer != null ? characterRenderer.sortingOrder : 100;
            float speed = Mathf.Max(0.2f, speedMultiplier);

            if (frames != null)
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    var f = frames[i];
                    weaponHolder.transform.localPosition = f.ToLocalPosition(64f);
                    weaponHolder.transform.localRotation = Quaternion.Euler(0f, 0f, f.RotationZ);
                    weaponRenderer.sortingOrder = baseSorting + f.SortingOffset;

                    // Frame 2: Điểm va chạm cực đại (Peak Hit) & kích hoạt sát thương + VFX
                    if (i == 2)
                    {
                        onHitFrame?.Invoke();
                    }

                    float duration = f.DurationSeconds / speed;
                    yield return new WaitForSeconds(duration);
                }
            }

            HideWeapon();
            activeSwipeCoroutine = null;
            onComplete?.Invoke();
        }

        public void UpdateWeaponSocket(CombatActionType actionType, CharacterFacing facing, int frameIndex, string weaponId)
        {
            HideWeapon();
        }

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
