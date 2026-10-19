using System;
using System.Collections.Generic;

namespace PigTycoon.Core
{
    // =========================================================================
    // SỔ TAY DỰ ÁN v3.0 · MỤC 10: GIAO DIỆN HỢP ĐỒNG GIỮA CÁC MODULE (PigTycoon.Contracts)
    // & 10 SỰ KIỆN STRUCT BẤT BIẾN TRÊN EVENT BUS
    // Không phụ thuộc UnityEngine; tương thích 100% .NET Standard 2.1 (Unity 6) & .NET 8.0.
    // =========================================================================

    #region 10 Canonical Struct Events (Sổ tay v3.0 §10)

    public readonly struct PigBorn
    {
        public string PigId { get; }
        public GeneLineId GeneLine { get; }
        public GeneRarity Rarity { get; }
        public int Day { get; }

        public PigBorn(string pigId, GeneLineId geneLine, GeneRarity rarity, int day)
        {
            PigId = pigId ?? string.Empty;
            GeneLine = geneLine;
            Rarity = rarity;
            Day = day;
        }
    }

    public readonly struct PigDied
    {
        public string PigId { get; }
        public string Cause { get; }
        public int Day { get; }

        public PigDied(string pigId, string cause, int day)
        {
            PigId = pigId ?? string.Empty;
            Cause = cause ?? string.Empty;
            Day = day;
        }
    }

    public readonly struct PigSold
    {
        public string PigId { get; }
        public int GoldEarned { get; }
        public bool SoldAtNightMarket { get; }
        public int Day { get; }

        public PigSold(string pigId, int goldEarned, bool soldAtNightMarket, int day)
        {
            PigId = pigId ?? string.Empty;
            GoldEarned = goldEarned;
            SoldAtNightMarket = soldAtNightMarket;
            Day = day;
        }
    }

    public readonly struct DayStarted
    {
        public int Day { get; }
        public WeatherType Weather { get; }

        public DayStarted(int day, WeatherType weather)
        {
            Day = day;
            Weather = weather;
        }
    }

    public readonly struct WeatherChanged
    {
        public WeatherType PreviousWeather { get; }
        public WeatherType NewWeather { get; }
        public int Day { get; }

        public WeatherChanged(WeatherType previousWeather, WeatherType newWeather, int day)
        {
            PreviousWeather = previousWeather;
            NewWeather = newWeather;
            Day = day;
        }
    }

    public readonly struct WaveStarted
    {
        public int WaveIndex { get; }
        public int ThreatBudget { get; }
        public bool IsBossWave { get; }
        public int Day { get; }

        public WaveStarted(int waveIndex, int threatBudget, bool isBossWave, int day)
        {
            WaveIndex = waveIndex;
            ThreatBudget = threatBudget;
            IsBossWave = isBossWave;
            Day = day;
        }
    }

    public readonly struct WaveEnded
    {
        public int WaveIndex { get; }
        public bool Victory { get; }
        public int PigsLost { get; }
        public int Day { get; }

        public WaveEnded(int waveIndex, bool victory, int pigsLost, int day)
        {
            WaveIndex = waveIndex;
            Victory = victory;
            PigsLost = pigsLost;
            Day = day;
        }
    }

    public readonly struct BuildingDamaged
    {
        public string BuildingInstanceId { get; }
        public string BlueprintId { get; }
        public int RemainingDurability { get; }
        public int MaxDurability { get; }

        public BuildingDamaged(string buildingInstanceId, string blueprintId, int remainingDurability, int maxDurability)
        {
            BuildingInstanceId = buildingInstanceId ?? string.Empty;
            BlueprintId = blueprintId ?? string.Empty;
            RemainingDurability = remainingDurability;
            MaxDurability = maxDurability;
        }
    }

    public readonly struct BuildingDestroyed
    {
        public string BuildingInstanceId { get; }
        public string BlueprintId { get; }
        public int Day { get; }

        public BuildingDestroyed(string buildingInstanceId, string blueprintId, int day)
        {
            BuildingInstanceId = buildingInstanceId ?? string.Empty;
            BlueprintId = blueprintId ?? string.Empty;
            Day = day;
        }
    }

