using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class DevModePopup : MonoBehaviour
    {
        public static DevModePopup Instance { get; private set; }

        [Header("UI Elements")]
        public GameObject DevButtonObj;
        public GameObject ContentPanel;
        public TextMeshProUGUI StatusText;
        public Button CloseButton;

        private GameObject[] tabPanels = new GameObject[5];
        private Image[] tabButtons = new Image[5];
        private int currentTabIndex = 0;

        // Dev states
        private bool isInfiniteStamina = false;
        private bool isFastMove = false;

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
            // Phím tắt ` (dấu ngã / tilde), F1 hoặc F12 để bật/tắt nhanh Dev Mode
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.F12))
            {
                Toggle();
            }

            // Phím Esc để đóng nếu đang mở
            if (Input.GetKeyDown(KeyCode.Escape) && ContentPanel != null && ContentPanel.activeSelf)
            {
                Hide();
            }

            // Duy trì Bất tử Thể Lực nếu đang bật
            if (isInfiniteStamina)
            {
                var charData = MobileGameController.Instance?.Engine?.Character;
                if (charData != null)
                {
                    charData.Stamina.CurrentStamina = charData.Stamina.MaxStamina;
                }
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
            SwitchTab(currentTabIndex);
            ShowStatus("[DEV MODE] Sẵn sàng thực hiện lệnh dev.");
        }

        public void Hide()
        {
            if (ContentPanel != null) ContentPanel.SetActive(false);
        }

        public void ShowStatus(string message)
        {
            if (StatusText != null)
            {
                StatusText.text = $"<b>[DEV]</b> {message}";
            }
            Debug.Log($"<color=yellow>[DevMode]</color> {message}");
        }

        public void SwitchTab(int tabIndex)
        {
            currentTabIndex = Mathf.Clamp(tabIndex, 0, tabPanels.Length - 1);

            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] != null)
                {
                    tabPanels[i].SetActive(i == currentTabIndex);
                }
                if (tabButtons[i] != null)
                {
                    bool isSelected = (i == currentTabIndex);
                    tabButtons[i].sprite = isSelected ? UISpriteLoader.GetFrameSlotSelected() : UISpriteLoader.GetFrameSlotNormal();
                    tabButtons[i].color = isSelected ? new Color(1f, 0.95f, 0.8f) : new Color(0.9f, 0.9f, 0.9f);
                }
            }
        }

        public void EnsureUIExists()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Tạo Nút Mở Dev Mode ở góc trên bên trái màn hình
            if (DevButtonObj == null)
            {
                var existingBtn = canvas.transform.Find("DevMode_Button");
                if (existingBtn != null)
                {
                    DevButtonObj = existingBtn.gameObject;
                }
                else
                {
                    var devBtn = new GameObject("DevMode_Button");
                    devBtn.transform.SetParent(canvas.transform, false);
                    var bRt = devBtn.AddComponent<RectTransform>();
                    bRt.anchorMin = new Vector2(0f, 1f);
                    bRt.anchorMax = new Vector2(0f, 1f);
                    bRt.pivot = new Vector2(0f, 1f);
                    bRt.anchoredPosition = new Vector2(25f, -25f);
                    bRt.sizeDelta = new Vector2(108f, 38f);

                    var bImg = devBtn.AddComponent<Image>();
                    bImg.sprite = UISpriteLoader.GetFrameWoodPanel();
                    bImg.type = Image.Type.Sliced;
                    bImg.color = Color.white;

                    var btnComp = devBtn.AddComponent<Button>();
                    btnComp.onClick.AddListener(Toggle);

                    var bTextObj = new GameObject("Label");
                    bTextObj.transform.SetParent(devBtn.transform, false);
                    var btRt = bTextObj.AddComponent<RectTransform>();
                    btRt.anchorMin = Vector2.zero;
                    btRt.anchorMax = Vector2.one;
                    btRt.offsetMin = Vector2.zero;
                    btRt.offsetMax = Vector2.zero;

                    var tmp = bTextObj.AddComponent<TextMeshProUGUI>();
                    tmp.text = "<b>[ DEV ⚙ ]</b>";
                    tmp.fontSize = 14f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(1f, 0.88f, 0.45f);
                    tmp.alignment = TextAlignmentOptions.Center;

                    DevButtonObj = devBtn;
                }
            }

            // 2. Tạo Hộp Thoại Dev Mode Popup ở trung tâm
            if (ContentPanel != null) return;

            var existingPopup = canvas.transform.Find("Dev_Mode_Popup");
            if (existingPopup != null)
            {
                ContentPanel = existingPopup.gameObject;
                return;
            }

            var popupObj = new GameObject("Dev_Mode_Popup");
            popupObj.transform.SetParent(canvas.transform, false);
            var pRt = popupObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.anchoredPosition = Vector2.zero;
            pRt.sizeDelta = new Vector2(680f, 500f);

            var pImg = popupObj.AddComponent<Image>();
            pImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            pImg.type = Image.Type.Sliced;
            pImg.color = Color.white;
            ContentPanel = popupObj;

            // Parchment
            var parchObj = new GameObject("Parchment");
            parchObj.transform.SetParent(popupObj.transform, false);
            var parchRt = parchObj.AddComponent<RectTransform>();
            parchRt.anchorMin = Vector2.zero;
            parchRt.anchorMax = Vector2.one;
            parchRt.offsetMin = new Vector2(14f, 14f);
            parchRt.offsetMax = new Vector2(-14f, -14f);
            var parchImg = parchObj.AddComponent<Image>();
            parchImg.sprite = UISpriteLoader.GetFrameParchment();
            parchImg.type = Image.Type.Sliced;
            parchImg.color = Color.white;

            // Title
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(parchObj.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -10);
            tRt.sizeDelta = new Vector2(0, 30);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>BẢNG ĐIỀU KHIỂN NHÀ PHÁT TRIỂN (DEV MODE)</b>";
            titleTmp.fontSize = 18f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.28f, 0.12f, 0.03f);
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Close button
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

            // Tab Buttons Bar (5 tabs)
            var tabContainer = new GameObject("Tab_Bar");
            tabContainer.transform.SetParent(parchObj.transform, false);
            var tabRt = tabContainer.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0, 1);
            tabRt.anchorMax = new Vector2(1, 1);
            tabRt.pivot = new Vector2(0.5f, 1);
            tabRt.anchoredPosition = new Vector2(0, -42);
            tabRt.sizeDelta = new Vector2(-30, 34);

            string[] tabNames = { "Tài Nguyên", "Thời Gian", "Đàn Heo", "Hạ Tầng", "Nhân Vật" };
            float tabWidth = 120f;
            float startTabX = -((tabNames.Length * tabWidth) / 2f) + (tabWidth / 2f);

            for (int i = 0; i < tabNames.Length; i++)
            {
                int tabIdx = i;
                var tabBtn = new GameObject($"Tab_{tabNames[i]}");
                tabBtn.transform.SetParent(tabContainer.transform, false);
                var tbRt = tabBtn.AddComponent<RectTransform>();
                tbRt.anchorMin = new Vector2(0.5f, 0.5f);
                tbRt.anchorMax = new Vector2(0.5f, 0.5f);
                tbRt.pivot = new Vector2(0.5f, 0.5f);
                tbRt.anchoredPosition = new Vector2(startTabX + (i * tabWidth), 0);
                tbRt.sizeDelta = new Vector2(tabWidth - 6f, 32f);

                var img = tabBtn.AddComponent<Image>();
                img.sprite = UISpriteLoader.GetFrameSlotNormal();
                img.type = Image.Type.Sliced;
                tabButtons[i] = img;

                var btn = tabBtn.AddComponent<Button>();
                btn.onClick.AddListener(() => SwitchTab(tabIdx));

                var lblObj = new GameObject("Text");
                lblObj.transform.SetParent(tabBtn.transform, false);
                var lRt = lblObj.AddComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.offsetMin = Vector2.zero;
                lRt.offsetMax = Vector2.zero;
                var lTmp = lblObj.AddComponent<TextMeshProUGUI>();
                lTmp.text = tabNames[i];
                lTmp.fontSize = 13.5f;
                lTmp.fontStyle = FontStyles.Bold;
                lTmp.color = new Color(0.25f, 0.12f, 0.04f);
                lTmp.alignment = TextAlignmentOptions.Center;
            }

            // Tab Panels
            for (int i = 0; i < 5; i++)
            {
                var panel = new GameObject($"TabPanel_{i}");
                panel.transform.SetParent(parchObj.transform, false);
                var pnlRt = panel.AddComponent<RectTransform>();
                pnlRt.anchorMin = new Vector2(0, 0);
                pnlRt.anchorMax = new Vector2(1, 1);
                pnlRt.offsetMin = new Vector2(15, 60);
                pnlRt.offsetMax = new Vector2(-15, -82);
                tabPanels[i] = panel;
            }

            // Build individual tab content
            BuildResourcesTab(tabPanels[0].transform);
            BuildTimeTab(tabPanels[1].transform);
            BuildHerdTab(tabPanels[2].transform);
            BuildFarmTab(tabPanels[3].transform);
            BuildPlayerTab(tabPanels[4].transform);

            // Footer Status
            var statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(parchObj.transform, false);
            var stRt = statusObj.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0, 0);
            stRt.anchorMax = new Vector2(1, 0);
            stRt.pivot = new Vector2(0.5f, 0);
            stRt.anchoredPosition = new Vector2(0, 10);
            stRt.sizeDelta = new Vector2(-40, 42);
            StatusText = statusObj.AddComponent<TextMeshProUGUI>();
            StatusText.fontSize = 13f;
            StatusText.color = new Color(0.2f, 0.35f, 0.15f);
            StatusText.alignment = TextAlignmentOptions.Center;
            StatusText.text = "<b>[DEV]</b> Sẵn sàng thực hiện lệnh dev (Phím tắt: [~] hoặc [F1]).";
        }

        #region Tab Content Builders
        private void BuildResourcesTab(Transform parent)
        {
            float w = 195f, h = 42f;
            // Row 0
            CreateDevActionButton(parent, "+1,000 Vàng", () => AddGold(1000), new Vector2(15, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "+10,000 Vàng", () => AddGold(10000), new Vector2(220, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "+50,000 Vàng", () => AddGold(50000), new Vector2(425, -15), new Vector2(w, h));

            // Row 1
            CreateDevActionButton(parent, "+20 Cọc Gỗ", () => AddWood(20), new Vector2(15, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Hồi Đầy Thể Lực", RefillStamina, new Vector2(220, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Bất Tử Thể Lực (Toggle)", ToggleInfiniteStamina, new Vector2(425, -68), new Vector2(w, h));

            // Row 2
            CreateDevActionButton(parent, "Đầy Bao Cám (20kg)", RefillFeedBag, new Vector2(15, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Đầy Xô Nước (50L)", RefillWaterBucket, new Vector2(220, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Reset Kho Về Gốc", ResetResourcesToDefault, new Vector2(425, -121), new Vector2(w, h));
        }

        private void BuildTimeTab(Transform parent)
        {
            float w = 195f, h = 42f;
            // Row 0: Tốc độ
            CreateDevActionButton(parent, "Tốc Độ 1x (Chuẩn)", () => SetTimeScale(1.0f), new Vector2(15, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "Tốc Độ 2x (Nhanh)", () => SetTimeScale(2.0f), new Vector2(220, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "Tốc Độ 5x (Siêu Tốc)", () => SetTimeScale(5.0f), new Vector2(425, -15), new Vector2(w, h));

            // Row 1: Tua nhanh
            CreateDevActionButton(parent, "Tua +1 Giờ", () => AdvanceHours(1), new Vector2(15, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Tua +6 Giờ", () => AdvanceHours(6), new Vector2(220, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Tua Sang Ngày Mới", AdvanceToNextDay, new Vector2(425, -68), new Vector2(w, h));

            // Row 2: Khung giờ
            CreateDevActionButton(parent, "Đến 06:00 (Sáng)", () => JumpToHour(6), new Vector2(15, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Đến 12:00 (Trưa)", () => JumpToHour(12), new Vector2(220, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Đến 20:00 (Đêm/Chợ)", () => JumpToHour(20), new Vector2(425, -121), new Vector2(w, h));
        }

        private void BuildHerdTab(Transform parent)
        {
            float w = 195f, h = 42f;
            // Row 0: Spawn Heo
            CreateDevActionButton(parent, "+ Heo Hồng Điền", () => SpawnPig(GeneLineId.HongDien, GeneRarity.Thuong, "Hồng Điền"), new Vector2(15, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "+ Heo Lam Khê", () => SpawnPig(GeneLineId.LamKhe, GeneRarity.Kha, "Lam Khê"), new Vector2(220, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "+ Heo Kim Thọ", () => SpawnPig(GeneLineId.KimTho, GeneRarity.HuyenThoai, "Kim Thọ"), new Vector2(425, -15), new Vector2(w, h));

            // Row 1: Spawn & Nuôi
            CreateDevActionButton(parent, "+ Heo Hư Thể", () => SpawnPig(GeneLineId.HuThe, GeneRarity.Hiem, "Hư Thể"), new Vector2(15, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Cho Cả Đàn Ăn No", FeedAllPigs, new Vector2(220, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Chữa Khỏe Cả Đàn", HealAndCalmAllPigs, new Vector2(425, -68), new Vector2(w, h));

            // Row 2: Trưởng thành & Tình cảm
            CreateDevActionButton(parent, "+1 Giai Đoạn Tuổi", PromotePigStage, new Vector2(15, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Tối Đa Thân Thiết (5 Tim)", MaxBondingAll, new Vector2(220, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Kích Hoạt Di Biến", AwakenDeviantTraits, new Vector2(425, -121), new Vector2(w, h));
        }

        private void BuildFarmTab(Transform parent)
        {
            float w = 195f, h = 42f;
            // Row 0: Máng
            CreateDevActionButton(parent, "Đổ Đầy Máng Ăn", RefillAllFeeders, new Vector2(15, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "Đổ Đầy Máng Nước", RefillAllWaterTroughs, new Vector2(220, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "Tăng Sinh Thái (+15 SC)", () => AdjustHabitat(15), new Vector2(425, -15), new Vector2(w, h));

            // Row 1: Rào chuồng
            CreateDevActionButton(parent, "Khôi Phục Rào Chuẩn", ResetDefaultFences, new Vector2(15, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Xóa Toàn Bộ Rào", ClearAllFences, new Vector2(220, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Giảm Áp Lực (-25 ÁLSK)", () => AdjustPressure(-25), new Vector2(425, -68), new Vector2(w, h));

            // Row 2: Khác
            CreateDevActionButton(parent, "Kích Hoạt Bạch Vân", TriggerBachVan, new Vector2(15, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Đặt Rào Tại Vị Trí Đứng", PlaceFenceAtPlayerPos, new Vector2(220, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Tăng Sức Chứa Trại (+10)", ExpandCapacity, new Vector2(425, -121), new Vector2(w, h));
        }

        private void BuildPlayerTab(Transform parent)
        {
            float w = 195f, h = 42f;
            // Row 0: Di chuyển
            CreateDevActionButton(parent, "Chạy 7 m/s [Bật/Tắt]", ToggleFastMovement, new Vector2(15, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "Về Tâm Chuồng (0, 0)", TeleportToCenter, new Vector2(220, -15), new Vector2(w, h));
            CreateDevActionButton(parent, "Ra Cổng Nam (0, -14)", TeleportToGate, new Vector2(425, -15), new Vector2(w, h));

            // Row 1: Camera Zoom
            CreateDevActionButton(parent, "Cận 40x22 m", () => SetCameraZoom(CameraFrames.OrthoSizeForHeight(CameraFrames.CloseHeightMeters)), new Vector2(15, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Mặc định 64x36 m", () => SetCameraZoom(CameraFrames.OrthoSizeForHeight(CameraFrames.DefaultHeightMeters)), new Vector2(220, -68), new Vector2(w, h));
            CreateDevActionButton(parent, "Toàn cảnh 128x72 m", () => SetCameraZoom(CameraFrames.OrthoSizeForHeight(CameraFrames.OverviewHeightMeters)), new Vector2(425, -68), new Vector2(w, h));

            // Row 2: Võ kỹ chiến đấu
            CreateDevActionButton(parent, "Thi Triển Thế Công", () => ExecuteSkill(CombatSlotType.TheCong), new Vector2(15, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Thi Triển Thế Thủ", () => ExecuteSkill(CombatSlotType.TheThu), new Vector2(220, -121), new Vector2(w, h));
            CreateDevActionButton(parent, "Thi Triển Thế Biến", () => ExecuteSkill(CombatSlotType.TheBien), new Vector2(425, -121), new Vector2(w, h));
        }

        private GameObject CreateDevActionButton(Transform parent, string label, Action onClick, Vector2 pos, Vector2 size)
        {
            var btnObj = new GameObject($"Btn_{label.Replace(" ", "_")}");
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = btnObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameSlotNormal();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(4, 2);
            textRt.offsetMax = new Vector2(-4, -2);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 12.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.25f, 0.12f, 0.04f);
            tmp.alignment = TextAlignmentOptions.Center;

            return btnObj;
        }
        #endregion

        #region Dev Action Handlers
        // --- TÀI NGUYÊN ---
        public void AddGold(int amount)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                engine.Farm.Gold += amount;
                MobileGameController.Instance.OnStateUpdated?.Invoke();
                StardewHUDController.Instance?.RefreshHUD();
                ShowStatus($"Đã cộng +{amount:N0} Vàng vào ngân khố! (Hiện có: {engine.Farm.Gold:N0}g)");
            }
        }

        public void AddWood(int amount)
        {
            var pInt = PlayerInteractionController.Instance;
            if (pInt != null)
            {
                pInt.CarriedFences += amount;
                pInt.CarriedFeeders += 2;
                pInt.CarriedWaterTroughs += 2;
                HotbarController.Instance?.UpdateToolNameDisplay();
                ShowStatus($"Đã cộng +{amount} Rào, +2 Máng Ăn, +2 Máng Nước! (Hiện có: {pInt.CarriedFences} Rào, {pInt.CarriedFeeders} Máng ăn, {pInt.CarriedWaterTroughs} Máng nước)");
            }
        }

        public void RefillStamina()
        {
            var charData = MobileGameController.Instance?.Engine?.Character;
            if (charData != null)
            {
                charData.Stamina.CurrentStamina = charData.Stamina.MaxStamina;
                StardewHUDController.Instance?.RefreshHUD();
                ShowStatus("Đã hồi đầy 100% Thể Lực (100/100 STA)!");
            }
        }

        public void ToggleInfiniteStamina()
        {
            isInfiniteStamina = !isInfiniteStamina;
            if (isInfiniteStamina)
            {
                RefillStamina();
                ShowStatus("Bất tử Thể Lực: BẬT (STA luôn khóa ở 100%)");
            }
            else
            {
                ShowStatus("Bất tử Thể Lực: TẮT (Tiêu hao thể lực bình thường)");
            }
        }

        public void RefillFeedBag()
        {
            var pInt = PlayerInteractionController.Instance;
            if (pInt != null)
            {
                pInt.CurrentBagFeedKg = pInt.MaxBagFeedKg;
                HotbarController.Instance?.UpdateToolNameDisplay();
                ShowStatus($"Đã đổ đầy Bao Cám ({pInt.MaxBagFeedKg:0}kg)!");
            }
        }

        public void RefillWaterBucket()
        {
            var pInt = PlayerInteractionController.Instance;
            if (pInt != null)
            {
                pInt.CurrentBucketWaterLiters = pInt.MaxBucketWaterLiters;
                HotbarController.Instance?.UpdateToolNameDisplay();
                ShowStatus($"Đã múc đầy Xô Nước ({pInt.MaxBucketWaterLiters:0}L)!");
            }
        }

        public void ResetResourcesToDefault()
        {
            var engine = MobileGameController.Instance?.Engine;
            var pInt = PlayerInteractionController.Instance;
            if (engine != null) engine.Farm.Gold = 2500;
            if (pInt != null)
            {
                pInt.CarriedFences = 10;
                pInt.CarriedFeeders = 2;
                pInt.CarriedWaterTroughs = 2;
                pInt.CurrentBagFeedKg = 0f;
                pInt.CurrentBucketWaterLiters = 0f;
            }
            RefillStamina();
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            StardewHUDController.Instance?.RefreshHUD();
            HotbarController.Instance?.UpdateToolNameDisplay();
            ShowStatus("Đã đưa tài nguyên về mức khởi đầu (2,500g, 10 rào, 2 máng ăn, 2 máng nước).");
        }

        // --- THỜI GIAN ---
        public void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
            ShowStatus($"Đã đặt tốc độ trò chơi: {scale:0.0}x");
        }

        public void AdvanceHours(int hours)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                engine.Tick(hours * 60);
                MobileGameController.Instance.OnStateUpdated?.Invoke();
                StardewHUDController.Instance?.RefreshHUD();
                var clock = engine.Clock;
                ShowStatus($"Đã tua nhanh +{hours} giờ! Hiện tại: Ngày {clock.CurrentDay} - {clock.CurrentHour:00}:{clock.CurrentMinute:00}");
            }
        }

        public void AdvanceToNextDay()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                int currentMins = (engine.Clock.CurrentHour * 60) + engine.Clock.CurrentMinute;
                int remainingToNextDay = (24 * 60) - currentMins + (6 * 60); // 06:00 sáng hôm sau
                engine.Tick(remainingToNextDay);
                MobileGameController.Instance.OnStateUpdated?.Invoke();
                StardewHUDController.Instance?.RefreshHUD();
                ShowStatus($"Đã chuyển sang Ngày {engine.Clock.CurrentDay} (06:00 Sáng)!");
            }
        }

        public void JumpToHour(int targetHour)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                int currentTotal = (engine.Clock.CurrentHour * 60) + engine.Clock.CurrentMinute;
                int targetTotal = targetHour * 60;
                int diff = targetTotal - currentTotal;
                if (diff <= 0) diff += 24 * 60;

                engine.Tick(diff);
                MobileGameController.Instance.OnStateUpdated?.Invoke();
                StardewHUDController.Instance?.RefreshHUD();
                ShowStatus($"Đã chuyển thời gian tới {targetHour:00}:00 ({engine.Clock.GetTimeOfDay()})!");
            }
        }

        // --- ĐÀN HEO ---
        public void SpawnPig(GeneLineId geneLine, GeneRarity rarity, string breedName)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            Vector2 spawnPos = new Vector2(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-4f, 4f));
            int count = (engine.Pigs?.Count ?? 0) + 1;
            string pId = $"pig_dev_{count}";
            string pName = $"{breedName} #{count}";

            // 1. Tạo Pig Model trong Core
            var pigModel = Pig.CreateDefault(pId, pName, geneLine, rarity, initialAge: 6, initialWeight: 35f);
            pigModel.Stage = PigStage.DangLon;
            engine.Pigs.Add(pigModel);

            // 2. Tạo Pig GameObject trong 2D Scene
            var existingPig = FindAnyObjectByType<PigAgentView>();
            GameObject pigObj;
            if (existingPig != null)
            {
                pigObj = Instantiate(existingPig.gameObject, spawnPos, Quaternion.identity);
                pigObj.name = $"Pig_{count} ({pName})";
            }
            else
            {
                pigObj = new GameObject($"Pig_{count} ({pName})");
                pigObj.transform.position = spawnPos;
                var sr = pigObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = Mathf.RoundToInt(-spawnPos.y * 100);

                var rb = pigObj.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var col = pigObj.AddComponent<CircleCollider2D>();
                col.offset = new Vector2(0f, -0.15f);
                col.radius = 0.32f;

                pigObj.AddComponent<PigAgentView>();
                pigObj.AddComponent<PigSpriteAnimator>();
            }

            var agent = pigObj.GetComponent<PigAgentView>();
            var playerTr = PlayerInteractionController.Instance?.transform;
            agent.Bind(pigModel, playerTr);

            // Cập nhật Sprite đúng giống loài
            string folder = geneLine switch
            {
                GeneLineId.HongDien => "hong_dien",
                GeneLineId.LamKhe => "lam_khe",
                GeneLineId.KimTho => "kim_tho",
                GeneLineId.HuThe => "hu_the",
                _ => "hong_dien"
            };

            Sprite idleSp = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/idle.png", 16);
            Sprite walk1Sp = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/walk1.png", 16) ?? idleSp;
            Sprite walk2Sp = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/walk2.png", 16) ?? idleSp;
            Sprite eatSp = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/eat.png", 16) ?? idleSp;
            Sprite sleepSp = UISpriteLoader.LoadSprite($"Art/Sprites/Pigs/{folder}/sleep.png", 16) ?? idleSp;

            var anim = pigObj.GetComponent<PigSpriteAnimator>();
            if (anim != null && idleSp != null)
            {
                anim.SetBreedSprites(idleSp, new[] { walk1Sp, walk2Sp }, eatSp, sleepSp);
            }
            if (agent.SpriteRenderer != null && idleSp != null)
            {
                agent.SpriteRenderer.sprite = idleSp;
            }

            FarmEnvironment2D.Instance?.RefreshInfrastructureRegistries();
            MobileGameController.Instance.OnStateUpdated?.Invoke();
            StardewHUDController.Instance?.RefreshHUD();

            ShowStatus($"Đã sinh sản Heo {breedName} [{rarity}] tại bãi chăn thả!");
        }

        public void FeedAllPigs()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                foreach (var p in engine.Pigs)
                {
                    p.Mood = Mathf.Min(100, p.Mood + 25);
                    p.WeightKg += 1.5f;
                }
            }
            foreach (var a in FindObjectsByType<PigAgentView>())
            {
                a.ShowThought("No nê", 2.5f);
            }
            ShowStatus("Đã cho toàn bộ đàn heo ăn no nê (Tâm trạng +25, Cân nặng +1.5kg)!");
        }

        public void HealAndCalmAllPigs()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                foreach (var p in engine.Pigs)
                {
                    p.Health = 100;
                    p.Mood = 100;
                }
            }
            foreach (var a in FindObjectsByType<PigAgentView>())
            {
                a.ShowThought("Khỏe mạnh", 2.5f);
            }
            ShowStatus("Đã hồi phục 100% Sức Khỏe & Tinh Thần cho toàn bộ đàn heo!");
        }

        public void PromotePigStage()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                foreach (var p in engine.Pigs)
                {
                    if (p.Stage == PigStage.HeoNon)
                    {
                        p.Stage = PigStage.DangLon;
                        p.AgeDays = 7;
                        p.WeightKg = 35f;
                    }
                    else if (p.Stage == PigStage.DangLon)
                    {
                        p.Stage = PigStage.TruongThanh;
                        p.AgeDays = 18;
                        p.WeightKg = 85f;
                    }
                    else if (p.Stage == PigStage.TruongThanh)
                    {
                        p.Stage = PigStage.HeoGia;
                        p.AgeDays = 30;
                        p.WeightKg = 110f;
                    }
                }
            }
            ShowStatus("Đã nâng 1 giai đoạn trưởng thành cho toàn bộ heo trong trại!");
        }

        public void MaxBondingAll()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                foreach (var p in engine.Pigs)
                {
                    p.Bonding = 100f;
                }
            }
            foreach (var a in FindObjectsByType<PigAgentView>())
            {
                a.ShowThought("5 Tim", 2.5f);
            }
            ShowStatus("Đã nâng Độ Thân Thiết lên mức tối đa (5 Tim / 100%)!");
        }

        public void AwakenDeviantTraits()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine != null)
            {
                foreach (var p in engine.Pigs)
                {
                    foreach (var trait in p.Traits)
                    {
                        if (trait.IsDeviant) trait.IsAwakened = true;
                    }
                }
            }
            ShowStatus("Đã đánh thức toàn bộ Đặc Tính Dị Biến của đàn heo!");
        }

        // --- NÔNG TRẠI & HẠ TẦNG ---
        public void RefillAllFeeders()
        {
            var feeders = FindObjectsByType<Feeder2DView>();
            foreach (var f in feeders)
            {
                f.Refill(f.MaxFoodKg);
            }
            ShowStatus($"Đã đổ đầy thức ăn cho {feeders.Length} máng ăn trong trại!");
        }

        public void RefillAllWaterTroughs()
        {
            var troughs = FindObjectsByType<WaterTrough2DView>();
            foreach (var t in troughs)
            {
                t.Refill(t.MaxWaterLiters);
            }
            ShowStatus($"Đã bơm đầy nước ngọt cho {troughs.Length} máng nước!");
        }

        public void ResetDefaultFences()
        {
            var env = FarmEnvironment2D.Instance;
            if (env != null)
            {
                env.RebuildPastureFences();
                ShowStatus("Đã khôi phục hoàn chỉnh hàng rào khuôn viên chuồng thả 40m x 28m!");
            }
        }

        public void ClearAllFences()
        {
            var env = FarmEnvironment2D.Instance;
            if (env != null)
            {
                env.ClearAllFences();
                ShowStatus("Đã tháo dỡ toàn bộ hàng rào nông trại!");
            }
        }

        public void PlaceFenceAtPlayerPos()
        {
            var player = PlayerInteractionController.Instance;
            var env = FarmEnvironment2D.Instance;
            if (player != null && env != null)
            {
                Vector2Int pos = new Vector2Int(Mathf.RoundToInt(player.transform.position.x), Mathf.RoundToInt(player.transform.position.y));
                env.BuildFence(pos);
                ShowStatus($"Đã cắm 1 cọc rào mới tại ({pos.x}, {pos.y}) & tự động nối với rào lân cận!");
            }
        }

        public void AdjustHabitat(int amount)
        {
            var farm = MobileGameController.Instance?.Engine?.Farm;
            if (farm != null)
            {
                farm.HabitatIndex = Mathf.Clamp(farm.HabitatIndex + amount, 0, 100);
                MobileGameController.Instance.OnStateUpdated?.Invoke();
                StardewHUDController.Instance?.RefreshHUD();
                ShowStatus($"Điểm Sinh Thái SC: {farm.HabitatIndex}/100 [{farm.HabitatTier}]");
            }
        }

        public void AdjustPressure(int amount)
        {
            var farm = MobileGameController.Instance?.Engine?.Farm;
            if (farm != null)
            {
                farm.EventPressureIndex = Mathf.Clamp(farm.EventPressureIndex + amount, 0, 100);
                MobileGameController.Instance.OnStateUpdated?.Invoke();
                StardewHUDController.Instance?.RefreshHUD();
                ShowStatus($"Áp Lực Sự Kiện: {farm.EventPressureIndex}/100 [{farm.EventPressureState}]");
            }
        }

        public void ExpandCapacity()
        {
            var farm = MobileGameController.Instance?.Engine?.Farm;
            if (farm != null)
            {
                farm.HabitatIndex = Mathf.Min(100, farm.HabitatIndex + 20);
                MobileGameController.Instance.OnStateUpdated?.Invoke();
                StardewHUDController.Instance?.RefreshHUD();
                ShowStatus("Đã mở rộng sức chứa và tăng bậc môi trường nông trại!");
            }
        }

        public void TriggerBachVan()
        {
            var def = MobileGameController.Instance?.Engine?.Defense;
            if (def != null)
            {
                var bv = def.Buildings.Find(b => b.IsBachVan && !b.IsBroken);
                if (bv != null)
                {
                    var res = def.TriggerBachVanManual(bv.InstanceId);
                    ShowStatus($"[Bạch Vân Ký] {res.message}");
                }
                else
                {
                    ShowStatus("Chưa có công trình Bạch Vân hoặc đang bảo trì!");
                }
            }
        }

        // --- NHÂN VẬT & CAMERA ---
        public void ToggleFastMovement()
        {
            var player = FindAnyObjectByType<PlayerMobileController>();
            if (player != null)
            {
                player.DevForceRun = !player.DevForceRun;
                isFastMove = player.DevForceRun;
                ShowStatus(player.DevForceRun
                    ? "Chạy 7 m/s (GDD 3.6): BẬT. Đi bộ không trừ thể lực."
                    : "Chạy nhanh: TẮT. Đi bộ 4.5 m/s.");
            }
        }

        public void TeleportToCenter()
        {
            var player = FindAnyObjectByType<PlayerMobileController>();
            if (player != null)
            {
                player.transform.position = Vector3.zero;
                ShowStatus("Đã dịch chuyển người chơi về tâm chuồng trại (0, 0)!");
            }
        }

        public void TeleportToGate()
        {
            var player = FindAnyObjectByType<PlayerMobileController>();
            if (player != null)
            {
                player.transform.position = new Vector3(0f, -14f, 0f);
                ShowStatus("Đã dịch chuyển người chơi đến Cổng Nam bãi chăn thả!");
            }
        }

        public void SetCameraZoom(float orthoSize)
        {
            var camFollow = FindAnyObjectByType<CameraFollow2D>();
            if (camFollow != null)
            {
                camFollow.TargetOrthoSize = orthoSize;
                ShowStatus($"Camera ortho {orthoSize:0.0}, cao {orthoSize * 2f:0} m.");
            }
        }

        public void ExecuteSkill(CombatSlotType slot)
        {
            var player = FindAnyObjectByType<PlayerMobileController>();
            if (player != null)
            {
                player.ExecuteMartialSkill(slot);
                ShowStatus($"Đã thi triển kỹ năng chiến đấu: {slot}!");
            }
        }
        #endregion
    }
}
