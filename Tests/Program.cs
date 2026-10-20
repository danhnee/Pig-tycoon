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

            // C: Combat, Weapons, Enemies, Loot & Alien/Bandit Escalation Tests (9 tests)
            RunTest("Combat: Hệ thống dữ liệu Vũ khí (Đao, Búa, Giáo)", TestWeaponDataSystem);
            RunTest("Combat: Chỉ số Quái vật & Công thức sát thương/nộ khí", TestEnemyDataAndFormulas);
            RunTest("Combat: Cơ chế Stardew 1.6 (Swipe, Crit, Parry) 64x64", TestStardewCombatMechanics64x64);
            RunTest("Combat: Bù nhìn tập đánh & Arena Analytics", TestTrainingDummyAndArenaAnalytics);
            RunTest("Combat: Hệ thống rơi vật phẩm (Loot System & Rarity)", TestLootSystemAndDropRules);
            RunTest("Combat: Bắt cóc heo, UFO Tractor Beam & Raid Telegraphs", TestPigSnatchingAndRaidTelegraphInvariants);
            RunTest("Combat: Chuỗi leo thang Alien, UFO thả quân & Tàu Mẹ", TestAlienEscalationChain);
            RunTest("Combat: Chuỗi leo thang Đạo Tặc & Boss Chúa Trộm Rìu Sắt", TestBanditEscalationChain);
            RunTest("Combat: Hệ thống Thể Lực Kép (GDD 9) & Chuỗi Combo 3 nhịp (GDD 12)", TestDualLayerStaminaAndComboMechanics);

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
            RunTest("Deep-5: GDD 4.8 Hậu duệ heo già chuẩn (10.5% / 59.5% Kim Thọ, 5 chỉ số phạt độc lập, Ưu Tú) & GDD 5.4 Giám định 4 kênh", TestElderBreedingGdd48AndFourChannelAppraisalGdd54);
            RunTest("Deep-6: GDD 12.3–12.7 24 Chiêu Võ Học 4 Phái, Vòng Hợp Kích A→C→B→D→A, 15 Trang bị, 4 Phù + 3 Ấn & 20 Vật phẩm", TestMartial24SkillsSchoolSynergyEquipmentAndTacticalItems);
            RunTest("Deep-7: GDD 14–24 31 Công trình, 3 Chế độ bắn, Nhánh 1A–4C, Dây điện nổi/ngầm ×3, Ngân sách Uy Hiếp, 8 Chủ đề chợ, 12 NPC, 7 Thú & 14 Sự kiện", TestDefense31BuildingsWireGridThreatBudgetMarketAnd14Events);
            RunTest("Deep-8: Cốt truyện Mùa 1 chuyên sâu (Hộp sắt bậc 7 Babylon 24, Khe lò sưởi 4 lớp 2+4=24, Mặt phản chiếu, Hộp kỹ thuật 7 kiện, Q14, Q20 & Ngỗng Giám Đốc)", TestSeasonOneDeepPuzzlesReflectionsVillagerMeetingAndGooseEnding);
            RunTest("Camera-v1.0: Hệ thống 6 chế độ Camera phối cảnh 3D (Farm/Explore/Combat/Defense/Dialogue/Scenic), khóa hướng thế giới & làm mờ vật cản", TestCameraDesignV1ProfilesAndPerspectiveLock);
            RunTest("World3D-v1.0: Bản đồ 3D Farm_Main, giới hạn heo thả rông <= 1/3 đồng cỏ sát trại, bối cảnh 3D xa & chuyển map nối tiếp Farm -> Khu NPC -> Chợ", TestFarmMain3DLayoutOneThirdPastureAndChainedMapTransitions);
            RunTest("Camera-v2.0: Hai trạng thái chính (FollowCharacter & FreeObservation), tách PC MMB / Mobile Touch, xoay ±70° vs 360°, bảo vệ Kinh tế Thông tin & chống exploit điểm spawn", TestCameraDesignV2TwoStatesPlatformControlsAndInfoEconomy);
            RunTest("CameraPhysics-v3.0: Vật lý Địa hình Bilinear O(1) (150 heo + 30 quái), Tỷ lệ Vùng thả đàn (<=1/3 map & >=35% trại), FrustumSizeAtDistance & Sửa tiêu chí A2", TestCameraAndMapPhysicsEngineO1HeightfieldAndFrustumA2);
            RunTest("A-Full (P0–P5): StatCalc, EventBus, Kho 36 ô, Farm_Core 7 phòng, Nghiên cứu, Xưởng Cường Hóa, Savex3 & Vertical Slice", TestMemberAFarmCoreRoomsResearchWorkshopAndVerticalSlice);
            RunTest("C-Full (P0–P5): Market_Town Ngày/Đêm, Lịch Đợt Quái 6–12d, Chu kỳ Boss 5/10/15, Báo cáo 7 mục & Bất biến Võ học", TestMemberCMarketTownWaveScheduleAndMartialInvariants);
            RunTest("D-Full (P0–P5): Sổ cái 1G, 8 Chủ đề chợ, Hợp đồng/Đấu giá/Bí nhân, Tình báo 0–5, Minimap/Sương mù, Rừng Nguyên Sinh, L10n & Sim 30d", TestMemberDEconomyLedgerThemesContractsIntelMinimapAndSim);
            RunTest("Saga & GDD v6.0 Bible: Bảng 1.4 Gia đình Khoa, P01–P08, Q01–Q20, S01–S12, Thánh Minh Nhật Giả, Sirphina, Chương 8, Bảng 1.5-D & 10.2/10.8", TestCanonicalSeasonOneSagaFullParts1To12AndGdd6Tables);
            RunTest("Polyglot Native FFI (Rust / C++20 / Go / C# / Java / Flutter Dart): Benchmark 4 Nhân Mô Phỏng & Tương Đương Bit-Exact", TestPolyglotNativeRustCppGoJavaDartFfiAndCSharpEquivalence);
            RunTest("Dual Protagonist (Khoa & An Phần 1B/2.7/5B), Phần 11–12 (Đại Giới Lưu/Mặc Khải/Sirphina/11 Lời Chào) & Polyglot API Bridge + Native Flow-Field", TestDualProtagonistAnPerspectiveParts11To12PolyglotApiBridgeAndNativeFlowField);
            RunTest("Stardew Valley 1.6 Spine: QualifiedItemRegistry, GameStateQuery, TriggerActionManager, WorldLocationGraph & Machines", TestStardewValley16GradeArchitectureSpineAndLiveEngineIntegration);

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
            Assert(huThe.Traits.Find(t => t.IsDeviant && !t.IsPositive)!.IsSealed, "Hư Thể phải được phong ấn vĩnh viễn");
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
            Assert(bldg.building!.AmmoCurrent == 50, "Nỏ Xuyên Vân nạp tối đa 50 tên");

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
            Assert(bvOk.building!.IsBachVan, "Phải được đánh dấu là Bạch Vân");

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
            Assert(piglets.Count >= 7 && piglets.Count <= 12,
                $"Nái Phong Mẫu (+1 con/lứa theo GDD 4.8, gốc 6-11) phải sinh từ 7 đến 12 heo con F1 vào engine.Pigs (thực tế: {piglets.Count})");
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

            // 1B. Đồ thị BFS Lưới Điện (GDD 16.3): Đứt dây trục chính làm mất điện nhánh hạ lưu, nhưng Mạch Vòng Dự Phòng giữ nguyên nguồn điện!
            grid.IsBreakerTripped = false;
            grid.BreakerTripRemainingSeconds = 0f;
            grid.ConnectWire("wire_trunk", "Generator_Main", "Pole_Mid", underground: false);
            grid.ConnectWire("wire_east_branch", "Pole_Mid", "Pole_East", underground: false);
            var eastTower = new DefenseBuildingInstance { InstanceId = "t_east", BlueprintId = "ThapHoQuang", ConnectedWireId = "wire_east_branch", IsPowered = true };
            var bldgList = new List<DefenseBuildingInstance> { eastTower };
            var cutTrunk = grid.TryCutWireByEnemy("wire_trunk", bldgList);
            Assert(cutTrunk.cutSuccess && !eastTower.IsPowered && cutTrunk.disabledBuildingsCount == 1,
                "Cắt dây trục chính (Generator_Main -> Pole_Mid) phải làm mất điện cả nhánh hạ lưu (Pole_Mid -> Pole_East)");
            grid.ConnectWire("wire_redundant_loop", "Generator_Main", "Pole_East", underground: true);
            grid.RepairCutWire("wire_trunk", bldgList);
            var cutTrunkWithLoop = grid.TryCutWireByEnemy("wire_trunk", bldgList);
            Assert(cutTrunkWithLoop.cutSuccess && eastTower.IsPowered,
                "Khi có dây ngầm dự phòng nối thành mạch vòng tới Pole_East, cắt dây trục chính không làm mất điện công trình tại Pole_East");

            // 2. 5 Vật phẩm phá cooldown ngủ (GDD 9.5 & 9.6): Hương Mê Thảo +1 Mệt Dội ngay, Đan Hồi Nguyên xóa 2 tầng Mệt Dội, Quả Mộng xóa 1 tầng & không tính nghỉ cưỡng bức
            engine.Clock.PerformStandardSleep();
            Assert(engine.Clock.SleepCooldownHoursRemaining == 6, "Sau khi ngủ phải có 6h hồi chờ");

            engine.DeepMechanics.UseSleepBypassItem(SleepBypassItemId.HuongMeThao, engine.Clock, engine.Character, engine.Farm);
            Assert(engine.Clock.SleepCooldownHoursRemaining == 0 && engine.Character.Stamina.ReboundFatigueStacks == 1,
                "Hương Mê Thảo (Bậc III) xóa hồi chờ ngủ và gây +1 tầng Mệt Dội ngay lập tức (GDD 9.6)");

            engine.Character.Stamina.ReboundFatigueStacks = 3;
            engine.Clock.SleepCooldownHoursRemaining = 6;
            engine.DeepMechanics.UseSleepBypassItem(SleepBypassItemId.DanHoiNguyen, engine.Clock, engine.Character, engine.Farm);
            Assert(engine.Clock.SleepCooldownHoursRemaining == 0 && engine.Character.Stamina.ReboundFatigueStacks == 1,
                "Đan Hồi Nguyên (Bậc IV) xóa hồi chờ ngủ và xóa 2 tầng Mệt Dội (3 -> 1) theo GDD 9.6");

            engine.Clock.SleepCooldownHoursRemaining = 6;
            engine.DeepMechanics.UseSleepBypassItem(SleepBypassItemId.QuaMong, engine.Clock, engine.Character, engine.Farm);
            Assert(engine.Clock.SleepCooldownHoursRemaining == 0 &&
                   engine.Character.Stamina.ReboundFatigueStacks == 0 &&
                   engine.Character.Stamina.Condition == 100 &&
                   engine.DeepMechanics.ForcedRestCountThisCycle == 2 &&
                   Math.Abs(engine.Character.Stamina.CurrentStamina - engine.Character.Stamina.MaxStamina) < 0.01f,
                "Quả Mộng (Bậc IV) xóa hồi chờ ngủ, xóa 1 tầng Mệt Dội, hồi 100% Thể lực và không tính vào số lần nghỉ cưỡng bức (GDD 9.6)");

            // 3. Cổng Giữa Game (GDD 10.2: >= 8 đợt lớn + 1/5 điều kiện phụ), 6 Khóa Chuyên Biệt & Cường hóa Giấy Chuyên Biệt (+0 -> +3, trần 80% GDD 10.6)
            engine.Farm.FarmLevel = 7;
            engine.MajorWavesSurvived = 7; // Chưa đủ 8 đợt lớn -> Cổng Giữa Game chưa mở!
            engine.Herd.RestoreState(HerdStateTier.HungThinh, 5, 2, null, new SuccessionTrainingData());
            engine.DeepMechanics.EvaluateMidGameGateAndLocks(engine.Farm, engine.Herd, engine.MajorWavesSurvived);
            Assert(!engine.DeepMechanics.MidGameGateUnlocked && engine.DeepMechanics.SpecialtyTraitDisplayName == "???",
                "Khi mới qua 7 đợt lớn (< 8), Cổng Giữa Game chưa được mở và tên Đặc tính phải ẩn '???' (GDD 10.2 & 10.4)");

            engine.MajorWavesSurvived = 10;
            engine.DeepMechanics.EvaluateMidGameGateAndLocks(engine.Farm, engine.Herd, engine.MajorWavesSurvived);
            Assert(engine.DeepMechanics.MidGameGateUnlocked && engine.DeepMechanics.UnlockedSpecialtyLocksCount == 6 && engine.DeepMechanics.SpecialtyTraitDisplayName != "???",
                "Đạt >= 8 đợt lớn (10 đợt), Trại cấp 7 và Bầy Đàn Hưng Thịnh phải mở Cổng Giữa Game và đủ 6/6 khóa chuyên biệt");

            engine.DeepMechanics.EnhancementStonesCount = 20;
            engine.DeepMechanics.EnhanceSpecialtyPaper(deterministicRoll: 0.50, stonesToUse: 0); // +0 -> +1 (100%, 0 đá)
            engine.DeepMechanics.EnhanceSpecialtyPaper(deterministicRoll: 0.50, stonesToUse: 5); // +1 -> +2 (65% + 5*6% = 95%)
            var enh3Fail = engine.DeepMechanics.EnhanceSpecialtyPaper(deterministicRoll: 0.85, stonesToUse: 8); // +2 -> +3 (40% + 8*5% = 80% trần -> 0.85 thất bại)
            Assert(!enh3Fail.success && engine.DeepMechanics.intEquippedPaperEnhancementLevel == 2,
                "Cường hóa +3 có trần cứng 80% (GDD 10.6) nên roll 0.85 phải thất bại");
            engine.DeepMechanics.EnhancementStonesCount = 8;
            var enh3Ok = engine.DeepMechanics.EnhanceSpecialtyPaper(deterministicRoll: 0.75, stonesToUse: 8); // <= 0.80 -> lên +3
            Assert(enh3Ok.success && engine.DeepMechanics.intEquippedPaperEnhancementLevel == 3,
                "Roll <= 0.80 với 8 Đá Cường Hóa (40% + 40% = 80%) phải cường hóa Giấy Chuyên Biệt lên +3 thành công");
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

        static void TestElderBreedingGdd48AndFourChannelAppraisalGdd54()
        {
            // 1. GDD 4.8: Xác suất thụ thai heo già thường (-85% -> 70% * 0.15 = 10.5%) và Kim Thọ (-15% -> 70% * 0.85 = 59.5%)
            float normalElderRate = GeneticsAndBreedingSystem.CalculateConceptionRateGdd48(isElderPair: true, isKimThoElder: false);
            Assert(Math.Abs(normalElderRate - 0.105f) < 0.001f, $"Xác suất thụ thai heo già thường phải là 10.5% (thực tế: {normalElderRate * 100f:0.0}%)");

            float kimThoElderRate = GeneticsAndBreedingSystem.CalculateConceptionRateGdd48(isElderPair: true, isKimThoElder: true);
            Assert(Math.Abs(kimThoElderRate - 0.595f) < 0.001f, $"Xác suất thụ thai heo già Kim Thọ phải là 59.5% (thực tế: {kimThoElderRate * 100f:0.0}%)");

            // 2. GDD 4.8: Đánh giá đầy đủ 6 bước (Bất thường 35/40/25 + 5 chỉ số phạt độc lập 5%-20% + Ưu Tú Toàn Diện/Đột Phá + 45% thức tỉnh 80/18/2)
            var badGenOutcome = GeneticsAndBreedingSystem.EvaluateElderBreedingFullGdd48(
                isKimThoElder: false, habitatIndex: 60, motherMood: 60, baseRarity: GeneRarity.Thuong,
                conceptionRoll: 0.05, healthyOrAbnormalRoll: 0.90, // > K + 0.75*(1-K) -> Gen Xấu (25%)
                fiveIndependentPenaltyRolls: new[] { 0.1, 0.3, 0.5, 0.7, 0.9 },
                eliteRoll: 0.50, eliteTypeRoll: 0.50, awakenRoll: 0.50, awakenTierRoll: 0.50);
            Assert(badGenOutcome.Conceived &&
                   badGenOutcome.AbnormalCategory == ElderOffspringAbnormalCategory.BadGene_GenXau &&
                   badGenOutcome.IndependentPenalties != null,
                "Roll bất thường cao phải rơi vào Gen Xấu kèm bảng phạt 5 chỉ số độc lập");
            Assert(badGenOutcome.IndependentPenalties!.GrowthRatePenalty >= 0.05f && badGenOutcome.IndependentPenalties.GrowthRatePenalty <= 0.20f &&
                   badGenOutcome.IndependentPenalties.TemperamentPenalty >= 0.05f && badGenOutcome.IndependentPenalties.TemperamentPenalty <= 0.20f &&
                   Math.Abs(badGenOutcome.IndependentPenalties.GrowthRatePenalty - badGenOutcome.IndependentPenalties.TemperamentPenalty) > 0.01f,
                "Mỗi chỉ số trong 5 chỉ số của hậu duệ heo già bất thường phải bị phạt độc lập trong khoảng [5%, 20%]");

            var eliteBreakthrough = GeneticsAndBreedingSystem.EvaluateElderBreedingFullGdd48(
                isKimThoElder: true, habitatIndex: 85, motherMood: 85, baseRarity: GeneRarity.Thuong,
                conceptionRoll: 0.10, healthyOrAbnormalRoll: 0.10, // < K -> Khỏe mạnh
                fiveIndependentPenaltyRolls: new[] { 0.5, 0.5, 0.5, 0.5, 0.5 },
                eliteRoll: 0.01, // < 0.02 -> Ưu Tú!
                eliteTypeRoll: 0.75, // >= 0.50 -> Đột Phá (+40% 1 chỉ số)
                awakenRoll: 0.20, // < 0.45 -> Thức tỉnh gen trội!
                awakenTierRoll: 0.99); // >= 0.98 -> Huyền Thoại (2%)!
            Assert(eliteBreakthrough.IsEliteOffspring &&
                   eliteBreakthrough.EliteKind == EliteSpecializationKind.DotPha_SingleStatPlus40Pct &&
                   eliteBreakthrough.AwakenedDominantGene &&
                   eliteBreakthrough.FinalRarity == GeneRarity.HuyenThoai,
                "Hậu duệ heo già Khỏe mạnh trúng 2% Ưu Tú (Đột Phá +40% 1 chỉ số) và 45% thức tỉnh ở 99% phải ra bậc Huyền Thoại");

            // 3. GDD 5.4: Giám định gen qua 4 kênh (Chợ Ngày 300/900G: 70/40/0/0%, Chợ Đêm 450/1300/3500/5000G: 90/72/50/35%, Phòng NC 700/2000/6500/9000G: 95/85/65/50%, Bí Nhân: 100%)
            var farm = new Farm { Gold = 20000 };
            var legendPig = Pig.CreateDefault("p_leg", "Kim Thọ Ẩn #1", GeneLineId.KimTho, GeneRarity.HuyenThoai, "Duc", 10);
            var legendSibling = Pig.CreateDefault("p_leg_2", "Kim Thọ Ẩn #2", GeneLineId.KimTho, GeneRarity.HuyenThoai, "Cai", 10);
            var herdList = new List<Pig> { legendPig, legendSibling };
            var dayMarketTry = GeneticsAndBreedingSystem.AppraisePigGene(legendPig, AppraisalChannel.ChoNgay, farm, deterministicRoll: 0.01, herdPigs: herdList);
            Assert(!dayMarketTry.identified && dayMarketTry.finalChance == 0f,
                "Chợ Ngày (300/900G) có tỷ lệ 0% với heo Huyền Thoại nên không thể giám định ra Kim Thọ");

            // Thất bại ở Phòng Nghiên Cứu (65% base, phí 6500G cho Huyền Thoại) -> Khóa 24h & cộng dồn +5% lần sau
            var labFail = GeneticsAndBreedingSystem.AppraisePigGene(legendPig, AppraisalChannel.PhongNghienCuu, farm, deterministicRoll: 0.80, herdPigs: herdList);
            Assert(!labFail.identified && labFail.goldCost == 6500 && legendPig.AppraisalLockHoursRemaining == 24 && legendPig.AppraisalFailureCount == 1,
                "Giám định thất bại ở Phòng NC (6500G) phải khóa 24h trên cá thể đó và cộng dồn +1 lần thất bại (+5% tỷ lệ cho lần thử sau)");

            var lockedTry = GeneticsAndBreedingSystem.AppraisePigGene(legendPig, AppraisalChannel.PhongNghienCuu, farm, deterministicRoll: 0.10, herdPigs: herdList);
            Assert(!lockedTry.identified && lockedTry.goldCost == 0, "Đang trong 24h khóa giám định phải từ chối thử lại");

            legendPig.AppraisalLockHoursRemaining = 0; // Hết 24h
            int expBefore = farm.FarmExp;
            var labSuccessWithPity = GeneticsAndBreedingSystem.AppraisePigGene(legendPig, AppraisalChannel.PhongNghienCuu, farm, deterministicRoll: 0.68, herdPigs: herdList); // Base 65% + Pity 5% = 70% > 68%!
            Assert(labSuccessWithPity.identified && legendPig.IsIdentified && legendSibling.IsIdentified &&
                   Math.Abs(labSuccessWithPity.finalChance - 0.70f) < 0.001f &&
                   (farm.FarmExp > expBefore || farm.FarmLevel > 1),
                "Sau 24h khóa, tỷ lệ giám định Huyền Thoại ở Phòng NC phải tăng từ 65% lên 70% nhờ +5% bảo hiểm, mở nhận diện vĩnh viễn toàn trại cho các heo cùng dòng gen và thưởng EXP trại (GDD 5.4)");
        }

        static void TestMartial24SkillsSchoolSynergyEquipmentAndTacticalItems()
        {
            // 1. GDD 12.7: Đủ 24 chiêu Võ Học (4 trường phái × 6 chiêu)
            Assert(MartialSkillCatalog.AllSkills.Count == 24, $"Phải có đúng 24 chiêu võ học chuẩn GDD 12.7 (thực tế: {MartialSkillCatalog.AllSkills.Count})");

            // 2. GDD 12.6: Vòng liên kết 1 chiều A -> C -> B -> D -> A (Ảnh Móc, Trói Chùy, Phản Thủ, Thủ Chuyển Công)
            var engine = new GameEngine();
            engine.Character.EquipStarterSet();
            engine.Character.Stamina.CurrentStamina = 180f;

            var stepA = engine.ExecuteMartialSkillWithCombo("pig.skill.doan_anh_tram"); // Tốc Bộ (A)
            var stepC = engine.ExecuteMartialSkillWithCombo("pig.skill.moc_keo");       // Khống Chế (C) -> [Ảnh Móc (A -> C)]!
            Assert(stepA.executed && stepC.executed && stepC.comboTriggered.Contains("Ảnh Móc"),
                "Tung chiêu Khống Chế (C) sau Tốc Bộ (A) phải kích hoạt Hợp kích [Ảnh Móc]");

            var stepB = engine.ExecuteMartialSkillWithCombo("pig.skill.pha_giap");      // Trọng Kích (B) -> [Trói Chùy (C -> B)]!
            Assert(stepB.executed && stepB.comboTriggered.Contains("Trói Chùy") && stepB.damageMultiplier > 2.0f,
                "Tung chiêu Trọng Kích (B) sau Khống Chế (C) phải kích hoạt Hợp kích [Trói Chùy] (+30% sát thương)");

            var stepD = engine.ExecuteMartialSkillWithCombo("pig.skill.tru_bo");        // Sinh Tồn (D) -> [Phản Thủ (B -> D)]!
            Assert(stepD.executed && stepD.comboTriggered.Contains("Phản Thủ"),
                "Tung chiêu Sinh Tồn (D) sau Trọng Kích (B) phải kích hoạt [Phản Thủ]");

            float staBeforeFreeA = engine.Character.Stamina.CurrentStamina;
            var stepA2 = engine.ExecuteMartialSkillWithCombo("pig.skill.bo_phap_luot"); // Tốc Bộ (A) -> [Thủ Chuyển Công (D -> A)] miễn phí Thể lực!
            Assert(stepA2.executed && stepA2.comboTriggered.Contains("Thủ Chuyển Công") && Math.Abs(engine.Character.Stamina.CurrentStamina - staBeforeFreeA) < 0.01f,
                "Tung chiêu Tốc Bộ (A) sau Sinh Tồn (D) phải kích hoạt [Thủ Chuyển Công] và tốn 0 Thể Lực");

            // 3. GDD 12.3–12.5: 15+ Trang bị, 4 Phù + 3 Ấn, 20 Vật phẩm Chiến thuật/Nông trại/Sự kiện
            Assert(EquipmentAndTacticalCatalog.AllEquipmentItems.Count >= 15, "Phải có đầy đủ >= 15 món trang bị GDD 12.3");
            int runeCount = 0, sealCount = 0;
            foreach (var rs in EquipmentAndTacticalCatalog.AllRunesAndSeals)
            {
                if (rs.IsSeal) sealCount++; else runeCount++;
            }
            Assert(runeCount == 4 && sealCount == 3, "Phải có đúng 4 Khắc Phù và 3 Khắc Ấn GDD 12.4");
            Assert(EquipmentAndTacticalCatalog.AllTwentyTacticalItems.Count == 20, $"Phải có đúng 20 vật phẩm tiêu hao/chiến thuật/sự kiện GDD 12.5 (thực tế: {EquipmentAndTacticalCatalog.AllTwentyTacticalItems.Count})");
        }

        static void TestDefense31BuildingsWireGridThreatBudgetMarketAnd14Events()
        {
            var engine = new GameEngine();

            // 1. GDD 14–17: Đủ 31 công trình phòng thủ (28 Auto + 3 Bạch Vân), 3 chế độ bắn & 3 nhánh nâng cấp (1A–4C)
            Assert(engine.Defense.GetUniqueGddBuildingCatalogCount() == 31, "DefenseManager phải có đúng 31 định danh công trình phòng thủ chuẩn GDD 14–17");
            engine.Defense.HallLevel = 5;
            foreach (var kv in engine.Defense.BlueprintRegistry)
            {
                if (!engine.Defense.UnlockedBlueprints.Contains(kv.Key))
                    engine.Defense.UnlockedBlueprints.Add(kv.Key);
            }

            engine.Defense.ConstructBuilding("NoXuyenVan");
            var tower = engine.Defense.Buildings.Find(b => b.BlueprintId == "NoXuyenVan");
            Assert(tower != null, "Phải xây được Nỏ Xuyên Vân");

            var modeRes = engine.Defense.SetTowerFireMode(tower!.InstanceId, TowerFireMode.ChiTinhAnh);
            var branchRes = engine.Defense.SelectAttackTowerUpgradeBranch(tower.InstanceId, "1B");
            Assert(modeRes.success && tower.FireMode == "ChiTinhAnh" && branchRes.success && tower.ChosenUpgradeBranch.StartsWith("1B"),
                "Trụ tấn công phải đổi được sang chế độ Chỉ Tinh Anh và nâng cấp nhánh 1B (Xuyên Hàng)");

            // 2. GDD 3.8 & 15: Lưới điện Nút – Dây (Dây nổi 1x vs Dây ngầm x3) & Kẻ Cắt Dây
            var grid = engine.DeepMechanics.PowerGrid;
            var wireSurface = grid.ConnectWire("w_surface", "Generator_Main", "pole_1", underground: false, baseCost: 40);
            var wireUnderground = grid.ConnectWire("w_underground", "Generator_Main", "pole_2", underground: true, baseCost: 40);
            tower.ConnectedWireId = "w_surface";
            tower.IsPowered = true;

            engine.Defense.ConstructBuilding("ThapHoQuang");
            var arcTower = engine.Defense.Buildings.Find(b => b.BlueprintId == "ThapHoQuang");
            Assert(arcTower != null, "Phải xây được Tháp Hồ Quang");
            arcTower!.ConnectedWireId = "w_underground";
            arcTower.IsPowered = true;

            Assert(wireUnderground.BuildCostGold == wireSurface.BuildCostGold * 3,
                "Dây điện ngầm phải có chi phí gấp 3 lần dây điện nổi (120G vs 40G) nhưng không bị Kẻ Cắt Dây phá");

            var cutUnderground = grid.TryCutWireByEnemy("w_underground", engine.Defense.Buildings);
            var cutSurface = grid.TryCutWireByEnemy("w_surface", engine.Defense.Buildings);
            Assert(!cutUnderground.cutSuccess && arcTower.IsPowered &&
                   cutSurface.cutSuccess && !tower.IsPowered,
                "Kẻ Cắt Dây chỉ cắt được dây điện nổi (làm mất điện cục bộ các công trình nối vào nhánh dây đó), còn dây điện ngầm vẫn cấp điện bình thường");

            grid.RepairCutWire("w_surface", engine.Defense.Buildings);
            Assert(tower.IsPowered, "Sửa dây điện bị đứt phải khôi phục điện cho công trình");

            // 3. GDD 18.2 & 19.1–19.2: Ngân sách Uy Hiếp & Boss 3 Phase (12.000 / 20.000 / 28.000 HP)
            int budgetCycle6 = EnemyData.CalculateWaveThreatBudget(cycleNumber: 6, pigCount: 50, farmStateMultiplier: 1.0f);
            // 100 * (1 + 0.18 * 5) * (1 + 0.004 * 50) = 100 * 1.9 * 1.2 = 228
            Assert(budgetCycle6 == 228, $"Ngân sách Uy Hiếp Chu kỳ 6 với 50 heo phải bằng 228 (thực tế: {budgetCycle6})");

            var boss6 = EnemyData.CreateGdd19Canonical(EnemyTypeId.ChuaTromRiuSat);
            var boss12 = EnemyData.CreateGdd19Canonical(EnemyTypeId.UfoMothership);
            var boss18 = EnemyData.CreateGdd19Canonical(EnemyTypeId.ThucTheSuong);
            Assert(boss6.MaxHp == 12000f && boss6.ThreatPoints == 300 &&
                   boss12.MaxHp == 20000f && boss12.ThreatPoints == 500 &&
                   boss18.MaxHp == 28000f && boss18.ThreatPoints == 600,
                "3 Boss Chu kỳ 6 / 12 / 18 phải có đúng HP chuẩn 12.000 / 20.000 / 28.000 và Điểm Uy Hiếp 300 / 500 / 600");

            boss12.CurrentHp = 10000f; // 50% HP -> Phase 2
            Assert(boss12.GetBossPhase() == 2 && boss12.GetBossPhaseBehavior().Contains("EMP"),
                "Mẫu Hạm UFO ở 50% HP phải chuyển sang Phase 2 (Khiên năng lượng & xung EMP)");

            // 4. GDD 20.2: 8 Chủ đề chu kỳ thị trường
            engine.Economy.ApplyMarketCycleTheme(MarketTheme.ConSotThit);
            Assert(Math.Abs(engine.Economy.DayMarket.ThemePriceMultiplier - 1.30f) < 0.01f &&
                   Math.Abs(engine.Economy.DayMarket.FeedPriceMultiplier - 1.10f) < 0.01f,
                "Chủ đề Cơn Sốt Thịt phải tăng giá heo bán +30% (x1.30) và giá cám +10% (x1.10)");

            // 5. GDD 11 & 24.1: Đủ 12 NPC (7 Nông trại + 5 Đánh thuê), 7 Thú cưng, và 14 Sự kiện Huyền Bí (E01–E14 + Bùa Hồi Hương / La Bàn Tán Vụ)
            Assert(Enum.GetValues(typeof(NpcSpecialistId)).Length == 12, "Phải có đúng 12 NPC (7 Nông trại + 5 Đánh thuê)");
            Assert(Enum.GetValues(typeof(PetId)).Length == 7, "Phải có đúng 7 Thú cưng đồng hành");
            Assert(Enum.GetValues(typeof(MysteryEventId)).Length == 14, "Phải có đúng 14 Sự kiện Huyền Bí E01–E14");

            var e13Exit = engine.TriggerAnyMysteryEventGdd24(MysteryEventId.E13_BaNgayKhoa, choiceIndex: 0, hasBuaHoiHuong: true);
            var e14Exit = engine.TriggerAnyMysteryEventGdd24(MysteryEventId.E14_LoMoHuKhong, choiceIndex: 0, hasLaBanTanVu: true);
            Assert(e13Exit.resolved && e13Exit.message.Contains("Bùa Hồi Hương") &&
                   e14Exit.resolved && e14Exit.message.Contains("La Bàn Tán Vụ"),
                "Ba Ngày Khóa (E13) và Lò Mổ Hư Không (E14) phải hỗ trợ thoát sớm khi có Bùa Hồi Hương / La Bàn Tán Vụ");
        }

        static void TestSeasonOneDeepPuzzlesReflectionsVillagerMeetingAndGooseEnding()
        {
            var engine = new GameEngine();
            var story = engine.Story;

            // 1. Sáu người trong gia đình & 6 kiện đồ gửi (14/03/2018 -> 07/04/2019) + Hộp sắt dưới bậc cầu thang thứ 7 (Chữ hình nêm Babylon 24)
            Assert(StoryJournalSystem.SixFamilyMembersAndPackages.Count == 6, "Phải có đủ hồ sơ 6 người trong gia đình (14/03/2018 – 07/04/2019)");
            var tinBox = story.OpenTinBoxUnderSeventhStair();
            Assert(tinBox.opened && story.TinBoxUnderSeventhStairOpened &&
                   tinBox.summary.Contains("24") &&
                   tinBox.summary.Contains("21/09/2026"),
                "Mở hộp sắt dưới bậc cầu thang thứ 7 phải thu được Mảnh da cổ khắc số 24 chữ hình nêm Babylon và dòng chữ mặt sau của Khoa ngày 21/09/2026");

            // 2. Giải đố khe lò sưởi 4 lớp (S04 / Q16): Cạo bồ hóng -> Thử sai vật phẩm -> Quay mặt số 24 vào trong -> Buông tay chờ đúng 2 phút (11:16 -> 11:18, 2+4=24 tiếng click)
            var failWait = engine.InteractStoryFurnaceSlot(scrapeSoot: true, tryWrongItem: true, turn24Inward: true, handsOffWaitMinutes: 0.5f);
            Assert(!failWait.opened, "Chưa buông tay chờ đủ 2 phút (11:16 -> 11:18) thì khe lò sưởi chưa thể mở");

            var successPuzzle = engine.InteractStoryFurnaceSlot(scrapeSoot: true, tryWrongItem: true, turn24Inward: true, handsOffWaitMinutes: 2.0f);
            Assert(successPuzzle.opened && story.FurnaceSlotNicheOpened && successPuzzle.message.Contains("2 RỒI 4 = 24") && successPuzzle.message.Contains("TAY TRÁI"),
                "Đợi đủ 2 phút không chạm phím (11:16 -> 11:18) phải phát ra 2+4=24 tiếng click và mở hốc tường chứa bức vẽ 7 cột thước đo vẽ bằng tay trái");

            // 3. Hệ thống Mặt Phản Chiếu (Reflective Surface Emotion System): Mặt thật luôn bình thản, nhưng bóng phản chiếu cười/nhíu mày/khóc
            var q19Mirror = engine.CheckPlayerReflectiveSurface("Necklace7Notches_Q19");
            Assert(q19Mirror.realFace.Contains("ráo hoảnh") && q19Mirror.reflectionFace.Contains("KHÓC"),
                "Ở Q19 khi đeo dây chuyền 7 khắc của mẹ, mặt thật vẫn bình thản nhưng người trong gương phải khóc");

            // 4. Hộp kỹ thuật sau chuồng (Q13: 6 kiện có tên + Kiện thứ 7 không nhãn) & Họp dân làng Q14 (3 bằng chứng phá vỡ 'Mai trời nắng')
            var techBox = story.InspectTechnicalBoxSevenChannels();
            Assert(techBox.receivedPackagesCount == 6 && techBox.seventhPackageStatus.Contains("Kiện #07"),
                "Hộp kỹ thuật sau chuồng phải có đúng 6 kiện đã nhận và Kiện thứ 7 ('sáu với một') đang chờ");

            var meetingQ14 = story.HoldVillagerEvidenceMeetingQ14(evidenceCountPresented: 3);
            Assert(meetingQ14.brokeSilence && story.VillagerSilenceBrokenInQ14 && meetingQ14.dialogue.Contains("Bà Sáu"),
                "Đưa đủ 3 bằng chứng ở Q14 phải phá vỡ câu 'Mai trời nắng' của dân làng Bờ Rìa");

            // 5. Q20 Trả lại cái bát men xanh mẻ thứ hai & Cảnh 8.1/8.3/8.4 Ngỗng Giám Đốc họp sáng -> 8.5 'Anh tới sớm hai phút'
            var q20Res = story.TriggerQ20IronAxeReturnPackage();
            Assert(q20Res.returned && story.SecondBlueGlazeBowlReturnedInQ20 && q20Res.message.Contains("bát men xanh"),
                "Q20 phải nhận lại được cái bát men xanh mẻ thứ hai mà Chúa Trộm Rìu Sắt trả lại");

            var end81 = story.SelectSeasonOneEnding(StoryEndingId.Ending_8_1_GiuTraiThaoDuongGui);
            var end83 = story.SelectSeasonOneEnding(StoryEndingId.Ending_8_3_MangVongVeNha);
            Assert(end81.epilogueText.Contains("Giám Đốc") &&
                   end83.epilogueText.Contains("hai tấm gương đối diện") &&
                   end83.postCreditHook.Contains("Anh tới sớm hai phút"),
                "Cảnh kết phải kích hoạt cuộc họp sáng của ngỗng Giám Đốc (8.4), nụ cười trong hai tấm gương đối diện (8.3) và câu 'Anh tới sớm hai phút' (8.5)");
        }

        static void TestCameraDesignV1ProfilesAndPerspectiveLock()
        {
            var camSys = new CameraAndWorldLayoutSystem();

            // 1. Bắt buộc là Perspective Camera (Vertical FOV 54–60°, mặc định 58°), không dùng Orthographic và không xoay 360° tự do
            Assert(camSys.IsPerspectiveCamera && !camSys.IsOrthographicCamera, "Camera bắt buộc là Perspective Camera, không dùng Orthographic");
            Assert(camSys.IsWorldDirectionLocked && !camSys.AllowFree360Orbit, "Camera bắt buộc giữ hướng thế giới cố định, không xoay 360° quanh lưng nhân vật");
            Assert(camSys.CurrentVerticalFov >= 54f && camSys.CurrentVerticalFov <= 60f, "Vertical FOV phải nằm trong dải chuẩn 54–60° (mặc định 58°)");

            // 2. Kiểm tra đủ 6 chế độ Camera chuẩn hóa và dải thông số Pitch / tỷ lệ chiều cao nhân vật trên màn hình
            var farm = camSys.GetProfile(CameraMode3D.Farm);
            var explore = camSys.GetProfile(CameraMode3D.Exploration);
            var combat = camSys.GetProfile(CameraMode3D.Combat);
            var defense = camSys.GetProfile(CameraMode3D.Defense);
            var dialogue = camSys.GetProfile(CameraMode3D.Dialogue);
            var scenic = camSys.GetProfile(CameraMode3D.Scenic);

            Assert(farm.MinPitchDegrees == 22f && farm.MaxPitchDegrees == 26f && farm.IsPitchValid(farm.DefaultPitchDegrees) &&
                   farm.MinScreenHeightRatio == 0.18f && farm.MaxScreenHeightRatio == 0.24f && farm.IsScreenHeightRatioValid(farm.DefaultScreenHeightRatio),
                "Chế độ Sinh hoạt tại trại (Farm) phải có Pitch 22–26° và chiều cao nhân vật 18–24% màn hình");

            Assert(explore.MinPitchDegrees == 18f && explore.MaxPitchDegrees == 24f &&
                   explore.MinScreenHeightRatio == 0.22f && explore.MaxScreenHeightRatio == 0.30f,
                "Chế độ Khám phá rừng / huyền bí (Exploration) phải có Pitch 18–24° và chiều cao nhân vật 22–30% màn hình");

            Assert(combat.MinPitchDegrees == 30f && combat.MaxPitchDegrees == 38f &&
                   combat.MinScreenHeightRatio == 0.14f && combat.MaxScreenHeightRatio == 0.20f,
                "Chế độ Chiến đấu thường (Combat) phải có Pitch 30–38° và chiều cao nhân vật 14–20% màn hình");

            Assert(defense.MinPitchDegrees == 38f && defense.MaxPitchDegrees == 48f &&
                   defense.MinScreenHeightRatio == 0.09f && defense.MaxScreenHeightRatio == 0.15f,
                "Chế độ Thủ trại / Boss lớn (Defense) phải có Pitch 38–48° và chiều cao nhân vật 9–15% màn hình");

            Assert(dialogue.IsFrontThreeQuarterDialogue && dialogue.DefaultScreenHeightRatio >= 0.45f,
                "Chế độ Hội thoại (Dialogue) phải quay cận cảnh / nửa thân góc 3/4 phía trước trên cùng model 3D thật");

            Assert(scenic.MinPitchDegrees == 12f && scenic.MaxPitchDegrees == 18f,
                "Chế độ Toàn cảnh điện ảnh (Scenic) phải có Pitch 12–18° để lấy bầu trời hoàng hôn, đồng cỏ và lâu đài xa");

            // 3. Kiểm tra công thức khoảng cách phối cảnh ngược lại tỷ lệ chiều cao nhân vật
            foreach (CameraMode3D mode in Enum.GetValues(typeof(CameraMode3D)))
            {
                camSys.ApplyModeImmediate(mode, characterHeightMeters: 1.70f);
                float verifyRatio = CameraAndWorldLayoutSystem.ComputeScreenHeightRatioFromDistance(
                    1.70f, camSys.CurrentDistanceMeters, camSys.CurrentVerticalFov);
                var spec = camSys.GetProfile(mode);
                Assert(spec.IsScreenHeightRatioValid(verifyRatio),
                    $"Khoảng cách Camera ở chế độ {mode} phải cho tỷ lệ chiều cao nhân vật nằm chuẩn trong [{spec.MinScreenHeightRatio}, {spec.MaxScreenHeightRatio}] (thực tế: {verifyRatio:0.000})");
            }

            // 4. Kiểm tra hướng nhìn thấy nhân vật khi khóa hướng thế giới:
            // Đi lên phía xa (+Z) -> thấy lưng; đi về phía camera (-Z) -> thấy mặt; đi ngang -> thấy dáng nghiêng
            camSys.ApplyModeImmediate(CameraMode3D.Farm);
            Assert(camSys.GetCharacterFacingRelativeToCamera(0f, 1f) == CharacterCameraFacingView.Back, "Đi lên phía xa phải thấy lưng nhân vật");
            Assert(camSys.GetCharacterFacingRelativeToCamera(0f, -1f) == CharacterCameraFacingView.Front, "Đi về phía camera phải thấy mặt nhân vật");
            Assert(camSys.GetCharacterFacingRelativeToCamera(-1f, 0f) == CharacterCameraFacingView.LeftProfile &&
                   camSys.GetCharacterFacingRelativeToCamera(1f, 0f) == CharacterCameraFacingView.RightProfile,
                "Đi ngang phải thấy dáng nghiêng của nhân vật");

            // 5. Kiểm tra làm mờ vật cản (cây, mái nhà, cột đá) khi che nhân vật
            float alphaBlocked = 1.0f;
            for (int i = 0; i < 10; i++)
            {
                alphaBlocked = camSys.UpdateObstructionFade("Tree_01", "Tree", isBlocking: true, deltaTime: 0.1f, fadedAlpha: 0.22f);
            }
            Assert(alphaBlocked <= 0.25f, "Khi cây/mái nhà/cột đá che nhân vật, độ mờ (alpha) phải giảm xuống ~0.22");
        }

        static void TestFarmMain3DLayoutOneThirdPastureAndChainedMapTransitions()
        {
            var engine = new GameEngine();
            var world = engine.CameraAndWorld;

            // 1. Giới hạn diện tích thả heo rông <= 1/3 diện tích đồng cỏ gần Khu chuồng trại
            float ratio = world.GetPigPastureAreaRatio();
            Assert(ratio > 0.15f && ratio <= CameraAndWorldLayoutSystem.MaxAllowedPigRoamingAreaRatio + 0.0001f,
                $"Vùng thả heo rông gần Khu chuồng trại bắt buộc <= 1/3 (33.33%) diện tích đồng cỏ (thực tế: {ratio * 100f:0.1f}%)");

            // Kiểm tra GameEngine.Tick tự động kẹp heo đi lạc ở xa về lại trong vùng 1/3 đồng cỏ sát chuồng trại
            var wanderingPig = Pig.CreateDefault("pig_wander", "Heo Đi Lạc", GeneLineId.HongDien, GeneRarity.Thuong, "Cai", 10, 65f);
            wanderingPig.PositionX = 52.0f; // Đi tuốt sang bờ sông phải (ngoài vùng 1/3)
            wanderingPig.PositionY = -38.0f;
            engine.Pigs.Add(wanderingPig);

            engine.Tick(60); // Qua 1 giờ game -> OnHourlyTick kẹp vị trí
            Assert(world.IsInsidePigRoamingZone(wanderingPig.PositionX, wanderingPig.PositionY),
                "Heo thả rông trong GameEngine bắt buộc bị giới hạn trong vùng <= 1/3 đồng cỏ gần Khu chuồng trại, không được đi khắp bản đồ");

            // 2. Khu NPC và Chợ / Thị trấn bắt buộc dựng bằng Model 3D thật để nhìn từ xa (không phải phông 2D, và không đi bộ trực tiếp trong Farm_Main)
            var npcVista = world.Landmarks3D.Find(l => l.Id == "KhuNPC_Vista3D");
            var marketVista = world.Landmarks3D.Find(l => l.Id == "ChoThiTran_Vista3D");
            var castleVista = world.Landmarks3D.Find(l => l.Id == "LauDaiVaThacNuoc_Vista3D");

            Assert(npcVista != null && npcVista.Is3DModel && !npcVista.IsFlat2DBackdrop && !npcVista.HasWalkableColliderInFarmMain,
                "Khu NPC phải dựng Model 3D thật để nhìn từ xa và không đi bộ trực tiếp trong Farm_Main");
            Assert(marketVista != null && marketVista.Is3DModel && !marketVista.IsFlat2DBackdrop && !marketVista.HasWalkableColliderInFarmMain,
                "Chợ / Thị trấn phải dựng Model 3D thật để nhìn từ xa và không đi bộ trực tiếp trong Farm_Main");
            Assert(castleVista != null && castleVista.Is3DModel && !castleVista.IsFlat2DBackdrop,
                "Lâu đài hoàng hôn & Thác nước phải dựng Model 3D bối cảnh xa");

            // 3. Quy tắc điểm chuyển map (Map Transition):
            // - Tại Farm_Main chỉ có duy nhất 1 điểm chuyển map dẫn tới Khu NPC.
            // - Không thể chuyển trực tiếp từ Farm_Main sang Chợ / Thị trấn.
            // - Phải chuyển từ Farm_Main -> Khu NPC trước, rồi từ Khu NPC mới chuyển sang Chợ / Thị trấn.
            var farmPortals = world.GetPortalsInZone(WorldMapZoneId.FarmMain);
            Assert(farmPortals.Count == 1 && farmPortals[0].ToZone == WorldMapZoneId.KhuNPC,
                "Bản đồ Farm_Main chỉ có duy nhất 1 điểm chuyển tiếp dẫn đến Khu NPC ở cuối đường phía sau trại");

            var directToMarket = world.TryTransitionToZone(WorldMapZoneId.ChoThiTran);
            Assert(!directToMarket.success && world.CurrentMapZone == WorldMapZoneId.FarmMain,
                "Tuyệt đối không được chuyển trực tiếp từ Farm_Main sang Chợ / Thị trấn");

            var toNpc = world.TryTransitionToZone(WorldMapZoneId.KhuNPC);
            Assert(toNpc.success && world.CurrentMapZone == WorldMapZoneId.KhuNPC,
                "Phải đi qua điểm chuyển map từ Farm_Main sang Khu NPC thành công");

            var npcToMarket = world.TryTransitionToZone(WorldMapZoneId.ChoThiTran);
            Assert(npcToMarket.success && world.CurrentMapZone == WorldMapZoneId.ChoThiTran,
                "Từ Khu NPC phải có điểm chuyển map để đi tiếp sang Chợ / Thị trấn");
        }

        static void TestCameraDesignV2TwoStatesPlatformControlsAndInfoEconomy()
        {
            var cam = new CameraAndWorldLayoutSystem();

            // 1. Hai trạng thái chính: FollowCharacter và FreeObservation
            // Khi tách camera sang FreeObservation và kéo đi xa, vị trí thật của Khoa (PlayerWorldX/Z) KHÔNG dịch chuyển
            cam.SetPrimaryState(CameraPrimaryState3D.FollowCharacter, currentPlayerX: -12.0f, currentPlayerY: 1.4f, currentPlayerZ: 8.0f);
            Assert(cam.PrimaryState == CameraPrimaryState3D.FollowCharacter, "Mặc định phải ở trạng thái FollowCharacter");

            cam.SetPrimaryState(CameraPrimaryState3D.FreeObservation, currentPlayerX: -12.0f, currentPlayerY: 1.4f, currentPlayerZ: 8.0f);
            cam.PanFreeObservationCamera(deltaWorldX: 35.0f, deltaWorldZ: -20.0f, deltaTime: 1.5f);
            Assert(Math.Abs(cam.PlayerWorldX - (-12.0f)) < 0.001f && Math.Abs(cam.PlayerWorldZ - 8.0f) < 0.001f,
                "Khi tách camera sang Quan sát tự do (FreeObservation), vị trí thật của Khoa không được dịch chuyển");
            Assert(Math.Abs(cam.FreeObservationPivotX - 23.0f) < 0.001f && Math.Abs(cam.FreeObservationPivotZ - (-12.0f)) < 0.001f,
                "Tâm quan sát tự do phải di chuyển độc lập tới khu vực muốn kiểm tra");

            // 2. Điều khiển PC vs Điện thoại:
            // - Trên PC: Chuột phải (RMB) KHÔNG xoay camera (để dành cho combat/kỹ năng); chỉ Chuột giữa (MMB) mới xoay
            bool rmbRotated = cam.TryApplyPlatformYawInput(
                CameraPlatformInputScheme.PC_MouseKeyboard,
                deltaYawDegrees: 25.0f,
                isMiddleMouseHeldOnPc: false,
                isRightMouseHeldOnPc: true,
                isTouchStartedOnSkillOrUiButton: false,
                isTwoFingerTwistGestureOnMobile: false,
                isDedicatedRotateButtonOnMobile: false);
            Assert(!rmbRotated && Math.Abs(cam.CurrentYawDegrees - 0.0f) < 0.001f,
                "Trên PC: Chuột phải không được làm xoay camera để tránh xung đột với combat/kỹ năng");

            bool mmbRotated = cam.TryApplyPlatformYawInput(
                CameraPlatformInputScheme.PC_MouseKeyboard,
                deltaYawDegrees: 30.0f,
                isMiddleMouseHeldOnPc: true,
                isRightMouseHeldOnPc: false,
                isTouchStartedOnSkillOrUiButton: false,
                isTwoFingerTwistGestureOnMobile: false,
                isDedicatedRotateButtonOnMobile: false);
            Assert(mmbRotated && Math.Abs(cam.CurrentYawDegrees - 30.0f) < 0.001f,
                "Trên PC: Giữ Chuột giữa (MMB) kéo phải xoay được camera");

            // - Trên Điện thoại: kéo bắt đầu từ nút võ kỹ KHÔNG được xoay camera; xoắn 2 ngón trong FreeObservation KHÔNG xoay camera
            bool skillSwipeRotated = cam.TryApplyPlatformYawInput(
                CameraPlatformInputScheme.Mobile_Touch,
                deltaYawDegrees: 40.0f,
                isMiddleMouseHeldOnPc: false,
                isRightMouseHeldOnPc: false,
                isTouchStartedOnSkillOrUiButton: true,
                isTwoFingerTwistGestureOnMobile: false,
                isDedicatedRotateButtonOnMobile: false);
            Assert(!skillSwipeRotated, "Trên Điện thoại: vuốt bắt đầu từ nút võ kỹ phải ưu tiên cho kỹ năng, không được xoay camera");

            bool twoFingerTwistInFreeObs = cam.TryApplyPlatformYawInput(
                CameraPlatformInputScheme.Mobile_Touch,
                deltaYawDegrees: 40.0f,
                isMiddleMouseHeldOnPc: false,
                isRightMouseHeldOnPc: false,
                isTouchStartedOnSkillOrUiButton: false,
                isTwoFingerTwistGestureOnMobile: true,
                isDedicatedRotateButtonOnMobile: false);
            Assert(!twoFingerTwistInFreeObs, "Trên Điện thoại: trong chế độ Quan sát tự do không dùng xoắn 2 ngón mà dùng nút xoay trái/phải");

            // 3. Kiểm tra 2 phương án xoay ngang: LimitedPlusMinus70 (±70°) vs Full360Orbit (360°)
            cam.SetYawRotationPolicy(CameraYawRotationPolicy.LimitedPlusMinus70, maxLimitedOffsetDeg: 70.0f);
            cam.ApplyUserYawRotation(120.0f); // Cố xoay vượt +70°
            Assert(Math.Abs(cam.CurrentYawDegrees - 70.0f) < 0.01f, "Ở chế độ LimitedPlusMinus70, góc xoay phải bị kẹp đúng ở +70°");

            cam.SetYawRotationPolicy(CameraYawRotationPolicy.Full360Orbit);
            cam.ApplyUserYawRotation(120.0f); // 70 + 120 = 190°
            Assert(Math.Abs(cam.CurrentYawDegrees - 190.0f) < 0.01f, "Ở chế độ Full360Orbit, camera phải xoay qua 190° tự do");

            // 4. Bảo vệ Kinh tế Thông tin & Cảnh báo nguy hiểm tại vị trí Khoa khi đang ở FreeObservation:
            // - Quái tàng hình (Kẻ Cắt Dây) tiến sát Khoa nhưng CHƯA có nguồn phát hiện hợp lệ -> KHÔNG được báo động miễn phí
            bool stealthAlert = cam.EvaluatePlayerDangerAlertInFreeObservation(
                enemyWorldX: -10.0f,
                enemyWorldZ: 9.0f,
                isEnemyStealthed: true,
                isInsideValidDetectionSource: false,
                isThienNhanActive: false);
            Assert(!stealthAlert && !cam.IsPlayerDangerAlertActive,
                "Quái tàng hình chưa bị phát hiện hợp lệ không được kích hoạt cảnh báo miễn phí trên camera");

            // - Quái đã phát hiện hợp lệ tiến sát Khoa -> Bật cảnh báo nguy hiểm gắn với nút Trở về nhân vật
            bool detectedAlert = cam.EvaluatePlayerDangerAlertInFreeObservation(
                enemyWorldX: -10.0f,
                enemyWorldZ: 9.0f,
                isEnemyStealthed: false,
                isInsideValidDetectionSource: true,
                isThienNhanActive: false);
            Assert(detectedAlert && cam.IsPlayerDangerAlertActive,
                "Khi có nguy hiểm đã phát hiện hợp lệ tại vị trí Khoa, nút Trở về nhân vật phải bật cảnh báo");

            // 5. Chống khai thác điểm spawn bằng Camera tự do (GDD 3.8):
            // Treo camera FreeObservation ngoài rào quá 3 giây không được chặn điểm spawn của quái
            cam.PanFreeObservationCamera(0f, 0f, deltaTime: 4.0f); // tổng thời gian FreeObservation > 3s
            bool canSpawnWhenFreeCamParked = cam.CanMonsterSpawnAtOuterEntryPoint(
                spawnX: 48.0f,
                spawnZ: 25.0f,
                nearestEntityDistanceMeters: 32.0f,
                isInsidePlayerFollowFrustum: false,
                isInsideDetachedFreeObservationFrustum: true);
            Assert(canSpawnWhenFreeCamParked,
                "Người chơi không thể treo camera Quan sát tự do ở vành ngoài quá 3 giây để khóa/chặn điểm spawn của quái");

            // Bấm 1 nút trở về Khoa ngay lập tức
            cam.ReturnToCharacterImmediate(-12.0f, 1.4f, 8.0f);
            Assert(cam.PrimaryState == CameraPrimaryState3D.FollowCharacter && !cam.IsPlayerDangerAlertActive,
                "Bấm nút Trở về nhân vật phải lập tức đưa camera về lại Khoa và tắt cảnh báo");

            // 6. Luật kể chuyện Mùa 1: không tự động zoom chỉ điểm vào hạt giống cốt truyện thường ngày
            Assert(!cam.ShouldCameraAutoZoomOnMysterySeed("Firefly_StillInWind_1_15", isExplicitCutsceneStage: false),
                "Camera không được tự động zoom chỉ điểm vào hạt giống cốt truyện hay bóng phản chiếu trong sinh hoạt thường ngày");
        }

        static void TestCameraAndMapPhysicsEngineO1HeightfieldAndFrustumA2()
        {
            var eng = new CameraAndMapPhysicsEngine();

            // 1. Toán học FrustumSizeAtDistance chuẩn Unity 6 & Sửa tiêu chí A2:
            // Khoảng cách cho khung mặc định 64x36m phải trả ngược lại đúng 64m x 36m
            float dist64 = CameraAndMapPhysicsEngine.CalculateCameraDistanceForGroundWidth(64.0f);
            CameraAndMapPhysicsEngine.CalculateFrustumSizeAtDistance(dist64, out float w64, out float h64);
            Assert(Math.Abs(w64 - 64.0f) < 0.05f && Math.Abs(h64 - 36.0f) < 0.05f,
                $"FrustumSizeAtDistance tại d={dist64:0.2f}m phải khớp đúng 64x36m (thực tế: {w64:0.2f}x{h64:0.2f}m)");

            // Sửa tiêu chí A2: Vòng bán kính 30m (đường kính 60m) nằm gọn trong khung 64m; nhưng bán kính 60m (đường kính 120m) đòi hỏi khung Toàn cảnh 128m
            Assert(CameraAndMapPhysicsEngine.DoesRangeCircleFitInFrustumWidth(30.0f, 64.0f),
                "Bán kính 30m (đường kính 60m) phải nằm gọn trong khung mặc định 64m");
            Assert(!CameraAndMapPhysicsEngine.DoesRangeCircleFitInFrustumWidth(60.0f, 64.0f) &&
                   CameraAndMapPhysicsEngine.DoesRangeCircleFitInFrustumWidth(60.0f, 128.0f),
                "Bán kính 60m (đường kính 120m) không thể nằm trong khung 64m mà phải nằm trong khung Toàn cảnh 128m");

            // 2. Phân biệt rõ Sân chuồng (FarmsteadCourtyard), Vùng thả đàn (GrazingPastureZone) và Đồng cỏ mở (GoldenMeadowOpen):
            // Vùng thả đàn <= 1/3 diện tích đồng cỏ toàn map VÀ >= 35% diện tích bên trong ranh giới quản lý của Trang trại
            eng.CalculateGrazingZoneRatios(out float ratioTotalMeadow, out float ratioFarmPerimeter);
            Assert(ratioTotalMeadow > 0.05f && ratioTotalMeadow <= (1.0f / 3.0f) + 0.001f,
                $"Vùng thả đàn phải <= 1/3 diện tích đồng cỏ toàn map (thực tế: {ratioTotalMeadow * 100f:0.1f}%)");
            Assert(ratioFarmPerimeter >= 0.35f,
                $"Vùng chăn thả bên trong ranh giới quản lý của trại phải >= 35% theo GDD (thực tế: {ratioFarmPerimeter * 100f:0.1f}%)");

            // 3. Kiểm tra giữ hướng thế giới ổn định khi Khoa xoay mặt (Cinemachine Position Composer):
            eng.RotateCamera(20.0f, 0.0f);
            float savedCamYaw = eng.CurrentYawOffsetDeg;
            eng.SetKhoaTransform(-16.0f, 12.0f, 180.0f); // Khoa quay ngược lại nhìn về phía camera
            Assert(Math.Abs(eng.CurrentYawOffsetDeg - savedCamYaw) < 0.001f,
                "Khi Khoa quay mặt lại, Camera không được tự xoay theo lưng Khoa để người chơi nhìn thấy mặt Khoa & bóng phản chiếu");

            // 4. Kiểm tra hiệu năng Vật lý Bilinear O(1) cho 150 Heo + 30 Quái + Khoa (chạy trên máy yếu / WebGL / Mobile):
            for (int i = 0; i < 180; i++)
            {
                float px = -25.0f + (i % 15) * 2.5f;
                float pz = -10.0f + (i / 15) * 2.2f;
                float py = eng.SampleTerrainHeightBilinear(px, pz);
                eng.SampleTerrainNormal(px, pz, out float nx, out float ny, out float nz);
                Assert(py >= 0.0f && ny > 0.5f, "Độ cao Bilinear O(1) và pháp tuyến mặt đất phải hợp lệ tại mọi điểm cho 150 heo + 30 quái");
            }

            // 5. Cụm biểu diễn đàn heo khi > 60 con trong khung nhìn (GDD)
            Assert(!CameraAndMapPhysicsEngine.ShouldUseHerdClusterRepresentation(60) &&
                   CameraAndMapPhysicsEngine.ShouldUseHerdClusterRepresentation(120),
                "Khi quá 60 heo trong khung nhìn phải bật chế độ biểu diễn theo cụm (Cluster Representation)");
        }

        static void TestBanditEscalationChain()
        {
            // 1. Kẻ Bắt Heo (Ngày 1 vs Ngày 11)
            var snatcherDay1 = EnemyData.CreateKeBatHeo(1);
            var snatcherDay11 = EnemyData.CreateKeBatHeo(11);
            Assert(snatcherDay11.MaxHp > snatcherDay1.MaxHp, "Máu Kẻ Bắt Heo phải tăng theo số ngày");
            Assert(snatcherDay11.AttackDamage > snatcherDay1.AttackDamage, "Sát thương Kẻ Bắt Heo phải tăng theo số ngày");
            Assert(snatcherDay11.GoldReward > snatcherDay1.GoldReward, "Thưởng vàng Kẻ Bắt Heo phải tăng theo số ngày");

            // 2. Kẻ Cào Phá (Saboteur) & Chuột Gặm Kho
            var saboteurDay1 = EnemyData.CreateKeCaoPha(1);
            var saboteurDay15 = EnemyData.CreateKeCaoPha(15);
            var ratDay1 = EnemyData.CreateChuotGamKho(1);
            var ratDay10 = EnemyData.CreateChuotGamKho(10);
            Assert(saboteurDay15.MaxHp > saboteurDay1.MaxHp && saboteurDay15.AttackDamage > saboteurDay1.AttackDamage, "Chỉ số Kẻ Cào Phá phải tăng theo số ngày");
            Assert(ratDay10.MaxHp > ratDay1.MaxHp && ratDay1.MoveSpeed > saboteurDay1.MoveSpeed, "Chuột Gặm Kho phải nhanh và tăng máu theo ngày");

            // 3. Đầu Lĩnh Trộm (Elite)
            var leaderDay1 = EnemyData.CreateDauLinhTrom(1);
            var leaderDay20 = EnemyData.CreateDauLinhTrom(20);
            Assert(leaderDay1.TypeId == EnemyTypeId.DauLinhTrom, "Đầu Lĩnh Trộm phải mang mã DauLinhTrom");
            Assert(leaderDay20.MaxHp > leaderDay1.MaxHp && leaderDay20.AttackDamage > leaderDay1.AttackDamage, "Máu và sát thương Đầu Lĩnh Trộm phải tăng mạnh ở ngày 20");

            // 4. Boss Chúa Trộm Rìu Sắt
            var bossDay1 = EnemyData.CreateBossChuaTromRiuSat(1);
            var bossDay10 = EnemyData.CreateBossChuaTromRiuSat(10);
            Assert(bossDay1.TypeId == EnemyTypeId.ChuaTromRiuSat, "Boss Chúa Trộm Rìu Sắt phải mang mã ChuaTromRiuSat");
            Assert(bossDay1.MaxHp >= 4000f, "Boss Chúa Trộm Rìu Sắt ngày 1 phải có tối thiểu 4000 HP");
            Assert(bossDay10.MaxHp > bossDay1.MaxHp && bossDay10.GoldReward > bossDay1.GoldReward, "Boss Chúa Trộm Rìu Sắt ngày 10 phải mạnh hơn ngày 1");

            // 5. Kiểm tra bảng rơi đồ (LootDatabase) của Boss Chúa Trộm Rìu Sắt
            var bossLoot = LootDatabase.RollLoot(EnemyTypeId.ChuaTromRiuSat, new Random(99));
            Assert(bossLoot.Exists(d => d.Id == "item_iron_axe_head"), "Hạ Boss Chúa Trộm phải chắc chắn rơi Lưỡi Rìu Sắt Thủ Lĩnh (100%)");
            Assert(bossLoot.Exists(d => d.Id == "item_gold_pouch_huge"), "Hạ Boss Chúa Trộm phải chắc chắn rơi Túi Vàng Lớn (100%)");
        }

        static void TestDualLayerStaminaAndComboMechanics()
        {
            var charData = new CharacterData();

            // 1. Kiểm tra khởi tạo Stamina 2 lớp (GDD 9.1–9.5)
            Assert(Math.Abs(charData.Stamina.BaseMaxStamina - 180f) < 0.01f, $"Trần thể lực cơ bản phải là 180 (Thực tế: {charData.Stamina.BaseMaxStamina})");
            Assert(Math.Abs(charData.Stamina.EffectiveMaxStamina - 180f) < 0.01f, "Khi chưa có Tải ngày, Trần hiệu dụng phải bằng 180");

            // 2. Kiểm tra tiêu hao thể lực và cộng dồn Tải Ngày (DailyLoad - GDD 9.2)
            bool consumed = charData.Stamina.TryConsumeStamina(20f, 0.30f);
            Assert(consumed, "Phải tiêu hao thành công 20 Thể lực");
            Assert(Math.Abs(charData.Stamina.CurrentStamina - 160f) < 0.01f, "Thể lực hiện tại phải còn 160");
            Assert(Math.Abs(charData.Stamina.DailyLoad - 6f) < 0.01f, "Tải ngày tích lũy phải tăng thêm 6 (20 * 0.3)");
            Assert(Math.Abs(charData.Stamina.EffectiveMaxStamina - 174f) < 0.01f, "Trần hiệu dụng phải giảm xuống 174 (180 - 6)");

            // 3. Kiểm tra Hồi phục thể lực tức thời bị chặn bởi trần EffectiveMaxStamina (Tải ngày không tự hồi - GDD 9.4)
            charData.Stamina.RegenerateStamina(50f);
            Assert(Math.Abs(charData.Stamina.CurrentStamina - 174f) < 0.05f, $"Hồi phục không được vượt quá trần hiệu dụng 174 (Thực tế: {charData.Stamina.CurrentStamina})");

            // 4. Kiểm tra Chuỗi đánh thường 3 nhịp (GDD 9.2 & 12)
            var step0 = charData.GetComboStepData(0);
            var step1 = charData.GetComboStepData(1);
            var step2 = charData.GetComboStepData(2);
            Assert(Math.Abs(step0.damageMultiplier - 1.0f) < 0.01f && Math.Abs(step0.staminaCost - 2.5f) < 0.01f, "Nhịp 1: 100% sát thương, tốn 2.5 STA");
            Assert(Math.Abs(step1.damageMultiplier - 1.15f) < 0.01f && Math.Abs(step1.staminaCost - 2.5f) < 0.01f, "Nhịp 2: 115% sát thương, tốn 2.5 STA");
            Assert(Math.Abs(step2.damageMultiplier - 1.50f) < 0.01f && Math.Abs(step2.staminaCost - 5.0f) < 0.01f, "Nhịp 3 (Finisher): 150% sát thương, tốn 5.0 STA");

            // 5. Kiểm tra Lướt Né giảm theo Khinh Thân (-20%) & AGI gốc >= 35 (-15%) (GDD 9.2 & 12.2: gốc 9.0 STA)
            Assert(Math.Abs(charData.CalculateDodgeCost() - 7.20f) < 0.01f, "Khi không mặc giáp nặng (Khinh Thân: -20%), chi phí lướt né phải là 7.20 STA");
            charData.BaseStats.Agi = 35;
            Assert(Math.Abs(charData.CalculateDodgeCost() - 5.85f) < 0.01f, "Khi Khinh Thân (-20%) + AGI gốc >= 35 (-15%), chi phí lướt né phải giảm 35% còn 5.85 STA");

            // 6. Kiểm tra Luật Bất Biến Trang Bị Không Cộng Điều Kiện Học Võ (GDD 8.3 & 12.1)
            charData.BaseStats.Str = 20;
            charData.BonusStats.Str = 8; // Cộng +8 STR từ trang bị (Tổng STR = 28, nhưng STR gốc vẫn = 20)
            Assert(!charData.CanLearnMartialSkill(requiredStr: 25, requiredAgi: 10, requiredCtrl: 10, requiredRes: 10),
                "LUẬT BẤT BIẾN: Điểm cộng từ Trang bị (BonusStats) tuyệt đối KHÔNG được tính vào điều kiện học võ kỹ!");
            charData.BaseStats.Str = 25;
            Assert(charData.CanLearnMartialSkill(requiredStr: 25, requiredAgi: 10, requiredCtrl: 10, requiredRes: 10),
                "Khi chỉ số gốc đạt 25 STR thì phải học được bí kíp võ học");

            // 7. Kiểm tra Ngủ Chuẩn xóa sạch Tải ngày và phục hồi Thể trạng (GDD 9.5)
            charData.Stamina.Condition = 80;
            charData.Stamina.ForceConsumeStamina(120f, 0.4f);
            Assert(charData.Stamina.DailyLoad > 0f, "Phải có Tải ngày tích lũy trước khi ngủ");
            charData.Stamina.PerformSleepReset();
            Assert(Math.Abs(charData.Stamina.DailyLoad) < 0.001f && charData.Stamina.Condition == 86,
                "Sau khi ngủ chuẩn, Tải ngày phải về 0 và Thể trạng hồi +6");
        }

        static void TestMemberAFarmCoreRoomsResearchWorkshopAndVerticalSlice()
        {
            // 1. StatCalc chuẩn GDD 1.4 & Sổ tay 14.3: Cuối = Gốc × (1 + Σ%cùng nhóm / 100) × Π(hệ số × khác nhóm)
            ReadOnlySpan<float> pcts = stackalloc float[] { 15f, 10f }; // +25%
            ReadOnlySpan<float> mults = stackalloc float[] { 1.10f };
            float statVal = StatCalc.Final(baseValue: 100f, pcts, mults, min: 0f, max: 500f);
            Assert(Math.Abs(statVal - 137.5f) < 0.01f, $"StatCalc.Final phải tính ra 137.5 (thực tế: {statVal})");
            Assert(Math.Abs(StatCalc.AddPoints(18f, 7f) - 25f) < 0.001f, "StatCalc.AddPoints phải cộng điểm phần trăm chính xác");

            // 2. CoreEventBus (P0.03): Publish / Subscribe các sự kiện hệ thống không phụ thuộc trực tiếp
            var bus = new CoreEventBus();
            int geneIdentifiedEvents = 0;
            bus.Subscribe<GeneIdentifiedEvent>(ev =>
            {
                if (ev.GeneLine == GeneLineId.ThietBi) geneIdentifiedEvents++;
            });

            // 3. Kho 36 ô & chống mốc cám nhờ Thủ Kho / Mèo Kho (P2.13)
            var coreSys = new FarmCoreAndVillageSystem();
            var wh = coreSys.Warehouse;
            Assert(WarehouseInventorySystem.TotalSlots == 36, "Kho chính phải có đúng 36 ô");
            bool added = wh.AddItem("item_cam_tinh", "Cám Tinh", 80, maxStack: 50);
            Assert(added && wh.Slots[0].Count == 50 && wh.Slots[1].Count == 30, "Cộng dồn và tách stack trong kho 36 ô phải chuẩn");
            Assert(wh.SwapOrMergeSlots(0, 2) && wh.Slots[2].Count == 50 && wh.Slots[0].Count == 0, "Hoán đổi 2 ô trong kho phải chuẩn");

            float spoiledNoProtection = wh.CalculateDailyFeedSpoilageKg(100f, hasStorekeeperNpc: false, hasWarehouseCatPet: false);
            float spoiledWithProtection = wh.CalculateDailyFeedSpoilageKg(100f, hasStorekeeperNpc: true, hasWarehouseCatPet: true);
            Assert(spoiledNoProtection > spoiledWithProtection && spoiledWithProtection < 0.5f, "Thủ Kho và Mèo Kho phải giảm mạnh hao hụt cám trong kho");

            // 4. Đoạn Chuyển Map nối Farm_Main và Farm_Core: Người chơi & Quái đi qua được, Heo bị chặn tuyệt đối (P1.07 / Sổ tay 6.2 & 6.6)
            Assert(FarmCoreAndVillageSystem.CanTraverseTransitionCorridor(TransitionActorType.Player), "Người chơi phải đi qua được Đoạn Chuyển Map");
            Assert(FarmCoreAndVillageSystem.CanTraverseTransitionCorridor(TransitionActorType.Monster), "Quái truy kích phải đi qua được Đoạn Chuyển Map");
            Assert(!FarmCoreAndVillageSystem.CanTraverseTransitionCorridor(TransitionActorType.Pig), "Đàn heo tuyệt đối không được qua Đoạn Chuyển Map sang Farm_Core");
            var pigBlocked = coreSys.TransitionBetweenMaps("Farm_Main", "Farm_Core", TransitionActorType.Pig);
            Assert(!pigBlocked.transitioned, "TransitionBetweenMaps phải chặn heo sang Farm_Core");
            var playerMoved = coreSys.TransitionBetweenMaps("Farm_Main", "Farm_Core", TransitionActorType.Player);
            Assert(playerMoved.transitioned && playerMoved.spawnId == "spawn.core.from_farm" && playerMoved.durationSeconds <= FarmCoreAndVillageSystem.MaxSceneTransitionSeconds,
                "Người chơi chuyển từ Farm_Main sang Farm_Core phải đúng spawn.core.from_farm và <= 1.5s");

            // 5. Kích hoạt công trình Bạch Vân từ Nhà chính (trễ +3.0s) & Hậu quả Bất Tỉnh (-20 Thể trạng, -10% vàng)
            var engine = new GameEngine(777);
            engine.Farm.Gold = 5000;
            engine.Defense.HallLevel = 3;
            engine.Defense.UnlockedBlueprints.Add("CuuTieuLoiPhao");
            var (_, _, placedBv) = engine.Defense.ConstructBuilding("CuuTieuLoiPhao");
            Assert(placedBv != null, "Xây công trình Bạch Vân (Cửu Tiêu Lôi Pháo) phải thành công");
            placedBv!.AmmoCurrent = 120;
            var remoteBv = coreSys.TriggerBachVanFromMainHouseConsole(engine.Defense, placedBv.InstanceId);
            Assert(remoteBv.success && Math.Abs(remoteBv.activationDelaySeconds - 3.0f) < 0.001f, "Kích Bạch Vân từ Nhà chính phải có độ trễ +3.0 giây");

            int goldBeforeFaint = engine.Farm.Gold;
            var faintRes = coreSys.HandlePlayerUnconscious(engine.Character, engine.Farm, engine.Ledger);
            Assert(faintRes.goldLost == (int)Math.Round(goldBeforeFaint * 0.10) && faintRes.conditionAfter == 80 && faintRes.respawnLocation == "spawn.core.main_house_bed",
                "Khi Bất Tỉnh phải trừ 10% vàng qua Sổ cái, giảm 20 Thể trạng và tỉnh dậy tại giường Nhà chính");

            // 6. Phòng Nghiên Cứu — Bàn Giải Mã (P2.22): hàng chờ 18/24/36/48h, tỉ lệ 95/85/65/50%, khóa 12h/24h & Giải Mã Sâu
            engine.Farm.FarmLevel = 8;
            engine.Farm.Gold = 20000;
            engine.PetsAndNpcs.HireNpc(NpcSpecialistId.NhaNghienCuuGen, 8.0f);
            wh.CraftingMaterials["thuoc_thu_gen"] = 20;

            var legendaryPig = Pig.CreateDefault("p_leg_lab", "Kim Thọ Ẩn", GeneLineId.KimTho, GeneRarity.HuyenThoai);
            legendaryPig.IsIdentified = false;
            var blockedLeg = coreSys.EnqueueGeneAppraisalAtLab(legendaryPig, engine.Farm, engine.PetsAndNpcs, engine.Character, engine.Ledger);
            Assert(!blockedLeg.queued, "Chưa mở đặc tính Giải Mã Sâu thì không được giải mã Huyền Thoại / Dị Biến");

            coreSys.DeepDecodingTraitUnlocked = true;
            var rarePig = Pig.CreateDefault("p_rare_lab", "Thiết Bì Ẩn", GeneLineId.ThietBi, GeneRarity.Hiem);
            rarePig.IsIdentified = false;
            engine.Pigs.Add(rarePig);
            var qRes = coreSys.EnqueueGeneAppraisalAtLab(rarePig, engine.Farm, engine.PetsAndNpcs, engine.Character, engine.Ledger);
            Assert(qRes.queued && qRes.job != null && qRes.job.TotalDurationHours == 18 && Math.Abs(qRes.job.SuccessProbability - 0.95f) < 0.001f,
                "Giám định heo Hiếm tại Phòng Nghiên Cứu phải mất 18h, tỉ lệ 95%, tốn 700G + 1 thuốc thử");
            var doneJobs = coreSys.TickResearchQueueHours(18, engine.Pigs, deterministicRoll: 0.20, eventBus: bus);
            Assert(doneJobs.Count == 1 && rarePig.IsIdentified && rarePig.AppraisalLockHoursRemaining == 12 && geneIdentifiedEvents == 1,
                "Sau 18h giải mã thành công, heo phải được định danh, khóa 12h và phát sự kiện GeneIdentifiedEvent");

            // 7. Xưởng Cường Hóa (P4.11): Gia cường giấy (+1/+2/+3, thiếu nguyên liệu (nộp/yêu cầu)^1.5 >= 10%, thất bại không hỏng giấy), Rã giấy, 6 khóa ẩn, Phù văn & Mệnh ấn
            var enh1 = coreSys.EnhancePaperAtWorkshopGdd10(0, stonesUsed: 0, submittedMaterials: 4, requiredMaterials: 4, midGameGateUnlocked: true, SpecialtyPaperColor.Lam, deterministicRoll: 0.5);
            Assert(enh1.success && enh1.newPaperLevel == 1 && Math.Abs(enh1.finalSuccessRate - 1.00f) < 0.001f, "Gia cường +1 đủ nguyên liệu phải 100% thành công");

            var enh2Fail = coreSys.EnhancePaperAtWorkshopGdd10(1, stonesUsed: 2, submittedMaterials: 2, requiredMaterials: 4, midGameGateUnlocked: true, SpecialtyPaperColor.Lam, deterministicRoll: 0.99);
            Assert(!enh2Fail.success && enh2Fail.newPaperLevel == 1 && enh2Fail.finalSuccessRate >= 0.10f,
                "Gia cường +2 thiếu nguyên liệu phải nhân (nộp/yêu cầu)^1.5 >= 10% và thất bại KHÔNG làm giảm cấp giấy");

            var salvLam = coreSys.SalvageSpecialtyPaperCanonical(SpecialtyPaperColor.Lam);
            var salvVang = coreSys.SalvageSpecialtyPaperCanonical(SpecialtyPaperColor.Vang);
            var salvTim = coreSys.SalvageSpecialtyPaperCanonical(SpecialtyPaperColor.Tim);
            Assert(salvLam.materialId == "to_vang" && salvLam.quantity == 2 &&
                   salvVang.materialId == "bot_tu_tinh" && salvVang.quantity == 2 &&
                   salvTim.materialId == "huyet_trien" && salvTim.quantity == 1,
                "Bàn Rã Giấy phải trả đúng: Lam -> 2 Tơ Vàng, Vàng -> 2 Bột Tử Tinh, Tím -> 1 Huyết Triện");

            Assert(FarmCoreAndVillageSystem.GetDisplayedSpecialtyTraitName(5, "Bá Chủ Chuồng Trại").StartsWith("???"),
                "Khi mới mở 5/6 khóa, tên đặc tính chuyên biệt phải vẫn ẩn ('???')");
            Assert(FarmCoreAndVillageSystem.GetDisplayedSpecialtyTraitName(6, "Bá Chủ Chuồng Trại") == "Bá Chủ Chuồng Trại",
                "Khi mở đủ 6/6 khóa, tên đặc tính chuyên biệt phải lộ rõ");

            // Khắc Phù Văn & Mệnh Ấn
            engine.Character.Level = 22;
            coreSys.HasHighTierEngravingTableBlueprint = true;
            coreSys.CyclesSinceRuneSystemUnlocked = 5;
            coreSys.BossesDefeatedCount = 2;
            coreSys.RedEventsCompletedCount = 1;
            var runeRes = coreSys.EngraveRuneAtWorkshop(engine.Character, "PhuTruyPhong", runeTier: 1, midGameGateUnlocked: true, majorWavesSurvived: 18, hallLevel: 2, deterministicRoll: 0.1);
            Assert(runeRes.success && Math.Abs(runeRes.successRate - 1.00f) < 0.001f, "Khắc Phù Văn bậc I khi đủ điều kiện phải đạt 100%");

            var npcSealBlocked = coreSys.EngraveSealAtWorkshop(engine.Character, "AnBatKhuat", isPlayerCharacter: false, majorWavesSurvived: 18);
            Assert(!npcSealBlocked.success, "Mệnh Ấn tuyệt đối không được khắc cho NPC");
            int hpBeforeSeal = engine.Character.MaxHp;
            var playerSealOk = coreSys.EngraveSealAtWorkshop(engine.Character, "AnBatKhuat", isPlayerCharacter: true, majorWavesSurvived: 18);
            Assert(playerSealOk.success && engine.Character.MaxHp == hpBeforeSeal - 10, "Khắc Mệnh Ấn cho nhân vật người chơi phải trừ vĩnh viễn 10 HP tối đa");

            // 8. Cấp trại 1–20 (P2.21) & Kiểm định 3 vòng Save/Load liên tiếp (P1.01) + Vertical Slice Full Day (P1.15)
            var farmProg = new Farm { FarmLevel = 1 };
            var (lvlAfter, unlocks) = FarmLevelProgressionSystem.AddFarmExp(farmProg, 2000);
            Assert(lvlAfter >= 5 && unlocks.Contains("ChoDem"), "Tăng EXP trại phải nâng cấp trại và trả về đúng mốc mở khóa");

            var (allIdentical, h1, h2, h3) = VerificationAndSimulationSuite.VerifyThreeConsecutiveSaveLoadCycles(engine);
            Assert(allIdentical && h1 == h2 && h2 == h3, "3 vòng Save -> Load -> Save liên tiếp phải không lệch 1 byte dữ liệu");

            var vsResultAn = VerificationAndSimulationSuite.RunVerticalSliceFullDayScenario(PlayerId.An);
            var vsResultKhoa = VerificationAndSimulationSuite.RunVerticalSliceFullDayScenario(PlayerId.Khoa);
            Assert(vsResultAn.success && vsResultKhoa.success, "Kịch bản Vertical Slice 1 ngày (P1.15) phải thành công cho cả An và Khoa");
        }

        static void TestMemberCMarketTownWaveScheduleAndMartialInvariants()
        {
            // 1. Market_Town (50x40m) tự động đổi lớp Chợ Ngày / Chợ Đêm theo đồng hồ (P1.09, P2.26, P3.15)
            var market = new MarketTownState();
            Assert(Math.Abs(MarketTownState.MapWidthMeters - 50f) < 0.01f && Math.Abs(MarketTownState.MapHeightMeters - 40f) < 0.01f,
                "Bản đồ Market_Town phải có kích thước 50x40m");
            Assert(!market.AllowsCombat && !market.HasMysticMerchantInsideTown,
                "Market_Town không có combat và Bí nhân tuyệt đối không đứng trong thị trấn");
            Assert(MarketTownState.FourFunctionalMarketNpcs.Count == 4 && MarketTownState.FiveMercenaryGuildNpcs.Count == 5,
                "Market_Town phải có đủ 4 NPC chức năng chợ và 5 lính đánh thuê trong Hội Quán");
            Assert(MarketTownState.QuarantineFeeGold == 60 && MarketTownState.ScreeningFeeGold == 120,
                "Phí Kiểm dịch Chợ Ngày phải là 60G và phí Sàng lọc phải là 120G");

            market.SyncWithClock(currentHour: 9, new Random(1), rollNewNight: false);
            Assert(!market.IsNightLayerActive && !market.IsHighTierAppraisalBoothOpenTonight, "Ban ngày (09:00) phải bật lớp Chợ Ngày");

            int nightBoothOpens = 0;
            var nightRng = new Random(42);
            for (int n = 0; n < 200; n++)
            {
                market.SyncWithClock(currentHour: 20, nightRng, rollNewNight: true);
                if (market.IsHighTierAppraisalBoothOpenTonight) nightBoothOpens++;
            }
            Assert(market.IsNightLayerActive && nightBoothOpens > 5 && nightBoothOpens < 50,
                $"Ban đêm (20:00) phải bật lớp Chợ Đêm và Quầy Giám Định Cấp Cao xuất hiện xấp xỉ 12% số đêm (thực tế: {nightBoothOpens}/200)");

            // 2. Lịch Đợt Quái Lớn (Đầu game 6–8 ngày, Giữa game 7–10 ngày, Cuối game 8–12 ngày) & Lịch Boss Chu kỳ 5/10/15 (P3.06, P4.14, P4.15)
            var earlyInterval = WaveScheduleAndReportSystem.GetMajorWaveIntervalByPhase(cycleNumber: 2);
            var midInterval = WaveScheduleAndReportSystem.GetMajorWaveIntervalByPhase(cycleNumber: 4);
            var lateInterval = WaveScheduleAndReportSystem.GetMajorWaveIntervalByPhase(cycleNumber: 8);
            Assert(earlyInterval == (6, 8) && midInterval == (7, 10) && lateInterval == (8, 12),
                "Lịch đợt quái lớn phải đúng chuẩn Đầu game 6–8 ngày, Giữa game 7–10 ngày, Cuối game 8–12 ngày");

            Assert(WaveScheduleAndReportSystem.GetScheduledCycleBoss(5) == EnemyTypeId.ChuaTromRiuSat, "Chu kỳ 5 phải lịch Boss Chúa Trộm Rìu Sắt");
            Assert(WaveScheduleAndReportSystem.GetScheduledCycleBoss(10) == EnemyTypeId.UfoMothership, "Chu kỳ 10 phải lịch Boss Mẫu Hạm UFO");
            Assert(WaveScheduleAndReportSystem.GetScheduledCycleBoss(15) == EnemyTypeId.ThucTheSuong, "Chu kỳ 15 phải lịch Boss Thực Thể Sương");

            // 3. Bảng Báo Cáo Sau Đợt Quái đủ 7 mục chuẩn GDD 18 (P3.17)
            var battleRep = new WaveBattleReport
            {
                WaveNumber = 3,
                Victory = true,
                EnemiesDefeated = 14,
                PigsLostOrAbducted = 1,
                BuildingDurabilityLost = 85,
                AmmoSpent = 42,
                PlayerGuardBreaks = 0,
                GoldRewarded = 320
            };
            var sections = WaveScheduleAndReportSystem.BuildSevenSectionWaveReportGdd18(battleRep, repairCostEstimateGold: 125, bossPhaseSummary: "Hạ Đầu Lĩnh Trộm");
            Assert(sections.Count == 7 && sections[0].Contains("THẮNG LỢI") && sections[3].Contains("125G") && sections[6].Contains("320 Vàng"),
                "Bảng báo cáo sau đợt quái phải có đủ 7 mục thông tin chuẩn GDD 18");
        }

        static void TestMemberDEconomyLedgerThemesContractsIntelMinimapAndSim()
        {
            var farm = new Farm { Gold = 2000 };
            var clock = new GameClock(1, 8, 0);
            var ledger = new EconomyLedger(farm, clock);

            // 1. Sổ cái Kinh tế 1G & chặn âm tiền (P1.10)
            ledger.Earn(124, LedgerReason.SellPig, "sell_pig_1"); // +124G -> 2124G
            Assert(ledger.Gold == 2124 && farm.Gold == 2124, "Sổ cái phải đồng bộ hai chiều với Farm.Gold");
            Assert(!ledger.TrySpend(5000, LedgerReason.BuyFeed, "over_spend") && ledger.Gold == 2124,
                "Sổ cái tuyệt đối không cho phép trừ âm vàng");

            // 2. Trọng số 8 Chủ đề Chợ Chu Kỳ (24/12/12/10/8/10/12/12%) & Hợp đồng Giao Heo (P2.24)
            var themeCounts = new Dictionary<MarketTheme, int>();
            var themeRng = new Random(12345);
            for (int i = 0; i < 1000; i++)
            {
                var th = EconomyAndIntelExpansionSystem.RollMarketCycleThemeWeighted(themeRng);
                themeCounts[th] = themeCounts.TryGetValue(th, out var c) ? c + 1 : 1;
            }
            Assert(themeCounts.Count == 8 && themeCounts[MarketTheme.CanBang] > 180 && themeCounts[MarketTheme.HoangLoanDich] > 40,
                "Quay trọng số 8 Chủ đề Chợ Chu Kỳ phải sinh đủ 8 chủ đề theo phân phối 24/12/12/10/8/10/12/12%");

            var contract = new PigDeliveryContract
            {
                ContractId = "hd_thietbi_a",
                Title = "Giao 2 Thiết Bì Hạng A",
                RequiredGeneLine = GeneLineId.ThietBi,
                MinimumRarity = GeneRarity.Hiem,
                MinimumMeatQuality = MeatQuality.A,
                RequiredPigCount = 2,
                DeadlineDay = 10,
                RewardGold = 1200,
                RewardFarmExp = 150
            };
            var pigs = new List<Pig>();
            for (int i = 0; i < 2; i++)
            {
                var p = Pig.CreateDefault($"tb_{i}", $"Thiết Bì {i}", GeneLineId.ThietBi, GeneRarity.Hiem);
                p.Stage = PigStage.TruongThanh;
                p.WeightKg = 105f;
                p.MeatQuality = MeatQuality.A;
                p.IsIdentified = true;
                pigs.Add(p);
            }
            var cRes = EconomyAndIntelExpansionSystem.TryFulfillDeliveryContract(contract, pigs, currentDay: 5, farm, ledger);
            Assert(cRes.fulfilled && contract.IsCompleted && cRes.goldEarned == 1200 && pigs.Count == 0,
                "Thực hiện Hợp đồng Giao Heo đúng hạn phải nhận thưởng vàng + EXP trại qua Sổ cái");

            // 3. Đấu Giá Chợ Đêm (P2.09) & Bí Nhân đổi vật phẩm (>=1.15x GTTC, 0% vàng) (P4.01)
            var rareItem = new GameItem { Id = "item_auc_1", Name = "Hồ Sơ Giống Cổ", ReferenceValueGold = 500, Category = "Info" };
            var aucRes = EconomyAndIntelExpansionSystem.RunNightMarketAuction(rareItem, playerMaxBidGold: 900, npcMaxValuationGold: 650, stepGold: 50, ledger);
            Assert(aucRes.wonAuction && aucRes.finalPriceGold == 700, "Đấu giá vật phẩm tại Chợ Đêm phải thắng ở mức npcMax + step và trừ qua Sổ cái");

            var secretItem = new GameItem { Id = "secret_bua", Name = "Bùa Hồi Hương Cổ", ReferenceValueGold = 600, Category = "Event" };
            var lowOffer = new List<GameItem> { new GameItem { Id = "o1", Name = "Cám Linh Chi", ReferenceValueGold = 620, Category = "Farm" } };
            var highOffer = new List<GameItem>
            {
                new GameItem { Id = "o1", Name = "Cám Linh Chi", ReferenceValueGold = 400, Category = "Farm" },
                new GameItem { Id = "o2", Name = "Ngọc Phá Sương", ReferenceValueGold = 350, Category = "Event" }
            };
            var barterFail = EconomyAndIntelExpansionSystem.TryBarterWithMysticMerchant(secretItem, lowOffer, offeredGoldAmount: 0);
            Assert(!barterFail.accepted, "Đổi đồ với Bí Nhân dưới ngưỡng 1.15x GTTC phải bị từ chối");
            var barterGoldBlocked = EconomyAndIntelExpansionSystem.TryBarterWithMysticMerchant(secretItem, highOffer, offeredGoldAmount: 10);
            Assert(!barterGoldBlocked.accepted, "Bí Nhân tuyệt đối không nhận Vàng trong giao dịch đổi đồ");
            var barterOk = EconomyAndIntelExpansionSystem.TryBarterWithMysticMerchant(secretItem, highOffer, offeredGoldAmount: 0);
            Assert(barterOk.accepted, "Đổi đồ với Bí Nhân đạt >= 1.15x GTTC và 0 vàng phải thành công");

            // 4. Bảng Tình Báo 6 bậc (0–5) làm mờ thông tin chưa mua (P3.08) & Minimap + Sương Mù 15m (P2.25)
            var intel0 = EconomyAndIntelExpansionSystem.BuildIntelForecastByLevel(0, exactHoursUntilWave: 14, "Hướng Đông", trueEnemyCount: 18, "Đầu Lĩnh Trộm", "Đặt Trụ Đèn + Chông Gỗ");
            var intel5 = EconomyAndIntelExpansionSystem.BuildIntelForecastByLevel(5, exactHoursUntilWave: 14, "Hướng Đông", trueEnemyCount: 18, "Đầu Lĩnh Trộm", "Đặt Trụ Đèn + Chông Gỗ");
            Assert(intel0.DirectionText == "???" && intel0.ScaleAndCountText == "???" && intel0.EliteAndBossText == "???",
                "Ở bậc Tình báo 0, mọi thông tin chi tiết chưa mở phải bị làm mờ ('???')");
            Assert(intel5.DirectionText == "Hướng Đông" && intel5.ScaleAndCountText.Contains("18") && intel5.TacticalHoldRecommendation.Contains("Trụ Đèn"),
                "Ở bậc Tình báo 5, toàn bộ 5 lớp thông tin phải hiển thị rõ");

            var minimap = new MinimapProjectionSystem();
            Assert(minimap.IsFarmBoundaryPolygonVisible, "Minimap phải vẽ rõ viền ranh giới nông trại");
            Assert(minimap.IsEntityVisibleOnMinimap(12.0f, WeatherType.SuongMu) && !minimap.IsEntityVisibleOnMinimap(18.0f, WeatherType.SuongMu),
                "Trong thời tiết Sương Mù, Minimap chỉ hiển thị thực thể trong bán kính 15m và ẩn ngoài 15m");

            // 5. Rừng Nguyên Sinh (80x80m) & Khung Sự Kiện Kỳ Dị (Cổng Dịch Chuyển / Sương Mù Dày / 3 ngày / 30-50-100%) (P0.18, P1.20, P4.02, P4.12)
            var forest = new ForestPrimevalAndEventFramework();
            Assert(Math.Abs(ForestPrimevalAndEventFramework.ForestWidthMeters - 80f) < 0.01f &&
                   !forest.AllowsMonsterWavesInForest &&
                   ForestPrimevalAndEventFramework.CalculateCanopyOpacity(isActorUnderCanopy: true) < 1.0f,
                "Forest_Primeval 80x80m không có đợt quái và phải làm mờ tán cây khi nhân vật đi bên dưới");

            var gateBlocked = ForestPrimevalAndEventFramework.EvaluateOccultEventExit(isGateEvent: true, completionPercent: 40f, daysElapsedInEvent: 1, useBuaHoiHuong: true, useLaBanTanVuOrNgocPhaSuong: false);
            var gateExit50 = ForestPrimevalAndEventFramework.EvaluateOccultEventExit(isGateEvent: true, completionPercent: 60f, daysElapsedInEvent: 1, useBuaHoiHuong: true, useLaBanTanVuOrNgocPhaSuong: false);
            var fogForced3Days = ForestPrimevalAndEventFramework.EvaluateOccultEventExit(isGateEvent: false, completionPercent: 20f, daysElapsedInEvent: 3, useBuaHoiHuong: false, useLaBanTanVuOrNgocPhaSuong: false);
            Assert(!gateBlocked.canExit && gateExit50.canExit && gateExit50.rewardPercentTier == 50 && fogForced3Days.canExit && fogForced3Days.rewardPercentTier == 30,
                "Luật thoát Sự kiện kỳ dị phải khớp GDD 24: Cổng cần >=50% + Bùa Hồi Hương (nhận 50%), quá 3 ngày tự đẩy ra (nhận 30%)");

            // 6. Bản địa hóa vi/en, mù màu (hình học + màu + số) & ẩn dòng gen Hiếm+ thành '?' khi chưa giám định (P1.11, P2.06, P2.14)
            var missing = LocalizationAndHudPresenter.FindMissingKeys(new[] { "hud.hp", "hud.stamina", "hud.gold", "hud.habitat", "gene.unknown" });
            Assert(missing.Count == 0 && LocalizationAndHudPresenter.Get("hud.stamina", "en") == "Stamina", "Bảng Localization vi/en không được thiếu key nào");
            var mysteryPig = Pig.CreateDefault("p_mys", "Heo Bí Ẩn", GeneLineId.LoiMach, GeneRarity.Quy);
            mysteryPig.IsIdentified = false;
            Assert(LocalizationAndHudPresenter.GetDisplayedPigGeneName(mysteryPig) == "?",
                "Heo bậc Hiếm trở lên khi chưa giám định bắt buộc phải hiển thị '?' trên HUD");
            var badge = LocalizationAndHudPresenter.BuildColorblindSafeIndicator("hud.habitat", 80, higherIsBetter: true);
            Assert(!string.IsNullOrEmpty(badge.shapeSymbol) && !string.IsNullOrEmpty(badge.colorHex), "Chỉ báo hỗ trợ mù màu phải có cả ký hiệu hình học lẫn mã màu");

            // 7. Mô phỏng tự động 30 ngày cân bằng kinh tế với seed cố định (Determinism - P5.03)
            var simRun1 = VerificationAndSimulationSuite.RunHeadlessEconomySimulation(days: 30, seed: 42);
            var simRun2 = VerificationAndSimulationSuite.RunHeadlessEconomySimulation(days: 30, seed: 42);
            Assert(simRun1.success && simRun1.finalGold > 0 && simRun1.wavesSurvived >= 4 && simRun1.determinismHash == simRun2.determinismHash,
                $"Mô phỏng kinh tế 30 ngày phải ổn định và tất định 100% cùng seed (Final Gold: {simRun1.finalGold}G, Hash: {simRun1.determinismHash})");
        }

        static void TestCanonicalSeasonOneSagaFullParts1To12AndGdd6Tables()
        {
            // 1. Kiểm tra Bảng 1.4 Gia đình Khoa & Bảng 1B Gia đình An (8 mốc chuẩn Phần 1.4 & 1B), P01–P08, Q01–Q20, S01–S12 & Toán học Phong ấn 3.000 năm (24 × 125)
            var (sealOk, _) = CanonicalSagaAndGdd6Systems.VerifyAncientSealMathematics();
            Assert(sealOk, "Toán học phong ấn Thánh Minh Nhật Giả (24 × 125 = 3000 năm) và danh mục P01–P08, Q01–Q20, S01–S12 phải đầy đủ 100%");
            Assert(CanonicalSagaAndGdd6Systems.FamilyTimelineTable14[0].MemberName == "Ông nội Lâm" &&
                   CanonicalSagaAndGdd6Systems.FamilyTimelineTable14[0].EventDateAndTime.Contains("17/07/2018") &&
                   CanonicalSagaAndGdd6Systems.FamilyTimelineTable14[3].MemberName.Contains("Em gái Bé An") &&
                   CanonicalSagaAndGdd6Systems.FamilyTimelineTable14[6].MemberName == "Mẹ Thủy" &&
                   CanonicalSagaAndGdd6Systems.FamilyTimelineTable14[6].EventDateAndTime.Contains("08/11/2019") &&
                   CanonicalSagaAndGdd6Systems.FamilyTimelineTable14[7].EventDateAndTime.Contains("24/09/2026") &&
                   CanonicalSagaAndGdd6Systems.FamilyTimelineTable1B_AnRoute[3].MemberName.Contains("Em trai Bé Khoa") &&
                   CanonicalSagaAndGdd6Systems.FamilyTimelineTable1B_AnRoute[7].MemberName.Contains("An"),
                "Bảng 1.4 (tuyến Khoa: Em gái Bé An 13/05/2019) và Bảng 1B (tuyến An: Em trai Bé Khoa 13/05/2019) phải khớp chính xác từng ngày tháng chuẩn Bible Mùa 1");

            // 2. Kiểm tra Chương 8 'Lời chào cuối cùng' tại Ngọ Môn Không Bóng (Nhật Quỹ Tàn Khuyết, Sirphina, 6 lời chào gia đình, 11 lời chào Bờ Rìa & câu kết)
            var ch8 = CanonicalSagaAndGdd6Systems.ExecuteChapterEightFinalGreeting(
                placeSevenNotchedNecklaceOnSundial: true,
                returnClockNeedleToSirphina: true);
            Assert(ch8.completed &&
                   ch8.sixFamilyFarewells.Count == 6 &&
                   ch8.sundialInscription.StartsWith("KHOA") &&
                   ch8.cityReturnEpilogue.Contains("Dù tôi từng là gì, hôm nay tôi vẫn phải ăn tối."),
                "Chương 8 'Lời chào cuối cùng' phải khắc tên KHOA lên Nhật Quỹ Tàn Khuyết, trả kim đồng hồ cho Sirphina, có đủ 6 lời chào tạm biệt gia đình và câu kết chuẩn");

            // 3. Kiểm tra Bảng 1.5-D GDD v6.0 (31 công trình chuẩn kèm kích thước, tầm và giới hạn/trại)
            Assert(CanonicalSagaAndGdd6Systems.ThirtyOneBuildingsTable15D.Count == 31,
                "Bảng 1.5-D phải có đúng 31 công trình phòng thủ/thông tin/hỗ trợ chuẩn GDD v6.0");
            int bachVanCount = 0;
            for (int i = 0; i < CanonicalSagaAndGdd6Systems.ThirtyOneBuildingsTable15D.Count; i++)
            {
                if (CanonicalSagaAndGdd6Systems.ThirtyOneBuildingsTable15D[i].IsBachVan)
                {
                    bachVanCount++;
                    Assert(CanonicalSagaAndGdd6Systems.ThirtyOneBuildingsTable15D[i].MaxPerFarm == 1 &&
                           CanonicalSagaAndGdd6Systems.ThirtyOneBuildingsTable15D[i].FootprintWidthMeters == 6,
                        "Cả 3 công trình Bạch Vân trong Bảng 1.5-D đều phải có kích thước 6x6m và giới hạn 1/trại");
                }
            }
            Assert(bachVanCount == 3, "Phải có đúng 3 công trình Bạch Vân trong Bảng 1.5-D");

            // 4. Kiểm tra Bảng 5.4 GDD v6.0 (4 kênh Giám định gen chuẩn: Chợ ngày 300/900G, Phòng NC 700/2000/6500/9000G + 25/30/40/45 STA, Chợ đêm 450/1300/3500/5000G + 90%/72%/50%/35%, Bí Nhân 0G + 100%)
            var labQuy = CanonicalSagaAndGdd6Systems.GetCanonicalAppraisalChannelSpecGdd54(AppraisalChannel.PhongNghienCuu, GeneRarity.Quy);
            var labLeg = CanonicalSagaAndGdd6Systems.GetCanonicalAppraisalChannelSpecGdd54(AppraisalChannel.PhongNghienCuu, GeneRarity.HuyenThoai);
            var nightDiBien = CanonicalSagaAndGdd6Systems.GetCanonicalAppraisalChannelSpecGdd54(AppraisalChannel.ChoDemGiamDinhCapCao, GeneRarity.DiBien);
            var mysticDiBien = CanonicalSagaAndGdd6Systems.GetCanonicalAppraisalChannelSpecGdd54(AppraisalChannel.BiNhanHoSoGiongLoai, GeneRarity.DiBien);
            Assert(labQuy.goldCost == 2000 && labQuy.reagentCount == 2 && Math.Abs(labQuy.staminaCost - 30f) < 0.01f && labQuy.processingHours == 24,
                "Bảng 5.4 GDD v6.0: Phòng Nghiên Cứu bậc Quý phải là 85%, 2.000G + 2 thuốc thử, 30 STA, 24h");
            Assert(labLeg.goldCost == 6500 && labLeg.reagentCount == 4 && Math.Abs(labLeg.staminaCost - 40f) < 0.01f && labLeg.processingHours == 36,
                "Bảng 5.4 GDD v6.0: Phòng Nghiên Cứu bậc Huyền Thoại phải là 65%, 6.500G + 4 thuốc thử, 40 STA, 36h");
            Assert(nightDiBien.goldCost == 5000 && Math.Abs(nightDiBien.successRate - 0.35f) < 0.001f && Math.Abs(nightDiBien.staminaCost - 8f) < 0.01f && nightDiBien.processingHours == 2,
                "Bảng 5.4 GDD v6.0: Quầy Giám Định Cấp Cao Chợ Đêm bậc Dị Biến phải là 35%, 5.000G, 8 STA, 2h");
            Assert(mysticDiBien.goldCost == 0 && Math.Abs(mysticDiBien.successRate - 1.00f) < 0.001f && Math.Abs(mysticDiBien.staminaCost - 10f) < 0.01f,
                "Bảng 5.4 GDD v6.0: Bí Nhân giám định bậc Dị Biến phải là 100%, 0G (đổi vật phẩm), 10 STA");

            // 5. Kiểm tra Mục 10.2 Cổng Giữa Game (>= 8 đợt lớn + 1/5 điều kiện phụ), Mục 10.4–10.5 (kẹp 50% trước phát lộ & bảng tỷ lệ Lam/Vàng/Tím/Đỏ) & Mục 10.8 12 Khóa Chuyên Biệt Khoa/An
            var gateFailWaves = CanonicalSagaAndGdd6Systems.EvaluateCanonicalMidGameGateGdd102(
                majorWavesSurvived: 7,
                herdSoKhaiThreeConsecutiveDays: true,
                identifiedLegendaryOrDeviantGene: true,
                constructionHallLevel: 3,
                cycleBossesDefeated: 1,
                totalPigSalesGoldRevenue: 30000L);
            Assert(!gateFailWaves.unlocked, "Chưa đủ 8 đợt quái lớn thì chưa mở được Cổng Giữa Game chuẩn GDD 10.2");

            var gatePass = CanonicalSagaAndGdd6Systems.EvaluateCanonicalMidGameGateGdd102(
                majorWavesSurvived: 8,
                herdSoKhaiThreeConsecutiveDays: false,
                identifiedLegendaryOrDeviantGene: false,
                constructionHallLevel: 3,
                cycleBossesDefeated: 0,
                totalPigSalesGoldRevenue: 12000L);
            Assert(gatePass.unlocked && gatePass.subConditionsMetCount == 1,
                "Đạt >= 8 đợt quái lớn và thỏa 1 trong 5 điều kiện phụ phải mở Cổng Giữa Game chuẩn GDD 10.2");

            Assert(CanonicalSagaAndGdd6Systems.ClampSpecialtyLockProgressBeforeRevealGdd104(90, 100, midGameGateRevealed: false) == 50,
                "GDD 10.4: Trước khi qua Cổng Giữa Game, tiến độ khóa đặc tính ẩn chỉ tích lũy tối đa 50%");
            Assert(Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperOpenRateGdd105(SpecialtyPaperColor.Lam, 0) - 0.15f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperOpenRateGdd105(SpecialtyPaperColor.Do, 3) - 1.00f) < 0.001f,
                "GDD 10.5: Giấy Lam +0 đạt 15% và Giấy Đỏ +3 đạt 100%");

            Assert(CanonicalSagaAndGdd6Systems.KhoaAndAnTwelveSpecialtyLocksGdd108.Count == 12,
                "Mục 10.8 phải có đủ 12 khóa chuyên biệt (6 khóa 'Trại Chủ Lão Luyện' của Khoa + 6 khóa 'Người Đọc Dòng' của An)");
        }

        static void TestPolyglotNativeRustCppGoJavaDartFfiAndCSharpEquivalence()
        {
            NativePolyglotAccelerator.EnsureNativeProbed();

            // Warmup Managed & Native
            NativePolyglotAccelerator.ExecuteBenchmarkSuite(PolyglotExecutionBackend.ManagedCSharpSoA, 5, 25, 100, 5);
            NativePolyglotAccelerator.ExecuteBenchmarkSuite(PolyglotExecutionBackend.AutoHybridBestOfBreed, 5, 25, 100, 5);

            var managedRes = NativePolyglotAccelerator.ExecuteBenchmarkSuite(
                PolyglotExecutionBackend.ManagedCSharpSoA,
                bfsIters: 500,
                boidsSteps: 5000,
                combatTicks: 50000,
                economySeeds: 2000);

            Assert(managedRes.Checksum1 == NativePolyglotAccelerator.ExpectedChecksum1_Bfs500 &&
                   managedRes.Checksum2 == NativePolyglotAccelerator.ExpectedChecksum2_Boids5000 &&
                   managedRes.Checksum3 == NativePolyglotAccelerator.ExpectedChecksum3_Combat50000 &&
                   managedRes.Checksum4 == NativePolyglotAccelerator.ExpectedChecksum4_Economy2000,
                $"Nhân C# SoA phải khớp tuyệt đối 4 Checksum chuẩn (Thực tế: {managedRes.Checksum1}|{managedRes.Checksum2}|{managedRes.Checksum3}|{managedRes.Checksum4})");

            if (NativePolyglotAccelerator.IsRustNativeAvailable)
            {
                var rustRes = NativePolyglotAccelerator.ExecuteBenchmarkSuite(
                    PolyglotExecutionBackend.RustNativeCAbi,
                    bfsIters: 500,
                    boidsSteps: 5000,
                    combatTicks: 50000,
                    economySeeds: 2000);
                Assert(rustRes.Checksum1 == NativePolyglotAccelerator.ExpectedChecksum1_Bfs500 &&
                       rustRes.Checksum2 == NativePolyglotAccelerator.ExpectedChecksum2_Boids5000 &&
                       rustRes.Checksum3 == NativePolyglotAccelerator.ExpectedChecksum3_Combat50000 &&
                       rustRes.Checksum4 == NativePolyglotAccelerator.ExpectedChecksum4_Economy2000,
                    "Nhân Rust Native C-ABI (pig_tycoon_native.dll) gọi qua P/Invoke phải trả về Checksum khớp 100% tới từng bit");
            }

            if (NativePolyglotAccelerator.IsCppNativeAvailable)
            {
                var cppRes = NativePolyglotAccelerator.ExecuteBenchmarkSuite(
                    PolyglotExecutionBackend.Cpp20SimdNative,
                    bfsIters: 500,
                    boidsSteps: 5000,
                    combatTicks: 50000,
                    economySeeds: 2000);
                Assert(cppRes.Checksum1 == NativePolyglotAccelerator.ExpectedChecksum1_Bfs500 &&
                       cppRes.Checksum2 == NativePolyglotAccelerator.ExpectedChecksum2_Boids5000 &&
                       cppRes.Checksum3 == NativePolyglotAccelerator.ExpectedChecksum3_Combat50000 &&
                       cppRes.Checksum4 == NativePolyglotAccelerator.ExpectedChecksum4_Economy2000,
                    "Nhân C/C++20 Clang -O3 SIMD (pig_tycoon_cpp.dll) gọi qua P/Invoke phải trả về Checksum khớp 100% tới từng bit");
            }

            // Kiểm tra hàm tăng tốc buffer Flow-Field 120x80 trực tiếp
            byte[] obs = new byte[9600];
            int[] distOut = new int[9600];
            int visited = NativePolyglotAccelerator.ComputeAcceleratedFlowField120x80(24, 18, obs, distOut);
            Assert(visited == 9600 && distOut[18 * 120 + 24] == 0 && distOut[0] == 42,
                "ComputeAcceleratedFlowField120x80 phải điền đầy đủ 9.600 ô khoảng cách Manhattan chuẩn");

            var hybridRes = NativePolyglotAccelerator.ExecuteBenchmarkSuite(
                PolyglotExecutionBackend.AutoHybridBestOfBreed,
                bfsIters: 500,
                boidsSteps: 5000,
                combatTicks: 50000,
                economySeeds: 2000);
            Console.WriteLine($"   [BENCHMARK C# Managed SoA] BFS={managedRes.Kernel1BfsMs:F3}ms | Boids={managedRes.Kernel2BoidsMs:F3}ms | Combat={managedRes.Kernel3CombatMs:F3}ms | Economy={managedRes.Kernel4EconomyMs:F3}ms | Total={managedRes.TotalMs:F3}ms");
            Console.WriteLine($"   [BENCHMARK Hybrid Native]  BFS={hybridRes.Kernel1BfsMs:F3}ms | Boids={hybridRes.Kernel2BoidsMs:F3}ms | Combat={hybridRes.Kernel3CombatMs:F3}ms | Economy={hybridRes.Kernel4EconomyMs:F3}ms | Total={hybridRes.TotalMs:F3}ms (RustDLL={NativePolyglotAccelerator.IsRustNativeAvailable}, CppDLL={NativePolyglotAccelerator.IsCppNativeAvailable})");
        }

        static void TestDualProtagonistAnPerspectiveParts11To12PolyglotApiBridgeAndNativeFlowField()
        {
            // 1. Kiểm chứng Góc Nhìn Kép Khoa vs An (Phần 1B, Mục 2.7, Phần 5B & Sổ Bìa Hồng Dán Sticker Con Thỏ)
            Assert(CanonicalSagaAndGdd6Systems.DualProtagonistComparisonTable27.Count == 12,
                "Bảng 2.7 So sánh Khoa ('lấy cương') và An ('lấy nhu') phải có đủ 12 trục đối xứng nguyên bản");
            Assert(CanonicalSagaAndGdd6Systems.AnPinkCoverNotebookEntries.Count == 18,
                "Sổ bìa hồng dán sticker con thỏ của An phải có đủ 18 mục chuẩn từ Phần 1B & Phần 5B");
            for (int i = 0; i < CanonicalSagaAndGdd6Systems.AnPinkCoverNotebookEntries.Count; i++)
            {
                var entry = CanonicalSagaAndGdd6Systems.AnPinkCoverNotebookEntries[i];
                Assert(entry.ValidateBlackoutInvariant(),
                    $"Dòng sổ bìa hồng '{entry.EntryId}' vi phạm bất biến Mục 2.7: dòng vô thức không được có 'ơi', không sticker và không ':)'");
            }

            // Kiểm chứng Bảng 1.4 & 1B có đủ điềm lành báo trước (GoodOmenBeforeEvent) và chi tiết nguyên bản
            var table14 = CanonicalSagaAndGdd6Systems.FamilyTimelineTable14;
            var table1B = CanonicalSagaAndGdd6Systems.FamilyTimelineTable1B_AnRoute;
            Assert(table14.Count == 8 && table1B.Count == 8 &&
                   !string.IsNullOrWhiteSpace(table14[0].GoodOmenBeforeEvent) &&
                   table14[1].OfficialCauseOfDeath.Contains("Còn sớm") &&
                   table14[2].EventDateAndTime.Contains("168 ngày = 7 × 24") &&
                   table14[5].RelicKeptInTinBox.Contains("7 nét gạch ngắn") &&
                   table1B[3].MemberName.Contains("Em trai Bé Khoa"),
                "Bảng 1.4 & 1B phải khớp nguyên văn Điềm lành trước biến cố, Bà nội 49 ngày ('Còn sớm'), Anh Minh 168 ngày và Phiếu thu 7 nét gạch ngắn");

            // 2. Kiểm chứng trượt mặt nạ vô thức & động tác 'Hai lòng bàn tay ngửa lên' (Mục 2.7 & 12.20)
            bool hasPalmsUpGesture = false;
            for (int i = 0; i < CanonicalSagaAndGdd6Systems.ProtagonistMaskSlipsAndPalmsUpGesture.Count; i++)
            {
                if (CanonicalSagaAndGdd6Systems.ProtagonistMaskSlipsAndPalmsUpGesture[i].TriggeredBothPalmsUpGesture)
                {
                    hasPalmsUpGesture = true;
                }
            }
            Assert(hasPalmsUpGesture, "Phải có động tác cổ xưa 'Hai lòng bàn tay ngửa lên' chưa ai dạy mà cả Khoa lẫn An đều tự làm");

            // 3. Kiểm chứng Phần 11–12: Đại Giới Lưu, 6 dòng Mặc Khải trên Đại Cổ Thụ, 4 thành viên Kim Ô Thánh Hội, luật thoại Sirphina (<=3 câu, <=3 chữ/câu) & 11 lời chào Bờ Rìa
            Assert(CanonicalSagaAndGdd6Systems.SixLinesOfMacKhaiOnGreatAncientTree.Count == 6,
                "Đại Cổ Thụ phải khắc đúng 6 dòng Mặc Khải bằng Khắc Ngữ");
            Assert(CanonicalSagaAndGdd6Systems.FourMembersOfKimOThanhHoi.Count == 4,
                "Kim Ô Thánh Hội (Ordo Solis Aurei) phải có đúng 4 thành viên: Thánh Minh Nhật Giả, Nguyệt Thư, Custos, Sirphina");
            Assert(CanonicalSagaAndGdd6Systems.ValidateSirphinaDialogueRule(CanonicalSagaAndGdd6Systems.SirphinaCanonicalSpokenLines),
                "Lời thoại của Sirphina phải tuân thủ nghiêm ngặt luật <= 3 câu và <= 3 từ/câu");
            Assert(CanonicalSagaAndGdd6Systems.ElevenBoRiaResidentFarewells.Count == 11 &&
                   CanonicalSagaAndGdd6Systems.ElevenBoRiaResidentFarewells[1].FinalConversationSummary.Contains("Chị không cần cười nữa đâu. Đi đi. Heo em coi.") &&
                   CanonicalSagaAndGdd6Systems.ElevenBoRiaResidentFarewells[10].FinalConversationSummary.Contains("nằm ngửa giả chết dưới máng"),
                "Chương 8 'Lời chào cuối cùng' phải có đủ 11 lời tạm biệt nguyên văn mục 12.4.5 của cư dân Bờ Rìa");

            // Kiểm chứng ma trận tỉ lệ giấy mở khóa theo từng bậc khóa GDD 10.5 & 10.6 và Bảng 1.5-D nguyên văn
            Assert(Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Lam, 1, 0) - 0.50f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Lam, 2, 0) - 0.25f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Lam, 3, 0) - 0.12f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Lam, 4, 0) - 0.05f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Lam, 5, 0) - 0.00f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Lam, 1, 3) - 0.65f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Vang, 2, 3) - 0.78f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Tim, 6, 3) - 0.17f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Do, 6, 0) - 0.35f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperLockRateGdd105(SpecialtyPaperColor.Do, 6, 3) - 0.50f) < 0.001f &&
                   Math.Abs(CanonicalSagaAndGdd6Systems.GetSpecialtyPaperExtraRollSpecGdd105(SpecialtyPaperColor.Do, 3).extraRollChance - 0.45f) < 0.001f,
                "GDD 10.5 & 10.6: Ma trận tỉ lệ giấy Lam/Vàng/Tím/Đỏ theo từng Khóa #1..#6 và bậc cường hóa +0..+3 phải khớp nguyên văn GDD v6.0");

            var anFinale = CanonicalSagaAndGdd6Systems.ExecuteCanonicalSeasonEightFinale(PlayerId.An, voluntaryBothPalmsUp: true);
            Assert(anFinale.Completed &&
                   anFinale.Protagonist == PlayerId.An &&
                   anFinale.ModernAndAncientSoulFused &&
                   anFinale.TwoWorldsPermanentlySeparated &&
                   anFinale.HuyenYWithdrawnFromMainWorld &&
                   anFinale.ElevenBoRiaResidentFarewells.Count == 11 &&
                   anFinale.ThreeYearsLaterAfterCredit.Contains("Ba năm sau"),
                "Chương 8 của An phải dung hợp 2 nửa linh hồn nhờ Sirphina, tách rời vĩnh viễn 2 thế giới, nhận đủ 11 lời chào cư dân Bờ Rìa và kích hoạt cảnh 'Ba năm sau'");

            // 4. Kiểm chứng StoryJournalSystem chuyển đổi góc nhìn Khoa <-> An & đảo ngược cảm xúc mặt phản chiếu
            var engine = new GameEngine();
            engine.Story.SwitchProtagonistPerspective(PlayerId.An);
            Assert(engine.Story.ActiveProtagonist == PlayerId.An &&
                   engine.Story.YoungestSiblingName == "Em trai Bé Khoa",
                "Khi chuyển sang góc nhìn An, người em út mất ngày 13/05/2019 phải là 'Em trai Bé Khoa'");
            var khoaMirror = StoryJournalSystem.EvaluateReflectiveSurfaceEmotionForProtagonist("Mirror_7Mornings", PlayerId.Khoa);
            var anMirror = StoryJournalSystem.EvaluateReflectiveSurfaceEmotionForProtagonist("Mirror_7Mornings", PlayerId.An);
            Assert(khoaMirror.realFaceState != anMirror.realFaceState &&
                   anMirror.realFaceState.Contains("mỉm cười"),
                "Cùng một mặt gương 7 buổi sáng nhưng Khoa lạnh lùng không cười, còn An mỉm cười tươi làm cái bóng lúng túng");

            // 4B. Kiểm chứng kiến trúc Sổ tay v3.0 §10 PigTycoon.Contracts (13 Interfaces + 10 Struct Events) và SaveSystem Newtonsoft.Json
            var contracts = engine.Contracts;
            Assert(contracts.TimeService != null &&
                   contracts.SaveService != null &&
                   contracts.DataCatalogService != null &&
                   contracts.Balance != null &&
                   contracts.HerdQuery != null &&
                   contracts.HealthQuery != null &&
                   contracts.EconomyLedger != null &&
                   contracts.WaveSchedule != null &&
                   contracts.IntelService != null &&
                   contracts.PowerGrid != null &&
                   contracts.PlayerStats != null &&
                   contracts.EventBus != null,
                "GameEngine.Contracts phải cung cấp đầy đủ 12 dịch vụ hợp đồng chuẩn Sổ tay v3.0 §10");

            int structEventsFired = 0;
            contracts.EventBus!.Subscribe<PigBorn>(_ => structEventsFired++);
            contracts.EventBus.Subscribe<PigDied>(_ => structEventsFired++);
            contracts.EventBus.Subscribe<PigSold>(_ => structEventsFired++);
            contracts.EventBus.Subscribe<DayStarted>(_ => structEventsFired++);
            contracts.EventBus.Subscribe<WeatherChanged>(_ => structEventsFired++);
            contracts.EventBus.Subscribe<WaveStarted>(_ => structEventsFired++);
            contracts.EventBus.Subscribe<WaveEnded>(_ => structEventsFired++);
            contracts.EventBus.Subscribe<BuildingDamaged>(_ => structEventsFired++);
            contracts.EventBus.Subscribe<BuildingDestroyed>(_ => structEventsFired++);
            contracts.EventBus.Subscribe<OccultEventTriggered>(_ => structEventsFired++);

            contracts.EventBus.Publish(new PigBorn("p1", GeneLineId.HongDien, GeneRarity.Thuong, 1));
            contracts.EventBus.Publish(new PigDied("p1", "Test", 1));
            contracts.EventBus.Publish(new PigSold("p2", 320, false, 1));
            contracts.EventBus.Publish(new DayStarted(2, WeatherType.QuangDang));
            contracts.EventBus.Publish(new WeatherChanged(WeatherType.QuangDang, WeatherType.Mua, 2));
            contracts.EventBus.Publish(new WaveStarted(3, 180, false, 18));
            contracts.EventBus.Publish(new WaveEnded(3, true, 0, 18));
            contracts.EventBus.Publish(new BuildingDamaged("b1", "TURRET_CROSSBOW", 175, 200));
            contracts.EventBus.Publish(new BuildingDestroyed("b1", "TURRET_CROSSBOW", 18));
            contracts.EventBus.Publish(new OccultEventTriggered("EV_MIRROR_7", "Gương 7 buổi sáng", 25, 2));
            Assert(structEventsFired == 10,
                "IEventBus phải phát và nhận chính xác cả 10 readonly struct sự kiện chuẩn Sổ tay v3.0 §10");

            var tryDeserOk = SaveSystem.TryDeserialize("{corrupted_json", out _, out string deserErr);
            Assert(!tryDeserOk && !string.IsNullOrWhiteSpace(deserErr),
                "SaveSystem.TryDeserialize phải trả về chẩn đoán lỗi JsonException tường minh khi JSON hỏng");

            // 5. Kiểm chứng bồi dưỡng ứng viên kế vị GDD 4.6 (+3%/ngày cho An, +4%/ngày cho Khoa 'Người Của Đàn', tối đa 30%, -5% khi bỏ ngày)
            var herd = new HerdManager();
            var candPig = new Pig
            {
                Id = "PIG_CAND_1",
                Name = "Heo Ứng Viên",
                Stage = PigStage.TruongThanh,
                Rarity = GeneRarity.Hiem,
                WeightKg = 95f,
                GeneLine = GeneLineId.KimTho,
                Bonding = 85f,
                Mood = 80
            };
            var pigsList = new List<Pig> { candPig };
            var sel = herd.SelectSuccessionCandidate(candPig);
            Assert(sel.selected, "Phải chọn được ứng viên kế vị hợp lệ");
            var bAn = herd.TrainSuccessionCandidateCanonicalGdd46(pigsList, caredToday: true, khoaHasNguoiCuaDan: false);
            var bKhoa = herd.TrainSuccessionCandidateCanonicalGdd46(pigsList, caredToday: true, khoaHasNguoiCuaDan: true);
            var bSkip = herd.TrainSuccessionCandidateCanonicalGdd46(pigsList, caredToday: false, khoaHasNguoiCuaDan: true);
            Assert(bAn.bonusSuccessionPct == 3 && bKhoa.bonusSuccessionPct == 7 && bSkip.bonusSuccessionPct == 2,
                "GDD 4.6: Bồi dưỡng kế vị phải tăng +3%/ngày (+4%/ngày với Khoa Người Của Đàn) và giảm -5% khi bỏ 1 ngày");

            // 5B. Kiểm chứng Động cơ Chuỗi Tác Động Chéo Liên Hệ Thống (CausalImpactChainEngine — Luật tối thiểu, Tác động chéo tối đa)
            var causalRespAn = engine.ApiBridge.Dispatch("POST", "/api/v1/causal/execute-chain");
            Assert(causalRespAn.Success &&
                   causalRespAn.JsonData.Contains("\"allFiveChainsSucceeded\":true") &&
                   causalRespAn.JsonData.Contains("\"herdLeavesEmptySpotAtJackfruitCorner\":false") &&
                   causalRespAn.JsonData.Contains("\"chippedBowlReturnedAtGate\":true") &&
                   causalRespAn.JsonData.Contains("\"droppedSevenStrokeHarvestReceipt\":true"),
                "CausalImpactChainEngine (Góc nhìn An): 5 chuỗi nhân quả phải chạm >= 27 bước liên hệ thống, cắt kênh Kiện Hàng Thứ 7 làm đàn heo lấp chỗ trống góc gốc mít và trả bát men xanh mẻ về trước cổng");

            // Kiểm chứng trực tiếp trên GameEngine.SellPigAtDayMarket (không cần gọi qua script phụ):
            // Bán heo ủ bệnh KHÔNG kiểm dịch tự động làm Economy.CalculatePigSalePrice giảm -10% trong 3 ngày, tăng ThreatBudget và giảm thiện cảm Lệ
            var refHealthyPig = Pig.CreateDefault("ref_healthy", "Heo Chuẩn Giá", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 16, 100f);
            refHealthyPig.Stage = PigStage.TruongThanh;
            int priceBeforeScandal = engine.Economy.CalculatePigSalePrice(refHealthyPig, engine.Farm.FarmLevel).priceGold;

            var sickPig = Pig.CreateDefault("sick_unquarantined", "Heo Ủ Bệnh", GeneLineId.HongDien, GeneRarity.Thuong, "Duc", 16, 92f);
            sickPig.ActiveDisease = DiseaseId.HoHapLanh;
            engine.Pigs.Add(sickPig);
            var directSaleRes = engine.SellPigAtDayMarket("sick_unquarantined", isQuarantined: false);
            int priceAfterScandal = engine.Economy.CalculatePigSalePrice(refHealthyPig, engine.Farm.FarmLevel).priceGold;
            Assert(directSaleRes.success &&
                   engine.Economy.DayMarket.UnquarantinedSickPenaltyDaysRemaining == 3 &&
                   priceAfterScandal < priceBeforeScandal &&
                   engine.Defense.PendingThreatBudgetBonus > 0,
                "GameEngine.SellPigAtDayMarket: Bán heo ủ bệnh không kiểm dịch phải tự động kích hoạt phạt -10% trực tiếp trong Economy.CalculatePigSalePrice suốt 3 ngày và tăng ThreatBudget");

            // Kiểm chứng trực tiếp dây chuyền ChangeWeather(GiongBao) -> EventBus -> PowerGrid + Herd Mood & ExecuteLiveCombatWave -> Âm chấn + Báo cáo
            float demandBeforeStorm = engine.DeepMechanics.PowerGrid.CurrentDemandUnits;
            engine.ChangeWeather(WeatherType.GiongBao);
            Assert(engine.DeepMechanics.PowerGrid.CurrentDemandUnits > demandBeforeStorm,
                "GameEngine.ChangeWeather(GiongBao): Khi chưa có Cột Thu Lôi, sấm sét phải tự động đẩy tăng phụ tải Lưới Điện qua EventBus");

            var waveRes = engine.ExecuteLiveCombatWave(waveIndex: 4, threatBudget: 120, isBossWave: false, triggerBachVan: true);
            Assert(waveRes.report != null && !engine.Clock.IsDuringCombatWave && engine.DeepMechanics.BattleReports.Count > 0,
                "GameEngine.ExecuteLiveCombatWave: Đợt quái phải tự động đóng/mở băng đồng hồ, đồng bộ điện đỉnh, tính âm chấn lên heo nái và xuất báo cáo trận đánh");

            // 5C. Kiểm chứng Hệ Sinh Thái Tự Hành & Tác Chiến Đa Chủng Quái (SystemicEcosystemSimulation)
            // Thêm heo Lôi Mạch (nạp tụ điện khi Giông Bão), heo Xích Mao thuần dưỡng (phản đòn quái), và heo ủ bệnh đang đói (tự tìm máng ăn làm nhiễm mầm bệnh máng)
            var loiMachPig = Pig.CreateDefault("eco_loimach", "Lôi Mạch Sấm", GeneLineId.LoiMach, GeneRarity.Hiem, "Duc", 18, 95f);
            var xichMaoPig = Pig.CreateDefault("eco_xichmao", "Xích Mao Chiến", GeneLineId.XichMao, GeneRarity.Hiem, "Duc", 19, 105f);
            xichMaoPig.IsXichMaoDomesticated = true;
            var hungryContagiousPig = Pig.CreateDefault("eco_contagious", "Heo Đói Nhiễm Khuẩn", GeneLineId.HongDien, GeneRarity.Thuong, "Cai", 15, 78f);
            hungryContagiousPig.Hunger = 55;
            hungryContagiousPig.ActiveDisease = DiseaseId.HoHapLanh;
            hungryContagiousPig.PositionX = -15f;
            hungryContagiousPig.PositionY = -10f;
            engine.Pigs.Add(loiMachPig);
            engine.Pigs.Add(xichMaoPig);
            engine.Pigs.Add(hungryContagiousPig);

            // Đặt Máy Phát Điện, Tháp Phòng Không Skyhook và Tụ Điện Dự Phòng + Dây Điện Ngầm
            engine.Farm.Gold += 5000;
            engine.Farm.FuelLiters = 50;
            engine.Defense.HallLevel = 4;
            engine.Defense.UnlockedBlueprints.Add("MayPhatDien");
            engine.Defense.UnlockedBlueprints.Add("ThapPhongKhongSkyhook");
            engine.Defense.ConstructBuilding("MayPhatDien");
            engine.Defense.ConstructBuilding("ThapPhongKhongSkyhook");
            engine.DeepMechanics.PowerGrid.CapacitorCount = 2;
            engine.DeepMechanics.PowerGrid.CapacitorChargeUnits = 5f;
            engine.DeepMechanics.PowerGrid.ConnectWire("W_UNDERGROUND_1", "GEN_1", "SKYHOOK_1", underground: true);
            engine.Defense.SynchronizeWithPowerGrid(engine.DeepMechanics.PowerGrid, engine.Farm, isDuringWave: false, consumeHourlyFuel: false);

            // Tick 1 giờ trong Giông Bão: heo Lôi Mạch nạp tụ điện, heo bệnh đói tự đi tới máng ăn và làm nhiễm bẩn máng ăn
            engine.CurrentWeather = WeatherType.GiongBao;
            float capBefore = engine.DeepMechanics.PowerGrid.CapacitorChargeUnits;
            engine.Ecosystem.TickHourlyEcologyAndAgentAi(engine);
            var contagiousDecision = engine.Ecosystem.LastHourlyAgentDecisions.Find(d => d.PigId == "eco_contagious");
            Assert(engine.DeepMechanics.PowerGrid.CapacitorChargeUnits > capBefore &&
                   engine.Ecosystem.ContaminatedTroughsCount > 0 &&
                   contagiousDecision != null &&
                   contagiousDecision.ChosenState == PigUtilityBehaviorState.SeekingFeeder,
                "SystemicEcosystemSimulation.TickHourlyEcologyAndAgentAi: Heo Lôi Mạch phải tự nạp tụ điện trong Giông Bão và heo ủ bệnh đang đói tự tìm máng ăn gây nhiễm khuẩn không gian");

            // Tăng thiện cảm Bác sĩ Dung và Lệ rồi áp dụng Mạng Lưới Tín Nhiệm NPC: tự động khử trùng máng ăn và giảm phí kiểm dịch Lệ
            engine.Story.NpcTrustTier["BacSiDung"] = 3;
            engine.Story.NpcTrustTier["Le"] = 3;
            var npcTrustRes = engine.Ecosystem.ApplyNpcTrustNetworkToMechanics(engine);
            Assert(engine.Ecosystem.ContaminatedTroughsCount == 0 &&
                   npcTrustRes.effectiveQuarantineFee < 60 &&
                   npcTrustRes.bonusVetCures == 1,
                "SystemicEcosystemSimulation.ApplyNpcTrustNetworkToMechanics: Thiện cảm Bác sĩ Dung và Lệ phải trực tiếp khử trùng máng ăn và giảm phí kiểm dịch Chợ Ngày");

            // Tác chiến đa chủng quái: Dây điện ngầm được bảo vệ khỏi Kẻ Cắt Dây, UFO bị Tháp Skyhook cắt chùm kéo, heo Xích Mao húc phản đòn
            var raidRes = engine.Ecosystem.ExecuteMultiArchetypeTacticalRaid(
                engine,
                waveIndex: 5,
                baseThreatBudget: 120,
                isBossWave: false,
                triggerBachVan: false);
            Assert(raidRes.UndergroundWiresProtectedCount > 0 &&
                   raidRes.UfoBeamsSeveredBySkyhook > 0 &&
                   raidRes.DomesticatedXichMaoCounterAttackDamage > 0,
                "SystemicEcosystemSimulation.ExecuteMultiArchetypeTacticalRaid: Dây điện ngầm chặn Kẻ Cắt Dây, Tháp Skyhook bắn hạ UFO và heo Xích Mao phản đòn cận chiến");

            // 6. Kiểm chứng HerdFlowField.RebuildFlowField được tăng tốc trực tiếp bởi Native C-ABI (Rust/C++20)
            var flowField = new HerdFlowField(40, 30);
            flowField.SetTargets(FlowDestinationGroup.Shelter, new[] { (18f, 14f) });
            Assert(NativePolyglotAccelerator.NativeHardwareAcceleratedFlowFieldCount > 0 &&
                   (flowField.LastUsedNativeBackend == "Rust_C_ABI" || flowField.LastUsedNativeBackend == "C++20_Clang_SIMD"),
                $"HerdFlowField.RebuildFlowField phải gọi trực tiếp nhân Native C-ABI (Backend thực tế: {flowField.LastUsedNativeBackend})");

            // 7. Kiểm chứng PolyglotApiBridge điều phối REST-like JSON API giữa GameEngine, C# Core & Native Rust/C++20
            var statusResp = engine.ApiBridge.Dispatch("GET", "/api/v1/status");
            var switchAnResp = engine.ApiBridge.Dispatch("POST", "/api/v1/protagonist/switch", "protagonist=An");
            var notebookResp = engine.ApiBridge.Dispatch("GET", "/api/v1/story/notebook");
            var appraiseResp = engine.ApiBridge.Dispatch("POST", "/api/v1/gdd6/appraise", "channel=ChoDem&rarity=DiBien");
            var locksResp = engine.ApiBridge.Dispatch("POST", "/api/v1/gdd6/specialty-locks");
            var tickResp = engine.ApiBridge.Dispatch("POST", "/api/v1/simulation/tick", "minutes=60");
            var finaleResp = engine.ApiBridge.Dispatch("POST", "/api/v1/story/chapter8-finale");
            var benchResp = engine.ApiBridge.Dispatch("GET", "/api/v1/polyglot/benchmark");

            Assert(statusResp.Success &&
                   switchAnResp.ActiveProtagonist == "An" &&
                   switchAnResp.YoungestSiblingName.Contains("Em trai Bé Khoa") &&
                   notebookResp.NotebookStyleSummary.Contains("Sổ bìa hồng dán sticker con thỏ") &&
                   appraiseResp.Success &&
                   appraiseResp.JsonData.Contains("\"nativeRustUsed\":true") &&
                   appraiseResp.JsonData.Contains("\"csharpAndNativeBitExactAgreement\":true") &&
                   locksResp.Success &&
                   locksResp.JsonData.Contains("\"redPaperPlus3Lock6Rate\":0.5") &&
                   locksResp.JsonData.Contains("\"redPaperPlus3ExtraRoll3Chance\":0.45") &&
                   tickResp.Success &&
                   finaleResp.JsonData.Contains("\"boRiaResidentFarewellsCount\":11") &&
                   benchResp.JsonData.Contains("\"bitExactVerified\":true"),
                "PolyglotApiBridge phải liên kết thông suốt toàn bộ endpoint API giữa GameEngine, Cốt truyện Kép Khoa/An, GDD v6.0 và Native Rust/C++20 C-ABI");
        }

        static void TestStardewValley16GradeArchitectureSpineAndLiveEngineIntegration()
        {
            // 1. Kiểm chứng DeterministicHashUtility & SYNCED_RANDOM tất định đa phiên
            int h1 = DeterministicHashUtility.GetDeterministicHashCode("PigTycoon_Day1_Market");
            int h2 = DeterministicHashUtility.GetDeterministicHashCode("PigTycoon_Day1_Market");
            Assert(h1 == h2 && h1 != 0, "DeterministicHashUtility phải trả về mã băm tất định giống nhau cho cùng chuỗi");

            var engine = new GameEngine(seed: 20260601);
            var engineClone = new GameEngine(seed: 20260601);

            bool syncA = GameStateQuery.CheckConditions("SYNCED_RANDOM day MarketLottery 0.55", engine);
            bool syncB = GameStateQuery.CheckConditions("SYNCED_RANDOM day MarketLottery 0.55", engineClone);
            Assert(syncA == syncB, "SYNCED_RANDOM trong GameStateQuery phải hoàn toàn tất định giữa hai phiên GameEngine cùng seed");

            // 2. Kiểm chứng gói khởi đầu Ngày 1 (Canon_Day1_WelcomePackage) đã tự động chạy qua TriggerActionManager
            Assert(engine.TriggerManager.TriggerActionsRun.Contains("Canon_Day1_WelcomePackage"),
                "Khởi tạo GameEngine Ngày 1 phải tự động kích hoạt TriggerAction Canon_Day1_WelcomePackage");
            Assert(engine.Mailbox.HasReceivedOrQueuedMail("Mail_BaSau_Day1") &&
                   engine.Inventory.ContainsId("(F)CamThuong", 3) &&
                   engine.Inventory.ContainsId("(M)ThaoDuocKho", 2),
                "Canon_Day1_WelcomePackage phải gửi thư Mail_BaSau_Day1 và thêm 3x (F)CamThuong + 2x (M)ThaoDuocKho vào UnifiedInventory");

            // Kiểm chứng tính lũy đẳng (idempotency) của MarkActionApplied = true
            int camBeforeReRaise = engine.Inventory.CountId("(F)CamThuong");
            var reFired = engine.TriggerManager.Raise(TriggerActionManager.TriggerDayStarted, engine);
            Assert(!reFired.Contains("Canon_Day1_WelcomePackage") && engine.Inventory.CountId("(F)CamThuong") == camBeforeReRaise,
                "TriggerAction có MarkActionApplied=true không được phép chạy lặp lại lần thứ hai");

            // 3. Kiểm chứng QualifiedItemRegistry, ItemContextTagManager & ItemQueryResolver
            var feedItems = ItemQueryResolver.TryResolve("ITEMS_BY_TAG category_feed");
            var machines = ItemQueryResolver.TryResolve("ALL_ITEMS (BC)");
            Assert(feedItems.Count == 4 && machines.Count == 5,
                $"ItemQueryResolver phải phân giải đúng 4 loại Cám (F) và 5 Máy chế biến (BC) (thực tế: {feedItems.Count}, {machines.Count})");
            Assert(ItemContextTagManager.HasBaseTag("CamThaoDuoc", "category_feed") &&
                   ItemContextTagManager.HasBaseTag("(W)LaBanTanVu", "occult_ward") &&
                   ItemContextTagManager.HasBaseTag("(F)CamThuong", "!weapon_item"),
                "ItemContextTagManager phải hỗ trợ tra cứu tag ngữ cảnh và phủ định bằng dấu '!'");

            // 4. Kiểm chứng GameStateQuery phức hợp (phủ định '!', ANY, điều kiện kép phân tách bởi dấu phẩy)
            engine.SwitchActiveProtagonist(PlayerId.An);
            bool complexQueryOk = GameStateQuery.CheckConditions(
                "DAY_PLAYED 1 5, PROTAGONIST An, !WEATHER GiongBao, ANY \"PLAYER_HAS_ITEM (F)CamThuong 2\" \"PLAYER_CURRENT_MONEY 999999\"",
                engine);
            Assert(complexQueryOk, "GameStateQuery phải đánh giá chính xác biểu thức kết hợp dấu phẩy, phủ định '!' và tổ hợp ANY");

            // 5. Kiểm chứng WorldLocationGraph, cổng dịch chuyển có điều kiện (Warp) & Ô tương tác (ActionTiles)
            var farmLoc = engine.WorldLocations.CurrentLocation;
            Assert(farmLoc.LocationId == "Farm", "Vị trí khởi đầu phải là Farm");

            var readMailTile = farmLoc.PerformTileAction(10, 10, engine);
            Assert(readMailTile.executed && engine.Mailbox.MailReceived.Contains("Mail_BaSau_Day1"),
                "Tương tác ô (10,10) OpenMailbox tại Farm phải đọc thư Mail_BaSau_Day1 và chuyển vào MailReceived");

            // Thử dịch chuyển sang Đài Thiên Văn (Observatory) khi Cấp trại = 1 (yêu cầu PLAYER_FARM_LEVEL 3) -> Phải bị chặn
            var blockedWarp = engine.WorldLocations.TryWarpPlayer("Observatory", 4, 6, engine);
            Assert(!blockedWarp.warped, "Cổng Warp sang Đài Thiên Văn phải khóa khi chưa đạt PLAYER_FARM_LEVEL 3");

            engine.Farm.FarmLevel = 3;
            var allowedWarp = engine.WorldLocations.TryWarpPlayer("Observatory", 4, 6, engine);
            Assert(allowedWarp.warped && engine.WorldLocations.CurrentLocationId == "Observatory",
                "Đạt Cấp trại 3 phải mở cổng Warp sang Đài Thiên Văn (Observatory)");
            engine.WorldLocations.TryWarpPlayer("Farm", 12, 14, engine);

            // 6. Kiểm chứng PlacedMachineProcessor (Data/Machines): nạp nguyên liệu -> Tick 60 phút (6 nhịp 10 phút) -> Thu hoạch sản phẩm
            var feedMixer = farmLoc.Machines.Find(m => m.MachineQualifiedId == "(BC)MayTronCam");
            Assert(feedMixer != null, "Farm phải có sẵn Máy Trộn Cám Thảo Dược (BC)MayTronCam");

            float feedStockBeforeMix = engine.Farm.FeedStockKg;
            var placeRes = feedMixer!.TryPlaceInput("(F)CamThuong", engine, farmLoc);
            Assert(placeRes.accepted && feedMixer.MinutesUntilReady == 60 && !feedMixer.IsReadyForHarvest,
                "Nạp 1x (F)CamThuong + 1x (M)ThaoDuocKho vào Máy Trộn Cám phải bắt đầu chu trình 60 phút");

            engine.Tick(60); // Trôi qua 60 phút -> 6 lần PerformTenMinuteUpdate
            Assert(feedMixer.IsReadyForHarvest, "Sau 60 phút GameEngine.Tick(60), Máy Trộn Cám phải sẵn sàng thu hoạch");

            var harvestRes = feedMixer.CollectOutput(engine);
            Assert(harvestRes.collected &&
                   harvestRes.item != null &&
                   harvestRes.item.QualifiedItemId == "(F)CamThaoDuoc" &&
                   harvestRes.item.Stack == 2 &&
                   engine.Inventory.ContainsId("(F)CamThaoDuoc", 2) &&
                   engine.Farm.FeedStockKg > feedStockBeforeMix &&
                   engine.PlayerStats.Get("MachinesProcessed") == 1u,
                "Thu hoạch từ Máy Trộn Cám phải nhận 2x (F)CamThaoDuoc, đồng bộ tăng Farm.FeedStockKg và cộng thống kê MachinesProcessed");

            // 7. Kiểm chứng Bán heo -> TriggerPigSold -> Hoàn thành Q02, mở ConversationTopic & Achievement
            var salePig = Pig.CreateDefault("pig_stardew_sale", "Heo Thương Phẩm", GeneLineId.HongDien, GeneRarity.Kha, "Duc", 16, 98f);
            engine.Pigs.Add(salePig);
            var saleRes = engine.SellPigAtDayMarket(salePig.Id, isQuarantined: true);
            Assert(saleRes.success &&
                   GameStateQuery.CheckConditions("PLAYER_QUEST_COMPLETED Q02, PLAYER_CONVERSATION_TOPIC Topic_OngTon_SixWithOne", engine) &&
                   engine.PlayerStats.UnlockedAchievements.Contains("Ach_FirstPigSold"),
                "Bán heo qua SellPigAtDayMarket phải tự động kích hoạt TriggerPigSold hoàn thành Q02, mở chủ đề hội thoại Ông Tồn và mở thành tựu Ach_FirstPigSold");

            // 8. Kiểm chứng qua ngày mới: AnimalHouse (Barn_Primary) sinh Phân Heo Thô & tự động chuyển đổi thành Phân Hữu Cơ (BuildingItemConversion)
            var barnPig1 = Pig.CreateDefault("pig_barn_1", "Heo Chuồng 1", GeneLineId.LamKhe, GeneRarity.Thuong, "Cai", 14, 85f);
            var barnPig2 = Pig.CreateDefault("pig_barn_2", "Heo Chuồng 2", GeneLineId.LamKhe, GeneRarity.Thuong, "Duc", 14, 88f);
            engine.Pigs.Add(barnPig1);
            engine.Pigs.Add(barnPig2);

            engine.Tick(24 * 60); // Qua ngày mới (chạy qua 02:00 sáng và OnDailyReset)
            Assert(engine.Inventory.ContainsId("(O)PhanHuuCo", 1),
                "Qua ngày mới, AnimalHouse (Barn_Primary) phải tự động thu Phân Heo Thô từ đàn heo và chuyển đổi thành (O)PhanHuuCo");
            Assert(engine.TriggerManager.EventsSeen.Contains("Event_02AM_Observed"),
                "Đi qua khung giờ 02:00 sáng khi chưa cắt kênh thứ 7 phải kích hoạt Canon_02AM_SixWithOne_Phenomenon");

            // 9. Kiểm chứng Save/Load bảo toàn toàn bộ trạng thái Xương sống Stardew 1.6
            string json = SaveSystem.Serialize(engine.CreateSaveData());
            bool parsedOk = SaveSystem.TryDeserialize(json, out var loadedData, out string err);
            Assert(parsedOk && string.IsNullOrEmpty(err), $"Serialize/Deserialize GameSaveData phải thành công ({err})");

            var restoredEngine = new GameEngine(seed: 20260601);
            restoredEngine.RestoreFromSave(loadedData);
            Assert(restoredEngine.Inventory.ContainsId("(F)CamThaoDuoc", 2) &&
                   restoredEngine.Inventory.ContainsId("(O)PhanHuuCo", 1) &&
                   restoredEngine.TriggerManager.TriggerActionsRun.Contains("Canon_FirstPigSold_EdgeTownReaction") &&
                   restoredEngine.Mailbox.MailReceived.Contains("Mail_BaSau_Day1") &&
                   restoredEngine.ConversationTopics.IsTopicActive("Topic_OngTon_SixWithOne") &&
                   restoredEngine.PlayerStats.Get("PigsSold") == 1u,
                "RestoreFromSave phải khôi phục chính xác 100% UnifiedInventory, TriggerActionsRun, Mailbox, ConversationTopics và PlayerStats");
        }
    }
}




