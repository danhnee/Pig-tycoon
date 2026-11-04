#if PIGTYCOON_PLAYTEST
// QA playtest bot for the pure-logic Core (GameEngine and friends). It plays whole games through public Core APIs,
// checks invariants after every farm day and runs targeted exploit probes. Nothing here changes Core state except
// through the same calls the runtime makes; where a probe injects state to reach a late-game rule quickly it says so.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PigTycoon.Core;

namespace PigTycoon.Playtest
{
    sealed class Violation
    {
        public string Severity = "P1";
        public string System = "";
        public string Key = "";
        public int Seed;
        public int Day;
        public string Action = "";
        public string Detail = "";
        public int Count = 1;
    }

    static class Report
    {
        public static readonly Dictionary<string, Violation> ByKey = new Dictionary<string, Violation>();
        public static readonly List<string> Notes = new List<string>();

        public static void V(string sev, string system, string key, int seed, int day, string action, string detail)
        {
            string k = system + "|" + key;
            if (ByKey.TryGetValue(k, out var v)) { v.Count++; return; }
            ByKey[k] = new Violation { Severity = sev, System = system, Key = key, Seed = seed, Day = day, Action = action, Detail = detail };
        }

        public static void Note(string s) { Notes.Add(s); Console.WriteLine("  [note] " + s); }
    }

    static class F
    {
        public static bool Bad(float x) => float.IsNaN(x) || float.IsInfinity(x);
        public static bool Bad(double x) => double.IsNaN(x) || double.IsInfinity(x);
        public static string G(float x) => x.ToString("0.##", CultureInfo.InvariantCulture);
    }

    sealed class CycleRow
    {
        public int Cycle, StartDay, RolledLength, GoldAtStart, HerdAtStart, Adults;
        public int ActualDays;
        public float FeedAtStart;
        public int NpcContracts;
    }

    sealed class ContractTrack
    {
        public NpcSpecialistId Id;
        public ContractKind Kind;
        public int SignedDay;
        public int Terms;
        public int TotalPrice;
        public int Deposit;
        public long Paid;              // deposits + installments seen leaving the ledger for this NPC
        public int MajorWins, AllWaves;
        public bool Flagged;
    }

    sealed class Bot
    {
        readonly int seed;
        readonly PlayerId hero;
        readonly Random rng;
        public GameEngine e = null!;
        string action = "";
        int waveIndex = 0;
        public readonly List<CycleRow> Cycles = new List<CycleRow>();
        public readonly Dictionary<NpcMetric, (int first, int last)> MetricRange = new Dictionary<NpcMetric, (int, int)>();
        readonly Dictionary<NpcSpecialistId, ContractTrack> tracks = new Dictionary<NpcSpecialistId, ContractTrack>();
        public int MajorLosses, MinorWaves, MajorWins, Faints, NightBartersOk, NightBarterTries, FeedBuys, PigsSold, Litters, Breeds;
        public int SignOk, RenewOk, Terminations, Defaults, Completions;
        public int RoundTrips, StuckDays;
        public int UnfedPigDays;
        int lastProcessedCheckDay;

        public Bot(int seed, PlayerId hero)
        {
            this.seed = seed;
            this.hero = hero;
            rng = new Random(seed * 7 + 1);
        }

        void V(string sev, string sys, string key, string detail) => Report.V(sev, sys, key, seed, e.Clock.CurrentDay, action, detail);

        public void Run(int cycleTarget, int maxDays)
        {
            e = new GameEngine(seed);
            NewGameSetup.ChooseProtagonist(e, hero);
            StarterHerd.FillTo(e);
            StartCycleRow();
            foreach (NpcMetric m in Enum.GetValues(typeof(NpcMetric))) { int v = NpcContracts.Metric(e, m); MetricRange[m] = (v, v); }
            lastProcessedCheckDay = e.Clock.CurrentDay;
            while (e.Clock.CycleNumber <= cycleTarget && e.Clock.CurrentDay < maxDays)
            {
                try { PlayOneDay(); }
                catch (Exception ex)
                {
                    V("P0", "Crash", "exception:" + ex.GetType().Name + ":" + ex.TargetSite?.Name, ex.GetType().Name + ": " + ex.Message + " @ " + FirstFrame(ex));
                    // advance one day so a single throw does not loop forever
                    try { e.Tick(e.Clock.MinutesUntilNextMorning()); } catch { break; }
                }
            }
            CloseCycleRow();
        }

        static string FirstFrame(Exception ex)
        {
            var st = ex.StackTrace ?? "";
            var line = st.Split('\n').FirstOrDefault(l => l.Contains(".cs:")) ?? st.Split('\n').FirstOrDefault() ?? "";
            return line.Trim();
        }

        void StartCycleRow()
        {
            var living = e.Pigs.Where(p => p.IsAlive).ToList();
            Cycles.Add(new CycleRow
            {
                Cycle = e.Clock.CycleNumber, StartDay = e.Clock.CurrentDay, RolledLength = e.Clock.CycleTotalDays,
                GoldAtStart = e.Farm.Gold, HerdAtStart = living.Count, Adults = living.Count(p => p.Stage == PigStage.TruongThanh || p.Stage == PigStage.HeoGia),
                FeedAtStart = e.Farm.FeedStockKg, NpcContracts = e.NpcRelations.Contracts.Count,
            });
            // GDD 2.3: early (cycle 1–7) 6–8, mid (8–19) 7–10, late (20+) 8–12; Golden season 12–15; losses +1, high pressure −1.
            int c = e.Clock.CycleNumber, len = e.Clock.CycleTotalDays;
            int lo = c <= 7 ? 6 : c <= 19 ? 7 : 8, hi = c <= 7 ? 8 : c <= 19 ? 10 : 12;
            bool golden = len >= 12 && len <= 16;
            if (!golden && (len < lo - 1 || len > hi + 1))
                V("P2", "Cycle", "length-out-of-range", $"cycle {c} rolled {len} days, GDD range {lo}–{hi} (±1 for losses/pressure)");
        }

        void CloseCycleRow()
        {
            if (Cycles.Count == 0) return;
            var row = Cycles[^1];
            row.ActualDays = e.Clock.CurrentDay - row.StartDay;
        }

        // ------------------------------------------------------------------ the day
        public bool Trace;
        /// <summary>Lean = pure farming (no NPCs, building, intel, gear, appraisal): isolates the pig economy.</summary>
        public bool Lean;
        public int BreedTries, BreedFails;
        public readonly Dictionary<string, int> BreedFailReasons = new Dictionary<string, int>();
        void PlayOneDay()
        {
            // the bot always starts its day at 06:00
            int day = e.Clock.CurrentDay;
            if (Trace && day <= 80 && day % 2 == 0)
            {
                var lv = e.Pigs.Where(p => p.IsAlive).ToList();
                Console.WriteLine($"   d{day} {e.Clock.CurrentHour:00}h gold {e.Farm.Gold} pigs {lv.Count} (non {lv.Count(p => p.Stage == PigStage.HeoNon)}, lon {lv.Count(p => p.Stage == PigStage.DangLon)}, tt {lv.Count(p => p.Stage == PigStage.TruongThanh)}, gia {lv.Count(p => p.Stage == PigStage.HeoGia)}; F {lv.Count(p => p.Gender == "Cai")}) preg {lv.Count(p => p.GestationDaysRemaining > 0)} feed {e.Farm.FeedStockKg:0} unfed {lv.Count(p => !p.FedToday)} dead {e.Pigs.Count(p => !p.IsAlive)} hab {e.Farm.HabitatIndex} sick {lv.Count(p => p.ActiveDisease.HasValue)} mood~{(lv.Count > 0 ? lv.Average(p => p.Mood) : 0):0} hp~{(lv.Count > 0 ? lv.Average(p => p.Health) : 0):0} breeds {Breeds}/{BreedTries} sold {PigsSold} faints {Faints} cyc {e.Clock.CycleNumber} {e.Clock.CycleDayIndex}/{e.Clock.CycleTotalDays}");
            }
            int gm = e.Farm.Gold;
            MorningActions();
            int ga = e.Farm.Gold;
            AdvanceTo(12);
            if (Trace && day <= 16) Console.WriteLine($"      d{day} morning {gm}→{ga}, tick to noon →{e.Farm.Gold}");
            if (e.Clock.CurrentDay == day && e.Clock.CurrentHour == 12 && day % 3 == 0) SaveRoundTrip();
            MiddayActions();
            AdvanceTo(20);
            if (e.Clock.CurrentDay == day && e.Clock.CycleDayIndex >= e.Clock.CycleTotalDays) RunWave(major: true);
            AdvanceTo(21);
            NightActions();
            SleepToMorning();
        }

        void AdvanceTo(int hour)
        {
            int guard = 0;
            int startDay = e.Clock.CurrentDay;
            while (e.Clock.CurrentHour != hour && guard++ < 30)
            {
                int dayBefore = e.Clock.CurrentDay;
                action = $"Tick to {hour:00}:00";
                e.Tick(60 - e.Clock.CurrentMinute);
                if (e.Clock.IsMinorWaveTriggered) { e.Clock.IsMinorWaveTriggered = false; RunWave(major: false); }
                if (e.Clock.CurrentDay != dayBefore) AfterDayRollover();
                if (e.Clock.CurrentDay != startDay && hour >= 6) break; // crossed midnight by a rescue; let the caller continue
            }
        }

        void SleepToMorning()
        {
            int day = e.Clock.CurrentDay;
            action = "PlayerRestCommands.Sleep";
            var s = PlayerRestCommands.Sleep(e);
            if (e.Clock.IsMinorWaveTriggered) { e.Clock.IsMinorWaveTriggered = false; RunWave(major: false); }
            int guard = 0;
            while (!(e.Clock.CurrentHour == 6 && e.Clock.CurrentMinute == 0 && e.Clock.CurrentDay > day) && guard++ < 40)
            {
                int dayBefore = e.Clock.CurrentDay;
                if (e.Clock.CurrentDay > day && e.Clock.CurrentHour >= 6) break; // a rescue pushed us past 06:00
                action = s.success ? "Tick after interrupted sleep" : "Tick night (sleep blocked: " + s.message + ")";
                e.Tick(Math.Min(60 - e.Clock.CurrentMinute, Math.Max(1, e.Clock.MinutesUntilNextMorning())));
                if (e.Clock.IsMinorWaveTriggered) { e.Clock.IsMinorWaveTriggered = false; RunWave(major: false); }
                if (e.Clock.CurrentDay != dayBefore) AfterDayRollover();
            }
            if (e.Clock.CurrentDay > day && guard == 0) AfterDayRollover(); // the uninterrupted sleep crossed the day inside Sleep
            if (e.Clock.CurrentDay == day) { V("P1", "Clock", "sleep-no-progress", $"sleep/tick did not reach the next morning (hour {e.Clock.CurrentHour})"); e.Tick(e.Clock.MinutesUntilNextMorning()); AfterDayRollover(); }
            // a rescue may have left us at e.g. 07:00; resync to the next action point naturally (bot acts from now)
        }

        // ------------------------------------------------------------------ morning
        void MorningActions()
        {
            var c = e.Character;
            // yard work: the overview popup's cleaning (habitat) and the corpse-lot sanitation labor (quest counter)
            for (int i = 0; i < 3; i++) { action = "HerdLifecycleRules.TryCleanYard"; HerdLifecycleRules.TryCleanYard(e); }
            for (int i = 0; i < 2; i++) { action = "FarmLaborStamina.TrySpend(CleanPlot)"; FarmLaborStamina.TrySpend(e, FarmLaborTask.CleanPlot); }
            action = "FarmLaborStamina.TrySpend(FeedTrough/Water)";
            FarmLaborStamina.TrySpend(e, FarmLaborTask.FeedTrough);
            FarmLaborStamina.TrySpend(e, FarmLaborTask.RefillWaterBasin);
            if (c.Stamina.CurrentStamina < c.GetEffectiveMaxStamina() * 0.5f) { action = "EatMeal"; PlayerRestCommands.EatMeal(e, rng.NextDouble() < 0.3); }

            void Step(string name, Action a)
            {
                int g = e.Farm.Gold;
                a();
                if (Trace && e.Clock.CurrentDay <= 16 && e.Farm.Gold != g) Console.WriteLine($"      d{e.Clock.CurrentDay} {name}: gold {g}→{e.Farm.Gold}");
            }
            Step("carcass", ProcessCarcasses);
            if (Trace && e.Clock.CurrentDay <= 16) Console.WriteLine($"      d{e.Clock.CurrentDay} density {PigCareCommands.CurrentDensity(e):0.00} maint {e.Farm.CalculateCapacityReport(e.Pigs.Count(p => p.IsAlive), 0).DailyEmptyMaintenanceCost}");
            Step("feed", BuyFeed);
            Step("sell", SellPigs);
            Step("breed", Breed);
            if (!Lean)
            {
                if (e.Clock.CurrentDay % 3 == 0 && e.Farm.Gold > 2500) Step("appraise", Appraise);
                Step("intel", BuyIntel);
                Step("npc", ManageNpcs);
                Step("build", Build);
                Step("gear", BuyGear);
            }
            Step("martial", Martial);
            CheckInstant("morning");
        }

        void MiddayActions()
        {
            action = "FarmLaborStamina.TrySpend(GatherMaterial)";
            FarmLaborStamina.TrySpend(e, FarmLaborTask.GatherMaterial);
            if (rng.NextDouble() < 0.3) { action = "PlayerRestCommands.Rest(1)"; PlayerRestCommands.Rest(e, 1); }
            CheckInstant("midday");
        }

