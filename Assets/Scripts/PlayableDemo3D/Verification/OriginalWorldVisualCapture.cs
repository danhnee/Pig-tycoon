using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PigTycoon.Candidates.EvaluatedExport;
using UnityEngine;

namespace PigTycoon.PlayableDemo3D.Verification
{
    /// <summary>
    /// Opt-in standalone GPU visual capture for OriginalBlenderWorld.unity.
    /// Activated only when -originalWorldCaptureDir is passed on the command line.
    /// </summary>
    public sealed class OriginalWorldVisualCapture : MonoBehaviour
    {
        [Serializable]
        private sealed class CaptureReceipt
        {
            public bool passed;
            public string unityVersion;
            public string graphicsDevice;
            public long loadedInstances;
            public int totalBatches;
            public string[] screenshots;
        }

        private static string GetArg(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int idx = Array.IndexOf(args, key);
            return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : "";
        }

        private static bool HasFlag(string flag) => Array.IndexOf(Environment.GetCommandLineArgs(), flag) >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallIfRequested()
        {
            string dir = GetArg("-originalWorldCaptureDir");
            if (string.IsNullOrWhiteSpace(dir)) dir = GetArg("-originalWorldCaptureOut");
            if (string.IsNullOrWhiteSpace(dir)) return;
            var go = new GameObject("OriginalWorldVisualCapture");
            DontDestroyOnLoad(go);
            go.AddComponent<OriginalWorldVisualCapture>();
        }

