using System;
using System.IO;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    [DefaultExecutionOrder(-10000)]
    public class MobileGameController : MonoBehaviour
    {
        public static MobileGameController Instance { get; private set; }

        public GameEngine Engine { get; private set; }
        public bool HasRestoredSave { get; private set; }
        public string SaveLoadError { get; private set; }
        /// <summary>HANDBOOK P1.02/P1.21: a fresh game waits for the An/Khoa start screen (CharacterSelectScreen).</summary>
        public bool NeedsCharacterSelect { get; private set; }

        [Header("Simulation Timing")]
        [Tooltip("1 game minute = 0.67s real time")]
        public float RealSecondsPerGameMinute = 0.6667f;
        private float minuteTimer = 0f;

        [Header("Events")]
        public Action OnStateUpdated;

        private void Awake()
        {
            Application.targetFrameRate = 60; // Android mặc định 30 khung hình/giây
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeEngine();
        }

        private void InitializeEngine()
        {
            GameSaveData savedData = LoadSaveData();
            // Ván mới: hạt ngẫu nhiên riêng (kịch bản quay video giữ hạt cố định). Ván đã lưu: dùng lại hạt của ván.
            bool scripted = System.Environment.CommandLine.Contains("-pigtycoon-scenario");
            int seed = savedData != null && savedData.BaseSeed != 0 ? savedData.BaseSeed
                : scripted ? 20260601 : (System.Environment.TickCount & 0x7FFFFFFF) | 1;
            Engine = new GameEngine(seed);
            // Core's player-facing messages (wave papers, recycling, drove, Bí Nhân…) used to reach nobody
            Engine.OnLogEmitted += ToastOverlay.Show;

            if (savedData != null && savedData.SaveVersion >= 1 && savedData.SaveVersion <= SaveSystem.CurrentSaveVersion)
            {
                Engine.RestoreFromSave(savedData);
                HasRestoredSave = true;
                Debug.Log($"[PigTycoon] Đã khôi phục dữ liệu game (SaveVersion v{savedData.SaveVersion}).");
                OnStateUpdated?.Invoke();
                return;
            }

            if (savedData != null)
            {
                Debug.LogWarning(
                    $"[PigTycoon] Phiên bản file lưu (v{savedData.SaveVersion}) không tương thích. Tạo game mới.");
            }

            bool presetByLauncher = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            // Select before the actor roster awakens; a valid existing save returned above.
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            {
                string selected = intent.Call<string>("getStringExtra", "pigtycoon.player");
                if (selected == "An" || selected == "Khoa")
                {
                    Engine.SwitchActiveProtagonist(selected == "Khoa" ? PlayerId.Khoa : PlayerId.An);
                    presetByLauncher = true;
                }
            }
#endif
            // The video autopilot and the smoke test keep An and never wait on the start screen.
            NeedsCharacterSelect = NewGameSetup.ShouldOfferCharacterSelect(false, presetByLauncher,
                RecordingScenario.IsRequested || PlayableDemoSmoke.IsRequested);

            // Ván mới: đàn bàn giao của ông Năm ("một đàn nhỏ" — 12 con trưởng thành, ADR-0019 PROVISIONAL)
            StarterHerd.FillTo(Engine, StarterHerd.DefaultSize);

            Engine.Defense.ConstructBuilding("NoXuyenVan");
            // ADR-0027: the prologue starts when the protagonist is fixed (launcher preset now, start screen on confirm).
            if (presetByLauncher) Engine.FireNarrative(NarrativeTrigger.NewGame);
        }
        /// <summary>Start screen choice: applies the roster stats/trait and keeps the story protagonist in sync.</summary>
        public void CompleteCharacterSelect(PlayerId id)
        {
            if (Engine == null) return;
            NewGameSetup.ChooseProtagonist(Engine, id);
            NeedsCharacterSelect = false;
            Engine.FireNarrative(NarrativeTrigger.NewGame);
            OnStateUpdated?.Invoke();
        }

        private string SavePath => PlayableDemoSmoke.IsRequested
            ? PlayableDemoSmoke.SavePath
            : Path.Combine(Application.persistentDataPath, "pig_tycoon_save.json");
        private void Update()
        {
            if (Engine == null) return;

            minuteTimer += Time.deltaTime;
            if (minuteTimer >= RealSecondsPerGameMinute)
            {
                minuteTimer -= RealSecondsPerGameMinute;
                Engine.Tick(1);
                OnStateUpdated?.Invoke();
            }
        }

        #region Android Lifecycle & Auto Save
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGameToPersistentStorage();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGameToPersistentStorage();
        }
        public GameSaveData LoadSaveData()
        {
            if (!File.Exists(SavePath))
                return null;

            try
            {
                string json = File.ReadAllText(SavePath);
                if (SaveSystem.TryDeserialize(json, out var data, out var error))
                {
                    return data;
                }
                SaveLoadError = error;
                Debug.LogError("[PigTycoon] Không đọc được bản lưu; giữ nguyên file: " + error);
                return null;
            }
            catch (Exception ex)
            {
                SaveLoadError = ex.Message;
                Debug.LogError($"[PigTycoon] Tải file lưu thất bại: {ex.Message}");
                return null;
            }
        }

        // Tránh lỗi khi lưu game nha
        public void SaveGameToPersistentStorage()
        {
            if (!string.IsNullOrEmpty(SaveLoadError)) return;
            var lifecycleBridge = FindFirstObjectByType<PixelWorldAndroidBridge>();
            if (lifecycleBridge != null && !PlayableDemoSmoke.IsRequested)
            {
                lifecycleBridge.TrySave(out _);
                return;
            }
            if (Engine == null)
            {
                Debug.LogWarning("[PigTycoon] Chưa khởi tạo Engine, không thể lưu.");
                return;
            }

            try
            {
                string json = SaveSystem.Serialize(Engine.CreateSaveData());
                File.WriteAllText(SavePath, json);

                Debug.Log($"[PigTycoon] Đã lưu game tại: {SavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PigTycoon] Lưu game thất bại: {ex.Message}");
            }
        }
        #endregion
    }
}
