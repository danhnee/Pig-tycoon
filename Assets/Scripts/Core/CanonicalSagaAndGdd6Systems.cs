using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    // =========================================================================
    // 1. CỐT TRUYỆN CHÍNH THỨC MÙA 1: "SÁU VỚI MỘT" (PHẦN 1 ĐẾN PHẦN 12 — FULL BIBLE)
    // =========================================================================

    public enum SagaFactionId
    {
        ThatVien_VongBayChiecGhe = 1,      // 1. Thất Viên ("Lục Tế Bảo Nhất", giữ Khoa trễ 2 phút ở ngã tư 07:00 -> 07:02)
        HoangHonThuHonGia_Sirphina = 2,    // 2. Hoàng Hôn Thu Hồn Giả (Sirphina — giữ 6 linh hồn gia đình ở bờ hoàng hôn)
        DongCanhGio = 3,                   // 3. Dòng Canh Giờ (giữ 24 chốt lịch cổ, vé số 24, mảnh da Babylon 24, hợp đồng B.R.)
        ThiTranBoRia_PhongAn100Nam = 4,    // 4. Thị trấn Bờ Rìa (phong ấn ký ức 100 năm của những người lính gác thung lũng)
        SoLuuTruNgoaiLe = 5,               // 5. Sở Lưu Trữ Ngoại Lệ (tổ chức trần thế khai thác khe nứt bằng thùng kim loại đen)
        KhuRungKhongTen = 6,               // 6. Khu Rừng Không Tên (biên giới hoàng hôn vĩnh cửu không có mặt trời mọc/lặn)
        BiNhanVaKhoKhac = 7                // 7. Bí Nhân & Kho Khác (giữ lời hứa ngàn năm 0 Vàng, chỉ đổi vật mang nhân quả)
    }

    [Serializable]
    public sealed class CanonicalFamilyTimelineRecord
    {
        public int OrderIndex { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string RoleOrAge { get; set; } = string.Empty;
        public string EventDateAndTime { get; set; } = string.Empty;
        public string HiddenNumberRule { get; set; } = string.Empty;
        public string RelicLeftBehind { get; set; } = string.Empty;
        public string SirphinaTwilightSign { get; set; } = string.Empty;
        public string FarewellActionInChapter8 { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class CanonicalQuestDefinition
    {
        public string Code { get; set; } = string.Empty; // P01..P08, Q01..Q20, S01..S12
        public string ChapterOrGroup { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TriggerOrCondition { get; set; } = string.Empty;
        public string CoreActionAndStoryTruth { get; set; } = string.Empty;
        public string RewardOrUnlock { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class GddBuildingTableSpec
    {
        public int Number { get; set; }
        public string BlueprintId { get; set; } = string.Empty;
        public string CatalogId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public BuildingCategory Category { get; set; }
        public int FootprintWidthMeters { get; set; }
        public int FootprintHeightMeters { get; set; }
        public int BuildCostGold { get; set; }
        public float RangeMeters { get; set; }
        public int PowerContinuous { get; set; }
        public int PowerPeak { get; set; }
        public int MaxPerFarm { get; set; }
        public bool IsBachVan { get; set; }
    }

    [Serializable]
    public sealed class SpecialtyLockEffectSpec
    {
        public PlayerId CharacterIdentity { get; set; }
        public string TrueSpecialtyTitle { get; set; } = string.Empty;
        public int LockNumber { get; set; } // 1..6
        public string UnlockConditionSummary { get; set; } = string.Empty;
        public string EffectSummary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Hệ thống Cốt Truyện Mùa 1 "Sáu Với Một" (Đầy đủ Phần 1–12: Mở đầu P01–P08, Chính tuyến Q01–Q20,
    /// Phụ tuyến S01–S12, 14 Sự kiện E01–E14, Hồ sơ Tác giả về Thánh Minh Nhật Giả, Hoàng Hôn Thu Hồn Giả Sirphina,
    /// 7 Thế lực, Nhật Quỹ Tàn Khuyết tại Ngọ Môn Không Bóng, và Chương 8 "Lời chào cuối cùng")
    /// kết hợp Bảng Chuẩn GDD v6.0 (Bảng 1.5-D 31 công trình, Bảng 5.4 Giám định 4 kênh, Mục 10.2 &amp; 10.8 Cổng Giữa Game &amp; 6 Khóa Chuyên Biệt).
    /// </summary>
    public static class CanonicalSagaAndGdd6Systems
    {
        // ---------------------------------------------------------------------
        // A. BẢNG 1.4: DÒNG THỜI GIAN GIA ĐÌNH KHOA (8 MỐC CHUẨN PHẦN 1.4)
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<CanonicalFamilyTimelineRecord> FamilyTimelineTable14 = new List<CanonicalFamilyTimelineRecord>
        {
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 1,
                MemberName = "Ông nội Lâm",
                RoleOrAge = "72 tuổi · Ông nội",
                EventDateAndTime = "17/07/2018 (06:58)",
                HiddenNumberRule = "17 + 7 = 24 (24 chốt phong ấn / 24 canh giờ)",
                RelicLeftBehind = "Tờ vé số kiến thiết cũ có 2 chữ số cuối là 24 ép nhựa trong ví da",
                SirphinaTwilightSign = "Mép cơn mưa bóng mây dừng đúng ở bậc cổng, không bước vào sân; người khách xin nước lúc 06:58 áo khô nguyên",
                FarewellActionInChapter8 = "Ông nội Lâm đội chiếc mũ vải cũ bước tới vỗ mạnh vào vai Khoa hai cái như hồi ông còn khỏe."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 2,
                MemberName = "Bà nội Sương",
                RoleOrAge = "69 tuổi · Bà nội",
                EventDateAndTime = "04/09/2018 (07:02)",
                HiddenNumberRule = "Ngày thứ 49 sau ông nội (49 = 7 × 7); Khoa tới trễ đúng 2 phút (07:02)",
                RelicLeftBehind = "Chiếc nhẫn cưới vàng tây mỏng khắc năm 1977 giấu trong hũ gạo",
                SirphinaTwilightSign = "Vệt nước mưa qua tán phượng vĩ chảy xuống hiên có màu đỏ sẫm như ráng chiều; người áo mưa xám đen nắm cổ tay Khoa nói 'Còn sớm'",
                FarewellActionInChapter8 = "Bà nội Sương đưa bàn tay ấm áp vuốt lại mái tóc rối của Khoa: 'Lớn từng này rồi mà vẫn để người ta phải nhắc giờ.'"
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 3,
                MemberName = "Anh trai Minh",
                RoleOrAge = "22 tuổi · Sinh viên năm 3 Xây dựng",
                EventDateAndTime = "19/02/2019",
                HiddenNumberRule = "2019 - 19 = 2000; cách ngày bà mất đúng 168 ngày (168 = 7 × 24)",
                RelicLeftBehind = "Giấy báo học bổng toàn phần đặt ngay ngắn trên bàn học; câu cuối: 'Mày tắt giùm anh cái quạt.'",
                SirphinaTwilightSign = "Tấm gương nhà tắm mờ hơi nước hiện lên khuôn mặt thấp hơn Khoa nửa cái đầu đang nhìn lại",
                FarewellActionInChapter8 = "Anh Minh đấm nhẹ vào bả vai Khoa, cười hết cỡ: 'Anh tự tắt quạt rồi. Mày khỏi lo nữa.'"
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 4,
                MemberName = "Em gái Bé Na",
                RoleOrAge = "10 tuổi · Học sinh tiểu học",
                EventDateAndTime = "13/05/2019",
                HiddenNumberRule = "Tổng chữ số ngày 1+3+0+5+2+0+1+9 = 21 (21 = 3 × 7)",
                RelicLeftBehind = "Bức tranh sáp màu đạt giải nhất 'Gia đình em' (vẽ thêm bóng người thứ 8 bằng bút chì không tô màu đứng ngoài hàng rào) và hạt đậu đen trong túi áo",
                SirphinaTwilightSign = "Hạt đậu đen đem trồng suốt 7 năm không nảy mầm cũng không thối; giấc mơ Bé Na nói 'Anh đừng quay lại'",
                FarewellActionInChapter8 = "Bé Na chạy ùa tới ôm chặt lấy eo Khoa, dúi vào tay anh tờ giấy vẽ sáp màu đã tô kín màu nắng ấm cho tất cả mọi người."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 5,
                MemberName = "Cha Hậu",
                RoleOrAge = "48 tuổi · Kỹ sư thủy lợi",
                EventDateAndTime = "24/08/2019",
                HiddenNumberRule = "Ngày 24/08 — Ngày đầu tiên trong 3 ngày lễ Mundus patet (cổng âm dương mở lần 1 trong năm)",
                RelicLeftBehind = "Bản hợp đồng khảo sát và sửa chữa trạm bơm cũ tại địa danh viết tắt 'B.R.' (Bờ Rìa)",
                SirphinaTwilightSign = "Nước giếng sau nhà tự nhiên ấm lên suốt một đêm; đàn kiến đen bò thành vòng tròn khép kín quanh miệng cống",
                FarewellActionInChapter8 = "Cha Hậu đứng cạnh mẹ, gật đầu nhìn Khoa bằng ánh mắt tin cậy của một người đàn ông giao lại mái nhà cho con trai."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 6,
                MemberName = "Đêm Giữ Chỗ (Không ai mất)",
                RoleOrAge = "Khoa 17 tuổi · Sốt li bì 2 ngày",
                EventDateAndTime = "05/10/2019 (21:14 – 23:58)",
                HiddenNumberRule = "Ngày 05/10 — Ngày thứ hai trong năm cổng Mundus mở; Khoa thức trắng canh cành hoa sứ nên giữ được nụ thứ 7 không nở",
                RelicLeftBehind = "Mép tờ báo cũ của cha có dòng bút chì '02:00. Đã giữ chỗ' và biên lai mua trước 7 suất đất nghĩa trang năm 2017 (suất thứ 7 để trống tên, ký 7 nét gạch)",
                SirphinaTwilightSign = "Nụ hoa sứ thứ 7 khẽ rung lên lúc 23:58 nhưng khép lại vì Khoa vẫn mở mắt nhìn nó",
                FarewellActionInChapter8 = "Suất thứ 7 mãi mãi không còn là chỗ giữ cho cái chết — nụ hoa sứ thứ 7 nở bung trắng muốt dưới nắng chiều."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 7,
                MemberName = "Mẹ Thủy",
                RoleOrAge = "45 tuổi · Giáo viên tiểu học",
                EventDateAndTime = "08/11/2019",
                HiddenNumberRule = "Ngày 08/11 — Ngày thứ ba cổng Mundus mở trong năm (8 + 11 = 19, nối lại số ngày của anh Minh)",
                RelicLeftBehind = "Quyết định nghỉ phép có lương 1 tháng; 6 cái bát úp và duy nhất cái bát của Khoa nằm ngửa; chiếc vòng cổ đồng 7 khía rơi ra dưới gầm bàn cha sau 3 tháng",
                SirphinaTwilightSign = "Chiếc bát ăn cơm của Khoa đặt nằm ngửa trên giá bát như một lời chúc người sống phải tiếp tục ăn cơm",
                FarewellActionInChapter8 = "Mẹ Thủy bước lại gần nhất, đưa tay chỉnh lại cổ áo sơ mi cho Khoa: 'Về nhớ ăn cơm tử tế. Cái bát mẹ để sẵn trên giá rồi đấy.'"
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 8,
                MemberName = "Khoa (Nhân vật chính)",
                RoleOrAge = "Sinh 07/09/2002 · 24 tuổi vào tháng 09/2026",
                EventDateAndTime = "24/09/2026 (07:02)",
                HiddenNumberRule = "24 tuổi, ngày 24, đứng trước gương 7 buổi sáng từ 06:58 đến 07:02; bóng trong gương chớp mắt chậm nửa nhịp",
                RelicLeftBehind = "Vé xe buýt tuyến 24 in giờ 07:02 ngày 24/09/2026 đóng dấu 'ĐÃ SỬ DỤNG' trong hộp bánh quy thiếc dưới bậc cầu thang thứ 7",
                SirphinaTwilightSign = "Người đàn ông áo mưa xám đen (Thất Viên) gọi giật giọng giữ Khoa lại 2 phút ở vạch sơn trắng lúc 07:00 -> 07:02 để tránh thời khắc gặt hồn",
                FarewellActionInChapter8 = "Khoa viết chữ 'KHOA' lên mặt đá Nhật Quỹ Tàn Khuyết, trả lại cây kim đồng hồ cho Sirphina và trở về nhà nấu bữa tối."
            }
        };

        // ---------------------------------------------------------------------
        // B. DANH MỤC ĐẦY ĐỦ 8 CẢNH MỞ ĐẦU (P01–P08), 20 QUEST CHÍNH (Q01–Q20) & 12 QUEST PHỤ (S01–S12)
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<CanonicalQuestDefinition> PrologueScenesP01ToP08 = new List<CanonicalQuestDefinition>
        {
            new CanonicalQuestDefinition { Code = "P01", ChapterOrGroup = "Mở đầu", Title = "Một nhà bình thường", TriggerOrCondition = "Trước tháng 07/2018", CoreActionAndStoryTruth = "Bữa cơm chiều đủ 7 người trong căn nhà có cầu thang gỗ 13 bậc; cành hoa sứ 7 bông (6 nở, 1 nụ) cắm trong chai nước mắm thủy tinh ở chiếu nghỉ.", RewardOrUnlock = "Mở ký ức nền gia đình Khoa" },
            new CanonicalQuestDefinition { Code = "P02", ChapterOrGroup = "Mở đầu", Title = "Cơn mưa dừng ở cổng", TriggerOrCondition = "17/07/2018", CoreActionAndStoryTruth = "Ông nội Lâm mất; tờ vé số đuôi 24 trong ví da và mép mưa bóng mây dừng đúng ở bậc cổng.", RewardOrUnlock = "Dấu vết số 24 đầu tiên" },
            new CanonicalQuestDefinition { Code = "P03", ChapterOrGroup = "Mở đầu", Title = "Hai phút", TriggerOrCondition = "04/09/2018, 07:02", CoreActionAndStoryTruth = "Bà nội Sương mất ngày thứ 49 (7×7); người áo mưa xám đen giữ cổ tay Khoa nói 'Còn sớm' khiến Khoa trễ 2 phút.", RewardOrUnlock = "Kỷ vật nhẫn cưới 1977" },
            new CanonicalQuestDefinition { Code = "P04", ChapterOrGroup = "Mở đầu", Title = "Cái quạt", TriggerOrCondition = "19/02/2019", CoreActionAndStoryTruth = "Anh Minh mất cách bà 168 ngày (7×24); bóng mờ trong gương nhà tắm thấp hơn Khoa nửa cái đầu.", RewardOrUnlock = "Giấy báo học bổng & ký ức cái quạt" },
            new CanonicalQuestDefinition { Code = "P05", ChapterOrGroup = "Mở đầu", Title = "Bức tranh giải nhất", TriggerOrCondition = "13/05/2019", CoreActionAndStoryTruth = "Bé Na mất (tổng chữ số 21 = 3×7); bức tranh 'Gia đình em' có bóng người thứ 8 vẽ chì ngoài hàng rào.", RewardOrUnlock = "Bức tranh giải nhất & hạt đậu đen" },
            new CanonicalQuestDefinition { Code = "P06", ChapterOrGroup = "Mở đầu", Title = "Đêm giữ chỗ", TriggerOrCondition = "24/08/2019 & 05/10/2019", CoreActionAndStoryTruth = "Cha Hậu mất ngày Mundus mở lần 1; ngày 05/10 Khoa sốt li bì thức canh cành hoa sứ từ 21:14 đến 23:58 giữ nụ thứ 7 không nở.", RewardOrUnlock = "Hợp đồng B.R. & Biên lai 7 suất mộ ký 7 nét" },
            new CanonicalQuestDefinition { Code = "P07", ChapterOrGroup = "Mở đầu", Title = "Cái bát nằm ngửa", TriggerOrCondition = "08/11/2019", CoreActionAndStoryTruth = "Mẹ Thủy mất ngày Mundus mở lần 3; 6 cái bát úp trên giá, duy nhất bát của Khoa nằm ngửa; vòng đồng 7 khía rơi ra dưới bàn cha.", RewardOrUnlock = "Chiếc vòng cổ đồng 7 khía của mẹ" },
            new CanonicalQuestDefinition { Code = "P08", ChapterOrGroup = "Mở đầu", Title = "Người đứng đợi", TriggerOrCondition = "21/09/2026 – 24/09/2026 (07:02)", CoreActionAndStoryTruth = "Dùng vòng cổ 7 khía mở khóa ngôi sao 7 cánh trên hộp bánh quy thiếc dưới bậc cầu thang thứ 7 (6 tiếng kêu + cánh thứ 7 im lặng); tai nạn ngã tư lúc 07:02.", RewardOrUnlock = "Mảnh da Babylon 24, vé xe buýt 24 & chuyển cảnh tới Bờ Rìa" }
        };

        public static readonly IReadOnlyList<CanonicalQuestDefinition> TwentyMainQuestsQ01ToQ20 = new List<CanonicalQuestDefinition>
        {
            new CanonicalQuestDefinition { Code = "Q01", ChapterOrGroup = "Chương 1", Title = "Cậu tới nhận trại", TriggerOrCondition = "Ngày 1 tại Bờ Rìa", CoreActionAndStoryTruth = "Bà Sáu trao chìa khóa buộc dây cước đỏ thắt 6 nút; đồng hồ tường dừng ở 07:02; lịch treo tường khoanh 24/09/2018.", RewardOrUnlock = "Mở Farm_Main & Farm_Core" },
            new CanonicalQuestDefinition { Code = "Q02", ChapterOrGroup = "Chương 1", Title = "Ba con heo đầu tiên", TriggerOrCondition = "Sau Q01", CoreActionAndStoryTruth = "Mận giao 3 heo giống Hồng Điền; con heo út chân trắng có vết sẹo số 7 ngược ở tai phải.", RewardOrUnlock = "3 heo giống khởi đầu" },
            new CanonicalQuestDefinition { Code = "Q03", ChapterOrGroup = "Chương 1", Title = "Sáu với một", TriggerOrCondition = "Giao dịch đầu tiên tại Chợ Thị Trấn", CoreActionAndStoryTruth = "Chị Lệ đếm 'năm, sáu, sáu với một, tám'; người Bờ Rìa tuyệt đối không nói số 7 và đáp 'Mai trời nắng' khi bị hỏi.", RewardOrUnlock = "Mở Sổ Hai Cột (Thấy / Có thật)" },
            new CanonicalQuestDefinition { Code = "Q04", ChapterOrGroup = "Chương 2", Title = "Đêm đầu tiên có kẻ cắt rào", TriggerOrCondition = "Đợt quái nhỏ đầu tiên", CoreActionAndStoryTruth = "Đánh退 toán trộm đêm; nhặt được mảnh vải xám đen giống hệt áo mưa người đàn ông ở ngã tư.", RewardOrUnlock = "Mở Phòng Xây Dựng & Hàng rào" },
            new CanonicalQuestDefinition { Code = "Q05", ChapterOrGroup = "Chương 2", Title = " Bậc gạch thứ bảy", TriggerOrCondition = "Khảo sát giếng cạn góc Đông Bắc", CoreActionAndStoryTruth = "Bậc gạch thứ 7 trong lòng giếng cạn kêu bộp rỗng, khắc vạch sâu và nối với bó cáp đồng trục dẫn về Nhà chính.", RewardOrUnlock = "Xác thực mục #1 trong Sổ Hai Cột" },
            new CanonicalQuestDefinition { Code = "Q06", ChapterOrGroup = "Chương 2", Title = "Người không nhận vàng", TriggerOrCondition = "ÁLSK >= 25 ban đêm", CoreActionAndStoryTruth = "Gặp Bí Nhân ngoài hàng rào (<30m, 0 Vàng); Bí Nhân nhìn vòng cổ 7 khía và nói: 'Cái khóa này còn thiếu một tiếng không kêu.'", RewardOrUnlock = "Mở kênh giao dịch Bí Nhân" },
            new CanonicalQuestDefinition { Code = "Q07", ChapterOrGroup = "Chương 3", Title = "Cuốn sổ mất trang", TriggerOrCondition = "Mở Kho & Nhà chính", CoreActionAndStoryTruth = "Tìm thấy sổ nhật ký trại năm 2018 bị xé mất các trang sau ngày 24/09/2018.", RewardOrUnlock = "Xác thực mục #2 trong Sổ Hai Cột" },
            new CanonicalQuestDefinition { Code = "Q08", ChapterOrGroup = "Chương 3", Title = "Tấm ảnh sau lịch", TriggerOrCondition = "Gỡ tờ lịch 24/09/2018", CoreActionAndStoryTruth = "Phát hiện bức ảnh 7 người đứng trước cổng trại năm 2018; người thứ 7 ở góc Tây Bắc bị cạo trắng mặt nhưng đeo vòng đồng 7 khía.", RewardOrUnlock = "Xác thực mục #3 trong Sổ Hai Cột" },
            new CanonicalQuestDefinition { Code = "Q09", ChapterOrGroup = "Chương 3", Title = "Bàn Giải Mã lên đèn", TriggerOrCondition = "Cấp trại >= 8, thuê Cúc", CoreActionAndStoryTruth = "Khởi động Phòng Nghiên Cứu cùng Cúc; phát hiện tần số cộng hưởng của dòng gen Dị Biến trùng với tín hiệu lúc 02:00.", RewardOrUnlock = "Mở Bàn Giải Mã & Thuần Hóa Huyết Thống" },
            new CanonicalQuestDefinition { Code = "Q10", ChapterOrGroup = "Chương 4", Title = "Ánh đèn xanh lúc hai giờ", TriggerOrCondition = "Đợt UFO Trinh Sát đầu tiên", CoreActionAndStoryTruth = "UFO không chỉ bắt heo mà quét chùm tia tìm kiếm cá thể mang dấu ấn ký ức quanh giếng cạn.", RewardOrUnlock = "Xác thực mục #4 trong Sổ Hai Cột" },
            new CanonicalQuestDefinition { Code = "Q11", ChapterOrGroup = "Chương 4", Title = "Chỗ trống giữa đàn", TriggerOrCondition = "Đạt Bầy Đàn Sơ Khai (3 ngày liên tục)", CoreActionAndStoryTruth = "Đàn heo hơn 100 con khi ngủ luôn tự động chừa trống một vòng tròn đường kính 1,2m ở góc Tây Bắc — đúng chỗ người thứ 7 trong ảnh.", RewardOrUnlock = "Mở manh mối Cổng Giữa Game" },
            new CanonicalQuestDefinition { Code = "Q12", ChapterOrGroup = "Chương 4", Title = "Cổng Giữa Game mở khóa", TriggerOrCondition = ">= 8 đợt lớn + 1 điều kiện phụ", CoreActionAndStoryTruth = "Vượt Cổng Giữa Game; mở Xưởng Cường Hóa và đối chiếu vòng tròn trống góc Tây Bắc với tấm ảnh năm 2018.", RewardOrUnlock = "Xác thực mục #5 Sổ Hai Cột & mở 6 Khóa Chuyên Biệt" },
            new CanonicalQuestDefinition { Code = "Q13", ChapterOrGroup = "Chương 5", Title = "Hộp kỹ thuật sáu với một", TriggerOrCondition = "Khảo sát sau vách chuồng chính", CoreActionAndStoryTruth = "Mở hộp kỹ thuật kim loại đen của Sở Lưu Trữ Ngoại Lệ: có 6 kênh đã đóng gói (gắn với 6 mốc ngày gia đình Khoa) và kênh #07 đang chờ.", RewardOrUnlock = "Bằng chứng hộp kỹ thuật 7 kênh" },
            new CanonicalQuestDefinition { Code = "Q14", ChapterOrGroup = "Chương 5", Title = "Không ai nói 'Mai trời nắng' nữa", TriggerOrCondition = "Trình đủ 3 bằng chứng trước dân làng", CoreActionAndStoryTruth = "Bà Sáu và Tám Vá thừa nhận lời thề trăm năm của Bờ Rìa: kiêng số 7 để cỗ máy dưới giếng không khóa được tọa độ người thứ 7.", RewardOrUnlock = "Phá vỡ sự im lặng của thị trấn Bờ Rìa" },
            new CanonicalQuestDefinition { Code = "Q15", ChapterOrGroup = "Chương 5", Title = "Chúa Trộm Rìu Sắt lộ diện", TriggerOrCondition = "Boss Chu kỳ Chúa Trộm Rìu Sắt", CoreActionAndStoryTruth = "Đánh bại Chúa Trộm Rìu Sắt; phát hiện gã chính là cựu đội trưởng bảo vệ từng đánh cắp kiện hàng thứ 7 để ngăn Sở Lưu Trữ Ngoại Lệ.", RewardOrUnlock = "Mảnh bản đồ dẫn vào Khu Rừng Không Tên" },
            new CanonicalQuestDefinition { Code = "Q16", ChapterOrGroup = "Chương 6", Title = "Khe gạch bên lò sưởi", TriggerOrCondition = "Giải đố 4 lớp với mảnh da Babylon 24", CoreActionAndStoryTruth = "Cạo bồ hóng, thử sai vật phẩm, đút mảnh da số 24 quay vào trong và buông tay chờ đúng 2 phút (11:16 -> 11:18) nghe 2+4=24 tiếng click.", RewardOrUnlock = "Bức vẽ 7 cột thước đo vẽ bằng tay trái của Khoa" },
            new CanonicalQuestDefinition { Code = "Q17", ChapterOrGroup = "Chương 6", Title = "Sáu dấu hoàng hôn của Sirphina", TriggerOrCondition = "Đi sâu vào Rừng Nguyên Sinh lúc chiều tà", CoreActionAndStoryTruth = "Gặp Hoàng Hôn Thu Hồn Giả Sirphina; biết rõ 6 dấu hiệu lạ ngày gia đình mất là lúc nàng đón và giữ 6 linh hồn ở bờ hoàng hôn.", RewardOrUnlock = "Mở khóa bí mật 3.000 năm Thánh Minh Nhật Giả" },
            new CanonicalQuestDefinition { Code = "Q18", ChapterOrGroup = "Chương 7", Title = "Trạm thu trong sương sụp đổ", TriggerOrCondition = "Tiến vào trung tâm Khu Rừng Không Tên", CoreActionAndStoryTruth = "Đánh sập trạm thu phát dã chiến của Sở Lưu Trữ Ngoại Lệ và vượt qua Thực Thể Sương.", RewardOrUnlock = "Mở lối vào Ngọ Môn Không Bóng" },
            new CanonicalQuestDefinition { Code = "Q19", ChapterOrGroup = "Chương 7", Title = "Nhật Quỹ Tàn Khuyết", TriggerOrCondition = "Đứng trước đài đá cổ tại Ngọ Môn Không Bóng", CoreActionAndStoryTruth = "Đeo vòng cổ 7 khía trước gương đá (mặt thật ráo hoảnh nhưng bóng trong gương khóc); đặt vòng lên rãnh khuyết của Nhật Quỹ.", RewardOrUnlock = "Lựa chọn vận mệnh tại Nhật Quỹ Tàn Khuyết" },
            new CanonicalQuestDefinition { Code = "Q20", ChapterOrGroup = "Chương 7 & 8", Title = "Rìu Sắt trả hàng & Lời chào cuối cùng", TriggerOrCondition = "Khép khe nứt & trở về trại", CoreActionAndStoryTruth = "Nhận lại kiện hàng 'Gửi trả người gửi' chứa chiếc bát men xanh sứt miệng thứ hai; viết tên 'Khoa', tạm biệt 6 người thân và về nhà ăn tối.", RewardOrUnlock = "Hoàn thành Mùa 1 'Sáu Với Một' & mở Hậu kết thúc" }
        };

        public static readonly IReadOnlyList<CanonicalQuestDefinition> TwelveSideQuestsS01ToS12 = new List<CanonicalQuestDefinition>
        {
            new CanonicalQuestDefinition { Code = "S01", ChapterOrGroup = "Phụ tuyến", Title = "Giám Đốc không chịu nghỉ", TriggerOrCondition = "Nuôi Ngỗng Báo Động 'Giám Đốc'", CoreActionAndStoryTruth = "Con ngỗng Giám Đốc mỗi sáng đứng trên thùng phuy điểm danh đàn heo đúng 6 tiếng kêu rồi nghỉ.", RewardOrUnlock = "+4s cảnh báo sớm đợt quái" },
            new CanonicalQuestDefinition { Code = "S02", ChapterOrGroup = "Phụ tuyến", Title = "Con heo giả chết trên bàn cân", TriggerOrCondition = "Cân con heo chân trắng #01", CoreActionAndStoryTruth = "Đặt lên cân là nằm ngửa cứng đờ báo 0.0 kg; phải gõ máng ăn 2 nhịp mới bật dậy ăn tiếp.", RewardOrUnlock = "Mở đặc tính gắn bó riêng của heo #01" },
            new CanonicalQuestDefinition { Code = "S03", ChapterOrGroup = "Phụ tuyến", Title = "Đài radio của Tám Vá", TriggerOrCondition = "Thân thiết với Kỹ Sư Tám Vá", CoreActionAndStoryTruth = "Sửa chiếc radio đèn bán dẫn bị nhiễu sóng mỗi đêm lúc 02:00 phát ra 6 tiếng bíp ngắn và 1 khoảng lặng.", RewardOrUnlock = "Giảm 15% chi phí sửa công trình" },
            new CanonicalQuestDefinition { Code = "S04", ChapterOrGroup = "Phụ tuyến", Title = "Vết bồ hóng bên lò sưởi", TriggerOrCondition = "Quan sát lò sưởi Nhà chính", CoreActionAndStoryTruth = "Phát hiện vết cạo mới bên dưới lớp bồ hóng cũ dẫn tới câu đố 4 lớp.", RewardOrUnlock = "Manh mối mở khóa Q16" },
            new CanonicalQuestDefinition { Code = "S05", ChapterOrGroup = "Phụ tuyến", Title = "Chuyến hàng thiếu một bao", TriggerOrCondition = "Thuê Thủ Kho Ông Tồn", CoreActionAndStoryTruth = "Ông Tồn luôn ghi phiếu nhập kho theo bội số của 6 cộng 1 bao lẻ đặt riêng chống ẩm.", RewardOrUnlock = "+8kg cám bảo quản mỗi ngày" },
            new CanonicalQuestDefinition { Code = "S06", ChapterOrGroup = "Phụ tuyến", Title = "Cúc và tập mẫu bị niêm phong", TriggerOrCondition = "Thuê Nhà Nghiên Cứu Gen Cúc", CoreActionAndStoryTruth = "Cúc từng làm việc ở vòng ngoài của Sở Lưu Trữ Ngoại Lệ trước khi bỏ về Bờ Rìa nghiên cứu giống Kim Thọ.", RewardOrUnlock = "Mở đặc tính Giải Mã Sâu" },
            new CanonicalQuestDefinition { Code = "S07", ChapterOrGroup = "Phụ tuyến", Title = "Độ và tấm khiên gãy góc", TriggerOrCondition = "Thuê Vệ Binh Khiên Độ", CoreActionAndStoryTruth = "Gia cố lại tấm khiên chắn cổng từng chịu đòn của Chúa Trộm Rìu Sắt 8 năm trước.", RewardOrUnlock = "+10% giảm sát thương tại cổng chính" },
            new CanonicalQuestDefinition { Code = "S08", ChapterOrGroup = "Phụ tuyến", Title = "Tư Trời nhìn mây đoán UFO", TriggerOrCondition = "Thuê Thợ Săn UFO Tư Trời", CoreActionAndStoryTruth = "Dựng ăng-ten tự chế trên mái kho để bắt sóng từ trường trước khi UFO hạ độ cao.", RewardOrUnlock = "Tăng sát thương lên mục tiêu bay & UFO" },
            new CanonicalQuestDefinition { Code = "S09", ChapterOrGroup = "Phụ tuyến", Title = "Cây hoa sứ ở chiếu nghỉ", TriggerOrCondition = "Nhớ lại căn nhà cũ ở thành phố", CoreActionAndStoryTruth = "Đối chiếu cành hoa sứ 6 bông nở 1 nụ trong chai nước mắm với 7 mốc thời gian trong Sổ Hai Cột.", RewardOrUnlock = "Tăng +10 Tinh thần toàn đàn & hồi Thể trạng" },
            new CanonicalQuestDefinition { Code = "S10", ChapterOrGroup = "Phụ tuyến", Title = "Phiếu thu nghĩa trang bảy nét", TriggerOrCondition = "Kiểm tra ngăn kéo bàn làm việc", CoreActionAndStoryTruth = "Soi chữ ký 7 nét gạch trên phiếu thu năm 2017 trùng khớp với ký hiệu của Thất Viên.", RewardOrUnlock = "Mở hồ sơ ẩn về Thất Viên" },
            new CanonicalQuestDefinition { Code = "S11", ChapterOrGroup = "Phụ tuyến", Title = "Con mèo tên Phiếu", TriggerOrCondition = "Nhận nuôi Mèo Kho 'Phiếu'", CoreActionAndStoryTruth = "Con mèo kho thích nằm ngủ trên xấp phiếu xuất kho cũ và đuổi sạch chuột gặm cám.", RewardOrUnlock = "-80% hao hụt cám do chuột" },
            new CanonicalQuestDefinition { Code = "S12", ChapterOrGroup = "Phụ tuyến", Title = "Bát của ai", TriggerOrCondition = "Sau Q14 tại bếp Bà Sáu", CoreActionAndStoryTruth = "Bà Sáu kể về chiếc bát men xanh sứt miệng luôn được rửa sạch úp sẵn trên chạn chờ người thứ 7 trở về ăn cơm.", RewardOrUnlock = "Mở điều kiện nhận chiếc bát thứ hai ở Q20" }
        };

        // ---------------------------------------------------------------------
        // C. HỒ SƠ TÁC GIẢ (PHẦN 10–12): THÁNH MINH NHẬT GIẢ, SIRPHINA & 7 THẾ LỰC
        // ---------------------------------------------------------------------
        public const int SealTotalYears = 3000;
        public const int SealTotalNodes = 24;
        public const int SealCycleIntervalYears = 125; // 24 * 125 = 3000 năm
        public const int EdgeTownMemorySealYears = 100;

        public static (bool verified, string loreSummary) VerifyAncientSealMathematics()
        {
            bool ok = (SealTotalNodes * SealCycleIntervalYears == SealTotalYears) &&
                      FamilyTimelineTable14.Count == 8 &&
                      PrologueScenesP01ToP08.Count == 8 &&
                      TwentyMainQuestsQ01ToQ20.Count == 20 &&
                      TwelveSideQuestsS01ToS12.Count == 12;

            string summary =
                $"Thánh Minh Nhật Giả: {SealTotalNodes} chốt phong ấn × {SealCycleIntervalYears} năm = {SealTotalYears} năm | " +
                $"Hoàng Hôn Thu Hồn Giả Sirphina (tinh linh đồng cỏ cuối cùng không thuộc tính) giữ 6 linh hồn gia đình Khoa ở bờ hoàng hôn | " +
                $"Phong ấn ký ức Bờ Rìa: {EdgeTownMemorySealYears} năm | " +
                $"Đủ 8 cảnh P01–P08, 20 quest chính Q01–Q20, 12 quest phụ S01–S12.";
            return (ok, summary);
        }

        /// <summary>
        /// Chương 8 — "Lời chào cuối cùng" tại Ngọ Môn Không Bóng trước Nhật Quỹ Tàn Khuyết
        /// và cảnh trở về căn nhà cũ ở thành phố ("Dù tôi từng là gì, hôm nay tôi vẫn phải ăn tối.").
        /// </summary>
        public static (bool completed, string sundialInscription, List<string> sixFamilyFarewells, string cityReturnEpilogue) ExecuteChapterEightFinalGreeting(
            bool placeSevenNotchedNecklaceOnSundial,
            bool returnClockNeedleToSirphina)
        {
            if (!placeSevenNotchedNecklaceOnSundial || !returnClockNeedleToSirphina)
            {
                return (false, string.Empty, new List<string>(), "Cần đặt chiếc vòng đồng 7 khía lên Nhật Quỹ Tàn Khuyết và trả lại kim đồng hồ cho Sirphina.");
            }

            var farewells = new List<string>(6);
            for (int i = 0; i < FamilyTimelineTable14.Count; i++)
            {
                var rec = FamilyTimelineTable14[i];
                if (rec.OrderIndex != 6 && rec.OrderIndex != 8) // 6 người thân đã khuất (Ông, Bà, Anh Minh, Bé Na, Cha, Mẹ)
                {
                    farewells.Add($"{rec.MemberName}: {rec.FarewellActionInChapter8}");
                }
            }

            string inscription = "KHOA (Không phải Thánh Minh Nhật Giả — chọn làm người sống hôm nay)";
            string epilogue =
                "[CHƯƠNG 8 · LỜI CHÀO CUỐI CÙNG & HẬU KẾT THÚC] Tại Ngọ Môn Không Bóng, Khoa viết tên 'KHOA' lên mặt đá Nhật Quỹ Tàn Khuyết và trả lại cây kim đồng hồ cho Hoàng Hôn Thu Hồn Giả Sirphina. " +
                "Sau lời chào tạm biệt trọn vẹn với 6 người thân trong ánh hoàng hôn ấm áp, Khoa trở về căn nhà cũ ở thành phố. " +
                "Trên bậu cửa sổ chiếu nghỉ, nụ hoa sứ thứ 7 đã nở bung trắng muốt. Trong bếp, Khoa lấy chiếc bát nằm ngửa trên giá xuống, múc một bát canh nóng: " +
                "\"Dù tôi từng là gì, hôm nay tôi vẫn phải ăn tối.\"";

            return (true, inscription, farewells, epilogue);
        }

        // ---------------------------------------------------------------------
        // D. BẢNG 1.5-D GDD v6.0: ĐẦY ĐỦ 31 CÔNG TRÌNH (KÍCH THƯỚC, TẦM, GIỚI HẠN/TRẠI)
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<GddBuildingTableSpec> ThirtyOneBuildingsTable15D = new List<GddBuildingTableSpec>
        {
            // Thông tin — Trinh thám đợt quái (4)
            new GddBuildingTableSpec { Number = 1, BlueprintId = "ThapCanh", CatalogId = "pig.building.thap_canh_quan_sat", DisplayName = "Tháp Canh Quan Sát", Category = BuildingCategory.ThongTin_TrinhTham, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 180, RangeMeters = 28f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 4 },
            new GddBuildingTableSpec { Number = 2, BlueprintId = "RadarUyHiep", CatalogId = "pig.building.radar_uy_hiep", DisplayName = "Radar Uy Hiếp", Category = BuildingCategory.ThongTin_TrinhTham, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 520, RangeMeters = 45f, PowerContinuous = 6, PowerPeak = 8, MaxPerFarm = 2 },
            new GddBuildingTableSpec { Number = 3, BlueprintId = "MayCongHuongHuyenBi", CatalogId = "pig.building.may_cong_huong_huyen_bi", DisplayName = "Máy Cộng Hưởng Huyền Bí", Category = BuildingCategory.ThongTin_TrinhTham, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 950, RangeMeters = 50f, PowerContinuous = 8, PowerPeak = 12, MaxPerFarm = 2 },
            new GddBuildingTableSpec { Number = 4, BlueprintId = "ThienNhanVongDai", CatalogId = "pig.building.thien_nhan_vong_dai", DisplayName = "Bạch Vân · Thiên Nhãn Vọng Đài", Category = BuildingCategory.ThongTin_TrinhTham, FootprintWidthMeters = 6, FootprintHeightMeters = 6, BuildCostGold = 3200, RangeMeters = 120f, PowerContinuous = 12, PowerPeak = 20, MaxPerFarm = 1, IsBachVan = true },

            // Thông tin — Hàng ngày (4)
            new GddBuildingTableSpec { Number = 5, BlueprintId = "TramKhiTuong", CatalogId = "pig.building.tram_khi_tuong", DisplayName = "Trạm Khí Tượng", Category = BuildingCategory.ThongTin_HangNgay, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 240, RangeMeters = 0f, PowerContinuous = 2, PowerPeak = 2, MaxPerFarm = 1 },
            new GddBuildingTableSpec { Number = 6, BlueprintId = "TramTiepSongCho", CatalogId = "pig.building.tram_tiep_song_cho", DisplayName = "Trạm Tiếp Sóng Chợ", Category = BuildingCategory.ThongTin_HangNgay, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 320, RangeMeters = 0f, PowerContinuous = 3, PowerPeak = 4, MaxPerFarm = 1 },
            new GddBuildingTableSpec { Number = 7, BlueprintId = "BanTinhBao", CatalogId = "pig.building.ban_tinh_bao", DisplayName = "Bàn Tình Báo", Category = BuildingCategory.ThongTin_HangNgay, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 450, RangeMeters = 0f, PowerContinuous = 2, PowerPeak = 2, MaxPerFarm = 1 },
            new GddBuildingTableSpec { Number = 8, BlueprintId = "DaiDoDiTuong", CatalogId = "pig.building.dai_do_di_tuong", DisplayName = "Đài Dò Dị Tượng", Category = BuildingCategory.ThongTin_HangNgay, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 680, RangeMeters = 60f, PowerContinuous = 6, PowerPeak = 10, MaxPerFarm = 1 },

            // Hỗ trợ (6)
            new GddBuildingTableSpec { Number = 9, BlueprintId = "TramHoiSuc", CatalogId = "pig.building.tram_hoi_suc", DisplayName = "Trạm Hồi Sức", Category = BuildingCategory.HoTro, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 350, RangeMeters = 8f, PowerContinuous = 4, PowerPeak = 6, MaxPerFarm = 3 },
            new GddBuildingTableSpec { Number = 10, BlueprintId = "DenPhaCongNghiep", CatalogId = "pig.building.den_pha_cong_nghiep", DisplayName = "Đèn Pha Công Nghiệp", Category = BuildingCategory.HoTro, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 160, RangeMeters = 18f, PowerContinuous = 4, PowerPeak = 5, MaxPerFarm = 8 },
            new GddBuildingTableSpec { Number = 11, BlueprintId = "MayPhatDien", CatalogId = "pig.building.may_phat_dien", DisplayName = "Máy Phát Điện", Category = BuildingCategory.HoTro, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 400, RangeMeters = 12f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 3 },
            new GddBuildingTableSpec { Number = 12, BlueprintId = "BomLamMat", CatalogId = "pig.building.bom_lam_mat", DisplayName = "Bơm Làm Mát", Category = BuildingCategory.HoTro, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 280, RangeMeters = 25f, PowerContinuous = 5, PowerPeak = 6, MaxPerFarm = 4 },
            new GddBuildingTableSpec { Number = 13, BlueprintId = "CotThuLoi", CatalogId = "pig.building.cot_thu_loi", DisplayName = "Cột Thu Lôi", Category = BuildingCategory.HoTro, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 260, RangeMeters = 22f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 4 },
            new GddBuildingTableSpec { Number = 14, BlueprintId = "VanLinhHoiNguyenTran", CatalogId = "pig.building.van_linh_hoi_nguyen_tran", DisplayName = "Bạch Vân · Vạn Linh Hồi Nguyên Trận", Category = BuildingCategory.HoTro, FootprintWidthMeters = 6, FootprintHeightMeters = 6, BuildCostGold = 3500, RangeMeters = 35f, PowerContinuous = 15, PowerPeak = 25, MaxPerFarm = 1, IsBachVan = true },

            // Tấn công (5)
            new GddBuildingTableSpec { Number = 15, BlueprintId = "NoXuyenVan", CatalogId = "pig.building.no_xuyen_van", DisplayName = "Nỏ Xuyên Vân", Category = BuildingCategory.TanCong, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 320, RangeMeters = 18f, PowerContinuous = 4, PowerPeak = 6, MaxPerFarm = 6 },
            new GddBuildingTableSpec { Number = 16, BlueprintId = "ThapHoQuang", CatalogId = "pig.building.thap_ho_quang", DisplayName = "Tháp Hồ Quang", Category = BuildingCategory.TanCong, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 580, RangeMeters = 12f, PowerContinuous = 10, PowerPeak = 18, MaxPerFarm = 4 },
            new GddBuildingTableSpec { Number = 17, BlueprintId = "ThapPhongKhongSkyhook", CatalogId = "pig.building.thap_phong_khong_skyhook", DisplayName = "Tháp Phòng Không Skyhook", Category = BuildingCategory.TanCong, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 640, RangeMeters = 26f, PowerContinuous = 12, PowerPeak = 22, MaxPerFarm = 3 },
            new GddBuildingTableSpec { Number = 18, BlueprintId = "DaiThienLoi", CatalogId = "pig.building.dai_thien_loi", DisplayName = "Đài Thiên Lôi", Category = BuildingCategory.TanCong, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 920, RangeMeters = 22f, PowerContinuous = 16, PowerPeak = 30, MaxPerFarm = 2 },
            new GddBuildingTableSpec { Number = 19, BlueprintId = "CuuTieuLoiPhao", CatalogId = "pig.building.cuu_tieu_loi_phao", DisplayName = "Bạch Vân · Cửu Tiêu Lôi Pháo", Category = BuildingCategory.TanCong, FootprintWidthMeters = 6, FootprintHeightMeters = 6, BuildCostGold = 3800, RangeMeters = 45f, PowerContinuous = 20, PowerPeak = 45, MaxPerFarm = 1, IsBachVan = true },

            // Phòng thủ (4)
            new GddBuildingTableSpec { Number = 20, BlueprintId = "RaoGo", CatalogId = "pig.building.hang_rao", DisplayName = "Hàng Rào (Gỗ / Thép)", Category = BuildingCategory.PhongThu, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 25, RangeMeters = 0f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 80 },
            new GddBuildingTableSpec { Number = 21, BlueprintId = "CongGiaCo", CatalogId = "pig.building.cong_gia_co", DisplayName = "Cổng Gia Cố", Category = BuildingCategory.PhongThu, FootprintWidthMeters = 4, FootprintHeightMeters = 2, BuildCostGold = 180, RangeMeters = 0f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 6 },
            new GddBuildingTableSpec { Number = 22, BlueprintId = "MayChieuKhien", CatalogId = "pig.building.may_chieu_khien", DisplayName = "Máy Chiếu Khiên", Category = BuildingCategory.PhongThu, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 620, RangeMeters = 10f, PowerContinuous = 10, PowerPeak = 16, MaxPerFarm = 3 },
            new GddBuildingTableSpec { Number = 23, BlueprintId = "UBaoCat", CatalogId = "pig.building.u_bao_cat", DisplayName = "Ụ Bao Cát", Category = BuildingCategory.PhongThu, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 45, RangeMeters = 0f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 12 },

            // Bẫy (5)
            new GddBuildingTableSpec { Number = 24, BlueprintId = "BanChong", CatalogId = "pig.building.ban_chong", DisplayName = "Bàn Chông", Category = BuildingCategory.Bay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 40, RangeMeters = 2f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 20 },
            new GddBuildingTableSpec { Number = 25, BlueprintId = "TamSocDien", CatalogId = "pig.building.tam_soc_dien", DisplayName = "Tấm Sốc Điện", Category = BuildingCategory.Bay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 110, RangeMeters = 2.5f, PowerContinuous = 4, PowerPeak = 8, MaxPerFarm = 12 },
            new GddBuildingTableSpec { Number = 26, BlueprintId = "HoKeo", CatalogId = "pig.building.ho_keo", DisplayName = "Hố Keo", Category = BuildingCategory.Bay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 65, RangeMeters = 3f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 12 },
            new GddBuildingTableSpec { Number = 27, BlueprintId = "LuoiEmp", CatalogId = "pig.building.luoi_emp", DisplayName = "Lưới EMP", Category = BuildingCategory.Bay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 190, RangeMeters = 4f, PowerContinuous = 6, PowerPeak = 12, MaxPerFarm = 8 },
            new GddBuildingTableSpec { Number = 28, BlueprintId = "MinNo", CatalogId = "pig.building.min_no", DisplayName = "Mìn Nổ", Category = BuildingCategory.Bay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 95, RangeMeters = 3.5f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 16 },

            // Triển khai (3)
            new GddBuildingTableSpec { Number = 29, BlueprintId = "ChotCanhGac", CatalogId = "pig.building.chot_canh_gac", DisplayName = "Chốt Canh Gác", Category = BuildingCategory.TrienKhai, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 150, RangeMeters = 10f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 6 },
            new GddBuildingTableSpec { Number = 30, BlueprintId = "CoTapKet", CatalogId = "pig.building.co_tap_ket", DisplayName = "Cờ Tập Kết", Category = BuildingCategory.TrienKhai, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 120, RangeMeters = 8f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 3 },
            new GddBuildingTableSpec { Number = 31, BlueprintId = "DenHieuDanhChan", CatalogId = "pig.building.den_hieu_danh_chan", DisplayName = "Đèn Hiệu Đánh Chặn", Category = BuildingCategory.TrienKhai, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 260, RangeMeters = 14f, PowerContinuous = 4, PowerPeak = 6, MaxPerFarm = 3 }
        };

        // ---------------------------------------------------------------------
        // E. BẢNG 5.4 GDD v6.0: THÔNG SỐ BỐN KÊNH GIÁM ĐỊNH GEN CHUẨN
        //    (Giữ đúng số trong bảng GDD 5.4 theo AGENTS.md)
        // ---------------------------------------------------------------------
        public static (float successRate, int goldCost, int reagentCount, float staminaCost, int processingHours) GetCanonicalAppraisalChannelSpecGdd54(
            AppraisalChannel channel,
            GeneRarity rarity)
        {
            return channel switch
            {
                // Chợ ngày — Quầy Sơ Định: Hiếm 70% (300G), Quý 50% (900G), 5 STA, tức thì; không mở được Huyền Thoại / Dị Biến
                AppraisalChannel.ChoNgay => rarity switch
                {
                    GeneRarity.Hiem => (0.70f, 300, 0, 5f, 0),
                    GeneRarity.Quy => (0.50f, 900, 0, 5f, 0),
                    _ => (0.00f, 0, 0, 5f, 0)
                },

                // Phòng Nghiên Cứu — Bàn Giải Mã: 95/85/65/50%, 700G+1 / 2000G+2 / 6500G+4 / 9000G+6, 25/30/40/45 STA, 18/24/36/48h
                AppraisalChannel.PhongNghienCuu => rarity switch
                {
                    GeneRarity.Hiem => (0.95f, 700, 1, 25f, 18),
                    GeneRarity.Quy => (0.85f, 2000, 2, 30f, 24),
                    GeneRarity.HuyenThoai => (0.65f, 6500, 4, 40f, 36),
                    GeneRarity.DiBien => (0.50f, 9000, 6, 45f, 48),
                    _ => (1.00f, 300, 1, 15f, 6)
                },

                // Chợ đêm — Quầy Giám Định Cấp Cao (12% số đêm): 100%, 450 / 1300 / 3500 / 5000G, 8 STA, tức thì
                AppraisalChannel.ChoDemGiamDinhCapCao => rarity switch
                {
                    GeneRarity.Hiem => (1.00f, 450, 0, 8f, 0),
                    GeneRarity.Quy => (1.00f, 1300, 0, 8f, 0),
                    GeneRarity.HuyenThoai => (1.00f, 3500, 0, 8f, 0),
                    GeneRarity.DiBien => (1.00f, 5000, 0, 8f, 0),
                    _ => (1.00f, 250, 0, 8f, 0)
                },

                // Bí Nhân — Sự kiện Giám Định Vô Danh: 100%, 0 Vàng (đổi vật phẩm/mẫu hiếm), 10 STA, tức thì
                AppraisalChannel.BiNhanHoSoGiongLoai => (1.00f, 0, 0, 10f, 0),
                _ => (0.00f, 0, 0, 0f, 0)
            };
        }

        // ---------------------------------------------------------------------
        // F. MỤC 10.2 & 10.8 GDD v6.0: CỔNG GIỮA GAME (>= 8 ĐỢT LỚN + 1/5 ĐIỀU KIỆN PHỤ)
        //    VÀ 6 KHÓA ĐẶC TÍNH CHUYÊN BIỆT CHO KHOA ("TRẠI CHỦ LÃO LUYỆN") & AN ("NGƯỜI ĐỌC DÒNG")
        // ---------------------------------------------------------------------
        public static (bool unlocked, int subConditionsMetCount, List<string> metSubConditions) EvaluateCanonicalMidGameGateGdd102(
            int majorWavesSurvived,
            bool herdSoKhaiThreeConsecutiveDays,
            bool identifiedLegendaryOrDeviantGene,
            int constructionHallLevel,
            int cycleBossesDefeated,
            long totalPigSalesGoldRevenue)
        {
            var met = new List<string>();
            if (herdSoKhaiThreeConsecutiveDays) met.Add("1. Đạt Bầy Đàn Sơ Khai >= 3 ngày liên tục");
            if (identifiedLegendaryOrDeviantGene) met.Add("2. Định danh thành công >= 1 dòng gen Huyền Thoại hoặc Dị Biến");
            if (constructionHallLevel >= 3) met.Add("3. Nâng cấp Phòng Xây Dựng lên cấp 3");
            if (cycleBossesDefeated >= 1) met.Add("4. Hạ gục >= 1 Boss Chu kỳ");
            if (totalPigSalesGoldRevenue >= 25000L) met.Add("5. Tổng doanh thu bán heo >= 25.000 Vàng");

            bool gateOpen = majorWavesSurvived >= 8 && met.Count >= 1;
            return (gateOpen, met.Count, met);
        }

        public static readonly IReadOnlyList<SpecialtyLockEffectSpec> KhoaAndAnTwelveSpecialtyLocksGdd108 = new List<SpecialtyLockEffectSpec>
        {
            // KHOA — "Trại Chủ Lão Luyện" (6 khóa)
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 1, UnlockConditionSummary = "Qua Cổng Giữa Game + Giấy Lam +1", EffectSummary = "+10% tốc độ sửa chữa công trình và hàng rào" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 2, UnlockConditionSummary = "Mở Khóa 1 + Giấy Lam +2", EffectSummary = "-15% hao thể lực khi vác nặng và kéo xác heo" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 3, UnlockConditionSummary = "Mở Khóa 2 + Giấy Vàng +1", EffectSummary = "Tiếng Huýt Tập Hợp Đàn mở rộng bán kính thêm +8m" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 4, UnlockConditionSummary = "Mở Khóa 3 + Giấy Vàng +2", EffectSummary = "+12% sát thương kỹ năng Trọng Kích (B) và Khống Chế (C) trong phạm vi trại" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 5, UnlockConditionSummary = "Mở Khóa 4 + Giấy Tím +2", EffectSummary = "Các công trình phòng thủ trong bán kính 15m quanh Khoa được +15% tốc bắn" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 6, UnlockConditionSummary = "Mở Khóa 5 + Giấy Đỏ +3", EffectSummary = "Mở khóa hoàn chỉnh [Trại Chủ Lão Luyện]: Khi Khoa trực tiếp chiến đấu giữ trại, toàn đàn heo giảm 50% hoảng loạn và lính đánh thuê nhận -20% sát thương" },

            // AN — "Người Đọc Dòng" (6 khóa)
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 1, UnlockConditionSummary = "Qua Cổng Giữa Game + Giấy Lam +1", EffectSummary = "+10% hiệu quả thuốc thú y và thảo dược" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 2, UnlockConditionSummary = "Mở Khóa 1 + Giấy Lam +2", EffectSummary = "+15% tốc độ tăng Gắn Bó của đàn heo khi An chăm sóc" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 3, UnlockConditionSummary = "Mở Khóa 2 + Giấy Vàng +1", EffectSummary = "+10 điểm % tỉ lệ thành công khi giải mã gen tại Phòng Nghiên Cứu" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 4, UnlockConditionSummary = "Mở Khóa 3 + Giấy Vàng +2", EffectSummary = "+12% sát thương kỹ năng Tốc Bộ (A) và Sinh Tồn (D)" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 5, UnlockConditionSummary = "Mở Khóa 4 + Giấy Tím +2", EffectSummary = "Nhận diện sớm dấu hiệu bệnh tiềm ẩn và dự báo biến động giá chợ trước 6 giờ" },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 6, UnlockConditionSummary = "Mở Khóa 5 + Giấy Đỏ +3", EffectSummary = "Mở khóa hoàn chỉnh [Người Đọc Dòng]: Heo do An định danh tăng +1 bậc khuynh hướng Phẩm chất thịt khi trưởng thành và giảm 30% thời gian khóa giám định" }
        };
    }
}
