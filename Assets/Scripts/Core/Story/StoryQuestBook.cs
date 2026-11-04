using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    /// <summary>Things the player does that quest steps listen for. Presentation and engine call StoryQuestBook.Signal.</summary>
    public enum QuestSignal
    {
        FeedPoured,
        WaterRefilled,
        FenceTempPatched,
        FenceRepaired,
        HerdRecalled,
        HerdGathered90,
        CapacityBoardRead,
        DayMarketSale,
        NightMarketTrade,
        MysticTrade,
        NpcHired,
        AppraisalUsed,
        StructureBuilt,
        HouseBoxInspected,
        TalkBaSau,
        TalkTamVa,
        TalkLe,
        TalkOngTon,
        TalkDung,
        TalkCuc,
        TalkMan,
        TalkAnhCa,
        ForestEventResolved,
        PetAdopted,
        AuctionWon
    }

    public sealed class QuestStep
    {
        public string Label = "";
        public Func<GameEngine, StoryQuestBook, bool> Done = (_, __) => false;
    }

    public sealed class QuestDefinition
    {
        public string Id = "";
        public string Title = "";
        public string Trigger = "";          // story doc "Điều kiện mở"
        public string Objective = "";        // story doc "Việc người chơi làm"
        public string KhoaLine = "";         // one line of Khoa's notebook voice shown on completion
        public string AnLine = "";           // An's route (canon 5B); empty → no line on An's route, never Khoa's voice
        public int RewardGold;
        public int RewardExp;
        public string RewardNote = "";
        public bool IsSide;
        public Func<GameEngine, StoryQuestBook, bool> Unlocks = (_, __) => true;
        public List<QuestStep> Steps = new List<QuestStep>();
    }

    public sealed class QuestView
    {
        public string Id = "";
        public string Title = "";
        public string Objective = "";
        public bool IsSide;
        public List<(string label, bool done)> Steps = new List<(string, bool)>();
        public string Reward = "";
    }

    /// <summary>
    /// Season 1 quest line, rows Q01–Q20 and the side quests of Docs/COT_TRUYEN_PIG_TYCOON.md (lines 344–531): titles,
    /// unlock conditions, objectives and rewards copied from the doc. Steps read real game state or signals recorded while
    /// the quest is active, so nothing completes by itself. Main quests run one at a time in order.
    /// </summary>
    [Serializable]
    public sealed class StoryQuestBook
    {
        public static readonly IReadOnlyList<QuestDefinition> Main = BuildMain();
        public static readonly IReadOnlyList<QuestDefinition> Side = BuildSide();

        public HashSet<string> Completed { get; } = new HashSet<string>();
        /// <summary>Signals received per quest id while that quest was active (main) or unlocked (side).</summary>
        public Dictionary<string, HashSet<QuestSignal>> Received { get; } = new Dictionary<string, HashSet<QuestSignal>>();
        public StoryEndingId Ending { get; private set; } = StoryEndingId.ChuaChon;
        public List<string> Log { get; } = new List<string>();

        public event Action<QuestDefinition>? QuestCompleted;

        public QuestDefinition? ActiveMain => Main.FirstOrDefault(q => !Completed.Contains(q.Id));

        public bool Has(string questId, QuestSignal s) => Received.TryGetValue(questId, out var set) && set.Contains(s);

        public IEnumerable<QuestDefinition> OpenSide(GameEngine engine) =>
            Side.Where(q => !Completed.Contains(q.Id) && q.Unlocks(engine, this));

        public void Signal(GameEngine engine, QuestSignal s)
        {
            var main = ActiveMain;
            if (main != null && main.Unlocks(engine, this)) Add(main.Id, s);
            foreach (var q in OpenSide(engine)) Add(q.Id, s);
            Evaluate(engine);
        }

        private void Add(string id, QuestSignal s)
        {
            if (!Received.TryGetValue(id, out var set)) Received[id] = set = new HashSet<QuestSignal>();
            set.Add(s);
        }

        /// <summary>Completes every quest whose steps all hold. Main quests chain, so one call can finish several.</summary>
        public List<QuestDefinition> Evaluate(GameEngine engine)
        {
            var done = new List<QuestDefinition>();
            for (int guard = 0; guard < Main.Count; guard++)
            {
                var q = ActiveMain;
                if (q == null || q.Id == "Q20" || !q.Unlocks(engine, this) || !q.Steps.All(st => st.Done(engine, this))) break;
                Finish(engine, q);
                done.Add(q);
            }
            foreach (var q in OpenSide(engine).ToList())
            {
                if (q.Steps.All(st => st.Done(engine, this))) { Finish(engine, q); done.Add(q); }
            }
            return done;
        }

        /// <summary>Q20 ends with a choice (story 8.1–8.3); it can be made once its steps hold.</summary>
        public bool CanChooseEnding(GameEngine engine)
        {
            var q = ActiveMain;
            return q != null && q.Id == "Q20" && q.Unlocks(engine, this) && q.Steps.All(st => st.Done(engine, this));
        }

        public (bool ok, string message) ChooseEnding(GameEngine engine, StoryEndingId ending)
        {
            if (ending == StoryEndingId.ChuaChon || !CanChooseEnding(engine)) return (false, "Chưa tới lúc chọn.");
            Ending = ending;
            engine.Story.ChosenEnding = ending;
            // the ending scenes (NarrativeContentChapter4 E81/E82/E83) wait on these flags, whichever UI made the choice
            engine.NarrativeState.Flags.Add("s1_ending_chosen");
            engine.NarrativeState.Flags.Add(ending switch
            {
                StoryEndingId.Ending_8_1_GiuTraiThaoDuongGui => "s1_ending_8_1",
                StoryEndingId.Ending_8_2_GiuMayLamHoSo => "s1_ending_8_2",
                _ => "s1_ending_8_3"
            });
            Finish(engine, Main.First(q => q.Id == "Q20"));
            return (true, ending switch
            {
                StoryEndingId.Ending_8_1_GiuTraiThaoDuongGui => "Cắt kênh gói thứ bảy. Đàn thôi chừa chỗ.",
                StoryEndingId.Ending_8_2_GiuMayLamHoSo => "Giữ máy, lập hồ sơ.",
                _ => "Mang vòng về nhà."
            });
        }

        private void Finish(GameEngine engine, QuestDefinition q)
        {
            Completed.Add(q.Id);
            if (q.RewardGold > 0) engine.Ledger.Earn(q.RewardGold, LedgerReason.ContractReward, "quest_" + q.Id);
            if (q.RewardExp > 0) engine.Farm.AddFarmExp(q.RewardExp);
            var legacy = engine.Story.Quests.Find(r => r.QuestId == q.Id);
            if (legacy != null) { legacy.IsUnlocked = true; legacy.IsCompleted = true; legacy.Title = q.Title; legacy.ObjectiveSummary = q.Objective; }
            Log.Add($"Ngày {engine.Clock.CurrentDay}: xong {q.Id} {q.Title}");
            QuestCompleted?.Invoke(q);
            engine.FireNarrative(NarrativeTrigger.QuestCompleted, q.Id);
        }

        public QuestView? View(GameEngine engine, QuestDefinition? q)
        {
            if (q == null) return null;
            var v = new QuestView
            {
                Id = q.Id,
                Title = q.Title,
                Objective = q.Unlocks(engine, this) ? q.Objective : "Chưa mở: " + q.Trigger,
                IsSide = q.IsSide,
                Reward = RewardText(q)
            };
            foreach (var st in q.Steps) v.Steps.Add((st.Label, q.Unlocks(engine, this) && st.Done(engine, this)));
            return v;
        }

        public static string RewardText(QuestDefinition q)
        {
            var parts = new List<string>();
            if (q.RewardGold > 0) parts.Add($"{q.RewardGold}G");
            if (q.RewardExp > 0) parts.Add($"{q.RewardExp} EXP");
            if (q.RewardNote.Length > 0) parts.Add(q.RewardNote);
            return string.Join(", ", parts);
        }

        /// <summary>Maps a world station id (PixelMythWorldBuilder.ConfigureStation) to the talk/visit signal it gives.</summary>
        public static QuestSignal? SignalForStation(string stationId) => stationId switch
        {
            "quan_ba_sau" => QuestSignal.TalkBaSau,
            "nha_tam_va" => QuestSignal.TalkTamVa,
            "nha_le" => QuestSignal.TalkLe,
            "nha_ong_ton" => QuestSignal.TalkOngTon,
            "nha_dung" or "kiem_dich_sang_loc" => QuestSignal.TalkDung,
            "nha_cuc" or "research_lab" or "research_house" => QuestSignal.TalkCuc,
            "nha_man" => QuestSignal.TalkMan,
            "nha_anh_ca" => QuestSignal.TalkAnhCa,
            "main_house" => QuestSignal.HouseBoxInspected,
            "capacity_board" => QuestSignal.CapacityBoardRead,
            _ => null
        };

        // ------------------------------------------------------------------ data

        private static QuestStep Sig(string label, string id, QuestSignal s) =>
            new QuestStep { Label = label, Done = (e, b) => b.Has(id, s) };

        private static QuestStep State(string label, Func<GameEngine, bool> f) =>
            new QuestStep { Label = label, Done = (e, b) => f(e) };

        private static bool Done(StoryQuestBook b, params string[] ids) => ids.All(b.Completed.Contains);

        private static List<QuestDefinition> BuildMain()
        {
            var list = new List<QuestDefinition>
            {
                new QuestDefinition
                {
                    KhoaLine = "Con heo chân trắng lại nằm ngửa giả chết dưới máng. Mận cười. Tôi ghi sổ.",
                    Id = "Q01", Title = "Cậu tới nhận trại", Trigger = "Đến trại",
                    Objective = "Rải cám, thay nước, chặn đoạn rào hỏng bằng rào tạm (30G/đoạn), dẫn một con heo lạc về vùng máng.",
                    RewardGold = 150, RewardExp = 80, RewardNote = "Hồ sơ chăm sóc tạm của ông Năm",
                    Steps =
                    {
                        Sig("Rải cám vào máng", "Q01", QuestSignal.FeedPoured),
                        Sig("Thay nước bồn", "Q01", QuestSignal.WaterRefilled),
                        Sig("Chặn đoạn rào hỏng bằng rào tạm", "Q01", QuestSignal.FenceTempPatched),
                        Sig("Dẫn heo lạc về (tù và)", "Q01", QuestSignal.HerdRecalled),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Một con heo đẩy bản kế hoạch dùng chung máng xuống bùn rồi nằm đè lên. Anh Ca đề xuất thêm cột “ý kiến người sử dụng”.",
                    Id = "Q02", Title = "Máng ít heo nhiều", Trigger = "Sau Q01",
                    Objective = "Đọc bảng sức chứa MIN, bổ sung đúng nguồn đang nghẽn, bố trí lối đi tới máng.",
                    RewardGold = 200, RewardExp = 100,
                    Unlocks = (e, b) => Done(b, "Q01"),
                    Steps =
                    {
                        Sig("Đọc bảng sức chứa", "Q02", QuestSignal.CapacityBoardRead),
                        Sig("Bổ sung cám", "Q02", QuestSignal.FeedPoured),
                        Sig("Bổ sung nước", "Q02", QuestSignal.WaterRefilled),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Đêm. Tôi thấy một vật sáng lơ lửng trên đồng. Cả xóm tắt đèn.",
                    Id = "Q03", Title = "Gió lốc hướng lên", Trigger = "Đợt lớn đầu tiên",
                    Objective = "Nạp trước, cài chế độ công trình, lùa đàn về Nơi Trú. Cứu heo bị trói hoặc giữ nhóm heo ở rìa.",
                    RewardGold = 250, RewardExp = 100, RewardNote = "Phiếu thu hoạch đầu tiên",
                    Unlocks = (e, b) => Done(b, "Q02"),
                    Steps =
                    {
                        Sig("Lùa đàn về Nơi Trú", "Q03", QuestSignal.HerdRecalled),
                        State("Sống qua đợt lớn đầu tiên", e => e.MajorWavesSurvived >= 1),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Người bán treo biển: “Chỉ nhận heo biết hát.” Ông bảo heo nào hoảng là hát.",
                    Id = "Q04", Title = "Chợ đêm không thối tiền", Trigger = "Trại cấp 3, đã xong Q03",
                    Objective = "Đọc “món muốn nhận” của một ô hàng và hoàn tất một giao dịch trao đổi hợp lệ.",
                    RewardGold = 200, RewardExp = 100,
                    Unlocks = (e, b) => Done(b, "Q03") && e.Farm.FarmLevel >= 3,
                    Steps = { Sig("Hoàn tất một giao dịch ở chợ đêm", "Q04", QuestSignal.NightMarketTrade) }
                },
                new QuestDefinition
                {
                    KhoaLine = "Lệ bán cho tôi “tin độc quyền” về “vụ trộm lớn mùa này”. Nó trông như một tờ giấy ghi “sẽ có rìu”.",
                    Id = "Q05", Title = "Bố của cái rìu", Trigger = "Boss Chúa Trộm Rìu Sắt, chu kỳ 5",
                    Objective = "Dồn Boss vào cửa hẹp, xử lý Kẻ Bắt Heo được gọi thêm mỗi 30 giây.",
                    RewardGold = 400, RewardExp = 160, RewardNote = "Bản sao phiếu giao hàng",
                    Unlocks = (e, b) => Done(b, "Q04"),
                    Steps = { State("Qua trận Boss chu kỳ 5 (thắng hay thua đều tính)", e => e.BossWavesFought >= 1) }
                },
                new QuestDefinition
                {
                    KhoaLine = "Cái hộp kỹ thuật trong nhà chính gõ cạch cạch dù máy phát đã tắt. Tám Vá bảo: “Sửa rào trước đã.”",
                    Id = "Q06", Title = "Phòng xây dựng thiếu người", Trigger = "Sau Q05",
                    Objective = "Thuê một người trực Phòng Xây Dựng cấp 1 (Kỹ Sư Xây Dựng 70%, NPC khác nghề 40%), rồi sửa đoạn rào hỏng từ Q01 ngoài đợt.",
                    RewardGold = 250, RewardExp = 120,
                    Unlocks = (e, b) => Done(b, "Q05"),
                    Steps =
                    {
                        State("Có người trực Phòng Xây Dựng", e => ConstructionHall.OnDutyNpc(e) != null),
                        Sig("Sửa hẳn đoạn rào hỏng (ngoài đợt)", "Q06", QuestSignal.FenceRepaired),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Ông Tồn đặt năm tờ phiếu lên bàn, tờ nào cũng có chữ ký nhận trước ngày xuất kho.",
                    Id = "Q07", Title = "Cái này mua bằng cái kia", Trigger = "Sau Q06",
                    Objective = "Lên ngân sách cho một chu kỳ, hoàn tất một hợp đồng chợ ngày và một giao dịch chợ đêm.",
                    RewardGold = 300, RewardExp = 120, RewardNote = "Quyền xem sổ đối chiếu của Tồn",
                    Unlocks = (e, b) => Done(b, "Q06"),
                    Steps =
                    {
                        Sig("Bán heo ở chợ ngày", "Q07", QuestSignal.DayMarketSale),
                        Sig("Một giao dịch chợ đêm", "Q07", QuestSignal.NightMarketTrade),
                        Sig("Gặp ông Tồn ở kho", "Q07", QuestSignal.TalkOngTon),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Ngón tay Cúc cứ đặt mãi lên khoảng trống bên cạnh con heo. “Thói quen,” cô nói.",
                    Id = "Q08", Title = "Con heo có dấu hỏi", Trigger = "Sau Q07",
                    Objective = "Đối chiếu hồ sơ một dòng gen “?”, phân biệt dấu hiệu cần thú y với dấu hiệu cần giám định.",
                    RewardGold = 300, RewardExp = 140, RewardNote = "Bản ghi hành vi",
                    Unlocks = (e, b) => Done(b, "Q07"),
                    Steps =
                    {
                        Sig("Xem hồ sơ với Cúc", "Q08", QuestSignal.TalkCuc),
                        new QuestStep { Label = "Giám định hoặc nhờ thú y", Done = (e, b) => b.Has("Q08", QuestSignal.AppraisalUsed) || b.Has("Q08", QuestSignal.TalkDung) },
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Anh Ca chúc mừng tôi “đủ điều kiện hỏi câu khó”. Tôi cất tờ chứng nhận cạnh tờ còn lại.",
                    Id = "Q09", Title = "Qua tám lần mới được hỏi", Trigger = "Cổng Giữa Game (≥ 8 đợt lớn + 1 điều kiện phụ)",
                    Objective = "Xem hồ sơ sống sót, chọn điểm yếu của trại để cải thiện.",
                    RewardGold = 350, RewardExp = 160,
                    Unlocks = (e, b) => Done(b, "Q08") && e.MajorWavesSurvived >= 8,
                    Steps =
                    {
                        Sig("Nhận giấy của anh Ca", "Q09", QuestSignal.TalkAnhCa),
                        Sig("Hỏi bà Sáu câu khó đầu tiên", "Q09", QuestSignal.TalkBaSau),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Tám Vá đối chiếu tờ phiếu với nhật ký của cái hộp. Tín hiệu từ trại phát ra trước.",
                    Id = "Q10", Title = "Mẫu Hạm có mùi cám", Trigger = "Boss Mẫu Hạm UFO, chu kỳ 10",
                    Objective = "Chống chùm kéo bằng Skyhook, EMP, Thợ Săn UFO hoặc phân tán đàn.",
                    RewardGold = 500, RewardExp = 200,
                    Unlocks = (e, b) => Done(b, "Q09"),
                    Steps =
                    {
                        State("Qua trận Boss chu kỳ 10 (thắng hay thua đều tính)", e => e.BossWavesFought >= 2),
                        Sig("Tám Vá đối chiếu tờ phiếu với cái hộp", "Q10", QuestSignal.TalkTamVa),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Mận gọi sáu con heo già là “hội phụ huynh”.",
                    Id = "Q11", Title = "Đàn này có người", Trigger = "Sau Q10",
                    Objective = "Lập Bầy Đàn Sơ Khai: đủ cả bảy điều kiện của GDD, duy trì liên tục 3 ngày.",
                    RewardGold = 500, RewardExp = 220, RewardNote = "Chưa bảo đảm có thủ lĩnh",
                    Unlocks = (e, b) => Done(b, "Q10"),
                    Steps = { State("Bầy Đàn Sơ Khai giữ 3 ngày", e => e.Herd.Tier >= HerdStateTier.SoKhai && (e.Herd.Tier > HerdStateTier.SoKhai || e.Herd.ConsecutiveDaysInState >= 3)) }
                },
                new QuestDefinition
                {
                    KhoaLine = "Người bán tin bảo: “‘Lúc nào’ thì tôi bán được. ‘Tại sao’ thì tôi chưa định giá nổi.”",
                    Id = "Q12", Title = "Mua tin về chuyện mình vừa thấy", Trigger = "Sau Q11",
                    Objective = "Mua tình báo đến cấp phù hợp, so sánh với nhật ký quan sát đàn.",
                    RewardGold = 400, RewardExp = 160, RewardNote = "Mở nhật ký câu hỏi về điểm nhận",
                    Unlocks = (e, b) => Done(b, "Q11"),
                    Steps =
                    {
                        Sig("Mua tin của Lệ", "Q12", QuestSignal.TalkLe),
                        Sig("Quan sát đàn ở Nơi Trú", "Q12", QuestSignal.HerdRecalled),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Đội hình đàn trùng tấm ảnh gia đình.",
                    Id = "Q13", Title = "Chỗ không con nào đứng", Trigger = "Sau Q12",
                    Objective = "Ghi lại đội hình đàn từ hai góc khi đàn bình ổn, kiểm lại số heo, nhờ Dung khám nhóm heo được quan sát.",
                    RewardGold = 400, RewardExp = 160, RewardNote = "Ảnh bữa cơm có ông Năm",
                    Unlocks = (e, b) => Done(b, "Q12"),
                    Steps =
                    {
                        Sig("Gom đàn và ghi đội hình", "Q13", QuestSignal.HerdGathered90),
                        Sig("Nhờ Dung khám", "Q13", QuestSignal.TalkDung),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Bàn họp đủ ghế, vậy mà ai đến cũng tự vác theo ghế riêng.",
                    Id = "Q14", Title = "Buổi họp ai cũng mang ghế", Trigger = "Sau Q13, ngoài đợt",
                    Objective = "Mang đúng ba vật chứng đến ba người: bà Sáu, Tám Vá, Lệ.",
                    RewardGold = 450, RewardExp = 180, RewardNote = "Sơ đồ trạm cũ",
                    Unlocks = (e, b) => Done(b, "Q13") && !e.Clock.IsDuringCombatWave,
                    Steps =
                    {
                        Sig("Phiếu thu hoạch Q03 cho bà Sáu", "Q14", QuestSignal.TalkBaSau),
                        Sig("Bản ghi hai phút Q10 cho Tám Vá", "Q14", QuestSignal.TalkTamVa),
                        Sig("Danh sách mua heo Q04 cho Lệ", "Q14", QuestSignal.TalkLe),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Giữa sương, tôi nghe tiếng mẹ: “Vào ăn đi con.” Một giọng trẻ con đáp: “Dạ.”",
                    Id = "Q15", Title = "Hết sương chưa hết chuyện", Trigger = "Boss Thực Thể Sương, chu kỳ 15",
                    Objective = "Chuẩn bị đèn và thông tin, xử lý ba pha của Boss đúng theo luật.",
                    RewardGold = 600, RewardExp = 240,
                    Unlocks = (e, b) => Done(b, "Q14"),
                    Steps = { State("Qua trận Boss chu kỳ 15 (thắng hay thua đều tính)", e => e.BossWavesFought >= 3) }
                },
                new QuestDefinition
                {
                    KhoaLine = "Tám Vá: “Tên càng kêu thì người ta càng chịu đọc hướng dẫn. Dầu thì vẫn tính theo lít.”",
                    Id = "Q16", Title = "Bạch Vân và người cầm kìm", Trigger = "Sau Q15",
                    Objective = "Kiểm tra lưới điện, dự trù nạp, chọn thiết bị phù hợp.",
                    RewardGold = 400, RewardExp = 160, RewardNote = "Sơ đồ đường xác nhận",
                    Unlocks = (e, b) => Done(b, "Q15"),
                    Steps =
                    {
                        Sig("Xem lưới điện với Tám Vá", "Q16", QuestSignal.TalkTamVa),
                        Sig("Xây hoặc nâng một thiết bị", "Q16", QuestSignal.StructureBuilt),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Ông Tồn lấy được bản sao sổ gửi thư và trích đoạn camera bưu điện ngày 21/09/2026.",
                    Id = "Q17", Title = "Chủ cũ không có mặt để ký", Trigger = "Sau Q16",
                    Objective = "Nghe băng ghi âm của ông Năm. Nhờ ông Tồn đối chiếu lá thư. Cùng Tám Vá đọc nhật ký gửi của hộp.",
                    RewardGold = 300, RewardExp = 120,
                    Unlocks = (e, b) => Done(b, "Q16"),
                    Steps =
                    {
                        Sig("Nghe băng ở nhà chính", "Q17", QuestSignal.HouseBoxInspected),
                        Sig("Ông Tồn đối chiếu lá thư", "Q17", QuestSignal.TalkOngTon),
                        Sig("Đọc nhật ký hộp với Tám Vá", "Q17", QuestSignal.TalkTamVa),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Anh Ca lập danh sách ba cột “đủ / thiếu / chưa biết”. Cột “chưa biết” dài nhất.",
                    Id = "Q18", Title = "Gom đàn chứ đừng gom sợ", Trigger = "Đã xong Q17",
                    Objective = "Gom đàn về một chỗ để ghi mẫu đàn, ngoài combat.",
                    RewardGold = 450, RewardExp = 180, RewardNote = "Mẫu đàn và bộ thao tác ngắt tuyến",
                    Unlocks = (e, b) => Done(b, "Q17"),
                    Steps = { Sig("Gom ≥ 90% đàn về Nơi Trú", "Q18", QuestSignal.HerdGathered90) }
                },
                new QuestDefinition
                {
                    KhoaLine = "Tôi đặt chiếc vòng cạnh cái bát. Mặt kim loại phản chiếu một bàn ăn có bảy chỗ.",
                    Id = "Q19", Title = "Bữa tối chưa có khách", Trigger = "Ngoài combat, trước Q20",
                    Objective = "Nói chuyện với bà Sáu trong bếp, xem cái bát mang từ nhà cũ, quan sát đàn qua cửa sổ.",
                    RewardNote = "Không thưởng vàng. Ghi lại quan sát.",
                    Unlocks = (e, b) => Done(b, "Q18") && !e.Clock.IsDuringCombatWave,
                    Steps =
                    {
                        Sig("Nói chuyện với bà Sáu", "Q19", QuestSignal.TalkBaSau),
                        Sig("Quan sát đàn từ nhà chính", "Q19", QuestSignal.HouseBoxInspected),
                    }
                },
                new QuestDefinition
                {
                    KhoaLine = "Sáng hôm sau, ở cổng trại có một kiện hàng dán nhãn “trả về người gửi”.",
                    Id = "Q20", Title = "Rìu Sắt trả hàng", Trigger = "Đã xong Q17–Q19; đợt lớn kế tiếp từ chu kỳ 20",
                    Objective = "Sau đợt lớn kế tiếp, tương tác với hộp ở nhà chính: so mẫu đàn với các kênh, rồi chọn kết thúc.",
                    RewardGold = 800, RewardExp = 300, RewardNote = "Chọn kết thúc",
                    Unlocks = (e, b) => Done(b, "Q19") && e.Clock.CycleNumber >= 20,
                    Steps =
                    {
                        State("Qua đợt lớn kế tiếp từ chu kỳ 20", e => e.LastMajorWaveCycle >= 20),
                        Sig("Mở hộp ở nhà chính", "Q20", QuestSignal.HouseBoxInspected),
                    }
                },
            };
            return list;
        }

        private static List<QuestDefinition> BuildSide() => new List<QuestDefinition>
        {
            new QuestDefinition
            {
                Id = "S02", Title = "Người chết ăn rất khỏe", IsSide = true, Trigger = "Con heo chân trắng còn trong trại",
                Objective = "Mang cân tới là nó nằm ngửa. Nhờ thú y loại trừ khả năng bị bệnh.",
                RewardGold = 150, RewardNote = "Một trang nhật ký",
                Unlocks = (e, b) => b.Completed.Contains("Q02"),
                Steps = { Sig("Nhờ Dung khám", "S02", QuestSignal.TalkDung) }
            },
            new QuestDefinition
            {
                Id = "S10", Title = "Bí nhân không nhận thứ Hai", IsSide = true, Trigger = "Đã gặp Bí nhân",
                Objective = "Xem ba lời đề nghị và trao đổi với Bí nhân (không nhận vàng).",
                RewardExp = 100,
                Steps = { Sig("Trao đổi với Bí nhân", "S10", QuestSignal.MysticTrade) }
            },
            new QuestDefinition
            {
                Id = "S11", Title = "Anh Ca đổi tên", IsSide = true, Trigger = "Đã tuyển Quản Lý Ca",
                Objective = "Đưa anh Ca lên xã làm thủ tục đổi tên.",
                RewardExp = 120, RewardNote = "Bảng tên trống",
                Unlocks = (e, b) => e.PetsAndNpcs.HiredNpcs.Any(n => n.Id == NpcSpecialistId.QuanLyCa),
                Steps = { Sig("Gặp anh Ca", "S11", QuestSignal.TalkAnhCa) }
            },
        };

        // ------------------------------------------------------------------ save

        public List<string> SaveCompleted() => Completed.ToList();

        public List<string> SaveReceived() =>
            Received.SelectMany(kv => kv.Value.Select(s => kv.Key + ":" + s)).ToList();

        public void Load(List<string>? completed, List<string>? received, int ending)
        {
            Completed.Clear();
            Received.Clear();
            if (completed != null) foreach (var id in completed) Completed.Add(id);
            if (received != null)
                foreach (var row in received)
                {
                    var p = row.Split(':');
                    if (p.Length == 2 && Enum.TryParse<QuestSignal>(p[1], out var s)) Add(p[0], s);
                }
            Ending = (StoryEndingId)ending;
        }
    }
}
