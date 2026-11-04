# ADR-0026 — Thuê NPC: phẩm chất theo vai trò, hảo cảm dài hơi, hợp đồng theo chu kỳ / theo đợt

Ngày: 02/11/2026. Trạng thái: luật **do người dùng chốt**. Số tính từ GDD có ghi nguồn; số đánh dấu TẠM chờ người dùng duyệt.

## 1. Luật người dùng chốt (ghi đè GDD 11 "lương theo ngày")

1. **Mở khóa chỉ cho phép thuê, không phải thuê vĩnh viễn.** Mỗi lần thuê là một hợp đồng có kì hạn.
2. **Mọi NPC có nhiệm vụ riêng.** Xong nhiệm vụ mở đầu thì:
   - NPC thường mới mở thanh hảo cảm, rồi phải đạt ngưỡng mới thuê được;
   - lính đánh thuê mở được thuê bằng tiền ngay, không cần hảo cảm, nhưng giá đắt hơn.
3. **Hảo cảm 0–100, đi đường dài.** Không tăng chỉ nhờ trò chuyện hay tặng quà.
   - Ngưỡng thuê cao nhất là 30.
   - Các mốc 30 / 50 / 80 / 100 khó dần.
   - Từ 50 trở lên chủ yếu tăng thuộc tính.
   - Dưới 50 thì không mở được đặc tính chuyên môn, dù đã có giấy và xong cả 6 khóa.
4. **Phẩm chất (màu) định nghĩa vai trò, không phải độ hiếm hay sức mạnh.** Các vai trò có độ trọng yếu khác nhau và phải phân tích. Ví dụ: nghiên cứu và kỹ sư xây dựng trọng yếu ngang nhau.
5. **Giá không dùng hệ số.**
   - Khi mở, NPC đã có một giá theo chu kỳ, định theo năng lực.
   - Giá lên xuống theo thị trường mỗi chu kỳ, giống giá heo và giá trao đổi.
   - Hảo cảm tăng hiệu suất nhưng KHÔNG làm tăng giá.
6. **Hợp đồng chỉ có hai kiểu: theo chu kỳ, hoặc theo đợt.** Không có kiểu theo ngày, vì dễ quên và khó kiểm soát.
   - Theo chu kỳ: phải cọc trước 30% tổng tiền.
   - NPC đặc biệt (nghiên cứu) phải thuê dài, ví dụ 2 chu kỳ lớn.
   - Chu kỳ nhỏ 7–10 ngày, chu kỳ lớn 13–15 ngày.

## 2. Độ trọng yếu của vai trò → phẩm chất

Mỗi vai trò chấm 1–5 trên 4 tiêu chí, lấy căn cứ từ chữ GDD 11.1–11.2, 13.2, 6, 19:

| Tiêu chí | Trọng số | Ý nghĩa |
|---|---|---|
| A. Phạm vi hệ thống | 0,25 | Vai trò chạm tới bao nhiêu hệ (đàn, kho, phòng thủ, gen, chợ, tự động hóa) |
| B. Khó thay thế | 0,30 | 5 = không ai làm thay được; 1 = người chơi tự làm được |
| C. Hậu quả khi thiếu | 0,25 | Thiếu vai trò này thì mất gì |
| D. Chiều sâu chuyên môn | 0,20 | Mức tri thức hoặc tay nghề cần có |

