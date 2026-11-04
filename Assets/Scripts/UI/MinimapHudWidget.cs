using System;
using System.Collections.Generic;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Interactive 2.5D Minimap Radar (Top-Right HUD), Full 920x560 Tactical World Map Modal,
    /// Interactive 4-Daypart & Weather Forecast Popover (Top-Left Sundial click), and
    /// Living World 4-Daypart/Weather/Building/Footstep Animator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapHudWidget : MonoBehaviour
    {
        public static MinimapHudWidget Instance { get; private set; }

        public static bool IsWorldMapModalOpen =>
            Instance != null && Instance.worldMapModal != null && Instance.worldMapModal.activeSelf;

        public static bool IsWeatherPanelOpen =>
            Instance != null && Instance.weatherPopover != null && Instance.weatherPopover.activeSelf;

        private static readonly Color Gold = new Color(0.957f, 0.824f, 0.478f, 1f);
        private static readonly Color Ivory = new Color(0.98f, 0.94f, 0.84f, 1f);

        private RectTransform hostSafeRoot;
        private Canvas overlayModalCanvas;
        private Canvas atmosphereCanvas;

        // --- 1. Compact Top-Right 2.5D Minimap Radar (108x82 at (-4, -84) on safeRoot) ---
        private RectTransform radarRoot;
        private Image radarMapImage;
        private RectTransform radarViewport;
        private RectTransform radarMarkerLayer;
        private RectTransform radarFogRoot;
        private TextMeshProUGUI radarZoneText;
        private TextMeshProUGUI radarFogBadgeText;
        private RectTransform playerChevron;
        private readonly List<Image> radarBlipPool = new List<Image>();

        // --- 2. Full Interactive 920x560 World Map & Tactical Command Modal ---
        private GameObject worldMapModal;
        private RectTransform fullMapViewport;
        private Image fullMapImage;
        private RectTransform fullMapMarkerLayer;
        private TextMeshProUGUI modalTelemetryText;
        private TextMeshProUGUI modalSelectionTitle;
        private TextMeshProUGUI modalSelectionBody;
        private TextMeshProUGUI modalSelectionActionLabel;
        private Action pendingSelectionAction;
        private readonly List<GameObject> fullMapMarkerObjects = new List<GameObject>();
        private readonly Dictionary<string, Image> zoneCardBgs = new Dictionary<string, Image>();
        private readonly Dictionary<string, TextMeshProUGUI> zoneCardSubTexts = new Dictionary<string, TextMeshProUGUI>();

        // --- 3. Interactive 4-Daypart & Weather Forecast Popover ---
        private GameObject weatherPopover;
        private RectTransform popoverNeedlePreview;
        private Image popoverWeatherIcon;
        private TextMeshProUGUI popoverClockTitle;
        private TextMeshProUGUI popoverDaypartRuleText;
        private TextMeshProUGUI popoverWeatherTitle;
        private TextMeshProUGUI popoverWeatherEffectsText;
        private TextMeshProUGUI popoverStatusPillsText;
        private readonly Image[] daypartChipBgs = new Image[4];
        private readonly TextMeshProUGUI[] daypartChipTexts = new TextMeshProUGUI[4];

        // --- 4. Living World Atmosphere, Building & Grounding Animator ---
        private Image atmosphereOverlay;
        private Image atmosphereCornerGlow;
        private float nextRadarSampleTime;
        private float nextWorldAnimTime;
        private float footstepTimer;
        private Vector3 lastPlayerPos;
        private bool hasLastPlayerPos;
        private Sprite cachedMapSprite;
        private Sprite cachedWhiteSprite;
        private Sprite cachedCircleSprite;
        private Sprite cachedDiamondSprite;
        private Sprite cachedChevronSprite;

        private static readonly (TimeOfDay part, string title, string hours, Color tint)[] DaypartMeta =
        {
            (TimeOfDay.Sang,  "SÁNG",  "06:00–10:00", new Color(0.95f, 0.62f, 0.46f, 1f)),
            (TimeOfDay.Trua,  "TRƯA",  "10:00–14:00", new Color(0.32f, 0.68f, 0.94f, 1f)),
            (TimeOfDay.Chieu, "CHIỀU", "14:00–18:00", new Color(0.92f, 0.42f, 0.24f, 1f)),
            (TimeOfDay.Toi,   "ĐÊM",   "18:00–06:00", new Color(0.22f, 0.28f, 0.64f, 1f)),
        };

        private static readonly (string locId, string sceneName, string title, string subtitle)[] ZoneCardsMeta =
        {
            ("Farm", "Farm_Main", "Nông Trại Chính", "Đồng cỏ chăn thả, chuồng trại & phòng thủ"),
            ("MarketTown", "Market_Town", "Thị Trấn Chợ Cảng", "Chợ Ngày (06–18h) · Đấu giá & Chợ Đêm"),
            ("Hub_Village", "Hub_Village", "Làng Bờ Rìa (Làng Đông)", "Giao thương dân làng, Quán Bà Sáu & rèn đúc"),
            ("Forest_Primeval", "Forest_Primeval", "Rừng Nguyên Sinh", "Thám hiểm cổ địa, thảo dược & dị tượng"),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (Instance != null) return;
            var hud = FindFirstObjectByType<MythHudController>();
            if (hud != null) return; // MythHudController.Awake calls Install(safeRoot)
            if (FindFirstObjectByType<PixelWorldScene>() != null)
            {
                var go = new GameObject("MinimapHudWidget");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<MinimapHudWidget>();
            }
        }

        public static MinimapHudWidget Install(RectTransform safeRoot)
        {
            if (Instance == null)
            {
                var go = new GameObject("MinimapHudWidget");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<MinimapHudWidget>();
            }
            if (safeRoot != null)
            {
                Instance.AttachToSafeRoot(safeRoot);
            }
            return Instance;
        }

        public static void ToggleWorldMapModal()
        {
            if (Instance == null) return;
            Instance.SetWorldMapModalOpen(!IsWorldMapModalOpen);
        }

        public static void ToggleWeatherDaypartPanel()
        {
            if (Instance == null) return;
            Instance.SetWeatherPopoverOpen(!IsWeatherPanelOpen);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureProceduralSprites();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void AttachToSafeRoot(RectTransform safeRoot)
        {
            if (safeRoot == null) return;
            if (hostSafeRoot == safeRoot && radarRoot != null) return;

            hostSafeRoot = safeRoot;
            EnsureProceduralSprites();

            if (radarRoot != null) Destroy(radarRoot.gameObject);
            if (weatherPopover != null) Destroy(weatherPopover);
            radarBlipPool.Clear();

            EnsureAtmosphereCanvas();
            EnsureOverlayModalCanvas();
            BuildCompactMinimapRadar(hostSafeRoot);
            BuildWeatherDaypartPopover(hostSafeRoot);

            RefreshRadarMarkers();
            UpdateWeatherPopoverContent();
        }

        private void EnsureProceduralSprites()
        {
            if (cachedWhiteSprite == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
                tex.Apply();
                cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 32f);
            }
            if (cachedCircleSprite == null)
            {
                int sz = 12;
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                float c = (sz - 1) * 0.5f;
                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        float r = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                        if (r > 5.4f) tex.SetPixel(x, y, Color.clear);
                        else if (r > 4.1f) tex.SetPixel(x, y, new Color(0.08f, 0.05f, 0.04f, 0.95f));
                        else tex.SetPixel(x, y, Color.white);
                    }
                }
                tex.Apply();
                cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 32f);
            }
            if (cachedDiamondSprite == null)
            {
                int sz = 12;
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                float c = (sz - 1) * 0.5f;
                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        float d = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                        if (d > 5.5f) tex.SetPixel(x, y, Color.clear);
                        else if (d > 4.2f) tex.SetPixel(x, y, new Color(0.08f, 0.05f, 0.04f, 0.95f));
                        else tex.SetPixel(x, y, Color.white);
                    }
                }
                tex.Apply();
                cachedDiamondSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 32f);
            }
            if (cachedChevronSprite == null)
            {
                int sz = 14;
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                for (int y = 0; y < sz; y++)
                    for (int x = 0; x < sz; x++)
                        tex.SetPixel(x, y, Color.clear);

                float cx = 6.5f;
                for (int y = 2; y <= 11; y++)
                {
                    float halfW = (11 - y) * 0.52f;
                    for (int x = 1; x < sz - 1; x++)
                    {
                        float dx = Mathf.Abs(x - cx);
                        if (dx <= halfW + 1.0f)
                        {
                            bool notch = y <= 4 && dx < 1.6f;
                            if (notch) continue;
                            bool border = dx >= halfW || y == 11 || y == 2;
                            tex.SetPixel(x, y, border
                                ? new Color(0.10f, 0.06f, 0.04f, 1f)
                                : (x < cx ? new Color(1f, 0.96f, 0.72f, 1f) : new Color(0.95f, 0.74f, 0.24f, 1f)));
                        }
                    }
                }
                tex.Apply();
                cachedChevronSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 32f);
            }
            if (cachedMapSprite == null)
            {
                cachedMapSprite = UISpriteLoader.LoadSprite("Art/PixelMyth/Maps/farm_preview_crop_x3.png", 32)
                               ?? UISpriteLoader.LoadSprite("Art/PixelMyth/Maps/farm_preview.png", 32);
            }
        }

        private static TextMeshProUGUI MakeLabel(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions align,
            Color color,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 pos,
            Vector2 size,
            bool bold = true)
        {
            var rt = MythUi.Rect(name, parent, anchor, pivot, pos, size);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = align;
            tmp.color = color;
            tmp.outlineWidth = 0.26f;
            tmp.outlineColor = new Color32(16, 10, 8, 255);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void ApplySlicedFrame(Image img, Sprite frameSpr, Color tint)
        {
            if (img == null) return;
            img.sprite = frameSpr;
            img.enabled = frameSpr != null;
            if (frameSpr != null && frameSpr.border != Vector4.zero)
                img.type = Image.Type.Sliced;
            img.color = tint;
        }

        // =====================================================================
        // 1. LIVING ATMOSPHERE CANVAS (4-Daypart & Weather Directional Light)
        // =====================================================================
        private void EnsureAtmosphereCanvas()
        {
            if (atmosphereCanvas != null) return;
            var go = new GameObject("LivingWorldAtmosphereCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            atmosphereCanvas = go.GetComponent<Canvas>();
            atmosphereCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            atmosphereCanvas.sortingOrder = -20; // Underneath HUD and popups, pure visual tint

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(768f, 432f);
            scaler.matchWidthOrHeight = 0.5f;

            var fullRt = MythUi.Rect("AtmosphereTint", go.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            fullRt.anchorMin = Vector2.zero;
            fullRt.anchorMax = Vector2.one;
            fullRt.offsetMin = fullRt.offsetMax = Vector2.zero;
            atmosphereOverlay = fullRt.gameObject.AddComponent<Image>();
            atmosphereOverlay.sprite = cachedWhiteSprite;
            atmosphereOverlay.color = new Color(0f, 0f, 0f, 0f);
            atmosphereOverlay.raycastTarget = false;

            var glowRt = MythUi.Rect("TopDirectionalSunMoonGlow", fullRt, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            glowRt.anchorMin = new Vector2(0f, 0.68f);
            glowRt.anchorMax = new Vector2(1f, 1f);
            glowRt.offsetMin = glowRt.offsetMax = Vector2.zero;
            atmosphereCornerGlow = glowRt.gameObject.AddComponent<Image>();
            atmosphereCornerGlow.sprite = cachedWhiteSprite;
            atmosphereCornerGlow.color = new Color(1f, 0.88f, 0.55f, 0f);
            atmosphereCornerGlow.raycastTarget = false;
        }

        // =====================================================================
        // 2. COMPACT TOP-RIGHT 2.5D MINIMAP RADAR (108x82 at (-4, -84))
        // =====================================================================
        private void BuildCompactMinimapRadar(RectTransform parent)
        {
            radarRoot = MythUi.Rect("MinimapRadar25D", parent, MythUi.TR, MythUi.TR, new Vector2(-4f, -84f), new Vector2(108f, 82f));

            var outerBg = radarRoot.gameObject.AddComponent<Image>();
            ApplySlicedFrame(outerBg, UISpriteLoader.GetFrameWoodPanel() ?? cachedWhiteSprite, new Color(0.14f, 0.12f, 0.18f, 0.96f));
            outerBg.raycastTarget = true;

            var innerRim = MythUi.Rect("BronzeRim", radarRoot, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(104f, 78f));
            var rimImg = innerRim.gameObject.AddComponent<Image>();
            ApplySlicedFrame(rimImg, UISpriteLoader.GetFrameSlotSelected() ?? UISpriteLoader.GetFrameSlotNormal() ?? cachedWhiteSprite, new Color(0.92f, 0.72f, 0.32f, 0.45f));
            rimImg.raycastTarget = false;

            // Header bar: Zone badge + [M] keycap badge
            radarZoneText = MakeLabel(radarRoot, "ZoneText", "Nông Trại", 9.5f, TextAlignmentOptions.MidlineLeft,
                Gold, MythUi.TL, MythUi.TL, new Vector2(6f, -3f), new Vector2(76f, 14f));
            radarZoneText.textWrappingMode = TextWrappingModes.NoWrap;

            var keyBadge = MythUi.Rect("KeyBadgeM", radarRoot, MythUi.TR, MythUi.TR, new Vector2(-5f, -3f), new Vector2(18f, 13f));
            var keyBg = keyBadge.gameObject.AddComponent<Image>();
            ApplySlicedFrame(keyBg, UISpriteLoader.GetFrameSlotNormal() ?? cachedWhiteSprite, new Color(0.18f, 0.14f, 0.10f, 0.96f));
            keyBg.raycastTarget = false;
            MakeLabel(keyBadge, "KeyTxt", "M", 8.5f, TextAlignmentOptions.Center, Gold, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(18f, 13f));

            // Radar Viewport (98x60)
            radarViewport = MythUi.Rect("Viewport", radarRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(98f, 60f));
            var vpBg = radarViewport.gameObject.AddComponent<Image>();
            vpBg.sprite = cachedWhiteSprite;
            vpBg.color = new Color(0.13f, 0.24f, 0.15f, 1f);
            vpBg.raycastTarget = false;
            radarViewport.gameObject.AddComponent<RectMask2D>();

            radarMapImage = MythUi.Img("MapTexture", radarViewport, cachedMapSprite ?? cachedWhiteSprite,
                MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(98f, 60f));
            radarMapImage.color = cachedMapSprite != null ? Color.white : new Color(0.22f, 0.38f, 0.22f, 1f);

            // Fence guide inside radar
            var fenceGuide = MythUi.Img("FenceGuide", radarViewport, cachedWhiteSprite,
                MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(64f, 36f));
            fenceGuide.color = new Color(0.95f, 0.78f, 0.36f, 0.14f);

            // Marker Layer (BL-anchored so (0..vpSize.x, 0..vpSize.y) maps 1:1)
            radarMarkerLayer = MythUi.Rect("Markers", radarViewport, MythUi.BL, MythUi.BL, Vector2.zero, new Vector2(98f, 60f));

            // Fog of war overlay (LacHuong 15m radius)
            radarFogRoot = MythUi.Rect("LacHuongFog", radarViewport, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(98f, 60f));
            var fogImg = radarFogRoot.gameObject.AddComponent<Image>();
            fogImg.sprite = cachedWhiteSprite;
            fogImg.color = new Color(0.20f, 0.16f, 0.30f, 0.62f);
            fogImg.raycastTarget = false;
            radarFogBadgeText = MakeLabel(radarFogRoot, "FogTxt", "Sương Mù 15m", 8.0f, TextAlignmentOptions.Bottom,
                new Color(0.92f, 0.84f, 1f, 0.96f), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(94f, 56f));
            radarFogRoot.gameObject.SetActive(false);

            // Player Directional Chevron
            var chevImg = MythUi.Img("PlayerChevron", radarViewport, cachedChevronSprite,
                MythUi.BL, MythUi.MID, new Vector2(49f, 30f), new Vector2(11f, 11f));
            playerChevron = chevImg.rectTransform;

            // Tap radar -> Open Full 920x560 Tactical World Map Modal
            radarRoot.gameObject.AddComponent<MythTap>().OnTap = ToggleWorldMapModal;
        }

        // =====================================================================
        // 3. INTERACTIVE 4-DAYPART & WEATHER FORECAST POPOVER (Top-Left Sundial)
        // =====================================================================
        private void BuildWeatherDaypartPopover(RectTransform parent)
        {
            var popRt = MythUi.Rect("WeatherDaypartPopover", parent, MythUi.TL, MythUi.TL, new Vector2(4f, -72f), new Vector2(316f, 224f));
            weatherPopover = popRt.gameObject;

            var bg = popRt.gameObject.AddComponent<Image>();
            ApplySlicedFrame(bg, UISpriteLoader.GetFrameWoodPanel() ?? cachedWhiteSprite, new Color(0.11f, 0.09f, 0.15f, 0.97f));
            bg.raycastTarget = true;

            var borderRt = MythUi.Rect("GoldRim", popRt, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(310f, 218f));
            var borderImg = borderRt.gameObject.AddComponent<Image>();
            ApplySlicedFrame(borderImg, UISpriteLoader.GetFrameSlotSelected() ?? cachedWhiteSprite, new Color(0.90f, 0.72f, 0.32f, 0.35f));
            borderImg.raycastTarget = false;

            // Header bar
            popoverClockTitle = MakeLabel(popRt, "ClockTitle", "NHẬT NGUYỆT ĐÀI · 06:00 SÁNG", 10.0f,
                TextAlignmentOptions.MidlineLeft, Gold, MythUi.TL, MythUi.TL, new Vector2(8f, -6f), new Vector2(272f, 18f));

            CreateStyledButton(popRt, "CloseBtn", "✕", MythUi.TR, MythUi.TR, new Vector2(-6f, -5f), new Vector2(22f, 18f),
                new Color(0.58f, 0.20f, 0.18f, 0.95f), () => SetWeatherPopoverOpen(false));

            // 4-Daypart Wheel & Quadrant Strip
            var dialBox = MythUi.Rect("DialPreview", popRt, MythUi.TL, MythUi.TL, new Vector2(8f, -28f), new Vector2(44f, 44f));
            MythUi.Img("Base", dialBox, MythUi.Kit("sundial_base"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(44f, 44f));
            var needleImg = MythUi.Img("Needle", dialBox, MythUi.Kit("sundial_needle"), MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(44f, 44f));
            popoverNeedlePreview = needleImg.rectTransform;

            for (int i = 0; i < 4; i++)
            {
                var meta = DaypartMeta[i];
                float xOff = 56f + i * 63f;
                var chip = MythUi.Rect("Chip_" + meta.part, popRt, MythUi.TL, MythUi.TL, new Vector2(xOff, -28f), new Vector2(60f, 44f));
                var chipImg = chip.gameObject.AddComponent<Image>();
                ApplySlicedFrame(chipImg, UISpriteLoader.GetFrameSlotNormal() ?? cachedWhiteSprite,
                    new Color(meta.tint.r * 0.35f, meta.tint.g * 0.35f, meta.tint.b * 0.35f, 0.90f));
                daypartChipBgs[i] = chipImg;
                daypartChipTexts[i] = MakeLabel(chip, "Txt", $"{meta.title}\n{meta.hours}", 8.0f,
                    TextAlignmentOptions.Center, Ivory, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(58f, 40f));
            }

            // Active Daypart Rule Banner
            var ruleBox = MythUi.Rect("DaypartRule", popRt, MythUi.TL, MythUi.TL, new Vector2(8f, -75f), new Vector2(300f, 24f));
            var ruleBg = ruleBox.gameObject.AddComponent<Image>();
            ruleBg.sprite = cachedWhiteSprite;
            ruleBg.color = new Color(0.18f, 0.15f, 0.24f, 0.88f);
            popoverDaypartRuleText = MakeLabel(ruleBox, "RuleTxt", "", 8.2f, TextAlignmentOptions.MidlineLeft,
                Ivory, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(290f, 22f), false);

            // Current Weather Card + Active Environmental Effects
            var weatherCard = MythUi.Rect("WeatherCard", popRt, MythUi.TL, MythUi.TL, new Vector2(8f, -102f), new Vector2(300f, 62f));
            var wcBg = weatherCard.gameObject.AddComponent<Image>();
            wcBg.sprite = cachedWhiteSprite;
            wcBg.color = new Color(0.13f, 0.18f, 0.26f, 0.92f);

            popoverWeatherIcon = MythUi.Img("WeatherIcon", weatherCard, MythUi.Icon("weather", "quang_dang", 24),
                MythUi.TL, MythUi.TL, new Vector2(6f, -6f), new Vector2(24f, 24f));

            popoverWeatherTitle = MakeLabel(weatherCard, "WeatherTitle", "Thời tiết: Quang Đãng", 9.5f,
                TextAlignmentOptions.TopLeft, Gold, MythUi.TL, MythUi.TL, new Vector2(36f, -4f), new Vector2(258f, 15f));

            popoverWeatherEffectsText = MakeLabel(weatherCard, "WeatherEffects", "", 8.0f,
                TextAlignmentOptions.TopLeft, Ivory, MythUi.TL, MythUi.TL, new Vector2(36f, -19f), new Vector2(258f, 26f), false);

            popoverStatusPillsText = MakeLabel(weatherCard, "StatusPills", "", 8.0f,
                TextAlignmentOptions.BottomLeft, new Color(0.55f, 0.94f, 0.68f, 1f), MythUi.BL, MythUi.BL, new Vector2(6f, 3f), new Vector2(288f, 14f));

            // 4 Direct Interactive Action Buttons (2x2 Grid at bottom)
            CreateStyledButton(popRt, "BtnRest1h", "Nghỉ Ngơi +1 Giờ", MythUi.BL, MythUi.BL, new Vector2(8f, 31f), new Vector2(147f, 22f),
                new Color(0.22f, 0.36f, 0.52f, 0.96f), OnClickRestOneHour);
            CreateStyledButton(popRt, "BtnTempShade", "Dựng Mái Che Tạm", MythUi.BR, MythUi.BR, new Vector2(-8f, 31f), new Vector2(147f, 22f),
                new Color(0.24f, 0.44f, 0.34f, 0.96f), OnClickBuildTempShade);
            CreateStyledButton(popRt, "BtnSleepHouse", "Vào Nhà Chính Ngủ", MythUi.BL, MythUi.BL, new Vector2(8f, 6f), new Vector2(147f, 22f),
                new Color(0.42f, 0.30f, 0.54f, 0.96f), OnClickEnterMainHouseSleep);
            CreateStyledButton(popRt, "BtnIntelWave", "Dự Báo Đợt Quái", MythUi.BR, MythUi.BR, new Vector2(-8f, 6f), new Vector2(147f, 22f),
                new Color(0.54f, 0.32f, 0.20f, 0.96f), OnClickOpenWaveIntel);

            weatherPopover.SetActive(false);
        }

        // =====================================================================
        // 4. FULL INTERACTIVE 920x560 2.5D WORLD MAP & TACTICAL COMMAND MODAL
        // =====================================================================
        private void EnsureOverlayModalCanvas()
        {
            if (overlayModalCanvas != null) return;
            var go = new GameObject("WorldMapModalCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            overlayModalCanvas = go.GetComponent<Canvas>();
            overlayModalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayModalCanvas.sortingOrder = 118; // Above HUD, matches other tactical popups

            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1024f, 600f);
            sc.matchWidthOrHeight = 0.5f;

            BuildFullWorldMapModal(go.transform as RectTransform);
        }

        private void BuildFullWorldMapModal(RectTransform canvasRt)
        {
            var modalRt = MythUi.Rect("WorldMapTacticalModal", canvasRt, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            modalRt.anchorMin = Vector2.zero;
            modalRt.anchorMax = Vector2.one;
            modalRt.offsetMin = modalRt.offsetMax = Vector2.zero;
            worldMapModal = modalRt.gameObject;

            var backdrop = modalRt.gameObject.AddComponent<Image>();
            backdrop.sprite = cachedWhiteSprite;
            backdrop.color = new Color(0.03f, 0.02f, 0.05f, 0.78f);
            backdrop.raycastTarget = true;
            modalRt.gameObject.AddComponent<MythTap>().OnTap = () => SetWorldMapModalOpen(false);

            // 920x560 Tactical Board
            var board = MythUi.Rect("MapBoard920x560", modalRt, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(920f, 560f));
            var boardBg = board.gameObject.AddComponent<Image>();
            ApplySlicedFrame(boardBg, UISpriteLoader.GetFrameWoodPanel() ?? cachedWhiteSprite, new Color(0.11f, 0.09f, 0.15f, 0.98f));
            boardBg.raycastTarget = true;
            board.gameObject.AddComponent<MythTap>().OnTap = () => { };

            var boardBorder = MythUi.Rect("GoldenFrame", board, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(910f, 550f));
            var borderImg = boardBorder.gameObject.AddComponent<Image>();
            ApplySlicedFrame(borderImg, UISpriteLoader.GetFrameSlotSelected() ?? cachedWhiteSprite, new Color(0.88f, 0.70f, 0.30f, 0.32f));
            borderImg.raycastTarget = false;

            // Top Header
            MakeLabel(board, "Title", "SA BÀN CHIẾN THUẬT 2.5D & BẢN ĐỒ TỨ ĐẠI KHU VỰC [M]", 14.5f,
                TextAlignmentOptions.MidlineLeft, Gold, MythUi.TL, MythUi.TL, new Vector2(16f, -10f), new Vector2(760f, 30f));

            CreateStyledButton(board, "BtnClose", "ĐÓNG [ESC]", MythUi.TR, MythUi.TR, new Vector2(-14f, -11f), new Vector2(96f, 28f),
                new Color(0.62f, 0.22f, 0.20f, 0.96f), () => SetWorldMapModalOpen(false));

            // Left Interactive 2.5D Map Viewport (584x472)
            fullMapViewport = MythUi.Rect("FullMapViewport", board, MythUi.BL, MythUi.BL, new Vector2(14f, 38f), new Vector2(584f, 472f));
            var vpBg = fullMapViewport.gameObject.AddComponent<Image>();
            vpBg.sprite = cachedWhiteSprite;
            vpBg.color = new Color(0.12f, 0.22f, 0.14f, 1f);
            fullMapViewport.gameObject.AddComponent<RectMask2D>();

            fullMapImage = MythUi.Img("HighResMap", fullMapViewport, cachedMapSprite ?? cachedWhiteSprite,
                MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(584f, 472f));
            fullMapImage.color = cachedMapSprite != null ? Color.white : new Color(0.20f, 0.35f, 0.20f, 1f);

            // Fence perimeter guide
            var fenceGuide = MythUi.Img("FencePerimeter", fullMapViewport, cachedWhiteSprite,
                MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(380f, 265f));
            fenceGuide.color = new Color(0.96f, 0.80f, 0.34f, 0.12f);

            fullMapMarkerLayer = MythUi.Rect("InteractiveMarkers", fullMapViewport, MythUi.BL, MythUi.BL, Vector2.zero, new Vector2(584f, 472f));

            // Legend Strip below Full Map Viewport
            var legendRt = MythUi.Rect("LegendStrip", board, MythUi.BL, MythUi.BL, new Vector2(14f, 10f), new Vector2(584f, 24f));
            var legBg = legendRt.gameObject.AddComponent<Image>();
            legBg.sprite = cachedWhiteSprite;
            legBg.color = new Color(0.07f, 0.06f, 0.10f, 0.92f);
            MakeLabel(legendRt, "LegendTxt",
                "▲ Nhân vật   ● Heo khỏe   ● Heo đói/bệnh (nhấp để khám)   ■ Công trình/Tháp (nhấp để mở)   ◆ NPC   ★ Cổng Vùng",
                10.0f, TextAlignmentOptions.Center, Ivory, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(576f, 22f));

            // Right Tactical Sidebar (296x502)
            var sidebar = MythUi.Rect("TacticalSidebar", board, MythUi.BR, MythUi.BR, new Vector2(-14f, 10f), new Vector2(296f, 500f));
            var sbBg = sidebar.gameObject.AddComponent<Image>();
            sbBg.sprite = cachedWhiteSprite;
            sbBg.color = new Color(0.14f, 0.12f, 0.19f, 0.96f);

            modalTelemetryText = MakeLabel(sidebar, "Telemetry", "", 10.0f, TextAlignmentOptions.TopLeft,
                Ivory, MythUi.TL, MythUi.TL, new Vector2(8f, -8f), new Vector2(280f, 64f), false);

            MakeLabel(sidebar, "ZonesHdr", "TỨ ĐẠI KHU VỰC (NHẤP ĐỂ XEM / DI CHUYỂN)", 10.0f,
                TextAlignmentOptions.MidlineLeft, Gold, MythUi.TL, MythUi.TL, new Vector2(8f, -74f), new Vector2(280f, 18f));

            for (int i = 0; i < ZoneCardsMeta.Length; i++)
            {
                var zm = ZoneCardsMeta[i];
                float yPos = -96f - i * 54f;
                var card = MythUi.Rect("ZoneCard_" + zm.locId, sidebar, MythUi.TL, MythUi.TL, new Vector2(8f, yPos), new Vector2(280f, 50f));
                var cardBg = card.gameObject.AddComponent<Image>();
                ApplySlicedFrame(cardBg, UISpriteLoader.GetFrameSlotNormal() ?? cachedWhiteSprite, new Color(0.20f, 0.18f, 0.28f, 0.95f));
                cardBg.raycastTarget = true;
                zoneCardBgs[zm.locId] = cardBg;

                MakeLabel(card, "Title", zm.title, 10.5f, TextAlignmentOptions.TopLeft,
                    Gold, MythUi.TL, MythUi.TL, new Vector2(8f, -4f), new Vector2(198f, 18f));

                var subLbl = MakeLabel(card, "Sub", zm.subtitle, 8.8f, TextAlignmentOptions.BottomLeft,
                    Ivory, MythUi.BL, MythUi.BL, new Vector2(8f, 4f), new Vector2(198f, 22f), false);
                zoneCardSubTexts[zm.locId] = subLbl;

                string capturedLoc = zm.locId;
                CreateStyledButton(card, "BtnGo_" + zm.locId, "Chọn / Đi", MythUi. TR, MythUi.TR, new Vector2(-6f, -12f), new Vector2(64f, 26f),
                    new Color(0.24f, 0.38f, 0.54f, 0.96f), () => OnSelectOrTravelZone(capturedLoc));
                card.gameObject.AddComponent<MythTap>().OnTap = () => OnSelectOrTravelZone(capturedLoc);
            }

            // Selection Inspector Card
            var inspectBox = MythUi.Rect("SelectionBox", sidebar, MythUi.BL, MythUi.BL, new Vector2(8f, 66f), new Vector2(280f, 114f));
            var ibBg = inspectBox.gameObject.AddComponent<Image>();
            ibBg.sprite = cachedWhiteSprite;
            ibBg.color = new Color(0.09f, 0.13f, 0.19f, 0.96f);

            modalSelectionTitle = MakeLabel(inspectBox, "SelTitle", "Điểm đang chọn: Nông Trại Chính", 10.5f,
                TextAlignmentOptions.TopLeft, Gold, MythUi.TL, MythUi.TL, new Vector2(8f, -5f), new Vector2(264f, 18f));

            modalSelectionBody = MakeLabel(inspectBox, "SelBody",
                "Nhấp vào bất kỳ công trình, tháp canh, đàn heo, NPC hoặc cổng vùng trên bản đồ để mở bảng điều khiển trực tiếp.",
                9.0f, TextAlignmentOptions.TopLeft, Ivory, MythUi.TL, MythUi.TL, new Vector2(8f, -24f), new Vector2(264f, 54f), false);

            var selBtnGo = CreateStyledButton(inspectBox, "SelActionBtn", "Mở Bảng Tương Tác", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 6f), new Vector2(196f, 26f), new Color(0.26f, 0.46f, 0.34f, 0.98f), () => pendingSelectionAction?.Invoke());
            modalSelectionActionLabel = selBtnGo.GetComponentInChildren<TextMeshProUGUI>();

            // Bottom Tactical Shortcuts Row (2x2 buttons)
            CreateStyledButton(sidebar, "CmdOverview", "Tổng Quan [O]", MythUi.BL, MythUi.BL, new Vector2(8f, 34f), new Vector2(136f, 25f),
                new Color(0.24f, 0.34f, 0.50f, 0.96f), () =>
                {
                    SetWorldMapModalOpen(false);
                    FarmOverviewPopup.Ensure()?.TogglePopup();
                });
            CreateStyledButton(sidebar, "CmdDefense", "Tháp & Rào [P]", MythUi.BR, MythUi.BR, new Vector2(-8f, 34f), new Vector2(136f, 25f),
                new Color(0.24f, 0.34f, 0.50f, 0.96f), () =>
                {
                    SetWorldMapModalOpen(false);
                    DefenseBuildPopup.Instance?.Toggle();
                });
            CreateStyledButton(sidebar, "CmdInterior", "Nội Thất [H]", MythUi.BL, MythUi.BL, new Vector2(8f, 6f), new Vector2(136f, 25f),
                new Color(0.24f, 0.34f, 0.50f, 0.96f), () =>
                {
                    SetWorldMapModalOpen(false);
                    BuildingInteriorPopup.Ensure().Toggle();
                });
            CreateStyledButton(sidebar, "CmdIntel", "Tình Báo Quái", MythUi.BR, MythUi.BR, new Vector2(-8f, 6f), new Vector2(136f, 25f),
                new Color(0.24f, 0.34f, 0.50f, 0.96f), () =>
                {
                    SetWorldMapModalOpen(false);
                    IntelPopup.Open();
                });

            worldMapModal.SetActive(false);
        }

        private GameObject CreateStyledButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size,
            Color bgColor,
            Action onClick)
        {
            var rt = MythUi.Rect(name, parent, anchor, pivot, anchoredPos, size);
            var img = rt.gameObject.AddComponent<Image>();
            ApplySlicedFrame(img, UISpriteLoader.GetFrameActionButton() ?? cachedWhiteSprite, bgColor);
            img.raycastTarget = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            MakeLabel(rt, "Label", label, 9.2f, TextAlignmentOptions.Center, Gold, MythUi.MID, MythUi.MID, Vector2.zero, size - new Vector2(4f, 2f));
            return rt.gameObject;
        }

        // =====================================================================
        // UPDATE LOOP: INPUT, RADAR BLIPS, POPOVER, AND LIVING WORLD ANIMATOR
        // =====================================================================
        private void Update()
        {
            if (hostSafeRoot == null && MythHudController.Instance != null)
            {
                var safe = MythHudController.Instance.transform.Find("SafeArea") as RectTransform;
                if (safe != null) AttachToSafeRoot(safe);
            }
            if (hostSafeRoot == null) return;

            HandleHotkeys();

            if (Time.unscaledTime >= nextRadarSampleTime)
            {
                nextRadarSampleTime = Time.unscaledTime + 0.10f;
                RefreshRadarMarkers();
                if (IsWeatherPanelOpen) UpdateWeatherPopoverContent();
            }

            UpdateLivingWorldAtmosphere();
            UpdateLivingWorldBuildingsAndShadows();
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsWorldMapModalOpen)
                {
                    SetWorldMapModalOpen(false);
                    return;
                }
                if (IsWeatherPanelOpen)
                {
                    SetWeatherPopoverOpen(false);
                    return;
                }
            }

            // [M] opens the Full 920x560 Tactical World Map when not standing next to an inspectable pig
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (IsWorldMapModalOpen)
                {
                    SetWorldMapModalOpen(false);
                    return;
                }
                if (!UISpriteLoader.IsAnyModalOrDialogueOpen() && !IsPlayerNearInspectablePig())
                {
                    SetWorldMapModalOpen(true);
                }
            }
        }

        private static bool IsPlayerNearInspectablePig()
        {
            var player = PlayerMobileController.Instance;
            if (player == null) return false;
            var pigs = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            for (int i = 0; i < pigs.Length; i++)
            {
                if (pigs[i] != null && Vector2.Distance(player.transform.position, pigs[i].transform.position) <= 3.6f)
                    return true;
            }
            return false;
        }

        public void SetWorldMapModalOpen(bool open)
        {
            EnsureOverlayModalCanvas();
            if (worldMapModal == null) return;
            if (open && weatherPopover != null) weatherPopover.SetActive(false);
            worldMapModal.SetActive(open);
            if (open)
            {
                worldMapModal.transform.SetAsLastSibling();
                RebuildFullWorldMapModalContent();
            }
        }

        public void SetWeatherPopoverOpen(bool open)
        {
            if (weatherPopover == null) return;
            if (open && worldMapModal != null) worldMapModal.SetActive(false);
            weatherPopover.SetActive(open);
            if (open)
            {
                weatherPopover.transform.SetAsLastSibling();
                UpdateWeatherPopoverContent();
            }
        }

        // =====================================================================
        // RADAR MARKER PROJECTION (EVERY 0.1s)
        // =====================================================================
        private static Rect GetActiveWorldBounds()
        {
            if (PixelMythFarmGameplay.Instance != null)
                return PixelMythFarmGameplay.Instance.MapRect;
            return new Rect(0f, 0f, 184f, 160f);
        }

        private static Vector2 WorldToNormalized(Vector3 worldPos, Rect mapRect)
        {
            float nx = Mathf.Clamp01((worldPos.x - mapRect.xMin) / Mathf.Max(1f, mapRect.width));
            float ny = Mathf.Clamp01((worldPos.y - mapRect.yMin) / Mathf.Max(1f, mapRect.height));
            return new Vector2(nx, ny);
        }

        private void RefreshRadarMarkers()
        {
            if (radarViewport == null || radarMarkerLayer == null) return;

            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            Rect mapBounds = GetActiveWorldBounds();
            Vector2 vpSize = new Vector2(98f, 60f);

            if (radarZoneText != null)
            {
                radarZoneText.text = GetShortSceneZoneLabel(SceneManager.GetActiveScene().name, engine);
            }

            var player = PlayerMobileController.Instance;
            Vector3 playerPos = player != null
                ? player.transform.position
                : new Vector3(mapBounds.center.x, mapBounds.center.y, 0f);

            Vector2 playerNorm = WorldToNormalized(playerPos, mapBounds);
            if (playerChevron != null)
            {
                playerChevron.anchoredPosition = new Vector2(playerNorm.x * vpSize.x, playerNorm.y * vpSize.y);
                Vector2 dir = player != null ? player.FacingDirection : Vector2.down;
                if (dir.sqrMagnitude < 0.001f) dir = Vector2.down;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                playerChevron.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            float? maxRadius = engine?.WeatherStatus?.MinimapRadiusMeters(engine);
            if (radarFogRoot != null)
            {
                bool fogActive = maxRadius.HasValue;
                if (radarFogRoot.gameObject.activeSelf != fogActive)
                    radarFogRoot.gameObject.SetActive(fogActive);
                if (fogActive && radarFogBadgeText != null)
                    radarFogBadgeText.text = $"Sương Mù {maxRadius.Value:0}m";
            }

            int blipIndex = 0;

            // 1. Buildings & Defense Towers (Amber / Cyan mini-squares)
            var customBuildings = FindObjectsByType<CustomBuildingController>(FindObjectsSortMode.None);
            for (int i = 0; i < customBuildings.Length; i++)
            {
                if (customBuildings[i] == null || IsClippedByFog(customBuildings[i].transform.position, playerPos, maxRadius)) continue;
                PlotRadarBlip(ref blipIndex, customBuildings[i].transform.position, mapBounds, vpSize,
                    cachedWhiteSprite, new Color(0.96f, 0.76f, 0.26f, 0.95f), new Vector2(5.5f, 5.5f));
            }

            var shelters = FindObjectsByType<Shelter2DView>(FindObjectsSortMode.None);
            for (int i = 0; i < shelters.Length; i++)
            {
                if (shelters[i] == null || IsClippedByFog(shelters[i].transform.position, playerPos, maxRadius)) continue;
                PlotRadarBlip(ref blipIndex, shelters[i].transform.position, mapBounds, vpSize,
                    cachedWhiteSprite, new Color(0.92f, 0.68f, 0.24f, 0.95f), new Vector2(5f, 5f));
            }

            var feeders = FindObjectsByType<Feeder2DView>(FindObjectsSortMode.None);
            for (int i = 0; i < feeders.Length; i++)
            {
                if (feeders[i] == null || IsClippedByFog(feeders[i].transform.position, playerPos, maxRadius)) continue;
                PlotRadarBlip(ref blipIndex, feeders[i].transform.position, mapBounds, vpSize,
                    cachedWhiteSprite, new Color(0.88f, 0.64f, 0.22f, 0.90f), new Vector2(4.2f, 4.2f));
            }

            var troughs = FindObjectsByType<WaterTrough2DView>(FindObjectsSortMode.None);
            for (int i = 0; i < troughs.Length; i++)
            {
                if (troughs[i] == null || IsClippedByFog(troughs[i].transform.position, playerPos, maxRadius)) continue;
                PlotRadarBlip(ref blipIndex, troughs[i].transform.position, mapBounds, vpSize,
                    cachedWhiteSprite, new Color(0.36f, 0.76f, 0.96f, 0.90f), new Vector2(4.2f, 4.2f));
            }

            var towers = FindObjectsByType<DefenseTower2DView>(FindObjectsSortMode.None);
            for (int i = 0; i < towers.Length; i++)
            {
                if (towers[i] == null || IsClippedByFog(towers[i].transform.position, playerPos, maxRadius)) continue;
                PlotRadarBlip(ref blipIndex, towers[i].transform.position, mapBounds, vpSize,
                    cachedWhiteSprite, new Color(0.32f, 0.88f, 0.98f, 0.95f), new Vector2(4.8f, 4.8f));
            }

            // 2. NPCs (Sky-blue diamonds)
            var npcs = FindObjectsByType<PixelMythNpc>(FindObjectsSortMode.None);
            for (int i = 0; i < npcs.Length; i++)
            {
                if (npcs[i] == null || IsClippedByFog(npcs[i].transform.position, playerPos, maxRadius)) continue;
                PlotRadarBlip(ref blipIndex, npcs[i].transform.position, mapBounds, vpSize,
                    cachedDiamondSprite, new Color(0.38f, 0.82f, 1.0f, 0.95f), new Vector2(5.5f, 5.5f));
            }

            // 3. Herd Pigs (Emerald dots for healthy, pulsing Crimson for hungry/sick/overheated)
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 7.5f);
            var pigViews = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            if (pigViews != null && pigViews.Length > 0)
            {
                for (int i = 0; i < pigViews.Length; i++)
                {
                    var pv = pigViews[i];
                    if (pv == null || IsClippedByFog(pv.transform.position, playerPos, maxRadius)) continue;
                    Pig pigData = pv.PigModel;
                    if (pigData != null && !pigData.IsAlive) continue;
                    bool alert = IsPigAlert(engine, pigData);
                    Color col = alert
                        ? new Color(1f, 0.22f, 0.22f, pulse)
                        : new Color(0.36f, 0.94f, 0.56f, 0.95f);
                    float sz = alert ? 5.2f : 4.2f;
                    PlotRadarBlip(ref blipIndex, pv.transform.position, mapBounds, vpSize, cachedCircleSprite, col, new Vector2(sz, sz));
                }
            }
            else if (engine?.Pigs != null)
            {
                for (int i = 0; i < engine.Pigs.Count; i++)
                {
                    var pig = engine.Pigs[i];
                    if (pig == null || !pig.IsAlive) continue;
                    Vector3 pos = GetFallbackPigWorldPos(pig, i, mapBounds);
                    if (IsClippedByFog(pos, playerPos, maxRadius)) continue;
                    bool alert = IsPigAlert(engine, pig);
                    Color col = alert
                        ? new Color(1f, 0.22f, 0.22f, pulse)
                        : new Color(0.36f, 0.94f, 0.56f, 0.95f);
                    PlotRadarBlip(ref blipIndex, pos, mapBounds, vpSize, cachedCircleSprite, col, new Vector2(4.5f, 4.5f));
                }
            }

            // 4. Enemies (Pulsing red danger blips)
            var enemies = FindObjectsByType<EnemyAgentView>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || !enemies[i].IsAlive || IsClippedByFog(enemies[i].transform.position, playerPos, maxRadius)) continue;
                PlotRadarBlip(ref blipIndex, enemies[i].transform.position, mapBounds, vpSize,
                    cachedDiamondSprite, new Color(1f, 0.16f, 0.12f, pulse), new Vector2(6.5f, 6.5f));
            }

            for (int i = blipIndex; i < radarBlipPool.Count; i++)
            {
                if (radarBlipPool[i] != null && radarBlipPool[i].gameObject.activeSelf)
                    radarBlipPool[i].gameObject.SetActive(false);
            }
        }

        private static bool IsClippedByFog(Vector3 targetPos, Vector3 playerPos, float? maxRadiusMeters)
        {
            if (!maxRadiusMeters.HasValue) return false;
            return Vector2.Distance(targetPos, playerPos) > maxRadiusMeters.Value;
        }

        private void PlotRadarBlip(ref int index, Vector3 worldPos, Rect mapBounds, Vector2 vpSize, Sprite sprite, Color color, Vector2 size)
        {
            Image img;
            if (index < radarBlipPool.Count && radarBlipPool[index] != null)
            {
                img = radarBlipPool[index];
                if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);
            }
            else
            {
                img = MythUi.Img("Blip_" + index, radarMarkerLayer, sprite, MythUi.BL, MythUi.MID, Vector2.zero, size);
                if (index < radarBlipPool.Count) radarBlipPool[index] = img;
                else radarBlipPool.Add(img);
            }

            img.sprite = sprite;
            img.color = color;
            img.enabled = true;
            Vector2 norm = WorldToNormalized(worldPos, mapBounds);
            var rect = img.rectTransform;
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(norm.x * vpSize.x, norm.y * vpSize.y);
            index++;
        }

        private static bool IsPigAlert(GameEngine engine, Pig p)
        {
            if (p == null || !p.IsAlive) return false;
            bool hungry = engine != null ? TroughCommands.WantsFood(engine, p) : p.Hunger < 35f;
            bool overheated = engine != null && engine.UncooledHeatHours(p.Id) >= 1f;
            return hungry || p.ActiveDisease.HasValue || p.SeizurePending || overheated; // no thirst stat in Core (water is per basin)
        }

        private static Vector3 GetFallbackPigWorldPos(Pig pig, int idx, Rect mapBounds)
        {
            if (pig != null && (pig.PositionX != 0f || pig.PositionY != 0f))
                return new Vector3(pig.PositionX, pig.PositionY, 0f);
            int hash = pig?.Id != null ? pig.Id.GetHashCode() : idx * 7919;
            float fx = 42f + Mathf.Abs(hash % 96);
            float fy = 44f + Mathf.Abs((hash / 97) % 68);
            return new Vector3(Mathf.Clamp(fx, mapBounds.xMin + 12f, mapBounds.xMax - 12f),
                               Mathf.Clamp(fy, mapBounds.yMin + 12f, mapBounds.yMax - 12f), 0f);
        }

        // =====================================================================
        // FULL WORLD MAP MODAL REBUILD & INTERACTIVE MARKERS
        // =====================================================================
        private void RebuildFullWorldMapModalContent()
        {
            if (fullMapMarkerLayer == null) return;

            for (int i = 0; i < fullMapMarkerObjects.Count; i++)
            {
                if (fullMapMarkerObjects[i] != null) Destroy(fullMapMarkerObjects[i]);
            }
            fullMapMarkerObjects.Clear();

            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            Rect mapBounds = GetActiveWorldBounds();
            Vector2 mapSize = new Vector2(584f, 472f);

            var player = PlayerMobileController.Instance;
            Vector3 playerPos = player != null ? player.transform.position : new Vector3(mapBounds.center.x, mapBounds.center.y, 0f);

            if (modalTelemetryText != null)
            {
                string sceneLabel = GetShortSceneZoneLabel(SceneManager.GetActiveScene().name, engine);
                int pigCount = 0;
                int alertPigs = 0;
                if (engine?.Pigs != null)
                {
                    for (int i = 0; i < engine.Pigs.Count; i++)
                    {
                        var p = engine.Pigs[i];
                        if (p == null || !p.IsAlive) continue;
                        pigCount++;
                        if (IsPigAlert(engine, p)) alertPigs++;
                    }
                }
                string timeStr = engine != null
                    ? $"Ngày {engine.Clock.CurrentDay} · {engine.Clock.CurrentHour:00}:{engine.Clock.CurrentMinute:00} ({GetDaypartName(engine.Clock.GetTimeOfDay())})"
                    : "Ngày 1 · 06:00 (Sáng)";
                string weatherStr = engine != null ? GetWeatherVietnameseName(engine.CurrentWeather) : "Quang Đãng";
                int sc = engine?.Farm != null ? engine.Farm.HabitatIndex : 80;
                modalTelemetryText.text =
                    $"Vùng: {sceneLabel}  |  Tọa độ: ({playerPos.x:0}, {playerPos.y:0})\n" +
                    $"{timeStr} · {weatherStr}\n" +
                    $"Đàn heo: {pigCount} con (Cần chăm: {alertPigs})  |  Sinh cảnh: {sc}";
            }

            string curLoc = engine?.WorldLocations?.CurrentLocationId ?? "Farm";
            for (int i = 0; i < ZoneCardsMeta.Length; i++)
            {
                var zm = ZoneCardsMeta[i];
                bool isCurrent = string.Equals(curLoc, zm.locId, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(SceneManager.GetActiveScene().name, zm.sceneName, StringComparison.OrdinalIgnoreCase);
                bool canGo = isCurrent || (engine != null && PixelWorldRoutes.CanTravel(curLoc, zm.locId, engine));
                if (zoneCardSubTexts.TryGetValue(zm.locId, out var sub) && sub != null)
                {
                    sub.text = isCurrent ? $"Đang ở đây · {zm.subtitle}" : $"{zm.subtitle}";
                }
                if (zoneCardBgs.TryGetValue(zm.locId, out var cardBg) && cardBg != null)
                {
                    cardBg.color = isCurrent
                        ? new Color(0.26f, 0.42f, 0.24f, 0.98f)
                        : (canGo ? new Color(0.20f, 0.18f, 0.28f, 0.95f) : new Color(0.16f, 0.14f, 0.20f, 0.88f));
                }
            }

            // 1. Spawn Main Farm Buildings (Clicking opens BuildingInteriorPopup to that room)
            SpawnBuildingMarkersOnFullMap(mapBounds, mapSize);

            // 2. Spawn Defense Towers (Clicking opens DefenseBuildPopup)
            var towers = FindObjectsByType<DefenseTower2DView>(FindObjectsSortMode.None);
            for (int i = 0; i < towers.Length; i++)
            {
                if (towers[i] == null) continue;
                var pos = towers[i].transform.position;
                int idx = i + 1;
                CreateInteractiveMapMarker(pos, mapBounds, mapSize, cachedWhiteSprite, new Color(0.30f, 0.88f, 0.98f, 1f),
                    new Vector2(14f, 14f), $"Tháp #{idx}", () =>
                    {
                        SelectMarkerInSidebar($"Tháp Phòng Thủ #{idx}",
                            $"Vị trí: ({pos.x:0}, {pos.y:0}). Bảo vệ chuồng trại trước đợt tấn công ban đêm.",
                            "Mở Bảng Tháp & Rào [P]", () =>
                            {
                                SetWorldMapModalOpen(false);
                                DefenseBuildPopup.Instance?.Toggle();
                            });
                    });
            }

            // 3. Spawn NPCs
            var npcs = FindObjectsByType<PixelMythNpc>(FindObjectsSortMode.None);
            for (int i = 0; i < npcs.Length; i++)
            {
                var npc = npcs[i];
                if (npc == null) continue;
                var pos = npc.transform.position;
                string npcName = string.IsNullOrEmpty(npc.DisplayName) ? npc.Id : npc.DisplayName;
                string npcRole = string.IsNullOrEmpty(npc.HomeRoleId) ? "Cư dân thần thoại" : npc.HomeRoleId;
                CreateInteractiveMapMarker(pos, mapBounds, mapSize, cachedDiamondSprite, new Color(0.38f, 0.84f, 1f, 1f),
                    new Vector2(15f, 15f), npcName, () =>
                    {
                        SelectMarkerInSidebar($"NPC: {npcName} ({npcRole})",
                            $"Đang đứng tại ({pos.x:0}, {pos.y:0}). Lại gần và nhấn [E] để trò chuyện hoặc mở Sổ Tay [N] xem độ thân mật.",
                            "Mở Sổ Tay Thân Mật [N]", () =>
                            {
                                SetWorldMapModalOpen(false);
                                QuestTrackerPanel.Instance?.ToggleJournal();
                            });
                    });
            }

            // 4. Spawn Herd Pigs (Clicking opens PigInspectPopup)
            var pigViews = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            if (pigViews != null && pigViews.Length > 0)
            {
                for (int i = 0; i < pigViews.Length; i++)
                {
                    var pv = pigViews[i];
                    if (pv == null || pv.PigModel == null || !pv.PigModel.IsAlive) continue;
                    SpawnPigMarkerOnFullMap(engine, pv.PigModel, pv.transform.position, mapBounds, mapSize);
                }
            }
            else if (engine?.Pigs != null)
            {
                for (int i = 0; i < engine.Pigs.Count; i++)
                {
                    var pig = engine.Pigs[i];
                    if (pig == null || !pig.IsAlive) continue;
                    Vector3 pos = GetFallbackPigWorldPos(pig, i, mapBounds);
                    SpawnPigMarkerOnFullMap(engine, pig, pos, mapBounds, mapSize);
                }
            }

            // 5. Spawn 4 Zone Portals
            SpawnZonePortalMarkers(mapBounds, mapSize);

            // 6. Spawn Player Marker on top
            string heroName = engine?.Character != null && engine.Character.Identity == PlayerId.Khoa ? "Khoa" : "An";
            CreateInteractiveMapMarker(playerPos, mapBounds, mapSize, cachedChevronSprite, new Color(1f, 0.94f, 0.45f, 1f),
                new Vector2(18f, 18f), $"{heroName} (Bạn)", () =>
                {
                    SelectMarkerInSidebar($"Nhân Vật: {heroName}",
                        $"Tọa độ hiện tại: ({playerPos.x:0}, {playerPos.y:0}). Nhấp vào công trình, đàn heo hoặc thẻ khu vực bên phải để điều phối nhanh.",
                        "Mở Hồ Sơ Nhân Vật [C]", () =>
                        {
                            SetWorldMapModalOpen(false);
                            CharacterPanel.Toggle();
                        });
                });
        }

        private void SpawnBuildingMarkersOnFullMap(Rect mapBounds, Vector2 mapSize)
        {
            var buildings = new (string label, Vector3 pos, BuildingInteriorPopup.InteriorRoom room, string desc)[]
            {
                ("Nhà Chính & Bếp Lò", new Vector3(92f, 108f, 0f), BuildingInteriorPopup.InteriorRoom.NhaChinh,
                    "Nơi nghỉ ngơi qua ngày, nấu bữa ăn hồi phục Thể lực và tránh thời tiết khắc nghiệt (Cảm Nắng, Ướt Lạnh, Giông Bão)."),
                ("Chuồng Trại & Y Tế", new Vector3(64f, 94f, 0f), BuildingInteriorPopup.InteriorRoom.ChuongTraiLon,
                    "Châm máng cám, bơm nước mát, khám chữa bệnh và phối giống đàn heo."),
                ("Xưởng Rèn & Phòng Thủ", new Vector3(118f, 94f, 0f), BuildingInteriorPopup.InteriorRoom.XuongRenPhongThu,
                    "Cường hóa trang bị, rèn vũ khí, xây dựng & nạp đạn tháp canh phòng thủ."),
                ("Quán Bà Sáu & Trạm Tin", new Vector3(92f, 134f, 0f), BuildingInteriorPopup.InteriorRoom.QuanBaSauTramTin,
                    "Trao đổi vật tư dân làng, mua tình báo dự báo đợt quái ban đêm và xem tin đồn chợ."),
            };

            for (int i = 0; i < buildings.Length; i++)
            {
                var b = buildings[i];
                CreateInteractiveMapMarker(b.pos, mapBounds, mapSize, cachedWhiteSprite, new Color(0.96f, 0.76f, 0.24f, 1f),
                    new Vector2(16f, 16f), b.label, () =>
                    {
                        SelectMarkerInSidebar($"Công Trình: {b.label}", b.desc, $"Bước Vào {b.label}", () =>
                        {
                            SetWorldMapModalOpen(false);
                            BuildingInteriorPopup.Ensure().Open(b.room);
                        });
                    });
            }
        }

        private void SpawnPigMarkerOnFullMap(GameEngine engine, Pig pig, Vector3 worldPos, Rect mapBounds, Vector2 mapSize)
        {
            bool alert = IsPigAlert(engine, pig);
            Color col = alert ? new Color(1f, 0.26f, 0.24f, 1f) : new Color(0.36f, 0.94f, 0.56f, 1f);
            string shortLabel = $"{pig.Name} ({pig.WeightKg:0}kg)";
            CreateInteractiveMapMarker(worldPos, mapBounds, mapSize, cachedCircleSprite, col,
                new Vector2(alert ? 14f : 12f, alert ? 14f : 12f), "", () =>
                {
                    string statusDesc = $"Cân nặng: {pig.WeightKg:0.0}kg · No: {pig.Hunger:0}% · Tâm trạng: {pig.Mood}";
                    SelectMarkerInSidebar($"Cá Thể Heo: {shortLabel}", statusDesc, "Mở Bảng Khám & Chăm Sóc", () =>
                    {
                        SetWorldMapModalOpen(false);
                        if (PigInspectPopup.Instance != null) PigInspectPopup.Instance.Show(pig);
                        else AppraisalPopup.Open();
                    });
                });
        }

        private void SpawnZonePortalMarkers(Rect mapBounds, Vector2 mapSize)
        {
            var portals = new (string locId, Vector3 pos, string label)[]
            {
                ("MarketTown", new Vector3(172f, 80f, 0f), "Cổng Thị Trấn Chợ →"),
                ("Hub_Village", new Vector3(92f, 14f, 0f), "↓ Cổng Làng Bờ Rìa"),
                ("Forest_Primeval", new Vector3(16f, 80f, 0f), "← Cổng Rừng Nguyên Sinh"),
            };

            for (int i = 0; i < portals.Length; i++)
            {
                var p = portals[i];
                CreateInteractiveMapMarker(p.pos, mapBounds, mapSize, cachedDiamondSprite, new Color(0.98f, 0.58f, 0.92f, 1f),
                    new Vector2(16f, 16f), p.label, () => OnSelectOrTravelZone(p.locId));
            }
        }

        private void CreateInteractiveMapMarker(
            Vector3 worldPos,
            Rect mapBounds,
            Vector2 mapSize,
            Sprite iconSprite,
            Color color,
            Vector2 size,
            string caption,
            Action onClick)
        {
            Vector2 norm = WorldToNormalized(worldPos, mapBounds);
            var img = MythUi.Img("MapMarker", fullMapMarkerLayer, iconSprite, MythUi.BL, MythUi.MID,
                new Vector2(norm.x * mapSize.x, norm.y * mapSize.y), size, true);
            img.color = color;
            var rt = img.rectTransform;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            if (!string.IsNullOrEmpty(caption))
            {
                var capRt = MythUi.Rect("Cap", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(124f, 15f));
                var capBg = capRt.gameObject.AddComponent<Image>();
                capBg.sprite = cachedWhiteSprite;
                capBg.color = new Color(0.06f, 0.05f, 0.09f, 0.84f);
                capBg.raycastTarget = false;
                MakeLabel(capRt, "Txt", caption, 8.5f, TextAlignmentOptions.Center, Ivory, MythUi.MID, MythUi.MID, Vector2.zero, new Vector2(120f, 14f));
            }

            fullMapMarkerObjects.Add(rt.gameObject);
        }

        private void SelectMarkerInSidebar(string title, string body, string actionLabel, Action action)
        {
            if (modalSelectionTitle != null) modalSelectionTitle.text = title;
            if (modalSelectionBody != null) modalSelectionBody.text = body;
            if (modalSelectionActionLabel != null) modalSelectionActionLabel.text = actionLabel;
            pendingSelectionAction = action;
        }

        private void OnSelectOrTravelZone(string targetLocationId)
        {
            var map = PixelWorldRoutes.ForLocation(targetLocationId);
            string title = map.DisplayName;
            string subtitle = "Tuyến đường liên vùng 2.5D";
            for (int i = 0; i < ZoneCardsMeta.Length; i++)
            {
                if (ZoneCardsMeta[i].locId == map.LocationId)
                {
                    title = ZoneCardsMeta[i].title;
                    subtitle = ZoneCardsMeta[i].subtitle;
                    break;
                }
            }

            SelectMarkerInSidebar(
                $"Khu Vực: {title}",
                $"{subtitle}\nNhấn nút bên dưới để di chuyển qua cổng vùng hoặc mở giao diện tương ứng.",
                $"Di Chuyển Tới {title}",
                () =>
                {
                    SetWorldMapModalOpen(false);
                    var travel = FindFirstObjectByType<PixelWorldTravelController>();
                    if (travel != null && travel.TravelToLocation(map.LocationId))
                    {
                        ToastOverlay.Show($"Đang khởi hành tới {title}...");
                        return;
                    }
                    if (map.LocationId == "MarketTown")
                    {
                        DayMarketPopup.Open();
                        return;
                    }
                    if (map.LocationId == "Hub_Village")
                    {
                        BuildingInteriorPopup.Ensure().Open(BuildingInteriorPopup.InteriorRoom.QuanBaSauTramTin);
                        return;
                    }
                    ToastOverlay.Show($"Hãy đi tới cổng vùng trên bản đồ để sang {title}.");
                });
        }

        // =====================================================================
        // INTERACTIVE 4-DAYPART & WEATHER POPOVER LOGIC + 4 ACTION BUTTONS
        // =====================================================================
        private void UpdateWeatherPopoverContent()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;

            int hour = engine.Clock.CurrentHour;
            int min = engine.Clock.CurrentMinute;
            TimeOfDay dp = engine.Clock.GetTimeOfDay();

            if (popoverClockTitle != null)
            {
                popoverClockTitle.text = $"NHẬT NGUYỆT ĐÀI · NGÀY {engine.Clock.CurrentDay} · {hour:00}:{min:00} ({GetDaypartName(dp).ToUpperInvariant()})";
            }

            if (popoverNeedlePreview != null)
            {
                int step = (hour + 12) % 24;
                popoverNeedlePreview.localRotation = Quaternion.Euler(0f, 0f, -15f * step);
            }

            for (int i = 0; i < 4; i++)
            {
                var meta = DaypartMeta[i];
                bool active = meta.part == dp;
                if (daypartChipBgs[i] != null)
                {
                    daypartChipBgs[i].color = active
                        ? new Color(meta.tint.r * 0.88f, meta.tint.g * 0.88f, meta.tint.b * 0.88f, 1f)
                        : new Color(meta.tint.r * 0.25f, meta.tint.g * 0.25f, meta.tint.b * 0.25f, 0.82f);
                }
                if (daypartChipTexts[i] != null)
                {
                    daypartChipTexts[i].color = active ? Gold : Ivory;
                }
            }

            if (popoverDaypartRuleText != null)
            {
                popoverDaypartRuleText.text = GetDaypartGameplayRule(dp);
            }

            string wStem = GetWeatherStem(engine.CurrentWeather);
            if (popoverWeatherIcon != null)
            {
                popoverWeatherIcon.sprite = MythUi.Icon("weather", wStem, 24);
                popoverWeatherIcon.enabled = popoverWeatherIcon.sprite != null;
            }

            if (popoverWeatherTitle != null)
            {
                popoverWeatherTitle.text = $"Thời tiết hiện tại: {GetWeatherVietnameseName(engine.CurrentWeather)}";
            }

            if (popoverWeatherEffectsText != null)
            {
                popoverWeatherEffectsText.text = GetWeatherGameplayEffectSummary(engine.CurrentWeather);
            }

            if (popoverStatusPillsText != null && engine.WeatherStatus != null)
            {
                var activeIds = engine.WeatherStatus.ActiveStatuses(engine);
                string envTag = engine.WeatherStatus.IsOutdoors ? "Ngoài trời" : "Dưới mái che";
                if (activeIds.Count == 0)
                {
                    popoverStatusPillsText.text = $"Cơ thể ({envTag}): Bình thường — không dính hiệu ứng thời tiết.";
                    popoverStatusPillsText.color = new Color(0.52f, 0.94f, 0.66f, 1f);
                }
                else
                {
                    var names = new List<string>();
                    for (int i = 0; i < activeIds.Count; i++)
                        names.Add(PlayerWeatherStatus.DisplayName(activeIds[i]));
                    popoverStatusPillsText.text = $"Cảnh báo ({envTag}): Đang bị {string.Join(", ", names)}!";
                    popoverStatusPillsText.color = new Color(1f, 0.56f, 0.36f, 1f);
                }
            }
        }

        private void OnClickRestOneHour()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            var (ok, msg, _) = PlayerRestCommands.Rest(engine, 1);
            ToastOverlay.Show(msg);
            if (ok) MobileGameController.Instance?.OnStateUpdated?.Invoke();
            UpdateWeatherPopoverContent();
        }

        private void OnClickBuildTempShade()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;

            var player = PlayerMobileController.Instance;
            Vector3 pos = player != null ? player.transform.position : new Vector3(92f, 96f, 0f);

            // Find MaiCheTam slot in SpaceBag if bracelet is worn
            int slotIdx = -1;
            if (engine.Carry != null && engine.Carry.HasBracelet && engine.SpaceBag != null)
            {
                for (int i = 0; i < engine.SpaceBag.Items.Count; i++)
                {
                    if (ItemUse.BareId(engine.SpaceBag.Items[i].ItemId) == "MaiCheTam")
                    {
                        slotIdx = i;
                        break;
                    }
                }
            }

            var (ok, message) = ItemUse.Use(engine, "MaiCheTam", null, slotIdx, atWarehouse: false, placeX: pos.x, placeY: pos.y);
            if (ok)
            {
                if (engine.WeatherStatus != null)
                {
                    engine.WeatherStatus.IsOutdoors = false;
                    engine.WeatherStatus.IsFarFromRoof = false;
                }
                ToastOverlay.Show(message);
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
            }
            else
            {
                if (engine.WeatherStatus != null)
                {
                    engine.WeatherStatus.IsOutdoors = false;
                    engine.WeatherStatus.IsFarFromRoof = false;
                }
                ToastOverlay.Show($"{message} — Đã tạm nép vào bóng râm hiên trại để tránh Nắng Gắt / Mưa.");
            }
            UpdateWeatherPopoverContent();
        }

        private void OnClickEnterMainHouseSleep()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;

            if (engine.Clock.CurrentHour >= 18 || engine.Clock.CurrentHour < 6)
            {
                var (ok, msg, _) = PlayerRestCommands.Sleep(engine);
                if (ok)
                {
                    ToastOverlay.Show(msg);
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    UpdateWeatherPopoverContent();
                    return;
                }
            }

            SetWeatherPopoverOpen(false);
            BuildingInteriorPopup.Ensure().Open(BuildingInteriorPopup.InteriorRoom.NhaChinh);
            ToastOverlay.Show("Đã bước vào Nhà Chính. Bạn có thể nghỉ ngơi hoặc ngủ qua ngày tại Giường.");
        }

        private void OnClickOpenWaveIntel()
        {
            SetWeatherPopoverOpen(false);
            IntelPopup.Open();
        }

        // =====================================================================
        // 5. LIVING WORLD 4-DAYPART ATMOSPHERE, BUILDING & SHADOW ANIMATOR
        // =====================================================================
        private void UpdateLivingWorldAtmosphere()
        {
            if (atmosphereOverlay == null) return;
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;

            TimeOfDay tod = engine.Clock.GetTimeOfDay();
            Color targetTint;
            Color targetGlow;

            switch (tod)
            {
                case TimeOfDay.Sang:
                    targetTint = new Color(0.98f, 0.72f, 0.52f, 0.055f);
                    targetGlow = new Color(1.0f, 0.82f, 0.56f, 0.09f);
                    break;
                case TimeOfDay.Trua:
                    targetTint = new Color(1.0f, 0.96f, 0.80f, 0.025f);
                    targetGlow = new Color(1.0f, 0.95f, 0.68f, 0.07f);
                    break;
                case TimeOfDay.Chieu:
                    targetTint = new Color(0.92f, 0.42f, 0.22f, 0.105f);
                    targetGlow = new Color(0.98f, 0.54f, 0.26f, 0.14f);
                    break;
                default:
                    targetTint = new Color(0.08f, 0.11f, 0.28f, 0.28f);
                    targetGlow = new Color(0.28f, 0.38f, 0.78f, 0.10f);
                    break;
            }

            switch (engine.CurrentWeather)
            {
                case WeatherType.NangGat:
                    targetTint = Color.Lerp(targetTint, new Color(1f, 0.58f, 0.18f, 0.12f), 0.65f);
                    break;
                case WeatherType.Mua:
                case WeatherType.NomAm:
                    targetTint = Color.Lerp(targetTint, new Color(0.16f, 0.26f, 0.42f, 0.16f), 0.6f);
                    break;
                case WeatherType.GiongBao:
                    float flash = Mathf.Sin(Time.unscaledTime * 13f) > 0.96f ? 0.22f : 0f;
                    targetTint = Color.Lerp(targetTint, new Color(0.12f, 0.16f, 0.30f, 0.22f + flash), 0.7f);
                    break;
                case WeatherType.SuongMu:
                    targetTint = Color.Lerp(targetTint, new Color(0.68f, 0.72f, 0.80f, 0.18f), 0.65f);
                    break;
                case WeatherType.RetDam:
                    targetTint = Color.Lerp(targetTint, new Color(0.45f, 0.78f, 0.98f, 0.13f), 0.55f);
                    break;
                case WeatherType.MuaBucXa:
                    targetTint = Color.Lerp(targetTint, new Color(0.48f, 0.18f, 0.62f, 0.16f), 0.6f);
                    break;
            }

            atmosphereOverlay.color = Color.Lerp(atmosphereOverlay.color, targetTint, Time.unscaledDeltaTime * 3.5f);
            if (atmosphereCornerGlow != null)
                atmosphereCornerGlow.color = Color.Lerp(atmosphereCornerGlow.color, targetGlow, Time.unscaledDeltaTime * 3.5f);
        }

        private void UpdateLivingWorldBuildingsAndShadows()
        {
            var player = PlayerMobileController.Instance;
            if (player != null)
            {
                EnsureGroundingShadow(player.transform, new Vector3(0f, -0.42f, 0f), new Vector3(0.85f, 0.36f, 1f));
                Vector3 pos = player.transform.position;
                if (hasLastPlayerPos && Vector2.Distance(pos, lastPlayerPos) > 0.025f)
                {
                    footstepTimer += Time.deltaTime;
                    if (footstepTimer >= 0.22f)
                    {
                        footstepTimer = 0f;
                        SpawnFootstepDustMote(pos + new Vector3(0f, -0.38f, 0f));
                    }
                }
                lastPlayerPos = pos;
                hasLastPlayerPos = true;
            }

            if (Time.unscaledTime < nextWorldAnimTime) return;
            nextWorldAnimTime = Time.unscaledTime + 0.15f;

            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            bool isNight = engine != null && engine.Clock.GetTimeOfDay() == TimeOfDay.Toi;
            float t = Time.unscaledTime;

            var pigs = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            for (int i = 0; i < pigs.Length; i++)
            {
                if (pigs[i] != null)
                    EnsureGroundingShadow(pigs[i].transform, new Vector3(0f, -0.28f, 0f), new Vector3(0.68f, 0.28f, 1f));
            }

            var buildings = FindObjectsByType<CustomBuildingController>(FindObjectsSortMode.None);
            for (int i = 0; i < buildings.Length; i++)
            {
                var b = buildings[i];
                if (b == null) continue;
                var sr = b.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    float warm = isNight ? (0.94f + 0.06f * Mathf.Sin(t * 3.2f + i)) : 1f;
                    sr.color = isNight ? new Color(1f, 0.93f * warm, 0.78f * warm, 1f) : Color.white;
                }
            }

            var towers = FindObjectsByType<DefenseTower2DView>(FindObjectsSortMode.None);
            for (int i = 0; i < towers.Length; i++)
            {
                if (towers[i] != null)
                    EnsureGroundingShadow(towers[i].transform, new Vector3(0f, -0.35f, 0f), new Vector3(0.90f, 0.38f, 1f));
            }
        }

        private void EnsureGroundingShadow(Transform target, Vector3 localOffset, Vector3 localScale)
        {
            if (target == null) return;
            if (target.Find("ContactShadow25D") != null) return;

            var go = new GameObject("ContactShadow25D");
            go.transform.SetParent(target, false);
            go.transform.localPosition = localOffset;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = cachedCircleSprite;
            sr.color = new Color(0.05f, 0.04f, 0.08f, 0.34f);
            var parentSr = target.GetComponentInChildren<SpriteRenderer>();
            if (parentSr != null)
            {
                sr.sortingLayerID = parentSr.sortingLayerID;
                sr.sortingOrder = parentSr.sortingOrder - 1;
            }
        }

        private void SpawnFootstepDustMote(Vector3 worldPos)
        {
            var go = new GameObject("FootstepDust25D");
            go.transform.position = worldPos + new Vector3(UnityEngine.Random.Range(-0.12f, 0.12f), UnityEngine.Random.Range(-0.05f, 0.05f), 0f);
            go.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = cachedCircleSprite;
            sr.color = new Color(0.82f, 0.74f, 0.58f, 0.42f);
            sr.sortingOrder = 5;
            Destroy(go, 0.38f);
        }

        // =====================================================================
        // HELPERS & VIETNAMESE COPY
        // =====================================================================
        private static string GetShortSceneZoneLabel(string sceneName, GameEngine engine)
        {
            if (string.Equals(sceneName, "Market_Town", StringComparison.OrdinalIgnoreCase)) return "Thị Trấn";
            if (string.Equals(sceneName, "Hub_Village", StringComparison.OrdinalIgnoreCase)) return "Làng Đông";
            if (string.Equals(sceneName, "Forest_Primeval", StringComparison.OrdinalIgnoreCase)) return "Rừng Cổ";
            string loc = engine?.WorldLocations?.CurrentLocationId ?? "";
            if (string.Equals(loc, "MarketTown", StringComparison.OrdinalIgnoreCase)) return "Thị Trấn";
            if (string.Equals(loc, "Hub_Village", StringComparison.OrdinalIgnoreCase)) return "Làng Đông";
            if (string.Equals(loc, "Forest_Primeval", StringComparison.OrdinalIgnoreCase)) return "Rừng Cổ";
            return "Nông Trại";
        }

        public static string GetDaypartName(TimeOfDay dp) => dp switch
        {
            TimeOfDay.Sang => "Sáng",
            TimeOfDay.Trua => "Trưa",
            TimeOfDay.Chieu => "Chiều",
            TimeOfDay.Toi => "Đêm",
            _ => "Sáng"
        };

        private static string GetDaypartGameplayRule(TimeOfDay dp) => dp switch
        {
            TimeOfDay.Sang => "Sáng (06–10h): Thể lực ngoài trời −5% hao, Sinh cảnh hồi +5%, đàn heo ăn khỏe +5%.",
            TimeOfDay.Trua => "Trưa (10–14h): Nắng gắt đỉnh điểm — Thể lực ngoài trời +10% hao, heo khát nước +10%.",
            TimeOfDay.Chieu => "Chiều (14–18h): Hoàng hôn dịu mát — Chợ Ngày sức mua +10%, gia cố tháp canh trước tối.",
            TimeOfDay.Toi => "Đêm (18–06h): Tầm nhìn −15%, quái & kẻ trộm hoạt động mạnh, Chợ Đêm mở ô vàng.",
            _ => ""
        };

        public static string GetWeatherStem(WeatherType w) => w switch
        {
            WeatherType.QuangDang => "quang_dang",
            WeatherType.NhieuMay => "nhieu_may",
            WeatherType.NangGat => "nang_gat",
            WeatherType.Mua => "mua",
            WeatherType.GiongBao => "giong_bao",
            WeatherType.SuongMu => "suong_mu",
            WeatherType.RetDam => "ret_dam",
            WeatherType.NomAm => "nom_am",
            WeatherType.GioKho => "gio_kho",
            WeatherType.MuaBucXa => "mua_buc_xa",
            _ => "quang_dang"
        };

        public static string GetWeatherVietnameseName(WeatherType w) => w switch
        {
            WeatherType.QuangDang => "Quang Đãng",
            WeatherType.NhieuMay => "Nhiều Mây",
            WeatherType.NangGat => "Nắng Gắt",
            WeatherType.Mua => "Mưa Rào",
            WeatherType.GiongBao => "Giông Bão",
            WeatherType.SuongMu => "Sương Mù",
            WeatherType.RetDam => "Rét Đậm",
            WeatherType.NomAm => "Nồm Ẩm",
            WeatherType.GioKho => "Gió Khô",
            WeatherType.MuaBucXa => "Mưa Bức Xạ",
            _ => "Quang Đãng"
        };

        private static string GetWeatherGameplayEffectSummary(WeatherType w) => w switch
        {
            WeatherType.QuangDang => "Trời đẹp: Heo tăng trưởng chuẩn, tầm nhìn bản đồ 100%, không phạt Thể lực.",
            WeatherType.NhieuMay => "Mát dịu: Không lo sốc nhiệt, thích hợp chăn thả đàn heo ngoài đồng.",
            WeatherType.NangGat => "Nóng bức: Ở ngoài 2h (10–16h) dễ bị Cảm Nắng (−10% trần Thể lực); heo dễ Sốc Nhiệt.",
            WeatherType.Mua => "Mưa: Ở ngoài 1h bị Ướt Lạnh (Miễn dịch hao ×1.5, Thể lực +5% hao) trừ khi mặc Áo Mưa Dầu.",
            WeatherType.GiongBao => "Nguy hiểm: Xa mái che >15m có 3%/giờ bị Sét Đánh (−40 HP, choáng 3s) & dễ dính Cảm Gió.",
            WeatherType.SuongMu => "Sương dày: Gây Lạc Hướng (Minimap giới hạn 15m) trừ khi đeo Mặt Dây Đèn Bão.",
            WeatherType.RetDam => "Giá lạnh: Gây Cóng Tay (thao tác chậm +15%) trừ khi mặc Quần Da Lót Bông; heo tốn cám giữ ấm.",
            WeatherType.NomAm => "Ẩm thấp: Mầm bệnh chuồng trại dễ phát sinh — chú ý vệ sinh và kháng thể đàn heo.",
            WeatherType.GioKho => "Hanh khô: Gây Khô Rát (chạy nhanh tốn thêm +0.2 Thể lực/giây); heo mau khát nước.",
            WeatherType.MuaBucXa => "Độc xạ: Mỗi giờ ngoài trời tích tầng Nhiễm Xạ (−4 Miễn dịch/tầng, ≥3 tầng giảm 50% hồi HP).",
            _ => ""
        };
    }
}
