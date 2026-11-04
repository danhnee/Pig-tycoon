using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Giao diện Quản Lý & Chế Tạo Phòng Thủ Trang Trại 2.5D (Defense Workshop Popup):
    /// - Hiển thị từng bản vẽ & công trình đang hoạt động kèm biểu tượng công trình 2.5D có đế (grounded 2.5D sprite:
    ///   Assets/Art/PixelMyth/Buildings/&lt;id&gt;/&lt;id&gt;_static.png).
    /// - Thanh tiến độ trực quan Độ bền (DurabilityBar) & Đạn/Lần kích (AmmoOrTriggerBar), huy hiệu nhánh (1A–5C).
    /// - Các nút tương tác lớn: Xây dựng (BuildBtn), Nạp đạn (ReloadBtn), Sửa chữa (RepairBtn kèm ActionFx "repair"),
    ///   Đổi chế độ bắn (FireModeBtn khóa khi IsDuringCombatWave), Nhánh (BranchBrowse) và Tháo dỡ (DemolishBtn).
    /// </summary>
    public class DefenseBuildPopup : MonoBehaviour
    {
        public static DefenseBuildPopup Instance { get; private set; }

        public bool IsOpen => contentPanel != null && contentPanel.activeSelf;

        private GameObject modalBackdropObj;
        private GameObject contentPanel;
        private Transform blueprintListContent;
        private Transform activeListContent;

        private TextMeshProUGUI resourceInfoText;
        private TextMeshProUGUI hallLevelText;
        private TextMeshProUGUI dutyInfoText;
        private TextMeshProUGUI statusFeedbackText;
        private int actionSeq;

        private readonly List<(string orderId, TextMeshProUGUI label, RectTransform fill)> constructionRows =
            new List<(string, TextMeshProUGUI, RectTransform)>();
        private readonly List<ActiveCardView> activeCardViews = new List<ActiveCardView>();
        private int shownOrderCount = -1;
        private float progressTimer;
        private Image hallUpgradeImage;
        private GameObject hudDefenseButtonObj;

        private sealed class ActiveCardView
        {
            public string InstanceId;
            public Image CardBg;
            public TextMeshProUGUI BranchBadgeLabel;
            public TextMeshProUGUI InfoText;
            public RectTransform AmmoFill;
            public TextMeshProUGUI AmmoLabel;
            public RectTransform DurabilityFill;
            public Image DurabilityFillImg;
            public TextMeshProUGUI DurabilityLabel;
            public Button FireModeBtn;
            public Image FireModeBtnImg;
            public TextMeshProUGUI FireModeBtnLabel;
            public TextMeshProUGUI BranchBtnLabel;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EnsureUIExists();
            Hide();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) // H belongs to the interiors (BuildingInteriorPopup)
            {
                Toggle();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
            {
                Hide();
            }

            if (IsOpen)
            {
                progressTimer -= Time.unscaledDeltaTime;
                if (progressTimer <= 0f)
                {
                    progressTimer = 0.5f;
                    RefreshConstructionProgress();
                }
            }
        }

        public void Toggle()
        {
            EnsureUIExists();
            if (IsOpen) Hide();
            else Show();
        }

        public void Show()
        {
            EnsureUIExists();

            if (modalBackdropObj != null)
            {
                modalBackdropObj.transform.SetAsLastSibling();
                modalBackdropObj.SetActive(true);
            }

            if (contentPanel != null)
            {
                contentPanel.transform.SetAsLastSibling();
                contentPanel.SetActive(true);
            }

            RefreshUI();
            if (contentPanel != null) UISpriteLoader.Apply25DWindowHierarchy(contentPanel.transform);
        }

        public void Hide()
        {
            if (contentPanel != null) contentPanel.SetActive(false);
            if (modalBackdropObj != null) modalBackdropObj.SetActive(false);
            // a pending confirm must not reappear over the buttons when the panel is reopened
            if (branchConfirmModal != null) branchConfirmModal.SetActive(false);
            pendingBranchConfirmAction = null;
        }

        private void ReportStatus(string message)
        {
            actionSeq++;
            PlayerInteractionController.Instance?.ShowFeedback(message);
            ToastOverlay.Show(message);
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = $"✦ {message} (#{actionSeq})";
            }
        }

        private void EnsureUIExists()
        {
            if (contentPanel != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = VillageBarterPopup.FindScreenSpaceCanvas();
            if (canvas == null)
            {
                var cObj = new GameObject("Defense_Canvas");
                canvas = cObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 115;
                var sc = cObj.AddComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1280f, 720f);
                sc.matchWidthOrHeight = 0.5f;
                cObj.AddComponent<GraphicRaycaster>();
            }

            // 1. Modal Backdrop
            modalBackdropObj = new GameObject("Defense_Backdrop");
            modalBackdropObj.transform.SetParent(canvas.transform, false);
            var bdRt = modalBackdropObj.AddComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.sizeDelta = Vector2.zero;

            var bdImg = modalBackdropObj.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.07f, 0.11f, 0.78f);
            var bdBtn = modalBackdropObj.AddComponent<Button>();
            bdBtn.onClick.AddListener(Hide);

            // 2. Main Content Panel (2.5D Wood Frame)
            contentPanel = new GameObject("Defense_Panel");
            contentPanel.transform.SetParent(canvas.transform, false);
            var cpRt = contentPanel.AddComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0.5f, 0.5f);
            cpRt.anchorMax = new Vector2(0.5f, 0.5f);
            cpRt.pivot = new Vector2(0.5f, 0.5f);
            cpRt.sizeDelta = new Vector2(1060f, 620f);

            var cpImg = contentPanel.AddComponent<Image>();
            cpImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            cpImg.type = Image.Type.Sliced;
            cpImg.color = new Color(0.93f, 0.89f, 0.83f, 1f);

            // Header Banner
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(contentPanel.transform, false);
            var hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.04f, 0.91f);
            hRt.anchorMax = new Vector2(0.92f, 0.98f);
            hRt.sizeDelta = Vector2.zero;

            var titleText = headerObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "<b>[PHÒNG THỦ 2.5D] XƯỞNG QUỐC PHÒNG & PHÒNG XÂY DỰNG NÔNG TRẠI</b>";
            titleText.fontSize = 20f;
            titleText.color = new Color(0.32f, 0.18f, 0.06f);
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.raycastTarget = false;

            // Close Button [X]
            var closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(contentPanel.transform, false);
            var cRt = closeBtnObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.93f, 0.90f);
            cRt.anchorMax = new Vector2(0.98f, 0.98f);
            cRt.sizeDelta = Vector2.zero;

            var cImg = closeBtnObj.AddComponent<Image>();
            cImg.sprite = UISpriteLoader.GetIconCloseCross();
            var closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = cImg;
            closeBtn.onClick.AddListener(Hide);

            // Sub Header: Resource & Hall Level Info
            var subHeaderObj = new GameObject("SubHeader");
            subHeaderObj.transform.SetParent(contentPanel.transform, false);
            var shRt = subHeaderObj.AddComponent<RectTransform>();
            shRt.anchorMin = new Vector2(0.04f, 0.83f);
            shRt.anchorMax = new Vector2(0.44f, 0.90f);
            shRt.sizeDelta = Vector2.zero;

            resourceInfoText = subHeaderObj.AddComponent<TextMeshProUGUI>();
            resourceInfoText.fontSize = 15f;
            resourceInfoText.color = new Color(0.18f, 0.34f, 0.18f);
            resourceInfoText.alignment = TextAlignmentOptions.Left;
            resourceInfoText.raycastTarget = false;

            // Hall Upgrade Button (GDD 13.2)
            var hallBtnObj = new GameObject("HallUpgradeBtn");
            hallBtnObj.transform.SetParent(contentPanel.transform, false);
            var hbRt = hallBtnObj.AddComponent<RectTransform>();
            hbRt.anchorMin = new Vector2(0.45f, 0.83f);
            hbRt.anchorMax = new Vector2(0.71f, 0.90f);
            hbRt.sizeDelta = Vector2.zero;
            hallUpgradeImage = hallBtnObj.AddComponent<Image>();
            hallUpgradeImage.sprite = UISpriteLoader.GetFrameActionButton();
            hallUpgradeImage.type = Image.Type.Sliced;
            hallUpgradeImage.color = new Color(0.75f, 0.55f, 0.20f);
            var hallBtn = hallBtnObj.AddComponent<Button>();
            hallBtn.targetGraphic = hallUpgradeImage;
            hallBtn.onClick.AddListener(OnUpgradeHallClicked);

            var hbTextObj = new GameObject("Text");
            hbTextObj.transform.SetParent(hallBtnObj.transform, false);
            var hbTRt = hbTextObj.AddComponent<RectTransform>();
            hbTRt.anchorMin = Vector2.zero;
            hbTRt.anchorMax = Vector2.one;
            hbTRt.sizeDelta = Vector2.zero;
            hallLevelText = hbTextObj.AddComponent<TextMeshProUGUI>();
            hallLevelText.fontSize = 14f;
            hallLevelText.fontStyle = FontStyles.Bold;
            hallLevelText.color = Color.white;
            hallLevelText.alignment = TextAlignmentOptions.Center;
            hallLevelText.raycastTarget = false;

            // Quick Reload All Button
            var quickReloadBtnObj = new GameObject("QuickReloadBtn");
            quickReloadBtnObj.transform.SetParent(contentPanel.transform, false);
            var qrRt = quickReloadBtnObj.AddComponent<RectTransform>();
            qrRt.anchorMin = new Vector2(0.73f, 0.83f);
            qrRt.anchorMax = new Vector2(0.96f, 0.90f);
            qrRt.sizeDelta = Vector2.zero;

            var qrImg = quickReloadBtnObj.AddComponent<Image>();
            qrImg.sprite = UISpriteLoader.GetFrameActionButton();
            qrImg.type = Image.Type.Sliced;
            qrImg.color = new Color(0.28f, 0.58f, 0.82f);
            var qrBtn = quickReloadBtnObj.AddComponent<Button>();
            qrBtn.targetGraphic = qrImg;
            qrBtn.onClick.AddListener(OnQuickReloadAllClicked);

            var qrTextObj = new GameObject("Text");
            qrTextObj.transform.SetParent(quickReloadBtnObj.transform, false);
            var qrTRt = qrTextObj.AddComponent<RectTransform>();
            qrTRt.anchorMin = Vector2.zero;
            qrTRt.anchorMax = Vector2.one;
            qrTRt.sizeDelta = Vector2.zero;
            var qrText = qrTextObj.AddComponent<TextMeshProUGUI>();
            qrText.text = "<b>[NẠP ĐẦY TẤT CẢ THÁP]</b>";
            qrText.fontSize = 14f;
            qrText.color = Color.white;
            qrText.alignment = TextAlignmentOptions.Center;
            qrText.raycastTarget = false;

            // Duty & Build Slots Info (GDD 13.2)
            var dutyObj = new GameObject("DutyInfo");
            dutyObj.transform.SetParent(contentPanel.transform, false);
            var dRt = dutyObj.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.04f, 0.76f);
            dRt.anchorMax = new Vector2(0.96f, 0.82f);
            dRt.sizeDelta = Vector2.zero;
            dutyInfoText = dutyObj.AddComponent<TextMeshProUGUI>();
            dutyInfoText.fontSize = 14f;
            dutyInfoText.alignment = TextAlignmentOptions.Left;
            dutyInfoText.color = new Color(0.28f, 0.18f, 0.08f);
            dutyInfoText.raycastTarget = false;

            // Bottom Status Banner
            var statusObj = new GameObject("StatusBanner");
            statusObj.transform.SetParent(contentPanel.transform, false);
            var stRt = statusObj.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.04f, 0.01f);
            stRt.anchorMax = new Vector2(0.96f, 0.05f);
            stRt.sizeDelta = Vector2.zero;
            statusFeedbackText = statusObj.AddComponent<TextMeshProUGUI>();
            statusFeedbackText.fontSize = 14f;
            statusFeedbackText.fontStyle = FontStyles.Bold;
            statusFeedbackText.color = new Color(0.32f, 0.18f, 0.06f);
            statusFeedbackText.alignment = TextAlignmentOptions.Center;
            statusFeedbackText.raycastTarget = false;
            statusFeedbackText.text = "✦ Chạm [ĐẶT XÂY] để bố trí tháp 2.5D, hoặc [NẠP ĐẠN] / [SỬA CHỮA] / [CHẾ ĐỘ BẮN] trên công trình đang vận hành.";

            // Left Column (54%): Blueprints
            CreateBlueprintScrollView();

            // Right Column (42%): Active Defense Structures
            CreateActiveBuildingsScrollView();
        }

        private void CreateBlueprintScrollView()
        {
            var svObj = new GameObject("Blueprint_ScrollView");
            svObj.transform.SetParent(contentPanel.transform, false);
            var svRt = svObj.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.03f, 0.06f);
            svRt.anchorMax = new Vector2(0.55f, 0.75f);
            svRt.sizeDelta = Vector2.zero;

            var sv = svObj.AddComponent<ScrollRect>();
            sv.horizontal = false;
            sv.vertical = true;

            var viewObj = new GameObject("Viewport");
            viewObj.transform.SetParent(svObj.transform, false);
            var vRt = viewObj.AddComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.sizeDelta = Vector2.zero;
            viewObj.AddComponent<RectMask2D>();
            sv.viewport = vRt;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewObj.transform, false);
            var cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 600f);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.content = cRt;
            blueprintListContent = contentObj.transform;
        }

        private void CreateActiveBuildingsScrollView()
        {
            var svObj = new GameObject("Active_ScrollView");
            svObj.transform.SetParent(contentPanel.transform, false);
            var svRt = svObj.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.57f, 0.06f);
            svRt.anchorMax = new Vector2(0.97f, 0.75f);
            svRt.sizeDelta = Vector2.zero;

            var sv = svObj.AddComponent<ScrollRect>();
            sv.horizontal = false;
            sv.vertical = true;

            var viewObj = new GameObject("Viewport");
            viewObj.transform.SetParent(svObj.transform, false);
            var vRt = viewObj.AddComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.sizeDelta = Vector2.zero;
            viewObj.AddComponent<RectMask2D>();
            sv.viewport = vRt;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewObj.transform, false);
            var cRt = contentObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 600f);

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.content = cRt;
            activeListContent = contentObj.transform;
        }

        private void RefreshHeaderInfo(GameEngine engine)
        {
            if (engine == null) return;
            int woodStock = engine.Inventory.CountId("Go") + engine.Inventory.CountId("Wood");
            if (resourceInfoText != null)
            {
                resourceInfoText.text = $"<b>Vàng:</b> {engine.Farm.Gold:N0}G  |  <b>Gỗ:</b> {woodStock}  |  <b>Thép:</b> {engine.Inventory.CountId(ConstructionHall.SteelItemId)}  |  <b>Phòng XD:</b> Cấp {engine.Defense.HallLevel}/3";
            }
            if (hallLevelText != null)
            {
                bool maxed = engine.Defense.HallLevel >= ConstructionHall.MaxLevel;
                hallLevelText.text = maxed ? "<b>[PHÒNG XÂY DỰNG CẤP 3]</b>"
                                           : $"<b>[NÂNG CẤP] {ConstructionHall.DescribeNext(engine.Defense.HallLevel)}</b>";
                if (hallUpgradeImage != null)
                    hallUpgradeImage.color = maxed ? new Color(0.50f, 0.50f, 0.50f) : new Color(0.75f, 0.55f, 0.20f);
            }
            if (dutyInfoText != null)
            {
                bool staffed = ConstructionHall.OnDutyNpc(engine) != null;
                int slots = ConstructionHall.BuildSlots(engine.Defense.HallLevel, ConstructionHall.EngineerTraitOnDuty(engine));
                dutyInfoText.text = $"<b>{ConstructionHall.DescribeDuty(engine)}</b>  |  Ô thi công: <b>{engine.Defense.ConstructionOrders.Count}/{slots}</b>";
                dutyInfoText.color = staffed ? new Color(0.28f, 0.18f, 0.08f) : new Color(0.75f, 0.12f, 0.10f);
            }
        }

        public void RefreshUI()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            RefreshHeaderInfo(engine);

            // 1. Populate Blueprints
            if (blueprintListContent != null)
            {
                foreach (Transform child in blueprintListContent)
                {
                    Destroy(child.gameObject);
                }

                var blueprints = engine.Defense.GetAllBlueprints();
                foreach (var bp in blueprints)
                {
                    CreateBlueprintCard(bp);
                }
            }

            // 2. Populate Active Buildings
            if (activeListContent != null)
            {
                foreach (Transform child in activeListContent)
                {
                    Destroy(child.gameObject);
                }

                constructionRows.Clear();
                activeCardViews.Clear();
                foreach (var order in engine.Defense.ConstructionOrders)
                {
                    CreateConstructionCard(engine, order);
                }
                shownOrderCount = engine.Defense.ConstructionOrders.Count;

                var buildings = engine.Defense.Buildings;
                if (buildings.Count == 0 && engine.Defense.ConstructionOrders.Count == 0)
                {
                    var emptyObj = new GameObject("EmptyMsg");
                    emptyObj.transform.SetParent(activeListContent, false);
                    var le = emptyObj.AddComponent<LayoutElement>();
                    le.minHeight = 64f;
                    le.preferredHeight = 64f;
                    var t = emptyObj.AddComponent<TextMeshProUGUI>();
                    t.text = "<i>Chưa có công trình phòng thủ nào được xây trong trại. Hãy chọn bản vẽ 2.5D bên trái để xây dựng!</i>";
                    t.fontSize = 14f;
                    t.color = new Color(0.35f, 0.28f, 0.22f);
                    t.raycastTarget = false;
                }
                else
                {
                    foreach (var b in buildings)
                    {
                        CreateActiveBuildingCard(b);
                    }
                }
            }
        }

        private void CreateBlueprintCard(DefenseBlueprint bp)
        {
            var engine = MobileGameController.Instance?.Engine;
            bool isUnlocked = engine != null && engine.Defense.UnlockedBlueprints.Contains(bp.Id);
            bool canCraft = engine != null && engine.Defense.CanCraftBlueprint(bp.Id);
            var startCheck = engine != null ? ConstructionQueue.CanStart(engine, bp.Id) : (ok: false, message: "Chưa sẵn sàng");
            bool canStart = startCheck.ok;
            bool staffedNow = engine != null && ConstructionHall.OnDutyNpc(engine) != null;
            int quoteMinutes = engine != null ? ConstructionQueue.QuoteMinutes(engine, bp.Id) : -1;

            var cardObj = new GameObject($"Card_{bp.Id}");
            cardObj.transform.SetParent(blueprintListContent, false);
            var le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 108f;
            le.preferredHeight = 108f;

            var bg = cardObj.AddComponent<Image>();
            bg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bg.type = Image.Type.Sliced;
            bg.color = !isUnlocked
                ? new Color(0.86f, 0.86f, 0.86f, 0.78f)
                : (bp.IsBachVan ? new Color(0.96f, 0.90f, 0.74f) : new Color(0.96f, 0.94f, 0.90f));

            // Grounded 2.5D Building Icon Pedestal
            var pedObj = new GameObject("IconPedestal", typeof(RectTransform), typeof(Image));
            pedObj.transform.SetParent(cardObj.transform, false);
            var pRt = (RectTransform)pedObj.transform;
            pRt.anchorMin = new Vector2(0.02f, 0.12f);
            pRt.anchorMax = new Vector2(0.18f, 0.88f);
            pRt.sizeDelta = Vector2.zero;
            var pImg = pedObj.GetComponent<Image>();
            pImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            pImg.type = Image.Type.Sliced;
            pImg.color = new Color(0.20f, 0.15f, 0.11f, 0.96f);
            pImg.raycastTarget = false;

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(pedObj.transform, false);
            var iRt = iconObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.08f, 0.08f);
            iRt.anchorMax = new Vector2(0.92f, 0.92f);
            iRt.sizeDelta = Vector2.zero;
            var iImg = iconObj.AddComponent<Image>();
            iImg.sprite = GetSpriteForBlueprint(bp.Id);
            iImg.preserveAspect = true;
            iImg.raycastTarget = false;
            if (!isUnlocked) iImg.color = new Color(0.55f, 0.55f, 0.55f, 0.65f);

            // Branch Badge (1A-5C) on Pedestal if available
            if (bp.AvailableUpgradeBranches != null && bp.AvailableUpgradeBranches.Count > 0)
            {
                string firstShort = bp.AvailableUpgradeBranches[0].Length >= 2 ? bp.AvailableUpgradeBranches[0].Substring(0, 2) : "1A";
                string lastCode = bp.AvailableUpgradeBranches[bp.AvailableUpgradeBranches.Count - 1];
                string lastShort = lastCode.Length >= 2 ? lastCode.Substring(0, 2) : "1C";
                var badgeObj = new GameObject("BranchBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
                badgeObj.transform.SetParent(pedObj.transform, false);
                var bdgRt = (RectTransform)badgeObj.transform;
                bdgRt.anchorMin = new Vector2(0f, 0f);
                bdgRt.anchorMax = new Vector2(1f, 0.28f);
                bdgRt.sizeDelta = Vector2.zero;
                var bdgTxt = badgeObj.GetComponent<TextMeshProUGUI>();
                bdgTxt.text = $"{firstShort}–{lastShort}";
                bdgTxt.fontSize = 13.5f;
                bdgTxt.fontStyle = FontStyles.Bold;
                bdgTxt.color = new Color(1f, 0.88f, 0.44f);
                bdgTxt.alignment = TextAlignmentOptions.Center;
                bdgTxt.raycastTarget = false;
            }

            // Info Text (14pt readable)
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var ifRt = infoObj.AddComponent<RectTransform>();
            ifRt.anchorMin = new Vector2(0.20f, 0.06f);
            ifRt.anchorMax = new Vector2(0.73f, 0.94f);
            ifRt.sizeDelta = Vector2.zero;

            var ifText = infoObj.AddComponent<TextMeshProUGUI>();
            string tier = DefenseManager.TierOf(bp.Id);
            string tag = tier == "BachVan" ? "<color=#b45309>[BẠCH VÂN]</color> "
                       : tier == "CaoCap" ? "<color=#7e22ce>[CAO CẤP]</color> "
                       : tier == "Tinh" ? "<color=#0369a1>[TINH]</color> "
                       : "<color=#374151>[THƯỜNG]</color> ";
            string tierSource = tier == "BachVan" ? "Đấu giá đêm / Bí Nhân / Boss chu kỳ ≥ 10"
                              : tier == "CaoCap" ? "Đấu giá đêm (sau Cổng Giữa Game)"
                              : tier == "Tinh" ? "Mua ở Chợ Đêm"
                              : Array.IndexOf(DefenseManager.StarterBlueprints, bp.Id) >= 0 ? "Có sẵn từ đầu" : "Mua ở Chợ Đêm";
            string lockNotice = !isUnlocked
                ? $"\n<color=#dc2626>Chưa học bản vẽ · {tierSource}</color>"
                : $"\n<color=#4b5563>Nguồn: {tierSource}</color>";

            ifText.text = $"<b>{tag}{bp.Name}</b> (Phòng XD Cấp {bp.RequiredHallLevel})\n" +
                          $"<b>Giá:</b> <color=#b45309>{bp.BuildCostGold:N0}G</color> · <b>Tầm:</b> {bp.AttackRange:0}m · <b>Thi công:</b> {(quoteMinutes < 0 ? "cần NPC trực" : ConstructionQueue.FormatMinutes(quoteMinutes) + "h")}" +
                          lockNotice;
            ifText.fontSize = 14f;
            ifText.color = new Color(0.16f, 0.14f, 0.12f);
            ifText.raycastTarget = false;

            // Build Button (BuildBtn)
            var btnObj = new GameObject("BuildBtn");
            btnObj.transform.SetParent(cardObj.transform, false);
            var bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.75f, 0.22f);
            bRt.anchorMax = new Vector2(0.97f, 0.78f);
            bRt.sizeDelta = Vector2.zero;

            var bImg = btnObj.AddComponent<Image>();
            bImg.sprite = UISpriteLoader.GetFrameActionButton();
            bImg.type = Image.Type.Sliced;

            if (!isUnlocked) bImg.color = new Color(0.55f, 0.55f, 0.55f, 0.85f);
            else if (!canCraft) bImg.color = new Color(0.82f, 0.46f, 0.26f);
            else if (!canStart) bImg.color = new Color(0.62f, 0.54f, 0.42f);
            else if (bp.IsBachVan) bImg.color = new Color(0.88f, 0.58f, 0.18f);
            else bImg.color = new Color(0.26f, 0.72f, 0.38f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bImg;
            string bId = bp.Id;
            string bName = bp.Name;
            string failReason = !isUnlocked ? $"Bản vẽ [{bName}] chưa mở khóa ({tierSource})."
                              : !canCraft ? $"Chưa đủ Vàng/Nguyên liệu hoặc cấp Phòng Xây Dựng cho [{bName}]."
                              : !canStart ? startCheck.message : "";
            btn.onClick.AddListener(() => OnBuildBlueprintClicked(bId, canUnlockedCraftStart: isUnlocked && canCraft && canStart, failReason));

            var btTextObj = new GameObject("Text");
            btTextObj.transform.SetParent(btnObj.transform, false);
            var btTRt = btTextObj.AddComponent<RectTransform>();
            btTRt.anchorMin = Vector2.zero;
            btTRt.anchorMax = Vector2.one;
            btTRt.sizeDelta = Vector2.zero;

            var btText = btTextObj.AddComponent<TextMeshProUGUI>();
            string lockBtnLabel = tier == "BachVan" ? "<b>[RẤT HIẾM]</b>" : tier == "CaoCap" ? "<b>[ĐẤU GIÁ]</b>" : tier == "Tinh" ? "<b>[CHỢ ĐÊM]</b>" : "<b>[KHÓA]</b>";
            btText.text = !isUnlocked ? lockBtnLabel : (!canCraft ? "<b>[THIẾU ĐỒ]</b>"
                : !canStart ? (staffedNow ? "<b>[KÍN Ô]</b>" : "<b>[CẦN TRỰC]</b>")
                : "<b>[XÂY DỰNG]</b>");
            btText.fontSize = 14f;
            btText.color = Color.white;
            btText.alignment = TextAlignmentOptions.Center;
            btText.raycastTarget = false;
        }

        private void CreateConstructionCard(GameEngine engine, ConstructionOrder order)
        {
            var cardObj = new GameObject($"Construction_{order.OrderId}");
            cardObj.transform.SetParent(activeListContent, false);
            var le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 76f;
            le.preferredHeight = 76f;
            var bg = cardObj.AddComponent<Image>();
            bg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.98f, 0.92f, 0.78f);

            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var ifRt = infoObj.AddComponent<RectTransform>();
            ifRt.anchorMin = new Vector2(0.04f, 0.40f);
            ifRt.anchorMax = new Vector2(0.96f, 0.95f);
            ifRt.sizeDelta = Vector2.zero;
            var label = infoObj.AddComponent<TextMeshProUGUI>();
            label.fontSize = 14f;
            label.color = new Color(0.25f, 0.15f, 0.05f);
            label.raycastTarget = false;

            var barObj = new GameObject("ProgressBar");
            barObj.transform.SetParent(cardObj.transform, false);
            var barRt = barObj.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.04f, 0.12f);
            barRt.anchorMax = new Vector2(0.96f, 0.34f);
            barRt.sizeDelta = Vector2.zero;
            var barBg = barObj.AddComponent<Image>();
            barBg.color = new Color(0.25f, 0.20f, 0.15f, 0.88f);
            barBg.raycastTarget = false;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            var fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(order.Progress), 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.95f, 0.70f, 0.20f);
            fillImg.raycastTarget = false;

            constructionRows.Add((order.OrderId, label, fillRt));
            label.text = DescribeConstruction(engine, order);
        }

        private static string DescribeConstruction(GameEngine engine, ConstructionOrder order)
        {
            int left = ConstructionQueue.RemainingMinutes(engine, order);
            string tail = left < 0 ? "<color=#dc2626>tạm dừng — chưa có NPC trực</color>" : $"còn {ConstructionQueue.FormatMinutes(left)} giờ game";
            return $"<b>{order.Name}</b> [<color=#b45309>ĐANG THI CÔNG</color>] {Mathf.FloorToInt(order.Progress * 100f)}% • {tail}";
        }

        private void RefreshConstructionProgress()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;
            if (engine.Defense.ConstructionOrders.Count != shownOrderCount) { RefreshUI(); return; }
            foreach (var row in constructionRows)
            {
                var order = engine.Defense.ConstructionOrders.Find(o => o.OrderId == row.orderId);
                if (order == null) { RefreshUI(); return; }
                if (row.label != null) row.label.text = DescribeConstruction(engine, order);
                if (row.fill != null) row.fill.anchorMax = new Vector2(Mathf.Clamp01(order.Progress), 1f);
            }
        }

        private static TowerFireMode ParseFireMode(string mode) =>
            mode == "TietKiem" ? TowerFireMode.TietKiem : mode == "ChiTinhAnh" ? TowerFireMode.ChiTinhAnh : TowerFireMode.TuDo;

        private static string FireModeLabel(TowerFireMode mode)
        {
            switch (mode)
            {
                case TowerFireMode.TietKiem: return "Tiết Kiệm";
                case TowerFireMode.ChiTinhAnh: return "Chỉ Tinh Anh";
                default: return "Tự Do";
            }
        }

        private void CreateActiveBuildingCard(DefenseBuildingInstance b)
        {
            var engine = MobileGameController.Instance?.Engine;
            var cardObj = new GameObject($"Active_{b.InstanceId}");
            cardObj.transform.SetParent(activeListContent, false);
            var le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 148f;
            le.preferredHeight = 148f;

            var bg = cardObj.AddComponent<Image>();
            bg.sprite = UISpriteLoader.GetFrameWoodPanel();
            bg.type = Image.Type.Sliced;

            // Grounded 2.5D Building Icon Pedestal on Active Card
            var pedObj = new GameObject("IconPedestal", typeof(RectTransform), typeof(Image));
            pedObj.transform.SetParent(cardObj.transform, false);
            var pRt = (RectTransform)pedObj.transform;
            pRt.anchorMin = new Vector2(0.02f, 0.36f);
            pRt.anchorMax = new Vector2(0.18f, 0.94f);
            pRt.sizeDelta = Vector2.zero;
            var pImg = pedObj.GetComponent<Image>();
            pImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            pImg.type = Image.Type.Sliced;
            pImg.color = new Color(0.18f, 0.14f, 0.10f, 0.96f);
            pImg.raycastTarget = false;

            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(pedObj.transform, false);
            var iRt = (RectTransform)iconObj.transform;
            iRt.anchorMin = new Vector2(0.08f, 0.08f);
            iRt.anchorMax = new Vector2(0.92f, 0.92f);
            iRt.sizeDelta = Vector2.zero;
            var iImg = iconObj.GetComponent<Image>();
            iImg.sprite = GetSpriteForBlueprint(b.BlueprintId);
            iImg.preserveAspect = true;
            iImg.raycastTarget = false;

            var branchBadgeObj = new GameObject("BranchBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            branchBadgeObj.transform.SetParent(pedObj.transform, false);
            var bbRt = (RectTransform)branchBadgeObj.transform;
            bbRt.anchorMin = new Vector2(0f, 0f);
            bbRt.anchorMax = new Vector2(1f, 0.30f);
            bbRt.sizeDelta = Vector2.zero;
            var branchBadgeTxt = branchBadgeObj.GetComponent<TextMeshProUGUI>();
            branchBadgeTxt.fontSize = 13.5f;
            branchBadgeTxt.fontStyle = FontStyles.Bold;
            branchBadgeTxt.color = new Color(1f, 0.88f, 0.42f);
            branchBadgeTxt.alignment = TextAlignmentOptions.Center;
            branchBadgeTxt.raycastTarget = false;

            // Info Text
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(cardObj.transform, false);
            var ifRt = infoObj.AddComponent<RectTransform>();
            ifRt.anchorMin = new Vector2(0.20f, 0.36f);
            ifRt.anchorMax = new Vector2(0.65f, 0.95f);
            ifRt.sizeDelta = Vector2.zero;

            var ifText = infoObj.AddComponent<TextMeshProUGUI>();
            ifText.fontSize = 13.5f;
            ifText.color = new Color(0.14f, 0.12f, 0.10f);
            ifText.raycastTarget = false;

            // Progress Bars: AmmoOrTriggerBar & DurabilityBar
            RectTransform ammoFill = null;
            TextMeshProUGUI ammoLbl = null;
            if (b.AmmoCapacity > 0)
            {
                BuildMiniStatBar(cardObj.transform, "AmmoOrTriggerBar", new Vector2(0.02f, 0.19f), new Vector2(0.64f, 0.33f),
                    Mathf.Clamp01((float)b.AmmoCurrent / b.AmmoCapacity),
                    new Color(0.20f, 0.56f, 0.82f), "", out ammoFill, out _, out ammoLbl);
            }
            BuildMiniStatBar(cardObj.transform, "DurabilityBar", new Vector2(0.02f, 0.04f), new Vector2(0.64f, 0.17f),
                Mathf.Clamp01(b.Durability / 100f),
                new Color(0.22f, 0.65f, 0.42f), "", out var durFill, out var durFillImg, out var durLbl);

            string instId = b.InstanceId;
            bool isSpikeTrap = b.BlueprintId == "BanChong" || (b.AmmoCapacity == 6 && b.Name.IndexOf("Chông", StringComparison.OrdinalIgnoreCase) >= 0);

            // Row 1 Right: ReloadBtn & RepairBtn
            CreateSmallCardButton(cardObj.transform, "ReloadBtn", new Vector2(0.66f, 0.68f), new Vector2(0.81f, 0.94f),
                new Color(0.26f, 0.58f, 0.84f), isSpikeTrap ? "<b>NẠP BẪY</b>" : "<b>NẠP ĐẠN</b>",
                () => OnReloadBuildingClicked(instId), out _, out _, out _);

            CreateSmallCardButton(cardObj.transform, "RepairBtn", new Vector2(0.82f, 0.68f), new Vector2(0.98f, 0.94f),
                new Color(0.84f, 0.46f, 0.22f), "<b>SỬA CHỮA</b>",
                () => OnRepairBuildingClicked(instId), out _, out _, out _);

            // Row 2 Right: FireModeBtn (Contract #2: locked when IsDuringCombatWave)
            bool duringWave = engine != null && engine.Clock != null && engine.Clock.IsDuringCombatWave;
            CreateSmallCardButton(cardObj.transform, "FireModeBtn", new Vector2(0.66f, 0.36f), new Vector2(0.98f, 0.62f),
                duringWave ? new Color(0.45f, 0.46f, 0.48f, 0.72f) : new Color(0.32f, 0.58f, 0.36f),
                duringWave ? "<b>KHÓA (ĐỢT QUÁI)</b>" : "<b>ĐỔI CHẾ ĐỘ BẮN</b>",
                () => OnFireModeClicked(instId),
                out var fireBtn, out var fireBtnImg, out var fireBtnLbl, interactable: !duringWave);

            // Row 3 Right: BranchBrowse (if available) & DemolishBtn
            var bpOfB = engine?.Defense.GetBlueprint(b.BlueprintId);
            bool hasBranches = bpOfB != null && bpOfB.AvailableUpgradeBranches.Count > 0;
            TextMeshProUGUI branchBtnLbl = null;
            if (hasBranches)
            {
                string bName = b.Name;
                var codes = bpOfB.AvailableUpgradeBranches;
                CreateSmallCardButton(cardObj.transform, "BranchBrowse", new Vector2(0.66f, 0.04f), new Vector2(0.82f, 0.30f),
                    new Color(0.62f, 0.40f, 0.78f), "<b>NHÁNH</b>", () =>
                    {
                        int k = (branchBrowseIndex.TryGetValue(instId, out var cur) ? cur : 0) % codes.Count;
                        branchBrowseIndex[instId] = k + 1;
                        string code = codes[k];
                        string shortCode = code.Length > 2 ? code.Substring(0, 2) : code;
                        ShowBranchConfirmModal(bName, b.BlueprintId, instId, shortCode, DefenseBranches.DisplayName(code));
                    }, out _, out _, out branchBtnLbl);
            }

            Vector2 demolishMin = hasBranches ? new Vector2(0.83f, 0.04f) : new Vector2(0.66f, 0.04f);
            CreateSmallCardButton(cardObj.transform, "DemolishBtn", demolishMin, new Vector2(0.98f, 0.30f),
                new Color(0.72f, 0.26f, 0.24f), "<b>THÁO DỠ</b>",
                () => OnDemolishBuildingClicked(instId), out _, out _, out _);

            var view = new ActiveCardView
            {
                InstanceId = instId,
                CardBg = bg,
                BranchBadgeLabel = branchBadgeTxt,
                InfoText = ifText,
                AmmoFill = ammoFill,
                AmmoLabel = ammoLbl,
                DurabilityFill = durFill,
                DurabilityFillImg = durFillImg,
                DurabilityLabel = durLbl,
                FireModeBtn = fireBtn,
                FireModeBtnImg = fireBtnImg,
                FireModeBtnLabel = fireBtnLbl,
                BranchBtnLabel = branchBtnLbl
            };
            activeCardViews.Add(view);
            UpdateActiveCardViewInPlace(engine, view, b);
        }

        private void UpdateActiveCardViewInPlace(GameEngine engine, ActiveCardView view, DefenseBuildingInstance b)
        {
            if (view == null || b == null) return;
            view.CardBg.color = b.IsBroken ? new Color(0.96f, 0.78f, 0.78f) : new Color(0.93f, 0.95f, 0.97f);

            var durState = DefenseDurability.StateOf(b.Durability);
            string durLabel = DefenseDurability.StateName(durState);
            var repairQuote = engine != null ? DefenseRepairs.Quote(engine, b) : default;
            var reloadQuote = engine != null ? DefenseReloads.Quote(b, engine.Inventory) : default;

            string statusStr = b.IsBroken ? "<color=#dc2626>HỎNG</color>"
                : engine != null && !engine.Defense.IsOperational(b) ? "<color=#64748b>MẤT ĐIỆN</color>"
                : durState == DurabilityState.NguyKich ? "<color=#dc2626>NGUY KỊCH</color>"
                : durState == DurabilityState.Mon ? "<color=#d97706>MÒN</color>"
                : "<color=#15803d>TỐT</color>";

            bool isSpikeTrap = b.BlueprintId == "BanChong" || (b.AmmoCapacity == 6 && b.Name.IndexOf("Chông", StringComparison.OrdinalIgnoreCase) >= 0);
            string ammoLabel = isSpikeTrap ? $"Lần kích {b.AmmoCurrent}/{b.AmmoCapacity}" : (b.AmmoCapacity > 0 ? $"{b.AmmoName}: {b.AmmoCurrent}/{b.AmmoCapacity}" : "Không dùng đạn");
            string reloadStr = reloadQuote != null && reloadQuote.Gold > 0 ? $" · Nạp: {reloadQuote.Gold}G" : "";
            var bpOfB = engine?.Defense.GetBlueprint(b.BlueprintId);
            bool hasBranches = bpOfB != null && bpOfB.AvailableUpgradeBranches.Count > 0;
            string branchStr = !hasBranches ? "" : string.IsNullOrEmpty(b.ChosenUpgradeBranch) ? " · Nhánh: <b>Chưa chọn</b>" : $" · Nhánh: <b>{DefenseBranches.DisplayName(b.ChosenUpgradeBranch)}</b>";

            if (view.BranchBadgeLabel != null)
            {
                view.BranchBadgeLabel.text = !string.IsNullOrEmpty(b.ChosenUpgradeBranch)
                    ? b.ChosenUpgradeBranch
                    : (hasBranches ? "1A–C" : "");
            }

            var parsedFire = ParseFireMode(b.FireMode);
            string fireStr = b.IsBachVan ? "Thủ công" : FireModeLabel(parsedFire);

            view.InfoText.text = $"<b>{b.Name}</b> [{statusStr}]{branchStr}\n" +
                                 $"Máu: <b>{b.CurrentHp}/{b.MaxHp}</b> · <b>{ammoLabel}</b>{reloadStr}\n" +
                                 $"Chế độ bắn: <b>{fireStr}</b> · Sửa: {(repairQuote != null && repairQuote.Gold > 0 ? $"{repairQuote.Gold}G ({repairQuote.Minutes}p)" : "Tốt (100%)")}";

            if (view.AmmoFill != null && b.AmmoCapacity > 0)
            {
                view.AmmoFill.anchorMax = new Vector2(Mathf.Clamp01((float)b.AmmoCurrent / b.AmmoCapacity), 1f);
            }
            if (view.AmmoLabel != null)
            {
                view.AmmoLabel.text = ammoLabel;
            }

            if (view.DurabilityFill != null)
            {
                view.DurabilityFill.anchorMax = new Vector2(Mathf.Clamp01(b.Durability / 100f), 1f);
            }
            if (view.DurabilityFillImg != null)
            {
                view.DurabilityFillImg.color = durState == DurabilityState.NguyKich
                    ? new Color(0.82f, 0.22f, 0.20f)
                    : durState == DurabilityState.Mon ? new Color(0.82f, 0.54f, 0.16f) : new Color(0.22f, 0.65f, 0.42f);
            }
            if (view.DurabilityLabel != null)
            {
                view.DurabilityLabel.text = $"Độ bền {b.Durability}/100 ({durLabel})";
            }

            bool duringWave = engine != null && engine.Clock != null && engine.Clock.IsDuringCombatWave;
            if (view.FireModeBtn != null)
                view.FireModeBtn.interactable = !duringWave;
            if (view.FireModeBtnImg != null)
                view.FireModeBtnImg.color = duringWave ? new Color(0.45f, 0.46f, 0.48f, 0.72f) : new Color(0.32f, 0.58f, 0.36f);
            if (view.FireModeBtnLabel != null)
                view.FireModeBtnLabel.text = duringWave ? "<b>KHÓA (ĐỢT QUÁI)</b>" : $"<b>BẮN: {fireStr.ToUpperInvariant()}</b>";

            if (view.BranchBtnLabel != null && hasBranches)
            {
                int shown = branchBrowseIndex.TryGetValue(b.InstanceId, out var bi) ? bi : 0;
                view.BranchBtnLabel.text = string.IsNullOrEmpty(b.ChosenUpgradeBranch)
                    ? $"<b>NHÁNH {shown % bpOfB.AvailableUpgradeBranches.Count + 1}/{bpOfB.AvailableUpgradeBranches.Count}</b>"
                    : $"<b>{b.ChosenUpgradeBranch}</b>";
            }
        }

        private void RefreshActiveCardsInPlace()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;
            RefreshHeaderInfo(engine);
            for (int i = 0; i < activeCardViews.Count; i++)
            {
                var view = activeCardViews[i];
                var b = engine.Defense.Buildings.Find(x => x.InstanceId == view.InstanceId);
                if (b != null)
                {
                    UpdateActiveCardViewInPlace(engine, view, b);
                }
            }
        }

        private readonly Dictionary<string, int> branchBrowseIndex = new Dictionary<string, int>();

        private GameObject branchConfirmModal;
        private Image branchConfirmIcon;
        private TextMeshProUGUI branchConfirmText;
        private Action pendingBranchConfirmAction;

        private void ShowBranchConfirmModal(string buildingName, string blueprintId, string instId, string shortCode, string fullDisplay)
        {
            if (contentPanel == null) return;
            if (branchConfirmModal == null)
            {
                branchConfirmModal = DayMarketPopup.BuildStyledPopupFrame(contentPanel.transform, "BranchConfirmModal", new Vector2(640f, 320f),
                    out var body, out var hdr, out var ftr);
                var hTxt = DayMarketPopup.Text(hdr, Vector2.zero, new Vector2(560f, 34f), 18f, TextAlignmentOptions.Center);
                hTxt.text = "<b>XÁC NHẬN CHỌN NHÁNH NÂNG CẤP 2.5D</b>";
                hTxt.color = new Color(1f, 0.94f, 0.68f);
                hTxt.raycastTarget = false;

                var iconCard = DayMarketPopup.MakeSlotCard(body, new Vector2(-210f, 10f), new Vector2(110f, 110f));
                iconCard.GetComponent<Image>().raycastTarget = false;
                var icGo = new GameObject("BranchIcon", typeof(RectTransform), typeof(Image));
                icGo.transform.SetParent(iconCard.transform, false);
                ((RectTransform)icGo.transform).sizeDelta = new Vector2(88f, 88f);
                branchConfirmIcon = icGo.GetComponent<Image>();
                branchConfirmIcon.preserveAspect = true;
                branchConfirmIcon.raycastTarget = false;

                branchConfirmText = DayMarketPopup.Text(body, new Vector2(64f, 10f), new Vector2(410f, 160f), 15f, TextAlignmentOptions.Left);
                branchConfirmText.textWrappingMode = TextWrappingModes.Normal;
                branchConfirmText.raycastTarget = false;

                var okBtn = DayMarketPopup.Button(ftr, new Vector2(-120f, 0f), new Vector2(210f, 38f), out var okLbl);
                okLbl.text = "Xác Nhận Nâng Cấp";
                okLbl.fontSize = 15f;
                okLbl.fontStyle = FontStyles.Bold;
                okLbl.raycastTarget = false;
                okBtn.onClick.AddListener(() =>
                {
                    branchConfirmModal.SetActive(false);
                    var act = pendingBranchConfirmAction;
                    pendingBranchConfirmAction = null;
                    act?.Invoke();
                });

                var cancelBtn = DayMarketPopup.Button(ftr, new Vector2(120f, 0f), new Vector2(190f, 38f), out var cLbl);
                cLbl.text = "Đóng / Xem Tiếp";
                cLbl.fontSize = 15f;
                cLbl.fontStyle = FontStyles.Bold;
                cLbl.raycastTarget = false;
                cancelBtn.onClick.AddListener(() =>
                {
                    branchConfirmModal.SetActive(false);
                    pendingBranchConfirmAction = null;
                    ReportStatus($"Đã đóng hộp thoại nhánh [{shortCode}].");
                });
            }

            if (branchConfirmIcon != null)
            {
                branchConfirmIcon.sprite = GetSpriteForBlueprint(blueprintId);
            }

            pendingBranchConfirmAction = () =>
            {
                var engine = MobileGameController.Instance?.Engine;
                if (engine == null) return;
                var res = engine.Defense.SelectAttackTowerUpgradeBranch(instId, shortCode);
                ReportStatus(res.message);
                RefreshActiveCardsInPlace();
            };

            branchConfirmText.text = $"Công trình: <b><color=#FDE047>{buildingName}</color></b>\n" +
                                     $"Nhánh chọn: <b><color=#86EFAC>{fullDisplay}</color></b>\n\n" +
                                     "<color=#FCA5A5>Lưu ý: Mỗi công trình tấn công chỉ chọn 1 nhánh nâng cấp duy nhất (yêu cầu Phòng Xây Dựng cấp 2+).</color>";
            branchConfirmModal.SetActive(true);
            branchConfirmModal.transform.SetAsLastSibling();
            ReportStatus($"Đang xem nhánh [{fullDisplay}] của {buildingName}.");
        }

        private static void BuildMiniStatBar(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float ratio,
            Color fillColor,
            string text,
            out RectTransform fillRtOut,
            out Image fillImgOut,
            out TextMeshProUGUI labelOut)
        {
            var barObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            barObj.transform.SetParent(parent, false);
            var rt = (RectTransform)barObj.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;
            var bg = barObj.GetComponent<Image>();
            bg.color = new Color(0.14f, 0.16f, 0.20f, 0.90f);
            bg.raycastTarget = false;

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(barObj.transform, false);
            fillRtOut = (RectTransform)fillObj.transform;
            fillRtOut.anchorMin = Vector2.zero;
            fillRtOut.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fillRtOut.sizeDelta = Vector2.zero;
            fillImgOut = fillObj.GetComponent<Image>();
            fillImgOut.color = fillColor;
            fillImgOut.raycastTarget = false;

            var lblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblObj.transform.SetParent(barObj.transform, false);
            var lrt = (RectTransform)lblObj.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6f, 0f);
            lrt.offsetMax = new Vector2(-6f, 0f);
            labelOut = lblObj.GetComponent<TextMeshProUGUI>();
            labelOut.text = text;
            labelOut.fontSize = 13.5f;
            labelOut.fontStyle = FontStyles.Bold;
            labelOut.color = Color.white;
            labelOut.alignment = TextAlignmentOptions.MidlineLeft;
            labelOut.raycastTarget = false;
        }

        private static void CreateSmallCardButton(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color,
            string label,
            UnityEngine.Events.UnityAction onClick,
            out Button btnOut,
            out Image imgOut,
            out TextMeshProUGUI txtOut,
            bool interactable = true)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;

            imgOut = btnObj.AddComponent<Image>();
            imgOut.sprite = UISpriteLoader.GetFrameActionButton();
            imgOut.type = Image.Type.Sliced;
            imgOut.color = color;

            btnOut = btnObj.AddComponent<Button>();
            btnOut.targetGraphic = imgOut;
            btnOut.interactable = interactable;
            btnOut.onClick.AddListener(onClick);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRt = txtObj.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(2f, 1f);
            tRt.offsetMax = new Vector2(-2f, -1f);
            txtOut = txtObj.AddComponent<TextMeshProUGUI>();
            txtOut.text = label;
            txtOut.fontSize = 13.5f;
            txtOut.color = Color.white;
            txtOut.alignment = TextAlignmentOptions.Center;
            txtOut.raycastTarget = false;
        }

        private void OnBuildBlueprintClicked(string blueprintId, bool canUnlockedCraftStart, string failReason)
        {
            if (!canUnlockedCraftStart)
            {
                ReportStatus(string.IsNullOrEmpty(failReason) ? $"Chưa đủ điều kiện đặt xây [{blueprintId}]." : failReason);
                return;
            }
            Hide();
            if (DefenseBuildController.Instance != null)
            {
                DefenseBuildController.Instance.StartPlacement(blueprintId);
            }
        }

        private void OnQuickReloadAllClicked()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            if (engine.Clock != null && engine.Clock.IsDuringCombatWave)
            {
                ReportStatus("Không được nạp đạn trong khi có đợt quái tấn công!");
                return;
            }

            int reloadedTowers = 0, goldSpent = 0;
            string lastFail = "";
            foreach (var b in engine.Defense.Buildings.ToArray())
            {
                if (b.AmmoCapacity <= 0 || b.AmmoCurrent >= b.AmmoCapacity) continue;
                var r = DefenseReloads.Reload(engine, b.InstanceId);
                if (r.success) { reloadedTowers++; goldSpent += r.goldSpent; }
                else lastFail = r.message;
            }

            foreach (var tv in FindObjectsByType<DefenseTower2DView>())
            {
                if (tv.BuildingModel != null) tv.SynchronizeFromModel();
            }

            ReportStatus(reloadedTowers > 0
                ? $"Đã nạp {reloadedTowers} công trình, tốn {goldSpent}G." + (lastFail.Length > 0 ? " " + lastFail : "")
                : (lastFail.Length > 0 ? lastFail : "Mọi công trình phòng thủ đã đầy đạn."));
            RefreshActiveCardsInPlace();
        }

        private void OnReloadBuildingClicked(string instanceId)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var res = DefenseReloads.Reload(engine, instanceId);
            ReportStatus(res.message);

            foreach (var tv in FindObjectsByType<DefenseTower2DView>())
            {
                if (tv.BuildingModel != null && tv.BuildingModel.InstanceId == instanceId)
                {
                    tv.SynchronizeFromModel();
                    break;
                }
            }

            RefreshActiveCardsInPlace();
        }

        private void OnRepairBuildingClicked(string instanceId)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var result = DefenseRepairs.Request(engine, instanceId);
            ReportStatus(result.message);

            foreach (var tv in FindObjectsByType<DefenseTower2DView>())
            {
                if (tv.BuildingModel != null && tv.BuildingModel.InstanceId == instanceId)
                {
                    ActionFx.Play(ActionCue.Repair, tv.transform.position);
                    tv.SynchronizeFromModel();
                    break;
                }
            }

            RefreshActiveCardsInPlace();
        }

        private void OnFireModeClicked(string instanceId)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;
            var b = engine.Defense.Buildings.Find(x => x.InstanceId == instanceId);
            if (b == null) return;

            var cur = ParseFireMode(b.FireMode);
            TowerFireMode next = cur == TowerFireMode.TuDo ? TowerFireMode.TietKiem
                : (cur == TowerFireMode.TietKiem ? TowerFireMode.ChiTinhAnh : TowerFireMode.TuDo);
            var res = engine.Defense.SetTowerFireMode(instanceId, next);
            ReportStatus(res.message);
            RefreshActiveCardsInPlace();
        }

        private void OnDemolishBuildingClicked(string instanceId)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;
            if (engine.Clock != null && engine.Clock.IsDuringCombatWave)
            {
                ReportStatus("Không thể tháo dỡ công trình khi đang trong đợt quái tấn công!");
                return;
            }
            var b = engine.Defense.Buildings.Find(x => x.InstanceId == instanceId);
            if (b == null) return;

            var bp = engine.Defense.GetBlueprint(b.BlueprintId);
            int refund = bp != null ? bp.BuildCostGold / 2 : 50;
            ShowBranchConfirmModal(
                b.Name,
                b.BlueprintId,
                instanceId,
                "THÁO DỠ",
                $"Tháo dỡ công trình và thu hồi {refund:N0} Vàng (50% chi phí)");
            pendingBranchConfirmAction = () =>
            {
                engine.Defense.Buildings.Remove(b);
                engine.Farm.Gold += refund;
                ReportStatus($"Đã tháo dỡ [{b.Name}] và thu hồi {refund:N0}G.");
                RefreshUI();
            };
        }

        private void OnUpgradeHallClicked()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;
            var res = ConstructionHall.TryUpgrade(engine);
            ReportStatus(res.message);
            if (res.success)
            {
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                RefreshUI();
            }
            else
            {
                RefreshHeaderInfo(engine);
            }
        }

        private Sprite GetSpriteForBlueprint(string blueprintId)
        {
            return UISpriteLoader.LoadBuildingRowZero(blueprintId) ?? UISpriteLoader.GetTowerSprite(blueprintId);
        }

        private void EnsureHudButton(Canvas canvas)
        {
            if (hudDefenseButtonObj != null || canvas == null) return;
            hudDefenseButtonObj = new GameObject("HUD_Defense_Button");
            hudDefenseButtonObj.transform.SetParent(canvas.transform, false);

            var rt = hudDefenseButtonObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.sizeDelta = new Vector2(148f, 34f);

            var img = hudDefenseButtonObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameActionButton();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.35f, 0.55f, 0.75f, 0.95f);

            var btn = hudDefenseButtonObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(Toggle);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(hudDefenseButtonObj.transform, false);
            var tRt = textObj.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            var t = textObj.AddComponent<TextMeshProUGUI>();
            t.text = "<b>[Phòng Thủ - P]</b>";
            t.fontSize = 14f;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
        }
    }
}
