# Đọc trọn GDD v6.0 (Pig.docx) — phân tích từng chương so với game hiện tại

Người đọc: Claude, ngày 02/11/2026, theo yêu cầu của người dùng: "đọc từ đầu full toàn bộ text, đọc và phân tích hết".

Nguồn đã đọc: bản text trích từ `Docs/Pig.docx`, 3.084 dòng, đọc tuần tự từ dòng 1. Mỗi chương ghi các luật cốt lõi, rồi đối chiếu với code sau đợt sửa sáng 02/11 (bốn luồng C, S1, S2, S3).

Ký hiệu: ✅ có và đúng · 🟡 có một phần hoặc sai số · ❌ thiếu.

## Phần I — Tổng quan (dòng 66–287)

Triết lý của game là "Luật tối thiểu — Tác động chéo tối đa": mỗi quyết định phải gây hậu quả ở ít nhất hai hệ. Đây là thước đo để chấm từng cơ chế bên dưới.

- **5 trụ cột:**
  - tác động chéo;
  - ngân sách là van khóa: công trình mạnh nhưng giới hạn bởi đạn, dầu, hao mòn, hồi chiêu;
  - nhịp combat với công trình: Đánh → Giữ chân → Rút lui → Trụ bọc lót → Tái giao tranh;
  - thông tin là hàng hóa;
  - khám phá có giá.
- **6 lớp tương tác:** đàn, thị trường, thông tin, thời gian, tấn công, phòng thủ.
- **Phạm vi 1.0:** 2 nhân vật, 7 NPC trại, 5 lính, 7 thú cưng, 12 dòng gen, 4 phái / 24 kỹ năng ("mỗi kỹ năng một động từ riêng"), 31 công trình, 14 loại quái / 3 Boss, 10 thời tiết, 10 mối đe dọa sức khỏe, 5 vật phẩm phá hồi chiêu ngủ, 14 sự kiện huyền bí.
- **Thang thời gian:**
  - 1 ngày game = 16 phút thực; 1 giờ game = 40 giây.
  - Chu kỳ: 6–8 ngày đầu game, 7–10 giữa game, 8–12 cuối game; 8% là "Mùa Kinh Tế Vàng" dài 12–15 ngày.
  - Trong đợt quái, đồng hồ ngày dừng.
  - 🟡 Cần kiểm lại tốc độ đồng hồ thật trong game.
- **Đợt lớn và đợt nhỏ (2.3):**
  - Đợt lớn: 3–5 làn, Boss mỗi 5 chu kỳ, cảnh báo trước 1–2 ngày, dài 4–9 phút.
  - Đợt nhỏ: 15–30% điểm đe dọa, 1 làn, tối đa 1 tinh anh từ chu kỳ 6.
  - Thứ tự ưu tiên mục tiêu: Đàn → Kho → Hạ tầng → Nhà chính → Người chơi.
  - Điều chỉnh độ dài chu kỳ: ÁLSK ≥ 60 thì −1 ngày; thua nặng thì +1 ngày; cấp trại tăng ≥ 2 thì điểm đe dọa +10%.
  - 🟡 Phần điều chỉnh chu kỳ chưa kiểm.
- **Giai đoạn chơi:** đầu game có đàn 10–80 con; giữa game là sau Cổng Giữa Game; cuối game từ chu kỳ 20+, cấp trại 15–20.

## Chương 3 — Không gian trại (dòng 289–411)

- Thả rông có ranh giới; rào và cổng là tuyến phòng thủ đầu tiên. ✅
- **Sức chứa** = MIN(Ô Đất×8; Mái trú×6; Bồn×12; Máng×10) − xác nằm ngoài Khu Xử Lý.
  - Phí duy trì mỗi ngày: Ô Đất 4G, Mái trú 6G, Bồn 3G, Máng 5G.
  - Khu Xử Lý có 4 ô tập kết, nâng lên 8.
  - 🟡 Số mái trú và ô đất đang cố định; ❌ chưa thu phí duy trì mỗi ngày; ❌ Khu Xử Lý chưa có ô tập kết; ❌ giao diện chưa chỉ ra điểm nghẽn.
- **Mật độ (3.3):** 5 vùng, hệ số dịch nội suy mềm. Dưới 70%: tăng trọng +3%, Tinh thần +10%, quái chú ý −10%, nhưng vẫn trả phí hạ tầng bỏ trống.
  - S2 đã làm: Tinh thần −1/−2/−4 và thức ăn ×1,10.
  - ❌ Thiếu hiệu ứng "quái chú ý" và "hoảng loạn lan".
