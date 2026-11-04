using System.Collections.Generic;

namespace PigTycoon.Core
{
    /// <summary>
    /// Chương 0 — Giấy chứng nhận, bản Khoa (story bible Phần 1, 1.0–1.16; Phần 4, P01–P08).
    /// P01–P07 play on full-screen backdrops (VN "CG"); P08 uses the live village map for Bờ Rìa and backdrops for the farm room.
    /// Rules kept here: first person through the narrator; each death is one line alone on a static backdrop, no music, no joke;
    /// "tôi có giấy" only at 1.0, 1.3 and the chapter card; no villager says the number or names the thing in the sky; a silent
    /// beat after every mystery; Khoa speaks only with the neutral portrait. Every puzzle loop terminates when the first enabled
    /// option is always taken (headless test), and its default option is the solution (skip).
    /// </summary>
    public static class NarrativeContentPrologueKhoa
    {
        const string K = CastIds.Khoa;
        const string N = Portraits.Neutral;
        const StageSide R = StageSide.Right;

        /// <summary>Every backdrop id this file uses (Argon paints them), in order of first appearance.</summary>
        public static readonly string[] Backdrops =
        {
            // P01
            "bg_the_chuong_0", "bg_so_hai_cot", "bg_hem_nha_cu", "bg_san_chua_hoa_su", "bg_cau_thang_hoi_nho",
            "bg_nha_cu_phong_khach_chieu", "bg_nha_cu_khung_cua_vach_do", "bg_nha_cu_phong_minh_cai_quat", "bg_nha_cu_bua_com",
            // P02
            "bg_nha_cu_tu_tho_ve_so", "bg_nha_cu_cong_mua_bong_may", "bg_cau_thang_chieu_nghi_toi", "bg_nen_tinh_ngay_mat",
            "bg_cau_thang_chieu_nghi_ngay", "bg_nha_cu_hu_gao", "bg_hem_hoa_phuong_rung", "bg_nha_cu_bua_com_ghe_trong",
            // P03
            "bg_goc_pho_sang_som", "bg_bac_da_chua", "bg_bien_ban_giay", "bg_tru_so_cong_an_phuong", "bg_hem_nha_thuoc_sap_xoi",
            "bg_co_tay_vet_bam", "bg_hanh_lang_ban_nga",
            // P04
            "bg_giay_bao_hoc_bong", "bg_guong_nha_tam_mo", "bg_tranh_gia_dinh_em_dong_khung", "bg_tui_ao_hat_dau_den",
            "bg_gieng_sau_nha_kien", "bg_hien_nha_nen_cup_dien", "bg_so_trang_dau_nam_dong", "bg_phong_ngu_khoa_toi",
            "bg_bep_me_gia_bat", "bg_bau_cua_vong_tron_sach",
            // P05
            "bg_phong_tro", "bg_van_phong_thuong_nong", "bg_phong_tro_guong_nha_tam", "bg_nga_tu_sang", "bg_nen_den",
            "bg_nga_tu_nguoi_ao_mua", "bg_phong_benh_giac_mo", "bg_phong_benh_tinh_day", "bg_ho_so_benh_an", "bg_phong_tro_cua_so_sang",
            // P06
            "bg_nghia_trang", "bg_so_sau_ngay_mat", "bg_phieu_thu_suat_mo", "bg_nghia_trang_cong_cay_su", "bg_nha_cu_cua_chinh",
            "bg_nha_cu_phong_khach_bui", "bg_nha_cu_ban_lam_viec_cha", "bg_vong_co_cua_me", "bg_nha_cu_tuong_cham_sang",
            "bg_anh_tet_2018_cham_sang", "bg_tranh_gia_dinh_em_ban_goc", "bg_cau_thang_muoi_bon_bac", "bg_hop_thiec_khoa_sao",
            // P07
            "bg_hop_thiec_mo", "bg_so_bia_vai_bay_phan", "bg_anh_ca_nha_o_trai", "bg_manh_da_dau_nem", "bg_manh_da_mat_sau",
            "bg_cau_thang_chieu_nghi_hoa_tan", "bg_nha_cu_bua_com_tu_cua_bep", "bg_xe_khach_hang_ghe_cuoi", "bg_manh_da_dung_dem",
            "bg_xe_khach_cua_so_ruong", "bg_duong_nga_ba_cay_gao", "bg_duong_cau_sat", "bg_duong_ranh_nuoc_chay_nguoc",
            // P08
            "bg_bien_bo_ria_xe_khach", "bg_la_thu_gui_quan_ba_sau", "bg_dong_vang_chang_vang", "bg_trai_cong_then_xe",
            "bg_trai_mang_heo_gia_chet", "bg_phong_canh_bep_lo_toi", "bg_phong_canh_bep_lo_dem_den_dau",
            "bg_so_bia_vai_trang_cuoi_cha", "bg_phong_canh_bep_lo_sang", "bg_thanh_lo_vet_nang", "bg_hoc_tuong_bay_cot",
            "bg_trai_cong_ban_ngay_xe_om", "bg_ho_so_ban_giao_trai", "bg_phong_canh_bep_lo_chieu",
        };

        public static IEnumerable<SceneDefinition> Scenes()
        {
            yield return P01();
            yield return P02();
            yield return P03();
            yield return P04();
            yield return P05a();
            yield return P05b();
            yield return P05c();
            yield return P06a();
            yield return P06b();
            yield return P06c();
            yield return P07();
            yield return P08a();
            yield return P08b();
            yield return P08c();
            yield return P08d();
        }

        // ---------------------------------------------------------------- helpers

        static SceneDefinition Scene(string id, string title, string recap, string location = "")
            => new SceneDefinition
            {
                Id = id, Title = title, Recap = recap, Location = location,
                Condition = "PROTAGONIST Khoa", Once = true, Skippable = true,
            };

        /// <summary>Option shown only while <paramref name="condition"/> holds (puzzle options disappear once tried).</summary>
        static NarrativeOption OptIf(string text, string label, string condition)
            => new NarrativeOption { Text = text, GotoLabel = label, Condition = condition };

        static SceneDefinition Custom(SceneDefinition s, string id) => s.Do(NarrativeEffectKind.Custom, id);
        static string Notebook(string entry) => NarrativeCustomIds.NotebookPrefix + entry;
        static string Reflection(string portrait) => NarrativeCustomIds.ReflectionPrefix + portrait;

        /// <summary>A death: silence, a static backdrop, one line alone, no comment (story bible 2.3, design note 7).</summary>
        static SceneDefinition DeathLine(SceneDefinition s, string line)
            => s.Music("").FadeOut(900).Backdrop("bg_nen_tinh_ngay_mat", 0).FadeIn(900).Pause(900)
                .Narrate(line)
                .Pause(2200);

        // ---------------------------------------------------------------- P01 — Một nhà bình thường

        static SceneDefinition P01()
        {
            var s = Scene("P01_khoa", "Một nhà bình thường",
                "Khoa tự giới thiệu: anh có giấy chứng nhận tâm thần và một cuốn sổ hai cột “Thấy” / “Có thật”. " +
                "Hồi tưởng căn nhà cuối hẻm (ông, bà, cha, mẹ, anh Minh, bé An), cành hoa sứ hồi bé Khoa cắm trong chai trên bậu cửa sổ chiếu nghỉ, và những mùa hè ở trại bác Năm.");
            s.Trigger = NarrativeTrigger.NewGame;
            s.Priority = 10;

            s.Music("").Backdrop("bg_the_chuong_0", 0)
             .Banner("Chương 0 — Giấy chứng nhận", 2400)
             .Banner("“Tôi có giấy chứng nhận. Những gì dưới đây, xin đọc kèm giấy.”", 3600)
             .Backdrop("bg_so_hai_cot", 900).Pause(600)
             .Narrate("Tôi tên Khoa. Tôi điên.")
             .Narrate("Tôi có giấy: bệnh án, chẩn đoán tâm thần phân liệt, chữ ký bác sĩ, dấu đỏ của bệnh viện.")
             .Narrate("Trong số những người tôi quen, tôi là người duy nhất có giấy chứng nhận mình không bình thường.")
             .Narrate("Những người còn lại chỉ là chưa đi khám.")
             .Pause(600)
             .Narrate("Nên tôi ghi lại hết, vào một cuốn sổ chia hai cột: “Thấy” và “Có thật”. Cột bên trái luôn dài hơn.")
             .Narrate("Có vài thứ nằm ở cột bên phải. Tôi vẫn chưa quyết định được thứ nào đáng sợ hơn.")
             .Pause(900)

             .FadeOut(700).Music("bgm_nha_cu_ngay_xua").Backdrop("bg_hem_nha_cu", 0).FadeIn(900)
             .Sfx("sfx_cau_thang_go_cot_ket")
             .Narrate("Nhà tôi ở cuối một con hẻm, ba tầng, cầu thang gỗ kêu cót két như phàn nàn về cân nặng từng người.")
             .Narrate("Ông nội Lâm luôn nói mình ăn ít, rồi xới nốt phần của người vừa đứng dậy. Bà nội Sương đi chùa mỗi sáng.")
             .Narrate("Cha Hậu sửa máy, tay thơm mùi dầu. Mẹ Thủy gọt táo để vỏ nối thành một sợi dài không đứt.")
             .Narrate("Anh Minh hơn tôi hai tuổi. Bé An kém tôi sáu tuổi, cười nhiều đến mức mẹ bảo nó cười thay phần anh.")
             .Pause(700)

             .FadeOut(500).Backdrop("bg_san_chua_hoa_su", 0).FadeIn(700)
             .Sfx("sfx_chuong_chua_xa")
             .Narrate("Hồi nhỏ tôi theo bà đi chùa. Bà khấn lâu, tôi đi nhặt hoa sứ rụng dưới gốc cây già.");
            Custom(s, NarrativeCustomIds.TutorialPrefix + "interact");
            s.Choice("Sân chùa", 0, SceneDefinition.Opt("Nhặt một cành hoa sứ còn nguyên"))
             .Backdrop("bg_cau_thang_hoi_nho")
             .Narrate("Tôi cắm nó vào cái chai nước mắm nhỏ, đặt trên bậu cửa sổ ở chiếu nghỉ cầu thang.")
             .Narrate("Lớn lên thì thôi. Không ai còn nhớ cái chai ấy đi đâu. Kể cả tôi.")
             .Pause(800)

             .FadeOut(500).Backdrop("bg_nha_cu_phong_khach_chieu", 0).FadeIn(700)
             .Banner("Một buổi chiều, năm 2018", 1600)
             .Label("viec")
             .Choice("Trước bữa cơm", 0,
                 OptIf("Gọi bé An vào ăn", "goi_an", "!STORY_FLAG p01_goi_an"),
                 OptIf("Giành cái quạt với anh Minh", "quat", "!STORY_FLAG p01_quat"))
             .Goto("ngoi")

             .Label("goi_an")
             .Backdrop("bg_nha_cu_khung_cua_vach_do")
             .Narrate("An kiễng chân sát khung cửa, cây bút chì đặt ngang đỉnh đầu: “Anh ký làm chứng đã! Em cao thêm.”")
             .Say(K, N, "Ăn xong anh ký.")
             .Narrate("Nó cười như vừa thắng kiện, rồi chạy vào bếp trước tôi.")
             .Flag("p01_goi_an")
             .Goto("viec")

             .Label("quat")
             .Backdrop("bg_nha_cu_phong_minh_cai_quat")
             .Sfx("sfx_quat_cu_quay")
             .Narrate("Phòng Minh nóng như cái nồi. Tôi xoay cái quạt về phía mình, Minh xoay lại. Cái quạt nghe lời cả hai.")
             .Narrate("Mẹ gọi vọng lên: “Hai đứa xuống ăn! Cái quạt không ăn được đâu!”")
             .Flag("p01_quat")
             .Goto("viec")

             .Label("ngoi")
             .Backdrop("bg_nha_cu_bua_com")
             .Sfx("sfx_bat_dua")
             .Narrate("Mẹ đẩy nồi cơm về phía tôi: “Thằng này ăn chậm, ngồi xa là hết.”")
             .Narrate("Mùa hè nào cả nhà cũng lên “trại của bác Năm” ở một vùng quê rất xa.")
             .Narrate("Ông bảo đường lên đó “phải đi bằng trí nhớ chứ không đi bằng bản đồ”. Ông chưa đùa bao giờ.")
             .Pause(800)
             .Narrate("Nếu ai hỏi nhà tôi có gì lạ, tôi sẽ nghĩ rất lâu rồi trả lời: không có gì. Đó là câu trả lời thật.")
             .Pause(1000)
             .FadeOut(900)
             .Then("P02_khoa")
             .End();
            return s;
        }

        // ---------------------------------------------------------------- P02 — Những ngày may mắn

