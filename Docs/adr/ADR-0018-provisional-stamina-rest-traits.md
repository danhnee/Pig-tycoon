# ADR-0018 — Thể lực, ngủ, đặc tính cơ bản và 6 khóa: các giá trị tạm (PROVISIONAL)

- Ngày: 2026-11-02
- Trạng thái: PROVISIONAL (chờ thiết kế xác nhận)
- Phạm vi: GDD_PIG_MECHANICS.md mục 8.1 (hai đặc tính cơ bản), chương 9 (Thể lực), chương 10 (6 khóa).

## Bối cảnh

Bản dựng chạy thật bỏ qua mô phỏng khi ngủ, cho hồi đầy mọi Thể lực ngoài combat, chưa có Mắt Nhà Nghề,
tiếng huýt của Khoa là lệnh lùa về Nơi Trú 25 m cho cả hai nhân vật, và 6 khóa chuyên biệt tự mở theo cấp trại.
Bản sửa giữ đúng số trong GDD. Những chỗ GDD không cho số được chọn tối thiểu như dưới đây.

## Số lấy thẳng từ GDD (không phải tạm)

- 9.2: rải thức ăn 4 (100%, +20% nếu bao > 20 kg); thay nước bồn 6 / dội xô 3 (100%, Nắng Gắt ×1,15);
  tiêm thuốc 2 (+50% heo hoảng); kéo xác 12; nạp đạn 5; gom vật liệu 8 (80%); đi chợ 5 (+2/con heo).
- 9.3: hệ số Buổi −5/+10/0/+5%, thời tiết +15/+10/+20/+10/+5/−3%, Thể trạng 0/5/10/20/35%,
  mang vác > 35 kg +35% (+15% nếu Thể trạng < 40), kẹp ×0,60–×2,00; thời tiết Cố định bỏ hệ số Buổi.
- 9.4: hồi 10/giây ngoài combat, 2/giây trong combat; ngồi nghỉ 6/giờ game; bữa thường +20, bữa tốt +35, tối đa 3 bữa/ngày;
  Tải ngày > 55% → Thể trạng −8; > 75% → −15 và 20% Kiệt Sức.
- 9.5: cooldown ngủ 6 giờ; Stamina ≥ 80%, xóa Tải ngày; Mệt Dội −8% Stamina tối đa và −15% hồi Thể trạng mỗi tầng, tối đa 4,
  giảm 1 tầng mỗi ngày sạch; không ngủ trong đợt quái, Sương Mù, hoặc khi có quái trong ranh giới trại.
- 8.1: huýt sáo tối đa 12 heo trong 20 m, đi theo 60 giây thực, không tốn Stamina, hồi 2 giờ game;
  Mắt Nhà Nghề quan sát 5 giây; 1 lần/ngày kiểm bệnh tiềm ẩn miễn phí thay kiểm dịch 60G.
- 10.4–10.5, 10.8: lộ khóa theo thứ tự bằng giấy, tỉ lệ theo bảng, trần 50% tiến độ trước khi lộ; điều kiện từng khóa;
  hiệu ứng chỉ khi đủ 6.

## Quyết định tạm (PROVISIONAL)

