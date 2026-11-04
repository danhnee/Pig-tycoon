using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// ADR-0027 Visual Novel & In-World Cutscene View (<see cref="INarrativeView"/>).
    /// Renders <see cref="NarrativeRunner"/> directly:
    /// - Full-screen illustrated <see cref="NarrativeRunner.BackdropId"/> (with automatic <see cref="NarrativeBackdrops.Base"/> fallback).
    /// - Left & Right 2.5D Bust Portraits (<see cref="UISpriteLoader.GetPortrait"/>), speaker nameplate color, and role badge.
    /// - Centered narration strip (<see cref="LineView.IsNarration"/>) and chapter/location <see cref="RunnerPhase.Banner"/> card.
    /// - Branching choices (<see cref="RunnerPhase.Choice"/>) with locked-option reason display.
    /// - Scrollable <see cref="NarrativeRunner.Backlog"/> viewer, <see cref="NarrativeRunner.Auto"/> toggle,
    ///   hold-to-<see cref="NarrativeRunner.FastForward"/> (with <see cref="NarrativeSettings.SkipUnread"/> option),
    ///   and <see cref="NarrativeRunner.Skip"/> confirmation with HSR-style <see cref="SceneDefinition.Recap"/>.
    /// - Stage commands (<see cref="Perform"/>: Fade, Backdrop, Shake, Emote, Sfx, Music) and <see cref="OnCustom"/>
    ///   (<c>reflection:&lt;portrait&gt;</c>, <c>notebook:&lt;id&gt;</c>, <c>tutorial:&lt;verb&gt;</c>).
    /// </summary>
    public sealed class VnStoryDialoguePopup : MonoBehaviour, INarrativeView
    {
        public static VnStoryDialoguePopup Instance { get; private set; }
        public static bool IsOpen =>
            Instance != null &&
            ((Instance.panel != null && Instance.panel.activeSelf) ||
             (MobileGameController.Instance?.Engine?.Narrative?.Runner?.InScene ?? false));

        /// <summary>
        /// Ensures the UI hierarchy is built when queried by <see cref="NarrativeStageController"/> so scenes never auto-skip.
        /// </summary>
        public bool Ready
        {
            get
            {
                if (panel == null) EnsureBuilt();
                return panel != null;
            }
        }

        public struct DialogueBeat
        {
            public bool SpeakerIsHero;
            public string SpeakerName;
            public string HeroEmotion;
            public string NpcEmotion;
            public string Text;
        }

        private Canvas dedicatedCanvas;
        private GameObject panel;
        private Image panelBgImage;
        private GameObject backdropLayer;
        private Image backdropImage;
        private TextMeshProUGUI backdropCaption;

        private GameObject leftFrame;
        private Image leftPortrait;
        private GameObject rightFrame;
        private Image rightPortrait;
        private TextMeshProUGUI emoteBadgeText;
        private float emoteTimer;

        private GameObject vnBox;
        private RectTransform vnBoxRect;
        private GameObject headerBannerGo;
        private Vector2 vnBoxBasePos = new Vector2(0f, -190f);
        private TextMeshProUGUI speakerTitle;
        private TextMeshProUGUI affectionBadge;
        private TextMeshProUGUI dialogueText;

        private Button nextBtn, autoBtn, ffBtn, backlogBtn, skipBtn, shopBtn, closeBtn;
        private TextMeshProUGUI nextBtnLabel, autoBtnLabel, ffBtnLabel, backlogBtnLabel, skipBtnLabel, shopBtnLabel;
        private bool ffHeldByPointer;

        // Choice overlay
        private GameObject choiceContainer;
        private TextMeshProUGUI choicePromptText;
        private readonly Button[] choiceButtons = new Button[5];
        private readonly TextMeshProUGUI[] choiceLabels = new TextMeshProUGUI[5];

        // Chapter / Location Banner card
        private GameObject bannerCard;
        private TextMeshProUGUI bannerText;

        // Backlog modal
        private GameObject backlogModal;
        private TextMeshProUGUI backlogContentText;

        // Skip / Recap confirmation modal
        private GameObject skipConfirmModal;
        private TextMeshProUGUI skipRecapText;

        // Reflection / Custom effect card (Khoa's mirror reflections & tutorial banners)
        private GameObject reflectionCard;
        private Image reflectionPortraitImg;
        private TextMeshProUGUI reflectionCaptionText;
        private float reflectionTimer;

        // Full-screen Fade overlay (for NarrativeCmd.Fade out/in)
        private Image fadeOverlay;
        private float shakeTimer;
        private float shakeMagnitude = 8f;

        // Optional shop callback when opened from an NPC interaction
        private Action onOpenShopCallback;
        private string currentNpcId = "";
        private string lastSceneId = "";

        // Legacy fallback beats (used only if Open() is called for an NPC without a registered Talk scene)
        private bool legacyModeActive;
        private readonly List<DialogueBeat> legacyBeats = new List<DialogueBeat>();
        private int legacyBeatIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallBeforeSceneLoad()
        {
            EnsureBootstrapInstance();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureBootstrapInstance();
            Instance.EnsureBuilt();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureBootstrapInstance();
            if (Instance != null)
            {
                NarrativeStageController.View = Instance;
                Instance.EnsureBuilt();
            }
        }

        private static void EnsureBootstrapInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("VnNarrativeViewHost");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<VnStoryDialoguePopup>();
            }
            NarrativeStageController.View = Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            NarrativeStageController.View = this;
        }

        private void OnEnable()
        {
            NarrativeStageController.View = this;
        }

        // =====================================================================
        // PUBLIC ENTRY POINTS (Backwards-compatible with NPC / Shop callers)
        // =====================================================================

        public static void Open(string npcId, string displayName, string initialLine = null, Action onOpenShop = null)
        {
            EnsureBootstrapInstance();
            Instance.EnsureBuilt();
            Instance.currentNpcId = string.IsNullOrWhiteSpace(npcId) ? "ba_sau" : npcId.Trim().ToLowerInvariant();
            Instance.onOpenShopCallback = onOpenShop;

            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine != null)
            {
                var scene = engine.TalkTo(Instance.currentNpcId);
                if (scene != null && engine.Narrative.Runner.InScene)
                {
                    Instance.legacyModeActive = false;
                    Instance.SyncShopButtonVisibility();
                    Instance.RefreshFromRunner(engine, engine.Narrative.Runner);
                    return;
                }
            }

            // Fallback if no NarrativeTrigger.Talk scene matched
            Instance.StartLegacyFallback(Instance.currentNpcId, displayName, initialLine, onOpenShop);
        }

        public static void OpenWithBeats(string npcId, string displayName, IList<DialogueBeat> externalBeats, Action onOpenShop = null, Action onFinishedAllBeats = null)
        {
            Open(npcId, displayName, externalBeats != null && externalBeats.Count > 0 ? externalBeats[0].Text : null, onOpenShop);
        }

        public static void ShowExternalBeat(string npcId, string displayName, DialogueBeat beat, Action onNextBeat, Action onOpenShop = null)
        {
            Open(npcId, displayName, beat.Text, onOpenShop);
        }

        public void Close()
        {
            var runner = MobileGameController.Instance?.Engine?.Narrative?.Runner;
            if (runner != null && runner.InScene && runner.Scene != null && runner.Scene.Skippable)
            {
                runner.Skip();
            }
            legacyModeActive = false;
            if (panel != null) panel.SetActive(false);
            if (backlogModal != null) backlogModal.SetActive(false);
            if (skipConfirmModal != null) skipConfirmModal.SetActive(false);
        }

        // =====================================================================
        // INarrativeView IMPLEMENTATION
        // =====================================================================

        public bool Perform(StageEvent ev, Action done)
        {
            EnsureBuilt();
            if (ev == null || ev.Command == null)
            {
                done?.Invoke();
                return true;
            }

            var cmd = ev.Command;
            switch (cmd.Kind)
            {
                case NarrativeCmd.Backdrop:
                    UpdateBackdropVisual(cmd.Label);
                    if (ev.Blocking && cmd.Ms > 0 && gameObject.activeInHierarchy)
                        StartCoroutine(WaitUnscaledAndDone(Mathf.Clamp(cmd.Ms / 1000f, 0.04f, 0.25f), done));
                    else
                        done?.Invoke();
                    return true;

                case NarrativeCmd.Fade:
                    bool fadeOut = string.Equals(cmd.Label, "out", StringComparison.OrdinalIgnoreCase);
                    float dur = Mathf.Clamp(cmd.Ms / 1000f, 0.05f, 0.35f);
                    if (gameObject.activeInHierarchy)
                        StartCoroutine(AnimateFadeCoroutine(fadeOut ? 1f : 0f, dur, done));
                    else
                    {
                        SetFadeAlpha(fadeOut ? 1f : 0f);
                        done?.Invoke();
                    }
                    return true;

                case NarrativeCmd.Shake:
                    shakeTimer = Mathf.Clamp(cmd.Ms / 1000f, 0.12f, 0.6f);
                    done?.Invoke();
                    return true;

                case NarrativeCmd.Emote:
                    ShowEmoteBadge(cmd.Actor, cmd.Label);
                    done?.Invoke();
                    return true;

                case NarrativeCmd.Sfx:
                case NarrativeCmd.Music:
                    done?.Invoke();
                    return true;

                default:
                    return false;
            }
        }

        public void OnCustom(NarrativeEffect effect)
        {
            EnsureBuilt();
            if (effect == null || string.IsNullOrEmpty(effect.Id)) return;

            if (effect.Id.StartsWith("reflection:", StringComparison.OrdinalIgnoreCase))
            {
                string portraitId = effect.Id.Substring("reflection:".Length).Trim();
                ShowReflectionOverlay(portraitId);
            }
            else if (effect.Id.StartsWith("notebook:", StringComparison.OrdinalIgnoreCase))
            {
                string noteId = effect.Id.Substring("notebook:".Length).Trim();
                ToastOverlay.Show("Sổ tay có ghi chép mới."); // the raw entry id is not player text
            }
            else if (effect.Id.StartsWith("tutorial:", StringComparison.OrdinalIgnoreCase))
            {
                string verb = effect.Id.Substring("tutorial:".Length).Trim();
                ShowTutorialCallout(verb);
            }
        }

        // =====================================================================
        // UI HIERARCHY BUILDER (Dedicated ScreenSpaceOverlay Canvas)
        // =====================================================================

        private void EnsureBuilt()
        {
            if (panel != null) return;

            if (dedicatedCanvas == null)
            {
                dedicatedCanvas = gameObject.GetComponent<Canvas>();
                if (dedicatedCanvas == null) dedicatedCanvas = gameObject.AddComponent<Canvas>();
                dedicatedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                dedicatedCanvas.sortingOrder = 4200;

                var scaler = gameObject.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;

                if (gameObject.GetComponent<GraphicRaycaster>() == null)
                    gameObject.AddComponent<GraphicRaycaster>();
            }

            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            panel = new GameObject("VnStoryDialogueModal", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            panelBgImage = panel.GetComponent<Image>();
            panelBgImage.color = new Color(0.02f, 0.03f, 0.05f, 0.22f);

            // Clicking anywhere on the background advances lines (VN standard)
            var bgClick = panel.AddComponent<Button>();
            bgClick.transition = Selectable.Transition.None;
            bgClick.onClick.AddListener(OnUserTapAdvance);

            // 1. Full-screen Backdrop Layer (for scenes away from the playable maps)
            backdropLayer = new GameObject("BackdropLayer", typeof(RectTransform), typeof(Image));
            backdropLayer.transform.SetParent(panel.transform, false);
            var bgRt = (RectTransform)backdropLayer.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            backdropImage = backdropLayer.GetComponent<Image>();
            backdropImage.raycastTarget = false;
            backdropLayer.SetActive(false);

            var capCard = DayMarketPopup.MakeSlotCard(backdropLayer.transform, new Vector2(-470f, 322f), new Vector2(300f, 34f));
            backdropCaption = DayMarketPopup.Text(capCard.transform, Vector2.zero, new Vector2(280f, 28f), 13.5f, TextAlignmentOptions.Left);
            backdropCaption.color = new Color(0.96f, 0.90f, 0.72f);

            // 2. Left & Right 2.5D Bust Portrait Frames (raised to y=88f above vnBox top edge -66f, hidden until active speaker — #143 items 1, 4)
            leftFrame = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(-400f, 88f), new Vector2(236f, 288f));
            leftPortrait = DayMarketPopup.MakePortraitIcon(leftFrame.transform, Vector2.zero, new Vector2(220f, 272f));
            leftPortrait.preserveAspect = true;
            leftPortrait.raycastTarget = false;
            leftFrame.SetActive(false);

            rightFrame = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(400f, 88f), new Vector2(236f, 288f));
            rightPortrait = DayMarketPopup.MakePortraitIcon(rightFrame.transform, Vector2.zero, new Vector2(220f, 272f));
            rightPortrait.preserveAspect = true;
            rightPortrait.raycastTarget = false;
            rightFrame.SetActive(false);

            // Emote floating badge
            var emoteGo = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, 210f), new Vector2(180f, 36f));
            emoteBadgeText = DayMarketPopup.Text(emoteGo.transform, Vector2.zero, new Vector2(166f, 30f), 14f, TextAlignmentOptions.Center);
            emoteBadgeText.transform.parent.gameObject.SetActive(false);

            // 3. Bottom Visual Novel Dialogue Box
            vnBox = DayMarketPopup.BuildStyledPopupFrame(panel.transform, "VnBox", new Vector2(1080f, 248f),
                out var bodyParchment, out var headerBanner, out var footerBanner);
            vnBoxRect = (RectTransform)vnBox.transform;
            headerBannerGo = headerBanner != null ? headerBanner.gameObject : null;
            vnBoxRect.anchoredPosition = vnBoxBasePos;

            var bodyBtn = bodyParchment.gameObject.AddComponent<Button>();
            bodyBtn.transition = Selectable.Transition.None;
            bodyBtn.onClick.AddListener(OnUserTapAdvance);

            speakerTitle = DayMarketPopup.Text(headerBanner, new Vector2(-150f, 0f), new Vector2(680f, 36f), 19.5f, TextAlignmentOptions.Left);
            speakerTitle.color = new Color(1f, 0.94f, 0.68f);

            affectionBadge = DayMarketPopup.Text(headerBanner, new Vector2(300f, 0f), new Vector2(380f, 34f), 14f, TextAlignmentOptions.Right);
            affectionBadge.color = new Color(1f, 0.84f, 0.72f);

            dialogueText = DayMarketPopup.Text(bodyParchment, new Vector2(0f, 6f), new Vector2(1000f, 114f), 17.5f, TextAlignmentOptions.TopLeft);
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.raycastTarget = false;

            // Footer VN Controls Bar
            nextBtn = DayMarketPopup.Button(footerBanner, new Vector2(-405f, 0f), new Vector2(190f, 34f), out nextBtnLabel);
            nextBtnLabel.text = "Tiếp [Space]";
            nextBtnLabel.fontSize = 13.5f;
            nextBtn.onClick.AddListener(OnUserTapAdvance);

            autoBtn = DayMarketPopup.Button(footerBanner, new Vector2(-225f, 0f), new Vector2(146f, 34f), out autoBtnLabel);
            autoBtnLabel.text = "Tự chạy [A]: Tắt";
            autoBtnLabel.fontSize = 13f;
            autoBtn.onClick.AddListener(ToggleAutoMode);

            ffBtn = DayMarketPopup.Button(footerBanner, new Vector2(-68f, 0f), new Vector2(148f, 34f), out ffBtnLabel);
            ffBtnLabel.text = "Tua [Ctrl]";
            ffBtnLabel.fontSize = 13f;
            ffBtn.onClick.AddListener(ToggleSkipUnreadMode);

            backlogBtn = DayMarketPopup.Button(footerBanner, new Vector2(88f, 0f), new Vector2(144f, 34f), out backlogBtnLabel);
            backlogBtnLabel.text = "Nhật ký [L]";
            backlogBtnLabel.fontSize = 13f;
            backlogBtn.onClick.AddListener(ToggleBacklogModal);

            shopBtn = DayMarketPopup.Button(footerBanner, new Vector2(248f, 0f), new Vector2(156f, 34f), out shopBtnLabel);
            shopBtnLabel.text = "Cửa Hàng [M]";
            shopBtnLabel.fontSize = 13f;
            shopBtn.onClick.AddListener(InvokeOpenShop);
            shopBtn.gameObject.SetActive(false);

            skipBtn = DayMarketPopup.Button(footerBanner, new Vector2(416f, 0f), new Vector2(156f, 34f), out skipBtnLabel);
            skipBtnLabel.text = "Bỏ qua [Tab]";
            skipBtnLabel.fontSize = 13f;
            skipBtn.onClick.AddListener(RequestSkipScene);

            // 4. Choice Container (Centered above VN Box)
            choiceContainer = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, 64f), new Vector2(720f, 250f));
            var chBg = choiceContainer.GetComponent<Image>();
            if (chBg != null) chBg.color = new Color(0.08f, 0.07f, 0.06f, 0.95f);

            choicePromptText = DayMarketPopup.Text(choiceContainer.transform, new Vector2(0f, 94f), new Vector2(680f, 34f), 15.5f, TextAlignmentOptions.Center);
            choicePromptText.color = new Color(1f, 0.92f, 0.68f);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int slot = i;
                float y = 50f - i * 42f;
                choiceButtons[i] = DayMarketPopup.Button(choiceContainer.transform, new Vector2(0f, y), new Vector2(660f, 36f), out choiceLabels[i]);
                choiceLabels[i].fontSize = 14.5f;
                choiceButtons[i].onClick.AddListener(() => OnChoiceClicked(slot));
            }
            choiceContainer.SetActive(false);

            // 5. Chapter / Location Banner Card
            bannerCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, 24f), new Vector2(780f, 118f));
            var bImg = bannerCard.GetComponent<Image>();
            if (bImg != null) bImg.color = new Color(0.06f, 0.05f, 0.08f, 0.94f);
            bannerText = DayMarketPopup.Text(bannerCard.transform, Vector2.zero, new Vector2(720f, 88f), 26f, TextAlignmentOptions.Center);
            bannerText.color = new Color(1f, 0.93f, 0.72f);
            bannerCard.SetActive(false);

            // 6. Reflection / Custom Effect Overlay Card (Khoa's Mirror Reflection)
            reflectionCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, 92f), new Vector2(340f, 260f));
            var rfImg = reflectionCard.GetComponent<Image>();
            if (rfImg != null) rfImg.color = new Color(0.10f, 0.16f, 0.22f, 0.96f);
            reflectionPortraitImg = DayMarketPopup.MakePortraitIcon(reflectionCard.transform, new Vector2(0f, 18f), new Vector2(176f, 200f));
            reflectionPortraitImg.preserveAspect = true;
            reflectionCaptionText = DayMarketPopup.Text(reflectionCard.transform, new Vector2(0f, -104f), new Vector2(316f, 32f), 13f, TextAlignmentOptions.Center);
            reflectionCaptionText.color = new Color(0.72f, 0.92f, 1f);
            reflectionCard.SetActive(false);

            // 7. Backlog Modal
            BuildBacklogModal();

            // 8. Skip Confirmation Modal (shows Scene.Recap)
            BuildSkipConfirmModal();

            // 9. Full-screen Fade Overlay (topmost inside panel)
            var fadeGo = new GameObject("StageFadeOverlay", typeof(RectTransform), typeof(Image));
            fadeGo.transform.SetParent(panel.transform, false);
            var fRt = (RectTransform)fadeGo.transform;
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = fRt.offsetMax = Vector2.zero;
            fadeOverlay = fadeGo.GetComponent<Image>();
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.raycastTarget = false;

            panel.SetActive(false);
        }

        private void BuildBacklogModal()
        {
            backlogModal = DayMarketPopup.BuildStyledPopupFrame(panel.transform, "BacklogModal", new Vector2(920f, 540f),
                out var body, out var header, out var footer);
            var hTxt = DayMarketPopup.Text(header, Vector2.zero, new Vector2(820f, 34f), 19f, TextAlignmentOptions.Center);
            hTxt.text = "<b>NHẬT KÝ HỘI THOẠI (BACKLOG — TỐI ĐA 200 DÒNG)</b>";
            hTxt.color = new Color(1f, 0.94f, 0.68f);

            backlogContentText = DayMarketPopup.Text(body, new Vector2(0f, 0f), new Vector2(850f, 380f), 14.5f, TextAlignmentOptions.TopLeft);
            backlogContentText.textWrappingMode = TextWrappingModes.Normal;
            backlogContentText.overflowMode = TextOverflowModes.Ellipsis;

            var closeBacklogBtn = DayMarketPopup.Button(footer, Vector2.zero, new Vector2(220f, 36f), out var closeLbl);
            closeLbl.text = "Đóng Nhật Ký [L / ESC]";
            closeBacklogBtn.onClick.AddListener(() => backlogModal.SetActive(false));
            backlogModal.SetActive(false);
        }

        private void BuildSkipConfirmModal()
        {
            skipConfirmModal = DayMarketPopup.BuildStyledPopupFrame(panel.transform, "SkipConfirmModal", new Vector2(680f, 340f),
                out var body, out var header, out var footer);
            var hTxt = DayMarketPopup.Text(header, Vector2.zero, new Vector2(600f, 34f), 18.5f, TextAlignmentOptions.Center);
            hTxt.text = "<b>TÓM TẮT CẢNH & XÁC NHẬN BỎ QUA</b>";
            hTxt.color = new Color(1f, 0.92f, 0.66f);

            skipRecapText = DayMarketPopup.Text(body, Vector2.zero, new Vector2(610f, 180f), 15f, TextAlignmentOptions.Center);
            skipRecapText.textWrappingMode = TextWrappingModes.Normal;

            var confirmBtn = DayMarketPopup.Button(footer, new Vector2(-130f, 0f), new Vector2(220f, 36f), out var cLbl);
            cLbl.text = "Bỏ qua cảnh này [Enter]";
            confirmBtn.onClick.AddListener(ConfirmSkipNow);

            var cancelBtn = DayMarketPopup.Button(footer, new Vector2(130f, 0f), new Vector2(220f, 36f), out var xLbl);
            xLbl.text = "Tiếp tục đọc [ESC]";
            cancelBtn.onClick.AddListener(() => skipConfirmModal.SetActive(false));

            skipConfirmModal.SetActive(false);
        }

        // =====================================================================
        // UPDATE LOOP — Drives View from Engine.Narrative.Runner
        // =====================================================================

        private void Update()
        {
            if (NarrativeStageController.View != this)
                NarrativeStageController.View = this;

            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            var runner = engine?.Narrative?.Runner;

            if (runner != null && runner.InScene)
            {
                legacyModeActive = false;
                EnsureBuilt();
                if (!panel.activeSelf)
                {
                    panel.SetActive(true);
                    panel.transform.SetAsLastSibling();
                }

                // Clear any leftover shop callback if a non-talk scene started
                if (runner.Scene != null && runner.Scene.Id != lastSceneId)
                {
                    lastSceneId = runner.Scene.Id;
                    if (runner.Scene.Trigger != NarrativeTrigger.Talk)
                        onOpenShopCallback = null;
                    SyncShopButtonVisibility();
                }

                // Hold Ctrl for FastForward
                bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || ffHeldByPointer;
                runner.FastForward = ctrlHeld;

                RefreshFromRunner(engine, runner);
                HandleRunnerHotkeys(runner);
                TickVisualTimers();
                return;
            }

            if (legacyModeActive)
            {
                TickVisualTimers();
                if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) { OnUserTapAdvance(); return; }
                return;
            }

            // Runner is Idle and no legacy fallback is active -> hide modal cleanly
            if (panel != null && panel.activeSelf)
            {
                panel.SetActive(false);
                if (backlogModal != null) backlogModal.SetActive(false);
                if (skipConfirmModal != null) skipConfirmModal.SetActive(false);
            }
        }

        private void RefreshFromRunner(GameEngine engine, NarrativeRunner runner)
        {
            // 1. Backdrop Layer + P0* Flashback Memory Sepia Frame (#168/#171)
            bool isFlashbackMemory = runner.Scene != null && !string.IsNullOrEmpty(runner.Scene.Id) &&
                                     runner.Scene.Id.StartsWith("P0", StringComparison.OrdinalIgnoreCase);
            UpdateBackdropVisual(runner.BackdropId, runner.Scene?.Title);
            if (backdropImage != null && backdropImage.sprite != null)
            {
                backdropImage.color = isFlashbackMemory
                    ? new Color(1.0f, 0.92f, 0.78f, 1.0f)
                    : Color.white;
            }

            // 2. Controls Bar state
            if (autoBtnLabel != null)
                autoBtnLabel.text = runner.Auto ? "<color=#86EFAC>Tự chạy [A]: BẬT</color>" : "Tự chạy [A]: Tắt";
            if (ffBtnLabel != null)
                ffBtnLabel.text = runner.State.Settings.SkipUnread ? "<color=#FDE047>Tua [Ctrl]: Mọi dòng</color>" : "Tua [Ctrl]: Đã đọc";
            if (skipBtn != null)
                skipBtn.gameObject.SetActive(runner.Scene != null && runner.Scene.Skippable);

            // 3. Phase rendering
            switch (runner.Phase)
            {
                case RunnerPhase.Banner:
                    if (bannerCard != null)
                    {
                        bannerCard.SetActive(true);
                        bannerText.text = $"<b>{runner.BannerText}</b>";
                    }
                    if (vnBox != null) vnBox.SetActive(false);
                    if (choiceContainer != null) choiceContainer.SetActive(false);
                    if (leftFrame != null) leftFrame.SetActive(false);
                    if (rightFrame != null) rightFrame.SetActive(false);
                    break;

                case RunnerPhase.Line:
                    if (bannerCard != null) bannerCard.SetActive(false);
                    if (choiceContainer != null) choiceContainer.SetActive(false);
                    if (vnBox != null) vnBox.SetActive(true);
                    RenderLineView(engine, runner, runner.Line);
                    break;

                case RunnerPhase.Choice:
                    if (bannerCard != null) bannerCard.SetActive(false);
                    if (vnBox != null) vnBox.SetActive(true);
                    RenderChoiceView(runner);
                    break;

                case RunnerPhase.Stage:
                case RunnerPhase.Pause:
                    if (bannerCard != null) bannerCard.SetActive(false);
                    if (choiceContainer != null) choiceContainer.SetActive(false);
                    break;
            }
        }

        private void RenderLineView(GameEngine engine, NarrativeRunner runner, LineView line)
        {
            if (line == null) return;

            float scale = Mathf.Clamp(runner.State.Settings.TextScale, 0.8f, 1.4f);
            dialogueText.fontSize = 17.5f * scale;

            if (line.IsNarration)
            {
                // #143 items 1 & 5: Narration strip has NO "— Dẫn truyện —" header bar and hides unused portrait frames
                if (headerBannerGo != null) headerBannerGo.SetActive(false);
                speakerTitle.text = "";
                affectionBadge.text = "";
                if (leftFrame != null) leftFrame.SetActive(false);
                if (rightFrame != null) rightFrame.SetActive(false);
                var vnImg = vnBox != null ? vnBox.GetComponent<Image>() : null;
                if (vnImg != null) vnImg.color = new Color(0.06f, 0.07f, 0.11f, 0.92f);

                dialogueText.alignment = TextAlignmentOptions.Center;
                string vis = line.VisibleText;
                if (line.Complete && Mathf.Sin(Time.unscaledTime * 6.5f) > 0f)
                    vis += " <color=#FDE047>►</color>";
                dialogueText.text = $"<i><color=#f1ebd8>{vis}</color></i>";
                dialogueText.maxVisibleCharacters = 99999;
            }
            else
            {
                if (headerBannerGo != null) headerBannerGo.SetActive(true);
                var vnImg = vnBox != null ? vnBox.GetComponent<Image>() : null;
                if (vnImg != null) vnImg.color = Color.white;

                dialogueText.alignment = TextAlignmentOptions.TopLeft;
                var cast = NarrativeCast.Get(line.SpeakerId);
                string roleTag = cast != null && !string.IsNullOrEmpty(cast.Role)
                    ? $"  <size=14><color=#cbd5e1>({cast.Role})</color></size>"
                    : "";
                Color nameCol = new Color(1f, 0.94f, 0.68f);
                if (!string.IsNullOrEmpty(line.NameplateColor) && ColorUtility.TryParseHtmlString(line.NameplateColor, out var parsed))
                    nameCol = parsed;
                speakerTitle.color = nameCol;
                speakerTitle.text = $"<b>{line.SpeakerName}</b>{roleTag}";

                // Specialist affection / scene title badge via engine.RelationOf (ADR-0026)
                var rel = engine != null ? engine.RelationOf(line.SpeakerId) : default;
                if (rel.Hireable)
                {
                    affectionBadge.text = $"♥ <b>{rel.Label}</b> ({rel.Affinity}/100)";
                }
                else
                {
                    affectionBadge.text = runner.Scene != null && !string.IsNullOrEmpty(runner.Scene.Title)
                        ? $"<color=#d8c49a>{runner.Scene.Title}</color>"
                        : "";
                }

                // #143 items 1 & 3: Never show an empty frame or white untextured Image
                Sprite portSpr = UISpriteLoader.GetPortrait(line.SpeakerId, line.Portrait)
                              ?? UISpriteLoader.GetNeutralSilhouettePortrait();
                if (line.Side == StageSide.Left)
                {
                    if (leftFrame != null)
                    {
                        leftFrame.SetActive(portSpr != null);
                        leftPortrait.sprite = portSpr;
                        leftPortrait.color = Color.white;
                        leftFrame.transform.localScale = new Vector3(1.03f, 1.03f, 1f);
                    }
                    if (rightFrame != null)
                    {
                        bool keepRight = rightFrame.activeSelf && rightPortrait != null && rightPortrait.sprite != null;
                        rightFrame.SetActive(keepRight);
                        if (keepRight)
                        {
                            rightPortrait.color = new Color(0.55f, 0.55f, 0.62f, 0.85f);
                            rightFrame.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                        }
                    }
                }
                else
                {
                    if (rightFrame != null)
                    {
                        rightFrame.SetActive(portSpr != null);
                        rightPortrait.sprite = portSpr;
                        rightPortrait.color = Color.white;
                        rightFrame.transform.localScale = new Vector3(1.03f, 1.03f, 1f);
                    }
                    if (leftFrame != null)
                    {
                        bool keepLeft = leftFrame.activeSelf && leftPortrait != null && leftPortrait.sprite != null;
                        leftFrame.SetActive(keepLeft);
                        if (keepLeft)
                        {
                            leftPortrait.color = new Color(0.55f, 0.55f, 0.62f, 0.85f);
                            leftFrame.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                        }
                    }
                }

                string vis = line.VisibleText;
                if (line.Complete && Mathf.Sin(Time.unscaledTime * 6.5f) > 0f)
                    vis += " <color=#FDE047>►</color>";
                dialogueText.text = vis;
                dialogueText.maxVisibleCharacters = 99999;
            }

            if (nextBtnLabel != null)
                nextBtnLabel.text = line.Complete ? "Tiếp tục [Space] ►" : "Hiện hết [Space]";
        }

        private void RenderChoiceView(NarrativeRunner runner)
        {
            if (choiceContainer == null) return;
            choiceContainer.SetActive(true);
            choicePromptText.text = string.IsNullOrEmpty(runner.ChoicePrompt)
                ? "<b>Chọn hướng phản hồi:</b>"
                : $"<b>{runner.ChoicePrompt}</b>";

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < runner.Choices.Count)
                {
                    var ch = runner.Choices[i];
                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtons[i].interactable = ch.Enabled;
                    choiceLabels[i].text = ch.Enabled
                        ? $"<b>[{i + 1}]</b>  {ch.Text}"
                        : $"<color=#94a3b8>[{i + 1}]  {ch.Text}</color>  <color=#F87171><i>({ch.LockedReason})</i></color>";
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void HandleRunnerHotkeys(NarrativeRunner runner)
        {
            if (skipConfirmModal != null && skipConfirmModal.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) { ConfirmSkipNow(); return; }
                if (Input.GetKeyDown(KeyCode.Escape)) { skipConfirmModal.SetActive(false); return; }
                return;
            }

            if (backlogModal != null && backlogModal.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.L)) { backlogModal.SetActive(false); return; }
                return;
            }

            if (Input.GetKeyDown(KeyCode.L)) { ToggleBacklogModal(); return; }
            if (Input.GetKeyDown(KeyCode.A)) { ToggleAutoMode(); return; }
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape)) { RequestSkipScene(); return; }
            if (Input.GetKeyDown(KeyCode.M) && onOpenShopCallback != null) { InvokeOpenShop(); return; }

            if (runner.Phase == RunnerPhase.Choice)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) OnChoiceClicked(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) OnChoiceClicked(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) OnChoiceClicked(2);
                else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) OnChoiceClicked(3);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                OnUserTapAdvance();
            }
        }

        private void OnUserTapAdvance()
        {
            if (backlogModal != null && backlogModal.activeSelf) return;
            if (skipConfirmModal != null && skipConfirmModal.activeSelf) return;

            var runner = MobileGameController.Instance?.Engine?.Narrative?.Runner;
            if (runner != null && runner.InScene)
            {
                runner.Tap();
                return;
            }

            if (legacyModeActive && legacyBeats.Count > 0)
            {
                legacyBeatIndex++;
                if (legacyBeatIndex >= legacyBeats.Count)
                {
                    Close();
                    return;
                }
                ShowLegacyBeat();
            }
        }

        private void OnChoiceClicked(int slotIndex)
        {
            var runner = MobileGameController.Instance?.Engine?.Narrative?.Runner;
            if (runner == null || runner.Phase != RunnerPhase.Choice) return;
            if (slotIndex < 0 || slotIndex >= runner.Choices.Count) return;
            var ch = runner.Choices[slotIndex];
            if (!ch.Enabled) return;
            runner.Choose(ch.Index);
        }

        private void ToggleAutoMode()
        {
            var runner = MobileGameController.Instance?.Engine?.Narrative?.Runner;
            if (runner == null) return;
            runner.Auto = !runner.Auto;
        }

        private void ToggleSkipUnreadMode()
        {
            var runner = MobileGameController.Instance?.Engine?.Narrative?.Runner;
            if (runner == null) return;
            runner.State.Settings.SkipUnread = !runner.State.Settings.SkipUnread;
        }

        private void ToggleBacklogModal()
        {
            if (backlogModal == null) return;
            bool nextState = !backlogModal.activeSelf;
            backlogModal.SetActive(nextState);
            if (!nextState) return;

            var runner = MobileGameController.Instance?.Engine?.Narrative?.Runner;
            if (runner == null || runner.Backlog.Count == 0)
            {
                backlogContentText.text = "<i>(Chưa có dòng thoại nào trong nhật ký.)</i>";
                return;
            }

            var sb = new StringBuilder();
            int start = Mathf.Max(0, runner.Backlog.Count - 16);
            for (int i = start; i < runner.Backlog.Count; i++)
            {
                var entry = runner.Backlog[i];
                if (entry.IsRecap)
                    sb.AppendLine($"<color=#FDE047><b>[Tóm tắt]:</b> {entry.Text}</color>");
                else if (entry.IsChoice)
                    sb.AppendLine($"<color=#86EFAC>  ➤ Đã chọn: {entry.Text}</color>");
                else if (string.IsNullOrEmpty(entry.Speaker))
                    sb.AppendLine($"<i><color=#cbd5e1>{entry.Text}</color></i>");
                else
                    sb.AppendLine($"<b><color=#F2B8A0>{entry.Speaker}:</color></b> {entry.Text}");
            }
            backlogContentText.text = sb.ToString();
        }

        private void RequestSkipScene()
        {
            var runner = MobileGameController.Instance?.Engine?.Narrative?.Runner;
            if (runner == null || !runner.InScene || runner.Scene == null)
            {
                Close();
                return;
            }
            if (!runner.Scene.Skippable) return;

            if (!string.IsNullOrWhiteSpace(runner.Scene.Recap) && skipConfirmModal != null)
            {
                skipRecapText.text = $"<b>{runner.Scene.Title}</b>\n\n<i>“{runner.Scene.Recap}”</i>\n\n<size=13><color=#cbd5e1>(Mọi cờ cốt truyện, vật phẩm và lựa chọn mặc định vẫn được áp dụng đầy đủ.)</color></size>";
                skipConfirmModal.SetActive(true);
                return;
            }

            ConfirmSkipNow();
        }

        private void ConfirmSkipNow()
        {
            if (skipConfirmModal != null) skipConfirmModal.SetActive(false);
            var runner = MobileGameController.Instance?.Engine?.Narrative?.Runner;
            if (runner != null && runner.InScene)
                runner.Skip();
        }

        private void SyncShopButtonVisibility()
        {
            if (shopBtn != null)
                shopBtn.gameObject.SetActive(onOpenShopCallback != null);
        }

        private void InvokeOpenShop()
        {
            var cb = onOpenShopCallback;
            onOpenShopCallback = null;
            var runner = MobileGameController.Instance?.Engine?.Narrative?.Runner;
            if (runner != null && runner.InScene && runner.Scene != null && runner.Scene.Skippable)
                runner.Skip();
            legacyModeActive = false;
            if (panel != null) panel.SetActive(false);
            cb?.Invoke();
        }

        // =====================================================================
        // STAGE & CUSTOM EFFECTS HELPERS
        // =====================================================================

        private void UpdateBackdropVisual(string backdropId, string sceneTitle = null)
        {
            if (backdropLayer == null || backdropImage == null) return;
            if (string.IsNullOrWhiteSpace(backdropId))
            {
                backdropLayer.SetActive(false);
                if (panelBgImage != null) panelBgImage.color = new Color(0.02f, 0.03f, 0.05f, 0.18f);
                return;
            }

            Sprite spr = UISpriteLoader.GetBackdrop(backdropId);
            if (spr != null)
            {
                backdropImage.sprite = spr;
                backdropImage.color = Color.white;
            }
            else
            {
                backdropImage.sprite = null;
                backdropImage.color = new Color(0.06f, 0.07f, 0.10f, 1f);
            }
            backdropLayer.SetActive(true);
            if (panelBgImage != null) panelBgImage.color = new Color(0.02f, 0.03f, 0.05f, 0.35f);
            if (backdropCaption != null)
            {
                string label = !string.IsNullOrEmpty(sceneTitle) ? sceneTitle : backdropId;
                backdropCaption.text = $"<b>{label}</b>";
            }
        }

        private IEnumerator WaitUnscaledAndDone(float seconds, Action done)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            done?.Invoke();
        }

        private IEnumerator AnimateFadeCoroutine(float targetAlpha, float duration, Action done)
        {
            if (fadeOverlay == null) { done?.Invoke(); yield break; }
            float startAlpha = fadeOverlay.color.a;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, u));
                yield return null;
            }
            SetFadeAlpha(targetAlpha);
            done?.Invoke();
        }

        private void SetFadeAlpha(float a)
        {
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a));
        }

        private void ShowEmoteBadge(string actorId, string emoteLabel)
        {
            if (emoteBadgeText == null) return;
            emoteBadgeText.transform.parent.gameObject.SetActive(true);
            emoteBadgeText.text = $"<b>{actorId}: {emoteLabel}</b>";
            emoteTimer = 1.6f;
        }

        private void ShowReflectionOverlay(string portraitId)
        {
            if (reflectionCard == null) return;
            reflectionCard.SetActive(true);
            // the reflection is the active protagonist's (An's mirror scenes used to show Khoa)
            var who = MobileGameController.Instance?.Engine?.Character?.Identity == PlayerId.An ? "an" : "khoa";
            reflectionPortraitImg.sprite = UISpriteLoader.GetPortrait(who, portraitId) ?? UISpriteLoader.GetNeutralSilhouettePortrait();
            string desc = portraitId switch
            {
                Portraits.KhoaReflectFrown => "Bóng phản chiếu của Khoa khẽ nhíu mày",
                Portraits.KhoaReflectSmile => "Bóng phản chiếu trong gương mỉm cười thật khẽ",
                Portraits.KhoaReflectCry => "Bóng phản chiếu rơi một giọt nước mắt lặng lẽ",
                _ => $"Phản chiếu: {portraitId}"
            };
            reflectionCaptionText.text = $"<i>“{desc}”</i>";
            reflectionTimer = 2.8f;
        }

        private void ShowTutorialCallout(string verb)
        {
            // Every verb the scenes use (.Hint(...)) has a player sentence; the raw verb never reaches the screen
            // (3 Nov phone playtest showed "Hướng dẫn: interact"). Touch players get button names, PC players keys.
            bool touch = Application.isMobilePlatform || Input.touchSupported;
            string msg = verb switch
            {
                "move" => touch ? "Kéo cần điều khiển bên trái để đi." : "Đi bằng WASD hoặc phím mũi tên, giữ Shift để chạy.",
                "interact" => touch ? "Đứng cạnh đồ vật hoặc người rồi chạm nút lớn góc phải để tương tác." : "Đứng cạnh đồ vật hoặc người rồi bấm E để tương tác.",
                "feed" => touch ? "Lấy cám ở kho cám, đứng cạnh máng rồi chạm nút lớn để đổ cám." : "Lấy cám ở kho cám (E), đứng cạnh máng rồi bấm E để đổ cám.",
                "water" => touch ? "Đứng cạnh bồn nước rồi chạm nút lớn để thay nước." : "Đứng cạnh bồn nước rồi bấm E để thay nước.",
                "whistle" or "recall" => touch ? "Chạm nút Huýt sáo để lùa đàn về Nơi Trú." : "Bấm G để thổi tù và lùa đàn về Nơi Trú.",
                "observe" => touch ? "Chạm vào một con heo để xem thẻ của nó." : "Bấm M cạnh một con heo để quan sát nó.",
                "market" => "Đi qua cổng phía Bắc để sang thị trấn chợ (mở 06:00–18:00).",
                "dodge" => touch ? "Đòn nháy cam: chạm Lướt né để tránh." : "Đòn nháy cam: bấm Ctrl để lướt né.",
                "counter" => touch ? "Đòn nháy tím: chạm Phản kích đúng lúc đòn sắp chạm." : "Đòn nháy tím: bấm Q đúng lúc đòn sắp chạm.",
                "build" => touch ? "Chạm biểu tượng búa trên cùng để mở bảng xây dựng." : "Bấm P để mở bảng xây dựng.",
                "defend" => touch ? "Chạm nút đánh lớn để đánh, giữ đàn heo an toàn." : "Bấm Space hoặc chuột trái để đánh, giữ đàn heo an toàn.",
                _ => null
            };
            if (msg != null) ToastOverlay.Show(msg);
        }

        private void TickVisualTimers()
        {
            if (emoteTimer > 0f)
            {
                emoteTimer -= Time.unscaledDeltaTime;
                if (emoteTimer <= 0f && emoteBadgeText != null)
                    emoteBadgeText.transform.parent.gameObject.SetActive(false);
            }

            if (reflectionTimer > 0f)
            {
                reflectionTimer -= Time.unscaledDeltaTime;
                if (reflectionTimer <= 0f && reflectionCard != null)
                    reflectionCard.SetActive(false);
            }

            if (shakeTimer > 0f && vnBoxRect != null)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                if (shakeTimer > 0f)
                {
                    float ox = UnityEngine.Random.Range(-shakeMagnitude, shakeMagnitude);
                    float oy = UnityEngine.Random.Range(-shakeMagnitude, shakeMagnitude);
                    vnBoxRect.anchoredPosition = vnBoxBasePos + new Vector2(ox, oy);
                }
                else
                {
                    vnBoxRect.anchoredPosition = vnBoxBasePos;
                }
            }
        }

        // =====================================================================
        // LEGACY FALLBACK (only when an NPC has no registered Talk scene)
        // =====================================================================

        private void StartLegacyFallback(string npcId, string displayName, string initialLine, Action onOpenShop)
        {
            legacyModeActive = true;
            legacyBeats.Clear();
            legacyBeatIndex = 0;

            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            bool isKhoa = engine?.Character != null && engine.Character.Identity == PlayerId.Khoa;
            string heroName = isKhoa ? "Khoa" : "An";

            legacyBeats.Add(new DialogueBeat
            {
                SpeakerIsHero = false,
                SpeakerName = string.IsNullOrWhiteSpace(displayName) ? npcId : displayName,
                HeroEmotion = Portraits.Neutral,
                NpcEmotion = Portraits.Neutral,
                Text = !string.IsNullOrWhiteSpace(initialLine)
                    ? initialLine
                    : $"Chào {heroName}, công việc ở trại hôm nay ổn cả chứ?"
            });

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            SyncShopButtonVisibility();
            ShowLegacyBeat();
        }

        private void ShowLegacyBeat()
        {
            if (legacyBeats.Count == 0) return;
            var beat = legacyBeats[Mathf.Clamp(legacyBeatIndex, 0, legacyBeats.Count - 1)];
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            bool isKhoa = engine?.Character != null && engine.Character.Identity == PlayerId.Khoa;
            string heroKey = isKhoa ? "khoa" : "an";

            if (backdropLayer != null) backdropLayer.SetActive(false);
            if (bannerCard != null) bannerCard.SetActive(false);
            if (choiceContainer != null) choiceContainer.SetActive(false);
            if (vnBox != null) vnBox.SetActive(true);

            leftFrame.SetActive(true);
            rightFrame.SetActive(true);
            leftPortrait.sprite = UISpriteLoader.GetPortrait(heroKey, beat.HeroEmotion) ?? UISpriteLoader.GetNeutralSilhouettePortrait();
            rightPortrait.sprite = UISpriteLoader.GetPortrait(currentNpcId, beat.NpcEmotion) ?? UISpriteLoader.GetNeutralSilhouettePortrait();
            leftPortrait.color = beat.SpeakerIsHero ? Color.white : new Color(0.55f, 0.55f, 0.62f, 0.85f);
            rightPortrait.color = !beat.SpeakerIsHero ? Color.white : new Color(0.55f, 0.55f, 0.62f, 0.85f);

            speakerTitle.text = $"<b>{beat.SpeakerName}</b>";
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.text = beat.Text + " <color=#FDE047>►</color>";
            dialogueText.maxVisibleCharacters = 99999;
        }
    }
}
