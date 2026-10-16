# ADR-0003: Hệ Thống Điều Khiển Twin-Stick Hybrid Thông Minh (Combat 360° & Farm Smart Reticle)

- **Trạng thái**: ĐÃ CHẤP THUẬN (APPROVED)
- **Ngày**: 2026-10-06
- **Chủ trì**: Thành viên C (Combat – Defense – Enemy Lead)
- **Tham gia**: Thành viên B (World/Farm Lead), Thành viên A (Tech Lead)

---

## 1. Bối cảnh (Context)
Trò chơi *Pig Tycoon* kết hợp 2 nhịp trải nghiệm chính:
1. **Quản lý trang trại ban ngày (Farming / Construction)**: Đặt cọc rào, dựng máng ăn/máng nước theo lưới ô vuông chuẩn $2\times 2\text{m}$, đổ cám, dội nước mát, khám chữa bệnh cho heo.
2. **Chiến đấu thủ thành ban đêm / Đợt quái (Combat / Wave Defense)**: Di chuyển né đòn, dùng vũ khí cận chiến / tầm xa ngăn quái, bảo vệ đàn heo và kho thức ăn.

Trải nghiệm chiến đấu đòi hỏi khả năng **Strafe** (vừa lùi về phía bẫy/cổng trại vừa vung vũ khí hoặc bắn đạn nỏ về phía quái đuổi theo). Nếu chỉ dùng 1 joystick di chuyển và 1 nút đánh cố định, nhân vật bắt buộc phải quay mặt vào quái mới đánh được, khiến việc vừa lùi vừa giữ khoảng cách (kiting) bất khả thi.

Ngược lại, nếu áp dụng cơ chế Twin-Stick tự do $360^\circ$ cho toàn bộ các công cụ trang trại, việc đặt công trình theo lưới ô vuông $2\times 2\text{m}$ hoặc dọn dẹp vệ sinh sẽ bị trượt, lệch ô và gây mỏi tay khi phải kéo-thả cần liên tục hàng chục lần.

---

## 2. Quyết định kiến trúc (Decision)
Dự án áp dụng giải pháp **Twin-Stick Hybrid Thông Minh (Contextual Twin-Stick)**:

### 2.1. Cần Di Chuyển - Move Joystick (Bên Trái)
- Điều khiển vector di chuyển vật lý của nhân vật ($X, Y$).
- Khi không chạm vào cần phải, hướng quay mặt của nhân vật (`FacingDirection`) bám theo hướng di chuyển.

### 2.2. Cần Thao Tác / Ngắm - Action/Aim Joystick (Bên Phải)
Hệ thống tự động chuyển đổi logic xử lý tùy thuộc vào vật phẩm người chơi đang cầm trên tay:

#### A. Khi cầm VŨ KHÍ (Dao Rựa, Giáo Móc, Búa Tạ, Nỏ Tay, v.v.):
- **Góc ngắm $360^\circ$ tự do**: Ghi đè `FacingDirection` của nhân vật sang 4 hướng gần nhất; vệt chém hoặc đường đạn xoay tự do $360^\circ$.
- **Cơ chế Kích hoạt đòn đánh**:
  - **Tap (Nhấp nhanh)**: Khóa mục tiêu (auto-target) quái gần nhất trong tầm đánh và tung 1 đòn trong combo 3 đòn.
  - **Drag (Kéo giữ)**: Hiện indicator hình quạt / mũi tên ngắm $360^\circ$. Vượt ngưỡng ($> 0.5$) hoặc thả tay để ra đòn nặng / bắn đạn.
  - **Vùng Hủy (Cancel Zone)**: Rê cần về tâm để hủy đòn nhằm bảo toàn Stamina.

#### B. Khi cầm TOOL ĐẶT CÔNG TRÌNH & SỬA CHỮA (Cọc Rào, Máng Ăn, Máng Nước, Búa Thợ Mộc):
- **Tap (Nhấp nhanh)**: Tự động tác động / đặt vào **ô vuông hợp lệ ngay trước mặt** nhân vật theo `FacingDirection`. Cho phép người chơi vừa chạy vừa nhấp để cắm một dãy hàng rào thẳng tắp cực nhanh.
- **Drag (Kéo định hướng)**: Di chuyển **Khung ô vuông ảo (Grid Reticle)** sang các ô xung quanh (bán kính 1–2 ô) để người chơi tự chọn ô mà không cần bước tới. Nhả tay để thi công / sửa chữa / tháo dỡ.

#### C. Khi cầm TOOL CHĂM SÓC HEO (Xô Nước, Bao Cám, Bàn Chải, Kính Lúp):
- **Tap**: Tự động tương tác với máng ăn/nước gần nhất hoặc con heo đang cần chăm sóc nhất.
- **Drag**: Vẽ đường trỏ đích danh con heo cụ thể trong đàn muốn tương tác.

---

## 3. Hệ quả (Consequences)
- **Tích cực**:
  - Tối ưu hóa trải nghiệm chiến đấu trên mobile & gamepad: Cho phép Strafe mượt mà, cảm giác đánh chính xác chuẩn *Momodora*.
  - Giữ nguyên sự thuận tiện, tốc độ và tính công thái học (ergonomics) của các thao tác làm việc trại hằng ngày.
  - Kiểm soát triệt để thể lực (Stamina): Không bị spam đòn ngoài ý muốn làm cạn kiệt thể lực.
- **Tiêu cực / Thách thức**:
  - Cần cài đặt State Pattern hoặc Strategy Pattern trong tầng Controller để xử lý chuyển đổi mượt mà giữa Combat Mode và Farm Tool Mode.

---

## 4. Kế hoạch triển khai
1. Module Presentation / Input:
   - Thêm `AimJoystick` bên cạnh `MoveJoystick` trên Canvas UI Mobile.
   - Viết `PlayerAimController` liên kết với `PlayerMobileController` và `PlayerInteractionController`.
2. Ánh xạ đa nền tảng:
   - **Mobile**: Move Joystick (Trái) + Aim Joystick (Phải).
   - **PC**: Phím `WASD` + Con trỏ chuột (Crosshair/Mouse Direction).
   - **Gamepad**: Left Stick + Right Stick.
