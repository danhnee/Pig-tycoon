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
        public HerdFlowField FlowField { get; private set; }
        public FarmGridAndZones GridZones { get; private set; }
        public PetNpcAndOccultManager PetsAndNpcs { get; private set; }

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
            FlowField = new HerdFlowField();
            GridZones = new FarmGridAndZones();
            PetsAndNpcs = new PetNpcAndOccultManager();
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

        public GameSaveData CreateSaveData()
        {
            var data = new GameSaveData();

            // Trạng thái đồng hồ
            data.CurrentDay = Clock.CurrentDay;
            data.CurrentHour = Clock.CurrentHour;
            data.CurrentMinute = Clock.CurrentMinute;
            data.CycleNumber = Clock.CycleNumber;
            data.CycleTotalDays = Clock.CycleTotalDays;
            data.CycleDayIndex = Clock.CycleDayIndex;
            data.MinorWavesRemainingInCycle = Clock.MinorWavesRemainingInCycle;
            data.HoursUntilNextMinorWave = Clock.HoursUntilNextMinorWave;
            data.SleepCooldownHoursRemaining = Clock.SleepCooldownHoursRemaining;
            data.IsDuringCombatWave = Clock.IsDuringCombatWave;

            // Trạng thái chung
            data.CurrentWeather = CurrentWeather;
            data.MajorWavesSurvived = MajorWavesSurvived;
            data.MinorWavesDefeated = MinorWavesDefeated;
            data.TotalPigsSold = TotalPigsSold;
            data.TotalGoldEarned = TotalGoldEarned;

            // Nông trại
            data.FarmLevel = Farm.FarmLevel;
            data.FarmExp = Farm.FarmExp;
            data.HabitatIndex = Farm.HabitatIndex;
            data.EventPressureIndex = Farm.EventPressureIndex;
            data.Gold = Farm.Gold;
            data.FuelLiters = Farm.FuelLiters;
            data.CorpseLotCount = Farm.CorpseLotCount;
            data.Infrastructure = Farm.Infrastructure;

            // Nhân vật
            data.CharacterId = Character.Id;
            data.CharacterName = Character.Name;
            data.CharacterLevel = Character.Level;
            data.CharacterExp = Character.Exp;
            data.Hp = Character.Hp;
            data.MaxHp = Character.MaxHp;
            data.Rage = Character.Rage;
            data.PrimarySchool = Character.PrimarySchool;

            // Danh sách heo
            foreach (var pig in Pigs)
            {
                data.Pigs.Add(new PigSaveData
                {
                    Id = pig.Id,
                    Name = pig.Name,
                    AgeDays = pig.AgeDays,
                    WeightKg = pig.WeightKg,
                    Stage = pig.Stage,
                    Gender = pig.Gender,
                    GeneLine = pig.GeneLine,
                    Rarity = pig.Rarity,
                    IsIdentified = pig.IsIdentified,
                    IsXichMaoDomesticated = pig.IsXichMaoDomesticated,
                    Health = pig.Health,
                    Temperament = pig.Temperament,
                    Mood = pig.Mood,
                    Bonding = pig.Bonding,
                    MeatQuality = pig.MeatQuality,
                    GrowthRateMultiplier = pig.GrowthRateMultiplier,
                    MaxWeightKg = pig.MaxWeightKg,
                    IsQuarantinedForSale = pig.IsQuarantinedForSale,
                    IsAlive = pig.IsAlive,
                    IsInProcessingLot = pig.IsInProcessingLot,
                    DaysDeceased = pig.DaysDeceased
                });
            }

            // Công trình phòng thủ
            data.HallLevel = Defense.HallLevel;
            data.UnlockedBlueprints = new List<string>(Defense.UnlockedBlueprints);
            data.Buildings = new List<DefenseBuildingInstance>(Defense.Buildings);

            return data;
        }

        public void RestoreFromSave(GameSaveData data)
        {
            if (data == null)
                return;

            Clock.RestoreFromSave(
                data.CurrentDay,
                data.CurrentHour,
                data.CurrentMinute,
                data.CycleNumber,
                data.CycleTotalDays,
                data.CycleDayIndex,
                data.MinorWavesRemainingInCycle,
                data.HoursUntilNextMinorWave,
                data.SleepCooldownHoursRemaining,
                data.IsDuringCombatWave);

            Farm.FarmLevel = data.FarmLevel;
            Farm.FarmExp = data.FarmExp;
            Farm.HabitatIndex = data.HabitatIndex;
            Farm.EventPressureIndex = data.EventPressureIndex;
            Farm.Gold = data.Gold;
            Farm.FuelLiters = data.FuelLiters;
            Farm.CorpseLotCount = data.CorpseLotCount;
            Farm.Infrastructure = data.Infrastructure ?? new FarmInfrastructure();

            Defense.HallLevel = data.HallLevel;
            Defense.UnlockedBlueprints = data.UnlockedBlueprints != null
                ? new List<string>(data.UnlockedBlueprints)
                : new List<string>();
            Defense.Buildings = new List<DefenseBuildingInstance>();
            if (data.Buildings != null)
            {
                foreach (var savedBuilding in data.Buildings)
                {
                    if (savedBuilding == null ||
                        string.IsNullOrWhiteSpace(savedBuilding.InstanceId) ||
                        string.IsNullOrWhiteSpace(savedBuilding.BlueprintId))
                    {
                        continue;
                    }

                    Defense.Buildings.Add(new DefenseBuildingInstance
                    {
                        InstanceId = savedBuilding.InstanceId,
                        BlueprintId = savedBuilding.BlueprintId,
                        Name = string.IsNullOrWhiteSpace(savedBuilding.Name)
                            ? savedBuilding.BlueprintId
                            : savedBuilding.Name,
                        Category = savedBuilding.Category,
                        IsBachVan = savedBuilding.IsBachVan,
                        Durability = savedBuilding.Durability,
                        MaxHp = savedBuilding.MaxHp,
                        CurrentHp = savedBuilding.CurrentHp,
                        IsBroken = savedBuilding.IsBroken,
                        AmmoCurrent = savedBuilding.AmmoCurrent,
                        AmmoCapacity = savedBuilding.AmmoCapacity,
                        AmmoName = savedBuilding.AmmoName ?? string.Empty,
                        AmmoCostPerShot = savedBuilding.AmmoCostPerShot,
                        CooldownSeconds = savedBuilding.CooldownSeconds,
                        CurrentCooldownRemaining = savedBuilding.CurrentCooldownRemaining,
                        FireMode = savedBuilding.FireMode ?? "TuDo"
                    });
                }
            }

            if (data.CharacterId == PlayerRoster.KhoaDataId || data.CharacterName == "Khoa")
            {
                PlayerRoster.Apply(Character, PlayerId.Khoa);
            }
            else if (data.CharacterId == PlayerRoster.AnDataId || data.CharacterName == "An")
            {
                PlayerRoster.Apply(Character, PlayerId.An);
            }

            Character.Id = string.IsNullOrWhiteSpace(data.CharacterId)
                ? Character.Id
                : data.CharacterId;
            Character.Name = string.IsNullOrWhiteSpace(data.CharacterName)
                ? Character.Name
                : data.CharacterName;
            Character.Level = data.CharacterLevel;
            Character.Exp = data.CharacterExp;
            Character.Hp = data.Hp;
            Character.MaxHp = data.MaxHp;
            Character.Rage = data.Rage;
            Character.PrimarySchool = data.PrimarySchool;

            CurrentWeather = data.CurrentWeather;
            MajorWavesSurvived = data.MajorWavesSurvived;
            MinorWavesDefeated = data.MinorWavesDefeated;
            TotalPigsSold = data.TotalPigsSold;
            TotalGoldEarned = data.TotalGoldEarned;

            Pigs.Clear();

            if (data.Pigs != null)
            {
                foreach (var savedPig in data.Pigs)
                {
                    if (savedPig == null ||
                        string.IsNullOrWhiteSpace(savedPig.Id) ||
                        string.IsNullOrWhiteSpace(savedPig.Name))
                    {
                        continue;
                    }

                    var pig = Pig.CreateDefault(
                        savedPig.Id,
                        savedPig.Name,
                        savedPig.GeneLine,
                        savedPig.Rarity,
                        string.IsNullOrWhiteSpace(savedPig.Gender) ? "Duc" : savedPig.Gender,
                        savedPig.AgeDays);

                    pig.WeightKg = savedPig.WeightKg;
                    pig.Stage = savedPig.Stage;
                    pig.IsIdentified = savedPig.IsIdentified;
                    pig.IsXichMaoDomesticated = savedPig.IsXichMaoDomesticated;
                    pig.Health = (int)savedPig.Health;
                    pig.Temperament = (int)savedPig.Temperament;
                    pig.Mood = (int)savedPig.Mood;
                    pig.Bonding = savedPig.Bonding;
                    pig.MeatQuality = savedPig.MeatQuality;
                    pig.GrowthRateMultiplier = savedPig.GrowthRateMultiplier;
                    pig.MaxWeightKg = savedPig.MaxWeightKg;
                    pig.IsQuarantinedForSale = savedPig.IsQuarantinedForSale;
                    pig.IsAlive = savedPig.IsAlive;
                    pig.IsInProcessingLot = savedPig.IsInProcessingLot;
                    pig.DaysDeceased = savedPig.DaysDeceased;

                    Pigs.Add(pig);
                }
            }
        }
    }
}
