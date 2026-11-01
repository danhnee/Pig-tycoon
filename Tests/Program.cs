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
            RunTest("GameEngine: bed sleep advances the day and restores stamina", TestGameEngineSleep);
            RunTest("Weather: daily roll, weighted distribution and cycle 6 unlock", TestWeatherSystem);
            RunTest("Weather: current weather Save/Load", TestWeatherSaveLoad);
            RunTest("Weather: duration, day transitions and legacy saves", TestWeatherDurationLifecycle);
            RunTest("Farm: Capacity bottleneck & soft density interpolation", TestFarmCapacity);
            RunTest("Farm: cumulative EXP levels 1-20 and tier unlocks", TestFarmProgression);
            RunTest("Save/Load: legacy farm level synchronization from cumulative EXP", TestLegacyFarmProgressionSave);
            RunTest("Pig: 4 stages, Hư Thể seal & Xích Mao domestication", TestPigLifecycle);
            RunTest("HerdManager: 7 conditions & 3 consecutive days for Sơ Khai", TestHerdManager);
            RunTest("Economy: Official pricing formula matching GDD v6.0 section 4.5", TestEconomyPricing);
            RunTest("Economy: Night market 18% gold slots & Bí nhân 0% gold", TestNightMarketAndMystic);
            RunTest("Defense: Building Hall requirement & Bạch Vân manual trigger", TestDefense);
            RunTest("Grid & Modular Fence: 16-way neighbor bitmask & auto-connection logic", TestFenceGridAutoConnect);
            RunTest("Inventory: quantity updates and safe consumption", TestInventoryManager);
            RunTest("Inventory: stacking, transfer, movement and full capacity", TestInventorySlotsAndCapacity);
            RunTest("Save/Load: Khôi phục thời gian", TestSaveLoadClock);
            RunTest("Save/Load: Khôi phục vàng và cấp độ nông trại", TestSaveLoadFarm);
            RunTest("Save/Load: Khôi phục danh sách heo", TestSaveLoadPigs);
            RunTest("Save/Load: Khôi phục phòng thủ", TestSaveLoadDefense);
            RunTest("Save/Load: Khôi phục bản vẽ", TestSaveLoadBlueprints);
            RunTest("Save/Load: Khôi phục hạ tầng", TestSaveLoadInfrastructure);
            RunTest("Save/Load: Khôi phục nhân vật", TestSaveLoadCharacter);
            RunTest("Save/Load JSON: Round-trip nhân vật, hạ tầng và phòng thủ", TestJsonRoundTripCharacterInfrastructureAndDefense);
            RunTest("Save/Load JSON: Nhân vật và dữ liệu heo thiếu/null", TestJsonNullAndMissingCharacterAndPigData);
            RunTest("Save/Load: Tương thích inventory trong save cũ", TestLegacyInventorySave);
            RunTest("Save/Load JSON: Công trình thiếu/null", TestJsonNullAndMissingBuildingData);
            RunTest("Save/Load: Lưu dữ liệu xuống file JSON", TestSaveToFile);
            RunTest("Save/Load: Đọc file và khôi phục dữ liệu", TestLoadFromFile);
            RunTest("Save/Load: File không tồn tại", TestLoadMissingFile);
            RunTest("Save/Load: File JSON bị hỏng", TestLoadInvalidJson);
            RunTest("Save/Load: tương thích v1-v4 và bảo vệ save không hợp lệ", TestSaveCompatibilityAndProtection);

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

        static void TestSaveCompatibilityAndProtection()
        {
            for (int version = 1; version <= GameSaveData.CurrentSaveVersion; version++)
            {
                Assert(GameSaveCompatibility.IsSupportedVersion(version),
                    $"Save v{version} phải được hỗ trợ");
                Assert(GameSaveCompatibility.Classify(true, new GameSaveData { SaveVersion = version }) == SaveFileStatus.Loaded,
                    $"Save v{version} phải được phân loại là có thể nạp");
            }

            Assert(!GameSaveCompatibility.IsSupportedVersion(0), "Save version 0 phải không hợp lệ");
            Assert(!GameSaveCompatibility.IsSupportedVersion(5), "Save version mới hơn hiện tại phải không được hỗ trợ");
            Assert(GameSaveCompatibility.Classify(false, null!) == SaveFileStatus.Missing,
                "Không có file save phải được phân loại riêng là Missing");
            Assert(GameSaveCompatibility.Classify(true, null!) == SaveFileStatus.Invalid,
                "File đã tồn tại nhưng không đọc được phải được phân loại là Invalid");
            Assert(GameSaveCompatibility.Classify(true, new GameSaveData { SaveVersion = 5 }) == SaveFileStatus.UnsupportedVersion,
                "File có version tương lai phải được phân loại là UnsupportedVersion");
            Assert(GameSaveCompatibility.CanWriteSave(SaveFileStatus.Missing) &&
                GameSaveCompatibility.CanWriteSave(SaveFileStatus.Loaded),
                "Chỉ file vắng mặt hoặc save đã nạp hợp lệ mới cho phép ghi save");
            Assert(!GameSaveCompatibility.CanWriteSave(SaveFileStatus.Invalid) &&
                !GameSaveCompatibility.CanWriteSave(SaveFileStatus.UnsupportedVersion),
                "File lỗi hoặc không tương thích phải chặn việc ghi đè");

            var legacyData = System.Text.Json.JsonSerializer.Deserialize<GameSaveData>(
                "{\"SaveVersion\":3}", new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
            Assert(legacyData != null && GameSaveCompatibility.Classify(true, legacyData) == SaveFileStatus.Loaded,
                "JSON save cũ thiếu trường mới vẫn phải được chấp nhận");
            var legacyEngine = new GameEngine();
            legacyEngine.RestoreFromSave(legacyData!);
            Assert(legacyEngine.WeatherDaysRemaining == 1,
                "Save cũ thiếu thời hạn thời tiết phải dùng mặc định tương thích là một ngày");

            var versionFour = new GameEngine().CreateSaveData();
            Assert(versionFour.SaveVersion == 4 &&
                GameSaveCompatibility.Classify(true, versionFour) == SaveFileStatus.Loaded,
                "Dữ liệu save v4 mới phải được chấp nhận");
            string path = GetUniqueJsonTestPath();
            try
            {
                SaveSystem.Save(versionFour, path);
                var loadedV4 = SaveSystem.Load(path);
                Assert(loadedV4.SaveVersion == 4 &&
                    GameSaveCompatibility.Classify(true, loadedV4) == SaveFileStatus.Loaded,
                    "JSON v4 đã ghi file phải đọc lại và được chấp nhận");
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
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

        static void TestGameEngineSleep()
        {
            var engine = new GameEngine(() => 0.1d, () => 0d);
            engine.Clock.RestoreFromSave(1, 20, 30, 1, 7, 1, 0, 12, 0, false);
            engine.Character.Stamina.CurrentStamina = 30f;
            engine.Character.Stamina.DailyLoad = 25f;

            var result = engine.PerformStandardSleep();

            Assert(result.success, "Ngủ tại giường phải thành công khi không giao tranh/cooldown");
            Assert(engine.Clock.CurrentDay == 2 && engine.Clock.CurrentHour == 6 && engine.Clock.CurrentMinute == 0,
                "Ngủ phải đi qua luồng GameEngine và thức dậy lúc 06:00 ngày kế tiếp");
            Assert(engine.Character.Stamina.CurrentStamina == engine.Character.Stamina.MaxStamina,
                "Ngủ phải hồi thể lực theo hành vi reset ngày hiện có");
            Assert(engine.Character.Stamina.DailyLoad == 0f,
                "Ngủ phải xóa Daily Load theo quy tắc ngủ chuẩn");
            Assert(engine.Clock.SleepCooldownHoursRemaining == 6,
                "Ngủ phải giữ cooldown 6 giờ hiện có");

            engine.Clock.RestoreFromSave(2, 20, 0, 1, 7, 1, 0, 12, 0, true);
            var blocked = engine.PerformStandardSleep();
            Assert(!blocked.success && engine.Clock.CurrentDay == 2 && engine.Clock.CurrentHour == 20,
                "Không được ngủ khi đang trong giao tranh");
        }

        static void TestWeatherSystem()
        {
            var random = new Random(61420);
            var durationRandom = new Random(61421);
            var engine = new GameEngine(() => random.NextDouble(), () => durationRandom.NextDouble());
            engine.Clock.RestoreFromSave(1, 6, 0, 6, 8, 1, 0, 24, 0, false);

            var counts = new Dictionary<WeatherType, int>();
            const int sampleCount = 2000;
            for (int i = 0; i < sampleCount; i++)
            {
                engine.Tick(1080);
                counts.TryGetValue(engine.CurrentWeather, out int count);
                counts[engine.CurrentWeather] = count + 1;
            }

            WeatherType[] weatherTypes =
            {
                WeatherType.QuangDang, WeatherType.NhieuMay, WeatherType.NangGat,
                WeatherType.Mua, WeatherType.GiongBao, WeatherType.SuongMu,
                WeatherType.RetDam, WeatherType.NomAm, WeatherType.GioKho,
                WeatherType.MuaBucXa
            };
            double[] expectedPercentages = { 25, 20, 12, 15, 6, 8, 5, 5, 3, 1 };
            for (int i = 0; i < weatherTypes.Length; i++)
            {
                counts.TryGetValue(weatherTypes[i], out int count);
                double actualPercentage = count * 100d / sampleCount;
                Assert(Math.Abs(actualPercentage - expectedPercentages[i]) < (i == 9 ? 0.6d : 2.5d),
                    $"Tỷ lệ {weatherTypes[i]} phải xấp xỉ thiết kế, thực tế {actualPercentage:0.00}%");
            }

            var beforeUnlock = new GameEngine(() => 0.999999d, () => 0d);
            beforeUnlock.Clock.RestoreFromSave(1, 6, 0, 5, 8, 1, 0, 24, 0, false);
            beforeUnlock.Tick(1080);
            Assert(beforeUnlock.CurrentWeather == WeatherType.GioKho, "Mưa Bức Xạ không được xuất hiện trước chu kỳ 6; các tỷ lệ còn lại được chuẩn hóa");

            var unlocked = new GameEngine(() => 0.999999d, () => 0d);
            unlocked.Clock.RestoreFromSave(1, 6, 0, 6, 8, 1, 0, 24, 0, false);
            unlocked.Tick(1080);
            Assert(unlocked.CurrentWeather == WeatherType.MuaBucXa, "Mưa Bức Xạ phải có thể xuất hiện từ chu kỳ 6");

            double[] unlockedBoundaries = { 0.25d, 0.45d, 0.57d, 0.72d, 0.78d, 0.86d, 0.91d, 0.96d, 0.99d };
            WeatherType[] nextWeatherTypes =
            {
                WeatherType.NhieuMay, WeatherType.NangGat, WeatherType.Mua,
                WeatherType.GiongBao, WeatherType.SuongMu, WeatherType.RetDam,
                WeatherType.NomAm, WeatherType.GioKho, WeatherType.MuaBucXa
            };
            for (int i = 0; i < unlockedBoundaries.Length; i++)
                Assert(GetWeatherAtRoll(unlockedBoundaries[i], 6) == nextWeatherTypes[i],
                    $"Ranh giới tỷ lệ chu kỳ 6 tại {unlockedBoundaries[i]:P0} phải chọn loại thời tiết kế tiếp");

            double[] lockedBoundaries = { 25d / 99d, 45d / 99d, 57d / 99d, 72d / 99d, 78d / 99d, 86d / 99d, 91d / 99d, 96d / 99d };
            WeatherType[] lockedNextWeatherTypes =
            {
                WeatherType.NhieuMay, WeatherType.NangGat, WeatherType.Mua,
                WeatherType.GiongBao, WeatherType.SuongMu, WeatherType.RetDam,
                WeatherType.NomAm, WeatherType.GioKho
            };
            for (int i = 0; i < lockedBoundaries.Length; i++)
                Assert(GetWeatherAtRoll(lockedBoundaries[i], 5) == lockedNextWeatherTypes[i],
                    $"Ranh giới tỷ lệ đã chuẩn hóa trước chu kỳ 6 tại {lockedBoundaries[i]:P2} phải chọn loại kế tiếp");

            int weatherRollIndex = 0;
            var dayTransition = new GameEngine(() => weatherRollIndex++ == 0 ? 0.1d : 0.26d, () => 0d);
            WeatherType initialWeather = dayTransition.CurrentWeather;
            dayTransition.Tick(1080);
            Assert(dayTransition.CurrentWeather == WeatherType.NhieuMay && dayTransition.CurrentWeather != initialWeather,
                "Thời tiết phải được chọn lại khi ngày mới bắt đầu");
        }

        static WeatherType GetWeatherAtRoll(double roll, int cycleNumber)
        {
            var engine = new GameEngine(() => roll, () => 0d);
            engine.Clock.RestoreFromSave(1, 6, 0, cycleNumber, 8, 1, 0, 24, 0, false);
            engine.Tick(1080);
            return engine.CurrentWeather;
        }

        static void TestWeatherDurationLifecycle()
        {
            (double roll, int days)[] durationCases = { (0d, 1), (0.5d, 2), (0.999999d, 3) };
            foreach (var (roll, days) in durationCases)
            {
                var durationEngine = new GameEngine(() => 0d, () => roll);
                Assert(durationEngine.WeatherDaysRemaining == days,
                    $"Giá trị gieo thời hạn {roll} phải chọn {days} ngày");
            }

            int weatherRollIndex = 0;
            int durationRollIndex = 0;
            double[] weatherRolls = { 0.1d, 0.26d, 0.46d, 0.58d };
            double[] durationRolls = { 0.5d, 0d, 0d, 0d };
            var engine = new GameEngine(
                () => weatherRolls[Math.Min(weatherRollIndex++, weatherRolls.Length - 1)],
                () => durationRolls[Math.Min(durationRollIndex++, durationRolls.Length - 1)]);
            WeatherType firstWeather = engine.CurrentWeather;
            Assert(engine.WeatherDaysRemaining == 2, "Thời tiết khởi đầu phải giữ thời hạn đã gieo là 2 ngày");

            engine.Tick(1080);
            Assert(engine.CurrentWeather == firstWeather && engine.WeatherDaysRemaining == 1 && weatherRollIndex == 1,
                "Ngày mới đầu tiên chỉ giảm thời hạn, không gieo thời tiết khác");

            engine.Tick(1440);
            Assert(engine.CurrentWeather == WeatherType.NhieuMay && engine.WeatherDaysRemaining == 1 && weatherRollIndex == 2,
                "Chỉ khi thời hạn hết mới gieo thời tiết mới và thời hạn mới");

            int multiDayWeatherRolls = 0;
            var multiDayEngine = new GameEngine(() => { multiDayWeatherRolls++; return 0.1d; }, () => 0d);
            multiDayEngine.Tick(2880);
            Assert(multiDayWeatherRolls == 3 && multiDayEngine.WeatherDaysRemaining == 1,
                "Mỗi ngày mới trong một Tick nhiều ngày phải được xử lý đúng một lần");

            var legacyMissingWeather = RestoreEngineFromJson("""
                { "SaveVersion": 3 }
                """);
            Assert(legacyMissingWeather.CurrentWeather == WeatherType.QuangDang &&
                legacyMissingWeather.WeatherDaysRemaining == 1,
                "Save cũ thiếu thời tiết và thời hạn phải dùng Quang Đãng mặc định hợp lệ và 1 ngày");

            var legacyMissingDuration = RestoreEngineFromJson("""
                { "SaveVersion": 3, "CurrentWeather": 8 }
                """);
            Assert(legacyMissingDuration.CurrentWeather == WeatherType.GioKho &&
                legacyMissingDuration.WeatherDaysRemaining == 1,
                "Save cũ phải giữ thời tiết hợp lệ và mặc định thời hạn còn 1 ngày");

            var invalidWeather = RestoreEngineFromJson("""
                { "SaveVersion": 4, "CurrentWeather": 999, "WeatherDaysRemaining": 0 }
                """);
            Assert(invalidWeather.CurrentWeather == WeatherType.QuangDang &&
                invalidWeather.WeatherDaysRemaining == 1,
                "Enum thời tiết không hợp lệ và thời hạn không hợp lệ phải dùng mặc định an toàn");
        }

        static void TestWeatherSaveLoad()
        {
            string path = GetUniqueJsonTestPath();
            try
            {
                var engine = new GameEngine(() => 0.5d, () => 0.5d) { CurrentWeather = WeatherType.RetDam };
                engine.Tick(1080);
                Assert(engine.WeatherDaysRemaining == 1, "Thời hạn phải giảm sau khi bắt đầu ngày mới");
                SaveSystem.Save(engine.CreateSaveData(), path);

                int restoredWeatherRolls = 0;
                var restored = new GameEngine(() => { restoredWeatherRolls++; return 0.3d; }, () => 0d);
                int rollCountBeforeRestore = restoredWeatherRolls;
                restored.RestoreFromSave(SaveSystem.Load(path));
                Assert(restored.CurrentWeather == WeatherType.RetDam && restored.WeatherDaysRemaining == 1,
                    "Thời tiết và số ngày còn lại phải được khôi phục nguyên trạng");
                Assert(restoredWeatherRolls == rollCountBeforeRestore,
                    "Nạp save không được gieo lại thời tiết");

                restored.Tick(1440);
                Assert(restored.CurrentWeather == WeatherType.NhieuMay && restoredWeatherRolls == rollCountBeforeRestore + 1,
                    "Sau khi thời hạn đã lưu hết, ngày mới tiếp theo mới gieo lại thời tiết");
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        static void TestInventoryManager()
        {
            var inventory = new InventoryManager();
            Assert(inventory.GetQuantity("thao_duoc") == 3, "Vật phẩm khởi đầu phải được tạo trong inventory Core");
            Assert(inventory.TryConsume("thao_duoc", 2), "Phải tiêu thụ được số lượng hợp lệ");
            Assert(inventory.GetQuantity("thao_duoc") == 1, "Số lượng phải giảm sau khi tiêu thụ");
            Assert(!inventory.TryConsume("thao_duoc", 2), "Không được tiêu thụ quá số lượng đang có");
            Assert(inventory.GetQuantity("thao_duoc") == 1, "Thao tác thất bại không được đổi số lượng");
            Assert(!inventory.TryConsume("missing_item"), "Vật phẩm không tồn tại không thể tiêu thụ");
        }

        static void TestInventorySlotsAndCapacity()
        {
            var inventory = new InventoryManager();
            float leftover = inventory.AddItem("seed", 250f, 99f);
            Assert(leftover == 0f && inventory.GetQuantity("seed") == 250f, "Thêm vật phẩm phải tự chia thành nhiều stack");
            Assert(inventory.Slots.Exists(slot => slot != null && slot.ItemId == "seed" && slot.Quantity == 99f), "Stack không được vượt giới hạn stack");

            int firstSeedSlot = inventory.Slots.FindIndex(slot => slot != null && slot.ItemId == "seed");
            Assert(inventory.TryMove(firstSeedSlot, 16, 10f), "Chuyển một phần stack sang ô trống phải thành công");
            Assert(inventory.Slots[16] != null && inventory.Slots[16].ItemId == "seed" && inventory.Slots[16].Quantity == 10f, "Ô đích phải nhận đúng số lượng được chuyển");
            Assert(inventory.GetQuantity("seed") == 250f, "Chuyển ô không được làm thay đổi tổng số vật phẩm");

            var mergeInventory = new InventoryManager();
            mergeInventory.Slots[14] = new InventoryStack { ItemId = "wood", Quantity = 20f, MaxStack = 30f };
            mergeInventory.Slots[15] = new InventoryStack { ItemId = "wood", Quantity = 5f, MaxStack = 30f };
            Assert(mergeInventory.TryMove(14, 15), "Stack cùng loại phải gộp được");
            Assert(mergeInventory.Slots[15].Quantity == 25f && mergeInventory.Slots[14] == null, "Gộp stack phải giữ đúng tổng số lượng");
            mergeInventory.Slots[14] = new InventoryStack { ItemId = "stone", Quantity = 2f, MaxStack = 99f };
            Assert(mergeInventory.TryMove(14, 15), "Chuyển toàn bộ vào ô khác loại phải đổi chỗ hai stack");
            Assert(mergeInventory.Slots[14].ItemId == "wood" && mergeInventory.Slots[15].ItemId == "stone", "Đổi chỗ phải giữ nguyên vật phẩm ở hai ô");

            var fullInventory = new InventoryManager();
            for (int i = 0; i < InventoryManager.StorageSlotCount; i++)
                fullInventory.Slots[i] = new InventoryStack { ItemId = $"item_{i}", Quantity = 1f, MaxStack = 1f };
            Assert(fullInventory.AddItem("overflow", 5f, 1f) == 5f, "Kho đầy phải trả lại toàn bộ số lượng chưa thêm được");
            Assert(fullInventory.GetQuantity("overflow") == 0f, "Kho đầy không được thêm vật phẩm âm thầm");
            Assert(!fullInventory.TryRemove("item_0", 2f) && fullInventory.GetQuantity("item_0") == 1f, "Không được trừ vượt quá số lượng hiện có");
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

        static void TestFarmProgression()
        {
            int[] expectedThresholds =
            {
                0, 600, 1200, 3100, 5000, 8667, 12333, 16000, 24000, 32000,
                40000, 53750, 67500, 81250, 95000, 136000, 177000, 218000, 259000, 300000
            };
            for (int level = 1; level <= 20; level++)
                Assert(Farm.GetFarmExpThreshold(level) == expectedThresholds[level - 1],
                    $"Ngưỡng EXP tích lũy cấp {level} phải khớp bảng đã duyệt");

            for (int targetLevel = 2; targetLevel <= 20; targetLevel++)
            {
                var farm = new Farm();
                farm.AddFarmExp(expectedThresholds[targetLevel - 1] - 1);
                Assert(farm.FarmLevel == targetLevel - 1,
                    $"Thiếu 1 EXP so với ngưỡng không được tăng lên cấp {targetLevel}");
                farm.AddFarmExp(1);
                Assert(farm.FarmLevel == targetLevel && farm.FarmExp == expectedThresholds[targetLevel - 1],
                    $"Đạt đúng ngưỡng EXP phải lên cấp {targetLevel} và giữ nguyên EXP tích lũy");
            }

            var multiLevelFarm = new Farm();
            multiLevelFarm.AddFarmExp(90000);
            Assert(multiLevelFarm.FarmLevel == 14 && multiLevelFarm.FarmExp == 90000,
                "Một lần cộng EXP có thể vượt nhiều cấp và giữ EXP dư đến mốc kế tiếp");
            multiLevelFarm.AddFarmExp(5000);
            Assert(multiLevelFarm.FarmLevel == 15 && multiLevelFarm.FarmExp == 95000,
                "EXP dư tích lũy phải giúp đạt mốc cấp tiếp theo");

            var maxLevelFarm = new Farm();
            maxLevelFarm.AddFarmExp(350000);
            maxLevelFarm.AddFarmExp(int.MaxValue);
            Assert(maxLevelFarm.FarmLevel == 20 && maxLevelFarm.FarmExp == int.MaxValue,
                "Cấp trại không vượt 20 và tổng EXP không tràn số nguyên khi đã đạt cấp tối đa");

            (FarmUnlock unlock, int level)[] unlockThresholds =
            {
                (FarmUnlock.BasicHusbandry, 1), (FarmUnlock.DayMarket, 1),
                (FarmUnlock.WoodenFence, 1), (FarmUnlock.ObservationWatchtower, 1), (FarmUnlock.Caltrops, 1),
                (FarmUnlock.NightMarket, 3), (FarmUnlock.SelectiveBreeding, 3),
                (FarmUnlock.SkyPiercerCrossbowTier1, 3), (FarmUnlock.MedicalTent, 3), (FarmUnlock.FirstPet, 3),
                (FarmUnlock.AutomationTier2, 5), (FarmUnlock.ArcTower, 5), (FarmUnlock.AntiAirTower, 5),
                (FarmUnlock.MediumPowerGrid, 5), (FarmUnlock.HerdLeader, 5),
                (FarmUnlock.ResearchRoom, 8), (FarmUnlock.NightAuction, 8),
                (FarmUnlock.MysticResonanceMachine, 8), (FarmUnlock.BuildingUpgradeTier1, 8),
                (FarmUnlock.AutomationTier3, 11), (FarmUnlock.LargeGenerator, 11),
                (FarmUnlock.WhiteCloudBuildings, 12),
                (FarmUnlock.SkyThunderPlatform, 15), (FarmUnlock.AutomationTier4, 15),
                (FarmUnlock.HighLevelIntelligence, 15), (FarmUnlock.FinalBuildingUpgradeBranch, 15)
            };
            var unlocks = new Farm();
            foreach (var (unlock, level) in unlockThresholds)
            {
                if (level > 1)
                {
                    unlocks.FarmLevel = level - 1;
                    Assert(!unlocks.IsUnlockAvailable(unlock), $"{unlock} chưa mở trước cấp {level}");
                }

                unlocks.FarmLevel = level;
                Assert(unlocks.IsUnlockAvailable(unlock), $"{unlock} phải mở ở cấp {level}");
            }

            var marketEngine = new GameEngine(() => 0.5d);
            marketEngine.Farm.FarmLevel = 2;
            marketEngine.Tick(1080);
            Assert(marketEngine.Economy.NightListings.Count == 0,
                "Chợ đêm phải bị khóa trước cấp 3");
            marketEngine.Farm.FarmLevel = 3;
            marketEngine.Tick(1440);
            Assert(marketEngine.Economy.NightListings.Count == 8,
                "Chợ đêm hiện danh sách khi trại đạt cấp mở khóa 3");
        }

        static void TestLegacyFarmProgressionSave()
        {
            var behindLevel = RestoreEngineFromJson("""
                { "SaveVersion": 3, "FarmLevel": 3, "FarmExp": 95000 }
                """);
            Assert(behindLevel.Farm.FarmLevel == 15 && behindLevel.Farm.FarmExp == 95000,
                "Save cấp 3 với 95.000 EXP phải đồng bộ lên cấp 15 mà không đổi EXP");
            Assert(behindLevel.Farm.IsUnlockAvailable(FarmUnlock.WhiteCloudBuildings),
                "Quyền mở khóa phải phản ánh cấp đã đồng bộ sau khi khôi phục");

            var higherSavedLevel = RestoreEngineFromJson("""
                { "SaveVersion": 3, "FarmLevel": 17, "FarmExp": 95000 }
                """);
            Assert(higherSavedLevel.Farm.FarmLevel == 17,
                "Khôi phục không được hạ cấp save có FarmLevel cao hơn ngưỡng EXP");

            var maxLevel = RestoreEngineFromJson("""
                { "SaveVersion": 3, "FarmLevel": 4, "FarmExp": 2147483647 }
                """);
            Assert(maxLevel.Farm.FarmLevel == 20 && maxLevel.Farm.FarmExp == int.MaxValue,
                "EXP bão hòa phải đồng bộ lên nhưng không vượt cấp 20 hoặc đổi EXP");

            string path = GetUniqueJsonTestPath();
            try
            {
                SaveSystem.Save(maxLevel.CreateSaveData(), path);
                var restoredMaxLevel = new GameEngine();
                restoredMaxLevel.RestoreFromSave(SaveSystem.Load(path));
                Assert(restoredMaxLevel.Farm.FarmLevel == 20 && restoredMaxLevel.Farm.FarmExp == int.MaxValue,
                    "Cấp 20 và EXP bão hòa phải được giữ nguyên qua JSON Save/Load");
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
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
                engine.Character.BaseStats.Str = 31;
                engine.Character.Stamina.CurrentStamina = 73.5f;
                engine.Character.Stamina.MaxStamina = 210f;
                engine.Character.Stamina.DailyLoad = 41.25f;
                engine.Character.Stamina.Condition = 82;
                engine.Character.Stamina.Immunity = 76;
                engine.Character.Stamina.ReboundFatigueStacks = 2;
                engine.Character.EquipItem(new EquipmentItem
                {
                    Id = "json_weapon",
                    Name = "Kiếm kiểm thử",
                    Slot = EquipSlot.Weapon,
                    WeightScore = 2,
                    Defense = 12,
                    BonusStr = 3,
                    SpecialMechanic = "Không rơi"
                });
                engine.Character.BonusStats.Agi = 6;
                engine.Character.ActiveMartialSkills[CombatSlotType.TheCong] = "skill_json_test";
                engine.Inventory.SetQuantity("cam_heo", 14.5f, 20f);
                engine.Inventory.SetQuantity("xo_nuoc", 27.25f, 50f);
                engine.Inventory.SetQuantity("coc_go", 8f);
                engine.Inventory.SetQuantity("mang_an", 4f);
                engine.Inventory.SetQuantity("mang_nuoc", 6f);
                engine.Inventory.SetQuantity("thao_duoc", 7);
                engine.Inventory.SetQuantity("test_material", 12);
                engine.Inventory.BindHotbar(0, "thao_duoc");
                engine.Inventory.SelectHotbarSlot(3);

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
                Assert(restored.Character.BaseStats.Str == 31 && restored.Character.BonusStats.Agi == 6, "Chỉ số gốc và thưởng phải round-trip qua JSON");
                Assert(Math.Abs(restored.Character.Stamina.CurrentStamina - 73.5f) < 0.001f && Math.Abs(restored.Character.Stamina.MaxStamina - 210f) < 0.001f, "Thể lực hiện tại và tối đa phải round-trip qua JSON");
                Assert(Math.Abs(restored.Character.Stamina.DailyLoad - 41.25f) < 0.001f && restored.Character.Stamina.Condition == 82 && restored.Character.Stamina.Immunity == 76 && restored.Character.Stamina.ReboundFatigueStacks == 2, "Trạng thái thể lực phải round-trip qua JSON");
                var restoredWeapon = restored.Character.Equipment[EquipSlot.Weapon];
                Assert(restoredWeapon != null && restoredWeapon.Id == "json_weapon" && restoredWeapon.Name == "Kiếm kiểm thử" && restoredWeapon.Defense == 12 && restoredWeapon.BonusStr == 3 && restoredWeapon.SpecialMechanic == "Không rơi", "Trang bị nhân vật phải round-trip qua JSON");
                Assert(restored.Character.ActiveMartialSkills[CombatSlotType.TheCong] == "skill_json_test", "Kỹ năng võ học đang kích hoạt phải round-trip qua JSON");
                Assert(Math.Abs(restored.Inventory.GetQuantity("cam_heo") - 14.5f) < 0.001f && Math.Abs(restored.Inventory.GetQuantity("xo_nuoc") - 27.25f) < 0.001f, "Cám và nước đang mang phải round-trip qua JSON");
                Assert(restored.Inventory.GetQuantity("coc_go") == 8f && restored.Inventory.GetQuantity("mang_an") == 4f && restored.Inventory.GetQuantity("mang_nuoc") == 6f, "Số lượng hạ tầng mang theo phải round-trip qua JSON");
                Assert(restored.Inventory.GetQuantity("thao_duoc") == 7 && restored.Inventory.GetQuantity("test_material") == 12, "Vật phẩm trong túi phải round-trip qua JSON");
                Assert(restored.Inventory.GetHotbarItemId(0) == "thao_duoc" && restored.Inventory.SelectedHotbarIndex == 3, "Gán vật phẩm và ô hotbar đang chọn phải round-trip qua JSON");
                Assert(restored.Inventory.Slots.Count == InventoryManager.StorageSlotCount && restored.Inventory.Slots[11]?.ItemId == "thao_duoc", "Vị trí các ô inventory phải round-trip qua JSON");

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
            Assert(missingCharacter.Inventory.GetQuantity("coc_go") == 10 && missingCharacter.Inventory.GetQuantity("thao_duoc") == 3, "Save cũ thiếu inventory phải giữ vật phẩm khởi đầu mặc định");

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

        static void TestLegacyInventorySave()
        {
            var versionOneSave = RestoreEngineFromJson("""
                { "SaveVersion": 1, "CharacterId": "legacy_char", "CharacterName": "Save cũ" }
                """);
            Assert(versionOneSave.Character.Id == "legacy_char" && versionOneSave.Inventory.GetQuantity("coc_go") == 10 && versionOneSave.Inventory.GetQuantity("thao_duoc") == 3, "Save v1 thiếu inventory phải nạp với mặc định tương thích");

            var legacySave = RestoreEngineFromJson("""
                {
                  "SaveVersion": 2,
                  "HasInventoryData": true,
                  "Inventory": {
                    "FeedKg": 6.5,
                    "WaterLiters": 12,
                    "CarriedFences": 4,
                    "CarriedFeeders": 3,
                    "CarriedWaterTroughs": 1,
                    "Items": [
                      { "ItemId": "thao_duoc", "Quantity": 8 },
                      { "ItemId": "khu_trung", "Quantity": 2 }
                    ]
                  }
                }
                """);

            Assert(Math.Abs(legacySave.Inventory.GetQuantity("cam_heo") - 6.5f) < 0.001f && legacySave.Inventory.GetQuantity("xo_nuoc") == 12f, "Save v2 cũ phải khôi phục lượng cám/nước");
            Assert(legacySave.Inventory.GetQuantity("coc_go") == 4f && legacySave.Inventory.GetQuantity("mang_an") == 3f && legacySave.Inventory.GetQuantity("mang_nuoc") == 1f, $"Save v2 cũ phải khôi phục vật liệu mang theo: {legacySave.Inventory.GetQuantity("coc_go")}, {legacySave.Inventory.GetQuantity("mang_an")}, {legacySave.Inventory.GetQuantity("mang_nuoc")}");
            Assert(legacySave.Inventory.GetQuantity("thao_duoc") == 8f && legacySave.Inventory.GetQuantity("khu_trung") == 2f, "Các stack trong save v2 cũ phải được chuyển sang các ô inventory mới");
            Assert(legacySave.Inventory.GetQuantity("bua_go") == 1f && legacySave.Inventory.GetHotbarItemId(0) == "cam_heo", "Vật phẩm công cụ và liên kết hotbar mặc định phải được giữ khi nâng save cũ");
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
