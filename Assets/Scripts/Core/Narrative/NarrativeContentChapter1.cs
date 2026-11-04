using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Chương 1 — "Chồn" (Q01–Q05), both routes. Canon: story bible 2.3–2.7 (luật hài/bí ẩn, mặt Khoa, hai góc nhìn),
    /// Phần 5 / 5B rows Q01–Q05. Khoa goes by confrontation and writes in front of people; An lets people talk, never
    /// opens her notebook in public and writes letters at night (★ ai cũng kiểm được, ☾ chỉ An nghe ra).
    /// Teach-by-doing: a beat asks for one action and ends on a tutorial hint; the matching QuestSignal plays the next
    /// beat. Rewards are paid by StoryQuestBook; these scenes only set flags, open notebook pages and show hints.
    /// Village rules kept: nobody names the thing in the sky, no villager says the number, a silent beat (no music)
    /// follows every mystery and life comes back through a pig, and nothing jokes about losses (Q05 is outcome-neutral).
    /// Flow: C1_arrive → Q01 beats (feed → water → fence → recall) → Q01_done ⇒ Q02_start → Q02_board → Q02_done ⇒
    /// Q02_night; next DayStart Q03_start → Q03_herd → Q03_done (night of the wave); next DayStart Q03_morning (⇒ Q04_start
    /// when the farm is level 3, else a later DayStart) → Q04_done; next DayStart Q05_start → Q05_done; next DayStart C1_end.
    /// </summary>
    public static class NarrativeContentChapter1
    {
        const string K = CastIds.Khoa, A = CastIds.An, Man = CastIds.Man, Tam = CastIds.TamVa, Le = CastIds.Le,
            Ca = CastIds.AnhCa, Sau = CastIds.BaSau;
        const string N = Portraits.Neutral, H = Portraits.Happy, L = Portraits.Laugh, U = Portraits.Surprised,
            T = Portraits.Thinking, Sd = Portraits.Sad, W = Portraits.Worried;
        const string IsKhoa = "PROTAGONIST Khoa", IsAn = "PROTAGONIST An";
        const StageSide R = StageSide.Right;

        /// <summary>Flag set when the chapter-end image has played (Chapter 2 can wait on it).</summary>
        public const string ChapterDoneFlag = "chapter1_done";

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

        public static IEnumerable<SceneDefinition> Scenes()
        {
            foreach (var s in Khoa()) yield return s;
            foreach (var s in An()) yield return s;
        }

        // =====================================================================================================
        // Khoa — lấy cương: hỏi thẳng, ghi sổ trước mặt người ta, giọng biên bản, mặt không đổi.
        // =====================================================================================================
        static IEnumerable<SceneDefinition> Khoa()
        {
            const string Q01On = "STORY_QUEST_ACTIVE Q01,STORY_FLAG prologue_done," + IsKhoa;

            // ---- Chapter card + Q01 first beat (first control: move, then interact)
            yield return Scene("C1_arrive_khoa", "Chương 1 — Chồn", NarrativeTrigger.EnterLocation, "Farm",
                    "STORY_FLAG prologue_done,STORY_QUEST_ACTIVE Q01," + IsKhoa,
                    "Khoa nhận trại. Mận dặn: cám trước, nước sau, rồi coi rào. Bao cám để cạnh máng.", priority: 10)
                .Music("")
                .Banner("Chương 1 — Chồn", 2200)
                .Narrate("“Ở đây có rất nhiều chồn. Chồn ở đây bay được.”")
                .PlaceAt("player", "farm:spawn", CardinalDirection.North)
                .PlaceAt(Man, "farm:station:main_house", CardinalDirection.South)
                .FadeIn(600)
                .Say(Man, N, "Heo dậy từ sớm. Tụi nó chờ anh.", R)
                .Say(Man, N, "Ông Năm dạy: cám trước, nước sau, rồi coi rào.", R)
                .Say(K, N, "Để rồi tính.")
                .Say(Man, H, "Không để được đâu anh. Tụi nó đói thiệt. Bao cám cạnh máng, anh ra đứng sát rồi chạm vô.", R)
                .Hint("move")
                .Hint("interact");

            yield return Scene("Q01_fed_khoa", "Cám", NarrativeTrigger.Signal, Sig(QuestSignal.FeedPoured), Q01On,
                    "Cả đàn tới máng cùng lúc. Mận bảo giờ thay nước ở bồn kế bên.")
                .Narrate("Cám rơi xuống máng. Cả đàn tới cùng lúc. Không con nào xếp hàng.")
                .Say(Man, N, "Giờ tới nước. Bồn kế bên máng, anh chạm vô là thay.", R)
                .Hint("water");

            yield return Scene("Q01_watered_khoa", "Rào hở", NarrativeTrigger.Signal, Sig(QuestSignal.WaterRefilled), Q01On,
                    "Đoạn rào phía đồng hở. Mận gọi là chồn. Chặn tạm trước, sửa hẳn phải đợi chú Tám.")
                .Say(Man, Sd, "Đoạn rào phía đồng hở một khúc. Tối chồn vô là mất heo.", R)
                .Say(K, N, "Chồn.")
                .Say(Man, N, "Chồn.", R)
                .Say(Man, N, "Anh chặn rào tạm trước đi. Sửa hẳn thì phải đợi chú Tám.", R)
                .Hint("interact");

            yield return Scene("Q01_patched_khoa", "Chỗ trống", NarrativeTrigger.Signal, Sig(QuestSignal.FenceTempPatched), Q01On,
                    "Mận đếm chỗ trống và thấy thiếu một con. Khoa thổi tù và gọi heo lạc về.")
                .Say(Man, U, "Ơ, chỗ cạnh cột thứ ba trống. Thiếu một con rồi anh.", R)
                .Say(K, N, "Cô đếm chỗ trống.")
                .Say(Man, H, "Nhanh hơn mà anh. Anh thổi tù và đi, nghe tiếng là nó về.", R)
                .Narrate("Nhanh hơn thật. Tôi không thích việc nó nhanh hơn thật.")
                .Hint("recall");

            yield return Scene("Q01_done_khoa", "Cậu tới nhận trại", NarrativeTrigger.QuestCompleted, "Q01", IsKhoa,
                    "Con heo chân trắng lại giả chết. Mận kể: phòng bếp lò bị ông Năm khóa, chìa gửi kho ông Tồn.", priority: 10)
                .Narrate("Dưới máng, con heo chân trắng lại nằm ngửa giả chết. Nghe tiếng xô cám, nó lật dậy.")
                .Narrate("Mận cười. Tôi ghi sổ. Ở trại này chỉ tôi với nó có hồ sơ bệnh án. Của nó ghi “không bệnh”.")
                .Say(K, N, "Phòng bếp lò có ổ khóa mới.")
                .Say(Man, N, "Ông Năm khóa từ hồi em còn nhỏ. Chìa gửi kho ông Tồn. Bữa anh lên, bà Sáu phải sang mượn.", R)
                .Say(Man, T, "Mà phòng đó có gì đâu anh. Cái lò với cái chai.", R)
                .Narrate("Cột phải: “Chìa: kho ông Tồn.” Hồ sơ bàn giao vẫn ghi người nhận: “(sáu với một)”.")
                .Notebook("Q01")
                .Then("Q02_start_khoa");

            // ---- Q02 Máng ít heo nhiều
            yield return Scene("Q02_start_khoa", "Máng ít heo nhiều", NarrativeTrigger.QuestCompleted, "Q01",
                    "STORY_QUEST_ACTIVE Q02," + IsKhoa,
                    "Anh Ca lên trại với lịch mới. Phải đọc bảng sức chứa trước khi bàn chuyện dùng chung máng.")
                .PlaceAt(Ca, "farm:station:capacity_board", CardinalDirection.South)
                .Say(Ca, H, "Chào cậu. Lịch hôm nay của tôi có một dòng: lên trại cậu.", R)
                .Narrate("Anh Ca có một lịch mới mỗi sáng. Lịch hôm nay có tên tôi. Chưa ai hỏi ý tôi.")
                .Say(Ca, T, "Heo nhiều, máng ít. Cậu đọc bảng sức chứa đã. Số nào thấp nhất là chỗ đang nghẽn.", R)
                .CameraTo("farm:station:capacity_board", 600)
                .Hint("interact");

            yield return Scene("Q02_board_khoa", "Bảng sức chứa", NarrativeTrigger.Signal, Sig(QuestSignal.CapacityBoardRead),
                    "STORY_QUEST_ACTIVE Q02," + IsKhoa,
                    "Trại nuôi được bao nhiêu là tính theo chỗ thiếu nhất. Anh Ca nhờ bổ sung cả cám lẫn nước.")
                .Say(Ca, N, "Trại nuôi được bao nhiêu là tính theo chỗ thiếu nhất. Không tính theo chỗ đủ nhất.", R)
                .Narrate("Tôi ghi: “Sức chứa = chỗ thiếu nhất.” Áp dụng được cho nhiều thứ ngoài heo.")
                .Say(Ca, H, "Giờ cậu bổ sung cám với nước. Cả hai. Tôi đứng đây ghi.", R)
                .Hint("feed");

            yield return Scene("Q02_done_khoa", "Ý kiến người sử dụng", NarrativeTrigger.QuestCompleted, "Q02", IsKhoa,
                    "Một con heo đè lên kế hoạch dùng chung máng. Bản vẽ cũ của trại có một “lối khách”.", priority: 10)
                .Say(Ca, H, "Kế hoạch dùng chung máng. Mỗi con một khung giờ. Tôi kẻ tay cả đêm.", R)
                .Narrate("Một con heo húc bản kế hoạch xuống bùn, rồi nằm đè lên. Rất thong thả.")
                .Say(Ca, T, "Tôi đề xuất thêm một cột: “ý kiến người sử dụng”.", R)
                .Narrate("Tôi đồng ý. Người sử dụng đã cho ý kiến rất rõ.")
                .Narrate("Bản vẽ cũ của trại có một lối đi vào khoảnh sân không máng. Ghi chú: “lối khách”. Tôi không hỏi khách nào.")
                .Notebook("Q02")
                .Then("Q02_night_khoa");

            yield return Scene("Q02_night_khoa", "Vạch bút chì", NarrativeTrigger.Manual, "", IsKhoa,
                    "Đêm, Khoa so giấy khám sức khỏe với các vạch 2019–2025 trong hốc: vạch nào cũng khớp chiều cao thật.")
                .FadeOut(600)
                .Music("")
                .FadeIn(600)
                .Narrate("Tối đó tôi lấy giấy khám sức khỏe của công ty ra, so với các vạch bút chì trong hốc.")
                .Narrate("2019 đến 2025. Vạch nào cũng khớp chiều cao thật của tôi năm đó. Lệch chưa tới nửa phân.")
                .Narrate("Cột phải: “Người kẻ biết chiều cao của tôi, từng năm.” Lề: “Hoặc cao đúng bằng tôi.”")
                .Pause(1600)
                .Narrate("Ngoài chuồng, con heo vẫn nằm trên bản kế hoạch. Nó ngáy. Nó ngủ ngon hơn tôi.");

            // ---- Q03 Gió lốc hướng lên
            yield return Scene("Q03_start_khoa", "Gió lốc hướng lên", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q03," + IsKhoa,
                    "Cả xóm đóng thêm then cửa. Mận dặn: trời trở gió thì lùa heo vô Nơi Trú. Khoa huýt sáo gọi đàn.", priority: 18)
                .Narrate("Sáng. Nhà nào trong xóm cũng đang đóng thêm then cửa. Không ai nói vì sao.")
                .Say(Man, N, "Mấy bữa nay trời trở. Có gió lớn là anh lùa heo vô Nơi Trú liền nha.", R)
                .Say(K, N, "Gió lớn cỡ nào.")
                .CameraTo("farm:station:herd_shelter", 600)
                .Say(Man, N, "Cỡ… gió lốc. Anh huýt sáo thử coi, tụi nó theo anh liền.", R)
                .Hint("whistle");

            yield return Scene("Q03_herd_khoa", "Nơi Trú", NarrativeTrigger.Signal, Sig(QuestSignal.HerdRecalled),
                    "STORY_QUEST_ACTIVE Q03," + IsKhoa,
                    "Đàn vào Nơi Trú. Mận dặn: có gì lẻn qua rào thì né trước, đừng đứng chắn.")
                .Narrate("Đàn theo tôi vào Nơi Trú. Không con nào hỏi vì sao.")
                .Say(Man, N, "Lỡ có gì lẻn qua rào thì anh né trước. Đừng đứng chắn.", R)
                .Hint("dodge");

            // Mystery beat: monologue cut to one or two short lines, no music, no joke (bible 2.3, 2.5 note 5).
            yield return Scene("Q03_done_khoa", "Đêm", NarrativeTrigger.QuestCompleted, "Q03", IsKhoa,
                    "Đêm đợt lớn đầu tiên: một vật sáng lơ lửng trên đồng. Cả xóm tắt đèn. Khoa ghi: “Tôi đang thấy.”", priority: 10)
                .Music("")
                .Narrate("Đêm.")
                .Narrate("Trên đồng có một vật sáng lơ lửng.")
                .Narrate("Cả xóm tắt đèn.")
                .Narrate("Tôi chỉ ghi được một dòng: “Tôi đang thấy.”")
                .Notebook("Q03");

            yield return Scene("Q03_morning_khoa", "Thấy rào", NarrativeTrigger.DayStart, "", "STORY_QUEST_DONE Q03," + IsKhoa,
                    "Sáng sau đêm có ánh sáng, xóm lên vá rào. Mận chỉ “thấy rào”. Chú Tám ghi: gió lốc, hướng lên. " +
                    "Ở vành ngoài có phiếu thu hoạch, chữ ký bảy nét.", priority: 20)
                .Music("")
                .PlaceAt(Man, "farm:spawn", CardinalDirection.East)
                .PlaceAt(Tam, "farm:station:warehouse", CardinalDirection.South)
                .Narrate("Sáng. Mận và chú Tám lên vá rào từ sớm. Không ai hỏi chuyện đêm qua.")
                .Say(K, N, "Cô có thấy không?")
                .Say(Man, N, "Thấy rào.", R)
                .Say(Tam, T, "Biên bản: thiệt hại do gió lốc. Cường độ: mạnh. Hướng: lên. Cậu ký đây.", R)
                .Narrate("Ai cũng ký. Tôi ký. Chữ ký của tôi chậm, như đang chép lại.")
                .Narrate("Ở mép vành ngoài, chỗ đêm qua có thứ đi vào, có một tờ phiếu thu hoạch nằm sẵn.")
                .Narrate("Người nhận đã ký. Địa chỉ để trống. Chữ ký có bảy nét, giống hệt chữ ký trên phiếu thu suất mộ.")
                .Narrate("Cột trái: “Vật sáng. Hút heo.” Cột phải: “Phiếu thu hoạch.” Thứ đầu tiên ở trại vào được cột phải.")
                .Pause(1800)
                .Narrate("Con heo chân trắng tới húc vào ống quần tôi. Nó muốn ăn sáng. Nó không quan tâm cột nào.")
                .Flag("c1_q03_morning_done")
                .IfGoto("!PLAYER_FARM_LEVEL 3", "end")
                .IfGoto("!STORY_QUEST_ACTIVE Q04", "end")
                .Then("Q04_start_khoa")
                .Label("end");

            // ---- Q04 Chợ đêm không thối tiền (opens at farm level 3)
            yield return Scene("Q04_start_khoa", "Chợ đêm không thối tiền", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q04,PLAYER_FARM_LEVEL 3,SCENE_SEEN Q03_morning_khoa," + IsKhoa,
                    "Lệ báo chợ đêm đã tiếp trại: không thối tiền, đổi món lấy món. Lệ chào bán bảo hiểm chồn.", priority: 15)
                .PlaceAt(Le, "farm:station:main_house", CardinalDirection.South)
                .Say(Le, H, "Trại cậu lớn rồi ha. Vậy là chợ đêm tiếp cậu rồi đó.", R)
                .Say(Le, T, "Nhớ luật: chợ đêm không thối tiền. Đọc kỹ ô hàng muốn nhận gì, đổi món lấy món.", R)
                .Say(Le, H, "Tiện thì mua bảo hiểm chồn. Giá bằng một con heo. Chồn loại nào cũng bồi thường.", R)
                .Narrate("Tôi không mua. Tôi ghi lại mức giá, phòng khi chồn lên giá.")
                .Hint("market");

            yield return Scene("Q04_done_khoa", "Heo biết hát", NarrativeTrigger.QuestCompleted, "Q04", IsKhoa,
                    "Người bán chỉ nhận “heo biết hát”. Ở quầy Lệ, Khoa đọc trộm danh sách heo già “đã quen đường về”.",
                    priority: 10, location: "")
                .Narrate("Người bán treo biển: “Chỉ nhận heo biết hát.” Tôi hỏi heo hát thế nào. Ông bảo heo nào hoảng là hát.")
                .Narrate("Giao dịch xong. Hôm nay không con nào phải hát.")
                .Narrate("Tôi đọc trộm tờ danh sách Lệ để quên trên quầy: mua heo già. Cạnh mỗi con: “đã quen đường về”.")
                .Say(K, N, "Đường về đâu.")
                .Say(Le, N, "Đường về chuồng chứ đâu.", R)
                .Narrate("Lệ cất tờ giấy đi, nhanh hơn mức cần thiết cho một con đường về chuồng.")
                .Notebook("Q04");

            // ---- Q05 Bố của cái rìu
            yield return Scene("Q05_start_khoa", "Bố của cái rìu", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q05," + IsKhoa,
                    "Lệ tặng “tin độc quyền”: sẽ có rìu. Chú Tám dặn dồn nó vô cửa hẹp, đỡ một nhát rồi đánh trả.", priority: 18)
                .Say(Le, H, "Tin độc quyền: vụ trộm lớn nhất mùa này. Lần đầu chị tặng, lần sau tính tiền.", R)
                .Narrate("Lệ nhất quyết không nói kẻ trộm cao bao nhiêu. Tờ tin ghi đúng một dòng: “Sẽ có rìu.”")
                .Say(Tam, N, "Biên bản tôi chỉ ghi “một cái rìu”. Chữ “Chúa” là con Lệ thêm cho bán được giá.", R)
                .Say(Tam, T, "Nó vô thì dồn nó vô cửa hẹp. Thấy rìu giơ lên thì đỡ, rồi đánh trả.", R)
                .Hint("counter");

            // After the boss wave, win or lose (bible: both count) — no line states the outcome.
            yield return Scene("Q05_done_khoa", "Phiếu giao hàng", NarrativeTrigger.QuestCompleted, "Q05", IsKhoa,
                    "Sau trận rìu, Khoa nhặt ở mép rào một tờ phiếu giao hàng vẽ đúng ranh giới trại.", priority: 10)
                .Music("")
                .Narrate("Đợt đã qua. Chú Tám đi một vòng rào, gõ từng cọc ba cái.")
                .Narrate("Ở mép rào, tôi nhặt được một tờ phiếu giao hàng.")
                .Narrate("Trên phiếu vẽ ranh giới trại. Từng góc rào, đúng tới từng cọc.")
                .Narrate("Cột phải: “Phiếu giao hàng. Vẽ đúng trại. Người nhận: đã ký.”")
                .Notebook("Q05");

            // Chapter-end image: Bà Sáu folds the signature away. Silence, then a pig.
            yield return Scene("C1_end_khoa", "Hóa đơn in sai", NarrativeTrigger.DayStart, "", "STORY_QUEST_DONE Q05," + IsKhoa,
                    "Bà Sáu mang cháo lên. Khoa đặt phiếu giao hàng lên bàn; bà gấp vội phần chữ ký rồi đưa lại.", priority: 20)
                .PlaceAt(Sau, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Sáng. Bà Sáu mang một nồi cháo lên trại. Sau mỗi đợt, xóm này nấu cháo.")
                .Narrate("Tôi đặt tờ phiếu giao hàng lên bàn, cạnh nồi cháo.")
                .Say(K, N, "Phiếu này nằm ở mép rào. Vẽ đúng trại.")
                .Music("")
                .Narrate("Bà Sáu nhìn tờ phiếu. Rồi gấp vội phần chữ ký vào trong.")
                .Narrate("Bà đưa lại cho tôi, như đưa một tờ hóa đơn bị in sai.")
                .Narrate("Tôi đứng yên, chờ bà nói tiếp.")
                .Say(Sau, N, "Ăn thêm miếng nữa đi con.", R)
                .Pause(2000)
                .Narrate("Ngoài sân, con heo chân trắng lật úp cái xô, rồi nằm ngửa bên cạnh. Giả chết, đúng giờ ăn.")
                .Flag(ChapterDoneFlag);
        }

        // =====================================================================================================
        // An — lấy nhu: hỏi ngây ngô, khen đúng chỗ, để người ta tự nói; nghe ra trước rồi mới nhìn; viết thư ban đêm.
        // =====================================================================================================
        static IEnumerable<SceneDefinition> An()
        {
            const string Q01On = "STORY_QUEST_ACTIVE Q01,STORY_FLAG prologue_done," + IsAn;

            yield return Scene("C1_arrive_an", "Chương 1 — Chồn", NarrativeTrigger.EnterLocation, "Farm",
                    "STORY_FLAG prologue_done,STORY_QUEST_ACTIVE Q01," + IsAn,
                    "An nhận trại. Mận dặn: cám trước, nước sau, rồi coi rào. Bao cám để cạnh máng.", priority: 10)
                .Music("")
                .Banner("Chương 1 — Chồn", 2200)
                .Narrate("“Nhà mình ơi, ở đây nhiều chồn lắm. Chồn ở đây bay được luôn :)”")
                .PlaceAt("player", "farm:spawn", CardinalDirection.North)
                .PlaceAt(Man, "farm:station:main_house", CardinalDirection.South)
                .FadeIn(600)
                .Say(Man, H, "Chị! Heo dậy hết rồi, đứng chờ chị kìa.", R)
                .Say(A, L, "Ơ, thật hả? Mới ngày đầu mà chị có người hâm mộ rồi.")
                .Say(Man, N, "Ông Năm dạy: cám trước, nước sau, rồi coi rào.", R)
                .Say(A, H, "Dạ, cô giáo Mận. Cám ở đâu cô?")
                .Say(Man, H, "Bao cám cạnh máng á chị. Chị ra đứng sát rồi chạm vô.", R)
                .Hint("move")
                .Hint("interact");

            yield return Scene("Q01_fed_an", "Cám", NarrativeTrigger.Signal, Sig(QuestSignal.FeedPoured), Q01On,
                    "Đàn heo im re khi An đổ cám. Mận bảo giờ thay nước ở bồn kế bên.")
                .Narrate("Cám rào rào xuống máng. Tiếng ủn ỉn dồn về một chỗ, rồi đều lại như nồi cơm sôi.")
                .Say(Man, U, "Chị đổ cám mà tụi nó im re. Với em là tụi nó cãi nhau hoài.", R)
                .Say(A, L, "Tại chị hối lộ đó.")
                .Say(Man, H, "Giờ thay nước nha chị. Bồn kế bên máng.", R)
                .Hint("water");

            yield return Scene("Q01_watered_an", "Rào hở", NarrativeTrigger.Signal, Sig(QuestSignal.WaterRefilled), Q01On,
                    "Đoạn rào phía đồng hở. Hỏi chồn to cỡ nào, Mận bảo “cỡ chồn”. Chặn rào tạm trước.")
                .Say(Man, Sd, "Đoạn rào phía đồng hở một khúc. Tối chồn vô đó chị.", R)
                .Say(A, U, "Ơ, thật hả? Chồn ở đây to cỡ nào em?")
                .Say(Man, N, "…Cỡ chồn.", R)
                .Say(Man, N, "Chị chặn rào tạm trước đi. Sửa hẳn phải đợi chú Tám.", R)
                .Hint("interact");

            yield return Scene("Q01_patched_an", "Chỗ trống", NarrativeTrigger.Signal, Sig(QuestSignal.FenceTempPatched), Q01On,
                    "Mận đếm chỗ trống, thấy thiếu một con. An thổi tù và gọi heo lạc về.")
                .Say(Man, U, "Ơ, chỗ cạnh cột thứ ba trống. Thiếu một con rồi chị.", R)
                .Say(A, H, "Em đếm chỗ trống hả? Hay ghê, dạy chị với.")
                .Say(Man, H, "Nhanh hơn mà chị. Chị thổi tù và đi, nghe tiếng là nó về.", R)
                .Narrate("Nhanh thật. Tôi cười, rồi đếm lại theo cách của tôi: từng con một.")
                .Hint("recall");

            yield return Scene("Q01_done_an", "Cháu tới nhận trại", NarrativeTrigger.QuestCompleted, "Q01", IsAn,
                    "Con heo chân trắng lại giả chết, An vỗ tay khen. Hỏi đùa chuyện ma, Mận tự kể: chìa phòng bếp lò ở kho ông Tồn.",
                    priority: 10)
                .Narrate("Tiếng xô cám vừa chạm thành máng, con heo chân trắng đang nằm ngửa đã lật dậy.")
                .Say(A, L, "Trời ơi, diễn sâu quá! Cho một tràng pháo tay nè.")
                .Say(A, U, "Mà Mận nè, phòng bếp lò có ma không em? Ổ khóa mới tinh luôn.")
                .Say(Man, H, "Ma gì chị! Ông Năm khóa từ hồi em còn nhỏ xíu. Chìa gửi kho ông Tồn đó.", R)
                .Say(Man, N, "Bữa chị lên, bà Sáu phải lội sang tận nơi mượn. Mà trong đó có gì đâu. Cái lò với cái chai.", R)
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Ba ơi, phòng ba từng ở, chìa gửi ở kho ông Tồn ★ :)”")
                .Notebook("Q01")
                .Then("Q02_start_an");

            // ---- Q02
            yield return Scene("Q02_start_an", "Máng ít heo nhiều", NarrativeTrigger.QuestCompleted, "Q01",
                    "STORY_QUEST_ACTIVE Q02," + IsAn,
                    "Anh Ca lên trại với lịch mới. Phải đọc bảng sức chứa trước khi bàn chuyện dùng chung máng.")
                .PlaceAt(Ca, "farm:station:capacity_board", CardinalDirection.South)
                .Say(A, H, "Anh Ca! Lịch hôm nay của anh có tên em hả?")
                .Say(Ca, H, "Có. Dòng thứ ba. Sau “tưới cây”, trước “đi họp”.", R)
                .Say(Ca, T, "Heo nhiều, máng ít. Em đọc bảng sức chứa giùm tôi đã. Số nào thấp nhất là chỗ nghẽn.", R)
                .CameraTo("farm:station:capacity_board", 600)
                .Hint("interact");

            yield return Scene("Q02_board_an", "Bảng sức chứa", NarrativeTrigger.Signal, Sig(QuestSignal.CapacityBoardRead),
                    "STORY_QUEST_ACTIVE Q02," + IsAn,
                    "Bảng chỉ ra chỗ thiếu nhất. An muốn đứng nhìn đàn để biết con nào chịu thiệt; rồi bổ sung cám và nước.")
                .Say(Ca, N, "Thấy chưa? Trại nuôi được bao nhiêu là tính theo chỗ thiếu nhất.", R)
                .Say(A, T, "Dạ. Mà bảng đâu có nói con nào đang chịu thiệt.")
                .Say(A, H, "Để em đứng nhìn tụi nó một chút. Heo hay kể lể lắm.")
                .Say(Ca, W, "Nhìn xong thì bổ sung cám với nước. Cả hai. Tôi ghi lịch rồi.", R)
                .Hint("observe");

            yield return Scene("Q02_done_an", "Ý kiến của heo", NarrativeTrigger.QuestCompleted, "Q02", IsAn,
                    "An khen kế hoạch của anh Ca và đề nghị thêm cột “ý kiến của heo”. Một con heo cho ý kiến đầu tiên.", priority: 10)
                .Say(Ca, H, "Kế hoạch dùng chung máng. Mỗi con một khung giờ. Tôi kẻ tay cả đêm.", R)
                .Say(A, H, "Kỹ quá anh ơi! Mà thêm một cột “ý kiến của heo” nữa là hết chê.")
                .Narrate("Anh Ca đỏ mặt, ghi thật. Một con heo húc bản kế hoạch xuống bùn, rồi nằm đè lên.")
                .Say(A, L, "Đó, ý kiến đầu tiên. Ủng hộ nhiệt liệt luôn.")
                .Notebook("Q02")
                .Then("Q02_night_an");

            yield return Scene("Q02_night_an", "Vạch bút chì", NarrativeTrigger.Manual, "", IsAn,
                    "Đêm, một mình, An so giấy khám sức khỏe với các vạch bút chì trong hốc: vạch nào cũng đúng chiều cao năm đó.")
                .FadeOut(600)
                .Music("")
                .FadeIn(600)
                .Narrate("Đêm đó, chờ cả trại ngủ, tôi mới lấy giấy khám sức khỏe ra so với các vạch chì trong hốc.")
                .Narrate("Tôi viết: “Mẹ ơi, vạch nào cũng đúng chiều cao con năm đó ★.”")
                .Narrate("Ngoài lề, bút chì: “Người kẻ biết An cao bao nhiêu. Hoặc cao đúng bằng An.”")
                .Pause(1600)
                .Narrate("Ngoài chuồng, con heo vẫn nằm trên bản kế hoạch, ngáy đều. Tôi nghe một lúc mới ngủ được.");

            // ---- Q03
            yield return Scene("Q03_start_an", "Gió lốc hướng lên", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q03," + IsAn,
                    "Cả xóm đóng thêm then cửa. Mận dặn: trời trở gió thì thổi tù và lùa heo vô Nơi Trú.", priority: 18)
                .Narrate("Sáng. Khắp xóm vang tiếng búa. Nhà nào cũng đóng thêm then cửa, vừa đóng vừa nói chuyện thời tiết.")
                .Say(Man, N, "Mấy bữa nay trời trở. Có gió lớn là chị lùa heo vô Nơi Trú liền nha.", R)
                .Say(A, U, "Ơ, thật hả? Gió lớn cỡ nào em?")
                .CameraTo("farm:station:herd_shelter", 600)
                .Say(Man, N, "Cỡ… gió lốc. Chị thổi tù và, tụi nó về liền.", R)
                .Hint("recall");

            yield return Scene("Q03_herd_an", "Nơi Trú", NarrativeTrigger.Signal, Sig(QuestSignal.HerdRecalled),
                    "STORY_QUEST_ACTIVE Q03," + IsAn,
                    "Đàn vào Nơi Trú. Mận dặn: có gì lẻn qua rào thì né trước, đừng đứng chắn.")
                .Say(Man, N, "Lỡ có gì lẻn qua rào thì chị né trước nha. Đừng đứng chắn.", R)
                .Say(A, L, "Né thì chị giỏi lắm. Hồi nhỏ chị né rửa chén suốt.")
                .Hint("dodge");

            yield return Scene("Q03_done_an", "Đêm", NarrativeTrigger.QuestCompleted, "Q03", IsAn,
                    "Đêm đợt lớn đầu tiên: An tắt đèn như cả xóm, áp tai vào cửa. Tiếng heo dồn lại rồi im bặt.", priority: 10)
                .Music("")
                .Narrate("Đêm. Tôi tắt đèn như cả xóm, rồi áp tai vào cửa.")
                .Narrate("Ngoài đồng, tiếng heo kêu dồn về một chỗ.")
                .Narrate("Rồi im bặt. Như có ai nhấc cả nhóm lên khỏi mặt đất.")
                .Narrate("Tôi viết: “Nhà mình ơi, An đang nghe. Cả xóm đang không nghe. Cả hai đều đang cố ☾.”")
                .Notebook("Q03");

            // An's one mask slip of the chapter: a flat line while patching the fence; Mận looks one beat longer.
            yield return Scene("Q03_morning_an", "Gió lốc to ghê", NarrativeTrigger.DayStart, "", "STORY_QUEST_DONE Q03," + IsAn,
                    "Vá rào, An than to về gió lốc rồi nghe xem ai ngừng tay: Mận ngừng, chú Tám thì không. " +
                    "Ở vành ngoài có phiếu thu hoạch, chữ ký bảy nét.", priority: 20)
                .Music("")
                .PlaceAt(Man, "farm:spawn", CardinalDirection.East)
                .PlaceAt(Tam, "farm:station:warehouse", CardinalDirection.South)
                .Narrate("Sáng. Mận với chú Tám lên vá rào từ sớm. Không ai nhắc chuyện đêm qua.")
                .Say(A, L, "Gió lốc đêm qua ghê ha! Nhà em có bay mái không Mận?")
                .Narrate("Tôi vừa đóng đinh vừa nghe. Tiếng búa của Mận ngừng một nhịp. Tiếng búa của chú Tám thì không.")
                .Say(Man, N, "…Không chị. Mái nhà em nặng.", R)
                .Say(Tam, T, "Biên bản: thiệt hại do gió lốc. Cường độ: mạnh. Hướng: lên. Cháu ký đây.", R)
                .Say(A, Portraits.AnSilentTilt, "Hướng lên.")
                .Narrate("Mận nhìn tôi lâu thêm một nhịp. Tôi cười tiếp, rồi ký.")
                .Narrate("Ở mép vành ngoài có một tờ phiếu thu hoạch nằm sẵn. Người nhận đã ký, địa chỉ để trống.")
                .Narrate("Chữ ký có bảy nét. Giống hệt chữ ký trên phiếu thu suất mộ.")
                .Pause(1800)
                .Narrate("Con heo chân trắng tới gặm dây giày tôi. Nó đòi ăn sáng, đúng giờ hơn cả xóm.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, Mận ngừng tay. Chú Tám thì không, chú quen rồi ☾.”")
                .Narrate("Ngoài lề, không gửi ai: “Lúc nói “hướng lên”, mình quên cười. Mận nghe ra.”")
                .Flag("c1_q03_morning_done")
                .IfGoto("!PLAYER_FARM_LEVEL 3", "end")
                .IfGoto("!STORY_QUEST_ACTIVE Q04", "end")
                .Then("Q04_start_an")
                .Label("end");

            // ---- Q04
            yield return Scene("Q04_start_an", "Chợ đêm không thối tiền", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q04,PLAYER_FARM_LEVEL 3,SCENE_SEEN Q03_morning_an," + IsAn,
                    "Lệ báo chợ đêm đã tiếp trại: không thối tiền, đổi món lấy món. Lệ chào bán bảo hiểm chồn.", priority: 15)
                .PlaceAt(Le, "farm:station:main_house", CardinalDirection.South)
                .Say(Le, H, "Trại em lớn rồi ha. Vậy là chợ đêm tiếp em rồi đó.", R)
                .Say(A, U, "Ơ, thật hả? Chợ đêm có bán trà sữa không chị?")
                .Say(Le, T, "Không thối tiền, không trà sữa. Đọc kỹ ô hàng muốn nhận gì, đổi món lấy món.", R)
                .Say(Le, H, "Mà mua bảo hiểm chồn không em? Giá bằng một con heo. Chồn loại nào cũng bồi thường.", R)
                .Hint("market");

            yield return Scene("Q04_done_an", "Heo biết hát", NarrativeTrigger.QuestCompleted, "Q04", IsAn,
                    "An mặc cả với người bán “chỉ nhận heo biết hát”. Khen chữ Lệ đẹp, An được đưa cả xấp giấy, " +
                    "trong đó có danh sách heo già “đã quen đường về”.", priority: 10, location: "")
                .Narrate("Biển ghi: “Chỉ nhận heo biết hát.” Tôi mặc cả với ông như mặc cả mớ rau. Ông cười khà khà.")
                .Say(A, H, "Chị Lệ cho em mượn cây bút. Chữ chị đẹp ghê, chị luyện ở đâu vậy?")
                .Narrate("Lệ đưa luôn cả xấp giấy. Tờ dưới cùng: danh sách mua heo già, con nào cũng “đã quen đường về”.")
                .Narrate("Tôi trả lại cả xấp. Không hỏi gì.")
                .Say(Le, H, "À… đường về chuồng thôi em.", R)
                .Narrate("Lệ cất tờ giấy đi, nhanh hơn mức cần thiết cho một con đường về chuồng.")
                .Narrate("Tối đó tôi viết: “Nhà mình ơi, chị Lệ tự kể chuyện đường về chuồng. Không ai hỏi hết ★ :)”")
                .Notebook("Q04");

            // ---- Q05
            yield return Scene("Q05_start_an", "Bố của cái rìu", NarrativeTrigger.DayStart, "",
                    "STORY_QUEST_ACTIVE Q05," + IsAn,
                    "Lệ tặng “tin độc quyền”: sẽ có rìu, và tự khai chữ “Chúa” là chị thêm. Chú Tám dặn đỡ rồi đánh trả.",
                    priority: 18)
                .Say(Le, H, "Tin độc quyền nè: “Sẽ có rìu.” Vụ trộm lớn nhất mùa này. Lần đầu chị tặng em.", R)
                .Say(A, L, "Trời, tin hay quá chị! Rìu cỡ nào chị?")
                .Say(Le, T, "Nói nhỏ nghe: chữ “Chúa” là chị thêm cho bán được giá. Biên bản chú Tám chỉ ghi “một cái rìu”.", R)
                .Say(Tam, T, "Nó vô thì dồn nó vô cửa hẹp. Thấy rìu giơ lên thì đỡ, rồi đánh trả.", R)
                .Hint("counter");

            yield return Scene("Q05_done_an", "Phiếu giao hàng", NarrativeTrigger.QuestCompleted, "Q05", IsAn,
                    "Sau trận rìu, An nghe tiếng giấy ở mép rào: một tờ phiếu giao hàng vẽ đúng ranh giới trại.", priority: 10)
                .Music("")
                .Narrate("Đợt đã qua. Tôi đi một vòng rào, nghe từng cọc.")
                .Narrate("Ở mép rào có tiếng giấy sột soạt. Một tờ phiếu giao hàng, vướng vào cọc.")
                .Narrate("Trên phiếu vẽ ranh giới trại, từng góc rào. Có cả cái cọc tôi đang vịn.")
                .Narrate("Tối đó tôi viết: “Ba ơi, bản sao phiếu ★.”")
                .Notebook("Q05");

            yield return Scene("C1_end_an", "Gấp lại cho phẳng", NarrativeTrigger.DayStart, "", "STORY_QUEST_DONE Q05," + IsAn,
                    "Bà Sáu mang cháo lên, thấy tờ phiếu và gấp vội phần chữ ký. An giả như không thấy, xin gấp giùm cho phẳng.",
                    priority: 20)
                .PlaceAt(Sau, "farm:station:main_house", CardinalDirection.South)
                .Narrate("Sáng. Bà Sáu mang một nồi cháo lên trại. Tờ phiếu giao hàng tôi để trên bàn, chưa kịp cất.")
                .Music("")
                .Narrate("Bà Sáu thấy nó trước tôi. Bà gấp vội phần chữ ký vào trong.")
                .Narrate("Tôi không nhìn bà. Tôi múc cháo.")
                .Say(A, H, "Phiếu in sai hả bà? Để con gấp lại giùm cho phẳng.")
                .Narrate("Bà đưa. Tờ giấy sột soạt trong tay bà lâu hơn cần thiết.")
                .Say(Sau, N, "Ăn thêm miếng nữa đi con.", R)
                .Pause(2000)
                .Narrate("Ngoài sân, con heo chân trắng lật úp cái xô, rồi nằm ngửa bên cạnh. Giả chết, đúng giờ ăn.")
                .FadeOut(500)
                .FadeIn(500)
                .Narrate("Ngoài lề, bút chì: “Tay bà Sáu run khi đưa.”")
                .Flag(ChapterDoneFlag);
        }
    }
}
