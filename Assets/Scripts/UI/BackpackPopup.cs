using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Tactile 2.5D SpaceBag, Hand Carry & Warehouse Modal (GDD v6.0 §12).
    /// Icon-First 72x72 cells (44x44 crisp 2.5D icon + Rarity Bevel Rim I-IV + 14pt bold corner stack badge)
    /// and rich bottom Visual Item Inspector Banner (64x64 icon, 18pt bold title, category/rarity badge, and 1-click actions).
    /// </summary>
    public class BackpackPopup : MonoBehaviour
    {
        public static BackpackPopup Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.ContentPanel != null && Instance.ContentPanel.activeSelf;
        static int lastClosedFrame = -1;
        public static bool InputBlockedThisFrame => IsOpen || Time.frameCount == lastClosedFrame;
        public bool IsWarehouseOpen => IsOpen && warehouseOpen;

        // Preserve serialized bindings used by earlier scenes; rebuild their old grid on first use.
        public GameObject ContentPanel;
        public Button CloseButton;
        public Image DetailIcon;
        public TextMeshProUGUI DetailTitleText;
        public TextMeshProUGUI DetailCategoryText;
        public TextMeshProUGUI DetailDescriptionText;
        public Button DetailActionButton;
        public TextMeshProUGUI DetailActionButtonText;

        static readonly Color Gold = new Color(.96f, .78f, .35f);
        static readonly Color Ivory = new Color(.98f, .94f, .84f);
        static readonly Color Jade = new Color(.48f, .92f, .62f);

        RectTransform safeRoot, window, headerBanner, bagView, objectsRoot, capacityRoot, warehouseView, stockRoot, detailRoot;
        TextMeshProUGUI headerTitle, bagSectionTitle, warehouseSectionTitle, counter, feedback;
        Image detailIconBoxBg, detailCategoryBadgeBg;
        ScrollRect stockScroll;
        bool built, warehouseOpen, viaLink;
        int selectedIndex = -1;
        bool selectedFromWarehouse;
        int inspectToggleStep;
        int visibleSignature;
        float nextStockCheck;
        Rect lastSafeArea;
        float feedbackUntil;
        RectTransform transferGlyphs;

        Button handOrSwapButton, warehouseTransferButton, useButton;
        TextMeshProUGUI handOrSwapButtonText, warehouseTransferButtonText, useButtonText;

        readonly List<Image> carriedSlotFrames = new List<Image>();
        readonly List<Image> stockSlotFrames = new List<Image>();

        GameEngine Engine => MobileGameController.Instance?.Engine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start() { if (!built) { EnsureUIExists(); Hide(); } }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            if (built && ContentPanel != null) Destroy(ContentPanel);
        }

        void OnDisable() { if (Instance == this) Hide(); }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I)) Toggle();
            else if (Input.GetKeyDown(KeyCode.Escape) && IsOpen) Hide();
            if (!IsOpen) return;
            if (viaLink && Engine != null && !Engine.Carry.CanUseWarehouseLink(out _)) { Hide(); return; }
            if (Screen.safeArea != lastSafeArea) Layout();
            if (feedback != null && feedbackUntil > 0 && Time.unscaledTime >= feedbackUntil)
            {
                feedback.text = string.Empty;
                feedbackUntil = 0;
            }
            if (Engine != null && Time.unscaledTime >= nextStockCheck)
            {
                nextStockCheck = Time.unscaledTime + .25f;
                if (StateSignature() != visibleSignature) RefreshData();
            }
        }

        public void Toggle() { if (IsOpen) Hide(); else Show(); }
        public void Show() => Open(false, false);
        /// <summary>At the warehouse building, in person (works in combat).</summary>
        public void ShowWarehouse() => Open(true, false);
        /// <summary>Bracelet remote link: anywhere outside combat (ADR-0012).</summary>
        public void ShowWarehouseLink()
        {
            if (Engine == null || !Engine.Carry.CanUseWarehouseLink(out _)) return;
            Open(true, true);
        }

        public static BackpackPopup Ensure()
        {
            if (Instance != null) return Instance;
            var canvas = FindFirstObjectByType<Canvas>();
            return canvas != null ? canvas.gameObject.AddComponent<BackpackPopup>() : null;
        }

        IReadOnlyList<UnifiedItemInstance> Carried()
        {
            var carry = Engine.Carry;
            if (carry.HasBracelet) return Engine.SpaceBag.Items;
            return carry.HandItem != null ? new List<UnifiedItemInstance> { carry.HandItem } : new List<UnifiedItemInstance>();
        }

        int CarriedCapacity() => Engine.Carry.HasBracelet ? Engine.SpaceBag.Capacity : CarryRules.HandStackLimit;

        void Open(bool fromWarehouse, bool remoteLink)
        {
            EnsureUIExists();
            if (!built) return;
            warehouseOpen = fromWarehouse;
            viaLink = remoteLink;
            selectedFromWarehouse = false;
            selectedIndex = Carried().Count > 0 ? 0 : -1;
            inspectToggleStep = 0;
            ContentPanel.SetActive(true);
            ContentPanel.transform.SetAsLastSibling();
            UISpriteLoader.Apply25DWindowHierarchy(ContentPanel.transform);
            EventSystem.current?.SetSelectedGameObject(null);
            Layout();
            feedback.text = string.Empty;
            RefreshData();
        }

        public void Hide()
        {
            if (ContentPanel != null && ContentPanel.activeSelf)
            {
                ContentPanel.SetActive(false);
                lastClosedFrame = Time.frameCount;
                EventSystem.current?.SetSelectedGameObject(null);
            }
            warehouseOpen = false;
            viaLink = false;
        }

        public void EnsureUIExists()
        {
            if (built && ContentPanel != null) return;
            Canvas host = null;
            foreach (var candidate in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (candidate.renderMode != RenderMode.WorldSpace && candidate.isRootCanvas)
                { host = candidate; break; }
            if (host == null) return;
            if (ContentPanel != null && ContentPanel != gameObject) ContentPanel.SetActive(false);
            var obsolete = host.transform.Find("Pig_Backpack_Popup");
            if (obsolete != null) { obsolete.gameObject.SetActive(false); Destroy(obsolete.gameObject); }

            var modal = MakeRect("Pig_SpaceBag_Modal", host.transform, Vector2.zero, Vector2.zero);
            Stretch(modal);
            var canvas = modal.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;
            modal.gameObject.AddComponent<GraphicRaycaster>();
            var shade = modal.gameObject.AddComponent<Image>();
            shade.color = new Color(.02f, .04f, .08f, .82f);
            shade.raycastTarget = true;
            ContentPanel = modal.gameObject;

            safeRoot = MakeRect("SafeArea", modal, Vector2.zero, Vector2.zero);
            Stretch(safeRoot);

            window = MakeRect("BagVessel", safeRoot, Vector2.zero, new Vector2(620, 620));
            Paint(window, UISpriteLoader.GetFrameWoodPanel(), new Color(0.93f, 0.87f, 0.79f, 0.99f), true);
            var inset = MakeRect("IndigoLining", window, Vector2.zero, Vector2.zero);
            Stretch(inset, 12);
            Paint(inset, UISpriteLoader.GetFrameParchment(), new Color(0.16f, 0.12f, 0.09f, 0.98f), true);

            // Header Banner (18-20pt bold)
            headerBanner = MakeRect("HeaderBanner", window, new Vector2(0, -10), new Vector2(592, 46));
            headerBanner.anchorMin = headerBanner.anchorMax = headerBanner.pivot = new Vector2(0.5f, 1f);
            Paint(headerBanner, UISpriteLoader.GetFrameSlotSelected(), new Color(0.46f, 0.21f, 0.14f, 0.98f), false);
            headerTitle = Label("HeaderTitle", headerBanner, new Vector2(-18, 0), new Vector2(510, 38), 19, new Color(1f, 0.94f, 0.68f));
            headerTitle.fontStyle = FontStyles.Bold;
            headerTitle.alignment = TextAlignmentOptions.Left;

            CloseButton = IconButton("Close", window, UISpriteLoader.GetIconCloseCross(), Ivory, Hide);
            CloseButton.GetComponent<RectTransform>().anchorMin = CloseButton.GetComponent<RectTransform>().anchorMax = Vector2.one;
            CloseButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-34, -32);

            // Carried / SpaceBag View
            bagView = MakeRect("CarriedObjects", window, new Vector2(0, 48), new Vector2(520, 340));
            bagSectionTitle = Label("BagSectionTitle", bagView, new Vector2(0, 152), new Vector2(460, 26), 16, Gold);
            bagSectionTitle.fontStyle = FontStyles.Bold;
            bagSectionTitle.text = "HÀNH TRANG MANG THEO (ICON 2.5D)";

            var halo = MakeRect("VesselGlow", bagView, new Vector2(0, 8), new Vector2(310, 236));
            Paint(halo, UISpriteLoader.GetSoftCircleSprite(), new Color(.82f, .58f, .22f, .15f));
            var vessel = MakeRect("Bag", bagView, new Vector2(0, 8), new Vector2(88, 88));
            Paint(vessel, UISpriteLoader.GetIconBackpack(), Ivory);

            objectsRoot = MakeRect("OwnedObjects", bagView, new Vector2(0, 8), Vector2.zero);
            Stretch(objectsRoot);

            capacityRoot = MakeRect("CapacityRunes", bagView, new Vector2(0, -130), new Vector2(280, 22));

            // Contract #2 requirement: CarriedCount wide at 280x30, 16pt bold, NoWrap
            counter = Label("CarriedCount", bagView, new Vector2(0, -158), new Vector2(280, 30), 16, Ivory);
            counter.fontStyle = FontStyles.Bold;
            counter.textWrappingMode = TextWrappingModes.NoWrap;
            counter.overflowMode = TextOverflowModes.Overflow;

            // Warehouse View (Icon-First 5-column Grid of 72x72 cells)
            warehouseView = MakeRect("Warehouse", window, new Vector2(-248, 48), new Vector2(436, 360));
            var seal = MakeRect("WarehouseSeal", warehouseView, new Vector2(-176, 154), new Vector2(40, 40));
            Paint(seal, MythUi.Icon("places", "kho", 24) ?? UISpriteLoader.GetBanditOutpostSprite() ?? UISpriteLoader.GetIconBackpack(), Color.white);
            warehouseSectionTitle = Label("WarehouseTitle", warehouseView, new Vector2(16, 154), new Vector2(340, 28), 16, Gold);
            warehouseSectionTitle.fontStyle = FontStyles.Bold;
            warehouseSectionTitle.alignment = TextAlignmentOptions.Left;
            warehouseSectionTitle.text = "KHO VẬT TƯ TRẠI (CHẠM ĐỂ XEM / LẤY)";

            var viewport = MakeRect("StockViewport", warehouseView, new Vector2(0, -14), new Vector2(424, 316));
            Paint(viewport, UISpriteLoader.GetFrameSlotNormal(), new Color(0.12f, 0.09f, 0.07f, 0.96f), true);
            viewport.gameObject.AddComponent<RectMask2D>();
            stockRoot = MakeRect("OwnedWarehouseStock", viewport, Vector2.zero, new Vector2(408, 0));
            stockRoot.anchorMin = new Vector2(0, 1);
            stockRoot.anchorMax = Vector2.one;
            stockRoot.pivot = new Vector2(.5f, 1);
            stockRoot.sizeDelta = Vector2.zero;
            stockScroll = viewport.gameObject.AddComponent<ScrollRect>();
            stockScroll.viewport = viewport;
            stockScroll.content = stockRoot;
            stockScroll.horizontal = false;
            stockScroll.vertical = true;
            stockScroll.movementType = ScrollRect.MovementType.Clamped;

            transferGlyphs = MakeRect("TransferDirections", window, new Vector2(-14, 48), new Vector2(40, 60));
            Arrow(transferGlyphs, new Vector2(0, 12), false);
            Arrow(transferGlyphs, new Vector2(0, -12), true);

            // Rich Bottom Visual Item Inspector Banner (64x64 icon, 18pt bold name, category/rarity badge, 3 action buttons)
            detailRoot = MakeRect("SelectedItem", window, new Vector2(0, -196), new Vector2(576, 134));
            Paint(detailRoot, UISpriteLoader.GetFrameSlotNormal(), new Color(0.24f, 0.17f, 0.12f, 0.98f));

            var detailIconBox = MakeRect("IconBox", detailRoot, new Vector2(-236, 14), new Vector2(76, 76));
            detailIconBoxBg = Paint(detailIconBox, UISpriteLoader.GetFrameSlotSelected(), new Color(0.18f, 0.14f, 0.10f, 0.98f));
            DetailIcon = Paint(MakeRect("Icon", detailIconBox, Vector2.zero, new Vector2(64, 64)), null, Color.white);

            DetailTitleText = Label("ItemName", detailRoot, new Vector2(-24, 38), new Vector2(320, 26), 18, new Color(1f, 0.93f, 0.66f));
            DetailTitleText.fontStyle = FontStyles.Bold;
            DetailTitleText.alignment = TextAlignmentOptions.Left;

            var catBadgeRt = MakeRect("CategoryBadge", detailRoot, new Vector2(204, 38), new Vector2(146, 26));
            detailCategoryBadgeBg = Paint(catBadgeRt, UISpriteLoader.GetFrameSlotSelected(), new Color(0.36f, 0.24f, 0.14f, 0.96f));
            DetailCategoryText = Label("CategoryText", catBadgeRt, Vector2.zero, new Vector2(138, 24), 14, Ivory);
            DetailCategoryText.fontStyle = FontStyles.Bold;

            DetailDescriptionText = Label("ItemDescription", detailRoot, new Vector2(44, 2), new Vector2(460, 40), 14, new Color(.92f, .88f, .78f));
            DetailDescriptionText.alignment = TextAlignmentOptions.TopLeft;
            DetailDescriptionText.textWrappingMode = TextWrappingModes.Normal;

            // 1-Click Action Bar at the bottom of the Inspector Banner
            handOrSwapButton = CreateActionButton("EquipOrHandBtn", detailRoot, new Vector2(-176, -42), new Vector2(180, 38),
                new Color(0.22f, 0.38f, 0.52f, 0.98f), "Đưa Lên Tay / Đổi Đồ", OnHandOrSwapClicked, out handOrSwapButtonText);

            warehouseTransferButton = CreateActionButton("WarehouseTransferBtn", detailRoot, new Vector2(12, -42), new Vector2(176, 38),
                new Color(0.42f, 0.28f, 0.15f, 0.98f), "Cất Vào Kho", OnWarehouseTransferClicked, out warehouseTransferButtonText);

            useButton = CreateActionButton("UseItem", detailRoot, new Vector2(190, -42), new Vector2(160, 38),
                new Color(0.20f, 0.46f, 0.24f, 0.98f), "Dùng Ngay", UseSelected, out useButtonText);

            DetailActionButton = useButton;
            DetailActionButtonText = useButtonText;

            feedback = Label("CommandFailure", window, new Vector2(0, -280), new Vector2(560, 26), 15, new Color(1f, .74f, .50f));
            feedback.fontStyle = FontStyles.Bold;

            built = true;
            Layout();
            ContentPanel.SetActive(false);
        }

        static Button CreateActionButton(string name, Transform parent, Vector2 pos, Vector2 size, Color bgColor, string text, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI btnLabel)
        {
            var rt = MakeRect(name, parent, pos, size);
            var bg = Paint(rt, UISpriteLoader.GetFrameActionButton() ?? UISpriteLoader.GetFrameSlotSelected(), bgColor, true);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);
            btnLabel = Label("Text", rt, Vector2.zero, new Vector2(size.x - 10f, size.y - 4f), 14, Ivory);
            btnLabel.fontStyle = FontStyles.Bold;
            btnLabel.raycastTarget = false;
            return btn;
        }

        void Layout()
        {
            lastSafeArea = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Mathf.Max(1, Screen.width), lastSafeArea.yMin / Mathf.Max(1, Screen.height));
            safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Mathf.Max(1, Screen.width), lastSafeArea.yMax / Mathf.Max(1, Screen.height));
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            var available = safeRoot.rect.size;
            bool portrait = available.x < available.y;
            bool stacked = warehouseOpen && portrait;
            float width = warehouseOpen && !stacked ? 1020 : 620;
            float height = stacked ? 1020 : 620;
            window.sizeDelta = new Vector2(width, height);
            if (headerBanner != null) headerBanner.sizeDelta = new Vector2(width - 28f, 46f);
            if (headerTitle != null) headerTitle.rectTransform.sizeDelta = new Vector2(width - 104f, 38f);
            float scale = Mathf.Min(portrait ? 2.5f : 1f, Mathf.Min((available.x - 24) / width, (available.y - 24) / height));
            window.localScale = Vector3.one * Mathf.Max(.1f, scale);

            bagView.anchoredPosition = stacked ? new Vector2(0, -120) : new Vector2(warehouseOpen ? 236 : 0, 52);
            warehouseView.anchoredPosition = stacked ? new Vector2(0, 250) : new Vector2(-254, 52);
            detailRoot.anchoredPosition = stacked ? new Vector2(0, -384) : new Vector2(0, -196);
            detailRoot.sizeDelta = new Vector2(warehouseOpen && !stacked ? 964 : 576, 134);
            feedback.rectTransform.anchoredPosition = new Vector2(0, stacked ? -468 : -280);
            transferGlyphs.anchoredPosition = stacked ? new Vector2(0, 62) : new Vector2(-14, 52);
            transferGlyphs.localRotation = Quaternion.Euler(0, 0, stacked ? 90 : 0);
            warehouseView.gameObject.SetActive(warehouseOpen);
            transferGlyphs.gameObject.SetActive(warehouseOpen);
        }

        public void RefreshData()
        {
            if (!built || Engine == null) return;
            float scrollPosition = stockScroll != null ? stockScroll.verticalNormalizedPosition : 1f;
            Clear(objectsRoot);
            Clear(capacityRoot);
            Clear(stockRoot);
            carriedSlotFrames.Clear();
            stockSlotFrames.Clear();

            var carried = Carried();
            int capacity = CarriedCapacity();
            visibleSignature = StateSignature();
            nextStockCheck = Time.unscaledTime + .25f;

            if (headerTitle != null)
            {
                headerTitle.text = warehouseOpen
                    ? (viaLink ? "LIÊN KẾT KHO TỪ XA  ·  TÚI KHÔNG GIAN" : "KHO VẬT TƯ TRẠI  ·  TÚI KHÔNG GIAN")
                    : (Engine.Carry.HasBracelet ? "TÚI KHÔNG GIAN 2.5D [B]" : "HÀNH TRANG TRÊN TAY [B]");
            }

            int usedSlots = Engine.Carry.HasBracelet ? Engine.SpaceBag.UsedSlots : carried.Count;
            counter.text = $"Ô chứa: {usedSlots} / {capacity} · Đang mang {carried.Count} món";

            var pipOn = MythUi.Kit("day_pip_on");
            var pipOff = MythUi.Kit("day_pip_off");
            for (int i = 0; i < capacity; i++)
            {
                bool lit = i < usedSlots;
                var rune = MakeRect("Capacity_" + i, capacityRoot, new Vector2((i - (capacity - 1) * .5f) * 24, 0), new Vector2(14, 14));
                var pipSpr = lit ? pipOn : pipOff;
                if (pipSpr != null)
                {
                    Paint(rune, pipSpr, Color.white);
                }
                else
                {
                    rune.localRotation = Quaternion.Euler(0, 0, 45);
                    Paint(rune, null, lit ? Gold : new Color(.38f, .41f, .43f));
                }
            }

            // Icon-First 72x72 Carried Slots around the 2.5D Bag Vessel
            int ringCount = Mathf.Max(carried.Count, 1);
            for (int i = 0; i < carried.Count; i++)
            {
                int index = i;
                var expected = carried[i];
                float angle = (90f + i * (360f / ringCount)) * Mathf.Deg2Rad;
                Vector2 pos = carried.Count == 1
                    ? new Vector2(0, 88)
                    : new Vector2(Mathf.Cos(angle) * 156f, Mathf.Sin(angle) * 96f);

                bool isSel = !selectedFromWarehouse && i == selectedIndex;
                var slotFrameImg = CreateIconFirstCell(
                    "Carried_" + i,
                    objectsRoot,
                    pos,
                    expected,
                    isSel,
                    () => OnCarriedCellClicked(index, expected));
                carriedSlotFrames.Add(slotFrameImg);
            }

            if (warehouseOpen)
            {
                BuildStockGrid();
                if (stockScroll != null) stockScroll.verticalNormalizedPosition = scrollPosition;
            }

            RefreshInspectorFromSelection();
        }

        void BuildStockGrid()
        {
            var stock = Engine.Inventory.Slots;
            const int cols = 5;
            const float cellSize = 72f;
            const float gap = 8f;
            int rows = Mathf.Max(1, Mathf.CeilToInt(stock.Count / (float)cols));
            stockRoot.sizeDelta = new Vector2(0, rows * (cellSize + gap) + 16f);

            float startX = -((cols - 1) * (cellSize + gap)) * 0.5f;
            for (int i = 0; i < stock.Count; i++)
            {
                int index = i;
                var expected = stock[i].Clone();
                int col = i % cols;
                int row = i / cols;
                Vector2 pos = new Vector2(startX + col * (cellSize + gap), -12f - cellSize * 0.5f - row * (cellSize + gap));
                bool isSel = selectedFromWarehouse && i == selectedIndex;
                var slotFrameImg = CreateIconFirstCell(
                    "Stock_" + i,
                    stockRoot,
                    pos,
                    expected,
                    isSel,
                    () => OnStockCellClicked(index, expected),
                    topAnchored: true);
                stockSlotFrames.Add(slotFrameImg);
            }
        }

        /// <summary>
        /// Creates a 72x72 Icon-First inventory square:
        /// - Outer Rarity Bevel Rim
        /// - Crisp 44x44 2.5D Item Icon
        /// - Top-left Roman Quality Badge (I-IV at 14pt bold)
        /// - Bottom-right Stack Badge (xN at 14pt bold)
        /// - Zero multi-line item names inside grid squares!
        /// </summary>
        static Image CreateIconFirstCell(
            string name,
            Transform parent,
            Vector2 pos,
            UnifiedItemInstance item,
            bool selected,
            UnityEngine.Events.UnityAction onClick,
            bool topAnchored = false)
        {
            var slotRt = MakeRect(name, parent, pos, new Vector2(72, 72));
            if (topAnchored)
            {
                slotRt.anchorMin = slotRt.anchorMax = new Vector2(0.5f, 1f);
            }

            int qualityTier = Mathf.Clamp(item.Quality, 0, 3);
            Color rimColor = selected
                ? new Color(1f, 0.86f, 0.36f, 1f)
                : UISpriteLoader.GetQualityFrameColor((ItemTier)qualityTier);

            var slotBgSpr = selected ? UISpriteLoader.GetFrameSlotSelected() : UISpriteLoader.GetFrameSlotNormal();
            var slotBg = Paint(slotRt, slotBgSpr, rimColor, true);

            var innerRt = MakeRect("InnerWell", slotRt, Vector2.zero, new Vector2(62, 62));
            Paint(innerRt, UISpriteLoader.GetFrameSlotNormal(), UISpriteLoader.GetQualitySlotBackground((ItemTier)qualityTier), false);

            var button = slotRt.gameObject.AddComponent<Button>();
            button.targetGraphic = slotBg;
            button.onClick.AddListener(onClick);

            // 44x44 crisp 2.5D Item Icon
            var itemSpr = UISpriteLoader.GetLootSprite(item.ItemId);
            var iconRt = MakeRect("ItemIcon", slotRt, Vector2.zero, new Vector2(44, 44));
            Paint(iconRt, itemSpr, Color.white, false);

            // Roman Quality Rim Badge (I-IV at 14pt bold)
            string roman = UISpriteLoader.QualityRoman((ItemTier)qualityTier);
            var qualLbl = Label("QualityBadge", slotRt, new Vector2(-18, 22), new Vector2(30, 20), 14, UISpriteLoader.GetQualityFrameColor((ItemTier)qualityTier));
            qualLbl.fontStyle = FontStyles.Bold;
            qualLbl.alignment = TextAlignmentOptions.TopLeft;
            qualLbl.text = roman;

            // Bold Corner Stack Badge (x5 at 14pt bold)
            var stackLbl = Label("Stack", slotRt, new Vector2(12, -22), new Vector2(42, 20), 14, item.Stack > 1 ? Gold : new Color(0.85f, 0.82f, 0.74f, 0.88f));
            stackLbl.fontStyle = FontStyles.Bold;
            stackLbl.alignment = TextAlignmentOptions.BottomRight;
            stackLbl.text = "×" + Mathf.Max(1, item.Stack);

            return slotBg;
        }

        void OnCarriedCellClicked(int index, UnifiedItemInstance expected)
        {
            if (warehouseOpen && !selectedFromWarehouse && selectedIndex == index)
            {
                Return(index, expected);
                return;
            }
            SelectSlot(index);
        }

        void OnStockCellClicked(int index, UnifiedItemInstance expected)
        {
            if (warehouseOpen && selectedFromWarehouse && selectedIndex == index)
            {
                Withdraw(index, expected);
                return;
            }
            selectedFromWarehouse = true;
            selectedIndex = index;
            inspectToggleStep++;
            UpdateSelectionHighlightsInPlace();
            RefreshInspectorFromSelection();
        }

        void Withdraw(int index, UnifiedItemInstance expected)
        {
            if (!IsWarehouseOpen || Engine == null) return;
            var stock = Engine.Inventory.Slots;
            string message;
            if (index < 0 || index >= stock.Count || !SameItem(stock[index], expected))
                message = "Kho đã thay đổi.";
            else if (Engine.Carry.HasBracelet
                ? Engine.Carry.TryWithdrawToBag(index, !viaLink, out message)
                : Engine.Carry.TryTakeToHand(index, out message))
            {
                selectedFromWarehouse = false;
                selectedIndex = Mathf.Max(0, Carried().Count - 1);
            }
            RefreshData();
            if (!string.IsNullOrEmpty(message)) Fail(message);
        }

        void Return(int index, UnifiedItemInstance expected)
        {
            if (!IsWarehouseOpen || Engine == null) return;
            var carried = Carried();
            string message;
            if (index < 0 || index >= carried.Count || !SameItem(carried[index], expected))
                message = "Túi đã thay đổi.";
            else if (Engine.Carry.HasBracelet
                ? Engine.Carry.TryReturnFromBag(index, !viaLink, out message)
                : Engine.Carry.TryDepositHand(out message))
            {
                selectedIndex = Carried().Count > 0 ? 0 : -1;
            }
            RefreshData();
            if (!string.IsNullOrEmpty(message)) Fail(message);
        }

        static bool SameItem(UnifiedItemInstance a, UnifiedItemInstance b) =>
            a.QualifiedItemId == b.QualifiedItemId && a.Quality == b.Quality && a.ItemId == b.ItemId && a.DisplayName == b.DisplayName;

        void Fail(string message)
        {
            inspectToggleStep++;
            if (feedback != null)
                feedback.text = $"{message} (#{inspectToggleStep})";
            feedbackUntil = Time.unscaledTime + 3.5f;
        }

        public void SelectSlot(int index)
        {
            if (!IsOpen || Engine == null) return;
            var items = Carried();
            if (index < 0 || index >= items.Count) return;
            selectedFromWarehouse = false;
            selectedIndex = index;
            inspectToggleStep++;
            UpdateSelectionHighlightsInPlace();
            ShowDetail(items[index]);
        }

        void UpdateSelectionHighlightsInPlace()
        {
            var carried = Carried();
            for (int i = 0; i < carriedSlotFrames.Count && i < carried.Count; i++)
            {
                if (carriedSlotFrames[i] == null) continue;
                bool isSel = !selectedFromWarehouse && i == selectedIndex;
                int q = Mathf.Clamp(carried[i].Quality, 0, 3);
                carriedSlotFrames[i].sprite = isSel ? UISpriteLoader.GetFrameSlotSelected() : UISpriteLoader.GetFrameSlotNormal();
                carriedSlotFrames[i].color = isSel ? new Color(1f, 0.86f, 0.36f, 1f) : UISpriteLoader.GetQualityFrameColor((ItemTier)q);
            }
            if (warehouseOpen && Engine != null)
            {
                var stock = Engine.Inventory.Slots;
                for (int i = 0; i < stockSlotFrames.Count && i < stock.Count; i++)
                {
                    if (stockSlotFrames[i] == null) continue;
                    bool isSel = selectedFromWarehouse && i == selectedIndex;
                    int q = Mathf.Clamp(stock[i].Quality, 0, 3);
                    stockSlotFrames[i].sprite = isSel ? UISpriteLoader.GetFrameSlotSelected() : UISpriteLoader.GetFrameSlotNormal();
                    stockSlotFrames[i].color = isSel ? new Color(1f, 0.86f, 0.36f, 1f) : UISpriteLoader.GetQualityFrameColor((ItemTier)q);
                }
            }
        }

        void RefreshInspectorFromSelection()
        {
            if (Engine == null) return;
            var carried = Carried();
            var stock = Engine.Inventory.Slots;

            if (selectedFromWarehouse)
            {
                if (warehouseOpen && selectedIndex >= 0 && selectedIndex < stock.Count)
                {
                    ShowDetail(stock[selectedIndex]);
                    return;
                }
                selectedFromWarehouse = false;
            }

            if (selectedIndex >= 0 && selectedIndex < carried.Count)
            {
                ShowDetail(carried[selectedIndex]);
            }
            else if (carried.Count > 0)
            {
                selectedIndex = 0;
                ShowDetail(carried[0]);
            }
            else if (warehouseOpen && stock.Count > 0)
            {
                selectedFromWarehouse = true;
                selectedIndex = 0;
                ShowDetail(stock[0]);
            }
            else
            {
                selectedIndex = -1;
                ShowEmptyDetail();
            }
        }

        void ShowEmptyDetail()
        {
            detailRoot.gameObject.SetActive(true);
            DetailIcon.sprite = UISpriteLoader.GetIconBackpack();
            DetailIcon.enabled = DetailIcon.sprite != null;
            DetailTitleText.text = "Chưa có vật phẩm trong ô chọn";
            if (DetailCategoryText != null) DetailCategoryText.text = "TRỐNG · BẬC I";
            DetailDescriptionText.text = "Hãy chạm vào bất kỳ ô vật phẩm 2.5D nào trong túi hoặc kho để xem chi tiết và thao tác nhanh.";
            if (handOrSwapButtonText != null) handOrSwapButtonText.text = "Đưa Lên Tay / Đổi Đồ";
            if (warehouseTransferButtonText != null) warehouseTransferButtonText.text = "Cất Vào Kho";
            if (useButtonText != null) useButtonText.text = "Dùng Ngay";
        }

        void ShowDetail(UnifiedItemInstance item)
        {
            detailRoot.gameObject.SetActive(true);
            DetailIcon.sprite = UISpriteLoader.GetLootSprite(item.ItemId);
            DetailIcon.enabled = DetailIcon.sprite != null;

            int qualityTier = Mathf.Clamp(item.Quality, 0, 3);
            string roman = UISpriteLoader.QualityRoman((ItemTier)qualityTier);
            Color qCol = UISpriteLoader.GetQualityFrameColor((ItemTier)qualityTier);
            if (detailIconBoxBg != null) detailIconBoxBg.color = qCol;
            if (detailCategoryBadgeBg != null) detailCategoryBadgeBg.color = new Color(qCol.r * 0.45f, qCol.g * 0.45f, qCol.b * 0.45f, 0.96f);

            DetailTitleText.text = $"{ItemName(item)}  (×{Mathf.Max(1, item.Stack)})";

            var data = QualifiedItemRegistry.GetData(item.QualifiedItemId);
            string catLabel = data != null && !string.IsNullOrEmpty(data.Category) ? data.Category.ToUpperInvariant() : "VẬT PHẨM";
            if (DetailCategoryText != null)
                DetailCategoryText.text = $"{catLabel} · BẬC {roman}";

            string baseDesc = data != null && !string.IsNullOrEmpty(data.Description)
                ? data.Description
                : "Vật tư trang trại & chiến đấu 2.5D. Có thể mang theo, cất kho hoặc sử dụng trực tiếp.";
            string sourceTag = selectedFromWarehouse ? "[Đang ở Kho Trại]" : "[Đang mang bên mình]";
            DetailDescriptionText.text = (inspectToggleStep % 2 == 1)
                ? $"{sourceTag} {baseDesc} · Đã kiểm định phẩm chất bậc {roman}."
                : $"{sourceTag} {baseDesc}";

            string bareId = ItemUse.BareId(item.ItemId);
            bool isQuickConsumable = QuickBag.TryParseWarehouseId(bareId, out var quickId);

            if (handOrSwapButtonText != null)
            {
                if (selectedFromWarehouse)
                    handOrSwapButtonText.text = isQuickConsumable
                        ? $"Nạp Túi Nhanh ({QuickBag.Count(Engine.Character, quickId)}/{QuickBag.MaxPerKind})"
                        : "Đưa Lên Tay / Lấy Ra";
                else
                    handOrSwapButtonText.text = "Đưa Lên Tay / Đổi Đồ";
            }

            if (warehouseTransferButtonText != null)
            {
                warehouseTransferButtonText.text = selectedFromWarehouse ? "Rút Về Túi [B]" : "Cất Vào Kho";
            }

            bool usable = ItemUse.IsUsable(bareId);
            bool forPig = usable && ItemUse.NeedsPig(bareId);
            if (useButtonText != null)
            {
                useButtonText.text = forPig ? "Cho Heo Ăn" : "Dùng Ngay";
            }
            useButton.gameObject.SetActive(true);
            useButton.interactable = true;
        }

        void OnHandOrSwapClicked()
        {
            if (Engine == null) return;
            if (selectedFromWarehouse)
            {
                var stock = Engine.Inventory.Slots;
                if (selectedIndex >= 0 && selectedIndex < stock.Count)
                {
                    var item = stock[selectedIndex];
                    string bareId = ItemUse.BareId(item.ItemId);
                    if (QuickBag.TryParseWarehouseId(bareId, out var quickId))
                    {
                        var (ok, msg) = QuickBag.LoadFromWarehouse(Engine, quickId, inPerson: !viaLink);
                        ToastOverlay.Show(ok ? (msg + " (F5–F8)") : msg);
                        Fail(ok ? (msg + " (F5–F8)") : msg);
                        RefreshData();
                        MobileGameController.Instance?.OnStateUpdated?.Invoke();
                        return;
                    }
                    Withdraw(selectedIndex, item.Clone());
                    return;
                }
            }

            var carried = Carried();
            if (selectedIndex >= 0 && selectedIndex < carried.Count)
            {
                var chosen = carried[selectedIndex];
                string msg = $"Đã sẵn sàng trên tay: {ItemName(chosen)} (×{chosen.Stack})";
                ToastOverlay.Show(msg);
                Fail(msg);
                ShowDetail(chosen);
                return;
            }

            Fail("Chưa chọn vật phẩm để đưa lên tay.");
        }

        void OnWarehouseTransferClicked()
        {
            if (Engine == null) return;
            if (selectedFromWarehouse)
            {
                var stock = Engine.Inventory.Slots;
                if (selectedIndex >= 0 && selectedIndex < stock.Count)
                {
                    Withdraw(selectedIndex, stock[selectedIndex].Clone());
                    return;
                }
            }

            var carried = Carried();
            if (selectedIndex >= 0 && selectedIndex < carried.Count)
            {
                if (!warehouseOpen && Engine.Carry.CanUseWarehouseLink(out _))
                {
                    warehouseOpen = true;
                    viaLink = true;
                    Layout();
                }
                if (warehouseOpen)
                {
                    Return(selectedIndex, carried[selectedIndex]);
                    return;
                }
                string msg = "Hãy đứng gần Kho Vật Tư hoặc đeo Vòng Tay Không Gian để cất đồ vào kho.";
                ToastOverlay.Show(msg);
                Fail(msg);
                return;
            }

            Fail("Chưa có vật phẩm để chuyển kho.");
        }

        void UseSelected()
        {
            if (Engine == null) return;
            if (selectedFromWarehouse)
            {
                var stock = Engine.Inventory.Slots;
                if (selectedIndex >= 0 && selectedIndex < stock.Count)
                {
                    Withdraw(selectedIndex, stock[selectedIndex].Clone());
                    return;
                }
            }

            var items = Carried();
            if (selectedIndex < 0 || selectedIndex >= items.Count)
            {
                Fail("Hãy chọn một vật phẩm trong túi trước khi dùng.");
                return;
            }
            string id = ItemUse.BareId(items[selectedIndex].ItemId);
            if (!ItemUse.IsUsable(id))
            {
                string msg = $"{ItemName(items[selectedIndex])}: Vật tư nguyên liệu / trang bị (dùng tại công trình hoặc bảng Nhân Vật).";
                ToastOverlay.Show(msg);
                Fail(msg);
                return;
            }
            if (ItemUse.NeedsPig(id))
            {
                string msg = "Chạm vào con heo cần chăm, rồi chọn vật phẩm trên thẻ của nó.";
                ToastOverlay.Show(msg);
                Fail(msg);
                return;
            }

            var player = PlayerInteractionController.Instance;
            float px = player != null ? player.transform.position.x : float.NaN;
            float py = player != null ? player.transform.position.y : float.NaN;
            var (ok, message) = ItemUse.Use(Engine, id, null, Engine.Carry.HasBracelet ? selectedIndex : -1, atWarehouse: warehouseOpen && !viaLink, placeX: px, placeY: py);
            ToastOverlay.Show(message);
            if (ok)
            {
                selectedIndex = Carried().Count > 0 ? 0 : -1;
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
            }
            RefreshData();
            if (!string.IsNullOrEmpty(message)) Fail(message);
        }

        static string ItemName(UnifiedItemInstance item)
        {
            var data = QualifiedItemRegistry.GetData(item.QualifiedItemId);
            var name = data != null ? data.DisplayName : item.DisplayName;
            string roman = UISpriteLoader.QualityRoman((ItemTier)Mathf.Clamp(item.Quality, 0, 3));
            return item.Quality > 0 ? $"{name} [{roman}]" : name;
        }

        int StateSignature()
        {
            unchecked
            {
                int hash = CarriedCapacity() * 2 + (Engine.Carry.HasBracelet ? 1 : 0);
                foreach (var item in Carried()) hash = hash * 31 + ItemSignature(item);
                if (warehouseOpen) foreach (var item in Engine.Inventory.Slots) hash = hash * 31 + ItemSignature(item);
                return hash;
            }
        }

        static int ItemSignature(UnifiedItemInstance item)
        {
            unchecked { return ((item.QualifiedItemId?.GetHashCode() ?? 0) * 397 ^ item.Quality) * 31 + item.Stack; }
        }

        static void Arrow(Transform parent, Vector2 position, bool reverse)
        {
            var arrow = MakeRect("Transfer", parent, position, new Vector2(30, 20));
            if (reverse) arrow.localRotation = Quaternion.Euler(0, 0, 180);
            Paint(MakeRect("Stem", arrow, Vector2.zero, new Vector2(22, 2)), null, Gold);
            foreach (float direction in new[] { -1f, 1f })
            {
                var arm = MakeRect("ArrowHead", arrow, new Vector2(8, direction * 3), new Vector2(10, 2));
                arm.localRotation = Quaternion.Euler(0, 0, direction * -45);
                Paint(arm, null, Gold);
            }
        }

        static RectTransform MakeRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        static void Stretch(RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.one * inset;
            rt.offsetMax = -Vector2.one * inset;
        }

        static Image Paint(RectTransform rt, Sprite sprite, Color color, bool raycast = false)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            image.preserveAspect = !raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0) image.type = Image.Type.Sliced;
            return image;
        }

        static Button IconButton(string name, Transform parent, Sprite sprite, Color color, UnityEngine.Events.UnityAction action)
        {
            var rt = MakeRect(name, parent, Vector2.zero, new Vector2(52, 52));
            var image = Paint(rt, sprite, color, true);
            image.preserveAspect = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            return button;
        }

        static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var label = MakeRect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.richText = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        static void Clear(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}