- **ÁLSK 0–100:** cập nhật lúc 06:00; các mức +Thấp/+Vừa/+Cao/Tức thời; giảm tự nhiên −8%/ngày; 4 trạng thái kèm tỉ lệ sự kiện.
  - 🟡 Đang cập nhật lúc nửa đêm; S2 đã đưa lên màn F.
- **Cấp trại 1–20:**
  - EXP từ bán heo (1/100G), sống sót đợt quái (150×chu kỳ, tối đa 3.000), hợp đồng, định danh gen, xây công trình.
  - Bảng mở khóa theo cấp.
  - ✅ S2 đã làm bảng EXP; ❌ các mở khóa theo cấp chưa chặn thật trong game (chợ đêm cấp 3, phòng nghiên cứu cấp 8…).

## Chương 4 — Vòng đời heo (dòng 412–672)

- **4 giai đoạn:** non 0–4, đang lớn 5–10, trưởng thành 11–28, già 29+. Cân nặng và thức ăn theo từng giai đoạn. ✅
- **Chỉ số heo:** tăng trọng, trần 110 kg, hạng thịt C/B/A/S, thụ thai 70%, 8 con/lứa, di truyền 25%, tuổi thọ 40, sức khỏe, tính khí, Tinh thần, Hòa nhập, Độ bám đàn.
  - 🟡 Hành vi theo Độ bám đàn chưa có: dưới 40 thì lạc ra rìa trại, trên 70 thì bám thủ lĩnh.
- **Tinh thần, 4 trạng thái:**
  - Bất an: lùa tốn +20% Stamina.
  - Hoảng sợ: chạy ngẫu nhiên, không vào chuồng.
  - Hoảng loạn: húc rào 5 sát thương/giây, kéo Tinh thần con xung quanh xuống, 5%/giờ phá rào thoát ra.
  - Nguồn giảm và nguồn hồi có số theo giờ.
  - 🟡 Đang thiếu nguồn theo giờ: quái trong 20 m −8, nổ −12, sét −10, thấy xác −2/giờ, yên tĩnh +5/giờ. ❌ Hành vi húc rào và phá rào khi Hoảng loạn.
- **Sinh sản:** ✅ S2 làm F1/F2 25/25, F2 +4%, cận huyết, Mưa Bức Xạ. 🟡 Mưa Bức Xạ +1,5% chưa kiểm.
- **Giá bán:** công thức chính thức. ✅ Kiểm dịch 60G; ✅ S2 làm tỉ lệ bán khi không kiểm dịch; ❌ phạt sức mua −10% trong 3 ngày khi bán heo bệnh.
- **Bầy Đàn:**
  - 7 điều kiện, 3 bậc, tan rã.
  - Thủ lĩnh với tỉ lệ hình thành; Neo tinh thần trong 20 m; mất thủ lĩnh thì −25.
  - Kế vị, Bồi Dưỡng Kế Vị, Truyền Thừa 3 tầng.
  - 🟡 Core có; Neo đang áp toàn đàn thay vì trong 20 m; ❌ chưa có giao diện Bồi Dưỡng Kế Vị.
- **Heo già và xác:** thiêu hủy 45G+8L trong 6 giờ, tái chế 80G+10L trong 18 giờ, bán cho Bí nhân. ✅ S2 làm thẻ xác; ❌ chưa có thời gian xử lý 6/18 giờ và ô tập kết.

## Chương 5 — Gen (dòng 673–893)

- **6 bậc hiếm:** 78/17/4/0,9/0,1/0%. ✅
- **12 dòng gen**, mỗi dòng có cơ chế riêng, mặt trái và bệnh tiềm ẩn. S2 đã làm thêm 4 dòng. Còn thiếu:
  - ❌ Lôi Mạch "Báo Động Bầy": tự dẫn 8 heo về mái trú.
  - ❌ Mộng Nhãn "Giấc Báo": tình báo miễn phí.
  - ❌ Xích Mao: nghiên cứu Thuần Hóa 1.200G.
  - ❌ Phong Mẫu: nhận nuôi heo mồ côi trong 10 m.
- **Tính chất phụ:** 0/1/2 phụ theo tỉ lệ 40/45/15%, có hành vi riêng. ✅ S2 đã tung tính chất. ❌ Phần hành vi chưa có: Trung Thành, Tò Mò, Kêu Báo, Ồn Ào… Có cách khắc chế sau khi định danh.
- **Dị Biến (Hư Thể):** ✅ thức tỉnh và phong ấn.
- **Giám định:** 4 kênh với tỉ lệ, giá và thời gian riêng; khóa 12/24 giờ; thất bại được +5%; sàng lọc thú y 120G. ✅ S2 sửa RNG. ❌ Chợ đêm "Giám Định Cấp Cao" xuất hiện 12% số đêm.

