using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Village small talk (story bible Phần 2, 3, 9.3). Per villager: a first real talk (talk_&lt;id&gt;_meet), a pool of
    /// daily lines (one per day, <see cref="NarrativeTalk.TagDaily"/>), one repeat line, plus a few one-off incidental
    /// moments from 9.3. Hard rules kept here: talking never gives affinity, gold or items; nobody names the thing in the
    /// sky; villagers never say the number after six ("sáu với một", "đủ"); hard questions get a chore or "Mai trời nắng".
    /// NPCs are wary of Khoa first, and love An at once (then, later, notice that she remembers everything).
    /// </summary>
    public static class NarrativeContentVillage
    {
        const string N = Portraits.Neutral, H = Portraits.Happy, S = Portraits.Sad, A = Portraits.Angry, U = Portraits.Surprised,
            T = Portraits.Thinking, W = Portraits.Worried, L = Portraits.Laugh;
        const string Met = "STORY_FLAG prologue_done";
        const string IsAn = "PROTAGONIST An";
        const StageSide R = StageSide.Right;
        const string Khoa = CastIds.Khoa, An = CastIds.An;

        static string With(string extra) => string.IsNullOrEmpty(extra) ? Met : Met + ", " + extra;

        static SceneDefinition Meet(string npc) => new SceneDefinition
        {
            Id = "talk_" + npc + "_meet", Trigger = NarrativeTrigger.Talk, TriggerArg = npc,
            Condition = Met, Once = true, Priority = 10, Skippable = true,
        };

        static SceneDefinition Daily(string npc, int n, string extra = "") => new SceneDefinition
        {
            Id = "talk_" + npc + "_d" + n, Trigger = NarrativeTrigger.Talk, TriggerArg = npc,
            Condition = With(extra), Once = false, Tag = NarrativeTalk.TagDaily, Skippable = true,
        };

        static SceneDefinition Repeat(string npc, string portrait, string text) => new SceneDefinition
        {
            Id = "talk_" + npc + "_repeat", Trigger = NarrativeTrigger.Talk, TriggerArg = npc,
            Condition = Met, Once = false, Tag = NarrativeTalk.TagRepeat, Skippable = true,
        }.Say(npc, portrait, text).End();

        /// <summary>One-off story talk (bible 9.3 incidental moments). Priority below the first meeting.</summary>
        static SceneDefinition Moment(string npc, string name, string extra) => new SceneDefinition
        {
            Id = "talk_" + npc + "_" + name, Trigger = NarrativeTrigger.Talk, TriggerArg = npc,
            Condition = With(extra), Once = true, Priority = 5, Skippable = true,
        };

        /// <summary>The protagonist's line: Khoa (always neutral) or An. A null side is skipped on that route.</summary>
        static SceneDefinition Pc(this SceneDefinition s, string khoa, string an, string anPortrait = H)
        {
            string tag = "pc" + s.Commands.Count;
            s.IfGoto(IsAn, tag + "_an");
            if (khoa != null) s.Say(Khoa, N, khoa, R);
            s.Goto(tag + "_end").Label(tag + "_an");
            if (an != null) s.Say(An, anPortrait, an, R);
            return s.Label(tag + "_end");
        }

        static NarrativeOption Opt(string text, string label) => SceneDefinition.Opt(text, label);

        public static IEnumerable<SceneDefinition> Scenes()
        {
            foreach (var s in BaSau()) yield return s;
            foreach (var s in Man()) yield return s;
            foreach (var s in TamVa()) yield return s;
            foreach (var s in Dung()) yield return s;
            foreach (var s in Cuc()) yield return s;
            foreach (var s in Le()) yield return s;
            foreach (var s in AnhCa()) yield return s;
            foreach (var s in OngTon()) yield return s;
            foreach (var s in AnhTu()) yield return s;
            foreach (var s in BiNhan()) yield return s;
            foreach (var s in Mercenaries()) yield return s;
        }

        // ───────── Bà Sáu: quán cơm, dúi đồ ăn, trả lời câu khó bằng một việc vặt ─────────
        static IEnumerable<SceneDefinition> BaSau()
        {
            const string id = CastIds.BaSau;
            yield return Meet(id)
                .Narrate("Quán bà Sáu giờ trưa. Bàn nào cũng có một cái bát úp trước một cái ghế trống.")
                .IfGoto(IsAn, "an")
                .Say(id, N, "Cậu ngồi đi. Đừng ngồi ghế có bát úp.")
                .Say(Khoa, N, "Ghế đó có người?", R)
                .Say(id, H, "Ăn thêm miếng nữa đi con.")
                .Narrate("Bà xới thêm cơm vào bát tôi. Bát tôi chưa vơi miếng nào.")
                .End()
                .Label("an")
                .Say(id, H, "Con ngồi đây, gần nồi cơm cho ấm.")
                .Say(An, H, "Ơ, ghế bên cạnh có bát úp kìa bà. Có ai sắp tới hả bà?", R)
                .Say(id, H, "Ăn thêm miếng nữa đi con.")
                .Say(id, N, "Mai bà nấu canh chua. Con ghé.")
                .End();

            yield return Daily(id, 1)
                .Say(id, H, "Ăn thêm miếng nữa đi con. Ốm vầy heo nó coi thường.")
                .End();
            yield return Daily(id, 2)
                .Say(id, N, "Cái bát úp đó của quán. Đừng lật nha.")
                .Say(id, N, "Lật lên là {an:con|khoa:cậu} mời khách giùm bà.")
                .End();
            yield return Daily(id, 3)
                .Narrate("Nắp nồi lẩu chặn một cục gạch. Cục gạch hơi nhúc nhích.")
                .Say(id, N, "Nồi mua ở chợ huyện. Nồi chợ huyện nó hăng vậy đó.")
                .End();
            yield return Daily(id, 4)
                .Say(id, H, "Rảnh không? Xách giùm bà thùng nước.")
                .Choice("", 0, Opt("Xách giùm bà", "a"), Opt("Để lát nữa", "b"))
                .Label("a").Say(id, H, "Giỏi. Bữa hổm mẹ {an:con|khoa:cậu} cũng xách hai thùng một lượt.").End()
                .Label("b").Say(id, N, "Ừ. Thùng nước nó cũng không đi đâu.").End();
            yield return Daily(id, 5, "TIME 17 21, STORY_QUEST_DONE Q03")
                .Say(id, W, "Tối nay quán đóng sớm. Không có gì đâu. Tại bà buồn ngủ.")
                .Say(id, N, "{an:Con|khoa:Cậu} cũng về sớm. Đóng cửa, bật ti vi to lên.")
                .End();
            yield return Daily(id, 6, "WEATHER Mua GiongBao")
                .Say(id, N, "Mưa thì mưa. Mai trời nắng.")
                .Say(id, H, "Mà {an:con|khoa:cậu} cứ mang áo mưa theo cho chắc.")
                .End();
            yield return Daily(id, 7, "STORY_QUEST_DONE Q09")
                .Say(id, N, "Câu khó thì hỏi sau bữa cơm. Bụng đói thì câu trả lời cũng đói.")
                .End();

            yield return Repeat(id, H, "Mới ăn đó. Ăn thêm miếng nữa đi con.");

            // 9.3: "Nhà tôi còn mấy người?" → cả nhóm đồng thanh "Mai trời nắng." (rồi một việc vặt, 2.2)
            yield return Moment(id, "may_nguoi", "STORY_QUEST_DONE Q02, !STORY_QUEST_DONE Q09")
                .IfGoto(IsAn, "an")
                .Say(Khoa, N, "Nhà tôi còn mấy người?", R)
                .Narrate("Cả quán ngừng đũa cùng một lúc.")
                .Say(id, N, "Mai trời nắng.")
                .Narrate("Cả quán nói theo, không lệch một nhịp. Ngoài trời không có gì để dự báo.")
                .Say(id, N, "Xách giùm bà thùng cám đã.")
                .Narrate("Tôi xách xong quay lại. Bà đã đi chợ.")
                .End()
                .Label("an")
                .Say(An, H, "Bà ơi, nhà con giờ còn mấy người ha?", R)
                .Narrate("Cả quán ngừng đũa cùng một lúc.")
                .Say(id, N, "Mai trời nắng.")
                .Say(An, L, "Ơ, thật hả bà? Vậy mai con phơi chăn.", R)
                .Say(id, W, "Ừ. Xách giùm bà thùng cám đã.")
                .End();
        }

        // ───────── Mận: Nông Công, nói với heo như bạn cùng lớp, cây dù đen, bài ru ─────────
        static IEnumerable<SceneDefinition> Man()
        {
            const string id = CastIds.Man;
            yield return Meet(id)
                .Narrate("Mận ngồi xổm cạnh máng, cây dù đen gác trên vai dù trời không mưa.")
                .Say(id, N, "{an:Chị|khoa:Anh} tới trễ hai phút.")
                .IfGoto(IsAn, "an")
                .Say(Khoa, N, "Tôi không hẹn.", R)
                .Say(id, N, "Dạ. Em vẫn tính.")
                .Say(id, H, "Bạn này hôm qua chưa làm bài. Anh đừng cho bạn ăn trước.")
                .End()
                .Label("an")
                .Say(An, L, "Ơ, chị có hẹn em hả?", R)
                .Say(id, H, "Dạ không. Em tính trước cho chị luôn.")
                .Say(id, H, "Bạn này hôm qua chưa làm bài. Chị đừng cho bạn ăn trước nha.")
                .End();

            yield return Daily(id, 1)
                .Say(id, N, "Bạn này ăn xong không chịu rửa mặt. Em nói hoài.")
                .End();
            yield return Daily(id, 2)
                .Narrate("Mận vừa rải cám vừa ngân khẽ: “…nắng vàng trên đồng cỏ…”")
                .Say(id, T, "Bài ru thôi {an:chị|khoa:anh}. Ai dạy hả? Chắc ai đó trong xóm.")
                .End();
            yield return Daily(id, 3, "WEATHER Mua GiongBao NomAm")
                .Say(id, N, "Mưa thì em có dù. Nắng em cũng có dù.")
                .Say(id, N, "Dù để che. Che cái gì thì tùy hôm.")
                .End();
            yield return Daily(id, 4, "TIME 17 19")
                .Say(id, H, "Chiều nào đồng mình cũng đẹp vậy đó {an:chị|khoa:anh}.")
                .Say(id, N, "Đừng đứng nhìn lâu. Muỗi.")
                .End();
            yield return Daily(id, 5, "STORY_QUEST_DONE Q03")
                .Say(id, N, "Đếm heo hả {an:chị|khoa:anh}? Đếm chỗ trống nhanh hơn.")
                .Say(id, H, "Em đếm rồi. Đủ.")
                .End();
            yield return Daily(id, 6)
                .Say(id, T, "{an:Chị|khoa:Anh} thấy bạn nào trong chuồng hiền nhất?")
                .Choice("", 0, Opt("Con chân trắng", "a"), Opt("Con nào cũng hiền", "b"))
                .Label("a").Say(id, H, "Bạn đó giả chết giỏi chứ đâu có hiền. Em biết bạn lâu rồi.").End()
                .Label("b").Say(id, N, "Vậy là {an:chị|khoa:anh} chưa cho tụi nó ăn trễ lần nào.").End();
            // Khoa: Mận chờ anh cười rồi thôi chờ. An: thân như chị em, kể chuyện chưa kể với ai.
            yield return Daily(id, 7, "PROTAGONIST Khoa")
                .Say(id, H, "Hồi sáng có bạn ngồi lên máng, tưởng mình là cái nắp.")
                .Narrate("Mận nhìn tôi một lúc, như đang chờ gì đó.")
                .Say(id, N, "Thôi. Bữa khác em kể lại.")
                .End();
            yield return Daily(id, 8, IsAn)
                .Say(id, H, "Chị đừng kể ai nha. Hồi nhỏ em sợ con heo nái nhất trại.")
                .Say(id, N, "Giờ thì nó sợ em.")
                .End();

            yield return Repeat(id, H, "Lần này {an:chị|khoa:anh} tới sớm. Em chưa kịp tính.");
        }

        // ───────── Tám Vá: Kỹ Sư Xây Dựng, tên máy theo chiêu võ, gõ ba cái để nghe ─────────
        static IEnumerable<SceneDefinition> TamVa()
        {
            const string id = CastIds.TamVa;
            yield return Meet(id)
                .Narrate("Tám Vá gõ vào thân máy bơm ba cái, nghiêng tai nghe.")
                .Say(id, T, "Nghe chưa? Nó khỏe. Nó tên Thủy Long Ngâm.")
                .IfGoto(IsAn, "an")
                .Say(Khoa, N, "Máy bơm có tên?", R)
                .Say(id, N, "Cái gì chạy được thì phải có tên. Cháu cũng vậy.")
                .Say(id, N, "Hư gì thì kêu chú. Đừng tự gõ. Gõ sai nó giận.")
                .End()
                .Label("an")
                .Say(An, L, "Ơ, máy bơm cũng có tên hả chú? Dễ thương ghê.", R)
                .Say(id, H, "Dễ thương gì. Nó cắn tay chú hai lần rồi.")
                .Say(id, H, "Hư gì thì kêu chú. Đừng tự gõ. Gõ sai nó giận.")
                .End();

            yield return Daily(id, 1)
                .Narrate("Cốc. Cốc. Cốc.")
                .Say(id, N, "Gõ ba cái là biết. Cái thứ tư là để nó biết mình biết.")
                .End();
            yield return Daily(id, 2)
                .Say(id, H, "Cái xe rùa chú đặt tên Bình Sa Lạc Nhạn. Từ bữa đó nó chịu chạy thẳng.")
                .End();
            yield return Daily(id, 3, "STORY_QUEST_DONE Q03")
                .Say(id, N, "Biên bản đêm qua: thiệt hại do thời tiết. Ký đi cháu.")
                .Say(id, N, "Thời tiết nào hả? Thời tiết đêm qua.")
                .End();
            yield return Daily(id, 4, "WEATHER GiongBao Mua")
                .Say(id, T, "Sấm to vầy mà mái kho còn nằm yên. Mái kho có chí khí.")
                .End();
            yield return Daily(id, 5)
                .Say(id, H, "Cháu muốn nghe tên cái máy phát không?")
                .Choice("", 0, Opt("Nghe chứ", "a"), Opt("Để bữa khác", "b"))
                .Label("a").Say(id, H, "Phi Long Tại Thiên. Nó không bay. Tên là để nó cố.").End()
                .Label("b").Say(id, N, "Ừ. Nó cũng không thích khoe.").End();
            yield return Daily(id, 6, "STORY_QUEST_DONE Q06")
                .Say(id, N, "Cái hộp trong nhà chính hả? Nguồn dự phòng cũ.")
                .Say(id, N, "Nó gõ là nó khỏe. Cháu đừng gõ lại.")
                .End();
            // Khoa: dè chừng rồi tin như tin một cái cân. An: thương ngay, rồi lạnh gáy vì chị nhớ hết.
            yield return Daily(id, 7, "PROTAGONIST Khoa, STORY_QUEST_DONE Q06")
                .Say(id, N, "Biên bản bữa nay cháu ghi giùm chú. Cháu ghi đúng hơn chú.")
                .End();
            yield return Daily(id, 8, "PROTAGONIST An, STORY_QUEST_DONE Q06")
                .Say(An, H, "Chú ơi, Thủy Long Ngâm hết cắn tay chú chưa?", R)
                .Pause(600)
                .Say(id, T, "Chú nói cái tên đó có một lần thôi mà.")
                .Say(id, N, "…Hết rồi. Cháu nhớ dai thiệt.")
                .End();

            yield return Repeat(id, N, "Gõ rồi. Nó vẫn khỏe. Cháu cũng vậy.");

            // 9.3: sáng sau một đêm có ánh sáng trên đồng.
            yield return Moment(id, "chon_lan_hai", "STORY_QUEST_DONE Q03, MAJOR_WAVES_SURVIVED 2, TIME 6 11")
                .Pc("Đêm qua có thứ gì đó đánh dấu heo của mình.", "Chú ơi, đêm qua heo nhà con bị ai đánh dấu hay sao á. Lạ ghê.", W)
                .Narrate("Tám Vá không ngẩng lên.")
                .Say(id, N, "Ghi vào biên bản: chồn, lần hai.")
                .End();
            // 9.3: báo chi phí Lôi Pháo trước, rồi "đừng kể với tôi cái này tên là Tiết Kiệm".
            yield return Moment(id, "loi_phao", "BUILDINGS_CONSTRUCTED CuuTieuLoiPhao")
                .Say(id, T, "Cửu Tiêu Lôi Pháo. Chú báo phí trước: nó ăn điện như trâu ăn cỏ non.")
                .Pc("Nếu bắn vào mấy con nhỏ?", "Ơ, vậy bắn mấy con nhỏ cho vui được không chú?", L)
                .Say(id, A, "Được. Nhưng lần sau đừng kể với chú cái đó tên là Tiết Kiệm.")
                .End();
        }

        // ───────── Bác sĩ Dung: thú y, khô như ngói, ghét chữ "bị ám" ─────────
        static IEnumerable<SceneDefinition> Dung()
        {
            const string id = CastIds.Dung;
            yield return Meet(id)
                .Narrate("Bác sĩ Dung đang ghi hồ sơ, không ngẩng lên.")
                .Say(id, N, "Thú y. Heo bệnh thì gọi tôi. Heo buồn thì tự lo.")
                .IfGoto(IsAn, "an")
                .Say(Khoa, N, "Heo ở đây có bị gì lạ không?", R)
                .Say(id, A, "Không con nào bị ám. Có con bị giun.")
                .Say(id, N, "Cậu cần gì thì nói triệu chứng. Đừng kể chuyện.")
                .End()
                .Label("an")
                .Say(An, H, "Bác sĩ ơi, heo nhà con hay nằm ngửa lắm, có sao không ạ?", R)
                .Say(id, T, "Nằm ngửa là tư thế. Không phải chẩn đoán.")
                .Say(id, N, "Cô hỏi đúng chỗ. Hiếm đó.")
                .End();

            yield return Daily(id, 1)
                .Say(id, A, "Ai nói heo bị ám thì dẫn qua đây. Tôi khám người đó trước.")
                .End();
            yield return Daily(id, 2)
                .Say(id, N, "Hôm nay không có ca nào. Tôi đang khám cái ghế. Nó ổn.")
                .End();
            yield return Daily(id, 3, "WEATHER Mua NomAm")
                .Say(id, N, "Trời nồm. Ghẻ thích trời này. Thay ổ lót đi.")
                .End();
            yield return Daily(id, 4, "WEATHER RetDam")
                .Say(id, N, "Rét đậm. Heo non ủ ấm. Người lớn tự lo.")
                .End();
            yield return Daily(id, 5, "STORY_QUEST_DONE Q03")
                .Say(id, T, "Đêm hoảng thì sáng ăn kém. Cái đó tôi chữa được.")
                .Say(id, N, "Cái khác thì không phải việc của tôi.")
                .End();
            yield return Daily(id, 6)
                .Say(id, T, "{an:Cô|khoa:Cậu} có ngủ đủ không?")
                .Choice("", 0, Opt("Đủ", "a"), Opt("Không", "b"))
                .Label("a").Say(id, N, "Mắt {an:cô|khoa:cậu} nói khác. Mắt thường thật thà hơn miệng.").End()
                .Label("b").Say(id, N, "Ít ra {an:cô|khoa:cậu} thành thật. Heo cũng vậy.").End();
            yield return Daily(id, 7, "STORY_QUEST_DONE Q08")
                .Say(id, N, "Con heo dấu hỏi đó khỏe. Cái dấu hỏi là việc của Cúc.")
                .End();

            yield return Repeat(id, N, "Khám rồi. Không có gì mới. Có thì tôi báo.");

            // 9.3: Dung khám con heo giả chết.
            yield return Moment(id, "gia_chet", "STORY_QUEST_DONE Q01, HERD_HAS_WHITE_FOOT_PIG")
                .Pc("Con heo chân trắng lại nằm ngửa.", "Bác sĩ ơi, con chân trắng lại chết nữa rồi kìa!", L)
                .Narrate("Bác sĩ Dung ngồi xuống, nghe tim con heo rất lâu.")
                .Say(id, N, "Cứ đổ cám xuống. Nó sống lại thì mai vẫn phải đi cân.")
                .IfGoto("PROTAGONIST Khoa", "end")
                .Say(id, A, "Cô đừng vỗ tay. Nó diễn vì có khán giả.")
                .Label("end")
                .End();
        }

        // ───────── Cúc: Nhà Nghiên Cứu Gen, cựu nghiên cứu rau, chỉ vào chỗ trống, nói bằng số liệu ─────────
        static IEnumerable<SceneDefinition> Cuc()
        {
            const string id = CastIds.Cuc;
            yield return Meet(id)
                .Narrate("Cúc đang đếm luống rau sau nhà, ngón tay chỉ vào chỗ không có cây.")
                .Say(id, N, "Tôi từng nghiên cứu rau. Một cây cải thi qua cả thực tập sinh.")
                .Say(id, N, "Từ đó tôi chuyển sang heo.")
                .IfGoto(IsAn, "an")
                .Say(Khoa, N, "Cây cải thi gì?", R)
                .Say(id, T, "Nhận biết màu. Thực tập sinh sai hai câu. Cải sai một.")
                .End()
                .Label("an")
                .Say(An, L, "Ơ, thật hả chị? Cây cải giỏi vậy luôn?", R)
                .Say(id, H, "Thật. Có biên bản. Em là người đầu tiên tin ngay.")
                .End();

            yield return Daily(id, 1)
                .Say(id, T, "Rau mọc theo nắng. Heo đi theo con heo đi trước. Tôi đang đo cái sau.")
                .End();
            yield return Daily(id, 2)
                .Narrate("Cúc chỉ vào một khoảng đất trống.")
                .Say(id, T, "Chỗ này. Không có gì. Tôi ghi lại để mai so.")
                .End();
            yield return Daily(id, 3, "WEATHER NangGat")
                .Say(id, N, "Nắng gắt. Rau héo trước, heo cáu sau. Thứ tự đó chưa sai lần nào.")
                .End();
            yield return Daily(id, 4, "TIME 6 9")
                .Say(id, H, "Sáng sớm là lúc số liệu sạch nhất. Chưa ai kịp làm nó bẩn.")
                .End();
            yield return Daily(id, 5)
                .Say(id, T, "Em thích con heo nào nhất?")
                .Choice("", 0, Opt("Con mập nhất", "a"), Opt("Chưa biết", "b"))
                .Label("a").Say(id, N, "Mập là chỉ số. Thích là cảm xúc. Tôi chỉ đo được cái đầu.").End()
                .Label("b").Say(id, H, "Tốt. Chưa biết là trạng thái khoa học nhất.").End();
            yield return Daily(id, 6, "STORY_QUEST_DONE Q08")
                .Say(id, N, "Gen dấu hỏi không phải lỗi. Là số liệu chưa chịu khai.")
                .End();
            yield return Daily(id, 7, "STORY_QUEST_DONE Q11")
                .Say(id, W, "Em có để ý đàn hay đứng chừa ra một chỗ không?")
                .Say(id, N, "Thôi. Chưa đủ mẫu. Quên câu tôi vừa hỏi đi.")
                .End();

            yield return Repeat(id, N, "Em ghé lần hai. Tôi ghi vào cột “lặp lại”.");
        }

        // ───────── Lệ: Nhà Buôn, lanh, bán "bảo hiểm chồn", bán heo già như bán xe cũ ─────────
        static IEnumerable<SceneDefinition> Le()
        {
            const string id = CastIds.Le;
            yield return Meet(id)
                .Say(id, H, "Khách mới! Heo già, heo non, tin tức, bảo hiểm chồn. Em cần món nào?")
                .IfGoto(IsAn, "an")
                .Say(Khoa, N, "Bảo hiểm chồn là gì?", R)
                .Say(id, T, "Chồn tới thì chị buồn giùm em. Phí bằng một con heo.")
                .Say(Khoa, N, "Bồi thường?", R)
                .Say(id, N, "Chị buồn rất thật.")
                .End()
                .Label("an")
                .Say(An, L, "Ơ, bảo hiểm chồn hả chị? Chồn có ký hợp đồng không chị?", R)
                .Say(id, H, "Chồn thì không. Em thì nên.")
                .Say(id, H, "Em dễ thương. Chị chưa giảm giá, nhưng chị nhớ mặt em rồi.")
                .End();

            yield return Daily(id, 1)
                .Say(id, H, "Heo già là heo qua mấy đợt rồi, máy êm. Có thì bán chị, chị có mối.")
                .End();
            yield return Daily(id, 2)
                .Say(id, N, "Bảo hiểm chồn vẫn giá cũ. Chồn chưa báo tăng giá.")
                .End();
            yield return Daily(id, 3, "TIME 18 23")
                .Say(id, N, "Tối rồi, chị dọn sạp. Không phải vì gì. Tại khách về sớm.")
                .End();
            yield return Daily(id, 4, "WEATHER Mua GiongBao")
                .Say(id, A, "Mưa thì áo mưa lên giá. Heo thì không. Đời bất công.")
                .End();
            yield return Daily(id, 5)
                .Say(id, T, "Mua tin không em? Tin nóng.")
                .Choice("", 0, Opt("Tin gì vậy?", "a"), Opt("Thôi chị", "b"))
                .Label("a").Say(id, H, "Mai trời nắng. Tin này chị cho, vì ai cũng biết.").End()
                .Label("b").Say(id, N, "Khôn. Tin nóng hay phỏng tay.").End();
            yield return Daily(id, 6, "STORY_QUEST_DONE Q05")
                .Say(id, N, "Tin vụ trộm lớn chị bán rồi. Kẻ trộm cao bao nhiêu thì chị giữ lại.")
                .End();
            yield return Daily(id, 7, "PROTAGONIST An, STORY_QUEST_DONE Q06")
                .Say(id, H, "Giá chị báo hồi đầu mùa mà em còn nhớ đúng từng đồng hả?")
                .Say(id, N, "…Chị phải ghi sổ mới nhớ. Em thì khỏi cần.")
                .End();

            yield return Repeat(id, N, "Hôm nay chị báo giá cho em rồi. Báo lần hai là tính phí.");

            // 9.3: hỏi Lệ về nguồn hàng.
            yield return Moment(id, "nguon_hang", "STORY_QUEST_DONE Q04")
                .Pc("Hàng của chị lấy từ đâu?", "Chị Lệ ơi, hàng đẹp vậy chị lấy ở đâu ra hay vậy?")
                .Say(id, N, "Nguồn thì ở ngoài kia. Phí vận chuyển thì ở trong này.")
                .Say(id, T, "Chị nói cái gì tính được trước.")
                .End();
            // 9.3: Khoa lỡ miệng nói số đó ngoài chợ (bản Khoa; lần lỡ của An là cảnh truyện khác).
            yield return Moment(id, "sau_voi_mot", "PROTAGONIST Khoa, STORY_QUEST_DONE Q04")
                .Say(Khoa, N, "Lấy bảy ký cám.", R)
                .Pause(900)
                .Narrate("Cả chợ im lặng. Bà bán rau bên cạnh đặt bó rau xuống, rất nhẹ.")
                .Say(id, N, "Sáu với một ký, em.")
                .Say(Khoa, N, "Sáu với một ký.", R)
                .Say(id, H, "Ừ. Giờ chợ nói tiếp được rồi.")
                .End();
        }

        // ───────── Anh Ca: Quản Lý Ca, lịch mới mỗi sáng, không ai đọc đủ tên anh ─────────
        static IEnumerable<SceneDefinition> AnhCa()
        {
            const string id = CastIds.AnhCa;
            yield return Meet(id)
                .Narrate("Anh Ca đứng trước cửa quán, kẹp một xấp lịch còn thơm mực.")
                .Say(id, H, "Lịch hôm nay. Có ô cho em rồi.")
                .IfGoto(IsAn, "an")
                .Say(Khoa, N, "Anh tên đầy đủ là gì?", R)
                .Say(id, W, "Anh Ca. Đủ rồi. Gọi đủ tên thì lịch rối.")
                .Say(Khoa, N, "Ghi rồi.", R)
                .End()
                .Label("an")
                .Say(An, H, "Ơ, anh tên Ca thiệt hả anh?", R)
                .Say(id, W, "Gọi Ca thôi. Gọi đủ thì lịch rối.")
                .Say(id, H, "Em gọi gì anh cũng chịu. Trừ đủ tên.")
                .End();

            yield return Daily(id, 1, "TIME 6 10")
                .Say(id, H, "Lịch sáng nay mới in. Lịch hôm qua hết hạn lúc nửa đêm, em vứt đi.")
                .End();
            yield return Daily(id, 2)
                .Say(id, N, "Đồng hồ quán bà Sáu nghỉ trưa mười lăm phút. Anh xếp đó. Chưa trễ lần nào.")
                .End();
            yield return Daily(id, 3, "STORY_QUEST_DONE Q02")
                .Say(id, T, "Cột “ý kiến người sử dụng” hôm nay lại đầy. Toàn dấu móng.")
                .End();
            yield return Daily(id, 4)
                .Say(id, T, "Em muốn ca sáng hay ca chiều?")
                .Choice("", 0, Opt("Ca sáng", "a"), Opt("Ca nào cũng được", "b"))
                .Label("a").Say(id, H, "Ghi rồi. Ca sáng của em bắt đầu lúc em dậy. Anh linh động.").End()
                .Label("b").Say(id, W, "Câu đó nguy hiểm. Anh sẽ xếp em cả hai.").End();
            yield return Daily(id, 5, "STORY_QUEST_DONE Q06")
                .Say(id, T, "Lịch tuần này có một ô anh không xếp ai.")
                .Say(id, N, "Đừng hỏi. Ô đó tự lo.")
                .End();
            yield return Daily(id, 6, "WEATHER GiongBao")
                .Say(id, N, "Giông thì hoãn ca ngoài đồng. Lịch hoãn anh in sẵn từ tuần trước.")
                .End();

            yield return Repeat(id, N, "Lịch hôm nay em xem rồi. Lịch mai chưa in, đừng hỏi trước.");
        }

        // ───────── Ông Tồn: Thủ Kho, giữ hộp rỗng, ký nhận trước khi xuất ─────────
        static IEnumerable<SceneDefinition> OngTon()
        {
            const string id = CastIds.OngTon;
            yield return Meet(id)
                .Narrate("Kho ông Tồn: hộp đầy một bên, hộp rỗng một bên. Bên rỗng nhiều hơn.")
                .Say(id, N, "Ký trước. Xuất sau. Kho này vậy.")
                .IfGoto(IsAn, "an")
                .Say(Khoa, N, "Hộp rỗng giữ làm gì?", R)
                .Say(id, T, "Để đối chiếu. Hộp đầy thì ai cũng nhớ.")
                .Say(id, N, "Cậu ghi. Tôi cũng ghi. Hai sổ khớp nhau thì tốt.")
                .End()
                .Label("an")
                .Say(An, H, "Ông ơi, cho con xin một cái hộp rỗng đựng kẹo được không ông?", R)
                .Say(id, U, "Hộp rỗng thì không cho được, cháu. Nó là chứng từ.")
                .Say(id, N, "Cháu cần gì thì ký. Ký đẹp thì tôi xuất nhanh.")
                .End();

            yield return Daily(id, 1)
                .Say(id, N, "Hộp này rỗng từ năm ngoái. Vẫn đúng số. Rỗng cũng là một số.")
                .End();
            yield return Daily(id, 2)
                .Say(id, N, "Cứ ký nhận trước. Hàng nó đi sau. Hàng ở đây quen rồi.")
                .End();
            yield return Daily(id, 3, "TIME 17 21")
                .Say(id, N, "Kho đóng rồi. Chìa tôi giữ. Mai mở đúng giờ, không sớm hơn.")
                .End();
            yield return Daily(id, 4)
                .Say(id, T, "{an:Cháu|khoa:Cậu} đếm giùm tôi chồng hộp này.")
                .Choice("", 0, Opt("Đếm từng hộp", "a"), Opt("Đếm chỗ trống", "b"))
                .Label("a").Say(id, N, "Chậm. Nhưng đúng. Tôi ưa người chậm mà đúng.").End()
                .Label("b").Say(id, U, "Nhanh hơn. {an:Cháu|khoa:Cậu} học xóm này nhanh đấy.").End();
            yield return Daily(id, 5, "STORY_QUEST_DONE Q07")
                .Say(id, T, "Phiếu cũ tôi vẫn giữ. Ai hỏi thì tôi đưa. Chưa ai hỏi đúng câu.")
                .End();
            yield return Daily(id, 6, "WEATHER Mua NomAm")
                .Say(id, S, "Mưa thì kho ẩm. Giấy ẩm thì chữ ký loang. Tôi ký lại.")
                .End();
            yield return Daily(id, 7, "PROTAGONIST Khoa, STORY_QUEST_DONE Q07")
                .Say(id, N, "Cậu đếm rồi thì tôi khỏi đếm lại. Cái cân trong kho chưa được vậy.")
                .End();

            yield return Repeat(id, N, "Hôm nay {an:cháu|khoa:cậu} ký sổ khách rồi. Ký hai lần là tôi phải lập biên bản.");

            // 9.3: người chơi chạy tới cái tháp đã hết đạn.
            yield return Moment(id, "het_dan", "STORY_QUEST_DONE Q03")
                .Pc("Tháp ngoài rào hết đạn.", "Ông ơi, cái tháp ngoài rào hết đạn rồi. Kho mình còn không ông?", W)
                .Say(id, T, "Sổ ghi xuất đủ. Sổ ghi bắn hết.")
                .Say(id, N, "Tôi ghi hết là hết. Mực trong sổ chưa bắn hộ ai bao giờ.")
                .End();
        }

        // ───────── Anh Tư: xe ôm, phương tiện công cộng duy nhất, chạy "theo giờ anh Tư" ─────────
        static IEnumerable<SceneDefinition> AnhTu()
        {
            const string id = CastIds.AnhTu;
            yield return Meet(id)
                .Narrate("Anh Tư ngồi trên yên xe, nón kéo sụp, không có vẻ gì đang chờ khách.")
                .Say(id, H, "Đi huyện không? Không đi cũng được. Anh ngồi đây theo giờ anh.")
                .IfGoto(IsAn, "an")
                .Say(Khoa, N, "Mấy giờ xe chạy?", R)
                .Say(id, N, "Lúc anh chạy.")
                .Say(Khoa, N, "Ghi rồi.", R)
                .End()
                .Label("an")
                .Say(An, H, "Anh Tư ơi, kẹo bữa trước anh ăn chưa?", R)
                .Say(id, H, "Để dành. Kẹo người ta cho thì phải ăn lúc buồn.")
                .Say(id, N, "Chừng nào muốn về thì gọi. Anh tới. Theo giờ anh.")
                .End();

            yield return Daily(id, 1)
                .Say(id, N, "Hôm nay anh chạy hai chuyến. Chuyến thứ hai chưa quyết đi đâu.")
                .End();
            yield return Daily(id, 2)
                .Say(id, N, "Xe anh không có giờ. Xe anh có tâm trạng.")
                .End();
            yield return Daily(id, 3, "WEATHER Mua GiongBao")
                .Say(id, N, "Mưa thì anh nghỉ. Nắng anh cũng nghỉ một chút. Cho công bằng.")
                .End();
            yield return Daily(id, 4, "TIME 6 9")
                .Say(id, N, "Sớm vầy anh chưa chạy. Xe nó còn ngủ.")
                .End();
            yield return Daily(id, 5)
                .Say(id, H, "Đi huyện không em?")
                .Choice("", 0, Opt("Chưa", "a"), Opt("Đi", "b"))
                .Label("a").Say(id, H, "Ừ. Ở đây chưa biết cũng là một ngày trong tuần.").End()
                .Label("b").Say(id, N, "Ừ. Bữa khác. Hôm nay anh đi đám giỗ xã bên.").End();
            yield return Daily(id, 6)
                .Say(id, H, "Bộ đồ đám cưới bữa đó anh mặc thêm mấy bữa nữa. Cho đáng tiền may.")
                .End();

            yield return Repeat(id, N, "Anh vẫn ngồi đây. Chưa tới giờ anh.");
        }

        // ───────── Bí nhân: người buôn sau rào, câu ngắn, không nhận vàng, không nhận đồ cũ ─────────
        static IEnumerable<SceneDefinition> BiNhan()
        {
            const string id = CastIds.BiNhan;
            yield return Meet(id)
                .Narrate("Sau rào có một người đứng. Không đèn, không sạp, không biển.")
                .Say(id, N, "Không nhận vàng.")
                .Pc("Nhận gì?", "Ơ, vậy mình nhận gì ạ?", T)
                .Say(id, N, "Thứ chưa ai dùng.")
                .Say(id, N, "Hỏi đúng giá thì có hàng.")
                .End();

            yield return Daily(id, 1)
                .Say(id, N, "Hàng mới. Chưa ai dùng. Không ai nên dùng.")
                .End();
            yield return Daily(id, 2)
                .Say(id, N, "Hỏi đúng thì trả đúng. Hỏi sai thì trả lại câu hỏi.")
                .End();
            yield return Daily(id, 3, "TIME 18 23")
                .Say(id, N, "Đêm nay yên. Yên cũng có giá.")
                .End();
            yield return Daily(id, 4)
                .Say(id, N, "Vàng nặng. Tôi đi đường xa.")
                .End();
            yield return Daily(id, 5, "STORY_QUEST_DONE Q03")
                .Say(id, N, "Đêm đồng sáng, tôi không bán.")
                .Say(id, N, "Tôi đứng xem.")
                .End();

            yield return Repeat(id, N, "Một ngày, một lần. Lần hai tính giá.");
        }

        // ───────── Lính đánh thuê chợ đêm: nhận việc, nhận tiền, không gọi tên thứ phải đánh ─────────
        static IEnumerable<SceneDefinition> Mercenaries()
        {
            const string doId = CastIds.Do, mien = CastIds.Mien, chot = CastIds.Chot, tuTroi = CastIds.TuTroi, van = CastIds.Van;

            yield return Daily(doId, 1).Say(doId, N, "Cổng rộng nhiêu? Tôi đo rồi. Vừa tôi với cái khiên.").End();
            yield return Daily(doId, 2).Say(doId, H, "Cổng hẹp thì tôi chặn bằng khiên. Cổng rộng thì chặn bằng cầu nguyện.").End();
            yield return Repeat(doId, N, "Đo rồi. Cổng không đổi từ sáng.");

            yield return Daily(mien, 1).Say(mien, N, "Gió đông. Nhẹ. Bắn được.").End();
            yield return Daily(mien, 2).Say(mien, T, "Gió đêm qua đổi hướng hai lần. Gió không đổi hướng vô cớ.").End();
            yield return Repeat(mien, N, "Gió vẫn vậy.");

            yield return Daily(chot, 1).Say(chot, H, "Mấy cái bẫy hết lượt là đồng nghiệp đang nghỉ phép. Ai cũng cần nghỉ.").End();
            yield return Daily(chot, 2).Say(chot, N, "Bẫy bên trái lười. Tôi báo cáo rồi, nó chưa phản hồi.").End();
            yield return Repeat(chot, N, "Tôi đang họp với mấy cái bẫy. Lát nữa.");

            yield return Daily(tuTroi, 1).Say(tuTroi, T, "Săn chim lớn. Nó lớn. Thế thôi.").End();
            yield return Daily(tuTroi, 2).Say(tuTroi, N, "Tôi không tả nó. Tả rồi nó biết mình đang được tả.").End();
            yield return Repeat(tuTroi, N, "Trời vẫn đó. Chim chưa về.");
            // 9.3: Tư Trời nhận việc "săn chim lớn" (lần nói chuyện đầu).
            yield return Moment(tuTroi, "chim_gi", "")
                .Say(tuTroi, N, "Việc của tôi là săn chim lớn.")
                .Say(tuTroi, T, "Chủ trại muốn săn chim gì?")
                .Narrate("Tôi chỉ lên trời.")
                .Say(tuTroi, N, "Tôi hỏi loại. Trời thì tôi biết rồi.")
                .End();

            yield return Daily(van, 1).Say(van, A, "Tôi đi được trong bóng tối. Không có nghĩa là tôi đi giao bưu phẩm.").End();
            yield return Daily(van, 2).Say(van, N, "Đêm nay tôi canh phía rào tối. Phía sáng để người khác.").End();
            yield return Repeat(van, A, "Không. Tôi không cầm thư giùm ai hết.");
        }
    }
}
