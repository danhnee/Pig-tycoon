using System;
using System.Collections.Generic;
using System.Linq;

namespace PigTycoon.Core
{
    [Serializable]
    public partial class GameEngine
    {
        public GameClock Clock { get; private set; }
        public Farm Farm { get; private set; }
        public HerdManager Herd { get; private set; }
        public EconomyManager Economy { get; private set; }
        public VillageManager Village { get; private set; }
        /// <summary>GDD 23 Bàn Tình Báo: bought intel level 0–5 for the running cycle (IntelBureau).</summary>
        public IntelBureau Intel { get; private set; } = new IntelBureau();
        /// <summary>ADR-0026: nhiệm vụ riêng, hảo cảm và hợp đồng của NPC.</summary>
        public NpcRelationsState NpcRelations { get; private set; } = new NpcRelationsState();
        public List<DefenseRepairOrder> RepairOrders { get; } = new List<DefenseRepairOrder>();
        public DefenseManager Defense { get; private set; }
        public CharacterData Character { get; private set; }
        public List<Pig> Pigs { get; private set; }
        public HerdFlowField FlowField { get; private set; }
        public FarmGridAndZones GridZones { get; private set; }
        public PlayableMapConfiguration? PlayableMap { get; private set; }
        public PetNpcAndOccultManager PetsAndNpcs { get; private set; }
        public StoryJournalSystem Story { get; private set; }
        public StoryQuestBook Quests { get; private set; } = new StoryQuestBook();
        /// <summary>Đặc tính cơ bản Khoa/An (GDD 8.1) và "Ngủ Say" (GDD 10.8). Xem ProtagonistTraits.</summary>
        public ProtagonistTraitState TraitState { get; private set; } = new ProtagonistTraitState();
        /// <summary>Trạng thái thời tiết lên người (GDD 7.2) + trang bị phòng tránh (12.3). Xem PlayerWeatherStatus, ADR-0022.</summary>
        public PlayerWeatherStatus WeatherStatus { get; private set; } = new PlayerWeatherStatus(0);
        public NightAuction Auction { get; } = new NightAuction();

        /// <summary>Records a player action for the Season 1 quest line (StoryQuestBook) and completes quests whose steps now hold.</summary>
        public void SignalQuest(QuestSignal signal)
        {
            Quests.Signal(this, signal);
            FireNarrative(NarrativeTrigger.Signal, signal.ToString());
        }
        public DeepMechanicsSystem DeepMechanics { get; private set; }
        public CoreEventBus EventBus { get; private set; }
        public EconomyLedger Ledger { get; private set; }
        public FarmCoreAndVillageSystem CoreAndVillage { get; private set; }
        public MarketTownState MarketTown { get; private set; }
        public MinimapProjectionSystem Minimap { get; private set; }
        public ForestPrimevalAndEventFramework ForestAndEvents { get; private set; }
        public PigTycoonContractHub Contracts { get; private set; }
        public SystemicEcosystemSimulation Ecosystem { get; private set; }

        // 6 Trụ Cột Kiến Trúc Chuẩn Stardew Valley 1.6 (Data-Driven Registry, GameStateQuery, TriggerActionManager, WorldLocationGraph, Mailbox, Stats)
        public int BaseSeed { get; private set; }
        public int LastDailyProcessedFarmDay { get; private set; }
        public UnifiedInventory Inventory { get; private set; }
        public SpaceBag SpaceBag { get; private set; }
        public PlayerCarry Carry { get; private set; }
        public TriggerActionManager TriggerManager { get; private set; }
        public WorldLocationGraph WorldLocations { get; private set; }
        public PlayerMailboxSystem Mailbox { get; private set; }
        public ConversationTopicTracker ConversationTopics { get; private set; }
        public PlayerStatsDictionary PlayerStats { get; private set; }

        public WeatherType CurrentWeather { get; set; } = WeatherType.QuangDang;
        public int MajorWavesSurvived { get; set; } = 0;
        public int MinorWavesDefeated { get; set; } = 0;
        public int TotalPigsSold { get; set; } = 0;
        public int TotalGoldEarned { get; set; } = 0;

        public (bool success, GameItem? item, int excessValueGold, string message) TradeWithVillager(
            VillagerId villagerId, string slotId, List<GameItem>? offeredItems, string? offeredPigId, bool payWithGold)
        {
            Pig? pig = offeredPigId == null ? null : Pigs.Find(p => p.Id == offeredPigId);
            if (offeredPigId != null && pig == null) return (false, null, 0, "Không sở hữu heo đã chọn.");
            var res = Village.TradeWithVillager(this, villagerId, slotId, offeredItems, pig, payWithGold);
            if (res.success)
            {
                if (villagerId == VillagerId.ChoDem) SignalQuest(QuestSignal.NightMarketTrade);
                if (villagerId == VillagerId.ChoDem && !payWithGold) DeepMechanics.SpecialtyLocks.RecordNightMarketBarter(); // An khóa 5 (GDD 10.8)
                else if (villagerId == VillagerId.BiNhan) SignalQuest(QuestSignal.MysticTrade);
            }
            return res;
        }

        /// <summary>Night-market / Bí nhân barter with several pigs (GDD 20.1 "2 heo…", GDD 21 "3–8 heo già").</summary>
        public (bool success, GameItem? item, int excessValueGold, string message) TradeWithVillagerPigs(
            VillagerId villagerId, string slotId, List<GameItem>? offeredItems, IReadOnlyList<string>? offeredPigIds, bool payWithGold)
        {
            var pigs = new List<Pig>();
            foreach (var id in offeredPigIds ?? Array.Empty<string>())
            {
                var pig = Pigs.Find(p => p.Id == id);
                if (pig == null) return (false, null, 0, "Không sở hữu heo đã chọn.");
                pigs.Add(pig);
            }
            var res = Village.TradeWithVillagerPigs(this, villagerId, slotId, offeredItems, pigs, payWithGold);
            if (res.success)
            {
                if (villagerId == VillagerId.ChoDem) SignalQuest(QuestSignal.NightMarketTrade);
                if (villagerId == VillagerId.ChoDem && !payWithGold) DeepMechanics.SpecialtyLocks.RecordNightMarketBarter();
                else if (villagerId == VillagerId.BiNhan) SignalQuest(QuestSignal.MysticTrade);
            }
            return res;
        }

        public event Action<string>? OnLogEmitted;

        /// <summary>Tell the player something from Core (shown as a toast by the presentation).</summary>
        public void Notify(string message) { if (!string.IsNullOrWhiteSpace(message)) OnLogEmitted?.Invoke(message); }

        /// <summary>Pigs being taken to / brought back from the day market (C-01).</summary>
        public MarketDrove Drove { get; } = new MarketDrove();
        /// <summary>Today's pigs for sale at the day market (C-02) and the day they were rolled.</summary>
        public List<MarketPigOffer> MarketPigOffers { get; } = new List<MarketPigOffer>();
        public int MarketPigOffersDay { get; set; } = -1;

        private readonly Random random;

        internal double NextEnhancementRoll() => random.NextDouble();

        public GameEngine(int? seed = null)
        {
            int resolvedSeed = seed ?? 20260601;
            BaseSeed = resolvedSeed;
            random = new Random(resolvedSeed);
            WeatherStatus = new PlayerWeatherStatus(resolvedSeed); // RNG riêng: không xê dịch chuỗi random của ván
            Clock = new GameClock(seed: resolvedSeed);
            Farm = new Farm();
            Herd = new HerdManager();
            Economy = new EconomyManager(resolvedSeed);
            Village = new VillageManager();
            // the night market is only generated on a day change and is not saved: fill it on first use
            Village.NightMarketSource = () =>
            {
                if (Economy.NightListings.Count == 0) Economy.GenerateNightMarket(Farm.FarmLevel);
                return Economy.NightListings;
            };
            Village.MysticStockSource = () => Economy.MysticMerchant.Stock;   // GDD 21: tonight's 2–4 slots
            // GDD 13.1: the night market sells Thường and Tinh blueprints; Cao cấp / Bạch Vân go through the auction
            Economy.BlueprintSource = () => Defense.GetAllBlueprints()
                .Where(b => b.BuildCostGold > 0 && !Defense.UnlockedBlueprints.Contains(b.Id) &&
                            (DefenseManager.TierOf(b.Id) == "Thuong" || DefenseManager.TierOf(b.Id) == "Tinh"))
                .OrderBy(b => b.Id, StringComparer.Ordinal)
                .Select(b => (b.Id, b.Name, b.BuildCostGold, b.IsBachVan));
            Defense = new DefenseManager();
            Defense.IsDuringWave = () => Clock.IsDuringCombatWave;
            Character = new CharacterData(PlayerId.An);
            Character.EquipStarterSet(); // ADR-0017: Dao Rựa
            Pigs = new List<Pig>();
            FlowField = new HerdFlowField();
            GridZones = new FarmGridAndZones();
            PetsAndNpcs = new PetNpcAndOccultManager();
            Story = new StoryJournalSystem();
            Story.SwitchProtagonistPerspective(Character.Identity); // ván mới: cốt truyện theo đúng nhân vật đang chơi
            DeepMechanics = new DeepMechanicsSystem();
            DeepMechanics.SpecialtyLocks.Owner = Character.Identity;
            EventBus = new CoreEventBus();
            Ledger = new EconomyLedger(Farm, Clock);
            CoreAndVillage = new FarmCoreAndVillageSystem();
            MarketTown = new MarketTownState();
            Minimap = new MinimapProjectionSystem();
            ForestAndEvents = new ForestPrimevalAndEventFramework();
            Ecosystem = new SystemicEcosystemSimulation();

            Inventory = new UnifiedInventory();
            SpaceBag = new SpaceBag();
            Carry = new PlayerCarry(this);
            Inventory.BindFarm(Farm);
            TriggerManager = new TriggerActionManager();
            WorldLocations = new WorldLocationGraph();
            Mailbox = new PlayerMailboxSystem();
            ConversationTopics = new ConversationTopicTracker();
            PlayerStats = new PlayerStatsDictionary();
            Contracts = new PigTycoonContractHub(this);
            WireReactiveDomainEventBus();
            TriggerManager.Raise(TriggerActionManager.TriggerDayStarted, this, WorldLocations.CurrentLocation);
            ForestAndEvents.Director.TryOfferDay(this);
        }

        /// <summary>
        /// Đấu nối lưới phản ứng sự kiện liên tầng (Reactive Domain Event Mesh — Sổ tay v3.0 §10 &amp; GDD 1.1–1.2):
        /// Mọi sự kiện phát qua EventBus tự động kích hoạt phản ứng dây chuyền giữa Herd, Economy, Defense, PowerGrid, DeepMechanics, TriggerActionManager và Story.
        /// </summary>
        private void WireReactiveDomainEventBus()
        {
            EventBus.Subscribe<PigBorn>(evt =>
            {
                if (evt.Rarity >= GeneRarity.Hiem)
                {
                    Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + 2);
                }
                if (evt.GeneLine == GeneLineId.HuThe)
                {
                    Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + 4);
                }
                PlayerStats.Increment("PigletsBorn", 1u);
                TriggerManager.Raise(TriggerActionManager.TriggerPigBorn, this, WorldLocations.CurrentLocation);
            });

