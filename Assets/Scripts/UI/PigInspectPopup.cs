using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PigTycoon.Core;

namespace PigTycoon.Presentation
{
    public class PigInspectPopup : MonoBehaviour
    {
        public static PigInspectPopup Instance { get; private set; }

        [Header("Root Panel")]
        public GameObject ContentPanel;

        [Header("Text Fields")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI GeneText;
        public TextMeshProUGUI MoodText;
        public TextMeshProUGUI BondingHeartsText;
        public TextMeshProUGUI WeightText;
        public TextMeshProUGUI StageText;
        public TextMeshProUGUI MeatQualityText;
        public TextMeshProUGUI TraitsText;

        [Header("Visual Avatar")]
        public Image PigAvatarImage;
        public Button CloseButton;

        private Pig currentPig;
        private int selectedPartnerCursor = 0;

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

            Hide();
        }

        private readonly List<Image> heartImages = new List<Image>();
        private GameObject heartsContainer;

        public void Show(Pig pig)
        {
            if (pig == null) return;
            currentPig = pig;

            if (ContentPanel != null)
            {
                ContentPanel.SetActive(true);
                UISpriteLoader.Apply25DWindowHierarchy(ContentPanel.transform);

                // Nâng cấp khung gỗ bên ngoài
                var panelImg = ContentPanel.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.sprite = UISpriteLoader.GetFrameWoodPanel();
                    panelImg.type = Image.Type.Sliced;
                    panelImg.color = Color.white;
                }
                var pOutline = ContentPanel.GetComponent<Outline>();
                if (pOutline != null) pOutline.enabled = false;

                // Nâng cấp nền giấy cuộn bên trong
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
            }

            // Nâng cấp nút đóng cửa sổ
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

            // 1. Tên & Dòng Gen
            if (TitleText != null)
            {
                string deadTag = pig.IsAlive ? "" : (pig.IsInProcessingLot ? " <color=#6B7280>[Đang ở Khu Xử Lý]</color>" : " <color=#991B1B>[XÁC HEO]</color>");
                TitleText.text = $"<b>{pig.Name}</b>{deadTag} <size=70%><color=#8C5E32>(ID: #{pig.Id})</color></size>";
            }
            if (GeneText != null)
            {
                string rColor = pig.Rarity == GeneRarity.HuyenThoai ? "#B45309" : (pig.Rarity == GeneRarity.Hiem ? "#7E22CE" : "#451A03");
                string genderStr = PigText.Gender(pig.Gender);
                string lineName = PigText.Line(pig); // GDD 5.1/5.4: Thường/Khá at once, rarer lines "?" until appraised
                GeneText.text = $"Dòng: <color={rColor}><b>{lineName}</b> [{PigText.Rarity(pig.Rarity)}]</color>  |  Giới tính: <b>{genderStr}</b>";
            }

            // 2. Avatar Heo Pixel Art thật từ spritesheet
            if (PigAvatarImage != null)
            {
                var pigSprite = UISpriteLoader.GetPigAvatar(pig.GeneLine);
                if (pigSprite != null)
                {
                    PigAvatarImage.sprite = pigSprite;
                    PigAvatarImage.color = Color.white;
                    PigAvatarImage.preserveAspect = true;
                }
                var avatarBoxImg = PigAvatarImage.transform.parent?.GetComponent<Image>();
                if (avatarBoxImg != null && avatarBoxImg.gameObject != PigAvatarImage.gameObject)
                {
                    avatarBoxImg.sprite = UISpriteLoader.GetFrameSlotNormal();
                    avatarBoxImg.type = Image.Type.Sliced;
                    avatarBoxImg.color = Color.white;
                }
            }

            // 3. Trái Tim Thân Thiết (Dãy 5 trái tim pixel art Stardew Valley)
            int fullHearts = Mathf.Clamp(Mathf.RoundToInt(pig.Bonding / 20f), 0, 5);
            UpdateBondingHeartsVisual(fullHearts, pig.Bonding);