## Chương 6 — Sinh Cảnh và dịch bệnh (dòng 894–1017)

- 5 bậc SC kèm hệ số; nguồn tăng và giảm SC; tự trôi về 60. ✅ S2.
- **5 cách xử lý sức khỏe:** cách ly (rào tạm 30G), xử lý môi trường (vôi 20G/ô), điều trị cá thể, phòng ngừa đàn (vắc-xin 50G), nghiên cứu. ❌ Chưa có giao diện cho cách ly, vôi và vắc-xin.
- **10 mối đe dọa**, mỗi bệnh có cơ chế riêng:
  - Hô Hấp: nghịch lý mái trú.
  - Ghẻ: tái nhiễm từ ổ lót.
  - Dịch Tả: phong tỏa 3 ngày.
  - Sốt Bùn: biến bãi bùn thành ổ nhiễm.
  - Chuồng Nhiễm.
  - Say Nắng: heo đứng lì, gục rồi chết.
  - Rối Loạn Sinh Sản.
  - Nấm Phát Quang: giữ lại để bán.
  - Lôi Tâm: báo trước 10 giây.
  - Hư Mạch.
  - 🟡 Hầu hết mới là dòng chọn bệnh theo nguyên nhân; ❌ chưa có hành vi cơ chế riêng của từng bệnh.
- Công thức phát dịch và lây mỗi 6 giờ. ✅ S2.

## Chương 7 — Thời gian và thời tiết (dòng 1018–1111)

- 4 buổi có hiệu ứng nền. 🟡 Mới có ánh sáng và giờ chợ.
- **10 thời tiết**, mỗi loại kèm trạng thái lên người và lên heo, cùng cách phòng tránh:
  - Cảm Nắng, Ướt Lạnh, Sét Đánh, Cảm Gió, Lạc Hướng, Cóng Tay, Trơn Trượt, Khô Rát, Nhiễm Xạ.
  - Đồ phòng tránh: Áo Mưa Dầu, Vòng Cách Điện, Quần Da Lót Bông, Giày Đinh, Áo Chì, Mặt Dây Đèn Bão.
  - ❌ Chưa có trạng thái lên người. Đây là nơi trang bị (12.3) phối với thời tiết; thiếu nó thì các món đồ chống thời tiết vô dụng.

## Chương 8 — Nhân vật (dòng 1113–1173)

- Hai nhân vật, không khóa build. Không có kỹ năng bẩm sinh: sức chiến đấu đến từ trang bị, vật phẩm và võ kỹ.
- Chỉ số gốc cố định, chỉ tăng bằng cấp; chỉ số thưởng tách riêng.
- **HP 100**, trần 140/180; HP về 0 thì bất tỉnh và Thể trạng −20.
  - ❌ Combat hiện đang ngất do thanh "Chống Chịu" (Guard) và Stamina — luật không có trong GDD. GDD dùng HP. Cần đổi lại.
- Thể lực 180, trần 220/260; Thể trạng; Miễn dịch.
  - ❌ Miễn dịch chưa có tác dụng.
- **Tác dụng của 4 chỉ số:** STR/AGI/CTRL/RES như ghi trong file. ✅ C đã làm.
  - CTRL còn tác dụng "hiệu lực choáng/làm chậm". ❌ Chưa dùng CTRL để tăng thời gian choáng và làm chậm của kỹ năng.
- Cấp 1–30. ✅ C.

## Chương 9 — Thể lực (dòng 1174–1365)

- **Hai lớp tiêu hao:** hao tức thời và Tải ngày; bảng chi phí theo từng việc; hệ số môi trường và sức khỏe nằm trong khoảng ×0,6–2,0. ✅ S1.
- **Lướt né 9 STA, đỡ đòn 5 STA;** AGI gốc ≥ 35 thì né −15%. ❌ Chưa có nút lướt né. Đây là động tác nền mà nhiều luật phối dựa vào:
  - Khinh Thân −20%;
  - Trọng Giáp +25%;
  - Guốc Gió: cú lướt thứ 2 trong 1 giây miễn phí;
  - Phù Truy Phong: lướt để lại vệt chậm;
  - Nồm Ẩm: lướt có thể trượt ngã;
  - Giày Đinh: miễn trượt ngã.
- **Mục tiêu cân bằng:** chăm trại tốn 70–105 / 180 Stamina mỗi ngày.
- **Hồi phục:**
  - trong combat 2/s;
  - ngoài combat 10/s, chỉ đến mức trước combat;
  - Trạm Hồi Sức +50/75/100%;
  - ngồi nghỉ 6/giờ;
  - bữa ăn +20/+35, tối đa 3 bữa;
  - dưới 20% Stamina: chạy −15%, đòn nặng chậm +20%.
  - ✅ S1 làm phần lõi; ❌ chưa có giao diện ngồi nghỉ và ăn.
