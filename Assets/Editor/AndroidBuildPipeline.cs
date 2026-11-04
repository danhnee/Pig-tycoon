#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
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

            // Android chỉ đọc được ảnh trong Resources: đóng gói HUD/UI + mọi ảnh bản PC đã đọc khi chơi trọn tuyến
            MythHudPackaging.PackageRuntimeArt();

            // Xuất file .apk cài được (không phải dự án Gradle, không phải .aab)
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.buildAppBundle = false;

            // 1. Cấu hình thông số chuẩn cho nền tảng Android
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8.0
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // Tên hiện trên máy tester và số phiên bản (mỗi lần build tăng versionCode theo giờ build)
            PlayerSettings.productName = "Pig Tycoon";
            PlayerSettings.companyName = "Pig Tycoon Team";
            PlayerSettings.bundleVersion = "0.10.0-test";
            PlayerSettings.Android.bundleVersionCode = (int)((System.DateTime.UtcNow - new System.DateTime(2026, 1, 1)).TotalHours);

            string currentAppId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(currentAppId) || currentAppId.Contains("DefaultCompany"))
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.pigtycoon.core");
            }

            // 2. Thu thập danh sách Scene cần đóng gói
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
                throw new System.InvalidOperationException("EditorBuildSettings has no scenes; run PixelMythWorldBuilder.BuildAll first.");

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
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Debug.LogError($"[PigTycoon] Build APK thất bại ({summary.result}) với {summary.totalErrors} lỗi!");
                if (Application.isBatchMode) EditorApplication.Exit(1); // để headless runner báo lỗi thật
            }
        }
    }
}
#endif
