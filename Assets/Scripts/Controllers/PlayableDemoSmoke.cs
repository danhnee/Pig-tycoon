using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PigTycoon.Core;
using UnityEngine;
using UnityEngine.EventSystems;

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
            Check(player != null && player.Joystick != null, "Baseline player input exists");
            var pointer = new PointerEventData(EventSystem.current);
            MobileJoystick joystick = player.Joystick;
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, joystick.BackgroundRect.position) + Vector2.right * 300f;
            Vector3 before = player.transform.position;
            joystick.OnPointerDown(pointer);
            yield return new WaitForSecondsRealtime(0.25f);
            joystick.OnPointerUp(pointer);
            Check(Vector3.Distance(before, player.transform.position) > 0.1f,
                "Joystick pointer callback moves real Rigidbody2D player");
            Check(player.GetComponent<SpriteRenderer>().sprite != null, "Animated player has a sprite");
            Check(Camera.main != null && Camera.main.GetComponent<CameraFollow2D>()?.Target == player.transform,
                "Baseline camera follows player");

            controller.SaveGameToPersistentStorage();
            Check(File.Exists(SavePath) && SaveSystem.TryDeserialize(File.ReadAllText(SavePath), out var saved, out _)
                && saved.Pigs.Count == engine.Pigs.Count, "Isolated native save preserves herd count");
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
            if (!finished && Time.realtimeSinceStartup - started > 30f)
            {
                errors.Add("Standalone smoke exceeded 30 seconds.");
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