- **Ngủ:** hồi chiêu 6 giờ; Mệt Dội tối đa 4 tầng; không ngủ được trong đợt quái, trong Sương mù, hoặc khi có quái trong trại. ✅ S1.
- **5 vật phẩm phá hồi chiêu ngủ** (Trà Lá Vông, Thuốc An Thần, Hương Mê Thảo, Đan Hồi Nguyên, Quả Mộng) với tác dụng phụ. 🟡 Mới có ở Core; chưa có nguồn trong chợ.

## Chương 10 — Đặc tính và giấy (dòng 1367–1541)

- Cổng Giữa Game: ≥ 8 đợt lớn + 1 trong 5 điều kiện phụ.
- Nguồn giấy theo tỉ lệ; 6 khóa lộ theo thứ tự; giấy cấp lượt mở; tiến độ trước khi lộ chỉ tính 50%.
- Gia cường giấy +1/+2/+3 dùng Đá Cường Hóa; có bảng nguyên liệu.
- Đặc tính chuyên biệt của Khoa và An (10.8).
- ✅ S1 làm 6 khóa; ❌ chưa có giao diện dùng giấy, gia cường, và chưa rơi giấy khi sống sót đợt quái hoặc hạ Boss.
- NPC và thú cưng cũng có đặc tính chuyên môn với 6 khóa. ❌

## Chương 11 — NPC và thú cưng (dòng 1543–1664)

- **7 NPC trại**, mỗi người một "động từ" riêng:
  - Quản Lý Ca: lập lịch tự động hóa.
  - Nhà Nghiên Cứu: giải mã gen.
  - Bác Sĩ Thú Y: chẩn đoán, kiểm dịch miễn phí 10 con/ngày.
  - Kỹ Sư: thi công và sửa.
  - Nhà Buôn: bán hộ 10 heo.
  - Thủ Kho: tự nạp trước đạn.
  - Nông Công: làm 4 việc chăm sóc mỗi ngày.
  - Số NPC tối đa theo cấp trại: 2/4/6/8.
  - 🟡 Mới có lương và vài tác dụng tạm; ❌ thiếu đa số "động từ" và giới hạn số NPC.
- **5 lính**, mỗi lính có HP, sát thương, tầm và một động từ riêng; Stamina 150; rút về Cờ Tập Kết; thuê dài hạn ×2,5.
  - ❌ Chưa có thân thể trong thế giới.
  - Xạ Thủ có "Mắt Ưng": quái bị đánh dấu nhận +25% sát thương. Đây là chỗ Ấn Tín Hiệu / Nỏ Tay của người chơi phối với lính.
- **7 thú cưng**, mỗi con có động từ và đặc tính riêng; số ô 2/4/6. 🟡

## Chương 12 — Combat: võ kỹ là một hệ PHỐI, không phải "học 1 chiêu là xong"

Đọc trọn chương 12 cùng các chương liên quan (8, 9, 7, 11, 13, 16, 19) thì sức chiến đấu là kết quả của nhiều lớp nhân với nhau:

1. **Phái chính quyết định kho kỹ năng dùng được.** Vòng liên kết một chiều A→C→B→D→A:
   - Phái A dùng kỹ năng A và C; C dùng C và B; B dùng B và D; D dùng D và A.
   - Phái chính học đủ 6 kỹ năng, yêu cầu chỉ số −15%, có Tuyệt kỹ.
   - Phái liên kết học Cấp 1–2, khi chỉ số chủ đạo ≥ 30/≥ 34 kèm điều kiện hành vi; phí +25%, Stamina +10%.
   - Hai phái còn lại: mỗi phái chỉ học được 1 kỹ năng Cấp 1, cần chỉ số ≥ 38 và Bí Kíp III; Stamina +20%.
   - Đổi phái chính: 20.000G, 1 lần mỗi 3 chu kỳ.
   - ❌ Trước đây code coi cả hai phái kề bên là liên kết, và chưa có chức năng đổi phái.
2. **Ba cặp trang bị mở ba ô; mỗi ô quyết định nguồn năng lượng:** Công dùng Stamina; Thủ dùng Stamina hoặc HP (Huyết Tế); Biến dùng Nộ. ✅
3. **Ô thứ 4 từ Ấn Song Thế** (Mệnh Ấn, cuối game), trả bằng Nộ. Đây là cách duy nhất để nối hợp kích giữa hai kỹ năng CÙNG loại ô:
   - Ảnh Móc: Bộ Pháp Lướt (Công) → Móc Kéo (Công).
   - Phản Thủ: Phản Chấn (Thủ) → Hộ Thân (Thủ).
   - Trói Chùy (Thủ→Công) và Thủ Chuyển Công (Thủ→Công) dùng được ngay với 2 ô.
   - ❌ Chưa có ô thứ 4.
