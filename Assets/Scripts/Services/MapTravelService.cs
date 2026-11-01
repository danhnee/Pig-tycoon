using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using PigTycoon.Presentation;

namespace PigTycoon.Presentation.Services
{
    /// <summary>
    /// Travels between map-only additive scenes while retaining the loaded farm scene,
    /// its player, camera, HUD and EventSystem as the single shared presentation set.
    /// </summary>
    public sealed class MapTravelService : MonoBehaviour
    {
        public static MapTravelService Instance { get; private set; }

        private bool transitionInProgress;
        private CameraFollow2D farmCameraFollow;
        private bool originalFarmCameraClamp;
        private bool cameraClampCaptured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateService()
        {
            if (Instance != null)
                return;

            var serviceObject = new GameObject("PigTycoon Map Travel Service");
            serviceObject.AddComponent<MapTravelService>();
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
        }

        public void Travel(string destinationScene, string destinationSpawnId, Action<bool> completed = null)
        {
            if (transitionInProgress)
            {
                completed?.Invoke(false);
                return;
            }

            GameServiceBootstrap bootstrap = GameServiceBootstrap.Instance;
            PlayerInteractionController player = PlayerInteractionController.Instance;
            if (bootstrap == null || bootstrap.SceneLoader == null || player == null ||
                string.IsNullOrWhiteSpace(destinationScene) || string.IsNullOrWhiteSpace(destinationSpawnId))
            {
                Debug.LogError("[PigTycoon] Map transition unavailable: service, player or destination is missing.");
                completed?.Invoke(false);
                return;
            }

            transitionInProgress = true;
            bootstrap.SceneLoader.LoadAdditive(destinationScene, result =>
            {
                if (!result.Succeeded)
                {
                    Debug.LogError($"[PigTycoon] Could not enter map '{destinationScene}': {result.Error}");
                    transitionInProgress = false;
                    completed?.Invoke(false);
                    return;
                }

                MapSpawnPoint spawn = FindSpawn(result.Scene, destinationSpawnId);
                if (spawn == null)
                {
                    Debug.LogError($"[PigTycoon] Spawn '{destinationSpawnId}' was not found in '{destinationScene}'.");
                    if (result.Status == SceneLoadStatus.Loaded)
                        StartCoroutine(UnloadAfterFailure(result.Scene));
                    transitionInProgress = false;
                    completed?.Invoke(false);
                    return;
                }

                Scene sourceScene = SceneManager.GetActiveScene();
                Vector3 previousPosition = player.transform.position;
                player.transform.position = spawn.transform.position;

                if (!SceneManager.SetActiveScene(result.Scene))
                {
                    player.transform.position = previousPosition;
                    Debug.LogError($"[PigTycoon] Could not activate destination scene '{destinationScene}'.");
                    if (result.Status == SceneLoadStatus.Loaded)
                        StartCoroutine(UnloadAfterFailure(result.Scene));
                    transitionInProgress = false;
                    completed?.Invoke(false);
                    return;
                }

                if (result.Scene.name == "Hub_Village")
                {
                    farmCameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
                    if (farmCameraFollow != null)
                    {
                        originalFarmCameraClamp = farmCameraFollow.ClampToFarmBounds;
                        cameraClampCaptured = true;
                        farmCameraFollow.ClampToFarmBounds = false;
                    }
                }
                else if (result.Scene.name == "Farm_Core" && cameraClampCaptured && farmCameraFollow != null)
                {
                    farmCameraFollow.ClampToFarmBounds = originalFarmCameraClamp;
                    cameraClampCaptured = false;
                    farmCameraFollow = null;
                }

                if (sourceScene.name == "Hub_Village" && result.Scene.name == "Farm_Core")
                    StartCoroutine(UnloadSourceAfterTransition(sourceScene));

                PlayerInteractionController.Instance?.ShowFeedback($"Đã đến {destinationScene}.");
                transitionInProgress = false;
                completed?.Invoke(true);
            }, setActiveScene: false);
        }

        private static MapSpawnPoint FindSpawn(Scene scene, string spawnId)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MapSpawnPoint point in root.GetComponentsInChildren<MapSpawnPoint>(true))
                {
                    if (string.Equals(point.SpawnId, spawnId, StringComparison.Ordinal))
                        return point;
                }
            }

            return null;
        }

        private IEnumerator UnloadAfterFailure(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded && scene.name != "Farm_Core")
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        private IEnumerator UnloadSourceAfterTransition(Scene scene)
        {
            yield return null;
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
