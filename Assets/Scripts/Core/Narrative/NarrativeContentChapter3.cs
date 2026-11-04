using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Chương 3 — "Chỗ trống" (Q11–Q15), both routes. Canon: story bible 2.2–2.7 (luật hài/bí ẩn, mặt Khoa, hai góc nhìn),
    /// chapter table row 3, Phần 5 / 5B rows Q11–Q15, 9.1 (cảnh hỏi về tấm ảnh, Q14). Khoa counts, asks flat and puts the
    /// papers on the table; An lets others count and talk, never opens her notebook in public, writes letters at night
    /// (★ ai cũng kiểm được, ☾ chỉ An nghe ra; the 02:00 line has no addressee and no smiley).
    /// Teach-by-doing: each start beat asks for one action and ends on a tutorial hint; the matching QuestSignal (or the
    /// quest's NPC talk) plays the next beat. Rewards are paid by StoryQuestBook; these scenes only set flags, open notebook
    /// pages and show hints. Village rules kept: nobody names the thing in the sky or the fog, no villager says the number
    /// after six, a silent beat (no music) follows every mystery and life comes back through a pig, nothing removes progress.
    /// Order safety: a quest's last step can finish it before its mid beat plays (TalkTo / SignalQuest complete the quest
    /// first), so every done scene replays a missed mid beat in two or three lines (IfGoto SCENE_SEEN). A quest can also be
    /// finished before its start beat (records already met, canon "công nhận việc đã làm"): done scenes wait on
    /// SCENE_SEEN of their start, and the start beat jumps straight to the done scene.
    /// Flow: C3_card (DayStart after chapter2_done; Q11 start) → Q11_done ⇒ Q12_start → Q12_le (talk Lệ) / Q12_herd →
    /// Q12_done; next DayStart Q13_start → Q13_herd / Q13_dung (talk Dung) → Q13_done; next DayStart Q14_start → talk Bà Sáu
    /// / Tám Vá / Lệ → Q14_done ⇒ Q14_hop2 ⇒ Q14_toi; next DayStart Q15_start → Q15_built → (boss wave) Q15_done ⇒ C3_end.
    /// </summary>
    public static class NarrativeContentChapter3
    {
        const string K = CastIds.Khoa, A = CastIds.An, Man = CastIds.Man, Tam = CastIds.TamVa, Le = CastIds.Le,
            Sau = CastIds.BaSau, Dung = CastIds.Dung, Cuc = CastIds.Cuc;
        const string N = Portraits.Neutral, H = Portraits.Happy, L = Portraits.Laugh, U = Portraits.Surprised,
            T = Portraits.Thinking, Sd = Portraits.Sad, W = Portraits.Worried, Ag = Portraits.Angry;
        const string IsKhoa = "PROTAGONIST Khoa", IsAn = "PROTAGONIST An";
        const StageSide R = StageSide.Right;

        /// <summary>Set by Chương 2's chapter-end image; this chapter's card waits on it (literal, no class reference).</summary>
        const string PrevChapterFlag = "chapter2_done";
        /// <summary>Q13 used Mận's photo of ông Năm's old herd (fewer than six old pigs on the farm, canon Q13 note).</summary>
        const string OldPhotoFlag = "c3_q13_old_photo";

        /// <summary>Flag set when the chapter-end image has played (Chapter 4 can wait on it).</summary>
        public const string ChapterDoneFlag = "chapter3_done";

        static SceneDefinition Scene(string id, string title, NarrativeTrigger trigger, string arg, string condition, string recap,
            int priority = 0, string location = "Farm")
            => new SceneDefinition
            {
                Id = id, Title = title, Trigger = trigger, TriggerArg = arg, Condition = condition, Recap = recap,
                Priority = priority, Location = location, Once = true, Skippable = true,
            };

        static SceneDefinition Hint(this SceneDefinition s, string verb) => s.Do(NarrativeEffectKind.Custom, NarrativeCustomIds.TutorialPrefix + verb);
        static SceneDefinition Notebook(this SceneDefinition s, string entry) => s.Do(NarrativeEffectKind.Custom, NarrativeCustomIds.NotebookPrefix + entry);
        static string Sig(QuestSignal s) => s.ToString();

        /// <summary>Narration that only plays when <paramref name="sceneId"/> was never seen (a mid beat the quest outran).</summary>
        static SceneDefinition IfMissed(this SceneDefinition s, string sceneId, params string[] lines)
        {
            string tag = "missed" + s.Commands.Count;
            s.IfGoto("SCENE_SEEN " + sceneId, tag);
            foreach (var line in lines) s.Narrate(line);
            return s.Label(tag);
        }

        /// <summary>A line about the white-footed pig, or <paramref name="otherwise"/> (may be null) when it is no longer on the farm.</summary>
        static SceneDefinition WhiteFoot(this SceneDefinition s, string line, string otherwise)
        {
            string tag = "wf" + s.Commands.Count;
            s.IfGoto("!HERD_HAS_WHITE_FOOT_PIG", tag + "_no");
            s.Narrate(line).Goto(tag + "_end").Label(tag + "_no");
            if (otherwise != null) s.Narrate(otherwise);
            return s.Label(tag + "_end");
        }

        public static IEnumerable<SceneDefinition> Scenes()
        {
            foreach (var s in Khoa()) yield return s;
            foreach (var s in An()) yield return s;
        }

        // =====================================================================================================
        // Khoa — lấy cương: đếm ba lần, hỏi thẳng, đặt giấy lên bàn, giọng biên bản, mặt không đổi.
        // =====================================================================================================
        static IEnumerable<SceneDefinition> Khoa()
        {
            // ---- Chapter card + Q11 Đàn này có người
            yield return Scene("C3_card_khoa", "Chương 3 — Chỗ trống", NarrativeTrigger.DayStart, "",
                    "STORY_FLAG " + PrevChapterFlag + ",STORY_QUEST_DONE Q10," + IsKhoa,
                    "Mận gọi sáu con heo già là “hội phụ huynh”. Muốn đàn thành bầy: heo thân nhau, vui, không bệnh, " +
                    "chuồng vừa chỗ, hội phụ huynh đủ sáu bà thân với đàn, giữ ba ngày liền.", priority: 20)
                .Music("")
                .Banner("Chương 3 — Chỗ trống", 2200)
                .Narrate("“Đàn heo đủ số. Tôi đếm ba lần. Mận bảo tôi đừng đếm nữa.”")
                .IfGoto("STORY_QUEST_DONE Q11", "formed")
                .PlaceAt(Man, "farm:station:herd_shelter", CardinalDirection.South)
                .FadeIn(600)
                .Say(Man, H, "Anh coi mấy bà heo già kìa. Em gọi là hội phụ huynh.", R)
                .Say(Man, N, "Heo non ăn thì tụi nó đứng ngoài nhìn, y như phụ huynh đứng ngoài cổng trường.", R)
                .Narrate("Tôi nhìn. Đúng là dáng đứng đợi con tan học. Có bà còn khịt mũi như chê cơm căng tin.")
                .Say(K, N, "Đàn thành bầy thì sao.")
                .Say(Man, N, "Thì tụi nó tự giữ nhau, anh đỡ cực. Mà phải đủ điều kiện.", R)
                .Say(Man, N, "Heo thân nhau, vui, không con nào bệnh. Chuồng đừng chật quá, cũng đừng trống quá.", R)
                .Say(Man, T, "Hội phụ huynh phải đủ sáu bà, bà nào cũng thân với đàn. Giữ vậy ba ngày liền.", R)
                .Narrate("Tôi ghi đủ. Danh sách dài hơn giấy khám sức khỏe của tôi, và khó đạt hơn.")
                .Say(Man, H, "Anh chạm vô từng con mà coi thẻ. Con nào đứng lẻ thì chăm con đó trước.", R)
                .Hint("observe")
                .Goto("end")
                .Label("formed")
                .Narrate("Không cần đợi. Sáng nay đàn đã đứng thành bầy.")
                .Then("Q11_done_khoa")
                .Label("end");

            // Mystery beat: the herd leaves a space. Short lines, no music; Mận's line is the silent turn, then a pig.
            yield return Scene("Q11_done_khoa", "Chừa một chỗ", NarrativeTrigger.QuestCompleted, "Q11",
                    "SCENE_SEEN C3_card_khoa," + IsKhoa,
                    "Sáng đàn thành bầy, sáu con heo già cùng xoay người chừa ra một chỗ. Khoa đếm ba lần: đủ số. " +
                    "Cột phải: “Đủ số.” Cột trái: “Chừa một chỗ.”", priority: 10)
                .Music("")
                .Narrate("Sáng. Sáu con heo già cùng xoay người một lúc, như nghe chung một tiếng gọi.")
                .Narrate("Giữa đội hình hở ra một khoảng trống.")
                .Narrate("Những con khác đi vòng qua chỗ đó.")
                .WhiteFoot("Kể cả con chân trắng. Nó chưa từng đi vòng qua thứ gì, kể cả cái xô.", null)
                .Narrate("Tôi đếm lại ba lần. Số heo vẫn khớp với sổ.")
                .Say(Man, N, "Đủ mà anh. Anh đừng đếm nữa.", R)
                .Narrate("Cột phải: “Đủ số.” Cột trái: “Chừa một chỗ.”")
                .Pause(1800)
                .Narrate("Một con heo non lẻn tới máng của hội phụ huynh. Sáu bà cùng quay lại nhìn.")
                .Narrate("Nó lùi về chỗ cũ, rất lễ phép. Ở trại này, heo non hiểu luật nhanh hơn tôi.")
                .Notebook("Q11")
                .Then("Q12_start_khoa");

            // ---- Q12 Mua tin về chuyện mình vừa thấy (steps: talk Lệ, herd into Nơi Trú)
            const string Q12On = "STORY_QUEST_ACTIVE Q12,SCENE_SEEN Q12_start_khoa," + IsKhoa;

            yield return Scene("Q12_start_khoa", "Mua tin về chuyện mình vừa thấy", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_DONE Q11,SCENE_SEEN Q11_done_khoa," + IsKhoa,
                    "Khoa muốn mua tin về chỗ trống trong đàn. Mận chỉ: chị Lệ bán tin, nhà ở dưới xóm. " +
                    "Mua xong thì lùa đàn vô Nơi Trú để so.", priority: 18)
                .IfGoto("STORY_QUEST_DONE Q12", "done")
                .Narrate("Tôi có một chuyện vừa thấy, và không ai chịu xác nhận. Ở xóm này, thứ đó gọi là tin.")
                .Say(Man, N, "Tin thì chị Lệ bán. Chị bán cả tin lẫn bảo hiểm chồn. Nhà chị ở dưới xóm.", R)
                .Narrate("Tôi sắp đi mua tin về đàn heo của chính mình. Người ngoài nghe chắc thấy lạ. Người trong xóm thì không.")
                .Say(Man, N, "Mua xong anh lùa đàn vô Nơi Trú mà coi lại. Tin phải so với heo thật.", R)
                .Hint("interact")
                .Goto("end")
                .Label("done")
                .Then("Q12_done_khoa")
                .Label("end");

            yield return Scene("Q12_le_khoa", "Sạp không bán loại “ai”", NarrativeTrigger.Talk, Le, Q12On,
                    "Lệ bán “lúc nào”: lúc đàn yên, chỗ trống càng rõ. Tặng kèm: đêm 05/10 hằng năm, đèn phòng bếp lò sáng " +
                    "từ 02:00 tới 02:02. Sạp không bán loại “ai”.", priority: 20, location: "")
                .Say(Le, H, "Lại mua tin hả cậu? Lần đầu chị tặng rồi. Lần này tính tiền.", R)
                .Say(K, N, "Tôi mua tin về đàn của tôi. Vì sao nó chừa một chỗ.")
                .Say(Le, T, "“Lúc nào” thì chị bán được. “Tại sao” thì chị chưa định giá nổi.", R)
                .Say(K, N, "Vậy bán “lúc nào”.")
                .Say(Le, N, "Lúc đàn yên. Đàn càng yên, chỗ trống càng rõ. Hết.", R)
                .Narrate("Tôi trả đúng giá. Lệ đếm tiền hai lần, rồi đẩy thêm một mẩu giấy qua quầy.")
                .Say(Le, H, "Món rẻ nhất sạp, tặng kèm. Cậu không hỏi, nhưng chị cứ đưa.", R)
                .Narrate("Mẩu giấy ghi: đêm 05/10 hằng năm, đèn phòng bếp lò sáng đúng từ 02:00 tới 02:02.")
                .Say(Le, N, "Cả xóm biết. Không ai hỏi.", R)
                .Say(K, N, "Ai bật đèn.")
                .Say(Le, T, "Câu đó thuộc loại “ai”. Sạp chị không bán loại “ai”.", R)
                .Narrate("Tôi ghi ngay trên quầy. Lệ nhìn cây bút của tôi như nhìn một khách không trả tiền.")
                .Say(Le, H, "Giờ về lùa đàn vô Nơi Trú mà coi. Tin chị bán, heo cậu nuôi.", R)
                .Hint("whistle");

            yield return Scene("Q12_herd_khoa", "Đàn yên", NarrativeTrigger.Signal, Sig(QuestSignal.HerdRecalled), Q12On,
                    "Trong Nơi Trú chật, đàn vẫn chừa ra một chỗ. Còn phải mua tin của Lệ để so.")
                .Backdrop("bg_noi_tru_cho_trong")
                .Narrate("Đàn vào Nơi Trú. Trong này chật hơn ngoài sân. Con nào cũng ép sát con bên cạnh.")
                .Narrate("Vậy mà vẫn chừa ra một chỗ. Không con nào lấn vào, kể cả lúc bị con khác đẩy.")
                .Backdrop("")
                .Narrate("Còn thiếu phần tin. Phải xuống xóm mua của Lệ, rồi đem so với cái tôi vừa thấy.")
                .Hint("interact");

            yield return Scene("Q12_done_khoa", "Đã giữ chỗ", NarrativeTrigger.QuestCompleted, "Q12",
                    "SCENE_SEEN Q12_start_khoa," + IsKhoa,
                    "Tin đúng: đàn yên thì chỗ trống rõ. Đêm đó, sổ năm mười bảy tuổi có dòng “02:00. Đã giữ chỗ.” " +
                    "Cột phải: đèn sáng 2 phút, đêm 05/10, từ 2019.", priority: 10)
                .IfMissed("Q12_le_khoa",
                    "Lệ bán tôi chữ “lúc nào”: lúc đàn yên. Còn “tại sao” thì chị chưa định giá nổi.",
                    "Chị tặng kèm món rẻ nhất sạp: đêm 05/10 hằng năm, đèn phòng bếp lò sáng từ 02:00 tới 02:02.",
                    "Tôi hỏi ai bật đèn. Chị bảo sạp chị không bán loại “ai”.")
                .IfMissed("Q12_herd_khoa",
                    "Trong Nơi Trú, đàn ép sát vào nhau. Vẫn chừa ra một chỗ.")
                .Narrate("Tin của Lệ đúng. Lần đầu ở xóm này tôi trả tiền cho một câu đúng.")
                .FadeOut(600)
                .Music("")
                .Backdrop("bg_so_17_tuoi_0200")
                .FadeIn(600)
                .Narrate("Tối đó tôi lật về cuốn sổ năm mười bảy tuổi.")
                .Narrate("Có một trang, một dòng chữ của tôi mà tôi không nhớ đã viết: “02:00. Đã giữ chỗ.”")
                .Narrate("Cột phải: “Đèn sáng 2 phút, đêm 05/10, từ 2019.”")
                .Narrate("Lề: “Đêm đầu tiên là đêm tôi canh cái chai.”")
                .Pause(1800)
                .Backdrop("")
                .Narrate("Ngoài Nơi Trú, một con heo ngáy to tới mức con bên cạnh phải trở mình. Tôi nghe một lúc rồi ngủ.")
                .Notebook("Q12");

            // ---- Q13 Chỗ không con nào đứng (steps: gather ≥ 90 % into Nơi Trú, talk Dung)
            const string Q13On = "STORY_QUEST_ACTIVE Q13,SCENE_SEEN Q13_start_khoa," + IsKhoa;

            yield return Scene("Q13_start_khoa", "Chỗ không con nào đứng", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_DONE Q12,SCENE_SEEN Q12_done_khoa," + IsKhoa,
                    "Khoa vẽ đội hình đàn lúc đàn yên: gom cả đàn vô Nơi Trú, vẽ từ hai góc, đếm lại, rồi nhờ bác sĩ Dung khám.",
                    priority: 18)
                .IfGoto("STORY_QUEST_DONE Q13", "done")
                .PlaceAt(Man, "farm:station:herd_shelter", CardinalDirection.South)
                .Narrate("Sáng. Lệ nói đàn càng yên, chỗ trống càng rõ. Tôi quyết định vẽ nó ra giấy.")
                .Say(Man, U, "Anh vẽ đàn hả? Vậy đợi tụi nó ăn no, đứng yên rồi hẵng vẽ.", R)
                .Say(Man, N, "Lùa hết vô Nơi Trú. Vẽ một lần từ cửa, một lần từ vách sau. Hai góc mới chắc.", R)
                .Say(K, N, "Rồi đếm lại.")
                .Say(Man, T, "Anh đếm thì em cản không nổi. Xong nhờ bác sĩ Dung khám mấy bà già luôn.", R)
                .CameraTo("farm:station:herd_shelter", 600)
                .Hint("whistle")
                .Goto("end")
                .Label("done")
                .Then("Q13_done_khoa")
                .Label("end");

            yield return Scene("Q13_herd_khoa", "Hai góc", NarrativeTrigger.Signal, Sig(QuestSignal.HerdGathered90), Q13On,
                    "Khoa vẽ đội hình từ hai góc: chỗ trống nằm cùng một chỗ. Số heo khớp sổ. Còn phải nhờ bác sĩ Dung khám.")
                .IfGoto("HERD_ELDER_COUNT 6", "own")
                .Say(Man, N, "Hội phụ huynh nhà mình chưa đủ sáu bà. Anh lấy tạm ảnh này, em chụp đàn cũ của ông Năm.", R)
                .Narrate("Ảnh hơi mờ. Sáu con heo già đứng quây lại. Giữa chúng có một chỗ trống.")
                .Flag(OldPhotoFlag)
                .Label("own")
                .Backdrop("bg_noi_tru_cho_trong")
                .Narrate("Đàn đứng yên trong Nơi Trú. Tôi vẽ một lần từ cửa, một lần từ vách sau.")
                .Narrate("Hai bản vẽ, hai góc. Chỗ trống nằm cùng một chỗ.")
                .Narrate("Tôi đếm lại. Số heo khớp với sổ. Lần này tôi chỉ đếm một lần. Mận vỗ tay.")
                .Backdrop("")
                .Narrate("Còn bác sĩ Dung. Bà khám heo, không khám chỗ đứng, nhưng tôi vẫn phải hỏi.")
                .Hint("interact");

            yield return Scene("Q13_dung_khoa", "Không có bệnh", NarrativeTrigger.Talk, Dung, Q13On,
                    "Khoa nhờ bác sĩ Dung khám nhóm heo già. Triệu chứng “đứng chừa một chỗ” là đội hình. Kết luận: không có bệnh.",
                    priority: 20, location: "")
                .Say(K, N, "Bác sĩ khám giùm mấy con heo già.")
                .Say(Dung, T, "Triệu chứng?", R)
                .Say(K, N, "Đứng chừa một chỗ.")
                .Say(Dung, N, "Đó là đội hình. Không phải triệu chứng.", R)
                .Narrate("Bà vẫn khám. Từng con một: mắt, mũi, móng, tiếng thở. Lâu hơn tôi nghĩ.")
                .Say(Dung, N, "Không có bệnh.", R)
                .Narrate("Bà rửa tay rất kỹ. Rồi rửa lại lần nữa.")
                .IfGoto("SCENE_SEEN Q13_herd_khoa", "end")
                .Narrate("Còn bản vẽ đội hình. Phải gom cả đàn vô Nơi Trú lúc tụi nó yên.")
                .Hint("whistle")
                .Label("end");

            // Mystery beat: the formation matches the family photo. No music, no joke, nothing written.
            yield return Scene("Q13_done_khoa", "Cho vui", NarrativeTrigger.QuestCompleted, "Q13",
                    "SCENE_SEEN Q13_start_khoa," + IsKhoa,
                    "Khoa chồng tấm ảnh gia đình lên bản vẽ đội hình: sáu con heo già trùng chỗ sáu người thân, khoảng trống " +
                    "trùng chỗ thằng bé cầm cành hoa sứ. Mận: “Em tưởng ai cũng thấy.”", priority: 10)
                .Music("")
                .IfGoto("SCENE_SEEN Q13_herd_khoa", "drawn")
                .IfGoto("HERD_ELDER_COUNT 6", "drawn_own")
                .Narrate("Hội phụ huynh chưa đủ sáu bà. Mận đưa tôi tấm ảnh cô chụp đàn cũ của ông Năm.")
                .Flag(OldPhotoFlag)
                .Label("drawn_own")
                .Narrate("Tôi vẽ đội hình đàn từ hai góc. Chỗ trống nằm cùng một chỗ. Số heo khớp sổ.")
                .Label("drawn")
                .IfMissed("Q13_dung_khoa", "Bác sĩ Dung khám từng con, chỉ nói: “Không có bệnh.”")
                .FadeOut(600)
                .FadeIn(600)
                .Narrate("Tối đó tôi đặt tấm ảnh gia đình chồng lên bản vẽ đội hình. Chỉ để xem có gì vui không.")
                .Backdrop("bg_anh_doi_hinh_chong")
                .IfGoto("STORY_FLAG " + OldPhotoFlag, "old")
                .Narrate("Vị trí sáu con heo già trùng khớp với chỗ sáu người thân đứng trong ảnh.")
                .Goto("match")
                .Label("old")
                .Narrate("Sáu con heo già trong ảnh của Mận trùng khớp với chỗ sáu người thân đứng trong ảnh nhà tôi.")
                .Label("match")
                .Narrate("Khoảng trống trùng đúng chỗ thằng bé năm, sáu tuổi cầm cành hoa sứ.")
                .Narrate("Tức là tôi.")
                .Pause(1500)
                .Narrate("Mận đứng sau lưng tôi từ lúc nào.")
                .Say(Man, N, "Em tưởng ai cũng thấy.", R)
                .Narrate("Tôi không ghi gì vào sổ. Tôi không biết cái này thuộc cột nào.")
                .Pause(2000)
                .Backdrop("")
                .Narrate("Ngoài chuồng, một bà trong hội phụ huynh hắt xì. Cả hội cùng quay sang nhìn, như trách.");

            // ---- Q14 Buổi họp ai cũng mang ghế (steps: talk Bà Sáu, Tám Vá, Lệ; outside waves)
            const string Q14On = "STORY_QUEST_ACTIVE Q14,SCENE_SEEN Q14_start_khoa," + IsKhoa;

            yield return Scene("Q14_start_khoa", "Buổi họp ai cũng mang ghế", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_DONE Q13,SCENE_SEEN Q13_done_khoa," + IsKhoa,
                    "Ba tờ giấy, ba người cần xem: phiếu thu hoạch cho bà Sáu, bản ghi hai phút cho chú Tám, danh sách heo già " +
                    "cho Lệ. Khoa đi mời từng người lên họp ở nhà chính.", priority: 18)
                .IfGoto("STORY_QUEST_DONE Q14", "done")
                .PlaceAt(Man, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Sáng. Tôi bày ba tờ giấy lên bàn, theo thứ tự thời gian.")
                .Narrate("Phiếu thu hoạch ở vành ngoài, chữ ký bảy nét. Bản ghi “đã nhận” sớm hơn “đã gửi” hai phút.")
                .Narrate("Và danh sách heo già “đã quen đường về”, tôi chép lại từ trí nhớ tối hôm đọc trộm ở quầy Lệ.")
                .Narrate("Mỗi tờ có một người cần xem. Chiều nay họp ở nhà chính.")
                .Say(Man, U, "Anh mời họp hả? Ở xóm này đi họp là ai cũng tự mang ghế đó.", R)
                .Say(K, N, "Nhà chính đủ ghế.")
                .Say(Man, N, "Em biết. Em nói vậy thôi.", R)
                .Narrate("Bà Sáu, chú Tám, Lệ. Tôi đi mời tận nơi, từng người một.")
                .Hint("interact")
                .Goto("end")
                .Label("done")
                .Then("Q14_done_khoa")
                .Label("end");

            yield return Scene("Q14_sau_khoa", "Mời bà Sáu", NarrativeTrigger.Talk, Sau, Q14On,
                    "Khoa mời bà Sáu lên họp vì một tờ phiếu thu hoạch. Bà giao xách thùng nước, nhưng lần này bà đứng đợi.",
                    priority: 20, location: "")
                .Say(K, N, "Chiều nay họp ở nhà chính trên trại. Có một tờ giấy bà cần xem.")
                .Say(Sau, W, "Giấy gì cậu?", R)
                .Say(K, N, "Phiếu thu hoạch.")
                .Narrate("Bà Sáu đưa tôi cái thùng nước. Tôi xách ra sau quán.")
                .Narrate("Lúc quay lại, bà vẫn đứng đó. Lần đầu tiên.")
                .Say(Sau, N, "Chiều bà lên. Bà mang ghế.", R);

            yield return Scene("Q14_tam_khoa", "Mời chú Tám", NarrativeTrigger.Talk, Tam, Q14On,
                    "Khoa mời chú Tám lên họp xem lại bản ghi của cái hộp. Chú nhận lời, tự mang ghế.",
                    priority: 20, location: "")
                .Say(K, N, "Chiều nay họp ở nhà chính. Có bản ghi của cái hộp chú cần xem lại.")
                .Narrate("Chú Tám gõ ba cái vào cột rào gần nhất. Nghe. Rồi gật.")
                .Say(Tam, T, "Họp thì tôi tới. Tôi mang ghế. Ghế nhà người ta kêu, tôi ngồi không yên.", R);

            yield return Scene("Q14_le_khoa", "Mời Lệ", NarrativeTrigger.Talk, Le, Q14On,
                    "Khoa mời Lệ lên họp xem một danh sách. Lệ tới làm khách, tự mang ghế.",
                    priority: 20, location: "")
                .Say(K, N, "Chiều nay họp ở nhà chính. Có một danh sách chị cần xem.")
                .Say(Le, T, "Họp mà không bán gì thì chị tới làm khách.", R)
                .Narrate("Lệ không hỏi danh sách nào. Người buôn không hỏi câu mình đoán được giá.")
                .Say(Le, H, "Chị mang ghế. Ghế đi họp phải là ghế quen.", R);

            yield return Scene("Q14_done_khoa", "Không. Mai mưa.", NarrativeTrigger.QuestCompleted, "Q14",
                    "SCENE_SEEN Q14_start_khoa," + IsKhoa,
                    "Ai tới họp cũng tự mang ghế. Cả phòng sắp nói “Mai trời nắng” thì Mận nói: “Không. Mai mưa.” Bà Sáu " +
                    "thấy chữ ký bảy nét, kể đã đón đủ “sáu với một” người nhà Khoa mỗi mùa hè.", priority: 10)
                .PlaceAt(Sau, "farm:station:main_house", CardinalDirection.South)
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                .Backdrop("bg_trai_nha_chinh_ban_hop")
                .Narrate("Chiều. Bàn họp ở nhà chính đủ ghế.")
                .Narrate("Vậy mà ai tới cũng tự vác theo ghế riêng. Bà Sáu, chú Tám, Lệ. Cả Mận.")
                .Narrate("Tôi là người duy nhất ngồi ghế của trại. Tôi bắt đầu nghĩ có thể tôi sai.")
                .Say(K, N, "Nhà tôi lên đây làm gì.")
                .Narrate("Cả phòng hít vào một lượt, sắp đồng thanh câu quen: “Mai trời…”")
                .Music("")
                .Say(Man, N, "Không. Mai mưa.", R)
                .Narrate("Mọi người quay lại nhìn Mận. Mận nhìn xuống bàn, không rút lời.")
                .Narrate("Tôi đặt từng tờ giấy trước mặt từng người.")
                .Narrate("Bà Sáu nhìn chữ ký bảy nét trên tờ phiếu thu hoạch. Nhìn lâu.")
                .Say(Sau, Sd, "Chữ ký này bà thấy rồi. Trên giấy hẹn ông Năm nhận.", R)
                .Say(Sau, N, "Bà hứa với ông Năm không làm người mới sợ mà bỏ đi. Nên bà im.", R)
                .Say(Sau, N, "Mà chữ này trùng phiếu mộ nhà cậu. Nhà cậu có trong hồ sơ lâu rồi. Bà im cũng không che được ai.", R)
                .Say(Sau, N, "Hè nào bà cũng đón đủ sáu với một người nhà cậu.", R)
                .Say(Sau, W, "Có một lần, bà với ông Năm thấy một chuyến giao hàng hỏng.", R)
                .Say(Sau, Sd, "Còn chuyện ở thành phố thì bà không biết.", R)
                // 9.1 — the photo. Loss: no joke, no report voice.
                .Narrate("Tôi đặt tấm ảnh có đủ sáu với một người trước mặt bà.")
                .Say(K, N, "Bà nhớ mẹ tôi.")
                .Narrate("Bà lau một vệt nước trên bàn, đặt khăn xuống cạnh tấm ảnh. Không chạm vào.")
                .Say(Sau, N, "Nhớ.", R)
                .Say(K, N, "Mẹ tôi lên đây làm gì.")
                .Say(Sau, N, "Chăm đàn. Ghi lại những gì bả thấy.", R)
                .Say(K, N, "Còn cha tôi.")
                .Say(Sau, N, "Sửa máy với ông Tám.", R)
                .Say(K, N, "Nhà tôi chết gần hết. Vậy mà ở đây ai nhìn cái vòng cũng như đã biết từ trước.")
                .Say(Sau, Sd, "Bà biết họ từng ở đây. Bà biết ông Năm đợi người mang vòng tới.", R)
                .Say(Sau, Sd, "Còn chuyện xảy ra ở nhà cậu thì bà không biết.", R)
                .Say(K, N, "Sao bà không nói từ đầu.")
                .Say(Sau, N, "Bà cứ nghĩ để cậu ăn một bữa, ngủ một đêm, sáng mai nói sẽ dễ hơn.", R)
                .Say(K, N, "Bà đã nghĩ như vậy bao nhiêu đêm rồi.")
                .Narrate("Bà nhìn tôi.")
                .Say(Sau, Sd, "Nhiều lắm.", R)
                .Narrate("Ngoài sân, một con heo húc lăn cái xô. Hai chúng tôi vẫn ngồi yên.")
                .Narrate("Tôi không ghi gì vào sổ.")
                .Then("Q14_hop2_khoa");

            yield return Scene("Q14_hop2_khoa", "Ba tờ giấy", NarrativeTrigger.Manual, "", IsKhoa,
                    "Chú Tám: cái hộp chú sửa vẫn đang gửi; cha Khoa cài phần “chép sổ”. Lệ: bên mua heo già và bên ký phiếu " +
                    "là một, chỉ đòi heo “giữ nguyên đàn”, chưa từng tới lấy hàng.")
                .Backdrop("bg_trai_nha_chinh_ban_hop")
                .Narrate("Chú Tám nhìn bản ghi hai phút. Chú gõ ba cái lên mặt bàn, như gõ vào vỏ máy.")
                .Say(Tam, N, "Cái hộp tôi sửa vẫn đang gửi. Tôi chỉ hỏi nó có chạy không. Chưa bao giờ hỏi nó gửi cho ai.", R)
                .Say(Tam, N, "Tôi xấu hổ. Nên tôi im.", R)
                .Say(Tam, T, "Cha cậu sửa trạm với tôi mấy mùa hè. Phần “chép sổ” là cha cậu cài.", R)
                .Narrate("Chú trải lên bàn một tờ sơ đồ trạm cũ. Nếp gấp đã mòn tới mức tờ giấy tự mở ra.")
                .Say(Tam, H, "Tôi đặt tên nó là “Thiên Lý Truyền Âm”. Tên kêu thì người ta mới chịu đọc.", R)
                .Narrate("Lệ nhìn tờ danh sách “đã quen đường về”. Chị không hỏi tôi chép lúc nào.")
                .Say(Le, N, "Bên mua heo già với bên ký phiếu là một. Chị biết lâu rồi.", R)
                .Say(Le, T, "Chị im để giữ mối. Mối này trả giá gấp đôi.", R)
                .Say(Le, N, "Họ chỉ đòi heo “giữ nguyên đàn”. Và chưa bao giờ tới tận nơi lấy hàng.", R)
                .Narrate("Không ai nói tới thứ trên trời. Không ai cần nói.")
                .Then("Q14_toi_khoa");

            yield return Scene("Q14_toi_khoa", "Cơm tối", NarrativeTrigger.Manual, "", IsKhoa,
                    "Tối đó bà Sáu tự mang cơm lên trại, không ai gọi. Từ đó tối nào cũng vậy.")
                .FadeOut(600)
                .PlaceAt(Sau, "farm:station:main_house", CardinalDirection.South)
                .FadeIn(600)
                .Narrate("Tối đó bà Sáu mang cơm lên trại. Không ai gọi.")
                .Say(Sau, H, "Ăn thêm miếng nữa đi con.", R)
                .Narrate("Từ đó tối nào cũng vậy.")
                .Narrate("Lề: “Không có lời nguyền nào cấm nói. Chỉ có những người đã hứa.”")
                .Notebook("Q14");

            // ---- Q15 Hết sương chưa hết chuyện (boss wave of cycle 15; win or lose both count)
            yield return Scene("Q15_start_khoa", "Hết sương chưa hết chuyện", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_DONE Q14,SCENE_SEEN Q14_done_khoa," + IsKhoa,
                    "Chú Tám báo: đợt lớn tới, sương sẽ phủ kín trại. Dựng Đèn Pha quanh chuồng và nhớ cấp điện.", priority: 18)
                .IfGoto("STORY_QUEST_DONE Q15", "done")
                .PlaceAt(Tam, "farm:station:warehouse", CardinalDirection.South)
                .Narrate("Sáng. Chú Tám lên trại với một cuộn dây điện và một gương mặt nghiêm.")
                .Say(Tam, T, "Đợt lớn tới, sương sẽ phủ kín trại. Không phải sương thường.", R)
                .Say(K, N, "Sương gì.")
                .Say(Tam, N, "Sương. Cậu ghi vậy là đủ.", R)
                .Say(Tam, H, "Cậu dựng Đèn Pha quanh chuồng. Tôi gọi nó là “Phá Vụ Thần Đăng”.", R)
                .Say(Tam, T, "Đèn phải có điện. Đèn tắt thì tên kêu cỡ nào cũng vô ích.", R)
                .Narrate("Tôi ghi: “Đèn. Điện. Tên.” Theo thứ tự quan trọng, ngược với thứ tự chú nói.")
                .Hint("build")
                .Goto("end")
                .Label("done")
                .Then("Q15_done_khoa")
                .Label("end");

            yield return Scene("Q15_built_khoa", "Luật của sương", NarrativeTrigger.Signal, Sig(QuestSignal.StructureBuilt),
                    "STORY_QUEST_ACTIVE Q15,SCENE_SEEN Q15_start_khoa," + IsKhoa,
                    "Luật chú Tám dặn: đầu tiên giữ đàn trong Nơi Trú; khi nó nhảy giữa chỗ tối thì chỉ đánh lúc nó đứng trong đèn; " +
                    "khi nó tách bóng thì đánh nhanh. Lệ bán tin “lúc nào”.")
                .Say(Tam, N, "Được. Giờ nghe luật.", R)
                .Say(Tam, T, "Lúc đầu nó phủ sương, thả bóng với chó sương. Cậu giữ đàn trong Nơi Trú.", R)
                .Say(Tam, T, "Rồi nó nhảy giữa các chỗ tối. Chỉ lúc nó đứng trong đèn mới đánh đau được.", R)
                .Say(Tam, Ag, "Cuối cùng nó tách ra làm mấy cái bóng. Đừng đứng nhìn. Đàn mất tinh thần từng giây.", R)
                .Say(Tam, N, "Muốn biết “lúc nào” thì mua tin của Lệ. Mua trước thì đỡ phải đoán.", R)
                .Narrate("Tôi ghi đủ ba pha. Phía dưới, chừa một dòng trống. Tôi không biết để làm gì.")
                .Hint("defend");

            yield return Scene("Q15_done_khoa", "Gáy sổ", NarrativeTrigger.QuestCompleted, "Q15",
                    "SCENE_SEEN Q15_start_khoa," + IsKhoa,
                    "Sau đợt sương, Cúc chồng tuyến đi của sương lên bản ghi tín hiệu phát từ trại: khớp. Mận đưa cốc nước; " +
                    "Khoa mới thấy mình siết cuốn sổ tới gãy gáy.", priority: 10)
                .Music("")
                .PlaceAt(Cuc, "farm:station:main_house", CardinalDirection.South)
                .Backdrop("bg_trai_suong_dem")
                .Narrate("Đợt đã qua. Sương rút về phía đồng, chậm như người đi giật lùi.")
                .Backdrop("")
                .Narrate("Cúc trải bản ghi tín hiệu phát đi từ trại lên bàn, rồi chồng lên đó các tuyến sương đã đi.")
                .Say(Cuc, T, "Tuyến đi của nó trùng tín hiệu phát từ trại. Từng khúc một.", R)
                .Say(Cuc, N, "Tôi đo ba lần. Số liệu không đổi ý.", R)
                .Narrate("Cột phải: “Tuyến đi của sương trùng tín hiệu phát từ trại.”")
                .Narrate("Còn một chuyện khác thì tôi không ghi.")
                .Narrate("Mặt tôi không đổi. Người ta nói vậy.")
                .Narrate("Mận đưa tôi cốc nước. Lúc đưa tay ra nhận, tôi mới thấy tay kia đang siết cuốn sổ.")
                .Narrate("Siết chặt tới mức gáy sổ gãy làm đôi.")
                .Narrate("Tôi không nhớ vì sao.")
                .Notebook("Q15")
                .Then("C3_end_khoa");

            // Chapter-end image: the part he did not write. A child's voice answers "Dạ" from the fog. Silence, then a pig.
            yield return Scene("C3_end_khoa", "Phần không ghi", NarrativeTrigger.DayStart, "",
                    "SCENE_SEEN Q15_done_khoa," + IsKhoa,
                    "Phần Khoa không ghi: giữa sương, tiếng mẹ gọi “Đứng đó làm gì, vào ăn đi con.” Từ một hướng khác, " +
                    "giọng một đứa trẻ đáp: “Dạ.”", priority: 20)
                .Music("")
                .FadeOut(800)
                .Backdrop("bg_trai_suong_dem")
                .FadeIn(800)
                .Narrate("Đây là phần tôi không ghi.")
                .Narrate("Giữa sương, tôi nghe tiếng mẹ.")
                .Narrate("“Đứng đó làm gì, vào ăn đi con.”")
                .Narrate("Từ một hướng khác, giọng một đứa trẻ:")
                .Narrate("“Dạ.”")
                .Pause(2600)
                .Narrate("Sau đó tôi không nghĩ thêm được gì, cho tới hết đợt.")
                .Pause(1600)
                .Backdrop("")
                .WhiteFoot("Sáng ra, con heo chân trắng nằm ngửa trước cửa Nơi Trú, chờ ai đó cầm xô cám tới.",
                    "Sáng ra, một con heo non nằm chắn cửa Nơi Trú, chờ ai đó cầm xô cám tới.")
                .Narrate("Tôi cầm xô cám tới.")
                .Flag(ChapterDoneFlag);
        }

        // =====================================================================================================
        // An — lấy nhu: nhờ người khác đếm, khen đúng chỗ, để người ta tự lật giấy; nghe trước rồi mới nhìn; thư ban đêm.
        // =====================================================================================================
        static IEnumerable<SceneDefinition> An()
        {
            yield return Scene("C3_card_an", "Chương 3 — Chỗ trống", NarrativeTrigger.DayStart, "",
                    "STORY_FLAG " + PrevChapterFlag + ",STORY_QUEST_DONE Q10," + IsAn,
                    "Mận gọi sáu con heo già là “hội phụ huynh”; An đặt tên từng bà. Muốn đàn thành bầy: heo thân nhau, vui, " +
                    "không bệnh, chuồng vừa chỗ, hội phụ huynh đủ sáu bà thân với đàn, giữ ba ngày liền.", priority: 20)
                .Music("")
                .Banner("Chương 3 — Chỗ trống", 2200)
                .Narrate("“Nhà mình ơi, đàn heo đủ số. Mận đếm giùm An. An không đếm :)”")
                .IfGoto("STORY_QUEST_DONE Q11", "formed")
                .PlaceAt(Man, "farm:station:herd_shelter", CardinalDirection.South)
                .FadeIn(600)
                .Say(Man, H, "Chị coi mấy bà heo già kìa. Em gọi là hội phụ huynh.", R)
                .Say(Man, N, "Heo non ăn thì tụi nó đứng ngoài nhìn, y như phụ huynh đứng ngoài cổng trường.", R)
                .Say(A, L, "Trời, giống thiệt! Vậy chị đặt tên cho từng bà nha.")
                .Narrate("Tôi đặt theo tên mấy bà trong hội Phật tử của bà nội. Bà nào được gọi cũng vẫy tai.")
                .Say(Man, U, "Chị đặt tên rồi thì phải lo cho tụi nó thành bầy đó.", R)
                .Say(A, U, "Ơ, thật hả? Thành bầy là sao em?")
                .Say(Man, N, "Là đàn tự giữ nhau. Heo thân nhau, vui, không con nào bệnh. Chuồng đừng chật, đừng trống.", R)
                .Say(Man, T, "Hội phụ huynh phải đủ sáu bà, bà nào cũng thân với đàn. Giữ vậy ba ngày liền.", R)
                .Say(A, H, "Dạ, thưa cô giáo. Vậy chị đi thăm từng bà một, bà nào đứng lẻ thì chị ngồi chơi với bà đó.")
                .Hint("observe")
                .Goto("end")
                .Label("formed")
                .Narrate("Không cần đợi. Sáng nay đàn đã đứng thành bầy.")
                .Then("Q11_done_an")
                .Label("end");

            // Mystery beat, heard first: the grunting stops at one spot. An doesn't count; Mận counts for her.
            yield return Scene("Q11_done_an", "Chỗ đó im", NarrativeTrigger.QuestCompleted, "Q11",
                    "SCENE_SEEN C3_card_an," + IsAn,
                    "Sáng đàn thành bầy, tiếng ủn ỉn tắt ở một chỗ: sáu bà trong hội chừa ra một khoảng trống. An không đếm, " +
                    "nhờ Mận đếm: đủ. Thư: “Đàn chừa một chỗ, chỗ đó im.”", priority: 10)
                .Music("")
                .Narrate("Sáng. Tôi nghe ra trước khi nhìn: tiếng ủn ỉn quanh chuồng tắt đi ở đúng một chỗ.")
                .Narrate("Sáu bà trong hội cùng xoay người một lúc. Giữa đội hình hở ra một khoảng trống.")
                .Narrate("Mấy con khác đi vòng qua chỗ đó.")
                .WhiteFoot("Cả con chân trắng, đứa chưa từng chịu đi vòng qua thứ gì.", null)
                .Narrate("Tôi không đếm. Tôi để Mận đếm.")
                .Say(A, H, "Đủ chưa em?")
                .Say(Man, N, "Đủ chị.", R)
                .Pause(1800)
                .Narrate("Một con heo non lẻn tới máng của hội. Sáu bà cùng khịt mũi một lượt.")
                .Narrate("Nó lùi về chỗ cũ, ngoan như bị gọi tên trước lớp.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Bà ơi, hội của bà đủ số, Mận đếm ★. Đàn chừa một chỗ, chỗ đó im ☾.”")
                .Notebook("Q11")
                .Then("Q12_start_an");

            // ---- Q12
            const string Q12On = "STORY_QUEST_ACTIVE Q12,SCENE_SEEN Q12_start_an," + IsAn;

            yield return Scene("Q12_start_an", "Mua tin về chuyện mình vừa thấy", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_DONE Q11,SCENE_SEEN Q11_done_an," + IsAn,
                    "An muốn biết chỗ im trong đàn là gì. Mận chỉ: chị Lệ bán tin, nhà ở dưới xóm. Mua xong thì lùa đàn vô Nơi Trú để so.",
                    priority: 18)
                .IfGoto("STORY_QUEST_DONE Q12", "done")
                .Narrate("Tôi muốn biết chỗ im đó là gì. Ở xóm này, muốn biết thì phải mua.")
                .Say(A, U, "Mận ơi, ở đây ai bán tin vậy em?")
                .Say(Man, H, "Chị Lệ. Chị bán cả tin lẫn bảo hiểm chồn. Nhà chị ở dưới xóm.", R)
                .Say(Man, N, "Mua xong chị lùa đàn vô Nơi Trú mà nghe lại. Tin phải so với heo thật.", R)
                .Say(A, L, "Đi mua tin về heo nhà mình. Nghe như đi hỏi cô giáo con mình học sao.")
                .Hint("interact")
                .Goto("end")
                .Label("done")
                .Then("Q12_done_an")
                .Label("end");

            yield return Scene("Q12_le_an", "Sạp không bán loại “ai”", NarrativeTrigger.Talk, Le, Q12On,
                    "An khen sạp Lệ gọn nhất xóm. Lệ bán “lúc nào”: lúc đàn yên. Tặng kèm: đêm 05/10 hằng năm, đèn phòng bếp lò " +
                    "sáng từ 02:00 tới 02:02. Sạp không bán loại “ai”.", priority: 20, location: "")
                .Say(A, H, "Chị Lệ ơi, sạp chị gọn nhất xóm luôn á. Em mua tin nha.")
                .Say(Le, H, "Khen vậy là chị nhớ mặt em rồi đó. Tin gì?", R)
                .Say(A, U, "Sao đàn em cứ chừa ra một chỗ vậy chị?")
                .Say(Le, T, "“Lúc nào” thì chị bán được. “Tại sao” thì chị chưa định giá nổi.", R)
                .Say(A, H, "Dạ, vậy em mua “lúc nào”.")
                .Say(Le, N, "Lúc đàn yên. Đàn càng yên, chỗ trống càng rõ.", R)
                .Narrate("Tôi trả đúng giá. Lệ gói thêm một mẩu giấy, như người bán rau dúi thêm cọng hành.")
                .Say(Le, H, "Món rẻ nhất sạp, tặng em. Em không hỏi, nhưng chị cứ cho.", R)
                .Narrate("Mẩu giấy ghi: đêm 05/10 hằng năm, đèn phòng bếp lò sáng đúng từ 02:00 tới 02:02.")
                .Say(Le, N, "Cả xóm biết. Không ai hỏi.", R)
                .Say(A, U, "Ai bật vậy chị?")
                .Say(Le, T, "Sạp chị không bán loại “ai”.", R)
                .Narrate("Tôi cất mẩu giấy vào túi, không đọc lại. Tôi cười cảm ơn, và nhớ từng chữ.")
                .Say(Le, H, "Về lùa đàn vô Nơi Trú mà coi. Tin chị bán, heo em nuôi.", R)
                .Hint("recall");

            yield return Scene("Q12_herd_an", "Một góc im", NarrativeTrigger.Signal, Sig(QuestSignal.HerdRecalled), Q12On,
                    "Trong Nơi Trú ồn như chợ sáng, chỉ một góc là im. Còn phải mua tin của chị Lệ để so.")
                .Backdrop("bg_noi_tru_cho_trong")
                .Narrate("Đàn vào Nơi Trú. Trong này chật, tiếng ủn ỉn chen nhau như chợ sáng.")
                .Narrate("Chỉ một góc là im. Không con nào lấn vào, kể cả lúc bị đẩy.")
                .Backdrop("")
                .Narrate("Còn phải ghé chị Lệ mua tin, rồi đem so với cái góc im này.")
                .Hint("interact");

            yield return Scene("Q12_done_an", "Không gọi ai", NarrativeTrigger.QuestCompleted, "Q12",
                    "SCENE_SEEN Q12_start_an," + IsAn,
                    "Tin đúng: đàn yên thì chỗ trống rõ. Đêm đó, sổ năm mười bảy tuổi có dòng “02:00. Đã giữ chỗ.”, đầu dòng " +
                    "không gọi ai, cuối dòng không mặt cười. Thư gửi ba: đèn sáng hai phút, đêm 05/10, từ 2019.", priority: 10)
                .IfMissed("Q12_le_an",
                    "Chị Lệ bán tôi chữ “lúc nào”: lúc đàn yên. Còn “tại sao” thì chị chưa định giá nổi.",
                    "Chị tặng kèm món rẻ nhất sạp: đêm 05/10 hằng năm, đèn phòng bếp lò sáng từ 02:00 tới 02:02.",
                    "Tôi hỏi ai bật. Chị bảo sạp chị không bán loại “ai”.")
                .IfMissed("Q12_herd_an",
                    "Trong Nơi Trú, tiếng ủn ỉn chen nhau. Chỉ một góc là im.")
                .Narrate("Tin của chị Lệ đúng. Tôi khen chị thêm một câu nữa trong bụng.")
                .FadeOut(600)
                .Music("")
                .Backdrop("bg_so_17_tuoi_0200")
                .FadeIn(600)
                .Narrate("Tối đó, chờ cả trại ngủ, tôi mới lật về cuốn sổ năm mười bảy tuổi.")
                .Narrate("Có một dòng chữ của tôi: “02:00. Đã giữ chỗ.”")
                .Narrate("Đầu dòng không gọi ai. Cuối dòng không có mặt cười.")
                .Narrate("Tôi viết: “Ba ơi, đèn phòng bếp lò sáng hai phút, đêm 05/10, từ 2019 ★.”")
                .Narrate("Ngoài lề, bút chì: “Đêm đầu tiên là đêm An thức canh cái chai.”")
                .Pause(1800)
                .Backdrop("")
                .Narrate("Ngoài Nơi Trú có tiếng ngáy đều đều. Tôi đếm theo tiếng ngáy, không đếm heo, rồi ngủ.")
                .Notebook("Q12");

            // ---- Q13
            const string Q13On = "STORY_QUEST_ACTIVE Q13,SCENE_SEEN Q13_start_an," + IsAn;

            yield return Scene("Q13_start_an", "Chỗ không con nào đứng", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_DONE Q12,SCENE_SEEN Q12_done_an," + IsAn,
                    "An vẽ đội hình đàn lúc đàn yên: gom cả đàn vô Nơi Trú, vẽ từ hai góc, nhờ Mận đếm, rồi mời chị Dung khám.",
                    priority: 18)
                .IfGoto("STORY_QUEST_DONE Q13", "done")
                .PlaceAt(Man, "farm:station:herd_shelter", CardinalDirection.South)
                .Narrate("Sáng. Chị Lệ bảo đàn càng yên, chỗ trống càng rõ. Tôi muốn vẽ nó ra. Cho vui thôi.")
                .Say(Man, U, "Chị vẽ đàn hả? Vậy đợi tụi nó ăn no, đứng yên rồi hẵng vẽ nha.", R)
                .Say(Man, N, "Lùa hết vô Nơi Trú. Vẽ một lần từ cửa, một lần từ vách sau. Hai góc mới chắc.", R)
                .Say(A, H, "Rồi em đếm lại giùm chị nha. Xong chị mời chị Dung lên khám mấy bà.")
                .CameraTo("farm:station:herd_shelter", 600)
                .Hint("recall")
                .Goto("end")
                .Label("done")
                .Then("Q13_done_an")
                .Label("end");

            yield return Scene("Q13_herd_an", "Hai góc", NarrativeTrigger.Signal, Sig(QuestSignal.HerdGathered90), Q13On,
                    "An vẽ đội hình từ hai góc: chỗ trống nằm cùng một chỗ. Mận đếm: đủ. Còn phải mời chị Dung khám.")
                .IfGoto("HERD_ELDER_COUNT 6", "own")
                .Say(Man, N, "Hội phụ huynh nhà mình chưa đủ sáu bà. Chị lấy tạm ảnh này, em chụp đàn cũ của ông Năm.", R)
                .Narrate("Ảnh hơi mờ. Sáu con heo già đứng quây lại. Giữa chúng có một chỗ trống.")
                .Flag(OldPhotoFlag)
                .Label("own")
                .Backdrop("bg_noi_tru_cho_trong")
                .Narrate("Đàn đứng yên trong Nơi Trú. Tôi vẽ một lần từ cửa, một lần từ vách sau.")
                .Narrate("Hai bản vẽ, hai góc. Chỗ trống vẫn nằm đó, như có ai giữ.")
                .Say(A, H, "Đủ không em?")
                .Say(Man, N, "Đủ.", R)
                .Backdrop("")
                .Narrate("Còn chị Dung. Muốn chị khám thì phải hỏi đúng câu chị ghét nhất.")
                .Hint("interact");

            yield return Scene("Q13_dung_an", "Không có bệnh", NarrativeTrigger.Talk, Dung, Q13On,
                    "An hỏi bác sĩ Dung mấy bà heo già có “bị ám” không. Dung khám từng con: không có bệnh. " +
                    "Đứng chỗ nào là việc của tụi nó.", priority: 20, location: "")
                .Say(A, U, "Chị Dung ơi, mấy bà heo già nhà em có bị ám không chị?")
                .Say(Dung, Ag, "Không có bệnh nào tên là “bị ám”.", R)
                .Say(A, H, "Dạ, vậy chị khám giùm em coi bệnh tên gì.")
                .Narrate("Chị khám từng con: mắt, mũi, móng, tiếng thở. Tôi đứng nghe. Chị khám lâu hơn mức chị định.")
                .Say(Dung, N, "Không có bệnh.", R)
                .Say(Dung, T, "Còn tụi nó đứng chỗ nào là việc của tụi nó. Tôi không khám chỗ đứng.", R)
                .Narrate("Chị rửa tay rất kỹ. Rồi rửa lại lần nữa.")
                .IfGoto("SCENE_SEEN Q13_herd_an", "end")
                .Narrate("Còn bản vẽ đội hình. Phải gom cả đàn vô Nơi Trú lúc tụi nó yên.")
                .Hint("recall")
                .Label("end");

            // Mystery beat + An's one mask slip of the chapter: she smiles too long; Mận holds her face.
            yield return Scene("Q13_done_an", "Chị thôi đi", NarrativeTrigger.QuestCompleted, "Q13",
                    "SCENE_SEEN Q13_start_an," + IsAn,
                    "An chồng tấm ảnh gia đình lên bản vẽ đội hình: sáu con heo già khớp chỗ sáu người thân, khoảng trống khớp " +
                    "chỗ con bé cầm cành hoa sứ. Mận: “Em tưởng ai cũng thấy.” An cười lâu tới mức Mận phải ôm mặt chị.",
                    priority: 10)
                .Music("")
                .IfGoto("SCENE_SEEN Q13_herd_an", "drawn")
                .IfGoto("HERD_ELDER_COUNT 6", "drawn_own")
                .Narrate("Hội phụ huynh chưa đủ sáu bà. Mận đưa tôi tấm ảnh cô chụp đàn cũ của ông Năm.")
                .Flag(OldPhotoFlag)
                .Label("drawn_own")
                .Narrate("Tôi vẽ đội hình đàn từ hai góc. Chỗ trống nằm cùng một chỗ. Mận đếm: đủ.")
                .Label("drawn")
                .IfMissed("Q13_dung_an", "Chị Dung khám từng con, chỉ nói: “Không có bệnh.”")
                .FadeOut(600)
                .FadeIn(600)
                .Narrate("Tối đó tôi chồng tấm ảnh gia đình lên bản vẽ đội hình. Cho vui thôi.")
                .Backdrop("bg_anh_doi_hinh_chong")
                .IfGoto("STORY_FLAG " + OldPhotoFlag, "old")
                .Narrate("Sáu con heo già khớp đúng chỗ sáu người thân đứng trong ảnh.")
                .Goto("match")
                .Label("old")
                .Narrate("Sáu con heo già trong ảnh của Mận khớp đúng chỗ sáu người thân đứng trong ảnh nhà tôi.")
                .Label("match")
                .Narrate("Khoảng trống khớp đúng chỗ con bé cầm cành hoa sứ.")
                .Pause(1500)
                .Narrate("Sau lưng tôi, Mận thì thầm:")
                .Say(Man, N, "Em tưởng ai cũng thấy.", R)
                .Narrate("Tôi quay sang cười với Mận. Cười thật lâu.")
                .Say(A, Portraits.AnTooBright, "Ơ, thật hả?")
                .Narrate("Lâu tới mức Mận phải lấy hai tay ôm mặt tôi.")
                .Say(Man, Sd, "Chị thôi đi.", R)
                .Narrate("Tôi không ghi gì vào sổ.")
                .Pause(2000)
                .Backdrop("")
                .Narrate("Ngoài chuồng, một bà trong hội hắt xì. Cả hội cùng quay sang nhìn, như trách.");

            // ---- Q14
            const string Q14On = "STORY_QUEST_ACTIVE Q14,SCENE_SEEN Q14_start_an," + IsAn;

            yield return Scene("Q14_start_an", "Buổi họp ai cũng mang ghế", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_DONE Q13,SCENE_SEEN Q13_done_an," + IsAn,
                    "Ba tờ giấy cho ba người: phiếu thu hoạch, bản ghi hai phút, danh sách heo già. An không đưa tận tay; " +
                    "chị đi mời bà Sáu, chú Tám, chị Lệ lên uống trà ở nhà chính.", priority: 18)
                .IfGoto("STORY_QUEST_DONE Q14", "done")
                .PlaceAt(Man, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Sáng. Tôi lấy ra ba tờ giấy, nhưng không định đưa tận tay ai.")
                .Narrate("Phiếu thu hoạch ở vành ngoài, chữ ký bảy nét. Bản ghi “đã nhận” sớm hơn “đã gửi” hai phút.")
                .Narrate("Và danh sách heo già “đã quen đường về”. Hôm đó tôi trả lại cả xấp, tối về chép lại từ trí nhớ.")
                .Narrate("Chiều nay tôi mời trà ở nhà chính. Mời từng người, mỗi người một cái cớ dễ thương.")
                .Say(Man, U, "Chị mời trà hả? Ở xóm này đi uống trà là ai cũng tự mang ghế đó.", R)
                .Say(A, L, "Vậy chị khỏi lo thiếu ghế. Tiện ghê.")
                .Hint("interact")
                .Goto("end")
                .Label("done")
                .Then("Q14_done_an")
                .Label("end");

            yield return Scene("Q14_sau_an", "Mời bà Sáu", NarrativeTrigger.Talk, Sau, Q14On,
                    "An mời bà Sáu lên uống trà, nghe chuyện hè nhà mình. Bà giao xách thùng nước, rồi đứng đợi.",
                    priority: 20, location: "")
                .Say(A, H, "Bà ơi, chiều nay con pha trà ở nhà chính. Bà lên nghe con kể chuyện hè hồi nhỏ nha.")
                .Say(Sau, H, "Chuyện hè nhà con hả? Bà nghe mấy lần cũng được.", R)
                .Narrate("Bà đưa tôi cái thùng nước. Tôi xách ra sau quán, vừa đi vừa hát.")
                .Narrate("Lúc quay lại, bà đang đứng đợi.")
                .Say(Sau, N, "Chiều bà lên. Bà mang ghế.", R);

            yield return Scene("Q14_tam_an", "Mời chú Tám", NarrativeTrigger.Talk, Tam, Q14On,
                    "An mời chú Tám lên uống trà, kể chuyện máy nào tên kêu nhất. Chú nhận lời, tự mang ghế.",
                    priority: 20, location: "")
                .Say(A, H, "Chú Tám ơi, chiều nay con pha trà. Chú lên kể con nghe máy nào tên kêu nhất nha.")
                .Narrate("Chú Tám gõ ba cái vào cột rào gần nhất, nghe, rồi bật cười.")
                .Say(Tam, H, "Trà thì tôi tới. Tôi mang ghế. Ghế nhà người ta kêu, tôi ngồi không yên.", R);

            yield return Scene("Q14_le_an", "Mời chị Lệ", NarrativeTrigger.Talk, Le, Q14On,
                    "An mời Lệ lên uống trà, không bán gì. Lệ tới làm khách, tự mang ghế.",
                    priority: 20, location: "")
                .Say(A, H, "Chị Lệ ơi, chiều nay em pha trà. Có bánh. Không bán gì hết.")
                .Say(Le, T, "Không bán gì mà mời chị? Lạ à nha.", R)
                .Say(Le, H, "Thôi được, chị tới làm khách. Chị mang ghế. Ghế đi uống trà phải là ghế quen.", R);

            yield return Scene("Q14_done_an", "Không. Mai mưa.", NarrativeTrigger.QuestCompleted, "Q14",
                    "SCENE_SEEN Q14_start_an," + IsAn,
                    "Ai tới cũng tự mang ghế. An rót trà, ba tờ giấy nằm dưới ba cái chén. Cả phòng sắp nói “Mai trời nắng” thì Mận " +
                    "nói: “Không. Mai mưa.” Bà Sáu nhấc chén, thấy chữ ký bảy nét, và tự kể.", priority: 10)
                .PlaceAt(Sau, "farm:station:main_house", CardinalDirection.South)
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                .Backdrop("bg_trai_nha_chinh_ban_hop")
                .Narrate("Chiều. Bàn ở nhà chính đủ ghế. Vậy mà ai tới cũng tự vác theo ghế riêng.")
                .Narrate("Tôi rót trà. Ba tờ giấy nằm sẵn dưới ba cái chén, như lót chén cho khỏi nóng tay.")
                .Say(A, H, "Hè nào mẹ con cũng nấu lẩu, ăn cay tới chảy nước mắt. Ba thì ngồi sửa cái quạt suốt.")
                .Say(Sau, H, "Mẹ con ăn cay thiệt. Bà nêm cỡ nào cũng chê nhạt.", R)
                .Narrate("Bà nói xong mới nghe ra mình vừa nói gì. Tôi rót thêm trà, như không nghe.")
                .Say(A, U, "Ơ, vậy hè nào nhà con cũng lên đây hả bà? Lên làm gì vậy ta?")
                .Narrate("Cả phòng hít vào một lượt, sắp đồng thanh câu quen: “Mai trời…”")
                .Music("")
                .Say(Man, N, "Không. Mai mưa.", R)
                .Narrate("Mọi người quay lại nhìn Mận. Rồi ai cũng nhấc chén trà lên, cho có việc mà làm.")
                .Narrate("Bà Sáu nhấc chén. Dưới chén là chữ ký bảy nét. Bà đặt chén xuống, không uống.")
                .Say(Sau, Sd, "Chữ ký này bà thấy rồi. Trên giấy hẹn ông Năm nhận.", R)
                .Say(Sau, N, "Bà hứa với ông Năm không làm người mới sợ mà bỏ đi. Nên bà im.", R)
                .Say(Sau, N, "Mà chữ này trùng phiếu mộ nhà con. Nhà con có trong hồ sơ lâu rồi. Bà im cũng không che được ai.", R)
                .Say(Sau, N, "Hè nào bà cũng đón đủ sáu với một người nhà con.", R)
                .Say(Sau, W, "Có một lần, bà với ông Năm thấy một chuyến giao hàng hỏng.", R)
                .Say(Sau, Sd, "Còn chuyện ở thành phố thì bà không biết.", R)
                // 9.1, An's way: she asks softly, lets Bà Sáu answer at her own pace. Loss: no joke.
                .Say(A, H, "Bà nhớ mẹ con hả bà?")
                .Narrate("Bà lau một vệt nước trên bàn, đặt khăn xuống cạnh chén trà của tôi.")
                .Say(Sau, N, "Nhớ.", R)
                .Say(A, N, "Hồi đó mẹ con lên đây làm gì vậy bà?")
                .Say(Sau, N, "Chăm đàn. Ghi lại những gì bả thấy.", R)
                .Say(A, N, "Còn ba con?")
                .Say(Sau, N, "Sửa máy với ông Tám.", R)
                .Say(Sau, Sd, "Bà biết nhà con từng ở đây. Bà biết ông Năm đợi người mang vòng tới.", R)
                .Say(Sau, Sd, "Còn chuyện xảy ra ở nhà con thì bà không biết.", R)
                .Say(A, N, "Sao bà không kể con từ đầu?")
                .Say(Sau, N, "Bà cứ nghĩ để con ăn một bữa, ngủ một đêm, sáng mai kể sẽ dễ hơn.", R)
                .Say(A, N, "Bà nghĩ vậy mấy đêm rồi bà?")
                .Narrate("Bà nhìn tôi.")
                .Say(Sau, Sd, "Nhiều lắm.", R)
                .Narrate("Ngoài sân, một con heo húc lăn cái xô. Hai bà cháu vẫn ngồi yên.")
                .Narrate("Tôi rót thêm trà cho bà. Lần này tôi không kể chuyện gì.")
                .Then("Q14_hop2_an");

            yield return Scene("Q14_hop2_an", "Ba cái chén", NarrativeTrigger.Manual, "", IsAn,
                    "Chú Tám nhấc chén, thấy bản ghi hai phút: cái hộp chú sửa vẫn đang gửi; ba An cài phần “chép sổ”. " +
                    "Lệ nhấc chén, thấy danh sách: bên mua heo già và bên ký phiếu là một.")
                .Backdrop("bg_trai_nha_chinh_ban_hop")
                .Narrate("Chú Tám nhấc chén, thấy bản ghi hai phút. Chú gõ ba cái lên mặt bàn, như gõ vào vỏ máy.")
                .Say(Tam, N, "Cái hộp tôi sửa vẫn đang gửi. Tôi chỉ hỏi nó có chạy không. Chưa bao giờ hỏi nó gửi cho ai.", R)
                .Say(Tam, N, "Tôi xấu hổ. Nên tôi im.", R)
                .Say(Tam, T, "Ba cháu sửa trạm với tôi mấy mùa hè. Phần “chép sổ” là ba cháu cài.", R)
                .Narrate("Chú trải lên bàn một tờ sơ đồ trạm cũ. Nếp gấp đã mòn tới mức tờ giấy tự mở ra.")
                .Say(A, H, "Trạm này tên gì vậy chú?")
                .Say(Tam, H, "“Thiên Lý Truyền Âm”. Tên kêu thì người ta mới chịu đọc.", R)
                .Narrate("Chị Lệ nhấc chén, thấy danh sách “đã quen đường về”. Chị nhìn tôi một nhịp, rồi cười.")
                .Narrate("Nụ cười của người buôn vừa bị trả giá đúng.")
                .Say(Le, N, "Bên mua heo già với bên ký phiếu là một. Chị biết lâu rồi.", R)
                .Say(Le, T, "Chị im để giữ mối. Mối này trả giá gấp đôi.", R)
                .Say(Le, N, "Họ chỉ đòi heo “giữ nguyên đàn”. Và chưa bao giờ tới tận nơi lấy hàng.", R)
                .Narrate("Không ai nhắc tới chuyện trên trời. Ai cũng uống hết trà.")
                .Then("Q14_toi_an");

            yield return Scene("Q14_toi_an", "Cơm tối", NarrativeTrigger.Manual, "", IsAn,
                    "Tối đó bà Sáu tự mang cơm lên trại, không ai gọi. Từ đó tối nào cũng vậy. An viết ba lá thư.")
                .FadeOut(600)
                .PlaceAt(Sau, "farm:station:main_house", CardinalDirection.South)
                .FadeIn(600)
                .Narrate("Tối đó bà Sáu mang cơm lên trại. Không ai gọi.")
                .Say(Sau, H, "Ăn thêm miếng nữa đi con.", R)
                .Narrate("Từ đó tối nào cũng vậy.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Khuya, tôi viết: “Mẹ ơi, bà Sáu nhớ mẹ. Mẹ lên đây chăm đàn, ghi lại những gì mẹ thấy ★ :)”")
                .Narrate("“Ba ơi, phần “chép sổ” trong trạm là ba cài. Chú Tám còn giữ sơ đồ ★.”")
                .Narrate("“Nhà mình ơi, bên mua heo già với bên ký phiếu là một ★.”")
                .Narrate("Ngoài lề, bút chì: “Không có lời nguyền nào cấm nói. Chỉ có những người đã hứa.”")
                .Notebook("Q14");

            // ---- Q15
            yield return Scene("Q15_start_an", "Hết sương chưa hết chuyện", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_DONE Q14,SCENE_SEEN Q14_done_an," + IsAn,
                    "Chú Tám báo: đợt lớn tới, sương sẽ phủ kín trại. Dựng Đèn Pha quanh chuồng và nhớ cấp điện.", priority: 18)
                .IfGoto("STORY_QUEST_DONE Q15", "done")
                .PlaceAt(Tam, "farm:station:warehouse", CardinalDirection.South)
                .Narrate("Sáng. Chú Tám lên trại với một cuộn dây điện, mặt nghiêm hơn mọi hôm.")
                .Say(Tam, T, "Đợt lớn tới, sương sẽ phủ kín trại. Không phải sương thường.", R)
                .Say(A, U, "Ơ, thật hả? Sương gì vậy chú?")
                .Say(Tam, N, "Sương. Cháu cứ gọi vậy.", R)
                .Say(Tam, H, "Cháu dựng Đèn Pha quanh chuồng. Tôi gọi nó là “Phá Vụ Thần Đăng”.", R)
                .Say(A, L, "Tên hay ghê, nghe như chiêu cuối luôn chú.")
                .Say(Tam, T, "Đèn phải có điện. Đèn tắt thì tên kêu cỡ nào cũng vô ích.", R)
                .Hint("build")
                .Goto("end")
                .Label("done")
                .Then("Q15_done_an")
                .Label("end");

            yield return Scene("Q15_built_an", "Luật của sương", NarrativeTrigger.Signal, Sig(QuestSignal.StructureBuilt),
                    "STORY_QUEST_ACTIVE Q15,SCENE_SEEN Q15_start_an," + IsAn,
                    "Luật chú Tám dặn: đầu tiên giữ đàn trong Nơi Trú; khi nó nhảy giữa chỗ tối thì chỉ đánh lúc nó đứng trong đèn; " +
                    "khi nó tách bóng thì đánh nhanh. Chị Lệ bán tin “lúc nào”.")
                .Say(Tam, N, "Được. Giờ nghe luật.", R)
                .Say(Tam, T, "Lúc đầu nó phủ sương, thả bóng với chó sương. Cháu giữ đàn trong Nơi Trú.", R)
                .Say(Tam, T, "Rồi nó nhảy giữa các chỗ tối. Chỉ lúc nó đứng trong đèn mới đánh đau được.", R)
                .Say(Tam, Ag, "Cuối cùng nó tách ra làm mấy cái bóng. Đừng đứng nhìn. Đàn mất tinh thần từng giây.", R)
                .Say(Tam, N, "Muốn biết “lúc nào” thì mua tin của Lệ. Mua trước thì đỡ phải đoán.", R)
                .Say(A, H, "Dạ. Đèn sáng thì đánh, bóng tách thì đánh nhanh. Con nhớ rồi.")
                .Hint("defend");

            yield return Scene("Q15_done_an", "Không sao đâu mà", NarrativeTrigger.QuestCompleted, "Q15",
                    "SCENE_SEEN Q15_start_an," + IsAn,
                    "Sau đợt sương, Cúc chồng tuyến đi của sương lên bản ghi tín hiệu phát từ trại: khớp. Mận đưa cốc nước; " +
                    "An cười cảm ơn. Mận ngồi xuống cạnh chị, tới khi chị thôi cười.", priority: 10)
                .Music("")
                .PlaceAt(Cuc, "farm:station:main_house", CardinalDirection.South)
                .Backdrop("bg_trai_suong_dem")
                .Narrate("Đợt đã qua. Sương rút về phía đồng, chậm như người đi giật lùi.")
                .Backdrop("")
                .Narrate("Cúc trải bản ghi tín hiệu phát đi từ trại lên bàn, rồi chồng lên đó các tuyến sương đã đi.")
                .Say(Cuc, T, "Tuyến đi của nó trùng tín hiệu phát từ trại. Từng khúc một.", R)
                .Say(Cuc, N, "Tôi đo ba lần. Số liệu không đổi ý.", R)
                .Narrate("Mận đưa tôi cốc nước. Tôi cười cảm ơn.")
                .Say(A, Portraits.AnTooBright, "Cảm ơn em nha. Chị không sao đâu mà!")
                .Narrate("Mận không cầm cốc về. Mận ngồi xuống cạnh tôi, không nói gì.")
                .Narrate("Ngồi tới khi tôi thôi cười.")
                .Pause(2000)
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, đường đi của sương trùng tín hiệu phát từ trại. Chị Cúc đo ba lần ★.”")
                .Notebook("Q15")
                .Then("C3_end_an");

            // Chapter-end image, heard: a girl's voice answers "Dạ" from the fog. Silence, then a pig.
            yield return Scene("C3_end_an", "Chuyện không viết", NarrativeTrigger.DayStart, "",
                    "SCENE_SEEN Q15_done_an," + IsAn,
                    "Chuyện An không viết cho ai: giữa sương, tiếng mẹ gọi “Đứng đó làm gì, vào ăn đi con.” Từ một hướng khác, " +
                    "giọng một bé gái đáp: “Dạ.”", priority: 20)
                .Music("")
                .FadeOut(800)
                .Backdrop("bg_trai_suong_dem")
                .FadeIn(800)
                .Narrate("Có một chuyện tôi không viết cho ai.")
                .Narrate("Giữa sương, tôi nghe trước khi kịp nhìn. Tiếng mẹ.")
                .Narrate("“Đứng đó làm gì, vào ăn đi con.”")
                .Narrate("Từ một hướng khác, giọng một bé gái:")
                .Narrate("“Dạ.”")
                .Pause(2600)
                .Narrate("Sau đó tôi không nghe thêm được gì, cho tới hết đợt.")
                .Pause(1600)
                .Backdrop("")
                .WhiteFoot("Sáng ra, con heo chân trắng nằm ngửa trước cửa Nơi Trú. Nghe tiếng xô cám, nó lật dậy, đúng giờ.",
                    "Sáng ra, một con heo non nằm chắn cửa Nơi Trú. Nghe tiếng xô cám, nó lồm cồm dậy, đúng giờ.")
                .Flag(ChapterDoneFlag);
        }
    }
}