            // 4. Tâm Trạng
            if (MoodText != null)
            {
                string moodStateName = pig.MoodState switch
                {
                    MoodState.BinhOn => "Bình Ổn",
                    MoodState.BatAn => "Bất An",
                    MoodState.HoangSo => "Hoảng Sợ",
                    _ => "Hoảng Loạn (Húc rào!)"
                };
                string moodColor = pig.MoodState switch
                {
                    MoodState.BinhOn => "#15803D",
                    MoodState.BatAn => "#D97706",
                    _ => "#DC2626"
                };
                MoodText.text = $"Tâm trạng: <color={moodColor}><b>[{moodStateName}]</b></color> ({pig.Mood}/100)";
            }

            // 5. Cân nặng & Giai đoạn
            if (WeightText != null)
            {
                WeightText.text = $"Cân nặng: <b>{pig.WeightKg:0.0} kg</b> <size=85%><color=#78350F>(Tối đa: {pig.MaxWeightKg:0.0} kg)</color></size>";
            }
            if (StageText != null)
            {
                StageText.text = $"Giai đoạn: <b>{PigText.Stage(pig.Stage)}</b> <size=85%><color=#78350F>({pig.AgeDays} ngày tuổi)</color></size>";
            }

            // 6. Phẩm chất thịt, Khẩu phần ăn, Huyết thống & Mang thai (GDD 4.1, 4.2, 4.4, 21.3)
            if (MeatQualityText != null)
            {
                bool hasHauAn = pig.Traits != null && pig.Traits.Exists(t => t.Id == "HauAn" && !t.IsSealed);
                float feedKg = GeneticsAndBreedingSystem.GetDailyFeedKilograms(pig.Stage, pig.GeneLine, hasHauAn);
                string genBadge = pig.Generation == 0 ? "F0" : (pig.Generation == 1 ? "F1 (+8%)" : "F2+");
                if (pig.IsInbred) genBadge += " [Cận huyết]";
                string pregStr = pig.GestationDaysRemaining > 0
                    ? $" | <color=#BE185D><b>Mang thai ({pig.GestationDaysRemaining} ngày)</b></color>"
                    : (pig.Gender == "Cai" ? $" | Lứa: {pig.LittersBorn}/{BreedingLedger.MaxLitters}" : "");
                var engNow = MobileGameController.Instance?.Engine;
                int curDay = engNow?.Clock?.CurrentDay ?? -1;
                string disStr = pig.ActiveDisease == DiseaseId.SayNang
                    ? $" | <color=#DC2626><b>[GỤC SAY NẮNG · {pig.HeatCollapsedHours}/{GameEngine.HeatCollapseDeathHours}h — Dội Xô Nước / Mái Che Tạm!]</b></color>"
                    : pig.ActiveDisease.HasValue
                    ? $" | <color=#DC2626><b>Bệnh: {pig.ActiveDisease.Value}</b></color>"
                    : (pig.ImmuneDays > 0 ? $" | <color=#0284C7>Miễn dịch ({pig.ImmuneDays}d)</color>" : "");
                string heatWarn = (engNow != null && pig.ActiveDisease != DiseaseId.SayNang && engNow.UncooledHeatHours(pig.Id) >= 1f)
                    ? $" | <color=#D97706><b>[Quá nhiệt {engNow.UncooledHeatHours(pig.Id):0.#}h]</b></color>" : "";
                string seizureWarn = pig.SeizurePending
                    ? " | <color=#DC2626><b>[RUN GIẬT LÔI TÂM (10s) — Thuốc An Thần / Mái trú!]</b></color>" : "";
                string feedBuffs = (pig.LinhChiFedDay == curDay ? " | <color=#15803D><b>[Đã ăn Linh Chi hôm nay]</b></color>" : "")
                    + (pig.CalmFeedDay == curDay ? " | <color=#0284C7><b>[Hạ Hỏa]</b></color>" : "");

                MeatQualityText.text =
                    $"Thịt: <color=#D97706><b>Hạng {pig.MeatQuality}</b></color> | Khẩu phần: <b>{feedKg:0.0}kg</b> | Bám đàn: <b>{pig.AdhesionScore:0.0}</b> | Thế hệ: <b>{genBadge}</b>{pregStr}{disStr}{heatWarn}{seizureWarn}{feedBuffs}";
            }

