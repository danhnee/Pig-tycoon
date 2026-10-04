using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public enum PigActivityState
    {
        Idling,          // Đứng nghỉ, ngắm cảnh, gặm cỏ
        Wandering,       // Đi dạo thong thả đến điểm mới
        SeekingFood,     // Đi đến máng ăn
        Eating,          // Đang đứng ăn cám tại máng
        SeekingWater,    // Đi đến bồn nước
        Drinking,        // Đang cúi đầu uống nước
        SeekingMud,      // Đi đến bãi bùn
        Bathing,         // Đang tắm mát trong bãi bùn
        SeekingShelter,  // Đi về mái hiên chuồng trú mưa/tối
        Sleeping,        // Đang nằm ngủ say sưa
        FollowingPlayer  // Tò mò chạy lại gần người chơi
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class PigAgentView : MonoBehaviour
    {
        public Pig PigModel { get; private set; }

        [Header("Activity State")]
        public PigActivityState CurrentActivity = PigActivityState.Idling;

        [Header("2D Visual & Sorting")]
        public SpriteRenderer SpriteRenderer;
        public bool AutoDynamicSortingOrder = true;
        public int SortingPrecision = 100;

        [Header("2D Movement & Wander")]
        public float WalkSpeed = 1.4f;
        public float PanicSpeed = 3.2f;
        public float WanderRadius = 4.0f;

        [Header("Infrastructure State")]
        public bool IsInMudPit { get; private set; }
        public bool IsInShelter { get; private set; }

        private Rigidbody2D rb;
        private Vector2 targetPosition;
        private float activityTimer = 0f;
        private Camera mainCamera;
        private Transform leaderTransform;
        private float stuckTimer = 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.offset = new Vector2(0f, -0.15f);
                col.radius = 0.32f;
            }

            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            targetPosition = rb != null ? rb.position : (Vector2)transform.position;
        }

        private void Start()
        {
            if (targetPosition == Vector2.zero)
            {
                targetPosition = rb != null ? rb.position : (Vector2)transform.position;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (leaderTransform == null)
            {
                leaderTransform = GetPlayerTransform();
            }

            EnsurePigModel();

            // Khởi tạo timer ngẫu nhiên để các chú heo không làm cùng 1 hành động cùng lúc
            activityTimer = Random.Range(1.0f, 3.5f);
            CurrentActivity = PigActivityState.Idling;
        }

        public void Bind(Pig model, Transform leader = null)
        {
            PigModel = model;
            targetPosition = rb != null ? rb.position : (Vector2)transform.position;
            mainCamera = Camera.main;
            leaderTransform = leader != null ? leader : GetPlayerTransform();
        }

        public void EnsurePigModel()
        {
            if (PigModel != null) return;

            var engine = MobileGameController.Instance?.Engine;
            // 1. Tìm trong Engine.Pigs xem đã có heo nào khớp tên/ID với GameObject này không
            if (engine != null && engine.Pigs != null)
            {
                foreach (var p in engine.Pigs)
                {
                    if (gameObject.name.Contains(p.Name) || gameObject.name.Contains(p.Id))
                    {
                        PigModel = p;
                        return;
                    }
                }
            }

            // 2. Nhận diện giống loài từ tên GameObject
            GeneLineId gene = GeneLineId.HongDien;
            string pigName = "Hồng Điền";

            if (gameObject.name.Contains("Hồng Điền") || gameObject.name.Contains("HongDien"))
            {
                gene = GeneLineId.HongDien;
                pigName = "Hồng Điền";
            }
            else if (gameObject.name.Contains("Lam Khê") || gameObject.name.Contains("LamKhe"))
            {
                gene = GeneLineId.LamKhe;
                pigName = "Lam Khê";
            }
            else if (gameObject.name.Contains("Kim Thọ") || gameObject.name.Contains("KimTho"))
            {
                gene = GeneLineId.KimTho;
                pigName = "Kim Thọ";
            }
            else if (gameObject.name.Contains("Hư Thể") || gameObject.name.Contains("HuThe"))
            {
                gene = GeneLineId.HuThe;
                pigName = "Hư Thể";
            }
            else if (gameObject.name.Contains("Xích Mao") || gameObject.name.Contains("XichMao"))
            {
                gene = GeneLineId.XichMao;
                pigName = "Xích Mao";
            }

            string id = $"pig_{Mathf.Abs(gameObject.GetInstanceID())}";
            PigModel = Pig.CreateDefault(id, pigName, gene, GeneRarity.Thuong);
            PigModel.Stage = PigStage.TruongThanh;
            PigModel.WeightKg = Random.Range(85f, 115f);
            PigModel.Bonding = Random.Range(45f, 65f);

            if (engine != null && engine.Pigs != null && !engine.Pigs.Contains(PigModel))
            {
                engine.Pigs.Add(PigModel);
            }
        }

        public void SetInMudPit(bool inMud)
        {
            IsInMudPit = inMud;
            if (inMud && SpriteRenderer != null)
            {
                SpriteRenderer.color = Color.Lerp(SpriteRenderer.color, new Color(0.55f, 0.45f, 0.35f), 0.35f);
            }
        }

        public void SetInShelter(bool inShelter)
        {
            IsInShelter = inShelter;
        }

        private void Update()
        {
            if (PigModel == null)
            {
                EnsurePigModel();
                if (PigModel == null) return;
            }

            if (!PigModel.IsAlive) return;

            // 1. Dynamic Y-sorting trong Universal 2D
            if (AutoDynamicSortingOrder && SpriteRenderer != null)
            {
                SpriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * SortingPrecision);
            }

            // 2. Mobile LOD Culling trong 2D
            if (mainCamera != null)
            {
                float distSqr = ((Vector2)transform.position - (Vector2)mainCamera.transform.position).sqrMagnitude;
                if (distSqr > 1600f) // > 40m
                {
                    return;
                }
            }

            // 3. Xử lý di chuyển theo trạng thái Tinh Thần & Hoạt động thường nhật
            if (PigModel.MoodState == MoodState.HoangLoan || PigModel.MoodState == MoodState.HoangSo)
            {
                HandlePanicMovement2D();
            }
            else
            {
                HandleRoutineActivities2D();
            }
        }

        private void FixedUpdate()
        {
            if (rb == null || PigModel == null || !PigModel.IsAlive) return;

            // Nếu đang trong trạng thái đứng yên -> Triệt tiêu vận tốc
            if (CurrentActivity == PigActivityState.Idling ||
                CurrentActivity == PigActivityState.Eating ||
                CurrentActivity == PigActivityState.Drinking ||
                CurrentActivity == PigActivityState.Bathing ||
                CurrentActivity == PigActivityState.Sleeping)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Nếu đang di chuyển
            Vector2 currentPos = rb.position;
            Vector2 toTarget = targetPosition - currentPos;

            // Nếu đang đi theo người chơi và đã đứng đủ gần (< 1.4m) -> Dừng lại ủn ỉn, nhìn người chơi
            if (CurrentActivity == PigActivityState.FollowingPlayer)
            {
                Transform playerTr = GetPlayerTransform();
                if (playerTr != null && Vector2.Distance(currentPos, playerTr.position) < 1.4f)
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

            if (toTarget.sqrMagnitude > 0.12f)
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

        private void HandleRoutineActivities2D()
        {
            activityTimer -= Time.deltaTime;

            var env = FarmEnvironment2D.Instance;
            var engine = MobileGameController.Instance?.Engine;
            var weather = engine?.CurrentWeather ?? WeatherType.QuangDang;
            var timeOfDay = engine?.Clock?.GetTimeOfDay() ?? TimeOfDay.Sang;

            // Kiểm tra điều kiện thời tiết đặc biệt để chuyển sang trú ẩn / ngủ
            bool isNightOrStorm = timeOfDay == TimeOfDay.Toi || weather == WeatherType.GiongBao || weather == WeatherType.Mua || weather == WeatherType.RetDam;
            if (isNightOrStorm && CurrentActivity != PigActivityState.Sleeping && CurrentActivity != PigActivityState.SeekingShelter)
            {
                StartSeekingShelter();
                return;
            }

            // Xử lý các trạng thái đang thực hiện
            if (CurrentActivity == PigActivityState.Eating)
            {
                if (activityTimer <= 0f)
                {
                    FinishCurrentActivity();
                }
                return;
            }

            if (CurrentActivity == PigActivityState.Drinking)
            {
                if (activityTimer <= 0f)
                {
                    FinishCurrentActivity();
                }
                return;
            }

            if (CurrentActivity == PigActivityState.Bathing)
            {
                if (activityTimer <= 0f)
                {
                    FinishCurrentActivity();
                }
                return;
            }

            if (CurrentActivity == PigActivityState.Sleeping)
            {
                if (!isNightOrStorm && activityTimer <= 0f)
                {
                    CurrentActivity = PigActivityState.Idling;
                    activityTimer = Random.Range(2.0f, 4.0f);
                }
                return;
            }

            if (CurrentActivity == PigActivityState.Idling)
            {
                if (activityTimer <= 0f)
                {
                    DecideNextActivity();
                }
                return;
            }

            // Nếu đang di chuyển đến mục tiêu (Wandering, SeekingFood, SeekingWater, SeekingMud, SeekingShelter, FollowingPlayer)
            Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;
            float distToTarget = Vector2.Distance(currentPos, targetPosition);

            if (distToTarget <= 0.45f || activityTimer <= 0f)
            {
                OnArrivedAtTarget();
            }
        }

        private void OnArrivedAtTarget()
        {
            switch (CurrentActivity)
            {
                case PigActivityState.SeekingFood:
                    CurrentActivity = PigActivityState.Eating;
                    activityTimer = Random.Range(4.0f, 7.0f);
                    var feeder = FarmEnvironment2D.Instance?.GetNearestFeeder(transform.position, false);
                    if (feeder != null)
                    {
                        feeder.EatFood(0.5f);
                    }
                    if (PigModel != null)
                    {
                        PigModel.WeightKg += 0.05f;
                    }
                    break;

                case PigActivityState.SeekingWater:
                    CurrentActivity = PigActivityState.Drinking;
                    activityTimer = Random.Range(3.0f, 5.0f);
                    break;

                case PigActivityState.SeekingMud:
                    CurrentActivity = PigActivityState.Bathing;
                    activityTimer = Random.Range(5.0f, 9.0f);
                    SetInMudPit(true);
                    if (PigModel != null)
                    {
                        PigModel.Mood = Mathf.Min(100, PigModel.Mood + 5);
                    }
                    break;

                case PigActivityState.SeekingShelter:
                    CurrentActivity = PigActivityState.Sleeping;
                    activityTimer = Random.Range(12.0f, 25.0f);
                    SetInShelter(true);
                    break;

                case PigActivityState.FollowingPlayer:
                case PigActivityState.Wandering:
                default:
                    CurrentActivity = PigActivityState.Idling;
                    activityTimer = Random.Range(2.5f, 5.0f);
                    break;
            }
        }

        private void DecideNextActivity()
        {
            var env = FarmEnvironment2D.Instance;
            var engine = MobileGameController.Instance?.Engine;
            var weather = engine?.CurrentWeather ?? WeatherType.QuangDang;
            var timeOfDay = engine?.Clock?.GetTimeOfDay() ?? TimeOfDay.Sang;
            Transform playerTr = GetPlayerTransform();

            // 1. Kiểm tra ban đêm / mưa bão -> Đi về chuồng ngủ
            if (timeOfDay == TimeOfDay.Toi || weather == WeatherType.GiongBao || weather == WeatherType.Mua || weather == WeatherType.RetDam)
            {
                StartSeekingShelter();
                return;
            }

            // 2. Kiểm tra người chơi ở gần và có hành động thân thiện
            if (playerTr != null)
            {
                float distToPlayer = Vector2.Distance(transform.position, playerTr.position);
                var activeTool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool : StardewToolType.CamHat;

                // Cầm Bàn chải hoặc Bao cám và ở gần < 5m: 50% chạy lại gần đòi ăn / cưng nựng
                if ((activeTool == StardewToolType.BanChai || activeTool == StardewToolType.CamHat) && distToPlayer < 5.0f && Random.value < 0.50f)
                {
                    CurrentActivity = PigActivityState.FollowingPlayer;
                    activityTimer = Random.Range(3.0f, 6.0f);
                    Vector2 approach = (Vector2)playerTr.position + (Random.insideUnitCircle.normalized * Random.Range(1.2f, 1.8f));
                    targetPosition = env != null ? env.ClampInsideFarm(approach) : approach;
                    return;
                }

                // Heo thân thiết (Bonding >= 40) tò mò lại gần (20%)
                if (distToPlayer < 4.0f && PigModel != null && PigModel.Bonding >= 40f && Random.value < 0.20f)
                {
                    CurrentActivity = PigActivityState.FollowingPlayer;
                    activityTimer = Random.Range(3.0f, 5.0f);
                    Vector2 approach = (Vector2)playerTr.position + (Random.insideUnitCircle.normalized * Random.Range(1.3f, 2.0f));
                    targetPosition = env != null ? env.ClampInsideFarm(approach) : approach;
                    return;
                }
            }

            // 3. Nhu cầu Ăn tại Máng (25% xác suất nếu có máng ăn có thức ăn)
            if (env != null && Random.value < 0.25f)
            {
                var feeder = env.GetNearestFeeder(transform.position, true);
                if (feeder != null)
                {
                    CurrentActivity = PigActivityState.SeekingFood;
                    activityTimer = Random.Range(6.0f, 10.0f);
                    targetPosition = (Vector2)feeder.transform.position + new Vector2(Random.Range(-0.6f, 0.6f), -0.75f);
                    targetPosition = env.ClampInsideFarm(targetPosition);
                    return;
                }
            }

            // 4. Nhu cầu Uống nước tại Bồn (20% xác suất)
            if (env != null && (timeOfDay == TimeOfDay.Trua || Random.value < 0.20f))
            {
                var trough = env.GetNearestWaterTrough(transform.position, true);
                if (trough != null)
                {
                    CurrentActivity = PigActivityState.SeekingWater;
                    activityTimer = Random.Range(5.0f, 8.0f);
                    targetPosition = (Vector2)trough.transform.position + new Vector2(Random.Range(-0.6f, 0.6f), -0.75f);
                    targetPosition = env.ClampInsideFarm(targetPosition);
                    return;
                }
            }

            // 5. Nhu cầu Tắm bùn (35% xác suất nếu trời Nắng Gắt hoặc là giống Lam Khê)
            bool lovesMud = weather == WeatherType.NangGat || (PigModel != null && PigModel.GeneLine == GeneLineId.LamKhe);
            if (env != null && ((lovesMud && Random.value < 0.40f) || Random.value < 0.15f))
            {
                var mud = env.GetNearestMudPit(transform.position);
                if (mud != null)
                {
                    CurrentActivity = PigActivityState.SeekingMud;
                    activityTimer = Random.Range(6.0f, 10.0f);
                    targetPosition = (Vector2)mud.transform.position + (Random.insideUnitCircle * 1.5f);
                    return;
                }
            }

            // 6. Đi dạo thong thả (Wandering) quanh bãi cỏ
            CurrentActivity = PigActivityState.Wandering;
            activityTimer = Random.Range(3.5f, 6.0f);
            Vector2 randomWalk = (Vector2)transform.position + (Random.insideUnitCircle * WanderRadius);
            targetPosition = env != null ? env.ClampInsideFarm(randomWalk) : randomWalk;
        }

        private void StartSeekingShelter()
        {
            var env = FarmEnvironment2D.Instance;
            var shelter = env != null ? env.GetNearestShelter(transform.position) : null;
            if (shelter != null)
            {
                CurrentActivity = PigActivityState.SeekingShelter;
                activityTimer = 15.0f;
                targetPosition = shelter.GetShelterRestSpot();
            }
            else
            {
                CurrentActivity = PigActivityState.Sleeping;
                activityTimer = 15.0f;
            }
        }

        private void FinishCurrentActivity()
        {
            CurrentActivity = PigActivityState.Idling;
            activityTimer = Random.Range(2.0f, 4.0f);
        }

        private void HandlePanicMovement2D()
        {
            activityTimer -= Time.deltaTime;
            if (activityTimer <= 0f)
            {
                activityTimer = Random.Range(0.8f, 1.6f);
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

        private void OnCollisionStay2D(Collision2D collision)
        {
            stuckTimer += Time.fixedDeltaTime;
            // Nếu bị kẹt cản vào vật thể > 0.6s -> Tự động chuyển hướng quay đầu và chuyển sang Wandering
            if (stuckTimer > 0.6f)
            {
                stuckTimer = 0f;
                activityTimer = Random.Range(3.0f, 5.0f);
                CurrentActivity = PigActivityState.Wandering;
                Vector2 normal = (collision.contactCount > 0) ? collision.GetContact(0).normal : (Vector2)Random.insideUnitCircle.normalized;
                targetPosition = (Vector2)transform.position + (normal * WanderRadius);
                var env = FarmEnvironment2D.Instance;
                if (env != null)
                {
                    targetPosition = env.ClampInsideFarm(targetPosition);
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            stuckTimer = 0f;
        }
    }
}