4. **Hợp kích là cặp chiêu đi từ phái chính sang phái liên kết của nó**, nên mỗi phái chính có đúng 1 hợp kích "tự nhiên":
   - A: Ảnh Móc;
   - C: Trói Chùy;
   - B: Phản Thủ;
   - D: Thủ Chuyển Công.
   - Cửa sổ 2 giây. ✅ đã làm cặp chiêu; cần sửa luật liên kết cho khớp.
5. **Hạng cân của 6 món quyết định lối chơi**, tự bật:
   - Khinh Thân (6–9 điểm): lướt −20% Stamina, chạy +8%, nhận sát thương +10%.
   - Cân Bằng (10–13): hồi chiêu chiêu Biến −10%.
   - Trọng Giáp (14–18): đòn nặng +15% và **choáng ngắn**, không bị quái nhỏ đẩy, lướt +25% Stamina, chạy −10%.
   - Phái hợp lối chơi −10% Stamina; phái trái lối chơi +15%.
   - 🟡 Còn thiếu: lướt, choáng của đòn nặng Trọng Giáp, và tính phần thay đổi Stamina cho kỹ năng lướt.
6. **Vũ khí có cơ chế riêng, phối với chiêu và công trình:**
   - Dao Rựa cắt dây trói, cứu heo trong 1 giây.
   - Giáo Móc: đòn nặng kéo quái 4 m vào bẫy.
   - Búa Tạ choáng 0,3 giây, phá khiên năng lượng nhỏ.
   - Nỏ Tay bắn tên tín hiệu: công trình ưu tiên mục tiêu trong 5 giây; tối đa 40 tên.
   - Người dùng chốt: nhân vật là người hiện đại, vũ khí thiên về gọn nhẹ, nhưng build hạng nặng vẫn dùng vũ khí nặng.
   - 🟡 Đã làm: kéo, choáng, tên tín hiệu. ❌ Chưa làm: cắt dây trói, phá khiên.
7. **Đồ có phẩm chất I–IV:** tăng sức mạnh cơ chế và chỉ số thưởng +1/+3/+5/+8.
   - Đồ chống thời tiết phối với chương 7: Áo Mưa Dầu chống Ướt Lạnh, Áo Chì chống Nhiễm Xạ, Quần Da Lót Bông chống Cóng Tay, Vòng Cách Điện chống sét, Giày Đinh chống Trơn Trượt, Mặt Dây Đèn Bão chống Lạc Hướng.
   - ❌ Chưa có phẩm chất; các trạng thái thời tiết lên người cũng chưa có, nên đồ chống thời tiết đang vô dụng.
8. **Phù Văn**, khắc lên đồ bậc II+, mỗi món 1 Phù:
   - Truy Phong: lướt để lại vệt chậm.
   - Nộ Hỏa: +1 Nộ mỗi đòn trúng.
   - Kiên Thạch: đỡ đòn giảm thêm 10%.
   - Hộ Đàn: Neo của thủ lĩnh +5 khi người chơi đứng gần.
   - Điều kiện mở: qua Cổng Giữa Game, ≥ 12 đợt lớn, cấp ≥ 15, Phòng Xây Dựng cấp 2, có Bàn Khắc; Mực Phù; tỉ lệ 100/80/60%.
   - ❌ Chưa có tác dụng trong trận.
9. **Mệnh Ấn**, khắc lên người chơi, tối đa 3, mỗi ấn −10 HP:
   - Bất Khuất: HP về 0 thì còn 1 HP, miễn sát thương 2 giây.
   - Song Thế: ô thứ 4.
   - Mục Đồng: Tiếng Huýt / Mắt Nhà Nghề hồi chiêu −50%.
   - ❌ Code đang làm Bất Khuất theo thanh Guard; chưa có ô thứ 4 và Mục Đồng.
