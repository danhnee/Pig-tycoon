using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Read-only farm ledger, intelligence report, and protagonist notebook. Targeted world actions are handled
    /// by their own interaction/station; this overview never grants or fabricates progress.
    /// </summary>
    public class FarmOverviewPopup : MonoBehaviour
    {
        public static FarmOverviewPopup Instance { get; private set; }
        public bool IsOpen => rootModal != null && rootModal.activeSelf;

        public bool ShowServiceRadiusOverlay { get; private set; } = false;
        public bool ShowCoolingRadiusOverlay { get; private set; } = false;
        public bool ShowBuildGridOverlay { get; private set; } = false;

        private GameObject rootModal;
        private TextMeshProUGUI headerTitleText;
        private TextMeshProUGUI bodyReportText;
        private TextMeshProUGUI actionStatusText;
        private int activeTabIndex = 0;
        private bool showExpandedBottleneckAdvice = false;

        public static FarmOverviewPopup Ensure()
        {
            if (Instance != null)
            {
                if (Instance.rootModal == null)
                {
                    var c = VillageBarterPopup.FindScreenSpaceCanvas();
                    if (c != null) Instance.EnsureUIExists(c.transform);
                }
                return Instance;
            }
            var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
            if (canvas == null) return null;
            var popup = canvas.gameObject.AddComponent<FarmOverviewPopup>();
            popup.EnsureUIExists(canvas.transform);
            return popup;
        }

        public void Open()
        {
            if (rootModal == null)
            {
                var c = VillageBarterPopup.FindScreenSpaceCanvas();
                if (c != null) EnsureUIExists(c.transform);
            }
            if (rootModal != null)
            {
                rootModal.SetActive(true);
                rootModal.transform.SetAsLastSibling();
                RefreshContent();
            }
        }

        private readonly List<GameObject> worldOverlayObjects = new List<GameObject>();
        private readonly List<UnityEngine.Object> worldOverlayAssets = new List<UnityEngine.Object>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            ClearWorldOverlays();
            if (Instance == this) Instance = null;
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
            openTxt.text = "SỔ TRẠI & BẦY ĐÀN [F]";
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

            // Nút vào trực tiếp nội thất Nhà Chính / Chuồng Trại [H]
            CreateActionButtonWithRef(parchObj.transform, "VÀO NHÀ CHÍNH / CHUỒNG TRẠI", new Vector2(-264f, 232f), new Vector2(228f, 28f), () =>
            {
                rootModal.SetActive(false);
                BuildingInteriorPopup.Ensure().Open(BuildingInteriorPopup.InteriorRoom.NhaChinh);
            });

            // 5 Nút chuyển Tab
            tabButtonImages.Clear();
            tabButtonTexts.Clear();
            string[] tabNames =
            {
                // short names: the long ones were cut off on both sides of a 146 px tab (probe screenshot 3 Nov)
                "SỨC CHỨA",
                "BẦY ĐÀN",
                "SINH SẢN",
                "Y TẾ · TIN",
                "SỔ TAY"
            };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;
                var (tBtn, tTxt) = CreateActionButtonWithRef(parchObj.transform, tabNames[i], new Vector2(-304f + i * 152f, 200f), new Vector2(146f, 32f), () =>
                {
                    if (activeTabIndex == idx)
                    {
                        showExpandedBottleneckAdvice = !showExpandedBottleneckAdvice;
                        RefreshContent();
                        var eng = MobileGameController.Instance?.Engine;
                        if (eng != null)
                        {
                            int liv = eng.Pigs.FindAll(p => p.IsAlive).Count;
                            int crp = eng.Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
                            var cap = eng.Farm.CalculateCapacityReport(liv, crp);
                            string msg = $"Sức chứa hiện tại: {cap.LivingPigCount}/{cap.EffectiveCapacity} heo — Điểm nghẽn: {cap.Bottleneck}";
                            SetStatus(msg);
                            ToastOverlay.Show(msg);
                        }
                        else
                        {
                            ToastOverlay.Show($"Đang xem thẻ {tabNames[idx]}");
                        }
                    }
                    else
                    {
                        activeTabIndex = idx;
                        showExpandedBottleneckAdvice = false;
                        RefreshContent();
                        ToastOverlay.Show($"Đã chuyển sang thẻ: {tabNames[idx]}");
                    }
                });
                tabButtonImages.Add(tBtn.GetComponent<Image>());
                tabButtonTexts.Add(tTxt);
            }

            // Khung thẻ lót cho nội dung báo cáo chi tiết
            var bodyCardObj = new GameObject("Body_Card", typeof(RectTransform), typeof(Image));
            bodyCardObj.transform.SetParent(parchObj.transform, false);
            var bodyCardRt = (RectTransform)bodyCardObj.transform;
            bodyCardRt.anchorMin = new Vector2(0f, 0f);
            bodyCardRt.anchorMax = new Vector2(1f, 1f);
            bodyCardRt.offsetMin = new Vector2(16f, 102f);
            bodyCardRt.offsetMax = new Vector2(-16f, -80f);
            var bodyCardImg = bodyCardObj.GetComponent<Image>();
            bodyCardImg.sprite = UISpriteLoader.GetFrameParchment();
            bodyCardImg.type = Image.Type.Sliced;
            bodyCardImg.color = new Color(1f, 0.97f, 0.90f, 0.94f);
            bodyCardImg.raycastTarget = false;

            // Nội dung báo cáo chi tiết
            var bodyObj = new GameObject("Body_Report");
            bodyObj.transform.SetParent(parchObj.transform, false);
            var bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(28f, 110f);
            bodyRt.offsetMax = new Vector2(-28f, -88f);
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
            actionStatusText.fontSize = 13.5f;
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

        private readonly List<Image> tabButtonImages = new List<Image>();
        private readonly List<TextMeshProUGUI> tabButtonTexts = new List<TextMeshProUGUI>();
        private readonly List<Button> bottomButtons = new List<Button>();
        private readonly List<TextMeshProUGUI> bottomButtonTexts = new List<TextMeshProUGUI>();
        private int notebookPage;

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
            tmp.fontSize = 11f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.24f, 0.11f, 0.03f);
            tmp.raycastTarget = false;
            return (btn, tmp);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F)) // Tab belongs to the tool wheel (ADR-0012)
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
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null || bodyReportText == null) return;
            for (int i = 0; i < tabButtonImages.Count; i++)
            {
                bool active = i == activeTabIndex;
                if (tabButtonImages[i] != null)
                {
                    tabButtonImages[i].sprite = active ? UISpriteLoader.GetFrameSlotSelected() : UISpriteLoader.GetFrameSlotNormal();
                    tabButtonImages[i].color = active ? new Color(1f, 0.90f, 0.56f, 1f) : new Color(0.88f, 0.78f, 0.62f, 0.92f);
                }
                if (i < tabButtonTexts.Count && tabButtonTexts[i] != null)
                {
                    tabButtonTexts[i].color = active ? new Color(0.28f, 0.10f, 0.02f) : new Color(0.32f, 0.18f, 0.08f);
                }
            }
            SetStatus(string.Empty);
            var living = engine.Pigs.FindAll(pig => pig.IsAlive);
            var report = new StringBuilder();
            UpdateCapacityBarsVisual(engine, activeTabIndex == 0);
            if (activeTabIndex == 0)
            {
                headerTitleText.text = "SỨC CHỨA TRẠI";
                var farm = engine.Farm;
                var inf = farm.Infrastructure;
                int plotLimit = FarmExpansion.PlotLimit(farm.FarmLevel);
                int roofLimit = inf.LandPlots * FarmExpansion.RoofsPerPlot;
                SetBottomButtonLabels(
                    "VÙNG PHỤC VỤ & MÁT",
                    $"KHAI HOANG · {FarmExpansion.PlotGold}G·{FarmExpansion.PlotStamina:0}TL ({inf.LandPlots}/{plotLimit})",
                    $"DỰNG MÁI · {FarmExpansion.RoofGold}G+{FarmExpansion.RoofWood} gỗ ({inf.Shelters}/{roofLimit})",
                    $"DỌN BÃI ({farm.CleaningsToday}/{HerdLifecycleRules.MaxCountedCleaningsPerDay})");
                int corpses = engine.Pigs.FindAll(pig => !pig.IsAlive && !pig.IsInProcessingLot).Count;
                var capacity = farm.CalculateCapacityReport(living.Count, corpses);
                string nextExp = farm.ExpForNextFarmLevel > 0 ? $"{farm.FarmExp:N0}/{farm.ExpForNextFarmLevel:N0} EXP" : $"{farm.FarmExp:N0} EXP (tối đa)";
                string alskState = farm.EventPressureState switch
                {
                    EventPressureState.YenA => "Yên ả",
                    EventPressureState.GonSong => "Gợn sóng",
                    EventPressureState.BatOn => "Bất ổn",
                    _ => "Rạn nứt"
                };
                report.AppendLine($"<b>Cấp trại {farm.FarmLevel}</b> · {nextExp}  |  <b>Áp lực sự kiện:</b> {farm.EventPressureIndex}/100 ({alskState})");
                report.AppendLine($"<b>Đàn hiện tại:</b> {capacity.LivingPigCount}/{capacity.EffectiveCapacity} con ({capacity.RawDensityRatio * 100f:0.#}% · {capacity.DensityZoneName})  |  <color=#B91C1C><b>Điểm nghẽn: {capacity.Bottleneck}</b></color>");
                if (corpses > 0) report.AppendLine($"<color=#B91C1C>Xác chưa xử lý: {corpses} (-{corpses} sức chứa, Sinh Cảnh -3/xác/ngày) — bấm E vào xác để xử lý</color>");
                report.AppendLine($"<b>Sinh cảnh:</b> {farm.HabitatIndex}/100 ({farm.HabitatTier}) · Đã dọn bãi hôm nay {farm.CleaningsToday} lần · <b>Kho cám:</b> {farm.FeedStockKg:0.#} kg");
                if (showExpandedBottleneckAdvice)
                    report.AppendLine("<color=#15803D><i>• Gợi ý mở rộng: Nâng cấp ô đất / mái che bên dưới hoặc bấm [VÀO NHÀ CHÍNH / CHUỒNG TRẠI] để vào bên trong kiểm tra máng cám, nước và ổ đẻ.</i></color>");
                SyncWorldShelters(engine);
            }
            else if (activeTabIndex == 1)
            {
                headerTitleText.text = "BẦY ĐÀN & TRUYỀN THỪA THỦ LĨNH";
                SetBottomButtonLabels("THỦ LĨNH HIỆN TẠI", "ĐÀN ĐANG BẤT AN", "ỨNG VIÊN KẾ VỊ", "TIẾN ĐỘ BỒI DƯỠNG");
                var herd = engine.Herd;
                report.AppendLine($"<b>Trạng thái Đàn:</b> {herd.Tier} · Duy trì {herd.ConsecutiveDaysInState} ngày liên tiếp");
                report.AppendLine($"<b>Tầng Truyền Thừa:</b> {herd.TransmissionStacks}/3 tầng");
                report.AppendLine(herd.Leader == null ? "<b>Thủ lĩnh:</b> Chưa có thủ lĩnh" : $"<b>Thủ lĩnh:</b> {herd.Leader.Name} · Tại vị {herd.Leader.DaysAsLeader} ngày");
                report.AppendLine($"<b>Heo gắn kết cao (>=50):</b> {living.FindAll(pig => pig.Bonding >= 50).Count} con  |  <b>Heo bất an (<60):</b> {living.FindAll(pig => pig.Mood < 60).Count} con");
                report.AppendLine($"<b>Ứng viên kế vị:</b> {(string.IsNullOrEmpty(herd.SuccessionTraining.CandidatePigId) ? "Chưa chọn" : herd.SuccessionTraining.CandidatePigId)} ({herd.SuccessionTraining.ConsecutiveCareDays} ngày chăm)");
            }
            else if (activeTabIndex == 2)
            {
                headerTitleText.text = "SINH SẢN, HUYẾT THỐNG & XỬ LÝ XÁC";
                SetBottomButtonLabels("CẶP PHỐI KHẢ DỤNG", "HEO ĐANG MANG THAI", "HEO GIÀ / HẾT LỨA", "VIỆC XỬ LÝ XÁC");
                report.AppendLine($"<b>Heo Trưởng Thành:</b> {living.FindAll(pig => pig.Stage == PigStage.TruongThanh).Count} con  |  <b>Heo Già:</b> {living.FindAll(pig => pig.Stage == PigStage.HeoGia).Count} con");
                report.AppendLine($"<b>Đang mang thai:</b> {living.FindAll(pig => pig.GestationDaysRemaining > 0).Count} con (Chu kỳ thai {BreedingLedger.GestationDays} ngày · Hồi sức sau đẻ {BreedingLedger.RestDays} ngày)");
                report.AppendLine($"<b>Giới hạn sinh sản:</b> Tối đa {BreedingLedger.MaxLitters} lứa/mẹ · Thế hệ F1 cộng thưởng +8%");
                report.AppendLine($"<b>Công việc xử lý xác đang chạy:</b> {engine.GridZones.ActiveCarcassJobs.FindAll(job => !job.Completed).Count} việc (Chôn lấp / Ủ phân / Thiêu hủy)");
            }
            else if (activeTabIndex == 3)
            {
                headerTitleText.text = "Y TẾ, NHÂN SỰ & BÀN TÌNH BÁO ĐỢT QUÁI";
                SetBottomButtonLabels("HEO ĐANG BỆNH", "THÚ & NHÂN SỰ", "BÀN TÌNH BÁO", "BÁO CÁO TRẬN ĐÁNH");
                report.AppendLine($"<b>Đang bệnh:</b> {living.FindAll(pig => pig.ActiveDisease.HasValue).Count} con  |  <b>Đang cách ly:</b> {living.FindAll(pig => pig.IsIsolated).Count} con");
                report.AppendLine($"<b>Linh thú đi cùng:</b> {engine.PetsAndNpcs.ActivePets.Count}  |  <b>Nhân sự đã thuê:</b> {engine.PetsAndNpcs.HiredNpcs.Count}");
                report.AppendLine($"<b>Bệnh đã nghiên cứu:</b> {engine.PetsAndNpcs.ResearchedDiseases.Count}/5 loại bệnh");
                report.AppendLine($"<b>Cấp Bàn Tình Báo:</b> {engine.DeepMechanics.CurrentIntelTier}  |  <b>Số báo cáo sau đợt quái:</b> {engine.DeepMechanics.BattleReports.Count} bản");
                bool hasDesk = engine.Defense.Buildings.Exists(b => b.BlueprintId == IntelBureau.DeskBlueprintId);
                int qToday = engine.Intel.QuestionsDay == engine.Clock.CurrentDay ? engine.Intel.QuestionsToday : 0;
                var intelNow = engine.Intel.BuildReport(engine);
                report.AppendLine($"<b>Lượt hỏi hôm nay:</b> {qToday}/{IntelBureau.QuestionsPerDay}  |  " +
                    (hasDesk ? $"<color=#15803D>Đã xây Bàn Tình Báo</color>" : "<color=#B91C1C>Cần xây Bàn Tình Báo (bản vẽ Tinh · chợ đêm)</color>"));
                var minorNow = WaveForecast.Minor(engine);
                report.AppendLine($"\n<color=#78350F><b>[Tình báo chu kỳ {intelNow.CycleNumber} · cấp {intelNow.Level}]</b> {WaveForecast.Major(engine).Text}{(minorNow.known ? " · " + minorNow.text : "")}</color>");
                foreach (var line in intelNow.AllLines.Take(3)) report.AppendLine($"<color=#78350F>• {line}</color>");
            }
            else
            {
                headerTitleText.text = engine.Character.Identity == PlayerId.An ? "SỔ TAY CỦA AN" : "SỔ HAI CỘT CỦA KHOA";
                SetBottomButtonLabels("NHIỆM VỤ ĐANG MỞ", "TRANG SỔ TAY TIẾP", "TRANG BỊ ĐANG MẶC", "THỂ TRẠNG HIỆN TẠI");
                report.AppendLine($"<b>Chương hiện tại:</b> Chương {(int)engine.Story.CurrentChapter}");
                report.AppendLine($"<b>Nhiệm vụ đang mở:</b> {(engine.Quests.ActiveMain != null ? 1 : 0) + engine.Quests.OpenSide(engine).Count()} (chính: {engine.Quests.ActiveMain?.Title ?? "đã xong"})");
                report.AppendLine($"<b>Ghi nhận đã xác thực:</b> {engine.Story.NotebookEntries.FindAll(entry => entry.IsVerifiedToRightColumn).Count}");
                report.AppendLine($"<b>Giấy đã chọn:</b> {engine.DeepMechanics.EquippedPaperColor} +{engine.DeepMechanics.intEquippedPaperEnhancementLevel}");
            }
            bodyReportText.text = report.ToString();
        }

        // The overview reads the real session. World actions belong to their target or station;
        // this panel must never spawn animals, grant items, force events or complete quests.
        // Exception: "Dọn bãi" (GDD 6.2) is yard-wide work with no single target; it costs real stamina (GDD 9.2).
        private void OnClickContextualBottomButton(int slot)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null || slot < 0 || slot > 3) return;
            var report = new StringBuilder();
            if (activeTabIndex == 0)
            {
                if (slot == 0) { ShowServiceRadiusOverlay = !ShowServiceRadiusOverlay; ShowCoolingRadiusOverlay = ShowServiceRadiusOverlay; }
                else if (slot == 1 || slot == 2)
                {
                    var (_, msg) = slot == 1 ? FarmExpansion.ExpandLandPlot(engine) : FarmExpansion.BuildShelterRoof(engine);
                    RefreshContent();
                    SetStatus(msg);
                    PlayerInteractionController.Instance?.ShowFeedback(msg);
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    return;
                }
                else
                {
                    var (_, cleanMsg) = HerdLifecycleRules.TryCleanYard(engine);
                    RefreshContent();
                    SetStatus(cleanMsg);
                    MobileGameController.Instance?.OnStateUpdated?.Invoke();
                    return;
                }
                RebuildWorldOverlays();
                SetStatus("Lớp phủ từ vị trí công trình đã đặt.");
                return;
            }
            if (activeTabIndex == 1)
            {
                if (slot == 3)
                {
                    var training = engine.Herd.SuccessionTraining;
                    report.AppendLine($"Ứng viên: {training.CandidatePigId}");
                    report.AppendLine($"Ngày chăm liên tục: {training.ConsecutiveCareDays}");
                    report.AppendLine($"Kế vị +{training.BonusSuccessionPercentage}% · Truyền thừa +{training.BonusTransmissionPercentage}%");
                }
                else
                {
                    foreach (var pig in engine.Pigs)
                    {
                        if (!pig.IsAlive) continue;
                        bool include = slot == 0 ? engine.Herd.Leader?.PigId == pig.Id : slot == 1 ? pig.Mood < 60 :
                            (pig.Stage == PigStage.TruongThanh || pig.Stage == PigStage.HeoGia) && pig.Bonding >= 75f;
                        if (include) report.AppendLine($"{pig.Name} · {pig.Id} · Tinh thần {pig.Mood:0} · Hòa nhập {pig.Bonding:0}");
                    }
                }
            }
            else if (activeTabIndex == 2)
            {
                if (slot == 0)
                {
                    report.AppendLine("<b>DỰ BÁO CẶP PHỐI GIỐNG KHẢ DỤNG:</b>");
                    foreach (var pig in engine.Pigs)
                    {
                        if (!pig.IsAlive || pig.Gender != "Cai") continue;
                        var partners = engine.ListBreedingPartners(pig.Id);
                        foreach (var father in partners)
                        {
                            var prev = engine.PreviewPigBreeding(pig.Id, father.Id);
                            report.AppendLine($"• Mẹ <b>{pig.Name}</b> ({PigText.Line(pig)}) × Cha <b>{father.Name}</b> ({PigText.Line(father)}) -> {prev.message} ({prev.chance * 100f:0}%)");
                        }
                    }
                }
                else if (slot == 1)
                {
                    foreach (var pig in engine.Pigs)
                        if (pig.IsAlive && pig.GestationDaysRemaining > 0)
                            report.AppendLine($"{pig.Name} ({PigText.Line(pig)}) · Còn {pig.GestationDaysRemaining} ngày đẻ · Lứa {pig.LittersBorn + 1}/{BreedingLedger.MaxLitters}");
                }
                else if (slot == 2)
                {
                    foreach (var pig in engine.Pigs)
                        if (pig.IsAlive && (pig.Stage == PigStage.HeoGia || pig.LittersBorn >= BreedingLedger.MaxLitters))
                            report.AppendLine($"{pig.Name} · {PigText.Stage(pig.Stage)} · Đã đẻ {pig.LittersBorn}/{BreedingLedger.MaxLitters} lứa · {pig.WeightKg:0.#} kg");
                }
                else
                {
                    foreach (var job in engine.GridZones.ActiveCarcassJobs)
                        report.AppendLine($"{job.PigId} · {job.Method} · {job.HoursRemaining} giờ · {(job.Completed ? "Hoàn tất" : "Đang xử lý")}");
                }
            }
            else if (activeTabIndex == 3)
            {
                if (slot == 0)
                    foreach (var pig in engine.Pigs)
                        if (pig.IsAlive && pig.ActiveDisease.HasValue)
                            report.AppendLine($"{pig.Name} · {pig.ActiveDisease} · {(pig.IsIsolated ? "Cách ly" : "Trong đàn")}");
                if (slot == 1)
                {
                    foreach (var pet in engine.PetsAndNpcs.ActivePets)
                        report.AppendLine($"[Linh Thú] {pet.Name} · No bụng {pet.StaminaOrSatiety}/100");
                    foreach (var npc in engine.PetsAndNpcs.HiredNpcs)
                        report.AppendLine($"[Nhân Sự] {npc.Name} · {npc.DailyWageGold}G/ngày · {(npc.IsActive ? "Đang làm" : "Nghỉ")}");
                }
                if (slot == 2)
                {
                    var intel = engine.Intel.BuildReport(engine);
                    var minorTip = WaveForecast.Minor(engine);
                    report.AppendLine($"<b>BÀN TÌNH BÁO (cấp {intel.Level}, chu kỳ {intel.CycleNumber}) · {WaveForecast.Major(engine).Text}{(minorTip.known ? " · " + minorTip.text : "")}</b>");
                    if (intel.Level == 0) report.AppendLine("Chưa mua tin tình báo chu kỳ này — mua ở Bàn Tình Báo chợ thị trấn.");
                    foreach (var line in intel.Warnings) report.AppendLine($"<color=#B91C1C>! {line}</color>");
                    foreach (var line in intel.Confirmed) report.AppendLine($"• {line}");
                    foreach (var line in intel.Speculation) report.AppendLine($"<color=#6B7280>? {line}</color>");
                }
                if (slot == 3)
                {
                    report.AppendLine("<b>BÁO CÁO SAU ĐỢT QUÁI:</b>");
                    foreach (var br in engine.DeepMechanics.BattleReports)
                        report.AppendLine($"• Đợt {br.WaveNumber}: {(br.Victory ? "Thắng" : "Thua")} · Hạ {br.EnemiesDefeated} quái | Công trình -{br.BuildingDurabilityLost} bền | Mất {br.PigsLostOrAbducted} heo | Đạn {br.AmmoSpent} | Vỡ Chống Chịu {br.PlayerGuardBreaks} lần | +{br.GoldRewarded}G");
                }
            }
            else if (slot == 0)
            {
                var main = engine.Quests.ActiveMain;
                if (main != null) report.AppendLine($"<b>{main.Id} · {main.Title}</b> (chính)\n");
                foreach (var quest in engine.Quests.OpenSide(engine)) report.AppendLine($"{quest.Id} · {quest.Title} (phụ)\n");
            }
            else if (slot == 1)
            {
                if (engine.Character.Identity == PlayerId.An)
                {
                    var earned = engine.Story.AnPinkNotebookEntries.FindAll(entry =>
                        engine.Quests.Completed.Any(id => entry.QuestOrSceneRef.EndsWith(id, StringComparison.Ordinal)));
                    if (earned.Count > 0)
                    {
                        var entry = earned[notebookPage++ % earned.Count];
                        report.AppendLine(entry.RecipientSalutation).AppendLine(entry.LetterBody).AppendLine(entry.PencilMarginHypothesis);
                    }
                }
                else if (engine.Story.NotebookEntries.Count > 0)
                {
                    var entry = engine.Story.NotebookEntries[notebookPage++ % engine.Story.NotebookEntries.Count];
                    report.AppendLine(entry.Title).AppendLine("Thấy: " + entry.LeftColumn_Thay);
                    if (entry.IsVerifiedToRightColumn) report.AppendLine("Có thật: " + entry.RightColumn_CoThat);
                    report.AppendLine(entry.PencilMargin_SuyDoan);
                }
            }
            else if (slot == 2)
            {
                foreach (var equipment in engine.Character.Equipment)
                    if (equipment.Value != null) report.AppendLine($"{equipment.Key} · {equipment.Value.Name}");
                foreach (var seal in engine.Character.EngravedSeals) report.AppendLine(seal);
            }
            else
            {
                var stamina = engine.Character.Stamina;
                report.AppendLine($"Thể trạng {stamina.Condition}/100 · Miễn dịch {stamina.Immunity}/100");
                report.AppendLine($"Thể lực {stamina.CurrentStamina:0}/{engine.Character.GetEffectiveMaxStamina():0}");
                report.AppendLine($"Tải ngày {stamina.DailyLoad:0} · Mệt dội {stamina.ReboundFatigueStacks}");
                report.AppendLine($"Hồi giấc ngủ {engine.Clock.SleepCooldownHoursRemaining:0.#} giờ");
            }
            bodyReportText.text = report.Length == 0 ? "Chưa có ghi nhận trong mục này." : report.ToString();
            SetStatus(string.Empty);
        }


        private GameObject capacityBarsRoot;
        private readonly Image[] capBarFills = new Image[4];
        private readonly TextMeshProUGUI[] capBarLabels = new TextMeshProUGUI[4];

        private void UpdateCapacityBarsVisual(GameEngine engine, bool visible)
        {
            if (bodyReportText == null) return;
            var parent = bodyReportText.transform.parent;
            if (capacityBarsRoot == null && parent != null)
            {
                capacityBarsRoot = new GameObject("CapacityBars_Root", typeof(RectTransform));
                capacityBarsRoot.transform.SetParent(parent, false);
                var rt = (RectTransform)capacityBarsRoot.transform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 112f);
                rt.sizeDelta = new Vector2(-56f, 156f);

                for (int i = 0; i < 4; i++)
                {
                    var row = new GameObject($"CapBar_{i}", typeof(RectTransform), typeof(Image));
                    row.transform.SetParent(capacityBarsRoot.transform, false);
                    var rrt = (RectTransform)row.transform;
                    rrt.anchorMin = new Vector2(0f, 1f);
                    rrt.anchorMax = new Vector2(1f, 1f);
                    rrt.pivot = new Vector2(0.5f, 1f);
                    rrt.anchoredPosition = new Vector2(0f, -i * 38f);
                    rrt.sizeDelta = new Vector2(0f, 32f);
                    var bg = row.GetComponent<Image>();
                    bg.color = new Color(0.18f, 0.14f, 0.10f, 0.88f);
                    bg.raycastTarget = false;

                    var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                    fill.transform.SetParent(row.transform, false);
                    var frt = (RectTransform)fill.transform;
                    frt.anchorMin = Vector2.zero;
                    frt.anchorMax = new Vector2(0.5f, 1f);
                    frt.offsetMin = new Vector2(2f, 2f);
                    frt.offsetMax = new Vector2(-2f, -2f);
                    capBarFills[i] = fill.GetComponent<Image>();
                    capBarFills[i].raycastTarget = false;

                    var lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    lbl.transform.SetParent(row.transform, false);
                    var lrt = (RectTransform)lbl.transform;
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = new Vector2(10f, 0f);
                    lrt.offsetMax = new Vector2(-10f, 0f);
                    capBarLabels[i] = lbl.GetComponent<TextMeshProUGUI>();
                    capBarLabels[i].fontSize = 13f;
                    capBarLabels[i].fontStyle = FontStyles.Bold;
                    capBarLabels[i].color = Color.white;
                    capBarLabels[i].alignment = TextAlignmentOptions.MidlineLeft;
                    capBarLabels[i].raycastTarget = false;
                }
            }
            if (capacityBarsRoot == null) return;
            capacityBarsRoot.SetActive(visible);
            if (!visible || engine == null) return;

            var living = engine.Pigs.FindAll(p => p.IsAlive);
            int corpses = engine.Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
            var cap = engine.Farm.CalculateCapacityReport(living.Count, corpses);
            var inf = engine.Farm.Infrastructure;
            int plotLimit = FarmExpansion.PlotLimit(engine.Farm.FarmLevel);
            int roofLimit = inf.LandPlots * FarmExpansion.RoofsPerPlot;
            int minCap = Mathf.Min(Mathf.Min(cap.CapacityByLand, cap.CapacityByShelters), Mathf.Min(cap.CapacityByWater, cap.CapacityByFeeders));
            float maxRef = Mathf.Max(120f, Mathf.Max(Mathf.Max(cap.CapacityByLand, cap.CapacityByShelters), Mathf.Max(cap.CapacityByWater, cap.CapacityByFeeders)));

            string tempShadeTag = engine.Farm.TempShadePigsToday > 0
                ? $" (+{engine.Farm.TempShadePigsToday} chỗ che tạm)"
                : "";
            (string title, int val, string detail)[] items =
            {
                ("Ô Đất (×8)", cap.CapacityByLand, $"{inf.LandPlots}/{plotLimit} ô (tối đa {FarmExpansion.MaxPlots})"),
                ("Mái Trú (×6)", cap.CapacityByShelters, $"{inf.Shelters}/{roofLimit} mái{tempShadeTag}"),
                ("Bồn Nước (×12)", cap.CapacityByWater, $"{inf.WaterTroughs} bồn"),
                ("Máng Ăn (×10)", cap.CapacityByFeeders, $"{inf.Feeders} máng")
            };

            for (int i = 0; i < 4; i++)
            {
                bool isBottleneck = items[i].val == minCap;
                float ratio = Mathf.Clamp01(items[i].val / maxRef);
                var frt = (RectTransform)capBarFills[i].transform;
                frt.anchorMax = new Vector2(Mathf.Max(0.06f, ratio), 1f);
                capBarFills[i].color = isBottleneck ? new Color(0.84f, 0.26f, 0.20f, 0.95f) : new Color(0.22f, 0.62f, 0.45f, 0.92f);
                string badge = isBottleneck ? "  <color=#FDE047>[ĐIỂM NGHẼN]</color>" : "";
                capBarLabels[i].text = $"{items[i].title}: <b>{items[i].val} chỗ</b> · {items[i].detail}{badge}";
            }
        }

        private readonly List<GameObject> spawnedExtraShelters = new List<GameObject>();

        private void SyncWorldShelters(GameEngine engine)
        {
            if (engine == null) return;
            int extraRoofs = Mathf.Max(0, engine.Farm.Infrastructure.Shelters - 14);
            var existingViews = FindObjectsByType<Shelter2DView>(FindObjectsSortMode.None);
            if (existingViews == null || existingViews.Length == 0) return;

            while (spawnedExtraShelters.Count < extraRoofs)
            {
                int idx = spawnedExtraShelters.Count;
                var template = existingViews[0];
                var clone = Instantiate(template.gameObject, template.transform.parent);
                clone.name = $"ShelterRoof_Expanded_{idx + 1}";
                clone.transform.position = template.transform.position + new Vector3(3.4f * (idx + 1), 0f, 0f);
                spawnedExtraShelters.Add(clone);
            }
        }

        private void SetStatus(string text)
        {
            if (actionStatusText != null)
            {
                actionStatusText.text = text;
            }
        }

        private void RebuildWorldOverlays()
        {
            ClearWorldOverlays();

            var engine = MobileGameController.Instance?.Engine;
            if (engine?.GridZones == null) return;
            foreach (var structure in engine.GridZones.Structures)
            {
                var center = structure.WorldCenter(engine.GridZones.OriginX, engine.GridZones.OriginY);
                var position = new Vector3(center.wx, center.wy, 0f);
                if (ShowServiceRadiusOverlay && (structure.ProvidesServiceFeed || structure.ProvidesServiceWater || structure.ProvidesServiceShelter))
                    CreateRingIndicator("Service_" + structure.InstanceId, position, FarmGridAndZones.ServiceRadiusMeters, new Color(0.15f, 0.85f, 0.30f, 0.14f));
                if (ShowCoolingRadiusOverlay && structure.ProvidesCooling)
                    CreateRingIndicator("Cooling_" + structure.InstanceId, position, FarmGridAndZones.CoolingRadiusMeters, new Color(0.20f, 0.65f, 0.95f, 0.14f));
            }
            if (ShowCoolingRadiusOverlay && engine.GridZones.TempShades != null)
            {
                for (int i = 0; i < engine.GridZones.TempShades.Count; i++)
                {
                    var (tx, ty) = engine.GridZones.TempShades[i];
                    CreateRingIndicator($"TempShadeCooling_{i}", new Vector3(tx, ty, 0f), FarmGridAndZones.TempShadeRadiusMeters, new Color(0.20f, 0.82f, 0.72f, 0.18f));
                }
            }
            if (ShowBuildGridOverlay && engine?.GridZones != null)
            {
                BuildGridCellPreviewOverlay(engine.GridZones, engine.Clock.IsDuringCombatWave);
            }
        }

        private void ClearWorldOverlays()
        {
            foreach (var overlay in worldOverlayObjects) if (overlay != null) Destroy(overlay);
            worldOverlayObjects.Clear();
            foreach (var asset in worldOverlayAssets) if (asset != null) Destroy(asset);
            worldOverlayAssets.Clear();
        }

        private void BuildGridCellPreviewOverlay(FarmGridAndZones grid, bool isCombatWave)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            worldOverlayAssets.Add(tex);
            tex.filterMode = FilterMode.Point;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    bool border = x < 4 || y < 4 || x >= size - 4 || y >= size - 4;
                    tex.SetPixel(x, y, border ? new Color(1f, 1f, 1f, 0.85f) : new Color(1f, 1f, 1f, 0.28f));
                }
            }
            tex.Apply();
            var cellSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            worldOverlayAssets.Add(cellSprite);

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
            go.transform.localScale = new Vector3(radiusMeters, radiusMeters, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Ground";
            sr.sortingOrder = 2;

            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            worldOverlayAssets.Add(tex);
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
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            worldOverlayAssets.Add(sr.sprite);
            worldOverlayObjects.Add(go);
        }
    }
}
