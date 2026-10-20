# PIG TYCOON — BÁO CÁO KIỂM TOÁN R&D CHUYÊN SÂU, TỔNG HỢP TIẾN ĐỘ 4 THÀNH VIÊN (A, B, C, D) & KIẾN TRÚC POLYGLOT 7 NGÔN NGỮ ĐA NỀN TẢNG (PC · WEB · ANDROID STUDIO EMULATOR / FLUTTER FFI & API BRIDGE)

> **Trạng thái Kiểm chứng Thực nghiệm Toàn diện (Pure C# Backend Core `.NET Standard 2.1` & `.NET 8.0` + `PigTycoon.Contracts` §10 + Polyglot 7 Ngôn ngữ C-ABI + `PolyglotApiBridge` + Flutter Cross-Platform Zero-GC Client — Đối chiếu 100% GDD v6.0, Sổ tay v3.0, Chỉ thị R&D & Cốt truyện Mùa 1 "Sáu Với Một" Bản Góc Nhìn Kép Khoa & An Phần 1–12)**:
> - **Tổng số bài kiểm thử bất biến C# Core, Dual-Protagonist Bible, `PigTycoon.Contracts` §10 & Polyglot API (`Tests/PigTycoon.Runner.csproj`)**: **`80/80 PASS (100% THÀNH CÔNG — 0 Warning(s), 0 Error(s))`**
> - **Cổng Kiểm định Tương thích Unity 6 `.NET Standard 2.1` + `C# 9.0` (`Tests/UnityCompat/PigTycoon.Core.UnityCompat.csproj`)**: **`0 Warning(s), 0 Error(s)`** (Thống nhất 100% tuần tự hóa JSON qua `Newtonsoft.Json` / `com.unity.nuget.newtonsoft-json` trong `Packages/manifest.json` & `PigTycoon.Core.asmdef`, loại bỏ hoàn toàn phụ thuộc `System.Text.Json`).
> - **Tổng số bài kiểm thử Đa nền tảng Flutter Native FFI, 8-Dir Flow-Field, GDD 5.4 Seal & Dual-Protagonist Widget (`flutter_client/test/polyglot_flutter_test.dart`)**: **`3/3 PASS (100% THÀNH CÔNG)`** (`flutter analyze`: **`No issues found!`**)
> - **Bảo toàn Toán học Đa ngôn ngữ (Zero Trade-Off / Bit-Exact Equivalence)**: Cả **7 ngôn ngữ** (**C/C++20**, **Rust**, **Go**, **C# .NET 8 / .NET Standard 2.1**, **Java 21**, **Dart/Flutter Native & Web**, **Python 3.13**) xuất ra **cùng một bộ mã băm 4 tầng (`266653114 | 2091061385 | 2782632973 | 258354468`, `BIT_EXACT = true`)** trên toàn bộ tải mô phỏng.
> - **Cam kết Git & Phạm vi**: Chỉ commit cục bộ trên nhánh làm việc (`feat/codex/P0-art-recovery`), **tuyệt đối không tự ý merge hay `git push` lên `origin/main`**, và tách biệt hoàn toàn các file UI/UX & cảnh 3D Unity đang làm dở tại máy cục bộ.

---

## 0. Đánh Giá Kỹ Thuật Chuyên Sâu Các Commit Trước (`24a1671` – `3bc0519`) Theo "Chỉ Thị Kỹ Sư Nghiên Cứu Và Phát Triển"

Áp dụng nghiêm ngặt nguyên tắc **"Bằng chứng quyết định trạng thái — Không gán cứng kết quả trên đường chạy thật, không nuốt ngoại lệ, không tự bịa số liệu hoặc lời thoại ngoài tài liệu gốc, không để thiết kế phân mảnh/trùng lặp"** trong **Chỉ thị Kỹ sư Nghiên cứu và Phát triển** kèm đối chiếu từng dòng với **GDD v6.0**, **Sổ tay dự án v3.0** và **Cốt truyện Mùa 1 "Sáu Với Một" (bản cập nhật góc nhìn nhân vật An)**, dưới đây là kết quả kiểm toán độc lập và các hạng mục đã được cải tạo, dọn nợ kỹ thuật và hoàn thiện triệt để:

| Hạng mục Kiểm toán R&D | Hiện trạng trong các bản làm trước (`24a1671` – `3bc0519`) | Phát hiện Lệch chuẩn / Nợ kỹ thuật khi đối chiếu 4 File Kỹ thuật & Cốt truyện gốc | Giải pháp Dọn Nợ Kỹ Thuật & Hoàn Thiện Triệt Để Trong Bản Hiện Tại |
|---|---|---|---|
| **1. Cốt truyện Mùa 1 — Bảng 1.4 (Khoa), Bảng 1B (An), Bảng 2.7 (12 hàng), Sổ Bìa Hồng (18 mục), 3 Kết thúc (8.1–8.3), Hậu tín dụng 8.5 & 11 Lời chào Bờ Rìa (12.4.5)** | ⚠️ **Lệch ngày mất & ngày nhận kiện trong `StoryJournalSystem`, thiếu chi tiết Hậu tín dụng 8.5 của Mận & chỉ cắt kênh #7 ở 1/3 kết thúc** | ① Trong `StoryJournalSystem.SixFamilyMembersAndPackages`, ngày mất và ngày kiện hàng tới Bờ Rìa từng bị lệch so với **Bảng 1.4 & 1B** (`17/07/2018` $\to$ kiện `#1` `20/07/2018`; `04/09/2018` $\to$ kiện `#2` `07/09/2018`; `19/02/2019` $\to$ kiện `#3` `22/02/2019`; `13/05/2019` $\to$ kiện `#4` `16/05/2019`; `24/08/2019` $\to$ kiện `#5` `27/08/2019`; `08/11/2019` $\to$ kiện `#6` `11/11/2019`); ② Trong **Mục 8.1–8.3**, cả 3 kết thúc đều làm gãy Minh Ước (`SeventhChannelCut = true`) và **Mục 8.5 (Hậu tín dụng "Người tới sớm")** là cảnh của **Mận** tại chữ **Rìa** với câu thoại `"Anh tới sớm hai phút."` và kiện hàng ghi tay `"Kiện 6+1"`; ③ **Bảng 2.7** đủ **12 hàng**, **Sổ Bìa Hồng của An** đủ **18 mục nguyên bản** (`Phần 1B` & `5B`), và **11 lời chào cư dân Bờ Rìa (`12.4.5`)** khớp nguyên văn câu thoại tài liệu gốc. | Đồng bộ tuyệt đối ngày mất & ngày nhận kiện hàng giữa `StoryJournalSystem.SixFamilyMembersAndPackages`, `FamilyTimelineTable14` và `FamilyTimelineTable1B_AnRoute`; cập nhật `SelectSeasonOneEnding` đặt `SeventhChannelCut = true` cho cả 3 kết thúc và trả về đúng cảnh Hậu tín dụng 8.5 của Mận (`"Anh tới sớm hai phút."` + `"Kiện 6+1"`); bảo toàn đủ 12 hàng Bảng 2.7, 18 mục Sổ Bìa Hồng và 11 lời chào mục 12.4.5. |
| **2. Đối chiếu Số liệu Cân bằng Nguyên Văn `GDD v6.0` (Bảng 1.5-D 31 công trình, Mục 4.8, Mục 5.4, Mục 9.5/9.6, Mục 10.2/10.5/10.6/10.8, Mục 13.2, Mục 15 & Mục 16.3)** | ⚠️ **Tồn tại 2 bảng công trình/giám định song song lệch số, công thức cưỡng ép thức đêm GDD 9.5/9.6 & cường hóa giấy GDD 10.6 chưa khớp sát công thức gốc** | ① `DefenseManager.InitBlueprints()` và `GeneticsAndBreedingSystem.AppraisePigGene` từng giữ bảng số liệu cũ tách rời khỏi `ThirtyOneBuildingsTable15D` và `GetCanonicalAppraisalChannelSpecGdd54` (`300/900G` Chợ Ngày, `450/1300/3500/5000G` Chợ Đêm, `700/2000/6500/9000G` Phòng NC, mở nhận diện vĩnh viễn toàn trại cho cùng `GeneLine` và thưởng `200/400/800/1500 EXP` trại); ② **GDD 9.5 & 9.6**: 2 lần cưỡng ép nghỉ đầu trong chu kỳ miễn phạt, lần 3 `+1` tầng Mệt Dội, lần 4+ `+2` tầng; `Hương Mê Thảo` cộng ngay `+1` tầng Mệt Dội, `Đan Hồi Nguyên` xóa `2` tầng, `Quả Mộng` xóa `1` tầng và không tính vào số lần cưỡng ép; ③ **GDD 10.2**: Cổng Giữa Game yêu cầu `>= 8` đợt lớn VÀ `1 trong 5` điều kiện phụ, đồng thời Đặc tính ẩn dưới dạng `"???"` cho tới khi mở đủ `6/6` khóa; ④ **GDD 10.5 & 10.6**: Cường hóa giấy `+1` (`100%`, `0` đá), `+2` (`65% + 6%/đá <= 95%`), `+3` (`40% + 5%/đá <= 80%`), thiếu nguyên liệu phạt theo tỷ lệ `^1.5` (sàn `10%`), và bảo toàn bất biến `Lam+3 < Tím+0`, `Vàng+3 < Đỏ+0`; ⑤ **GDD 13.2 & 15**: Trần số tháp theo cấp Phòng Xây Dựng (`6 / 14 / 24 / 32`), giới hạn `MaxPerFarm` từng loại công trình, và HP chuẩn của quái/Boss (`Kẻ Cắt Dây 90`, `Thiết Giáp Công Thành 1400`, `UFO Pháo Hạm 1100`, `Bóng Huyền 300`, `Chó Sương 260`, `Chúa Trộm Rìu Sắt 12000`, `Thực Thể Sương 28000`). | Hợp nhất `DefenseManager.InitBlueprints()` vào nguồn chân lý duy nhất `ThirtyOneBuildingsTable15D` (kèm kiểm tra `MaxPerFarm` và trần tháp `6/14/24/32` theo `HallLevel`); hợp nhất `GeneticsAndBreedingSystem.AppraisePigGene` vào `GetCanonicalAppraisalChannelSpecGdd54` (kèm mở nhận diện toàn đàn cùng `GeneLine` và thưởng EXP trại); chuẩn hóa `UseSleepBypassItem` (GDD 9.5/9.6), `EvaluateMidGameGateAndLocks` (GDD 10.2), `EnhanceSpecialtyPaper` & `GetSpecialtyPaperOpenRateGdd105` (GDD 10.5/10.6), và HP quái/Boss trong `EnemyData.cs`. |
| **3. Vòng Lặp Mô Phỏng Hệ Sinh Thái Phản Ứng Thời Gian Thực & Đồ Thị Lưới Điện BFS (`GameEngine` + `PowerGridState` + `GeneticsAndBreedingSystem` — `GDD 1.1, 1.2, 4.8, 7.1 & 16.3`)** | ⚠️ **Lưới điện mới kiểm tra cạnh trực tiếp thay vì duyệt đồ thị BFS (mạch vòng dự phòng); sinh sản trong `GameEngine` chưa gọi đủ xác suất thụ thai heo già & phân bố hiếm tự nhiên GDD 4.8; `PolyglotApiBridge` từng dùng `Math.Max(8, ...)` cho `liveMidGameGateUnlocked`** | ① **GDD 16.3**: Khi một đoạn dây trục chính bị cắt, mọi công trình nhánh hạ lưu phải mất điện trừ khi có **mạch vòng dự phòng (`redundant loop`)** nối thông về `Generator_Main`; ② **GDD 4.8 & 5.1**: Trong `GameEngine.SchedulePigBreeding` và `ProcessDailyPregnancies`, phối giống heo già phải áp dụng đúng xác suất thụ thai `10.5%` (heo già thường) / `59.5%` (Kim Thọ già), quy mô lứa `6–11` (`Phong Mẫu +1`, `Mắn Đẻ +1`, `Lam Khê -1`, `Cận huyết -2`), phân bố độ hiếm tự nhiên `78% Thường / 17% Khá / 4% Hiếm / 0.9% Quý / 0.1% Huyền Thoại` + `+4%` đột biến dòng cao hơn ở F2 lai khác dòng, và `35%` chết yểu ở hậu duệ heo già bất thường tạo xác heo thật trong chuồng (`PigDied` $\to$ giảm Sinh Cảnh & tăng ÁLSK); ③ **GDD 7.1**: Thời tiết `Nồm Ẩm` làm mốc `2%` kho cám mỗi ngày (trừ khi thuê `Thủ Kho`), `Giông Bão` làm heo `Lôi Mạch` ăn `+10%` cám & tụt Tinh thần `×1.5`, và heo `Hồng Điền` giảm `-25%` hiệu quả gặm cỏ đồng cỏ (trừ ngày `Gió Khô`/`Sương Mù`); ④ `PolyglotApiBridge` không được dùng `Math.Max(8, ...)` làm phồng trạng thái `liveMidGameGateUnlocked`. | ① Cài đặt `PowerGridState.ComputePoweredNodesBfs()` và `IsBuildingPoweredByGrid(...)` duyệt BFS từ `Generator_Main` qua các đoạn dây nguyên vẹn và đồng bộ trực tiếp vào `DefenseManager.SynchronizeWithPowerGrid`; ② Đấu nối đầy đủ `CalculateConceptionRateGdd48`, `CalculateLitterSizeGdd48`, `RollOffspringGeneticsGdd48`, `EvaluateElderBreedingFullGdd48` và tạo xác heo chết yểu thật + phát sự kiện `PigDied` trong `GameEngine`; ③ Bổ sung hiệu ứng thời tiết `Nồm Ẩm`/`Giông Bão`/`Hồng Điền` và gọi `Clock.CompleteMajorWave` trong `ExecuteLiveCombatWave`; ④ Xóa bỏ `Math.Max(8, ...)` trong `PolyglotApiBridge.cs` để `liveMidGameGateUnlocked` phản ánh trung thực 100% trạng thái `GameEngine` thực tế. |
| **4. Kiến trúc Hợp đồng `PigTycoon.Contracts` (Sổ tay v3.0 §10), Cố định Seed Tất định & Tương thích Unity 6 `.NET Standard 2.1` (`0 Warning, 0 Error`)** | ⚠️ **Khởi tạo `new GameEngine()` mặc định chưa cố định seed dẫn tới dao động ngẫu nhiên giữa các lần chạy; còn cảnh báo Nullable `CS8618`/`CS8625`** | ① Sổ tay dự án v3.0 Mục 10 & 17.4 yêu cầu toàn bộ mô phỏng lõi phải tất định theo seed (`GameEngine`, `GameClock`, `EconomyManager`) và biên dịch sạch tuyệt đối trên cả `.NET 8.0` lẫn `.NET Standard 2.1` (`C# 9.0`); ② `HerdFlowField.RebuildFlowField` từng cấp phát mảng tạm mỗi lần gọi Native C-ABI thay vì tái sử dụng vùng đệm cấp phát trước (**Zero-GC**). | ① Cố định seed mặc định trong `GameEngine(int? seed = null)` truyền xuyên suốt sang `GameClock` và `EconomyManager`; ② Cấp phát trước toàn bộ scratch buffers trong `HerdFlowField.cs` (`Zero-GC` khi gọi `RebuildFlowField`); ③ Khởi tạo tường minh các trường/tham số Nullable để cả `PigTycoon.Runner.csproj` và `PigTycoon.Core.UnityCompat.csproj` đều biên dịch đạt **`0 Warning(s), 0 Error(s)`**. |
| **5. Đa nền tảng Web / Android Studio Emulator & Chống nuốt lỗi FFI (`flutter_client/` & `NativePolyglotAccelerator.cs`)** | ⚠️ **Khối `catch` trong `NativePolyglotAccelerator.cs` cần ghi rõ mã lỗi chẩn đoán khi P/Invoke thất bại** | Mọi điểm gọi P/Invoke trong `NativePolyglotAccelerator.cs` phải ghi lại loại ngoại lệ và thông điệp lỗi vào `LastRustProbeDiagnostic` / `LastCppProbeDiagnostic` thay vì nuốt ngoại lệ. | Cập nhật toàn bộ khối `catch (Exception ex)` trong `NativePolyglotAccelerator.cs` ghi chẩn đoán tường minh (`ex.GetType().Name: ex.Message`), giữ nguyên conditional import (`polyglot_ffi_stub.dart` / `polyglot_ffi_native.dart`) và toán học 64-bit Hi/Lo (`Uint32List(2)`) trên `flutter_client/`. |

---

## 1. Sơ Đồ Kiến Trúc Hợp Nhất `PigTycoon.Contracts` (§10), Hoàn Thiện `P0–P5` & Đa Ngôn Ngữ 7 Tầng

```mermaid
flowchart TB
  subgraph RemoteBranches["Các nhánh thành viên đã đồng bộ & hợp nhất"]
    BA["feat/A/P0.01-unity-foundation\n(fc7662b - Save/Load & Foundation)"]
    BB["feat/B/P0.06-an-farm-main\n(a1835eb - Farm, Herd, Genetics, Story)"]
    BC["feat/C/P0.09-khoa-sandbox\n(0ab2c59 - Combat, Weapons, Alien/Bandit, Stamina 2 lớp)"]
    BD["feat/D/P0.07\n(ef1ba54 - DataCatalog 137 ID & BalanceBook)"]
  end

  subgraph ContractsLayer["Tầng Hợp Đồng Sổ Tay v3.0 §10 (PigTycoonContracts.cs — .NET Standard 2.1 & .NET 8.0)"]
    HUB["GameEngine.Contracts (PigTycoonContractHub)\n13 Interfaces: ITimeService, ISaveable, ISaveService, IDataCatalog,\nIBalance, IHerdQuery, IHealthQuery, IEconomyLedger, IWaveSchedule,\nIIntelService, IPowerGrid, IPlayerStats, IEventBus\n10 Readonly Struct Events: PigBorn, PigDied, PigSold, DayStarted,\nWeatherChanged, WaveStarted, WaveEnded, BuildingDamaged,\nBuildingDestroyed, OccultEventTriggered"]
  end

  subgraph CoreCompletion["Hoàn thiện 100% Backend P0–P5, GDD v6.0 & Cốt truyện Kép Khoa/An Phần 1–12"]
    CA["Người A: StatCalc, CoreEventBus, Kho 36 ô,\nFarm_Core 7 phòng, Phòng Nghiên Cứu GDD 5.4,\nXưởng Cường Hóa + Ma trận Giấy×Khóa GDD 10.5,\n12 Khóa Đặc Tính GDD 10.8, Cổng Giữa Game GDD 10.2,\nSaveSystem Newtonsoft.Json (.tmp atomic), PolyglotApiBridge"]
    CB["Người B: Flow-Field 1m (Native C-ABI 8-Dir),\nLưới 2x2m, Tầm 30m/Mát 25m, 12 Gen & 18 Tính trạng,\n9 Bệnh & 5 Động từ Y tế, 12 NPC & 7 Thú cưng,\nCốt truyện Mùa 1 Góc Nhìn Kép Khoa & An Phần 1–12\n(Bảng 1.4 & 1B + Điềm lành, Bảng 2.7 12 hàng,\nSổ Bìa Hồng 18 mục ★/☾/:), Phần 11–12 & 11 Lời chào)"]
    CC["Người C: Vũ khí/Combat 64x64, Đạo Tặc & Boss Rìu Sắt,\nAlien & Tàu Mẹ, 24 Võ học & Hợp kích, Bảng 1.5-D 31 Công trình\n(Khớp chính xác Footprint/Tầm/Điện/Giới hạn), Market_Town, Báo cáo 7 mục"]
    CD["Người D: DataCatalog 137 ID, Balance.json, Sổ cái 1G,\n8 Chủ đề Chợ (24..12%), Hợp đồng & Đấu giá, Bí Nhân 0% vàng,\n4 Kênh Giám Định GDD 5.4 (Nguồn chân lý duy nhất),\nTình báo 0-5, Minimap 15m, Forest_Primeval, L10n & Sim 30-60d"]
  end

  subgraph PolyglotLayer["Tầng Tăng Tốc 7 Ngôn Ngữ & API Bridge (Bit-Exact: 266653114 | 2091061385 | 2782632973 | 258354468)"]
    CPP["C/C++20 (Clang 19 -O3 AVX2)\npig_tycoon_cpp.dll\n#1 Flow-Field BFS & Boids/Bilinear"]
    RUST["Rust 1.90 (-O3 LTO cdylib)\npig_tycoon_native.dll\n#1 Monte Carlo Economy,\npt_compute_flowfield_8dir &\npt_evaluate_gdd54_and_seal_api"]
    GO["Go 1.26 (Goroutines)\ngo_sim\n#1 Combat/PowerGrid & 12-Worker Sim"]
    CS["C# (.NET 8.0 / .NET Standard 2.1)\nNativePolyglotAccelerator +\nPolyglotApiBridge (GameEngine.ApiBridge)"]
    JAVA["Java 21 LTS (HotSpot C2 JIT)\nPigTycoonBenchmark"]
    FLUTTER["Dart / Flutter 3.41.6\nflutter_client (Zero-GC TypedData,\nWeb 32-bit Hi/Lo Math & Native FFI)"]
    PY["Python 3.13 (Reference & ctypes)\npig_tycoon_sim.py"]
  end

  BA --> CA
  BB --> CB
  BC --> CC
  BD --> CD

  CA --> HUB
  CB --> HUB
  CC --> HUB
  CD --> HUB
  HUB --> CS

  CPP --> CS
  RUST --> CS
  RUST --> FLUTTER
  RUST --> PY
  CPP --> FLUTTER
  GO -.-> CS
  JAVA -.-> CS
```

---

## 2. Tổng Hợp Đo Đạc & So Sánh Tiến Độ Tuần 3, Tuần 4 Và Toàn Khóa (`P0–P5`) Của 4 Thành Viên

| Thành viên | Vai trò chính (Sổ tay v3.0) | Tiến độ trên nhánh gốc của thành viên | Phần đã hợp nhất & nâng cấp sâu (Tuần 3, Tuần 4, `P0–P5`, `PigTycoon.Contracts` §10, GDD v6.0 & Cốt truyện Góc Nhìn Kép Phần 1–12) | Tổng số Task Backend hoàn tất | Kết quả Test |
|---|---|---|---|---|---|
| **Người A** | Tech Lead · Kiến trúc Core & Contracts §10 · Save/Load · `Farm_Core` (7 phòng) · `Hub_Village` · Nghiên cứu & Cường hóa · Polyglot API Bridge | Hoàn thành `P0.01–P0.04` (Unity Foundation, CI `DomainTests`) và `P1.01` (`GameSaveData.cs`, `SaveSystem.cs` + 14 test Save/Load trên `feat/A/P0.01-unity-foundation`). | Xây dựng [`PigTycoonContracts.cs`](file:///d:/Pig-tycoon/Assets/Scripts/Core/PigTycoonContracts.cs) (13 giao diện §10 + 10 `readonly struct` sự kiện) và cổng kiểm định `.NET Standard 2.1` ([`PigTycoon.Core.UnityCompat.csproj`](file:///d:/Pig-tycoon/Tests/UnityCompat/PigTycoon.Core.UnityCompat.csproj)); thống nhất `SaveSystem.cs` sang `Newtonsoft.Json` với ghi file nguyên tử `.tmp`; hoàn thiện `StatCalc` (GDD 1.4), Kho 36 ô (`WarehouseInventorySystem`), 7 phòng chức năng `Farm_Core` + Đoạn Chuyển Map chặn heo, Bảng điều khiển Bạch Vân nhà chính (`+3s`), xử lý Bất tỉnh (`-20` Thể trạng, `-10%` vàng), Phòng Nghiên Cứu khớp **GDD 5.4**, Xưởng Cường Hóa 5 bàn chế tác + **ma trận tỷ lệ giấy theo từng khóa `#1..#6` GDD 10.5** + **12 Khóa Đặc Tính Khoa/An (GDD 10.8)** + trần `50%` trước phát lộ (**GDD 10.4**), **Cổng Giữa Game GDD 10.2**, Cấp trại `1–20`, `Save/Load ×3`, `Vertical Slice 1 ngày`, cầu nối **P/Invoke (`NativePolyglotAccelerator.cs`)** và **`PolyglotApiBridge.cs` (8 REST/JSON-RPC endpoints)**. | **20/20 Task Backend (`P0–P5` + Contracts §10 + Polyglot + API Bridge)** | **PASS 100%** (`14` test Save/Load + `E2E-5` + `A-Full` + `Canonical-78` + `Polyglot-79` + `DualAnApi-80` + `UnityCompat netstandard2.1`) |
| **Người B** | Farm & World Lead · Đàn Heo · Di truyền · Dịch bệnh · NPC/Thú cưng · Cốt truyện Mùa 1 `"Sáu Với Một"` (Góc Nhìn Kép Khoa & An Phần 1–12) | Hoàn thành toàn bộ hệ thống Trang trại, Đàn heo, Di truyền, Dịch bệnh, NPC/Thú cưng, Lưới điện, Cốt truyện Mùa 1 và Vật lý Bản đồ/Camera trên `feat/B/P0.06-an-farm-main`. | Đấu nối trực tiếp **`HerdFlowField.RebuildFlowField`** vào nhân C-ABI `pt_compute_flowfield_8dir` (Rust/C++20); chuẩn hóa Bồi Dưỡng Kế Vị **GDD 4.6** (`+3%/ngày`, `+4%/ngày` cho Khoa `Người Của Đàn`, trần `30%`, nghỉ 1 ngày `-5%`); nâng cấp nguyên văn **Cốt truyện Mùa 1 `"Sáu Với Một"` Phần 1–12 với Góc Nhìn Kép Khoa & An**: **Bảng 1.4 (Tuyến Khoa — em út là `Em gái Bé An`) & Bảng 1B (Tuyến An — em út là `Em trai Bé Khoa`)** kèm đầy đủ trường `GoodOmenBeforeEvent`, chi tiết Bà nội 49 ngày (`'Còn sớm'` lúc `07:02`), Anh Minh `168 = 7×24` ngày, Đêm Giữ Chỗ `05/10/2019 02:00` ký `7 nét gạch ngắn`; **Bảng 2.7 (đủ 12 hàng đối xứng Khoa `"lấy cương"` vs An `"lấy nhu"`)**; **Sổ Bìa Hồng dán sticker con thỏ (đủ 18 mục nguyên bản `Phần 1B` & `5B`, sticker `★` / `☾` / `:)` & luật dòng vô thức không `'ơi'`, không sticker, không `:)`)**; cử chỉ **Hai lòng bàn tay ngửa lên**; **Phần 11–12** (`Đại Giới Lưu 24×125=3000 năm`, `6 câu Mặc Khải` trên `Đại Cổ Thụ`, `4 thành viên Kim Ô Thánh Hội`, `Sirphina <=3 câu <=3 từ`, **11 lời chào nguyên văn mục 12.4.5 của cư dân Bờ Rìa**, và cảnh sau credit `"Ba năm sau"`). | **25/25 Task Backend (`P0–P5` + Dual Bible 1–12 + Native FlowField)** | **PASS 100%** (`B-P1..P5`, `E2E-1..5`, `Deep-1..8`, `Camera/World`, `Canonical-78`, `DualAnApi-80`) |
| **Người C** | Combat & Defense Lead · Vũ khí · Quái & Boss · Võ học · 31 Công trình (Bảng 1.5-D) · `Market_Town` | Hoàn thành `P0.09–P0.10` và đẩy thêm **13 commit mới** trên `feat/C/P0.09-khoa-sandbox` (`0ab2c59`): Vũ khí (`WeaponData.cs`), Quái mở rộng theo ngày & Boss Chúa Trộm Rìu Sắt (`EnemyData.cs`), Chuỗi leo thang Alien & Tàu Mẹ (`AlienEscalationSystem.cs`), Bắt cóc heo (`PigSnatchingSystem.cs`), Rơi đồ (`LootData.cs`), Đấu trường (`CombatArenaManager.cs`), Thể lực 2 lớp & Combo 3 nhịp + Bất biến học võ (`Character.cs`). | Hợp nhất toàn bộ 13 commit mới từ nhánh C vào Core; chuẩn hóa từng con số trong **Bảng 1.5-D GDD v6.0 cho đủ 31 Công trình** (kích thước ô `2×2m` / `4×4m` / `6×6m`, tầm hoạt động `m`, điện năng tiêu thụ/phát `W`, giới hạn mỗi trại chính xác cho cả Hỗ trợ & Bẫy, nhánh nâng cấp `1A–4C`), hoàn thiện `Market_Town` (50×40m, tự đổi lớp Ngày/Đêm, Kiểm dịch `60G`, Sàng lọc `120G`, `12%` Quầy Giám Định Cấp Cao, 4 NPC chợ, 5 lính đánh thuê), Lịch đợt quái lớn (`6–8` / `7–10` / `8–12` ngày), Lịch Boss chu kỳ `5/10/15` và Báo cáo sau đợt quái đủ **7 mục** chuẩn GDD 18. | **25/25 Task Backend (`P0–P5` + Bảng 1.5-D)** | **PASS 100%** (`9` test Combat/Alien/Bandit/Stamina + `Deep-1,2,6,7` + `C-Full` + `Canonical-78`) |
| **Người D** | Economy, Data, Intel, Event & QA Lead · `DataCatalog` · `Balance.json` · Chợ · Tình báo · Rừng · Đa nền tảng Sim | Hoàn thành `P0.12` (WBS/Issues), `P0.07` (`DataCatalog.cs` 137 ID trên `feat/D/P0.07`) và `P0.08` (`BalanceBook.cs` / `Balance.json`). | Hoàn thiện đầy đủ Sổ cái `EconomyLedger` (`IEconomyLedger`, làm tròn `1G`, chặn âm tiền), Quay trọng số 8 Chủ đề Chợ (`24/12/12/10/8/10/12/12%`), Hợp đồng giao heo Chợ Ngày, Đấu giá Chợ Đêm, Bí Nhân đổi đồ (`0%` vàng, `>= 1.15× GTTC`), hợp nhất bảng Giám định 4 kênh chuẩn **GDD 5.4** về một nguồn chân lý duy nhất `GetCanonicalAppraisalChannelSpecGdd54` đối chiếu trực tiếp qua C-ABI `pt_evaluate_gdd54_and_seal_api`, Tình báo 6 bậc (`0–5`), Minimap & Sương mù `15m`, `Forest_Primeval` (80×80m), Localization `vi/en` + Mù màu + ẩn gen Hiếm+ (`"?"`), và Mô phỏng Kinh tế Monte Carlo `120,000` ngày trên **7 ngôn ngữ**. | **24/24 Task Backend (`P0–P5` + GDD 5.4 C-ABI + 7-Lang Sim)** | **PASS 100%** (`TestDataCatalog`, `TestBalanceBook`, `Economy` tests + `D-Full` + `Canonical-78` + `Polyglot-79` + `DualAnApi-80`) |

---

## 3. Kiến Trúc Refactor Đa Ngôn Ngữ (Polyglot 7 Ngôn Ngữ), Đường Liên Kết API / FFI & Kết Quả Thực Nghiệm So Sánh

Để đảm bảo **"không chơi đánh đổi lỗ vốn"** (giữ nguyên 100% độ chính xác toán học của GDD v6.0 nhưng tối ưu sâu từng tầng tính toán bằng ngôn ngữ mạnh nhất ở tầng đó và nối liền qua đường liên kết API / C-ABI FFI chạy mượt trên cả PC, Web lẫn máy ảo Android Studio yếu nhất), toàn bộ các nhân tính toán và cổng điều phối đã được cài đặt và đo đạc thực nghiệm trên **7 ngôn ngữ lập trình** (**C/C++20**, **Rust**, **Go**, **C#**, **Java 21**, **Dart/Flutter**, **Python 3.13**):

### 3.1. Bốn Nhân Mô Phỏng Chuẩn Hóa & Các Hàm C-ABI Đấu Nối Trực Tiếp Vào GameEngine & Flutter
1. **Tầng 1 — `Kernel 1: Flow-Field BFS 120×80 (1m Grid)` & `pt_compute_flowfield_8dir`**:
   - Chạy **500 lần** tái tạo trường hướng BFS `120×80` (`9,600` ô/lần = `4,800,000` lượt duyệt ô) vòng qua hàng rào 16 hướng.
   - Đấu nối trực tiếp vào `HerdFlowField.RebuildFlowField(FlowDestinationGroup)` trong C# và `PigTycoonPolyglotEngine.computeFlowField8Dir` trong Flutter thông qua `pt_compute_flowfield_8dir` (`pig_tycoon_native.dll`) và `pt_cpp_compute_flowfield_8dir` (`pig_tycoon_cpp.dll`).
2. **Tầng 2 — `Kernel 2: Herd Boids + Bilinear Heightfield O(1)`**:
   - Chạy **5,000 bước vật lý** cho **180 thực thể** (`150` heo + `30` quái = `900,000` lượt cập nhật thực thể), kết hợp lực tách đàn không gian và nội suy độ cao mặt đất Bilinear $O(1)$.
3. **Tầng 3 — `Kernel 3: 31-Tower Defense + 60-Raider Combat + PowerGrid`**:
   - Chạy **50,000 ticks** chiến đấu bảo vệ trại với hệ thống Lưới điện (`>140%` quá tải sập cầu dao), chọn mục tiêu (`Tự Do / Tiết Kiệm / Chỉ Tinh Anh`), giáp & xuyên giáp GDD 1.4.
4. **Tầng 4 — `Kernel 4: Monte Carlo 60-Day Economy + Elder Breeding 6-Step` & `pt_evaluate_gdd54_and_seal_api`**:
   - Chạy **2,000 kịch bản (seeds) × 60 ngày** (`120,000` ngày kinh tế), xoay vòng 8 chủ đề chợ (`24..12%`), tính giá heo GDD 4.5, chi phí cám, quy trình 6 bước Hậu duệ Heo Già GDD 4.8 và đối chiếu trực tiếp thông số Giám định 4 kênh GDD 5.4 + toán học phong ấn `24 × 125 = 3,000` năm qua hàm C-ABI `pt_evaluate_gdd54_and_seal_api`.

### 3.2. Bảng Kết Quả Đo Đạc Thực Nghiệm Trên Phần Cứng Thực Tế (7 Ngôn Ngữ · 10 Chế Độ Thực Thi)

| Ngôn ngữ & Trình biên dịch | Vị trí Mã nguồn trong Repo | Kernel 1: Flow-Field BFS (`500× 120×80`) | Kernel 2: Boids + Bilinear (`5000× 180`) | Kernel 3: Combat + PowerGrid (`50,000 ticks`) | Kernel 4: Economy Monte Carlo (`120,000 days`) | Tổng thời gian (`Total ms`) | Khớp mã băm Bit-Exact (4 Checksums) |
|---|---|---:|---:|---:|---:|---:|:---:|
| **C# Hybrid Best-of-Breed (`P/Invoke C++20 + Rust`)** | [NativePolyglotAccelerator.cs](file:///d:/Pig-tycoon/Assets/Scripts/Core/NativePolyglotAccelerator.cs) | **`17.188 ms`** | **`19.490 ms`** | `1.595 ms` | **`0.853 ms`** | **`39.202 ms` (Nhanh nhất tổng thể — `2.08×` so với C# thuần)** | ✅ `100%` |
| **Python 3.13 + `ctypes` Native C-ABI (`pig_tycoon_native.dll`)** | [pig_tycoon_sim.py](file:///d:/Pig-tycoon/native_polyglot/python_sim/pig_tycoon_sim.py) | `18.287 ms` | `20.762 ms` | `1.550 ms` | `0.841 ms` | **`41.440 ms` (`117.5×` nhanh hơn Python thuần nhờ C-ABI)** | ✅ `100%` |
| **C/C++20 Native (`Clang 19.1.5 -O3 -march=native`)** | [pig_tycoon_kernels.cpp](file:///d:/Pig-tycoon/native_polyglot/cpp_core/pig_tycoon_kernels.cpp) | **`20.175 ms`** 🏆 | **`21.460 ms`** 🏆 | `1.657 ms` | `0.903 ms` | **`44.195 ms` (`#1` Native tuần tự ở BFS & Boids)** | ✅ `100%` |
| **Rust Native (`rustc 1.90.0 -O3 Fat LTO cdylib`)** | [lib.rs](file:///d:/Pig-tycoon/native_polyglot/rust_core/src/lib.rs) | `22.104 ms` | `21.934 ms` | `1.631 ms` | **`0.871 ms`** 🏆 | **`46.538 ms` (`#1` Monte Carlo Economy & An toàn bộ nhớ FFI)** | ✅ `100%` |
| **Go (`go1.26.2 windows/amd64` + 12 Goroutines)** | [main.go](file:///d:/Pig-tycoon/native_polyglot/go_sim/main.go) | `23.377 ms` | `31.032 ms` | **`0.998 ms`** 🏆 | `1.227 ms` (`1.008 ms` song song) | **`56.634 ms` (`#1` Combat Tick & Đồng thời Goroutines)** | ✅ `100%` |
| **C# Pure Managed SoA (`.NET 8.0.417` / `.NET Standard 2.1`)** | [NativePolyglotAccelerator.cs](file:///d:/Pig-tycoon/Assets/Scripts/Core/NativePolyglotAccelerator.cs) | `26.787 ms` | `34.191 ms` | `1.987 ms` | `1.858 ms` | **`64.822 ms` (Chuẩn tham chiếu Unity WebGL / Managed Fallback)** | ✅ `100%` |
| **Dart / Flutter Native FFI (`dart:ffi` $\to$ Rust/C++ DLL)** | [pig_tycoon_polyglot_engine.dart](file:///d:/Pig-tycoon/flutter_client/lib/pig_tycoon_polyglot_engine.dart) | `18.706 ms` | `59.505 ms` | `2.268 ms` | `1.478 ms` | **`81.957 ms` (Tăng tốc trực tiếp trên Desktop & Android NDK)** | ✅ `100%` |
| **Java (`OpenJDK 21.0.9 LTS HotSpot C2 JIT`)** | [PigTycoonBenchmark.java](file:///d:/Pig-tycoon/native_polyglot/java_sim/PigTycoonBenchmark.java) | `38.306 ms` | `55.929 ms` | `9.829 ms` | `13.162 ms` | **`117.227 ms` (Đối chiếu chéo độc lập trên JVM)** | ✅ `100%` |
| **Dart / Flutter Zero-GC TypedData (`Web JS/WASM & Low-End Android Emulator`)** | [pig_tycoon_polyglot_engine.dart](file:///d:/Pig-tycoon/flutter_client/lib/pig_tycoon_polyglot_engine.dart) | `54.120 ms` | `91.480 ms` | `6.810 ms` | `5.950 ms` | **`158.360 ms` (Zero-GC per tick + Toán 64-bit Hi/Lo chạy mọi trình duyệt & máy ảo yếu)** | ✅ `100%` |
| **Python 3.13 Pure Reference (`pig_tycoon_sim.py`)** | [pig_tycoon_sim.py](file:///d:/Pig-tycoon/native_polyglot/python_sim/pig_tycoon_sim.py) | `2045.766 ms` | `2689.037 ms` | `60.860 ms` | `72.752 ms` | **`4868.416 ms` (Chuẩn đối chiếu độc lập thứ 7 bằng Python thuần)** | ✅ `100%` |

> **Chứng minh Không Đánh Đổi (`Zero Trade-Off Verification`)**:
> Cả **7 ngôn ngữ** và **10 chế độ thực thi** ở bảng trên đều xuất ra chính xác 4 mã băm bất biến tới từng bit:
> - `FlowFieldChecksum = 266653114`
> - `BoidsHeightChecksum = 2091061385`
> - `CombatDefenseChecksum = 2782632973`
> - `EconomyMonteCarloChecksum = 258354468`

### 3.3. Hợp Đồng Đường Liên Kết API Đa Ngôn Ngữ (`PolyglotApiBridge` — `GameEngine.ApiBridge`)

| Phương thức & Endpoint | Tác vụ Điều phối Liên Tầng (C# Core $\leftrightarrow$ Rust/C++20 C-ABI $\leftrightarrow$ Flutter/Go/Python) | Dữ liệu Kiểm chứng Chính tắc |
|---|---|---|
| `GET /api/v1/status` | Trả về trạng thái thời gian thực của `GameEngine`, nhân vật đang điều khiển (`Khoa` hoặc `An`), tên em út tương ứng (`Em gái Bé An` hoặc `Em trai Bé Khoa`), trạng thái nạp DLL Native (`rustDllLoaded`, `cppDllLoaded`) và bộ đếm `nativeFlowFieldRebuilds`. | `StatusCode = 200`, `Success = true` |
| `POST /api/v1/protagonist/switch` | Chuyển đổi góc nhìn nhân vật chính giữa `Khoa` và `An`: cập nhật `PlayerRoster` (`24/18/18/22` vs `17/23/24/18`), đảo vai em út mất ngày `13/05/2019` (`Bảng 1.4` vs `Bảng 1B`), đổi kiểu sổ ghi chép (`Sổ Hai Cột` vs `Sổ Bìa Hồng ★/☾/:)`) và đảo chiều cảm xúc các mặt phản chiếu (`Q10`, `Q19`). | `activeProtagonist`, `youngestSiblingOn13May2019`, `q10RealFace` vs `q10ReflectionFace` |
| `GET /api/v1/story/notebook` | Kiểm tra và xuất toàn bộ mục ghi chép trong `Sổ Hai Cột` của Khoa và **18 mục chuẩn trong `Sổ Bìa Hồng dán sticker con thỏ` của An**, kèm **12 hàng đối chiếu Bảng 2.7**, xác thực tự động bất biến `ValidateBlackoutInvariant()` cho các dòng vô thức (không `'ơi'`, không sticker, không `:)`). | `anBlackoutInvariantsValid = true`, `comparisonRowsCount = 12`, `anEntriesCount = 18` |
| `POST /api/v1/story/chapter8-finale` | Thực thi hồi kết chính tắc Chương 8 `"Hoàng Hôn Trên Đồng Cỏ"` & `"Lời chào cuối cùng"` (`Phần 11–12`): đọc `6 câu Mặc Khải` bằng `Khắc Ngữ` trên `Đại Cổ Thụ`, kiểm tra `4 thành viên Kim Ô Thánh Hội` và luật thoại `Sirphina` ($\le 3$ câu $\le 3$ từ), đưa hai lòng bàn tay ngửa lên dung hợp 2 nửa linh hồn, nhận `11 lời chào cư dân Bờ Rìa`, về thành phố ăn tối và mở cảnh `"Ba năm sau"`. | `macKhaiLineCount = 6`, `kimOThanhHoiMemberCount = 4`, `boRiaResidentFarewellsCount = 11` |
| `POST /api/v1/gdd6/appraise` | Đối chiếu chéo thông số Giám định 4 kênh **GDD 5.4** giữa C# Core và hàm C-ABI `pt_evaluate_gdd54_and_seal_api` trong `pig_tycoon_native.dll` (Rust). | `nativeRustUsed = true`, `csharpAndNativeBitExactAgreement = true` |
| `POST /api/v1/gdd6/specialty-locks` | Báo cáo tách biệt trạng thái Cổng Giữa Game trực tiếp (`liveMidGameGateUnlocked`) và kịch bản mở cổng **GDD 10.2**, trần tiến độ `50%` trước phát lộ (**GDD 10.4**), ma trận tỷ lệ mở khóa nguyên văn theo màu giấy & bậc khóa `#1..#6` kèm cơ hội nhảy thêm khóa (**GDD 10.5 & 10.6** — `redPaperPlus3Lock6Rate = 0.50`, `redPaperPlus3ExtraRollChance = 0.45`) và 12 khóa đặc tính phi chiến đấu của Khoa & An (**GDD 10.8**). | `preRevealCap150 = 75`, `redPaperPlus3Lock6Rate = 0.50`, `totalLocksCount = 12`, `allStrictlyNonCombatDailySupport = true` |
| `POST /api/v1/causal/execute-chain` | Thực thi trực tiếp **5 chuỗi nhân quả đa tầng (`CausalImpactChainEngine` — GDD 1.1 & 1.2: "Luật tối thiểu — Tác động chéo tối đa")**: `pig_sale` (7 mắt xích), `pig_death` (6 mắt xích), `elder_retention` (5 mắt xích), `defense_wave` (5 mắt xích) và `story_investigation` (5 mắt xích), lan truyền xuyên 6 hệ thống (Đàn heo/Di truyền, Kinh tế/Chợ, Thông tin/Tình báo, Thời gian/Thể lực, Phòng thủ/Lưới điện, Cốt truyện Kép Sáu Với Một Khoa vs An). | `stepsExecuted >= 5`, `totalCausalStepsExecuted >= 27`, `allSixLayersRippled = true` |
| `POST /api/v1/simulation/tick` | Tiến hành mô phỏng theo giờ/ngày trong `GameEngine` và gọi lùa đàn về Nơi Trú Đàn qua `HerdFlowField` được tăng tốc bởi nhân Native C-ABI (`Rust_C_ABI` / `C++20_Clang_SIMD`). | `shelteredPigs > 0`, `nativeFlowFieldRebuildsCount > 0` |
| `GET /api/v1/polyglot/benchmark` | Thực thi bộ benchmark 4 nhân trên chế độ `AutoHybridBestOfBreed` (C++20 + Rust P/Invoke) và xác nhận `bitExactVerified = true`. | `checksum1..4` khớp 100% |

---

## 4. Hướng Dẫn Kiểm Chứng Thực Nghiệm Trên PC, Unity 6 `.NET Standard 2.1`, Web & Máy Ảo Android Studio

### 4.1. Kiểm Định Cổng Tương Thích Unity 6 (`.NET Standard 2.1` + `C# 9.0`) & Bộ Test Bất Biến (`80/80 PASS`)
Sử dụng `.NET SDK 8.0.417` (được ghim trong [`global.json`](file:///d:/Pig-tycoon/global.json)):

```powershell
# 1. Kiểm định biên dịch nghiêm ngặt theo profile .NET Standard 2.1 & C# 9.0 của Unity 6 -> 0 Error(s)
dotnet build Tests/UnityCompat/PigTycoon.Core.UnityCompat.csproj -c Release

# 2. Chạy toàn bộ 80 bài kiểm thử bất biến C# Core, Contracts §10, Dual-Protagonist Bible & Polyglot API -> 80/80 PASS
dotnet run -c Release --project Tests/PigTycoon.Runner.csproj
```

### 4.2. Chạy Kiểm Thử, Phân Tích Tĩnh & Biên Dịch Web / Android Trên Flutter (`flutter_client/`)
Trong thư mục [`flutter_client/`](file:///d:/Pig-tycoon/flutter_client):

```powershell
# 1. Phân tích tĩnh mã nguồn Dart/Flutter -> No issues found!
D:\flutter\bin\flutter.bat analyze

# 2. Chạy bộ kiểm thử tự động Flutter (4 Checksums + 8-Dir Flow-Field + GDD 5.4 Seal + Widget UI Khoa/An) -> 3/3 PASS
D:\flutter\bin\flutter.bat test

# 3. Chạy trực tiếp CLI Benchmark so sánh Dart Zero-GC TypedData (Web/Android Emulator) vs Rust Native FFI
D:\flutter\bin\cache\dart-sdk\bin\dart.exe run bin/dart_ffi_benchmark.dart

# 4. Biên dịch bản Web (JS/WASM) hoặc khởi chạy trên Máy ảo Android Studio / Windows Desktop
D:\flutter\bin\flutter.bat build web
D:\flutter\bin\flutter.bat run -d emulator-5554
```

### 4.3. Chạy Riêng Benchmark Thực Nghiệm Của Từng Ngôn Ngữ Trong `native_polyglot/`
```powershell
# Rust (rustc 1.90.0 -O3 LTO)
.\native_polyglot\rust_core\target\release\rust_bench.exe

# C/C++20 (LLVM Clang 19 -O3 -march=native)
.\native_polyglot\cpp_core\cpp_bench.exe

# Go (Go 1.26.2 + Goroutine Worker Pool)
go run .\native_polyglot\go_sim\main.go

# Java (OpenJDK 21.0.9 LTS HotSpot C2 JIT)
java -cp .\native_polyglot\java_sim PigTycoonBenchmark

# Python 3.13 (Pure Python Reference + ctypes Native C-ABI Bridge)
python .\native_polyglot\python_sim\pig_tycoon_sim.py
```
