# Kiểm chứng nối logic ↔ UI/UX (02/11/2026)

Chuẩn của người dùng:
- Có logic mà không có UI, hoặc có UI mà không có logic, đều tính là **CHƯA ĐẠT**.
- Phải đối chiếu với tài liệu thiết kế (GDD v6, sổ tay, ADR) để tìm phần thiếu ở cả hai lớp.

Cách làm:
- Một agent chỉ-đọc dò toàn bộ Core, Controllers, UI và bản đồng bộ v10 của Argon. Bảng gốc 30 dòng + danh sách thiếu cả hai lớp nằm trong bản audit, tóm tắt dưới đây.
- Biên dịch thử bằng `.agent_scratch/quick_compile.sh`: Core + Presentation dựng trên DLL Unity, khoảng 20 giây/lần.
- Test Core: `bash .agent_scratch/run_tests.sh`, 144/144.

## A. Lỗi "UI không có logic" trong bản UI v10 của Argon — đã sửa khi ghép

Bản Argon gửi gọi hơn 30 member Core không tồn tại; nếu chép nguyên văn thì không biên dịch được (59 lỗi). Danh sách đã gửi Argon ở tin #108. Ngoài ra:

| Chỗ | Lỗi | Đã sửa |
|---|---|---|
| Khắc Phù Văn / Mệnh Ấn | Khắc ID bịa ("PhuVan_Cap1"), không qua điều kiện Bàn Khắc | 6 nút theo ID thật. Gọi `MartialProgression.EngraveRune/EngraveSeal` → luật Bàn Khắc GDD 12.4 |
| Bàn Tình Báo trong Tổng Quan Trại | Truyền tên quái và hướng gõ tay | Đọc `engine.Intel.BuildReport` (tin thật theo cấp đã mua) |
| Báo cáo trận | Đọc trường không có | Đọc đúng `WaveBattleReport` (thắng/thua, quái hạ, heo mất, đạn, độ bền, số lần vỡ Chống Chịu) |
| Chỉ báo nhịp phản kích | Đoán đòn từ màu nháy, tự tính tỉ lệ khác `CounterResolver`, tự phóng to nhân vật | `EnemyAgentView` gọi từng khung với tiến độ đòn thật. Tỉ lệ thật lấy từ `PlayerCombatController.PreviewCounterChance`. Vùng vàng = khung +45% (≤ 0,08 giây trước khi chạm) |
| Nhánh nâng cấp tháp | Bấm xoay vòng, mỗi lần +400 HP miễn phí | Chọn 1 lần theo `AvailableUpgradeBranches`. Core: cần Phòng Xây Dựng cấp 2, bỏ +400 HP bịa (GDD 16.1 không có) |

## B. Đã nối trong vòng này

| # | Hạng mục | Trước | Sau |
|---|---|---|---|
| 1 | Lướt né / Phản kích trên màn hình cảm ứng | Chỉ có phím Ctrl / Q | 2 nút trên HUD (nhãn phím + nhãn chữ) gọi `ExecuteDodge` / `PressCounter` |
| 2 | Chỉ báo nhịp phản kích | Không ai gọi | Gắn vào vòng lấy đà của quái (xem A) |
| 3 | Giấy Chuyên Biệt (lỗi khai thác) | Dùng, gia cường, rã giấy không cần sở hữu | `SpecialtyPaperActions` (Core): chỉ chọn màu đang có; dùng/rã trừ 1 tờ; gia cường qua `EnhancementTransactions.TryEnhance` (kiểm vàng, nguyên liệu, xưởng). Đá rã ra vào kho |
| 4 | Chế độ bắn của tháp | Chỉ lưu chuỗi, tháp không đọc | `DefenseTower2DView`: "Chỉ tinh anh" chỉ nhắm Tinh Anh/Boss/UFO; "Tiết kiệm" chỉ bắn khi địch vào 60% tầm hoặc có nhóm ≥ 3 |
| 5 | Điều kiện Bàn Khắc không bao giờ đạt | Bản vẽ Bàn Khắc, số chu kỳ mở Phù Văn, số sự kiện Đỏ: không ai ghi | Sự kiện Đỏ (E10–E14) được đếm. Sự kiện Đỏ đầu tiên sau Cổng Giữa Game trao bản vẽ Bàn Khắc (GDD bảng nguồn bản vẽ Cao cấp; tỉ lệ tạm, ADR-0017). Đếm chu kỳ từ khi mở Phù Văn mỗi ngày |
| 6 | Lưu game thiếu | Phù Văn, Mệnh Ấn, màu giấy, bụi/đá, tiến độ Bàn Khắc không lưu | Đã lưu và nạp; test round-trip |
| 7 | An khóa 2 ("dùng 40 lượt hỏi") | `RecordIntelQuestion` không ai gọi | Mỗi lần mua tin ở Bàn Tình Báo = 1 lượt hỏi |
| 8 | Tổng Quan Trại đọc sổ nhiệm vụ cũ | Số liệu khác bảng nhiệm vụ | Đọc `engine.Quests` (cùng nguồn với bảng nhiệm vụ) |
| 9 | HUD thiếu tiềm năng | Không hiện cấp/EXP, tên nỏ, túi nhanh; Nộ chỉ hiện khi có đợt quái | Dòng dưới thanh Thể lực: Cấp · EXP · điểm cộng · Nộ · tên nỏ · hồi chiêu Tiếng Huýt (Khoa) · F5–F8 túi nhanh. Ngọn lửa Nộ hiện khi Nộ > 0 |
| 10 | Mắt Nhà Nghề từ thẻ heo | Lộ dấu hiệu ngay lập tức | Chạy cùng nhịp quan sát 5 giây như phím M |
| 11 | Ngủ từ bảng Nhân vật | Bỏ qua luật "còn quái trong trại" | Nút chỉ báo "Ngủ: giường Nhà Chính". Ngủ ở giường thì lưu game |
| 13 | Nguồn Giấy Chuyên Biệt | Không có | Thắng đợt lớn tung 1 lần; hạ Boss chu kỳ tung riêng (tỉ lệ GDD 10.3). Giấy vào kho |
| 14 | Thanh Boss | Chỉ có thanh máu nhỏ trên đầu quái | Thanh Boss lớn ở đỉnh màn hình: tên, pha 1–3 theo % máu (GDD 19.2), báo khi đổi pha |
| 15 | Đồ có cơ chế nhưng không có tác dụng | Dao Rựa, Còi Chăn Heo | Dao Rựa −20% Stamina dọn bãi/gom vật liệu. Còi Chăn Heo +10 Tinh thần cho heo hoảng loạn trong 10 m, 1 lần/2 phút. Có test |
| 16 | Nhánh nâng cấp tháp (Core) | Miễn phí, +400 HP bịa | Cần Phòng Xây Dựng cấp 2, 1 nhánh/công trình (GDD 16.1) |
| 17 | Lính đánh thuê có thân | Thuê chỉ tạo bản ghi | Vào trận theo đợt quái, đúng bảng GDD 11.2 (HP/sát thương/tầm/hành vi), quái đánh được lính, hết Stamina rút về Cờ Tập Kết 8 giây (ADR-0025) |
| 18 | Thuê NPC theo luật người dùng (ADR-0026) | Thuê 1 lần là xong, trả lương theo ngày | Nhiệm vụ riêng 3 bước; hảo cảm 0–100 (mốc 30/50/80/100 khó dần, cổng theo nhiệm vụ); phẩm chất theo độ trọng yếu; giá theo năng lực và chủ đề chợ mỗi chu kỳ; hợp đồng theo chu kỳ (cọc 30%) / chu kỳ lớn (Cam) / theo đợt (lính); trần NPC theo cấp trại. Quầy tuyển hiện màu phẩm chất, nhiệm vụ, hảo cảm, giá, 2 nút hợp đồng |
| 12 | Thẻ heo | — | Thêm Mắt Nhà Nghề, Khử khuẩn/Phòng dịch, Nghiên cứu bệnh. Giữ bộ chọn phối giống và nút xử lý xác |

