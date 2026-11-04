# ADR-0017 — Số tạm: trang bị, võ kỹ, cấp nhân vật

Ngày: 02/11/2026 · Trạng thái: TẠM. Thay khi GDD có số.

GDD chương 8.2 và chương 12 được làm theo nguyên văn ở các phần sau:
- 6 ô trang bị và 3 cặp đồ mở ô Công, Thủ, Biến.
- Hạng cân và lối chơi.
- 16 món đồ với hạng cân, chỉ số Thủ và sát thương vũ khí.
- 24 kỹ năng: ô, tiêu hao, hồi chiêu, yêu cầu chỉ số gốc.
- Chi phí học võ: vàng + Điểm Võ.
- Luật phái chính −15%, phái liên kết, phái khác.
- Hợp kích theo cặp chiêu.
- Nộ: +2 / +10 / +1, giảm 5/s, về 0 cuối đợt.
- Điểm Võ.
- Cấp 1–30, +2 điểm và +3 HP mỗi cấp, +8 Thể lực mỗi 5 cấp, trần 40/55.
- Công thức sát thương nhận.

Các chỗ dưới đây GDD không ghi số hoặc ghi chưa rõ, nên dùng giá trị tạm:

| Luật | Giá trị dùng | Chỗ trong code |
|---|---|---|
| EXP lên cấp | 100 × cấp hiện tại | `MartialProgression.ExpToNext` |
| EXP hạ quái ("2–400") | thường 2, tinh anh 40, Boss 400 | `KillExp` |
| EXP sống sót đợt quái | 20 × số chu kỳ | `WaveSurvivalExp` |
| Đồ khởi đầu | chỉ Dao Rựa (người hiện đại, vũ khí nhẹ) | `CharacterData.EquipStarterSet` |
| Giá đồ bậc I, mua ở Chợ Thị Trấn 06–18 | Nhẹ 150G / Vừa 300G / Nặng 500G | `PriceGold`, bảng Nhân vật |
| Tên nỏ | 40 tên, nạp đầy mỗi sáng | `PlayerCombatController.Arrows` |
| Nhịp bắn Nỏ Tay | 1,0 giây; tầm 12 m | danh mục và `FireBolt` |
| Chỉ số quyết định sát thương | Nặng → STR, Nhẹ → AGI, Tầm xa → CTRL, Giáo Móc (Vừa) → trung bình STR/AGI; +1%/điểm | `WeaponHitDamage` |
| Hệ số chí mạng | ×2 | `SyncWeaponFromEquipment` |
| Tầm và góc đánh | Dao 1,4 m 130°; Giáo 2,1 m 40°; Búa 1,3 m; tay không 1 m, 5 sát thương | như trên |
| "Phái liên kết" | ĐÃ SỬA theo GDD 12.6 sau khi đọc trọn tài liệu: chỉ phái **kế tiếp** trên vòng một chiều (A→C, C→B, B→D, D→A) | `Relation` |
| "Trái lối chơi" | mọi phái không hợp lối chơi đang dùng (+15% Thể lực) | `StaminaFactor` |
| Cửa sổ hợp kích | 2 giây cho cả 4 cặp (GDD chỉ ghi 2 giây cho Ảnh Móc) | `ComboWindowSeconds` |
| "Điều kiện hành vi" của phái liên kết | chưa xét | `CanLearn` |
| Bí Kíp | chưa có nguồn trong game, nên tuyệt kỹ và kỹ năng phái khác còn khóa | `SecretManuals` |
| (đã bỏ) HP người chơi | thay bằng thanh Chống Chịu theo quyết định người dùng | `ProcessIncomingAttack` |
| Phá Giáp "giáp −25%" | quái nhận thêm sát thương ×1/(1−0,25) trong 6 giây | hook `ISkillReactive.ApplyArmorBreak` |
| Ô trống khi tính tổng hạng cân | tính 1 điểm | `GetTotalWeightScore` |

Đã bỏ các luật cũ không có trong GDD:
- Bị đánh trúng thì được cộng Nộ.
- Hạ quái nào cũng +15 Nộ.
- Trọng Giáp giảm 20% sát thương nhận.
- Hợp kích tính theo phái.
- Bộ đồ khởi đầu 6 món tự đặt.
- Huyết Tế trả bằng Chống Chịu thay vì HP.
- Ba nút võ kỹ cứng: lướt chém, hồi 25 HP, chấn 30 Nộ.

Bổ sung ngày 02/11, sau khi đọc trọn GDD, HANDBOOK và cốt truyện:

