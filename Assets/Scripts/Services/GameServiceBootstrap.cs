using System;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation.Services
{
    public enum ServiceBootstrapState
    {
        NotStarted,
        Initializing,
        Ready,
        Failed
    }

    [DefaultExecutionOrder(-10000)]
    public sealed class GameServiceBootstrap : MonoBehaviour
    {
        public static GameServiceBootstrap Instance { get; private set; }

        private GameEngine gameEngine;
        public GameEngine Engine => GameDataReady ? gameEngine : null;
        public GameSaveService SaveService { get; private set; }
        public SceneLoader SceneLoader { get; private set; }
        public GameSaveLoadResult LoadResult => SaveService != null
            ? SaveService.LastLoadResult
            : GameSaveLoadResult.Unavailable(InitializationError?.Message ?? "Save service is unavailable.");
        public ServiceBootstrapState State { get; private set; }
        public bool GameDataReady { get; private set; }
        public Exception InitializationError { get; private set; }
        public event Action<GameServiceBootstrap> ServicesReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBeforeFirstScene()
        {
            if (Instance != null)
                return;

            var bootstrapObject = new GameObject("PigTycoon Services");
            bootstrapObject.AddComponent<GameServiceBootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }

        private void InitializeServices()
        {
            State = ServiceBootstrapState.Initializing;
            try
            {
                SaveService = new GameSaveService();
                GameSaveLoadResult loadResult = SaveService.Load();

                gameEngine = new GameEngine();
                if (loadResult.Status == GameSaveLoadStatus.Parsed)
                {
                    try
                    {
                        gameEngine.RestoreFromSave(loadResult.Data);
                        SaveService.MarkRestoreSucceeded(loadResult.Data);
                        GameDataReady = true;
                    }
                    catch (Exception ex)
                    {
                        InitializationError = ex;
                        loadResult = SaveService.MarkRestoreFailed(ex);
                        Debug.LogError($"[PigTycoon] Save đọc được nhưng không thể khôi phục. Giữ nguyên file; game data chưa sẵn sàng: {ex}");
                    }
                }
                else if (loadResult.Status == GameSaveLoadStatus.Missing)
                {
                    InitializeNewGame(gameEngine);
                    GameDataReady = true;
                }
                else
                {
                    Debug.LogError(
                        $"[PigTycoon] Save load failed at {loadResult.FailureStage} ({loadResult.Status}); " +
                        $"giữ nguyên file và không tạo dữ liệu mới. {loadResult.Message} {loadResult.Error}");
                }

                SceneLoader = gameObject.AddComponent<SceneLoader>();
                State = ServiceBootstrapState.Ready;
                ServicesReady?.Invoke(this);
                Debug.Log("[PigTycoon] Shared services are ready.");
            }
            catch (Exception ex)
            {
                InitializationError = ex;
                State = ServiceBootstrapState.Failed;
                Debug.LogError($"[PigTycoon] Service bootstrap failed: {ex}");
            }
        }

        private static void InitializeNewGame(GameEngine engine)
        {
            var firstPig = Pig.CreateDefault(
                "p_1", "Hồng Điền #1", GeneLineId.HongDien, GeneRarity.Thuong);
            firstPig.Stage = PigStage.TruongThanh;
            firstPig.WeightKg = 100f;
            firstPig.Bonding = 65f;

            var secondPig = Pig.CreateDefault(
                "p_2", "Kim Thọ #1", GeneLineId.KimTho, GeneRarity.HuyenThoai);
            secondPig.Stage = PigStage.TruongThanh;
            secondPig.Bonding = 80f;

            engine.Pigs.Add(firstPig);
            engine.Pigs.Add(secondPig);
            engine.Defense.ConstructBuilding("NoXuyenVan");
        }

        public GameSaveOperationResult SaveGame()
        {
            if (State != ServiceBootstrapState.Ready || !GameDataReady || SaveService == null || gameEngine == null)
            {
                const string message = "Services are not ready; save skipped.";
                Debug.LogWarning($"[PigTycoon] {message}");
                return new GameSaveOperationResult(GameSaveWriteStatus.NotReady, message);
            }

            return SaveService.Save(gameEngine);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null;
            ServicesReady = null;
        }
    }
}
