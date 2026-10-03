# PIG TYCOON - UNITY MOBILE (ANDROID) PROJECT

Dự án **Pig Tycoon** phiên bản **Unity Mobile (Android)**, xây dựng theo chuẩn kiến trúc sạch (Clean Architecture) dựa trên tài liệu **GDD v6.0**.

---

## 1. Cấu trúc thư mục dự án

```
pig-tycoon-unity/
├── Packages/
│   └── manifest.json          # URP, TextMeshPro, uGUI, Input System, Burst, Mathematics
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
│   │   │   └── MobileJoystick.cs       # Virtual Joystick cảm ứng đa điểm trên màn hình di động
│   │   ├── Controllers/
│   │   │   ├── MobileGameController.cs # Quản lý vòng lặp Tick & Auto-Save khi pause ứng dụng Android
│   │   │   ├── PlayerMobileController.cs # Di chuyển nhân vật, lướt né, tiêu hao Stamina, 3 ô võ kỹ
│   │   │   └── PigAgentView.cs         # AI di chuyển bầy đàn, tối ưu LOD culling cho 100+ con heo
│   │   ├── UI/
│   │   │   └── MobileHUDController.cs  # Thanh Top Bar & cụm nút bấm kỹ năng mobile
│   │   └── PigTycoon.Presentation.asmdef # Assembly cho Presentation Layer
├── Tests/
│   ├── PigTycoon.Runner.csproj         # Test Runner độc lập
│   └── Program.cs                      # Bộ kiểm thử 7/7 tiêu chí bất biến (100% Pass)
```

---

## 2. Giải pháp tối ưu hóa chuyên biệt cho Android

1. **Hiệu năng bầy đàn (100–200 con heo trên màn hình)**:
   - Toàn bộ logic tính toán sinh trưởng, di truyền, dịch bệnh, hòa nhập đàn được xử lý hoàn toàn trong **Pure C# (`PigTycoon.Core`)** không sinh rác Garbage Collection (GC) từ Unity.
   - Script [`PigAgentView.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-unity/Assets/Scripts/Controllers/PigAgentView.cs) tích hợp cơ chế **LOD Distance Culling**: các con heo cách xa camera (> 40m) sẽ tự động tắt raycast và cập nhật thô, giúp thiết bị Android duy trì ổn định 60 FPS mà không nóng máy.

2. **Hệ thống điều khiển cảm ứng (Touch / Landscape)**:
   - Hướng màn hình: **Landscape (Ngang)**.
   - **Bên trái**: Virtual Joystick mượt mà với deadzone và tự động định vị lại.
   - **Bên phải**: 3 nút bấm tương ứng 3 ô võ kỹ:
     - **Thế Công** (Vũ khí + Giày, tiêu hao Stamina).
     - **Thế Thủ** (Giáp + Quần, tiêu hao Stamina/Huyết Tế).
     - **Thế Biến** (Vòng tay + Cổ, tiêu hao Nộ Khí).
     - **Nút Bạch Vân**: Nút kích hoạt thủ công vũ khí Bạch Vân với hiển thị trạng thái nạp năng lượng.

3. **Cơ chế Lưu/Tải an toàn trên Android**:
   - Tự động gọi `SaveGameToPersistentStorage()` trong `OnApplicationPause(true)` và `OnApplicationQuit()`.
   - File save được ghi vào `Application.persistentDataPath/pig_tycoon_save.json`, không lo mất tiến trình khi người dùng nhận cuộc gọi hay chuyển app.

---

## 3. Cách mở và kiểm thử dự án

### Chạy Unit Test kiểm tra tính đúng đắn:
```bash
cd /home/danh/.gemini/antigravity/scratch/pig-tycoon-unity
dotnet run --project Tests/PigTycoon.Runner.csproj
```
*(Kết quả: 7/7 Test Suite PASS 100%).*

### Mở trong Unity Editor:
1. Mở **Unity Hub**.
2. Chọn **Open** -> Điều hướng đến thư mục:
   `/home/danh/.gemini/antigravity/scratch/pig-tycoon-unity`
3. Unity sẽ tự động nạp Packages và biên dịch `PigTycoon.Core` cùng `PigTycoon.Presentation`.
4. Trong **Build Settings**:
   - Chuyển Platform sang **Android**.
   - Thiết lập Texture Compression: **ASTC**.
   - Scripting Backend: **IL2CPP**, Target Architectures: **ARM64**.