        static SceneDefinition P02()
        {
            var s = Scene("P02_khoa", "Những ngày may mắn",
                "Tháng Bảy 2018 ông trúng vé số đuôi 24, mép mưa bóng mây dừng ở bậc cổng, cành hoa sứ bảy bông (sáu nở, một nụ) hiện trên bậu cửa sổ; ông mất sáng 17/07/2018. " +
                "Cuối tháng Tám bà tìm lại nhẫn cưới, cánh phượng dồn thành vệt về cổng, cành hoa lại hiện; bà dặn Khoa sáng mai bảy giờ ra chân bậc đá chùa đỡ bà.");

            s.Music("bgm_nha_cu_ngay_xua").Backdrop("bg_nha_cu_tu_tho_ve_so", 0).FadeIn(700)
             .Banner("Tháng Bảy · 2018", 1600)
             .Narrate("Tháng Bảy năm 2018, ông nội trúng giải khuyến khích xổ số, đủ để khao cả nhà một bữa lẩu.")
             .Narrate("Tờ vé đuôi 24. Ông bảo “số đẹp”, rồi dán lên tủ thờ cạnh ảnh cụ cố, chắc để cụ cũng được khoe.")
             .Backdrop("bg_nha_cu_cong_mua_bong_may")
             .Sfx("sfx_mua_bong_may")
             .Narrate("Ba hôm sau, giữa trưa, có cơn mưa bóng mây. Hàng xóm cười: “mưa con nít”, năm phút là tạnh.")
             .Narrate("Tôi để ý mép mưa dừng đúng ở bậc cổng nhà mình. Ngoài cổng, nắng vẫn khô.")
             .Choice("Mép mưa ở bậc cổng", 1,
                 SceneDefinition.Opt("Hỏi", "hoi_mua"),
                 SceneDefinition.Opt("Bỏ qua", "qua_mua"))
             .Label("hoi_mua")
             .Say(K, N, "Cô ơi, sao mưa chỉ mưa trong cổng nhà cháu?")
             .Narrate("Cô hàng xóm nhìn trời: “Tại mái hiên nhà bên đó con.”")
             .Goto("sau_mua")
             .Label("qua_mua")
             .Narrate("Tôi định ra đứng thử, một chân trong mưa, một chân ngoài nắng.")
             .Label("sau_mua")
             .Narrate("Rồi ông gọi vào rửa nồi lẩu. Lời giải thích nào đi kèm một nồi lẩu cần rửa thì bao giờ cũng hợp lý.")
             .Pause(500)

             .Music("").Backdrop("bg_cau_thang_chieu_nghi_toi")
             .Sfx("sfx_cau_thang_go_cot_ket")
             .Narrate("Tối đó, trên bậu cửa sổ chiếu nghỉ có một cái chai nước mắm nhỏ cắm cành hoa sứ trắng.")
             .Narrate("Nhãn bóc dở, cổ chai mẻ, đúng cái tôi dùng hồi bé. Bảy bông: sáu bông nở, một bông còn là nụ.")
             .Choice("Cành hoa trên bậu cửa sổ", 1,
                 SceneDefinition.Opt("Hỏi", "hoi_hoa1"),
                 SceneDefinition.Opt("Bỏ qua", "sau_hoa1"))
             .Label("hoi_hoa1")
             .Say(K, N, "Ai cắm hoa vậy mẹ?")
             .Narrate("Mẹ đang rửa chén dưới bếp: “Chắc bà đi chùa về.”")
             .Label("sau_hoa1")
             .Narrate("Nhà có người đi chùa, có hoa là chuyện thường.")
             .Pause(1000);

            DeathLine(s, "Sáng 17/07/2018, ông ngã thang ở nhà văn hóa phường khi đang treo băng rôn. Năm đó ông bảy mươi bảy tuổi.");

            s.Backdrop("bg_cau_thang_chieu_nghi_ngay")
             .Narrate("Chiều đưa tang, bậu cửa sổ trống trơn. Chắc ai đó đã dọn trong lúc lo tang. Chuyện thường.")
             .Pause(1200)

             .FadeOut(700).Music("bgm_nha_cu_lang").Backdrop("bg_nha_cu_hu_gao", 0).FadeIn(700)
             .Banner("Cuối tháng Tám · 2018", 1600)
             .Narrate("Bà nội tìm lại chiếc nhẫn cưới đánh rơi từ năm 1977, dưới đáy hũ gạo nhà đổ đầy suốt bốn mươi năm.")
             .Narrate("Bà khóc, rồi cười. Từ hôm ông mất, đó là lần đầu bà cười.")
             .Backdrop("bg_hem_hoa_phuong_rung")
             .Sfx("sfx_gio_lon_dem")
             .Narrate("Đêm đó gió lớn. Sáng ra, hoa phượng rụng đỏ con hẻm. Ai cũng bảo tại gió.")
             .Narrate("Nhưng cánh phượng dồn thành một vệt dài chạy thẳng tới cổng nhà mình, như có ai quét về một hướng.")
             .Choice("Vệt hoa phượng", 1,
                 SceneDefinition.Opt("Hỏi", "hoi_phuong"),
                 SceneDefinition.Opt("Bỏ qua", "qua_phuong"))
             .Label("hoi_phuong")
             .Say(K, N, "Bác ơi, sao hoa nằm thành một đường vậy?")
             .Narrate("Ông hàng xóm chống chổi: “Gió thổi một chiều thì hoa nằm một chiều.”")
             .Goto("sau_phuong")
             .Label("qua_phuong")
             .Narrate("Chắc thằng nhóc nào trong xóm rảnh quá đi gom hoa chơi.")
             .Label("sau_phuong")
             .Sfx("sfx_trong_truong")
             .Narrate("Rồi trống trường vọng tới. Tôi chạy. Ý nghĩ ấy rơi lại giữa hẻm, cùng mấy cánh phượng dưới đế dép.")
             .Pause(500)

             .Music("").Backdrop("bg_cau_thang_chieu_nghi_toi")
             .Narrate("Tối hôm ấy, cái chai và cành hoa lại nằm trên bậu cửa sổ. Bảy bông, sáu nở, một nụ.")
             .Choice("Cành hoa trên bậu cửa sổ", 1,
                 SceneDefinition.Opt("Hỏi", "hoi_hoa2"),
                 SceneDefinition.Opt("Bỏ qua", "qua_hoa2"))
             .Label("hoi_hoa2")
             .Say(K, N, "Mẹ ơi, cái chai trên bậu cửa sổ…")
             .Goto("sau_hoa2")
             .Label("qua_hoa2")
             .Narrate("Tôi định đi tiếp, rồi đứng lại, đưa tay định nhấc cái chai lên xem.")
             .Label("sau_hoa2")
             .Narrate("Dưới nhà, mẹ gọi: “Khoa ơi, xuống bưng nồi cơm giùm mẹ, nóng tay quá!” Tôi chạy xuống.")
             .Music("bgm_nha_cu_lang").Backdrop("bg_nha_cu_bua_com_ghe_trong")
             .Narrate("Bữa tối, ghế của ông để trống. Đến lúc tắt đèn, tôi không còn nhớ mình từng định xem cái gì.")
             .Narrate("Bà dặn: sáng mai, đúng bảy giờ, ra chân bậc đá chùa đỡ bà xuống. Bậc đá vừa lát lại, trơn lắm.");
            Custom(s, Notebook("p02_nhung_thu_tren_bau_cua_so"));
            s.Pause(900)
             .FadeOut(900)
             .Then("P03_khoa")
             .End();
            return s;
        }

        // ---------------------------------------------------------------- P03 — Hai phút

        static SceneDefinition P03()
        {
            var s = Scene("P03_khoa", "Hai phút",
                "Sáng 04/09/2018 Khoa va phải một người mặc áo mưa xám đen, mùi cám heo, nắm cổ tay anh nói “Còn sớm”; anh tới chân bậc đá lúc 07:02 và thấy bà ngã chúi về trước, phía sau không có ai. " +
                "Công an, nhà thuốc (camera hỏng), sạp xôi (nghỉ bán) không xác nhận gì; cổ tay bầm một tuần, các bản ngã bắt đầu, và Khoa nhận chẩn đoán trước sinh nhật mười sáu.");

            s.Music("").Backdrop("bg_goc_pho_sang_som", 0).FadeIn(700)
             .Banner("04/09/2018", 1600)
             .Sfx("sfx_buoc_chan_voi")
             .Narrate("Sáng 04/09/2018, tôi đi sớm. Ở góc phố cuối, tôi va mạnh vào vai một người đàn ông đi ngược chiều.")
             .Sfx("sfx_va_cham_tui_roi")
             .Narrate("Áo mưa xám đen, mũ trùm sụp xuống dù trời không mưa. Nắng rọi sau lưng, dưới mép mũ chỉ là khoảng tối.")
             .Narrate("Người ông có mùi cám heo. Ông nắm chặt cổ tay tôi. Ngón tay khô, lạnh, dưới móng đóng đất đỏ.")
             .Pause(600)
             .Narrate("Ông nhìn đồng hồ, nói đúng hai chữ:")
             .Narrate("“Còn sớm.”")
             .Pause(900)
             .Narrate("Rồi buông ra, đi mất vào con hẻm bên cạnh.")
             .Narrate("Ông này khùng à? Trời nắng mặc áo mưa, còn nắm tay người ta để xem gi—")
             .Sfx("sfx_dong_ho_tich")
             .Banner("06:59", 1200)
             .Narrate("Bảy giờ kém một phút. Bà đang đợi. Tôi vơ túi đồ, chạy.")
             .Narrate("Đó là lần đầu tôi nghĩ người khác bị khùng. Cũng là lần cuối tôi còn đủ tư cách để nghĩ vậy.")

             .Backdrop("bg_bac_da_chua", 300)
             .Narrate("Tôi tới chân bậc đá lúc 07:02, vừa kịp thấy bà ở bậc trên cùng. Bà bước xuống, hụt chân, và ngã.")
             .Pause(900)
             .Narrate("Không ngửa ra sau như người trượt chân. Bà đổ chúi về phía trước, vai lệch như bị ai đẩy vào lưng.")
             .Pause(1300)
             .Narrate("Phía sau bà, trên bậc đá, không có ai cả.")
             .Pause(1400);

            DeathLine(s, "Sáng 04/09/2018, 07:02, bà nội ngã ở bậc đá chùa.");

            s.Backdrop("bg_bien_ban_giay")
             .Narrate("Biên bản ghi: “Trượt ngã trên bậc đá ẩm. Không có dấu hiệu bất thường.”")
             .Label("hoi")
             .Choice("Kể chuyện người mặc áo mưa với ai?", 0,
                 OptIf("Chú công an phường", "cong_an", "!STORY_FLAG p03_cong_an"),
                 OptIf("Nhà thuốc đầu hẻm", "nha_thuoc", "!STORY_FLAG p03_nha_thuoc"),
                 OptIf("Sạp xôi ở góc phố", "xoi", "!STORY_FLAG p03_xoi"))
             .Goto("het_hoi")
             .Label("cong_an")
             .Backdrop("bg_tru_so_cong_an_phuong")
             .Narrate("Chú công an ghi vào sổ đúng một dòng, rồi vỗ vai tôi: “Cháu sốc quá thôi.”")
             .Flag("p03_cong_an")
             .Goto("hoi")
             .Label("nha_thuoc")
             .Backdrop("bg_hem_nha_thuoc_sap_xoi")
             .Narrate("Camera trước nhà thuốc đầu hẻm hỏng từ tuần trước. Chủ tiệm chưa kịp gọi thợ.")
             .Flag("p03_nha_thuoc")
             .Goto("hoi")
             .Label("xoi")
             .Backdrop("bg_hem_nha_thuoc_sap_xoi")
             .Narrate("Sạp xôi nghỉ bán, bà chủ về quê đám giỗ. Người tập thể dục đã về từ trước sáu giờ rưỡi.")
             .Flag("p03_xoi")
             .Goto("hoi")
             .Label("het_hoi")
             .Backdrop("bg_bien_ban_giay")
             .Narrate("Không ai bảo tôi nói dối. Không ai xác nhận được gì. Cả thế giới như đã thống nhất là không nhìn thấy.")
             .Backdrop("bg_co_tay_vet_bam")
             .Narrate("Chỉ còn vết bầm ở cổ tay, gần một tuần mới tan. Mẹ hỏi, tôi bảo va vào cửa.")
             .Narrate("Cái cửa nhà tôi chưa bao giờ có ngón tay.")
             .Pause(800)
             .Narrate("Nếu không va vào người đó, tôi đã tới đúng bảy giờ. Hai phút. Chỉ hai phút.")
             .Pause(1400)

             .Backdrop("bg_hanh_lang_ban_nga")
             .Narrate("Từ đó tôi nghe giọng nói, có khi là giọng ai đó nói “còn sớm” từ phòng bên.")
             .Narrate("Tôi thấy những tôi khác: một tôi ở góc phố, một tôi nhìn đồng hồ, một tôi luôn tới muộn hai phút.")
             .Narrate("Bác sĩ kết luận: tâm thần phân liệt khởi phát sớm. Còn ba ngày nữa tôi tròn mười sáu.")
             .Narrate("Quà sinh nhật mười sáu tuổi của tôi là một tờ giấy chứng nhận.")
             .Narrate("Tôi học cách không nói ra. Người điên giỏi nhất là người điên không ai biết.")
             .Pause(900)
             .Backdrop("bg_cau_thang_chieu_nghi_ngay")
             .Narrate("Hôm đưa tang bà, bậu cửa sổ lại trống.")
             .Pause(1800)
             .FadeOut(900)
             .Then("P04_khoa")
             .End();
            return s;
        }

