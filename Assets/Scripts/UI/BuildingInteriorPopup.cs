using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Stardew Valley-style interactive cutaway building interior popup.
    /// Allows players to step inside the 4 key farm/village buildings, walk their protagonist avatar
    /// (An or Khoa) between 4 interactive stations per room, inspect live farm/herd/defense/market state,
    /// and trigger real Core commands or specialized sub-popups with a single tap/click.
    /// Also provides direct world tap-to-inspect on pigs and tap-to-enter on buildings, plus [H] shortcut.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingInteriorPopup : MonoBehaviour
    {
        public enum InteriorRoom
        {
            NhaChinh = 0,           // 1. Nhà Chính & Bếp Lò
            ChuongTraiLon = 1,      // 2. Chuồng Trại & Y Tế
            XuongRenPhongThu = 2,   // 3. Xưởng Rèn & Phòng Thủ
            QuanBaSauTramTin = 3    // 4. Quán Bà Sáu & Trạm Tin
        }

        public static BuildingInteriorPopup Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.rootModal != null && Instance.rootModal.activeSelf;

        public InteriorRoom CurrentRoom { get; private set; } = InteriorRoom.NhaChinh;

        private Canvas popupCanvas;
        private GameObject rootModal;
        private RectTransform windowRect;
        private TextMeshProUGUI headerTitleText;
        private TextMeshProUGUI clockAndGoldText;
        private Image roomBackdropImage;
        private RectTransform roomViewportRect;
        private RectTransform avatarRootRect;
        private Image avatarBodyImage;
        private TextMeshProUGUI avatarNameText;
        private TextMeshProUGUI statusBannerText;
        private RectTransform miniPigContainer;

        private readonly List<Image> roomTabImages = new List<Image>();
        private readonly List<TextMeshProUGUI> roomTabTexts = new List<TextMeshProUGUI>();
        private readonly List<StationHotspotUI> stationHotspots = new List<StationHotspotUI>();
        private readonly List<Image> miniPigImages = new List<Image>();

        private Vector2 avatarTargetPos = new Vector2(0f, -36f);
        private int selectedStationIndex = -1;
        private float refreshTimer;

        private sealed class StationHotspotUI
        {
            public RectTransform Root;
            public Image FrameImage;
            public Image IconImage;
            public TextMeshProUGUI TitleText;
            public TextMeshProUGUI StatusText;
            public TextMeshProUGUI ActionBadgeText;
            public Vector2 AvatarStandOffset;
            public Action OnActivate;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (FindFirstObjectByType<MobileGameController>() != null || FindFirstObjectByType<PixelWorldScene>() != null)
            {
                Ensure();
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindFirstObjectByType<MobileGameController>() != null || FindFirstObjectByType<PixelWorldScene>() != null)
            {
                Ensure();
            }
        }

        public static BuildingInteriorPopup Ensure()
        {
            if (Instance != null)
            {
                Instance.EnsureUIExists();
                return Instance;
            }

            var existing = FindFirstObjectByType<BuildingInteriorPopup>();
            if (existing != null)
            {
                Instance = existing;
                existing.EnsureUIExists();
                return existing;
            }

            var go = new GameObject("BuildingInteriorPopup_Canvas");
            DontDestroyOnLoad(go);
            var popup = go.AddComponent<BuildingInteriorPopup>();
            popup.EnsureUIExists();
            return popup;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureUIExists();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(InteriorRoom room = InteriorRoom.NhaChinh)
        {
            EnsureUIExists();
            if (rootModal == null) return;

            CurrentRoom = room;
            selectedStationIndex = -1;
            avatarTargetPos = new Vector2(0f, -32f);
            if (avatarRootRect != null) avatarRootRect.anchoredPosition = avatarTargetPos;

            rootModal.SetActive(true);
            rootModal.transform.SetAsLastSibling();
            RefreshRoomView();
            SetStatusBanner($"Đã bước vào {GetRoomTitle(room)} — Chạm vào từng trạm để di chuyển nhân vật & thao tác.");
        }

        public void Close()
        {
            if (rootModal != null && rootModal.activeSelf)
            {
                rootModal.SetActive(false);
            }
        }

        public void Hide() => Close();

        public void Toggle(InteriorRoom room = InteriorRoom.NhaChinh)
        {
            if (IsOpen) Close();
            else Open(room);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                if (IsOpen)
                {
                    Close();
                    return;
                }
                if (!UISpriteLoader.IsAnyModalOrDialogueOpen())
                {
                    Open(ResolveDefaultRoomForPlayerPosition());
                    return;
                }
            }

            if (!IsOpen)
            {
                HandleDirectWorldTap();
                return;
            }

            if (avatarRootRect != null)
            {
                avatarRootRect.anchoredPosition = Vector2.Lerp(
                    avatarRootRect.anchoredPosition,
                    avatarTargetPos,
                    Time.unscaledDeltaTime * 10f);
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.4f;
                RefreshLiveStationLabels();
            }
        }

        private InteriorRoom ResolveDefaultRoomForPlayerPosition()
        {
            var player = PlayerMobileController.Instance;
            if (player != null)
            {
                Vector2 pos = player.transform.position;
                var shelters = FindObjectsByType<Shelter2DView>(FindObjectsSortMode.None);
                for (int i = 0; i < shelters.Length; i++)
                {
                    if (shelters[i] != null && Vector2.Distance(pos, shelters[i].transform.position) <= 6f)
                        return InteriorRoom.ChuongTraiLon;
                }

                var buildings = FindObjectsByType<CustomBuildingController>(FindObjectsSortMode.None);
                float bestDist = 8f;
                CustomBuildingController nearest = null;
                for (int i = 0; i < buildings.Length; i++)
                {
                    var b = buildings[i];
                    if (b == null || !b.isActiveAndEnabled) continue;
                    float d = Vector2.Distance(pos, b.transform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        nearest = b;
                    }
                }
                if (nearest != null) return MapBuildingToRoom(nearest);
            }
            return InteriorRoom.NhaChinh;
        }

        private static InteriorRoom MapBuildingToRoom(CustomBuildingController b)
        {
            if (b == null) return InteriorRoom.NhaChinh;
            string nm = (b.gameObject.name ?? "").ToLowerInvariant();
            if (nm.Contains("barn") || nm.Contains("shelter") || nm.Contains("chuong") || nm.Contains("vet") || nm.Contains("breed"))
                return InteriorRoom.ChuongTraiLon;
            if (nm.Contains("forge") || nm.Contains("smith") || nm.Contains("defense") || nm.Contains("hall") || nm.Contains("tower") || nm.Contains("xuong"))
                return InteriorRoom.XuongRenPhongThu;
            if (nm.Contains("market") || nm.Contains("barter") || nm.Contains("intel") || nm.Contains("apprais") || nm.Contains("ba_sau") || nm.Contains("quan"))
                return InteriorRoom.QuanBaSauTramTin;

            switch (b.AssignedFunction)
            {
                case BuildingInteractionFunction.GiaoDichDanLang:
                case BuildingInteractionFunction.DocBangTinLore:
                case BuildingInteractionFunction.BanTinhBao:
                    return InteriorRoom.QuanBaSauTramTin;
                case BuildingInteractionFunction.MoKhoLuuTru:
                case BuildingInteractionFunction.NghiNgoiGiuong:
                    return InteriorRoom.NhaChinh;
                default:
                    return InteriorRoom.NhaChinh;
            }
        }

        private void HandleDirectWorldTap()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (UISpriteLoader.IsAnyModalOrDialogueOpen()) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPt3 = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 tapPos = new Vector2(worldPt3.x, worldPt3.y);

            // 1. Check direct tap on any PigAgentView (within 1.1m) -> open PigInspectPopup directly
            var pigs = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            PigAgentView nearestPig = null;
            float bestPigDist = 1.1f;
            for (int i = 0; i < pigs.Length; i++)
            {
                var pv = pigs[i];
                if (pv == null || !pv.isActiveAndEnabled || pv.PigModel == null) continue;
                float d = Vector2.Distance(tapPos, pv.transform.position);
                if (d <= bestPigDist)
                {
                    bestPigDist = d;
                    nearestPig = pv;
                }
            }
            if (nearestPig != null)
            {
                PigInspectPopup.Instance?.Show(nearestPig.PigModel);
                ToastOverlay.Show($"Đang xem chi tiết heo: {nearestPig.PigModel.Name}");
                return;
            }

            // 2. Check direct tap on Shelter2DView / Feeder2DView / WaterTrough2DView (within 1.8m) -> open ChuongTraiLon
            var shelters = FindObjectsByType<Shelter2DView>(FindObjectsSortMode.None);
            for (int i = 0; i < shelters.Length; i++)
            {
                if (shelters[i] != null && shelters[i].isActiveAndEnabled &&
                    Vector2.Distance(tapPos, shelters[i].transform.position) <= 1.8f)
                {
                    Open(InteriorRoom.ChuongTraiLon);
                    ToastOverlay.Show("Đã vào Chuồng Trại & Y Tế [H]");
                    return;
                }
            }

            // 3. Check direct tap on CustomBuildingController (within 1.8m) -> open matching InteriorRoom
            var buildings = FindObjectsByType<CustomBuildingController>(FindObjectsSortMode.None);
            CustomBuildingController nearestBldg = null;
            float bestBldgDist = 1.8f;
            for (int i = 0; i < buildings.Length; i++)
            {
                var b = buildings[i];
                if (b == null || !b.isActiveAndEnabled) continue;
                float d = Vector2.Distance(tapPos, b.transform.position);
                if (d <= bestBldgDist)
                {
                    bestBldgDist = d;
                    nearestBldg = b;
                }
            }
            if (nearestBldg != null)
            {
                var room = MapBuildingToRoom(nearestBldg);
                Open(room);
                ToastOverlay.Show($"Đã bước vào: {GetRoomTitle(room)}");
            }
        }

        private GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        private void EnsureUIExists()
        {
            if (rootModal != null) return;

            popupCanvas = GetComponent<Canvas>();
            if (popupCanvas == null) popupCanvas = gameObject.AddComponent<Canvas>();
            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            popupCanvas.sortingOrder = 108;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            // Full-screen dimmed backdrop
            rootModal = new GameObject("InteriorModal_Root", typeof(RectTransform), typeof(Image), typeof(Button));
            rootModal.transform.SetParent(transform, false);
            var rootRt = (RectTransform)rootModal.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var dimImg = rootModal.GetComponent<Image>();
            dimImg.color = new Color(0.04f, 0.06f, 0.10f, 0.80f);
            rootModal.GetComponent<Button>().onClick.AddListener(Close);

            // Centered 920x540 2.5D wooden frame window
            var winGo = new GameObject("InteriorWindow_920x540", typeof(RectTransform), typeof(Image));
            winGo.transform.SetParent(rootModal.transform, false);
            windowRect = (RectTransform)winGo.transform;
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(920f, 540f);

            var winImg = winGo.GetComponent<Image>();
            winImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            winImg.type = Image.Type.Sliced;
            winImg.color = new Color(0.96f, 0.92f, 0.84f, 1f);
            winImg.raycastTarget = true;

            // Inner parchment lining
            var parchGo = new GameObject("ParchmentInner", typeof(RectTransform), typeof(Image));
            parchGo.transform.SetParent(windowRect, false);
            var parchRt = (RectTransform)parchGo.transform;
            parchRt.anchorMin = Vector2.zero;
            parchRt.anchorMax = Vector2.one;
            parchRt.offsetMin = new Vector2(12f, 12f);
            parchRt.offsetMax = new Vector2(-12f, -12f);
            var parchImg = parchGo.GetComponent<Image>();
            parchImg.sprite = UISpriteLoader.GetFrameParchment();
            parchImg.type = Image.Type.Sliced;
            parchImg.color = new Color(0.22f, 0.16f, 0.12f, 0.98f);
            parchImg.raycastTarget = false;

            // Header bar
            var headerGo = new GameObject("HeaderBanner", typeof(RectTransform), typeof(Image));
            headerGo.transform.SetParent(windowRect, false);
            var headerRt = (RectTransform)headerGo.transform;
            headerRt.anchorMin = new Vector2(0.5f, 1f);
            headerRt.anchorMax = new Vector2(0.5f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -14f);
            headerRt.sizeDelta = new Vector2(884f, 40f);
            var headerImg = headerGo.GetComponent<Image>();
            headerImg.sprite = UISpriteLoader.GetFrameSlotSelected() ?? UISpriteLoader.GetFrameSlotNormal();
            headerImg.type = Image.Type.Sliced;
            headerImg.color = new Color(0.42f, 0.20f, 0.14f, 0.98f);
            headerImg.raycastTarget = false;

            headerTitleText = CreateText("HeaderTitle", headerRt, new Vector2(-130f, 0f), new Vector2(560f, 34f), 17f,
                new Color(1f, 0.93f, 0.68f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

            clockAndGoldText = CreateText("ClockAndGold", headerRt, new Vector2(210f, 0f), new Vector2(280f, 32f), 13.5f,
                new Color(0.95f, 0.88f, 0.72f), TextAlignmentOptions.MidlineRight, FontStyles.Bold);

            var closeBtn = CreateStyledButton("Btn_Close", headerRt, new Vector2(396f, 0f), new Vector2(76f, 30f),
                new Color(0.68f, 0.24f, 0.20f, 0.98f), "Đóng [ESC]", 12f, Close, out _);
            closeBtn.transform.SetAsLastSibling();

            // 4 Room Navigation Tabs
            roomTabImages.Clear();
            roomTabTexts.Clear();
            string[] tabTitles =
            {
                "1. Nhà Chính",
                "2. Chuồng Trại & Y Tế",
                "3. Xưởng Rèn & Phòng Thủ",
                "4. Quán Bà Sáu & Trạm Tin"
            };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var roomEnum = (InteriorRoom)idx;
                var tabBtn = CreateStyledButton(
                    $"TabRoom_{idx}",
                    windowRect,
                    new Vector2(-333f + idx * 222f, 198f),
                    new Vector2(214f, 32f),
                    new Color(0.32f, 0.23f, 0.16f, 0.96f),
                    tabTitles[idx],
                    12.5f,
                    () =>
                    {
                        if (CurrentRoom == roomEnum)
                        {
                            RefreshRoomView();
                            string msg = $"Đang ở {GetRoomTitle(roomEnum)} — Bấm vào 1 trong 4 trạm bên dưới để tương tác.";
                            SetStatusBanner(msg);
                            ToastOverlay.Show(msg);
                        }
                        else
                        {
                            CurrentRoom = roomEnum;
                            selectedStationIndex = -1;
                            avatarTargetPos = new Vector2(0f, -32f);
                            RefreshRoomView();
                            string msg = $"Đã chuyển sang: {GetRoomTitle(roomEnum)}";
                            SetStatusBanner(msg);
                            ToastOverlay.Show(msg);
                        }
                    },
                    out var tabTmp);
                roomTabImages.Add(tabBtn.GetComponent<Image>());
                roomTabTexts.Add(tabTmp);
            }

            // 880x360 Cutaway Room Viewport
            var viewportGo = new GameObject("RoomCutawayViewport_880x360", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(windowRect, false);
            roomViewportRect = (RectTransform)viewportGo.transform;
            roomViewportRect.anchorMin = new Vector2(0.5f, 0.5f);
            roomViewportRect.anchorMax = new Vector2(0.5f, 0.5f);
            roomViewportRect.pivot = new Vector2(0.5f, 0.5f);
            roomViewportRect.anchoredPosition = new Vector2(0f, -4f);
            roomViewportRect.sizeDelta = new Vector2(880f, 356f);

            var vpFrameImg = viewportGo.GetComponent<Image>();
            vpFrameImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            vpFrameImg.type = Image.Type.Sliced;
            vpFrameImg.color = new Color(0.12f, 0.09f, 0.07f, 0.98f);
            vpFrameImg.raycastTarget = false;

            // Room Backdrop Image inside Viewport
            var bgGo = new GameObject("InteriorBackdrop", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(roomViewportRect, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(4f, 4f);
            bgRt.offsetMax = new Vector2(-4f, -4f);
            roomBackdropImage = bgGo.GetComponent<Image>();
            roomBackdropImage.color = new Color(0.92f, 0.88f, 0.82f, 0.92f);
            roomBackdropImage.raycastTarget = false;

            // Warm wooden floor rug / walkway overlay in the center of the cutaway room
            var walkwayGo = new GameObject("CenterWalkway", typeof(RectTransform), typeof(Image));
            walkwayGo.transform.SetParent(roomViewportRect, false);
            var wwRt = (RectTransform)walkwayGo.transform;
            wwRt.anchorMin = new Vector2(0.5f, 0.5f);
            wwRt.anchorMax = new Vector2(0.5f, 0.5f);
            wwRt.anchoredPosition = new Vector2(0f, -18f);
            wwRt.sizeDelta = new Vector2(360f, 160f);
            var wwImg = walkwayGo.GetComponent<Image>();
            wwImg.sprite = UISpriteLoader.GetFrameParchment();
            wwImg.type = Image.Type.Sliced;
            wwImg.color = new Color(0.28f, 0.20f, 0.14f, 0.52f);
            wwImg.raycastTarget = false;

            // Mini pig pen container (visible in ChuongTraiLon)
            var pigPenGo = new GameObject("MiniPigPenContainer", typeof(RectTransform));
            pigPenGo.transform.SetParent(roomViewportRect, false);
            miniPigContainer = (RectTransform)pigPenGo.transform;
            miniPigContainer.anchorMin = new Vector2(0.5f, 0.5f);
            miniPigContainer.anchorMax = new Vector2(0.5f, 0.5f);
            miniPigContainer.anchoredPosition = new Vector2(0f, -20f);
            miniPigContainer.sizeDelta = new Vector2(280f, 120f);

            // Build the 4 Interactive Station Hotspots inside the room
            stationHotspots.Clear();
            Vector2[] stationPositions =
            {
                new Vector2(-268f, 84f),
                new Vector2(268f, 84f),
                new Vector2(-268f, -84f),
                new Vector2(268f, -84f)
            };
            Vector2[] avatarStandOffsets =
            {
                new Vector2(-92f, 42f),
                new Vector2(92f, 42f),
                new Vector2(-92f, -48f),
                new Vector2(92f, -48f)
            };

            for (int s = 0; s < 4; s++)
            {
                int slotIdx = s;
                stationHotspots.Add(BuildStationHotspot(slotIdx, stationPositions[slotIdx], avatarStandOffsets[slotIdx]));
            }

            // Protagonist Avatar (An or Khoa) inside the room
            var avRootGo = new GameObject("ProtagonistAvatar", typeof(RectTransform));
            avRootGo.transform.SetParent(roomViewportRect, false);
            avatarRootRect = (RectTransform)avRootGo.transform;
            avatarRootRect.anchorMin = new Vector2(0.5f, 0.5f);
            avatarRootRect.anchorMax = new Vector2(0.5f, 0.5f);
            avatarRootRect.pivot = new Vector2(0.5f, 0.5f);
            avatarRootRect.anchoredPosition = avatarTargetPos;
            avatarRootRect.sizeDelta = new Vector2(84f, 96f);

            var shadowGo = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            shadowGo.transform.SetParent(avatarRootRect, false);
            var shRt = (RectTransform)shadowGo.transform;
            shRt.anchoredPosition = new Vector2(0f, -34f);
            shRt.sizeDelta = new Vector2(54f, 22f);
            var shImg = shadowGo.GetComponent<Image>();
            shImg.sprite = UISpriteLoader.GetSoftShadowSprite();
            shImg.color = new Color(0f, 0f, 0f, 0.65f);
            shImg.raycastTarget = false;

            var bodyGo = new GameObject("AvatarSprite", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(avatarRootRect, false);
            var bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchoredPosition = new Vector2(0f, 6f);
            bodyRt.sizeDelta = new Vector2(72f, 84f);
            avatarBodyImage = bodyGo.GetComponent<Image>();
            avatarBodyImage.preserveAspect = true;
            avatarBodyImage.raycastTarget = false;

            var badgeGo = new GameObject("AvatarNamePill", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(avatarRootRect, false);
            var badgeRt = (RectTransform)badgeGo.transform;
            badgeRt.anchoredPosition = new Vector2(0f, -44f);
            badgeRt.sizeDelta = new Vector2(116f, 20f);
            var badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = new Color(0.14f, 0.10f, 0.08f, 0.92f);
            badgeImg.raycastTarget = false;

            avatarNameText = CreateText("AvatarLabel", badgeRt, Vector2.zero, new Vector2(112f, 18f), 11f,
                new Color(1f, 0.92f, 0.64f), TextAlignmentOptions.Center, FontStyles.Bold);

            // Bottom Status & Quick Action Bar
            var footerGo = new GameObject("FooterStatusBanner", typeof(RectTransform), typeof(Image));
            footerGo.transform.SetParent(windowRect, false);
            var footerRt = (RectTransform)footerGo.transform;
            footerRt.anchorMin = new Vector2(0.5f, 0f);
            footerRt.anchorMax = new Vector2(0.5f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.anchoredPosition = new Vector2(-95f, 18f);
            footerRt.sizeDelta = new Vector2(690f, 44f);
            var footerImg = footerGo.GetComponent<Image>();
            footerImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            footerImg.type = Image.Type.Sliced;
            footerImg.color = new Color(0.16f, 0.12f, 0.09f, 0.96f);
            footerImg.raycastTarget = false;

            statusBannerText = CreateText("StatusBannerText", footerRt, Vector2.zero, new Vector2(670f, 38f), 13f,
                new Color(0.86f, 0.96f, 0.78f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal);

            // Quick link button back to Sổ Trại [F]
            CreateStyledButton(
                "Btn_OpenFarmLedger",
                windowRect,
                new Vector2(352f, -230f),
                new Vector2(180f, 42f),
                new Color(0.46f, 0.32f, 0.18f, 0.98f),
                "Sổ Trại & Đàn [F]",
                13f,
                () =>
                {
                    Close();
                    FarmOverviewPopup.Ensure()?.Open();
                },
                out _);

            rootModal.SetActive(false);
        }

        private StationHotspotUI BuildStationHotspot(int index, Vector2 anchoredPos, Vector2 avatarStandOffset)
        {
            var cardGo = new GameObject($"StationHotspot_{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGo.transform.SetParent(roomViewportRect, false);
            var rt = (RectTransform)cardGo.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(316f, 134f);

            var frameImg = cardGo.GetComponent<Image>();
            frameImg.sprite = UISpriteLoader.GetFrameSlotSelected() ?? UISpriteLoader.GetFrameWoodPanel();
            frameImg.type = Image.Type.Sliced;
            frameImg.color = new Color(0.24f, 0.17f, 0.12f, 0.94f);
            frameImg.raycastTarget = true;

            var iconBoxGo = new GameObject("IconBox", typeof(RectTransform), typeof(Image));
            iconBoxGo.transform.SetParent(rt, false);
            var ibRt = (RectTransform)iconBoxGo.transform;
            ibRt.anchorMin = new Vector2(0f, 0.5f);
            ibRt.anchorMax = new Vector2(0f, 0.5f);
            ibRt.pivot = new Vector2(0f, 0.5f);
            ibRt.anchoredPosition = new Vector2(12f, 8f);
            ibRt.sizeDelta = new Vector2(64f, 64f);
            var ibImg = iconBoxGo.GetComponent<Image>();
            ibImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            ibImg.type = Image.Type.Sliced;
            ibImg.color = new Color(0.16f, 0.12f, 0.09f, 0.96f);
            ibImg.raycastTarget = false;

            var iconGo = new GameObject("IconSprite", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(ibRt, false);
            var icRt = (RectTransform)iconGo.transform;
            icRt.anchorMin = Vector2.zero;
            icRt.anchorMax = Vector2.one;
            icRt.offsetMin = new Vector2(6f, 6f);
            icRt.offsetMax = new Vector2(-6f, -6f);
            var icImg = iconGo.GetComponent<Image>();
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;

            var titleTmp = CreateText("StationTitle", rt, new Vector2(36f, 36f), new Vector2(216f, 28f), 14.5f,
                new Color(1f, 0.92f, 0.62f), TextAlignmentOptions.Left, FontStyles.Bold);

            var statusTmp = CreateText("StationStatus", rt, new Vector2(36f, -2f), new Vector2(216f, 48f), 12f,
                new Color(0.92f, 0.88f, 0.80f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
            statusTmp.textWrappingMode = TextWrappingModes.Normal;

            var badgeGo = new GameObject("ActionBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(rt, false);
            var bRt = (RectTransform)badgeGo.transform;
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 8f);
            bRt.sizeDelta = new Vector2(292f, 26f);
            var bImg = badgeGo.GetComponent<Image>();
            bImg.sprite = UISpriteLoader.GetFrameActionButton();
            bImg.type = Image.Type.Sliced;
            bImg.color = new Color(0.38f, 0.56f, 0.28f, 0.96f);
            bImg.raycastTarget = false;

            var actionTmp = CreateText("ActionLabel", bRt, Vector2.zero, new Vector2(284f, 24f), 12f,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

            var hotspot = new StationHotspotUI
            {
                Root = rt,
                FrameImage = frameImg,
                IconImage = icImg,
                TitleText = titleTmp,
                StatusText = statusTmp,
                ActionBadgeText = actionTmp,
                AvatarStandOffset = avatarStandOffset
            };

            cardGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedStationIndex = index;
                avatarTargetPos = hotspot.AvatarStandOffset;
                HighlightSelectedStation();
                hotspot.OnActivate?.Invoke();
            });

            return hotspot;
        }

        private void HighlightSelectedStation()
        {
            for (int i = 0; i < stationHotspots.Count; i++)
            {
                bool sel = i == selectedStationIndex;
                stationHotspots[i].FrameImage.color = sel
                    ? new Color(0.44f, 0.30f, 0.16f, 0.99f)
                    : new Color(0.24f, 0.17f, 0.12f, 0.94f);
            }
        }

        public void RefreshRoomView()
        {
            var engine = Engine;
            if (headerTitleText != null)
            {
                headerTitleText.text = $"<b>{GetRoomTitle(CurrentRoom)}</b> <size=12><color=#E2E8F0>[Phím H]</color></size>";
            }

            for (int i = 0; i < roomTabImages.Count; i++)
            {
                bool active = i == (int)CurrentRoom;
                roomTabImages[i].sprite = active ? UISpriteLoader.GetFrameSlotSelected() : UISpriteLoader.GetFrameSlotNormal();
                roomTabImages[i].color = active
                    ? new Color(0.56f, 0.36f, 0.18f, 1f)
                    : new Color(0.26f, 0.19f, 0.14f, 0.94f);
                roomTabTexts[i].color = active
                    ? new Color(1f, 0.95f, 0.72f)
                    : new Color(0.82f, 0.76f, 0.66f);
            }

            if (roomBackdropImage != null)
            {
                roomBackdropImage.sprite = ResolveRoomBackdropSprite(CurrentRoom);
            }

            if (engine != null && avatarBodyImage != null)
            {
                var id = engine.Character != null ? engine.Character.Identity : PlayerId.An;
                avatarBodyImage.sprite = UISpriteLoader.GetHeroPaperDollBaseSprite(id)
                    ?? UISpriteLoader.LoadCharacterCellZero(id == PlayerId.An ? "an" : "khoa");
                if (avatarNameText != null)
                {
                    string roleName = id == PlayerId.An ? "An (Mắt Nhà Nghề)" : "Khoa (Sổ Hai Cột)";
                    avatarNameText.text = roleName;
                }
            }

            HighlightSelectedStation();
            RefreshMiniPigPen();
            RefreshLiveStationLabels();
        }

        private void RefreshMiniPigPen()
        {
            if (miniPigContainer == null) return;
            bool showPigs = CurrentRoom == InteriorRoom.ChuongTraiLon;
            miniPigContainer.gameObject.SetActive(showPigs);
            if (!showPigs) return;

            var engine = Engine;
            if (engine == null) return;

            for (int i = 0; i < miniPigImages.Count; i++)
            {
                if (miniPigImages[i] != null) Destroy(miniPigImages[i].gameObject);
            }
            miniPigImages.Clear();

            var living = engine.Pigs.Where(p => p.IsAlive).Take(6).ToList();
            for (int i = 0; i < living.Count; i++)
            {
                var pig = living[i];
                var pigGo = new GameObject($"InteriorPig_{pig.Id}", typeof(RectTransform), typeof(Image), typeof(Button));
                pigGo.transform.SetParent(miniPigContainer, false);
                var prt = (RectTransform)pigGo.transform;
                float x = (i - (living.Count - 1) * 0.5f) * 42f;
                float y = (i % 2 == 0) ? 14f : -16f;
                prt.anchoredPosition = new Vector2(x, y);
                prt.sizeDelta = new Vector2(40f, 40f);

                var pImg = pigGo.GetComponent<Image>();
                pImg.sprite = UISpriteLoader.GetPigAvatar(pig.GeneLine);
                pImg.preserveAspect = true;

                var capturedPig = pig;
                pigGo.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Close();
                    PigInspectPopup.Instance?.Show(capturedPig);
                    ToastOverlay.Show($"Đang khám heo: {capturedPig.Name}");
                });
                miniPigImages.Add(pImg);
            }
        }

        private void RefreshLiveStationLabels()
        {
            var engine = Engine;
            if (engine == null || stationHotspots.Count < 4) return;

            if (clockAndGoldText != null)
            {
                clockAndGoldText.text = $"Ngày {engine.Clock.CurrentDay} · {engine.Clock.CurrentHour:00}:{engine.Clock.CurrentMinute:00}  |  <color=#FDE047>{engine.Farm.Gold:N0} G</color>";
            }

            switch (CurrentRoom)
            {
                case InteriorRoom.NhaChinh:
                    ConfigureNhaChinhStations(engine);
                    break;
                case InteriorRoom.ChuongTraiLon:
                    ConfigureChuongTraiStations(engine);
                    break;
                case InteriorRoom.XuongRenPhongThu:
                    ConfigureXuongRenStations(engine);
                    break;
                case InteriorRoom.QuanBaSauTramTin:
                    ConfigureQuanBaSauStations(engine);
                    break;
            }
        }

        private void ConfigureNhaChinhStations(GameEngine engine)
        {
            // Station 1: Giường Ngủ Gỗ
            float curSta = engine.Character != null ? engine.Character.Stamina.CurrentStamina : 0f;
            float maxSta = engine.Character != null ? engine.Character.GetEffectiveMaxStamina() : 100f;
            string sleepBlock = PlayerRestCommands.SleepBlockedReason(engine);
            bool canSleepNight = string.IsNullOrEmpty(sleepBlock);
            SetupStation(
                0,
                MythUi.Icon("actions", "ngu", 24) ?? UISpriteLoader.GetIconEnergyBolt(),
                "1. Giường Ngủ Gỗ",
                $"Thể lực: <color=#86EFAC>{curSta:0}/{maxSta:0} TL</color>\n{(canSleepNight ? "Đã đến giờ ngủ qua đêm (sang 06:00)" : "Ban ngày: Chợp mắt 1 giờ hồi Thể Lực")}",
                canSleepNight ? "NGỦ QUA ĐÊM (LƯU GAME)" : "NGHỈ NGƠI 1 GIỜ (+TL)",
                () =>
                {
                    string block = PlayerRestCommands.SleepBlockedReason(engine);
                    if (string.IsNullOrEmpty(block))
                    {
                        var res = PlayerRestCommands.Sleep(engine);
                        MobileGameController.Instance?.SaveGameToPersistentStorage();
                        MobileGameController.Instance?.OnStateUpdated?.Invoke();
                        SetStatusBanner(res.message);
                        ToastOverlay.Show(res.message);
                    }
                    else
                    {
                        var restRes = PlayerRestCommands.Rest(engine, 1);
                        MobileGameController.Instance?.OnStateUpdated?.Invoke();
                        string msg = restRes.success ? restRes.message : $"{block} ({restRes.message})";
                        SetStatusBanner(msg);
                        ToastOverlay.Show(msg);
                    }
                    RefreshLiveStationLabels();
                });

            // Station 2: Rương Kho Vật Tư (100 Ô)
            int usedWarehouse = engine.Inventory != null ? engine.Inventory.Slots.Count : 0;
            SetupStation(
                1,
                MythUi.Icon("places", "kho", 24) ?? UISpriteLoader.GetIconBackpack(),
                "2. Rương Kho Vật Tư (100 Ô)",
                $"Đang cất: <color=#FDE047>{usedWarehouse}/100 ô</color>\nKho cám dự trữ: <b>{engine.Farm.FeedStockKg:0.#} kg</b>",
                "MỞ RƯƠNG KHO 100 Ô",
                () =>
                {
                    Close();
                    var bp = BackpackPopup.Ensure();
                    if (bp != null) bp.ShowWarehouse(); // the chest in the house is the warehouse itself (in person)
                    ToastOverlay.Show($"Đã mở Rương Kho Vật Tư ({usedWarehouse}/100 ô)");
                });

            // Station 3: Bàn Sổ Sách & Sổ Hai Cột
            string mainQuestTitle = engine.Quests?.ActiveMain != null ? engine.Quests.ActiveMain.Title : "Đã hoàn thành tuyến chính";
            SetupStation(
                2,
                MythUi.Icon("misc", "tinh_bao", 24) ?? UISpriteLoader.GetBlueprintSprite(),
                "3. Bàn Sổ Sách & Sổ Hai Cột",
                $"Nhiệm vụ: <color=#FDE047>{mainQuestTitle}</color>\nCấp trại: {engine.Farm.FarmLevel} · Sinh cảnh: {engine.Farm.HabitatIndex}/100",
                "MỞ SỔ TAY NHIỆM VỤ [N]",
                () =>
                {
                    Close();
                    QuestTrackerPanel.Ensure();
                    if (QuestTrackerPanel.Instance != null) QuestTrackerPanel.Instance.Open();
                    ToastOverlay.Show("Đã mở Sổ Tay Trang Trại & Nhiệm Vụ");
                });

            // Station 4: Bếp Lò Bà Sáu Cho Mượn
            int mealsToday = engine.Character != null ? engine.Character.Stamina.MealsToday : 0;
            int condition = engine.Character != null ? engine.Character.Stamina.Condition : 100;
            SetupStation(
                3,
                UISpriteLoader.GetLootSprite("BinhHoiLuc") ?? UISpriteLoader.GetIconHeartFull(),
                "4. Bếp Lò & Bàn Ăn Nóng",
                $"Cơm nóng hôm nay: <b>{mealsToday}/{StaminaSystem.MaxMealsPerDay} bữa</b>\nThể trạng bền bỉ: <color=#86EFAC>{condition}/100</color>",
                "ĂN CƠM NÓNG HỒI SỨC",
                () =>
                {
                    var mealRes = PlayerRestCommands.EatMeal(engine, goodMeal: true);
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    SetStatusBanner(mealRes.message);
                    ToastOverlay.Show(mealRes.message);
                    RefreshLiveStationLabels();
                });
        }

        private void ConfigureChuongTraiStations(GameEngine engine)
        {
            var living = engine.Pigs.Where(p => p.IsAlive).ToList();
            int hungryCount = living.Count(p => TroughCommands.WantsFood(engine, p) || p.Hunger >= 30);

            // Station 1: Máng Cám Trung Tâm
            SetupStation(
                0,
                MythUi.Icon("actions", "cho_an", 24) ?? UISpriteLoader.GetIconFeeder(),
                "1. Máng Cám Trung Tâm",
                $"Kho cám: <color=#FDE047>{engine.Farm.FeedStockKg:0.#} kg</color>\nHeo cần ăn: <b>{hungryCount}/{living.Count} con</b>",
                "ĐỔ ĐẦY CÁM & CHO ĐÀN ĂN",
                () =>
                {
                    float totalPut = 0f;
                    if (engine.PlayableMap != null)
                    {
                        foreach (var pt in engine.PlayableMap.ServicePoints)
                        {
                            if (!pt.Feed) continue;
                            float need = Mathf.Max(0f, TroughCommands.FeederCapacityKg - pt.FeedKg);
                            if (need <= 0.01f) continue;
                            float taken = TroughCommands.TakeFeedFromStock(engine, need);
                            if (taken > 0f) totalPut += TroughCommands.FillFeeder(engine, pt.Id, taken);
                        }
                    }
                    foreach (var fv in FindObjectsByType<Feeder2DView>(FindObjectsSortMode.None))
                    {
                        if (fv != null) fv.Refill(TroughCommands.FeederCapacityKg);
                    }
                    int fedNow = 0;
                    for (int i = 0; i < living.Count; i++)
                    {
                        var p = living[i];
                        if (!p.IsIsolated && p.Mood >= 20 && p.LastPaidRationDay < engine.Clock.CurrentDay)
                        {
                            float kg = PigCareCommands.DailyFeedKilograms(engine, p);
                            if (engine.Farm.FeedStockKg >= kg)
                            {
                                engine.Farm.FeedStockKg -= kg;
                                p.LastPaidRationDay = engine.Clock.CurrentDay;
                                p.Hunger = 0;
                                p.FedToday = true;
                                fedNow++;
                            }
                        }
                    }
                    if (fedNow > 0 || totalPut > 0f) engine.Carry?.NotifyHerdFed();
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    string msg = (totalPut > 0f || fedNow > 0)
                        ? $"Đã châm máng ({totalPut:0.#} kg) & cho {fedNow} heo ăn no! Kho cám còn {engine.Farm.FeedStockKg:0.#} kg."
                        : (engine.Farm.FeedStockKg <= 0.1f ? "Kho cám đã cạn — hãy mua thêm Bao Cám tại Chợ Ngày!" : "Máng cám và đàn heo đều đã no đủ hôm nay.");
                    SetStatusBanner(msg);
                    ToastOverlay.Show(msg);
                    RefreshLiveStationLabels();
                });

            // Station 2: Bể Nước & Giếng Đá
            int overheated = living.Count(p => engine.UncooledHeatHours(p.Id) > 0f || p.ActiveDisease == DiseaseId.SayNang);
            SetupStation(
                1,
                MythUi.Icon("actions", "cho_uong", 24) ?? UISpriteLoader.GetIconWaterTrough(),
                "2. Bể Nước Sạch & Phun Mát",
                $"Đàn sống: <b>{living.Count} con</b> · Quá nhiệt: <b>{overheated} con</b>\nSinh cảnh chuồng: <color=#86EFAC>{engine.Farm.HabitatIndex}/100</color>",
                "BƠM ĐẦY BỂ NƯỚC (100L)",
                () =>
                {
                    float litersAdded = 0f;
                    if (engine.PlayableMap != null)
                    {
                        foreach (var pt in engine.PlayableMap.ServicePoints)
                        {
                            if (pt.Water) litersAdded += TroughCommands.FillBasin(engine, pt.Id, TroughCommands.BasinCapacityL);
                        }
                    }
                    foreach (var wv in FindObjectsByType<WaterTrough2DView>(FindObjectsSortMode.None))
                    {
                        if (wv != null) wv.Refill(TroughCommands.BasinCapacityL);
                    }
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    string msg = $"Đã bơm đầy tất cả bể nước sạch trong trại (+{Mathf.Max(100f, litersAdded):0} L)!";
                    SetStatusBanner(msg);
                    ToastOverlay.Show(msg);
                    RefreshLiveStationLabels();
                });

            // Station 3: Khu Phối Giống & Ổ Đẻ
            var adults = living.Where(p => p.Stage == PigStage.TruongThanh).ToList();
            int pregnant = living.Count(p => p.GestationDaysRemaining > 0);
            SetupStation(
                2,
                UISpriteLoader.GetIconHeartFull(),
                "3. Khu Phối Giống & Ổ Đẻ",
                $"Heo trưởng thành: <color=#FDE047>{adults.Count} con</color>\nĐang mang thai: <b>{pregnant} nái</b> (thai kỳ {BreedingLedger.GestationDays} ngày)",
                "CHỌN CẶP PHỐI GIỐNG",
                () =>
                {
                    if (adults.Count > 0)
                    {
                        Close();
                        BreedingPickerPopup.Open(adults[0]);
                        ToastOverlay.Show($"Đang chọn bạn phối giống cho: {adults[0].Name}");
                    }
                    else
                    {
                        string msg = "Chưa có heo Trưởng Thành đủ điều kiện phối giống.";
                        SetStatusBanner(msg);
                        ToastOverlay.Show(msg);
                    }
                });

            // Station 4: Tủ Thuốc Thú Y & Chuồng Cách Ly
            var sickPig = living.FirstOrDefault(p => p.ActiveDisease.HasValue)
                       ?? living.FirstOrDefault(p => p.IsIsolated)
                       ?? living.FirstOrDefault();
            int sickCount = living.Count(p => p.ActiveDisease.HasValue);
            int isolatedCount = living.Count(p => p.IsIsolated);
            SetupStation(
                3,
                UISpriteLoader.GetLootSprite("VacXinTongHop") ?? MythUi.Icon("actions", "giam_dinh", 24),
                "4. Tủ Thuốc & Chuồng Cách Ly",
                $"Đang bệnh: <color=#FCA5A5>{sickCount} con</color> · Cách ly: <b>{isolatedCount} con</b>\nBệnh đã giải mã: {engine.PetsAndNpcs.ResearchedDiseases.Count}/5",
                "KHÁM & CHỮA BỆNH ĐÀN HEO",
                () =>
                {
                    if (sickPig != null)
                    {
                        Close();
                        PigInspectPopup.Instance?.Show(sickPig);
                        ToastOverlay.Show($"Đang khám chi tiết cho: {sickPig.Name}");
                    }
                    else
                    {
                        string msg = "Hiện chưa có cá thể heo nào trong chuồng.";
                        SetStatusBanner(msg);
                        ToastOverlay.Show(msg);
                    }
                });
        }

        private void ConfigureXuongRenStations(GameEngine engine)
        {
            int stoneCount = engine.Inventory != null ? engine.Inventory.CountId(EnhancementTransactions.StoneId) : 0;

            // Station 1: Đe Rèn Cường Hóa
            SetupStation(
                0,
                MythUi.Icon("actions", "cuong_hoa", 24) ?? UISpriteLoader.GetLootSprite(EnhancementTransactions.StoneId),
                "1. Đe Rèn Cường Hóa (+1..+5)",
                $"Đá Cường Hóa: <color=#FDE047>{stoneCount} viên</color>\nVàng hiện có: <b>{engine.Farm.Gold:N0} G</b>",
                "MỞ BẢNG RÈN TRANG BỊ [C]",
                () =>
                {
                    Close();
                    CharacterPanel.Show(0);
                    ToastOverlay.Show("Đã mở Bảng Trang Bị & Cường Hóa (+1..+5)");
                });

            // Station 2: Bàn Bản Vẽ Phòng Thủ
            SetupStation(
                1,
                MythUi.Icon("actions", "sua", 24) ?? UISpriteLoader.GetBlueprintSprite(),
                "2. Bàn Bản Vẽ Phòng Xây Dựng",
                $"Cấp Phòng Xây Dựng: <b>Cấp {engine.Defense.HallLevel}</b>\nBản vẽ đã mở: <color=#86EFAC>{engine.Defense.UnlockedBlueprints.Count} mẫu</color>",
                "MỞ BẢN VẼ PHÒNG THỦ [P]",
                () =>
                {
                    Close();
                    DefenseBuildPopup.Instance?.Toggle();
                    ToastOverlay.Show("Đã mở Xưởng Quốc Phòng & Bản Vẽ Tháp Canh");
                });

            // Station 3: Kho Đạn & Linh Kiện
            int towerCount = engine.Defense.Buildings.Count;
            int brokenCount = engine.Defense.Buildings.Count(b => b.IsBroken || b.Durability < 60);
            SetupStation(
                2,
                MythUi.Icon("actions", "nap_dan", 24) ?? UISpriteLoader.GetDefenseAmmoCrateSprite(),
                "3. Kho Đạn & Bảo Trì Tháp",
                $"Công trình đã dựng: <b>{towerCount} tháp</b> (cần bảo trì: {brokenCount})\nDự báo: <color=#FDE047>{WaveForecast.Major(engine).Text}</color>",
                "NẠP ĐẦY ĐẠN TOÀN TRẠI",
                () =>
                {
                    if (engine.Clock != null && engine.Clock.IsDuringCombatWave)
                    {
                        string warn = "Không thể nạp đạn hàng loạt khi đang trong đợt quái tấn công!";
                        SetStatusBanner(warn);
                        ToastOverlay.Show(warn);
                        return;
                    }
                    int reloaded = 0, spent = 0;
                    foreach (var b in engine.Defense.Buildings.ToArray())
                    {
                        if (b.AmmoCapacity <= 0 || b.AmmoCurrent >= b.AmmoCapacity) continue;
                        var r = DefenseReloads.Reload(engine, b.InstanceId);
                        if (r.success) { reloaded++; spent += r.goldSpent; }
                    }
                    foreach (var tv in FindObjectsByType<DefenseTower2DView>(FindObjectsSortMode.None))
                    {
                        if (tv != null && tv.BuildingModel != null) tv.SynchronizeFromModel();
                    }
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    string msg = reloaded > 0
                        ? $"Đã nạp đầy đạn cho {reloaded} tháp phòng thủ (tốn {spent} G)!"
                        : $"Tất cả {towerCount} tháp phòng thủ đều đã đầy đạn.";
                    SetStatusBanner(msg);
                    ToastOverlay.Show(msg);
                    RefreshLiveStationLabels();
                });

            // Station 4: Bàn Khắc Triện & Giấy Màu
            int sealInk = RareSources.SealInkCount(engine);
            SetupStation(
                3,
                UISpriteLoader.GetLootSprite("muc_an") ?? UISpriteLoader.GetLootSprite("GiayTim"),
                "4. Bàn Khắc Triện & Giấy Màu",
                $"Mực Ấn: <color=#FDE047>{sealInk} lọ</color>\nGiấy đang gắn: <b>{engine.DeepMechanics.EquippedPaperColor} +{engine.DeepMechanics.intEquippedPaperEnhancementLevel}</b>",
                "MỞ BẢNG TIỀM NĂNG & ẤN [C]",
                () =>
                {
                    Close();
                    CharacterPanel.Show(3);
                    ToastOverlay.Show("Đã mở Bảng Tiềm Năng, Khắc Triện & Giấy Màu");
                });
        }

        private void ConfigureQuanBaSauStations(GameEngine engine)
        {
            var baSauRel = engine.RelationOf(CastIds.BaSau);

            // Station 1: Quầy Bà Sáu
            SetupStation(
                0,
                UISpriteLoader.GetVillagerAvatar(VillagerId.BaSau) ?? MythUi.Icon("actions", "trao_doi", 24),
                "1. Quầy Tạp Hóa Bà Sáu",
                $"Thân mật Bà Sáu: <color=#F472B6>{baSauRel.Affinity}/100 ({baSauRel.Label})</color>\nĐổi thảo dược, vật tư & nhu yếu phẩm hàng ngày",
                "GIAO THƯƠNG VỚI BÀ SÁU",
                () =>
                {
                    Close();
                    var popup = EnsureVillageBarterPopup();
                    popup?.OpenForVillager(VillagerId.BaSau);
                    ToastOverlay.Show("Đã mở Quán Nước & Bếp Ăn Bà Sáu");
                });

            // Station 2: Bàn Tình Báo Đợt Quái
            int qToday = engine.Intel.QuestionsDay == engine.Clock.CurrentDay ? engine.Intel.QuestionsToday : 0;
            SetupStation(
                1,
                MythUi.Icon("misc", "tinh_bao", 24) ?? UISpriteLoader.GetBlueprintSprite(),
                "2. Bàn Tình Báo Đợt Quái",
                $"Dự báo: <color=#FDE047>{WaveForecast.Major(engine).Text}</color>\nLượt hỏi tin hôm nay: <b>{qToday}/{IntelBureau.QuestionsPerDay}</b>",
                "MỞ BÀN TÌNH BÁO",
                () =>
                {
                    Close();
                    IntelPopup.Open();
                    ToastOverlay.Show("Đã mở Bàn Tình Báo Đợt Quái");
                });

            // Station 3: Bàn Thẩm Định Anh Tư
            SetupStation(
                2,
                MythUi.Icon("actions", "giam_dinh", 24) ?? UISpriteLoader.GetLootSprite("BiKip"),
                "3. Trạm Thẩm Định Anh Tư",
                $"Giám định Bí Kíp Võ Học, Cổ Vật & Mẫu Gen\nVàng hiện có: <color=#FDE047>{engine.Farm.Gold:N0} G</color>",
                "MỞ BÀN THẨM ĐỊNH",
                () =>
                {
                    Close();
                    AppraisalPopup.Open();
                    ToastOverlay.Show("Đã mở Trạm Giám Định Anh Tư");
                });

            // Station 4: Quầy Thương Hội & Đấu Giá
            int h = engine.Clock.CurrentHour;
            bool isNight = h >= 18 || h < 6;
            SetupStation(
                3,
                UISpriteLoader.GetIconGold() ?? MythUi.Icon("actions", "trao_doi", 24),
                "4. Thương Hội & Sàn Đấu Giá",
                isNight
                    ? "Đang giờ <color=#C084FC>Chợ Đêm & Đấu Giá (18:00–06:00)</color>\nĐấu giá vật phẩm quý & bản vẽ Cao Cấp"
                    : $"Đang giờ <color=#86EFAC>Chợ Ngày (06:00–18:00)</color>\nGiá cám: {TroughCommands.FeedSackPrice(engine)}G / bao {TroughCommands.FeedSackKg:0}kg",
                isNight ? "MỞ SÀN ĐẤU GIÁ ĐÊM" : "MỞ QUẦY CHỢ NGÀY",
                () =>
                {
                    Close();
                    if (isNight)
                    {
                        AuctionPopup.Open(AuctionPopup.Mode.Auction);
                        ToastOverlay.Show("Đã mở Sàn Đấu Giá Đêm");
                    }
                    else
                    {
                        DayMarketPopup.Open();
                        ToastOverlay.Show("Đã mở Quầy Chợ Ngày");
                    }
                });
        }

        private void SetupStation(int index, Sprite icon, string title, string status, string actionLabel, Action onClick)
        {
            if (index < 0 || index >= stationHotspots.Count) return;
            var st = stationHotspots[index];
            st.IconImage.sprite = icon;
            st.IconImage.enabled = icon != null;
            st.TitleText.text = title;
            st.StatusText.text = status;
            st.ActionBadgeText.text = actionLabel;
            st.OnActivate = onClick;
        }

        private static VillageBarterPopup EnsureVillageBarterPopup()
        {
            if (VillageBarterPopup.Instance != null) return VillageBarterPopup.Instance;
            var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
            return canvas != null ? canvas.gameObject.AddComponent<VillageBarterPopup>() : null;
        }

        private static Sprite ResolveRoomBackdropSprite(InteriorRoom room)
        {
            switch (room)
            {
                case InteriorRoom.NhaChinh:
                    return UISpriteLoader.LoadSprite("Art/PixelMyth/UI/Interiors/interior_nha_chinh.png", 32)
                        ?? UISpriteLoader.GetBackdrop("bg_phong_bep_lo")
                        ?? UISpriteLoader.LoadSprite("Art/PixelMyth/UI/Backdrops/bg_phong_bep_lo.png", 32)
                        ?? UISpriteLoader.LoadSprite("Art/PixelMyth/UI/Backdrops/bg_nha_cu_bep_2018.png", 32);
                case InteriorRoom.ChuongTraiLon:
                    return UISpriteLoader.LoadSprite("Art/PixelMyth/UI/Interiors/interior_chuong_trai.png", 32)
                        ?? UISpriteLoader.GetBackdrop("bg_trai_chang_vang")
                        ?? UISpriteLoader.LoadSprite("Art/PixelMyth/UI/Backdrops/bg_trai_chang_vang.png", 32);
                case InteriorRoom.XuongRenPhongThu:
                    return UISpriteLoader.LoadSprite("Art/PixelMyth/UI/Interiors/interior_xuong_ren.png", 32)
                        ?? UISpriteLoader.GetBackdrop("bg_hoc_tuong_bay_cot")
                        ?? UISpriteLoader.LoadSprite("Art/PixelMyth/UI/Backdrops/bg_hoc_tuong_bay_cot.png", 32);
                case InteriorRoom.QuanBaSauTramTin:
                    return UISpriteLoader.LoadSprite("Art/PixelMyth/UI/Interiors/interior_quan_ba_sau.png", 32)
                        ?? UISpriteLoader.GetBackdrop("bg_hem_nha_cu")
                        ?? UISpriteLoader.LoadSprite("Art/PixelMyth/UI/Backdrops/bg_hem_nha_cu.png", 32);
                default:
                    return UISpriteLoader.GetBackdrop("bg_phong_bep_lo");
            }
        }

        private static string GetRoomTitle(InteriorRoom room) => room switch
        {
            InteriorRoom.NhaChinh => "NỘI THẤT NHÀ CHÍNH & BẾP LÒ",
            InteriorRoom.ChuongTraiLon => "NỘI THẤT CHUỒNG TRẠI & Y TẾ",
            InteriorRoom.XuongRenPhongThu => "XƯỞNG RÈN & PHÒNG XÂY DỰNG",
            InteriorRoom.QuanBaSauTramTin => "QUÁN BÀ SÁU & TRẠM TÌNH BÁO",
            _ => "NỘI THẤT CÔNG TRÌNH"
        };

        private void SetStatusBanner(string msg)
        {
            if (statusBannerText != null) statusBannerText.text = msg;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            Vector2 anchoredPos,
            Vector2 size,
            float fontSize,
            Color color,
            TextAlignmentOptions align,
            FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateStyledButton(
            string name,
            Transform parent,
            Vector2 anchoredPos,
            Vector2 size,
            Color bgColor,
            string label,
            float fontSize,
            UnityEngine.Events.UnityAction onClick,
            out TextMeshProUGUI labelTmp)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameActionButton();
            img.type = Image.Type.Sliced;
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            labelTmp = CreateText("Text", rt, Vector2.zero, size - new Vector2(8f, 4f), fontSize,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            return btn;
        }
    }
}