            // 7. Đặc tính Gen (Traits), Phối giống khả dụng & Biện pháp khắc chế sau giám định
            if (TraitsText != null)
            {
                var profile = GeneticsAndBreedingSystem.GetGeneProfile(pig.GeneLine);
                string xmInfo = pig.GeneLine == GeneLineId.XichMao
                    ? (pig.IsXichMaoDomesticated ? " <color=#15803D>[Đã thuần dưỡng húc trộm]</color>" : $" <color=#B45309>[Thuần dưỡng: {pig.XichMaoDomesticationDays}/3 ngày]</color>")
                    : "";
                // GDD 5.4: an unidentified pig is a "?" card: no line, no main trait, no downside
                string tStr = PigText.LineVisible(pig.Rarity, pig.IsIdentified)
                    ? $"Nội tại [{profile.Name}]{xmInfo}: <i>{profile.PassiveSummary}</i>"
                    : "Nội tại: <b>?</b>";
                if (!pig.IsIdentified)
                {
                    tStr += "\n• <color=#B45309><b>[Chưa Giám Định]</b> — Bấm [Giám Định] hoặc [Mắt Nhà Nghề (An)] để mở khóa tính trạng ẩn!</color>";
                }
                else if (pig.Traits != null && pig.Traits.Count > 0)
                {
                    foreach (var t in pig.Traits)
                    {
                        string statusBadge = t.IsSealed ? " <color=#15803D>[ĐÃ PHONG ẤN / KHẮC CHẾ]</color>" :
                                            (t.IsAwakened ? " <color=#DC2626>[ĐÃ THỨC TỈNH]</color>" : "");
                        tStr += $"\n• <b>{t.Name}</b>{statusBadge}: {t.Description}";
                    }
                }
                else
                {
                    tStr += "\n• Đặc tính phụ: Thuần chủng, không có dị biến.";
                }

                var engine = MobileGameController.Instance?.Engine;
                if (engine != null)
                {
                    var partners = engine.ListBreedingPartners(pig.Id);
                    if (partners != null && partners.Count > 0)
                    {
                        var partner = partners[selectedPartnerCursor % partners.Count];
                        string motherId = pig.Gender == "Cai" ? pig.Id : partner.Id;
                        string fatherId = pig.Gender == "Duc" ? pig.Id : partner.Id;
                        var preview = engine.PreviewPigBreeding(motherId, fatherId);
                        tStr += $"\n• <color=#15803D><b>[Phối giống với {partner.Name}{" (" + PigText.Line(partner) + ")"}]</b>: {preview.message} ({preview.chance * 100f:0}%)</color>";
                    }
                }
                TraitsText.enableAutoSizing = true;
                TraitsText.fontSizeMin = 9.5f;
                TraitsText.fontSizeMax = 12.5f;
                var trRt = TraitsText.rectTransform;
                if (trRt != null && trRt.offsetMin.y < 76f)
                {
                    trRt.offsetMin = new Vector2(trRt.offsetMin.x, 76f);
                }
                TraitsText.text = tStr;
            }

            EnsureInteractiveButtons();
            UpdateActionBars(pig);
        }

        /// <summary>Vẽ lại thẻ của heo đang mở (sau khi phối giống, xử lý xác...).</summary>
        public void Refresh()
        {
            if (currentPig != null && ContentPanel != null && ContentPanel.activeSelf) Show(currentPig);
        }

        private GameObject actionButtonsBar;
        private GameObject breedingBar;
        private GameObject corpseBar;
        private GameObject droveBar;
        private TextMeshProUGUI droveLabel;
        private GameObject itemsBar;
        private readonly TextMeshProUGUI[] itemLabels = new TextMeshProUGUI[3];
        private readonly string[] itemIds = new string[3];
        private static readonly string[] PigItemOrder = { "XoNuoc", "ThuocAnThanHeo", "CamLinhChi", "CamHaHoa", "HoSoGiongLoai" };
        private TextMeshProUGUI isolateOrCureLabel;
        private TextMeshProUGUI sanitizeOrPreventLabel;
        private TextMeshProUGUI researchLabel;
        private TextMeshProUGUI identifyLabel;
        private TextMeshProUGUI breedingLabel;

