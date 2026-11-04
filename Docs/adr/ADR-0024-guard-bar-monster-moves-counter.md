# ADR-0024 — Thanh Chống Chịu, đòn quái cơ bản / đặc biệt, phản kích theo kỹ năng

Ngày: 02/11/2026 · Trạng thái: luật **chốt bởi người dùng**; số thiết kế **TẠM**, chỉnh sau khi playtest.

## Luật người dùng chốt (ghi đè GDD 8.2 "HP 100")

- Nhân vật không có HP. Thanh sống còn là **Chống Chịu**. Chống Chịu khác Thể lực.
- Đòn quái trừ Chống Chịu. Đòn chỉ trừ Thể lực khi đòn đó có cơ chế rút sức. Rút sức là thuộc tính của từng đòn, không chia theo hạng quái.
- Chống Chịu về 0 thì ngất, được kéo về hiên nhà chính hồi sức: 2 giờ game, −10% vàng, −20 Thể trạng, tỉnh lại với 60%.
- **Chống Chịu dưới 30%:** tổng Thể lực giảm 25%, tốc độ hồi Thể lực chỉ còn 6/10.
- **Lướt né** chỉ tránh được đòn cơ bản.
- **Đòn đặc biệt** không né được, chỉ phản kích được, tỉ lệ gốc 5%. Muốn tăng tỉ lệ thì dựa vào kỹ năng thuần của người chơi: tốc độ phản ứng, căn đúng nhịp animation… Phản kích là chuyện kỹ năng, đỡ đòn thường không tự phản kích.

## Số thiết kế tạm

| Mục | Giá trị | Chỗ trong code |
|---|---|---|
| Báo trước đòn cơ bản | 0,18 giây, nháy cam | `MonsterMovesets.B` |
| Báo trước đòn đặc biệt | 0,5–0,9 giây theo đòn, nháy tím, hiện "!! tên đòn" | `MonsterMovesets.S` |
| Lúc nào tung đòn đặc biệt | đã hết hồi chiêu riêng (8–12 giây) và tung xác suất 35% mỗi lần ra đòn | `MonsterMovesets.Pick` |
| Thể lực bị rút | sát thương sau giảm trừ × hệ số rút sức của đòn (0,3–1,5) | `ProcessIncomingAttack(staminaDrainRatio)` |
| Tỉ lệ phản kích | gốc 5% | `CounterResolver.BaseChance` |
| Thưởng căn đúng nhịp đòn chạm | ≤ 0,08 giây +45%; ≤ 0,15 giây +30%; ≤ 0,25 giây +15% | `CounterResolver.Chance` |
| Thưởng đọc đòn | +10% khi bấm đúng 1 lần, sau lúc quái lấy đà | như trên |
| Phạt bấm sẵn hoặc bấm loạn | bấm trước khi quái lấy đà, hoặc bấm > 1 lần trong lúc báo trước → chỉ còn 5% | như trên |
| Thưởng chuỗi phản kích | +5% mỗi lần thành công liên tiếp, tối đa +15% | như trên |
| Thưởng Phản Chấn (kỹ năng) | +20% khi cửa sổ Phản Chấn đang mở | như trên |
| Trần tỉ lệ phản kích | 90% | `CounterResolver.MaxChance` |
| Phản kích thành công | phản 150% sát thương (giống Phản Chấn), choáng quái 0,5 giây × CTRL, +1 Nộ | `PlayerCombatController.ResolveEnemyMove` |
| Phím | Q phản kích, Ctrl lướt né | như trên |

Bộ đòn của từng loại quái (`Core/MonsterMoves.cs`) dựng theo hành vi trong GDD 19.1–19.2. Ví dụ:
- Kẻ Bắt Heo có đòn đặc biệt "Quăng thòng lọng", trói lại nên rút sức.
- Thú Bão có "Gọi sét", làm tê liệt nên rút sức.
- Bầy Nhảy là quái thường nhưng cắn liên tục nên rút sức.
- Đầu Lĩnh Trộm là tinh anh nhưng không rút sức.

Test: `Tests/MonsterMoveTests.cs`, và test Deep-1 trong `Tests/Program.cs`.
