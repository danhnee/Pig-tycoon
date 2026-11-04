using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PigTycoon.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Opt-in demo autopilot for the gameplay video (<c>-pigtycoon-scenario=video</c>, installed by
    /// <see cref="RecordingScenario"/>). It plays one route through all four maps by injecting a move vector into
    /// <see cref="PlayerMobileController.ScriptedMoveInput"/> and calling the same code the player's buttons call
    /// (CustomBuildingController.ExecuteInteraction, PixelWorldTravelController.BeginTravel, Core APIs for the gate offer
    /// and the exit charm). Targets are found at runtime by the world builder's object names
    /// ("&lt;role&gt;:&lt;sprite&gt; [&lt;interact id&gt;]", "Portal_&lt;id&gt;"); baked coordinates from
    /// Assets/Art/PixelMyth/Maps/*_layout.json are only the fallback. Walking uses lazy-grid A* over Physics2D overlap
    /// probes with line-of-sight smoothing; a stuck player replans, then teleports one waypoint (logged).
    /// Every step logs "[Autopilot] ...".
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class DemoAutopilot : MonoBehaviour
    {
        private const float CellSize = 0.5f;
        private const float ProbeRadius = 0.3f;
        private const float WideShotAtSeconds = 312f;
        private const string ExitCharm = "BuaHoiHuong";
        private static readonly BindingFlags NonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo TravellingField = typeof(PixelWorldTravelController).GetField("travelling", NonPublic);
        private static readonly MethodInfo BeginTravelMethod = typeof(PixelWorldTravelController).GetMethod("BeginTravel", NonPublic);

        private static DemoAutopilot instance;

        private Vector2 move;
        private bool driving;
        private float clock;
        private string caption = string.Empty;
        private float captionUntil;
        private bool captions = true;
        private GUIStyle captionStyle;

        // Lazy passability grid for the current scene.
        private readonly Dictionary<Vector2Int, bool> passable = new Dictionary<Vector2Int, bool>();
        private readonly HashSet<Vector2Int> extraBlocked = new HashSet<Vector2Int>();
        private readonly Collider2D[] overlapBuffer = new Collider2D[24];
        private readonly RaycastHit2D[] castBuffer = new RaycastHit2D[24];
        private int gridSceneHandle = -1;
        private int sceneSerial; // bumped on every scene load
        private Vector2 feetOffset;
        private Rect mapBounds = new Rect(-50, -50, 400, 400);
        private ContactFilter2D filter;
        private Rigidbody2D selfBody;

        public static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("DemoAutopilot");
            DontDestroyOnLoad(go);
            go.AddComponent<DemoAutopilot>();
        }

        private void Awake()
        {
            instance = this;
            SceneManager.sceneLoaded += (_, __) => sceneSerial++;
            foreach (var arg in Environment.GetCommandLineArgs())
                if (arg == "-pigtycoon-captions=0") captions = false;
        }

        private IEnumerator Start()
        {
            yield return StartCoroutine(Route());
        }

        private void OnDestroy() { if (instance == this) instance = null; }

        private void Update()
        {
            clock += Time.deltaTime;
            var player = Player;
            if (driving && player != null) player.ScriptedMoveInput = move;
        }

        // ------------------------------------------------------------------ route

        private IEnumerator Route()
        {
            Log("waiting for the Farm session");
            while (!(Engine != null && Player != null && FindFirstObjectByType<PixelWorldScene>() != null &&
                     FindFirstObjectByType<PixelWorldTravelController>() != null))
                yield return null;
            yield return Wait(2.5f);
            driving = true;
            var scene = FindFirstObjectByType<PixelWorldScene>();
            if (scene == null || scene.LocationId != "Farm" || Engine.WorldLocations.CurrentLocationId != "Farm")
            {
                Warn($"expected to start on the Farm, found '{(scene != null ? scene.LocationId : "none")}' (a restored save?); aborting");
                Finish("not on Farm");
                yield break;
            }
            Log($"START Farm, player {Player.name}, game clock day {Engine.Clock.CurrentDay} {Engine.Clock.CurrentHour:00}:{Engine.Clock.CurrentMinute:00}");
            Say("Nông trại Pig Tycoon", 3f);
            EnsureCameraTarget();
            // Step clear of the herd-shelter roof (it sorts in front of the spawn point) so the heroine is visible.
            Vector2 spawnPos = Player.transform.position;
            yield return Walk("step south of the spawn", spawnPos + new Vector2(0f, -6f), 0.8f);
            EnsureCameraTarget();
            yield return Wait(2f);

            // ---- Farm: service core -------------------------------------------------
            Say("Khu dịch vụ: nhà chính", 3f);
            var house = Locate("building:main_house:", new Vector2(82.5f, 114f));
            yield return Walk("main house door", house.Stand, 1.2f);
            Face(Vector2.up);
            yield return Wait(1.2f);

            Say("Nhà kho: mở kho lưu trữ", 3f);
            var warehouse = Locate("building:warehouse:", new Vector2(101f, 114f));
            yield return Walk("warehouse door", warehouse.Stand, 1.2f);
            Face(Vector2.up);
            yield return Wait(0.6f);
            Interact("warehouse storage", warehouse.Building);
            yield return Wait(3f);
            CloseOpenPopups();
            yield return Wait(0.8f);

            // ---- Farm: pasture ------------------------------------------------------
            Say("Bãi chăn: giữa đàn heo", 3f);
            yield return Walk("pasture centre", new Vector2(92f, 80f), 1f);
            yield return Walk("pasture west", new Vector2(76f, 70f), 1f);
            yield return Walk("pasture east", new Vector2(100f, 64f), 1f);
            yield return StartCoroutine(FeedingLoopDemo());
            yield return StartCoroutine(FencePatchDemo());
            yield return StartCoroutine(ToolWheelDemo());
            yield return StartCoroutine(HerdRecallDemo());
            LogQuests("after the herd recall");
            yield return StartCoroutine(CapacityBoardDemo());
            yield return StartCoroutine(SwordDemo());
            yield return StartCoroutine(FightDemo());
            yield return StartCoroutine(PlannedWaveDemo());

            // ---- Farm -> Forest through the real mystic gate -------------------------
            Say("Cổng huyền bí mở trong sáu giờ", 3f);
            bool gateOpen = RecordingScenario.SeedGate(Engine);
            Log($"gate offer seeded through Core: open={gateOpen}");
            bool inForest = false;
            if (gateOpen)
            {
                // the gate rises at a random free spot of the gate-event zone nearest the heroine, on camera
                var gate = MysticGate.FarmGate;
                if (gate != null)
                {
                    // demo framing: the gate pushes out of the ground just beside the heroine so the rise is on camera
                    // (camera half-height is 5.6 m; the gate is ~3.2 m tall)
                    gate.Raise(Player.transform.position + new Vector3(3.6f, 1.8f, 0f), 0.05f);   // ~4 m: outside the 2.2 m step-in radius
                    Log($"mystic gate rises at {(Vector2)gate.transform.position}");
                }
                yield return Wait(2.8f);
                if (CurrentLocation() == "Forest_Primeval") { Log("stepped into the gate while it rose"); travelOk = true; }
                else yield return StartCoroutine(UsePortal("Farm->Forest_Primeval", null, new Vector2(64f, 88f)));
                inForest = travelOk;
            }

            if (inForest)
            {
                Say("Rừng Nguyên Thủy", 3f);
                yield return Wait(1.5f);
                // only the running event's glade is furnished (ForestEventSites); walk the game trail to it and back
                string site = ForestEventSites.ActiveSiteId ?? "doi_mot_lay_mot";
                var landmark = Locate("site:" + site + ":", new Vector2(45.5f, 40.6f));
                Say("Địa điểm sự kiện", 3f);
                yield return Walk("event site " + site, landmark.Position + new Vector2(0f, -1.8f), 1.0f, 60f);
                Face(Vector2.up);
                yield return Wait(0.6f);
                if (landmark.Building != null) Interact("event site " + site, landmark.Building);
                yield return Wait(1.5f);
                if (ForestEventPopup.IsOpen)
                {
                    ForestEventPopup.Instance.Choose(0);
                    Log($"forest event resolved at the site: completion {Engine.ForestAndEvents.Expedition.CompletionPercent:0}%");
                }
                yield return Wait(2.4f);
                CloseOpenPopups();
                var back = FindPortal("Forest_Primeval->Farm");
                Vector2 gatePos = back != null ? (Vector2)back.transform.position : new Vector2(30f, 33.5f);
                Say("Cổng chắp vá", 3f);
                yield return Walk("patched gate", gatePos + new Vector2(0f, -1.6f), 0.8f);
                Face(Vector2.up);
                yield return Wait(2f);
                Say("Nhặt Bùa Hồi Hương để thoát cổng", 3f);
                GrantExitCharm();
                yield return Wait(1.5f);
                yield return StartCoroutine(UsePortal("Forest_Primeval->Farm", ExitCharm, gatePos));
                Log($"forest exit {(travelOk ? "completed" : "FAILED")}; expedition active={Engine.ForestAndEvents.Expedition.IsActive}, bag items={Engine.SpaceBag.Count}");
            }

            // ---- Farm -> MarketTown -------------------------------------------------
            if (CurrentLocation() == "Farm")
            {
                // C-01: pick two grown pigs for the drove; they walk to the gate and go to the market with the player
                foreach (var pig in Engine.Pigs.Where(p => p.IsAlive && !p.IsIsolated && p.Stage == PigStage.TruongThanh).Take(2).ToList())
                    Log("drove: " + Engine.Drove.Consign(Engine, pig.Id).message);
                Say("Lùa hai con heo lớn ra chợ thị trấn", 3f);
                yield return StartCoroutine(UsePortal("Farm->MarketTown", null, new Vector2(182.5f, 147f)));
            }
            if (CurrentLocation() == "MarketTown")
            {
                Say("Chợ Thị Trấn", 3f);
                yield return Wait(1.2f);
                var stall = Locate("building:stall_day_2:", new Vector2(27.5f, 17f));
                Say("Chợ ngày: bán heo lấy vàng", 3f);
                yield return Walk("day stall", stall.Stand, 1.2f);
                Face(Vector2.up);
                yield return Wait(0.6f);
                Interact("day market counter", stall.Building);
                yield return Wait(2f);
                if (DayMarketPopup.IsOpen)
                {
                    int goldBefore = Engine.Farm.Gold, pigsBefore = Engine.Pigs.Count;
                    DayMarketPopup.Instance.SellRow(0);
                    Log($"day market sale: gold {goldBefore} -> {Engine.Farm.Gold}, herd {pigsBefore} -> {Engine.Pigs.Count}");
                    yield return Wait(2.5f);
                    DayMarketPopup.Instance.Close();
                }
                else Warn("day market screen did not open");
                // GDD 20: the night market counter lists the day's slots (closed until 18:00)
                Engine.Village.RefreshStockForDay(Engine.Clock.CurrentDay);
                var nightCounter = Engine.Village.GetVillager(VillagerId.ChoDem);
                Log($"night market slots: {nightCounter?.Listings.Count ?? 0}, gold slots: {nightCounter?.Listings.FindAll(l => l.AcceptsGold).Count ?? 0}");
                var barterPopup = VillageBarterPopup.Instance;
                if (barterPopup == null)
                {
                    var cv = VillageBarterPopup.FindScreenSpaceCanvas();
                    if (cv != null) barterPopup = cv.gameObject.AddComponent<VillageBarterPopup>();
                }
                if (barterPopup != null) barterPopup.OpenForVillager(VillagerId.ChoDem);
                yield return Wait(3f);
                CloseOpenPopups();
                yield return Wait(0.8f);
                yield return StartCoroutine(TalkToNpc("ba_ban_rau", "Bà bán rau"));
                Say("Quảng trường chợ", 3f);
                yield return Walk("square centre", new Vector2(65f, 32.5f), 1.2f);
                var quarantine = Locate("building:quarantine_post:", new Vector2(4.3f, 30.2f));
                Say("Trạm kiểm dịch thú y", 3f);
                yield return Walk("quarantine post", quarantine.Stand, 1.2f);
                Face(Vector2.up);
                yield return Wait(0.5f);
                Interact("quarantine post", quarantine.Building);
                yield return Wait(2.4f);
                CloseOpenPopups();
                // GDD 5.4 appraisal house and GDD 11 recruit hall
                var appraisal = Locate("building:appraisal_house:", new Vector2(22f, 25f));
                Say("Nhà giám định gen", 3f);
                yield return Walk("appraisal house", appraisal.Stand, 1.2f);
                Interact("appraisal house", appraisal.Building);
                yield return Wait(2.5f);
                Log($"appraisal screen open={AppraisalPopup.IsOpen}");
                CloseOpenPopups();
                var recruit = Locate("building:recruit_hall:", new Vector2(34f, 44f));
                Say("Hội tuyển người", 3f);
                yield return Walk("recruit hall", recruit.Stand, 1.2f);
                Interact("recruit hall", recruit.Building);
                yield return Wait(1.5f);
                var engR = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
                if (engR != null) SeedForVideo(engR, NpcSpecialistId.BacSiThuY, 25f); // kịch bản video: đã xong nhiệm vụ riêng, hảo cảm 25 (Tím)
                if (RecruitPopup.IsOpen)
                {
                    RecruitPopup.Instance.HireRow(2);   // Bác Sĩ Thú Y: chợ ngày, hợp đồng 1 chu kỳ (cọc 30%)
                    Log($"recruit: hired NPCs = {Engine.PetsAndNpcs.HiredNpcs.Count}");
                    yield return Wait(2.5f);
                }
                CloseOpenPopups();
                yield return StartCoroutine(UsePortal("MarketTown->Hub_Village", null, new Vector2(1.2f, 26f)));
            }

            // ---- Hub_Village --------------------------------------------------------
            if (CurrentLocation() == "Hub_Village")
            {
                Say("Làng Bờ Rìa", 3f);
                yield return Wait(1.2f);
                var well = Locate("prop:gieng_chung:", new Vector2(36f, 38f));
                Say("Giếng chung", 2.5f);
                yield return Walk("common well", well.Position + new Vector2(0f, -1.8f), 1.2f);
                var board = Locate("prop:notice_board:", new Vector2(44f, 30.6f));
                Say("Bảng tin làng", 3f);
                yield return Walk("notice board", board.Stand, 1.2f);
                Face(Vector2.up);
                yield return Wait(0.6f);
                Interact("notice board", board.Building);
                yield return Wait(2f);
                CloseOpenPopups();
                var boardAnim = Player != null ? Player.GetComponent<CharacterSpriteAnimator>() : null;
                if (boardAnim != null) { Log("whistle near the board"); boardAnim.TriggerWhistle(); }
                yield return Wait(1.6f);
                Say("Quán bà Sáu", 3f);
                yield return StartCoroutine(TalkToNpc("ba_sau", "Bà Sáu"));
                yield return StartCoroutine(UsePortal("Hub_Village->Farm", null, new Vector2(74.6f, 28f)));
            }

            // ---- Back on the Farm: wide shot of the pasture --------------------------
            if (CurrentLocation() == "Farm")
            {
                Say("Về lại nông trại", 3f);
                float budget = Mathf.Max(8f, WideShotAtSeconds - Recorded - 2f);
                yield return Walk("pasture for the closing shot", new Vector2(92f, 70f), 1.5f, budget);
            }
            driving = false;
            move = Vector2.zero;
            if (Player != null) Player.ScriptedMoveInput = Vector2.zero;
            WideShot();
            Say("Pig Tycoon", 8f);
            yield return Wait(8f);
            Finish("route complete");
        }

        // ------------------------------------------------------------------ steps

        private bool travelOk;

        private IEnumerator UsePortal(string portalId, string exitItem, Vector2 fallback)
        {
            travelOk = false;
            var portal = FindPortal(portalId);
            Vector2 where = portal != null ? (Vector2)portal.transform.position : fallback;
            if (portal != null && exitItem != null) portal.RequestedExitItemId = exitItem; // as if the player chose the owned item
            string fromScene = SceneManager.GetActiveScene().name;
            float within = portal != null ? Mathf.Max(0.8f, portal.InteractionRadius - 0.7f) : 1.5f;
            yield return Walk("portal " + portalId, where, within, 150f);
            if (!Travelling)
            {
                yield return Wait(0.5f);
                if (!Travelling && SceneManager.GetActiveScene().name == fromScene && portal != null)
                {
                    Log($"portal {portalId}: interact (same path as the E key: PixelWorldTravelController.BeginTravel)");
                    InvokeBeginTravel(portal);
                }
            }
            // wait in REAL time: with Time.captureFramerate the game clock runs ~2.5x faster than the wall clock,
            // and a 30 s game-time window was only ~12 s of real loading on this machine (first run 31 Oct)
            Application.backgroundLoadingPriority = ThreadPriority.High;
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 120f)
            {
                if (SceneManager.GetActiveScene().name != fromScene && !Travelling) break;
                yield return null;
            }
            travelOk = SceneManager.GetActiveScene().name != fromScene && !Travelling;
            Log($"portal {portalId}: {(travelOk ? "arrived" : "FAILED")} -> location {CurrentLocation()} scene {SceneManager.GetActiveScene().name}");
            driving = true;
            EnsureCameraTarget();
            yield return Wait(1.5f);
            EnsureCameraTarget();
        }

        /// <summary>The roster activates either An or Khoa after every scene load; the camera must follow that one.</summary>
        private void EnsureCameraTarget()
        {
            var player = Player;
            if (player == null) return;
            foreach (var follow in FindObjectsByType<CameraFollow2D>(FindObjectsSortMode.None))
            {
                if (follow.Target != player.transform) { Log($"camera target -> {player.name}"); follow.Target = player.transform; }
                follow.SnapToTarget();
            }
            DiagnosePlayerVisibility(player);
        }

        /// <summary>The heroine vanished on every Farm re-entry in the 31 Oct videos; log what the camera sees.</summary>
        private void DiagnosePlayerVisibility(PlayerMobileController player)
        {
            var all = FindObjectsByType<PlayerMobileController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var sb = new System.Text.StringBuilder();
            foreach (var p in all)
            {
                var r = p.GetComponentInChildren<SpriteRenderer>(true);
                sb.Append($" [{p.name} scene={p.gameObject.scene.name} active={p.isActiveAndEnabled} pos={(Vector2)p.transform.position}");
                if (r != null)
                    sb.Append($" sr={r.enabled}/{r.gameObject.activeInHierarchy} sprite={(r.sprite != null ? r.sprite.name : "null")} a={r.color.a:0.00} layer={r.sortingLayerName} order={r.sortingOrder} scale={r.transform.lossyScale.x:0.00}");
                sb.Append(p == player ? " DRIVEN]" : "]");
            }
            var cam = Camera.main;
            Log($"visibility: {all.Length} player(s){sb}; Camera.main={(cam != null ? cam.name + " scene=" + cam.gameObject.scene.name + " pos=" + (Vector2)cam.transform.position : "none")}");
        }

        // ---- radial tool wheel (public Open/Close; highlight is driven by reflection because the sweep has no public API)
        private static readonly FieldInfo WheelHighlight = typeof(RadialToolWheel).GetField("highlight", NonPublic);

        private IEnumerator ToolWheelDemo()
        {
            var wheel = RadialToolWheel.Instance;
            var hotbar = HotbarController.Instance;
            if (wheel == null || hotbar == null) { Warn("radial tool wheel not present; skipping"); yield break; }
            Say("Vòng dụng cụ (Tab)", 3f);
            wheel.Open();
            if (!wheel.IsOpen) { Warn("tool wheel refused to open"); yield break; }
            Log("tool wheel opened");
            int weapon = hotbar.Wheel.IndexOf("DaoGo");
            var sweep = new List<int>();
            for (int i = 0; i < hotbar.Wheel.Slots.Count && sweep.Count < 2; i++)
                if (i != weapon && !string.IsNullOrEmpty(hotbar.Wheel.Slots[i])) sweep.Add(i);
            sweep.Add(weapon >= 0 ? weapon : hotbar.Wheel.SelectedIndex);
            float each = 2f / sweep.Count;
            foreach (int slot in sweep)
            {
                float until = clock + each;
                while (clock < until) { if (WheelHighlight != null) WheelHighlight.SetValue(wheel, slot); yield return null; }
                Log($"tool wheel highlight sector {slot} ({hotbar.Wheel.Slots[slot]})");
            }
            yield return Wait(0.5f);
            wheel.Close(true);
            Log($"tool wheel closed; tool now {hotbar.Wheel.SelectedToolId}");
            yield return Wait(0.8f);
        }

        private IEnumerator SwordDemo()
        {
            var dummy = Locate("decor:scarecrow", new Vector2(131f, 60f));
            Say("Tập kiếm bên bù nhìn", 3f);
            Vector2 stand = dummy.Position + new Vector2(-1.8f, 0f);
            yield return Walk("scarecrow training spot", stand, 0.8f);
            Face(Vector2.right);
            var anim = Player != null ? Player.GetComponent<CharacterSpriteAnimator>() : null;
            if (anim == null) { Warn("no CharacterSpriteAnimator for the sword demo"); yield break; }
            yield return Wait(0.5f);
            for (int i = 0; i < 3; i++)
            {
                int n = i + 1;
                Log($"sword attack {n}/3 (PlayCombatAction Slash)");
                anim.PlayCombatAction(CombatActionType.Slash, Vector2.right, 1f, () => Log($"attack {n} hit frame"), null, "dao_go");
                yield return Wait(0.8f);
            }
            yield return Wait(0.6f);
        }

        /// <summary>
        /// A Kẻ Cào Phá (clawing beast) breaks out of the tall grass beside the training spot; the heroine fights it
        /// through the real combat path (PlayerCombatController.ExecuteAttack: auto-target, stamina, hit detection).
        /// </summary>
        // GDD 3.7 feeding loop as a player does it: bag at the feed store -> trough; bucket at the well -> basin.
        private IEnumerator FeedingLoopDemo()
        {
            var pic = PlayerInteractionController.Instance;
            var hotbar = HotbarController.Instance;
            var silo = FindAnyObjectByType<FeedSilo2DView>();
            var well = FindAnyObjectByType<WaterWell2DView>();
            Feeder2DView feeder = null; WaterTrough2DView basin = null;
            foreach (var f in FindObjectsByType<Feeder2DView>())
                if (feeder == null || ((Vector2)f.transform.position - new Vector2(80f, 90f)).sqrMagnitude < ((Vector2)feeder.transform.position - new Vector2(80f, 90f)).sqrMagnitude) feeder = f;
            foreach (var b in FindObjectsByType<WaterTrough2DView>())
                if (basin == null || ((Vector2)b.transform.position - new Vector2(76f, 84f)).sqrMagnitude < ((Vector2)basin.transform.position - new Vector2(76f, 84f)).sqrMagnitude) basin = b;
            if (pic == null || hotbar == null || silo == null || feeder == null)
            {
                Warn($"feeding loop not wired (interaction={pic != null}, hotbar={hotbar != null}, store={silo != null}, trough={feeder != null})");
                yield break;
            }
            feeder.EatFood(feeder.CurrentFoodKg);   // start from an empty trough so the refill is visible
            Say("Xúc cám ở kho cám", 3f);
            hotbar.EquipTool(StardewToolType.CamHat);
            yield return Walk("feed store", (Vector2)silo.transform.position + new Vector2(0f, -1.7f), 0.7f);
            Face(Vector2.up);
            yield return Wait(0.5f);
            pic.PerformAction();
            Log($"scooped feed: bag {pic.CurrentBagFeedKg:0} kg, store {silo.CurrentFeedKg:0} kg");
            Say("Đổ cám vào máng", 3f);
            yield return Walk("trough", (Vector2)feeder.transform.position + new Vector2(0f, -1.4f), 0.7f);
            Face(Vector2.up);
            yield return Wait(0.5f);
            float before = feeder.CurrentFoodKg;
            pic.PerformAction();
            Log($"poured feed: trough {before:0} -> {feeder.CurrentFoodKg:0} kg (target {pic.CurrentTarget?.GetType().Name ?? "none"})");
            yield return Wait(1.5f);
            if (well == null || basin == null) { Warn($"water loop not wired (well={well != null}, basin={basin != null})"); yield break; }
            basin.DrinkWater(basin.CurrentWaterLiters);
            Say("Múc nước giếng, đổ vào bồn", 3f);
            hotbar.EquipTool(StardewToolType.XoNuoc);
            yield return Walk("well", (Vector2)well.transform.position + new Vector2(0f, -1.7f), 0.7f);
            Face(Vector2.up);
            yield return Wait(0.5f);
            pic.PerformAction();
            Log($"drew water: bucket {pic.CurrentBucketWaterLiters:0} L");
            yield return Walk("basin", (Vector2)basin.transform.position + new Vector2(0f, -1.4f), 0.7f);
            Face(Vector2.up);
            yield return Wait(0.5f);
            before = basin.CurrentWaterLiters;
            pic.PerformAction();
            Log($"poured water: basin {before:0} -> {basin.CurrentWaterLiters:0} L");
            yield return Wait(1.5f);
        }

        // Story Q01: the farm is handed over with one broken fence segment; E beside it puts up a temporary panel (30G).
        private IEnumerator FencePatchDemo()
        {
            PixelFenceSegment broken = null;
            foreach (var f in FindObjectsByType<PixelFenceSegment>()) if (f != null && f.IsBroken) { broken = f; break; }
            if (broken == null) { Warn("no broken fence segment for Q01"); yield break; }
            Say("Q01: chặn đoạn rào hỏng bằng rào tạm", 3f);
            var p = (Vector2)broken.transform.position;
            var farm = PixelMythFarmGameplay.Instance;
            Vector2 inside = farm != null ? Vector2.MoveTowards(p, farm.FenceRect.center, 1.6f) : p + Vector2.right * 1.6f;
            yield return Walk("broken fence", inside, 0.8f);
            yield return Wait(0.5f);
            PixelFenceSegment.TryPlayerFix(Player.transform.position, out var msg);
            PlayerInteractionController.Instance?.ShowFeedback(msg);
            Log($"fence patch at {p}: {msg} (broken now={broken.IsBroken}, temp={broken.IsTemporaryPatch})");
            yield return Wait(2f);
        }

        private IEnumerator CapacityBoardDemo()
        {
            var board = Locate("prop:info_board", new Vector2(87f, 107f));
            if (board.Building == null) { Warn("capacity board not found"); yield break; }
            Say("Q02: đọc bảng sức chứa", 3f);
            yield return Walk("capacity board", board.Stand, 1.2f);
            Face(Vector2.up);
            yield return Wait(0.5f);
            Interact("capacity board", board.Building);
            yield return Wait(2.5f);
            CloseOpenPopups();
            LogQuests("after the capacity board");
        }

        private void LogQuests(string when)
        {
            var e = Engine;
            if (e == null) return;
            var v = e.Quests.View(e, e.Quests.ActiveMain);
            string steps = v == null ? "" : string.Join(", ", v.Steps.Select(st => (st.done ? "[x] " : "[ ] ") + st.label));
            Log($"quests {when}: done [{string.Join(",", e.Quests.Completed)}], active {v?.Id} {v?.Title}: {steps}");
        }

        // GDD 3.7 "Lùa về Nơi Trú": the horn medallion (G); pigs walk to the shelter front.
        private IEnumerator HerdRecallDemo()
        {
            var shelter = FarmEnvironment2D.Instance != null ? FarmEnvironment2D.Instance.Shelter : null;
            if (PixelMythFarmGameplay.Instance == null || shelter == null) { Warn("herd recall not wired"); yield break; }
            Say("Thổi tù và: lùa đàn về Nơi Trú", 3f);
            yield return Walk("beside the shelter", (Vector2)shelter.transform.position + new Vector2(5f, -4f), 1f);
            MythHudController.HerdRecall();
            Log("herd recall issued");
            // GDD 3.10 C2: >= 90% of the herd in the refuge within 2 game hours (~80 s real); stop once reached
            for (int s = 0; s < 32; s++)
            {
                yield return Wait(2.5f);
                int near = 0, all = 0;
                foreach (var p in FindObjectsByType<PigAgentView>())
                {
                    if (p == null || p.PigModel == null || !p.PigModel.IsAlive) continue;
                    all++;
                    if (Vector2.Distance(p.transform.position, shelter.transform.position) < 9f) near++;
                }
                Log($"herd near the shelter: {near}/{all}");
                if (all > 0 && near >= 0.9f * all) { Log($"GDD C2 reached: {near}/{all} in {(s + 1) * 2.5f:0} s"); break; }
            }
            var env = FarmEnvironment2D.Instance;
            foreach (var p in FindObjectsByType<PigAgentView>())
            {
                if (p == null || p.PigModel == null || !p.PigModel.IsAlive) continue;
                if (Vector2.Distance(p.transform.position, shelter.transform.position) < 9f) continue;
                Log($"straggler {p.PigModel.Name} at {(Vector2)p.transform.position} activity={p.CurrentActivity} held={p.IsHeldAtRefuge} inside={(env != null && env.IsInsidePasture(p.transform.position))}");
            }
        }

        // GDD 3.8: a minor wave from the sector the cycle seeded; the heroine meets the first raider.
        /// <summary>Video scenario only: marks an NPC's personal quest done with the given affinity so the hiring scene can be filmed.</summary>
        private static void SeedForVideo(GameEngine eng, NpcSpecialistId id, float affinity)
        {
            var r = eng.NpcRelations.Get(id);
            if (r.QuestStage < 1) r.QuestStage = 1;
            r.Affinity = Mathf.Max(r.Affinity, affinity);
        }

        private IEnumerator PlannedWaveDemo()
        {
            var spawner = EnemyWaveSpawner.Instance;
            if (spawner == null || PixelMythFarmGameplay.Instance == null) { Warn("planned waves not wired"); yield break; }
            // GDD 11.2: thuê 2 lính bằng vàng (luật thuê thật) để thấy lính vào trận cùng đợt quái
            var eng = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (eng != null)
            {
                eng.Farm.Gold += 2000;
                // Kịch bản video: bỏ qua nhiệm vụ riêng của 2 lính (ADR-0026) để quay cảnh lính vào trận; luật ký hợp đồng vẫn là luật thật
                SeedForVideo(eng, NpcSpecialistId.VeBinhKhien, 0f);
                Log("hire Vệ Binh Khiên: " + NpcContracts.Sign(eng, NpcSpecialistId.VeBinhKhien, ContractKind.TheoDot, 3).message);
            }
            spawner.TriggerPlannedWave(major: false);
            Log($"planned wave from {spawner.LastWaveDirection}, active={spawner.IsWaveActive}");
            Say($"Quấy phá từ hướng {spawner.LastWaveDirection}", 3f);
            yield return Wait(3.5f);
            EnemyAgentView first = null;
            foreach (var e in FindObjectsByType<EnemyAgentView>()) if (e != null && e.IsAlive) { first = e; break; }
            if (first == null) { Warn("no raider spawned"); yield break; }
            Log($"first raider {first.Model.TypeId} at {first.Position}; mercenaries on field = {MercenaryAgentView.Active.Count}");
            // walk out to meet it (raiders come from the map rim; the camera follows the heroine)
            float until = Recorded + 40f;
            while (first != null && first.IsAlive && Recorded < until && Vector2.Distance(Player.transform.position, first.Position) > 6f)
                yield return Walk("toward the raider", first.Position, 5f, 4f);
            var combat = PlayerCombatController.Instance;
            for (int swing = 0; swing < 16 && first != null && first.IsAlive && combat != null; swing++)
            {
                Vector2 to = first.Position - (Vector2)Player.transform.position;
                if (to.magnitude > 1.6f) yield return Walk("close in on the raider", first.Position - to.normalized * 1.1f, 0.5f, 4f);
                if (first == null || !first.IsAlive) break;
                Player.SetFacingDirection((first.Position - (Vector2)Player.transform.position).normalized);
                combat.ExecuteAttack();
                yield return Wait(0.6f);
            }
            Log($"raider {(first == null || !first.IsAlive ? "defeated" : "still alive")}; wave active={spawner.IsWaveActive}, enemies left={spawner.ActiveEnemyCount}, broken fences={CountBrokenFences()}");
            // finish the remaining raiders off-camera so the clock (frozen during waves) resumes for the rest of the tour
            foreach (var e in FindObjectsByType<EnemyAgentView>()) if (e != null && e.IsAlive) e.TakeDamage(99999f, Vector2.right, 0f);
            yield return Wait(2.5f);
            Log($"after clean-up: wave active={spawner.IsWaveActive}, combat clock frozen={Engine.Clock.IsDuringCombatWave}, minor waves defeated={Engine.MinorWavesDefeated}");
        }

        private static int CountBrokenFences()
        {
            int n = 0;
            foreach (var f in FindObjectsByType<PixelFenceSegment>()) if (f.IsBroken) n++;
            return n;
        }

        private IEnumerator FightDemo()
        {
            var player = Player;
            var combat = PlayerCombatController.Instance;
            if (player == null || combat == null) { Warn("no player combat controller; skipping the fight"); yield break; }
            yield return GearAndSkillDemo();
            Say("Kẻ Cào Phá xông vào đồng", 3f);
            yield return Duel(EnemyTypeId.KeCaoPha, EnemyData.CreateKeCaoPha(), new Vector2(4.5f, 1.2f), "crawler");
            Say("Kẻ Bắt Heo áo mưa", 3f);
            yield return Duel(EnemyTypeId.KeBatHeo, EnemyData.CreateKeBatHeo(), new Vector2(-4.5f, -1.5f), "thief");
        }

        /// <summary>
        /// GDD 8/12 on camera: the character panel (C), wearing boots next to the Dao Rựa opens Thế Công, learning
        /// Phá Giáp with Điểm Võ, then the first duel opens with the skill (Z). Demo-only grant of 300G + 10 Điểm Võ.
        /// </summary>
        private IEnumerator GearAndSkillDemo()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) yield break;
            var c = engine.Character;
            Say("Nhân vật: trang bị, võ kỹ, tiềm năng (phím C)", 3f);
            CharacterPanel.Toggle();
            yield return Wait(2.5f);
            if (!c.OwnedEquipment.Contains("boot_giay_dinh")) c.OwnedEquipment.Add("boot_giay_dinh");
            Log("equip: " + MartialProgression.Equip(c, "boot_giay_dinh").message + $" (Công open={c.IsMartialSlotUnlocked(CombatSlotType.TheCong)})");
            c.MartialPoints = Math.Max(c.MartialPoints, 10);
            engine.Farm.Gold += 800;
            var learnId = c.PrimarySchool == MartialSchool.KhongChe_C ? "pig.skill.moc_keo" : "pig.skill.pha_giap";
            var learn = MartialProgression.Learn(engine, learnId);
            Log($"learn {learnId}: {learn.message}");
            CharacterPanel.Show(1);
            yield return Wait(2.5f);
            CharacterPanel.Show(2); // Phối võ: phái, hợp kích, ô năng lượng, lối chơi, vũ khí
            yield return Wait(3.5f);
            CharacterPanel.Toggle();
            demoSkillReady = learn.ok;
            // Lướt né (Ctrl): 9 Thể lực × lối chơi, miễn sát thương 0,25 giây
            var combatCtl = PlayerCombatController.Instance;
            if (combatCtl != null)
            {
                float before = c.Stamina.CurrentStamina;
                combatCtl.ExecuteDodge();
                yield return Wait(0.6f);
                Log($"dodge: stamina {before:0.0} -> {c.Stamina.CurrentStamina:0.0} (style {MartialProgression.PlaystyleName(c)})");
            }
        }

        private bool demoSkillReady;

        private IEnumerator Duel(EnemyTypeId type, EnemyData data, Vector2 offset, string label)
        {
            var player = Player;
            var combat = PlayerCombatController.Instance;
            if (player == null || combat == null) yield break;
            Vector2 spawn = (Vector2)player.transform.position + offset;
            var enemy = EnemyWaveSpawner.Instance != null
                ? EnemyWaveSpawner.Instance.SpawnEnemyAt(type, spawn)
                : SpawnEnemy(data, spawn);
            bool myth = enemy != null && enemy.GetComponent<EnemyMythAnimator>() != null;
            Log($"{label} spawned at {spawn} (PixelMyth clips: {myth})");
            yield return Wait(1.4f);
            for (int swing = 0; swing < 14 && enemy != null && enemy.IsAlive; swing++)
            {
                Vector2 to = enemy.Position - (Vector2)player.transform.position;
                if (to.magnitude > 1.6f)
                    yield return Walk($"close in on the {label}", enemy.Position - to.normalized * 1.1f, 0.5f, 4f);
                if (enemy == null || !enemy.IsAlive) break;
                player.SetFacingDirection((enemy.Position - (Vector2)player.transform.position).normalized);
                if (demoSkillReady)
                {
                    demoSkillReady = false;
                    player.ExecuteMartialSkill(CombatSlotType.TheCong);
                    Log($"skill Z at the {label} (hp {enemy.Model.CurrentHp:0}/{enemy.Model.MaxHp:0})");
                    yield return Wait(0.8f);
                    if (enemy == null || !enemy.IsAlive) break;
                }
                combat.ExecuteAttack();
                Log($"sword swing {swing + 1} at the {label} (hp {enemy.Model.CurrentHp:0}/{enemy.Model.MaxHp:0})");
                yield return Wait(0.6f);
            }
            if (enemy != null && enemy.IsAlive)
            {
                Warn($"{label} survived 14 swings; finishing it so the death clip plays");
                enemy.TakeDamage(9999f, Vector2.right, 0f);
            }
            Log($"{label} defeated");
            yield return Wait(2.4f);
        }

        private static EnemyAgentView SpawnEnemy(EnemyData data, Vector2 position)
        {
            var go = new GameObject($"Enemy_{data.TypeId}");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 100;
            go.AddComponent<CapsuleCollider2D>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = 6f;
            rb.freezeRotation = true;
            var agent = go.AddComponent<EnemyAgentView>();
            agent.Bind(data);
            return agent;
        }

        /// <summary>Walk up to a named NPC (PixelMythNpc, layout "npcs") and trigger its greeting bubble.</summary>
        private IEnumerator TalkToNpc(string id, string label)
        {
            PixelMythNpc npc = null;
            foreach (var n in PixelMythNpc.All) if (n != null && n.Id == id) { npc = n; break; }
            if (npc == null) { Warn($"npc {id} not in this scene; skipping the talk"); yield break; }
            yield return Walk("talk to " + label, npc.transform.position + new Vector3(0f, -1.1f, 0f), 1.0f, 14f);
            Face(((Vector2)npc.transform.position - (Vector2)Player.transform.position).normalized);
            npc.Greet(Player.transform.position);
            Log($"talk: {id} greets the heroine");
            yield return Wait(3.2f);
        }

        private void GrantExitCharm()
        {
            var engine = Engine;
            try
            {
                bool progress = engine.ForestAndEvents.Director.TryRecordScenarioProgress(50f);
                engine.Inventory.AddItem(ExitCharm);
                int index = -1;
                for (int i = 0; i < engine.Inventory.Slots.Count; i++)
                    if (engine.Inventory.Slots[i].ItemId == ExitCharm) { index = i; break; }
                string message = "charm not in warehouse";
                bool carried = index >= 0 && engine.SpaceBag.TryWithdrawOne(engine.Inventory, index, out message);
                var exit = engine.ForestAndEvents.Expedition.EvaluateExit(engine.Clock.CurrentDay, engine.SpaceBag, ExitCharm);
                Log($"exit charm: progress recorded={progress}, carried in SpaceBag={carried} ({message}), exit rule canExit={exit.canExit} reward {exit.rewardPercentTier}% ({exit.message})");
            }
            catch (Exception ex) { Warn("granting the exit charm failed: " + ex.Message); }
        }

        private void Interact(string label, CustomBuildingController station)
        {
            if (station == null) { Warn($"interact {label}: no CustomBuildingController found"); return; }
            Log($"interact {label}: CustomBuildingController.ExecuteInteraction ({station.AssignedFunction})");
            try { station.ExecuteInteraction(Player); }
            catch (Exception ex) { Warn($"interact {label} threw: {ex.Message}"); }
        }

        private void CloseOpenPopups()
        {
            try
            {
                if (BackpackPopup.IsOpen) { Log("closing storage popup"); BackpackPopup.Instance.Hide(); }
                if (DayMarketPopup.IsOpen) { Log("closing day market"); DayMarketPopup.Instance.Close(); }
                if (AuctionPopup.IsOpen) { Log("closing night stall"); AuctionPopup.Instance.Close(); }
                if (QuestTrackerPanel.Instance != null) QuestTrackerPanel.Instance.CloseJournal();
                if (RecruitPopup.IsOpen) { Log("closing recruit counter"); RecruitPopup.Instance.Close(); }
                if (ForestEventPopup.IsOpen) { Log("closing forest event"); ForestEventPopup.Instance.Close(); }
                if (AppraisalPopup.IsOpen) { Log("closing appraisal"); AppraisalPopup.Instance.Close(); }
                var barter = VillageBarterPopup.Instance;
                if (barter != null && barter.IsOpen) { Log("closing trade popup"); barter.Hide(); }
            }
            catch (Exception ex) { Warn("closing popups failed: " + ex.Message); }
        }

        private void WideShot()
        {
            Log("wide shot of the pasture");
            foreach (var follow in FindObjectsByType<CameraFollow2D>(FindObjectsSortMode.None))
            {
                follow.MinOrthoSize = follow.MaxOrthoSize = follow.TargetOrthoSize = 11.25f;
                var ppc = follow.GetComponent<PixelPerfectCamera>();
                if (ppc != null) { ppc.refResolutionX = 1280; ppc.refResolutionY = 720; } // 1x pixel scale, whole-pixel safe
            }
        }

        private void Finish(string reason)
        {
            driving = false;
            var recorder = DemoFrameRecorder.Instance;
            if (recorder != null) recorder.Finish(reason);
            else Application.Quit();
        }

        // ------------------------------------------------------------------ targets

        private struct Target
        {
            public Vector2 Position;
            public Vector2 Stand;
            public CustomBuildingController Building;
        }

        private Target Locate(string namePrefix, Vector2 fallback)
        {
            var result = new Target { Position = fallback, Stand = fallback + new Vector2(0f, -1.5f) };
            Transform found = null;
            foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (renderer.gameObject.name.StartsWith(namePrefix, StringComparison.Ordinal)) { found = renderer.transform; break; }
            if (found == null) { Warn($"object '{namePrefix}' not in scene; using baked layout position"); return result; }
            result.Position = found.position;
            result.Building = found.GetComponent<CustomBuildingController>();
            result.Stand = result.Building != null ? result.Building.DoorWorldPosition : result.Position + new Vector2(0f, -1.5f);
            Log($"target {namePrefix} at ({result.Position.x:0.0},{result.Position.y:0.0}) <- {found.name}");
            return result;
        }

        private static PixelWorldPortal FindPortal(string portalId)
        {
            foreach (var portal in FindObjectsByType<PixelWorldPortal>(FindObjectsSortMode.None))
                if (portal.PortalId == portalId) return portal;
            return null;
        }

        private static void InvokeBeginTravel(PixelWorldPortal portal)
        {
            var controller = FindFirstObjectByType<PixelWorldTravelController>();
            if (controller == null || BeginTravelMethod == null) { Debug.LogWarning("[Autopilot] travel controller unavailable"); return; }
            BeginTravelMethod.Invoke(controller, new object[] { portal });
        }

        private static bool Travelling
        {
            get
            {
                var controller = FindFirstObjectByType<PixelWorldTravelController>();
                return controller != null && TravellingField != null && (bool)TravellingField.GetValue(controller);
            }
        }

        private static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
        private static string CurrentLocation() => Engine != null ? Engine.WorldLocations.CurrentLocationId : string.Empty;
        private float Recorded => DemoFrameRecorder.Instance != null ? DemoFrameRecorder.Instance.GameSeconds : clock;

        private static PlayerMobileController Player
        {
            get
            {
                var p = PlayerMobileController.Instance;
                if (p != null && p.isActiveAndEnabled) return p;
                foreach (var c in FindObjectsByType<PlayerMobileController>(FindObjectsSortMode.None))
                    if (c.isActiveAndEnabled) return c;
                return null;
            }
        }

        // ------------------------------------------------------------------ walking

        private IEnumerator Wait(float seconds)
        {
            move = Vector2.zero;
            float until = clock + seconds;
            while (clock < until) yield return null;
        }

        private void Face(Vector2 direction)
        {
            move = Vector2.zero;
            if (Player != null) Player.SetFacingDirection(direction);
        }

        private IEnumerator Walk(string label, Vector2 goal, float stopWithin, float maxSeconds = 90f)
        {
            Log($"walk -> {label} ({goal.x:0.0},{goal.y:0.0})");
            float startedAt = clock;
            int sceneHandle = sceneSerial;
            List<Vector2> path = null;
            int index = 0;
            float bestRemaining = float.MaxValue, progressAt = clock;
            bool replanned = false;
            while (true)
            {
                if (Travelling || sceneSerial != sceneHandle) { move = Vector2.zero; yield break; }
                var player = Player;
                if (player == null || !player.CanMove) { move = Vector2.zero; yield return null; continue; }
                Vector2 position = player.transform.position;
                if (Vector2.Distance(position, goal) <= stopWithin) break;
                if (clock - startedAt > maxSeconds) { Warn($"walk -> {label}: timeout after {maxSeconds:0}s at ({position.x:0.0},{position.y:0.0})"); break; }
                if (path == null)
                {
                    path = Plan(player, position, goal);
                    index = 0;
                    if (path == null) { Warn($"walk -> {label}: no path found, steering straight"); path = new List<Vector2> { goal }; }
                    bestRemaining = float.MaxValue; progressAt = clock; replanned = false;
                }
                Vector2 waypoint = path[index];
                float distance = Vector2.Distance(position, waypoint);
                bool last = index == path.Count - 1;
                if (distance <= (last ? Mathf.Min(stopWithin, 0.4f) : 0.45f))
                {
                    if (last) break;
                    index++; bestRemaining = float.MaxValue; progressAt = clock;
                    continue;
                }
                move = (waypoint - position).normalized;
                if (distance < bestRemaining - 0.25f) { bestRemaining = distance; progressAt = clock; replanned = false; }
                else
                {
                    float stuckFor = clock - progressAt;
                    if (stuckFor > 3f)
                    {
                        Vector2 hop = index + 1 < path.Count ? path[index + 1] : waypoint;
                        Warn($"STUCK > 3 s at ({position.x:0.0},{position.y:0.0}) walking to {label}; short teleport to ({hop.x:0.0},{hop.y:0.0})");
                        Teleport(player, hop);
                        path = null;
                    }
                    else if (stuckFor > 1.5f && !replanned)
                    {
                        replanned = true;
                        Warn($"stuck at ({position.x:0.0},{position.y:0.0}) walking to {label}; replanning around it");
                        BlockAround(position + move * 0.8f);
                        var again = Plan(player, position, goal);
                        if (again != null) { path = again; index = 0; bestRemaining = float.MaxValue; progressAt = clock - 1.6f; }
                    }
                }
                yield return null;
            }
            move = Vector2.zero;
        }

        private static void Teleport(PlayerMobileController player, Vector2 position)
        {
            player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
            Physics2D.SyncTransforms();
        }

        // ------------------------------------------------------------------ planning

        private void PrepareGrid(PlayerMobileController player)
        {
            int handle = sceneSerial;
            selfBody = player.GetComponent<Rigidbody2D>();
            var collider = player.GetComponent<Collider2D>();
            feetOffset = collider != null ? (Vector2)collider.bounds.center - (Vector2)player.transform.position : new Vector2(0f, -0.35f);
            filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(player.gameObject.layer));
            if (handle == gridSceneHandle) return;
            gridSceneHandle = handle;
            passable.Clear();
            extraBlocked.Clear();
            var follow = FindFirstObjectByType<CameraFollow2D>();
            mapBounds = follow != null && follow.UseWorldBounds
                ? new Rect(follow.WorldBounds.x + 0.8f, follow.WorldBounds.y + 0.8f, follow.WorldBounds.width - 1.6f, follow.WorldBounds.height - 1.6f)
                : new Rect(-50, -50, 400, 400);
        }

        private static Vector2Int ToCell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.y / CellSize));
        private static Vector2 CellCenter(Vector2Int c) => new Vector2((c.x + 0.5f) * CellSize, (c.y + 0.5f) * CellSize);

        private bool IsStaticBlocker(Collider2D collider)
        {
            if (collider == null || collider.isTrigger) return false;
            var body = collider.attachedRigidbody;
            if (body != null && body == selfBody) return false;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic) return false; // pigs and other movers
            return true;
        }

        private bool IsFree(Vector2Int cell)
        {
            if (extraBlocked.Contains(cell)) return false;
            if (passable.TryGetValue(cell, out bool cached)) return cached;
            Vector2 center = CellCenter(cell);
            bool free = mapBounds.Contains(center);
            if (free)
            {
                int count = Physics2D.OverlapCircle(center + feetOffset, ProbeRadius, filter, overlapBuffer);
                for (int i = 0; i < count && free; i++)
                    if (IsStaticBlocker(overlapBuffer[i])) free = false;
            }
            passable[cell] = free;
            return free;
        }

        private bool LineClear(Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.01f) return true;
            int count = Physics2D.CircleCast(a + feetOffset, ProbeRadius * 0.9f, delta / length, filter, castBuffer, length);
            for (int i = 0; i < count; i++)
                if (IsStaticBlocker(castBuffer[i].collider)) return false;
            return true;
        }

        private void BlockAround(Vector2 point)
        {
            var center = ToCell(point);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    extraBlocked.Add(new Vector2Int(center.x + dx, center.y + dy));
        }

        private List<Vector2> Plan(PlayerMobileController player, Vector2 from, Vector2 to)
        {
            PrepareGrid(player);
            Physics2D.SyncTransforms();
            var start = ToCell(from);
            var goal = ToCell(to);
            if (!IsFree(goal))
            {
                bool found = false;
                for (int r = 1; r <= 12 && !found; r++)
                    for (int dx = -r; dx <= r && !found; dx++)
                        for (int dy = -r; dy <= r && !found; dy++)
                        {
                            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                            var candidate = new Vector2Int(goal.x + dx, goal.y + dy);
                            if (IsFree(candidate)) { goal = candidate; found = true; }
                        }
                if (!found) return null;
            }
            var cells = AStar(start, goal, 60) ?? AStar(start, goal, 1000);
            if (cells == null) return null;
            var points = new List<Vector2>(cells.Count);
            foreach (var c in cells) points.Add(CellCenter(c));
            return Smooth(from, points);
        }

        private List<Vector2> Smooth(Vector2 from, List<Vector2> points)
        {
            var result = new List<Vector2>();
            Vector2 anchor = from;
            int i = 0;
            while (i < points.Count)
            {
                int j = points.Count - 1;
                while (j > i && (Vector2.Distance(anchor, points[j]) > 10f || !LineClear(anchor, points[j]))) j--;
                result.Add(points[j]);
                anchor = points[j];
                i = j + 1;
            }
            return result;
        }

        private List<Vector2Int> AStar(Vector2Int start, Vector2Int goal, int pad)
        {
            int minX = Mathf.Min(start.x, goal.x) - pad, maxX = Mathf.Max(start.x, goal.x) + pad;
            int minY = Mathf.Min(start.y, goal.y) - pad, maxY = Mathf.Max(start.y, goal.y) + pad;
            var cost = new Dictionary<Vector2Int, float> { [start] = 0f };
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var closed = new HashSet<Vector2Int>();
            var open = new SortedSet<(float f, int id)>();
            var byId = new Dictionary<int, Vector2Int>();
            int nextId = 0;
            void Push(Vector2Int c, float f) { byId[nextId] = c; open.Add((f, nextId++)); }
            Push(start, Heuristic(start, goal));
            int expansions = 0;
            while (open.Count > 0 && expansions++ < 160000)
            {
                var top = open.Min;
                open.Remove(top);
                var current = byId[top.id];
                byId.Remove(top.id);
                if (!closed.Add(current)) continue;
                if (current == goal)
                {
                    var path = new List<Vector2Int> { current };
                    while (parent.TryGetValue(current, out var p)) { current = p; path.Add(current); }
                    path.Reverse();
                    return path;
                }
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var next = new Vector2Int(current.x + dx, current.y + dy);
                        if (next.x < minX || next.x > maxX || next.y < minY || next.y > maxY || closed.Contains(next)) continue;
                        if (!IsFree(next)) continue;
                        if (dx != 0 && dy != 0 && (!IsFree(new Vector2Int(current.x + dx, current.y)) || !IsFree(new Vector2Int(current.x, current.y + dy)))) continue;
                        float g = cost[current] + (dx != 0 && dy != 0 ? 1.4142f : 1f);
                        if (cost.TryGetValue(next, out float old) && old <= g) continue;
                        cost[next] = g;
                        parent[next] = current;
                        Push(next, g + 1.5f * Heuristic(next, goal)); // weighted: fast and good enough for a camera route
                    }
            }
            return null;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            return dx + dy + (1.4142f - 2f) * Mathf.Min(dx, dy);
        }

        // ------------------------------------------------------------------ logging / overlay

        private static void Log(string message) => Debug.Log("[Autopilot] " + message + $"  (t={(instance != null ? instance.Recorded : 0f):0.0}s)");
        private static void Warn(string message) => Debug.LogWarning("[Autopilot] " + message);

        private void Say(string text, float seconds)
        {
            Log("step: " + text);
            caption = text;
            captionUntil = clock + seconds;
        }

        private void OnGUI()
        {
            if (!captions || clock >= captionUntil || string.IsNullOrEmpty(caption)) return;
            if (captionStyle == null)
                captionStyle = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            float width = Mathf.Min(760f, Screen.width - 40f);
            GUI.Box(new Rect(20f, Screen.height - 76f, width, 56f), caption, captionStyle);
        }
    }
}
