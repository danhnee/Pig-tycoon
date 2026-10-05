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

## 2. KIẾN TRÚC MÃ NGUỒN (ARCHITECTURE INVARIANTS)
Dự án được xây dựng theo nguyên tắc phân tầng nghiêm ngặt:
```
pig-tycoon-core/
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
│       └── MainFarm2D.unity                 # Scene chính hoàn chỉnh của game
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

2. **Cơ Chế Rào Chuồng 1x1 & Đặt/Dỡ Rào Theo Vị Trí Chỉ Định**:
   - Tất cả rào đều là cọc đơn 1m x 1m độc lập (`SubdivideMonolithicFences()`).
   - Cầm Búa gỗ click vào cọc rào trong tầm $\le 3.8\text{m}$ $\to$ Tháo dỡ đúng 1 cọc đó, thu hồi +1 Cọc Gỗ, -5 Stamina.
   - Cầm Búa gỗ click vào mặt đất trống trong tầm $\le 3.8\text{m}$ $\to$ Cắm 1 cọc rào mới, -1 Cọc Gỗ, -5 Stamina.

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
Kết quả kỳ vọng: **7/7 tests passed (100% THÀNH CÔNG)**:
- GameClock: 16-min day, 4 day parts & sleep cooldown.
- Farm: Capacity bottleneck & soft density interpolation.
- Pig: 4 stages, Hư Thể seal & Xích Mao domestication.
- HerdManager: 7 conditions & 3 consecutive days for Sơ Khai.
- Economy: Official pricing formula matching GDD v6.0 section 4.5.
- Economy: Night market 18% gold slots & Bí nhân 0% gold.
- Defense: Building Hall requirement & Bạch Vân manual trigger.

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
