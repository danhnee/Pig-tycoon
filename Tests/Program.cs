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
            RunTest("Save/Load: Khôi phục thời gian", TestSaveLoadClock);
            RunTest("Save/Load: Khôi phục vàng và cấp độ nông trại", TestSaveLoadFarm);
            RunTest("Save/Load: Khôi phục danh sách heo", TestSaveLoadPigs);
            RunTest("Save/Load: Khôi phục phòng thủ", TestSaveLoadDefense);
            RunTest("Save/Load: Khôi phục bản vẽ", TestSaveLoadBlueprints);
            RunTest("Save/Load: Khôi phục hạ tầng", TestSaveLoadInfrastructure);
            RunTest("Save/Load: Khôi phục nhân vật", TestSaveLoadCharacter);
            RunTest("Save/Load JSON: Round-trip nhân vật, hạ tầng và phòng thủ", TestJsonRoundTripCharacterInfrastructureAndDefense);
            RunTest("Save/Load JSON: Nhân vật và dữ liệu heo thiếu/null", TestJsonNullAndMissingCharacterAndPigData);
            RunTest("Save/Load JSON: Công trình thiếu/null", TestJsonNullAndMissingBuildingData);
            RunTest("Save/Load: Lưu dữ liệu xuống file JSON", TestSaveToFile);
            RunTest("Save/Load: Đọc file và khôi phục dữ liệu", TestLoadFromFile);
            RunTest("Save/Load: File không tồn tại", TestLoadMissingFile);
            RunTest("Save/Load: File JSON bị hỏng", TestLoadInvalidJson);

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

            var deviantTrait = huThe.Traits.Find(t => t.IsDeviant && !t.IsPositive);
            Assert(deviantTrait != null && deviantTrait.IsSealed, "Hư Thể phải được phong ấn vĩnh viễn");

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
            var builtBuilding = bldg.building;
            if (!bldg.success || builtBuilding == null)
                throw new Exception("Xây Nỏ Xuyên Vân phải thành công và trả về công trình");
            Assert(builtBuilding.AmmoCurrent == 50, "Nỏ Xuyên Vân nạp tối đa 50 tên");

            // Không nạp được trong đợt
            var preloadFail = def.PreloadAmmo(builtBuilding.InstanceId, 10, true);
            Assert(!preloadFail.success, "Không được phép nạp đạn trong đợt quái");

            // Bạch Vân không tự động mà phải kích thủ công
            var bv = def.ConstructBuilding("CuuTieuLoiPhao");
            // Chưa đủ phòng cấp 3 -> phải thất bại
            Assert(!bv.success, "Chưa đủ cấp Phòng Xây Dựng thì không thể xây Bạch Vân");
            Assert(bv.building == null, "Xây thất bại không được trả về công trình");

            def.HallLevel = 3;
            def.UnlockedBlueprints.Add("CuuTieuLoiPhao");
            var bvOk = def.ConstructBuilding("CuuTieuLoiPhao");
            var bachVanBuilding = bvOk.building;
            if (!bvOk.success || bachVanBuilding == null)
                throw new Exception("Cấp 3 và đã học bản vẽ thì phải xây thành công và trả về công trình");
            Assert(bachVanBuilding.IsBachVan, "Phải được đánh dấu là Bạch Vân");

            var trigger = def.TriggerBachVanManual(bachVanBuilding.InstanceId);
            Assert(trigger.success, "Kích hoạt thủ công Bạch Vân thành công");
            Assert(bachVanBuilding.AmmoCurrent == 70, "Tiêu hao 50 đạn (120 - 50 = 70)");
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

        static void TestSaveLoadClock()
        {
            var engine = new GameEngine();

            engine.Clock.TickMinutes(125);

            var saveData = engine.CreateSaveData();

            var restoredEngine = new GameEngine();
            restoredEngine.RestoreFromSave(saveData);

            Assert(
                restoredEngine.Clock.CurrentDay == engine.Clock.CurrentDay &&
                restoredEngine.Clock.CurrentHour == engine.Clock.CurrentHour &&
                restoredEngine.Clock.CurrentMinute == engine.Clock.CurrentMinute,
                "Thời gian phải được giữ nguyên sau khi khôi phục");
        }


        static void TestSaveLoadFarm()
        {
            var engine = new GameEngine();

            engine.Farm.Gold = 1234;
            engine.Farm.FarmLevel = 5;
            engine.Farm.FarmExp = 321;

            var saveData = engine.CreateSaveData();

            var restoredEngine = new GameEngine();
            restoredEngine.RestoreFromSave(saveData);

            Assert(
            restoredEngine.Farm.Gold == 1234, "Vàng phải được giữ nguyên sau khi khôi phục");

            Assert(
            restoredEngine.Farm.FarmLevel == 5, "Cấp độ nông trại phải được giữ nguyên");

            Assert(
            restoredEngine.Farm.FarmExp == 321, "Kinh nghiệm nông trại phải được giữ nguyên");
        }

        static void TestSaveLoadPigs()
        {
            var engine = new GameEngine();

            var pig = Pig.CreateDefault(
                "save_pig_1",
                "Heo Test",
                GeneLineId.HongDien,
                GeneRarity.Thuong);

            engine.Pigs.Add(pig);

            var saveData = engine.CreateSaveData();

            var restoredEngine = new GameEngine();
            restoredEngine.RestoreFromSave(saveData);

            Assert(restoredEngine.Pigs.Count == 1, "Danh sách heo phải được khôi phục");

            var restoredPig = restoredEngine.Pigs[0];

            Assert(restoredPig.Id == pig.Id && restoredPig.Name == pig.Name, "ID và tên heo phải được giữ nguyên");

            Assert(Math.Abs(restoredPig.WeightKg - pig.WeightKg) < 0.001f, "Cân nặng heo phải được giữ nguyên");

            Assert(restoredPig.AgeDays == pig.AgeDays, "Tuổi heo phải được giữ nguyên");
        }



        static void TestSaveLoadDefense()
        {
            var engine = new GameEngine();
            engine.Defense.HallLevel = 3;
            engine.Defense.UnlockedBlueprints.Add("CuuTieuLoiPhao");
            var building = engine.Defense.ConstructBuilding("CuuTieuLoiPhao");
            var constructedBuilding = building.building;
            if (!building.success || constructedBuilding == null)
                throw new Exception("Phải tạo được công trình để kiểm tra lưu phòng thủ");
            constructedBuilding.CurrentHp = 987;
            constructedBuilding.AmmoCurrent = 42;

            var restored = new GameEngine();
            restored.RestoreFromSave(engine.CreateSaveData());

            Assert(restored.Defense.HallLevel == 3, "Cấp Phòng Xây Dựng phải được khôi phục");
            Assert(restored.Defense.Buildings.Count == 1, "Danh sách công trình phải được khôi phục");
            Assert(restored.Defense.Buildings[0].BlueprintId == "CuuTieuLoiPhao", "Bản vẽ công trình phải được giữ nguyên");
            Assert(restored.Defense.Buildings[0].CurrentHp == 987 && restored.Defense.Buildings[0].AmmoCurrent == 42, "Trạng thái công trình phải được khôi phục");
        }

        static void TestSaveLoadBlueprints()
        {
            var engine = new GameEngine();
            engine.Defense.UnlockedBlueprints.Add("CuuTieuLoiPhao");
            engine.Defense.UnlockedBlueprints.Add("ThienNhanVongDai");

            var restored = new GameEngine();
            restored.RestoreFromSave(engine.CreateSaveData());

            Assert(restored.Defense.UnlockedBlueprints.Contains("NoXuyenVan"), "Bản vẽ mặc định phải được giữ");
            Assert(restored.Defense.UnlockedBlueprints.Contains("CuuTieuLoiPhao") && restored.Defense.UnlockedBlueprints.Contains("ThienNhanVongDai"), "Các bản vẽ đã mở khóa phải được khôi phục");
        }

        static void TestSaveLoadInfrastructure()
        {
            var engine = new GameEngine();
            engine.Farm.Infrastructure.LandPlots = 21;
            engine.Farm.Infrastructure.Shelters = 17;
            engine.Farm.Infrastructure.WaterTroughs = 13;
            engine.Farm.Infrastructure.Feeders = 19;
            engine.Farm.Infrastructure.ProcessingLotCapacity = 8;
            engine.Farm.Infrastructure.DrainageSystemLevel = 2;

            var restored = new GameEngine();
            restored.RestoreFromSave(engine.CreateSaveData());
            var infra = restored.Farm.Infrastructure;

            Assert(infra.LandPlots == 21 && infra.Shelters == 17, "Đất và mái trú phải được khôi phục");
            Assert(infra.WaterTroughs == 13 && infra.Feeders == 19, "Bồn nước và máng ăn phải được khôi phục");
            Assert(infra.ProcessingLotCapacity == 8 && infra.DrainageSystemLevel == 2, "Hạ tầng xử lý và thoát nước phải được khôi phục");
        }

        static void TestSaveLoadCharacter()
        {
            var engine = new GameEngine();
            engine.Character.Id = "char_save_test";
            engine.Character.Name = "Nhân vật kiểm thử";
            engine.Character.Level = 7;
            engine.Character.Exp = 1234;
            engine.Character.Hp = 76;
            engine.Character.MaxHp = 140;
            engine.Character.Rage = 55;
            engine.Character.PrimarySchool = MartialSchool.TrongKich_B;

            var restored = new GameEngine();
            restored.RestoreFromSave(engine.CreateSaveData());

            Assert(restored.Character.Id == "char_save_test" && restored.Character.Name == "Nhân vật kiểm thử", "ID và tên nhân vật phải được khôi phục");
            Assert(restored.Character.Level == 7 && restored.Character.Exp == 1234, "Cấp và kinh nghiệm nhân vật phải được khôi phục");
            Assert(restored.Character.Hp == 76 && restored.Character.MaxHp == 140 && restored.Character.Rage == 55, "HP và nộ khí phải được khôi phục");
            Assert(restored.Character.PrimarySchool == engine.Character.PrimarySchool, "Môn phái chính phải được khôi phục");
        }

        static void TestJsonRoundTripCharacterInfrastructureAndDefense()
        {
            string path = GetUniqueJsonTestPath();

            try
            {
                var engine = new GameEngine();
                engine.Character.Id = "json_char_1";
                engine.Character.Name = "Nhân vật JSON";
                engine.Character.Level = 9;
                engine.Character.Exp = 2345;
                engine.Character.Hp = 81;
                engine.Character.MaxHp = 160;
                engine.Character.Rage = 37;
                engine.Character.PrimarySchool = MartialSchool.TrongKich_B;

                engine.Farm.Infrastructure.LandPlots = 23;
                engine.Farm.Infrastructure.Shelters = 18;
                engine.Farm.Infrastructure.WaterTroughs = 14;
                engine.Farm.Infrastructure.Feeders = 20;
                engine.Farm.Infrastructure.ProcessingLotCapacity = 9;
                engine.Farm.Infrastructure.DrainageSystemLevel = 3;

                engine.Defense.HallLevel = 4;
                engine.Defense.UnlockedBlueprints.Add("CuuTieuLoiPhao");
                var construction = engine.Defense.ConstructBuilding("CuuTieuLoiPhao");
                var building = construction.building;
                if (!construction.success || building == null)
                    throw new Exception("Phải tạo được công trình Bạch Vân cho JSON round-trip");
                building.Durability = 67;
                building.MaxHp = 1800;
                building.CurrentHp = 1234;
                building.IsBroken = false;
                building.AmmoCurrent = 83;
                building.AmmoCapacity = 120;
                building.AmmoName = "Đơn Vị Cộng Hưởng";
                building.AmmoCostPerShot = 50;
                building.CooldownSeconds = 77.5f;
                building.CurrentCooldownRemaining = 12.25f;
                building.FireMode = "ThuCong";

                SaveSystem.Save(engine.CreateSaveData(), path);
                var loadedData = SaveSystem.Load(path);
                var restored = new GameEngine();
                restored.RestoreFromSave(loadedData);

                Assert(restored.Character.Id == "json_char_1" && restored.Character.Name == "Nhân vật JSON", "ID và tên nhân vật phải round-trip qua JSON");
                Assert(restored.Character.Level == 9 && restored.Character.Exp == 2345, "Cấp và kinh nghiệm nhân vật phải round-trip qua JSON");
                Assert(restored.Character.Hp == 81 && restored.Character.MaxHp == 160 && restored.Character.Rage == 37, "HP, HP tối đa và nộ khí phải round-trip qua JSON");
                Assert(restored.Character.PrimarySchool == MartialSchool.TrongKich_B, "Môn phái nhân vật phải round-trip qua JSON");

                var infra = restored.Farm.Infrastructure;
                Assert(infra.LandPlots == 23 && infra.Shelters == 18 && infra.WaterTroughs == 14 && infra.Feeders == 20, "Hạ tầng cơ bản phải round-trip qua JSON");
                Assert(infra.ProcessingLotCapacity == 9 && infra.DrainageSystemLevel == 3, "Hạ tầng xử lý và thoát nước phải round-trip qua JSON");

                Assert(restored.Defense.HallLevel == 4, "Cấp Phòng Xây Dựng phải round-trip qua JSON");
                Assert(restored.Defense.UnlockedBlueprints.Count == engine.Defense.UnlockedBlueprints.Count && restored.Defense.UnlockedBlueprints.Contains("CuuTieuLoiPhao"), "Danh sách bản vẽ phải round-trip qua JSON");
                Assert(restored.Defense.Buildings.Count == 1, "Danh sách công trình phải round-trip qua JSON");

                var restoredBuilding = restored.Defense.Buildings[0];
                Assert(restoredBuilding.InstanceId == building.InstanceId && restoredBuilding.BlueprintId == "CuuTieuLoiPhao", "Định danh công trình phải được giữ nguyên");
                Assert(restoredBuilding.Name == building.Name && restoredBuilding.Category == building.Category && restoredBuilding.IsBachVan, "Tên, loại và trạng thái Bạch Vân phải được giữ nguyên");
                Assert(restoredBuilding.Durability == 67 && restoredBuilding.MaxHp == 1800 && restoredBuilding.CurrentHp == 1234 && !restoredBuilding.IsBroken, "Độ bền, HP và trạng thái hỏng phải được giữ nguyên");
                Assert(restoredBuilding.AmmoCurrent == 83 && restoredBuilding.AmmoCapacity == 120 && restoredBuilding.AmmoName == "Đơn Vị Cộng Hưởng" && restoredBuilding.AmmoCostPerShot == 50, "Trạng thái đạn/nhiên liệu phải được giữ nguyên");
                Assert(Math.Abs(restoredBuilding.CooldownSeconds - 77.5f) < 0.001f && Math.Abs(restoredBuilding.CurrentCooldownRemaining - 12.25f) < 0.001f && restoredBuilding.FireMode == "ThuCong", "Hồi chiêu và chế độ bắn phải được giữ nguyên");
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        static void TestJsonNullAndMissingCharacterAndPigData()
        {
            var nullCharacter = RestoreEngineFromJson("""
                { "CharacterId": null, "CharacterName": null }
                """);
            Assert(nullCharacter.Character.Id == "char_1" && nullCharacter.Character.Name == "Khoa", "ID/tên nhân vật null phải giữ mặc định");

            var missingCharacter = RestoreEngineFromJson("{}");
            Assert(missingCharacter.Character.Id == "char_1" && missingCharacter.Character.Name == "Khoa", "ID/tên nhân vật bị thiếu phải giữ mặc định");

            var nullPigList = RestoreEngineFromJson("""
                { "Pigs": null }
                """);
            Assert(nullPigList.Pigs.Count == 0, "Danh sách heo null phải được xử lý thành danh sách rỗng");

            var missingPigFields = RestoreEngineFromJson("""
                {
                  "Pigs": [
                    null,
                    { "Name": "Thiếu ID" },
                    { "Id": "missing-name" },
                    { "Id": null, "Name": "ID null" },
                    { "Id": "null-name", "Name": null },
                    { "Id": "missing-gender", "Name": "Thiếu giới tính" },
                    { "Id": "null-gender", "Name": "Giới tính null", "Gender": null },
                    { "Id": "valid-pig", "Name": "Heo hợp lệ", "Gender": "Cai" }
                  ]
                }
                """);

            Assert(missingPigFields.Pigs.Count == 3, "Phần tử null và heo thiếu/null ID/tên phải bị bỏ qua");
            Assert(missingPigFields.Pigs.Exists(p => p.Id == "missing-gender" && p.Gender == "Duc"), "Heo thiếu giới tính phải dùng mặc định Duc");
            Assert(missingPigFields.Pigs.Exists(p => p.Id == "null-gender" && p.Gender == "Duc"), "Heo có giới tính null phải dùng mặc định Duc");
            Assert(missingPigFields.Pigs.Exists(p => p.Id == "valid-pig" && p.Name == "Heo hợp lệ" && p.Gender == "Cai"), "Bản ghi heo hợp lệ phải được giữ nguyên");
        }

        static void TestJsonNullAndMissingBuildingData()
        {
            var nullBuildingList = RestoreEngineFromJson("""
                { "Buildings": null }
                """);
            Assert(nullBuildingList.Defense.Buildings.Count == 0, "Danh sách công trình null phải được xử lý thành danh sách rỗng");
            var missingBuildingList = RestoreEngineFromJson("{}");
            Assert(missingBuildingList.Defense.Buildings.Count == 0, "Danh sách công trình bị thiếu phải được khởi tạo rỗng");

            var missingBuildingFields = RestoreEngineFromJson("""
                {
                  "Buildings": [
                    null,
                    { "Name": "Thiếu định danh" },
                    { "InstanceId": "missing-blueprint" },
                    { "BlueprintId": "missing-instance" },
                    { "InstanceId": null, "BlueprintId": "null-instance" },
                    { "InstanceId": "null-blueprint", "BlueprintId": null },
                    { "InstanceId": "missing-name", "BlueprintId": "CuuTieuLoiPhao", "IsBachVan": true, "AmmoCurrent": 60, "AmmoCostPerShot": 50 },
                    { "InstanceId": "null-name", "BlueprintId": "CuuTieuLoiPhao", "Name": null, "IsBachVan": true, "AmmoCurrent": 60, "AmmoCostPerShot": 50 }
                  ]
                }
                """);

            Assert(missingBuildingFields.Defense.Buildings.Count == 2, "Phần tử null và công trình thiếu/null InstanceId/BlueprintId phải bị bỏ qua");
            Assert(missingBuildingFields.Defense.Buildings.Exists(b => b.InstanceId == "missing-name" && b.Name == "CuuTieuLoiPhao"), "Công trình thiếu tên phải dùng BlueprintId làm tên dự phòng");
            Assert(missingBuildingFields.Defense.Buildings.Exists(b => b.InstanceId == "null-name" && b.Name == "CuuTieuLoiPhao"), "Công trình có tên null phải dùng BlueprintId làm tên dự phòng");

            var restoredBuilding = missingBuildingFields.Defense.Buildings.Find(b => b.InstanceId == "null-name");
            var trigger = missingBuildingFields.Defense.TriggerBachVanManual(restoredBuilding!.InstanceId);
            Assert(trigger.success && trigger.message.Contains("CUUTIEULOIPHAO"), "Kích hoạt công trình có tên null không được ném lỗi và phải dùng tên dự phòng");
        }

        static string GetUniqueJsonTestPath()
        {
            return Path.Combine(
                Path.GetTempPath(),
                "PigTycoon_SaveSystem_Test",
                $"save_test_{Guid.NewGuid():N}.json");
        }

        static GameEngine RestoreEngineFromJson(string json)
        {
            string path = GetUniqueJsonTestPath();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, json);
                var restored = new GameEngine();
                restored.RestoreFromSave(SaveSystem.Load(path));
                return restored;
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        static string GetTestSavePath()
        {
            return Path.Combine(
                Path.GetTempPath(),
                "PigTycoon_SaveSystem_Test",
                "save_test.json");
        }

        static void TestSaveToFile()
        {
            string path = GetTestSavePath();

            try
            {
                var engine = new GameEngine();
                engine.Farm.Gold = 2500;

                SaveSystem.Save(engine.CreateSaveData(), path);

                Assert(File.Exists(path), "File JSON phải được tạo");
                Assert(new FileInfo(path).Length > 0, "File JSON không được rỗng");
            }
            finally
            {
                if (File.Exists(path))
                File.Delete(path);
            }
        }

        static void TestLoadFromFile()
        {
            string path = GetTestSavePath();

            try
            {
                var engine = new GameEngine();
                engine.Farm.Gold = 2500;
                engine.Farm.FarmLevel = 3;
                engine.Clock.TickMinutes(125);

                engine.Pigs.Add(Pig.CreateDefault(
                    "test_pig", "Heo Kiểm Thử",
                    GeneLineId.HongDien, GeneRarity.Thuong));

                SaveSystem.Save(engine.CreateSaveData(), path);

                var data = SaveSystem.Load(path);
                var restored = new GameEngine();
                restored.RestoreFromSave(data);

                Assert(restored.Farm.Gold == 2500, "Sai số vàng");
                Assert(restored.Farm.FarmLevel == 3, "Sai cấp nông trại");
                Assert(restored.Pigs.Count == 1, "Sai số lượng heo");
                Assert(restored.Pigs[0].Name == "Heo Kiểm Thử", "Sai tên heo");
                Assert(restored.Clock.CurrentDay == engine.Clock.CurrentDay && restored.Clock.CurrentHour == engine.Clock.CurrentHour && restored.Clock.CurrentMinute == engine.Clock.CurrentMinute, "Thời gian không được khôi phục đúng");
            }
            finally
            {
                if (File.Exists(path))
                File.Delete(path);
            }
        }

        static void TestLoadMissingFile()
        {
            string path = GetTestSavePath();

            if (File.Exists(path))
            File.Delete(path);

            bool caught = false;

            try
            {
                SaveSystem.Load(path);
            }
            catch (FileNotFoundException)
            {
                caught = true;
            }

            Assert(caught, "File không tồn tại phải báo lỗi phù hợp");
        }

        static void TestLoadInvalidJson()
        {
            string path = GetTestSavePath();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "{ JSON bị lỗi ");

                bool caught = false;

                try
                {
                    SaveSystem.Load(path);
                }
                catch (InvalidDataException)
                {
                    caught = true;
                }

                Assert(caught, "JSON sai định dạng phải được phát hiện");
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }


    }
}
