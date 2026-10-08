# PIG TYCOON - TÀI LIỆU BÀN GIAO DỰ ÁN (HANDOVER & PROJECT CONTEXT BRIEF)

> **Mục đích tài liệu**: Tài liệu này tóm tắt toàn bộ bối cảnh, kiến trúc, quy chuẩn kỹ thuật, các tính năng đã hoàn thiện và quy trình làm việc của dự án **Pig Tycoon**. Dành cho AI agent hoặc lập trình viên mới khi mở một phiên làm việc (chat) mới để nắm bắt 100% dự án trong vài giây mà không cần đọc lại lịch sử chat cũ, giúp tiết kiệm tối đa token.

---

## 1. THÔNG TIN CỐT LÕI DỰ ÁN
* **Tên dự án**: Pig Tycoon (Game mô phỏng chăn nuôi heo kết hợp yếu tố huyền ảo, phòng thủ, kinh tế sâu sắc).
* **Đường dẫn thư mục dự án**: `/home/danh/.gemini/antigravity/scratch/pig-tycoon-core`
* **Git Branch**: `Prototype`
* **Nền tảng & Engine**: Unity 6 (`6000.6.3f1`), Universal 2D (URP 2D), Camera Orthographic 2D phong cách Top-Down Cozy (tương tự *Stardew Valley*).
* **Ngôn ngữ**: C# (.NET 8.0).
* **Phong cách đồ họa**: Pixel Art 16-bit / 32-bit sắc nét (FilterMode: Point, Clamp).

---

## 2. TÀI LIỆU CƠ CHẾ GỐC (GAME DESIGN DOCUMENT - GDD v6.0)
Tất cả cơ chế, công thức toán học, lore, giống gien, chỉ số kinh tế, chiến đấu và phòng thủ từ file gốc **`Pig.docx`** đã được lưu trữ trực tiếp trong repository:
* **File gốc**: [`Docs/Pig.docx`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Docs/Pig.docx) (Được bảo lưu an toàn 100%).
* **Bản Markdown số hóa hoàn chỉnh**: [`Docs/GDD_PIG_MECHANICS.md`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Docs/GDD_PIG_MECHANICS.md) (1,873 dòng, có đầy đủ bảng biểu và mục lục).
* **Mẹo tối ưu Token khi tra cứu**:
  * Khi AI cần tra cứu cơ chế (ví dụ: công thức định giá, gien heo, đợt quái, phòng thủ), **CHỈ DÙNG `view_file` xem đúng mục liên quan** trong `Docs/GDD_PIG_MECHANICS.md` (không đọc toàn bộ file một lúc để tiết kiệm token).
  * Mục lục gồm 6 phần:
    - Phần I: Tổng quan thiết kế & Vòng lặp chơi (Core Loop, 16 phút/ngày).
    - Phần II: Không gian trang trại, Vòng đời heo, Hệ Gen (Hồng Điền, Lam Khê, Kim Thọ, Hư Thể...), Sinh Cảnh & Thời tiết.
    - Phần III: Nhân vật, Thể lực (Stamina), Combat 6 ô trang bị & Võ kỹ.
    - Phần IV: Phòng thủ, Công trình, Bản vẽ, Bạch Vân Ký & Đợt quái.
    - Phần V: Kinh tế (Công thức giá heo 4.5, Chợ ngày/đêm, Bí nhân, ÁLSK).
    - Phần VI: Tổng hợp công thức & Bất biến nghiệm thu.

---