10. **Túi nhanh 4 ô** (mỗi loại tối đa 3 khi vào đợt): Bình Hồi Lực 90G, Băng Gạc 60G, Lựu Khói, Bom Keo, Pháo Sáng 80G, Tinh Nộ Đan +50 Nộ… ❌ Chưa có túi nhanh, giá trong code sai.
11. **Nộ** +2 / +10 / +1, giảm 5/s, về 0 khi hết đợt; Tuyệt kỹ 60–100 Nộ nên mỗi đợt chỉ dùng 1–3 lần. ✅
12. **Chiêu phối với công trình và lính:**
    - Móc Kéo / Chấn Địa / Xoay Bầy dồn quái vào bẫy và vùng hỏa lực.
    - Ấn Tín Hiệu khiến công trình và lính đổi mục tiêu; Xạ Thủ có Mắt Ưng +25%.
    - Neo Kích tạo cửa sổ cho Lôi Pháo.
    - Dựng Chốt dồn làn cho Lôi Pháo.
    - Trói Lưới ngắt chùm kéo của UFO.
    - Chắn Mạng đỡ thay cho heo và công trình.
    - Khiêu Chiến kéo quái khỏi heo.
    - ✅ hook S3; ❌ Lôi Pháo và lính chưa có.
13. **Thanh sống còn:** GDD ghi HP, nhưng **người dùng đã chốt không có HP**: dùng thanh Chống Chịu, bị đánh thì trừ cả Chống Chịu lẫn Thể Lực, về 0 thì ngất và về hồi sức. ✅ Giữ theo quyết định người dùng.
14. **CTRL tăng hiệu lực choáng và làm chậm.** ❌ Thời gian choáng và trói của kỹ năng chưa nhân theo CTRL.

## Chương 13–19 — Phòng thủ (dòng 2093–2648)

S3 đã làm phần lớn. Còn thiếu:
- 3 chế độ bắn Tự do / Tiết kiệm / Chỉ tinh anh;
- NPC trực Phòng Xây Dựng và hiệu suất 40/70/100%;
- ô thi công và thời gian xây;
- nhánh nâng cấp 16.1;
- Bạch Vân: giữ nút 1,5 giây trong 12 m, và Bảng Điều Khiển ở nhà chính;
- quy trình Lôi Pháo: chọn vùng, truyền năng 20 giây;
- Trạm Hồi Sức;
- Ụ Bao Cát che 40%;
- Đèn Hiệu đặt luật ưu tiên cho lính;
- sửa theo bảng giá nhẹ / vừa / nặng / Bạch Vân, phí khởi động lại 10%;
- báo cáo sau đợt đủ mục.

Đợt quái: Điểm Đe Dọa theo công thức; sự kiện phòng thủ; thứ tự mục tiêu; bất tỉnh mất 10% vàng **mang theo**.

## Chương 20–24 — Kinh tế, thông tin, sự kiện (dòng 2650–2916)

- **Chợ đêm:** trao đổi đúng "món muốn nhận", hoặc thay bằng món cùng nhóm ≥ 110% GTTC; 18% ô nhận vàng với giá GTTC×1,4; phí 5%; bán lấy vàng chỉ được 40%. 🟡
- **8 chủ đề chu kỳ** với tỉ lệ, giá, sức mua; phản ứng sau đột kích. ❌
- **Sức mua theo giai đoạn;** giá nền của cám, vật liệu, thuốc. 🟡
- **Bí nhân:** 2 giờ đêm, sát trại, đèn lồng xanh; tỉ lệ xuất hiện theo công thức; 0% vàng, tỉ giá 130–200%; bể hàng 2–4 ô; mỗi giao dịch ÁLSK +2..+10. 🟡
- **Ngân sách kép:** vàng và kho giá trị. ❌ Chưa có màn ngân sách.
- **Tình báo 0–5:** WHAT / WHEN / WHAT TO HOLD; giá 150/300/450/600G; phải mua theo thứ tự cấp; giao diện tách Cảnh báo / Xác nhận / Suy đoán. ❌
- **Sự kiện:**
  - Cổng 6 giờ, thoát khi đạt ≥ 50% bằng Bùa;
  - Sương mù kẹt 3 ngày, thoát bằng La Bàn / Ngọc;
  - Sương mù phòng thủ;
  - 3 màu; 14 mẫu sự kiện, mỗi mẫu một luật chơi.
  - 🟡 Mới có 4/14.

## Chương 25–28 — Công thức, bất biến, hạng mục hoãn (dòng 2918–3036)

- Các bất biến phải có test. Riêng "ô võ kỹ cần đủ cặp" và "chỉ số thưởng không mở điều kiện học võ" ✅.
- Khi các chương mâu thuẫn, thứ tự ưu tiên: 13 → 20 → 4.6 → 10 → 5 → bảng danh mục.

## HANDBOOK v3.0 (KY_THUAT.md trùng 100% với HANDBOOK)

Những yêu cầu bắt buộc liên quan đến game mà code còn thiếu:

