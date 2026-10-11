using System;
using System.IO;
using UnityEngine;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class MobileGameController : MonoBehaviour
    {
        public static MobileGameController Instance { get; private set; }

        public GameEngine Engine { get; private set; }

        [Header("Simulation Timing")]
        [Tooltip("1 game minute = 0.67s real time")]
        public float RealSecondsPerGameMinute = 0.6667f;
        private float minuteTimer = 0f;

        [Header("Events")]
        public Action OnStateUpdated;

        private void Awake()
        {
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
            Engine = new GameEngine();

            // Khởi tạo một số cá thể heo mẫu cho demo
            var p1 = Pig.CreateDefault("p_1", "Hồng Điền #1", GeneLineId.HongDien, GeneRarity.Thuong);
            p1.Stage = PigStage.TruongThanh;
            p1.WeightKg = 100f;
            p1.Bonding = 65f;

            var p2 = Pig.CreateDefault("p_2", "Kim Thọ #1", GeneLineId.KimTho, GeneRarity.HuyenThoai);
            p2.Stage = PigStage.TruongThanh;
            p2.Bonding = 80f;

            Engine.Pigs.Add(p1);
            Engine.Pigs.Add(p2);

            // Nạp công trình cơ bản
            Engine.Defense.ConstructBuilding("NoXuyenVan");
        }

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

        public void SaveGameToPersistentStorage()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "pig_tycoon_save.json");
                string json = JsonUtility.ToJson(Engine);
                File.WriteAllText(path, json);
                Debug.Log($"[PigTycoon] Đã tự động lưu game vào: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PigTycoon] Lưu game thất bại: {ex.Message}");
            }
        }
        #endregion
    }
}
