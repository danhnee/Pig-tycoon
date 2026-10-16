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

            // 4 Nút chuyển Tab
            string[] tabNames =
            {
                "1. SỨC CHỨA & LỚP PHỦ",
                "2. BẦY ĐÀN & LÙA TRÚ",
                "3. PHỐI GIỐNG & XỬ LÝ",
                "4. Y TẾ, THÚ CƯNG & NPC"
            };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;
                CreateActionButton(parchObj.transform, tabNames[i], new Vector2(-285f + i * 190f, 200f), new Vector2(180f, 32f), () =>
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

            // Hàng nút thao tác nhanh ở đáy bảng
            CreateActionButton(parchObj.transform, "LÙA VỀ NƠI TRÚ", new Vector2(-290f, -215f), new Vector2(180f, 36f), OnClickRecallToRefuge);
            CreateActionButton(parchObj.transform, "TIẾNG HUÝT KHOA", new Vector2(-98f, -215f), new Vector2(180f, 36f), OnClickKhoaWhistle);
            CreateActionButton(parchObj.transform, "DỌN CHUỒNG & TRỊ BỆNH", new Vector2(98f, -215f), new Vector2(180f, 36f), OnClickCleanAndTreat);
            CreateActionButton(parchObj.transform, "BẬT/TẮT LỚP PHỦ 30M", new Vector2(290f, -215f), new Vector2(180f, 36f), OnClickToggleOverlays);

            rootModal.SetActive(false);
        }

        private void CreateActionButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size, Action onClick)
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
            tmp.fontSize = 12.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.24f, 0.11f, 0.03f);
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
                var checklist = engine.GridZones.ValidateChecklistGdd310();
                bodyReportText.text =
                    $"<b>1. Sức Chứa Chuồng Theo Công Thức MIN(4 Nguồn) — GDD 3.2:</b>\n" +
                    $"   • Ô Đất ({farm.Infrastructure.LandPlots} × 8) = <b>{cap.CapacityByLand}</b> chỗ   |   Mái Trú ({farm.Infrastructure.Shelters} × 6) = <b>{cap.CapacityByShelters}</b> chỗ\n" +
                    $"   • Bồn Nước ({farm.Infrastructure.WaterTroughs} × 12) = <b>{cap.CapacityByWater}</b> chỗ   |   Máng Ăn ({farm.Infrastructure.Feeders} × 10) = <b>{cap.CapacityByFeeders}</b> chỗ\n" +
                    $"   • <color=#B45309><b>Điểm Nghẽn Hiện Tại: [{cap.Bottleneck}] → Sức Chứa Hiệu Dụng: {cap.EffectiveCapacity} con</b></color>\n" +
                    $"   • Mật Độ Đàn: <b>{cap.LivingPigCount}/{cap.EffectiveCapacity} ({cap.RawDensityRatio * 100f:0.0}%)</b> — Vùng: <b>{cap.DensityZoneName}</b> (Hệ số bệnh ×{cap.DiseaseDensityMultiplier:0.00})\n\n" +
                    $"<b>2. Chỉ Số Môi Trường & Huyền Bí (GDD 6.1 & 23.1):</b>\n" +
                    $"   • Chỉ số Sinh Cảnh (SC): <color=#15803D><b>{farm.HabitatIndex}/100 [{farm.HabitatTier}]</b></color>   |   Áp Lực Sự Kiện (ÁLSK): <color=#7E22CE><b>{farm.EventPressureIndex}/100 [{farm.EventPressureState}]</b></color>\n" +
                    $"   • Tầm Phục Vụ 30m & Nguồn Mát 25m (Lam Khê 20m): <b>Đang phủ toàn bộ chuồng trung tâm</b>\n" +
                    $"   • Nghiệm thu GDD 3.10 (Nhóm A–H): <color=#15803D><b>{(checklist.AllPassed ? "ĐẠT 8/8 NHÓM CHUẨN" : "ĐANG KIỂM TRA")}</b></color>";
            }
            else if (activeTabIndex == 1)
            {
                headerTitleText.text = "PHÂN KHU 2: TRẠNG THÁI BẦY ĐÀN, THỦ LĨNH, KẾ VỊ & LÙA TRÚ";
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
                    $"   • Ứng viên Kế Vị: <b>{(string.IsNullOrEmpty(herd.SuccessionTraining.CandidatePigId) ? "Chưa chọn" : herd.SuccessionTraining.CandidatePigId)}</b> (+{herd.SuccessionTraining.BonusSuccessionPercentage}% kế vị, +{herd.SuccessionTraining.BonusTransmissionPercentage}% Truyền Thừa)\n" +
                    $"   • Nơi Trú Đàn: <b>{engine.GridZones.HerdRefugePlotCount * 8} chỗ</b> ({engine.GridZones.HerdRefugeEntranceCount} cổng rộng {engine.GridZones.HerdRefugeMinEntranceWidthMeters:0.1}m — Gom ≥90% đàn trong ≤2 giờ game)";
            }
            else if (activeTabIndex == 2)
            {
                headerTitleText.text = "PHÂN KHU 3: PHỐI GIỐNG F1/F2, HẬU DUỆ HEO GIÀ & KHU XỬ LÝ XÁC";
                bodyReportText.text =
                    $"<b>1. Quy Tắc Phối Giống & Cửa Sổ Định Hình (GDD 4.4, 4.7, 4.8):</b>\n" +
                    $"   • Tuổi phối tối thiểu: <b>{BreedingLedger.MinAgeDays} ngày</b> | Mang thai: <b>{BreedingLedger.GestationDays} ngày</b> | Nghỉ: <b>{BreedingLedger.RestDays} ngày</b> | Tối đa: <b>{BreedingLedger.MaxLitters} lứa/nái</b>\n" +
                    $"   • Tỷ lệ thụ thai chuẩn: <b>70%</b> (Heo già: <b>38.5%</b>). Heo non mồ côi tử vong <b>12%/ngày</b> (<b>0%</b> nếu có nái <b>Phong Mẫu</b> trong 10m).\n" +
                    $"   • 6 Bước Hậu Duệ Heo Già: Khỏe mạnh K ∈ <b>[22%, 32%]</b>; phần bất thường chia <b>35% Chết yểu / 40% Thể trạng yếu / 25% Gen xấu</b> (phạt 5–20%); <b>2% Ưu Tú → 45% Thức tỉnh gen trội</b>.\n\n" +
                    $"<b>2. Khu Xử Lý Xác ({engine.GridZones.ProcessingLotSlots} ô tạm, cách nguồn nước ≥20m) & Khu Cách Ly ({engine.GridZones.QuarantinePlotCount} Ô Đất):</b>\n" +
                    $"   • Kéo xác: <b>+1 STA / 10m</b>   |   Thiêu Hủy: <b>45 Vàng + 8L nhiên liệu (6 giờ)</b>   |   Tái Chế Sinh Học: <b>80 Vàng + 10L (18 giờ, thu 25–32% cám)</b>\n" +
                    $"   • Sổ Tay 12 Dòng Gen: <b>Hồng Điền, Lam Khê, Mộc Cước, Thổ Trầm, Xích Mao, Phong Mẫu, Thiết Bì, Tinh Quang, Lôi Mạch, Mộng Nhãn, Kim Thọ, Hư Thể</b> ({GeneticsAndBreedingSystem.AllTraitDefinitions.Count} tính trạng phụ & dị biến).";
            }
            else
            {
                headerTitleText.text = "PHÂN KHU 4: Y TẾ 5 ĐỘNG TỪ, THÚ CƯNG, NPC & 4 SỰ KIỆN HUYỀN BÍ (B)";
                var petBonuses = engine.PetsAndNpcs.GetActivePetBonuses();
                bodyReportText.text =
                    $"<b>1. 10 Mối Đe Dọa Sức Khỏe & 5 Động Từ Xử Lý (GDD 6.4–6.5):</b>\n" +
                    $"   • Nhóm A (Lây): Hô Hấp Lạnh, Ghẻ Ký Sinh, Dịch Tả Heo | Nhóm B (Vệ sinh): Sốt Bùn, Chuồng Nhiễm\n" +
                    $"   • Nhóm C (Thời tiết/Sinh lý): Say Nắng, Rối Loạn Sinh Sản | Nhóm D (Dị tượng): Nấm Phát Quang, Lôi Tâm, Hư Mạch\n" +
                    $"   • 5 Động Từ: <b>[1] Cách Ly  •  [2] Xử Lý Môi Trường  •  [3] Điều Trị Cá Thể  •  [4] Phòng Ngừa Đàn  •  [5] Nghiên Cứu</b>\n\n" +
                    $"<b>2. Thú Cưng Đi Kèm ({engine.PetsAndNpcs.ActivePets.Count}/{PetNpcAndOccultManager.MaxActivePets}) & NPC Của Người B:</b>\n" +
                    $"   • Thú Cưng: <b>Chó Chăn Đàn</b> (+20% tốc độ lùa đàn) & <b>Chó Ngao Canh</b> (chặn trộm đêm) — Tốc độ lùa hiện tại: <b>×{petBonuses.recallSpeedMultiplier:0.00}</b>\n" +
                    $"   • 3 NPC Chủ Lực B: <b>Bác Sĩ Thú Y</b> (chữa 2 heo/ngày) • <b>Nông Công</b> (+8 SC/ngày) • <b>Vệ Binh Khiên</b> (giữ cổng -40% sát thương)\n" +
                    $"   • 4 Sự Kiện Huyền Bí B: <b>Vườn Mười Hai Quả • Đồng Cỏ Không Bóng • Bí Nhân Mua Già • Đổi Một Lấy Một</b>";
            }
        }

        private void OnClickRecallToRefuge()
        {
            if (MobileGameController.Instance == null) return;
            var engine = MobileGameController.Instance.Engine;
            var views = FindObjectsByType<PigAgentView>(FindObjectsSortMode.None);
            var agents = new List<PigSteeringAgent>();
            foreach (var v in views)
            {
                if (v != null && v.PigData != null && v.PigData.IsAlive)
                {
                    agents.Add(new PigSteeringAgent
                    {
                        PigId = v.PigData.Id,
                        X = v.transform.position.x,
                        Y = v.transform.position.y,
                        GeneLine = v.PigData.GeneLine,
                        Mood = v.PigData.Mood
                    });
                }
            }

            var res = engine.FlowField.ExecuteRecallToRefuge(agents, engine.GridZones.HerdRefugeCenter, 2, 6.2f, hasShepherdDog: true);
            foreach (var v in views)
            {
                if (v != null) v.CommandRecallToShelter(new Vector3(engine.GridZones.HerdRefugeCenter.x, engine.GridZones.HerdRefugeCenter.y, 0f));
            }

            if (actionStatusText != null)
            {
                actionStatusText.text = $"✔ Đã phát lệnh LÙA VỀ NƠI TRÚ: {res.recalledCount}/{res.eligibleCount} heo ({res.ratioRecalled * 100f:0}% đàn) về chuồng trong {res.estimatedGameHours:0.1} giờ game!";
            }
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

            if (actionStatusText != null)
            {
                actionStatusText.text = $"✔ TIẾNG HUÝT CHĂN ĐÀN (Khoa + Chó Chăn Đàn): Đã thu hút & trấn an {calmed} heo (+8 Tinh thần)!";
            }
            RefreshContent();
        }

        private void OnClickCleanAndTreat()
        {
            if (MobileGameController.Instance == null) return;
            var engine = MobileGameController.Instance.Engine;
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.BacSiThuY);
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.NongCong);
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.VeBinhKhien);

            engine.PetsAndNpcs.ExecuteHealthVerb(HealthTreatmentVerb.XuLyMoiTruong, engine.Farm, null, null, null);
            foreach (var p in engine.Pigs)
            {
                if (p.IsAlive)
                {
                    p.Health = Math.Min(100, p.Health + 15);
                    p.Mood = Math.Min(100, p.Mood + 6);
                }
            }

            if (actionStatusText != null)
            {
                actionStatusText.text = $"✔ Đã thực thi XỬ LÝ MÔI TRƯỜNG & Ca trực NPC (Bác Sĩ Thú Y + Nông Công): Sinh Cảnh = {engine.Farm.HabitatIndex}/100!";
            }
            RefreshContent();
        }

        private void OnClickToggleOverlays()
        {
            ShowServiceRadiusOverlay = !ShowServiceRadiusOverlay;
            ShowCoolingRadiusOverlay = ShowServiceRadiusOverlay;
            ShowBuildGridOverlay = ShowServiceRadiusOverlay;
            RebuildWorldOverlays();

            if (actionStatusText != null)
            {
                actionStatusText.text = ShowServiceRadiusOverlay
                    ? "✔ Đã BẬT lớp phủ Bản đồ: Tầm phục vụ 30m (Xanh lá), Nguồn mát 25m (Xanh lam) & Lưới Ô Xây 2×2m!"
                    : "✔ Đã TẮT lớp phủ Bản đồ.";
            }
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