## 3. KIẾN TRÚC MÃ NGUỒN (ARCHITECTURE INVARIANTS)
Dự án được xây dựng theo nguyên tắc phân tầng nghiêm ngặt:
```
pig-tycoon-core/
├── Docs/
│   ├── Pig.docx                   # FILE WORD GỐC CHỨA TOÀN BỘ CƠ CHẾ GAME
│   └── GDD_PIG_MECHANICS.md       # BẢN MARKDOWN TRA CỨU CƠ CHẾ NHANH VÀ TIẾT KIỆM TOKEN
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                  # PURE C# CORE (HOÀN TOÀN KHÔNG PHỤ THUỘC UNITYENGINE)
│   │   │   ├── Enums.cs           # Các enum định danh: GeneLineId, GeneRarity, PigStage, CombatSlotType...
│   │   │   ├── GameClock.cs       # Đồng hồ: 16 phút/ngày thực, 4 buổi (Sáng, Trưa, Chiều, Tối), chu kỳ combat
│   │   │   ├── Farm.cs            # Quản lý diện tích, sức chứa, chỉ số Sinh Thái SC, Áp Lực Sự Kiện ÁLSK
│   │   │   ├── Pig.cs             # Dữ liệu heo: gien, độ hiếm, tuổi, cân nặng, tâm trạng, thân thiết, dị biến
│   │   │   ├── HerdManager.cs     # Bầy đàn, phân cấp thủ lĩnh, điều kiện thăng hạng Sơ Khai
│   │   │   ├── EconomyManager.cs  # Công thức định giá heo GDD v6.0, chợ đêm 18% gold, bí nhân 0% gold
│   │   │   ├── DefenseManager.cs  # Công trình phòng thủ nông trại, Bạch Vân Ký
│   │   │   ├── Character.cs       # Dữ liệu nhân vật, hệ thống Thể Lực (Stamina), Nộ Khí (Rage)
│   │   │   └── GameEngine.cs      # Trái tim mô phỏng liên kết toàn bộ hệ thống Core
│   │   ├── Controllers/           # UNITY MONOBEHAVIOUR PRESENTATION
│   │   │   ├── MobileGameController.cs      # Cầu nối GameEngine với Unity Update & vòng đời Android
│   │   │   ├── PlayerMobileController.cs    # Di chuyển Top-down 2D, Joystick ảo, Y-sorting, võ kỹ
│   │   │   ├── PlayerInteractionController.cs# Tương tác phạm vi 2.5m, tháo dỡ/cắm rào trong tầm 3.8m
│   │   │   ├── FarmEnvironment2D.cs         # Quản lý hạ tầng 2D: Rào, máng ăn, máng nước, bùn, mái trú
│   │   │   ├── PigAgentView.cs              # AI cá thể heo 2D: Flocking, tìm ăn, uống nước, tắm bùn, ngủ
│   │   │   ├── PigSpriteAnimator.cs         # Hoạt ảnh Sprite 4 trạng thái theo giống loài
│   │   │   └── CameraFollow2D.cs            # Camera 2D theo dõi nhân vật, hỗ trợ Zoom con lăn chuột
│   │   └── UI/                    # GIAO DIỆN NGƯỜI CHÙNG (CANVAS & POPUP)
│   │       ├── UISpriteLoader.cs            # Bộ nạp Sprite runtime giữ chuẩn Pixel Art sắc nét
│   │       ├── StardewHUDController.cs      # Bảng đồng hồ gỗ góc trên phải, thanh năng lượng 'E' góc dưới phải
│   │       ├── HotbarController.cs          # Thanh công cụ 8 ô đáy màn hình + Ô Balo thứ 9 ở cuối
│   │       ├── BackpackPopup.cs             # TÚI ĐỒ MINECRAFT: 36 ô vuông, stack count, bảng soi item
│   │       ├── DevModePopup.cs              # MENU DEV: 5 Tab cheats đầy đủ (Tài nguyên, Thời gian, Heo, Hạ tầng, Người)
│   │       └── PigInspectPopup.cs           # Bảng gỗ giám định chi tiết chỉ số từng chú heo
│   ├── Editor/
│   │   └── Universal2DSetup.cs              # Tool tạo scene tự động (Menu: PigTycoon/2. Tạo & Mở Scene...)
│   └── Scenes/
│       └── Farm_Main.unity                  # Scene chính. Tên cũ: MainFarm2D. GUID giữ nguyên.
└── Tests/
    ├── PigTycoon.Runner.csproj              # Test Runner kiểm thử toàn bộ 7 Invariants cốt lõi
    └── Program.cs
```

---