        public int FeedGoldSpent;
        /// <summary>
        /// Core has no feed shop (Farm.BuyFeedStock has no caller; ADR/GDD give CamThuong GTTC 12G per 3.5 kg sack). The bot buys
        /// sacks at that catalog price × the market's FeedPriceMultiplier through Farm.BuyFeedStock, keeping 4 days of ration.
        /// </summary>
        void BuyFeed()
        {
            float need = e.Pigs.Where(p => p.IsAlive).Sum(p => PigCareCommands.DailyFeedKilograms(e, p));
            if (e.Farm.FeedStockKg >= need * 2f) return;
            int sacks = (int)Math.Ceiling((need * 4f - e.Farm.FeedStockKg) / 3.5f);
            int unit = Math.Max(1, (int)Math.Round(12 * e.Economy.DayMarket.FeedPriceMultiplier));
            sacks = Math.Min(sacks, Math.Max(0, (e.Farm.Gold - 100) / unit));
            if (sacks <= 0) return;
            action = $"Farm.BuyFeedStock({sacks * 3.5f} kg, {sacks * unit}G)";
            if (e.Farm.BuyFeedStock(sacks * 3.5f, sacks * unit)) { FeedGoldSpent += sacks * unit; FeedBuys++; }
        }

        void PrepareDefense()
        {
            foreach (var b in e.Defense.Buildings.ToList())
            {
                if (b.AmmoCapacity <= 0 || b.AmmoCurrent >= b.AmmoCapacity) continue;
                action = $"DefenseReloads.Reload({b.BlueprintId})";
                var q = DefenseReloads.Quote(b, e.Inventory);
                if (q.Gold > e.Farm.Gold - 300) continue;
                DefenseReloads.Reload(e, b.InstanceId);
            }
        }

        void SellPigs()
        {
            var living = e.Pigs.Where(p => p.IsAlive).ToList();
            int females = living.Count(p => p.Gender == "Cai" && (p.Stage == PigStage.TruongThanh));
            int males = living.Count(p => p.Gender == "Duc" && (p.Stage == PigStage.TruongThanh));
            bool poor = e.Farm.Gold < 400;
            int quota = poor ? 4 : (living.Count > 30 ? 4 : 3);
            var cands = living.Where(p => p.Stage == PigStage.HeoGia || (p.Stage == PigStage.TruongThanh && (p.WeightKg >= 95f || p.AgeDays >= 26)))
                .OrderByDescending(p => p.Stage == PigStage.HeoGia).ThenByDescending(p => p.WeightKg).ToList();
            // piglets: keep 1 female piglet per 2 adult sows as replacement stock, sell the rest (the market's flat 315G×0.35 piglet price)
            int youngF = living.Count(p => p.Gender == "Cai" && p.Stage == PigStage.DangLon);
            int youngM = living.Count(p => p.Gender == "Duc" && p.Stage == PigStage.DangLon);
            int keepFemales = Math.Max(0, 10 - females - youngF);
            int keepMales = Math.Max(0, 4 - males - youngM);
            foreach (var pl in living.Where(p => p.Stage == PigStage.HeoNon).ToList())
            {
                if (pl.Gender == "Cai" && keepFemales > 0) { keepFemales--; continue; }
                if (pl.Gender == "Duc" && keepMales > 0) { keepMales--; continue; }
                bool q = !Lean && rng.NextDouble() < 0.3;
                action = $"SellPigAtDayMarket(piglet, quarantine={q})";
                var r = e.SellPigAtDayMarket(pl.Id, q);
                if (r.success) { PigsSold++; PigletsSold++; PigletGold += r.goldEarned; }
            }
            foreach (var p in cands)
            {
                if (quota <= 0) break;
                if (p.Stage == PigStage.TruongThanh && p.Gender == "Cai" && females <= (poor ? 4 : 6)) continue;
                if (p.Stage == PigStage.TruongThanh && p.Gender == "Duc" && males <= 2) continue;
                if (p.GestationDaysRemaining > 0) continue;
                bool quarantine = rng.NextDouble() < 0.8;
                var (price, _) = e.Economy.CalculatePigSalePrice(p, e.Farm.FarmLevel);
                if (price <= 0 || F.Bad((float)price)) V("P1", "Market", "price-nonpositive", $"price {price} for {p.Stage} {p.GeneLine}");
                int g0 = e.Farm.Gold;
                action = $"SellPigAtDayMarket(quarantine={quarantine})";
                var r = e.SellPigAtDayMarket(p.Id, quarantine);
                if (r.success)
                {
                    PigsSold++;
                    quota--;
                    if (p.Gender == "Cai" && p.Stage == PigStage.TruongThanh) females--;
                    if (p.Gender == "Duc" && p.Stage == PigStage.TruongThanh) males--;
                    if (e.Farm.Gold - g0 != r.goldEarned) V("P1", "Market", "sale-gold-mismatch", $"gold moved {e.Farm.Gold - g0} but sale reported {r.goldEarned}");
                    if (r.goldEarned <= 0) V("P1", "Market", "sale-nonpositive", $"sale paid {r.goldEarned}");
                }
            }
        }

        public int CarcassesProcessed, PigletsSold, PigletGold, CarcassFails, Treatments;
        public string LastCarcassFail = "";
        void ProcessCarcasses()
        {
            foreach (var d in e.Pigs.Where(p => !p.IsAlive && !p.IsInProcessingLot).ToList())
            {
                action = "ProcessDeadPigCarcass";
                if (e.ProcessDeadPigCarcass(d.Id, CarcassProcessMethod.Incinerate_ThieuHuy).started) { CarcassesProcessed++; continue; }
                var r = e.ProcessDeadPigCarcass(d.Id, CarcassProcessMethod.BioRecycle_TaiCheSinhHoc);
                if (r.started) CarcassesProcessed++;
                else { CarcassFails++; LastCarcassFail = r.message + $" (gold {e.Farm.Gold}, fuel {e.Farm.FuelLiters})"; }
            }
            // treat the sick (GDD 6.3 verbs): isolate + individual treatment with each drug in turn
            foreach (var sick in e.Pigs.Where(p => p.IsAlive && p.ActiveDisease.HasValue).Take(4).ToList())
            {
                action = "ExecuteHealthTreatment";
                if (!sick.IsIsolated) e.ExecuteHealthTreatment(HealthTreatmentVerb.CachLy, sick.Id);
                foreach (FarmDrug drug in Enum.GetValues(typeof(FarmDrug)))
                {
                    var t = e.ExecuteHealthTreatment(HealthTreatmentVerb.DieuTriCaThe, sick.Id, drug);
                    if (t.success) { Treatments++; break; }
                }
                if (!sick.ActiveDisease.HasValue && sick.IsIsolated) sick.IsIsolated = false; // release after cure (runtime moves it back)
            }
            if (e.Pigs.Any(p => p.IsAlive && p.ActiveDisease.HasValue)) { action = "ExecuteHealthTreatment(XuLyMoiTruong)"; e.ExecuteHealthTreatment(HealthTreatmentVerb.XuLyMoiTruong); }
        }

        void Breed()
        {
            var living = e.Pigs.Where(p => p.IsAlive).ToList();
            if (living.Count > 45 || PigCareCommands.CurrentDensity(e) > 0.65f) return;
            // budget-aware: a litter (~7) eats ~2.5 kg/day each for ~11 days before it can be sold
            float feedKg = living.Sum(p => PigCareCommands.DailyFeedKilograms(e, p));
            int pregnant = living.Count(p => p.GestationDaysRemaining > 0);
            if (pregnant >= 4 || e.Farm.Gold < 300) return;
            int tries = 0;
            foreach (var sow in living.Where(p => p.Gender == "Cai" && p.GestationDaysRemaining <= 0).ToList())
            {
                if (tries >= 3) break;
                if (sow.LastMatingDay >= e.Clock.CurrentDay - 2) continue;
                var partners = e.ListBreedingPartners(sow.Id).Where(m => !e.IsInbredPair(sow, m)).ToList();
                if (partners.Count == 0) continue;
                var boar = partners[rng.Next(partners.Count)];
                tries++;
                action = "SchedulePigBreeding";
                BreedTries++;
                var r = e.SchedulePigBreeding(sow.Id, boar.Id);
                if (r.scheduled) Breeds++;
                else { var key = r.message.Length > 40 ? r.message.Substring(r.message.IndexOf(']') + 1, Math.Min(30, r.message.Length - r.message.IndexOf(']') - 1)) : r.message; BreedFailReasons.TryGetValue(key, out var n); BreedFailReasons[key] = n + 1; }
            }
        }

        void Appraise()
        {
            var p = e.Pigs.FirstOrDefault(x => x.IsAlive && !x.IsIdentified && x.AppraisalLockHoursRemaining <= 0);
            if (p == null) return;
            int g0 = e.Farm.Gold;
            if (rng.NextDouble() < 0.5) return;
            action = "AppraisePigGeneAtChannel(ChoNgay)";
            var r = e.AppraisePigGeneAtChannel(p.Id, AppraisalChannel.ChoNgay);
            int spent = g0 - e.Farm.Gold;
            if (spent != r.costGold && r.costGold > 0) V("P1", "Appraisal", "cost-mismatch", $"gold moved {spent}, reported cost {r.costGold}");
            if (spent < 0) V("P1", "Appraisal", "gold-gain", $"appraisal increased gold by {-spent}");
        }

        void BuyIntel()
        {
            if (e.Clock.CycleDayIndex > 2) return;
            if (e.Intel.Level == 0 && e.Farm.Gold > 2500)
            {
                int g0 = e.Farm.Gold;
                action = "Intel.Purchase(1)";
                var r = e.Intel.Purchase(e, 1);
                if (r.success && g0 - e.Farm.Gold != IntelBureau.CostOf(1)) V("P1", "Intel", "cost", $"paid {g0 - e.Farm.Gold}, expected {IntelBureau.CostOf(1)}");
                if (r.success && rng.NextDouble() < 0.3 && e.Farm.Gold > 1200) { action = "Intel.Purchase(2)"; e.Intel.Purchase(e, 2); }
                action = "Intel.BuildReport";
                var rep = e.Intel.BuildReport(e);
                if (rep.Level != e.Intel.Level) V("P2", "Intel", "report-level", $"report level {rep.Level} vs bought {e.Intel.Level}");
            }
        }

        void Build()
        {
            if (e.Farm.Gold < 3500) return;
            string[] order = { "NoXuyenVan", "ThapCanh", "RaoGo", "UBaoCat", "NoXuyenVan", "RaoGo", "BanChong" };
            foreach (var id in order)
            {
                var ok = ConstructionQueue.CanStart(e, id);
                if (!ok.ok) continue;
                var bp = e.Defense.GetBlueprint(id);
                if (bp == null || bp.BuildCostGold > e.Farm.Gold - 800) continue;
                int g0 = e.Farm.Gold;
                action = $"ConstructionQueue.TryStart({id})";
                var r = ConstructionQueue.TryStart(e, id);
                if (r.success && g0 - e.Farm.Gold != bp.BuildCostGold) V("P1", "Construction", "cost", $"paid {g0 - e.Farm.Gold}, blueprint costs {bp.BuildCostGold}");
                break;
            }
        }

        void BuyGear()
        {
            var c = e.Character;
            if (e.Farm.Gold > 2500 && QuickBag.Count(c, QuickItemId.BinhHoiLuc) < 2) { action = "QuickBag.Buy(BinhHoiLuc)"; QuickBag.Buy(e, QuickItemId.BinhHoiLuc); }
            if (e.Farm.Gold > 2500 && QuickBag.Count(c, QuickItemId.BangGac) < 1) { action = "QuickBag.Buy(BangGac)"; QuickBag.Buy(e, QuickItemId.BangGac); }
            if (e.Farm.Gold > 5000)
            {
                var item = EquipmentAndTacticalCatalog.AllEquipmentItems.Where(i => !c.OwnedEquipment.Contains(i.Id)).OrderBy(i => MartialProgression.PriceGold(i)).FirstOrDefault();
                if (item != null)
                {
                    action = $"MartialProgression.Buy({item.Id})";
                    var r = MartialProgression.Buy(e, item.Id);
                    if (r.ok && rng.NextDouble() < 0.5) { action = $"MartialProgression.Equip({item.Id})"; MartialProgression.Equip(c, item.Id); }
                }
            }
        }

        void Martial()
        {
            var c = e.Character;
            while (c.StatPoints > 0)
            {
                action = "SpendStatPoint";
                var st = rng.NextDouble() < 0.6 ? SkillStat.Str : new[] { SkillStat.Agi, SkillStat.Res, SkillStat.Ctrl }[rng.Next(3)];
                if (!MartialProgression.SpendStatPoint(c, st).ok) break;
            }
            foreach (var s in MartialSkillCatalog.AllSkills.OrderBy(s => s.UnlockTier))
            {
                if (!MartialProgression.CanLearn(c, s).ok) continue;
                var (gold, pts) = MartialProgression.LearnCost(c, s);
                if (gold > e.Farm.Gold - 1000) continue;
                int g0 = e.Farm.Gold, mp0 = c.MartialPoints;
                action = $"MartialProgression.Learn({s.Id})";
                var r = MartialProgression.Learn(e, s.Id);
                if (r.ok && (g0 - e.Farm.Gold != gold || mp0 - c.MartialPoints != pts)) V("P1", "Martial", "learn-cost", $"learn {s.Id}: gold −{g0 - e.Farm.Gold} (expected {gold}), points −{mp0 - c.MartialPoints} (expected {pts})");
                break;
            }
        }

