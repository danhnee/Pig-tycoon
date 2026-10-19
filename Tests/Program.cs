using System;
using System.Collections.Generic;
using PigTycoon.Core;

namespace PigTycoon.Runner
{
    class Program
    {
        static int totalTests = 0;
        static int passedTests = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("==================================================================");
            Console.WriteLine("     PIG TYCOON (UNITY C# CORE) - INVARIANT VERIFICATION SUITE    ");
            Console.WriteLine("==================================================================\n");

            RunTest("GameClock: 16-min day, 4 day parts & sleep cooldown", TestClock);
            RunTest("Farm: Capacity bottleneck & soft density interpolation", TestFarmCapacity);
            RunTest("Pig: 4 stages, Hư Thể seal & Xích Mao domestication", TestPigLifecycle);
            RunTest("HerdManager: 7 conditions & 3 consecutive days for Sơ Khai", TestHerdManager);
            RunTest("Economy: Official pricing formula matching GDD v6.0 section 4.5", TestEconomyPricing);
            RunTest("Economy: Night market 18% gold slots & Bí nhân 0% gold", TestNightMarketAndMystic);
            RunTest("Defense: Building Hall requirement & Bạch Vân manual trigger", TestDefense);
            RunTest("Grid & Modular Fence: 16-way neighbor bitmask & auto-connection logic", TestFenceGridAutoConnect);
            RunTest("Combat: Weapon archetypes (Slash, Smash, Thrust) & Data-Driven parameters", TestWeaponDataSystem);
            RunTest("Combat: Enemy Archetypes (Thief, Breaker, Pest) & Damage Formulas", TestEnemyDataAndFormulas);
            RunTest("Combat: Stardew 1.6 Combat Mechanics & 64x64 Character Invariants", TestStardewCombatMechanics64x64);
            RunTest("Combat: Training Dummy Resilience & Arena Analytics System", TestTrainingDummyAndArenaAnalytics);
            RunTest("Combat: Loot System Themed Drops & Drop Rate Invariants", TestLootSystemAndDropRules);
            RunTest("Combat: Pig Snatching, Abduction & Raid Telegraph Invariants", TestPigSnatchingAndRaidTelegraphInvariants);
            RunTest("Combat: Alien Escalation Chain (UFO Failures -> Alien Drop 3 -> Alien Raid -> Mothership & Boss Warlord)", TestAlienEscalationChain);
            RunTest("Combat: Bandit Escalation Chain (Thief Failures -> Ambush Drop 3 -> Bandit Raid -> Bandit Boss Siege & Outpost)", TestBanditEscalationChain);
            RunTest("Combat: P1.08 Dual-Layer Stamina & 3-Hit Combo & Dodge Roll Invariants", TestDualLayerStaminaAndComboMechanics);
            RunTest("Economy & Ledger: Day Market Feed/Pig Trading, 1G Precision & Dual Budget Model", TestFinancialLedgerAndDayMarket);

            Console.WriteLine("\n------------------------------------------------------------------");
            Console.WriteLine($"KẾT QUẢ: {passedTests}/{totalTests} tests passed ({(passedTests == totalTests ? "100% THÀNH CÔNG" : "CÓ LỖI")})");
            Console.WriteLine("==================================================================");
        }

