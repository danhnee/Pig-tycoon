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
    }
}
