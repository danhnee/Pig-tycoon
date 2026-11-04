using System;

namespace PigTycoon.Core
{
    /// <summary>
    /// Đàn bàn giao của ông Năm khi bắt đầu ván mới. Truyện chỉ ghi "một đàn nhỏ" (COT_TRUYEN, cảnh ký nhận trại) và GDD 2
    /// ghi đầu game "đàn 10–80 con"; không tài liệu nào cho con số chính xác, nên giữ 12 con như bản dựng trước (ADR-0019, PROVISIONAL).
    /// Giữ 2 con cũ của bản lưu mẫu (Hồng Điền #1, Kim Thọ #1); 10 con còn lại là heo trưởng thành dòng Thường, đực/cái xen kẽ,
    /// có con heo chân trắng của cốt truyện.
    /// </summary>
    public static class StarterHerd
    {
        public const int DefaultSize = 12;
        public const int AdultAgeDays = 20;
        public const float AdultWeightKg = 90f;
        public const string WhiteFootPigId = "pig_white_foot_2018";

        private static readonly GeneLineId[] CommonLines = { GeneLineId.HongDien, GeneLineId.LamKhe, GeneLineId.MocCuoc };

        /// <summary>Thêm heo cho tới khi đàn sống đạt <paramref name="targetSize"/>. Trả về số heo đã thêm.</summary>
        public static int FillTo(GameEngine engine, int targetSize = DefaultSize)
        {
            if (engine == null) return 0;
            int added = 0;
            int living = engine.LivingPigCount;

            if (living < targetSize && !engine.Pigs.Exists(p => p.Id == "p_1"))
            {
                var p1 = Pig.CreateDefault("p_1", "Hồng Điền #1", GeneLineId.HongDien, GeneRarity.Thuong, "Cai", AdultAgeDays, 100f);
                p1.Bonding = 65f;
                Add(engine, p1); added++; living++;
            }
            if (living < targetSize && !engine.Pigs.Exists(p => p.Id == "p_2"))
            {
                var p2 = Pig.CreateDefault("p_2", "Kim Thọ #1", GeneLineId.KimTho, GeneRarity.HuyenThoai, "Duc", AdultAgeDays, AdultWeightKg);
                p2.Bonding = 80f;
                Add(engine, p2); added++; living++;
            }
            if (living < targetSize && !engine.Pigs.Exists(p => p.Id == WhiteFootPigId))
            {
                var wf = Pig.CreateDefault(WhiteFootPigId, "Heo Chân Trắng", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", AdultAgeDays, AdultWeightKg);
                Add(engine, wf); added++; living++;
            }

            for (int i = 1; living < targetSize && i < 1000; i++)
            {
                string id = $"p_start_{i}";
                if (engine.Pigs.Exists(p => p.Id == id)) continue;
                var line = CommonLines[i % CommonLines.Length];
                string gender = i % 2 == 0 ? "Duc" : "Cai";
                var pig = Pig.CreateDefault(id, $"{GeneticsAndBreedingSystem.GetGeneProfile(line).Name} #{i + 1}", line, GeneRarity.Thuong,
                    gender, AdultAgeDays, AdultWeightKg);
                Add(engine, pig); added++; living++;
            }
            return added;
        }

        private static void Add(GameEngine engine, Pig pig)
        {
            engine.PrepareNewPig(pig);
            engine.Pigs.Add(pig);
        }
    }
}
