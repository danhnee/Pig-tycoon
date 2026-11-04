using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Chương 2 — "Hai phút" (Q06–Q10), both routes. Canon: story bible 2.3–2.7, chapter table row 2 (thẻ chương, câu hỏi
    /// "Ai đang nhận hàng, và nhận trước khi gửi bằng cách nào?", mốc lạ Q06/Q07/Q10, hình kết chương: cái bóng trong kính),
    /// Phần 5 / 5B rows Q06–Q10, notebook table 10.4 (Q06, Q07). Same rules as Chapter 1: Khoa confronts and writes in front
    /// of people; An lets people talk, never opens her notebook in public and writes letters at night (★ ai cũng kiểm được,
    /// ☾ chỉ An nghe ra). Villagers never say the number after six ("sáu với một"), nobody names the thing in the sky
    /// ("gió lốc", "thứ kéo heo lên"), a silent beat (no music) follows every mystery and life comes back through a pig or a
    /// villager's habit. Rewards are paid by StoryQuestBook; these scenes only set flags, open notebook pages and show hints.
    /// One mask slip each this chapter (bible 2.7): Khoa speaks gently to a pig (Q08); An stops smiling for a minute (Q08).
    /// Flow: next DayStart after chapter1_done → C2_card (+ Q06 ask) → NpcHired → Q06_done ⇒ Q07_start → day/night market,
    /// Tồn → Q07_done ⇒ Q08_start → TalkCuc → Q08_done (⇒ Q09_start if ≥ 8 major waves, else a later DayStart) → TalkAnhCa →
    /// Q09_done ⇒ Q10_start → boss wave ends → TalkTamVa → Q10_done ⇒ C2_end (sets <see cref="ChapterDoneFlag"/>).
    /// Steps may come in any order: each mid beat checks what is still missing (flags / SCENE_SEEN) and points at it.
    /// </summary>
    public static class NarrativeContentChapter2
    {
        const string K = CastIds.Khoa, A = CastIds.An, Man = CastIds.Man, Tam = CastIds.TamVa, Ton = CastIds.OngTon,
            Ca = CastIds.AnhCa, Sau = CastIds.BaSau, Cuc = CastIds.Cuc;
        const string N = Portraits.Neutral, H = Portraits.Happy, L = Portraits.Laugh, U = Portraits.Surprised,
            T = Portraits.Thinking, Sd = Portraits.Sad, W = Portraits.Worried;
        const string IsKhoa = "PROTAGONIST Khoa", IsAn = "PROTAGONIST An";
        const string Gate = "MAJOR_WAVES_SURVIVED 8";
        const StageSide R = StageSide.Right;

        /// <summary>Flag set when the chapter-end image has played (Chapter 3 can wait on it).</summary>
        public const string ChapterDoneFlag = "chapter2_done";

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
        static string Sig(QuestSignal s) => s.ToString();

        public static IEnumerable<SceneDefinition> Scenes()
        {
            foreach (var s in Khoa()) yield return s;
            foreach (var s in An()) yield return s;
        }

        // =====================================================================================================
        // Khoa — lấy cương: hỏi thẳng, làm xong việc vặt rồi quay lại hỏi đúng câu cũ, ghi sổ trước mặt người ta.
        // =====================================================================================================
        static IEnumerable<SceneDefinition> Khoa()
        {
            const string Q06On = "STORY_QUEST_ACTIVE Q06," + IsKhoa, Q07On = "STORY_QUEST_ACTIVE Q07," + IsKhoa,
                Q09On = "STORY_QUEST_ACTIVE Q09," + Gate + "," + IsKhoa, Q10On = "STORY_QUEST_ACTIVE Q10," + IsKhoa;

            // ---- Chapter card + Q06 ask: the box knocks with the generator off; the hard question gets a chore.
            yield return Scene("C2_card_khoa", "Chương 2 — Hai phút", NarrativeTrigger.DayStart, "",
                    "STORY_FLAG " + NarrativeContentChapter1.ChapterDoneFlag + "," + IsKhoa,
                    "Máy phát tắt mà cái hộp trong nhà chính vẫn gõ. Tám Vá chỉ bảo “sửa rào trước đã”: " +
                    "thuê người trực Phòng Xây Dựng ở Hội Tuyển Người, rồi sửa hẳn đoạn rào phía đồng.", priority: 19)
                .Music("")
                .Banner("Chương 2 — Hai phút", 2200)
                .Narrate("“Cái hộp trong nhà chính chạy nhanh hai phút. Tôi cũng vậy, theo lời bác sĩ.”")
                .IfGoto("!STORY_QUEST_ACTIVE Q06", "end")
                .PlaceAt("player", "farm:station:main_house", CardinalDirection.North)
                .PlaceAt(Tam, "farm:spawn", CardinalDirection.North)
                .FadeIn(600)
                .Sfx("sfx_go_dac")
                .Narrate("Sáng. Máy phát tắt từ tối qua. Cái hộp kỹ thuật trong nhà chính vẫn gõ cạch cạch.")
                .Narrate("Tôi rút hẳn phích máy phát ra. Nó vẫn gõ.")
                .MoveTo(Tam, "farm:station:main_house")
                .Say(K, N, "Máy phát tắt rồi. Cái hộp vẫn gõ.")
                .Say(Tam, N, "Nguồn dự phòng cũ.", R)
                .Say(K, N, "Dự phòng cho cái gì.")
                .Say(Tam, N, "Sửa rào trước đã.", R)
                .Narrate("Ở đây, câu hỏi khó được trả lời bằng một việc vặt. Hôm nay việc vặt dài nguyên một đoạn rào.")
                .Say(Tam, T, "Đoạn rào phía đồng, chỗ cậu chặn tạm hôm đầu. Phải sửa hẳn. Mà Phòng Xây Dựng không ai trực.", R)
                .Say(Tam, N, "Phòng không người thì cái búa cũng nghỉ. Cậu thuê một người về trực đã.", R)
                .Say(Tam, N, "Lên thị trấn, tới Hội Tuyển Người. Thợ đúng nghề thì nhanh. Khác nghề cũng làm được, chậm hơn.", R)
                .Say(K, N, "Chú đúng nghề.")
                .Say(Tam, H, "Tôi đúng nghề. Tiền công tôi cũng lấy như người ta.", R)
                .Narrate("Tôi ghi: “Cái hộp: gõ khi máy tắt. Dự phòng cho: (chưa trả lời).” Câu hỏi để dành tới khi rào xong.")
                .Hint("interact")
                .Label("end");

            yield return Scene("Q06_hired_khoa", "Người trực", NarrativeTrigger.Signal, Sig(QuestSignal.NpcHired), Q06On,
                    "Có người trực thì Phòng Xây Dựng chạy. Tám Vá dặn sửa hẳn đoạn rào phía đồng, ngoài đợt.", location: "")
                .Say(Tam, N, "Phòng Xây Dựng có người trực rồi thì cái búa chạy.", R)
                .Say(Tam, T, "Giờ ra đoạn rào phía đồng, sửa hẳn. Ngoài đợt thôi. Đang đánh nhau thì không ai sửa rào.", R)
                .Narrate("Tôi ghi: “Sửa rào: ngoài đợt.” Ở đây việc gì cũng có giờ của nó. Trừ cái hộp.")
                .Hint("build");

            // Mystery beat (bible: mốc lạ Q06). Khoa finishes the chore, then asks the same question again.
            yield return Scene("Q06_done_khoa", "Bộ đếm sau khe", NarrativeTrigger.QuestCompleted, "Q06", IsKhoa,
                    "Rào xong, Khoa hỏi lại câu cũ. Tám Vá xem giúp cái khe ở bếp lò: bộ đếm bánh răng cha Khoa lắp hè 2017 " +
                    "chỉ 9 lần mở. Khoa chỉ nhớ một lần. “Nó thành thật.”", priority: 10)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Sfx("sfx_go_ba_cai")
                .Narrate("Rào xong. Tám Vá đi dọc đoạn mới, gõ từng cọc ba cái, nghiêng tai nghe.")
                .Say(K, N, "Rào xong rồi. Dự phòng cho cái gì.")
                .Say(Tam, T, "…Cậu nhớ dai.", R)
                .Say(Tam, N, "Câu đó tôi không trả lời được. Hỏi câu khác.", R)
                .Say(K, N, "Vậy chú xem giùm tôi cái khe ở bếp lò.")
                .FadeOut(500)
                .Backdrop("bg_phong_bep_lo", 600)
                .FadeIn(500)
                .Narrate("Tám Vá cạy tấm ốp sau khe bằng cán tua vít. Bên trong là một bộ đếm bánh răng, nhỏ bằng bao diêm.")
                .Backdrop("bg_bo_dem_banh_rang", 600)
                .Say(Tam, N, "Cái này ba cậu lắp. Hè 2017. Đồ là tôi chở lên.", R)
                .Say(Tam, N, "Nó đếm số lần cái hốc mở. Chín.", R)
                .Say(Tam, T, "Sáu với một lần cũ. Hai lần mới.", R)
                .Say(K, N, "Tôi mới mở có một lần.")
                .Sfx("sfx_go_ba_cai")
                .Narrate("Tám Vá gõ vào bộ đếm ba cái. Nghe.")
                .Say(Tam, N, "Nó thành thật.", R)
                .Music("")
                .Narrate("Cột phải: “Hốc mở 9 lần từ 2017. Mảnh da ở nhà tôi suốt thời gian đó.”")
                .Narrate("Lề: “Trong một cái hộp chưa từng mở.”")
                .Pause(1800)
                .Backdrop("", 600)
                .Narrate("Ngoài sân, con heo chân trắng đang gặm thử cọc rào mới. Nó cũng muốn biết cọc có thành thật không.")
                .Notebook("Q06")
                .Then("Q07_start_khoa");

            // ---- Q07 Cái này mua bằng cái kia
            yield return Scene("Q07_start_khoa", "Cái này mua bằng cái kia", NarrativeTrigger.QuestCompleted, "Q06", Q07On,
                    "Khoa xin xem sổ mượn chìa. Ông Tồn đòi xem sổ chi tiêu trước: bán một mẻ ở chợ ngày, " +
                    "đổi một món ở chợ đêm, rồi mang sổ xuống kho.")
                .PlaceAt(Ton, "farm:station:warehouse", CardinalDirection.South)
                .Narrate("Chiều, ông Tồn lên trại kiểm kho. Ông kiểm cả vỏ hộp rỗng, từng cái một.")
                .Say(K, N, "Chìa phòng bếp lò gửi ở kho ông. Tôi muốn xem sổ mượn chìa.")
                .Say(Ton, N, "Sổ của tôi chỉ mở cho người biết làm sổ.", R)
                .Say(Ton, T, "Cậu lên ngân sách một chu kỳ cho tôi coi. Cái này mua bằng cái kia, ghi cho rõ.", R)
                .Say(Ton, N, "Bán một mẻ heo ở chợ ngày. Đổi một món ở chợ đêm. Rồi mang sổ xuống kho, tôi đối chiếu.", R)
                .Narrate("Lại một việc vặt. Lần này có hóa đơn.")
                .Hint("market");

            yield return Scene("Q07_day_khoa", "Chợ ngày", NarrativeTrigger.Signal, Sig(QuestSignal.DayMarketSale), Q07On,
                    "Một dòng chợ ngày đã vào sổ chi tiêu.", location: "")
                .Flag("c2_q07_day_khoa")
                .Narrate("Bán xong một mẻ ở chợ ngày. Tôi ghi giá, ghi cả tên người mua, phòng khi ông Tồn hỏi.")
                .IfGoto("STORY_FLAG c2_q07_night_khoa", "both")
                .Narrate("Còn một dòng chợ đêm. Chợ đêm không thối tiền, nên dòng đó sẽ không có số lẻ.")
                .Hint("market")
                .Goto("end")
                .Label("both")
                .Narrate("Sổ đủ hai dòng. Giờ mang xuống kho ông Tồn trong xóm.")
                .Hint("interact")
                .Label("end");

            yield return Scene("Q07_night_khoa", "Chợ đêm", NarrativeTrigger.Signal, Sig(QuestSignal.NightMarketTrade), Q07On,
                    "Một dòng chợ đêm đã vào sổ chi tiêu.", location: "")
                .Flag("c2_q07_night_khoa")
                .Narrate("Đổi xong một món ở chợ đêm. Không ai thối tiền. Không ai hỏi tôi có cần thối không.")
                .IfGoto("STORY_FLAG c2_q07_day_khoa", "both")
                .Narrate("Còn một dòng chợ ngày: bán một mẻ heo.")
                .Hint("market")
                .Goto("end")
                .Label("both")
                .Narrate("Sổ đủ hai dòng. Giờ mang xuống kho ông Tồn trong xóm.")
                .Hint("interact")
                .Label("end");

            yield return Scene("Q07_ton_early_khoa", "Chồng hộp rỗng", NarrativeTrigger.Signal, Sig(QuestSignal.TalkOngTon), Q07On,
                    "Sổ chưa đủ dòng. Ông Tồn giao Khoa xách chồng hộp rỗng rồi khóa tủ sổ.", location: "")
                .Say(Ton, N, "Sổ chưa đủ dòng. Chợ ngày một dòng, chợ đêm một dòng. Đủ rồi hẵng xuống.", R)
                .Say(K, N, "Tôi xuống trước để hỏi một câu.")
                .Say(Ton, N, "Xách giùm tôi chồng hộp rỗng này vô góc đã.", R)
                .Narrate("Tôi xách. Quay lại thì tủ sổ đã khóa. Câu hỏi của tôi vẫn nguyên, tôi để dành.")
                .Hint("market");

            // Mystery beat (mốc lạ Q07): signed before shipped, two 24-year-old handovers, the key log.
            yield return Scene("Q07_done_khoa", "Sổ mượn chìa", NarrativeTrigger.QuestCompleted, "Q07", IsKhoa,
                    "Ông Tồn bày năm tờ phiếu ký nhận trước ngày xuất kho, hai hồ sơ bàn giao cũ (người nhận đều 24 tuổi) " +
                    "và sổ mượn chìa: phòng bếp lò, mỗi năm một dòng 2019–2025, “2 phút”, ký bảy nét.", priority: 10, location: "")
                .FadeOut(500)
                .Backdrop("bg_kho_ong_ton", 0)
                .FadeIn(500)
                .Narrate("Kho ông Tồn. Ông đọc sổ chi tiêu của tôi từ đầu tới cuối, rồi đặt sang một bên. Không chê.")
                .Sfx("sfx_lat_so")
                .Narrate("Rồi ông đặt năm tờ phiếu lên bàn. Tờ nào cũng có chữ ký nhận trước ngày xuất kho.")
                .Say(K, N, "Nhận trước khi gửi.")
                .Say(Ton, N, "Ở kho này, giấy tờ đôi khi đi nhanh hơn hàng.", R)
                .Narrate("Ông lôi ra hai hồ sơ bàn giao trại cũ. Người nhận đều đúng 24 tuổi.")
                .Narrate("Một hồ sơ có dòng bút chì: “Đã rời đi. Vẫn gửi thư về.” Hồ sơ kia không ghi gì.")
                .Say(K, N, "Tôi cũng 24.")
                .Narrate("Ông Tồn không trả lời. Ông mở sổ mượn chìa.")
                .Backdrop("bg_so_muon_chia", 600)
                .Narrate("Mục “phòng bếp lò” có đúng bảy dòng. Mỗi năm một dòng, từ 2019 đến 2025.")
                .Narrate("Người mượn ký bảy nét. Thời gian mượn: “2 phút”.")
                .Narrate("Cột ngày nhòe nước, từ hồi một trận “gió lốc” làm dột mái kho.")
                .Say(Ton, Sd, "Tôi không nhớ đã đưa chìa cho ai.", R)
                .Say(Ton, Sd, "Món gì ra khỏi kho này tôi cũng nhớ. Trừ cái chìa đó.", R)
                .Music("")
                .Narrate("Chuyện đó làm ông khó chịu hơn mọi trận gió lốc.")
                .Narrate("Cột phải: “Có người mượn chìa mỗi năm một lần. Trước tôi có ít nhất hai người 24 tuổi.”")
                .Narrate("Lề: “Hai phút đủ để kẻ một vạch.”")
                .Pause(1800)
                .Backdrop("", 600)
                .Say(Ton, N, "Sổ đối chiếu của tôi, từ giờ cậu được xem. Ký vào đây.", R)
                .Say(Ton, N, "Một lần thôi. Ký hai lần là tôi phải lập biên bản.", R)
                .Narrate("Tôi ký. Chữ ký của tôi vẫn chậm, như đang chép lại.")
                .Narrate("Tối về, con heo chân trắng đứng đợi ở cổng. Nó đòi ăn tối. Nó không cần ai ký nhận.")
                .Notebook("Q07")
                .Then("Q08_start_khoa");

            // ---- Q08 Con heo có dấu hỏi
            yield return Scene("Q08_start_khoa", "Con heo có dấu hỏi", NarrativeTrigger.QuestCompleted, "Q07",
                    "STORY_QUEST_ACTIVE Q08," + IsKhoa,
                    "Cúc nhắn: có hồ sơ một con heo gen dấu hỏi trong đàn cũ ông Năm. Ghé nhà Cúc trong xóm.", location: "")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Hôm sau, Cúc gửi lên trại một mảnh giấy. Chữ nhỏ, thẳng hàng như luống rau.")
                .Narrate("“Trại em có con nào gen ghi dấu hỏi không? Không có cũng được. Tôi có hồ sơ đàn cũ của ông Năm.”")
                .Narrate("“Ghé nhà tôi trong xóm. Mang theo câu hỏi. Không cần mang heo.”")
                .Narrate("Cúc từng nghiên cứu rau, tới khi một cây cải thi đỗ hơn thực tập sinh. Tôi tin cô về số liệu.")
                .Hint("interact");

            yield return Scene("Q08_cuc_khoa", "Khoảng trống trong ảnh", NarrativeTrigger.Signal, Sig(QuestSignal.TalkCuc),
                    "STORY_QUEST_ACTIVE Q08," + IsKhoa,
                    "Cúc nói về con heo dấu hỏi, ngón tay đặt mãi lên khoảng trống cạnh nó. “Thói quen.” " +
                    "Ba dấu hiệu: bệnh thì hỏi bác sĩ Dung, gen thì ra quầy giám định.", location: "")
                .Backdrop("bg_anh_heo_lech_dan", 600)
                .Narrate("Cúc trải hồ sơ ra bàn. Một tấm ảnh: một con heo đứng lệch khỏi đàn, cách cả bầy một khoảng.")
                .Say(Cuc, T, "Cân nặng chuẩn. Lông đều. Răng đúng tuổi. Gen ghi dấu hỏi.", R)
                .Say(Cuc, N, "Dấu hỏi nghĩa là tôi chưa biết. Chưa biết không có nghĩa là xấu.", R)
                .Narrate("Cô nói về cân nặng, màu lông, tuổi răng. Ngón tay cô thì đặt mãi lên khoảng trống bên cạnh con heo.")
                .Say(K, N, "Chị đang chỉ gì.")
                .Music("")
                .Narrate("Cúc nhìn xuống tay mình, như nhìn tay người khác. Rồi rút về.")
                .Say(Cuc, N, "Thói quen.", R)
                .Pause(1600)
                .Backdrop("", 600)
                .Say(Cuc, T, "Có ba dấu hiệu. Cái nào là bệnh thì hỏi bác sĩ Dung. Cái nào là gen thì ra quầy giám định.", R)
                .Say(Cuc, N, "Quầy giám định ở thị trấn. Hỏi một chỗ là đủ cho hồ sơ.", R)
                .Hint("interact");

            // Khoa's one slip this chapter (bible 2.7): a gentle voice he never uses with people; Mận stops one beat.
            yield return Scene("Q08_done_khoa", "Bản ghi hành vi", NarrativeTrigger.QuestCompleted, "Q08", IsKhoa,
                    "Hồ sơ con heo dấu hỏi có thêm một chữ ký; ba dấu hiệu chia hai nhóm. Khoa ghi ra lề: ở đây ai cũng " +
                    "có thói quen chỉ vào chỗ trống. Câu khó phải đợi qua tám đợt lớn.", priority: 10, location: "")
                // Dung / appraisal came before Cúc: the photo beat has not played yet, so it plays here.
                .IfGoto("SCENE_SEEN Q08_cuc_khoa", "ho_so")
                .Backdrop("bg_anh_heo_lech_dan", 600)
                .Narrate("Cúc trải hồ sơ ra bàn. Một tấm ảnh: một con heo đứng lệch khỏi đàn, cách cả bầy một khoảng.")
                .Narrate("Cô nói về cân nặng, màu lông, tuổi răng. Ngón tay cô thì đặt mãi lên khoảng trống bên cạnh con heo.")
                .Say(K, N, "Chị đang chỉ gì.")
                .Music("")
                .Narrate("Cúc nhìn xuống tay mình, như nhìn tay người khác. Rồi rút về.")
                .Say(Cuc, N, "Thói quen.", R)
                .Pause(1600)
                .Backdrop("", 600)
                .Label("ho_so")
                .Narrate("Hồ sơ con heo dấu hỏi có thêm một chữ ký. Cúc xếp ba dấu hiệu vào hai cột: “thú y” và “giám định”.")
                .Say(Cuc, H, "Hai cột. Gọn.", R)
                .Say(K, N, "Tôi quen hai cột.")
                .Narrate("Cô không hỏi cột thứ ba. Cột thứ ba trong sổ tôi là cái lề.")
                .Narrate("Lề: “Ở đây ai cũng có một thói quen chỉ vào chỗ trống.”")
                .Notebook("Q08")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối về trại, con heo chân trắng ra tận cổng đón. Tôi ngồi xuống ngang tầm nó.")
                .Say(K, N, "Về rồi hả. Ngoan. Đứng đâu cũng được, miễn là về.")
                .Narrate("Mận ngừng tay rải cám. Em nhìn tôi lâu thêm một nhịp, rồi rải tiếp.")
                .Narrate("Giọng đó tôi chưa dùng với người bao giờ.")
                .IfGoto("!" + Gate, "wait")
                .IfGoto("!STORY_QUEST_ACTIVE Q09", "end")
                .Then("Q09_start_khoa")
                .Goto("end")
                .Label("wait")
                .Narrate("Lịch anh Ca dán ở bảng tin có một dòng mới: “Câu khó: sau đợt lớn thứ tám.”")
                .Narrate("Tôi đếm số đợt trại đã qua. Chưa đủ. Tôi về giữ heo.")
                .Hint("defend")
                .Label("end");

            // ---- Q09 Qua tám lần mới được hỏi (opens at the mid-game gate: ≥ 8 major waves)
            yield return Scene("Q09_start_khoa", "Qua tám lần mới được hỏi", NarrativeTrigger.DayStart, "", Q09On,
                    "Trại đã qua tám đợt lớn. Anh Ca mang hồ sơ sống sót lên: chọn chỗ yếu của trại mà sửa, " +
                    "rồi ghé nhà anh nhận giấy có đóng dấu.", priority: 18)
                .PlaceAt(Ca, "farm:station:capacity_board", CardinalDirection.South)
                .Say(Ca, H, "Lịch hôm nay của tôi có một dòng in đậm: chúc mừng cậu.", R)
                .Narrate("Anh Ca đưa tôi một xấp hồ sơ: tám đợt lớn trại đã sống qua. Đợt nào mất gì, giữ được gì.")
                .Say(Ca, T, "Đọc hồ sơ sống sót đi. Chỗ nào yếu nhất thì sửa chỗ đó trước.", R)
                .Say(Ca, N, "Xong thì ghé nhà tôi. Có tờ giấy phải đóng dấu, không mang lên trại được.", R)
                .Narrate("Tôi đọc. Chỗ yếu nhất của trại không có trong hồ sơ. Tôi sửa chỗ yếu thứ hai.")
                .Hint("interact");

            yield return Scene("Q09_cert_khoa", "Đủ điều kiện hỏi câu khó", NarrativeTrigger.Signal, Sig(QuestSignal.TalkAnhCa), Q09On,
                    "Anh Ca trao giấy chứng nhận “đủ điều kiện hỏi câu khó”. Khoa cất nó cạnh tờ chứng nhận còn lại. " +
                    "Câu khó thì ra quán bà Sáu.", location: "")
                .Say(Ca, H, "Chúc mừng. Cậu đủ điều kiện hỏi câu khó.", R)
                .Backdrop("bg_giay_chung_nhan_cau_kho", 600)
                .Narrate("Anh Ca trao tôi một tờ giấy chứng nhận, đóng dấu đỏ hẳn hoi.")
                .Narrate("Tôi cất nó cạnh tờ giấy chứng nhận còn lại. Bộ sưu tập có thêm một tờ. Lần này chứng nhận điều ngược lại.")
                .Backdrop("", 600)
                .Say(Ca, W, "Nói nhỏ: câu cậu muốn hỏi thì không cơ quan nào tiếp nhận.", R)
                .Say(Ca, N, "Câu khó thì ra quán bà Sáu. Hỏi sau bữa cơm.", R)
                .Hint("interact");

            yield return Scene("Q09_sau_early_khoa", "Bụng đói", NarrativeTrigger.Signal, Sig(QuestSignal.TalkBaSau),
                    Q09On + ",!SCENE_SEEN Q09_cert_khoa",
                    "Bà Sáu chưa trả lời: câu khó phải có giấy anh Ca. Khoa được dúi thêm một chén cơm.", location: "")
                .Say(K, N, "Tôi có một câu khó.")
                .Say(Sau, N, "Câu khó thì phải có giấy anh Ca. Chưa có giấy thì ăn cơm.", R)
                .Say(Sau, H, "Ăn thêm miếng nữa đi con.", R)
                .Narrate("Tôi ăn hết chén cơm. Câu hỏi vẫn còn nguyên. Tôi đi lấy giấy.")
                .Hint("interact");

            // Mystery-adjacent answer: no joke after it, no music; Bà Sáu's habit brings life back.
            yield return Scene("Q09_done_khoa", "Sáu với một", NarrativeTrigger.QuestCompleted, "Q09", IsKhoa,
                    "Chiều đó ở quán, Khoa hỏi câu dễ nhất trong những câu khó. Bà Sáu: “Nhà cậu lên đây từ năm 2008. " +
                    "Sáu với một người.”", priority: 10, location: "")
                .IfGoto("SCENE_SEEN Q09_cert_khoa", "quan")
                .Say(Ca, H, "Chúc mừng. Cậu đủ điều kiện hỏi câu khó.", R)
                .Backdrop("bg_giay_chung_nhan_cau_kho", 600)
                .Narrate("Anh Ca trao tôi tờ giấy chứng nhận có đóng dấu. Tôi cất nó cạnh tờ giấy chứng nhận còn lại.")
                .Narrate("Bộ sưu tập có thêm một tờ. Lần này chứng nhận điều ngược lại.")
                .Say(Ca, W, "Nói nhỏ: câu cậu muốn hỏi thì không cơ quan nào tiếp nhận.", R)
                .Label("quan")
                .FadeOut(500)
                .Backdrop("bg_quan_ba_sau_chieu", 0)
                .FadeIn(500)
                .Narrate("Chiều đó, quán bà Sáu. Tôi đợi hết bữa mới hỏi.")
                .Narrate("Tôi chọn câu dễ nhất trong những câu khó. Muốn biết câu khó ở đây được trả lời thế nào.")
                .Say(K, N, "Nhà tôi lên đây từ bao giờ?")
                .Music("")
                .Narrate("Bà Sáu đặt cái muôi xuống.")
                .Say(Sau, N, "Nhà cậu lên đây từ năm 2008.", R)
                .Say(Sau, N, "Sáu với một người.", R)
                .Pause(1800)
                .Narrate("Cột phải: “Nhà tôi lên Bờ Rìa từ 2008. Sáu với một người.”")
                .Say(Sau, H, "Ăn thêm miếng nữa đi con.", R)
                .Notebook("Q09")
                .Then("Q10_start_khoa");

            // ---- Q10 Mẫu Hạm có mùi cám (boss wave, cycle 10). Nobody names it: "gió lốc", "thứ kéo heo lên".
            yield return Scene("Q10_start_khoa", "Mẫu Hạm có mùi cám", NarrativeTrigger.QuestCompleted, "Q09", Q10On,
                    "Tám Vá dặn: gió lốc lớn sắp tới, thứ gì kéo heo lên thì phải có thứ giữ lại. Qua đợt rồi mang tờ phiếu " +
                    "rơi sau đợt tới cho ông so với cái hộp.", location: "")
                .Backdrop("bg_quan_ba_sau_chieu", 600)
                .Narrate("Tám Vá ngồi bàn trong, ăn chậm. Nãy giờ ông nghe hết.")
                .Say(Tam, T, "Lịch anh Ca ghi: đợt lớn sắp tới có gió lốc. Loại to nhất mùa.", R)
                .Say(Tam, N, "Thứ gì kéo heo lên thì phải có thứ giữ lại.", R)
                .Say(Tam, N, "Dựng tháp phòng không. Rải lưới chặn. Hoặc thuê Tư Trời đi “săn chim lớn”.", R)
                .Say(Tam, N, "Không có gì hết thì tản đàn ra. Đừng để tụi nó đứng chụm một chỗ.", R)
                .Say(Tam, T, "Qua đợt rồi thì ghé tôi. Sau gió lốc hay có tờ phiếu rơi. Tôi muốn so nó với cái hộp.", R)
                .Narrate("Tôi ghi: “Giữ đàn. Nhặt phiếu. Mang cho Tám Vá.” Ba việc. Chỉ việc thứ nhất là việc của người.")
                .Hint("defend");

            yield return Scene("Q10_boss_khoa", "Mùi cám", NarrativeTrigger.WaveEnded, "boss", Q10On,
                    "Sau trận gió lốc lớn, ở vành ngoài có một tờ phiếu nằm sẵn. Nó có mùi cám. Khoa mang cho Tám Vá.",
                    priority: 10, location: "")
                .Music("")
                .Narrate("Đợt qua.")
                .Narrate("Ở vành ngoài lại có một tờ phiếu nằm sẵn.")
                .Narrate("Lần này nó có mùi cám.")
                .Pause(1800)
                .Narrate("Tôi kẹp tờ phiếu vào sổ. Trong chuồng, cả đàn đã nằm. Con heo chân trắng ngáy to hơn mọi khi.")
                .Hint("interact");

            yield return Scene("Q10_tam_early_khoa", "Chưa có phiếu", NarrativeTrigger.Signal, Sig(QuestSignal.TalkTamVa), Q10On,
                    "Chưa qua trận gió lốc lớn thì chưa có tờ phiếu. Tám Vá bảo về giữ đàn.", location: "")
                .Say(Tam, N, "Chưa qua trận gió lốc lớn thì chưa có tờ phiếu. Không có phiếu thì tôi so cái gì.", R)
                .Say(Tam, T, "Về giữ đàn đi. Gió tới thì tản tụi nó ra.", R)
                .Hint("defend");

            // Mystery beat (mốc lạ Q10). The end image follows in C2_end_khoa.
            yield return Scene("Q10_done_khoa", "Đã nhận, đã gửi", NarrativeTrigger.QuestCompleted, "Q10", IsKhoa,
                    "Tám Vá so tờ phiếu với nhật ký cái hộp: tín hiệu từ trại phát ra trước. Rút dây, cắm lại, thử ba lần: " +
                    "“đã nhận” luôn sáng trước “đã gửi” đúng hai phút. Cái gì đã nhận? “Cái hộp.”", priority: 10)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                // Tám was asked before the boss wave: the quest closed on the wave, so the slip is found here.
                .IfGoto("SCENE_SEEN Q10_boss_khoa", "hop")
                .Music("")
                .Narrate("Đợt qua. Ở vành ngoài lại có một tờ phiếu nằm sẵn.")
                .Narrate("Lần này nó có mùi cám.")
                .Pause(1800)
                .Label("hop")
                .Narrate("Tám Vá theo tôi lên trại. Ông mở nắp cái hộp kỹ thuật trong nhà chính.")
                .Backdrop("bg_hop_ky_thuat_nhat_ky", 600)
                .Sfx("sfx_go_ba_cai")
                .Narrate("Ông gõ ba cái, nghe. Rồi đặt tờ phiếu cạnh cuốn nhật ký của cái hộp.")
                .Say(Tam, T, "Tín hiệu từ trại phát ra trước. Rồi mới có tờ phiếu.", R)
                .Music("")
                .Sfx("sfx_tach")
                .Narrate("Ông rút một sợi dây. Mục “đã nhận” vẫn sáng lên trước mục “đã gửi”.")
                .Narrate("Sớm hơn đúng hai phút.")
                .Narrate("Ông cắm lại. Rút ra. Thử ba lần.")
                .Narrate("Lần nào cũng hai phút.")
                .Say(K, N, "Cái gì đã nhận?")
                .Say(Tam, N, "Cái hộp.", R)
                .Narrate("Rồi ông đậy nắp lại, như đậy một nồi cơm đã nghe đủ chuyện.")
                .Backdrop("", 600)
                .Narrate("Cột phải: “Đã nhận trước đã gửi: hai phút. Tám Vá thử ba lần.”")
                .Notebook("Q10")
                .Then("C2_end_khoa");

            // Chapter-end image: the reflection in the window frowns; Khoa does not. Silence, then a pig.
            yield return Scene("C2_end_khoa", "Cái bóng trong kính", NarrativeTrigger.Manual, "", IsKhoa,
                    "Mặt Khoa không đổi; Mận nhìn anh và không nói gì. Trong tấm kính cửa sổ phía sau, cái bóng của anh cau mày.")
                .Music("")
                .Narrate("Mặt tôi không đổi. Tôi biết, vì Mận đang nhìn tôi và không nói gì.")
                .Pause(1200)
                .Backdrop("bg_kinh_cua_so_nha_chinh", 600)
                .Reflection(Portraits.KhoaReflectFrown)
                .Narrate("Nhưng trong tấm kính cửa sổ phía sau, cái bóng của tôi đang cau mày.")
                .Pause(2400)
                .Backdrop("", 600)
                .Narrate("Ngoài chuồng, con heo chân trắng đẩy cái xô rỗng tới chân tôi.")
                .Narrate("Tới giờ ăn tối. Nó không cần đồng hồ.")
                .Flag(ChapterDoneFlag);
        }

        // =====================================================================================================
        // An — lấy nhu: hỏi ngây ngô, làm việc vặt thật vui để người ta tự kể; nghe ra trước rồi mới nhìn;
        // không mở sổ trước mặt ai, viết thư ban đêm.
        // =====================================================================================================
        static IEnumerable<SceneDefinition> An()
        {
            const string Q06On = "STORY_QUEST_ACTIVE Q06," + IsAn, Q07On = "STORY_QUEST_ACTIVE Q07," + IsAn,
                Q09On = "STORY_QUEST_ACTIVE Q09," + Gate + "," + IsAn, Q10On = "STORY_QUEST_ACTIVE Q10," + IsAn;

            yield return Scene("C2_card_an", "Chương 2 — Hai phút", NarrativeTrigger.DayStart, "",
                    "STORY_FLAG " + NarrativeContentChapter1.ChapterDoneFlag + "," + IsAn,
                    "An nghe cái hộp trong nhà chính gõ dù máy phát đã tắt. Tám Vá chỉ bảo “sửa rào trước đã”: " +
                    "thuê người trực Phòng Xây Dựng ở Hội Tuyển Người, rồi sửa hẳn đoạn rào phía đồng.", priority: 19)
                .Music("")
                .Banner("Chương 2 — Hai phút", 2200)
                .Narrate("“Nhà mình ơi, cái hộp trong nhà chính chạy nhanh hai phút. Bác sĩ bảo An cũng vậy :)”")
                .IfGoto("!STORY_QUEST_ACTIVE Q06", "end")
                .PlaceAt("player", "farm:station:main_house", CardinalDirection.North)
                .PlaceAt(Tam, "farm:spawn", CardinalDirection.North)
                .FadeIn(600)
                .Sfx("sfx_go_dac")
                .Narrate("Sáng. Máy phát tắt từ tối qua. Tôi nghe tiếng cạch cạch trước, rồi mới tìm ra nó.")
                .Narrate("Cái hộp kỹ thuật trong nhà chính. Nhịp gõ đều. Đều hơn tim tôi một chút.")
                .MoveTo(Tam, "farm:station:main_house")
                .Say(A, U, "Chú Tám ơi, máy tắt rồi mà cái hộp còn gõ nè. Nó đang nói chuyện với ai vậy chú?")
                .Say(Tam, N, "Nguồn dự phòng cũ.", R)
                .Say(A, H, "Dạ. Dự phòng cho cái gì vậy chú?")
                .Say(Tam, N, "Sửa rào trước đã.", R)
                .Say(A, L, "Dạ, sửa rào trước. Con thích việc có thứ tự.")
                .Say(Tam, T, "Đoạn rào phía đồng, chỗ cháu chặn tạm hôm đầu. Phải sửa hẳn. Mà Phòng Xây Dựng không ai trực.", R)
                .Say(Tam, N, "Lên thị trấn, tới Hội Tuyển Người, thuê một người về trực. Thợ đúng nghề thì nhanh.", R)
                .Say(Tam, N, "Khác nghề cũng làm được. Chậm hơn.", R)
                .Say(A, H, "Thợ đúng nghề nhất xóm là chú mà.")
                .Say(Tam, H, "Khen thì chú nhận. Tiền công thì chú vẫn lấy như người ta.", R)
                .Hint("interact")
                .Label("end");

            yield return Scene("Q06_hired_an", "Người trực", NarrativeTrigger.Signal, Sig(QuestSignal.NpcHired), Q06On,
                    "Có người trực thì Phòng Xây Dựng chạy. Tám Vá dặn sửa hẳn đoạn rào phía đồng, ngoài đợt.", location: "")
                .Say(Tam, N, "Phòng Xây Dựng có người trực rồi thì cái búa chạy.", R)
                .Say(Tam, T, "Giờ ra đoạn rào phía đồng, sửa hẳn. Ngoài đợt thôi nha cháu.", R)
                .Say(A, L, "Dạ. Lúc đánh nhau thì ai rảnh mà sửa rào ha chú.")
                .Hint("build");

            // An makes the chore pleasant; Tám opens the lid himself. First villager to feel a chill because of An.
            yield return Scene("Q06_done_an", "Cái khe có đói không", NarrativeTrigger.QuestCompleted, "Q06", IsAn,
                    "Rào xong, An hỏi cái khe ở bếp lò có ăn đồ không. Tám Vá bật cười, mở nắp: bộ đếm cha An lắp hè 2017 " +
                    "chỉ 9 lần mở. “Nó thành thật.” “Giống con.”", priority: 10)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .Sfx("sfx_go_ba_cai")
                .Narrate("Rào xong. Tám Vá đi dọc đoạn mới, gõ từng cọc ba cái. Tiếng nào cũng đặc.")
                .Say(A, H, "Chú nghe cọc như bác sĩ nghe tim vậy đó.")
                .Say(Tam, H, "Cọc tốt. Cháu đóng chắc tay.", R)
                .FadeOut(500)
                .Backdrop("bg_phong_bep_lo", 600)
                .FadeIn(500)
                .Say(A, U, "Chú ơi, cái khe này có ăn đồ không chú? Con sợ nó đói.")
                .Narrate("Tám Vá bật cười. Rồi ông lấy cán tua vít cạy tấm ốp sau khe, mở cho tôi xem.")
                .Backdrop("bg_bo_dem_banh_rang", 600)
                .Say(Tam, N, "Bộ đếm bánh răng. Ba cháu lắp, hè 2017. Đồ là chú chở lên.", R)
                .Say(Tam, N, "Nó đếm số lần cái hốc mở. Chín.", R)
                .Say(Tam, T, "Sáu với một lần cũ. Hai lần mới.", R)
                .Narrate("Tôi mới mở có một lần. Tôi không nói ra.")
                .Sfx("sfx_go_ba_cai")
                .Narrate("Tám Vá gõ vào bộ đếm ba cái. Nghe.")
                .Say(Tam, N, "Nó thành thật.", R)
                .Say(A, Portraits.AnTooBright, "Giống con.")
                .Music("")
                .Narrate("Tám Vá nhìn tôi lâu hơn cần thiết.")
                .Narrate("Rồi ông đậy nắp lại.")
                .Pause(1800)
                .Backdrop("", 600)
                .Narrate("Ngoài sân, con heo chân trắng đang gặm thử cọc rào mới. Nó cũng muốn biết cọc có thành thật không.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Ba ơi, cái khe ba lắp đã mở 9 lần từ 2017 ★.”")
                .Narrate("Ngoài lề, bút chì: “Mảnh da nằm trong hộp thiếc ở nhà suốt. Hộp chưa từng mở.”")
                .Notebook("Q06")
                .Then("Q07_start_an");

            // ---- Q07
            yield return Scene("Q07_start_an", "Cái này mua bằng cái kia", NarrativeTrigger.QuestCompleted, "Q06", Q07On,
                    "Ông Tồn lên kiểm kho: trại chưa có sổ chi tiêu. Bán một mẻ ở chợ ngày, đổi một món ở chợ đêm, " +
                    "rồi mang sổ xuống kho ông đối chiếu.")
                .PlaceAt(Ton, "farm:station:warehouse", CardinalDirection.South)
                .Narrate("Chiều, ông Tồn lên trại kiểm kho. Ông kiểm cả vỏ hộp rỗng, từng cái một.")
                .Say(A, H, "Bác Tồn! Kho con có bị trừ điểm không bác?")
                .Say(Ton, N, "Kho thì được. Sổ thì chưa.", R)
                .Say(Ton, T, "Cháu chưa có sổ chi tiêu. Cái này mua bằng cái kia, phải ghi ra cho rõ.", R)
                .Say(Ton, N, "Lên ngân sách một chu kỳ. Bán một mẻ heo ở chợ ngày, đổi một món ở chợ đêm.", R)
                .Say(Ton, N, "Xong mang sổ xuống kho, bác đối chiếu.", R)
                .Say(A, U, "Ơ, thật hả? Bác dạy con làm sổ luôn nha. Sổ con toàn vẽ mặt cười.")
                .Say(Ton, N, "Mặt cười thì không đối chiếu được.", R)
                .Hint("market");

            yield return Scene("Q07_day_an", "Chợ ngày", NarrativeTrigger.Signal, Sig(QuestSignal.DayMarketSale), Q07On,
                    "Một dòng chợ ngày đã vào sổ chi tiêu.", location: "")
                .Flag("c2_q07_day_an")
                .Narrate("Bán xong một mẻ ở chợ ngày. Bà bán rau sạp bên khen heo tôi sạch. Tôi khen rau bà xanh.")
                .Narrate("Hai bên đều nói thật. Tôi ghi giá vào sổ, rồi vẽ thêm một con heo nhỏ cạnh con số.")
                .IfGoto("STORY_FLAG c2_q07_night_an", "both")
                .Narrate("Còn một dòng chợ đêm nữa.")
                .Hint("market")
                .Goto("end")
                .Label("both")
                .Narrate("Sổ đủ hai dòng rồi. Giờ mang xuống kho bác Tồn trong xóm.")
                .Hint("interact")
                .Label("end");

            yield return Scene("Q07_night_an", "Chợ đêm", NarrativeTrigger.Signal, Sig(QuestSignal.NightMarketTrade), Q07On,
                    "Một dòng chợ đêm đã vào sổ chi tiêu.", location: "")
                .Flag("c2_q07_night_an")
                .Narrate("Đổi xong một món ở chợ đêm. Không ai thối tiền. Tôi ghi vào sổ: “Đổi ngang. Hai bên vui.”")
                .IfGoto("STORY_FLAG c2_q07_day_an", "both")
                .Narrate("Còn một dòng chợ ngày: bán một mẻ heo.")
                .Hint("market")
                .Goto("end")
                .Label("both")
                .Narrate("Sổ đủ hai dòng rồi. Giờ mang xuống kho bác Tồn trong xóm.")
                .Hint("interact")
                .Label("end");

            yield return Scene("Q07_ton_early_an", "Cơm chưa", NarrativeTrigger.Signal, Sig(QuestSignal.TalkOngTon), Q07On,
                    "Sổ chưa đủ dòng. An hỏi ông Tồn ăn cơm chưa; ông dặn đủ dòng chợ ngày, chợ đêm rồi hẵng xuống.",
                    location: "")
                .Say(Ton, N, "Sổ chưa đủ dòng. Chợ ngày một dòng, chợ đêm một dòng. Đủ rồi hẵng xuống.", R)
                .Say(A, H, "Dạ. Mà bác ăn cơm chưa bác?")
                .Say(Ton, U, "…Chưa.", R)
                .Say(Ton, N, "Ăn rồi thì sổ cũng phải đủ dòng.", R)
                .Narrate("Bác nói vậy, nhưng lúc tôi ra cửa, bác đã đi hâm cơm.")
                .Hint("market");

            yield return Scene("Q07_done_an", "Mắt làm thủ kho", NarrativeTrigger.QuestCompleted, "Q07", IsAn,
                    "An phụ ông Tồn xếp phiếu theo ngày. Ông mừng, tự mở năm tờ phiếu ký nhận trước ngày xuất kho, " +
                    "hai hồ sơ bàn giao cũ (người nhận đều 24 tuổi) và sổ mượn chìa: phòng bếp lò, 2019–2025, “2 phút”.",
                    priority: 10, location: "")
                .FadeOut(500)
                .Backdrop("bg_kho_ong_ton", 0)
                .FadeIn(500)
                .Narrate("Chiều đó tôi mang sổ xuống kho. Bác Tồn đang xếp một chồng phiếu cao tới cằm.")
                .Say(A, H, "Để con phụ bác. Xếp theo ngày hả bác?")
                .Narrate("Tôi xếp nhanh, đúng từng ngày. Bác Tồn đứng nhìn, mừng ra mặt, rồi cố giấu cái mừng đi. Không kịp.")
                .Say(Ton, U, "Cháu có mắt làm thủ kho đấy.", R)
                .Say(A, L, "Con chỉ có mắt làm con gái thôi bác.")
                .Sfx("sfx_lat_so")
                .Narrate("Để cảm ơn, bác tự mở tủ sổ. Tôi không hỏi gì.")
                .Narrate("Năm tờ phiếu tôi vừa xếp, tờ nào cũng ký nhận trước ngày xuất kho. Lúc xếp tôi thấy rồi. Tôi im.")
                .Narrate("Hai hồ sơ bàn giao trại cũ. Người nhận đều đúng 24 tuổi.")
                .Narrate("Một hồ sơ có dòng bút chì: “Đã rời đi. Vẫn gửi thư về.” Hồ sơ kia không ghi gì.")
                .Backdrop("bg_so_muon_chia", 600)
                .Narrate("Rồi sổ mượn chìa. Mục “phòng bếp lò” có bảy dòng, mỗi năm một dòng, từ 2019 đến 2025.")
                .Narrate("Người mượn ký bảy nét. Thời gian mượn: “2 phút”. Cột ngày nhòe nước, từ hồi “gió lốc” dột mái kho.")
                .Say(Ton, Sd, "Bác không nhớ đã đưa chìa cho ai.", R)
                .Say(Ton, Sd, "Món gì ra khỏi kho bác cũng nhớ. Trừ cái chìa này.", R)
                .Music("")
                .Narrate("Tay bác vuốt mép sổ, như vuốt phẳng một chỗ nhăn không có.")
                .Pause(1800)
                .Backdrop("", 600)
                .Say(Ton, N, "Sổ đối chiếu của bác, từ giờ cháu được xem. Ký một lần thôi.", R)
                .Say(Ton, N, "Ký hai lần là bác phải lập biên bản.", R)
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, có người mượn chìa phòng bếp lò mỗi năm một lần, mỗi lần hai phút ★.”")
                .Narrate("“Trước An có hai người nhận trại. Cả hai đều 24 tuổi ★ :)”")
                .Narrate("Ngoài lề, bút chì: “Hai phút đủ để kẻ một vạch.”")
                .Notebook("Q07")
                .Then("Q08_start_an");

            // ---- Q08
            yield return Scene("Q08_start_an", "Con heo có dấu hỏi", NarrativeTrigger.QuestCompleted, "Q07",
                    "STORY_QUEST_ACTIVE Q08," + IsAn,
                    "Mận nhắn lời Cúc: Cúc có hồ sơ con heo gen dấu hỏi trong đàn cũ ông Năm. Ghé nhà Cúc trong xóm.",
                    location: "")
                .FadeOut(500)
                .FadeIn(500)
                .Say(Man, N, "Chị ơi, chị Cúc nhắn chị ghé nhà chỉ. Chỉ có hồ sơ đàn cũ của ông Năm.", R)
                .Say(Man, N, "Có một con heo gen ghi dấu hỏi.", R)
                .Say(A, U, "Ơ, heo cũng có dấu hỏi hả em? Chị tưởng chỉ người mới có.")
                .Say(Man, H, "Dấu hỏi là gen chưa rõ á chị. Không phải bệnh. Chị Cúc nói vậy hoài.", R)
                .Hint("interact");

            // An does not ask; she mirrors the habit and lets Cúc catch herself.
            yield return Scene("Q08_cuc_an", "Gõ phách", NarrativeTrigger.Signal, Sig(QuestSignal.TalkCuc),
                    "STORY_QUEST_ACTIVE Q08," + IsAn,
                    "Ngón tay Cúc gõ nhẹ lên khoảng trống cạnh con heo trong ảnh. An gõ cùng nhịp; Cúc giật tay về: " +
                    "“Thói quen.” Ba dấu hiệu: bệnh thì hỏi bác sĩ Dung, gen thì ra quầy giám định.", location: "")
                .Backdrop("bg_anh_heo_lech_dan", 600)
                .Narrate("Cúc trải hồ sơ ra bàn. Một tấm ảnh: một con heo đứng lệch khỏi đàn.")
                .Say(Cuc, T, "Cân nặng chuẩn. Lông đều. Răng đúng tuổi. Gen ghi dấu hỏi.", R)
                .Narrate("Cô nói về số liệu. Ngón tay cô thì gõ nhẹ lên khoảng trống cạnh con heo. Đều như nhịp đồng hồ.")
                .Music("")
                .Narrate("Tôi không hỏi. Tôi gõ ngón tay mình cạnh ngón tay cô, cùng nhịp. Như hai đứa nhỏ chơi gõ phách.")
                .Narrate("Cúc giật tay về trước. Cô nhìn tay mình như nhìn tay người khác.")
                .Say(Cuc, N, "Thói quen.", R)
                .Say(A, H, "Em cũng có.")
                .Pause(1600)
                .Backdrop("", 600)
                .Say(Cuc, T, "Có ba dấu hiệu. Cái nào là bệnh thì hỏi bác sĩ Dung. Cái nào là gen thì ra quầy giám định.", R)
                .Say(Cuc, N, "Quầy giám định ở thị trấn. Hỏi một chỗ là đủ cho hồ sơ.", R)
                .Hint("interact");

            // An's one slip this chapter (bible 2.7): a minute without a smile until Mận waves; noticed only afterwards.
            yield return Scene("Q08_done_an", "Một phút không cười", NarrativeTrigger.QuestCompleted, "Q08", IsAn,
                    "Hồ sơ con heo dấu hỏi có thêm một chữ ký; ba dấu hiệu chia hai nhóm. Tối đó An viết: ở đây ai cũng " +
                    "gõ vào chỗ trống. Câu khó phải đợi qua tám đợt lớn.", priority: 10, location: "")
                // Dung / appraisal came before Cúc: the photo beat has not played yet, so it plays here.
                .IfGoto("SCENE_SEEN Q08_cuc_an", "ho_so")
                .Backdrop("bg_anh_heo_lech_dan", 600)
                .Narrate("Cúc trải hồ sơ ra bàn. Một tấm ảnh: một con heo đứng lệch khỏi đàn.")
                .Narrate("Cô nói về số liệu. Ngón tay cô thì gõ nhẹ lên khoảng trống cạnh con heo. Đều như nhịp đồng hồ.")
                .Music("")
                .Narrate("Tôi không hỏi. Tôi gõ ngón tay mình cạnh ngón tay cô, cùng nhịp. Như hai đứa nhỏ chơi gõ phách.")
                .Narrate("Cúc giật tay về trước. Cô nhìn tay mình như nhìn tay người khác.")
                .Say(Cuc, N, "Thói quen.", R)
                .Say(A, H, "Em cũng có.")
                .Pause(1600)
                .Backdrop("", 600)
                .Label("ho_so")
                .Narrate("Hồ sơ con heo dấu hỏi có thêm một chữ ký. Cúc xếp ba dấu hiệu vào hai nhóm: “thú y” và “giám định”.")
                .Say(Cuc, H, "Gọn. Em làm hồ sơ như người trồng rau.", R)
                .Say(A, L, "Chị khen vậy là em mừng như được điểm mười.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối về trại, tôi ngồi trên thành máng, nhìn bức tường chuồng.")
                .Pause(1600)
                .Say(Man, U, "Chị? Chị An?", R)
                .Narrate("Mận vẫy tay trước mặt tôi. Tôi cười, ngay lập tức, đúng lúc.")
                .Say(A, L, "Ơ, chị đang tính coi tường này cần mấy bao vôi đó.")
                .Narrate("Mận cười theo. Chậm hơn tôi một chút.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, chị Cúc có thói quen gõ vào chỗ trống trong ảnh ★.”")
                .Narrate("Ngoài lề, bút chì: “Ở đây ai cũng gõ vào chỗ trống. Chị cũng vậy.”")
                .Narrate("Ngoài lề, không gửi ai: “Hóa ra không cười cũng không ai chết. Lạ thật.”")
                .Notebook("Q08")
                .IfGoto("!" + Gate, "wait")
                .IfGoto("!STORY_QUEST_ACTIVE Q09", "end")
                .Then("Q09_start_an")
                .Goto("end")
                .Label("wait")
                .Narrate("Lịch anh Ca dán ở bảng tin có một dòng mới: “Câu khó: sau đợt lớn thứ tám.”")
                .Narrate("Tôi không đếm. Tôi để lịch đếm giùm, rồi đi giữ heo.")
                .Hint("defend")
                .Label("end");

            // ---- Q09
            yield return Scene("Q09_start_an", "Qua tám lần mới được hỏi", NarrativeTrigger.DayStart, "", Q09On,
                    "Trại đã qua tám đợt lớn. Anh Ca mang hồ sơ sống sót lên: chọn chỗ yếu của trại mà sửa, " +
                    "rồi ghé nhà anh nhận giấy có đóng dấu.", priority: 18)
                .PlaceAt(Ca, "farm:station:capacity_board", CardinalDirection.South)
                .Say(A, H, "Anh Ca! Lịch hôm nay có tên em không anh?")
                .Say(Ca, H, "Có. In đậm. Dòng đầu tiên: chúc mừng em.", R)
                .Narrate("Anh Ca đưa tôi một xấp hồ sơ: tám đợt lớn trại đã sống qua. Đợt nào mất gì, giữ được gì.")
                .Say(Ca, T, "Đọc hồ sơ sống sót đi. Chỗ nào yếu nhất thì sửa chỗ đó trước.", R)
                .Say(A, H, "Anh chỉ giùm em đi. Anh ghi kỹ vầy mà.")
                .Say(Ca, W, "Anh chỉ là em ỷ. Em tự đọc.", R)
                .Say(Ca, N, "Xong thì ghé nhà anh. Có tờ giấy phải đóng dấu, không mang lên trại được.", R)
                .Narrate("Tôi đọc hết, rồi đi một vòng trại để nghe. Chỗ yếu thường kêu trước khi gãy.")
                .Hint("interact");

            // Canon 5B: An says the joke out loud, at the eatery, and the whole room laughs.
            yield return Scene("Q09_cert_an", "Hai tờ giấy", NarrativeTrigger.Signal, Sig(QuestSignal.TalkAnhCa), Q09On,
                    "Anh Ca trao giấy “đủ điều kiện hỏi câu khó” ở quán bà Sáu. An ôm nó: giờ có hai giấy, một tờ chứng nhận " +
                    "điên, một tờ chứng nhận đủ tỉnh để hỏi. Cả quán cười.", location: "")
                .Say(Ca, H, "Trao giấy phải có người làm chứng. Ra quán bà Sáu.", R)
                .FadeOut(500)
                .Backdrop("bg_quan_ba_sau_chieu", 0)
                .FadeIn(500)
                .Narrate("Quán bà Sáu giờ cơm. Anh Ca đứng lên, hắng giọng như sắp đọc lịch.")
                .Say(Ca, H, "Chúc mừng. Em đủ điều kiện hỏi câu khó.", R)
                .Backdrop("bg_giay_chung_nhan_cau_kho", 600)
                .Narrate("Tờ giấy chứng nhận có đóng dấu đỏ hẳn hoi. Tôi ôm nó vào ngực.")
                .Say(A, L, "Giờ con có hai giấy rồi. Một tờ chứng nhận con điên, một tờ chứng nhận con đủ tỉnh để hỏi.")
                .Backdrop("bg_quan_ba_sau_chieu", 600)
                .Narrate("Cả quán cười. Bà Sáu cười to nhất.")
                .Say(Ca, W, "Nói nhỏ: câu em muốn hỏi thì không cơ quan nào tiếp nhận.", R)
                .Narrate("Sau quầy, bà Sáu lau mãi một cái chén đã khô. Như đang đợi tôi lại gần.")
                .Backdrop("", 600)
                .Hint("interact");

            yield return Scene("Q09_sau_early_an", "Mặt con hỏi", NarrativeTrigger.Signal, Sig(QuestSignal.TalkBaSau),
                    Q09On + ",!SCENE_SEEN Q09_cert_an",
                    "Bà Sáu bảo câu khó phải có giấy anh Ca, rồi dúi cho An thêm một chén cơm.", location: "")
                .Say(Sau, N, "Ăn đi đã. Có giấy anh Ca rồi hẵng hỏi câu khó.", R)
                .Say(A, U, "Ơ, con đâu có hỏi gì đâu bà.")
                .Say(Sau, H, "Mặt con hỏi. Ăn thêm miếng nữa đi con.", R)
                .Hint("interact");

            // An never asks: Bà Sáu answers by herself while An helps with the dishes.
            yield return Scene("Q09_done_an", "Sáu với một", NarrativeTrigger.QuestCompleted, "Q09", IsAn,
                    "Chiều đó An phụ bà Sáu rửa chén, không hỏi gì. Bà tự nói: “Nhà cháu lên đây từ năm 2008. Sáu với một người.”",
                    priority: 10, location: "")
                .IfGoto("SCENE_SEEN Q09_cert_an", "chieu")
                .Backdrop("bg_giay_chung_nhan_cau_kho", 600)
                .Narrate("Anh Ca trao giấy ngay giữa quán, có đóng dấu đỏ hẳn hoi. Tôi ôm nó vào ngực.")
                .Say(A, L, "Giờ con có hai giấy rồi. Một tờ chứng nhận con điên, một tờ chứng nhận con đủ tỉnh để hỏi.")
                .Narrate("Cả quán cười. Bà Sáu cười to nhất.")
                .Label("chieu")
                .FadeOut(500)
                .Backdrop("bg_quan_ba_sau_chieu", 0)
                .FadeIn(500)
                .Narrate("Chiều đó quán vãn khách. Tôi phụ bà Sáu rửa chén. Không hỏi gì.")
                .Narrate("Bà Sáu lau tay vào tạp dề. Rồi bà tự nói, mắt vẫn nhìn chồng chén.")
                .Music("")
                .Say(Sau, N, "Nhà cháu lên đây từ năm 2008.", R)
                .Say(Sau, N, "Sáu với một người.", R)
                .Pause(1800)
                .Narrate("Tôi cười cảm ơn bà. Bà gật, rồi đưa tôi cái chén tiếp theo.")
                .Say(Sau, H, "Ăn thêm miếng nữa đi con.", R)
                .FadeOut(500)
                .Backdrop("", 0)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, nhà mình lên Bờ Rìa từ năm 2008. Bà Sáu nói: sáu với một người ★.”")
                .Narrate("Tôi không đếm. Tôi để câu đó nằm yên trên giấy, rồi vẽ một cái mặt cười.")
                .Notebook("Q09")
                .Then("Q10_start_an");

            // ---- Q10
            yield return Scene("Q10_start_an", "Mẫu Hạm có mùi cám", NarrativeTrigger.QuestCompleted, "Q09", Q10On,
                    "Tám Vá dặn: gió lốc lớn sắp tới, thứ gì kéo heo lên thì phải có thứ giữ lại. Qua đợt rồi mang tờ phiếu " +
                    "rơi sau đợt tới cho ông so với cái hộp.", location: "")
                .Backdrop("bg_quan_ba_sau_chieu", 600)
                .Narrate("Tám Vá ngồi bàn trong, ăn chậm.")
                .Say(A, H, "Chú Tám ăn chậm vậy là món ngon, hay chú đang nghe lén con?")
                .Say(Tam, H, "Cả hai.", R)
                .Say(Tam, T, "Lịch anh Ca ghi: đợt lớn sắp tới có gió lốc. Loại to nhất mùa.", R)
                .Say(Tam, N, "Thứ gì kéo heo lên thì phải có thứ giữ lại.", R)
                .Say(Tam, N, "Dựng tháp phòng không. Rải lưới chặn. Hoặc thuê Tư Trời đi “săn chim lớn”.", R)
                .Say(Tam, N, "Không có gì hết thì tản đàn ra. Đừng để tụi nó đứng chụm một chỗ.", R)
                .Say(Tam, T, "Qua đợt rồi thì ghé chú. Sau gió lốc hay có tờ phiếu rơi. Chú muốn so nó với cái hộp.", R)
                .Say(A, H, "Dạ. Con giữ heo, nhặt giấy, mang cho chú. Con giỏi nhặt giấy lắm.")
                .Hint("defend");

            yield return Scene("Q10_boss_an", "Mùi cám", NarrativeTrigger.WaveEnded, "boss", Q10On,
                    "Sau trận gió lốc lớn, An nghe tiếng giấy ở vành ngoài: một tờ phiếu nằm sẵn, có mùi cám.",
                    priority: 10, location: "")
                .Music("")
                .Narrate("Đợt qua.")
                .Narrate("Ở vành ngoài có tiếng giấy sột soạt. Một tờ phiếu nằm sẵn.")
                .Narrate("Nó có mùi cám.")
                .Pause(1800)
                .Narrate("Tôi gấp tờ phiếu làm tư, cất vào túi áo. Trong chuồng, cả đàn đã nằm. Tiếng ngáy đều trở lại.")
                .Hint("interact");

            yield return Scene("Q10_tam_early_an", "Chưa có phiếu", NarrativeTrigger.Signal, Sig(QuestSignal.TalkTamVa), Q10On,
                    "Chưa qua trận gió lốc lớn thì chưa có tờ phiếu. Tám Vá bảo về giữ đàn.", location: "")
                .Say(Tam, N, "Chưa qua trận gió lốc lớn thì chưa có tờ phiếu. Không có phiếu thì chú so cái gì.", R)
                .Say(A, H, "Dạ. Con về giữ heo. Gió tới thì con cho tụi nó tản ra.")
                .Hint("defend");

            yield return Scene("Q10_done_an", "Đúng giờ hơn con", NarrativeTrigger.QuestCompleted, "Q10", IsAn,
                    "Tám Vá rút dây, cắm lại, thử ba lần: “đã nhận” luôn sớm hơn “đã gửi” hai phút. An rót trà mời, cười: " +
                    "“Cái hộp này đúng giờ hơn con.”", priority: 10)
                .PlaceAt(Tam, "farm:station:main_house", CardinalDirection.South)
                .PlaceAt(Man, "farm:spawn", CardinalDirection.North)
                // Tám was asked before the boss wave: the quest closed on the wave, so the slip is found here.
                .IfGoto("SCENE_SEEN Q10_boss_an", "hop")
                .Music("")
                .Narrate("Đợt qua. Ở vành ngoài có tiếng giấy sột soạt. Một tờ phiếu nằm sẵn.")
                .Narrate("Nó có mùi cám.")
                .Pause(1800)
                .Label("hop")
                .Narrate("Chú Tám theo tôi lên trại. Chú mở nắp cái hộp kỹ thuật trong nhà chính.")
                .Backdrop("bg_hop_ky_thuat_nhat_ky", 600)
                .Sfx("sfx_go_ba_cai")
                .Narrate("Chú gõ ba cái, nghe. Rồi đặt tờ phiếu cạnh cuốn nhật ký của cái hộp.")
                .Say(Tam, T, "Tín hiệu từ trại phát ra trước. Rồi mới có tờ phiếu.", R)
                .Music("")
                .Sfx("sfx_tach")
                .Narrate("Chú rút một sợi dây. Tôi nghe tiếng tách trước, rồi mới thấy mục “đã nhận” sáng lên.")
                .Narrate("Trước mục “đã gửi”. Sớm hơn đúng hai phút.")
                .Narrate("Chú cắm lại. Rút ra. Thử ba lần. Lần nào cũng hai phút.")
                .Say(Tam, N, "Cái hộp nhận.", R)
                .Narrate("Rồi chú đậy nắp lại, như đậy một nồi cơm đã nghe đủ chuyện.")
                .Backdrop("", 600)
                .Narrate("Tôi rót trà mời chú. Rót đầy, hai tay.")
                .Say(A, L, "Cái hộp này đúng giờ hơn con.")
                .Notebook("Q10")
                .Then("C2_end_an");

            // Chapter-end image (An's mirror of the bible image): An smiles; the reflection in the window does not.
            yield return Scene("C2_end_an", "Cái bóng trong kính", NarrativeTrigger.Manual, "", IsAn,
                    "An cười; Mận không cười theo. Trong tấm kính cửa sổ phía sau, cái bóng của An không cười. Tối đó An viết thư cho em út.")
                .Music("")
                .Narrate("Mận nhìn tôi cười. Em không cười theo.")
                .Pause(1200)
                .Backdrop("bg_kinh_cua_so_nha_chinh", 600)
                .Reflection(Portraits.AnStare)
                .Narrate("Trong tấm kính cửa sổ phía sau, cái bóng của tôi không cười.")
                .Pause(2400)
                .Backdrop("", 600)
                .Narrate("Ngoài chuồng, con heo chân trắng đẩy cái xô rỗng tới chân tôi. Tới giờ ăn tối.")
                .Narrate("Tôi cười với nó. Nó không cần biết tôi cười thật hay không. Nó cần cám.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Ba ơi, cái hộp nhận trước khi gửi hai phút. Chú Tám thử ba lần ★.”")
                .Narrate("Rồi một dòng nữa: “Khoa ơi, hôm nay cái bóng của chị trong kính không cười ☾.”")
                .Flag(ChapterDoneFlag);
        }
    }
}