| Luật | Giá trị dùng | Chỗ trong code |
|---|---|---|
| Ô thứ 4 (Mệnh Ấn Song Thế) trả bằng Nộ | 1 Thể lực của kỹ năng = 1 Nộ | `SongTheRageCost` |
| Lướt né | quãng 3 m, miễn sát thương 0,25 giây, phím Ctrl | `PlayerCombatController.ExecuteDodge` |
| CTRL "hiệu lực choáng / làm chậm" | +0,2%/điểm thời gian khống chế (cùng hệ số chí mạng) | `ControlSeconds` |
| Trọng Giáp "choáng ngắn" của đòn nặng | 0,3 giây (theo số choáng của Búa Tạ) | `PerformHitDetection` |
| Phù Truy Phong "vệt làm chậm" | chậm 20% trong 3 giây, rộng 1 m (theo Xuyên Bầy) | `DodgeRoutine` |
| Bom Keo "vũng keo 3 m, 5 giây" | chậm 60% (theo Hố Keo) | `UseQuickItem` |
| Lựu Khói | quái trong 4 m bị kéo sang một mồi nhử cách 6 m trong 3 giây | `UseQuickItem` |
| Túi nhanh | giới hạn 3 cái mỗi loại áp dụng mọi lúc, không chỉ khi vào đợt quái | `QuickBag` |
| Phím túi nhanh | F5–F8 | `MartialSkillRunner.Update` |

Hiểu đúng theo GDD 12.6: mỗi phái chính có một hợp kích, đi từ kỹ năng phái mình sang kỹ năng phái liên kết. Ảnh Móc và Phản Thủ ghép hai chiêu cùng loại ô (Công–Công, Thủ–Thủ), nên chỉ nối được khi có ô thứ 4 từ Ấn Song Thế.

Chưa làm:
- Phẩm chất đồ I–IV (chưa có nguồn bán đồ bậc II–IV).
- Khắc Phù Văn và Mệnh Ấn từ giao diện (chưa có bản vẽ Bàn Khắc). Hiệu ứng trong trận của Truy Phong, Nộ Hỏa, Kiên Thạch, Song Thế, Mục Đồng đã chạy khi có phù hoặc ấn.
- Phù Hộ Đàn.


**Quyết định của người dùng (02/11, ghi đè GDD 8.2 "HP 100"):** nhân vật KHÔNG có HP. Thanh sống còn là Chống Chịu: bị đánh thì trừ Chống Chịu và trừ Thể Lực; Chống Chịu hoặc Thể Lực về 0 thì ngất và được kéo về hồi sức. Các hiệu ứng "HP" trong GDD đều áp lên Chống Chịu:
- Huyết Tế hiến Chống Chịu;
- Băng Gạc hồi Chống Chịu;
- sét đánh trừ Chống Chịu;
- +3 mỗi cấp cộng vào Chống Chịu tối đa;
- mỗi Mệnh Ấn −10 Chống Chịu tối đa;
- Ấn Bất Khuất giữ 15 Chống Chịu.

Test: `Tests/MartialProgressionTests.cs`.

## Bổ sung 02/11 (vòng nối logic ↔ UI)

| Mục | Quyết định tạm | Lý do |
|---|---|---|
| Nguồn bản vẽ Bàn Khắc | Sự kiện Đỏ (E10–E14) đầu tiên hoàn thành sau Cổng Giữa Game trao bản vẽ | GDD liệt kê sự kiện Đỏ là nguồn bản vẽ Cao cấp nhưng không ghi tỉ lệ rơi. Đấu giá đêm và Bí nhân chưa bán Bàn Khắc vì GDD không ghi giá |
| Đếm "đã mở Phù Văn ≥ 5 chu kỳ" | Tính từ chu kỳ đầu tiên mà mọi điều kiện Phù Văn cùng đạt | Đúng câu chữ GDD 12.4 |
| Nguyên liệu khắc (Mực Phù; Huyết Triện + Tinh Hoa Dị Biến) | Chưa trừ | GDD không ghi số lượng. Chờ số chốt |
| Nhánh nâng cấp tháp | Cần Phòng Xây Dựng cấp 2, 1 nhánh/công trình, chưa có giá. Bỏ "+400 HP" bịa | GDD 16.1 không ghi giá và không cộng HP |

## Addendum 2 Nov 2026 (night) — gear quality I–IV and its sources (GDD 12.3 / 12.5 / 20)

- **Quality bonus** follows GDD 12.3 exactly. A piece's bonus stat becomes +1 / +3 / +5 / +8 at quality I / II / III / IV. "Thủ" and weapon damage do not change.
- **Mechanic strength per quality is not modelled.** The GDD says quality raises "độ mạnh cơ chế" but gives no numbers.
- **Owned gear stores its quality in the id**, e.g. `wpn_dao_rua#III`. Quality I has no suffix, so old saves load unchanged.
- **Sources**, per GDD 12.5 / 20:
  - Day market: gold, quality I only.
  - Night market: every piece at quality II, plus quality III. Gear keeps the share the old 6 gear stalls had; III and above stays at or below 2% of stalls.
  - Night auction: one quality III–IV lot each night, next to the 3 blueprint lots.
- **TẠM prices.** The GDD gives no prices above quality I, so these are provisional until balance is decided:
  - price = quality-I price (150 / 300 / 500 by weight, this ADR) × the quality's stat bonus. That is ×3 for II, ×5 for III, ×8 for IV.
  - The auction is quality IV on 1 night in 4.
- **Still open:** GDD 12.4 says runes go on "món trang bị bậc II trở lên, tối đa 1 Phù Văn/món". Engraving is still per character, not per piece.