        // ---------------------------------------------------------------- P04 — Bậu cửa sổ (montage)

        static SceneDefinition P04()
        {
            var s = Scene("P04_khoa", "Bậu cửa sổ",
                "Năm 2019: anh Minh mất 19/02, bé An mất 13/05, cha mất 24/08 (biên bản ghi tên trại “B.R.”); đêm 04/10 Khoa canh bậu cửa, ngủ li bì tới tối 05/10, sổ có dòng lạ “02:00. Đã giữ chỗ.” và không ai mất; mẹ mất 08/11, chiếc vòng cổ không có trong túi đồ trả về. " +
                "Sau đám tang, Khoa nhấc cái chai lên: vòng tròn sạch dưới đáy, sáu cuống hoa khô; anh kẻ sổ hai cột rồi dọn ra ở trọ.");

            s.Music("bgm_nha_cu_lang").Backdrop("bg_nha_cu_bua_com_ghe_trong", 0).FadeIn(700)

             // Minh
             .Sfx("sfx_xe_to_lich").Backdrop("bg_giay_bao_hoc_bong")
             .Banner("Mùa xuân · 2019", 1400)
             .Narrate("Minh nhận học bổng toàn phần. Mẹ bắc nồi lẩu, lần đầu từ đám tang. Tôi mừng. Tôi cũng sợ.")
             .Backdrop("bg_guong_nha_tam_mo")
             .Narrate("Trời nồm. Lau gương nhà tắm, chỗ khô đầu tiên bao giờ cũng hiện hình một khuôn mặt. Chắc là mặt mình.")
             .Backdrop("bg_nha_cu_phong_minh_cai_quat")
             .Narrate("Tối thứ bảy, cành hoa nằm trên bậu cửa. Minh gọi tôi lên giữ cái quạt cho nó vặn ốc tới khuya.")
             .Narrate("Lần đầu tiên, Minh để tôi cầm cái quạt mà không đòi lại. Tôi tưởng nó đang lớn.");
            DeathLine(s, "Sáng 19/02/2019, xe của Minh trượt vệt dầu trên cầu.");
            s.Backdrop("bg_guong_nha_tam_mo")
             .Narrate("Về sau tôi mới nhớ: chỗ khô trên gương nằm thấp hơn mặt tôi một chút. Đúng bằng chiều cao của Minh.")
             .Narrate("Cái quạt từ đó là của tôi. Tôi chưa bật nó lên lần nào nữa.")

             // bé An
             .Music("bgm_nha_cu_lang").Sfx("sfx_xe_to_lich").Backdrop("bg_tranh_gia_dinh_em_dong_khung")
             .Banner("Tháng Năm · 2019", 1400)
             .Narrate("An nói đủ phần cho cả nhà. Tháng Năm, tranh “Gia đình em” của nó đạt giải nhất thành phố.")
             .Narrate("Nó vẽ từ Tết 2018, bắt cả nhà ký tên phía sau. Tôi ký vào cái góc nó chừa sẵn, nhỏ hơn chỗ mọi người.")
             .Backdrop("bg_tui_ao_hat_dau_den")
             .Narrate("An kể mơ thấy có người đứng sau lưng dặn đừng quay lại. “Giọng giống anh Khoa,” nó nói, rồi cười.")
             .Narrate("Sáng ấy, trong túi áo khoác tôi có một hạt đậu đen. Chè căng-tin, tôi đoán, rồi vứt đi.")
             .Narrate("Tối đó cành hoa lại nằm trên bậu cửa. An kéo tôi xuống xem tranh. Tôi không lên lầu nữa.");
            DeathLine(s, "Chiều 13/05/2019, sau một cơn giông, một mảng tường rào trường học đổ xuống.");

            // cha
            s.Music("bgm_nha_cu_lang").Sfx("sfx_xe_to_lich").Backdrop("bg_gieng_sau_nha_kien")
             .Banner("Tháng Tám · 2019", 1400)
             .Narrate("Sau An, nhà tôi thôi nói chuyện trong bữa ăn. Cha nhận sửa trạm bơm cho một trại chăn nuôi xa, công gấp ba.")
             .Narrate("Nước giếng sau nhà hơi ấm; cha bảo mô-tơ chạy nóng. Kiến bò thành vòng tròn quanh miệng giếng.")
             .Backdrop("bg_hien_nha_nen_cup_dien")
             .Sfx("sfx_cup_dien")
             .Narrate("Đêm cành hoa xuất hiện, cả xóm cúp điện. Cha sai tôi đi mua nến, rồi ngồi quạt tay ngoài hiên tới sáng.")
             .Narrate("Cha kể về một con heo trên trại xa, cứ đến giờ cân là nằm ngửa giả chết.")
             .Narrate("Tôi cười. Đó là lần cuối tôi cười trước mặt cha.");
            DeathLine(s, "Ngày 24/08/2019, cha bị điện giật khi đang sửa trạm.");
            s.Backdrop("bg_bien_ban_giay")
             .Narrate("Biên bản ghi tên trại bằng hai chữ viết tắt: B.R.")
             .Pause(900)

             // sổ, 05/10
             .Backdrop("bg_so_trang_dau_nam_dong")
             .Narrate("Tôi mua một cuốn sổ, chép năm ngày mất vào trang đầu. Lần sau, tôi sẽ không để ai gọi đi.")
             .Sfx("sfx_xe_to_lich").Music("").Backdrop("bg_cau_thang_chieu_nghi_toi")
             .Banner("Tháng Mười · 2019", 1400)
             .Narrate("Đầu tháng Mười, tôi nhặt được một tờ tiền, và nhìn nó như nhìn một tờ giấy báo tử.")
             .Narrate("Tối 04/10, cành hoa nằm trên bậu cửa. Không ai gọi. Tôi ngồi xuống bậc thang, ghi: “21:14.”")
             .Pause(1000)
             .Narrate("Dòng cuối tôi nhớ mình viết: “23:58. Vẫn còn.”")
             .FadeOut(1500).Pause(1200)
             .Backdrop("bg_phong_ngu_khoa_toi", 0).FadeIn(1200)
             .Narrate("Tôi tỉnh dậy trên giường, tối 05/10/2019. Mẹ bảo tôi sốt li bì gần trọn một ngày.")
             .Narrate("Dưới móng tay có đất màu nâu đỏ. Không ai gặp chuyện gì. Bậu cửa sổ trống.")
             .Narrate("Trong sổ có thêm một dòng, chữ của tôi, mà tôi không nhớ đã viết:")
             .Narrate("“02:00. Đã giữ chỗ.”")
             .Pause(1800)

             // mẹ
             .Music("bgm_nha_cu_lang").Sfx("sfx_xe_to_lich").Backdrop("bg_bep_me_gia_bat")
             .Banner("Tháng Mười Một · 2019", 1400)
             .Narrate("Mẹ được nghỉ phép về quê, lần đầu sau mười năm. Mẹ hát trong bếp, sai lời, và tôi không sửa.")
             .Narrate("Đêm trước chuyến đi, mẹ úp cả chồng bát lên giá. Chỉ cái bát của tôi nằm ngửa. Tôi ghi: “Bát ngửa.”")
             .Narrate("Cành hoa nằm trên bậu cửa. Tôi ghi: “Hoa. Bảy bông.” Rồi mẹ gọi xuống xếp vali tới khuya.")
             .Narrate("Cuốn sổ để quên trên tủ giày. Mẹ đeo chiếc vòng cổ đi ngủ, bảo mai đi sớm khỏi phải tìm.");
            DeathLine(s, "Ngày 08/11/2019, chiếc xe khách mẹ đi bị lật.");
            s.Backdrop("bg_so_trang_dau_nam_dong")
             .Narrate("Túi đồ người ta trả về không có chiếc vòng.")
             .Narrate("Hai dòng trong sổ vẫn nằm đó, đúng chính tả. Tôi đã ghi đủ. Tôi chỉ không đọc lại.")
             .Pause(2000)

             // 1.5 — cái chai
             .FadeOut(900).Backdrop("bg_cau_thang_chieu_nghi_ngay", 0).FadeIn(900)
             .Narrate("Sau đám tang mẹ, nhà chỉ còn mình tôi, mười bảy tuổi. Tôi lau dọn, lên tới bậu cửa sổ chiếu nghỉ.")
             .Narrate("Cái chai nằm ở góc bậu, bị tấm rèm che mất một nửa.")
             .Choice("Cái chai", 0, SceneDefinition.Opt("Nhấc cái chai lên"))
             .Backdrop("bg_bau_cua_vong_tron_sach")
             .Narrate("Dưới đáy chai, mặt bậu sạch một vòng tròn giữa lớp bụi dày nhiều tháng.")
             .Narrate("Trong chai, sáu cuống hoa sứ khô đen nằm chồng lên nhau thành từng lớp.")
             .Pause(1600)
             .Narrate("Những hôm đưa tang, tôi chưa bao giờ bước lại gần bậu cửa. Tôi chỉ liếc qua, thấy trống, rồi đi.")
             .Pause(900)
             .Backdrop("bg_so_hai_cot")
             .Narrate("Tôi kẻ đôi trang sổ. Bên phải: “Chai. Vòng sạch dưới đáy. Sáu cuống khô.”")
             .Narrate("Bên trái: “Hoa: lúc có, lúc không.” Ngoài lề, bút chì: “Hay là nó chưa bao giờ đi đâu cả?”");
            Custom(s, Notebook("p04_vong_tron_sach"));
            s.Narrate("Tôi đặt cái chai về đúng vòng tròn sạch, rồi đi xuống, bật hết đèn trong nhà.")
             .Pause(900)
             .Music("bgm_phong_tro").Backdrop("bg_hem_nha_cu")
             .Narrate("Năm sau, đủ mười tám, tôi dọn ra ở trọ. Ai khuyên bán nhà, tôi bảo “để rồi tính”.")
             .Pause(800)
             .FadeOut(900)
             .Then("P05_khoa_a")
             .End();
            return s;
        }

        // ---------------------------------------------------------------- P05 — Ngày hai mươi bốn

        static SceneDefinition P05a()
        {
            var s = Scene("P05_khoa_a", "Ngày hai mươi bốn",
                "Sáu năm ở trọ, Khoa sống cùng các bản ngã; tháng 9/2026 anh được thưởng nóng, điều may đầu tiên sau gần bảy năm. " +
                "Sáng 24/09/2026 cái bóng trong gương chớp chậm nửa nhịp; lúc 07:02 anh bị ô tô vượt đèn tông, bên kia đường có người mặc áo mưa xám đen đứng nhìn đồng hồ.");

            s.Music("bgm_phong_tro").Backdrop("bg_phong_tro", 0).FadeIn(700)
             .Narrate("Sáu năm ở trọ, tôi uống thuốc đều, đi làm đều, và học cách không trả lời ngay mọi giọng nói.")
             .Narrate("Một tôi mười lăm tuổi ở góc phố, một tôi mười bảy ngồi cuối giường, một tôi luôn đến muộn hai phút.")
             .Narrate("Không ai trong chúng tôi trả tiền nhà, trừ tôi. Phòng trọ không có bình hoa nào. Tôi cố ý như vậy.")
             .Backdrop("bg_van_phong_thuong_nong")
             .Banner("Tháng Chín · 2026", 1400)
             .Sfx("sfx_vo_tay")
             .Narrate("Công ty thưởng nóng cho tôi. Lần đầu sau gần bảy năm, tôi nhận được một điều may.")
             .Narrate("Đồng nghiệp bảo trông tôi như vừa nhận giấy triệu tập.")
             .Pause(700)

             .FadeOut(700).Music("").Backdrop("bg_phong_tro_guong_nha_tam", 0).FadeIn(700)
             .Banner("24/09/2026", 1600)
             .Sfx("sfx_danh_rang")
             .Narrate("Sáng 24/09/2026, tôi đánh răng.")
             .Choice("Trước gương", 0, SceneDefinition.Opt("Chớp mắt"));
            Custom(s, Reflection(N));
            s.Pause(700)
             .Narrate("Cái bóng trong gương chớp chậm hơn nửa nhịp.")
             .Pause(900)
             .Narrate("Tôi ghi: “Gương chậm. Không đau đầu. Đã uống thuốc.” Đã bảy giờ kém năm. Tôi đi làm.")
             .Backdrop("bg_nga_tu_sang")
             .Sfx("sfx_den_qua_duong")
             .Narrate("Đèn qua đường chuyển xanh. Tôi bước xuống. Một chiếc ô tô vượt đèn lao tới.")
             .Sfx("sfx_phanh_xa")
             .FadeOut(400).Backdrop("bg_nen_den", 0)
             .Narrate("Tiếng phanh. Mặt đường áp vào má. Mọi thứ thật đến mức đáng sợ, chính vì nó quá bình thường.")
             .Pause(800)
             .Backdrop("bg_nga_tu_nguoi_ao_mua", 0).FadeIn(1400)
             .Narrate("Bên kia đường, một người mặc áo mưa xám đen đứng yên, mũ trùm che mặt, nhìn đồng hồ đeo tay.")
             .FadeOut(1500).Pause(1400)
             .Backdrop("bg_bien_ban_giay", 0).FadeIn(900)
             .Narrate("Biên bản ghi thời điểm va chạm: 07:02. Không nhân chứng nào nhắc tới người mặc áo mưa.")
             .Narrate("Có những thứ nên để yên ở cột bên trái.")
             .Pause(1200)
             .FadeOut(900)
             .Then("P05_khoa_b")
             .End();
            return s;
        }