        // ------------------------------------------------------------------ NPCs (ADR-0026)
        void ManageNpcs()
        {
            int cap = NpcContracts.MaxNpcs(e.Farm.FarmLevel);
            foreach (var p in NpcContracts.Profiles)
            {
                var c = e.NpcRelations.ContractOf(p.Id);
                var r = e.NpcRelations.Get(p.Id);
                if (c == null)
                {
                    if (NpcContracts.HireBlockedReason(e, p.Id) != "") continue;
                    ContractKind kind = p.Tier == RoleTier.Cam ? ContractKind.ChuKyLon : p.IsMercenary ? (rng.NextDouble() < 0.5 ? ContractKind.TheoDot : ContractKind.ChuKyNho) : ContractKind.ChuKyNho;
                    int terms = kind == ContractKind.ChuKyLon ? 2 : kind == ContractKind.TheoDot ? NpcContracts.WaveTermOptions[rng.Next(3)] : 1 + rng.Next(2);
                    var q = NpcContracts.QuoteFor(e, p.Id, kind, terms);
                    if (!q.Ok) { V("P2", "NPC contract", "quote-refused:" + p.Id, q.Reason); continue; }
                    bool reckless = rng.NextDouble() < 0.15; // sometimes sign leaving almost no gold for the instalments
                    if (!reckless && e.Farm.Gold < q.Deposit + q.Installment + 2500) continue;
                    if (reckless && e.Farm.Gold < q.Deposit) continue;
                    int expectDeposit = kind == ContractKind.TheoDot ? 0 : (int)Math.Round(q.Total * NpcContracts.DepositRatio);
                    if (q.Deposit != expectDeposit) V("P1", "NPC contract", "deposit-not-30", $"{p.Role} {kind}×{terms}: deposit {q.Deposit} of total {q.Total} (expected {expectDeposit})");
                    long sumInst = (long)q.Deposit + (long)q.Installment * q.Installments;
                    if (sumInst != q.Total) V("P2", "NPC contract", "instalments-sum-ne-total", $"{p.Role} {kind}×{terms}: deposit {q.Deposit} + {q.Installments}×{q.Installment} = {sumInst} vs total {q.Total} (ceil rounding)");
                    int g0 = e.Farm.Gold;
                    action = $"NpcContracts.Sign({p.Id},{kind},{terms})";
                    var s = NpcContracts.Sign(e, p.Id, kind, terms);
                    if (!s.ok) continue;
                    SignOk++;
                    if (g0 - e.Farm.Gold != q.Deposit) V("P1", "NPC contract", "deposit-charged-wrong", $"{p.Role}: gold −{g0 - e.Farm.Gold}, deposit {q.Deposit}");
                    if (e.NpcRelations.Contracts.Count > cap) V("P1", "NPC contract", "over-cap", $"{e.NpcRelations.Contracts.Count} contracts > MaxNpcs({e.Farm.FarmLevel})={cap}");
                    tracks[p.Id] = new ContractTrack { Id = p.Id, Kind = kind, SignedDay = e.Clock.CurrentDay, Terms = terms, TotalPrice = q.Total, Deposit = q.Deposit, Paid = q.Deposit };
                    continue;
                }
                // running contract: renew near the end, rarely terminate
                if (c.InstallmentsLeft == 1 && rng.NextDouble() < 0.5)
                {
                    bool forceDown = rng.NextDouble() < 0.5;
                    int market = NpcContracts.MarketUnitPrice(e, p, c.Kind);
                    int oldUnit = c.UnitPrice;
                    float aff0 = r.Affinity;
                    int terms = c.Kind == ContractKind.TheoDot ? 3 : 1;
                    int g0 = e.Farm.Gold;
                    long owedBefore = (long)c.InstallmentsLeft * c.InstallmentAmount;
                    action = $"NpcContracts.Renew({p.Id},{terms},forceDown={forceDown})";
                    var res = NpcContracts.Renew(e, p.Id, terms, forceDown);
                    if (!res.ok) continue;
                    RenewOk++;
                    int expectUnit = market > oldUnit ? (aff0 >= NpcContracts.PriceLockAffinity ? oldUnit : market) : market < oldUnit ? (forceDown ? market : oldUnit) : market;
                    if (c.UnitPrice != expectUnit) V("P1", "NPC renewal", "unit-rule", $"{p.Role}: market {market}, old {oldUnit}, aff {aff0}, forceDown {forceDown} → unit {c.UnitPrice} (expected {expectUnit})");
                    if (forceDown && market < oldUnit && Math.Abs((aff0 - r.Affinity) - Math.Min(aff0, NpcContracts.ForceDownAffinityLoss)) > 0.01f)
                        V("P1", "NPC renewal", "forcedown-affinity", $"{p.Role}: affinity {aff0} → {r.Affinity} (expected −5)");
                    if (!(forceDown && market < oldUnit) && r.Affinity < aff0 - 0.01f) V("P1", "NPC renewal", "affinity-lost-without-forcedown", $"{p.Role}: {aff0} → {r.Affinity}");
                    if (tracks.TryGetValue(p.Id, out var t)) { t.Paid += g0 - e.Farm.Gold; t.Terms = c.Terms; t.TotalPrice = c.TotalPrice; }
                    continue;
                }
                if (rng.NextDouble() < 0.02)
                {
                    int g0 = e.Farm.Gold; float a0 = r.Affinity;
                    action = $"NpcContracts.Terminate({p.Id})";
                    NpcContracts.Terminate(e, p.Id);
                    Terminations++;
                    tracks.Remove(p.Id);
                    if (e.Farm.Gold != g0) V("P1", "NPC contract", "terminate-gold", $"terminate moved gold by {e.Farm.Gold - g0}");
                    if (e.PetsAndNpcs.HiredNpcs.Any(n => n.Id == p.Id)) V("P1", "NPC contract", "terminate-npc-stays", $"{p.Role} still in HiredNpcs after Terminate");
                }
            }
        }

        // ------------------------------------------------------------------ waves and combat
        void RunWave(bool major)
        {
            if (major) action = "ExecuteLiveCombatWave(major)"; else action = "ExecuteLiveCombatWave(minor)";
            int cycle0 = e.Clock.CycleNumber, majors0 = e.MajorWavesSurvived, minors0 = e.MinorWavesDefeated;
            int pending = e.Defense.PendingThreatBudgetBonus;
            var before = e.NpcRelations.Contracts.ToDictionary(c => c.Id, c => (c.InstallmentsLeft, c.Kind));
            int contracts0 = e.NpcRelations.Contracts.Count;
            int threat = major ? 150 + 10 * (e.Clock.CycleNumber - 1) : Math.Min(120, 50 + 3 * e.Clock.CycleNumber);
            bool boss = major && e.Clock.CycleNumber % 5 == 0;
            var payments0 = NpcLedgerTotals();
            if (major) PrepareDefense();
            int strPart = e.Contracts.PlayerStats.EffectiveStr * 8;
            var res = e.ExecuteLiveCombatWave(++waveIndex, threat, boss, false);
            var payments1 = NpcLedgerTotals();
            foreach (var kv in payments1) if (tracks.TryGetValue(kv.Key, out var t)) t.Paid += kv.Value - (payments0.TryGetValue(kv.Key, out var p0) ? p0 : 0);
            bool victory = res.report.Victory;
            bool countedMajor = e.MajorWavesSurvived > majors0;
            if (!major)
            {
                MinorWaves++;
                if (countedMajor)
                    V("P1", "Waves", "minor-promoted-to-major", $"minor wave with threat {threat} + pending bonus {pending} (≥150) was counted as a major wave: MajorWavesSurvived {majors0}→{e.MajorWavesSurvived}, cycle {cycle0}→{e.Clock.CycleNumber} on cycle day {e.Clock.CycleDayIndex}");
                if (victory)
                    foreach (var kv in before)
                    {
                        var c = e.NpcRelations.ContractOf(kv.Key);
                        if (kv.Value.Kind == ContractKind.TheoDot && c != null && c.InstallmentsLeft == kv.Value.InstallmentsLeft && !countedMajor)
                            V("P1", "NPC contract", "theodot-not-charged-minor", $"{kv.Key} per-wave contract not charged/decremented after a won minor wave via GameEngine.ExecuteLiveCombatWave (InstallmentsLeft stays {c.InstallmentsLeft})");
                    }
            }
            else
            {
                if (victory) MajorWins++;
                else
                {
                    MajorLosses++;
                    CoreLossDetail = $"threat {threat}+{pending} vs STR×8 {strPart} + towers ({e.Defense.Buildings.Count} built)";
                    // The Unity spawner ends a wave only when every monster is dead, so a real player who keeps fighting closes the
                    // cycle. Mirror EnemyWaveSpawner.OnWaveCompleted (Controllers/EnemyWaveSpawner.cs:1158-1185) with Core calls.
                    RuntimeMirrorWaveEnd(major: true, boss, threat);
                    RuntimeMirrorWins++;
                }
                if (victory && e.Clock.CycleNumber == cycle0) V("P1", "Waves", "major-win-no-cycle", $"major win (threat {threat}+{pending}) did not close cycle {cycle0}");
            }
            if (countedMajor)
            {
                foreach (var t in tracks.Values) { t.MajorWins++; }
            }
            foreach (var t in tracks.Values) t.AllWaves++;
            if (e.Clock.CycleNumber != cycle0) { CloseCycleRow(); StartCycleRow(); }
            if (e.Clock.IsDuringCombatWave) V("P0", "Waves", "clock-stuck-in-wave", "IsDuringCombatWave still true after ExecuteLiveCombatWave");
            Combat(major);
            AfterContractsChanged();
        }

        public int RuntimeMirrorWins;
        public string CoreLossDetail = "";
        void RuntimeMirrorWaveEnd(bool major, bool boss, int threat)
        {
            action = "RuntimeMirrorWaveEnd (EnemyWaveSpawner.OnWaveCompleted via Core)";
            int idx = ++waveIndex;
            var pay0 = NpcLedgerTotals();
            WaveDebrief.Begin(e, idx, threat, boss);
            e.Clock.IsDuringCombatWave = false;
            NpcContracts.OnWaveEnded(e, major);
            if (major)
            {
                e.MajorWavesSurvived += 1;
                if (boss) e.DeepMechanics.CycleBossesDefeatedCount += 1;
                SpecialtyPaperActions.OnMajorWaveWon(e, boss, () => rng.NextDouble());
                e.Clock.CompleteMajorWave();
            }
            else e.MinorWavesDefeated += 1;
            int kills = Math.Max(3, threat / 20);
            WaveDebrief.Close(e, idx, true, kills, kills * 8);
            e.Farm.AddFarmExp(50);
            var pay1 = NpcLedgerTotals();
            foreach (var kv in pay1) if (tracks.TryGetValue(kv.Key, out var t)) t.Paid += kv.Value - (pay0.TryGetValue(kv.Key, out var p0) ? p0 : 0);
            foreach (var t in tracks.Values) { if (major) t.MajorWins++; t.AllWaves++; }
        }

        Dictionary<NpcSpecialistId, long> NpcLedgerTotals()
        {
            var d = new Dictionary<NpcSpecialistId, long>();
            foreach (var t in e.Ledger.Transactions)
            {
                if (!t.IsExpense || t.Reason != LedgerReason.NpcContract) continue;
                foreach (var p in NpcContracts.Profiles)
                    if (t.SourceId.Contains("_" + p.Id + "_")) { d.TryGetValue(p.Id, out var v); d[p.Id] = v + t.AmountGold; break; }
            }
            return d;
        }

        void AfterContractsChanged()
        {
            foreach (var id in tracks.Keys.ToList())
            {
                var t = tracks[id];
                var c = e.NpcRelations.ContractOf(id);
                if (c != null)
                {
                    // contracts must end after their terms
                    if (t.Kind == ContractKind.ChuKyNho && t.MajorWins > c.Terms && !t.Flagged) { t.Flagged = true; V("P1", "NPC contract", "chukynho-overrun", $"{id} survived {t.MajorWins} major waves on a {c.Terms}-cycle contract"); }
                    if (t.Kind == ContractKind.TheoDot && t.AllWaves > c.Terms && !t.Flagged) { t.Flagged = true; V("P1", "NPC contract", "theodot-overrun", $"{id} {c.Terms}-wave contract still active after {t.AllWaves} waves ({t.MajorWins} counted majors) — GameEngine.ExecuteLiveCombatWave only settles contracts on won major waves"); }
                    continue;
                }
                // ended: completed or defaulted
                var rel = e.NpcRelations.Get(id);
                if (t.Paid < t.TotalPrice)
                {
                    Defaults++;
                    // a default is fine (rule: unpaid instalment ends the contract); record how much was skipped
                }
                else
                {
                    Completions++;
                    if (t.Paid > t.TotalPrice) V("P2", "NPC contract", "overpaid", $"{id} {t.Kind}: paid {t.Paid} for a contract totalling {t.TotalPrice} (+{t.Paid - t.TotalPrice}, ceil-rounded instalments)");
                }
                tracks.Remove(id);
            }
        }

