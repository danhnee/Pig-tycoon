using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Icon-first gameplay HUD (docs: Docs/art-direction/2d-ui-style-guide.md). Built entirely at runtime on its own
    /// Screen Space Overlay canvas from Assets/Art/PixelMyth/UI. No explanatory text; the only text is the gold number.
    /// Reads real engine state; anything the engine does not expose is left in its disabled/empty state.
    /// The legacy HUD is only hidden (never destroyed), so its logic keeps running.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1200)]
    public sealed class MythHudController : MonoBehaviour
    {
        public static MythHudController Instance { get; private set; }
        /// <summary>True while the Myth HUD is alive; legacy controllers hide their old buttons when set.</summary>
        public static bool LegacyActionButtonsHidden => Instance != null;

        const float RefW = 768f, RefH = 432f; // Responsive scale for both Mobile & Windows PC
        static readonly Color Bright = new Color(0.957f, 0.824f, 0.478f);
        static readonly Color Cinnabar = new Color(0.66f, 0.22f, 0.16f);
        static readonly string[] LegacyNames =
        {
            "Stardew_Clock_Panel", "Stardew_Energy_Bar", "Stardew_Guard_Bar", "Stardew_Hotbar", "Context_Action_Button",
            "Mobile_Joystick_Background", "Joystick_Background", "Btn_FarmOverview_B", "ContextActions",
            "DevMode_Button" // dev tools are not part of the play flow (ADR-0011 #7); F1/~ still opens them
        };

        // ---------------------------------------------------------------- install
        public static MythHudController Install()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("MythHud");
            return go.AddComponent<MythHudController>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInstall()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryAutoInstall();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { TryAutoInstall(); }

        static void TryAutoInstall()
        {
            if (Instance == null && FindFirstObjectByType<PixelWorldScene>() != null) Install();
        }

        // ---------------------------------------------------------------- state
        Canvas canvas;
        CanvasScaler scaler;
        CanvasGroup group;
        RectTransform safeRoot;
        int lastW, lastH;
        Rect lastSafe;

        // sundial
        Image needle, weatherIcon;
        readonly List<Image> dayPips = new List<Image>();
        readonly List<Image> skulls = new List<Image>();
        RectTransform pipRow, skullRow;
        Sprite pipOn, pipOff, skullSprite;
        WeatherType? shownWeather;
        int shownNeedleStep = -1;

        // bars
        MythClipFill hpFill, staminaBright, staminaLoad;
        Image staminaRim, rageBack;
        MythClipFill rageFill;
        Image rageFlame;
        RectTransform rageRoot;
        Sprite[] flameFrames = new Sprite[4];

        // status row
        RectTransform statusRow;
        sealed class StatusSlot { public Image Icon; public Image[] Dots; }
        readonly Dictionary<string, StatusSlot> statusSlots = new Dictionary<string, StatusSlot>();

        // top-right
        TextMeshProUGUI goldText;
        TextMeshProUGUI habitatText;   // GDD 6.1 Chỉ số Sinh Cảnh ("mức độ môi trường")
        Image herdLit, herdFace, capacityTick;
        Sprite[] faces = new Sprite[4];
        TextMeshProUGUI farmSubLineText, bagCountText;
        RectTransform faintOverlayRoot;
        RectTransform drinkBarRoot;
        MythClipFill drinkBarFill;
        float drinkStartTime = -999f;
        bool wasDrinking;
        readonly List<GameObject> tempShadeObjects = new List<GameObject>();
        RectTransform crisisAlertRoot;
        TextMeshProUGUI crisisAlertText;
        string crisisTargetPigId;
        Image[] bagSockets = new Image[8];
        Image[] bagItems = new Image[8];
        Image[] bagRims = new Image[8];
        RectTransform bagRoot;
        int bagLayoutBuilt = -1;
        string bagSignature = string.Empty;

        // controls
        MobileJoystick joystick;
        MythMedallion warehouseLink;
        MythMedallion contextButton, attackButton, defendButton, burstButton, whistleButton;
        TextMeshProUGUI congLabel, thuLabel, bienLabel, songTheLabel;
        MythMedallion songTheButton;
        MythMedallion dodgeButton, counterButton, observeButton;
        Component contextTarget;
        CustomBuildingController nearBuilding;
        PixelMythNpc nearNpc;
        PixelFenceSegment nearBrokenFence;
        MysticMerchantView nearMysticMerchant;
        CustomBuildingController[] buildings = new CustomBuildingController[0];
        PixelFenceSegment[] fences = new PixelFenceSegment[0];
        MysticMerchantView mysticMerchant;
        float nextBuildingRefresh, nextCoarse, nextLegacySweep;
        bool bound;

        GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        // ---------------------------------------------------------------- lifecycle
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildCanvas();
            BuildSundial();
            BuildBars();
            BuildStatusRow();
            BuildTopRight();
            BuildJoystick();
            BuildActions();
            RadialToolWheel.Create(gameObject, safeRoot, joystick); // Tab / corner medallion, hidden by default
            BuildingInteriorPopup.Ensure();
            MinimapHudWidget.Install(safeRoot);
            ApplyScaleAndSafeArea(true);
            HideLegacy();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            ApplyScaleAndSafeArea(false);
            // the tool wheel opens over a visible HUD (Stardew toolbar), it never blanks the joystick and buttons
            bool modalOpen = UISpriteLoader.IsAnyModalOpenIgnoringToolWheel();
            group.blocksRaycasts = !modalOpen;
            group.alpha = modalOpen ? 0f : 1f;

            if (Time.unscaledTime >= nextLegacySweep) { nextLegacySweep = Time.unscaledTime + 0.25f; HideLegacy(); BindPlayerJoystick(); }

            var engine = Engine;
            if (engine == null) return;

            UpdateSundial(engine);
            UpdateBars(engine);
            UpdateDrinkBar();
            UpdateBossBar();
            UpdateTradeEyeBar(engine);
            if (Time.unscaledTime >= nextCoarse)
            {
                nextCoarse = Time.unscaledTime + 0.2f;
                UpdateStatus(engine);
                UpdateTopRight(engine);
                UpdateBag(engine);
                TrySpawnStarterPickup(engine);
                SyncTempShadesInWorld(engine);
                UpdatePigCrisisIndicators(engine);
                ScanInteraction();
            }
            UpdateActions(engine);
        }

        // ---------------------------------------------------------------- canvas, scale, safe area
        void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.referencePixelsPerUnit = 32f; // 1 texel of 32-PPU art == 1 canvas unit, so tiled fills stay 1:1
            // Stay under popups that live on the legacy HUD canvas.
            var legacy = FindFirstObjectByType<StardewHUDController>();
            var legacyCanvas = legacy != null ? legacy.GetComponent<Canvas>() : null;
            canvas.sortingOrder = legacyCanvas != null ? legacyCanvas.sortingOrder - 1 : 0;
            scaler = gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
            group = gameObject.AddComponent<CanvasGroup>();
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            safeRoot = MythUi.Rect("SafeArea", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            safeRoot.anchorMin = Vector2.zero; safeRoot.anchorMax = Vector2.one;
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        void ApplyScaleAndSafeArea(bool force)
        {
            if (!force && lastW == Screen.width && lastH == Screen.height && lastSafe == Screen.safeArea) return;
            lastW = Screen.width; lastH = Screen.height; lastSafe = Screen.safeArea;
            canvas.pixelPerfect = false;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefW, RefH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Rect sa = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        // ---------------------------------------------------------------- legacy hide (logic untouched)
        void HideLegacy()
        {
            var hud = StardewHUDController.Instance != null ? StardewHUDController.Instance : FindFirstObjectByType<StardewHUDController>();
            if (hud != null)
            {
                foreach (Transform child in hud.transform)
                {
                    for (int i = 0; i < LegacyNames.Length; i++)
                        if (child.name == LegacyNames[i] && child.gameObject.activeSelf) { child.gameObject.SetActive(false); break; }
                }
            }
            foreach (var j in FindObjectsByType<MobileJoystick>(FindObjectsSortMode.None))
                if (j != joystick && j.GetComponent<Canvas>() == null && j.gameObject.activeSelf) j.gameObject.SetActive(false);
            var hotbar = HotbarController.Instance;
            if (hotbar != null && hotbar.SlotsContainer != null && hotbar.SlotsContainer.gameObject.activeSelf)
                hotbar.SlotsContainer.gameObject.SetActive(false);
        }

        void BindPlayerJoystick()
        {
            var p = PlayerMobileController.Instance;
            if (p != null && joystick != null && p.Joystick != joystick) p.Joystick = joystick;
        }

        // ---------------------------------------------------------------- sundial (top-left)
        TextMeshProUGUI sunClockBadgeText;
        void BuildSundial()
        {
            var dial = MythUi.Rect("Sundial", safeRoot, MythUi.TL, MythUi.TL, new Vector2(4, -4), new Vector2(64, 64));
            MythUi.Img("Base", dial, MythUi.Kit("sundial_base"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(64, 64), true);
            weatherIcon = MythUi.Img("Weather", dial, null, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(24, 24));
            needle = MythUi.Img("Needle", dial, MythUi.Kit("sundial_needle"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(64, 64));
            dial.gameObject.AddComponent<MythTap>().OnTap = MinimapHudWidget.ToggleWeatherDaypartPanel;

            // Compact clickable clock + 4-daypart badge next to the Sundial
            var clockPill = MythUi.Rect("SundialClockPill", safeRoot, MythUi.TL, MythUi.TL, new Vector2(72, -20), new Vector2(82, 16));
            var clockBg = clockPill.gameObject.AddComponent<Image>();
            clockBg.sprite = UISpriteLoader.GetFrameSlotNormal();
            clockBg.type = Image.Type.Sliced;
            clockBg.color = new Color(0.12f, 0.10f, 0.16f, 0.92f);
            clockBg.raycastTarget = true;
            clockPill.gameObject.AddComponent<MythTap>().OnTap = MinimapHudWidget.ToggleWeatherDaypartPanel;
            var clockTxtRt = MythUi.Rect("Txt", clockPill, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(78, 15));
            sunClockBadgeText = clockTxtRt.gameObject.AddComponent<TextMeshProUGUI>();
            sunClockBadgeText.fontSize = 9.5f;
            sunClockBadgeText.fontStyle = FontStyles.Bold;
            sunClockBadgeText.alignment = TextAlignmentOptions.Center;
            sunClockBadgeText.color = Bright;
            sunClockBadgeText.outlineWidth = 0.26f;
            sunClockBadgeText.outlineColor = new Color32(16, 10, 8, 255);
            sunClockBadgeText.textWrappingMode = TextWrappingModes.NoWrap;
            sunClockBadgeText.raycastTarget = false;
            sunClockBadgeText.text = "06:00 · Sáng";

            pipOn = MythUi.Kit("sundial_day_pip_on");
            pipOff = MythUi.Kit("sundial_day_pip_off");
            skullSprite = MythUi.Kit("raid_skull_pip");
            pipRow = MythUi.Rect("DayPips", safeRoot, MythUi.TL, MythUi.TL, new Vector2(4, -72), new Vector2(120, 6));
            skullRow = MythUi.Rect("RaidSkulls", safeRoot, MythUi.TL, MythUi.TL, new Vector2(72, -8), new Vector2(120, 8));
        }

        static string WeatherStem(WeatherType w)
        {
            switch (w)
            {
                case WeatherType.QuangDang: return "quang_dang";
                case WeatherType.NhieuMay: return "nhieu_may";
                case WeatherType.NangGat: return "nang_gat";
                case WeatherType.Mua: return "mua";
                case WeatherType.GiongBao: return "giong_bao";
                case WeatherType.SuongMu: return "suong_mu";
                case WeatherType.RetDam: return "ret_dam";
                case WeatherType.NomAm: return "nom_am";
                case WeatherType.GioKho: return "gio_kho";
                default: return "mua_buc_xa";
            }
        }

        void UpdateSundial(GameEngine engine)
        {
            var clock = engine.Clock;
            // Art: top = noon, needle step k (0..23) rotates clockwise 15 deg * k, hour = (12 + k) % 24.
            int step = (clock.CurrentHour + 12) % 24;
            if (step != shownNeedleStep)
            {
                shownNeedleStep = step;
                needle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -15f * step);
            }
            if (sunClockBadgeText != null)
            {
                string dpStr = MinimapHudWidget.GetDaypartName(clock.GetTimeOfDay());
                string clkStr = $"{clock.CurrentHour:00}:{clock.CurrentMinute:00} · {dpStr}";
                if (sunClockBadgeText.text != clkStr) sunClockBadgeText.text = clkStr;
            }
            if (shownWeather != engine.CurrentWeather)
            {
                shownWeather = engine.CurrentWeather;
                weatherIcon.sprite = MythUi.Icon("weather", WeatherStem(engine.CurrentWeather), 24);
                weatherIcon.enabled = weatherIcon.sprite != null;
            }

            // Day pips: one per day of the current cycle; lit up to today. The last one is the raid day.
            int total = Mathf.Clamp(clock.CycleTotalDays, 1, 14);
            SyncPool(dayPips, pipRow, total, pipOn, 8f, 6f);
            for (int i = 0; i < dayPips.Count; i++)
            {
                dayPips[i].sprite = i < clock.CycleDayIndex ? pipOn : pipOff;
                dayPips[i].enabled = dayPips[i].sprite != null;
            }

            // Raid skulls = whole days left before the big raid; on raid day a single skull blinks cinnabar.
            // C-15: only what the player knows (default 1–2 day warning, or the Intel bought this cycle); unknown = one dim skull
            var forecast = WaveForecast.Major(engine);
            int left = forecast.Known ? Mathf.Clamp(forecast.MinDays, 0, 8) : 8;
            int skullCount = !forecast.Known ? 1 : (left > 0 ? left : 1);
            SyncPool(skulls, skullRow, skullCount, skullSprite, 9f, 8f);
            bool warn = left <= 2;
            bool blinkOn = ((int)(Time.unscaledTime * 2f) & 1) == 0;
            for (int i = 0; i < skulls.Count; i++)
            {
                skulls[i].color = !forecast.Known ? new Color(1f, 1f, 1f, 0.35f)
                                 : left == 0 ? (blinkOn ? new Color(1f, 0.35f, 0.3f) : new Color(0.5f, 0.15f, 0.12f))
                                 : !forecast.Exact ? new Color(1f, 1f, 1f, 0.6f)
                                 : (warn && !blinkOn ? new Color(0.65f, 0.6f, 0.55f) : Color.white);
            }
        }

        static void SyncPool(List<Image> pool, RectTransform row, int count, Sprite sprite, float stride, float size)
        {
            while (pool.Count < count)
            {
                var img = MythUi.Img("P" + pool.Count, row, sprite, MythUi.TL, MythUi.TL, new Vector2(pool.Count * stride, 0), new Vector2(size, size));
                pool.Add(img);
            }
            for (int i = 0; i < pool.Count; i++) pool[i].gameObject.SetActive(i < count);
        }

        // ---------------------------------------------------------------- HP, stamina, rage
        void BuildBars()
        {
            // HeroPortrait25D medallion removed (Argon, 3 Nov): duplicate of the hero level badge, which already opens CharacterPanel [C].

            // HP vessel: trough -> clipped fill (window x12 w62) -> frame.
            var hp = MythUi.Rect("HpVessel", safeRoot, MythUi.TL, MythUi.TL, new Vector2(4, -82), new Vector2(80, 12));
            MythUi.Img("Trough", hp, MythUi.Kit("bar_hp_trough"), MythUi.TL, MythUi.TL, Vector2.zero);
            hpFill = new MythClipFill("Clip", hp, MythUi.Kit("bar_hp_fill"), MythUi.TL, MythUi.TL, new Vector2(12, -2), 62, 8);
            MythUi.Img("Frame", hp, MythUi.Kit("bar_hp_vessel_frame"), MythUi.TL, MythUi.TL, Vector2.zero);
            var hpTxtRt = MythUi.Rect("GuardNum", hp, MythUi.MID, MythUi.MID, new Vector2(5, 0), new Vector2(62, 12));
            guardValueText = hpTxtRt.gameObject.AddComponent<TextMeshProUGUI>();
            guardValueText.fontSize = 9.5f;
            guardValueText.fontStyle = FontStyles.Bold;
            guardValueText.alignment = TextAlignmentOptions.Center;
            guardValueText.color = Color.white;
            guardValueText.outlineWidth = 0.28f;
            guardValueText.outlineColor = new Color32(16, 10, 8, 255);
            guardValueText.raycastTarget = false;

            // Stamina: dark daily-load layer (from the right) under the bright recoverable layer (from the left).
            var sta = MythUi.Rect("Stamina", safeRoot, MythUi.TL, MythUi.TL, new Vector2(4, -96), new Vector2(96, 12));
            MythUi.Img("Trough", sta, MythUi.Kit("bar_stamina_trough"), MythUi.TL, MythUi.TL, Vector2.zero);
            staminaLoad = new MythClipFill("LoadClip", sta, MythUi.Kit("bar_stamina_fill_load"), MythUi.TL, MythUi.TR, new Vector2(88, -2), 80, 8, false, true);
            staminaBright = new MythClipFill("BrightClip", sta, MythUi.Kit("bar_stamina_fill_bright"), MythUi.TL, MythUi.TL, new Vector2(8, -2), 80, 8);
            // Pivot fix: the load clip is right-anchored visually, so size it from its right edge.
            staminaLoad.Root.pivot = new Vector2(1f, 1f);
            staminaLoad.Root.anchoredPosition = new Vector2(88, -2);
            MythUi.Img("Frame", sta, MythUi.Kit("bar_stamina_frame"), MythUi.TL, MythUi.TL, Vector2.zero);
            staminaRim = MythUi.Img("DangerRim", sta, MythUi.Kit("bar_stamina_frame"), MythUi.TL, MythUi.TL, Vector2.zero);
            staminaRim.color = new Color(1f, 0.15f, 0.1f, 0f);
            var staTxtRt = MythUi.Rect("StaminaNum", sta, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(80, 12));
            staminaValueText = staTxtRt.gameObject.AddComponent<TextMeshProUGUI>();
            staminaValueText.fontSize = 9.5f;
            staminaValueText.fontStyle = FontStyles.Bold;
            staminaValueText.alignment = TextAlignmentOptions.Center;
            staminaValueText.color = Color.white;
            staminaValueText.outlineWidth = 0.28f;
            staminaValueText.outlineColor = new Color32(16, 10, 8, 255);
            staminaValueText.raycastTarget = false;

            // Rage flame: visible only during a raid; fill height = Nộ Khí / 100.
            rageRoot = MythUi.Rect("RageFlame", safeRoot, MythUi.TL, MythUi.TL, new Vector2(104, -82), new Vector2(16, 24));
            for (int i = 0; i < 4; i++) flameFrames[i] = MythUi.Kit("bar_rage_flame_" + i);
            rageBack = MythUi.Img("Ghost", rageRoot, flameFrames[0], MythUi.TL, MythUi.TL, Vector2.zero);
            rageBack.color = new Color(0.25f, 0.2f, 0.2f, 0.7f);
            rageFill = new MythClipFill("Clip", rageRoot, flameFrames[0], MythUi.BL, MythUi.BL, Vector2.zero, 24, 16, true, false, false);
            rageFlame = rageFill.Fill;
            rageFlame.rectTransform.anchorMin = rageFlame.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rageFlame.rectTransform.pivot = new Vector2(0.5f, 0f);
            rageRoot.gameObject.SetActive(false);

            // Hero Level Badge (22x22) + radial EXP ring + QuickBag F5–F8 slots right beside it (icon-first, zero text clutter)
            heroBadgeRoot = MythUi.Rect("HeroBadge", safeRoot, MythUi.TL, MythUi.TL, new Vector2(4, -110), new Vector2(22, 22));
            MythUi.Img("BadgeBg", heroBadgeRoot, MythUi.Kit("hero_level_badge"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(22, 22), true);
            heroExpRing = MythUi.Img("ExpRing", heroBadgeRoot, MythUi.Kit("hero_exp_ring"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(22, 22));
            if (heroExpRing.sprite != null)
            {
                heroExpRing.type = Image.Type.Filled;
                heroExpRing.fillMethod = Image.FillMethod.Radial360;
                heroExpRing.fillOrigin = (int)Image.Origin360.Top;
                heroExpRing.fillClockwise = true;
                heroExpRing.fillAmount = 0f;
            }
            var lvlRt = MythUi.Rect("LvlText", heroBadgeRoot, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(22, 16));
            heroLevelText = lvlRt.gameObject.AddComponent<TextMeshProUGUI>();
            heroLevelText.fontSize = 10.5f;
            heroLevelText.fontStyle = FontStyles.Bold;
            heroLevelText.alignment = TextAlignmentOptions.Center;
            heroLevelText.color = Bright;
            heroLevelText.outlineWidth = 0.28f;
            heroLevelText.outlineColor = new Color32(16, 10, 8, 255);
            heroLevelText.raycastTarget = false;
            heroBadgeRoot.gameObject.AddComponent<MythTap>().OnTap = () => CharacterPanel.Toggle();

            // 4 ô túi nhanh F5–F8 có icon thật từ QuickBag (ngay bên phải HeroBadge)
            quickBagRow = MythUi.Rect("QuickBagRow", safeRoot, MythUi.TL, MythUi.TL, new Vector2(30, -110), new Vector2(104, 22));
            var slotFrame = MythUi.Kit("quick_slot_frame") ?? MythUi.Kit("bag_socket_empty");
            for (int i = 0; i < 4; i++)
            {
                int slotIdx = i;
                var slotRt = MythUi.Rect("QuickSlot" + i, quickBagRow, MythUi.TL, MythUi.TL, new Vector2(i * 24f, 0f), new Vector2(22, 22));
                var frameImg = MythUi.Img("Frame", slotRt, slotFrame, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(22, 22), true);
                frameImg.gameObject.AddComponent<MythTap>().OnTap = () => PlayerMobileController.Instance?.GetComponent<MartialSkillRunner>()?.UseQuickItem(slotIdx);
                quickSlotIcons[i] = MythUi.Img("Icon", slotRt, null, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(16, 16));
                var keyRt = MythUi.Rect("Key", slotRt, MythUi.TL, MythUi.TL, new Vector2(1f, -1f), new Vector2(16, 9));
                quickSlotKeys[i] = keyRt.gameObject.AddComponent<TextMeshProUGUI>();
                quickSlotKeys[i].fontSize = 7.5f;
                quickSlotKeys[i].fontStyle = FontStyles.Bold;
                quickSlotKeys[i].alignment = TextAlignmentOptions.TopLeft;
                quickSlotKeys[i].color = new Color(0.95f, 0.85f, 0.55f);
                quickSlotKeys[i].outlineWidth = 0.25f;
                quickSlotKeys[i].outlineColor = new Color32(16, 10, 8, 255);
                quickSlotKeys[i].raycastTarget = false;
                quickSlotKeys[i].text = "F" + (5 + i);

                var cntRt = MythUi.Rect("Count", slotRt, MythUi.BR, MythUi.BR, new Vector2(-1f, 1f), new Vector2(18, 10));
                quickSlotCounts[i] = cntRt.gameObject.AddComponent<TextMeshProUGUI>();
                quickSlotCounts[i].fontSize = 8.5f;
                quickSlotCounts[i].fontStyle = FontStyles.Bold;
                quickSlotCounts[i].alignment = TextAlignmentOptions.BottomRight;
                quickSlotCounts[i].color = Color.white;
                quickSlotCounts[i].outlineWidth = 0.25f;
                quickSlotCounts[i].outlineColor = new Color32(16, 10, 8, 255);
                quickSlotCounts[i].raycastTarget = false;
                quickSlotRoots[i] = slotRt;
            }
        }

        RectTransform heroPortraitRoot;
        Image heroPortraitImg;
        PlayerId? shownHeroPortraitId;
        string shownHeroEmotion;
        TextMeshProUGUI guardValueText;
        TextMeshProUGUI staminaValueText;
        RectTransform heroBadgeRoot;
        Image heroExpRing;
        TextMeshProUGUI heroLevelText;
        RectTransform quickBagRow;
        readonly RectTransform[] quickSlotRoots = new RectTransform[4];
        readonly Image[] quickSlotIcons = new Image[4];
        readonly TextMeshProUGUI[] quickSlotKeys = new TextMeshProUGUI[4];
        readonly TextMeshProUGUI[] quickSlotCounts = new TextMeshProUGUI[4];
        TextMeshProUGUI heroLine;
        float heroLineTimer;

        // ---------------------------------------------------------------- Boss bar (GDD 19.2: 3 pha theo % máu)
        RectTransform bossRoot;
        MythClipFill bossFill;
        TextMeshProUGUI bossText;
        readonly Image[] bossPhasePips = new Image[3];
        int bossPhaseShown;

        void UpdateBossBar()
        {
            EnemyAgentView boss = null;
            foreach (var en in EnemyAgentView.Registry)
                if (en != null && en.IsAlive && CombatRewards.Classify(en.Model.TypeId) == KillClass.Boss) { boss = en; break; }
            if (boss == null)
            {
                if (bossRoot != null && bossRoot.gameObject.activeSelf) bossRoot.gameObject.SetActive(false);
                bossPhaseShown = 0;
                return;
            }
            if (bossRoot == null)
            {
                var top = new Vector2(0.5f, 1f);
                bossRoot = MythUi.Rect("BossBar", safeRoot, top, top, new Vector2(0, -34), new Vector2(248, 30));
                var vessel = MythUi.Rect("Vessel", bossRoot, top, top, new Vector2(0, -13), new Vector2(240, 12));
                var troughSpr = MythUi.Kit("boss_bar_trough") ?? MythUi.Kit("bar_hp_trough");
                var fillSpr = MythUi.Kit("boss_bar_fill") ?? MythUi.Kit("bar_hp_fill");
                var frameSpr = MythUi.Kit("boss_bar_frame");
                MythUi.Img("Trough", vessel, troughSpr, MythUi.TL, MythUi.TL, Vector2.zero).rectTransform.sizeDelta = new Vector2(240, 12);
                bossFill = new MythClipFill("Clip", vessel, fillSpr, MythUi.TL, MythUi.TL, new Vector2(6, -2), 228, 8);
                if (frameSpr != null)
                {
                    var fr = MythUi.Img("Frame", vessel, frameSpr, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(248, 16));
                    fr.raycastTarget = false;
                }
                for (int p = 0; p < 3; p++)
                {
                    bossPhasePips[p] = MythUi.Img("PhasePip" + p, bossRoot, MythUi.Kit("boss_phase_pip_off"), top, top,
                        new Vector2(-16f + p * 16f, -26f), new Vector2(10, 10));
                }
                var t = MythUi.Rect("Name", bossRoot, top, top, Vector2.zero, new Vector2(300, 12));
                bossText = t.gameObject.AddComponent<TextMeshProUGUI>();
                bossText.fontSize = 8f;
                bossText.alignment = TextAlignmentOptions.Center;
                bossText.color = Bright;
                bossText.raycastTarget = false;
            }
            if (!bossRoot.gameObject.activeSelf) bossRoot.gameObject.SetActive(true);
            var m = boss.Model;
            bossFill.Set(m.MaxHp > 0f ? Mathf.Clamp01(m.CurrentHp / m.MaxHp) : 0f);
            int phase = m.GetBossPhase();
            var pipOnSpr = MythUi.Kit("boss_phase_pip_on");
            var pipOffSpr = MythUi.Kit("boss_phase_pip_off");
            for (int p = 0; p < 3; p++)
            {
                if (bossPhasePips[p] != null)
                    bossPhasePips[p].sprite = (p < phase) ? pipOnSpr : pipOffSpr;
            }
            bossText.text = $"<b>{m.Name}</b> · Pha {phase}/3 · {m.CurrentHp:0}/{m.MaxHp:0}";
            if (phase != bossPhaseShown)
            {
                if (bossPhaseShown != 0) PlayerInteractionController.Instance?.ShowFeedback($"{m.Name} sang PHA {phase}: {m.GetBossPhaseBehavior()}");
                bossPhaseShown = phase;
            }
        }

        void UpdateHeroLine(GameEngine engine, CharacterData ch)
        {
            if ((heroLineTimer -= Time.unscaledDeltaTime) > 0f) return;
            heroLineTimer = 0.25f;
            int expNext = MartialProgression.ExpToNext(ch.Level);
            if (heroLevelText != null) heroLevelText.text = ch.Level.ToString();
            if (heroExpRing != null)
            {
                heroExpRing.fillAmount = expNext > 0 ? Mathf.Clamp01((float)ch.Exp / expNext) : 1f;
                heroExpRing.color = ch.StatPoints > 0 ? new Color(0.6f, 0.95f, 0.48f) : Bright;
            }

            var kinds = QuickBag.Kinds(ch);
            for (int i = 0; i < 4; i++)
            {
                if (quickSlotRoots[i] == null) continue;
                bool hasSlot = i < kinds.Count;
                quickSlotRoots[i].gameObject.SetActive(hasSlot);
                if (!hasSlot) continue;
                var kind = kinds[i];
                int cnt = QuickBag.Count(ch, kind);
                var spr = UISpriteLoader.GetQuickItemIcon(kind, 16);
                quickSlotIcons[i].sprite = spr;
                quickSlotIcons[i].enabled = spr != null;
                quickSlotIcons[i].color = cnt > 0 ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.65f);
                quickSlotCounts[i].text = $"{cnt}";
                quickSlotCounts[i].color = cnt > 0 ? Color.white : new Color(0.65f, 0.55f, 0.55f);
            }
        }

        void UpdateBars(GameEngine engine)
        {
            var ch = engine.Character;
            if (ch == null) return;
            hpFill.Set(ch.MaxGuard > 0f ? ch.CurrentGuard / ch.MaxGuard : 0f); // thanh sống còn = Chống Chịu (không có HP)
            if (guardValueText != null) guardValueText.text = $"{ch.CurrentGuard:0}/{ch.MaxGuard:0}";

            var st = ch.Stamina;
            staminaBright.Set(st.StaminaRatio);
            // Lớp "bình bị thu": Tải ngày + Mệt Dội + Chống Chịu < 30% (bình 180 còn 135) — phần không thể hồi tới
            staminaLoad.Set(st.MaxStamina > 0f ? Mathf.Clamp01(1f - ch.GetEffectiveMaxStamina() / st.MaxStamina) : st.DailyLoadRatio);
            if (staminaValueText != null) staminaValueText.text = $"{st.CurrentStamina:0}/{ch.GetEffectiveMaxStamina():0}";
            // Fatigued (<20%): red rim pulses.
            float pulse = st.IsFatigued ? 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f)) : 0f;
            staminaRim.color = new Color(1f, 0.15f, 0.1f, pulse);

            if (heroPortraitImg != null)
            {
                string emo = ch.IsFainted ? "sad" : (st.IsFatigued ? "thoughtful" : (engine.Clock.IsDuringCombatWave ? "angry" : "normal"));
                if (shownHeroPortraitId != ch.Identity || shownHeroEmotion != emo)
                {
                    shownHeroPortraitId = ch.Identity;
                    shownHeroEmotion = emo;
                    var pSpr = UISpriteLoader.GetPortrait(ch.Identity == PlayerId.Khoa ? "khoa" : "an", emo) ?? UISpriteLoader.GetNeutralSilhouettePortrait();
                    if (pSpr != null) heroPortraitImg.sprite = pSpr;
                }
            }

            UpdateHeroLine(engine, ch);
            // Nộ hiện khi đang có đợt quái hoặc còn Nộ (Nộ chỉ tắt dần sau 10 giây ngoài giao tranh)
            bool raid = engine.Clock.IsDuringCombatWave || ch.Rage > 0;
            if (rageRoot.gameObject.activeSelf != raid) rageRoot.gameObject.SetActive(raid);
            if (raid)
            {
                int f = ((int)(Time.unscaledTime * 8f)) & 3;
                rageBack.sprite = flameFrames[f];
                rageFlame.sprite = flameFrames[f];
                rageFill.Set(Mathf.Clamp01(ch.Rage / 100f));
            }
        }

        // ---------------------------------------------------------------- status effects (16 px icons, stacks = dots)
        void BuildStatusRow()
        {
            statusRow = MythUi.Rect("Status", safeRoot, MythUi.TL, MythUi.TL, new Vector2(4, -135), new Vector2(180, 16));
        }

        StatusSlot Slot(string stem)
        {
            if (statusSlots.TryGetValue(stem, out var slot)) return slot;
            slot = new StatusSlot();
            slot.Icon = MythUi.Img(stem, statusRow, MythUi.Icon("status", stem, 16), MythUi.TL, MythUi.TL, Vector2.zero);
            slot.Dots = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                slot.Dots[i] = MythUi.Img("Dot" + i, slot.Icon.transform, null, MythUi.BL, MythUi.BL, new Vector2(1 + i * 3, 0), new Vector2(2, 2));
                slot.Dots[i].enabled = true;
                slot.Dots[i].color = Bright;
            }
            statusSlots[stem] = slot;
            return slot;
        }

        void UpdateStatus(GameEngine engine)
        {
            var ch = engine.Character;
            if (ch == null) return;
            // Icon-first status row: all player & weather status effects use 16 px icons (stacks = dots)
            var active = new List<KeyValuePair<string, int>>();
            if (ch.IsFainted) active.Add(new KeyValuePair<string, int>("bat_tinh", 0));
            if (ch.Stamina.CurrentStamina <= 0f) active.Add(new KeyValuePair<string, int>("kiet_suc", 0));
            if (ch.Stamina.ReboundFatigueStacks > 0) active.Add(new KeyValuePair<string, int>("met_doi", Mathf.Min(4, ch.Stamina.ReboundFatigueStacks)));
            // GDD 7.2 trạng thái thời tiết lên người (PlayerWeatherStatus); Nhiễm Xạ: chấm = tầng
            var ws = engine.WeatherStatus;
            if (ws.CamNang) active.Add(new KeyValuePair<string, int>("cam_nang", 0));
            if (ws.UotLanh) active.Add(new KeyValuePair<string, int>("uot_lanh", 0));
            if (ws.HasCamGio(engine)) active.Add(new KeyValuePair<string, int>("cam_gio", 0));
            if (ws.NhiemXaStacks > 0) active.Add(new KeyValuePair<string, int>("nhiem_xa", Mathf.Min(4, ws.NhiemXaStacks)));
            if (ws.HasCongTay(engine)) active.Add(new KeyValuePair<string, int>("cong_tay", 0));
            if (ws.HasKhoRat(engine)) active.Add(new KeyValuePair<string, int>("kho_rat", 0));
            if (ws.HasLacHuong(engine)) active.Add(new KeyValuePair<string, int>("lac_huong", 0));
            if (engine.FogWarningUntilMinute >= engine.Clock.TotalElapsedGameMinutes)
                active.Add(new KeyValuePair<string, int>("la_do_suong", 0));
            if (weatherStatusText != null && weatherStatusText.gameObject.activeSelf)
                weatherStatusText.gameObject.SetActive(false);

            foreach (var kv in statusSlots) kv.Value.Icon.gameObject.SetActive(false);
            for (int i = 0; i < active.Count; i++)
            {
                var slot = Slot(active[i].Key);
                slot.Icon.gameObject.SetActive(true);
                slot.Icon.rectTransform.anchoredPosition = new Vector2(i * 18, 0);
                for (int d = 0; d < 4; d++) slot.Dots[d].gameObject.SetActive(d < active[i].Value);
            }
        }

        TextMeshProUGUI weatherStatusText;

        // ---------------------------------------------------------------- coin, herd, morale, satchel (top-right)
        void BuildTopRight()
        {
            // herd ring gauge 32x32 (lit over dim), pig head in the centre
            var herd = MythUi.Rect("Herd", safeRoot, MythUi.TR, MythUi.TR, new Vector2(-4, -4), new Vector2(32, 32));
            MythUi.Img("Dim", herd, MythUi.Kit("herd_ring_gauge_dim"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(32, 32), true);
            herdLit = MythUi.Img("Lit", herd, MythUi.Kit("herd_ring_gauge"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(32, 32));
            herdLit.type = Image.Type.Filled;
            herdLit.fillMethod = Image.FillMethod.Radial360;
            herdLit.fillOrigin = (int)Image.Origin360.Top;
            herdLit.fillClockwise = true;
            herdLit.fillAmount = 0f;
            MythUi.Img("Pig", herd, MythUi.Kit("herd_pighead_16"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(16, 16));
            capacityTick = MythUi.Img("CapacityTick", herd, null, MythUi.BL, MythUi.BL, Vector2.zero, new Vector2(2, 2));
            capacityTick.enabled = true; capacityTick.color = Bright;
            herd.gameObject.AddComponent<MythTap>().OnTap = () => { if (FarmOverviewPopup.Instance != null) FarmOverviewPopup.Instance.TogglePopup(); };

            for (int i = 0; i < 4; i++) faces[i] = MythUi.Kit("morale_face_" + i);
            herdFace = MythUi.Img("Morale", herd, faces[0], MythUi.BL, MythUi.BL, new Vector2(-4, -4), new Vector2(12, 12));

            // Quick-access top-right medallions for mouse/touch ([C] Nhân vật, [N] Sổ tay, [P] Xây dựng, [B] Trại)
            var btnChar = MythMedallion.Create("TopBtnChar", safeRoot, false, MythUi.TR, MythUi.TR, new Vector2(-60, -4));
            btnChar.SetIcon(MythUi.Icon("actions", "hoc_vo", 16));
            btnChar.SetKeyHint("C");
            btnChar.OnClick = () => CharacterPanel.Toggle();

            var btnJournal = MythMedallion.Create("TopBtnJournal", safeRoot, false, MythUi.TR, MythUi.TR, new Vector2(-94, -4));
            btnJournal.SetIcon(MythUi.Icon("misc", "tinh_bao", 16) ?? MythUi.Icon("actions", "giam_dinh", 16));
            btnJournal.SetKeyHint("N");
            btnJournal.OnClick = () => QuestTrackerPanel.Instance?.ToggleJournal();

            var btnBuild = MythMedallion.Create("TopBtnBuild", safeRoot, false, MythUi.TR, MythUi.TR, new Vector2(-128, -4));
            btnBuild.SetIcon(MythUi.Icon("actions", "sua", 16));
            btnBuild.SetKeyHint("P");
            btnBuild.OnClick = () => DefenseBuildPopup.Instance?.Toggle();

            var btnInterior = MythMedallion.Create("TopBtnInterior", safeRoot, false, MythUi.TR, MythUi.TR, new Vector2(-162, -4));
            btnInterior.SetIcon(MythUi.Icon("actions", "vao_cua", 16));
            btnInterior.SetKeyHint("H");
            btnInterior.OnClick = () => BuildingInteriorPopup.Ensure().Toggle();

            // coin + gold number + compact icon-first Sinh Cảnh & Cám/Đoàn gauges
            var infoPill = MythUi.Rect("TopRightInfoPill", safeRoot, MythUi.TR, MythUi.TR, new Vector2(-4, -38), new Vector2(138, 42));
            var infoPillImg = infoPill.gameObject.AddComponent<Image>();
            infoPillImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            infoPillImg.type = Image.Type.Sliced;
            infoPillImg.color = new Color(0.08f, 0.11f, 0.16f, 0.94f);
            infoPillImg.raycastTarget = false;

            MythUi.Img("Coin", safeRoot, MythUi.Kit("coin_icon"), MythUi.TR, MythUi.TR, new Vector2(-8, -41), new Vector2(16, 16));
            var textRt = MythUi.Rect("Gold", safeRoot, MythUi.TR, MythUi.TR, new Vector2(-26, -40), new Vector2(108, 18));
            goldText = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            goldText.fontSize = 12.5f;
            goldText.fontStyle = FontStyles.Bold;
            goldText.alignment = TextAlignmentOptions.MidlineRight;
            goldText.color = Bright;
            goldText.outlineWidth = 0.28f;
            goldText.outlineColor = new Color32(20, 12, 6, 255);
            goldText.textWrappingMode = TextWrappingModes.NoWrap;
            goldText.raycastTarget = false;
            goldText.text = string.Empty;

            // GDD 6.1: Compact icon + bold number for Sinh Cảnh (0–100)
            MythUi.Img("HabitatIcon", safeRoot, MythUi.Icon("places", "khu_cach_ly", 16) ?? MythUi.Icon("weather", "quang_dang", 16),
                MythUi.TR, MythUi.TR, new Vector2(-124, -60), new Vector2(14, 14));
            var scRt = MythUi.Rect("Habitat", safeRoot, MythUi.TR, MythUi.TR, new Vector2(-86, -59), new Vector2(36, 16));
            habitatText = scRt.gameObject.AddComponent<TextMeshProUGUI>();
            habitatText.fontSize = 10.5f;
            habitatText.fontStyle = FontStyles.Bold;
            habitatText.alignment = TextAlignmentOptions.MidlineLeft;
            habitatText.outlineWidth = 0.28f;
            habitatText.outlineColor = new Color32(20, 12, 6, 255);
            habitatText.textWrappingMode = TextWrappingModes.NoWrap;
            habitatText.raycastTarget = false;

            // Compact icon + bold number for Feed / Drove
            MythUi.Img("FeedIcon", safeRoot, MythUi.Icon("actions", "cho_an", 16),
                MythUi.TR, MythUi.TR, new Vector2(-68, -60), new Vector2(14, 14));
            var fsRt = MythUi.Rect("FarmDroveAndFeed", safeRoot, MythUi.TR, MythUi.TR, new Vector2(-8, -59), new Vector2(58, 16));
            farmSubLineText = fsRt.gameObject.AddComponent<TextMeshProUGUI>();
            farmSubLineText.fontSize = 10.5f;
            farmSubLineText.fontStyle = FontStyles.Bold;
            farmSubLineText.alignment = TextAlignmentOptions.MidlineRight;
            farmSubLineText.outlineWidth = 0.28f;
            farmSubLineText.outlineColor = new Color32(20, 12, 6, 255);
            farmSubLineText.textWrappingMode = TextWrappingModes.NoWrap;
            farmSubLineText.color = new Color(0.96f, 0.90f, 0.72f);
            farmSubLineText.raycastTarget = false;

            // Satchel positioned at (-116, -84) cleanly to the left of the 108x82 Minimap Radar at (-4, -84)
            bagRoot = MythUi.Rect("Satchel", safeRoot, MythUi.TR, MythUi.TR, new Vector2(-116, -84), new Vector2(64, 64));
            var satchel = MythUi.Img("Bag", bagRoot, MythUi.Kit("bag_satchel"), MythUi.TL, MythUi.TL, new Vector2(16, -12), null, true);
            var bcRt = MythUi.Rect("BagCount", bagRoot, MythUi.TL, MythUi.TL, new Vector2(8, -44), new Vector2(48, 15));
            bagCountText = bcRt.gameObject.AddComponent<TextMeshProUGUI>();
            bagCountText.fontSize = 10.5f; bagCountText.fontStyle = FontStyles.Bold;
            bagCountText.alignment = TextAlignmentOptions.Center;
            bagCountText.color = Bright; bagCountText.outlineWidth = 0.28f; bagCountText.outlineColor = new Color32(20, 12, 6, 255);
            bagCountText.textWrappingMode = TextWrappingModes.NoWrap; bagCountText.raycastTarget = false;
            satchel.gameObject.AddComponent<MythTap>().OnTap = () =>
            {
                if (HotbarController.Instance != null) HotbarController.Instance.OnBaloClicked();
                else { var popup = BackpackPopup.Instance; if (popup != null) popup.Toggle(); }
            };

            // warehouse link (bracelet only): opens the 100-slot warehouse anywhere outside combat
            warehouseLink = MythMedallion.Create("WarehouseLink", safeRoot, false, MythUi.TR, MythUi.TR, new Vector2(-128, -148));
            warehouseLink.SetIcon(MythUi.Icon("places", "kho", 16));
            warehouseLink.OnClick = OpenWarehouseLink;
            warehouseLink.gameObject.SetActive(false);
        }

        float nextStarterScan;
        /// <summary>Farm map only, once: a feed sack ~3 m in front of the warehouse for the starter quest's deposit objective.</summary>
        void TrySpawnStarterPickup(GameEngine engine)
        {
            if (!engine.Carry.ShouldSpawnStarterPickup || Time.unscaledTime < nextStarterScan) return;
            nextStarterScan = Time.unscaledTime + 1f;
            if (!string.Equals(engine.WorldLocations.CurrentLocationId, "Farm", System.StringComparison.OrdinalIgnoreCase)) return;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                string n = t.name;
                if (!n.StartsWith("building:", System.StringComparison.OrdinalIgnoreCase) ||
                    n.IndexOf("warehouse", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                var data = new LootItemData
                {
                    Id = PlayerCarry.StarterPickupItemId, DisplayName = "Bao cám", Category = LootCategory.Material,
                    Rarity = LootRarity.Thuong, Quantity = 1, GoldValue = 0, SpriteKey = "item_corn_feed"
                };
                LootDrop2DView.Spawn(data, (Vector2)t.position + new Vector2(0f, -3f));
                engine.Carry.MarkStarterPickupSpawned();
                return;
            }
        }

        void OpenWarehouseLink()
        {
            var engine = Engine;
            if (engine == null) return;
            if (!engine.Carry.CanUseWarehouseLink(out var why))
            {
                if (PlayerInteractionController.Instance != null) PlayerInteractionController.Instance.ShowFeedback(why);
                return;
            }
            BackpackPopup.Ensure()?.ShowWarehouseLink();
        }

        void EnsureBagSockets(int count)
        {
            if (bagLayoutBuilt == count) return;
            bagLayoutBuilt = count;
            for (int i = 0; i < 8; i++)
            {
                if (bagSockets[i] != null) Destroy(bagSockets[i].gameObject);
                bagSockets[i] = bagItems[i] = bagRims[i] = null;
            }
            Vector2[] pts = count <= 1
                ? new[] { new Vector2(33, 3) } // before the bracelet: one "hand" socket
                : count <= 3
                ? new[] { new Vector2(9, 15), new Vector2(33, 3), new Vector2(57, 15) }
                : new[] { new Vector2(33, 3), new Vector2(54, 12), new Vector2(63, 33), new Vector2(54, 54),
                          new Vector2(33, 63), new Vector2(12, 54), new Vector2(3, 33), new Vector2(12, 12) };
            var empty = MythUi.Kit("bag_socket_empty");
            var rim = MythUi.Kit("bag_socket_filled_rim");
            for (int i = 0; i < pts.Length; i++)
            {
                var p = new Vector2(pts[i].x, -pts[i].y);
                bagSockets[i] = MythUi.Img("Socket" + i, bagRoot, empty, MythUi.TL, MythUi.TL, p, new Vector2(14, 14));
                bagItems[i] = MythUi.Img("Item" + i, bagSockets[i].transform, null, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(12, 12));
                bagRims[i] = MythUi.Img("Rim" + i, bagSockets[i].transform, rim, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(14, 14));
                bagRims[i].enabled = false;
            }
            bagSignature = string.Empty;
        }

        void UpdateTopRight(GameEngine engine)
        {
            goldText.text = engine.Farm.Gold.ToString("N0");
            if (habitatText != null)
            {
                int sc = engine.Farm.HabitatIndex;
                Color col = sc >= 90 ? new Color(0.55f, 1f, 0.68f) : sc >= 70 ? new Color(0.78f, 0.96f, 0.68f)
                    : sc >= 50 ? new Color(0.96f, 0.90f, 0.66f) : sc >= 30 ? new Color(1f, 0.68f, 0.42f) : new Color(1f, 0.42f, 0.38f);
                habitatText.text = $"{sc}";
                habitatText.color = col;
            }
            if (farmSubLineText != null)
            {
                int droveCount = engine.Drove.PigIds.Count;
                if (droveCount > 0)
                {
                    farmSubLineText.text = $"<color=#FDE047>{droveCount}/{MarketDrove.MaxDroveSize}</color>";
                }
                else
                {
                    int hungry = 0;
                    for (int i = 0; i < engine.Pigs.Count; i++) if (TroughCommands.WantsFood(engine, engine.Pigs[i])) hungry++;
                    string hungryTag = hungry > 0 ? $" <color=#FCA5A5>!{hungry}</color>" : "";
                    farmSubLineText.text = $"{engine.Farm.FeedStockKg:0}kg{hungryTag}";
                }
            }

            int living = 0, moodSum = 0;
            var pigs = engine.Pigs;
            for (int i = 0; i < pigs.Count; i++) { if (!pigs[i].IsAlive) continue; living++; moodSum += pigs[i].Mood; }
            herdLit.fillAmount = Mathf.Clamp01(living / 200f); // art maps 0..200 pigs to 360 degrees, bands baked into the ring
            int cap = engine.Farm.CalculateCapacityReport(living, 0).EffectiveCapacity;
            float ang = Mathf.Clamp01(cap / 200f) * 2f * Mathf.PI;
            capacityTick.rectTransform.anchoredPosition = new Vector2(Mathf.Round(16f + 14f * Mathf.Sin(ang) - 1f), Mathf.Round(16f + 14f * Mathf.Cos(ang) - 1f));

            // Morale face from average Mood, with the same thresholds Pig.GetMoodState uses (60 / 40 / 20).
            int face = 0;
            if (living > 0)
            {
                float avg = (float)moodSum / living;
                face = avg >= 60f ? 0 : avg >= 40f ? 1 : avg >= 20f ? 2 : 3;
            }
            herdFace.sprite = faces[face];
            herdFace.enabled = faces[face] != null && living > 0;
        }

        void UpdateBag(GameEngine engine)
        {
            var bag = engine.SpaceBag;
            var carry = engine.Carry;
            if (bag == null || carry == null) return;
            bool bracelet = carry.HasBracelet;
            // No bracelet: one hand socket. Bracelet: bag sockets (3 basic / 8 premium) + warehouse-link icon.
            EnsureBagSockets(bracelet ? (bag.Capacity <= 3 ? 3 : 8) : 1);
            var items = new List<UnifiedItemInstance>();
            if (bracelet)
            {
                // one socket per bag slot: same item + quality share a slot up to CarryRules.BagStackPerSlot (GDD L285)
                foreach (var g in bag.Items.GroupBy(it => (it.QualifiedItemId, it.Quality)))
                {
                    var list = g.ToList();
                    for (int k = 0; k < list.Count; k += CarryRules.BagStackPerSlot) items.Add(list[k]);
                }
            }
            else if (carry.HandItem != null) items.Add(carry.HandItem);
            string sig = (bracelet ? "B" : "H") + items.Count + ":";
            for (int i = 0; i < items.Count; i++) sig += items[i].ItemId + "x" + items[i].Stack + ";";
            if (bagCountText != null) bagCountText.text = bracelet ? $"{items.Count}/{(bag.Capacity <= 3 ? 3 : 8)}" : (items.Count > 0 ? "1/1" : "0/1");
            warehouseLink.gameObject.SetActive(bracelet);
            warehouseLink.SetEnabled(bracelet && !carry.InCombat);
            if (sig == bagSignature) return;
            bagSignature = sig;
            for (int i = 0; i < bagLayoutBuilt; i++)
            {
                bool filled = i < items.Count;
                Sprite s = filled ? UISpriteLoader.GetLootSprite(items[i].ItemId) : null;
                bagItems[i].sprite = s;
                bagItems[i].enabled = s != null;
                bagRims[i].enabled = filled;
                bagSockets[i].enabled = false; bagItems[i].enabled = false; bagRims[i].enabled = false;
            }
        }

        // ---------------------------------------------------------------- joystick (bottom-left)
        void BuildJoystick()
        {
            var root = MythUi.Rect("Joystick", safeRoot, MythUi.BL, MythUi.BL, new Vector2(8, 8), new Vector2(112, 112));
            var hit = root.gameObject.AddComponent<Image>(); // transparent, gives the whole 112 px area to the touch
            hit.color = new Color(0, 0, 0, 0); hit.raycastTarget = true;
            var baseImg = MythUi.Img("Base", root, MythUi.Kit("joystick_base"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(96, 96));
            var knobNormal = MythUi.Kit("joystick_knob");
            var knob = MythUi.Img("Knob", root, knobNormal, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(40, 40));

            joystick = root.gameObject.AddComponent<MobileJoystick>();
            joystick.BackgroundRect = baseImg.rectTransform;
            joystick.HandleRect = knob.rectTransform;
            joystick.InputRadiusUnits = 30f;  // drag 30 canvas units = full deflection
            joystick.HandleTravelUnits = 10f; // knob stays inside the r=30 recess (knob r=20)
            var skin = root.gameObject.AddComponent<MythJoystickSkin>();
            skin.Knob = knob; skin.Normal = knobNormal; skin.Pressed = MythUi.Kit("joystick_knob_pressed");
            BindPlayerJoystick();
            // a 112 px touch stick on a keyboard PC is only clutter (3 Nov playtest): touch screens keep it
            root.gameObject.SetActive(Application.isMobilePlatform || Input.touchSupported);
        }

        // ---------------------------------------------------------------- action cluster (bottom-right, icon-first medallions)
        void BuildActions()
        {
            contextButton = MythMedallion.Create("Context", safeRoot, true, MythUi.BR, MythUi.BR, new Vector2(-8, 10));
            contextButton.OnClick = OnContextTapped;
            contextButton.SetKeyHint("");
            contextButton.SetEnabled(true);

            // Core 3-button arc around Primary Context/Attack medallion (-8, 10):
            // 1) Dodge (-74, 14), 2) SkillCong [Z] (-62, 68), 3) Whistle [G] (-14, 76)
            dodgeButton = MythMedallion.Create("Dodge", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-74, 14));
            dodgeButton.SetIcon(MythUi.Icon("actions", "luot_ne", 16) ?? MythUi.Icon("actions", "vao_cua", 16));
            dodgeButton.OnClick = () => PlayerCombatController.Instance?.ExecuteDodge();
            dodgeButton.SetKeyHint("Ctrl");

            attackButton = MythMedallion.Create("SkillCong", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-62, 68));
            attackButton.SetIcon(MythUi.Icon("actions", "hoc_vo", 16));
            attackButton.OnClick = () => Skill(CombatSlotType.TheCong);

            // Secondary combat buttons (auto-hidden outside combat waves / when no enemy is nearby):
            defendButton = MythMedallion.Create("SkillThu", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-120, 68));
            defendButton.SetIcon(MythUi.Icon("actions", "do_don", 16));
            defendButton.OnClick = () => Skill(CombatSlotType.TheThu);
            burstButton = MythMedallion.Create("SkillBien", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-126, 14));
            burstButton.SetIcon(MythUi.Icon("actions", "hoc_vo", 16));
            burstButton.OnClick = () => Skill(CombatSlotType.TheBien);
            songTheButton = MythMedallion.Create("SkillSongThe", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-14, 128));
            songTheButton.SetIcon(MythUi.Icon("actions", "cuong_hoa", 16));
            songTheButton.OnClick = () => Skill(CombatSlotType.TheSongThe);

            counterButton = MythMedallion.Create("Counter", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-68, 122));
            counterButton.SetIcon(MythUi.Icon("actions", "phan_kich", 16) ?? MythUi.Icon("actions", "do_don", 16));
            counterButton.OnClick = () => PlayerCombatController.Instance?.PressCounter();
            counterButton.SetKeyHint("Q");

            // Whistle horn = "Lùa về Nơi Trú" (GDD 3.7), on the PixelMyth farm only; G on a keyboard.
            whistleButton = MythMedallion.Create("Whistle", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-14, 76));
            whistleButton.SetIcon(MythUi.Icon("actions", "huyt_sao", 16));
            whistleButton.OnClick = HerdRecall;
            whistleButton.SetKeyHint("G");
            whistleButton.SetEnabled(false);

            // Inspect pig ("Quan sát" / Mắt Nhà Nghề) touch medallion (M key)
            observeButton = MythMedallion.Create("Observe", safeRoot, false, MythUi.BR, MythUi.BR, new Vector2(-172, 16));
            observeButton.SetIcon(MythUi.Kit("trade_eye_icon") ?? MythUi.Icon("actions", "giam_dinh", 16));
            observeButton.SetKeyHint("M");
            observeButton.OnClick = OnObserveTapped;
            observeButton.gameObject.SetActive(false);
        }

        void OnObserveTapped()
        {
            if (UISpriteLoader.IsAnyModalOrDialogueOpen()) return;
            var pic = PlayerInteractionController.Instance;
            PigAgentView pv = contextTarget as PigAgentView ?? (pic != null ? pic.CurrentTarget as PigAgentView : null);
            if (pv != null && pv.PigModel != null)
            {
                var engine = Engine;
                if (engine != null && engine.Character != null && engine.Character.Identity == PlayerId.An && pic != null)
                {
                    // Mắt Nhà Nghề: the real 5 s observation in PlayerInteractionController (GDD 8.1, same as M).
                    // The old tap only ran the HUD bar and never reached ProtagonistTraits (3 Nov audit #5).
                    pic.StartTradeEyeObservationFor(pv.PigModel.Id);
                    NotifyTradeEyeStarted(pv.PigModel.Id);
                    return;
                }
                PigInspectPopup.Instance?.Show(pv.PigModel);
            }
        }

        public static void HerdRecall()
        {
            if (UISpriteLoader.IsAnyModalOrDialogueOpen()) return;
            var farm = PixelMythFarmGameplay.Instance;
            var p = PlayerMobileController.Instance;
            if (farm == null || p == null) return;
            var anim = p.GetComponent<CharacterSpriteAnimator>();
            if (anim != null) anim.TriggerWhistle();
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool khoa = engine != null && engine.Character != null && engine.Character.Identity == PlayerId.Khoa;
            bool whistleReady = khoa && ProtagonistTraits.WhistleCooldownMinutesLeft(engine) == 0;
            string msg = khoa && !shift && whistleReady ? farm.KhoaWhistle(p.transform) : farm.RecallHerd(p.transform.position);
            PlayerInteractionController.Instance?.ShowFeedback(msg);
        }

        /// <summary>Small caption under a skill medallion: slotted skill name, or fallback combat action.</summary>
        static TextMeshProUGUI SkillLabel(MythMedallion m, string key)
        {
            var pillRt = MythUi.Rect("LabelPill", m.transform, MythUi.MID, new Vector2(0.5f, 1f), new Vector2(0, -17), new Vector2(48, 13));
            var pillImg = pillRt.gameObject.AddComponent<Image>();
            pillImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            pillImg.type = Image.Type.Sliced;
            pillImg.color = new Color(0.18f, 0.14f, 0.10f, 0.90f);
            pillImg.raycastTarget = false;
            var rt = MythUi.Rect("Label", pillRt, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(46, 12));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = 8.5f;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Truncate;
            t.raycastTarget = false;
            t.color = new Color(1f, 0.95f, 0.82f);
            t.outlineWidth = 0.28f;
            t.outlineColor = new Color32(18, 10, 6, 255);
            t.text = key;
            return t;
        }

        static readonly string[] SlotKeys = { "Z", "X", "V", "R" };
        static readonly string[] DefaultSlotIconStems = { "danh", "do_don", "hoc_vo", "cuong_hoa" };
        static readonly string[] DefaultSlotFallbackLabels = { "Võ kỹ", "Thủ thế", "Biến chiêu", "Song thế" };

        void UpdateSkillSlot(GameEngine engine, MythMedallion m, TextMeshProUGUI label, CombatSlotType slot, int keyIndex, bool alive)
        {
            var c = engine.Character;
            bool open = c.IsMartialSlotUnlocked(slot);
            var s = MartialProgression.Slotted(c, slot);
            string key = SlotKeys[keyIndex];
            m.SetKeyHint(key);
            if (s != null)
            {
                var skillSpr = UISpriteLoader.GetSkillIcon(s.Id, 16) ?? MythUi.Icon("actions", DefaultSlotIconStems[keyIndex], 16);
                m.SetIcon(skillSpr);
            }
            else
            {
                m.SetIcon(MythUi.Icon("actions", DefaultSlotIconStems[keyIndex], 16));
            }
            // Keep Z/X/V clickable whenever alive: fallback triggers basic attack / secondary guard / Martial tab [C]
            bool canPress = slot == CombatSlotType.TheSongThe ? (alive && open && s != null) : alive;
            m.SetEnabled(canPress);
            m.SetCooldown(open && s != null ? MartialProgression.CooldownRemaining01(c, slot, Time.time) : 0f);
            if (label == null) return;
            if (s != null && open) label.text = s.Name;
            else label.text = DefaultSlotFallbackLabels[keyIndex];
        }

        // ---------------------------------------------------------------- An's 5-second Mắt Nhà Nghề progress bar above pig head
        static string tradeEyeTargetPigId;
        static float tradeEyeStartUnscaledTime = -999f;
        RectTransform tradeEyeBarRoot;
        MythClipFill tradeEyeFill;
        TextMeshProUGUI tradeEyeLabel;

        /// <summary>Called when An starts the 5-second Mắt Nhà Nghề observation on a pig (via M key or PigInspectPopup).</summary>
        public static void NotifyTradeEyeStarted(string pigId)
        {
            if (string.IsNullOrEmpty(pigId)) return;
            tradeEyeTargetPigId = pigId;
            tradeEyeStartUnscaledTime = Time.unscaledTime;
        }

        void UpdateTradeEyeBar(GameEngine engine)
        {
            if (engine == null || engine.Character == null)
            {
                if (tradeEyeBarRoot != null && tradeEyeBarRoot.gameObject.activeSelf) tradeEyeBarRoot.gameObject.SetActive(false);
                return;
            }

            // Detect M key press near a pig to inspect & observe
            if (Input.GetKeyDown(KeyCode.M) && !UISpriteLoader.IsAnyModalOrDialogueOpen())
            {
                OnObserveTapped();
            }

            if (engine.Character.Identity != PlayerId.An)
            {
                if (tradeEyeBarRoot != null && tradeEyeBarRoot.gameObject.activeSelf) tradeEyeBarRoot.gameObject.SetActive(false);
                return;
            }

            float elapsed = Time.unscaledTime - tradeEyeStartUnscaledTime;
            float duration = ProtagonistTraits.ObserveRealSeconds > 0f ? ProtagonistTraits.ObserveRealSeconds : 5f;
            if (string.IsNullOrEmpty(tradeEyeTargetPigId) || elapsed < 0f || elapsed > duration)
            {
                if (tradeEyeBarRoot != null && tradeEyeBarRoot.gameObject.activeSelf) tradeEyeBarRoot.gameObject.SetActive(false);
                return;
            }

            PigAgentView targetView = null;
            foreach (var pv in FindObjectsByType<PigAgentView>(FindObjectsSortMode.None))
            {
                if (pv != null && pv.PigModel != null && pv.PigModel.Id == tradeEyeTargetPigId)
                {
                    targetView = pv;
                    break;
                }
            }

            var player = PlayerMobileController.Instance;
            if (targetView == null || player == null ||
                Vector2.Distance(player.transform.position, targetView.transform.position) > (PlayerInteractionController.Instance != null ? PlayerInteractionController.Instance.InteractionRadius + 1f : 3.5f))
            {
                if (tradeEyeBarRoot != null && tradeEyeBarRoot.gameObject.activeSelf) tradeEyeBarRoot.gameObject.SetActive(false);
                return;
            }

            if (tradeEyeBarRoot == null)
            {
                tradeEyeBarRoot = MythUi.Rect("TradeEyeBar", safeRoot, MythUi.BL, MythUi.BL, Vector2.zero, new Vector2(64, 18));
                var vessel = MythUi.Rect("Vessel", tradeEyeBarRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(64, 10));
                MythUi.Img("Frame", vessel, MythUi.Kit("trade_eye_frame") ?? MythUi.Kit("bar_hp_trough"), MythUi.TL, MythUi.TL, Vector2.zero, new Vector2(64, 10));
                tradeEyeFill = new MythClipFill("Fill", vessel, MythUi.Kit("trade_eye_fill") ?? MythUi.Kit("bar_stamina_fill_bright"),
                    MythUi.TL, MythUi.TL, new Vector2(3, -2), 58, 6);
                MythUi.Img("EyeIcon", tradeEyeBarRoot, MythUi.Kit("trade_eye_icon") ?? MythUi.Icon("actions", "giam_dinh", 16),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-9f, -2f), new Vector2(14, 14));
                var lblRt = MythUi.Rect("Label", tradeEyeBarRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(96, 9));
                tradeEyeLabel = lblRt.gameObject.AddComponent<TextMeshProUGUI>();
                tradeEyeLabel.fontSize = 6f;
                tradeEyeLabel.alignment = TextAlignmentOptions.Center;
                tradeEyeLabel.color = Bright;
                tradeEyeLabel.raycastTarget = false;
            }

            var cam = Camera.main;
            if (cam == null) return;
            Vector3 worldPos = targetView.transform.position + new Vector3(0f, 0.85f, 0f);
            Vector3 screenPt = cam.WorldToScreenPoint(worldPos);
            if (screenPt.z < 0f)
            {
                tradeEyeBarRoot.gameObject.SetActive(false);
                return;
            }

            if (!tradeEyeBarRoot.gameObject.activeSelf) tradeEyeBarRoot.gameObject.SetActive(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot, screenPt, null, out Vector2 localPt);
            tradeEyeBarRoot.anchorMin = tradeEyeBarRoot.anchorMax = new Vector2(0.5f, 0.5f);
            tradeEyeBarRoot.anchoredPosition = localPt;
            float ratio = Mathf.Clamp01(elapsed / duration);
            tradeEyeFill.Set(ratio);
            if (tradeEyeLabel != null)
                tradeEyeLabel.text = $"Mắt Nhà Nghề · {(duration - elapsed):0.0}s";
        }

        static void Skill(CombatSlotType slot)
        {
            if (UISpriteLoader.IsAnyModalOrDialogueOpen()) return;
            var p = PlayerMobileController.Instance;
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            var c = engine != null ? engine.Character : null;
            if (p != null && c != null && c.IsMartialSlotUnlocked(slot) && MartialProgression.Slotted(c, slot) != null)
            {
                p.ExecuteMartialSkill(slot);
                return;
            }
            // Fallback when slot is not yet equipped with a martial skill:
            if (slot == CombatSlotType.TheCong)
            {
                if (HotbarController.Instance != null) HotbarController.Instance.EquipTool(StardewToolType.DaoGo);
                PlayerCombatController.Instance?.ExecuteAttack();
            }
            else if (slot == CombatSlotType.TheThu)
            {
                if (PlayerCombatController.Instance != null) PlayerCombatController.Instance.ExecuteSecondarySkill();
                else CharacterPanel.Show(1);
            }
            else
            {
                CharacterPanel.Show(1);
                PlayerInteractionController.Instance?.ShowFeedback("Hãy lắp Võ Kỹ tại thẻ Võ Học [C].");
            }
        }

        void UpdateDrinkBar()
        {
            var runner = PlayerMobileController.Instance != null ? PlayerMobileController.Instance.GetComponent<MartialSkillRunner>() : null;
            bool drinking = runner != null && runner.IsDrinking;
            if (!drinking)
            {
                wasDrinking = false;
                if (drinkBarRoot != null && drinkBarRoot.gameObject.activeSelf) drinkBarRoot.gameObject.SetActive(false);
                return;
            }
            if (!wasDrinking)
            {
                wasDrinking = true;
                drinkStartTime = Time.unscaledTime;
            }
            if (drinkBarRoot == null)
            {
                drinkBarRoot = MythUi.Rect("DrinkProgressBar", safeRoot, MythUi.TL, MythUi.TL, new Vector2(4, -150), new Vector2(96, 10));
                MythUi.Img("Trough", drinkBarRoot, MythUi.Kit("trade_eye_frame") ?? MythUi.Kit("bar_hp_trough"), MythUi.TL, MythUi.TL, Vector2.zero, new Vector2(96, 10));
                drinkBarFill = new MythClipFill("Fill", drinkBarRoot, MythUi.Kit("bar_stamina_fill_bright"), MythUi.TL, MythUi.TL, new Vector2(3, -2), 90, 6);
                var lblRt = MythUi.Rect("Label", drinkBarRoot, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(94, 10));
                var t = lblRt.gameObject.AddComponent<TextMeshProUGUI>();
                t.text = "Bình Hồi Lực (1s)";
                t.fontSize = 6.5f;
                t.fontStyle = FontStyles.Bold;
                t.alignment = TextAlignmentOptions.Center;
                t.color = Color.white;
                t.raycastTarget = false;
            }
            if (!drinkBarRoot.gameObject.activeSelf) drinkBarRoot.gameObject.SetActive(true);
            float ratio = Mathf.Clamp01((Time.unscaledTime - drinkStartTime) / Mathf.Max(0.1f, QuickBag.DrinkSeconds));
            drinkBarFill.Set(ratio);
        }

        void SyncTempShadesInWorld(GameEngine engine)
        {
            var shades = engine?.GridZones?.TempShades;
            int targetCount = (shades != null && string.Equals(engine.WorldLocations.CurrentLocationId, "Farm", System.StringComparison.OrdinalIgnoreCase))
                ? shades.Count : 0;
            while (tempShadeObjects.Count > targetCount)
            {
                int last = tempShadeObjects.Count - 1;
                if (tempShadeObjects[last] != null) Destroy(tempShadeObjects[last]);
                tempShadeObjects.RemoveAt(last);
            }
            if (targetCount == 0) return;

            var canopySpr = UISpriteLoader.GetTempShadeCanopySprite();
            var circleSpr = UISpriteLoader.GetSoftCircleSprite();
            for (int i = 0; i < targetCount; i++)
            {
                var (sx, sy) = shades[i];
                GameObject go = i < tempShadeObjects.Count ? tempShadeObjects[i] : null;
                if (go == null)
                {
                    go = new GameObject($"TempShade_Canopy_{i}");
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = canopySpr;
                    sr.sortingOrder = Mathf.RoundToInt(-sy * 100f) + 2;

                    var aura = new GameObject("CoolRadiusAura");
                    aura.transform.SetParent(go.transform, false);
                    float d = FarmGridAndZones.TempShadeRadiusMeters * 2f;
                    aura.transform.localScale = new Vector3(d, d * 0.72f, 1f);
                    var asr = aura.AddComponent<SpriteRenderer>();
                    asr.sprite = circleSpr;
                    asr.color = new Color(0.28f, 0.76f, 0.68f, 0.18f);
                    asr.sortingOrder = -32000;

                    if (i < tempShadeObjects.Count) tempShadeObjects[i] = go;
                    else tempShadeObjects.Add(go);
                }
                go.transform.position = new Vector3(sx, sy, 0f);
            }
        }

        void UpdatePigCrisisIndicators(GameEngine engine)
        {
            if (engine == null || engine.Pigs == null) return;
            Pig urgentPig = null;
            int bestPriority = 0; // 3 = SeizurePending, 2 = SayNang, 1 = Quá nhiệt >= 1h

            var views = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                var pv = views[i];
                if (pv == null || pv.PigModel == null) continue;
                var p = pv.PigModel;
                int prio = 0;
                string iconStem = null;
                if (p.IsAlive && p.SeizurePending) { prio = 3; iconStem = "loi_tam_spark"; }
                else if (p.IsAlive && p.ActiveDisease == DiseaseId.SayNang) { prio = 2; iconStem = "say_nang"; }
                else if (p.IsAlive && engine.UncooledHeatHours(p.Id) >= 1f) { prio = 1; iconStem = "qua_nhiet"; }

                var badgeTr = pv.transform.Find("ArgonCrisisBadge");
                if (prio == 0)
                {
                    if (badgeTr != null && badgeTr.gameObject.activeSelf) badgeTr.gameObject.SetActive(false);
                    continue;
                }
                if (prio > bestPriority) { bestPriority = prio; urgentPig = p; }
                if (badgeTr == null)
                {
                    var bGo = new GameObject("ArgonCrisisBadge");
                    bGo.transform.SetParent(pv.transform, false);
                    bGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                    var bSr = bGo.AddComponent<SpriteRenderer>();
                    bSr.sortingOrder = 30000;
                    badgeTr = bGo.transform;
                }
                if (!badgeTr.gameObject.activeSelf) badgeTr.gameObject.SetActive(true);
                float bob = 1.12f + 0.08f * Mathf.Sin(Time.unscaledTime * 8f);
                badgeTr.localPosition = new Vector3(0f, bob, 0f);
                var sr = badgeTr.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = MythUi.Icon("status", iconStem, 16);
            }

            if (urgentPig == null)
            {
                for (int i = 0; i < engine.Pigs.Count; i++)
                {
                    var p = engine.Pigs[i];
                    if (p == null || !p.IsAlive) continue;
                    int prio = p.SeizurePending ? 3 : (p.ActiveDisease == DiseaseId.SayNang ? 2 : (engine.UncooledHeatHours(p.Id) >= 1f ? 1 : 0));
                    if (prio > bestPriority) { bestPriority = prio; urgentPig = p; }
                }
            }

            if (urgentPig == null)
            {
                if (crisisAlertRoot != null && crisisAlertRoot.gameObject.activeSelf) crisisAlertRoot.gameObject.SetActive(false);
                return;
            }

            if (crisisAlertRoot == null)
            {
                crisisAlertRoot = MythUi.Rect("PigCrisisPointer", safeRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(216f, 20f));
                var bg = crisisAlertRoot.gameObject.AddComponent<Image>();
                bg.sprite = UISpriteLoader.GetFrameSlotSelected() ?? UISpriteLoader.GetFrameSlotNormal();
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.46f, 0.11f, 0.10f, 0.96f);
                bg.raycastTarget = true;
                crisisAlertRoot.gameObject.AddComponent<MythTap>().OnTap = () =>
                {
                    var eng = Engine;
                    if (eng == null || string.IsNullOrEmpty(crisisTargetPigId)) return;
                    var found = eng.Pigs.Find(x => x.Id == crisisTargetPigId);
                    if (found != null) PigInspectPopup.Instance?.Show(found);
                };
                var txtRt = MythUi.Rect("Text", crisisAlertRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                txtRt.offsetMin = new Vector2(6f, 0f);
                txtRt.offsetMax = new Vector2(-6f, 0f);
                crisisAlertText = txtRt.gameObject.AddComponent<TextMeshProUGUI>();
                crisisAlertText.fontSize = 8f;
                crisisAlertText.fontStyle = FontStyles.Bold;
                crisisAlertText.alignment = TextAlignmentOptions.Center;
                crisisAlertText.color = new Color(1f, 0.92f, 0.60f);
                crisisAlertText.raycastTarget = false;
            }
            if (!crisisAlertRoot.gameObject.activeSelf) crisisAlertRoot.gameObject.SetActive(true);
            crisisTargetPigId = urgentPig.Id;

            string dirArrow = "";
            var player = PlayerMobileController.Instance;
            if (player != null && (urgentPig.PositionX != 0f || urgentPig.PositionY != 0f))
            {
                Vector2 delta = new Vector2(urgentPig.PositionX, urgentPig.PositionY) - (Vector2)player.transform.position;
                if (delta.sqrMagnitude > 4f)
                {
                    float ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    dirArrow = ang >= -22.5f && ang < 22.5f ? " [→]"
                             : ang >= 22.5f && ang < 67.5f ? " [↗]"
                             : ang >= 67.5f && ang < 112.5f ? " [↑]"
                             : ang >= 112.5f && ang < 157.5f ? " [↖]"
                             : (ang >= 157.5f || ang < -157.5f) ? " [←]"
                             : ang >= -157.5f && ang < -112.5f ? " [↙]"
                             : ang >= -112.5f && ang < -67.5f ? " [↓]" : " [↘]";
                }
            }
            string crisisLabel = bestPriority == 3 ? $"KHẨN: {urgentPig.Name} RUN GIẬT LÔI TÂM!{dirArrow}"
                               : bestPriority == 2 ? $"KHẨN: {urgentPig.Name} GỤC SAY NẮNG!{dirArrow}"
                               : $"CẢNH BÁO: {urgentPig.Name} quá nhiệt!{dirArrow}";
            crisisAlertText.text = crisisLabel;
        }

        void UpdateFaintOverlay(bool fainted)
        {
            if (!fainted)
            {
                if (faintOverlayRoot != null && faintOverlayRoot.gameObject.activeSelf) faintOverlayRoot.gameObject.SetActive(false);
                return;
            }
            if (faintOverlayRoot == null)
            {
                faintOverlayRoot = MythUi.Rect("FaintOverlay", safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                faintOverlayRoot.offsetMin = faintOverlayRoot.offsetMax = Vector2.zero;
                var dim = faintOverlayRoot.gameObject.AddComponent<Image>();
                dim.color = new Color(0.04f, 0.05f, 0.08f, 0.48f);
                dim.raycastTarget = false;
                var banner = MythUi.Rect("Banner", faintOverlayRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(320f, 36f));
                var bImg = banner.gameObject.AddComponent<Image>();
                bImg.sprite = UISpriteLoader.GetFrameSlotSelected() ?? UISpriteLoader.GetFrameSlotNormal();
                bImg.type = Image.Type.Sliced;
                bImg.color = new Color(0.28f, 0.10f, 0.10f, 0.94f);
                bImg.raycastTarget = false;
                var txtRt = MythUi.Rect("Text", banner, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
                var t = txtRt.gameObject.AddComponent<TextMeshProUGUI>();
                t.text = "<b>Bất tỉnh — tỉnh lại khi hết đợt quái</b>";
                t.fontSize = 11f;
                t.alignment = TextAlignmentOptions.Center;
                t.color = new Color(1f, 0.88f, 0.78f);
                t.raycastTarget = false;
            }
            if (!faintOverlayRoot.gameObject.activeSelf) faintOverlayRoot.gameObject.SetActive(true);
        }

        void UpdateActions(GameEngine engine)
        {
            bool fainted = engine.Character != null && engine.Character.IsFainted;
            UpdateFaintOverlay(fainted);
            bool inCombat = !fainted && (engine.Clock.IsDuringCombatWave || (engine.Carry != null && engine.Carry.InCombat) || HasNearbyHostile());
            if (attackButton != null && attackButton.gameObject.activeSelf == fainted) attackButton.gameObject.SetActive(!fainted);
            if (dodgeButton != null && dodgeButton.gameObject.activeSelf == fainted) dodgeButton.gameObject.SetActive(!fainted);
            if (contextButton != null && contextButton.gameObject.activeSelf == fainted) contextButton.gameObject.SetActive(!fainted);
            if (defendButton != null && defendButton.gameObject.activeSelf != inCombat) defendButton.gameObject.SetActive(inCombat);
            if (burstButton != null && burstButton.gameObject.activeSelf != inCombat) burstButton.gameObject.SetActive(inCombat);
            if (counterButton != null && counterButton.gameObject.activeSelf != inCombat) counterButton.gameObject.SetActive(inCombat);
            bool alive = engine.Character != null && !fainted && PlayerMobileController.Instance != null;
            UpdateSkillSlot(engine, attackButton, congLabel, CombatSlotType.TheCong, 0, alive);
            if (inCombat)
            {
                UpdateSkillSlot(engine, defendButton, thuLabel, CombatSlotType.TheThu, 1, alive);
                UpdateSkillSlot(engine, burstButton, bienLabel, CombatSlotType.TheBien, 2, alive);
            }
            bool songThe = inCombat && engine.Character.IsMartialSlotUnlocked(CombatSlotType.TheSongThe);
            if (songTheButton != null && songTheButton.gameObject.activeSelf != songThe) songTheButton.gameObject.SetActive(songThe);
            if (songThe) UpdateSkillSlot(engine, songTheButton, songTheLabel, CombatSlotType.TheSongThe, 3, alive);
            if (alive && !UISpriteLoader.IsAnyModalOrDialogueOpen())
            {
                if (Input.GetKeyDown(KeyCode.Z)) Skill(CombatSlotType.TheCong);
                if (Input.GetKeyDown(KeyCode.X)) Skill(CombatSlotType.TheThu);
                if (Input.GetKeyDown(KeyCode.V)) Skill(CombatSlotType.TheBien);
                if (Input.GetKeyDown(KeyCode.R) && songThe) Skill(CombatSlotType.TheSongThe);
            }
            if (Input.GetKeyDown(KeyCode.C) && !UISpriteLoader.IsAnyModalOrDialogueOpen()) CharacterPanel.Toggle();
            whistleButton.SetEnabled(alive && PixelMythFarmGameplay.Instance != null);
            if (alive && PixelMythFarmGameplay.Instance != null && !UISpriteLoader.IsAnyModalOrDialogueOpen() && Input.GetKeyDown(KeyCode.G)) HerdRecall();
            if (observeButton != null)
            {
                bool nearPig = contextTarget is PigAgentView;
                if (observeButton.gameObject.activeSelf != nearPig) observeButton.gameObject.SetActive(nearPig);
                if (nearPig) observeButton.SetEnabled(alive);
            }

            bool hasInteract = HasInteractTarget();
            contextButton.SetEnabled(alive);
            contextButton.SetReady(hasInteract);
            contextButton.SetKeyHint("");
            // Icon-first rule: only show a context prompt pill when standing right next to an interactable target
            contextButton.SetBadgeLabel(hasInteract ? ContextLabel() + " [E]" : string.Empty);
        }

        bool HasNearbyHostile()
        {
            var p = PlayerMobileController.Instance;
            if (p == null) return false;
            var enemies = FindObjectsByType<EnemyAgentView>(FindObjectsSortMode.None);
            Vector2 pos = p.transform.position;
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i] != null && enemies[i].isActiveAndEnabled && Vector2.Distance(pos, enemies[i].transform.position) <= 10f)
                    return true;
            return false;
        }

        bool HasInteractTarget() =>
            contextTarget != null || nearNpc != null || nearBrokenFence != null || nearBuilding != null || nearMysticMerchant != null;

        // ---------------------------------------------------------------- context medallion
        void ScanInteraction()
        {
            var pic = PlayerInteractionController.Instance;
            Component target = pic != null ? pic.CurrentTarget : null;
            nearBuilding = null; nearNpc = null; nearBrokenFence = null; nearMysticMerchant = null;
            var player = PlayerMobileController.Instance;
            if (player != null)
            {
                if (Time.unscaledTime >= nextBuildingRefresh)
                {
                    nextBuildingRefresh = Time.unscaledTime + 1f;
                    buildings = FindObjectsByType<CustomBuildingController>(FindObjectsSortMode.None);
                    fences = FindObjectsByType<PixelFenceSegment>(FindObjectsSortMode.None);
                    if (mysticMerchant == null) mysticMerchant = FindFirstObjectByType<MysticMerchantView>();
                }
                Vector2 pos = player.transform.position;
                nearNpc = PixelMythNpc.NearestTo(pos, PixelMythNpc.InteractRange + 0.35f);
                if (target == null)
                {
                    float bestFenceD = 2.2f * 2.2f;
                    for (int i = 0; i < fences.Length; i++)
                    {
                        var f = fences[i];
                        if (f == null || (!f.IsBroken && !f.IsTemporaryPatch && f.CurrentHp >= f.MaxHp)) continue;
                        float d = ((Vector2)f.transform.position - pos).sqrMagnitude;
                        if (d < bestFenceD) { bestFenceD = d; nearBrokenFence = f; }
                    }
                    for (int i = 0; i < buildings.Length; i++)
                    {
                        var b = buildings[i];
                        if (b != null && b.isActiveAndEnabled && b.IsPlayerInRange(pos)) { nearBuilding = b; break; }
                    }
                    var engine = Engine;
                    if (mysticMerchant != null && engine != null && engine.Economy.MysticMerchant.IsActive &&
                        Vector2.Distance(pos, mysticMerchant.transform.position) <= 1.85f)
                    {
                        nearMysticMerchant = mysticMerchant;
                    }
                }
            }
            contextTarget = target;
            contextButton.SetIcon(ContextIcon());
        }

        Sprite ContextIcon()
        {
            if (contextTarget != null)
            {
                switch (contextTarget)
                {
                    case PigAgentView _: return MythUi.Icon("actions", "giam_dinh", 24);
                    case WaterTrough2DView _:
                    case WaterWell2DView _: return MythUi.Icon("actions", "cho_uong", 24);
                    case Feeder2DView _:
                    case FeedSilo2DView _: return MythUi.Icon("actions", "cho_an", 24);
                    case Fence2DView _: return MythUi.Icon("actions", "sua", 24);
                    case DefenseTower2DView _: return MythUi.Icon("actions", "nap_dan", 24);
                    case CorpseLot2DView _: return MythUi.Icon("places", "khu_xu_ly", 24);
                    default: return MythUi.Icon("actions", "sua", 24);
                }
            }
            if (nearNpc != null || nearMysticMerchant != null) return MythUi.Icon("actions", "trao_doi", 24);
            if (nearBrokenFence != null) return MythUi.Icon("actions", "sua", 24);
            if (nearBuilding != null)
            {
                switch (nearBuilding.AssignedFunction)
                {
                    case BuildingInteractionFunction.GiaoDichDanLang: return MythUi.Icon("actions", "trao_doi", 24);
                    case BuildingInteractionFunction.MoKhoLuuTru: return MythUi.Icon("places", "kho", 24);
                    case BuildingInteractionFunction.NghiNgoiGiuong: return MythUi.Icon("actions", "ngu", 24);
                    case BuildingInteractionFunction.DocBangTinLore:
                    case BuildingInteractionFunction.BanTinhBao: return MythUi.Icon("misc", "tinh_bao", 24);
                    default: return MythUi.Icon("actions", "vao_cua", 24);
                }
            }
            return MythUi.Icon("actions", "danh", 24);
        }

        string ContextLabel()
        {
            if (contextTarget != null)
            {
                switch (contextTarget)
                {
                    case PigAgentView _: return "Chăm heo";
                    case WaterTrough2DView _: return "Đổ nước";
                    case WaterWell2DView _: return "Múc nước";
                    case Feeder2DView _: return "Cho ăn";
                    case FeedSilo2DView _: return "Lấy cám";
                    case Fence2DView _: return "Sửa rào";
                    case DefenseTower2DView _: return "Nạp đạn";
                    case CorpseLot2DView _: return "Xử lý";
                    default: return "Tương tác";
                }
            }
            if (nearNpc != null) return !string.IsNullOrEmpty(nearNpc.DisplayName) ? nearNpc.DisplayName : "Trò chuyện";
            if (nearMysticMerchant != null) return "Bí nhân";
            if (nearBrokenFence != null) return nearBrokenFence.IsBroken ? "Rào tạm" : "Sửa rào";
            if (nearBuilding != null)
            {
                switch (nearBuilding.AssignedFunction)
                {
                    case BuildingInteractionFunction.GiaoDichDanLang: return "Giao dịch";
                    case BuildingInteractionFunction.MoKhoLuuTru: return "Mở kho";
                    case BuildingInteractionFunction.NghiNgoiGiuong: return "Nghỉ ngơi";
                    case BuildingInteractionFunction.DocBangTinLore:
                    case BuildingInteractionFunction.BanTinhBao: return "Tình báo";
                    default: return "Vào nhà";
                }
            }
            return "Tấn công";
        }

        // Tool verb for the context target (the old ContextActionPalette kept a copy of this mapping).
        static StardewToolType Verb(Component target)
        {
            switch (target)
            {
                case PigAgentView _: return StardewToolType.BanChai;
                case WaterTrough2DView _:
                case WaterWell2DView _: return StardewToolType.XoNuoc;
                case Feeder2DView _:
                case FeedSilo2DView _: return StardewToolType.CamHat;
                default: return StardewToolType.BuaGo;
            }
        }

        void OnContextTapped()
        {
            if (UISpriteLoader.IsAnyModalOrDialogueOpen()) return;
            var pic = PlayerInteractionController.Instance;
            var player = PlayerMobileController.Instance;
            if (contextTarget != null && pic != null)
            {
                // Identical to the old primary button: equip the verb, then PerformAction.
                if (HotbarController.Instance != null) HotbarController.Instance.EquipTool(Verb(contextTarget));
                pic.PerformAction();
            }
            else if (nearNpc != null && player != null)
            {
                nearNpc.Talk(player.transform.position);
            }
            else if (nearBrokenFence != null && player != null)
            {
                if (PixelFenceSegment.TryPlayerFix(player.transform.position, out var msg) && !string.IsNullOrEmpty(msg))
                    pic?.ShowFeedback(msg);
            }
            else if (nearMysticMerchant != null)
            {
                var popup = VillageBarterPopup.Instance;
                if (popup == null)
                {
                    var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
                    if (canvas != null) popup = canvas.gameObject.AddComponent<VillageBarterPopup>();
                }
                popup?.OpenForVillager(VillagerId.BiNhan);
            }
            else if (nearBuilding != null) nearBuilding.ExecuteInteraction(player);
            else
            {
                // Fallback primary action: equip weapon and strike!
                if (HotbarController.Instance != null) HotbarController.Instance.EquipTool(StardewToolType.DaoGo);
                PlayerCombatController.Instance?.ExecuteAttack();
            }
        }
    }
}
