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
        [Header("Idle Sprites")]
        public Sprite DownIdle;
        public Sprite UpIdle;
        public Sprite SideIdle;

        [Header("Walk Animation Frames (3 frames each)")]
        public Sprite[] DownWalk;
        public Sprite[] UpWalk;
        public Sprite[] SideWalk;

        [Header("Ready Stance (Holding Weapon)")]
        public Sprite ReadyDown;
        public Sprite ReadyUp;
        public Sprite ReadySide;

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
        public float WalkFPS = 8.0f;

        private SpriteRenderer spriteRenderer;
        private PlayerMobileController playerController;
        private Rigidbody2D rb;
        private CharacterFacing currentFacing = CharacterFacing.Down;
        private float actionTimer = 0f;
        private bool isAttacking = false;
        private bool isHoldingWeapon = false;

        public CharacterFacing CurrentFacing => currentFacing;
        public bool IsAttacking => isAttacking;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerController = GetComponent<PlayerMobileController>();
            rb = GetComponent<Rigidbody2D>();
            LoadSpritesIfNeeded();
        }

        public void SetHoldingWeapon(bool holding)
        {
            isHoldingWeapon = holding;
        }

        private void LoadSpritesIfNeeded()
        {
            // Tải sprites cơ bản nếu chưa gán
            if (DownIdle == null) DownIdle = UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_idle.png");
            if (UpIdle == null) UpIdle = UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_idle.png");
            if (SideIdle == null) SideIdle = UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_idle.png");

            if (ActionSprite == null) ActionSprite = UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_action.png");

            // Walk frames
            if (DownWalk == null || DownWalk.Length == 0 || DownWalk[0] == null)
            {
                DownWalk = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_walk1.png"),
                    DownIdle,
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_down_walk2.png")
                };
            }

            if (SideWalk == null || SideWalk.Length == 0 || SideWalk[0] == null)
            {
                SideWalk = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_walk1.png"),
                    SideIdle,
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_side_walk2.png")
                };
            }

            if (UpWalk == null || UpWalk.Length == 0 || UpWalk[0] == null)
            {
                UpWalk = new Sprite[]
                {
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_walk1.png"),
                    UpIdle,
                    UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_up_walk2.png")
                };
            }

            // Ready stances
            if (ReadyDown == null) ReadyDown = UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_ready_down.png");
            if (ReadyUp == null) ReadyUp = UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_ready_up.png");
            if (ReadySide == null) ReadySide = UISpriteLoader.LoadSprite("Art/Sprites/Characters/player_ready_side.png");

            // Slash frames
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

            // Smash frames
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

            // Thrust frames
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

            // Nếu đang trong trạng thái tấn công hoặc animation hành động, bỏ qua update di chuyển thông thường
            if (isAttacking) return;

            if (actionTimer > 0f)
            {
                actionTimer -= Time.deltaTime;
                if (ActionSprite != null)
                {
                    spriteRenderer.sprite = ActionSprite;
                    return;
                }
            }

            // 2. Lấy hướng di chuyển từ Controller hoặc Rigidbody
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
                    PlayFrames(SideWalk);
                }
                else if (moveVec.y > 0f)
                {
                    currentFacing = CharacterFacing.Up;
                    spriteRenderer.flipX = false;
                    PlayFrames(UpWalk);
                }
                else
                {
                    currentFacing = CharacterFacing.Down;
                    spriteRenderer.flipX = false;
                    PlayFrames(DownWalk);
                }
            }
            else
            {
                // Trả về Idle hoặc Ready Stance của hướng nhìn gần nhất
                if (isHoldingWeapon)
                {
                    switch (currentFacing)
                    {
                        case CharacterFacing.Up:
                            if (ReadyUp != null) spriteRenderer.sprite = ReadyUp;
                            break;
                        case CharacterFacing.Side:
                            if (ReadySide != null) spriteRenderer.sprite = ReadySide;
                            break;
                        default:
                            if (ReadyDown != null) spriteRenderer.sprite = ReadyDown;
                            break;
                    }
                }
                else
                {
                    switch (currentFacing)
                    {
                        case CharacterFacing.Up:
                            if (UpIdle != null) spriteRenderer.sprite = UpIdle;
                            break;
                        case CharacterFacing.Side:
                            if (SideIdle != null) spriteRenderer.sprite = SideIdle;
                            break;
                        default:
                            if (DownIdle != null) spriteRenderer.sprite = DownIdle;
                            break;
                    }
                }
            }
        }

        private void PlayFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0) return;
            int index = (int)(Time.time * WalkFPS) % frames.Length;
            if (frames[index] != null)
            {
                spriteRenderer.sprite = frames[index];
            }
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

            // Phase 1: Wind-up
            if (frames[0] != null) spriteRenderer.sprite = frames[0];
            yield return new WaitForSeconds(windupTime);

            // Phase 2: Active (Điểm chạm Hitbox)
            if (frames[1] != null) spriteRenderer.sprite = frames[1];
            onHitFrame?.Invoke();
            yield return new WaitForSeconds(activeTime);

            // Phase 3: Recovery
            if (frames[2] != null) spriteRenderer.sprite = frames[2];
            yield return new WaitForSeconds(recoveryTime);

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
