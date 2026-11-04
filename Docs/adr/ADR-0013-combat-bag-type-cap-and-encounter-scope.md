# ADR-0013 — Túi chiến đấu: giới hạn theo loại, phạm vi giao tranh, kho liên kết

Ngày: 2026-10-31. Nguồn: thảo luận R4 với Codex (`audit/codex/R4/R4-design.md`, `ux-table.md`, `simulate_ux.py`), GDD v6.0 và phán quyết người dùng 30–31/10/2026. Bổ sung ADR-0012: trả lời câu hỏi mở 1, 3, 6, 7, 8 của ADR-0012. Câu 2, 4, 5 vẫn mở.

## Quyết định

1. **Chồng trong túi và giới hạn theo loại (ADR-0012 câu 1).** GDD L285: "mỗi loại tối đa 3 cái khi vào đợt quái".
   - Một ô túi chứa tối đa 3 món cùng vật phẩm và cùng chất lượng (`CarryRules.BagStackPerSlot = 3`).
   - Thêm trần **3 món cho mỗi loại, gộp mọi chất lượng** (`CarryRules.BagMaxPerType = 3`), để không lách bằng cách tách chất lượng thành nhiều chồng.
   - Số ô 3/8 quyết định *bao nhiêu loại* mang theo; trần theo loại quyết định *bao nhiêu món mỗi loại*. Gói 8 ô cao cấp là để mang đa dạng hơn, không phải để mang 24 lọ thuốc.
   - Trần chỉ kiểm khi thêm vào túi. Khi tải save, túi cũ vẫn nạp được: save trước ADR này có thể có tới 8 món cùng loại, và luật lưu/tải của ADR-0012 cấm tự bỏ vật phẩm.
   - Trần không áp cho vật liệu xây dựng hay 40 mũi tên của Nỏ Tay (G973); đó là bộ đếm của vũ khí.
2. **Phạm vi "giao tranh" (ADR-0012 câu 7).** Luật mới (P7): túi 3/8 và việc đóng liên kết kho chỉ áp dụng khi có **đụng độ thù địch chưa giải quyết**, gồm:
   - đợt quái;
   - khoảng nghỉ 25–40 giây giữa các đợt con (G1142): không mở lại liên kết để tiếp tế;
   - kẻ trộm lẻ ngoài đợt (G825), kẻ trộm/UFO (G1154, G1158–1159);
   - chuyến rừng huyền bí đang mở.

   Một lần đi rừng yên bình *không* tự động bị tính là giao tranh. Kiểm tra phải đặt ở tầng lệnh Core, không chỉ ẩn UI.
   - **Hiện trạng:** `PlayerCarry.InCombat` đang là `Clock.IsDuringCombatWave || Expedition.IsActive`. Còn thiếu kẻ trộm lẻ và UFO ngoài đợt. Việc tiếp theo là thêm một predicate đụng độ trung tâm trong Core, với test đỏ trước.
3. **Kho liên kết 100 ô (ADR-0012 câu 3, P8).**
   - 100 là trần *số mục nhìn thấy/thao tác từ xa* qua liên kết, theo (vật phẩm, chất lượng). Đó không phải dung lượng vật lý của kho.
   - Liên kết trỏ vào cùng một kho vật lý, không tạo kho thứ ba.
   - Thủ Kho +15% (G945) không biến 100 thành 115.
   - GDD không có dung lượng gốc, đơn vị đo hay kích thước chồng cho kho vật lý. `WarehouseStackLimitPerSlot` giữ nguyên "không giới hạn" của bản cũ và được ghi là **chưa có nguồn**, không phải số cân bằng.
4. **Lấy/gửi tại cửa kho khi đang giao tranh (ADR-0012 câu 8).** Vẫn cho phép, chỉ liên kết từ xa bị đóng. Cấm nạp đạn tháp trong đợt (G520/G867) là luật riêng, vẫn giữ ngay cả ở cửa kho.
5. **Vòng đeo tay là tiện ích, không phải trang bị (P3).**
   - "Vòng đeo tay sơ cấp — Liên kết kho" là tiến trình tiện ích vĩnh viễn: không có chỉ số chiến đấu, không chiếm 1 trong 6 món trang bị chiến đấu (G271/G426), không tạo ô thứ 7.
   - Thiết kế mộc như dụng cụ trại, không dùng lại vòng cổ 7 khấc của mẹ (S115–S126).
   - Đề xuất: bà Sáu trao vòng cùng bộ dụng cụ sau Q01.
6. **Vòng dụng cụ 6 ô (ADR-0012 câu 6, P6).**
   - Sáu cung là *động từ theo ngữ cảnh*: cho ăn, nước, bảo trì tạm, chăm sóc, vũ khí đang sở hữu, kỹ năng riêng của nhân vật.
   - Kỹ năng riêng: Khoa là còi chăn đàn (G841: tối đa 12 heo, 20 m); An là quan sát 5 giây.
   - Bạch Vân vẫn cần đủ điều kiện cấp trại 12, xây và kích hoạt (G564/G301). Chọn ô trên vòng không cho An kỹ năng của Khoa.
   - **Hiện trạng:** vòng mặc định vẫn dùng bộ 6 của ADR-0012 (có Bạch Vân) và coi 11 dụng cụ là đã sở hữu. Mở khóa theo nhân vật và theo tiến trình là việc kế tiếp, chưa làm.
7. **Nhiệm vụ khởi đầu (P5).** Không chèn việc cày cấy vào P01–P08. Bài học gửi kho nằm trong phần cho ăn/dụng cụ của Q01, giữ đủ mục tiêu gốc và thưởng 150G/80EXP. Vòng cấp một lần khi Q01 xong, chỉ từ lệnh Core thành công thật, không tính bấm animation. **Hiện trạng:** nhiệm vụ nhỏ hai mục tiêu của ADR-0012 vẫn đang dùng; chuyển sang Q01 là việc kế tiếp.

## Còn mở (không đoán số)

- Kích thước "một chồng" trên tay trước khi có vòng (`HandStackLimit`, đang là 1). R4 cho thấy 1 món/chuyến làm 12 món tốn 12 lượt đi về. Nên giữ nguyên số lượng của gói nhặt được, nhưng chưa có số gốc.
- Dung lượng vật lý của kho (gốc, đơn vị, cách làm tròn) nếu sau này cần trần thật.
- Giá và nguồn nhận túi cao cấp. Số đồ khởi đầu chuyển giao lúc nhận trại.

## Kiểm chứng

- `Tests/PlayerCarryTests.cs` `RunBagTypeCap`: đỏ trước (85/86), xanh sau khi cài (86/86). Nhặt 5 băng gạc ở chất lượng 0,0,1,1,2 trong túi 8 ô thì chỉ giữ 3; một loại khác vẫn vào được.
- Mô phỏng UX của Codex (`simulate_ux.py`): túi 3 ô với chồng 3 đủ cho đột kích trại (3 thuốc hồi + 3 hồi lực = 2 ô). Cửa cổng rừng kèm 2 loại đồ nhặt thì 3 ô không đủ chứa hết, 8 ô thì đủ. Đây là số học trên khối lượng giả định, không phải playtest.
