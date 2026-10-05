using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        private readonly InventoryItem[] inventoryItems = new InventoryItem[36];
        private int selectedSlotIndex = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
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
            if (Input.GetKeyDown(KeyCode.Escape) && ContentPanel != null && ContentPanel.activeSelf)
            {
                Hide();
            }
        }

        public void Toggle()
        {
            EnsureUIExists();
            if (ContentPanel != null && ContentPanel.activeSelf)
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
            if (ContentPanel != null) ContentPanel.SetActive(true);
            RefreshData();
            SelectSlot(selectedSlotIndex);
        }

        public void Hide()
        {
            if (ContentPanel != null) ContentPanel.SetActive(false);
        }

        public void EnsureUIExists()
        {
            if (ContentPanel != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existingPopup = canvas.transform.Find("Pig_Backpack_Popup");
            if (existingPopup != null)
            {
                ContentPanel = existingPopup.gameObject;
                return;
            }

            // 1. Popup Root Window (640 x 570 px)
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
            pImg.color = Color.white;
            ContentPanel = popupObj;

            // 2. Parchment Inner Layer
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
            parchImg.color = Color.white;

            // 3. Header Title
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(parchObj.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -10);
            tRt.sizeDelta = new Vector2(0, 26);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>TÚI ĐỒ TRANG TRẠI (INVENTORY)</b>";
            titleTmp.fontSize = 18f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.28f, 0.12f, 0.03f);
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 4. Close Button [X]
            var closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(popupObj.transform, false);
            var cbRt = closeObj.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1, 1);
            cbRt.anchorMax = new Vector2(1, 1);
            cbRt.pivot = new Vector2(0.5f, 0.5f);
            cbRt.anchoredPosition = new Vector2(6, 6);
            cbRt.sizeDelta = new Vector2(36, 36);
            var cbImg = closeObj.AddComponent<Image>();
            cbImg.sprite = UISpriteLoader.GetIconCloseCross();
            CloseButton = closeObj.AddComponent<Button>();
            CloseButton.onClick.AddListener(Hide);

            // 5. Main Storage Grid (3 rows x 9 columns = 27 square slots like Minecraft)
            var storageLabelObj = new GameObject("StorageLabel");
            storageLabelObj.transform.SetParent(parchObj.transform, false);
            var slRt = storageLabelObj.AddComponent<RectTransform>();
            slRt.anchorMin = new Vector2(0, 1);
            slRt.anchorMax = new Vector2(1, 1);
            slRt.pivot = new Vector2(0.5f, 1);
            slRt.anchoredPosition = new Vector2(0, -38);
            slRt.sizeDelta = new Vector2(534, 18);
            var slTmp = storageLabelObj.AddComponent<TextMeshProUGUI>();
            slTmp.text = "<b>Kho Đồ Balo (27 Ô Vuông)</b>";
            slTmp.fontSize = 12f;
            slTmp.color = new Color(0.4f, 0.22f, 0.08f);
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
            hlTmp.text = "<b>Thanh Công Cụ Nhanh (Hotbar)</b>";
            hlTmp.fontSize = 12f;
            hlTmp.color = new Color(0.4f, 0.22f, 0.08f);
            hlTmp.alignment = TextAlignmentOptions.Left;

            float hotbarPosY = -272f;
            for (int c = 0; c < 9; c++)
            {
                int slotIdx = 27 + c;
                float posX = startX + (c * (slotSize + spacing));
                CreateSquareSlot(parchObj.transform, slotIdx, new Vector2(posX, hotbarPosY), slotSize, isHotbar: true, hotkeyNumber: c + 1);
            }

            // 7. Item Detail / Tooltip Box (Minecraft GUI bottom panel)
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
            dImg.color = new Color(0.9f, 0.85f, 0.75f, 0.95f);

            // Detail Icon Frame
            var dIconFrame = new GameObject("IconFrame");
            dIconFrame.transform.SetParent(detailBoxObj.transform, false);
            var difRt = dIconFrame.AddComponent<RectTransform>();
            difRt.anchorMin = new Vector2(0, 0.5f);
            difRt.anchorMax = new Vector2(0, 0.5f);
            difRt.pivot = new Vector2(0.5f, 0.5f);
            difRt.anchoredPosition = new Vector2(40f, 0f);
            difRt.sizeDelta = new Vector2(52f, 52f);
            var difImg = dIconFrame.AddComponent<Image>();
            difImg.sprite = UISpriteLoader.GetFrameSlotNormal();
            difImg.type = Image.Type.Sliced;

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
            DetailTitleText.color = new Color(0.25f, 0.12f, 0.04f);

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
            DetailCategoryText.color = new Color(0.45f, 0.25f, 0.10f);

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
            DetailDescriptionText.color = new Color(0.28f, 0.14f, 0.05f);

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
            fTmp.text = "Click ô vuông để xem chi tiết • Phím [B] hoặc [I] để đóng/mở túi đồ.";
            fTmp.fontSize = 11.5f;
            fTmp.color = new Color(0.45f, 0.28f, 0.12f);
            fTmp.alignment = TextAlignmentOptions.Center;
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
            sImg.color = isHotbar ? new Color(1f, 0.96f, 0.88f) : Color.white;

            var btn = slotObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectSlot(slotIndex));

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
                hkTmp.color = new Color(0.35f, 0.20f, 0.08f, 0.8f);
                hkTmp.raycastTarget = false;
            }

            slotObjects[slotIndex] = slotObj;
            slotBackgrounds[slotIndex] = sImg;
            slotIcons[slotIndex] = iImg;
            slotQuantities[slotIndex] = qTmp;
        }

        public void RefreshData()
        {
            Array.Clear(inventoryItems, 0, inventoryItems.Length);

            var pInt = PlayerInteractionController.Instance;
            var engine = MobileGameController.Instance?.Engine;
            var farm = engine?.Farm;

            // --- 1. LƯỚI CHỨA ĐỒ CHÍNH (STORAGE SLOTS 0 - 26) ---
            // Slot 0: Bao Cám
            inventoryItems[0] = new InventoryItem
            {
                Id = "cam_heo",
                Name = "Bao Cám Heo",
                Category = "Thức ăn gia súc",
                Quantity = pInt != null ? Mathf.RoundToInt(pInt.CurrentBagFeedKg) : 0,
                QuantityDisplay = pInt != null && pInt.CurrentBagFeedKg > 0 ? $"{pInt.CurrentBagFeedKg:0}kg" : "",
                Icon = UISpriteLoader.GetToolIcon(StardewToolType.CamHat),
                Description = "Cám dinh dưỡng cao thảo nguyên. Đổ vào máng ăn để đàn heo lớn nhanh và tăng cân đều đặn.",
                BoundTool = StardewToolType.CamHat
            };

            // Slot 1: Xô Nước
            inventoryItems[1] = new InventoryItem
            {
                Id = "xo_nuoc",
                Name = "Xô Nước Sạch",
                Category = "Nước uống & Vệ sinh",
                Quantity = pInt != null ? Mathf.RoundToInt(pInt.CurrentBucketWaterLiters) : 0,
                QuantityDisplay = pInt != null && pInt.CurrentBucketWaterLiters > 0 ? $"{pInt.CurrentBucketWaterLiters:0}L" : "",
                Icon = UISpriteLoader.GetToolIcon(StardewToolType.XoNuoc),
                Description = "Nước ngọt thảo nguyên. Đổ vào máng nước để đàn heo giải khát và duy trì sức khỏe.",
                BoundTool = StardewToolType.XoNuoc
            };

            // Slot 2: Búa Gỗ Thợ Mộc (Chỉ dùng tháo dỡ & sửa chữa)
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

            // Slot 3: Cọc Rào Chuồng (Item Hàng Rào)
            inventoryItems[3] = new InventoryItem
            {
                Id = "coc_go",
                Name = "Cọc Rào Chuồng",
                Category = "Vật liệu xây dựng",
                Quantity = pInt != null ? pInt.CarriedFences : 10,
                QuantityDisplay = pInt != null && pInt.CarriedFences > 0 ? $"{pInt.CarriedFences}" : "0",
                Icon = UISpriteLoader.GetIconWoodPlank(),
                Description = "Cọc gỗ 1m x 1m. Cầm cọc rào click chuột vào ô đất trống trong tầm để đặt rào mới, tự động nối liền 16 hướng với các rào xung quanh.",
                BoundTool = StardewToolType.HangRao
            };

            // Slot 4: Máng Ăn Heo (Item Máng Ăn)
            inventoryItems[4] = new InventoryItem
            {
                Id = "mang_an",
                Name = "Máng Ăn Heo",
                Category = "Hạ tầng chăn nuôi",
                Quantity = pInt != null ? pInt.CarriedFeeders : 2,
                QuantityDisplay = pInt != null && pInt.CarriedFeeders > 0 ? $"{pInt.CarriedFeeders}" : "0",
                Icon = UISpriteLoader.GetIconFeeder(),
                Description = "Máng chứa thức ăn bổ sung cho đàn heo. Cầm máng ăn click vào ô đất trống để đặt máng ăn mới trên nông trại.",
                BoundTool = StardewToolType.MangAn
            };

            // Slot 5: Máng Nước Sạch (Item Máng Nước)
            inventoryItems[5] = new InventoryItem
            {
                Id = "mang_nuoc",
                Name = "Máng Nước Sạch",
                Category = "Hạ tầng chăn nuôi",
                Quantity = pInt != null ? pInt.CarriedWaterTroughs : 2,
                QuantityDisplay = pInt != null && pInt.CarriedWaterTroughs > 0 ? $"{pInt.CarriedWaterTroughs}" : "0",
                Icon = UISpriteLoader.GetIconWaterTrough(),
                Description = "Bồn nước ngọt cho đàn heo giải khát. Cầm máng nước click vào ô đất trống để đặt máng nước mới trên nông trại.",
                BoundTool = StardewToolType.MangNuoc
            };

            // Slot 6: Bàn Chải
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

            // Slot 7: Kính Lúp
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

            // Slot 8: Dao Găm
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

            // Slot 9: Thuốc Khử Trùng
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

            // Slot 10: Bạch Vân Ký
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

            // Slot 11: Túi Tiền Vàng
            inventoryItems[11] = new InventoryItem
            {
                Id = "tien_vang",
                Name = "Túi Tiền Vàng (Gold)",
                Category = "Tiền tệ giao thương",
                Quantity = farm != null ? farm.Gold : 2500,
                QuantityDisplay = farm != null ? $"{farm.Gold:N0}g" : "2,500g",
                Icon = UISpriteLoader.GetIconGold(),
                Description = "Ngân khố trang trại dùng để mua thêm giống heo, nâng cấp hạ tầng hoặc giao dịch tại Chợ Đêm.",
                BoundTool = null
            };

            // Slot 12: Thảo Dược Hồi Lực
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

            // Slot 13: Trái Tim Gắn Kết
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
                inventoryItems[27] = inventoryItems[0]; // Cám
                inventoryItems[28] = inventoryItems[1]; // Nước
                inventoryItems[29] = inventoryItems[2]; // Búa
                inventoryItems[30] = inventoryItems[3]; // Rào
                inventoryItems[31] = inventoryItems[4]; // Máng ăn
                inventoryItems[32] = inventoryItems[5]; // Máng nước
                inventoryItems[33] = inventoryItems[6]; // Bàn chải
                inventoryItems[34] = inventoryItems[7]; // Kính lúp
            }

            inventoryItems[35] = new InventoryItem // Balo
            {
                Id = "balo",
                Name = "Balo Túi Đồ Da",
                Category = "Túi đựng cá nhân",
                Quantity = 1,
                QuantityDisplay = "B",
                Icon = UISpriteLoader.GetIconBackpack(),
                Description = "Túi đồ cá nhân phong cách Minecraft, chứa toàn bộ vật phẩm và công cụ của người chăn nuôi.",
                BoundTool = null
            };

            // Cập nhật giao diện toàn bộ 36 ô vuông
            for (int i = 0; i < 36; i++)
            {
                var item = inventoryItems[i];
                if (slotIcons[i] != null)
                {
                    if (item != null && item.Icon != null)
                    {
                        slotIcons[i].sprite = item.Icon;
                        slotIcons[i].color = Color.white;
                        slotIcons[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        slotIcons[i].gameObject.SetActive(false);
                    }
                }

                if (slotQuantities[i] != null)
                {
                    if (item != null && !string.IsNullOrEmpty(item.QuantityDisplay))
                    {
                        slotQuantities[i].text = item.QuantityDisplay;
                        slotQuantities[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        slotQuantities[i].gameObject.SetActive(false);
                    }
                }
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
                if (DetailDescriptionText != null) DetailDescriptionText.text = "Ô chứa đồ trống, sẵn sàng tiếp nhận tài nguyên hoặc nông sản mới.";
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
