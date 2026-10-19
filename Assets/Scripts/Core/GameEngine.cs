using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameEngine
    {
        public GameClock Clock { get; private set; }
        public Farm Farm { get; private set; }
        public HerdManager Herd { get; private set; }
        public EconomyManager Economy { get; private set; }
        public DefenseManager Defense { get; private set; }
        public CharacterData Character { get; private set; }
        public List<Pig> Pigs { get; private set; }

        public WeatherType CurrentWeather { get; set; } = WeatherType.QuangDang;
        public int MajorWavesSurvived { get; set; } = 0;
        public int MinorWavesDefeated { get; set; } = 0;
        public int TotalPigsSold { get; set; } = 0;
        public int TotalGoldEarned { get; set; } = 0;

        public event Action<string> OnLogEmitted;

        public GameEngine()
        {
            Clock = new GameClock();
            Farm = new Farm();
            Herd = new HerdManager();
            Economy = new EconomyManager();
            Defense = new DefenseManager();
            Character = new CharacterData();
            Pigs = new List<Pig>();
        }

        public void Tick(int minutes = 1)
        {
            var tickResult = Clock.TickMinutes(minutes);

            if (Clock.GetTimeOfDay() == TimeOfDay.Toi && Clock.CurrentMinute == 0)
            {
                if (Economy.RollMysticMerchant(Clock.CurrentHour, Farm.EventPressureIndex))
                {
                    OnLogEmitted?.Invoke("Bí Nhân nửa người nửa quỷ đã xuất hiện sát ranh giới trại!");
                }
            }

            Economy.UpdateMysticMerchant(Clock.CurrentHour);

            if (tickResult.NewDayStarted)
            {
                OnDailyReset();
            }
        }

        private void OnDailyReset()
        {
            OnLogEmitted?.Invoke($"=== BẮT ĐẦU NGÀY {Clock.CurrentDay} (Chu kỳ {Clock.CycleNumber}) ===");

            var livingPigs = Pigs.FindAll(p => p.IsAlive);
            int corpsesOutside = Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
            var capReport = Farm.CalculateCapacityReport(livingPigs.Count, corpsesOutside);

            bool isInHerd = Herd.Tier != HerdStateTier.KhongCo;
            float densityPenalty = capReport.RawDensityRatio > 1.2f ? 0.08f : (capReport.RawDensityRatio > 1.5f ? 0.15f : 0f);

            foreach (var p in Pigs)
            {
                p.AdvanceDay(Farm.HabitatIndex, isInHerd, densityPenalty);
            }

            if (capReport.DailyEmptyMaintenanceCost > 0)
            {
                Farm.SpendGold(capReport.DailyEmptyMaintenanceCost);
                Economy.Ledger.RecordTransaction(
                    day: Clock.CurrentDay,
                    hour: Clock.CurrentHour,
                    minute: Clock.CurrentMinute,
                    type: TransactionType.Expense,
                    category: TransactionCategory.BaoTriTrangTrai,
                    amountGold: capReport.DailyEmptyMaintenanceCost,
                    balanceAfter: Farm.Gold,
                    description: $"Bảo trì hạ tầng chuồng trại ngày {Clock.CurrentDay}"
                );
            }

            Farm.UpdateDailyHabitat(1, corpsesOutside, 0, capReport.RawDensityRatio, true, true);

            int elderPigs = livingPigs.FindAll(p => p.Stage == PigStage.HeoGia).Count;
            Farm.UpdateDailyEventPressure(elderPigs, livingPigs.Count, capReport.RawDensityRatio, corpsesOutside);

            var herdRes = Herd.AdvanceDay(Pigs, Farm, MajorWavesSurvived);
            if (herdRes.tierChanged && !string.IsNullOrEmpty(herdRes.message))
            {
                OnLogEmitted?.Invoke(herdRes.message);
            }

            Economy.DayMarket.CurrentPigsSoldToday = 0;
            Economy.GenerateNightMarket(Farm.FarmLevel);

            Character.Stamina.DailyLoad = 0;
            Character.Stamina.CurrentStamina = Character.Stamina.MaxStamina;
        }

        public (bool success, int goldEarned, string message) SellPigAtDayMarket(string pigId, bool isQuarantined = true)
        {
            var pig = Pigs.Find(p => p.Id == pigId);
            if (pig == null || !pig.IsAlive)
            {
                return (false, 0, "Heo không tồn tại hoặc đã chết!");
            }

            var (priceGold, successChance) = Economy.CalculatePigSalePrice(pig, Farm.FarmLevel);
            if (priceGold <= 0)
            {
                return (false, 0, "Không thể bán heo này ở chợ ngày!");
            }

            int finalGold = priceGold;
            int quarantineFee = isQuarantined ? Economy.DayMarket.QuarantineFeePerPig : 0;
            finalGold -= quarantineFee;

            Pigs.Remove(pig);
            Farm.AddGold(priceGold);
            TotalGoldEarned += priceGold;

            // Ghi nhận doanh thu bán heo
            Economy.Ledger.RecordTransaction(
                day: Clock.CurrentDay,
                hour: Clock.CurrentHour,
                minute: Clock.CurrentMinute,
                type: TransactionType.Income,
                category: TransactionCategory.BanHeo,
                amountGold: priceGold,
                balanceAfter: Farm.Gold,
                description: $"Bán heo {pig.Name} ({pig.WeightKg:F1}kg, Phẩm chất {pig.MeatQuality})"
            );

            // Ghi nhận phí kiểm dịch nếu có
            if (quarantineFee > 0)
            {
                Farm.SpendGold(quarantineFee);
                Economy.Ledger.RecordTransaction(
                    day: Clock.CurrentDay,
                    hour: Clock.CurrentHour,
                    minute: Clock.CurrentMinute,
                    type: TransactionType.Expense,
                    category: TransactionCategory.KiemDich,
                    amountGold: quarantineFee,
                    balanceAfter: Farm.Gold,
                    description: $"Phí kiểm dịch thú y heo {pig.Name}"
                );
            }

            Farm.AddFarmExp(priceGold / 100);
            TotalPigsSold += 1;
            Economy.DayMarket.CurrentPigsSoldToday += 1;

            return (true, finalGold, $"Đã bán {pig.Name} thu về {finalGold}G{(quarantineFee > 0 ? $" (đã trừ {quarantineFee}G phí kiểm dịch)" : "")}.");
        }

        public (bool success, int goldSpent, string message) BuyFeedAtDayMarket(FeedTypeId feedType, int packageKg)
        {
            return Economy.BuyFeedAtDayMarket(Farm, feedType, packageKg, Clock.CurrentDay, Clock.CurrentHour, Clock.CurrentMinute);
        }

        public DualBudgetReport GetDualBudgetReport()
        {
            return Economy.CalculateDualBudgetReport(Farm, Pigs, Clock.CurrentDay);
        }

        public bool IsNightMarketOpen => Clock.GetTimeOfDay() == TimeOfDay.Toi;

        public (bool success, GameItem? item, string message) BuyNightMarketWithGold(string slotId)
        {
            return Economy.PurchaseNightListingWithGold(Farm, slotId, Clock.CurrentDay, Clock.CurrentHour, Clock.CurrentMinute);
        }

        public (bool success, GameItem? item, int excessValue, string message) BarterNightMarket(
            string slotId,
            List<GameItem>? offeredItems,
            string? offeredPigId = null)
        {
            Pig? pig = null;
            if (!string.IsNullOrEmpty(offeredPigId))
            {
                pig = Pigs.Find(p => p.Id == offeredPigId);
                if (pig == null) return (false, null, 0, "Không tìm thấy heo được chỉ định trong trang trại!");
            }

            var result = Economy.BarterNightListing(slotId, offeredItems, pig, Farm.FarmLevel);
            if (result.success && pig != null)
            {
                Pigs.Remove(pig);
            }
            return result;
        }

        public (bool success, GameItem? item, int excessValue, string message) BarterMysticMerchant(
            string slotId,
            List<GameItem>? offeredItems,
            string? offeredPigId = null)
        {
            Pig? pig = null;
            if (!string.IsNullOrEmpty(offeredPigId))
            {
                pig = Pigs.Find(p => p.Id == offeredPigId);
                if (pig == null) return (false, null, 0, "Không tìm thấy heo được chỉ định trong trang trại!");
            }

            var result = Economy.BarterMysticMerchantListing(slotId, offeredItems, pig, Farm.FarmLevel);
            if (result.success && pig != null)
            {
                Pigs.Remove(pig);
            }
            return result;
        }
    }
}