        static SceneDefinition P05b()
        {
            var s = Scene("P05_khoa_b", "Giấc mơ trong phòng bệnh",
                "Sau ca mổ Khoa ngủ hai ngày và mơ thấy cả nhà ngồi quanh giường bệnh rồi lần lượt rời đi, bé An đếm người mà không nói ra, mẹ không quay lại; anh không nói, không cử động được. " +
                "Tỉnh dậy, phòng thoang thoảng mùi táo; hồ sơ ghi 24 tuổi, chấn thương đầu nhẹ, suy giảm trí nhớ ngắn hạn.");

            s.Music("").Backdrop("bg_phong_benh_giac_mo", 0).FadeIn(1500)
             .Sfx("sfx_may_do_nhip_tim")
             .Narrate("Sau ca mổ, tôi ngủ hai ngày, và mơ thấy đúng căn phòng này: rèm xanh, tiếng máy đo nhịp tim.")
             .Narrate("Mẹ ngồi gọt táo, vỏ nối không đứt. Cha đứng cạnh cửa sổ. Ông bà ở chân giường. Minh cầm túi nilon.")
             .Narrate("An ngồi mép ghế, hai chân đung đưa chưa chạm đất.")
             .Pause(1200)
             .Narrate("Tôi không mở được miệng, không nhấc được tay, không chớp được mắt. Tôi chỉ nhìn được.")
             .Pause(900)
             .Narrate("An giơ ngón tay đếm người trong phòng, môi mấp máy. Đếm xong, em không nói ra con số.")
             .Pause(1500)
             .Narrate("Rồi ông bà đứng dậy, đi ra cửa. Tôi gào trong đầu. Căn phòng chỉ có tiếng máy.")
             .Pause(900)
             .Narrate("Minh đặt cái túi nilon xuống chân giường, không nhìn tôi. An chạy theo, ngoái lại một lần, rồi vội quay đi.")
             .Pause(1300)
             .Narrate("Mẹ đi cuối. Mẹ đặt quả táo lên bàn, đứng ở ngưỡng cửa, quay lưng về phía tôi.")
             .Pause(1000)
             .Narrate("Tôi dồn hết vào ngón tay trỏ. Nó nhích lên một li.")
             .Sfx("sfx_may_do_nhip_tim_gap")
             .Pause(1300)
             .Narrate("Mẹ không quay lại.")
             .Pause(1500)
             .Sfx("sfx_cua_khep")
             .Narrate("Cánh cửa khép lại.")
             .Pause(2200)

             .Backdrop("bg_phong_benh_tinh_day", 200)
             .Sfx("sfx_may_do_nhip_tim_bao")
             .Narrate("Tôi bật dậy, hớp không khí như người vừa bị dìm nước. Phòng không có ai. Trên bàn không có quả táo.")
             .Pause(1000)
             .Narrate("Nhưng cả căn phòng thoang thoảng mùi táo.")
             .Pause(1800)
             .Backdrop("bg_ho_so_benh_an")
             .Narrate("Hồ sơ: nam, 24 tuổi, chấn thương đầu nhẹ, suy giảm trí nhớ ngắn hạn. Thêm một tờ giấy cho bộ sưu tập.")
             .Narrate("Tôi không còn nhớ thứ tự người nhà đã mất. Nhưng khuôn mặt từng người trong mơ thì tôi nhớ rõ.")
             .Pause(900)
             .FadeOut(900)
             .Then("P05_khoa_c")
             .End();
            return s;
        }

        static SceneDefinition P05c()
        {
            var s = Scene("P05_khoa_c", "Bảy buổi sáng trước gương",
                "Bảy buổi sáng liền Khoa tự đứng trước gương từ 06:58 đến 07:02 và ghi chép: ngày 1 cái bóng chớp trễ, ngày 3 mắt trong gương không cười theo miệng, ngày 7 chỉ còn một khuôn mặt không buồn không vui đang chờ. " +
                "Anh quen nhìn ánh sáng, bóng và độ trễ, rồi đi thăm mộ để đọc lại tên người nhà.");

            s.Music("").Backdrop("bg_phong_tro_guong_nha_tam", 0).FadeIn(900)
             .Banner("Ngày thứ nhất · 06:58", 1500)
             .Narrate("Về phòng trọ, sáng nào tôi cũng thức dậy lúc 06:58 và đứng trước gương. Không ai bảo tôi làm vậy.");
            Custom(s, Reflection(N));
            s.Label("n1")
             .Choice("Ngày thứ nhất", 2,
                 OptIf("Chớp mắt", "n1_chop", "!STORY_FLAG p05_n1_chop"),
                 OptIf("Mỉm cười", "n1_cuoi", "!STORY_FLAG p05_n1_cuoi"),
                 OptIf("Ghi sổ", "n1_xong", "STORY_FLAG p05_n1_chop"))
             .Goto("n1_xong")
             .Label("n1_chop")
             .Pause(500)
             .Narrate("Cái bóng trễ nửa nhịp, như người đến muộn vẫn cố làm cho kịp.")
             .Flag("p05_n1_chop")
             .Goto("n1")
             .Label("n1_cuoi")
             .Narrate("Tôi kéo hai khóe miệng lên. Cái bóng kéo theo, cùng lúc. Tôi ghi: “Cười: không trễ.”")
             .Flag("p05_n1_cuoi")
             .Goto("n1")
             .Label("n1_xong")
             .Banner("07:02", 900)
             .Sfx("sfx_lat_so").Banner("Ngày thứ hai", 700)

             .Banner("Ngày thứ ba · 06:58", 1400)
             .Label("n3")
             .Choice("Ngày thứ ba", 2,
                 OptIf("Chớp mắt", "n3_chop", "!STORY_FLAG p05_n3_chop"),
                 OptIf("Mỉm cười", "n3_cuoi", "!STORY_FLAG p05_n3_cuoi"),
                 OptIf("Ghi sổ", "n3_xong", "STORY_FLAG p05_n3_cuoi"))
             .Goto("n3_xong")
             .Label("n3_chop")
             .Narrate("Tôi chớp. Cái bóng chớp cùng lúc. Nó không trễ nữa.")
             .Flag("p05_n3_chop")
             .Goto("n3")
             .Label("n3_cuoi");
            Custom(s, Reflection(N));
            s.Pause(800)
             .Narrate("Miệng trong gương cười theo. Mắt trong gương thì không.")
             .Pause(900)
             .Flag("p05_n3_cuoi")
             .Goto("n3")
             .Label("n3_xong")
             .Banner("07:02", 900)
             .Sfx("sfx_lat_so").Banner("Ngày thứ tư", 700)
             .Banner("Ngày thứ năm · 06:58", 1300)
             .Narrate("Ngày thứ năm, tôi không còn nhớ người ta buồn thì trông thế nào.")
             .Sfx("sfx_lat_so").Banner("Ngày thứ sáu", 700)
             .Banner("Ngày thứ bảy · 06:58", 1600);
            Custom(s, Reflection(N));
            s.Choice("Ngày thứ bảy", 2,
                 SceneDefinition.Opt("Chớp mắt", "n7"),
                 SceneDefinition.Opt("Mỉm cười", "n7"),
                 SceneDefinition.Opt("Đứng yên", "n7"))
             .Label("n7")
             .Pause(1300)
             .Narrate("Trong gương chỉ còn một khuôn mặt không buồn, không vui, đang chờ một điều gì đó.")
             .Pause(2000)
             .Narrate("Tôi ghi: “Sợ: không thấy. Ghi chú: lẽ ra phải thấy.”");
            Custom(s, Notebook("p05_bay_buoi_sang_truoc_guong"));
            s.Pause(1300)
             .Music("bgm_phong_tro").Backdrop("bg_phong_tro_cua_so_sang")
             .Narrate("Từ đó tôi nhìn mọi thứ như nhìn vào gương: ánh sáng, cái bóng, thứ gì đang lệch đi một nhịp.")
             .Narrate("Tôi đi thăm mộ để đọc lại tên người nhà. Người ta thăm mộ để nhớ. Tôi đi để tra cứu.")
             .Pause(800)
             .FadeOut(900)
             .Then("P06_khoa_a")
             .End();
            return s;
        }

        // ---------------------------------------------------------------- P06 — Ánh kim

        static SceneDefinition P06a()
        {
            var s = Scene("P06_khoa_a", "Sáu cái tên và một suất trống",
                "Ở nghĩa trang Khoa chép sáu ngày mất cạnh nhau; ông trông nghĩa trang bảo nhà anh mua bảy suất, mới dùng sáu. " +
                "Phiếu thu đề ngày 05/10/2019 (hôm Khoa sốt li bì), chữ ký người nộp gồm bảy nét ngắn không giống ai trong nhà; Khoa chụp lại, ghi cột phải.");

            s.Music("").Backdrop("bg_nghia_trang", 0).FadeIn(900)
             .Sfx("sfx_choi_quet_la")
             .Narrate("Ở nghĩa trang, tôi chép sáu ngày mất vào sổ, cẩn thận như chép số điện thoại.")
             .Backdrop("bg_so_sau_ngay_mat")
             .Narrate("17/07/2018. 04/09/2018. 19/02/2019. 13/05/2019. 24/08/2019. 08/11/2019.")
             .Backdrop("bg_nghia_trang")
             .Narrate("Người trông nghĩa trang đang quét lá. Ông hỏi: “Cậu thăm đủ chưa?”")
             .Say(K, N, "Đủ rồi ạ.")
             .Narrate("“Còn một suất nữa mà. Nhà cậu mua bảy suất, mới dùng sáu.”")
             .Choice("Sổ của nghĩa trang", 0, SceneDefinition.Opt("Xem tờ phiếu thu"))
             .Backdrop("bg_phieu_thu_suat_mo")
             .Narrate("Phiếu đứng tên gia đình, đề ngày 05/10/2019. Hôm tôi sốt li bì. Khi đó mẹ còn sống.")
             .Pause(800)
             .Narrate("Chữ ký người nộp gồm bảy nét ngắn, không giống chữ ai trong nhà.")
             .Pause(1300)
             .Narrate("“Mua trước cho cả nhà thì nhiều người làm lắm,” ông nhún vai. Tôi chụp lại tờ phiếu, ghi cột phải.");
            Custom(s, Notebook("p06_phieu_thu_suat_mo"));
            s.Backdrop("bg_nghia_trang_cong_cay_su")
             .Narrate("Ra cổng, mùi hoa sứ thoảng qua. Có gì đó rất quen. Xe ôm bấm còi giục. Tôi đi tiếp.")
             .Pause(700)
             .FadeOut(900)
             .Then("P06_khoa_b")
             .End();
            return s;
        }

