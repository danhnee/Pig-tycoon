using System;
using System.Collections;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public enum CharacterFacing
    {
        Down,
        Up,
        Left,
        Right,
        Side
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterSpriteAnimator : MonoBehaviour
    {
        [Header("Idle Animation Frames (Authentic Artist Sprites)")]
        public Sprite[] DownIdleFrames;
        public Sprite[] UpIdleFrames;
        public Sprite[] LeftIdleFrames;
        public Sprite[] RightIdleFrames;
        public Sprite[] SideIdleFrames;

        [Header("Single Idle Fallbacks")]
        public Sprite DownIdle;
        public Sprite UpIdle;
        public Sprite LeftIdle;
        public Sprite RightIdle;
        public Sprite SideIdle;

        [Header("Walk Animation Frames (4 frames each)")]
        public Sprite[] DownWalk;
        public Sprite[] UpWalk;
        public Sprite[] LeftWalk;
        public Sprite[] RightWalk;
        public Sprite[] SideWalk;


        [Header("Happy / Level-Up Emote Frames (4 frames)")]
        public Sprite[] HappyFrames;

        [Header("Action Sprite (Rải cám / Chải lông / Sửa rào)")]
        public Sprite ActionSprite;

        [Header("Animation Settings")]
        public float IdleFPS = 4.0f;
        public float WalkFPS = 7.5f;

        [Header("Backpack Visual Variation (Từ Sprite Sheet gốc)")]
        [SerializeField] private bool hasBackpack = false;
        public bool HasBackpack
        {
            get => hasBackpack;
            set
            {
                if (hasBackpack != value)
                {
                    hasBackpack = value;
                    ReloadSprites();
                }
            }
        }

        private SpriteRenderer spriteRenderer;
        private PlayerMobileController playerController;
        private Rigidbody2D rb;
        private WeaponSocketController weaponSocketController;
        private CharacterFacing currentFacing = CharacterFacing.Down;
        private float actionTimer = 0f;
        private float happyTimer = 0f;
        private bool isAttacking = false;

        public CharacterFacing CurrentFacing => currentFacing;
        public bool IsAttacking => isAttacking;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerController = GetComponent<PlayerMobileController>();
            rb = GetComponent<Rigidbody2D>();
            weaponSocketController = GetComponent<WeaponSocketController>();
            if (weaponSocketController == null)
            {
                weaponSocketController = gameObject.AddComponent<WeaponSocketController>();
            }
            LoadSpritesIfNeeded();
        }

        public void ReloadSprites()
        {
            string prefix = hasBackpack ? "player_backpack_" : "player_";

            DownIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_idle_4.png")
            };

            UpIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_idle_4.png")
            };

            LeftIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}left_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}left_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}left_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}left_idle_4.png")
            };

            RightIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}right_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}right_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}right_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}right_idle_4.png")
            };

            SideIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_idle_4.png")
            };

            DownIdle = DownIdleFrames[0];
            UpIdle = UpIdleFrames[0];
            LeftIdle = LeftIdleFrames[0];
            RightIdle = RightIdleFrames[0];
            SideIdle = SideIdleFrames[0];

            DownWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_walk4.png")
            };

            UpWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_walk4.png")
            };

            LeftWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}left_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}left_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}left_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}left_walk4.png")
            };

            RightWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}right_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}right_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}right_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}right_walk4.png")
            };

            SideWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_walk4.png")
            };
        }

        private void LoadSpritesIfNeeded()
        {
            if (DownIdleFrames == null || DownIdleFrames.Length == 0 || DownIdleFrames[0] == null)
            {
                ReloadSprites();
            }

            // 3. Tải Happy Emote Frames
            if (HappyFrames == null || HappyFrames.Length == 0 || HappyFrames[0] == null)
            {
                HappyFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_happy_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_happy_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_happy_3.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_happy_4.png")
                };
            }

            // 4. Action Sprite
            if (ActionSprite == null) ActionSprite = UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_action.png");
        }

        private void Update()
        {
            if (spriteRenderer == null) return;

            // 1. Đang tấn công
            if (isAttacking) return;

            // 2. Đang phát Happy Emote
            if (happyTimer > 0f)
            {
                happyTimer -= Time.deltaTime;
                PlayFrames(HappyFrames, 6f);
                return;
            }

            // 3. Đang phát Action tương tác (rải cám, tưới nước)
            if (actionTimer > 0f)
            {
                actionTimer -= Time.deltaTime;
                if (ActionSprite != null)
                {
                    spriteRenderer.sprite = ActionSprite;
                    return;
                }
            }

            // 4. Lấy vector di chuyển
            Vector2 moveVec = Vector2.zero;
            if (playerController != null && playerController.MoveInput.sqrMagnitude > 0.01f)
            {
                moveVec = playerController.MoveInput;
            }
            else if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                moveVec = rb.linearVelocity;
            }

            bool isMoving = moveVec.sqrMagnitude > 0.05f;

            if (isMoving)
            {
                walkAnimTimer += Time.deltaTime;

                // Xác định hướng chiếm ưu thế (Down, Up, Left, Right hoàn toàn độc lập, không flipX)
                spriteRenderer.flipX = false;

                if (Mathf.Abs(moveVec.x) > Mathf.Abs(moveVec.y) * 0.8f)
                {
                    if (moveVec.x < 0f)
                    {
                        currentFacing = CharacterFacing.Left;
                        PlayFramesWithTimer(LeftWalk != null && LeftWalk.Length > 0 ? LeftWalk : SideWalk, WalkFPS, walkAnimTimer);
                    }
                    else
                    {
                        currentFacing = CharacterFacing.Right;
                        PlayFramesWithTimer(RightWalk != null && RightWalk.Length > 0 ? RightWalk : SideWalk, WalkFPS, walkAnimTimer);
                    }
                }
                else if (moveVec.y > 0f)
                {
                    currentFacing = CharacterFacing.Up;
                    PlayFramesWithTimer(UpWalk, WalkFPS, walkAnimTimer);
                }
                else
                {
                    currentFacing = CharacterFacing.Down;
                    PlayFramesWithTimer(DownWalk, WalkFPS, walkAnimTimer);
                }
            }
            else
            {
                walkAnimTimer = 0f;
                spriteRenderer.flipX = false;

                // Khi đứng yên (Idle): Trả về sprite đứng yên chuẩn xác từ Sprite Sheet gốc
                switch (currentFacing)
                {
                    case CharacterFacing.Up:
                        spriteRenderer.sprite = UpIdle != null ? UpIdle : (UpIdleFrames != null && UpIdleFrames.Length > 0 ? UpIdleFrames[0] : null);
                        break;
                    case CharacterFacing.Left:
                        spriteRenderer.sprite = LeftIdle != null ? LeftIdle : (LeftIdleFrames != null && LeftIdleFrames.Length > 0 ? LeftIdleFrames[0] : null);
                        break;
                    case CharacterFacing.Right:
                        spriteRenderer.sprite = RightIdle != null ? RightIdle : (RightIdleFrames != null && RightIdleFrames.Length > 0 ? RightIdleFrames[0] : null);
                        break;
                    case CharacterFacing.Side:
                        spriteRenderer.sprite = SideIdle != null ? SideIdle : (SideIdleFrames != null && SideIdleFrames.Length > 0 ? SideIdleFrames[0] : null);
                        break;
                    default:
                        spriteRenderer.sprite = DownIdle != null ? DownIdle : (DownIdleFrames != null && DownIdleFrames.Length > 0 ? DownIdleFrames[0] : null);
                        break;
                }
            }
        }

        private float walkAnimTimer = 0f;

        private void PlayFramesWithTimer(Sprite[] frames, float fps, float timer)
        {
            if (frames == null || frames.Length == 0) return;
            int index = (int)(timer * fps) % frames.Length;
            if (frames[index] != null)
            {
                spriteRenderer.sprite = frames[index];
            }
        }

        private void PlayFrames(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) return;
            int index = (int)(Time.time * fps) % frames.Length;
            if (frames[index] != null)
            {
                spriteRenderer.sprite = frames[index];
            }
        }

        public void TriggerHappy(float duration = 1.0f)
        {
            happyTimer = duration;
        }

        public void TriggerAction(float duration = 0.35f)
        {
            actionTimer = duration;
            if (ActionSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = ActionSprite;
            }
        }

        /// <summary>
        /// Kích hoạt đòn tấn công: Áp dụng cơ chế vung kiếm 6 frames phân lớp động chuẩn Stardew Valley.
        /// Thân nhân vật hướng về phía mục tiêu (giữ sprite Idle nguyên bản), kiếm xoay quét 180 độ theo socket.
        /// </summary>
        public void PlayCombatAction(CombatActionType actionType, Vector2 aimDirection, float speedMultiplier, Action onHitFrame, Action onComplete, string weaponId = "kiem_sat")
        {
            // Xác định hướng tấn công (Down, Up, Left, Right)
            if (Mathf.Abs(aimDirection.x) > Mathf.Abs(aimDirection.y) * 0.8f)
            {
                currentFacing = aimDirection.x < 0f ? CharacterFacing.Left : CharacterFacing.Right;
            }
            else
            {
                currentFacing = aimDirection.y > 0f ? CharacterFacing.Up : CharacterFacing.Down;
            }

            // Đặt sprite nhân vật đứng yên theo hướng tấn công chuẩn xác
            switch (currentFacing)
            {
                case CharacterFacing.Up:    if (UpIdle != null) spriteRenderer.sprite = UpIdle; break;
                case CharacterFacing.Left:  if (LeftIdle != null) spriteRenderer.sprite = LeftIdle; break;
                case CharacterFacing.Right: if (RightIdle != null) spriteRenderer.sprite = RightIdle; break;
                default:                    if (DownIdle != null) spriteRenderer.sprite = DownIdle; break;
            }

            isAttacking = true;

            // Thực thi chuỗi hoạt họa vung kiếm kiểu Stardew Valley
            if (weaponSocketController != null)
            {
                weaponSocketController.PlaySwordSwipe(
                    currentFacing,
                    speedMultiplier,
                    onHitFrame: () => {
                        onHitFrame?.Invoke();
                    },
                    onComplete: () => {
                        isAttacking = false;
                        onComplete?.Invoke();
                    },
                    weaponId: weaponId
                );
            }
            else
            {
                isAttacking = false;
                onHitFrame?.Invoke();
                onComplete?.Invoke();
            }
        }
    }
}
