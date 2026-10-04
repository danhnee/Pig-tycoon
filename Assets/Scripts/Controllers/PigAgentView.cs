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
        public float WanderRadius = 3.5f;

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

            // 2. Mobile LOD Culling trong 2D
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

            // Nếu đang đứng sát người chơi và không hoảng sợ -> Dừng lại ủn ỉn, hướng mắt về người chơi
            Transform playerTr = GetPlayerTransform();
            if (playerTr != null && PigModel.MoodState != MoodState.HoangLoan && PigModel.MoodState != MoodState.HoangSo)
            {
                float distToPlayer = Vector2.Distance(currentPos, playerTr.position);
                if (distToPlayer < 1.35f)
                {
                    rb.linearVelocity = Vector2.zero;
                    if (SpriteRenderer != null)
                    {
                        float dx = playerTr.position.x - currentPos.x;
                        if (Mathf.Abs(dx) > 0.05f)
                        {
                            SpriteRenderer.flipX = dx < 0f;
                        }
                    }
                    return;
                }
            }

            if (toTarget.sqrMagnitude > 0.15f)
            {
                float currentSpeed = (PigModel.MoodState == MoodState.HoangLoan || PigModel.MoodState == MoodState.HoangSo)
                    ? PanicSpeed
                    : WalkSpeed;

                Vector2 moveVelocity = toTarget.normalized * currentSpeed;
                rb.linearVelocity = moveVelocity;

                // Lật sprite theo hướng di chuyển
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

            wanderTimer = Random.Range(3.0f, 5.5f);

            var env = FarmEnvironment2D.Instance;
            var engine = MobileGameController.Instance?.Engine;
            var weather = engine?.CurrentWeather ?? WeatherType.QuangDang;
            var timeOfDay = engine?.Clock?.GetTimeOfDay() ?? TimeOfDay.Sang;

            Transform playerTr = GetPlayerTransform();
            var activeTool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;

            // 1. Phản xạ thân thiện với Người Chơi (Stardew Valley Pet & Friendly AI)
            if (playerTr != null)
            {
                float distToPlayer = Vector2.Distance(transform.position, playerTr.position);

                // 1.1. Người chơi đang cầm Bao Cám (🌾) -> Mùi thức ăn thu hút heo chạy lại gần đòi ăn!
                if (activeTool == StardewToolType.CamHat && distToPlayer < 8.0f)
                {
                    Vector2 approachOffset = Random.insideUnitCircle.normalized * Random.Range(1.2f, 1.8f);
                    targetPosition = (Vector2)playerTr.position + approachOffset;
                    if (env != null) targetPosition = env.ClampInsideFarm(targetPosition);
                    return;
                }

                // 1.2. Người chơi đang cầm Bàn Chải (❤️) -> Heo thích gãi lưng, chủ động tiến lại gần!
                if (activeTool == StardewToolType.BanChai && distToPlayer < 6.0f)
                {
                    Vector2 approachOffset = Random.insideUnitCircle.normalized * Random.Range(1.0f, 1.5f);
                    targetPosition = (Vector2)playerTr.position + approachOffset;
                    if (env != null) targetPosition = env.ClampInsideFarm(targetPosition);
                    return;
                }

                // 1.3. Heo thân thiết (Bonding >= 30, mặc định ban đầu là 40-60)
                // Khi người chơi ở gần (< 4.5m), có 65% xác suất heo tò mò bước lại gần người chơi ủn ỉn!
                if (distToPlayer < 4.5f && PigModel.Bonding >= 30f && Random.value < 0.65f)
                {
                    Vector2 friendlyOffset = Random.insideUnitCircle.normalized * Random.Range(1.2f, 2.0f);
                    targetPosition = (Vector2)playerTr.position + friendlyOffset;
                    if (env != null) targetPosition = env.ClampInsideFarm(targetPosition);
                    return;
                }
            }

            // 2. Nếu Mưa, Giông Bão, Rét Đậm hoặc Đêm tối -> Trốn vào Mái trú (Shelter)
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

            // 3. Nếu Nắng Gắt hoặc là giống Lam Khê (ưa nước bùn) -> Tìm bãi bùn tắm mát
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

            // 4. Nếu Buổi Trưa (khát nước) -> Tìm bồn nước
            if (timeOfDay == TimeOfDay.Trua && env != null && Random.value < 0.40f)
            {
                var water = env.GetNearestWaterTrough(transform.position);
                if (water != null)
                {
                    targetPosition = (Vector2)water.transform.position + (Random.insideUnitCircle * 0.8f);
                    return;
                }
            }

            // 5. Tìm máng ăn định kỳ
            if (env != null && Random.value < 0.25f)
            {
                var feeder = env.GetNearestFeeder(transform.position);
                if (feeder != null)
                {
                    targetPosition = (Vector2)feeder.transform.position + (Random.insideUnitCircle * 0.8f);
                    return;
                }
            }

            // 6. Đi dạo ấm cúng xung quanh vị trí hiện tại
            Vector2 randomOffset = Random.insideUnitCircle * WanderRadius;
            targetPosition = (Vector2)transform.position + randomOffset;

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
                Vector2 randomOffset = Random.insideUnitCircle * (WanderRadius * 1.8f);
                targetPosition = (Vector2)transform.position + randomOffset;

                var env = FarmEnvironment2D.Instance;
                if (env != null)
                {
                    targetPosition = env.ClampInsideFarm(targetPosition, 0.5f);
                }
            }
        }

        private Transform GetPlayerTransform()
        {
            if (leaderTransform != null) return leaderTransform;

            var player = GameObject.FindWithTag("Player");
            if (player != null) return player.transform;

            var pCtrl = FindAnyObjectByType<PlayerMobileController>();
            if (pCtrl != null) return pCtrl.transform;

            return null;
        }
    }
}
