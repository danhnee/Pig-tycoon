using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
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

        [Header("Action Sprite (Rải cám / Chải lông / Sửa rào)")]
        public Sprite ActionSprite;

        [Header("Animation Settings")]
        public float WalkFPS = 8.0f;

        private SpriteRenderer spriteRenderer;
        private PlayerMobileController playerController;
        private Rigidbody2D rb;
        private CardinalDirection currentDirection = CardinalDirection.South;
        private float actionTimer = 0f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerController = GetComponent<PlayerMobileController>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (spriteRenderer == null) return;

            // 1. Nếu đang phát Action Animation
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

            currentDirection = CardinalFacing.Resolve(moveVec.x, moveVec.y, currentDirection);
            bool isMoving = moveVec.sqrMagnitude > CardinalFacing.MoveEpsilonSqr;
            spriteRenderer.flipX = CardinalFacing.MirrorSideSprite(currentDirection);

            if (isMoving)
            {
                PlayFrames(FramesFor(currentDirection));
                return;
            }

            Sprite idle = IdleFor(currentDirection);
            if (idle != null)
            {
                spriteRenderer.sprite = idle;
            }
        }

        private Sprite[] FramesFor(CardinalDirection direction)
        {
            if (direction == CardinalDirection.North)
            {
                return UpWalk;
            }

            if (direction == CardinalDirection.East || direction == CardinalDirection.West)
            {
                return SideWalk;
            }

            return DownWalk;
        }

        private Sprite IdleFor(CardinalDirection direction)
        {
            if (direction == CardinalDirection.North)
            {
                return UpIdle;
            }

            if (direction == CardinalDirection.East || direction == CardinalDirection.West)
            {
                return SideIdle;
            }

            return DownIdle;
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
    }
}
