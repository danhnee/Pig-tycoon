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

            GameSaveData savedData = LoadSaveData();

            if (savedData != null && savedData.SaveVersion >= 1 && savedData.SaveVersion <= SaveSystem.CurrentSaveVersion)
            {
                Engine.RestoreFromSave(savedData);
                Debug.Log($"[PigTycoon] Đã khôi phục dữ liệu game (SaveVersion v{savedData.SaveVersion}).");
                OnStateUpdated?.Invoke();
                return;
            }

            if (savedData != null)
            {
                Debug.LogWarning(
                    $"[PigTycoon] Phiên bản file lưu (v{savedData.SaveVersion}) không tương thích. Tạo game mới.");
            }

            // Chỉ tạo dữ liệu mẫu khi chưa có file lưu hợp lệ
            var p1 = Pig.CreateDefault(
                "p_1", "Hồng Điền #1",
                GeneLineId.HongDien, GeneRarity.Thuong);

            p1.Stage = PigStage.TruongThanh;
            p1.WeightKg = 100f;
            p1.Bonding = 65f;

            var p2 = Pig.CreateDefault(
                "p_2", "Kim Thọ #1",
                GeneLineId.KimTho, GeneRarity.HuyenThoai);

            p2.Stage = PigStage.TruongThanh;
            p2.Bonding = 80f;

            Engine.Pigs.Add(p1);
            Engine.Pigs.Add(p2);

            Engine.Defense.ConstructBuilding("NoXuyenVan");
        }
        private string SavePath => Path.Combine(Application.persistentDataPath, "pig_tycoon_save.json");
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
                if (SaveSystem.TryDeserialize(json, out var data, out _))
                {
                    return data;
                }
                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PigTycoon] Tải file lưu thất bại: {ex.Message}");
                return null;
            }
        }

        // Tránh lỗi khi lưu game nha
        public void SaveGameToPersistentStorage()
        {
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
