using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PigTycoon.Presentation.Services
{
    public enum SceneLoadStatus
    {
        Loaded,
        AlreadyLoaded,
        Failed
    }

    public sealed class SceneLoadResult
    {
        public SceneLoadStatus Status { get; }
        public Scene Scene { get; }
        public string Error { get; }
        public bool Succeeded => Status != SceneLoadStatus.Failed;

        public SceneLoadResult(SceneLoadStatus status, Scene scene, string error = null)
        {
            Status = status;
            Scene = scene;
            Error = error;
        }
    }

    public sealed class SceneLoader : MonoBehaviour
    {
        private readonly Dictionary<string, List<Action<SceneLoadResult>>> pendingLoads =
            new Dictionary<string, List<Action<SceneLoadResult>>>(StringComparer.Ordinal);

        public void LoadAdditive(
            string sceneNameOrPath,
            Action<SceneLoadResult> completed = null,
            Action<float> progressChanged = null,
            bool setActiveScene = false)
        {
            if (string.IsNullOrWhiteSpace(sceneNameOrPath))
            {
                CompleteCallback(completed, new SceneLoadResult(
                    SceneLoadStatus.Failed, default, "Scene name or path is empty."));
                return;
            }

            Scene loadedScene = FindScene(sceneNameOrPath);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                if (setActiveScene && !SceneManager.SetActiveScene(loadedScene))
                {
                    CompleteCallback(completed, new SceneLoadResult(
                        SceneLoadStatus.Failed, loadedScene, "Scene is loaded but could not be made active."));
                    return;
                }

                CompleteCallback(completed, new SceneLoadResult(SceneLoadStatus.AlreadyLoaded, loadedScene));
                return;
            }

            if (pendingLoads.TryGetValue(sceneNameOrPath, out List<Action<SceneLoadResult>> callbacks))
            {
                if (completed != null)
                    callbacks.Add(completed);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneNameOrPath))
            {
                CompleteCallback(completed, new SceneLoadResult(
                    SceneLoadStatus.Failed, default,
                    $"Scene '{sceneNameOrPath}' is not available in the current build scene list."));
                return;
            }

            pendingLoads.Add(sceneNameOrPath, new List<Action<SceneLoadResult>>());
            if (completed != null)
                pendingLoads[sceneNameOrPath].Add(completed);
            StartCoroutine(LoadRoutine(sceneNameOrPath, progressChanged, setActiveScene));
        }

        private IEnumerator LoadRoutine(string sceneNameOrPath, Action<float> progressChanged, bool setActiveScene)
        {
            AsyncOperation operation = null;
            Exception loadError = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(sceneNameOrPath, LoadSceneMode.Additive);
            }
            catch (Exception ex)
            {
                loadError = ex;
            }

            if (loadError != null)
            {
                FinishLoad(sceneNameOrPath, new SceneLoadResult(SceneLoadStatus.Failed, default, loadError.Message));
                yield break;
            }

            if (operation == null)
            {
                FinishLoad(sceneNameOrPath, new SceneLoadResult(
                    SceneLoadStatus.Failed, default, "Unity did not create a scene load operation."));
                yield break;
            }

            while (!operation.isDone)
            {
                try
                {
                    progressChanged?.Invoke(Mathf.Clamp01(operation.progress));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                yield return null;
            }

            Scene scene = FindScene(sceneNameOrPath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                FinishLoad(sceneNameOrPath, new SceneLoadResult(
                    SceneLoadStatus.Failed, default, "Unity completed the load but the requested scene is not loaded."));
                yield break;
            }

            if (setActiveScene && !SceneManager.SetActiveScene(scene))
            {
                FinishLoad(sceneNameOrPath, new SceneLoadResult(
                    SceneLoadStatus.Failed, scene, "Scene loaded additively but could not be made active."));
                yield break;
            }

            try
            {
                progressChanged?.Invoke(1f);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            FinishLoad(sceneNameOrPath, new SceneLoadResult(SceneLoadStatus.Loaded, scene));
        }

        private void FinishLoad(string sceneNameOrPath, SceneLoadResult result)
        {
            if (!pendingLoads.TryGetValue(sceneNameOrPath, out List<Action<SceneLoadResult>> callbacks))
                return;

            pendingLoads.Remove(sceneNameOrPath);
            if (!result.Succeeded)
                Debug.LogError($"[PigTycoon] Additive scene load failed: {result.Error}");

            foreach (Action<SceneLoadResult> callback in callbacks)
                CompleteCallback(callback, result);
        }

        private static Scene FindScene(string sceneNameOrPath)
        {
            Scene scene = SceneManager.GetSceneByPath(sceneNameOrPath);
            return scene.IsValid() ? scene : SceneManager.GetSceneByName(sceneNameOrPath);
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<string, List<Action<SceneLoadResult>>> pending in pendingLoads)
            {
                var result = new SceneLoadResult(
                    SceneLoadStatus.Failed, default, "SceneLoader was destroyed before loading completed.");
                foreach (Action<SceneLoadResult> callback in pending.Value)
                    CompleteCallback(callback, result);
            }
            pendingLoads.Clear();
        }

        private static void CompleteCallback(Action<SceneLoadResult> callback, SceneLoadResult result)
        {
            if (callback == null)
                return;

            try
            {
                callback.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
