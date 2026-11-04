using System;
using System.IO;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>Lifecycle boundary: acknowledge native return only after a durable save succeeds.</summary>
    public sealed class PixelWorldAndroidBridge : MonoBehaviour
    {
        static PixelWorldAndroidBridge instance;
        static string SavePath => PlayableDemoSmoke.IsRequested ? PlayableDemoSmoke.SavePath :
            Path.Combine(Application.persistentDataPath, "pig_tycoon_save.json");
        void Awake()
        {
            if (instance != null) { Destroy(gameObject); return; }
            instance = this;
            gameObject.name = "PixelWorldAndroidBridge";
            DontDestroyOnLoad(gameObject);
        }

        public bool TrySave(out string error)
        {
            if (!string.IsNullOrEmpty(MobileGameController.Instance?.SaveLoadError))
            {
                error = "Bản lưu cũ cần khôi phục; không ghi đè dữ liệu.";
                return false;
            }
            string temporary = null;
            try
            {
                var engine = MobileGameController.Instance?.Engine;
                if (engine == null) throw new InvalidOperationException("Phiên chơi chưa sẵn sàng để lưu.");
                var player = FindFirstObjectByType<PlayerMobileController>();
                var scene = FindFirstObjectByType<PixelWorldScene>();
                // During async travel the new actor can exist before the domain warp commits.
                if (player != null && scene != null && scene.LocationId == engine.WorldLocations.CurrentLocationId)
                {
                    engine.WorldLocations.PlayerTileX = Mathf.RoundToInt(player.transform.position.x);
                    engine.WorldLocations.PlayerTileY = Mathf.RoundToInt(player.transform.position.y);
                }
                string path = SavePath;
                temporary = path + ".pending";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string json = SaveSystem.Serialize(engine.CreateSaveData());
                using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(file))
                {
                    writer.Write(json);
                    writer.Flush();
                    file.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Debug.LogError("[PigTycoon] Không thể lưu trước khi đóng: " + error);
                return false;
            }
            finally
            {
                if (temporary != null && File.Exists(temporary))
                {
                    try { File.Delete(temporary); }
                    catch (Exception ex) { Debug.LogWarning("Không thể dọn tệp lưu tạm: " + ex.Message); }
                }
            }
        }

        public void SaveAndReturn(string payload)
        {
            if (!TrySave(out var error))
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var host = new AndroidJavaClass("com.pigtycoon.pig_tycoon_flutter_client.UnityGameActivity"))
                    host.CallStatic("saveFailed");
#endif
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var host = new AndroidJavaClass("com.pigtycoon.pig_tycoon_flutter_client.UnityGameActivity"))
                host.CallStatic("finishAfterSave");
#endif
        }

        void OnApplicationPause(bool paused) { if (paused) TrySave(out _); }
        void OnApplicationQuit() { TrySave(out _); }
        void OnDestroy() { if (instance == this) instance = null; }
    }
}
