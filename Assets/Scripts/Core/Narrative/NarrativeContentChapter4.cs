using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Chương 4 — "Bữa tối chưa có khách" (Q16–Q20) and the Season 1 endings (story bible Phần 8: 8.1–8.3 chosen at Q20,
    /// 8.4 epilogue, 8.5 post-credit), both routes. Canon: 2.3–2.7, Phần 5 / 5B rows Q16–Q20, 10.4, 12.15 (An variants).
    /// Village rules kept: nobody names the thing in the sky, no villager says the number (they say "gói cuối", "cho đủ",
    /// "sáu với một"), a silent beat follows every mystery and life comes back through a pig, Q19 is a loss scene (no report
    /// voice, no joke, no notebook line), and no scene removes buildings, herd or money (2.6, 8.4).
    ///
    /// Ordering: StoryQuestBook fires QuestCompleted inside Signal(), before the Signal narrative trigger, so a multi-step
    /// quest's last step scene would otherwise play after its "done" scene. Here step scenes (Signal triggers) are gated on
    /// "previous done scene seen, own done scene not seen", and each step scene chains the Manual done scene itself once the
    /// quest is complete (<see cref="ThenWhenDone"/>). Single-step Q18 uses QuestCompleted as Chapter 1 does.
    ///
    /// Flow: C4_card (DayStart after chapter3_done) → Q16_grid / Q16_built → Q16_done ⇒ Q17_start → Q17_tape / Q17_letter /
    /// Q17_log → Q17_done ⇒ Q18_start → (QuestCompleted Q18) Q18_done ⇒ Q19_start → Q19_window / Q19_kitchen → Q19_done;
    /// DayStart from cycle 20: Q20_start (→ Q20_ready the morning after the major wave, cycle ≥ 21) → box (HouseBoxInspected)
    /// Q20_box ⇒ Q20_choice. The choice sets s1_ending_8_x + s1_ending_chosen, queues the ending scene, and raises Custom
    /// "ending:&lt;StoryEndingId&gt;" for the host to call StoryQuestBook.ChooseEnding (which completes Q20 and fires
    /// QuestCompleted Q20; the ending scenes also listen there, gated on the flag). Next DayStart after Q20 is done: S1_after
    /// (8.4) ⇒ S1_postcredit (8.5). "Để rồi tính" leaves Q20 open; the box offers the choice again.
    /// </summary>
    public static class NarrativeContentChapter4
    {
        const string K = CastIds.Khoa, A = CastIds.An, Man = CastIds.Man, Tam = CastIds.TamVa, Sau = CastIds.BaSau,
            Ton = CastIds.OngTon, Cuc = CastIds.Cuc, Ca = CastIds.AnhCa;
        const string N = Portraits.Neutral, H = Portraits.Happy, L = Portraits.Laugh, U = Portraits.Surprised,
            T = Portraits.Thinking, Sd = Portraits.Sad, W = Portraits.Worried;
        const string IsKhoa = "PROTAGONIST Khoa", IsAn = "PROTAGONIST An";
        const StageSide R = StageSide.Right;

        /// <summary>Flag set when an ending (or the 8.4 epilogue) has played.</summary>
        public const string ChapterDoneFlag = "chapter4_done";
        /// <summary>Chapter 3's end flag; the chapter card waits on it.</summary>
        public const string PreviousChapterFlag = "chapter3_done";

        /// <summary>Set by the Q20 choice (any ending). Not set when the ending is picked from the journal buttons.</summary>
        public const string EndingChosenFlag = "s1_ending_chosen";
        public const string Ending81Flag = "s1_ending_8_1", Ending82Flag = "s1_ending_8_2", Ending83Flag = "s1_ending_8_3";
        /// <summary>
        /// Custom effect "ending:Ending_8_1_GiuTraiThaoDuongGui" (StoryEndingId name). The host must call
        /// <c>engine.Quests.ChooseEnding(engine, id)</c>; that completes Q20 (reward, save) and fires QuestCompleted Q20.
        /// </summary>
        public const string EndingCustomPrefix = "ending:";

        const string Q20ReadyFlag = "c4_q20_ready", Q20CutFlag = "c4_q20_cut";

        static SceneDefinition Scene(string id, string title, NarrativeTrigger trigger, string arg, string condition, string recap,
            int priority = 0, string location = "Farm")
            => new SceneDefinition
            {
                Id = id, Title = title, Trigger = trigger, TriggerArg = arg, Condition = condition, Recap = recap,
                Priority = priority, Location = location, Once = true, Skippable = true,
            };

        static SceneDefinition Hint(this SceneDefinition s, string verb) => s.Do(NarrativeEffectKind.Custom, NarrativeCustomIds.TutorialPrefix + verb);
        static SceneDefinition Notebook(this SceneDefinition s, string entry) => s.Do(NarrativeEffectKind.Custom, NarrativeCustomIds.NotebookPrefix + entry);
        static SceneDefinition Reflection(this SceneDefinition s, string portrait) => s.Do(NarrativeEffectKind.Custom, NarrativeCustomIds.ReflectionPrefix + portrait);
        static SceneDefinition NoSkip(this SceneDefinition s) { s.Skippable = false; return s; }
        static SceneDefinition Repeatable(this SceneDefinition s) { s.Once = false; return s; }
        static string Sig(QuestSignal s) => s.ToString();

        /// <summary>Last step scene of a quest plays after the quest is already complete: chain its done scene then.</summary>
        static SceneDefinition ThenWhenDone(this SceneDefinition s, string questId, string doneSceneId)
            => s.IfGoto("!STORY_QUEST_DONE " + questId, "chain_end").Then(doneSceneId).Label("chain_end");

        /// <summary>Step scenes of a quest: after the previous done scene, until this quest's done scene has played.</summary>
        static string Step(string afterScene, string doneScene, string who) => "SCENE_SEEN " + afterScene + ",!SCENE_SEEN " + doneScene + "," + who;

        static NarrativeOption EndingOpt(string text, string flag, StoryEndingId id, string endingScene)
            => SceneDefinition.Opt(text, "chosen",
                SceneDefinition.Fx(NarrativeEffectKind.SetFlag, flag),
                SceneDefinition.Fx(NarrativeEffectKind.SetFlag, EndingChosenFlag),
                // queue the ending first: when the host then completes Q20, Fire(QuestCompleted) sees it queued and skips it
                SceneDefinition.Fx(NarrativeEffectKind.PlayScene, endingScene),
                SceneDefinition.Fx(NarrativeEffectKind.Custom, EndingCustomPrefix + id));

        public static IEnumerable<SceneDefinition> Scenes()
        {
            foreach (var s in Khoa()) yield return s;
            foreach (var s in An()) yield return s;
        }

        // =====================================================================================================
        // Khoa — giọng biên bản, hỏi thẳng, ghi sổ trước mặt người ta; mặt không đổi, cái bóng cảm thay.
        // =====================================================================================================
        static IEnumerable<SceneDefinition> Khoa()
        {
            // ---- Chapter card + Q16 Bạch Vân và người cầm kìm
            yield return Scene("C4_card_khoa", "Chương 4 — Bữa tối chưa có khách", NarrativeTrigger.DayStart, "",
                    "STORY_FLAG " + PreviousChapterFlag + "," + IsKhoa,
                    "Tám Vá muốn coi lại lưới điện trước đợt lớn. Khoa ghé nhà ông, rồi xây hoặc nâng một thiết bị.", priority: 20)
                .Music("")
                .Banner("Chương 4 — Bữa tối chưa có khách", 2400)
                .Narrate("“Bà Sáu dọn cơm cho sáu với một người. Tôi ngồi chỗ của một.”")
                .Narrate("Đầu trang, tôi ghi câu hỏi: “Ai đã gửi lá thư? Cái hộp đang chờ gửi gì?”")
                .IfGoto("STORY_QUEST_DONE Q16", "q16_done")
                .IfGoto("!STORY_QUEST_ACTIVE Q16", "end")
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Say(Tam, N, "Sắp tới đợt lớn. Lưới điện trại cậu phải coi lại.", R)
                .Say(Tam, T, "Chiều ghé nhà tôi ngoài xóm. Tôi trải sơ đồ ra cho coi chỗ nào đang gánh quá sức.", R)
                .Say(K, N, "Ghi rồi.")
                .Hint("interact")
                .Goto("end")
                .Label("q16_done")
                .Then("Q16_done_khoa")
                .Label("end");

            const string Q16Step = "STORY_FLAG " + PreviousChapterFlag + ",SCENE_SEEN C4_card_khoa,!SCENE_SEEN Q16_done_khoa," + IsKhoa;

            yield return Scene("Q16_grid_khoa", "Sơ đồ lưới", NarrativeTrigger.Signal, Sig(QuestSignal.TalkTamVa), Q16Step,
                    "Tám Vá giải thích vì sao máy nào cũng mang tên kiếm hiệp. Cái hộp trong nhà chính thì ông không đặt tên. " +
                    "Ông bắt đầu in nhật ký gửi của nó; Khoa cần xây hoặc nâng một thiết bị.", location: "")
                .Narrate("Tám Vá trải sơ đồ lưới điện ra bàn. Máy nào cũng có tên: Cửu Tiêu Lôi Pháo, Thiên Nhãn Vọng Đài.")
                .Say(K, N, "Sao máy nào cũng tên kiếm hiệp.")
                .Say(Tam, H, "Tên càng kêu thì người ta càng chịu đọc hướng dẫn.", R)
                .Say(Tam, N, "Dầu thì vẫn tính theo lít.", R)
                .Narrate("Tôi ghi. Hợp lý hơn tôi tưởng.")
                .Say(K, N, "Cái hộp trong nhà chính tên gì.")
                .Narrate("Ông nghĩ rất lâu. Lâu hơn cả lúc tính dầu.")
                .Say(Tam, T, "Cái đó không phải máy của tôi. Nên tôi không đặt tên.", R)
                .Say(Tam, N, "Từ tối nay tôi in nhật ký gửi của nó ra giấy. Mỗi tối một xấp.", R)
                .Say(Tam, N, "Còn lưới điện: chỗ nào thiếu thì xây thêm, hoặc nâng một máy lên. Xây xong tôi gõ thử.", R)
                .Hint("build")
                .ThenWhenDone("Q16", "Q16_done_khoa");

            yield return Scene("Q16_built_khoa", "Gõ thử", NarrativeTrigger.Signal, Sig(QuestSignal.StructureBuilt), Q16Step,
                    "Tám Vá gõ ba cái lên thiết bị mới. “Kêu đúng.”")
                .Narrate("Chiều, Tám Vá ghé ngang. Ông gõ ba cái lên cái máy mới, nghe, rồi gật đầu.")
                .Sfx("sfx_go_ba_cai")
                .Say(Tam, H, "Kêu đúng.", R)
                .IfGoto("STORY_QUEST_DONE Q16", "chain")
                .Say(Tam, N, "Ghé nhà tôi coi sơ đồ lưới nữa là xong.", R)
                .Hint("interact")
                .Goto("end")
                .Label("chain")
                .Then("Q16_done_khoa")
                .Label("end");

            // Mystery: the raincoat. Silent beat, then a pig.
            yield return Scene("Q16_done_khoa", "Áo mưa trong kho", NarrativeTrigger.Manual, "", IsKhoa,
                    "Đi lấy giấy in trong kho, Khoa thấy chiếc áo mưa xám đen, vai dính đất đỏ. Tám Vá: “Của ông Năm.” " +
                    "Nhưng chưa thấy ông Năm mặc bao giờ.", location: "")
                .IfGoto("SCENE_SEEN Q16_grid_khoa", "coat")
                .Say(K, N, "Cái hộp trong nhà chính tên gì.")
                .Say(Tam, T, "Cái đó không phải máy của tôi. Nên tôi không đặt tên.", R)
                .Say(Tam, N, "Từ tối nay tôi in nhật ký gửi của nó ra giấy. Mỗi tối một xấp.", R)
                .Label("coat")
                .Narrate("Tám Vá cần giấy in. Tôi vào kho trại lấy.")
                .Music("")
                .Backdrop("bg_kho_trai_ao_mua")
                .Narrate("Trên cây đinh sau cửa kho treo một chiếc áo mưa xám đen. Có mũ trùm.")
                .Narrate("Vai áo dính một vệt đất đỏ.")
                .Say(K, N, "Áo này của ai.")
                .Say(Tam, N, "Của ông Năm.", R)
                .Say(K, N, "Ông Năm hay mặc không.")
                .Narrate("Tám Vá nghĩ một lúc.")
                .Say(Tam, T, "Chưa thấy bao giờ.", R)
                .Narrate("Cột phải: “Áo mưa xám đen. Kho trại.” Lề: “Ông Năm?”")
                .Pause(1800)
                .Backdrop("")
                .Narrate("Ngoài sân, một con heo ngoạm mất tờ giấy in đầu tiên. Tám Vá ghi biên bản: “Giấy. Bị ăn.”")
                .Notebook("Q16")
                .Then("Q17_start_khoa");

            // ---- Q17 Chủ cũ không có mặt để ký
            yield return Scene("Q17_start_khoa", "Chủ cũ không có mặt để ký", NarrativeTrigger.Manual, "", IsKhoa,
                    "Ông Năm để lại một cuộn băng cho người nhận trại. Ông Tồn có bản sao sổ gửi thư; Tám Vá in xong nhật ký hộp.",
                    location: "")
                .Narrate("Sáng hôm sau, Mận tới, tay cầm cây dù đen.")
                .Say(Man, N, "Ông Năm để một cuộn băng trong hộc tủ nhà chính. Ổng dặn: đưa người nhận trại.", R)
                .Say(K, N, "Cô nghe chưa.")
                .Say(Man, N, "Ổng dặn đưa anh. Em không nghe.", R)
                .Narrate("Cùng sáng đó, ông Tồn nhắn đã lấy được bản sao sổ gửi thư ở bưu điện.")
                .Narrate("Tám Vá in xong xấp nhật ký đầu tiên của cái hộp.")
                .Narrate("Ba việc. Nghe băng ở nhà chính. Mang lá thư sang ông Tồn. Đọc nhật ký hộp với Tám Vá.")
                .Hint("interact");

            string q17Step = Step("Q16_done_khoa", "Q17_done_khoa", IsKhoa);

            yield return Scene("Q17_tape_khoa", "Cuộn băng", NarrativeTrigger.Signal, Sig(QuestSignal.HouseBoxInspected), q17Step,
                    "Băng của ông Năm: lịch chăm trại, rồi lời xin lỗi người nhận trại. Cuối băng có tiếng bát kéo trên bàn. " +
                    "“Tôi chưa mời ai.”", priority: 10)
                .Music("")
                .Narrate("Cuộn băng nằm trong hộc tủ. Nhãn viết tay: “Người nhận trại.”")
                .Narrate("Tôi bấm nghe. Tiếng rè. Rồi giọng ông Năm, chậm, như đọc sổ.")
                .Narrate("“Cám trước. Nước sau. Rồi coi rào.”")
                .Narrate("Ông đọc lịch chăm trại gần hết một mặt băng. Giờ cho ăn. Con nào hay lạc. Đoạn rào nào hay hở.")
                .Narrate("Cuối băng, giọng ông nhỏ lại.")
                .Narrate("“Tôi xin lỗi người nhận trại. Tôi để cháu mang cái vòng lên mà không nói trước chuyện có thể xảy ra.”")
                .Pause(1400)
                .Narrate("Có tiếng một cái bát bị kéo trên mặt bàn.")
                .Narrate("“Tôi chưa mời ai.”")
                .Pause(2200)
                .Narrate("Băng hết.")
                .Narrate("Ngoài cửa sổ, con heo chân trắng húc cái xô rỗng, đều đều, như gõ cửa xin cơm.")
                .ThenWhenDone("Q17", "Q17_done_khoa");

            yield return Scene("Q17_letter_khoa", "Sổ gửi thư", NarrativeTrigger.Signal, Sig(QuestSignal.TalkOngTon), q17Step,
                    "Camera bưu điện 21/09/2026: người gửi lá thư lúc 07:02 là Khoa, ký tay trái. Ông Tồn đọc dấu nêm: 24. " +
                    "Cúc: vạch cuối kẻ bằng cây bút trong túi áo Khoa; mọi vạch đều nét tay trái.", location: "")
                .Say(Ton, N, "Bản sao sổ gửi thư. Với một đoạn camera bưu điện, ngày hai mốt tháng chín.", R)
                .Narrate("Ông đặt hai thứ lên bàn, ngay ngắn, như đặt biên nhận.")
                .Music("")
                .Narrate("Góc hình ghi 07:02. Người đứng ở quầy gửi lá thư cho quán bà Sáu là tôi.")
                .Narrate("Tôi ký sổ bằng tay trái.")
                .Narrate("Tôi thuận tay phải.")
                .Pause(1400)
                .Narrate("Cột phải: “Gửi thư: tôi.” Lời dặn trên mảnh da cùng nét, cùng ngày, nhưng không ai thấy lúc viết.")
                .Narrate("Cái đó tôi ghi ngoài lề.")
                .Narrate("Ông Tồn cầm mảnh da lên, đọc hàng dấu nêm như đọc số trong sổ kho.")
                .Say(Ton, T, "Người xưa viết số như vầy. Hai dấu mười, bốn dấu một. Hai mươi tư.", R)
                .Sfx("sfx_tach_hai")
                .Narrate("Tôi nhớ lại hai tiếng tách.")
                .Sfx("sfx_tach_bon")
                .Narrate("Rồi bốn tiếng tách, trong bếp lò.")
                .Narrate("Cúc sang so mực. Cô đặt kính lúp lên từng vạch chụp từ hốc tường.")
                .Say(Cuc, T, "Vạch ngày mười ba tháng mười cùng mực với dòng “Đừng đếm”.", R)
                .Say(Cuc, N, "Cùng mực với cây bút bi trong túi áo anh.", R)
                .Narrate("Cây bút tôi mượn của phụ xe. Nó vẫn nằm trong túi áo tôi.")
                .Say(Cuc, W, "Mấy vạch bút chì từ 2019 tới 2025 cũng vậy. Kẻ từ phải sang trái, đè đậm bên phải.", R)
                .Say(Cuc, N, "Nét của người thuận tay trái.", R)
                .Narrate("Cột phải: “Vạch cuối: bút của tôi, đêm tôi ngất, tay trái.”")
                .Narrate("Lề: “Tôi thuận tay phải. Bảy vạch trước cũng tay trái.”")
                .Pause(1600)
                .Narrate("Ông Tồn đưa tôi ký nhận bản sao. Tôi ký bằng tay phải. Chậm, như đang chép lại.")
                .ThenWhenDone("Q17", "Q17_done_khoa");

            yield return Scene("Q17_log_khoa", "Gói cuối", NarrativeTrigger.Signal, Sig(QuestSignal.TalkTamVa), q17Step,
                    "Nhật ký hộp: sáu gói “đã nhận” mang đúng sáu ngày mất. Gói thứ bảy chờ “phần thứ bảy được viết”. " +
                    "Khoa cầm bút định viết, rồi đặt xuống.", location: "")
                .Say(Tam, N, "Xấp đầu tiên đây. Tôi in từ cái ngày nó bắt đầu chạy.", R)
                .Music("")
                .Narrate("Nhật ký có sáu gói “đã nhận”. Mỗi gói một ngày.")
                .Narrate("Tôi không cần mở sổ để dò. Sáu ngày mất của nhà tôi, tôi thuộc.")
                .Pause(1600)
                .Narrate("Gói thứ bảy đang chờ. Trạng thái: “chờ xác nhận: phần thứ bảy được viết”.")
                .Backdrop("bg_so_bia_vai_bay_phan")
                .Narrate("Tôi mở quyển sổ bìa vải tới phần còn trắng. Tôi cầm bút.")
                .Narrate("Rồi tôi đặt bút xuống. Tôi chưa viết.")
                .Narrate("Có một cái hộp đang chờ tôi viết.")
                .Pause(1600)
                .Backdrop("")
                .Narrate("Tám Vá không nói gì. Ông gõ ba cái lên xấp giấy, như gõ lên một cái máy.")
                .Sfx("sfx_go_ba_cai")
                .Narrate("Cột phải: “Gói 7 đang chờ phần 7.”")
                .Narrate("Lề: “Sáu gói trùng sáu ngày mất. Trùng chưa phải là gây ra. Nhưng tôi không viết khi hộp còn chạy.”")
                .ThenWhenDone("Q17", "Q17_done_khoa");

            yield return Scene("Q17_done_khoa", "Phần còn trắng", NarrativeTrigger.Manual, "", IsKhoa,
                    "Khoa ghép sáu phần sổ cũ với sáu nhóm quan sát: khớp từng phần. Phần thứ bảy để trắng.", location: "")
                .FadeOut(600)
                .FadeIn(600)
                .Narrate("Tối đó tôi trải hết lên bàn: sổ gửi thư, đoạn camera, nhật ký hộp, cuộn băng.")
                .Narrate("Tôi ghép sáu phần sổ cũ của nhà tôi với sáu nhóm quan sát. Phần nào cũng khớp.")
                .Narrate("Phần thứ bảy để trắng.")
                .Narrate("Tôi có nửa câu trả lời. Người gửi lá thư là tôi. Cái hộp đang chờ gửi phần của tôi.")
                .Notebook("Q17")
                .Then("Q18_start_khoa");

            // ---- Q18 Gom đàn chứ đừng gom sợ (Khoa's one slip of the chapter: he answers a voice nobody used)
            yield return Scene("Q18_start_khoa", "Gom đàn chứ đừng gom sợ", NarrativeTrigger.Manual, "", IsKhoa,
                    "Họp ở nhà chính: bảng “đủ / thiếu / chưa biết”. Tám Vá đoán cái hộp mượn chỗ trống của đàn. " +
                    "Muốn cắt đúng kênh phải có mẫu đàn: ngoài đợt, gom đàn về Nơi Trú.")
                .PlaceAt(Ca, "farm:station:main_house", CardinalDirection.South)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.West)
                .Narrate("Họp ở nhà chính. Anh Ca mang tới một tờ giấy kẻ ba cột: “đủ / thiếu / chưa biết”.")
                .Say(Ca, W, "Cột “chưa biết” dài nhất. Tôi kẻ lại hai lần rồi, nó vẫn dài nhất.", R)
                .Say(K, N, "Thêm cột thứ tư. “Chỉ tôi thấy.”")
                .Say(Ca, T, "Giấy không đủ khổ, cậu.", R)
                .Narrate("Cả buổi không ai nhắc chữ “khách”.")
                .Say(Tam, T, "Tôi có một giả thuyết. Cậu ghi ngoài lề thôi, đừng ghi vô cột.", R)
                .Say(Tam, N, "Phần cuối cuốn sổ nhà cậu còn trống. Cái hộp mượn chỗ trống trong đàn để giữ chỗ cho gói cuối.", R)
                .Say(Tam, N, "Có khi nó gây ra chỗ trống. Có khi nó chỉ đọc một thứ có sẵn. Tôi không biết.", R)
                .Say(Tam, T, "Kênh của gói đó chỉ mở trong đợt lớn, lẫn giữa các kênh khác. Muốn cắt đúng thì phải có mẫu đàn.", R)
                .Narrate("Ông chỉ vào dòng trạng thái: “chờ xác nhận: phần thứ bảy được viết”.")
                .Say(Tam, N, "Nó không vội. Nó chờ cậu.", R)
                .Say(K, N, "Ừ, biết rồi.")
                .Narrate("Tám Vá ngừng gõ ngón tay. Anh Ca đưa tôi tờ giấy tiếp theo.")
                .Narrate("Không cắt thì hôm nay không ai mất gì, kể cả khi vài chục đợt lớn trôi qua.")
                .Narrate("Chỉ là tôi sẽ không bao giờ dám viết phần của mình vào sổ nhà mình. Và đàn sẽ mãi chừa ra một chỗ.")
                .Say(Tam, N, "Ngoài đợt, cậu gom đàn về Nơi Trú cho đủ. Đứng yên một chỗ là tôi ghi mẫu.", R)
                .Narrate("Lề: “Hộp mượn chỗ trống. Hoặc chỉ đang đọc nó.”")
                .Narrate("Lúc về tôi mới nhận ra: lúc tôi nói “biết rồi”, không ai nói gì với tôi cả.")
                .Hint("whistle");

            yield return Scene("Q18_done_khoa", "Mẫu đàn", NarrativeTrigger.QuestCompleted, "Q18", IsKhoa,
                    "Đàn đứng yên trong Nơi Trú, Tám Vá ghi mẫu vào hộp và đưa Khoa các bước ngắt tuyến (chỉ dùng ngoài đợt).",
                    priority: 10)
                .Narrate("Đàn về Nơi Trú, đứng yên. Tám Vá cắm dây vào hộp, đọc từng con số, chậm.")
                .Say(Tam, H, "Có mẫu rồi. Đợt lớn tới nó mở kênh nào, mình so là biết.", R)
                .Say(Tam, N, "Còn đây là các bước ngắt tuyến. Chỉ làm ngoài đợt.", R)
                .Narrate("Tôi kẹp tờ giấy vào sổ. Đàn vẫn đứng như trong ảnh. Vẫn chừa một chỗ.")
                .Notebook("Q18")
                .Then("Q19_start_khoa");

            // ---- Q19 Bữa tối chưa có khách (loss scene: no report voice, no joke, no notebook line)
            yield return Scene("Q19_start_khoa", "Bữa tối chưa có khách", NarrativeTrigger.Manual, "", IsKhoa,
                    "Bà Sáu mời Khoa tối nay ăn cơm ở quán, dặn mang theo cái bát ở nhà cũ. Trước khi đi, Khoa nhìn đàn từ nhà chính.")
                .Narrate("Chiều, Mận ghé.")
                .Say(Man, N, "Bà Sáu dặn tối nay anh qua quán ăn cơm. Bà nấu canh chua.", R)
                .Say(K, N, "Có dịp gì.")
                .Say(Man, N, "Không có dịp gì hết. Bà dặn anh mang theo cái bát ở nhà cũ. Bà muốn coi.", R)
                .Narrate("Tôi gói cái bát men lam vào khăn. Trước khi đi, tôi định đứng ở cửa sổ nhà chính nhìn đàn một lúc.")
                .Narrate("Việc đó thành thói quen rồi.")
                .Hint("interact");

            string q19Step = Step("Q18_done_khoa", "Q19_done_khoa", IsKhoa);

            yield return Scene("Q19_window_khoa", "Cửa sổ", NarrativeTrigger.Signal, Sig(QuestSignal.HouseBoxInspected), q19Step,
                    "Qua cửa sổ nhà chính, đàn vẫn chừa một chỗ. Khoa ghi giờ.", priority: 10)
                .Narrate("Qua cửa sổ nhà chính, đàn đang ăn. Sáu con heo già đứng đúng chỗ cũ.")
                .Narrate("Vẫn chừa ra một chỗ.")
                .Narrate("Tôi ghi giờ. Rồi tôi thấy mình đang ghi giờ cho một chỗ trống.")
                .ThenWhenDone("Q19", "Q19_done_khoa");

            yield return Scene("Q19_kitchen_khoa", "Bữa tối chưa có khách", NarrativeTrigger.Signal, Sig(QuestSignal.TalkBaSau), q19Step,
                    "Mặt vòng phản chiếu một bàn ăn bảy chỗ. Bà Sáu kể chuyện mẹ đo chiều cao cả nhà mỗi hè, và cái bóng " +
                    "cao hơn ông Năm một cái đầu. Bà đặt khăn trước mặt Khoa; cái bóng trong mặt vòng đang khóc.", location: "")
                .Music("")
                .Backdrop("bg_quan_ba_sau_bep")
                .Narrate("Bà Sáu dọn cơm trong bếp, như mọi bữa: cho sáu với một người. Tôi ngồi chỗ của một.")
                .Narrate("Tôi mở khăn, đặt cái bát mang từ nhà cũ lên bàn. Men lam, sứt miệng.")
                .Narrate("Rồi tôi đặt chiếc vòng của mẹ cạnh cái bát.")
                .Backdrop("bg_vong_ban_an_bay_cho")
                .Narrate("Mặt kim loại phản chiếu một bàn ăn. Bảy chỗ.")
                .Narrate("Tôi nghiêng đầu. Hình biến mất.")
                .Backdrop("bg_quan_ba_sau_bep")
                .Narrate("Bà Sáu múc canh, chan cho tôi trước.")
                .Say(Sau, N, "Mẹ con lúc nào cũng cho thằng Khoa ngồi gần nồi cơm.", R)
                .Say(Sau, H, "Cuối mỗi mùa hè, trước hôm về, mẹ con đo cho cả nhà. Con bé An cầm bút chì kẻ.", R)
                .Say(Sau, H, "Ông bà cũng phải đứng vô. Cho đủ.", R)
                .Say(Sau, Sd, "Năm ông con mất thì nhà con không lên.", R)
                .Say(Sau, N, "Năm sau nữa, đêm mồng năm tháng mười, bà thấy đèn phòng đó sáng một lát.", R)
                .Say(Sau, N, "Bà tưởng ông Năm về lấy đồ.", R)
                .Pause(1600)
                .Say(Sau, W, "Mà cái bóng trong phòng cao hơn ông Năm một cái đầu.", R)
                .Narrate("Tôi ngồi yên.")
                .Narrate("Bà Sáu lặng lẽ đặt một chiếc khăn trước mặt tôi.")
                .Narrate("Tôi không hiểu vì sao bà đặt khăn. Cho tới khi tôi nhìn xuống mặt vòng.")
                .Backdrop("bg_vong_ban_an_bay_cho")
                .Reflection(Portraits.KhoaReflectCry)
                .Narrate("Cái bóng trong đó đang khóc.")
                .Pause(2600)
                .Backdrop("bg_quan_ba_sau_bep")
                .Say(Sau, N, "Ăn thêm miếng nữa đi con.", R)
                .ThenWhenDone("Q19", "Q19_done_khoa");

            yield return Scene("Q19_done_khoa", "Không ghi", NarrativeTrigger.Manual, "", IsKhoa,
                    "Khoa đi bộ về trại. Tối đó anh mở sổ và không ghi gì.", location: "")
                .FadeOut(600)
                .Music("")
                .FadeIn(600)
                .Narrate("Tôi đi bộ về trại. Đường tối. Tôi không đếm bước.")
                .Narrate("Tối đó tôi mở sổ.")
                .Narrate("Tôi không ghi gì.")
                .Pause(2000)
                .Narrate("Sáng ra, con heo chân trắng nằm ngửa chắn ngay cửa, chờ ai gõ máng. Tôi gõ. Nó lật dậy.");

            // ---- Q20 Rìu Sắt trả hàng (opens from cycle 20; the box can be opened once a major wave has closed cycle 20)
            yield return Scene("Q20_start_khoa", "Rìu Sắt trả hàng", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q20,CYCLE_NUMBER 20," + IsKhoa,
                    "Mẫu đàn đã cắm vào hộp. Đợt lớn kế tiếp, hộp tự ghi các kênh; Khoa chỉ cần giữ trại qua đợt đó, rồi mở hộp.",
                    priority: 18)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Say(Tam, N, "Mẫu đàn tôi cắm vô hộp rồi.", R)
                .IfGoto("CYCLE_NUMBER 21", "ready")
                .Say(Tam, N, "Đợt lớn tới, kênh nào mở là nó tự ghi. Trong lúc đánh, cậu không phải làm gì với nó.", R)
                .Say(Tam, T, "Cậu chỉ việc giữ trại qua đêm đó. Thắng thua gì nó cũng ghi.", R)
                .Say(K, N, "Rồi sao.")
                .Say(Tam, N, "Sáng ra mở hộp. So mẫu với các kênh. Kênh nào lạ thì cắt.", R)
                .Narrate("Tôi ghi lịch. Đợt lớn tới. Sau đó: mở hộp.")
                .Hint("defend")
                .Goto("out")
                .Label("ready")
                .Say(Tam, N, "Đợt lớn vừa rồi nó ghi đủ cả. Cậu vô nhà chính mở hộp đi. So mẫu với các kênh.", R)
                .Flag(Q20ReadyFlag)
                .Hint("interact")
                .Label("out");

            yield return Scene("Q20_ready_khoa", "Giấy in dài tới cửa", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q20,CYCLE_NUMBER 21,SCENE_SEEN Q20_start_khoa,!STORY_FLAG " + Q20ReadyFlag + "," + IsKhoa,
                    "Đợt lớn đã qua, hộp ghi đủ các kênh. Tám Vá bảo Khoa vào nhà chính mở hộp.", priority: 19)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Sáng. Đợt lớn đã qua. Rào còn đứng, sổ kho ghi đủ thiệt hại.")
                .Say(Tam, H, "Đêm qua nó ghi đủ. Giấy in ra dài tới cửa.", R)
                .Say(Tam, N, "Vô nhà chính mở hộp đi cậu. Mẫu đàn tôi để sẵn bên cạnh.", R)
                .Flag(Q20ReadyFlag)
                .Hint("interact");

            yield return Scene("Q20_box_khoa", "Cắt kênh", NarrativeTrigger.Signal, Sig(QuestSignal.HouseBoxInspected),
                    "STORY_QUEST_ACTIVE Q20,CYCLE_NUMBER 21,STORY_FLAG " + Q20ReadyFlag + "," + IsKhoa,
                    "Khoa so mẫu đàn với các kênh và cắt kênh của gói cuối: “hủy — không tìm thấy người gửi”. Sáng sau, ở cổng " +
                    "có kiện “trả về người gửi”: cái bát men lam sứt miệng thứ hai. Đàn không còn chừa chỗ.", priority: 20)
                .Music("")
                .Backdrop("bg_hop_nhat_ky_kenh")
                .Narrate("Trên giấy in, các kênh chồng lên nhau như rễ cây. Tôi đặt mẫu đàn lên trên.")
                .Narrate("Khớp hết. Trừ một đường.")
                .Narrate("Đường đó mở đúng lúc đàn chừa chỗ, đóng đúng lúc đàn đứng lại. Nhãn: gói thứ bảy.")
                .Say(Tam, N, "Cái đó. Cắt thì cậu cắt. Máy không phải của tôi.", R)
                .Choice("Kênh của gói cuối", 0, SceneDefinition.Opt("Cắt kênh"))
                .Sfx("sfx_tach")
                .Narrate("Một tiếng tách. Không có gì sáng lên. Không có gì tắt.")
                .Narrate("Dòng trạng thái đổi: “hủy — không tìm thấy người gửi”.")
                .Sfx("sfx_go_ba_cai")
                .Say(Tam, N, "Nghe vẫn thành thật.", R)
                .Flag(Q20CutFlag)
                .Pause(1600)
                .FadeOut(700)
                .Backdrop("bg_cong_trai_kien_tra_ve", 0)
                .FadeIn(700)
                .Narrate("Sáng hôm sau, ở cổng trại có một kiện hàng. Nhãn dán: “trả về người gửi”.")
                .Narrate("Bên trong là một cái bát men lam sứt miệng. Giống hệt cái tôi mang theo từ thành phố, đến cả vết sứt.")
                .Say(Ton, T, "Sổ kho tôi ghi: chưa xác định nguồn.", R)
                .Narrate("Tôi ghi cả hai cái bát vào cột phải, vì ai cũng cầm được. Ngoài lề, bút chì: “Là một cái?”")
                .Pause(1600)
                .Backdrop("")
                .Narrate("Ngoài sân, đàn heo đứng ăn. Tôi đếm.")
                .Narrate("Không còn chỗ trống.")
                .Pause(1800)
                .Narrate("Con heo chân trắng chen vào đúng chỗ đó, nằm ngửa, giả chết. Nó chọn chỗ đẹp nhất.")
                .Notebook("Q20")
                .Then("Q20_choice_khoa");

            // The choice. Not skippable (a skip would pick for the player); repeatable while Q20 stays open ("Để rồi tính").
            yield return Scene("Q20_choice_khoa", "Giữ lại gì", NarrativeTrigger.Signal, Sig(QuestSignal.HouseBoxInspected),
                    "STORY_QUEST_ACTIVE Q20,STORY_FLAG " + Q20CutFlag + ",!STORY_FLAG " + EndingChosenFlag + "," + IsKhoa,
                    "Gói cuối đã hủy. Còn cái hộp, chiếc vòng và hai cái bát: Khoa chọn giữ lại gì.", priority: 15)
                .NoSkip()
                .Repeatable()
                .Music("")
                .Say(Tam, N, "Tháo hẳn bộ gửi thì tôi tháo được. Giữ bộ thu thì Cúc với ông Tồn mừng.", R)
                .Say(Tam, N, "Còn cái vòng là của nhà cậu.", R)
                .Narrate("Cái hộp đã thôi chờ. Tôi thì chưa.")
                .Choice("Giữ lại gì?", 0,
                    EndingOpt("Giữ trại, tháo đường gửi", Ending81Flag, StoryEndingId.Ending_8_1_GiuTraiThaoDuongGui, "E81_khoa"),
                    EndingOpt("Giữ máy để làm hồ sơ", Ending82Flag, StoryEndingId.Ending_8_2_GiuMayLamHoSo, "E82_khoa"),
                    EndingOpt("Mang vòng về nhà", Ending83Flag, StoryEndingId.Ending_8_3_MangVongVeNha, "E83_khoa"),
                    SceneDefinition.Opt("Để rồi tính.", "later"))
                .Label("chosen")
                .Narrate("Tôi đóng nắp hộp. Lần này tôi ghi ngày bằng tay phải.")
                .End()
                .Label("later")
                .Narrate("Tôi đóng nắp hộp. Để rồi tính.")
                .Say(Tam, N, "Nó không vội. Cậu cũng đừng vội. Chừng nào chọn được thì mở nắp, tôi để nguyên đó.", R);

            // ---- 8.1 Giữ trại, tháo đường gửi
            yield return Scene("E81_khoa", "Giữ trại, tháo đường gửi", NarrativeTrigger.QuestCompleted, "Q20",
                    "STORY_FLAG " + Ending81Flag + "," + IsKhoa,
                    "Tám Vá tháo bộ gửi. Khoa viết phần thứ bảy của sổ, kẻ vạch mới bằng tay phải, về nhà cũ lật ngửa sáu cái bát. " +
                    "Sổ bàn giao có phiếu mới: “không tìm thấy người gửi”, ô người nhận để trống.", priority: 30)
                .Music("")
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Tám Vá tháo bộ phận gửi xác nhận. Ông tháo rất chậm, lập biên bản cho từng con ốc.")
                .Say(Tam, N, "Ốc. Bình thường.", R)
                .Say(Tam, N, "Ốc. Bình thường.", R)
                .Say(Tam, T, "Ốc. Hơi lỏng. Bình thường.", R)
                .Narrate("Ông đưa tôi con ốc cuối. Tôi nhận bằng hai lòng bàn tay ngửa.")
                .Narrate("Ông chỉ tháo đúng một bộ phận. Mọi máy khác trong trại vẫn chạy như cũ.")
                .FadeOut(600)
                .Backdrop("bg_so_bia_vai_bay_phan", 0)
                .FadeIn(600)
                .Narrate("Tối đó tôi viết phần thứ bảy của quyển sổ bìa vải.")
                .Narrate("Tôi không viết lời từ biệt thay cho mẹ.")
                .Narrate("Tôi ghi những lần tự dẫn đàn về trú, những khoản tiền sửa rào, tên những người đã giúp tôi.")
                .Narrate("Phần của tôi hóa ra là một bảng kế toán. An chắc sẽ chê.")
                .Backdrop("bg_hoc_tuong_bay_cot")
                .Narrate("Rồi tôi vào phòng bếp lò, mở cái hốc bằng mảnh da, kẻ một vạch mới trong cột của mình.")
                .Narrate("Bút chì. Tay phải. Từ trái sang phải. Tôi ghi ngày bên cạnh.")
                .Narrate("Lần đầu tiên kể từ hồi An còn cầm bút chì, cột của tôi có một vạch mà tôi nhớ mình đã kẻ.")
                .Narrate("Cột phải: “Hôm nay. Tay phải.”")
                .Music("bgm_nha_cu_lang")
                .Backdrop("bg_bep_me_gia_bat")
                .Narrate("Tôi về căn nhà cũ. Trên giá, sáu cái bát vẫn úp như hôm mẹ úp.")
                .Narrate("Tôi lật ngửa từng cái.")
                .Pause(1600)
                .Narrate("Sáng hôm sau, chúng vẫn nằm ngửa. Lần đầu tiên sau ngần ấy năm.")
                .Music("")
                .Backdrop("")
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                .Narrate("Tôi quay lại trại.")
                .Say(Man, H, "Lứa mới có một con nhỏ xíu. Anh muốn giữ nó không?", R)
                .Say(K, N, "Giống gì.")
                .Say(Man, T, "Chưa biết. Mà chắc chắn là ăn khỏe.", R)
                .Backdrop("bg_so_tay")
                .Narrate("Trong sổ bàn giao có thêm một phiếu mới, đóng dấu “không tìm thấy người gửi”.")
                .Narrate("Ô người nhận vẫn để trống.")
                .Pause(2000)
                .Flag(ChapterDoneFlag);

            // ---- 8.2 Giữ máy để làm hồ sơ
            yield return Scene("E82_khoa", "Giữ máy để làm hồ sơ", NarrativeTrigger.QuestCompleted, "Q20",
                    "STORY_FLAG " + Ending82Flag + "," + IsKhoa,
                    "Khoa cắt chức năng gửi, giữ bộ thu để nghiên cứu cùng Cúc và ông Tồn. Hai người nhận trại trước cũng 24 tuổi; " +
                    "một người vẫn gửi thư về. Bộ thu nhận một dòng: “Sáu với một vị trí: đủ.”", priority: 30)
                .Music("")
                .Narrate("Tôi cắt chức năng gửi. Bộ thu thì giữ lại, để cùng Cúc và ông Tồn làm hồ sơ.")
                .Say(Cuc, N, "Hồ sơ chia ba cột: đã xác nhận, ai kể lại, còn là suy đoán.", R)
                .Say(K, N, "Thêm cột thứ tư. “Chỉ Khoa thấy.”")
                .Say(Cuc, W, "Cột đó dài lắm anh. Phải in khổ A3.", R)
                .Narrate("Những lần thấy người thân ở bệnh viện vào cột của tôi.")
                .Narrate("Bia mộ, phiếu thu, sơ đồ trạm vào cột đối chiếu được.")
                .Narrate("Ông Tồn mở hai hồ sơ bàn giao cũ. Hai người từng nhận trại trước tôi cũng tới đây năm hai mươi bốn tuổi.")
                .Narrate("Ngày sinh khác tôi. Một người còn gửi thư về sau khi đã rời trại.")
                .Say(Ton, N, "Người này đi rồi mà thư vẫn về. Đều lắm.", R)
                .Narrate("Vậy người cầm vòng không nhất thiết phải chết, hay biến mất.")
                .Narrate("Tôi ghi vào cột “Có thật”. Gạch chân hai lần.")
                .Pause(1600)
                .FadeOut(600)
                .Backdrop("bg_so_tay", 0)
                .FadeIn(600)
                .Narrate("Đêm đó, bộ thu nhận được một gói dữ liệu. Chỉ một dòng.")
                .Narrate("“Sáu với một vị trí: đủ.”")
                .Pause(1800)
                .Narrate("Sinh nhật sau, tôi mở máy lại. Ô xác nhận tuổi vẫn đòi nhập số 24.")
                .Pause(2000)
                .Flag(ChapterDoneFlag);

            // ---- 8.3 Mang vòng về nhà
            yield return Scene("E83_khoa", "Mang vòng về nhà", NarrativeTrigger.QuestCompleted, "Q20",
                    "STORY_FLAG " + Ending83Flag + "," + IsKhoa,
                    "Khoa mang chiếc vòng và cái bát thứ hai về nhà cũ. Suất mộ trống có một viên đá nhỏ. Cái bóng trong gương " +
                    "mỉm cười và không thôi. Trên bậc thang thứ bảy có đất Bờ Rìa còn mới.", priority: 30)
                .Music("")
                .PlaceAt(Man, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Sau khi ngắt tuyến, tôi xếp lịch chăm trại cho đủ số người tôi trả được lương.")
                .Say(Man, N, "Cám trước, nước sau. Em nhớ mà.", R)
                .FadeOut(600)
                .Music("bgm_nha_cu_lang")
                .Backdrop("bg_nha_cu_bua_com_ghe_trong", 0)
                .FadeIn(600)
                .Narrate("Tôi mang chiếc vòng và cái bát thứ hai về nhà cũ.")
                .Narrate("Tôi đặt hai cái bát sứt miệng giống hệt nhau lên bàn ăn. Một chỗ gần nồi cơm. Một chỗ ở cửa bếp.")
                .Music("")
                .Backdrop("bg_nghia_trang")
                .Narrate("Tôi ra nghĩa trang. Suất mộ thứ bảy vẫn trống.")
                .Narrate("Nhưng có ai đã đặt lên đó một viên đá nhỏ. Không có tên.")
                .Pause(1800)
                .Backdrop("bg_guong_nha_tam_mo")
                .Narrate("Về nhà, tôi đứng trước gương.")
                .Narrate("Người ta bảo mặt tôi vẫn như bảy buổi sáng năm ấy. Không buồn, không vui.")
                .Reflection(Portraits.KhoaReflectSmile)
                .Narrate("Cái bóng trong gương mỉm cười với tôi.")
                .Narrate("Tôi đứng đó rất lâu, chờ xem nó có thôi cười không.")
                .Pause(2400)
                .Narrate("Nó không thôi.")
                .Narrate("Tôi mở sổ, cầm bút. Lần đầu tiên tôi không biết phải ghi vào cột nào.")
                .Backdrop("bg_cau_thang_dat_do")
                .Narrate("Trên bậc thang thứ bảy có một ít đất màu nâu đỏ. Còn mới.")
                .Narrate("Tám Vá lên tận nơi coi. Ông bốc một nhúm, bóp, ngửi.")
                .Say(Tam, T, "Đất Bờ Rìa.", R)
                .Say(K, N, "Mang tới lúc nào.")
                .Sfx("sfx_go_ba_cai")
                .Say(Tam, N, "Cái đó thì tôi không biết.", R)
                .Pause(2400)
                .Flag(ChapterDoneFlag);

            // ---- 8.4 Sau kết thúc (any ending, also when it was picked from the journal) ⇒ 8.5 post-credit
            yield return Scene("S1_after_khoa", "Sau kết thúc", NarrativeTrigger.DayStart, "", "STORY_QUEST_DONE Q20," + IsKhoa,
                    "Sáng đầu tiên sau truyện: trại vẫn nguyên, đàn vẫn đói đúng giờ. Giám Đốc đòi mở cuộc họp. " +
                    "Đàn không còn chừa chỗ; Khoa đếm ba lần rồi thôi đếm.", priority: 30)
                .Music("bgm_trai_cu_sang")
                .PlaceAt(Ca, "farm:station:main_house", CardinalDirection.South)
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                .Narrate("Sáng. Trại vẫn là trại. Rào đứng chỗ cũ, máy chạy chỗ cũ, đàn vẫn đói đúng giờ.")
                .Narrate("Sáu người đã mất không sống lại. Tôi biết. Tôi ghi điều đó từ lâu rồi.")
                .Narrate("Tôi vẫn đôi khi nghe và thấy những thứ người khác không thấy.")
                .Narrate("Tôi xử lý theo những cách tôi đã học: ghi sổ, uống thuốc, đi cho heo ăn.")
                .Narrate("Con ngỗng Giám Đốc đứng cạnh cái hộp trong nhà chính, kêu inh ỏi.")
                .Say(Ca, T, "Nó muốn mở cuộc họp.", R)
                .Say(K, N, "Ai dự.")
                .Say(Man, N, "Nãy giờ mới có mình nó.", R)
                .Narrate("Ngoài sân, đàn heo đứng ăn. Không con nào chừa chỗ trống. Hôm sau cũng không.")
                .Narrate("Tôi đếm ba lần, rồi thôi đếm.")
                .Pause(1600)
                .Banner("Hết Mùa 1", 2800)
                .Flag(ChapterDoneFlag)
                .Then("S1_postcredit_khoa");

            yield return Scene("S1_postcredit_khoa", "Người tới sớm", NarrativeTrigger.Manual, "", IsKhoa,
                    "Một tuần sau, Mận tìm lại kẹp tóc của mẹ dưới máng heo. Tối đó, cái chai trên bậu cửa cắm một cành hoa sứ: " +
                    "sáu bông nở, một nụ. Ở chữ Rìa, Mận đón một người xuống xe: “Anh tới sớm hai phút.”")
                .FadeOut(800)
                .Music("")
                .FadeIn(800)
                .Narrate("Một tuần sau. Một buổi sáng bình thường ở trại.")
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                .Say(Man, H, "Anh! Kẹp tóc của mẹ em nè. Em làm rớt hồi lớp ba.", R)
                .Say(Man, U, "Nằm ngay dưới máng heo. Cái máng em cọ cả nghìn lần rồi.", R)
                .Narrate("Tôi đứng im.")
                .Say(Man, N, "Anh không mừng cho em hả?", R)
                .Say(K, N, "Có.")
                .Narrate("Tôi không nói rằng tôi vừa ghi ngày hôm nay vào sổ.")
                .FadeOut(700)
                .Backdrop("bg_bau_cua_hoa_su_bay_bong", 0)
                .FadeIn(700)
                .Narrate("Tối đó, đi ngang nhà chính, tôi ngửi thấy mùi hoa sứ.")
                .Narrate("Trên bậu cửa sổ, cái chai thủy tinh rỗng từ ngày tôi tới đang cắm một cành hoa sứ trắng.")
                .Narrate("Bảy bông. Sáu bông nở, một nụ.")
                .Narrate("Tôi không hỏi ai cắm. Sẽ chẳng ai nhận, và cũng chẳng ai thấy lạ.")
                .Narrate("Lần này không có ai gọi tôi đi bưng nồi cơm. Tôi đứng lại, nhìn cho tới khi chắc mình sẽ không quên.")
                .Pause(1800)
                .Backdrop("bg_bien_bo_ria")
                .Sfx("sfx_coi_xe_khach")
                .Narrate("Cùng lúc đó, ở chữ Rìa dưới tấm biển, một chiếc xe khách dừng lại.")
                .Narrate("Mận đứng đợi sẵn với cây dù đen. Không ai báo trước cho cô chuyến này.")
                .Narrate("Cô nhìn đồng hồ, rồi nói với người vừa xuống xe:")
                .Say(Man, N, "Anh tới sớm hai phút.", R)
                .Pause(1400)
                .FadeOut(900)
                .Backdrop("bg_ngay_mat", 0)
                .FadeIn(600)
                .Banner("Mùa 2 — Người tới sớm", 3600);
        }

        // =====================================================================================================
        // An — hỏi ngây ngô, khen đúng chỗ, để người ta tự nói; nghe ra trước rồi mới nhìn; viết thư ban đêm,
        // không bao giờ mở sổ trước mặt ai. ★ ai cũng kiểm được, ☾ chỉ An nghe ra, bút chì ngoài lề là đoán.
        // =====================================================================================================
        static IEnumerable<SceneDefinition> An()
        {
            yield return Scene("C4_card_an", "Chương 4 — Bữa tối chưa có khách", NarrativeTrigger.DayStart, "",
                    "STORY_FLAG " + PreviousChapterFlag + "," + IsAn,
                    "Tám Vá muốn coi lại lưới điện trước đợt lớn. An ghé nhà chú, rồi xây hoặc nâng một thiết bị.", priority: 20)
                .Music("")
                .Banner("Chương 4 — Bữa tối chưa có khách", 2400)
                .Narrate("“Nhà mình ơi, bà Sáu dọn cơm cho sáu với một người. An ngồi chỗ của một :)”")
                .Narrate("Ngoài lề, bút chì: “Ai gửi lá thư? Cái hộp đang chờ gửi gì?”")
                .IfGoto("STORY_QUEST_DONE Q16", "q16_done")
                .IfGoto("!STORY_QUEST_ACTIVE Q16", "end")
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Say(Tam, N, "Sắp tới đợt lớn. Lưới điện trại cháu phải coi lại.", R)
                .Say(Tam, H, "Chiều ghé nhà chú ngoài xóm. Chú trải sơ đồ ra cho coi chỗ nào đang gánh quá sức.", R)
                .Say(A, H, "Dạ! Con mang trà qua nha chú.")
                .Hint("interact")
                .Goto("end")
                .Label("q16_done")
                .Then("Q16_done_an")
                .Label("end");

            const string Q16Step = "STORY_FLAG " + PreviousChapterFlag + ",SCENE_SEEN C4_card_an,!SCENE_SEEN Q16_done_an," + IsAn;

            yield return Scene("Q16_grid_an", "Sơ đồ lưới", NarrativeTrigger.Signal, Sig(QuestSignal.TalkTamVa), Q16Step,
                    "An hỏi vì sao máy nào của Tám Vá cũng tên như chiêu võ, rồi xin đặt tên giùm cái hộp. " +
                    "Tám Vá: “Cái đó không phải máy của tôi.” Ông bắt đầu in nhật ký của nó; An cần xây hoặc nâng một thiết bị.",
                    location: "")
                .Narrate("Tám Vá trải sơ đồ lưới điện ra bàn. Tôi rót trà. Máy nào cũng có tên: Cửu Tiêu Lôi Pháo, Thiên Nhãn Vọng Đài.")
                .Say(A, U, "Sao máy nào của chú cũng tên nghe như chiêu võ vậy chú?")
                .Say(Tam, H, "Tên càng kêu thì người ta càng chịu đọc hướng dẫn.", R)
                .Say(Tam, N, "Dầu thì vẫn tính theo lít.", R)
                .Say(A, L, "Hay ghê. Vậy cái hộp trong nhà chính tên gì chú? Hay con đặt giùm nha.")
                .Narrate("Chú đặt chén trà xuống. Chén chạm bàn mà không kêu tiếng nào.")
                .Say(Tam, T, "Cái đó không phải máy của tôi. Nên tôi không đặt tên.", R)
                .Narrate("Chú vừa xưng “tôi” với tôi. Cả buổi trước đó chú xưng “chú”.")
                .Say(Tam, N, "Từ tối nay chú in nhật ký gửi của nó ra giấy. Mỗi tối một xấp.", R)
                .Say(Tam, N, "Còn lưới điện: chỗ nào thiếu thì xây thêm, hoặc nâng một máy lên. Xây xong chú gõ thử.", R)
                .Hint("build")
                .ThenWhenDone("Q16", "Q16_done_an");

            yield return Scene("Q16_built_an", "Gõ thử", NarrativeTrigger.Signal, Sig(QuestSignal.StructureBuilt), Q16Step,
                    "Tám Vá gõ ba cái lên thiết bị mới. “Kêu đúng.”")
                .Narrate("Chiều, Tám Vá ghé ngang. Chú gõ ba cái lên cái máy mới, nghiêng tai nghe.")
                .Sfx("sfx_go_ba_cai")
                .Say(Tam, H, "Kêu đúng.", R)
                .Say(A, L, "Nó khen chú hả?")
                .Say(Tam, N, "Nó khai thật.", R)
                .IfGoto("STORY_QUEST_DONE Q16", "chain")
                .Say(Tam, N, "Ghé nhà chú coi sơ đồ lưới nữa là xong.", R)
                .Hint("interact")
                .Goto("end")
                .Label("chain")
                .Then("Q16_done_an")
                .Label("end");

            yield return Scene("Q16_done_an", "Áo mưa trong kho", NarrativeTrigger.Manual, "", IsAn,
                    "Trong kho có chiếc áo mưa xám đen, vai dính đất đỏ, mùi cám heo. Áo dài hơn ông Năm; " +
                    "Tám Vá chưa thấy ông Năm mặc bao giờ.", location: "")
                .IfGoto("SCENE_SEEN Q16_grid_an", "coat")
                .Say(A, H, "Chú ơi, cái hộp trong nhà chính tên gì vậy chú?")
                .Say(Tam, T, "Cái đó không phải máy của tôi. Nên tôi không đặt tên.", R)
                .Say(Tam, N, "Từ tối nay chú in nhật ký gửi của nó ra giấy. Mỗi tối một xấp.", R)
                .Label("coat")
                .Narrate("Tám Vá cần giấy in. Tôi chạy vào kho trại lấy giùm.")
                .Music("")
                .Backdrop("bg_kho_trai_ao_mua")
                .Narrate("Trên cây đinh sau cửa kho treo một chiếc áo mưa xám đen. Có mũ trùm. Vai dính một vệt đất đỏ.")
                .Narrate("Tôi áp mặt vào áo, như ngửi áo người quen. Mùi cám heo.")
                .Narrate("Mùi này tôi nhớ. Năm mười lăm tuổi tôi từng ngửi thấy nó. Ở đâu thì chưa nhớ ra.")
                .Say(A, H, "Chú ơi, ông Năm cao cỡ nào hả chú? Áo này dài quá trời.")
                .Narrate("Tám Vá nghĩ một lúc.")
                .Say(Tam, T, "Chưa thấy ổng mặc bao giờ.", R)
                .Pause(1800)
                .Backdrop("")
                .Narrate("Ngoài sân, một con heo ngoạm mất tờ giấy in đầu tiên. Tám Vá ghi biên bản: “Giấy. Bị ăn.”")
                .Say(A, L, "Chú ghi luôn: “Heo đọc trước.”")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, kho trại có một cái áo mưa xám đen, vai dính đất đỏ ★.”")
                .Narrate("Ngoài lề, bút chì: “Áo dài hơn ông Năm.”")
                .Notebook("Q16")
                .Then("Q17_start_an");

            // ---- Q17
            yield return Scene("Q17_start_an", "Chủ cũ không có mặt để ký", NarrativeTrigger.Manual, "", IsAn,
                    "Ông Năm để lại một cuộn băng cho người nhận trại. Ông Tồn có bản sao sổ gửi thư; Tám Vá in xong nhật ký hộp.",
                    location: "")
                .Narrate("Sáng hôm sau, Mận tới, tay cầm cây dù đen.")
                .Say(Man, N, "Chị ơi, hộc tủ nhà chính có cuộn băng của ông Năm. Ổng dặn đưa người nhận trại.", R)
                .Say(A, U, "Ơ, thật hả? Em nghe thử chưa?")
                .Say(Man, N, "Ổng dặn đưa chị. Em không nghe.", R)
                .Narrate("Mận nói xong thì nhìn xuống đất. Em không muốn nghe thật.")
                .Narrate("Cùng sáng đó, ông Tồn nhắn đã lấy được bản sao sổ gửi thư ở bưu điện. Tám Vá in xong xấp nhật ký đầu.")
                .Say(A, H, "Vậy hôm nay chị đi ba chỗ: nghe băng, qua ông Tồn, rồi qua chú Tám.")
                .Hint("interact");

            string q17Step = Step("Q16_done_an", "Q17_done_an", IsAn);

            yield return Scene("Q17_tape_an", "Cuộn băng", NarrativeTrigger.Signal, Sig(QuestSignal.HouseBoxInspected), q17Step,
                    "Băng của ông Năm: lịch chăm trại, rồi lời xin lỗi người nhận trại. An nghe ra tiếng bát kéo trên bàn " +
                    "trước cả câu cuối: “Tôi chưa mời ai.”", priority: 10)
                .Music("")
                .Narrate("Cuộn băng nằm trong hộc tủ. Nhãn viết tay: “Người nhận trại.”")
                .Narrate("Tôi bấm nghe. Tiếng rè. Rồi giọng ông Năm, chậm, khàn, như người đọc sổ.")
                .Narrate("“Cám trước. Nước sau. Rồi coi rào.”")
                .Narrate("Ông đọc lịch chăm trại gần hết một mặt băng. Tôi nghe ra cả tiếng ông lật trang.")
                .Narrate("Cuối băng, giọng ông nhỏ lại.")
                .Narrate("“Tôi xin lỗi người nhận trại. Tôi để cháu mang cái vòng lên mà không nói trước chuyện có thể xảy ra.”")
                .Pause(1400)
                .Narrate("Tiếng một cái bát bị kéo trên mặt bàn. Tôi nghe ra nó trước khi kịp hiểu.")
                .Narrate("“Tôi chưa mời ai.”")
                .Pause(2200)
                .Narrate("Băng hết.")
                .Narrate("Ngoài cửa sổ, con heo chân trắng húc cái xô rỗng, đều đều, như gõ cửa xin cơm.")
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, ông Năm xin lỗi người nhận trại ★.”")
                .Narrate("Ngoài lề: “Tiếng bát kéo. Ông ăn một mình, mà vẫn dọn cho ai đó.”")
                .ThenWhenDone("Q17", "Q17_done_an");

            yield return Scene("Q17_letter_an", "Sổ gửi thư", NarrativeTrigger.Signal, Sig(QuestSignal.TalkOngTon), q17Step,
                    "Camera bưu điện 21/09/2026: người gửi lá thư lúc 07:02 là An, ký tay trái, và cô gái ở quầy không cười. " +
                    "Ông Tồn đọc dấu nêm: 24. Cúc: mọi vạch sau 2017 đều nét tay trái.", location: "")
                .Narrate("Tôi phụ ông Tồn xếp lại chồng phiếu cũ trước. Ông xếp chậm, tôi xếp theo ông.")
                .Say(Ton, N, "Bản sao sổ gửi thư. Với một đoạn camera bưu điện, ngày hai mốt tháng chín.", R)
                .Music("")
                .Narrate("Góc hình ghi 07:02. Người đứng ở quầy gửi lá thư cho quán bà Sáu là tôi.")
                .Narrate("Cô nhân viên đưa bút. Cô gái ở quầy cầm lấy, ký.")
                .Narrate("Cô gái ấy không cười.")
                .Pause(1600)
                .Narrate("Tôi chưa từng không cười ở chỗ đông người.")
                .Narrate("Cô ấy ký bằng tay trái. Tôi thuận tay phải. Chuyện đó, tôi để tới chiều mới thấy sợ.")
                .Narrate("Ông Tồn cầm mảnh da lên, đọc hàng dấu nêm như đọc số trong sổ kho.")
                .Say(Ton, T, "Người xưa viết số như vầy. Hai dấu mười, bốn dấu một. Hai mươi tư.", R)
                .Sfx("sfx_tach_hai")
                .Narrate("Tôi nghe lại hai tiếng tách.")
                .Sfx("sfx_tach_bon")
                .Narrate("Rồi bốn tiếng tách, trong bếp lò.")
                .Narrate("Cúc sang so mực. Tôi đưa cô cây bút bi mượn của phụ xe. Nó vẫn nằm trong túi áo tôi.")
                .Say(Cuc, T, "Vạch ngày mười ba tháng mười cùng mực với dòng “Đừng đếm”. Cùng mực với cây bút này.", R)
                .Say(Cuc, W, "Mấy vạch sau năm 2017 đều kẻ từ phải sang trái, đè đậm bên phải. Nét người thuận tay trái.", R)
                .Say(A, H, "Chị Cúc nhìn nét chữ hay ghê. Dạy em với.")
                .Narrate("Cúc không cười lại. Cô cất kính lúp, chậm hơn mức cần thiết.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Mẹ ơi, người gửi thư cho quán bà Sáu là con ★.”")
                .Narrate("Ngoài lề, bút chì: “Cô gái ở quầy không cười. Mình nhìn cô ấy mà không nhận ra mình.”")
                .ThenWhenDone("Q17", "Q17_done_an");

            yield return Scene("Q17_log_an", "Gói cuối", NarrativeTrigger.Signal, Sig(QuestSignal.TalkTamVa), q17Step,
                    "Nhật ký hộp: sáu gói “đã nhận” mang đúng sáu ngày mất. Gói thứ bảy chờ “phần thứ bảy được viết”. " +
                    "An cầm bút định viết, rồi đặt xuống.", location: "")
                .Say(Tam, N, "Xấp đầu tiên đây. Chú in từ cái ngày nó bắt đầu chạy.", R)
                .Say(A, H, "Dày ghê. Chú in cả đêm hả chú?")
                .Music("")
                .Narrate("Nhật ký có sáu gói “đã nhận”. Mỗi gói một ngày.")
                .Narrate("Tôi không cần dò. Sáu ngày đó tôi thuộc, như thuộc giờ cơm.")
                .Pause(1600)
                .Narrate("Gói thứ bảy đang chờ. Trạng thái: “chờ xác nhận: phần thứ bảy được viết”.")
                .Backdrop("bg_so_bia_vai_bay_phan")
                .Narrate("Tôi mở quyển sổ bìa vải của nhà mình tới phần còn trắng. Tôi cầm bút.")
                .Narrate("Rồi tôi đặt bút xuống.")
                .Narrate("Có một cái hộp đang chờ tôi viết.")
                .Pause(1600)
                .Backdrop("")
                .Narrate("Tám Vá không nói gì. Chú rót cho tôi chén trà, lần đầu tiên chú rót trước.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, gói cuối đang chờ phần của An ★.”")
                .Narrate("Ngoài lề, bút chì: “Sáu gói trùng sáu ngày. Trùng chưa phải là gây ra. Nhưng hộp còn chạy thì An chưa viết.”")
                .ThenWhenDone("Q17", "Q17_done_an");

            yield return Scene("Q17_done_an", "Phần còn trắng", NarrativeTrigger.Manual, "", IsAn,
                    "An ghép sáu phần sổ cũ với sáu nhóm quan sát: khớp từng phần. Phần thứ bảy để trắng.", location: "")
                .FadeOut(600)
                .FadeIn(600)
                .Narrate("Đêm đó, chờ cả trại ngủ, tôi trải hết lên giường: sổ gửi thư, ảnh camera, nhật ký hộp, cuộn băng.")
                .Narrate("Tôi ghép sáu phần sổ cũ của nhà mình với sáu nhóm quan sát. Phần nào cũng khớp.")
                .Narrate("Phần thứ bảy để trắng.")
                .Narrate("Tôi viết: “Nhà mình ơi, người gửi thư là An. Cái hộp đang chờ phần của An ★.”")
                .Narrate("Tôi định vẽ mặt cười ở cuối. Bút dừng lại trên giấy một lúc, rồi tôi vẫn vẽ.")
                .Notebook("Q17")
                .Then("Q18_start_an");

            // ---- Q18 (An's one slip of the chapter: she counts the people at the table and stops at six)
            yield return Scene("Q18_start_an", "Gom đàn chứ đừng gom sợ", NarrativeTrigger.Manual, "", IsAn,
                    "Họp ở nhà chính: bảng “đủ / thiếu / chưa biết”, An xin thêm cột “chỉ chị nghe ra”. Tám Vá đoán hộp mượn " +
                    "chỗ trống của đàn. Muốn cắt đúng kênh phải có mẫu đàn: ngoài đợt, gom đàn về Nơi Trú.")
                .PlaceAt(Ca, "farm:station:main_house", CardinalDirection.South)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.West)
                .Narrate("Họp ở nhà chính. Anh Ca mang tới một tờ giấy kẻ ba cột: “đủ / thiếu / chưa biết”.")
                .Say(Ca, W, "Cột “chưa biết” dài nhất. Tôi kẻ lại hai lần rồi, nó vẫn dài nhất.", R)
                .Say(A, H, "Thêm cột “chỉ chị nghe ra” nữa nha anh.")
                .Say(Ca, T, "Giấy không đủ khổ, em.", R)
                .Say(A, L, "Thì in khổ A3 anh ơi.")
                .Narrate("Cả phòng cười. Anh Ca ghi thật: “A3”. Cả buổi không ai nhắc chữ “khách”.")
                .Narrate("Tôi rót trà, đếm chén. Một, hai, ba, bốn, năm, sáu.")
                .Narrate("Rồi tôi dừng. Tay tôi cầm ấm trà, không rót tiếp.")
                .Say(Ca, N, "…Em, chén của tôi nè.", R)
                .Say(A, H, "Dạ dạ, xin lỗi anh!")
                .Say(Tam, T, "Chú có một giả thuyết. Cháu ghi ngoài lề thôi, đừng tin vội.", R)
                .Say(Tam, N, "Phần cuối cuốn sổ nhà cháu còn trống. Cái hộp mượn chỗ trống trong đàn để giữ chỗ cho gói cuối.", R)
                .Say(Tam, N, "Có khi nó gây ra chỗ trống. Có khi nó chỉ đọc một thứ có sẵn. Chú không biết.", R)
                .Say(Tam, T, "Kênh của gói đó chỉ mở trong đợt lớn, lẫn giữa các kênh khác. Muốn cắt đúng thì phải có mẫu đàn.", R)
                .Narrate("Chú chỉ vào dòng trạng thái: “chờ xác nhận: phần thứ bảy được viết”.")
                .Say(Tam, N, "Nó không vội. Nó chờ cháu.", R)
                .Narrate("Không cắt thì hôm nay không ai mất gì, kể cả khi vài chục đợt lớn trôi qua.")
                .Narrate("Chỉ là tôi sẽ không bao giờ dám viết phần của mình vào sổ nhà mình. Và đàn sẽ mãi chừa ra một chỗ im.")
                .Say(Tam, N, "Ngoài đợt, cháu lùa đàn về Nơi Trú cho đủ. Đứng yên một chỗ là chú ghi mẫu.", R)
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó, ngoài lề, bút chì: “Hộp mượn chỗ trống. Hoặc chỉ đang đọc nó???”")
                .Narrate("Dòng dưới, không gửi ai: “Đếm tới sáu thì mình dừng. Không ai bảo dừng.”")
                .Hint("recall");

            yield return Scene("Q18_done_an", "Mẫu đàn", NarrativeTrigger.QuestCompleted, "Q18", IsAn,
                    "Đàn đứng yên trong Nơi Trú, Tám Vá ghi mẫu vào hộp và đưa An các bước ngắt tuyến (chỉ dùng ngoài đợt).",
                    priority: 10)
                .Narrate("Đàn về Nơi Trú, ủn ỉn rồi đứng yên. Tám Vá cắm dây vào hộp, đọc từng con số, chậm.")
                .Say(Tam, H, "Có mẫu rồi. Đợt lớn tới nó mở kênh nào, mình so là biết.", R)
                .Say(Tam, N, "Còn đây là các bước ngắt tuyến. Chỉ làm ngoài đợt nghe cháu.", R)
                .Say(A, H, "Dạ. Con cất kỹ.")
                .Narrate("Đàn ồn ào khắp Nơi Trú. Chỉ có một chỗ là im.")
                .Notebook("Q18")
                .Then("Q19_start_an");

            // ---- Q19 (loss scene: An's smile, the reflection cries — same frame)
            yield return Scene("Q19_start_an", "Bữa tối chưa có khách", NarrativeTrigger.Manual, "", IsAn,
                    "Bà Sáu mời An tối nay ăn cơm ở quán, dặn mang theo cái bát ở nhà cũ. Trước khi đi, An nhìn đàn từ nhà chính.")
                .Narrate("Chiều, Mận ghé.")
                .Say(Man, H, "Chị ơi, bà Sáu dặn tối nay chị qua quán ăn cơm. Bà nấu canh chua.", R)
                .Say(A, U, "Ơ, thật hả? Có dịp gì vậy em?")
                .Say(Man, N, "Không có dịp gì hết. Bà dặn chị mang theo cái bát ở nhà cũ. Bà muốn coi.", R)
                .Narrate("Tôi gói cái bát men lam vào khăn. Trước khi đi, tôi định đứng ở cửa sổ nhà chính nghe đàn một lúc.")
                .Narrate("Việc đó thành thói quen rồi.")
                .Hint("interact");

            string q19Step = Step("Q18_done_an", "Q19_done_an", IsAn);

            yield return Scene("Q19_window_an", "Cửa sổ", NarrativeTrigger.Signal, Sig(QuestSignal.HouseBoxInspected), q19Step,
                    "Qua cửa sổ nhà chính, đàn ồn ào khắp nơi, trừ một chỗ im.", priority: 10)
                .Narrate("Tôi mở cửa sổ nhà chính. Tiếng đàn ăn tràn vào phòng, ồn từ đầu này tới đầu kia.")
                .Narrate("Trừ một chỗ. Chỗ đó im.")
                .Narrate("Tôi đứng nghe cái chỗ im đó lâu hơn nghe cả đàn.")
                .ThenWhenDone("Q19", "Q19_done_an");

            yield return Scene("Q19_kitchen_an", "Bữa tối chưa có khách", NarrativeTrigger.Signal, Sig(QuestSignal.TalkBaSau), q19Step,
                    "Mặt vòng phản chiếu một bàn ăn bảy chỗ. Bà Sáu kể chuyện mẹ đo chiều cao cả nhà mỗi hè, và cái bóng " +
                    "cao hơn ông Năm một cái đầu. An mỉm cười cảm ơn; bà đặt khăn trước mặt chị. Trong mặt vòng, cái bóng khóc.",
                    location: "")
                .Music("")
                .Backdrop("bg_quan_ba_sau_bep")
                .Narrate("Bà Sáu dọn cơm trong bếp, như mọi bữa: cho sáu với một người. Tôi ngồi chỗ của một.")
                .Narrate("Tôi mở khăn, đặt cái bát mang từ nhà cũ lên bàn. Men lam, sứt miệng.")
                .Narrate("Rồi tôi đặt chiếc vòng của mẹ cạnh cái bát.")
                .Backdrop("bg_vong_ban_an_bay_cho")
                .Narrate("Mặt kim loại phản chiếu một bàn ăn. Bảy chỗ.")
                .Narrate("Tôi nghiêng đầu. Hình biến mất.")
                .Backdrop("bg_quan_ba_sau_bep")
                .Narrate("Bà Sáu múc canh, chan cho tôi trước.")
                .Say(Sau, N, "Mẹ con lúc nào cũng cho con bé ngồi gần nồi cơm.", R)
                .Say(Sau, H, "Cuối mỗi mùa hè, trước hôm về, mẹ con đo cho cả nhà. Thằng bé Khoa cầm bút chì kẻ.", R)
                .Say(Sau, H, "Ông bà cũng phải đứng vô. Cho đủ.", R)
                .Say(Sau, Sd, "Năm ông con mất thì nhà con không lên.", R)
                .Say(Sau, N, "Năm sau nữa, đêm mồng năm tháng mười, bà thấy đèn phòng đó sáng một lát.", R)
                .Say(Sau, N, "Bà tưởng ông Năm về lấy đồ.", R)
                .Pause(1600)
                .Say(Sau, W, "Mà cái bóng trong phòng cao hơn ông Năm một cái đầu.", R)
                .Say(A, Portraits.AnTooBright, "Dạ. Con cảm ơn bà.")
                .Narrate("Giọng tôi bình thường. Bình thường đến hoàn hảo.")
                .Narrate("Bà Sáu không đáp. Bà lặng lẽ đặt một chiếc khăn trước mặt tôi.")
                .Narrate("Tôi nhìn xuống mặt vòng.")
                .Backdrop("bg_vong_ban_an_bay_cho")
                .Reflection(Portraits.Sad)
                .Narrate("Tôi đang cười. Cái bóng trong đó đang khóc.")
                .Pause(2600)
                .Backdrop("bg_quan_ba_sau_bep")
                .Say(Sau, N, "Ăn thêm miếng nữa đi con.", R)
                .ThenWhenDone("Q19", "Q19_done_an");

            yield return Scene("Q19_done_an", "Không viết", NarrativeTrigger.Manual, "", IsAn,
                    "An đi bộ về trại. Tối đó chị không viết thư.", location: "")
                .FadeOut(600)
                .Music("")
                .FadeIn(600)
                .Narrate("Tôi đi bộ về trại. Đường tối. Tôi nghe tiếng dép mình, nghe cả tiếng mình thở.")
                .Narrate("Tối đó tôi không viết thư.")
                .Pause(2000)
                .Narrate("Sáng ra, con heo chân trắng nằm ngửa chắn ngay cửa, chờ ai gõ máng. Tôi gõ. Nó lật dậy.");

            // ---- Q20
            yield return Scene("Q20_start_an", "Rìu Sắt trả hàng", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q20,CYCLE_NUMBER 20," + IsAn,
                    "Mẫu đàn đã cắm vào hộp. Đợt lớn kế tiếp, hộp tự ghi các kênh; An chỉ cần giữ trại qua đợt đó, rồi mở hộp.",
                    priority: 18)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Say(Tam, N, "Mẫu đàn chú cắm vô hộp rồi.", R)
                .IfGoto("CYCLE_NUMBER 21", "ready")
                .Say(Tam, N, "Đợt lớn tới, kênh nào mở là nó tự ghi. Trong lúc đánh, cháu không phải làm gì với nó.", R)
                .Say(Tam, T, "Cháu chỉ việc giữ trại qua đêm đó. Thắng thua gì nó cũng ghi.", R)
                .Say(A, U, "Rồi sáng ra mình làm gì chú?")
                .Say(Tam, N, "Sáng ra mở hộp. So mẫu với các kênh. Kênh nào lạ thì cắt.", R)
                .Say(A, H, "Dạ. Vậy đêm đó con giữ trại, sáng con mở hộp.")
                .Hint("defend")
                .Goto("out")
                .Label("ready")
                .Say(Tam, N, "Đợt lớn vừa rồi nó ghi đủ cả. Cháu vô nhà chính mở hộp đi. So mẫu với các kênh.", R)
                .Flag(Q20ReadyFlag)
                .Hint("interact")
                .Label("out");

            yield return Scene("Q20_ready_an", "Giấy in dài tới cửa", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q20,CYCLE_NUMBER 21,SCENE_SEEN Q20_start_an,!STORY_FLAG " + Q20ReadyFlag + "," + IsAn,
                    "Đợt lớn đã qua, hộp ghi đủ các kênh. Tám Vá bảo An vào nhà chính mở hộp.", priority: 19)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Sáng. Đợt lớn đã qua. Rào còn đứng. Cả xóm lên vá rào, nói chuyện thời tiết.")
                .Say(Tam, H, "Đêm qua nó ghi đủ. Giấy in ra dài tới cửa.", R)
                .Say(Tam, N, "Vô nhà chính mở hộp đi cháu. Mẫu đàn chú để sẵn bên cạnh.", R)
                .Flag(Q20ReadyFlag)
                .Hint("interact");

            yield return Scene("Q20_box_an", "Cắt kênh", NarrativeTrigger.Signal, Sig(QuestSignal.HouseBoxInspected),
                    "STORY_QUEST_ACTIVE Q20,CYCLE_NUMBER 21,STORY_FLAG " + Q20ReadyFlag + "," + IsAn,
                    "An so mẫu đàn với các kênh và cắt kênh của gói cuối: “hủy — không tìm thấy người gửi”. Sáng sau, kiện " +
                    "“trả về người gửi” ở cổng: cái bát thứ hai, gõ cùng một tiếng. Đàn ăn ồn ào, không còn chỗ nào im.", priority: 20)
                .Music("")
                .Narrate("Tôi áp tai vào hộp trước khi nhìn giấy. Các kênh rè rè chồng lên nhau, như nhiều người nói cùng lúc.")
                .Backdrop("bg_hop_nhat_ky_kenh")
                .Narrate("Tôi đặt mẫu đàn lên giấy in. Khớp hết. Trừ một đường.")
                .Narrate("Đường đó mở đúng lúc đàn có một chỗ im, đóng đúng lúc đàn đứng lại. Nhãn: gói thứ bảy.")
                .Say(Tam, N, "Cái đó. Cắt thì cháu cắt. Máy không phải của tôi.", R)
                .Choice("Kênh của gói cuối", 0, SceneDefinition.Opt("Cắt kênh"))
                .Sfx("sfx_tach")
                .Narrate("Một tiếng tách. Rồi tiếng rè thưa đi một giọng.")
                .Narrate("Dòng trạng thái đổi: “hủy — không tìm thấy người gửi”.")
                .Sfx("sfx_go_ba_cai")
                .Say(Tam, N, "Nghe vẫn thành thật.", R)
                .Flag(Q20CutFlag)
                .Pause(1600)
                .FadeOut(700)
                .Backdrop("bg_cong_trai_kien_tra_ve", 0)
                .FadeIn(700)
                .Narrate("Sáng hôm sau, ở cổng trại có một kiện hàng. Nhãn dán: “trả về người gửi”.")
                .Narrate("Bên trong là một cái bát men lam sứt miệng. Giống hệt cái bát của tôi, đến cả vết sứt.")
                .Say(Ton, T, "Sổ kho tôi ghi: chưa xác định nguồn.", R)
                .Narrate("Tôi gõ nhẹ vào hai cái bát. Cùng một tiếng. Không lệch một nốt.")
                .Narrate("Tôi vẽ một mặt cười dưới đáy mỗi cái. Mặt cười trên cái bát mới cứ bị mực trôi đi.")
                .Narrate("Tôi vẽ ba lần. Vẫn trôi.")
                .Pause(1600)
                .Backdrop("")
                .Narrate("Ngoài sân, đàn heo ăn ồn ào, từ đầu này tới đầu kia. Không còn chỗ nào im.")
                .Say(A, H, "Mận ơi, đếm giùm chị.")
                .Say(Man, N, "…Đủ chị.", R)
                .Pause(1600)
                .Narrate("Con heo chân trắng chen vào giữa đàn, nằm ngửa, giả chết. Ồn nhất là nó.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Mẹ ơi, hai cái bát ★.” Ngoài lề, bút chì: “Là một cái?”")
                .Notebook("Q20")
                .Then("Q20_choice_an");

            yield return Scene("Q20_choice_an", "Giữ lại gì", NarrativeTrigger.Signal, Sig(QuestSignal.HouseBoxInspected),
                    "STORY_QUEST_ACTIVE Q20,STORY_FLAG " + Q20CutFlag + ",!STORY_FLAG " + EndingChosenFlag + "," + IsAn,
                    "Gói cuối đã hủy. Còn cái hộp, chiếc vòng và hai cái bát: An chọn giữ lại gì.", priority: 15)
                .NoSkip()
                .Repeatable()
                .Music("")
                .Say(Tam, N, "Tháo hẳn bộ gửi thì chú tháo được. Giữ bộ thu thì Cúc với ông Tồn mừng.", R)
                .Say(Tam, N, "Còn cái vòng là của nhà cháu.", R)
                .Narrate("Cái hộp đã thôi chờ. Giờ tới lượt tôi.")
                .Choice("Giữ lại gì?", 0,
                    EndingOpt("Giữ trại, tháo đường gửi", Ending81Flag, StoryEndingId.Ending_8_1_GiuTraiThaoDuongGui, "E81_an"),
                    EndingOpt("Giữ máy để làm hồ sơ", Ending82Flag, StoryEndingId.Ending_8_2_GiuMayLamHoSo, "E82_an"),
                    EndingOpt("Mang vòng về nhà", Ending83Flag, StoryEndingId.Ending_8_3_MangVongVeNha, "E83_an"),
                    SceneDefinition.Opt("Để mai hẵng tính.", "later"))
                .Label("chosen")
                .Narrate("Tôi đóng nắp hộp, vỗ vỗ lên nó hai cái, như vỗ lưng một con heo vừa ăn xong.")
                .End()
                .Label("later")
                .Say(A, H, "Không sao đâu mà! Để mai con tính.")
                .Say(Tam, N, "Nó không vội. Cháu cũng đừng vội. Chừng nào chọn được thì mở nắp, chú để nguyên đó.", R);

            // ---- 8.1
            yield return Scene("E81_an", "Giữ trại, tháo đường gửi", NarrativeTrigger.QuestCompleted, "Q20",
                    "STORY_FLAG " + Ending81Flag + "," + IsAn,
                    "Tám Vá tháo bộ gửi. An viết phần thứ bảy của sổ thành một lá thư dài, kẻ vạch mới bằng tay phải, về nhà cũ " +
                    "lật ngửa sáu cái bát. Sổ bàn giao có phiếu mới: “không tìm thấy người gửi”, ô người nhận để trống.", priority: 30)
                .Music("")
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Tám Vá tháo bộ phận gửi xác nhận. Chú tháo rất chậm, lập biên bản cho từng con ốc.")
                .Say(Tam, N, "Ốc. Bình thường.", R)
                .Say(A, L, "Chú lập biên bản cho con ốc luôn hả chú?")
                .Say(Tam, T, "Ốc. Hơi lỏng. Bình thường.", R)
                .Narrate("Chú đưa tôi con ốc cuối. Tôi đưa hai lòng bàn tay ngửa ra đón.")
                .Narrate("Chú chỉ tháo đúng một bộ phận. Mọi máy khác trong trại vẫn chạy như cũ.")
                .FadeOut(600)
                .Backdrop("bg_so_bia_vai_bay_phan", 0)
                .FadeIn(600)
                .Narrate("Tối đó tôi viết phần thứ bảy của quyển sổ bìa vải.")
                .Narrate("Tôi không viết lời từ biệt thay cho mẹ.")
                .Narrate("Tôi viết một lá thư dài gửi nhà mình: những lần tự lùa đàn về trú, tiền sửa rào, tên những người đã giúp.")
                .Narrate("Phần của tôi hóa ra là một cuốn sổ chợ. Bé Khoa chắc sẽ nhìn chị, hỏi chị viết thật hay viết giỡn.")
                .Narrate("Cuối thư, tôi định vẽ mặt cười. Rồi thôi. Lần này không cần.")
                .Backdrop("bg_hoc_tuong_bay_cot")
                .Narrate("Rồi tôi vào phòng bếp lò, mở cái hốc bằng mảnh da, kẻ một vạch mới trong cột của mình.")
                .Narrate("Bút chì. Tay phải. Từ trái sang phải. Tôi ghi ngày bên cạnh.")
                .Narrate("Lần đầu tiên kể từ hồi bé Khoa còn cầm bút chì, cột của tôi có một vạch mà tôi nhớ mình đã kẻ.")
                .Narrate("Ngoài lề, tôi dán một ngôi sao: “Hôm nay. Tay phải ★.”")
                .Music("bgm_nha_cu_lang")
                .Backdrop("bg_bep_me_gia_bat")
                .Narrate("Tôi về căn nhà cũ. Trên giá, sáu cái bát vẫn úp như hôm mẹ úp.")
                .Narrate("Tôi lật ngửa từng cái. Cái nào cũng kêu một tiếng nhỏ, như người ta đáp “dạ”.")
                .Pause(1600)
                .Narrate("Sáng hôm sau, chúng vẫn nằm ngửa. Lần đầu tiên sau ngần ấy năm.")
                .Music("")
                .Backdrop("")
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                .Narrate("Tôi quay lại trại.")
                .Say(Man, H, "Lứa mới có một con nhỏ xíu. Chị muốn giữ nó không?", R)
                .Say(A, U, "Giống gì vậy em?")
                .Say(Man, T, "Chưa biết. Mà chắc chắn là ăn khỏe.", R)
                .Say(A, L, "Vậy giống chị rồi.")
                .Backdrop("bg_so_tay")
                .Narrate("Trong sổ bàn giao có thêm một phiếu mới, đóng dấu “không tìm thấy người gửi”.")
                .Narrate("Ô người nhận vẫn để trống.")
                .Pause(2000)
                .Flag(ChapterDoneFlag);

            // ---- 8.2
            yield return Scene("E82_an", "Giữ máy để làm hồ sơ", NarrativeTrigger.QuestCompleted, "Q20",
                    "STORY_FLAG " + Ending82Flag + "," + IsAn,
                    "An cắt chức năng gửi, giữ bộ thu để nghiên cứu cùng Cúc và ông Tồn. Hai người nhận trại trước cũng 24 tuổi; " +
                    "một người vẫn gửi thư về. Bộ thu nhận một dòng: “Sáu với một vị trí: đủ.”", priority: 30)
                .Music("")
                .Narrate("Tôi cắt chức năng gửi. Bộ thu thì giữ lại, để cùng chị Cúc và ông Tồn làm hồ sơ.")
                .Say(Cuc, N, "Hồ sơ chia ba cột: đã xác nhận, ai kể lại, còn là suy đoán.", R)
                .Say(A, H, "Thêm cột “chỉ An nghe ra” nha chị.")
                .Say(Cuc, W, "Cột đó dài lắm em. Phải in khổ A3.", R)
                .Say(A, L, "Anh Ca có giấy A3 rồi chị. Ảnh mua từ bữa họp.")
                .Narrate("Những lần nghe ra người thân ở bệnh viện vào cột của tôi.")
                .Narrate("Bia mộ, phiếu thu, sơ đồ trạm vào cột đối chiếu được.")
                .Narrate("Ông Tồn mở hai hồ sơ bàn giao cũ. Hai người từng nhận trại trước tôi cũng tới đây năm hai mươi bốn tuổi.")
                .Narrate("Ngày sinh khác tôi. Một người còn gửi thư về sau khi đã rời trại.")
                .Say(Ton, N, "Người này đi rồi mà thư vẫn về. Đều lắm.", R)
                .Narrate("Vậy người cầm vòng không nhất thiết phải chết, hay biến mất.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, người cầm vòng không nhất thiết phải đi luôn ★★.”")
                .Narrate("Hai ngôi sao. Lần đầu tôi dán hai ngôi sao cho một dòng.")
                .Pause(1600)
                .FadeOut(600)
                .Backdrop("bg_so_tay", 0)
                .FadeIn(600)
                .Narrate("Đêm đó, bộ thu nhận được một gói dữ liệu. Chỉ một dòng.")
                .Narrate("“Sáu với một vị trí: đủ.”")
                .Pause(1800)
                .Narrate("Sinh nhật sau, tôi mở máy lại. Ô xác nhận tuổi vẫn đòi nhập số 24.")
                .Pause(2000)
                .Flag(ChapterDoneFlag);

            // ---- 8.3 (An's mirror is the inverse of Khoa's: she stops smiling, the reflection does not)
            yield return Scene("E83_an", "Mang vòng về nhà", NarrativeTrigger.QuestCompleted, "Q20",
                    "STORY_FLAG " + Ending83Flag + "," + IsAn,
                    "An mang chiếc vòng và cái bát thứ hai về nhà cũ. Suất mộ trống có một viên đá nhỏ. Trước gương An thôi cười, " +
                    "cái bóng thì không. Trên bậc thang thứ bảy có đất Bờ Rìa còn mới.", priority: 30)
                .Music("")
                .PlaceAt(Man, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Sau khi ngắt tuyến, tôi xếp lịch chăm trại cho đủ số người tôi trả được lương.")
                .Say(Man, N, "Cám trước, nước sau. Em nhớ mà chị.", R)
                .Say(A, H, "Chị biết. Chị chỉ muốn nghe em nói.")
                .FadeOut(600)
                .Music("bgm_nha_cu_lang")
                .Backdrop("bg_nha_cu_bua_com_ghe_trong", 0)
                .FadeIn(600)
                .Narrate("Tôi mang chiếc vòng và cái bát thứ hai về nhà cũ.")
                .Narrate("Tôi đặt hai cái bát sứt miệng giống hệt nhau lên bàn ăn. Một chỗ gần nồi cơm. Một chỗ ở cửa bếp.")
                .Music("")
                .Backdrop("bg_nghia_trang")
                .Narrate("Tôi ra nghĩa trang. Suất mộ thứ bảy vẫn trống.")
                .Narrate("Nhưng có ai đã đặt lên đó một viên đá nhỏ. Không có tên.")
                .Pause(1800)
                .Backdrop("bg_nha_cu_nha_tam")
                .Narrate("Về nhà, tôi đứng trước gương và cười, như mọi buổi sáng.")
                .Narrate("Rồi tôi thôi cười. Lần đầu tiên, trước gương.")
                .Reflection(Portraits.Happy)
                .Narrate("Cái bóng trong gương thì không thôi. Nó cười với tôi, đúng nụ cười của tôi.")
                .Narrate("Tôi đứng đó rất lâu, chờ xem nó có thôi không.")
                .Pause(2400)
                .Narrate("Nó không thôi.")
                .Narrate("Tôi mở sổ. Lần đầu tiên tôi không biết phải dán nhãn nào, cũng không biết gửi thư cho ai.")
                .Backdrop("bg_cau_thang_dat_do")
                .Narrate("Trên bậc thang thứ bảy có một ít đất màu nâu đỏ. Còn mới.")
                .Narrate("Tám Vá lên tận nơi coi. Chú bốc một nhúm, bóp, ngửi.")
                .Say(Tam, T, "Đất Bờ Rìa.", R)
                .Say(A, U, "Ơ… mang tới hồi nào vậy chú?")
                .Sfx("sfx_go_ba_cai")
                .Say(Tam, N, "Cái đó thì chú không biết.", R)
                .Pause(2400)
                .Flag(ChapterDoneFlag);

            // ---- 8.4 ⇒ 8.5
            yield return Scene("S1_after_an", "Sau kết thúc", NarrativeTrigger.DayStart, "", "STORY_QUEST_DONE Q20," + IsAn,
                    "Sáng đầu tiên sau truyện: trại vẫn nguyên, đàn vẫn đói đúng giờ. Giám Đốc đòi mở cuộc họp, An xin dự. " +
                    "Đàn không còn chỗ nào im; lần đầu An tự đếm, một lần.", priority: 30)
                .Music("bgm_trai_cu_sang")
                .PlaceAt(Ca, "farm:station:main_house", CardinalDirection.South)
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                .Narrate("Sáng. Trại vẫn là trại. Rào đứng chỗ cũ, máy chạy chỗ cũ, đàn vẫn đói đúng giờ.")
                .Narrate("Sáu người đã mất không sống lại. Tôi biết. Tôi không viết thư để họ về.")
                .Narrate("Tôi vẫn đôi khi nghe ra những tiếng người khác không nghe.")
                .Narrate("Tôi xử lý theo những cách tôi đã học: viết thư, uống thuốc, đi cho heo ăn.")
                .Narrate("Con ngỗng Giám Đốc đứng cạnh cái hộp trong nhà chính, kêu inh ỏi.")
                .Say(Ca, T, "Nó muốn mở cuộc họp.", R)
                .Say(A, U, "Ơ, thật hả? Ai dự vậy anh?")
                .Say(Man, N, "Nãy giờ mới có mình nó.", R)
                .Say(A, L, "Vậy chị dự. Cho nó đủ người.")
                .Narrate("Ngoài sân, đàn heo đứng ăn, ồn ào từ đầu này tới đầu kia. Không chỗ nào im. Hôm sau cũng vậy.")
                .Narrate("Lần đầu tiên ở trại, tôi tự đếm. Một lần, từ đầu tới cuối, không dừng ở đâu cả.")
                .Pause(1600)
                .Banner("Hết Mùa 1", 2800)
                .Flag(ChapterDoneFlag)
                .Then("S1_postcredit_an");

            yield return Scene("S1_postcredit_an", "Người tới sớm", NarrativeTrigger.Manual, "", IsAn,
                    "Một tuần sau, Mận tìm lại kẹp tóc của mẹ dưới máng heo. Tối đó, cái chai trên bậu cửa cắm một cành hoa sứ: " +
                    "sáu bông nở, một nụ. Ở chữ Rìa, Mận đón một người xuống xe: “Anh tới sớm hai phút.”")
                .FadeOut(800)
                .Music("")
                .FadeIn(800)
                .Narrate("Một tuần sau. Một buổi sáng bình thường ở trại.")
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                .Say(Man, H, "Chị! Kẹp tóc của mẹ em nè. Em làm rớt hồi lớp ba.", R)
                .Say(Man, U, "Nằm ngay dưới máng heo. Cái máng em cọ cả nghìn lần rồi.", R)
                .Narrate("Tôi đứng im.")
                .Say(Man, N, "Chị không mừng cho em hả?", R)
                .Say(A, H, "Mừng chứ!")
                .Narrate("Tôi cười. Tôi không nói rằng tôi vừa viết ngày hôm nay vào thư.")
                .FadeOut(700)
                .Backdrop("bg_bau_cua_hoa_su_bay_bong", 0)
                .FadeIn(700)
                .Narrate("Tối đó, đi ngang nhà chính, mùi hoa sứ tới trước. Tôi đứng lại rồi mới nhìn.")
                .Narrate("Trên bậu cửa sổ, cái chai thủy tinh rỗng từ ngày tôi tới đang cắm một cành hoa sứ trắng.")
                .Narrate("Bảy bông. Sáu bông nở, một nụ.")
                .Narrate("Tôi không hỏi ai cắm. Sẽ chẳng ai nhận, và cũng chẳng ai thấy lạ.")
                .Narrate("Lần này không có ai gọi tôi đi bưng nồi cơm. Tôi đứng lại, nhìn cho tới khi chắc mình sẽ không quên.")
                .Pause(1800)
                .Backdrop("bg_bien_bo_ria")
                .Sfx("sfx_coi_xe_khach")
                .Narrate("Cùng lúc đó, ở chữ Rìa dưới tấm biển, một chiếc xe khách dừng lại.")
                .Narrate("Mận đứng đợi sẵn với cây dù đen. Không ai báo trước cho em chuyến này.")
                .Narrate("Mận nhìn đồng hồ, rồi nói với người vừa xuống xe:")
                .Say(Man, N, "Anh tới sớm hai phút.", R)
                .Pause(1400)
                .FadeOut(900)
                .Backdrop("bg_ngay_mat", 0)
                .FadeIn(600)
                .Banner("Mùa 2 — Người tới sớm", 3600);
        }
    }
}
