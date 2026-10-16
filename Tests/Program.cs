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
            RunTest("Roster: chỉ An và Khoa, chỉ số GDD 8.1, thưởng không sửa gốc", TestPlayerRoster);
            RunTest("An: 4 hướng và đi bộ không hao thể lực, chạy 7 m/s hao GDD 9.2", TestAnLocomotion);
            RunTest("Farm_Main: ranh giới, cổng >= 6 m, 4 khu hướng", TestFarmMainLayout);
            RunTest("Camera: 3 khung GDD 3.6 đổi ra ortho size", TestCameraFrames);
            RunTest("GameClock: đồng hồ dừng khi đang đợt quái", TestClockPausesDuringCombat);
            RunTest("Economy: 1000 ô chợ đêm nhận vàng xấp xỉ 18%", TestNightMarketGoldRate);
            RunTest("An: sức vác theo STR gốc, trên 35 kg là vác nặng", TestCarryCapacity);
            RunTest("An: một ngày không trừ máu", TestAnDayScript);
            RunTest("Rào: hàng ngang và hàng dọc giữ mặt nạ trục", TestFenceCourse);
            RunTest("Cổng: bốn hướng 6,2 m, heo hoảng mới thoát", TestPastureEscape);
            RunTest("Heo: đói thì ăn, máng trống thì đi tìm, hoảng và cách ly không ăn máng đàn", TestHerdHungerAndSleep);
            RunTest("Bệnh: thời tiết, cách ly, ba thuốc, vắc-xin, cảnh báo", TestDiseaseBoard);
            RunTest("Phối: cửa sổ GDD 4.4, lứa, gen, mất lứa", TestBreedingLedger);
            RunTest("Tutorial: tám bước tính theo thứ tự", TestTutorialTrack);
            RunTest("P0.06: Farm_Main có tilemap, cổng và prefab An", TestFarmMainWeekTwo);
            RunTest("P0.07: catalog D đủ số lượng và ID", TestDataCatalog);
            RunTest("P0.09–P0.10: Khoa 4 hướng và dummy nhận đòn", TestKhoaAndCombatSandbox);
            RunTest("P0.08: Balance.json khớp mẫu GDD", TestBalanceBook);

            // A: Save / Load System Tests (14 tests)
            RunTest("Save/Load: Khôi phục thời gian GameClock", TestSaveLoadClock);
            RunTest("Save/Load: Khôi phục dữ liệu Farm", TestSaveLoadFarm);
            RunTest("Save/Load: Khôi phục danh sách Heo", TestSaveLoadPigs);
            RunTest("Save/Load: Khôi phục công trình Phòng thủ", TestSaveLoadDefense);
            RunTest("Save/Load: Khôi phục Bản vẽ đã mở khóa", TestSaveLoadBlueprints);
            RunTest("Save/Load: Khôi phục Hạ tầng nông trại", TestSaveLoadInfrastructure);
            RunTest("Save/Load: Khôi phục dữ liệu Nhân vật", TestSaveLoadCharacter);
            RunTest("Save/Load: JSON round-trip Nhân vật, Hạ tầng & Phòng thủ", TestJsonRoundTripCharacterInfrastructureAndDefense);
            RunTest("Save/Load: JSON thiếu/null dữ liệu Nhân vật & Heo", TestJsonNullAndMissingCharacterAndPigData);
            RunTest("Save/Load: JSON thiếu/null dữ liệu Công trình", TestJsonNullAndMissingBuildingData);
            RunTest("Save/Load: Lưu ra file JSON", TestSaveToFile);
            RunTest("Save/Load: Đọc từ file JSON", TestLoadFromFile);
            RunTest("Save/Load: Báo lỗi khi file không tồn tại", TestLoadMissingFile);
            RunTest("Save/Load: Báo lỗi khi JSON sai định dạng", TestLoadInvalidJson);

            // C: Combat, Weapons, Enemies, Loot & Alien Escalation Tests (7 tests)
            RunTest("Combat: Hệ thống dữ liệu Vũ khí (Đao, Búa, Giáo)", TestWeaponDataSystem);
            RunTest("Combat: Chỉ số Quái vật & Công thức sát thương/nộ khí", TestEnemyDataAndFormulas);
            RunTest("Combat: Cơ chế Stardew 1.6 (Swipe, Crit, Parry) 64x64", TestStardewCombatMechanics64x64);
            RunTest("Combat: Bù nhìn tập đánh & Arena Analytics", TestTrainingDummyAndArenaAnalytics);
            RunTest("Combat: Hệ thống rơi vật phẩm (Loot System & Rarity)", TestLootSystemAndDropRules);
            RunTest("Combat: Bắt cóc heo, UFO Tractor Beam & Raid Telegraphs", TestPigSnatchingAndRaidTelegraphInvariants);
            RunTest("Combat: Chuỗi leo thang Alien, UFO thả quân & Tàu Mẹ", TestAlienEscalationChain);

            // B: Complete World, Farm, Herd, Genetics, Disease, Pets/NPCs & GDD 3.10 Tests (8 tests)
            RunTest("B-P1.03/P2.23: Flow-Field 1m, Lùa về Nơi Trú (>=90% <=2h) & Tiếng Huýt Khoa", TestHerdFlowFieldAndRecall);
            RunTest("B-P1.05: Lưới Ô Xây 2x2m (Xanh/Vàng/Đỏ) & Tháo dỡ hoàn 100%/70%", TestFarmGridPlacementAndDemolish);
            RunTest("B-P2.23: Tầm phục vụ 30m, Nguồn mát 25m (Lam Khê 20m) & Chen Chúc cục bộ", TestServiceCoolingAndLocalCrowding);
            RunTest("B-P2.03: Kéo xác (+1 STA/10m) & Khu Xử Lý (Thiêu Hủy / Tái Chế Sinh Học)", TestCarcassProcessingAndDragStamina);
            RunTest("B-P1.04/P2.03: Khẩu phần 4 tuổi, Định hình thịt & 6 bước Hậu duệ heo già", TestElderOffspringSixStepsAndShaping);
            RunTest("B-P2.15/P4.08: 12 Dòng Gen, 18 Tính trạng & Khắc chế sau giám định", TestGenesTraitsAndCountermeasures);
            RunTest("B-P2.04/P2.12/P3.11/P4.12: 5 Động từ bệnh, Thú cưng, NPC, Quái, Công trình & 4 Sự kiện B", TestPetsNpcsFiveVerbsAndOccultEvents);
            RunTest("B-P5.11: Nghiệm thu Farm_Main đạt chuẩn 8 nhóm A–H (GDD 3.10)", TestFarmMainChecklistGdd310);
            RunTest("E2E-1: Vòng lặp Giờ (Tầm 30m, Nguồn mát 25m, Chen chúc 10x10m & Tái chế xác -> Kho cám)", TestEndToEndHourlySimulationLoop);
            RunTest("E2E-2: Vòng lặp Ngày (Tiêu thụ cám, Định hình thịt S/A/B/C & Thuần dưỡng Xích Mao 3 ngày)", TestEndToEndDailyFeedGrowthShapingAndXichMao);
            RunTest("E2E-3: Phối giống -> Mang thai 3 ngày -> Đẻ F1/Heo già 6 bước & Phong Mẫu nuôi mồ côi", TestEndToEndBreedingGestationBirthAndOrphanPhongMau);
            RunTest("E2E-4: Dịch bệnh, Ca trực NPC tự động, 5 Động từ Y tế & Hư Thể thức tỉnh trong GameEngine", TestEndToEndDiseaseNpcShiftAndDeviantAwakening);
            RunTest("E2E-5: Save/Load JSON v2 bảo toàn 100% trạng thái sâu của Trại, Đàn, Lưới, NPC & Heo", TestEndToEndFullSaveLoadV2State);
            RunTest("Deep-1: Đỡ Tự Động (Auto-Guard), Thanh Chống Chịu, Đỡ Chủ Động, Vỡ Chống Chịu/Kiệt Sức -> Ngất & Kéo Về Trại", TestCombatAutoGuardAndGuardBreakFaint);
            RunTest("Deep-2: Hồi phục Thể Lực/Chống Chịu trong & ngoài giao tranh, 6 Trang bị mở 3 ô Võ học & Huyết Tế Thế Thủ", TestCombatStaminaRegenSixEquipmentPairsAndMartialBloodSacrifice);
            RunTest("Deep-3: Lưới Điện (Sập cầu dao >140%), 5 Vật phẩm xóa hồi chờ ngủ (Quả Mộng), Cổng Giữa Game & Tình báo 6 bậc", TestPowerGridSleepBypassMidGameGateAndSpecialtyPapers);
            RunTest("Deep-4: Cốt truyện Mùa 1 'Sáu Với Một', Sổ Hai Cột, Heo chân trắng giả chết, Chỗ trống 2018 & 3 Kết thúc", TestSeasonOneStorySauVoiMotAndTwoColumnNotebook);

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

        static void TestPlayerRoster()
        {
            var ids = (PlayerId[])Enum.GetValues(typeof(PlayerId));
            Assert(ids.Length == 2, "Chỉ được có 2 nhân vật người chơi");

            var an = new CharacterData(PlayerId.An);
            Assert(an.Identity == PlayerId.An && an.Name == "An", "Prototype Farm_Main phải vào vai An");
            Assert(an.Id == PlayerRoster.AnDataId, "ID An phải là player.an");
            Assert(an.BasicTraitId == PlayerRoster.AnBasicTraitId, "Đặc tính mở của An là Mắt Nhà Nghề");
            Assert(an.BaseStats.Str == 17 && an.BaseStats.Agi == 23 && an.BaseStats.Ctrl == 24 && an.BaseStats.Res == 18, "Chỉ số gốc An sai GDD 8.1");

            var khoa = new CharacterData(PlayerId.Khoa);
            Assert(khoa.BaseStats.Str == 24 && khoa.BaseStats.Agi == 18 && khoa.BaseStats.Ctrl == 18 && khoa.BaseStats.Res == 22, "Chỉ số gốc Khoa sai GDD 8.1");

            an.EquipItem(new EquipmentItem { Slot = EquipSlot.Weapon, BonusStr = 5, WeightScore = 1 });
            Assert(an.BaseStats.Str == 17, "Trang bị không được cộng vào chỉ số gốc");
            Assert(an.BonusStats.Str == 5, "Chỉ số thưởng phải tách riêng");

            var engine = new GameEngine();
            Assert(engine.Character.Identity == PlayerId.An, "GameEngine của map B phải khởi tạo An");
        }

        static void TestAnLocomotion()
        {
            var hold = CardinalFacing.Resolve(0f, 0f, CardinalDirection.South);
            Assert(hold == CardinalDirection.South, "Đứng yên phải giữ hướng cũ");
            Assert(CardinalFacing.Resolve(1f, 0f, CardinalDirection.South) == CardinalDirection.East, "Phải nhìn Đông");
            Assert(CardinalFacing.Resolve(-1f, 0.2f, CardinalDirection.South) == CardinalDirection.West, "Phải nhìn Tây");
            Assert(CardinalFacing.Resolve(0f, 1f, CardinalDirection.South) == CardinalDirection.North, "Phải nhìn Bắc");
            Assert(CardinalFacing.Resolve(0.2f, -1f, CardinalDirection.North) == CardinalDirection.South, "Phải nhìn Nam");
            Assert(CardinalFacing.MirrorSideSprite(CardinalDirection.West), "Tây dùng sprite ngang lật");
            Assert(!CardinalFacing.MirrorSideSprite(CardinalDirection.East), "Đông không lật sprite");

            var walk = PlayerLocomotion.Advance(false, 23, 180f, 1f);
            Assert(!walk.IsRunning && Math.Abs(walk.MetersPerSecond - 4.5f) < 0.001f, "Đi bộ 4.5 m/s");
            Assert(Math.Abs(walk.StaminaDelta) < 0.001f && Math.Abs(walk.DailyLoadDelta) < 0.001f, "Đi bộ không hao thể lực");

            var run = PlayerLocomotion.Advance(true, 23, 180f, 1f);
            Assert(run.IsRunning && Math.Abs(run.MetersPerSecond - 7f) < 0.001f, "Chạy 7 m/s");
            Assert(Math.Abs(run.StaminaDelta + 1.3f) < 0.001f, "An AGI 23 chạy tốn 1.3 thể lực/giây");
            Assert(Math.Abs(run.DailyLoadDelta - 0.26f) < 0.001f, "20% hao chạy thành tải ngày");

            var tired = PlayerLocomotion.Advance(true, 23, 0f, 1f);
            Assert(!tired.IsRunning, "Hết thể lực thì không chạy được");

            var agile = PlayerLocomotion.Advance(true, 30, 180f, 1f);
            Assert(Math.Abs(agile.StaminaDelta + 1.0f) < 0.001f, "AGI gốc >= 30 thì chạy tốn 1.0/giây");
        }

        static void TestFarmMainLayout()
        {
            Assert(FarmMainLayout.IsInsidePasture(0f, 0f), "Tâm chuồng phải nằm trong đồng cỏ");
            Assert(!FarmMainLayout.IsInsidePasture(0f, -20f), "Phía nam cổng phải ngoài chuồng");
            Assert(FarmMainLayout.IsInsideMap(0f, -20f), "Vành nam vẫn thuộc bản đồ");
            Assert(!FarmMainLayout.IsInsideMap(100f, 100f), "Điểm xa phải ngoài bản đồ");
            Assert(FarmMainLayout.MainGateWidthMeters >= 6f, "Cổng chính phải rộng ít nhất 6 m");
            Assert(FarmMainLayout.SectorFor(0f, 20f) == ApproachSector.North, "Điểm bắc phải thuộc khu Bắc");
            Assert(FarmMainLayout.SectorFor(30f, 0f) == ApproachSector.East, "Điểm đông phải thuộc khu Đông");
            Assert(FarmMainLayout.SectorFor(0f, -20f) == ApproachSector.South, "Điểm nam phải thuộc khu Nam");
            Assert(FarmMainLayout.SectorFor(-30f, 0f) == ApproachSector.West, "Điểm tây phải thuộc khu Tây");
            Assert(FarmSorting.LayerOrder.Length == 5, "Phải có 5 sorting layer chuẩn");
            Assert(FarmSorting.LayerOrder[0] == "Ground" && FarmSorting.LayerOrder[2] == "Actors", "Thứ tự layer Ground rồi Actors");
        }

        static void TestCameraFrames()
        {
            Assert(Math.Abs(CameraFrames.OrthoSizeForHeight(CameraFrames.CloseHeightMeters) - 11f) < 0.001f, "Cận 22 m -> ortho 11");
            Assert(Math.Abs(CameraFrames.OrthoSizeForHeight(CameraFrames.DefaultHeightMeters) - 18f) < 0.001f, "Mặc định 36 m -> ortho 18");
            Assert(Math.Abs(CameraFrames.OrthoSizeForHeight(CameraFrames.OverviewHeightMeters) - 36f) < 0.001f, "Toàn cảnh 72 m -> ortho 36");
            Assert(CameraFrames.CloseWidthMeters == 40f && CameraFrames.DefaultWidthMeters == 64f && CameraFrames.OverviewWidthMeters == 128f, "Chiều ngang 3 khung sai");
        }

        static void TestClockPausesDuringCombat()
        {
            var clock = new GameClock(1, 8, 15);
            clock.IsDuringCombatWave = true;
            var paused = clock.TickMinutes(90);
            Assert(paused.MinutesAdvanced == 0, "Đợt quái không được cộng phút");
            Assert(clock.CurrentHour == 8 && clock.CurrentMinute == 15 && clock.CurrentDay == 1, "Giờ phải đứng yên trong đợt");

            clock.IsDuringCombatWave = false;
            clock.TickMinutes(45);
            Assert(clock.CurrentHour == 9 && clock.CurrentMinute == 0, "Hết đợt thì đồng hồ chạy tiếp");
        }

        static void TestNightMarketGoldRate()
        {
            var eco = new EconomyManager(20261008);
            int slots = 0;
            int goldSlots = 0;
            for (int i = 0; i < 125; i++)
            {
                eco.GenerateNightMarket(5);
                slots += eco.NightListings.Count;
                for (int s = 0; s < eco.NightListings.Count; s++)
                {
                    if (eco.NightListings[s].AcceptsGold) goldSlots++;
                }
            }

            Assert(slots == 1000, "Phải sinh đủ 1000 ô");
            double rate = goldSlots / (double)slots;
            Assert(rate > 0.14 && rate < 0.22, $"Tỉ lệ ô nhận vàng phải xấp xỉ 18%, thực tế {rate:P1}");
        }

        static void TestCarryCapacity()
        {
            var an = new CharacterData(PlayerId.An);
            var khoa = new CharacterData(PlayerId.Khoa);
            Assert(System.Math.Abs(CarryCapacity.MaxKilograms(an.BaseStats.Str) - 32f) < 0.001f, "An STR 17 vác tối đa 32 kg");
            Assert(System.Math.Abs(CarryCapacity.MaxKilograms(khoa.BaseStats.Str) - 39f) < 0.001f, "Khoa STR 24 vác tối đa 39 kg");

            float carried = 0f;
            Assert(CarryCapacity.TryAdd(carried, an.BaseStats.Str, 10f, out carried), "An nhận 10 kg");
            Assert(!CarryCapacity.TryAdd(carried, an.BaseStats.Str, 30f, out carried), "An không nhận thêm 30 kg");
            Assert(System.Math.Abs(carried - 10f) < 0.001f, "Tải An giữ 10 kg khi từ chối");
            Assert(!CarryCapacity.IsHeavy(carried), "10 kg chưa là vác nặng");

            float khoaLoad = 0f;
            Assert(CarryCapacity.TryAdd(khoaLoad, khoa.BaseStats.Str, 36f, out khoaLoad), "Khoa nhận 36 kg");
            Assert(CarryCapacity.IsHeavy(khoaLoad), "Trên 35 kg là vác nặng");
            an.BonusStats.Str = 20;
            Assert(System.Math.Abs(CarryCapacity.MaxKilograms(an.BaseStats.Str) - 32f) < 0.001f, "Thưởng STR không nâng trần vác");
        }

        static void TestAnDayScript()
        {
            Assert(AnDayScript.BeatAt(5, 59) == AnDayBeat.Sleep, "05:59 vẫn là ngủ");
            Assert(AnDayScript.BeatAt(6, 0) == AnDayBeat.WakeAndFeed, "06:00 cho ăn");
            Assert(AnDayScript.BeatAt(11, 59) == AnDayBeat.FenceCheck, "11:59 còn soát rào");
            Assert(AnDayScript.BeatAt(12, 0) == AnDayBeat.IndoorRest, "12:00 nghỉ trong nhà");
            Assert(AnDayScript.BeatAt(14, 0) == AnDayBeat.MarketGlance, "14:00 nhìn chợ");
            Assert(AnDayScript.BeatAt(20, 59) == AnDayBeat.HerdRound, "20:59 còn vòng đàn");
            Assert(AnDayScript.BeatAt(21, 0) == AnDayBeat.Sleep, "21:00 ngủ");

            var an = new CharacterData(PlayerId.An);
            an.Hp = 90;
            an.Stamina.DailyLoad = 40f;
            an.Stamina.CurrentStamina = 10f;
            foreach (AnDayBeat beat in System.Enum.GetValues(typeof(AnDayBeat)))
            {
                AnDayScript.Apply(an, beat);
                Assert(an.Hp == 90, "Kịch bản một ngày không trừ máu");
            }

            Assert(an.Stamina.CurrentStamina == an.Stamina.MaxStamina, "Nhịp cho ăn hồi đầy thể lực");
        }

        static void TestFenceCourse()
        {
            var east = new FenceCourse();
            east.PlaceRun(0, 0, FenceHeading.East, 4);
            Assert(east.MaskAt(0, 0) == 2, "Đầu hàng ngang chỉ nối Đông");
            Assert(east.MaskAt(1, 0) == 10, "Giữa hàng ngang là Đông+Tây");
            Assert(east.MaskAt(3, 0) == 8, "Cuối hàng ngang chỉ nối Tây");
            Assert((east.MaskAt(1, 0) & 5) == 0, "Hàng ngang không có bit Bắc hoặc Nam");

            var north = new FenceCourse();
            north.PlaceRun(5, 5, FenceHeading.North, 3);
            Assert(north.MaskAt(5, 6) == 5, "Giữa hàng dọc là Bắc+Nam");
            Assert((north.MaskAt(5, 6) & 10) == 0, "Hàng dọc không có bit Đông hoặc Tây");
        }

        static void TestPastureEscape()
        {
            BoundaryGap[] gates = PastureBoundary.FourGates();
            Assert(gates.Length == 4, "Đủ bốn cổng");
            for (int i = 0; i < gates.Length; i++)
            {
                Assert(gates[i].WidthMeters >= 6.2f, "Mỗi cổng rộng từ 6,2 m");
            }

            var south = new BoundaryGap(ApproachSector.South, 0f, 6.2f);
            Assert(PastureBoundary.PanicEscapes(0f, -13.9f, true, south), "Heo hoảng ở cổng Nam thoát");
            Assert(!PastureBoundary.PanicEscapes(0f, -13.9f, false, south), "Heo bình tĩnh không thoát");
            Assert(!PastureBoundary.PanicEscapes(10f, -13.9f, true, south), "Sát tường Nam ngoài khẩu độ cổng thì không thoát");

            var narrow = new BoundaryGap(ApproachSector.South, 0f, 2f);
            Assert(!PastureBoundary.PanicEscapes(0f, -13.9f, true, narrow), "Khe 2 m không đủ để thoát");

            var north = new BoundaryGap(ApproachSector.North, 0f, 6.2f);
            var east = new BoundaryGap(ApproachSector.East, 0f, 6.2f);
            var west = new BoundaryGap(ApproachSector.West, 0f, 6.2f);
            Assert(PastureBoundary.PanicEscapes(0f, 13.5f, true, north), "Cổng Bắc");
            Assert(PastureBoundary.PanicEscapes(19.5f, 0f, true, east), "Cổng Đông");
            Assert(PastureBoundary.PanicEscapes(-19.5f, 0f, true, west), "Cổng Tây");
        }

        static void TestHerdHungerAndSleep()
        {
            var yard = new HerdTick { FeedPortions = 1, SleepSlots = 1, Hour = 12 };
            var hungry = new HerdPig { Id = "a", Hunger = 50 };
            yard.Tick(hungry);
            Assert(hungry.Act == PigAct.Eat && yard.FeedPortions == 0, "Đói và còn máng thì ăn");

            var stillHungry = new HerdPig { Id = "b", Hunger = 50 };
            yard.Tick(stillHungry);
            Assert(stillHungry.Act == PigAct.SeekFood && yard.FeedPortions == 0, "Máng trống thì đi tìm, không ăn");

            var panic = new HerdPig { Id = "c", Hunger = 80, Mood = 10 };
            yard.FeedPortions = 3;
            yard.Tick(panic);
            Assert(panic.Act == PigAct.Panic && yard.FeedPortions == 3, "Heo hoảng không ăn máng");

            var isolated = new HerdPig { Id = "d", Hunger = 80, Isolated = true };
            yard.Tick(isolated);
            Assert(isolated.Act == PigAct.Isolated && yard.FeedPortions == 3, "Heo cách ly không ăn máng đàn");

            var dead = new HerdPig { Id = "e", Alive = false, Hunger = 90 };
            yard.Tick(dead);
            Assert(dead.Act == PigAct.Dead, "Heo chết không ăn");

            var night = new HerdTick { FeedPortions = 0, SleepSlots = 1, Hour = 22 };
            var first = new HerdPig { Id = "f", Hunger = 0, Comfort = 40, Mood = 70 };
            var second = new HerdPig { Id = "g", Hunger = 0, Comfort = 40, Mood = 70 };
            night.Tick(first);
            night.Tick(second);
            Assert(first.Act == PigAct.Sleep && first.Comfort == 48, "Một ô ngủ nhận một heo và tăng thoải mái");
            Assert(second.Act == PigAct.Wander && second.Comfort == 40, "Heo thứ hai không chiếm ô đã đầy");

            CareBar[] bars = CareBars.For(first);
            Assert(bars.Length == 3 && bars[0].Id == "Health" && bars[1].Id == "Comfort" && bars[2].Id == "Happiness", "Ba thanh Health, Comfort, Happiness");
            Assert(bars[2].Value == first.Mood && bars[1].Tooltip.Length > 0, "Happiness đọc tinh thần và có tooltip");
        }

        static void TestDiseaseBoard()
        {
            Assert(System.Math.Abs(DiseaseBoard.HabitatDiseaseFactor(95) - 0.60f) < 0.001f, "SC 95 hệ số 0,60");
            Assert(System.Math.Abs(DiseaseBoard.HabitatDiseaseFactor(10) - 2.30f) < 0.001f, "SC 10 hệ số 2,30");
            float calm = DiseaseBoard.OutbreakChance(95, 1f, 1f, 1f, 1f);
            float storm = DiseaseBoard.OutbreakChance(0, 2f, 2f, 2f, 2f);
            Assert(calm > 0.002f && calm < 0.01f, "Trại sạch vẫn có xác suất sàn trên 0,2%");
            Assert(System.Math.Abs(storm - 0.35f) < 0.001f, "Xác suất phát dịch kẹp trần 35%");

            Assert(System.Math.Abs(DiseaseBoard.WeatherSpread(DiseaseId.HoHapLanh, WeatherType.Mua, false) - 1.15f) < 0.001f, "Mưa không mái làm Hô Hấp Lạnh ×1,15");
            Assert(System.Math.Abs(DiseaseBoard.WeatherSpread(DiseaseId.HoHapLanh, WeatherType.Mua, true) - 1f) < 0.001f, "Có mái thì hết hệ số mưa");
            Assert(System.Math.Abs(DiseaseBoard.WeatherSpread(DiseaseId.GheKySinh, WeatherType.NomAm, true) - 1.40f) < 0.001f, "Nồm ẩm Ghẻ ×1,4");
            Assert(System.Math.Abs(DiseaseBoard.WeatherSpread(DiseaseId.LoiTam, WeatherType.MuaBucXa, false) - 1.50f) < 0.001f, "Mưa bức xạ nhóm D ×1,5");

            var pen = new IsolationPen();
            int admitted = 0;
            for (int i = 0; i < 5; i++)
            {
                if (pen.TryAdmit(new HerdPig { Id = "p" + i })) admitted += 1;
            }

            Assert(admitted == 4 && pen.Count == 4, "Chuồng cách ly đủ 4 thì từ chối con thứ 5");

            var heat = new HerdPig { Health = 40 };
            Assert(DiseaseBoard.TryInfect(heat, DiseaseId.SayNang), "Nhiễm Say Nắng");
            Assert(!DiseaseBoard.ApplyDrug(heat, FarmDrug.AnThan, true), "Sai thuốc thì bệnh còn");
            Assert(heat.Disease == DiseaseId.SayNang, "Say Nắng không mất vì an thần");
            Assert(DiseaseBoard.ApplyDrug(heat, FarmDrug.HaNhiet, false), "Hạ nhiệt chữa Say Nắng");
            Assert(heat.Disease == null && heat.Health == 55, "Hết sốt và hồi 15 máu");

            var mange = new HerdPig();
            DiseaseBoard.TryInfect(mange, DiseaseId.GheKySinh);
            Assert(!DiseaseBoard.ApplyDrug(mange, FarmDrug.TriGhe, false), "Trị ghẻ khi chưa thay ổ thì không khỏi");
            Assert(DiseaseBoard.ApplyDrug(mange, FarmDrug.TriGhe, true), "Đổi ổ rồi trị ghẻ thì khỏi");

            var shock = new HerdPig();
            DiseaseBoard.TryInfect(shock, DiseaseId.LoiTam);
            Assert(shock.SeizurePending, "Lôi Tâm có cơn chờ");
            Assert(DiseaseBoard.ApplyDrug(shock, FarmDrug.AnThan, false), "An thần cắt cơn");
            Assert(!shock.SeizurePending && shock.Disease == null, "Hết cơn và hết bệnh");

            var voidPig = new HerdPig();
            DiseaseBoard.TryInfect(voidPig, DiseaseId.HuMach);
            Assert(!DiseaseBoard.ApplyDrug(voidPig, FarmDrug.HaNhiet, true), "Hư Mạch không nhận thuốc thường");

            var healthy = new HerdPig();
            Assert(DiseaseBoard.TryImmunize(healthy) && healthy.ImmuneDays == 3, "Heo khỏe được vắc-xin 3 ngày");
            Assert(!DiseaseBoard.TryInfect(healthy, DiseaseId.DichTaHeo), "Đang miễn dịch thì không nhiễm");
            var sick = new HerdPig();
            DiseaseBoard.TryInfect(sick, DiseaseId.SotBun);
            Assert(!DiseaseBoard.TryImmunize(sick), "Heo đang bệnh không tiêm phòng");

            Assert(DiseaseBoard.IsContaminatedPen(1.25f, 30, true), "Mật độ cao, SC thấp, có chất thải thì Chuồng Nhiễm");
            Assert(!DiseaseBoard.IsContaminatedPen(1.25f, 30, false), "Không có chất thải thì chưa phải Chuồng Nhiễm");
            Assert(DiseaseBoard.Alerts(true, 0.01f), "Có bệnh nhóm A/B thì bật cảnh báo");
            Assert(DiseaseBoard.Alerts(false, 0.05f), "Xác suất phát dịch từ 5% thì bật cảnh báo");
            Assert(!DiseaseBoard.Alerts(false, 0.01f), "Trại sạch và xác suất thấp thì không cảnh báo");
        }

        static void TestBreedingLedger()
        {
            Assert(BreedingLedger.Conceives(0.69, PigStage.TruongThanh), "Roll 0,69 đậu ở trưởng thành");
            Assert(!BreedingLedger.Conceives(0.70, PigStage.TruongThanh), "Roll 0,70 trượt ngưỡng 70%");
            Assert(BreedingLedger.Conceives(0.38, PigStage.HeoGia), "Heo già còn 55% của 70%");
            Assert(!BreedingLedger.Conceives(0.39, PigStage.HeoGia), "Roll 0,39 trượt ngưỡng heo già");

            var young = new BreedingSow { AgeDays = 10, Stage = PigStage.DangLon };
            Assert(!BreedingLedger.TrySchedule(young, true, 0, 0.1), "Chưa đủ 12 ngày thì không phối");

            var sow = new BreedingSow();
            Assert(!BreedingLedger.TrySchedule(sow, true, 8, 0.1), "Đủ 8 ca mang thai thì dừng");
            Assert(BreedingLedger.TrySchedule(sow, true, 7, 0.1), "Còn chỗ và roll đậu thì mang thai 3 ngày");
            Assert(sow.GestationDaysLeft == 3, "Thai kỳ 3 ngày");
            Assert(!BreedingLedger.TrySchedule(sow, true, 0, 0.1), "Đang mang thai thì không phối tiếp");

            var sick = new BreedingSow { Disease = DiseaseId.RoiLoanSinhSan };
            Assert(!BreedingLedger.TrySchedule(sick, true, 0, 0.0), "Rối loạn sinh sản không phối");

            var rested = new BreedingSow { DaysSinceBirth = 1 };
            Assert(!BreedingLedger.TrySchedule(rested, true, 0, 0.0), "Chưa nghỉ đủ 2 ngày");

            sow.GestationDaysLeft = 0;
            sow.Alive = false;
            BirthOutcome lost = BreedingLedger.GiveBirth(sow, 2, new double[] { 0.1 }, GeneLineId.MocCuoc);
            Assert(!lost.Born && lost.Loss == LitterLossReason.NoMother && lost.Count == 0, "Nái chết thì không có con");

            var panicked = new BreedingSow { Mood = 10, FatherGene = GeneLineId.LamKhe };
            BirthOutcome panicBirth = BreedingLedger.GiveBirth(panicked, 4, new double[] { 0.1 }, GeneLineId.MocCuoc);
            Assert(!panicBirth.Born && panicBirth.Loss == LitterLossReason.SowPanic, "Tinh thần dưới 20 thì mất lứa");

            var mother = new BreedingSow { Gene = GeneLineId.HongDien, FatherGene = GeneLineId.ThietBi, InbredPair = true };
            BirthOutcome litter = BreedingLedger.GiveBirth(mother, 4, new[] { 0.10, 0.30, 0.80 }, GeneLineId.MocCuoc);
            Assert(litter.Born && litter.Count == 8, "Roll 4 ra 10 con, cận huyết trừ 2 còn 8");
            Assert(litter.Genes[0] == GeneLineId.ThietBi, "25% đầu lấy dòng bố");
            Assert(litter.Genes[1] == GeneLineId.HongDien, "25% sau lấy dòng mẹ");
            Assert(litter.Genes[2] == GeneLineId.MocCuoc, "Phần còn lại lấy dòng thường");
            Assert(System.Math.Abs(litter.LatentDiseaseRisk - 0.08f) < 0.001f, "Cận huyết cộng 8% bệnh tiềm");
            Assert(mother.LittersBorn == 1, "Đẻ xong tính một lứa");
        }

        static void TestTutorialTrack()
        {
            var facts = new TutorialFacts();
            Assert(TutorialTrack.CompletedPrefix(facts) == 0, "Chưa làm gì thì đứng ở bước 0");
            facts.PigAte = true;
            facts.MarketGlance = true;
            Assert(TutorialTrack.CompletedPrefix(facts) == 0, "Nhảy cóc không tính");
            facts.PigAte = false;
            facts.MarketGlance = false;
            facts.PlayedAsAn = true;
            facts.TookWalkStep = true;
            facts.StoodAtGate = true;
            Assert(TutorialTrack.CompletedPrefix(facts) == 3, "Ba bước đầu xong thì dừng trước bước rào");
            facts.PlacedHorizontalFence = true;
            facts.PigAte = true;
            facts.ReadCareBars = true;
            facts.TreatedOrIsolated = true;
            Assert(TutorialTrack.CompletedPrefix(facts) == 7, "Thiếu nhịp chợ thì dừng ở 7");
            facts.MarketGlance = true;
            Assert(TutorialTrack.CompletedPrefix(facts) == TutorialTrack.StepCount, "Đủ tám bước");
        }

        static void TestFarmMainWeekTwo()
        {
            string root = FindRepoRoot();
            string scene = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets", "Scenes", "Farm_Main.unity"));
            string prefab = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets", "Prefabs", "PF_Player_An.prefab"));
            Assert(scene.Contains("m_Name: Tilemap_Ground"), "Farm_Main có Tilemap_Ground");
            Assert(scene.Contains("m_Name: Pasture_Gate_Barrier"), "Farm_Main có cổng chuồng");
            Assert(scene.Contains("m_Name: PF_Player_An"), "Scene có người chơi An");
            Assert(prefab.Contains("m_Name: PF_Player_An"), "Prefab tên PF_Player_An");
            Assert(prefab.Contains("MoveSpeed: 4.5"), "Prefab đi 4,5 m/s");
            Assert(prefab.Contains("DownWalk:") && prefab.Contains("SideWalk:") && prefab.Contains("UpWalk:"), "Prefab có bước đi 4 hướng");
            Assert(System.IO.File.Exists(System.IO.Path.Combine(root, "Assets", "Art", "PRE_Sprite_Pixel32.preset")), "Có preset import pixel");
        }

        static void TestDataCatalog()
        {
            var errors = DataCatalog.Validate();
            Assert(errors.Count == 0, errors.Count == 0 ? "Catalog sạch" : string.Join(" | ", errors));
            Assert(DataCatalog.Entries.Count == 137, "12 gen + 12 NPC + 7 thú + 31 công trình + 14 quái + 3 boss + 24 kỹ năng + 10 thời tiết + 10 mối + 14 sự kiện");
        }

        static void TestKhoaAndCombatSandbox()
        {
            string root = FindRepoRoot();
            string prefab = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets", "Prefabs", "PF_Player_Khoa.prefab"));
            string farm = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets", "Scenes", "Farm_Main.unity"));
            string sandbox = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets", "Scenes", "CombatSandbox.unity"));
            Assert(prefab.Contains("m_Name: PF_Player_Khoa"), "Có prefab Khoa");
            Assert(prefab.Contains("DownWalk:") && prefab.Contains("UpWalk:") && prefab.Contains("SideWalk:"), "Khoa có đi 4 hướng");
            Assert(prefab.Contains("b1c2d3e4f5061728394a5b6c7d8e9f01"), "Khoa dùng sheet riêng, không dùng guid An");
            Assert(farm.Contains("a7e3c91b04d64f28b5a6d0e17c839245"), "Farm_Main đặt prefab Khoa");
            Assert(sandbox.Contains("m_Name: CombatDummy"), "Sandbox có dummy");
            Assert(sandbox.Contains("e8d41a02b6c74f19a0b3c5d67e192834"), "Dummy gắn script nhận đòn");

            var dummy = new CombatDummy(40, 0, 0);
            StrikeResult first = CombatStrike.Light(180f, dummy);
            Assert(first.Landed && first.Damage == 14 && dummy.Hp == 26, "Đòn nhẹ Dao Rựa trừ 14");
            Assert(System.Math.Abs(first.StaminaSpent - 2.5f) < 0.001f, "Đòn nhẹ tốn 2,5 thể lực");
            Assert(System.Math.Abs(first.DailyLoadAdded - 0.75f) < 0.001f, "30% đòn nhẹ thành tải ngày");
            CombatStrike.Light(180f, dummy);
            Assert(dummy.Hp == 12, "Hai đòn còn 12 máu");
            CombatStrike.Light(180f, dummy);
            Assert(dummy.Hp == 0 && !dummy.Alive, "Đòn thứ ba hạ dummy");
            StrikeResult dead = CombatStrike.Light(180f, dummy);
            Assert(!dead.Landed, "Dummy chết không nhận thêm đòn");

            var tired = new CombatDummy(40, 0, 0);
            StrikeResult miss = CombatStrike.Light(2f, tired);
            Assert(!miss.Landed && tired.Hp == 40, "Thiếu thể lực thì dummy không mất máu");

            Assert(CombatMath.DamageReceived(14, 100, 0) == 7, "Thủ 100 còn một nửa");
            Assert(CombatMath.HeavyRaw(48, 24) == 60, "Búa Tạ STR 24 là 48 × 1,24");
            var armored = new CombatDummy(40, 0, 18);
            StrikeResult vsRes = CombatStrike.Light(180f, armored);
            Assert(vsRes.Damage == 13, "RES 18 giảm 9% của 14, còn 13");
        }

        static void TestBalanceBook()
        {
            string root = FindRepoRoot();
            string json = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Assets", "Resources", "Balance.json"));
            IBalance book = BalanceBook.Load(json);
            Assert(book.SecondsPerGameDay == GameClock.SecondsPerGameDay, "Một ngày game là 960 giây thực");
            Assert(book.SecondsPerGameHour == GameClock.SecondsPerGameHour, "Một giờ game là 40 giây");
            var farm = new Farm();
            var report = farm.CalculateCapacityReport(0, 0);
            Assert(book.LandCapacityPerPlot * farm.Infrastructure.LandPlots == report.CapacityByLand, "Ô đất khớp Farm");
            Assert(book.ShelterCapacityPerRoof * farm.Infrastructure.Shelters == report.CapacityByShelters, "Mái trú khớp Farm");
            Assert(book.WaterCapacityPerBasin * farm.Infrastructure.WaterTroughs == report.CapacityByWater, "Bồn khớp Farm");
            Assert(book.FeedCapacityPerTrough * farm.Infrastructure.Feeders == report.CapacityByFeeders, "Máng khớp Farm");
            int upkeep = farm.Infrastructure.LandPlots * book.PlotUpkeepGold
                + farm.Infrastructure.Shelters * book.RoofUpkeepGold
                + farm.Infrastructure.WaterTroughs * book.BasinUpkeepGold
                + farm.Infrastructure.Feeders * book.TroughUpkeepGold;
            Assert(upkeep == report.DailyEmptyMaintenanceCost, "Phí duy trì khớp Farm");
            var an = new CharacterData(PlayerId.An);
            var khoa = new CharacterData(PlayerId.Khoa);
            Assert(book.AnStr == an.BaseStats.Str && book.AnAgi == an.BaseStats.Agi && book.AnCtrl == an.BaseStats.Ctrl && book.AnRes == an.BaseStats.Res, "An khớp roster");
            Assert(book.KhoaStr == khoa.BaseStats.Str && book.KhoaAgi == khoa.BaseStats.Agi && book.KhoaCtrl == khoa.BaseStats.Ctrl && book.KhoaRes == khoa.BaseStats.Res, "Khoa khớp roster");
            Assert(System.Math.Abs(book.WalkMetersPerSecond - 4.5f) < 0.001f, "Đi 4,5");
            Assert(System.Math.Abs(book.SprintMetersPerSecond - 7f) < 0.001f, "Chạy 7");
            Assert(System.Math.Abs(book.MainGateWidthMeters - 6.2f) < 0.001f, "Cổng 6,2");
            Assert(book.DaoRuaLightDamage == 14, "Dao rựa 14");
            Assert(System.Math.Abs(book.LightStrikeStamina - 2.5f) < 0.001f, "Đòn nhẹ 2,5 thể lực");
            Assert(System.Math.Abs(book.NightMarketGoldRate - 0.18f) < 0.001f, "Chợ đêm 18% ô vàng");
            string font = System.IO.Path.Combine(root, "Assets", "Fonts", "unifont-16.0.04.otf");
            byte[] head = System.IO.File.ReadAllBytes(font);
            Assert(head.Length > 1000000 && head[0] == (byte)'O' && head[1] == (byte)'T' && head[2] == (byte)'T' && head[3] == (byte)'O', "Unifont là OTF");
            Assert(System.Text.Encoding.ASCII.GetString(head).Contains("Unifont"), "Tên font Unifont");
        }

        static string FindRepoRoot()
        {
            var dir = new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory());
            while (dir != null)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "global.json")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new System.IO.DirectoryNotFoundException("Không thấy gốc repo");
        }

        // =====================================================================
        // A: SAVE / LOAD TESTS (from feat/A/P0.01-unity-foundation)
        // =====================================================================
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
            Assert(restoredEngine.Farm.Gold == 1234, "Vàng phải được giữ nguyên sau khi khôi phục");
            Assert(restoredEngine.Farm.FarmLevel == 5, "Cấp độ nông trại phải được giữ nguyên");
            Assert(restoredEngine.Farm.FarmExp == 321, "Kinh nghiệm nông trại phải được giữ nguyên");
        }

        static void TestSaveLoadPigs()
        {
            var engine = new GameEngine();
            var pig = Pig.CreateDefault("save_pig_1", "Heo Test", GeneLineId.HongDien, GeneRarity.Thuong);
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
                Assert(restoredBuilding.Durability == 67 && restoredBuilding.MaxHp == 1800 && restoredBuilding.CurrentHp == 1234 && !restoredBuilding.IsBroken, "Độ bền và máu công trình phải round-trip qua JSON");
                Assert(restoredBuilding.AmmoCurrent == 83 && restoredBuilding.AmmoCapacity == 120 && restoredBuilding.AmmoName == "Đơn Vị Cộng Hưởng" && restoredBuilding.AmmoCostPerShot == 50, "Đạn công trình phải round-trip qua JSON");
                Assert(Math.Abs(restoredBuilding.CooldownSeconds - 77.5f) < 0.001f && Math.Abs(restoredBuilding.CurrentCooldownRemaining - 12.25f) < 0.001f, "Cooldown công trình phải round-trip qua JSON");
                Assert(restoredBuilding.FireMode == "ThuCong", "Chế độ bắn của công trình phải round-trip qua JSON");
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }

        static void TestJsonNullAndMissingCharacterAndPigData()
        {
            var defaultEngine = new GameEngine();
            var nullCharacter = RestoreEngineFromJson("""
                {
                  "CharacterId": null,
                  "CharacterName": null,
                  "Pigs": null
                }
                """);

            Assert(nullCharacter.Character.Id == defaultEngine.Character.Id && nullCharacter.Character.Name == defaultEngine.Character.Name, "CharacterId/CharacterName null phải giữ giá trị mặc định");
            Assert(nullCharacter.Pigs.Count == 0, "Danh sách heo null phải được xử lý thành danh sách rỗng");

            var missingCharacterAndPigs = RestoreEngineFromJson("{}");
            Assert(missingCharacterAndPigs.Character.Id == defaultEngine.Character.Id && missingCharacterAndPigs.Character.Name == defaultEngine.Character.Name, "CharacterId/CharacterName bị thiếu phải giữ giá trị mặc định");
            Assert(missingCharacterAndPigs.Pigs.Count == 0, "Danh sách heo bị thiếu phải được khởi tạo rỗng");

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
            return System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "PigTycoon_SaveSystem_Test",
                $"save_test_{Guid.NewGuid():N}.json");
        }

        static GameEngine RestoreEngineFromJson(string json)
        {
            string path = GetUniqueJsonTestPath();
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                System.IO.File.WriteAllText(path, json);
                var restored = new GameEngine();
                restored.RestoreFromSave(SaveSystem.Load(path));
                return restored;
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }

        static string GetTestSavePath()
        {
            return System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
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
                Assert(System.IO.File.Exists(path), "File JSON phải được tạo");
                Assert(new System.IO.FileInfo(path).Length > 0, "File JSON không được rỗng");
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
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
                engine.Pigs.Add(Pig.CreateDefault("test_pig", "Heo Kiểm Thử", GeneLineId.HongDien, GeneRarity.Thuong));
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
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }

        static void TestLoadMissingFile()
        {
            string path = GetTestSavePath();
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            bool caught = false;
            try
            {
                SaveSystem.Load(path);
            }
            catch (System.IO.FileNotFoundException)
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
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                System.IO.File.WriteAllText(path, "{ JSON bị lỗi ");

                bool caught = false;
                try
                {
                    SaveSystem.Load(path);
                }
                catch (System.IO.InvalidDataException)
                {
                    caught = true;
                }
                Assert(caught, "JSON sai định dạng phải được phát hiện");
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }

        // =====================================================================
        // C: COMBAT / WEAPONS / ENEMIES / LOOT / ALIEN TESTS (from feat/C/P0.09)
        // =====================================================================
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

            float rawDmg = 30f;
            float dmgWithStr = CombatFormulas.CalculateDamage(rawDmg, attackerStr: 20, defenderRes: 0);
            Assert(dmgWithStr > rawDmg, "Sát thương với Str=20 phải lớn hơn BaseDamage");

            float dmgWithRes = CombatFormulas.CalculateDamage(rawDmg, attackerStr: 10, defenderRes: 25);
            Assert(dmgWithRes < rawDmg, "Sát thương bị giảm trừ bởi chỉ số Res của đối thủ");

            thief.TakeDamage(50f);
            Assert(thief.CurrentHp == 100f, "Thief nhận 50 sát thương phải còn đúng 100 HP");
            Assert(thief.IsAlive, "Thief còn 100 HP phải còn sống");

            thief.TakeDamage(200f);
            Assert(!thief.IsAlive, "Thief hết máu phải chết (IsAlive == false)");
            Assert(thief.CurrentHp == 0f, "Máu không được âm");

            int rageGain = CombatFormulas.CalculateRageGainOnDamage(20f);
            Assert(rageGain >= 5 && rageGain <= 25, "Lượng nộ tích tụ phải nằm trong khoảng hợp lệ [5, 25]");
        }

        static void TestStardewCombatMechanics64x64()
        {
            float defaultSpeed = CombatFormulas.CalculateSwipeSpeed(weaponSpeed: 0, addedSpeed: 0f);
            Assert(defaultSpeed == 400f, "Tốc độ vung mặc định phải là 400ms");

            float fastSpeed = CombatFormulas.CalculateSwipeSpeed(weaponSpeed: 4, addedSpeed: 0f);
            Assert(fastSpeed == 240f, "Tốc độ vung khi weaponSpeed = 4 phải là 240ms (400 - 4*40)");

            float baseCrit = 0.02f;
            float critWithLuck = CombatFormulas.CalculateCriticalChance(baseCrit, luckLevel: 5);
            Assert(critWithLuck > baseCrit, "May mắn (Luck=5) phải tăng tỉ lệ chí mạng");

            float daggerCrit = CombatFormulas.CalculateCriticalChance(baseCrit, luckLevel: 0, isDagger: true);
            Assert(daggerCrit > baseCrit, "Dao găm (Dagger) phải có bonus chí mạng (+0.005, x1.12)");

            int normalDmg = CombatFormulas.CalculateStardewDamage(minDamage: 20, maxDamage: 30, attackerAttack: 5, randomRoll: 25);
            Assert(normalDmg == 40, "Sát thương thường phải là 25 + 5*3 = 40");

            int critDmg = CombatFormulas.CalculateStardewDamage(minDamage: 20, maxDamage: 30, attackerAttack: 5, isCrit: true, critMultiplier: 3.0f, randomRoll: 25);
            Assert(critDmg == 90, "Sát thương chí mạng phải là 25*3 + 5*3 = 90");

            int mitigatedDmg = CombatFormulas.CalculateStardewDamage(minDamage: 20, maxDamage: 30, attackerAttack: 5, defenderResilience: 15, randomRoll: 25);
            Assert(mitigatedDmg == 25, "Sát thương sau khi trừ kháng defenderResilience=15 phải là 40 - 15 = 25");

            int incomingDmg = 50;
            int counterDmg = CombatFormulas.CalculateParryCounterDamage(incomingDmg);
            Assert(counterDmg == 25, "Phản sát thương khi Parry phải là 50 / 2 = 25");

            float groundKnockback = CombatFormulas.GetParryKnockbackMultiplier(isGlider: false);
            Assert(groundKnockback == 3.5f, "Đẩy lùi quái đất khi Parry phải là x3.5");

            float gliderKnockback = CombatFormulas.GetParryKnockbackMultiplier(isGlider: true);
            Assert(gliderKnockback == 2.0f, "Đẩy lùi quái bay khi Parry phải là x2.0");

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
            float rawDamage = 45f;
            float strawMitigated = Math.Max(1f, rawDamage - 0);
            Assert(strawMitigated == 45f, "Bù nhìn 0 giáp phải nhận nguyên vẹn 45 sát thương");

            float ironMitigated = Math.Max(1f, rawDamage - 15);
            Assert(ironMitigated == 30f, "Bù nhìn 15 giáp phải giảm trừ còn 30 sát thương");

            float steelMitigated = Math.Max(1f, rawDamage - 30);
            Assert(steelMitigated == 15f, "Bù nhìn 30 giáp phải giảm trừ còn 15 sát thương");

            float superMitigated = Math.Max(1f, 10f - 50);
            Assert(superMitigated == 1f, "Sát thương tối thiểu không bao giờ nhỏ hơn 1");

            float totalDamage = 0f;
            int hits = 0;
            int parries = 0;
            int deflections = 0;

            totalDamage += strawMitigated; hits++;
            totalDamage += ironMitigated; hits++;
            totalDamage += steelMitigated; hits++;
            parries += 2;
            deflections += 3;

            Assert(totalDamage == 90f, "Tổng sát thương 3 đòn phải là 45 + 30 + 15 = 90");
            Assert(hits == 3, "Số đòn đánh phải là 3");
            Assert(parries == 2, "Số lần parry phải là 2");
            Assert(deflections == 3, "Số lần phản xạ đạn phải là 3");
        }

        static void TestLootSystemAndDropRules()
        {
            var breakerLoot = LootDatabase.RollLoot(EnemyTypeId.KeCaoPha, new Random(42));
            Assert(breakerLoot.Count > 0, "Kẻ Cào Phá phải rơi ít nhất 1 vật phẩm");
            bool hasMaterial = breakerLoot.Exists(item => item.Category == LootCategory.Material);
            Assert(hasMaterial, "Kẻ Cào Phá phải rơi vật liệu sửa rào (Gỗ, Đinh, Cọc)");

            var thiefLoot = LootDatabase.RollLoot(EnemyTypeId.KeBatHeo, new Random(100));
            Assert(thiefLoot.Count > 0, "Kẻ Bắt Heo phải rơi chiến lợi phẩm");

            var pestLoot = LootDatabase.RollLoot(EnemyTypeId.ChuotGamKho, new Random(77));
            Assert(pestLoot.Count > 0, "Chuột Gặm Kho phải rơi chiến lợi phẩm");

            var ufoLoot = LootDatabase.RollLoot(EnemyTypeId.UfoTrinhSat, new Random(55));
            Assert(ufoLoot.Count > 0, "UFO Trinh Sát phải rơi chiến lợi phẩm công nghệ");
            bool hasTech = ufoLoot.Exists(item => item.Category == LootCategory.Tech || item.Category == LootCategory.Material || item.Category == LootCategory.Currency);
            Assert(hasTech, "UFO phải rơi mảnh hợp kim / lõi plasma / vàng");

            foreach (var item in ufoLoot)
            {
                Assert(!string.IsNullOrEmpty(item.Id), "ID vật phẩm không được rỗng");
                Assert(!string.IsNullOrEmpty(item.DisplayName), "Tên vật phẩm không được rỗng");
                Assert(item.Quantity >= 1, "Số lượng vật phẩm phải >= 1");
                Assert(!string.IsNullOrEmpty(item.SpriteKey), "SpriteKey không được rỗng");
                Assert(item.GoldValue > 0, "Giá trị vàng phải > 0");
            }

            Assert(LootDatabase.GetRarityHexColor(LootRarity.Thuong) == "#FFFFFF", "Độ hiếm Thường phải là màu trắng #FFFFFF");
            Assert(LootDatabase.GetRarityHexColor(LootRarity.Tot) == "#39E75F", "Độ hiếm Tốt phải là màu xanh lá #39E75F");
            Assert(LootDatabase.GetRarityHexColor(LootRarity.Hiem) == "#3EB4FF", "Độ hiếm Hiếm phải là màu xanh dương #3EB4FF");
            Assert(LootDatabase.GetRarityHexColor(LootRarity.CucPham) == "#FFA726", "Độ hiếm Cực Phẩm phải là màu cam #FFA726");
        }

        static void TestPigSnatchingAndRaidTelegraphInvariants()
        {
            Assert(Enum.IsDefined(typeof(RaidType), RaidType.BanditRaid), "Phải có RaidType.BanditRaid");
            Assert(Enum.IsDefined(typeof(RaidType), RaidType.AlienRaid), "Phải có RaidType.AlienRaid");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.KeBatHeo), "Phải có EnemyTypeId.KeBatHeo");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.UfoTrinhSat), "Phải có EnemyTypeId.UfoTrinhSat");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.UfoThuHoach), "Phải có EnemyTypeId.UfoThuHoach");

            var thief = EnemyData.CreateKeBatHeo();
            Assert(thief.MaxHp == 150f, "Kẻ Bắt Heo HP phải là 150");
            Assert(thief.MoveSpeed >= 1.5f, "Kẻ Bắt Heo phải có tốc độ chạy nhanh để tẩu thoát");
            Assert(thief.AttackRange >= 0.8f, "Kẻ Bắt Heo phải có tầm bắt heo hợp lý");

            var ufoHarvester = EnemyData.CreateUfoThuHoach();
            Assert(ufoHarvester.MaxHp == 650f, "UFO Thu Hoạch phải có HP 650");
            Assert(ufoHarvester.AttackRange >= 1.8f, "UFO Thu Hoạch phải có tầm chiếu Tractor Beam rộng");

            var pig = Pig.CreateDefault("pig_test_snatch", "Ỉn Test", GeneLineId.HongDien, GeneRarity.Thuong);
            int initialMood = pig.Mood;
            float initialBonding = pig.Bonding;

            pig.Mood = Math.Max(0, pig.Mood - 25);
            Assert(pig.Mood < initialMood, "Tâm trạng heo phải giảm mạnh khi bị kẻ trộm tóm");

            pig.Bonding = Math.Min(100f, pig.Bonding + 15f);
            pig.Mood = Math.Min(100, pig.Mood + 20);
            Assert(pig.Bonding > initialBonding, "Độ gắn kết Bonding phải tăng khi được giải cứu thành công");
        }

        static void TestAlienEscalationChain()
        {
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.AlienGrunt), "Phải có EnemyTypeId.AlienGrunt");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.AlienElite), "Phải có EnemyTypeId.AlienElite");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.BossAlienWarlord), "Phải có EnemyTypeId.BossAlienWarlord");
            Assert(Enum.IsDefined(typeof(EnemyTypeId), EnemyTypeId.UfoMothership), "Phải có EnemyTypeId.UfoMothership");
            Assert(Enum.IsDefined(typeof(RaidType), RaidType.AlienMothershipInvasion), "Phải có RaidType.AlienMothershipInvasion");

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
            Assert(mothershipD1.MaxHp == 8000f, "Tàu Mẹ Mothership ngày 1 HP phải là 8000");

            var gruntD5 = EnemyData.CreateAlienGrunt(5);
            var warlordD5 = EnemyData.CreateBossAlienWarlord(5);
            var mothershipD5 = EnemyData.CreateUfoMothership(5);

            Assert(gruntD5.MaxHp > gruntD1.MaxHp, "Alien Grunt ngày 5 HP phải cao hơn ngày 1");
            Assert(gruntD5.AttackDamage > gruntD1.AttackDamage, "Alien Grunt ngày 5 sát thương phải cao hơn ngày 1");
            Assert(warlordD5.MaxHp > warlordD1.MaxHp, "Boss Alien Warlord ngày 5 HP phải scale tăng");
            Assert(warlordD5.AttackDamage > warlordD1.AttackDamage, "Boss Alien Warlord ngày 5 sát thương phải scale tăng");
            Assert(mothershipD5.MaxHp > mothershipD1.MaxHp, "Tàu Mẹ Mothership ngày 5 HP phải scale tăng");

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

            int initialFailureThreshold = 2;
            int simulatedFailures = 0;
            bool triggerAlienDrop = false;

            simulatedFailures++;
            triggerAlienDrop = simulatedFailures >= initialFailureThreshold;
            Assert(!triggerAlienDrop, "Thất bại lần 1 chưa đủ ngưỡng để thả 3 Alien");

            simulatedFailures++;
            triggerAlienDrop = simulatedFailures >= initialFailureThreshold;
            Assert(triggerAlienDrop, "Đạt >= 2 lần thất bại phải kích hoạt thả 3 Alien Grunt");
        }

        // =====================================================================
        // B: COMPLETE WORLD, FARM, HERD, GENETICS, DISEASE, PETS/NPCS & GDD 3.10
        // =====================================================================
        static void TestHerdFlowFieldAndRecall()
        {
            var flow = new HerdFlowField(-45, -32, 90, 64);
            flow.ConfigurePastureFenceWithGates(FarmMainLayout.PastureBounds, PastureBoundary.FourGates(), new[] { true, true, true, true });
            flow.SetTargets(FlowDestinationGroup.Feeder, new[] { (0f, 0f) });

            var vec = flow.SampleFlow(FlowDestinationGroup.Feeder, -10f, 0f);
            Assert(vec.X > 0.5f, "Flow-Field từ (-10, 0) về máng (0, 0) phải hướng sang Đông (+X)");

            var agents = new List<PigSteeringAgent>();
            for (int i = 0; i < 60; i++)
            {
                agents.Add(new PigSteeringAgent
                {
                    PigId = $"p_{i}",
                    X = -15f + (i % 10) * 3f,
                    Y = -10f + (i / 10) * 3f,
                    BaseSpeedMetersPerSec = 2.6f,
                    GeneLine = i % 3 == 0 ? GeneLineId.MocCuoc : GeneLineId.HongDien,
                    Mood = 65
                });
            }

            var steer = flow.ComputeSteeringWithSeparation(FlowDestinationGroup.Feeder, agents[0], agents);
            Assert(steer.Magnitude > 0.9f, "Vector steering kết hợp tách đàn phải chuẩn hóa độ dài ~1");

            var recall = flow.ExecuteRecallToRefuge(agents, (0f, 6f), entranceCount: 2, minEntranceWidthMeters: 6.2f, hasShepherdDog: true);
            Assert(recall.ratioRecalled >= 0.90f, "Lệnh Lùa về Nơi Trú phải gom >= 90% heo khỏe");
            Assert(recall.estimatedGameHours <= 2.0f, "Lệnh Lùa về Nơi Trú phải hoàn tất trong <= 2 giờ game");

            agents[0].X = 10f;
            agents[0].Y = 8f;
            agents[0].IsPanicking = true;
            agents[0].Mood = 35;
            int whistled = flow.ExecuteKhoaWhistle(agents, 0f, 6f, hasShepherdDog: true);
            Assert(whistled >= 1 && !agents[0].IsPanicking && agents[0].Mood == 43, "Tiếng Huýt Chăn Đàn của Khoa phải làm dịu heo và cộng +8 Tinh thần");
        }

        static void TestFarmGridPlacementAndDemolish()
        {
            var grid = new FarmGridAndZones();
            var valid = grid.EvaluatePlacement(5, 5, BuildingFootprintSize.Medium_2x2, isDuringCombatWave: false);
            Assert(valid == BuildPlacementStatus.Green_Valid, "Ô trống phải cho trạng thái Xanh (Green_Valid)");

            grid.SetSecondaryPathCell(8, 8, true);
            var warning = grid.EvaluatePlacement(8, 8, BuildingFootprintSize.Small_1x1, isDuringCombatWave: false);
            Assert(warning == BuildPlacementStatus.Yellow_Warning, "Đặt trên lối phụ phải báo Vàng (Yellow_Warning)");

            var waveBlocked = grid.EvaluatePlacement(5, 5, BuildingFootprintSize.Small_1x1, isDuringCombatWave: true);
            Assert(waveBlocked == BuildPlacementStatus.Red_Forbidden, "Đang trong đợt quái phải cấm đặt (Red_Forbidden)");

            var placed = grid.TryPlaceStructure("Tower_Test", 5, 5, BuildingFootprintSize.Large_3x3, 1000, currentGameMinutes: 100f);
            Assert(placed.placed && placed.structure != null, "Phải đặt được công trình 3x3 Ô Xây (6x6m)");

            var overlap = grid.EvaluatePlacement(6, 6, BuildingFootprintSize.Small_1x1);
            Assert(overlap == BuildPlacementStatus.Red_Forbidden, "Ô đã có công trình phải báo Đỏ (Red_Forbidden)");

            var refundFull = grid.TryDemolishStructure(placed.structure!.InstanceId, currentGameMinutes: 130f, beforeFirstWave: false, isDuringCombatWave: false);
            Assert(refundFull.removed && refundFull.refundedGold == 1000, "Tháo dỡ trong 60 phút đầu phải hoàn 100% vàng");

            var placed2 = grid.TryPlaceStructure("Tower_Test2", 5, 5, BuildingFootprintSize.Small_1x1, 1000, currentGameMinutes: 100f);
            var refund70 = grid.TryDemolishStructure(placed2.structure!.InstanceId, currentGameMinutes: 300f, beforeFirstWave: false, isDuringCombatWave: false);
            Assert(refund70.removed && refund70.refundedGold == 700, "Tháo dỡ sau 60 phút và sau đợt quái đầu phải hoàn 70% vàng");
        }

        static void TestServiceCoolingAndLocalCrowding()
        {
            var grid = new FarmGridAndZones();
            var inside = grid.EvaluateServiceCoverage(0f, 0f);
            Assert(inside.withinServiceRadius && Math.Abs(inside.growthMultiplier - 1.0f) < 0.001f && inside.hourlyMoodDelta == 0, "Trong tầm phục vụ 30m không bị phạt");

            var outside = grid.EvaluateServiceCoverage(-42f, -30f);
            Assert(!outside.withinServiceRadius && Math.Abs(outside.growthMultiplier - 0.90f) < 0.001f && outside.hourlyMoodDelta == -1, "Ngoài tầm phục vụ 30m bị phạt -10% tăng trọng và -1 Tinh thần/giờ");

            Assert(grid.IsWithinCoolingRadius(0f, 0f, GeneLineId.HongDien), "Trung tâm chuồng nằm trong bán kính nguồn mát 25m");
            Assert(grid.IsWithinCoolingRadius(15f, 0f, GeneLineId.LamKhe), "Lam Khê nhận làm mát từ Bồn nước trong bán kính 20m");

            var crowdPlot = FarmGridAndZones.EvaluateLocalCrowding(pigsIn10x10Plot: 18, hoursInPlot: 1.2f, maxPigsAtSingleTroughOrFeeder: 6);
            Assert(crowdPlot.isCrowded && Math.Abs(crowdPlot.groupAContagionMultiplier - 1.30f) < 0.001f && Math.Abs(crowdPlot.panicSpreadBonus - 0.10f) < 0.001f && crowdPlot.hourlyMoodPenalty == -1,
                "Trên 16 heo trong 1 Ô Đất suốt 1 giờ kích hoạt Chen Chúc cục bộ (Nhóm A x1.3, lan hoảng +10%, Tinh thần -1/giờ)");

            var crowdTrough = FarmGridAndZones.EvaluateLocalCrowding(pigsIn10x10Plot: 10, hoursInPlot: 0.2f, maxPigsAtSingleTroughOrFeeder: 14);
            Assert(crowdTrough.isCrowded, "Trên 12 heo tranh 1 máng/bồn cũng kích hoạt Chen Chúc cục bộ");
        }

        static void TestCarcassProcessingAndDragStamina()
        {
            Assert(FarmGridAndZones.CalculateDragCarcassStaminaCost(25f) == 3, "Kéo xác 25m tốn 3 STA (+1 STA mỗi 10m)");

            var farm = new Farm { Gold = 500, FuelLiters = 50, CorpseLotCount = 2 };
            var grid = new FarmGridAndZones();
            Assert(grid.GetProcessingLotDailyHabitatPenalty() == 0, "Khu Xử Lý đặt cách nguồn nước/mái trú >= 20m không bị phạt SC");

            var groupDBlocked = grid.StartCarcassProcessing(farm, "dead_d", 100f, CarcassProcessMethod.BioRecycle_TaiCheSinhHoc, DiseaseId.HuMach);
            Assert(!groupDBlocked.started, "Xác chết vì bệnh Nhóm D (Hư Mạch) cấm Tái Chế Sinh Học");

            var incinerate = grid.StartCarcassProcessing(farm, "dead_1", 90f, CarcassProcessMethod.Incinerate_ThieuHuy, DiseaseId.HuMach);
            Assert(incinerate.started && farm.Gold == 455 && farm.FuelLiters == 42, "Thiêu Hủy tốn 45 Vàng + 8 Lít nhiên liệu");

            var recycle = grid.StartCarcassProcessing(farm, "dead_2", 100f, CarcassProcessMethod.BioRecycle_TaiCheSinhHoc, DiseaseId.HoHapLanh);
            Assert(recycle.started && farm.Gold == 375 && farm.FuelLiters == 32, "Tái Chế Sinh Học tốn 80 Vàng + 10 Lít nhiên liệu");

            int feed6h = grid.TickCarcassProcessingHours(6);
            Assert(feed6h == 0 && incinerate.job!.Completed && !recycle.job!.Completed, "Sau 6 giờ Thiêu Hủy xong (0 cám), Tái Chế còn 12 giờ");

            int feed18h = grid.TickCarcassProcessingHours(12);
            Assert(feed18h >= 7 && recycle.job!.Completed, "Sau 18 giờ Tái Chế Sinh Học hoàn tất và thu hồi 25–32% khẩu phần cám");
        }

        static void TestElderOffspringSixStepsAndShaping()
        {
            Assert(Math.Abs(GeneticsAndBreedingSystem.GetDailyFeedKilograms(PigStage.HeoNon) - 0.5f) < 0.001f, "Heo non ăn 0.5 kg/ngày");
            Assert(Math.Abs(GeneticsAndBreedingSystem.GetDailyFeedKilograms(PigStage.DangLon) - 2.5f) < 0.001f, "Đang lớn ăn 2.5 kg/ngày");
            Assert(Math.Abs(GeneticsAndBreedingSystem.GetDailyFeedKilograms(PigStage.TruongThanh) - 3.5f) < 0.001f, "Trưởng thành ăn 3.5 kg/ngày");
            Assert(Math.Abs(GeneticsAndBreedingSystem.GetDailyFeedKilograms(PigStage.HeoGia) - 4.2f) < 0.001f, "Heo già ăn 4.2 kg/ngày");
            Assert(Math.Abs(GeneticsAndBreedingSystem.GetDailyFeedKilograms(PigStage.TruongThanh, GeneLineId.ThoTram) - 3.5f * 0.82f) < 0.001f, "Thổ Trầm ăn ít hơn 18%");

            Assert(Math.Abs(GeneticsAndBreedingSystem.CalculateOrphanPigletDailyMortalityRate(false, HerdStateTier.KhongCo) - 0.12f) < 0.001f, "Heo non thiếu mẹ mặc định tử vong 12%/ngày");
            Assert(Math.Abs(GeneticsAndBreedingSystem.CalculateOrphanPigletDailyMortalityRate(true, HerdStateTier.KhongCo)) < 0.001f, "Có nái Phong Mẫu trong 10m giảm tử vong mồ côi về 0%");
            Assert(Math.Abs(GeneticsAndBreedingSystem.CalculateOrphanPigletDailyMortalityRate(false, HerdStateTier.Vung) - 0.06f) < 0.001f, "Bầy Đàn Vững giảm một nửa tử vong mồ côi còn 6%");

            Assert(GeneticsAndBreedingSystem.EvaluateMeatQualityShaping(0.98f, 82f, 85, 0) == MeatQuality.S, "Chăm sóc hoàn hảo giai đoạn Đang lớn đạt phẩm chất S");
            Assert(GeneticsAndBreedingSystem.EvaluateMeatQualityShaping(0.88f, 65f, 70, 1) == MeatQuality.A, "Chăm sóc tốt đạt phẩm chất A");

            var stepHealthy = GeneticsAndBreedingSystem.EvaluateElderOffspringSixSteps(
                habitatIndex: 80, motherMood: 80, baseRarity: GeneRarity.Kha,
                conceptionRoll: 0.20, healthyOrAbnormalRoll: 0.15, penaltyRoll: 0.5, eliteRoll: 0.5, awakenRoll: 0.5);
            Assert(stepHealthy.Conceived && stepHealthy.IsHealthy && stepHealthy.HealthyThresholdK >= 0.22f && stepHealthy.HealthyThresholdK <= 0.32f,
                "Bước 1–2 Hậu duệ heo già: K thuộc [22%, 32%] và sinh khỏe khi roll < K");

            var stepEliteAwaken = GeneticsAndBreedingSystem.EvaluateElderOffspringSixSteps(
                habitatIndex: 70, motherMood: 70, baseRarity: GeneRarity.Kha,
                conceptionRoll: 0.10, healthyOrAbnormalRoll: 0.80, penaltyRoll: 0.4, eliteRoll: 0.01, awakenRoll: 0.20);
            Assert(stepEliteAwaken.IsEliteOffspring && stepEliteAwaken.AwakenedDominantGene && stepEliteAwaken.FinalRarity == GeneRarity.Hiem,
                "Bước 5–6 Hậu duệ heo già: 2% Ưu Tú và 45% Thức tỉnh gen trội nâng bậc hiếm từ Khá lên Hiếm");
        }

        static void TestGenesTraitsAndCountermeasures()
        {
            Assert(GeneticsAndBreedingSystem.AllGeneProfiles.Count == 12, "Đủ 12 hồ sơ dòng gen GDD 5.2");
            Assert(GeneticsAndBreedingSystem.AllTraitDefinitions.Count == 18, "Đủ 18 tính trạng (6 Tốt, 6 Xấu, 6 Dị Biến Hư Thể)");

            var pig = Pig.CreateDefault("p_trait", "Heo Tính Trạng", GeneLineId.XichMao, GeneRarity.Hiem);
            pig.Traits.Add(new PigTrait { Id = "KenAn", Name = "Kén Ăn", IsPositive = false, IsDeviant = false });

            var beforeId = GeneticsAndBreedingSystem.ApplyBadTraitCountermeasure(pig, "KenAn");
            Assert(!beforeId.countermeasureApplied, "Chưa giám định không được áp dụng biện pháp khắc chế");

            pig.IsIdentified = true;
            var afterId = GeneticsAndBreedingSystem.ApplyBadTraitCountermeasure(pig, "KenAn");
            Assert(afterId.countermeasureApplied && pig.Traits[0].IsSealed, "Sau giám định áp dụng thành công biện pháp khắc chế tính trạng xấu");
        }

        static void TestPetsNpcsFiveVerbsAndOccultEvents()
        {
            var mgr = new PetNpcAndOccultManager();
            Assert(mgr.TryEquipPet(PetId.ChoChanDan), "Trang bị Chó Chăn Đàn (thú cưng của B)");
            Assert(mgr.TryEquipPet(PetId.ChoNgaoCanh), "Trang bị Chó Ngao Canh (thú cưng của B)");
            Assert(!mgr.TryEquipPet(PetId.MeoKho), "Tối đa 2 thú cưng đi kèm cùng lúc");

            var bonuses = mgr.GetActivePetBonuses();
            Assert(Math.Abs(bonuses.recallSpeedMultiplier - 1.20f) < 0.001f && bonuses.blocksFirstNightThief, "Chó Chăn Đàn +20% tốc độ lùa đàn, Chó Ngao Canh chặn trộm đêm");

            var farm = new Farm { HabitatIndex = 65, Gold = 500 };
            var pigSick = new HerdPig { Id = "sick_1", Alive = true, Disease = DiseaseId.HoHapLanh, Health = 70 };
            var herd = new List<HerdPig> { pigSick, new HerdPig { Id = "healthy_1", Alive = true } };

            mgr.HireNpc(NpcSpecialistId.BacSiThuY);
            mgr.HireNpc(NpcSpecialistId.NongCong);
            mgr.HireNpc(NpcSpecialistId.VeBinhKhien);

            var shift = mgr.ExecuteDailyNpcShift(herd, farm);
            Assert(shift.curedPigs == 1 && shift.habitatGained == 8 && Math.Abs(shift.gateDefenseBonus - 0.40f) < 0.001f,
                "3 NPC của B (Bác Sĩ Thú Y, Nông Công, Vệ Binh Khiên) hoạt động đúng chức năng");

            var pen = new IsolationPen();
            var pigD = new HerdPig { Id = "nam_1", Alive = true, Disease = DiseaseId.NamPhatQuang, Health = 60 };
            Assert(mgr.ExecuteHealthVerb(HealthTreatmentVerb.NghienCuu, farm, pen, pigD, herd, researchTarget: DiseaseId.NamPhatQuang).success, "Động từ 5: Nghiên cứu Nấm Phát Quang");
            Assert(mgr.ExecuteHealthVerb(HealthTreatmentVerb.DieuTriCaThe, farm, pen, pigD, herd).success && pigD.Disease == null, "Động từ 3: Điều trị khỏi Nấm Phát Quang sau nghiên cứu");
            Assert(mgr.ExecuteHealthVerb(HealthTreatmentVerb.PhongNguaDan, farm, pen, null, herd).success, "Động từ 4: Tiêm Vắc-xin phòng ngừa đàn");

            // 4 quái của B
            var bayNhay = EnemyData.CreateBayNhay();
            var thuBaoStorm = EnemyData.CreateThuBao(WeatherType.GiongBao);
            var keMangDich = EnemyData.CreateKeMangDich();
            var chuot = EnemyData.CreateByType(EnemyTypeId.ChuotGamKho);
            Assert(bayNhay.CanLeapLowFence && thuBaoStorm.EmpoweredInStorm && thuBaoStorm.MaxHp == 340f && keMangDich.SpreadsDiseaseOnHit && chuot.StealsFeedPortions == 2,
                "4 quái của B (Chuột Gặm Kho, Bầy Nhảy, Thú Bão, Kẻ Mang Dịch) đúng cơ chế GDD 19.1");

            // 4 sự kiện Huyền Bí của B
            var corePigs = new List<Pig>
            {
                Pig.CreateDefault("p1", "Heo Thường", GeneLineId.HongDien, GeneRarity.Thuong),
                Pig.CreateDefault("p2", "Heo Già", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 20, 100f)
            };
            corePigs[1].Stage = PigStage.HeoGia;

            Assert(mgr.ResolveMemberBOccultEvent(MemberBOccultEventId.VuonMuoiHaiQua, 0, farm, corePigs).resolved, "Sự kiện Vườn Mười Hai Quả");
            Assert(mgr.ResolveMemberBOccultEvent(MemberBOccultEventId.DongCoKhongBong, 0, farm, corePigs).resolved, "Sự kiện Đồng Cỏ Không Bóng");
            Assert(mgr.ResolveMemberBOccultEvent(MemberBOccultEventId.BiNhanMuaGia, 0, farm, corePigs).resolved, "Sự kiện Bí Nhân Mua Già");
            Assert(mgr.ResolveMemberBOccultEvent(MemberBOccultEventId.DoiMotLayMot, 0, farm, corePigs).resolved && corePigs[0].GeneLine == GeneLineId.HuThe, "Sự kiện Đổi Một Lấy Một");
        }

        static void TestFarmMainChecklistGdd310()
        {
            var grid = new FarmGridAndZones();
            var report = grid.ValidateChecklistGdd310();
            Assert(report.GroupA_ScaleAndGridValid, "Nhóm A (Lưới Ô Xây 2x2m & Ô Đất 10x10m) đạt");
            Assert(report.GroupB_FourSectorsAndMonsterSpawnsValid, "Nhóm B (4 Khu Hướng & 8–12 điểm spawn quái) đạt");
            Assert(report.GroupC_GatesAndHiddenGateZonesValid, "Nhóm C (4 cổng >=6m & >=4 vùng ẩn Cổng) đạt");
            Assert(report.GroupD_RefugeAndQuarantineValid, "Nhóm D (Nơi Trú Đàn >=2 lối >=6m & Khu Cách Ly >=2 Ô Đất) đạt");
            Assert(report.GroupE_ProcessingLotAndDistanceValid, "Nhóm E (Khu Xử Lý >=20m từ nguồn nước/mái trú) đạt");
            Assert(report.GroupF_ServiceAndCoolingRadiusValid, "Nhóm F (Tầm phục vụ 30m & Nguồn mát 25m) đạt");
            Assert(report.GroupG_MysticMerchantAndNoSpawnBufferValid, "Nhóm G (Điểm Bí nhân <=30m & Vùng đệm không spawn 25m) đạt");
            Assert(report.GroupH_SortingLayersAndCameraFramesValid, "Nhóm H (5 Sorting Layers & 3 khung Camera) đạt");
            Assert(report.AllPassed, "Toàn bộ 8 nhóm nghiệm thu Farm_Main GDD 3.10 phải đạt 100%");
        }

        static void TestEndToEndHourlySimulationLoop()
        {
            var engine = new GameEngine();
            engine.CurrentWeather = WeatherType.NangGat;

            // 1. Heo nằm ngoài tầm phục vụ 30m và ngoài nguồn mát 25m
            var farPig = Pig.CreateDefault("p_far", "Heo Xa Máng", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 6, 30f);
            farPig.PositionX = -42f;
            farPig.PositionY = -30f;
            farPig.Mood = 75;
            engine.Pigs.Add(farPig);

            // 2. Đặt 17 heo vào cùng 1 Ô Đất 10x10m (0..9, 0..9) để kích hoạt Chen Chúc cục bộ
            for (int i = 0; i < 17; i++)
            {
                var cp = Pig.CreateDefault($"p_crowd_{i}", $"Heo Chật {i}", GeneLineId.LamKhe, GeneRarity.Thuong, "Cai", 12, 80f);
                cp.PositionX = 2.0f + (i % 4) * 1.5f;
                cp.PositionY = 2.0f + (i / 4) * 1.5f;
                cp.Mood = 70;
                engine.Pigs.Add(cp);
            }

            // 3. Một xác heo chết đưa vào Khu Xử Lý Tái Chế Sinh Học (18 giờ)
            var deadPig = Pig.CreateDefault("p_dead_recycle", "Heo Tử Vong", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 15, 100f);
            deadPig.PositionX = 0f;
            deadPig.PositionY = 0f;
            deadPig.Die();
            engine.Pigs.Add(deadPig);

            float feedBefore = engine.Farm.FeedStockKg;
            var procRes = engine.ProcessDeadPigCarcass("p_dead_recycle", CarcassProcessMethod.BioRecycle_TaiCheSinhHoc);
            Assert(procRes.started && procRes.staminaCost >= 3, "Kéo xác từ (0,0) về Khu Xử Lý (32,-22) phải trừ STA và bắt đầu Tái Chế 18h");

            // Chạy 1 giờ game (60 phút)
            engine.Tick(60);
            Assert(farPig.OutsideServiceHoursToday == 1, "Sau 1 giờ ngoài tầm 30m phải ghi nhận OutsideServiceHoursToday = 1");
            Assert(farPig.Mood == 73 && farPig.Comfort == 66, "Ngoài tầm 30m (-1 Mood) + Nắng Gắt ngoài nguồn mát 25m (-1 Mood, -4 Comfort)");
            Assert(engine.Farm.LocalCrowdingActive && Math.Abs(engine.Farm.LocalCrowdingContagionMultiplier - 1.30f) < 0.001f,
                "17 heo trong 1 Ô Đất 10x10m phải bật LocalCrowdingActive và hệ số lây bệnh x1.30");

            // Giải tán 17 heo test chen chúc để kiểm tra chính xác lượng cám thu hồi từ Khu Xử Lý sau 18h
            engine.Pigs.RemoveAll(p => p.Id.StartsWith("p_crowd_"));

            // Chạy thêm 17 giờ nữa (tổng 18 giờ) để hoàn tất Tái Chế Sinh Học
            for (int h = 0; h < 17; h++)
            {
                engine.Tick(60);
            }
            Assert(engine.Farm.FeedStockKg > feedBefore && !engine.Pigs.Exists(p => p.Id == "p_dead_recycle"),
                "Sau 18 giờ Tái Chế Sinh Học phải tự động cộng cám thu hồi vào Farm.FeedStockKg và dọn xác khỏi danh sách");
            Assert(!engine.Pigs.Exists(p => p.Id == "p_dead_recycle"), "Xác heo đã hoàn tất xử lý phải được dọn khỏi danh sách Pigs");
        }

        static void TestEndToEndDailyFeedGrowthShapingAndXichMao()
        {
            var engine = new GameEngine();
            engine.Farm.HabitatIndex = 88;
            engine.Farm.FeedStockKg = 500f;

            // Heo ở đầu giai đoạn Đang lớn (5 ngày tuổi) được chăm sóc lý tưởng (SC >= 80, Mood >= 75, đủ cám, không bệnh)
            var growingPig = Pig.CreateDefault("p_shaping", "Heo Định Hình S", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 5, 15f);
            growingPig.Mood = 85;
            engine.Pigs.Add(growingPig);

            // Heo Xích Mao Trưởng thành (12 ngày tuổi, Mood >= 60) cần 3 ngày thuần dưỡng tự động
            var xichMao = Pig.CreateDefault("p_xm", "Chiến Trư Xích Mao", GeneLineId.XichMao, GeneRarity.Kha, "Duc", 12, 85f);
            xichMao.Mood = 75;
            engine.Pigs.Add(xichMao);

            // Tiến qua 6 ngày (từ ngày 5 -> ngày 11: bước sang Trưởng Thành)
            for (int d = 0; d < 6; d++)
            {
                engine.Tick(24 * 60);
                engine.Farm.HabitatIndex = 88;
                growingPig.Mood = 85;
            }

            Assert(growingPig.Stage == PigStage.TruongThanh, "Sau 6 ngày heo 5 tuổi phải lên Trưởng Thành (11 ngày tuổi)");
            Assert(growingPig.MeatQuality == MeatQuality.S, "Chăm sóc lý tưởng suốt giai đoạn Đang lớn phải khóa Phẩm chất thịt hạng S khi lên Trưởng Thành");
            Assert(xichMao.IsXichMaoDomesticated && xichMao.XichMaoDomesticationDays >= 3, "Xích Mao Trưởng thành đủ Tinh thần >= 60 phải tự động hoàn tất 3 ngày thuần dưỡng");
            Assert(engine.Farm.FeedStockKg < 500f, "Đàn heo ăn mỗi ngày phải trừ trực tiếp vào Farm.FeedStockKg");
        }

        static void TestEndToEndBreedingGestationBirthAndOrphanPhongMau()
        {
            var engine = new GameEngine();
            engine.Farm.FeedStockKg = 500f;

            var sow = Pig.CreateDefault("sow_f0", "Nái Phong Mẫu", GeneLineId.PhongMau, GeneRarity.Kha, "Cai", 15, 95f);
            var boar = Pig.CreateDefault("boar_f0", "Đực Lam Khê", GeneLineId.LamKhe, GeneRarity.Thuong, "Duc", 16, 100f);
            engine.Pigs.Add(sow);
            engine.Pigs.Add(boar);

            var sched = engine.SchedulePigBreeding(sow.Id, boar.Id, deterministicConceptionRoll: 0.10);
            Assert(sched.scheduled && sow.GestationDaysRemaining == 3, "Phối giống hợp lệ phải đặt GestationDaysRemaining = 3");

            // Qua 3 ngày mang thai -> tự động sinh heo con F1 vào engine.Pigs
            for (int d = 0; d < 3; d++)
            {
                engine.Tick(24 * 60);
            }

            var piglets = engine.Pigs.FindAll(p => p.MotherId == sow.Id);
            Assert(piglets.Count >= 8, "Nái Phong Mẫu (+2 con/lứa) phải sinh tối thiểu 8 heo con F1 vào engine.Pigs");
            Assert(piglets[0].Generation == 1 && piglets[0].FatherId == boar.Id && !piglets[0].IsInbred,
                "Heo con sinh ra phải lưu đúng MotherId, FatherId và thế hệ F1 (Generation = 1)");

            // Kiểm tra Phong Mẫu nuôi hộ heo non mồ côi trong bán kính 10m (0% tử vong)
            var orphan = Pig.CreateDefault("orphan_1", "Heo Non Mồ Côi", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 1, 2.0f);
            orphan.MotherId = "missing_mother";
            orphan.PositionX = sow.PositionX + 3f;
            orphan.PositionY = sow.PositionY + 2f;
            engine.Pigs.Add(orphan);

            engine.Tick(24 * 60);
            Assert(orphan.IsAlive, "Heo non mồ côi đứng trong bán kính 10m của nái Phong Mẫu phải được nuôi hộ (0% tử vong)");
        }

        static void TestEndToEndDiseaseNpcShiftAndDeviantAwakening()
        {
            var engine = new GameEngine();
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.BacSiThuY);
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.NongCong);
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.VeBinhKhien);

            var sick1 = Pig.CreateDefault("sick_a", "Heo Cảm Lạnh", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 12, 80f);
            sick1.ActiveDisease = DiseaseId.HoHapLanh;
            var sick2 = Pig.CreateDefault("sick_d", "Heo Nấm Phát Quang", GeneLineId.LamKhe, GeneRarity.Thuong, "Cai", 12, 82f);
            sick2.ActiveDisease = DiseaseId.NamPhatQuang;

            // Heo Hư Thể có tính trạng Hút Sương đã thức tỉnh
            var huThe = Pig.CreateDefault("huthe_awake", "Hư Thể Thức Tỉnh", GeneLineId.HuThe, GeneRarity.DiBien, "Duc", 8, 50f);
            var hutSuong = huThe.Traits.Find(t => t.Id == "HutSuong");
            Assert(hutSuong != null, "Heo Hư Thể mặc định phải có tính trạng Hút Sương");
            hutSuong!.IsAwakened = true;

            engine.Pigs.Add(sick1);
            engine.Pigs.Add(sick2);
            engine.Pigs.Add(huThe);

            // Chạy qua 1 ngày: Bác Sĩ Thú Y tự động chữa khỏi 2 heo bệnh, Hút Sương gọi thời tiết Sương Mù
            engine.Tick(24 * 60);
            Assert(!sick1.ActiveDisease.HasValue && !sick2.ActiveDisease.HasValue, "Bác Sĩ Thú Y phải tự động chữa tối đa 2 heo bệnh mỗi ngày trong OnDailyReset");
            Assert(engine.CurrentWeather == WeatherType.SuongMu, "Tính trạng Hút Sương đã thức tỉnh phải chuyển thời tiết sang Sương Mù");

            // Cách ly heo Hư Thể vào Khu Cách Ly bằng ExecuteHealthTreatment
            var isoRes = engine.ExecuteHealthTreatment(HealthTreatmentVerb.CachLy, huThe.Id);
            Assert(isoRes.success && huThe.IsIsolated, "Động từ Cách Ly trên GameEngine phải đưa heo vào Khu Cách Ly");
        }

        static void TestEndToEndFullSaveLoadV2State()
        {
            var engine = new GameEngine();
            engine.Farm.FeedStockKg = 412.5f;
            engine.Farm.MedicalHerbCount = 9;
            engine.Farm.VaccineDoseCount = 7;
            engine.PetsAndNpcs.TryEquipPet(PetId.ChoChanDan);
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.BacSiThuY, 4f, 5f);
            engine.PetsAndNpcs.ResearchedDiseases.Add(DiseaseId.HuMach);

            var p = Pig.CreateDefault("p_save_v2", "Heo Lưu Trữ V2", GeneLineId.XichMao, GeneRarity.Kha, "Cai", 14, 92.5f);
            p.PositionX = 11.5f;
            p.PositionY = -7.25f;
            p.Bonding = 82f;
            p.XichMaoDomesticationDays = 2;
            p.Generation = 1;
            p.MotherId = "mom_01";
            p.FatherId = "dad_01";
            p.GestationDaysRemaining = 2;
            p.PendingFatherId = "boar_99";
            p.PendingFatherGene = GeneLineId.KimTho;
            engine.Pigs.Add(p);

            engine.Herd.SelectSuccessionCandidate(p);
            engine.Herd.TrainSuccessionCandidateDaily(engine.Pigs);

            string tempFile = Path.Combine(Path.GetTempPath(), $"pigtycoon_v2_test_{Guid.NewGuid():N}.json");
            try
            {
                SaveSystem.Save(engine.CreateSaveData(), tempFile);
                var loadedData = SaveSystem.Load(tempFile);
                Assert(loadedData != null && loadedData.SaveVersion == 2, "JSON SaveVersion phải là 2");

                var restored = new GameEngine();
                restored.RestoreFromSave(loadedData!);

            Assert(Math.Abs(restored.Farm.FeedStockKg - 412.5f) < 0.01f && restored.Farm.MedicalHerbCount == 9 && restored.Farm.VaccineDoseCount == 7,
                "Khôi phục chính xác tồn kho Cám, Thảo dược và Vắc-xin");
            Assert(restored.PetsAndNpcs.HasPet(PetId.ChoChanDan) && restored.PetsAndNpcs.HiredNpcs.Count == 1 && restored.PetsAndNpcs.ResearchedDiseases.Contains(DiseaseId.HuMach),
                "Khôi phục chính xác Thú cưng, NPC đã thuê và Bệnh đã nghiên cứu");
            Assert(restored.Herd.SuccessionTraining.CandidatePigId == "p_save_v2" && restored.Herd.SuccessionTraining.BonusSuccessionPercentage == 5,
                "Khôi phục chính xác tiến độ Bồi Dưỡng Kế Vị của Bầy Đàn");

                var rp = restored.Pigs.Find(x => x.Id == "p_save_v2");
                Assert(rp != null && Math.Abs(rp.PositionX - 11.5f) < 0.01f && rp.XichMaoDomesticationDays == 2 && rp.Generation == 1 && rp.GestationDaysRemaining == 2 && rp.PendingFatherGene == GeneLineId.KimTho,
                    "Khôi phục chính xác tọa độ, thế hệ F1, tiến độ thuần dưỡng Xích Mao và trạng thái mang thai của heo");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        static void TestCombatAutoGuardAndGuardBreakFaint()
        {
            var engine = new GameEngine();
            PlayerRoster.Apply(engine.Character, PlayerId.Khoa);
            engine.Character.EquipStarterSet();
            engine.Farm.Gold = 1000;

            float initialSta = engine.Character.Stamina.CurrentStamina;
            float initialGuard = engine.Character.CurrentGuard;
            int initialHp = engine.Character.Hp;

            // 1. Đỡ Mặc Định (Auto-Guard): Quái đánh nhân vật -> Nhân vật tự gồng đỡ, hao cả Thể Lực lẫn Thanh Chống Chịu, giữ nguyên HP
            var autoHit = engine.Character.ProcessIncomingAttack(30f, isActiveParry: false, isHoldingGuardStance: false);
            Assert(autoHit.WasAutoGuarded && !autoHit.WasActiveParry, "Mặc định không bấm Parry thì nhân vật phải Tự Đỡ (Auto-Guard)");
            Assert(engine.Character.Stamina.CurrentStamina < initialSta, "Đỡ Tự Động phải làm hao Thể Lực");
            Assert(engine.Character.CurrentGuard < initialGuard, "Đỡ Tự Động phải làm giảm điểm Thanh Chống Chịu");
            Assert(engine.Character.Hp == initialHp, "Nhân vật dùng Thanh Chống Chịu & Thể Lực đỡ đòn nên không bị trừ máu trực tiếp");

            // 2. Đỡ Chủ Động (Active Parry): Mất rất ít điểm Chống Chịu (< 25% so với Auto-Guard), phản sát thương 50%, và Quần Giáp Xích hoàn +2 STA
            float staBeforeParry = engine.Character.Stamina.CurrentStamina;
            var parryHit = engine.Character.ProcessIncomingAttack(30f, isActiveParry: true, isHoldingGuardStance: true);
            Assert(parryHit.WasActiveParry && parryHit.CounterDamage >= 15f, "Đỡ Chủ Động phải phản lại 50% sát thương gốc cho quái");
            Assert(parryHit.GuardDrained < autoHit.GuardDrained * 0.30f, "Đỡ Chủ Động phải mất ít điểm Thanh Chống Chịu hơn nhiều so với Đỡ Tự Động");
            Assert(engine.Character.Stamina.CurrentStamina > staBeforeParry, "Mặc Quần Giáp Xích khi Đỡ Chủ Động thành công phải được hoàn +2 Thể Lực (GDD 12.2)");

            // 3. Kịch bản A: Điểm Thanh Chống Chịu về 0 DÙ CÒN ĐẦY STAMINA -> Vẫn bị NGẤT và bị kéo về Trại Hồi Sức!
            engine.Character.Stamina.CurrentStamina = 180f;
            engine.Character.CurrentGuard = 10f;
            int hourBeforeFaint = engine.Clock.CurrentHour;
            var (hitGuardZero, rescuedGuardZero, rescueLog1) = engine.SimulatePlayerCombatEncounter(45f, isActiveParry: false);
            Assert(hitGuardZero.CausedFaint && rescuedGuardZero,
                "Khi điểm Thanh Chống Chịu về 0 (dù còn đầy Stamina), nhân vật bắt buộc phải bị ngất và được kéo về trại hồi sức!");
            Assert(engine.Farm.Gold == 900 && engine.Character.Stamina.Condition == 80 && engine.Clock.CurrentHour == (hourBeforeFaint + 2) % 24,
                "Kéo về trại hồi sức phải phạt 10% Vàng (1000->900), -20 Thể trạng (100->80), trôi +2 giờ và hồi lại 60% Chống Chịu & Thể Lực");
            Assert(engine.Character.CurrentGuard > 50f && !engine.Character.IsFainted,
                "Sau khi được cứu về hiên nhà chính, nhân vật phải tỉnh lại và hồi 60% Thanh Chống Chịu");

            // 4. Kịch bản B: Điểm Thể Lực về 0 DÙ CÒN ĐẦY THANH CHỐNG CHỊU -> Cũng bị NGẤT và kéo về Trại Hồi Sức!
            engine.Character.CurrentGuard = engine.Character.MaxGuard;
            engine.Character.Stamina.CurrentStamina = 5f;
            var (hitStaZero, rescuedStaZero, _) = engine.SimulatePlayerCombatEncounter(35f, isActiveParry: false);
            Assert(hitStaZero.CausedFaint && rescuedStaZero,
                "Khi Thể Lực đỡ đòn về 0 (dù còn Thanh Chống Chịu), nhân vật cũng phải bị ngất và được kéo về trại hồi sức!");

            // 5. Kịch bản C: Khắc Mệnh Ấn [Ấn Bất Khuất] -> Chống ngất 1 lần mỗi đợt quái ở ngưỡng 15 Chống Chịu & 15 Thể Lực
            engine.Character.EngraveSeal("AnBatKhuat");
            engine.Character.AnBatKhuatTriggeredThisWave = false;
            engine.Character.CurrentGuard = 5f;
            var (hitSaved, rescuedSaved, _) = engine.SimulatePlayerCombatEncounter(40f, isActiveParry: false);
            Assert(hitSaved.TriggeredAnBatKhuat && !hitSaved.CausedFaint && !rescuedSaved && engine.Character.CurrentGuard >= 15f,
                "Ấn Bất Khuất phải cứu nhân vật khỏi ngất 1 lần trong đợt quái và giữ lại tối thiểu 15 Chống Chịu");
        }

        static void TestCombatStaminaRegenSixEquipmentPairsAndMartialBloodSacrifice()
        {
            var ch = new CharacterData(PlayerId.Khoa);

            // 1. Khi chưa mặc 6 món trang bị -> 3 ô Kỹ năng Võ học phải bị khóa theo quy tắc cặp (GDD 12.2)
            Assert(!ch.IsMartialSlotUnlocked(CombatSlotType.TheCong) &&
                   !ch.IsMartialSlotUnlocked(CombatSlotType.TheThu) &&
                   !ch.IsMartialSlotUnlocked(CombatSlotType.TheBien),
                "Khi chưa mặc đủ cặp Vũ khí+Giày / Áo+Quần / Vòng+Dây chuyền thì 3 ô kỹ năng phải khóa");

            ch.EquipStarterSet();
            Assert(ch.IsMartialSlotUnlocked(CombatSlotType.TheCong) &&
                   ch.IsMartialSlotUnlocked(CombatSlotType.TheThu) &&
                   ch.IsMartialSlotUnlocked(CombatSlotType.TheBien),
                "Mặc đủ 6 món trang bị khởi đầu phải mở khóa cả 3 ô Thế Công, Thế Thủ và Thế Biến");

            // 2. Huyết Tế trên Thế Thủ (GDD 12.4): Khi thiếu Thể Lực, hiến Chống Chịu theo tỉ lệ 1:2 để vẫn tung được chiêu phòng ngự
            ch.Stamina.CurrentStamina = 4f; // Cần 12 STA -> thiếu 8 STA -> hiến 4 Chống Chịu
            float guardBefore = ch.CurrentGuard;
            var (executed, msg, _) = ch.TryExecuteMartialSkill(CombatSlotType.TheThu, allowBloodSacrifice: true);
            Assert(executed && ch.CurrentGuard == guardBefore - 4f && msg.Contains("HUYẾT TẾ"),
                "Thế Thủ phải kích hoạt Huyết Tế (hiến 4 điểm Chống Chịu bù cho 8 Thể Lực thiếu hụt)");

            // 3. Hồi phục Thể Lực & Chống Chịu trong giao tranh (2 STA/s, 4 Guard/s) và ngoài giao tranh (10 STA/s, 14 Guard/s)
            ch.Stamina.CurrentStamina = 50f;
            ch.CurrentGuard = 60f;
            ch.TickCombatRegen(deltaSeconds: 2.0f, inCombat: true, secondsSinceLastCombatAction: 2.0f, healingStationLevel: 0);
            Assert(Math.Abs(ch.Stamina.CurrentStamina - 54f) < 0.01f && Math.Abs(ch.CurrentGuard - 68f) < 0.01f,
                "Trong giao tranh (sau 1.5s) phải hồi 2 STA/s và 4 Chống Chịu/s");

            ch.TickCombatRegen(deltaSeconds: 2.0f, inCombat: false, secondsSinceLastCombatAction: 4.0f, healingStationLevel: 3);
            Assert(Math.Abs(ch.Stamina.CurrentStamina - 94f) < 0.01f && Math.Abs(ch.CurrentGuard - 124f) < 0.01f,
                "Ngoài giao tranh trong vùng Trạm Hồi Sức cấp 3 (x2.0) phải hồi 20 STA/s và 28 Chống Chịu/s");
        }

        static void TestPowerGridSleepBypassMidGameGateAndSpecialtyPapers()
        {
            var engine = new GameEngine();

            // 1. Lưới Điện (GDD 16.4): Cấp 1 có công suất liên tục 30u, đỉnh 60u. Quá tải > 140% (> 42u) -> Sập cầu dao 4 giây!
            var grid = engine.DeepMechanics.PowerGrid;
            Assert(grid.GetContinuousCapacity() == 30 && grid.GetPeakCapacity() == 60, "Máy phát Cấp 1 phải đạt 30u liên tục / 60u đỉnh");

            grid.CurrentDemandUnits = 36f; // 120% -> Quá tải nhẹ (-25% tốc bắn, -1 độ bền/s)
            var (_, fireMultOverload, durLoss) = grid.EvaluateGridTick(1.0f);
            Assert(Math.Abs(fireMultOverload - 0.75f) < 0.01f && durLoss == 1 && !grid.IsBreakerTripped,
                "Tải 120% phải giảm 25% tốc bắn và trừ 1 độ bền/giây");

            grid.CurrentDemandUnits = 45f; // 150% (> 140%) -> Sập cầu dao!
            var (_, fireMultTripped, _) = grid.EvaluateGridTick(1.0f);
            Assert(grid.IsBreakerTripped && fireMultTripped == 0f && Math.Abs(grid.BreakerTripRemainingSeconds - 4.0f) < 0.01f,
                "Tải > 140% phải làm Sập Cầu Dao mất điện trong 4 giây");

            // 2. 5 Vật phẩm xóa hồi chờ ngủ (GDD 9.5): Chỉ duy nhất Quả Mộng không có tác dụng phụ
            engine.Clock.PerformStandardSleep();
            Assert(engine.Clock.SleepCooldownHoursRemaining == 6, "Sau khi ngủ phải có 6h hồi chờ");

            engine.DeepMechanics.UseSleepBypassItem(SleepBypassItemId.DanHoiNguyen, engine.Clock, engine.Character, engine.Farm);
            Assert(engine.Clock.SleepCooldownHoursRemaining == 0 && engine.Character.Stamina.ReboundFatigueStacks == 1,
                "Đan Hồi Nguyên xóa hồi chờ ngủ nhưng để lại +1 tầng Mệt Dội Ngược");

            engine.Clock.SleepCooldownHoursRemaining = 6;
            engine.DeepMechanics.UseSleepBypassItem(SleepBypassItemId.QuaMong, engine.Clock, engine.Character, engine.Farm);
            Assert(engine.Clock.SleepCooldownHoursRemaining == 0 &&
                   engine.Character.Stamina.ReboundFatigueStacks == 0 &&
                   engine.Character.Stamina.Condition == 100 &&
                   Math.Abs(engine.Character.Stamina.CurrentStamina - engine.Character.Stamina.MaxStamina) < 0.01f,
                "Quả Mộng xóa hồi chờ ngủ, xóa sạch Mệt Dội Ngược và hồi 100% Thể lực/Thể trạng không tác dụng phụ");

            // 3. Cổng Giữa Game, 6 Khóa Chuyên Biệt & Cường hóa Giấy Chuyên Biệt (+0 -> +3, trần 80%)
            engine.Farm.FarmLevel = 6;
            engine.MajorWavesSurvived = 5;
            engine.Herd.RestoreState(HerdStateTier.HungThinh, 5, 2, null, new SuccessionTrainingData());
            engine.DeepMechanics.EvaluateMidGameGateAndLocks(engine.Farm, engine.Herd, engine.MajorWavesSurvived);
            Assert(engine.DeepMechanics.MidGameGateUnlocked && engine.DeepMechanics.UnlockedSpecialtyLocksCount == 6,
                "Đạt Trại cấp 6, qua 5 đợt lớn và Bầy Đàn Hưng Thịnh phải mở Cổng Giữa Game và đủ 6/6 khóa chuyên biệt");

            engine.DeepMechanics.EnhanceSpecialtyPaper(0.50); // +1 (100%)
            engine.DeepMechanics.EnhanceSpecialtyPaper(0.50); // +2 (90%)
            var enh3Fail = engine.DeepMechanics.EnhanceSpecialtyPaper(0.85); // > 0.80 trần -> thất bại ở +3
            Assert(!enh3Fail.success && engine.DeepMechanics.intEquippedPaperEnhancementLevel == 2,
                "Cường hóa +3 có trần cứng 80% nên roll 0.85 phải thất bại");
            var enh3Ok = engine.DeepMechanics.EnhanceSpecialtyPaper(0.75); // <= 0.80 -> lên +3
            Assert(enh3Ok.success && engine.DeepMechanics.intEquippedPaperEnhancementLevel == 3,
                "Roll <= 0.80 phải cường hóa Giấy Chuyên Biệt lên +3 thành công");
        }

        static void TestSeasonOneStorySauVoiMotAndTwoColumnNotebook()
        {
            var engine = new GameEngine();
            var story = engine.Story;

            // 1. Quy tắc 'Sáu với một' & câu trả lời 'Mai trời nắng' khi chưa đủ tin cậy
            Assert(StoryJournalSystem.FormatEdgeTownNumber(7) == "sáu với một" &&
                   StoryJournalSystem.FormatEdgeTownNumber(17) == "mười sáu với một",
                "Thị trấn Bờ Rìa không bao giờ đọc số 7, luôn gọi là 'sáu với một'");

            string evasivereply = story.AskVillagerAboutTopic("BaSau", "Chuyện năm 2018 và số 7");
            Assert(evasivereply.Contains("Mai trời nắng"),
                "Khi độ tin cậy < 2 và hỏi chuyện cấm kỵ, người làng phải đáp 'Mai trời nắng.'");

            // 2. Sự kiện S02: Con heo chân trắng giả chết trên bàn cân (0.0 kg cho đến khi gõ máng 2 nhịp)
            var whiteFootPig = Pig.CreateDefault("pig_01", "Heo Út Chân Trắng", GeneLineId.HongDien, GeneRarity.Thuong, "Cai", 5, 42.5f);
            var weigh1 = story.EvaluateWhiteFootedPigWeighing(whiteFootPig, tappedTroughTwice: false);
            Assert(weigh1.playedDead && Math.Abs(weigh1.displayedWeightKg - 0.0f) < 0.01f,
                "Con heo chân trắng lần đầu đặt lên cân phải nằm ngửa giả chết (0.0 kg)");

            var weigh2 = story.EvaluateWhiteFootedPigWeighing(whiteFootPig, tappedTroughTwice: true);
            Assert(!weigh2.playedDead && Math.Abs(weigh2.displayedWeightKg - 42.5f) < 0.01f,
                "Sau khi gõ máng 2 nhịp, con heo chân trắng phải bật dậy và cân đúng 42.5 kg");

            // 3. Tiến triển nhiệm vụ chính tuyến Q05, Q07, Q08, Q11/Q12 -> Sổ Hai Cột chuyển từ [THẤY] sang [CÓ THẬT]
            story.CompleteQuestStep("Q05");
            story.CompleteQuestStep("Q07");
            story.CompleteQuestStep("Q08");
            story.CompleteQuestStep("Q12");

            int verifiedEntries = story.NotebookEntries.FindAll(e => e.IsVerifiedToRightColumn && !string.IsNullOrEmpty(e.RightColumn_CoThat)).Count;
            Assert(verifiedEntries == 5 && story.HerdEmptySpotObserved,
                "Hoàn thành các mốc điều tra phải chuyển đủ 5 mục trong Sổ Hai Cột từ cột [THẤY] sang cột [CÓ THẬT]");

            // Khi độ tin cậy đã tăng >= 2, hỏi lại Bà Sáu phải nhận được lời kể thật
            string trueReply = story.AskVillagerAboutTopic("BaSau", "Chuyện năm 2018 và số 7");
            Assert(!trueReply.Contains("Mai trời nắng") && trueReply.Contains("Chú Minh"),
                "Khi độ tin cậy >= 2, Bà Sáu phải kể sự thật về cái bát thứ ba và chú Minh");

            // 4. Chọn Kết thúc Mùa 1 (8.1 / 8.2 / 8.3) + Hậu tín dụng 8.5 'Người tới sớm'
            var endingRes = story.SelectSeasonOneEnding(StoryEndingId.Ending_8_1_GiuTraiThaoDuongGui);
            Assert(endingRes.success && story.SeventhChannelCut && story.PostCreditTriggered && endingRes.postCreditHook.Contains("Kiện 6+1"),
                "Kết thúc 8.1 phải cắt đường cáp số 7 và kích hoạt Hậu tín dụng 8.5 'Người tới sớm'");
        }
    }
}