## 3. CÁC TÍNH NĂNG ĐÃ HOÀN THIỆN ĐẾN THỜI ĐIỂM HIỆN TẠI
1. **Thảo Nguyên 2D Rộng Lớn (90m x 64m)**:
   - Khuôn viên chăn thả heo ở trung tâm (40m x 28m), có lối đi đất đỏ kết nối cổng phía Nam (rộng 6m).
   - Hồ nước thảo nguyên tự nhiên phía Đông.
   - Hố tắm bùn làm mát ở phía Đông Nam khuôn viên.
   - Mái trú bão (Shelter) ở góc Tây Bắc.
   - Máng ăn tự động (Feeder) và máng nước ngọt (Water Trough) chuẩn va chạm cứng (Solid Collider).

2. **Cơ Chế Tách Biệt Búa Tháo Lắp & Item Đặt Công Trình Chuẩn Ô Vuông (Grid 1m x 1m)**:
   - Mọi thao tác đặt/tháo dỡ công trình đều bắt buộc căn chỉnh theo ô vuông Tilemap `Vector2Int(x, y)` trên bản đồ thảo nguyên, hoàn toàn không cho phép đặt tự do sai lệch.
   - **Tâm ô Tilemap (Cell Center)**: Trong Unity Tilemap (size 1m x 1m), mỗi cell `(x, y)` có tâm đồ họa tại `(x + 0.5f, y + 0.5f)`. Toàn bộ hệ thống (`FarmEnvironment2D`, `PlayerInteractionController`, `Fence2DView`) sử dụng `WorldToGrid()` và `GridToWorldCenter()` để đồng bộ 100% pixel-perfect với các tile nền đất/cỏ.
   - **Tách Biệt Hoàn Toàn Giữa Búa Gỗ Và Các Item Đặt Công Trình**:
     - **Búa Thợ Mộc (`BuaGo` - Phím 3)**:
       - **Chuyên dùng để Tháo Dỡ & Sửa Chữa công trình**, TUYỆT ĐỐI không dùng để đặt công trình mới trên đất.
       - Khi trỏ vào đất trống: Khung con trỏ tự động ẩn đi. Click vào đất trống sẽ có thông báo hướng dẫn chọn Item tương ứng để đặt mới.
       - Khi trỏ vào Hàng Rào: Nếu hỏng hiện khung Xanh Dương (Sửa chữa +75 HP); nếu nguyên vẹn hiện khung Vàng Hổ Phách (Tháo dỡ, thu hồi 1 Item Hàng Rào).
       - Khi trỏ vào Máng Ăn: Tháo dỡ Máng Ăn, thu hồi **1 Item Máng Ăn** (`CarriedFeeders++`), giảm `Infrastructure.Feeders`.
       - Khi trỏ vào Máng Nước: Tháo dỡ Máng Nước, thu hồi **1 Item Máng Nước** (`CarriedWaterTroughs++`), giảm `Infrastructure.WaterTroughs`.
     - **Item Hàng Rào (`HangRao` - Phím 4)**:
       - Cầm cọc rào trên tay: Khung con trỏ màu Xanh Lá xuất hiện ở các ô đất trống hợp lệ (màu Đỏ nếu vướng cản / ngoài tầm).
       - Click chuột: Đặt cọc rào mới, tiêu hao 1 Item Hàng Rào (`CarriedFences--`), tự động nối khớp 16 hướng với các rào lân cận.
     - **Item Máng Ăn (`MangAn` - Phím 5)**:
       - Cầm máng ăn: Khung con trỏ màu Xanh Lá. Click chuột vào ô đất trống hợp lệ: Đặt Máng Ăn mới (`FarmEnvironment2D.BuildFeeder`), tiêu hao 1 Item Máng Ăn (`CarriedFeeders--`), tăng `Infrastructure.Feeders++`.
     - **Item Máng Nước (`MangNuoc` - Phím 6)**:
       - Cầm máng nước: Khung con trỏ màu Xanh Lá. Click chuột vào ô đất trống hợp lệ: Đặt Máng Nước mới (`FarmEnvironment2D.BuildWaterTrough`), tiêu hao 1 Item Máng Nước (`CarriedWaterTroughs--`), tăng `Infrastructure.WaterTroughs++`.
   - **Tự Động Nối Khớp Rào 16 Hướng (Modular Auto-Connecting Fence)**:
     - Hệ thống 16 Sprite pixel-art độc lập ứng với 16 mặt nạ kết nối 4 hướng (Bắc = 1, Đông = 2, Nam = 4, Tây = 8):
       - `fence_post.png` (Đơn lập - Mask 0)
       - `fence_end_n.png`, `fence_end_e.png`, `fence_end_s.png`, `fence_end_w.png` (Đầu cụt 1 hướng)
       - `fence_h.png`, `fence_v.png` (Thẳng ngang / Thẳng dọc)
       - `fence_corner_ne.png`, `fence_corner_nw.png`, `fence_corner_se.png`, `fence_corner_sw.png` (4 Góc vuông)
       - `fence_t_north.png`, `fence_t_south.png`, `fence_t_east.png`, `fence_t_west.png` (4 Ngã ba chữ T)
       - `fence_cross.png` (Ngã tư giao cắt)
     - Khi đặt 1 rào mới: Tự động kiểm tra 4 ô vuông lân cận. Nếu có rào bên cạnh, rào mới và các rào lân cận sẽ lập tức đồng bộ nối liền với nhau cả về đồ họa lẫn BoxCollider.
     - Khi tháo dỡ 1 rào: Các rào lân cận lập tức tự động ngắt kết nối và chuyển về kiểu sprite thích hợp.
     - Toàn bộ hàng rào chuồng trại ban đầu đều tự động kích hoạt khớp nối hoàn hảo khi khởi chạy game.