        private IEnumerator Start()
        {
            string rawDir = GetArg("-originalWorldCaptureDir");
            if (string.IsNullOrWhiteSpace(rawDir)) rawDir = GetArg("-originalWorldCaptureOut");
            string outDir = Path.GetFullPath(rawDir);
            Directory.CreateDirectory(outDir);

            // Wait for Bootstrap, PlayableFarmController, Animator pose evaluation, and EvaluatedMapViewer
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            var viewer = FindAnyObjectByType<EvaluatedMapViewer>();
            var farm = FindAnyObjectByType<PlayableFarmController>();
            var manGo = GameObject.Find("NPC_Man_GuideCandidate");

            float khoaY = farm != null ? farm.GroundHeight(0f, -15f) : 7.220f;
            float manY = farm != null ? farm.GroundHeight(-4.8f, -15f) : 7.133f;
            float pigY = farm != null ? farm.GroundHeight(-2.0f, -11.8f) : 7.235f;

            Vector3 khoaPos = farm != null && farm.PlayerRoot != null ? farm.PlayerRoot.position : new Vector3(0f, khoaY, -15f);
            Vector3 manPos = manGo != null ? manGo.transform.position : new Vector3(-4.8f, manY, -15f);

            // Orient Khoa and Man toward the portrait cameras (-Z front) during capture
            Quaternion savedPlayerRot = Quaternion.identity;
            if (farm != null && farm.PlayerRoot != null)
            {
                savedPlayerRot = farm.PlayerRoot.rotation;
                farm.PlayerRoot.rotation = Quaternion.Euler(0f, 170f, 0f);
            }
            Quaternion savedManRot = Quaternion.identity;
            if (manGo != null)
            {
                savedManRot = manGo.transform.rotation;
                manGo.transform.rotation = Quaternion.Euler(0f, 165f, 0f);
            }
            var pigGo = GameObject.Find("FarmPig_01_HongDien");
            Quaternion savedPigRot = pigGo != null ? pigGo.transform.rotation : Quaternion.identity;

            var shots = new (string file, Vector3 pos, Vector3 lookAt, float fov, bool cullMicro)[]
            {
                ("01_original_world_overview.png", new Vector3(-16f, 76f, -88f), new Vector3(8f, 12f, 22f), 46f, false),
                ("02_image5_exact_meadow_overview.png", new Vector3(-4.5f, 36.5f, -48.0f), new Vector3(1.5f, 10.5f, 12.0f), 44f, false),
                ("03_farm_pasture_gameplay.png", new Vector3(0f, khoaPos.y + 8.5f, -24.5f), new Vector3(0f, khoaPos.y + 0.8f, -12.0f), 45f, false),
                ("04_khoa_closeup_unity.png", new Vector3(khoaPos.x + 0.45f, khoaPos.y + 1.34f, khoaPos.z - 2.15f), new Vector3(khoaPos.x, khoaPos.y + 1.02f, khoaPos.z), 36f, false),
                ("05_man_closeup_unity.png", new Vector3(manPos.x + 0.45f, manPos.y + 1.28f, manPos.z - 2.05f), new Vector3(manPos.x, manPos.y + 0.98f, manPos.z), 36f, false),
                ("06_pigs_elastic_grass_unity.png", new Vector3(-1.05f, pigY + 0.86f, -13.95f), new Vector3(-1.85f, pigY + 0.34f, -11.55f), 38f, false),
                ("07_mainhouse_and_lab_unity.png", new Vector3(-25f, 22f, -12f), new Vector3(-50f, 11f, 28f), 45f, false),
                ("08_observatory_and_bridge_unity.png", new Vector3(16f, 26f, 6f), new Vector3(52f, 12f, 44f), 46f, false),
                ("09_harbor_town_unity.png", new Vector3(212f, 36f, 42f), new Vector3(148f, 11f, 96f), 44f, false)
            };

            var camGo = new GameObject("__VerificationRenderCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.74f, 0.83f, 0.92f, 1f);

            int width = 1920, height = 1080;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            var savedPaths = new List<string>();

            foreach (var shot in shots)
            {
                if (pigGo != null)
                {
                    pigGo.transform.rotation = shot.file == "06_pigs_elastic_grass_unity.png"
                        ? Quaternion.Euler(0f, 148f, 0f)
                        : savedPigRot;
                }
                if (viewer != null)
                {
                    viewer.enableFrustumCulling = true;
                    viewer.microScatterCullDistance = shot.cullMicro ? 180f : 380f;
                }
                cam.fieldOfView = shot.fov;
                cam.transform.position = shot.pos;
                cam.transform.LookAt(shot.lookAt);
                cam.targetTexture = rt;

                if (viewer != null) viewer.SubmitBatches(cam, cam);
                cam.Render();

                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                string outPath = Path.Combine(outDir, shot.file);
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                savedPaths.Add(outPath);
            }

            if (viewer != null) viewer.microScatterCullDistance = 280f;
            if (farm != null && farm.PlayerRoot != null) farm.PlayerRoot.rotation = savedPlayerRot;
            if (manGo != null) manGo.transform.rotation = savedManRot;
            if (pigGo != null) pigGo.transform.rotation = savedPigRot;

            cam.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            Destroy(tex);
            Destroy(camGo);

            // Capture live interactive UI/UX modals, building inspection cameras, and open-world combat vs 3D monsters
            if (farm != null && farm.Session != null)
            {
                // 10. Research Lab 3D Inspection + 4-Channel Appraisal & F1/F2 Breeding Terminal UI
                farm.InspectBuildingByKind(FarmInteractionKind.ResearchLab, 2);
                farm.Session.AppraisePig4Channel(farm.Session.Engine.Pigs.Count > 0 ? farm.Session.Engine.Pigs[0].Id : "", PigTycoon.Core.AppraisalChannel.PhongNghienCuu);
                yield return null;
                yield return new WaitForEndOfFrame();
                SaveScreenCapture(outDir, "10_live_building_terminal_ui.png", savedPaths);

                // 11. Warehouse & Observatory Upgrade Terminal UI
                farm.InspectBuildingByKind(FarmInteractionKind.ObservatoryIntel, 5);
                farm.Session.UpgradeWarehouseCapacity();
                farm.Session.ScanObservatoryIntel();
                yield return null;
                yield return new WaitForEndOfFrame();
                SaveScreenCapture(outDir, "11_live_observatory_intel_ui.png", savedPaths);

                // 12. Return to Khoa in golden-hour daylight, spawn open-world monster skirmish, and execute combo + martial slash
                farm.ExitBuildingStation();
                farm.Session.ShiftTimeToHour(10);
                farm.UpdateDynamicDayNightLighting();
                float prevDist = farm.CameraDistance, prevPitch = farm.CameraPitch, prevYaw = farm.CameraYaw;
                farm.CameraDistance = 6.6f;
                farm.CameraPitch = 22f;
                farm.CameraYaw = -10f;
                if (farm.PlayerRoot != null)
                    farm.PlayerRoot.rotation = Quaternion.Euler(0f, -8f, 0f);
                var raidView = FindAnyObjectByType<PlayableFarmRaidView>();
                if (raidView != null)
                {
                    raidView.SpawnImmediateSkirmish();
                    yield return null;
                    raidView.PracticeOrAttackCombo();
                    raidView.TriggerMartialSkill();
                }
                yield return new WaitForSeconds(0.08f);
                yield return new WaitForEndOfFrame();
                SaveScreenCapture(outDir, "12_live_combat_slash_and_dusk.png", savedPaths);
                farm.CameraDistance = prevDist;
                farm.CameraPitch = prevPitch;
                farm.CameraYaw = prevYaw;
            }

            var receipt = new CaptureReceipt
            {
                passed = savedPaths.Count >= shots.Length,
                unityVersion = Application.unityVersion,
                graphicsDevice = SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")",
                loadedInstances = viewer != null ? viewer.loadedInstances : 0,
                totalBatches = viewer != null ? viewer.totalBatches : 0,
                screenshots = savedPaths.ToArray()
            };
            File.WriteAllText(Path.Combine(outDir, "gpu_capture_receipt.json"), JsonUtility.ToJson(receipt, true));

            if (HasFlag("-originalWorldCaptureOnly"))
            {
                Application.Quit(0);
            }
        }

        private static void SaveScreenCapture(string outDir, string fileName, List<string> savedPaths)
        {
            var screenTex = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenTex == null) return;
            string p = Path.Combine(outDir, fileName);
            File.WriteAllBytes(p, screenTex.EncodeToPNG());
            Destroy(screenTex);
            savedPaths.Add(p);
        }
    }
}