        private void EnsureInteractiveButtons()
        {
            if (ContentPanel == null || actionButtonsBar != null) return;

            actionButtonsBar = CreateBar("PigCard_ActionButtons", 10f);
            identifyLabel = CreateCardButton(actionButtonsBar.transform, "GIÁM ĐỊNH", new Vector2(-204f, 0f), OnClickIdentifyPig);
            CreateCardButton(actionButtonsBar.transform, "PHONG ẤN / KHẮC CHẾ", new Vector2(-68f, 0f), OnClickSealOrCounterTrait);
            CreateCardButton(actionButtonsBar.transform, "THUẦN DƯỠNG / KẾ VỊ", new Vector2(68f, 0f), OnClickTrainOrSuccession);
            isolateOrCureLabel = CreateCardButton(actionButtonsBar.transform, "CÁCH LY / TRỊ BỆNH", new Vector2(204f, 0f), OnClickIsolateOrCurePig);

            // GDD 4.4: phối giống chọn nái + đực trưởng thành
            breedingBar = CreateBar("PigCard_BreedingButton", 42f);
            breedingLabel = CreateCardButton(breedingBar.transform, "PHỐI GIỐNG", new Vector2(-204f, 0f), OnClickBreed);
            CreateCardButton(breedingBar.transform, "MẮT NHÀ NGHỀ (AN)", new Vector2(-68f, 0f), OnClickAnObserveOrScreen);
            sanitizeOrPreventLabel = CreateCardButton(breedingBar.transform, "KHỬ KHUẨN / PHÒNG DỊCH", new Vector2(68f, 0f), OnClickSanitizeOrPrevent);
            researchLabel = CreateCardButton(breedingBar.transform, "NGHIÊN CỨU BỆNH", new Vector2(204f, 0f), OnClickResearchDisease);

            // C-01: take this pig to the day market (it walks to the farm gate and leaves with the player)
            droveBar = CreateBar("PigCard_DroveButton", 74f);
            droveLabel = CreateCardButton(droveBar.transform, "ĐƯA VÀO ĐOÀN ĐI CHỢ", new Vector2(204f, 0f), OnClickDrove);

            // GDD 12.8: items given to one pig (Cám Linh Chi, Cám Hạ Hỏa, Thuốc An Thần, Xô Nước, Hồ Sơ Giống Loài);
            // up to three that the player actually has, bag first then warehouse (ItemUse)
            itemsBar = CreateBar("PigCard_ItemButtons", 74f);
            for (int i = 0; i < itemLabels.Length; i++)
            {
                int slot = i;
                itemLabels[i] = CreateCardButton(itemsBar.transform, "", new Vector2(-204f + 136f * i, 0f), () => OnClickPigItem(slot));
            }

            // GDD 4.7: xác heo — thiêu hủy hoặc tái chế thành thức ăn tại Khu Xử Lý
            corpseBar = CreateBar("PigCard_CorpseButtons", 12f);
            CreateCardButton(corpseBar.transform, "THIÊU HỦY (45G+8L)", new Vector2(-80f, 0f),
                () => OnClickDisposeCorpse(CarcassProcessMethod.Incinerate_ThieuHuy));
            CreateCardButton(corpseBar.transform, "TÁI CHẾ CÁM (80G+10L)", new Vector2(80f, 0f),
                () => OnClickDisposeCorpse(CarcassProcessMethod.BioRecycle_TaiCheSinhHoc));
        }

