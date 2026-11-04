using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// "Does the UI really play, or is it for show?" — opt-in probe (<c>-pigtycoon-scenario=uiprobe</c>) that clicks every
    /// visible control in the built player the way a finger would: a raycast at the control's screen centre, then
    /// pointer down/up/click on whatever is really on top. Before/after each click it compares the whole Core save,
    /// the open popups, the toast channel and the UI tree, and writes one line per control:
    ///   REAL (engine changed) · OPENS (a panel) · FEEDBACK (toast only) · UI (visual change only) · DEAD (nothing) ·
    ///   COVERED (another element takes the tap) · OFFSCREEN.
    /// Popups a click opens are probed one level deep and must close with their own close button.
    /// Report: <c>-pigtycoon-uiprobe-out=&lt;file.md&gt;</c> (default persistentDataPath/uiprobe.md). Never runs without the flag.
    /// </summary>
    public sealed class UiProbe : MonoBehaviour
    {
        public const string Flag = "-pigtycoon-scenario=uiprobe";
        const int MaxClicks = 400;
        static readonly string[] CloseWords = { "đóng", "close", "×", "x", "thoát", "hủy", "huỷ", "quay lại" };

        readonly List<string> lines = new List<string>();
        readonly Dictionary<string, int> tally = new Dictionary<string, int>();
        readonly HashSet<string> probed = new HashSet<string>();
        readonly HashSet<string> probedPanels = new HashSet<string>();
        int clicks, shots;

        static string ReportPath => Environment.GetCommandLineArgs().Select(a => a.StartsWith("-pigtycoon-uiprobe-out=") ? a.Substring(23) : null).FirstOrDefault(a => a != null)
                                    ?? System.IO.Path.Combine(Application.persistentDataPath, "uiprobe.md");

        static bool Requested => Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!Requested || FindFirstObjectByType<UiProbe>() != null) return;
            var go = new GameObject("UiProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<UiProbe>();
        }

        static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        IEnumerator Start()
        {
            float waited = 0f;
            while (!(Engine != null && PlayerMobileController.Instance != null && EventSystem.current != null) && waited < 60f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(4f);
            if (VnStoryDialoguePopup.IsOpen) yield return SkipStory();
            lines.Add($"# UI probe — {DateTime.Now:yyyy-MM-dd HH:mm}, scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}, {Screen.width}×{Screen.height}");
            lines.Add("");
            lines.Add("| # | Layer | Control | Verdict | Evidence |");
            lines.Add("|---|---|---|---|---|");

            var hud = Clickables();
            foreach (var c in hud) probed.Add(Key(c));
            foreach (var c in hud)
            {
                if (clicks >= MaxClicks) break;
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                yield return ProbeOne(c, "HUD", depth: 0);
            }
            yield return PanelShots();
            Finish();
        }

        /// <summary>One picture per main panel, so a person can judge the look (the headless window cannot be
        /// screen-captured; render the camera with the HUD moved to screen-space-camera, like DemoFrameRecorder).</summary>
        IEnumerator PanelShots()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ReportPath) ?? ".", "uiprobe_panels");
            Directory.CreateDirectory(dir);
            var steps = new (string name, Action open)[]
            {
                ("hud", () => { }),
                ("equipment", () => CharacterPanel.Show(0)),
                ("skills", () => CharacterPanel.Show(1)),
                ("potential", () => CharacterPanel.Show(3)),
                ("bag", () => BackpackPopup.Instance?.Show()),
                ("farm_overview", () => FarmOverviewPopup.Instance?.TogglePopup()),
                ("defense", () => DefenseBuildPopup.Instance?.Show()),
                ("journal", () => QuestTrackerPanel.Instance?.ToggleJournal()),
            };
            foreach (var (name, open) in steps)
            {
                CloseAll(null);
                yield return new WaitForSecondsRealtime(0.3f);
                try { open(); } catch (Exception ex) { Debug.LogWarning($"[UiProbe] open {name}: {ex.Message}"); }
                yield return new WaitForSecondsRealtime(0.8f);
                yield return new WaitForEndOfFrame();
                try
                {
                    var png = RenderView();
                    if (png != null) File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), png);
                }
                catch (Exception ex) { Debug.LogWarning($"[UiProbe] shot {name}: {ex.Message}"); }
            }
            CloseAll(null);
        }

        static byte[] RenderView()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            int w = Mathf.Max(640, Screen.width), h = Mathf.Max(360, Screen.height);
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f + canvas.sortingOrder * 0.0001f; // same as DemoFrameRecorder
            }
            Canvas.ForceUpdateCanvases();
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            tex.Apply(false);
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            var bytes = tex.EncodeToPNG();
            Destroy(tex);
            return bytes;
        }

        IEnumerator ProbeOne(Component target, string layer, int depth)
        {
            clicks++;
            string label = Label(target);
            // The game keeps running (dodges, swings and walks need time). What changes on its own during a quiet
            // half second is drift, and is subtracted from what changes after the click.
            var before = Snapshot();
            yield return new WaitForSecondsRealtime(0.5f);
            var mid = Snapshot();
            var hudBefore = new HashSet<string>(Clickables().Select(Key));
            var (verdictHit, hitName) = Click(target);
            // short clips (counter, dodge) can start and end inside the wait: remember every clip shown
            var seen = new HashSet<string>();
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                var a = PlayerMobileController.Instance != null ? PlayerMobileController.Instance.GetComponent<CharacterSpriteAnimator>() : null;
                if (a != null && a.Myth != null && !string.IsNullOrEmpty(a.Myth.ShownClip)) seen.Add(a.Myth.ShownClip);
                yield return null;
            }
            var after = Snapshot();
            seen.Remove("idle"); seen.Remove(mid.Anim);
            if (seen.Count > 0 && (after.Anim == mid.Anim || after.Anim == "idle")) after.Anim = string.Join("/", seen);
            if (verdictHit != null) { Row(layer, label, verdictHit, hitName); yield return null; yield return null; yield break; }

            string verdict = Classify(before, mid, after, out string evidence);
            Row(layer, label, verdict, evidence);
            yield return null; // let a screenshot requested by Row capture this state

            var opened = after.Popups.Except(mid.Popups).ToList();
            string panel = string.Join("+", opened);
            if (opened.Count > 0 && depth == 0)
            {
                var inner = Clickables().Where(c => !hudBefore.Contains(Key(c))).ToList();
                var closers = inner.Where(IsCloser).ToList();
                if (probedPanels.Add(panel))
                {
                    Row(panel, "(panel)", "INFO", $"{inner.Count} tappable controls, {closers.Count} close");
                    // the panel's own controls, backdrops (click-outside) last; reopen the panel if a control closed it
                    var todo = inner.Except(closers)
                        .OrderBy(c => c.gameObject.name.IndexOf("Backdrop", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0).ToList();
                    foreach (var c in todo)
                    {
                        if (clicks >= MaxClicks) break;
                        if (!Snapshot().Popups.Overlaps(opened))
                        {
                            CloseAll(null);
                            yield return new WaitForSecondsRealtime(0.3f);
                            Click(target);
                            yield return new WaitForSecondsRealtime(0.6f);
                            if (!Snapshot().Popups.Overlaps(opened)) { Row(panel, "(reopen)", "REOPEN-FAILS", "opener no longer opens the panel"); break; }
                        }
                        if (c == null || !c.gameObject.activeInHierarchy) continue;
                        yield return ProbeOne(c, panel, depth + 1);
                        if (Snapshot().Popups.Except(after.Popups).Any())
                        {
                            CloseAll(null); // a nested panel: closed; the outer one is reopened before the next control
                            yield return new WaitForSecondsRealtime(0.3f);
                        }
                    }
                }
                if (!Snapshot().Popups.Overlaps(opened))
                {
                    CloseAll(null);
                    yield return new WaitForSecondsRealtime(0.3f);
                    Click(target);
                    yield return new WaitForSecondsRealtime(0.6f);
                }
                if (Snapshot().Popups.Overlaps(opened))
                {
                    var closer = Clickables().Where(c => !hudBefore.Contains(Key(c))).Where(IsCloser)
                        .FirstOrDefault(c => c != null && c.gameObject.activeInHierarchy);
                    if (closer == null) Row(panel, "(close)", "NO-CLOSE", "no visible close/Đóng button in the panel");
                    else
                    {
                        clicks++;
                        var (bad, hit) = Click(closer);
                        yield return new WaitForSecondsRealtime(0.4f);
                        bool closed = !Snapshot().Popups.Overlaps(opened);
                        Row(panel, Label(closer), bad ?? (closed ? "CLOSES" : "CLOSE-FAILS"), bad != null ? hit : (closed ? "" : "panel still open after its close button"));
                    }
                }
                CloseAll(null);
                yield return new WaitForSecondsRealtime(0.3f);
            }
            else if (opened.Count > 0) CloseAll(null);
            if (VnStoryDialoguePopup.IsOpen && depth == 0) yield return SkipStory();
        }

        // ------------------------------------------------------------------ clicking

        (string verdict, string hitName) Click(Component target)
        {
            var rt = target.transform as RectTransform;
            var canvas = target.GetComponentInParent<Canvas>();
            if (rt == null || canvas == null) return ("OFFSCREEN", "no RectTransform/Canvas");
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[2]) * 0.5f);
            var scroll = target.GetComponentInParent<ScrollRect>();
            if (scroll != null && scroll.viewport != null)
            {
                // inside a scroll list: only rows inside the viewport are tappable; the rest needs a scroll first
                var vc = new Vector3[4];
                scroll.viewport.GetWorldCorners(vc);
                Vector2 lo = RectTransformUtility.WorldToScreenPoint(cam, vc[0]), hi = RectTransformUtility.WorldToScreenPoint(cam, vc[2]);
                if (centre.x < lo.x || centre.y < lo.y || centre.x > hi.x || centre.y > hi.y) return ("SCROLL", "outside the scroll viewport (not a bug)");
            }
            if (centre.x < 0 || centre.y < 0 || centre.x > Screen.width || centre.y > Screen.height) return ("OFFSCREEN", $"centre {centre}");

            var ped = new PointerEventData(EventSystem.current) { position = centre, button = PointerEventData.InputButton.Left, clickCount = 1 };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, hits);
            if (hits.Count == 0) return ("COVERED", "nothing receives a tap here: " + WhyNoRaycast(target));
            var top = hits[0].gameObject;
            var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(top);
            bool onTarget = handler != null && (handler == target.gameObject || handler.transform.IsChildOf(target.transform) || target.transform.IsChildOf(handler.transform));
            ped.pointerCurrentRaycast = ped.pointerPressRaycast = hits[0];
            ped.rawPointerPress = top;
            ped.eligibleForClick = true;
            var down = ExecuteEvents.ExecuteHierarchy(top, ped, ExecuteEvents.pointerDownHandler);
            ped.pointerPress = down != null ? down : handler;
            if (ped.pointerPress != null) ExecuteEvents.Execute(ped.pointerPress, ped, ExecuteEvents.pointerUpHandler);
            if (handler != null) ExecuteEvents.Execute(handler, ped, ExecuteEvents.pointerClickHandler);
            if (!onTarget) return ("COVERED", $"tap lands on '{Path(top.transform)}'");
            return (null, null);
        }

        /// <summary>Why a visible control gets no raycast: a parent CanvasGroup blocking or hiding it, a disabled
        /// raycaster, or no raycast-target graphic.</summary>
        static string WhyNoRaycast(Component target)
        {
            var why = new List<string>();
            for (var t = target.transform; t != null; t = t.parent)
            {
                var g = t.GetComponent<CanvasGroup>();
                if (g != null && (!g.blocksRaycasts || !g.interactable || g.alpha < 0.05f))
                    why.Add($"CanvasGroup on '{t.name}' blocks={g.blocksRaycasts} alpha={g.alpha:0.##}");
                var c = t.GetComponent<Canvas>();
                if (c != null)
                {
                    var gr = t.GetComponent<GraphicRaycaster>();
                    if (gr == null || !gr.isActiveAndEnabled) why.Add($"canvas '{t.name}' has no active GraphicRaycaster");
                }
            }
            if (!target.GetComponentsInChildren<Graphic>().Any(gr => gr.raycastTarget && gr.isActiveAndEnabled)) why.Add("no raycastTarget graphic");
            var root = target.GetComponentInParent<Canvas>()?.rootCanvas;
            why.Add($"root canvas '{(root != null ? root.name : "?")}'");
            return string.Join("; ", why);
        }

        // ------------------------------------------------------------------ state

        sealed class State
        {
            public string[] Save = new string[0];
            public HashSet<string> Popups = new HashSet<string>();
            public int Toasts;
            public string LastToast = "";
            public int ActiveUi;
            public string Tool = "";
            public string Anim = "";
            public int TextHash;
            public Vector2 Pos;
        }

        State Snapshot()
        {
            var s = new State();
            try
            {
                var e = Engine;
                if (e != null)
                    s.Save = SaveSystem.Serialize(e.CreateSaveData()).Split(new[] { ",\"" }, StringSplitOptions.None)
                        .Where(t => t.IndexOf("Clock", StringComparison.OrdinalIgnoreCase) < 0 && t.IndexOf("Time", StringComparison.OrdinalIgnoreCase) < 0
                                    && t.IndexOf("Minute", StringComparison.OrdinalIgnoreCase) < 0 && t.IndexOf("Seconds", StringComparison.OrdinalIgnoreCase) < 0)
                        .ToArray();
            }
            catch (Exception ex) { s.Save = new[] { "save failed: " + ex.Message }; }
            void P(bool open, string name) { if (open) s.Popups.Add(name); }
            P(CharacterPanel.IsOpen, "CharacterPanel");
            P(BackpackPopup.IsOpen, "Backpack");
            P(FarmOverviewPopup.Instance != null && FarmOverviewPopup.Instance.IsOpen, "FarmOverview");
            P(DefenseBuildPopup.Instance != null && DefenseBuildPopup.Instance.IsOpen, "DefenseBuild");
            P(PigInspectPopup.Instance != null && PigInspectPopup.Instance.ContentPanel != null && PigInspectPopup.Instance.ContentPanel.activeSelf, "PigInspect");
            P(RadialToolWheel.Instance != null && RadialToolWheel.Instance.IsOpen, "ToolWheel");
            P(DayMarketPopup.IsOpen, "DayMarket"); P(AuctionPopup.IsOpen, "Auction"); P(RecruitPopup.IsOpen, "Recruit");
            P(AppraisalPopup.IsOpen, "Appraisal"); P(ForestEventPopup.IsOpen, "ForestEvent"); P(IntelPopup.IsOpen, "Intel");
            P(BreedingPickerPopup.IsOpen, "BreedingPicker"); P(VnStoryDialoguePopup.IsOpen, "VnStory");
            P(VillageBarterPopup.Instance != null && VillageBarterPopup.Instance.IsOpen, "Barter");
            P(JournalOpen(), "QuestJournal");
            P(BuildingInteriorPopup.IsOpen, "Interior");
            P(MinimapHudWidget.IsWorldMapModalOpen, "WorldMap");
            P(DevModePopup.Instance != null && DevModePopup.Instance.ContentPanel != null && DevModePopup.Instance.ContentPanel.activeSelf, "DevMode");
            s.Toasts = ToastOverlay.ShownCount;
            s.LastToast = ToastOverlay.LastMessage ?? "";
            s.ActiveUi = FindObjectsByType<Graphic>(FindObjectsSortMode.None).Count(g => g.isActiveAndEnabled && g.canvas != null && g.canvas.renderMode != RenderMode.WorldSpace);
            s.Tool = HotbarController.Instance != null ? HotbarController.Instance.CurrentTool.ToString() : "";
            unchecked
            {
                int h = 17;
                foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                    if (t.isActiveAndEnabled && t.canvas != null && t.canvas.renderMode != RenderMode.WorldSpace && t.GetComponentInParent<ToastOverlay>() == null)
                        h = h * 31 + (t.text ?? "").GetHashCode();
                s.TextHash = h;
            }
            var player = PlayerMobileController.Instance;
            if (player != null)
            {
                s.Pos = player.transform.position;
                var anim = player.GetComponent<CharacterSpriteAnimator>();
                s.Anim = anim != null && anim.Myth != null ? anim.Myth.ShownClip ?? "" : "";
            }
            return s;
        }

        static bool JournalOpen()
        {
            var q = QuestTrackerPanel.Instance;
            if (q == null) return false;
            var f = typeof(QuestTrackerPanel).GetField("journal", BindingFlags.Instance | BindingFlags.NonPublic);
            return f != null && f.GetValue(q) is GameObject g && g != null && g.activeSelf;
        }

        static string TokenKey(string token) { int i = token.IndexOf(':'); return i > 0 ? token.Substring(0, i) : token; }

        /// <summary>a = before, m = after a quiet half second (drift), b = after the click.</summary>
        static string Classify(State a, State m, State b, out string evidence)
        {
            var drifting = new HashSet<string>(a.Save.Except(m.Save).Select(TokenKey));
            var added = b.Save.Except(m.Save).Where(t => !drifting.Contains(TokenKey(t))).ToList();
            var opened = b.Popups.Except(m.Popups).ToList();
            var closed = m.Popups.Except(b.Popups).ToList();
            bool moved = (b.Pos - m.Pos).sqrMagnitude > 0.04f && (m.Pos - a.Pos).sqrMagnitude < 0.01f;
            bool animated = b.Anim != m.Anim && b.Anim != "idle";
            var ev = new List<string>();
            if (added.Count > 0) ev.Add("save: " + string.Join(" ; ", added.Take(3).Select(t => Trim(t, 70))) + (added.Count > 3 ? $" (+{added.Count - 3})" : ""));
            if (opened.Count > 0) ev.Add("opens " + string.Join(",", opened));
            if (closed.Count > 0) ev.Add("closes " + string.Join(",", closed));
            if (b.Toasts > m.Toasts) ev.Add("toast “" + Trim(b.LastToast, 90) + "”");
            if (m.Tool != b.Tool) ev.Add($"tool {m.Tool}→{b.Tool}");
            if (animated) ev.Add($"anim {m.Anim}→{b.Anim}");
            if (moved) ev.Add($"moved {(b.Pos - m.Pos).magnitude:0.0} m");
            if (b.ActiveUi != m.ActiveUi) ev.Add($"ui {m.ActiveUi}→{b.ActiveUi}");
            bool textChanged = b.TextHash != m.TextHash && m.TextHash == a.TextHash;
            if (textChanged) ev.Add("text changed");
            evidence = string.Join(" · ", ev).Replace("|", "/");
            if (added.Count > 0 || m.Tool != b.Tool) return "REAL";
            if (opened.Count > 0) return "OPENS";
            if (closed.Count > 0) return "CLOSES";
            if (animated || moved) return "ACTION";
            if (b.Toasts > m.Toasts) return "FEEDBACK";
            if (b.ActiveUi != m.ActiveUi || textChanged) return "UI";
            return "DEAD";
        }

        // ------------------------------------------------------------------ discovery

        static List<Component> Clickables()
        {
            var list = new List<Component>();
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (!(mb is IPointerClickHandler) || !mb.isActiveAndEnabled) continue;
                if (mb is Selectable sel && !sel.IsInteractable()) { list.Add(mb); continue; } // reported as DEAD/disabled
                var canvas = mb.GetComponentInParent<Canvas>();
                if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) continue;
                if (mb.GetComponentInParent<UiProbe>() != null || mb.GetComponentInParent<ToastOverlay>() != null) continue;
                // dev cheats only: DevModePopup sits on the shared HUD canvas, so "has a DevModePopup parent" would hide real panels
                var dev = DevModePopup.Instance;
                if ((dev != null && dev.ContentPanel != null && mb.transform.IsChildOf(dev.ContentPanel.transform))
                    || Path(mb.transform).IndexOf("DevMode_", StringComparison.Ordinal) >= 0) continue;
                if (mb is ScrollRect || mb.GetType().Name == "InputField" || mb is TMP_InputField) continue;
                list.Add(mb);
            }
            // top-left to bottom-right, so the report reads like the screen
            return list.OrderByDescending(c => ((RectTransform)c.transform).position.y).ThenBy(c => ((RectTransform)c.transform).position.x).ToList();
        }

        static bool IsCloser(Component c)
        {
            string l = Label(c).ToLowerInvariant();
            string n = c.gameObject.name.ToLowerInvariant();
            return n.Contains("close") || CloseWords.Any(w => l == w || l.StartsWith(w + " ") || l.StartsWith("[" + w));
        }

        static string Label(Component c)
        {
            var t = c.GetComponentsInChildren<TMP_Text>(true).Select(x => x.text).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (t == null) { var u = c.GetComponentInChildren<Text>(true); t = u != null ? u.text : null; }
            string label = string.IsNullOrWhiteSpace(t) ? c.gameObject.name : StripTags(t);
            if (c is Selectable s && !s.IsInteractable()) label += " (disabled)";
            return Trim(label.Replace("\n", " ").Replace("|", "/"), 48) + $" <{c.GetType().Name}>";
        }

        static string Key(Component c) => c == null ? "" : c.GetEntityId().ToString();

        static string Path(Transform t)
        {
            var parts = new List<string>();
            for (int i = 0; t != null && i < 4; i++, t = t.parent) parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        static string StripTags(string s) => System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "").Trim();
        static string Trim(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        // ------------------------------------------------------------------ housekeeping

        void Row(string layer, string label, string verdict, string evidence)
        {
            tally[verdict] = tally.TryGetValue(verdict, out var n) ? n + 1 : 1;
            lines.Add($"| {clicks} | {layer} | {label} | **{verdict}** | {evidence} |");
            Debug.Log($"[UiProbe] {layer} | {label} | {verdict} | {evidence}");
        }

        static void CloseAll(string only)
        {
            try
            {
                if (CharacterPanel.IsOpen) CharacterPanel.Toggle();
                if (BackpackPopup.IsOpen) BackpackPopup.Instance.Hide();
                if (FarmOverviewPopup.Instance != null && FarmOverviewPopup.Instance.IsOpen) FarmOverviewPopup.Instance.TogglePopup();
                if (DefenseBuildPopup.Instance != null && DefenseBuildPopup.Instance.IsOpen) DefenseBuildPopup.Instance.Hide();
                if (PigInspectPopup.Instance != null) PigInspectPopup.Instance.Hide();
                if (RadialToolWheel.Instance != null && RadialToolWheel.Instance.IsOpen) RadialToolWheel.Instance.Close(false);
                if (QuestTrackerPanel.Instance != null) QuestTrackerPanel.Instance.CloseJournal();
                if (DayMarketPopup.IsOpen) DayMarketPopup.Instance.Close();
                if (AuctionPopup.IsOpen) AuctionPopup.Instance.Close();
                if (RecruitPopup.IsOpen) RecruitPopup.Instance.Close();
                if (AppraisalPopup.IsOpen) AppraisalPopup.Instance.Close();
                if (ForestEventPopup.IsOpen) ForestEventPopup.Instance.Close();
                if (IntelPopup.IsOpen) IntelPopup.Instance.Close();
                if (BreedingPickerPopup.IsOpen) BreedingPickerPopup.Instance.Close();
                if (VillageBarterPopup.Instance != null && VillageBarterPopup.Instance.IsOpen) VillageBarterPopup.Instance.Hide();
                if (BuildingInteriorPopup.IsOpen) BuildingInteriorPopup.Instance.Close();
                if (MinimapHudWidget.IsWorldMapModalOpen) MinimapHudWidget.ToggleWorldMapModal();
            }
            catch (Exception ex) { Debug.LogWarning("[UiProbe] close failed: " + ex.Message); }
        }

        IEnumerator SkipStory()
        {
            var runner = Engine?.Narrative?.Runner;
            for (int i = 0; i < 20 && VnStoryDialoguePopup.IsOpen; i++)
            {
                try { runner?.Skip(); } catch { }
                yield return new WaitForSecondsRealtime(0.3f);
            }
        }

        void Finish()
        {
            lines.Insert(1, "");
            lines.Insert(2, "Totals: " + string.Join(", ", tally.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}")) + $" — {clicks} clicks.");
            string outPath = ReportPath;
            try { File.WriteAllText(outPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false)); Debug.Log("[UiProbe] report " + outPath); }
            catch (Exception ex) { Debug.LogError("[UiProbe] report failed: " + ex.Message); }
            Application.Quit();
        }
    }
}
