# Sổ sai lệch GDD v6.0 so với game đang chạy (02/11/2026)

Bốn lượt kiểm tra đối chiếu `Docs/GDD_PIG_MECHANICS.md` với code của bản build 13. Một cơ chế chỉ được tính là **CÓ** khi lúc chơi có code gọi tới và người chơi thấy hoặc dùng được nó.

Ký hiệu:
- **CÓ**: đã chạy đúng.
- **MỘT PHẦN**: có chạy nhưng thiếu hoặc sai số.
- **CHỈ LÕI**: logic có trong Core nhưng không nơi nào gọi tới.
- **THIẾU**: chưa có.
- **SAI SỐ**: chạy nhưng số liệu khác GDD.

Cột "Người sửa":
- **C** = Claude (luồng chính).
- **S1** = agent thể lực, ngủ, đặc tính.
- **S2** = agent đàn heo và sinh sản.
- **S3** = agent phòng thủ và quái.

## Chương 8 — Nhân vật, chỉ số, tiềm năng

| Mục GDD | Hiện trạng | Người sửa |
|---|---|---|
| Cấp nhân vật 1–30; mỗi cấp +2 điểm chỉ số và +3 HP; cứ 5 cấp +8 Stamina; EXP từ lao động, hạ quái, sống sót đợt quái | THIẾU: không ai cộng EXP, không lên cấp được | C |
| Chỉ số gốc và chỉ số thưởng hiển thị dạng "STR 24 (+3)" | MỘT PHẦN: trần +8 đúng, nhưng chưa có giao diện | C |
| Tác dụng chỉ số: STR +1%/điểm vũ khí nặng, AGI +0,4% tốc chạy và vũ khí nhẹ, CTRL chí mạng 0,2%, RES giảm sát thương 0,5% | SAI SỐ: sát thương theo công thức Stardew, chí mạng dùng LUK, AGI không đổi tốc chạy | C |
| HP trần mềm/cứng 140/180 | MỘT PHẦN: HP cố định 100 | C |
| Tiếng Huýt của Khoa: 12 heo, 20 m, đi theo 60 giây, hồi chiêu 2 giờ | SAI SỐ: thành gọi về Nơi Trú trong 25 m, không giới hạn, không hồi chiêu | S1 |
| Mắt Nhà Nghề của An | THIẾU | S1 |
| Chọn nhân vật; đồng bộ nhân vật chính | MỘT PHẦN: game mới dùng chỉ số An nhưng nhánh truyện Khoa | S1 |

## Chương 9 — Thể lực

| Mục | Hiện trạng | Người sửa |
|---|---|---|
| Hai lớp tiêu hao (tức thời và Tải ngày) cho việc trại | MỘT PHẦN: việc trại trừ thẳng, không cộng Tải ngày | S1 |
| Hồi 10/s chỉ áp cho phần thể lực tiêu trong combat | SAI: hồi đầy mọi thứ, thể lực gần như vô hạn | S1 |
| Nghỉ 6/giờ; bữa ăn +20/+35 | THIẾU | S1 |
| Quá sức: Tải >55% → Thể trạng −8; >75% → −15 | THIẾU | S1 |
| Ngủ | LỖI: ngủ nhảy giờ, bỏ qua mô phỏng ngày | S1 |
| Không ngủ được khi có sương mù | THIẾU | S1 |
| Mệt Dội giảm dần | CHỈ LÕI, và có hai công thức mâu thuẫn | S1 |
| 5 vật phẩm phá hồi chiêu ngủ | CHỈ LÕI | S1 (nếu kịp) |

## Chương 10 — Đặc tính, Giấy Chuyên Biệt, 6 khóa

| Mục | Hiện trạng | Người sửa |
|---|---|---|
| Cổng Giữa Game | CHỈ LÕI: số đúng nhưng người chơi không thấy | S1 |
| 6 khóa mở theo thứ tự, ô "???" | SAI: khóa tự mở theo cấp trại, hiệu ứng có ngay từ khóa 1 | S1 |
| Nguồn nhận giấy (10.3) | THIẾU, thay bằng công thức chế tự đặt | S1 (nếu kịp) |
| Tỉ lệ giấy, gia cường, Đá Cường Hóa | CHỈ LÕI, có 3 bản sao | sau |

## Chương 12 — Trang bị, võ kỹ, Nộ, Phù Văn, Mệnh Ấn

