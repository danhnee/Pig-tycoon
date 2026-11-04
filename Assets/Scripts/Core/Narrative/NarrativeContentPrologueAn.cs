using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Chương 0 bản An (story bible Phần 1B, 1B.0–1B.16 = P01–P08). Ngôi "tôi" là An: nghe trước rồi mới nhìn,
    /// không bao giờ đối chất, viết thư cho người nhà trong sổ hồng (★ ai cũng kiểm được, ☾ chỉ An nhận ra, bút chì = đoán).
    /// Ngày mất đứng riêng trên phông tĩnh "bg_ngay_mat", không nhạc; ở đó An thôi cười (không dùng chân dung An).
    /// Ở Bờ Rìa không ai nói con số ("sáu với một") và không ai gọi tên thứ trên trời.
    /// Chân dung ẩn của An chỉ dùng một lần trong chương: lần lỡ "Chị kể chuyện này ba lần rồi. Chị mệt." (P08_an_b).
    /// Mọi lựa chọn có mặc định là nhánh đúng, để bỏ qua (Skip) luôn chạy hết cảnh và áp đủ hiệu ứng.
    /// </summary>
    public static class NarrativeContentPrologueAn
    {
        /// <summary>Phông nền (CG) các cảnh ngoài bản đồ chơi được. Tên trùng bản Khoa cho những chỗ dùng chung.</summary>
        public static readonly string[] Backdrops =
        {
            "bg_vi_giay_chung_nhan",   // ví An: giấy chứng nhận, dấu đỏ, ảnh thẻ cười rất tươi
            "bg_nha_cu_bep_2018",      // (chung) bếp và bàn ăn nhà cũ, nồi cơm, quạt, nồi lẩu
            "bg_chua",                 // (chung) sân chùa, cây sứ, bậc đá
            "bg_bau_cua_so",           // (chung) chiếu nghỉ, bậu cửa sổ, cái chai nước mắm sau tấm rèm
            "bg_nha_cu_cau_thang",     // (chung) cầu thang gỗ nhà cũ
            "bg_hem_nha_cu",           // con hẻm trước cổng: mái tôn nhà bên, bậc cổng, vệt phượng
            "bg_goc_pho_cuoi",         // góc phố cuối, sáng nắng, con hẻm bên cạnh
            "bg_ngay_mat",             // phông tĩnh cho từng ngày mất (tờ lịch trắng, không trang trí)
            "bg_benh_vien",            // (chung) hành lang / phòng bệnh viện
            "bg_nha_cu_nha_tam",       // nhà tắm nhà cũ mùa nồm, vòi nước
            "bg_nha_cu_hien",          // hiên nhà đêm cúp điện, quạt tay, giếng sau nhà
            "bg_phong_ngu_an_cu",      // phòng ngủ cũ của An, khăn chườm
            "bg_tiem_tap_hoa",         // tiệm tạp hóa đầu hẻm, vỉ sticker
            "bg_phong_tro",            // phòng trọ của An, không có bình hoa
            "bg_phong_tro_guong",      // (chung) gương phòng tắm nhà trọ
            "bg_nga_tu_den_do",        // ngã tư, vạch qua đường, sáng 24/09/2026
            "bg_phong_benh_giac_mo",   // phòng bệnh trong giấc mơ: rèm xanh, sáu chiếc ghế
            "bg_nghia_trang",          // (chung) nghĩa trang, khoảnh đất trống cuối hàng
            "bg_nha_cu_phong_khach",   // phòng khách nhà cũ: đồng hồ 7:02, ảnh Tết 2018, bàn của cha
            "bg_tranh_gia_dinh_em",    // bức tranh "Gia đình em" bản gốc, mặt sau bảy chữ ký
            "bg_hop_thiec",            // hộp bánh quy thiếc mở: sổ bìa vải, ảnh ở trại, mảnh da
            "bg_manh_da_mat_sau",      // mặt sau mảnh da: mực bi xanh, chữ tròn của An
            "bg_xe_buyt",              // (chung) hàng ghế cuối xe khách
            "bg_bien_bo_ria",          // tấm biển BỜ RÌA giữa ruộng, dòng viết tay
            "bg_dong_sau_rao",         // cánh đồng sau hàng rào lúc chạng vạng, một chỗ cỏ đứng yên
            "bg_trai_chang_vang",      // cổng trại ông Năm lúc chạng vạng, máng heo, nhà kho
            "bg_phong_bep_lo",         // phòng cạnh bếp lò ở trại, thành lò ám muội
            "bg_hoc_tuong_bay_cot",    // hốc tường trát vôi, bảy cột vạch chiều cao
            "bg_cong_trai",            // cổng trại ban chiều, xe máy anh Tư
        };

        public static IEnumerable<SceneDefinition> Scenes()
        {
            yield return P01();
            yield return P02();
            yield return P03();
            yield return P04a();
            yield return P04b();
            yield return P05a();
            yield return P05b();
            yield return P05c();
            yield return P06a();
            yield return P06b();
            yield return P06c();
            yield return P07a();
            yield return P07b();
            yield return P08a();
            yield return P08b();
            yield return P08c();
        }

        // ---- shorthands (private: không đụng file nội dung khác) ----
        const string A = CastIds.An;
        const string Player = "player";
        const string Neu = Portraits.Neutral, Hap = Portraits.Happy, Lau = Portraits.Laugh, Sad = Portraits.Sad,
            Sur = Portraits.Surprised, Thi = Portraits.Thinking, Wor = Portraits.Worried;
        const string Village = "Hub_Village";
        const string SilentMusic = "";

        static SceneDefinition NewScene(string id, string title, string recap, string location = "",
            NarrativeTrigger trigger = NarrativeTrigger.Manual)
            => new SceneDefinition
            {
                Id = id, Title = title, Trigger = trigger, Condition = "PROTAGONIST An", Once = true,
                Skippable = true, Recap = recap, Location = location, Priority = trigger == NarrativeTrigger.NewGame ? 100 : 0,
            };

        static NarrativeOption O(string text, string label, params NarrativeEffect[] fx) => SceneDefinition.Opt(text, label, fx);
        static NarrativeOption OIf(string text, string label, string condition, string lockedReason = "")
            => new NarrativeOption { Text = text, GotoLabel = label, Condition = condition, ShowWhenLocked = lockedReason != "", LockedReason = lockedReason };
        static NarrativeEffect SetF(string flag) => SceneDefinition.Fx(NarrativeEffectKind.SetFlag, flag);
        static SceneDefinition Note(SceneDefinition s, string entry) => s.Do(NarrativeEffectKind.Custom, NarrativeCustomIds.NotebookPrefix + entry);

        /// <summary>Một ngày mất: phông tĩnh, tắt nhạc, ngày tháng đứng riêng, rồi một câu sự việc. Không đùa, không chân dung.</summary>
        static SceneDefinition DeathDate(SceneDefinition s, string date, string fact)
            => s.Music(SilentMusic).Backdrop("bg_ngay_mat", 900).Banner(date, 2600).Narrate(fact).Pause(1500);

        // =====================================================================================
        // P01 — Một nhà rất bình thường (1B.0, 1B.1)
        // =====================================================================================
        static SceneDefinition P01()
        {
            var s = NewScene("P01_an", "Một nhà rất bình thường",
                "An tự giới thiệu: có giấy chứng nhận nhưng không ai tin vì chị cười nhiều quá. Nhà ở cuối hẻm đông người; "
                + "chị học từ mẹ cách né một câu hỏi bằng một câu trả lời dễ thương.",
                trigger: NarrativeTrigger.NewGame);
            s.Music(SilentMusic).Backdrop("", 0).Banner("Chương 0 — Giấy chứng nhận", 2400)
             .Narrate("Tôi có giấy chứng nhận. Chuyện gì tôi kể cũng nghe vui. Xin đọc kèm giấy.")
             .Pause(900)
             .Backdrop("bg_vi_giay_chung_nhan", 900)
             .Narrate("Tôi tên An. Tôi điên. Có giấy hẳn hoi nhé: chẩn đoán, chữ ký bác sĩ, dấu đỏ bệnh viện.")
             .Narrate("Không ai tin tờ giấy đó, vì tôi cười nhiều quá. Người ta tin cái mặt hơn. Thế cũng tiện.")
             .Pause(700)
             .Narrate("Tôi không giỏi nhìn. Tôi giỏi nghe: ai ngừng nửa nhịp trước khi trả lời, ai cười trễ hơn câu đùa.")
             .Narrate("Thứ lạ đến với tôi bằng tai trước, rồi mới tới mắt.")
             .Narrate("Tôi có một cuốn sổ bìa hồng dán sticker con thỏ. Toàn thư ngắn gửi người nhà, thư nào cũng có mặt cười.")
             .Narrate("Không ai trong số họ đọc được nữa, nên tôi viết thoải mái.")
             .Pause(600)
             .Narrate("Chuyện gì tôi kể cũng nghe vui. Bạn cứ để ý chỗ nào tôi thôi cười là được.")
             .FadeOut(700)
             .Backdrop("bg_nha_cu_bep_2018", 0).Music("bgm_nha_cu_am").FadeIn(900)
             .Narrate("Nhà tôi ở cuối hẻm, ba tầng, cầu thang gỗ kêu cót két như đang mách lẻo.")
             .Narrate("Ông nội Lâm nói mình ăn ít. Chỉ tôi đếm được ông xới mấy lần; tôi không mách, ông phần tôi cơm cháy.")
             .Sfx("sfx_chen_dua")
             .Narrate("Bà nội Sương đi chùa mỗi sáng. Cha Hậu sửa máy. Mẹ Thủy gọt táo không đứt vỏ.")
             .Narrate("Mẹ bắt tôi ngồi gần nồi cơm: “Con bé này ăn chậm mà hay ngồi nói.”")
             .Narrate("Anh Minh hơn tôi hai tuổi. Trưa hè, cái quạt chỉ quay về phía anh.")
             .Do(NarrativeEffectKind.Custom, NarrativeCustomIds.TutorialPrefix + "interact")
             // Vòng chọn: than nóng đủ ba lần thì anh tự xoay quạt. Giành thì An không nỡ. Mặc định = than.
             .Label("quat")
             .Choice("Trưa nóng quá. Làm sao có quạt?", 0,
                 O("Than nóng một tiếng", "than"),
                 O("Giành cái quạt", "gianh"))
             .Label("gianh")
             .Narrate("Tay tôi chạm tới nút quạt rồi rụt lại. Giành thì anh cáu. Tôi chưa giành quạt với anh lần nào.")
             .Goto("quat")
             .Label("than")
             .IfGoto("STORY_FLAG an_p01_than_2", "than_3")
             .IfGoto("STORY_FLAG an_p01_than_1", "than_2")
             .Flag("an_p01_than_1")
             .Say(A, Wor, "Nóng quá à…")
             .Narrate("Anh Minh lật trang truyện. Chưa đâu. Phải đủ ba lần.")
             .Goto("quat")
             .Label("than_2")
             .Flag("an_p01_than_2")
             .Say(A, Sad, "Nóng muốn tan luôn á anh…")
             .Narrate("Anh liếc qua. Một lần nữa.")
             .Goto("quat")
             .Label("than_3")
             .Say(A, Sad, "Em chảy thành nước luôn rồi nè…")
             .Sfx("sfx_quat_xoay")
             .Narrate("Anh Minh không nói gì, tự xoay quạt về phía tôi. Đúng ba lần. Lần nào cũng vậy.")
             .Pause(500)
             .Narrate("Bé Khoa, em út, kém tôi sáu tuổi, tháng nào cũng bắt cả nhà ký làm chứng chiều cao trên khung cửa.")
             .Narrate("Tôi luôn ký kèm một mặt cười. Nó là đứa duy nhất trong nhà không cười lại khi tôi cười.")
             .Narrate("Nó chỉ nhìn tôi, như đang kiểm tra cái mặt cười ấy là thật hay giả.")
             .Pause(700)
             .Backdrop("bg_chua")
             .Narrate("Hồi nhỏ tôi hay theo bà đi chùa, nhặt hoa sứ rụng về cắm vào cái chai nước mắm trên bậu cửa sổ.")
             .Backdrop("bg_bau_cua_so")
             .Narrate("Cái chai ở chiếu nghỉ cầu thang. Lớn lên thì thôi. Không ai còn nhớ nó đi đâu.")
             .Backdrop("bg_nha_cu_bep_2018")
             .Narrate("Ai hỏi nhà có mấy người, mẹ luôn trả lời bằng tên từng người, không bao giờ bằng một con số.")
             .Narrate("Tôi học được cách ấy từ bé: muốn khỏi trả lời một câu, cứ trả lời một câu khác thật dễ thương.")
             .Pause(500)
             .Narrate("Hè nào cả nhà cũng lên “trại của bác Năm”. Ông bảo đường lên đó “phải đi bằng trí nhớ”.")
             .Say(A, Lau, "Đi bằng trí nhớ thì con lạc mất ông ơi!")
             .Narrate("Tôi cười. Ông không cười. Hồi đó tôi chưa biết phải ghi cái đó vào đâu.")
             .FadeOut(800)
             .Then("P02_an");
            return s;
        }

        // =====================================================================================
        // P02 — Những ngày may mắn (1B.2, đầu 1B.3)
        // =====================================================================================
        static SceneDefinition P02()
        {
            var s = NewScene("P02_an", "Những ngày may mắn",
                "Ông trúng vé số đuôi 24; mưa bóng mây dừng ở bậc cổng; cành hoa sứ bảy bông hiện trên bậu cửa sổ. Ông mất 17/07/2018. "
                + "Bà tìm lại nhẫn cưới, cánh phượng dồn về cổng, cái chai lạnh buốt; bà hẹn An bảy giờ sáng ở chùa.");
            s.Backdrop("bg_nha_cu_bep_2018", 0).Music("bgm_nha_cu_am").FadeIn(700)
             .Banner("Tháng Bảy, 2018")
             .Narrate("Tháng Bảy năm 2018, ông trúng giải khuyến khích. Tờ vé đuôi 24.")
             .Say(A, Lau, "Ông dán lên tủ thờ đi, cho cụ cố khoe với hàng xóm dưới đó!")
             .Backdrop("bg_hem_nha_cu").Sfx("sfx_mua_mai_ton")
             .Narrate("Ba hôm sau, mưa bóng mây giữa trưa. Mái tôn nhà bên mưa rào rào. Mái nhà mình im bặt.")
             .Narrate("Tiếng mưa dừng đúng ở bậc cổng, như có ai lấy kéo cắt ngang. Tôi không chạy ra xem. Tôi gọi vọng vào.")
             .Say(A, Hap, "Ông ơi, nhà mình có mưa riêng nè!")
             .Pause(600)
             .Narrate("Rồi tôi lắng nghe xem tiếng nào im đi. Cả nhà vẫn cười nói. Chỉ có tiếng quạt nan của ông dừng lại.")
             .Narrate("Ông ra cửa, đứng lâu hơn một người già đứng ngắm mưa, rồi bảo: “Mưa con nít.”")
             .Narrate("Hàng xóm nói câu đó xong thì cười. Ông thì không.")
             .Narrate("Ông gọi tôi vào rửa nồi lẩu. Tôi đi ngay, vừa đi vừa hát. Đã có thêm một người nghe ra.")
             .Pause(700)
             .Music(SilentMusic).Backdrop("bg_nha_cu_cau_thang")
             .Narrate("Tối đó, mùi hoa sứ tới trước. Tôi lần theo mùi lên cầu thang.")
             .Backdrop("bg_bau_cua_so")
             .Narrate("Cái chai trên bậu cửa sổ cắm một cành hoa sứ trắng. Bảy bông. Sáu nở, một nụ.")
             // Lựa chọn 1 (bản An của "Hỏi / Bỏ qua"): không lựa chọn nào cứu được ai. Mặc định = hỏi (đúng canon 1B.2).
             .Choice("Dưới bếp có tiếng mẹ rửa chén.", 0,
                 O("Hỏi mẹ, thật vô tư", "hoi1", SetF("an_p02_hoi_hoa")),
                 O("Bỏ qua, xuống ăn chè", "bo1"))
             .Label("hoi1")
             .Say(A, Hap, "Mẹ ơi, bà đi chùa mang hoa về hả mẹ?")
             .Sfx("sfx_nuoc_rua_chen_ngung")
             .Narrate("Dưới bếp, tiếng nước rửa chén ngừng đúng một nhịp. “Chắc vậy.”")
             .Narrate("Tôi không hỏi bà; hỏi bà thì bà buồn. Cái nhịp ngừng ấy đã trả lời rồi: không ai trong nhà mang hoa về.")
             .Goto("sau1")
             .Label("bo1")
             .Narrate("Tôi xuống ăn chè, kể chuyện mưa riêng thêm một lần. Cả nhà cười. Ông cười theo, trễ hơn một chút.")
             .Narrate("Khuya, tôi nghe tiếng dép ông dừng ở chiếu nghỉ, lâu như lúc ông đứng ngắm mưa.")
             .Label("sau1")
             .Pause(800);
            DeathDate(s, "17/07/2018", "Sáng 17/07/2018, ông ngã thang ở nhà văn hóa phường.");
            s.Backdrop("bg_nha_cu_cau_thang", 900)
             .Narrate("Chiều đưa tang, bậu cửa sổ trống, cầu thang hết mùi hoa.")
             .Narrate("Tôi đứng ở chiếu nghỉ, cười với từng người lên thắp nhang, để không ai phải dừng lại đó mà an ủi tôi.")
             .Pause(1200)
             .Backdrop("bg_nha_cu_bep_2018").Music("bgm_nha_cu_am")
             .Banner("Cuối tháng Tám, 2018")
             .Narrate("Cuối tháng Tám, bà tìm lại chiếc nhẫn cưới đánh rơi từ năm 1977, dưới đáy hũ gạo.")
             .Say(A, Lau, "Vậy là cả nhà ăn cơm có nhẫn suốt bốn mươi năm luôn hả bà!")
             .Narrate("Bà cười chảy nước mắt, lần đầu từ hôm ông mất. Tôi giữ tiếng cười ấy như giữ một món đồ dễ vỡ.")
             .Backdrop("bg_hem_nha_cu").Sfx("sfx_choi_tre")
             .Narrate("Đêm đó gió lớn. Sáng ra, chổi tre của bà hàng xóm quét mãi theo một đường, không quét ra hai bên.")
             .Narrate("Ra cổng mới thấy cánh phượng dồn thành một vệt đỏ, chạy thẳng từ đầu xóm về cổng nhà mình.")
             .Say(A, Hap, "Bà ơi, gió trải thảm đỏ đón bà đi chùa nè!")
             .Narrate("Bà cười. Đi ngang vệt phượng lần thứ hai, tôi thôi cười.")
             .Pause(700)
             .Music(SilentMusic).Backdrop("bg_bau_cua_so")
             .Narrate("Tối đó mùi hoa sứ lại tới. Lần này tôi nhấc được cái chai lên. Nó lạnh hơn cả nước giếng.")
             .Narrate("Dưới bếp, bà đang kể chuyện chiếc nhẫn lần thứ ba, giọng vui như con gái.")
             // Lựa chọn 2. Mặc định = đặt xuống (đúng canon 1B.3).
             .Choice("Cái chai lạnh buốt trong tay.", 1,
                 O("Cầm xuống hỏi cả nhà", "hoi2", SetF("an_p02_hoi_chai")),
                 O("Đặt xuống, đi nghe bà kể", "bo2"))
             .Label("hoi2")
             .Narrate("Tôi xuống tới bậc cuối. Tiếng bà ngừng ngang câu, chờ tôi.")
             .Say(A, Hap, "Cái chai trên bậu lạnh ghê, ai bỏ tủ lạnh hả ba?")
             .Narrate("“Gió lùa cầu thang đó con,” cha nói, mắt không rời cái quạt đang sửa.")
             .Narrate("Bà nhìn cái chai, rồi kể tiếp, nhỏ hơn. Tôi đem chai lên đặt lại chỗ cũ. Tiếng bà không to lại nữa.")
             .Goto("sau2")
             .Label("bo2")
             .Narrate("Tôi đặt cái chai xuống. Tôi không muốn làm người dập tắt cái giọng đó.")
             .Narrate("Tôi xuống nhà, ngồi nghe bà kể lần thứ ba, và cười đúng những chỗ đã cười hai lần trước.")
             .Label("sau2")
             .Pause(600)
             .Narrate("Bà dặn sáng mai bảy giờ ra chân bậc đá chùa đỡ bà xuống.");
            Note(s, "nhung_thu_tren_bau_cua_so");
            s.FadeOut(800).Then("P03_an");
            return s;
        }

        // =====================================================================================
        // P03 — Hai phút (1B.3)
        // =====================================================================================
        static SceneDefinition P03()
        {
            var s = NewScene("P03_an", "Hai phút",
                "Trên đường đón bà, An va vào người mặc áo mưa xám đen có đồng hồ chạy nhanh hai phút. An tới lúc 07:02; bà ngã, "
                + "phía sau không có ai. Không ai thấy người đàn ông. An nhận giấy chẩn đoán và học cách cười để người ta thôi lo.");
            s.Backdrop("bg_goc_pho_cuoi", 0).Music(SilentMusic).FadeIn(700)
             .Banner("Sáng thứ Ba")
             .Narrate("Sáng hôm sau, tôi đi đón bà. Ở góc phố cuối, tôi va vào một người đàn ông.")
             .Narrate("Áo mưa xám đen, mũ trùm kéo sụp, dù trời nắng. Người ông có mùi cám heo.")
             .Sfx("sfx_tui_roi")
             .Narrate("Túi đồ của tôi rơi. Ông nắm cổ tay tôi. Ngón tay khô và lạnh, dưới móng có đất đỏ.")
             .Narrate("Ông giơ tay lên xem đồng hồ: “Còn sớm.”")
             .Say(A, Sur, "Dạ con xin lỗi, con xin lỗi chú, tại con đi không nhìn…")
             .Sfx("sfx_tich_tac_nhanh")
             .Narrate("Trong lúc xin lỗi, tôi nghe tiếng đồng hồ của ông sát bên tai. Nó tích tắc nhanh hơn tim tôi.")
             .Narrate("Ông buông tay, đi vào con hẻm bên cạnh. Mặt ông thì tôi không thấy.")
             .Narrate("Chú này kỳ ghê, trời nắng mà mặc áo mưa, rồi còn—")
             .Banner("06:59", 1400)
             .Narrate("Tôi nhặt túi, chạy.")
             .Backdrop("bg_chua").Sfx("sfx_dep_bac_da")
             .Narrate("Tôi tới chân bậc đá lúc 07:02.")
             .Narrate("Tôi nghe tiếng dép của bà ở bậc trên cùng. Rồi một tiếng hụt hơi, như có ai vừa đẩy mạnh vào lưng bà.")
             .Narrate("Bà đổ chúi về phía trước. Phía sau bà không có ai.")
             .Pause(1000);
            DeathDate(s, "04/09/2018", "Biên bản ghi: “trượt ngã”.");
            s.Backdrop("bg_goc_pho_cuoi", 900)
             .Narrate("Tôi không đi khai báo. Tôi đi hỏi từng người, bằng giọng của một đứa cháu đang buồn.")
             // Vòng hỏi: hỏi nơi nào thì nơi đó đóng lại; luôn có lối "thôi, về nhà".
             .Label("hoi")
             .Choice("Hỏi ai?", 0,
                 OIf("Chị bán thuốc đầu phố", "nha_thuoc", "!STORY_FLAG an_p03_nha_thuoc"),
                 OIf("Sạp xôi cạnh cổng chùa", "xoi", "!STORY_FLAG an_p03_xoi"),
                 OIf("Chú công an phường", "cong_an", "!STORY_FLAG an_p03_cong_an"),
                 O("Thôi, về nhà", "xong"))
             .Label("nha_thuoc")
             .Flag("an_p03_nha_thuoc")
             .Say(A, Sad, "Chị ơi, sáng nay bà con đi ngang đây, chị có để ý ai đi theo bà không chị?")
             .Narrate("Chị thương tôi, kể nhiều hơn những gì đã kể với công an: camera nhà thuốc hỏng từ tuần trước.")
             .Goto("hoi")
             .Label("xoi")
             .Flag("an_p03_xoi")
             .Narrate("Sạp xôi đóng cửa. Bà bán nước kế bên kể: bà bán xôi về quê đám giỗ. Sáng thứ Ba, đường vắng.")
             .Goto("hoi")
             .Label("cong_an")
             .Flag("an_p03_cong_an")
             .Say(A, Wor, "Chú ơi, lúc đó có ai đi ngang bậc đá không chú?")
             .Narrate("“Trượt ngã thôi cháu. Đá ẩm.” Chú rót cho tôi cốc trà, tràn cả miệng cốc, như đang nghĩ chuyện khác.")
             .Goto("hoi")
             .Label("xong")
             .Narrate("Không ai thấy người đàn ông đó. Không ai bảo tôi nói sai. Cũng không ai bảo tôi nói đúng.")
             .Narrate("Vết bầm ở cổ tay tím gần một tuần. Tôi đeo một cái vòng bản to suốt tuần ấy.")
             .Narrate("Bé Khoa khen đẹp, mặt không cười, như đang báo cáo.")
             .Pause(700)
             .Backdrop("bg_nha_cu_cau_thang")
             .Narrate("Từ sau hôm ấy, thế giới của tôi lệch đi. Tôi nghe giọng mình vọng ra từ phòng bên khi chưa mở miệng.")
             .Narrate("Tôi gặp những An khác: một An mười lăm tuổi đứng ở góc phố mà không cười. Một An luôn tới muộn hai phút.")
             .Backdrop("bg_benh_vien")
             .Narrate("Bác sĩ kết luận: tâm thần phân liệt khởi phát sớm, có thể do sang chấn.")
             .Narrate("Tôi mười lăm tuổi, ba ngày nữa tròn mười sáu.")
             .Narrate("Mẹ khóc ở hành lang bệnh viện. Tôi ra ôm mẹ.")
             .Say(A, Lau, "Không sao đâu mà! Giờ con có giấy khen điên hẳn hoi, cả xóm chưa ai có đâu mẹ.")
             .Narrate("Mẹ bật cười giữa lúc đang khóc.")
             .Pause(700)
             .Narrate("Hôm đó tôi học được điều quan trọng nhất đời mình: nếu tôi cười, người ta sẽ thôi lo cho tôi.")
             .Narrate("Người điên giỏi nhất là người điên không ai biết. Tôi giỏi việc đó hơn bất kỳ ai.")
             .Pause(900)
             .Music(SilentMusic).Backdrop("bg_bau_cua_so")
             .Narrate("Hôm đưa tang bà, bậu cửa sổ lại trống.")
             .Pause(1400)
             .FadeOut(800)
             .Then("P04_an_a");
            return s;
        }

        // =====================================================================================
        // P04 — Bậu cửa sổ (1B.4, 1B.5)
        // =====================================================================================
        static SceneDefinition P04a()
        {
            var s = NewScene("P04_an_a", "Giữ người ta trong tầm tay",
                "An cố giữ từng người bên mình. Anh Minh mất 19/02/2019, bé Khoa 13/05/2019, cha 24/08/2019; "
                + "biên bản của cha ghi tên trại bằng hai chữ B.R.");
            s.Backdrop("bg_nha_cu_bep_2018", 0).Music("bgm_nha_cu_buon").FadeIn(700)
             .Banner("Mùa xuân, 2019")
             .Narrate("Sau bà, tôi có một cách mới: không ngăn được điều gì thì ít nhất phải giữ người ta trong tầm tay.")
             .Narrate("Anh Minh nhận học bổng toàn phần. Mẹ lại bắc nồi lẩu.")
             .Backdrop("bg_nha_cu_nha_tam").Sfx("sfx_giot_nuoc")
             .Narrate("Tuần sau trời nồm. Đêm nào nhà tắm cũng có tiếng nước nhỏ giọt, dù vòi đã khóa chặt.")
             .Narrate("Mỗi lần anh Minh đi ngang, tiếng giọt nước ngừng lại, như nín thở chờ anh đi qua.")
             .Backdrop("bg_hem_nha_cu")
             .Narrate("Tôi không nói gì. Tôi bám anh cả tuần: đòi dạy xe máy, đòi đi ăn chè, nhõng nhẽo tới mức anh bảo tôi bị bệnh.")
             .Say(A, Lau, "Có giấy mà anh!")
             .Narrate("Anh cười to. Tối thứ Bảy, mùi hoa sứ tràn xuống cầu thang, đúng lúc anh cầm tay tôi chỉ cách vào số.")
             .Narrate("Tôi không chạy lên. Lần đầu từ đám tang bà, anh cười to như vậy. Tôi không muốn làm tắt tiếng cười ấy.");
            DeathDate(s, "19/02/2019", "Sáng 19/02/2019, xe của anh trượt vệt dầu trên cầu.");
            s.Backdrop("bg_nha_cu_cau_thang", 900)
             .Narrate("Bé Khoa được giải nhất với bức tranh “Gia đình em”.")
             .Narrate("Mấy đêm sau, nó kể mơ thấy có người đứng sau lưng dặn đừng quay lại.")
             .Narrate("“Giọng giống chị An,” nó nói, mặt không cười, như đang báo cáo.")
             .Sfx("sfx_hat_dau")
             .Narrate("Sáng hôm ấy, trong túi áo khoác tôi có một hạt đậu đen. Tôi nghe nó lạo xạo trước khi sờ thấy.")
             .Narrate("Tối đó, mùi hoa lại tới. Lần này tôi không chần chừ. Tôi kéo Khoa sang ngủ chung, ôm nó cả đêm.")
             .Narrate("Tôi kể chuyện tới lúc nó ngủ say, nghĩ rằng ôm đủ chặt thì giữ được.")
             .Narrate("Nó không cười với chuyện nào của tôi. Nhưng cũng không buông tay tôi ra.");
            DeathDate(s, "13/05/2019", "Chiều 13/05/2019, sau một cơn giông, mảng tường rào trường học đổ xuống.");
            s.Backdrop("bg_nha_cu_hien", 900)
             .Narrate("Mùa hè, cha nhận hợp đồng sửa trạm bơm ở một trại xa.")
             .Sfx("sfx_gau_gieng")
             .Narrate("Nước giếng sau nhà hơi ấm. Tiếng gàu thả xuống vang lâu hơn mọi khi, như giếng vừa sâu thêm một đoạn.")
             .Narrate("Kiến bò thành vòng quanh miệng giếng.")
             .Narrate("Lần này tôi nói. Theo cách của tôi: khóc, nũng nịu, bám tay cha, viện đủ lý do dễ thương nhất.")
             .Narrate("Cha cười, véo má tôi: “Con gái lớn rồi mà còn nhõng nhẽo.”")
             .Pause(600)
             .Narrate("Đêm mùi hoa tới thì cả xóm cúp điện. Hai cha con với mẹ ngồi quạt tay ngoài hiên tới gần sáng.")
             .Narrate("Cha kể con heo ở trại hễ đến giờ cân là nằm ngửa giả chết.")
             .Say(A, Lau, "Heo mà cũng biết sợ cân hả ba!")
             .Narrate("Tôi cười thật. Đó là lần cuối tôi cười thật trước mặt cha.");
            DeathDate(s, "24/08/2019", "Ngày 24/08/2019, cha bị điện giật ở trạm. Biên bản ghi tên trại bằng hai chữ: B.R.");
            s.FadeOut(800).Then("P04_an_b");
            return s;
        }

        static SceneDefinition P04b()
        {
            var s = NewScene("P04_an_b", "Cái chai, và câu chị nói với em",
                "An bắt đầu sổ hồng. Đêm 04/10 An canh hoa rồi mất trọn ngày 05/10, sổ có dòng lạ “02:00. Đã giữ chỗ.”. "
                + "Mẹ mất 08/11/2019. Cái chai có vòng sạch và sáu cuống hoa khô; An dọn ra ở trọ.");
            s.Backdrop("bg_tiem_tap_hoa", 0).Music(SilentMusic).FadeIn(700)
             .Narrate("Sau đám tang cha, tôi mua một cuốn sổ bìa hồng ở tiệm tạp hóa đầu hẻm. Bà chủ tiệm khen dễ thương.");
            Note(s, "ba_oi_con_chep_ngay");
            s.Narrate("Tối đó tôi viết lá thư đầu tiên: “Ba ơi, con chép ngày lại đây, kẻo sau này cả nhà hỏi mà con quên :)”")
             .Narrate("Bên dưới là năm ngày mất, mỗi ngày một bông hoa nhỏ vẽ tay. Kể ra giấy rồi thì không phải kể ra miệng.")
             .Backdrop("bg_nha_cu_cau_thang")
             .Banner("Tháng Mười, 2019")
             .Narrate("Đầu tháng Mười, tôi nhặt được một tờ tiền trên đường đi học. Tối 04/10, mùi hoa tới.")
             .Narrate("Lần này tôi canh, ôm gối ngồi ở bậc thang, ăn kẹo cho tỉnh.")
             .Narrate("“Ba ơi, 21:14, hoa vẫn ở đó :)”")
             .Pause(900)
             .Narrate("“Ba ơi, 23:58. Vẫn còn. Con chưa ngủ đâu :)”")
             .FadeOut(1200).Pause(900)
             .Backdrop("bg_phong_ngu_an_cu", 0).FadeIn(1200)
             .Narrate("Tôi tỉnh dậy trên giường khi trời đã tối hẳn. Tối ngày 05/10/2019. Mẹ đang chườm khăn.")
             .Narrate("Dưới móng tay tôi có đất nâu đỏ. Không ai gặp chuyện gì.")
             .Narrate("Trong sổ có thêm một dòng: “02:00. Đã giữ chỗ.”")
             .Narrate("Chữ của tôi. Nhưng đầu dòng không gọi ai, cuối dòng không có mặt cười.")
             .Narrate("Tôi chưa từng viết một dòng nào mà không gửi cho một người trong nhà.")
             .Pause(1500)
             .Backdrop("bg_nha_cu_bep_2018")
             .Banner("Tháng Mười Một, 2019")
             .Narrate("Tháng Mười Một, mẹ được nghỉ phép về quê. Mẹ hát sai lời trong bếp.")
             .Sfx("sfx_bat_up")
             .Narrate("Đêm trước chuyến đi, tôi nghe mẹ úp chồng bát lên giá: sáu tiếng cạch. Thiếu một.")
             .Narrate("Ra xem thì cái bát của tôi nằm ngửa. “Mẹ ơi, bát của con nằm ngửa :)”")
             .Backdrop("bg_bau_cua_so")
             .Narrate("Lên lầu, mùi hoa sứ đã đầy cầu thang. “Mẹ ơi, hoa. Bảy bông :)”")
             .Narrate("Tôi đã viết đủ, và viết cho chính mẹ. Rồi tôi làm điều tôi giỏi nhất: không để mẹ lo.")
             .Backdrop("bg_nha_cu_bep_2018")
             .Narrate("Tôi xếp vali giúp mẹ, dặn mẹ mặc ấm, hát cùng mẹ bài hát sai lời.")
             .Narrate("Mẹ hôn lên trán tôi, dặn ở nhà nhớ khóa ga. Câu “Mẹ đừng đi” đã lên tới cổ họng.")
             .Say(A, Hap, "Dạ. Mẹ đi vui nha mẹ.")
             .Narrate("Tôi nuốt câu kia xuống, cười, gật đầu.");
            DeathDate(s, "08/11/2019", "Ngày 08/11/2019, chiếc xe khách mẹ đi bị lật. Túi đồ người ta trả về không có chiếc vòng.");
            s.Backdrop("bg_nha_cu_cau_thang", 900)
             .Narrate("Hai lá thư vẫn nằm đó, tròn trịa, có mặt cười. Tôi đã biết đủ. Tôi chỉ đã chọn không làm ai buồn.")
             .Pause(1200)
             .Narrate("Sau đám tang mẹ, căn nhà chỉ còn mình tôi, mười bảy tuổi.")
             .Narrate("Hàng xóm sang nhiều lắm. Tôi pha trà, cười tiễn từng người. Rồi tôi đi lau nhà; ngồi yên thì tôi sẽ hết cười.")
             .Backdrop("bg_bau_cua_so").Sfx("sfx_luc_cuc")
             .Narrate("Ở chiếu nghỉ, tôi nhấc cái chai sau tấm rèm. Nó nhẹ hơn tôi tưởng, bên trong có tiếng lục cục khe khẽ.")
             .Narrate("Dưới đáy chai là một vòng tròn sạch giữa lớp bụi dày nhiều tháng.")
             .Narrate("Trong chai còn chút nước đục, và sáu cuống hoa sứ khô đen nằm chồng lên nhau.")
             .Pause(1500)
             .Narrate("Tôi ngồi trên bậc thang đến tối. Rồi tôi rửa cái chai thật sạch, lau khô, đặt lại đúng vào vòng tròn ấy.")
             .Narrate("Rồi tôi nói với nó, bằng cái giọng ngày trước vẫn dỗ cả nhà.")
             .Say(A, Hap, "Chị biết rồi. Lần sau đừng lấy của chị ai nữa nhé.")
             .Pause(800)
             .Narrate("Giọng tôi vẫn vui. Tôi nghe giọng mình vui, và đó là lần đầu tiên tôi thấy sợ chính mình.")
             .Pause(1500)
             .Backdrop("bg_tiem_tap_hoa")
             .Narrate("Hôm sau tôi mua hai vỉ sticker: một vỉ ngôi sao, một vỉ mặt trăng. Bà chủ tiệm hỏi mua cho em nào.")
             .Say(A, Hap, "Dạ, cho cả nhà ạ.")
             .Narrate("Từ đó thư nào cũng có nhãn: ngôi sao ★ cho thứ ai cầm lên cũng được, ai nghe cũng ra.")
             .Narrate("Mặt trăng ☾ cho thứ chỉ mình tôi nhận ra. Điều tôi đoán thì viết bút chì ra lề, thật nhỏ.");
            Note(s, "nha_minh_oi_cai_chai");
            s.Narrate("“Nhà mình ơi, cái chai có một vòng sạch dưới đáy, sáu cuống hoa khô.” ★")
             .Narrate("“Hoa lúc có, lúc không.” ☾ Ngoài lề, bút chì: “Hay là nó chưa bao giờ đi đâu cả?”")
             .Narrate("Cuối thư, lần đầu tiên, tôi không vẽ mặt cười.")
             .Pause(1000)
             .Backdrop("bg_phong_tro")
             .Narrate("Năm sau, vừa đủ mười tám, tôi dọn ra ở trọ, mang theo cuốn sổ và cái bát của mình.")
             .Narrate("Hàng xóm hỏi sao không bán nhà.")
             .Say(A, Lau, "Để nhà nghỉ ngơi một chút ạ!")
             .Narrate("Ai cũng cười. Câu ấy dần thành câu trả lời cho mọi việc tôi chưa đủ sức làm.")
             .FadeOut(900)
             .Then("P05_an_a");
            return s;
        }

        // =====================================================================================
        // P05 — Ngày hai mươi bốn (1B.6, 1B.7, 1B.8)
        // =====================================================================================
        static SceneDefinition P05a()
        {
            var s = NewScene("P05_an_a", "Sáu năm cười, rồi ngày hai mươi bốn",
                "Sáu năm ở trọ, An vui vẻ, không ai biết chị có giấy. Sau đợt thưởng nóng, sáng 24/09/2026 gương chào lại chậm "
                + "nửa nhịp; An bị ô tô tông lúc 07:02 và nghe lại tiếng đồng hồ nhanh hơn tim.");
            s.Backdrop("bg_phong_tro", 0).Music("bgm_thanh_pho_sang").FadeIn(800)
             .Banner("Sáu năm sau")
             .Narrate("Sáu năm ở trọ, tôi uống thuốc đều, đi làm đều, và được cả công ty quý vì lúc nào cũng vui.")
             .Narrate("Không ai trong văn phòng biết tôi có giấy.")
             .Narrate("Các An khác vẫn theo tôi. Một An mười bảy tuổi ngồi cuối giường ôm gối. Một An luôn đến muộn hai phút.")
             .Narrate("Sáng nào tôi cũng chào họ bằng mắt, rồi chào bảo vệ tòa nhà bằng miệng.")
             .Narrate("Phòng trọ của tôi không có bình hoa nào. Tôi cố ý như vậy.")
             .Pause(700)
             .Banner("Tháng Chín, 2026")
             .Narrate("Tháng Chín năm 2026, công ty thưởng nóng cho tôi. Cả phòng vỗ tay. Tôi khao trà sữa cả phòng.")
             .Narrate("Về tới phòng trọ, tôi đi một vòng, hít từng góc phòng xem có mùi hoa không. Không có gì.")
             .Narrate("Mình không nhận ra vì nó không có ở đó, hay vì mình đã quá giỏi việc không nghe?")
             .Pause(900)
             .Music(SilentMusic).Backdrop("bg_phong_tro_guong")
             .Banner("Sáng 24/09/2026")
             .Say(A, Hap, "Chào buổi sáng!")
             .Pause(400).Sfx("sfx_tieng_vong")
             .Narrate("Câu chào vọng lại từ mặt gương, chậm hơn tôi nửa nhịp.")
             .Narrate("Trong một cái phòng tắm bé tí, không có chỗ nào để vọng.");
            Note(s, "me_oi_guong_noi_cham");
            s.Narrate("“Mẹ ơi, gương nói chậm :) Con uống thuốc rồi.” ☾")
             .Backdrop("bg_nga_tu_den_do")
             .Narrate("Đèn qua đường chuyển xanh. Một chiếc ô tô vượt đèn.")
             .Sfx("sfx_tich_tac_nhanh")
             .Narrate("Thứ cuối cùng tôi nghe là tiếng tích tắc nhanh hơn tim mình, rõ mồn một giữa tiếng phanh và tiếng hét.")
             .Narrate("Tôi quay đầu được một chút. Bên kia đường, một người đàn ông mặc áo mưa xám đen đứng yên, nhìn đồng hồ.")
             .Narrate("Tôi kịp nghĩ một câu, và nó vẫn là một câu đùa: “Lần này mình đúng giờ rồi đó.”")
             .FadeOut(1500).Pause(1200)
             .Backdrop("bg_benh_vien", 0).FadeIn(1000)
             .Narrate("Biên bản ghi thời điểm va chạm: 07:02. Hồ sơ bệnh án ghi: nữ, 24 tuổi.")
             .Then("P05_an_b");
            return s;
        }

        static SceneDefinition P05b()
        {
            var s = NewScene("P05_an_b", "Giấc mơ trong phòng bệnh",
                "Sau ca mổ, An mơ cả nhà ngồi quanh giường rồi lần lượt rời đi; bé Khoa đếm người bằng ngón tay. "
                + "Lần đầu tiên An không cười được, nên mẹ không quay lại. Tỉnh dậy, An mất trí nhớ ngắn hạn về thứ tự những ngày mất.");
            s.Backdrop("bg_phong_benh_giac_mo", 0).Music(SilentMusic).FadeIn(1200)
             .Sfx("sfx_may_do_nhip_tim")
             .Narrate("Sau ca mổ tôi ngủ hai ngày, và mơ đúng căn phòng ấy: rèm xanh, tiếng máy đo nhịp tim.")
             .Narrate("Mẹ ngồi gọt táo, vỏ không đứt. Cha đứng ở cửa sổ. Ông ngồi chân giường, bà kéo lại vạt áo cho ông.")
             .Narrate("Anh Minh cầm một cái túi nilon. Bé Khoa ngồi mép ghế, chân đung đưa.")
             .Narrate("Nó giơ ngón tay đếm người trong phòng mà không nói ra con số.")
             .Narrate("Tôi không cử động được, không nói được. Tôi chỉ nhìn được.")
             .Pause(1500)
             .Narrate("Rồi lần lượt từng người đứng dậy đi ra cửa. Khoa ngoái lại một lần, rồi vội quay đi.")
             .Narrate("Mẹ đi cuối. Mẹ đặt quả táo đã gọt lên bàn, đứng ở ngưỡng cửa một lúc, quay lưng về phía tôi.")
             .Narrate("Tôi biết mẹ đang chờ gì. Cả đời, mỗi lần mẹ lo, tôi đều cười, và mẹ yên tâm.")
             .Pause(1000)
             .Narrate("Tôi dồn hết sức để cười cho mẹ đi. Tôi không cười được.")
             .Narrate("Lần đầu tiên trong đời tôi không cười được. Và chính vì thế mẹ không quay lại.")
             .Pause(2000)
             .Sfx("sfx_may_do_bao_dong").Backdrop("bg_benh_vien", 300)
             .Narrate("Tôi bật dậy, hớp không khí, máy đo nhịp tim kêu inh ỏi. Cô điều dưỡng chạy vào.")
             .Narrate("Tôi đã cười với cô ấy trước cả khi kịp thở đều. “Em tỉnh rồi thì tốt quá.”")
             .Narrate("Căn phòng không có quả táo nào, nhưng thoang thoảng mùi táo.")
             .Narrate("Hai bên má tôi tê rần suốt một ngày, như vừa cố nhấc một thứ gì đó rất nặng.")
             .Pause(800)
             .Narrate("Bác sĩ kết luận: chấn thương đầu nhẹ, suy giảm trí nhớ ngắn hạn. Tôi không còn nhớ thứ tự người nhà đã mất.")
             .Narrate("Nhưng khuôn mặt từng người trong giấc mơ thì tôi nhớ rõ hơn bất cứ thứ gì.")
             .FadeOut(900)
             .Then("P05_an_c");
            return s;
        }

        static SceneDefinition P05c()
        {
            var s = NewScene("P05_an_c", "Bảy buổi sáng tập cười",
                "Bảy buổi sáng liền, từ 06:58 đến 07:02, An chào gương và nghe tiếng vọng: chậm nửa nhịp, mất tiếng cười, rồi phẳng lì; "
                + "mặt người trong gương không cười. An học cách chờ tiếng vọng về rồi mới nhìn, và quyết định đi thăm mộ.");
            s.Backdrop("bg_phong_tro_guong", 0).Music(SilentMusic).FadeIn(900)
             .Narrate("Về lại phòng trọ, sáng đầu tiên tôi thức dậy lúc 06:58, không cần báo thức, và đi thẳng tới cái gương.")
             .Narrate("Sáu năm nay sáng nào tôi cũng chào gương. Chỉ là giờ giấc bỗng chính xác đến từng phút: 06:58 đến 07:02.")
             .Narrate("Như một cuộc hẹn tôi không nhớ đã hẹn với ai.")
             .Banner("Ngày thứ nhất — 06:58", 1400)
             // Ngày 1: phải chừa chỗ cho tiếng vọng.
             .Label("d1")
             .Choice("Trước gương.", 0,
                 O("Chào thật to, rồi nín thở nghe", "d1_ok"),
                 O("Chào liền hai câu cho vui", "d1_sai"))
             .Label("d1_sai")
             .Say(A, Lau, "Chào buổi sáng! Chào buổi sáng nè!")
             .Narrate("Hai câu chào chồng lên tiếng vọng, rối như hai cái radio mở cùng lúc. Phải chừa chỗ cho nó về.")
             .Goto("d1")
             .Label("d1_ok")
             .Say(A, Hap, "Chào buổi sáng!")
             .Pause(500).Sfx("sfx_tieng_vong")
             .Narrate("“…buổi sáng!” Câu chào vọng lại chậm nửa nhịp. Tôi đếm được cái nửa nhịp ấy, như đếm phách.")
             .Banner("Ngày thứ ba — 06:58", 1400)
             // Ngày 3: chỗ lệch nằm ở tai, không ở mắt.
             .Label("d3")
             .Choice("Hôm nay chào thế nào?", 0,
                 O("Chào kèm một tiếng cười, nghe kỹ phần đuôi", "d3_ok"),
                 O("Chào xong nhìn kỹ miệng mình trong gương", "d3_sai"))
             .Label("d3_sai")
             .Narrate("Miệng trong gương mấp máy đúng nhịp với miệng tôi. Mắt không bắt được gì. Chỗ lệch nằm ở tai.")
             .Goto("d3")
             .Label("d3_ok")
             .Say(A, Lau, "Chào buổi sáng! Hì hì.")
             .Pause(500).Sfx("sfx_tieng_vong")
             .Narrate("Câu chào vọng lại đủ chữ. Nhưng mất tiếng cười tôi gắn ở cuối.")
             .Banner("Ngày thứ năm", 1200)
             .Narrate("Ngày thứ năm, tôi thử im lặng. Trong phòng tắm vẫn có tiếng thở, đều hơn tiếng thở của tôi.")
             .Banner("Ngày thứ bảy — 06:58", 1400)
             // Ngày 7: nghe hết rồi mới nhìn (bài hướng dẫn ngầm cho chấm sáng ở P06).
             .Label("d7")
             .Choice("Chào bằng giọng tươi nhất. Rồi sao?", 0,
                 O("Nghe hết tiếng vọng, rồi mới nhìn gương", "d7_ok"),
                 O("Nhìn gương ngay khi vừa chào", "d7_sai"))
             .Label("d7_sai")
             .Narrate("Trong gương, một cô gái cười rất tươi, cười đúng lúc. Mặt tôi. Tôi nhìn sớm quá; tiếng vọng chưa về.")
             .Goto("d7")
             .Label("d7_ok")
             .Say(A, Lau, "Chào buổi sáng!!")
             .Pause(700).Sfx("sfx_tieng_vong_phang")
             .Narrate("Câu chào vọng lại phẳng lì, như có ai đang đọc nó ra từ một tờ giấy.")
             .Do(NarrativeEffectKind.Custom, NarrativeCustomIds.ReflectionPrefix + Portraits.Neutral)
             .Narrate("Rồi tôi mới nhìn. Trong gương, mặt tôi vẫn cười. Mặt người kia thì không.")
             .Pause(1800);
            Note(s, "me_oi_con_van_cuoi_duoc");
            s.Narrate("“Mẹ ơi, con vẫn cười được nè! Mà không biết cười cho ai nữa.” ☾")
             .Narrate("Rồi tôi gạch dấu chấm than đi.")
             .Pause(800)
             .Narrate("Bảy buổi sáng ấy để lại cho tôi một thói quen: nghe mọi thứ như nghe tiếng vọng.")
             .Narrate("Người khác tưởng tôi đang ngân nga. Tôi để họ tưởng vậy.")
             .Narrate("Tôi quyết định đi thăm mộ để đọc lại tên cả nhà cho đúng.")
             .Narrate("Người ta đi thăm mộ để nhớ. Tôi đi để học thuộc lại.")
             .FadeOut(900)
             .Then("P06_an_a");
            return s;
        }

        // =====================================================================================
        // P06 — Ánh kim (1B.9, 1B.10, 1B.11)
        // =====================================================================================
        static SceneDefinition P06a()
        {
            var s = NewScene("P06_an_a", "Hai cái bánh bò và một suất trống",
                "Ở nghĩa trang, ông trông mộ cho biết nhà An mua bảy suất, mới dùng sáu. Phiếu thu đề ngày 05/10/2019, "
                + "chữ ký người nộp gồm bảy nét ngắn không giống ai trong nhà; An chụp lại tờ phiếu.");
            s.Backdrop("bg_nghia_trang", 0).Music("bgm_gio_nhe").FadeIn(900)
             .Narrate("Tôi mang theo hoa, nhang và một túi bánh bò. Hoa cho người nhà, bánh cho người sống.")
             .Narrate("Tôi lau tên từng ngôi mộ, chép sáu ngày mất vào sổ, vẽ một bông hoa cạnh mỗi dòng.")
             .Sfx("sfx_choi_quet_la")
             .Narrate("Ông trông nghĩa trang đang quét lá. Tôi mời ông bánh.")
             .Narrate("Ông ăn cái thứ hai thì tự hỏi: “Cháu thăm đủ chưa?”")
             .Say(A, Hap, "Dạ, đủ rồi ạ.")
             .Narrate("Ông chỉ khoảnh đất trống cuối hàng: “Còn một suất nữa mà. Nhà cháu mua bảy suất, mới dùng sáu.”")
             .Pause(800)
             .Say(A, Sur, "Ơ, thật hả bác? Bác cho con xem với, con dốt mấy giấy tờ này lắm.")
             .Narrate("Ông mở sổ chỉ tận tay: phiếu thu đứng tên gia đình, đề ngày 05/10/2019.")
             .Narrate("Đúng hôm tôi sốt li bì, khi mẹ còn sống.")
             .Narrate("Chữ ký người nộp tiền gồm bảy nét ngắn, không giống chữ ai trong nhà.")
             .Narrate("“Mua trước cho cả nhà thì nhiều người làm lắm,” ông nói.")
             .Say(A, Hap, "Dạ, con chụp lại để về hỏi họ hàng nha bác. Bác ăn thêm bánh đi bác.");
            Note(s, "phieu_thu_suat_mo");
            s.Narrate("Tôi để lại cho ông nửa túi bánh.")
             .Music(SilentMusic)
             .Narrate("Ra tới cổng, mùi hoa sứ từ cây sứ già cạnh nhà tưởng niệm thoảng qua. Tôi đứng lại.")
             .Pause(1800)
             .Narrate("Lần này không có xe ôm nào bấm còi. Tôi vẫn đi tiếp, vì tôi đã quen đi tiếp.")
             .FadeOut(800)
             .Then("P06_an_b");
            return s;
        }

        static SceneDefinition P06b()
        {
            var s = NewScene("P06_an_b", "Chào nhà",
                "An về nhà cũ, chào từng chỗ. Chiếc vòng của mẹ rơi ra từ bàn cha; chờ tiếng “tách” rồi mới nhìn, chấm sáng dừng "
                + "trên ảnh Tết 2018, lộ bảy khía trên mặt vòng. Cây đinh treo ảnh còn ấm.");
            s.Backdrop("bg_nha_cu_phong_khach", 0).Music(SilentMusic).FadeIn(900)
             .Narrate("Lần đầu tiên sau sáu năm, tôi mở cửa nhà cũ. Ổ khóa không gỉ, chìa xoay êm, như vừa được tra dầu.")
             .Say(A, Hap, "Con về rồi nè!")
             .Pause(900)
             .Narrate("Không ai đáp, và tôi đã biết sẽ không ai đáp. Tôi nói vì nhà cần được chào.")
             .Narrate("Áo của cha sau cánh cửa: “Con chào ba.” Hộp kim chỉ của mẹ ở ngăn thứ hai.")
             .Narrate("Vạch chiều cao của Khoa trên khung cửa, đủ sáu chữ ký. Thiếu chữ ký của tôi.")
             .Narrate("Phòng khách im đến mức tôi phải đứng lại khá lâu mới nhận ra thứ mình thiếu: tiếng tích tắc.")
             .Narrate("Đồng hồ đứng ở 7 giờ 2 phút. Tôi không chào nó.")
             .Backdrop("bg_bau_cua_so")
             .Narrate("Cái chai trên bậu cửa sổ chiếu nghỉ rỗng, đứng trong vòng tròn sạch. Cái chai tôi đã rửa sáu năm trước.")
             .Backdrop("bg_nha_cu_phong_khach").Sfx("sfx_kim_loai_lach_cach")
             .Narrate("Tôi kéo bàn làm việc của cha. Có tiếng kim loại lách cách trong khe dưới mặt bàn.")
             .Narrate("Tôi thò tay vào, kéo ra một chiếc vòng cổ. Chiếc mẹ đeo đi ngủ đêm trước chuyến xe cuối cùng.")
             .Narrate("Tôi cầm sợi xích, để mặt vòng tự xoay. Nắng chiều hắt một chấm sáng từ mặt vòng lên tường.")
             // Câu đố chấm sáng: nghe trước, nhìn sau (bài học từ cái gương). Mặc định = đúng.
             .Label("vong")
             .Choice("Chấm sáng trôi trên tường.", 0,
                 O("Nhắm mắt, chờ tiếng “tách”, rồi mới nhìn", "vong_ok"),
                 O("Mở to mắt, đuổi theo chấm sáng", "vong_sai"),
                 O("Lấy tay giữ mặt vòng cho đứng yên", "vong_giu"))
             .Label("vong_sai")
             .Narrate("Chấm sáng lướt qua khung cửa, qua đồng hồ, rồi trượt mất. Mắt tôi luôn tới trễ hơn nó một chút.")
             .IfGoto("STORY_FLAG an_p06_vong_goi_y", "vong")
             .Flag("an_p06_vong_goi_y")
             .Sfx("sfx_xich_tach")
             .Narrate("Mỗi lần mặt vòng ngừng xoay, sợi xích kêu “tách” rất khẽ. Chấm sáng chỉ dừng sau tiếng ấy nửa nhịp.")
             .Narrate("Đúng cái nửa nhịp của tiếng vọng trong gương.")
             .Goto("vong")
             .Label("vong_giu")
             .Narrate("Mặt vòng đứng yên thì chấm sáng cũng đứng chết, chỉ vào khoảng tường trống. Nó cần được tự xoay.")
             .Goto("vong")
             .Label("vong_ok")
             .Sfx("sfx_xich_tach")
             .Narrate("Tôi nhắm mắt. Tách. Đếm nửa nhịp. Rồi mới mở mắt.")
             .Narrate("Chấm sáng trôi qua khung cửa có vạch đo, qua mặt đồng hồ, và dừng trên tấm ảnh Tết 2018.")
             .Narrate("Đúng chỗ cổ mẹ.")
             .Pause(900)
             .Narrate("Trong ảnh, mặt vòng lật ngược, để lộ bảy khía nhỏ. Tôi đã ngắm tấm ảnh này hàng trăm lần mà chưa thấy.")
             .Pause(1200)
             .Narrate("Lần này không ai gọi tôi đi, không có tiếng cười nào để giữ. Tôi gỡ tấm ảnh xuống.")
             .Narrate("Cây đinh trên tường còn ấm.")
             .Pause(1500);
            Note(s, "ba_oi_cay_dinh");
            s.Narrate("“Ba ơi, cây đinh ba đóng trên tường.” ★ “Nó còn ấm.” ☾")
             .Narrate("Ngoài lề, bút chì: “Có người vừa ở đây?” Tôi định vẽ mặt cười, rồi thôi.")
             .Then("P06_an_c");
            return s;
        }

        /// <summary>Một cánh khóa sao: một tiếng động trong nhà, An phải chào đúng người. Chào sai thì tiếng động lặp lại, chờ.</summary>
        static void StarArm(SceneDefinition s, string id, string sfx, string prompt, string greeting, int correctIndex,
            string retry, params string[] wrong)
        {
            var opts = new List<NarrativeOption>();
            int w = 0;
            for (int i = 0; i <= wrong.Length; i++)
            {
                if (i == correctIndex) { opts.Add(O(greeting, id + "_ok")); continue; }
                // a wrong greeting can be tried once, then it drops out, so the lock always opens
                string tried = "an_p06_" + id + "_sai_" + w;
                var o = O(wrong[w++], id + "_sai", SetF(tried));
                o.Condition = "!STORY_FLAG " + tried;
                opts.Add(o);
            }
            s.Sfx(sfx)
             .Label(id)
             .Choice(prompt, correctIndex, opts.ToArray())
             .Label(id + "_sai")
             .Sfx(sfx)
             .Narrate(retry)
             .Goto(id)
             .Label(id + "_ok")
             .Say(A, Hap, greeting.Trim('“', '”'));
        }

        static SceneDefinition P06c()
        {
            var s = NewScene("P06_an_c", "Bức tranh của Khoa và sáu lời chào",
                "Sau khung ảnh là tranh gốc “Gia đình em” của bé Khoa; mũi tên chỉ bậc thang thứ bảy. Dưới bậc là hộp thiếc khóa sao "
                + "bảy cánh; An chào từng người qua sáu tiếng động, cánh thứ bảy là im lặng tuyệt đối, rồi khóa mở.");
            s.Backdrop("bg_tranh_gia_dinh_em", 0).Music(SilentMusic).FadeIn(800)
             .Narrate("Sau tấm bìa lót khung ảnh là bức tranh “Gia đình em” của thằng út, bản gốc.")
             .Narrate("Mặt sau đủ bảy chữ ký. Chữ của tôi nằm trong cái góc nó chừa sẵn, kèm mặt cười tôi vẽ hồi Tết 2018.")
             .Narrate("Người que thứ bảy đứng ở chiếu nghỉ, cạnh cái chai cắm hoa trắng. Một mũi tên chỉ xuống cầu thang.")
             .Narrate("Dấu X đỏ: “chỗ cất đồ bí mật của chị An”. Người que ấy là người duy nhất trong tranh đang cười.")
             .Narrate("Tôi không nhớ mình có chỗ cất đồ bí mật nào. Nhưng em trai tôi chưa bao giờ vẽ sai nhà mình.")
             .Backdrop("bg_nha_cu_cau_thang").Sfx("sfx_ngan_nga")
             .Narrate("Tôi cầm bức tranh ra cầu thang, vừa ngân nga bài hát mẹ hay hát sai lời, vừa gõ từng bậc.")
             // Câu đố bậc thang: ba lần gõ sai thì mũi tên trong tranh sáng lên. Mặc định = bậc thứ bảy.
             .Label("bac")
             .Choice("Gõ bậc nào?", 3,
                 // each wrong step can be knocked once (it then drops out), so the loop always ends
                 new NarrativeOption { Text = "Bậc thứ hai", GotoLabel = "bac_2", Condition = "!STORY_FLAG an_p06_go_2" },
                 new NarrativeOption { Text = "Bậc thứ năm", GotoLabel = "bac_5", Condition = "!STORY_FLAG an_p06_go_5" },
                 new NarrativeOption { Text = "Bậc ngay dưới chiếu nghỉ", GotoLabel = "bac_tren", Condition = "!STORY_FLAG an_p06_go_tren" },
                 O("Bậc thứ bảy", "bac_7"))
             .Label("bac_2").Flag("an_p06_go_2").Sfx("sfx_go_dac")
             .Narrate("Cộc. Đặc như đá. Chỉ có tiếng tôi ngân nga dội lại.")
             .Goto("bac_sai")
             .Label("bac_5").Flag("an_p06_go_5").Sfx("sfx_go_dac")
             .Narrate("Cót két, như mọi khi. Nó mách lẻo đủ thứ chuyện, trừ chuyện tôi cần.")
             .Goto("bac_sai")
             .Label("bac_tren").Flag("an_p06_go_tren").Sfx("sfx_go_dac")
             .Narrate("Đặc. Chỉ có tiếng tim tôi gõ theo.")
             .Goto("bac_sai")
             .Label("bac_sai")
             .IfGoto("STORY_FLAG an_p06_bac_sai_3", "bac")
             .IfGoto("STORY_FLAG an_p06_bac_sai_2", "bac_goi_y")
             .IfGoto("STORY_FLAG an_p06_bac_sai_1", "bac_sai_2")
             .Flag("an_p06_bac_sai_1")
             .Goto("bac")
             .Label("bac_sai_2")
             .Flag("an_p06_bac_sai_2")
             .Goto("bac")
             .Label("bac_goi_y")
             .Flag("an_p06_bac_sai_3")
             .Narrate("Tôi giơ bức tranh lên ngang mắt. Nắng chiếu xuyên qua giấy, và mũi tên đỏ sáng lên.")
             .Narrate("Bé Khoa vẽ đủ từng bậc thang, bậc nào cũng đếm được. Đầu mũi tên chạm đúng bậc thứ bảy.")
             .Goto("bac")
             .Label("bac_7")
             .Sfx("sfx_go_rong")
             .Narrate("Bậc thứ bảy kêu rỗng.")
             .Pause(900)
             .Narrate("Dưới tấm ván là một hộp bánh quy thiếc, khóa hình ngôi sao bảy cánh.")
             .Narrate("Bảy khía trên mặt vòng khớp với bảy cánh. Tôi đặt mặt vòng vào, xoay cánh thứ nhất.");
            // Sáu cánh: mỗi cánh một tiếng động trong nhà; An chào đúng người (suy ra từ những gì đã kể ở P01–P04).
            StarArm(s, "c1", "sfx_tu_go_ket", "Cánh tủ gỗ trong phòng ông kẽo kẹt hé ra.", "“Con chào ông.”", 0,
                "Ổ khóa không nhúc nhích. Cánh tủ lại kẽo kẹt, như có người đang hắng giọng chờ.",
                "Xoay tiếp, không chào");
            StarArm(s, "c2", "sfx_chuong_gio", "Chuông gió trước bàn thờ reo một tiếng.", "“Con chào bà.”", 1,
                "Chuông gió lặng đi, rồi reo lại đúng một tiếng, kiên nhẫn như người đi chùa mỗi sáng.",
                "“Con chào mẹ.”", "“Anh để em cầm quạt nha.”");
            StarArm(s, "c3", "sfx_quat_xoay", "Cái quạt của anh Minh quay một vòng. Phích đã rút từ sáu năm trước.",
                "“Anh để em cầm quạt nha.”", 2,
                "Cái quạt dừng, rồi lại quay một vòng về phía tôi. Đúng kiểu của một người.",
                "“Con chào ba.”", "“Khoa đứng thẳng lưng lên.”");
            StarArm(s, "c4", "sfx_khung_cua_cot", "Khung cửa có vạch đo kêu cót.", "“Khoa đứng thẳng lưng lên.”", 0,
                "Khung cửa kêu cót lần nữa, như có đứa nhỏ đang nhón chân cho cao hơn.",
                "“Con chào ông.”", "“Con chào mẹ.”");
            StarArm(s, "c5", "sfx_co_le", "Hai cái cờ lê trên bàn cha chạm nhau leng keng.", "“Con chào ba.”", 1,
                "Leng keng lần nữa, như tiếng ai gõ cờ lê gọi con ra phụ một tay.",
                "“Anh để em cầm quạt nha.”", "“Con chào bà.”");
            StarArm(s, "c6", "sfx_giot_nuoc", "Vòi nước dưới bếp nhỏ một giọt.", "“Con chào mẹ.”", 2,
                "Thêm một giọt. Dưới bếp, như có ai đang chờ con ngồi xuống cạnh nồi cơm.",
                "“Con chào ông.”", "“Khoa đứng thẳng lưng lên.”");
            s.Narrate("Tôi chào từng người, giọng vẫn vui. Nếu giọng tôi run, có lẽ họ sẽ không dám ở lại thêm giây nào.")
             .Pause(900)
             // Cánh thứ bảy: im lặng tuyệt đối. Mặc định = không chào ai.
             .Label("c7")
             .Choice("Cánh thứ bảy kẹt cứng.", 1,
                 OIf("Chào như mọi lần", "c7_sai", "!STORY_FLAG an_p06_c7_sai"),
                 O("Ấn mạnh. Không chào ai.", "c7_ok"))
             .Label("c7_sai")
             .Flag("an_p06_c7_sai")
             .Narrate("Tôi mở miệng: “Con chào…” Không có cái tên nào theo sau. Tôi không biết phải chào ai.")
             .Goto("c7")
             .Label("c7_ok")
             .Narrate("Tôi ấn mạnh. Mọi âm thanh tắt hẳn, cả tiếng thở của chính tôi.")
             .Pause(2500)
             .Sfx("sfx_khoa_tach")
             .Narrate("Ổ khóa kêu tách. Tiếng xe máy ngoài hẻm ùa trở lại.")
             .Pause(1500)
             .Then("P07_an_a");
            return s;
        }

        // =====================================================================================
        // P07 — Đừng đếm (1B.12, 1B.13)
        // =====================================================================================
        static SceneDefinition P07a()
        {
            var s = NewScene("P07_an_a", "Ba thứ trong hộp thiếc",
                "Hộp thiếc có sổ bìa vải bảy phần, ảnh cả nhà ở trại và mảnh da khắc dấu nêm, mặt sau là lời dặn chữ An ngày "
                + "21/09/2026. Cành hoa sứ tàn hiện trên bậu cửa; An nhớ một bữa cơm nhìn từ cửa bếp rồi ngất.");
            s.Backdrop("bg_hop_thiec", 0).Music(SilentMusic).FadeIn(800)
             .Narrate("Trong hộp có ba thứ. Quyển sổ bìa vải chia bảy phần; phần thứ bảy để trống: “Phần này của người về sau.”")
             .Narrate("Tấm ảnh cả nhà trước hàng rào gỗ ở trại. Và mảnh da gấp tư, dưới đáy hộp.")
             .Narrate("Trong ảnh, tôi chừng năm, sáu tuổi, đứng giữa ông và cha, tay cầm cành hoa sứ trắng, cười toe toét.")
             .Narrate("Sau hàng rào có một con heo nằm ngửa giả chết.")
             .Sfx("sfx_cuoi_vong")
             .Narrate("Tôi bật cười thành tiếng, một mình giữa căn nhà trống. Tiếng cười dội lại từ cầu thang.")
             .Narrate("Nghe như của người khác. Tôi im ngay.")
             .Pause(1200)
             .Narrate("Mảnh da thô, dày, ngả màu trà, cũ hơn mọi thứ trong nhà. Mặt trước khắc hai dấu góc nhọn và bốn vạch nêm.")
             .Narrate("Lướt ngón tay qua, những rãnh khắc lạnh hơn phần da xung quanh.")
             .Backdrop("bg_manh_da_mat_sau")
             .Narrate("Mặt sau là ba dòng mực bi xanh, nét chữ tròn của tôi.")
             .Narrate("“Hai mươi bốn tuổi thì mang vòng về trại. Đường đi nằm ở phần chưa viết.”")
             .Narrate("“Ở đó người ta sẽ hỏi nhà mình còn mấy người. Đừng trả lời.”")
             .Pause(900)
             .Narrate("Ngày 21/09/2026, hôm tôi được thưởng nóng. Chữ của tôi, không sai một nét.")
             .Narrate("Nhưng đầu dòng không gọi ai, cuối dòng không có mặt cười. Tôi lật đi lật lại mảnh da để tìm. Không có.");
            Note(s, "le_hop_chua_tung_mo");
            s.Narrate("Ngoài lề, bút chì: “Hộp chưa từng mở. Chữ của mình ở trong. Bằng đường nào?”")
             .Narrate("Rồi một dòng nhỏ hơn: “Mình viết mà không gửi cho ai trong nhà. Từ bao giờ mình viết cho người ngoài vậy?”")
             .Pause(1500)
             .Backdrop("bg_bau_cua_so")
             .Narrate("Mùi hoa sứ xộc tới. Cái chai trên bậu cửa sổ, lúc nãy còn rỗng, giờ cắm một cành hoa đã tàn hết.")
             .Narrate("Bảy bông rũ xuống, cánh nâu sẫm. Cả bông nụ cũng quắt lại.")
             .Pause(600)
             // Ký ức xẹt qua: chuỗi hình cắt nhanh, không tương tác. Nhật ký không lưu cảnh đếm.
             .Narrate("Miếng cơm cháy ông phần.")
             .Pause(250)
             .Narrate("Mép mưa ở bậc cổng. Bà cười chảy nước mắt vì chiếc nhẫn. Vệt phượng đỏ.")
             .Pause(250)
             .Narrate("Cổ tay bị siết. Chiếc đồng hồ nhanh hai phút. Anh Minh cầm tay tôi chỉ số xe.")
             .Pause(250)
             .Narrate("Thằng út ngủ trong vòng tay tôi. Kiến bò thành vòng. Cái bát ngửa. Sợi vỏ táo không đứt.")
             .Pause(900)
             .Narrate("Tôi đếm bằng ngón tay, y như thằng út trong giấc mơ, sợ buông ra là mất.")
             .Narrate("“Ông, bà, ba, mẹ, anh Minh, bé Khoa. Và mình.” Bảy ngón tay.")
             .Pause(1200)
             .Narrate("Tôi khựng lại. Không phải vì con số lạ, mà vì tôi chợt nhận ra cả đời mình chưa từng đếm nhà mình.")
             .Narrate("Tôi đếm khách, đếm tiền, đếm số lần ông xới cơm. Nhà mình thì chưa.")
             .Narrate("Cái bàn ăn có mấy chỗ? Tôi không nhớ. Cành hoa sứ có mấy bông? Bảy. Cái đó thì tôi nhớ.")
             .Pause(900)
             .Backdrop("bg_nha_cu_bep_2018", 1200)
             .Narrate("Một ký ức khác trồi lên: bữa cơm tối, nhìn từ cửa bếp vào.")
             .Narrate("Quanh bàn có ông, bà, ba, mẹ, anh Minh, bé Khoa, và một con bé ngồi sát nồi cơm. Là tôi.")
             .Pause(1800)
             .Narrate("Vậy ai đang đứng ở cửa bếp?")
             .Pause(1500)
             .Narrate("Mẹ không ngẩng lên: “Đứng đó làm gì, vào ăn đi con.”")
             .Narrate("Con bé ngồi cạnh nồi cơm đáp: “Dạ.”")
             .Pause(1500)
             .Narrate("Tôi cố nhớ khuôn mặt người đứng ở cửa. Một cơn đau như đinh xuyên qua thái dương.")
             .FadeOut(1800)
             .Narrate("Mọi thứ tắt.")
             .Pause(1500)
             .Then("P07_an_b");
            return s;
        }

        static SceneDefinition P07b()
        {
            var s = NewScene("P07_an_b", "Đừng đếm",
                "An tỉnh dậy trên xe khách lúc 13:31 ngày 13/10, mất khoảng 24 giờ; vé in giờ 07:02, đất đỏ dưới móng tay. "
                + "Mảnh da có thêm dòng “Đừng đếm.”. Xe đang đi đúng đường ông vẽ trong sổ bìa vải.");
            s.Backdrop("bg_xe_buyt", 0).Music(SilentMusic).FadeIn(1500)
             .Sfx("sfx_coi_xe")
             .Narrate("Tiếng còi xe đánh thức tôi ở hàng ghế cuối một chiếc xe khách.")
             .Narrate("Ba lô trên đùi, xếp gọn hơn mọi lần tôi tự xếp: quần áo, thuốc, sổ hồng, giấy chứng nhận.")
             .Narrate("Cái bát sứt miệng, sổ bìa vải, tấm ảnh, mảnh da. Dưới móng tay có đất nâu đỏ.")
             .Narrate("Điện thoại báo 13:31, ngày 13/10. Vé xe trong túi áo in giờ 07:02.")
             .Pause(700)
             .Say(A, Hap, "Em ơi, chị lên xe ở đâu ấy nhỉ? Chị ngủ quên mất.")
             .Narrate("Cậu phụ xe nhìn tôi lạ lùng: “Chị lên ở bến từ sáng mà. Nói chuyện vui lắm, còn cho em kẹo.”")
             .Say(A, Thi, "Vui là thế nào?")
             .Narrate("Cậu ta nghĩ một lúc: “Vui hơn bây giờ.”")
             .Pause(1200)
             .Narrate("Tôi mở mảnh da. Dưới ba dòng mực có thêm dòng thứ tư, mực còn mới.")
             .Narrate("“Đừng đếm.” Đầu dòng không gọi ai, không mặt cười.")
             .Pause(1800)
             // Đường của ông: đọc từng mốc và nghe; game cho biết xe đi đúng đường mà không ai giải thích.
             .Label("so")
             .Choice("Mở phần của ông trong sổ bìa vải.", 0,
                 O("Đọc từng mốc, nhắm mắt nghe đường", "so_ok"),
                 O("Nhìn ra cửa sổ tìm mốc", "so_sai"))
             .Label("so_sai")
             .Narrate("Ruộng, cột điện, ruộng. Cảnh trôi nhanh hơn mắt tôi đọc. Đường của ông phải đi bằng trí nhớ.")
             .Goto("so")
             .Label("so_ok")
             .Narrate("Ông vẽ một ngã ba có cây gạo, một cây cầu sắt, một con rãnh chảy ngược chiều đường. Tôi nhắm mắt.")
             .Sfx("sfx_cau_sat")
             .Narrate("Bánh xe dội lên một tràng khi qua cầu sắt, đúng lúc tôi đọc tới dòng “cầu sắt”.")
             .Sfx("sfx_nuoc_rach")
             .Narrate("Rồi tiếng nước róc rách bên đường, ngược chiều tiếng máy xe.")
             .Pause(800);
            Note(s, "ong_oi_dung_duong_ong_ve");
            s.Narrate("“Ông ơi, con mất khoảng 24 giờ. Xe đang đi đúng đường ông vẽ. Con không nhớ đã chọn đi.” ★")
             .Narrate("Rồi tôi vẽ một mặt cười thật to bên cạnh, cho chắc lá thư này là của mình.")
             .FadeOut(1000)
             .Then("P08_an_a");
            return s;
        }

        // =====================================================================================
        // P08 — Người đứng đợi (1B.14, 1B.15, 1B.16) — trên bản đồ làng thật
        // =====================================================================================
        static SceneDefinition P08a()
        {
            var s = NewScene("P08_an_a", "Người đứng đợi ở chữ Rìa",
                "Xe dừng ở chữ Rìa lúc 14:02; Mận đón An với lá thư chữ An dấu bưu điện 21/09/2026 và dặn nói “sáu với một”. "
                + "Bà Sáu bảo “Giống ba.” mà không cười lại; hỏi chuyện nhà, cả quán đồng thanh “Mai trời nắng.” dù trời đang mưa.",
                location: Village);
            s.Backdrop("bg_bien_bo_ria", 0).Music(SilentMusic).FadeIn(900)
             .Banner("Bờ Rìa — 14:02")
             .Narrate("Tài xế dừng ở tấm biển BỜ RÌA: “Tôi chỉ chở tới chữ Rìa thôi.”")
             .Say(A, Sur, "Sao vậy chú?")
             .Narrate("“Vào thì phải ra.” Tôi cảm ơn rối rít, chúc chú thượng lộ bình an. Chú quay xe đi như sợ trễ giờ của ai.")
             .Narrate("Dưới biển có dòng viết tay: “Ruộng không nhận lời xin lỗi.” B.R. Hai chữ trên biên bản của cha.")
             .Pause(700)
             .Narrate("Dưới tấm biển có một cô gái tay dính cám, cầm dù đen. Cô nhìn đồng hồ, rồi nhìn tôi.")
             .Say(CastIds.Man, Neu, "Chị tới trễ hai phút. Xe lịch hai giờ.", StageSide.Right)
             .Say(A, Hap, "Em đợi chị hả?")
             .Narrate("Tôi cười trước khi cô kịp trả lời. Cô tên Mận.")
             .Narrate("Mận đưa tôi một lá thư nhàu, dấu bưu điện ngày 21/09/2026.")
             .Narrate("“Tôi là con nhà Hậu – Thủy. Tôi sẽ lên khi đủ sức, đi chuyến xe chiều. Nhờ người ra đón.” Ký tên: An.")
             .Narrate("Chữ của tôi. Đầu dòng không gọi ai, không mặt cười.")
             .Pause(900)
             .Say(A, Lau, "Để chị cầm dù giùm em nha. Mà em thích ăn gì nhất? Con heo nào hư nhất trại?")
             .Backdrop("", 800)
             .PlaceAt(Player, "village:gate", CardinalDirection.North)
             .PlaceAt(CastIds.Man, "village:gate", CardinalDirection.West)
             .CameraTo("village:gate", 700)
             .Music("bgm_lang_bo_ria")
             .Say(CastIds.Man, Hap, "Con chân trắng hư nhất. Nó giả chết hoài.", StageSide.Right)
             .Say(CastIds.Man, Neu, "Em ra đây đón chuyến hai giờ hơn hai mươi buổi chiều rồi. Đứng đúng chỗ này. Bà Sáu dặn.",
                 StageSide.Right)
             .Narrate("Mận không biết mình vừa kể.")
             .Say(CastIds.Man, Neu, "Ở đây mình nói “sáu với một” nha chị. Đừng nói con số kia.", StageSide.Right)
             .Say(CastIds.Man, Neu, "Lỡ nói là tối phải nấu thêm nồi cháo.", StageSide.Right)
             .Say(A, Hap, "Thế nhà nào có người đi vắng thì nói sao em?")
             .Music(SilentMusic)
             .Pause(1800).Sfx("sfx_dep_keo_dat")
             .Narrate("Mận im rất lâu, lâu tới mức tôi nghe rõ tiếng dép cô kéo trên đất.")
             .Say(CastIds.Man, Sad, "Thì vẫn nói sáu với một.", StageSide.Right)
             .Pause(1200)
             .Music("bgm_lang_bo_ria")
             .MoveTo(Player, "village:well")
             .MoveTo(CastIds.Man, "village:well", async: true)
             .CameraTo("village:well", 600)
             .Narrate("Mận dẫn tôi một vòng làng. Ai cũng gật đầu khi nghe “con nhà Hậu – Thủy”.")
             .Narrate("Bà cụ phơi lúa nhìn tôi: “Cao hơn rồi.”")
             .Say(A, Lau, "Dạ, con ăn nhiều lắm bà ơi!")
             .Narrate("Đến người thứ mười hai, tôi nhận ra chưa ai hỏi tôi lên đây làm gì. Tôi cũng chưa phải nói dối ai.")
             .PlaceAt(CastIds.BaSau, "village:station:quan_ba_sau", CardinalDirection.South)
             .MoveTo(Player, "village:eatery")
             .MoveTo(CastIds.Man, "village:eatery", async: true)
             .CameraTo("village:eatery", 700)
             .Sfx("sfx_mua")
             .Face(CastIds.BaSau, CardinalDirection.North)
             .Pause(600)
             .Say(CastIds.BaSau, Neu, "Giống ba.", StageSide.Right)
             .Narrate("Tôi cười với bà. Lần đầu tiên trong ngày, có một người không cười lại.")
             .Narrate("Bà dọn cơm, đặt thêm một cái bát úp ở ghế trống bên cạnh.")
             .Say(A, Hap, "Bà ơi, bát này để phần ai vậy bà? Con ngồi chỗ đó được không, con thích ngồi gần nồi.")
             .Say(CastIds.BaSau, Neu, "Ngồi đây.", StageSide.Right)
             .Narrate("Bà kéo tay tôi sang ghế khác, nhẹ mà chắc.")
             .Pause(600)
             .Say(CastIds.BaSau, Hap, "Ba con sửa máy cho ông Năm mấy mùa hè. Mẹ con ăn cay giỏi hơn cả quán.", StageSide.Right)
             .Say(CastIds.BaSau, Hap, "Bé Khoa thì đặt tên hết đàn heo.", StageSide.Right)
             .Say(A, Hap, "Ơ, thật hả bà? Vậy hồi đó nhà con lên đây làm gì vậy bà?")
             .Music(SilentMusic).Pause(500)
             .Narrate("Cả quán đồng thanh: “Mai trời nắng.”")
             .Sfx("sfx_mua_to")
             .Narrate("Ngoài trời đang mưa. Không ai lệch nhịp, không ai hụt chữ, đều như hát đồng ca.")
             .Pause(900)
             .Emote(Player, "♪")
             .Say(A, Lau, "Cả quán dự báo chung luôn hả, vui ghê!")
             .Narrate("Mọi người cười theo, như thở phào. Bà Sáu thì không.")
             .Pause(1200)
             .Say(CastIds.BaSau, Neu, "Xe về huyện chỉ có anh Tư. Ba giờ chiều mai. Hôm nay nó đi ăn cưới.", StageSide.Right)
             .Narrate("Vậy là tôi ở lại một đêm, trong phòng cạnh bếp lò ở trại ông Năm.")
             .Then("P08_an_b");
            return s;
        }

        static SceneDefinition P08b()
        {
            var s = NewScene("P08_an_b", "Cả quán dự báo thời tiết",
                "Chạng vạng, giữa đồng có một chỗ gió không kêu và một đốm sáng đứng yên; ở trại, tay An tự biết then cổng. "
                + "Tối rửa bát, An lỡ nói thật một câu. Đêm, An ngã vào thành lò và đọc trang cuối của cha: “số quay vào trong”.",
                location: Village);
            s.Backdrop("bg_dong_sau_rao", 900).Music(SilentMusic)
             .Sfx("sfx_gio_dong")
             .Narrate("Chạng vạng, Mận dắt tôi ra trại. Gió qua cánh đồng sau hàng rào nghe như tiếng sóng.")
             .Narrate("Chỉ có một chỗ giữa đồng gió đi qua mà cỏ không xào xạc.")
             .Narrate("Tôi nhìn theo chỗ im ấy mới thấy một đốm sáng nhỏ như đom đóm. Không bay. Đứng yên giữa gió.")
             .Narrate("Cả cánh đồng ngả vàng như lúa sắp gặt.")
             .Say(A, Sur, "Mận ơi, em có nghe gì không?")
             .Say(CastIds.Man, Hap, "Đồng mình chiều nào cũng đẹp vậy đó chị.", StageSide.Right)
             .Pause(1800)
             .Backdrop("bg_trai_chang_vang")
             .Narrate("Tay tôi tự nhấc then cổng lên một chút rồi mới kéo.")
             .Narrate("Tấm ván thứ ba kêu đúng lúc tôi biết nó sẽ kêu.")
             .Narrate("Công tắc đèn nhà kho nằm thấp, sau cánh cửa, đúng tầm tay đứa bé.")
             .Sfx("sfx_xo_cam")
             .Narrate("Dưới máng, con heo chân trắng nằm ngửa giả chết. Nghe tiếng xô cám là lật dậy.")
             .Say(A, Lau, "Trời ơi, nó diễn đạt ghê chưa kìa!")
             .Say(CastIds.Man, Hap, "Nó diễn với ai cũng vậy đó chị.", StageSide.Right)
             .Narrate("Tôi cười thành tiếng cùng Mận, cười đến cúi gập người. Mận vui lắm.")
             .FadeOut(700)
             // Tối ở quán: lần lỡ duy nhất của chương. Không nhạc, không camera nhấn; NPC chỉ đưa cái bát tiếp theo.
             .Backdrop("", 0)
             .PlaceAt(Player, "village:eatery", CardinalDirection.North)
             .PlaceAt(CastIds.BaSau, "village:station:quan_ba_sau", CardinalDirection.South)
             .CameraTo("village:eatery", 0)
             .FadeIn(700)
             .Narrate("Tối đó, ngồi rửa bát giúp bà Sáu, tôi kể chuyện con heo giả chết cho mấy đứa nhỏ hàng xóm.")
             .Narrate("Lần thứ ba. Vẫn cười đúng chỗ.")
             .Say(A, Portraits.AnStare, "Chị kể chuyện này ba lần rồi. Chị mệt.")
             .Pause(1500)
             .Narrate("Mấy đứa nhỏ im bặt.")
             .Say(A, Lau, "Chị đùa đó! Đứa nào muốn nghe lần thứ tư nào?")
             .Narrate("Tụi nhỏ cười theo. Bà Sáu không cười. Bà chỉ đưa tôi cái bát tiếp theo.")
             .Pause(1200)
             .Narrate("Tôi rửa cái bát ấy rất lâu, vì câu vừa rồi không phải đùa.")
             .Pause(900)
             .Say(CastIds.BaSau, Neu, "Con gà con lạc vô bếp. Mang ra chuồng giùm bà.", StageSide.Right)
             .Narrate("Tôi đưa hai tay ra nhận, lòng bàn tay ngửa lên. Tôi chưa từng cầm thứ gì theo cách đó.")
             .FadeOut(900)
             .Backdrop("bg_phong_bep_lo", 0).Music(SilentMusic).FadeIn(900)
             .Narrate("Đêm ấy, trong phòng cạnh bếp lò, tôi viết thư. Một mình, như mọi tối.");
            Note(s, "nha_minh_oi_xom_dem_cho");
            s.Narrate("“Nhà mình ơi, xóm này không đếm người. Xóm đếm chỗ. Giống nhà mình :)” ★")
             .Narrate("“Mẹ ơi, cái quán mẹ hay ăn cay ấy, cả quán nói cùng một câu, đều như tập.” ★")
             .Narrate("“Bà Sáu là người duy nhất không cười khi con vỗ tay.” ☾")
             .Narrate("“Bà ơi, giữa đồng có một chỗ gió không kêu. Có đom đóm. Không bay.” ☾ Ra lề: “Ấm.”")
             .Pause(700)
             .Narrate("Lần đầu tiên tôi muốn vẽ một mặt cười mà không phải để cho ai xem.")
             .Narrate("Ra lề, bút chì: “Cười: đúng lúc. Có ai nghe ra mình cười giả không: chưa.”")
             .Narrate("Rồi một dòng không gửi ai: “Hóa ra mình cũng nói được như thế. Không ai chết cả.”")
             .Pause(1200)
             .Narrate("Tôi cố nhớ vì sao mình lên xe. Đầu choáng.")
             .Sfx("sfx_va_lo")
             .Narrate("Vai tôi đập vào thành lò. Mảng muội bong ra. Cuốn sổ bìa vải rơi úp ở trang cuối.")
             .Narrate("Nét chữ cha: “Bờ Rìa, hè 2017. Ông Năm cho ở phòng cạnh bếp lò. Mảnh da lấy ra mang về.”")
             .Narrate("“Sau này ai mang trả thì nhớ: số quay vào trong.”")
             .Flag("an_p08_trang_cuoi_cua_cha");
            Note(s, "trang_cuoi_cua_cha");
            s.Pause(900)
             .Narrate("Tôi ôm cuốn sổ ngủ tới sáng, như hồi bé ôm tay ba.")
             .FadeOut(1200)
             .Then("P08_an_c");
            return s;
        }

        static SceneDefinition P08c()
        {
            var s = NewScene("P08_an_c", "Cái khe, hai lượt hát và cột “Chị An”",
                "An từ chối trông trại, rồi mở cái khe ở thành lò bằng mảnh da (dấu khắc quay vào trong) và hai lượt hát chờ. "
                + "Hốc tường có bảy cột chiều cao; cột “Chị An” được đo tới 13/10/2026. An ở lại và ký nhận trại.",
                location: Village);
            s.Backdrop("bg_phong_bep_lo", 0).Music(SilentMusic).FadeIn(900)
             .Narrate("Sáng hôm sau, bà Sáu, Tám Vá và ông Tồn lên trại với nồi cháo.")
             .Sfx("sfx_go_ba_cai")
             .Say(CastIds.TamVa, Hap, "Còn tốt.", StageSide.Right)
             .Say(CastIds.BaSau, Neu, "Con ở lại trông trại giùm ông Năm nghen. Trại bỏ không lâu rồi, heo nó buồn.", StageSide.Right)
             .Say(A, Hap, "Con cảm ơn bà nhiều lắm. Mà con còn phải về đóng tiền điện cho bà chủ trọ.")
             .Say(A, Lau, "Bà hiền lắm, con mà trễ là bà lo.")
             .Narrate("Không ai nài. Bà Sáu để nồi cháo lại. Ông Tồn ghi thêm một dòng vào sổ.")
             .Sfx("sfx_buoc_xuong_doc")
             .Narrate("Tôi nghe ba người đi xuống dốc mà không ai nói với ai câu nào.")
             .Narrate("Người ta không nài là vì người ta đã biết trước câu trả lời. Tôi thì chưa biết.")
             .Pause(1500)
             // (1) Tìm khe.
             .Narrate("Nắng sáng rọi xiên vào thành lò, đúng chỗ vai tôi đập tối qua.")
             .Label("tim")
             .Choice("Thành lò ám muội.", 0,
                 O("Gõ thử chỗ vai mình đập vào", "tim_ok"),
                 O("Gõ thử góc lò", "tim_sai"))
             .Label("tim_sai").Sfx("sfx_go_dac")
             .Narrate("Cộc. Gạch đặc, ấm dần lên vì nắng.")
             .Goto("tim")
             .Label("tim_ok").Sfx("sfx_go_rong")
             .Narrate("Có một chỗ kêu rỗng. Tôi cạo muội. Một cái khe hiện ra, viền kim loại lạnh, rộng chừng một bàn tay.")
             // (2) Chọn vật. Mỗi món một câu riêng; sai 4 lần: "lạnh này quen"; sai 6 lần: nhớ ra mảnh da. Mặc định = mảnh da.
             .Label("vat")
             .Choice("Thử món gì?", 6,
                 OIf("Chìa khóa nhà", "v_chia", "!STORY_FLAG an_p08_v_chia"),
                 OIf("Thẻ căn cước", "v_cccd", "!STORY_FLAG an_p08_v_cccd"),
                 OIf("Vé xe", "v_ve", "!STORY_FLAG an_p08_v_ve"),
                 OIf("Thìa của bà Sáu", "v_thia", "!STORY_FLAG an_p08_v_thia"),
                 OIf("Cờ lê chú Tám để quên", "v_cole", "!STORY_FLAG an_p08_v_cole"),
                 OIf("Hóa đơn tiền điện", "v_hoadon", "!STORY_FLAG an_p08_v_hoadon"),
                 O("Mảnh da", "v_da"))
             .Label("v_chia").Flag("an_p08_v_chia")
             .Say(A, Hap, "Không thích chìa khóa hả?")
             .Narrate("Dày quá, chỉ lọt được cái đầu.")
             .Goto("vat_sai")
             .Label("v_cccd").Flag("an_p08_v_cccd")
             .Say(A, Thi, "Thẻ căn cước cũng không?")
             .Narrate("Trượt vào nửa chừng rồi kẹt. Căn cước của tôi không có giá trị ở đây.")
             .Goto("vat_sai")
             .Label("v_ve").Flag("an_p08_v_ve")
             .Say(A, Hap, "Vé xe nè, giờ in đẹp lắm đó.")
             .Narrate("Mềm quá, gập đôi ngay ở miệng khe.")
             .Goto("vat_sai")
             .Label("v_thia").Flag("an_p08_v_thia")
             .Say(A, Lau, "Thìa của bà Sáu nha, múc cháo ngon lắm.")
             .Narrate("Lọt vào, chạm đáy, rồi không có gì. Tôi lau thìa, để lại cạnh nồi cháo.")
             .Goto("vat_sai")
             .Label("v_cole").Flag("an_p08_v_cole")
             .Say(A, Hap, "Cờ lê của chú Tám, chắc có tên chiêu võ gì đó đàng hoàng.")
             .Narrate("Lắc lư trong khe, như ai đó đang lắc đầu.")
             .Goto("vat_sai")
             .Label("v_hoadon").Flag("an_p08_v_hoadon")
             .Say(A, Lau, "Hóa đơn tiền điện thì chắc không rồi ha.")
             .Narrate("Đúng là không. Cái khe này còn khó tính hơn bà chủ trọ.")
             .Goto("vat_sai")
             .Label("vat_sai")
             .IfGoto("STORY_FLAG an_p08_vat_sai_6", "vat")
             .IfGoto("STORY_FLAG an_p08_vat_sai_5", "vat_sai_6")
             .IfGoto("STORY_FLAG an_p08_vat_sai_4", "vat_sai_5")
             .IfGoto("STORY_FLAG an_p08_vat_sai_3", "vat_sai_4")
             .IfGoto("STORY_FLAG an_p08_vat_sai_2", "vat_sai_3")
             .IfGoto("STORY_FLAG an_p08_vat_sai_1", "vat_sai_2")
             .Flag("an_p08_vat_sai_1").Goto("vat")
             .Label("vat_sai_2").Flag("an_p08_vat_sai_2").Goto("vat")
             .Label("vat_sai_3").Flag("an_p08_vat_sai_3").Goto("vat")
             .Label("vat_sai_4").Flag("an_p08_vat_sai_4")
             .Narrate("Ngón tay tôi chạm vào viền khe. Lạnh. Cái lạnh này quen.")
             .Goto("vat")
             .Label("vat_sai_5").Flag("an_p08_vat_sai_5").Goto("vat")
             .Label("vat_sai_6").Flag("an_p08_vat_sai_6")
             .Narrate("Tôi nhớ ra cái lạnh này: những rãnh khắc trên mảnh da. Rồi trang cuối của cha: số quay vào trong.")
             .Goto("vat")
             // (3) Chiều: mặt khắc quay vào lòng lò. Sai thì kẹt, trang của cha hiện lại.
             .Label("v_da")
             .Label("chieu")
             .Choice("Đưa mảnh da vào thế nào?", 0,
                 O("Mặt khắc dấu quay vào lòng lò", "chieu_ok"),
                 OIf("Mặt có chữ của mình quay vào lòng lò", "chieu_sai", "!STORY_FLAG an_p08_chieu_sai"))
             .Label("chieu_sai").Flag("an_p08_chieu_sai").Sfx("sfx_ket")
             .Narrate("Kẹt cứng giữa chừng. Tôi rút ra. Nét chữ cha hiện lại trong đầu: “số quay vào trong.”")
             .Goto("chieu")
             .Label("chieu_ok")
             .Narrate("Mảnh da trượt vào êm như chưa từng bị lấy ra.")
             // (4) Chờ: ấn hay rút đều nhả về đầu; buông tay và hát hai lượt (vừa tròn hai phút) thì mở.
             .Label("cho")
             .Sfx("sfx_tach_hai")
             .Narrate("Một tiếng tách. Hai. Rồi thôi.")
             .Choice("Mảnh da thò ra khỏi tường như một lá thư nhét dở.", 2,
                 OIf("Ấn thêm cho vào hẳn", "cho_an", "!STORY_FLAG an_p08_cho_an"),
                 OIf("Rút ra, đưa vào lại", "cho_rut", "!STORY_FLAG an_p08_cho_rut"),
                 O("Buông tay, ngồi xuống, hát bài mẹ hay hát sai lời", "hat_1"))
             .Label("cho_an").Flag("an_p08_cho_an")
             .Sfx("sfx_tach_nha")
             .Narrate("Không nhúc nhích. Rồi hai tiếng tách nhả ngược lại, như ai vừa mở hai ngón tay đang nắm.")
             .Goto("cho_sai")
             .Label("cho_rut").Flag("an_p08_cho_rut")
             .Sfx("sfx_tach_nha")
             .Narrate("Hai tiếng tách nhả ngược. Tôi đưa mảnh da vào lại.")
             .Goto("cho_sai")
             .Label("cho_sai")
             .IfGoto("STORY_FLAG an_p08_cho_sai_2", "cho")
             .IfGoto("STORY_FLAG an_p08_cho_sai_1", "cho_sai_2")
             .Flag("an_p08_cho_sai_1")
             .Goto("cho")
             .Label("cho_sai_2")
             .Flag("an_p08_cho_sai_2")
             .Narrate("Bảy buổi sáng trước gương, tôi chỉ học kỹ đúng một việc: chừa chỗ cho tiếng vọng về.")
             .Goto("cho")
             .Label("hat_1")
             .Music("bgm_bai_hat_me_sai_loi")
             .Narrate("Tôi buông tay, ngồi xuống sàn, hát nhỏ bài hát mẹ hay hát sai lời.")
             .Pause(3000)
             .Narrate("Hết lượt thứ nhất. Trong tường chưa có tiếng gì.")
             .Choice("Còn ngồi đó.", 0,
                 O("Hát thêm một lượt nữa", "hat_2"),
                 O("Đứng dậy ấn thử", "cho_an"))
             .Label("hat_2")
             .Narrate("Lượt thứ hai, tôi hát sai đúng những chỗ mẹ hát sai.")
             .Pause(3000)
             .Music(SilentMusic)
             .Narrate("Hết lượt thứ hai thì vừa tròn hai phút.")
             .Pause(900)
             .Sfx("sfx_tach_bon")
             .Narrate("Bên trong tường: ba tiếng tách, rồi một tiếng nữa. Viên gạch trượt sang ngang.")
             .Pause(1200)
             .Backdrop("bg_hoc_tuong_bay_cot")
             .Narrate("Hốc tường trát vôi trắng, bảy cột vạch bút chì. Đầu cột là nét chữ tròn của Khoa.")
             .Narrate("Ông. Bà. Ba. Mẹ. Anh Minh. Khoa. Chị An.")
             // Cột "Chị An": đọc cột của mình rồi mới áp đầu vào tường. Khi bỏ qua, lựa chọn khoá rơi về lựa chọn đầu còn mở.
             .Label("cot")
             .Choice("Bảy cột vạch bút chì.", 2,
                 OIf("Đọc cột “Chị An”", "cot_an", "!STORY_FLAG an_p08_doc_cot_an"),
                 OIf("Đọc các cột của cả nhà", "cot_nha", "!STORY_FLAG an_p08_doc_cot_nha"),
                 OIf("Quay lưng, áp đầu vào tường", "cot_ap", "STORY_FLAG an_p08_doc_cot_an", "Đọc cột của mình trước đã."))
             .Label("cot_nha").Flag("an_p08_doc_cot_nha")
             .Narrate("Cột của ông và bà vạch nào cũng ngang nhau. Người lớn có cao thêm đâu.")
             .Narrate("Chắc Khoa vẫn kẻ cho đủ, để không ai bị bỏ sót. Các cột dừng ở 2017.")
             .Goto("cot")
             .Label("cot_an")
             .Flag("an_p08_doc_cot_an")
             .Narrate("Các cột dừng ở 2017. Trừ cột của tôi.")
             .Narrate("2018 trống. Rồi 2019, 2020… mỗi năm cao hơn một chút. Nét không còn là của Khoa.")
             .Narrate("Vạch cuối bằng bút bi xanh: 13/10/2026. Hôm qua.")
             .Goto("cot")
             .Label("cot_ap")
             .Narrate("Tôi quay lưng, áp đầu vào tường.")
             .Pause(1800)
             .Narrate("Khớp.")
             .Pause(2200)
             .Narrate("Tôi không cười. Không có ai để cười cho. Tôi đứng như vậy rất lâu.");
            Note(s, "nha_minh_oi_bay_cot");
            s.Narrate("“Nhà mình ơi, cái hốc có đủ bảy cột, Khoa kẻ cho cả nhà. Cột của An được đo tiếp tới hôm qua.”")
             .Narrate("“Năm 2018 trống. Vạch cuối bút bi xanh.” ★")
             .Narrate("Ngoài lề: “Ai đã đo An suốt những năm An không ở đây?” Không có mặt cười.")
             .Pause(1500)
             .Backdrop("bg_cong_trai").Sfx("sfx_coi_xe_may")
             .Narrate("Ba giờ chiều, anh Tư tới cổng trong bộ đồ đi ăn cưới hôm qua.")
             .Narrate("Tôi đưa anh tiền xe, coi như tiền chờ, kèm một nắm kẹo.")
             .Say(CastIds.AnhTu, Neu, "Không đi à?", StageSide.Right)
             .Say(A, Hap, "Dạ, chưa đi được anh ơi.")
             .Say(CastIds.AnhTu, Hap, "Ở đây chưa đi được cũng là một kiểu đi.", StageSide.Right)
             .Narrate("Anh Tư nhét kẹo vào túi, rồi chạy mất.")
             .FadeOut(800)
             .Backdrop("", 0)
             .PlaceAt(Player, "village:eatery", CardinalDirection.North)
             .PlaceAt(CastIds.BaSau, "village:station:quan_ba_sau", CardinalDirection.South)
             .PlaceAt(CastIds.OngTon, "village:npc:ong_ton", CardinalDirection.East)
             .CameraTo("village:eatery", 0)
             .Music("bgm_lang_bo_ria")
             .FadeIn(800)
             .Narrate("Chiều đó tôi xuống quán. Bà Sáu đang rửa bát.")
             .Say(A, Hap, "Bà ơi, cái trại. Con nhận ạ. Con thích chỗ này.")
             .Narrate("Bà không hỏi vì sao. Câu “con thích chỗ này” là thật, nhưng không phải lý do.")
             .Pause(700)
             .Narrate("Bà Sáu đưa hồ sơ bàn giao. Dòng người nhận bỏ trống; bên cạnh có ai viết bút chì “(sáu với một)”.")
             .Narrate("Góc trên đóng con dấu: hai dấu móc, bốn vạch nêm.")
             .MoveTo(CastIds.OngTon, "village:eatery")
             .Narrate("Ông Tồn chép dấu nêm vào sổ, không đọc thành tiếng.")
             .Say(A, Hap, "Chữ ông đẹp ghê. Ông viết bao lâu mới được vậy ông?")
             .Say(CastIds.OngTon, Sur, "Đẹp gì đâu… Tôi giữ kho này ba mươi năm rồi, quen tay thôi.", StageSide.Right)
             .Narrate("Ông đỏ mặt, rồi kể thêm cho tôi nghe chuyện kho, lâu hơn ông định.")
             .Pause(600)
             .Narrate("Tôi ký vào hồ sơ. Mực thấm chậm, như tờ giấy đang cân nhắc có nên nhận tôi không.")
             .Pause(1200)
             .Narrate("Lý do thật thì tôi chỉ viết vào sổ, tối hôm ấy.");
            Note(s, "nha_minh_oi_phai_o");
            s.Narrate("“Nhà mình ơi, có người đã đo An suốt những năm An không có ở đây. Muốn biết là ai thì phải ở.”")
             .Narrate("“Mà muốn ở yên thì phải cười :)”")
             .Pause(900)
             .MoveTo(Player, "village:gate", async: true)
             .MoveTo(CastIds.OngTon, "village:gate")
             .FadeOut(1000)
             .Flag("prologue_done")
             .Do(NarrativeEffectKind.Custom, NarrativeCustomIds.GotoMapPrefix + "Farm")
             .End();
            return s;
        }
    }
}
