# ADR-0019 — Đàn heo, phối giống, Sinh Cảnh, sức khỏe: các số tạm (PROVISIONAL)

Ngày: 2026-11-02 · Trạng thái: PROVISIONAL — thay khi GDD/sổ tay cho số chính thức

Phạm vi: GDD v6.0 chương 3–7 (trại, đàn, di truyền, Sinh Cảnh, sức khỏe, thời tiết). Mọi số có trong GDD được dùng đúng
GDD; bảng dưới chỉ liệt kê chỗ GDD không cho số hoặc cách đọc còn mơ hồ. Mã: `HerdLifecycleRules`, `GameEngineHerdLifecycle`,
`StarterHerd`, `GeneticsAndBreedingSystem`. Kiểm thử: `Tests/HerdLifecycleTests.cs`.

## Số tạm

| # | Luật | Giá trị dùng | Lý do / nguồn | Ở đâu |
|---|---|---|---|---|
| 1 | Mốc EXP từng cấp trong một dải (GDD 3.5 chỉ cho mốc dải 0 / 1.200 / 5.000 / 16.000 / 40.000 / 95.000 / 300.000) | Chia đều tuyến tính trong dải: L2 600 · L3 1.200 · L4 3.100 · L5 5.000 · L6 8.667 · L7 12.333 · L8 16.000 · L9 24.000 · L10 32.000 · L11 40.000 · L12 53.750 · L13 67.500 · L14 81.250 · L15 95.000 · L16 129.167 · L17 163.333 · L18 197.500 · L19 231.667 · L20 265.833 | Mốc đầu dải khớp GDD; không bịa độ cong | `HerdLifecycleRules.CumulativeExpForLevel` |
| 2 | Đàn khởi đầu | 12 heo trưởng thành (20 ngày, 90–100 kg): Hồng Điền #1, Kim Thọ #1 (giữ từ bản lưu mẫu cũ), Heo Chân Trắng, 9 heo dòng Thường đực/cái xen kẽ | Truyện chỉ ghi "một đàn nhỏ" (COT_TRUYEN, cảnh ký nhận trại; Q01 không cho số); GDD 2 ghi đầu game "đàn 10–80 con". Hai tài liệu không cho số → giữ 12 như bản dựng trước | `StarterHerd.FillTo` |
| 3 | Số lần phối mỗi nái | 1 lần/nái/ngày (đậu hay hụt) | GDD không giới hạn; không có giới hạn thì 70% vô nghĩa vì bấm lại tới khi đậu | `PreviewPigBreeding` |
| 4 | Bán không kiểm dịch bị từ chối | Người mua không nhận lại con đó trong cùng ngày | GDD 4.5 cho tỉ lệ 70/65/55/45/35% nhưng không nói thất bại thì sao; không chặn thì bấm lại tới khi được | `CheckDayMarketHealthGate` |
| 5 | Phạm vi tiếp xúc khi lây 6 giờ (GDD 6.5) | 10 m (1 Ô Đất); mỗi heo bệnh trong tầm cho một lần tung | GDD cho công thức P(lây) nhưng không cho khoảng cách | `HerdLifecycleRules.ContactRadiusMeters` |
| 6 | Thể lực dọn bãi | 8 (cận dưới khoảng 8–12 của GDD 9.2) × hệ số buổi/thời tiết, +25% nếu SC < 40, +15% khi Mưa; tính tối đa 3 lần/ngày (= trần +12 SC) | GDD cho khoảng, chọn cận dưới | `HerdLifecycleRules.TryCleanYard` |
| 7 | Bệnh tiềm ẩn theo dòng (GDD 5.2 cột "Tiềm ẩn") | Tung 1 lần lúc sinh với đúng % của GDD (Thổ Trầm 1% Sốt Bùn, Xích Mao 3% Lôi Tâm, Phong Mẫu 2% RLSS, Tinh Quang 8% Nấm Phát Quang, Lôi Mạch 10% Lôi Tâm, Kim Thọ 3% RLSS); cận huyết +8 đ% (chỉ dòng có tiềm ẩn); Gen Xấu ×2 | Đọc "%" là tỉ lệ mang mầm bệnh của từng cá thể | `RollLatentDisease` |
| 8 | Khi nào bệnh tiềm ẩn bộc phát | Lôi Tâm: Tinh thần < 30 (GDD 6.4) · Nấm Phát Quang: Mưa Bức Xạ hoặc trời ẩm (Mưa, Nồm Ẩm) · Sốt Bùn: Mưa · RLSS: Tinh thần < 40 | "Tinh thần thấp kéo dài" chưa có số → dùng ngưỡng Bất an (< 40), kiểm mỗi ngày | `LatentTriggered` |
| 9 | Tranh máng ×1,10 cám | Áp cho mọi mật độ > 100% | GDD ghi ở dòng 101–120%; coi như còn hiệu lực ở các bậc cao hơn | `DensityFeedMultiplier` |
| 10 | "Bị kích động" (Tính khí, Thổ Trầm Tâm Tĩnh) | Áp cho các nguồn trong bảng 4.3 đã mô phỏng: Sương Mù (−1/giờ, Mộng Nhãn −3/giờ) và quá tải mật độ (−1/−2/−4 mỗi ngày) | GDD không định nghĩa "kích động" | `AgitationMoodLoss` |
| 11 | Heo non chết do rét/thiếu mẹ −30% | Áp cho SC ≥ 70 (cả Trong Lành) | Bảng 6.1 chỉ ghi ở dòng Ổn Định | `HabitatPigletMortalityFactor` |
| 12 | Say Nắng | Không có nguồn mát 2 giờ liền → mắc Say Nắng; Thiết Bì tích 1,5 giờ/giờ (Say Nắng ×1,5) | Đọc "×1,5" là nhanh gấp rưỡi; "gục thêm 1 giờ → chết" CHƯA làm | `TickHerdHealthHour` |
| 13 | Đột biến Mưa Bức Xạ lúc Đang lớn | Tung mỗi ngày Mưa Bức Xạ: 1,5% lên một bậc, 0,05% thành Hư Thể | GDD không nói tung một lần hay mỗi ngày | `ApplyDailyHerdPressure` |
| 14 | Bùng phát A/B khi không có nguồn thời tiết | Chuồng Nhiễm/bãi bẩn → Sốt Bùn; còn lại → Hô Hấp Lạnh ("miễn dịch đàn thấp") | Giữ cách chọn cũ cho trường hợp này | `ChooseOutbreakDisease` |
| 15 | Ưu Tú Toàn Diện/Đột Phá trên "hệ số chuyển đổi thức ăn" | Tốt hơn = cần ít cám hơn (chia cho 1,10 / 1,40); Sức khỏe trần 100 nên +% không đổi | GDD không nói chiều | `ApplyElderOffspringStats` |