| Vai trò | A | B | C | D | Điểm | Căn cứ |
|---|---|---|---|---|---|---|
| Nông Công | 1 | 1 | 1 | 1 | **1,00** | 4 việc chăm/ngày. Người chơi tự làm được |
| Thủ Kho | 2 | 2 | 2 | 2 | **2,00** | Nạp trước đạn; kho không hỏng cám khi Nồm Ẩm |
| Nhà Buôn | 2 | 2 | 2 | 3 | **2,20** | Bán hộ 10 heo khi người chơi bận |
| Vệ Binh Khiên | 2 | 2 | 3 | 2 | **2,25** | Chặn cửa. Tháp và người chơi đỡ thay được một phần |
| Kỹ Sư Chiến Trường | 2 | 3 | 2 | 3 | **2,50** | Đặt lại bẫy đã cạn: chỉ lính này làm trong đợt |
| Xạ Thủ Nỏ | 3 | 2 | 3 | 3 | **2,70** | Săn tinh anh 18 m |
| Thích Khách Đêm | 3 | 3 | 3 | 4 | **3,20** | Quái tàng hình và quái trộm đang rút |
| Bác Sĩ Thú Y | 3 | 3 | 4 | 4 | **3,45** | Phân biệt Rối Loạn Sinh Sản; báo ổ dịch trước 12 giờ |
| Quản Lý Ca | 4 | 4 | 3 | 3 | **3,55** | Mở tự động hóa bậc 2–4 cho mọi NPC khác |
| Thợ Săn UFO | 3 | 4 | 4 | 4 | **3,75** | Gần như lực phòng không duy nhất; cứu heo khỏi chùm kéo |
| Kỹ Sư Xây Dựng | 5 | 4 | 5 | 4 | **4,50** | Xây và sửa mọi công trình; người khác trực chỉ đạt 40% |
| Nhà Nghiên Cứu Gen | 4 | 5 | 4 | 5 | **4,50** | Chỉ người này chạy được phòng nghiên cứu (Hiếm/Quý, Thuần Hóa) |

Ngưỡng màu: **Trắng** < 1,75 ≤ **Lục** < 2,6 ≤ **Lam** < 3,4 ≤ **Tím** < 4,2 ≤ **Cam**.

| Phẩm chất | Vai trò | Nghĩa |
|---|---|---|
| Trắng — Phổ thông | Nông Công | Việc tay chân, ai cũng làm được |
| Lục — Hậu cần | Thủ Kho, Nhà Buôn, Vệ Binh Khiên, Kỹ Sư Chiến Trường | Giữ nhịp kho, chợ, cửa |
| Lam — Chuyên môn | Xạ Thủ Nỏ, Thích Khách Đêm | Xử lý mối nguy riêng |
| Tím — Then chốt | Bác Sĩ Thú Y, Quản Lý Ca, Thợ Săn UFO | Thiếu là cả một hệ lung lay |
| Cam — Trụ cột | Kỹ Sư Xây Dựng, Nhà Nghiên Cứu Gen | Không ai thay; ngang nhau (4,50) |

## 3. Hảo cảm

- **Mở thanh:** xong nhiệm vụ riêng bước 1 thì thanh mở ở 0 và cộng ngay +10.
- **Ngưỡng thuê** (NPC thường): Trắng 10 · Lục 15 · Lam 20 · Tím 25 · Cam 30. Lính đánh thuê: 0 (chỉ cần xong bước 1).
- **Tăng hảo cảm** chỉ qua việc thật. Trò chuyện và quà không cộng.

  | Nguồn | Điểm gốc |
  |---|---|
  | Nhiệm vụ riêng bước 1 / 2 / 3 | +10 / +10 / +10 |
  | Việc nhờ (1 việc mỗi chu kỳ, nộp đồ NPC cần) | +5 |
  | Làm trọn một kì hợp đồng | +6 |
  | Lính sống sót qua một đợt quái | +1 |
  | Trễ thanh toán | −10 |
  | Chấm dứt hợp đồng sớm | −15 |

- **Càng lên càng khó:** điểm gốc nhân theo dải hảo cảm hiện tại.

  | Dải | Hệ số |
  |---|---|
  | 0–30 | ×1,0 |
  | 30–50 | ×0,6 |
  | 50–80 | ×0,35 |
  | 80–100 | ×0,2 |

- **Cổng mốc:** muốn vượt 50 phải xong nhiệm vụ riêng bước 2; muốn vượt 80 phải xong bước 3. Thanh dừng ở 49 hoặc 79 cho tới khi xong.
- **Ước lượng thời gian:**
  - Cam đạt 30: bước 1 (+10) + 4 việc nhờ = 4 chu kỳ ≈ 32 ngày.
  - Từ 50 lên 80: cần (30 ÷ 0,35) ≈ 86 điểm gốc ≈ 14 kì hợp đồng. Đúng tinh thần "dài hơi".