## C. Còn thiếu (chưa đạt), theo mức ưu tiên

P0/P1: người chơi gặp ngay.
1. **Trang bị bậc I–IV** và nguồn đồ bậc cao. Phù Văn cần đồ bậc II+, nên hệ Phù Văn vẫn bị chặn lần hai.
2. **Nguồn Bí Kíp.** Giấy Chuyên Biệt đã có nguồn rơi đúng bảng GDD 10.3 (sống sót đợt lớn 8/3/0,8/0,2%, hạ Boss chu kỳ 30/15/5/1,5%). Lô giấy ở đấu giá đêm và chợ đêm còn thiếu.
3. **Thú nuôi có thân trong thế giới** (hiện chỉ là chỉ số cộng thêm). Lính đánh thuê đã có thân.
5. ~~**Hiệu ứng nhánh nâng cấp tháp**~~ — XONG 2/11 tối: 15 nhánh đúng tên và số GDD 16.1 (`DefenseBranches`), tháp đổi cách bắn (xích, trói, vùng cháy chặn đường, choáng, màn mảnh UFO phải vòng, kéo UFO xuống thấp, xuyên tuyến 60 m), băng đạn đổi khi chọn, mã cũ trong save đọc theo vị trí. Còn hở: "Theo Dấu" bỏ phạt mục tiêu nhỏ bay thấp nhưng GDD chưa định nghĩa phạt gốc.
6. **Sổ hai cột của Khoa:** `VerifyObservationToRightColumn` không ai gọi.
7. **10/14 sự kiện huyền bí** chưa có điểm kích hoạt. ~~P01–P08~~ (XONG: ADR-0027, cả hai tuyến), 9/12 nhiệm vụ phụ, màn hình chính.
8. **Vũ khí cầm tay theo hướng:** bản WeaponSocket của Argon dựa trên animator cũ. Cần ghép tay phần `UpdateWeaponSocket` vào animator hiện tại.

P2:
- Giáp Da Thuộc "−50% nhiễm trùng khi bị cắn": game chưa có cơ chế nhiễm trùng cho người chơi.
- Búa Tạ "vỡ khiên năng lượng nhỏ", "đóng cọc rào −20% thời gian".
- 7 việc riêng của NPC trại và trần NPC theo cấp trại.
- Lôi Pháo / Bạch Vân điều khiển tay.
- Dự báo thời tiết.
- Bảng ngân sách kép.
- Mốc mở khóa theo cấp trại.
- Hành vi đàn (hoảng loạn, độ bám đàn, tiểu tính cách).
- Cơ chế riêng của 10 bệnh.
- Dao Rựa cắt dây trói / Búa Tạ phá khiên.
- Lưới dây điện.
- Hệ số Cóng Tay vào thời gian thao tác.
- Hướng dẫn 8 bước.
- Vật phẩm bỏ qua giấc ngủ.
- Phòng nghiên cứu.
- Bản đồ nhỏ / Lạc Hướng.

## D. Mỹ thuật chờ người dùng duyệt (không tự ghép)

- Sprite An/Khoa "2.5D HD" v10 của Argon (ô 204×240, lọc mượt). Bản này rời khỏi pixel 32 PPU đã duyệt. Đang giữ bản pixel v5.