        static SceneDefinition P06b()
        {
            var s = Scene("P06_khoa_b", "Căn nhà cũ và ánh kim",
                "Khoa về nhà cũ sau sáu năm: ổ khóa trơn như vừa tra dầu, đồng hồ đứng ở 7 giờ 2 phút, cái chai rỗng trong vòng tròn sạch. " +
                "Chiếc vòng cổ của mẹ rơi ra từ khe bàn cha; chấm sáng từ mặt vòng trễ một nhịp và dừng trên ảnh Tết 2018, nơi mặt sau vòng lộ bảy khía; cây đinh treo ảnh còn ấm.");

            s.Music("").Backdrop("bg_nha_cu_cua_chinh", 0).FadeIn(900)
             .Narrate("Lần đầu tiên sau sáu năm, tôi về nhà cũ.")
             .Choice("Cửa nhà cũ", 0, SceneDefinition.Opt("Mở khóa"))
             .Sfx("sfx_o_khoa_tron")
             .Narrate("Ổ khóa không gỉ chút nào. Chìa xoay trơn như vừa có người tra dầu hôm qua. Chắc hàng xóm tốt bụng.")
             .Music("bgm_nha_cu_bui").Backdrop("bg_nha_cu_phong_khach_bui")
             .Narrate("Áo cha vẫn treo sau cửa. Vạch chiều cao cuối của An đủ sáu chữ ký làm chứng, thiếu đúng chữ của tôi.")
             .Narrate("Đồng hồ phòng khách đứng ở 7 giờ 2 phút. Hết pin thì phải dừng ở giờ nào đó. Tôi không nhìn lần hai.")
             .Narrate("Trên bậu cửa chiếu nghỉ, cái chai rỗng đứng trong vòng tròn sạch. Thấy quen, nhưng đầu tôi trống rỗng.")
             .Backdrop("bg_nha_cu_ban_lam_viec_cha")
             .Choice("Bàn làm việc của cha", 0, SceneDefinition.Opt("Kéo cái bàn ra khỏi mép thảm"))
             .Sfx("sfx_kim_loai_roi_san")
             .Narrate("Một vật trượt ra từ khe dưới mặt bàn, va vào chân ghế rồi nằm im.")
             .Music("").Backdrop("bg_vong_co_cua_me")
             .Narrate("Chiếc vòng cổ mẹ đeo đêm trước chuyến xe cuối cùng. Chiếc không có trong túi đồ người ta trả về.")
             .Pause(1500)

             .Backdrop("bg_nha_cu_tuong_cham_sang")
             .Narrate("Nắng chiều hắt từ mặt vòng một chấm sáng lên tường. Sau bảy buổi sáng trước gương, mắt tôi tự dõi theo.")
             .Label("cham")
             .Choice("Chấm sáng trên tường", 2,
                 OptIf("Nghiêng mặt vòng sang một bên", "nghieng", "!STORY_FLAG p06_nghieng"),
                 OptIf("Nghiêng ngược lại", "nguoc", "!STORY_FLAG p06_nguoc"),
                 SceneDefinition.Opt("Thả lỏng tay, để mặt vòng tự xoay", "tha"),
                 OptIf("Giữ thật chặt cho chấm sáng đứng yên", "chat", "!STORY_FLAG p06_chat"))
             .Label("nghieng")
             .Narrate("Chấm sáng chạy ngang tường. Tôi dừng tay, nó vẫn trôi thêm một đoạn mới đứng lại.")
             .Flag("p06_nghieng")
             .Goto("cham")
             .Label("nguoc")
             .Narrate("Lần này nó đứng yên một nhịp rồi mới trượt theo. Đúng cái độ trễ tôi đã ghi kín bảy trang sổ.")
             .Flag("p06_nguoc")
             .Goto("cham")
             .Label("chat")
             .Narrate("Tay tôi run, chấm sáng nhảy lung tung. Trước gương, tôi chưa bao giờ giữ chặt cái gì. Tôi chỉ đứng yên.")
             .Flag("p06_chat")
             .Goto("cham")
             .Label("tha")
             .Pause(800)
             .Narrate("Chấm sáng trôi chậm qua khung cửa có vạch đo của An, qua mặt cái đồng hồ đứng,")
             .Backdrop("bg_anh_tet_2018_cham_sang", 1200)
             .Narrate("rồi dừng trên tấm ảnh Tết 2018, đúng chỗ cổ mẹ. Trong ảnh, mặt vòng lật ngược, lộ bảy khía nhỏ.")
             .Pause(1300)
             .Narrate("Mặt trời khuất sau mái nhà bên. Chấm sáng vẫn nằm trên cổ mẹ thêm một nhịp nữa rồi mới tắt.")
             .Pause(2200)
             .Narrate("Lần này không ai gọi tôi đi. Tôi ghi cột trái: “Chấm sáng ở lại sau khi nguồn sáng đi.”")
             .Choice("Tấm ảnh Tết", 0, SceneDefinition.Opt("Gỡ tấm ảnh xuống"))
             .Pause(600)
             .Narrate("Cây đinh trên tường còn ấm, như vừa có ai cầm vào.")
             .Pause(1300)
             .Narrate("Cây đinh: cột phải. Hơi ấm: cột trái. Ngoài lề, bút chì: “Có người vừa ở đây?”")
             .Pause(1500)
             .Then("P06_khoa_c")
             .End();
            return s;
        }

        static SceneDefinition P06c()
        {
            var s = Scene("P06_khoa_c", "Bức tranh của An và sáu tiếng động",
                "Sau tấm bìa lót khung ảnh là bức tranh “Gia đình em” bản gốc của bé An, có mũi tên đỏ chỉ “chỗ cất đồ bí mật của anh Khoa”; bậc thứ bảy, ngay dưới chiếu nghỉ, kêu rỗng. " +
                "Bên dưới là hộp bánh quy thiếc khóa sao bảy cánh: bảy khía của chiếc vòng xoay từng cánh, sáu cánh là sáu tiếng động quen trong nhà, cánh thứ bảy là im lặng tuyệt đối, rồi ổ khóa mở.");

            s.Music("").Backdrop("bg_tranh_gia_dinh_em_ban_goc", 0).FadeIn(900)
             .Narrate("Sau tấm bìa lót khung ảnh là bức tranh “Gia đình em” bản gốc của An, mặt sau đủ bảy chữ ký.")
             .Narrate("Ngôi nhà bổ đôi, bảy người que ở chỗ quen: ông cạnh tủ gỗ, bà trước bàn thờ, Minh cạnh cái quạt,")
             .Narrate("An ở khung cửa, cha ở bàn làm việc, mẹ trong bếp. Người que thứ bảy đứng ở chiếu nghỉ, cạnh cái chai.")
             .Narrate("Từ chân người que ấy, một mũi tên chỉ xuống cầu thang, dấu X đỏ: “chỗ cất đồ bí mật của anh Khoa”.")
             .Narrate("Tôi không nhớ mình có chỗ nào như thế. Nhưng tôi không có tư cách cãi với một bức tranh đạt giải.")
             .Backdrop("bg_cau_thang_muoi_bon_bac")
             .Narrate("Cầu thang có mười bốn bậc, chiếu nghỉ ở giữa.")
             .Label("go")
             .IfGoto("STORY_FLAG p06_goi_y", "chon")
             .IfGoto("!STORY_FLAG p06_sai_3", "chon")
             .Sfx("sfx_anh_sang_tranh")
             .Narrate("Nắng rọi lên bức tranh, mũi tên đỏ sáng lên: nó chỉ xuống đúng một bậc dưới chân người que.")
             .Flag("p06_goi_y")
             .Label("chon")
             .Choice("Gõ bậc nào?", 2,
                 OptIf("Bậc dưới cùng", "g1", "!STORY_FLAG p06_go_1"),
                 OptIf("Bậc thứ tư từ dưới lên", "g4", "!STORY_FLAG p06_go_4"),
                 SceneDefinition.Opt("Bậc ngay dưới chiếu nghỉ", "dung"),
                 OptIf("Bậc ngay trên chiếu nghỉ", "g8", "!STORY_FLAG p06_go_8"),
                 OptIf("Bậc trên cùng", "g14", "!STORY_FLAG p06_go_14"))
             .Label("g1").Flag("p06_go_1").Goto("sai")
             .Label("g4").Flag("p06_go_4").Goto("sai")
             .Label("g8").Flag("p06_go_8").Goto("sai")
             .Label("g14").Flag("p06_go_14").Goto("sai")
             .Label("sai")
             .Sfx("sfx_go_bac_dac")
             .IfGoto("STORY_FLAG p06_sai_2", "sai3")
             .IfGoto("STORY_FLAG p06_sai_1", "sai2")
             .Flag("p06_sai_1")
             .Narrate("Cộc. Đặc.")
             .Goto("go")
             .Label("sai2")
             .Flag("p06_sai_2")
             .Narrate("Cộc. Cũng đặc.")
             .Goto("go")
             .Label("sai3")
             .Flag("p06_sai_3")
             .Narrate("Cộc. Gỗ đặc, chắc như cái bàn thờ.")
             .Goto("go")

             .Label("dung")
             .Sfx("sfx_go_bac_rong")
             .Narrate("Cộc.")
             .Pause(800)
             .Narrate("Rỗng.")
             .Pause(1200)
             .Narrate("Bậc thứ bảy, ngay dưới chiếu nghỉ, ngay dưới bậu cửa có cái chai.")
             .Choice("Bậc thứ bảy", 0, SceneDefinition.Opt("Nhấc tấm ván lên"))
             .Sfx("sfx_van_go_nhac")
             .Backdrop("bg_hop_thiec_khoa_sao")
             .Narrate("Một hộp bánh quy thiếc, khóa hình sao bảy cánh. Bảy khía của chiếc vòng khớp vào. Mỗi cánh xoay riêng.")
             .Choice("Ổ khóa sao", 0, SceneDefinition.Opt("Xoay cánh thứ nhất"))
             .Sfx("sfx_tu_go_ket")
             .Narrate("Trong phòng ông, cánh tủ gỗ kẽo kẹt hé ra, như mọi lần ông mở tủ lấy áo.")
             .Choice("Ổ khóa sao", 0, SceneDefinition.Opt("Xoay cánh thứ hai"))
             .Sfx("sfx_chuong_gio_mot_tieng")
             .Narrate("Chuông gió trước bàn thờ khẽ reo một tiếng, dù cửa sổ đóng kín.")
             .Choice("Ổ khóa sao", 0, SceneDefinition.Opt("Xoay cánh thứ ba"))
             .Sfx("sfx_quat_cu_quay_mot_vong")
             .Narrate("Trên lầu, cái quạt của Minh quay một vòng rồi dừng. Phích cắm đã rút từ sáu năm trước.")
             .Choice("Ổ khóa sao", 0, SceneDefinition.Opt("Xoay cánh thứ tư"))
             .Sfx("sfx_khung_cua_cot")
             .Narrate("Khung cửa có vạch đo kêu cót một tiếng, như có ai vừa tựa lưng vào để đo chiều cao.")
             .Choice("Ổ khóa sao", 0, SceneDefinition.Opt("Xoay cánh thứ năm"))
             .Sfx("sfx_co_le_leng_keng")
             .Narrate("Trên bàn của cha, hai cái cờ lê chạm vào nhau leng keng.")
             .Choice("Ổ khóa sao", 0, SceneDefinition.Opt("Xoay cánh thứ sáu"))
             .Sfx("sfx_giot_nuoc_bon_rua")
             .Narrate("Dưới bếp, vòi nước nhỏ một giọt xuống bồn rửa.")
             .Pause(1200)
             .Sfx("sfx_tim_dap")
             .Narrate("Nhà cũ thì cửa kêu, nước rỉ. Tôi vẫn nghe tim mình đập thình thịch.")
             .Label("canh7")
             .Choice("Cánh thứ bảy kẹt cứng, như chưa từng có ai xoay.", 0,
                 SceneDefinition.Opt("Ấn mạnh", "an_manh"),
                 OptIf("Để rồi tính", "de_roi_tinh", "!STORY_FLAG p06_de_roi_tinh"))
             .Label("de_roi_tinh")
             .Narrate("Tôi đi một vòng phòng khách rồi ngồi xuống lại. “Để rồi tính” chưa bao giờ có tác dụng với hộp thiếc.")
             .Flag("p06_de_roi_tinh")
             .Goto("canh7")
             .Label("an_manh")
             .Sfx("sfx_im_lang_tuyet_doi")
             .Pause(1800)
             .Narrate("Mọi âm thanh tắt hẳn. Tiếng xe, tiếng chó, cả tiếng thở của chính tôi.")
             .Pause(2200)
             .Narrate("Căn nhà im như một tấm ảnh.")
             .Pause(2200)
             .Sfx("sfx_khoa_tach")
             .Sfx("sfx_xe_may_hem")
             .Narrate("Tách. Tiếng xe máy ngoài hẻm ùa trở lại, như chưa từng mất đi.")
             .Pause(1300)
             .Narrate("Đó là lần đầu tiên khía thứ bảy được dùng đến.");
            Custom(s, Notebook("p06_hop_thiec_khoa_sao"));
            s.Pause(1500)
             .Then("P07_khoa")
             .End();
            return s;
        }

        // ---------------------------------------------------------------- P07 — Đừng đếm

