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

        [Header("Slash Attack Frames (3 frames: Wind-up, Active, Recovery)")]
        public Sprite[] SlashDown;
        public Sprite[] SlashUp;
        public Sprite[] SlashSide;

        [Header("Smash Attack Frames (3 frames)")]
        public Sprite[] SmashDown;
        public Sprite[] SmashUp;
        public Sprite[] SmashSide;

        [Header("Thrust Attack Frames (3 frames)")]
        public Sprite[] ThrustDown;
        public Sprite[] ThrustUp;
        public Sprite[] ThrustSide;

        [Header("Action Sprite (Rải cám / Chải lông / Sửa rào)")]
        public Sprite ActionSprite;

        [Header("Animation Settings")]
        public float IdleFPS = 4.0f;
        public float WalkFPS = 7.5f;

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

        private void LoadSpritesIfNeeded()
        {
            // 1. Tải Idle Breathing Frames (4 frames thở nhấp nhô & chớp mắt)
            if (DownIdleFrames == null || DownIdleFrames.Length == 0 || DownIdleFrames[0] == null)
            {
                DownIdleFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_idle_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_idle_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_idle_3.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_idle_4.png")
                };
            }

            if (SideIdleFrames == null || SideIdleFrames.Length == 0 || SideIdleFrames[0] == null)
            {
                SideIdleFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_idle_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_idle_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_idle_3.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_idle_4.png")
                };
            }

            if (UpIdleFrames == null || UpIdleFrames.Length == 0 || UpIdleFrames[0] == null)
            {
                UpIdleFrames = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_idle_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_idle_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_idle_3.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_idle_4.png")
                };
            }

            if (DownIdle == null) DownIdle = DownIdleFrames[0];
            if (SideIdle == null) SideIdle = SideIdleFrames[0];
            if (UpIdle == null) UpIdle = UpIdleFrames[0];

            // 2. Tải Walk Frames (4 frames mượt mà)
            if (DownWalk == null || DownWalk.Length == 0 || DownWalk[0] == null)
            {
                DownWalk = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_walk1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_walk2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_walk3.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_walk4.png")
                };
            }

            if (SideWalk == null || SideWalk.Length == 0 || SideWalk[0] == null)
            {
                SideWalk = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_walk1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_walk2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_walk3.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_walk4.png")
                };
            }

            if (UpWalk == null || UpWalk.Length == 0 || UpWalk[0] == null)
            {
                UpWalk = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_walk1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_walk2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_walk3.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_walk4.png")
                };
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

            // 5. Slash frames
            if (SlashDown == null || SlashDown.Length == 0 || SlashDown[0] == null)
            {
                SlashDown = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_slash_down_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_slash_down_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_slash_down_3.png")
                };
            }

            if (SlashSide == null || SlashSide.Length == 0 || SlashSide[0] == null)
            {
                SlashSide = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_slash_side_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_slash_side_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_slash_side_3.png")
                };
            }

            if (SlashUp == null || SlashUp.Length == 0 || SlashUp[0] == null)
            {
                SlashUp = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_slash_up_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_slash_up_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_slash_up_3.png")
                };
            }

            // 6. Smash frames
            if (SmashDown == null || SmashDown.Length == 0 || SmashDown[0] == null)
            {
                SmashDown = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_smash_down_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_smash_down_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_smash_down_3.png")
                };
            }

            if (SmashSide == null || SmashSide.Length == 0 || SmashSide[0] == null)
            {
                SmashSide = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_smash_side_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_smash_side_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_smash_side_3.png")
                };
            }

            if (SmashUp == null || SmashUp.Length == 0 || SmashUp[0] == null)
            {
                SmashUp = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_smash_up_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_smash_up_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_smash_up_3.png")
                };
            }

            // 7. Thrust frames
            if (ThrustDown == null || ThrustDown.Length == 0 || ThrustDown[0] == null)
            {
                ThrustDown = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_thrust_down_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_thrust_down_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_thrust_down_3.png")
                };
            }

            if (ThrustSide == null || ThrustSide.Length == 0 || ThrustSide[0] == null)
            {
                ThrustSide = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_thrust_side_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_thrust_side_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_thrust_side_3.png")
                };
            }

            if (ThrustUp == null || ThrustUp.Length == 0 || ThrustUp[0] == null)
            {
                ThrustUp = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_thrust_up_1.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_thrust_up_2.png"),
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_thrust_up_3.png")
                };
            }
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
                // Xác định hướng chiếm ưu thế
                if (Mathf.Abs(moveVec.x) > Mathf.Abs(moveVec.y) * 0.8f)
                {
                    currentFacing = CharacterFacing.Side;
                    spriteRenderer.flipX = moveVec.x < 0f;
                    PlayFrames(SideWalk, WalkFPS);
                }
                else if (moveVec.y > 0f)
                {
                    currentFacing = CharacterFacing.Up;
                    spriteRenderer.flipX = false;
                    PlayFrames(UpWalk, WalkFPS);
                }
                else
                {
                    currentFacing = CharacterFacing.Down;
                    spriteRenderer.flipX = false;
                    PlayFrames(DownWalk, WalkFPS);
                }
            }
            else
            {
                // Khi đứng yên (Idle): Phát chuỗi Idle Breathing (Thở nhấp nhô & chớp mắt nhẹ, không đơ)
                switch (currentFacing)
                {
                    case CharacterFacing.Up:
                        PlayFrames(UpIdleFrames, IdleFPS);
                        break;
                    case CharacterFacing.Side:
                        PlayFrames(SideIdleFrames, IdleFPS);
                        break;
                    default:
                        PlayFrames(DownIdleFrames, IdleFPS);
                        break;
                }
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
            if (isAttacking) return;
            StartCoroutine(CombatActionRoutine(actionType, aimDirection, speedMultiplier, onHitFrame, onComplete));
        }

        private IEnumerator CombatActionRoutine(CombatActionType actionType, Vector2 aimDirection, float speedMultiplier, Action onHitFrame, Action onComplete)
        {
            isAttacking = true;

            // 1. Xác định hướng nhìn từ Vector ngắm
            if (Mathf.Abs(aimDirection.x) > Mathf.Abs(aimDirection.y) * 0.8f)
            {
                currentFacing = CharacterFacing.Side;
                spriteRenderer.flipX = aimDirection.x < 0f;
            }
            else if (aimDirection.y > 0f)
            {
                currentFacing = CharacterFacing.Up;
                spriteRenderer.flipX = false;
            }
            else
            {
                currentFacing = CharacterFacing.Down;
                spriteRenderer.flipX = false;
            }

            // 2. Chọn mảng frame tương ứng
            Sprite[] frames = GetFramesForAction(actionType, currentFacing);
            if (frames == null || frames.Length < 3)
            {
                isAttacking = false;
                onComplete?.Invoke();
                yield break;
            }

            float windupTime = 0.07f / speedMultiplier;
            float activeTime = 0.09f / speedMultiplier;
            float recoveryTime = 0.06f / speedMultiplier;

            // Phase 1: Wind-up (Lấy đà)
            if (frames[0] != null) spriteRenderer.sprite = frames[0];
            yield return new WaitForSeconds(windupTime);

            // Phase 2: Active (Vung đòn + Hitbox + VFX)
            if (frames[1] != null) spriteRenderer.sprite = frames[1];
            onHitFrame?.Invoke();
            yield return new WaitForSeconds(activeTime);

            // Phase 3: Recovery (Thu đòn)
            if (frames[2] != null) spriteRenderer.sprite = frames[2];
            yield return new WaitForSeconds(recoveryTime);

            // Hoàn tất đòn đánh
            isAttacking = false;
            onComplete?.Invoke();
        }

        private Sprite[] GetFramesForAction(CombatActionType actionType, CharacterFacing facing)
        {
            switch (actionType)
            {
                case CombatActionType.Smash:
                    return facing switch
                    {
                        CharacterFacing.Up => SmashUp,
                        CharacterFacing.Side => SmashSide,
                        _ => SmashDown
                    };
                case CombatActionType.Thrust:
                    return facing switch
                    {
                        CharacterFacing.Up => ThrustUp,
                        CharacterFacing.Side => ThrustSide,
                        _ => ThrustDown
                    };
                case CombatActionType.Slash:
                default:
                    return facing switch
                    {
                        CharacterFacing.Up => SlashUp,
                        CharacterFacing.Side => SlashSide,
                        _ => SlashDown
                    };
            }
        }
    }
}
