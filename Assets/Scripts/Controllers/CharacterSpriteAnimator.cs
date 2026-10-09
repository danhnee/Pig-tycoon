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
        Side
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterSpriteAnimator : MonoBehaviour
    {
        [Header("Idle Breathing Animation Frames (4 frames each - Không bị đơ)")]
        public Sprite[] DownIdleFrames;
        public Sprite[] UpIdleFrames;
        public Sprite[] SideIdleFrames;

        [Header("Legacy Single Idle Fallbacks")]
        public Sprite DownIdle;
        public Sprite UpIdle;
        public Sprite SideIdle;

        [Header("Walk Animation Frames (4 frames each)")]
        public Sprite[] DownWalk;
        public Sprite[] UpWalk;
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

            SideIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_idle_4.png")
            };

            UpIdleFrames = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_idle_1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_idle_2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_idle_3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_idle_4.png")
            };

            DownIdle = DownIdleFrames[0];
            SideIdle = SideIdleFrames[0];
            UpIdle = UpIdleFrames[0];

            DownWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}down_walk4.png")
            };

            SideWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}side_walk4.png")
            };

            UpWalk = new Sprite[]
            {
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_walk1.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_walk2.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_walk3.png"),
                UISpriteLoader.LoadSprite($"Art/Sprites/Characters/{prefix}up_walk4.png")
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

                // Xác định hướng chiếm ưu thế
                if (Mathf.Abs(moveVec.x) > Mathf.Abs(moveVec.y) * 0.8f)
                {
                    currentFacing = CharacterFacing.Side;
                    spriteRenderer.flipX = moveVec.x < 0f;
                    PlayFramesWithTimer(SideWalk, WalkFPS, walkAnimTimer);
                }
                else if (moveVec.y > 0f)
                {
                    currentFacing = CharacterFacing.Up;
                    spriteRenderer.flipX = false;
                    PlayFramesWithTimer(UpWalk, WalkFPS, walkAnimTimer);
                }
                else
                {
                    currentFacing = CharacterFacing.Down;
                    spriteRenderer.flipX = false;
                    PlayFramesWithTimer(DownWalk, WalkFPS, walkAnimTimer);
                }
            }
            else
            {
                walkAnimTimer = 0f;

                // Khi đứng yên (Idle): Trả về sprite đứng yên chuẩn xác từ Sprite Sheet gốc
                switch (currentFacing)
                {
                    case CharacterFacing.Up:
                        spriteRenderer.sprite = UpIdle != null ? UpIdle : (UpIdleFrames != null && UpIdleFrames.Length > 0 ? UpIdleFrames[0] : null);
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

        public void PlayCombatAction(CombatActionType actionType, Vector2 aimDirection, float speedMultiplier, Action onHitFrame, Action onComplete)
        {
            // Không chuyển đổi sprite tấn công; thực thi đòn đánh và kích hoạt hitbox trực tiếp
            onHitFrame?.Invoke();
            onComplete?.Invoke();
        }
    }
}
