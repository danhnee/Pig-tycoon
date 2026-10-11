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
            Character = new CharacterData(PlayerId.An);
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

            Farm.SpendGold(capReport.DailyEmptyMaintenanceCost);

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
            if (isQuarantined)
            {
                finalGold -= Economy.DayMarket.QuarantineFeePerPig;
            }

            Pigs.Remove(pig);
            Farm.AddGold(finalGold);
            Farm.AddFarmExp(finalGold / 100);
            TotalPigsSold += 1;
            TotalGoldEarned += finalGold;
            Economy.DayMarket.CurrentPigsSoldToday += 1;

            return (true, finalGold, $"Đã bán {pig.Name} thu về {finalGold}G.");
        }
    }
}
