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
                TitleText.text = $"<b>{pig.Name}</b> <size=70%><color=#8C5E32>(ID: #{pig.Id})</color></size>";
            }
            if (GeneText != null)
            {
                string rColor = pig.Rarity == GeneRarity.HuyenThoai ? "#B45309" : (pig.Rarity == GeneRarity.Hiem ? "#7E22CE" : "#451A03");
                string genderStr = pig.Gender == "Duc" ? "Đực ♂" : "Cái ♀";
                GeneText.text = $"Dòng: <color={rColor}><b>{pig.GeneLine}</b> [{pig.Rarity}]</color>  |  Giới tính: <b>{genderStr}</b>";
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
                StageText.text = $"Giai đoạn: <b>{pig.Stage}</b> <size=85%><color=#78350F>({pig.AgeDays} ngày tuổi)</color></size>";
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
                string disStr = pig.ActiveDisease.HasValue
                    ? $" | <color=#DC2626><b>Bệnh: {pig.ActiveDisease.Value}</b></color>"
                    : (pig.ImmuneDays > 0 ? $" | <color=#0284C7>Miễn dịch ({pig.ImmuneDays}d)</color>" : "");

                MeatQualityText.text =
                    $"Thịt: <color=#D97706><b>Hạng {pig.MeatQuality}</b></color> | Khẩu phần: <b>{feedKg:0.0}kg</b> | Bám đàn: <b>{pig.AdhesionScore:0.0}</b> | Thế hệ: <b>{genBadge}</b>{pregStr}{disStr}";
            }

            // 7. Đặc tính Gen (Traits) & Biện pháp khắc chế sau giám định
            if (TraitsText != null)
            {
                var profile = GeneticsAndBreedingSystem.GetGeneProfile(pig.GeneLine);
                string xmInfo = pig.GeneLine == GeneLineId.XichMao
                    ? (pig.IsXichMaoDomesticated ? " <color=#15803D>[Đã thuần dưỡng húc trộm]</color>" : $" <color=#B45309>[Thuần dưỡng: {pig.XichMaoDomesticationDays}/3 ngày]</color>")
                    : "";
                string tStr = $"Nội tại [{profile.Name}]{xmInfo}: <i>{profile.PassiveSummary}</i>";
                if (!pig.IsIdentified)
                {
                    tStr += "\n• <color=#B45309><b>[Chưa Giám Định]</b> — Bấm [Giám Định] để mở khóa tính trạng ẩn & biện pháp khắc chế!</color>";
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
                TraitsText.text = tStr;
            }

            EnsureInteractiveButtons();
        }

        private GameObject actionButtonsBar;

        private void EnsureInteractiveButtons()
        {
            if (ContentPanel == null || actionButtonsBar != null) return;

            actionButtonsBar = new GameObject("PigCard_ActionButtons");
            actionButtonsBar.transform.SetParent(ContentPanel.transform, false);
            var barRt = actionButtonsBar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0f);
            barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, 12f);
            barRt.sizeDelta = new Vector2(520f, 34f);

            CreateCardButton(actionButtonsBar.transform, "GIÁM ĐỊNH (50G)", new Vector2(-195f, 0f), OnClickIdentifyPig);
            CreateCardButton(actionButtonsBar.transform, "PHONG ẤN / KHẮC CHẾ", new Vector2(-65f, 0f), OnClickSealOrCounterTrait);
            CreateCardButton(actionButtonsBar.transform, "THUẦN DƯỠNG / KẾ VỊ", new Vector2(65f, 0f), OnClickTrainOrSuccession);
            CreateCardButton(actionButtonsBar.transform, "CÁCH LY / TRỊ BỆNH", new Vector2(195f, 0f), OnClickIsolateOrCurePig);
        }

        private void CreateCardButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(124f, 28f);

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
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 10.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.24f, 0.11f, 0.03f);
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
                AppraisalChannel channel = (currentPig.Rarity == GeneRarity.Hiem || currentPig.Rarity == GeneRarity.Quy || currentPig.Rarity == GeneRarity.HuyenThoai)
                    ? AppraisalChannel.PhongNghienCuu
                    : AppraisalChannel.ChoNgay;
                var (_, _, _, msg) = engine.AppraisePigGeneAtChannel(currentPig.Id, channel);
                PlayerInteractionController.Instance?.ShowFeedback(msg);
                Show(currentPig);
                return;
            }
            currentPig.IsIdentified = true;
            PlayerInteractionController.Instance?.ShowFeedback($"Đã Giám Định thành công [{currentPig.Name}]!");
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
