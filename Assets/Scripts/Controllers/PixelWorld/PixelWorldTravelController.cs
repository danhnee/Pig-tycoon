using System;
using System.Collections;
using PigTycoon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PigTycoon.Presentation
{
    /// <summary>Changes the scene, retaining MobileGameController and its single domain session.</summary>
    public sealed class PixelWorldTravelController : MonoBehaviour
    {
        static PixelWorldTravelController instance;
        bool travelling;
        string message;
        float messageUntil;
        PlayerMobileController player;
        PixelWorldScene worldScene;
        PixelWorldPortal[] portals = Array.Empty<PixelWorldPortal>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (instance != null || FindFirstObjectByType<PixelWorldScene>() == null) return;
            new GameObject("PixelWorldTravelController").AddComponent<PixelWorldTravelController>();
        }

        void Awake()
        {
            if (instance != null) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += SceneLoaded;
            if (FindFirstObjectByType<PixelWorldAndroidBridge>() == null)
                new GameObject("PixelWorldAndroidBridge").AddComponent<PixelWorldAndroidBridge>();
        }

        IEnumerator Start()
        {
            yield return null;
            BindScene();
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null || worldScene == null) yield break;
            PixelWorldRoutes.Register(engine.WorldLocations);
            if (!MobileGameController.Instance.HasRestoredSave)
            {
                // Domain prototype tile coordinates are not an authored world-space spawn.
                engine.WorldLocations.CurrentLocationId = worldScene.LocationId;
                if (PlacePlayer(worldScene.DefaultArrival, out _)) MobileGameController.Instance.SaveGameToPersistentStorage();
                else ShowError("Không tìm được điểm xuất hiện an toàn trong scene nguồn.");
                yield break;
            }
            PixelWorldMap saved;
            try { saved = PixelWorldRoutes.ForLocation(engine.WorldLocations.CurrentLocationId); }
            catch (ArgumentException ex) { ShowError(ex.Message); yield break; }
            engine.WorldLocations.CurrentLocationId = saved.LocationId;
            if (saved.SceneName != SceneManager.GetActiveScene().name)
                yield return Travel(saved, null, true);
            else if (!PlacePlayer(new Vector2(engine.WorldLocations.PlayerTileX, engine.WorldLocations.PlayerTileY), out _))
                ShowError("Không tìm được vị trí an toàn để khôi phục nhân vật.");
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (instance == this) instance = null;
        }

        void SceneLoaded(Scene scene, LoadSceneMode mode) { BindScene(); }
        void BindScene()
        {
            player = FindFirstObjectByType<PlayerMobileController>();
            worldScene = FindFirstObjectByType<PixelWorldScene>();
            portals = FindObjectsByType<PixelWorldPortal>(FindObjectsSortMode.None);
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null) PixelWorldRoutes.Register(engine.WorldLocations);
        }

        void Update()
        {
            if (travelling || worldScene == null || player == null) return;
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                engine.WorldLocations.PlayerTileX = Mathf.RoundToInt(player.transform.position.x);
                engine.WorldLocations.PlayerTileY = Mathf.RoundToInt(player.transform.position.y);
            }
            var expedition = engine?.ForestAndEvents.Expedition;
            if (expedition != null && expedition.IsActive)
            {
                bool forcedReturn = worldScene.LocationId == "Forest_Primeval" &&
                    engine.Clock.CurrentDay - expedition.StartedDay >= ForestPrimevalAndEventFramework.MaxDaysTrappedInEvent;
                bool fogArrival = worldScene.LocationId == "Farm" && !expedition.IsGateEvent;
                if (forcedReturn || fogArrival)
                {
                    string destination = forcedReturn ? "Farm" : "Forest_Primeval";
                    foreach (var encounterPortal in portals)
                        if (encounterPortal.TargetLocationId == destination)
                        { BeginTravel(encounterPortal); return; }
                }
            }
            var portal = NearestPortal();
            bool steppedIntoGate = portal != null && portal.TargetLocationId == "Forest_Primeval" &&
                engine.ForestAndEvents.Director.HasOpenGate(engine) &&
                Vector2.Distance(player.transform.position, portal.transform.position) <= portal.InteractionRadius;
            if (portal != null && (steppedIntoGate || Input.GetKeyDown(KeyCode.E))) BeginTravel(portal);
        }

        PixelWorldPortal NearestPortal()
        {
            if (player == null || worldScene == null) return null;
            PixelWorldPortal nearest = null;
            float best = float.PositiveInfinity;
            foreach (var portal in portals)
            {
                var engine = MobileGameController.Instance?.Engine;
                bool offeredGate = engine != null && worldScene.LocationId == "Farm" && portal.TargetLocationId == "Forest_Primeval" &&
                    engine.ForestAndEvents.Director.HasOpenGate(engine);
                if (engine == null || (!offeredGate && !PixelWorldRoutes.CanTravel(worldScene.LocationId, portal.TargetLocationId, engine, portal.RequestedExitItemId))) continue;
                float distance = Vector2.Distance(player.transform.position, portal.transform.position);
                if (distance <= portal.InteractionRadius && distance < best) { nearest = portal; best = distance; }
            }
            return nearest;
        }

        /// <summary>Story travel (ADR-0027 "goto_map:"): use this scene's portal that leads to <paramref name="locationId"/>.</summary>
        public bool TravelToLocation(string locationId)
        {
            if (travelling || worldScene == null || string.IsNullOrEmpty(locationId)) return false;
            if (worldScene.LocationId == locationId) return true;
            foreach (var p in portals)
                if (p != null && p.TargetLocationId == locationId) { BeginTravel(p); return true; }
            return false;
        }

        public bool IsTravelling => travelling;
        public string CurrentLocationId => worldScene != null ? worldScene.LocationId : "";

        void BeginTravel(PixelWorldPortal portal)
        {
            if (travelling || worldScene == null) return;
            var engine = MobileGameController.Instance?.Engine;
            bool offeredGate = engine != null && worldScene.LocationId == "Farm" && portal.TargetLocationId == "Forest_Primeval" &&
                engine.ForestAndEvents.Director.HasOpenGate(engine);
            if (engine == null || (!offeredGate && !PixelWorldRoutes.CanTravel(worldScene.LocationId, portal.TargetLocationId, engine, portal.RequestedExitItemId)))
            { ShowError("Cổng không nối tới khu vực này."); return; }
            StartCoroutine(Travel(PixelWorldRoutes.ForLocation(portal.TargetLocationId), portal.TargetPortalId, false, portal.RequestedExitItemId));
        }

        IEnumerator Travel(PixelWorldMap target, string targetPortalId, bool restoring, string exitItemId = null)
        {
            if (!Application.CanStreamedLevelBeLoaded(target.SceneName))
            { ShowError("Chưa có scene trong bản dựng: " + target.SceneName); yield break; }
            var controller = MobileGameController.Instance;
            if (controller?.Engine == null) { ShowError("Phiên chơi chưa sẵn sàng."); yield break; }
            var engine = controller.Engine;
            string previousScene = SceneManager.GetActiveScene().name;
            string previousLocation = engine.WorldLocations.CurrentLocationId;
            Vector2 previousPosition = player != null ? (Vector2)player.transform.position : Vector2.zero;
            Vector2 savedPosition = new Vector2(engine.WorldLocations.PlayerTileX, engine.WorldLocations.PlayerTileY);
            travelling = true;
            if (player != null) player.CanMove = false;
            // Stop the same simulation owner while loading, preserving its minute accumulator.
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(target.SceneName, LoadSceneMode.Single); }
            catch (Exception ex) { ShowError(ex.Message); }
            if (operation != null) yield return operation;
            BindScene();
            PixelWorldPortal arrival = null;
            foreach (var p in portals)
                if (p.PortalId == targetPortalId) { arrival = p; break; }
            bool valid = operation != null && player != null && worldScene != null && worldScene.LocationId == target.LocationId &&
                         (restoring || arrival != null);
            if (valid)
            {
                Vector2 position = restoring ? savedPosition : (Vector2)arrival.transform.position + arrival.ArrivalOffset;
                valid = PlacePlayer(position, out position);
                if (valid)
                {
                    // Commit a qualified gate encounter only once its destination scene/spawn is valid.
                    if (!restoring && target.LocationId == "Forest_Primeval" && !engine.ForestAndEvents.Expedition.IsActive)
                        valid = engine.ForestAndEvents.Director.TryEnterOfferedGate(engine);
                    var result = valid ? engine.WorldLocations.TryWarpPlayer(target.LocationId, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), engine, exitItemId) : (warped: false, message: "Cổng đã đóng.");
                    valid = result.warped;
                    if (!valid) ShowError(result.message);
                }
            }
            if (!valid)
            {
                ShowError("Không thể đi qua cổng; đang trở lại khu vực trước.");
                if (SceneManager.GetActiveScene().name != previousScene)
                {
                    var rollback = SceneManager.LoadSceneAsync(previousScene, LoadSceneMode.Single);
                    if (rollback != null) yield return rollback;
                }
                BindScene();
                PlacePlayer(previousPosition, out previousPosition);
                engine.WorldLocations.CurrentLocationId = previousLocation;
                engine.WorldLocations.PlayerTileX = Mathf.RoundToInt(previousPosition.x);
                engine.WorldLocations.PlayerTileY = Mathf.RoundToInt(previousPosition.y);
            }
            controller.enabled = wasEnabled;
            if (player != null) player.CanMove = true;
            travelling = false;
            if (valid)
            {
                controller.OnStateUpdated?.Invoke();
                controller.SaveGameToPersistentStorage();
            }
        }

        bool PlacePlayer(Vector2 requested, out Vector2 position)
        {
            position = requested;
            if (player == null) return false;
            var body = player.GetComponent<Rigidbody2D>();
            var collider = player.GetComponent<Collider2D>();
            Vector2 original = player.transform.position;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(player.gameObject.layer));
            var overlaps = new Collider2D[32];
            bool ClearAt(Vector2 candidate)
            {
                player.transform.position = new Vector3(candidate.x, candidate.y, player.transform.position.z);
                if (body != null) { body.position = candidate; body.linearVelocity = Vector2.zero; }
                Physics2D.SyncTransforms();
                if (collider == null) return true;
                int count = collider.Overlap(filter, overlaps);
                for (int i = 0; i < count; i++)
                    if (overlaps[i] != null && overlaps[i] != collider && overlaps[i].attachedRigidbody != body)
                        return false;
                return true;
            }
            bool clear = ClearAt(position);
            Vector2[] directions = { Vector2.down, Vector2.up, Vector2.left, Vector2.right,
                new Vector2(-1, -1).normalized, new Vector2(1, -1).normalized,
                new Vector2(-1, 1).normalized, new Vector2(1, 1).normalized };
            for (float radius = 0.5f; !clear && radius <= 3f; radius += 0.5f)
                foreach (var direction in directions)
                    if (ClearAt(requested + direction * radius))
                    { position = requested + direction * radius; clear = true; break; }
            if (!clear && worldScene != null && ClearAt(worldScene.DefaultArrival))
            { position = worldScene.DefaultArrival; clear = true; }
            if (!clear) { ClearAt(original); position = original; return false; }
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                engine.WorldLocations.PlayerTileX = Mathf.RoundToInt(position.x);
                engine.WorldLocations.PlayerTileY = Mathf.RoundToInt(position.y);
            }
            foreach (var camera in FindObjectsByType<CameraFollow2D>(FindObjectsSortMode.None)) camera.Target = player.transform;
            return true;
        }

        void ShowError(string text) { message = text; messageUntil = Time.unscaledTime + 8; Debug.LogError(text); }
        void OnGUI()
        {
            float width = Mathf.Min(440, Screen.width - 32);
            if (travelling) GUI.Box(new Rect((Screen.width - width) / 2, 16, width, 44), "Đang qua khu vực…");
            else
            {
                var portal = NearestPortal();
                if (portal != null && GUI.Button(new Rect((Screen.width - width) / 2, Screen.height - 100, width, 60), portal.DisplayName + "  [E]")) BeginTravel(portal);
            }
            if (Time.unscaledTime < messageUntil) GUI.Box(new Rect((Screen.width - width) / 2, 64, width, 70), message);
        }
    }
}
