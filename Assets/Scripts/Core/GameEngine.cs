using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameEngine
    {
        private readonly Random weatherRandom = new Random();
        private readonly Random weatherDurationRandom = new Random();
        [NonSerialized] private Func<double> weatherRollSource = null!;
        [NonSerialized] private Func<double> weatherDurationRollSource = null!;

        public GameClock Clock { get; private set; }
        public Farm Farm { get; private set; }
        public HerdManager Herd { get; private set; }
        public EconomyManager Economy { get; private set; }
        public DefenseManager Defense { get; private set; }
        public CharacterData Character { get; private set; }
        public InventoryManager Inventory { get; private set; }
        public List<Pig> Pigs { get; private set; }

        public WeatherType CurrentWeather { get; set; } = WeatherType.QuangDang;
        public int WeatherDaysRemaining { get; private set; } = 1;
        public int MajorWavesSurvived { get; set; } = 0;
        public int MinorWavesDefeated { get; set; } = 0;
        public int TotalPigsSold { get; set; } = 0;
        public int TotalGoldEarned { get; set; } = 0;

        public event Action<string> OnLogEmitted;

        public GameEngine(Func<double> weatherRollSource = null!, Func<double> weatherDurationRollSource = null!)
        {
            Clock = new GameClock();
            Farm = new Farm();
            Herd = new HerdManager();
            Economy = new EconomyManager();
            Defense = new DefenseManager();
            Character = new CharacterData();
            Inventory = new InventoryManager();
            Pigs = new List<Pig>();
            this.weatherRollSource = weatherRollSource ?? this.weatherRandom.NextDouble;
            this.weatherDurationRollSource = weatherDurationRollSource ?? this.weatherDurationRandom.NextDouble;
            StartNewWeather();
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

            for (int day = 0; day < tickResult.NewDaysStarted; day++)
            {
                AdvanceWeatherAtNewDay();
            }

            if (tickResult.NewDayStarted)
            {
                OnDailyReset();
            }
        }

        public (bool success, string message, int hoursSlept) PerformStandardSleep()
        {
            var result = Clock.PerformStandardSleep(false, Tick);
            if (!result.success)
                return result;

            Character.Stamina.CurrentStamina = Character.Stamina.MaxStamina;
            Character.Stamina.DailyLoad = 0f;
            return result;
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
            if (Farm.IsUnlockAvailable(FarmUnlock.NightMarket))
                Economy.GenerateNightMarket(Farm.FarmLevel);
            else
                Economy.NightListings.Clear();

            Character.Stamina.DailyLoad = 0;
            Character.Stamina.CurrentStamina = Character.Stamina.MaxStamina;
        }

        private WeatherType RollWeatherForCurrentCycle()
        {
            double roll = weatherRollSource();
            if (double.IsNaN(roll) || double.IsInfinity(roll))
                roll = 0d;
            roll = Math.Max(0d, Math.Min(0.9999999999999999d, roll));

            // Các trọng số theo WeatherType trong Enums.cs; Mưa Bức Xạ chỉ xuất hiện từ chu kỳ 6.
            bool radiationUnlocked = Clock.CycleNumber >= 6;
            double totalWeight = radiationUnlocked ? 100d : 99d;
            double cumulative = 0d;

            WeatherType[] weatherTypes =
            {
                WeatherType.QuangDang, WeatherType.NhieuMay, WeatherType.NangGat,
                WeatherType.Mua, WeatherType.GiongBao, WeatherType.SuongMu,
                WeatherType.RetDam, WeatherType.NomAm, WeatherType.GioKho,
                WeatherType.MuaBucXa
            };
            int[] weights = { 25, 20, 12, 15, 6, 8, 5, 5, 3, 1 };

            for (int i = 0; i < weatherTypes.Length; i++)
            {
                if (i == weatherTypes.Length - 1 && !radiationUnlocked)
                    break;

                cumulative += weights[i];
                if (roll < cumulative / totalWeight)
                    return weatherTypes[i];
            }

            return radiationUnlocked ? WeatherType.MuaBucXa : WeatherType.GioKho;
        }

        private void StartNewWeather()
        {
            CurrentWeather = RollWeatherForCurrentCycle();

            double roll = weatherDurationRollSource();
            if (double.IsNaN(roll) || double.IsInfinity(roll))
                roll = 0d;
            roll = Math.Max(0d, Math.Min(0.9999999999999999d, roll));
            WeatherDaysRemaining = (int)(roll * 3d) + 1;
        }

        private void AdvanceWeatherAtNewDay()
        {
            if (WeatherDaysRemaining > 1)
            {
                WeatherDaysRemaining--;
                return;
            }

            StartNewWeather();
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
            data.WeatherDaysRemaining = WeatherDaysRemaining;
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
            data.HasExtendedCharacterData = true;
            data.BaseStr = Character.BaseStats.Str;
            data.BaseAgi = Character.BaseStats.Agi;
            data.BaseCtrl = Character.BaseStats.Ctrl;
            data.BaseRes = Character.BaseStats.Res;
            data.BonusStr = Character.BonusStats.Str;
            data.BonusAgi = Character.BonusStats.Agi;
            data.BonusCtrl = Character.BonusStats.Ctrl;
            data.BonusRes = Character.BonusStats.Res;
            data.CurrentStamina = Character.Stamina.CurrentStamina;
            data.MaxStamina = Character.Stamina.MaxStamina;
            data.DailyLoad = Character.Stamina.DailyLoad;
            data.Condition = Character.Stamina.Condition;
            data.Immunity = Character.Stamina.Immunity;
            data.ReboundFatigueStacks = Character.Stamina.ReboundFatigueStacks;

            foreach (var equipment in Character.Equipment)
            {
                if (equipment.Value == null)
                    continue;

                data.Equipment.Add(new EquipmentSaveData
                {
                    Slot = equipment.Key,
                    Id = equipment.Value.Id,
                    Name = equipment.Value.Name,
                    WeightScore = equipment.Value.WeightScore,
                    Defense = equipment.Value.Defense,
                    BonusStr = equipment.Value.BonusStr,
                    BonusAgi = equipment.Value.BonusAgi,
                    BonusCtrl = equipment.Value.BonusCtrl,
                    BonusRes = equipment.Value.BonusRes,
                    SpecialMechanic = equipment.Value.SpecialMechanic
                });
            }

            foreach (var skill in Character.ActiveMartialSkills)
            {
                data.ActiveMartialSkills.Add(new MartialSkillSaveData
                {
                    Slot = skill.Key,
                    SkillId = skill.Value
                });
            }

            data.HasInventoryData = true;
            data.Inventory = Inventory.DeepCopy();

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

                    Farm.FarmLevel = Math.Max(1, Math.Min(20, data.FarmLevel));
                    Farm.FarmExp = data.FarmExp;
                    while (Farm.FarmLevel < 20 && Farm.FarmExp >= Farm.GetFarmExpThreshold(Farm.FarmLevel + 1))
                        Farm.FarmLevel++;
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

                    if (data.HasExtendedCharacterData)
                    {
                        Character.BaseStats.Str = data.BaseStr;
                        Character.BaseStats.Agi = data.BaseAgi;
                        Character.BaseStats.Ctrl = data.BaseCtrl;
                        Character.BaseStats.Res = data.BaseRes;
                        Character.BonusStats.Str = data.BonusStr;
                        Character.BonusStats.Agi = data.BonusAgi;
                        Character.BonusStats.Ctrl = data.BonusCtrl;
                        Character.BonusStats.Res = data.BonusRes;

                        Character.Stamina.CurrentStamina = data.CurrentStamina;
                        Character.Stamina.MaxStamina = data.MaxStamina;
                        Character.Stamina.DailyLoad = data.DailyLoad;
                        Character.Stamina.Condition = data.Condition;
                        Character.Stamina.Immunity = data.Immunity;
                        Character.Stamina.ReboundFatigueStacks = data.ReboundFatigueStacks;

                        foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
                            Character.Equipment[slot] = null;

                        if (data.Equipment != null)
                        {
                            foreach (var savedEquipment in data.Equipment)
                            {
                                if (savedEquipment == null ||
                                    !Enum.IsDefined(typeof(EquipSlot), savedEquipment.Slot))
                                    continue;

                                Character.Equipment[savedEquipment.Slot] = new EquipmentItem
                                {
                                    Id = savedEquipment.Id,
                                    Name = savedEquipment.Name,
                                    Slot = savedEquipment.Slot,
                                    WeightScore = savedEquipment.WeightScore,
                                    Defense = savedEquipment.Defense,
                                    BonusStr = savedEquipment.BonusStr,
                                    BonusAgi = savedEquipment.BonusAgi,
                                    BonusCtrl = savedEquipment.BonusCtrl,
                                    BonusRes = savedEquipment.BonusRes,
                                    SpecialMechanic = savedEquipment.SpecialMechanic
                                };
                            }
                        }

                        foreach (CombatSlotType slot in Enum.GetValues(typeof(CombatSlotType)))
                            Character.ActiveMartialSkills[slot] = null;

                        if (data.ActiveMartialSkills != null)
                        {
                            foreach (var savedSkill in data.ActiveMartialSkills)
                            {
                                if (savedSkill == null ||
                                    !Enum.IsDefined(typeof(CombatSlotType), savedSkill.Slot))
                                    continue;

                                Character.ActiveMartialSkills[savedSkill.Slot] = savedSkill.SkillId;
                            }
                        }
                    }

                    if (data.HasInventoryData && data.Inventory != null)
                    {
                        Inventory = data.Inventory.DeepCopy();
                    }

                    CurrentWeather = Enum.IsDefined(typeof(WeatherType), data.CurrentWeather)
                        ? data.CurrentWeather
                        : WeatherType.QuangDang;
                    WeatherDaysRemaining = data.SaveVersion < 4
                        ? 1
                        : Math.Max(1, data.WeatherDaysRemaining);
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
