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
        public StoryChapterId CurrentChapter { get; set; } = StoryChapterId.Ch0_GiayChungNhan;
        public StoryEndingId ChosenEnding { get; set; } = StoryEndingId.ChuaChon;
        public bool PostCreditTriggered { get; set; } = false;
        public bool SeventhChannelCut { get; set; } = false;
        public bool HerdEmptySpotObserved { get; set; } = false;
        public bool WhiteFootedPigPrankSeen { get; set; } = false;
        public int ClockOffsetMinutes { get; set; } = 2; // Đồng hồ nhà chính luôn nhanh 2 phút (07:02 vs 07:00)

        public List<NotebookTwoColumnEntry> NotebookEntries { get; set; } = new List<NotebookTwoColumnEntry>();
        public List<StoryQuestRecord> Quests { get; set; } = new List<StoryQuestRecord>();
        public Dictionary<string, int> NpcTrustTier { get; set; } = new Dictionary<string, int>();

        public StoryJournalSystem()
        {
            InitNpcTrust();
            InitNotebookAndQuests();
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

        /// <summary>
        /// Chọn 1 trong 3 Kết thúc Mùa 1 (Mục 8.1, 8.2, 8.3) + Hậu tín dụng 8.5 (COT_TRUYEN_PIG_TYCOON.md).
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
                title = "KẾT THÚC 8.1 · GIỮ TRẠI, THÁO ĐƯỜNG GỬI";
                epilogue = "Khoa dùng kìm cộng lực cắt đứt sợi cáp đồng trục số 7 dưới đáy giếng cạn lúc 02:02. Đèn xanh trên bầu trời Bờ Rìa tắt hẳn. Sáng hôm sau, đồng hồ trên tường lần đầu tiên chạy đúng 07:00. Đàn heo ùa ra sân cỏ, không còn chừa chỗ trống góc Tây Bắc nữa — vì Khoa đã thật sự đứng ở đó.";
            }
            else if (ending == StoryEndingId.Ending_8_2_GiuMayLamHoSo)
            {
                SeventhChannelCut = false;
                title = "KẾT THÚC 8.2 · GIỮ MÁY, LÀM HỒ SƠ";
                epilogue = "Khoa và An giữ nguyên đường truyền dưới giếng nhưng đấu nối bộ ghi băng của Tám Vá để sao lưu toàn bộ tín hiệu gửi về Trạm Bạch Vân. Cuốn Sổ Hai Cột giờ đã kín đặc cột 'Có thật', trở thành tập hồ sơ bằng chứng đầu tiên phơi bày dự án 'Sáu Với Một'.";
            }
            else
            {
                SeventhChannelCut = true;
                title = "KẾT THÚC 8.3 · MANG VÒNG CỔ VỀ NHÀ";
                epilogue = "Khoa trao lại chùm chìa khóa dây cước đỏ cho Mận và Tám Vá, bỏ chiếc vòng cổ đồng số 19 cùng tấm ảnh năm 2018 vào túi áo. Chuyến xe đò 06:15 rời Bờ Rìa đi về hướng núi Bạch Vân — nơi có tiếng quạt thông gió mà chú Minh từng nhắc.";
            }

            string postCredit = "[HẬU TÍN DỤNG 8.5 · NGƯỜI TỚI SỚM] Đồng hồ điểm 07:00 (sớm 2 phút). Có tiếng gõ cửa đúng 'sáu với một' nhịp. Dưới khe cửa nhà chính là tờ biên lai mới còn ấm: 'Kiện 6+1: Đã nhận trước khi gửi — Hẹn gặp ở Mùa 2'.";
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
    public class PowerGridState
    {
        public int GeneratorLevel = 1; // Cấp 1: 30/60u, Cấp 2: 70/130u, Cấp 3: 140/240u
        public int CapacitorCount = 1; // Mỗi Tụ Điện +60u dự trữ
        public float CapacitorChargeUnits = 60f;
        public float CurrentDemandUnits = 18f;
        public bool IsBreakerTripped = false;
        public float BreakerTripRemainingSeconds = 0f;

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
    /// Hệ thống Cơ Chế Chuyên Sâu GDD v6.0:
    /// - Lưới Điện (Generator 30/70/140, Peak 60/130/240, Tụ Điện 60u, Sập cầu dao >140%)
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
        public int CurrentIntelTier { get; set; } = 1; // Bậc 0..5 (GDD 14.3)
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
        /// Sử dụng 1 trong 5 vật phẩm xóa hồi chờ ngủ 6h (GDD 9.5):
        /// Chỉ duy nhất Quả Mộng không để lại tác dụng phụ hôm sau.
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

            switch (item)
            {
                case SleepBypassItemId.TraLaVong:
                    character.Stamina.DailyLoad = Math.Min(character.Stamina.MaxStamina * 0.6f, character.Stamina.DailyLoad + 15f);
                    return (true, "Đã uống [Trà Lá Vông]: Xóa hồi chờ ngủ 6h! (Tác dụng phụ: +15 Tích tải thể lực ngày mai).");

                case SleepBypassItemId.ThuocAnThan:
                    character.Stamina.Condition = Math.Max(20, character.Stamina.Condition - 10);
                    return (true, "Đã dùng [Thuốc An Thần]: Xóa hồi chờ ngủ 6h! (Tác dụng phụ: -10 Thể trạng do uể oải).");

                case SleepBypassItemId.HuongMeThao:
                    farm.EventPressureIndex = Math.Min(100, farm.EventPressureIndex + 6);
                    return (true, "Đã đốt [Hương Mê Thảo]: Xóa hồi chờ ngủ 6h! (Tác dụng phụ: +6 Áp lực Sự kiện do mùi hương dẫn dụ).");

                case SleepBypassItemId.DanHoiNguyen:
                    character.Stamina.ReboundFatigueStacks += 1;
                    character.Stamina.CurrentStamina = character.GetEffectiveMaxStamina();
                    return (true, "Đã nuốt [Đan Hồi Nguyên]: Xóa hồi chờ ngủ 6h & hồi đầy Thể lực! (Tác dụng phụ: +1 tầng Mệt Dội Ngược).");

                case SleepBypassItemId.QuaMong:
                    character.Stamina.ReboundFatigueStacks = 0;
                    character.Stamina.DailyLoad = 0f;
                    character.Stamina.Condition = 100;
                    character.Stamina.CurrentStamina = character.Stamina.MaxStamina;
                    character.CurrentGuard = character.MaxGuard;
                    return (true, "Đã thưởng thức [Quả Mộng · Vườn Mười Hai Quả]: Xóa hồi chờ ngủ 6h, hồi 100% Thể lực & Chống chịu, KHÔNG có tác dụng phụ!");

                default:
                    return (false, "Vật phẩm không hợp lệ.");
            }
        }

        /// <summary>
        /// Kiểm tra điều kiện mở Cổng Giữa Game (GDD 20) và mở khóa tuần tự 6 khóa Tính chất chuyên biệt ẩn (GDD 8.2).
        /// </summary>
        public (bool unlocked, string message) EvaluateMidGameGateAndLocks(Farm farm, HerdManager herd, int majorWavesSurvived)
        {
            if (farm.FarmLevel >= 5 && majorWavesSurvived >= 2 && herd.Tier >= HerdStateTier.SoKhai)
            {
                MidGameGateUnlocked = true;
            }

            int targetLocks = 0;
            if (farm.FarmLevel >= 2) targetLocks = 1;
            if (farm.FarmLevel >= 4) targetLocks = 2;
            if (MidGameGateUnlocked) targetLocks = 3;
            if (MidGameGateUnlocked && herd.Tier >= HerdStateTier.Vung) targetLocks = 4;
            if (MidGameGateUnlocked && majorWavesSurvived >= 4) targetLocks = 5;
            if (MidGameGateUnlocked && herd.Tier == HerdStateTier.HungThinh && majorWavesSurvived >= 5) targetLocks = 6;

            if (targetLocks > UnlockedSpecialtyLocksCount)
            {
                UnlockedSpecialtyLocksCount = targetLocks;
                return (true, $"Đã mở khóa Tính Chất Chuyên Biệt Ẩn bậc {UnlockedSpecialtyLocksCount}/6! (Cổng Giữa Game: {(MidGameGateUnlocked ? "ĐÃ MỞ" : "Chưa mở")}).");
            }

            return (MidGameGateUnlocked, $"Trạng thái Cổng Giữa Game: {(MidGameGateUnlocked ? "ĐÃ MỞ" : "Chưa mở")} | Khóa Chuyên Biệt: {UnlockedSpecialtyLocksCount}/6.");
        }

        /// <summary>
        /// Cường hóa Giấy Chuyên Biệt từ +0 lên +3 bằng Đá Cường Hóa (GDD 8.2: trần tỉ lệ thành công +3 là 80%).
        /// </summary>
        public (bool success, string message) EnhanceSpecialtyPaper(double? deterministicRoll = null)
        {
            if (intEquippedPaperEnhancementLevel >= 3)
                return (false, "Giấy Chuyên Biệt đã đạt cấp cường hóa tối đa (+3)!");

            if (EnhancementStonesCount <= 0)
                return (false, "Không đủ Đá Cường Hóa!");

            EnhancementStonesCount -= 1;
            int nextLevel = intEquippedPaperEnhancementLevel + 1;
            float successRate = nextLevel switch
            {
                1 => 1.00f,
                2 => 0.90f,
                3 => 0.80f, // Trần cứng 80% theo GDD 8.2
                _ => 0.80f
            };

            double roll = deterministicRoll ?? new Random().NextDouble();
            if (roll <= successRate)
            {
                intEquippedPaperEnhancementLevel = nextLevel;
                return (true, $"Cường hóa Giấy Chuyên Biệt [{EquippedPaperColor}] thành công lên +{intEquippedPaperEnhancementLevel} (tỉ lệ {successRate * 100f:0}%)!");
            }

            return (false, $"Cường hóa Giấy Chuyên Biệt lên +{nextLevel} thất bại (trần tỉ lệ {successRate * 100f:0}%). Giữ nguyên cấp +{intEquippedPaperEnhancementLevel}.");
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