    public readonly struct OccultEventTriggered
    {
        public string EventId { get; }
        public string DisplayName { get; }
        public int EventPressureIndex { get; }
        public int Day { get; }

        public OccultEventTriggered(string eventId, string displayName, int eventPressureIndex, int day)
        {
            EventId = eventId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            EventPressureIndex = eventPressureIndex;
            Day = day;
        }
    }

    #endregion

    #region Canonical Module Interfaces (Sổ tay v3.0 §10 — bổ sung đầy đủ bên cạnh IEventBus, IEconomyLedger, IHerdQuery, IBalance)

    public interface ITimeService
    {
        int CurrentDay { get; }
        int CurrentHour { get; }
        int CurrentMinute { get; }
        TimeOfDay CurrentTimeOfDay { get; }
        bool IsPausedForWave { get; }
        int SleepCooldownHoursRemaining { get; }
        (bool success, string message) PerformStandardSleep();
    }

    public interface ISaveable
    {
        string SaveSectionKey { get; }
        string CaptureStateJson();
        void RestoreStateJson(string json);
    }

    public interface ISaveService
    {
        int MaxSlotCount { get; }
        (bool success, string message) SaveToSlot(int slotIndex, GameEngine engine);
        (bool success, string message) LoadFromSlot(int slotIndex, GameEngine engine);
    }

    public interface IDataCatalog
    {
        int TotalEntryCount { get; }
        bool ContainsId(string catalogId);
        CatalogEntry? GetById(string catalogId);
    }

    public interface IHealthQuery
    {
        int SickPigCount { get; }
        int IsolatedPigCount { get; }
        int HabitatScore { get; }
        int DeadCorpseLotCount { get; }
    }

    public interface IWaveSchedule
    {
        int CurrentWaveIndex { get; }
        int NextWaveDay { get; }
        bool IsBossCycleWave(int waveIndex);
    }

    public interface IIntelService
    {
        int CurrentIntelLevel { get; }
        IntelForecastView GetWaveForecast(
            int intelLevel,
            int exactHoursUntilWave,
            string trueDirection,
            int trueEnemyCount,
            string eliteOrBossName,
            string holdRecommendation);
    }

    public interface IPowerGrid
    {
        int ContinuousCapacityUnits { get; }
        int PeakCapacityUnits { get; }
        float CurrentDemandUnits { get; }
        bool IsBreakerTripped { get; }
    }

    public interface IPlayerStats
    {
        PlayerId Identity { get; }
        int EffectiveStr { get; }
        int EffectiveAgi { get; }
        int EffectiveInt { get; }
        int EffectiveRes { get; }
        float CurrentStamina { get; }
        float EffectiveMaxStamina { get; }
    }

    #endregion