        static SceneDefinition P07()
        {
            var s = Scene("P07_khoa", "Đừng đếm",
                "Trong hộp thiếc: sổ bìa vải bảy phần (phần cuối “của người về sau”), ảnh cả nhà ở trại hồi Khoa năm sáu tuổi, và mảnh da cổ khắc dấu nêm với lời dặn chữ Khoa ngày 21/09/2026. " +
                "Cành hoa tàn hiện trên bậu cửa, Khoa ngất; anh tỉnh trên xe khách lúc 13:31 ngày 13/10 với đất đỏ dưới móng, vé 07:02, mảnh da thêm dòng “Đừng đếm”, và con đường khớp với sổ của ông.");

            s.Music("").Backdrop("bg_hop_thiec_mo", 0).FadeIn(900)
             .Narrate("Trong hộp có ba thứ.")
             .Label("hop")
             .Choice("Hộp thiếc", 0,
                 OptIf("Quyển sổ bìa vải", "so", "!STORY_FLAG p07_so"),
                 OptIf("Tấm ảnh", "anh", "!STORY_FLAG p07_anh"),
                 OptIf("Mảnh gì đó gấp làm tư", "da", "!STORY_FLAG p07_da"))
             .Goto("du_ba")

             .Label("so")
             .Backdrop("bg_so_bia_vai_bay_phan")
             .Narrate("Sổ bìa vải dính đất nâu đỏ, bảy phần, bảy nét chữ: ông vẽ đường, bà ghi thời tiết, cha vẽ máy,")
             .Narrate("mẹ chép cách chăm heo, Minh kẻ bảng, An vẽ heo. Phần thứ bảy trống: “Phần này của người về sau.”")
             .Flag("p07_so")
             .Goto("hop")

             .Label("anh")
             .Backdrop("bg_anh_ca_nha_o_trai")
             .Narrate("Ảnh cả nhà trước hàng rào gỗ chắp vá. Mẹ bế An đỏ hỏn. Tôi năm, sáu tuổi, cầm cành hoa sứ trắng.")
             .Narrate("Sau rào, một con heo nằm ngửa giả chết. Nó là đứa duy nhất có vẻ biết mình đang làm gì.")
             .Flag("p07_anh")
             .Goto("hop")

             .Label("da")
             .Backdrop("bg_manh_da_dau_nem")
             .Narrate("Một mảnh da thuộc khô cứng, nâu như lá trà ủ lâu năm, cũ hơn cả căn nhà.")
             .Narrate("Mặt trước khắc lõm hai dấu góc nhọn nằm nghiêng và bốn vạch như những cái nêm nhỏ. Rãnh khắc lạnh hơn da.")
             .Choice("Mảnh da", 0, SceneDefinition.Opt("Lật mặt sau"))
             .Backdrop("bg_manh_da_mat_sau")
             .Narrate("Mặt sau, mực bi xanh còn tươi, chữ của tôi:")
             .Pause(600)
             .Narrate("“Hai mươi bốn tuổi thì mang vòng về trại. Đường đi nằm ở phần chưa viết.")
             .Narrate("Ở đó người ta sẽ hỏi nhà mình còn mấy người. Đừng trả lời.”")
             .Pause(1500)
             .Narrate("Ngày 21/09/2026, hôm tôi được thưởng nóng. Tôi không nhớ đã viết. Không nhớ đã về đây.")
             .Narrate("Ngoài lề: “Hộp chưa từng mở. Chữ của tôi ở trong. Bằng đường nào?”")
             .Flag("p07_da")
             .Goto("hop")

             .Label("du_ba")
             .Pause(1200)
             .Backdrop("bg_cau_thang_chieu_nghi_hoa_tan")
             .Narrate("Mùi hoa sứ xộc tới. Tôi quay lại.")
             .Pause(1000)
             .Narrate("Cái chai trên bậu cửa đang cắm một cành hoa sứ. Cả bảy bông đã tàn, kể cả bông nụ.")
             .Narrate("Cánh hoa rụng thành một vòng, nằm gọn trong vòng tròn sạch.")
             .Pause(1500)
             .Narrate("Lần này không có ai gọi tôi đi.")
             .Pause(900)
             // memory flash: images only, no interaction (P07 design note)
             .Backdrop("bg_nha_cu_tu_tho_ve_so", 120).Pause(500)
             .Backdrop("bg_nha_cu_cong_mua_bong_may", 120).Pause(500)
             .Backdrop("bg_nha_cu_hu_gao", 120).Pause(450)
             .Backdrop("bg_hem_hoa_phuong_rung", 120).Pause(450)
             .Backdrop("bg_co_tay_vet_bam", 120).Pause(450)
             .Backdrop("bg_guong_nha_tam_mo", 120).Pause(400)
             .Backdrop("bg_tui_ao_hat_dau_den", 120).Pause(400)
             .Backdrop("bg_nha_cu_phong_minh_cai_quat", 120).Pause(400)
             .Backdrop("bg_gieng_sau_nha_kien", 120).Pause(400)
             .Backdrop("bg_hien_nha_nen_cup_dien", 120).Pause(400)
             .Backdrop("bg_bep_me_gia_bat", 120).Pause(400)
             .Backdrop("bg_nha_cu_khung_cua_vach_do", 120).Pause(700)
             .Backdrop("bg_cau_thang_chieu_nghi_hoa_tan", 300).Pause(800)
             .Say(K, N, "Ông, bà, ba, mẹ, anh Minh, bé An. Và mình.")
             .Pause(1600)
             .Narrate("Bảy.")
             .Pause(2000)
             .Narrate("Tôi chưa bao giờ đếm. Chưa ai trong nhà nói “nhà mình có bảy người”.")
             .Narrate("Bàn ăn có mấy chỗ? Tôi không nhớ. Cành hoa có mấy bông? Bảy. Cái đó thì tôi nhớ.")
             .Pause(1500)
             .Backdrop("bg_nha_cu_bua_com_tu_cua_bep", 900)
             .Narrate("Một ký ức: bữa cơm tối, nhìn từ cửa bếp vào. Ông, bà, ba, mẹ, anh Minh, bé An, và tôi, sát nồi cơm.")
             .Pause(2000)
             .Narrate("Vậy thì ai đang đứng ở cửa bếp?")
             .Pause(2000)
             .Narrate("Mẹ không ngẩng lên: “Đứng đó làm gì, vào ăn đi con.”")
             .Pause(900)
             .Narrate("Thằng bé cạnh nồi cơm đáp: “Dạ.”")
             .Pause(1500)
             .Narrate("Tôi cố nhớ khuôn mặt người ở cửa. Một cơn đau như đinh xuyên qua thái dương.")
             .FadeOut(300).Backdrop("bg_nen_den", 0)
             .Narrate("Mọi thứ tắt ngấm.")
             .Pause(2800)

             .Sfx("sfx_coi_xe_khach")
             .Backdrop("bg_xe_khach_hang_ghe_cuoi", 0).FadeIn(1400)
             .Narrate("Tiếng còi xe đánh thức tôi. Hàng ghế cuối một chiếc xe khách. Ba lô xếp gọn hơn mọi lần tôi tự xếp.")
             .Narrate("Dưới móng tay có đất nâu đỏ. Điện thoại báo 13:31, ngày 13/10. Vé xe trong túi in giờ 07:02.")
             .Say(K, N, "Tôi lên xe ở đâu vậy em?")
             .Narrate("Cậu phụ xe nhìn tôi lạ lùng: “Anh lên ở bến từ sáng mà. Nói chuyện bình thường.”")
             .Say(K, N, "Bình thường là thế nào?")
             .Narrate("“Bình thường hơn bây giờ.”")
             .Choice("Mảnh da", 0, SceneDefinition.Opt("Mở mảnh da ra"))
             .Backdrop("bg_manh_da_dung_dem")
             .Narrate("Dưới ba dòng mực xanh có thêm dòng thứ tư, mực còn mới, chữ của tôi:")
             .Pause(800)
             .Narrate("“Đừng đếm.”")
             .Pause(2000)
             .Narrate("Tôi không nhớ gì về chuyện đếm. Chỉ nhớ đã nghĩ về gia đình và thấy rất ấm.")
             .Pause(1200)

             .Music("bgm_duong_len_trai").Backdrop("bg_xe_khach_cua_so_ruong")
             .Label("xe")
             .Choice("Trên xe", 1,
                 OptIf("Nhìn ra cửa sổ", "cua_so", "!STORY_FLAG p07_nhin_ra"),
                 SceneDefinition.Opt("Mở phần của ông nội trong sổ bìa vải", "so_ong"))
             .Label("cua_so")
             .Narrate("Ruộng. Cột điện. Không có gì tôi nhận ra.")
             .Flag("p07_nhin_ra")
             .Goto("xe")
             .Label("so_ong")
             .Narrate("Ông vẽ một ngã ba có cây gạo, một cây cầu sắt, một con rãnh nước chảy ngược chiều đường.")
             .Music("")
             .Backdrop("bg_duong_nga_ba_cay_gao", 900)
             .Narrate("Xe chạy qua một cây gạo ở ngã ba.")
             .Pause(1200)
             .Backdrop("bg_duong_cau_sat", 900)
             .Narrate("Mười phút sau, cây cầu sắt.")
             .Pause(1200)
             .Backdrop("bg_duong_ranh_nuoc_chay_nguoc", 900)
             .Narrate("Rồi một con rãnh nước chạy dọc bên đường, chảy ngược chiều xe.")
             .Pause(2000)
             .Backdrop("bg_xe_khach_hang_ghe_cuoi")
             .Narrate("Tôi ghi: “Mất khoảng 24 giờ. Đang đi đúng đường ông vẽ. Không nhớ đã chọn đi.”")
             .Narrate("Một người điên mà con đường lại biết mình đi đâu, thì tôi chưa có cột nào để ghi.")
             .Pause(1000)
             .FadeOut(900)
             .Then("P08_khoa_a")
             .End();
            return s;
        }

        // ---------------------------------------------------------------- P08 — Người đứng đợi (Bờ Rìa)

        static SceneDefinition P08a()
        {
            var s = Scene("P08_khoa_a", "Người đứng đợi ở chữ Rìa",
                "Xe khách chỉ chở tới tấm biển BỜ RÌA (B.R., như biên bản của cha); Mận đón Khoa, “Anh tới trễ hai phút”, đưa lá thư ký tên Khoa gửi quán bà Sáu, dấu bưu điện 21/09/2026, rồi dặn ở đây nói “sáu với một”. " +
                "Cả quán đồng thanh “Mai trời nắng” khi Khoa hỏi nhà anh lên đây làm gì; xe về duy nhất là anh Tư, 15:00 mai, nên Khoa ngủ tạm ở phòng cạnh bếp lò.",
                "Hub_Village");

            s.Music("").Backdrop("bg_bien_bo_ria_xe_khach", 0).FadeIn(900)
             .Banner("Bờ Rìa · 14:02", 1500)
             .Narrate("Tài xế dừng ở tấm biển BỜ RÌA: “Tôi chỉ chở tới chữ Rìa thôi.”")
             .Say(K, N, "Sao không chở vào hẳn?")
             .Narrate("“Vào thì phải ra.” Ông quay đầu xe ngay giữa đường, chạy như sợ trễ giờ của ai đó.")
             .Sfx("sfx_xe_khach_di_xa")
             .PlaceAt("player", "village:spawn", CardinalDirection.North)
             .PlaceAt(CastIds.Man, "village:gate", CardinalDirection.South)
             .CameraTo("village:gate", 0)
             .Backdrop("", 1200)
             .Narrate("Dưới biển, dòng viết tay: “Xe quay đầu ở sân quán. Đừng quay trong ruộng. Ruộng không nhận lời xin lỗi.”")
             .Narrate("Bờ Rìa. B.R. Hai chữ trên biên bản tai nạn của cha.")
             .Pause(800)
             .Narrate("Một cô gái tay dính cám, cầm dù đen, nhìn đồng hồ rồi nhìn tôi.")
             .MoveTo("player", "village:gate")
             .Say(CastIds.Man, N, "Anh tới trễ hai phút. Xe lịch hai giờ.", R)
             .Say(K, N, "Cô đợi ai?")
             .Say(CastIds.Man, N, "Anh.", R)
             .Narrate("Cô tên Mận. Cô rút ra một lá thư đã nhàu.")
             .Choice("Lá thư", 0, SceneDefinition.Opt("Đọc lá thư"))
             .Backdrop("bg_la_thu_gui_quan_ba_sau")
             .Narrate("Gửi quán bà Sáu, dấu bưu điện 21/09/2026: “Tôi là con nhà Hậu – Thủy. Tôi sẽ lên khi đủ sức,")
             .Narrate("đi chuyến xe chiều. Nhờ người ra đón.” Ký tên: Khoa. Nét chữ của tôi. Tôi không nhớ đã gửi.")
             .Backdrop("", 700)
             .Say(CastIds.Man, N, "Gần ba tuần nay chiều nào em cũng ra đón chuyến hai giờ. Bà Sáu dặn vậy.", R)
             .Narrate("Có vẻ như lúc tôi không để ý, tôi là một người rất chu đáo.");
            Custom(s, Notebook("p08_la_thu_21_09_2026"));
            s.Music("bgm_bo_ria_lang")
             .MoveTo(CastIds.Man, "village:well", true)
             .MoveTo("player", "village:well")
             .Say(CastIds.Man, N, "Ở đây mình nói “sáu với một” nha anh. Đừng nói con số kia. Lỡ nói là tối phải nấu thêm nồi cháo.", R)
             .Say(K, N, "Ai đặt ra quy định?")
             .Say(CastIds.Man, N, "Xóm.", R)
             .Narrate("Tôi ghi: “Xóm có quy định. Xóm tự đặt. Xóm là ai: chưa rõ.”")
             .MoveTo(CastIds.Man, "village:station:nha_van_hoa", true)
             .MoveTo("player", "village:station:nha_van_hoa")
             .Say(CastIds.Man, N, "Giếng chung. Nhà văn hóa. Cây này mùa mưa đừng đứng dưới. Cây kia mùa nào cũng đừng.", R)
             .Narrate("Ai nghe “con nhà Hậu – Thủy” cũng gật đầu như nghe một tin đã biết từ lâu. Chưa ai hỏi tôi lên làm gì.")

             .PlaceAt(CastIds.AnhTu, "village:npc:anh_tu", CardinalDirection.South)
             .CameraTo("village:npc:anh_tu", 900)
             .MoveTo(CastIds.Man, "village:npc:anh_tu", true)
             .MoveTo("player", "village:npc:anh_tu")
             .Sfx("sfx_xe_may_no_may")
             .Narrate("Một anh sơ mi trắng đóng thùng đang nổ máy xe.")
             .Say(CastIds.AnhTu, Portraits.Happy, "Con nhà Hậu – Thủy hả. Anh đi ăn cưới xã bên, về rồi nói chuyện.", R)
             .MoveTo(CastIds.AnhTu, "village:gate", true)

             .PlaceAt(CastIds.BaSau, "village:eatery", CardinalDirection.South)
             .PlaceAt(CastIds.AnhCa, "village:station:quan_ba_sau", CardinalDirection.West)
             .CameraTo("village:eatery", 900)
             .MoveTo(CastIds.Man, "village:eatery", true)
             .MoveTo("player", "village:eatery")
             .Narrate("Quán bà Sáu ở cuối vòng. Bà nhìn tôi từ đầu đến chân.")
             .Say(CastIds.BaSau, N, "Giống mẹ.", R)
             .Narrate("Bà dọn cơm, đặt thêm một cái bát úp ở ghế trống. Bàn nào trong quán cũng có một cái bát úp.")
             .Label("hoi")
             .Choice("Hỏi bà Sáu", 3,
                 OptIf("Ba tôi hồi đó lên đây làm gì?", "ba", "!STORY_FLAG p08_hoi_ba"),
                 OptIf("Còn mẹ tôi?", "me", "!STORY_FLAG p08_hoi_me"),
                 OptIf("Còn bé An?", "an", "!STORY_FLAG p08_hoi_an"),
                 SceneDefinition.Opt("Nhà tôi lên đây làm gì?", "nha"))
             .Label("ba")
             .Say(CastIds.BaSau, N, "Sửa máy cho ông Năm. Mấy mùa hè liền.", R)
             .Flag("p08_hoi_ba")
             .Goto("hoi")
             .Label("me")
             .Say(CastIds.BaSau, Portraits.Happy, "Mẹ con ăn cay giỏi hơn cả quán này.", R)
             .Flag("p08_hoi_me")
             .Goto("hoi")
             .Label("an")
             .Say(CastIds.BaSau, Portraits.Sad, "Con bé đặt tên cho hết đàn heo. Có lần một con bị bán, nó khóc cả buổi.", R)
             .Flag("p08_hoi_an")
             .Goto("hoi")
             .Label("nha")
             .Say(K, N, "Nhà tôi lên đây làm gì?")
             .Music("")
             .Narrate("Bà chưa kịp mở miệng, cả quán đã đồng thanh như tập sẵn:")
             .Narrate("“Mai trời nắng.”")
             .Sfx("sfx_mua_tren_mai")
             .Pause(1600)
             .Narrate("Ngoài trời đang mưa.")
             .Pause(1300)
             .Narrate("Cột phải: “Cả quán nói cùng một câu. Có Mận làm chứng.” Ngoài lề: “Tập lúc nào?”")
             .Say(CastIds.AnhCa, N, "Cậu sinh ngày nào?", R)
             .Narrate("Tôi chỉ vào ngày sinh trong sổ. Anh ta gạch một dòng trong cuốn lịch, không giải thích.")
             .Music("bgm_bo_ria_lang")
             .Say(K, N, "Đường về thành phố đi thế nào ạ?")
             .Say(CastIds.BaSau, N, "Có anh Tư chạy xe ôm ra chợ huyện. Mai ba giờ chiều. Nó dậy trễ.", R)
             .Say(K, N, "Còn cách nào khác không bà?")
             .Say(CastIds.BaSau, N, "Có. Đi bộ.", R)
             .Narrate("Mười hai cây số, qua hai cánh đồng không nhận lời xin lỗi. Vậy là tôi ở lại một đêm.")
             .Say(CastIds.BaSau, N, "Trại ông Năm còn cái phòng cạnh bếp lò, chăn gối giặt rồi. Cậu ngủ tạm.", R)
             .Narrate("Tôi ghi: “Xe: anh Tư, 15:00 mai.” Lịch trình của tôi phụ thuộc hoàn toàn vào một đám cưới.")
             .Pause(800)
             .FadeOut(900)
             .Then("P08_khoa_b")
             .End();
            return s;
        }

