using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// GDD 23 "Thông tin &amp; Tình báo" at the Bàn Tình Báo (farm info station): buy intel levels 1–4 in order
    /// (150/300/450/600G), see only what was bought, split into Cảnh báo / Tin đã xác nhận / Suy đoán. Level 5 comes from
    /// Bí nhân or events. Core: IntelBureau. Built with the DayMarketPopup frame helpers.
    /// </summary>
    public sealed class IntelPopup : MonoBehaviour
    {
        public static IntelPopup Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

        private GameObject panel;
        private TextMeshProUGUI header, footer, warnings, confirmed, speculation, status;
        private Button buy;
        private TextMeshProUGUI buyText;
        private readonly Image[] levelPips = new Image[IntelBureau.FullLevel];
        private string lastMessage = "";

        public static void Open()
        {
            if (Instance == null)
            {
                var go = new GameObject("IntelPopupCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = go.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 115;
                var sc = go.GetComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1280f, 720f);
                sc.matchWidthOrHeight = 0.5f;
                Instance = go.AddComponent<IntelPopup>();
                Instance.Build(go.transform);
            }
            Instance.lastMessage = "";
            Instance.panel.SetActive(true);
            Instance.panel.transform.SetAsLastSibling();
            Instance.Refresh();
        }

        public void Close() { if (panel != null) panel.SetActive(false); }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private static TextMeshProUGUI Block(Transform parent, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            var t = DayMarketPopup.Text(parent, pos, size, fontSize, TextAlignmentOptions.TopLeft);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Truncate;
            t.color = color;
            return t;
        }

        private void Build(Transform root)
        {
            panel = DayMarketPopup.BuildStyledPopupFrame(root, "IntelPopup", new Vector2(740f, 548f), out _, out var headerBanner, out var footerBanner);

            header = DayMarketPopup.Text(headerBanner, new Vector2(-64f, 0f), new Vector2(540f, 38f), 18, TextAlignmentOptions.Left);
            header.color = new Color(1f, 0.94f, 0.68f);

            for (int i = 0; i < IntelBureau.FullLevel; i++)
            {
                var pipGo = new GameObject("IntelPip" + i, typeof(RectTransform), typeof(Image));
                pipGo.transform.SetParent(headerBanner, false);
                var prt = (RectTransform)pipGo.transform;
                prt.anchoredPosition = new Vector2(186f + i * 18f, 0f);
                prt.sizeDelta = new Vector2(14f, 14f);
                levelPips[i] = pipGo.GetComponent<Image>();
                levelPips[i].sprite = MythUi.Kit("quest_step_pip_locked");
                levelPips[i].preserveAspect = true;
                levelPips[i].raycastTarget = false;
            }

            var close = DayMarketPopup.Button(headerBanner, new Vector2(316f, 0f), new Vector2(68f, 32f), out var closeText);
            closeText.text = "Đóng";
            close.onClick.AddListener(Close);

            // Section 1: CẢNH BÁO
            var warnCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, 158f), new Vector2(684f, 102f));
            var warnBg = warnCard.GetComponent<Image>();
            if (warnBg != null) warnBg.color = new Color(0.36f, 0.18f, 0.14f, 0.96f);
            var warnLabel = DayMarketPopup.Text(warnCard.transform, new Vector2(0f, 34f), new Vector2(656f, 22f), 15, TextAlignmentOptions.Left);
            warnLabel.text = "<b>CẢNH BÁO KHẨN</b>";
            warnLabel.color = new Color(1f, 0.62f, 0.46f);
            warnings = Block(warnCard.transform, new Vector2(0f, -10f), new Vector2(656f, 66f), 13.5f, new Color(1f, 0.85f, 0.78f));

            // Section 2: TIN ĐÃ XÁC NHẬN
            var confCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, 38f), new Vector2(684f, 122f));
            var confBg = confCard.GetComponent<Image>();
            if (confBg != null) confBg.color = new Color(0.18f, 0.28f, 0.18f, 0.96f);
            var confLabel = DayMarketPopup.Text(confCard.transform, new Vector2(0f, 44f), new Vector2(656f, 22f), 15, TextAlignmentOptions.Left);
            confLabel.text = "<b>TIN ĐÃ XÁC NHẬN</b>";
            confLabel.color = new Color(0.68f, 0.94f, 0.68f);
            confirmed = Block(confCard.transform, new Vector2(0f, -10f), new Vector2(656f, 84f), 13.5f, new Color(0.88f, 0.98f, 0.88f));

            // Section 3: SUY ĐOÁN
            var specCard = DayMarketPopup.MakeSlotCard(panel.transform, new Vector2(0f, -76f), new Vector2(684f, 92f));
            var specBg = specCard.GetComponent<Image>();
            if (specBg != null) specBg.color = new Color(0.20f, 0.22f, 0.28f, 0.96f);
            var specLabel = DayMarketPopup.Text(specCard.transform, new Vector2(0f, 30f), new Vector2(656f, 22f), 15, TextAlignmentOptions.Left);
            specLabel.text = "<b>SUY ĐOÁN</b>  <size=12><i>(độ tin cậy 70–85%)</i></size>";
            specLabel.color = new Color(0.76f, 0.84f, 0.96f);
            speculation = Block(specCard.transform, new Vector2(0f, -10f), new Vector2(656f, 56f), 13.5f, new Color(0.84f, 0.88f, 0.94f));
            speculation.fontStyle = FontStyles.Italic;

            buy = DayMarketPopup.Button(panel.transform, new Vector2(-176f, -158f), new Vector2(330f, 38f), out buyText);
            buy.onClick.AddListener(BuyNext);
            status = Block(panel.transform, new Vector2(170f, -158f), new Vector2(336f, 38f), 12.5f, new Color(0.96f, 0.90f, 0.74f));

            footer = DayMarketPopup.Text(footerBanner, Vector2.zero, new Vector2(680f, 32f), 13, TextAlignmentOptions.Center);
            footer.color = new Color(0.96f, 0.90f, 0.74f);
        }

        private static string Bullets(List<string> lines, string empty) =>
            lines.Count == 0 ? $"<i>{empty}</i>" : string.Join("\n", lines.Select(l => "• " + l));

        private void Refresh()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null || panel == null) return;
            var report = engine.Intel.BuildReport(engine);
            bool hasDesk = engine.Defense != null
                && engine.Defense.Buildings.Exists(b => b.BlueprintId == IntelBureau.DeskBlueprintId);
            int qToday = engine.Intel.QuestionsDay == engine.Clock.CurrentDay ? engine.Intel.QuestionsToday : 0;

            header.text = $"<b>BÀN TÌNH BÁO</b>  ·  Chu kỳ {report.CycleNumber}  ·  Tin cấp {report.Level}/5  ·  Lượt hỏi hôm nay: <b>{qToday}/{IntelBureau.QuestionsPerDay}</b>   •   <color=#FFE270>{engine.Farm.Gold:N0} G</color>";
            var pipDone = MythUi.Kit("quest_step_pip_done");
            var pipLocked = MythUi.Kit("quest_step_pip_locked");
            for (int i = 0; i < levelPips.Length; i++)
            {
                if (levelPips[i] != null)
                    levelPips[i].sprite = i < report.Level ? pipDone : pipLocked;
            }
            warnings.text = Bullets(report.Warnings, "Không có cảnh báo.");
            confirmed.text = Bullets(report.Confirmed, "Chưa có tin xác nhận — mua tin cấp 2 trở lên.");
            speculation.text = Bullets(report.Speculation, report.Level >= IntelBureau.FullLevel
                ? "Không còn suy đoán — đã có tin toàn phần." : "Chưa có suy đoán — mua tin cấp 1.");

            int next = report.Level + 1;
            if (!hasDesk)
            {
                buyText.text = "Cần xây Bàn Tình Báo (bản vẽ Tinh · chợ đêm)";
                buy.interactable = false;
                status.text = $"Lượt hỏi hôm nay: {qToday}/{IntelBureau.QuestionsPerDay}. Mua bản vẽ Bàn Tình Báo ở chợ đêm và xây tại trại để hỏi tin.";
            }
            else if (qToday >= IntelBureau.QuestionsPerDay)
            {
                buyText.text = $"Đã hết lượt hỏi hôm nay ({qToday}/{IntelBureau.QuestionsPerDay})";
                buy.interactable = false;
                status.text = $"Mỗi ngày Bàn Tình Báo chỉ giải mã tối đa {IntelBureau.QuestionsPerDay} lượt tin.";
            }
            else if (next <= IntelBureau.MaxPurchasableLevel)
            {
                int cost = IntelBureau.CostOf(next);
                buyText.text = $"Mua tin cấp {next} — {cost} G ({qToday}/{IntelBureau.QuestionsPerDay} lượt)";
                buy.interactable = engine.Farm.Gold >= cost;
                status.text = $"Lượt hỏi hôm nay: {qToday}/{IntelBureau.QuestionsPerDay}. Tin hết hạn khi sang chu kỳ mới.";
            }
            else
            {
                buyText.text = report.Level >= IntelBureau.FullLevel ? "Đã có tin toàn phần" : "Cấp 5: chỉ từ Bí nhân / sự kiện";
                buy.interactable = false;
                status.text = $"Lượt hỏi hôm nay: {qToday}/{IntelBureau.QuestionsPerDay}. Tin hết hạn khi sang chu kỳ mới.";
            }
            footer.text = string.IsNullOrEmpty(lastMessage)
                ? "Thông tin không tăng tỉ lệ thắng — nó giúp có mặt đúng chỗ, giữ đúng tài nguyên."
                : lastMessage;
        }

        private void BuyNext()
        {
            var engine = MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;
            if (engine == null) return;
            var res = engine.Intel.Purchase(engine, engine.Intel.Level + 1);
            lastMessage = res.message;
            PlayerInteractionController.Instance?.ShowFeedback(lastMessage);
            if (res.success) MobileGameController.Instance?.OnStateUpdated?.Invoke();
            Refresh();
        }
    }
}