## Số cũ không có trong GDD, giữ nguyên (không thuộc phạm vi việc này)

Ưu thế lai F1 +8% tăng trọng và cận huyết −10% tăng trọng; Thổ Trầm ăn −18% (`GetDailyFeedKilograms`); trần 8 ca mang thai
cùng lúc; nái Tinh thần < 20 mất cả lứa; con Chết Yểu chết ngay lúc sinh (GDD: trong 0–4 ngày); Tự Phân Rã làm cả đàn
−5 Tinh thần/ngày; Trung Thành (+3 Hòa nhập/ngày) và Sạch Sẽ (+1 Tinh thần khi nóng); "vệ sinh tiêu độc" ở Khu Xử Lý cũ giảm ÁLSK −8.

## Đã sửa theo GDD (để đối chiếu)

- Di truyền dòng: 25% bố • 25% mẹ • còn lại bảng tự nhiên 78/17/4/0,9/0,1 (trước là 55/45); Mộng Nhãn 40%; F2 = con của hai F1, đột biến +4% (+3% Mộng Nhãn).
- Thụ thai: Htuổi lấy hệ số thấp hơn của hai bên; Htrại (Hưng Thịnh +5%, Truyền Thừa +3%/tầng) nhân theo GDD 1.4 (trước cộng thẳng); bỏ "Mắn Đẻ +10% thụ thai" (GDD: Mắn Đẻ chỉ +1 con/lứa); Rối Loạn Sinh Sản là phối hụt, không báo bệnh.
- K Hậu duệ heo già gieo một lần mỗi lứa trong [22%; 32%]; Thể Trạng Yếu (Sức khỏe tối đa 70, dễ lây ×1,3); Gen Xấu (phụ xấu + bệnh tiềm ẩn ×2); phạt 5 chỉ số áp đúng chỉ số.
- Ăn Khỏe: cám cho 1 kg −6% (trước: ăn +10%, lớn +12%). Kén Ăn chỉ phạt ngày đổi cám (chưa có hệ đổi cám → không phạt).
- Hút Sương thức: ÁLSK +Vừa (+3) thay vì +6; Hư Nhiễm thức: mang Hư Mạch (+5 tức thời, +1/ngày) thay vì +8/ngày.
