using System;
using System.Linq;
using PigTycoon.Core;

/// <summary>GDD chương 3–7: phối giống F1/F2, đàn khởi đầu, xác heo, tính chất phụ, giám định, SC, cấp trại, kiểm dịch, mật độ, bệnh.</summary>
internal static class HerdLifecycleTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private static Pig Adult(string id, GeneLineId line, string gender, int age = 16)
    {
        var rarity = GeneticsAndBreedingSystem.GetGeneProfile(line).DefaultRarity;
        var p = Pig.CreateDefault(id, id, line, rarity, gender, age, 95f);
        p.SecondaryTraitsRolled = true; // giữ thẻ sạch cho phép thử
        return p;
    }

    public static void RunQuietRecoveryAndAppraisalLocks()
    {
        var e = new GameEngine(seed: 11);
        var scared = Adult("sc", GeneLineId.HongDien, "Cai"); scared.Mood = 10; scared.AppraisalLockHoursRemaining = 24;
        e.Pigs.Add(scared); e.Farm.FeedStockKg = 500f;
        e.Tick(3 * 60);
        Check(scared.Mood >= 25, $"GDD 4.3: a quiet pig recovers +5 per game hour (10 → {scared.Mood} after 3 h)");
        Check(scared.AppraisalLockHoursRemaining == 21, $"GDD 5.4: an appraisal lock runs out with game time (24 → {scared.AppraisalLockHoursRemaining})");
        e.Tick(24 * 60);
        Check(scared.AppraisalLockHoursRemaining == 0, "after a day the failed-appraisal lock is gone");
        Check(scared.IsAlive && scared.Mood > 20, "a Hoảng loạn pig is not pushed into a hunger/health death spiral");
    }

    public static void RunHealthVerbsCost()
    {
        var e = new GameEngine(seed: 5);
        var sick = Adult("s", GeneLineId.HongDien, "Cai"); sick.ActiveDisease = DiseaseId.GheKySinh; e.Pigs.Add(sick);
        var well = Adult("w", GeneLineId.HongDien, "Duc"); e.Pigs.Add(well);
        e.Farm.Gold = 1000;
        Check(e.ExecuteHealthTreatment(HealthTreatmentVerb.DieuTriCaThe, "s", FarmDrug.TriGhe).success && e.Farm.Gold == 1000 - HealthCosts.TreatmentGold(DiseaseId.GheKySinh),
            "GDD 6.5: treatment costs 40–150 G per pig");
        int g = e.Farm.Gold;
        Check(e.ExecuteHealthTreatment(HealthTreatmentVerb.PhongNguaDan).success && e.Farm.Gold == g - 2 * HealthCosts.VaccineGoldPerPig, "vaccine 50 G per healthy pig");
        g = e.Farm.Gold;
        Check(e.ExecuteHealthTreatment(HealthTreatmentVerb.XuLyMoiTruong).success && e.Farm.Gold == g - HealthCosts.LimeGoldPerPlot, "lime 20 G a plot");
        e.Farm.Gold = 10;
        Check(!e.ExecuteHealthTreatment(HealthTreatmentVerb.CachLy, "s").success && !sick.IsIsolated, "no gold, no isolation fence");
    }

    public static void RunXichMaoOncePerDay()
    {
        var x = Adult("xm", GeneLineId.XichMao, "Duc");
        x.Mood = 80;
        Check(x.TrainXichMaoDomesticationDay().trained && x.XichMaoDomesticationDays == 1, "first training today counts");
        Check(!x.TrainXichMaoDomesticationDay().trained && !x.TrainXichMaoDomesticationDay().trained, "more taps the same day are refused");
        Check(x.XichMaoDomesticationDays == 1 && !x.IsXichMaoDomesticated, "three taps no longer finish the 3-day process");
        x.XichMaoTrainedToday = false; // what the daily tick does at day end
        Check(x.TrainXichMaoDomesticationDay().trained && x.XichMaoDomesticationDays == 2, "next day trains again");
    }

    public static void RunBreedingGdd44()
    {
        // Bước 1: 70% trưởng thành; heo già 10,5%; + Hưng Thịnh + 3 tầng Truyền Thừa ≈ 12,0%; heo già Thiết Bì 9,24% (bảng 4.8-B)
        var sow = Adult("sow", GeneLineId.HongDien, "Cai");
        var boar = Adult("boar", GeneLineId.LamKhe, "Duc");
        Check(Math.Abs(GeneticsAndBreedingSystem.CalculatePairConceptionRate(sow, boar, HerdStateTier.KhongCo, 0) - 0.70f) < 0.001f, "adult pair conceives at 70%");
        var oldBoar = Adult("old", GeneLineId.HongDien, "Duc", 30);
        Check(oldBoar.Stage == PigStage.HeoGia, "day 30 is elder");
        float elder = GeneticsAndBreedingSystem.CalculatePairConceptionRate(sow, oldBoar, HerdStateTier.HungThinh, 3);
        Check(Math.Abs(elder - 0.1197f) < 0.001f, $"elder + Hưng Thịnh + 3 tầng = 12,0% (got {elder:P2})");
        var oldThietBi = Adult("oldtb", GeneLineId.ThietBi, "Duc", 30);
        Check(Math.Abs(GeneticsAndBreedingSystem.CalculatePairConceptionRate(sow, oldThietBi, HerdStateTier.KhongCo, 0) - 0.0924f) < 0.001f, "elder Thiết Bì 9,24%");
        var oldKimTho = Adult("oldkt", GeneLineId.KimTho, "Duc", 45);
        var oldSow = Adult("oldsow", GeneLineId.HongDien, "Cai", 30);
        Check(Math.Abs(GeneticsAndBreedingSystem.CalculatePairConceptionRate(oldSow, oldKimTho, HerdStateTier.KhongCo, 0) - 0.105f) < 0.001f,
            "Htuổi takes the lower factor of the two parents (old sow × old Kim Thọ = 10,5%)");

        // Đầy đủ trong engine: chỉ nái+đực trưởng thành; thất bại thì nái phải đợi sang ngày; đậu thì đẻ sau 3 ngày
        var e = new GameEngine(seed: 11);
        e.Farm.FeedStockKg = 2000f;
        e.Pigs.Add(sow); e.Pigs.Add(boar);
        var young = Adult("young", GeneLineId.HongDien, "Duc", 8);
        e.Pigs.Add(young);
        Check(!e.SchedulePigBreeding(sow.Id, young.Id, 0.0).scheduled, "growing boar cannot breed");
        Check(e.ListBreedingPartners(sow.Id).Count == 1 && e.ListBreedingPartners(sow.Id)[0] == boar, "card offers only adult boars");
        Check(!e.SchedulePigBreeding(sow.Id, boar.Id, 0.99).scheduled, "roll above 70% misses");
        Check(!e.SchedulePigBreeding(sow.Id, boar.Id, 0.0).scheduled, "one mating attempt per sow per day (ADR-0019)");
        e.Tick(24 * 60);
        var ok = e.SchedulePigBreeding(sow.Id, boar.Id, 0.10);
        Check(ok.scheduled && sow.GestationDaysRemaining == BreedingLedger.GestationDays, "next day the pair conceives: " + ok.message);
        for (int d = 0; d < 3; d++) e.Tick(24 * 60);
        var piglets = e.Pigs.Where(p => p.MotherId == sow.Id).ToList();
        Check(piglets.Count >= 6 && piglets.Count <= 11, $"litter 6–11 (got {piglets.Count})");
        Check(piglets.All(p => p.Generation == 1 && p.SecondaryTraitsRolled), "different lines → F1, sub-traits rolled at birth");
        Check(piglets.All(p => GeneticsAndBreedingSystem.GetGeneProfile(p.GeneLine).DefaultRarity == p.Rarity), "child rarity matches its line");
        Check(sow.LittersBorn == 1 && sow.DaysSinceLastBirth <= 1, "sow records the litter");
    }

    public static void RunInheritanceGdd44()
    {
        var mother = Adult("m", GeneLineId.HongDien, "Cai");
        var father = Adult("f", GeneLineId.LamKhe, "Duc");
        var rng = new Random(5);
        int n = 20000, fromFather = 0, fromMother = 0;
        for (int i = 0; i < n; i++)
        {
            double inherit = rng.NextDouble();
            var r = GeneticsAndBreedingSystem.RollOffspringLineGdd44(mother, father, false, inherit, rng.NextDouble(), rng.NextDouble(), rng.NextDouble());
            if (inherit < 0.25) { fromFather++; Check(r.geneLine == GeneLineId.LamKhe, "first 25% is the father's line"); }
            else if (inherit < 0.50) { fromMother++; Check(r.geneLine == GeneLineId.HongDien, "next 25% is the mother's line"); }
            else Check(!r.fromParent, "rest rolls the natural table");
        }
        Check(Math.Abs(fromFather / (double)n - 0.25) < 0.02 && Math.Abs(fromMother / (double)n - 0.25) < 0.02, "25/25 split, not 55/45");

        // Mộng Nhãn: 40% thay cho 25%
        var mnFather = Adult("mn", GeneLineId.MongNhan, "Duc");
        Check(GeneticsAndBreedingSystem.LineInheritanceChance(mnFather) == 0.40, "Mộng Nhãn passes its line 40%");
        var mn = GeneticsAndBreedingSystem.RollOffspringLineGdd44(mother, mnFather, false, 0.39, 0.1, 0.1, 0.9);
        Check(mn.geneLine == GeneLineId.MongNhan && mn.rarity == GeneRarity.Quy, "roll 0.39 still inherits Mộng Nhãn");
        var mom = GeneticsAndBreedingSystem.RollOffspringLineGdd44(mother, mnFather, false, 0.60, 0.1, 0.1, 0.9);
        Check(mom.geneLine == GeneLineId.HongDien, "mother's 25% follows the Mộng Nhãn 40%");

        // F2: +4% đột biến lên một bậc, Mộng Nhãn +3%
        var f2 = GeneticsAndBreedingSystem.RollOffspringLineGdd44(mother, father, true, 0.10, 0.1, 0.1, 0.03);
        Check(f2.mutated && f2.rarity == GeneRarity.Kha, "F2 mutation under 4% raises one tier");
        Check(!GeneticsAndBreedingSystem.RollOffspringLineGdd44(mother, father, true, 0.10, 0.1, 0.1, 0.05).mutated, "no F2 mutation at 5%");
        Check(GeneticsAndBreedingSystem.RollOffspringLineGdd44(mother, mnFather, true, 0.60, 0.1, 0.1, 0.065).mutated, "Mộng Nhãn parent: F2 mutation 7%");
        var f1a = Adult("a", GeneLineId.HongDien, "Cai"); f1a.Generation = 1;
        var f1b = Adult("b", GeneLineId.HongDien, "Duc"); f1b.Generation = 1;
        Check(GeneticsAndBreedingSystem.OffspringGeneration(f1a, f1b) == 2, "two F1 parents → F2");
        Check(GeneticsAndBreedingSystem.OffspringGeneration(mother, father) == 1, "different lines → F1");
    }

    public static void RunStarterHerdAndCorpse()
    {
        var e = new GameEngine(seed: 3);
        int added = StarterHerd.FillTo(e);
        Check(added == StarterHerd.DefaultSize && e.LivingPigCount == 12, "new game starts with the 12-pig handed-over herd");
        Check(e.Pigs.All(p => p.Stage == PigStage.TruongThanh && p.SecondaryTraitsRolled), "starter pigs are adults with rolled sub-traits");
        Check(e.Pigs.Any(p => p.Gender == "Cai") && e.Pigs.Any(p => p.Gender == "Duc"), "both sows and boars");
        Check(e.Pigs.Any(p => p.Id == StarterHerd.WhiteFootPigId), "story white-footed pig is in the herd");
        Check(StarterHerd.FillTo(e) == 0, "filling again adds nothing");

        // GDD 4.7: thiêu hủy 45G + 8 lít; xác đã vào Khu Xử Lý không xử lý lại
        var dead = e.Pigs[0];
        dead.Die();
        int gold = e.Farm.Gold, fuel = e.Farm.FuelLiters;
        var res = e.ProcessDeadPigCarcass(dead.Id, CarcassProcessMethod.Incinerate_ThieuHuy);
        Check(res.started && e.Farm.Gold == gold - 45 && e.Farm.FuelLiters == fuel - 8 && dead.IsInProcessingLot, "incineration costs 45G + 8L: " + res.message);
        Check(!e.ProcessDeadPigCarcass(dead.Id, CarcassProcessMethod.Incinerate_ThieuHuy).started, "a corpse is processed once");
        for (int h = 0; h < 7; h++) e.Tick(60);
        Check(!e.Pigs.Contains(dead), "after 6 hours the corpse leaves the farm");
    }

    public static void RunSubTraitsAndGenePassives()
    {
        var rng = new Random(9);
        int n = 20000; int zero = 0, one = 0, two = 0, good = 0, total = 0;
        for (int i = 0; i < n; i++)
        {
            var p = Pig.CreateDefault("t" + i, "t", GeneLineId.HongDien, GeneRarity.Thuong);
            HerdLifecycleRules.RollSecondaryTraits(p, rng);
            int c = p.Traits.Count;
            if (c == 0) zero++; else if (c == 1) one++; else two++;
            good += p.Traits.Count(t => t.IsPositive); total += c;
        }
        Check(Math.Abs(zero / (double)n - 0.40) < 0.02 && Math.Abs(one / (double)n - 0.45) < 0.02 && Math.Abs(two / (double)n - 0.15) < 0.02, "0/1/2 sub-traits at 40/45/15%");
        Check(Math.Abs(good / (double)total - 0.60) < 0.02, "Thường lines roll 60% good");
        var hu = Pig.CreateDefault("hu", "hu", GeneLineId.HuThe, GeneRarity.DiBien);
        HerdLifecycleRules.RollSecondaryTraits(hu, rng);
        Check(hu.Traits.Count == 2 && hu.Traits.Count(t => t.IsDeviant && t.IsPositive) == 1 && hu.Traits.Count(t => t.IsDeviant && !t.IsPositive && !t.IsAwakened) == 1,
            "Hư Thể: one good + one sleeping bad deviant trait");
        var bad = Pig.CreateDefault("bad", "bad", GeneLineId.HongDien, GeneRarity.Thuong);
        HerdLifecycleRules.RollSecondaryTraits(bad, rng, badGene: true);
        Check(bad.Traits.Any(t => !t.IsPositive), "Gen Xấu always carries a bad sub-trait");

        var shortLived = Pig.CreateDefault("dt", "dt", GeneLineId.HongDien, GeneRarity.Thuong);
        shortLived.Traits.Add(new PigTrait { Id = "KhoNgu", Name = "Đoản Thọ" });
        Check(shortLived.MaxLifespanDays == 37, "Đoản Thọ: lifespan −3 days");

        // Thổ Trầm: Tâm Tĩnh mất 50%; heo khác trong 5 m mất −20%
        var tho = Adult("tt", GeneLineId.ThoTram, "Duc");
        var near = Adult("nb", GeneLineId.HongDien, "Cai"); near.PositionX = 3f;
        var herd = new[] { tho, near };
        Check(HerdLifecycleRules.AgitationMoodLoss(tho, 10, false) == 5, "Thổ Trầm loses half");
        Check(HerdLifecycleRules.IsNearThoTram(near, herd) && HerdLifecycleRules.AgitationMoodLoss(near, 10, true) == 8, "neighbour within 5 m loses 20% less");

        // Tinh Quang: SC ≥ 60 suốt Đang lớn → ≥ A; SC < 50 một ngày → C
        Pig Grow(int[] scDays)
        {
            var p = Pig.CreateDefault("tq" + scDays[0] + scDays[^1], "tq", GeneLineId.TinhQuang, GeneRarity.Hiem, "Duc", 4, 12f);
            p.Mood = 50;
            foreach (int sc in scDays) { p.FedToday = false; p.AdvanceDay(sc, false, 0f); p.Health = 100; p.Mood = 50; }
            return p;
        }
        var lit = Grow(new[] { 65, 65, 65, 65, 65, 65, 65 });
        Check(lit.Stage == PigStage.TruongThanh && lit.MeatQuality >= MeatQuality.A, $"Tinh Quang raised at SC ≥ 60 is at least A (got {lit.MeatQuality})");
        var dim = Grow(new[] { 80, 80, 45, 80, 80, 80, 80 });
        Check(dim.MeatQuality == MeatQuality.C, "Tinh Quang that saw SC < 50 drops to C");

        // Thiết Bì: miễn Ghẻ Ký Sinh
        Check(HerdLifecycleRules.ChooseOutbreakDisease(Adult("tb", GeneLineId.ThietBi, "Duc"), WeatherType.NomAm, false) != DiseaseId.GheKySinh, "Thiết Bì never catches Ghẻ");
    }

    public static void RunAppraisalUsesEngineRng()
    {
        var e = new GameEngine(seed: 21);
        e.Farm.Gold = 1_000_000;
        int ok = 0, n = 300;
        for (int i = 0; i < n; i++)
        {
            var p = Pig.CreateDefault("r" + i, "r", GeneLineId.ThietBi, GeneRarity.Hiem, "Duc", 16, 90f);
            e.Pigs.Add(p);
            if (e.AppraisePigGeneAtChannel(p.Id, AppraisalChannel.ChoNgay).success) ok++;
            e.Pigs.Where(x => x.GeneLine == GeneLineId.ThietBi).ToList().ForEach(x => x.IsIdentified = false);
        }
        double rate = ok / (double)n;
        Check(rate > 0.60 && rate < 0.80, $"Chợ ngày appraises Hiếm at ~70% with the engine RNG (got {rate:P0})");
        var spec = CanonicalSagaAndGdd6Systems.GetCanonicalAppraisalChannelSpecGdd54(AppraisalChannel.PhongNghienCuu, GeneRarity.Hiem);
        Check(spec.goldCost == 700, "research-room price for Hiếm is 700G (the card label shows this)");
    }

    public static void RunHabitatCleaningAndFarmLevel()
    {
        GameEngine Make() { var e = new GameEngine(seed: 4); e.Farm.FeedStockKg = 500f; e.Farm.HabitatIndex = 70; e.Pigs.Add(Adult("x", GeneLineId.HongDien, "Cai")); return e; }
        var clean = Make(); var idle = Make();
        Check(HerdLifecycleRules.TryCleanYard(clean).success && HerdLifecycleRules.TryCleanYard(clean).success, "player cleans twice");
        clean.Tick(24 * 60); idle.Tick(24 * 60);
        Check(clean.Farm.HabitatIndex - idle.Farm.HabitatIndex == 8, $"two real cleanings add +8 SC (got {clean.Farm.HabitatIndex} vs {idle.Farm.HabitatIndex})");
        Check(clean.Farm.CleaningsToday == 0, "cleanings reset at 06:00");
        var lazy = Make(); lazy.Farm.HabitatIndex = 60;
        lazy.Tick(24 * 60);
        Check(lazy.Farm.HabitatIndex <= 62, "no free cleaning: SC no longer climbs +4 a day by itself");

        // 6.1 tier effects
        Check(HerdLifecycleRules.HabitatGrowthModifier(95) == 0.04f && HerdLifecycleRules.HabitatGrowthModifier(40) == -0.05f && HerdLifecycleRules.HabitatGrowthModifier(10) == -0.12f, "SC growth +4% / −5% / −12%");
        // contract change 3 Nov: the quiet +5 is per game hour (GDD 4.3), applied by RecoverQuietHour; the daily rollup keeps Ô Uế −2/day
        var calm = Adult("c", GeneLineId.HongDien, "Cai"); calm.Mood = 50;
        calm.RecoverQuietHour(95, 1f); calm.AdvanceDay(95, false, 0f);
        var foul = Adult("f", GeneLineId.HongDien, "Cai"); foul.Mood = 50;
        foul.RecoverQuietHour(20, 1f); foul.AdvanceDay(20, false, 0f);
        Check(calm.Mood == 56 && foul.Mood == 53, $"Trong Lành recovers +15% (56), Ô Uế −2/day (53): {calm.Mood}/{foul.Mood}");

        // 3.5 Cấp trại theo EXP tích lũy, lên nhiều cấp một lần
        var farm = new Farm();
        farm.AddFarmExp(1199); Check(farm.FarmLevel == 2, "1.199 EXP is level 2");
        farm.AddFarmExp(1); Check(farm.FarmLevel == 3, "1.200 EXP is level 3");
        farm.AddFarmExp(16000 - 1200); Check(farm.FarmLevel == 8, "16.000 EXP jumps straight to level 8");
        farm.AddFarmExp(500000); Check(farm.FarmLevel == 20 && farm.ExpForNextFarmLevel == 0, "cap at level 20");
    }

    public static void RunQuarantineDensityAndDisease()
    {
        var e = new GameEngine(seed: 8);
        var pig = Adult("sell", GeneLineId.HongDien, "Duc", 20);
        e.Pigs.Add(pig);
        var miss = e.SellPigAtDayMarket(pig.Id, isQuarantined: false, deterministicSaleRoll: 0.75);
        Check(!miss.success && e.Pigs.Contains(pig), "unquarantined Thường sells only 70% of the time: " + miss.message);
        Check(!e.SellPigAtDayMarket(pig.Id, isQuarantined: false, deterministicSaleRoll: 0.0).success, "the buyer does not come back the same day");
        Check(e.SellPigAtDayMarket(pig.Id, isQuarantined: true).success, "quarantined healthy pig always sells");

        var sick = Adult("sick", GeneLineId.HongDien, "Duc", 20);
        sick.ActiveDisease = DiseaseId.HoHapLanh;
        e.Pigs.Add(sick);
        Check(!e.SellPigAtDayMarket(sick.Id, true).success && !e.SellPigAtDayMarket(sick.Id, false, 0.0).success, "a known-sick pig cannot be sold");

        // 3.3 mật độ: Tinh thần −1/−2/−4, cám ×1,10
        Check(HerdLifecycleRules.DensityDailyMoodLoss(1.1f) == 1 && HerdLifecycleRules.DensityDailyMoodLoss(1.3f) == 2 && HerdLifecycleRules.DensityDailyMoodLoss(1.6f) == 4, "density morale −1/−2/−4");
        var crowd = new GameEngine(seed: 2);
        crowd.Farm.Infrastructure.LandPlots = 1; crowd.Farm.Infrastructure.Shelters = 1;  // sức chứa 6
        for (int i = 0; i < 7; i++) crowd.Pigs.Add(Adult("c" + i, GeneLineId.LamKhe, "Cai", 20));
        float overKg = PigCareCommands.DailyFeedKilograms(crowd, crowd.Pigs[0]);
        crowd.Pigs.RemoveAt(6);
        float okKg = PigCareCommands.DailyFeedKilograms(crowd, crowd.Pigs[0]);
        Check(Math.Abs(overKg / okKg - 1.10f) < 0.001f, "overloaded troughs need ×1,10 feed");

        // 6.4: bệnh theo nguồn phát & dòng gen
        var plain = Adult("pl", GeneLineId.HongDien, "Duc");
        Check(HerdLifecycleRules.ChooseOutbreakDisease(plain, WeatherType.GiongBao, false) != DiseaseId.LoiTam &&
              HerdLifecycleRules.ChooseOutbreakDisease(plain, WeatherType.MuaBucXa, false) != DiseaseId.NamPhatQuang &&
              HerdLifecycleRules.ChooseOutbreakDisease(plain, WeatherType.NangGat, false) != DiseaseId.SayNang,
            "gene-line (D) and acute (C) diseases never come from the A/B outbreak roll");
        var loi = new GameEngine(seed: 6);
        var lm = Adult("lm", GeneLineId.LoiMach, "Duc"); lm.LatentDisease = DiseaseId.LoiTam; lm.Mood = 10;
        loi.Pigs.Add(lm); loi.Farm.FeedStockKg = 100f;
        loi.Tick(24 * 60);
        Check(lm.ActiveDisease == DiseaseId.LoiTam || !lm.IsAlive, "latent Lôi Tâm of a Lôi Mạch breaks out when morale < 30");

        // 6.5: lây 6 giờ — chỉ khi gần nhau, Thiết Bì không làm cầu nối, heo cách ly không lây
        int Spread(GeneLineId sourceLine, bool isolated, float distance)
        {
            int hits = 0;
            for (int t = 0; t < 200; t++)
            {
                var g = new GameEngine(seed: 100 + t);
                var src = Adult("s", sourceLine, "Duc"); src.ActiveDisease = DiseaseId.HoHapLanh; src.IsIsolated = isolated;
                var dst = Adult("d", GeneLineId.HongDien, "Cai"); dst.PositionX = distance;
                g.Pigs.Add(src); g.Pigs.Add(dst);
                hits += g.RunSixHourSpreadCheck();
            }
            return hits;
        }
        Check(Spread(GeneLineId.HongDien, false, 2f) > 0, "a sick pig infects a close neighbour over time");
        Check(Spread(GeneLineId.HongDien, false, 30f) == 0, "no spread beyond contact range");
        Check(Spread(GeneLineId.ThietBi, false, 2f) == 0 && Spread(GeneLineId.HongDien, true, 2f) == 0, "Thiết Bì and isolated pigs do not pass it on");
    }
}
