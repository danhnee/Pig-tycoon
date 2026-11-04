using System.Linq;
using PigTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// Season 1 Quest Tracker + 3-Tab Journal ([N]):
    /// - Tab 0: Nhiệm Vụ Chính Tuyến & Việc Phụ (Main Story & Side Quests)
    /// - Tab 1: Tác Vụ Hàng Ngày (Daily Farm Checklist: feeding, watering, cleaning, fence check, villager talk)
    /// - Tab 2: Sổ Tay Nhân Vật & Thân Mật NPC (Villager Relationships 0-100 & Favors)
    /// </summary>
    public sealed class QuestTrackerPanel : MonoBehaviour
    {
        public static QuestTrackerPanel Instance { get; private set; }

        private RectTransform tracker;
        private Image trackerBg;
        private Image trackerInnerImg;
        private TextMeshProUGUI trackerText;
        private GameObject journal;
        private TextMeshProUGUI journalText;
        private Button[] endingButtons = new Button[3];
        private Button[] tabButtons = new Button[3];
        private int activeTab;
        private float refreshTimer;
        private string lastActiveId = "";
        private GameEngine hooked;
        private bool minimized = true; // #146 item 3: collapsed to 1 line by default, tap to expand

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded += (_, __) => Ensure();
            Ensure();
        }

        public static void Ensure()
        {
            if (Instance != null) return;
            var canvas = VillageBarterPopup.FindScreenSpaceCanvas();
            if (canvas == null) return;
            var trackerCanvas = OwnCanvas("QuestTrackerCanvas", canvas.sortingOrder - 1);
            var journalCanvas = OwnCanvas("QuestJournalCanvas", canvas.sortingOrder + 20);
            // own root canvas: nested under the tracker canvas the journal sat ~200 px too high at 1080p (tabs and Đóng off screen, 3 Nov UI probe)
            Instance = trackerCanvas.gameObject.AddComponent<QuestTrackerPanel>();
            Instance.Build(trackerCanvas.transform, journalCanvas.transform);
        }

        private static Canvas OwnCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.overrideSorting = true;
            c.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return c;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Build(Transform root, Transform journalRoot)
        {
            var go = new GameObject("QuestTracker", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);
            tracker = (RectTransform)go.transform;
            // #146 item 3: Stardew-style compact objective line tucked under top-left clock/bars, collapsed by default
            tracker.anchorMin = tracker.anchorMax = new Vector2(0f, 1f);
            tracker.pivot = new Vector2(0f, 1f);
            tracker.anchoredPosition = new Vector2(6f, -204f);
            tracker.sizeDelta = new Vector2(252f, 38f);
            trackerBg = go.GetComponent<Image>();
            trackerBg.sprite = UISpriteLoader.GetFrameSlotNormal();
            trackerBg.type = Image.Type.Sliced;
            trackerBg.color = new Color(0.18f, 0.14f, 0.10f, 0.88f);
            trackerBg.raycastTarget = true;

            var trackerInner = new GameObject("TrackerInner", typeof(RectTransform), typeof(Image));
            trackerInner.transform.SetParent(go.transform, false);
            var tirt = (RectTransform)trackerInner.transform;
            tirt.anchorMin = Vector2.zero;
            tirt.anchorMax = Vector2.one;
            tirt.offsetMin = new Vector2(4f, 2f);
            tirt.offsetMax = new Vector2(-4f, -2f);
            trackerInnerImg = trackerInner.GetComponent<Image>();
            trackerInnerImg.sprite = UISpriteLoader.GetFrameParchment();
            trackerInnerImg.type = Image.Type.Sliced;
            trackerInnerImg.color = new Color(0.18f, 0.14f, 0.10f, 0.92f);
            trackerInnerImg.raycastTarget = false;
            trackerInnerImg.enabled = false;

            trackerText = DayMarketPopup.Text(trackerInner.transform, Vector2.zero, new Vector2(244f, 34f), 12f, TextAlignmentOptions.MidlineLeft);
            trackerText.textWrappingMode = TextWrappingModes.NoWrap;
            trackerText.overflowMode = TextOverflowModes.Ellipsis;
            trackerText.margin = new Vector4(4f, 1f, 4f, 1f);
            trackerText.raycastTarget = false;

            // (3 Nov) the strip's own "N" button duplicated the top-right journal medallion [N]; removed

            // Tapping the tracker strip expands/collapses the objective details (#146 item 3: "chạm để mở")
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                minimized = !minimized;
                tracker.sizeDelta = minimized ? new Vector2(252f, 38f) : new Vector2(268f, 102f);
                if (trackerBg != null)
                {
                    trackerBg.sprite = minimized ? UISpriteLoader.GetFrameSlotNormal() : UISpriteLoader.GetFrameWoodPanel();
                    trackerBg.type = Image.Type.Sliced;
                    trackerBg.color = minimized ? new Color(0.18f, 0.14f, 0.10f, 0.88f) : new Color(0.88f, 0.80f, 0.70f, 0.94f);
                }
                if (trackerInnerImg != null) trackerInnerImg.enabled = !minimized;
                if (trackerText != null)
                {
                    trackerText.alignment = minimized ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.TopLeft;
                    trackerText.textWrappingMode = minimized ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
                }
                Refresh();
            });

            // Full 3-Tab Quest & Daily Tasks Journal Modal [N]
            journal = DayMarketPopup.BuildStyledPopupFrame(journalRoot, "QuestJournal", new Vector2(820f, 560f),
                out var bodyParchment, out var headerBanner, out var footerBanner);

            var hdrTitle = DayMarketPopup.Text(headerBanner, new Vector2(-24f, 0f), new Vector2(660f, 38f), 19, TextAlignmentOptions.Left);
            hdrTitle.color = new Color(1f, 0.94f, 0.68f);
            hdrTitle.text = "<b>SỔ TAY TRANG TRẠI & NHIỆM VỤ — MÙA 1 \"SÁU VỚI MỘT\"</b>";

            var close = DayMarketPopup.Button(headerBanner, new Vector2(354f, 0f), new Vector2(68f, 32f), out var ct);
            ct.text = "Đóng";
            close.onClick.AddListener(() => journal.SetActive(false));

            string[] tabNames = { "1. Nhiệm Vụ Chính & Phụ", "2. Tác Vụ Hàng Ngày", "3. Thân Mật Dân Làng" };
            for (int t = 0; t < 3; t++)
            {
                int tabIdx = t;
                tabButtons[t] = DayMarketPopup.Button(journal.transform, new Vector2(-246f + t * 246f, 202f), new Vector2(232f, 34f), out var tt);
                tt.text = tabNames[t];
                tt.fontSize = 14f;
                tt.raycastTarget = false;
                tabButtons[t].onClick.AddListener(() =>
                {
                    if (activeTab == tabIdx)
                    {
                        showExpandedQuestHint = !showExpandedQuestHint;
                        ToastOverlay.Show("Đang xem Nhiệm Vụ Chính & Phụ — Bấm vào nhiệm vụ để xem chi tiết.");
                    }
                    else
                    {
                        activeTab = tabIdx;
                        showExpandedQuestHint = false;
                        ToastOverlay.Show($"Đã chuyển sang thẻ: {tabNames[tabIdx]}");
                    }
                    Refresh();
                });
            }

            journalText = DayMarketPopup.Text(bodyParchment, new Vector2(0f, -18f), new Vector2(752f, 380f), 14, TextAlignmentOptions.TopLeft);
            journalText.textWrappingMode = TextWrappingModes.Normal;
            journalText.overflowMode = TextOverflowModes.Truncate;

            string[] endings = { "Cắt kênh, giữ trại", "Giữ máy, lập hồ sơ", "Mang vòng về nhà" };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                endingButtons[i] = DayMarketPopup.Button(footerBanner, new Vector2(-250f + i * 250f, 0f), new Vector2(228f, 30f), out var et);
                et.text = endings[i];
                endingButtons[i].onClick.AddListener(() => ChooseEnding(k));
            }
            journal.SetActive(false);
        }

        private static GameEngine Engine => MobileGameController.Instance != null ? MobileGameController.Instance.Engine : null;

        /// <summary>The completion line in the active protagonist's voice (An's route never shows Khoa's notebook line).</summary>
        static string VoiceLine(QuestDefinition q)
        {
            bool an = Engine?.Character?.Identity == PlayerId.An;
            string line = an ? q.AnLine : q.KhoaLine;
            return string.IsNullOrEmpty(line) ? "" : "\n<i>" + line + "</i>";
        }

        private void Update()
        {
            var engine = Engine;
            if (engine != null && hooked != engine)
            {
                hooked = engine;
                engine.Quests.QuestCompleted += q =>
                    PlayerInteractionController.Instance?.ShowFeedback($"Xong nhiệm vụ: {q.Title}  (+{StoryQuestBook.RewardText(q)})" + VoiceLine(q));
            }
            if (tracker != null)
            {
                bool modalOpen = UISpriteLoader.IsAnyModalOrDialogueOpen() || (journal != null && journal.activeSelf);
                if (tracker.gameObject.activeSelf == modalOpen) tracker.gameObject.SetActive(!modalOpen);
            }
            if (Input.GetKeyDown(KeyCode.N)) ToggleJournal();
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.5f;
            Refresh();
        }

        private bool showExpandedQuestHint;

        public void Open()
        {
            if (journal != null)
            {
                journal.SetActive(true);
                journal.transform.SetAsLastSibling();
                Refresh();
            }
        }

        public void CloseJournal() { if (journal != null) journal.SetActive(false); }

        public void ToggleJournal()
        {
            journal.SetActive(!journal.activeSelf);
            if (journal.activeSelf) journal.transform.SetAsLastSibling();
            Refresh();
        }

        private void Refresh()
        {
            var engine = Engine;
            if (engine == null || trackerText == null) return;
            var book = engine.Quests;
            var active = book.ActiveMain;
            var view = book.View(engine, active);

            int livingPigs = engine.Pigs.Count(p => p.IsAlive);
            int fedPigs = engine.Pigs.Count(p => p.IsAlive && p.Hunger >= 60f);
            int cleanHabit = engine.Farm.HabitatIndex;

            if (view == null)
            {
                trackerText.text = book.Ending != StoryEndingId.ChuaChon ? "<b>Mùa 1 đã khép lại.</b>\nĐàn vẫn cần ăn cử sáng." : "";
            }
            else if (minimized)
            {
                string nextStep = view.Objective;
                foreach (var (label, done) in view.Steps) { if (!done) { nextStep = label; break; } }
                trackerText.text = $"<color=#FFE270><b>{view.Title}</b></color>\n<size=10.5>{nextStep}</size>";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.Append($"<color=#FFE270><b>{view.Title}</b></color>\n<size=11.2>{view.Objective}</size>\n");
                foreach (var (label, done) in view.Steps) sb.Append(done ? $"<color=#9be37b>[√] {label}</color>\n" : $"• {label}\n");
                sb.Append($"<size=10.8><color=#86EFAC>Việc ngày: Cho ăn {fedPigs}/{livingPigs} · Sinh cảnh {cleanHabit}</color> · <color=#d8c49a>[N] Sổ tay</color></size>");
                trackerText.text = sb.ToString();
                if (view.Id != lastActiveId && lastActiveId.Length > 0)
                    PlayerInteractionController.Instance?.ShowFeedback($"Nhiệm vụ mới: {view.Title}");
                lastActiveId = view.Id;
            }

            if (!journal.activeSelf) return;

            var j = new System.Text.StringBuilder();
            if (activeTab == 0)
            {
                j.Append("<size=16><color=#FFE270><b>I. TUYẾN NHIỆM VỤ CHÍNH (MÙA 1 — \"SÁU VỚI MỘT\")</b></color></size>\n\n");
                foreach (var q in StoryQuestBook.Main)
                {
                    bool done = book.Completed.Contains(q.Id);
                    bool cur = active != null && active.Id == q.Id;
                    if (!done && !cur) continue;
                    j.Append(done
                        ? $"<color=#9be37b>[√] {q.Title}</color>\n"
                        : $"<color=#FFE270><b>[►] {q.Title}</b></color> — {view?.Objective}\n");
                }
                var side = book.OpenSide(engine).ToList();
                if (side.Count > 0)
                {
                    j.Append("\n<size=15><color=#FFE270><b>II. ỦY THÁC & VIỆC PHỤ ĐANG MỞ</b></color></size>\n");
                    foreach (var q in side) j.Append($"• <b>{q.Title}</b> — {q.Objective}\n");
                }
                if (book.Log.Count > 0)
                    j.Append("\n<size=12.5><color=#c8b48a><b>Nhật ký gần nhất:</b>\n" + string.Join("\n", book.Log.Skip(System.Math.Max(0, book.Log.Count - 4))) + "</color></size>");
                if (showExpandedQuestHint)
                    j.Append("\n<size=12><color=#86EFAC><i>• Gợi ý chi tiết: Bấm vào mục tiêu trên dải theo dõi góc trái hoặc phím [H] Vào Nhà Chính để kiểm tra sổ sách & nhiệm vụ.</i></color></size>");
            }
            else if (activeTab == 1)
            {
                j.Append($"<size=16><color=#FFE270><b>BẢNG TÁC VỤ HÀNG NGÀY (Ngày {engine.Clock.CurrentDay} · {engine.Clock.CurrentHour:00}:00)</b></color></size>\n\n");
                bool allFed = livingPigs > 0 && fedPigs >= livingPigs;
                bool goodMood = livingPigs > 0 && engine.Pigs.Where(p => p.IsAlive).Average(p => p.Mood) >= 60.0;
                bool cleanEnv = cleanHabit >= 70;
                bool fullStamina = engine.Character != null && !engine.Character.Stamina.IsFatigued;

                j.Append(allFed
                    ? $"<color=#9be37b>[√] 1. Cho đàn heo ăn cử sáng & chiều ({fedPigs}/{livingPigs} con no bụng)</color>\n"
                    : $"<color=#FDE047>[ ] 1. Cho đàn heo ăn cử sáng & chiều (Hiện tại: {fedPigs}/{livingPigs} con no — đổ cám vào Máng Ăn)</color>\n");
                j.Append(goodMood
                    ? "<color=#9be37b>[√] 2. Chải lông / Giữ tinh thần đàn heo ổn định (Tinh thần TB ≥ 60)</color>\n"
                    : "<color=#FDE047>[ ] 2. Chải lông & cấp nước sạch tại Máng Nước để nâng tinh thần đàn heo</color>\n");
                j.Append(cleanEnv
                    ? $"<color=#9be37b>[√] 3. Giữ vệ sinh chuồng trại & Sinh Cảnh Trong Lành (Sinh Cảnh: {cleanHabit}/100)</color>\n"
                    : $"<color=#FDE047>[ ] 3. Dọn phân heo & rắc vôi bột nâng Chỉ số Sinh Cảnh (Hiện tại: {cleanHabit}/100, mục tiêu ≥ 70)</color>\n");
                j.Append(fullStamina
                    ? $"<color=#9be37b>[√] 4. Quản lý Thể Lực & Chống Chịu trước giờ tuần đêm ({engine.Character.Stamina.CurrentStamina:0}/{engine.Character.GetEffectiveMaxStamina():0} TL)</color>\n"
                    : "<color=#FCA5A5>[!] 4. Nhân vật đang Mệt Mỏi — hãy dùng Bình Hồi Lực (F5) hoặc nghỉ ngơi tại Nhà Chính</color>\n");
                j.Append($"• <b>{WaveForecast.Major(engine).Text}</b>{(WaveForecast.Minor(engine).known ? " · " + WaveForecast.Minor(engine).text : "")}\n");
                j.Append("• <b>Gợi ý:</b> hảo cảm chỉ tăng khi làm nhiệm vụ riêng, việc nhờ mỗi chu kỳ và làm trọn hợp đồng — trò chuyện thì không.");
            }
            else
            {
                j.Append("<size=16><color=#FFE270><b>SỔ TAY QUAN HỆ & THÂN MẬT DÂN LÀNG (0–100 TIM)</b></color></size>\n\n");
                string[] npcIds = { CastIds.BaSau, CastIds.Man, CastIds.TamVa, CastIds.Dung, CastIds.Cuc, CastIds.Le, CastIds.AnhCa, CastIds.OngTon, CastIds.AnhTu };
                foreach (var id in npcIds)
                {
                    var rel = engine.RelationOf(id);
                    if (!rel.Hireable)
                    {
                        j.Append($"• <b>{rel.Name}</b> ({rel.Role})\n");
                        continue;
                    }
                    int bars = Mathf.Clamp(Mathf.FloorToInt(rel.Affinity / 10f), 0, 10);
                    string barStr = new string('█', bars) + new string('░', 10 - bars);
                    j.Append($"• <b>{rel.Name}</b> ({rel.Role}): <color=#F472B6>{barStr}</color> {rel.Label}\n   <size=13>{rel.Status}</size>\n");
                }
            }

            journalText.text = j.ToString();
            bool canChoose = book.CanChooseEnding(engine);
            foreach (var b in endingButtons) b.gameObject.SetActive(canChoose);
        }

        private void ChooseEnding(int i)
        {
            var engine = Engine;
            if (engine == null) return;
            var ending = i == 0 ? StoryEndingId.Ending_8_1_GiuTraiThaoDuongGui : i == 1 ? StoryEndingId.Ending_8_2_GiuMayLamHoSo : StoryEndingId.Ending_8_3_MangVongVeNha;
            var res = engine.Quests.ChooseEnding(engine, ending);
            PlayerInteractionController.Instance?.ShowFeedback(res.message);
            Refresh();
        }
    }
}
