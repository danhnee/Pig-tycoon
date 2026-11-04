# ADR-0014: Đưa lõi về đúng số GDD v6.0 (lương NPC, ô thú cưng, thời tiết, Bí nhân, đợt quái)

- **Ngày:** 01–02/11/2026
- **Người làm:** Claude
- **Bối cảnh:** chơi thử bản 2D. Theo quy tắc AGENTS.md: "Khi hai bên lệch, giữ số trong GDD và ghi ADR."

## Lệch đã sửa

| Hạng mục | Trước | GDD v6.0 | Nguồn |
|---|---|---|---|
| Lương NPC trang trại | Quản Lý Ca 60, Nhà Nghiên Cứu Gen 70, Bác Sĩ Thú Y 65, Kỹ Sư Xây Dựng 65, Nhà Buôn 55, Thủ Kho 50, Nông Công 40 G/ngày | 120, 220, 160, 200, 150 (+2%), 100, 60 G/ngày | §11.1 |
| Ô thú cưng | Cố định 2 | 2 ô từ Cấp trại 3, 4 ô từ Cấp 8, 6 ô từ Cấp 14 | §11.3 |
| Thời tiết | Không bao giờ đổi (chỉ tính trạng Hút Sương gọi sương mù) | Mỗi đợt kéo dài 1–3 ngày, rút theo tần suất bảng 7.2; Mưa Bức Xạ chỉ từ chu kỳ 6 | §7.2 |
| Bí nhân | Gieo 3% mỗi giờ đêm (thực tế khoảng 30% mỗi đêm), không bao giờ rời đi | Gieo 1 lần mỗi đêm: 3% × hệ số chu kỳ × (1 + ÁLSK/100), tối đa 25%; xuất hiện trong khung 18:00–04:00 và ở đúng 2 giờ | §21 |
| Đợt quái | Chỉ 3 loại; mọi loại khác sinh ra thành Kẻ Bắt Heo; không có Boss hay khu hướng | 14 quái, Boss mỗi 5 chu kỳ (+15% HP mỗi lần quay lại), 3–5 làn cách nhau 25–40 giây, khu hướng gieo lại mỗi chu kỳ | §2.3, §3.8, §19 |

## Chưa theo GDD (ghi lại để làm sau)

- **Lính đánh thuê và thú cưng:** GDD ghi giá thuê bằng vật phẩm cụ thể (Giáp Da, tên, pin…). Các vật phẩm này chưa có mã trong danh mục, nên chưa thuê được qua trao đổi.
- **Nhà Buôn:** phần +2% doanh số chưa tính.

## Kiểm chứng

`dotnet run --project Tests/PigTycoon.Runner.csproj`: 96/96 PASS, gồm các test mới:
- WeatherTests
- MysticMerchantTests
- WaveApproachTests (gồm RunRosterRulesGdd19)
- MarketTests
