using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class InventoryItem
    {
        public string Id;
        public string Name;
        public string Category;
        public int Quantity;
        public string QuantityDisplay;
        public Sprite Icon;
        public string Description;
        public StardewToolType? BoundTool;
        public Action OnUseAction;
    }

    public class BackpackPopup : MonoBehaviour
    {
        public static BackpackPopup Instance { get; private set; }

        [Header("UI Root")]
        public GameObject ContentPanel;
        public Button CloseButton;

        [Header("Minecraft Grid Slots (36 Slots Total)")]
        // 27 storage slots (3x9) + 9 hotbar slots (1x9)
        private readonly GameObject[] slotObjects = new GameObject[36];
        private readonly Image[] slotBackgrounds = new Image[36];
        private readonly Image[] slotIcons = new Image[36];
        private readonly TextMeshProUGUI[] slotQuantities = new TextMeshProUGUI[36];

        [Header("Selected Item Detail Panel")]
        public Image DetailIcon;
        public TextMeshProUGUI DetailTitleText;
        public TextMeshProUGUI DetailCategoryText;
        public TextMeshProUGUI DetailDescriptionText;
        public Button DetailActionButton;
        public TextMeshProUGUI DetailActionButtonText;

        [Header("Drag and Drop System")]
        private GameObject dragGhostObj;
        private Image dragGhostIcon;
        private int draggingSourceIndex = -1;

        private readonly InventoryItem[] inventoryItems = new InventoryItem[36];
        private int selectedSlotIndex = 0;
        private bool isDataInitialized = false;

        public bool IsOpen => ContentPanel != null && ContentPanel.activeSelf;
        private GameObject modalBackdropObj;

        public InventoryItem[] GetInventoryItems()
        {
            if (!isDataInitialized) RefreshData();
            return inventoryItems;
        }

        public void DeductItemQuantity(string id, int count)
        {
            if (string.IsNullOrEmpty(id) || count <= 0) return;
            for (int i = 0; i < inventoryItems.Length; i++)
            {
                var item = inventoryItems[i];
                if (item != null && item.Id == id)
                {
                    item.Quantity = Mathf.Max(0, item.Quantity - count);
                    if (item.Quantity <= 0)
                    {
                        inventoryItems[i] = null;
                    }
                    else
                    {
                        item.QuantityDisplay = item.Quantity > 1 ? item.Quantity.ToString() : "";
                    }
                    break;
                }
            }
        }

        public void AddGameItem(GameItem gameItem, int count = 1)
        {
            if (gameItem == null || count <= 0) return;
            if (!isDataInitialized) RefreshData();

            for (int i = 0; i < inventoryItems.Length; i++)
            {
                var it = inventoryItems[i];
                if (it != null && it.Id == gameItem.Id)
                {
                    it.Quantity += count;
                    it.QuantityDisplay = it.Quantity.ToString();
                    return;
                }
            }

            for (int i = 0; i < 27; i++)
            {
                if (inventoryItems[i] == null)
                {
                    inventoryItems[i] = new InventoryItem
                    {
                        Id = gameItem.Id,
                        Name = gameItem.Name,
                        Category = gameItem.Category,
                        Quantity = count,
                        QuantityDisplay = count > 1 ? count.ToString() : "",
                        Description = gameItem.Description,
                        Icon = UISpriteLoader.GetLootSprite(gameItem.Id) ?? UISpriteLoader.GetIconWoodPlank()
                    };
                    return;
                }
            }
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
            // Phím B hoặc I để bật/tắt túi đồ Balo
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I))
            {
                Toggle();
            }

            // Phím Escape để đóng túi đồ
            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
            {
                Hide();
            }
        }

        public void Toggle()
        {
            EnsureUIExists();
            if (IsOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            EnsureUIExists();

            if (modalBackdropObj != null)
            {
                modalBackdropObj.transform.SetAsLastSibling();
                modalBackdropObj.SetActive(true);
            }

            if (ContentPanel != null)
            {
                ContentPanel.transform.SetAsLastSibling();
                ContentPanel.SetActive(true);
            }

            EnsureCloseButtonListener();
            if (CloseButton != null)
            {
                CloseButton.transform.SetAsLastSibling();
            }

            RefreshData();
            SelectSlot(selectedSlotIndex);
        }

        public void Hide()
        {
            if (modalBackdropObj != null) modalBackdropObj.SetActive(false);
            if (dragGhostObj != null) dragGhostObj.SetActive(false);
            if (ContentPanel != null) ContentPanel.SetActive(false);
        }

        public void EnsureUIExists()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Tạo hoặc liên kết Modal Backdrop làm mờ màn hình và chặn click thế giới
            EnsureModalBackdrop(canvas);

            // 2. Nếu đã có slotObjects[0] và ContentPanel và CloseButton thì chỉ cần đảm bảo binding
            if (ContentPanel != null && slotObjects[0] != null && CloseButton != null)
            {
                EnsureCloseButtonListener();
                return;
            }

            // 3. Tìm kiếm Pig_Backpack_Popup đã có trong Canvas (từ scene hoặc tạo trước đó)
            if (ContentPanel == null)
            {
                var existingPopup = canvas.transform.Find("Pig_Backpack_Popup");
                if (existingPopup != null)
                {
                    ContentPanel = existingPopup.gameObject;
                }
            }

            // 4. Nếu ContentPanel đã tồn tại -> Liên kết toàn bộ components và listeners
            if (ContentPanel != null)
            {
                BindExistingUI(canvas, ContentPanel);
                return;
            }

            // 5. Nếu chưa tồn tại trong scene -> Khởi tạo mới từ code
            CreateFreshUI(canvas);
        }

        private void EnsureModalBackdrop(Canvas canvas)
        {
            if (modalBackdropObj == null)
            {
                var existing = canvas.transform.Find("Pig_Backpack_Backdrop");
                if (existing != null)
                {
                    modalBackdropObj = existing.gameObject;
                }
                else
                {
                    modalBackdropObj = new GameObject("Pig_Backpack_Backdrop");
                    modalBackdropObj.transform.SetParent(canvas.transform, false);
                    var mbRt = modalBackdropObj.AddComponent<RectTransform>();
                    mbRt.anchorMin = Vector2.zero;
                    mbRt.anchorMax = Vector2.one;
                    mbRt.offsetMin = Vector2.zero;
                    mbRt.offsetMax = Vector2.zero;
                    var mbImg = modalBackdropObj.AddComponent<Image>();
                    mbImg.color = new Color(0f, 0f, 0f, 0.45f);
                    mbImg.raycastTarget = true;
                    var mbBtn = modalBackdropObj.AddComponent<Button>();
                    mbBtn.onClick.AddListener(Hide);
                    modalBackdropObj.SetActive(false);
                }
            }
        }

        private void EnsureCloseButtonListener()
        {
            if (CloseButton != null)
            {
                CloseButton.onClick.RemoveAllListeners();
                CloseButton.onClick.AddListener(Hide);
            }
        }

        private void EnsureCloseButtonLabel(Transform closeTr)
        {
            Transform lblTr = closeTr.Find("Label");
            TextMeshProUGUI tmp;
            if (lblTr == null)
            {
                var lblObj = new GameObject("Label");
                lblObj.transform.SetParent(closeTr, false);
                var lRt = lblObj.AddComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.offsetMin = Vector2.zero;
                lRt.offsetMax = Vector2.zero;
                tmp = lblObj.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                tmp = lblTr.GetComponent<TextMeshProUGUI>();
            }

            if (tmp != null)
            {
                tmp.text = "<b>✕</b>";
                tmp.fontSize = 20f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
            }
        }

        private void EnsureDragGhost(Canvas canvas)
        {
            if (dragGhostObj == null)
            {
                var existingGhost = canvas.transform.Find("DragGhostIcon");
                if (existingGhost != null)
                {
                    dragGhostObj = existingGhost.gameObject;
                    dragGhostIcon = dragGhostObj.GetComponent<Image>();
                }
                else
                {
                    var ghostObj = new GameObject("DragGhostIcon");
                    ghostObj.transform.SetParent(canvas.transform, false);
                    var gRt = ghostObj.AddComponent<RectTransform>();
                    gRt.sizeDelta = new Vector2(46f, 46f);
                    gRt.pivot = new Vector2(0.5f, 0.5f);
                    dragGhostIcon = ghostObj.AddComponent<Image>();
                    dragGhostIcon.preserveAspect = true;
                    dragGhostIcon.raycastTarget = false;
                    ghostObj.SetActive(false);
                    dragGhostObj = ghostObj;
                }
            }
        }

        private void BindExistingUI(Canvas canvas, GameObject popupObj)
        {
            ContentPanel = popupObj;

            Transform closeTr = popupObj.transform.Find("CloseButton");
            if (closeTr == null)
            {
                var closeObj = new GameObject("CloseButton");
                closeObj.transform.SetParent(popupObj.transform, false);
                closeTr = closeObj.transform;
            }

            var cbRt = closeTr.GetComponent<RectTransform>() ?? closeTr.gameObject.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1, 1);
            cbRt.anchorMax = new Vector2(1, 1);
            cbRt.pivot = new Vector2(1, 1);
            cbRt.anchoredPosition = new Vector2(-10, -10);
            cbRt.sizeDelta = new Vector2(40, 40);

            var cbImg = closeTr.GetComponent<Image>() ?? closeTr.gameObject.AddComponent<Image>();
            cbImg.sprite = UISpriteLoader.GetIconCloseCross();
            cbImg.color = Color.white;
            cbImg.raycastTarget = true;

            CloseButton = closeTr.GetComponent<Button>() ?? closeTr.gameObject.AddComponent<Button>();
            EnsureCloseButtonListener();
            EnsureCloseButtonLabel(closeTr);
            closeTr.SetAsLastSibling();

            Transform parchTr = popupObj.transform.Find("Parchment");
            if (parchTr != null)
            {
                for (int i = 0; i < 36; i++)
                {
                    Transform sTr = parchTr.Find($"Slot_{i}");
                    if (sTr != null)
                    {
                        slotObjects[i] = sTr.gameObject;
                        slotBackgrounds[i] = sTr.GetComponent<Image>();
                        slotIcons[i] = sTr.Find("Icon")?.GetComponent<Image>();
                        slotQuantities[i] = sTr.Find("Quantity")?.GetComponent<TextMeshProUGUI>();

                        var drag = sTr.GetComponent<BackpackSlotDragDrop>() ?? sTr.gameObject.AddComponent<BackpackSlotDragDrop>();
                        drag.SlotIndex = i;
                        drag.Popup = this;

                        var btn = sTr.GetComponent<Button>() ?? sTr.gameObject.AddComponent<Button>();
                        int slotIdx = i;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SelectSlot(slotIdx));
                    }
                }

                Transform dBox = parchTr.Find("ItemDetailBox");
                if (dBox != null)
                {
                    DetailIcon = dBox.Find("IconFrame/Icon")?.GetComponent<Image>();
                    DetailTitleText = dBox.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                    DetailCategoryText = dBox.Find("CategoryText")?.GetComponent<TextMeshProUGUI>();
                    DetailDescriptionText = dBox.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();

                    Transform abTr = dBox.Find("ActionButton");
                    if (abTr != null)
                    {
                        DetailActionButton = abTr.GetComponent<Button>();
                        DetailActionButtonText = abTr.Find("Text")?.GetComponent<TextMeshProUGUI>();
                        if (DetailActionButton != null)
                        {
                            DetailActionButton.onClick.RemoveAllListeners();
                            DetailActionButton.onClick.AddListener(OnDetailActionButtonClicked);
                        }
                    }
                }
            }

            EnsureDragGhost(canvas);
        }

        private void CreateFreshUI(Canvas canvas)
        {
            // 1. Popup Root Window (640 x 570 px) - Tông gỗ sẫm màu chống chói mắt
            var popupObj = new GameObject("Pig_Backpack_Popup");
            popupObj.transform.SetParent(canvas.transform, false);
            var pRt = popupObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.anchoredPosition = Vector2.zero;
            pRt.sizeDelta = new Vector2(640f, 570f);

            var pImg = popupObj.AddComponent<Image>();
            pImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            pImg.type = Image.Type.Sliced;
            pImg.color = new Color(0.35f, 0.23f, 0.14f, 1f); // Khung gỗ óc chó đậm sang trọng
            ContentPanel = popupObj;

            // 2. Parchment Inner Layer - Da thuộc cổ / giấy da sẫm màu dịu mắt
            var parchObj = new GameObject("Parchment");
            parchObj.transform.SetParent(popupObj.transform, false);
            var parchRt = parchObj.AddComponent<RectTransform>();
            parchRt.anchorMin = Vector2.zero;
            parchRt.anchorMax = Vector2.one;
            parchRt.offsetMin = new Vector2(12f, 12f);
            parchRt.offsetMax = new Vector2(-12f, -12f);
            var parchImg = parchObj.AddComponent<Image>();
            parchImg.sprite = UISpriteLoader.GetFrameParchment();
            parchImg.type = Image.Type.Sliced;
            parchImg.color = new Color(0.20f, 0.15f, 0.10f, 0.98f); // Nền da thuộc sẫm màu, hoàn toàn không chói

            // 3. Header Title - Chữ vàng ấm nổi bật trên nền tối
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(parchObj.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -10);
            tRt.sizeDelta = new Vector2(-80, 26);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>TÚI ĐỒ TRANG TRẠI (INVENTORY)</b>";
            titleTmp.fontSize = 18f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.92f, 0.72f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.raycastTarget = false;

            // 4. Close Button [X] - Đặt tại góc trên bên phải, raycastTarget = true, đảm bảo nhận click 100%
            var closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(popupObj.transform, false);
            var cbRt = closeObj.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1, 1);
            cbRt.anchorMax = new Vector2(1, 1);
            cbRt.pivot = new Vector2(1, 1);
            cbRt.anchoredPosition = new Vector2(-10, -10); // Đặt bên trong góc trên bên phải
            cbRt.sizeDelta = new Vector2(40, 40);
            var cbImg = closeObj.AddComponent<Image>();
            cbImg.sprite = UISpriteLoader.GetIconCloseCross();
            cbImg.color = Color.white;
            cbImg.raycastTarget = true;
            CloseButton = closeObj.AddComponent<Button>();
            EnsureCloseButtonListener();
            EnsureCloseButtonLabel(closeObj.transform);
            closeObj.transform.SetAsLastSibling();

            // 5. Main Storage Grid (3 rows x 9 columns = 27 square slots)
            var storageLabelObj = new GameObject("StorageLabel");
            storageLabelObj.transform.SetParent(parchObj.transform, false);
            var slRt = storageLabelObj.AddComponent<RectTransform>();
            slRt.anchorMin = new Vector2(0, 1);
            slRt.anchorMax = new Vector2(1, 1);
            slRt.pivot = new Vector2(0.5f, 1);
            slRt.anchoredPosition = new Vector2(0, -38);
            slRt.sizeDelta = new Vector2(534, 18);
            var slTmp = storageLabelObj.AddComponent<TextMeshProUGUI>();
            slTmp.text = "<b>Kho Đồ Balo (Kéo thả để sắp xếp)</b>";
            slTmp.fontSize = 12f;
            slTmp.color = new Color(0.92f, 0.82f, 0.62f);
            slTmp.alignment = TextAlignmentOptions.Left;

            float slotSize = 54f;
            float spacing = 6f;
            float startX = -((9 * slotSize + 8 * spacing) / 2f) + (slotSize / 2f);

            // Row 0 to 2: Storage slots 0 to 26
            for (int r = 0; r < 3; r++)
            {
                float posY = -64f - (r * (slotSize + spacing));
                for (int c = 0; c < 9; c++)
                {
                    int slotIdx = (r * 9) + c;
                    float posX = startX + (c * (slotSize + spacing));
                    CreateSquareSlot(parchObj.transform, slotIdx, new Vector2(posX, posY), slotSize);
                }
            }

            // 6. Hotbar Grid (1 row x 9 columns = 9 slots)
            var hotbarLabelObj = new GameObject("HotbarLabel");
            hotbarLabelObj.transform.SetParent(parchObj.transform, false);
            var hlRt = hotbarLabelObj.AddComponent<RectTransform>();
            hlRt.anchorMin = new Vector2(0, 1);
            hlRt.anchorMax = new Vector2(1, 1);
            hlRt.pivot = new Vector2(0.5f, 1);
            hlRt.anchoredPosition = new Vector2(0, -248);
            hlRt.sizeDelta = new Vector2(534, 18);
            var hlTmp = hotbarLabelObj.AddComponent<TextMeshProUGUI>();
            hlTmp.text = "<b>Thanh Công Cụ Nhanh (Hotbar 1 - 8 & Balo)</b>";
            hlTmp.fontSize = 12f;
            hlTmp.color = new Color(0.92f, 0.82f, 0.62f);
            hlTmp.alignment = TextAlignmentOptions.Left;

            float hotbarPosY = -272f;
            for (int c = 0; c < 9; c++)
            {
                int slotIdx = 27 + c;
                float posX = startX + (c * (slotSize + spacing));
                CreateSquareSlot(parchObj.transform, slotIdx, new Vector2(posX, hotbarPosY), slotSize, isHotbar: true, hotkeyNumber: c + 1);
            }

            // 7. Item Detail / Tooltip Box (Minecraft GUI bottom panel) - Khay gỗ tối màu
            var detailBoxObj = new GameObject("ItemDetailBox");
            detailBoxObj.transform.SetParent(parchObj.transform, false);
            var dRt = detailBoxObj.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0f);
            dRt.anchorMax = new Vector2(0.5f, 0f);
            dRt.pivot = new Vector2(0.5f, 0f);
            dRt.anchoredPosition = new Vector2(0, 32f);
            dRt.sizeDelta = new Vector2(534f, 96f);

            var dImg = detailBoxObj.AddComponent<Image>();
            dImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            dImg.type = Image.Type.Sliced;
            dImg.color = new Color(0.14f, 0.10f, 0.07f, 0.98f); // Khay gỗ đen sẫm cao cấp

            // Detail Icon Frame
            var dIconFrame = new GameObject("IconFrame");
            dIconFrame.transform.SetParent(detailBoxObj.transform, false);
            var difRt = dIconFrame.AddComponent<RectTransform>();
            difRt.anchorMin = new Vector2(0, 0.5f);
            difRt.anchorMax = new Vector2(0.5f, 0.5f);
            difRt.pivot = new Vector2(0.5f, 0.5f);
            difRt.anchoredPosition = new Vector2(40f, 0f);
            difRt.sizeDelta = new Vector2(52f, 52f);
            var difImg = dIconFrame.AddComponent<Image>();
            difImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            difImg.type = Image.Type.Sliced;
            difImg.color = new Color(0.35f, 0.28f, 0.20f);

            var dIconObj = new GameObject("Icon");
            dIconObj.transform.SetParent(dIconFrame.transform, false);
            var diRt = dIconObj.AddComponent<RectTransform>();
            diRt.anchorMin = new Vector2(0.5f, 0.5f);
            diRt.anchorMax = new Vector2(0.5f, 0.5f);
            diRt.pivot = new Vector2(0.5f, 0.5f);
            diRt.anchoredPosition = Vector2.zero;
            diRt.sizeDelta = new Vector2(38f, 38f);
            DetailIcon = dIconObj.AddComponent<Image>();
            DetailIcon.preserveAspect = true;

            // Detail Title
            var dtObj = new GameObject("TitleText");
            dtObj.transform.SetParent(detailBoxObj.transform, false);
            var dttRt = dtObj.AddComponent<RectTransform>();
            dttRt.anchorMin = new Vector2(0, 1);
            dttRt.anchorMax = new Vector2(1, 1);
            dttRt.pivot = new Vector2(0, 1);
            dttRt.anchoredPosition = new Vector2(76f, -8f);
            dttRt.sizeDelta = new Vector2(-190f, 22f);
            DetailTitleText = dtObj.AddComponent<TextMeshProUGUI>();
            DetailTitleText.fontSize = 15f;
            DetailTitleText.fontStyle = FontStyles.Bold;
            DetailTitleText.color = new Color(1f, 0.94f, 0.80f);

            // Detail Category & Subtitle
            var dcObj = new GameObject("CategoryText");
            dcObj.transform.SetParent(detailBoxObj.transform, false);
            var dctRt = dcObj.AddComponent<RectTransform>();
            dctRt.anchorMin = new Vector2(0, 1);
            dctRt.anchorMax = new Vector2(1, 1);
            dctRt.pivot = new Vector2(0, 1);
            dctRt.anchoredPosition = new Vector2(76f, -30f);
            dctRt.sizeDelta = new Vector2(-190f, 18f);
            DetailCategoryText = dcObj.AddComponent<TextMeshProUGUI>();
            DetailCategoryText.fontSize = 12f;
            DetailCategoryText.color = new Color(0.85f, 0.72f, 0.50f);

            // Detail Description
            var ddObj = new GameObject("DescriptionText");
            ddObj.transform.SetParent(detailBoxObj.transform, false);
            var ddtRt = ddObj.AddComponent<RectTransform>();
            ddtRt.anchorMin = new Vector2(0, 0);
            ddtRt.anchorMax = new Vector2(1, 1);
            ddtRt.pivot = new Vector2(0, 0);
            ddtRt.anchoredPosition = new Vector2(76f, 8f);
            ddtRt.sizeDelta = new Vector2(-190f, -54f);
            DetailDescriptionText = ddObj.AddComponent<TextMeshProUGUI>();
            DetailDescriptionText.fontSize = 11.5f;
            DetailDescriptionText.color = new Color(0.88f, 0.84f, 0.78f);

            // Action Button [TRANG BỊ] / [SỬ DỤNG]
            var actBtnObj = new GameObject("ActionButton");
            actBtnObj.transform.SetParent(detailBoxObj.transform, false);
            var abRt = actBtnObj.AddComponent<RectTransform>();
            abRt.anchorMin = new Vector2(1, 0.5f);
            abRt.anchorMax = new Vector2(1, 0.5f);
            abRt.pivot = new Vector2(1, 0.5f);
            abRt.anchoredPosition = new Vector2(-12f, 0f);
            abRt.sizeDelta = new Vector2(100f, 38f);
            var abImg = actBtnObj.AddComponent<Image>();
            abImg.sprite = UISpriteLoader.GetFrameActionButton();
            abImg.type = Image.Type.Sliced;
            abImg.color = Color.white;
            DetailActionButton = actBtnObj.AddComponent<Button>();
            DetailActionButton.onClick.AddListener(OnDetailActionButtonClicked);

            var abtObj = new GameObject("Text");
            abtObj.transform.SetParent(actBtnObj.transform, false);
            var abtRt = abtObj.AddComponent<RectTransform>();
            abtRt.anchorMin = Vector2.zero;
            abtRt.anchorMax = Vector2.one;
            abtRt.offsetMin = Vector2.zero;
            abtRt.offsetMax = Vector2.zero;
            DetailActionButtonText = abtObj.AddComponent<TextMeshProUGUI>();
            DetailActionButtonText.text = "<b>TRANG BỊ</b>";
            DetailActionButtonText.fontSize = 13f;
            DetailActionButtonText.fontStyle = FontStyles.Bold;
            DetailActionButtonText.color = Color.white;
            DetailActionButtonText.alignment = TextAlignmentOptions.Center;

            // 8. Footer Guide
            var footObj = new GameObject("FooterText");
            footObj.transform.SetParent(parchObj.transform, false);
            var fRt = footObj.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0, 0);
            fRt.anchorMax = new Vector2(1, 0);
            fRt.pivot = new Vector2(0.5f, 0);
            fRt.anchoredPosition = new Vector2(0, 8f);
            fRt.sizeDelta = new Vector2(0, 20f);
            var fTmp = footObj.AddComponent<TextMeshProUGUI>();
            fTmp.text = "Kéo thả vật phẩm để đổi vị trí • Phím [B] hoặc [I] để đóng/mở túi đồ.";
            fTmp.fontSize = 11.5f;
            fTmp.color = new Color(0.75f, 0.68f, 0.58f);
            fTmp.alignment = TextAlignmentOptions.Center;

            // 9. Drag Ghost Icon (Biểu tượng nổi khi kéo vật phẩm)
            var ghostObj = new GameObject("DragGhostIcon");
            ghostObj.transform.SetParent(canvas.transform, false);
            var gRt = ghostObj.AddComponent<RectTransform>();
            gRt.sizeDelta = new Vector2(46f, 46f);
            gRt.pivot = new Vector2(0.5f, 0.5f);
            dragGhostIcon = ghostObj.AddComponent<Image>();
            dragGhostIcon.preserveAspect = true;
            dragGhostIcon.raycastTarget = false;
            ghostObj.SetActive(false);
            dragGhostObj = ghostObj;
        }

        private void CreateSquareSlot(Transform parent, int slotIndex, Vector2 anchoredPos, float size, bool isHotbar = false, int hotkeyNumber = 0)
        {
            var slotObj = new GameObject($"Slot_{slotIndex}");
            slotObj.transform.SetParent(parent, false);
            var sRt = slotObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 1f);
            sRt.anchorMax = new Vector2(0.5f, 1f);
            sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = anchoredPos;
            sRt.sizeDelta = new Vector2(size, size);

            var sImg = slotObj.AddComponent<Image>();
            sImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            sImg.type = Image.Type.Sliced;
            // Màu nền từng ô vuông sẫm ấm, dịu mắt
            sImg.color = isHotbar ? new Color(0.36f, 0.28f, 0.20f) : new Color(0.26f, 0.20f, 0.15f);

            // Gắn bộ bắt sự kiện Kéo thả và Click
            var dragHandler = slotObj.AddComponent<BackpackSlotDragDrop>();
            dragHandler.SlotIndex = slotIndex;
            dragHandler.Popup = this;

            var slotBtn = slotObj.AddComponent<Button>();
            int capturedSlot = slotIndex;
            slotBtn.onClick.AddListener(() => SelectSlot(capturedSlot));

            // Icon
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            var iRt = iconObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.5f, 0.5f);
            iRt.anchorMax = new Vector2(0.5f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = new Vector2(0, 1f);
            iRt.sizeDelta = new Vector2(36f, 36f);
            var iImg = iconObj.AddComponent<Image>();
            iImg.preserveAspect = true;
            iImg.raycastTarget = false;

            // Stack Quantity (Minecraft style bottom-right number)
            var qtyObj = new GameObject("Quantity");
            qtyObj.transform.SetParent(slotObj.transform, false);
            var qRt = qtyObj.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(1f, 0f);
            qRt.anchorMax = new Vector2(1f, 0f);
            qRt.pivot = new Vector2(1f, 0f);
            qRt.anchoredPosition = new Vector2(-3f, 2f);
            qRt.sizeDelta = new Vector2(38f, 15f);

            var qTmp = qtyObj.AddComponent<TextMeshProUGUI>();
            qTmp.fontSize = 11f;
            qTmp.fontStyle = FontStyles.Bold;
            qTmp.color = Color.white;
            qTmp.alignment = TextAlignmentOptions.BottomRight;
            qTmp.raycastTarget = false;

            var qOutline = qtyObj.AddComponent<Outline>();
            qOutline.effectColor = new Color(0.12f, 0.08f, 0.02f, 0.95f);
            qOutline.effectDistance = new Vector2(1, -1);

            // Hotkey badge (if hotbar slot)
            if (isHotbar && hotkeyNumber > 0)
            {
                var hkObj = new GameObject("Hotkey");
                hkObj.transform.SetParent(slotObj.transform, false);
                var hkRt = hkObj.AddComponent<RectTransform>();
                hkRt.anchorMin = new Vector2(0f, 1f);
                hkRt.anchorMax = new Vector2(0f, 1f);
                hkRt.pivot = new Vector2(0f, 1f);
                hkRt.anchoredPosition = new Vector2(3f, -2f);
                hkRt.sizeDelta = new Vector2(14f, 14f);
                var hkTmp = hkObj.AddComponent<TextMeshProUGUI>();
                hkTmp.text = hotkeyNumber == 9 ? "B" : $"{hotkeyNumber}";
                hkTmp.fontSize = 9.5f;
                hkTmp.fontStyle = FontStyles.Bold;
                hkTmp.color = new Color(1f, 0.88f, 0.45f, 0.9f);
                hkTmp.raycastTarget = false;
            }

            slotObjects[slotIndex] = slotObj;
            slotBackgrounds[slotIndex] = sImg;
            slotIcons[slotIndex] = iImg;
            slotQuantities[slotIndex] = qTmp;
        }

        #region Drag and Drop Handler
        public void OnBeginDragSlot(int slotIndex, PointerEventData eventData)
        {
            if (slotIndex < 0 || slotIndex >= inventoryItems.Length) return;
            var item = inventoryItems[slotIndex];
            if (item == null || item.Icon == null) return;

            draggingSourceIndex = slotIndex;
            if (dragGhostObj != null && dragGhostIcon != null)
            {
                dragGhostIcon.sprite = item.Icon;
                dragGhostIcon.color = new Color(1f, 1f, 1f, 0.85f);
                dragGhostObj.transform.SetAsLastSibling();
                dragGhostObj.SetActive(true);
                UpdateGhostPosition(eventData.position);
            }

            // Làm mờ nhẹ icon ở ô nguồn
            if (slotIcons[slotIndex] != null)
            {
                slotIcons[slotIndex].color = new Color(1f, 1f, 1f, 0.35f);
            }
        }

        public void OnDragSlot(PointerEventData eventData)
        {
            if (draggingSourceIndex < 0) return;
            UpdateGhostPosition(eventData.position);
        }

        public void OnEndDragSlot(int sourceSlotIndex, PointerEventData eventData)
        {
            if (draggingSourceIndex < 0) return;

            // Khôi phục hiển thị ô nguồn
            if (slotIcons[sourceSlotIndex] != null)
            {
                slotIcons[sourceSlotIndex].color = Color.white;
            }

            if (dragGhostObj != null)
            {
                dragGhostObj.SetActive(false);
            }

            int targetSlotIndex = -1;

            // Dò tìm ô đích người chơi thả vào thông qua Raycast UI
            var raycastResults = new List<RaycastResult>();
            EventSystem.current?.RaycastAll(eventData, raycastResults);
            foreach (var result in raycastResults)
            {
                var slotHandler = result.gameObject.GetComponentInParent<BackpackSlotDragDrop>();
                if (slotHandler != null)
                {
                    targetSlotIndex = slotHandler.SlotIndex;
                    break;
                }
            }

            // Hoán đổi vị trí hai ô nếu hợp lệ
            if (targetSlotIndex >= 0 && targetSlotIndex != sourceSlotIndex)
            {
                SwapOrMoveSlots(sourceSlotIndex, targetSlotIndex);
            }

            draggingSourceIndex = -1;
        }

        private void UpdateGhostPosition(Vector2 screenPos)
        {
            if (dragGhostObj == null) return;

            var canvas = dragGhostObj.GetComponentInParent<Canvas>();
            if (canvas != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos, canvas.worldCamera, out Vector2 localPoint))
            {
                dragGhostObj.GetComponent<RectTransform>().anchoredPosition = localPoint;
            }
            else
            {
                dragGhostObj.transform.position = screenPos;
            }
        }

        public void SwapOrMoveSlots(int sourceIdx, int targetIdx)
        {
            if (sourceIdx < 0 || sourceIdx >= inventoryItems.Length || targetIdx < 0 || targetIdx >= inventoryItems.Length) return;

            // Hoán đổi 2 vị trí trong mảng
            var temp = inventoryItems[sourceIdx];
            inventoryItems[sourceIdx] = inventoryItems[targetIdx];
            inventoryItems[targetIdx] = temp;

            // Đồng bộ lại với HotbarController nếu có ô liên quan đến Hotbar
            if (sourceIdx >= 27 || targetIdx >= 27)
            {
                SyncHotbarFromSlots();
            }

            // Cập nhật giao diện của 2 ô
            UpdateSlotVisual(sourceIdx);
            UpdateSlotVisual(targetIdx);

            // Chọn ô đích vừa chuyển tới
            SelectSlot(targetIdx);
        }

        private void SyncHotbarFromSlots()
        {
            var hotbarCtrl = HotbarController.Instance;
            if (hotbarCtrl == null) return;

            // Các ô 27..34 tương ứng với 8 ô công cụ của Hotbar (0..7)
            for (int h = 0; h < 8; h++)
            {
                int slotIdx = 27 + h;
                var item = inventoryItems[slotIdx];
                if (item != null && item.BoundTool.HasValue)
                {
                    hotbarCtrl.ToolSlots[h] = HotbarController.CreateSlotData(item.BoundTool.Value);
                }
            }

            hotbarCtrl.InitializeSlots();
            hotbarCtrl.UpdateToolNameDisplay();
        }

        private void UpdateSlotVisual(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 36) return;

            var item = inventoryItems[slotIndex];
            var iconImg = slotIcons[slotIndex];
            var qtyTxt = slotQuantities[slotIndex];

            if (iconImg != null)
            {
                if (item != null && item.Icon != null)
                {
                    iconImg.sprite = item.Icon;
                    iconImg.color = Color.white;
                    iconImg.gameObject.SetActive(true);
                }
                else
                {
                    iconImg.gameObject.SetActive(false);
                }
            }

            if (qtyTxt != null)
            {
                if (item != null && !string.IsNullOrEmpty(item.QuantityDisplay))
                {
                    qtyTxt.text = item.QuantityDisplay;
                    qtyTxt.gameObject.SetActive(true);
                }
                else
                {
                    qtyTxt.gameObject.SetActive(false);
                }
            }
        }
        #endregion

        public void RefreshData()
        {
            var pInt = PlayerInteractionController.Instance;
            var engine = MobileGameController.Instance?.Engine;
            var farm = engine?.Farm;

            // Nếu chưa khởi tạo lần nào thì nạp danh mục mặc định
            if (!isDataInitialized)
            {
                Array.Clear(inventoryItems, 0, inventoryItems.Length);

                // --- 1. LƯỚI CHỨA ĐỒ CHÍNH (STORAGE SLOTS 0 - 26) ---
                inventoryItems[0] = new InventoryItem
                {
                    Id = "cam_heo",
                    Name = "Bao Cám Heo",
                    Category = "Thức ăn gia súc",
                    Icon = UISpriteLoader.GetToolIcon(StardewToolType.CamHat),
                    Description = "Cám dinh dưỡng cao thảo nguyên. Đổ vào máng ăn để đàn heo lớn nhanh và tăng cân đều đặn.",
                    BoundTool = StardewToolType.CamHat
                };

                inventoryItems[1] = new InventoryItem
                {
                    Id = "xo_nuoc",
                    Name = "Xô Nước Sạch",
                    Category = "Nước uống & Vệ sinh",
                    Icon = UISpriteLoader.GetToolIcon(StardewToolType.XoNuoc),
                    Description = "Nước ngọt thảo nguyên. Đổ vào máng nước để đàn heo giải khát và duy trì sức khỏe.",
                    BoundTool = StardewToolType.XoNuoc
                };

                inventoryItems[2] = new InventoryItem
                {
                    Id = "bua_go",
                    Name = "Búa Thợ Mộc (Tháo Lắp)",
                    Category = "Công cụ tháo dỡ & sửa chữa",
                    Quantity = 1,
                    QuantityDisplay = "",
                    Icon = UISpriteLoader.GetToolIcon(StardewToolType.BuaGo),
                    Description = "Búa thợ mộc chuyên dụng. Gõ vào công trình (Hàng rào, Máng ăn, Máng nước...) để tháo dỡ thu hồi vật phẩm, hoặc sửa chữa hàng rào bị hư hại.",
                    BoundTool = StardewToolType.BuaGo
                };

                inventoryItems[3] = new InventoryItem
                {
                    Id = "coc_go",
                    Name = "Cọc Rào Chuồng",
                    Category = "Vật liệu xây dựng",
                    Icon = UISpriteLoader.GetIconWoodPlank(),
                    Description = "Cọc gỗ 1m x 1m. Cầm cọc rào click chuột vào ô đất trống trong tầm để đặt rào mới, tự động nối liền 16 hướng với các rào xung quanh.",
                    BoundTool = StardewToolType.HangRao
                };

                inventoryItems[4] = new InventoryItem
                {
                    Id = "mang_an",
                    Name = "Máng Ăn Heo",
                    Category = "Hạ tầng chăn nuôi",
                    Icon = UISpriteLoader.GetIconFeeder(),
                    Description = "Máng chứa thức ăn bổ sung cho đàn heo. Cầm máng ăn click vào ô đất trống để đặt máng ăn mới trên nông trại.",
                    BoundTool = StardewToolType.MangAn
                };

                inventoryItems[5] = new InventoryItem
                {
                    Id = "mang_nuoc",
                    Name = "Máng Nước Sạch",
                    Category = "Hạ tầng chăn nuôi",
                    Icon = UISpriteLoader.GetIconWaterTrough(),
                    Description = "Bồn nước ngọt cho đàn heo giải khát. Cầm máng nước click vào ô đất trống để đặt máng nước mới trên nông trại.",
                    BoundTool = StardewToolType.MangNuoc
                };

                inventoryItems[6] = new InventoryItem
                {
                    Id = "ban_chai",
                    Name = "Bàn Chải Lông Thảo Nguyên",
                    Category = "Dụng cụ chăm sóc",
                    Quantity = 1,
                    QuantityDisplay = "",
                    Icon = UISpriteLoader.GetToolIcon(StardewToolType.BanChai),
                    Description = "Chải chuốt vệ sinh cho đàn heo, xoa dịu căng thẳng và tăng nhanh điểm Thân Thiết.",
                    BoundTool = StardewToolType.BanChai
                };

                inventoryItems[7] = new InventoryItem
                {
                    Id = "kinh_lup",
                    Name = "Kính Lúp Giám Định",
                    Category = "Dụng cụ chuyên gia",
                    Quantity = 1,
                    QuantityDisplay = "",
                    Icon = UISpriteLoader.GetToolIcon(StardewToolType.KinhLup),
                    Description = "Soi chiếu chỉ số gien di truyền, tâm trạng, phẩm chất thịt và phát hiện dị biến của từng cá thể heo.",
                    BoundTool = StardewToolType.KinhLup
                };

                inventoryItems[8] = new InventoryItem
                {
                    Id = "dao_gam",
                    Name = "Dao Găm Phòng Thân",
                    Category = "Vũ khí cận chiến",
                    Quantity = 1,
                    QuantityDisplay = "",
                    Icon = UISpriteLoader.GetToolIcon(StardewToolType.DaoGo),
                    Description = "Trang bị tự vệ phòng thân, sẵn sàng đối phó với thú săn mồi thảo nguyên trong các đợt quấy nhiễu.",
                    BoundTool = StardewToolType.DaoGo
                };

                inventoryItems[9] = new InventoryItem
                {
                    Id = "khu_trung",
                    Name = "Bình Khử Trùng Dược Thảo",
                    Category = "Dược phẩm trang trại",
                    Quantity = 5,
                    QuantityDisplay = "5",
                    Icon = UISpriteLoader.GetToolIcon(StardewToolType.KhuTrung),
                    Description = "Tiêu độc khử trùng bãi phân, chuồng nuôi và hố bùn, nâng cao chỉ số Sinh Thái SC.",
                    BoundTool = StardewToolType.KhuTrung
                };

                inventoryItems[10] = new InventoryItem
                {
                    Id = "bach_van",
                    Name = "Bạch Vân Ký (Phù Chú)",
                    Category = "Bảo vật phòng thủ",
                    Quantity = 1,
                    QuantityDisplay = "",
                    Icon = UISpriteLoader.GetToolIcon(StardewToolType.BachVan),
                    Description = "Phù chú cổ truyền kích hoạt hào quang sương trắng xua tan áp lực sự kiện và bảo vệ nông trại.",
                    BoundTool = StardewToolType.BachVan
                };

                inventoryItems[11] = new InventoryItem
                {
                    Id = "tien_vang",
                    Name = "Túi Tiền Vàng (Gold)",
                    Category = "Tiền tệ giao thương",
                    Icon = UISpriteLoader.GetIconGold(),
                    Description = "Ngân khố trang trại dùng để mua thêm giống heo, nâng cấp hạ tầng hoặc giao dịch tại Chợ Đêm.",
                    BoundTool = null
                };

                inventoryItems[12] = new InventoryItem
                {
                    Id = "thao_duoc",
                    Name = "Thảo Dược Bổ Lực",
                    Category = "Dược liệu tự nhiên",
                    Quantity = 3,
                    QuantityDisplay = "3",
                    Icon = UISpriteLoader.GetIconEnergyBolt(),
                    Description = "Thảo mộc hái ngoài đồng cỏ thảo nguyên, nhai vào hồi phục ngay 40 điểm Thể Lực.",
                    BoundTool = null,
                    OnUseAction = () =>
                    {
                        var charData = MobileGameController.Instance?.Engine?.Character;
                        if (charData != null)
                        {
                            charData.Stamina.CurrentStamina = Mathf.Min(charData.Stamina.MaxStamina, charData.Stamina.CurrentStamina + 40f);
                            StardewHUDController.Instance?.RefreshHUD();
                        }
                    }
                };

                inventoryItems[13] = new InventoryItem
                {
                    Id = "trai_tim",
                    Name = "Trái Tim Thân Thiết",
                    Category = "Chỉ số tình cảm",
                    Quantity = 5,
                    QuantityDisplay = "5",
                    Icon = UISpriteLoader.GetIconHeartFull(),
                    Description = "Mức độ thân thiết cao nhất giữa bạn và đàn heo, giúp tăng sản lượng và mở khóa đặc tính hiếm.",
                    BoundTool = null
                };

                // --- 2. THANH CÔNG CỤ NHANH (HOTBAR SLOTS 27 - 35) ---
                var hotbarCtrl = HotbarController.Instance;
                if (hotbarCtrl != null && hotbarCtrl.ToolSlots != null)
                {
                    for (int h = 0; h < 8 && h < hotbarCtrl.ToolSlots.Count; h++)
                    {
                        var toolType = hotbarCtrl.ToolSlots[h].ToolType;
                        InventoryItem matched = null;
                        for (int s = 0; s < 14; s++)
                        {
                            if (inventoryItems[s] != null && inventoryItems[s].BoundTool == toolType)
                            {
                                matched = inventoryItems[s];
                                break;
                            }
                        }
                        inventoryItems[27 + h] = matched ?? new InventoryItem
                        {
                            Id = toolType.ToString().ToLower(),
                            Name = hotbarCtrl.ToolSlots[h].ToolName,
                            Category = "Công cụ Hotbar",
                            Icon = UISpriteLoader.GetToolIcon(toolType),
                            BoundTool = toolType
                        };
                    }
                }
                else
                {
                    inventoryItems[27] = inventoryItems[0];
                    inventoryItems[28] = inventoryItems[1];
                    inventoryItems[29] = inventoryItems[2];
                    inventoryItems[30] = inventoryItems[3];
                    inventoryItems[31] = inventoryItems[4];
                    inventoryItems[32] = inventoryItems[5];
                    inventoryItems[33] = inventoryItems[6];
                    inventoryItems[34] = inventoryItems[7];
                }

                inventoryItems[35] = new InventoryItem
                {
                    Id = "balo",
                    Name = "Balo Túi Đồ Da",
                    Category = "Túi đựng cá nhân",
                    Quantity = 1,
                    QuantityDisplay = "B",
                    Icon = UISpriteLoader.GetIconBackpack(),
                    Description = "Túi đồ cá nhân chứa toàn bộ vật phẩm và công cụ của người chăn nuôi thảo nguyên.",
                    BoundTool = null
                };

                isDataInitialized = true;
            }

            // Cập nhật số lượng động (Dynamic Quantities) cho từng vật phẩm ở đúng vị trí ô hiện tại
            for (int i = 0; i < 36; i++)
            {
                var item = inventoryItems[i];
                if (item == null) continue;

                if (item.Id == "cam_heo")
                {
                    item.Quantity = pInt != null ? Mathf.RoundToInt(pInt.CurrentBagFeedKg) : 0;
                    item.QuantityDisplay = pInt != null && pInt.CurrentBagFeedKg > 0 ? $"{pInt.CurrentBagFeedKg:0}kg" : "";
                }
                else if (item.Id == "xo_nuoc")
                {
                    item.Quantity = pInt != null ? Mathf.RoundToInt(pInt.CurrentBucketWaterLiters) : 0;
                    item.QuantityDisplay = pInt != null && pInt.CurrentBucketWaterLiters > 0 ? $"{pInt.CurrentBucketWaterLiters:0}L" : "";
                }
                else if (item.Id == "coc_go")
                {
                    item.Quantity = pInt != null ? pInt.CarriedFences : 10;
                    item.QuantityDisplay = pInt != null && pInt.CarriedFences > 0 ? $"{pInt.CarriedFences}" : "0";
                }
                else if (item.Id == "mang_an")
                {
                    item.Quantity = pInt != null ? pInt.CarriedFeeders : 2;
                    item.QuantityDisplay = pInt != null && pInt.CarriedFeeders > 0 ? $"{pInt.CarriedFeeders}" : "0";
                }
                else if (item.Id == "mang_nuoc")
                {
                    item.Quantity = pInt != null ? pInt.CarriedWaterTroughs : 2;
                    item.QuantityDisplay = pInt != null && pInt.CarriedWaterTroughs > 0 ? $"{pInt.CarriedWaterTroughs}" : "0";
                }
                else if (item.Id == "tien_vang")
                {
                    item.Quantity = farm != null ? farm.Gold : 2500;
                    item.QuantityDisplay = farm != null ? $"{farm.Gold:N0}g" : "2,500g";
                }
            }

            // Cập nhật giao diện toàn bộ 36 ô vuông
            for (int i = 0; i < 36; i++)
            {
                UpdateSlotVisual(i);
            }
        }

        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 36) return;
            selectedSlotIndex = slotIndex;

            var normalFrame = UISpriteLoader.GetFrameSlotNormal();
            var selectedFrame = UISpriteLoader.GetFrameSlotSelected();

            for (int i = 0; i < 36; i++)
            {
                if (slotBackgrounds[i] != null)
                {
                    bool isSelected = (i == selectedSlotIndex);
                    slotBackgrounds[i].sprite = isSelected ? selectedFrame : normalFrame;
                    slotBackgrounds[i].transform.localScale = isSelected ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
                }
            }

            var item = inventoryItems[selectedSlotIndex];
            if (item != null && item.Icon != null)
            {
                if (DetailIcon != null)
                {
                    DetailIcon.sprite = item.Icon;
                    DetailIcon.gameObject.SetActive(true);
                }
                if (DetailTitleText != null)
                {
                    DetailTitleText.text = $"<b>{item.Name}</b> {(string.IsNullOrEmpty(item.QuantityDisplay) ? "" : $"[{item.QuantityDisplay}]")}";
                }
                if (DetailCategoryText != null)
                {
                    DetailCategoryText.text = $"Phân loại: <b>{item.Category}</b>";
                }
                if (DetailDescriptionText != null)
                {
                    DetailDescriptionText.text = item.Description;
                }

                if (DetailActionButton != null && DetailActionButtonText != null)
                {
                    if (item.BoundTool.HasValue)
                    {
                        DetailActionButton.gameObject.SetActive(true);
                        DetailActionButtonText.text = "<b>TRANG BỊ</b>";
                    }
                    else if (item.OnUseAction != null)
                    {
                        DetailActionButton.gameObject.SetActive(true);
                        DetailActionButtonText.text = "<b>SỬ DỤNG</b>";
                    }
                    else
                    {
                        DetailActionButton.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (DetailIcon != null) DetailIcon.gameObject.SetActive(false);
                if (DetailTitleText != null) DetailTitleText.text = "<i>[ Ô Trống ]</i>";
                if (DetailCategoryText != null) DetailCategoryText.text = "Chưa có vật phẩm";
                if (DetailDescriptionText != null) DetailDescriptionText.text = "Kéo thả vật phẩm vào ô này để cất giữ hoặc sắp xếp túi đồ.";
                if (DetailActionButton != null) DetailActionButton.gameObject.SetActive(false);
            }
        }

        private void OnDetailActionButtonClicked()
        {
            var item = inventoryItems[selectedSlotIndex];
            if (item == null) return;

            if (item.BoundTool.HasValue && HotbarController.Instance != null)
            {
                HotbarController.Instance.EquipTool(item.BoundTool.Value);
                Hide();
            }
            else if (item.OnUseAction != null)
            {
                item.OnUseAction.Invoke();
                RefreshData();
                SelectSlot(selectedSlotIndex);
            }
        }
    }
}
