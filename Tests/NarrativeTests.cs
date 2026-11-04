using System;
using System.Collections.Generic;
using System.Linq;
using PigTycoon.Core;

/// <summary>ADR-0027: runner kể chuyện — chữ chạy, chạm 2 nhịp, lựa chọn có điều kiện, bỏ qua giữ trọn hiệu ứng, nhật ký, tua dòng đã đọc.</summary>
public static class NarrativeTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    sealed class FakeHost : INarrativeHost
    {
        public PlayerId Protagonist { get; set; } = PlayerId.An;
        public int Day { get; set; } = 1;
        public readonly HashSet<string> Flags = new HashSet<string>();
        public readonly HashSet<string> Seen = new HashSet<string>();
        public readonly List<string> Applied = new List<string>();
        public bool Check(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            bool neg = condition.StartsWith("!");
            string f = condition.TrimStart('!').Replace("STORY_FLAG ", "");
            return Flags.Contains(f) != neg;
        }
        public void Apply(NarrativeEffect e)
        {
            Applied.Add(e.ToString());
            if (e.Kind == NarrativeEffectKind.SetFlag) Flags.Add(e.Id);
            if (e.Kind == NarrativeEffectKind.ClearFlag) Flags.Remove(e.Id);
        }
        public void MarkSceneSeen(string id) => Seen.Add(id);
        public bool HasSeenScene(string id) => Seen.Contains(id);
    }

    static SceneDefinition Sample()
    {
        var s = new SceneDefinition { Id = "T01", Recap = "Tóm tắt T01" };
        s.Say(CastIds.An, Portraits.Happy, "Chào {an:chị|khoa:anh} {player}.")
         .Do(NarrativeEffectKind.Gold, "", 50)
         .Pause(400)
         .Choice("Đi đâu?", 1,
             SceneDefinition.Opt("Ra chuồng", "chuong", SceneDefinition.Fx(NarrativeEffectKind.SetFlag, "chon_chuong")),
             SceneDefinition.Opt("Ra chợ", "cho", SceneDefinition.Fx(NarrativeEffectKind.SetFlag, "chon_cho")),
             new NarrativeOption { Text = "Bí mật", Condition = "STORY_FLAG biet_bi_mat", ShowWhenLocked = true, LockedReason = "Cần biết bí mật" })
         .Label("chuong").Say(CastIds.Khoa, Portraits.Happy, "Chuồng.").Goto("het")
         .Label("cho").Narrate("Chợ đông.").Do(NarrativeEffectKind.Item, "CamThuong", 2)
         .Label("het").Flag("xong_T01").End();
        return s;
    }

    public static void RunRunner()
    {
        var host = new FakeHost();
        var r = new NarrativeRunner(host, new NarrativeState());
        Check(r.Start(Sample()), "start");
        Check(r.Phase == RunnerPhase.Line && r.Line.FullText == "Chào chị An.", "biến thể An + tên: " + r.Line?.FullText);
        Check(r.Line.VisibleChars == 0 && r.LocksInput, "chữ chạy từ 0, khoá điều khiển");
        r.Tick(0.1f);
        Check(r.Line.VisibleChars > 0 && !r.Line.Complete, "chữ chạy dần");
        r.Tap();
        Check(r.Line.Complete, "chạm 1: hiện hết dòng");
        r.Tap();
        Check(r.Phase == RunnerPhase.Pause && host.Applied.Contains("Gold::50"), "chạm 2: sang lệnh kế, hiệu ứng chạy");
        r.Tick(0.5f);
        Check(r.Phase == RunnerPhase.Choice && r.Choices.Count == 3, "3 lựa chọn (1 khoá vẫn hiện)");
        var locked = r.Choices.Single(c => c.Index == 2);
        Check(!locked.Enabled && locked.LockedReason == "Cần biết bí mật", "lựa chọn khoá hiện lý do");
        Check(!r.Choose(2), "không chọn được lựa chọn khoá");
        Check(r.Choose(0) && host.Flags.Contains("chon_chuong"), "chọn chuồng");
        Check(r.Line.SpeakerName == "Khoa" && r.Line.Portrait == Portraits.Neutral, "Khoa không có chân dung vui → neutral");
        r.Tap(); r.Tap();
        Check(!r.InScene && host.Seen.Contains("T01") && host.Flags.Contains("xong_T01") && !host.Applied.Any(a => a.StartsWith("Item")), "kết cảnh đúng nhánh");
        Check(r.Backlog.Count == 3 && r.Backlog[1].IsChoice && r.Backlog[1].Text == "Ra chuồng", "nhật ký có thoại + lựa chọn");

        // Khoa variant
        var hostK = new FakeHost { Protagonist = PlayerId.Khoa };
        var rk = new NarrativeRunner(hostK, new NarrativeState());
        rk.Start(Sample());
        Check(rk.Line.FullText == "Chào anh Khoa.", "biến thể Khoa");
    }

    public static void RunSkipKeepsState()
    {
        // Skip at the first line: all effects on the default path (option 1 = chợ) still apply; nothing presentational.
        var host = new FakeHost();
        var r = new NarrativeRunner(host, new NarrativeState());
        bool skippedFlag = false;
        r.SceneEnded += (s, skipped) => skippedFlag = skipped;
        r.Start(Sample());
        Check(r.Skip(), "bỏ qua được");
        Check(!r.InScene && skippedFlag, "kết thúc dạng bỏ qua");
        Check(host.Applied.SequenceEqual(new[] { "Gold::50", "SetFlag:chon_cho:0", "Item:CamThuong:2", "SetFlag:xong_T01:0" }),
            "bỏ qua áp đủ hiệu ứng nhánh mặc định: " + string.Join(",", host.Applied));
        Check(host.Seen.Contains("T01") && r.Backlog.Last().IsRecap && r.Backlog.Last().Text == "Tóm tắt T01", "có thẻ tóm tắt");

        // Skip while a choice is open uses the default; a default whose condition fails falls back to the first allowed.
        var host2 = new FakeHost();
        var s2 = new SceneDefinition { Id = "T02" };
        s2.Choice("?", 1,
            SceneDefinition.Opt("A", "", SceneDefinition.Fx(NarrativeEffectKind.SetFlag, "a")),
            new NarrativeOption { Text = "B", Condition = "STORY_FLAG khong_co", Effects = { SceneDefinition.Fx(NarrativeEffectKind.SetFlag, "b") } })
          .End();
        var r2 = new NarrativeRunner(host2, new NarrativeState());
        r2.Start(s2);
        Check(r2.Phase == RunnerPhase.Choice && r2.Choices.Count == 1, "B ẩn vì điều kiện sai");
        r2.Skip();
        Check(host2.Flags.Contains("a") && !host2.Flags.Contains("b"), "mặc định không hợp lệ → lấy lựa chọn hợp lệ đầu");

        // Unskippable scene
        var s3 = new SceneDefinition { Id = "T03", Skippable = false };
        s3.Narrate("Không bỏ qua.").End();
        var r3 = new NarrativeRunner(new FakeHost(), new NarrativeState());
        r3.Start(s3);
        Check(!r3.Skip() && r3.InScene, "cảnh khoá bỏ qua");
    }

    public static void RunReadSkipAutoAndStage()
    {
        var state = new NarrativeState();
        var host = new FakeHost();
        var r = new NarrativeRunner(host, state);
        var s = new SceneDefinition { Id = "T04", Once = false };
        s.Narrate("Một.").Narrate("Hai.").End();

        // first read: hold fast-forward does not skip unread lines
        r.Start(s);
        r.FastForward = true;
        r.Tick(0.016f);
        Check(r.InScene && r.Line.FullText == "Một." && !r.Line.Complete, "tua không bỏ dòng chưa đọc");
        r.FastForward = false;
        r.Tap(); r.Tap(); r.Tap(); r.Tap();
        Check(!r.InScene, "đọc hết lần 1");
        // second read: both lines are read → fast-forward runs through
        r.Start(s);
        Check(r.Line.ReadBefore, "dòng đã đọc được đánh dấu");
        r.FastForward = true;
        for (int i = 0; i < 6 && r.InScene; i++) r.Tick(0.016f);
        Check(!r.InScene, "tua hết các dòng đã đọc");
        r.FastForward = false;

        // auto mode advances after the delay
        state.Settings.TextCps = 0f;
        r.Start(s);
        r.Auto = true;
        Check(r.Line.Complete, "cps 0 = hiện tức thì");
        r.Tick(0.5f);
        Check(r.Line.FullText == "Một.", "chưa đủ thời gian tự chuyển");
        r.Tick(1.0f);
        Check(r.Line.FullText == "Hai.", "tự chuyển sau 1,2 s + 0,035 s/ký tự");

        // stage commands: headless runs straight through; a view-driven runner waits for blocking ones
        var s5 = new SceneDefinition { Id = "T05" };
        s5.Place("ba_sau", 3, 4, CardinalDirection.South).Move("ba_sau", 5, 4).Emote("ba_sau", "!").Narrate("Xong.").End();
        var rv = new NarrativeRunner(new FakeHost(), new NarrativeState()) { Headless = false };
        rv.Start(s5);
        Check(rv.Phase == RunnerPhase.Stage && rv.PendingStage.Count == 1, "đặt nhân vật là lệnh chặn");
        var ev = rv.TakeStageEvents().Single();
        rv.StageDone(ev.Seq + 99);
        Check(rv.Phase == RunnerPhase.Stage, "seq sai không mở chặn");
        rv.StageDone(ev.Seq);
        Check(rv.Phase == RunnerPhase.Stage && rv.PendingStage.Single().Command.Kind == NarrativeCmd.ActorMove, "đi bộ là lệnh chặn");
        rv.StageDone(rv.TakeStageEvents().Single().Seq);
        Check(rv.Phase == RunnerPhase.Line && rv.PendingStage.Single().Command.Kind == NarrativeCmd.Emote, "cảm xúc không chặn, thoại hiện");

        // director: trigger, priority, once, queue
        var host6 = new FakeHost();
        var d = new NarrativeDirector(host6, new NarrativeState());
        var a = new SceneDefinition { Id = "A", Trigger = NarrativeTrigger.EnterLocation, TriggerArg = "Town", Priority = 1 };
        a.Narrate("A").End();
        var b = new SceneDefinition { Id = "B", Trigger = NarrativeTrigger.EnterLocation, TriggerArg = "Town", Priority = 5, Condition = "STORY_FLAG mo_b" };
        b.Narrate("B").End();
        d.Register(a); d.Register(b);
        Check(d.Fire(NarrativeTrigger.EnterLocation, "Town")?.Id == "A", "B chưa đủ điều kiện → A");
        d.Runner.Skip();
        Check(d.Find(NarrativeTrigger.EnterLocation, "Town") == null, "A chỉ một lần");
        host6.Flags.Add("mo_b");
        Check(d.Find(NarrativeTrigger.EnterLocation, "Farm") == null && d.Find(NarrativeTrigger.EnterLocation, "Town")?.Id == "B", "đúng nơi, đủ điều kiện");
    }
}

