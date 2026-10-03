# PIG TYCOON - UNITY UNIVERSAL 2D (ANDROID MOBILE)

Dự án **Pig Tycoon** phiên bản **Universal 2D (URP 2D)** chạy trên **Android Mobile**, xây dựng theo chuẩn kiến trúc sạch (Clean Architecture) dựa trên tài liệu **GDD v6.0**.

---

## 1. Điểm đặc trưng của phiên bản Universal 2D (URP 2D)

1. **Góc nhìn Top-down 2D & Xếp lớp chiều sâu (Y-Sorting)**:
   - Sử dụng `SpriteRenderer` kết hợp sắp xếp độ sâu động theo trục Y:
     `SpriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);`
   - Nhân vật, đàn heo và công trình đứng ở dưới màn hình sẽ tự động che phủ tự nhiên các đối tượng đứng phía sau.

2. **Hệ thống Chiếu sáng 2D (Light2D) theo 4 Buổi trong ngày**:
   - Tích hợp [`DayNight2DController.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-unity/Assets/Scripts/Controllers/DayNight2DController.cs) điều khiển **Global Light 2D**:
     - **Sáng (06:00 - 10:00)**: Ánh bình minh dịu ấm (Dawn Color).
     - **Trưa (10:00 - 14:00)**: Nắng trắng chói chang (Noon Color, cường độ 1.15).
     - **Chiều (14:00 - 18:00)**: Ánh hoàng hôn vàng cam (Dusk Color).
     - **Tối (18:00 - 06:00)**: Màn đêm xanh thẫm huyền bí (Night Color, cường độ 0.35, giảm 15% tầm nhìn).
   - **Đèn Lồng Xanh của Bí Nhân**: Khi Bí nhân xuất hiện trong 2 giờ đêm sát ranh giới trại, `Point Light 2D` màu xanh lục sẽ tự động phát sáng báo hiệu!
   - **Đèn Pha Công Nghiệp (Loại 2)**: Chiếu luồng sáng hình nón 25m trong đêm để phá tàng hình và xóa phạt tầm nhìn.

3. **Vật lý 2D Top-down & Điều khiển cảm ứng Mobile**:
   - Sử dụng `Rigidbody2D` (gravityScale = 0) kết hợp `MobileJoystick` (Virtual Joystick màn hình ngang).
   - Di chuyển mượt mà trên mặt phẳng $XY$, tự động lật `SpriteRenderer.flipX` theo hướng trái/phải.
   - Quản lý tiêu hao Stamina khi chạy (1.3 STA/giây theo GDD mục 9.2).

4. **Tối ưu hóa Bầy Đàn (100–200 con heo trên thiết bị Android)**:
   - Script [`PigAgentView.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-unity/Assets/Scripts/Controllers/PigAgentView.cs) tối ưu hóa khoảng cách (LOD Culling): các con heo cách xa camera ($> 35\text{ m}$) sẽ bỏ qua tính toán chi tiết, đảm bảo máy Android không bị tụt FPS hay nóng pin.

---

## 2. Cấu trúc thư mục

```
pig-tycoon-unity/
├── Packages/
│   └── manifest.json          # URP (2D Renderer), TextMeshPro, uGUI, Input System, Burst
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              # PURE C# DOMAIN LAYER (Không phụ thuộc UnityEngine)
│   │   │   ├── PigTycoon.Core.asmdef   (noEngineReferences: true)
│   │   │   ├── PigTycoon.Core.csproj   (Target: netstandard2.1 / C# 9.0)
│   │   │   ├── Enums.cs                (12 dòng gen, 10 thời tiết, 4 buổi, chợ, bầy đàn)
│   │   │   ├── GameClock.cs            (16 phút/ngày, 4 buổi, ngủ 6h cooldown, chu kỳ)
│   │   │   ├── Farm.cs                 (Sức chứa gốc/hiệu dụng, nội suy mật độ 0.85 -> 2.20, SC, ÁLSK)
│   │   │   ├── Pig.cs                  (Vòng đời 4 giai đoạn, phong ấn Hư Thể, thuần Xích Mao, Neo Tinh Thần)
│   │   │   ├── HerdManager.cs          (7 điều kiện Bầy Đàn, Alpha Leader, Kế Vị, Truyền Thừa)
│   │   │   ├── EconomyManager.cs       (Công thức giá mục 4.5, Chợ đêm trao đổi + 18% ô vàng, Bí nhân)
│   │   │   ├── DefenseManager.cs       (Phòng Xây Dựng 1-3, nạp đạn trước, kích hoạt Bạch Vân thủ công)
│   │   │   ├── Character.cs            (Chỉ số gốc/thưởng, 6 ô trang bị / 3 cặp võ kỹ, Stamina 2 lớp)
│   │   │   └── GameEngine.cs           (Bộ điều phối trung tâm)
│   │   ├── Input/
│   │   │   └── MobileJoystick.cs       # Virtual Joystick cảm ứng đa điểm 2D
│   │   ├── Controllers/
│   │   │   ├── MobileGameController.cs # Quản lý vòng lặp Tick & Auto-Save Android
│   │   │   ├── PlayerMobileController.cs # Di chuyển Top-down 2D, lật sprite, tiêu hao Stamina, 3 ô võ kỹ
│   │   │   ├── PigAgentView.cs         # AI di chuyển bầy đàn 2D, bám thủ lĩnh, LOD culling
│   │   │   ├── DayNight2DController.cs # Hiệu ứng ánh sáng Light2D URP 4 buổi & đèn lồng Bí nhân
│   │   │   └── DefenseTower2DView.cs   # Tháp thủ 2D tự động xoay và bắn quái
│   │   ├── UI/
│   │   │   └── MobileHUDController.cs  # Thanh Top Bar & cụm nút bấm võ kỹ + Bạch Vân
│   │   └── PigTycoon.Presentation.asmdef # Assembly cho Presentation Layer (kèm URP Runtime)
├── Tests/
│   ├── PigTycoon.Runner.csproj         # Test Runner độc lập
│   └── Program.cs                      # Bộ kiểm thử 7/7 tiêu chí bất biến (100% Pass)
```

---

## 3. Kiểm thử & Chạy dự án

### Chạy Unit Test kiểm tra tính đúng đắn:
```bash
cd /home/danh/.gemini/antigravity/scratch/pig-tycoon-unity
dotnet run --project Tests/PigTycoon.Runner.csproj
```
*(Kết quả: 7/7 Test Suite PASS 100%).*

### Mở trong Unity Editor:
1. Mở **Unity Hub** (khuyên dùng Unity 2022.3 LTS hoặc Unity 6).
2. Nhấn nút **Add** -> chọn thư mục:
   `/home/danh/.gemini/antigravity/scratch/pig-tycoon-unity`
3. Trong **Project Settings -> Graphics / Quality**:
   - Chọn Pipeline Asset: **URP 2D Asset** (Universal Renderer Pipeline với 2D Renderer).
   - Transparency Sort Mode: **Custom Axis (X: 0, Y: 1, Z: 0)** để hỗ trợ Y-sorting tự động hoàn hảo.
4. Trong **Build Settings**:
   - Chuyển Platform sang **Android**.
   - Texture Compression: **ASTC**.
   - Scripting Backend: **IL2CPP**, Target Architectures: **ARM64**.
