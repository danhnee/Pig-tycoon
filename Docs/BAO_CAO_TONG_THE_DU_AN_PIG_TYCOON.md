# BÁO CÁO TỔNG THỂ TOÀN DIỆN DỰ ÁN PIG TYCOON 2D
## TỔNG KẾT TOÀN BỘ CÔNG VIỆC, TÍNH NĂNG & HỆ THỐNG ĐÃ PHÁT TRIỂN

---

* **Dự án:** Pig Tycoon 2D (Top-down Cozy Pig Farming, Survival & Defense)
* **Thời gian hoàn thành:** 20/10/2026
* **Trạng thái tổng thể:** Hoàn tất 100% các phân hệ — 19/19 Unit Tests tự động đạt chuẩn xanh
* **Công nghệ nền tảng:** Unity 2022/6000 LTS, Universal Render Pipeline (URP 2D), C# .NET 8, TextMeshPro, Input System

---

## MỤC LỤC
1. [Lời Mở Đầu & Mục Đích Báo Cáo](#1-lời-mở-đầu--mục-đích-báo-cáo)
2. [Phần I: Hệ Thống Cốt Lõi & Logic Nghiệp Vụ (Core Game Engine)](#phần-i-hệ-thống-cốt-lõi--logic-nghiệp-vụ-core-game-engine)
3. [Phần II: Hệ Thống Chiến Đấu, Vũ Khí & Quái Vật (Combat & Escalation)](#phần-ii-hệ-thống-chiến-đấu-vũ-khí--quái-vật-combat--escalation)
4. [Phần III: Đồ Họa, Hoạt Họa 2D & Hệ Thống Ánh Sáng (Art & Animation)](#phần-iii-đồ-họa-hoạt-họa-2d--hệ-thống-ánh-sáng-art--animation)
5. [Phần IV: Giao Diện Người Dùng Cozy Stardew Valley (Cozy UI & HUD)](#phần-iv-giao-diện-người-dùng-cozy-stardew-valley-cozy-ui--hud)
6. [Phần V: Thiết Kế Bản Đồ, Thế Giới Mở & Lối Mòn Chuyển Cảnh](#phần-v-thiết-kế-bản-đồ-thế-giới-mở--lối-mòn-chuyển-cảnh)
7. [Phần VI: Xây Dựng Bản Đồ & Kinh Tế Làng Bờ Rìa (Bờ Rìa Village)](#phần-vi-xây-dựng-bản-đồ--kinh-tế-làng-bờ-rìa-bờ-rìa-village)
8. [Phần VII: Tối Ưu Hóa Camera & Điều Khiển Tương Tác](#phần-vii-tối-ưu-hóa-camera--điều-khiển-tương-tác)
9. [Phần VIII: Kết Quả Kiểm Thử & Tính Bất Biến (19/19 Tests)](#phần-viii-kết-quả-kiểm-thử--tính-bất-biến-1919-tests)
10. [Phần IX: Danh Mục File Mã Nguồn Đã Thực Hiện](#phần-ix-danh-mục-file-mã-nguồn-đã-thực-hiện)

---

## 1. LỜI MỞ ĐẦU & MỤC ĐÍCH BÁO CÁO

Báo cáo này tổng hợp **toàn bộ quá trình nghiên cứu, thiết kế kiến trúc và hiện thực hóa mã nguồn** cho dự án **Pig Tycoon 2D**. Dự án không dừng lại ở việc tạo dựng bản đồ và ngôi làng, mà là một hệ sinh thái game hoàn chỉnh tích hợp giữa:
* Mô phỏng chăn nuôi chuyên sâu (Simulation & Genetics).
* Kinh tế kép thực tế và trao đổi vật phẩm (Dual Budget & Barter Economy).
* Chiến đấu hành động góc nhìn Top-down chuẩn mực (Stardew 1.6 Combat Mechanics).
* Thế giới mở 2 phân vùng (Nông trại thảo nguyên & Làng Bờ Rìa u ám).

Xuyên suốt báo cáo, **các khung đánh dấu vị trí chèn ảnh minh chứng** (`📸 [CHÈN ẢNH MINH CHỨNG ...]`) đã được thiết lập sẵn với mô tả chi tiết để phục vụ công tác nghiệm thu.

---

## PHẦN I: HỆ THỐNG CỐT LÕI & LOGIC NGHIỆP VỤ (CORE GAME ENGINE)

### 1.1. Hệ Thống Thời Gian Thực & Chu Kỳ 4 Buổi (`GameClock.cs`)
* **Thời lượng 1 ngày:** Chu kỳ chuẩn 16 phút đời thực (tương ứng 24 giờ trong game).
* **4 buổi sinh hoạt:**
  - *Buổi Sáng (06:00 - 12:00):* Cho ăn, dọn dẹp, kiểm tra sức khỏe đàn heo.
  - *Buổi Chiều (12:00 - 18:00):* Huấn luyện, mở rộng rào, giao thương chợ ngày.
  - *Buổi Tối (18:00 - 22:00):* Heo về chuồng, kiểm tra rào chắn, chuẩn bị phòng thủ.
  - *Ban Đêm (22:00 - 06:00):* Quái vật, thổ phỉ và Alien xuất hiện; chợ đêm mở cửa.
* **Cơ chế Cooldown Giấc Ngủ:** Quy định thời gian giãn cách ngủ để tránh lạm dụng tua nhanh thời gian (Time warp).

### 1.2. Mô Hình Đàn Heo & Di Truyền Học (`Pig.cs`, `HerdManager.cs`)
* **4 Giai đoạn tăng trưởng:** Heo Con (Piglet) $\rightarrow$ Hậu Bị (Juvenile) $\rightarrow$ Trưởng Thành (Adult) $\rightarrow$ Lão Hóa (Elder).
* **Hệ thống 6 dòng giống di truyền:**
  - *Hồng Điền:* Giống heo thịt phổ thông, dễ nuôi, thích nghi cao.
  - *Kim Thọ:* Dòng giống cao cấp, tăng trọng nhanh, giá trị thương phẩm cao.
  - *Xích Mao:* Giống heo rừng hung dữ, sức đề kháng vượt trội, cần cơ chế thuần hóa.
  - *Hắc Tuyến:* Dòng heo xương đen quý hiếm, phẩm chất thịt Hạng A.
  - *Bạch Vân:* Giống heo thần thánh, kích hoạt hào quang bảo vệ trang trại thủ công.
  - *Dị Biến (Mutant):* Heo nhiễm phóng xạ/dị thể Alien, người thường xa lánh, chỉ Bí Nhân nhận mua.
* **Điều kiện tấn phong cấp đàn (Sơ Khai):** Đòi hỏi thỏa mãn đồng thời 7 chỉ số (Cân nặng, Tâm trạng, Độ no, Nước uống, Vệ sinh, Thân thiết, Số lượng) duy trì liên tục trong 3 ngày.
* **Hàm nội suy mật độ mềm & Nút thắt sức chứa:** Tính toán áp lực chuồng trại ($D(t)$) khi số lượng vượt trần diện tích, tự động kích hoạt dịch bệnh hoặc giảm tăng trọng.

### 1.3. Mô Hình Kinh Tế Kép & Bảng Định Giá GDD v6.0 (`EconomyManager.cs`)
* **Mô hình Ngân sách Kép (Dual Budget Model):** Tách bạch giữa *Vốn Lưu Động* (Operating Cash) để mua cám, thuốc men và *Vốn Đầu Tư* (Investment Capital) để xây dựng công trình, mở rộng đất đai.
* **Độ chính xác tuyệt đối:** Tính toán tiền tệ với độ chính xác 1 Gold (1G), không làm tròn sai số.
* **Chợ Ngày vs Chợ Đêm vs Bí Nhân:**
  - *Chợ Ngày:* Mua bán cám gạo, xuất chuồng heo thương phẩm theo biểu giá thị trường.
  - *Chợ Đêm:* Mở vào ban đêm, 18% cơ hội xuất hiện các vật phẩm hiếm thanh toán bằng Vàng.
  - *Bí Nhân (Alien Barter):* Tuyệt đối không nhận tiền mặt (0% Vàng), chỉ chấp nhận trao đổi heo quý hiếm.

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 01 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Giao diện Sổ Tay Trang Trại / Dev Mode hiển thị thông số      │
│  Core Engine: Đồng hồ 4 buổi, thông số di truyền đàn heo (Hồng Điền,  │
│  Kim Thọ), chỉ số sức chứa chuồng và số dư ngân sách 1G.               │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## PHẦN II: HỆ THỐNG CHIẾN ĐẤU, VŨ KHÍ & QUÁI VẬT (COMBAT & ESCALATION)

### 2.1. Ba Archetype Vũ Khí & Cơ Chế Stardew 1.6 (`PlayerCombatController.cs`)
Hệ thống chiến đấu được thiết kế hướng dữ liệu (Data-driven) với 3 trường phái rõ rệt:

| Archetype Vũ Khí | Tên Vũ Khí Mẫu | Tầm Đánh | Cooldown | Hình Dáng Hitbox | Cơ Chế Knockback & Đặc Tính |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **Slash (Vung Chém)** | Đao Gỗ / Đao Sắt | $1.4m$ | $0.35s$ | Hình quạt $120^\circ$ | Quét rộng giải vây, đẩy lùi kẻ địch về phía trước. |
| **Smash (Đập Nện)** | Búa Tạ / Chùy Gỗ | $1.2m$ | $0.85s$ | Hình tròn $360^\circ$ ($1.6m$) | Đập lan mặt đất, choáng diện rộng, phá giáp hộ thể. |
| **Thrust (Đâm Thẳng)** | Giáo Nhọn / Chĩa | $2.2m$ | $0.45s$ | Tia hẹp thẳng dài | Xuyên thấu mục tiêu thẳng hàng, tầm xa an toàn. |

### 2.2. Thể Lực Hai Tầng P1.08 & Combo 3 Đòn
* **Hệ thống Stamina 2 tầng:**
  - *Tầng 1 (Active Stamina):* Tiêu hao khi vung vũ khí, cuốc đất, gánh nước.
  - *Tầng 2 (Exertion / Mệt dội):* Khi xả kiệt tầng 1, người chơi rơi vào trạng thái Mệt Dội, tốc độ di chuyển giảm 40%, không thể né đòn.
* **Combo 3 đòn liên hoàn:** Đòn 1 $\rightarrow$ Đòn 2 $\rightarrow$ Đòn 3 (Kích hoạt hiệu ứng kết liễu Crit $150\%$ và Knockback cực đại).
* **Độn Thổ / Lăn Né (Dodge Roll):** Nhấn phím cách lướt nhanh 3.5m với $0.25s$ khung bất tử (I-frames).

### 2.3. Ba Nhánh Kẻ Địch & Chuỗi Leo Thang Xâm Lược (Escalation Chains)
* **Kẻ Địch Cơ Bản:**
  - *Sơn Tặc (Thief):* Luồn lách vào chuồng cõng heo chạy trốn.
  - *Phá Rào (Breaker):* Cầm búa nện phá hàng rào mở đường cho bầy quái.
  - *Quái Dị (Pest):* Bò sát mặt đất cắn xé gia súc và lây lan mầm bệnh.
* **Chuỗi Leo Thang Sơn Tặc (Bandit Escalation):**
  - Trộm lẻ tẻ $\rightarrow$ Phục kích nhóm 3 tên $\rightarrow$ Đột kích bang hội $\rightarrow$ **Trùm Chúa Trộm Rìu Sắt (Boss Chúa Trộm)** mang theo lính công thành.
* **Chuỗi Leo Thang Alien (Alien Escalation):**
  - Đĩa bay UFO xuất hiện ban đêm chiếu luồng sáng (Tractor Beam) hút bắt heo $\rightarrow$ Rơi vãi Alien Grunt $\rightarrow$ Đổ bộ Alien Elite $\rightarrow$ **Tàu Mẹ & Trùm Chiến Tướng Alien (Boss Alien Warlord)**.

### 2.4. Đấu Trường Thử Nghiệm & Bù Nhìn Đo DPS (`CombatArenaSetup.cs`)
* Tách biệt hẳn một Scene chuyên dụng: `CombatTestArena2D.unity`.
* Trang bị hệ thống Bù Nhìn Tập Luyện 3 cấp độ:
  - *Bù Nhìn Rơm (Straw Dummy):* Đo lường DPS cơ bản.
  - *Bù Nhìn Sắt (Iron Dummy):* Kiểm tra giáp cản và phản lực.
  - *Bù Nhìn Thép (Steel Dummy):* Thử nghiệm độ bền vũ khí cực hạn.
* Bảng thống kê hiển thị thời gian thực: DPS trung bình, sát thương phát đơn lớn nhất, số hit combo.

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 02 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Scene đấu trường CombatTestArena2D. Nhân vật vung Đao Gỗ       │
│  chém Bù Nhìn Rơm, hiển thị hiệu ứng vệt chém (Slash VFX), số sát      │
│  thương nổi (Floating Damage Numbers) và thanh Thể lực 2 tầng.         │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 03 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Kẻ địch xuất hiện ban đêm (Sơn Tặc cõng heo hoặc Đĩa Bay UFO   │
│  chiếu luồng sáng Tractor Beam tím hút heo). Thấy rõ hiệu ứng cảnh báo │
│  nguy hiểm (Combat Telegraphs).                                        │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## PHẦN III: ĐỒ HỌA, HOẠT HỌA 2D & HỆ THỐNG ÁNH SÁNG (ART & ANIMATION)

### 3.1. Cấu Hình Universal Render Pipeline (URP 2D)
* Tạo lập tự động bộ Asset: `Universal2D_Renderer.asset` và `Universal2D_PipelineAsset.asset`.
* Cấu hình chế độ sắp xếp trục đứng Top-down: `TransparencySortMode.CustomAxis` với vector `(0, 1, 0)` đảm bảo chiều sâu thị giác (nhân vật đứng trước che khuất công trình phía sau một cách tự nhiên).

### 3.2. Chu Kỳ Ánh Sáng Ngày / Đêm 2D (`DayNight2DController.cs`)
* Tích hợp `Light2D` toàn cục (Global Light).
* Chuyển đổi mượt mà giữa các dải nhiệt độ màu:
  - *Bình minh:* Vàng hồng dịu nhẹ (`intensity 0.85`).
  - *Chính ngọ:* Vàng rực rỡ ấm áp (`intensity 1.1`).
  - *Hoàng hôn:* Cam đỏ sẫm (`intensity 0.75`).
  - *Đêm tối:* Xanh thẫm huyền bí (`intensity 0.25`), làm nổi bật các ngọn đuốc và đèn lồng.

### 3.3. Ma Trận Hoạt Họa Pixel Art Đầy Đủ (Animation Matrices)
Hệ thống đồ họa được nhập và cấu hình Sprite Sheet chuyên nghiệp:
* **Nhân vật Khoa:**
  - 4 hướng di chuyển: Xuống (Down), Lên (Up), Sang Phải (Right), Sang Trái (Left).
  - Các trạng thái: Đứng yên (Idle 4 frames), Đi bộ (Walk 4 frames), Hành động chém/đập (Action), Vui mừng vuốt ve thú nuôi (Happy), và biến thể đeo Balo (Backpack).
* **Đàn Heo:** Bộ hoạt họa hoàn chỉnh gồm Đứng thở (Idle), Bước đi lon ton (Walk 1, 2), Cúi đầu gặm máng (Eat), và Nằm ngủ phập phồng (Sleep) cho từng dòng giống.
* **Kẻ Địch & Trùm:** Hoạt họa Di chuyển (Walk), Tấn công (Attack), Bị thương (Hurt), và Chết tan biến (Die) cho Sơn Tặc, Phá Rào, Quái Dị và Trùm Alien.

### 3.4. Hệ Thống Hàng Rào Tự Động Nối 16 Hướng (`Modular Fence System`)
* Ứng dụng thuật toán mặt nạ bitmask 16 hướng lân cận:
  - Tự động nhận diện rào lân cận 4 hướng (Bắc, Nam, Đông, Tây).
  - Tự động chọn đúng Sprite: Rào ngang (`fence_h`), Rào dọc (`fence_v`), Ngã ba (`fence_t`), Ngã tư (`fence_cross`), Góc cua (`fence_corner`), và Cột chặn đầu cuối (`fence_end`).
  - Cơ chế hư hỏng: Hiển thị Sprite rào nứt vỡ (`fence_broken`, `fence_damaged_overlay`) khi bị quái vật tấn công.

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 04 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Ma trận Animation của nhân vật Khoa (Khoa Animation Matrix)    │
│  hoặc hình ảnh các đàn heo đang ăn, ngủ trong chuồng dưới ánh nắng     │
│  vàng hoàng hôn của hệ thống URP 2D.                                   │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 05 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Hệ thống Hàng rào chuồng heo tự động nối góc, chữ T và        │
│  ngã tư. Thấy rõ một đoạn rào bị nứt vỡ đang hiển thị hiệu ứng hư hại  │
│  kèm lượng máu HP rào.                                                 │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## PHẦN IV: GIAO DIỆN NGƯỜI DÙNG COZY STARDEW VALLEY (COZY UI & HUD)

### 4.1. Thanh HUD Đỉnh Màn Hình (`StardewHUDController.cs`)
* Hộp thông tin phong cách bảng gỗ mộc mạc:
  - Đồng hồ cơ học nhảy số, Icon thời tiết (Nắng ấm / Mưa phùn).
  - Tên buổi trong ngày (Sáng, Trưa, Chiều, Tối).
  - Hũ tiền vàng với hiệu ứng số nảy lách cách khi giao dịch.
  - Thanh Thể Lực 2 tầng (Stamina Fill Bar) đổi màu linh hoạt từ Xanh lá $\rightarrow$ Vàng hổ phách $\rightarrow$ Đỏ kiệt sức.

### 4.2. Thanh Công Cụ Hotbar 8 Ô (`HotbarController.cs`)
* Tích hợp 8 ô vật phẩm nhanh:
  `[1] Cám Hạt`, `[2] Xô Nước`, `[3] Búa Gỗ (Sửa/Tháo dỡ)`, `[4] Đao Gỗ (Chiến đấu)`, `[5] Hàng Rào`, `[6] Máng Ăn`, `[7] Máng Nước`, `[8] Thuốc Kháng Dịch`.
* Hỗ trợ phím số $1 \dots 8$, cuộn con lăn chuột, hoặc chạm trực tiếp trên màn hình cảm ứng để chuyển đổi dụng cụ tức thì.

### 4.3. Bảng Giám Định Heo Khung Gỗ Cuộn Giấy (`PigInspectPopup.cs`)
* Lấy cảm hứng từ bảng theo dõi gia súc của Stardew Valley:
  - Avatar cử động trực tiếp của chú heo được chọn.
  - Tên định danh, Dòng giống, Giới tính, Tuổi đời (Ngày).
  - Thước đo Thân Thiết: 5 Trái Tim (Heart Bar) tăng giảm theo mức độ chăm sóc.
  - Tâm trạng (Vui vẻ, Bình ổn, Cáu gắt, Hoảng loạn), Cân nặng chính xác đến $0.1\text{kg}$.
  - Phẩm chất thịt thương phẩm (Hạng C $\rightarrow$ B $\rightarrow$ A $\rightarrow$ S).
  - Danh sách đặc tính ẩn / đột biến gen.

### 4.4. Menu Nhà Phát Triển & Cheat Tool (`DevModePopup.cs`)
* Bấm phím `~` (tilde) hoặc `F1` để mở bảng điều khiển với 6 phân tab:
  - *Tab 1 (Chung):* Tua giờ, thêm 10,000G, hồi đầy thể lực.
  - *Tab 2 (Đàn Heo):* Thêm heo Hồng Điền/Kim Thọ, ép heo lớn nhanh, làm vui đàn heo.
  - *Tab 3 (Công Trình):* Làm đầy máng ăn/nước, sửa chữa toàn bộ hàng rào 100% HP.
  - *Tab 4 (Combat Test):* Trang bị vũ khí, dịch chuyển vào đấu trường Test Arena.
  - *Tab 5 (Spawner):* Triệu hồi quái vật (Sơn Tặc, Phá Rào, Alien, Trùm Rìu Sắt).
  - *Tab 6 (Làng Bờ Rìa):* Dịch chuyển nhanh xuống làng, mở trực tiếp Menu Barter từng dân làng.

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 06 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Giao diện Bảng Giám Định Heo Khung Gỗ (PigInspectPopup) khi    │
│  người chơi tương tác với heo Hồng Điền #1. Thấy rõ 5 trái tim thân    │
│  thiết, cân nặng, tâm trạng và phẩm chất thịt.                         │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 07 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Bảng menu Dev Mode Popup với 6 tab công cụ chẩn đoán, thanh    │
│  HUD Stardew trên đỉnh màn hình và Hotbar 8 ô vật phẩm bên dưới.      │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## PHẦN V: THIẾT KẾ BẢN ĐỒ, THẾ GIỚI MỞ & LỐI MÒN CHUYỂN CẢNH

### 5.1. Nông Trại Thảo Nguyên Bao La (`DefaultMapBuilder.cs`)
* **Diện tích:** $90m \times 64m$ ($X \in [-48 \dots +48], Y \in [-35 \dots +35]$).
* **Quy hoạch cảnh quan tự nhiên:**
  - *Hồ Nước Ốc Đảo Phía Đông:* $X \in [23 \dots 39], Y \in [-10 \dots 6]$ với bờ cỏ sậy dập dờn.
  - *Vạt Bùn Tắm Nằm Trong Chuồng:* $X \in [8 \dots 16], Y \in [-11 \dots -5]$ cho heo ngâm bùn.
  - *Hạ tầng chăn nuôi mặc định:* Giếng nước đá (`water_well`), Tháp trữ cám (`feed_silo`), Máng ăn máng nước, Khu xử lý ám khí (`corpse_lot`), Tháp canh gác (`defense_tower`).
* **Đường Biên Rừng Rậm 3 Lớp So Le:** Hàng trăm cây đại thụ so le kết hợp đá tảng và hoa dại bao bọc kín 4 hướng, chừa lối đi phía Nam.

### 5.2. Tái Cấu Trúc Lối Mòn Phía Nam & Cơ Chế Chuyển Cảnh Chủ Động
* **Khắc phục lỗi dịch chuyển cưỡng chế:** Kéo dài đường mòn đất đỏ từ $Y = -28$ xuống tận $Y = -31$; dời trigger `Trigger_Farm_To_Village` xuống điểm cuối tại tọa độ $(0f, -29.5f)$.
* **Bảng tương tác chủ động (Proactive Waypoint Prompt):**
  - Không tự ý dịch chuyển người chơi.
  - Hiển thị bảng nổi `[Đi Sang Làng Bờ Rìa] (Bấm E / Chạm)`.
  - Hỗ trợ phím `[E]`, `[Space]` hoặc click chuột/chạm nút màn hình để xác nhận.
* **Màn hình chuyển cảnh điện ảnh (Cinematic Diary Overlay):**
  - Fade-out màn hình đen $0.5s$.
  - Trích dẫn nhật ký sâu lắng của Khoa về cấm kỵ làng, sương mù, độ trễ thời gian.
  - Điểm hạ cánh tại Làng: $(104f, -16f)$ đối diện quảng trường, tránh lặp spawn.

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 08 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Toàn cảnh Nông trại thảo nguyên bao la nhìn từ góc rộng        │
│  (thấy rõ chuồng thả heo, giếng nước, kho cám, hồ nước và rừng rậm).   │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 09 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Nhân vật Khoa đứng tại cuối lối mòn Cổng Nam (y = -29.5) với   │
│  bảng tương tác nổi "[Đi Sang Làng Bờ Rìa] (Bấm E / Chạm)".            │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## PHẦN VI: XÂY DỰNG BẢN ĐỒ & KINH TẾ LÀNG BỜ RÌA (BỜ RÌA VILLAGE)

### 6.1. Bố Cục Địa Hình & Cảnh Quan Làng Bờ Rìa
* **Tọa độ địa lý:** $X \in [94 \dots 146], Y \in [-29 \dots -4]$ (cách xa nông trại 50m).
* **Mặt đất & Đường mòn u ám:** Đất xám tro mục nát, thảm hoa gai dại tàn lụi, các vũng bùn đất đỏ trơn trượt đầu dốc cổng làng và quanh bãi cân heo.
* **Cổng Làng & Lối về Nông Trại:** Tọa độ $(100f, -16f)$ với cột gỗ, rào gãy, ngọn đuốc mờ leo lét và biển chỉ dẫn `ĐƯỜNG VỀ TRANG TRẠI (Dốc Đất Đỏ Sương Mù)`.
* **Rừng cây u ám bao bọc (Perimeter):** Toàn bộ làng được quây kín bằng rừng cây đại thụ xám lạnh (`tree_prairie` tint `#525850`) ngăn người chơi bước ra ngoài khoảng không.

### 6.2. Năm Gian Hàng Dân Làng Theo Cốt Truyện & Luật Kiêng Kỵ
1. **Quán Bà Sáu (Bếp Ăn & Nước Chè Đầu Dốc) — `(108, -13)`:**
   - Mái tranh quán nước, bàn gỗ dài.
   - **Đạo cụ "Cái Bát Úp Dở" (Cấm kỵ #2):** Chiếc bát sứ men xanh úp ngược cố định trên mặt bàn (*"dành cho khách chưa tới theo gió đêm, chớ lật ngửa"*).
   - Ấm trà bốc khói, đèn lồng vàng, NPC Bà Sáu mang tạp dề.
   - Bán cháo nóng (+50 thể lực) và trà đắng (xóa mệt dội). Nhận Vàng ($140\%$) hoặc Barter.
2. **Xưởng Mộc Tám Vá (Vá Rào Chống Quái) — `(118, -10)`:**
   - Xưởng gỗ thông sẫm màu, các kiện gỗ xẻ chất đống bên tường.
   - Bù nhìn rơm đặt ngoài sân dùng làm bia thử nghiệm độ cứng của rào chắn.
   - NPC Tám Vá tay cầm búa đục. Đổi kiện gỗ xẻ (+300 HP rào) và cọc rào lõi sắt (**Cấm Vàng**, thuần Barter).
3. **Tiệm Thuốc Thú Y Cô Dung — `(110, -22)`:**
   - Tiệm thuốc thảo mộc rêu phong.
   - **Giếng đá rửa dược liệu** chuyên dụng bên hiên nhà phục vụ sắc thuốc.
   - Đá rêu, đèn lồng ánh xanh lá thảo mộc, NPC Cô Dung.
   - Cung cấp Thuốc bột kháng dịch đàn heo và Thuốc an thần (**Cấm Vàng**).
4. **Trạm Cân "6+1" (Anh Ca & Ông Tồn) — `(122, -18)`:**
   - Bãi quây rào gỗ nhốt heo ($4m \times 3m$) có máng ăn tạm.
   - **Bàn cân cơ học cấm kỵ (Cấm kỵ #1):** Thước đo vạch cân nhảy từ số `6` vọt sang số `8`, tuyệt đối không có số 7.
   - Bàn làm việc đặt cuốn **"Sổ Đen"** nợ nần của Ông Tồn.
   - Bộ đôi NPC Anh Ca (cân heo) và Ông Tồn (phân xử tranh chấp thương lái).
5. **Gốc Đa Ranh Giới (Bí Nhân 25m - Outskirts) — `(138, -25)`:**
   - Nằm tách biệt 25m về phía Đông Nam theo con đường mòn sương mù ngoằn ngoèo.
   - **Cây Đa Cổ Thụ khổng lồ (scale 2.8x)** mang sắc tím xám huyền bí rợn ngợp.
   - Cự thạch chạm khắc phù chú phong ấn.
   - **Luồng hào quang năng lượng dị biến (Tractor beam purple aura)** lập lòe phát sáng.
   - NPC Bí Nhân khoác áo choàng đen-tím. **Tuyệt đối không nhận vàng (0% Gold)**, chỉ thu mua heo Quý / Dị Biến ($5,000\text{G}$ GTTC) để đổi công nghệ ngoài hành tinh.

### 6.3. Bảng Hương Ước Làng Bờ Rìa — `(114, -14)`
* Đặt tại trung tâm ngã ba làng, bảng gỗ khắc sâu 3 điều răn sống còn:
  > *1. Cấm Kỵ Số 7 | 2. Bát Úp Chớ Lật | 3. Tuyệt Đối Không Nhìn Lên Trời*

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 10 TẠI ĐÂY]                  │
│                                                                        │
│  Mô tả: Toàn cảnh Làng Bờ Rìa với sắc thái sương lạnh u ám, mạng lưới  │
│  đường mòn đất nện, cổng làng và các gian nhà gỗ của dân làng.        │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 11 TẠI ĐÂY]                  │
│                                                                        │
│  Mô tả: Cận cảnh Quán Bà Sáu: Biển hiệu nổi "Quán Bà Sáu", bàn ăn gỗ, │
│  chiếc bát úp ngược, ấm trà và NPC Bà Sáu đứng trước quán.            │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 12 TẠI ĐÂY]                  │
│                                                                        │
│  Mô tả: Cận cảnh Trạm Cân "6+1": Bãi quây nhốt heo, thước cân nhảy từ  │
│  6 qua 8 bỏ số 7, bàn cuốn Sổ Đen và hai NPC Anh Ca, Ông Tồn.          │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 13 TẠI ĐÂY]                  │
│                                                                        │
│  Mô tả: Khu vực Gốc Đa Ranh Giới (x = 138, y = -25): Cây cổ thụ 2.8x  │
│  khổng lồ, luồng hào quang tím ma mị và thương nhân Bí Nhân.          │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## PHẦN VII: TỐI ƯU HÓA CAMERA & ĐIỀU KHIỂN TƯƠNG TÁC

### 7.1. Camera Kẹp Biên Đa Phân Vùng Động (`CameraFollow2D.cs`)
* **Khắc phục triệt để lỗi Camera kẹp cứng:**
  - Tự động nhận diện trạng thái `TransitionZoneController.Instance.IsInVillage`.
  - Khi ở Nông trại: Kẹp camera trong `FarmBounds` ($[-45 \dots 45, -32 \dots 32]$).
  - Khi ở Làng Bờ Rìa: Tự động chuyển vùng kẹp sang `VillageBounds` ($[96 \dots 144, -29 \dots -4]$).
  - Tích hợp hàm `SnapToTarget()` giúp Camera định vị ngay lập tức vào nhân vật trong khoảnh khắc màn hình đen, triệt tiêu hiện tượng camera trôi dạt 50m xuyên qua khoảng không.

### 7.2. Tương Tác Ngữ Cảnh Hợp Nhất (`PlayerInteractionController.cs`)
* Tích hợp thành phần `VillageInteractableTrigger`:
  - Nút bấm ngữ cảnh nổi trên màn hình cảm ứng tự động đổi nhãn: `[Gặp Quán Bà Sáu]`, `[Gặp Xưởng Mộc Tám Vá]`, `[Gặp Trạm Cân 6+1]`,...
  - Hỗ trợ đầy đủ phím tắt bàn phím: Bấm `[E]` hoặc `[Space]` để mở đối thoại ngay.
  - Hỗ trợ click chuột trực tiếp vào trạm/dân làng.
  - Mở trực tiếp cửa sổ giao dịch đổi chác [`VillageBarterPopup.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/UI/VillageBarterPopup.cs) với quy tắc không hoàn tiền thừa GTTC và từ chối heo Dị Biến.

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 14 TẠI ĐÂY]                  │
│                                                                        │
│  Mô tả: Cửa sổ giao diện Popup đổi chác (VillageBarterPopup) khi Khoa │
│  giao dịch với dân làng. Hiển thị danh mục mặt hàng, bảng tính GTTC   │
│  và phản ứng từ chối khi thử đưa heo Dị Biến vào giao dịch.          │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## PHẦN VIII: KẾT QUẢ KIỂM THỬ & TÍNH BẤT BIẾN (19/19 TESTS)

Toàn bộ hệ thống logic nghiệp vụ, toán học kinh tế, chiến đấu và quy tắc cốt truyện được xác thực bằng bộ Test Suite chạy tự động qua lệnh:
```bash
dotnet run --project Tests/PigTycoon.Runner.csproj
```

```
==================================================================
     PIG TYCOON (UNITY C# CORE) - INVARIANT VERIFICATION SUITE    
==================================================================

✔ [PASS] GameClock: 16-min day, 4 day parts & sleep cooldown
✔ [PASS] Farm: Capacity bottleneck & soft density interpolation
✔ [PASS] Pig: 4 stages, Hư Thể seal & Xích Mao domestication
✔ [PASS] HerdManager: 7 conditions & 3 consecutive days for Sơ Khai
✔ [PASS] Economy: Official pricing formula matching GDD v6.0 section 4.5
✔ [PASS] Economy: Night market 18% gold slots & Bí nhân 0% gold
✔ [PASS] Defense: Building Hall requirement & Bạch Vân manual trigger
✔ [PASS] Grid & Modular Fence: 16-way neighbor bitmask & auto-connection logic
✔ [PASS] Combat: Weapon archetypes (Slash, Smash, Thrust) & Data-Driven parameters
✔ [PASS] Combat: Enemy Archetypes (Thief, Breaker, Pest) & Damage Formulas
✔ [PASS] Combat: Stardew 1.6 Combat Mechanics & 64x64 Character Invariants
✔ [PASS] Combat: Training Dummy Resilience & Arena Analytics System
✔ [PASS] Combat: Loot System Themed Drops & Drop Rate Invariants
✔ [PASS] Combat: Pig Snatching, Abduction & Raid Telegraph Invariants
✔ [PASS] Combat: Alien Escalation Chain (UFO Failures -> Alien Drop 3 -> Alien Raid -> Mothership & Boss Warlord)
✔ [PASS] Combat: Bandit Escalation Chain (Thief Failures -> Ambush Drop 3 -> Bandit Raid -> Bandit Boss Siege & Outpost)
✔ [PASS] Combat: P1.08 Dual-Layer Stamina & 3-Hit Combo & Dodge Roll Invariants
✔ [PASS] Economy & Ledger: Day Market Feed/Pig Trading, 1G Precision & Dual Budget Model
✔ [PASS] Village Economy & Lore Barter: Bờ Rìa Villagers, Non-Refund Excess GTTC & Mutant Rejection

------------------------------------------------------------------
KẾT QUẢ: 19/19 tests passed (100% THÀNH CÔNG)
==================================================================
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 15 TẠI ĐÂY]                  │
│                                                                        │
│  Mô tả: Ảnh chụp màn hình cửa sổ Terminal / Console chạy lệnh          │
│  "dotnet run --project Tests/PigTycoon.Runner.csproj" với kết quả     │
│  19/19 tests passed (100% THÀNH CÔNG).                                │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## PHẦN IX: DANH MỤC FILE MÃ NGUỒN ĐÃ THỰC HIỆN

### 1. Phân Hệ Logic & Cốt Lõi (`Assets/Scripts/Core/`)
* [`GameEngine.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Core/GameEngine.cs): Trục điều phối trung tâm kết nối toàn bộ hệ thống thời gian, nông trại, nhân vật và kinh tế.
* [`GameClock.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Core/GameClock.cs): Đồng hồ 16 phút, 4 buổi và quản lý cooldown giấc ngủ.
* [`Pig.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Core/Pig.cs) & [`HerdManager.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Core/HerdManager.cs): Mô hình vòng đời 4 giai đoạn, di truyền gen, phẩm chất thịt và điều kiện nâng cấp cấp đàn.
* [`Farm.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Core/Farm.cs): Sức chứa đàn heo, áp lực ám khí và tính toán nội suy mật độ mềm.
* [`EconomyManager.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Core/EconomyManager.cs): Mô hình ngân sách kép, độ chính xác 1G, định giá heo sống theo GDD 4.5.
* [`CombatSystem.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Core/CombatSystem.cs): Quản lý dữ liệu vũ khí (Slash, Smash, Thrust), tính toán sát thương, độ bền và loot drop.
* [`DefenseManager.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Core/DefenseManager.cs): Quản lý công trình phòng thủ, tháp canh và cơ chế kích hoạt Bạch Vân.

### 2. Phân Hệ Điều Khiển & Trình Diễn (`Assets/Scripts/Controllers/`)
* [`DefaultMapBuilder.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/DefaultMapBuilder.cs): Trình xây dựng tự động toàn bộ bản đồ Thảo nguyên $90m \times 64m$, đường biên rừng 3 lớp, địa hình Làng Bờ Rìa và 5 trạm dân làng.
* [`TransitionZoneController.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/TransitionZoneController.cs): Quản lý chuyển cảnh Cổng Nam <-> Làng Bờ Rìa, waypoint prompt tương tác chủ động (bấm nút/E), hiệu ứng màn mờ, nhật ký Khoa và đổi màu sương lạnh.
* [`CameraFollow2D.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/CameraFollow2D.cs): Camera bám mượt mà, kẹp biên đa phân vùng động (`FarmBounds` vs `VillageBounds`), bổ sung hàm `SnapToTarget()`.
* [`PlayerInteractionController.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/PlayerInteractionController.cs): Nhận diện công trình/thú cưng/dân làng, xử lý nút ngữ cảnh nổi, phím `[E]` và phím `[Space]`.
* [`PlayerCombatController.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/PlayerCombatController.cs): Thực thi vung vũ khí, combo 3 đòn, vệt chém Slash VFX và lăn né bất tử.
* [`FarmEnvironment2D.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/FarmEnvironment2D.cs): Quản lý danh bạ hạ tầng, tự động nối rào 16 hướng bitmask, auto-tiling đường mòn.
* [`DayNight2DController.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/DayNight2DController.cs): Điều khiển ánh sáng Global Light 2D theo 4 buổi trong ngày.

### 3. Phân Hệ Giao Diện Người Dùng (`Assets/Scripts/UI/`)
* [`StardewHUDController.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/UI/StardewHUDController.cs): Thanh HUD gỗ Stardew, đồng hồ buổi, tiền vàng và thể lực 2 tầng.
* [`HotbarController.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/UI/HotbarController.cs): Thanh công cụ 8 slot với phím số $1 \dots 8$.
* [`PigInspectPopup.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/UI/PigInspectPopup.cs): Bảng giám định heo phong cách giấy cuộn cổ điển kèm đo tim thân thiết.
* [`VillageBarterPopup.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/UI/VillageBarterPopup.cs): Cửa sổ trao đổi hàng hóa với 5 dân làng Bờ Rìa, định giá heo sống và quy tắc trừ hao không thối lại tiền dư GTTC.
* [`DevModePopup.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/UI/DevModePopup.cs): Bảng điều khiển nhà phát triển với 6 tab chẩn đoán và phím tắt `~` / `F1`.
* [`BackpackPopup.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/UI/BackpackPopup.cs): Túi đồ người chơi dạng lưới lưới pixel art.

### 4. Phân Hệ Công Cụ Biên Tập & Kiểm Thử (`Assets/Editor/` & `Tests/`)
* [`Universal2DSetup.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Editor/Universal2DSetup.cs): Công cụ tự động cấu hình URP 2D, sinh bản đồ mẫu MainFarm2D và nạp toàn bộ Làng Bờ Rìa chỉ bằng 1 cú nhấp chuột trong Unity Editor.
* [`CombatArenaSetup.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Editor/CombatArenaSetup.cs): Tự động khởi tạo Scene kiểm thử chiến đấu `CombatTestArena2D.unity` với hệ thống bù nhìn rơm/sắt/thép.
* [`TilePaletteBuilder.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Editor/TilePaletteBuilder.cs): Tự động tạo Palette gạch vẽ map thủ công trong Unity Tile Palette window.
* [`Program.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Tests/Program.cs): Bộ kiểm thử tự động gồm 19 kịch bản xác thực toàn bộ các ràng buộc nghiệp vụ của dự án.

---

*Báo cáo kỹ thuật tổng thể được lập tự động dựa trên toàn bộ cây mã nguồn dự án Pig Tycoon 2D.*
