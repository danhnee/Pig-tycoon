using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameSaveData
    {
        public int SaveVersion = 2;

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
        public float FeedStockKg = 300f;
        public int MedicalHerbCount = 5;
        public int VaccineDoseCount = 5;
        public bool LocalCrowdingActive;
        public float LocalCrowdingContagionMultiplier = 1.0f;
        public float LocalCrowdingPanicBonus;
        public bool IsBarnContaminated;
        public FarmInfrastructure Infrastructure = new FarmInfrastructure();

        // Trạng thái Bầy Đàn (GDD 4.3)
        public HerdStateTier HerdTier;
        public int HerdConsecutiveDaysInState;
        public int HerdTransmissionStacks;
        public AlphaLeaderData? HerdLeader;
        public SuccessionTrainingData HerdSuccessionTraining = new SuccessionTrainingData();

        // Lưới công trình trại, Khu Xử Lý, Thú cưng, NPC & Nghiên cứu bệnh
        public List<PlacedFarmStructure> PlacedFarmStructures = new List<PlacedFarmStructure>();
        public List<CarcassProcessingJob> ActiveCarcassJobs = new List<CarcassProcessingJob>();
        public List<ActivePetSlot> ActivePets = new List<ActivePetSlot>();
        public List<HiredNpcRecord> HiredNpcs = new List<HiredNpcRecord>();
        public List<DiseaseId> ResearchedDiseases = new List<DiseaseId>();

        // Nhân vật
        public string CharacterId = string.Empty;
        public string CharacterName = string.Empty;
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
        public List<DefenseBuildingInstance> Buildings = new List<DefenseBuildingInstance>();
    }

    [Serializable]
    public class PigSaveData
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public int AgeDays;
        public float WeightKg;
        public PigStage Stage;
        public string Gender = "Duc";
        public GeneLineId GeneLine;
        public GeneRarity Rarity;
        public bool IsIdentified;
        public bool IsXichMaoDomesticated;
        public int XichMaoDomesticationDays;
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

        // Trạng thái không gian, ăn uống, bệnh tật, huyết thống & định hình thịt
        public float PositionX;
        public float PositionY;
        public int Hunger;
        public int Comfort = 70;
        public bool FedToday = true;
        public int OutsideServiceHoursToday;
        public bool IsIsolated;
        public DiseaseId? ActiveDisease;
        public int ImmuneDays;
        public bool SeizurePending;
        public string MotherId = string.Empty;
        public string FatherId = string.Empty;
        public int Generation;
        public bool IsInbred;
        public int GestationDaysRemaining;
        public int LittersBorn;
        public int DaysSinceLastBirth = 99;
        public string PendingFatherId = string.Empty;
        public GeneLineId PendingFatherGene;
        public PigStage PendingFatherStage;
        public bool PendingInbred;
        public int ShapingDaysRecorded;
        public int ShapingDaysFed;
        public int ShapingMoodSum;
        public int ShapingHabitatSum;
        public int ShapingSickDays;
        public List<PigTrait> Traits = new List<PigTrait>();
    }
}