/// <summary>ADR-0027: every authored scene is well-formed (labels, chains, cast, portraits, line length, talk never gives affinity).</summary>
public static class NarrativeContentTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    public const int MaxLineChars = 120;

    public static List<string> Problems()
    {
        var problems = new List<string>();
        var scenes = NarrativeLibrary.All().ToList();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in scenes)
            if (!ids.Add(s.Id)) problems.Add($"{s.Id}: trùng id");
        foreach (var s in scenes)
        {
            var labels = new HashSet<string>(s.Commands.Where(c => c.Kind == NarrativeCmd.Label).Select(c => c.Label));
            bool talk = s.Trigger == NarrativeTrigger.Talk;
            if (s.Skippable && !talk && string.IsNullOrWhiteSpace(s.Recap) && s.Commands.Count(c => c.Kind == NarrativeCmd.Say || c.Kind == NarrativeCmd.Narrate) > 3)
                problems.Add($"{s.Id}: cảnh dài bỏ qua được nhưng thiếu Recap");
            foreach (var c in s.Commands)
            {
                switch (c.Kind)
                {
                    case NarrativeCmd.Say:
                        var cast = NarrativeCast.Get(c.Speaker);
                        if (cast == null) problems.Add($"{s.Id}: người nói lạ '{c.Speaker}'");
                        else if (!cast.Portraits.Contains(c.Portrait)) problems.Add($"{s.Id}: {c.Speaker} không có chân dung '{c.Portrait}'");
                        goto case NarrativeCmd.Narrate;
                    case NarrativeCmd.Narrate:
                        foreach (var who in new[] { PlayerId.An, PlayerId.Khoa })
                        {
                            var text = NarrativeRunner.ResolveFor(c.Text, who);
                            if (text.Length > MaxLineChars) problems.Add($"{s.Id}: dòng dài {text.Length} ký tự: {text.Substring(0, 40)}…");
                            if (text.Contains("{")) problems.Add($"{s.Id}: token chưa thay: {text.Substring(0, Math.Min(60, text.Length))}");
                        }
                        break;
                    case NarrativeCmd.Goto:
                    case NarrativeCmd.IfGoto:
                        if (!labels.Contains(c.Label)) problems.Add($"{s.Id}: thiếu nhãn '{c.Label}'");
                        break;
                    case NarrativeCmd.Choice:
                        if (c.Options == null || c.Options.Count == 0) { problems.Add($"{s.Id}: lựa chọn rỗng"); break; }
                        foreach (var o in c.Options)
                        {
                            if (!string.IsNullOrEmpty(o.GotoLabel) && !labels.Contains(o.GotoLabel)) problems.Add($"{s.Id}: lựa chọn tới nhãn thiếu '{o.GotoLabel}'");
                            if (talk && o.Effects.Any(e => e.Kind == NarrativeEffectKind.Affinity || e.Kind == NarrativeEffectKind.Gold || e.Kind == NarrativeEffectKind.Item))
                                problems.Add($"{s.Id}: trò chuyện không được cho hảo cảm/vàng/đồ");
                        }
                        break;
                    case NarrativeCmd.Effect:
                        if (c.Effect.Kind == NarrativeEffectKind.PlayScene && !ids.Contains(c.Effect.Id)) problems.Add($"{s.Id}: Then tới cảnh thiếu '{c.Effect.Id}'");
                        if (talk && (c.Effect.Kind == NarrativeEffectKind.Affinity || c.Effect.Kind == NarrativeEffectKind.Gold || c.Effect.Kind == NarrativeEffectKind.Item))
                            problems.Add($"{s.Id}: trò chuyện không được cho hảo cảm/vàng/đồ");
                        break;
                }
            }
        }
        return problems;
    }

    public static void RunContent()
    {
        var p = Problems();
        Check(p.Count == 0, $"{p.Count} lỗi nội dung:\n  " + string.Join("\n  ", p.Take(40)));
        var scenes = NarrativeLibrary.All().ToList();
        foreach (var who in new[] { "Khoa", "An" })
            Check(scenes.Any(s => s.Trigger == NarrativeTrigger.NewGame && s.Condition.Contains("PROTAGONIST " + who)), $"ván mới của {who} có cảnh mở đầu");
        // every backdrop has a first-pass base plate, so no scene is ever shown without a picture
        var unmapped = NarrativeBackdrops.UsedIds().Where(id => !NarrativeBackdrops.BaseSet.Contains(NarrativeBackdrops.Base(id))).ToList();
        Check(unmapped.Count == 0, "backdrop chưa có tranh nền gốc: " + string.Join(", ", unmapped));
    }

    /// <summary>Headless play-through: the whole prologue of each route runs by tapping, ends on the farm flag, then a talk works.</summary>
    public static void RunPrologueHeadless()
    {
        foreach (var who in new[] { PlayerId.Khoa, PlayerId.An })
        {
            var e = new GameEngine(seed: 7);
            NewGameSetup.ChooseProtagonist(e, who);
            var gotoFarm = false;
            e.NarrativeCustom += fx => { if (fx.Id == "goto_map:Farm") gotoFarm = true; };
            var first = e.FireNarrative(NarrativeTrigger.NewGame);
            Check(first != null, $"{who}: có cảnh NewGame");
            var r = e.Narrative.Runner;
            int taps = 0;
            while (r.InScene && taps < 20000)
            {
                if (r.Phase == RunnerPhase.Choice) r.Choose(r.Choices.First(c => c.Enabled).Index);
                else if (r.Phase == RunnerPhase.Pause || r.Phase == RunnerPhase.Banner) r.Tick(5f);
                else r.Tap();
                taps++;
            }
            Check(!r.InScene, $"{who}: phần mở đầu kết thúc (không kẹt vòng lặp) sau {taps} lần chạm");
            Check(e.NarrativeState.Flags.Contains("prologue_done") && gotoFarm, $"{who}: mở đầu đặt prologue_done và đưa về trại");
            var talked = e.TalkTo(CastIds.BaSau);
            if (talked != null) r.Skip();
            Check(!r.InScene, $"{who}: nói chuyện với bà Sáu chạy được");
        }
    }

    /// <summary>StoryPacing (4 Nov): a new game reaches the farm after one short scene; memories come back one per morning.</summary>
    public static void RunPacing()
    {
        foreach (var who in new[] { PlayerId.Khoa, PlayerId.An })
        {
            var e = new GameEngine(seed: 7);
            NewGameSetup.ChooseProtagonist(e, who);
            var first = e.FireNarrative(NarrativeTrigger.NewGame);
            Check(first != null && first.Id == StoryPacing.OpeningScene[who.ToString()], $"{who}: ván mới mở bằng cảnh nhận trại ({first?.Id})");
            var r = e.Narrative.Runner;
            int lines = 0, taps = 0;
            while (r.InScene && taps < 5000)
            {
                if (r.Phase == RunnerPhase.Choice) r.Choose(r.Choices.First(c => c.Enabled).Index);
                else if (r.Phase == RunnerPhase.Pause || r.Phase == RunnerPhase.Banner) r.Tick(5f);
                else { if (r.Phase == RunnerPhase.Line) lines++; r.Tap(); }
                taps++;
            }
            Check(!r.InScene && e.NarrativeState.Flags.Contains("prologue_done"), $"{who}: vào trại ngay sau cảnh mở đầu");
            Check(lines <= 140, $"{who}: cảnh mở đầu ngắn ({lines} lần chạm chữ)");
            Check(e.NarrativeState.Waiting.Count == 1 && e.NarrativeState.Waiting[0].StartsWith("P01"), $"{who}: P01 chờ tới sáng ({string.Join(",", e.NarrativeState.Waiting)})");
            var morning = e.FireNarrative(NarrativeTrigger.DayStart);
            Check(morning != null && morning.Id.StartsWith("P01"), $"{who}: sáng hôm sau là một ký ức ({morning?.Id})");
            r.Skip();
            Check(!r.InScene, $"{who}: ký ức chạy một cảnh rồi dừng, không kéo cả chuỗi");
            Check(e.NarrativeState.Waiting.Any(w => w.StartsWith("P02")), $"{who}: ký ức kế tiếp chờ sáng sau");
        }
    }
}
