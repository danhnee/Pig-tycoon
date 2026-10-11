using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PigSpriteAnimator : MonoBehaviour
    {
        [Header("Animation Sprites")]
        public Sprite IdleSprite;
        public Sprite[] WalkSprites;
        public Sprite EatSprite;
        public Sprite SleepSprite;

        [Header("Settings")]
        public float TrotFPS = 6.0f;

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private PigAgentView agentView;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            agentView = GetComponent<PigAgentView>();
        }

        private void Update()
        {
            if (spriteRenderer == null) return;

            // 1. Trạng thái ngủ (Chỉ khi đã tới chuồng và đang nằm ngủ say sưa)
            bool isSleeping = agentView != null && agentView.CurrentActivity == PigActivityState.Sleeping;
            if (isSleeping && SleepSprite != null)
            {
                spriteRenderer.sprite = SleepSprite;
                return;
            }

            // 2. Di chuyển / Chạy lon ton (Walk / Trot) - Ưu tiên kiểm tra di chuyển trước
            bool isMoving = rb != null && rb.linearVelocity.sqrMagnitude > 0.04f;
            if (isMoving && WalkSprites != null && WalkSprites.Length > 0)
            {
                int index = (int)(Time.time * TrotFPS) % WalkSprites.Length;
                if (WalkSprites[index] != null)
                {
                    spriteRenderer.sprite = WalkSprites[index];
                }

                // Lật sprite theo hướng vận tốc X
                if (Mathf.Abs(rb.linearVelocity.x) > 0.05f)
                {
                    spriteRenderer.flipX = rb.linearVelocity.x < 0f;
                }
                return;
            }

            // 3. Trạng thái ăn / uống tại chỗ (khi đã dừng lại hoàn toàn)
            bool isEating = agentView != null && (agentView.CurrentActivity == PigActivityState.Eating || agentView.CurrentActivity == PigActivityState.Drinking);
            if (isEating && EatSprite != null)
            {
                spriteRenderer.sprite = EatSprite;
                return;
            }

            // 4. Trạng thái đứng dạo chơi / Nhìn ngó (Idle)
            if (IdleSprite != null)
            {
                spriteRenderer.sprite = IdleSprite;
            }
        }

        public void SetBreedSprites(Sprite idle, Sprite[] walk, Sprite eat, Sprite sleep)
        {
            IdleSprite = idle;
            WalkSprites = walk;
            EatSprite = eat;
            SleepSprite = sleep;

            if (spriteRenderer != null && IdleSprite != null)
            {
                spriteRenderer.sprite = IdleSprite;
            }
        }
    }
}
