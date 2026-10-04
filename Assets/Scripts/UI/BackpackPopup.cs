using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class BackpackPopup : MonoBehaviour
    {
        public static BackpackPopup Instance { get; private set; }

        [Header("Root Panel")]
        public GameObject ContentPanel;

        [Header("Text Displays")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ResourcesText;
        public TextMeshProUGUI FarmStatsText;
        public TextMeshProUGUI ToolDetailText;

        [Header("Buttons")]
        public Button CloseButton;
        public Transform ToolGridContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(Hide);
            }
        }

        private void Start()
        {
            InitializeVisuals();
            Hide();
        }

        private void Update()
        {
            // Phím B hoặc I để bật/tắt Balo
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I))
            {
                Toggle();
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
        }

        public void EnsureUIExists()
        {
            if (ContentPanel != null) return;

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var popupObj = new GameObject("Pig_Backpack_Popup");
            popupObj.transform.SetParent(canvas.transform, false);
            var pRt = popupObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.anchoredPosition = Vector2.zero;
            pRt.sizeDelta = new Vector2(540f, 440f);

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
            parchRt.offsetMin = new Vector2(16, 16);
            parchRt.offsetMax = new Vector2(-16, -16);
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
            tRt.anchoredPosition = new Vector2(0, -12);
            tRt.sizeDelta = new Vector2(0, 32);
            TitleText = titleObj.AddComponent<TextMeshProUGUI>();
            TitleText.text = "<b>TÚI ĐỒ TRANG TRẠI</b>";
            TitleText.fontSize = 22f;
            TitleText.fontStyle = FontStyles.Bold;
            TitleText.color = new Color(0.28f, 0.12f, 0.03f);
            TitleText.alignment = TextAlignmentOptions.Center;

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

            // ResourcesText (Trái)
            var resObj = new GameObject("ResourcesText");
            resObj.transform.SetParent(parchObj.transform, false);
            var resRt = resObj.AddComponent<RectTransform>();
            resRt.anchorMin = new Vector2(0, 0.45f);
            resRt.anchorMax = new Vector2(0.5f, 1);
            resRt.pivot = new Vector2(0, 1);
            resRt.anchoredPosition = new Vector2(25, -50);
            resRt.sizeDelta = new Vector2(230, 130);
            ResourcesText = resObj.AddComponent<TextMeshProUGUI>();
            ResourcesText.fontSize = 13.5f;
            ResourcesText.color = new Color(0.22f, 0.10f, 0.03f);

            // FarmStatsText (Phải)
            var fsObj = new GameObject("FarmStatsText");
            fsObj.transform.SetParent(parchObj.transform, false);
            var fsRt = fsObj.AddComponent<RectTransform>();
            fsRt.anchorMin = new Vector2(0.5f, 0.45f);
            fsRt.anchorMax = new Vector2(1, 1);
            fsRt.pivot = new Vector2(0, 1);
            fsRt.anchoredPosition = new Vector2(15, -50);
            fsRt.sizeDelta = new Vector2(235, 130);
            FarmStatsText = fsObj.AddComponent<TextMeshProUGUI>();
            FarmStatsText.fontSize = 13.5f;
            FarmStatsText.color = new Color(0.22f, 0.10f, 0.03f);

            // ToolDetailText (Dưới)
            var tdObj = new GameObject("ToolDetailText");
            tdObj.transform.SetParent(parchObj.transform, false);
            var tdRt = tdObj.AddComponent<RectTransform>();
            tdRt.anchorMin = new Vector2(0, 0);
            tdRt.anchorMax = new Vector2(1, 0.45f);
            tdRt.pivot = new Vector2(0.5f, 0.5f);
            tdRt.anchoredPosition = new Vector2(0, 8);
            tdRt.sizeDelta = new Vector2(470, 160);
            ToolDetailText = tdObj.AddComponent<TextMeshProUGUI>();
            ToolDetailText.fontSize = 12.5f;
            ToolDetailText.color = new Color(0.3f, 0.16f, 0.05f);
        }

        public void Hide()
        {
            if (ContentPanel != null) ContentPanel.SetActive(false);
        }

        private void InitializeVisuals()
        {
            if (ContentPanel == null) return;

            // Khung gỗ bên ngoài
            var panelImg = ContentPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.sprite = UISpriteLoader.GetFrameWoodPanel();
                panelImg.type = Image.Type.Sliced;
                panelImg.color = Color.white;
            }
            var pOutline = ContentPanel.GetComponent<Outline>();
            if (pOutline != null) pOutline.enabled = false;

            // Nền giấy cuộn
            Transform parchTr = ContentPanel.transform.Find("Parchment");
            if (parchTr != null)
            {
                var parchImg = parchTr.GetComponent<Image>();
                if (parchImg != null)
                {
                    parchImg.sprite = UISpriteLoader.GetFrameParchment();
                    parchImg.type = Image.Type.Sliced;
                    parchImg.color = Color.white;
                }
                var parchOutline = parchTr.GetComponent<Outline>();
                if (parchOutline != null) parchOutline.enabled = false;
            }

            // Nút đóng
            if (CloseButton != null)
            {
                var cbImg = CloseButton.GetComponent<Image>();
                if (cbImg != null)
                {
                    cbImg.sprite = UISpriteLoader.GetIconCloseCross();
                    cbImg.color = Color.white;
                }
                var cbOutline = CloseButton.GetComponent<Outline>();
                if (cbOutline != null) cbOutline.enabled = false;
                var cbTxt = CloseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (cbTxt != null) cbTxt.enabled = false;
            }
        }

        public void RefreshData()
        {
            var engine = MobileGameController.Instance?.Engine;
            var pInt = PlayerInteractionController.Instance;
            if (engine == null) return;

            var farm = engine.Farm;
            var stamina = engine.Character?.Stamina;
            var living = engine.Pigs.FindAll(p => p.IsAlive);
            var cap = farm.CalculateCapacityReport(living.Count, 0);

            // 1. Tài nguyên mang theo
            if (ResourcesText != null && pInt != null)
            {
                ResourcesText.text = 
                    $"• <b>Cọc Gỗ Rào:</b> <color=#B45309>{pInt.CarriedWoodPlanks}</color> cọc (Đóng/Sửa rào)\n" +
                    $"• <b>Ngân Khố:</b> <color=#D97706>{farm.Gold:N0}g</color> Vàng\n" +
                    $"• <b>Bao Cám:</b> <color=#CA8A04>{pInt.CurrentBagFeedKg:0} / {pInt.MaxBagFeedKg:0} kg</color>\n" +
                    $"• <b>Xô Nước:</b> <color=#0284C7>{pInt.CurrentBucketWaterLiters:0} / {pInt.MaxBucketWaterLiters:0} L</color>\n" +
                    $"• <b>Thể Lực:</b> <color=#16A34A>{(stamina != null ? stamina.CurrentStamina : 100):0} / 100</color>";
            }

            // 2. Chỉ số trang trại
            if (FarmStatsText != null)
            {
                FarmStatsText.text =
                    $"• <b>Đàn Heo:</b> {living.Count}/{cap.EffectiveCapacity} con ({cap.RawDensityRatio * 100:0}% mật độ)\n" +
                    $"• <b>Cảnh Quan (SC):</b> {farm.HabitatIndex}/100 [{farm.HabitatTier}]\n" +
                    $"• <b>Ám Khí (ÁLSK):</b> {farm.EventPressureIndex}/100 [{farm.EventPressureState}]\n" +
                    $"• <b>Bầy Đàn:</b> [{engine.Herd.Tier}] (Kế thừa: {engine.Herd.TransmissionStacks}/3)";
            }

            // 3. Danh mục công cụ
            if (ToolDetailText != null)
            {
                ToolDetailText.text = 
                    "<b>Dụng Cụ Trang Bị:</b>\n" +
                    "1. <b>Bao Cám</b>: Xúc từ Kho -> Đổ Máng Ăn\n" +
                    "2. <b>Xô Nước</b>: Múc từ Giếng -> Đổ Bồn Nước\n" +
                    "3. <b>Búa Gỗ</b>: Chạm vào đất để đóng cọc mới; Chạm vào rào để gỡ thu hồi 1 Gỗ; Gỡ Máng/Bồn/Tháp\n" +
                    "4. <b>Bàn Chải</b>: Chải lông heo (+12% Gắn kết)\n" +
                    "5. <b>Kính Lúp</b>: Soi chi tiết gen & thể trạng\n" +
                    "6. <b>Đao Gỗ</b>: Võ học đánh quái bảo vệ trại\n" +
                    "7. <b>Khử Trùng</b>: Tiêu độc dịch tả tại Khu Xử Lý\n" +
                    "8. <b>Lệnh Bạch Vân</b>: Kích hoạt Lôi Pháo";
            }
        }
    }
}