3. **Giao Diện Túi Đồ Phong Cách Minecraft (`BackpackPopup.cs`)**:
   - Mở bằng cách click vào ô Balo ở cuối Hotbar hoặc bấm phím **`B`** / **`I`**.
   - Bố cục 36 ô vuông chuẩn Minecraft:
     - 27 ô kho chứa đồ phía trên (3 hàng $\times$ 9 cột).
     - 9 ô công cụ nhanh phía dưới (Hotbar).
     - Số lượng xếp chồng (Stack Count) hiển thị ở góc dưới bên phải mỗi ô chữ trắng viền đen đổ bóng.
     - Khung soi chi tiết vật phẩm ở đáy bảng: Icon lớn, tên, chủng loại, mô tả, nút **[TRANG BỊ]** / **[SỬ DỤNG]**.
   - Có sprite riêng cho Cọc Gỗ Rào: `Assets/Art/UI/item_wood_plank.png`.

4. **Bảng Điều Khiển Dev Mode (`DevModePopup.cs`)**:
   - Nút `[ DEV ⚙ ]` góc trên bên trái màn hình hoặc phím tắt **`~`** (tilde) / **`F1`** / **`F12`**.
   - 5 Tab chức năng:
     - **Tài Nguyên**: +1k/+10k/+50k Vàng, +20 Cọc Gỗ, Hồi đầy Stamina, Bất tử Thể lực, Đầy cám/nước.
     - **Thời Gian**: Đổi tốc độ 1x/2x/5x, tua +1h/+6h/sang ngày, nhảy thẳng tới 06:00 (Sáng) hoặc 20:00 (Đêm/Chợ).
     - **Đàn Heo**: Spawn tức thì Heo Hồng Điền, Lam Khê, Kim Thọ, Hư Thể; Cho cả đàn ăn no; Hồi phục 100% sức khỏe/tâm trạng; Tăng cấp trưởng thành; Tối đa thân thiết (5 Tim).
     - **Hạ Tầng**: Đổ đầy máng ăn/nước; Khôi phục rào mặc định; Xóa trắng rào; Tăng sinh thái SC (+15); Giảm áp lực ÁLSK (-25); Kích hoạt Bạch Vân.
     - **Nhân Vật & Camera**: Chạy nhanh x2.2; Teleport về tâm chuồng (0, 0) hoặc cổng Nam; Zoom Camera 3.8m / 5.2m / 8.5m; Thi triển võ kỹ.

5. **AI Đàn Heo Thảo Nguyên (`PigAgentView.cs`)**:
   - Flocking bầy đàn, tự động di chuyển tìm máng ăn khi đói, tìm máng nước khi khát, lội bùn giải nhiệt khi trời nóng, vào mái trú khi mưa hoặc đêm.
   - Bong bóng suy nghĩ (Thought Bubble) hiển thị cảm xúc khi ăn, uống, tắm, ngủ.
   - Bảng giám định cá thể heo chuẩn phong cách Stardew Valley (`PigInspectPopup.cs`).

