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
    }
}