| Mục | Hiện trạng | Người sửa |
|---|---|---|
| 6 ô trang bị, mặc và tháo, lưu game | THIẾU: không có giao diện; trang bị luôn rỗng và không được lưu | C |
| 3 cặp đồ mở ô võ kỹ Công/Thủ/Biến | CHỈ LÕI: HUD luôn bật cả 3 nút | C |
| Nguồn năng lượng theo ô (Thủ dùng Huyết Tế: 1 HP thay 2 STA) | CHỈ LÕI, và dùng Chống Chịu thay HP | C |
| Hạng cân → lối chơi Khinh Thân / Cân Bằng / Trọng Giáp | CHỈ LÕI, SAI SỐ (+12% thay vì +8%; thiếu −10% hồi chiêu Biến; thiếu ±10/15% STA theo phái) | C |
| Danh mục 16 món (mỗi món một cơ chế; vũ khí 14/22/48/26) | SAI SỐ: hạng cân Nỏ, Giáp Da, Quần Giáp Xích sai; không có số sát thương; chỉ 2 cơ chế chạy | C |
| Nộ: +2 quái thường, +10 tinh anh, +1 khi đỡ; giảm 5/s sau 10 s; về 0 khi hết đợt | SAI SỐ: +15 mỗi lần hạ, không giảm, không reset | C |
| 24 kỹ năng (ô, tiêu hao, hồi chiêu, yêu cầu chỉ số gốc) | CHỈ LÕI, sai nhiều; HUD dùng bộ chiêu cố định, không hồi chiêu | C |
| Học võ: chi phí vàng + Điểm Võ; luật phái chính, phái liên kết, phái khác | THIẾU | C |
| Vòng hợp kích A→C→B→D→A | CHỈ LÕI, hệ số sai | C |
| Đòn đánh phụ thuộc vũ khí và hướng | MỘT PHẦN: vũ khí luôn là dao gỗ; kỹ năng không theo hướng | C |
| Túi nhanh 4 ô; vật phẩm 4 nhóm | THIẾU hoặc CHỈ LÕI | sau |
| Phù Văn, Mệnh Ấn | CHỈ LÕI, mã không khớp danh mục | sau |

## Chương 3–7 — Trại, đàn heo, gen, Sinh Cảnh, thời tiết

| Mục | Hiện trạng | Người sửa |
|---|---|---|
| Phối giống F1/F2 | CHỈ LÕI: không ai gọi tới, đàn chỉ có giảm; tỉ lệ di truyền 55/45 sai | S2 |
| Heo mới sinh có hình trong thế giới | THIẾU: hình heo chỉ tạo một lần | S2 |
| Xử lý xác heo | CHỈ LÕI: xác nằm mãi, trừ sức chứa và Sinh Cảnh | S2 |
| Tính chất phụ, Dị Biến; nội tại dòng gen | THIẾU hoặc MỘT PHẦN | S2 |
| Giám định "?" | LỖI: lần thử lại có kết quả cố định, giá hiển thị sai | S2 |
| Sinh Cảnh | SAI: +5/ngày miễn phí; dọn chuồng không tính | S2 |
| Cấp trại 1–20 | SAI SỐ: 1500×cấp, không hiện ra | S2 |
| Kiểm dịch | MỘT PHẦN: bán không kiểm dịch vẫn luôn thành công | S2 |
| Áp lực mật độ | MỘT PHẦN | S2 |
| Bệnh theo nguyên nhân và dòng gen; lây theo chu kỳ 6 giờ | MỘT PHẦN | S2 |
| Thời tiết và buổi trong ngày tác động lên nhân vật | MỘT PHẦN | sau |

## Chương 11, 13–19 — NPC, công trình, quái, Boss

| Mục | Hiện trạng | Người sửa |
|---|---|---|
| Nâng cấp Phòng Xây Dựng 1→3 | THIẾU: 18/31 công trình không bao giờ xây được | S3 |
| Cơ chế riêng của từng công trình | THIẾU: mọi thứ (cả rào, cổng, radar) đều là súng 40 sát thương | S3 |
| Heo bị quái đánh | THIẾU: 7 loại quái tới heo mà không làm gì | S3 |
| Lưu vị trí công trình | THIẾU: mở lại game là mất hình | S3 |
| 14 loại quái | 3 đủ, 7 một phần, 4 thiếu; HP 4 loại sai | S3 |
| Hao mòn, nạp đạn tốn phí, trạng thái Mòn / Nguy kịch | CHỈ LÕI hoặc THIẾU | S3 |
| Boss | Có xuất hiện nhưng không có pha nào; Mẫu Hạm 8.000 HP thay vì 20.000 và đứng im | S3 |
| Báo cáo sau đợt; sự kiện WaveStarted/WaveEnded | THIẾU: chỉ thưởng cố định 100 G | S3 |
| Lưới điện | CHỈ LÕI | S3 |
| Lính đánh thuê có thân trong thế giới; thú cưng gác | THIẾU | sau |
| Chủ đề chu kỳ chợ; chợ phản ứng sau đợt | CHỈ LÕI | sau |
| Tình báo 0–5, Bàn Tình Báo, dự báo | THIẾU | sau |
| Bí nhân, trao đổi đêm (chênh giá, ÁLSK, hàng) | MỘT PHẦN | sau |
| Sự kiện huyền bí | 4/14 | sau |

