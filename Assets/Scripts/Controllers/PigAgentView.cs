using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PigAgentView : MonoBehaviour
    {
        public Pig PigModel { get; private set; }

        [Header("2D Visual & Sorting")]
        public SpriteRenderer SpriteRenderer;
        public bool AutoDynamicSortingOrder = true;
        public int SortingPrecision = 100;

        [Header("2D Movement & Wander")]
        public float WalkSpeed = 1.6f;
        public float PanicSpeed = 3.6f;
        public float WanderRadius = 8f;

        private Rigidbody2D rb;
        private Vector2 targetPosition;
        private float wanderTimer = 0f;
        private Camera mainCamera;
        private Transform leaderTransform;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public void Bind(Pig model, Transform leader = null)
        {
            PigModel = model;
            targetPosition = rb != null ? rb.position : (Vector2)transform.position;
            mainCamera = Camera.main;
            leaderTransform = leader;
        }

        private void Update()
        {
            if (PigModel == null || !PigModel.IsAlive) return;

            // 1. Dynamic Y-sorting trong Universal 2D (Heo ở dưới màn hình vẽ đè lên heo ở trên)
            if (AutoDynamicSortingOrder && SpriteRenderer != null)
            {
                SpriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * SortingPrecision);
            }

            // 2. Mobile LOD Culling trong 2D: Nếu quá xa camera thì giảm tần suất tính toán
            if (mainCamera != null)
            {
                float distSqr = ((Vector2)transform.position - (Vector2)mainCamera.transform.position).sqrMagnitude;
                if (distSqr > 1200f) // > 35m
                {
                    return;
                }
            }

            // 3. Xử lý di chuyển theo trạng thái Tinh Thần
            if (PigModel.MoodState == MoodState.HoangLoan || PigModel.MoodState == MoodState.HoangSo)
            {
                HandlePanicMovement2D();
            }
            else
            {
                HandlePeacefulWander2D();
            }
        }

        private void FixedUpdate()
        {
            if (rb == null || PigModel == null || !PigModel.IsAlive) return;

            Vector2 currentPos = rb.position;
            Vector2 toTarget = targetPosition - currentPos;

            if (toTarget.sqrMagnitude > 0.1f)
            {
                float currentSpeed = (PigModel.MoodState == MoodState.HoangLoan || PigModel.MoodState == MoodState.HoangSo)
                    ? PanicSpeed
                    : WalkSpeed;

                Vector2 moveVelocity = toTarget.normalized * currentSpeed;
                rb.velocity = moveVelocity;

                // Lật sprite theo hướng nhìn
                if (SpriteRenderer != null && Mathf.Abs(moveVelocity.x) > 0.05f)
                {
                    SpriteRenderer.flipX = moveVelocity.x < 0f;
                }
            }
            else
            {
                rb.velocity = Vector2.zero;
            }
        }

        private void HandlePeacefulWander2D()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(3f, 7f);

                // Nếu có thủ lĩnh và heo có Độ bám đàn cao (> 70) -> hướng về thủ lĩnh
                if (leaderTransform != null && PigModel.AdhesionScore > 70f && Random.value < 0.65f)
                {
                    Vector2 leaderPos = leaderTransform.position;
                    targetPosition = leaderPos + (Random.insideUnitCircle * 4f);
                }
                else
                {
                    Vector2 randomOffset = Random.insideUnitCircle * WanderRadius;
                    targetPosition = (Vector2)transform.position + randomOffset;
                }
            }
        }

        private void HandlePanicMovement2D()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(0.8f, 2f);
                Vector2 randomOffset = Random.insideUnitCircle * (WanderRadius * 1.5f);
                targetPosition = (Vector2)transform.position + randomOffset;
            }
        }
    }
}
