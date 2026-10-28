using System;
using System.Collections.Generic;
using System.IO;
using PigTycoon.Core;
using PigTycoon.Presentation;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PigTycoon.PlayableDemo3D
{
    /// <summary>
    /// Isolated 3D presentation. Assign authored prefabs and terrain/colliders in the scene.
    /// Public hooks support deterministic standalone smoke input without transform teleportation.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayableFarmController : MonoBehaviour
    {
        [Header("Core owner")]
        public MobileGameController CoreController;
        public bool UseExistingCore = true;
        public bool OwnSimulationClock = true;
        public bool LoadDemoSaveOnStart = true;
        public bool FailOnInvalidExistingSave;
        public bool ShowHud = true;
        public bool UseConfiguredMap;
        public string ScenarioProfileId = "";
        public string RequiredTowerInstanceId = "";
        public string ScenarioLabel = "";
        public bool LoadedExistingSave { get; private set; }
        public PlayableMapConfiguration MapConfiguration = new PlayableMapConfiguration();
        public string SaveFileName = "pig-tycoon-demo3d.json";
        [Header("Authored actor slots (no generated fallback geometry)")]
        public Transform PlayerRoot;
        public CharacterController PlayerMotor;
        public GameObject KhoaPrefab, AnPrefab, PigPrefab;
        public Transform PigRoot;
        public Camera GameplayCamera;
        public Vector3 PlayerSpawn = new Vector3(0f, 0f, -8f);
        [Header("Scene geometry")]
        public LayerMask GroundMask = ~0;
        public Collider GroundCollider;
        public LayerMask ObstacleMask = ~0;
        public Collider[] NavigationObstacles = Array.Empty<Collider>();
        public float GroundProbeHeight = 40f;
        public float PigFootOffset;
        [Header("Camera")]
        public float CameraDistance = 14f;
        public float CameraPitch = 45f;
        public float CameraYaw;
        public float LookSensitivity = 0.18f;

        public PlayableFarmSession Session { get; private set; }
        public IReadOnlyDictionary<string, Transform> PigViews => pigViews;
        public IReadOnlyList<PlayableFarmStation> Stations => stations;
        public Vector2 MoveInput { get; private set; }
        public PlayableFarmStation NearestStation { get; private set; }
        public string NearestPigId { get; private set; } = "";
        public bool IsPaused => pauseMenu;
        public void SetPaused(bool paused)
        { pauseMenu = paused; if (paused) { ActualIsRunning = false; PushLocomotionState(); } PauseChanged?.Invoke(paused); }
        public bool ActualIsRunning { get; private set; }
        public LocomotionStep LastLocomotionStep { get; private set; }
        public event Action<PlayableFarmSession> SessionReady;
        public event Action SaveRestored;
        public event Action NavigationRebuilt;
        public event Action<bool> PauseChanged;

        private readonly Dictionary<string, Transform> pigViews = new Dictionary<string, Transform>();
        private readonly List<PlayableFarmStation> stations = new List<PlayableFarmStation>();
        private readonly RaycastHit[] collisionHits = new RaycastHit[24];
        private readonly List<string> stalePigIds = new List<string>();
        private GameObject playerModel;
        private PlayerId shownIdentity;
        private bool running, suppliedInput, initialized, pauseMenu, confirmSale;
        private float minuteTimer, verticalVelocity;
        private string message = "";
        private bool loggedMissingPig;
        private string saveDirectoryOverride = "";
        public PlayableFarmStation ActiveBuildingStation { get; private set; }
        public bool ShowBuildingTerminalModal { get; set; }
        public int ActiveTerminalTab { get; set; }
        private Vector2 terminalScroll;
        private Light directionalSunLight;
        private Texture2D hudPanelTex, hudModalTex, hudHeaderTex, hudAccentTex;
        private GUIStyle headerStyle, subHeaderStyle, boxPanelStyle, modalPanelStyle, tabButtonStyle, activeTabButtonStyle;
        public string SaveDirectory => saveDirectoryOverride.Length == 0 ? Application.persistentDataPath : saveDirectoryOverride;
        public string SavePath => Path.Combine(SaveDirectory, SaveFileName);
        public void ConfigureSaveDirectory(string absoluteDirectory)
        {
            if (initialized || Session != null) throw new InvalidOperationException("Configure save storage before initializing the canonical session.");
            saveDirectoryOverride = PlayableDemoLaunchOptions.RequireAbsoluteDirectory(absoluteDirectory);
        }

        private void Start()
        {
            if (initialized) return;
            if (CoreController == null && UseExistingCore) CoreController = MobileGameController.Instance;
            if (CoreController == null && UseExistingCore) CoreController = FindFirstObjectByType<MobileGameController>();
            if (CoreController != null && CoreController.Engine != null) Initialize(CoreController.Engine);
            else Initialize(new GameEngine());
            // A scene that wants the repository's sample scenario supplies MobileGameController.
            // An empty new engine remains empty: never invent a herd to match scenery.
        }

        public void Initialize(GameEngine engine)
        {
            if (initialized) throw new InvalidOperationException("The 3D controller already has a canonical engine.");
            if (UseConfiguredMap) engine.ConfigurePlayableMap(MapConfiguration);
            Session = new PlayableFarmSession(engine);
            Session.State.ScenarioProfileId = ScenarioProfileId;
            Session.RequiredTowerInstanceId = RequiredTowerInstanceId;
            if (CoreController != null && OwnSimulationClock) CoreController.enabled = false;
            bool restored = false;
            if (LoadDemoSaveOnStart && File.Exists(SavePath))
            {
                var loaded = TryLoad();
                restored = loaded.Success;
                if (!restored && FailOnInvalidExistingSave) throw new InvalidOperationException(loaded.Message);
            }
            LoadedExistingSave = restored;
            if (!restored)
            {
                engine.SwitchActiveProtagonist(PlayerId.Khoa);
                Session.SetPlayerPosition(PlayerSpawn.x, PlayerSpawn.z);
            }
            if (GameplayCamera == null) GameplayCamera = Camera.main;
            foreach (var station in FindObjectsByType<PlayableFarmStation>(FindObjectsSortMode.None)) RegisterStation(station);
            if (PigRoot == null) PigRoot = new GameObject("CorePigViews").transform;
            if (PlayerRoot == null) PlayerRoot = new GameObject("ControlledPlayer").transform;
            if (PlayerMotor == null) PlayerMotor = PlayerRoot.GetComponent<CharacterController>();
            if (PlayerMotor == null)
            {
                PlayerMotor = PlayerRoot.gameObject.AddComponent<CharacterController>();
                PlayerMotor.height = 1.8f; PlayerMotor.radius = 0.35f; PlayerMotor.center = Vector3.up * 0.9f;
            }
            PlacePlayerFromState();
            initialized = true;
            RebuildNavigation();
            RefreshViews(); SessionReady?.Invoke(Session);
        }

        public void RegisterStation(PlayableFarmStation station)
        {
            if (station == null || stations.Contains(station)) return;
            stations.Add(station);
            if (Session != null && station.Kind == FarmInteractionKind.Water) Session.RegisterWaterStation(station.Point.Id);
            if (Session != null && station.Kind == FarmInteractionKind.Feed) Session.RegisterFeedStation(station.Point);
            if (Session != null && station.Kind == FarmInteractionKind.Construction)
                Session.RegisterFence(station.Point.Id, station.Point.X, station.Point.Y, station.InitiallyBuilt,
                    station.CompletedFenceCollider == null ? 0.5f : new Vector2(station.CompletedFenceCollider.bounds.extents.x,
                        station.CompletedFenceCollider.bounds.extents.z).magnitude);
            RefreshFenceFootprint(station);
            if (Session?.Engine.PlayableMap != null && (station.Kind == FarmInteractionKind.Feed ||
                station.Kind == FarmInteractionKind.Water || station.ProvidesHerdShelter || station.ProvidesCooling))
            {
                var services = Session.Engine.GridZones.SceneServicePoints;
                if (!services.Exists(s => s.Id == station.Point.Id)) services.Add(new FarmServicePoint
                { Id = station.Point.Id, X = station.Point.X, Y = station.Point.Y, Feed = station.Kind == FarmInteractionKind.Feed,
                    Water = station.Kind == FarmInteractionKind.Water, Shelter = station.ProvidesHerdShelter, Cooling = station.ProvidesCooling });
                if (station.ProvidesHerdShelter) Session.RegisterDestination(FlowDestinationGroup.Shelter, station.Point);
            }
        }

        public void RebuildNavigation()
        {
            if (Session == null) return;
            var flow = Session.Engine.FlowField;
            foreach (var station in stations) if (station != null)
            { station.ApplyState(Session.State); RefreshFenceFootprint(station); }
            for (int x = 0; x < flow.Width; x++) for (int y = 0; y < flow.Height; y++) flow.SetBlocked(x, y, false);
            foreach (var obstacle in NavigationObstacles)
            {
                if (obstacle == null || !obstacle.enabled || !obstacle.gameObject.activeInHierarchy) continue;
                var b = obstacle.bounds;
                Session.SetNavigationObstacle(new MapRect(b.min.x, b.min.z, b.size.x, b.size.z), true);
            }
            foreach (var structure in Session.Engine.GridZones.Structures)
            {
                var center = structure.WorldCenter(Session.Engine.GridZones.OriginX, Session.Engine.GridZones.OriginY);
                float span = (int)structure.Size * FarmGridAndZones.BuildCellMeters;
                Session.SetNavigationObstacle(new MapRect(center.wx - span * 0.5f, center.wy - span * 0.5f, span, span), true);
            }
            foreach (var station in stations)
            {
                if (station == null) continue;
                var fenceCollider = station.Kind == FarmInteractionKind.Construction ? station.CompletedFenceCollider : null;
                if (fenceCollider != null && fenceCollider.enabled && fenceCollider.gameObject.activeInHierarchy)
                {
                    var bounds = fenceCollider.bounds;
                    Session.SetNavigationObstacle(new MapRect(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z), true);
                }
                if (station.Kind == FarmInteractionKind.Feed) Session.RegisterFeedStation(station.Point);
                if (station.Kind == FarmInteractionKind.Water) Session.RegisterDestination(FlowDestinationGroup.WaterTrough, station.Point);
            }
            NavigationRebuilt?.Invoke();
        }
        private void RefreshFenceFootprint(PlayableFarmStation station)
        {
            if (Session == null || station.Kind != FarmInteractionKind.Construction || !(station.CompletedFenceCollider is BoxCollider box)) return;
            // Use authored local box geometry even while disabled; Collider.bounds would be empty then.
            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 axisX = box.transform.TransformVector(Vector3.right), axisY = box.transform.TransformVector(Vector3.forward);
            float halfX = box.size.x * new Vector2(axisX.x, axisX.z).magnitude * 0.5f;
            float halfY = box.size.z * new Vector2(axisY.x, axisY.z).magnitude * 0.5f;
            if (halfX <= 0f || halfY <= 0f) return;
            Session.RegisterFence(station.Point.Id, center.x, center.z, station.InitiallyBuilt);
            Session.SetFenceBoxFootprint(station.Point.Id, center.x, center.z, halfX, halfY, Mathf.Atan2(axisX.z, axisX.x));
        }

        public bool RegisterPigView(string id, Transform view)
        {
            if (Session == null || Session.GetPig(id) == null || view == null || pigViews.ContainsKey(id)) return false;
            pigViews.Add(id, view);
            var actor = view.GetComponent<PlayableFarmActor>();
            if (actor == null) actor = view.gameObject.AddComponent<PlayableFarmActor>();
            actor.CorePigId = id;
            return true;
        }

        public void SetMoveInput(Vector2 input, bool run)
        { MoveInput = Vector2.ClampMagnitude(input, 1f); running = run; suppliedInput = true; }
        public void ReleaseMoveInput() { suppliedInput = false; MoveInput = Vector2.zero; running = false; }

        private void Update()
        {
            if (!initialized || Session == null) return;
            ReadInput();
            UpdateDynamicDayNightLighting();
            if (pauseMenu) return;
            AdvancePlayer(Time.deltaTime);
            Session.AdvanceMotion(Time.deltaTime, CanPigTraverse);
            if (OwnSimulationClock)
            {
                minuteTimer += Time.deltaTime;
                while (minuteTimer >= GameClock.SecondsPerGameMinute)
                { minuteTimer -= GameClock.SecondsPerGameMinute; Session.AdvanceSimulationMinutes(1); }
            }
            RefreshViews(); UpdateContext();
        }

        private void ReadInput()
        {
            if (!suppliedInput)
            {
#if ENABLE_INPUT_SYSTEM
                var keyboard = Keyboard.current;
                MoveInput = keyboard == null ? Vector2.zero : new Vector2(
                    (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                    (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                    (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0));
                MoveInput = Vector2.ClampMagnitude(MoveInput, 1f);
                running = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
                if (keyboard != null)
                {
                    if (keyboard.escapeKey.wasPressedThisFrame)
                    {
                        if (ActiveBuildingStation != null || ShowBuildingTerminalModal) ExitBuildingStation();
                        else SetPaused(!pauseMenu);
                    }
                    if (keyboard.eKey.wasPressedThisFrame && !pauseMenu) InteractNearest();
                    if (keyboard.gKey.wasPressedThisFrame && !pauseMenu) ToggleGuiding();
                    if (keyboard.qKey.wasPressedThisFrame && !pauseMenu) TryWhistle();
                    if (keyboard.bKey.wasPressedThisFrame && !pauseMenu) ShowBuildingTerminalModal = !ShowBuildingTerminalModal;
                    if (keyboard.digit1Key.wasPressedThisFrame && !pauseMenu) Report(Session.EquipWeaponBySlotKey(1));
                    if (keyboard.digit2Key.wasPressedThisFrame && !pauseMenu) Report(Session.EquipWeaponBySlotKey(2));
                    if (keyboard.digit3Key.wasPressedThisFrame && !pauseMenu) Report(Session.EquipWeaponBySlotKey(3));
                    if (keyboard.f5Key.wasPressedThisFrame) Report(TrySave());
                    if (keyboard.f9Key.wasPressedThisFrame) Report(TryLoad());
                    if (keyboard.tabKey.wasPressedThisFrame && !pauseMenu) SwitchProtagonist();
                }
                var mouse = Mouse.current;
                if (mouse != null && mouse.middleButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    CameraYaw += delta.x * LookSensitivity;
                    CameraPitch = Mathf.Clamp(CameraPitch - delta.y * LookSensitivity, 20f, 70f);
                }
                if (mouse != null && !MouseOverHud())
                    CameraDistance = Mathf.Clamp(CameraDistance - mouse.scroll.ReadValue().y * 0.01f, 6f, 28f);
#else
                MoveInput = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
                running = Input.GetKey(KeyCode.LeftShift);
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (ActiveBuildingStation != null || ShowBuildingTerminalModal) ExitBuildingStation();
                    else SetPaused(!pauseMenu);
                }
                if (Input.GetKeyDown(KeyCode.E)) InteractNearest();
                if (Input.GetKeyDown(KeyCode.G)) ToggleGuiding();
                if (Input.GetKeyDown(KeyCode.Q)) TryWhistle();
                if (Input.GetKeyDown(KeyCode.B)) ShowBuildingTerminalModal = !ShowBuildingTerminalModal;
                if (Input.GetKeyDown(KeyCode.Alpha1)) Report(Session.EquipWeaponBySlotKey(1));
                if (Input.GetKeyDown(KeyCode.Alpha2)) Report(Session.EquipWeaponBySlotKey(2));
                if (Input.GetKeyDown(KeyCode.Alpha3)) Report(Session.EquipWeaponBySlotKey(3));
                if (Input.GetKeyDown(KeyCode.F5)) Report(TrySave());
                if (Input.GetKeyDown(KeyCode.F9)) Report(TryLoad());
                if (Input.GetKeyDown(KeyCode.Tab)) SwitchProtagonist();
                if (Input.GetMouseButton(2)) { CameraYaw += Input.GetAxis("Mouse X") * 3f; CameraPitch = Mathf.Clamp(CameraPitch - Input.GetAxis("Mouse Y") * 3f, 20f, 70f); }
#endif
            }
        }

        public bool MouseOverHud()
        {
#if ENABLE_INPUT_SYSTEM
            if (!ShowHud || Mouse.current == null) return false;
            Vector2 p = Mouse.current.position.ReadValue();
            float guiY = Screen.height - p.y;
            if (p.x < 435f && guiY < 685f) return true;
            if (ShowBuildingTerminalModal && p.x >= 445f && p.x <= 1135f && guiY >= 14f && guiY <= 665f) return true;
            return false;
#else
            if (!ShowHud) return false;
            float guiY = Screen.height - Input.mousePosition.y;
            if (Input.mousePosition.x < 435f && guiY < 685f) return true;
            if (ShowBuildingTerminalModal && Input.mousePosition.x >= 445f && Input.mousePosition.x <= 1135f && guiY >= 14f && guiY <= 665f) return true;
            return false;
#endif
        }

        public void AdvancePlayer(float deltaSeconds)
        {
            ActualIsRunning = false;
            LastLocomotionStep = new LocomotionStep(0f, 0f, 0f, false);
            if (IsPaused || PlayerMotor == null || Session.Engine.Character.IsFainted || deltaSeconds <= 0f)
            { PushLocomotionState(); return; }
            var character = Session.Engine.Character;
            Vector3 forward = GameplayCamera == null ? Vector3.forward : GameplayCamera.transform.forward;
            forward.y = 0f; forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 direction = forward * MoveInput.y + right * MoveInput.x;
            float speed = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                var step = PlayerLocomotion.Advance(running, character.BaseStats.Agi, character.Stamina.CurrentStamina, deltaSeconds);
                LastLocomotionStep = step; ActualIsRunning = step.IsRunning;
                character.Stamina.ForceConsumeStamina(-step.StaminaDelta, MovementSpec.RunDailyLoadFraction);
                speed = step.MetersPerSecond * character.GetMoveSpeedMultiplier();
                PlayerRoot.rotation = Quaternion.RotateTowards(PlayerRoot.rotation,
                    Quaternion.LookRotation(direction), 720f * deltaSeconds);
                if (PlayerMotor.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
                verticalVelocity += Physics.gravity.y * deltaSeconds;
                Vector3 displacement = (direction * speed + Vector3.up * verticalVelocity) * deltaSeconds;
                var bounds = Session.Engine.PlayableMap?.MapBounds ?? FarmMainLayout.MapBounds;
                displacement.x = Mathf.Clamp(PlayerRoot.position.x + displacement.x, bounds.XMin + PlayerMotor.radius,
                    bounds.XMax - PlayerMotor.radius) - PlayerRoot.position.x;
                displacement.z = Mathf.Clamp(PlayerRoot.position.z + displacement.z, bounds.YMin + PlayerMotor.radius,
                    bounds.YMax - PlayerMotor.radius) - PlayerRoot.position.z;
                PlayerMotor.Move(displacement);
                Session.SetPlayerPosition(PlayerRoot.position.x, PlayerRoot.position.z);
            }
            PushLocomotionState();
        }

        private void PushLocomotionState()
        { if (playerModel != null) playerModel.GetComponent<PlayableFarmActor>()?.SetLocomotionState(ActualIsRunning); }

        private bool CanPigTraverse(float ax, float az, float bx, float bz)
            => CanTraverseActorSegment(ax, az, bx, bz, true);

        public bool CanTraverseActorSegment(float ax, float az, float bx, float bz, bool keepInMap)
        {
            var map = Session.Engine.PlayableMap?.MapBounds ?? FarmMainLayout.MapBounds;
            bool inside = map.Contains(bx, bz);
            if (keepInMap && !inside) return false;
            var cell = Session.Engine.GridZones.WorldToBuildCell(bx, bz);
            if (inside && Session.Engine.GridZones.EvaluatePlacement(cell.cx, cell.cy, BuildingFootprintSize.Small_1x1) == BuildPlacementStatus.Red_Forbidden)
                return false;
            float ay = GroundHeight(ax, az), by = GroundHeight(bx, bz);
            Vector3 origin = new Vector3(ax, ay + 0.5f, az), target = new Vector3(bx, by + 0.5f, bz);
            Vector3 delta = target - origin;
            int count = Physics.SphereCastNonAlloc(origin, 0.25f, delta.normalized, collisionHits,
                delta.magnitude, ObstacleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = collisionHits[i].collider;
                if (hit == null || hit.GetComponentInParent<PlayableFarmActor>() != null || hit == PlayerMotor) continue;
                return false;
            }
            return true;
        }

        public float GroundHeight(float x, float z)
        {
            Ray ray = new Ray(new Vector3(x, GroundProbeHeight, z), Vector3.down);
            if (GroundCollider != null && GroundCollider.Raycast(ray, out var ground, GroundProbeHeight * 2f)) return ground.point.y;
            int count = Physics.RaycastNonAlloc(ray, collisionHits, GroundProbeHeight * 2f, GroundMask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity, height = 0f;
            for (int i = 0; i < count; i++)
            {
                var hit = collisionHits[i];
                if (hit.collider == null || hit.collider is CharacterController || hit.collider.GetComponentInParent<PlayableFarmActor>() != null) continue;
                if (hit.distance < nearest) { nearest = hit.distance; height = hit.point.y; }
            }
            return height;
        }

        public int EquippedWeaponSlotIndex
        {
            get
            {
                if (Session?.Engine?.Character?.Equipment != null &&
                    Session.Engine.Character.Equipment.TryGetValue(EquipSlot.Weapon, out var wpn) && wpn != null)
                {
                    if (wpn.Id == "wpn_bua_ta") return 1;
                    if (wpn.Id == "wpn_giao_tre") return 2;
                }
                return 0;
            }
        }

        public void RefreshViews()
        {
            if (Session == null) return;
            stalePigIds.Clear();
            foreach (var pair in pigViews) if (Session.GetPig(pair.Key) == null || pair.Value == null) stalePigIds.Add(pair.Key);
            foreach (string id in stalePigIds)
            { if (pigViews[id] != null) Destroy(pigViews[id].gameObject); pigViews.Remove(id); }
            foreach (var pig in Session.Engine.Pigs)
            {
                if (!pig.IsAlive) continue;
                if (!pigViews.TryGetValue(pig.Id, out var view))
                {
                    if (PigPrefab == null)
                    { if (!loggedMissingPig) { Debug.LogWarning("[PlayableDemo3D] Assign the approved pig prefab; no fallback art is created."); loggedMissingPig = true; } continue; }
                    view = Instantiate(PigPrefab, PigRoot).transform;
                    view.name = "Pig_" + pig.Id; RegisterPigView(pig.Id, view);
                }
                view.localScale = Vector3.one * 1.28f;
                Vector3 next = new Vector3(pig.PositionX, GroundHeight(pig.PositionX, pig.PositionY) + PigFootOffset, pig.PositionY);
                Vector3 motion = next - view.position; motion.y = 0f;
                if (motion.sqrMagnitude > 0.0001f) view.rotation = Quaternion.LookRotation(motion);
                view.position = next;
            }
            if (PlayerRoot != null && (playerModel == null || shownIdentity != Session.Engine.Character.Identity)) ReplacePlayerModel();
            if (playerModel != null)
                playerModel.GetComponent<PlayableFarmActor>()?.SetEquippedWeaponVisual(EquippedWeaponSlotIndex);
            bool navigationChanged = false;
            foreach (var station in stations) if (station != null) navigationChanged |= station.ApplyState(Session.State);
            if (navigationChanged) RebuildNavigation();
        }

        private void ReplacePlayerModel()
        {
            var prefab = Session.Engine.Character.Identity == PlayerId.Khoa ? KhoaPrefab : AnPrefab;
            if (prefab == null) return;
            shownIdentity = Session.Engine.Character.Identity;
            if (playerModel != null) Destroy(playerModel);
            playerModel = Instantiate(prefab, PlayerRoot);
            playerModel.transform.localPosition = Vector3.zero;
            playerModel.transform.localRotation = Quaternion.identity;
            var actor = playerModel.GetComponent<PlayableFarmActor>();
            if (actor == null) actor = playerModel.AddComponent<PlayableFarmActor>();
            actor.SetEquippedWeaponVisual(EquippedWeaponSlotIndex);
        }

        private void UpdateContext()
        {
            NearestStation = null; NearestPigId = "";
            float nearest = float.PositiveInfinity;
            foreach (var station in stations)
            {
                if (station == null || !Session.CanInteract(station.Point)) continue;
                float distance = Vector2.Distance(new Vector2(Session.PlayerX, Session.PlayerY), new Vector2(station.Point.X, station.Point.Y));
                if (distance < nearest) { NearestStation = station; nearest = distance; }
            }
            nearest = 2.5f;
            foreach (var pig in Session.Engine.Pigs)
            {
                if (!pig.IsAlive) continue;
                float distance = Vector2.Distance(new Vector2(Session.PlayerX, Session.PlayerY), new Vector2(pig.PositionX, pig.PositionY));
                if (distance < nearest) { NearestPigId = pig.Id; nearest = distance; }
            }
        }

        public FarmActionResult SelectPig(string id) { var result = Session.InspectPig(id); Report(result); return result; }
        public FarmActionResult TryWhistle()
        {
            if (IsPaused) return Report(new FarmActionResult(false, "Đang tạm nghỉ."));
            var result = Session.StartWhistle();
            if (result.Success && playerModel != null) playerModel.GetComponent<PlayableFarmActor>()?.PlayWhistle();
            return Report(result);
        }

        public FarmActionResult InteractNearest()
        {
            if (IsPaused) return Report(new FarmActionResult(false, "Đang tạm nghỉ."));
            UpdateContext();
            var station = NearestStation;
            if (station == null) return string.IsNullOrEmpty(NearestPigId) ? Report(new FarmActionResult(false, "Đến gần heo hoặc một điểm tương tác.")) : SelectPig(NearestPigId);
            FarmActionResult result;
            switch (station.Kind)
            {
                case FarmInteractionKind.Feed: result = Session.FeedPig(station.Point, SelectedOrNearest()); PlayPigEat(result); break;
                case FarmInteractionKind.Water: result = Session.ChangeWater(station.Point); break;
                case FarmInteractionKind.Care: result = Session.BrushPig(SelectedOrNearest()); break;
                case FarmInteractionKind.Construction: result = Session.PlaceTemporaryFence(station.Point); break;
                case FarmInteractionKind.Defense: result = Session.Preload(station.Point); break;
                case FarmInteractionKind.Save: result = TrySave(); break;
                case FarmInteractionKind.Load: result = TryLoad(); break;
                case FarmInteractionKind.Sleep:
                    result = Session.Sleep(station.Point); break;
                case FarmInteractionKind.Market:
                    confirmSale = false; result = new FarmActionResult(true, "Chợ ngày: dẫn heo tới quầy, kiểm tra báo giá rồi xác nhận bán."); break;
                case FarmInteractionKind.NightMarket:
                    result = new FarmActionResult(true, "Quầy đạn chợ đêm mở từ 18:00 đến 06:00."); break;
                case FarmInteractionKind.Capacity:
                    var c = Session.Capacity; result = new FarmActionResult(true, $"Sức chứa {c.EffectiveCapacity} · Điểm nghẽn {c.Bottleneck}"); break;
                case FarmInteractionKind.Dialogue:
                    var npc = station.GetComponentInParent<PlayableFarmNpcView>();
                    result = npc != null ? npc.Talk(Session) : new FarmActionResult(false, "Chưa có người đối thoại tại đây."); break;
                case FarmInteractionKind.MainHouseHub:
                    EnterOrToggleBuildingStation(station, 0);
                    result = new FarmActionResult(true, "Đã vào Đại Sảnh Nhà Chính (nâng cấp, lưu/tải, nghỉ ngơi)."); break;
                case FarmInteractionKind.WarehouseTerminal:
                    EnterOrToggleBuildingStation(station, 1);
                    result = new FarmActionResult(true, "Đã mở Kho Nông Sản & Vật Tư (36 ô, nâng cấp sức chứa)."); break;
                case FarmInteractionKind.ResearchLab:
                    EnterOrToggleBuildingStation(station, 2);
                    result = new FarmActionResult(true, "Đã vào Phòng Nghiên Cứu Sinh Học & Phối Giống F1/F2."); break;
                case FarmInteractionKind.MedicalTent:
                    EnterOrToggleBuildingStation(station, 3);
                    result = new FarmActionResult(true, "Đã vào Lều Quân Y Thú Y (5 động từ chăm sóc & điều trị)."); break;
                case FarmInteractionKind.WorkshopTerminal:
                    EnterOrToggleBuildingStation(station, 4);
                    result = new FarmActionResult(true, "Đã vào Xưởng Cơ Khí & Cường Hóa Vũ Khí."); break;
                case FarmInteractionKind.ObservatoryIntel:
                    EnterOrToggleBuildingStation(station, 5);
                    result = Session.ScanObservatoryIntel(); break;
                case FarmInteractionKind.HarborTownHub:
                    EnterOrToggleBuildingStation(station, 6);
                    result = new FarmActionResult(true, "Đã mở Trung Tâm Giao Thương Thị Trấn Cảng."); break;
                default: result = new FarmActionResult(true, station.DisplayName); break;
            }
            if (result.Success && playerModel != null) playerModel.GetComponent<PlayableFarmActor>()?.PlayInteraction();
            RefreshViews();
            if (result.Success && station.Kind == FarmInteractionKind.Construction) RebuildNavigation();
            return Report(result);
        }

        public void EnterOrToggleBuildingStation(PlayableFarmStation station, int tabIndex = -1)
        {
            if (station == null) { ExitBuildingStation(); return; }
            ActiveBuildingStation = station;
            ShowBuildingTerminalModal = true;
            if (tabIndex >= 0) ActiveTerminalTab = tabIndex;
        }

        public void ExitBuildingStation()
        {
            ActiveBuildingStation = null;
            ShowBuildingTerminalModal = false;
        }

        public void InspectBuildingByKind(FarmInteractionKind kind, int tabIndex)
        {
            ActiveTerminalTab = tabIndex;
            ShowBuildingTerminalModal = true;
            foreach (var st in stations)
            {
                if (st != null && st.Kind == kind && st.HasInteriorView)
                {
                    ActiveBuildingStation = st;
                    message = $"Đang quan sát & vận hành: {st.DisplayName}";
                    return;
                }
            }
        }

        public FarmActionResult ReportExternalAction(FarmActionResult result) => Report(result);

        public void UpdateDynamicDayNightLighting()
        {
            if (Session == null) return;
            if (directionalSunLight == null) directionalSunLight = RenderSettings.sun;

            float hour = Session.Engine.Clock.CurrentHour + Session.Engine.Clock.CurrentMinute / 60f;
            Color sunCol;
            float sunIntensity;
            Quaternion sunRot;
            Color skyAmb, eqAmb, gndAmb, fogCol;

            if (hour >= 6f && hour < 16.5f)
            {
                // Golden-Morning & Crisp Meadow Daylight (matches Image 5 reference)
                float t = Mathf.Clamp01((hour - 6f) / 4f);
                sunCol = Color.Lerp(new Color(1.0f, 0.90f, 0.76f, 1f), new Color(1.0f, 0.95f, 0.86f, 1f), t);
                sunIntensity = Mathf.Lerp(0.95f, 1.05f, t);
                sunRot = Quaternion.Euler(Mathf.Lerp(28f, 38f, t), -42f, 0f);
                skyAmb = new Color(0.66f, 0.74f, 0.86f, 1f);
                eqAmb = new Color(0.58f, 0.56f, 0.52f, 1f);
                gndAmb = new Color(0.38f, 0.34f, 0.29f, 1f);
                fogCol = new Color(0.78f, 0.84f, 0.90f, 1f);
            }
            else if (hour >= 16.5f && hour < 19.5f)
            {
                // Warm Sunset / Dusk
                float t = Mathf.Clamp01((hour - 16.5f) / 3.0f);
                sunCol = Color.Lerp(new Color(1.0f, 0.92f, 0.80f, 1f), new Color(1.0f, 0.62f, 0.36f, 1f), t);
                sunIntensity = Mathf.Lerp(1.02f, 0.68f, t);
                sunRot = Quaternion.Euler(Mathf.Lerp(36f, 15f, t), Mathf.Lerp(-42f, -72f, t), 0f);
                skyAmb = Color.Lerp(new Color(0.66f, 0.74f, 0.86f, 1f), new Color(0.48f, 0.42f, 0.54f, 1f), t);
                eqAmb = Color.Lerp(new Color(0.58f, 0.56f, 0.52f, 1f), new Color(0.46f, 0.36f, 0.32f, 1f), t);
                gndAmb = Color.Lerp(new Color(0.38f, 0.34f, 0.29f, 1f), new Color(0.24f, 0.20f, 0.18f, 1f), t);
                fogCol = Color.Lerp(new Color(0.78f, 0.84f, 0.90f, 1f), new Color(0.52f, 0.44f, 0.48f, 1f), t);
            }
            else
            {
                // Starlit Night with clear silver-blue moonlight & glowing building windows/doorways
                sunCol = new Color(0.58f, 0.72f, 0.95f, 1f);
                sunIntensity = 0.44f;
                sunRot = Quaternion.Euler(46f, 128f, 0f);
                skyAmb = new Color(0.22f, 0.28f, 0.42f, 1f);
                eqAmb = new Color(0.18f, 0.22f, 0.32f, 1f);
                gndAmb = new Color(0.12f, 0.14f, 0.18f, 1f);
                fogCol = new Color(0.16f, 0.22f, 0.34f, 1f);
            }

            if (directionalSunLight != null)
            {
                directionalSunLight.color = sunCol;
                directionalSunLight.intensity = sunIntensity;
                directionalSunLight.transform.rotation = sunRot;
            }
            Vector3 sunDirToLight = -(sunRot * Vector3.forward);
            Shader.SetGlobalVector("_PigTycoonSunDir", new Vector4(sunDirToLight.x, sunDirToLight.y, sunDirToLight.z, 0f));
            Color effSun = sunCol * sunIntensity;
            Shader.SetGlobalVector("_PigTycoonSunColor", new Vector4(effSun.r, effSun.g, effSun.b, 1f));

            RenderSettings.ambientSkyColor = skyAmb;
            RenderSettings.ambientEquatorColor = eqAmb;
            RenderSettings.ambientGroundColor = gndAmb;
            RenderSettings.fogColor = fogCol;
            if (GameplayCamera != null) GameplayCamera.backgroundColor = fogCol;
        }

        private string SelectedOrNearest() => string.IsNullOrEmpty(Session.SelectedPigId) ? NearestPigId : Session.SelectedPigId;
        private void PlayPigEat(FarmActionResult result)
        {
            if (result.Success && pigViews.TryGetValue(SelectedOrNearest(), out var view)) view.GetComponent<PlayableFarmActor>()?.PlayInteraction(true);
        }
        private void ToggleGuiding()
        {
            if (Session.FollowingPigIds.Count > 0) { Session.StopGuiding(); message = "Đã dừng lùa đàn."; return; }
            Report(Session.StartGuiding(SelectedOrNearest()));
        }
        public void SwitchProtagonist()
        {
            if (IsPaused) return;
            if (Session.Engine.Clock.IsDuringCombatWave) { message = "Đổi nhân vật sau đợt quái."; return; }
            bool toAn = Session.Engine.Character.Identity == PlayerId.Khoa;
            if ((toAn ? AnPrefab : KhoaPrefab) == null) { message = "Nhân vật này chưa sẵn sàng trong cảnh hiện tại."; return; }
            Session.StopGuiding();
            Session.Engine.SwitchActiveProtagonist(toAn ? PlayerId.An : PlayerId.Khoa);
            RefreshViews();
        }

        public FarmActionResult TrySave()
        {
            if (Session == null) return new FarmActionResult(false, "Chưa bắt đầu ngày trại.");
            if (Session.Engine.Clock.IsDuringCombatWave) return new FarmActionResult(false, "Lưu sau khi kết thúc đợt quái.");
            try
            {
                Directory.CreateDirectory(SaveDirectory);
                string temp = SavePath + ".tmp"; File.WriteAllText(temp, Session.CaptureSave());
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, SavePath + ".backup"); else File.Move(temp, SavePath);
                return new FarmActionResult(true, "Đã lưu ngày trại.");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { Debug.LogError(ex); return new FarmActionResult(false, "Chưa thể ghi file lưu."); }
        }
        public FarmActionResult TryLoad()
        {
            if (Session == null || !File.Exists(SavePath)) return new FarmActionResult(false, "Chưa có file lưu demo.");
            if (Session.Engine.Clock.IsDuringCombatWave) return new FarmActionResult(false, "Tải sau khi kết thúc đợt quái.");
            try
            {
                string json = File.ReadAllText(SavePath);
                if (PlayableFarmSession.TryReadSavedProtagonist(json, out var identity) && (identity == PlayerId.An ? AnPrefab : KhoaPrefab) == null)
                    return new FarmActionResult(false, "Nhân vật trong file lưu chưa sẵn sàng ở cảnh hiện tại.");
                var result = Session.RestoreSave(json);
                if (result.Success && initialized)
                { PlacePlayerFromState(); minuteTimer = 0f; RebuildNavigation(); RefreshViews(); SaveRestored?.Invoke(); }
                return result;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { Debug.LogError(ex); return new FarmActionResult(false, "Chưa thể đọc file lưu."); }
        }
        private void PlacePlayerFromState()
        {
            if (PlayerRoot == null) return;
            bool motorEnabled = PlayerMotor != null && PlayerMotor.enabled;
            if (motorEnabled) PlayerMotor.enabled = false;
            float skin = PlayerMotor != null ? PlayerMotor.skinWidth : 0f;
            PlayerRoot.position = new Vector3(Session.PlayerX, GroundHeight(Session.PlayerX, Session.PlayerY) + skin, Session.PlayerY);
            if (motorEnabled) PlayerMotor.enabled = true;
            verticalVelocity = 0f;
            ActualIsRunning = false;
            if (playerModel != null) playerModel.GetComponent<PlayableFarmActor>()?.ResetMotionSample();
        }
        public void RescuePlayerToHouse()
        { Session.SetPlayerPosition(PlayerSpawn.x, PlayerSpawn.z); PlacePlayerFromState(); }
        private FarmActionResult Report(FarmActionResult result) { message = result.Message; return result; }

        private void LateUpdate()
        {
            if (!initialized || PlayerRoot == null || GameplayCamera == null) return;
            if (ActiveBuildingStation != null && ActiveBuildingStation.HasInteriorView)
            {
                Vector3 camPos = ActiveBuildingStation.InspectionCameraPosition;
                Vector3 lookDir = ActiveBuildingStation.InspectionCameraLookAt - camPos;
                Quaternion camRot = lookDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(lookDir.normalized, Vector3.up) : Quaternion.Euler(22f, 0f, 0f);
                GameplayCamera.transform.SetPositionAndRotation(camPos, camRot);
                return;
            }
            Quaternion rotation = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            Vector3 target = PlayerRoot.position + Vector3.up;
            GameplayCamera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * CameraDistance, rotation);
        }
        private void OnApplicationPause(bool paused) { if (paused && initialized) TrySave(); }
        private void OnApplicationQuit() { if (initialized) TrySave(); }

        private void EnsureGuiStyles()
        {
            if (boxPanelStyle != null && hudPanelTex != null) return;
            hudPanelTex = MakeSolidTexture(new Color(0.11f, 0.10f, 0.09f, 0.88f));
            hudModalTex = MakeSolidTexture(new Color(0.14f, 0.12f, 0.10f, 0.94f));
            hudHeaderTex = MakeSolidTexture(new Color(0.26f, 0.20f, 0.12f, 0.95f));
            hudAccentTex = MakeSolidTexture(new Color(0.76f, 0.56f, 0.20f, 0.95f));

            boxPanelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                normal = { background = hudPanelTex, textColor = new Color(0.96f, 0.93f, 0.86f, 1f) }
            };
            modalPanelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12),
                normal = { background = hudModalTex, textColor = new Color(0.97f, 0.94f, 0.88f, 1f) }
            };
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.84f, 0.44f, 1f) }
            };
            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.86f, 0.92f, 0.76f, 1f) }
            };
            tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { background = hudHeaderTex, textColor = new Color(0.92f, 0.88f, 0.78f, 1f) }
            };
            activeTabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { background = hudAccentTex, textColor = new Color(0.10f, 0.08f, 0.05f, 1f) }
            };
        }

        private static Texture2D MakeSolidTexture(Color col)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { col, col, col, col });
            tex.Apply();
            return tex;
        }

        private static void DrawSolidRect(Rect r, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static void DrawStylizedBar(Rect r, float ratio, Color fillCol, string labelText, GUIStyle textStyle)
        {
            DrawSolidRect(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), new Color(0.78f, 0.62f, 0.28f, 0.85f));
            DrawSolidRect(r, new Color(0.08f, 0.08f, 0.10f, 0.94f));
            float fillW = Mathf.Max(0f, (r.width - 2f) * Mathf.Clamp01(ratio));
            if (fillW > 0.5f)
                DrawSolidRect(new Rect(r.x + 1f, r.y + 1f, fillW, r.height - 2f), fillCol);
            GUI.Label(r, labelText, textStyle);
        }

        private void OnGUI()
        {
            if (!ShowHud || Session == null) return;
            EnsureGuiStyles();
            GUI.skin.label.wordWrap = true;
            var engine = Session.Engine;
            Camera cam = GameplayCamera != null ? GameplayCamera : Camera.main;

            // 1. World-Space Floating Interaction Prompt [E] & Pig Status Badges
            if (cam != null && ActiveBuildingStation == null)
            {
                GUIStyle badgeStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                badgeStyle.normal.textColor = new Color(1.0f, 0.96f, 0.84f);

                if (NearestStation != null)
                {
                    Vector3 stPos = NearestStation.transform.position + Vector3.up * 1.95f;
                    Vector3 sp = cam.WorldToScreenPoint(stPos);
                    if (sp.z > 0.2f)
                    {
                        float bw = 220f;
                        float bx = sp.x - bw * 0.5f;
                        float by = (Screen.height - sp.y) - 24f;
                        DrawSolidRect(new Rect(bx - 2f, by - 2f, bw + 4f, 28f), new Color(0.95f, 0.76f, 0.26f, 0.94f));
                        DrawSolidRect(new Rect(bx, by, bw, 24f), new Color(0.10f, 0.12f, 0.16f, 0.94f));
                        GUI.Label(new Rect(bx, by + 2f, bw, 20f), $"[E] {NearestStation.DisplayName}", badgeStyle);
                    }
                }

                foreach (var pig in engine.Pigs)
                {
                    if (!pig.IsAlive) continue;
                    Vector3 pigWS = new Vector3(pig.PositionX, GroundHeight(pig.PositionX, pig.PositionY) + 1.15f, pig.PositionY);
                    if (PlayerRoot != null && Vector3.Distance(PlayerRoot.position, pigWS) > 16f) continue;
                    Vector3 sp = cam.WorldToScreenPoint(pigWS);
                    if (sp.z <= 0.2f) continue;
                    bool isSel = pig.Id == Session.SelectedPigId;
                    float bw = isSel ? 175f : 138f;
                    float bx = sp.x - bw * 0.5f;
                    float by = (Screen.height - sp.y) - 22f;
                    DrawSolidRect(new Rect(bx - 1f, by - 1f, bw + 2f, 22f), isSel ? new Color(0.98f, 0.80f, 0.28f, 0.92f) : new Color(0.22f, 0.18f, 0.12f, 0.78f));
                    DrawSolidRect(new Rect(bx, by, bw, 20f), new Color(0.12f, 0.11f, 0.09f, 0.88f));
                    GUI.Label(new Rect(bx, by + 1f, bw, 18f), $"🐖 {PlayableFarmSession.DisplayPigName(pig)} · {pig.WeightKg:0.0}kg", badgeStyle);
                }
            }

            // 2. Left-Side Stylized RPG & Farm Operations HUD Panel
            Rect mainRect = new Rect(14f, 14f, 424f, Mathf.Min(Screen.height - 84f, 650f));
            DrawSolidRect(new Rect(mainRect.x - 2f, mainRect.y - 2f, mainRect.width + 4f, mainRect.height + 4f), new Color(0.82f, 0.64f, 0.26f, 0.90f));
            GUILayout.BeginArea(mainRect, boxPanelStyle);

            string phaseTag = engine.Clock.CurrentHour >= 19 || engine.Clock.CurrentHour < 6 ? "🌙 Đêm Sao"
                : (engine.Clock.CurrentHour >= 16 ? "🌅 Hoàng Hôn" : "☀ Ban Ngày");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"👑 {engine.Character.Name.ToUpperInvariant()} · Ngày {engine.Clock.CurrentDay} · {engine.Clock.CurrentHour:00}:{engine.Clock.CurrentMinute:00} ({phaseTag})", headerStyle);
            if (AnPrefab != null && KhoaPrefab != null && GUILayout.Button("Đổi An/Khoa [Tab]", activeTabButtonStyle, GUILayout.Width(122f)))
                SwitchProtagonist();
            GUILayout.EndHorizontal();

            string wpnName = Session.EquippedWeaponDisplayName;
            int wpnEnh = engine.DeepMechanics.intEquippedPaperEnhancementLevel;
            int livePigCount = engine.Pigs.FindAll(p => p.IsAlive).Count;
            GUILayout.Label($"💰 Vàng: {engine.Farm.Gold}G   |   🌾 Cám: {engine.Farm.FeedStockKg:0.0} kg   |   🐖 Đàn heo: {livePigCount}/{Session.Capacity.EffectiveCapacity} con", subHeaderStyle);

            // Graphical Vitality Bars (HP, Stamina, Guard)
            GUIStyle barTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            Rect barArea = GUILayoutUtility.GetRect(390f, 56f);
            float hpRatio = engine.Character.Hp / Mathf.Max(1f, engine.Character.MaxHp);
            float staRatio = engine.Character.Stamina.CurrentStamina / Mathf.Max(1f, engine.Character.Stamina.EffectiveMaxStamina);
            float grdRatio = engine.Character.CurrentGuard / Mathf.Max(1f, engine.Character.MaxGuard);
            DrawStylizedBar(new Rect(barArea.x, barArea.y + 2f, barArea.width, 16f), hpRatio, new Color(0.84f, 0.22f, 0.22f, 1f),
                $"♥ SINH LỰC (HP): {engine.Character.Hp:0}/{engine.Character.MaxHp:0}", barTextStyle);
            DrawStylizedBar(new Rect(barArea.x, barArea.y + 20f, barArea.width, 16f), staRatio, new Color(0.20f, 0.68f, 0.36f, 1f),
                $"⚡ THỂ LỰC (STA): {engine.Character.Stamina.CurrentStamina:0}/{engine.Character.Stamina.EffectiveMaxStamina:0}", barTextStyle);
            DrawStylizedBar(new Rect(barArea.x, barArea.y + 38f, barArea.width, 16f), grdRatio, new Color(0.84f, 0.64f, 0.18f, 1f),
                $"🛡 HỘ THỂ (PARRY): {engine.Character.CurrentGuard:0}/{engine.Character.MaxGuard:0}  ·  Vũ khí: {wpnName} +{wpnEnh}", barTextStyle);

            GUILayout.Space(3f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(ShowBuildingTerminalModal ? "Đóng 7 Công Trình [B]" : "🏯 Mở 7 Công Trình [B]", activeTabButtonStyle))
                ShowBuildingTerminalModal = !ShowBuildingTerminalModal;
            var raidView = GetComponent<PlayableFarmRaidView>();
            if (raidView != null && GUILayout.Button("⚔ Thực Chiến Quái [V]", tabButtonStyle, GUILayout.Width(148f)))
                raidView.SpawnImmediateSkirmish();
            if (ActiveBuildingStation != null && GUILayout.Button("Quay Về [Esc]", GUILayout.Width(95f)))
                ExitBuildingStation();
            GUILayout.EndHorizontal();

            GUILayout.Space(3f);
            GUILayout.Label("💬 " + message, subHeaderStyle);
            if (pauseMenu) { GUILayout.Label("⏸ Đang Tạm Nghỉ"); if (GUILayout.Button("Tiếp tục")) SetPaused(false); }
            else
            {
                if (!string.IsNullOrEmpty(NearestPigId) && GUILayout.Button("🔍 Kiểm tra heo gần nhất")) SelectPig(NearestPigId);
                if (!string.IsNullOrEmpty(Session.SelectedPigId))
                {
                    var selected = Session.GetPig(Session.SelectedPigId);
                    if (selected != null) GUILayout.Label($"🐖 Đang chọn: {PlayableFarmSession.DisplayPigName(selected)} [{selected.Id}] · {selected.Stage} · {selected.WeightKg:0.0} kg");
                    if (GUILayout.Button("🚩 Lùa / dừng lùa heo [G]")) ToggleGuiding();
                }
                if (NearestStation != null)
                {
                    GUILayout.Label("📍 Điểm gần nhất: " + NearestStation.DisplayName, subHeaderStyle);
                    if (NearestStation.Kind == FarmInteractionKind.Dialogue)
                    {
                        var npc = NearestStation.GetComponentInParent<PlayableFarmNpcView>();
                        if (npc != null) GUILayout.Label(npc.EmploymentStatus);
                    }
                    if (GUILayout.Button("✋ [E] Tương tác ngay")) InteractNearest();
                    if (NearestStation.Kind == FarmInteractionKind.Market) DrawMarket();
                    if (NearestStation.Kind == FarmInteractionKind.Defense && Session.State.RecoverableArrowTenths >= 10 &&
                        GUILayout.Button("Thu gom tên sau giao tranh")) Report(Session.CollectRecoveredArrows(NearestStation.Point));
                    if (NearestStation.Kind == FarmInteractionKind.NightMarket && GUILayout.Button("Mua 10 Tên Thép từ ô hàng nhận vàng"))
                        Report(Session.BuySteelArrows(NearestStation.Point, 10));
                }
                GUILayout.Space(4f);
                var c = Session.Capacity;
                GUILayout.Label($"📊 Sức chứa MIN(Đất {c.CapacityByLand}, Mái {c.CapacityByShelters}, Nước {c.CapacityByWater}, Máng {c.CapacityByFeeders}) = {c.EffectiveCapacity} (Nghẽn: {c.Bottleneck})");
                GUILayout.Label("📜 Nhiệm vụ khởi nghiệp tại Thảo Nguyên:", subHeaderStyle);
                GUILayout.Label(Objective("feed-consumed", "Cho heo ăn") + "   " + Objective("water", "Thay nước") + "   " +
                                Objective("temporary-fence", "Dựng rào tạm") + "   " + Objective("guided-to-trough", "Lùa heo tới máng"));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("💾 Lưu ngày trại [F5]")) Report(TrySave());
                if (GUILayout.Button("📂 Tải trại [F9]")) Report(TryLoad());
                if (GUILayout.Button("🎵 Huýt sáo gọi đàn [Q]")) TryWhistle();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();

            // 3. Bottom-Center Action RPG Weapon & Skill Hotbar
            float barW = Mathf.Min(780f, Screen.width - 28f);
            Rect hotbarRect = new Rect((Screen.width - barW) * 0.5f, Screen.height - 54f, barW, 42f);
            DrawSolidRect(new Rect(hotbarRect.x - 2f, hotbarRect.y - 2f, hotbarRect.width + 4f, hotbarRect.height + 4f), new Color(0.84f, 0.66f, 0.26f, 0.92f));
            DrawSolidRect(hotbarRect, new Color(0.09f, 0.10f, 0.13f, 0.94f));
            GUILayout.BeginArea(new Rect(hotbarRect.x + 8f, hotbarRect.y + 5f, hotbarRect.width - 16f, hotbarRect.height - 10f));
            GUILayout.BeginHorizontal();
            int curWpn = EquippedWeaponSlotIndex;
            if (GUILayout.Button("[1] ⚔ Dao Rựa", curWpn == 0 ? activeTabButtonStyle : tabButtonStyle)) Report(Session.EquipWeaponBySlotKey(1));
            if (GUILayout.Button("[2] 🔨 Búa Tạ", curWpn == 1 ? activeTabButtonStyle : tabButtonStyle)) Report(Session.EquipWeaponBySlotKey(2));
            if (GUILayout.Button("[3] 🔱 Giáo Tre", curWpn == 2 ? activeTabButtonStyle : tabButtonStyle)) Report(Session.EquipWeaponBySlotKey(3));
            if (raidView != null)
            {
                if (GUILayout.Button("[J/LMB] Liên Hoàn", tabButtonStyle)) raidView.PracticeOrAttackCombo();
                if (GUILayout.Button("[K] Sát Gia Trảm", activeTabButtonStyle)) raidView.TriggerMartialSkill();
                if (GUILayout.Button("[Space] Phản Đòn", tabButtonStyle)) raidView.TriggerParry();
                if (GUILayout.Button("[C] Lướt Né", tabButtonStyle)) raidView.TriggerDashRoll();
                if (GUILayout.Button("[V] Triệu Hồi Quái", tabButtonStyle)) raidView.SpawnImmediateSkirmish();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (ShowBuildingTerminalModal)
            {
                DrawBuildingTerminalModal();
            }
        }

        private void DrawBuildingTerminalModal()
        {
            var engine = Session.Engine;
            float modalWidth = Mathf.Min(680f, Screen.width - 455f);
            if (modalWidth < 360f) modalWidth = 480f;
            GUILayout.BeginArea(new Rect(442f, 14f, modalWidth, Mathf.Min(Screen.height - 28f, 650f)), modalPanelStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("TRUNG TÂM ĐIỀU HÀNH CÔNG TRÌNH & HỆ THỐNG TRẠI 3D", headerStyle);
            if (GUILayout.Button("Đóng [X]", GUILayout.Width(80f))) ExitBuildingStation();
            GUILayout.EndHorizontal();

            string[] tabs =
            {
                "1.Nhà Chính", "2.Kho 36 Ô", "3.Nghiên Cứu", "4.Quân Y",
                "5.Xưởng/Thủ", "6.Thiên Văn", "7.Thị Trấn/Giờ"
            };
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabs.Length; i++)
            {
                var st = ActiveTerminalTab == i ? activeTabButtonStyle : tabButtonStyle;
                if (GUILayout.Button(tabs[i], st)) ActiveTerminalTab = i;
            }
            GUILayout.EndHorizontal();

            if (ActiveBuildingStation != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Camera Cận Cảnh: {ActiveBuildingStation.DisplayName} — {ActiveBuildingStation.BuildingSummary}", subHeaderStyle);
                if (GUILayout.Button("Trở về góc nhìn Khoa", GUILayout.Width(155f))) ActiveBuildingStation = null;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6f);
            terminalScroll = GUILayout.BeginScrollView(terminalScroll);

            switch (ActiveTerminalTab)
            {
                case 0:
                    GUILayout.Label($"ĐẠI SẢNH NHÀ CHÍNH (Cấp {engine.Defense.HallLevel}/5 · Cấp Trại {engine.Farm.FarmLevel})", headerStyle);
                    GUILayout.Label("Trung tâm chỉ huy nông trại, quản lý cấp độ đại sảnh, mở khóa giới hạn đàn heo và lưu trữ hồ sơ.");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Camera Cận Cảnh Nhà Chính")) InspectBuildingByKind(FarmInteractionKind.MainHouseHub, 0);
                    if (GUILayout.Button("Nâng Cấp Đại Sảnh Nhà Chính")) Report(Session.UpgradeMainHouseHall());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Lưu Hồ Sơ Trại [F5]")) Report(TrySave());
                    if (GUILayout.Button("Tải Hồ Sơ Trại [F9]")) Report(TryLoad());
                    if (GUILayout.Button("Nghỉ Qua Đêm (Hồi Thể Lực)")) Report(Session.ShiftTimeToHour(6));
                    GUILayout.EndHorizontal();
                    break;

                case 1:
                    GUILayout.Label($"NHÀ KHO NÔNG SẢN & VẬT TƯ ({Session.State.WarehouseSlotCount}/36 Ô · Cám {engine.Farm.FeedStockKg:0.0}/{Session.State.WarehouseSlotCount * 25:0} kg)", headerStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Camera Cận Cảnh Nhà Kho")) InspectBuildingByKind(FarmInteractionKind.WarehouseTerminal, 1);
                    if (GUILayout.Button("Nâng Cấp Kho Cám & Kho 36 Ô (+4 Ô)")) Report(Session.UpgradeWarehouseCapacity());
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Danh sách vật tư trong kho hiện tại:", subHeaderStyle);
                    foreach (string itemId in new[] { "TenThep", "Go", "Da", "ThaoDuoc", "ThuocTriBenh", "Vaccine", "ThuocBo", "TinhHoaGen", "ThitHeo", "DaHeo" })
                    {
                        int cnt = engine.Inventory.CountId(itemId);
                        if (cnt > 0) GUILayout.Label($"  • {itemId}: {cnt} đơn vị");
                    }
                    break;

                case 2:
                    GUILayout.Label("PHÒNG NGHIÊN CỨU SINH HỌC & LAI TẠO GEN (F1/F2 & Trưởng Lão 6 Bước)", headerStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Camera Cận Cảnh Phòng Nghiên Cứu")) InspectBuildingByKind(FarmInteractionKind.ResearchLab, 2);
                    if (GUILayout.Button("Nâng Cấp Phòng Nghiên Cứu")) Report(Session.UpgradeInfrastructureFacility("research_lab"));
                    GUILayout.EndHorizontal();
                    string targetPigId = SelectedOrNearest();
                    if (string.IsNullOrEmpty(targetPigId) && engine.Pigs.Count > 0) targetPigId = engine.Pigs[0].Id;
                    GUILayout.Label($"Heo mục tiêu giám định: {(string.IsNullOrEmpty(targetPigId) ? "Chưa có" : targetPigId)}", subHeaderStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Kênh 1: Chợ Ngày (180G)")) Report(Session.AppraisePig4Channel(targetPigId, AppraisalChannel.ChoNgay));
                    if (GUILayout.Button("Kênh 2: Chợ Đêm Cấp Cao (420G)")) Report(Session.AppraisePig4Channel(targetPigId, AppraisalChannel.ChoDemGiamDinhCapCao));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Kênh 3: Phòng Nghiên Cứu (300G)")) Report(Session.AppraisePig4Channel(targetPigId, AppraisalChannel.PhongNghienCuu));
                    if (GUILayout.Button("Kênh 4: Hồ Sơ Bí Nhân (100%)")) Report(Session.AppraisePig4Channel(targetPigId, AppraisalChannel.BiNhanHoSoGiongLoai));
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4f);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Nhập Heo Giống Trưởng Thành (220G)")) Report(Session.AcquireCertifiedBreedingPartner());
                    if (GUILayout.Button("Tiến Hành Phối Giống F1/F2 & Trưởng Lão")) { Report(Session.BreedPigsInLab()); RefreshViews(); }
                    GUILayout.EndHorizontal();
                    break;

                case 3:
                    GUILayout.Label("LỀU QUÂN Y & THÚ Y (5 Động Từ Chăm Sóc & Điều Trị)", headerStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Camera Cận Cảnh Lều Quân Y")) InspectBuildingByKind(FarmInteractionKind.MedicalTent, 3);
                    if (GUILayout.Button("Nâng Cấp Lều Quân Y")) Report(Session.UpgradeInfrastructureFacility("medical_tent"));
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Thực hành 5 động từ thú y chuẩn GDD trên đàn heo:", subHeaderStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("1. Cách Ly (CachLy)")) Report(Session.ExecuteMedicalTreatmentVerb(HealthTreatmentVerb.CachLy));
                    if (GUILayout.Button("2. Xử Lý Môi Trường")) Report(Session.ExecuteMedicalTreatmentVerb(HealthTreatmentVerb.XuLyMoiTruong));
                    if (GUILayout.Button("3. Điều Trị Cá Thể")) Report(Session.ExecuteMedicalTreatmentVerb(HealthTreatmentVerb.DieuTriCaThe));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("4. Phòng Ngừa Đàn")) Report(Session.ExecuteMedicalTreatmentVerb(HealthTreatmentVerb.PhongNguaDan));
                    if (GUILayout.Button("5. Nghiên Cứu Bệnh")) Report(Session.ExecuteMedicalTreatmentVerb(HealthTreatmentVerb.NghienCuu));
                    GUILayout.EndHorizontal();
                    break;

                case 4:
                    GUILayout.Label("XƯỞNG CƠ KHÍ, VŨ KHÍ & PHÒNG THỦ TRẠI", headerStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Camera Cận Cảnh Xưởng")) InspectBuildingByKind(FarmInteractionKind.WorkshopTerminal, 4);
                    if (GUILayout.Button("Nâng Cấp Xưởng Cơ Khí")) Report(Session.UpgradeInfrastructureFacility("workshop"));
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Trang bị & Đổi Vũ Khí Cận Chiến (Phím 1 / 2 / 3):", subHeaderStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("[1] Dao Rựa (Nhanh)")) Report(Session.EquipWeaponBySlotKey(1));
                    if (GUILayout.Button("[2] Búa Tạ (Phá Giáp)")) Report(Session.EquipWeaponBySlotKey(2));
                    if (GUILayout.Button("[3] Giáo Tre (Tầm Xa)")) Report(Session.EquipWeaponBySlotKey(3));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Cường Hóa Vũ Khí Đang Cầm")) Report(Session.EnhanceWeaponAtWorkshop());
                    if (GUILayout.Button("Gia Cố Rào & Nạp Đầy Tên Thép Nỏ Xuyên Vân")) { Report(Session.RepairAndFortifyAllDefenses()); RebuildNavigation(); }
                    GUILayout.EndHorizontal();
                    var raidView = FindAnyObjectByType<PlayableFarmRaidView>();
                    if (raidView != null)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Luyện Đòn Chém [J]")) raidView.PracticeOrAttackCombo();
                        if (GUILayout.Button("Thi Triển Võ Kỹ [K]")) raidView.TriggerMartialSkill();
                        if (GUILayout.Button("Thế Thủ / Phản Đòn [Space]")) raidView.TriggerParry();
                        GUILayout.EndHorizontal();
                    }
                    break;

                case 5:
                    GUILayout.Label("ĐÀI THIÊN VĂN & TRẠI THÔNG TIN (Viền Vàng Mái Xanh)", headerStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Camera Cận Cảnh Đài Thiên Văn")) InspectBuildingByKind(FarmInteractionKind.ObservatoryIntel, 5);
                    if (GUILayout.Button("Nâng Cấp Đài Thiên Văn")) Report(Session.UpgradeInfrastructureFacility("observatory"));
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("Quét Tinh Cầu Thiên Văn — Dự Báo Đợt Quái & Thời Tiết"))
                        Report(Session.ScanObservatoryIntel());
                    break;

                case 6:
                    GUILayout.Label("THỊ TRẤN CẢNG CHÂU ÂU CỔ & ĐIỀU KHIỂN NGÀY/ĐÊM", headerStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Camera Cận Cảnh Thị Trấn Cảng")) InspectBuildingByKind(FarmInteractionKind.HarborTownHub, 6);
                    if (GUILayout.Button("Nâng Cấp Thương Hội Cảng")) Report(Session.UpgradeInfrastructureFacility("harbor_guild"));
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Giao thương hàng hóa tại Bến Cảng:", subHeaderStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Lô 1: +60kg Cám, +20 Tên, +3 Thảo Dược")) Report(Session.ExecuteHarborTownTrade(1));
                    if (GUILayout.Button("Lô 2: Hợp Đồng Thương Thuyền (+240G)")) Report(Session.ExecuteHarborTownTrade(2));
                    if (GUILayout.Button("Lô 3: Khế Ước Mở Rộng Đất (+16 Chỗ)")) Report(Session.ExecuteHarborTownTrade(3));
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4f);
                    GUILayout.Label("Chuyển nhanh khung giờ Ngày / Hoàng Hôn / Đêm (Ánh sáng động):", subHeaderStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("06:00 Bình Minh")) { Report(Session.ShiftTimeToHour(6)); UpdateDynamicDayNightLighting(); }
                    if (GUILayout.Button("12:00 Giữa Trưa")) { Report(Session.ShiftTimeToHour(12)); UpdateDynamicDayNightLighting(); }
                    if (GUILayout.Button("17:30 Hoàng Hôn")) { Report(Session.ShiftTimeToHour(17)); UpdateDynamicDayNightLighting(); }
                    if (GUILayout.Button("20:30 Đêm Sao")) { Report(Session.ShiftTimeToHour(20)); UpdateDynamicDayNightLighting(); }
                    GUILayout.EndHorizontal();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        private string Objective(string action, string label) => (Session.HasEvidence(action) ? "✓ " : "○ ") + label;
        private void DrawMarket()
        {
            if (GUILayout.Button("Mua 10 kg cám thường")) Report(Session.BuyFeed(NearestStation.Point, 10));
            var pig = Session.GetPig(Session.SelectedPigId);
            if (pig == null) return;
            var quote = Session.Engine.Economy.CalculatePigSalePrice(pig, Session.Engine.Farm.FarmLevel);
            int final = quote.Item1 <= 0 ? 0 : Math.Max(10, quote.Item1 - Session.Engine.Economy.DayMarket.QuarantineFeePerPig);
            GUILayout.Label($"Giá dự kiến sau kiểm dịch: {final}G");
            if (!confirmSale && GUILayout.Button("Bán heo đang chọn…")) confirmSale = true;
            else if (confirmSale)
            {
                if (GUILayout.Button("Xác nhận bán " + PlayableFarmSession.DisplayPigName(pig))) { Report(Session.SellPig(NearestStation.Point, pig.Id, true)); confirmSale = false; RefreshViews(); }
                if (GUILayout.Button("Giữ heo lại")) confirmSale = false;
            }
        }
    }
}
