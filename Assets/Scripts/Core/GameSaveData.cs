
using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameSaveData
    {
        public const int CurrentSaveVersion = 4;
        public int SaveVersion = CurrentSaveVersion;

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
        public int WeatherDaysRemaining = 1;
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
        public bool HasExtendedCharacterData;
        public int BaseStr;
        public int BaseAgi;
        public int BaseCtrl;
        public int BaseRes;
        public int BonusStr;
        public int BonusAgi;
        public int BonusCtrl;
        public int BonusRes;
        public float CurrentStamina;
        public float MaxStamina;
        public float DailyLoad;
        public int Condition;
        public int Immunity;
        public int ReboundFatigueStacks;
        public List<EquipmentSaveData> Equipment = new List<EquipmentSaveData>();
        public List<MartialSkillSaveData> ActiveMartialSkills = new List<MartialSkillSaveData>();

        // Túi đồ nhân vật
        public bool HasInventoryData;
        public InventoryManager Inventory = new InventoryManager();

        // Danh sách heo
        public List<PigSaveData> Pigs = new List<PigSaveData>();

        // Công trình phòng thủ
        public int HallLevel;
        public List<string> UnlockedBlueprints = new List<string>();
        public List<DefenseBuildingInstance> Buildings =
            new List<DefenseBuildingInstance>();
    }

    [Serializable]
    public class EquipmentSaveData
    {
        public EquipSlot Slot;
        public string Id = string.Empty;
        public string Name = string.Empty;
        public int WeightScore;
        public int Defense;
        public int BonusStr;
        public int BonusAgi;
        public int BonusCtrl;
        public int BonusRes;
        public string SpecialMechanic = string.Empty;
    }

    [Serializable]
    public class MartialSkillSaveData
    {
        public CombatSlotType Slot;
        public string SkillId = string.Empty;
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

    public enum SaveFileStatus
    {
        Missing,
        Loaded,
        Invalid,
        UnsupportedVersion
    }

    public static class GameSaveCompatibility
    {
        public static bool IsSupportedVersion(int version)
        {
            return version >= 1 && version <= GameSaveData.CurrentSaveVersion;
        }

        public static SaveFileStatus Classify(bool fileExists, GameSaveData data)
        {
            if (!fileExists)
                return SaveFileStatus.Missing;
            if (data == null)
                return SaveFileStatus.Invalid;
            return IsSupportedVersion(data.SaveVersion)
                ? SaveFileStatus.Loaded
                : SaveFileStatus.UnsupportedVersion;
        }

        public static bool CanWriteSave(SaveFileStatus status)
        {
            return status == SaveFileStatus.Missing || status == SaveFileStatus.Loaded;
        }
    }
}
