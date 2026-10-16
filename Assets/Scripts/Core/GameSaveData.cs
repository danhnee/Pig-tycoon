
using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameSaveData
    {
        public int SaveVersion = 1;

        // Trạng thái chung
        public int CurrentDay;
        public int CurrentHour;
        public int CurrentMinute;
        public int CycleNumber;
        public int CycleTotalDays;
        public int CycleDayIndex;
        public int MinorWavesRemainingInCycle;
        public int HoursUntilNextMinorWave;
        public int SleepCooldownHoursRemaining;
        public bool IsDuringCombatWave;

        public WeatherType CurrentWeather;
        public int MajorWavesSurvived;
        public int MinorWavesDefeated;
        public int TotalPigsSold;
        public int TotalGoldEarned;

        // Nông trại
        public int FarmLevel;
        public int FarmExp;
        public int HabitatIndex;
        public int EventPressureIndex;
        public int Gold;
        public int FuelLiters;
        public int CorpseLotCount;
        public FarmInfrastructure Infrastructure = new FarmInfrastructure();

        // Nhân vật
        public string CharacterId;
        public string CharacterName;
        public int CharacterLevel;
        public int CharacterExp;
        public int Hp;
        public int MaxHp;
        public int Rage;
        public MartialSchool PrimarySchool;

        // Danh sách heo
        public List<PigSaveData> Pigs = new List<PigSaveData>();

        // Công trình phòng thủ
        public int HallLevel;
        public List<string> UnlockedBlueprints = new List<string>();
        public List<DefenseBuildingInstance> Buildings =
            new List<DefenseBuildingInstance>();
    }

    [Serializable]
    public class PigSaveData
    {
        public string Id;
        public string Name;
        public int AgeDays;
        public float WeightKg;
        public PigStage Stage;
        public string Gender;
        public GeneLineId GeneLine;
        public GeneRarity Rarity;
        public bool IsIdentified;
        public bool IsXichMaoDomesticated;
        public float Health;
        public float Temperament;
        public float Mood;
        public float Bonding;
        public MeatQuality MeatQuality;
        public float GrowthRateMultiplier;
        public float MaxWeightKg;
        public bool IsQuarantinedForSale;
        public bool IsAlive;
        public bool IsInProcessingLot;
        public int DaysDeceased;
    }
}