        static SceneDefinition P08b()
        {
            var s = Scene("P08_khoa_b", "Phòng cạnh bếp lò",
                "Chạng vạng, giữa cánh đồng vàng có một đốm sáng đứng yên trong gió; ở trại, tay Khoa tự biết mở then cổng xệ, tấm ván thứ ba kêu, công tắc đèn kho ở tầm tay trẻ con. " +
                "Đêm, anh choáng, ngã vào thành lò, sổ bìa vải rơi mở ở trang cuối: chữ cha, hè 2017, dặn ai mang trả mảnh da thì “số quay vào trong”.");

            s.Music("bgm_bo_ria_chang_vang").Backdrop("bg_dong_vang_chang_vang", 0).FadeIn(1200)
             .Narrate("Chạng vạng, cánh đồng sau trại ngả vàng như lúa sắp gặt. Giữa đồng có một đốm sáng nhỏ như đom đóm.")
             .Music("")
             .Pause(800)
             .Narrate("Gió thổi, cỏ rạp. Nó không bay, đứng nguyên chỗ cũ.")
             .Say(K, N, "Cô có thấy không?")
             .Say(CastIds.Man, N, "Đồng mình chiều nào cũng đẹp vậy đó anh.", R)
             .Narrate("Cột trái: “Đom đóm. Không bay.” Ngoài lề, chẳng hiểu vì sao: “Ấm.”")
             .Pause(1500)
             .Narrate("Mận gỡ một cọng cỏ dính trên cây dù, đưa cho tôi.")
             .Choice("Cọng cỏ", 0, SceneDefinition.Opt("Nhận lấy"))
             .Narrate("Tôi nhận bằng hai tay, lòng bàn tay ngửa lên, như nhận thứ gì quý. Tôi không nhớ mình học cách đó ở đâu.")

             .Music("bgm_trai_cu_chieu").Backdrop("bg_trai_cong_then_xe")
             .Choice("Cổng trại", 0, SceneDefinition.Opt("Mở then cổng"))
             .Sfx("sfx_then_cong_xe")
             .Narrate("Tay tôi tự biết nhấc then cổng xệ lên rồi mới kéo. Tôi biết tấm ván thứ ba sẽ kêu. Nó kêu.")
             .Narrate("Công tắc đèn kho nằm thấp, đúng tầm tay một đứa bé. Tôi không cần tra sổ để biết mình đang ở đâu.")
             .Narrate("Điều đó làm tôi lo hơn mọi giọng nói.")
             .Backdrop("bg_trai_mang_heo_gia_chet")
             .Sfx("sfx_xo_cam")
             .Narrate("Dưới máng, một con heo chân trắng nằm ngửa giả chết. Nghe tiếng xô cám, nó lật dậy ngay.")
             .Narrate("Mận bật cười, rồi chờ tôi cười theo.")
             .Pause(1200)
             .Say(CastIds.Man, N, "Người ta thấy là cười đó anh.", R)
             .Narrate("Tôi ghi: “Con heo giả chết. Buồn cười.” Mận thôi chờ.")
             .Backdrop("bg_phong_canh_bep_lo_toi")
             .Narrate("Phòng tôi cạnh bếp lò, thành lò đen muội. Trên bậu cửa sổ, một cái chai rỗng trong vòng tròn sạch.")
             .Say(CastIds.Man, N, "Cái chai đó có từ hồi em chưa đẻ. Không ai dám dọn.", R)
             .Say(K, N, "Tại sao?")
             .Say(CastIds.Man, N, "Tại không ai dọn.", R)
             .Pause(1000)

             .FadeOut(900).Music("").Backdrop("bg_phong_canh_bep_lo_dem_den_dau", 0).FadeIn(900)
             .Narrate("Đêm, bên đèn dầu, tôi cố nhớ vì sao mình lên xe. Cơn đau như cây đinh lại tới.")
             .Choice("Đầu đau như có đinh", 0, SceneDefinition.Opt("Đứng dậy mở cửa sổ"))
             .Sfx("sfx_vai_dap_lo_gach")
             .Narrate("Sàn nghiêng. Vai tôi đập vào thành lò, muội rơi lả tả. Cuốn sổ bìa vải rơi, mở ở trang cuối.")
             .Narrate("Sổ toàn chuyện nghỉ hè: chỗ bán xăng rẻ, “mua thêm gioăng”, An ghi “con này ghét anh Khoa”.")
             .Choice("Sổ bìa vải", 0, SceneDefinition.Opt("Lật mặt trong bìa sau"))
             .Backdrop("bg_so_bia_vai_trang_cuoi_cha")
             .Narrate("Chữ của cha, mực đã phai: “Bờ Rìa, hè 2017. Ông Năm cho ở phòng cạnh bếp lò.")
             .Narrate("Mảnh da lấy ra mang về. Sau này ai mang trả thì nhớ: số quay vào trong. Đừng để số nhìn ra ngoài.”")
             .Flag("p08_doc_trang_cha");
            Custom(s, Notebook("p08_trang_cuoi_cua_cha"));
            s.Pause(2000)
             .Backdrop("bg_phong_canh_bep_lo_dem_den_dau")
             .Narrate("Cột phải: “Cha từng ở phòng này. Mảnh da từng ở đây.” Ngoài lề: “Trả vào đâu?”")
             .Pause(1300)
             // mask slip of chapter 0 (story bible 2.7): late laugh, no music, no camera, one line
             .Narrate("Gần nửa đêm, tôi bật cười to và lâu vì con heo lúc chiều. Tôi ghi: “Cười. Trễ sáu tiếng.”")
             .Pause(1200)
             .FadeOut(1200)
             .Then("P08_khoa_c")
             .End();
            return s;
        }

