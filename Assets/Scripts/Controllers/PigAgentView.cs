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
        public float WanderRadius = 6f;

        [Header("Infrastructure State")]
        public bool IsInMudPit { get; private set; }
        public bool IsInShelter { get; private set; }

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

        public void SetInMudPit(bool inMud)
        {
            IsInMudPit = inMud;
            if (inMud && SpriteRenderer != null)
            {
                // Bám bùn: sẫm màu nhẹ
                SpriteRenderer.color = Color.Lerp(SpriteRenderer.color, new Color(0.55f, 0.45f, 0.35f), 0.35f);
            }
        }

        public void SetInShelter(bool inShelter)
        {
            IsInShelter = inShelter;
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

            // 3. Xử lý di chuyển theo trạng thái Tinh Thần & Nhu cầu Hạ Tầng
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

            if (toTarget.sqrMagnitude > 0.15f)
            {
                float currentSpeed = (PigModel.MoodState == MoodState.HoangLoan || PigModel.MoodState == MoodState.HoangSo)
                    ? PanicSpeed
                    : WalkSpeed;

                Vector2 moveVelocity = toTarget.normalized * currentSpeed;
                rb.linearVelocity = moveVelocity;

                // Lật sprite theo hướng nhìn
                if (SpriteRenderer != null && Mathf.Abs(moveVelocity.x) > 0.05f)
                {
                    SpriteRenderer.flipX = moveVelocity.x < 0f;
                }
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        private void HandlePeacefulWander2D()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer > 0f) return;

            wanderTimer = Random.Range(3.5f, 6.5f);

            var env = FarmEnvironment2D.Instance;
            var engine = MobileGameController.Instance?.Engine;
            var weather = engine?.Clock?.CurrentWeather ?? WeatherType.QuangDang;
            var timeOfDay = engine?.Clock?.CurrentTimeOfDay ?? TimeOfDay.Sang;

            // 1. Nếu Mưa, Giông Bão, Rét Đậm hoặc Đêm tối -> Có khuynh hướng trốn vào Mái trú (Shelter)
            bool needsShelter = weather == WeatherType.Mua || weather == WeatherType.GiongBao || weather == WeatherType.RetDam || timeOfDay == TimeOfDay.Toi;
            if (needsShelter && env != null && Random.value < 0.70f)
            {
                var shelter = env.GetNearestShelter(transform.position);
                if (shelter != null)
                {
                    targetPosition = (Vector2)shelter.transform.position + (Random.insideUnitCircle * 1.5f);
                    return;
                }
            }

            // 2. Nếu Nắng Gắt hoặc là giống Lam Khê (ưa nước bùn) -> Tìm bãi bùn tắm mát
            bool seeksMud = weather == WeatherType.NangGat || (PigModel != null && PigModel.GeneLine == GeneLineId.LamKhe);
            if (seeksMud && env != null && Random.value < 0.60f)
            {
                var mudPit = env.GetNearestMudPit(transform.position);
                if (mudPit != null)
                {
                    targetPosition = (Vector2)mudPit.transform.position + (Random.insideUnitCircle * 1.2f);
                    return;
                }
            }

            // 3. Nếu Buổi Trưa (khát nước) -> Tìm bồn nước
            if (timeOfDay == TimeOfDay.Trua && env != null && Random.value < 0.40f)
            {
                var water = env.GetNearestWaterTrough(transform.position);
                if (water != null)
                {
                    targetPosition = (Vector2)water.transform.position + (Random.insideUnitCircle * 0.8f);
                    return;
                }
            }

            // 4. Tìm máng ăn định kỳ
            if (env != null && Random.value < 0.25f)
            {
                var feeder = env.GetNearestFeeder(transform.position);
                if (feeder != null)
                {
                    targetPosition = (Vector2)feeder.transform.position + (Random.insideUnitCircle * 0.8f);
                    return;
                }
            }

            // 5. Nếu có thủ lĩnh và heo có Độ bám đàn cao (> 70) -> hướng về thủ lĩnh
            if (leaderTransform != null && PigModel.AdhesionScore > 70f && Random.value < 0.65f)
            {
                Vector2 leaderPos = leaderTransform.position;
                targetPosition = leaderPos + (Random.insideUnitCircle * 3.5f);
            }
            else
            {
                Vector2 randomOffset = Random.insideUnitCircle * WanderRadius;
                targetPosition = (Vector2)transform.position + randomOffset;
            }

            // Giới hạn trong khuôn viên trang trại
            if (env != null)
            {
                targetPosition = env.ClampInsideFarm(targetPosition);
            }
        }

        private void HandlePanicMovement2D()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(0.6f, 1.5f);
                Vector2 randomOffset = Random.insideUnitCircle * (WanderRadius * 1.5f);
                targetPosition = (Vector2)transform.position + randomOffset;

                var env = FarmEnvironment2D.Instance;
                if (env != null)
                {
                    targetPosition = env.ClampInsideFarm(targetPosition, 0.5f);
                }
            }
        }
    }
}