## Thứ tự làm

Đợt 1 làm song song bốn luồng C, S1, S2, S3. Sau đó tôi build Unity, quay video kiểm, đóng gói. Các mục "sau" làm ở đợt 2 theo cùng thứ tự ảnh hưởng tới người chơi.


## Cập nhật trưa 02/11, sau đợt 1 và sau khi đọc trọn tài liệu

Đã đọc trọn từ đầu đến cuối: GDD (Pig.docx, 3.084 dòng), HANDBOOK v3.0 (KY_THUAT trùng 100%), cốt truyện Mùa 1 Phần 1–12. Phân tích từng chương ở `2026-11-02-gdd-full-read-analysis.md`.

Đã sửa trong đợt 1 (test lõi 128/128; Unity build 14 biên dịch được):
- **C — võ kỹ và nhân vật:**
  - 16 món đồ, 24 kỹ năng đúng số GDD;
  - 3 cặp đồ mở 3 ô; ô thứ 4 Song Thế trả bằng Nộ;
  - vòng liên kết một chiều; hợp kích theo phái chính;
  - hạng cân → lối chơi; lướt né;
  - Trọng Giáp: đòn nặng gây choáng; CTRL kéo dài khống chế;
  - đòn nặng theo vũ khí (kéo / choáng / tên tín hiệu);
  - Nộ, Điểm Võ; cấp 1–30 và điểm chỉ số;
  - học võ bằng vàng + Điểm Võ; đổi phái chính;
  - túi nhanh 4 ô;
  - Phù Kiên Thạch / Nộ Hỏa / Truy Phong, Ấn Bất Khuất / Song Thế / Mục Đồng;
  - thanh sống còn là Chống Chịu (quyết định người dùng, không có HP);
  - bảng Nhân vật (C) 4 tab; HUD ô võ kỹ có tên chiêu, khóa và hồi chiêu.
- **S1 — thể lực, ngủ, đặc tính:** ngủ chạy mô phỏng; Tải ngày; quá sức; Mệt Dội; Tiếng Huýt 12 heo / 20 m / 60 giây / 2 giờ; Mắt Nhà Nghề; 6 khóa theo giấy.
- **S2 — đàn heo:** phối giống F1/F2 qua giao diện; heo mới sinh có hình; xử lý xác; tính chất phụ; giám định; Sinh Cảnh; cấp trại theo bảng; kiểm dịch; mật độ; bệnh theo nguyên nhân và lây 6 giờ.
- **S3 — phòng thủ:** nâng Phòng Xây Dựng; hành vi theo từng loại công trình; heo bị quái đánh; lưu vị trí công trình; 14 quái; độ bền; Boss; báo cáo sau đợt.

Còn thiếu (đợt 2), theo thứ tự ảnh hưởng tới người chơi:
1. Màn chọn An/Khoa đầu game.
2. Phòng Xây Dựng cần NPC trực, có hiệu suất 40/70/100%; thời gian thi công.
3. Trạng thái thời tiết lên người (Cảm Nắng, Ướt Lạnh, Sét Đánh…) để đồ chống thời tiết có tác dụng.
4. Phẩm chất đồ I–IV và nguồn đồ bậc cao; Bí Kíp; bản vẽ Bàn Khắc; giao diện khắc Phù Văn và Mệnh Ấn.
5. Lính đánh thuê và thú cưng gác có thân trong thế giới; Đèn Hiệu; Cờ Tập Kết.
6. Tình báo 0–5 và Bàn Tình Báo; 8 chủ đề chu kỳ; chợ đêm theo "món muốn nhận".
7. 10/14 sự kiện huyền bí; 9/12 quest phụ; 8 quest mở đầu P01–P08; sổ hai cột của Khoa.
8. Hành vi theo tính chất phụ và theo Tinh thần (húc rào, chạy loạn, Độ bám đàn).