        void Combat(bool major)
        {
            var c = e.Character;
            int hits = major ? 8 : 4;
            var types = new[] { EnemyTypeId.KeBatHeo, EnemyTypeId.KeCaoPha, EnemyTypeId.DauLinhTrom, EnemyTypeId.AlienElite, EnemyTypeId.ChoSuong, EnemyTypeId.ThucTheSuong, EnemyTypeId.BossAlienWarlord, EnemyTypeId.BayNhay };
            double now = 0; double specialReady = 0;
            // equip a learned skill if any
            for (int i = 0; i < hits; i++)
            {
                now += 1.2;
                c.Now = now;
                var type = types[rng.Next(types.Length)];
                var move = MonsterMovesets.Pick(type, now, ref specialReady, rng.NextDouble());
                float raw = (9f + 1.2f * e.Clock.CycleNumber) * move.DamageMultiplier;
                bool parry = rng.NextDouble() < 0.4, stance = !parry && rng.NextDouble() < 0.2;
                float g0 = c.CurrentGuard, s0 = c.Stamina.CurrentStamina;
                action = $"ProcessIncomingAttack({type}/{move.Name}, raw {F.G(raw)}, drain {F.G(move.StaminaDrainRatio)}, parry {parry})";
                var hit = c.ProcessIncomingAttack(raw, parry, stance, move.StaminaDrainRatio);
                if (F.Bad(c.CurrentGuard) || F.Bad(c.Stamina.CurrentStamina)) V("P0", "Guard", "nan", $"guard {c.CurrentGuard}, stamina {c.Stamina.CurrentStamina}");
                if (c.CurrentGuard < 0 || c.CurrentGuard > c.MaxGuard + 0.01f) V("P1", "Guard", "guard-range", $"guard {c.CurrentGuard} / {c.MaxGuard}");
                if (c.Stamina.CurrentStamina < 0 || c.Stamina.CurrentStamina > c.GetEffectiveMaxStamina() + 0.05f) V("P1", "Guard", "stamina-above-tank", $"stamina {F.G(c.Stamina.CurrentStamina)} > effective tank {F.G(c.GetEffectiveMaxStamina())} (guard {F.G(c.CurrentGuard)}/{F.G(c.MaxGuard)}, low={c.IsGuardLow})");
                if (c.IsGuardLow != c.LowGuardStaminaScaled && !hit.CausedFaint) V("P1", "Guard", "lowguard-flag-desync", $"IsGuardLow {c.IsGuardLow} but LowGuardStaminaScaled {c.LowGuardStaminaScaled} after a hit");
                if (move.StaminaDrainRatio <= 0f && !parry && Math.Abs(c.Stamina.CurrentStamina - s0) > 0.01f && c.IsGuardLow == (g0 < c.MaxGuard * 0.3f))
                    V("P2", "Guard", "plain-hit-drains-stamina", $"basic move without drain changed stamina {F.G(s0)}→{F.G(c.Stamina.CurrentStamina)}");
                if (hit.CausedFaint)
                {
                    Faints++;
                    int day0 = e.Clock.CurrentDay;
                    action = "ExecutePlayerFaintRescue";
                    e.ExecutePlayerFaintRescue(hit.FaintReason);
                    if (e.Clock.CurrentDay != day0 && e.LastDailyProcessedFarmDay != e.Clock.CurrentDay - 1)
                        V("P1", "Guard", "rescue-skips-day", $"faint rescue at {(e.Clock.CurrentHour + 22) % 24:00}:xx moved the clock across 06:00 with Clock.TickMinutes(120): day {day0}→{e.Clock.CurrentDay} but LastDailyProcessedFarmDay={e.LastDailyProcessedFarmDay} (daily reset skipped)");
                    break;
                }
                // in-fight regen between hits; low guard must regen ×0.6
                float st0 = c.Stamina.CurrentStamina; bool low = c.IsGuardLow; float cap = c.GetEffectiveMaxStamina();
                action = "TickCombatRegen(0.5s, inCombat, 2s)";
                c.TickCombatRegen(0.5f, true, 2f);
                if (low && c.IsGuardLow && st0 + 0.5f < cap)
                {
                    float d = c.Stamina.CurrentStamina - st0;
                    if (Math.Abs(d - 2f * 0.6f * 0.5f) > 0.02f) V("P1", "Guard", "lowguard-regen", $"low-guard in-combat regen +{F.G(d)} over 0.5 s (expected {F.G(0.6f)})");
                }
                if (i % 3 == 2)
                {
                    foreach (var slot in MartialProgression.AllSlots)
                    {
                        if (MartialProgression.Slotted(c, slot) == null) continue;
                        action = $"MartialProgression.Cast({slot})";
                        var cr = MartialProgression.Cast(c, slot, now);
                        if (F.Bad(cr.DamageMultiplier)) V("P0", "Martial", "cast-nan", "damage multiplier NaN");
                        break;
                    }
                }
                if (c.Stamina.CurrentStamina < 20 && QuickBag.Count(c, QuickItemId.BinhHoiLuc) > 0) { action = "QuickBag.Use(BinhHoiLuc)"; QuickBag.Use(c, QuickItemId.BinhHoiLuc); }
            }
            // after the fight: out-of-combat regen to full
            for (int k = 0; k < 40 && !c.IsFainted; k++) c.TickCombatRegen(1f, false, 5f);
        }

        // ------------------------------------------------------------------ night
        void NightActions()
        {
            var listings = e.Economy.NightListings;
            if (listings.Count == 0) e.Economy.GenerateNightMarket(e.Farm.FarmLevel);
            listings = e.Economy.NightListings;
            foreach (var l in listings.ToList())
            {
                if (l.IsSoldOut) continue;
                bool feed = string.Equals(l.OfferedItem.Category, "Feed", StringComparison.OrdinalIgnoreCase) || (l.OfferedItem.Id ?? "").StartsWith("Cam", StringComparison.Ordinal);
                if (feed && l.AcceptsGold && e.Farm.FeedStockKg < 250 && e.Farm.Gold > l.GoldPrice + 500)
                {
                    float f0 = e.Farm.FeedStockKg;
                    action = $"TradeWithVillager(ChoDem,{l.SlotId},gold)";
                    var r = e.TradeWithVillager(VillagerId.ChoDem, l.SlotId, null, null, true);
                    if (r.success) { FeedBuys++; if (e.Farm.FeedStockKg <= f0) V("P2", "Feed", "feed-bought-not-stocked", $"bought {l.OfferedItem.Name} but FeedStockKg {f0}→{e.Farm.FeedStockKg}"); }
                    continue;
                }
                // pig barters (Nhà Buôn quest: night barters)
                var pigWant = l.Wants.FirstOrDefault(w => w.PigKind != WantedPigKind.None);
                if (pigWant == null || l.Wants.Count != 1) continue;
                var living = e.Pigs.Where(p => p.IsAlive && p.GestationDaysRemaining <= 0).ToList();
                List<Pig> offer = pigWant.PigKind switch
                {
                    WantedPigKind.AnyAdult => living.Where(p => p.Stage == PigStage.TruongThanh).OrderBy(p => p.WeightKg).Take(pigWant.Quantity).ToList(),
                    WantedPigKind.AdultGradeA => living.Where(p => p.Stage == PigStage.TruongThanh && (p.MeatQuality == MeatQuality.A || p.MeatQuality == MeatQuality.S)).Take(pigWant.Quantity).ToList(),
                    WantedPigKind.PigletCommon => living.Where(p => p.Stage == PigStage.HeoNon && p.Rarity == GeneRarity.Thuong).Take(pigWant.Quantity).ToList(),
                    WantedPigKind.IdentifiedKhaPlus => living.Where(p => p.IsIdentified && p.Rarity >= GeneRarity.Kha).Take(pigWant.Quantity).ToList(),
                    _ => new List<Pig>(),
                };
                if (offer.Count < pigWant.Quantity || living.Count - offer.Count < 10) continue;
                NightBarterTries++;
                int herd0 = e.Pigs.Count(p => p.IsAlive);
                action = $"TradeWithVillagerPigs(ChoDem,{l.SlotId},{offer.Count} pigs {pigWant.PigKind})";
                var res = e.TradeWithVillagerPigs(VillagerId.ChoDem, l.SlotId, null, offer.Select(p => p.Id).ToList(), false);
                if (res.success)
                {
                    NightBartersOk++;
                    if (e.Pigs.Count(p => p.IsAlive) != herd0 - offer.Count) V("P1", "Night market", "barter-herd-mismatch", $"herd {herd0}→{e.Pigs.Count(p => p.IsAlive)} after giving {offer.Count}");
                }
            }
        }

        // ------------------------------------------------------------------ invariants
        void CheckInstant(string when)
        {
            var c = e.Character;
            if (e.Farm.Gold < 0) V("P0", "Economy", "gold-negative", $"gold {e.Farm.Gold} ({when})");
            if (c.CurrentGuard < 0 || c.CurrentGuard > c.MaxGuard + 0.01f || F.Bad(c.CurrentGuard)) V("P1", "Guard", "guard-range", $"guard {c.CurrentGuard}/{c.MaxGuard} ({when})");
            if (c.Stamina.CurrentStamina < -0.01f || F.Bad(c.Stamina.CurrentStamina)) V("P1", "Stamina", "stamina-negative", $"{c.Stamina.CurrentStamina} ({when})");
            if (c.Stamina.CurrentStamina > c.GetEffectiveMaxStamina() + 0.05f)
                V("P1", "Stamina", "stamina-above-effective-max:" + when, $"stamina {F.G(c.Stamina.CurrentStamina)} > GetEffectiveMaxStamina {F.G(c.GetEffectiveMaxStamina())} (low guard {c.IsGuardLow}, load {F.G(c.Stamina.DailyLoad)}) after {action}");
        }

        void AfterDayRollover()
        {
            int day = e.Clock.CurrentDay;
            if (day <= lastProcessedCheckDay) return;
            lastProcessedCheckDay = day;
            var c = e.Character;
            string a = action;
            // daily processing ran exactly once for the finished day
            if (e.LastDailyProcessedFarmDay != day - 1) V("P1", "Clock", "daily-reset-missing", $"day {day} started but LastDailyProcessedFarmDay={e.LastDailyProcessedFarmDay} (last action: {a})");
            if (e.Farm.Gold < 0) V("P0", "Economy", "gold-negative", $"gold {e.Farm.Gold}");
            if (F.Bad(e.Farm.FeedStockKg) || e.Farm.FeedStockKg < 0) V("P0", "Feed", "feed-invalid", $"FeedStockKg {e.Farm.FeedStockKg}");
            if (F.Bad(c.CurrentGuard) || F.Bad(c.MaxGuard) || F.Bad(c.Stamina.CurrentStamina)) V("P0", "Guard", "nan", "guard/stamina NaN");
            if (c.CurrentGuard < 0 || c.CurrentGuard > c.MaxGuard + 0.01f) V("P1", "Guard", "guard-range-day", $"guard {c.CurrentGuard}/{c.MaxGuard}");
            if (c.IsGuardLow != c.LowGuardStaminaScaled) V("P2", "Guard", "lowguard-flag-after-day", $"after the daily reset guard={F.G(c.CurrentGuard)}/{F.G(c.MaxGuard)} (low={c.IsGuardLow}) but LowGuardStaminaScaled={c.LowGuardStaminaScaled}: OnDailyReset refills guard without SyncLowGuardStamina, so the stamina tank stays shrunk until the next hit/regen tick");
            if (c.Stamina.CurrentStamina > c.GetEffectiveMaxStamina() + 0.05f) V("P1", "Stamina", "stamina-above-max-day", $"stamina {F.G(c.Stamina.CurrentStamina)} > {F.G(c.GetEffectiveMaxStamina())}");
            // NPC affinity, caps, contracts
            int cap = NpcContracts.MaxNpcs(e.Farm.FarmLevel);
            if (e.NpcRelations.Contracts.Count > cap) V("P1", "NPC contract", "over-cap-day", $"{e.NpcRelations.Contracts.Count} > {cap}");
            foreach (var r in e.NpcRelations.Relations.Values)
            {
                if (F.Bad(r.Affinity) || r.Affinity < 0 || r.Affinity > 100) V("P0", "NPC affinity", "range", $"{r.Id} affinity {r.Affinity}");
                if (!r.Unlocked && r.Affinity != 0) V("P1", "NPC affinity", "locked-nonzero", $"{r.Id} affinity {r.Affinity} before quest step 1");
                if (r.Affinity > NpcContracts.AffinityCap(r) + 0.001f) V("P1", "NPC affinity", "cap", $"{r.Id} affinity {r.Affinity} > cap {NpcContracts.AffinityCap(r)} at quest stage {r.QuestStage}");
            }
            foreach (var ct in e.NpcRelations.Contracts)
            {
                var rec = e.PetsAndNpcs.HiredNpcs.FirstOrDefault(n => n.Id == ct.Id);
                if (rec == null) V("P1", "NPC contract", "contract-without-worker", $"{ct.Id} has a contract but is not in HiredNpcs");
                else if (rec.DailyWageGold != 0) V("P1", "NPC contract", "daily-wage-on-contract", $"{ct.Id} contracted but DailyWageGold={rec.DailyWageGold} (ExecuteDailyNpcShiftOnPigs will charge it)");
                if (ct.InstallmentsLeft < 0) V("P1", "NPC contract", "negative-instalments", $"{ct.Id} InstallmentsLeft {ct.InstallmentsLeft}");
                if (ct.Kind == ContractKind.ChuKyLon && day > ct.StartDay + ct.LargeCycleDays.Sum() + 1) V("P1", "NPC contract", "chukylon-overrun", $"{ct.Id} still running on day {day}, signed day {ct.StartDay} for {ct.LargeCycleDays.Sum()} days");
            }
            foreach (var n in e.PetsAndNpcs.HiredNpcs)
                if (e.NpcRelations.ContractOf(n.Id) == null) V("P1", "NPC contract", "worker-without-contract", $"{n.Id} in HiredNpcs without a contract");
            // NPC market prices
            foreach (var p in NpcContracts.Profiles)
            {
                float f = NpcContracts.MarketFactor(e, p);
                if (F.Bad(f) || f <= 0) V("P0", "NPC price", "factor", $"{p.Id} market factor {f}");
            }
            // herd and prices
            var living = e.Pigs.Where(p => p.IsAlive).ToList();
            foreach (var p in living)
            {
                if (F.Bad(p.WeightKg) || p.WeightKg <= 0) V("P0", "Herd", "weight-invalid", $"{p.Id} weight {p.WeightKg}");
                var (price, chance) = e.Economy.CalculatePigSalePrice(p, e.Farm.FarmLevel);
                if (price <= 0 && p.Rarity != GeneRarity.DiBien) V("P1", "Market", "price-nonpositive", $"{p.Id} price {price}");
                if (!p.FedToday) UnfedPigDays++;
            }
            if (living.Count > 250) V("P1", "Herd", "explosion", $"{living.Count} living pigs");
            if (living.Count == 0) V("P1", "Herd", "vanished", "no living pigs left");
            if (F.Bad(e.Economy.DayMarket.ThemePriceMultiplier) || e.Economy.DayMarket.ThemePriceMultiplier <= 0) V("P0", "Market", "theme-mult", $"{e.Economy.DayMarket.ThemePriceMultiplier}");
            // NPC quest metrics (reachability)
            foreach (NpcMetric m in Enum.GetValues(typeof(NpcMetric)))
            {
                int v = NpcContracts.Metric(e, m);
                var (first, last) = MetricRange[m];
                MetricRange[m] = (first, Math.Max(last, v));
            }
            if (e.Clock.CycleDayIndex > e.Clock.CycleTotalDays + 3) V("P2", "Cycle", "stuck", $"cycle {e.Clock.CycleNumber} day index {e.Clock.CycleDayIndex}/{e.Clock.CycleTotalDays}: major wave keeps failing in GameEngine.ExecuteLiveCombatWave");
            AfterContractsChanged();
        }

