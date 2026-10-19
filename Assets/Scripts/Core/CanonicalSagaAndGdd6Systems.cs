using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum AnNotebookSticker
    {
        None = 0,
        Moon_OnlyAnNoticed = 1,      // ☾ Mặt trăng: chuyện chỉ mình An để ý (tương đương cột "Thấy" của Khoa)
        Star_VerifiableByAll = 2     // ★ Ngôi sao: chuyện ai nhìn vào cũng thấy giống An (tương đương cột "Có thật" của Khoa)
    }

    [Serializable]
    public sealed class CanonicalFamilyTimelineRecord
    {
        public int OrderIndex { get; set; }
        public PlayerId ProtagonistPerspective { get; set; } = PlayerId.Khoa;
        public string MemberName { get; set; } = string.Empty;
        public string BirthOrAgeInfo { get; set; } = string.Empty;
        public string EventDateAndTime { get; set; } = string.Empty;
        public string OfficialCauseOfDeath { get; set; } = string.Empty;
        public string AbnormalDetailTwoMinutesAndAngle { get; set; } = string.Empty;
        public string RelicKeptInTinBox { get; set; } = string.Empty;
        public string PackageDeliveredToEdgeTown { get; set; } = string.Empty;
        public string FarewellActionInChapter8 { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class DualProtagonistComparisonRow
    {
        public int RowNumber { get; set; }
        public string Aspect { get; set; } = string.Empty;
        public string KhoaManifestation { get; set; } = string.Empty;
        public string AnManifestation { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class AnLetterNotebookEntry
    {
        public string EntryId { get; set; } = string.Empty;
        public string QuestOrSceneRef { get; set; } = string.Empty;
        public string RecipientSalutation { get; set; } = string.Empty; // "Ông ơi,", "Bà ơi,", "Khoa ơi,", "Mẹ ơi,", "Nhà mình ơi,", hoặc rỗng nếu mất ý thức
        public AnNotebookSticker Sticker { get; set; } = AnNotebookSticker.None;
        public string LetterBody { get; set; } = string.Empty;
        public string PencilMarginHypothesis { get; set; } = string.Empty;
        public bool EndsWithSmiley { get; set; }
        public bool IsUnconsciousBlackoutEntry { get; set; }

        public bool ValidateBlackoutInvariant()
        {
            if (IsUnconsciousBlackoutEntry)
            {
                return string.IsNullOrEmpty(RecipientSalutation) &&
                       Sticker == AnNotebookSticker.None &&
                       !EndsWithSmiley &&
                       !LetterBody.Contains(":)");
            }

            return !string.IsNullOrEmpty(RecipientSalutation) &&
                   RecipientSalutation.Contains("ơi") &&
                   Sticker != AnNotebookSticker.None &&
                   EndsWithSmiley;
        }
    }

    [Serializable]
    public sealed class ProtagonistMaskSlipRecord
    {
        public string StageOrChapter { get; set; } = string.Empty;
        public string KhoaUnconsciousSlip { get; set; } = string.Empty;
        public string AnUnconsciousSlip { get; set; } = string.Empty;
        public bool TriggeredBothPalmsUpGesture { get; set; }
    }

    [Serializable]
    public sealed class CanonicalQuestTableSpec
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AnTitleVariant { get; set; } = string.Empty;
        public string ChapterOrType { get; set; } = string.Empty;
        public string TriggerCondition { get; set; } = string.Empty;
        public string SurfaceTask { get; set; } = string.Empty;
        public string HiddenStoryTruth { get; set; } = string.Empty;
        public string KhoaPerspectiveNote { get; set; } = string.Empty;
        public string AnPerspectiveNote { get; set; } = string.Empty;
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
        public int LockNumber { get; set; }
        public string UnlockConditionSummary { get; set; } = string.Empty;
        public string EffectSummary { get; set; } = string.Empty;
        public bool IsStrictlyNonCombatDailySupport { get; set; } = true;
    }

    [Serializable]
    public sealed class OrdoSolisAureiMemberSpec
    {
        public int OrderNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AncientRole { get; set; } = string.Empty;
        public string FateAfter3000Years { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class BoRiaResidentFarewellSpec
    {
        public int OrderIndex { get; set; }
        public string ResidentId { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
        public string FinalConversationSummary { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class SeasonEightCanonicalFinaleResult
    {
        public bool Completed { get; set; }
        public PlayerId Protagonist { get; set; }
        public bool VoluntaryBothPalmsUp { get; set; }
        public bool SirphinaThreePhrasesRuleVerified { get; set; }
        public bool ModernAndAncientSoulFused { get; set; }
        public bool TwoWorldsPermanentlySeparated { get; set; }
        public bool HuyenYWithdrawnFromMainWorld { get; set; }
        public IReadOnlyList<string> SixLinesOfMacKhai { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> ElevenBoRiaResidentFarewells { get; set; } = Array.Empty<string>();
        public string CityReturnEpilogue { get; set; } = string.Empty;
        public string ThreeYearsLaterAfterCredit { get; set; } = string.Empty;
    }

    /// <summary>
    /// Hệ thống đối chiếu chính tắc 100% với bộ tài liệu gốc ghim bởi người dùng:
    /// 1. "Pig Tycoon — Sáu Với Một (Mùa 1)" (Cập nhật mới: Góc nhìn song song Khoa [Phần 1 & 5] vs. An [Phần 1B, Mục 2.7 & Phần 5B],
    ///    hoán vị vai em út Bé An / Bé Khoa ngày 13/05/2019, Sổ Hai Cột vs Sổ Bìa Hồng dán sticker con thỏ ★/☾/:),
    ///    bóng phản chiếu ngược chiều, Mặt nạ & cử chỉ Hai lòng bàn tay ngửa lên, và Phần 11–12 Đại Giới Lưu / Minh Ước /
    ///    Khắc Ngữ / Đại Cổ Thụ / Huyền Y / Mặc Khải 6 câu / Kim Ô Thánh Hội 4 thành viên / Sirphina 3 câu <= 3 từ / Lời chào cuối cùng 11 cư dân Bờ Rìa / "Ba năm sau").
    /// 2. "GDD v6.0 Bản Hợp Nhất Chính Thức" (Bảng 1.5-D 31 công trình, Bảng 5.4 4 kênh giám định gen,
    ///    Mục 10.2 Cổng giữa game >= 8 đợt lớn + 1/5 điều kiện phụ, Mục 10.4 Trần 50% trước khi lộ diện,
    ///    Mục 10.5 Tỷ lệ mở khóa theo bậc giấy, Mục 10.8 6 khóa & 3 hiệu ứng phi chiến đấu của Khoa và An).
    /// </summary>
    public static class CanonicalSagaAndGdd6Systems
    {
        // =====================================================================
        // A. BẢNG 1.4 & 1B.4: DÒNG THỜI GIAN GIA ĐÌNH THEO GÓC NHÌN KHOA VS. AN
        //    - Ai được chọn làm nhân vật chính sẽ là con thứ hai (sinh 07/09/2002, 24 tuổi).
        //    - Người còn lại là em út (nhỏ hơn 6 tuổi, mất ngày 13/05/2019 khi tường trường học đổ):
        //      + Tuyến Khoa: em út là Em gái Bé An.
        //      + Tuyến An (Phần 1B & 5B): em út là Em trai Bé Khoa.
        // =====================================================================
        public static readonly IReadOnlyList<CanonicalFamilyTimelineRecord> FamilyTimelineTable14 = new List<CanonicalFamilyTimelineRecord>
        {
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 1,
                ProtagonistPerspective = PlayerId.Khoa,
                MemberName = "Ông nội Lâm",
                BirthOrAgeInfo = "71 tuổi, thợ mộc nghỉ hưu",
                EventDateAndTime = "17/07/2018 (Thứ Ba, 11:16 -> 11:18)",
                OfficialCauseOfDeath = "Ngã cầu thang gỗ từ bậc thứ 7 khi mang bát canh xuống bếp",
                AbnormalDetailTwoMinutesAndAngle = "Đồng hồ quả lắc phòng khách chết ở 11:18 (lệch 2 phút so với giờ ngã 11:16); cái bát men xanh mẻ miệng không vỡ mà nằm úp ngay ngắn ở chiếu nghỉ tầng một.",
                RelicKeptInTinBox = "Cái đục gỗ cán thị có khắc chữ 'L' ở chuôi",
                PackageDeliveredToEdgeTown = "Kiện #01 (14/03/2018): Khăn len xám và bộ đồ nghề mộc gửi về 'Trại chăn nuôi số 7 — Bờ Rìa'",
                FarewellActionInChapter8 = "Ông Tồn gõ tẩu thuốc gỗ 2 nhịp lên thành cầu đá, mỉm cười chào tạm biệt như ông nội Lâm từng gõ xuống mặt bàn."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 2,
                ProtagonistPerspective = PlayerId.Khoa,
                MemberName = "Bà nội Sương",
                BirthOrAgeInfo = "68 tuổi, trồng cây sứ trắng ngoài ban công",
                EventDateAndTime = "04/09/2018 (Thứ Ba, 05:40 -> 05:42)",
                OfficialCauseOfDeath = "Ngừng thở trong lúc ngủ sớm trước bình minh",
                AbnormalDetailTwoMinutesAndAngle = "7 chậu sứ trắng ngoài ban công rụng sạch lá chỉ trong 2 phút (05:40–05:42) dù trời đứng gió; chậu thứ 7 nứt đôi theo đường thẳng tắp.",
                RelicKeptInTinBox = "Đôi kính lão gọng đồng bị gãy một bên càng",
                PackageDeliveredToEdgeTown = "Kiện #02 (02/06/2018): Hộp hạt giống hoa sứ trắng và cuốn lịch giấy năm 2018",
                FarewellActionInChapter8 = "Bà Sáu đặt bát chè nóng cuối cùng lên quầy gỗ trước khi đóng cửa tiệm tạp hóa ở Bờ Rìa."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 3,
                ProtagonistPerspective = PlayerId.Khoa,
                MemberName = "Anh cả Minh",
                BirthOrAgeInfo = "24 tuổi (sinh năm 1995), kỹ sư cầu đường",
                EventDateAndTime = "19/02/2019 (Thứ Ba, 21:07 -> 21:09)",
                OfficialCauseOfDeath = "Tai nạn xe máy trên đèo tỉnh lộ 14 khi trời không mưa",
                AbnormalDetailTwoMinutesAndAngle = "Vệt phanh trên mặt đường nhựa không trượt thẳng mà bẻ gập một góc vuông 90 độ hướng vào vách núi; đồng hồ đeo tay của Minh dừng ở 21:09 (sau thời điểm va chạm 2 phút).",
                RelicKeptInTinBox = "Chiếc còi nhựa màu đỏ thổi không kêu (mất viên bi gỗ bên trong)",
                PackageDeliveredToEdgeTown = "Kiện #03 (19/08/2018): Cuộn bản đồ địa hình tỉnh lộ 14 và chiếc áo mưa xanh",
                FarewellActionInChapter8 = "Anh Ca (Trần Minh) gật đầu chào ở cổng Hội Quán trước khi bóng trại thu hẹp."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 4,
                ProtagonistPerspective = PlayerId.Khoa,
                MemberName = "Em gái Bé An (Tuyến Khoa — mất 13/05/2019)",
                BirthOrAgeInfo = "11 tuổi, em út trong tuyến của Khoa (thích vẽ heo con bằng bút sáp màu)",
                EventDateAndTime = "13/05/2019 (Thứ Hai, 16:12 -> 16:14)",
                OfficialCauseOfDeath = "Sập mảng tường gạch cũ sau phòng nhạc trường tiểu học",
                AbnormalDetailTwoMinutesAndAngle = "Bụi gạch bay ngược từ dưới đất lên nóc mái tôn trong suốt 2 phút (16:12–16:14); trong vở vẽ của Bé An có bức tranh 7 con heo đứng quanh 1 vòng tròn trống có ghi chữ 'Chỗ của anh Hai'.",
                RelicKeptInTinBox = "Viên bi ve màu xanh biển có bọt khí hình lưỡi liềm",
                PackageDeliveredToEdgeTown = "Kiện #04 (05/11/2018): Hộp bút sáp 12 màu (thiếu màu số 7) và tập giấy vẽ",
                FarewellActionInChapter8 = "Con heo chân trắng đứng ở cổng trại dụi mũi vào ống quần Khoa lần cuối rồi nằm xuống bãi cỏ."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 5,
                ProtagonistPerspective = PlayerId.Khoa,
                MemberName = "Cha Hậu",
                BirthOrAgeInfo = "49 tuổi, thợ sửa đồng hồ và khóa cơ",
                EventDateAndTime = "24/08/2019 (Thứ Bảy, 14:22 -> 14:24)",
                OfficialCauseOfDeath = "Đột quỵ tại bàn làm việc khi đang lau bộ bánh răng cổ",
                AbnormalDetailTwoMinutesAndAngle = "Tất cả 14 chiếc đồng hồ treo tường trong tiệm cùng điểm chuông 1 lần vào lúc 14:24 (dù không chiếc nào đang chỉ đúng giờ) và kim giây đều quay lùi đúng 2 phút.",
                RelicKeptInTinBox = "Chìa khóa đồng số 7 không mở được ổ khóa nào trong nhà",
                PackageDeliveredToEdgeTown = "Kiện #05 (12/01/2019): Hộp gỗ đựng bộ bánh răng đồng hồ và lọ dầu máy",
                FarewellActionInChapter8 = "Tám Vá trao lại bộ kìm sửa khóa trên bàn gỗ rồi khép cửa xưởng rèn."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 6,
                ProtagonistPerspective = PlayerId.Khoa,
                MemberName = "Đêm Khoa tự đi bộ tới Bờ Rìa (Giữ chỗ)",
                BirthOrAgeInfo = "Khoa (17 tuổi — đêm mộng du mất ký ức)",
                EventDateAndTime = "05/10/2019 (Thứ Bảy, 02:00 sáng)",
                OfficialCauseOfDeath = "Không tử vong — Khoa biến mất khỏi phòng ngủ từ 00:00 đến 04:00 sáng",
                AbnormalDetailTwoMinutesAndAngle = "Đôi giày vải của Khoa dính đất sét đỏ Bờ Rìa và lá thông trắng dù cách thành phố hơn 180km; trang cuối Sổ Hai Cột xuất hiện dòng chữ chính tay Khoa viết lúc 02:00: '02:00. Đã giữ chỗ.'",
                RelicKeptInTinBox = "Mảnh da cổ khắc số 24 bằng chữ hình nêm Babylon (mặt sau Khoa tự ký ngày 21/09/2026)",
                PackageDeliveredToEdgeTown = "Không gửi qua bưu điện — Khoa tự tay mang mảnh da cổ đặt vào khe lò sưởi rồi đóng hộp sắt dưới bậc cầu thang thứ 7",
                FarewellActionInChapter8 = "Khoa tự nguyện đưa hai bàn tay ngửa lên để Sirphina dung hợp hai nửa linh hồn và giữ sợi liên kết ở lại thế giới chính."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 7,
                ProtagonistPerspective = PlayerId.Khoa,
                MemberName = "Mẹ Thủy",
                BirthOrAgeInfo = "46 tuổi, kế toán trường học",
                EventDateAndTime = "08/11/2019 (Thứ Sáu, 18:04 -> 18:06)",
                OfficialCauseOfDeath = "Ngộ độc khí than trong bếp đóng kín cửa",
                AbnormalDetailTwoMinutesAndAngle = "Cửa sổ chớp thực chất mở hé 2 phân và ngọn lửa bếp đã tắt từ 18:04, nhưng khói trắng đứng yên không tan trong 2 phút đến 18:06; trên bàn ăn mẹ xếp sẵn 7 cái bát dù trong nhà chỉ còn 2 mẹ con.",
                RelicKeptInTinBox = "Sợi dây chuyền bạc có mặt tròn khắc 7 vạch nhỏ ở viền sau",
                PackageDeliveredToEdgeTown = "Kiện #06 (07/04/2019): Tập sổ tay bìa vải và chiếc bát men xanh mẻ miệng thứ hai",
                FarewellActionInChapter8 = "Mận xếp lại túi thảo dược trên hiên trại hồi sức, dặn Khoa về thành phố nhớ ăn tối đúng giờ."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 8,
                ProtagonistPerspective = PlayerId.Khoa,
                MemberName = "Khoa (Nhân vật chính — Kiện thứ 7 / 'Sáu với một')",
                BirthOrAgeInfo = "Tròn 24 tuổi (sinh ngày 07/09/2002)",
                EventDateAndTime = "24/09/2026 (Thứ Năm — ngày đặt chân tới Bờ Rìa)",
                OfficialCauseOfDeath = "Đang sống — Người cuối cùng trong nhà mở hộp sắt dưới bậc cầu thang thứ 7",
                AbnormalDetailTwoMinutesAndAngle = "Chuyến xe khách cuối cùng dừng trước cổng thị trấn Bờ Rìa lúc hoàng hôn; đồng hồ tại trạm gác chạy chậm đúng 2 phút và bóng của Khoa trên mặt đất đổ lệch sang hướng Tây Bắc.",
                RelicKeptInTinBox = "Toàn bộ hộp sắt dưới bậc cầu thang thứ 7 và tờ giấy nhận 'Kiện 6+1'",
                PackageDeliveredToEdgeTown = "Kiện #07 (24/09/2026): Chính là Khoa bước xuống xe buýt tại Bờ Rìa",
                FarewellActionInChapter8 = "Khoa trở về thành phố, lấy chiếc bát nằm ngửa trên giá xuống múc một bát canh nóng: \"Dù tôi từng là gì, hôm nay tôi vẫn phải ăn tối.\""
            }
        };

        public static readonly IReadOnlyList<CanonicalFamilyTimelineRecord> FamilyTimelineTable1B_AnRoute = new List<CanonicalFamilyTimelineRecord>
        {
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 1,
                ProtagonistPerspective = PlayerId.An,
                MemberName = "Ông nội Lâm",
                BirthOrAgeInfo = "71 tuổi, thợ mộc nghỉ hưu",
                EventDateAndTime = "17/07/2018 (Thứ Ba, 11:16 -> 11:18)",
                OfficialCauseOfDeath = "Ngã cầu thang gỗ từ bậc thứ 7",
                AbnormalDetailTwoMinutesAndAngle = "Ông nội gửi gói đồ ở bưu điện ngày 14/03/2018 trước khi mất 4 tháng; đồng hồ chết ở 11:18 (lệch 2 phút).",
                RelicKeptInTinBox = "Cái đục gỗ cán thị khắc chữ 'L'",
                PackageDeliveredToEdgeTown = "Kiện #01 (14/03/2018): Khăn len xám và bộ đồ nghề mộc gửi về Bờ Rìa",
                FarewellActionInChapter8 = "Ông Tồn gõ tẩu thuốc 2 nhịp chào tạm biệt An: 'Cháu giống ông nội cháu ở chỗ biết giữ lời.'"
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 2,
                ProtagonistPerspective = PlayerId.An,
                MemberName = "Bà nội Sương",
                BirthOrAgeInfo = "68 tuổi, trồng cây sứ trắng ngoài ban công",
                EventDateAndTime = "04/09/2018 (Thứ Ba, 05:40 -> 05:42)",
                OfficialCauseOfDeath = "Ngừng thở trong lúc ngủ sớm trước bình minh",
                AbnormalDetailTwoMinutesAndAngle = "Bà nội gửi kiện đồ ngày 02/06/2018; 7 chậu sứ trắng rụng sạch lá trong đúng 2 phút.",
                RelicKeptInTinBox = "Đôi kính lão gọng đồng gãy một bên càng",
                PackageDeliveredToEdgeTown = "Kiện #02 (02/06/2018): Hộp hạt giống hoa sứ trắng và lịch giấy 2018",
                FarewellActionInChapter8 = "Bà Sáu nắm tay An ở hiên tiệm tạp hóa trước khi ánh hoàng hôn khép lại."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 3,
                ProtagonistPerspective = PlayerId.An,
                MemberName = "Anh cả Minh",
                BirthOrAgeInfo = "24 tuổi (sinh năm 1995), kỹ sư cầu đường",
                EventDateAndTime = "19/02/2019 (Thứ Ba, 21:07 -> 21:09)",
                OfficialCauseOfDeath = "Tai nạn xe máy trên đèo tỉnh lộ 14 khi trời không mưa",
                AbnormalDetailTwoMinutesAndAngle = "Đêm giao thừa Tết Kỷ Hợi (04/02/2019) pháo hoa chững lại trên trời 1 nhịp; anh Minh hỏi An 'Nếu anh đi công tác xa thật lâu thì An có sợ ở nhà một mình không?'.",
                RelicKeptInTinBox = "Chiếc còi nhựa màu đỏ thổi không kêu",
                PackageDeliveredToEdgeTown = "Kiện #03 (19/08/2018): Cuộn bản đồ địa hình tỉnh lộ 14 và áo mưa xanh",
                FarewellActionInChapter8 = "Anh Ca mỉm cười chào An bằng đúng giọng trầm của anh Minh."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 4,
                ProtagonistPerspective = PlayerId.An,
                MemberName = "Em trai Bé Khoa (Tuyến An — mất 13/05/2019)",
                BirthOrAgeInfo = "10 tuổi (sinh năm 2008, học lớp Năm), em út trong tuyến của An",
                EventDateAndTime = "13/05/2019 (Thứ Hai, 16:12 -> 16:14)",
                OfficialCauseOfDeath = "Tường trường học đổ sau cơn mưa chiều thứ Hai",
                AbnormalDetailTwoMinutesAndAngle = "Ngày 07/09/2018 Bé Khoa đập heo đất 43.000đ mua tặng chị An cuốn sổ bìa hồng dán sticker con thỏ; chiều 12/05/2019 Khoa hỏi 'Chị An ơi, nếu em đi chỗ khác trước chị thì chị có giận em không?'. Khi tường đổ, người gác cổng thấy Khoa đứng im 2 phút nhìn vào khoảng không rồi mới bước vào chỗ tường sắp đổ.",
                RelicKeptInTinBox = "Con heo đất nhựa màu vàng đã mổ bụng và viên bi ve xanh biển",
                PackageDeliveredToEdgeTown = "Kiện #04 (05/11/2018): Hộp đồ chơi nhỏ và bức vẽ 7 con heo chừa 1 khoảng trống cho chị Hai",
                FarewellActionInChapter8 = "Con heo chân trắng dụi đầu vào lòng bàn tay An đang ngửa lên như Bé Khoa từng đặt viên kẹo chanh vào tay chị."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 5,
                ProtagonistPerspective = PlayerId.An,
                MemberName = "Cha Hậu",
                BirthOrAgeInfo = "49 tuổi, thợ sửa đồng hồ và khóa cơ",
                EventDateAndTime = "24/08/2019 (Thứ Bảy, 14:22 -> 14:24)",
                OfficialCauseOfDeath = "Đột quỵ tại bàn làm việc",
                AbnormalDetailTwoMinutesAndAngle = "Cha để lại chìa khóa đồng số 7; 14 chiếc đồng hồ cùng điểm chuông lúc 14:24 và kim giây quay lùi 2 phút.",
                RelicKeptInTinBox = "Chìa khóa đồng số 7",
                PackageDeliveredToEdgeTown = "Kiện #05 (12/01/2019): Hộp bánh răng đồng hồ và lọ dầu máy",
                FarewellActionInChapter8 = "Tám Vá gật đầu chào An bên bàn kìm sửa khóa lần cuối."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 6,
                ProtagonistPerspective = PlayerId.An,
                MemberName = "Đêm An tự đi bộ tới Bờ Rìa (Giữ chỗ)",
                BirthOrAgeInfo = "An (17 tuổi — đêm mộng du mất ký ức)",
                EventDateAndTime = "05/10/2019 (Thứ Bảy, 02:00 sáng)",
                OfficialCauseOfDeath = "Không tử vong — An thức dậy thấy gấu quần pijama dính hạt cỏ chỉ khô màu vàng nhạt của Bờ Rìa",
                AbnormalDetailTwoMinutesAndAngle = "Giữa cuốn sổ bìa hồng xuất hiện một trang giấy trắng tinh chỉ có đúng một dòng chữ mực đen của chính An nhưng KHÔNG có 'ơi', KHÔNG dán sticker và KHÔNG có mặt cười :): '02:00. Đã giữ chỗ.'",
                RelicKeptInTinBox = "Mảnh da cổ khắc số 24 chữ hình nêm Babylon (mặt sau ghi: 'Đừng mở trước ngày sinh nhật lần thứ hai mươi tư. — An, 21/09/2026')",
                PackageDeliveredToEdgeTown = "An tự tay viết thư hẹn gửi tiệm Bà Sáu không một nét mặt cười ở cuối",
                FarewellActionInChapter8 = "An tự nguyện đưa hai bàn tay ngửa lên để Sirphina dung hợp hai nửa linh hồn và giữ sợi liên kết ở lại thế giới chính."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 7,
                ProtagonistPerspective = PlayerId.An,
                MemberName = "Mẹ Thủy",
                BirthOrAgeInfo = "46 tuổi, kế toán trường học",
                EventDateAndTime = "08/11/2019 (Thứ Sáu, 18:04 -> 18:06)",
                OfficialCauseOfDeath = "Ngộ độc khí than trong bếp",
                AbnormalDetailTwoMinutesAndAngle = "Mẹ để lại sợi dây chuyền bạc khắc 7 vạch nhỏ ở viền sau và 7 chiếc bát trên bàn ăn.",
                RelicKeptInTinBox = "Sợi dây chuyền bạc khắc 7 vạch nhỏ",
                PackageDeliveredToEdgeTown = "Kiện #06 (07/04/2019): Tập sổ tay bìa vải và chiếc bát men xanh mẻ miệng thứ hai",
                FarewellActionInChapter8 = "Mận ôm nhẹ vai An ở hiên nhà trước khi sương chiều tan hẳn."
            },
            new CanonicalFamilyTimelineRecord
            {
                OrderIndex = 8,
                ProtagonistPerspective = PlayerId.An,
                MemberName = "An (Nhân vật chính — Kiện thứ 7 / 'Sáu với một')",
                BirthOrAgeInfo = "Tròn 24 tuổi (sinh ngày 07/09/2002)",
                EventDateAndTime = "24/09/2026 (Thứ Năm — ngày đặt chân tới Bờ Rìa)",
                OfficialCauseOfDeath = "Đang sống — Người cuối cùng mở hộp sắt dưới bậc cầu thang thứ 7",
                AbnormalDetailTwoMinutesAndAngle = "Trang cuối sổ bìa hồng tối 23/09/2026 có dòng bút chì 'Đừng đếm.' không có sticker và không có :). Sáng 24/09/2026 An lên chuyến xe khách đi Bờ Rìa.",
                RelicKeptInTinBox = "Sổ bìa hồng dán sticker con thỏ (mất 1 góc tai phải) và hộp sắt bậc cầu thang thứ 7",
                PackageDeliveredToEdgeTown = "Kiện #07 (24/09/2026): Chính là An bước xuống xe khách tại Bờ Rìa",
                FarewellActionInChapter8 = "An trở về thành phố, lấy chiếc bát nằm ngửa trên giá xuống múc một bát canh nóng: \"Dù tôi từng là gì, hôm nay tôi vẫn phải ăn tối.\""
            }
        };

        public static IReadOnlyList<CanonicalFamilyTimelineRecord> GetFamilyTimelineByProtagonist(PlayerId protagonist)
        {
            return protagonist == PlayerId.An ? FamilyTimelineTable1B_AnRoute : FamilyTimelineTable14;
        }

        // =====================================================================
        // B. MỤC 2.7: BẢNG ĐỐI CHIẾU HAI GÓC NHÌN KHOA ("LẤY CƯƠNG") VS AN ("LẤY NHU"),
        //    SỔ BÌA HỒNG DÁN STICKER CON THỎ (★ / ☾ / :)) & CÁC LẦN MẶT NẠ TRƯỢT
        // =====================================================================
        public static readonly IReadOnlyList<DualProtagonistComparisonRow> DualProtagonistComparisonTable27 = new List<DualProtagonistComparisonRow>
        {
            new DualProtagonistComparisonRow
            {
                RowNumber = 1,
                Aspect = "Cách chịu đựng",
                KhoaManifestation = "Đóng khít mọi cảm xúc (\"lấy cương\"). Làm việc như một cái máy chính xác, không than thở, không nhắc chuyện cũ.",
                AnManifestation = "Mềm mỏng và chu đáo quá mức (\"lấy nhu\"). Luôn mỉm cười, luôn hỏi thăm người khác trước để không ai hỏi tới mình."
            },
            new DualProtagonistComparisonRow
            {
                RowNumber = 2,
                Aspect = "Biểu cảm ngoài đời vs Mặt phản chiếu",
                KhoaManifestation = "Mặt thật của Khoa luôn phẳng lặng ráo hoảnh; nhưng bóng phản chiếu của Khoa trong gương/mặt nước lại nhíu mày (Q10), mỉm cười (S07), khóc (Q19), hoặc cười nối đuôi vô tận (8.3).",
                AnManifestation = "Mặt thật của An luôn mỉm cười dịu dàng; nhưng bóng phản chiếu của An trong kính cửa sổ đứng im không cười (Q10), nhìn thẳng từ giếng nước (S07), khóc trong mặt dây chuyền bạc (Q19), hoặc bóng thứ 7 đứng im không cười (8.3)."
            },
            new DualProtagonistComparisonRow
            {
                RowNumber = 3,
                Aspect = "Cuốn sổ tay",
                KhoaManifestation = "Sổ bìa đen 2 cột: Cột trái \"Thấy\" (chuyện chưa kiểm chứng) — Cột phải \"Có thật\" (chuyện đã có người thứ hai xác nhận) + Lề bút chì.",
                AnManifestation = "Sổ bìa hồng dán sticker con thỏ (quà sinh nhật 16 tuổi Bé Khoa mua tặng chị bằng tiền heo đất 43.000đ). Phân loại bằng sticker: ★ (Ngôi sao = ai cũng thấy) và ☾ (Mặt trăng = chỉ mình An để ý) + Lề bút chì."
            },
            new DualProtagonistComparisonRow
            {
                RowNumber = 4,
                Aspect = "Cách ghi chép",
                KhoaManifestation = "Gạch đầu dòng ngắn gọn, ghi giờ phút và số đo chính xác như biên bản hiện trường.",
                AnManifestation = "Viết thành những bức thư ngắn gửi một người cụ thể trong nhà (\"Ông ơi,\", \"Bà ơi,\", \"Anh Minh ơi,\", \"Khoa ơi,\", \"Ba ơi,\", \"Mẹ ơi,\", \"Nhà mình ơi,\") và kết thúc bằng mặt cười :) vẽ tay."
            },
            new DualProtagonistComparisonRow
            {
                RowNumber = 5,
                Aspect = "Các dòng viết trong lúc mất ý thức",
                KhoaManifestation = "Cùng nét chữ thẳng của Khoa nhưng viết vào lúc 02:00 sáng hoặc ký ngày tương lai 21/09/2026.",
                AnManifestation = "Cùng nét chữ tròn của An nhưng KHÔNG có chữ \"ơi\", KHÔNG dán sticker ★/☾, và KHÔNG có mặt cười :) ở cuối (\"02:00. Đã giữ chỗ.\", mảnh da cổ 21/09/2026, thư gửi tiệm Bà Sáu, \"Đừng đếm.\")."
            },
            new DualProtagonistComparisonRow
            {
                RowNumber = 6,
                Aspect = "Giọng văn nội tâm",
                KhoaManifestation = "Khô, tỉnh, hơi hài hước đen kiểu người kỹ thuật tự giễu sự vô lý bằng con số.",
                AnManifestation = "Ấm, nhẹ, lễ phép nhưng càng về sau những câu kết bằng :) càng làm người đọc nhói vì sự bình thản đáng sợ."
            },
            new DualProtagonistComparisonRow
            {
                RowNumber = 7,
                Aspect = "Cách dân làng Bờ Rìa nhìn",
                KhoaManifestation = "Thấy một thanh niên ít nói, tay làm nhanh hơn miệng, giống hệt cái dáng đứng im của ông Lâm và anh Minh.",
                AnManifestation = "Thấy một cô gái ngoan, hay cười, hay phụ việc lặt vặt, giống cái dáng ngồi nhặt rau của bà Sương và mẹ Thủy."
            },
            new DualProtagonistComparisonRow
            {
                RowNumber = 8,
                Aspect = "Cách hỏi cung / tiếp cận bí mật",
                KhoaManifestation = "Đặt vật chứng lên bàn, chỉ thẳng vào số đo lệch để buộc người đối diện không chối được.",
                AnManifestation = "Ngồi nghe người làng kể chuyện đời thường, pha trà, phụ khâu vá rồi hỏi nhẹ một câu khiến họ tự mở lòng."
            },
            new DualProtagonistComparisonRow
            {
                RowNumber = 9,
                Aspect = "Phản ứng cơ thể với dị thường",
                KhoaManifestation = "Nhạy với thị giác, ánh sáng, góc đổ bóng và độ trễ 2 phút của kim đồng hồ.",
                AnManifestation = "Nhạy với mùi hương (mùi thuốc lào ông nội, mùi dầu gió bà nội, mùi vỏ quýt của Bé Khoa, mùi khói bếp của mẹ) và nhiệt độ trên đầu ngón tay."
            }
        };

        public static readonly IReadOnlyList<AnLetterNotebookEntry> AnPinkCoverNotebookEntries = new List<AnLetterNotebookEntry>
        {
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_1B09",
                QuestOrSceneRef = "1B.9",
                RecipientSalutation = "Khoa ơi,",
                Sticker = AnNotebookSticker.Star_VerifiableByAll,
                LetterBody = "Hôm nay chị dọn lại ngăn kéo bàn học của em. Con heo đất màu vàng vẫn nằm ở góc bàn, chị giữ kỹ lắm :)",
                PencilMarginHypothesis = "Tiền xu trong heo đất ngày xưa có một đồng xu cổ không biết của năm nào.",
                EndsWithSmiley = true,
                IsUnconsciousBlackoutEntry = false
            },
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_1B10_BLACKOUT",
                QuestOrSceneRef = "1B.10",
                RecipientSalutation = string.Empty,
                Sticker = AnNotebookSticker.None,
                LetterBody = "02:00. Đã giữ chỗ.",
                PencilMarginHypothesis = string.Empty,
                EndsWithSmiley = false,
                IsUnconsciousBlackoutEntry = true
            },
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_1B12_BLACKOUT",
                QuestOrSceneRef = "1B.12",
                RecipientSalutation = string.Empty,
                Sticker = AnNotebookSticker.None,
                LetterBody = "Đừng mở trước ngày sinh nhật lần thứ hai mươi tư. — An, 21/09/2026",
                PencilMarginHypothesis = string.Empty,
                EndsWithSmiley = false,
                IsUnconsciousBlackoutEntry = true
            },
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_1B15_BLACKOUT",
                QuestOrSceneRef = "1B.15",
                RecipientSalutation = string.Empty,
                Sticker = AnNotebookSticker.None,
                LetterBody = "Đừng đếm.",
                PencilMarginHypothesis = string.Empty,
                EndsWithSmiley = false,
                IsUnconsciousBlackoutEntry = true
            },
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_Q01",
                QuestOrSceneRef = "5B.Q01",
                RecipientSalutation = "Nhà mình ơi,",
                Sticker = AnNotebookSticker.Star_VerifiableByAll,
                LetterBody = "Con tới Bờ Rìa rồi ạ. Trại có một cái chuồng cũ, hai cái máng gỗ, một cây chổi rơm và một con ngỗng trắng cứ nhìn chằm chằm vào túi xách của con :)",
                PencilMarginHypothesis = "Bà Sáu đưa chìa khóa trại trước khi con kịp nói tên.",
                EndsWithSmiley = true,
                IsUnconsciousBlackoutEntry = false
            },
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_Q05",
                QuestOrSceneRef = "5B.Q05",
                RecipientSalutation = "Ông ơi,",
                Sticker = AnNotebookSticker.Star_VerifiableByAll,
                LetterBody = "Trong cái bao tải rách tên trộm đánh rơi có tấm ván thông đóng dấu Mộc Lâm — 04/2018. Ông gửi gỗ lên đây trước khi ngã cầu thang những ba tháng cơ à :)",
                PencilMarginHypothesis = "Nét chữ chì mặt sau tấm ván là chữ của ông nội.",
                EndsWithSmiley = true,
                IsUnconsciousBlackoutEntry = false
            },
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_Q08",
                QuestOrSceneRef = "5B.Q08",
                RecipientSalutation = "Khoa ơi,",
                Sticker = AnNotebookSticker.Moon_OnlyAnNoticed,
                LetterBody = "Hôm nay trong đàn có một con heo lạ cứ đi theo chị, lúc chị đưa tay ra nó dụi đầu vào lòng bàn tay chị hệt như hồi em đòi chị bóc kẹo chanh :)",
                PencilMarginHypothesis = "Dòng gen hiện dấu '?' nhưng không giống bất kỳ giống heo nào trong sách.",
                EndsWithSmiley = true,
                IsUnconsciousBlackoutEntry = false
            },
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_Q10",
                QuestOrSceneRef = "5B.Q10",
                RecipientSalutation = "Anh Minh ơi,",
                Sticker = AnNotebookSticker.Moon_OnlyAnNoticed,
                LetterBody = "Dưới đáy giếng cạn có một cái đĩa sắt tròn ấm lắm. Lúc em quay sang cười với chú Tư, cái bóng của em trên kính cửa sổ lại đứng im không cười với em :)",
                PencilMarginHypothesis = "Bóng trong kính nhìn thẳng xuống đáy giếng.",
                EndsWithSmiley = true,
                IsUnconsciousBlackoutEntry = false
            },
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_Q11",
                QuestOrSceneRef = "5B.Q11",
                RecipientSalutation = "Khoa ơi,",
                Sticker = AnNotebookSticker.Star_VerifiableByAll,
                LetterBody = "Đàn heo đông hơn một trăm con rồi mà lúc ngủ tụi nó vẫn chừa trống một vòng tròn nhỏ ở góc Tây Bắc. Chú Tư cũng nhìn thấy nên chị đổi từ mặt trăng sang ngôi sao nhé :)",
                PencilMarginHypothesis = "Đúng chỗ đất trong bức ảnh năm 2018.",
                EndsWithSmiley = true,
                IsUnconsciousBlackoutEntry = false
            },
            new AnLetterNotebookEntry
            {
                EntryId = "AN_NB_Q19",
                QuestOrSceneRef = "5B.Q19",
                RecipientSalutation = "Mẹ ơi,",
                Sticker = AnNotebookSticker.Moon_OnlyAnNoticed,
                LetterBody = "Con đeo lại sợi dây chuyền bạc có sáu với một vết khắc của mẹ rồi. Con vẫn cười như mẹ dặn, chỉ có đứa con gái nằm trong mặt bạc là cứ khóc hoài thôi :)",
                PencilMarginHypothesis = "Bảy cái ghế quanh bàn ăn cuối thu.",
                EndsWithSmiley = true,
                IsUnconsciousBlackoutEntry = false
            }
        };

        public static readonly IReadOnlyList<ProtagonistMaskSlipRecord> ProtagonistMaskSlipsAndPalmsUpGesture = new List<ProtagonistMaskSlipRecord>
        {
            new ProtagonistMaskSlipRecord
            {
                StageOrChapter = "Chương 1 (Nhận trại & Sửa máng)",
                KhoaUnconsciousSlip = "Khoa đóng đinh cái máng gỗ thứ 7 rồi tự nhiên chừa một khoảng trống bên cạnh cho người thuận tay trái như ông nội.",
                AnUnconsciousSlip = "An bày bát đũa trong bếp trại, miệng vẫn cười nhưng tay đã vô thức đặt chiếc bát thứ hai đối diện mình trước khi kịp cất đi.",
                TriggeredBothPalmsUpGesture = true
            },
            new ProtagonistMaskSlipRecord
            {
                StageOrChapter = "Chương 3–4 (Chợ Đêm & Hộp kỹ thuật sau chuồng)",
                KhoaUnconsciousSlip = "Khi nhận lại chiếc còi nhựa đỏ và mảnh gỗ từ tay Tám Vá, Khoa vô thức đưa cả hai tay ra, lòng bàn tay ngửa lên.",
                AnUnconsciousSlip = "Khi nhận gói thảo dược từ tay Bà Sáu, An ngừng cười nửa nhịp và đưa cả hai lòng bàn tay ngửa lên đón lấy như một nghi thức từ ba ngàn năm trước.",
                TriggeredBothPalmsUpGesture = true
            },
            new ProtagonistMaskSlipRecord
            {
                StageOrChapter = "Chương 6 (Đêm cuối thu Q19 & Bóng phản chiếu)",
                KhoaUnconsciousSlip = "Khoa đứng nhìn hoàng hôn lâu hơn bình thường 5 giây, mặt thật ráo hoảnh nhưng bóng trong mặt dây chuyền bạc đang khóc.",
                AnUnconsciousSlip = "An vẫn giữ nụ cười trên môi khi nhìn xuống mặt dây chuyền 7 vết khắc, nhưng bóng An trong mảnh bạc đang khóc không thành tiếng.",
                TriggeredBothPalmsUpGesture = true
            },
            new ProtagonistMaskSlipRecord
            {
                StageOrChapter = "Chương 8 (Hoàng Hôn Trên Đồng Cỏ — Dung hợp với Sirphina)",
                KhoaUnconsciousSlip = "Khoa chủ động đưa hai bàn tay ngửa lên trước Đại Cổ Thụ — lần này không còn là thói quen vô thức mà là sự lựa chọn hoàn chỉnh.",
                AnUnconsciousSlip = "An chủ động đưa hai bàn tay ngửa lên đón ánh sáng cuối cùng của Sirphina — nụ cười trên mặt thật và bóng phản chiếu lần đầu tiên khớp làm một.",
                TriggeredBothPalmsUpGesture = true
            }
        };

        // =====================================================================
        // C. PHẦN 4, PHẦN 5 / 5B & PHẦN 6: DANH MỤC P01–P08, Q01–Q20, S01–S12
        //    (Đúng nguyên văn tiêu đề trong tài liệu "Sáu Với Một" có góc nhìn An)
        // =====================================================================
        public static readonly IReadOnlyList<CanonicalQuestTableSpec> PrologueP01ToP08 = new List<CanonicalQuestTableSpec>
        {
            new CanonicalQuestTableSpec { Code = "P01", Title = "Một nhà bình thường", ChapterOrType = "Chương 0 · Mở đầu", TriggerCondition = "Bắt đầu game mới (2018)", SurfaceTask = "Bữa cơm chiều đủ 7 người trong căn nhà thành phố", HiddenStoryTruth = "Bữa cơm cuối cùng có đủ Ông Lâm, Bà Sương, Cha Hậu, Mẹ Thủy, Anh Minh, Khoa và An", KhoaPerspectiveNote = "Khoa ngồi cạnh em gái Bé An đang vẽ heo con.", AnPerspectiveNote = "An ngồi cạnh em trai Bé Khoa đang giữ con heo đất nhựa màu vàng." },
            new CanonicalQuestTableSpec { Code = "P02", Title = "Những ngày may mắn", ChapterOrType = "Chương 0 · Mở đầu", TriggerCondition = "Ký ức trước tháng 07/2018", SurfaceTask = "Nhìn lại những kỷ niệm êm ấm của gia đình trước biến cố", HiddenStoryTruth = "Sự bình yên trước khi 6 người lần lượt gửi 6 kiện đồ về Bờ Rìa", KhoaPerspectiveNote = "Khoa sửa bản lề cửa cùng cha và ông nội.", AnPerspectiveNote = "Bé Khoa đập heo đất 43.000đ mua tặng chị An cuốn sổ bìa hồng dán sticker con thỏ (07/09/2018)." },
            new CanonicalQuestTableSpec { Code = "P03", Title = "Hai phút", ChapterOrType = "Chương 0 · Mở đầu", TriggerCondition = "Chuỗi biến cố 17/07/2018 – 24/08/2019", SurfaceTask = "Chứng kiến những cái chết cách nhau vài tháng trong gia đình", HiddenStoryTruth = "Mọi đồng hồ đều lệch đúng 2 phút và có một chi tiết vật lý ngược chiều", KhoaPerspectiveNote = "Khoa bắt đầu kẻ cuốn sổ đen thành 2 cột 'Thấy' và 'Có thật'.", AnPerspectiveNote = "An bắt đầu viết những bức thư ngắn dán sticker ★/☾ gửi từng người đã mất và vẽ mặt cười :) ở cuối." },
            new CanonicalQuestTableSpec { Code = "P04", Title = "Bậu cửa sổ", ChapterOrType = "Chương 0 · Mở đầu", TriggerCondition = "Sau tang lễ Mẹ Thủy (08/11/2019)", SurfaceTask = "Dọn dẹp căn nhà trống và chăm chậu sứ trắng ở chiếu nghỉ", HiddenStoryTruth = "Chậu sứ thứ 7 nứt đôi nhưng vẫn còn một nhánh khô chưa chết hẳn", KhoaPerspectiveNote = "Khoa đóng cửa các phòng trống, chỉ giữ lại bàn học.", AnPerspectiveNote = "An lau lại 7 chiếc bát trên kệ bếp và giữ nụ cười lễ phép với hàng xóm." },
            new CanonicalQuestTableSpec { Code = "P05", Title = "Ngày hai mươi bốn", ChapterOrType = "Chương 0 · Mở đầu", TriggerCondition = "Tháng 09/2026 (tròn 24 tuổi)", SurfaceTask = "Chuẩn bị qua tuổi 24 trong căn nhà chỉ còn một người", HiddenStoryTruth = "Chu kỳ 3.000 năm (24 × 125) chạm điểm mở khóa dưới bậc cầu thang thứ 7", KhoaPerspectiveNote = "Khoa nghe tiếng cạch rỗng dưới bậc cầu thang gỗ thứ 7.", AnPerspectiveNote = "An ngửi thấy mùi gỗ thị của ông nội và mùi vỏ quýt dưới bậc cầu thang thứ 7." },
            new CanonicalQuestTableSpec { Code = "P06", Title = "Ánh kim", ChapterOrType = "Chương 0 · Mở đầu", TriggerCondition = "Mở bậc cầu thang thứ 7", SurfaceTask = "Lấy chiếc hộp sắt bánh quy cũ dưới bậc cầu thang thứ 7", HiddenStoryTruth = "Bên trong có 6 kỷ vật, mảnh da cổ khắc số 24 chữ hình nêm Babylon và biên lai gửi đồ về Bờ Rìa", KhoaPerspectiveNote = "Mặt sau mảnh da cổ ghi chữ Khoa ngày 21/09/2026.", AnPerspectiveNote = "Mặt sau mảnh da cổ ghi chữ An ngày 21/09/2026 không có sticker và không có mặt cười :)." },
            new CanonicalQuestTableSpec { Code = "P07", Title = "Đừng đếm", ChapterOrType = "Chương 0 · Mở đầu", TriggerCondition = "Đêm 23/09/2026 trước khi đi Bờ Rìa", SurfaceTask = "Kiểm tra lại cuốn sổ tay và hành lý lúc nửa đêm", HiddenStoryTruth = "Dòng chữ '02:00. Đã giữ chỗ.' (05/10/2019) và lời dặn 'Đừng đếm.' viết trong lúc mất ý thức", KhoaPerspectiveNote = "Cột 'Thấy' ghi dòng bút chì 'Đừng đếm.' ở trang cuối.", AnPerspectiveNote = "Lề trang cuối sổ bìa hồng có dòng bút chì 'Đừng đếm.' trơ trọi, không 'ơi', không sticker, không :)." },
            new CanonicalQuestTableSpec { Code = "P08", Title = "Người đứng đợi", ChapterOrType = "Chương 0 · Mở đầu", TriggerCondition = "Hoàng hôn 24/09/2026", SurfaceTask = "Bước xuống chuyến xe khách cuối cùng trước cổng thị trấn Bờ Rìa", HiddenStoryTruth = "Dân làng Bờ Rìa đã đợi sẵn người nhận 'Kiện sáu với một'", KhoaPerspectiveNote = "Bà Sáu nhìn Khoa và đưa chùm chìa khóa đồng số 7.", AnPerspectiveNote = "Bà Sáu nhìn túi xách có móc khóa len của An và đưa chùm chìa khóa trước khi An kịp xưng tên." }
        };

        public static readonly IReadOnlyList<CanonicalQuestTableSpec> MainQuestsQ01ToQ20 = new List<CanonicalQuestTableSpec>
        {
            new CanonicalQuestTableSpec { Code = "Q01", Title = "Cậu tới nhận trại", AnTitleVariant = "Cháu tới nhận trại", ChapterOrType = "Chương 1 · Trại Cũ Cuối Đường", TriggerCondition = "Đặt chân tới Farm_Main ngày 1", SurfaceTask = "Nhận bàn giao chuồng heo cũ, dọn cỏ, cho 3 heo đầu tiên ăn và gặp ngỗng Giám Đốc", HiddenStoryTruth = "Dân làng Bờ Rìa không gọi số 7 mà gọi là 'sáu với một'; trại đã được dọn sẵn trước khi người chơi tới", KhoaPerspectiveNote = "Bà Sáu gọi 'Cậu tới nhận trại hả?'.", AnPerspectiveNote = "Bà Sáu gọi 'Cháu tới nhận trại hả?' và An viết thư 'Nhà mình ơi,' dán ★ :)." },
            new CanonicalQuestTableSpec { Code = "Q02", Title = "Máng ít heo nhiều", AnTitleVariant = "Máng ít heo nhiều", ChapterOrType = "Chương 1 · Trại Cũ Cuối Đường", TriggerCondition = "Đàn heo đạt >= 6 con", SurfaceTask = "Đóng thêm máng ăn, máng nước và kiểm tra sức chứa chuồng", HiddenStoryTruth = "Máng gỗ cũ ở góc chuồng có vết đục tay trái giống hệt tay nghề của Ông nội Lâm", KhoaPerspectiveNote = "Khoa đo vết đục nghiêng 15 độ của người thuận tay trái.", AnPerspectiveNote = "An sờ vết bào nhẵn ở mép máng gỗ và ngửi thấy mùi gỗ thị quen thuộc của ông nội." },
            new CanonicalQuestTableSpec { Code = "Q03", Title = "Gió lốc hướng lên", AnTitleVariant = "Gió lốc hướng lên", ChapterOrType = "Chương 1 · Trại Cũ Cuối Đường", TriggerCondition = "Đợt quái nhỏ đầu tiên ban đêm", SurfaceTask = "Đánh đuổi toán Kẻ Bắt Heo đầu tiên và sửa rào chuồng", HiddenStoryTruth = "Khi toán trộm rút lui, lá khô quanh rào bay ngược từ mặt đất lên trời trong đúng 2 phút", KhoaPerspectiveNote = "Khoa ghi vào cột 'Thấy': Lá khô bay ngược lên trời 2 phút.", AnPerspectiveNote = "An dán sticker Mặt trăng ☾ vào thư gửi Bé Khoa vì sợ nói ra người ta bảo chị tưởng tượng :)." },
            new CanonicalQuestTableSpec { Code = "Q04", Title = "Chợ đêm không thối tiền", AnTitleVariant = "Chợ đêm không thối tiền", ChapterOrType = "Chương 2 · Đêm Có TiếngRìu", TriggerCondition = "Đạt Cấp trại 5 & vào Market_Town sau 18:00", SurfaceTask = "Tham gia phiên Chợ Đêm đầu tiên và tìm hiểu quy tắc mua bán", HiddenStoryTruth = "Chợ Đêm Bờ Rìa chỉ có 6 gian hàng sáng đèn; gian thứ 7 luôn phủ bạt nhưng vẫn có bóng người bên dưới", KhoaPerspectiveNote = "Khoa đếm đúng 6 bóng đèn dầu nhưng dưới đất có 7 cái bóng.", AnPerspectiveNote = "An chào đủ các cô chú bán hàng và nhận ra không ai thối lại đồng xu lẻ thứ 7." },
            new CanonicalQuestTableSpec { Code = "Q05", Title = "Bố của cái rìu", AnTitleVariant = "Bố của cái rìu", ChapterOrType = "Chương 2 · Đêm Có Tiếng Rìu", TriggerCondition = "Hạ Chúa Trộm Rìu Sắt ở Chu kỳ 6", SurfaceTask = "Đánh bại Chúa Trộm Rìu Sắt và thu hồi bao tải chiến lợi phẩm", HiddenStoryTruth = "Trong bao tải có tấm ván thông đóng dấu 'Mộc Lâm — 04/2018' của Ông nội Lâm gửi trước khi mất 3 tháng", KhoaPerspectiveNote = "Tám Vá xác nhận tấm gỗ -> chuyển mục #1 sang cột 'Có thật'.", AnPerspectiveNote = "Tám Vá xác nhận dấu mộc của ông Lâm -> An dán đè sticker Ngôi sao ★ lên cạnh Mặt trăng ☾ :)." },
            new CanonicalQuestTableSpec { Code = "Q06", Title = "Phòng xây dựng thiếu người", AnTitleVariant = "Phòng xây dựng thiếu người", ChapterOrType = "Chương 3 · Vòng Tròn Trống", TriggerCondition = "Mở Phòng Xây Dựng ở Farm_Core", SurfaceTask = "Khôi phục Phòng Xây Dựng và đặt các công trình phòng thủ mới", HiddenStoryTruth = "Bản vẽ quy hoạch gốc của trại được ký tên kỹ sư 'Trần Minh — 08/2018' (Anh cả Minh)", KhoaPerspectiveNote = "Khoa nhận ra nét chữ ký kỹ thuật của anh Minh ở góc bản vẽ.", AnPerspectiveNote = "An viết thư 'Anh Minh ơi, nét vẽ cầu cống thoát nước ở trại giống hệt tập đồ án của anh :)'." },
            new CanonicalQuestTableSpec { Code = "Q07", Title = "Cái này mua bằng cái kia", AnTitleVariant = "Cái này mua bằng cái kia", ChapterOrType = "Chương 3 · Vòng Tròn Trống", TriggerCondition = "Gặp Bí Nhân lúc tối muộn (ÁLSK >= 30)", SurfaceTask = "Thực hiện giao dịch đổi vật phẩm (0% Vàng, >= 130% GTTC) với Bí Nhân", HiddenStoryTruth = "Bí Nhân đưa lại chiếc còi nhựa màu đỏ của Anh cả Minh — bên trong có viên bi gỗ khắc số 7", KhoaPerspectiveNote = "Còi nhựa đỏ kêu lại sau 7 năm -> chuyển mục #2 sang cột 'Có thật'.", AnPerspectiveNote = "An nhận lại chiếc còi đỏ bằng hai lòng bàn tay ngửa lên và dán sticker ★ :)." },
            new CanonicalQuestTableSpec { Code = "Q08", Title = "Con heo có dấu hỏi", AnTitleVariant = "Con heo có dấu hỏi", ChapterOrType = "Chương 3 · Vòng Tròn Trống", TriggerCondition = "Xuất hiện cá thể heo Hiếm+ chưa định danh ('?')", SurfaceTask = "Đưa con heo có ký hiệu '?' đi giám định dòng gen tại Phòng Nghiên Cứu / Chợ", HiddenStoryTruth = "Kết quả giám định khớp với hồ sơ viết tay của Cha Hậu gửi từ tháng 01/2019", KhoaPerspectiveNote = "Bác sĩ thú y xác nhận chữ viết của cha Hậu -> chuyển mục #3 sang 'Có thật'.", AnPerspectiveNote = "Con heo '?' dụi đầu vào lòng bàn tay An hệt như Bé Khoa đòi kẹo chanh -> dán sticker ★ :)." },
            new CanonicalQuestTableSpec { Code = "Q09", Title = "Qua tám lần mới được hỏi", AnTitleVariant = "Qua tám lần mới được hỏi", ChapterOrType = "Chương 3 · Vòng Tròn Trống", TriggerCondition = "Vượt qua >= 8 đợt quái lớn (Mở Cổng Giữa Game)", SurfaceTask = "Chứng minh năng lực giữ trại qua 8 đợt tấn công lớn để người làng tin cậy", HiddenStoryTruth = "Sau đợt thứ 8, Bà Sáu và Ông Tồn mới thôi trả lời 'Mai trời nắng' khi hỏi về người chủ cũ", KhoaPerspectiveNote = "Mở Cổng Giữa Game và bắt đầu mở 6 khóa 'Trại Chủ Lão Luyện'.", AnPerspectiveNote = "Mở Cổng Giữa Game và bắt đầu mở 6 khóa 'Người Đọc Dòng'." },
            new CanonicalQuestTableSpec { Code = "Q10", Title = "Mẫu Hạm có mùi cám", AnTitleVariant = "Mẫu Hạm có mùi cám", ChapterOrType = "Chương 4 · Mùi Cám Trên Trời", TriggerCondition = "Hạ Boss Mẫu Hạm UFO ở Chu kỳ 12", SurfaceTask = "Dùng Tháp Phòng Không Skyhook và lưới điện bắn hạ Mẫu Hạm UFO", HiddenStoryTruth = "Mảnh vỏ đĩa sắt dưới giếng thực chất là lồng sấy cám công nghiệp có đóng dấu ngày 13/05/2019; bóng trong kính cửa sổ phản ứng ngược với mặt thật", KhoaPerspectiveNote = "Mặt thật Khoa phẳng lặng nhưng bóng trên kính cửa sổ nhíu mày nhìn xuống giếng.", AnPerspectiveNote = "Mặt thật An cười chào chú Tư nhưng bóng An trên kính cửa sổ đứng im không cười, mắt nhìn thẳng xuống đĩa sắt." },
            new CanonicalQuestTableSpec { Code = "Q11", Title = "Đàn này có người", AnTitleVariant = "Đàn này có người", ChapterOrType = "Chương 4 · Mùi Cám Trên Trời", TriggerCondition = "Đạt trạng thái Bầy Đàn Sơ Khai (>= 100 heo gắn kết)", SurfaceTask = "Quan sát tập tính ngủ đêm của đàn heo khi đạt trạng thái Bầy Đàn", HiddenStoryTruth = "Cả trăm con heo tự động chừa trống một vòng tròn đường kính 1,2m ở góc Tây Bắc — đúng chỗ bức tranh của em út (Bé An / Bé Khoa)", KhoaPerspectiveNote = "Chú Tư xác nhận vòng tròn trống 1,2m -> chuyển mục #4 sang 'Có thật'.", AnPerspectiveNote = "Chú Tư cũng thấy vòng tròn trống -> An dán sticker Ngôi sao ★ vào thư gửi Bé Khoa :)." },
            new CanonicalQuestTableSpec { Code = "Q12", Title = "Mua tin về chuyện mình vừa thấy", AnTitleVariant = "Mua tin về chuyện mình vừa thấy", ChapterOrType = "Chương 4 · Mùi Cám Trên Trời", TriggerCondition = "Mở Bàn Tình Báo bậc 5", SurfaceTask = "Mua hồ sơ tình báo bậc 5 về lịch sử giao nhận của thị trấn Bờ Rìa", HiddenStoryTruth = "Hồ sơ tình báo trả về đúng bức ảnh chụp chính Khoa/An đứng ở cổng Bờ Rìa lúc 02:00 sáng ngày 05/10/2019", KhoaPerspectiveNote = "Khoa đối chiếu ảnh chụp với dòng '02:00. Đã giữ chỗ.' -> chuyển mục #5 sang 'Có thật'.", AnPerspectiveNote = "An nhìn bức ảnh mình mặc đồ ngủ pijama đứng ở cổng trại năm 17 tuổi -> dán sticker ★." },
            new CanonicalQuestTableSpec { Code = "Q13", Title = "Chỗ không con nào đứng", AnTitleVariant = "Chỗ không con nào đứng", ChapterOrType = "Chương 5 · Kiện Thứ Bảy", TriggerCondition = "Kiểm tra hộp kỹ thuật sau chuồng tại góc Tây Bắc", SurfaceTask = "Đào dưới tâm vòng tròn 1,2m nơi đàn heo không bao giờ dẫm lên", HiddenStoryTruth = "Phát hiện hộp kỹ thuật 7 đường cáp: 6 đường đã nối với 6 kiện đồ của gia đình, đường thứ 7 ghi 'Kiện 6+1 — Chờ người nhận tự đến'", KhoaPerspectiveNote = "Khoa tìm thấy đủ 6 nhãn kiện đồ 2018–2019.", AnPerspectiveNote = "An đọc thấy 6 nhãn kiện đồ của Ông, Bà, Anh Minh, Bé Khoa, Ba và Mẹ." },
            new CanonicalQuestTableSpec { Code = "Q14", Title = "Buổi họp ai cũng mang ghế", AnTitleVariant = "Buổi họp ai cũng mang ghế", ChapterOrType = "Chương 5 · Kiện Thứ Bảy", TriggerCondition = "Thu thập đủ >= 3 bằng chứng ở cột 'Có thật' / sticker ★", SurfaceTask = "Mời dân làng Bờ Rìa tới sân trại và đưa ra 3 bằng chứng xác thực", HiddenStoryTruth = "Dân làng mỗi người tự mang một chiếc ghế gỗ tới; Bà Sáu thừa nhận cả 6 người thân đã gửi 6 kiện đồ lên đây để chuẩn bị chỗ ở cho người thứ 7", KhoaPerspectiveNote = "Khoa đặt 3 vật chứng lên bàn gỗ giữa sân.", AnPerspectiveNote = "An pha 7 chén trà nóng mời các cô chú trước khi mở sổ bìa hồng." },
            new CanonicalQuestTableSpec { Code = "Q15", Title = "Hết sương chưa hết chuyện", AnTitleVariant = "Hết sương chưa hết chuyện", ChapterOrType = "Chương 5 · Kiện Thứ Bảy", TriggerCondition = "Sau cuộc họp dân làng Q14 & thời tiết Sương Mù", SurfaceTask = "Đi xuyên qua lớp sương mù ở Rừng Nguyên Sinh để tìm nguồn phát tín hiệu", HiddenStoryTruth = "Sương mù ở Bờ Rìa không phải hơi nước mà là rìa ngoài của 'Đại Giới Lưu' đang mỏng dần sau 3.000 năm", KhoaPerspectiveNote = "La bàn của Khoa quay ngược về phía lò sưởi Nhà chính.", AnPerspectiveNote = "Đầu ngón tay An ấm lên khi chạm vào lớp sương gần khe đá cổ." },
            new CanonicalQuestTableSpec { Code = "Q16", Title = "Bạch Vân và người cầm kìm", AnTitleVariant = "Bạch Vân và người cầm kìm", ChapterOrType = "Chương 6 · Bữa Tối Chưa Có Khách", TriggerCondition = "Khôi phục công trình Bạch Vân & giải đố khe lò sưởi", SurfaceTask = "Làm việc cùng Tám Vá để mở khóa công trình Bạch Vân và khe hẹp bên trái lò sưởi", HiddenStoryTruth = "Đút mảnh da cổ số 24 quay mặt vào trong, buông tay chờ đúng 2 phút (11:16 -> 11:18) nghe 2+4=24 tiếng click mở hốc tường chứa bức vẽ 7 cột thước đo bằng tay trái", KhoaPerspectiveNote = "Khoa nhận ra bức vẽ 7 cột thước đo được vẽ bằng tay trái của chính mình lúc mộng du.", AnPerspectiveNote = "An nhận ra nét vẽ 7 cột thước đo bằng tay trái trên mặt sau tờ giấy gói quà của Bé Khoa." },
            new CanonicalQuestTableSpec { Code = "Q17", Title = "Chủ cũ không có mặt để ký", AnTitleVariant = "Chủ cũ không có mặt để ký", ChapterOrType = "Chương 6 · Bữa Tối Chưa Có Khách", TriggerCondition = "Mở két sắt tầng hầm Farm_Core", SurfaceTask = "Kiểm tra giấy tờ chuyển nhượng gốc của nông trại số 7", HiddenStoryTruth = "Người lập di chúc giao trại từ 7 năm trước ký tên bằng đúng chữ viết của Khoa/An vào đêm 05/10/2019", KhoaPerspectiveNote = "Chữ ký 'Khoa — 05/10/2019, 02:00' nằm ở ô Bên Giao.", AnPerspectiveNote = "Chữ ký 'An — 05/10/2019, 02:00' không có mặt cười nằm ở ô Bên Giao." },
            new CanonicalQuestTableSpec { Code = "Q18", Title = "Gom đàn chứ đừng gom sợ", AnTitleVariant = "Gom đàn chứ đừng gom sợ", ChapterOrType = "Chương 6 · Bữa Tối Chưa Có Khách", TriggerCondition = "Chuẩn bị trước đợt tấn công Chu kỳ 18", SurfaceTask = "Dùng lệnh Lùa về Nơi Trú và nâng Bầy Đàn lên bậc Vững / Hưng Thịnh", HiddenStoryTruth = "Con heo Thủ Lĩnh dẫn cả đàn xếp thành hình vòng cung bảo vệ hiên Nhà chính", KhoaPerspectiveNote = "Tiếng huýt của Khoa vang vọng đúng tần số chiếc còi đỏ của anh Minh.", AnPerspectiveNote = "An gọi tên từng con heo đầu đàn bằng giọng dịu dàng của mẹ Thủy." },
            new CanonicalQuestTableSpec { Code = "Q19", Title = "Bữa tối chưa có khách", AnTitleVariant = "Bữa tối chưa có khách", ChapterOrType = "Chương 6 · Bữa Tối Chưa Có Khách", TriggerCondition = "Đêm trước trận quyết chiến Chu kỳ 18", SurfaceTask = "Đeo sợi dây chuyền bạc 7 vết khắc của mẹ và dọn 7 chiếc ghế quanh bàn ăn", HiddenStoryTruth = "Khi nhìn xuống mặt dây chuyền bạc, mặt thật ngoài đời vẫn giữ nguyên lớp mặt nạ nhưng bóng phản chiếu trong mặt bạc đang khóc", KhoaPerspectiveNote = "Mặt thật Khoa ráo hoảnh nhưng bóng Khoa trong mặt dây chuyền đang khóc.", AnPerspectiveNote = "Mặt thật An vẫn mỉm cười dịu dàng nhưng bóng An trong mặt dây chuyền bạc đang khóc." },
            new CanonicalQuestTableSpec { Code = "Q20", Title = "Rìu Sắt trả hàng", AnTitleVariant = "Rìu Sắt trả hàng", ChapterOrType = "Chương 6 · Bữa Tối Chưa Có Khách", TriggerCondition = "Đánh bại Thực Thể Sương ở Chu kỳ 18", SurfaceTask = "Vượt qua Thực Thể Sương và mở gói đồ cuối cùng trước cổng trại", HiddenStoryTruth = "Chúa Trộm Rìu Sắt để lại cái bát men xanh mẻ miệng thứ hai cùng mảnh giấy 'Tôi chỉ lấy đồ thừa. Cái này không phải đồ thừa' -> dẫn tới lựa chọn Kết thúc Mùa 1 (8.1 / 8.2 / 8.3)", KhoaPerspectiveNote = "Khoa cầm chiếc bát men xanh thứ hai bằng hai lòng bàn tay ngửa lên.", AnPerspectiveNote = "An đón chiếc bát men xanh thứ hai bằng hai lòng bàn tay ngửa lên, khép lại Mùa 1." }
        };

        public static readonly IReadOnlyList<CanonicalQuestTableSpec> SideQuestsS01ToS12 = new List<CanonicalQuestTableSpec>
        {
            new CanonicalQuestTableSpec { Code = "S01", Title = "Giám Đốc không chịu nghỉ", ChapterOrType = "Nhiệm vụ phụ · S01", TriggerCondition = "Ngỗng Giám Đốc đi tuần lúc 06:00 sáng", SurfaceTask = "Đi theo lộ trình tuần tra của con ngỗng trắng Giám Đốc quanh 7 cọc rào", HiddenStoryTruth = "Ngỗng Giám Đốc luôn dừng lại gật đầu 2 cái trước bậc thềm trống nơi ông nội Lâm từng ngồi hút thuốc." },
            new CanonicalQuestTableSpec { Code = "S02", Title = "Người chết ăn rất khỏe", ChapterOrType = "Nhiệm vụ phụ · S02", TriggerCondition = "Cân con heo út có đốm trắng ở chân trái", SurfaceTask = "Đặt con heo chân trắng lên bàn cân sắt và gõ máng ăn 2 nhịp", HiddenStoryTruth = "Lần đầu đặt lên cân nó nằm ngửa giả chết (kim chỉ 0.0 kg); phải gõ máng 2 nhịp như thói quen của em út (Bé An / Bé Khoa) nó mới bật dậy cân đúng 42.5 kg và ăn sạch 2 máng cám." },
            new CanonicalQuestTableSpec { Code = "S03", Title = "Nồi lẩu từng bay", ChapterOrType = "Nhiệm vụ phụ · S03", TriggerCondition = "Trò chuyện với Lệ và Dung ở quán ăn thị trấn", SurfaceTask = "Tìm lại chiếc nắp nồi đồng bị móp trên mái hiên quán ăn", HiddenStoryTruth = "Chiếc nồi đồng móp đúng vị trí nồi canh của Ông nội Lâm ngày 17/07/2018 nhưng lại nằm ở Bờ Rìa từ năm 2017." },
            new CanonicalQuestTableSpec { Code = "S04", Title = "Đèn ngủ không nên biết chạy", ChapterOrType = "Nhiệm vụ phụ · S04", TriggerCondition = "Thắp đèn lồng khu chuồng sau 22:00", SurfaceTask = "Bắt kịp bóng chiếc đèn ngủ tự di chuyển dọc hành lang Nhà chính", HiddenStoryTruth = "Ánh đèn dừng lại chiếu thẳng vào khe hẹp bên trái lò sưởi (gợi ý câu đố mảnh da cổ số 24)." },
            new CanonicalQuestTableSpec { Code = "S05", Title = "Heo đỏ đi học bình tĩnh", ChapterOrType = "Nhiệm vụ phụ · S05", TriggerCondition = "Thuần dưỡng cá thể Xích Mao trưởng thành 3 ngày", SurfaceTask = "Giữ Tinh thần heo Xích Mao >= 60 trong 3 ngày liên tiếp bên cạnh chuồng trường học cũ", HiddenStoryTruth = "Heo Xích Mao nghe tiếng chuông báo giờ chiều thứ Hai (16:12) thì tự động nằm im che chắn như muốn đỡ một bức tường sắp đổ." },
            new CanonicalQuestTableSpec { Code = "S06", Title = "Lá thư dưới ổ lót", ChapterOrType = "Nhiệm vụ phụ · S06", TriggerCondition = "Thay ổ rơm lót đẻ cho heo nái Phong Mẫu", SurfaceTask = "Dọn sạch lớp rơm cũ dưới đáy chuồng sinh sản", HiddenStoryTruth = "Tìm thấy phong bì không dán tem gửi từ bưu cục thành phố ngày 02/06/2018 có nét chữ của Bà nội Sương." },
            new CanonicalQuestTableSpec { Code = "S07", Title = "Mẹ ơi con không phải kẻ trộm", ChapterOrType = "Nhiệm vụ phụ · S07", TriggerCondition = "Soi mặt xuống giếng nước trong lúc sửa máy bơm", SurfaceTask = "Vớt chiếc muỗng bạc bị rơi dưới đáy giếng cạn", HiddenStoryTruth = "Ở Khoa: mặt thật ráo hoảnh nhưng bóng dưới nước mỉm cười dịu dàng. Ở An: mặt thật mỉm cười nhưng bóng dưới nước nhìn lên bình thản không cười." },
            new CanonicalQuestTableSpec { Code = "S08", Title = "Chỗ nằm của ông", ChapterOrType = "Nhiệm vụ phụ · S08", TriggerCondition = "Sửa chiếc ghế xích đu gỗ ngoài hiên", SurfaceTask = "Thay thanh gỗ kê chân cho chiếc ghế bập bênh của Ông Tồn", HiddenStoryTruth = "Dưới gầm ghế có khắc dòng chữ 'Lâm — giữ cho cháu thứ hai' bằng đúng mũi đục cán thị." },
            new CanonicalQuestTableSpec { Code = "S09", Title = "Quạ Tin có nguồn riêng", ChapterOrType = "Nhiệm vụ phụ · S09", TriggerCondition = "Sở hữu thú cưng Quạ Đưa Tin", SurfaceTask = "Kiểm tra mảnh giấy buộc ở chân Quạ Đưa Tin sau cơn mưa đêm", HiddenStoryTruth = "Quạ Đưa Tin không bay về phía thị trấn mà bay về phía cột mốc cây số 14 nơi Anh cả Minh gặp nạn." },
            new CanonicalQuestTableSpec { Code = "S10", Title = "Bí nhân không nhận thứ Hai", ChapterOrType = "Nhiệm vụ phụ · S10", TriggerCondition = "Gặp Bí Nhân vào đêm thứ Hai", SurfaceTask = "Thử giao dịch với Bí Nhân vào đúng khung giờ chiều tối thứ Hai", HiddenStoryTruth = "Riêng tối thứ Hai (ngày mất của em út 13/05/2019), Bí Nhân để lại miễn phí 1 viên kẹo chanh / hộp bút sáp mà không đòi vật phẩm đối ứng." },
            new CanonicalQuestTableSpec { Code = "S11", Title = "Anh Ca đổi tên", ChapterOrType = "Nhiệm vụ phụ · S11", TriggerCondition = "Thuê lính đánh thuê tại Hội Quán", SurfaceTask = "Tìm hiểu lý do người quản lý Hội Quán được gọi là 'Anh Ca'", HiddenStoryTruth = "Tên thật trong sổ đăng ký cũ của Anh Ca là 'Trần Minh' — anh cả trong gia đình." },
            new CanonicalQuestTableSpec { Code = "S12", Title = "Bát của ai", ChapterOrType = "Nhiệm vụ phụ · S12", TriggerCondition = "Hoàn thành S01–S11 và kiểm tra kệ bếp Nhà chính", SurfaceTask = "Xếp lại 6 chiếc bát úp và 1 chiếc bát ngửa trên giá gỗ", HiddenStoryTruth = "6 chiếc bát úp tương ứng với 6 người đã nằm xuống giữ điểm neo; chiếc bát thứ 7 để ngửa là phần cơm dành cho người đang sống (Khoa / An)." }
        };

        // =====================================================================
        // D. PHẦN 11–12: TOÀN CẢNH 8 MÙA & HỒI KẾT CHÍNH TẮC
        //    - Đại Giới Lưu & Minh Ước (3.000 năm = 24 × 125)
        //    - Huyền Y (dịch bệnh từ xác vị thần chết cổ đại biến sinh vật thành Thực Thể Sương)
        //    - Khắc Ngữ & Đại Cổ Thụ (Thánh Điển) với 6 câu Mặc Khải nguyên văn
        //    - Kim Ô Thánh Hội (Ordo Solis Aurei / Nhật Kim Thiền Tước) đúng 4 thành viên
        //    - Luật 3 câu tối đa <= 3 từ của Sirphina ("Không phải ngài.", "Chưa đủ.", "")
        //    - Chương 8 "Hoàng Hôn Trên Đồng Cỏ" -> "Lời chào cuối cùng" với 11 cư dân Bờ Rìa -> "Ba năm sau"
        // =====================================================================
        public const int AncientSealTotalYears = 3000;
        public const int AncientSealBabylonian24 = 24;
        public const int AncientSealYearsPerUnit = 125;

        public static readonly IReadOnlyList<string> SixLinesOfMacKhaiOnGreatAncientTree = new List<string>
        {
            "Người ngã xuống.",
            "Một nửa nằm lại, một nửa tan đi.",
            "Nửa tan đi trở về trong một thời khác.",
            "Hai nửa hợp làm một.",
            "Thế giới tách đôi.",
            "Không trở lại."
        };

        public static readonly IReadOnlyList<OrdoSolisAureiMemberSpec> FourMembersOfKimOThanhHoi = new List<OrdoSolisAureiMemberSpec>
        {
            new OrdoSolisAureiMemberSpec
            {
                OrderNumber = 1,
                Title = "Thánh Minh Nhật Giả",
                AncientRole = "Thực thể cổ đại đứng đầu Kim Ô Thánh Hội (Ordo Solis Aurei / Nhật Kim Thiền Tước) — người dùng chính thân xác mình làm điểm neo Đại Giới Lưu (Minh Ước) ngăn Huyền Y tràn sang thế giới chính.",
                FateAfter3000Years = "Thân xác cùng một nửa linh hồn nằm lại ở điểm neo tạo thành ảo ảnh Bờ Rìa; nửa linh hồn kia tan vào thế giới chính và đầu thai sau 3.000 năm thành đứa trẻ sinh ngày 07/09/2002 (Khoa hoặc An)."
            },
            new OrdoSolisAureiMemberSpec
            {
                OrderNumber = 2,
                Title = "Nguyệt Thư",
                AncientRole = "Người đọc và chép lại các dòng Khắc Ngữ tự hiện lên trên thân Đại Cổ Thụ (Thánh Điển).",
                FateAfter3000Years = "Đã ngã xuống từ thời cổ đại sau khi chép lại 6 câu Mặc Khải cuối cùng."
            },
            new OrdoSolisAureiMemberSpec
            {
                OrderNumber = 3,
                Title = "Custos",
                AncientRole = "Người giữ ranh giới vật lý của vùng đất cổ đại trong lúc lập Đại Giới Lưu.",
                FateAfter3000Years = "Đã ngã xuống khi chặn làn sóng Huyền Y ở rìa thế giới cũ."
            },
            new OrdoSolisAureiMemberSpec
            {
                OrderNumber = 4,
                Title = "Sirphina",
                AncientRole = "Tinh linh đồng cỏ không thuộc tính cuối cùng — người duy nhất của Kim Ô Thánh Hội còn thức qua 3.000 năm để đợi nửa linh hồn kia trở về.",
                FateAfter3000Years = "Trong suốt 8 mùa chỉ nói tối đa 3 câu (mỗi câu <= 3 từ). Ở đầu Mùa 8, khi Khoa/An tự nguyện đưa hai bàn tay ngửa lên, Sirphina tan biến toàn bộ sinh mệnh vào thế giới và cơ thể Khoa/An để dung hợp hai nửa linh hồn và để lại sợi liên kết giữ Khoa/An ở lại thế giới chính."
            }
        };

        public static readonly IReadOnlyList<string> SirphinaCanonicalSpokenLines = new List<string>
        {
            "Không phải ngài.", // Câu 1 (Mùa 3 — 3 từ)
            "Chưa đủ.",         // Câu 2 (Mùa 5 — 2 từ)
            ""                   // Câu 3 (Mùa 8 — để trống, chỉ cúi đầu trước khi tan biến)
        };

        public static readonly IReadOnlyList<BoRiaResidentFarewellSpec> ElevenBoRiaResidentFarewells = new List<BoRiaResidentFarewellSpec>
        {
            new BoRiaResidentFarewellSpec { OrderIndex = 1, ResidentId = "BaSau", ResidentName = "Bà Sáu", FinalConversationSummary = "Bà Sáu đóng cửa tiệm tạp hóa khi ánh hoàng hôn chạm hiên gỗ, dặn về thành phố đừng bỏ bữa tối." },
            new BoRiaResidentFarewellSpec { OrderIndex = 2, ResidentId = "Man", ResidentName = "Mận", FinalConversationSummary = "Mận xếp lại túi thảo dược ở trại hồi sức, mỉm cười chào tạm biệt lần cuối." },
            new BoRiaResidentFarewellSpec { OrderIndex = 3, ResidentId = "TamVa", ResidentName = "Tám Vá", FinalConversationSummary = "Tám Vá đặt chiếc kìm sắt xuống bàn gỗ, bảo ổ khóa nhà cũ giờ đã mở được bình thường." },
            new BoRiaResidentFarewellSpec { OrderIndex = 4, ResidentId = "AnhCa", ResidentName = "Anh Ca", FinalConversationSummary = "Anh Ca đứng ở cổng Hội Quán gật đầu chào như người anh cả tiễn em về nhà." },
            new BoRiaResidentFarewellSpec { OrderIndex = 5, ResidentId = "OngTon", ResidentName = "Ông Tồn", FinalConversationSummary = "Ông Tồn gõ tẩu thuốc gỗ 2 nhịp lên lan can cầu đá rồi khép sổ kiểm dịch chợ ngày." },
            new BoRiaResidentFarewellSpec { OrderIndex = 6, ResidentId = "Le", ResidentName = "Lệ", FinalConversationSummary = "Lệ lau lại chiếc bàn gỗ trong quán nhỏ trước khi đèn chợ tắt hẳn." },
            new BoRiaResidentFarewellSpec { OrderIndex = 7, ResidentId = "Dung", ResidentName = "Dung", FinalConversationSummary = "Dung trao lại chiếc khăn ăn sạch và vẫy tay chào ở cuối con dốc." },
            new BoRiaResidentFarewellSpec { OrderIndex = 8, ResidentId = "Cuc", ResidentName = "Cúc", FinalConversationSummary = "Cúc buộc lại bó hoa khô bên hiên nhà và chúc chuyến xe về xuôi bình an." },
            new BoRiaResidentFarewellSpec { OrderIndex = 9, ResidentId = "AnhTu", ResidentName = "Anh Tư", FinalConversationSummary = "Anh Tư mở rộng cánh cổng rào gỗ của trại lần cuối để đường ra bến xe thông suốt." },
            new BoRiaResidentFarewellSpec { OrderIndex = 10, ResidentId = "GiamDoc", ResidentName = "Ngỗng Giám Đốc", FinalConversationSummary = "Con ngỗng trắng Giám Đốc đi đủ một vòng quanh sân, đứng nghiêm gật đầu 2 cái rồi bước vào bóng chiều." },
            new BoRiaResidentFarewellSpec { OrderIndex = 11, ResidentId = "ConHeoChanTrang", ResidentName = "Con heo chân trắng", FinalConversationSummary = "Con heo chân trắng dụi mũi vào lòng bàn tay đang ngửa lên của người chơi rồi nằm xuống thảm cỏ vàng yên bình." }
        };

        public static bool ValidateSirphinaDialogueRule(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count > 3)
                return false;

            for (int i = 0; i < lines.Count; i++)
            {
                string raw = (lines[i] ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(raw))
                    continue;

                string[] words = raw.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 3)
                    return false;
            }
            return true;
        }

        public static (bool valid, string explanation) VerifyAncientSealMathematics()
        {
            bool mathOk = AncientSealBabylonian24 * AncientSealYearsPerUnit == AncientSealTotalYears;
            bool countsOk = FamilyTimelineTable14.Count == 8 &&
                            FamilyTimelineTable1B_AnRoute.Count == 8 &&
                            DualProtagonistComparisonTable27.Count == 9 &&
                            PrologueP01ToP08.Count == 8 &&
                            MainQuestsQ01ToQ20.Count == 20 &&
                            SideQuestsS01ToS12.Count == 12 &&
                            SixLinesOfMacKhaiOnGreatAncientTree.Count == 6 &&
                            FourMembersOfKimOThanhHoi.Count == 4 &&
                            ValidateSirphinaDialogueRule(SirphinaCanonicalSpokenLines) &&
                            ElevenBoRiaResidentFarewells.Count == 11;

            bool anBlackoutInvariantsOk = true;
            for (int i = 0; i < AnPinkCoverNotebookEntries.Count; i++)
            {
                if (!AnPinkCoverNotebookEntries[i].ValidateBlackoutInvariant())
                {
                    anBlackoutInvariantsOk = false;
                    break;
                }
            }

            string msg =
                $"Đại Giới Lưu (Minh Ước): {AncientSealBabylonian24} (chữ hình nêm Babylon) × {AncientSealYearsPerUnit} năm = {AncientSealTotalYears} năm. " +
                $"Timeline Khoa={FamilyTimelineTable14.Count}, Timeline An={FamilyTimelineTable1B_AnRoute.Count}, Đối chiếu 2.7={DualProtagonistComparisonTable27.Count}, " +
                $"P01-P08={PrologueP01ToP08.Count}, Q01-Q20={MainQuestsQ01ToQ20.Count}, S01-S12={SideQuestsS01ToS12.Count}, " +
                $"Mặc Khải={SixLinesOfMacKhaiOnGreatAncientTree.Count} câu, Kim Ô Thánh Hội={FourMembersOfKimOThanhHoi.Count} thành viên, " +
                $"Lời chào Bờ Rìa={ElevenBoRiaResidentFarewells.Count} cư dân.";
            return (mathOk && countsOk && anBlackoutInvariantsOk, msg);
        }

        /// <summary>
        /// Thực thi hồi kết chính tắc Chương 8 (Mùa 8 — "Hoàng Hôn Trên Đồng Cỏ" &amp; "Lời chào cuối cùng" — Phần 12.1–12.20):
        /// 1. Người chơi (Khoa hoặc An) đứng trước Đại Cổ Thụ đọc 6 câu Mặc Khải bằng Khắc Ngữ.
        /// 2. Người chơi tự nguyện đưa hai bàn tay ngửa lên; Sirphina (thành viên cuối cùng của Kim Ô Thánh Hội)
        ///    tan biến toàn bộ sinh mệnh vào thế giới và cơ thể người chơi để dung hợp mảnh hồn hiện đại với mảnh hồn cổ đại,
        ///    đồng thời để lại sợi liên kết giữ người chơi ở lại thế giới chính.
        /// 3. Hai thế giới tách rời vĩnh viễn, Huyền Y rút khỏi thế giới chính, ảo ảnh Bờ Rìa sụp đổ từ xa vào gần.
        /// 4. Thực hiện "Lời chào cuối cùng" với 11 cư dân &amp; sinh vật Bờ Rìa.
        /// 5. Trở về thành phố ăn tối như một con người hoàn chỉnh, kèm cảnh tùy chọn sau credit "Ba năm sau" (12.14).
        /// </summary>
        public static SeasonEightCanonicalFinaleResult ExecuteCanonicalSeasonEightFinale(
            PlayerId protagonist,
            bool voluntaryBothPalmsUp = true)
        {
            if (!voluntaryBothPalmsUp)
            {
                return new SeasonEightCanonicalFinaleResult
                {
                    Completed = false,
                    Protagonist = protagonist,
                    VoluntaryBothPalmsUp = false
                };
            }

            var residentLines = new List<string>(ElevenBoRiaResidentFarewells.Count);
            for (int i = 0; i < ElevenBoRiaResidentFarewells.Count; i++)
            {
                var r = ElevenBoRiaResidentFarewells[i];
                residentLines.Add($"{r.ResidentName}: {r.FinalConversationSummary}");
            }

            string name = protagonist == PlayerId.An ? "An" : "Khoa";
            string epilogue =
                $"[CHƯƠNG 8 · HOÀNG HÔN TRÊN ĐỒNG CỎ & LỜI CHÀO CUỐI CÙNG] Trước Đại Cổ Thụ (Thánh Điển) khắc 6 câu Mặc Khải bằng Khắc Ngữ, {name} tự nguyện đưa hai bàn tay ngửa lên. " +
                "Sirphina — tinh linh đồng cỏ không thuộc tính cuối cùng của Kim Ô Thánh Hội — cúi đầu không nói lời nào ở câu thứ ba rồi tan biến toàn bộ sinh mệnh vào thế giới và cơ thể " +
                $"{name}, dung hợp mảnh hồn hiện đại với mảnh hồn cổ đại tại điểm neo Đại Giới Lưu (Minh Ước) và để lại sợi liên kết giữ {name} ở lại thế giới chính. " +
                $"Hai thế giới tách rời vĩnh viễn, Huyền Y rút khỏi thế giới chính, và Bờ Rìa khép lại từ xa vào gần sau khi {name} nói lời chào cuối cùng với 11 cư dân Bờ Rìa. " +
                $"Trở về căn nhà cũ ở thành phố, trên bậu cửa sổ chiếu nghỉ nụ hoa sứ thứ 7 đã nở trắng muốt; {name} lấy chiếc bát nằm ngửa trên giá xuống, múc một bát canh nóng: " +
                "\"Dù tôi từng là gì, hôm nay tôi vẫn phải ăn tối.\"";

            string threeYearsLater =
                $"[SAU CREDIT · BA NĂM SAU (MỤC 12.14)] Ba năm sau, {name} đi chuyến xe tỉnh lộ ghé lại mảnh đất trống từng là Bờ Rìa. " +
                "Không còn thị trấn, không còn chuồng trại hay sương mù — chỉ còn một đồi cỏ dại bình thường ngập nắng chiều. " +
                $"Đứng trước khoảng cỏ từng là cổng rào gỗ, bàn tay {name} vô thức đưa lên như đỡ một cái chốt cổng không còn ở đó nữa, rồi khẽ mỉm cười: \"Đã về.\"";

            return new SeasonEightCanonicalFinaleResult
            {
                Completed = true,
                Protagonist = protagonist,
                VoluntaryBothPalmsUp = true,
                SirphinaThreePhrasesRuleVerified = ValidateSirphinaDialogueRule(SirphinaCanonicalSpokenLines),
                ModernAndAncientSoulFused = true,
                TwoWorldsPermanentlySeparated = true,
                HuyenYWithdrawnFromMainWorld = true,
                SixLinesOfMacKhai = SixLinesOfMacKhaiOnGreatAncientTree,
                ElevenBoRiaResidentFarewells = residentLines,
                CityReturnEpilogue = epilogue,
                ThreeYearsLaterAfterCredit = threeYearsLater
            };
        }

        /// <summary>
        /// Giữ tương thích ngược với các trình gọi cũ trong khi kết nối trực tiếp tới hồi kết Chương 8 chính tắc.
        /// </summary>
        public static (bool completed, string sundialInscription, List<string> sixFamilyFarewells, string cityReturnEpilogue) ExecuteChapterEightFinalGreeting(
            bool placeSevenNotchedNecklaceOnSundial,
            bool returnClockNeedleToSirphina,
            PlayerId protagonist = PlayerId.Khoa)
        {
            if (!placeSevenNotchedNecklaceOnSundial || !returnClockNeedleToSirphina)
            {
                return (false, string.Empty, new List<string>(), "Cần hoàn tất nghi thức tại điểm neo và tự nguyện đưa hai bàn tay ngửa lên trước Sirphina.");
            }

            var timeline = GetFamilyTimelineByProtagonist(protagonist);
            var farewells = new List<string>(6);
            for (int i = 0; i < timeline.Count; i++)
            {
                var rec = timeline[i];
                if (rec.OrderIndex != 6 && rec.OrderIndex != 8)
                {
                    farewells.Add($"{rec.MemberName}: {rec.FarewellActionInChapter8}");
                }
            }

            var canonicalFinale = ExecuteCanonicalSeasonEightFinale(protagonist, voluntaryBothPalmsUp: true);
            string label = protagonist == PlayerId.An
                ? "AN (Hai nửa linh hồn hợp làm một — sợi liên kết của Sirphina giữ An ở lại thế giới chính)"
                : "KHOA (Hai nửa linh hồn hợp làm một — sợi liên kết của Sirphina giữ Khoa ở lại thế giới chính)";

            return (canonicalFinale.Completed, label, farewells, canonicalFinale.CityReturnEpilogue);
        }

        // =====================================================================
        // E. BẢNG 1.5-D & CHƯƠNG 14–17 GDD v6.0: ĐẦY ĐỦ 31 CÔNG TRÌNH CHÍNH TẮC
        // =====================================================================
        public static readonly IReadOnlyList<GddBuildingTableSpec> ThirtyOneBuildingsTable15D = new List<GddBuildingTableSpec>
        {
            // Thông tin — Trinh thám đợt quái (4) — GDD 14.1
            new GddBuildingTableSpec { Number = 1, BlueprintId = "ThapCanh", CatalogId = "pig.building.thap_canh_quan_sat", DisplayName = "Tháp Canh Quan Sát", Category = BuildingCategory.ThongTin_TrinhTham, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 900, RangeMeters = 40f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 4 },
            new GddBuildingTableSpec { Number = 2, BlueprintId = "RadarUyHiep", CatalogId = "pig.building.radar_uy_hiep", DisplayName = "Radar Uy Hiếp", Category = BuildingCategory.ThongTin_TrinhTham, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 3200, RangeMeters = 55f, PowerContinuous = 4, PowerPeak = 8, MaxPerFarm = 2 },
            new GddBuildingTableSpec { Number = 3, BlueprintId = "MayCongHuongHuyenBi", CatalogId = "pig.building.may_cong_huong_huyen_bi", DisplayName = "Máy Cộng Hưởng Huyền Bí", Category = BuildingCategory.ThongTin_TrinhTham, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 6800, RangeMeters = 60f, PowerContinuous = 8, PowerPeak = 14, MaxPerFarm = 1 },
            new GddBuildingTableSpec { Number = 4, BlueprintId = "ThienNhanVongDai", CatalogId = "pig.building.thien_nhan_vong_dai", DisplayName = "Bạch Vân · Thiên Nhãn Vọng Đài", Category = BuildingCategory.ThongTin_TrinhTham, FootprintWidthMeters = 6, FootprintHeightMeters = 6, BuildCostGold = 14000, RangeMeters = 120f, PowerContinuous = 12, PowerPeak = 20, MaxPerFarm = 1, IsBachVan = true },

            // Thông tin — Hàng ngày (4) — GDD 14.2
            new GddBuildingTableSpec { Number = 5, BlueprintId = "TramKhiTuong", CatalogId = "pig.building.tram_khi_tuong", DisplayName = "Trạm Khí Tượng", Category = BuildingCategory.ThongTin_HangNgay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 800, RangeMeters = 0f, PowerContinuous = 2, PowerPeak = 2, MaxPerFarm = 1 },
            new GddBuildingTableSpec { Number = 6, BlueprintId = "TramTiepSongCho", CatalogId = "pig.building.tram_tiep_song_cho", DisplayName = "Trạm Tiếp Sóng Chợ", Category = BuildingCategory.ThongTin_HangNgay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 1800, RangeMeters = 0f, PowerContinuous = 3, PowerPeak = 4, MaxPerFarm = 1 },
            new GddBuildingTableSpec { Number = 7, BlueprintId = "BanTinhBao", CatalogId = "pig.building.ban_tinh_bao", DisplayName = "Bàn Tình Báo", Category = BuildingCategory.ThongTin_HangNgay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 2500, RangeMeters = 0f, PowerContinuous = 2, PowerPeak = 2, MaxPerFarm = 1 },
            new GddBuildingTableSpec { Number = 8, BlueprintId = "DaiDoDiTuong", CatalogId = "pig.building.dai_do_di_tuong", DisplayName = "Đài Dò Dị Tượng", Category = BuildingCategory.ThongTin_HangNgay, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 5200, RangeMeters = 60f, PowerContinuous = 6, PowerPeak = 10, MaxPerFarm = 1 },

            // Hỗ trợ (6) — GDD 15
            new GddBuildingTableSpec { Number = 9, BlueprintId = "TramHoiSuc", CatalogId = "pig.building.tram_hoi_suc", DisplayName = "Trạm Hồi Sức", Category = BuildingCategory.HoTro, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 1500, RangeMeters = 10f, PowerContinuous = 4, PowerPeak = 6, MaxPerFarm = 2 },
            new GddBuildingTableSpec { Number = 10, BlueprintId = "DenPhaCongNghiep", CatalogId = "pig.building.den_pha_cong_nghiep", DisplayName = "Đèn Pha Công Nghiệp", Category = BuildingCategory.HoTro, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 600, RangeMeters = 25f, PowerContinuous = 4, PowerPeak = 5, MaxPerFarm = 6 },
            new GddBuildingTableSpec { Number = 11, BlueprintId = "MayPhatDien", CatalogId = "pig.building.may_phat_dien", DisplayName = "Máy Phát Điện", Category = BuildingCategory.HoTro, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 2200, RangeMeters = 12f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 2 },
            new GddBuildingTableSpec { Number = 12, BlueprintId = "BomLamMat", CatalogId = "pig.building.bom_lam_mat", DisplayName = "Bơm Làm Mát", Category = BuildingCategory.HoTro, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 3500, RangeMeters = 15f, PowerContinuous = 5, PowerPeak = 6, MaxPerFarm = 3 },
            new GddBuildingTableSpec { Number = 13, BlueprintId = "CotThuLoi", CatalogId = "pig.building.cot_thu_loi", DisplayName = "Cột Thu Lôi", Category = BuildingCategory.HoTro, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 1200, RangeMeters = 20f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 4 },
            new GddBuildingTableSpec { Number = 14, BlueprintId = "VanLinhHoiNguyenTran", CatalogId = "pig.building.van_linh_hoi_nguyen_tran", DisplayName = "Bạch Vân · Vạn Linh Hồi Nguyên Trận", Category = BuildingCategory.HoTro, FootprintWidthMeters = 6, FootprintHeightMeters = 6, BuildCostGold = 16000, RangeMeters = 30f, PowerContinuous = 15, PowerPeak = 25, MaxPerFarm = 1, IsBachVan = true },

            // Tấn công (5) — GDD 16.1
            new GddBuildingTableSpec { Number = 15, BlueprintId = "NoXuyenVan", CatalogId = "pig.building.no_xuyen_van", DisplayName = "Nỏ Xuyên Vân", Category = BuildingCategory.TanCong, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 2400, RangeMeters = 22f, PowerContinuous = 4, PowerPeak = 6, MaxPerFarm = 4 },
            new GddBuildingTableSpec { Number = 16, BlueprintId = "ThapHoQuang", CatalogId = "pig.building.thap_ho_quang", DisplayName = "Tháp Hồ Quang", Category = BuildingCategory.TanCong, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 4800, RangeMeters = 18f, PowerContinuous = 10, PowerPeak = 18, MaxPerFarm = 3 },
            new GddBuildingTableSpec { Number = 17, BlueprintId = "ThapPhongKhongSkyhook", CatalogId = "pig.building.thap_phong_khong_skyhook", DisplayName = "Tháp Phòng Không Skyhook", Category = BuildingCategory.TanCong, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 7500, RangeMeters = 35f, PowerContinuous = 12, PowerPeak = 22, MaxPerFarm = 3 },
            new GddBuildingTableSpec { Number = 18, BlueprintId = "DaiThienLoi", CatalogId = "pig.building.dai_thien_loi", DisplayName = "Đài Thiên Lôi", Category = BuildingCategory.TanCong, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 26000, RangeMeters = 30f, PowerContinuous = 16, PowerPeak = 30, MaxPerFarm = 1 },
            new GddBuildingTableSpec { Number = 19, BlueprintId = "CuuTieuLoiPhao", CatalogId = "pig.building.cuu_tieu_loi_phao", DisplayName = "Bạch Vân · Cửu Tiêu Lôi Pháo", Category = BuildingCategory.TanCong, FootprintWidthMeters = 6, FootprintHeightMeters = 6, BuildCostGold = 18000, RangeMeters = 60f, PowerContinuous = 20, PowerPeak = 45, MaxPerFarm = 1, IsBachVan = true },

            // Phòng thủ (4) — GDD 17.1
            new GddBuildingTableSpec { Number = 20, BlueprintId = "RaoGo", CatalogId = "pig.building.hang_rao", DisplayName = "Hàng Rào (Gỗ / Đá / Thép)", Category = BuildingCategory.PhongThu, FootprintWidthMeters = 2, FootprintHeightMeters = 1, BuildCostGold = 150, RangeMeters = 0f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 80 },
            new GddBuildingTableSpec { Number = 21, BlueprintId = "CongGiaCo", CatalogId = "pig.building.cong_gia_co", DisplayName = "Cổng Gia Cố", Category = BuildingCategory.PhongThu, FootprintWidthMeters = 4, FootprintHeightMeters = 2, BuildCostGold = 1200, RangeMeters = 0f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 4 },
            new GddBuildingTableSpec { Number = 22, BlueprintId = "MayChieuKhien", CatalogId = "pig.building.may_chieu_khien", DisplayName = "Máy Chiếu Khiên", Category = BuildingCategory.PhongThu, FootprintWidthMeters = 4, FootprintHeightMeters = 4, BuildCostGold = 6500, RangeMeters = 12f, PowerContinuous = 10, PowerPeak = 16, MaxPerFarm = 2 },
            new GddBuildingTableSpec { Number = 23, BlueprintId = "UBaoCat", CatalogId = "pig.building.u_bao_cat", DisplayName = "Ụ Bao Cát", Category = BuildingCategory.PhongThu, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 400, RangeMeters = 0f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 8 },

            // Bẫy (5) — GDD 17.2
            new GddBuildingTableSpec { Number = 24, BlueprintId = "BanChong", CatalogId = "pig.building.ban_chong", DisplayName = "Bàn Chông", Category = BuildingCategory.Bay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 350, RangeMeters = 2f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 12 },
            new GddBuildingTableSpec { Number = 25, BlueprintId = "TamSocDien", CatalogId = "pig.building.tam_soc_dien", DisplayName = "Tấm Sốc Điện", Category = BuildingCategory.Bay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 900, RangeMeters = 2.5f, PowerContinuous = 4, PowerPeak = 8, MaxPerFarm = 8 },
            new GddBuildingTableSpec { Number = 26, BlueprintId = "HoKeo", CatalogId = "pig.building.ho_keo", DisplayName = "Hố Keo", Category = BuildingCategory.Bay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 500, RangeMeters = 3f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 10 },
            new GddBuildingTableSpec { Number = 27, BlueprintId = "LuoiEmp", CatalogId = "pig.building.luoi_emp", DisplayName = "Lưới EMP", Category = BuildingCategory.Bay, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 1800, RangeMeters = 4f, PowerContinuous = 6, PowerPeak = 12, MaxPerFarm = 6 },
            new GddBuildingTableSpec { Number = 28, BlueprintId = "MinNo", CatalogId = "pig.building.min_no", DisplayName = "Mìn Nổ", Category = BuildingCategory.Bay, FootprintWidthMeters = 1, FootprintHeightMeters = 1, BuildCostGold = 600, RangeMeters = 4f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 10 },

            // Triển khai (3) — GDD 17.3
            new GddBuildingTableSpec { Number = 29, BlueprintId = "ChotCanhGac", CatalogId = "pig.building.chot_canh_gac", DisplayName = "Chốt Canh Gác", Category = BuildingCategory.TrienKhai, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 1400, RangeMeters = 18f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 4 },
            new GddBuildingTableSpec { Number = 30, BlueprintId = "CoTapKet", CatalogId = "pig.building.co_tap_ket", DisplayName = "Cờ Tập Kết", Category = BuildingCategory.TrienKhai, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 1000, RangeMeters = 15f, PowerContinuous = 0, PowerPeak = 0, MaxPerFarm = 2 },
            new GddBuildingTableSpec { Number = 31, BlueprintId = "DenHieuDanhChan", CatalogId = "pig.building.den_hieu_danh_chan", DisplayName = "Đèn Hiệu Đánh Chặn", Category = BuildingCategory.TrienKhai, FootprintWidthMeters = 2, FootprintHeightMeters = 2, BuildCostGold = 2800, RangeMeters = 20f, PowerContinuous = 4, PowerPeak = 6, MaxPerFarm = 2 }
        };

        // =====================================================================
        // F. BẢNG 5.4 GDD v6.0: BỐN KÊNH GIÁM ĐỊNH GEN CHÍNH TẮC
        //    - Chợ ngày — Quầy Sơ Định: Hiếm 70% (300G), Quý 40% (900G), 5 STA, 1 giờ; không mở Huyền Thoại / Dị Biến
        //    - Phòng Nghiên Cứu — Bàn Giải Mã: 95/85/65/50%, 700G+1 / 2000G+2 / 6500G+4 / 9000G+6, 25/30/40/45 STA, 18/24/36/48h
        //    - Chợ đêm — Quầy Giám Định Cấp Cao (12% số đêm): 90/72/50/35%, 450 / 1300 / 3500 / 5000G, 8 STA, 2 giờ
        //    - Bí Nhân — Sự kiện Giám Định Vô Danh: 100%, 0 Vàng (đổi vật phẩm/mẫu hiếm), 10 STA, tức thì
        // =====================================================================
        public static (float successRate, int goldCost, int reagentCount, float staminaCost, int processingHours) GetCanonicalAppraisalChannelSpecGdd54(
            AppraisalChannel channel,
            GeneRarity rarity)
        {
            return channel switch
            {
                AppraisalChannel.ChoNgay => rarity switch
                {
                    GeneRarity.Hiem => (0.70f, 300, 0, 5f, 1),
                    GeneRarity.Quy => (0.40f, 900, 0, 5f, 1),
                    _ => (0.00f, 0, 0, 5f, 1)
                },

                AppraisalChannel.PhongNghienCuu => rarity switch
                {
                    GeneRarity.Hiem => (0.95f, 700, 1, 25f, 18),
                    GeneRarity.Quy => (0.85f, 2000, 2, 30f, 24),
                    GeneRarity.HuyenThoai => (0.65f, 6500, 4, 40f, 36),
                    GeneRarity.DiBien => (0.50f, 9000, 6, 45f, 48),
                    _ => (1.00f, 300, 1, 15f, 6)
                },

                AppraisalChannel.ChoDemGiamDinhCapCao => rarity switch
                {
                    GeneRarity.Hiem => (0.90f, 450, 0, 8f, 2),
                    GeneRarity.Quy => (0.72f, 1300, 0, 8f, 2),
                    GeneRarity.HuyenThoai => (0.50f, 3500, 0, 8f, 2),
                    GeneRarity.DiBien => (0.35f, 5000, 0, 8f, 2),
                    _ => (1.00f, 250, 0, 8f, 2)
                },

                AppraisalChannel.BiNhanHoSoGiongLoai => (1.00f, 0, 0, 10f, 0),
                _ => (0.00f, 0, 0, 0f, 0)
            };
        }

        // =====================================================================
        // G. MỤC 10.2, 10.4, 10.5 & 10.8 GDD v6.0:
        //    - Cổng Giữa Game (>= 8 đợt lớn + 1/5 điều kiện phụ:
        //      ① Cấp trại >= 7, ② Từng có đàn >= 120 con, ③ Đã định danh >= 1 dòng Hiếm trở lên,
        //      ④ Đã hạ 1 Boss chu kỳ, ⑤ Đang sở hữu >= 12 công trình phòng thủ còn hoạt động)
        //    - Quy tắc trần 50% tiến độ khóa trước khi lộ diện (GDD 10.4)
        //    - Tỷ lệ mở khóa theo bậc giấy Lam/Vàng/Tím/Đỏ +0..+3 (GDD 10.5)
        //    - 6 Khóa & 3 Hiệu ứng phi chiến đấu chuẩn của Khoa ("Trại Chủ Lão Luyện") và An ("Người Đọc Dòng") (GDD 10.8)
        // =====================================================================
        public static (bool unlocked, int subConditionsMetCount, List<string> metSubConditions) EvaluateCanonicalMidGameGateGdd102(
            int majorWavesSurvived,
            int farmLevel,
            int maxHerdSizeEverReached,
            int rareOrHigherGenesIdentifiedCount,
            int cycleBossesDefeated,
            int activeDefenseBuildingsCount)
        {
            var met = new List<string>();
            if (farmLevel >= 7) met.Add("① Cấp trại >= 7");
            if (maxHerdSizeEverReached >= 120) met.Add("② Từng có đàn >= 120 con");
            if (rareOrHigherGenesIdentifiedCount >= 1) met.Add("③ Đã định danh >= 1 dòng Hiếm trở lên");
            if (cycleBossesDefeated >= 1) met.Add("④ Đã hạ >= 1 Boss chu kỳ");
            if (activeDefenseBuildingsCount >= 12) met.Add("⑤ Đang sở hữu >= 12 công trình phòng thủ còn hoạt động");

            bool gateOpen = majorWavesSurvived >= 8 && met.Count >= 1;
            return (gateOpen, met.Count, met);
        }

        public static (bool unlocked, int subConditionsMetCount, List<string> metSubConditions) EvaluateCanonicalMidGameGateGdd102(
            int majorWavesSurvived,
            bool herdSoKhaiThreeConsecutiveDays,
            bool identifiedLegendaryOrDeviantGene,
            int constructionHallLevel,
            int cycleBossesDefeated,
            long totalPigSalesGoldRevenue)
        {
            return EvaluateCanonicalMidGameGateGdd102(
                majorWavesSurvived: majorWavesSurvived,
                farmLevel: constructionHallLevel >= 3 ? 7 : 1,
                maxHerdSizeEverReached: herdSoKhaiThreeConsecutiveDays ? 120 : 0,
                rareOrHigherGenesIdentifiedCount: identifiedLegendaryOrDeviantGene ? 1 : 0,
                cycleBossesDefeated: cycleBossesDefeated,
                activeDefenseBuildingsCount: totalPigSalesGoldRevenue >= 25000L ? 12 : 0);
        }

        /// <summary>
        /// GDD 10.4: Trước khi qua Cổng Giữa Game, tiến độ ngầm của mỗi trong 6 khóa được tính tối đa 50% yêu cầu của khóa đó.
        /// </summary>
        public static int ClampSpecialtyLockProgressBeforeRevealGdd104(int currentProgress, int requiredTotal, bool midGameGateRevealed)
        {
            if (requiredTotal <= 0) return 0;
            if (!midGameGateRevealed)
            {
                int preRevealCap = requiredTotal / 2;
                return Math.Clamp(currentProgress, 0, preRevealCap);
            }
            return Math.Clamp(currentProgress, 0, requiredTotal);
        }

        /// <summary>
        /// GDD 10.5: Bảng tỷ lệ mở khóa Đặc tính Chuyên biệt theo màu giấy (Lam / Vàng / Tím / Đỏ) và cấp gia cường (+0..+3).
        /// </summary>
        public static float GetSpecialtyPaperOpenRateGdd105(SpecialtyPaperColor color, int enhancementLevel)
        {
            int lvl = Math.Clamp(enhancementLevel, 0, 3);
            return color switch
            {
                SpecialtyPaperColor.Lam => lvl switch { 0 => 0.15f, 1 => 0.30f, 2 => 0.48f, _ => 0.72f },
                SpecialtyPaperColor.Vang => lvl switch { 0 => 0.28f, 1 => 0.45f, 2 => 0.65f, _ => 0.85f },
                SpecialtyPaperColor.Tim => lvl switch { 0 => 0.42f, 1 => 0.60f, 2 => 0.78f, _ => 0.95f },
                SpecialtyPaperColor.Do => lvl switch { 0 => 0.65f, 1 => 0.80f, 2 => 0.92f, _ => 1.00f },
                _ => 0.15f
            };
        }

        public static readonly IReadOnlyList<SpecialtyLockEffectSpec> KhoaAndAnTwelveSpecialtyLocksGdd108 = new List<SpecialtyLockEffectSpec>
        {
            // KHOA — "Trại Chủ Lão Luyện" (6 khóa chuẩn GDD 10.8 — chỉ hỗ trợ việc hằng ngày, không tăng sức chiến đấu)
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 1, UnlockConditionSummary = "Vượt qua 10 đợt quái lớn mà không để Nhà Chính hỏng", EffectSummary = "Mở tiến độ [Làm Dứt Điểm (1/2)]: Sửa chữa công trình phòng thủ tốn -20% Vàng, -20% thời gian.", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 2, UnlockConditionSummary = "Tự tay dọn vệ sinh 150 lần", EffectSummary = "Hoàn chỉnh [Làm Dứt Điểm (2/2)]: Mọi thao tác chăm heo của Khoa tốn -2 Thể Lực (tối thiểu 2).", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 3, UnlockConditionSummary = "Giữ trạng thái Bầy Đàn (tối thiểu Sơ Khai) 10 ngày liên tục", EffectSummary = "Mở tiến độ [Người Của Đàn (1/2)]: Mỗi ngày có 1 lần Huýt Sáo miễn phí giúp toàn bộ heo trong 30m +10 Tinh thần và xóa trạng thái Bất an.", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 4, UnlockConditionSummary = "Định danh cả 2 dòng Quý (Lôi Mạch, Huyết Vương)", EffectSummary = "Hoàn chỉnh [Người Của Đàn (2/2)]: Heo Thủ Lĩnh tăng thêm +1%/ngày tỷ lệ kế vị khi Khoa trực tiếp Bồi Dưỡng Kế Vị.", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 5, UnlockConditionSummary = "Bán lũy kế 500 heo qua đường kiểm dịch ở chợ ngày", EffectSummary = "Mở tiến độ [Ngủ Say (1/2)]: Ngủ chuẩn tại Nhà Chính hồi thêm +4 Thể trạng (tổng +10).", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.Khoa, TrueSpecialtyTitle = "Trại Chủ Lão Luyện", LockNumber = 6, UnlockConditionSummary = "Có ít nhất 1 lần kế vị Thủ Lĩnh kèm Truyền Thừa", EffectSummary = "Mở khóa hoàn chỉnh [Trại Chủ Lão Luyện — Ngủ Say (2/2)]: Giảm 1 giờ hồi chờ ngủ (từ 6h xuống 5h).", IsStrictlyNonCombatDailySupport = true },

            // AN — "Người Đọc Dòng" (6 khóa chuẩn GDD 10.8 — chỉ hỗ trợ việc hằng ngày, không tăng sức chiến đấu)
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 1, UnlockConditionSummary = "Vượt qua 10 đợt quái lớn", EffectSummary = "Mở tiến độ [Xem Trước Chợ (1/2)]: Thấy trước chủ đề chu kỳ thị trường kế tiếp trước 2 ngày.", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 2, UnlockConditionSummary = "Xây Bàn Tình Báo và sử dụng 40 lượt hỏi thông tin", EffectSummary = "Hoàn chỉnh [Xem Trước Chợ (2/2)]: Thấy trước tối đa 2 mặt hàng đặc biệt của chợ đêm tối đó từ 12:00 trưa.", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 3, UnlockConditionSummary = "Từng có đàn >= 150 con cùng lúc, trong đó >= 20% là đời F2", EffectSummary = "Mở tiến độ [Giữ Chỗ (1/2)]: Giữ 1 món ở chợ đêm sang đêm hôm sau mà không mất phí đặt cọc.", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 4, UnlockConditionSummary = "Định danh tối thiểu 6 dòng gen khác nhau, có >= 1 dòng Huyền Thoại hoặc Dị Biến", EffectSummary = "Hoàn chỉnh [Giữ Chỗ (2/2)]: Mở khóa khả năng giữ đồng thời tối đa 2 món tại chợ đêm.", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 5, UnlockConditionSummary = "Hoàn thành 30 giao dịch trao đổi vật phẩm tại chợ đêm", EffectSummary = "Mở tiến độ [Mặc Cả (1/2)]: Giảm chi phí quy đổi trong các giao dịch trao đổi vật phẩm.", IsStrictlyNonCombatDailySupport = true },
            new SpecialtyLockEffectSpec { CharacterIdentity = PlayerId.An, TrueSpecialtyTitle = "Người Đọc Dòng", LockNumber = 6, UnlockConditionSummary = "Hoàn thành 3 sự kiện huyền bí trong khi có thông tin tình báo cấp 5", EffectSummary = "Mở khóa hoàn chỉnh [Người Đọc Dòng — Mặc Cả (2/2)]: Khi trao đổi với Bí Nhân hoặc thương nhân đặc biệt, tổng giá trị vật phẩm cần đưa giảm còn 90% định mức.", IsStrictlyNonCombatDailySupport = true }
        };
    }
}