- **Lợi ích từng mốc:**

  | Mốc | Lợi ích |
  |---|---|
  | 30 | Thuê được mọi bậc; NPC báo trước 1 ngày khi hợp đồng sắp hết |
  | 50 | Hiệu suất +10% (TẠM); **đủ điều kiện mở đặc tính chuyên môn** |
  | 80 | Hiệu suất +20% (TẠM) |
  | 100 | Hiệu suất +30% (TẠM); "Tri kỷ": bỏ qua 1 lần trễ thanh toán mỗi hợp đồng |

  "Hiệu suất" của lính là HP và sát thương. Của NPC trại là sản lượng ca trực (Nông Công: Sinh Cảnh; Bác Sĩ: số heo chữa; Kỹ Sư: hiệu suất Phòng Xây Dựng).

## 4. Giá theo năng lực và thị trường

- **Giá nền mỗi chu kỳ nhỏ**, định khi mở:
  - NPC trại = lương GDD 11.1 × 8 ngày. 8 ngày là chu kỳ mẫu giữa game ở GDD 22.
  - Lính = giá trị trao đổi (GTTC) của thứ GDD 11.2 đòi.
- **Lính cần một thang chung.** GDD chỉ quy được ra vàng cho Vệ Binh Khiên (3 heo trưởng thành × 491G = 1.473G). Các lính khác đòi đồ chưa có giá chuẩn. Vì vậy giá lính = điểm trọng yếu × 655G (thang của Vệ Binh). Các NPC trại thì quy ra 270–545G/điểm. Thành ra lính "đắt hơn NPC mở bằng hảo cảm" đúng như luật.

| Vai trò | Phẩm chất | Giá nền / chu kỳ nhỏ | Nguồn |
|---|---|---|---|
| Nông Công | Trắng | 480G | 60G × 8 |
| Thủ Kho | Lục | 800G | 100G × 8 |
| Nhà Buôn | Lục | 1.200G + 2% hoa hồng | 150G × 8 |
| Quản Lý Ca | Tím | 960G | 120G × 8 |
| Bác Sĩ Thú Y | Tím | 1.280G | 160G × 8 |
| Kỹ Sư Xây Dựng | Cam | 200G/ngày × 13–15 ngày/chu kỳ lớn | GDD |
| Nhà Nghiên Cứu Gen | Cam | 220G/ngày × 13–15 ngày/chu kỳ lớn | GDD |
| Vệ Binh Khiên (lính) | Lục | 1.473G | 3 heo trưởng thành |
| Kỹ Sư Chiến Trường (lính) | Lục | 1.640G | 2,50 × 655 |
| Xạ Thủ Nỏ (lính) | Lam | 1.770G | 2,70 × 655 |
| Thích Khách Đêm (lính) | Lam | 2.100G | 3,20 × 655 |
| Thợ Săn UFO (lính) | Tím | 2.460G | 3,75 × 655 |

- **Giá theo đợt** (chỉ lính) = giá chu kỳ ÷ 2,5. GDD 2.3: giữa game mỗi chu kỳ có 1 đợt lớn + 1–2 đợt nhỏ, trung bình 2,5 đợt.
- **Biến động mỗi chu kỳ:** giá chốt lúc chu kỳ mới bắt đầu, ký trong chu kỳ nào thì giữ giá chu kỳ đó. Tính theo chủ đề chợ (GDD 20.2). Biên độ TẠM:

  | Chủ đề chợ | Ảnh hưởng |
  |---|---|
  | Hoảng Loạn Dịch | Bác Sĩ +25 → +40% |
  | Mùa Công Trình | Kỹ Sư Xây Dựng, Kỹ Sư Chiến Trường +15 → +25% |
  | Khan Hiếm Hậu Đột Kích, Mùa Trang Bị | mọi lính +15 → +30% |
  | Cơn Sốt Thịt | Nhà Buôn +15 → +25% |
  | Mùa Heo Rẻ | Nhà Buôn −10 → −20% |
  | Khan Hiếm Thức Ăn | Thủ Kho, Nông Công +10 → +20% |
  | Mọi chủ đề, mọi vai trò | dao động nền ±5% |

