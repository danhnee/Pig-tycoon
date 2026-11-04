using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    [Serializable]
    public class GameSaveData
    {
        public int SaveVersion = 3;
        public int LastDailyProcessedFarmDay;
        public List<LedgerTransactionRecord> LedgerTransactions = new List<LedgerTransactionRecord>();
        public List<DefenseRepairOrder> RepairOrders = new List<DefenseRepairOrder>();
        public SpaceBagSnapshot? SpaceBag;
        public PlayerCarrySnapshot? PlayerCarry; // null in old saves
        public OccultExpeditionState? OccultExpedition;
        public ProtagonistTraitState? ProtagonistTraits; // null in old saves (GDD 8.1 Khoa/An traits)
        public SpecialtyLockTracker? SpecialtyLocks;     // null in old saves (GDD 10.4 six locks)
        public PlayerWeatherStatusSnapshot? PlayerWeatherStatus; // null in old saves (GDD 7.2 weather statuses, ADR-0022)
        public int VillageStockDay = -1;
        public List<string> VillageSoldSlots = new List<string>();
        public EconomyIntelSnapshot? EconomyIntel;       // null in old saves (GDD 20.2 cycle theme, GDD 23 intel, ADR-0023)
        public PlayableMapConfiguration? PlayableMap;
        /// <summary>Pigs in the market drove and whether it is at the market (MarketDrove, C-01).</summary>
        public List<string> DrovePigIds = new List<string>();
        public bool DroveAtMarket;
        public int TempShadePigsToday;          // Mái Che Tạm (GDD 12.8)
        public long FogWarningUntilMinute = -1; // Lá Dò Sương (GDD 12.8)
        public List<float> TempShadeXY = new List<float>(); // Mái Che Tạm positions, x,y pairs

        // Trạng thái chung
        public int CurrentDay;
        public int CurrentHour;
        public int CurrentMinute;
        public int CycleNumber;
        public int CycleTotalDays;
        public int CycleDayIndex;
        /// <summary>Hạt ngẫu nhiên của ván (0 = bản lưu cũ → dùng hạt mặc định).</summary>
        public int BaseSeed;
        public int MinorWavesRemainingInCycle;
        public int HoursUntilNextMinorWave;
        public bool IsGoldenSeason;
        public int MajorWarningDays;
        public int MinorWaveNoticeHours;
        public int SleepCooldownHoursRemaining;
        public bool IsDuringCombatWave;

        public WeatherType CurrentWeather;
        public int WeatherDaysRemaining;
        public List<string> QuestsCompleted = new List<string>();
        public List<string> QuestSignals = new List<string>();
        public int StoryEnding;
        public int CycleBossesDefeated;
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
        public int ContaminatedTroughsCount; // Optional old-save field defaults0; no inferred historical meals.
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
        public float CurrentGuard = 140f;
        public float MaxGuard = 140f;
        public float CurrentStamina = 180f;
        public float MaxStamina = 180f;
        public float DailyLoad = 0f;
        public int Condition = 100;
        public int ReboundFatigueStacks = 0;
        public bool StaminaExhausted;   // GDD 9.4 Kiệt Sức
        public int MealsToday;          // GDD 9.4 tối đa 3 bữa/ngày
        public long LastMealMinute;     // GDD 9.3 Đói
        public bool ForcedRestToday;    // GDD 9.5 ngày sạch
        public int FaintCount = 0;
        public MartialSchool PrimarySchool;
        // Tiềm năng, trang bị & võ kỹ (GDD 8.2, 12; MartialProgression)
        public int BaseStr, BaseAgi, BaseCtrl, BaseRes;
        public int StatPoints;
        public int MartialPoints;
        public int NormalKillsTowardMartialPoint;
        public float LaborStaminaTowardExp;
        public int SecretManuals;
        public List<string> LearnedSkills = new List<string>();
        public List<string> SlottedSkills = new List<string>(); // Công, Thủ, Biến, Song Thế
        public int LastSchoolChangeCycle;
        public List<string> QuickBagItems = new List<string>();
        public List<string> OwnedEquipment = new List<string>();
        public List<string> EquippedItems = new List<string>();

        // Cốt truyện Mùa 1 & Cơ chế chuyên sâu
        public StoryChapterId StoryChapter;
        public StoryEndingId ChosenEnding;
        public bool SeventhChannelCut;
        public bool HerdEmptySpotObserved;
        public bool MidGameGateUnlocked;
        public int UnlockedSpecialtyLocksCount;
        public int EquippedPaperEnhancementLevel;
        public int EquippedPaperColor;
        public int EnhancementDustCount;
        public int EnhancementStonesCount = 5;
        public List<string> EngravedRunes = new List<string>();
        public List<string> EngravedSeals = new List<string>();
        public bool HasEngravingTable;
        public int RuneFailedOnDay = -1;
        public int SickSalePenaltyDays;
        public int PendingThreatBudgetBonus;
        public int CleaningsToday;
        public int PigsSoldToday;
        public List<string> BreedSoldToday = new List<string>();
        public List<NpcRelationSave> NpcRelations = new List<NpcRelationSave>();
        public List<NpcContractSave> NpcContracts = new List<NpcContractSave>();
        public List<string> NpcCounters = new List<string>();
        public int CyclesSinceRuneSystemUnlocked;
        public int RuneSystemLastCountedCycle;
        public int RedEventsCompleted;
        public int PowerGeneratorLevel = 1;

        // Danh sách heo
        public List<PigSaveData> Pigs = new List<PigSaveData>();

        // Công trình phòng thủ
        public int HallLevel;
        public List<string> UnlockedBlueprints = new List<string>();
        public List<DefenseBuildingInstance> Buildings = new List<DefenseBuildingInstance>();
        public List<ConstructionOrder> ConstructionOrders = new List<ConstructionOrder>(); // GDD 13.2; empty in old saves

        // Xương sống kiến trúc chuẩn Stardew Valley 1.6 (Inventory, TriggerActions, Locations, Mailbox, Topics, Stats)
        public string CurrentLocationId = "Farm";
        public int PlayerTileX = 12;
        public int PlayerTileY = 14;
        public List<UnifiedItemInstance> UnifiedInventoryItems = new List<UnifiedItemInstance>();
        public List<string> TriggerActionsRun = new List<string>();
        public List<string> EventsSeen = new List<string>();
        public List<string> Mailbox = new List<string>();
        public List<string> MailForTomorrow = new List<string>();
        public List<string> MailReceived = new List<string>();
        public Dictionary<string, int> ActiveConversationTopics = new Dictionary<string, int>();
        /// <summary>ADR-0027: story flags, read lines, NPC talk-today, reading settings.</summary>
        public NarrativeSaveData Narrative = new NarrativeSaveData();
        public Dictionary<string, uint> PlayerStatsValues = new Dictionary<string, uint>();
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
        public int LastPaidRationDay = -1;
        public int OutsideServiceHoursToday;
        public bool IsIsolated;
        public DiseaseId? ActiveDisease;
        public int ImmuneDays;
        public bool SeizurePending;
        public int LinhChiFedDay = -1;
        public int CalmFeedDay = -1;
        public int HeatCollapsedHours;
        public long LastSeizureMinute = -1;
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
        // GDD 5.2/5.3 & ADR-0019: SC thấp nhất lúc Đang lớn, tính chất phụ đã tung, bệnh tiềm ẩn, ngày phối gần nhất
        public int ShapingHabitatMin = 101;
        public bool SecondaryTraitsRolled;
        public DiseaseId? LatentDisease;
        public int LastMatingDay = -1;
        public int MaxHealth = 100;
        public float FeedConversionMultiplier = 1f;
        public float InfectionRiskMultiplier = 1f;
        public float LatentDiseaseRiskMultiplier = 1f;
        public int AppraisalFailureCount;
        public int AppraisalLockHoursRemaining;
    }

    [Serializable]
    public class NpcRelationSave
    {
        public int Id;
        public int QuestStage;
        public float Affinity;
        public int TermsCompleted;
        public int FavorDoneCycle;
        public int FavorBaselineCycle;
        public int FavorBaseline;
    }

    [Serializable]
    public class NpcContractSave
    {
        public int Id;
        public int Kind;
        public int Terms;
        public int TotalPrice;
        public int Deposit;
        public int InstallmentsLeft;
        public int InstallmentAmount;
        public int NextDueDay;
        public List<int> LargeCycleDays = new List<int>();
        public int StartDay;
        public bool TrustSkipUsed;
        public int UnitPrice;
        public int PaidInstallments;
    }
}
