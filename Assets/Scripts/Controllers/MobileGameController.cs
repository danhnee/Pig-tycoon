using System;
using UnityEngine;
using PigTycoon.Core;
using PigTycoon.Presentation.Services;

namespace PigTycoon.Presentation
{
    public class MobileGameController : MonoBehaviour
    {
        public static MobileGameController Instance { get; private set; }

        public GameEngine Engine => GameServiceBootstrap.Instance != null
            ? GameServiceBootstrap.Instance.Engine
            : null;

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
        }

        private void Update()
        {
            GameEngine engine = Engine;
            if (engine == null) return;

            minuteTimer += Time.deltaTime;
            if (minuteTimer >= RealSecondsPerGameMinute)
            {
                minuteTimer -= RealSecondsPerGameMinute;
                engine.Tick(1);
                OnStateUpdated?.Invoke();
            }
        }

        #region Save Service Facade
        public GameSaveLoadResult LoadSaveData()
        {
            GameServiceBootstrap bootstrap = GameServiceBootstrap.Instance;
            return bootstrap != null
                ? bootstrap.LoadResult
                : GameSaveLoadResult.Unavailable("Service bootstrap is not available.");
        }

        public GameSaveOperationResult SaveGameToPersistentStorage()
        {
            GameServiceBootstrap bootstrap = GameServiceBootstrap.Instance;
            if (bootstrap == null)
            {
                const string message = "Service bootstrap is not available; save skipped.";
                Debug.LogWarning($"[PigTycoon] {message}");
                return new GameSaveOperationResult(GameSaveWriteStatus.NotReady, message);
            }

            return bootstrap.SaveGame();
        }
        #endregion
    }
}