        static SceneDefinition P08c()
        {
            var s = Scene("P08_khoa_c", "Cái khe cạnh bếp lò",
                "Sáng hôm sau bà Sáu, Tám Vá, ông Tồn mời Khoa ở lại trông trại; anh từ chối. Nắng làm lộ một cái khe kim loại lạnh sau lớp muội ở thành lò. " +
                "Khoa trả mảnh da vào khe, dấu khắc quay vào trong, buông tay chờ hai phút; hốc tường mở: bảy cột vạch chiều cao, cột của anh được đo tiếp tới 13/10/2026 và khớp đúng đầu anh.");

            s.Music("bgm_trai_cu_sang").Backdrop("bg_phong_canh_bep_lo_sang", 0).FadeIn(1200)
             .Sfx("sfx_go_ba_cai")
             .Narrate("Sáng ra, bà Sáu, Tám Vá và ông Tồn mang lên một nồi cháo. Tám Vá gõ thành lò ba cái.")
             .Say(CastIds.TamVa, N, "Còn tốt.", R)
             .Say(CastIds.BaSau, Portraits.Worried, "Cậu ở lại trông trại giùm ông Năm. Trại bỏ không lâu rồi, heo nó buồn.", R)
             .Say(K, N, "Cảm ơn bà. Tôi có việc ở thành phố. Ba giờ chiều nay tôi về, xe anh Tư.")
             .Say(CastIds.TamVa, N, "Vậy thôi.", R)
             .Narrate("Ông Tồn ghi thêm một dòng. Không ai ép. Đó lại là điều khiến tôi khó từ chối nhất.")
             .Narrate("Tôi còn sáu tiếng. Tôi định dùng chúng để gấp chăn.")

             // Layer 1 — find the slot
             .Label("tang1")
             .Choice("Phòng cạnh bếp lò", 1,
                 OptIf("Gấp chăn", "gap_chan", "!STORY_FLAG p08_gap_chan"),
                 SceneDefinition.Opt("Nhìn vệt nắng trên thành lò", "vet_nang"))
             .Label("gap_chan")
             .Narrate("Tôi gấp chăn. Nắng sáng chiếu xiên vào thành lò, đúng chỗ vai tôi đập tối qua.")
             .Flag("p08_gap_chan")
             .Goto("tang1")
             .Label("vet_nang")
             .Backdrop("bg_thanh_lo_vet_nang")
             .Choice("Thành lò", 0, SceneDefinition.Opt("Cạo lớp muội chỗ nắng chiếu"))
             .Sfx("sfx_cao_muoi")
             .Narrate("Dưới muội là một cái khe hẹp viền kim loại, rộng chừng một bàn tay. Viền lạnh hơn hẳn gạch xung quanh.")

             // Layer 2 — choose the object (hints after 4 and 6 wrong tries)
             .Label("vat")
             .Choice("Đưa gì vào khe?", 0,
                 SceneDefinition.Opt("Đồ trong ba lô", "balo"),
                 OptIf("Đồ trong phòng, trong túi áo", "phong", "!STORY_FLAG p08_phong_het"),
                 OptIf("Chạm vào viền khe", "cham", "!STORY_FLAG p08_cham"))
             .Label("cham")
             .Flag("p08_cham")
             .Narrate("Lạnh.")
             .Goto("vat")
             .Label("balo")
             .Choice("Ba lô", 5,
                 OptIf("Chìa khóa nhà cũ", "v_chia", "!STORY_FLAG p08_v_chia"),
                 OptIf("Thẻ căn cước", "v_cccd", "!STORY_FLAG p08_v_cccd"),
                 OptIf("Vé xe 07:02", "v_ve", "!STORY_FLAG p08_v_ve"),
                 OptIf("Tấm ảnh gia đình", "v_anh", "!STORY_FLAG p08_v_anh"),
                 OptIf("Chiếc vòng của mẹ", "v_vong", "!STORY_FLAG p08_v_vong"),
                 SceneDefinition.Opt("Mảnh da", "v_da"))
             .Goto("vat")
             .Label("phong")
             .Choice("Trong phòng", 0,
                 OptIf("Cái thìa của bà Sáu", "v_thia", "!STORY_FLAG p08_v_thia"),
                 OptIf("Cái cờ lê nhỏ Tám Vá để quên", "v_cole", "!STORY_FLAG p08_v_cole"),
                 OptIf("Tờ hóa đơn tiền điện", "v_hoadon", "!STORY_FLAG p08_v_hoadon"))
             .Goto("vat")
             .Label("v_chia").Flag("p08_v_chia").Narrate("Chìa khóa nhà cũ: dày quá, chỉ lọt được cái đầu.").Goto("dem")
             .Label("v_cccd").Flag("p08_v_cccd").Narrate("Thẻ căn cước kẹt nửa chừng. Căn cước của tôi không có giá trị ở đây.").Goto("dem")
             .Label("v_ve").Flag("p08_v_ve").Narrate("Vé xe 07:02: mềm quá, gập đôi ngay ở miệng khe.").Goto("dem")
             .Label("v_anh").Flag("p08_v_anh").Narrate("Tấm ảnh gia đình: tôi cầm lên rồi lại cất đi. Tôi không nỡ.").Goto("dem")
             .Label("v_vong").Flag("p08_v_vong").Narrate("Chiếc vòng lọt vừa khít. Tôi xoay đủ bảy hướng. Nó đã làm xong việc của nó ở nhà cũ.").Goto("dem")
             .Label("v_thia").Flag("p08_v_thia").Narrate("Cái thìa của bà Sáu lọt thỏm vào trong, không chạm vào đâu cả.").Goto("phong_kiem")
             .Label("v_cole").Flag("p08_v_cole").Narrate("Cái cờ lê kêu một tiếng khô. Không có gì.").Goto("phong_kiem")
             .Label("v_hoadon").Flag("p08_v_hoadon").Narrate("Cái khe không nhận hóa đơn tiền điện. Ít nhất nó có nguyên tắc.").Goto("phong_kiem")
             .Label("phong_kiem")
             .IfGoto("!STORY_FLAG p08_v_thia", "dem")
             .IfGoto("!STORY_FLAG p08_v_cole", "dem")
             .IfGoto("!STORY_FLAG p08_v_hoadon", "dem")
             .Flag("p08_phong_het")
             .Label("dem")
             .IfGoto("STORY_FLAG p08_sai_6", "vat")
             .IfGoto("STORY_FLAG p08_sai_5", "dem6")
             .IfGoto("STORY_FLAG p08_sai_4", "dem5")
             .IfGoto("STORY_FLAG p08_sai_3", "dem4")
             .IfGoto("STORY_FLAG p08_sai_2", "dem3")
             .IfGoto("STORY_FLAG p08_sai_1", "dem2")
             .Flag("p08_sai_1").Goto("vat")
             .Label("dem2").Flag("p08_sai_2").Goto("vat")
             .Label("dem3").Flag("p08_sai_3").Goto("vat")
             .Label("dem4").Flag("p08_sai_4")
             .Narrate("Tôi ngồi bệt xuống, tay vẫn đặt trên viền kim loại. Cái lạnh này quen. Tôi đã cảm thấy nó một lần rồi.")
             .Goto("vat")
             .Label("dem5").Flag("p08_sai_5").Goto("vat")
             .Label("dem6").Flag("p08_sai_6");
            Custom(s, Notebook("p08_ranh_khac_lanh_hon"));
            s.Narrate("Tôi lật sổ về những trang ở nhà cũ: “Mảnh da. Rãnh khắc lạnh hơn phần da xung quanh.”")
             .Goto("vat")
             .Label("v_da")
             .Narrate("Mảnh da. Rãnh khắc lạnh như viền khe. Phòng cạnh bếp lò. Chỗ cũ. Trả.")

             // Layer 3 — which side faces in
             .Label("chieu")
             .Choice("Mặt nào quay vào lòng lò?", 1,
                 OptIf("Chữ của tôi quay vào, dấu khắc quay ra", "sai_chieu", "!STORY_FLAG p08_sai_chieu"),
                 SceneDefinition.Opt("Dấu khắc quay vào, chữ của tôi quay ra", "dung_chieu"))
             .Label("sai_chieu")
             .Sfx("sfx_ket_cung");
            Custom(s, Notebook("p08_trang_cuoi_cua_cha"));
            s.Narrate("Nửa chừng thì kẹt. Trang cuối của cha tự mở ra trong đầu tôi: “Số quay vào trong.”")
             .Flag("p08_sai_chieu")
             .Goto("chieu")
             .Label("dung_chieu")
             .Sfx("sfx_truot_em")
             .Narrate("Mảnh da trượt vào êm như chưa từng bị lấy ra. Rãnh khắc khớp vào thứ gì đó bên trong.")

             // Layer 4 — hands off for two real minutes
             .Sfx("sfx_tach").Pause(500).Sfx("sfx_tach")
             .Narrate("Một cái. Hai cái. Rồi thôi.")
             .Label("cho")
             .Choice("Mảnh da thò ra khỏi tường như lá thư nhét dở vào hòm.", 2,
                 OptIf("Ấn thêm", "an_them", "!STORY_FLAG p08_an_them"),
                 OptIf("Rút ra, đưa vào lại", "rut_ra", "!STORY_FLAG p08_rut_ra"),
                 SceneDefinition.Opt("Buông tay. Đứng đợi.", "buong"))
             .Label("an_them")
             .Sfx("sfx_tach_nguoc")
             .Narrate("Không nhúc nhích. Rồi hai tiếng tách nhả ngược lại, như hai ngón tay vừa mở ra. Về lại từ đầu.")
             .Flag("p08_an_them")
             .Goto("cho_dem")
             .Label("rut_ra")
             .Sfx("sfx_tach_nguoc")
             .Narrate("Hai tiếng tách nhả ngược lại. Đưa vào lần nữa: một cái, hai cái, rồi thôi. Tôi ghi: “Hai. Rồi kẹt.”")
             .Flag("p08_rut_ra")
             .Goto("cho_dem")
             .Label("cho_dem")
             .IfGoto("!STORY_FLAG p08_an_them", "cho")
             .IfGoto("!STORY_FLAG p08_rut_ra", "cho")
             .Narrate("Bảy buổi sáng tôi đã đứng trước gương chờ một cái bóng bắt kịp mình.")
             .Narrate("Đợi là việc duy nhất tôi được huấn luyện bài bản.")
             .Goto("cho")
             .Label("buong")
             .Music("")
             .Narrate("Tôi buông tay, lùi một bước, nhìn đồng hồ. 11:16. Không chạm vào gì.")
             .Sfx("sfx_dong_ho_deo_tay_tich_tac")
             .Pause(60000)
             .Banner("11:17 · Không có gì.", 3000)
             .Pause(57000)
             .Sfx("sfx_tach").Sfx("sfx_tach").Sfx("sfx_tach")
             .Pause(700)
             .Sfx("sfx_tach")
             .Narrate("11:18. Bên trong tường, ba tiếng tách liền nhau, rồi một tiếng nữa. Bốn cái.")
             .Sfx("sfx_gach_truot")
             .Narrate("Viên gạch phía trên khe lùi vào, trượt sang ngang. Sau đó là một hốc tường, ngang tầm mắt tôi.")
             .Pause(800)

             .Backdrop("bg_hoc_tuong_bay_cot", 900)
             .Narrate("Vôi trắng, bảy cột vạch bút chì. Đầu mỗi cột, nét chữ tròn vo của An:")
             .Narrate("Ông. Bà. Ba. Mẹ. Anh Minh. An. Anh Khoa.")
             .Narrate("Vạch kèm số năm từ 2008. Cột ông bà ngang nhau, An vẫn kẻ cho đủ. Các cột dừng ở 2017.")
             .Pause(1500)
             .Narrate("Trừ cột của tôi.")
             .Pause(1500)
             .Narrate("2018 bỏ trống. Rồi 2019, 2020, 2021… Nét chì không còn là chữ của An.")
             .Narrate("Vạch cuối cùng bằng bút bi xanh: 13/10/2026.")
             .Pause(1500)
             .Narrate("Hôm qua.")
             .Pause(2200)
             .Choice("Hốc tường", 0, SceneDefinition.Opt("Quay lưng lại, áp đầu vào tường"))
             .Pause(1600)
             .Narrate("Khớp.")
             .Pause(3200)
             .Narrate("Cột phải: “Hốc tường. Bảy cột. Cột thứ bảy đo tiếp đến hôm qua. Vạch cuối bằng bút bi xanh.”")
             .Narrate("Ngoài lề: “Ai đã đo tôi suốt những năm tôi không có ở đây?”");
            Custom(s, Notebook("nb_04_hoc_tuong_bay_cot"));
            s.Pause(2000)
             .FadeOut(1200)
             .Then("P08_khoa_d")
             .End();
            return s;
        }

        static SceneDefinition P08d()
        {
            var s = Scene("P08_khoa_d", "Cậu tới nhận trại",
                "Ba giờ chiều anh Tư tới cổng trại; Khoa trả tiền chờ và chọn ở lại để biết ai đã đo chiều cao mình. " +
                "Bà Sáu đưa hồ sơ bàn giao trại ông Năm (ô “Người nhận” ghi bút chì “(sáu với một)”, con dấu hai móc bốn nêm); ông Tồn chép lại dấu, Khoa ký nhận trại.",
                "Hub_Village");

            s.Music("").Backdrop("bg_trai_cong_ban_ngay_xe_om", 0).FadeIn(900)
             .Banner("15:00", 1300)
             .Sfx("sfx_coi_xe_may")
             .Narrate("Ba giờ chiều, anh Tư bấm còi ở cổng trại, vẫn mặc bộ áo đi ăn cưới hôm qua.")
             .Say(CastIds.AnhTu, N, "Đi luôn không?", R)
             .Narrate("Tôi đứng ở then cổng, cái then tay tôi tự biết mở, rất lâu.")
             .Choice("Anh Tư đang đợi", 0, SceneDefinition.Opt("Đưa anh tiền xe, coi như tiền chờ"))
             .Say(CastIds.AnhTu, N, "Không đi à?", R)
             .Say(K, N, "Chưa.")
             .Say(CastIds.AnhTu, N, "Vậy mai?", R)
             .Say(K, N, "Chưa biết.")
             .Say(CastIds.AnhTu, Portraits.Happy, "Ở đây chưa biết cũng là một ngày trong tuần.", R)
             .Sfx("sfx_xe_may_di_xa")
             .Pause(900)
             .FadeOut(900)

             .PlaceAt(CastIds.BaSau, "village:eatery", CardinalDirection.South)
             .PlaceAt(CastIds.OngTon, "village:station:quan_ba_sau", CardinalDirection.West)
             .PlaceAt("player", "village:well", CardinalDirection.South)
             .CameraTo("village:eatery", 0)
             .Backdrop("", 0)
             .FadeIn(900)
             .Music("bgm_bo_ria_lang")
             .MoveTo("player", "village:eatery")
             .Sfx("sfx_rua_bat")
             .Say(K, N, "Cái trại. Tôi nhận.")
             .Emote(CastIds.BaSau, "...")
             .Narrate("Bà Sáu không hỏi vì sao. Tôi tự nói, chủ yếu để chính mình nghe:")
             .Say(K, N, "Có người đã đo chiều cao tôi ở đây suốt mấy năm tôi không có ở đây.")
             .Say(K, N, "Muốn biết là ai thì phải ở.")
             .Backdrop("bg_ho_so_ban_giao_trai")
             .Narrate("Hồ sơ bàn giao: 12 Ô Đất, một đàn nhỏ, nhà chính, kho, Phòng Xây Dựng cấp 1 chưa có người trực.")
             .Narrate("Ô “Người nhận” trống, cạnh đó bút chì: “(sáu với một)”. Con dấu đỏ: hai dấu móc, bốn vạch nêm.")
             .Pause(1300)
             .Backdrop("bg_phong_canh_bep_lo_chieu")
             .Narrate("Ông Tồn theo tôi lên trại, chép hai dấu móc và bốn vạch nêm vào sổ, tuyệt nhiên không đọc thành tiếng.");
            Custom(s, Notebook("p08_ban_chep_dau_nem_ong_ton"));
            s.Choice("Hồ sơ bàn giao", 0, SceneDefinition.Opt("Ký nhận trại"))
             .Sfx("sfx_ky_ten")
             .Narrate("Mực thấm chậm, như tờ giấy đang cân nhắc có nên nhận tôi không.")
             .Narrate("Cột phải: “Ở lại để biết ai đã đo. Xe về: anh Tư, khi nào tôi muốn.”")
             .Pause(1500)
             .Flag("prologue_done")
             .FadeOut(1200);
            Custom(s, NarrativeCustomIds.GotoMapPrefix + "Farm");
            s.End();
            return s;
        }
    }
}
