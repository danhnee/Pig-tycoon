using UnityEngine;

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

        [Header("Action Sprite (Rải cám / Chải lông / Sửa rào)")]
        public Sprite ActionSprite;

        [Header("Animation Settings")]
        public float WalkFPS = 8.0f;

        private SpriteRenderer spriteRenderer;
        private PlayerMobileController playerController;
        private Rigidbody2D rb;
        private CharacterFacing currentFacing = CharacterFacing.Down;
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
                // Trả về Idle của hướng nhìn gần nhất
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