- **Luồng màn hình (P1.02, P1.21):**
  - Menu chính và màn chọn An/Khoa đầu game. Hai nhân vật dùng chung controller, khác sprite và chỉ số.
  - Bất tỉnh: Thể trạng −20, mất 10% vàng mang theo, tỉnh lại ở nhà chính.
  - ❌ Chưa có màn chọn nhân vật.
- **Combat (P1.08):** combo 3 đòn; lướt né có miễn sát thương ngắn (i-frame); hitbox và hurtbox. ❌ Chưa có lướt né.
- **Bộ hoạt ảnh bắt buộc cho người chơi:** Idle, Walk, Run, Attack (3 đòn), Dodge, Interact, Hurt, Death × 4 hướng. Việc của Argon (art).
- **HUD:**
  - Góc trên trái: HP, Thể lực 2 lớp, Nộ.
  - Góc trên phải: đồng hồ, buổi, thời tiết, vàng.
  - Góc dưới: thanh võ kỹ 3 ô.
  - Trạng thái đàn đọc được trong 1 giây.
  - Tối đa 2 cú nhấp cho việc hằng ngày.
- **Menu dạng lưới thẻ:**
  - trang bị 6 ô;
  - Phù Văn / Mệnh Ấn;
  - Giấy Chuyên Biệt;
  - chợ đêm theo "món muốn nhận".
- **Trang phục thay theo trang bị:** STRETCH (không bắt buộc), nhưng người dùng đã yêu cầu.
- **Farm_Core có 7 phòng:**
  - nhà chính: ngủ / lưu, chọn nhân vật, Bảng Điều Khiển Bạch Vân;
  - kho;
  - Phòng Xây Dựng;
  - Phòng Nghiên Cứu: mở ở cấp 8–10, cần Nhà Nghiên Cứu Gen;
  - Xưởng Cường Hóa: gia cường giấy, rã giấy, Bàn Khắc Phù Văn, khắc Mệnh Ấn;
  - Lều Quân Y;
  - khu thông tin.
- **Kết nối giữa các map (6.6):**
  - Farm↔Market qua cổng Nam;
  - Farm↔Hub_Village qua cổng phụ phía Tây;
  - Farm↔Forest qua lối mòn phía Bắc;
  - Farm↔Core qua Đoạn Chuyển Map phía Bắc.
  - Logic chạy trong Core, nên vẫn tiếp diễn khi người chơi ở map khác.
- **Bất biến phải có test (17.4):** 11 mục, thêm các bất biến của GDD 27.

## Kế hoạch sửa hệ võ kỹ theo đúng cách phối (sau khi đọc trọn tài liệu)

1. **Vòng liên kết một chiều** A→C→B→D→A: liên kết = phái kế tiếp trên vòng. Sửa `MartialProgression.Relation` và các test.
2. **Ô thứ 4 (Ấn Song Thế):** thêm `CombatSlotType.TheSongThe`.
   - Lắp được mọi kỹ năng đã học; trả bằng Nộ, quy đổi 1 STA = 1 Nộ (TẠM, ghi ADR).
   - Mở khi `EngravedSeals` có `AnSongThe`.
   - Phím R; thêm nút trên HUD.
3. **Lướt né:**
   - Phím Left Ctrl, quãng 3 m (TẠM), miễn sát thương 0,25 giây (TẠM).
   - 9 STA; AGI gốc ≥ 35 thì −15%; Khinh Thân −20%; Trọng Giáp +25%.
   - Guốc Gió: cú lướt thứ 2 trong 1 giây miễn phí.
   - Phù Truy Phong: để lại vệt chậm.
   - Nồm Ẩm: 10% trượt ngã, trừ khi đi Giày Đinh.
4. **Trọng Giáp:** đòn nặng gây choáng ngắn 0,3 giây (TẠM, theo số của Búa Tạ).
5. **Kỹ năng loại lướt** (Bộ Pháp Lướt, Xuyên Bầy) chịu hệ số lướt của lối chơi.
6. **CTRL kéo dài thời gian choáng / trói / làm chậm:** +0,2%/điểm (TẠM, cùng hệ số với chí mạng).
7. **Phẩm chất đồ I–IV:** chỉ số thưởng +1/+3/+5/+8. Đồ trong kho lưu dạng "id#q".
8. **Phù Văn:** khắc lên đồ bậc II+, có hiệu ứng thật trong trận (Truy Phong, Nộ Hỏa, Kiên Thạch, Hộ Đàn).
9. **Mệnh Ấn:**
   - Bất Khuất tính theo HP;
   - Song Thế mở ô thứ 4;
   - Mục Đồng giảm hồi chiêu Tiếng Huýt / Mắt Nhà Nghề 50%.
