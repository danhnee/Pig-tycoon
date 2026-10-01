# Pig Tycoon - Prototype P0 (Core Foundation & Data Models)

Bản cài đặt Prototype P0 dựa trên tài liệu **Game Design Document v6.0 (Bản Hợp Nhất Chính Thức)**.

---

## 1. Cấu trúc thư mục

```
pig-tycoon-core/
├── package.json               # Cấu hình ES modules, scripts chạy với Node 24 native type-stripping
├── README.md
├── src/
│   ├── types/
│   │   ├── enums.ts           # 12 dòng gen, 10 thời tiết, 4 buổi, 4 bậc bầy đàn, 8 chủ đề chợ...
│   │   ├── pig.ts             # Cấu trúc thực thể Heo, tính chất Dị Biến, nhóm bệnh
│   │   ├── farm.ts            # Hạ tầng trại, báo cáo điểm nghẽn sức chứa, SC, ÁLSK
│   │   ├── herd.ts            # 7 điều kiện Bầy Đàn, Alpha Leader, Kế Vị, Truyền Thừa
│   │   ├── market.ts          # Vật phẩm, GTTC, Chợ ngày, Chợ đêm trao đổi, Bí nhân
│   │   ├── defense.ts         # Bản vẽ, Phòng Xây Dựng, độ bền, nạp trước, Bạch Vân
│   │   ├── character.ts       # Chỉ số gốc/thưởng, Thể lực 2 lớp (Tải ngày), 6 ô trang bị / 3 cặp võ kỹ
│   │   └── save.ts            # Schema lưu trữ toàn vẹn trạng thái v6.0
│   ├── models/
│   │   ├── GameClock.ts       # Đồng hồ 16 phút/ngày, 4 buổi, ngủ chuẩn/cooldown 6h, bộ gieo chu kỳ
│   │   ├── Farm.ts            # Công thức sức chứa gốc & hiệu dụng, nội suy mềm mật độ (0.85 -> 2.20)
│   │   ├── Pig.ts             # Vòng đời 4 giai đoạn, phong ấn Hư Thể, thuần hóa Xích Mao, Neo Tinh Thần
│   │   ├── HerdManager.ts     # Quản lý 7 điều kiện Bầy Đàn, bầu thủ lĩnh, Bồi Dưỡng Kế Vị, Truyền Thừa
│   │   ├── EconomyManager.ts  # Công thức giá heo thịt/giống, kiểm dịch 60G, 18% ô vàng, Bí nhân 0% vàng
│   │   └── DefenseManager.ts  # Xây dựng theo bản vẽ + cấp phòng, nạp đạn trước đợt, kích hoạt Bạch Vân
│   ├── engine/
│   │   ├── GameEngine.ts      # Bộ điều phối trung tâm, vòng lặp ngày/đêm, kiểm thử bất biến
│   │   └── SaveLoad.ts        # Serialization / Deserialization JSON toàn vẹn dữ liệu
│   ├── cli/
│   │   └── simulate.ts        # Kịch bản mô phỏng tương tác CLI & Dashboard trực quan
│   ├── tests/
│   │   ├── clock.test.ts      # Kiểm thử thời gian, 4 buổi, cooldown ngủ, gieo chu kỳ
│   │   ├── farm_capacity.test.ts # Kiểm thử điểm nghẽn, nội suy mật độ mềm, bậc Sinh Cảnh
│   │   ├── herd.test.ts       # Kiểm thử 7 điều kiện Bầy Đàn (SC >= 50, Kim Thọ = 3, 3 ngày liên tục)
│   │   ├── economy.test.ts    # Kiểm thử công thức giá heo chuẩn mục 4.5, 18% ô vàng, Bí nhân
│   │   └── save_load.test.ts  # Kiểm thử Roundtrip Save/Load bảo toàn dữ liệu
│   └── index.ts               # Entrypoint xuất khẩu các module
```

---