| Chỗ | Giá trị chọn | Lý do |
|---|---|---|
| Dọn bãi / vệ sinh 1 ô (GDD 8–12) | 10 | Giữa khoảng GDD; trước đó runtime cũng trừ 10. |
| Thao tác xây/sửa nhanh (đặt/tháo rào 5, máng ăn/nước 8, tháo tháp 12) | giữ số runtime cũ, 100% vào Tải ngày | GDD chỉ cho "phụ Phòng Xây Dựng 10/giờ", không có số cho thao tác tức thì. |
| Chải heo (Bàn Chải) | 4, 100% Tải ngày | Không có trong bảng 9.2; giữ số runtime cũ, xếp nhóm Chăm sóc. |
| Múc giếng, xúc cám ở kho | 0 (tính khi đổ vào bồn/máng) | Bảng 9.2 tính trọn việc "thay nước bồn"/"đổ máng", không tách bước múc. |
| Buổi Tối | luôn coi là "không đèn" (+5%) | Core chưa biết vị trí đèn. |
| Thể trạng khi ngủ chuẩn | +6 (trong khoảng +4..+10); Miễn dịch +5 (trong +3..+8) | Giữ số cũ, nằm trong khoảng GDD. |
| "20% Kiệt Sức" | đọc là xác suất 20%; Kiệt Sức = coi như Vùng mệt (GDD 9.4: chạy −15%, đòn nặng chậm +20%) tới lần ngủ chuẩn kế tiếp | GDD không định nghĩa hiệu ứng riêng của Kiệt Sức. |
| "Ngày sạch" (Mệt Dội) | ngày không bị phạt quá sức và không nghỉ cưỡng bức bằng vật phẩm (trừ Quả Mộng) | GDD không định nghĩa. |
| Qua 06:00 mà không ngủ | không hồi Thể lực, không xóa Tải ngày (chỉ chốt phạt quá sức và Mệt Dội) | GDD 9.1: Tải ngày chỉ hồi bằng ăn, nghỉ dài hoặc ngủ. |
| Bị đánh thức giữa giấc (đợt quái nhỏ nổ ra) | dừng ở giờ đó, hồi như ngồi nghỉ 6/giờ, không xóa Tải ngày, không tính cooldown | GDD không nói; đợt quái nhỏ không thể bị ngủ qua. |
| Bữa ăn / nghỉ dài | hồi trả Tải ngày trước, rồi tăng Thể lực cùng lượng | Để bữa/nghỉ thật sự hồi phần "ghi nợ" như GDD 9.1. |
| Mắt Nhà Nghề — "dấu hiệu ẩn" | lần lượt: mầm bệnh tiềm ẩn (LatentDiseaseRiskMultiplier > 1,05), sức đề kháng kém (InfectionRiskMultiplier > 1), từng tính chất phụ khi heo chưa giám định | Dùng đúng dữ liệu ẩn mà PigInspectPopup chưa hiện. |
| Kiểm bệnh miễn phí | con được kiểm (sạch hay bệnh) được miễn phí kiểm dịch ở lần bán có kiểm dịch kế tiếp | "Thay cho kiểm dịch 60G". |
| Huýt sáo — heo hoảng loạn | không đi theo (heo hoảng chạy theo logic hoảng) | GDD không nói. |
| Khóa 1 của Khoa "nhà chính không bị phá" | đếm mọi đợt lớn sống sót | Runtime chưa có cờ "nhà chính bị phá". |
| Khóa 2 của An "dùng 40 lượt hỏi" | có API `RecordIntelQuestion`, chưa có hành động hỏi trong runtime | Bàn Tình Báo chưa có giao diện hỏi. |
| Khóa 4 của An "≥ 1 Huyền Thoại hoặc Dị Biến" | heo định danh có độ hiếm ≥ Huyền Thoại, xét sau khi khóa 4 lộ | Điều kiện định tính, không có tiến độ. |
| Hiệu ứng "Ngủ Say" — "cứ 2 ngày xóa 1 tầng Mệt Dội" | mỗi 2 lần ngủ chuẩn xóa 1 tầng | Đếm theo đêm ngủ. |
| An +5% giá bán (ProtagonistNegotiationBonusMultiplier 1,05) và −10% phí kiểm dịch | chỉ có hiệu lực khi đủ 6 khóa | Số cũ không có trong GDD 10.8; giữ lại nhưng chặn tới khi đủ 6, cần thiết kế xem lại. |

## Hệ quả

- Ngủ dùng `PlayerRestCommands.Sleep(engine, monstersInsideFarm)` và tua qua `GameEngine.Tick`.
- Việc trại trong runtime đi qua `FarmLaborStamina.TrySpend` (tức thời + Tải ngày + EXP lao động GDD 8.2).
- `CharacterData.GetEffectiveMaxStamina()` vẫn dùng công thức cũ (−18/tầng Mệt Dội, hệ số Thể trạng); hồi phục
  (`TickCombatRegen`) đã chuyển sang `StaminaSystem.EffectiveMaxStamina` (GDD: −8%/tầng). Chủ sở hữu Character.cs cần
  đổi `GetEffectiveMaxStamina` sang công thức GDD để HUD khớp.