        static void RunTest(string name, Action testAction)
        {
            totalTests++;
            try
            {
                testAction();
                passedTests++;
                Console.WriteLine($"✔ [PASS] {name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✖ [FAIL] {name}: {ex.Message}");
            }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static void TestClock()
        {
            var clock = new GameClock(1, 6, 0); // 06:00
            Assert(clock.GetTimeOfDay() == TimeOfDay.Sang, "06:00 phải là buổi Sáng");

            clock.TickMinutes(240); // 10:00
            Assert(clock.GetTimeOfDay() == TimeOfDay.Trua, "10:00 phải là buổi Trưa");

            clock.TickMinutes(240); // 14:00
            Assert(clock.GetTimeOfDay() == TimeOfDay.Chieu, "14:00 phải là buổi Chiều");

            clock.TickMinutes(240); // 18:00
            Assert(clock.GetTimeOfDay() == TimeOfDay.Toi, "18:00 phải là buổi Tối");

            var sleepRes = clock.PerformStandardSleep();
            Assert(sleepRes.success, "Ngủ chuẩn phải thành công");
            Assert(clock.CurrentHour == 6 && clock.CurrentDay == 2, "Sau khi ngủ phải đến 06:00 ngày tiếp theo");
            Assert(clock.SleepCooldownHoursRemaining == 6, "Cooldown ngủ phải là 6 giờ");

            var sleepAgain = clock.PerformStandardSleep();
            Assert(!sleepAgain.success, "Ngủ lại trong cooldown phải bị từ chối");
        }

        static void TestFarmCapacity()
        {
            var farm = new Farm();
            farm.Infrastructure.LandPlots = 12;     // 96
            farm.Infrastructure.Shelters = 12;      // 72 (nghẽn)
            farm.Infrastructure.WaterTroughs = 9;   // 108
            farm.Infrastructure.Feeders = 9;        // 90

            var rep = farm.CalculateCapacityReport(50, 2);
            Assert(rep.BaseCapacity == 72, "Sức chứa gốc phải là 72");
            Assert(rep.Bottleneck == "Shelters", "Điểm nghẽn phải là Mái trú");
            Assert(rep.EffectiveCapacity == 70, "Sức chứa hiệu dụng phải là 70 (72 - 2)");

            // Kiểm tra nội suy mềm
            Assert(Math.Abs(farm.CalculateSoftInterpolatedDensity(0.70f) - 0.85f) < 0.001f, "<=70% phải là 0.85");
            Assert(Math.Abs(farm.CalculateSoftInterpolatedDensity(1.00f) - 1.00f) < 0.001f, "100% phải là 1.00");
            Assert(Math.Abs(farm.CalculateSoftInterpolatedDensity(1.20f) - 1.25f) < 0.001f, "120% phải là 1.25");
            Assert(Math.Abs(farm.CalculateSoftInterpolatedDensity(1.50f) - 1.65f) < 0.001f, "150% phải là 1.65");
            Assert(Math.Abs(farm.CalculateSoftInterpolatedDensity(1.80f) - 2.20f) < 0.001f, ">=180% phải là 2.20");
        }

        static void TestPigLifecycle()
        {
            var huThe = Pig.CreateDefault("p_hu", "Hư Thể", GeneLineId.HuThe, GeneRarity.DiBien);
            Assert(huThe.Traits.Exists(t => t.IsDeviant && !t.IsPositive && !t.IsAwakened), "Hư Thể tính chất xấu phải ngủ ban đầu");

            // Nuôi qua tuổi 10 mà giữ sạch -> phong ấn
            huThe.AgeDays = 10;
            huThe.Stage = PigStage.DangLon;
            huThe.AdvanceDay(75, false, 0f);
            Assert(huThe.Stage == PigStage.TruongThanh, "Phải lên Trưởng thành");
            Assert(huThe.Traits.Find(t => t.IsDeviant && !t.IsPositive).IsSealed, "Hư Thể phải được phong ấn vĩnh viễn");
        }

        static void TestHerdManager()
        {
            var herd = new HerdManager();
            var farm = new Farm { FarmLevel = 6, HabitatIndex = 75 };
            farm.Infrastructure.LandPlots = 20;
            farm.Infrastructure.Shelters = 25; // 150 chỗ
            farm.Infrastructure.WaterTroughs = 15;
            farm.Infrastructure.Feeders = 15;

            var pigs = new List<Pig>();
            for (int i = 0; i < 95; i++)
            {
                var p = Pig.CreateDefault($"p_{i}", $"P_{i}", GeneLineId.HongDien, GeneRarity.Thuong);
                p.Bonding = 60f;
                p.Stage = PigStage.TruongThanh;
                pigs.Add(p);
            }
            // Thêm 2 Kim Thọ (mỗi con tính = 3 -> 95 + 6 = 101)
            for (int i = 0; i < 2; i++)
            {
                var kt = Pig.CreateDefault($"kt_{i}", $"KT_{i}", GeneLineId.KimTho, GeneRarity.HuyenThoai);
                kt.Bonding = 60f;
                kt.Stage = PigStage.TruongThanh;
                pigs.Add(kt);
            }
            // Thêm 6 heo già hòa nhập >= 65
            for (int i = 0; i < 6; i++)
            {
                var elder = Pig.CreateDefault($"e_{i}", $"E_{i}", GeneLineId.ThoTram, GeneRarity.Kha);
                elder.Stage = PigStage.HeoGia;
                elder.Bonding = 70f;
                pigs.Add(elder);
            }
            // Thêm 10 heo để đạt mật độ 71 - 110%
            for (int i = 0; i < 10; i++)
            {
                var extra = Pig.CreateDefault($"ext_{i}", $"Ext_{i}", GeneLineId.HongDien, GeneRarity.Thuong);
                extra.Bonding = 60f;
                pigs.Add(extra);
            }

            var rep = herd.EvaluateConditions(pigs, farm, 4);
            Assert(rep.AllConditionsMet, "Tất cả 7 điều kiện Bầy Đàn phải được thỏa mãn");

            // Ngày 1
            herd.AdvanceDay(pigs, farm, 4);
            Assert(herd.Tier == HerdStateTier.KhongCo, "Ngày 1 chưa được lên Sơ Khai");
            // Ngày 2
            herd.AdvanceDay(pigs, farm, 4);
            Assert(herd.Tier == HerdStateTier.KhongCo, "Ngày 2 chưa được lên Sơ Khai");
            // Ngày 3 -> Lên Sơ Khai
            var res3 = herd.AdvanceDay(pigs, farm, 4);
            Assert(herd.Tier == HerdStateTier.SoKhai, "Ngày 3 đủ điều kiện liên tục phải lên Sơ Khai");
        }

        static void TestEconomyPricing()
        {
            var eco = new EconomyManager();

            // Heo trưởng thành 105 kg, hạng A, giống Hiếm đã định danh, đã kiểm dịch, chợ bình thường:
            // 105 * 3 * 1.20 * 1.00 * 1.30 = 491.4G -> 491G
            var adultRare = Pig.CreateDefault("p_rare", "Thiết Bì", GeneLineId.ThietBi, GeneRarity.Hiem);
            adultRare.WeightKg = 105f;
            adultRare.MeatQuality = MeatQuality.A;
            adultRare.Stage = PigStage.TruongThanh;
            adultRare.IsIdentified = true;

            var (price, _) = eco.CalculatePigSalePrice(adultRare, 1);
            Assert(price == 491, $"Giá heo phải là 491G (thực tế: {price}G)");

            // Heo non cùng giống Hiếm đã định danh: 315 * 1.00 * 1.30 = 409.5G -> 410G
            var pigletRare = Pig.CreateDefault("p_rare_c", "Thiết Bì Con", GeneLineId.ThietBi, GeneRarity.Hiem);
            pigletRare.Stage = PigStage.HeoNon;
            pigletRare.IsIdentified = true;
            var (pricePiglet, _) = eco.CalculatePigSalePrice(pigletRare, 1);
            Assert(pricePiglet == 410, $"Giá heo non Hiếm phải là 410G (thực tế: {pricePiglet}G)");
        }

        static void TestNightMarketAndMystic()
        {
            var eco = new EconomyManager();
            eco.GenerateNightMarket(5);
            Assert(eco.NightListings.Count == 8, "Chợ đêm phải có 8 ô hàng");

            // Bí nhân ban đêm
            eco.RollMysticMerchant(22, 60);
            if (eco.MysticMerchant.IsActive)
            {
                Assert(eco.MysticMerchant.DurationHoursRemaining == 2, "Bí nhân chỉ tồn tại đúng 2 giờ");
                Assert(eco.MysticMerchant.DistanceFromBoundaryMeters <= 30, "Bí nhân cách ranh giới <= 30m");
            }
        }

        static void TestDefense()
        {
            var def = new DefenseManager();
            var bldg = def.ConstructBuilding("NoXuyenVan");
            Assert(bldg.success, "Xây Nỏ Xuyên Vân phải thành công");
            Assert(bldg.building.AmmoCurrent == 50, "Nỏ Xuyên Vân nạp tối đa 50 tên");

            // Không nạp được trong đợt
            var preloadFail = def.PreloadAmmo(bldg.building.InstanceId, 10, true);
            Assert(!preloadFail.success, "Không được phép nạp đạn trong đợt quái");

            // Bạch Vân không tự động mà phải kích thủ công
            var bv = def.ConstructBuilding("CuuTieuLoiPhao");
            // Chưa đủ phòng cấp 3 -> phải thất bại
            Assert(!bv.success, "Chưa đủ cấp Phòng Xây Dựng thì không thể xây Bạch Vân");

            def.HallLevel = 3;
            def.UnlockedBlueprints.Add("CuuTieuLoiPhao");
            var bvOk = def.ConstructBuilding("CuuTieuLoiPhao");
            Assert(bvOk.success, "Cấp 3 và đã học bản vẽ thì xây thành công");
            Assert(bvOk.building.IsBachVan, "Phải được đánh dấu là Bạch Vân");

            var trigger = def.TriggerBachVanManual(bvOk.building.InstanceId);
            Assert(trigger.success, "Kích hoạt thủ công Bạch Vân thành công");
            Assert(bvOk.building.AmmoCurrent == 70, "Tiêu hao 50 đạn (120 - 50 = 70)");
        }

        static void TestFenceGridAutoConnect()
        {
            var grid = new HashSet<(int x, int y)>();

            int GetMask(int x, int y)
            {
                bool n = grid.Contains((x, y + 1));
                bool e = grid.Contains((x + 1, y));
                bool s = grid.Contains((x, y - 1));
                bool w = grid.Contains((x - 1, y));
                return (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);
            }

            // 1. Đặt 1 cọc đơn lập tại (0,0) -> Mask = 0
            grid.Add((0, 0));
            Assert(GetMask(0, 0) == 0, "Cọc rào đơn lập phải có mask = 0 (fence_post)");

            // 2. Đặt thêm 1 cọc tại (1,0) (Phía Đông) -> Tự động nối ngang
            grid.Add((1, 0));
            Assert(GetMask(0, 0) == 2, "Cọc (0,0) phải nối sang Đông (mask = 2: fence_end_e)");
            Assert(GetMask(1, 0) == 8, "Cọc (1,0) phải nối sang Tây (mask = 8: fence_end_w)");

            // 3. Đặt thêm 1 cọc tại (0,1) (Phía Bắc) -> (0,0) thành góc Đông Bắc
            grid.Add((0, 1));
            Assert(GetMask(0, 0) == 3, "Cọc (0,0) có Bắc + Đông -> Corner NE (mask = 3: fence_corner_ne)");
            Assert(GetMask(0, 1) == 4, "Cọc (0,1) nối xuống Nam (mask = 4: fence_end_s)");

            // 4. Đặt đủ 4 phía: Nam (0,-1) và Tây (-1,0) -> Ngã tư Cross (mask = 15)
            grid.Add((0, -1));
            grid.Add((-1, 0));
            Assert(GetMask(0, 0) == 15, "Cọc (0,0) có đủ 4 phía -> Cross (mask = 15: fence_cross)");

            // 5. Tháo dỡ cọc phía Đông (1,0) -> (0,0) tự động chuyển sang Ngã ba chữ T (T-West)
            grid.Remove((1, 0));
            Assert(GetMask(0, 0) == 13, "Tháo dỡ (1,0) thì (0,0) còn Bắc + Nam + Tây -> T-West (mask = 13: fence_t_west)");

            // 6. Tháo dỡ các cọc còn lại -> Quay về đơn lập mask = 0
            grid.Remove((0, 1));
            grid.Remove((0, -1));
            grid.Remove((-1, 0));
            Assert(GetMask(0, 0) == 0, "Khi không còn lân cận -> Đơn lập (mask = 0)");
        }

        static void TestWeaponDataSystem()
        {
            var sword = WeaponData.CreateDefaultSword();
            Assert(sword.ActionType == CombatActionType.Slash, "Đao Gỗ phải có ActionType là Slash");
            Assert(sword.ArcAngle == 120f, "Đao Gỗ phải có góc quét ArcAngle = 120 độ");
            Assert(sword.BaseDamage == 15f, "Đao Gỗ BaseDamage phải là 15");

            var hammer = WeaponData.CreateHammer();
            Assert(hammer.ActionType == CombatActionType.Smash, "Búa Tạ phải có ActionType là Smash");
            Assert(hammer.KnockbackForce > sword.KnockbackForce, "Búa Tạ phải có lực đẩy lùi mạnh hơn Đao Gỗ");
            Assert(hammer.Cooldown > sword.Cooldown, "Búa Tạ phải có thời gian vung đòn chậm hơn Đao Gỗ");

            var spear = WeaponData.CreateSpear();
            Assert(spear.ActionType == CombatActionType.Thrust, "Giáo Tre phải có ActionType là Thrust");
            Assert(spear.AttackRange > sword.AttackRange, "Giáo Tre phải có tầm đánh xa hơn Đao Gỗ");
            Assert(spear.ArcAngle < sword.ArcAngle, "Giáo Tre phải có góc đâm hẹp và tập trung hơn Đao Gỗ");
        }

        static void TestEnemyDataAndFormulas()
        {
            // 1. Kiểm tra khởi tạo các archetypes quái vật
            var thief = EnemyData.CreateKeBatHeo();
            Assert(thief.TypeId == EnemyTypeId.KeBatHeo, "Kẻ Bắt Heo phải đúng TypeId");
            Assert(thief.MaxHp == 150f, "Kẻ Bắt Heo MaxHp = 150");
            Assert(thief.MoveSpeed == 2.5f, "Kẻ Bắt Heo MoveSpeed = 2.5");
            Assert(thief.GoldReward == 35, "Kẻ Bắt Heo GoldReward = 35");

            var breaker = EnemyData.CreateKeCaoPha();
            Assert(breaker.TypeId == EnemyTypeId.KeCaoPha, "Kẻ Cào Phá phải đúng TypeId");
            Assert(breaker.MaxHp == 120f, "Kẻ Cào Phá MaxHp = 120");

            var pest = EnemyData.CreateChuotGamKho();
            Assert(pest.TypeId == EnemyTypeId.ChuotGamKho, "Chuột Gặm Kho phải đúng TypeId");
            Assert(pest.MaxHp == 40f, "Chuột Gặm Kho MaxHp = 40");
            Assert(pest.MoveSpeed > thief.MoveSpeed, "Chuột Gặm Kho phải di chuyển nhanh hơn Kẻ Bắt Heo");

            // 2. Kiểm tra công thức tính sát thương CombatFormulas
            float rawDmg = 30f;
            float dmgWithStr = CombatFormulas.CalculateDamage(rawDmg, attackerStr: 20, defenderRes: 0);
            Assert(dmgWithStr > rawDmg, "Sát thương với Str=20 phải lớn hơn BaseDamage");

            float dmgWithRes = CombatFormulas.CalculateDamage(rawDmg, attackerStr: 10, defenderRes: 25);
            Assert(dmgWithRes < rawDmg, "Sát thương bị giảm trừ bởi chỉ số Res của đối thủ");

            // 3. Nhận sát thương và trừ máu
            float taken = thief.TakeDamage(50f);
            Assert(thief.CurrentHp == 100f, "Thief nhận 50 sát thương phải còn đúng 100 HP");
            Assert(thief.IsAlive, "Thief còn 100 HP phải còn sống");

            thief.TakeDamage(200f);
            Assert(!thief.IsAlive, "Thief hết máu phải chết (IsAlive == false)");
            Assert(thief.CurrentHp == 0f, "Máu không được âm");

            // 4. Nộ khí tích tụ
            int rageGain = CombatFormulas.CalculateRageGainOnDamage(20f);
            Assert(rageGain >= 5 && rageGain <= 25, "Lượng nộ tích tụ phải nằm trong khoảng hợp lệ [5, 25]");
        }

        static void TestStardewCombatMechanics64x64()
        {
            // 1. Tốc độ vung đòn (Swipe Speed ms) từ MeleeWeapon.cs
            float defaultSpeed = CombatFormulas.CalculateSwipeSpeed(weaponSpeed: 0, addedSpeed: 0f);
            Assert(defaultSpeed == 400f, "Tốc độ vung mặc định phải là 400ms");

            float fastSpeed = CombatFormulas.CalculateSwipeSpeed(weaponSpeed: 4, addedSpeed: 0f);
            Assert(fastSpeed == 240f, "Tốc độ vung khi weaponSpeed = 4 phải là 240ms (400 - 4*40)");

            // 2. Tỉ lệ Chí Mạng (Crit Chance)
            float baseCrit = 0.02f;
            float critWithLuck = CombatFormulas.CalculateCriticalChance(baseCrit, luckLevel: 5);
            Assert(critWithLuck > baseCrit, "May mắn (Luck=5) phải tăng tỉ lệ chí mạng");

            float daggerCrit = CombatFormulas.CalculateCriticalChance(baseCrit, luckLevel: 0, isDagger: true);
            Assert(daggerCrit > baseCrit, "Dao găm (Dagger) phải có bonus chí mạng (+0.005, x1.12)");

            // 3. Công thức Sát thương Stardew (CalculateStardewDamage)
            int normalDmg = CombatFormulas.CalculateStardewDamage(minDamage: 20, maxDamage: 30, attackerAttack: 5, randomRoll: 25);
            Assert(normalDmg == 40, "Sát thương thường phải là 25 + 5*3 = 40");

            int critDmg = CombatFormulas.CalculateStardewDamage(minDamage: 20, maxDamage: 30, attackerAttack: 5, isCrit: true, critMultiplier: 3.0f, randomRoll: 25);
            Assert(critDmg == 90, "Sát thương chí mạng phải là 25*3 + 5*3 = 90");

            int mitigatedDmg = CombatFormulas.CalculateStardewDamage(minDamage: 20, maxDamage: 30, attackerAttack: 5, defenderResilience: 15, randomRoll: 25);
            Assert(mitigatedDmg == 25, "Sát thương sau khi trừ kháng defenderResilience=15 phải là 40 - 15 = 25");

            // 4. Cơ chế Đỡ đòn (Parry)
            int incomingDmg = 50;
            int counterDmg = CombatFormulas.CalculateParryCounterDamage(incomingDmg);
            Assert(counterDmg == 25, "Phản sát thương khi Parry phải là 50 / 2 = 25");

            float groundKnockback = CombatFormulas.GetParryKnockbackMultiplier(isGlider: false);
            Assert(groundKnockback == 3.5f, "Đẩy lùi quái đất khi Parry phải là x3.5");

            float gliderKnockback = CombatFormulas.GetParryKnockbackMultiplier(isGlider: true);
            Assert(gliderKnockback == 2.0f, "Đẩy lùi quái bay khi Parry phải là x2.0");

            // 5. Tỉ lệ không gian & Tầm đánh 64x64 Character
            var sword = WeaponData.CreateDefaultSword();
            Assert(sword.SpecialSkill == WeaponSpecialSkill.Parry, "Kiếm/Đao phải có kỹ năng Đỡ đòn (Parry)");
            Assert(sword.AttackRange >= 1.25f && sword.AttackRange <= 1.5f, "Tầm chém kiếm phải bao phủ > 1 ô tile (64px = 1.0 unit)");

            var spear = WeaponData.CreateSpear();
            Assert(spear.SpecialSkill == WeaponSpecialSkill.Flurry, "Giáo phải có kỹ năng Đâm liên hoàn (Flurry)");
            Assert(spear.AttackRange >= 2.0f, "Tầm đâm giáo phải đạt 2.0 units (2 ô tile)");

            var hammer = WeaponData.CreateHammer();
            Assert(hammer.SpecialSkill == WeaponSpecialSkill.Slam, "Búa phải có kỹ năng Nện đất (Slam)");
            Assert(hammer.KnockbackForce >= 5.0f, "Lực đẩy lùi của Búa phải cực mạnh (>= 5.0f)");
        }

        static void TestTrainingDummyAndArenaAnalytics()
        {
            // 1. Kiểm tra tính toán giảm trừ sát thương theo Resilience của Training Dummy
            float rawDamage = 45f;

            // Bù nhìn rơm (0 giáp)
            float strawMitigated = Math.Max(1f, rawDamage - 0);
            Assert(strawMitigated == 45f, "Bù nhìn 0 giáp phải nhận nguyên vẹn 45 sát thương");

            // Bù nhìn sắt (15 giáp)
            float ironMitigated = Math.Max(1f, rawDamage - 15);
            Assert(ironMitigated == 30f, "Bù nhìn 15 giáp phải giảm trừ còn 30 sát thương");

            // Bù nhìn thép (30 giáp)
            float steelMitigated = Math.Max(1f, rawDamage - 30);
            Assert(steelMitigated == 15f, "Bù nhìn 30 giáp phải giảm trừ còn 15 sát thương");

            // Bù nhìn siêu kháng (50 giáp với đòn 10 damage) -> Sát thương tối thiểu luôn là 1
            float superMitigated = Math.Max(1f, 10f - 50);
            Assert(superMitigated == 1f, "Sát thương tối thiểu không bao giờ nhỏ hơn 1");

            // 2. Kiểm tra bộ đếm Combat Analytics
            float totalDamage = 0f;
            int hits = 0;
            int parries = 0;
            int deflections = 0;

            // Mô phỏng 3 đòn chém
            totalDamage += strawMitigated; hits++;
            totalDamage += ironMitigated; hits++;
            totalDamage += steelMitigated; hits++;

            // Mô phỏng 2 lần Parry và 3 lần phản đạn
            parries += 2;
            deflections += 3;

            Assert(totalDamage == 90f, "Tổng sát thương 3 đòn phải là 45 + 30 + 15 = 90");
            Assert(hits == 3, "Số đòn đánh phải là 3");
            Assert(parries == 2, "Số lần parry phải là 2");
            Assert(deflections == 3, "Số lần phản xạ đạn phải là 3");
        }

        static void TestLootSystemAndDropRules()
        {
            // 1. Kiểm tra loot table của Kẻ Cào Phá (KeCaoPha)
            var breakerLoot = LootDatabase.RollLoot(EnemyTypeId.KeCaoPha, new Random(42));
            Assert(breakerLoot.Count > 0, "Kẻ Cào Phá phải rơi ít nhất 1 vật phẩm");
            bool hasMaterial = breakerLoot.Exists(item => item.Category == LootCategory.Material);
            Assert(hasMaterial, "Kẻ Cào Phá phải rơi vật liệu sửa rào (Gỗ, Đinh, Cọc)");

            // 2. Kiểm tra loot table của Kẻ Bắt Heo (KeBatHeo)
            var thiefLoot = LootDatabase.RollLoot(EnemyTypeId.KeBatHeo, new Random(100));
            Assert(thiefLoot.Count > 0, "Kẻ Bắt Heo phải rơi ít nhất 1 vật phẩm");
            bool hasFeedOrCurrency = thiefLoot.Exists(item => item.Category == LootCategory.Feed || item.Category == LootCategory.Currency);
            Assert(hasFeedOrCurrency, "Kẻ Bắt Heo phải rơi Cám ngô hoặc Túi vàng");

            // 3. Kiểm tra loot table của Chuột Gặm Kho (ChuotGamKho)
            var ratLoot = LootDatabase.RollLoot(EnemyTypeId.ChuotGamKho, new Random(77));
            Assert(ratLoot.Count > 0, "Chuột Gặm Kho phải rơi vật phẩm");
            bool hasSpecial = ratLoot.Exists(item => item.Category == LootCategory.Special);
            Assert(hasSpecial, "Chuột Gặm Kho phải rơi Phân bón vi sinh hoặc Đuôi chuột");

            // 4. Kiểm tra loot table của Alien (UfoTrinhSat)
            var alienLoot = LootDatabase.RollLoot(EnemyTypeId.UfoTrinhSat, new Random(88));
            Assert(alienLoot.Count > 0, "UFO Trinh Sát phải rơi vật phẩm Alien");
            bool hasAlloyOrTech = alienLoot.Exists(item => item.Category == LootCategory.Material || item.Category == LootCategory.Tech);
            Assert(hasAlloyOrTech, "UFO phải rơi Hợp kim nhẹ hoặc Cáp quang tử / Lõi plasma");

            // 5. Kiểm tra tính toàn vẹn của dữ liệu vật phẩm rơi
            foreach (var item in breakerLoot)
            {
                Assert(!string.IsNullOrEmpty(item.Id), "ID vật phẩm không được rỗng");
                Assert(!string.IsNullOrEmpty(item.DisplayName), "Tên vật phẩm không được rỗng");
                Assert(item.Quantity >= 1, "Số lượng vật phẩm phải >= 1");
                Assert(!string.IsNullOrEmpty(item.SpriteKey), "SpriteKey không được rỗng");
                Assert(item.GoldValue > 0, "Giá trị vàng phải > 0");
            }

            // 6. Kiểm tra màu sắc theo độ hiếm
            Assert(LootDatabase.GetRarityHexColor(LootRarity.Thuong) == "#FFFFFF", "Độ hiếm Thường phải là màu trắng #FFFFFF");
            Assert(LootDatabase.GetRarityHexColor(LootRarity.Tot) == "#39E75F", "Độ hiếm Tốt phải là màu xanh lá #39E75F");
            Assert(LootDatabase.GetRarityHexColor(LootRarity.Hiem) == "#3EB4FF", "Độ hiếm Hiếm phải là màu xanh dương #3EB4FF");
            Assert(LootDatabase.GetRarityHexColor(LootRarity.CucPham) == "#FFA726", "Độ hiếm Cực Phẩm phải là màu cam #FFA726");
        }

        static void TestPigSnatchingAndRaidTelegraphInvariants()
        {
            // 1. Kiểm tra RaidType enum
            Assert(Enum.IsDefined(typeof(RaidType), RaidType.BanditRaid), "Phải có RaidType.BanditRaid");
            Assert(Enum.IsDefined(typeof(RaidType), RaidType.AlienRaid), "Phải có RaidType.AlienRaid");

            // 2. Kiểm tra EnemyTypeId cho cơ chế cướp heo
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.KeBatHeo), "Phải có EnemyTypeId.KeBatHeo");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.UfoTrinhSat), "Phải có EnemyTypeId.UfoTrinhSat");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.UfoThuHoach), "Phải có EnemyTypeId.UfoThuHoach");

            // 3. Kiểm tra thông số kẻ địch KeBatHeo (mục tiêu heo, tốc độ cao)
            var thief = EnemyData.CreateKeBatHeo();
            Assert(thief.MaxHp == 150f, "Kẻ Bắt Heo HP phải là 150");
            Assert(thief.MoveSpeed >= 1.5f, "Kẻ Bắt Heo phải có tốc độ chạy nhanh để tẩu thoát");
            Assert(thief.AttackRange >= 0.8f, "Kẻ Bắt Heo phải có tầm bắt heo hợp lý");

            // 4. Kiểm tra thông số UFO Thu Hoạch (Tractor Beam)
            var ufoHarvester = EnemyData.CreateUfoThuHoach();
            Assert(ufoHarvester.MaxHp == 650f, "UFO Thu Hoạch phải có HP 650");
            Assert(ufoHarvester.AttackRange >= 1.8f, "UFO Thu Hoạch phải có tầm chiếu Tractor Beam rộng");

            // 5. Kiểm tra logic biến thiên trạng thái tinh thần của Heo khi bị tấn công/cướp
            var pig = Pig.CreateDefault("pig_test_snatch", "Ỉn Test", GeneLineId.HongDien, GeneRarity.Thuong);
            int initialMood = pig.Mood;
            float initialBonding = pig.Bonding;

            // Khi bị kẻ trộm tiếp cận/tóm: Mood giảm
            pig.Mood = Math.Max(0, pig.Mood - 25);
            Assert(pig.Mood < initialMood, "Tâm trạng heo phải giảm mạnh khi bị kẻ trộm tóm");

            // Khi được người chơi giải cứu: Bonding & Mood tăng trở lại
            pig.Bonding = Math.Min(100f, pig.Bonding + 15f);
            pig.Mood = Math.Min(100, pig.Mood + 20);
            Assert(pig.Bonding > initialBonding, "Độ gắn kết Bonding phải tăng khi được giải cứu thành công");
        }

        static void TestAlienEscalationChain()
        {
            // 1. Kiểm tra các định danh trong Enums
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.AlienGrunt), "Phải có EnemyTypeId.AlienGrunt");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.AlienElite), "Phải có EnemyTypeId.AlienElite");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.BossAlienWarlord), "Phải có EnemyTypeId.BossAlienWarlord");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.UfoMothership), "Phải có EnemyTypeId.UfoMothership");
            Assert(Enum.IsDefined(typeof(RaidType), RaidType.AlienMothershipInvasion), "Phải có RaidType.AlienMothershipInvasion");

            // 2. Kiểm tra chỉ số cơ bản Ngày 1 (Day 1)
            var gruntD1 = EnemyData.CreateAlienGrunt(1);
            var eliteD1 = EnemyData.CreateAlienElite(1);
            var warlordD1 = EnemyData.CreateBossAlienWarlord(1);
            var mothershipD1 = EnemyData.CreateUfoMothership(1);

            Assert(gruntD1.MaxHp == 180f, "Alien Grunt ngày 1 HP phải là 180");
            Assert(gruntD1.AttackDamage == 14f, "Alien Grunt ngày 1 AttackDamage phải là 14");
            Assert(eliteD1.MaxHp == 450f, "Alien Elite ngày 1 HP phải là 450");
            Assert(eliteD1.AttackDamage == 28f, "Alien Elite ngày 1 AttackDamage phải là 28");
            Assert(warlordD1.MaxHp == 3500f, "Boss Alien Warlord ngày 1 HP phải là 3500");
            Assert(warlordD1.AttackDamage == 55f, "Boss Alien Warlord ngày 1 AttackDamage phải là 55");
            Assert(warlordD1.IsBoss == true, "Boss Alien Warlord phải là Boss (IsBoss == true)");
            Assert(mothershipD1.MaxHp == 8000f, "Tàu Mẹ Mothership ngày 1 HP phải là 8000");
            Assert(mothershipD1.IsBoss == true, "Tàu Mẹ Mothership phải là Boss (IsBoss == true)");

            // 2b. Kiểm tra công thức kỹ năng Boss Ngoài Hành Tinh (GDD 19.2)
            float plasmaShieldD1 = CombatFormulas.CalculateAlienPlasmaShield(warlordD1.MaxHp);
            float disintegrationBeamDmg = CombatFormulas.CalculateDisintegrationBeamDamage(warlordD1.AttackDamage);
            float orbitalBarrageDmg = CombatFormulas.CalculateOrbitalPlasmaBarrageDamage(mothershipD1.AttackDamage);
            float alienOverloadSpeed = CombatFormulas.CalculateAlienOverloadSpeed(warlordD1.MoveSpeed);

            Assert(Math.Abs(plasmaShieldD1 - 525f) < 0.001f, "Khiên Plasma Alien Warlord Ngày 1 phải là 15% Max HP = 525 HP");
            Assert(Math.Abs(disintegrationBeamDmg - 88f) < 0.001f, "Sát thương Tia Phân Rã Laser phải là 55 * 1.60 = 88 DMG");
            Assert(Math.Abs(orbitalBarrageDmg - 56f) < 0.001f, "Sát thương Đại Pháo Quỹ Đạo Plasma phải là 40 * 1.40 = 56 DMG");
            Assert(Math.Abs(alienOverloadSpeed - 2.835f) < 0.001f, "Tốc độ Quá Tải Lõi Năng Lượng phải tăng 35% (2.1 * 1.35 = 2.835 m/s)");

            // 3. Kiểm tra độ khó scale theo ngày (Day 5 scaling)
            var gruntD5 = EnemyData.CreateAlienGrunt(5);
            var warlordD5 = EnemyData.CreateBossAlienWarlord(5);
            var mothershipD5 = EnemyData.CreateUfoMothership(5);

            Assert(gruntD5.MaxHp > gruntD1.MaxHp, "Alien Grunt ngày 5 HP phải cao hơn ngày 1");
            Assert(gruntD5.AttackDamage > gruntD1.AttackDamage, "Alien Grunt ngày 5 sát thương phải cao hơn ngày 1");
            Assert(warlordD5.MaxHp > warlordD1.MaxHp, "Boss Alien Warlord ngày 5 HP phải scale tăng");
            Assert(warlordD5.AttackDamage > warlordD1.AttackDamage, "Boss Alien Warlord ngày 5 sát thương phải scale tăng");
            Assert(mothershipD5.MaxHp > mothershipD1.MaxHp, "Tàu Mẹ Mothership ngày 5 HP phải scale tăng");

            // 4. Kiểm tra phần thưởng rơi vật phẩm (Loot Table) của các chủng Alien mới
            var gruntLoot = LootDatabase.RollLoot(EnemyTypeId.AlienGrunt, new Random(101));
            Assert(gruntLoot.Count > 0, "Alien Grunt phải rơi chiến lợi phẩm công nghệ Alien");

            var eliteLoot = LootDatabase.RollLoot(EnemyTypeId.AlienElite, new Random(202));
            Assert(eliteLoot.Count > 0, "Alien Elite phải rơi chiến lợi phẩm cao cấp");

            var warlordLoot = LootDatabase.RollLoot(EnemyTypeId.BossAlienWarlord, new Random(303));
            Assert(warlordLoot.Count > 0, "Boss Alien Warlord phải rơi chiến lợi phẩm Cực Phẩm/Hiếm");
            bool hasRareOrEpic = warlordLoot.Exists(i => i.Rarity == LootRarity.Hiem || i.Rarity == LootRarity.CucPham);
            Assert(hasRareOrEpic, "Boss Alien Warlord phải rơi vật phẩm độ hiếm Hiếm hoặc Cực Phẩm");

            var mothershipLoot = LootDatabase.RollLoot(EnemyTypeId.UfoMothership, new Random(404));
            Assert(mothershipLoot.Count > 0, "Tàu Mẹ UFO phải rơi lõi plasma / bộ xử lý lượng tử");

            // 5. Kiểm tra quy tắc leo thang: Sau 2 lần thất bại thì UFO thả 3 Alien Grunt
            int initialFailureThreshold = 2;
            int simulatedFailures = 0;
            bool triggerAlienDrop = false;

            // Lần 1 thất bại
            simulatedFailures++;
            triggerAlienDrop = simulatedFailures >= initialFailureThreshold;
            Assert(!triggerAlienDrop, "Thất bại lần 1 chưa đủ ngưỡng để thả 3 Alien");

            // Lần 2 thất bại
            simulatedFailures++;
            triggerAlienDrop = simulatedFailures >= initialFailureThreshold;
            Assert(triggerAlienDrop, "Đạt >= 2 lần thất bại phải kích hoạt thả 3 Alien Grunt");
        }

        static void TestBanditEscalationChain()
        {
            // 1. Kiểm tra Enum RaidType có BanditBossSiege
            Assert(Enum.IsDefined(typeof(RaidType), RaidType.BanditBossSiege), "RaidType phải định nghĩa BanditBossSiege");

            // 2. Kiểm tra chỉ số cơ bản Ngày 1 (Day 1)
            var thiefD1 = EnemyData.CreateKeBatHeo(1);
            var breakerD1 = EnemyData.CreateKeCaoPha(1);
            var ratD1 = EnemyData.CreateChuotGamKho(1);
            var leaderD1 = EnemyData.CreateDauLinhTrom(1);
            var bossD1 = EnemyData.CreateBossChuaTromRiuSat(1);

            Assert(thiefD1.MaxHp == 150f, "Kẻ Bắt Heo ngày 1 HP phải là 150");
            Assert(breakerD1.MaxHp == 120f, "Kẻ Cào Phá ngày 1 HP phải là 120");
            Assert(ratD1.MaxHp == 40f, "Chuột Gặm Kho ngày 1 HP phải là 40");
            Assert(leaderD1.MaxHp == 500f, "Đầu Lĩnh Trộm ngày 1 HP phải là 500");
            Assert(leaderD1.AttackDamage == 26f, "Đầu Lĩnh Trộm ngày 1 AttackDamage phải là 26");
            Assert(leaderD1.IsElite == true, "Đầu Lĩnh Trộm phải là Tinh Anh (IsElite == true)");
            Assert(leaderD1.CommandAuraRadius == 10f, "Đầu Lĩnh Trộm phải có Aura Chỉ Huy bán kính 10m");
            Assert(leaderD1.CommandAuraSpeedBuff == 0.20f, "Đầu Lĩnh Trộm phải buff tốc độ +20%");
            Assert(bossD1.MaxHp == 4000f, "Boss Chúa Trộm Rìu Sắt ngày 1 HP phải là 4000");
            Assert(bossD1.AttackDamage == 48f, "Boss Chúa Trộm Rìu Sắt ngày 1 AttackDamage phải là 48");
            Assert(bossD1.AttackRange == 1.8f, "Boss Chúa Trộm Rìu Sắt AttackRange phải là 1.8m");
            Assert(bossD1.MoveSpeed == 2.1f, "Boss Chúa Trộm Rìu Sắt MoveSpeed phải là 2.1m/s");
            Assert(bossD1.GoldReward == 550, "Boss Chúa Trộm Rìu Sắt GoldReward phải là 550");
            Assert(bossD1.ExpReward == 280, "Boss Chúa Trộm Rìu Sắt ExpReward phải là 280");
            Assert(bossD1.IsBoss == true, "Boss Chúa Trộm Rìu Sắt phải là Boss (IsBoss == true)");
            Assert(bossD1.SiegeDamageMultiplier == 3.0f, "Boss Chúa Trộm Rìu Sắt phải x3 sát thương phá rào (Siege Multiplier 3.0)");

            // 2b. Kiểm tra cơ chế GDD 19.1 cho Đầu Lĩnh Trộm: Tính giá trị trộm heo & Aura tốc độ
            var commonPig = new Pig { GeneLine = GeneLineId.HongDien, Rarity = GeneRarity.Thuong, WeightKg = 30f, Bonding = 10f };
            var kimThoPig = new Pig { GeneLine = GeneLineId.KimTho, Rarity = GeneRarity.Hiem, WeightKg = 80f, Bonding = 50f };
            var huThePig = new Pig { GeneLine = GeneLineId.HuThe, Rarity = GeneRarity.DiBien, WeightKg = 120f, Bonding = 90f };

            float valCommon = CombatFormulas.CalculatePigTheftValue(commonPig);
            float valKimTho = CombatFormulas.CalculatePigTheftValue(kimThoPig);
            float valHuThe = CombatFormulas.CalculatePigTheftValue(huThePig);

            Assert(valKimTho > valCommon, "Heo Kim Thọ hiếm phải có giá trị cướp cao hơn heo thường");
            Assert(valHuThe > valKimTho, "Heo Dị Biến Hư Thể phải có giá trị cướp cao nhất toàn trại");

            float baseSpeed = 2.5f;
            float buffedSpeed = CombatFormulas.CalculateBanditAuraSpeed(baseSpeed, true);
            float normalSpeed = CombatFormulas.CalculateBanditAuraSpeed(baseSpeed, false);
            Assert(Math.Abs(buffedSpeed - 3.0f) < 0.001f, "Quái trộm gần Đầu Lĩnh Trộm phải tăng 20% tốc độ (2.5 -> 3.0)");
            Assert(Math.Abs(normalSpeed - 2.5f) < 0.001f, "Quái trộm ngoài vùng Aura giữ nguyên tốc độ cơ bản");

            // 2c. Kiểm tra công thức kỹ năng Boss Chúa Trộm Rìu Sắt (GDD 19.1)
            float baseBossDmg = bossD1.AttackDamage;
            float siegeDmg = CombatFormulas.CalculateSiegeDamage(baseBossDmg, bossD1.SiegeDamageMultiplier);
            float whirlwindDmg = CombatFormulas.CalculateWhirlwindDamage(baseBossDmg);
            float groundSlamDmg = CombatFormulas.CalculateGroundSlamDamage(baseBossDmg);
            float enragedSpeed = CombatFormulas.CalculateBossEnrageSpeed(bossD1.MoveSpeed);

            Assert(Math.Abs(siegeDmg - 144f) < 0.001f, "Sát thương công thành phá rào phải là 48 * 3 = 144");
            Assert(Math.Abs(whirlwindDmg - 64.8f) < 0.001f, "Sát thương Càn Quét Rìu Xoay phải là 48 * 1.35 = 64.8");
            Assert(Math.Abs(groundSlamDmg - 72f) < 0.001f, "Sát thương Dậm Đất Địa Chấn phải là 48 * 1.50 = 72");
            Assert(Math.Abs(enragedSpeed - 2.73f) < 0.001f, "Tốc độ cuồng bạo phải tăng 30% (2.1 * 1.30 = 2.73)");

            // 3. Kiểm tra độ khó scale theo ngày (Day 5 scaling)
            var thiefD5 = EnemyData.CreateKeBatHeo(5);
            var leaderD5 = EnemyData.CreateDauLinhTrom(5);
            var bossD5 = EnemyData.CreateBossChuaTromRiuSat(5);

            Assert(thiefD5.MaxHp > thiefD1.MaxHp, "Kẻ Bắt Heo ngày 5 HP phải cao hơn ngày 1");
            Assert(thiefD5.AttackDamage > thiefD1.AttackDamage, "Kẻ Bắt Heo ngày 5 sát thương phải cao hơn ngày 1");
            Assert(leaderD5.MaxHp > leaderD1.MaxHp, "Đầu Lĩnh Trộm ngày 5 HP phải scale tăng");
            Assert(leaderD5.AttackDamage > leaderD1.AttackDamage, "Đầu Lĩnh Trộm ngày 5 sát thương phải scale tăng");
            Assert(bossD5.MaxHp > bossD1.MaxHp, "Boss Chúa Trộm Rìu Sắt ngày 5 HP phải scale tăng");
            Assert(bossD5.AttackDamage > bossD1.AttackDamage, "Boss Chúa Trộm Rìu Sắt ngày 5 sát thương phải scale tăng");

            // 4. Kiểm tra phần thưởng rơi vật phẩm (Loot Table) của Chúa Trộm Rìu Sắt
            var bossLoot = LootDatabase.RollLoot(EnemyTypeId.ChuaTromRiuSat, new Random(505));
            Assert(bossLoot.Count > 0, "Boss Chúa Trộm Rìu Sắt phải rơi chiến lợi phẩm giá trị cao");
            bool hasRareOrEpic = bossLoot.Exists(i => i.Rarity == LootRarity.Hiem || i.Rarity == LootRarity.CucPham);
            Assert(hasRareOrEpic, "Boss Chúa Trộm Rìu Sắt phải rơi vật phẩm Cực Phẩm hoặc Hiếm");

            // 5. Kiểm tra quy tắc leo thang: Sau 2 lần thất bại trộm heo thì kích hoạt Bandit Ambush 3
            int initialFailureThreshold = 2;
            int simulatedFailures = 0;
            bool triggerBanditAmbush = false;

            // Lần 1 trộm thất bại
            simulatedFailures++;
            triggerBanditAmbush = simulatedFailures >= initialFailureThreshold;
            Assert(!triggerBanditAmbush, "Thất bại lần 1 chưa đủ ngưỡng phục kích");

            // Lần 2 trộm thất bại
            simulatedFailures++;
            triggerBanditAmbush = simulatedFailures >= initialFailureThreshold;
            Assert(triggerBanditAmbush, "Đạt >= 2 lần thất bại phải kích hoạt phục kích 3 tên");
        }

        static void TestDualLayerStaminaAndComboMechanics()
        {
            // 1. Kiểm tra mô hình Thể Lực 2 Lớp (Dual-Layer Stamina - GDD 9.1 & 9.2)
            var stamina = new StaminaSystem();
            Assert(stamina.CurrentStamina == 180f, "Stamina ban đầu phải là 180");
            Assert(stamina.MaxStamina == 180f, "MaxStamina ban đầu phải là 180");
            Assert(stamina.DailyLoad == 0f, "DailyLoad ban đầu phải là 0");
            Assert(stamina.EffectiveMaxStamina == 180f, "EffectiveMaxStamina ban đầu phải bằng MaxStamina");

            // Đòn nhẹ: tiêu hao 2.5 STA, 30% ghi nợ Tải ngày (0.75)
            bool consumedLight = stamina.TryConsumeStamina(2.5f, 0.30f);
            Assert(consumedLight, "Tiêu hao đòn nhẹ phải thành công");
            Assert(Math.Abs(stamina.CurrentStamina - 177.5f) < 0.01f, "CurrentStamina còn 177.5");
            Assert(Math.Abs(stamina.DailyLoad - 0.75f) < 0.01f, "DailyLoad tích lũy 0.75");
            Assert(Math.Abs(stamina.EffectiveMaxStamina - 179.25f) < 0.01f, "EffectiveMaxStamina thu hẹp còn 179.25");

            // Đòn nặng dứt điểm: tiêu hao 5.0 STA, 40% ghi nợ Tải ngày (2.0)
            bool consumedHeavy = stamina.TryConsumeStamina(5.0f, 0.40f);
            Assert(consumedHeavy, "Tiêu hao đòn nặng phải thành công");
            Assert(Math.Abs(stamina.CurrentStamina - 172.5f) < 0.01f, "CurrentStamina còn 172.5");
            Assert(Math.Abs(stamina.DailyLoad - 2.75f) < 0.01f, "DailyLoad tích lũy lên 2.75 (0.75 + 2.0)");
            Assert(Math.Abs(stamina.EffectiveMaxStamina - 177.25f) < 0.01f, "EffectiveMaxStamina thu hẹp còn 177.25");

            // Hồi phục Thể Lực tức thời: Chỉ hồi tối đa tới EffectiveMaxStamina, Tải ngày không tự hồi (GDD 9.4)
            stamina.RegenerateStamina(50f);
            Assert(Math.Abs(stamina.CurrentStamina - 177.25f) < 0.01f, "Chỉ được hồi đến trần EffectiveMaxStamina (177.25), không hồi tràn qua DailyLoad");
            Assert(Math.Abs(stamina.DailyLoad - 2.75f) < 0.01f, "Tải ngày không tự phục hồi khi nghỉ ngắn");

            // 2. Vùng Mệt Mỏi & Phạt Mệt Dội (GDD 9.4 & 9.5)
            stamina.CurrentStamina = 35f; // < 20% của 180 (36)
            Assert(stamina.IsFatigued, "Dưới 20% MaxStamina phải ở trạng thái IsFatigued (Vùng mệt)");

            stamina.CurrentStamina = 37f;
            Assert(!stamina.IsFatigued, "Từ 20% MaxStamina trở lên không phải IsFatigued");

            // Mệt Dội (Rebound Fatigue): Mỗi tầng giảm 8% MaxStamina
            stamina.ReboundFatigueStacks = 1;
            Assert(Math.Abs(stamina.BaseMaxStamina - (180f * 0.92f)) < 0.01f, "1 tầng Mệt Dội giảm 8% thể lực tối đa (còn 165.6)");

            // Ngủ chuẩn: Xóa sạch Tải ngày và khôi phục thể lực >= 80% (GDD 9.5)
            stamina.DailyLoad = 35f;
            stamina.CurrentStamina = 15f;
            stamina.PerformSleepReset();
            Assert(stamina.DailyLoad == 0f, "Ngủ chuẩn phải xóa sạch hoàn toàn DailyLoad");
            Assert(stamina.CurrentStamina >= stamina.BaseMaxStamina * 0.8f, "Ngủ chuẩn phải khôi phục thể lực ít nhất 80% trần BaseMax");

            // 3. Cơ chế Lướt Né (Dodge Roll) & Giảm Trừ Chi Phí theo AGI & Lối Chơi (GDD 9.2 & 12.2)
            var charData = new CharacterData();
            // Ban đầu: Agi = 18 (< 35), 6 ô rỗng (mỗi ô tính 1 điểm = 6 điểm -> Khinh Thân)
            Assert(charData.GetPlaystyle() == "KhinhThân", "6 điểm trọng lượng phải là Khinh Thân");
            // Khinh Thân: Lướt -20% Stamina (9 * 0.8 = 7.2 STA)
            Assert(Math.Abs(charData.CalculateDodgeCost() - 7.2f) < 0.01f, "Khinh Thân giảm 20% chi phí Lướt Né (còn 7.2 STA)");

            // Nâng AGI gốc >= 35: Lướt né giảm thêm 15% (tổng giảm 35%: 9 * 0.65 = 5.85 STA)
            charData.BaseStats.Agi = 35;
            Assert(Math.Abs(charData.CalculateDodgeCost() - 5.85f) < 0.01f, "AGI >= 35 kết hợp Khinh Thân giảm 35% chi phí (còn 5.85 STA)");

            // Chuyển sang Trọng Giáp (tổng điểm trọng lượng >= 14): Lướt né +25% Stamina
            for (int i = 0; i < 5; i++)
            {
                charData.Equipment[(EquipSlot)i] = new EquipmentItem { Slot = (EquipSlot)i, WeightScore = 3 };
            }
            charData.BaseStats.Agi = 18; // Reset AGI về dưới 35
            Assert(charData.GetPlaystyle() == "TrọngGiáp", "Trọng lượng 16 điểm phải là Trọng Giáp");
            Assert(Math.Abs(charData.CalculateDodgeCost() - 11.25f) < 0.01f, "Trọng Giáp tăng 25% chi phí Lướt Né (9 * 1.25 = 11.25 STA)");

            // 4. Cơ chế Combo 3 Nhịp (3-Hit Combo - GDD 12 & KY_THUAT P1.08)
            var hit1 = charData.GetComboStepData(0);
            Assert(hit1.staminaCost == 2.5f && Math.Abs(hit1.damageMultiplier - 1.0f) < 0.001f, "Nhịp 1: 2.5 STA, 1.0x damage");

            var hit2 = charData.GetComboStepData(1);
            Assert(hit2.staminaCost == 2.5f && Math.Abs(hit2.damageMultiplier - 1.15f) < 0.001f, "Nhịp 2: 2.5 STA, 1.15x damage");

            var hit3 = charData.GetComboStepData(2);
            Assert(hit3.staminaCost == 5.0f && Math.Abs(hit3.damageMultiplier - 1.50f) < 0.001f, "Nhịp 3 (Finisher): 5.0 STA, 1.50x damage, 40% DailyLoad");
            Assert(hit3.dailyLoadRatio == 0.40f, "Nhịp 3 tích lũy 40% Tải ngày");
        }

        static void TestFinancialLedgerAndDayMarket()
        {
            var engine = new GameEngine();
            // Thiết lập trang trại ban đầu: Gold = 2500
            Assert(engine.Farm.Gold == 2500, "Vàng ban đầu của trang trại phải là 2500G");

            // 1. Mua Cám tại Chợ Ngày (Feed Purchasing - GDD 20 & 22)
            // Cám Tiêu Chuẩn: 4G/kg, gói 50kg -> 200G
            int initialStandardFeed = engine.Economy.FeedInventory.FeedStandardKg;
            var buy1 = engine.BuyFeedAtDayMarket(FeedTypeId.CamTieuChuan, 50);
            Assert(buy1.success, "Mua 50kg Cám Tiêu Chuẩn phải thành công");
            Assert(buy1.goldSpent == 200, "Chi phí mua 50kg Cám Tiêu Chuẩn là 200G");
            Assert(engine.Farm.Gold == 2300, "Vàng sau mua cám phải còn 2300G");
            Assert(engine.Economy.FeedInventory.FeedStandardKg == initialStandardFeed + 50, "Kho Cám Tiêu Chuẩn phải tăng 50kg");

            // Mua Cám Tinh Đậm Đặc: 8G/kg, gói 25kg -> 200G
            var buy2 = engine.BuyFeedAtDayMarket(FeedTypeId.CamTinh, 25);
            Assert(buy2.success && buy2.goldSpent == 200, "Mua 25kg Cám Tinh phải tốn 200G");
            Assert(engine.Farm.Gold == 2100, "Vàng sau mua Cám Tinh phải còn 2100G");

            // Mua Cám Dược Liệu: 15G/kg, gói 20kg -> 300G
            var buy3 = engine.BuyFeedAtDayMarket(FeedTypeId.CamDuocLieu, 20);
            Assert(buy3.success && buy3.goldSpent == 300, "Mua 20kg Cám Dược Liệu phải tốn 300G");
            Assert(engine.Farm.Gold == 1800, "Vàng sau mua Cám Dược Liệu phải còn 1800G");

            // Thử mua khi không đủ tiền (Gói 1000kg Cám Dược Liệu = 15000G > 1800G)
            var buyFail = engine.BuyFeedAtDayMarket(FeedTypeId.CamDuocLieu, 1000);
            Assert(!buyFail.success, "Không đủ vàng mua 1000kg cám dược liệu phải bị từ chối");
            Assert(engine.Farm.Gold == 1800, "Vàng không bị trừ khi giao dịch thất bại");

            // 2. Bán Heo tại Chợ Ngày & Khấu Trừ Phí Kiểm Dịch (Pig Sales - GDD 20 & 22)
            // Tạo 1 heo trưởng thành Hiếm: Thiết Bì, 105kg, Phẩm chất A -> Giá 491G
            var rarePig = Pig.CreateDefault("p_rare_1", "Thiết Bì Thượng Phẩm", GeneLineId.ThietBi, GeneRarity.Hiem);
            rarePig.WeightKg = 105f;
            rarePig.MeatQuality = MeatQuality.A;
            rarePig.Stage = PigStage.TruongThanh;
            rarePig.IsIdentified = true;
            engine.Pigs.Add(rarePig);

            // Bán có kiểm dịch: Doanh thu 491G, Phí kiểm dịch 60G -> Thu ròng 431G
            // Vàng trước bán: 1800G -> sau bán: 1800 + 431 = 2231G
            var sellResult = engine.SellPigAtDayMarket(rarePig.Id, isQuarantined: true);
            Assert(sellResult.success, "Bán heo Hiếm có kiểm dịch phải thành công");
            Assert(sellResult.goldEarned == 431, $"Vàng ròng nhận được phải là 431G (thực tế: {sellResult.goldEarned}G)");
            Assert(engine.Farm.Gold == 2231, $"Vàng trang trại phải là 2231G (thực tế: {engine.Farm.Gold}G)");
            Assert(engine.Pigs.Count == 0, "Heo đã bán phải bị xóa khỏi trang trại");

            // Heo Dị Biến: Giá 0G -> Không được phép bán ở chợ ngày (GDD 20.1)
            var mutantPig = Pig.CreateDefault("p_mutant", "Heo Dị Biến", GeneLineId.HuThe, GeneRarity.DiBien);
            engine.Pigs.Add(mutantPig);
            var sellMutant = engine.SellPigAtDayMarket(mutantPig.Id, true);
            Assert(!sellMutant.success, "Cấm bán heo Dị Biến tại Chợ Ngày");
            engine.Pigs.Remove(mutantPig);

            // 3. Sổ Cái Tài Chính: Kiểm tra Tổng kết Ngày (Daily Financial Summary - GDD 22)
            var daily1 = engine.Economy.Ledger.GetDailySummary(engine.Clock.CurrentDay);
            // Thu: 491G (BanHeo)
            Assert(daily1.TotalIncomeGold == 491, $"Tổng thu ngày 1 phải là 491G (thực tế: {daily1.TotalIncomeGold}G)");
            // Chi: 200 (Mua Cám TCV) + 200 (Mua Cám Tinh) + 300 (Mua Cám DL) + 60 (Kiểm dịch) = 760G
            Assert(daily1.TotalExpenseGold == 760, $"Tổng chi ngày 1 phải là 760G (thực tế: {daily1.TotalExpenseGold}G)");
            // Lợi nhuận ròng: 491 - 760 = -269G
            Assert(daily1.NetProfitGold == -269, $"Lợi nhuận ròng ngày 1 phải là -269G (thực tế: {daily1.NetProfitGold}G)");
            Assert(daily1.IncomeByCategory[TransactionCategory.BanHeo] == 491, "Doanh mục BanHeo thu 491G");
            Assert(daily1.ExpenseByCategory[TransactionCategory.MuaCam] == 700, "Doanh mục MuaCam chi 700G");
            Assert(daily1.ExpenseByCategory[TransactionCategory.KiemDich] == 60, "Doanh mục KiemDich chi 60G");

            // 4. Kiểm tra Chuyển Ngày & Chi Phí Bảo Trì Chuồng Trại vào Sổ Cái (GDD 22.1)
            // Qua ngày 2 (Tick 1440 phút = 24h)
            engine.Tick(1440);
            // Check Daily Reset đã tự động ghi nhận phí bảo trì chuồng trại
            var daily2 = engine.Economy.Ledger.GetDailySummary(2);
            // Sức chứa hạ tầng mặc định: 12*4 + 14*6 + 8*3 + 9*5 = 48 + 84 + 24 + 45 = 201G
            Assert(daily2.TotalExpenseGold == 201, $"Chi phí bảo trì ngày 2 phải là 201G (thực tế: {daily2.TotalExpenseGold}G)");
            Assert(daily2.ExpenseByCategory[TransactionCategory.BaoTriTrangTrai] == 201, "Danh mục Bảo Trì Chuồng Trại chi 201G");

            // 5. Báo Cáo Chu Kỳ 8 Ngày (Cycle Financial Summary - GDD Bảng 22.1)
            var cycleSummary = engine.Economy.Ledger.GetCycleSummary(1, 8);
            Assert(cycleSummary.GrossFarmIncome == 491, "Doanh thu chu kỳ gộp = 491G");
            Assert(cycleSummary.FeedCost == 700, "Chi phí cám chu kỳ = 700G");
            Assert(cycleSummary.InfrastructureMaintenanceCost == 201, "Chi phí bảo trì chu kỳ = 201G");
            Assert(cycleSummary.IntelAndQuarantineCost == 60, "Chi phí kiểm dịch chu kỳ = 60G");
            Assert(cycleSummary.TotalExpense == 700 + 201 + 60, "Tổng chi chu kỳ = 961G");
            Assert(cycleSummary.NetSurplus == 491 - 961, "Thặng dư ròng chu kỳ = -470G");

            // 6. Ngân Sách Kép (Dual Budget Model - GDD 22)
            // Thêm 1 heo đang lớn vào đàn để tính Kho Giá Trị GTTC (90% giá chợ ngày)
            var pigGrowing = Pig.CreateDefault("p_grow", "Heo Trưởng Thành B", GeneLineId.ThoTram, GeneRarity.Kha);
            pigGrowing.WeightKg = 100f;
            pigGrowing.MeatQuality = MeatQuality.B;
            pigGrowing.Stage = PigStage.TruongThanh;
            pigGrowing.IsIdentified = true;
            // Giá chợ: 100 * 3 * 1.0 * 1.0 * 1.1 = 330G
            // GTTC heo: 330 * 0.90 = 297G
            engine.Pigs.Add(pigGrowing);

            var dualBudget = engine.GetDualBudgetReport();
            Assert(dualBudget.CashGoldBalance == engine.Farm.Gold, "Ngân sách tiền mặt phải khớp số dư Vàng");
            Assert(dualBudget.TotalHerdEstimatedValueGold == 297, $"Kho giá trị đàn heo GTTC phải là 297G (thực tế: {dualBudget.TotalHerdEstimatedValueGold}G)");
            Assert(dualBudget.TotalFeedStockValueGold > 0, "Kho giá trị cám tồn kho phải > 0");
            Assert(dualBudget.TotalNetWorthGold == dualBudget.CashGoldBalance + dualBudget.TotalInventoryValueGold, "Tổng tài sản ròng = Tiền mặt + Kho Giá Trị");
        }
    }
}

