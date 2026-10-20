# BÁO CÁO TỔNG KẾT KỸ THUẬT & TIẾN ĐỘ NHIỆM VỤ
## THIẾT KẾ BẢN ĐỒ NÔNG TRẠI THẢO NGUYÊN, CƠ CHẾ CHUYỂN CẢNH & XÂY DỰNG LÀNG BỜ RÌA

---

* **Dự án:** Pig Tycoon 2D (Top-down Cozy Farm & Survival)
* **Thời gian hoàn thành:** 20/10/2026
* **Trạng thái:** Hoàn tất 100% — Vượt qua 19/19 Unit Tests
* **Các module phụ trách:** Level Design 2D, Procedural Tilemap, Transition Zone, Village Stations & Lore Barter

---

## MỤC LỤC
1. [Tổng Quan Phạm Vi Nhiệm Vụ](#1-tổng-quan-phạm-vi-nhiệm-vụ)
2. [Chi Tiết Nhiệm Vụ 1: Tái Thiết Kế Bản Đồ Thảo Nguyên Nông Trại](#2-nhiệm-vụ-1-tái-thiết-kế-bản-đồ-thảo-nguyên-nông-trại)
3. [Chi Tiết Nhiệm Vụ 2: Tái Cấu Trúc Hệ Thống Chuyển Cảnh & Lối Mòn Phía Nam](#3-nhiệm-vụ-2-tái-cấu-trúc-hệ-thống-chuyển-cảnh--lối-mòn-phía-nam)
4. [Chi Tiết Nhiệm Vụ 3: Thiết Kế & Xây Dựng Bản Đồ Làng Bờ Rìa Cơ Bản](#4-nhiệm-vụ-3-thiết-kế--xây-dựng-bản-đồ-làng-bờ-rìa-cơ-bản)
5. [Chi Tiết Nhiệm Vụ 4: Tối Ưu Hóa Camera & Cơ Chế Điều Khiển Tương Tác](#5-nhiệm-vụ-4-tối-ưu-hóa-camera--cơ-chế-điều-khiển-tương-tác)
6. [Kết Quả Kiểm Thử & Tính Bất Biến (Verification)](#6-kết-quả-kiểm-thử--tính-bất-biến-verification)
7. [Danh Mục File Mã Nguồn & Tài Nguyên Đã Thực Hiện](#7-danh-mục-file-mã-nguồn--tài-nguyên-đã-thực-hiện)

---

## 1. TỔNG QUAN PHẠM VI NHIỆM VỤ

Chuỗi nhiệm vụ tập trung vào việc chuyển đổi toàn bộ trải nghiệm không gian đầu game, từ một bãi thả heo đơn lẻ thành một thế giới mở 2D liền mạch gồm 2 phân vùng lớn:
1. **Nông Trại Thảo Nguyên Mặc Định (Prairie Farmstead):** Vùng đồng cỏ bao la 90m x 64m được bao bọc bởi rừng rậm nhiều lớp, quy hoạch bãi thả heo trung tâm và đầy đủ các công trình chăn nuôi sinh tồn.
2. **Lối Mòn & Điểm Chuyển Cảnh Phía Nam (South Forest Trail & Proactive Transition Zone):** Xóa bỏ cơ chế cưỡng chế dịch chuyển đột ngột; thay bằng lối đi xuyên rừng tự do và biển chỉ dẫn có nút kích hoạt chủ động.
3. **Làng Bờ Rìa (Bờ Rìa Village):** Một vùng đất giáp ranh u ám, bí ẩn mang đậm không khí sương mù và cốt truyện với 5 trạm dân làng tuân thủ nghiêm ngặt các luật kiêng kỵ (Cấm kỵ số 7, Cái bát úp dở, Kinh tế Barter thuần vật phẩm).

---

## 2. NHIỆM VỤ 1: TÁI THIẾT KẾ BẢN ĐỒ THẢO NGUYÊN NÔNG TRẠI

### 2.1. Địa Hình & Thuật Toán Phủ Cỏ Perlin Noise Đa Tầng
* **Quy mô bản đồ:** Mở rộng từ $36m \times 24m$ lên **$90m \times 64m$** ($X \in [-48 \dots +48], Y \in [-35 \dots +35]$).
* **Mặt đất tự nhiên đa tầng:** Ứng dụng hệ thống hàm nhiễu Perlin nhiều tần số (`baseNoise 0.085f`, `detailNoise 0.22f`, `flowerNoise 0.14f`) tạo ra sự chuyển tiếp tự nhiên giữa cỏ non mượt mà (`grass_steppe`), thảm cỏ cao hoang dã sát bìa rừng (`grass_tall`), và các vạt hoa dại uốn lượn (`grass_flower_1, 2, 3`).
* **Hồ Nước Ốc Đảo Phía Đông (Prairie Oasis Lake):** Tọa độ $X \in [23 \dots 39], Y \in [-10 \dots 6]$ với bờ cỏ lau sậy, thảm hoa vàng ven hồ và hệ thống va chạm nước `TilemapCollider2D`.
* **Bãi Bùn Tắm Tự Nhiên (Mud Pit):** Tọa độ $X \in [8 \dots 16], Y \in [-11 \dots -5]$ nằm trọn trong chuồng thả, phục vụ hành vi tắm bùn hạ nhiệt cho đàn heo.

### 2.2. Đường Biên Rừng Rậm 3 Lớp So Le (Dense Forest Perimeter)
* Thiết lập 3 hàng cây đại thụ so le (`tree_prairie`, bước nhảy $3.6m$) kết hợp tảng đá rêu (`rock_boulder`) và bụi hoa dại bao bọc 4 hướng bản đồ:
  - **Rìa Bắc & Tây & Đông:** Hàng rào cây khép kín, cản trở di chuyển ra khoảng trống vô tận.
  - **Rìa Nam:** Chừa khoảng hở rộng $7.6m$ ($|X| < 3.8m$) cho con đường mòn đất đỏ xuyên rừng đi xuống Làng Bờ Rìa.
* **Cổng Rào Chăn Thả:** Chuồng thả heo $40m \times 28m$ được bao bọc bởi hàng rào kết nối tự động (`Pasture_Fences`). Cổng Nam tích hợp vật cản vô hình `PigGateBarrier` (chỉ chặn heo vượt ngục, cho phép người chơi đi qua tự do).

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 01 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Toàn cảnh Nông trại thảo nguyên nhìn từ trên cao trong        │
│  Unity Scene / Game View. Thấy rõ bãi thả heo trung tâm, hồ nước       │
│  phía Đông, bãi bùn tắm và các hàng cây rậm rạp bao quanh 4 rìa map.   │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. NHIỆM VỤ 2: TÁI CẤU TRÚC HỆ THỐNG CHUYỂN CẢNH & LỐI MÒN PHÍA NAM

### 3.1. Vấn Đề Trước Khi Sửa & Giải Pháp Kỹ Thuật
* **Bất cập cũ:** Trigger chuyển cảnh đặt tại $Y = -25f$, trong khi đường mòn kéo dài tới $Y = -28f$. Khi người chơi vừa bước qua cổng chuồng heo thì đã bị dịch chuyển cưỡng chế ngay lập tức, không thể đi hết con đường mòn hay khám phá rừng cây.
* **Giải pháp khắc phục:**
  1. Kéo dài đường mòn đất đỏ trên `Tilemap_Paths` từ $Y = -28$ xuống tận đáy rừng $Y = -31$.
  2. Dời điểm kích hoạt chuyển cảnh `Trigger_Farm_To_Village` xuống đúng điểm cuối của lối mòn tại tọa độ $(0f, -29.5f)$.
  3. Bổ sung Đại lộ Cây Râm Mát (Avenue of Trees) hai bên đường mòn từ $Y = -20f$ đến $Y = -28f$.

### 3.2. Cơ Chế Tương Tác Chủ Động (Proactive Interaction Waypoint Prompt)
* Loại bỏ hoàn toàn việc bước chân vào là bị cưỡng chế dịch chuyển.
* Khi người chơi đứng trong vùng chuyển cảnh:
  - Xuất hiện bảng thông báo nổi trên màn hình: `[Đi Sang Làng Bờ Rìa]`.
  - Hỗ trợ phím tắt bàn phím: Nhấn `[E]` hoặc `[Space]` để kích hoạt.
  - Hỗ trợ chạm/click: Nút bấm trên giao diện cảm ứng cho thiết bị di động.
  - Người chơi có thể tự do quay đầu trở lại trang trại bất cứ lúc nào nếu không bấm nút.
* **Màn hình chuyển cảnh điện ảnh (Cinematic Cutscene Overlay):**
  - Mờ tối dần (Fade to black $0.5s$).
  - Hiện trích đoạn suy ngẫm trong Sổ Tay của Khoa (nhắc nhở về cấm kỵ số 7, sương mù, cái bát úp dở).
  - Tự động thay đổi màu nền Camera (`backgroundColor`) sang sắc thái sương lạnh u ám (`#1f2421`).
  - Điểm xuất hiện tại Làng: $(104f, -16f)$ — Khoa đứng quay mặt vào quảng trường, tránh vòng lặp spawn vô tận.

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 02 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Nhân vật Khoa đứng tại cuối đường mòn Cổng Nam (y = -29.5).    │
│  Hiển thị bảng tương tác nổi "[Đi Sang Làng Bờ Rìa] (Bấm E / Chạm)",   │
│  biển gỗ chỉ dẫn bên đường và hàng cây đại thụ hai bên lối đi.        │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 03 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Màn hình chuyển cảnh tối mờ hiển thị trích đoạn Sổ Tay         │
│  của Khoa ("Đoạn dốc đất đỏ trơn trượt sau cơn mưa phùn...") kèm dòng │
│  hướng dẫn "Nhấp chuột hoặc chạm bất kỳ để tiếp tục".                  │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 4. NHIỆM VỤ 3: THIẾT KẾ & XÂY DỰNG BẢN ĐỒ LÀNG BỜ RÌA CƠ BẢN

### 4.1. Bố Cục Địa Hình & Không Khí U Ám (Atmospheric Level Design)
* **Vị trí biệt lập an toàn:** Tọa độ $X \in [94 \dots 146], Y \in [-29 \dots -4]$ (cách biệt 50m so với Nông trại để triệt tiêu mọi rủi ro xung đột vật lý và rò rỉ tầm nhìn Camera).
* **Nền đất & Đường mòn (`Tilemap_Ground`, `Tilemap_Paths`):**
  - Đất xám tro, cỏ úa mục nát và thảm hoa dại tàn lụi.
  - Trục lộ chính Đông – Tây nối từ Cổng Làng $(97, -16)$ xuyên qua Quảng trường trung tâm làng $(110 \dots 116, -16)$.
  - Các nhánh rẽ dẫn thẳng tới từng gian hàng của dân làng.
  - Đường mòn sương mù ngoằn ngoèo dài 25m dẫn riêng biệt ra Gốc Đa Ranh Giới.
* **Vũng sình lầy (`Tilemap_MudPit`):** Đặt tại đầu dốc trơn trượt, trung tâm quảng trường và bãi quây cân heo.
* **Cổng Làng & Điểm Trở Về Nông Trại:** Đặt tại $(100f, -16f)$ gồm 2 cột gỗ lớn, rào gãy xiêu vẹo, đuốc mờ leo lét và biển hiệu chỉ dẫn `ĐƯỜNG VỀ TRANG TRẠI (Dốc Đất Đỏ Sương Mù)` kèm cơ chế bấm nút chủ động quay về.

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 04 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Toàn cảnh phân vùng Làng Bờ Rìa với tông màu sương lạnh        │
│  u ám, mạng lưới đường mòn đất nện, cổng làng và các gian nhà gỗ.     │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

### 4.2. Chi Tiết 5 Gian Hàng Dân Làng Theo Cốt Truyện & Luật Kiêng Kỵ

#### 1. Quán Bà Sáu (Bếp Ăn & Nước Chè Đầu Dốc) — Tọa độ: `(108, -13)`
* **Kiến trúc & Đạo cụ:**
  - Nhà mái tranh vách đất trát rơm (`shelter_barn` tint vàng nâu đất ấm).
  - Bàn gỗ ăn uống dài đặt trước hiên.
  - **Đạo cụ "Cái Bát Úp Dở" (Cấm kỵ #2):** Chiếc bát sứ men xanh bị úp ngược cố định trên mặt bàn (*"dành cho khách quen chưa tới theo gió đêm, tuyệt đối không được lật ngửa"*).
  - Ấm trà đun nước, đèn lồng vàng leo lét treo trên xà nhà.
  - NPC Bà Sáu trong trang phục tạp dề dân dã.
* **Chức năng:** Bán Cháo nóng (+50 thể lực) và Trà đắng Bờ Rìa (xóa 1 tầng Mệt Dội). Chấp nhận Vàng mặt ($140\%$) hoặc Barter.

#### 2. Xưởng Mộc Tám Vá (Vá Rào Chống Quái) — Tọa độ: `(118, -10)`
* **Kiến trúc & Đạo cụ:**
  - Gian xưởng mộc gỗ thông sẫm màu.
  - Đống gỗ xẻ và các cọc rào xếp chồng chất bên hông xưởng.
  - Bù nhìn rơm (`training_dummy_straw`) đặt ngoài sân dùng làm bia thử nghiệm độ cứng của rào chắn.
  - NPC Tám Vá tay cầm búa đục đẽo.
* **Chức năng:** Cung cấp Kiện Gỗ Xẻ (+300 HP rào) và Cọc Rào Lõi Sắt (**Cấm Vàng**, thuần Barter đổi vật tư).

#### 3. Tiệm Thuốc Thú Y Cô Dung — Tọa độ: `(110, -22)`
* **Kiến trúc & Đạo cụ:**
  - Gian nhà thuốc thảo dược phủ rêu xanh ngắt.
  - **Giếng đá rửa dược liệu** chuyên dụng ngoài trời phục vụ sắc thuốc.
  - Tảng đá rêu phong, đèn lồng ánh xanh lá thảo dược.
  - NPC Cô Dung trong trang phục dược sư.
* **Chức năng:** Cung cấp Gói thuốc bột kháng dịch đàn heo (+40% đề kháng) và Thuốc an thần (**Cấm Vàng**).

#### 4. Trạm Cân "6+1" (Anh Ca & Ông Tồn) — Tọa độ: `(122, -18)`
* **Kiến trúc & Đạo cụ:**
  - Bãi quây rào gỗ nhốt heo chuẩn bị lên bàn cân ($4m \times 3m$) có sẵn máng ăn tạm.
  - **Bàn cân cơ học với vạch chia cấm kỵ (Cấm kỵ #1):** Thước đo bàn cân nhảy trực tiếp từ số `6` sang số `8`, tuyệt đối không có số 7.
  - Bàn làm việc đặt cuốn **"Sổ Đen"** nợ nần và ghi chép thương lái.
  - Bộ đôi NPC: Anh Ca (thanh niên lực lưỡng cân heo) và Ông Tồn (lão làng giữ sổ đen & phân xử).
* **Chức năng:** Cân đo xuất chuồng đàn heo, phân xử tranh chấp thương lái theo giá Barter thị trường ngày phiên.

#### 5. Gốc Đa Ranh Giới (Bí Nhân 25m - Outskirts) — Tọa độ: `(138, -25)`
* **Kiến trúc & Đạo cụ:**
  - Tách biệt 25 mét về phía Đông Nam, chìm sâu trong làn sương dày đặc nhất.
  - **Cây Đa Cổ Thụ khổng lồ (scale 2.8x)** mang sắc tím xám huyền bí rợn ngợp.
  - Cặp cự thạch ranh giới chạm khắc phù chú phong ấn.
  - **Luồng hào quang năng lượng dị biến (Tractor beam purple aura)** lập lòe phát sáng dưới gốc cây.
  - Hàng rào gai gãy cảnh báo nguy hiểm, NPC Bí Nhân khoác áo choàng đen-tím bí hiểm.
* **Chức năng:** **Tuyệt đối không nhận vàng (0% Gold)**. Chỉ khao khát thu mua heo Quý và heo Dị Biến ($5,000\text{G}$ GTTC) để đổi lấy Hạt giống đột biến và Phù chú thao túng trọng lực.

#### Bảng Hương Ước Làng Bờ Rìa — Tọa độ: `(114, -14)`
* Dựng ngay tại trung tâm ngã ba làng, bảng gỗ khắc rõ 3 điều răn:
  > *1. Cấm Kỵ Số 7 | 2. Bát Úp Chớ Lật | 3. Tuyệt Đối Không Nhìn Lên Trời*

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 05 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Khu vực Quán Bà Sáu. Thấy rõ biển hiệu nổi "Quán Bà Sáu",      │
│  bàn ăn gỗ, chiếc bát úp ngược, ấm trà và NPC Bà Sáu đứng trước quán.  │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 06 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Khu vực Trạm Cân "6+1". Thấy rõ bãi quây nhốt heo, biển       │
│  vạch cân nhảy từ 6 qua 8, bàn Sổ Đen và hai NPC Anh Ca, Ông Tồn.     │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 07 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Khu vực Gốc Đa Ranh Giới ngoài rìa làng (x = 138, y = -25).   │
│  Cây đại thụ 2.8x khổng lồ, hào quang tím ma mị và NPC Bí Nhân.       │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 5. NHIỆM VỤ 4: TỐI ƯU HÓA CAMERA & CƠ CHẾ ĐIỀU KHIỂN TƯƠNG TÁC

### 5.1. Khắc Phục Lỗi Giới Hạn Camera (Dynamic Clamping Bounds)
* **Nguyên nhân lỗi cũ:** `CameraFollow2D` ban đầu chỉ kẹp trong `FarmBounds` ($[-45 \dots 45, -32 \dots 32]$). Khi Khoa chuyển cảnh sang Làng Bờ Rìa ($X \approx 104$), Camera bị kẹp cứng ở tọa độ $X \approx 36$ của Nông trại, khiến nhân vật biến mất khỏi màn hình.
* **Nâng cấp:**
  - Khi ở Nông trại: Camera kẹp theo `FarmBounds` ($90m \times 64m$).
  - Khi ở Làng Bờ Rìa: Tự động chuyển vùng kẹp sang `VillageBounds` ($X \in [96 \dots 144], Y \in [-29 \dots -4]$).
  - Tích hợp hàm `SnapToTarget()` giúp Camera định vị ngay lập tức vào nhân vật trong tích tắc chuyển cảnh (màn hình đen), loại bỏ hoàn toàn hiện tượng Camera bay trôi dạt 50m xuyên qua hư không.

### 5.2. Tương Tác Đa Nền Tảng (Multi-Platform Interaction)
* Tích hợp `VillageInteractableTrigger` trực tiếp vào `PlayerInteractionController`:
  1. Khi người chơi đứng gần bất kỳ trạm nào, nút bấm ngữ cảnh nổi trên màn hình Mobile tự đổi tên: `[Gặp Quán Bà Sáu]`, `[Gặp Xưởng Mộc Tám Vá]`, `[Gặp Trạm Cân 6+1]`,...
  2. Hỗ trợ phím tắt PC: Nhấn `[E]` hoặc `[Space]` để nói chuyện ngay.
  3. Hỗ trợ trỏ chuột: Nhấp chuột trực tiếp vào trạm/dân làng.
  4. Mở trực tiếp cửa sổ giao dịch đổi chác [`VillageBarterPopup.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/UI/VillageBarterPopup.cs) với đầy đủ định giá heo sống (90% giá ngày phiên), trừ hao không hoàn tiền dư GTTC và lời thoại hoảng hốt xua đuổi heo Dị Biến.

---

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                 📸 [CHÈN ẢNH MINH CHỨNG 08 TẠI ĐÂY]                   │
│                                                                        │
│  Mô tả: Giao diện Popup đổi hàng (VillageBarterPopup) khi Khoa        │
│  tương tác với dân làng. Thấy rõ danh mục hàng đổi chác, bảng tính     │
│  giá trị trao đổi (GTTC) và các nút bấm giao dịch.                    │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 6. KẾT QUẢ KIỂM THỬ & TÍNH BẤT BIẾN (VERIFICATION)

Chạy bộ kiểm thử tự động toàn diện dự án với lệnh:
```bash
dotnet run --project Tests/PigTycoon.Runner.csproj
```

### Kết quả đạt được: **19/19 Tests Passed (100% Xanh)**
* ✔ `GameClock`: Chu kỳ ngày 16 phút, 4 buổi và hồi phục giấc ngủ.
* ✔ `Farm`: Nút thắt sức chứa và hàm nội suy mật độ mềm.
* ✔ `Pig`: 4 giai đoạn phát triển, phong ấn Hư Thể & thuần hóa Xích Mao.
* ✔ `HerdManager`: 7 điều kiện và chuỗi 3 ngày liên tiếp cho đàn Sơ Khai.
* ✔ `Economy`: Công thức định giá chính thức theo GDD v6.0 mục 4.5.
* ✔ `Economy`: Chợ đêm 18% slot vàng & Bí Nhân 0% vàng.
* ✔ `Defense`: Yêu cầu Sảnh Chỉ Huy & kích hoạt Bạch Vân thủ công.
* ✔ `Grid & Modular Fence`: Bitmask 16 hướng lân cận & tự động nối rào.
* ✔ `Combat`: Archetype vũ khí (Chém, Đập, Đâm) & tham số hướng dữ liệu.
* ✔ `Combat`: Archetype kẻ địch (Trộm, Phá rào, Quái gặm) & công thức sát thương.
* ✔ `Combat`: Cơ chế chiến đấu Stardew 1.6 & bất biến nhân vật 64x64.
* ✔ `Combat`: Độ bền Bù nhìn tập luyện & hệ thống phân tích đấu trường.
* ✔ `Combat`: Hệ thống rơi chiến lợi phẩm theo chủ đề & tỉ lệ drop.
* ✔ `Combat`: Bắt trộm heo, hút heo đĩa bay UFO & điện báo đột kích.
* ✔ `Combat`: Chuỗi leo thang Alien (UFO xịt -> Alien Drop 3 -> Xâm lăng -> Tàu mẹ & Trùm Chiến Tướng).
* ✔ `Combat`: Chuỗi leo thang Sơn Tặc (Trộm xịt -> Phục kích -> Công thành -> Trùm Rìu Sắt).
* ✔ `Combat`: Hệ thống Thể lực 2 tầng P1.08, Combo 3 đòn & Độn thổ né đòn.
* ✔ `Economy & Ledger`: Giao dịch cám/heo chợ ngày, độ chính xác 1G & mô hình ngân sách kép.
* ✔ **`Village Economy & Lore Barter`:** Hệ thống Dân làng Bờ Rìa, luật không hoàn tiền thừa GTTC & từ chối heo Dị Biến.

---

## 7. DANH MỤC FILE MÃ NGUỒN & TÀI NGUYÊN ĐÃ THỰC HIỆN

| Đường Dẫn Tệp | Vai Trò & Đóng Góp Chính |
| :--- | :--- |
| [`DefaultMapBuilder.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/DefaultMapBuilder.cs) | Khởi tạo toàn bộ địa hình Thảo nguyên 90x64m, rừng rậm biên giới 3 hàng so le, nền đất, đường mòn, vũng sình lầy Làng Bờ Rìa và 5 trạm dân làng chi tiết. |
| [`TransitionZoneController.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/TransitionZoneController.cs) | Quản lý chuyển cảnh Cổng Nam <-> Làng Bờ Rìa, waypoint prompt tương tác chủ động (bấm nút/E), hiệu ứng màn mờ, nhật ký Khoa và đổi tint không khí. |
| [`CameraFollow2D.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/CameraFollow2D.cs) | Cơ chế kẹp camera đa phân vùng động (`FarmBounds` vs `VillageBounds`), bổ sung hàm `SnapToTarget()` triệt tiêu giật lag camera khi chuyển cảnh. |
| [`PlayerInteractionController.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/Controllers/PlayerInteractionController.cs) | Tích hợp nhận diện `VillageInteractableTrigger`, cập nhật nút hành động ngữ cảnh nổi trên Mobile và phím `[E]` / `[Space]` trên PC. |
| [`Universal2DSetup.cs`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Editor/Universal2DSetup.cs) | Đồng bộ toàn bộ quy trình sinh địa hình Làng Bờ Rìa và các trạm dân làng vào menu Unity Editor `PigTycoon -> Tạo & Mở Scene Nông Trại 2D Mẫu`. |
| [`bo_ria_village_design_and_transition_guide.md`](file:///home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/bo_ria_village_design_and_transition_guide.md) | Tài liệu đặc tả thiết kế chi tiết Làng Bờ Rìa, danh mục Barter, công thức định giá heo sống và quy tắc cấm kỵ. |

---

*Báo cáo được lập tự động dựa trên mã nguồn và lịch sử Git commit `cecc45a` của nhánh `feat/C/P0.09-khoa-sandbox`.*