    /// <summary>
    /// Bộ định tuyến Hợp đồng Trung tâm (Sổ tay v3.0 §10) kết nối trực tiếp vào GameEngine,
    /// đảm bảo mọi module A, B, C, D truy xuất trạng thái qua đúng giao diện chuẩn không tạo bản sao dữ liệu rời rạc.
    /// </summary>
    public sealed class PigTycoonContractHub :
        ITimeService,
        ISaveService,
        IDataCatalog,
        IBalance,
        IHerdQuery,
        IHealthQuery,
        IEconomyLedger,
        IWaveSchedule,
        IIntelService,
        IPowerGrid,
        IPlayerStats
    {
        private readonly GameEngine _engine;
        private readonly Dictionary<int, string> _saveSlotStore = new Dictionary<int, string>();

        public PigTycoonContractHub(GameEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public IEventBus EventBus => _engine.EventBus;
        public ITimeService TimeService => this;
        public ISaveService SaveService => this;
        public IDataCatalog DataCatalogService => this;
        public IBalance Balance => this;
        public IHerdQuery HerdQuery => this;
        public IHealthQuery HealthQuery => this;
        public IEconomyLedger EconomyLedger => this;
        public IWaveSchedule WaveSchedule => this;
        public IIntelService IntelService => this;
        public IPowerGrid PowerGrid => this;
        public IPlayerStats PlayerStats => this;

        // ITimeService
        public int CurrentDay => _engine.Clock.CurrentDay;
        public int CurrentHour => _engine.Clock.CurrentHour;
        public int CurrentMinute => _engine.Clock.CurrentMinute;
        public TimeOfDay CurrentTimeOfDay => _engine.Clock.GetTimeOfDay();
        public bool IsPausedForWave => _engine.Clock.IsDuringCombatWave;
        public int SleepCooldownHoursRemaining => _engine.Clock.SleepCooldownHoursRemaining;
        public (bool success, string message) PerformStandardSleep()
        {
            var r = _engine.Clock.PerformStandardSleep();
            return (r.success, r.message);
        }

        // ISaveService
        public int MaxSlotCount => 3;
        public (bool success, string message) SaveToSlot(int slotIndex, GameEngine engine)
        {
            if (slotIndex < 1 || slotIndex > MaxSlotCount)
                return (false, $"Slot #{slotIndex} vượt ngoài phạm vi 1..{MaxSlotCount}.");
            var target = engine ?? _engine;
            string json = SaveSystem.Serialize(target.CreateSaveData());
            _saveSlotStore[slotIndex] = json;
            return (true, $"Đã lưu trạng thái vào slot #{slotIndex} ({json.Length} bytes).");
        }

        public (bool success, string message) LoadFromSlot(int slotIndex, GameEngine engine)
        {
            if (!_saveSlotStore.TryGetValue(slotIndex, out string? json) || string.IsNullOrEmpty(json))
                return (false, $"Slot #{slotIndex} đang trống.");
            var target = engine ?? _engine;
            if (!SaveSystem.TryDeserialize(json, out var data, out string err) || data == null)
                return (false, $"Không thể giải mã slot #{slotIndex}: {err}");
            target.RestoreFromSave(data);
            return (true, $"Đã khôi phục trạng thái từ slot #{slotIndex}.");
        }

        // IDataCatalog
        public int TotalEntryCount => DataCatalog.Entries.Count;
        public bool ContainsId(string catalogId) => GetById(catalogId) != null;
        public CatalogEntry? GetById(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return null;
            for (int i = 0; i < DataCatalog.Entries.Count; i++)
            {
                if (string.Equals(DataCatalog.Entries[i].Id, catalogId, StringComparison.Ordinal))
                    return DataCatalog.Entries[i];
            }
            return null;
        }

        // IBalance (GDD v6.0 canonical constants)
        public float SecondsPerGameDay => 960f;
        public float SecondsPerGameHour => 40f;
        public int LandCapacityPerPlot => 8;
        public int ShelterCapacityPerRoof => 6;
        public int WaterCapacityPerBasin => 12;
        public int FeedCapacityPerTrough => 10;
        public int PlotUpkeepGold => 5;
        public int RoofUpkeepGold => 4;
        public int BasinUpkeepGold => 3;
        public int TroughUpkeepGold => 3;
        public int AnStr => 17;
        public int AnAgi => 23;
        public int AnCtrl => 24;
        public int AnRes => 18;
        public int KhoaStr => 24;
        public int KhoaAgi => 18;
        public int KhoaCtrl => 18;
        public int KhoaRes => 22;
        public float WalkMetersPerSecond => 4.5f;
        public float SprintMetersPerSecond => 7.0f;
        public float MainGateWidthMeters => 6.2f;
        public int DaoRuaLightDamage => 18;
        public float LightStrikeStamina => 6f;
        public float NightMarketGoldRate => 0.18f;

        // IHerdQuery
        public int LiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _engine.Pigs.Count; i++)
                {
                    if (_engine.Pigs[i].IsAlive) count++;
                }
                return count;
            }
        }

        public int OldCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _engine.Pigs.Count; i++)
                {
                    if (_engine.Pigs[i].IsAlive && _engine.Pigs[i].Stage == PigStage.HeoGia) count++;
                }
                return count;
            }
        }

        public int CarcassCount => _engine.Farm.CorpseLotCount;

        public float DensityRatio
        {
            get
            {
                var rep = _engine.Farm.CalculateCapacityReport(LiveCount, CarcassCount);
                return rep.RawDensityRatio;
            }
        }

        public int EventPressure => _engine.Farm.EventPressureIndex;
        public bool IsHerdState => _engine.Herd.Tier >= HerdStateTier.SoKhai;

        // IHealthQuery
        public int SickPigCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _engine.Pigs.Count; i++)
                {
                    var p = _engine.Pigs[i];
                    if (p.IsAlive && p.ActiveDisease.HasValue) count++;
                }
                return count;
            }
        }

        public int IsolatedPigCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _engine.Pigs.Count; i++)
                {
                    if (_engine.Pigs[i].IsAlive && _engine.Pigs[i].IsIsolated) count++;
                }
                return count;
            }
        }

        public int HabitatScore => _engine.Farm.HabitatIndex;
        public int DeadCorpseLotCount => _engine.Farm.CorpseLotCount;

        // IEconomyLedger
        public long Gold => _engine.Ledger.Gold;
        public bool TrySpend(long amount, LedgerReason reason, string sourceId) => _engine.Ledger.TrySpend(amount, reason, sourceId);
        public void Earn(long amount, LedgerReason reason, string sourceId) => _engine.Ledger.Earn(amount, reason, sourceId);

        // IWaveSchedule
        public int CurrentWaveIndex => _engine.MajorWavesSurvived + 1;
        public int NextWaveDay
        {
            get
            {
                var (minDays, _) = WaveScheduleAndReportSystem.GetMajorWaveIntervalByPhase(CurrentWaveIndex);
                return _engine.Clock.CurrentDay + minDays;
            }
        }

        public bool IsBossCycleWave(int waveIndex) => WaveScheduleAndReportSystem.GetScheduledCycleBoss(waveIndex).HasValue;

        // IIntelService
        public int CurrentIntelLevel => _engine.DeepMechanics.CurrentIntelTier;
        public IntelForecastView GetWaveForecast(
            int intelLevel,
            int exactHoursUntilWave,
            string trueDirection,
            int trueEnemyCount,
            string eliteOrBossName,
            string holdRecommendation)
        {
            return EconomyAndIntelExpansionSystem.BuildIntelForecastByLevel(
                intelLevel,
                exactHoursUntilWave,
                trueDirection,
                trueEnemyCount,
                eliteOrBossName,
                holdRecommendation);
        }

        // IPowerGrid
        public int ContinuousCapacityUnits => _engine.DeepMechanics.PowerGrid.GetContinuousCapacity();
        public int PeakCapacityUnits => _engine.DeepMechanics.PowerGrid.GetPeakCapacity();
        public float CurrentDemandUnits => _engine.DeepMechanics.PowerGrid.CurrentDemandUnits;
        public bool IsBreakerTripped => _engine.DeepMechanics.PowerGrid.IsBreakerTripped;

        // IPlayerStats
        public PlayerId Identity => _engine.Story.ActiveProtagonist;
        public int EffectiveStr => _engine.Character.BaseStats.Str + _engine.Character.BonusStats.Str;
        public int EffectiveAgi => _engine.Character.BaseStats.Agi + _engine.Character.BonusStats.Agi;
        public int EffectiveInt => _engine.Character.BaseStats.Ctrl + _engine.Character.BonusStats.Ctrl;
        public int EffectiveRes => _engine.Character.BaseStats.Res + _engine.Character.BonusStats.Res;
        public float CurrentStamina => _engine.Character.Stamina.CurrentStamina;
        public float EffectiveMaxStamina => _engine.Character.Stamina.MaxStamina;
    }
}
