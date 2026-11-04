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
        FollowingPlayer, // Tò mò chạy lại gần người chơi
        CapturedByThief, // Bị kẻ trộm tóm và kéo chạy đi
        AbductedByUfo    // Bị chùm sáng Tractor Beam của UFO hút lên trời
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class PigAgentView : MonoBehaviour, IDamageable
    {
        public Pig PigModel { get; private set; }
        bool farFromCamera;
        float nextHungryCheck;

        // IDamageable (GDD 19: monsters hurt the herd). Only a monster's blow counts: player sweeps and other
        // IDamageable scans are ignored. Damage goes into the Core pig (Health 0–100, death → PigDied).
        public bool IsAlive => PigModel != null && PigModel.IsAlive;
        public Vector2 Position => transform.position;
        public void TakeDamage(float amount, Vector2 knockbackDirection, float knockbackForce)
        {
            if (!EnemyAgentView.IsResolvingMonsterHit || PigModel == null || !PigModel.IsAlive) return;
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            bool died = MonsterPigDamage.Apply(engine, PigModel, amount, "quái");
            var body = GetComponent<Rigidbody2D>();
            if (!died && body != null && knockbackForce > 0f) body.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
            ShowThought(died ? "†" : "!!", 1.6f);
            if (died && SpriteRenderer != null) SpriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.85f);
        }

        [Header("Activity State")]
        public PigActivityState CurrentActivity = PigActivityState.Idling;

        [Header("2D Visual & Sorting")]
        public SpriteRenderer SpriteRenderer;
        public bool AutoDynamicSortingOrder = true;
        public int SortingPrecision = 100;

        [Header("2D Movement & Wander")]
        public float WalkSpeed = MovementSpec.AdultPigWalkMetersPerSecond;
        public float PanicSpeed = MovementSpec.AdultPigPanicMetersPerSecond;
        public float WanderRadius = 4.0f;

        [Header("Thought Bubble Visual Feedback")]
        public TMPro.TMP_Text ThoughtBubbleText;
        private float thoughtTimer = 0f;

        [Header("Infrastructure State")]
        public bool IsInMudPit { get; private set; }
        public bool IsInShelter { get; private set; }

        [Header("Capture & Abduction State")]
        public bool IsCaptured { get; private set; } = false;
        public Transform CaptorTransform { get; private set; }
        public bool IsAbductedByUfo { get; private set; } = false;
        public Transform UfoTransform { get; private set; }
        private float panicCryTimer = 0f;

        private Rigidbody2D rb;
        private Vector2 targetPosition;
        // walk-around waypoints toward the refuge (FarmNavGrid); empty = head straight for targetPosition
        private readonly System.Collections.Generic.List<Vector2> refugePath = new System.Collections.Generic.List<Vector2>();

        private void PlanRefugePath()
        {
            refugePath.Clear();
            var path = FarmNavGrid.FindPath(transform.position, targetPosition);
            if (path != null) refugePath.AddRange(path);
        }
        private float activityTimer = 0f;
        private Camera mainCamera;
        private Transform leaderTransform;
        private float stuckTimer = 0f;

        private void Awake()
        {
            WalkSpeed = MovementSpec.AdultPigWalkMetersPerSecond;
            PanicSpeed = MovementSpec.AdultPigPanicMetersPerSecond;
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

            EnsureThoughtBubble();

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

            string id = $"pig_{Mathf.Abs(gameObject.name.GetHashCode())}";
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

        private PigStage lastVisualStage = (PigStage)(-1);
        private float xichMaoChargeCooldown = 0f;

        private float heatCueTimer;
        private float seizureWarningLeft = -1f, seizureFitLeft;

        /// <summary>GDD 6.4 Lôi Tâm: 10 s visible twitch, then the fit (LoiTamSeizure decides panic and death).</summary>
        private void UpdateLoiTamSeizure()
        {
            var eng = MobileGameController.Instance?.Engine;
            if (eng == null || PigModel == null) return;
            if (seizureFitLeft > 0f)
            {
                // the fit: runs wildly for a few seconds
                seizureFitLeft -= Time.deltaTime;
                if (CurrentActivity != PigActivityState.Wandering || activityTimer <= 0.2f)
                {
                    CurrentActivity = PigActivityState.Wandering;
                    targetPosition = (Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
                    activityTimer = 0.8f;
                }
                return;
            }
            if (seizureWarningLeft < 0f)
            {
                if (LoiTamSeizure.ShouldWarn(eng, PigModel)) eng.Notify(LoiTamSeizure.BeginWarning(PigModel));
                if (!PigModel.SeizurePending) return;
                seizureWarningLeft = LoiTamSeizure.WarningSeconds;
            }
            seizureWarningLeft -= Time.deltaTime;
            if (PigModel.SeizurePending)
            {
                // twitch: a small shake the player can see, plus the thought bubble
                if (SpriteRenderer != null && SpriteRenderer.transform != transform) SpriteRenderer.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 60f) * 0.04f, 0f, 0f);
                else if (SpriteRenderer != null) SpriteRenderer.flipX = Mathf.Sin(Time.time * 30f) > 0f;
                if (heatCueTimer <= 0f) { heatCueTimer = 3f; ShowThought($"Run giật! {Mathf.CeilToInt(seizureWarningLeft)}s", 2.5f); }
            }
            if (seizureWarningLeft > 0f && PigModel.SeizurePending) return;
            if (SpriteRenderer != null && SpriteRenderer.transform != transform) SpriteRenderer.transform.localPosition = Vector3.zero;
            seizureWarningLeft = -1f;
            var shelter = FarmEnvironment2D.Instance?.GetNearestShelter(transform.position, false);
            bool sheltered = shelter != null && Vector2.Distance(transform.position, shelter.GetShelterRestSpot()) <= ShelterCalmRadius;
            var r = LoiTamSeizure.Resolve(eng, PigModel, sheltered, Random.value);
            eng.Notify(r.message);
            if (r.fit && !r.died) seizureFitLeft = 4f;
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
        }

        /// <summary>TẠM: a pig within this distance of a shelter's rest spot counts as "lùa vào mái trú".</summary>
        private const float ShelterCalmRadius = 3f;

        private void Update()
        {
            if (PigModel == null)
            {
                EnsurePigModel();
                if (PigModel == null) return;
            }

            if (!PigModel.IsAlive) return;

            // Đồng bộ tọa độ thực của heo vào Core PigModel để phục vụ Tầm phục vụ 30m, Nguồn mát 25m & Chen chúc 10x10m
            PigModel.PositionX = transform.position.x;
            PigModel.PositionY = transform.position.y;

            // GDD 6.4 visible heat incident: a collapsed pig lies still with a countdown; an overheating one pants "Nóng!"
            heatCueTimer -= Time.deltaTime;
            if (PigModel.ActiveDisease == DiseaseId.SayNang)
            {
                CurrentActivity = PigActivityState.Idling;
                activityTimer = 1f;
                if (heatCueTimer <= 0f)
                {
                    heatCueTimer = 4f;
                    ShowThought("Say nắng! Dội nước / che mát", 3.5f);
                }
            }
            else if (heatCueTimer <= 0f && MobileGameController.Instance?.Engine is GameEngine heatEng && heatEng.UncooledHeatHours(PigModel.Id) >= 1f)
            {
                heatCueTimer = 6f;
                ShowThought("Nóng!", 2.5f);
            }

            UpdateLoiTamSeizure();

            if (PigModel.Stage != lastVisualStage)
            {
                lastVisualStage = PigModel.Stage;
                ApplyStageVisualScale();
            }

            if (xichMaoChargeCooldown > 0f)
            {
                xichMaoChargeCooldown -= Time.deltaTime;
            }
            else if (PigModel.GeneLine == GeneLineId.XichMao && PigModel.IsXichMaoDomesticated && !IsCaptured)
            {
                TryExecuteXichMaoCounterCharge();
            }

            // 1. Dynamic Y-sorting trong Universal 2D
            if (AutoDynamicSortingOrder && SpriteRenderer != null)
            {
                SpriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * SortingPrecision);
            }

            // 2. Mobile LOD Culling trong 2D
            if (mainCamera != null)
            {
                float distSqr = ((Vector2)transform.position - (Vector2)mainCamera.transform.position).sqrMagnitude;
                // far pigs skip only the cosmetic work below; they keep walking to feeders and shelter
                // (a scattered herd used to freeze in place and miss its ration)
                farFromCamera = distSqr > 1600f; // > 40m
            }

            // 3. Quản lý thời gian hiển thị Thought Bubble & Sorting
            if (thoughtTimer > 0f)
            {
                thoughtTimer -= Time.deltaTime;
                if (thoughtTimer <= 0f && ThoughtBubbleText != null)
                {
                    ThoughtBubbleText.gameObject.SetActive(false);
                }
            }

            if (ThoughtBubbleText != null && ThoughtBubbleText.gameObject.activeSelf && SpriteRenderer != null)
            {
                if (ThoughtBubbleText is TMPro.TextMeshPro tmpMesh)
                {
                    tmpMesh.sortingOrder = SpriteRenderer.sortingOrder + 10;
                }
            }

            // 4. Xử lý di chuyển theo trạng thái Bị Bắt / Bắt Cóc / Tinh Thần / Hoạt động thường nhật
            if (IsCaptured)
            {
                panicCryTimer -= Time.deltaTime;
                if (panicCryTimer <= 0f)
                {
                    panicCryTimer = 1.4f;
                    ShowThought(IsAbductedByUfo ? "UFO!" : "Cứu!", 1.1f);
                }

                if (IsAbductedByUfo)
                {
                    if (UfoTransform == null)
                    {
                        ReleaseFromCapture(true);
                    }
                    else
                    {
                        // Hút lơ lửng dần lên phía khoang lái UFO
                        Vector3 targetUfoPos = UfoTransform.position - new Vector3(0f, 0.45f, 0f);
                        transform.position = Vector3.MoveTowards(transform.position, targetUfoPos, 1.4f * Time.deltaTime);
                    }
                    return;
                }
                else if (CaptorTransform != null)
                {
                    // Heo bị kéo lôi theo sau lưng tên trộm
                    Vector3 tetherPos = CaptorTransform.position - CaptorTransform.right * 0.75f;
                    transform.position = Vector3.MoveTowards(transform.position, tetherPos, 4.2f * Time.deltaTime);
                    return;
                }
                else
                {
                    // Kẻ bắt đã bị hạ gục hoặc biến mất
                    ReleaseFromCapture(true);
                    return;
                }
            }

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

            // Nếu đang trong trạng thái đứng yên -> Triệt tiêu vận tốc êm ái
            if (CurrentActivity == PigActivityState.Idling ||
                CurrentActivity == PigActivityState.Eating ||
                CurrentActivity == PigActivityState.Drinking ||
                CurrentActivity == PigActivityState.Bathing ||
                CurrentActivity == PigActivityState.Sleeping)
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 15f * Time.fixedDeltaTime);
                return;
            }

            // Nếu đang di chuyển (heo lọt ra ngoài rào thì vòng qua cổng gần nhất để vào lại)
            Vector2 currentPos = rb.position;
            var fenceEnv = FarmEnvironment2D.Instance;
            Vector2 steerTarget = fenceEnv != null ? fenceEnv.RouteIntoPasture(currentPos, targetPosition) : targetPosition;
            if (CurrentActivity == PigActivityState.SeekingShelter && refugePath.Count > 0)
            {
                while (refugePath.Count > 1 && Vector2.Distance(currentPos, refugePath[0]) < 0.9f) refugePath.RemoveAt(0);
                steerTarget = refugePath[0];
            }
            Vector2 toTarget = steerTarget - currentPos;

            // Nếu đang đi theo người chơi và đã đứng đủ gần (< 1.4m) -> Dừng lại ủn ỉn, nhìn người chơi
            if (CurrentActivity == PigActivityState.FollowingPlayer)
            {
                Transform playerTr = GetPlayerTransform();
                if (playerTr != null && Vector2.Distance(currentPos, playerTr.position) < 1.4f)
                {
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 15f * Time.fixedDeltaTime);
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

                if (PigModel.GeneLine == GeneLineId.MocCuoc) currentSpeed *= 1.15f;
                if (PigModel.Traits != null && PigModel.Traits.Exists(t => t.Id == "LuoiDi" && !t.IsSealed)) currentSpeed *= 0.80f;

                Vector2 desiredDir = toTarget.normalized;
                var engine = MobileGameController.Instance?.Engine;
                if (CurrentActivity == PigActivityState.SeekingShelter && engine?.FlowField != null)
                {
                    var flow = engine.FlowField.SampleFlow(FlowDestinationGroup.HerdRefuge, currentPos.x, currentPos.y);
                    Vector2 flowVec = new Vector2(flow.X, flow.Y);
                    if (flowVec.sqrMagnitude > 0.01f)
                    {
                        desiredDir = (desiredDir * 0.55f + flowVec.normalized * 0.45f).normalized;
                    }
                }

                Vector2 avoidance = ComputeObstacleAvoidance(currentPos, desiredDir);
                Vector2 separation = ComputeFlockingSeparation(currentPos);

                // Hòa trộn hướng di chuyển mong muốn + tránh chướng ngại vật + tách bầy
                Vector2 moveDir = (desiredDir + avoidance * 1.3f + separation * 0.8f).normalized;
                Vector2 targetVelocity = moveDir * currentSpeed;

                // Tăng tốc và ôm cua mượt mà (smooth acceleration & turning arc)
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, 12f * Time.fixedDeltaTime);

                // Lật sprite theo hướng vận tốc X
                if (SpriteRenderer != null && Mathf.Abs(rb.linearVelocity.x) > 0.05f)
                {
                    SpriteRenderer.flipX = rb.linearVelocity.x < 0f;
                }
            }
            else
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 15f * Time.fixedDeltaTime);
            }
        }

        private void HandleRoutineActivities2D()
        {
            activityTimer -= Time.deltaTime;

            var env = FarmEnvironment2D.Instance;
            var engine = MobileGameController.Instance?.Engine;

            // C-01: a pig put in the market drove walks to the farm gate toward the town and waits there for the player
            if (engine != null && PigModel != null && engine.Drove.Contains(PigModel.Id) && !engine.Drove.AtMarket && MarketGate(out Vector2 gate))
            {
                CurrentActivity = PigActivityState.Wandering;
                Vector2 spot = gate + Random.insideUnitCircle * 1.6f;
                targetPosition = env != null ? env.ClampInsideFarm(spot) : spot;
                activityTimer = Mathf.Max(8f, Vector2.Distance(transform.position, targetPosition) / Mathf.Max(0.5f, WalkSpeed) + 4f);
                if (Random.value < 0.25f) ShowThought("Đi chợ", 2f);
                return;
            }
            var weather = engine?.CurrentWeather ?? WeatherType.QuangDang;
            var timeOfDay = engine?.Clock?.GetTimeOfDay() ?? TimeOfDay.Sang;

            // GDD 8.1 Tiếng Huýt Chăn Đàn: đi theo Khoa tới hết thời gian (ưu tiên hơn đi ngủ/trú)
            if (whistleLeader != null && Time.time < whistleFollowUntil)
            {
                Vector2 leaderPos = whistleLeader.position;
                if (CurrentActivity != PigActivityState.FollowingPlayer || activityTimer <= 0f ||
                    Vector2.Distance(targetPosition, leaderPos) > 2.5f)
                {
                    CurrentActivity = PigActivityState.FollowingPlayer;
                    targetPosition = leaderPos + Random.insideUnitCircle.normalized * Random.Range(1.2f, 2.2f);
                    activityTimer = 1.0f;
                }
                return;
            }
            if (whistleLeader != null)
            {
                whistleLeader = null;
                CurrentActivity = PigActivityState.Idling;
                activityTimer = Random.Range(1.0f, 2.5f);
            }

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

            bool arrived = false;
            if (CurrentActivity == PigActivityState.SeekingWater)
            {
                var trough = env != null ? env.GetNearestWaterTrough(currentPos, false) : null;
                float distToTrough = trough != null ? Vector2.Distance(currentPos, trough.transform.position) : 999f;
                arrived = (distToTarget <= 0.55f) || (distToTrough <= 1.6f);
            }
            else if (CurrentActivity == PigActivityState.SeekingFood)
            {
                var feeder = env != null ? env.GetNearestFeeder(currentPos, false) : null;
                float distToFeeder = feeder != null ? Vector2.Distance(currentPos, feeder.transform.position) : 999f;
                arrived = (distToTarget <= 0.55f) || (distToFeeder <= 1.6f);
            }
            else if (CurrentActivity == PigActivityState.SeekingMud)
            {
                var mud = env != null ? env.GetNearestMudPit(currentPos) : null;
                float distToMud = mud != null ? Vector2.Distance(currentPos, mud.transform.position) : 999f;
                arrived = (distToTarget <= 0.55f) || (distToMud <= 3.2f);
            }
            else if (CurrentActivity == PigActivityState.SeekingShelter)
            {
                var shelter = env != null ? env.GetNearestShelter(currentPos, false) : null;
                float distToShelter = shelter != null ? Vector2.Distance(currentPos, shelter.transform.position) : 999f;
                arrived = (distToTarget <= 0.55f) || (distToShelter <= 2.8f);
            }
            else
            {
                arrived = (distToTarget <= 0.45f);
            }

            if (arrived)
            {
                OnArrivedAtTarget();
            }
            else if (activityTimer <= 0f)
            {
                // Hết thời gian di chuyển nhưng chưa tiếp cận được mục tiêu -> Dừng lại và chuyển về Idling
                CurrentActivity = PigActivityState.Idling;
                activityTimer = Random.Range(2.0f, 4.0f);
            }
        }

        private void OnArrivedAtTarget()
        {
            switch (CurrentActivity)
            {
                case PigActivityState.SeekingFood:
                    var feeder = FarmEnvironment2D.Instance?.GetNearestFeeder(transform.position, false);
                    float distToFeeder = feeder != null ? Vector2.Distance(transform.position, feeder.transform.position) : 999f;
                    var eng = MobileGameController.Instance?.Engine;
                    bool ate = false;
                    if (feeder != null && distToFeeder <= 1.8f && eng != null && PigModel != null && !string.IsNullOrEmpty(feeder.PointId))
                        ate = TroughCommands.PigEatsAtFeeder(eng, PigModel.Id, feeder.PointId).ate; // the daily ration, from the trough
                    if (ate)
                    {
                        CurrentActivity = PigActivityState.Eating;
                        activityTimer = Random.Range(4.0f, 7.0f);
                        ShowThought("Cám", 2.5f);
                        if (SpriteRenderer != null)
                        {
                            float dx = feeder.transform.position.x - transform.position.x;
                            if (Mathf.Abs(dx) > 0.1f) SpriteRenderer.flipX = dx < 0f;
                        }
                    }
                    else if (feeder != null && distToFeeder <= 1.8f && eng != null && PigModel != null && TroughCommands.WantsFood(eng, PigModel))
                    {
                        // at the trough but it is empty: wait there and complain, so the player sees what is wrong
                        CurrentActivity = PigActivityState.Idling;
                        activityTimer = Random.Range(5.0f, 8.0f);
                        ShowThought("Đói!", 3f);
                    }
                    else
                    {
                        CurrentActivity = PigActivityState.Idling;
                        activityTimer = Random.Range(2.0f, 4.0f);
                    }
                    break;

                case PigActivityState.SeekingWater:
                    var trough = FarmEnvironment2D.Instance?.GetNearestWaterTrough(transform.position, false);
                    float distToTrough = trough != null ? Vector2.Distance(transform.position, trough.transform.position) : 999f;
                    if (trough != null && distToTrough <= 1.8f && trough.HasWater)
                    {
                        CurrentActivity = PigActivityState.Drinking;
                        activityTimer = Random.Range(3.5f, 6.0f);
                        trough.DrinkWater(0.5f); // basin stock lives in Core now
                        if (PigModel != null)
                        {
                            PigModel.Mood = Mathf.Min(100, PigModel.Mood + 3);
                            PigModel.Health = Mathf.Min(100, PigModel.Health + 2);
                        }
                        ShowThought("Nước", 2.5f);
                        if (SpriteRenderer != null)
                        {
                            float dx = trough.transform.position.x - transform.position.x;
                            if (Mathf.Abs(dx) > 0.1f) SpriteRenderer.flipX = dx < 0f;
                        }
                    }
                    else
                    {
                        CurrentActivity = PigActivityState.Idling;
                        activityTimer = Random.Range(2.0f, 4.0f);
                    }
                    break;

                case PigActivityState.SeekingMud:
                    var mud = FarmEnvironment2D.Instance?.GetNearestMudPit(transform.position);
                    float distToMud = mud != null ? Vector2.Distance(transform.position, mud.transform.position) : 999f;
                    if (mud != null && distToMud <= 3.5f)
                    {
                        CurrentActivity = PigActivityState.Bathing;
                        activityTimer = Random.Range(5.0f, 9.0f);
                        SetInMudPit(true);
                        if (PigModel != null)
                        {
                            PigModel.Mood = Mathf.Min(100, PigModel.Mood + 5);
                        }
                        ShowThought("Bùn", 3.0f);
                    }
                    else
                    {
                        CurrentActivity = PigActivityState.Idling;
                        activityTimer = Random.Range(2.0f, 4.0f);
                    }
                    break;

                case PigActivityState.SeekingShelter:
                    var shelter = FarmEnvironment2D.Instance != null ? FarmEnvironment2D.Instance.GetNearestShelter(transform.position, false) : null;
                    float distToShelter = shelter != null ? Vector2.Distance(transform.position, shelter.transform.position) : 999f;
                    if (IsHeldAtRefuge && Vector2.Distance(transform.position, refugeCenter) <= RefugeRadius)
                    {
                        CurrentActivity = PigActivityState.Sleeping;   // resting in the refuge until released
                        activityTimer = Random.Range(8.0f, 14.0f);
                        SetInShelter(true);
                    }
                    else if (shelter != null && distToShelter <= 3.2f)
                    {
                        CurrentActivity = PigActivityState.Sleeping;
                        activityTimer = Random.Range(12.0f, 25.0f);
                        SetInShelter(true);
                        ShowThought("Zzz", 3.5f);
                    }
                    else
                    {
                        CurrentActivity = PigActivityState.Idling;
                        activityTimer = Random.Range(2.0f, 4.0f);
                    }
                    break;

                case PigActivityState.FollowingPlayer:
                    CurrentActivity = PigActivityState.Idling;
                    activityTimer = Random.Range(2.5f, 5.0f);
                    ShowThought("<3", 2.5f);
                    break;

                case PigActivityState.Wandering:
                default:
                    CurrentActivity = PigActivityState.Idling;
                    activityTimer = Random.Range(2.5f, 5.0f);
                    break;
            }
        }

        static PixelWorldPortal marketPortal;

        /// <summary>The farm's exit toward the market town (where a drove gathers).</summary>
        static bool MarketGate(out Vector2 position)
        {
            if (marketPortal == null)
                foreach (var p in FindObjectsByType<PixelWorldPortal>(FindObjectsSortMode.None))
                    if (p != null && p.TargetLocationId == MarketDrove.MarketId) { marketPortal = p; break; }
            position = marketPortal != null ? (Vector2)marketPortal.transform.position : Vector2.zero;
            return marketPortal != null;
        }

        private void DecideNextActivity()
        {
            if (IsHeldAtRefuge)
            {
                CurrentActivity = PigActivityState.SeekingShelter;
                targetPosition = refugeCenter + Random.insideUnitCircle * (RefugeRadius * 0.6f);
                activityTimer = Mathf.Clamp(Vector2.Distance(transform.position, targetPosition) + 6f, 6f, 60f);
                if (Vector2.Distance(transform.position, refugeCenter) > RefugeRadius) PlanRefugePath(); else refugePath.Clear();
                return;
            }
            var env = FarmEnvironment2D.Instance;
            var engine = MobileGameController.Instance?.Engine;
            var weather = engine?.CurrentWeather ?? WeatherType.QuangDang;
            var timeOfDay = engine?.Clock?.GetTimeOfDay() ?? TimeOfDay.Sang;
            Transform playerTr = GetPlayerTransform();

            // 0. Hungry: a pig that has not had its ration today walks to a feeder (Core decides; the trough must hold feed)
            if (env != null && engine != null && PigModel != null && timeOfDay != TimeOfDay.Toi && weather != WeatherType.GiongBao
                && TroughCommands.WantsFood(engine, PigModel))
            {
                var feeder = env.GetNearestFeeder(transform.position, true) ?? env.GetNearestFeeder(transform.position, false);
                if (feeder != null)
                {
                    CurrentActivity = PigActivityState.SeekingFood;
                    Vector2 approach = CalculateApproachSpot(feeder.transform.position, transform.position, 0.9f, 0.35f, 0.45f);
                    targetPosition = env.ClampInsideFarm(approach);
                    float dist = Vector2.Distance(transform.position, targetPosition);
                    activityTimer = Mathf.Max(12.0f, (dist / WalkSpeed) + 6.0f);
                    return;
                }
            }

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
                    Vector2 approach = CalculateApproachSpot(feeder.transform.position, transform.position, 0.9f, 0.35f, 0.45f);
                    targetPosition = env.ClampInsideFarm(approach);
                    float dist = Vector2.Distance(transform.position, targetPosition);
                    activityTimer = Mathf.Max(12.0f, (dist / WalkSpeed) + 6.0f);
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
                    Vector2 approach = CalculateApproachSpot(trough.transform.position, transform.position, 1.0f, 0.45f, 0.45f);
                    targetPosition = env.ClampInsideFarm(approach);
                    float dist = Vector2.Distance(transform.position, targetPosition);
                    activityTimer = Mathf.Max(12.0f, (dist / WalkSpeed) + 6.0f);
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

            // 6. Đi dạo thong thả (Wandering) - 30% đi theo cụm quanh Heo Đầu Đàn (Alpha Leader), 70% tự do khám phá
            var alphaLeader = GetAlphaLeaderPig();
            if (alphaLeader != null && Random.value < 0.30f)
            {
                CurrentActivity = PigActivityState.Wandering;
                activityTimer = Random.Range(4.0f, 7.0f);
                Vector2 nearAlpha = (Vector2)alphaLeader.transform.position + (Random.insideUnitCircle.normalized * Random.Range(2.0f, 4.5f));
                targetPosition = env != null ? env.ClampInsideFarm(nearAlpha) : nearAlpha;
                if (Random.value < 0.25f) ShowThought("...", 2.0f);
                return;
            }

            CurrentActivity = PigActivityState.Wandering;
            activityTimer = Random.Range(3.5f, 6.0f);
            Vector2 randomWalk = (Vector2)transform.position + (Random.insideUnitCircle * WanderRadius);
            targetPosition = env != null ? env.ClampInsideFarm(randomWalk) : randomWalk;
        }

        private Vector2 CalculateApproachSpot(Vector2 objectCenter, Vector2 pigPos, float halfWidth, float halfHeight, float standDist)
        {
            Vector2 diff = pigPos - objectCenter;
            Vector2 spot;

            // Quyết định tiếp cận từ phía nào dựa trên góc tương đối giữa heo và vật thể
            if (Mathf.Abs(diff.y) * halfWidth > Mathf.Abs(diff.x) * halfHeight)
            {
                // Tiếp cận từ trên (Bắc) hoặc dưới (Nam)
                float signY = diff.y >= 0f ? 1f : -1f;
                float clampX = Mathf.Clamp(diff.x, -halfWidth * 0.7f, halfWidth * 0.7f);
                spot = new Vector2(objectCenter.x + clampX, objectCenter.y + signY * (halfHeight + standDist));
            }
            else
            {
                // Tiếp cận từ trái (Tây) hoặc phải (Đông)
                float signX = diff.x >= 0f ? 1f : -1f;
                float clampY = Mathf.Clamp(diff.y, -halfHeight * 0.6f, halfHeight * 0.6f);
                spot = new Vector2(objectCenter.x + signX * (halfWidth + standDist), objectCenter.y + clampY);
            }

            return spot;
        }

        public PigAgentView GetAlphaLeaderPig()
        {
            var env = FarmEnvironment2D.Instance;
            if (env == null || env.Pigs == null || env.Pigs.Count == 0) return null;

            PigAgentView alpha = null;
            float maxWeight = -1f;

            for (int i = 0; i < env.Pigs.Count; i++)
            {
                var p = env.Pigs[i];
                if (p == null || p == this || !p.gameObject.activeInHierarchy) continue;
                float w = p.PigModel != null ? p.PigModel.WeightKg : 0f;
                if (w > maxWeight)
                {
                    maxWeight = w;
                    alpha = p;
                }
            }
            return alpha;
        }

        public void EnsureThoughtBubble()
        {
            if (ThoughtBubbleText != null) return;
            var child = transform.Find("Thought_Bubble");
            if (child != null)
            {
                ThoughtBubbleText = child.GetComponent<TMPro.TMP_Text>();
            }
            if (ThoughtBubbleText == null)
            {
                var go = new GameObject("Thought_Bubble");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                var tmp = go.AddComponent<TMPro.TextMeshPro>();
                tmp.fontSize = 4f;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.sortingOrder = 4000;
                ThoughtBubbleText = tmp;
                go.SetActive(false);
            }
        }

        public void ShowThought(string emojiOrText, float duration = 2.5f)
        {
            EnsureThoughtBubble();
            if (ThoughtBubbleText != null)
            {
                ThoughtBubbleText.text = emojiOrText;
                ThoughtBubbleText.gameObject.SetActive(true);
                thoughtTimer = duration;
            }
        }

        private Vector2 ComputeObstacleAvoidance(Vector2 currentPos, Vector2 desiredDir)
        {
            // Nếu đã gần đích (< 0.65m), không cản trở việc bước sát vào máng ăn / chuồng / bồn nước
            if (Vector2.Distance(currentPos, targetPosition) < 0.65f)
            {
                return Vector2.zero;
            }

            if (CurrentActivity == PigActivityState.SeekingWater)
            {
                var trough = FarmEnvironment2D.Instance?.GetNearestWaterTrough(currentPos, false);
                if (trough != null && Vector2.Distance(currentPos, trough.transform.position) < 1.5f)
                {
                    return Vector2.zero;
                }
            }
            else if (CurrentActivity == PigActivityState.SeekingFood)
            {
                var feeder = FarmEnvironment2D.Instance?.GetNearestFeeder(currentPos, false);
                if (feeder != null && Vector2.Distance(currentPos, feeder.transform.position) < 1.5f)
                {
                    return Vector2.zero;
                }
            }

            float forwardDist = 0.85f;
            float sideDist = 0.70f;
            float sideAngle = 35f;

            Vector2 leftDir = Quaternion.Euler(0f, 0f, sideAngle) * desiredDir;
            Vector2 rightDir = Quaternion.Euler(0f, 0f, -sideAngle) * desiredDir;

            RaycastHit2D hitCenter = CastWhisker(currentPos, desiredDir, forwardDist);
            RaycastHit2D hitLeft = CastWhisker(currentPos, leftDir, sideDist);
            RaycastHit2D hitRight = CastWhisker(currentPos, rightDir, sideDist);

            Vector2 avoidance = Vector2.zero;

            if (hitCenter.collider != null)
            {
                float weight = 1.0f - (hitCenter.distance / forwardDist);
                if (hitLeft.collider == null && hitRight.collider != null)
                {
                    avoidance += leftDir * (weight * 1.6f);
                }
                else if (hitRight.collider == null && hitLeft.collider != null)
                {
                    avoidance += rightDir * (weight * 1.6f);
                }
                else if (hitLeft.collider == null && hitRight.collider == null)
                {
                    Vector2 leftTangent = new Vector2(-hitCenter.normal.y, hitCenter.normal.x);
                    Vector2 rightTangent = new Vector2(hitCenter.normal.y, -hitCenter.normal.x);
                    if (Vector2.Dot(leftTangent, desiredDir) > Vector2.Dot(rightTangent, desiredDir))
                        avoidance += leftTangent * (weight * 1.5f);
                    else
                        avoidance += rightTangent * (weight * 1.5f);
                }
                else
                {
                    avoidance += hitCenter.normal * (weight * 2.0f);
                }
            }
            else
            {
                if (hitLeft.collider != null)
                {
                    float weight = 1.0f - (hitLeft.distance / sideDist);
                    avoidance += rightDir * (weight * 0.9f);
                }
                if (hitRight.collider != null)
                {
                    float weight = 1.0f - (hitRight.distance / sideDist);
                    avoidance += leftDir * (weight * 0.9f);
                }
            }

            return avoidance;
        }

        private RaycastHit2D CastWhisker(Vector2 origin, Vector2 dir, float dist)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, 0.16f, dir, dist);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == null) continue;
                if (h.collider.isTrigger) continue;
                if (h.collider.gameObject == gameObject) continue;
                if (h.collider.GetComponent<PigAgentView>() != null) continue; // Phân tách bầy do flocking phụ trách
                return h;
            }
            return default;
        }

        private Vector2 ComputeFlockingSeparation(Vector2 currentPos)
        {
            var env = FarmEnvironment2D.Instance;
            if (env == null || env.Pigs == null) return Vector2.zero;

            Vector2 separation = Vector2.zero;
            float separationRadius = 0.85f;
            int count = 0;

            for (int i = 0; i < env.Pigs.Count; i++)
            {
                var other = env.Pigs[i];
                if (other == null || other == this || !other.gameObject.activeInHierarchy) continue;

                Vector2 diff = currentPos - (Vector2)other.transform.position;
                float dist = diff.magnitude;

                if (dist > 0.001f && dist < separationRadius)
                {
                    float strength = (separationRadius - dist) / separationRadius;
                    separation += (diff / dist) * strength;
                    count++;
                }
            }

            if (count > 0)
            {
                separation /= count;
            }

            return separation;
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
            // Nếu đang đi tìm nước và chạm vào bồn nước -> Đã tiếp cận thành công!
            if (CurrentActivity == PigActivityState.SeekingWater && collision.gameObject.GetComponent<WaterTrough2DView>() != null)
            {
                stuckTimer = 0f;
                OnArrivedAtTarget();
                return;
            }

            // Nếu đang đi tìm thức ăn và chạm vào máng ăn -> Đã tiếp cận thành công!
            if (CurrentActivity == PigActivityState.SeekingFood && collision.gameObject.GetComponent<Feeder2DView>() != null)
            {
                stuckTimer = 0f;
                OnArrivedAtTarget();
                return;
            }

            stuckTimer += Time.fixedDeltaTime;
            // Nếu bị kẹt cản vào vật thể > 0.6s -> Tự động chuyển hướng quay đầu và chuyển sang Wandering
            if (stuckTimer > 0.6f && CurrentActivity == PigActivityState.SeekingShelter && IsHeldAtRefuge)
            {
                // recalled: walk round the obstacle instead of giving up and wandering off
                stuckTimer = 0f;
                PlanRefugePath();
                return;
            }
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
                ShowThought("?", 1.8f);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            stuckTimer = 0f;
        }

        #region Capture & Abduction Mechanics
        public void CaptureByThief(Transform thief)
        {
            if (thief == null) return;

            // Dòng gen Thiết Bì: da dày chống hiệu ứng kéo/bắt cóc (50% giằng thoát ngay lập tức)
            if (PigModel != null && PigModel.GeneLine == GeneLineId.ThietBi && Random.value < 0.50f)
            {
                ShowThought("■ Thiết Bì!", 2.2f);
                PlayerInteractionController.Instance?.ShowFeedback($"Heo Thiết Bì [{PigModel.Name}] đã giằng thoát khỏi tay kẻ trộm!");
                return;
            }

            // Dòng gen Xích Mao đã thuần dưỡng: lập tức húc trả kẻ trộm
            if (PigModel != null && PigModel.GeneLine == GeneLineId.XichMao && PigModel.IsXichMaoDomesticated)
            {
                var enemy = thief.GetComponent<EnemyAgentView>();
                if (enemy != null && enemy.IsAlive)
                {
                    Vector2 dir = ((Vector2)thief.position - (Vector2)transform.position).normalized;
                    enemy.TakeDamage(35f, dir, 3.5f);
                    xichMaoChargeCooldown = 4.0f;
                    ShowThought("!! Húc trả!", 2.5f);
                    PlayerInteractionController.Instance?.ShowFeedback($"Xích Mao [{PigModel.Name}] húc văng kẻ trộm (-35 HP)!");
                    return;
                }
            }

            IsCaptured = true;
            CaptorTransform = thief;
            IsAbductedByUfo = false;
            UfoTransform = null;
            CurrentActivity = PigActivityState.CapturedByThief;
            panicCryTimer = 0f;
            ShowThought("Cứu!", 3.0f);
            if (PigModel != null)
            {
                PigModel.Mood = Mathf.Max(0, PigModel.Mood - 25);
            }
        }

        public void AbductByUfo(Transform ufo)
        {
            if (ufo == null) return;

            // Tính trạng Dị Biến Da Phản Quang: 20% phản xạ tia kéo UFO
            if (PigModel != null && PigModel.Traits != null && PigModel.Traits.Exists(t => t.Id == "DaPhanQuang") && Random.value < 0.20f)
            {
                ShowThought("* Phản xạ!", 2.2f);
                PlayerInteractionController.Instance?.ShowFeedback($"[{PigModel.Name}] phản xạ tia kéo Tractor Beam nhờ Da Phản Quang!");
                return;
            }

            IsCaptured = true;
            IsAbductedByUfo = true;
            UfoTransform = ufo;
            CaptorTransform = null;
            CurrentActivity = PigActivityState.AbductedByUfo;
            panicCryTimer = 0f;
            ShowThought("UFO!", 3.0f);
            if (PigModel != null)
            {
                PigModel.Mood = Mathf.Max(0, PigModel.Mood - 30);
            }
        }

        private void TryExecuteXichMaoCounterCharge()
        {
            var enemies = FindObjectsByType<EnemyAgentView>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive || enemy.Model == null) continue;
                if (enemy.Model.TypeId != EnemyTypeId.KeBatHeo &&
                    enemy.Model.TypeId != EnemyTypeId.DauLinhTrom &&
                    enemy.Model.TypeId != EnemyTypeId.BayNhay &&
                    enemy.Model.TypeId != EnemyTypeId.ChuotGamKho)
                {
                    continue;
                }

                float d = Vector2.Distance(transform.position, enemy.transform.position);
                if (d <= 3.2f)
                {
                    Vector2 dir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
                    enemy.TakeDamage(32f, dir, 3.2f);
                    xichMaoChargeCooldown = 4.5f;
                    ShowThought("!! Húc!", 2.2f);
                    PlayerInteractionController.Instance?.ShowFeedback($"Xích Mao [{PigModel.Name}] chủ động húc trả [{enemy.Model.Name}]!");
                    break;
                }
            }
        }

        public void ReleaseFromCapture(bool rescuedByPlayer = true)
        {
            if (!IsCaptured) return;
            IsCaptured = false;
            CaptorTransform = null;
            IsAbductedByUfo = false;
            UfoTransform = null;
            CurrentActivity = PigActivityState.Wandering;

            if (rescuedByPlayer)
            {
                ShowThought("♥", 2.5f);
                if (PigModel != null)
                {
                    PigModel.Bonding = Mathf.Min(100f, PigModel.Bonding + 15f);
                    PigModel.Mood = Mathf.Min(100, PigModel.Mood + 20);
                }
                PlayerInteractionController.Instance?.ShowFeedback($"Bạn đã giải cứu thành công {PigModel?.Name ?? "Heo"}!");
            }
            targetPosition = (Vector2)transform.position + (Vector2)Random.insideUnitCircle * 2.0f;
        }

        public void OnPigStolenAndLost()
        {
            IsCaptured = false;
            PlayerInteractionController.Instance?.ShowFeedback($"<color=#ef4444>BÁO ĐỘNG: {PigModel?.Name ?? "Heo"} đã bị cướp mất!</color>");
        }

        public Pig PigData => PigModel;

        private Vector2 refugeCenter;
        private float refugeHoldUntil = -1f;
        public bool IsHeldAtRefuge => refugeHoldUntil > 0f &&
            (Time.time < refugeHoldUntil || (EnemyWaveSpawner.Instance != null && EnemyWaveSpawner.Instance.IsWaveActive));
        private const float RefugeRadius = 6f;

                public void CommandRecallToShelter(Vector3 refugePos)
        {
            if (IsCaptured || PigModel == null || !PigModel.IsAlive) return;
            whistleLeader = null; // tù và lùa về Nơi Trú thay lệnh huýt sáo đi theo
            CurrentActivity = PigActivityState.SeekingShelter;
            targetPosition = new Vector2(refugePos.x + Random.Range(-2.5f, 2.5f), refugePos.y + Random.Range(-1.8f, 1.8f));
            // the whole walk back, not a fixed 6 s: a pig 30 m out gave up half way (playtest 1 Nov 2026)
            float dist = Vector2.Distance(transform.position, targetPosition);
            activityTimer = Mathf.Clamp(dist / 1.0f + 6f, 6f, 90f);
            // GDD 3.7: a recalled herd stays in the refuge (2 game hours ~ 80 s, and as long as a wave is on)
            refugeCenter = refugePos;
            refugeHoldUntil = Time.time + 80f;
            PlanRefugePath();
            if (refugePath.Count > 1)
            {
                float len = 0f;
                Vector2 prev = transform.position;
                foreach (var p in refugePath) { len += Vector2.Distance(prev, p); prev = p; }
                activityTimer = Mathf.Clamp(len / 1.0f + 6f, 6f, 90f);
            }
            ShowThought("♦", 2.5f);
        }

        private Transform whistleLeader;
        private float whistleFollowUntil = -1f;
        public bool IsFollowingWhistle => whistleLeader != null && Time.time < whistleFollowUntil;

        /// <summary>GDD 8.1 Tiếng Huýt Chăn Đàn: đi theo <paramref name="leader"/> (Khoa) trong <paramref name="seconds"/> giây thực.</summary>
        public void CommandFollowLeader(Transform leader, float seconds)
        {
            if (leader == null || IsCaptured || PigModel == null || !PigModel.IsAlive) return;
            whistleLeader = leader;
            whistleFollowUntil = Time.time + Mathf.Max(0f, seconds);
            refugeHoldUntil = -1f; // huýt sáo thay lệnh giữ ở Nơi Trú
            refugePath.Clear();
            SetInShelter(false);
            CurrentActivity = PigActivityState.FollowingPlayer;
            targetPosition = (Vector2)leader.position + Random.insideUnitCircle.normalized * Random.Range(1.2f, 2.2f);
            activityTimer = 1.0f;
            ShowThought("♪", 2.2f);
        }

        public void CommandMoveTowards(Vector3 targetPos)
        {
            if (IsCaptured || PigModel == null || !PigModel.IsAlive) return;
            CurrentActivity = PigActivityState.FollowingPlayer;
            targetPosition = new Vector2(targetPos.x + Random.Range(-2.0f, 2.0f), targetPos.y + Random.Range(-2.0f, 2.0f));
            activityTimer = 5.0f;
            ShowThought("♪", 2.2f);
        }

        public void ApplyStageVisualScale()
        {
            if (PigModel == null) return;
            float scale = PigModel.Stage switch
            {
                PigStage.HeoNon => 0.62f,
                PigStage.DangLon => 0.84f,
                PigStage.TruongThanh => 1.00f,
                PigStage.HeoGia => 1.08f,
                _ => 1.00f
            };
            transform.localScale = new Vector3(scale, scale, 1f);
        }
        #endregion
    }
}