## 2. Các quy định thiết kế đã được hiện thực hóa (Design Locks)

1. **Đồng hồ & Chu kỳ**:
   - 1 ngày = 16 phút thực (960s), 1 giờ = 40s. 4 buổi: Sáng (06-10), Trưa (10-14), Chiều (14-18), Tối (18-06).
   - Ngủ chuẩn tua đến 06:00 sáng, xóa Tải ngày, cooldown 6 giờ game sau khi thức dậy.
   - Chu kỳ gieo đợt lớn: 6–8 ngày (đầu), 7–10 ngày (giữa), 8–12 ngày (cuối); 8% cơ hội "Mùa Kinh Tế Vàng" (12–15 ngày).
2. **Sức chứa & Mật độ**:
   - `Sức chứa gốc = MIN(Ô Đất * 8 ; Mái trú * 6 ; Bồn nước * 12 ; Máng ăn * 10)`
   - `Sức chứa hiệu dụng = MAX(1 ; Sức chứa gốc - Xác ngoài Khu Xử Lý)`
   - Nội suy mềm mật độ: `<=70% (x0.85)`, `100% (x1.00)`, `120% (x1.25)`, `150% (x1.65)`, `180%+ (x2.20)`.
3. **Trạng thái Bầy Đàn & Kế thừa**:
   - Đủ đồng thời 7 điều kiện liên tục trong 3 ngày mới thành lập Bầy Đàn Sơ Khai.
   - Heo Kim Thọ trưởng thành/già tính bằng 3 con gắn kết, hạ ngưỡng Tinh thần đàn từ 60 xuống 55.
   - Thủ lĩnh chỉ sinh ra từ Bầy Đàn; sở hữu hào quang **Neo Tinh Thần** (ngưỡng 25, +5 mỗi tầng Truyền Thừa).
   - Cơ chế kế thừa có tỉ lệ (Thường 5%, Kim Thọ 20%) kết hợp **Bồi Dưỡng Kế Vị** (tối đa +30%) và tích lũy tối đa 3 tầng **Truyền Thừa**.
4. **Hệ Gen & Tính chất đặc thù**:
   - Dòng Hư Thể có tính chất xấu ở trạng thái Ngủ; nếu qua hết giai đoạn Đang Lớn mà giữ sạch thì **phong ấn vĩnh viễn**.
   - Dòng Xích Mao có nghiên cứu thuần hóa, mở khóa thịt +1 bậc và tăng trọng +8%.
5. **Kinh tế & Chợ**:
   - Công thức giá heo thịt: `Cân nặng * 3G * Hạng thịt * Hệ số giai đoạn * Hệ số giống * Trạng thái chợ * Xu hướng chu kỳ`.
   - Phí kiểm dịch 60G; heo không kiểm dịch bị giảm tỉ lệ bán thành công.
   - Chợ đêm: Trao đổi theo món người bán yêu cầu; chỉ đúng 18% ô hàng chấp nhận Vàng (giá x1.4 GTTC).
   - Bí nhân: 0% nhận Vàng, xuất hiện 2 giờ ngẫu nhiên trong đêm sát ranh giới trại, tỉ giá 130–200% GTTC, tăng ÁLSK.
6. **Phòng thủ & Công trình**:
   - Bắt buộc có Bản vẽ + Phòng Xây Dựng đủ cấp.
   - Nạp trước đạn/nhiên liệu; cấm nạp đạn trong đợt quái.
   - Công trình Bạch Vân kích hoạt thủ công.
7. **Lưu/Tải**:
   - Toàn bộ trạng thái thế giới được lưu và nạp lại một cách xác định (deterministic) qua JSON.

---

## 3. Cách chạy

Yêu cầu: Node.js >= v22.6 (Hỗ trợ native `--experimental-strip-types`, không cần cài thêm dependency).

```bash
# Chạy mô phỏng CLI tương tác
npm start

# Chạy toàn bộ 12 unit test
npm test
```