        // ------------------------------------------------------------------ save / load
        void SaveRoundTrip()
        {
            RoundTrips++;
            action = "CreateSaveData → Serialize → TryDeserialize → RestoreFromSave";
            var data = e.CreateSaveData();
            string json = SaveSystem.Serialize(data);
            if (!SaveSystem.TryDeserialize(json, out var back, out var err)) { V("P0", "Save", "deserialize-failed", err); return; }
            var e2 = new GameEngine(seed);
            e2.RestoreFromSave(back);
            string json2 = SaveSystem.Serialize(e2.CreateSaveData());
            if (json2 != json)
            {
                var diff = JsonDiff(json, json2);
                V("P1", "Save", "roundtrip-json-drift:" + diff.Split(' ')[0], "save→load→save JSON differs: " + diff);
            }
            void Cmp(string name, object? a, object? b, string sev = "P1")
            {
                if (!Equals(a, b)) V(sev, "Save", "lost:" + name, $"{name}: before {a} / after load {b}");
            }
            Cmp("Gold", e.Farm.Gold, e2.Farm.Gold);
            Cmp("Day", e.Clock.CurrentDay, e2.Clock.CurrentDay);
            Cmp("Cycle", e.Clock.CycleNumber, e2.Clock.CycleNumber);
            Cmp("LivingPigs", e.Pigs.Count(p => p.IsAlive), e2.Pigs.Count(p => p.IsAlive));
            Cmp("Contracts", e.NpcRelations.Contracts.Count, e2.NpcRelations.Contracts.Count);
            Cmp("IntelLevel", e.Intel.Level, e2.Intel.Level);
            Cmp("Guard", e.Character.CurrentGuard, e2.Character.CurrentGuard);
            Cmp("MaxGuard", e.Character.MaxGuard, e2.Character.MaxGuard);
            Cmp("Stamina", e.Character.Stamina.CurrentStamina, e2.Character.Stamina.CurrentStamina);
            Cmp("Rage", e.Character.Rage, e2.Character.Rage, "P2");
            Cmp("HiredNpcs", e.PetsAndNpcs.HiredNpcs.Count, e2.PetsAndNpcs.HiredNpcs.Count);
            Cmp("Buildings", e.Defense.Buildings.Count, e2.Defense.Buildings.Count);
            Cmp("ConstructionOrders", e.Defense.ConstructionOrders.Count, e2.Defense.ConstructionOrders.Count);
            Cmp("BaseSeed", e.BaseSeed, e2.BaseSeed, "P2");
            // runtime state that is not in GameSaveData
            Cmp("Defense.PendingThreatBudgetBonus", e.Defense.PendingThreatBudgetBonus, e2.Defense.PendingThreatBudgetBonus);
            Cmp("Farm.CleaningsToday", e.Farm.CleaningsToday, e2.Farm.CleaningsToday);
            Cmp("DayMarket.CurrentPigsSoldToday", e.Economy.DayMarket.CurrentPigsSoldToday, e2.Economy.DayMarket.CurrentPigsSoldToday);
            Cmp("DayMarket.UnquarantinedSickPenaltyDaysRemaining", e.Economy.DayMarket.UnquarantinedSickPenaltyDaysRemaining, e2.Economy.DayMarket.UnquarantinedSickPenaltyDaysRemaining);
            Cmp("DayMarket.BreedSoldTodayCounts", string.Join(",", e.Economy.DayMarket.BreedSoldTodayCounts.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value)), string.Join(",", e2.Economy.DayMarket.BreedSoldTodayCounts.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value)));
            foreach (var p in e.Pigs)
            {
                var q = e2.Pigs.FirstOrDefault(x => x.Id == p.Id);
                if (q == null) { V("P1", "Save", "pig-missing", p.Id); continue; }
                Cmp("Pig.AppraisalLockHoursRemaining", p.AppraisalLockHoursRemaining, q.AppraisalLockHoursRemaining);
                Cmp("Pig.AppraisalFailureCount", p.AppraisalFailureCount, q.AppraisalFailureCount);
                Cmp("Pig.FeedConversionMultiplier", p.FeedConversionMultiplier, q.FeedConversionMultiplier);
                Cmp("Pig.InfectionRiskMultiplier", p.InfectionRiskMultiplier, q.InfectionRiskMultiplier);
                Cmp("Pig.LatentDiseaseRiskMultiplier", p.LatentDiseaseRiskMultiplier, q.LatentDiseaseRiskMultiplier);
                Cmp("Pig.MaxHealth", p.MaxHealth, q.MaxHealth, "P2");
                Cmp("Pig.WeightKg", p.WeightKg, q.WeightKg);
                Cmp("Pig.GestationDaysRemaining", p.GestationDaysRemaining, q.GestationDaysRemaining);
            }
            foreach (var r in e.NpcRelations.Relations.Values)
            {
                var r2 = e2.NpcRelations.Get(r.Id);
                Cmp("Npc.Affinity", r.Affinity, r2.Affinity);
                Cmp("Npc.QuestStage", r.QuestStage, r2.QuestStage);
            }
            foreach (var n in e.PetsAndNpcs.HiredNpcs)
            {
                var n2 = e2.PetsAndNpcs.HiredNpcs.FirstOrDefault(x => x.Id == n.Id);
                if (n2 == null) continue;
                Cmp("HiredNpc.DailyWageGold", n.DailyWageGold, n2.DailyWageGold);
            }
            // the save shares mutable objects with the live engine (shallow list copies)
            if (data.HiredNpcs.Count > 0 && ReferenceEquals(data.HiredNpcs[0], e.PetsAndNpcs.HiredNpcs.FirstOrDefault()))
                V("P2", "Save", "shallow-copy:HiredNpcs", "CreateSaveData copies HiredNpcs/Buildings/Structures/Traits by reference; mutating the engine after CreateSaveData mutates the pending save (GameEngine.cs:1343,1466,1477)");
        }

        static string JsonDiff(string a, string b)
        {
            var la = a.Split('\n'); var lb = b.Split('\n');
            int n = Math.Min(la.Length, lb.Length);
            for (int i = 0; i < n; i++)
                if (la[i] != lb[i]) return $"{la[i].Trim()} → {lb[i].Trim()}";
            return $"length {la.Length} vs {lb.Length}";
        }
    }

    // =====================================================================================================
    // Targeted probes (each builds its own engine; where state is injected to reach a late-game rule it says so)
    // =====================================================================================================
    static class Probes
    {
        static void Out(string s) => Console.WriteLine("    " + s);

        static GameEngine NewGame(int seed, PlayerId who = PlayerId.An)
        {
            var e = new GameEngine(seed);
            NewGameSetup.ChooseProtagonist(e, who);
            StarterHerd.FillTo(e);
            return e;
        }

        static void TickToHour(GameEngine e, int hour)
        {
            for (int i = 0; i < 30 && e.Clock.CurrentHour != hour; i++) { e.Tick(60 - e.Clock.CurrentMinute); e.Clock.IsMinorWaveTriggered = false; }
        }

        public static void RunAll()
        {
            Console.WriteLine("\n=== PROBES ===");
            Run("A faint rescue across 06:00", FaintRescueAcrossMorning);
            Run("B free skill via ExecuteMartialSkillWithCombo", FreeSkillViaCombo);
            Run("C rune engraving free retries", RuneFreeRetry);
            Run("D paper salvage keeps enhancement", PaperSalvageKeepsLevel);
            Run("E low guard: eat/rest exceed the shrunk tank", LowGuardEatRest);
            Run("F contract default vs terminate", ContractDefaultVsTerminate);
            Run("G trust-skip forgives an instalment", TrustSkipForgives);
            Run("H minor wave promoted to major", MinorPromoted);
            Run("I save/load resets market saturation + sick penalty", SaveScumMarket);
            Run("J renewal pricing rules", RenewalRules);
            Run("K cleaning paths split", CleaningPaths);
            Run("L day market open at night", NightSale);
            Run("M per-wave mercenary never charged on minor waves", MercenaryMinorWaves);
            Run("N BaseSeed not saved", BaseSeedNotSaved);
            Run("O low guard flag after daily reset", LowGuardAfterDay);
            Run("P mercenary kill quests", KillQuests);
            Run("Q feed supply", FeedSupply);
            Run("R terminate-before-wave discount", TerminateBeforeWave);
            Run("S contract cap counts", ContractCap);
            Run("T appraisal lock save-scum", AppraisalLockSaveScum);
            Run("U feed sacks vs FeedStockKg double ledger", FeedSackDouble);
            Run("V lost major wave stalls the cycle (Core path)", CoreMajorStall);
        }

        static void Run(string name, Action a)
        {
            Console.WriteLine(" - " + name);
            try { a(); }
            catch (Exception ex) { Report.V("P0", "Probe crash", name, 0, 0, name, ex.GetType().Name + ": " + ex.Message + " " + (ex.StackTrace ?? "").Split('\n').FirstOrDefault()?.Trim()); Out("CRASH " + ex.Message); }
        }

        static void FaintRescueAcrossMorning()
        {
            var e = NewGame(101);
            TickToHour(e, 5);
            int day0 = e.Clock.CurrentDay;
            int age0 = e.Pigs.First(p => p.IsAlive).AgeDays;
            var hit = e.SimulatePlayerCombatEncounter(100000f, false);
            Out($"faint={hit.hit.CausedFaint} rescued={hit.rescuedToFarmhouse}; clock now day {e.Clock.CurrentDay} {e.Clock.CurrentHour:00}:{e.Clock.CurrentMinute:00}; LastDailyProcessedFarmDay={e.LastDailyProcessedFarmDay}; pig age {age0}→{e.Pigs.First(p => p.IsAlive).AgeDays}");
            int processedAfterRescue = e.LastDailyProcessedFarmDay;
            e.Tick(24 * 60);
            Out($"after +24h: day {e.Clock.CurrentDay}, LastDailyProcessedFarmDay={e.LastDailyProcessedFarmDay}, pig age {e.Pigs.First(p => p.IsAlive).AgeDays} (two 06:00 crossings, expected +2)");
            if (e.Clock.CurrentDay - 1 != processedAfterRescue && processedAfterRescue == day0 - 1)
                Report.V("P1", "Guard / clock", "rescue-skips-daily-reset", 101, day0, "SimulatePlayerCombatEncounter at 05:00 → ExecutePlayerFaintRescue",
                    $"rescue advanced the clock 05:00→07:00 with Clock.TickMinutes(120) (no GameEngine.Tick): day {day0}→{day0 + 1} without OnDailyReset; after another 24 h pigs aged {e.Pigs.First(p => p.IsAlive).AgeDays - age0} day(s) for two mornings. Also skips 2 hourly ticks, repairs and construction progress.");
        }

        static void FreeSkillViaCombo()
        {
            var e = NewGame(102);
            var s = MartialSkillCatalog.AllSkills.Where(x => !x.IsPassive).OrderByDescending(x => x.UnlockTier).First();
            int g0 = e.Farm.Gold, mp0 = e.Character.MartialPoints;
            var can = MartialProgression.CanLearn(e.Character, s);
            var r = e.ExecuteMartialSkillWithCombo(s.Id, 10.0);
            bool learned = e.Character.LearnedSkills.Contains(s.Id);
            Out($"{s.Id} tier {s.UnlockTier} ultimate={s.IsUltimate}: CanLearn={can.ok} ({can.reason}); after ExecuteMartialSkillWithCombo learned={learned}, executed={r.executed}, gold {g0}→{e.Farm.Gold}, martial points {mp0}→{e.Character.MartialPoints}");
            if (learned && !can.ok)
                Report.V("P1", "Martial", "combo-api-learns-free", 102, e.Clock.CurrentDay, $"GameEngine.ExecuteMartialSkillWithCombo(\"{s.Id}\")",
                    $"adds a tier-{s.UnlockTier} skill to LearnedSkills and slots it with no gold/points/stat/Bí Kíp check (CanLearn said: {can.reason}); cast executed={r.executed}");
        }

        static void RuneFreeRetry()
        {
            var e = NewGame(103);
            // state injection: rune system prerequisites (mid-game gate, 12 major waves, level 15, hall 2, Bàn Khắc)
            e.DeepMechanics.MidGameGateUnlocked = true; e.MajorWavesSurvived = 12; e.Character.Level = 15; e.Defense.HallLevel = 2;
            e.CoreAndVillage.HasHighTierEngravingTableBlueprint = true;
            int g0 = e.Farm.Gold; int attempts = 0, fails = 0;
            var runes = MartialProgression.RuneIds.Select(r => r.id).ToArray();
            var inv0 = e.Inventory.Slots.Sum(x => x.Stack);
            foreach (var id in runes)
            {
                for (int k = 0; k < 200; k++)
                {
                    attempts++;
                    var r = MartialProgression.EngraveRune(e, id, 3);
                    if (r.ok) break;
                    fails++;
                }
            }
            // repeat on throwaway characters until at least one failure is observed (rate 60%, roll = new Random())
            for (int k = 0; k < 40 && fails == 0; k++)
            {
                e.Character.EngravedRunes.Clear();
                attempts++;
                if (!MartialProgression.EngraveRune(e, runes[0], 3).ok) fails++;
            }
            if (e.Inventory.Slots.Sum(x => x.Stack) != inv0) Out("inventory changed by engraving");
            Out($"engraved {e.Character.EngravedRunes.Count} tier-III runes in {attempts} attempts ({fails} failures at 60%); gold {g0}→{e.Farm.Gold}; inventory unchanged");
            if (fails > 0 && e.Farm.Gold == g0)
                Report.V("P1", "Engraving", "rune-retry-free", 103, e.Clock.CurrentDay, "MartialProgression.EngraveRune(tier 3) ×N",
                    $"a failed tier-III engrave (60%) costs nothing — {fails} failures, 0 gold/material spent — so the success rate is meaningless; roll uses new Random() (non-deterministic, not save-safe)");
        }

        static void PaperSalvageKeepsLevel()
        {
            var e = NewGame(104);
            string paper = EnhancementTransactions.PaperId(SpecialtyPaperColor.Lam);
            e.Inventory.AddItem(paper, 2);
            e.DeepMechanics.EquippedPaperColor = SpecialtyPaperColor.Lam;
            e.DeepMechanics.intEquippedPaperEnhancementLevel = 2; // state injection = result of two paid enhancements on the selected paper
            var r = SpecialtyPaperActions.Salvage(e);
            Out($"salvage ok={r.ok}: papers left {e.Inventory.CountId(paper)}, equipped enhancement level {e.DeepMechanics.intEquippedPaperEnhancementLevel}, dust {e.DeepMechanics.EnhancementDustCount}");
            if (r.ok && e.DeepMechanics.intEquippedPaperEnhancementLevel > 0)
                Report.V("P1", "Specialty paper", "salvage-keeps-enhancement", 104, e.Clock.CurrentDay, "SpecialtyPaperActions.Salvage after enhancing",
                    $"salvaging the enhanced paper refunds dust but leaves intEquippedPaperEnhancementLevel={e.DeepMechanics.intEquippedPaperEnhancementLevel}; the next paper of that colour inherits +2 for free (Use() resets it, Salvage() does not)");
        }

        static void LowGuardEatRest()
        {
            var e = NewGame(105);
            var c = e.Character;
            int n = 0;
            while (!c.IsGuardLow && n++ < 50) c.ProcessIncomingAttack(20f, false);
            float tank = c.GetEffectiveMaxStamina();
            Out($"guard {F.G(c.CurrentGuard)}/{F.G(c.MaxGuard)} low={c.IsGuardLow} scaled={c.LowGuardStaminaScaled}; stamina {F.G(c.Stamina.CurrentStamina)} / tank {F.G(tank)} (raw max {F.G(c.Stamina.EffectiveMaxStamina)})");
            // regen check ×0.6
            float s0 = c.Stamina.CurrentStamina;
            c.Stamina.CurrentStamina = Math.Min(s0, tank - 20f);
            float before = c.Stamina.CurrentStamina;
            c.TickCombatRegen(1f, false, 5f);
            Out($"out-of-combat regen while low: +{F.G(c.Stamina.CurrentStamina - before)} in 1 s (expected 6 = 10×0.6)");
            if (Math.Abs(c.Stamina.CurrentStamina - before - 6f) > 0.05f && c.IsGuardLow)
                Report.V("P1", "Guard", "lowguard-regen-ooc", 105, 1, "TickCombatRegen(1s, out of combat)", $"+{c.Stamina.CurrentStamina - before} (expected 6)");
            if (!c.IsGuardLow) { Out("guard healed during regen; skip eat test"); }
            // drive guard low again then eat
            while (!c.IsGuardLow) c.ProcessIncomingAttack(20f, false);
            c.Stamina.CurrentStamina = c.GetEffectiveMaxStamina();
            var meal = PlayerRestCommands.EatMeal(e, true);
            Out($"EatMeal(good) while guard low: stamina {F.G(c.Stamina.CurrentStamina)} vs tank {F.G(c.GetEffectiveMaxStamina())}");
            if (c.Stamina.CurrentStamina > c.GetEffectiveMaxStamina() + 0.05f)
                Report.V("P1", "Guard", "eat-rest-ignore-lowguard-tank", 105, 1, "PlayerRestCommands.EatMeal / Rest while Chống Chịu < 30%",
                    $"stamina {F.G(c.Stamina.CurrentStamina)} > low-guard tank {F.G(c.GetEffectiveMaxStamina())}: StaminaSystem.RecoverLoadAndStamina caps at Stamina.EffectiveMaxStamina, not CharacterData.GetEffectiveMaxStamina (×0.75); QuickBag/TickCombatRegen respect it");
            // TickCombatRegen keeps the over-cap value
            float over = c.Stamina.CurrentStamina;
            c.TickCombatRegen(1f, true, 2f);
            if (c.Stamina.CurrentStamina > c.GetEffectiveMaxStamina() + 0.05f) Out($"regen tick leaves it above the tank ({F.G(c.Stamina.CurrentStamina)})");
        }

        static GameEngine HireNongCong(int seed, int terms)
        {
            var e = NewGame(seed);
            for (int i = 0; i < 10; i++) e.DeepMechanics.SpecialtyLocks.RecordManualCleaning(); // = 10 real CleanPlot labours
            NpcContracts.OnDay(e);
            var s = NpcContracts.Sign(e, NpcSpecialistId.NongCong, ContractKind.ChuKyNho, terms);
            if (!s.ok) throw new InvalidOperationException("sign failed: " + s.message);
            return e;
        }

        static (bool victory, int cycle) WinMajor(GameEngine e)
        {
            e.Character.BaseStats.Str = 40; // state injection: enough STR that the abstract wave resolves as a win
            e.Character.RecalculateBonusStats();
            var r = e.ExecuteLiveCombatWave(1, 150, false, false);
            return (r.report.Victory, e.Clock.CycleNumber);
        }

        static void ContractDefaultVsTerminate()
        {
            // A: default (no gold at the major wave)
            var a = HireNongCong(106, 3);
            a.NpcRelations.Get(NpcSpecialistId.NongCong).Affinity = 30f; // state injection: favours done (cap 49 before step 2)
            var ca = a.NpcRelations.ContractOf(NpcSpecialistId.NongCong)!;
            float affA0 = a.NpcRelations.Get(NpcSpecialistId.NongCong).Affinity;
            int paidA = ca.Deposit;
            a.Farm.Gold = 0; // the player spent everything before the wave
            var wa = WinMajor(a);
            bool goneA = a.NpcRelations.ContractOf(NpcSpecialistId.NongCong) == null;
            float affA1 = a.NpcRelations.Get(NpcSpecialistId.NongCong).Affinity;
            Out($"default: total {ca.TotalPrice}, paid {paidA} (deposit), contract ended={goneA}, affinity {affA0}→{affA1}, gold after wave {a.Farm.Gold}");
            // B: terminate
            var b = HireNongCong(106, 3);
            b.NpcRelations.Get(NpcSpecialistId.NongCong).Affinity = 30f;
            float affB0 = b.NpcRelations.Get(NpcSpecialistId.NongCong).Affinity;
            NpcContracts.Terminate(b, NpcSpecialistId.NongCong);
            float affB1 = b.NpcRelations.Get(NpcSpecialistId.NongCong).Affinity;
            Out($"terminate: affinity {affB0}→{affB1}");
            if (goneA && (affA0 - affA1) < (affB0 - affB1))
                Report.V("P2", "NPC contract", "default-cheaper-than-terminate", 106, 1, "Sign ChuKyNho×3 → empty gold → won major wave",
                    $"defaulting on an instalment ends the contract for −{affA0 - affA1} affinity, terminating costs −{affB0 - affB1}; both forfeit only the deposit ({paidA}G of {ca.TotalPrice}G). Running out of gold is the cheaper exit. Also the wave reward is credited after NpcContracts.OnWaveEnded, so it can't pay that wave's instalment (GameEngine.cs:1196 vs 1209).");
        }

        static void TrustSkipForgives()
        {
            var e = HireNongCong(107, 2);
            var r = e.NpcRelations.Get(NpcSpecialistId.NongCong);
            r.QuestStage = 3; r.Affinity = 100f; // state injection: Tri kỷ
            var c = e.NpcRelations.ContractOf(NpcSpecialistId.NongCong)!;
            int left0 = c.InstallmentsLeft, total = c.TotalPrice, dep = c.Deposit, inst = c.InstallmentAmount;
            e.Farm.Gold = 0;
            WinMajor(e);
            var c1 = e.NpcRelations.ContractOf(NpcSpecialistId.NongCong);
            Out($"after a skipped payment: contract alive={c1 != null}, instalments {left0}→{c1?.InstallmentsLeft}, instalment {inst}→{c1?.InstallmentAmount}, gold {e.Farm.Gold}");
            if (c1 != null && c1.InstallmentsLeft < left0 && c1.InstallmentAmount == inst)
                Report.V("P2", "NPC contract", "trust-skip-forgives", 107, 1, "affinity 100 + no gold at instalment",
                    $"\"cho nợ lần này\" decrements InstallmentsLeft without carrying the {inst}G forward — the debt is forgiven, not deferred (NpcContracts.cs:490-495 then OnWaveEnded:540). TrustSkipUsed lives on the contract, so every re-signed contract gets another free instalment.");
        }

        static void MinorPromoted()
        {
            var e = NewGame(108);
            e.Character.BaseStats.Str = 40; e.Character.RecalculateBonusStats();
            // pending bonus from ordinary play: each sale adds max(1, gold/250); each death +15
            e.Defense.PendingThreatBudgetBonus = 100;
            int cyc = e.Clock.CycleNumber, day = e.Clock.CycleDayIndex;
            var r = e.ExecuteLiveCombatWave(1, 60, false, false);
            Out($"minor wave (threat 60 + pending 100): victory={r.report.Victory}, MajorWavesSurvived={e.MajorWavesSurvived}, MinorWavesDefeated={e.MinorWavesDefeated}, cycle {cyc}→{e.Clock.CycleNumber} on cycle day {day}");
            if (e.Clock.CycleNumber != cyc)
                Report.V("P1", "Waves", "minor-promoted-probe", 108, 1, "ExecuteLiveCombatWave(threat 60) with PendingThreatBudgetBonus 100",
                    $"GameEngine.cs:1187 classifies a wave as major by effectiveThreat ≥ 150 (threat + PendingThreatBudgetBonus), so a minor wave after a few sales/deaths closes the cycle on day {day} of {e.Clock.CycleTotalDays}, pays ChuKyNho instalments and rolls a paper drop");
        }

        static void SaveScumMarket()
        {
            var e = NewGame(109);
            var adults = e.Pigs.Where(p => p.IsAlive && p.Stage == PigStage.TruongThanh).ToList();
            foreach (var p in adults.Take(6)) e.SellPigAtDayMarket(p.Id, false);
            var probe = e.Pigs.First(p => p.IsAlive);
            // a sick pig sold without quarantine is detected by the buyer → −10% day-market buying power for 3 days (GDD 4.5)
            var sick = e.Pigs.First(p => p.IsAlive && p.Id != probe.Id && p.Stage == PigStage.TruongThanh);
            sick.ActiveDisease = DiseaseId.NamPhatQuang;
            e.SellPigAtDayMarket(sick.Id, false, 0.0);
            int priceBefore = e.Economy.CalculatePigSalePrice(probe, e.Farm.FarmLevel).priceGold;
            int sold = e.Economy.DayMarket.CurrentPigsSoldToday;
            int pen = e.Economy.DayMarket.UnquarantinedSickPenaltyDaysRemaining;
            var json = SaveSystem.Serialize(e.CreateSaveData());
            SaveSystem.TryDeserialize(json, out var d, out _);
            var e2 = new GameEngine(109); e2.RestoreFromSave(d);
            var probe2 = e2.Pigs.First(p => p.Id == probe.Id);
            int priceAfter = e2.Economy.CalculatePigSalePrice(probe2, e2.Farm.FarmLevel).priceGold;
            Out($"sold today {sold}→{e2.Economy.DayMarket.CurrentPigsSoldToday}; sick penalty days {pen}→{e2.Economy.DayMarket.UnquarantinedSickPenaltyDaysRemaining}; breed counts {e.Economy.DayMarket.BreedSoldTodayCounts.Count}→{e2.Economy.DayMarket.BreedSoldTodayCounts.Count}; probe price {priceBefore} (penalised) vs {priceAfter} after load");
            if (e2.Economy.DayMarket.UnquarantinedSickPenaltyDaysRemaining == 0)
                Report.V("P1", "Save", "savescum-market-penalties", 109, 1, "sell → save → load",
                    $"DayMarket.UnquarantinedSickPenaltyDaysRemaining, CurrentPigsSoldToday and BreedSoldTodayCounts are not in GameSaveData/EconomyIntelSnapshot (GameEngineEconomyIntel.cs:52-62): a save/load wipes the −10% sick-sale penalty and same-day saturation (probe price {priceBefore}→{priceAfter})");
        }

        static void RenewalRules()
        {
            var e = NewGame(110);
            var id = NpcSpecialistId.BacSiThuY;
            for (int i = 0; i < 5; i++) e.DeepMechanics.SpecialtyLocks.RecordQuarantinedDayMarketSale();
            NpcContracts.OnDay(e);
            var rel = e.NpcRelations.Get(id);
            rel.Affinity = 30f; // state injection: favours done
            e.Farm.Gold = 50000;
            var s = NpcContracts.Sign(e, id, ContractKind.ChuKyNho, 1);
            var c = e.NpcRelations.ContractOf(id)!;
            int unit0 = c.UnitPrice;
            // market up (Hoảng Loạn Dịch: vet +25–40%), affinity below 50 → market price
            e.Economy.DayMarket.Theme = MarketTheme.HoangLoanDich;
            int market = NpcContracts.MarketUnitPrice(e, NpcContracts.Profile(id)!, ContractKind.ChuKyNho);
            var r1 = NpcContracts.Renew(e, id, 1);
            Out($"signed at {unit0}; market up to {market}, affinity {rel.Affinity}: renew → unit {c.UnitPrice} ({r1.message})");
            // affinity ≥ 50 (needs quest step 2) keeps the old price
            rel.QuestStage = 2; rel.Affinity = 60f;
            e.Economy.DayMarket.Theme = MarketTheme.CanBang;
            int lower = NpcContracts.MarketUnitPrice(e, NpcContracts.Profile(id)!, ContractKind.ChuKyNho);
            int unitBeforeLock = c.UnitPrice;
            var r2 = NpcContracts.Renew(e, id, 1, forceDown: false);
            Out($"market back down to {lower} (unit {unitBeforeLock}), no force: unit {c.UnitPrice}");
            float aff = rel.Affinity;
            var r3 = NpcContracts.Renew(e, id, 1, forceDown: true);
            Out($"force down: unit {c.UnitPrice}, affinity {aff}→{rel.Affinity}");
            long owed = c.Deposit + (long)c.InstallmentsLeft * c.InstallmentAmount;
            Out($"contract total {c.TotalPrice}, deposits {c.Deposit}, {c.InstallmentsLeft}×{c.InstallmentAmount} left → {owed}");
            if (c.UnitPrice != lower || Math.Abs(aff - rel.Affinity - 5f) > 0.01f) Report.V("P1", "NPC renewal", "probe-rules", 110, 1, "Renew", $"force-down rule broken: unit {c.UnitPrice} (market {lower}), affinity {aff}→{rel.Affinity}");
            // Exploit check: renewing at a locked low price then force-down when theme drops — the lock never expires
            e.Economy.DayMarket.Theme = MarketTheme.HoangLoanDich;
            int high = NpcContracts.MarketUnitPrice(e, NpcContracts.Profile(id)!, ContractKind.ChuKyNho);
            var r4 = NpcContracts.Renew(e, id, 3);
            Out($"market up again to {high}, affinity {rel.Affinity} ≥ 50: renew 3 cycles at {c.UnitPrice} (locked)");
            if (c.UnitPrice < high && rel.Affinity >= 50f)
                Report.Note($"Renewal ratchet: force-down to {lower} (−5 aff) then lock that low price when the market rises again (aff ≥ 50) — the vet stays at {c.UnitPrice}G vs market {high}G indefinitely; working as designed but makes price-lock + force-down a one-way ratchet.");
        }

        static void CleaningPaths()
        {
            var e = NewGame(111);
            int hab0 = e.Farm.HabitatIndex;
            for (int day = 0; day < 4; day++)
            {
                for (int i = 0; i < 3; i++) HerdLifecycleRules.TryCleanYard(e);
                e.Tick(24 * 60); e.Clock.IsMinorWaveTriggered = false;
                e.Character.Stamina.PerformSleepReset();
            }
            int cleanYard = e.DeepMechanics.SpecialtyLocks.TotalManualCleanings;
            Out($"12× TryCleanYard over 4 days: TotalManualCleanings={cleanYard} (Nông Công quest needs 10), habitat {hab0}→{e.Farm.HabitatIndex}");
            var e2 = NewGame(111);
            int c0 = e2.Farm.CleaningsToday;
            for (int i = 0; i < 3; i++) FarmLaborStamina.TrySpend(e2, FarmLaborTask.CleanPlot);
            Out($"3× FarmLaborStamina CleanPlot: TotalManualCleanings={e2.DeepMechanics.SpecialtyLocks.TotalManualCleanings}, Farm.CleaningsToday {c0}→{e2.Farm.CleaningsToday}");
            if (cleanYard == 0)
                Report.V("P1", "NPC quests", "cleaning-paths-split", 111, 1, "HerdLifecycleRules.TryCleanYard ×12",
                    "the Farm Overview \"dọn bãi\" (HerdLifecycleRules.cs:376) raises Farm.CleaningsToday (habitat) but never calls SpecialtyLocks.RecordManualCleaning, while FarmLaborStamina CleanPlot (corpse-lot sanitation, FarmLaborStamina.cs:139) records the quest/lock but not CleaningsToday. Nông Công quest + Khoa lock 2 only move via the corpse lot; habitat only via the popup.");
        }

        static void NightSale()
        {
            var e = NewGame(112);
            TickToHour(e, 23);
            var p = e.Pigs.First(x => x.IsAlive && x.Stage == PigStage.TruongThanh);
            var r = e.SellPigAtDayMarket(p.Id, true);
            Out($"23:00 SellPigAtDayMarket → success={r.success} gold {r.goldEarned}");
            if (r.success) Report.V("P2", "Market", "day-market-at-night", 112, e.Clock.CurrentDay, "SellPigAtDayMarket at 23:00",
                "Core does not check the day-market hours (06:00–18:00) that RecruitmentRules.IsOpen and TradeWithVillager enforce; relies on the UI");
        }

        static void MercenaryMinorWaves()
        {
            var e = NewGame(113);
            e.MajorWavesSurvived = 2; NpcContracts.OnDay(e); // Vệ Binh Khiên quest: survive 2 major waves (state injection)
            var s = NpcContracts.Sign(e, NpcSpecialistId.VeBinhKhien, ContractKind.TheoDot, 3);
            var c = e.NpcRelations.ContractOf(NpcSpecialistId.VeBinhKhien)!;
            e.Character.BaseStats.Str = 30; e.Character.RecalculateBonusStats();
            int g0 = e.Farm.Gold;
            for (int i = 0; i < 4; i++) e.ExecuteLiveCombatWave(i + 1, 50, false, false);
            Out($"signed={s.ok}; 4 won minor waves via ExecuteLiveCombatWave: InstallmentsLeft {3}→{c.InstallmentsLeft}, minor wins {e.MinorWavesDefeated}, NPC gold paid {g0 + 4 * 120 - e.Farm.Gold}");
            if (c.InstallmentsLeft == 3 && e.MinorWavesDefeated > 0)
                Report.V("P1", "NPC contract", "theodot-free-on-minor", 113, 1, "Sign VeBinhKhien TheoDot×3 → 4 minor waves via GameEngine.ExecuteLiveCombatWave",
                    "GameEngine.ExecuteLiveCombatWave calls NpcContracts.OnWaveEnded only inside the won-major branch (GameEngine.cs:1196); minor waves and lost majors neither charge nor count down per-wave contracts, and mercenaries get no +1 affinity. The Unity spawner (EnemyWaveSpawner.cs:1162) does it for every wave, so the two wave paths disagree.");
        }

        static void BaseSeedNotSaved()
        {
            var e = new GameEngine(424242); StarterHerd.FillTo(e);
            var p = NpcContracts.Profile(NpcSpecialistId.NongCong)!;
            int price0 = NpcContracts.CyclePrice(e, p);
            var json = SaveSystem.Serialize(e.CreateSaveData());
            SaveSystem.TryDeserialize(json, out var d, out _);
            var e2 = new GameEngine(); e2.RestoreFromSave(d);
            Out($"BaseSeed {e.BaseSeed} → after load into new GameEngine() {e2.BaseSeed}; Nông Công cycle price {price0} → {NpcContracts.CyclePrice(e2, p)}");
            if (e.BaseSeed != e2.BaseSeed)
                Report.V("P2", "Save", "baseseed-not-saved", 424242, 1, "CreateSaveData/RestoreFromSave",
                    "BaseSeed is not in GameSaveData and the runtime builds the engine with new GameEngine() (MobileGameController.cs:42 → seed 20260601). Every new game shares one seed (weather, cycle lengths, NPC price factors, Core paper drops) and every load restarts the engine/clock RNG from the start of that seed, so post-load rolls (conception, appraisal, outbreak) are replayable by save-scumming.");
        }

        static void LowGuardAfterDay()
        {
            var e = NewGame(114);
            var c = e.Character;
            while (!c.IsGuardLow) c.ProcessIncomingAttack(20f, false);
            float tankLow = c.GetEffectiveMaxStamina();
            c.Stamina.CurrentStamina = tankLow;
            e.Tick(e.Clock.MinutesUntilNextMorning()); e.Clock.IsMinorWaveTriggered = false;
            Out($"after 06:00: guard {F.G(c.CurrentGuard)}/{F.G(c.MaxGuard)} low={c.IsGuardLow}, LowGuardStaminaScaled={c.LowGuardStaminaScaled}, stamina {F.G(c.Stamina.CurrentStamina)}");
            var data = e.CreateSaveData();
            var e2 = new GameEngine(114); e2.RestoreFromSave(data);
            float s1 = e2.Character.Stamina.CurrentStamina;
            c.TickCombatRegen(0.01f, false, 5f);
            Out($"live engine after first regen tick: stamina {F.G(c.Stamina.CurrentStamina)} (unscaled ×1/0.75); loaded copy keeps {F.G(s1)} with LowGuardStaminaScaled={e2.Character.LowGuardStaminaScaled}");
            if (e2.Character.LowGuardStaminaScaled == false && c.Stamina.CurrentStamina > s1 + 1f)
                Report.V("P2", "Guard", "lowguard-scale-lost-on-load", 114, 2, "low guard → 06:00 reset → save → load",
                    $"OnDailyReset (GameEngine.cs:657) refills guard but not the stamina scaling flag; a save taken then restores LowGuardStaminaScaled=IsGuardLow=false (GameEngine.cs:1667/1712), so the 25% shrink is never undone: live engine gets back to {F.G(c.Stamina.CurrentStamina)}, loaded game stays at {F.G(s1)}");
        }

        static void KillQuests()
        {
            var e = NewGame(115);
            e.Character.BaseStats.Str = 40; e.Character.RecalculateBonusStats();
            for (int i = 0; i < 6; i++) e.ExecuteLiveCombatWave(i + 1, 200, i % 2 == 0, false);
            int elite = NpcContracts.Metric(e, NpcMetric.EliteKills), ufo = NpcContracts.Metric(e, NpcMetric.UfoKills), thief = NpcContracts.Metric(e, NpcMetric.ThiefKills);
            Out($"6 won major/boss waves via Core: EliteKills={elite}, UfoKills={ufo}, ThiefKills={thief}");
            if (elite + ufo + thief == 0)
                Report.V("P1", "NPC quests", "kill-quests-runtime-only", 115, 1, "6 won major/boss waves via GameEngine.ExecuteLiveCombatWave",
                    "Xạ Thủ Nỏ (EliteKills), Thợ Săn UFO (UfoKills) and Thích Khách Đêm (ThiefKills) quests only move through NpcContracts.RecordKill, whose single caller is MercenaryAgentView.cs:212 (kills by an already-hired mercenary). The player's own kills and the Core wave path never count, so these three can only be unlocked after hiring Vệ Binh Khiên / Kỹ Sư Chiến Trường and letting them kill.");
        }

        static void FeedSupply()
        {
            var e = NewGame(116);
            float f0 = e.Farm.FeedStockKg;
            int days = 0;
            while (e.Farm.FeedStockKg > 1f && days < 60) { e.Tick(24 * 60); e.Clock.IsMinorWaveTriggered = false; days++; }
            int unfed = e.Pigs.Count(p => p.IsAlive && !p.FedToday);
            Out($"12-pig starter herd: feed {f0} kg lasts {days} days with no purchases; unfed today {unfed}/{e.Pigs.Count(p => p.IsAlive)}");
            Report.Note($"Feed: no Core API buys feed for gold (Farm.BuyFeedStock has no caller; only night-market rewards, NPC shifts, carcass recycling add feed). Starter {f0} kg lasts {days} days for 12 pigs.");
        }

        static void TerminateBeforeWave()
        {
            var e = HireNongCong(117, 1);
            var c = e.NpcRelations.ContractOf(NpcSpecialistId.NongCong)!;
            int total = c.TotalPrice, dep = c.Deposit;
            float aff0 = e.NpcRelations.Get(NpcSpecialistId.NongCong).Affinity;
            int hab0 = e.Farm.HabitatIndex;
            for (int d = 0; d < 6; d++) { e.Tick(24 * 60); e.Clock.IsMinorWaveTriggered = false; }
            NpcContracts.Terminate(e, NpcSpecialistId.NongCong);
            Out($"1-cycle contract total {total}G: paid only the {dep}G deposit, NPC worked 6 days (habitat {hab0}→{e.Farm.HabitatIndex}), terminated before the major wave; affinity {aff0}→{e.NpcRelations.Get(NpcSpecialistId.NongCong).Affinity}");
            Report.V("P2", "NPC contract", "terminate-before-settlement", 117, 7, "Sign ChuKyNho×1 → work the whole cycle → Terminate on major-wave day",
                $"ChuKyNho instalments are only collected at the major wave, so terminating (or defaulting) just before it gets a full cycle of work for the 30% deposit ({dep}G of {total}G). With affinity ≥ 45 a Cam/Tím NPC can repeat this every cycle and stay above the hire threshold.");
        }

        static void ContractCap()
        {
            var e = NewGame(118);
            e.Farm.Gold = 100000;
            for (int i = 0; i < 10; i++) e.DeepMechanics.SpecialtyLocks.RecordManualCleaning();
            e.MajorWavesSurvived = 2; e.MinorWavesDefeated = 3; // state injection: unlock both early mercenaries
            NpcContracts.OnDay(e);
            var a = NpcContracts.Sign(e, NpcSpecialistId.NongCong, ContractKind.ChuKyNho, 1);
            var b = NpcContracts.Sign(e, NpcSpecialistId.VeBinhKhien, ContractKind.TheoDot, 3);
            var c = NpcContracts.Sign(e, NpcSpecialistId.KySuChienTruong, ContractKind.TheoDot, 3);
            Out($"farm level {e.Farm.FarmLevel} cap {NpcContracts.MaxNpcs(e.Farm.FarmLevel)}: NongCong {a.ok}, VeBinh {b.ok}, KySuChienTruong {c.ok} ({c.message})");
            if (c.ok) Report.V("P1", "NPC contract", "cap-probe", 118, 1, "3 signs at farm level 1", "third contract accepted above MaxNpcs");
        }

        static void FeedSackDouble()
        {
            var e = NewGame(120);
            float f0 = e.Farm.FeedStockKg;
            e.Inventory.AddItem("(F)CamThuong", 20);
            float f1 = e.Farm.FeedStockKg;
            for (int d = 0; d < 3; d++) { e.Tick(24 * 60); e.Clock.IsMinorWaveTriggered = false; }
            float f2 = e.Farm.FeedStockKg;
            int sacks = e.Inventory.CountId("(F)CamThuong");
            Out($"+20 CamThuong sacks: FeedStockKg {F.G(f0)}→{F.G(f1)}; after 3 days pigs ate {F.G(f1 - f2)} kg, inventory still holds {sacks} sacks ({sacks * 3.5f} kg) that can be bartered");
            if (sacks == 20 && f1 > f0)
                Report.V("P1", "Feed / inventory", "feed-double-ledger", 120, 1, "Inventory.AddItem(\"(F)CamThuong\", 20) → 3 days",
                    $"a feed sack added to UnifiedInventory also adds 3.5 kg to Farm.FeedStockKg (StardewGradeArchitectureSpine.cs:632-635), but daily feeding only debits FeedStockKg (PigCareCommands.FeedDailyRollup). After the herd ate {F.G(f1 - f2)} kg the {sacks} sacks are still in the inventory and can be offered in night-market barters that want CamThuong (NightMarketBarter.cs:459) — the same feed is eaten and traded. Suspected: depends on whether the runtime ever adds feed through the inventory.");
        }

        static void CoreMajorStall()
        {
            var e = NewGame(121);
            int str = e.Contracts.PlayerStats.EffectiveStr;
            var r = e.ExecuteLiveCombatWave(1, 150, false, false);
            Out($"fresh game, {e.Character.Name} STR {str} (×8 = {str * 8}), {e.Defense.Buildings.Count} towers: major wave threat 150 → victory={r.report.Victory}; cycle {e.Clock.CycleNumber}, day index {e.Clock.CycleDayIndex}/{e.Clock.CycleTotalDays}");
            if (!r.report.Victory)
                Report.V("P2", "Waves", "core-major-unwinnable-early", 121, 1, "ExecuteLiveCombatWave(threat 150) on day 1",
                    $"the Core wave resolver needs tower damage + STR×8 ≥ threat, and a wave only counts as major at threat ≥ 150; a new farm has STR×8 = {str * 8} and no towers, so every Core-resolved major wave is lost and Clock.CompleteMajorWave never runs — the cycle stalls (CycleDayIndex keeps growing past CycleTotalDays) until towers are built. A lost major gives 40G and no instalments are collected.");
        }

        static void AppraisalLockSaveScum()
        {
            var e = NewGame(119);
            var p = e.Pigs.First(x => x.IsAlive && !x.IsIdentified);
            p.Rarity = GeneRarity.Quy; // ensure a failable roll
            var r = e.AppraisePigGeneAtChannel(p.Id, AppraisalChannel.ChoNgay, 0.999);
            Out($"failed appraisal: lock {p.AppraisalLockHoursRemaining} h, failures {p.AppraisalFailureCount}");
            var e2 = new GameEngine(119); e2.RestoreFromSave(e.CreateSaveData());
            var q = e2.Pigs.First(x => x.Id == p.Id);
            Out($"after save/load: lock {q.AppraisalLockHoursRemaining} h, failures {q.AppraisalFailureCount}");
            if (p.AppraisalLockHoursRemaining > 0 && q.AppraisalLockHoursRemaining == 0)
                Report.V("P1", "Save", "appraisal-lock-savescum", 119, 1, "failed appraisal → save → load",
                    $"Pig.AppraisalLockHoursRemaining ({p.AppraisalLockHoursRemaining} h) and AppraisalFailureCount are not in PigSaveData (GameEngine.cs:1415-1471): reloading clears the failed-appraisal lock immediately");
        }
    }

    static class Diag
    {
        public static void Mood()
        {
            var e = new GameEngine(11);
            NewGameSetup.ChooseProtagonist(e, PlayerId.An);
            StarterHerd.FillTo(e);
            for (int d = 0; d < 16; d++)
            {
                var lv = e.Pigs.Where(p => p.IsAlive).ToList();
                var p0 = lv[0];
                Console.WriteLine($"day {e.Clock.CurrentDay} weather {e.CurrentWeather} mood~{lv.Average(p => p.Mood):0} min {lv.Min(p => p.Mood)} comfort~{lv.Average(p => p.Comfort):0} hab {e.Farm.HabitatIndex} ALSK {e.Farm.EventPressureIndex} outsideH~{lv.Average(p => p.OutsideServiceHoursToday):0.0} pos0 ({p0.PositionX:0.0},{p0.PositionY:0.0}) crowd {e.Farm.LocalCrowdingActive} herd {e.Herd.Tier} sick {lv.Count(p => p.ActiveDisease.HasValue)} fed {lv.Count(p => p.FedToday)}");
                for (int h = 0; h < 24; h++) { int m0 = (int)lv.Average(p => p.Mood); e.Tick(60); e.Clock.IsMinorWaveTriggered = false; int m1 = (int)lv.Average(p => p.Mood); if (d < 2 && m1 != m0) Console.WriteLine($"   h{e.Clock.CurrentHour:00} mood {m0}->{m1}"); }
            }
        }
    }

    static class PlaytestProgram
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length > 0 && args[0] == "--diag") { Diag.Mood(); return 0; }
            int cycles = 30, maxDays = 420;
            int[] seeds = { 11, 23, 37, 41, 59, 73 };
            var bots = new List<Bot>();
            foreach (var s in seeds)
            {
                var hero = Array.IndexOf(seeds, s) % 2 == 0 ? PlayerId.An : PlayerId.Khoa;
                Console.WriteLine($"\n=== SEED {s} ({hero}) ===");
                var bot = new Bot(s, hero) { Trace = s == seeds[0] || s == seeds[^1], Lean = Array.IndexOf(seeds, s) >= 4 };
                if (bot.Lean) Console.WriteLine("  (LEAN pure-farming bot: no NPCs, building, intel, gear or appraisal)");
                var t0 = DateTime.UtcNow;
                bot.Run(cycles, maxDays);
                bots.Add(bot);
                var e = bot.e;
                Console.WriteLine($"  ended day {e.Clock.CurrentDay}, cycle {e.Clock.CycleNumber}, gold {e.Farm.Gold}, farm lv {e.Farm.FarmLevel}, living pigs {e.Pigs.Count(p => p.IsAlive)}, feed {e.Farm.FeedStockKg:0} kg, ({(DateTime.UtcNow - t0).TotalSeconds:0.0}s)");
                Console.WriteLine($"  major W/L {bot.MajorWins}/{bot.MajorLosses}, minor {bot.MinorWaves}, faints {bot.Faints}, sold {bot.PigsSold}, breeds {bot.Breeds}, night barters {bot.NightBartersOk}/{bot.NightBarterTries}, feed buys {bot.FeedBuys}, unfed pig-days {bot.UnfedPigDays}");
                Console.WriteLine($"  piglets sold {bot.PigletsSold} for {bot.PigletGold}G, carcasses {bot.CarcassesProcessed} (failed {bot.CarcassFails}: {bot.LastCarcassFail}), treatments {bot.Treatments}, fuel now {e.Farm.FuelLiters}");
                Console.WriteLine($"  core major wins {bot.MajorWins}, core major losses {bot.MajorLosses} (closed via runtime mirror {bot.RuntimeMirrorWins}); last loss: {bot.CoreLossDetail}; feed gold {bot.FeedGoldSpent}");
                Console.WriteLine("  ledger: " + string.Join(", ", e.Ledger.Transactions.GroupBy(t => (t.IsExpense ? "-" : "+") + t.Reason).Select(g => g.Key + " " + g.Sum(t => t.AmountGold)).OrderBy(x => x)));
                Console.WriteLine("  breed fails: " + string.Join("; ", bot.BreedFailReasons.Select(kv => kv.Key + "×" + kv.Value)));
                Console.WriteLine($"  NPC sign {bot.SignOk}, renew {bot.RenewOk}, terminate {bot.Terminations}, defaults {bot.Defaults}, completions {bot.Completions}, roundtrips {bot.RoundTrips}");
                Console.WriteLine($"  contracts now: {string.Join(", ", e.NpcRelations.Contracts.Select(c => c.Id + ":" + c.Kind + " left " + c.InstallmentsLeft))}");
                Console.WriteLine($"  relations: {string.Join(", ", e.NpcRelations.Relations.Values.Where(r => r.Unlocked).Select(r => r.Id + " q" + r.QuestStage + " a" + r.Affinity.ToString("0.#")))}");
                Console.WriteLine("  metrics (first→max): " + string.Join(", ", bot.MetricRange.Select(kv => kv.Key + " " + kv.Value.first + "→" + kv.Value.last)));
                Console.WriteLine("  cycle | start day | rolled len | actual days | gold@start | herd@start | adults | feed kg | contracts");
                foreach (var c in bot.Cycles)
                    Console.WriteLine($"  {c.Cycle,5} | {c.StartDay,9} | {c.RolledLength,10} | {c.ActualDays,11} | {c.GoldAtStart,10} | {c.HerdAtStart,10} | {c.Adults,6} | {c.FeedAtStart,7:0} | {c.NpcContracts}");
            }
            // quest reachability across seeds
            Console.WriteLine("\n=== QUEST METRIC REACHABILITY (max over all seeds) ===");
            foreach (NpcMetric m in Enum.GetValues(typeof(NpcMetric)))
            {
                int moved = bots.Max(b => b.MetricRange[m].last - b.MetricRange[m].first);
                int max = bots.Max(b => b.MetricRange[m].last);
                var npcs = NpcContracts.Profiles.Where(p => p.Metric == m).ToList();
                foreach (var p in npcs)
                {
                    int reachedStage = bots.Max(b => b.e.NpcRelations.Get(p.Id).QuestStage);
                    Console.WriteLine($"  {p.Role,-20} {m,-16} moved {moved,6}, max {max,6}, targets {string.Join("/", p.QuestTargets)}, best quest stage {reachedStage}");
                    if (moved == 0 && max < p.QuestTargets[0])
                        Report.V("P1", "NPC quests", "metric-never-moves:" + m, 0, 0, "full bot runs", $"{p.Role}: metric {m} never moved in {bots.Count} seeds × ~30 cycles of normal Core play (max {max}, step-1 target {p.QuestTargets[0]})");
                }
            }
            Probes.RunAll();

            Console.WriteLine("\n=== VIOLATIONS (deduplicated) ===");
            foreach (var v in Report.ByKey.Values.OrderBy(v => v.Severity).ThenBy(v => v.System))
                Console.WriteLine($"[{v.Severity}] {v.System} :: {v.Key} ×{v.Count} | seed {v.Seed} day {v.Day} | action: {v.Action} | {v.Detail}");
            Console.WriteLine("\n=== NOTES ===");
            foreach (var n in Report.Notes) Console.WriteLine(" - " + n);
            return 0;
        }
    }
}
#endif
