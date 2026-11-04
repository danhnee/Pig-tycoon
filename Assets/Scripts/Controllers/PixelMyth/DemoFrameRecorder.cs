using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Opt-in frame recorder for the gameplay video (<c>-pigtycoon-scenario=video</c>). Locks the player to
    /// Time.captureFramerate = 30 so game time and the written frames stay in lockstep regardless of how slowly the
    /// machine renders, then writes every presented frame as JPG (quality 90) to
    /// <c>&lt;persistentDataPath&gt;/video_frames/f_000000.jpg</c> (override with <c>-pigtycoon-frames=&lt;dir&gt;</c>).
    /// JPG encoding runs on the main thread (needs the texture); disk writes go through a bounded background queue.
    /// </summary>
    public sealed class DemoFrameRecorder : MonoBehaviour
    {
        public const int FrameRate = 30;
        public const float MaxGameSeconds = 540f;   // full tour incl. the 2-hour herd recall check and a planned wave
        private const string FramesArg = "-pigtycoon-frames=";

        public static DemoFrameRecorder Instance { get; private set; }
        public string Directory { get; private set; }
        public float GameSeconds { get; private set; }
        public int FramesWritten { get; private set; }
        public bool Stopping { get; private set; }

        private BlockingCollection<KeyValuePair<string, byte[]>> queue;
        private Thread writer;
        private volatile Exception writerError;

        public static DemoFrameRecorder Create()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("DemoFrameRecorder");
            DontDestroyOnLoad(go);
            return go.AddComponent<DemoFrameRecorder>();
        }

        private void Awake()
        {
            Instance = this;
            Directory = Path.Combine(Application.persistentDataPath, "video_frames");
            foreach (var arg in Environment.GetCommandLineArgs())
                if (arg.StartsWith(FramesArg, StringComparison.Ordinal) && arg.Length > FramesArg.Length)
                    Directory = arg.Substring(FramesArg.Length).Trim('"');
            System.IO.Directory.CreateDirectory(Directory);
            foreach (var old in System.IO.Directory.GetFiles(Directory, "f_*.jpg")) File.Delete(old);

            Time.captureFramerate = FrameRate;

            Debug.Log($"[Autopilot] recorder: {Screen.width}x{Screen.height} @ {FrameRate} fps -> {Directory}");
        }

        private IEnumerator Start()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (!Stopping)
            {
                yield return endOfFrame;
                if (Stopping) yield break;
                GameSeconds += Time.deltaTime;
                var tex = CaptureCamera();
                if (tex != null)
                {
                    byte[] jpg = tex.EncodeToJPG(85);
                    Destroy(tex);
                    string path = Path.Combine(Directory, $"f_{FramesWritten:000000}.jpg");
                    // Write on the main thread and never block: the bounded queue + background writer deadlocked the
                    // player at frame 2 (0 % CPU) whenever the writer thread died silently (31 Oct runs 02:03-03:16).
                    try { File.WriteAllBytes(path, jpg); FramesWritten++; }
                    catch (Exception ex) { Debug.LogError($"[Autopilot] frame write failed {path}: {ex.GetType().Name}: {ex.Message}"); }
                    if (FramesWritten % 300 == 0)
                        Debug.Log($"[Autopilot] recorder: {FramesWritten} frames, game time {GameSeconds:0.0}s");
                }
                if (GameSeconds >= MaxGameSeconds) Finish($"{MaxGameSeconds:0}s game-time limit");
            }
        }

        private RenderTexture target;
        private int captureW = 1280, captureH = 720;
        private Texture2D readback;

        /// <summary>
        /// Render the main camera (and the HUD, moved to Screen Space - Camera) into an off-screen target.
        /// ScreenCapture needs a visible swap chain; the headless runner hides the player window, which made
        /// CaptureScreenshotAsTexture fail on every frame (first run 31 Oct: 0 frames).
        /// </summary>
        private Texture2D CaptureCamera()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            if (target == null)
            {
                // capture at the player's screen size (record_video.py sets it; 1920×1080 frames the view like a phone)
                captureW = Mathf.Max(640, Screen.width);
                captureH = Mathf.Max(360, Screen.height);
                target = new RenderTexture(captureW, captureH, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
                readback = new Texture2D(captureW, captureH, TextureFormat.RGB24, false);
            }
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f + canvas.sortingOrder * 0.0001f;
            }
            var previous = cam.targetTexture;
            cam.targetTexture = target;
            cam.Render();
            cam.targetTexture = previous;
            var active = RenderTexture.active;
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, captureW, captureH), 0, 0, false);
            readback.Apply(false);
            RenderTexture.active = active;
            var copy = new Texture2D(captureW, captureH, TextureFormat.RGB24, false);
            copy.SetPixels32(readback.GetPixels32());
            return copy;
        }

        private void WriteLoop()
        {
            try { foreach (var item in queue.GetConsumingEnumerable()) File.WriteAllBytes(item.Key, item.Value); }
            catch (Exception ex) { writerError = ex; }
        }

        /// <summary>Flush queued frames and quit the player.</summary>
        public void Finish(string reason)
        {
            if (Stopping) return;
            Stopping = true;
            Debug.Log($"[Autopilot] finish ({reason}): {FramesWritten} frames = {FramesWritten / (float)FrameRate:0.0}s of video in {Directory}");
            Application.Quit();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
