using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum StoryChapterId
    {
        Ch0_GiayChungNhan = 0,
        Ch1_Chon = 1,
        Ch2_HaiPhut = 2,
        Ch3_ChoTrong = 3,
        Ch4_BuaToiChuaCoKhach = 4
    }

    public enum StoryEndingId
    {
        ChuaChon = 0,
        Ending_8_1_GiuTraiThaoDuongGui = 1,
        Ending_8_2_GiuMayLamHoSo = 2,
        Ending_8_3_MangVongVeNha = 3
    }

    [Serializable]
    public class NotebookTwoColumnEntry
    {
        public string Id = string.Empty;
        public string Title = string.Empty;
        public string LeftColumn_Thay = string.Empty;       // Cột trái: "Thấy"
        public string RightColumn_CoThat = string.Empty;    // Cột phải: "Có thật" (chỉ ghi khi có >= 2 chứng cứ)
        public string PencilMargin_SuyDoan = string.Empty;  // Lề bút chì: suy đoán cá nhân của Khoa
        public bool IsVerifiedToRightColumn;
        public int EvidenceCount;
    }

    [Serializable]
    public class StoryQuestRecord
    {
        public string QuestId = string.Empty;   // P01..P08, Q01..Q20, S01..S12
        public string Title = string.Empty;
        public StoryChapterId Chapter;
        public bool IsSideQuest;
        public bool IsUnlocked;
        public bool IsCompleted;
        public string ObjectiveSummary = string.Empty;
        public string KhoaDeadpanNote = string.Empty;
    }

    /// <summary>
    /// Hệ thống Cốt Truyện Mùa 1 "Sáu Với Một" & Sổ Hai Cột của Khoa (Docs/COT_TRUYEN_PIG_TYCOON.md).
    /// Thuần C# Core, không phụ thuộc UnityEngine.
    /// </summary>
    [Serializable]
    public class StoryJournalSystem
    {
        public PlayerId ActiveProtagonist { get; set; } = PlayerId.Khoa;
        public string YoungestSiblingName => ActiveProtagonist == PlayerId.An ? "Em trai Bé Khoa" : "Em gái Bé An";
        public StoryChapterId CurrentChapter { get; set; } = StoryChapterId.Ch0_GiayChungNhan;
        public StoryEndingId ChosenEnding { get; set; } = StoryEndingId.ChuaChon;
        public bool PostCreditTriggered { get; set; } = false;
        public bool SeventhChannelCut { get; set; } = false;
        public bool HerdEmptySpotObserved { get; set; } = false;
        public bool WhiteFootedPigPrankSeen { get; set; } = false;
        public int ClockOffsetMinutes { get; set; } = 2; // Đồng hồ nhà chính luôn nhanh 2 phút (07:02 vs 07:00)

        public List<NotebookTwoColumnEntry> NotebookEntries { get; set; } = new List<NotebookTwoColumnEntry>();
        public List<AnLetterNotebookEntry> AnPinkNotebookEntries { get; set; } = new List<AnLetterNotebookEntry>();
        public List<StoryQuestRecord> Quests { get; set; } = new List<StoryQuestRecord>();
        public Dictionary<string, int> NpcTrustTier { get; set; } = new Dictionary<string, int>();

        public StoryJournalSystem()
        {
            InitNpcTrust();
            InitNotebookAndQuests();
            SyncAnPinkNotebook();
        }

        public void SwitchProtagonistPerspective(PlayerId protagonist)
        {
            ActiveProtagonist = protagonist;
            SyncAnPinkNotebook();
        }

        private void SyncAnPinkNotebook()
        {
            AnPinkNotebookEntries.Clear();
            foreach (var item in CanonicalSagaAndGdd6Systems.AnPinkCoverNotebookEntries)
            {
                AnPinkNotebookEntries.Add(new AnLetterNotebookEntry
                {
                    EntryId = item.EntryId,
                    QuestOrSceneRef = item.QuestOrSceneRef,
                    RecipientSalutation = item.RecipientSalutation,
                    Sticker = item.Sticker,
                    LetterBody = item.LetterBody,
                    PencilMarginHypothesis = item.PencilMarginHypothesis,
                    EndsWithSmiley = item.EndsWithSmiley,
                    IsUnconsciousBlackoutEntry = item.IsUnconsciousBlackoutEntry
                });
            }
        }

        private void InitNpcTrust()
        {
            NpcTrustTier["BaSau"] = 1;
            NpcTrustTier["Man"] = 1;
            NpcTrustTier["TamVa"] = 1;
            NpcTrustTier["OngTon"] = 1;
            NpcTrustTier["BacSiDung"] = 1;
            NpcTrustTier["Cuc"] = 1;
            NpcTrustTier["Le"] = 1;
            NpcTrustTier["AnhCa"] = 1;
            NpcTrustTier["GiamDocNgong"] = 2;
        }

        private void InitNotebookAndQuests()
        {
            NotebookEntries.Clear();
            NotebookEntries.Add(new NotebookTwoColumnEntry
            {
                Id = "nb_01_chu_ky_bay_net",
                Title = "Chữ ký 7 nét & Quy tắc 'Sáu với một'",
                LeftColumn_Thay = "Giấy chứng nhận thừa kế có chữ ký 7 nét. Người ở Bờ Rìa không ai nói số 7, luôn đếm là 'sáu với một'.",
                RightColumn_CoThat = "Đã xác nhận: Ông Tồn ở chợ và Bà Sáu đều tránh số 7 vì 'gọi số thứ 7 là gọi kiện hàng cuối quay lại'.",
                PencilMargin_SuyDoan = "Chú Minh thuận tay trái. Nhưng nét gạch thứ 7 trên giấy lại kéo từ phải sang.",
                IsVerifiedToRightColumn = true,
                EvidenceCount = 2
            });
            NotebookEntries.Add(new NotebookTwoColumnEntry
            {
                Id = "nb_02_lech_hai_phut",
                Title = "Đồng hồ chạy nhanh 2 phút (07:02)",
                LeftColumn_Thay = "Đồng hồ treo tường nhà chính luôn nhanh hơn đài phát 2 phút. Hóa đơn chợ ghi giờ nhận trước giờ gửi 2 phút.",
                RightColumn_CoThat = "",
                PencilMargin_SuyDoan = "Không phải đồng hồ hỏng. Đường truyền dưới giếng cạn làm trễ pha thời gian 120 giây.",
                IsVerifiedToRightColumn = false,
                EvidenceCount = 1
            });
            NotebookEntries.Add(new NotebookTwoColumnEntry
            {
                Id = "nb_03_vong_co_so_19",
                Title = "Vòng cổ đồng số 19 & Dấu nêm 24",
                LeftColumn_Thay = "Đào được vòng cổ đồng khắc số 19 dưới gốc sung dù trại mới nhận 4 con heo giống. Mảnh da khắc dấu nêm Babylon = 24.",
                RightColumn_CoThat = "",
                PencilMargin_SuyDoan = "Trại đã qua ít nhất 3 đợt bàn giao trước khi mình xuống xe đò.",
                IsVerifiedToRightColumn = false,
                EvidenceCount = 1
            });
            NotebookEntries.Add(new NotebookTwoColumnEntry
            {
                Id = "nb_04_hoc_tuong_bay_cot",
                Title = "Hốc tường 7 cột đo chiều cao (2019–2026)",
                LeftColumn_Thay = "Sau tủ áo có 7 cột vạch đo chiều cao kẻ bằng tay trái suốt 2019–2026, dù mình chưa từng về đây từ năm 2018.",
                RightColumn_CoThat = "",
                PencilMargin_SuyDoan = "Ai đó có cùng chiều cao với mình đã sống trong căn phòng này mỗi mùa mưa.",
                IsVerifiedToRightColumn = false,
                EvidenceCount = 1
            });
            NotebookEntries.Add(new NotebookTwoColumnEntry
            {
                Id = "nb_05_cho_trong_giua_dan",
                Title = "Chỗ trống đường kính 1,2m giữa đàn heo",
                LeftColumn_Thay = "Khi đàn heo vào trạng thái Bầy Đàn, chúng luôn chừa trống một vòng tròn ở góc Tây Bắc.",
                RightColumn_CoThat = "",
                PencilMargin_SuyDoan = "Trùng khít vị trí mình đứng cạnh chú Minh trong tấm ảnh gia đình năm 2018.",
                IsVerifiedToRightColumn = false,
                EvidenceCount = 1
            });

            Quests.Clear();
            // 8 bước Mở đầu (P01 - P08)
            string[] prologueTitles =
            {
                "P01 · Xe đò dừng trước biển gỗ Bờ Rìa",
                "P02 · Chìa khóa dây cước đỏ của Bà Sáu",
                "P03 · Gặp Giám Đốc (Ngỗng Báo Động) trên thùng phuy",
                "P04 · Ba cái bát úp và chiếc đồng hồ nhanh 2 phút",
                "P05 · Nhận 4 heo giống từ Mận (con út đốm trắng chân trái)",
                "P06 · Đổ cám & vá vết cào 3 vạch cách nhau 12 cm",
                "P07 · Tiếng quạt trầm dưới đáy giếng lúc 02:02",
                "P08 · Kẻ Sổ Hai Cột ('Thấy' | 'Có thật' | 'Lề bút chì')"
            };
            for (int i = 0; i < prologueTitles.Length; i++)
            {
                Quests.Add(new StoryQuestRecord
                {
                    QuestId = $"P0{i + 1}",
                    Title = prologueTitles[i],
                    Chapter = StoryChapterId.Ch0_GiayChungNhan,
                    IsSideQuest = false,
                    IsUnlocked = true,
                    IsCompleted = i < 7, // Hoàn thành P01-P07 khi bắt đầu vào trại, P08 sẵn sàng hoàn tất
                    ObjectiveSummary = "Khởi đầu tiếp nhận trại heo thừa kế của chú Minh tại thị trấn Bờ Rìa.",
                    KhoaDeadpanNote = "Bà Sáu bảo chìa khóa hơi rít, xoay sang trái nửa vòng rồi hãy đẩy."
                });
            }

            // 20 nhiệm vụ Chính tuyến (Q01 - Q20)
            (string id, StoryChapterId ch, string title, string summary, string note)[] mainQuests =
            {
                ("Q01", StoryChapterId.Ch1_Chon, "Q01 · Dấu răng trên máng gỗ", "Kiểm tra vết cào 12cm và mảnh vảy xám ở kho cám.", "Chồn bình thường không có sáu ngón và không để lại mùi ozone."),
                ("Q02", StoryChapterId.Ch1_Chon, "Q02 · Chợ không nói số bảy", "Bán heo ở Chợ Ngày và nghe Ông Tồn đếm 'sáu với một'.", "Ông Tồn tính tiền rất chuẩn, chỉ trừ việc tờ hóa đơn không bao giờ có con số 7."),
                ("Q03", StoryChapterId.Ch1_Chon, "Q03 · Vòng cổ đồng số 19", "Đào dưới gốc sung tìm vòng cổ đồng khắc số 19.", "Mận nói con số 19 đi lạc từ mùa mưa trước. Nhưng chuồng chỉ có 12 ô."),
                ("Q04", StoryChapterId.Ch1_Chon, "Q04 · Đợt gió đầu mùa", "Chống trả đợt quái nhỏ đầu tiên và giữ an toàn cho đàn heo.", "Tám Vá gọi thứ vừa lao vào rào là 'chồn đi theo gió'."),
                ("Q05", StoryChapterId.Ch2_HaiPhut, "Q05 · Biên lai nhận trước hai phút", "Đối chiếu giờ in hóa đơn (07:00) sớm hơn giờ xuất chuồng (07:02).", "Máy in nhiệt của Ông Tồn không nối mạng, nhưng vẫn biết trước trọng lượng đàn heo 2 phút."),
                ("Q06", StoryChapterId.Ch2_HaiPhut, "Q06 · Bác sĩ không hỏi tên", "Mời Bác sĩ Dung điều trị ca bệnh đầu tiên trong trại.", "Bà Dung mang ống nghe tim người để khám cho con heo già."),
                ("Q07", StoryChapterId.Ch2_HaiPhut, "Q07 · Mảnh da khắc dấu nêm Babylon (24)", "Thu thập mảnh da từ Bí Nhân ngoài rào 30m và giải mã ký hiệu 24.", "Hai vạch đứng, bốn góc nêm: con số 24 trùng với tổng số vạch trên tường."),
                ("Q08", StoryChapterId.Ch2_HaiPhut, "Q08 · Hốc tường bảy cột (2019–2026)", "Mở tấm ván sau tủ áo nhà chính, kiểm tra 7 cột đo chiều cao.", "Vết bút chì nghiêng về bên trái. Người tự đo chiều cao cũng thuận tay trái."),
                ("Q09", StoryChapterId.Ch2_HaiPhut, "Q09 · Giọng nói trong ống nước", "Nghe băng cassette của Tám Vá thu âm lúc 02:02 sáng.", "Tiếng quạt thông gió nằm sâu ít nhất 15 mét dưới nền chuồng chính."),
                ("Q10", StoryChapterId.Ch2_HaiPhut, "Q10 · Đợt vây có đèn xanh", "Đánh lui UFO Trinh Sát đầu tiên xuất hiện trong đêm.", "Người làng đóng cửa sổ từ 9 giờ tối và gọi chùm sáng kéo heo lên trời là 'gió lốc'."),
                ("Q11", StoryChapterId.Ch3_ChoTrong, "Q11 · Chỗ trống giữa đàn", "Đưa đàn heo đạt trạng thái Bầy Đàn và quan sát khoảng trống góc Tây Bắc.", "Cả đàn ngủ sát nhau nhưng chừa đúng một vòng tròn đường kính 1,2 mét."),
                ("Q12", StoryChapterId.Ch3_ChoTrong, "Q12 · Tấm ảnh năm 2018", "Ghép nửa tấm ảnh trong hộp trà của Bà Sáu với vị trí chỗ trống.", "Chỗ đàn heo chừa trống là chỗ mình từng đứng chụp ảnh năm tám tuổi."),
                ("Q13", StoryChapterId.Ch3_ChoTrong, "Q13 · Con heo nhớ tên người", "Quan sát Thủ Lĩnh đàn gõ mõm 7 nhịp vào cửa sau lúc 02:02.", "Nó gõ sáu nhịp liền nhau, nghỉ một nhịp, rồi gõ nhịp cuối."),
                ("Q14", StoryChapterId.Ch3_ChoTrong, "Q14 · Người thu hồi đeo găng trắng", "Đánh bại toán thu hồi mẫu ngoài ranh giới và lấy thẻ từ B2.", "Trên thẻ từ in tên trại là 'Trạm Đệm Sinh Học Bờ Rìa #07'."),
                ("Q15", StoryChapterId.Ch3_ChoTrong, "Q15 · Đường dây dưới giếng cạn", "Cùng Tám Vá mở nắp giếng cạn kiểm tra đường cáp đồng trục.", "Dây cáp vẫn ấm. Mỗi đêm nó gửi đi một gói dữ liệu sinh trắc lúc 02:02."),
                ("Q16", StoryChapterId.Ch4_BuaToiChuaCoKhach, "Q16 · Hồ sơ kiện hàng thứ bảy", "Đọc nhật ký của chú Minh về 6 kiện mẫu vật và kiện thứ 7 ('sáu với một').", "Sáu kiện đầu là giống heo kháng bức xạ. Kiện thứ bảy là người trông trại."),
                ("Q17", StoryChapterId.Ch4_BuaToiChuaCoKhach, "Q17 · Bữa tối bày ba bát", "Cùng Bà Sáu và Mận dọn bữa tối trước đêm Đại Tập Kích.", "Bà Sáu lật ngửa chiếc bát thứ ba, xới đầy cơm nóng dù chưa có ai ngồi."),
                ("Q18", StoryChapterId.Ch4_BuaToiChuaCoKhach, "Q18 · Đêm mây thấp đè chuồng", "Sống sót qua Đại Đợt Quái (Major Wave) và bảo vệ giếng cạn.", "Đèn pha công nghiệp chớp tắt theo nhịp 6-1 suốt trận vây."),
                ("Q19", StoryChapterId.Ch4_BuaToiChuaCoKhach, "Q19 · Kênh gửi kiện sáu với một", "Mở bảng điều khiển dưới giếng cạn khi đồng hồ điểm 02:02.", "Màn hình hiện trạng thái: 'Đang chờ đóng gói Kiện 6+1 — Trại Chủ hiện tại'."),
                ("Q20", StoryChapterId.Ch4_BuaToiChuaCoKhach, "Q20 · Lựa chọn trước bình minh", "Chọn 1 trong 3 kết thúc Mùa 1 (Cắt cáp giữ trại / Giữ máy lập hồ sơ / Mang vòng số 19 đi tìm chú Minh).", "Dù chọn cách nào thì trước lúc mặt trời mọc, mình cũng phải cho đàn heo ăn cử sáng.")
            };

            for (int i = 0; i < mainQuests.Length; i++)
            {
                var mq = mainQuests[i];
                Quests.Add(new StoryQuestRecord
                {
                    QuestId = mq.id,
                    Title = mq.title,
                    Chapter = mq.ch,
                    IsSideQuest = false,
                    IsUnlocked = i == 0,
                    IsCompleted = false,
                    ObjectiveSummary = mq.summary,
                    KhoaDeadpanNote = mq.note
                });
            }

            // 12 nhiệm vụ Phụ (S01 - S12)
            (string id, string title, string summary)[] sideQuests =
            {
                ("S01", "S01 · Giám Đốc Ngỗng mất ngủ", "Tìm lại chiếc chuông đồng nhỏ dưới bờ ao cho Ngỗng Báo Động."),
                ("S02", "S02 · Con heo chân trắng giả chết khi cân", "Gõ máng 2 nhịp để đánh thức con heo chân trắng nằm ngửa giả chết trên bàn cân."),
                ("S03", "S03 · Ấm trà vối của Bà Sáu", "Mang 3 bó thảo dược sạch cho Bà Sáu pha trà giữ ấm đêm sương."),
                ("S04", "S04 · Radio một sóng của Tám Vá", "Nạp pin tụ điện cho chiếc radio bắt sóng tầm ngắn của Tám Vá."),
                ("S05", "S05 · Sổ nợ gạch xóa của Ông Tồn", "Kiểm tra các trang sổ bị xé ở Chợ Ngày."),
                ("S06", "S06 · Thuốc đỏ của Bác sĩ Dung", "Giúp Bác sĩ Dung phân loại 5 mẫu nấm phát quang."),
                ("S07", "S07 · Chiếc ủng chiếc trái của Mận", "Tìm lại chiếc ủng cao su chân trái bị lũ chuột tha ra bãi cỏ Đông."),
                ("S08", "S08 · Lồng đèn giấy của Cúc", "Treo 4 đèn báo hiệu quanh cổng Nơi Trú Đàn trước đêm mưa."),
                ("S09", "S09 · Bưu thiếp không tem của Lệ", "Nhận tấm bưu thiếp gửi từ năm 2019 không dán tem nhưng có dấu nhận."),
                ("S10", "S10 · Chốt gác đêm mưa của Anh Ca", "Tiếp tế cám nóng và nhiên liệu cho Chốt Canh Gác phía Bắc."),
                ("S11", "S11 · Mười hai quả trên cây khô", "Thu hoạch Quả Mộng từ sự kiện Vườn Mười Hai Quả."),
                ("S12", "S12 · Bóng thứ hai dưới nắng trưa", "Quan sát cái bóng lệch hướng ở Đồng Cỏ Không Bóng.")
            };

            for (int i = 0; i < sideQuests.Length; i++)
            {
                Quests.Add(new StoryQuestRecord
                {
                    QuestId = sideQuests[i].id,
                    Title = sideQuests[i].title,
                    Chapter = (StoryChapterId)Math.Min(4, (i / 3) + 1),
                    IsSideQuest = true,
                    IsUnlocked = true,
                    IsCompleted = false,
                    ObjectiveSummary = sideQuests[i].summary,
                    KhoaDeadpanNote = "Chuyện nhỏ ở Bờ Rìa, nhưng ghi vào lề sổ thì lại khớp với chuyện lớn."
                });
            }
        }

        /// <summary>
        /// Quy tắc ngôn ngữ thị trấn Bờ Rìa: Không bao giờ đọc số 7 là "bảy", luôn gọi là "sáu với một" (COT_TRUYEN_PIG_TYCOON.md).
        /// </summary>
        public static string FormatEdgeTownNumber(int number)
        {
            if (number == 7) return "sáu với một";
            if (number == 17) return "mười sáu với một";
            if (number == 70) return "sáu mươi với mười";
            return number.ToString();
        }

        /// <summary>
        /// Khi hỏi người làng về chủ đề cấm kỵ (tầng 2, năm 2018, đội thu hồi, kiện hàng thứ 7) khi chưa đủ độ tin cậy,
        /// họ luôn nhìn lên mái hiên và đáp "Mai trời nắng."
        /// </summary>
        public string AskVillagerAboutTopic(string npcId, string topic)
        {
            int trust = NpcTrustTier.TryGetValue(npcId, out var t) ? t : 1;
            bool isForbiddenTopic = topic.Contains("2018", StringComparison.OrdinalIgnoreCase) ||
                                    topic.Contains("tầng hai", StringComparison.OrdinalIgnoreCase) ||
                                    topic.Contains("thu hồi", StringComparison.OrdinalIgnoreCase) ||
                                    topic.Contains("số 7", StringComparison.OrdinalIgnoreCase) ||
                                    topic.Contains("chú Minh", StringComparison.OrdinalIgnoreCase);

            if (isForbiddenTopic && trust < 2)
            {
                return $"[{npcId}] nhìn lên mái tôn, im lặng hai giây rồi đáp: \"Mai trời nắng.\"";
            }

            return npcId switch
            {
                "BaSau" => "Bà Sáu thở dài: \"Cái bát thứ ba bà úp sẵn không phải cho khách lạ. Chú Minh dặn hễ đồng hồ lệch đủ hai phút thì cứ xới cơm.\"",
                "Man" => "Mận chỉ tay về góc Tây Bắc: \"Bầy heo nhớ mặt người lâu lắm anh Khoa. Chỗ trống đó là chỗ hồi nhỏ anh đứng cho tụi nó ăn.\"",
                "TamVa" => "Tám Vá gõ mỏ lết vào thành giếng: \"Dưới đáy giếng có đường cáp đồng trục chạy thẳng về trạm Bạch Vân. Đúng 02:02 là quạt làm mát quay.\"",
                "OngTon" => "Ông Tồn đẩy gọng kính: \"Ở cái chợ này đếm tới sáu là phải nói 'sáu với một'. Đếm số bảy thành tiếng là tối đó có đèn xanh rọi xuống bãi cân.\"",
                "BacSiDung" => "Bác sĩ Dung cất ống nghe: \"Bệnh Nấm Phát Quang không có trong sách thú y đâu Khoa. Nó mọc theo tần số phát dưới lòng đất.\"",
                _ => $"[{npcId}] gật đầu: \"Cứ giữ cho đàn heo no bụng trước khi sương xuống.\""
            };
        }

        public (bool promoted, string message) VerifyObservationToRightColumn(string notebookId, string verifiedFact)
        {
            var entry = NotebookEntries.Find(e => e.Id == notebookId);
            if (entry == null)
                return (false, "Không tìm thấy mục ghi chép trong Sổ Hai Cột.");

            entry.EvidenceCount += 1;
            entry.RightColumn_CoThat = verifiedFact;
            entry.IsVerifiedToRightColumn = true;
            return (true, $"[SỔ HAI CỘT] Đã chuyển '{entry.Title}' từ cột [THẤY] sang cột [CÓ THẬT]: {verifiedFact}");
        }

        public (bool completed, string message) CompleteQuestStep(string questId)
        {
            var q = Quests.Find(x => x.QuestId == questId);
            if (q == null)
                return (false, $"Không tìm thấy nhiệm vụ {questId}.");

            q.IsUnlocked = true;
            q.IsCompleted = true;

            // Mở khóa nhiệm vụ kế tiếp trong chuỗi chính tuyến
            int idx = Quests.IndexOf(q);
            if (!q.IsSideQuest && idx + 1 < Quests.Count && !Quests[idx + 1].IsSideQuest)
            {
                Quests[idx + 1].IsUnlocked = true;
                CurrentChapter = Quests[idx + 1].Chapter;
            }

            // Tự động cập nhật Sổ Hai Cột ở các mốc cốt truyện quan trọng
            if (questId == "Q05")
            {
                VerifyObservationToRightColumn("nb_02_lech_hai_phut", "Hóa đơn in lúc 07:00 cho chuyến hàng xuất lúc 07:02: độ lệch 2 phút là có thật.");
            }
            else if (questId == "Q07")
            {
                VerifyObservationToRightColumn("nb_03_vong_co_so_19", "Vòng cổ số 19 và mảnh da chữ nêm số 24 khớp với 24 lô bàn giao của Trạm Đệm Bờ Rìa.");
            }
            else if (questId == "Q08")
            {
                VerifyObservationToRightColumn("nb_04_hoc_tuong_bay_cot", "7 cột đo chiều cao (2019–2026) kẻ bằng tay trái trùng khớp với hồ sơ theo dõi bản sao.");
            }
            else if (questId == "Q12" || questId == "Q11")
            {
                HerdEmptySpotObserved = true;
                VerifyObservationToRightColumn("nb_05_cho_trong_giua_dan", "Đàn heo chừa chỗ trống đường kính 1,2m khớp chính xác vị trí của Khoa trong ảnh năm 2018.");
            }

            // Tăng độ tin cậy NPC khi hoàn thành nhiệm vụ
            foreach (var key in new List<string>(NpcTrustTier.Keys))
            {
                NpcTrustTier[key] = Math.Min(3, NpcTrustTier[key] + 1);
            }

            return (true, $"Hoàn thành nhiệm vụ [{q.Title}] — Ghi chú Khoa: \"{q.KhoaDeadpanNote}\"");
        }

        /// <summary>
        /// Sự kiện S02: Con heo chân trắng nằm ngửa giả chết khi đưa lên bàn cân (COT_TRUYEN_PIG_TYCOON.md).
        /// </summary>
        public (bool playedDead, float displayedWeightKg, string message) EvaluateWhiteFootedPigWeighing(Pig pig, bool tappedTroughTwice)
        {
            if (pig == null) return (false, 0f, "Không có heo trên bàn cân.");
            bool isWhiteFooted = pig.Name.Contains("Chân Trắng", StringComparison.OrdinalIgnoreCase) ||
                                 pig.Id.EndsWith("1", StringComparison.OrdinalIgnoreCase);

            if (isWhiteFooted && !tappedTroughTwice)
            {
                WhiteFootedPigPrankSeen = true;
                return (true, 0.0f, $"[{pig.Name}] vừa đặt lên bàn cân đã lăn đùng ra nằm ngửa, bốn chân duỗi thẳng giả chết! Kim cân chỉ 0.0 kg. (Gõ nhẹ máng ăn 2 nhịp để nó đứng dậy).");
            }

            if (isWhiteFooted && tappedTroughTwice)
            {
                CompleteQuestStep("S02");
                return (false, pig.WeightKg, $"Gõ máng 'cộc-cộc' 2 nhịp: [{pig.Name}] lập tức bật dậy vẫy tai, bàn cân hiện đúng {pig.WeightKg:0.1} kg!");
            }

            return (false, pig.WeightKg, $"Cân [{pig.Name}]: {pig.WeightKg:0.1} kg.");
        }

        // =====================================================================
        // CÁC CƠ CHẾ CỐT TRUYỆN CHUYÊN SÂU MÙA 1 "SÁU VỚI MỘT" (doc_2 / COT_TRUYEN_PIG_TYCOON.md)
        // =====================================================================

        public static readonly IReadOnlyList<(string memberName, string deathDate, string packageCode)> SixFamilyMembersAndPackages =
            new List<(string, string, string)>
            {
                ("Ông nội Lâm", "17/07/2018", "Kiện #01 — Đã nhận lúc 02:00 (20/07/2018)"),
                ("Bà nội Sương", "04/09/2018", "Kiện #02 — Đã nhận lúc 02:00 (07/09/2018)"),
                ("Anh Minh", "19/02/2019", "Kiện #03 — Đã nhận lúc 02:00 (22/02/2019)"),
                ("Em gái Bé An (Tuyến Khoa) / Em trai Bé Khoa (Tuyến An)", "13/05/2019", "Kiện #04 — Đã nhận lúc 02:00 (16/05/2019)"),
                ("Cha Hậu", "24/08/2019", "Kiện #05 — Đã nhận lúc 02:00 (27/08/2019)"),
                ("Mẹ Thủy", "08/11/2019", "Kiện #06 — Đã nhận lúc 02:00 (11/11/2019)")
            };

        public bool TinBoxUnderSeventhStairOpened { get; set; } = false;
        public bool FurnaceSlotSootScraped { get; set; } = false;
        public bool FurnaceSlotTriedWrongItem { get; set; } = false;
        public bool FurnaceSlotNicheOpened { get; set; } = false;
        public bool VillagerSilenceBrokenInQ14 { get; set; } = false;
        public bool SecondBlueGlazeBowlReturnedInQ20 { get; set; } = false;

        /// <summary>
        /// Mở hộp thiếc dưới bậc cầu thang thứ 7 (Mục 1.12 / 1B.12):
        /// Thu thập: (1) Sổ vải 7 phần (6 phần chữ Minh, phần 7 để trắng), (2) Ảnh gia đình ở trại heo có con heo đốm trắng chân trước bên trái nằm ngửa giả chết,
        /// (3) Mảnh da cổ khắc chữ nêm Babylon số 24 (2 cọc đứng = 20, 4 dấu góc = 4), mặt sau có chữ viết tay chính Khoa/An ngày 21/09/2026.
        /// </summary>
        public (bool opened, string summary) OpenTinBoxUnderSeventhStair()
        {
            TinBoxUnderSeventhStairOpened = true;
            string who = ActiveProtagonist == PlayerId.An ? "An (không có sticker và không có mặt cười :))" : "Khoa";
            return (true,
                "[HỘP THIẾC DƯỚI BẬC THANG THỨ 7] Gồm 3 vật: (1) Sổ vải chia 7 phần (Phần 1–6 chữ anh Minh, Phần 7 để trắng), " +
                "(2) Tấm ảnh gia đình chụp ở trại heo với con heo chân trắng nằm ngửa giả chết ở góc, " +
                $"(3) Mảnh da cổ khắc chữ nêm Babylon số 24 (2 cọc đứng = 20, 4 góc = 4), mặt sau ghi bằng nét chữ của chính {who} ngày 21/09/2026: 'Khi về nhớ thử mảnh này ở khe bếp lò nhà ông nội'.");
        }

        /// <summary>
        /// Câu đố 4 lớp tại Cái Khe Cạnh Bếp Lò (Mục 1.16 / 1B.16 / P08 / Q16):
        /// - Lớp 1: Cạo lớp muội than lộ khe đá rộng 3cm.
        /// - Lớp 2: Thử nhét ít nhất 1 đồ khác trong ba lô (chìa đồng, dao găm, thẻ gỗ) trước khi đút Mảnh da cổ.
        /// - Lớp 3: Quay mặt khắc chữ nêm số 24 VÀO TRONG (nếu quay ra ngoài chỉ kêu 1 tiếng 'cạch' rồi đứng).
        /// - Lớp 4: Nghe 2 tiếng 'cạch... cạch' rồi kẹt -> buông tay đợi đúng 2 phút (11:16 -> 11:18) để lò xo trong tường tự nảy thêm 4 tiếng ('2 rồi 4 = 24') mở hốc tường 7 cột đo chiều cao (kẻ bằng tay trái!).
        /// </summary>
        public (bool unlockedNiche, string message) AttemptFurnaceSlotPuzzle(
            bool scrapedSoot,
            bool triedOtherBackpackItemFirst,
            bool insertedAncientLeather24,
            bool number24FacingInward,
            bool waitedTwoMinutesHandsOff)
        {
            if (!scrapedSoot)
                return (false, "[LỚP 1] Khe đá còn bị lấp kín bởi muội than đen. Cần cạo sạch lớp muội trước.");

            FurnaceSlotSootScraped = true;
            if (!triedOtherBackpackItemFirst)
                return (false, "[LỚP 2] Thử đút chìa khóa đồng / thẻ gỗ vào khe đá 3cm nhưng đều không khớp.");

            FurnaceSlotTriedWrongItem = true;
            if (!insertedAncientLeather24)
                return (false, "[LỚP 2] Cần đưa Mảnh da cổ khắc dấu nêm 24 vào khe đá.");

            if (!number24FacingInward)
                return (false, "[LỚP 3] Mặt số 24 quay ra ngoài: trong tường chỉ kêu đúng một tiếng 'cạch' khô rồi đứng im. Phải rút ra xoay mặt số 24 quay vào trong!");

            if (!waitedTwoMinutesHandsOff)
                return (false, "[LỚP 4] Mặt số 24 quay vào trong: 'Một cái. Hai cái. Rồi kẹt.' Đừng ấn thêm — phải buông tay đứng đợi đúng 2 phút (11:16 -> 11:18)!");

            FurnaceSlotNicheOpened = true;
            CompleteQuestStep("P08");
            CompleteQuestStep("Q08");
            return (true,
                "[MỞ HỐC TƯỜNG BẾP LÒ · 2 RỒI 4 = 24] Đúng 11:18 (sau 2 phút buông tay), trong tường tự kêu thêm bốn tiếng 'cạch... cạch... cạch... cạch'. " +
                "Viên gạch cạnh bếp lò dịch ra, lộ hốc tường có 7 cột đo chiều cao (6 cột đầu dừng lại ở các năm mất của gia đình, cột thứ 7 vẫn lên đều mỗi năm và kẻ bằng TAY TRÁI — trong khi Khoa/An thuận tay phải!).");
        }

        /// <summary>
        /// Hệ thống Biểu Cảm Qua Bề Mặt Phản Chiếu đối xứng giữa Khoa ("lấy cương") và An ("lấy nhu") (Mục 1.7, 1B.7, 2.7, Q10, S07, Q19, 8.3):
        /// - Khoa: Ngoài đời mặt hoàn toàn đơ ráo hoảnh; bóng phản chiếu lại nhíu mày (Q10), mỉm cười (S07), khóc (Q19), hoặc cười nối đuôi vô tận (8.3).
        /// - An: Ngoài đời luôn mỉm cười dịu dàng; bóng phản chiếu lại đứng im không cười (Q10), nhìn thẳng từ giếng nước (S07), khóc trong mặt dây chuyền bạc (Q19), hoặc bóng thứ 7 đứng im không cười (8.3).
        /// </summary>
        public static (string realFaceState, string reflectionState, string storyContext) EvaluateReflectiveSurfaceEmotionForProtagonist(
            string sceneCode,
            PlayerId protagonist)
        {
            if (protagonist == PlayerId.An)
            {
                return sceneCode switch
                {
                    "Mirror_7Mornings" => (
                        "Mặt An ngoài đời mỉm cười dịu dàng tập chào trước gương lúc 06:58 -> 07:02.",
                        "Bóng An trong gương nhà tắm đứng im KHÔNG CƯỜI, hạ khóe môi xuống trước mặt thật đúng 2 giây.",
                        "Mục 1B.7 · Bảy buổi sáng trước gương của An."),
                    "WindowGlass_Q10" => (
                        "Mặt An ngoài đời mỉm cười lễ phép khi quay sang nói chuyện với chú Tư.",
                        "Bóng phản chiếu của An trên kính cửa sổ ĐỨNG IM KHÔNG CƯỜI, mặt phẳng lặng nhìn thẳng xuống chiếc đĩa sắt dưới giếng.",
                        "Mục 5B.Q10 · Mẫu Hạm có mùi cám (Góc nhìn An)."),
                    "WaterTrough_S07" => (
                        "Mặt An ngoài đời mỉm cười nhìn xuống mặt nước giếng.",
                        "Bóng An dưới mặt nước giếng KHÔNG CƯỜI, bình thản ngước nhìn lên như hai người đang đối diện nhau qua mặt nước.",
                        "Mục S07 · Mẹ ơi con không phải kẻ trộm (Góc nhìn An)."),
                    "Necklace7Notches_Q19" => (
                        "Mặt An ngoài đời nhìn xuống mặt bạc vẫn đang MỈM CƯỜI dịu dàng.",
                        "Bóng phản chiếu của An trong mặt dây chuyền bạc 7 vết khắc của mẹ đang KHÓC (nước mắt chảy dọc hai má trong mảnh bạc).",
                        "Mục 5B.Q19 · Bữa tối chưa có khách (Góc nhìn An)."),
                    "EndlessMirrors_Ending83" => (
                        "Mặt An ngoài đời mỉm cười trước hai tấm gương đối diện trong tiệm Tám Vá.",
                        "6 bóng đầu tiên mỉm cười giống An, nhưng bóng thứ 7 ('sáu với một') trong đáy gương ĐỨNG IM KHÔNG CƯỜI.",
                        "Mục 8.3 · Kết thúc Mang vòng về nhà (Góc nhìn An)."),
                    _ => (
                        "Mặt An ngoài đời luôn mỉm cười chu đáo.",
                        "Bóng phản chiếu đứng im không cười, giữ nguyên cảm xúc thật.",
                        "Nguyên tắc phản chiếu cảm xúc ngược chiều của An (Mục 2.7).")
                };
            }

            return sceneCode switch
            {
                "Mirror_7Mornings" => (
                    "Mặt ngoài đời đứng im, không biểu cảm (06:58 -> 07:02).",
                    "Bóng trong gương nhà tắm chớp mắt chậm hơn 1–2 giây, khóe miệng nhếch nhẹ rồi tắt.",
                    "Mục 1.7 · Bảy buổi sáng trước gương trước tai nạn ngã tư 07:02."),
                "WindowGlass_Q10" => (
                    "Mặt Khoa ngoài đời bình thản nhìn đàn heo.",
                    "Bóng phản chiếu trên mặt kính cửa sổ đang NHÍU MÀY thật sâu.",
                    "Mục Q10 · Đêm thứ 2 có tiếng còi lạ từ rừng Bờ Rìa."),
                "WaterTrough_S07" => (
                    "Mặt Khoa ngoài đời giữ nguyên không đổi.",
                    "Bóng Khoa dưới mặt nước máng ăn đang MỈM CƯỜI nhìn con heo chân trắng.",
                    "Mục S07 · Con heo chân trắng nằm ngửa giả chết lần thứ ba."),
                "Necklace7Notches_Q19" => (
                    "Mặt Khoa ngoài đời ráo hoảnh không một giọt nước mắt.",
                    "Bóng phản chiếu trên mảnh đồng 7 khía đang KHÓC.",
                    "Mục Q19 · Đọc mảnh giấy của anh Minh để lại."),
                "EndlessMirrors_Ending83" => (
                    "Mặt Khoa ngoài đời không cười.",
                    "Tất cả các bóng Khoa nối đuôi nhau trong hai tấm gương đối diện đều đang CƯỜI.",
                    "Mục 8.3 · Kết thúc Mang vòng về nhà."),
                _ => (
                    "Mặt Khoa ngoài đời không biểu cảm.",
                    "Bóng phản chiếu chậm hơn 2 giây.",
                    "Nguyên tắc phản chiếu cảm xúc của Khoa.")
            };
        }

        public static (string realFaceState, string reflectionState, string storyContext) EvaluateReflectiveSurfaceEmotion(string sceneCode)
        {
            if (!string.IsNullOrEmpty(sceneCode) && sceneCode.StartsWith("An_", StringComparison.OrdinalIgnoreCase))
            {
                return EvaluateReflectiveSurfaceEmotionForProtagonist(sceneCode.Substring(3), PlayerId.An);
            }
            return EvaluateReflectiveSurfaceEmotionForProtagonist(sceneCode, PlayerId.Khoa);
        }

        /// <summary>
        /// Kiểm tra Cái Hộp Kỹ Thuật trong nhà chính (Mục 5.2 / Q13 / Q19 / Q20):
        /// Ghi nhận 6 kiện đã nhận khớp 6 người thân trong gia đình (2018–2019), kiện thứ 7 đang ở trạng thái "Chờ xác nhận: phần thứ bảy được viết".
        /// </summary>
        public (int receivedPackagesCount, string seventhPackageStatus, string fullLog) InspectTechnicalBoxSevenChannels()
        {
            string status7 = SeventhChannelCut
                ? "Kiện #07: ĐÃ NGẮT KÊNH GỬI & HOÀN TRẢ NGƯỜI GỬI (Kèm chiếc bát men xanh mẻ thứ hai)"
                : "Kiện #07: ĐANG CHỜ XÁC NHẬN — 'Phần thứ bảy được viết'";
            return (6, status7,
                $"Bảng đèn 7 kênh trong hộp kỹ thuật: Kênh 1–6 báo 'ĐÃ NHẬN' trùng khít 6 kiện đồ của Ông Lâm, Bà Sương, Anh Minh, {YoungestSiblingName}, Cha Hậu, Mẹ Thủy (2018–2019). " +
                $"Kênh thứ 7 ('sáu với một'): {status7}.");
        }

        /// <summary>
        /// Cuộc họp Q14 (Mục 5.2 doc_2): Khi Khoa đặt từ 3 mảnh bằng chứng trở lên lên bàn, Bà Sáu, Tám Vá và Lệ ngừng nói câu "Mai trời nắng." và kể toàn bộ sự thật.
        /// </summary>
        public (bool brokeSilence, string dialogue) HoldVillagerEvidenceMeetingQ14(int evidenceCountPresented)
        {
            if (evidenceCountPresented < 3)
            {
                return (false, "Chưa đủ 3 mảnh bằng chứng. Bà Sáu, Tám Vá và Lệ nhìn lên mái hiên đáp: \"Mai trời nắng.\"");
            }

            VillagerSilenceBrokenInQ14 = true;
            foreach (var key in new List<string>(NpcTrustTier.Keys))
            {
                NpcTrustTier[key] = 3;
            }
            CompleteQuestStep("Q14");
            return (true,
                "[Q14 · KHÔNG AI NÓI 'MAI TRỜI NẮNG' NỮA] Trước 3 bằng chứng trên bàn, Bà Sáu bỏ ấm trà xuống: " +
                "\"Cái hộp trong nhà chính mỗi lần nhận đủ một phần sổ là đóng một kiện gửi đi lúc 02:00. Cả thị trấn phải nói 'sáu với một' để cái máy không nhận ra tiếng gọi số thứ bảy.\"");
        }

        /// <summary>
        /// Sự kiện Q20 "Rìu Sắt trả hàng" (Mục 5.2 doc_2): Tháo đường gửi thứ 7 khiến kiện hàng bật ngược về hiên nhà kèm tờ giấy "Gửi trả người gửi" và chiếc bát men xanh mẻ thứ hai.
        /// </summary>
        public (bool returned, string message) TriggerQ20IronAxeReturnPackage()
        {
            SeventhChannelCut = true;
            SecondBlueGlazeBowlReturnedInQ20 = true;
            CompleteQuestStep("Q20");
            return (true,
                "[Q20 · RÌU SẮT TRẢ HÀNG] Đường gửi thứ 7 bị tháo rời! Sáng hôm sau trước hiên nhà xuất hiện kiện hàng dán nhãn 'GỬI TRẢ NGƯỜI GỬI' — " +
                "bên trong là chiếc bát men xanh sứt miệng thứ hai, đặt cạnh chiếc bát của Bà Sáu trên bàn ăn.");
        }

        /// <summary>
        /// Chọn 1 trong 3 Kết thúc Mùa 1 (Mục 8.1, 8.2, 8.3) + Mục 8.4 (Cuộc họp của Ngỗng Giám Đốc) + Cảnh sau credit 8.5 ("Anh tới sớm hai phút" — Mùa 2: Người tới sớm).
        /// </summary>
        public (bool success, string endingTitle, string epilogueText, string postCreditHook) SelectSeasonOneEnding(StoryEndingId ending)
        {
            if (ending == StoryEndingId.ChuaChon)
                return (false, "", "Chưa chọn kết thúc.", "");

            ChosenEnding = ending;
            PostCreditTriggered = true;
            CompleteQuestStep("Q20");

            string title;
            string epilogue;
            if (ending == StoryEndingId.Ending_8_1_GiuTraiThaoDuongGui)
            {
                SeventhChannelCut = true;
                SecondBlueGlazeBowlReturnedInQ20 = true;
                title = "KẾT THÚC 8.1 · GIỮ TRẠI, THÁO ĐƯỜNG GỬI (KHẮC NGỮ)";
                epilogue = "Dù chọn kết nào, Minh Ước 3.000 năm đều vỡ và sáu người nhà tan thành khói ấm có mùi đúng với từng người. Đường gửi số 7 bị tháo rời lúc 02:02; đồng hồ trên tường lần đầu tiên chạy đúng 07:00. Đàn heo ùa ra sân cỏ, không còn chừa chỗ trống góc Tây Bắc nữa. " +
                           "[8.4 SAU KẾT THÚC]: Con ngỗng Giám Đốc đứng trên thùng phuy chủ trì 'cuộc họp sáng' với đàn heo, con heo chân trắng lại nằm ngửa giả chết đợi gõ máng 2 nhịp.";
            }
            else if (ending == StoryEndingId.Ending_8_2_GiuMayLamHoSo)
            {
                SeventhChannelCut = true;
                title = "KẾT THÚC 8.2 · GIỮ MỘT CHỖ (THÁNH ĐIỂN)";
                epilogue = "Minh Ước 3.000 năm vỡ, sáu người nhà tan thành khói ấm. Khoa và An ngắt đường gửi thứ 7 nhưng giữ lại một chỗ trống trong sổ và chiếc bát thứ ba trên bàn ăn để nhớ mặt từng người đã đi qua Bờ Rìa.";
            }
            else
            {
                SeventhChannelCut = true;
                title = "KẾT THÚC 8.3 · MANG VÒNG VỀ NHÀ (HUYỀN Y)";
                epilogue = "Minh Ước 3.000 năm vỡ. Khoa/An trao lại chùm chìa khóa dây cước đỏ cho Mận và Tám Vá, mang chiếc vòng đồng 7 khía cùng tấm ảnh năm 2018 rời Bờ Rìa. Trong hai tấm gương đối diện ở phòng tắm, hàng loạt bóng phản chiếu nối đuôi nhau — và bóng thứ 7 đứng nhìn thẳng từ đáy gương.";
            }

            string postCredit = "[CẢNH SAU CREDIT 8.5 · BA NĂM SAU TẠI CHỮ RÌA] Ba năm sau, tấm biển gỗ chỉ còn chữ 'Rìa'. Mận đặt ly nước vối xuống bàn trước hiên quán nhỏ, nhìn chiếc đồng hồ chỉ 07:00 (sớm 2 phút so với 07:02) và mỉm cười nói: 'Anh tới sớm hai phút.' Hồ sơ 'Kiện 6+1' đã khép lại.";
            return (true, title, epilogue, postCredit);
        }
    }

    public enum SleepBypassItemId
    {
        TraLaVong,
        ThuocAnThan,
        HuongMeThao,
        DanHoiNguyen,
        QuaMong
    }

    public enum SpecialtyPaperColor
    {
        Lam,
        Vang,
        Tim,
        Do
    }

    [Serializable]
    public class PowerPoleNode
    {
        public string PoleId = "";
        public float WorldX;
        public float WorldY;
        public float CoverageRadiusMeters = 12f; // GDD 3.8: Cột điện phủ bán kính 12m
    }

    [Serializable]
    public class PowerWireSegment
    {
        public string WireId = "";
        public string FromNodeId = "Generator_Main";
        public string ToNodeId = "";
        public bool IsUnderground = false; // Dây ngầm giá x3 nhưng không bị Kẻ Cắt Dây cắt (GDD 3.8 & 15)
        public bool IsCut = false;
        public int BuildCostGold = 40;
    }

    [Serializable]
    public class PowerGridState
    {
        public int GeneratorLevel = 1; // Cấp 1: 30/60u, Cấp 2: 70/130u, Cấp 3: 140/240u
        public int CapacitorCount = 1; // Mỗi Tụ Điện +60u dự trữ
        public float CapacitorChargeUnits = 60f;
        public float CurrentDemandUnits = 18f;
        public bool IsBreakerTripped = false;
        public float BreakerTripRemainingSeconds = 0f;

        public List<PowerPoleNode> Poles { get; set; } = new List<PowerPoleNode>();
        public List<PowerWireSegment> Wires { get; set; } = new List<PowerWireSegment>();

        public int GetContinuousCapacity() => GeneratorLevel switch
        {
            1 => 30,
            2 => 70,
            >= 3 => 140,
            _ => 30
        };

        public int GetPeakCapacity() => GeneratorLevel switch
        {
            1 => 60,
            2 => 130,
            >= 3 => 240,
            _ => 60
        };

        public float GetMaxCapacitorReserve() => CapacitorCount * 60f;

        /// <summary>
        /// Lắp đặt tuyến dây điện (Dây nổi giá gốc 40G có thể bị Kẻ Cắt Dây cắt; Dây ngầm giá ×3 = 120G miễn nhiễm bị cắt - GDD 3.8 & 15).
        /// </summary>
        public PowerWireSegment ConnectWire(string wireId, string fromNodeId, string toNodeId, bool underground = false, int baseCost = 40)
        {
            int finalCost = underground ? baseCost * 3 : baseCost;
            var seg = new PowerWireSegment
            {
                WireId = wireId,
                FromNodeId = fromNodeId,
                ToNodeId = toNodeId,
                IsUnderground = underground,
                IsCut = false,
                BuildCostGold = finalCost
            };
            Wires.RemoveAll(w => w.WireId == wireId);
            Wires.Add(seg);
            return seg;
        }

        /// <summary>
        /// Tính tập hợp các nút điện (NodeId) đang có điện thông qua duyệt đồ thị BFS từ Generator_Main qua các đoạn dây chưa đứt (!IsCut).
        /// Hỗ trợ đầy đủ mất điện dây chuyền hạ lưu (Upstream Trunk Cut) và mạch vòng dự phòng (Redundant Loop).
        /// </summary>
        public HashSet<string> ComputePoweredNodesBfs()
        {
            var poweredNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (IsBreakerTripped)
                return poweredNodes;

            var queue = new Queue<string>();
            poweredNodes.Add("Generator_Main");
            queue.Enqueue("Generator_Main");

            bool hasGeneratorRootWire = false;
            for (int i = 0; i < Wires.Count; i++)
            {
                if (string.Equals(Wires[i].FromNodeId, "Generator_Main", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Wires[i].ToNodeId, "Generator_Main", StringComparison.OrdinalIgnoreCase))
                {
                    hasGeneratorRootWire = true;
                    break;
                }
            }

            if (!hasGeneratorRootWire)
            {
                var toSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < Wires.Count; i++)
                {
                    if (!string.IsNullOrEmpty(Wires[i].ToNodeId))
                        toSet.Add(Wires[i].ToNodeId);
                }
                for (int i = 0; i < Wires.Count; i++)
                {
                    string from = Wires[i].FromNodeId;
                    if (!string.IsNullOrEmpty(from) && !toSet.Contains(from) && poweredNodes.Add(from))
                    {
                        queue.Enqueue(from);
                    }
                }
            }

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                for (int i = 0; i < Wires.Count; i++)
                {
                    var w = Wires[i];
                    if (w.IsCut) continue;

                    if (string.Equals(w.FromNodeId, current, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrEmpty(w.ToNodeId) &&
                        poweredNodes.Add(w.ToNodeId))
                    {
                        queue.Enqueue(w.ToNodeId);
                    }
                    else if (string.Equals(w.ToNodeId, current, StringComparison.OrdinalIgnoreCase) &&
                             !string.IsNullOrEmpty(w.FromNodeId) &&
                             poweredNodes.Add(w.FromNodeId))
                    {
                        queue.Enqueue(w.FromNodeId);
                    }
                }
            }

            return poweredNodes;
        }

        public bool IsBuildingPoweredByGrid(DefenseBuildingInstance b, HashSet<string>? poweredNodes = null)
        {
            if (b == null) return false;
            if (IsBreakerTripped) return false;
            if (string.IsNullOrEmpty(b.ConnectedWireId)) return true;

            poweredNodes ??= ComputePoweredNodesBfs();
            var wire = Wires.Find(w => string.Equals(w.WireId, b.ConnectedWireId, StringComparison.OrdinalIgnoreCase));
            if (wire == null)
            {
                return poweredNodes.Contains(b.ConnectedWireId);
            }

            if (!wire.IsCut &&
                poweredNodes.Contains(wire.FromNodeId) &&
                (string.IsNullOrEmpty(wire.ToNodeId) || poweredNodes.Contains(wire.ToNodeId)))
            {
                return true;
            }

            // Nếu dây trực tiếp bị cắt nhưng nút đích (ToNodeId) có ít nhất 1 dây dự phòng khác nối tới nó đang còn nguyên và có điện (Mạch vòng dự phòng - GDD 16.3)
            if (!string.IsNullOrEmpty(wire.ToNodeId) && poweredNodes.Contains(wire.ToNodeId))
            {
                for (int i = 0; i < Wires.Count; i++)
                {
                    var alt = Wires[i];
                    if (!alt.IsCut &&
                        !string.Equals(alt.WireId, wire.WireId, StringComparison.OrdinalIgnoreCase) &&
                        (string.Equals(alt.ToNodeId, wire.ToNodeId, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(alt.FromNodeId, wire.ToNodeId, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Kẻ Cắt Dây tấn công tuyến dây điện (GDD 15 & 19.1):
        /// - Nếu là Dây ngầm (IsUnderground == true): KHÔNG THỂ CẮT!
        /// - Nếu là Dây nổi: bị đứt và tính lại đồ thị BFS từ Máy Phát Chính, làm mất điện toàn bộ công trình hạ lưu (trừ khi có mạch vòng dự phòng).
        /// </summary>
        public (bool cutSuccess, int disabledBuildingsCount, string message) TryCutWireByEnemy(
            string wireId,
            List<DefenseBuildingInstance>? buildings = null)
        {
            var wire = Wires.Find(w => w.WireId == wireId);
            if (wire == null)
                return (false, 0, $"Không tìm thấy tuyến dây [{wireId}].");

            if (wire.IsUnderground)
                return (false, 0, $"Tuyến dây [{wireId}] là DÂY NGẦM (giá ×3) — Kẻ Cắt Dây không thể cắt!");

            wire.IsCut = true;
            int disabled = 0;
            if (buildings != null)
            {
                var poweredNodes = ComputePoweredNodesBfs();
                for (int i = 0; i < buildings.Count; i++)
                {
                    bool nowPowered = IsBuildingPoweredByGrid(buildings[i], poweredNodes);
                    if (!nowPowered && (buildings[i].IsPowered || buildings[i].ConnectedWireId == wireId))
                    {
                        disabled++;
                    }
                    buildings[i].IsPowered = nowPowered;
                }
            }
            return (true, disabled, $"Kẻ Cắt Dây đã cắt đứt dây nổi [{wireId}], làm mất điện cục bộ {disabled} công trình hạ lưu!");
        }

        public (bool repaired, int restoredBuildingsCount) RepairCutWire(
            string wireId,
            List<DefenseBuildingInstance>? buildings = null)
        {
            var wire = Wires.Find(w => w.WireId == wireId);
            if (wire == null) return (false, 0);
            wire.IsCut = false;
            int restored = 0;
            if (buildings != null)
            {
                var poweredNodes = ComputePoweredNodesBfs();
                for (int i = 0; i < buildings.Count; i++)
                {
                    bool wasPowered = buildings[i].IsPowered;
                    bool nowPowered = IsBuildingPoweredByGrid(buildings[i], poweredNodes);
                    buildings[i].IsPowered = nowPowered;
                    if (!wasPowered && nowPowered)
                    {
                        restored++;
                    }
                }
            }
            return (true, restored);
        }

        public (string status, float fireRateMultiplier, int durabilityLossPerSec) EvaluateGridTick(float deltaSeconds)
        {
            if (IsBreakerTripped)
            {
                BreakerTripRemainingSeconds = Math.Max(0f, BreakerTripRemainingSeconds - deltaSeconds);
                if (BreakerTripRemainingSeconds <= 0f)
                {
                    IsBreakerTripped = false;
                }
                return ("SẬP CẦU DAO (Mất điện toàn tuyến)", 0f, 0);
            }

            int cont = GetContinuousCapacity();
            float loadRatio = cont > 0 ? CurrentDemandUnits / cont : 0f;

            if (loadRatio <= 1.0f)
            {
                CapacitorChargeUnits = Math.Min(GetMaxCapacitorReserve(), CapacitorChargeUnits + 5f * deltaSeconds);
                return ("Ổn định", 1.0f, 0);
            }

            if (loadRatio <= 1.10f)
            {
                float drain = (CurrentDemandUnits - cont) * deltaSeconds;
                CapacitorChargeUnits = Math.Max(0f, CapacitorChargeUnits - drain);
                return ("Xả Tụ Điện (100–110%)", 1.0f, 0);
            }

            if (loadRatio <= 1.40f)
            {
                return ("QUÁ TẢI (110–140%: -25% tốc bắn, -1 Độ bền/s)", 0.75f, 1);
            }

            // Quá tải > 140% -> Sập cầu dao 4 giây (GDD 16.4)
            IsBreakerTripped = true;
            BreakerTripRemainingSeconds = 4.0f;
            return ("QUÁ TẢI NẶNG (>140%): SẬP CẦU DAO 4 GIÂY!", 0f, 2);
        }
    }

    [Serializable]
    public class WaveBattleReport
    {
        public int WaveNumber;
        public bool Victory;
        public int EnemiesDefeated;
        public int PigsLostOrAbducted;
        public int AmmoSpent;
        public int BuildingDurabilityLost;
        public int PlayerGuardBreaks;
        public int GoldRewarded;
        public string SummaryText = string.Empty;
    }

    /// <summary>
    /// Hệ thống Cơ Chế Chuyên Sâu GDD v6.0 & Sổ tay v3.0 (6.2.1):
    /// - Lưới Điện Vật Lý (Generator 30/70/140, Peak 60/130/240, Cột Điện 12m, Dây nổi / Dây ngầm ×3, Sập cầu dao >140%)
    /// - 2 Công trình Chuyên biệt: Phòng Nghiên Cứu (Giám định 95/85/65/50%, Thuần Hóa Huyết Thống 1200G/24h, Tinh Hoa Dị Biến) & Xưởng Cường Hóa (4 bàn: Gia Cường, Rã Giấy, Dùng Giấy, Khắc Phù/Ấn)
    /// - 5 Vật phẩm xóa hồi chờ ngủ (chỉ Quả Mộng không có tác dụng phụ)
    /// - Cổng Giữa Game & 6 Khóa Tính Chất Chuyên Biệt Ẩn + Cường hóa Giấy Chuyên Biệt (+0 -> +3, trần 80%)
    /// - Tình báo Đợt Quái 6 Bậc (0–5: WHAT / WHEN / WHAT TO HOLD) & Báo Cáo Sau Trận (WaveBattleReport)
    /// </summary>
    [Serializable]
    public class DeepMechanicsSystem
    {
        public PowerGridState PowerGrid { get; set; } = new PowerGridState();
        public bool MidGameGateUnlocked { get; set; } = false;
        public int UnlockedSpecialtyLocksCount { get; set; } = 0; // 0..6 khóa tuần tự (GDD 8.2)
        public SpecialtyPaperColor EquippedPaperColor { get; set; } = SpecialtyPaperColor.Lam;
        public int intEquippedPaperEnhancementLevel { get; set; } = 0; // +0..+3
        public int EnhancementStonesCount { get; set; } = 5;
        public int EnhancementDustCount { get; set; } = 0; // Bàn Rã Giấy tạo Bụi Cường Hóa
        public int DeviantEssenceCount { get; set; } = 0;  // Tinh Hoa Dị Biến từ Phòng Nghiên Cứu
        public int CurrentIntelTier { get; set; } = 1; // Bậc 0..5 (GDD 14.3)
        public int ForcedRestCountThisCycle { get; set; } = 0; // GDD 9.5: 2 lần đầu/chu kỳ không phạt, lần 3: +1 Mệt Dội, lần 4+: +2
        public int PeakHerdCountObserved { get; set; } = 0;
        public int RareOrHigherGenesIdentifiedCount { get; set; } = 0;
        public int CycleBossesDefeatedCount { get; set; } = 0;
        public int ActiveDefenseBuildingsCount { get; set; } = 0;
        public string SpecialtyTraitDisplayName => UnlockedSpecialtyLocksCount >= 6 ? "Trại Chủ Lão Luyện / Người Đọc Dòng" : "???";
        public List<WaveBattleReport> BattleReports { get; set; } = new List<WaveBattleReport>();

        public Dictionary<SleepBypassItemId, int> SleepItemsInventory { get; set; } = new Dictionary<SleepBypassItemId, int>
        {
            { SleepBypassItemId.TraLaVong, 2 },
            { SleepBypassItemId.ThuocAnThan, 1 },
            { SleepBypassItemId.HuongMeThao, 1 },
            { SleepBypassItemId.DanHoiNguyen, 1 },
            { SleepBypassItemId.QuaMong, 1 }
        };

        /// <summary>
        /// Sử dụng 1 trong 5 vật phẩm phá cooldown ngủ 6h (GDD 9.5 & 9.6):
        /// - Luật nghỉ cưỡng bức (GDD 9.5): 2 lần đầu mỗi chu kỳ không phạt; lần 3: +1 Mệt Dội; lần 4+: +2 Mệt Dội (Quả Mộng không tính vào số lần nghỉ cưỡng bức).
        /// - Trà Lá Vông (I): Ngủ ngắn 3h, Stamina >= 50%, không xóa Tải ngày, hôm sau Tải ngày +10%.
        /// - Thuốc An Thần (II): Ngủ chuẩn đầy đủ (Stamina >= 80%, xóa Tải ngày), Lờ Đờ 2h, Miễn dịch -3.
        /// - Hương Mê Thảo (III): Ngủ sâu (Stamina 100%, Thể trạng +6, xóa Tải ngày), +1 Mệt Dội ngay, Lờ Đờ 3h, 10% Mộng Mị (ÁLSK +5).
        /// - Đan Hồi Nguyên (IV): Ngủ ngay (Stamina 100%, Thể trạng +10, xóa 2 tầng Mệt Dội), Hư Hỏa 2 ngày (Stamina tối đa -10%, Miễn dịch -10).
        /// - Quả Mộng (IV): Ngủ ngay (Stamina 100%, Thể trạng +8, xóa 1 tầng Mệt Dội), không tác dụng phụ, không tính nghỉ cưỡng bức.
        /// </summary>
        public (bool used, string message) UseSleepBypassItem(
            SleepBypassItemId item,
            GameClock clock,
            CharacterData character,
            Farm farm)
        {
            if (!SleepItemsInventory.TryGetValue(item, out int count) || count <= 0)
            {
                return (false, $"Không còn [{item}] trong hành trang!");
            }

            SleepItemsInventory[item] = count - 1;
            clock.SleepCooldownHoursRemaining = 0;

            if (item != SleepBypassItemId.QuaMong)
            {
                ForcedRestCountThisCycle++;
                if (ForcedRestCountThisCycle == 3)
                {
                    character.Stamina.ReboundFatigueStacks = Math.Min(4, character.Stamina.ReboundFatigueStacks + 1);
                }
                else if (ForcedRestCountThisCycle >= 4)
                {
                    character.Stamina.ReboundFatigueStacks = Math.Min(4, character.Stamina.ReboundFatigueStacks + 2);
                }
            }

            switch (item)
            {
                case SleepBypassItemId.TraLaVong:
                    character.Stamina.CurrentStamina = Math.Max(character.Stamina.CurrentStamina, character.GetEffectiveMaxStamina() * 0.50f);
                    character.Stamina.DailyLoad = Math.Min(character.Stamina.MaxStamina * 0.6f, character.Stamina.DailyLoad * 1.10f + 18f);
                    return (true, "Đã uống [Trà Lá Vông (Bậc I)]: Ngủ ngắn 3h bất chấp cooldown, Stamina >= 50%! (Tác dụng phụ: Không xóa Tải ngày, Tải ngày +10%).");

                case SleepBypassItemId.ThuocAnThan:
                    character.Stamina.DailyLoad = 0f;
                    character.Stamina.CurrentStamina = Math.Max(character.Stamina.CurrentStamina, character.GetEffectiveMaxStamina() * 0.80f);
                    character.Stamina.Immunity = Math.Max(0, character.Stamina.Immunity - 3);
                    return (true, "Đã dùng [Thuốc An Thần (Bậc II)]: Ngủ chuẩn đầy đủ, xóa Tải ngày! (Tác dụng phụ: Lờ Đờ 2h, Miễn dịch -3).");

                case SleepBypassItemId.HuongMeThao:
                    character.Stamina.DailyLoad = 0f;
                    character.Stamina.Condition = Math.Min(100, character.Stamina.Condition + 6);
                    character.Stamina.ReboundFatigueStacks = Math.Min(4, character.Stamina.ReboundFatigueStacks + 1);
                    character.Stamina.CurrentStamina = character.GetEffectiveMaxStamina();
                    farm.EventPressureIndex = Math.Min(100, farm.EventPressureIndex + 5);
                    return (true, "Đã đốt [Hương Mê Thảo (Bậc III)]: Ngủ sâu Stamina 100%, Thể trạng +6, xóa Tải ngày! (Tác dụng phụ: +1 Mệt Dội ngay, Lờ Đờ 3h, Mộng Mị ÁLSK +5).");

                case SleepBypassItemId.DanHoiNguyen:
                    character.Stamina.DailyLoad = 0f;
                    character.Stamina.Condition = Math.Min(100, character.Stamina.Condition + 10);
                    character.Stamina.ReboundFatigueStacks = Math.Max(0, character.Stamina.ReboundFatigueStacks - 2);
                    character.Stamina.Immunity = Math.Max(0, character.Stamina.Immunity - 10);
                    character.Stamina.CurrentStamina = character.GetEffectiveMaxStamina();
                    return (true, "Đã nuốt [Đan Hồi Nguyên (Bậc IV)]: Stamina 100%, Thể trạng +10, xóa 2 tầng Mệt Dội! (Tác dụng phụ: Hư Hỏa 2 ngày, Miễn dịch -10).");

                case SleepBypassItemId.QuaMong:
                    character.Stamina.DailyLoad = 0f;
                    character.Stamina.Condition = Math.Min(100, character.Stamina.Condition + 8);
                    character.Stamina.ReboundFatigueStacks = Math.Max(0, character.Stamina.ReboundFatigueStacks - 1);
                    character.Stamina.CurrentStamina = character.GetEffectiveMaxStamina();
                    character.CurrentGuard = character.MaxGuard;
                    return (true, "Đã thưởng thức [Quả Mộng · Vườn Mười Hai Quả (Bậc IV)]: Stamina 100%, Thể trạng +8, xóa 1 tầng Mệt Dội, KHÔNG tác dụng phụ, không tính nghỉ cưỡng bức!");

                default:
                    return (false, "Vật phẩm không hợp lệ.");
            }
        }

        /// <summary>
        /// Phòng Nghiên Cứu (Sổ tay 6.2.1): Thuần Hóa Huyết Thống (1.200 Vàng, 24h) giúp cá thể Xích Mao hoàn tất thuần dưỡng chiến đấu ngay lập tức.
        /// </summary>
        public (bool success, string message) PerformBloodlineDomesticationAtLab(Pig pig, Farm farm)
        {
            if (pig == null || !pig.IsAlive || pig.GeneLine != GeneLineId.XichMao)
                return (false, "Chỉ cá thể thuộc dòng gen Xích Mao mới cần Thuần Hóa Huyết Thống.");
            if (pig.IsXichMaoDomesticated)
                return (false, $"[{pig.Name}] đã được thuần hóa hoàn toàn.");
            if (!farm.SpendGold(1200))
                return (false, "Cần 1.200 Vàng để thực hiện Thuần Hóa Huyết Thống (24h) tại Phòng Nghiên Cứu.");

            pig.XichMaoDomesticationDays = 3;
            pig.IsXichMaoDomesticated = true;
            pig.Bonding = Math.Min(100f, pig.Bonding + 15f);
            return (true, $"[PHÒNG NGHIÊN CỨU · THUẦN HÓA HUYẾT THỐNG] Đã chi 1.200G + Cám Hạ Hỏa thuần hóa hoàn tất chiến trư Xích Mao [{pig.Name}]!");
        }

        /// <summary>
        /// Phòng Nghiên Cứu (Sổ tay 6.2.1): Nghiên cứu mẫu Hư Thể để chiết xuất Tinh Hoa Dị Biến và phong ấn tính trạng xấu.
        /// </summary>
        public (bool success, string message) ExtractDeviantEssenceAtLab(Pig huThePig, Farm farm)
        {
            if (huThePig == null || !huThePig.IsAlive || huThePig.GeneLine != GeneLineId.HuThe)
                return (false, "Cần 1 cá thể Hư Thể còn sống để chiết xuất Tinh Hoa Dị Biến.");
            if (!farm.SpendGold(300))
                return (false, "Cần 300 Vàng chi phí vận hành Phòng Nghiên Cứu.");

            DeviantEssenceCount += 1;
            huThePig.SealDeviantTrait();
            return (true, $"[PHÒNG NGHIÊN CỨU] Đã chiết xuất +1 Tinh Hoa Dị Biến (hiện có: {DeviantEssenceCount}) và phong ấn tính trạng xấu cho [{huThePig.Name}]!");
        }

        /// <summary>
        /// Xưởng Cường Hóa — Bàn Rã Giấy (Sổ tay 6.2.1): Rã Giấy Chuyên Biệt dư thừa thành Bụi Cường Hóa và quy đổi thành Đá Cường Hóa.
        /// </summary>
        public (int dustGained, int stonesCrafted, string message) SalvageSpecialtyPaperAtWorkshop(ItemTier paperTier)
        {
            int dust = paperTier switch
            {
                ItemTier.I => 2,
                ItemTier.II => 4,
                ItemTier.III => 8,
                ItemTier.IV => 15,
                _ => 2
            };
            EnhancementDustCount += dust;
            int newStones = EnhancementDustCount / 4;
            if (newStones > 0)
            {
                EnhancementDustCount %= 4;
                EnhancementStonesCount += newStones;
            }
            return (dust, newStones, $"[XƯỞNG CƯỜNG HÓA · BÀN RÃ GIẤY] Thu hồi +{dust} Bụi Cường Hóa, ghép được +{newStones} Đá Cường Hóa (Tổng đá: {EnhancementStonesCount}).");
        }

        /// <summary>
        /// Kiểm tra điều kiện mở Cổng Giữa Game (GDD 10.2: Bắt buộc >= 8 đợt lớn + 1 trong 5 điều kiện phụ)
        /// và mở khóa tuần tự 6 khóa Tính chất chuyên biệt ẩn (GDD 10.4: tên & hiệu ứng ẩn '???' cho tới khi hoàn tất cả 6 khóa).
        /// </summary>
        public (bool unlocked, string message) EvaluateMidGameGateAndLocks(
            Farm farm,
            HerdManager herd,
            int majorWavesSurvived,
            int? peakHerdCount = null,
            int? rareGenesIdentified = null,
            int? cycleBossesDefeated = null,
            int? activeDefenseBuildings = null)
        {
            int effectivePeakHerd = peakHerdCount ?? PeakHerdCountObserved;
            int effectiveRareGenes = rareGenesIdentified ?? RareOrHigherGenesIdentifiedCount;
            int effectiveBosses = cycleBossesDefeated ?? CycleBossesDefeatedCount;
            int effectiveBuildings = activeDefenseBuildings ?? ActiveDefenseBuildingsCount;

            var gateCheck = CanonicalSagaAndGdd6Systems.EvaluateCanonicalMidGameGateGdd102(
                majorWavesSurvived,
                farm.FarmLevel,
                effectivePeakHerd,
                effectiveRareGenes,
                effectiveBosses,
                effectiveBuildings);

            if (gateCheck.unlocked)
            {
                MidGameGateUnlocked = true;
            }

            int targetLocks = 0;
            if (farm.FarmLevel >= 2) targetLocks = 1;
            if (farm.FarmLevel >= 4) targetLocks = 2;
            if (MidGameGateUnlocked) targetLocks = 3;
            if (MidGameGateUnlocked && herd.Tier >= HerdStateTier.Vung) targetLocks = 4;
            if (MidGameGateUnlocked && majorWavesSurvived >= 8) targetLocks = 5;
            if (MidGameGateUnlocked && herd.Tier == HerdStateTier.HungThinh && majorWavesSurvived >= 10) targetLocks = 6;

            if (targetLocks > UnlockedSpecialtyLocksCount)
            {
                UnlockedSpecialtyLocksCount = targetLocks;
                return (MidGameGateUnlocked, $"Đã mở khóa Tính Chất Chuyên Biệt Ẩn bậc {UnlockedSpecialtyLocksCount}/6 (Hiển thị: {SpecialtyTraitDisplayName})! (Cổng Giữa Game: {(MidGameGateUnlocked ? "ĐÃ MỞ" : "Chưa mở")}).");
            }

            return (MidGameGateUnlocked, $"Trạng thái Cổng Giữa Game: {(MidGameGateUnlocked ? "ĐÃ MỞ" : "Chưa mở")} | Khóa Chuyên Biệt: {UnlockedSpecialtyLocksCount}/6 ({SpecialtyTraitDisplayName}).");
        }

        /// <summary>
        /// Gia cường Giấy Chuyên Biệt từ +0 lên +3 theo đúng GDD 10.6:
        /// - +0 -> +1: 100% (Đủ nguyên liệu, không cần Đá Cường Hóa)
        /// - +1 -> +2: 65% gốc + 6%/Đá Cường Hóa (tối đa 5 viên -> trần 95%)
        /// - +2 -> +3: 40% gốc + 5%/Đá Cường Hóa (tối đa 8 viên -> trần cứng 80% không bao giờ vượt)
        /// - Thiếu nguyên liệu: tỉ lệ × (nộp/yêu cầu)^1.5, sàn 10%. Thất bại chỉ mất nguyên liệu và đá, không hỏng giấy.
        /// </summary>
        public (bool success, string message) EnhanceSpecialtyPaper(
            double? deterministicRoll = null,
            int? stonesToUse = null,
            float materialProvidedRatio = 1.0f)
        {
            if (intEquippedPaperEnhancementLevel >= 3)
                return (false, "Giấy Chuyên Biệt đã đạt cấp cường hóa tối đa (+3)!");

            int nextLevel = intEquippedPaperEnhancementLevel + 1;
            int maxUsefulStones = nextLevel switch
            {
                1 => 0,
                2 => 5,
                3 => 8,
                _ => 0
            };

            int requestedStones = stonesToUse ?? Math.Min(EnhancementStonesCount, maxUsefulStones);
            int actualStones = Math.Clamp(requestedStones, 0, Math.Min(EnhancementStonesCount, maxUsefulStones));
            EnhancementStonesCount -= actualStones;

            float baseRate = nextLevel switch
            {
                1 => 1.00f,
                2 => Math.Min(0.95f, 0.65f + 0.06f * actualStones),
                3 => Math.Min(0.80f, 0.40f + 0.05f * actualStones), // Trần cứng 80% theo GDD 10.6
                _ => 0.80f
            };

            float successRate = baseRate;
            if (materialProvidedRatio < 1.0f)
            {
                float clampedRatio = Math.Clamp(materialProvidedRatio, 0f, 1f);
                successRate = Math.Max(0.10f, baseRate * (float)Math.Pow(clampedRatio, 1.5));
                if (nextLevel == 3) successRate = Math.Min(0.80f, successRate);
            }

            double roll = deterministicRoll ?? new Random(1337 + nextLevel * 31 + EnhancementStonesCount).NextDouble();
            if (roll <= successRate)
            {
                intEquippedPaperEnhancementLevel = nextLevel;
                return (true, $"Cường hóa Giấy Chuyên Biệt [{EquippedPaperColor}] thành công lên +{intEquippedPaperEnhancementLevel} (dùng {actualStones} đá, tỉ lệ {successRate * 100f:0.#}%)!");
            }

            return (false, $"Cường hóa Giấy Chuyên Biệt lên +{nextLevel} thất bại (dùng {actualStones} đá, tỉ lệ {successRate * 100f:0.#}%). Giữ nguyên cấp +{intEquippedPaperEnhancementLevel} (không hỏng giấy).");
        }

        /// <summary>
        /// Tạo báo cáo Tình Báo Đợt Quái theo 6 bậc (Bậc 0 đến Bậc 5: WHAT / WHEN / WHAT TO HOLD - GDD 14.3).
        /// </summary>
        public string BuildWaveIntelReport(int hoursUntilWave, string primaryDirection = "Đông & Bắc")
        {
            return CurrentIntelTier switch
            {
                0 => "[Tình báo Bậc 0 · Mù]: Chưa có thông tin hướng hay thời gian quái tập kích.",
                1 => $"[Tình báo Bậc 1 · Sơ bộ]: Ngỗng Báo Động kêu hướng {primaryDirection}. Khoảng {hoursUntilWave} giờ nữa có động.",
                2 => $"[Tình báo Bậc 2 · Hướng & Quy mô]: Quái tập trung hướng {primaryDirection} sau {hoursUntilWave}h. Có Kẻ Cào Phá và Kẻ Bắt Heo.",
                3 => $"[Tình báo Bậc 3 · WHAT & WHEN]: Đợt tập kích sau đúng {hoursUntilWave}h tại {primaryDirection}. Có Thú Bão đi kèm phá thiết bị điện.",
                4 => $"[Tình báo Bậc 4 · Chi tiết Tinh Anh]: Đợt tập kích {primaryDirection} ({hoursUntilWave}h) có UFO Trinh Sát chiếu chùm kéo 6s. Khuyến nghị nạp sẵn Nỏ Xuyên Vân.",
                _ => $"[Tình báo Bậc 5 · Thiên Nhãn Toàn Cảnh (WHAT / WHEN / WHAT TO HOLD)]: Đợt {primaryDirection} sau {hoursUntilWave}h00p. Kẻ địch nhắm vào Thủ Lĩnh Đàn & Trạm Giếng Cạn — CẦN GIỮ LẠI 50 đạn Cửu Tiêu Lôi Pháo & Lùa Đàn về Nơi Trú trước 1 giờ!"
            };
        }

        public WaveBattleReport RecordWaveBattleReport(
            int waveNumber,
            bool victory,
            int enemiesDefeated,
            int pigsLost,
            int ammoSpent,
            int durabilityLost,
            int playerGuardBreaks,
            int goldReward)
        {
            var rep = new WaveBattleReport
            {
                WaveNumber = waveNumber,
                Victory = victory,
                EnemiesDefeated = enemiesDefeated,
                PigsLostOrAbducted = pigsLost,
                AmmoSpent = ammoSpent,
                BuildingDurabilityLost = durabilityLost,
                PlayerGuardBreaks = playerGuardBreaks,
                GoldRewarded = goldReward,
                SummaryText = $"[BÁO CÁO ĐỢT {waveNumber}] {(victory ? "THẮNG LỢI" : "THẤT THỦ")} | Hạ {enemiesDefeated} quái | Heo mất: {pigsLost} | Đạn tiêu hao: {ammoSpent} | Hao bền công trình: -{durabilityLost} | Ngất/Vỡ chống chịu: {playerGuardBreaks} | Thưởng: +{goldReward}G"
            };
            BattleReports.Add(rep);
            return rep;
        }
    }
}

