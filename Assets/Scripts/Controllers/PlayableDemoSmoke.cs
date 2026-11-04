using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PigTycoon.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PigTycoon.Presentation
{
    /// <summary>Opt-in standalone baseline check. Normal player sessions never create this object.</summary>
    public sealed class PlayableDemoSmoke : MonoBehaviour
    {
        private readonly List<string> checks = new List<string>();
        private readonly List<string> errors = new List<string>();
        private float started;
        private bool finished;

        public static bool IsRequested => Array.IndexOf(Environment.GetCommandLineArgs(), "-pigTycoonSmoke") >= 0;
        public static string ReportPath
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-pigTycoonSmokeReport");
                return Path.GetFullPath(index >= 0 && index + 1 < args.Length
                    ? args[index + 1]
                    : Path.Combine(Application.persistentDataPath, "baseline-smoke.json"));
            }
        }

        public static string SavePath => Path.Combine(Path.GetDirectoryName(ReportPath), "baseline-smoke-save.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (!IsRequested) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            var instance = new GameObject("OptIn_StandaloneBaselineSmoke").AddComponent<PlayableDemoSmoke>();
            DontDestroyOnLoad(instance.gameObject);
            instance.started = Time.realtimeSinceStartup;
            Application.runInBackground = true;
            Application.logMessageReceived += instance.RecordLog;
        }

        private IEnumerator Start()
        {
            IEnumerator routine = RunChecks();
            while (true)
            {
                object next;
                try
                {
                    if (!routine.MoveNext()) break;
                    next = routine.Current;
                }
                catch (Exception exception)
                {
                    errors.Add(exception.ToString());
                    break;
                }
                yield return next;
            }
            Finish();
        }

        private IEnumerator RunChecks()
        {
            yield return new WaitForSecondsRealtime(2f);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pigTycoonArtOnly") >= 0)
            {
                Check(!Application.isEditor && Application.unityVersion == "6000.6.3f1", "Pinned standalone visual candidate");
                CaptureCurrentView("gameplay");
                var camera = Camera.main;
                var follow = camera.GetComponent<CameraFollow2D>();
                Check(follow != null && follow.UseWorldBounds, "Overview uses actual candidate map bounds");
                Vector3 originalPosition = camera.transform.position;
                float originalSize = camera.orthographicSize;
                bool following = follow.enabled;
                follow.enabled = false;
                camera.transform.position = new Vector3(follow.WorldBounds.center.x, follow.WorldBounds.center.y, originalPosition.z);
                camera.orthographicSize = Mathf.Max(follow.WorldBounds.height, follow.WorldBounds.width / camera.aspect) * 0.53f;
                CaptureCurrentView("overview");
                camera.transform.position = originalPosition;
                camera.orthographicSize = originalSize;
                follow.enabled = following;
                yield break;
            }
            Check(!Application.isEditor, "Standalone process");
            Check(Application.unityVersion == "6000.6.3f1", "Pinned Unity version");
            Check(Directory.Exists(UISpriteLoader.ArtRootPath), "Runtime art package present");
            Check(UISpriteLoader.LoadSprite("Art/Sprites/Characters/Player/player_down_walk1.png") != null,
                "Player animation PNG loads outside repository");
            Check(UISpriteLoader.GetFrameWoodPanel() != null, "Runtime UI PNG loads");

            MobileGameController controller = MobileGameController.Instance;
            Check(controller != null && controller.Engine != null, "Canonical GameEngine initialized");
            GameEngine engine = controller.Engine;
            Check(engine.Character.Identity == PlayerId.An || engine.Character.Identity == PlayerId.Khoa,
                "Canonical two-player identity");
            PigAgentView[] pigs = FindObjectsByType<PigAgentView>();
            Check(pigs.Length > 0 && pigs.All(pig => pig.PigModel != null && engine.Pigs.Contains(pig.PigModel)),
                "Visible baseline pigs bind to canonical herd");

            PlayerMobileController player = FindAnyObjectByType<PlayerMobileController>();
            Check(FindObjectsByType<PlayerMobileController>().Length == 1, "Exactly one controllable actor is active");
            var roster = FindFirstObjectByType<PixelWorldPlayerRoster>();
            if (roster != null)
                Check(player == (engine.Character.Identity == PlayerId.Khoa ? roster.Khoa : roster.An),
                    "Active actor matches saved An/Khoa identity");
            Check(player != null && player.Joystick != null, "Baseline player input exists; active players=" +
                FindObjectsByType<PlayerMobileController>().Length + "; all players=" +
                FindObjectsByType<PlayerMobileController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length +
                "; joysticks=" + FindObjectsByType<MobileJoystick>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length +
                "; player=" + (player == null ? "null" : player.name));
            var pointer = new PointerEventData(EventSystem.current);
            MobileJoystick joystick = player.Joystick;
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, joystick.BackgroundRect.position) + Vector2.right * 300f;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pigTycoonCapture") >= 0)
            {
                CaptureCurrentView("Farm-start");
            }
            Vector3 before = player.transform.position;
            joystick.OnPointerDown(pointer);
            // A real-time sleep alone can end before the next physics step on a slow startup frame.
            yield return null;
            yield return new WaitForFixedUpdate();
            float movementDeadline = Time.realtimeSinceStartup + 3f;
            while (Vector3.Distance(before, player.transform.position) <= 0.1f && Time.realtimeSinceStartup < movementDeadline)
                yield return null;
            joystick.OnPointerUp(pointer);
            var contacts = new ContactPoint2D[16];
            int contactCount = player.GetComponent<Rigidbody2D>().GetContacts(contacts);
            string contactNames = string.Join(", ", contacts.Take(contactCount).Select(c =>
                c.collider.name + "/" + c.otherCollider.name + " normal=" + c.normal));
            Check(Vector3.Distance(before, player.transform.position) > 0.1f,
                "Joystick pointer callback moves real Rigidbody2D player; distance=" + Vector3.Distance(before, player.transform.position) +
                "; canMove=" + player.CanMove + "; input=" + player.MoveInput + "; timeScale=" + Time.timeScale +
                "; body=" + player.GetComponent<Rigidbody2D>().bodyType + "; simulated=" + player.GetComponent<Rigidbody2D>().simulated +
                "; position=" + player.transform.position + "; speed=" + player.MoveSpeed + "; multiplier=" + player.SpeedMultiplier +
                "; contacts=" + contactNames);
            Check(player.GetComponent<SpriteRenderer>().sprite != null, "Animated player has a sprite");
            Check(Camera.main != null && Camera.main.GetComponent<CameraFollow2D>()?.Target == player.transform,
                "Baseline camera follows player");

            controller.SaveGameToPersistentStorage();
            Check(File.Exists(SavePath) && SaveSystem.TryDeserialize(File.ReadAllText(SavePath), out var saved, out _)
                && saved.Pigs.Count == engine.Pigs.Count, "Isolated native save preserves herd count");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pigTycoonWorldRouteCheck") >= 0)
            {
                var originalHerd = engine.Pigs;
                var originalPig = engine.Pigs[0];
                var photographed = new HashSet<string>();
                foreach (string target in new[] { "Farm_Core", "Hub_Village", "MarketTown", "Hub_Village", "Farm_Core", "Farm", "Forest_Primeval", "Farm" })
                {
                    var portal = FindObjectsByType<PixelWorldPortal>(FindObjectsSortMode.None).FirstOrDefault(p => p.TargetLocationId == target);
                    Check(portal != null, "Real portal exists to " + target);
                    player = FindFirstObjectByType<PlayerMobileController>();
                    var body = player.GetComponent<Rigidbody2D>();
                    player.transform.position = portal.transform.position;
                    body.position = portal.transform.position;
                    body.linearVelocity = Vector2.zero;
                    yield return new WaitForFixedUpdate();
                    var travel = FindFirstObjectByType<PixelWorldTravelController>();
                    Check(travel != null, "Real travel controller exists");
                    travel.SendMessage("BeginTravel", portal, SendMessageOptions.RequireReceiver);
                    float deadline = Time.realtimeSinceStartup + 30;
                    while (Time.realtimeSinceStartup < deadline &&
                        (engine.WorldLocations.CurrentLocationId != target || FindFirstObjectByType<PixelWorldScene>()?.LocationId != target))
                        yield return null;
                    yield return new WaitForSecondsRealtime(0.25f);
                    Check(engine.WorldLocations.CurrentLocationId == target && FindFirstObjectByType<PixelWorldScene>()?.LocationId == target,
                        "Runtime portal traversal reaches " + target);
                    Check(ReferenceEquals(engine, MobileGameController.Instance.Engine), "One engine retained at " + target);
                    Check(ReferenceEquals(originalHerd, engine.Pigs) && engine.Pigs.Contains(originalPig), "Canonical herd identity retained at " + target);
                    if (photographed.Add(target) && Array.IndexOf(Environment.GetCommandLineArgs(), "-pigTycoonCapture") >= 0)
                    {
                        Check(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Capture uses real graphics device");
                        CaptureCurrentView(target);
                    }
                }
                controller.SaveGameToPersistentStorage();
                Check(SaveSystem.TryDeserialize(File.ReadAllText(SavePath), out var worldSave, out _) &&
                    worldSave.CurrentLocationId == engine.WorldLocations.CurrentLocationId, "Final world location saved");
            }
        }

        private void CaptureCurrentView(string name)
        {
            Check(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "Capture has a real graphics device");
            Camera camera = Camera.main;
            Check(camera != null, "Capture has the gameplay camera");
            var overlays = FindObjectsByType<Canvas>().Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var previousCameras = overlays.Select(c => c.worldCamera).ToArray();
            var previousDistances = overlays.Select(c => c.planeDistance).ToArray();
            var previousOrders = overlays.Select(c => c.sortingOrder).ToArray();
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = RenderTexture.active;
            Texture2D image = null;
            try
            {
                // Hidden Windows have no presented back buffer. Render the real camera and its UI
                // to an explicit destination through URP rather than accepting black screenshots.
                foreach (var canvas in overlays)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1f;
                    canvas.sortingOrder = short.MaxValue;
                }
                Canvas.ForceUpdateCanvases();
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                Check(RenderPipeline.SupportsRenderRequest(camera, request), "URP supports hidden camera capture");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                Check(image.GetPixels32().Any(c => c.r + c.g + c.b > 30), "Capture contains rendered color: " + name);
                File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(ReportPath), name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < overlays.Length; i++)
                {
                    overlays[i].renderMode = RenderMode.ScreenSpaceOverlay;
                    overlays[i].worldCamera = previousCameras[i];
                    overlays[i].planeDistance = previousDistances[i];
                    overlays[i].sortingOrder = previousOrders[i];
                }
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = previousTarget;
                RenderTexture.ReleaseTemporary(target);
                if (image != null) Destroy(image);
            }
        }

        private void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException(description);
            checks.Add(description);
        }

        private void RecordLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(message + "\n" + stack);
        }

        private void Update()
        {
            float limit = Array.IndexOf(Environment.GetCommandLineArgs(), "-pigTycoonWorldRouteCheck") >= 0 ? 300f : 30f;
            if (!finished && Time.realtimeSinceStartup - started > limit)
            {
                errors.Add("Standalone smoke exceeded " + limit + " seconds.");
                Finish();
            }
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;
            Application.logMessageReceived -= RecordLog;
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new SmokeReport
            {
                passed = errors.Count == 0,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                artRoot = UISpriteLoader.ArtRootPath,
                checks = checks.ToArray(),
                errors = errors.ToArray()
            }, true));
            Application.Quit(errors.Count == 0 ? 0 : 1);
        }

        [Serializable]
        private sealed class SmokeReport
        {
            public bool passed;
            public string scene;
            public string artRoot;
            public string[] checks;
            public string[] errors;
        }
    }
}
