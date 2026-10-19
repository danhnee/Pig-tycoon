using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    public enum TransactionType
    {
        Income,     // Thu (cộng tiền vào tài khoản)
        Expense     // Chi (trừ tiền khỏi tài khoản)
    }

    public enum TransactionCategory
    {
        BanHeo,                 // Bán heo tại chợ ngày
        MuaCam,                 // Mua thức ăn / cám chăn nuôi
        XayDung,                // Xây dựng chuồng trại, công trình
        SuaChua,                // Sửa chữa hàng rào, công trình
        LuongNPC,               // Trả lương cho NPC định kỳ
        KiemDich,               // Phí kiểm dịch thú y
        GiamDinh,               // Phí định danh giống heo quý
        NapPhongThu,            // Chi phí nạp đạn nỏ / lôi pháo
        BaoTriTrangTrai,        // Phí bảo dưỡng hạ tầng chuồng trại hàng ngày
        ThuongRaid,             // Thưởng vàng khi đánh lui đợt quái
        ThuongNhiemVu,          // Tiền thưởng hoàn thành nhiệm vụ
        DauGia,                 // Đấu giá bản vẽ / vật phẩm chợ đêm
        Khac                    // Thu chi phát sinh khác
    }

    public enum FeedTypeId
    {
        CamTieuChuan,   // Cám Tiêu Chuẩn (4G/kg) - Tăng trọng ổn định, no lâu
        CamTinh,        // Cám Tinh Đậm Đặc (8G/kg) - Thúc tăng trọng nhanh +20%
        CamDuocLieu,    // Cám Dược Liệu (15G/kg) - Tăng đề kháng, giảm nguy cơ mắc bệnh
        CamLenMen       // Cám Ngũ Cốc Lên Men (12G/kg) - Tăng Tinh thần (Mood) & Gắn kết (Bonding)
    }

    [Serializable]
    public class FeedDefinition
    {
        public FeedTypeId Id { get; set; }
        public string Name { get; set; } = "";
        public int PricePerKgGold { get; set; }
        public string Description { get; set; } = "";
        public string EffectSummary { get; set; } = "";
    }

    public static class FeedDatabase
    {
        private static readonly Dictionary<FeedTypeId, FeedDefinition> Feeds = new Dictionary<FeedTypeId, FeedDefinition>
        {
            {
                FeedTypeId.CamTieuChuan,
                new FeedDefinition
                {
                    Id = FeedTypeId.CamTieuChuan,
                    Name = "Cám Tiêu Chuẩn",
                    PricePerKgGold = 4,
                    Description = "Cám dinh dưỡng cơ bản từ ngô và cám gạo, cung cấp năng lượng đầy đủ mỗi ngày.",
                    EffectSummary = "Tăng trọng +1.0kg/ngày, duy trì chỉ số no."
                }
            },
            {
                FeedTypeId.CamTinh,
                new FeedDefinition
                {
                    Id = FeedTypeId.CamTinh,
                    Name = "Cám Tinh Đậm Đặc",
                    PricePerKgGold = 8,
                    Description = "Hàm lượng đạm cao kết hợp bột cá và đậu tương, tối ưu hóa quá trình vỗ béo heo thịt.",
                    EffectSummary = "Tăng trọng +20% (+1.2kg/ngày), kích thích phát triển cơ bắp."
                }
            },
            {
                FeedTypeId.CamDuocLieu,
                new FeedDefinition
                {
                    Id = FeedTypeId.CamDuocLieu,
                    Name = "Cám Dược Liệu",
                    PricePerKgGold = 15,
                    Description = "Bổ sung thảo mộc thiên nhiên (xuyên tâm liên, cam thảo) giúp thanh lọc cơ thể heo.",
                    EffectSummary = "Tăng đề kháng +25%, giảm 50% nguy cơ nhiễm dịch bệnh."
                }
            },
            {
                FeedTypeId.CamLenMen,
                new FeedDefinition
                {
                    Id = FeedTypeId.CamLenMen,
                    Name = "Cám Ngũ Cốc Lên Men",
                    PricePerKgGold = 12,
                    Description = "Ủ men vi sinh thơm dịu kích thích tiêu hóa, tạo cảm giác thư giãn cho đàn heo.",
                    EffectSummary = "Tăng Tinh thần (Mood +15), tăng Gắn kết (Bonding +10)."
                }
            }
        };

        public static FeedDefinition GetFeed(FeedTypeId type)
        {
            if (Feeds.TryGetValue(type, out var def)) return def;
            return Feeds[FeedTypeId.CamTieuChuan];
        }

        public static IEnumerable<FeedDefinition> GetAllFeeds() => Feeds.Values;
    }

    [Serializable]
    public class FarmFeedInventory
    {
        public int FeedStandardKg { get; set; } = 150;
        public int FeedConcentratedKg { get; set; } = 40;
        public int FeedMedicinalKg { get; set; } = 20;
        public int FeedFermentedKg { get; set; } = 20;

        public int GetStock(FeedTypeId type)
        {
            return type switch
            {
                FeedTypeId.CamTieuChuan => FeedStandardKg,
                FeedTypeId.CamTinh => FeedConcentratedKg,
                FeedTypeId.CamDuocLieu => FeedMedicinalKg,
                FeedTypeId.CamLenMen => FeedFermentedKg,
                _ => 0
            };
        }

        public void AddStock(FeedTypeId type, int kg)
        {
            if (kg <= 0) return;
            switch (type)
            {
                case FeedTypeId.CamTieuChuan: FeedStandardKg += kg; break;
                case FeedTypeId.CamTinh: FeedConcentratedKg += kg; break;
                case FeedTypeId.CamDuocLieu: FeedMedicinalKg += kg; break;
                case FeedTypeId.CamLenMen: FeedFermentedKg += kg; break;
            }
        }

        public bool ConsumeStock(FeedTypeId type, int kg)
        {
            if (kg <= 0) return true;
            if (GetStock(type) < kg) return false;

            switch (type)
            {
                case FeedTypeId.CamTieuChuan: FeedStandardKg -= kg; break;
                case FeedTypeId.CamTinh: FeedConcentratedKg -= kg; break;
                case FeedTypeId.CamDuocLieu: FeedMedicinalKg -= kg; break;
                case FeedTypeId.CamLenMen: FeedFermentedKg -= kg; break;
            }
            return true;
        }

        /// <summary>
        /// Tính tổng giá trị quy đổi kho cám (GTTC) theo đơn giá tham chiếu thị trường.
        /// </summary>
        public int CalculateTotalFeedValueGold()
        {
            return (FeedStandardKg * FeedDatabase.GetFeed(FeedTypeId.CamTieuChuan).PricePerKgGold) +
                   (FeedConcentratedKg * FeedDatabase.GetFeed(FeedTypeId.CamTinh).PricePerKgGold) +
                   (FeedMedicinalKg * FeedDatabase.GetFeed(FeedTypeId.CamDuocLieu).PricePerKgGold) +
                   (FeedFermentedKg * FeedDatabase.GetFeed(FeedTypeId.CamLenMen).PricePerKgGold);
        }
    }

    [Serializable]
    public class LedgerEntry
    {
        public string TransactionId { get; set; } = "";
        public int DayNumber { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public TransactionType Type { get; set; }
        public TransactionCategory Category { get; set; }
        public int AmountGold { get; set; }
        public int BalanceAfter { get; set; }
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    [Serializable]
    public class DailyFinancialSummary
    {
        public int DayNumber { get; set; }
        public int TotalIncomeGold { get; set; }
        public int TotalExpenseGold { get; set; }
        public int NetProfitGold => TotalIncomeGold - TotalExpenseGold;
        public int TransactionCount { get; set; }
        public Dictionary<TransactionCategory, int> IncomeByCategory { get; set; } = new Dictionary<TransactionCategory, int>();
        public Dictionary<TransactionCategory, int> ExpenseByCategory { get; set; } = new Dictionary<TransactionCategory, int>();
    }

    [Serializable]
    public class CycleFinancialSummary
    {
        public int StartDay { get; set; }
        public int EndDay { get; set; }
        public int GrossFarmIncome { get; set; }            // Bán heo + thưởng
        public int FeedCost { get; set; }                   // Tiền cám
        public int InfrastructureMaintenanceCost { get; set; } // Duy trì chuồng trại
        public int NpcWages { get; set; }                   // Lương NPC
        public int DefensePreloadCost { get; set; }         // Nạp phòng thủ
        public int IntelAndQuarantineCost { get; set; }     // Tình báo + kiểm dịch + giám định
        public int RepairsAndOtherCost { get; set; }        // Sửa chữa + xây dựng + khác
        public int TotalIncome => GrossFarmIncome;
        public int TotalExpense => FeedCost + InfrastructureMaintenanceCost + NpcWages + DefensePreloadCost + IntelAndQuarantineCost + RepairsAndOtherCost;
        public int NetSurplus => TotalIncome - TotalExpense;
    }

    [Serializable]
    public class DualBudgetReport
    {
        // 1. Dòng tiền Vàng (Cash Budget)
        public int CashGoldBalance { get; set; }
        public int TodayIncomeGold { get; set; }
        public int TodayExpenseGold { get; set; }
        public int NetDailyCashflowGold => TodayIncomeGold - TodayExpenseGold;

        // 2. Kho Giá Trị (Value Inventory - GTTC)
        public int TotalHerdEstimatedValueGold { get; set; }
        public int TotalFeedStockValueGold { get; set; }
        public int TotalMaterialsValueGold { get; set; }
        public int TotalInventoryValueGold => TotalHerdEstimatedValueGold + TotalFeedStockValueGold + TotalMaterialsValueGold;

        // Tổng tài sản ròng (Net Worth)
        public int TotalNetWorthGold => CashGoldBalance + TotalInventoryValueGold;
    }

    [Serializable]
    public class FinancialLedger
    {
        public List<LedgerEntry> Entries { get; private set; } = new List<LedgerEntry>();

        public event Action<LedgerEntry>? OnTransactionRecorded;

        public LedgerEntry RecordTransaction(
            int day,
            int hour,
            int minute,
            TransactionType type,
            TransactionCategory category,
            int amountGold,
            int balanceAfter,
            string description)
        {
            int sanitizedAmount = Math.Max(0, amountGold);

            var entry = new LedgerEntry
            {
                TransactionId = $"tx_{Guid.NewGuid():N}",
                DayNumber = day,
                Hour = hour,
                Minute = minute,
                Type = type,
                Category = category,
                AmountGold = sanitizedAmount,
                BalanceAfter = balanceAfter,
                Description = description ?? "",
                Timestamp = DateTime.UtcNow
            };

            Entries.Add(entry);
            OnTransactionRecorded?.Invoke(entry);
            return entry;
        }

        public DailyFinancialSummary GetDailySummary(int day)
        {
            var summary = new DailyFinancialSummary
            {
                DayNumber = day
            };

            foreach (var entry in Entries)
            {
                if (entry.DayNumber != day) continue;

                summary.TransactionCount++;

                if (entry.Type == TransactionType.Income)
                {
                    summary.TotalIncomeGold += entry.AmountGold;
                    if (!summary.IncomeByCategory.ContainsKey(entry.Category))
                        summary.IncomeByCategory[entry.Category] = 0;
                    summary.IncomeByCategory[entry.Category] += entry.AmountGold;
                }
                else
                {
                    summary.TotalExpenseGold += entry.AmountGold;
                    if (!summary.ExpenseByCategory.ContainsKey(entry.Category))
                        summary.ExpenseByCategory[entry.Category] = 0;
                    summary.ExpenseByCategory[entry.Category] += entry.AmountGold;
                }
            }

            return summary;
        }

        public CycleFinancialSummary GetCycleSummary(int startDay, int endDay)
        {
            var cycle = new CycleFinancialSummary
            {
                StartDay = startDay,
                EndDay = endDay
            };

            foreach (var entry in Entries)
            {
                if (entry.DayNumber < startDay || entry.DayNumber > endDay) continue;

                if (entry.Type == TransactionType.Income)
                {
                    cycle.GrossFarmIncome += entry.AmountGold;
                }
                else
                {
                    switch (entry.Category)
                    {
                        case TransactionCategory.MuaCam:
                            cycle.FeedCost += entry.AmountGold;
                            break;
                        case TransactionCategory.BaoTriTrangTrai:
                            cycle.InfrastructureMaintenanceCost += entry.AmountGold;
                            break;
                        case TransactionCategory.LuongNPC:
                            cycle.NpcWages += entry.AmountGold;
                            break;
                        case TransactionCategory.NapPhongThu:
                            cycle.DefensePreloadCost += entry.AmountGold;
                            break;
                        case TransactionCategory.KiemDich:
                        case TransactionCategory.GiamDinh:
                            cycle.IntelAndQuarantineCost += entry.AmountGold;
                            break;
                        default:
                            cycle.RepairsAndOtherCost += entry.AmountGold;
                            break;
                    }
                }
            }

            return cycle;
        }
    }
}
