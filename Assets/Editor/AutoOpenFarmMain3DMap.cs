using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoOpenFarmMain3DMap
{
    static AutoOpenFarmMain3DMap()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += Ensure3DMapSceneLoaded;
    }

    private static void Ensure3DMapSceneLoaded()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene active = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path) || active.name == "SampleScene" || active.name == "DongCoThaoNguyen")
        {
            string targetScene = "Assets/Scenes/Farm_Main_3D_Map.unity";
            if (System.IO.File.Exists(targetScene))
            {
                EditorSceneManager.OpenScene(targetScene, OpenSceneMode.Single);
            }
        }

        Camera mainCam = Camera.main;
        if (mainCam != null && SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.AlignViewToObject(mainCam.transform);
            SceneView.lastActiveSceneView.Repaint();
        }
    }
}