        private GameObject CreateBar(string name, float y)
        {
            var bar = new GameObject(name);
            bar.transform.SetParent(ContentPanel.transform, false);
            var barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0f);
            barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, y);
            barRt.sizeDelta = new Vector2(556f, 30f);
            return bar;
        }

        private static (AppraisalChannel channel, int goldCost) AppraisalPlan(Pig pig)
        {
            // Hiếm/Quý/Huyền Thoại -> Phòng nghiên cứu; còn lại -> Chợ ngày (giá lấy đúng bảng GDD 5.4 mà lệnh giám định trừ)
            AppraisalChannel channel = (pig.Rarity == GeneRarity.Hiem || pig.Rarity == GeneRarity.Quy || pig.Rarity == GeneRarity.HuyenThoai)
                ? AppraisalChannel.PhongNghienCuu
                : AppraisalChannel.ChoNgay;
            var spec = CanonicalSagaAndGdd6Systems.GetCanonicalAppraisalChannelSpecGdd54(channel, pig.Rarity);
            return (channel, spec.goldCost);
        }

        private void UpdateActionBars(Pig pig)
        {
            if (actionButtonsBar == null) return;
            actionButtonsBar.SetActive(pig.IsAlive);
            breedingBar.SetActive(pig.IsAlive);
            corpseBar.SetActive(!pig.IsAlive && !pig.IsInProcessingLot);
            var eng = MobileGameController.Instance?.Engine;
            bool interactive = eng != null && eng.InteractiveHerdOwned;
            droveBar.SetActive(pig.IsAlive && interactive);
            if (droveLabel != null && eng != null)
            {
                int dc = eng.Drove.PigIds.Count;
                droveLabel.text = eng.Drove.Contains(pig.Id)
                    ? $"BỎ KHỎI ĐOÀN CHỢ ({dc}/{MarketDrove.MaxDroveSize})"
                    : $"ĐƯA VÀO ĐOÀN ĐI CHỢ ({dc}/{MarketDrove.MaxDroveSize})";
            }
            RefreshPigItems(pig, eng);

            if (identifyLabel != null)
            {
                var (_, cost) = AppraisalPlan(pig);
                identifyLabel.text = pig.IsIdentified ? "ĐÃ GIÁM ĐỊNH" : (cost > 0 ? $"GIÁM ĐỊNH ({cost}G)" : "GIÁM ĐỊNH");
            }
            if (breedingLabel != null)
            {
                var engine = MobileGameController.Instance?.Engine;
                int partners = engine != null ? engine.ListBreedingPartners(pig.Id).Count : 0;
                breedingLabel.text = partners > 0 ? $"PHỐI GIỐNG ({partners})" : "PHỐI GIỐNG";
            }
            // Cost on the button (Argon UX): quotes come from Core HealthCosts, the same numbers ExecuteHealthTreatment charges.
            if (eng == null) return;
            if (isolateOrCureLabel != null)
            {
                if (pig.ActiveDisease.HasValue)
                {
                    var (g, s) = HealthCosts.Quote(eng, HealthTreatmentVerb.DieuTriCaThe, pig);
                    isolateOrCureLabel.text = $"TRỊ BỆNH · {g}G · {s:0}TL";
                }
                else
                {
                    var (g, _) = HealthCosts.Quote(eng, HealthTreatmentVerb.CachLy, pig);
                    isolateOrCureLabel.text = pig.IsIsolated ? $"ĐANG CÁCH LY ({g}G)" : $"CÁCH LY · {g}G";
                }
            }
            if (sanitizeOrPreventLabel != null)
            {
                var (lg, ls) = HealthCosts.Quote(eng, HealthTreatmentVerb.XuLyMoiTruong, pig);
                var (vg, _) = HealthCosts.Quote(eng, HealthTreatmentVerb.PhongNguaDan, pig);
                sanitizeOrPreventLabel.text = $"VÔI {lg}G·{ls:0}TL / VẮC-XIN {vg}G";
            }
            if (researchLabel != null)
            {
                var (rg, _) = HealthCosts.Quote(eng, HealthTreatmentVerb.NghienCuu, pig);
                researchLabel.text = pig.ActiveDisease.HasValue ? $"NGHIÊN CỨU · {rg}G" : "NGHIÊN CỨU BỆNH";
            }
        }

        private void OnClickBreed()
        {
            if (currentPig == null || !currentPig.IsAlive) return;
            BreedingPickerPopup.Open(currentPig);
        }

        private void OnClickDisposeCorpse(CarcassProcessMethod method)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null || currentPig == null || currentPig.IsAlive) return;
            var (started, msg, _) = engine.ProcessDeadPigCarcass(currentPig.Id, method);
            PlayerInteractionController.Instance?.ShowFeedback(msg);
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            if (started) Hide();
            else Show(currentPig);
        }

        private TextMeshProUGUI CreateCardButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(130f, 26f);

            var img = go.AddComponent<Image>();
            img.sprite = UISpriteLoader.GetFrameSlotNormal();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.96f, 0.88f, 0.70f, 1f);

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var txtObj = new GameObject("Label");
            txtObj.transform.SetParent(go.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(3f, 1f);
            txtRt.offsetMax = new Vector2(-3f, -1f);
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 9f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 7.5f;
            tmp.fontSizeMax = 9.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.24f, 0.11f, 0.03f);
            return tmp;
        }

        private void OnClickIdentifyPig()
        {
            if (currentPig == null) return;
            var engine = MobileGameController.Instance?.Engine;
            if (currentPig.IsIdentified)
            {
                PlayerInteractionController.Instance?.ShowFeedback($"{currentPig.Name} đã được giám định đầy đủ.");
                return;
            }
            if (engine != null)
            {
                var (channel, _) = AppraisalPlan(currentPig);
                var (_, _, _, msg) = engine.AppraisePigGeneAtChannel(currentPig.Id, channel);
                PlayerInteractionController.Instance?.ShowFeedback(msg);
                Show(currentPig);
                return;
            }
            currentPig.IsIdentified = true;
            PlayerInteractionController.Instance?.ShowFeedback($"Đã Giám Định thành công [{currentPig.Name}]!");
            Show(currentPig);
        }

        private void OnClickAnObserveOrScreen()
        {
            if (currentPig == null) return;
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            if (!currentPig.IsIdentified && ProtagonistTraits.FreeScreeningAvailable(engine))
            {
                var screenRes = ProtagonistTraits.AnFreeScreening(engine, currentPig.Id);
                PlayerInteractionController.Instance?.ShowFeedback(screenRes.message);
                Show(currentPig);
                return;
            }

            // Quan sát cần đứng gần 5 giây như phím M — không lộ dấu hiệu tức thì từ thẻ heo
            MythHudController.NotifyTradeEyeStarted(currentPig.Id);
            PlayerInteractionController.Instance?.StartTradeEyeObservationFor(currentPig.Id);
            Hide();
        }

        private void OnClickBreedPig()
        {
            if (currentPig == null) return;
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var partners = engine.ListBreedingPartners(currentPig.Id);
            if (partners == null || partners.Count == 0)
            {
                PlayerInteractionController.Instance?.ShowFeedback(
                    $"{currentPig.Name} chưa có bạn phối hợp lệ (cần khác giới, Trưởng Thành, không bệnh/cách ly, không quá {BreedingLedger.MaxLitters} lứa).");
                return;
            }

            var partner = partners[selectedPartnerCursor % partners.Count];
            selectedPartnerCursor++;
            string motherId = currentPig.Gender == "Cai" ? currentPig.Id : partner.Id;
            string fatherId = currentPig.Gender == "Duc" ? currentPig.Id : partner.Id;
            var res = engine.SchedulePigBreeding(motherId, fatherId);
            PlayerInteractionController.Instance?.ShowFeedback(res.message);
            Show(currentPig);
        }

        private void OnClickSanitizeOrPrevent()
        {
            if (currentPig == null) return;
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            var envRes = engine.ExecuteHealthTreatment(HealthTreatmentVerb.XuLyMoiTruong, currentPig.Id);
            if (!envRes.success)
            {
                var prevRes = engine.ExecuteHealthTreatment(HealthTreatmentVerb.PhongNguaDan);
                PlayerInteractionController.Instance?.ShowFeedback(prevRes.message);
            }
            else
            {
                PlayerInteractionController.Instance?.ShowFeedback(envRes.message);
            }
            Show(currentPig);
        }

        private void RefreshPigItems(Pig pig, GameEngine eng)
        {
            if (itemsBar == null) return;
            int n = 0;
            if (eng != null && pig.IsAlive)
                foreach (var id in PigItemOrder)
                {
                    if (n >= itemLabels.Length) break;
                    int have = PigItemCount(eng, id);
                    if (have <= 0) continue;
                    itemIds[n] = id;
                    var data = QualifiedItemRegistry.GetData(id);
                    itemLabels[n].text = $"{(data != null ? data.DisplayName : id).ToUpperInvariant()} ({have})";
                    n++;
                }
            for (int i = 0; i < itemLabels.Length; i++)
            {
                bool on = i < n;
                itemLabels[i].transform.parent.gameObject.SetActive(on);
                if (!on) itemIds[i] = null;
            }
            itemsBar.SetActive(n > 0);
        }

        private static int PigItemCount(GameEngine eng, string id)
        {
            int n = eng.Inventory.CountId(id);
            foreach (var it in eng.SpaceBag.Items) if (ItemUse.BareId(it.ItemId) == id) n++;
            return n;
        }

        private static int BagIndexOf(GameEngine eng, string id)
        {
            var bag = eng.SpaceBag.Items;
            for (int i = 0; i < bag.Count; i++) if (ItemUse.BareId(bag[i].ItemId) == id) return i;
            return -1;
        }

        private void OnClickPigItem(int slot)
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null || currentPig == null || slot < 0 || slot >= itemIds.Length || itemIds[slot] == null) return;
            string id = itemIds[slot];
            int bagIndex = BagIndexOf(engine, id);
            // standing next to the pig: the bag is at hand; warehouse stock goes through the bracelet link (closed in combat)
            var (ok, msg) = ItemUse.Use(engine, id, currentPig.Id, bagIndex, atWarehouse: false);
            if (PlayerInteractionController.Instance != null) PlayerInteractionController.Instance.ShowFeedback(msg);
            else ToastOverlay.Show(msg);
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Show(currentPig);
        }

        private void OnClickDrove()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null || currentPig == null) return;
            var r = engine.Drove.Contains(currentPig.Id) ? engine.Drove.Unconsign(engine, currentPig.Id) : engine.Drove.Consign(engine, currentPig.Id);
            PlayerInteractionController.Instance?.ShowFeedback(r.message);
            Show(currentPig);
        }

        private void OnClickResearchDisease()
        {
            if (currentPig == null) return;
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            // A sample needs a sick pig; a healthy pig used to default to Nấm Phát Quang and charge for it.
            if (!currentPig.ActiveDisease.HasValue)
            {
                PlayerInteractionController.Instance?.ShowFeedback($"{currentPig.Name} không mang bệnh: cần một con đang bệnh để lấy mẫu nghiên cứu.");
                return;
            }
            DiseaseId targetDisease = currentPig.ActiveDisease.Value;
            var res = engine.ExecuteHealthTreatment(HealthTreatmentVerb.NghienCuu, currentPig.Id, FarmDrug.HaNhiet, targetDisease);
            PlayerInteractionController.Instance?.ShowFeedback(res.message);
            Show(currentPig);
        }

        private void OnClickSealOrCounterTrait()
        {
            if (currentPig == null) return;
            if (currentPig.GeneLine == GeneLineId.HuThe)
            {
                var (ok, msg) = currentPig.SealDeviantTrait();
                PlayerInteractionController.Instance?.ShowFeedback(msg);
                Show(currentPig);
                return;
            }

            var badTrait = currentPig.Traits?.Find(t => !t.IsPositive && !t.IsSealed);
            if (badTrait != null)
            {
                var (_, msg) = GeneticsAndBreedingSystem.ApplyBadTraitCountermeasure(currentPig, badTrait.Id);
                PlayerInteractionController.Instance?.ShowFeedback(msg);
                Show(currentPig);
            }
            else
            {
                PlayerInteractionController.Instance?.ShowFeedback("Heo không có tính trạng xấu cần phong ấn / khắc chế.");
            }
        }

        private void OnClickTrainOrSuccession()
        {
            if (currentPig == null) return;
            var engine = MobileGameController.Instance?.Engine;
            if (currentPig.GeneLine == GeneLineId.XichMao && !currentPig.IsXichMaoDomesticated)
            {
                var (_, msg) = currentPig.TrainXichMaoDomesticationDay();
                PlayerInteractionController.Instance?.ShowFeedback(msg);
                Show(currentPig);
                return;
            }

            if (engine != null)
            {
                var (_, msg) = engine.Herd.SelectSuccessionCandidate(currentPig);
                PlayerInteractionController.Instance?.ShowFeedback(msg);
                Show(currentPig);
            }
        }

        private void OnClickIsolateOrCurePig()
        {
            if (currentPig == null) return;
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;

            if (currentPig.ActiveDisease.HasValue)
            {
                FarmDrug drug = currentPig.ActiveDisease.Value == DiseaseId.GheKySinh
                    ? FarmDrug.TriGhe
                    : (currentPig.ActiveDisease.Value == DiseaseId.LoiTam || currentPig.ActiveDisease.Value == DiseaseId.RoiLoanSinhSan
                        ? FarmDrug.AnThan
                        : FarmDrug.HaNhiet);
                var (_, msg) = engine.ExecuteHealthTreatment(HealthTreatmentVerb.DieuTriCaThe, currentPig.Id, drug);
                PlayerInteractionController.Instance?.ShowFeedback(msg);
            }
            else
            {
                var (_, msg) = engine.ExecuteHealthTreatment(HealthTreatmentVerb.CachLy, currentPig.Id);
                PlayerInteractionController.Instance?.ShowFeedback(msg);
            }
            Show(currentPig);
        }

        private void UpdateBondingHeartsVisual(int fullHearts, float bondingPercent)
        {
            if (BondingHeartsText == null) return;

            Transform parent = BondingHeartsText.transform.parent;
            if (parent == null) return;

            if (heartsContainer == null)
            {
                heartsContainer = new GameObject("Hearts_Container");
                heartsContainer.transform.SetParent(parent, false);
                var hRt = heartsContainer.AddComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0, 1);
                hRt.anchorMax = new Vector2(0, 1);
                hRt.pivot = new Vector2(0, 1);
                hRt.anchoredPosition = new Vector2(105, -73);
                hRt.sizeDelta = new Vector2(110, 20);

                heartImages.Clear();
                var fullSpr = UISpriteLoader.GetIconHeartFull();
                var emptySpr = UISpriteLoader.GetIconHeartEmpty();

                for (int i = 0; i < 5; i++)
                {
                    var hObj = new GameObject($"Heart_{i}");
                    hObj.transform.SetParent(heartsContainer.transform, false);
                    var rt = hObj.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = new Vector2(i * 21f, 0);
                    rt.sizeDelta = new Vector2(18f, 18f);

                    var img = hObj.AddComponent<Image>();
                    img.sprite = (i < fullHearts) ? fullSpr : emptySpr;
                    img.color = Color.white;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    heartImages.Add(img);
                }

                // Dời text phần trăm sang bên phải dãy trái tim
                var txtRt = BondingHeartsText.rectTransform;
                txtRt.anchoredPosition = new Vector2(215, -73);
                txtRt.sizeDelta = new Vector2(150, 22);
            }
            else
            {
                var fullSpr = UISpriteLoader.GetIconHeartFull();
                var emptySpr = UISpriteLoader.GetIconHeartEmpty();
                for (int i = 0; i < heartImages.Count; i++)
                {
                    if (heartImages[i] != null)
                    {
                        heartImages[i].sprite = (i < fullHearts) ? fullSpr : emptySpr;
                    }
                }
            }

            BondingHeartsText.text = $"<b>{bondingPercent:0.0}%</b> <size=85%>(Gắn kết)</size>";
            BondingHeartsText.color = new Color(0.75f, 0.15f, 0.22f);
        }

        public void Hide()
        {
            if (ContentPanel != null)
            {
                ContentPanel.SetActive(false);
            }
        }

        private Color GetPigColor(GeneLineId geneLine)
        {
            return geneLine switch
            {
                GeneLineId.HongDien => new Color(1f, 0.72f, 0.78f),
                GeneLineId.LamKhe => new Color(0.55f, 0.85f, 0.95f),
                GeneLineId.KimTho => new Color(1f, 0.85f, 0.25f),
                GeneLineId.HuThe => new Color(0.72f, 0.45f, 0.95f),
                _ => new Color(0.9f, 0.8f, 0.7f)
            };
        }
    }
}