10. **(HUỶ)** Không đổi sang HP. Người dùng đã chốt dùng thanh Chống Chịu.
11. **Đổi phái chính:** 20.000G, 1 lần mỗi 3 chu kỳ.
12. **Túi nhanh 4 ô:** Bình Hồi Lực, Băng Gạc, Lựu Khói, Bom Keo, Pháo Sáng, Tinh Nộ Đan; tối đa 3 mỗi loại; giá đúng GDD.
13. **Bảng Nhân vật hiện build:**
    - kho kỹ năng dùng được theo vòng;
    - hợp kích khả dụng với bộ chiêu đang lắp;
    - hiệu ứng lối chơi;
    - cơ chế riêng của vũ khí;
    - Phù Văn / Mệnh Ấn.

## Cốt truyện Mùa 1 "Sáu Với Một" (COT_TRUYEN_PIG_TYCOON.md, đọc trọn Phần 1–12)

Những gì cốt truyện yêu cầu về mặt game, và mức đã có:

- **8 quest mở đầu P01–P08** (hồi tưởng ở thành phố, nhà cũ, ổ khóa sao 7 cánh, câu đố khe bếp lò 4 tầng, chờ đúng 2 phút thực). ❌ Chưa có. Game đang bắt đầu thẳng từ Q01.
- **Trại lúc nhận bàn giao** (cuối mục 1.16): 12 Ô Đất, một đàn nhỏ, nhà chính, kho, Phòng Xây Dựng cấp 1 đã dựng sẵn nhưng **chưa có người trực**, tiền trong két đủ vài ngày.
  - 🟡 Ô Đất và nhà kho khớp. "Đàn nhỏ" không có số cụ thể (S2 đang để 12 con).
  - ❌ Phòng Xây Dựng chưa có luật "không người trực thì không xây"; Q06 dựa vào luật này.
- **20 quest chính Q01–Q20:** ✅ đã có khung (signal). Điều kiện mở gắn với hệ thống thật:
  - Q03: đợt lớn đầu tiên.
  - Q04: trại cấp 3 + giao dịch chợ đêm.
  - Q05 / Q10 / Q15: Boss chu kỳ 5 / 10 / 15.
  - Q06: thuê người trực Phòng Xây Dựng rồi sửa rào ngoài đợt.
  - Q09: Cổng Giữa Game.
  - Q11: lập Bầy Đàn.
  - Q20: từ chu kỳ 20.
  - Thưởng vàng và EXP trại theo bảng.
  - Vật chứng truyện nằm trong hồ sơ truyện.
  - 🟡 Phần lớn cảnh truyện, vật chứng và sổ hai cột chưa có giao diện.
- **12 quest phụ S01–S12:** ✅ có S02, S10, S11; ❌ thiếu 9 quest còn lại (S01 Giám Đốc, S03 nồi lẩu, S04 đèn ngủ, S05 Xích Mao, S06 lá thư ổ lót, S07 Phong Mẫu, S08 kế vị, S09 Quạ Tin, S12 bát của ai).
- **Sổ hai cột "Thấy" / "Có thật" và lề bút chì:** là giao diện nhật ký chính của truyện. ❌ Chưa có.
- **14 sự kiện:** mỗi sự kiện có chất hài và một chi tiết bí ẩn riêng. 🟡 Mới có 4.
- **3 kết thúc mở và cảnh sau credit:** ✅ có nút chọn kết thúc; ❌ chưa có cảnh.
- **Tên riêng:**
  - Lính: Độ (Vệ Binh Khiên), Miên (Xạ Thủ Nỏ), Chốt (Kỹ Sư Chiến Trường), Tư Trời (Thợ Săn UFO), Vân (Thích Khách Đêm).
  - Thú cưng: Cò, Phiếu, Tròn, Tin, Móp, Giám Đốc, Răng.
  - NPC: Mận (Nông Công), Tám Vá (Kỹ Sư), Dung (Thú Y), Cúc (Nghiên Cứu), Lệ (Nhà Buôn), anh Ca (Quản Lý Ca), ông Tồn (Thủ Kho).
  - Bà Sáu không tuyển được; bữa ăn của bà là giao dịch có phí, dùng luật bữa ăn có sẵn.
- **Luật của xóm:**
  - không nói số 7 ("sáu với một");
  - "Mai trời nắng";
  - bát úp;
  - đếm chỗ trống.
  - Phần thoại vặt ở mục 9.3.
- **Khoa mặt vô hồn suốt Mùa 1;** cảm xúc chỉ hiện trong bề mặt phản chiếu. **Bản An** dùng chung truyện, đổi xưng hô.
- **Luật bất khả xâm phạm:** không cảnh truyện nào được xóa công trình, đàn heo hay kinh tế của người chơi.
