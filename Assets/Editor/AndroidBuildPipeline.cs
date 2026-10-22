#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PigTycoon.EditorTools
{
    /// <summary>
    /// Tiện ích tự động hóa xuất bản Android (Android Automated Build Pipeline):
    /// Cung cấp menu 'Tools > PigTycoon > Build Android APK' trong Unity Editor.
    /// Tự động cấu hình ARM64, Min SDK 26, IL2CPP, khóa màn hình ngang và đóng gói file APK.
    /// </summary>
    public static class AndroidBuildPipeline
    {
        private const string OutputDir = "Builds/Android";
        private const string ApkName = "PigTycoon.apk";

        [MenuItem("Tools/PigTycoon/Build Android APK")]
        public static void BuildAndroidApk()
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            string outputPath = Path.Combine(OutputDir, ApkName);

            // 1. Cấu hình thông số chuẩn cho nền tảng Android
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8.0
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            string currentAppId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(currentAppId) || currentAppId.Contains("DefaultCompany"))
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.pigtycoon.core");
            }

            // 2. Thu thập danh sách Scene cần đóng gói
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                BoRiaVillageSetup.UpdateBuildSettings();
                scenes = EditorBuildSettings.scenes;
            }

            string[] scenePaths = new string[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenePaths[i] = scenes[i].path;
            }

            // 3. Tiến hành Build Player
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[PigTycoon] Đang tiến hành biên dịch và đóng gói APK Android tại: {outputPath}...");
            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[PigTycoon] XUẤT FILE APK THÀNH CÔNG!</color> Dung lượng: {summary.totalSize / (1024 * 1024)} MB tại {outputPath}");
                EditorUtility.RevealInFinder(outputPath);
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[PigTycoon] Build APK thất bại với {summary.totalErrors} lỗi!");
            }
        }
    }
}
#endif