- **Năng lực được thị trường công nhận:** mỗi kì hợp đồng NPC làm trọn thì giá nền +3% (TẠM), tối đa +30%. NPC càng nhiều kinh nghiệm, thị trường trả càng cao. Không liên quan hảo cảm.
- **Kiểm tra ngân sách** với chu kỳ mẫu 8 ngày, thu 24.000G:
  - Bộ đầu-giữa game (Nông Công + Thủ Kho + Bác Sĩ) = 2.560G/chu kỳ. Khớp mức "lương NPC 2.900G" của GDD 22.
  - Thêm Nhà Nghiên Cứu Gen: 2 chu kỳ lớn ≈ 28 ngày × 220G = 6.160G, cọc 1.848G. Một khoản "đầu tư lớn".
  - So với lính bậc thấp: Vệ Binh 1.473G/chu kỳ ≈ 184G/ngày; Nhà Nghiên Cứu Gen 220G/ngày (+20%). Khoản cam kết tối thiểu gấp khoảng 4 lần.
  - **Chờ người dùng xác nhận:** người dùng muốn nghiên cứu "cao hơn khá nhiều" lính bậc thấp. Nếu mức +20%/ngày là chưa đủ, cần chốt số mới cho Cam (GDD đang ghi 220G/ngày).

## 5. Hợp đồng

| Kiểu | Áp dụng | Kì hạn | Trả tiền |
|---|---|---|---|
| Theo chu kỳ nhỏ | NPC Trắng–Tím, lính | 1–3 chu kỳ quái (kết thúc ở đợt lớn) | Cọc 30% khi ký; 70% còn lại chia đều, tự trừ ở mỗi đợt lớn kết thúc chu kỳ |
| Theo chu kỳ lớn | NPC Cam (bắt buộc), tối thiểu 2 | 13–15 ngày/chu kỳ lớn, gieo khi ký | Cọc 30% khi ký; còn lại chia đều, tự trừ cuối mỗi chu kỳ lớn |
| Theo đợt | Chỉ lính | 3 / 5 / 8 đợt quái | Trả giá đợt khi đợt bắt đầu |

- Đến hạn mà không đủ vàng: NPC nghỉ việc ngay, hảo cảm −10, mất cọc. Mốc 100 bỏ qua 1 lần.
- Chấm dứt sớm: mất cọc, hảo cảm −15.
- Hết hạn: NPC về chợ. Ký lại thì tính giá chu kỳ mới.
- Số NPC tối đa theo cấp trại (GDD 11.1), tính cả lính dài hạn: 1–4: 2 · 5–9: 4 · 10–14: 6 · 15+: 8.

## 6. Hậu quả với code

- Bỏ trừ lương hằng ngày trong ca trực (`ExecuteDailyNpcShift`). Lương nay trả theo hợp đồng.
- `RecruitmentRules.Hire` đi qua `NpcContracts.Sign`.
- Lưu: tiến độ nhiệm vụ riêng, hảo cảm, việc nhờ, hợp đồng, công nhận năng lực.

## 7. Gia hạn và ép giá (người dùng chốt 02/11)

Mỗi hợp đồng lưu **đơn giá đã chốt**:
- chu kỳ nhỏ: G/chu kỳ;
- chu kỳ lớn: G/ngày;
- theo đợt: G/đợt.

Khi gia hạn hợp đồng đang chạy:

| Thị trường hiện tại so với đơn giá đang thuê | Hảo cảm | Giá gia hạn |
|---|---|---|
| Cao hơn (thị trường đã công nhận năng lực) | ≥ 50 (TẠM) | **Giữ giá cũ** |
| Cao hơn | < 50 | Theo giá thị trường |
| Thấp hơn | không cần | Người chơi chọn: giữ giá cũ (không mất gì), hoặc **ép giá** xuống theo thị trường (hảo cảm −5, TẠM) |

- Gia hạn theo chu kỳ cũng cọc 30% phần thêm.
- Các kì trả còn lại được chia đều lại.
- Quầy tuyển hiện "Đang thuê X G · thị trường Y G". Nút "Ép giá" chỉ hiện khi Y < X.

Code: `NpcContracts.RenewalUnit`, `NpcContracts.Renew`. Test: `NpcContractTests.RunPricesAndContracts`.
