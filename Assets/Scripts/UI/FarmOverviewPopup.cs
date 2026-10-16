using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Bảng điều khiển Trung Tâm Nông Trại & Đàn Heo (UI/UX của Người B cho scene Farm_Main).
    /// Bao gồm 4 phân khu:
    /// 1. Sức Chứa Điểm Nghẽn MIN(4 nguồn), Sinh Cảnh (SC), ÁLSK & Lớp phủ Bản đồ (30m phục vụ / 25m mát / Lưới 2x2m Xanh-Vàng-Đỏ)
    /// 2. Trạng Thái Bầy Đàn (7 điều kiện, Sơ Khai/Vững/Hưng Thịnh), Thủ Lĩnh, Kế Vị & Lệnh "Lùa về Nơi Trú" / "Tiếng Huýt Khoa"
    /// 3. Phối Giống F1/F2, 6 bước Hậu Duệ Heo Già, Sổ Tay 12 Dòng Gen & Khu Xử Lý (Thiêu Hủy / Tái Chế)
    /// 4. Y Tế 5 Động Từ, Thú Cưng (Chó Chăn Đàn, Chó Ngao Canh), NPC (Bác Sĩ Thú Y, Nông Công, Vệ Binh Khiên) & 4 Sự Kiện B
    /// </summary>
    public class FarmOverviewPopup : MonoBehaviour
    {
        public static FarmOverviewPopup Instance { get; private set; }

        public bool ShowServiceRadiusOverlay { get; private set; } = false;
        public bool ShowCoolingRadiusOverlay { get; private set; } = false;
        public bool ShowBuildGridOverlay { get; private set; } = false;

        private GameObject rootModal;
        private TextMeshProUGUI headerTitleText;
        private TextMeshProUGUI bodyReportText;
        private TextMeshProUGUI actionStatusText;
        private int activeTabIndex = 0;

        private readonly List<GameObject> worldOverlayObjects = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void EnsureUIExists(Transform canvasTransform)
        {
            if (rootModal != null || canvasTransform == null) return;

            // Nút mở bảng Quản Lý Trại & Đàn (Góc trên bên trái, dưới thanh máu/thể lực)
            var openBtnObj = new GameObject("Btn_FarmOverview_B");
            openBtnObj.transform.SetParent(canvasTransform, false);
            var openRt = openBtnObj.AddComponent<RectTransform>();
            openRt.anchorMin = new Vector2(0f, 1f);
            openRt.anchorMax = new Vector2(0f, 1f);
            openRt.pivot = new Vector2(0f, 1f);
            openRt.anchoredPosition = new Vector2(18f, -185f);
            openRt.sizeDelta = new Vector2(210f, 42f);

            var openImg = openBtnObj.AddComponent<Image>();
            openImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            openImg.type = Image.Type.Sliced;
            openImg.color = new Color(0.95f, 0.85f, 0.62f, 1f);

            var openBtn = openBtnObj.AddComponent<Button>();
            openBtn.onClick.AddListener(TogglePopup);

            var openTxtObj = new GameObject("Label");
            openTxtObj.transform.SetParent(openBtnObj.transform, false);
            var openTxtRt = openTxtObj.AddComponent<RectTransform>();
            openTxtRt.anchorMin = Vector2.zero;
            openTxtRt.anchorMax = Vector2.one;
            openTxtRt.offsetMin = Vector2.zero;
            openTxtRt.offsetMax = Vector2.zero;
            var openTxt = openTxtObj.AddComponent<TextMeshProUGUI>();
            openTxt.text = "SỔ TRẠI & BẦY ĐÀN [TAB]";
            openTxt.fontSize = 14f;
            openTxt.fontStyle = FontStyles.Bold;
            openTxt.alignment = TextAlignmentOptions.Center;
            openTxt.color = new Color(0.22f, 0.10f, 0.02f);

            // Cửa sổ chính
            rootModal = new GameObject("FarmOverview_Modal_B");
            rootModal.transform.SetParent(canvasTransform, false);
            var modalRt = rootModal.AddComponent<RectTransform>();
            modalRt.anchorMin = new Vector2(0.5f, 0.5f);
            modalRt.anchorMax = new Vector2(0.5f, 0.5f);
            modalRt.pivot = new Vector2(0.5f, 0.5f);
            modalRt.sizeDelta = new Vector2(820f, 540f);

            var modalImg = rootModal.AddComponent<Image>();
            modalImg.sprite = UISpriteLoader.GetFrameWoodPanel();
            modalImg.type = Image.Type.Sliced;
            modalImg.color = Color.white;

            // Nền giấy cuộn bên trong
            var parchObj = new GameObject("Parchment_Inner");
            parchObj.transform.SetParent(rootModal.transform, false);
            var parchRt = parchObj.AddComponent<RectTransform>();
            parchRt.anchorMin = Vector2.zero;
            parchRt.anchorMax = Vector2.one;
            parchRt.offsetMin = new Vector2(18f, 18f);
            parchRt.offsetMax = new Vector2(-18f, -18f);
            var parchImg = parchObj.AddComponent<Image>();
            parchImg.sprite = UISpriteLoader.GetFrameParchment();
            parchImg.type = Image.Type.Sliced;
            parchImg.color = Color.white;

            // Tiêu đề
            var titleObj = new GameObject("Header_Title");
            titleObj.transform.SetParent(parchObj.transform, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -10f);
            titleRt.sizeDelta = new Vector2(-80f, 34f);
            headerTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            headerTitleText.fontSize = 20f;
            headerTitleText.fontStyle = FontStyles.Bold;
            headerTitleText.alignment = TextAlignmentOptions.Center;
            headerTitleText.color = new Color(0.25f, 0.11f, 0.03f);

            // Nút đóng
            var closeObj = new GameObject("Btn_Close");
            closeObj.transform.SetParent(parchObj.transform, false);
            var closeRt = closeObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-8f, -8f);
            closeRt.sizeDelta = new Vector2(32f, 32f);
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.sprite = UISpriteLoader.GetIconCloseCross();
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => rootModal.SetActive(false));

            // 5 Nút chuyển Tab
            string[] tabNames =
            {
                "1. SỨC CHỨA & LỚP PHỦ",
                "2. BẦY ĐÀN & LÙA TRÚ",
                "3. PHỐI GIỐNG & XỬ LÝ",
                "4. Y TẾ, THÚ CƯNG & NPC",
                "5. SỔ HAI CỘT & CHỐNG CHỊU"
            };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;
                CreateActionButton(parchObj.transform, tabNames[i], new Vector2(-304f + i * 152f, 200f), new Vector2(146f, 32f), () =>
                {
                    activeTabIndex = idx;
                    RefreshContent();
                });
            }

            // Nội dung báo cáo chi tiết
            var bodyObj = new GameObject("Body_Report");
            bodyObj.transform.SetParent(parchObj.transform, false);
            var bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(24f, 110f);
            bodyRt.offsetMax = new Vector2(-24f, -88f);
            bodyReportText = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyReportText.fontSize = 14.5f;
            bodyReportText.alignment = TextAlignmentOptions.TopLeft;
            bodyReportText.color = new Color(0.20f, 0.09f, 0.02f);

            // Dòng thông báo kết quả thao tác
            var statusObj = new GameObject("Action_Status");
            statusObj.transform.SetParent(parchObj.transform, false);
            var statusRt = statusObj.AddComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0f, 0f);
            statusRt.anchorMax = new Vector2(1f, 0f);
            statusRt.pivot = new Vector2(0.5f, 0f);
            statusRt.anchoredPosition = new Vector2(0f, 68f);
            statusRt.sizeDelta = new Vector2(-40f, 28f);
            actionStatusText = statusObj.AddComponent<TextMeshProUGUI>();
            actionStatusText.fontSize = 14f;
            actionStatusText.fontStyle = FontStyles.Bold;
            actionStatusText.alignment = TextAlignmentOptions.Center;
            actionStatusText.color = new Color(0.08f, 0.45f, 0.18f);

            // Hàng 4 nút thao tác ngữ cảnh ở đáy bảng (thay đổi theo Tab 1–5)
            bottomButtons.Clear();
            bottomButtonTexts.Clear();
            for (int b = 0; b < 4; b++)
            {
                int slot = b;
                var (btn, txt) = CreateActionButtonWithRef(
                    parchObj.transform,
                    $"THAO TÁC {b + 1}",
                    new Vector2(-290f + b * 193f, -215f),
                    new Vector2(182f, 36f),
                    () => OnClickContextualBottomButton(slot));
                bottomButtons.Add(btn);
                bottomButtonTexts.Add(txt);
            }

            rootModal.SetActive(false);
        }

        private readonly List<Button> bottomButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> bottomButtonTexts = new List<TextMeshProUGUI>();
        private int healthVerbCycleIndex = 0;
        private int occultEventCycleIndex = 0;
        private int villagerDialogCycleIndex = 0;
        private int endingCycleIndex = 0;

        private void CreateActionButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size, Action onClick)
        {
            CreateActionButtonWithRef(parent, label, anchoredPos, size, onClick);
        }

        private (Button btn, TextMeshProUGUI txt) CreateActionButtonWithRef(Transform parent, string label, Vector2 anchoredPos, Vector2 size, Action onClick)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = btnObj.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameSlotNormal();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.96f, 0.88f, 0.70f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.24f, 0.11f, 0.03f);
            return (btn, tmp);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                TogglePopup();
            }
        }

        public void TogglePopup()
        {
            if (rootModal == null) return;
            bool next = !rootModal.activeSelf;
            rootModal.SetActive(next);
            if (next)
            {
                RefreshContent();
            }
        }

        private void SetBottomButtonLabels(string b0, string b1, string b2, string b3)
        {
            if (bottomButtonTexts.Count < 4) return;
            bottomButtonTexts[0].text = b0;
            bottomButtonTexts[1].text = b1;
            bottomButtonTexts[2].text = b2;
            bottomButtonTexts[3].text = b3;
        }

        public void RefreshContent()
        {
            if (MobileGameController.Instance == null || MobileGameController.Instance.Engine == null) return;
            var engine = MobileGameController.Instance.Engine;
            var farm = engine.Farm;
            var herd = engine.Herd;
            var livingPigs = engine.Pigs.FindAll(p => p.IsAlive);
            int corpsesOutside = engine.Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
            var cap = farm.CalculateCapacityReport(livingPigs.Count, corpsesOutside);

            if (activeTabIndex == 0)
            {
                headerTitleText.text = "PHÂN KHU 1: SỨC CHỨA ĐIỂM NGHẼN, SINH CẢNH & LỚP PHỦ BẢN ĐỒ";
                SetBottomButtonLabels(
                    "MỞ RỘNG +1 Ô ĐẤT (200G)",
                    $"NÂNG [{cap.Bottleneck}] (150G)",
                    "MUA +100KG CÁM (80G)",
                    "LỚP PHỦ 30M & LƯỚI 2×2M");

                var checklist = engine.GridZones.ValidateChecklistGdd310();
                string crowdStr = farm.LocalCrowdingActive
                    ? $"<color=#DC2626><b>ĐANG CHEN CHÚC CỤC BỘ (Lây bệnh ×{farm.LocalCrowdingContagionMultiplier:0.00}, Hoảng +{farm.LocalCrowdingPanicBonus * 100f:0}%)</b></color>"
                    : "<color=#15803D>Thông thoáng (≤ 16 heo/Ô Đất 10×10m)</color>";

                bodyReportText.text =
                    $"<b>1. Sức Chứa Chuồng Theo Công Thức MIN(4 Nguồn) — GDD 3.2:</b>\n" +
                    $"   • Ô Đất ({farm.Infrastructure.LandPlots}/40 × 8) = <b>{cap.CapacityByLand}</b> chỗ   |   Mái Trú ({farm.Infrastructure.Shelters} × 6) = <b>{cap.CapacityByShelters}</b> chỗ\n" +
                    $"   • Bồn Nước ({farm.Infrastructure.WaterTroughs} × 12) = <b>{cap.CapacityByWater}</b> chỗ   |   Máng Ăn ({farm.Infrastructure.Feeders} × 10) = <b>{cap.CapacityByFeeders}</b> chỗ\n" +
                    $"   • <color=#B45309><b>Điểm Nghẽn Hiện Tại: [{cap.Bottleneck}] → Sức Chứa Hiệu Dụng: {cap.EffectiveCapacity} con</b></color> (Kho cám: <b>{farm.FeedStockKg:0.0} kg</b>)\n" +
                    $"   • Mật Độ Đàn: <b>{cap.LivingPigCount}/{cap.EffectiveCapacity} ({cap.RawDensityRatio * 100f:0.0}%)</b> — Vùng: <b>{cap.DensityZoneName}</b> (Hệ số bệnh ×{cap.DiseaseDensityMultiplier:0.00})\n\n" +
                    $"<b>2. Chỉ Số Môi Trường, Chen Chúc Cục Bộ & Nghiệm Thu GDD 3.10:</b>\n" +
                    $"   • Chỉ số Sinh Cảnh (SC): <color=#15803D><b>{farm.HabitatIndex}/100 [{farm.HabitatTier}]</b></color>   |   Áp Lực Sự Kiện (ÁLSK): <color=#7E22CE><b>{farm.EventPressureIndex}/100 [{farm.EventPressureState}]</b></color>\n" +
                    $"   • Trạng thái Ô Đất 10×10m: {crowdStr}   |   Chuồng Nhiễm: <b>{(farm.IsBarnContaminated ? "CÓ (Cần dọn!)" : "Không")}</b>\n" +
                    $"   • Nghiệm thu GDD 3.10 (Nhóm A–H): <color=#15803D><b>{(checklist.AllPassed ? "ĐẠT 8/8 NHÓM CHUẨN" : "ĐANG KIỂM TRA")}</b></color>";
            }
            else if (activeTabIndex == 1)
            {
                headerTitleText.text = "PHÂN KHU 2: TRẠNG THÁI BẦY ĐÀN, THỦ LĨNH, KẾ VỊ & LÙA TRÚ";
                SetBottomButtonLabels(
                    "LÙA ĐÀN VỀ NƠI TRÚ",
                    "TIẾNG HUÝT KHOA (18M)",
                    "CHỌN ỨNG VIÊN KẾ VỊ",
                    "BỒI DƯỠNG KẾ VỊ (+5%)");

                var cond = herd.EvaluateConditions(engine.Pigs, farm, engine.MajorWavesSurvived);
                string leaderInfo = herd.Leader != null
                    ? $"<b>{herd.Leader.Name}</b> (#{herd.Leader.PigId}, tại vị {herd.Leader.DaysAsLeader} ngày, Neo Tinh Thần ≥ {herd.Leader.NeoThreshold} trong {herd.Leader.NeoRadiusMeters}m)"
                    : "Chưa bầu Thủ Lĩnh (Cần đạt Bầy Đàn Sơ Khai)";

                bodyReportText.text =
                    $"<b>1. Trạng Thái Bầy Đàn (GDD 4.3):</b> <color=#B45309><b>[{herd.Tier}]</b></color> (Duy trì {herd.ConsecutiveDaysInState} ngày | Truyền Thừa: {herd.TransmissionStacks} tầng)\n" +
                    $"   • ĐK1 Heo gắn kết (≥100, Kim Thọ ×3): <b>{cond.CohesivePigsCount}/100</b> {(cond.Condition1_Met ? "✔" : "✖")}\n" +
                    $"   • ĐK2 Sinh Cảnh ≥ 50: <b>{cond.CurrentSC}</b> {(cond.Condition2_Met ? "✔" : "✖")}   |   ĐK3 Tinh thần TB ≥ {cond.RequiredAvgMood} & 0 Hoảng: <b>{cond.AvgMood:0.0}</b> {(cond.Condition3_Met ? "✔" : "✖")}\n" +
                    $"   • ĐK4 Mật độ 71–110%: <b>{cond.DensityRatio * 100f:0.0}%</b> {(cond.Condition4_Met ? "✔" : "✖")}   |   ĐK5 Heo Già gắn kết ≥ 6: <b>{cond.ElderCohesiveCount}/6</b> {(cond.Condition5_Met ? "✔" : "✖")}\n" +
                    $"   • ĐK6 Không dịch/Chuồng nhiễm: {(cond.Condition6_Met ? "✔" : "✖")}   |   ĐK7 Trại cấp ≥ 6 & qua ≥ 4 đợt lớn: <b>Cấp {cond.FarmLevel} / {cond.MajorWavesSurvived} đợt</b> {(cond.Condition7_Met ? "✔" : "✖")}\n\n" +
                    $"<b>2. Thủ Lĩnh & Bồi Dưỡng Kế Vị:</b>\n" +
                    $"   • Thủ Lĩnh: {leaderInfo}\n" +
                    $"   • Ứng viên Kế Vị: <b>{(string.IsNullOrEmpty(herd.SuccessionTraining.CandidatePigId) ? "Chưa chọn" : herd.SuccessionTraining.CandidatePigId)}</b> ({herd.SuccessionTraining.ConsecutiveCareDays} ngày chăm | +{herd.SuccessionTraining.BonusSuccessionPercentage}% kế vị, +{herd.SuccessionTraining.BonusTransmissionPercentage}% Truyền Thừa)\n" +
                    $"   • Nơi Trú Đàn: <b>{engine.GridZones.HerdRefugePlotCount * 8} chỗ</b> ({engine.GridZones.HerdRefugeEntranceCount} cổng rộng {engine.GridZones.HerdRefugeMinEntranceWidthMeters:0.1}m — Gom ≥90% đàn trong ≤2 giờ game)";
            }
            else if (activeTabIndex == 2)
            {
                headerTitleText.text = "PHÂN KHU 3: PHỐI GIỐNG F1/F2, HẬU DUỆ HEO GIÀ & KHU XỬ LÝ XÁC";
                SetBottomButtonLabels(
                    "GHÉP ĐÔI PHỐI F1/F2",
                    "PHỐI HEO GIÀ (6 BƯỚC)",
                    "THIÊU HỦY XÁC (45G+8L)",
                    "TÁI CHẾ SINH HỌC (80G+10L)");

                int pregnantCount = livingPigs.FindAll(p => p.GestationDaysRemaining > 0).Count;
                int activeJobs = engine.GridZones.ActiveCarcassJobs.FindAll(j => !j.Completed).Count;
                bodyReportText.text =
                    $"<b>1. Quy Tắc Phối Giống & Cửa Sổ Định Hình (GDD 4.4, 4.7, 4.8):</b>\n" +
                    $"   • Đang mang thai: <b>{pregnantCount}/{BreedingLedger.MaxConcurrentPregnancies} ca</b> | Tuổi phối: <b>≥{BreedingLedger.MinAgeDays} ngày</b> | Mang thai: <b>{BreedingLedger.GestationDays} ngày</b> | Nghỉ: <b>{BreedingLedger.RestDays} ngày</b>\n" +
                    $"   • Tỷ lệ thụ thai chuẩn: <b>70%</b> (Heo già: <b>38.5%</b>). Heo non mồ côi tử vong <b>12%/ngày</b> (<b>0%</b> nếu có nái <b>Phong Mẫu</b> trong 10m).\n" +
                    $"   • 6 Bước Hậu Duệ Heo Già: Khỏe mạnh K ∈ <b>[22%, 32%]</b>; phần bất thường chia <b>35% Chết yểu / 40% Thể trạng yếu / 25% Gen xấu</b> (phạt 5–20%); <b>2% Ưu Tú → 45% Thức tỉnh gen trội</b>.\n\n" +
                    $"<b>2. Khu Xử Lý Xác ({activeJobs}/{engine.GridZones.ProcessingLotSlots} ô đang chạy, Xác ngoài chuồng: {corpsesOutside}) & Khu Cách Ly:</b>\n" +
                    $"   • Kéo xác: <b>+1 STA / 10m</b>   |   Thiêu Hủy: <b>45 Vàng + 8L nhiên liệu (6 giờ)</b>   |   Tái Chế Sinh Học: <b>80 Vàng + 10L (18 giờ, thu 25–32% cám)</b>\n" +
                    $"   • Sổ Tay 12 Dòng Gen: <b>Hồng Điền, Lam Khê, Mộc Cước, Thổ Trầm, Xích Mao, Phong Mẫu, Thiết Bì, Tinh Quang, Lôi Mạch, Mộng Nhãn, Kim Thọ, Hư Thể</b> ({GeneticsAndBreedingSystem.AllTraitDefinitions.Count} tính trạng phụ & dị biến).";
            }
            else if (activeTabIndex == 3)
            {
                headerTitleText.text = "PHÂN KHU 4: Y TẾ 5 ĐỘNG TỪ, THÚ CƯNG, NPC & 4 SỰ KIỆN HUYỀN BÍ (B)";
                SetBottomButtonLabels(
                    "THỰC THI 5 ĐỘNG TỪ Y TẾ",
                    "TRANG BỊ THÚ CƯNG B",
                    "THUÊ & CA TRỰC 3 NPC B",
                    "KÍCH HOẠT 4 SỰ KIỆN B");

                var petBonuses = engine.PetsAndNpcs.GetActivePetBonuses();
                int sickCount = livingPigs.FindAll(p => p.ActiveDisease.HasValue).Count;
                int isolatedCount = livingPigs.FindAll(p => p.IsIsolated).Count;
                bodyReportText.text =
                    $"<b>1. 10 Mối Đe Dọa Sức Khỏe & 5 Động Từ Xử Lý (GDD 6.4–6.5):</b>\n" +
                    $"   • Ca nhiễm hiện tại: <b>{sickCount} con</b> | Đang cách ly: <b>{isolatedCount}/{DiseaseBoard.IsolationCapacity * 2} con</b> | Thảo dược: <b>{farm.MedicalHerbCount}</b> | Vắc-xin: <b>{farm.VaccineDoseCount}</b>\n" +
                    $"   • Nhóm A (Lây): Hô Hấp Lạnh, Ghẻ Ký Sinh, Dịch Tả Heo | Nhóm B (Vệ sinh): Sốt Bùn, Chuồng Nhiễm | Nhóm C/D: Say Nắng, Rối Loạn Sinh Sản, Nấm Phát Quang, Lôi Tâm, Hư Mạch\n" +
                    $"   • 5 Động Từ: <b>[1] Cách Ly  •  [2] Xử Lý Môi Trường  •  [3] Điều Trị Cá Thể  •  [4] Phòng Ngừa Đàn  •  [5] Nghiên Cứu ({engine.PetsAndNpcs.ResearchedDiseases.Count} bệnh)</b>\n\n" +
                    $"<b>2. Thú Cưng Đi Kèm ({engine.PetsAndNpcs.ActivePets.Count}/{PetNpcAndOccultManager.MaxActivePets}) & NPC Của Người B ({engine.PetsAndNpcs.HiredNpcs.Count} đã thuê):</b>\n" +
                    $"   • Thú Cưng: <b>Chó Chăn Đàn</b> (+20% tốc độ lùa đàn) & <b>Chó Ngao Canh</b> (chặn trộm đêm) — Tốc độ lùa hiện tại: <b>×{petBonuses.recallSpeedMultiplier:0.00}</b>\n" +
                    $"   • 3 NPC Chủ Lực B: <b>Bác Sĩ Thú Y</b> (chữa 2 heo/ngày) • <b>Nông Công</b> (+8 SC/ngày) • <b>Vệ Binh Khiên</b> (giữ cổng -40% sát thương)\n" +
                    $"   • 4 Sự Kiện Huyền Bí B: <b>Vườn Mười Hai Quả • Đồng Cỏ Không Bóng • Bí Nhân Mua Già • Đổi Một Lấy Một</b>";
            }
            else
            {
                headerTitleText.text = "PHÂN KHU 5: THANH CHỐNG CHỊU, CƠ CHẾ SÂU & SỔ HAI CỘT MÙA 1 'SÁU VỚI MỘT'";
                SetBottomButtonLabels(
                    "TIẾN TRIỂN NHIỆM VỤ / SỔ TAY",
                    "HỎI CHUYỆN DÂN LÀNG BỜ RÌA",
                    "MẶC 6 TRANG BỊ & ẤN BẤT KHUẤT",
                    "DÙNG QUẢ MỘNG / CHỌN KẾT THÚC");

                var ch = engine.Character;
                var story = engine.Story;
                var deep = engine.DeepMechanics;
                var nextQuest = story.Quests.Find(q => !q.IsCompleted && !q.IsSideQuest) ?? story.Quests[story.Quests.Count - 1];
                int verifiedCount = story.NotebookEntries.FindAll(n => n.IsVerifiedToRightColumn).Count;

                bodyReportText.text =
                    $"<b>1. Chiến Đấu Đỡ Tự Động (Auto-Guard), Thanh Chống Chịu & Cơ Chế Chuyên Sâu (GDD 8–20):</b>\n" +
                    $"   • Thanh Chống Chịu: <color=#0284C7><b>{ch.CurrentGuard:0.0}/{ch.MaxGuard:0.0}</b></color> | Thể Lực 2 Lớp: <color=#15803D><b>{ch.Stamina.CurrentStamina:0.0}/{ch.GetEffectiveMaxStamina():0.0}</b></color> (Tích tải ngày: {ch.Stamina.DailyLoad:0.0}, Thể trạng: {ch.Stamina.Condition})\n" +
                    $"   • Cơ chế: Quái đánh mặc định nhân vật <b>Tự Đỡ (Auto-Guard)</b> làm hao Thể Lực & Chống Chịu; <b>Đỡ Chủ Động (Chuột Phải/U/L)</b> giảm 82% hao Chống Chịu & phản 50% ST. Về 0 điểm là <b>Ngất & Kéo về Hiên Nhà Chính</b>!\n" +
                    $"   • Lối chơi: <b>{ch.GetPlaystyle()}</b> (Phòng thủ TB: {ch.GetTotalEquipmentDefense()}, 3 cặp ô Võ học: {(ch.IsMartialSlotUnlocked(CombatSlotType.TheCong) ? "Đã mở đủ" : "Chưa mặc đủ 6 món")}) | Mệnh Ấn: <b>{(ch.EngravedSeals.Count > 0 ? string.Join(", ", ch.EngravedSeals) : "Chưa khắc")}</b>\n" +
                    $"   • Lưới Điện: <b>Máy phát Cấp {deep.PowerGrid.GeneratorLevel} ({deep.PowerGrid.CurrentDemandUnits:0}/{deep.PowerGrid.GetContinuousCapacity()}u, Đỉnh {deep.PowerGrid.GetPeakCapacity()}u, Tụ {deep.PowerGrid.CapacitorChargeUnits:0}u)</b> | Giấy Chuyên Biệt: <b>[{deep.EquippedPaperColor} +{deep.intEquippedPaperEnhancementLevel}]</b> (Khóa {deep.UnlockedSpecialtyLocksCount}/6)\n\n" +
                    $"<b>2. Cốt Truyện Mùa 1 'Sáu Với Một' & Sổ Hai Cột Của Khoa ([THẤY] | [CÓ THẬT] | [LỀ BÚT CHÌ]):</b>\n" +
                    $"   • Chương hiện tại: <color=#B45309><b>[{story.CurrentChapter}]</b></color> | Nhiệm vụ tiếp: <b>{nextQuest.Title}</b> ({nextQuest.ObjectiveSummary})\n" +
                    $"   • Sổ Hai Cột: Đã xác thực <b>{verifiedCount}/{story.NotebookEntries.Count}</b> mục sang cột <b>[CÓ THẬT]</b> | Chỗ trống giữa đàn (1,2m): <b>{(story.HerdEmptySpotObserved ? "ĐÃ KHỚP ẢNH 2018" : "Đang quan sát")}</b> | Kết thúc: <b>{story.ChosenEnding}</b>\n" +
                    $"   • Ghi chú Khoa: <i>\"{nextQuest.KhoaDeadpanNote}\"</i>";
            }
        }

        private void OnClickContextualBottomButton(int slot)
        {
            if (MobileGameController.Instance == null || MobileGameController.Instance.Engine == null) return;
            var engine = MobileGameController.Instance.Engine;
            var farm = engine.Farm;

            if (activeTabIndex == 0)
            {
                if (slot == 0)
                {
                    if (farm.Infrastructure.LandPlots >= FarmGridAndZones.MaxExpandedLandPlots)
                    {
                        SetStatus("Đã đạt tối đa 40 Ô Đất mở rộng!");
                    }
                    else if (farm.SpendGold(200))
                    {
                        farm.Infrastructure.LandPlots += 1;
                        SetStatus($"✔ Đã mở rộng thêm 1 Ô Đất (10×10m = 25 Ô Xây)! Tổng: {farm.Infrastructure.LandPlots}/40 Ô Đất.");
                    }
                    else SetStatus("Cần 200 Vàng để mở rộng 1 Ô Đất!");
                }
                else if (slot == 1)
                {
                    int living = engine.Pigs.FindAll(p => p.IsAlive).Count;
                    int corpses = engine.Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
                    var cap = farm.CalculateCapacityReport(living, corpses);
                    if (!farm.SpendGold(150))
                    {
                        SetStatus("Cần 150 Vàng để nâng cấp hạ tầng điểm nghẽn!");
                    }
                    else
                    {
                        int cellX = 14 + (engine.GridZones.Structures.Count % 12) * 2;
                        int cellY = 10 + (engine.GridZones.Structures.Count / 12) * 2;
                        if (cap.Bottleneck == "Shelters")
                        {
                            farm.Infrastructure.Shelters += 1;
                            engine.GridZones.TryPlaceStructure("Shelter_Exp", cellX, cellY, BuildingFootprintSize.Medium_2x2, 150, 0f, false, false, false, true, false);
                        }
                        else if (cap.Bottleneck == "Feeders")
                        {
                            farm.Infrastructure.Feeders += 1;
                            engine.GridZones.TryPlaceStructure("Feeder_Exp", cellX, cellY, BuildingFootprintSize.Small_1x1, 150, 0f, false, true, false, false, false);
                        }
                        else if (cap.Bottleneck == "WaterTroughs")
                        {
                            farm.Infrastructure.WaterTroughs += 1;
                            engine.GridZones.TryPlaceStructure("Water_Exp", cellX, cellY, BuildingFootprintSize.Small_1x1, 150, 0f, false, false, true, false, true);
                        }
                        else
                        {
                            farm.Infrastructure.LandPlots = Math.Min(40, farm.Infrastructure.LandPlots + 1);
                        }
                        SetStatus($"✔ Đã nâng cấp điểm nghẽn [{cap.Bottleneck}] và đặt công trình lên lưới 2×2m!");
                    }
                }
                else if (slot == 2)
                {
                    if (farm.BuyFeedStock(100f, 80))
                        SetStatus($"✔ Đã mua +100 kg cám! Tồn kho hiện tại: {farm.FeedStockKg:0.0} kg.");
                    else
                        SetStatus("Cần 80 Vàng để mua 100 kg cám!");
                }
                else
                {
                    OnClickToggleOverlays();
                }
                RefreshContent();
            }
            else if (activeTabIndex == 1)
            {
                if (slot == 0) OnClickRecallToRefuge();
                else if (slot == 1) OnClickKhoaWhistle();
                else if (slot == 2)
                {
                    Pig best = null;
                    foreach (var p in engine.Pigs)
                    {
                        if (p.IsAlive && (p.Stage == PigStage.TruongThanh || p.Stage == PigStage.HeoGia))
                        {
                            if (p.Bonding < 75f) p.Bonding = 78f; // Huấn luyện nhanh ứng viên để sẵn sàng đề cử
                            if (best == null || p.Bonding > best.Bonding) best = p;
                        }
                    }
                    if (best != null)
                    {
                        var (_, msg) = engine.Herd.SelectSuccessionCandidate(best);
                        SetStatus($"✔ {msg}");
                    }
                    else SetStatus("Chưa có heo Trưởng Thành / Heo Già đủ điều kiện làm ứng viên Kế Vị.");
                    RefreshContent();
                }
                else
                {
                    engine.Herd.TrainSuccessionCandidateDaily(engine.Pigs);
                    SetStatus($"✔ Đã Bồi Dưỡng Kế Vị (+{engine.Herd.SuccessionTraining.BonusSuccessionPercentage}% kế vị, +{engine.Herd.SuccessionTraining.BonusTransmissionPercentage}% Truyền Thừa)!");
                    RefreshContent();
                }
            }
            else if (activeTabIndex == 2)
            {
                if (slot == 0 || slot == 1)
                {
                    bool wantElder = (slot == 1);
                    Pig sow = engine.Pigs.Find(p => p.IsAlive && p.Gender == "Cai" && p.GestationDaysRemaining == 0 &&
                        (wantElder ? p.Stage == PigStage.HeoGia : p.Stage == PigStage.TruongThanh));
                    Pig boar = engine.Pigs.Find(p => p.IsAlive && p.Gender == "Duc" &&
                        (wantElder ? (p.Stage == PigStage.HeoGia || p.Stage == PigStage.TruongThanh) : p.Stage == PigStage.TruongThanh));

                    if (sow == null)
                    {
                        // Đảm bảo có cặp heo mẫu để người chơi test ngay trên UI nếu đàn chưa có heo Cái/Đực đúng tuổi
                        sow = Pig.CreateDefault($"pig_sow_{engine.Pigs.Count + 1}", wantElder ? "Nái Già Hồng Điền" : "Nái Giống Phong Mẫu", wantElder ? GeneLineId.HongDien : GeneLineId.PhongMau, GeneRarity.Kha, "Cai", wantElder ? 30 : 16, 95f);
                        engine.Pigs.Add(sow);
                    }
                    if (boar == null)
                    {
                        boar = Pig.CreateDefault($"pig_boar_{engine.Pigs.Count + 1}", wantElder ? "Đực Già Kim Thọ" : "Đực Giống Lam Khê", wantElder ? GeneLineId.KimTho : GeneLineId.LamKhe, GeneRarity.Hiem, "Duc", wantElder ? 30 : 16, 102f);
                        engine.Pigs.Add(boar);
                    }

                    var (_, msg) = engine.SchedulePigBreeding(sow.Id, boar.Id, deterministicConceptionRoll: 0.15);
                    SetStatus($"✔ {msg}");
                    RefreshContent();
                }
                else
                {
                    var method = slot == 2 ? CarcassProcessMethod.Incinerate_ThieuHuy : CarcassProcessMethod.BioRecycle_TaiCheSinhHoc;
                    var deadPig = engine.Pigs.Find(p => !p.IsAlive && !p.IsInProcessingLot);
                    if (deadPig == null)
                    {
                        SetStatus("Hiện không có xác heo nào ngoài chuồng cần đưa vào Khu Xử Lý.");
                    }
                    else
                    {
                        var (_, msg, sta) = engine.ProcessDeadPigCarcass(deadPig.Id, method);
                        SetStatus($"✔ {msg} (Hao {sta} STA kéo xác)");
                    }
                    RefreshContent();
                }
            }
            else if (activeTabIndex == 3)
            {
                if (slot == 0)
                {
                    var verbs = new[]
                    {
                        HealthTreatmentVerb.XuLyMoiTruong,
                        HealthTreatmentVerb.DieuTriCaThe,
                        HealthTreatmentVerb.PhongNguaDan,
                        HealthTreatmentVerb.CachLy,
                        HealthTreatmentVerb.NghienCuu
                    };
                    var verb = verbs[healthVerbCycleIndex % verbs.Length];
                    healthVerbCycleIndex++;
                    var (_, msg) = engine.ExecuteHealthTreatment(verb);
                    SetStatus($"✔ [{verb}] {msg}");
                    RefreshContent();
                }
                else if (slot == 1)
                {
                    engine.PetsAndNpcs.TryEquipPet(PetId.ChoChanDan);
                    engine.PetsAndNpcs.TryEquipPet(PetId.ChoNgaoCanh);
                    SetStatus("✔ Đã trang bị cặp Thú Cưng của B: Chó Chăn Đàn (+20% tốc độ lùa) & Chó Ngao Canh (chặn trộm đêm)!");
                    RefreshContent();
                }
                else if (slot == 2)
                {
                    OnClickCleanAndTreat();
                }
                else
                {
                    var events = new[]
                    {
                        MemberBOccultEventId.VuonMuoiHaiQua,
                        MemberBOccultEventId.DongCoKhongBong,
                        MemberBOccultEventId.BiNhanMuaGia,
                        MemberBOccultEventId.DoiMotLayMot
                    };
                    var ev = events[occultEventCycleIndex % events.Length];
                    occultEventCycleIndex++;
                    var (_, msg) = engine.TriggerMemberBOccultEvent(ev, 0);
                    SetStatus($"✔ {msg}");
                    RefreshContent();
                }
            }
            else
            {
                if (slot == 0)
                {
                    var nextQ = engine.Story.Quests.Find(q => !q.IsCompleted && !q.IsSideQuest)
                             ?? engine.Story.Quests.Find(q => !q.IsCompleted);
                    if (nextQ != null)
                    {
                        var (_, msg) = engine.Story.CompleteQuestStep(nextQ.QuestId);
                        SetStatus($"✔ {msg}");
                    }
                    else
                    {
                        SetStatus("✔ Đã hoàn thành toàn bộ chuỗi nhiệm vụ P01–P08, Q01–Q20 & S01–S12 của Mùa 1!");
                    }
                    RefreshContent();
                }
                else if (slot == 1)
                {
                    string[] npcs = { "BaSau", "Man", "TamVa", "OngTon", "BacSiDung" };
                    string npc = npcs[villagerDialogCycleIndex % npcs.Length];
                    villagerDialogCycleIndex++;
                    string reply = engine.Story.AskVillagerAboutTopic(npc, "năm 2018 & số 7");
                    SetStatus($"💬 {reply}");
                    RefreshContent();
                }
                else if (slot == 2)
                {
                    engine.Character.EquipStarterSet();
                    engine.Character.EngraveSeal("AnBatKhuat");
                    engine.Character.CurrentGuard = engine.Character.MaxGuard;
                    var (_, enhMsg) = engine.DeepMechanics.EnhanceSpecialtyPaper(0.25);
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    SetStatus($"✔ Đã mặc đủ 6 Trang Bị (mở khóa 3 ô Thế Công/Thủ/Biến), khắc [Ấn Bất Khuất] & {enhMsg}");
                    RefreshContent();
                }
                else
                {
                    if (engine.Clock.SleepCooldownHoursRemaining > 0 || engine.Character.CurrentGuard < engine.Character.MaxGuard)
                    {
                        engine.DeepMechanics.SleepItemsInventory[SleepBypassItemId.QuaMong] = Math.Max(1, engine.DeepMechanics.SleepItemsInventory[SleepBypassItemId.QuaMong]);
                        var (_, msg) = engine.DeepMechanics.UseSleepBypassItem(SleepBypassItemId.QuaMong, engine.Clock, engine.Character, engine.Farm);
                        MobileGameController.Instance?.OnStateUpdated?.Invoke();
                        SetStatus($"✔ {msg}");
                    }
                    else
                    {
                        var endings = new[]
                        {
                            StoryEndingId.Ending_8_1_GiuTraiThaoDuongGui,
                            StoryEndingId.Ending_8_2_GiuMayLamHoSo,
                            StoryEndingId.Ending_8_3_MangVongVeNha
                        };
                        var chosen = endings[endingCycleIndex % endings.Length];
                        endingCycleIndex++;
                        var (_, title, _, postCredit) = engine.Story.SelectSeasonOneEnding(chosen);
                        SetStatus($"✔ [{title}] — {postCredit}");
                    }
                    RefreshContent();
                }
            }
        }

        private void SetStatus(string text)
        {
            if (actionStatusText != null)
            {
                actionStatusText.text = text;
            }
        }

        private void OnClickRecallToRefuge()
        {
            if (MobileGameController.Instance == null) return;
            var engine = MobileGameController.Instance.Engine;
            var (sheltered, total, _) = engine.CommandHerdRecallToRefuge(false);
            var views = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            foreach (var v in views)
            {
                if (v != null) v.CommandRecallToShelter(new Vector3(engine.GridZones.HerdRefugeCenter.x, engine.GridZones.HerdRefugeCenter.y, 0f));
            }

            SetStatus($"✔ Đã phát lệnh LÙA VỀ NƠI TRÚ: {sheltered}/{total} heo về Nơi Trú an toàn trong ≤ 2 giờ game!");
            RefreshContent();
        }

        private void OnClickKhoaWhistle()
        {
            if (MobileGameController.Instance == null) return;
            var engine = MobileGameController.Instance.Engine;
            engine.PetsAndNpcs.TryEquipPet(PetId.ChoChanDan);
            engine.PetsAndNpcs.TryEquipPet(PetId.ChoNgaoCanh);

            Vector3 playerPos = Vector3.zero;
            var player = FindFirstObjectByType<PlayerMobileController>();
            if (player != null) playerPos = player.transform.position;

            var views = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            int calmed = 0;
            foreach (var v in views)
            {
                if (v != null && v.PigData != null && v.PigData.IsAlive)
                {
                    v.PigData.Mood = Math.Min(100, v.PigData.Mood + 8);
                    v.CommandMoveTowards(playerPos);
                    calmed++;
                }
            }

            SetStatus($"✔ TIẾNG HUÝT CHĂN ĐÀN (Khoa 18m + Chó Chăn Đàn): Đã thu hút & trấn an {calmed} heo (+8 Tinh thần)!");
            RefreshContent();
        }

        private void OnClickCleanAndTreat()
        {
            if (MobileGameController.Instance == null) return;
            var engine = MobileGameController.Instance.Engine;
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.BacSiThuY);
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.NongCong);
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.VeBinhKhien);

            var (cured, scBonus, _) = engine.PetsAndNpcs.ExecuteDailyNpcShiftOnPigs(engine.Pigs, engine.Farm);
            engine.ExecuteHealthTreatment(HealthTreatmentVerb.XuLyMoiTruong);

            SetStatus($"✔ Đã thuê & chạy ca trực 3 NPC B (Bác Sĩ chữa {cured} heo, Nông Công +{scBonus} SC, Vệ Binh giữ cổng): SC = {engine.Farm.HabitatIndex}/100!");
            RefreshContent();
        }

        private void OnClickToggleOverlays()
        {
            ShowServiceRadiusOverlay = !ShowServiceRadiusOverlay;
            ShowCoolingRadiusOverlay = ShowServiceRadiusOverlay;
            ShowBuildGridOverlay = ShowServiceRadiusOverlay;
            RebuildWorldOverlays();

            SetStatus(ShowServiceRadiusOverlay
                ? "✔ Đã BẬT lớp phủ Bản đồ: Tầm phục vụ 30m (Xanh lá), Nguồn mát 25m (Xanh lam) & Lưới Ô Xây 2×2m (Xanh/Vàng/Đỏ)!"
                : "✔ Đã TẮT lớp phủ Bản đồ.");
        }

        private void RebuildWorldOverlays()
        {
            for (int i = 0; i < worldOverlayObjects.Count; i++)
            {
                if (worldOverlayObjects[i] != null) Destroy(worldOverlayObjects[i]);
            }
            worldOverlayObjects.Clear();

            if (!ShowServiceRadiusOverlay) return;

            CreateRingIndicator("Overlay_ServiceRadius_30m", Vector3.zero, FarmGridAndZones.ServiceRadiusMeters, new Color(0.15f, 0.85f, 0.30f, 0.14f));
            CreateRingIndicator("Overlay_CoolingRadius_25m", new Vector3(-4f, 0f, 0f), FarmGridAndZones.CoolingRadiusMeters, new Color(0.20f, 0.65f, 0.95f, 0.14f));

            var engine = MobileGameController.Instance?.Engine;
            if (ShowBuildGridOverlay && engine?.GridZones != null)
            {
                BuildGridCellPreviewOverlay(engine.GridZones, engine.Clock.IsDuringCombatWave);
            }
        }

        private void BuildGridCellPreviewOverlay(FarmGridAndZones grid, bool isCombatWave)
        {
            // Hiển thị lưới 10×8 Ô Xây (2×2m) quanh trung tâm chuồng bằng màu Xanh / Vàng / Đỏ thực tế từ EvaluatePlacement
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    bool border = x == 0 || y == 0 || x == 15 || y == 15;
                    tex.SetPixel(x, y, border ? new Color(1f, 1f, 1f, 0.85f) : new Color(1f, 1f, 1f, 0.28f));
                }
            }
            tex.Apply();
            var cellSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 8f); // 16px / 8 PPU = 2m x 2m

            for (int cx = 18; cx <= 27; cx++)
            {
                for (int cy = 12; cy <= 19; cy++)
                {
                    var status = grid.EvaluatePlacement(cx, cy, BuildingFootprintSize.Small_1x1, isCombatWave);
                    Color c = status switch
                    {
                        BuildPlacementStatus.Green_Valid => new Color(0.18f, 0.85f, 0.32f, 0.45f),
                        BuildPlacementStatus.Yellow_Warning => new Color(0.95f, 0.78f, 0.15f, 0.55f),
                        _ => new Color(0.92f, 0.20f, 0.20f, 0.55f)
                    };

                    var go = new GameObject($"GridCell_{cx}_{cy}");
                    float wx = grid.OriginX + (cx + 0.5f) * FarmGridAndZones.BuildCellMeters;
                    float wy = grid.OriginY + (cy + 0.5f) * FarmGridAndZones.BuildCellMeters;
                    go.transform.position = new Vector3(wx, wy, 0f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = cellSprite;
                    sr.color = c;
                    sr.sortingLayerName = "Ground";
                    sr.sortingOrder = 3;
                    worldOverlayObjects.Add(go);
                }
            }
        }

        private void CreateRingIndicator(string objName, Vector3 center, float radiusMeters, Color fillColor)
        {
            var go = new GameObject(objName);
            go.transform.position = center;
            go.transform.localScale = new Vector3(radiusMeters * 2f, radiusMeters * 2f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Ground";
            sr.sortingOrder = 2;

            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            float r = size * 0.5f;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= r && dist >= r - 2.5f)
                        tex.SetPixel(x, y, new Color(fillColor.r, fillColor.g, fillColor.b, 0.75f));
                    else if (dist < r - 2.5f)
                        tex.SetPixel(x, y, fillColor);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            worldOverlayObjects.Add(go);
        }
    }
}