            EventBus.Subscribe<PigDied>(evt =>
            {
                var deadPig = Pigs.Find(p => p.Id == evt.PigId);
                if (deadPig != null)
                {
                    Herd.OnPigRemovedFromHerd(deadPig, Pigs, diedViolentlyOrSick: true, out _, out _, out _);
                }
                Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + 4);
                Defense.PendingThreatBudgetBonus += 15;
                PlayerStats.Increment("PigsDied", 1u);
                TriggerManager.Raise(TriggerActionManager.TriggerPigDied, this, WorldLocations.CurrentLocation);
            });

            EventBus.Subscribe<PigSold>(evt =>
            {
                Defense.PendingThreatBudgetBonus += Math.Max(1, evt.GoldEarned / 250);
                DeepMechanics.EvaluateMidGameGateAndLocks(Farm, Herd, MajorWavesSurvived);
                PlayerStats.Increment("PigsSold", 1u);
                if (evt.GoldEarned > 0)
                {
                    PlayerStats.Increment("GoldEarned", (uint)evt.GoldEarned);
                }
                TriggerManager.Raise(TriggerActionManager.TriggerPigSold, this, WorldLocations.CurrentLocation);
            });

            EventBus.Subscribe<WeatherChanged>(evt =>
            {
                if (evt.NewWeather == WeatherType.GiongBao)
                {
                    bool hasLightningRod = Defense.HasPoweredBuilding("CotThuLoi");
                    if (!hasLightningRod)
                    {
                        DeepMechanics.PowerGrid.CurrentDemandUnits += 20f;
                        int calmAura = Herd.CalculateElderCalmAuraBonus(Pigs);
                        int panicDrop = Math.Max(1, 6 - calmAura / 2);
                        for (int i = 0; i < Pigs.Count; i++)
                        {
                            if (Pigs[i].IsAlive && !Pigs[i].IsIsolated)
                            {
                                Pigs[i].Mood = Math.Max(0, Pigs[i].Mood - panicDrop);
                            }
                        }
                    }
                }
                else if (evt.NewWeather == WeatherType.SuongMu || evt.NewWeather == WeatherType.MuaBucXa)
                {
                    Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + 5);
                }
            });

            EventBus.Subscribe<WaveStarted>(evt =>
            {
                Clock.IsDuringCombatWave = true;
                Defense.SynchronizeWithPowerGrid(DeepMechanics.PowerGrid, Farm, isDuringWave: true, consumeHourlyFuel: false);
            });

            EventBus.Subscribe<WaveEnded>(evt =>
            {
                Clock.IsDuringCombatWave = false;
                Defense.SynchronizeWithPowerGrid(DeepMechanics.PowerGrid, Farm, isDuringWave: false, consumeHourlyFuel: false);
                DeepMechanics.EvaluateMidGameGateAndLocks(Farm, Herd, MajorWavesSurvived);
                if (evt.Victory)
                {
                    PlayerStats.Increment("WavesSurvived", 1u);
                }
                TriggerManager.Raise(TriggerActionManager.TriggerWaveEnded, this, WorldLocations.CurrentLocation);
                ApplyPostRaidMarketReaction();   // GDD 20.2: new cycle theme if the cycle closed + pigs scarce 1–3 days
            });

            EventBus.Subscribe<BuildingDamaged>(_ =>
            {
                Defense.SynchronizeWithPowerGrid(DeepMechanics.PowerGrid, Farm, Clock.IsDuringCombatWave, consumeHourlyFuel: false);
                DeepMechanics.CurrentIntelTier = Defense.ComputeLiveIntelTier(
                    DeepMechanics.CurrentIntelTier,
                    Story.ActiveProtagonist == PlayerId.An && DeepMechanics.SpecialtyTraitUnlocked);
            });

            EventBus.Subscribe<BuildingDestroyed>(_ =>
            {
                Defense.SynchronizeWithPowerGrid(DeepMechanics.PowerGrid, Farm, Clock.IsDuringCombatWave, consumeHourlyFuel: false);
                DeepMechanics.CurrentIntelTier = Defense.ComputeLiveIntelTier(
                    0,
                    Story.ActiveProtagonist == PlayerId.An && DeepMechanics.SpecialtyTraitUnlocked);
            });

            EventBus.Subscribe<OccultEventTriggered>(evt =>
            {
                Farm.EventPressureIndex = Math.Clamp(evt.EventPressureIndex, 0, 100);
            });
        }

        public int? SimulatedHourOverride { get; private set; }
        /// <summary>Free-roam pasture of the legacy layout (no PlayableMap): the third of the meadow next to the barns.</summary>
        public const float LegacyPastureMinX = -56f, LegacyPastureMaxX = 4f, LegacyPastureMinY = -10f, LegacyPastureMaxY = 44f;

        /// <summary>
        /// GDD 2.3: after the major wave the next cycle is rolled, then adjusted: Áp Lực Sự Kiện ≥ 60 or density > 120 %
        /// → −1 day (min 6); a heavy loss (≥ 30 % of the herd) → +1 recovery day. One rule for the world and the simulation.
        /// (The GDD's other pressure table, L739, uses 50–79 for a different effect; 2.3 is the cycle rule.)
        /// </summary>
        public void CloseMajorWaveCycle(int pigsLost, int herdAtWaveStart)
        {
            bool severe = herdAtWaveStart > 0 && pigsLost >= 0.30f * herdAtWaveStart;
            bool pressure = Farm.EventPressureIndex >= 60 || PigCareCommands.CurrentDensity(this) > 1.20f;
            Clock.CompleteMajorWave(hadSevereLosses: severe, highEventPressure: pressure);
        }

        /// <summary>GDD 3.5: surviving a wave gives farm EXP 150 × cycle number, at most 3,000.</summary>
        public static int WaveSurvivalFarmExp(int cycleNumber) => Math.Min(3000, 150 * Math.Max(1, cycleNumber));

        /// <summary>Lá Dò Sương (GDD 12.8): until this game minute a coming mystic fog is announced 10 minutes early.</summary>
        public long FogWarningUntilMinute { get; set; } = -1;

        public bool InteractiveHerdOwned => PlayableMap?.InteractiveHerdOwnership == true;

        public void ConfigurePlayableMap(PlayableMapConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            configuration.Validate();
            PlayableMap = configuration;
            FlowField = new HerdFlowField((int)Math.Floor(configuration.MapX), (int)Math.Floor(configuration.MapY),
                (int)configuration.MapWidth, (int)configuration.MapHeight);
            GridZones = new FarmGridAndZones(configuration.MapX, configuration.MapY, (int)configuration.MapWidth,
                (int)configuration.MapHeight, seedDefaults: false);
            GridZones.HerdRefugeCenter = (configuration.RefugeX, configuration.RefugeY);
            GridZones.SceneServicePoints = configuration.ServicePoints;
            if (configuration.MonsterEntries != null && configuration.MonsterEntries.Count > 0)
            {
                GridZones.MonsterSpawnPoints.Clear();
                foreach (var entry in configuration.MonsterEntries) GridZones.MonsterSpawnPoints.Add((entry.Sector, entry.X, entry.Y));
            }
            GridZones.RestoreGridState(configuration.InitialServices, null);
        }

        /// <summary>
        /// GDD 3.8: sectors and concrete entry points of the given wave of the current cycle (major 1–3 sectors,
        /// minor 1), from the entry points the playable map declared. Same answer for intel and for the spawner.
        /// </summary>
        public (List<ApproachSector> sectors, List<(ApproachSector sector, float x, float y)> entries) PlanWaveApproach(bool major, int waveIndexInCycle)
        {
            var available = new List<ApproachSector>();
            foreach (var p in GridZones.MonsterSpawnPoints) available.Add(p.sector);
            var sectors = WaveApproachPlanner.PlanSectors(BaseSeed, Clock.CycleNumber, waveIndexInCycle, major, available);
            var entries = new List<(ApproachSector sector, float x, float y)>();
            foreach (var p in GridZones.MonsterSpawnPoints)
                if (sectors.Contains(p.sector)) entries.Add(p);
            return (sectors, entries);
        }

        public void Tick(int minutes = 1)
        {
            if (minutes <= 0 || Clock.IsDuringCombatWave) return;
            bool minorTriggered = false;
            while (minutes > 0 && !Clock.IsDuringCombatWave)
            {
                int step = Math.Min(minutes, 60 - Clock.CurrentMinute);
                AdvanceClockBlock(step);
                minorTriggered |= Clock.IsMinorWaveTriggered;
                minutes -= step;
            }
            Clock.IsMinorWaveTriggered |= minorTriggered;
        }

        private void AdvanceClockBlock(int minutes)
        {
            var pendingFog = ForestAndEvents.Director.TickPendingFog(this);
            if (pendingFog.started) Notify(pendingFog.message);
            SyncEconomyCycle();   // GDD 20.2/23: a closed cycle seeds its theme and expires bought intel
            int hourBefore = Clock.CurrentHour;
            int totalMinutesBefore = Clock.TotalElapsedGameMinutes;
            int totalHoursBefore = (int)Math.Floor(Clock.TotalElapsedGameMinutes / 60d);
            var tickResult = Clock.TickMinutes(minutes);
            int totalMinutesAfter = Clock.TotalElapsedGameMinutes;
            DefenseRepairs.Advance(this, Math.Max(0, totalMinutesAfter - totalMinutesBefore));
            ConstructionQueue.Advance(this, Math.Max(0, totalMinutesAfter - totalMinutesBefore)); // GDD 13.2 thi công
            int totalHoursAfter = (int)Math.Floor(Clock.TotalElapsedGameMinutes / 60d);
            int elapsedHours = Math.Max(0, totalHoursAfter - totalHoursBefore);

            int tenMinBlocks = Math.Max(0, (int)Math.Floor(totalMinutesAfter / 10d) - (int)Math.Floor(totalMinutesBefore / 10d));
            if (tenMinBlocks > 0)
            {
                WorldLocations.PerformTenMinuteUpdateAllLocations(tenMinBlocks * 10);
            }

            for (int h = 0; h < elapsedHours; h++)
            {
                SimulatedHourOverride = (hourBefore + h + 1) % 24;
                OnHourlyTick();
            }
            SimulatedHourOverride = null;

            if (Clock.CurrentMinute == 0)
            {
                Quests.Evaluate(this);
                bool wasHere = Economy.MysticMerchant.IsActive;
                Economy.UpdateMysticMerchantHour(Clock.CurrentDay, Clock.CurrentHour, Farm.EventPressureIndex, Economy.DayMarket.Theme);
                if (!wasHere && Economy.MysticMerchant.IsActive)
                {
                    OnLogEmitted?.Invoke("Bí Nhân nửa người nửa quỷ đã xuất hiện sát ranh giới trại!");
                }
            }

            if (tickResult.NewDayStarted && Clock.CurrentDay - 1 > LastDailyProcessedFarmDay)
            {
                LastDailyProcessedFarmDay = Clock.CurrentDay - 1;
                TriggerManager.Raise(TriggerActionManager.TriggerDayEnding, this, WorldLocations.CurrentLocation);
                AdvanceWeatherDay();   // first: daily trait effects (Hư Thể "Hút Sương") may still call the fog in
                OnDailyReset();
                EventBus.Publish(new DayStarted(Clock.CurrentDay, CurrentWeather));
            }
        }

        /// <summary>
        /// Mô phỏng theo từng giờ game (GDD 3.7, 4.6, 5.2 & Camera/World Layout v1.0):
        /// - Đồng bộ Lưới Điện (PowerGrid) &amp; Nhiên liệu (FuelLiters) với 31 công trình Phòng thủ.
        /// - Giới hạn vùng thả heo rông không vượt quá 1/3 diện tích đồng cỏ sát Khu chuồng trại.
        /// - Tầm phục vụ 30m (ngoài 30m: đánh dấu giảm 10% tăng trọng và -1 Tinh thần/giờ).
        /// - Nguồn mát 25m / Lam Khê 20m trong thời tiết Nắng Gắt (nếu Sập cầu dao mất điện Bơm Làm Mát thì mất hiệu ứng làm mát nhân tạo).
        /// - Chen Chúc cục bộ (> 16 heo trong 1 Ô Đất 10x10m: lây bệnh Nhóm A ×1.3, lan hoảng +10%, -1 Tinh thần/giờ).
        /// - Hiện tượng 02:00 sáng 'Sáu Với Một': khi chưa cắt kênh kiện thứ 7, đàn heo tự chừa khoảng trống bán kính 1.2m ở góc gốc mít.
        /// - Tiến độ Khu Xử Lý xác (Thiêu Hủy 6h / Tái Chế Sinh Học 18h thu hồi cám).
        /// </summary>
        private string minorNoticeKey = "";
        private int majorNoticeCycle = -1, goldenNoticeCycle = -1;

        /// <summary>GDD 2.3 warnings, said once each: minor wave 2–6 h ahead (Radar earlier), major wave 1–2 days ahead,
        /// golden season at its start (C-15).</summary>
        private void AnnounceWaveForecast()
        {
            var minor = WaveForecast.Minor(this);
            string key = Clock.CycleNumber + ":" + Clock.MinorWavesRemainingInCycle;
            if (minor.known && minorNoticeKey != key) { minorNoticeKey = key; Notify("Trinh sát báo: " + minor.text + ". Gom heo ở rìa trại, giữ kho."); }
            if (majorNoticeCycle != Clock.CycleNumber && Clock.GetDaysUntilMajorWave() <= Clock.MajorWarningDays)
            {
                majorNoticeCycle = Clock.CycleNumber;
                Notify(WaveForecast.Major(this).Text + ". Nạp đạn, sửa rào trước khi trời tối.");
            }
            if (Clock.IsGoldenSeason && goldenNoticeCycle != Clock.CycleNumber) { goldenNoticeCycle = Clock.CycleNumber; Notify(WaveForecast.GoldenSeasonText(Clock)); }
        }

        private void OnHourlyTick()
        {
            AnnounceWaveForecast();
            int effectiveHour = SimulatedHourOverride ?? Clock.CurrentHour;
            Economy.MidGameGateOpen = DeepMechanics.MidGameGateUnlocked;

            // GDD 5.4: appraisal locks run out with game time (they used to count down only when the Gene NPC worked,
            // so a failed pig stayed locked forever)
            foreach (var lp in Pigs) if (lp.AppraisalLockHoursRemaining > 0) lp.AppraisalLockHoursRemaining -= 1;

            // Latent diseases break out the hour morale drops low (5.2/6.4), before any recovery this hour
            foreach (var lp in Pigs)
                if (lp.IsAlive && !lp.ActiveDisease.HasValue && lp.LatentDisease.HasValue && HerdLifecycleRules.LatentTriggered(lp, CurrentWeather))
                {
                    lp.ActiveDisease = lp.LatentDisease;
                    lp.LatentDisease = null;
                }
            var moodAtHourStart = new Dictionary<string, int>(Pigs.Count);
            foreach (var mp in Pigs) moodAtHourStart[mp.Id] = mp.Mood;

            // Đồng bộ Lưới Điện và công trình phòng thủ mỗi giờ (không trừ xăng nếu chưa xây Máy Phát Điện phụ)
            Defense.SynchronizeWithPowerGrid(DeepMechanics.PowerGrid, Farm, Clock.IsDuringCombatWave, consumeHourlyFuel: true);

            var plotCounts = new Dictionary<(int px, int py), List<Pig>>();
            int maxPigsInSinglePlot = 0;

            for (int i = 0; i < Pigs.Count; i++)
            {
                var p = Pigs[i];
                if (!p.IsAlive || p.IsIsolated) continue;

                // 0. Kẹp biên giới hạn heo đi lại tự do <= 1/3 diện tích đồng cỏ gần Khu chuồng trại
                if (!InteractiveHerdOwned)
                {
                    var (clampedX, clampedZ) = PlayableMap == null
                        ? (Math.Clamp(p.PositionX, LegacyPastureMinX, LegacyPastureMaxX), Math.Clamp(p.PositionY, LegacyPastureMinY, LegacyPastureMaxY))
                        : (Math.Clamp(p.PositionX, PlayableMap.PastureX, PlayableMap.PastureX + PlayableMap.PastureWidth - 0.001f),
                           Math.Clamp(p.PositionY, PlayableMap.PastureY, PlayableMap.PastureY + PlayableMap.PastureHeight - 0.001f));
                    p.PositionX = clampedX; p.PositionY = clampedZ;
                }

                // 0B. Hiện tượng 02:00 sáng 'Sáu Với Một': khi chưa cắt kênh thứ 7, đàn heo tránh vòng tròn đường kính 1.2m ở góc Tây Bắc (-18, 18)
                if (effectiveHour == 2 && !Story.SeventhChannelCut)
                {
                    float dxSpot = p.PositionX - (-18f);
                    float dySpot = p.PositionY - 18f;
                    if (!InteractiveHerdOwned && dxSpot * dxSpot + dySpot * dySpot < 1.44f && p.Id != "pig_white_foot_2018")
                    {
                        p.PositionX += 1.5f;
                    }
                }
                else if (Story.SeventhChannelCut)
                {
                }

                // 1. Tầm phục vụ 30m
                var (withinService, _, hourlyMoodDelta) = GridZones.EvaluateServiceCoverage(p.PositionX, p.PositionY);
                if (!withinService)
                {
                    p.OutsideServiceHoursToday += 1;
                    p.Mood = Math.Max(0, p.Mood + hourlyMoodDelta);
                }

                // 2. Nguồn mát trong Nắng Gắt (nếu lưới điện sập cầu dao thì Bơm Làm Mát mất tác dụng)
                if (CurrentWeather == WeatherType.NangGat)
                {
                    bool cooled = !DeepMechanics.PowerGrid.IsBreakerTripped &&
                                  GridZones.IsWithinCoolingRadius(p.PositionX, p.PositionY, p.GeneLine);
                    if (!cooled)
                    {
                        p.Comfort = Math.Max(0, p.Comfort - 4);
                        p.Mood = Math.Max(0, p.Mood - 1);
                    }
                    else if (p.Traits.Exists(t => t.Id == "ThichBun" && !t.IsSealed))
                    {
                        p.Mood = Math.Min(100, p.Mood + 1);
                    }
                }

                // 3. Gom nhóm theo Ô Đất 10x10m để kiểm tra Chen Chúc cục bộ
                int plotX = (int)Math.Floor(p.PositionX / FarmGridAndZones.LandPlotMeters);
                int plotY = (int)Math.Floor(p.PositionY / FarmGridAndZones.LandPlotMeters);
                var key = (plotX, plotY);
                if (!plotCounts.TryGetValue(key, out var list))
                {
                    list = new List<Pig>();
                    plotCounts[key] = list;
                }
                list.Add(p);
                if (list.Count > maxPigsInSinglePlot)
                {
                    maxPigsInSinglePlot = list.Count;
                }
            }

            var (isCrowded, contagionMult, panicBonus, crowdMoodPenalty) =
                FarmGridAndZones.EvaluateLocalCrowding(maxPigsInSinglePlot, 1.0f, 0);

            Farm.LocalCrowdingActive = isCrowded;
            Farm.LocalCrowdingContagionMultiplier = contagionMult;
            Farm.LocalCrowdingPanicBonus = panicBonus;

            if (isCrowded)
            {
                foreach (var kv in plotCounts)
                {
                    if (kv.Value.Count > 16)
                    {
                        for (int i = 0; i < kv.Value.Count; i++)
                        {
                            kv.Value[i].Mood = Math.Max(0, kv.Value[i].Mood + crowdMoodPenalty);
                        }
                    }
                }
            }

            // 3B. Sương Mù / Mộng Nhãn, Say Nắng, kiểm tra lây 6 giờ (GDD 5.2, 6.4, 6.5, 7.2)
            TickHerdHealthHour(effectiveHour);

            // 3C. Trạng thái thời tiết lên người chơi ngoài trời (GDD 7.2, 12.3; ADR-0022)
            WeatherStatus.TickHour(this, effectiveHour);

            // 4. Tiến trình Khu Xử Lý xác (Thiêu Hủy / Tái Chế Sinh Học)
            int recoveredPortions = GridZones.TickCarcassProcessingHours(1);
            if (recoveredPortions > 0)
            {
                float recoveredKg = recoveredPortions * 3.5f;
                Farm.FeedStockKg += recoveredKg;
                OnLogEmitted?.Invoke($"Khu Xử Lý hoàn tất Tái Chế Sinh Học: thu hồi +{recoveredPortions} khẩu phần cám (+{recoveredKg:0.0} kg)!");
            }

            // Xóa xác đã hoàn tất xử lý khỏi danh sách Pigs
            for (int i = GridZones.ActiveCarcassJobs.Count - 1; i >= 0; i--)
            {
                var job = GridZones.ActiveCarcassJobs[i];
                if (job.Completed && !string.IsNullOrEmpty(job.PigId))
                {
                    Pigs.RemoveAll(p => !p.IsAlive && p.Id == job.PigId);
                }
            }

            // 5. Đồng bộ lớp Chợ Ngày / Đêm của Market_Town, tiến trình Bàn Giải Mã của Phòng Nghiên Cứu, Utility AI Sinh thái & TriggerActionManager
            MarketTown.SyncWithClock(effectiveHour, random, effectiveHour == 18);
            CoreAndVillage.TickResearchQueueHours(1, Pigs, null, EventBus);
            Ecosystem.TickHourlyEcologyAndAgentAi(this);
            TriggerManager.Raise(TriggerActionManager.TriggerHourlyTick, this, WorldLocations.CurrentLocation);

            // GDD 4.3: "yên tĩnh, không có nguy hiểm: +5/giờ". Quiet = outside a wave and no Tinh thần loss this hour.
            if (!Clock.IsDuringCombatWave)
            {
                float density = PigCareCommands.CurrentDensity(this);
                foreach (var qp in Pigs)
                    if (moodAtHourStart.TryGetValue(qp.Id, out int before) && qp.Mood >= before)
                        qp.RecoverQuietHour(Farm.HabitatIndex, density);
            }
        }

        private void OnDailyReset()
        {
            Village.RefreshStockForDay(Clock.CurrentDay);
            Farm.TempShadePigsToday = 0; // Mái Che Tạm lasts one day (GDD 12.8)
            GridZones.TempShades.Clear();
            OnLogEmitted?.Invoke($"=== BẮT ĐẦU NGÀY {Clock.CurrentDay} (Chu kỳ {Clock.CycleNumber}) ===");

            PlayerStats.Increment("DaysPlayed", 1u);
            Mailbox.OnNewDayRollover();
            ConversationTopics.DecrementTopicsOnNewDay();
            WorldLocations.OnDayStartedAllLocations(this);
            FireNarrative(NarrativeTrigger.DayStart);

            // Đồng bộ kỹ năng Đặc tính giữa Nhân vật -> Đàn heo & Chợ + mạng lưới Thiện cảm NPC Thị trấn Bờ Rìa
            Herd.KhoaHasNguoiCuaDanUnlocked = Story.ActiveProtagonist == PlayerId.Khoa && DeepMechanics.SpecialtyTraitUnlocked;
            Economy.DayMarket.ProtagonistNegotiationBonusMultiplier =
                (Story.ActiveProtagonist == PlayerId.An && DeepMechanics.SpecialtyTraitUnlocked) ? 1.05f : 1.0f;
            Ecosystem.ApplyNpcTrustNetworkToMechanics(this);

            // 1. Ca làm việc hàng ngày của NPC & Thú cưng
            var (curedByVet, scFromWorker, _) = PetsAndNpcs.ExecuteDailyNpcShiftOnPigs(Pigs, Farm);
            if (curedByVet > 0 || scFromWorker > 0)
            {
                OnLogEmitted?.Invoke($"Ca trực NPC: Bác Sĩ Thú Y chữa {curedByVet} heo, Nông Công dọn chuồng +{scFromWorker} SC.");
            }

            var livingPigs = Pigs.FindAll(p => p.IsAlive);
            int corpsesOutside = Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
            var capReport = Farm.CalculateCapacityReport(livingPigs.Count, corpsesOutside);

            DeepMechanics.PeakHerdCountObserved = Math.Max(DeepMechanics.PeakHerdCountObserved, livingPigs.Count);
            DeepMechanics.RareOrHigherGenesIdentifiedCount = Pigs.FindAll(p => p.IsIdentified && p.Rarity >= GeneRarity.Hiem).Count;
            DeepMechanics.ActiveDefenseBuildingsCount = Defense.Buildings.FindAll(b => b != null && !b.IsBroken && b.CurrentHp > 0).Count;

            // Tác động Thời tiết Nồm Ẩm: hỏng 2% cám mỗi ngày nếu chưa tuyển Thủ Kho (GDD 7.2 & 11.1)
            if (CurrentWeather == WeatherType.NomAm && !PetsAndNpcs.HiredNpcs.Exists(n => n.Id == NpcSpecialistId.ThuKho && n.IsActive))
            {
                Farm.FeedStockKg = Math.Max(0f, Farm.FeedStockKg * 0.98f);
            }

            bool isInHerd = Herd.Tier != HerdStateTier.KhongCo;
            // Tăng trọng nhóm Trại (GDD 1.4): mật độ 3.3 + Sinh Cảnh 6.1 cộng trong nhóm
            float densityPenalty = -(HerdLifecycleRules.DensityGrowthModifier(capReport.RawDensityRatio) + HerdLifecycleRules.HabitatGrowthModifier(Farm.HabitatIndex));

            // 2. Cho đàn heo ăn theo khẩu phần 4 giai đoạn tuổi (0.5 / 2.5 / 3.5 / 4.2 kg) + đặc tính dòng gen & thời tiết
            foreach (var p in livingPigs)
            {
                PigCareCommands.FeedDailyRollup(this, p);
            }

            // 3. Tiến tuổi, định hình phẩm chất thịt (Đang lớn -> Trưởng thành), thuần dưỡng Xích Mao & tăng trọng; phát sự kiện PigDied nếu heo chết
            foreach (var p in Pigs)
            {
                bool wasAlive = p.IsAlive;
                p.AdvanceDay(Farm.HabitatIndex, isInHerd, densityPenalty, capReport.RawDensityRatio);
                if (wasAlive && !p.IsAlive)
                {
                    string cause = p.ActiveDisease.HasValue ? $"Tử vong do bệnh {p.ActiveDisease.Value}" : "Tử vong tuổi già";
                    EventBus.Publish(new PigDied(p.Id, cause, Clock.CurrentDay));
                }
            }

            // 4. Kiểm tra tỷ lệ tử vong heo non mồ côi mẹ (GDD 4.4 / P2.03)
            ProcessOrphanPigletMortality();

            // 4B. Áp lực mật độ (Tinh thần), bệnh tiềm ẩn bộc phát, Hư Mạch, Mưa Bức Xạ (GDD 3.3, 5.2, 6.4, 7.2)
            ApplyDailyHerdPressure(capReport.RawDensityRatio);

            // 5. Xử lý heo nái mang thai & sinh sản F1/F2 + 6 bước Hậu duệ heo già
            ProcessDailyPregnancies();

            // 6. Xử lý dịch bệnh, Chuồng Nhiễm & tính trạng Hư Thể thức tỉnh
            livingPigs = Pigs.FindAll(p => p.IsAlive);
            corpsesOutside = Pigs.FindAll(p => !p.IsAlive && !p.IsInProcessingLot).Count;
            capReport = Farm.CalculateCapacityReport(livingPigs.Count, corpsesOutside);
            int activeDiseaseCount = ProcessDailyDiseaseAndDeviantEffects(livingPigs, capReport, corpsesOutside);

            // 7. Kinh tế bảo trì, Sinh Cảnh (SC), Áp lực Sự kiện (ÁLSK) & Bầy Đàn
            Farm.SpendGold(capReport.DailyEmptyMaintenanceCost);

            bool leaderSafe = Herd.Leader == null || !Herd.Leader.IsUnderAttack;
            // GDD 6.2: dọn bãi thật của người chơi (+4/lần, tối đa +12); đồng cỏ tốt khi không quá tải
            Farm.UpdateDailyHabitat(Farm.CleaningsToday, corpsesOutside, activeDiseaseCount, capReport.RawDensityRatio, capReport.RawDensityRatio <= 1.00f, leaderSafe);
            Farm.CleaningsToday = 0;

            int lotPenalty = GridZones.GetProcessingLotDailyHabitatPenalty();
            if (lotPenalty < 0)
            {
                Farm.HabitatIndex = Math.Max(0, Farm.HabitatIndex + lotPenalty);
            }

            int elderPigs = livingPigs.FindAll(p => p.Stage == PigStage.HeoGia).Count;
            Farm.UpdateDailyEventPressure(elderPigs, livingPigs.Count, capReport.RawDensityRatio, corpsesOutside);
            ApplyFogEventPressure();

            var herdRes = Herd.AdvanceDay(Pigs, Farm, MajorWavesSurvived, Clock.CurrentDay - 1);
            if (herdRes.tierChanged && !string.IsNullOrEmpty(herdRes.message))
            {
                OnLogEmitted?.Invoke(herdRes.message);
            }

            if (Herd.Tier >= HerdStateTier.SoKhai && !Story.HerdEmptySpotObserved)
            {
                var qRes = Story.CompleteQuestStep("Q11");
                if (qRes.completed)
                {
                    OnLogEmitted?.Invoke("[CỐT TRUYỆN · CHỖ TRỐNG GIỮA ĐÀN] Đàn heo đạt Bầy Đàn Sơ Khai và tự động chừa trống một vòng tròn đường kính 1,2m ở góc Tây Bắc (trùng vị trí ảnh năm 2018)!");
                }
            }

            DeepMechanics.SpecialtyLocks.ObserveDay(this); // GDD 10.8: bộ đếm 6 khóa
            MartialProgression.TickRuneCycles(this);        // GDD 12.4: số chu kỳ từ khi mở Phù Văn
            foreach (var line in NpcContracts.OnDay(this)) OnLogEmitted?.Invoke(line); // ADR-0026: nhiệm vụ riêng, việc nhờ, kì trả chu kỳ lớn
            DeepMechanics.EvaluateMidGameGateAndLocks(Farm, Herd, MajorWavesSurvived);

            bool revealNextTheme = Defense.HasPoweredBuilding("TramTiepSongCho") ||
                                   (Story.ActiveProtagonist == PlayerId.An && DeepMechanics.SpecialtyTraitUnlocked);
            Economy.AdvanceMarketDay(Farm.FarmLevel, revealNextTheme, MarketGameStage);
            if (revealNextTheme) Economy.PeekNextCycleTheme();   // the preview shows a real pre-rolled theme

            // GDD 9.4/9.5: chốt quá sức cuối ngày + Mệt Dội ngày sạch. Không tự hồi đầy Thể lực / xóa Tải ngày:
            // chỉ ăn, nghỉ dài hoặc ngủ (PlayerRestCommands) mới hồi phần đó.
            var dayEnd = Character.Stamina.EvaluateEndOfDay(() => random.NextDouble());
            if (dayEnd.conditionLost > 0)
                OnLogEmitted?.Invoke($"Quá sức hôm qua: Thể trạng −{dayEnd.conditionLost}{(dayEnd.becameExhausted ? ", bị Kiệt Sức" : "")}.");
            Character.CurrentGuard = Character.MaxGuard;
            Character.SyncLowGuardStamina();
            Character.IsFainted = false;
            Character.AnBatKhuatTriggeredThisWave = false;

            TriggerManager.Raise(TriggerActionManager.TriggerDayStarted, this, WorldLocations.CurrentLocation);
            var offer = ForestAndEvents.Director.TryOfferDay(this);
            if (offer.offered) Notify(offer.message); // gate / fog / Lá Dò Sương warning reach the player
        }

        private void ProcessOrphanPigletMortality()
        {
            for (int i = 0; i < Pigs.Count; i++)
            {
                var piglet = Pigs[i];
                if (!piglet.IsAlive || piglet.Stage != PigStage.HeoNon || string.IsNullOrEmpty(piglet.MotherId))
                    continue;

                bool motherAlive = Pigs.Exists(m => m.Id == piglet.MotherId && m.IsAlive);
                if (motherAlive) continue;

                bool hasPhongMauNearby = Pigs.Exists(m =>
                    m.IsAlive &&
                    m.Gender == "Cai" &&
                    m.GeneLine == GeneLineId.PhongMau &&
                    (m.Stage == PigStage.TruongThanh || m.Stage == PigStage.HeoGia) &&
                    ((m.PositionX - piglet.PositionX) * (m.PositionX - piglet.PositionX) +
                     (m.PositionY - piglet.PositionY) * (m.PositionY - piglet.PositionY)) <= 100f);

                float mortalityRate = GeneticsAndBreedingSystem.CalculateOrphanPigletDailyMortalityRate(hasPhongMauNearby, Herd.Tier)
                                      * HerdLifecycleRules.HabitatPigletMortalityFactor(Farm.HabitatIndex);
                if (mortalityRate > 0f && random.NextDouble() < mortalityRate)
                {
                    piglet.Die();
                    EventBus.Publish(new PigDied(piglet.Id, "Mồ côi thiếu mẹ & không có Phong Mẫu nuôi hộ", Clock.CurrentDay));
                    OnLogEmitted?.Invoke($"Heo non mồ côi [{piglet.Name}] đã tử vong do thiếu mẹ và không có nái Phong Mẫu nuôi hộ.");
                }
            }
        }

        private void ProcessDailyPregnancies()
        {
            ProcessDailyPregnanciesGdd();
        }

        private int ProcessDailyDiseaseAndDeviantEffects(List<Pig> livingPigs, FarmCapacityReport capReport, int corpsesOutside)
        {
            Farm.IsBarnContaminated = DiseaseBoard.IsContaminatedPen(
                capReport.RawDensityRatio,
                Farm.HabitatIndex,
                corpsesOutside > 0 || Farm.CorpseLotCount > 0) || Defense.PlagueContaminatedPen; // GDD 19.1 Kẻ Mang Dịch

            // Kiểm tra tính trạng Hư Thể đã thức tỉnh trên các heo không bị cách ly
            for (int i = 0; i < livingPigs.Count; i++)
            {
                var p = livingPigs[i];
                if (p.IsIsolated || p.GeneLine != GeneLineId.HuThe) continue;

                for (int t = 0; t < p.Traits.Count; t++)
                {
                    var tr = p.Traits[t];
                    if (!tr.IsDeviant || tr.IsPositive || !tr.IsAwakened || tr.IsSealed) continue;

                    if (tr.Id == "HutSuong")
                    {
                        Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + 3); // +Vừa/ngày (GDD 5.3)
                        CurrentWeather = WeatherType.SuongMu;
                    }
                    else if (tr.Id == "TiengKeuLech")
                    {
                        livingPigs.ForEach(other =>
                        {
                            if (!other.IsIsolated) other.Mood = Math.Max(0, other.Mood - 5);
                        });
                    }
                    else if (tr.Id == "BongDoi" && p.ActiveDisease != DiseaseId.HuMach)
                    {
                        // Hư Nhiễm thức: mang Hư Mạch; mỗi con nhiễm ÁLSK +5 tức thời (GDD 5.3, 6.4)
                        p.ActiveDisease = DiseaseId.HuMach;
                        Farm.EventPressureIndex = Math.Min(100, Farm.EventPressureIndex + 5);
                    }
                }
            }

            // Đếm số ca bệnh đang hoạt động và kiểm tra bùng phát bệnh mới
            int activeDiseases = livingPigs.FindAll(p => p.ActiveDisease.HasValue).Count;
            float corpseFactor = 1.0f + corpsesOutside * 0.25f;
            float specialFactor = Farm.LocalCrowdingContagionMultiplier * (Farm.IsBarnContaminated ? 1.35f : 1.0f);
            float outbreakChance = DiseaseBoard.OutbreakChance(
                Farm.HabitatIndex,
                capReport.DiseaseDensityMultiplier,
                1.0f,
                corpseFactor,
                specialFactor);

            // Chỉ tự động lây nhiễm khi trại ô nhiễm / quá tải / có xác ngoài hoặc đã có ca nhiễm không cách ly
            bool hasUnisolatedSick = livingPigs.Exists(p => !p.IsIsolated && p.ActiveDisease.HasValue);
            if (Farm.HabitatIndex < 50 || capReport.RawDensityRatio > 1.20f || corpsesOutside > 0 || hasUnisolatedSick)
            {
                for (int i = 0; i < livingPigs.Count; i++)
                {
                    var p = livingPigs[i];
                    if (p.IsIsolated || p.ActiveDisease.HasValue || p.ImmuneDays > 0) continue;

                    // GDD 6.4/6.5: chỉ nhóm A/B theo nguồn phát; nhóm C/D đi theo luật riêng (Say Nắng, bệnh tiềm ẩn theo dòng)
                    var cause = HerdLifecycleRules.ChooseOutbreakDisease(p, CurrentWeather, Farm.IsBarnContaminated);
                    if (!cause.HasValue) continue;
                    float individualChance = Math.Min(0.35f, outbreakChance * DiseaseBoard.WeatherSpread(cause.Value, CurrentWeather, false));
                    if (p.Health < 40) individualChance *= 1.5f; // GDD 4.2
                    individualChance *= Math.Max(1f, p.InfectionRiskMultiplier); // Thể Trạng Yếu ×1,3 (GDD 4.4)

                    if (random.NextDouble() < individualChance)
                    {
                        p.ActiveDisease = cause.Value;
                        activeDiseases++;
                    }
                }
            }

            return activeDiseases;
        }

        /// <summary>
        /// Ghép đôi phối giống giữa heo nái và heo đực trong đàn (GDD 4.4, 4.8): kiểm tra cặp, tung thụ thai, đặt mang thai 3 ngày.
        /// </summary>
        public (bool scheduled, string message) SchedulePigBreeding(
            string motherPigId,
            string fatherPigId,
            double? deterministicConceptionRoll = null)
        {
            return SchedulePigBreedingGdd(motherPigId, fatherPigId, deterministicConceptionRoll);
        }

        /// <summary>
        /// Kéo xác heo chết về Khu Xử Lý và vận hành Thiêu Hủy (6h) hoặc Tái Chế Sinh Học (18h).
        /// </summary>
        public (bool started, string message, int staminaCost) ProcessDeadPigCarcass(string pigId, CarcassProcessMethod method)
        {
            var deadPig = Pigs.Find(p => p.Id == pigId && !p.IsAlive && !p.IsInProcessingLot);
            if (deadPig == null)
            {
                return (false, "Không tìm thấy xác heo cần xử lý.", 0);
            }

            float dx = deadPig.PositionX - GridZones.ProcessingLotCenter.x;
            float dy = deadPig.PositionY - GridZones.ProcessingLotCenter.y;
            float distMeters = (float)Math.Sqrt(dx * dx + dy * dy);
            int staCost = FarmGridAndZones.CalculateDragCarcassStaminaCost(distMeters);

            var (started, msg, _) = GridZones.StartCarcassProcessing(
                Farm,
                deadPig.Id,
                deadPig.WeightKg,
                method,
                deadPig.ActiveDisease);

            if (started)
            {
                deadPig.IsInProcessingLot = true;
                Character.Stamina.CurrentStamina = Math.Max(0, Character.Stamina.CurrentStamina - staCost);
                OnLogEmitted?.Invoke(msg);
            }

            return (started, msg, staCost);
        }

        /// <summary>
        /// Thực thi 5 động từ xử lý sức khỏe (Cách ly, Xử lý môi trường, Điều trị cá thể, Phòng ngừa đàn, Nghiên cứu) trực tiếp từ GameEngine.
        /// </summary>
        public (bool success, string message) ExecuteHealthTreatment(
            HealthTreatmentVerb verb,
            string? targetPigId = null,
            FarmDrug drug = FarmDrug.HaNhiet,
            DiseaseId researchTarget = DiseaseId.NamPhatQuang)
        {
            Pig? target = string.IsNullOrEmpty(targetPigId)
                ? Pigs.Find(p => p.IsAlive && p.ActiveDisease.HasValue)
                : Pigs.Find(p => p.Id == targetPigId);

            // GDD 6.5 costs (C-07): checked before, charged only when the verb succeeds
            var (gold, stamina) = HealthCosts.Quote(this, verb, target);
            if (gold > 0 && Farm.Gold < gold) return (false, $"Cần {gold}G.");
            if (stamina > 0f && Character.Stamina.CurrentStamina < stamina) return (false, $"Cần {stamina:0} Thể lực.");
            var res = PetsAndNpcs.ExecuteHealthVerbOnPigs(verb, Farm, target, Pigs, drug, researchTarget);
            if (res.success)
            {
                if (gold > 0) Farm.SpendGold(gold);
                if (stamina > 0f) Character.Stamina.TryConsumeStamina(stamina, 1f);
                if (gold > 0 || stamina > 0f) res = (true, res.message + $" (−{gold}G" + (stamina > 0f ? $", −{stamina:0} Thể lực)" : ")"));
                OnLogEmitted?.Invoke(res.message);
            }
            return res;
        }

        /// <summary>
        /// Giải quyết 4 sự kiện Huyền Bí thuộc phạm vi Người B (Vườn Mười Hai Quả, Đồng Cỏ Không Bóng, Bí Nhân Mua Già, Đổi Một Lấy Một).
        /// </summary>
        public (bool resolved, string message) TriggerMemberBOccultEvent(MemberBOccultEventId eventId, int choiceIndex)
        {
            var res = PetsAndNpcs.ResolveMemberBOccultEvent(eventId, choiceIndex, Farm, Pigs);
            if (res.resolved)
            {
                OnLogEmitted?.Invoke(res.outcomeMessage);
            }
            return res;
        }

        /// <summary>
        /// Giải quyết bất kỳ sự kiện nào trong 14 sự kiện huyền bí (E01-E14 — GDD 24.1).
        /// </summary>
        public (bool resolved, string message) TriggerAnyMysteryEventGdd24(
            MysteryEventId eventId,
            int choiceIndex,
            bool hasBuaHoiHuong = false,
            bool hasLaBanTanVu = false)
        {
            var res = PetsAndNpcs.ResolveAnyMysteryEventGdd24(
                eventId,
                choiceIndex,
                Farm,
                Pigs,
                gateOrFogProgressRatio: 1.0f,
                useBuaHoiHuong: hasBuaHoiHuong,
                useLaBanTanVu: hasLaBanTanVu);
            if (res.resolved)
            {
                OnLogEmitted?.Invoke(res.outcomeMessage);
                DeepMechanics.SpecialtyLocks.RecordOccultEventResolved(DeepMechanics.CurrentIntelTier); // An khóa 6 (GDD 10.8)
                string reward = MartialProgression.OnMysteryEventResolved(this, eventId);
                if (reward.Length > 0) OnLogEmitted?.Invoke(reward);
            }
            return res;
        }

        /// <summary>
        /// Giám định gen qua 4 kênh (GDD 5.4): Chợ Ngày (180G), Chợ Đêm (420G), Phòng Nghiên Cứu (300G), Bí Nhân (100%).
        /// </summary>
        public (bool success, int costGold, float effectiveChance, string message) AppraisePigGeneAtChannel(
            string pigId,
            AppraisalChannel channel,
            double? deterministicRoll = null)
        {
            var res = AppraisePigGeneInternal(pigId, channel, deterministicRoll);
            if (res.costGold > 0) SignalQuest(QuestSignal.AppraisalUsed);
            return res;
        }

        private (bool success, int costGold, float effectiveChance, string message) AppraisePigGeneInternal(
            string pigId,
            AppraisalChannel channel,
            double? deterministicRoll)
        {
            var pig = Pigs.Find(p => p.Id == pigId && p.IsAlive);
            if (pig == null)
                return (false, 0, 0f, "Không tìm thấy heo cần giám định.");

            var (identified, finalChance, goldCost, msg) = GeneticsAndBreedingSystem.AppraisePigGene(pig, channel, Farm, deterministicRoll ?? random.NextDouble(), Pigs);
            if (identified && !pig.SecondaryTraitsRolled) PrepareNewPig(pig); // GDD 5.3/5.4: lộ tính chất phụ khi giám định
            if (identified)
            {
                DeepMechanics.RareOrHigherGenesIdentifiedCount = Pigs.FindAll(p => p.IsIdentified && p.Rarity >= GeneRarity.Hiem).Count;
            }
            OnLogEmitted?.Invoke(msg);
            return (identified, goldCost, finalChance, msg);
        }

        /// <summary>
        /// Thi triển 1 trong 24 chiêu võ học (GDD 12.7) kèm kiểm tra Hợp Kích 4 phái (A -> C -> B -> D -> A).
        /// </summary>
        public (bool executed, float damageMultiplier, string comboTriggered, string message) ExecuteMartialSkillWithCombo(string skillId, double now)
        {
            var skillDef = MartialSkillCatalog.GetByIdOrFallback(skillId, Character.PrimarySchool, CombatSlotType.TheCong);
            if (!Character.LearnedSkills.Contains(skillDef.Id))
                return (false, 1f, "", $"Chưa học {skillDef.Name} — học ở bảng Võ kỹ (vàng + Điểm Võ).");
            var assign = MartialProgression.Assign(Character, skillDef.SlotType, skillDef.Id);
            if (!assign.ok) return (false, 1f, "", assign.message);
            var r = MartialProgression.Cast(Character, skillDef.SlotType, now);
            if (r.Ok) OnLogEmitted?.Invoke(r.Message);
            return (r.Ok, r.DamageMultiplier, r.Combo, r.Message);
        }

        /// <summary>
        /// Giải câu đố khe lò sưởi 4 lớp (Cốt truyện Mùa 1 - S04/Q16) và kiểm tra cảm xúc mặt phản chiếu.
        /// </summary>
        public (bool opened, string message) InteractStoryFurnaceSlot(
            bool scrapeSoot,
            bool tryWrongItem,
            bool turn24Inward,
            float handsOffWaitMinutes)
        {
            var res = Story.AttemptFurnaceSlotPuzzle(
                scrapedSoot: scrapeSoot,
                triedOtherBackpackItemFirst: tryWrongItem,
                insertedAncientLeather24: true,
                number24FacingInward: turn24Inward,
                waitedTwoMinutesHandsOff: handsOffWaitMinutes >= 2.0f);
            OnLogEmitted?.Invoke(res.message);
            return res;
        }

        public (string realFace, string reflectionFace, string description) CheckPlayerReflectiveSurface(string surfaceId)
        {
            var res = StoryJournalSystem.EvaluateReflectiveSurfaceEmotion(surfaceId);
            PlayerStats.Increment("ReflectiveSurfacesChecked", 1u);
            TriggerManager.Raise(TriggerActionManager.TriggerReflectiveSurfaceChecked, this, WorldLocations.CurrentLocation);
            OnLogEmitted?.Invoke(res.storyContext);
            return res;
        }

        /// <summary>
        /// Phát lệnh Lùa Đàn về Nơi Trú Đàn (hoặc Tiếng Huýt Khoa) và cập nhật FlowField + vị trí đàn heo.
        /// </summary>
        public (int shelteredCount, int totalEligible, string message) CommandHerdRecallToRefuge(bool khoaWhistle = false)
        {
            var res = CommandHerdRecallInternal(khoaWhistle);
            SignalQuest(QuestSignal.HerdRecalled);
            return res;
        }

        private (int shelteredCount, int totalEligible, string message) CommandHerdRecallInternal(bool khoaWhistle)
        {
            var agents = new List<PigSteeringAgent>();
            var eligiblePigs = new List<Pig>();
            bool hasShepherdDog = PetsAndNpcs.HasPet(PetId.ChoChanDan);

            for (int i = 0; i < Pigs.Count; i++)
            {
                var p = Pigs[i];
                if (!p.IsAlive || p.IsIsolated) continue;

                eligiblePigs.Add(p);
                agents.Add(new PigSteeringAgent
                {
                    PigId = p.Id,
                    X = p.PositionX,
                    Y = p.PositionY,
                    BaseSpeedMetersPerSec = 2.6f,
                    GeneLine = p.GeneLine,
                    IsPanicking = p.MoodState == MoodState.HoangLoan,
                    IsIsolated = p.IsIsolated,
                    IsAlive = p.IsAlive,
                    Mood = p.Mood
                });
            }

            if (khoaWhistle)
            {
                FlowField.ExecuteKhoaWhistle(agents, GridZones.HerdRefugeCenter.x, GridZones.HerdRefugeCenter.y, hasShepherdDog);
            }

            var (recalled, eligible, ratio, gameHours) = FlowField.ExecuteRecallToRefuge(
                agents,
                GridZones.HerdRefugeCenter,
                GridZones.HerdRefugeEntranceCount,
                GridZones.HerdRefugeMinEntranceWidthMeters,
                hasShepherdDog);

            for (int i = 0; i < eligiblePigs.Count; i++)
            {
                eligiblePigs[i].PositionX = agents[i].X;
                eligiblePigs[i].PositionY = agents[i].Y;
                eligiblePigs[i].Mood = agents[i].Mood;
            }

            string msg = $"Lệnh Lùa Đàn về Nơi Trú: {recalled}/{eligible} heo ({ratio * 100f:0}%) đã vào Nơi Trú trong {gameHours:0.1} giờ game.";
            OnLogEmitted?.Invoke(msg);
            return (recalled, eligible, msg);
        }

        public (bool success, int goldEarned, string message) SellPigAtDayMarket(string pigId, bool isQuarantined = true, double? deterministicSaleRoll = null)
        {
            var res = SellPigAtDayMarketInternal(pigId, isQuarantined, deterministicSaleRoll);
            if (res.success) SignalQuest(QuestSignal.DayMarketSale);
            return res;
        }

        private (bool success, int goldEarned, string message) SellPigAtDayMarketInternal(string pigId, bool isQuarantined, double? deterministicSaleRoll)
        {
            var pig = Pigs.Find(p => p.Id == pigId);
            if (pig == null || !pig.IsAlive)
            {
                return (false, 0, "Heo không tồn tại hoặc đã chết!");
            }

            var (priceGold, _) = Economy.CalculatePigSalePrice(pig, Farm.FarmLevel);
            if (priceGold <= 0)
            {
                return (false, 0, "Không thể bán heo này ở chợ ngày!");
            }
            if (InteractiveHerdOwned)
            {
                // GDD 20 + C-01: the day market is open 06:00–18:00 and buys the pigs the player brought in the drove
                if (Clock.CurrentHour < TroughCommands.MarketOpenHour || Clock.CurrentHour >= TroughCommands.MarketCloseHour)
                    return (false, 0, "Chợ ngày mua heo từ 06:00 đến 18:00.");
                if (!Drove.IsAway(pig.Id)) return (false, 0, $"{pig.Name} chưa được lùa ra chợ: đưa heo vào đoàn ở trại rồi dẫn ra chợ.");
            }

            // GDD 4.5: kiểm dịch phát hiện bệnh thì không bán; không kiểm dịch thì người mua nhận theo tỉ lệ của giống
            string? healthGate = CheckDayMarketHealthGate(pig, isQuarantined, deterministicSaleRoll);
            if (healthGate != null)
            {
                return (false, 0, healthGate);
            }

            int finalGold = priceGold;
            // GDD 8.1 Mắt Nhà Nghề: con An đã kiểm miễn phí thì không mất phí kiểm dịch
            if (isQuarantined && !ProtagonistTraits.TryConsumeFreeQuarantine(this, pig.Id))
            {
                finalGold = Math.Max(10, finalGold - Economy.DayMarket.QuarantineFeePerPig);
            }

            // 1. Tác động xã hội lên Bầy Đàn (nếu bán Thủ Lĩnh hoặc heo gắn bó cao)
            Pigs.Remove(pig);
            Drove.PigIds.Remove(pig.Id);
            Herd.OnPigRemovedFromHerd(pig, Pigs, diedViolentlyOrSick: false, out _, out _, out _);

            // 2. Tác động Kinh tế, Bão hòa giống & Phạt bán heo ủ bệnh không kiểm dịch (-10% trong 3 ngày)
            Economy.RecordPigSaleAndApplyMarketFeedback(pig, isQuarantined, out bool sickContamination);
            if (sickContamination)
            {
                if (Story.NpcTrustTier.ContainsKey("Le"))
                {
                    Story.NpcTrustTier["Le"] = Math.Max(0, Story.NpcTrustTier["Le"] - 1);
                }
            }
            else if (isQuarantined)
            {
                DeepMechanics.SpecialtyLocks.RecordQuarantinedDayMarketSale(); // Khoa khóa 5 (GDD 10.8)
            }

            // 3. Ghi sổ cái & phát sự kiện PigSold qua EventBus (tự động kích hoạt ThreatBudget, MidGameGate)
            Ledger.Earn(finalGold, LedgerReason.SellPig, pig.Id);
            Farm.AddFarmExp(finalGold / 100);
            TotalPigsSold += 1;
            TotalGoldEarned += finalGold;
            EventBus.Publish(new PigSold(pig.Id, finalGold, false, Clock.CurrentDay));

            return (true, finalGold, $"Đã bán {pig.Name} thu về {finalGold}G (Ông Tồn đếm số 7 là '{StoryJournalSystem.FormatEdgeTownNumber(7)}').");
        }

        /// <summary>
        /// Chuyển đổi nhân vật điều khiển trực tiếp giữa Khoa và An, đồng bộ tức thì Chỉ số gốc, Sổ tay,
        /// Kỹ năng Đặc tính Bầy Đàn ('Người Của Đàn') và Đàm phán Thị trường ('Người Đọc Dòng').
        /// </summary>
        public void SwitchActiveProtagonist(PlayerId protagonist)
        {
            PlayerRoster.Apply(Character, protagonist);
            Story.SwitchProtagonistPerspective(protagonist);
            DeepMechanics.SpecialtyLocks.Owner = protagonist;
            Herd.KhoaHasNguoiCuaDanUnlocked = protagonist == PlayerId.Khoa && DeepMechanics.SpecialtyTraitUnlocked;
            Economy.DayMarket.ProtagonistNegotiationBonusMultiplier =
                (protagonist == PlayerId.An && DeepMechanics.SpecialtyTraitUnlocked) ? 1.05f : 1.0f;
        }

        /// <summary>
        /// Đổi thời tiết và phát sự kiện WeatherChanged qua EventBus để kích hoạt dây chuyền Thời tiết -> Lưới điện -> Đàn heo -> Áp lực Sự kiện.
        /// </summary>
        /// <summary>Days left of the current weather spell (GDD 7.2: 1–3 days); 0 = roll at the next day start.</summary>
        public int WeatherDaysRemaining { get; set; }

        /// <summary>GDD 7.2: at each day start the current spell counts down; when it ends a new one is rolled.</summary>
        public void AdvanceWeatherDay()
        {
            if (WeatherDaysRemaining > 1) { WeatherDaysRemaining -= 1; return; }
            var (weather, days) = WeatherScheduler.Roll(BaseSeed, Clock.CurrentDay, Clock.CycleNumber);
            WeatherDaysRemaining = days;
            if (weather != CurrentWeather) ChangeWeather(weather);
        }

        public void ChangeWeather(WeatherType newWeather)
        {
            var prev = CurrentWeather;
            CurrentWeather = newWeather;
            EventBus.Publish(new WeatherChanged(prev, newWeather, Clock.CurrentDay));
        }

        /// <summary>
        /// Thực thi một đợt quái tấn công thời gian thực kết nối xuyên suốt 6 hệ thống:
        /// WaveStarted (đóng băng đồng hồ) -> Phụ tải điện đỉnh &amp; Sập cầu dao (>140%) -> Tháp bắn &amp; Tiêu hao đạn ->
        /// Âm chấn (Acoustic Shock) tác động lên heo nái mang thai ngoài Nơi Trú (được giảm nhẹ bởi Hào quang Heo Già) ->
        /// Quái phá công trình / uy hiếp Thủ Lĩnh Đàn -> Báo cáo 7 mục &amp; Đổi chủ đề chợ hậu đột kích.
        /// </summary>
        public (WaveBattleReport report, bool breakerTripped, int miscarriedSowsCount, int calmedByEldersBonus) ExecuteLiveCombatWave(
            int waveIndex,
            int threatBudget,
            bool isBossWave = false,
            bool triggerBachVan = false)
        {
            int effectiveThreat = Math.Max(40, threatBudget + Defense.PendingThreatBudgetBonus);
            Defense.PendingThreatBudgetBonus = 0;

            EventBus.Publish(new WaveStarted(waveIndex, effectiveThreat, isBossWave, Clock.CurrentDay));

            var gridSync = Defense.SynchronizeWithPowerGrid(DeepMechanics.PowerGrid, Farm, isDuringWave: true, consumeHourlyFuel: true);
            var salvo = Defense.ExecuteDefenseWaveSalvo(effectiveThreat, triggerBachVan);

            // Chấn động âm thanh từ Cửu Tiêu Lôi Pháo / Đài Thiên Lôi / Mìn Nổ tác động lên heo nái mang thai chưa vào Nơi Trú Đàn
            int calmAura = Herd.CalculateElderCalmAuraBonus(Pigs);
            int miscarried = 0;
            if (salvo.acousticShockScore > 0)
            {
                int rawMoodShock = Math.Max(4, salvo.acousticShockScore / 2 - calmAura);
                for (int i = 0; i < Pigs.Count; i++)
                {
                    var p = Pigs[i];
                    if (!p.IsAlive || p.IsIsolated) continue;

                    float dxRefuge = p.PositionX - GridZones.HerdRefugeCenter.x;
                    float dyRefuge = p.PositionY - GridZones.HerdRefugeCenter.y;
                    bool insideRefuge = (dxRefuge * dxRefuge + dyRefuge * dyRefuge) <= 324f; // bán kính 18m quanh Nơi Trú
                    if (insideRefuge) continue;

                    p.Mood = Math.Max(0, p.Mood - rawMoodShock);
                    if (p.Gender == "Cai" && p.GestationDaysRemaining > 0 && p.Mood < 20)
                    {
                        p.GestationDaysRemaining = 0;
                        p.DaysSinceLastBirth = 0;
                        miscarried++;
                    }
                }
            }

            bool victory = (salvo.totalTowerDamageDealt + Contracts.PlayerStats.EffectiveStr * 8) >= effectiveThreat;
            int pigsLost = victory ? 0 : Math.Min(2, Pigs.FindAll(p => p.IsAlive && !p.IsIsolated).Count);

            if (!victory && Herd.Leader != null)
            {
                Herd.Leader.IsUnderAttack = true;
            }

            if (salvo.buildingsDamagedCount > 0)
            {
                for (int i = 0; i < Defense.Buildings.Count; i++)
                {
                    var b = Defense.Buildings[i];
                    if (b.IsBroken)
                    {
                        EventBus.Publish(new BuildingDestroyed(b.InstanceId, b.BlueprintId, Clock.CurrentDay));
                    }
                    else if (b.CurrentHp < b.MaxHp)
                    {
                        EventBus.Publish(new BuildingDamaged(b.InstanceId, b.BlueprintId, b.Durability, 100));
                    }
                }
            }

            bool majorWave = isBossWave || threatBudget >= 150; // điểm đe dọa cộng thêm không biến đợt nhỏ thành đợt lớn
            int cycleOfWave = Clock.CycleNumber;
            // ADR-0026: hợp đồng theo đợt trả/đếm mọi đợt; theo chu kỳ nhỏ quyết toán khi đợt lớn khép chu kỳ
            foreach (var line in NpcContracts.OnWaveEnded(this, major: majorWave && victory)) OnLogEmitted?.Invoke(line);
            if (majorWave)
            {
                if (victory)
                {
                    MajorWavesSurvived += 1;
                    if (isBossWave)
                    {
                        DeepMechanics.CycleBossesDefeatedCount += 1;
                    }
                    var paperRng = new Random(unchecked(BaseSeed * 131 + MajorWavesSurvived));
                    string paperDrop = SpecialtyPaperActions.OnMajorWaveWon(this, isBossWave, () => paperRng.NextDouble());
                    if (paperDrop.Length > 0) OnLogEmitted?.Invoke(paperDrop);
                    CloseMajorWaveCycle(pigsLost, Pigs.Count(p => p.IsAlive) + pigsLost);
                }
            }
            else
            {
                if (victory) MinorWavesDefeated += 1;
            }
            RecordWaveEndedForStory(majorWave, isBossWave && majorWave, cycleOfWave);

            int goldReward = victory ? Math.Max(120, effectiveThreat * 2) : 40;
            Ledger.Earn(goldReward, LedgerReason.WaveReward, $"wave_{waveIndex}");

            var report = DeepMechanics.RecordWaveBattleReport(
                waveIndex,
                victory,
                enemiesDefeated: Math.Max(3, effectiveThreat / 20),
                pigsLost: pigsLost,
                ammoSpent: salvo.totalAmmoSpent,
                durabilityLost: salvo.buildingsDamagedCount * 5,
                playerGuardBreaks: Character.FaintCount,
                goldReward: goldReward);

            // GDD 20.2: Khan Hiếm Hậu Đột Kích is a seeded cycle theme, not a raid override; the raid's world reaction
            // (pigs scarce +10–35% for 1–3 days) runs in the WaveEnded subscriber for every wave path.

            EventBus.Publish(new WaveEnded(waveIndex, victory, pigsLost, Clock.CurrentDay));
            return (report, gridSync.breakerTripped, miscarried, calmAura);
        }

        /// <summary>
        /// Xử lý chu trình nhân vật bị Ngất (do Vỡ Thanh Chống Chịu = 0 dù còn Stamina, HOẶC Cạn Thể Lực Đỡ Đòn = 0 dù còn Chống Chịu)
        /// và được Tám Vá & Mận kéo về Trại Hồi Sức / Hiên Nhà Chính (GDD 8.2, 9.4, 19):
        /// - Phạt 10% Vàng đang mang, -20 Thể trạng (Condition), trôi qua +2 giờ game để hồi sức.
        /// - Hồi phục lại 60% Thanh Chống Chịu và 60% Thể Lực khả dụng tại hiên nhà chính.
        /// </summary>
        public (int goldLost, float restoredStamina, float restoredGuard, string rescueMessage) ExecutePlayerFaintRescue(string? overrideReason = null)
        {
            string reason = !string.IsNullOrWhiteSpace(overrideReason)
                ? overrideReason!
                : (!string.IsNullOrWhiteSpace(Character.LastFaintReason)
                    ? Character.LastFaintReason
                    : "Vỡ Thanh Chống Chịu / Kiệt Sức Đỡ Đòn");

            int goldPenalty = Math.Max(0, (int)Math.Round(Farm.Gold * 0.10f));
            if (goldPenalty > 0)
            {
                Farm.SpendGold(goldPenalty);
            }

            Character.Stamina.Condition = Math.Max(0, Character.Stamina.Condition - 20); // GDD 8.2: Thể trạng -20 (no floor at 20)
            if (Clock.IsDuringCombatWave)
            {
                // GDD 8.2: "tỉnh lại ở nhà chính sau đợt quái": out of the fight until the wave ends (WakeAfterWave)
                Character.IsFainted = true;
                string waveMsg = $"Bất tỉnh do [{reason}]: được kéo về Nhà Chính, tỉnh lại khi hết đợt quái (-{goldPenalty}G, -20 Thể trạng).";
                OnLogEmitted?.Invoke(waveMsg);
                return (goldPenalty, Character.Stamina.CurrentStamina, Character.CurrentGuard, waveMsg);
            }
            Character.IsFainted = false;
            Tick(120); // outside a wave (exhaustion): 2 game hours of recovery (C-12)

            Character.Hp = Character.MaxHp;
            Character.CurrentGuard = (float)Math.Round(Character.MaxGuard * 0.60f, 1);
            Character.LowGuardStaminaScaled = Character.IsGuardLow;
            Character.Stamina.CurrentStamina = (float)Math.Round(Character.GetEffectiveMaxStamina() * 0.60f, 1);

            string msg = $"[CỨU HỘ VỀ TRẠI HỒI SỨC] Nhân vật bị ngất do [{reason}]! Tám Vá và Mận đã kéo về Hiên Nhà Chính hồi sức sau 2 giờ (-{goldPenalty}G phí cấp cứu, -20 Thể trạng, hồi lại {Character.CurrentGuard:0} Chống Chịu & {Character.Stamina.CurrentStamina:0} Thể Lực).";
            OnLogEmitted?.Invoke(msg);
            return (goldPenalty, Character.Stamina.CurrentStamina, Character.CurrentGuard, msg);
        }

        /// <summary>GDD 8.2: a player knocked out during a wave wakes at the main house when it ends.</summary>
        public string WakeAfterWave()
        {
            if (!Character.IsFainted) return "";
            Character.IsFainted = false;
            Character.Hp = Character.MaxHp;
            Character.CurrentGuard = (float)Math.Round(Character.MaxGuard * 0.60f, 1);
            Character.LowGuardStaminaScaled = Character.IsGuardLow;
            Character.Stamina.CurrentStamina = (float)Math.Round(Character.GetEffectiveMaxStamina() * 0.60f, 1);
            const string msg = "Tỉnh lại ở Nhà Chính sau đợt quái.";
            OnLogEmitted?.Invoke(msg);
            return msg;
        }

        /// <summary>
        /// Mô phỏng nhân vật nhận đòn từ quái vật trong giao tranh (Auto-Guard hoặc Active Parry),
        /// tự động kích hoạt kéo về trại hồi sức nếu Thanh Chống Chịu hoặc Thể Lực chạm 0.
        /// </summary>
        public (CombatHitResolution hit, bool rescuedToFarmhouse, string rescueLog) SimulatePlayerCombatEncounter(
            float rawEnemyDamage,
            bool isActiveParry,
            bool isHoldingGuardStance = false)
        {
            var hit = Character.ProcessIncomingAttack(rawEnemyDamage, isActiveParry, isHoldingGuardStance);
            OnLogEmitted?.Invoke(hit.SummaryMessage);

            if (hit.CausedFaint)
            {
                var rescue = ExecutePlayerFaintRescue(hit.FaintReason);
                return (hit, true, rescue.rescueMessage);
            }

            return (hit, false, string.Empty);
        }

        public GameSaveData CreateSaveData()
        {
            var data = new GameSaveData();
            data.RepairOrders = RepairOrders.ConvertAll(order => order.Copy());
            data.LastDailyProcessedFarmDay = LastDailyProcessedFarmDay;
            data.PlayableMap = PlayableMap;
            data.DrovePigIds = new List<string>(Drove.PigIds);
            data.DroveAtMarket = Drove.AtMarket;
            data.TempShadePigsToday = Farm.TempShadePigsToday;
            data.FogWarningUntilMinute = FogWarningUntilMinute;
            data.TempShadeXY = new List<float>();
            foreach (var (sx, sy) in GridZones.TempShades) { data.TempShadeXY.Add(sx); data.TempShadeXY.Add(sy); }

            // Trạng thái đồng hồ
            data.CurrentDay = Clock.CurrentDay;
            data.CurrentHour = Clock.CurrentHour;
            data.CurrentMinute = Clock.CurrentMinute;
            data.CycleNumber = Clock.CycleNumber;
            data.CycleTotalDays = Clock.CycleTotalDays;
            data.CycleDayIndex = Clock.CycleDayIndex;
            data.BaseSeed = BaseSeed;
            data.MinorWavesRemainingInCycle = Clock.MinorWavesRemainingInCycle;
            data.HoursUntilNextMinorWave = Clock.HoursUntilNextMinorWave;
            data.IsGoldenSeason = Clock.IsGoldenSeason;
            data.MajorWarningDays = Clock.MajorWarningDays;
            data.MinorWaveNoticeHours = Clock.MinorWaveNoticeHours;
            data.SleepCooldownHoursRemaining = Clock.SleepCooldownHoursRemaining;
            data.IsDuringCombatWave = Clock.IsDuringCombatWave;

            // Trạng thái chung
            data.CurrentWeather = CurrentWeather;
            data.WeatherDaysRemaining = WeatherDaysRemaining;
            data.QuestsCompleted = Quests.SaveCompleted();
            data.QuestSignals = Quests.SaveReceived();
            data.StoryEnding = (int)Quests.Ending;
            data.CycleBossesDefeated = DeepMechanics.CycleBossesDefeatedCount;
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
            data.FeedStockKg = Farm.FeedStockKg;
            data.MedicalHerbCount = Farm.MedicalHerbCount;
            data.VaccineDoseCount = Farm.VaccineDoseCount;
            data.LocalCrowdingActive = Farm.LocalCrowdingActive;
            data.LocalCrowdingContagionMultiplier = Farm.LocalCrowdingContagionMultiplier;
            data.LocalCrowdingPanicBonus = Farm.LocalCrowdingPanicBonus;
            data.IsBarnContaminated = Farm.IsBarnContaminated;
            data.ContaminatedTroughsCount = Ecosystem.ContaminatedTroughsCount;
            data.Infrastructure = Farm.Infrastructure;

            // Bầy Đàn
            data.HerdTier = Herd.Tier;
            data.HerdConsecutiveDaysInState = Herd.ConsecutiveDaysInState;
            data.HerdTransmissionStacks = Herd.TransmissionStacks;
            data.HerdLeader = Herd.Leader;
            data.HerdSuccessionTraining = Herd.SuccessionTraining;

            // Lưới công trình, Khu Xử Lý, Thú cưng, NPC & Nghiên cứu bệnh
            data.PlacedFarmStructures = new List<PlacedFarmStructure>(GridZones.Structures);
            data.ActiveCarcassJobs = new List<CarcassProcessingJob>(GridZones.ActiveCarcassJobs);
            data.ActivePets = new List<ActivePetSlot>(PetsAndNpcs.ActivePets);
            data.HiredNpcs = new List<HiredNpcRecord>(PetsAndNpcs.HiredNpcs);
            data.ResearchedDiseases = new List<DiseaseId>(PetsAndNpcs.ResearchedDiseases);

            // Nhân vật
            data.CharacterId = Character.Id;
            data.CharacterName = Character.Name;
            data.CharacterLevel = Character.Level;
            data.CharacterExp = Character.Exp;
            data.Hp = Character.Hp;
            data.MaxHp = Character.MaxHp;
            data.Rage = Character.Rage;
            data.CurrentGuard = Character.CurrentGuard;
            data.MaxGuard = Character.MaxGuard;
            data.CurrentStamina = Character.Stamina.CurrentStamina;
            data.MaxStamina = Character.Stamina.MaxStamina;
            data.DailyLoad = Character.Stamina.DailyLoad;
            data.Condition = Character.Stamina.Condition;
            data.ReboundFatigueStacks = Character.Stamina.ReboundFatigueStacks;
            data.StaminaExhausted = Character.Stamina.IsExhausted;
            data.MealsToday = Character.Stamina.MealsToday;
            data.LastMealMinute = Character.Stamina.LastMealMinute;
            data.ForcedRestToday = Character.Stamina.ForcedRestToday;
            data.FaintCount = Character.FaintCount;
            data.PrimarySchool = Character.PrimarySchool;
            data.BaseStr = Character.BaseStats.Str;
            data.BaseAgi = Character.BaseStats.Agi;
            data.BaseCtrl = Character.BaseStats.Ctrl;
            data.BaseRes = Character.BaseStats.Res;
            data.StatPoints = Character.StatPoints;
            data.MartialPoints = Character.MartialPoints;
            data.NormalKillsTowardMartialPoint = Character.NormalKillsTowardMartialPoint;
            data.LaborStaminaTowardExp = Character.LaborStaminaTowardExp;
            data.SecretManuals = Character.SecretManuals;
            data.LearnedSkills = new List<string>(Character.LearnedSkills);
            data.SlottedSkills = new List<string>
            {
                MartialProgression.SlottedId(Character, CombatSlotType.TheCong),
                MartialProgression.SlottedId(Character, CombatSlotType.TheThu),
                MartialProgression.SlottedId(Character, CombatSlotType.TheBien),
                MartialProgression.SlottedId(Character, CombatSlotType.TheSongThe),
            };
            data.LastSchoolChangeCycle = Character.LastSchoolChangeCycle;
            data.QuickBagItems = QuickBag.Save(Character);
            data.OwnedEquipment = new List<string>(Character.OwnedEquipment);
            data.EquippedItems = Character.Equipment.Values.Where(i => i != null).Select(i => i!.Id).ToList();

            // Cốt truyện & Cơ chế chuyên sâu
            data.StoryChapter = Story.CurrentChapter;
            data.ChosenEnding = Story.ChosenEnding;
            data.SeventhChannelCut = Story.SeventhChannelCut;
            data.HerdEmptySpotObserved = Story.HerdEmptySpotObserved;
            data.MidGameGateUnlocked = DeepMechanics.MidGameGateUnlocked;
            data.UnlockedSpecialtyLocksCount = DeepMechanics.UnlockedSpecialtyLocksCount;
            data.EquippedPaperEnhancementLevel = DeepMechanics.intEquippedPaperEnhancementLevel;
            data.EquippedPaperColor = (int)DeepMechanics.EquippedPaperColor;
            data.EnhancementDustCount = DeepMechanics.EnhancementDustCount;
            data.EnhancementStonesCount = DeepMechanics.EnhancementStonesCount;
            data.EngravedRunes = new List<string>(Character.EngravedRunes);
            data.EngravedSeals = new List<string>(Character.EngravedSeals);
            data.HasEngravingTable = CoreAndVillage.HasHighTierEngravingTableBlueprint;
            data.RuneFailedOnDay = CoreAndVillage.RuneFailedOnDay;
            data.SickSalePenaltyDays = Economy.DayMarket.UnquarantinedSickPenaltyDaysRemaining;
            data.PendingThreatBudgetBonus = Defense.PendingThreatBudgetBonus;
            data.CleaningsToday = Farm.CleaningsToday;
            data.PigsSoldToday = Economy.DayMarket.CurrentPigsSoldToday;
            foreach (var kv in Economy.DayMarket.BreedSoldTodayCounts) data.BreedSoldToday.Add($"{(int)kv.Key}:{kv.Value}");
            foreach (var r in NpcRelations.Relations.Values)
                data.NpcRelations.Add(new NpcRelationSave { Id = (int)r.Id, QuestStage = r.QuestStage, Affinity = r.Affinity, TermsCompleted = r.TermsCompleted, FavorDoneCycle = r.FavorDoneCycle, FavorBaselineCycle = r.FavorBaselineCycle, FavorBaseline = r.FavorBaseline });
            foreach (var c in NpcRelations.Contracts)
                data.NpcContracts.Add(new NpcContractSave { Id = (int)c.Id, Kind = (int)c.Kind, Terms = c.Terms, TotalPrice = c.TotalPrice, Deposit = c.Deposit, InstallmentsLeft = c.InstallmentsLeft, InstallmentAmount = c.InstallmentAmount, NextDueDay = c.NextDueDay, LargeCycleDays = new List<int>(c.LargeCycleDays), StartDay = c.StartDay, TrustSkipUsed = c.TrustSkipUsed, UnitPrice = c.UnitPrice, PaidInstallments = c.PaidInstallments });
            foreach (var kv in NpcRelations.Counters) data.NpcCounters.Add($"{(int)kv.Key}:{kv.Value}");
            data.CyclesSinceRuneSystemUnlocked = CoreAndVillage.CyclesSinceRuneSystemUnlocked;
            data.RuneSystemLastCountedCycle = CoreAndVillage.RuneSystemLastCountedCycle;
            data.RedEventsCompleted = CoreAndVillage.RedEventsCompletedCount;
            data.PowerGeneratorLevel = DeepMechanics.PowerGrid.GeneratorLevel;

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
                    XichMaoDomesticationDays = pig.XichMaoDomesticationDays,
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
                    DaysDeceased = pig.DaysDeceased,
                    PositionX = pig.PositionX,
                    PositionY = pig.PositionY,
                    Hunger = pig.Hunger,
                    Comfort = pig.Comfort,
                    FedToday = pig.FedToday,
                    LastPaidRationDay = pig.LastPaidRationDay,
                    OutsideServiceHoursToday = pig.OutsideServiceHoursToday,
                    IsIsolated = pig.IsIsolated,
                    ActiveDisease = pig.ActiveDisease,
                    ImmuneDays = pig.ImmuneDays,
                    SeizurePending = pig.SeizurePending,
                    LinhChiFedDay = pig.LinhChiFedDay,
                    CalmFeedDay = pig.CalmFeedDay,
                    HeatCollapsedHours = pig.HeatCollapsedHours,
                    LastSeizureMinute = pig.LastSeizureMinute,
                    MotherId = pig.MotherId,
                    FatherId = pig.FatherId,
                    Generation = pig.Generation,
                    IsInbred = pig.IsInbred,
                    GestationDaysRemaining = pig.GestationDaysRemaining,
                    LittersBorn = pig.LittersBorn,
                    DaysSinceLastBirth = pig.DaysSinceLastBirth,
                    PendingFatherId = pig.PendingFatherId,
                    PendingFatherGene = pig.PendingFatherGene,
                    PendingFatherStage = pig.PendingFatherStage,
                    PendingInbred = pig.PendingInbred,
                    ShapingDaysRecorded = pig.ShapingDaysRecorded,
                    ShapingDaysFed = pig.ShapingDaysFed,
                    ShapingMoodSum = pig.ShapingMoodSum,
                    ShapingHabitatSum = pig.ShapingHabitatSum,
                    ShapingSickDays = pig.ShapingSickDays,
                    Traits = new List<PigTrait>(pig.Traits),
                    ShapingHabitatMin = pig.ShapingHabitatMin,
                    SecondaryTraitsRolled = pig.SecondaryTraitsRolled,
                    LatentDisease = pig.LatentDisease,
                    LastMatingDay = pig.LastMatingDay,
                    MaxHealth = pig.MaxHealth,
                    FeedConversionMultiplier = pig.FeedConversionMultiplier,
                    InfectionRiskMultiplier = pig.InfectionRiskMultiplier,
                    LatentDiseaseRiskMultiplier = pig.LatentDiseaseRiskMultiplier,
                    AppraisalFailureCount = pig.AppraisalFailureCount,
                    AppraisalLockHoursRemaining = pig.AppraisalLockHoursRemaining
                });
            }

            // Công trình phòng thủ
            data.HallLevel = Defense.HallLevel;
            data.UnlockedBlueprints = new List<string>(Defense.UnlockedBlueprints);
            data.Buildings = new List<DefenseBuildingInstance>(Defense.Buildings);
            data.ConstructionOrders = Defense.ConstructionOrders.ConvertAll(o => o.Copy()); // GDD 13.2 đang thi công

            // Trạng thái Xương sống Kiến trúc chuẩn Stardew Valley 1.6
            data.LedgerTransactions = new List<LedgerTransactionRecord>(Ledger.Transactions);
            data.VillageStockDay = Village.StockDay;
            data.VillageSoldSlots = new List<string>(Village.SoldSlots);
            data.EconomyIntel = CaptureEconomyIntel();   // GDD 20.2 theme / scarcity, GDD 23 intel
            data.CurrentLocationId = WorldLocations.CurrentLocationId;
            data.SpaceBag = SpaceBag.Capture();
            data.PlayerCarry = Carry.Capture();
            data.OccultExpedition = ForestAndEvents.Expedition.CreateSaveData();
            data.ProtagonistTraits = TraitState.Clone();
            data.PlayerWeatherStatus = WeatherStatus.Capture();
            data.SpecialtyLocks = DeepMechanics.SpecialtyLocks.Clone();
            data.PlayerTileX = WorldLocations.PlayerTileX;
            data.PlayerTileY = WorldLocations.PlayerTileY;
            data.UnifiedInventoryItems = new List<UnifiedItemInstance>();
            for (int i = 0; i < Inventory.Slots.Count; i++)
            {
                data.UnifiedInventoryItems.Add(Inventory.Slots[i].Clone());
            }
            data.TriggerActionsRun = new List<string>(TriggerManager.TriggerActionsRun);
            data.EventsSeen = new List<string>(TriggerManager.EventsSeen);
            data.Mailbox = new List<string>(Mailbox.Mailbox);
            data.MailForTomorrow = new List<string>(Mailbox.MailForTomorrow);
            data.MailReceived = new List<string>(Mailbox.MailReceived);
            data.ActiveConversationTopics = new Dictionary<string, int>(ConversationTopics.ActiveTopicsDaysRemaining);
            data.PlayerStatsValues = new Dictionary<string, uint>(PlayerStats.Values);
            data.Narrative = CaptureNarrative();

            return data;
        }

        public void RestoreFromSave(GameSaveData data)
        {
            if (data == null) return;
            var restoredBag = new SpaceBag();
            if (!restoredBag.TryRestore(data.SpaceBag, out var bagError))
                throw new ArgumentException(bagError, nameof(data));
            if (!OccultExpeditionSession.IsValidSnapshot(data.OccultExpedition, out var expeditionError))
                throw new ArgumentException(expeditionError, nameof(data));
            RepairOrders.Clear();
            if (data.RepairOrders != null)
                foreach (var order in data.RepairOrders)
                    if (order != null && order.RemainingMinutes >= 0 && !string.IsNullOrWhiteSpace(order.BuildingId))
                        RepairOrders.Add(order.Copy());
            if (data == null)
                return;

            data = SaveSystem.MigrateForFarmDayClock(data);
            if (data.PlayableMap != null) ConfigurePlayableMap(data.PlayableMap);
            Drove.PigIds.Clear();
            if (data.DrovePigIds != null) Drove.PigIds.AddRange(data.DrovePigIds);
            Drove.AtMarket = data.DroveAtMarket;
            Farm.TempShadePigsToday = data.TempShadePigsToday;
            FogWarningUntilMinute = data.FogWarningUntilMinute;
            GridZones.TempShades.Clear();
            if (data.TempShadeXY != null)
                for (int i = 0; i + 1 < data.TempShadeXY.Count; i += 2) GridZones.TempShades.Add((data.TempShadeXY[i], data.TempShadeXY[i + 1]));
            LastDailyProcessedFarmDay = Math.Max(0, data.LastDailyProcessedFarmDay);
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
            Clock.RestoreForecast(data.IsGoldenSeason, data.MajorWarningDays, data.MinorWaveNoticeHours);

            Farm.FarmLevel = data.FarmLevel;
            Farm.FarmExp = data.FarmExp;
            Farm.HabitatIndex = data.HabitatIndex;
            Farm.EventPressureIndex = data.EventPressureIndex;
            Farm.Gold = data.Gold;
            Farm.FuelLiters = data.FuelLiters;
            Farm.CorpseLotCount = data.CorpseLotCount;
            Farm.FeedStockKg = data.FeedStockKg;
            Farm.MedicalHerbCount = data.MedicalHerbCount;
            Farm.VaccineDoseCount = data.VaccineDoseCount;
            Farm.LocalCrowdingActive = data.LocalCrowdingActive;
            Farm.LocalCrowdingContagionMultiplier = data.LocalCrowdingContagionMultiplier > 0f ? data.LocalCrowdingContagionMultiplier : 1.0f;
            Farm.LocalCrowdingPanicBonus = data.LocalCrowdingPanicBonus;
            Farm.IsBarnContaminated = data.IsBarnContaminated;
            Ecosystem.ContaminatedTroughsCount = Math.Clamp(data.ContaminatedTroughsCount, 0, 10);
            Farm.Infrastructure = data.Infrastructure ?? new FarmInfrastructure();

            Herd.RestoreState(
                data.HerdTier,
                data.HerdConsecutiveDaysInState,
                data.HerdTransmissionStacks,
                data.HerdLeader,
                data.HerdSuccessionTraining);

            GridZones.RestoreGridState(data.PlacedFarmStructures, data.ActiveCarcassJobs);

            if (data.ActivePets != null)
            {
                PetsAndNpcs.ActivePets.Clear();
                foreach (var pet in data.ActivePets)
                {
                    if (pet != null) PetsAndNpcs.ActivePets.Add(pet);
                }
            }

            if (data.HiredNpcs != null)
            {
                PetsAndNpcs.HiredNpcs.Clear();
                foreach (var npc in data.HiredNpcs)
                {
                    if (npc != null) PetsAndNpcs.HiredNpcs.Add(npc);
                }
            }

            if (data.ResearchedDiseases != null)
            {
                PetsAndNpcs.ResearchedDiseases.Clear();
                foreach (var d in data.ResearchedDiseases)
                {
                    PetsAndNpcs.ResearchedDiseases.Add(d);
                }
            }

            Defense.HallLevel = data.HallLevel;
            ConstructionQueue.RestoreOrders(Defense, data.ConstructionOrders); // GDD 13.2: old saves have none
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
                        FireMode = savedBuilding.FireMode ?? "TuDo",
                        PosX = savedBuilding.PosX,
                        PosY = savedBuilding.PosY,
                        HasPosition = savedBuilding.HasPosition,
                        ShieldPoints = savedBuilding.ShieldPoints,
                        WireCutBy = savedBuilding.WireCutBy ?? string.Empty,
                        WearCarry = savedBuilding.WearCarry,
                        HitDamageCarry = savedBuilding.HitDamageCarry,
                        AmmoUsedThisWave = savedBuilding.AmmoUsedThisWave,
                        DurabilityLostFiring = savedBuilding.DurabilityLostFiring,
                        DurabilityLostHits = savedBuilding.DurabilityLostHits
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
            if (data.MaxGuard > 0f) Character.MaxGuard = data.MaxGuard;
            Character.CurrentGuard = Math.Max(0f, data.CurrentGuard);
            Character.LowGuardStaminaScaled = Character.IsGuardLow; // saved stamina is already scaled
            if (data.MaxStamina > 0f) Character.Stamina.MaxStamina = data.MaxStamina;
            Character.Stamina.CurrentStamina = Math.Max(0f, data.CurrentStamina);
            Character.Stamina.DailyLoad = data.DailyLoad;
            Character.Stamina.Condition = Math.Clamp(data.Condition, 0, 100);
            Character.Stamina.ReboundFatigueStacks = data.ReboundFatigueStacks;
            Character.Stamina.IsExhausted = data.StaminaExhausted;
            Character.Stamina.MealsToday = data.MealsToday;
            Character.Stamina.LastMealMinute = data.LastMealMinute;
            Character.Stamina.ForcedRestToday = data.ForcedRestToday;
            Character.FaintCount = data.FaintCount;
            Character.PrimarySchool = data.PrimarySchool;
            if (data.BaseStr > 0)
            {
                Character.BaseStats.Str = data.BaseStr;
                Character.BaseStats.Agi = data.BaseAgi;
                Character.BaseStats.Ctrl = data.BaseCtrl;
                Character.BaseStats.Res = data.BaseRes;
            }
            Character.Level = Math.Max(1, Character.Level);
            Character.StatPoints = data.StatPoints;
            Character.MartialPoints = data.MartialPoints;
            Character.NormalKillsTowardMartialPoint = data.NormalKillsTowardMartialPoint;
            Character.LaborStaminaTowardExp = data.LaborStaminaTowardExp;
            Character.SecretManuals = data.SecretManuals;
            Character.LearnedSkills = new List<string>(data.LearnedSkills ?? new List<string>());
            var slots = MartialProgression.AllSlots;
            Character.LastSchoolChangeCycle = data.LastSchoolChangeCycle;
            QuickBag.Load(Character, data.QuickBagItems);
            for (int k = 0; k < slots.Length; k++)
                Character.ActiveMartialSkills[slots[k]] = data.SlottedSkills != null && k < data.SlottedSkills.Count ? data.SlottedSkills[k] ?? "" : "";
            Character.OwnedEquipment = new List<string>(data.OwnedEquipment ?? new List<string>());
            foreach (EquipSlot es in Enum.GetValues(typeof(EquipSlot))) Character.Equipment[es] = null;
            if (data.EquippedItems != null)
                foreach (var id in data.EquippedItems)
                {
                    var item = EquipmentAndTacticalCatalog.Find(id);
                    if (item != null) Character.Equipment[item.Slot] = item;
                }
            if (Character.OwnedEquipment.Count == 0) Character.EquipStarterSet(); // saves from before equipment existed
            Character.RecalculateBonusStats();
            // keep the saved bars exactly (save → load → save must not drift)
            if (data.MaxHp > 0) Character.MaxHp = data.MaxHp;
            Character.Hp = data.Hp;
            if (data.MaxGuard > 0f) Character.MaxGuard = data.MaxGuard;
            Character.CurrentGuard = Math.Max(0f, data.CurrentGuard);
            Character.LowGuardStaminaScaled = Character.IsGuardLow; // saved stamina is already scaled

            Story.CurrentChapter = data.StoryChapter;
            Story.ChosenEnding = data.ChosenEnding;
            Story.SeventhChannelCut = data.SeventhChannelCut;
            Story.HerdEmptySpotObserved = data.HerdEmptySpotObserved;
            DeepMechanics.MidGameGateUnlocked = data.MidGameGateUnlocked;
            DeepMechanics.UnlockedSpecialtyLocksCount = data.UnlockedSpecialtyLocksCount;
            DeepMechanics.intEquippedPaperEnhancementLevel = data.EquippedPaperEnhancementLevel;
            DeepMechanics.EquippedPaperColor = (SpecialtyPaperColor)Math.Max(0, Math.Min(3, data.EquippedPaperColor));
            DeepMechanics.EnhancementDustCount = data.EnhancementDustCount;
            DeepMechanics.EnhancementStonesCount = data.EnhancementStonesCount;
            Character.EngravedRunes.Clear();
            if (data.EngravedRunes != null) Character.EngravedRunes.AddRange(data.EngravedRunes);
            Character.EngravedSeals.Clear();
            if (data.EngravedSeals != null) Character.EngravedSeals.AddRange(data.EngravedSeals);
            // the saved bars are authoritative (they already include the seal penalty); only the bonus stats are rebuilt
            int keepHp = Character.Hp, keepMaxHp = Character.MaxHp; float keepGuard = Character.CurrentGuard, keepMaxGuard = Character.MaxGuard;
            Character.RecalculateBonusStats();
            Character.MaxHp = keepMaxHp; Character.Hp = keepHp; Character.MaxGuard = keepMaxGuard; Character.CurrentGuard = keepGuard;
            CoreAndVillage.HasHighTierEngravingTableBlueprint = data.HasEngravingTable;
            CoreAndVillage.RuneFailedOnDay = data.RuneFailedOnDay;
            if (data.BaseSeed != 0) BaseSeed = data.BaseSeed;
            Economy.DayMarket.UnquarantinedSickPenaltyDaysRemaining = data.SickSalePenaltyDays;
            Defense.PendingThreatBudgetBonus = data.PendingThreatBudgetBonus;
            Farm.CleaningsToday = data.CleaningsToday;
            Economy.DayMarket.CurrentPigsSoldToday = data.PigsSoldToday;
            Economy.DayMarket.BreedSoldTodayCounts.Clear();
            if (data.BreedSoldToday != null)
                foreach (var kv in data.BreedSoldToday)
                {
                    var parts = kv.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var g) && int.TryParse(parts[1], out var n)) Economy.DayMarket.BreedSoldTodayCounts[(GeneLineId)g] = n;
                }
            NpcRelations = new NpcRelationsState();
            if (data.NpcRelations != null)
                foreach (var r in data.NpcRelations)
                {
                    var rel = NpcRelations.Get((NpcSpecialistId)r.Id);
                    rel.QuestStage = r.QuestStage; rel.Affinity = r.Affinity; rel.TermsCompleted = r.TermsCompleted;
                    rel.FavorDoneCycle = r.FavorDoneCycle; rel.FavorBaselineCycle = r.FavorBaselineCycle; rel.FavorBaseline = r.FavorBaseline;
                }
            if (data.NpcContracts != null)
                foreach (var c in data.NpcContracts)
                    NpcRelations.Contracts.Add(new NpcContract { Id = (NpcSpecialistId)c.Id, Kind = (ContractKind)c.Kind, Terms = c.Terms, TotalPrice = c.TotalPrice, Deposit = c.Deposit, InstallmentsLeft = c.InstallmentsLeft, InstallmentAmount = c.InstallmentAmount, NextDueDay = c.NextDueDay, LargeCycleDays = (c.LargeCycleDays ?? new List<int>()).ToArray(), StartDay = c.StartDay, TrustSkipUsed = c.TrustSkipUsed, UnitPrice = c.UnitPrice, PaidInstallments = c.PaidInstallments });
            // Bản lưu cũ: NPC thuê theo lương ngày → chuyển sang hợp đồng đã trả đủ tới hết chu kỳ này (ADR-0026), bỏ lương ngày
            foreach (var hired in PetsAndNpcs.HiredNpcs)
            {
                if (hired.DailyWageGold <= 0) continue; // chỉ bản ghi cũ (còn lương ngày) mới cần chuyển
                hired.DailyWageGold = 0;
                if (NpcRelations.ContractOf(hired.Id) != null || NpcContracts.Profile(hired.Id) == null) continue;
                var rel = NpcRelations.Get(hired.Id);
                if (rel.QuestStage < 1) rel.QuestStage = 1;
                NpcRelations.Contracts.Add(new NpcContract { Id = hired.Id, Kind = ContractKind.ChuKyNho, Terms = 1, InstallmentsLeft = 1, InstallmentAmount = 0, StartDay = Clock.CurrentDay, UnitPrice = NpcContracts.CyclePrice(this, NpcContracts.Profile(hired.Id)!) });
            }
            if (data.NpcCounters != null)
                foreach (var kv in data.NpcCounters)
                {
                    var parts = kv.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var k) && int.TryParse(parts[1], out var v)) NpcRelations.Counters[(NpcMetric)k] = v;
                }
            CoreAndVillage.CyclesSinceRuneSystemUnlocked = data.CyclesSinceRuneSystemUnlocked;
            CoreAndVillage.RuneSystemLastCountedCycle = data.RuneSystemLastCountedCycle;
            CoreAndVillage.RedEventsCompletedCount = data.RedEventsCompleted;
            if (data.PowerGeneratorLevel > 0) DeepMechanics.PowerGrid.GeneratorLevel = data.PowerGeneratorLevel;

            CurrentWeather = data.CurrentWeather;
            WeatherDaysRemaining = data.WeatherDaysRemaining;
            Quests.Load(data.QuestsCompleted, data.QuestSignals, data.StoryEnding);
            DeepMechanics.CycleBossesDefeatedCount = data.CycleBossesDefeated;
            MajorWavesSurvived = data.MajorWavesSurvived;
            MinorWavesDefeated = data.MinorWavesDefeated;
            TotalPigsSold = data.TotalPigsSold;
            TotalGoldEarned = data.TotalGoldEarned;

            Ledger.Transactions.Clear();
            if (data.LedgerTransactions != null) Ledger.Transactions.AddRange(data.LedgerTransactions);
            Village.RestoreStock(data.VillageStockDay, data.VillageSoldSlots);
            RestoreEconomyIntel(data.EconomyIntel);
            if (!string.IsNullOrWhiteSpace(data.CurrentLocationId))
            {
                WorldLocations.CurrentLocationId = data.CurrentLocationId;
                WorldLocations.PlayerTileX = data.PlayerTileX;
                WorldLocations.PlayerTileY = data.PlayerTileY;
            }
            if (data.UnifiedInventoryItems != null)
            {
                Inventory.ClearAndRestore(data.UnifiedInventoryItems);
            }
            SpaceBag = restoredBag;
            Carry.Restore(data.PlayerCarry, restoredBag);
            ForestAndEvents.Expedition.Restore(data.OccultExpedition);
            TraitState = data.ProtagonistTraits?.Clone() ?? new ProtagonistTraitState(); // null in old saves
            WeatherStatus.Restore(data.PlayerWeatherStatus); // null in old saves
            DeepMechanics.SpecialtyLocks = data.SpecialtyLocks?.Clone() ?? new SpecialtyLockTracker();
            // Old prototypes permitted a permanent forest road. Keep all possessions and resume at the pasture.
            if (string.Equals(WorldLocations.CurrentLocationId, "Forest_Primeval", StringComparison.OrdinalIgnoreCase) &&
                !ForestAndEvents.Expedition.IsActive)
            {
                WorldLocations.CurrentLocationId = "Farm";
                WorldLocations.PlayerTileX = 0;
                WorldLocations.PlayerTileY = 0;
            }
            if (data.TriggerActionsRun != null)
            {
                TriggerManager.TriggerActionsRun.Clear();
                foreach (var t in data.TriggerActionsRun)
                {
                    if (!string.IsNullOrWhiteSpace(t)) TriggerManager.TriggerActionsRun.Add(t);
                }
            }
            if (data.EventsSeen != null)
            {
                TriggerManager.EventsSeen.Clear();
                foreach (var e in data.EventsSeen)
                {
                    if (!string.IsNullOrWhiteSpace(e)) TriggerManager.EventsSeen.Add(e);
                }
            }
            if (data.Mailbox != null)
            {
                Mailbox.Mailbox.Clear();
                foreach (var m in data.Mailbox)
                {
                    if (!string.IsNullOrWhiteSpace(m)) Mailbox.Mailbox.Add(m);
                }
            }
            if (data.MailForTomorrow != null)
            {
                Mailbox.MailForTomorrow.Clear();
                foreach (var m in data.MailForTomorrow)
                {
                    if (!string.IsNullOrWhiteSpace(m)) Mailbox.MailForTomorrow.Add(m);
                }
            }
            if (data.MailReceived != null)
            {
                Mailbox.MailReceived.Clear();
                foreach (var m in data.MailReceived)
                {
                    if (!string.IsNullOrWhiteSpace(m)) Mailbox.MailReceived.Add(m);
                }
            }
            RestoreNarrative(data.Narrative);
            if (data.ActiveConversationTopics != null)
            {
                ConversationTopics.ActiveTopicsDaysRemaining.Clear();
                foreach (var kv in data.ActiveConversationTopics)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value > 0)
                        ConversationTopics.ActiveTopicsDaysRemaining[kv.Key] = kv.Value;
                }
            }
            if (data.PlayerStatsValues != null)
            {
                PlayerStats.Values.Clear();
                foreach (var kv in data.PlayerStatsValues)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key))
                        PlayerStats.Values[kv.Key] = kv.Value;
                }
                PlayerStats.CheckForAchievements();
            }

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
                    pig.XichMaoDomesticationDays = savedPig.XichMaoDomesticationDays;
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
                    pig.PositionX = savedPig.PositionX;
                    pig.PositionY = savedPig.PositionY;
                    pig.Hunger = savedPig.Hunger;
                    pig.Comfort = savedPig.Comfort; // trường thiếu trong bản lưu cũ đã mặc định 70 (PigSaveData)
                    pig.FedToday = savedPig.FedToday;
                    pig.LastPaidRationDay = savedPig.LastPaidRationDay;
                    pig.OutsideServiceHoursToday = savedPig.OutsideServiceHoursToday;
                    pig.IsIsolated = savedPig.IsIsolated;
                    pig.ActiveDisease = savedPig.ActiveDisease;
                    pig.ImmuneDays = savedPig.ImmuneDays;
                    pig.SeizurePending = savedPig.SeizurePending;
                    pig.LinhChiFedDay = savedPig.LinhChiFedDay;
                    pig.CalmFeedDay = savedPig.CalmFeedDay;
                    pig.HeatCollapsedHours = savedPig.HeatCollapsedHours;
                    pig.LastSeizureMinute = savedPig.LastSeizureMinute;
                    pig.MotherId = savedPig.MotherId ?? string.Empty;
                    pig.FatherId = savedPig.FatherId ?? string.Empty;
                    pig.Generation = savedPig.Generation;
                    pig.IsInbred = savedPig.IsInbred;
                    pig.GestationDaysRemaining = savedPig.GestationDaysRemaining;
                    pig.LittersBorn = savedPig.LittersBorn;
                    pig.DaysSinceLastBirth = savedPig.DaysSinceLastBirth;
                    pig.PendingFatherId = savedPig.PendingFatherId ?? string.Empty;
                    pig.PendingFatherGene = savedPig.PendingFatherGene;
                    pig.PendingFatherStage = savedPig.PendingFatherStage;
                    pig.PendingInbred = savedPig.PendingInbred;
                    pig.ShapingDaysRecorded = savedPig.ShapingDaysRecorded;
                    pig.ShapingDaysFed = savedPig.ShapingDaysFed;
                    pig.ShapingMoodSum = savedPig.ShapingMoodSum;
                    pig.ShapingHabitatSum = savedPig.ShapingHabitatSum;
                    pig.ShapingSickDays = savedPig.ShapingSickDays;
                    if (savedPig.Traits != null && savedPig.Traits.Count > 0)
                    {
                        pig.Traits = new List<PigTrait>(savedPig.Traits);
                    }
                    pig.ShapingHabitatMin = savedPig.ShapingHabitatMin;
                    pig.SecondaryTraitsRolled = savedPig.SecondaryTraitsRolled;
                    pig.LatentDisease = savedPig.LatentDisease;
                    pig.LastMatingDay = savedPig.LastMatingDay;
                    pig.MaxHealth = savedPig.MaxHealth > 0 ? savedPig.MaxHealth : 100;
                    pig.FeedConversionMultiplier = savedPig.FeedConversionMultiplier > 0f ? savedPig.FeedConversionMultiplier : 1f;
                    pig.InfectionRiskMultiplier = savedPig.InfectionRiskMultiplier > 0f ? savedPig.InfectionRiskMultiplier : 1f;
                    pig.LatentDiseaseRiskMultiplier = savedPig.LatentDiseaseRiskMultiplier > 0f ? savedPig.LatentDiseaseRiskMultiplier : 1f;
                    pig.AppraisalFailureCount = savedPig.AppraisalFailureCount;
                    pig.AppraisalLockHoursRemaining = savedPig.AppraisalLockHoursRemaining;

                    Pigs.Add(pig);
                }
            }
            Story.SwitchProtagonistPerspective(Character.Identity);
        }
    }
}
