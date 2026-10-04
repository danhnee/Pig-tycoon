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

            // 1. Kiểm tra trạng thái ngủ (Buổi tối hoặc CurrentActivity == Sleeping)
            bool isSleeping = agentView != null && agentView.CurrentActivity == PigActivityState.Sleeping;
            if (!isSleeping)
            {
                var engine = MobileGameController.Instance?.Engine;
                if (engine != null && engine.Clock != null)
                {
                    var timeOfDay = engine.Clock.GetTimeOfDay();
                    if (timeOfDay == TimeOfDay.Toi)
                    {
                        isSleeping = true;
                    }
                }
            }

            if (isSleeping && SleepSprite != null)
            {
                spriteRenderer.sprite = SleepSprite;
                return;
            }

            // 2. Kiểm tra trạng thái ăn / uống (CurrentActivity == Eating hoặc Drinking)
            bool isEating = agentView != null && (agentView.CurrentActivity == PigActivityState.Eating || agentView.CurrentActivity == PigActivityState.Drinking);
            if (!isEating && FarmEnvironment2D.Instance != null)
            {
                var nearestFeeder = FarmEnvironment2D.Instance.GetNearestFeeder(transform.position, false);
                if (nearestFeeder != null && Vector2.Distance(transform.position, nearestFeeder.transform.position) < 1.4f)
                {
                    isEating = true;
                }
            }

            if (isEating && EatSprite != null)
            {
                spriteRenderer.sprite = EatSprite;
                return;
            }

            // 3. Di chuyển / Chạy lon ton (Walk / Trot)
            bool isMoving = rb != null && rb.linearVelocity.sqrMagnitude > 0.05f;
            if (isMoving && WalkSprites != null && WalkSprites.Length > 0)
            {
                int index = (int)(Time.time * TrotFPS) % WalkSprites.Length;
                if (WalkSprites[index] != null)
                {
                    spriteRenderer.sprite = WalkSprites[index];
                }

                // Lật sprite theo hướng vận tốc
                if (Mathf.Abs(rb.linearVelocity.x) > 0.05f)
                {
                    spriteRenderer.flipX = rb.linearVelocity.x < 0f;
                }
            }
            else
            {
                // 4. Trạng thái đứng dạo chơi / Nhìn ngó (Idle)
                if (IdleSprite != null)
                {
                    spriteRenderer.sprite = IdleSprite;
                }
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