---

## 4. BỘ KIỂM THỬ TỰ ĐỘNG (VERIFICATION SUITE)
Để đảm bảo toàn vẹn code Core trước và sau mỗi thay đổi, chạy lệnh:
```bash
dotnet run --project Tests/PigTycoon.Runner.csproj
```
Kết quả kỳ vọng: **14/14 tests passed**. Chạy từ thư mục repo để `global.json` chọn SDK .NET 8 (SDK 10 trên máy này abort vì CET):
- GameClock: 16-min day, 4 day parts & sleep cooldown.
- Farm: Capacity bottleneck & soft density interpolation.
- Pig: 4 stages, Hư Thể seal & Xích Mao domestication.
- HerdManager: 7 conditions & 3 consecutive days for Sơ Khai.
- Economy: Official pricing formula matching GDD v6.0 section 4.5.
- Economy: Night market 18% gold slots & Bí nhân 0% gold.
- Defense: Building Hall requirement & Bạch Vân manual trigger.
- Grid & Modular Fence: 16-way neighbor bitmask & auto-connection logic.

---

## 5. QUY TẮC CỐT TỬ CẦN TUÂN THỦ (CRITICAL CONSTRAINTS)
1. **Không dùng ký tự Emoji Unicode trong TextMeshPro**: Font `LiberationSans SDF` không có glyph emoji, sẽ gây cảnh báo `u2764 replaced by square`. Luôn dùng text thuần ASCII/Việt hoặc sprite/icon.
2. **Không làm ô nhiễm tầng Core**: Thư mục `Assets/Scripts/Core/` phải tuyệt đối là Pure C#, không được `using UnityEngine;` để bộ test độc lập luôn chạy được trên mọi môi trường CI/CD.
3. **Giữ tương thích Scene & Runtime**: Mọi thành phần UI mới (như BackpackPopup, DevModePopup) đều phải có cơ chế `EnsureUIExists()` để tự động sinh đối tượng tại runtime khi người chơi nhấn Play, bất kể scene đã được tạo trước đó hay tạo mới.

---

## 6. HƯỚNG DẪN DÀNH CHO USER KHI MỞ CHAT MỚI TIẾT KIỆM TOKEN
Khi cuộc trò chuyện hiện tại quá dài, người dùng hãy:
1. Bấm tạo một **Cuộc hội thoại mới (New Chat)**.
2. Gõ prompt mở đầu cực ngắn:
   > *"Tôi đang phát triển dự án Pig Tycoon. Hãy đọc file `HANDOVER.md` tại `/home/danh/.gemini/antigravity/scratch/pig-tycoon-core/HANDOVER.md` để nắm toàn bộ bối cảnh dự án, sau đó tiếp tục công việc: [Mô tả tính năng hoặc việc bạn muốn làm tiếp theo]."*
3. AI ở chat mới sẽ chỉ tốn ~100 tokens để nhận lệnh, đọc file `HANDOVER.md` và ngay lập tức tiếp quản công việc trơn tru như người cũ, giúp bạn tiết kiệm đến 95% lượng token tiêu thụ!

---

## 7. CẬP NHẬT 2026-10-08 — NHÁNH `feat/B/P0.06-an-farm-main`

Người chơi trên Farm_Main là **An** (GDD 8.1: STR 17, AGI 23, CTRL 24, RES 18), không còn mặc định Khoa. Khoa vẫn tạo bằng `new CharacterData(PlayerId.Khoa)`.

Đi bộ 4,5 m/s không trừ thể lực. Chạy (Shift hoặc Dev) là 7 m/s và trừ 1,3 thể lực/giây. Heo đi 1,5 m/s, hoảng loạn 5 m/s. `GameClock.TickMinutes` đứng yên khi `IsDuringCombatWave`.

Art Bible bản 1: `Docs/art-bible/ART-BIBLE.md`. Chưa ép 16 PPU và chưa đổi lưới rào 1 m.
