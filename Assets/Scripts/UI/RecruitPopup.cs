using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// GDD §11 &amp; ADR-0026 recruit counters of Market_Town: "Hội Tuyển Người" (7 farm NPCs) and "Trại Lính Thuê" (5 mercenaries).
    /// - Left column: 7 NPC cards with RoleTier-colored frames (Trắng/Lục/Lam/Tím/Cam), 4-milestone affinity bar, 3-step quest pips, and quick contract/renew/force-down buttons.
    /// - Right column: Selected NPC's ADR-0026 dossier — 4-milestone Affinity Bar (30/50/80/100 + gate locks at 50 &amp; 80), 3-Step Personal Quest + Cycle Favor box, and Interactive Term Selector + Quote Breakdown (Total / 30% Deposit / Installment).
    /// Preserves all Core calls: NpcContracts.Status / QuoteFor / Sign / Renew / Terminate / RenewalUnit / MarketUnitPrice / TierName / HireBlockedReason.
    /// </summary>
    public sealed class RecruitPopup : MonoBehaviour
    {
        public static RecruitPopup Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

        private GameObject panel;
        private TextMeshProUGUI header, footer;

        private sealed class RowWidgets
        {
            public GameObject Card;
            public Image CardBg;
            public Image Icon;
            public TextMeshProUGUI Label;
            public Image AffinityFill;
            public Image[] QuestPips = new Image[3];
            public Button HireBtn;
            public TextMeshProUGUI HireText;
            public Button AltBtn;
            public TextMeshProUGUI AltText;
            public Button ForceBtn;
            public TextMeshProUGUI ForceText;
        }

        private readonly List<RowWidgets> rows = new List<RowWidgets>();

        // Right-hand ADR-0026 Inspector Dossier widgets
        private Image detailCardBg;
        private Image detailPortrait;
        private TextMeshProUGUI detailTitleText;
        private Image detailAffinityFill;
        private Image detailGate50Lock;
        private Image detailGate80Lock;
        private TextMeshProUGUI detailAffinityText;
        private readonly Image[] detailQuestPips = new Image[3];
        private TextMeshProUGUI detailQuestText;
        private TextMeshProUGUI detailQuoteText;
        private readonly List<(Button btn, Image bg, TextMeshProUGUI txt, ContractKind kind, int terms)> termOptionButtons =
            new List<(Button, Image, TextMeshProUGUI, ContractKind, int)>();
        private Button detailSignOrRenewBtn;
        private TextMeshProUGUI detailSignOrRenewText;
        private Button detailForceOrTermBtn;
        private TextMeshProUGUI detailForceOrTermText;

        private bool mercenaries;
        private int selectedRowIndex;
        private ContractKind selectedKind = ContractKind.ChuKyNho;
        private int selectedTerms = 1;
        private NpcSpecialistId lastSelectedNpcId = (NpcSpecialistId)(-1);
        private string lastMessage = "";

        public static void Open(bool mercenaries)
        {
            if (Instance == null)
            {
                var go = new GameObject("RecruitCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = go.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 115;
                var sc = go.GetComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1280f, 720f);
                sc.matchWidthOrHeight = 0.5f;
                Instance = go.AddComponent<RecruitPopup>();
                Instance.Build(go.transform);
            }
            Instance.mercenaries = mercenaries;
            Instance.selectedRowIndex = 0;
            Instance.lastSelectedNpcId = (NpcSpecialistId)(-1);
            Instance.lastMessage = "";
            Instance.panel.SetActive(true);
            Instance.panel.transform.SetAsLastSibling();
            Instance.Refresh();
        }

        public void Close() { if (panel != null) panel.SetActive(false); }

        private void Build(Transform root)
        {
            panel = DayMarketPopup.BuildStyledPopupFrame(root, "RecruitPopup", new Vector2(980f, 580f),
                out _, out var headerBanner, out var footerBanner);

            header = DayMarketPopup.Text(headerBanner, new Vector2(-40f, 0f), new Vector2(760f, 38f), 18f, TextAlignmentOptions.Left);
            header.color = new Color(1f, 0.94f, 0.68f);

            var close = DayMarketPopup.Button(headerBanner, new Vector2(400f, 0f), new Vector2(76f, 32f), out var ct);
            ct.text = "Đóng";
            close.onClick.AddListener(Close);

            var fillSpr = MythUi.Kit("affinity_bar_fill") ?? MythUi.Kit("bar_stamina_fill_bright");
            var frameSpr = MythUi.Kit("affinity_bar_frame") ?? MythUi.Kit("bar_hp_trough");
            var pipLockedSpr = MythUi.Kit("quest_step_pip_locked");

            // Left column: 7 NPC row cards
            for (int i = 0; i < 7; i++)
            {
                float y = 186f - i * 58f;
                var rowCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(-186f, y), new Vector2(564f, 54f));
                var rw = new RowWidgets
                {
                    Card = rowCard,
                    CardBg = rowCard.GetComponent<Image>()
                };

                // Clicking the card selects this NPC for the right-hand ADR-0026 dossier
                int idx = i;
                var selectBtn = rowCard.AddComponent<Button>();
                selectBtn.transition = Selectable.Transition.None;
                selectBtn.onClick.AddListener(() => SelectRow(idx));

                rw.Icon = DayMarketPopup.MakePortraitIcon(rowCard.transform, new Vector2(-256f, 0f), new Vector2(36f, 36f));

                rw.Label = DayMarketPopup.Text(rowCard.transform, new Vector2(-56f, 6f), new Vector2(348f, 36f), 10.5f, TextAlignmentOptions.TopLeft);
                rw.Label.richText = true;
                rw.Label.textWrappingMode = TextWrappingModes.Normal;
                rw.Label.overflowMode = TextOverflowModes.Truncate;

                // Mini 4-milestone affinity bar at bottom-left of card
                var barBgGo = new GameObject("AffinityBar", typeof(RectTransform), typeof(Image));
                barBgGo.transform.SetParent(rowCard.transform, false);
                var barBgRt = (RectTransform)barBgGo.transform;
                barBgRt.anchoredPosition = new Vector2(-156f, -18f);
                barBgRt.sizeDelta = new Vector2(140f, 8f);
                var barBgImg = barBgGo.GetComponent<Image>();
                barBgImg.sprite = frameSpr;
                barBgImg.raycastTarget = false;

                var barFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                barFillGo.transform.SetParent(barBgGo.transform, false);
                var barFillRt = (RectTransform)barFillGo.transform;
                barFillRt.anchorMin = new Vector2(0f, 0.5f);
                barFillRt.anchorMax = new Vector2(0f, 0.5f);
                barFillRt.pivot = new Vector2(0f, 0.5f);
                barFillRt.anchoredPosition = new Vector2(2f, 0f);
                barFillRt.sizeDelta = new Vector2(136f, 5f);
                rw.AffinityFill = barFillGo.GetComponent<Image>();
                rw.AffinityFill.sprite = fillSpr;
                rw.AffinityFill.type = Image.Type.Filled;
                rw.AffinityFill.fillMethod = Image.FillMethod.Horizontal;
                rw.AffinityFill.fillOrigin = 0;
                rw.AffinityFill.raycastTarget = false;

                // 3 Personal Quest Step Pips next to the mini affinity bar
                for (int q = 0; q < 3; q++)
                {
                    var pipGo = new GameObject("QPip" + q, typeof(RectTransform), typeof(Image));
                    pipGo.transform.SetParent(rowCard.transform, false);
                    var pipRt = (RectTransform)pipGo.transform;
                    pipRt.anchoredPosition = new Vector2(-72f + q * 13f, -18f);
                    pipRt.sizeDelta = new Vector2(10f, 10f);
                    rw.QuestPips[q] = pipGo.GetComponent<Image>();
                    rw.QuestPips[q].sprite = pipLockedSpr;
                    rw.QuestPips[q].raycastTarget = false;
                }

                // 3 Action Buttons on the right side of the row card
                rw.ForceBtn = DayMarketPopup.Button(rowCard.transform, new Vector2(96f, 0f), new Vector2(68f, 28f), out rw.ForceText);
                rw.HireBtn = DayMarketPopup.Button(rowCard.transform, new Vector2(168f, 0f), new Vector2(72f, 28f), out rw.HireText);
                rw.AltBtn = DayMarketPopup.Button(rowCard.transform, new Vector2(242f, 0f), new Vector2(68f, 28f), out rw.AltText);
                rw.HireText.fontSize = 9.5f;
                rw.AltText.fontSize = 9.5f;
                rw.ForceText.fontSize = 9f;
                rw.HireText.enableAutoSizing = rw.AltText.enableAutoSizing = rw.ForceText.enableAutoSizing = true;
                rw.HireText.fontSizeMin = rw.AltText.fontSizeMin = rw.ForceText.fontSizeMin = 7.5f;
                rw.HireText.fontSizeMax = rw.AltText.fontSizeMax = 9.5f;
                rw.ForceText.fontSizeMax = 9f;

                rw.HireBtn.onClick.AddListener(() => { SelectRow(idx); HireRow(idx, true); });
                rw.AltBtn.onClick.AddListener(() => { SelectRow(idx); HireRow(idx, false); });
                rw.ForceBtn.onClick.AddListener(() => { SelectRow(idx); ForceDownRow(idx); });

                rows.Add(rw);
            }

            // Right column: ADR-0026 Inspector Dossier & Term Quote Panel
            BuildDetailInspector();

            footer = DayMarketPopup.Text(footerBanner, Vector2.zero, new Vector2(860f, 34f), 13f, TextAlignmentOptions.Center);
            footer.color = new Color(0.96f, 0.90f, 0.74f);
        }

        private void BuildDetailInspector()
        {
            var detailCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(286f, 12f), new Vector2(364f, 404f));
            detailCardBg = detailCard.GetComponent<Image>();

            detailPortrait = DayMarketPopup.MakePortraitIcon(detailCard.transform, new Vector2(-146f, 162f), new Vector2(46f, 46f));

            detailTitleText = DayMarketPopup.Text(detailCard.transform, new Vector2(24f, 162f), new Vector2(276f, 52f), 11f, TextAlignmentOptions.Left);
            detailTitleText.richText = true;
            detailTitleText.textWrappingMode = TextWrappingModes.Normal;

            // Section 1: 4-Milestone Affinity Bar (0..100 with 30 / 50 / 80 / 100 ticks & gate locks)
            var affBox = DayMarketPopup.MakeSlotCard(detailCard.transform, new Vector2(0f, 98f), new Vector2(344f, 64f));
            affBox.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.09f, 0.92f);

            var barFrameGo = new GameObject("DetailAffinityBar", typeof(RectTransform), typeof(Image));
            barFrameGo.transform.SetParent(affBox.transform, false);
            var bfRt = (RectTransform)barFrameGo.transform;
            bfRt.anchoredPosition = new Vector2(0f, 12f);
            bfRt.sizeDelta = new Vector2(320f, 12f);
            var bfImg = barFrameGo.GetComponent<Image>();
            bfImg.sprite = MythUi.Kit("affinity_bar_frame") ?? MythUi.Kit("bar_hp_trough");
            bfImg.raycastTarget = false;

            var barFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            barFillGo.transform.SetParent(barFrameGo.transform, false);
            var fillRt = (RectTransform)barFillGo.transform;
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = new Vector2(3f, 0f);
            fillRt.sizeDelta = new Vector2(314f, 8f);
            detailAffinityFill = barFillGo.GetComponent<Image>();
            detailAffinityFill.sprite = MythUi.Kit("affinity_bar_fill") ?? MythUi.Kit("bar_stamina_fill_bright");
            detailAffinityFill.type = Image.Type.Filled;
            detailAffinityFill.fillMethod = Image.FillMethod.Horizontal;
            detailAffinityFill.fillOrigin = 0;
            detailAffinityFill.raycastTarget = false;

            var lockSpr = MythUi.Kit("affinity_gate_lock");
            var g50Go = new GameObject("Gate50Lock", typeof(RectTransform), typeof(Image));
            g50Go.transform.SetParent(barFrameGo.transform, false);
            ((RectTransform)g50Go.transform).anchoredPosition = new Vector2(0f, 9f);
            ((RectTransform)g50Go.transform).sizeDelta = new Vector2(10f, 10f);
            detailGate50Lock = g50Go.GetComponent<Image>();
            detailGate50Lock.sprite = lockSpr;
            detailGate50Lock.raycastTarget = false;

            var g80Go = new GameObject("Gate80Lock", typeof(RectTransform), typeof(Image));
            g80Go.transform.SetParent(barFrameGo.transform, false);
            ((RectTransform)g80Go.transform).anchoredPosition = new Vector2(96f, 9f);
            ((RectTransform)g80Go.transform).sizeDelta = new Vector2(10f, 10f);
            detailGate80Lock = g80Go.GetComponent<Image>();
            detailGate80Lock.sprite = lockSpr;
            detailGate80Lock.raycastTarget = false;

            detailAffinityText = DayMarketPopup.Text(affBox.transform, new Vector2(0f, -12f), new Vector2(332f, 34f), 9.5f, TextAlignmentOptions.TopLeft);
            detailAffinityText.richText = true;
            detailAffinityText.textWrappingMode = TextWrappingModes.Normal;

            // Section 2: 3-Step Personal Quest + Cycle Favor Box
            var questBox = DayMarketPopup.MakeSlotCard(detailCard.transform, new Vector2(0f, 8f), new Vector2(344f, 104f));
            questBox.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.09f, 0.92f);

            for (int q = 0; q < 3; q++)
            {
                var pipGo = new GameObject("DetailQPip" + q, typeof(RectTransform), typeof(Image));
                pipGo.transform.SetParent(questBox.transform, false);
                var pRt = (RectTransform)pipGo.transform;
                pRt.anchoredPosition = new Vector2(-156f, 22f - q * 20f);
                pRt.sizeDelta = new Vector2(12f, 12f);
                detailQuestPips[q] = pipGo.GetComponent<Image>();
                detailQuestPips[q].sprite = MythUi.Kit("quest_step_pip_locked");
                detailQuestPips[q].raycastTarget = false;
            }

            detailQuestText = DayMarketPopup.Text(questBox.transform, new Vector2(8f, 0f), new Vector2(316f, 96f), 9.5f, TextAlignmentOptions.TopLeft);
            detailQuestText.richText = true;
            detailQuestText.textWrappingMode = TextWrappingModes.Normal;

            // Section 3: Interactive Term Selector + Quote Breakdown Box (Total / Deposit / Installment)
            var quoteBox = DayMarketPopup.MakeSlotCard(detailCard.transform, new Vector2(0f, -124f), new Vector2(344f, 148f));
            quoteBox.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.09f, 0.92f);

            // Up to 6 term option buttons arranged in a top strip inside quoteBox
            (ContractKind kind, int terms, string label)[] allOptions =
            {
                (ContractKind.ChuKyNho, 1, "1 Chu kỳ"),
                (ContractKind.ChuKyNho, 2, "2 Chu kỳ"),
                (ContractKind.ChuKyNho, 3, "3 Chu kỳ"),
                (ContractKind.ChuKyLon, 2, "2 CK lớn"),
                (ContractKind.ChuKyLon, 3, "3 CK lớn"),
                (ContractKind.TheoDot, 3, "3 Đợt"),
                (ContractKind.TheoDot, 5, "5 Đợt"),
                (ContractKind.TheoDot, 8, "8 Đợt"),
            };

            for (int i = 0; i < allOptions.Length; i++)
            {
                var opt = allOptions[i];
                var b = DayMarketPopup.Button(quoteBox.transform, Vector2.zero, new Vector2(76f, 22f), out var bt);
                bt.text = opt.label;
                bt.fontSize = 9f;
                var bg = b.GetComponent<Image>();
                var kCopy = opt.kind;
                int tCopy = opt.terms;
                b.onClick.AddListener(() =>
                {
                    selectedKind = kCopy;
                    selectedTerms = tCopy;
                    Refresh();
                });
                termOptionButtons.Add((b, bg, bt, opt.kind, opt.terms));
            }

            detailQuoteText = DayMarketPopup.Text(quoteBox.transform, new Vector2(0f, -4f), new Vector2(328f, 72f), 9.5f, TextAlignmentOptions.TopLeft);
            detailQuoteText.richText = true;
            detailQuoteText.textWrappingMode = TextWrappingModes.Normal;

            detailSignOrRenewBtn = DayMarketPopup.Button(quoteBox.transform, new Vector2(-82f, -54f), new Vector2(160f, 28f), out detailSignOrRenewText);
            detailSignOrRenewText.fontSize = 10f;
            detailSignOrRenewBtn.onClick.AddListener(OnClickDetailPrimaryAction);

            detailForceOrTermBtn = DayMarketPopup.Button(quoteBox.transform, new Vector2(84f, -54f), new Vector2(156f, 28f), out detailForceOrTermText);
            detailForceOrTermText.fontSize = 9.5f;
            detailForceOrTermBtn.onClick.AddListener(OnClickDetailSecondaryAction);
        }

        private void SelectRow(int idx)
        {
            selectedRowIndex = idx;
            Refresh();
        }

        private static Sprite TierCardSprite(RoleTier tier)
        {
            string stem = tier switch
            {
                RoleTier.Trang => "npc_card_trang",
                RoleTier.Luc => "npc_card_luc",
                RoleTier.Lam => "npc_card_lam",
                RoleTier.Tim => "npc_card_tim",
                _ => "npc_card_cam",
            };
            return MythUi.Kit(stem) ?? UISpriteLoader.GetFrameSlotNormal();
        }

        private static Color TierTint(RoleTier tier, bool selected)
        {
            Color c = tier switch
            {
                RoleTier.Trang => new Color(0.88f, 0.90f, 0.94f, 0.96f),
                RoleTier.Luc => new Color(0.72f, 0.95f, 0.80f, 0.96f),
                RoleTier.Lam => new Color(0.72f, 0.86f, 0.98f, 0.96f),
                RoleTier.Tim => new Color(0.88f, 0.76f, 0.98f, 0.96f),
                _ => new Color(0.98f, 0.84f, 0.64f, 0.96f),
            };
            return selected ? Color.Lerp(c, Color.white, 0.35f) : c;
        }

        private RecruitmentRules.Offer[] Current() => RecruitmentRules.Offers.Where(o => o.IsMercenary == mercenaries).ToArray();

        private static string PortraitKey(NpcSpecialistId id) => id switch
        {
            NpcSpecialistId.VeBinhKhien => "merc_do",
            NpcSpecialistId.XaThuNo => "merc_mien",
            NpcSpecialistId.KySuChienTruong => "merc_chot",
            NpcSpecialistId.ThoSanUfo => "merc_tu_troi",
            NpcSpecialistId.ThichKhachDem => "merc_van",
            NpcSpecialistId.ThuKho => "ong_ton",
            NpcSpecialistId.BacSiThuY => "dung",
            _ => "npc_recruit"
        };

        private static string VenueLabel(RecruitmentRules.Venue v) => v switch
        {
            RecruitmentRules.Venue.DayMarket => "chợ ngày",
            RecruitmentRules.Venue.NightMarket => "chợ đêm",
            RecruitmentRules.Venue.NightAuction => "đấu giá đêm",
            _ => "ngày/đêm",
        };

        private void Refresh()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            header.text = mercenaries
                ? $"<b>TRẠI LÍNH THUÊ — HỢP ĐỒNG THEO ĐỢT / CHU KỲ</b>   •   <color=#FFE270>{engine.Farm.Gold:N0} G</color>"
                : $"<b>HỘI TUYỂN NGƯỜI — HỢP ĐỒNG NHÂN SỰ TRẠI</b>   •   <color=#FFE270>{engine.Farm.Gold:N0} G</color>";

            var offers = Current();
            if (offers.Length > 0)
                selectedRowIndex = Mathf.Clamp(selectedRowIndex, 0, offers.Length - 1);

            var pipDone = MythUi.Kit("quest_step_pip_done");
            var pipAct = MythUi.Kit("quest_step_pip_active");
            var pipLock = MythUi.Kit("quest_step_pip_locked");

            for (int i = 0; i < rows.Count; i++)
            {
                var rw = rows[i];
                bool has = i < offers.Length;
                rw.Card.SetActive(has);
                if (!has) continue;

                var o = offers[i];
                var prof = NpcContracts.Profile(o.Id);
                var rel = engine.NpcRelations.Get(o.Id);
                var contract = engine.NpcRelations.ContractOf(o.Id);
                bool isSel = (i == selectedRowIndex);

                if (rw.Icon != null)
                {
                    rw.Icon.sprite = UISpriteLoader.LoadCharacterCellZero(PortraitKey(o.Id));
                    rw.Icon.enabled = rw.Icon.sprite != null;
                }
                string where = VenueLabel(o.Where);
                if (prof == null)
                {
                    rw.Label.text = o.Role;
                    rw.HireBtn.interactable = false;
                    rw.AltBtn.gameObject.SetActive(false);
                    rw.ForceBtn.gameObject.SetActive(false);
                    continue;
                }

                if (rw.CardBg != null)
                {
                    rw.CardBg.sprite = TierCardSprite(prof.Tier);
                    rw.CardBg.type = Image.Type.Sliced;
                    rw.CardBg.color = TierTint(prof.Tier, isSel);
                }

                string tierColor = prof.Tier switch
                {
                    RoleTier.Trang => "#E5E7EB",
                    RoleTier.Luc => "#4ADE80",
                    RoleTier.Lam => "#60A5FA",
                    RoleTier.Tim => "#C084FC",
                    _ => "#FB923C",
                };
                string price = prof.Tier == RoleTier.Cam ? $"{NpcContracts.DailyPrice(engine, prof):N0}G/ngày × CK lớn (>=2)"
                    : prof.IsMercenary ? $"{NpcContracts.WavePrice(engine, prof):N0}G/đợt · {NpcContracts.CyclePrice(engine, prof):N0}G/CK"
                    : $"{NpcContracts.CyclePrice(engine, prof):N0}G/chu kỳ";
                string block = contract != null ? "" : NpcContracts.HireBlockedReason(engine, o.Id);
                string selTag = isSel ? "<color=#FFE270>> </color>" : "";

                rw.Label.text = $"{selTag}<b>{PetNpcAndOccultManager.GetNpcCanonicalTownName(o.Id)}</b> · <color={tierColor}>{NpcContracts.TierName(prof.Tier)}</color> · {o.Role} ({where})\n" +
                                $"<size=9.5><color=#FFE270>{price}</color> · {NpcContracts.Status(engine, o.Id)}</size>";

                if (rw.AffinityFill != null)
                {
                    rw.AffinityFill.fillAmount = rel.Unlocked ? Mathf.Clamp01(rel.Affinity / 100f) : 0f;
                }
                for (int q = 0; q < 3; q++)
                {
                    if (rw.QuestPips[q] != null)
                    {
                        rw.QuestPips[q].sprite = q < rel.QuestStage ? pipDone : (q == rel.QuestStage ? pipAct : pipLock);
                    }
                }

                if (contract != null)
                {
                    var (unit, pricing) = NpcContracts.RenewalUnit(engine, contract, forceDown: false);
                    int market = NpcContracts.MarketUnitPrice(engine, prof, contract.Kind);
                    rw.HireText.text = pricing == NpcContracts.RenewPricing.Locked ? "Gia hạn·giữ giá" : "Gia hạn";
                    rw.HireBtn.interactable = true;
                    rw.AltBtn.gameObject.SetActive(true);
                    rw.AltText.text = "Chấm dứt";
                    rw.AltBtn.interactable = true;
                    bool canForce = market < contract.UnitPrice;
                    rw.ForceBtn.gameObject.SetActive(canForce);
                    rw.ForceText.text = $"Ép giá {market:N0}";
                    rw.Label.text += $" · <size=9><color=#D8C89E>Thuê {contract.UnitPrice:N0}G / TT {market:N0}G</color></size>";
                }
                else
                {
                    var (k1, n1, k2, n2) = Options(prof);
                    rw.HireText.text = Label(k1, n1);
                    rw.AltText.text = Label(k2, n2);
                    bool can = block.Length == 0 && RecruitmentRules.IsOpen(o.Where, engine.Clock.CurrentHour);
                    rw.HireBtn.interactable = can;
                    rw.AltBtn.gameObject.SetActive(true);
                    rw.AltBtn.interactable = can;
                    rw.ForceBtn.gameObject.SetActive(false);
                }
            }

            if (offers.Length > 0)
            {
                RefreshDetailInspector(engine, offers[selectedRowIndex]);
            }

            int maxNpcs = NpcContracts.MaxNpcs(engine.Farm.FarmLevel);
            footer.text = string.IsNullOrEmpty(lastMessage)
                ? $"Vàng: {engine.Farm.Gold:N0} G  ·  Hợp đồng đang chạy: {engine.NpcRelations.Contracts.Count}/{maxNpcs} (Cấp trại {engine.Farm.FarmLevel})  ·  Giờ: {engine.Clock.CurrentHour:00}:{engine.Clock.CurrentMinute:00}"
                : lastMessage;
        }

        private void RefreshDetailInspector(GameEngine engine, RecruitmentRules.Offer offer)
        {
            var prof = NpcContracts.Profile(offer.Id);
            if (prof == null) return;
            var rel = engine.NpcRelations.Get(offer.Id);
            var contract = engine.NpcRelations.ContractOf(offer.Id);

            if (lastSelectedNpcId != offer.Id)
            {
                lastSelectedNpcId = offer.Id;
                var (defKind, defTerms, _, _) = Options(prof);
                selectedKind = defKind;
                selectedTerms = defTerms;
            }
            if (!NpcContracts.KindAllowed(prof, selectedKind))
            {
                var (defKind, defTerms, _, _) = Options(prof);
                selectedKind = defKind;
                selectedTerms = defTerms;
            }

            if (detailCardBg != null)
            {
                detailCardBg.sprite = TierCardSprite(prof.Tier);
                detailCardBg.type = Image.Type.Sliced;
                detailCardBg.color = TierTint(prof.Tier, false);
            }
            if (detailPortrait != null)
            {
                detailPortrait.sprite = UISpriteLoader.LoadCharacterCellZero(PortraitKey(offer.Id));
                detailPortrait.enabled = detailPortrait.sprite != null;
            }

            string tierColor = prof.Tier switch
            {
                RoleTier.Trang => "#E5E7EB",
                RoleTier.Luc => "#4ADE80",
                RoleTier.Lam => "#60A5FA",
                RoleTier.Tim => "#C084FC",
                _ => "#FB923C",
            };
            float effBonus = NpcContracts.EfficiencyBonus(engine, offer.Id) * 100f;
            int recogPct = Mathf.Min(30, rel.TermsCompleted * 3);
            detailTitleText.text =
                $"<b><size=12.5>{PetNpcAndOccultManager.GetNpcCanonicalTownName(offer.Id)}</size></b> · <color={tierColor}><b>{NpcContracts.TierName(prof.Tier)}</b></color>\n" +
                $"Vai trò: <b>{prof.Role}</b> (Trọng yếu <b>{prof.Criticality:0.00}</b>) · Tuyển: <b>{VenueLabel(offer.Where)}</b>\n" +
                $"<size=9.5>Hiệu suất hảo cảm: <color=#86EFAC><b>+{effBonus:0}%</b></color> · Uy tín TT: <color=#FFE270>+{recogPct}%</color> ({rel.TermsCompleted} kì)</size>";

            // 1. 4-Milestone Affinity Bar (30 / 50 / 80 / 100)
            if (detailAffinityFill != null)
            {
                detailAffinityFill.fillAmount = rel.Unlocked ? Mathf.Clamp01(rel.Affinity / 100f) : 0f;
            }
            if (detailGate50Lock != null) detailGate50Lock.gameObject.SetActive(rel.QuestStage < 2);
            if (detailGate80Lock != null) detailGate80Lock.gameObject.SetActive(rel.QuestStage < 3);

            int hireNeed = NpcContracts.HireAffinity(prof);
            float cap = NpcContracts.AffinityCap(rel);
            string affState = !rel.Unlocked
                ? "<color=#FCA5A5>Chưa mở thanh (cần xong Nhiệm vụ bước 1)</color>"
                : $"<b>{rel.Affinity:0.#}/100</b> (Trần hiện tại: {cap:0} · Hệ số dải ×{NpcContracts.BandMultiplier(rel.Affinity):0.##})";
            string hireGateStr = prof.IsMercenary ? "Lính thuê: 0 (mở bằng Bước 1)" : $"Ngưỡng thuê: <b>{hireNeed}</b>";
            detailAffinityText.text =
                $"Hảo cảm: {affState} · {hireGateStr}\n" +
                $"<size=8.5><color=#D8C89E>Mốc <b>30</b>: Báo trước 1d · <b>50</b>: +10% & mở Đặc tính {(rel.QuestStage < 2 ? "[Khóa B2]" : "")} · <b>80</b>: +20% {(rel.QuestStage < 3 ? "[Khóa B3]" : "")} · <b>100</b>: +30% & Tri kỷ</color></size>";

            // 2. 3-Step Personal Quest + Cycle Favor
            var pipDone = MythUi.Kit("quest_step_pip_done");
            var pipAct = MythUi.Kit("quest_step_pip_active");
            var pipLock = MythUi.Kit("quest_step_pip_locked");
            int curVal = NpcContracts.Metric(engine, prof.Metric);

            var qsb = new System.Text.StringBuilder();
            qsb.AppendLine("<b><color=#FFE270>NHIỆM VỤ RIÊNG 3 BƯỚC & VIỆC NHỜ CHU KỲ</color></b>");
            string[] gateNotes = { "Mở hảo cảm/thuê", "Mở cổng 50", "Mở cổng 80" };
            for (int q = 0; q < 3; q++)
            {
                bool done = q < rel.QuestStage;
                bool active = q == rel.QuestStage;
                if (detailQuestPips[q] != null)
                    detailQuestPips[q].sprite = done ? pipDone : (active ? pipAct : pipLock);

                int target = prof.QuestTargets[q];
                int shownCur = Mathf.Min(curVal, target);
                string stepCol = done ? "#86EFAC" : (active ? "#FDE047" : "#9CA3AF");
                string statusTag = done ? "[XONG +10]" : $"({shownCur}/{target})";
                qsb.AppendLine($"<color={stepCol}>• B{q + 1}: <b>{prof.QuestTitles[q]}</b> {statusTag} — <i>{gateNotes[q]}</i></color>");
            }
            bool favorDone = rel.FavorDoneCycle == engine.Clock.CycleNumber;
            string favorStatus = !rel.Unlocked ? "Chưa mở" : (favorDone ? "<color=#86EFAC>Đã xong CK này (+5)</color>" : "<color=#FDE047>Chưa xong (+5 gốc)</color>");
            qsb.Append($"<size=8.5>• Việc nhờ CK {engine.Clock.CycleNumber}: <i>{prof.FavorText}</i> [{favorStatus}]</size>");
            detailQuestText.text = qsb.ToString();

            // 3. Term Selector + Quote Breakdown
            int visibleBtnIdx = 0;
            for (int i = 0; i < termOptionButtons.Count; i++)
            {
                var (btn, bg, txt, kind, terms) = termOptionButtons[i];
                bool allowed = NpcContracts.KindAllowed(prof, kind);
                btn.gameObject.SetActive(allowed);
                if (!allowed) continue;

                // Position visible term buttons evenly along the top of quoteBox
                var rt = (RectTransform)btn.transform;
                rt.anchoredPosition = new Vector2(-124f + visibleBtnIdx * 82f, 54f);
                visibleBtnIdx++;

                bool chosen = (kind == selectedKind && terms == selectedTerms);
                if (bg != null)
                    bg.color = chosen ? new Color(0.92f, 0.72f, 0.24f, 1f) : new Color(0.55f, 0.44f, 0.30f, 0.9f);
                txt.color = chosen ? new Color(0.18f, 0.08f, 0.02f) : Color.white;
            }

            if (contract != null)
            {
                int market = NpcContracts.MarketUnitPrice(engine, prof, contract.Kind);
                var (renewUnit, pricing) = NpcContracts.RenewalUnit(engine, contract, forceDown: false);
                int rTerms = RenewTerms(contract);
                string unitName = contract.Kind == ContractKind.ChuKyLon ? "ngày" : (contract.Kind == ContractKind.TheoDot ? "đợt" : "chu kỳ");
                string pricingNote = pricing switch
                {
                    NpcContracts.RenewPricing.Locked => "<color=#86EFAC>Giữ giá cũ nhờ Hảo cảm >= 50</color>",
                    NpcContracts.RenewPricing.KeptHigher => "<color=#FDE047>Thị trường đang thấp hơn — có thể Ép giá (-5 HC)</color>",
                    _ => "Theo giá thị trường hiện tại",
                };
                detailQuoteText.text =
                    $"<b><color=#86EFAC>HỢP ĐỒNG ĐANG CHẠY ({NpcContracts.Status(engine, offer.Id)})</color></b>\n" +
                    $"• Đơn giá đang thuê: <b>{contract.UnitPrice:N0}G/{unitName}</b>  |  Giá thị trường: <b>{market:N0}G/{unitName}</b>\n" +
                    $"• Đã cọc: <b>{contract.Deposit:N0}G</b>  |  Mỗi kì trả: <b>{contract.InstallmentAmount:N0}G</b> × còn <b>{contract.InstallmentsLeft}</b> kì\n" +
                    $"• Gia hạn (+{rTerms} {unitName}): <b>{renewUnit:N0}G/{unitName}</b> — {pricingNote}";

                detailSignOrRenewText.text = pricing == NpcContracts.RenewPricing.Locked ? "Gia Hạn (Giữ Giá)" : $"Gia Hạn (+{rTerms})";
                detailSignOrRenewBtn.interactable = true;

                bool canForce = market < contract.UnitPrice;
                detailForceOrTermBtn.gameObject.SetActive(true);
                detailForceOrTermBtn.interactable = true;
                detailForceOrTermText.text = canForce ? $"Ép Giá {market:N0}G (-5 HC)" : "Chấm Dứt (-15 HC)";
            }
            else
            {
                var quote = NpcContracts.QuoteFor(engine, offer.Id, selectedKind, selectedTerms);
                int marketUnit = NpcContracts.MarketUnitPrice(engine, prof, selectedKind);
                string unitName = selectedKind == ContractKind.ChuKyLon ? "ngày" : (selectedKind == ContractKind.TheoDot ? "đợt" : "chu kỳ");
                string block = NpcContracts.HireBlockedReason(engine, offer.Id);
                bool venueOpen = RecruitmentRules.IsOpen(offer.Where, engine.Clock.CurrentHour);

                string dueNote = selectedKind == ContractKind.TheoDot
                    ? "Không cọc · Trả sau mỗi đợt quái"
                    : (selectedKind == ContractKind.ChuKyLon ? "Cọc 30% · Trả cuối mỗi chu kỳ lớn (13–15d)" : "Cọc 30% · Trả sau mỗi đợt lớn");

                detailQuoteText.text =
                    $"<b><color=#FFE270>BÁO GIÁ HỢP ĐỒNG ({Label(selectedKind, selectedTerms)})</color></b>\n" +
                    $"• Đơn giá thị trường: <b>{marketUnit:N0}G/{unitName}</b> (Chủ đề chợ: {engine.Economy.DayMarket.Theme})\n" +
                    $"• Tổng hợp đồng: <color=#FFE270><b>{quote.Total:N0}G</b></color>  |  Đặt cọc (30%): <color=#86EFAC><b>{quote.Deposit:N0}G</b></color>\n" +
                    $"• Kì trả: <b>{quote.Installment:N0}G/kì × {quote.Installments} kì</b> ({dueNote})\n" +
                    (block.Length > 0 ? $"<color=#FCA5A5>• Điều kiện: {block}</color>"
                        : (!venueOpen ? $"<color=#FCA5A5>• Quầy tuyển chỉ mở tại {VenueLabel(offer.Where)}.</color>"
                            : $"<color=#86EFAC>• Đủ điều kiện ký hợp đồng ngay!</color>"));

                bool canSign = quote.Ok && block.Length == 0 && venueOpen;
                detailSignOrRenewText.text = quote.Deposit > 0 ? $"Ký & Cọc {quote.Deposit:N0}G" : $"Ký ({quote.Total:N0}G)";
                detailSignOrRenewBtn.interactable = canSign;

                var (_, _, k2, n2) = Options(prof);
                detailForceOrTermBtn.gameObject.SetActive(true);
                detailForceOrTermText.text = Label(k2, n2);
                detailForceOrTermBtn.interactable = block.Length == 0 && venueOpen;
            }
        }

        private void OnClickDetailPrimaryAction()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            var offers = Current();
            if (engine == null || selectedRowIndex < 0 || selectedRowIndex >= offers.Length) return;
            var offer = offers[selectedRowIndex];
            var running = engine.NpcRelations.ContractOf(offer.Id);
            if (running != null)
            {
                lastMessage = NpcContracts.Renew(engine, offer.Id, RenewTerms(running), forceDown: false).message;
            }
            else if (!RecruitmentRules.IsOpen(offer.Where, engine.Clock.CurrentHour))
            {
                lastMessage = RecruitmentRules.Hire(engine, offer.Id).message;
            }
            else
            {
                lastMessage = NpcContracts.Sign(engine, offer.Id, selectedKind, selectedTerms).message;
            }
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Refresh();
        }

        private void OnClickDetailSecondaryAction()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            var offers = Current();
            if (engine == null || selectedRowIndex < 0 || selectedRowIndex >= offers.Length) return;
            var offer = offers[selectedRowIndex];
            var prof = NpcContracts.Profile(offer.Id);
            var running = engine.NpcRelations.ContractOf(offer.Id);
            if (running != null && prof != null)
            {
                int market = NpcContracts.MarketUnitPrice(engine, prof, running.Kind);
                if (market < running.UnitPrice)
                    lastMessage = NpcContracts.Renew(engine, offer.Id, RenewTerms(running), forceDown: true).message;
                else
                    lastMessage = NpcContracts.Terminate(engine, offer.Id).message;
                MobileGameController.Instance?.OnStateUpdated?.Invoke();
                Refresh();
            }
            else
            {
                HireRow(selectedRowIndex, false);
            }
        }

        /// <summary>Two contract options per NPC (ADR-0026 §5).</summary>
        private static (ContractKind, int, ContractKind, int) Options(NpcProfile p) =>
            p.Tier == RoleTier.Cam ? (ContractKind.ChuKyLon, 2, ContractKind.ChuKyLon, 3)
            : p.IsMercenary ? (ContractKind.TheoDot, 3, ContractKind.ChuKyNho, 1)
            : (ContractKind.ChuKyNho, 1, ContractKind.ChuKyNho, 2);

        private static string Label(ContractKind k, int n) => k switch
        {
            ContractKind.TheoDot => $"Ký {n} đợt",
            ContractKind.ChuKyLon => $"Ký {n} CK lớn",
            _ => $"Ký {n} chu kỳ",
        };

        public void HireRow(int i) => HireRow(i, true);

        private static int RenewTerms(NpcContract c) => c.Kind == ContractKind.TheoDot ? 3 : 1;

        /// <summary>Gia hạn với giá thị trường thấp hơn — ép giá, hảo cảm giảm (ADR-0026).</summary>
        public void ForceDownRow(int i)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            var offers = Current();
            if (engine == null || i >= offers.Length) return;
            var c = engine.NpcRelations.ContractOf(offers[i].Id);
            if (c == null) return;
            lastMessage = NpcContracts.Renew(engine, offers[i].Id, RenewTerms(c), forceDown: true).message;
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Refresh();
        }

        public void HireRow(int i, bool primary)
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            var offers = Current();
            if (engine == null || i >= offers.Length) return;
            var id = offers[i].Id;
            var prof = NpcContracts.Profile(id);
            (bool success, string message) res;
            var running = engine.NpcRelations.ContractOf(id);
            if (running != null)
                res = primary ? NpcContracts.Renew(engine, id, RenewTerms(running)) : NpcContracts.Terminate(engine, id);
            else if (prof == null) res = (false, "Không có người này.");
            else if (!RecruitmentRules.IsOpen(offers[i].Where, engine.Clock.CurrentHour)) res = RecruitmentRules.Hire(engine, id); // trả lời "chỉ tuyển ở chợ …"
            else
            {
                var (k1, n1, k2, n2) = Options(prof);
                res = primary ? NpcContracts.Sign(engine, id, k1, n1) : NpcContracts.Sign(engine, id, k2, n2);
            }
            lastMessage = res.message;
            Debug.Log($"[Recruit] {res.message}");
            MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Refresh();
        }
    }
}
