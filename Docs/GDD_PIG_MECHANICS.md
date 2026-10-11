
## GAME DESIGN DOCUMENT • BẢN HỢP NHẤT CHÍNH THỨC


## PIG TYCOON

Quản lý trang trại heo • Combat nhân vật • Công trình phòng thủ • Kinh tế thông tin



| Hạng mục | Nội dung |
| --- | --- |
| Phiên bản | 6.0 — Nâng cấp toàn diện từ v5.0: Trạng thái Bầy Đàn & kế thừa, gen có cơ chế và cách khắc chế, Sinh Cảnh, combat 6 ô trang bị, Phù Văn & Mệnh Ấn, công trình cần bản vẽ + Phòng Xây Dựng, 3 công trình Bạch Vân, trao đổi theo yêu cầu người bán, Bí nhân |
| Thể loại | Tycoon mô phỏng hệ thống + thủ thành (Tower Defense) có nhân vật điều khiển |
| Triết lý | “Luật tối thiểu — Tác động chéo tối đa”; ngân sách là van khóa sức mạnh |
| Trạng thái số liệu | Baseline v0.1 để làm prototype và playtest; tinh chỉnh sau dữ liệu thật |
| Ngày lập | 28/09/2026 |



## MỤC LỤC

​

​


## PHẦN I — TỔNG QUAN THIẾT KẾ4

1. Tóm tắt điều hành & triết lý thiết kế4

2. Vòng lặp cốt lõi, thang thời gian & giai đoạn chơi7


## PHẦN II — TRANG TRẠI, ĐÀN HEO & MÔI TRƯỜNG9

3. Không gian trang trại & Cấp trại9

4. Vòng đời heo, chỉ số heo & sinh sản12

5. Hệ Gen: dòng giống, tính chất & định danh17

6. Chỉ số Sinh Cảnh & Dịch bệnh20

7. Thời gian trong ngày & Thời tiết23


## PHẦN III — NHÂN VẬT, NPC, THÚ CƯNG & COMBAT25

8. Nhân vật người chơi & bảng chỉ số25

9. Stamina (Thể lực) — mô hình chi tiết26

10. Hệ Đặc tính & Giấy Chuyên Biệt29

11. NPC & Thú cưng31

12. Combat: 6 ô trang bị, ô võ kỹ, Phù Văn, Mệnh Ấn & vật phẩm33


## PHẦN IV — PHÒNG THỦ, CÔNG TRÌNH & ĐỢT QUÁI38

13. Kiến trúc phòng thủ, bản vẽ & Phòng Xây Dựng38

14. Loại 1 — Công trình Thông tin40

15. Loại 2 — Công trình Hỗ trợ & Lưới điện40

16. Loại 3 — Công trình Tấn công41

17. Loại 4 Phòng thủ • Loại 5 Bẫy • Loại 6 Triển khai42

18. Độ bền, hư hại & sửa chữa43

19. Đợt quái, danh mục quái & Boss44


## PHẦN V — KINH TẾ, THÔNG TIN & SỰ KIỆN HUYỀN BÍ45

20. Thị trường Ngày / Đêm45

21. Bí nhân46

22. Quản lý ngân sách47

23. Thông tin & Tình báo47

24. Sự kiện huyền bí48


## PHẦN VI — CÂN BẰNG, TRIỂN KHAI & NGHIỆM THU49

25. Tổng hợp công thức49

26. Kế hoạch phân rã công việc (WBS)49

27. Bất biến thiết kế & tiêu chí nghiệm thu50

28. Hạng mục hoãn50


## PHỤ LỤC50

Phụ lục A — Thuật ngữ & ghi chú50

Phụ lục B — Nhật ký thay đổi v5.0 → v6.051

Phụ lục C — Tra cứu nhanh51

​

​


# PHẦN I — TỔNG QUAN THIẾT KẾ


## 1. Tóm tắt điều hành & triết lý thiết kế

Pig Tycoon là game quản lý mô phỏng lấy trang trại heo thả rông làm trung tâm. Người chơi đồng thời phải điều hành đàn heo, dòng tiền, sức khỏe nhân vật, thông tin tình báo và chuẩn bị cho các đợt quái tấn công theo chu kỳ. Độ sâu của game không đến từ bản đồ mở vô hạn hay lượng nội dung khổng lồ, mà đến từ việc một trạng thái của trại tác động chéo đến nhiều hệ cùng lúc: số heo già ảnh hưởng dịch bệnh, sự kiện huyền bí và giá chợ; ngân sách mua đạn cho công trình cạnh tranh trực tiếp với tiền mở rộng trại và tiền giám định gen.



| KHÓA THI Ế T K Ế  (DESIGN LOCK)  v6 .0 B ả n  v6 .0  thay th ế   v5 .0.  Khi có mâu thu ẫ n, quy đ ị nh trong  v6 .0 đư ợ c ưu tiên. M ọ i con s ố  là   baseline v0.1   dùng cho prototype,  đư ợ c tinh ch ỉ nh b ằ ng d ữ  li ệ u playtest nhưng   không  thay đ ổ i c ấ u trúc lu ậ t   n ế u không c ậ p nh ậ t tài li ệ u. |
| --- |



### 1.1. Năm trụ cột thiết kế



| Trụ cột | Ý nghĩa | Biểu hiện trong game |
| --- | --- | --- |
| 1. Tác động chéo | Mỗi quyết định tạo hậu quả ở ít nhất hai hệ thống. | Giữ heo già → tăng thủ lĩnh đàn nhưng tăng áp lực dịch bệnh và sự kiện. |
| 2. Ngân sách là van khóa | Công trình không bị làm yếu; chúng bị giới hạn bằng chi phí vận hành. | Đạn, dầu, năng lượng, hao mòn, sửa chữa, cooldown. |
| 3. Combat + công trình | Nhân vật là lực phản ứng linh hoạt; công trình là xương sống thủ thành. | Nhịp Đánh → Giữ chân → Rút lui → Trụ bọc lót → Tái giao tranh. |
| 4. Thông tin là hàng hóa | Biết trước không làm dễ hơn, chỉ giúp chuẩn bị đúng lúc. | Trinh thám, tình báo hằng ngày, Bí nhân, giám định gen. |
| 5. Khám phá có giá | Hiếm không đồng nghĩa với tốt; bí ẩn phải trả giá để mở. | Gen “?”, đặc tính khóa, giấy chuyên biệt kiểu gacha. |


Hình 1.1 — Sáu lớp tương tác quanh trang trại (số trên hình ứng với bảng chú giải bên dưới).



| # | Lớp | Nội dung chính | Tác động sang lớp khác |
| --- | --- | --- | --- |
| 1 | Đàn heo | Tuổi, gen, tinh thần, mật độ, sức khỏe | Lượng hàng bán (2), áp lực sự kiện (5), mục tiêu của quái (6) |
| 2 | Thị trường | Chợ ngày bằng vàng, chợ đêm trao đổi và đấu giá | Ngân sách cho công trình (6), heo tồn đọng làm tăng mật độ (1) |
| 3 | Thông tin | Trinh thám, tin hằng ngày, Bí nhân, giám định | Chuẩn bị đúng lúc cho (5) và (6); chọn thời điểm bán ở (2) |
| 4 | Thời gian | 4 buổi, thời tiết, Stamina, chu kỳ | Hiệu suất lao động, trạng thái thời tiết lên người và heo (1) |
| 5 | Tấn công & sự kiện | Đợt lớn, đợt nhỏ, sự kiện huyền bí, dịch bệnh | Thiệt hại đàn (1), giá chợ sau đột kích (2) |
| 6 | Phòng thủ | Combat nhân vật + 6 loại công trình | Tiêu hao ngân sách (2), bảo vệ đàn (1) |



### 1.2. Các quyết định chuẩn hóa (v5.0 → v6.0)



| Hạng mục | Vấn đề ở bản cũ | Quy định v5.0 (chính thức) |
| --- | --- | --- |
| Nhịp chu kỳ | 10–15 ngày/chu kỳ = 3–4,5 giờ thực, quá dài. | 1 ngày = 16 phút thực. Đợt lớn mỗi 6–8 ngày (đầu game), 7–10 (giữa), 8–12 (cuối); chu kỳ dài 12–15 ngày chỉ là trường hợp hiếm. Thêm đợt nhỏ giữa hai đợt lớn. |
| Mật độ đàn | Vùng ≤70% tốt hơn mọi mặt → chiến lược áp đảo;  xác chưa có trong công thức. | Tách sức chứa sống và sức chứa xác; vùng ≤70% phải trả phí duy trì hạ tầng bỏ trống; thang Áp Lực Sự Kiện 0–100; hệ số nội suy mềm, không nhảy vọt. |
| Tinh thần đàn | Thiếu chỉ số tinh thần/ổn định cảm xúc. | M ỗ i con heo có  Tinh th ầ n   0–100  ( 4 tr ạ ng thái ) và  Hòa nhập  0–100. Đàn chỉ thành  Trạng thái Bầy Đàn  khi đủ ≥ 100 con gắn kết, Sinh Cảnh ≥ 50 và nhiều điều kiện khác; thủ lĩnh chỉ sinh ra từ Bầy Đàn. Kế thừa thủ lĩnh có tỉ lệ, tăng bằng  Bồi Dưỡng Kế Vị . |
| Hệ Gen | Các giống chỉ khác nhau bằng cộng/trừ chỉ số. | 12 dòng gen, mỗi dòng một  cơ chế riêng . Bậc cao có sức ảnh hưởng tương xứng (Kim Thọ là trụ cột Bầy Đàn). Có 1 gen Khá xấu ( Xích Mao ) khắc phục được sau khi định danh; tính chất xấu của  Hư Thể  chỉ thức tỉnh khi heo gặp trạng thái xấu, giữ được thì bị phong ấn. |
| Sức khỏe đàn | 10 “bệnh” thực chất là 10 dòng giảm chỉ số. | Chia 4 nhóm: dịch truyền nhiễm, bệnh môi trường, tình trạng cấp/mãn tính, bệnh đặc biệt theo gen. Dùng 5 “động từ” xử lý: cách ly, xử lý môi trường, điều trị cá thể, phòng ngừa đàn, nghiên cứu. |
| Phòng thủ | Nhiều công trình phải kích hoạt thủ công. | M ọ i công trình   t ự  đ ộ ng , mạnh áp đảo; bị giới hạn bằng  số lượng tối đa, nạp trước, hồi chiêu và hao mòn  chứ không bị quái khắc chế.  Ch ỉ  3 công trình c ấ p cao nh ấ t mang danh   hiệu  Bạch Vân  —  Cửu Tiêu Lôi Pháo  (tấn công),  Thiên Nhãn Vọng Đài  (trinh thám),  Vạn Linh Hồi Nguyên Trận  (hỗ trợ) — kích hoạt thủ công. Mọi công trình cần  bản vẽ + Phòng Xây Dựng . |
| Chỉ số & trang bị | Trang bị cộng thẳng chỉ số, có thể dùng để đủ điều kiện học võ. | Ch ỉ  s ố  g ố c c ố  đ ị nh, ch ỉ  tăng b ằ ng c ấ p đ ộ ; trang  b ị  ch ỉ  cho   ch ỉ  s ố  thư ở ng , không tính  đi ề u ki ệ n . Tối đa  6 ô trang bị ; mỗi  cặp món  (Vũ khí + Giày, Giáp + Quần, Vòng tay + Vòng cổ) mở 1 ô võ kỹ. Từ giữa–cuối game mở  Phù Văn  (khắc lên trang bị) và  Mệnh Ấn  (khắc lên nhân vật). |
| Đặc tính | Đặc tính nhân vật dính vào phòng thủ; các đặc tính na ná nhau. | Đặc tính của 2 nhân vật chỉ hỗ trợ việc hằng ngày. Chỉ NPC/thú cưng chuyên phòng thủ mới có đặc tính phòng thủ. Đặc tính chuyên biệt/chuyên môn bị ẩn hoàn toàn đến khi hoàn thành cả 6 khóa. |
| Thị trường | Mọi thứ quy về vàng. | Ch ợ  ngày giao d ị ch  b ằ ng vàng. Ch ợ  đêm: công trình , bản vẽ  và nguyên li ệ u công trình mua b ằ ng vàng/đ ấ u giá  hoặc trao đổi ; v ậ t ph ẩ m khác  là   trao đ ổ i đ ồ ng giá   theo đúng món người bán yêu cầu  — chỉ 18% ô hàng chấp nhận vàng.  Bí nhân không bao giờ nhận vàng , luôn đòi giá trị cao hơn món đưa ra . |
| Sự kiện | Chưa có lối thoát sớm. | Cổng: hoàn thành ≥50% có thể dùng vật phẩm bậc Thường để thoát. Sương mù: cần vật phẩm bậc cao để thoát. |
| Sinh Cảnh | Các bậc 50–69 và 70–89 gần như không khác nhau. | Mỗi bậc có tương tác riêng với Bầy Đàn: < 50 không thể lập bầy, 50–69 Sơ Khai, 70–89 mở bầy Vững, 90–100 mở bầy Hưng Thịnh. |



### 1.3. Phạm vi nội dung bản 1.0



| Nhóm nội dung | Số lượng | Ghi chú |
| --- | --- | --- |
| Nhân vật người chơi | 2 | 1 nam (Khoa), 1 nữ (An) |
| NPC trang trại / chuyên môn | 7 | Không tham chiến |
| NPC chiến đấu / lính đánh thuê | 5 | Chỉ phòng thủ, thuê ở chợ đêm |
| Thú cưng | 7 | 4 hỗ trợ hằng ngày + 3 canh gác |
| Dòng gen | 12 | 6 bậc độ hiếm, mỗi dòng một cơ chế riêng |
| Trường phái võ kỹ / kỹ năng | 4 / 24 | 6 kỹ năng mỗi phái, mỗi kỹ năng một “động từ” riêng |
| Công trình phòng thủ | 31 | 5 tấn công; 3 công trình Bạch Vân kích hoạt thủ công |
| Loại quái / Boss chu kỳ | 14 / 3 | Mặt đất, trên không, huyền bí, môi trường |
| Thời tiết / Mối đe dọa sức khỏe | 10 / 10 | Mỗi thời tiết kèm trạng thái; sức khỏe chia 4 nhóm |
| Vật phẩm phá cooldown ngủ | 5 | 4 bậc vật phẩm; chỉ Quả Mộng không có tác dụng phụ |
| Mẫu sự kiện huyền bí | 14 | 3 màu × 2 hình thức xuất hiện |



## 2. Vòng lặp cốt lõi, thang thời gian & giai đoạn chơi


### 2.1. Vòng lặp cốt lõi



| CORE LOOP Chăm đàn → Tạo dòng tiền (chợ ngày) → Phân bổ ngân sách (trại / công trình / vật phẩm chợ đêm / thông tin) → Thu thập tình báo → Nạp trước nhiên liệu, đạn cho công trình → Đợt quái hoặc sự kiện → Thiệt hại & cơ hội → Sửa chữa, phục hồi, tái cân bằng đàn → Lặp lại. |
| --- |


Game có hai nhịp chạy song song và lệch pha nhau: nhịp kinh tế (thị trường đổi mỗi ngày) và nhịp đe dọa (đợt quái lớn mỗi 6–12 ngày tùy giai đoạn, xen giữa là các đợt nhỏ). Vì hai nhịp không trùng nhau, người chơi luôn phải chọn giữa bán ngay hay giữ hàng, đầu tư mở rộng hay dự trữ cho phòng thủ, mua thông tin hay tin vào may mắn.


### 2.2. Thang thời gian



| Đơn vị trong game | Thời gian thực | Ghi chú |
| --- | --- | --- |
| 1 phút trong game | ≈ 0,67 giây | Đồng hồ HUD |
| 1 giờ trong game | 40 giây | Đơn vị tính cooldown giám định, xử lý xác |
| 1 ngày (24 giờ) | 16 phút | Khoảng tinh chỉnh cho phép 14–18 phút; có thể tua bằng ngủ |
| Chu kỳ đầu game (6–8 ngày) | 96–128 phút | Nhịp nhanh để học vòng lặp |
| Chu kỳ giữa game (7–10 ngày) | 112–160 phút | Trung bình ~8 ngày ≈ 2 giờ 08 phút |
| Chu kỳ cuối game (8–12 ngày) | 128–192 phút | Chuẩn bị nhiều hơn, đợt nặng hơn |
| Chu kỳ dài hiếm (12–15 ngày) | 192–240 phút | “Mùa Kinh Tế Vàng”: 8% số chu kỳ, báo trước áp lực quái thấp |
| Trong đợt quái | Thời gian thực (giây) | Đồng hồ ngày dừng; hồi chiêu công trình tính bằng giây thực |



### 2.3. Nhịp đe dọa: Đợt lớn, Đợt nhỏ & gắn với tiến trình trại

Đợt lớn là mốc “đổi trạng thái” của cả game: sau mỗi đợt lớn, xu hướng chợ được gieo lại, tình báo chu kỳ hết hạn, công trình được sửa và nạp lại. Để khoảng giữa hai đợt lớn không bị “chết”, game chèn thêm đợt nhỏ và các sự kiện bất chợt.



| Thông số | Đợt lớn (Đại Đợt) | Đợt nhỏ (Quấy Phá) |
| --- | --- | --- |
| Tần suất | 1 lần/chu kỳ, kết thúc chu kỳ | Đầu game 0–1; giữa game 1–2; cuối game 1–3 lần/chu kỳ |
| Quy mô | 100% Điểm Đe Dọa; 3–5 làn sóng con; Boss mỗi 5 chu kỳ | 15–30% Điểm Đe Dọa; 1 làn; không Boss; tối đa 1 tinh anh (từ chu kỳ 6) |
| Thời lượng | 4–9 phút thực | 1–2,5 phút thực |
| Cảnh báo mặc định | 1–2 ngày trước | 2–6 giờ game trước (Radar có thể báo sớm hơn) |
| Nạp lại công trình | Không được nạp trong đợt | Không được nạp trong đợt → ăn vào trữ lượng đã chuẩn bị cho đợt lớn |
| Mục tiêu ưu tiên | Đàn → Kho → Hạ tầng → Nhà chính → Người chơi | Kho và heo ở rìa trại (trộm, gặm kho) |


Gắn độ dài chu kỳ với tiến trình trại. Độ dài mỗi chu kỳ được gieo ngẫu nhiên trong khoảng của giai đoạn (ví dụ 7 → 10 → 8 ngày), rồi điều chỉnh:



| Điều kiện | Điều chỉnh |
| --- | --- |
| Áp Lực Sự Kiện ≥ 60 hoặc mật độ đàn > 120% khi gieo chu kỳ | −1 ngày (tối thiểu 6) |
| Đợt lớn trước thua nặng (mất ≥ 30% đàn hoặc nhà chính bị phá) | +1 ngày “hồi phục” |
| Cấp trại tăng ≥ 2 trong chu kỳ trước | Điểm Đe Dọa đợt kế +10% |
| Chu kỳ “Mùa Kinh Tế Vàng” (8%) | 12–15 ngày; không đợt nhỏ trong 5 ngày đầu |


Tình báo tiết lộ dần độ dài chu kỳ: cấp 1 cho biết khoảng giai đoạn, cấp 3 thu hẹp còn ±2 ngày, cấp 4 còn ±1 ngày và buổi trong ngày.


### 2.4. Giai đoạn chơi



| Giai đoạn | Nhận diện | Người chơi thấy | Hệ thống thực sự tính |
| --- | --- | --- | --- |
| Đầu game | Từ đầu đến trước Cổng Giữa Game; thường chu kỳ 1–7; đàn 10–80 con | Nuôi heo, bán, sửa trại, chống vài đợt quái nhỏ | Số đàn, tuổi, giá chợ, Stamina, Sinh Cảnh, đồng hồ đợt quái |
| Giữa game | Đ ạ t   C ổ ng Gi ữ a Game   (m ụ c 10.2 ): bắt buộc ≥ 8  đợt lớn + 1 điều kiện phụ;  thư ờ ng  chu k ỳ   8–10 | Canh bán, săn deal đêm, gen hiếm, giấy chuyên biệt, công trình đặc biệt | Cơ cấu tuổi, tình báo, điểm kích hoạt sự kiện, nạp trước, phân bổ ngân sách |
| Cuối game | Chu k ỳ   20 +; C ấ p tr ạ i 15–20 ; sở hữu công trình Bạch Vân | Chủ động kích/né sự kiện, tối ưu kế thừa đàn, build phòng thủ theo chu kỳ | Điều kiện biến thiên theo ván, nợ thể trạng, xu hướng chợ, áp lực huyền bí |



# PHẦN II — TRANG TRẠI, ĐÀN HEO & MÔI TRƯỜNG


## 3. Không gian trang trại & Cấp trại


### 3.1. Mô hình thả rông có ranh giới

Heo được thả rông trong toàn bộ lãnh thổ trang trại (không nhốt từng ô chuồng nhỏ) nhưng không được ra khỏi ranh giới. Bản đồ chính có quy mô nhỏ–vừa, chia khu chức năng. Hàng rào và cổng là ranh giới quản lý, đồng thời là tuyến phòng thủ đầu tiên.


### 3.2. Sức chứa đàn

Sức chứa không phải một con số “slot” cứng mà là giá trị nhỏ nhất của 4 nguồn:



| CÔNG THỨC CHÍNH THỨC Sức chứa gốc  = MIN(Ô Đất × 8 ; Mái trú × 6 ; Bồn nước × 12 ; Máng ăn × 10) Sức chứa hiệu dụng  = MAX(1 ; Sức chứa gốc − Xác nằm ngoài Khu Xử Lý) Mật độ  = Số heo còn sống / Sức chứa hiệu dụng |
| --- |


Đơn vị được gọi theo hạ tầng, không gắn mét vuông thật để không trói thiết kế bản đồ: 1 Ô Đất (1 ô lưới trang trại), 1 Mái trú, 1 Bồn nước, 1 Máng ăn. Mỗi đơn vị có phí duy trì cố định mỗi ngày dù có heo dùng hay không: Ô Đất 4G, Mái trú 6G, Bồn nước 3G, Máng ăn 5G.

Tách sức chứa sống và sức chứa xác. Xác heo nằm trong trại (chưa đưa về Khu Xử Lý) trừ thẳng 1 đơn vị sức chứa mỗi xác. Khu Xử Lý có 4 ô tập kết (nâng cấp 8 ô): xác nằm trong ô tập kết không trừ sức chứa và chỉ làm Sinh Cảnh −1/ngày thay vì −3, nhưng vẫn cộng Áp Lực Sự Kiện. Khi ô tập kết đầy, xác mới phải để lại trong trại.

Giao diện hiển thị điểm nghẽn (ví dụ):



| Nguồn | Sức chứa theo nguồn |
| --- | --- |
| Theo Ô Đất (12 ô × 8) | 96 |
| Theo Mái trú (12 mái × 6) | 72 ← ĐIỂM NGHẼN |
| Theo Bồn nước (9 bồn × 12) | 108 |
| Theo Máng ăn (9 máng × 10) | 90 |
| Xác ngoài Khu Xử Lý | −2 |
| Sức chứa hiệu dụng | 70 |



### 3.3. Áp lực mật độ đàn



| Mật độ | Tên vùng | Hệ số dịch | Hiệu ứng & đánh đổi |
| --- | --- | --- | --- |
| ≤ 70% | Dư sức chứa | ×0,85 | Sinh học tốt: tăng trọng +3%, Tinh thần hồi +10%, quái chú ý −10%. Mặt trái: hiệu suất vốn thấp — phí duy trì hạ tầng bỏ trống vẫn phải trả, doanh thu trên mỗi đơn vị hạ tầng thấp |
| 71–100% | Vận hành kinh tế | ×1,00 | Hiệu suất vốn cao nhất, còn biên an toàn; không thưởng, không phạt |
| 101–120% | Quá tải nhẹ | ×1,25 | Tranh máng: lượng thức ăn cần cấp ×1,10 (10% bị rơi vãi, trừ vào kho); hoảng loạn lan +10%; Tinh thần −1/ngày |
| 121–150% | Áp lực hệ thống | ×1,65 | Tăng trọng −8%; Áp Lực Sự Kiện +Vừa/ngày; quái chú ý +25%; Tinh thần −2/ngày |
| > 150% | Khủng hoảng | ×1,65 → ×2,20 | Tăng trọng −15%; hoảng loạn dây chuyền; Áp Lực Sự Kiện +Cao/ngày (làm tăng khả năng sự cố ngoài chu kỳ, không bảo đảm xảy ra); Tinh thần −4/ngày |


Nội suy mềm. Giao diện chỉ hiển thị 5 vùng, nhưng hệ số thực được nội suy tuyến tính giữa các mốc để bán hay sinh thêm 1 con không làm hệ số nhảy vọt. Mốc hệ số dịch: 70% → ×0,85; 100% → ×1,00; 120% → ×1,25; 150% → ×1,65; 180% trở lên → ×2,20 (trần). Ví dụ mật độ 160%: 1,65 + (0,55 × 10/30) ≈ ×1,83.


### 3.4. Thang Áp Lực Sự Kiện (ÁLSK)

Mọi nguồn “kích hoạt sự kiện” trong game được quy về một thang chung 0–100, tính riêng cho từng trại, cập nhật mỗi ngày lúc 06:00. Các mức Thấp/Vừa/Cao được dùng thống nhất trong toàn tài liệu.



| Ký hiệu | Giá trị | Ví dụ nguồn |
| --- | --- | --- |
| +Thấp | +1/ngày | Mỗi 10 heo già vượt ngưỡng mềm (15% đàn); mỗi xác trong ô tập kết |
| +Vừa | +3/ngày | Mật độ 121–150%; SC 30–49; mỗi xác nằm ngoài Khu Xử Lý (tối đa +12) |
| +Cao | +6/ngày | Mật độ > 150%; SC < 30; Sương Mù ngày thứ 2 trở đi |
| Tức thời | +2 đến +15 một lần | Giao dịch với Bí nhân, giữ vật phẩm huyền bí, heo nhiễm Hư Mạch |
| Suy giảm tự nhiên | −8%/ngày | Tính trên giá trị hiện tại, chỉ khi nguồn gây áp lực đã được xử lý |




| ÁLSK | Trạng thái | Ảnh hưởng |
| --- | --- | --- |
| 0–19 | Yên ả | Sự kiện bất chợt ở mức nền 0,25%/ngày |
| 20–49 | Gợn sóng | Sự kiện bất chợt 1,85–4,2%/ngày; Bí nhân ×1,2–1,5 |
| 50–79 | Bất ổn | Sự kiện 4,25–6,6%/ngày; chu kỳ kế tiếp −1 ngày; quái huyền bí xuất hiện trong đợt nhỏ |
| 80–100 | Rạn nứt | Sự kiện 6,65–8%/ngày (trần); sự kiện phòng thủ huyền bí ×2,6–3,0 |



### 3.5. Cấp trại (Farm Level 1–20)

Điểm kinh nghiệm trại (EXP trại) đến từ: bán heo (1 EXP / 100G doanh thu), sống sót đợt quái (150 × số thứ tự chu kỳ, tối đa 3.000), hoàn thành hợp đồng (80–400), định danh dòng gen mới (200–1.500 theo độ hiếm), xây công trình mới (5% giá trị). Mở khóa theo cấp chỉ cho quyền tiếp cận. Riêng công trình phòng thủ còn phải có bản vẽ và Phòng Xây Dựng đủ cấp (Chương 13).



| Cấp | EXP tích lũy | Mở khóa chính | Chưa mở |
| --- | --- | --- | --- |
| 1–2 | 0 – 1.200 | Chăn nuôi cơ bản, chợ ngày, hàng rào gỗ, Tháp Canh Quan Sát, Bàn Chông | Chợ đêm, sinh sản có chọn lọc |
| 3–4 | 1.200 – 5.000 | Chợ đêm, phối giống chọn lọc, Nỏ Xuyên Vân cấp 1, Lều Quân Y, thú cưng đầu tiên | Phòng nghiên cứu |
| 5–7 | 5.000 – 16.000 | Tự động hóa bậc 2, Tháp Hồ Quang, Tháp Phòng Không, lưới điện cỡ vừa, thủ lĩnh đàn | Công trình  Bạch Vân |
| 8–10 | 16.000 – 40.000 | Phòng nghiên c ứ u,  đ ấ u giá đêm, Máy C ộ ng Hư ở ng Huy ề n Bí , nhánh nâng cấp công trình tầng 1 | Công trình Bạch Vân |
| 11–14 | 40.000 – 95.000 | Từ Cấp 12: được xây 3 công trình Bạch Vân (cần bản vẽ); tự động hóa bậc 3; máy phát lớn | Đài Thiên Lôi , nhánh nâng cấp cuối |
| 15–20 | 95.000 – 300.000 | Đài Thiên Lôi, tự động hóa bậc 4, tình báo cấp cao, nâng cấp nhánh cuối của công trình | Không miễn chi phí, hao mòn, bất định thị trường |



## 4. Vòng đời heo, chỉ số heo & sinh sản

Mỗi con heo đi qua bốn giai đoạn, mỗi giai đoạn mở một loại giá trị và một loại rủi ro. Không có “giai đoạn tốt nhất” — người chơi quyết định thời điểm bán, giữ lại làm giống hay giữ đến già để hình thành thủ lĩnh đàn.

Hình 4.1 — Vòng đời heo: ① Heo non (nguồn F1/F2) → ② Đang lớn (cửa sổ định hình) → ③ Trưởng thành (bán, sinh sản) → ④ Heo già (Bầy Đàn, thủ lĩnh) → ⑤ Chết, xác, xử lý.


### 4.1. Bốn giai đoạn tuổi



| Giai đoạn | Tuổi (ngày) | Cân nặng chuẩn | Thức ăn/ngày | Giá trị & rủi ro |
| --- | --- | --- | --- | --- |
| Heo non | 0–4 | 1,5 → 12 kg | 0,5 kg + bú mẹ | Nguồn F1/F2, gen chưa bộc lộ; phụ thuộc mẹ 2 ngày đầu; tỉ lệ chết khi thiếu mẹ 12% |
| Đang lớn | 5–10 | 12 → 60 kg | 2,5 kg | “Cửa sổ định hình”: thức ăn, mật độ, thời tiết, stress quyết định hạng thịt và bộc lộ tính chất gen |
| Trưởng thành | 11–28 | 60 → 110 kg (trần) | 3,5 kg | Dòng tiền chính, sinh sản, hợp đồng; giá tốt nhất ở 95–115 kg |
| Heo già | 29 → tuổi thọ | Giảm 1 kg/ngày | 4,2 kg (+20%) | Mở thủ lĩnh đàn; khó bán; sinh sản −45%; tăng áp lực bệnh và sự kiện huyền bí |



### 4.2. Bảng chỉ số của một con heo



| Chỉ số | Giá trị gốc | Ảnh hưởng |
| --- | --- | --- |
| Tốc độ tăng trọng | 100% | Thời gian đạt cân xuất chuồng |
| Trọng lượng trần | 110 kg | Cân nặng tối đa; quyết định giá trần mỗi con |
| Chất lượng thịt | Hạng B | C ×0,80 • B ×1,00 • A ×1,20 • S ×1,50 lên giá bán |
| Hệ số chuyển đổi thức ăn | 100% | Lượng thức ăn cần để tăng 1 kg |
| Tỉ lệ thụ thai (nái) | 70%/lần phối | Khả năng đậu thai |
| Số con / lứa | 8 (6–11) | Quy mô tăng đàn |
| Tỉ lệ di truyền gen | 25%/con | Cơ hội con mang dòng gen của bố/mẹ (quyết định F2) |
| Tuổi thọ | 40 ngày | Từ ngày 36: chết tự nhiên 5%/ngày, +5% mỗi ngày tiếp theo |
| Sức khỏe | 100/100 | Dưới 40: tăng trọng −20%, dễ lây bệnh ×1,5 |
| Tính khí (bẩm sinh) | 50/100, cố định | Mỗi điểm trên 50: Tinh thần mất ít hơn 1% khi bị kích động; dưới 50 mất nhiều hơn 1%/điểm |
| Tinh thần (ổn định cảm xúc) | 70/100, động | Quyết định ăn, lớn, sinh sản và phản ứng khi có đợt quái/sự kiện (mục 4.3). Là điều kiện chính của Trạng thái Bầy Đàn và của việc phong ấn tính chất xấu Hư Thể |
| Hòa nhập đàn | 40/100 | +3/ngày khi sống cùng đàn (SC 30–49: +1,5; SC < 30: −1). Heo Hòa nhập ≥ 50 được tính là “gắn kết”; ≥ 65 (heo già) là ứng viên thủ lĩnh; ≥ 75 là ứng viên kế vị |
| Độ bám đàn | Tính từ Hòa nhập + Tinh thần | (Hòa nhập + Tinh thần)/2. Dưới 40: heo dễ tách đàn, lạc ra rìa trại khi hoảng; trên 70: luôn di chuyển cùng nhóm và bám thủ lĩnh |



### 4.3. Tinh thần đàn — trạng thái, nguồn giảm, nguồn hồi

Tinh thần là chỉ số quan trọng nhất khi trại bị tấn công hoặc gặp sự kiện: heo hoảng loạn chạy tán loạn, phá rào, bỏ ăn và rất dễ bị bắt. Tinh thần của cả đàn được hiển thị bằng giá trị trung bình và số con đang ở trạng thái xấu.



| Tinh thần | Trạng thái | Hành vi & hiệu ứng |
| --- | --- | --- |
| 60–100 | Bình ổn | Hành vi bình thường; đi theo đàn và thủ lĩnh; vào chuồng trú khi được lùa |
| 40–59 | Bất an | Tăng trọng −5%; thụ thai −10%; lùa heo tốn Stamina +20%; tụ lại thành cụm chặt (lây bệnh tiếp xúc +10%) |
| 20–39 | Hoảng sợ | Chạy tránh nguồn nguy hiểm theo hướng ngẫu nhiên; không vào chuồng trú; tăng trọng −12%; UFO khóa nhanh hơn 1 giây |
| 0–19 | Hoảng loạn | Húc rào (5 sát thương/giây vào hàng rào gỗ); bỏ ăn; kéo Tinh thần heo trong 6 m −3/giây; 5%/giờ phá rào thoát ra rìa trại; có thể kích hoạt Lôi Tâm |




| Nguồn làm giảm Tinh thần | Nguồn hồi Tinh thần |
| --- | --- |
| Quái xuất hiện trong 20 m: −8; bị tấn công: −20 | Yên tĩnh, không có nguy hiểm: +5/giờ game |
| Nổ lớn (mìn, Bạch Vân Pháo) trong 25 m: −12 | Trong chuồng trú khi mưa/giông: +2/giờ |
| Sét đánh trong 30 m: −10; Sương Mù: −1/giờ | Chó Chăn Đàn trong 15 m: hồi ×1,5 |
| Đồng loại chết trong 10 m: −6; thấy xác: −2/giờ | Thủ lĩnh đàn: hồi ×2 và “Neo tinh thần” (mục 4.6) |
| Quá tải mật độ: −1 đến −4/ngày | Mật độ ≤ 70%: hồi +10% |



### 4.4. Sinh sản, F1 và F2



| Thông số | Quy định |
| --- | --- |
| Tuổi phối giống | Heo trưởng thành từ ngày 12; heo già chỉ còn 55% khả năng thụ thai |
| Mang thai | 3 ngày; nái mang thai ăn +30%, không nên vận chuyển (stress +20) |
| Nghỉ sau đẻ | 2 ngày trước khi phối lại; tối đa 5 lứa / đời nái |
| F1 | Con của hai bố mẹ khác dòng gen. Mỗi con có 25% mang dòng của bố, 25% mang dòng của mẹ, còn lại mang dòng Thường ngẫu nhiên |
| F2 | Con của hai heo F1. Có 4% cơ hội  đột biến lên một bậc hiếm  (cộng dồn với tính chất gen “tăng tỉ lệ ra F2 mang gen”) |
| Đột biến môi trường | Mưa Bức Xạ khi heo đang ở giai đoạn “Đang lớn”: +1,5% đột biến; có thể xuất hiện tính chất xấu |
| Cận huyết | Phối cùng huyết thống 2 đời: số con/lứa −2, bệnh tiềm ẩn +8% |



### 4.5. Giá bán heo & kiểm dịch trước khi bán

Heo thịt (trưởng thành, già): Giá = Cân nặng × 3G × Hạng thịt × Hệ số giai đoạn × Hệ số giống × Trạng thái chợ × Xu hướng chu kỳ

Heo giống (non, đang lớn): Giá = 315G (giá tham chiếu 1 heo trưởng thành chuẩn) × Hệ số giai đoạn × Hệ số giống × Trạng thái chợ

Heo trưởng thành luôn là giai đoạn có hệ số cao nhất. Heo non và đang lớn chỉ đáng giá khi đã được định danh và thuộc giống Hiếm trở lên — khi đó hệ số giai đoạn được nâng lên ngang heo trưởng thành; muốn cao hơn nữa thì giống phải hiếm hơn.



| Giai đoạn | Hệ số giai đoạn (Thường/Khá hoặc chưa định danh) | Nếu đã định danh, giống Hiếm trở lên |
| --- | --- | --- |
| Heo non (0–4 ngày) | ×0,35 | ×1,00 (ngang trưởng thành) |
| Đang lớn (5–10 ngày) | ×0,55 | ×1,00 (ngang trưởng thành) |
| Trưởng thành 95–115 kg | ×1,00 (cao nhất) | ×1,00 |
| Trưởng thành < 95 kg | ×0,85 | ×0,85 |
| Heo già | ×0,15–0,30; 0–10% cơ hội có người mua/ngày | ×0,30; Bí nhân có thể mua giá cao hơn |




| Giống (đã định danh) | Hệ số giống | Tỉ lệ bán thành công nếu KHÔNG kiểm dịch | Ghi chú |
| --- | --- | --- | --- |
| Thường | ×1,00 | 70% (−30%) | Heo chưa định danh luôn tính như Thường |
| Khá | ×1,10 | 65% (−35%) | — |
| Hiếm | ×1,30 | 55% (−45%) | Heo non/đang lớn được nâng hệ số giai đoạn |
| Quý | ×1,60 | 45% (−55%) | — |
| Huyền Thoại | ×2,00 | 35% (−65%) | Sức mua chợ ngày chỉ 0–2 con/ngày |
| Dị Biến | Chợ đêm / Bí nhân | Không bán được ở chợ ngày | Trao đổi đồng giá ở chợ đêm |


Kiểm dịch trước khi bán: 60G/con tại chợ ngày (hoặc miễn phí nếu NPC Bác Sĩ Thú Y đã khám trong ngày). Heo kiểm dịch sạch bán với tỉ lệ 100%. Nếu phát hiện bệnh, heo không được bán cho tới khi chữa. Bán heo chưa kiểm dịch mà người mua phát hiện bệnh sau đó: sức mua chợ ngày với trại −10% trong 3 ngày.

Ví dụ: heo trưởng thành 105 kg, hạng A, giống Hiếm đã định danh, đã kiểm dịch, chợ bình thường: 105 × 3 × 1,20 × 1,00 × 1,30 = 491G. Heo non cùng giống: 315 × 1,00 × 1,30 = 410G; heo non giống Thường: 315 × 0,35 = 110G.


### 4.6. Trạng thái Bầy Đàn, thủ lĩnh & kế thừa

Một đám heo đông không tự động là “bầy đàn”. Trạng thái Bầy Đàn là một trạng thái của cả trại, chỉ hình thành khi đàn đủ lớn, đủ gắn kết, đủ bình ổn và môi trường đủ tốt. Thủ lĩnh chỉ có thể sinh ra khi Bầy Đàn đã hình thành — không phải heo già nào cũng lên được.

A. Điều kiện hình thành Trạng thái Bầy Đàn (phải đạt đồng thời, duy trì liên tục 3 ngày):



| # | Điều kiện | Ghi chú |
| --- | --- | --- |
| 1 | ≥ 100 heo  gắn kết  (Hòa nhập ≥ 50) | Heo Kim Thọ trưởng thành/già tính bằng 3 con |
| 2 | Sinh Cảnh ≥ 50 | SC 30–49: số heo được tính gắn kết tối đa 80 → không thể đạt điều kiện 1. SC < 30: bầy không thể tồn tại |
| 3 | Tinh thần trung bình đàn ≥ 60, không con nào Hoảng loạn trong 24 giờ gần nhất | Có Kim Thọ trong đàn: ngưỡng giảm còn 55 |
| 4 | Mật độ 71–110% | Quá thưa không gom được, quá đông thì hỗn loạn |
| 5 | ≥ 6 heo già có Hòa nhập ≥ 65 | Nguồn ứng viên thủ lĩnh |
| 6 | Không có dịch nhóm A/B đang hoạt động, không có khu Chuồng Nhiễm | Dịch bùng lên khi đang có bầy → bầy “lung lay” (mất hiệu ứng) đến khi dập dịch |
| 7 | Cấp trại ≥ 6 và đã qua ≥ 4 đợt lớn | Đàn phải từng “cùng sống sót” |


B. Ba bậc Bầy Đàn



| Bậc | Điều kiện thêm | Hiệu ứng toàn đàn |
| --- | --- | --- |
| Sơ Khai | Đạt 7 điều kiện trên | Tinh thần hồi +20%; hoảng loạn lan −20%; Hòa nhập +1/ngày; tính chất xấu Hư Thể chỉ thức 50% số lần; mở cuộc bầu thủ lĩnh |
| Vững | Có thủ lĩnh ≥ 5 ngày + SC ≥ 70 | Thêm: heo non chết do thiếu mẹ −50%; lùa đàn −20% Stamina; quái trộm bắt heo chậm +1 giây |
| Hưng Thịnh | Bầy Vững ≥ 10 ngày + SC ≥ 90 + ≥ 1 tầng Truyền Thừa | Thêm: sinh sản +5%; Tinh thần không mất quá 15 mỗi lần kích động; UFO khóa mục tiêu chậm +1 giây |


Tan rã: điều kiện 1–4 hoặc 6 bị vi phạm liên tục 3 ngày → bầy tan (SC < 30 thì tan sau 1 ngày). Thủ lĩnh vẫn sống nhưng mất hiệu ứng cho tới khi bầy tái lập.

C. Thủ lĩnh sáng lập



| Thành phần | Quy định |
| --- | --- |
| Ứng viên | Heo già trong bầy, Hòa nhập ≥ 65 (Kim Thọ: ≥ 50), sống cùng đàn ≥ 8 ngày, Tinh thần ≥ 60, không bệnh |
| Tỉ lệ hình thành/ngày | 6% + 0,15% × (Hòa nhập − 65) + 2% nếu từng sống sót 1 đợt lớn; ×1,5 nếu là Kim Thọ; tối đa 20%/ngày |
| Hiệu ứng “Neo tinh thần” | Trong 20 m quanh thủ lĩnh: Tinh thần heo không xuống dưới 25 (không Hoảng loạn); hoảng loạn lan −40%; Tinh thần hồi ×2. Thủ lĩnh bị tấn công trực tiếp → Neo tắt 30 giây |
| Mất thủ lĩnh | Thủ lĩnh chết/bị bắt: toàn đàn Tinh thần −25 tức thời. Chỉ mất toàn bộ tầng Truyền Thừa khi đồng thời đàn bị phá ≥ 70% trong cùng sự cố |


D. Kế thừa & Bồi Dưỡng Kế Vị



| Thành phần | Quy định |
| --- | --- |
| Ứng viên kế vị | Heo trưởng thành/già trong bầy, Hòa nhập ≥ 75, ở bầy ≥ 10 ngày. Kế vị không cần là heo già |
| Tỉ lệ kế vị/ngày (khi ghế trống) | Heo thường 5%;  heo Kim Thọ 20%  — kể cả khi thủ lĩnh trước không phải Kim Thọ |
| Bồi Dưỡng Kế Vị | Người chơi chỉ định 1 ứng viên (khi thủ lĩnh còn sống). Mỗi ngày đủ 3 việc: cho ăn  Cám Linh Chi  (bậc II), giữ ứng viên trong 15 m quanh thủ lĩnh ≥ 12 giờ, ứng viên không dưới Tinh thần 60 → +3% tỉ lệ kế vị và +3% tỉ lệ Truyền Thừa (tối đa +30%). Bỏ 1 ngày: mất 5% |
| Truyền Thừa (khi kế vị thành công) | Tung tỉ lệ nhận 1 tầng Truyền Thừa: heo thường 25%, Kim Thọ 50% (+ bồi dưỡng). Kim Thọ đạt tỉ lệ thì có thêm 30% nhận tầng thứ 2. Tối đa 3 tầng cho cả bầy |
| Hiệu ứng mỗi tầng | Ngưỡng Neo tinh thần +5 (25 → 30 → 35 → 40); bán kính Neo +5 m; Hòa nhập toàn đàn +0,5/ngày; sinh sản bầy +3%; hồi phục sau sự kiện +10% |



### 4.7. Heo già, xác heo & xử lý



| Trạng thái / Hành động | Hiệu ứng / Chi phí |
| --- | --- |
| Heo già còn sống | Ăn +20%; sinh s ả n − 8 5%;  Tinh thần hồi chậm −20%;  m ỗ i 10 heo già vư ợ t ngư ỡ ng m ề m (15% đàn):  Áp Lực Sự Kiện +Thấp/ngày |
| Xác heo | Không t ự  bi ế n m ấ t . Nằm trong trại: trừ  1  s ứ c ch ứ a ,  Sinh C ả nh −3/ngày ,  áp l ự c b ệ nh +8%/xác (t ố i đa +80 %), ÁLSK +Vừa /xác (t ố i đa + 12). Nằm trong ô tập kết của Khu Xử Lý: không trừ sức chứa, Sinh Cảnh −1/ngày, ÁLSK +Thấp |
| Thiêu hủy | 45G + 8 lít nhiên liệu; 6 giờ; không tạo sản phẩm |
| Tái chế thành thức ăn | 80G + 10 lít; 18 giờ; 1 ô xử lý; thu hồi  25-32 % lượng thức ăn tương đương — luôn đắt hơn mua thức ăn ở chợ |
| Bán cho Bí nhân | Chỉ khi Bí nhân xuất hiện (Chương 21);  h ạ n ng ạ ch 2–8 con;  Bí nhân  không  tr ả   vàng  mà đổi  b ằ ng v ậ t ph ẩ m  của hắn, luôn ở tỉ giá bất lợi ; m ỗ i giao d ị ch ÁLSK +3 đ ế n +10 t ứ c th ờ i |



## 5. Hệ Gen: dòng giống, tính chất & định danh

Mỗi con heo mang một dòng gen chính. Dòng gen quyết định một tính chất chính (cố định theo dòng), một mặt trái cố định, và 0–2 tính chất phụ được tung ngẫu nhiên cho từng cá thể. “Tính chất” khác “đặc tính” của nhân vật: tính chất không phải lúc nào cũng tốt — có thể giảm năng suất hoặc mang mầm dịch bệnh đặc biệt tiềm ẩn. Nguyên tắc bắt buộc: độ hiếm chỉ nói độ khó tìm và khó định danh, không nói sức mạnh.


### 5.1. Sáu bậc độ hiếm



| Bậc | Số dòng | Tỉ lệ xuất hiện tự nhiên / heo con | Hiển thị lần đầu | Giám định được ở |
| --- | --- | --- | --- | --- |
| Thường | 3 | 78% | Hiện tên ngay | Không cần |
| Khá | 3 | 17% | Hiện tên ngay | Không cần |
| Hiếm | 2 | 4% | Dấu “?” | Chợ ngày, chợ đêm, phòng nghiên cứu |
| Quý | 2 | 0,9% | Dấu “?” | Chợ ngày, chợ đêm, phòng nghiên cứu |
| Huyền Thoại | 1 | 0,1% | Dấu “?” | Chỉ chợ đêm sự kiện hoặc phòng nghiên cứu |
| Dị Biến | 1 | 0% (chỉ qua Mưa Bức Xạ 0,05%, sự kiện, Bí nhân) | Dấu “?” | Chỉ chợ đêm sự kiện hoặc phòng nghiên cứu |


F2 và tính chất “tăng tỉ lệ di truyền” làm tăng tỉ lệ các bậc từ Hiếm trở lên. Tỉ lệ bậc được chuẩn hóa lại để tổng luôn bằng 100%.


### 5.2. Mười hai dòng gen chính thức



| Dòng gen | Bậc | Vai trò | Cơ chế riêng (thay đổi cách chơi) | Mặt trái cố định | Tiềm ẩn |
| --- | --- | --- | --- | --- | --- |
| Hồng Điền | Thường | Ăn tạp, rẻ nuôi | Tự kiếm ăn trên đồng cỏ: cỏ tốt thay được tới 25% khẩu phần mua (heo khác 10%) | Gió Khô/đồng cỏ cạn: tăng trọng −15%; trần 105 kg | Không |
| Lam Khê | Thường | Chịu nóng | Tự tìm bùn/nước khi nóng; miễn Say Nắng nếu có bãi bùn hoặc bồn trong 20 m | Làm bẩn bãi bùn ×2 (dễ thành ổ Sốt Bùn); lứa −1 | Không |
| Mộc Cước | Thường | Chín muộn | Trưởng thành kéo dài 11–34 ngày (thay vì 11–28): cửa sổ bán dài hơn 6 ngày; già muộn nên ít áp lực heo già | Đạt 95 kg trễ 2 ngày | Không |
| Thổ Trầm | Khá | Ổn định tâm lý | Tâm Tĩnh : Tinh thần chỉ mất 50% khi bị kích động; không bị lây hoảng loạn; heo khác trong 5 m mất Tinh thần −20% | Chậm, không chạy trốn: UFO khóa nhanh hơn 1 giây; thịt tối đa A | 1% Sốt Bùn mạn |
| Xích Mao | Khá | Gen xấu có thể thuần hóa | Huyết Nóng : khi chưa thuần hóa không có mặt tốt. Sau khi định danh, mở nghiên cứu  Thuần Hóa Huyết Thống  (1.200G, 24 giờ): nuôi bằng  Cám Hạ Hỏa  + mật độ ≤ 90% → mặt trái giảm 70%; trong Trạng thái Bầy Đàn → mặt trái tắt hẳn và mở mặt tốt ẩn: hạng thịt +1 bậc, tăng trọng +8% | Tinh thần mất ×1,5; mật độ > 90% cắn heo bên cạnh; ăn +15%; hay tách đàn (Hòa nhập tăng một nửa) | 3% Lôi Tâm nhẹ |
| Phong Mẫu | Khá | Mẹ khéo nuôi | Nái nhận nuôi heo non mồ côi trong 10 m (chết do thiếu mẹ 12% → 0%); lứa +1. Đực: nái được phối +8% thụ thai | Nái giữ con: bế heo non tốn Stamina ×2, 20% bị húc (HP −10) | 2% Rối Loạn Sinh Sản |
| Thiết Bì | Hiếm | Kháng dịch | Miễn Ghẻ Ký Sinh; không làm “cầu nối” lây bệnh tiếp xúc — đặt xen giữa đàn như vách ngăn sống | Say Nắng ×1,5; thụ thai −12% | 15% Cứng Khớp |
| Tinh Quang | Hiếm | Thịt cao cấp | SC ≥ 60 suốt giai đoạn Đang lớn: thịt chắc chắn ≥ A (30% lên S); chợ đêm có đơn đặt hàng riêng | SC < 50 trong giai đoạn lớn: thịt tụt về C | 8% Nấm Phát Quang |
| Lôi Mạch | Quý | Phản xạ & dẫn đàn | Báo Động Bầy : khi quái vào ranh giới, tự dẫn tối đa 8 heo trong 10 m chạy về mái trú gần nhất; nhóm được dẫn không mất Tinh thần vì chạy loạn; UFO khóa nhóm chậm +2 giây. Tăng trọng +22%, xuất chuồng sớm 3 ngày | Giông Bão: Tinh thần mất ×1,5; ăn +10% | 10% Lôi Tâm |
| Mộng Nhãn | Quý | Linh cảm | Giấc Báo : 12 giờ trước Cổng/Sương mù và 1 ngày trước đợt lớn, heo tụ về hướng nguy cơ → tình báo miễn phí +1 cấp (hướng + khung giờ ±1 giờ). Tỉ lệ di truyền 25% → 40%; F2 đột biến +3%; trong Bầy Đàn giảm ÁLSK sương mù 30% | Sương Mù: Tinh thần −3/giờ; mỗi con ÁLSK +Thấp | 5% Mộng Du |
| Kim Thọ | Huyền Thoại | Trụ cột Bầy Đàn | Trụ Cột : Hòa nhập khởi đầu 60, tăng ×2; tính bằng 3 con vào ngưỡng Bầy Đàn; hạ ngưỡng Tinh thần bầy 60 → 55.  Ổn Định : heo trong 10 m không mất quá 10 Tinh thần/lần.  Trường Thọ : tuổi thọ 56 ngày; heo già vẫn sinh sản (−15%).  Chính Thống : ứng viên thủ lĩnh chỉ cần Hòa nhập 50, tỉ lệ ×1,5; kế vị 20%/ngày; Truyền Thừa 50% (+30% tầng thứ 2) | Tăng trọng −15%; trần 95 kg; ăn +20%; SC < 60 ngừng sinh sản; Kim Thọ đang là thủ lĩnh chết → đàn Tinh thần −40 | 3% Rối Loạn Sinh Sản |
| Hư Thể | Dị Biến | Tiềm năng có điều kiện | Tung 1 tính chất Dị Biến tốt + 1 tính chất Dị Biến xấu (mục 5.3). Tính chất xấu ở trạng thái  Ngủ , chỉ thức tỉnh khi heo gặp trạng thái xấu; giữ được sạch thì bị  phong ấn | Không bán được ở chợ ngày | Hư Mạch chỉ khi tính chất xấu thức tỉnh |



### 5.3. Tính chất phụ & bảng Dị Biến

Mỗi cá thể tung số tính chất phụ: 0 phụ (40%), 1 phụ (45%), 2 phụ (15%). Dòng Thường/Khá: 60% phụ tốt – 40% phụ xấu; dòng Hiếm trở lên: 50% – 50%. Tính chất phụ cũng là hành vi, không chỉ là cộng/trừ chỉ số.



| Phụ TỐT | Hành vi / hiệu ứng | Phụ XẤU | Hành vi / hiệu ứng |
| --- | --- | --- | --- |
| Trung Thành | Luôn bám theo thủ lĩnh/đàn, không bao giờ chạy lạc khi Hoảng sợ | Tò Mò | Hay ra rìa trại ban đêm; là mục tiêu dễ của Kẻ Bắt Heo |
| Lì Đòn | Sống sót 1 lần khi bị quái hạ (còn 1 HP), 1 lần/đời | Cắn Đàn | Mật độ > 100%: cắn heo bên cạnh, gây vết thương dễ nhiễm trùng |
| Kêu Báo | Phát hiện quái tàng hình trong 6 m và kêu báo động | Ồn Ào | Khi Hoảng sợ, kêu làm heo trong 8 m mất Tinh thần −2/giây |
| Mắn Đẻ | Nái +1 con/lứa | Hiếm Muộn | Thụ thai −10%; cần thú y chẩn đoán mới phân biệt với xui ngẫu nhiên |
| Ăn Khỏe | Thức ăn cho 1 kg −6% | Kén Ăn | Bỏ bữa khi đổi loại cám: tăng trọng ngày đó −30% |
| Sạch Sẽ | Không làm bẩn khu nằm; SC khu có nó −50% hao | Đoản Thọ | Tuổi thọ −3 ngày |


Khắc chế tính chất xấu sau khi định danh. Mọi tính chất phụ xấu và mặt trái đều có ít nhất một cách hạn chế, chỉ mở ra sau khi dòng gen được định danh: Tò Mò → rào thép khu rìa; Cắn Đàn → mật độ ≤ 90%; Ồn Ào → Còi Chăn Heo; Hiếm Muộn → thú y chẩn đoán + thuốc nội tiết; Kén Ăn → giữ một loại cám cố định; Đoản Thọ → không khắc phục (bán sớm).

Bảng tính chất Dị Biến (chỉ dòng Hư Thể): 1 tốt + 1 xấu. Tính chất xấu ngủ từ lúc sinh ra; thức tỉnh ngay khi heo dính dù chỉ 1 lần một trong các trạng thái: Tinh thần < 40 • Hoảng loạn • Kém ăn/bỏ bữa • cân nặng thấp hơn chuẩn giai đoạn > 10% • mắc bất kỳ bệnh nhóm A/B. Trại đang ở Trạng thái Bầy Đàn: mỗi lần dính chỉ 50% làm thức tỉnh. Qua hết giai đoạn Đang lớn mà chưa thức tỉnh → tính chất xấu bị phong ấn vĩnh viễn, heo sống như heo thường và vẫn giữ tính chất tốt.



| Tính chất Dị Biến | Loại | Hiệu ứng | Cách giữ phong ấn / tận dụng |
| --- | --- | --- | --- |
| Thịt Hư Không | Tốt | Chợ đêm có đơn trao đổi riêng, giá trị ×3 | Luôn hoạt động |
| Vô Ảnh | Tốt | UFO không khóa được chùm kéo vào cá thể này | Luôn hoạt động |
| Cộng Sinh | Tốt | Heo trong 5 m tăng trọng +5%, Tinh thần hồi +10% | Luôn hoạt động |
| Hút Sương | Xấu | Khi thức: ÁLSK +Vừa/ngày, thiên về sương mù | Giữ Tinh thần ≥ 60, tránh để heo ra ngoài khi Sương Mù |
| Tự Phân Rã | Xấu | Khi thức: tuổi thọ −50%; xác gây SC −6/ngày | Cám Cao Cấp đều đặn, không để hụt cân |
| Hư Nhiễm | Xấu | Khi thức: mang Hư Mạch, lây qua tiếp xúc | Tách khỏi khu có bệnh; tiêm phòng các bệnh nhóm A |



### 5.4. Khám phá dấu “?” và giám định

Khi người chơi nhận heo mang dòng gen từ Hiếm trở lên lần đầu tiên, thẻ heo hiển thị “?” thay cho tên giống, tính chất chính, mặt trái và tính chất phụ. Người chơi chỉ thấy ngoại hình, cân nặng, tuổi. Có 4 cách để mở thông tin:



| Kênh | Hiếm | Quý | Huyền Thoại | Dị Biến | Chi phí & thời gian |
| --- | --- | --- | --- | --- | --- |
| Chợ ngày | 70% | 40% | Không thể | Không thể | 300G / 900G; 1 giờ; 5 Stamina (đi chợ) |
| Chợ đêm – sự kiện “Giám Định Cấp Cao” | 90% | 72% | 50% | 35% | 450G / 1.300G / 3.500G / 5.000G; 2 giờ; chỉ xuất hiện 12% số đêm (từ giữa game) |
| Phòng nghiên cứu trại | 95% | 85% | 65% | 50% | 700G+1 thuốc thử / 2.000G+2 / 6.500G+4 / 9.000G+6; 25/30/40/45 Stamina; 18/24/36/48 giờ |
| Thông tin từ sự kiện đặc biệt (Bí nhân…) | 100% | 100% | 100% | 100% | Đổi  “H ồ  Sơ Gi ố ng Loài”  với Bí nhân (không nhận vàng; đòi heo gen “?” hoặc vật phẩm bậc III, giá trị 150–200%).  N ế u heo kh ớ p mô t ả  xu ấ t hi ệ n (k ể  c ả  l ầ n đ ầ u) s ẽ  hi ệ n tên và tính ch ấ t ngay, không có “?” |


Luật giám định

Giám định thành công: dòng gen được định danh vĩnh viễn trong ván chơi; mọi heo cùng dòng về sau hiện tên và tính chất ngay, không cần giám định lại. Kênh đó bị khóa 12 giờ trong game trước lần giám định tiếp theo.

Giám định thất bại: mất phí, kênh đó bị khóa 24 giờ trong game với dòng gen đó. Mỗi lần thất bại liên tiếp cùng dòng tại cùng kênh cộng +5% cho lần sau (tối đa +15%).

Chợ ngày có trần giám định: tối đa bậc Quý. Huyền Thoại và Dị Biến luôn thất bại tại chợ ngày — hệ thống không cho đặt yêu cầu.

Phòng nghiên cứu không cướp việc của chợ: phí cao gấp ~2,2 lần chợ, tốn nhiều Stamina, thời gian dài, và bắt buộc có NPC Nhà Nghiên Cứu Gen đã mở khóa đặc tính chuyên môn “Giải Mã Sâu”.

Bệnh tiềm ẩn của từng cá thể không lộ ra khi giám định dòng; cần Sàng lọc thú y ở chợ ngày (120G/con) hoặc NPC Bác sĩ thú y.


## 6. Chỉ số Sinh Cảnh & Dịch bệnh

“Mức độ môi trường” được đổi tên chính thức thành Chỉ số Sinh Cảnh (SC), thang 0–100, đo độ sạch và cân bằng sinh thái của trang trại. SC thấp làm dịch bệnh dễ phát sinh, nhưng SC cao không bao giờ đưa xác suất dịch bệnh về 0 — dịch bệnh đặc biệt từ gen và dịch vùng vẫn có thể xảy ra.


### 6.1. Năm bậc Sinh Cảnh



| SC | Tên bậc | Hệ số dịch | Đàn heo & Bầy Đàn | Kinh tế & hệ khác |
| --- | --- | --- | --- | --- |
| 90–100 | Trong Lành | ×0,60 | Mở bậc Bầy Đàn  Hưng Thịnh ; Hòa nhập +1/ngày thêm; Tinh thần hồi +15%; tính chất xấu Hư Thể thức tỉnh −25% | Tăng trọng +4%; +5% cơ hội lên hạng thịt; Tinh Quang/Kim Thọ phát huy trọn vẹn; quái chú ý −10% |
| 70–89 | Ổn Định | ×0,80 | Mở bậc Bầy Đàn  Vững ; Tinh thần hồi +5%; heo non chết do rét/thiếu mẹ −30% | Kim Thọ sinh sản bình thường (≥ 60); chi phí thuốc −10% vì bệnh nhẹ hơn |
| 50–69 | Trung Tính | ×1,00 | Chỉ đủ để hình thành Bầy Đàn  Sơ Khai ; Hòa nhập tăng bình thường (+3/ngày); không thưởng | Chuẩn. Tinh Quang dưới 60 không chắc hạng A |
| 30–49 | Ô Nhiễm | ×1,50 | Tối đa 80 heo được tính gắn kết →  không thể lập Bầy Đàn ; bầy đang có sẽ tan sau 3 ngày; Hòa nhập chỉ +1,5/ngày | Tăng trọng −5%; ÁLSK +Vừa/ngày |
| 0–29 | Ô Uế | ×2,30 | Bầy tan sau 1 ngày; Hòa nhập −1/ngày; Tinh thần −2/ngày | Tăng trọng −12%; ÁLSK +Cao/ngày; mùi hôi thu hút quái +15% |



### 6.2. Nguồn tăng/giảm Sinh Cảnh mỗi ngày



| Tăng SC | Giảm SC |
| --- | --- |
| Dọn bãi / vệ sinh: +4 mỗi lần (tối đa +12/ngày) | Mỗi xác chưa xử lý: −3 |
| NPC Bác sĩ thú y làm việc: +2 | Mật độ 101–120%: −1; 121–150%: −2; >150%: −4 |
| Đồng cỏ tốt (không quá tải): +1 | Nước bẩn / bồn chưa thay: −2 |
| Thủ lĩnh đàn, không có xác: +1 | Mưa hoặc Nồm ẩm khi thoát nước kém: −2 |
| Hệ thống thoát nước (công trình trại): +1 | Sau đợt quái có heo chết: −5 một lần |
| Tự điều chỉnh: nếu không có tác động, SC trôi dần về 60 với tốc độ 1 điểm/ngày | Dịch bệnh đang hoạt động trong trại: −1 mỗi cụm nhiễm |



### 6.3. Năm “động từ” xử lý sức khỏe

Mỗi mối đe dọa sức khỏe không phải là một dòng trừ chỉ số chờ uống thuốc, mà đòi hỏi một tổ hợp hành động khác nhau. Chỉ dùng 5 hành động để không biến thành 10 trò chơi con:



| Động từ | Người chơi làm gì | Chi phí điển hình |
| --- | --- | --- |
| Cách ly | Rào tạm một khu, lùa heo nhiễm/nghi nhiễm vào; khóa đường đi giữa các khu | Rào tạm 30G/đoạn; heo cách ly Tinh thần −10/ngày |
| Xử lý môi trường | Tháo nước, rải vôi, thay ổ lót, đóng tạm bãi bùn hoặc mái trú bị nhiễm | Vôi 20G/ô; 8–12 Stamina/ô; khu bị đóng 12–24 giờ |
| Điều trị cá thể | Tiêm/cho uống thuốc từng con, theo dõi liệu trình | 40–150G/con; 2 Stamina/con |
| Phòng ngừa đàn | Tiêm phòng heo chưa nhiễm, chia nhỏ đàn, bổ sung chất tăng miễn dịch | Vắc-xin 50G/con; chỉ có tác dụng với heo chưa nhiễm |
| Nghiên cứu / nguồn chữa đặc biệt | Phòng nghiên cứu, vật phẩm sự kiện, giao dịch với Bí nhân | Thời gian dài, vật phẩm hiếm |



### 6.4. Mười mối đe dọa sức khỏe theo 4 nhóm



| Tên | Nguồn phát | Cơ chế đặc trưng (vì sao nó khác) | Tổ hợp xử lý |
| --- | --- | --- | --- |
| A. DỊCH TRUYỀN NHIỄM — lây giữa các con |
| Hô Hấp Lạnh | Mưa/Rét Đậm, Miễn dịch đàn thấp | Nghịch lý mái trú : lây mạnh khi nhiều heo chen trong không gian kín (lây ×2 khi mái trú > 80% chỗ). Người chơi phải chọn: nhét cả đàn vào trú để tránh rét, hay chia đàn để  giảm lây. Heo nhiễm ho → Tinh thần đàn quanh đó −1/giờ | Phòng ngừa đàn (chia nhỏ) + điều trị cá thể |
| Ghẻ Ký Sinh | Nồm Ẩm, ổ lót bẩn | Tồn tại dai dẳng : ký sinh sống trên heo và trong ổ lót của mái trú. Chữa heo mà không thay ổ lót → 3 ngày sau tái nhiễm. Heo ngứa gãi rào: độ bền rào gỗ −1/ngày; kéo dài > 3 ngày thì hạng thịt −1 | Điều trị cá thể + xử lý môi trường (thay ổ lót) |
| Dịch Tả Heo | Dịch vùng (chợ báo động), heo mua về | Khẩn cấp cấp trại : phát hiện ca đầu → cả trại vào “Phong tỏa” 3 ngày: cấm bán heo sống, chợ không mua. Heo nhiễm nặng chết 8%/ngày; người chơi phải chọn tiêu hủy sớm (mất heo, dừng lây) hay cố chữa (tốn kém, rủi ro lan) | Cách ly + phòng ngừa đàn (vắc-xin) + quyết định tiêu hủy |
| B. BỆNH MÔI TRƯỜNG — gắn với địa điểm |
| Sốt Bùn | Bãi bùn bẩn + Mưa | Bệnh của địa điểm : con nhiễm đi vào bãi bùn biến bãi đó thành “ổ nhiễm”; mọi heo bước vào có 15%/lần nhiễm. Chữa heo mà không xử lý bãi → mai lại nhiễm. Phải đóng bãi, tháo nước hoặc chuyển đàn | Xử lý môi trường + điều trị cá thể |
| Chuồng Nhiễm (trạng thái khu) | Mật độ khu > 120% + SC < 40 + có xác/chất thải | Ổ dịch cấu trúc : cả một khu chuyển sang trạng thái Chuồng Nhiễm; heo khỏe đưa vào cũng có nguy cơ mắc bất kỳ bệnh nhóm A/B nào ×2. Không thể giải bằng tiêm từng con | Giảm mật độ + cách ly khu + xử lý môi trường (24 giờ) |
| C. TÌNH TRẠNG CẤP / MÃN TÍNH — không lây |
| Say Nắng (cấp tính) | Nắng Gắt, thiếu nước/bùn | S ự  c ố  nhìn th ấ y đư ợ c : heo quá nhi ệ t  thở dốc,  đi ch ậ m  và tự  tìm  bãi bùn/bồn  nư ớ c /mái trú gần nhất. Nếu trong 25 m không có nguồn mát, heo đứng lì giữa đồng cỏ;  2 gi ờ  game không  hạ nhiệt  → g ụ c; g ụ c thêm 1 gi ờ  → ch ế t.  Người chơi cứu bằng: huýt/lùa heo tới bãi bùn, dội  Xô Nước  (3 Stamina/con), dựng  Mái Che Tạm  tại chỗ (tồn tại 1 ngày), hoặc Bơm Làm Mát phun sương | Phản ứng tức thời (lùa, dội nước, Mái Che Tạm) + hạ tầng phòng ngừa (bãi bùn rải khắp đồng cỏ, bồn nước, Bơm Làm Mát) |
| Rối Loạn Sinh Sản (mãn tính) | Tinh thần thấp kéo dài; gen Phong Mẫu/Kim Thọ | Khó chẩn đoán : không có triệu chứng, chỉ thấy nái phối hụt liên tiếp. Người chơi không biết là xui hay bệnh cho tới khi thú y chẩn đoán | Chẩn đoán thú y + điều trị cá thể (nghỉ phối 3 ngày) |
| D. BỆNH ĐẶC BIỆT THEO GEN — luật riêng |
| Nấm Phát Quang | Gen Tinh Quang; Mưa Bức Xạ + ẩm | Bệnh là cơ hội : heo phát sáng mạnh dần mỗi ngày. Chữa ngay = an toàn. Cố tình giữ: chợ đêm trả giá tăng dần (×1,5 → ×3) và mở giao dịch Bí nhân riêng, nhưng đàn trở thành “đèn hiệu” — UFO chú ý +10%/ngày, cộng dồn | Nghiên cứu + vệ sinh, hoặc chủ động giữ để bán |
| Lôi Tâm | Gen Lôi Mạch | Cơn bệnh có báo trước : khi Tinh thần < 30, heo run giật 10 giây (dấu hiệu nhìn thấy), sau đó lên cơn: chạy điên cuồng làm heo quanh 8 m Hoảng sợ, rồi 25% đột tử. Trong 10 giây báo trước, dùng thuốc an thần hoặc lùa vào mái trú để dập cơn | Sàng lọc sớm + phản ứng tức thời |
| Hư Mạch | Gen Hư Thể; sự kiện huyền bí | Y tế thường vô hiệu : thuốc, vắc-xin không có tác dụng. Mỗi con nhiễm ÁLSK +5 tức thời và +Thấp/ngày; heo nhiễm ban đêm đi  về rìa trại như bị gọi. Có thể chấp nhận giữ để đổi lấy sự kiện hiếm | Cách ly + nghiên cứu / sự kiện Lò Mổ Hư Không / Bí nhân |



### 6.5. Công thức phát dịch & lây lan

P(phát dịch/cụm/ngày) = clamp(1% × Hệ số SC × Hệ số mật độ × Hệ số thời tiết × Hệ số xác × Hệ số đặc biệt ; 0,2% ; 35%)

Chỉ áp dụng cho nhóm A và B. Sau ca đầu tiên, cứ 6 giờ kiểm tra lây một lần: P(lây) = Lây gốc (10%) × Mật độ × SC × Thời tiết × Hệ số Tinh thần (Bất an ×1,1) × Hệ số thủ lĩnh (0,75 nếu có). Thủ lĩnh không chữa bệnh, chỉ giảm hỗn loạn di chuyển nên gián tiếp giảm tiếp xúc. Nhóm C không lây; nhóm D có luật riêng như bảng trên. Cứng Khớp và Mộng Du là tính chất xấu tiềm ẩn của gen, không tính là bệnh.


## 7. Thời gian trong ngày & Thời tiết

Một ngày 24 giờ chia 4 buổi: 12 giờ ban ngày chia đều cho Sáng, Trưa, Chiều (mỗi buổi 4 giờ); Tối chiếm 12 giờ còn lại. Buổi chỉ cộng/trừ phần trăm vào chỉ số đang có, không bao giờ tạo tính chất mới.

Hình 7.1 — Chu kỳ 24 giờ: Sáng 06–10, Trưa 10–14, Chiều 14–18, Tối 18–06 (chợ ngày và chợ đêm).


### 7.1. Hiệu ứng nền của 4 buổi



| Buổi | Khung giờ | Hiệu ứng nền | Hoạt động phù hợp |
| --- | --- | --- | --- |
| Sáng | 06:00–10:00 | Stamina việc ngoài trời −5%; SC hồi +5%; heo ăn +5% hiệu quả | Việc nặng, dọn dẹp, cho ăn |
| Trưa | 10:00–14:00 | Stamina ngoài trời +10%; nhiệt +15% (stress heo); nhu cầu nước +10% | Việc trong nhà, nghiên cứu, đi chợ |
| Chiều | 14:00–18:00 | Trung tính; chợ ngày đông khách: sức mua +10% | Bán heo, ký hợp đồng |
| Tối | 18:00–06:00 | Tầm nhìn −15%; Stamina +5% nếu không có đèn; quái trộm lẻn vào +20% | Chợ đêm, ngủ, canh gác |



### 7.2. Mười loại thời tiết

Thời tiết kéo dài 1–3 ngày. Loại “Cố định” (Nắng Gắt, Mưa, Giông Bão, Mưa Bức Xạ) không bị ảnh hưởng bởi Sáng/Trưa/Chiều/Tối. Mỗi thời tiết đi kèm một trạng thái có thể dính lên người (khi làm việc ngoài trời) và lên heo; trạng thái được phòng tránh bằng trang bị hoặc công trình tương ứng.



| Thời tiết | Tần suất / loại | Hiệu ứng chung | Trạng thái lên người (ngoài trời) | Trạng thái lên heo | Phòng tránh |
| --- | --- | --- | --- | --- | --- |
| Quang Đãng | 25% • Thường | Nhận đủ hiệu ứng 4 buổi | Dễ chịu : Tinh thần làm việc tốt, không phạt | — | — |
| Nhiều Mây | 20% • Thường | Stamina ngoài trời −3%; Trưa chỉ +7% | — | — | — |
| Nắng Gắt | 12% • Cố định | Stamina +15%; nước +20% | Cảm Nắng : sau 2 giờ liên tục ngoài trời 10:00–16:00, 20% dính: Stamina tối đa −10% đến khi nghỉ bóng mát 1 giờ | Nguy cơ Say Nắng | Mái Che Tạm ; bãi bùn, b ồ n, Bơm Làm Mát |
| Mưa | 15% • Cố định | Stamina +10%; di chuyển −5% | Ướt Lạnh : sau 1 giờ ngoài trời, Miễn dịch hao ×1,5; kéo dài 3 giờ sau khi vào nhà | Hô Hấp Lạnh ×1,15 nếu thiếu mái trú | Áo Mưa Dầu; mái trú |
| Giông Bão | 6% • Cố định | Stamina +20%; lưới điện dao động; heo Tinh thần −10 mỗi tiếng sét gần | Sét Đánh : mỗi giờ ngoài trời xa mái che > 15 m có 3% bị sét (HP −40, choáng 3 giây).  Cảm Gió : mỗi 2 giờ ngoài trời 10% dính (Miễn dịch −10, Stamina +10% suốt ngày) | Hoảng sợ hàng loạt; công trình bị sét 2%/giờ | Vòng  Cách Đi ệ n (−80% sét); C ộ t Thu Lôi |
| Sương Mù | 8% • Thường | Tầm nhìn −35%; trinh sát −1 cấp nếu không có đèn/cảm biến | Lạc Hướng : bản đồ nhỏ không hiện gì ngoài 15 m; có nguy cơ bị Sương mù huyền bí cuốn đi | Tinh thần −1/giờ | Đèn Pha;  Mặt Dây  Đèn Bão |
| Rét Đậm | 5% • Thường | Stamina +10%; Miễn dịch hao +15% (Tối +10% thêm) | Cóng Tay : thời gian thao tác (sửa, tiêm, giám định) +15% | Heo non chết 3%/ngày nếu không sưởi | Quần Da Lót Bông ; sư ở i mái trú |
| Nồm Ẩm | 5% • Thường | SC −1/ngày; Stamina +5% | Trơn Trượt : lướt/né trên nền cứng 10% trượt ngã (mất 1 giây) | Ghẻ Ký Sinh ×1,4; cám trong kho hỏng 2%/ngày | Giày Đinh; kho khô |
| Gió Khô | 3% • Thường | Nước +10%; đồng cỏ hồi −30% | Khô Rát : mỗi lần chạy nhanh tốn +0,2 Stamina/giây | Hồng Điền mất nguồn cỏ | Bình nước mang theo |
| Mưa Bức Xạ | 1% (từ chu kỳ  6) • Cố định | Đột biến gen +1,5% cho heo Đang lớn | Nhiễm Xạ : mỗi giờ ngoài trời +1 tầng (tối đa 5); mỗi  tầng Miễn dịch −4; ≥ 3 tầng HP hồi −50% | Bệnh nhóm D ×1,5 | Áo Chì; ở trong nhà |



# PHẦN III — NHÂN VẬT, NPC, THÚ CƯNG & COMBAT


## 8. Nhân vật người chơi & bảng chỉ số

Bản 1.0 có đúng 2 nhân vật người chơi: Khoa (nam) và An (nữ). Hai nhân vật chỉ khác chỉ số khởi đầu và đặc tính, không khóa build. Nhân vật không có kỹ năng tấn công/phòng thủ bẩm sinh: sức chiến đấu hình thành từ trang bị, vật phẩm và võ kỹ. Đặc tính của nhân vật người chơi chỉ phục vụ công việc hằng ngày, không tác động vào phòng thủ.



| LUẬT CHỈ SỐ CỐ ĐỊNH Chỉ số gốc (STR, AGI, CTRL, RES) là  cố định , chỉ thay đổi khi nhân vật lên cấp và phân phối điểm. Trang bị, vật phẩm, thức ăn  không cộng thẳng vào chỉ số gốc  mà cho  chỉ số thưởng  hiển thị riêng (ví dụ: STR 24  (+3) ). Chỉ số thưởng làm tăng hiệu quả chiến đấu nhưng  không bao giờ được tính  vào điều kiện học võ kỹ, điều kiện dùng kỹ năng ngoài phái, hay điều kiện mở khóa đặc tính. |
| --- |



### 8.1. Hai nhân vật



| Hạng mục | Khoa (nam) — “Người làm trại” | An (nữ) — “Người đọc chợ” |
| --- | --- | --- |
| Chỉ số khởi đầu | STR 24 • AGI 18 • CTRL 18 • RES 22 | STR 17 • AGI 23 • CTRL 24 • RES 18 |
| Đặc tính cơ bản (mở ngay) | Tiếng Huýt Chăn Đàn : huýt sáo gọi tối đa 12 heo trong 20 m đi theo Khoa trong 60 giây thực; lùa theo cách này không tốn Stamina theo phút. Hồi chiêu 2 giờ game | Mắt Nhà Nghề : quan sát 1 con heo 5 giây để thấy 1 dấu hiệu sức khỏe hoặc tính chất phụ bị ẩn. Mỗi ngày 1 lần miễn phí phát hiện bệnh tiềm ẩn của 1 con (thay cho kiểm dịch 60G) |
| Đặc tính chuyên biệt (ẩn) | Người chơi chỉ thấy một ô khóa “???”, không thấy tên, hiệu ứng hay điều kiện cho tới khi hoàn thành đủ 6 khóa (Chương 10). Nội dung dành cho nhà thiết kế ở mục 10.8 |
| Thiên hướng gợi ý | Quản lý đàn, lao động nặng; hợp phái Trọng Kích / Sinh Tồn | Chợ, giám định, thông tin; hợp phái Tốc Bộ / Khống Chế |



### 8.2. Chỉ số chính



| Chỉ số | Gốc | Trần mềm / cứng | Vai trò |
| --- | --- | --- | --- |
| HP (Máu) | 100 | 140 / 180 | Về 0: bất tỉnh, tỉnh lại ở nhà chính sau đợt quái, Thể trạng −20 |
| Stamina (Thể lực) | 180 | 220 / 260 | Lao động, di chuyển nặng, combat (Chương 9) |
| Thể trạng (Condition) | 100 | 100 | “Nợ sức khỏe” nhiều ngày; giảm Stamina tối đa hiệu dụng |
| Miễn dịch | 100 | 100 | Chống thời tiết xấu, lây nhiễm từ heo bệnh |
| STR – Sức mạnh | 17–24 | 40 / 55 | Sát thương vũ khí nặng +1%/điểm; mang vác; sửa chữa |
| AGI – Nhanh nhẹn | 18–23 | 40 / 55 | Tốc chạy +0,4%/điểm; né; vũ khí nhẹ +1%/điểm |
| CTRL – Khống chế | 18–24 | 40 / 55 | Chí mạng +0,2%/điểm; hiệu lực choáng/làm chậm; vũ khí tầm xa |
| RES – Bền bỉ | 18–22 | 40 / 55 | Giảm sát thương 0,5%/điểm; giảm tích lũy Daily Load, mất Thể trạng |


Phát triển: nhân vật có Cấp 1–30. Mỗi cấp +2 điểm phân phối vào chỉ số gốc và +3 HP; mỗi 5 cấp +8 Stamina tối đa. EXP nhân vật đến từ lao động (1 EXP / 10 Stamina tiêu hao), hạ quái (2–400 EXP) và sống sót đợt quái. Vượt trần mềm 40: mỗi điểm tốn gấp đôi. Chỉ số thưởng từ trang bị có trần riêng: tối đa +8 mỗi chỉ số, không vượt trần cứng 55.


## 9. Stamina (Thể lực) — mô hình chi tiết



| MÔ HÌNH ĐÃ CHỐT Thể lực  có giới hạn : hồi chậm khi đang combat, hồi nhanh khi ngoài combat, và được  công trình Hỗ trợ  tăng tốc hồi trong phạm vi. Không dùng thể lực vô hạn vì sẽ biến game thành ARPG chặt chém và làm lu mờ vai trò công trình. Nhịp mong muốn:  Đánh – Giữ chân – Rút lui – Trụ bọc lót – Tái giao tranh . |
| --- |



### 9.1. Hai lớp tiêu hao



| Lớp | Định nghĩa |
| --- | --- |
| Hao phí tức thời (hồi được) | Phần thể lực mất do gắng sức ngắn hạn; tự hồi khi nghỉ vài giây |
| Tải ngày (Daily Load) | Phần thể lực “ghi nợ” trong ngày; chỉ hồi bằng ăn, nghỉ dài hoặc ngủ. Tải ngày quá cao làm giảm Thể trạng cuối ngày |



### 9.2. Bảng tiêu hao theo loại công việc



| Nhóm | Công việc | Stamina gốc | % thành Tải ngày | Hệ số riêng |
| --- | --- | --- | --- | --- |
| Chăm sóc | Rải thức ăn / đổ máng nhỏ | 4 | 100% | +20% nếu bao > 20 kg |
|  | Thay nước bồn / dội Xô Nước | 6 / 3 | 100% | Nắng Gắt ×1,15 |
|  | Tiêm/cho uống thuốc (1 con) | 2 | 100% | +50% nếu heo hoảng |
| Vệ sinh | Dọn bãi / rải vôi / thay ổ lót (1 ô) | 8–12 | 100% | +25% nếu SC < 40; +15% khi Mưa |
|  | Kéo xác heo về Khu Xử Lý | 12 | 100% | Miễn dịch −2 nếu không có Găng Da Dày |
| Vận chuyển | Bế heo non / lùa heo trưởng thành | 3 / 10 mỗi phút | 70% | Heo Bất an +20%; STR gốc < 25: +25% |
|  | Mang vác nặng (> 35 kg) | +35% mọi việc | — | Thể trạng < 40: thêm +15% |
| Xây / Sửa | Tự tay phụ Phòng Xây Dựng (mỗi giờ) | 10 | 100% | Rút ngắn thời gian thi công/sửa 15% |
|  | Nạp đạn/nhiên liệu (mỗi công trình) | 5 | 100% | Chỉ ngoài đợt quái |
| Thu thập | Chặt/đập/gom vật liệu | 8/lần | 80% | Dụng cụ tốt −10% đến −30% |
| Thị trường | Đi chợ (1 lượt) | 5 | 100% | Mang heo theo: +2/con |
| Nghiên cứu | Giám định ở phòng nghiên cứu | 25–45 | 100% | Theo bậc gen |
| Di chuyển | Chạy nhanh | 1,3/giây | 20% | AGI gốc ≥ 30: 1,0/giây |
| Combat | Đòn nhẹ / đòn nặng | 2,5 / 7 | 30% / 40% | Theo hạng cân trang bị (mục 12.2) |
|  | Lướt né / đỡ đòn | 9 / 5 | 30% | AGI gốc ≥ 35: né −15% |
|  | Võ kỹ | 6–22 / Nộ / HP | 40% | Theo loại ô võ kỹ (mục 12.3) |


Mục tiêu cân bằng: chăm trại cơ bản đầu game tiêu 70–105 / 180 Stamina/ngày, còn 75–110 cho chợ, khám phá, chuẩn bị và combat.


### 9.3. Hệ số hiệu suất theo môi trường & sức khỏe

Stamina thực tế = Gốc × (1 + ΣMôi trường + ΣSức khỏe + ΣTải) × (1 − ΣGiảm trừ), giới hạn ×0,60–×2,00



| Nhóm | Điều kiện | Hệ số |
| --- | --- | --- |
| Buổi | Sáng / Trưa / Chiều / Tối không đèn | −5% / +10% / 0% / +5% |
| Thời tiết | Nắng Gắt / Mưa / Giông / Rét / Nồm / Nhiều Mây | +15% / +10% / +20% / +10% / +5% / −3% |
| Luật cố định | Thời tiết loại Cố định | Bỏ hệ số Buổi (giữ phạt tầm nhìn Tối) |
| Thể trạng | 80–100 / 60–79 / 40–59 / 20–39 / 0–19 | 0% / +5% / +10% / +20% / +35% |
| Trạng thái thời tiết | Ướt Lạnh / Cảm Gió / Cảm Nắng / Nhiễm Xạ ≥ 3 | +5% / +10% / trần −10% / +10% |
| Đói | Chưa ăn > 8 giờ / > 16 giờ | +10% / +25% |
| Tải | Bộ trang bị hạng Nặng / mang vác > 35 kg | +8% / +35% |
| Giảm trừ | Dụng cụ tốt / đặc tính / hào quang Trạm Hồi Sức (việc trại) | −10–30% / −8% / −10% |



### 9.4. Hồi phục



| Trạng thái | Tốc độ hồi |
| --- | --- |
| Đang combat | Sau 1,5 giây không ra đòn: hồi Hao phí tức thời 2,0/giây (≈ 15% tốc độ thường). Tải ngày không hồi |
| Ngoài combat ≥ 3 giây | 10/giây đến mức trước combat — đầy trong 3–4 giây |
| Trong hào quang Trạm Hồi Sức | +50% / +75% / +100% (cấp 1–3), kể cả khi đang combat |
| Nghỉ / ăn | Ngồi nghỉ 6/giờ game; bữa thường +20, bữa tốt +35 (tối đa 3 bữa/ngày) |
| Vùng mệt (< 20%) | Chạy −15%; đòn nặng chậm +20%; không khóa hành động |
| Quá sức | Tải ngày > 55%: Thể trạng −8 cuối ngày; > 75%: −15 và 20% Kiệt Sức |



### 9.5. Ngủ & cooldown ngủ



| Luật | Quy định |
| --- | --- |
| Ngủ chuẩn | Tua thời gian; Stamina ≥ 80% tối đa; xóa Tải ngày; Thể trạng +4 đến +10; Miễn dịch +3 đến +8 |
| Cooldown ngủ | 6 giờ game sau khi dậy |
| Nghỉ cưỡng bức | Chỉ bằng vật phẩm ở mục 9.6; 2 lần đầu mỗi chu kỳ không phạt; lần 3: +1 Mệt Dội; lần 4+: +2 |
| Mệt Dội | Mỗi tầng: Stamina tối đa −8%, hồi Thể trạng −15%; tối đa 4; giảm 1 tầng mỗi ngày sạch |
| Không ngủ được | Trong đợt quái, trong sự kiện Sương mù, khi có quái trong ranh giới trại |



### 9.6. Vật phẩm phá cooldown ngủ

Dùng chung 4 bậc vật phẩm: I – Thường, II – Tốt, III – Quý, IV – Cực Phẩm. Không vật phẩm nào bán đại trà bằng vàng. Ở chợ đêm, mỗi ô hàng ghi rõ món người bán muốn nhận; người chơi phải có đúng món đó (hoặc món cùng nhóm ngang giá) mới đổi được. Chỉ khoảng 18% ô hàng chấp nhận vàng, gần như chỉ với bậc I–II.



| Vật phẩm | Bậc | Tác dụng | Tác dụng phụ | Nguồn & ví dụ yêu cầu trao đổi |
| --- | --- | --- | --- | --- |
| Trà Lá Vông | I | Ngủ ngắn 3 giờ bất chấp cooldown; Stamina ≥ 50% | Không xóa Tải ngày; hôm sau Tải ngày +10% | Chợ đêm: đổi 2 heo con Thường hoặc 30 kg Cám Tăng Trọng; 18% ô nhận vàng (≈ 650G) |
| Thuốc An Thần | II | Ngủ chuẩn đầy đủ bất chấp cooldown | “Lờ Đờ” 2 giờ: chạy −10%, thao tác +15%; Miễn dịch −3 | Chợ đêm: đổi 1 heo trưởng thành hạng A đã kiểm dịch, hoặc 2 Bình Hồi Lực + 1 Lựu Khói; đấu giá |
| Hương Mê Thảo | III | Ngủ sâu: Stamina 100%, Thể trạng +6, xóa Tải ngày | +1 Mệt Dội ngay; Lờ Đờ 3 giờ; 10% “Mộng Mị” (ÁLSK +5) | Đấu giá; Bí nhân (đòi 1 heo gen “?” hoặc 3 heo già) |
| Đan Hồi Nguyên | IV | Ngủ ngay: Stamina 100%, Thể trạng +10, xóa 2 tầng Mệt Dội | “Hư Hỏa” 2 ngày: Stamina tối đa −10%, Miễn dịch −10; 1 lần/chu kỳ | Đấu giá; thưởng Boss; sự kiện Đỏ |
| Quả Mộng | IV | Ngủ ngay: Stamina 100%, Thể trạng +8, xóa 1 tầng Mệt Dội | Không tác dụng phụ ; không tính vào số lần nghỉ cưỡng bức | Chỉ từ sự kiện Xanh (Vườn Mười Hai Quả) và Bí nhân |



## 10. Hệ Đặc tính & Giấy Chuyên Biệt


### 10.1. Cấu trúc đặc tính



| Đối tượng | Loại đặc tính | Quy định |
| --- | --- | --- |
| Nhân vật người chơi | Đặc tính Cơ bản | Mở ngay khi chọn nhân vật; chỉ hỗ trợ việc hằng ngày |
| Nhân vật người chơi | Đặc tính Chuyên biệt | Ẩn hoàn toàn : chỉ hiện ô khóa “???”. Dùng giấy để lộ điều kiện từng khóa; tên và hiệu ứng chỉ hiện khi xong cả 6 nhiệm vụ. Chỉ hỗ trợ việc hằng ngày |
| NPC, thú cưng | Đặc tính Chuyên môn (duy nhất) | Cùng luật ẩn và 6 khóa. Đúng nghề: NPC/thú trang trại chỉ hỗ trợ trại; chỉ NPC chiến đấu và thú canh gác có đặc tính phòng thủ |



### 10.2. Cổng Giữa Game

Gồm 1 điều kiện bắt buộc và 1 trong 5 điều kiện phụ. Đạt cổng chỉ mở quyền có cơ hội nhận Giấy Chuyên Biệt, bản vẽ Cao cấp, giám định cấp cao ở chợ đêm; không bảo đảm nhận được.



| Loại | Điều kiện |
| --- | --- |
| Bắt buộc | Đã trải qua ≥ 8 đợt lớn (khoảng tinh chỉnh 8–10) |
| Phụ (chọn 1) | ①  Cấp trại ≥ 7 •  ②  Từng có đàn ≥ 120 con •  ③   Đã   đ ịnh danh ≥ 1 dòng Hiếm trở lên •  ④   Đã  hạ 1 Boss chu kỳ •  ⑤   Đ ang sở hữu ≥ 12 công trình phòng thủ còn hoạt động |



### 10.3. Nguồn nhận giấy (sau khi đạt cổng)



| Nguồn | Lam | Vàng | Tím | Đỏ | Ghi chú |
| --- | --- | --- | --- | --- | --- |
| Sống sót đợt lớn | 8% | 3% | 0,8% | 0,2% | 1 lần tung/đợt |
| Hạ Boss chu kỳ | 30% | 15% | 5% | 1,5% | Tung riêng |
| Đấu giá đêm (nguồn chính) | 15% | 10% | 5% | 2% | Xác suất có lô giấy mỗi đêm |
| Ô hàng chợ đêm (hiếm) | 5% | 2% | 0,6% | Không | Trao đổi theo yêu cầu người bán; Đỏ chỉ ở đấu giá |
| Sự kiện Vàng / Xanh | Có | Có | Hiếm | Rất hiếm | Phần thưởng lựa chọn |



### 10.4. Cơ chế 6 khóa

6 khóa lộ theo thứ tự 1 → 6. Dùng giấy = nhận số lượt mở; mỗi lượt nhắm khóa thấp nhất chưa lộ và tung theo tỉ lệ của khóa đó. Thất bại (“sịt”): mất lượt; lượt sau thử lại chính khóa đó. Giấy tiêu hao.

Lộ điều kiện chưa phải hoàn thành; phải làm xong nhiệm vụ. Tên và hiệu ứng vẫn ẩn cho đến khi xong cả 6.

Tiến độ trước khi lộ được ghi ngầm nhưng chỉ công nhận tối đa 50% (ví dụ duy trì đàn ≥ 50 con 10 vòng: đã làm 14 vòng → ghi 5/10).


### 10.5. Bảng tỉ lệ Giấy Chuyên Biệt



| Giấy | Số lượt mở | Khóa 1 | Khóa 2 | Khóa 3 | Khóa 4 | Khóa 5 | Khóa 6 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Xanh lam | 1 | 50% | 25% | 12% | 5% | Không | Không |
| Vàng | 1 + 25% có lượt 2 | 100% | 60% | 15% | 7% | 1% | Không |
| Tím | 1 chắc chắn + 40% lượt 2 | 100% | 90% | 60% | 39% | 12% | 8% |
| Đỏ | 2 chắc chắn + 30% lượt 3 | 100% | 100% | 80% | 60% | 45% | 35% |



### 10.6. Gia cường giấy & Đá Cường Hóa



| Bước | Đủ nguyên liệu, không đá | Mỗi Đá Cường Hóa | Trần tỉ lệ |
| --- | --- | --- | --- |
| +0 → +1 | 100% | Không cần | 100% |
| +1 → +2 | 70% | +6% (tối đa 5 viên) | 100% |
| +2 → +3 | 40% | +5% (tối đa 8 viên) | 80%  — không bao giờ vượt |


Thiếu nguyên liệu: tỉ lệ × (nộp / yêu cầu)^1,5, tối thiểu 10%. Thất bại chỉ mất nguyên liệu và đá, không hỏng giấy. Ràng buộc: Lam+3 < Tím+0; Vàng+3 < Đỏ+0. Kết quả tối đa ở +3: Lam 65/32,5/15,6/6,5/0/0 • Vàng 100/78/19,5/9,1/1,3/0 (lượt 2: 40%) • Tím 100/90/69/48/21/17 (lượt 2: 55%) • Đỏ 100/100/90/70/55/50 (lượt 3: 45%).


### 10.7. Nguyên liệu gia cường



| Giấy | +0 → +1 | +1 → +2 | +2 → +3 |
| --- | --- | --- | --- |
| Lam | 3 Mực Ấn + 200G | 5 Mực Ấn + 1 Tơ Vàng + 400G | 8 Mực Ấn + 2 Tơ Vàng + 700G |
| Vàng | 4 Tơ Vàng + 600G | 6 Tơ Vàng + 1 Bột Tử Tinh + 1.000G | 9 Tơ Vàng + 2 Bột Tử Tinh + 1.600G |
| Tím | 3 Bột Tử Tinh + 1.500G | 5 Bột Tử Tinh + 1 Huyết Triện + 2.500G | 8 Bột Tử Tinh + 2 Huyết Triện + 4.000G |
| Đỏ | 2 Huyết Triện + 4.000G | 3 Huyết Triện + 1 Tinh Hoa Dị Biến + 7.000G | 5 Huyết Triện + 2 Tinh Hoa Dị Biến + 12.000G |


Nguồn: Mực Ấn (I, chợ đêm); Tơ Vàng (II, sự kiện Vàng, rã giấy Lam → 2); Bột Tử Tinh (III, rã giấy Vàng → 2, Boss); Đá Cường Hóa (III, đấu giá, Boss 1–2 viên, sự kiện Đỏ); Huyết Triện (IV, Boss chu kỳ ≥ 15, sự kiện Đỏ, rã giấy Tím → 1); Tinh Hoa Dị Biến (IV, nghiên cứu heo Hư Thể, sự kiện Xanh).


### 10.8. Đặc tính chuyên biệt của 2 nhân vật (nội bộ — người chơi không thấy)



| Khóa | Khoa — Trại Chủ Lão Luyện | An — Người Đọc Dòng |
| --- | --- | --- |
| Hiệu ứng | Làm Dứt Điểm : 1 lần/ngày một việc chăm sóc/vệ sinh áp dụng cho cả khu với giá Stamina 1 ô.  Người Của Đàn : Bồi Dưỡng Kế Vị do Khoa trực tiếp làm +1%/ngày thêm.  Ngủ Say : ngủ chuẩn Thể trạng +3, cứ 2 ngày xóa 1 tầng Mệt Dội | Xem Trước Chợ : mỗi tối xem trước 1 mặt hàng chắc có ở chợ ngày mai.  Giữ Chỗ : 1 lần/chu kỳ giữ 1 ô hàng chợ đêm sang đêm kế.  Mặc Cả : trao đổi được chấp nhận khi hàng đưa chỉ đạt 90% giá trị yêu cầu |
| 1 – Đợt quái | Sống sót 10 đợt lớn mà nhà chính không bị phá | Sống sót 10 đợt lớn |
| 2 – Trang trại | Tự tay dọn vệ sinh 150 lần | Xây Bàn Tình Báo và dùng 40 lượt hỏi |
| 3 – Đàn heo | Giữ Trạng thái Bầy Đàn liên tục 10 ngày | Có ≥ 150 con cùng lúc, ≥ 20% là F2 |
| 4 – Gen | Định danh cả 2 dòng Quý | Định danh 6 dòng, ≥ 1 Huyền Thoại hoặc Dị Biến |
| 5 – Thị trường | Bán 500 heo đã kiểm dịch ở chợ ngày | Hoàn thành 30 giao dịch trao đổi ở chợ đêm |
| 6 – Thử thách | Có 1 lần kế vị thủ lĩnh nhận được tầng Truyền Thừa | Hoàn thành 3 sự kiện huyền bí đã có tình báo cấp 5 |



## 11. NPC & Thú cưng



| Quy tắc tách vai trò:  NPC trang trại và thú hỗ trợ hằng ngày không bao giờ tham chiến; đặc tính của chúng chỉ tác động việc trại. Chỉ NPC chiến đấu và thú canh gác được đặt vào Loại 6 — Triển khai và có đặc tính phòng thủ. Mỗi nhân vật có một  “động từ” riêng , không có hai nhân vật làm cùng một việc chỉ khác con số. |
| --- |



### 11.1. NPC trang trại / chuyên môn (7)



| NPC | Tuyển • Lương | Động từ riêng | Đặc tính chuyên môn (ẩn, 6 khóa) | Giới hạn |
| --- | --- | --- | --- | --- |
| Quản Lý Ca | Chợ đêm (trao đổi) • 120G/ngày | Lập lịch : mở tự động hóa bậc 2–4, chia việc lặp theo giờ cho NPC khác | Ca Kíp Linh Hoạt : thiếu vật tư thì lịch tự đổi việc thay vì dừng | Không bán heo, không chọn giống |
| Nhà Nghiên Cứu Gen | Đấu giá đêm • 220G/ngày | Giải mã : vận hành phòng nghiên cứu (Hiếm/Quý), chạy nghiên cứu Thuần Hóa Huyết Thống | Giải Mã Sâu : mở giám định Huyền Thoại/Dị Biến | Không giám định miễn phí |
| Bác Sĩ Thú Y | Chợ ngày • 160G/ngày | Chẩn đoán : phân biệt Rối Loạn Sinh Sản với xui; kiểm dịch miễn phí 10 con/ngày | Khoanh Vùng Ổ Dịch : báo khu sắp thành Chuồng Nhiễm trước 12 giờ | Không chữa Hư Mạch |
| Kỹ Sư Xây Dựng | Chợ ngày/đêm • 200G/ngày | Thi công & sửa chữa : trực Phòng Xây Dựng — biến bản vẽ thành công trình và sửa mọi công trình, hạ tầng (một NPC đảm nhận cả hai) | Tay Nghề Xây Dựng : hiệu suất phòng lên 100%, vật liệu −10%, sửa hoàn lại 25% phế liệu, mở thêm 1 ô thi công song song | Không sửa trong đợt quái |
| Nhà Buôn | Chợ đêm • 150G/ngày + 2% | Bán hộ : mang tối đa 10 heo đã kiểm dịch đi bán khi người chơi bận | Mối Quen : 1 lần/chu kỳ bán vượt sức mua 20 con không giảm giá | Không bán heo chưa kiểm dịch |
| Thủ Kho | Chợ ngày • 100G/ngày | Nạp trước : nạp đạn/nhiên liệu vào công trình theo danh sách (ngoài đợt) | Xếp Kho Chuẩn : kho không hỏng cám khi Nồm Ẩm; sức chứa kho +15% | Không nạp trong đợt |
| Nông Công | Chợ ngày • 60G/ngày | Chăm sóc : 4 việc cho ăn/vệ sinh mỗi ngày theo lịch | Tay Quen Việc : dọn vệ sinh luôn thay ổ lót (chặn Ghẻ tái nhiễm) | Không chạm heo bệnh/hiếm |


Hiệu suất Phòng Xây Dựng theo người trực: NPC bất kỳ khác nghề 40% • Kỹ Sư Xây Dựng chưa mở đặc tính 70% • Kỹ Sư đã mở đặc tính 100%. Hiệu suất nhân trực tiếp vào tốc độ thi công và sửa chữa (chi tiết mục 13.2). Số NPC tối đa (tính cả lính dài hạn): Cấp trại 1–4: 2 • 5–9: 4 • 10–14: 6 • 15+: 8.


### 11.2. NPC chiến đấu / lính đánh thuê (5)



| Lính | Chỉ số | Thuê / chu kỳ | Động từ riêng trong đợt quái | Đặc tính chuyên môn phòng thủ |
| --- | --- | --- | --- | --- |
| Vệ Binh Khiên | HP 420 • 18/đòn • cận | Đổi 3 heo trưởng thành hoặc 650G (hiếm) | Chặn cửa : quái nhỏ không đi xuyên qua được | Tường Người : hứng 30% sát thương cho công trình phía sau |
| Xạ Thủ Nỏ | HP 180 • 35/phát • 18 m | Đổi 2 Giáp Da + 30 tên | Săn tinh anh : bỏ quái thường, nhắm tinh anh gần nhất | Mắt Ưng : mục tiêu bị đánh dấu nhận +25% sát thương |
| Kỹ Sư Chiến Trường | HP 220 • 12/đòn | Đổi 1 bản vẽ Thường | Đặt lại bẫy : khôi phục 1 lần kích cho 1 bẫy đã cạn | Tay Nhanh : 2 lần/đợt |
| Thợ Săn UFO | HP 200 • 90/phát (trên không) • 25 m | Đổi 8 pin + 1 vật phẩm III | Cắt chùm kéo : trúng UFO đang kéo → heo rơi xuống an toàn | Đạn Xuyên Giáp : 1 phát/đợt gây gấp 4 |
| Thích Khách Đêm | HP 240 • 45/đòn • cận | Đổi 1 heo gen Khá đã định danh | Truy vết : săn quái tàng hình và quái trộm đang rút | Thấy Trong Tối : lộ tàng hình 8 m |


Lính có Stamina riêng 150; hết thì rút về Cờ Tập Kết 8 giây. Thuê theo chu kỳ hoặc dài hạn (giá ×2,5, được mở đặc tính).


### 11.3. Thú cưng (7)



| Thú | Nhóm | Có được / ăn | Động từ riêng | Đặc tính chuyên môn (ẩn) |
| --- | --- | --- | --- | --- |
| Chó Chăn Đàn | Hỗ trợ | Trao đổi / 8G | Gom đàn : tự lùa heo lạc về đàn; Tinh thần hồi ×1,5 trong 15 m; +2 Hòa nhập/ngày cho heo tách đàn | Chia Đàn : chia đàn thành 2 nhóm theo lệnh (hỗ trợ cách ly) |
| Mèo Kho | Hỗ trợ | Trao đổi / 4G | Bắt chuột : chặn hao hụt kho ngoài đợt quái | Canh Ổ : báo cám bắt đầu mốc |
| Heo Săn Nấm | Hỗ trợ | Trao đổi / 10G | Đào tìm : 1–3 vật liệu hiếm/ngày trong khu chỉ định | Mũi Thính : 5%/ngày đào ra Mực Ấn hoặc Tơ Vàng |
| Quạ Tin | Hỗ trợ (thông tin) | Trao đổi / 5G | Đưa tin : mỗi sáng 1 mẩu tin (thời tiết mai hoặc xu hướng chợ) | Tai Mắt : 1 lần/chu kỳ báo trước giờ Bí nhân xuất hiện |
| Chó Ngao Canh | Canh gác | Trao đổi / 12G | Đuổi trộm : HP 300, cắn 20; buộc Kẻ Bắt Heo thả heo | Cắn Giữ : quái bị cắn chậm 30% trong 2 giây |
| Ngỗng Báo Động | Canh gác | Trao đổi / 3G | Náo động : UFO khóa chậm +2 giây trong 10 m | Đàn Ngỗng : 2 con cạnh nhau cộng dồn +3 giây |
| Chó Săn Chiến | Canh gác cao cấp | Đấu giá / 30G | Đánh chặn : HP 600, cắn 45; lao theo quái nhanh | Vật Ngã : 1 lần/đợt quật ngã tinh anh 2 giây |


Ô thú cưng: 2 (Cấp trại 3), 4 (Cấp 8), 6 (Cấp 14).


## 12. Combat: 6 ô trang bị, ô võ kỹ, Phù Văn, Mệnh Ấn & vật phẩm


### 12.1. Sáu ô trang bị & ba cặp mở ô võ kỹ

Nhân vật chỉ mặc tối đa 6 món. Sáu ô ghép thành 3 cặp cố định. Khi cả hai món của một cặp được mặc, cặp đó mở 1 ô võ kỹ để lắp một kỹ năng đã học. Cặp không tạo ra kỹ năng mới; nó mở chỗ và quyết định kỹ năng đó dùng nguồn năng lượng nào.



| Cặp món | Ô võ kỹ mở ra | Nguồn năng lượng | Kỹ năng lắp được |
| --- | --- | --- | --- |
| Vũ khí + Giày | Ô Thế Công | Stamina | Kỹ năng tấn công, lướt, liên hoàn (thẻ “Công”) |
| Giáp thân + Quần giáp | Ô Thế Thủ | Stamina hoặc HP (Huyết Tế: 1 HP thay 2 Stamina) | Kỹ năng đỡ, che chắn, khiêu chiến (thẻ “Thủ”) |
| Vòng tay + Vòng cổ | Ô Thế Biến | Nộ Khí  0–100 | Kỹ năng khống chế diện rộng, Tuyệt kỹ (thẻ “Biến”) |


Nộ Khí: +2 mỗi quái thường hạ, +10 mỗi tinh anh, +1 mỗi lần đỡ đòn thành công; ngoài combat giảm 5/giây sau 10 giây; về 0 khi hết đợt quái. Tuyệt kỹ tốn 60–100 Nộ Khí nên mỗi đợt chỉ dùng 1–3 lần.


### 12.2. Hạng cân & lối chơi

Mỗi món có hạng cân (Nhẹ 1, Vừa 2, Nặng 3 điểm). Tổng điểm 6 món xác định Lối chơi — hiệu ứng tự bật, không cần chọn:



| Tổng điểm | Lối chơi | Hiệu ứng | Hợp phái |
| --- | --- | --- | --- |
| 6–9 | Khinh Thân | Lướt −20% Stamina; tốc chạy +8%; nhận sát thương +10% | A — Tốc Bộ |
| 10–13 | Cân Bằng | Kỹ năng thẻ “Biến” hồi chiêu −10%; không phạt | C — Khống Chế |
| 14–18 | Trọng Giáp | Đòn nặng +15% sát thương và gây choáng ngắn; không bị đẩy lùi bởi quái nhỏ; lướt +25% Stamina; tốc chạy −10% | B — Trọng Kích / D — Sinh Tồn |


Kỹ năng của phái hợp lối chơi được giảm 10% Stamina; kỹ năng của phái trái lối chơi (ví dụ Tốc Bộ khi đang Trọng Giáp) tăng 15% Stamina.


### 12.3. Danh mục trang bị mẫu (mỗi món một cơ chế)

Chỉ số thưởng hiển thị riêng, tối đa +8/chỉ số, không tính vào điều kiện học võ kỹ. Phẩm chất I–IV chỉ tăng độ mạnh cơ chế và chỉ số thưởng (+1 / +3 / +5 / +8).



| Ô | Món | Hạng cân | Chỉ số chính | Cơ chế riêng |
| --- | --- | --- | --- | --- |
| Vũ khí | Dao Rựa | Nhẹ | 14 • 0,5 giây | Cắt : chém đứt dây trói của quái trộm, giải cứu heo trong 1 giây; phát cỏ, dọn bụi −20% Stamina |
| Vũ khí | Giáo Móc | Vừa | 22 • 0,8 giây | Móc kéo : đòn nặng kéo quái nhỏ về 4 m (vào bẫy); kéo xác từ xa |
| Vũ khí | Búa Tạ | Nặng | 48 • 1,5 giây | Chấn : đòn nặng choáng 0,3 giây, vỡ khiên năng lượng nhỏ; đóng cọc rào −20% thời gian |
| Vũ khí | Nỏ Tay | Vừa | 26/phát | Tên tín hiệu : đánh dấu mục tiêu, công trình ưu tiên bắn 5 giây; mang tối đa 40 tên |
| Giày | Giày Đinh | Vừa | Thủ 2 | Miễn Trơn Trượt; lướt trên bùn không chậm |
| Giày | Guốc Gió | Nhẹ | AGI thưởng | Lướt liên tiếp lần 2 trong 1 giây không tốn Stamina |
| Giáp thân | Giáp Da Thuộc | Vừa | Thủ 12 | Giảm 50% nhiễm trùng khi bị quái cắn |
| Giáp thân | Giáp Lưới Thép | Nặng | Thủ 22 | Không bị kéo/đẩy bởi quái nhỏ |
| Giáp thân | Áo Mưa Dầu / Áo Chì | Nhẹ / Nặng | Thủ 4 / 10 | Miễn Ướt Lạnh / miễn Nhiễm Xạ |
| Quần giáp | Quần Da Lót Bông | Nhẹ | Thủ 3 | Miễn Cóng Tay (giữ ấm toàn thân); ngồi nghỉ hồi +20% |
| Quần giáp | Quần Giáp Xích | Nặng | Thủ 10 | Đỡ đòn thành công hoàn 2 Stamina |
| Vòng tay | Vòng Cách Điện | Nhẹ | — | Giảm 80% khả năng bị sét đánh; miễn choáng điện |
| Vòng tay | Vòng Huyết Nha | Vừa | STR thưởng | Nộ Khí nhận +50% từ tinh anh |
| Vòng cổ | Còi Chăn Heo | Nhẹ | — | Heo Hoảng sợ trong 10 m hồi +10 Tinh thần (1 lần/2 phút) |
| Vòng cổ | Mặt Dây Đèn Bão | Nhẹ | — | Bỏ Lạc Hướng; lộ tàng hình trong 5 m |


Sát thương nhận = Gốc × 100 / (100 + Phòng thủ) × (1 − RES gốc × 0,5%).


### 12.4. Phù Văn & Mệnh Ấn (giữa–cuối game)



| Hệ | Phù Văn — khắc lên trang bị | Mệnh Ấn — khắc lên chính nhân vật |
| --- | --- | --- |
| Đối tượng | Món trang bị bậc II trở lên; tối đa 1 Phù Văn/món (6 món → tối đa 6) | Chỉ nhân vật người chơi; NPC, lính, thú cưng không khắc được. Tối đa 3 Mệnh Ấn |
| Điều kiện mở (đồng thời) | Đã qua Cổng Giữa Game + ≥ 12 đợt lớn + Cấp nhân vật ≥ 15 + có Phòng Xây Dựng cấp 2 + sở hữu 1 Bàn Khắc (bản vẽ Cao cấp) | Đã mở Phù Văn ≥ 5 chu kỳ + ≥ 18 đợt lớn + Cấp nhân vật ≥ 22 + đã hạ 2 Boss + đã hoàn thành ≥ 1 sự kiện Đỏ |
| Nguyên liệu | Mực Phù (II–III) + vàng; tỉ lệ khắc 100% / 80% / 60% theo bậc Phù Văn I–III | Huyết Triện + Tinh Hoa Dị Biến; mỗi Mệnh Ấn trừ vĩnh viễn 10 HP tối đa |
| Ví dụ | Phù Truy Phong  (giày): lướt để lại vệt làm chậm •  Phù Nộ Hỏa  (vòng tay): Nộ Khí +1 mỗi đòn trúng •  Phù Kiên Thạch  (giáp): đỡ đòn giảm thêm 10% •  Phù Hộ Đàn  (vòng cổ): Neo tinh thần của thủ lĩnh mạnh thêm +5 khi người chơi đứng gần | Ấn Bất Khuất : 1 lần/đợt HP về 0 thì còn 1 HP và miễn sát thương 2 giây •  Ấn Song Thế : mở ô võ kỹ thứ 4 dùng Nộ Khí •  Ấn Mục Đồng : Tiếng Huýt/Mắt Nhà Nghề hồi chiêu −50% |



### 12.5. Danh mục vật phẩm theo 4 nhóm

Túi nhanh 4 ô; mỗi loại tối đa 3 cái khi vào đợt quái. Nguồn: chợ ngày (vàng, chỉ bậc I), chợ đêm (trao đổi theo yêu cầu người bán, 18% ô nhận vàng), đấu giá, Bí nhân, sự kiện.



| Nhóm | Vật phẩm | Bậc | Cơ chế | Nguồn chính |
| --- | --- | --- | --- | --- |
| Hỗ trợ trại | Xô Nước | I | Dội hạ nhiệt 1 heo đang Say Nắng; dùng lại được | Chợ ngày 20G |
|  | Mái Che Tạm | I | Dựng bóng mát 6 m tại chỗ trong 1 ngày; tính như 1 mái trú cho 4 heo | Chợ ngày 80G |
|  | Cám Linh Chi | II | Bắt buộc cho Bồi Dưỡng Kế Vị; +5 Tinh thần/ngày cho con được ăn | Chợ đêm trao đổi |
|  | Cám Hạ Hỏa | II | Thuần hóa Xích Mao (sau nghiên cứu); Tinh thần mất −20% trong 1 ngày | Phòng nghiên cứu chế tạo |
|  | Thuốc An Thần Heo | II | Ném vào heo: Tinh thần +30, dập cơn Lôi Tâm | Trao đổi |
| Thông tin | Tin Đồn Chợ | I | Lộ chủ đề chu kỳ đang chạy | Chợ đêm |
|  | Lá Dò Sương | II | 24 giờ: nếu Sương mù huyền bí sắp ập đến sẽ cảnh báo trước 10 phút game | Trao đổi |
|  | Bản Đồ Hướng Quái | III | Lộ hướng xuất hiện làn sóng đầu của đợt lớn kế tiếp | Đấu giá, Bí nhân |
|  | Hồ Sơ Giống Loài | III–IV | Định danh trước 1 dòng gen | Bí nhân |
| Chiến đấu | Bình Hồi Lực | I | Hoàn 40 Stamina phần hao phí tức thời; uống 1 giây, trúng đòn bị ngắt | Chợ ngày 90G |
|  | Băng Gạc | I | +30 HP trong 5 giây; dùng được cho lính | Chợ ngày 60G |
|  | Lựu Khói | II | Quái trong 4 m mất mục tiêu 3 giây — để rút lui | Trao đổi |
|  | Bom Keo | II | Vũng keo 3 m, 5 giây — dựng cửa hẹp tạm | Trao đổi |
|  | Pháo Sáng | I | Lộ tàng hình trong 12 m, 10 giây | Chợ ngày 80G |
|  | Tinh Nộ Đan | III | +50 Nộ Khí ngay | Đấu giá |
| Sự kiện | Bùa Hồi Hương | I | Thoát Cổng khi đã hoàn thành ≥ 50%, giữ 50% thưởng | Chợ đêm trao đổi |
|  | La Bàn Tán Vụ | III | Thoát Sương mù sau khi đã ở ≥ 1 ngày, giữ 30% thưởng | Đấu giá, sự kiện Vàng |
|  | Ngọc Phá Sương | IV | Thoát Sương mù bất cứ lúc nào, giữ 50% thưởng | Đấu giá, Bí nhân |
|  | Chìa Cổng Ngược | III | Giữ một Cổng không biến mất thêm 6 giờ | Sự kiện Xanh |



### 12.6. Bốn trường phái võ kỹ & vòng liên kết

Vòng một chiều A → C → B → D → A: học phái A dùng được kỹ năng A và C; C dùng C và B; B dùng B và D; D dùng D và A.

Hình 12.1 — A Tốc Bộ → C Khống Chế → B Trọng Kích → D Sinh Tồn → A.



| Phái | Chỉ số gốc | Lối chơi hợp | Vai trò | Hợp kích |
| --- | --- | --- | --- | --- |
| A — Tốc Bộ | AGI | Khinh Thân | Chạy tới lỗ hổng, cứu heo, rút lui | Ảnh Móc : Bộ Pháp Lướt → Móc Kéo trong 2 giây: kéo xa gấp đôi |
| C — Khống Chế | CTRL | Cân Bằng | Gom quái vào bẫy và vùng hỏa lực | Trói Chùy : Trói Lưới → Chấn Địa: mục tiêu bị trói nhận ×1,3 |
| B — Trọng Kích | STR | Trọng Giáp | Hạ giáp, choáng tinh anh, giữ cổng | Phản Thủ : Phản Chấn → Hộ Thân: khiên ×1,5 |
| D — Sinh Tồn | RES | Trọng Giáp | Giữ tuyến, che heo/công trình | Thủ Chuyển Công : Trụ Bộ → Đoạn Ảnh Trảm: nhát cuối chắc chắn chí mạng |


Luật ngoài phái (chỉ xét chỉ số gốc): phái chính học cả 6 kỹ năng, yêu cầu −15%, Tuyệt kỹ chỉ ở phái chính • phái liên kết học Cấp 1–2 khi chỉ số chủ đạo ≥ 30 / ≥ 34 + điều kiện hành vi, phí +25%, Stamina +10% • hai phái còn lại chỉ 1 kỹ năng Cấp 1/phái khi chỉ số chủ đạo ≥ 38 + Bí Kíp (bậc III), Stamina +20% • đổi phái chính 20.000G, 1 lần/3 chu kỳ. Chi phí học: Cấp 1: 800G + 10 Điểm Võ • Cấp 2: 2.500G + 30 • Cấp 3: 6.000G + 60 • Tuyệt kỹ: 15.000G + 120 + Bí Kíp đấu giá. Điểm Võ: +1/20 quái thường, +5/tinh anh, +25/Boss.


### 12.7. Danh mục 24 kỹ năng



| Phái | Kỹ năng (Cấp) | Ô | Tiêu hao • Hồi | Yêu cầu gốc | Cơ chế |
| --- | --- | --- | --- | --- | --- |
| A | Bộ Pháp Lướt (1) | Công | 8 STA • 3 giây | AGI 24 | Lướt 5 m, miễn sát thương 0,2 giây |
| A | Đoạn Ảnh Trảm (1) | Công | 10 STA • 6 giây | AGI 26 | 3 nhát nhanh; nhát 3 hất quái nhỏ khỏi heo đang bị cắn |
| A | Xuyên Bầy (2) | Công | 12 STA • 12 giây | AGI 32 | Chạy xuyên nhóm quái nhỏ, để vệt chậm 20% |
| A | Cứu Nguy (2) | Thủ | 10 STA • 20 giây | AGI 36 | Lao tới heo đang bị bắt/kéo trong 10 m và giật lại |
| A | Tiếp Viện Tốc Hành (3) | Biến | 30 Nộ • 25 giây | AGI 40 | Dịch chuyển tới vùng Triển khai gần nhất trong 25 m |
| A | Vô Ảnh Bộ  (Tuyệt) | Biến | 70 Nộ • 60 giây | AGI 45 | 2 ảnh ảo hút quái 5 giây; 4 lần né miễn phí |
| B | Phá Giáp (1) | Công | 9 STA • 5 giây | STR 26 | Giáp mục tiêu −25% 6 giây (mọi nguồn hưởng lợi) |
| B | Chấn Địa (1) | Công | 12 STA • 8 giây | STR 28 | Đẩy mọi quái nhỏ trong 3 m ra 2 m |
| B | Trảm Tuyến (2) | Công | 15 STA • 12 giây | STR 34 | Quét đường thẳng 6 m, đẩy lùi cả hàng |
| B | Phản Chấn (2) | Thủ | 12 STA/6 HP • 10 giây | STR 34 + RES 28 | Đỡ đúng lúc 0,3 giây → phản 150% |
| B | Neo Kích (3) | Biến | 40 Nộ • 20 giây | STR 40 | Ghim tinh anh 1,5 giây (Boss 0,5) — tạo cửa sổ cho Lôi Pháo |
| B | Thiên Chùy  (Tuyệt) | Biến | 80 Nộ • 60 giây | STR 45 | 1 đòn 400%, xóa khiên năng lượng |
| C | Móc Kéo (1) | Công | 8 STA • 6 giây | CTRL 26 | Kéo quái nhỏ 6 m vào bẫy/vùng hỏa lực |
| C | Ấn Tín Hiệu (1) | Công | 6 STA • 8 giây | CTRL 28 | Đánh dấu 5 giây: công trình và lính đổi mục tiêu |
| C | Xoay Bầy (2) | Biến | 25 Nộ • 15 giây | CTRL 34 | Đổi hướng bầy quái trong nón 60°, 6 m |
| C | Trói Lưới (2) | Thủ | 12 STA • 14 giây | CTRL 36 | Trói 1 quái 2,5 giây; trói UFO Thu Hoạch làm ngắt chùm kéo |
| C | Dựng Chốt (3) | Biến | 35 Nộ • 30 giây | CTRL 40 | Cắm 3 cọc tạm tạo cửa hẹp 8 giây |
| C | Thiên La  (Tuyệt) | Biến | 75 Nộ • 75 giây | CTRL 45 | Vùng 5 m: quái chậm 50% và không rời vùng 6 giây |
| D | Trụ Bộ (1) | Thủ | 6 STA/3 HP • 6 giây | RES 24 | 3 giây không bị đẩy/ngắt |
| D | Hộ Thân (1) | Thủ | 8 STA/4 HP • 10 giây | RES 26 | Khiên 40 HP 5 giây |
| D | Khiêu Chiến (2) | Thủ | 12 STA • 18 giây | RES 34 | Buộc quái trong 6 m bỏ heo/công trình, đánh người chơi 4 giây |
| D | Hồi Khí (2, bị động) | Thủ | — | RES 32 | Rời combat 5 giây: hồi Stamina +20% |
| D | Chắn Mạng (3) | Biến | 40 Nộ • 40 giây | RES 42 | Nhận thay 1 đòn lớn cho heo/công trình trong 4 m |
| D | Kim Cương Thể  (Tuyệt) | Biến | 70 Nộ • 90 giây | RES 45 | 6 giây giảm 50% sát thương; không tạo Tải ngày |



# PHẦN IV — PHÒNG THỦ, CÔNG TRÌNH & ĐỢT QUÁI


## 13. Kiến trúc phòng thủ, bản vẽ & Phòng Xây Dựng

Phòng thủ = combat của nhân vật + công trình phòng thủ. Công trình là xương sống thủ thành, luôn tự động và mạnh áp đảo mục tiêu của nó.



| COMBAT NGƯỜI CHƠI (trung tâm) — Phòng Xây Dựng cung cấp mọi công trình bên dưới |
| --- |
| 1 · THÔNG TIN   ☁ 1A Trinh thám • 1B Hằng ngày | 2 · HỖ TRỢ   ☁ Hồi sức • Điện • Chiếu sáng | 3 · TẤN CÔNG   ☁ Mặt đất • Phòng không |
| 4 · PHÒNG THỦ Rào • Cổng • Khiên • Ụ chắn | 5 · BẪY Choáng • Chậm • Gom • Phá khiên | 6 · TRIỂN KHAI Vùng cho thú canh gác & lính |


Hình 13.1 — Sáu loại công trình. Dấu ☁: loại có 1 công trình Bạch Vân kích hoạt thủ công.


### 13.1. Sáu loại hình & ba công trình Bạch Vân



| # | Loại | Chức năng | Số mẫu | Công trình Bạch Vân |
| --- | --- | --- | --- | --- |
| 1A | Thông tin – Trinh thám | Soi đợt quái và sự kiện huyền bí dạng phòng thủ | 4 | Bạch Vân · Thiên Nhãn Vọng Đài |
| 1B | Thông tin – Hằng ngày | Thời tiết, chợ, Sinh Cảnh, sự kiện dạng event | 4 | — |
| 2 | Hỗ trợ | Hồi Stamina, chiếu sáng, làm mát, điện, chống sét | 6 | Bạch Vân · Vạn Linh Hồi Nguyên Trận |
| 3 | Tấn công | Mặt đất + phòng không; mạnh áp đảo | 5 | Bạch Vân · Cửu Tiêu Lôi Pháo |
| 4 | Phòng thủ | Mua thời gian, định tuyến | 4 | — |
| 5 | Bẫy | Khống chế, số lần kích hữu hạn | 5 | — |
| 6 | Triển khai | Vùng cho thú canh gác và NPC chiến đấu | 3 | — |


Tự động mặc định: mọi công trình tự nhắm, tự bắn, tự kích, tự bật khiên. Trước đợt người chơi cài chế độ: Tự do • Tiết kiệm (chỉ bắn khi ≥ 3 mục tiêu hoặc mục tiêu đang tấn công heo) • Chỉ tinh anh. Ngoại lệ duy nhất — Bạch Vân: mỗi loại Tấn công, Trinh thám, Hỗ trợ có đúng 1 công trình cấp cao nhất. Vì sức mạnh vượt trội, nhiên liệu cực đắt và chỉ đủ 2–3 lần/đợt, Bạch Vân chỉ kích hoạt thủ công: đứng trong 12 m giữ nút 1,5 giây, hoặc dùng Bảng Điều Khiển ở nhà chính (trễ thêm 3 giây). Mỗi trại tối đa 1 công trình mỗi loại Bạch Vân.


### 13.2. Bản vẽ & Phòng Xây Dựng

Có vàng chưa đủ để xây. Mỗi công trình cần (1) bản vẽ đã học, (2) Phòng Xây Dựng đủ cấp và (3) một NPC trực phòng. Bản vẽ học một lần dùng mãi (Bạch Vân chỉ dựng được 1 công trình); mỗi lần xây vẫn tốn vật liệu và vàng.



| Bậc bản vẽ | Công trình ví dụ | Nguồn | Phòng Xây Dựng |
| --- | --- | --- | --- |
| Thường | Hàng rào, Tháp Canh, Bàn Chông, Cờ Tập Kết | Có sẵn khi xây phòng; chợ đêm | Cấp 1 |
| Tinh | Nỏ Xuyên Vân, Radar, Trạm Hồi Sức, Tấm Sốc, Cổng Gia Cố | Chợ đêm (trao đổi, 18% ô nhận vàng), sự kiện Vàng | Cấp 1 |
| Cao cấp | Tháp Hồ Quang, Skyhook, Máy Chiếu Khiên, Lưới EMP, Máy Cộng Hưởng, Bàn Khắc | Đấu giá đêm, Bí nhân, sự kiện Đỏ (sau Cổng Giữa Game) | Cấp 2 |
| Bạch Vân | 3 công trình Bạch Vân; Đài Thiên Lôi | Đấu giá (rất hiếm), Boss chu kỳ ≥ 10, sự kiện Đỏ cấp cao, Bí nhân | Cấp 3 + Cấp trại ≥ 12 |




| Phòng Xây Dựng | Giá nâng | Ô thi công | Số lượng tối đa công trình tấn công |
| --- | --- | --- | --- |
| Cấp 1 | 1.500G | 1 | Nỏ 2 |
| Cấp 2 | 6.000G + 40 thép | 1 (+1 nếu Kỹ Sư đã mở đặc tính) | Nỏ 3 • Hồ Quang 2 • Skyhook 2 |
| Cấp 3 | 18.000G + 100 thép + 2 Linh Kiện Tinh | 2 (+1) | Nỏ 4 • Hồ Quang 3 • Skyhook 3 • Thiên Lôi 1 • mỗi Bạch Vân 1 |


Hiệu suất theo NPC trực: NPC khác nghề 40% • Kỹ Sư Xây Dựng chưa mở đặc tính 70% • đã mở 100%. Thời gian thực tế = Thời gian gốc / Hiệu suất. Ví dụ Skyhook gốc 8 giờ: NPC khác nghề 20 giờ, Kỹ Sư chưa mở 11,4 giờ, đã mở 8 giờ. Không có NPC trực: phòng ngừng hoạt động.


### 13.3. Chín luật vận hành bắt buộc



| Luật | Nội dung |
| --- | --- |
| 1. Tự động | Mọi công trình tự hoạt động theo chế độ đã cài; chỉ Bạch Vân kích hoạt thủ công |
| 2. Nạp trước | Đạn, tên, dầu, điện, năng lượng nạp trước đợt quái (lớn hoặc nhỏ); trong đợt không nạp thêm |
| 3. Sức chứa có trần | Nỏ Xuyên Vân tối đa 50/50 mũi: mua 5.000 mũi cũng chỉ nạp được 50 |
| 4. Bản vẽ + Phòng Xây Dựng | Không có bản vẽ và phòng đủ cấp thì không xây được dù có vàng |
| 5. Hao mòn | Độ bền 0–100 giảm khi bắn/kích hoạt và khi bị quái tấn công; về 0 = Hỏng |
| 6. Sửa ngoài đợt | Chỉ Phòng Xây Dựng sửa, chỉ khi không có đợt quái |
| 7. Hồi chiêu | Mỗi công trình có nhịp bắn riêng; Bạch Vân hồi chiêu ≥ 75 giây |
| 8. Van ngân sách | Bản vẽ + vật liệu + nạp + sửa cạnh tranh trực tiếp với mở rộng trại, vật phẩm, tình báo |
| 9. Áp đảo, chỉ giới hạn bằng số lượng | Công trình không bị quái kháng hay miễn nhiễm: mọi phát trúng gây đủ sát thương lên đúng loại mục tiêu. Giới hạn duy nhất là số lượng tối đa, lượng nạp trước, hồi chiêu và hao mòn. Quái gây áp lực bằng số đông, nhiều hướng, tàng hình, phá dây điện |


Số lần dùng tối đa / đợt = floor(Lượng nạp trước / Tiêu hao mỗi lần)


### 13.4. Sơ đồ vận hành một công trình



| ①  BẢN VẼ Chợ đêm, đấu giá, Bí nhân, sự kiện | → | ②  THI C Ô NG Phòng Xây Dựng + NPC | → | ③  NẠP TRƯỚC Đến trần; cài chế độ | → | ④   Đ ỢT QUÁI Tự động; Bạch Vân thủ công | → | ⑤  SỬA Ngoài đợt; quay lại  ③ |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |



## 14. Loại 1 — Công trình Thông tin

Luật phân tuyến: nhánh 1A chỉ xử lý đợt quái và sự kiện huyền bí dạng phòng thủ; sự kiện huyền bí dạng event chỉ theo dõi bằng nhánh 1B.


### 14.1. Nhánh 1A — Trinh thám



| Công trình | Bản vẽ • Vật liệu | Nhiên liệu / sức chứa | Cơ chế riêng | Hồi / hao mòn |
| --- | --- | --- | --- | --- |
| Tháp Canh Quan Sát | Thường • 900G | Không | Vọng gác : tầm nhìn 40 m, tự báo động; 1 lính nỏ đứng trên +20% tầm bắn | — / khi bị đánh |
| Radar Uy Hiếp | Tinh • 2.600G | Tín Hiệu 80G/lượt; chứa 3 | Quét hướng : tự quét khi có làn sóng mới, hiện hướng + nhóm quái 45 giây; trước đợt 1 ngày báo giờ đến ±2 giờ | 60 giây / 3 |
| Máy Cộng Hưởng Huyền Bí | Cao cấp • 8.000G | Tinh Thể Cộng Hưởng; 2/lần; chứa 4 | Nghe dị động : báo trước 1 ngày nơi sương mù phòng thủ hình thành; trong đợt chỉ điểm sinh quái huyền bí 90 giây | 120 giây / 6 |
| ☁  Bạch Vân · Thiên Nhãn Vọng Đài | Bạch Vân • 12.500G + 60 thép + 3 Linh Kiện Tinh | Dầu Estropic (III):  3 lít/lần , bình tối đa  9 lít  → 3 lần/đợt | Thiên nhãn  (thủ công): đánh dấu toàn bộ quái trên toàn bản đồ 60 giây, kể cả tàng hình; lộ HP và mục tiêu dự định; công trình và lính nhận dữ liệu (tầm +20%) | 150 giây / 10 |


Nạp đầy Thiên Nhãn: 9 × 280G ≈ 2.520G (Dầu Estropic chỉ bán ở chợ đêm bằng vàng hoặc đấu giá). Dùng lúc ít quái = phí 1/3 bình; vì vậy không bao giờ tự kích.


### 14.2. Nhánh 1B — Thông tin hằng ngày



| Công trình | Bản vẽ • Vật liệu | Vận hành/ngày | Thông tin cung cấp |
| --- | --- | --- | --- |
| Trạm Khí Tượng | Thường • 1.500G | 5 điện | Dự báo thời tiết 2 ngày (đúng 85%); cảnh báo Mưa Bức Xạ, Giông Bão trước 12 giờ |
| Trạm Tiếp Sóng Chợ | Tinh • 3.000G | 6 điện + 1 Mã Dữ Liệu | Sức mua chợ ngày mai (±15%), 1 nhóm hàng có thể tăng giá, chủ đề chu kỳ |
| Bàn Tình Báo | Tinh • 5.000G | 2 lượt hỏi, 150–600G/lượt | Nâng cấp tình báo WHAT / WHEN / WHAT TO HOLD (Chương 23) |
| Đài Dò Dị Tượng | Cao cấp • 7.000G | 1 Tinh Thể Cộng Hưởng | Hiện ÁLSK chính xác; báo sự kiện dạng event đã Sẵn sàng; báo giờ Bí nhân có thể xuất hiện ±2 giờ |



## 15. Loại 2 — Công trình Hỗ trợ & Lưới điện



| Công trình | Bản vẽ • Vật liệu | Nạp trước | Cơ chế riêng | Chế độ |
| --- | --- | --- | --- | --- |
| Trạm Hồi Sức | Tinh • 1.800 / 4.000 / 8.000G | Hộp Y Tế; chứa 3; mỗi hộp 60 giây | Hào quang : tự bật khi có người/lính trong 8 m; hồi Stamina +50/75/100% kể cả đang combat | Tự động |
| Đèn Pha Công Nghiệp | Tinh • 2.200G | Điện 3–6u | Soi đường : tự xoay theo chuyển động; nón 25 m; bỏ 70% phạt tầm nhìn; lộ tàng hình trong nón | Tự động |
| Máy Phát Điện | Thường/Tinh/Cao cấp • 3.000 / 9.000 / 22.000G | Diesel; bình 20/40/70 lít; 1/2/3 lít/phút đợt | Cấp điện : 30/70/140u liên tục; đỉnh 60/130/240u | Tự động |
| Bơm Làm Mát | Tinh • 3.500G | Chất làm mát; bình 60 lít | Hạ nhiệt : giảm quá nhiệt công trình 40% trong 15 m; phun sương cứu heo Say Nắng | Tự động |
| Cột Thu Lôi | Thường • 1.200G | Không | Hứng sét : 20 m không bị sét; mỗi lần hứng nạp +20u vào Tụ Điện | Bị động |
| ☁  Bạch Vân · Vạn Linh Hồi Nguyên Trận | Bạch Vân • 16.000G + 3 Linh Kiện Tinh | Tinh Thạch Bạch Vân (IV): 2 viên/lần; chứa 4 → 2 lần/đợt | Hồi nguyên  (thủ công): 20 giây trong 30 m — người và lính hồi Stamina như ngoài combat, Nộ Khí +20, mọi công trình không mất độ bền và hồi chiêu nhanh gấp đôi, heo Tinh thần +40. Kéo đỉnh điện 50u | Hồi 180 giây • −20 độ bền |


Lưới điện: Hồ Quang 2u chờ / 8u mỗi phát • Skyhook 3u / 14u mỗi loạt • Cửu Tiêu Lôi Pháo 4u / 40u trong 20 giây truyền năng • Thiên Nhãn 5u / 18u • Máy Chiếu Khiên 4u / 15u khi chịu đòn. Tổng tải > 110% công suất liên tục 10 giây → công trình trên lưới mất 1 độ bền/giây; > 140% → cầu dao nhảy 3–5 giây. Tụ Điện (bản vẽ Tinh, 2.000G) trữ 60u gánh xung đỉnh.


## 16. Loại 3 — Công trình Tấn công

Chỉ có 5 công trình tấn công: 4 tự động + 1 Bạch Vân. Chúng áp đảo mục tiêu của mình, không quái nào kháng được. Cái giá nằm ở số lượng tối đa (theo cấp Phòng Xây Dựng), sức chứa đạn, hồi chiêu và hao mòn — người chơi luôn thèm có thêm một cái nữa mà không được. Nỏ Xuyên Vân có 3 nhánh nâng cấp để không giống trụ cung thông thường.



| Công trình | Mục tiêu | Sát thương • tầm | Nhịp bắn / hồi | Cơ chế riêng | Giới hạn thật |
| --- | --- | --- | --- | --- | --- |
| Nỏ Xuyên Vân | Mặt đất | 40/mũi, xuyên 2 • 22 m | 1,2 giây; mỗi 10 mũi lên dây 4 giây | Ghim & thu hồi : mũi trúng quái đang chạy ghim nó 0,5 giây; sau đợt nhặt lại 30% tên | 50 mũi/đợt; tối đa 2–4 cái |
| Tháp Hồ Quang | Mặt đất, bầy đông | 70, nảy 4 mục tiêu • 18 m | 2,2 giây; quá nhiệt nếu bắn liên tục 20 giây → nghỉ 6 giây | Chuỗi dẫn : tia nảy giữa quái cách nhau ≤ 4 m; quái dính keo cho nảy thêm 2 bước | 24 pin; tối đa 2–3 cái |
| Tháp Phòng Không Skyhook | Chỉ trên không | 180/loạt, nổ 4 m • 35 m | 3,8 giây/loạt | Màn mảnh : vùng mảnh trên không 4 giây buộc UFO vòng đường | 10 loạt; không bắn mặt đất |
| Đài Thiên Lôi | Đất + không | 8 tia × 450 • 30 m | Tự phóng khi đủ điện tích và ≥ 6 mục tiêu; hồi ≥ 90 giây | Tích điện theo máu : mỗi quái chết trong 30 m +1; đủ 40 thì phóng vào 8 mục tiêu HP cao nhất | 1 cái; 1 + tối đa 2 lượt/đợt |
| ☁  Bạch Vân · Cửu Tiêu Lôi Pháo | Boss / tinh anh | 6.000 đơn + 800 vùng 5 m • 60 m | Truyền năng 20 giây rồi bắn; hồi 75 giây | Truyền năng có định vị  (thủ công): chọn vùng trước, 20 giây sau khai hỏa — phải đoán trước vị trí Boss | 2 phát/đợt; kích sớm trúng quái con, muộn thì Boss đã qua |




| Công trình | Vật liệu xây | Đạn / nhiên liệu | Sức chứa | Nạp đầy | Hao mòn |
| --- | --- | --- | --- | --- | --- |
| Nỏ Xuyên Vân | 2.400G + 30 gỗ + 10 thép | Tên Thép 6G | 50 | 300G | 0,4/mũi |
| Tháp Hồ Quang | 4.800G + 25 thép | Pin Dẫn Điện 22G | 24 | 528G | 1,2/phát |
| Skyhook | 7.500G + 40 thép | Đạn Phòng Không 35G | 30 (3/loạt) | 1.050G | 2,0/loạt |
| Đài Thiên Lôi | 26.000G + 2 Linh Kiện Tinh | Lõi Sét 900G | 1 lõi | 900G | 15/lượt |
| ☁  Cửu Tiêu Lôi Pháo | 18.000G + 80 thép + 4 Linh Kiện Tinh | Đơn Vị Cộng Hưởng 18G | 120 (50/phát) | 2.160G | 22/phát |


Đạn, pin, lõi, nhiên liệu Bạch Vân chỉ có ở chợ đêm (vàng, đấu giá hoặc trao đổi) và từ chế tạo ở Phòng Xây Dựng.


### 16.1. Nhánh nâng cấp (từ Phòng Xây Dựng cấp 2; chọn 1 nhánh/công trình)



| Công trình | Nhánh 1 | Nhánh 2 | Nhánh 3 |
| --- | --- | --- | --- |
| Nỏ Xuyên Vân | Xích Liên : mũi xích nối 3 mục tiêu, cứ 5 mũi trói 1 giây; chứa 40 | Hỏa Tiễn : Tên Dầu; vùng cháy 2 m 4 giây chặn đường | Nỏ Trời : bắn cả mục tiêu bay thấp; chứa 36; mất xuyên |
| Tháp Hồ Quang | Lưới Điện : nảy 6 mục tiêu | Tê Liệt : 20% choáng 0,8 giây | Cao Áp : 1 tia 150 đơn mục tiêu |
| Skyhook | Mảnh Vụn : màn mảnh 6 m, 6 giây | Móc Trời : kéo UFO xuống thấp 3 giây | Theo Dấu : bỏ phạt mục tiêu nhỏ bay thấp |
| Đài Thiên Lôi | Lôi Võng : 12 tia × 320 | Thu Hồn : chỉ cần 30 điện tích | Cuồng Lôi : 8 tia × 600, hao mòn 25 |
| Cửu Tiêu Lôi Pháo | Tụ Nhanh : truyền năng 16 giây; 5.200 | Xuyên Tâm : xuyên tuyến 60 m, 3.500/mục tiêu | Dung Lượng : chứa 150 (3 phát) |



### 16.2. Quy trình kích hoạt Cửu Tiêu Lôi Pháo

Trước đợt: nạp tối đa 120 Đơn Vị Cộng Hưởng.

Trong đợt: đứng trong 12 m (hoặc Bảng Điều Khiển), chọn vùng mục tiêu, giữ nút 1,5 giây → truyền năng 20 giây, vòng đếm ngược hiện trên bản đồ; lưới điện +40u.

Cửa sổ lý tưởng: Boss đi vào vùng ở giây 15–20. Hỗ trợ: Radar/Thiên Nhãn đoán đường, Neo Kích ghim Boss, Dựng Chốt dồn làn.

Sau khi bắn: hồi 75 giây; độ bền −22; heo trong 25 m Tinh thần −12.


## 17. Loại 4 Phòng thủ • Loại 5 Bẫy • Loại 6 Triển khai

Tất cả đều tự động.



| Loại | Công trình | Bản vẽ • Vật liệu | HP / nạp | Cơ chế riêng |
| --- | --- | --- | --- | --- |
| 4 | Hàng Rào  gỗ / thép | Thường • 40G / 180G mỗi đoạn | 300 / 1.200 HP | Ranh giới : rào thép chống leo; heo Hoảng loạn húc được rào gỗ |
| 4 | Cổng Gia Cố | Tinh • 2.500G | 4.000 HP | Định tuyến : tự đóng khi quái trong 15 m, tự mở cho người/lính; dồn quái vào cửa hẹp |
| 4 | Máy Chiếu Khiên | Cao cấp • 8.800G | Gói Năng Lượng 260G, chứa 4 | Vòm khiên : mỗi gói 1.500 điểm khiên vùng 10 m; tự quá tải (×2 trong 12 giây) khi khiên < 30% và ≥ 5 quái bên ngoài |
| 4 | Ụ Bao Cát | Thường • 300G | 800 HP | Che chắn : đứng sau giảm 40% sát thương tầm xa |
| 5 | Bàn Chông | Thường • 600G | 6 lần | Gây thương : 60 + chảy máu (chậm 20% 2 giây) |
| 5 | Tấm Sốc Điện | Tinh • 1.450G | Gói Sạc 90G; 4 lần | Ngắt đòn : choáng 1,2 giây, hủy đòn đang vung (kể cả phá cổng) |
| 5 | Hố Keo | Tinh • 1.800G | Keo 120G; 3 lần | Gom cụm : chậm 60% 5 giây, hút quái trong 3 m vào giữa |
| 5 | Lưới EMP | Cao cấp • 3.200G | Lõi EMP 400G; 2 lần | Phá khiên/tàng hình  6 giây trong 6 m; UFO mất khóa mục tiêu |
| 5 | Mìn Nổ | Tinh • 2.000G | Thuốc Nổ 250G; 1 lần | Nổ vùng : 500 trong 4 m khi ≥ 4 quái; heo trong 15 m Tinh thần −12 |
| 6 | Chốt Canh Gác | Thường • 1.000G | — | Phân vùng : gán 1–3 thú canh gác/lính; tuần tra 12 m; 1 vùng chính + 1 vùng dự phòng |
| 6 | Cờ Tập Kết | Thường • 600G | — | Rút lui : đơn vị hết Stamina tự rút về, hồi +30% |
| 6 | Đèn Hiệu Đánh Chặn | Tinh • 2.400G | Pin Tín Hiệu; chứa 3 | Luật ưu tiên : cài trước “ưu tiên trên không / mặt đất / tinh anh / kẻ trộm” cho lính trong 20 m; đổi luật trong đợt tốn 1 pin |


Không thể gán vào Loại 6: 7 NPC trang trại và 4 thú hỗ trợ hằng ngày.


## 18. Độ bền, hư hại & sửa chữa



| Nguồn mất độ bền | Quy định |
| --- | --- |
| Hoạt động | Mỗi phát bắn/lần kích theo bảng từng công trình |
| Bị quái tấn công | Mỗi 100 sát thương nhận vào HP công trình = −1 độ bền; đòn plasma/phá công trình của tinh anh và Boss = −3 độ bền mỗi đòn. HP về 0 = Hỏng ngay, bất kể độ bền còn |
| Môi trường | Sét đánh −10; quá tải lưới −1/giây; Mưa làm Hồ Quang chập 10%/phát (−5) |




| Độ bền còn | Trạng thái | Hiệu ứng |
| --- | --- | --- |
| 61–100 | Tốt | Không phạt |
| 31–60 | Mòn | Hồi chiêu +5%; 3% bắn kẹt |
| 1–30 | Nguy kịch | Hồi chiêu +15%; sát thương −10%; Bạch Vân 5% kích thất bại (vẫn mất nhiên liệu) |
| 0 | Hỏng | Ngừng đến khi sửa; đạn trong kho công trình được giữ |


Sửa (chỉ ngoài đợt, tại Phòng Xây Dựng): mỗi 10 độ bền — nhẹ 10–25G + 2 phế liệu, vừa 40–60G + 3, nặng 55–90G + 4, Bạch Vân 160G + 7 + 1 Linh Kiện Tinh. Thời gian gốc 15 / 30 / 45 / 60 phút game chia cho hiệu suất phòng. Công trình Hỏng thêm phí khởi động lại 10% giá trị. Báo cáo sau đợt: đạn đã dùng, số lần kích Bạch Vân, độ bền còn và nguyên nhân mất (bắn hay bị đánh), heo mất, Tinh thần đàn thấp nhất, chi phí sửa và nạp lại dự kiến.


## 19. Đợt quái, danh mục quái & Boss



| Thông số | Quy định |
| --- | --- |
| Khoảng cách đợt lớn | 6–8 / 7–10 / 8–12 ngày theo giai đoạn; chu kỳ dài hiếm 12–15 (8%) |
| Đợt nhỏ | 0–3 lần/chu kỳ; 15–30% Điểm Đe Dọa |
| Cấu trúc đợt lớn | 3–5 làn sóng, cách 25–40 giây; 4–9 phút thực; Boss mỗi 5 chu kỳ |
| Điểm Đe Dọa | 100 × (1 + 0,18 × (Chu kỳ − 1)) × (1 + 0,004 × Số heo) × Trạng thái trại (1,0–1,4) |
| Sự kiện huyền bí phòng thủ | Chu kỳ 1–3: 0% • 4–10: 2% • 11+: 12% nền × (1 + ÁLSK/50) × hệ số chu kỳ; 2–45% |
| Thứ tự mục tiêu | Đàn heo → Kho → Hạ tầng/công trình → Nhà chính → Người chơi. Bất tỉnh: Thể trạng −20, mất 10% vàng mang theo, tỉnh ở nhà chính |




| T−2 → T−1 ngày Cảnh báo; tình báo; Radar quét xa; Mộng Nhãn báo hướng | → | Chuẩn bị Sửa, nạp, cài chế độ, gán lính/thú, lùa heo vào vùng an toàn | → | Giao tranh Công trình tự động; người chơi vá lỗ hổng, cứu heo, kích Bạch Vân | → | Hậu đợt Tổng kết, xử lý xác, chợ phản ứng, gieo chu kỳ mới |
| --- | --- | --- | --- | --- | --- | --- |


Hình 19.1 — Luồng một đợt quái lớn.


### 19.1. Mười bốn loại quái — gây áp lực bằng hành vi, không bằng kháng



| Quái | Loại | HP • tốc | Hành vi riêng | Cách đối phó tốt |
| --- | --- | --- | --- | --- |
| Chuột Gặm Kho | Đất – bầy | 40 • nhanh | 15–30 con thẳng vào kho, mỗi con ăn 2 kg cám/giây | Hồ Quang, Chấn Địa |
| Kẻ Cắt Dây | Đất – tàng hình | 90 • vừa | Cắt dây điện → nhánh công trình đó mất điện đến khi hạ nó | Đèn Pha, Lưới EMP, Thích Khách |
| Kẻ Bắt Heo | Đất – trộm | 150 • nhanh khi rút | Trói heo rìa trại (ưu tiên heo Độ bám đàn thấp) rồi kéo ra ngoài | Chó Ngao, Dao Rựa, Cứu Nguy |
| Kẻ Cào Phá | Đất | 120 • vừa | Cào rào tạo lỗ hổng tồn tại đến khi sửa | Nỏ, Bàn Chông |
| Bầy Nhảy | Đất – bầy | 30 • rất nhanh | Nhảy qua rào gỗ, lao vào đàn; mỗi lần cắn Tinh thần −10 | Rào thép, Chó Săn, Hố Keo |
| Thiết Giáp Công Thành | Đất – nặng | 1.100 • chậm | Bỏ mọi mục tiêu, chỉ đập cổng (−3 độ bền mỗi đòn) | Phá Giáp, Tấm Sốc |
| UFO Trinh Sát | Trên không | 160 • nhanh | Đánh dấu 1 nhóm heo; sống quá 30 giây thì UFO Thu Hoạch đến đúng nhóm đó | Skyhook, Nỏ Trời |
| UFO Thu Hoạch | Trên không | 650 • vừa | Chùm kéo 4 giây khóa để hút 1–3 heo | Thợ Săn UFO, Ngỗng, Trói Lưới |
| UFO Pháo Hạm | Trên không – nặng | 1.900 • chậm | Bắn plasma vào máy phát điện trước tiên | Skyhook, Máy Chiếu Khiên |
| Bóng Huyền | Huyền bí | 500 • biến đổi | Đi xuyên rào; chạm thủ lĩnh làm Neo tinh thần tắt 60 giây | Thiên Nhãn, Khiêu Chiến |
| Chó Sương | Huyền bí | 420 • nhanh | Săn lính/thú trong vùng Triển khai; nhanh hơn trong Sương Mù | Máy Cộng Hưởng, Chó Săn |
| Thú Bão | Môi trường | 1.600 • vừa | Chỉ khi Giông Bão; mỗi 10 giây gọi sét vào công trình gần nhất | Cột Thu Lôi |
| Đầu Lĩnh Trộm | Tinh anh | 1.400 • vừa | Quái trộm quanh 10 m nhanh +20%; nhắm heo giá trị cao (hiện rõ trên bản đồ) | Xạ Thủ, Ấn Tín Hiệu |
| Kẻ Mang Dịch | Tinh anh | 900 • chậm | Chết để lại vùng ô nhiễm → Chuồng Nhiễm nếu không rải vôi trong 12 giờ | Hạ ngoài rào bằng Nỏ |



### 19.2. Ba Boss chu kỳ



| Boss | Xuất hiện | HP | Cơ chế & cách đối phó |
| --- | --- | --- | --- |
| Chúa Trộm Rìu Sắt | Chu kỳ 5, 20, 35… | 12.000 (+15%/lần) | Phá cổng rất nhanh; gọi Kẻ Bắt Heo mỗi 30 giây. Dồn vào cửa hẹp rồi dùng Cửu Tiêu Lôi Pháo |
| Mẫu Hạm UFO | Chu kỳ 10, 25, 40… | 20.000 (+15%/lần) | Thả UFO Thu Hoạch; chùm kéo lớn 6 giây hút cả nhóm heo. Skyhook, Thiên Lôi, Lưới EMP; Móc Trời kéo xuống cho Lôi Pháo |
| Thực Thể Sương | Chu kỳ 15, 30, 45… | 28.000 (+15%/lần) | Phủ sương toàn trại; 3 pha, dịch chuyển giữa pha. Thiên Nhãn báo điểm hiện hình, Vạn Linh Hồi Nguyên Trận giữ Tinh thần đàn, Lôi Pháo bắn đúng điểm |



# PHẦN V — KINH TẾ, THÔNG TIN & SỰ KIỆN HUYỀN BÍ


## 20. Thị trường Ngày / Đêm

Chợ ngày là nơi người chơi bán nhiều, giao dịch bằng vàng. Chợ đêm là nơi người chơi mua nhiều; công trình, bản vẽ và nguyên liệu công trình bán bằng vàng, đấu giá hoặc trao đổi, còn phần lớn vật phẩm khác là trao đổi đồng giá theo yêu cầu của người bán.



| CHỢ NGÀY (06:00–18:00) — VÀNG | CHỢ ĐÊM (18:00–06:00) — TRAO ĐỔI là chính |
| --- | --- |
| Bán/mua heo thịt, heo giống; hợp đồng giao heo | Vàng / đấu giá / trao đổi:  bản vẽ, đạn, tên, dầu Estropic, lõi, gói năng lượng, vật liệu công trình |
| Kiểm dịch, giám định Hiếm–Quý, thuốc thú y | Trao đổi theo yêu cầu người bán:  vật phẩm, trang bị, bí kíp, thú cưng, thuê NPC/lính, giấy Lam–Tím |
| Thức ăn, vật liệu trại, dụng cụ, Diesel, vật phẩm bậc I | Vàng (hiếm):  ~18% ô hàng nhận vàng, gần như chỉ bậc I–II; bậc III–IV ≤ 2% số ô |
| Tuyển NPC cơ bản | Đấu giá (bậc III–IV, giấy Đỏ, bản vẽ Bạch Vân); Bí nhân; giám định cấp cao theo sự kiện |
| Giá 80–125% gốc; sức mua lớn | Số lượng ít, biến động mạnh; bán đồ lấy vàng chỉ được 40% GTTC |



### 20.1. Luật trao đổi đồng giá



| Thành phần | Quy định |
| --- | --- |
| Yêu cầu của người bán | Mỗi ô hàng ghi 1–2 “món muốn nhận” cụ thể (ví dụ: 2 heo trưởng thành hạng A đã kiểm dịch; 20 thép + 1 Bình Hồi Lực; 1 heo gen Khá đã định danh). Phải có đúng món đó mới đổi được |
| Thay thế ngang giá | Chỉ thay bằng món cùng nhóm (heo ↔ heo, vật liệu ↔ vật liệu, vật phẩm cùng bậc) có tổng GTTC ≥ 110% món yêu cầu |
| GTTC | Mọi món có giá trị tham chiếu; heo tính theo giá chợ ngày × 0,9 |
| Ô nhận vàng | 18% ô hàng; giá vàng = GTTC × 1,4 |
| Phí | Mỗi giao dịch trao đổi trả 5% GTTC bằng vàng |


Cân bằng vàng: vàng vẫn cần cho chợ ngày, xây dựng, sửa chữa, lương, phí, đấu giá, giám định — không bao giờ thành “túi rỗng”; nhưng không thể chỉ cày vàng rồi mua mọi thứ ở chợ đêm.


### 20.2. Ba lớp thay đổi & tám chủ đề chu kỳ

Làm mới hằng ngày (06:00, 18:00): mặt hàng, số lượng, giá, sức mua, lô đấu giá, “món muốn nhận”. Chủ đề chu kỳ: không đổi hình thức, đổi xác suất nhóm hàng, nhu cầu, bản chất hàng — ảnh hưởng mạnh nhất lên chợ ngày. Phản ứng thế giới: sau đột kích heo khan hiếm (+10–35%, 1–3 ngày); dịch vùng làm giá giảm và cấm bán heo sống.



| Chủ đề | Tỉ lệ | Giá heo | Sức mua | Chợ đêm | Áp lực |
| --- | --- | --- | --- | --- | --- |
| Cân Bằng | 24% | 95–105% | 90–110% | Trung tính | Chuẩn |
| Mùa Heo Rẻ | 12% | 65–85% | 50–70% | Nhiều ô đòi heo; heo định giá ×1,0 | Dễ kẹt heo → già |
| Cơn Sốt Thịt | 12% | 115–145% | 120–160% | Đấu giá sôi động | Cám dỗ bán sạch |
| Khan Hiếm Thức Ăn | 10% | 95–110% | 90–100% | Ô đòi cám | Giá cám +35–70% |
| Hoảng Loạn Dịch | 8% | 60–90% | 40–70% | Thuốc, vắc-xin nhiều ô | Nguy cơ Dịch Tả Heo |
| Khan Hiếm Hậu Đột Kích | 10% | 110–135% | 100–140% | Vật liệu sửa, đạn nhiều hơn | Giữ được đàn thì lãi |
| Mùa Trang Bị | 12% | 95–105% | 90–110% | Trang bị, bí kíp, Mực Phù ×2 | Cơ hội combat |
| Mùa Công Trình | 12% | 95–105% | 90–110% | Bản vẽ, đạn ×2; giá vàng −15% | Cơ hội phòng thủ |



### 20.3. Sức mua & giá nền chợ ngày

Sức mua (heo trưởng thành/ngày): đầu 20–60 • giữa 50–120 • cuối 100–220. Sau 50% sức mua, mỗi con thêm −0,5% giá (tối đa −25%). Giá nền: Cám Thường / Tăng Trọng / Cao Cấp 0,8 / 1,4 / 2,2 G/kg • heo con Thường ≈ 110G • gỗ / thép / phế liệu 5 / 18 / 8 G • Diesel 12G/lít • thuốc thường / đặc trị / vắc-xin 40 / 150 / 50 G • kiểm dịch 60G, sàng lọc 120G • vôi 20G/ô.


## 21. Bí nhân

Bí nhân là một thương nhân nửa người nửa quỷ. Hắn mang đến những thứ không nơi nào có — thông tin toàn phần, bản vẽ hiếm, vật phẩm bậc IV — nhưng không bao giờ nhận vàng và luôn đòi nhiều hơn giá trị món hắn đưa.



| Thông số | Quy định |
| --- | --- |
| Thời điểm | Một  khung giờ ngẫu nhiên trong đêm  (18:00–04:00); ở lại đúng  2 giờ game  rồi biến mất |
| Vị trí | Xuất hiện  ngay sát ranh giới bản đồ nông trại  (ngoài hàng rào, trong 30 m), có ngọn đèn lồng xanh làm dấu |
| Tỉ lệ xuất hiện | 3%/đêm × hệ số chu kỳ (0,5–3,0) × (1 + ÁLSK/100); tối đa 25%/đêm. Quạ Tin và Đài Dò Dị Tượng báo trước giờ xuất hiện |
| Cách trả | 0% vàng.  Chỉ trao đổi, với tỉ giá 130–200% GTTC (người chơi luôn thiệt). Hắn đòi thứ hiếm: heo gen “?” hoặc Quý+, heo già (3–8 con), xác heo, Tinh Hoa Dị Biến, Huyết Triện, vật phẩm bậc III–IV, đôi khi “1 ngày thời gian” (tua mất 1 ngày) |
| Bể hàng (2–4 ô) | Tình báo cấp 5 về 1 sự kiện • Hồ Sơ Giống Loài • Bản vẽ Cao cấp/Bạch Vân • Quả Mộng, Hương Mê Thảo, Ngọc Phá Sương • Giấy Tím/Đỏ • Thuốc chữa Hư Mạch • Bản Đồ Hướng Quái |
| Cái giá ẩn | Mỗi giao dịch ÁLSK +2 đến +10 tức thời |
| Hệ số chu kỳ | “Hoảng Loạn Dịch” ×2,0 (thiên đòi xác); “Mùa Heo Rẻ” ×1,5 (thiên đòi heo già); “Cân Bằng” ×1,0 |



## 22. Quản lý ngân sách

Người chơi quản lý hai ngân sách song song: vàng (chợ ngày, xây, sửa, lương, phí, đấu giá) và kho giá trị (heo, vật liệu, vật phẩm để trao đổi ở chợ đêm và với Bí nhân).



| NGUỒN THU Bán heo, hợp đồng, thưởng đợt quái | → | VÀNG Xây, sửa, lương, phí, đấu giá | ⇄ | KHO GIÁ TRỊ Heo, vật liệu, vật phẩm | → | CHI CHIẾN LƯỢC Trại • Phòng thủ • Tình báo • Gen |
| --- | --- | --- | --- | --- | --- | --- |


Hình 22.1 — Dòng ngân sách kép.



| Chu kỳ giữa game mẫu (8 ngày) | Thu / Chi | Ghi chú |
| --- | --- | --- |
| Thu gộp từ trại | +24.000G | ~3.000G/ngày |
| Thức ăn + phí duy trì hạ tầng | −5.600G | 700G/ngày |
| Lương NPC | −2.900G | ≈ 360G/ngày |
| Nạp trước phòng thủ | −3.200G | Nỏ + Hồ Quang + Skyhook + bẫy/khiên |
| Tình báo + kiểm dịch + giám định | −3.600G |  |
| Quỹ sửa chữa + phí trao đổi | −2.300G |  |
| Còn lại | +6.400G | Tranh chấp: nâng Phòng Xây Dựng, đấu giá bản vẽ, mở rộng trại |


FOMO có kiểm soát: bộ ba Bạch Vân (vật liệu 46.500G + 3 bản vẽ cực hiếm + Phòng Xây Dựng cấp 3) và chi phí nạp mỗi đợt (≈ 2.160 + 2.520 + 7.200G) tương đương nhiều chu kỳ tiền dư. Chúng mạnh đến mức một phát Lôi Pháo hạ 20–50% máu Boss — nhưng để có và vận hành chúng, người chơi phải hoãn mở rộng, cắt tình báo hoặc đem heo quý đi đổi.


## 23. Thông tin & Tình báo

Thông tin không tăng tỉ lệ thành công; nó giúp có mặt đúng chỗ, giữ đúng tài nguyên, chuẩn bị đúng lúc. Mỗi mẩu tin trả lời WHAT (cái gì), WHEN (khi nào), WHAT TO HOLD (cần giữ gì).



| TRẠNG THÁI TRẠI | → | ĐIỀU KIỆN | → | SẴN SÀNG | → | CỬA SỔ | → | SỰ KIỆN |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |


Hình 23.1 — Chuỗi hình thành sự kiện. Tình báo cấp 1–2 soi trạng thái trại, cấp 3 soi điều kiện, cấp 4 soi cửa sổ, cấp 5 soi toàn bộ.



| Cấp | Ví dụ nội dung | Độ chính xác | Nguồn / giá |
| --- | --- | --- | --- |
| 0 — Mù | “Một tín hiệu bất ổn đang hình thành.” | Không xác định | Miễn phí |
| 1 — Loại | “Nhiều khả năng sự kiện màu Vàng.” / “Chu kỳ này dài 7–10 ngày.” | 70–85% | Bàn Tình Báo 150G |
| 2 — Nguồn | “Heo già và sương mù đang góp phần.” | 1–2 yếu tố thật | 300G |
| 3 — Điều kiện | “Cần 24–32 heo già + 1 điều kiện chưa biết.” / “Đợt lớn sau 8±2 ngày.” | Dải ngưỡng | 450G |
| 4 — Cửa sổ | “Cửa sổ mở trong 2–4 ngày, thiên về buổi sáng.” | ±1 ngày | 600G; Mộng Nhãn (miễn phí) |
| 5 — Toàn phần | “29+ heo già + Sương Mù ≥ 2 ngày + không xác + giữ Quả Mộng; cửa sổ 06–10 ngày mai hoặc ngày kia.” | Đầy đủ | Chỉ Bí nhân / sự kiện |


Mua cấp N cần có cấp N−1 (trừ gói của Bí nhân). Tình báo hết hạn khi sang chu kỳ/ván mới; ngưỡng và cửa sổ được gieo lại. Giao diện tách Cảnh báo — Tin đã xác nhận — Suy đoán.


## 24. Sự kiện huyền bí



| Hình thức | Cách xuất hiện | Luật thoát |
| --- | --- | --- |
| Cổng | Vị trí ngẫu nhiên trong bản đồ nông trại (không bao giờ ngoài bản đồ); tồn tại 6 giờ; người chơi chủ động bước vào | Hoàn thành 100% → cổng trở về, đủ thưởng. Đã hoàn thành ≥ 50% → dùng  Bùa Hồi Hương  (bậc I) thoát ngay, giữ 50% thưởng |
| Sương mù | Tự ập đến bao quanh người chơi khi đang đi hoặc đứng yên, dịch chuyển sang bản đồ khác | Phải hoàn thành điều kiện; không xong thì kẹt 3 ngày rồi bị đẩy ra. Thoát sớm chỉ bằng vật phẩm bậc cao:  La Bàn Tán Vụ  (III, sau ≥ 1 ngày, giữ 30%) hoặc  Ngọc Phá Sương  (IV, bất cứ lúc nào, giữ 50%) |
| Sương mù phòng thủ | Khi là quái tấn công: sương hình thành ở xa trại như một đợt quái | Cho 3–6 giờ chuẩn bị; phát hiện bằng nhánh 1A |


Ba màu: Đỏ 45% — nguy hiểm, combat/phòng thủ (thưởng Huyết Triện, Đá Cường Hóa, bản vẽ) • Vàng 40% — trao đổi, đánh đổi, thực thể • Xanh 15% — khám phá cực hiếm (Quả Mộng, Tinh Hoa Dị Biến). Xác suất: sự kiện bất chợt = clamp(0,25% + 0,08% × ÁLSK; 0,25%; 8%)/ngày.


### 24.1. Mười bốn mẫu sự kiện



| Màu / hình thức | Tên | Điều kiện | Lối chơi & thưởng |
| --- | --- | --- | --- |
| Xanh / Cổng | Vườn Mười Hai Quả | Heo già + quả + thời tiết | 3 thử thách; chọn 1 trong 3 quả (có Quả Mộng) |
| Xanh / Sương | Đồng Cỏ Không Bóng | Sương Mù + SC cao | Tìm lối thoát trong 3 ngày; Tinh Hoa Dị Biến |
| Xanh / Cổng | Kho Lạnh Bỏ Quên | Sau đột kích | Giải đố hậu cần; Lõi Sét, bản vẽ Tinh |
| Xanh / Sương | Hạt Giống Trắng | Cực hiếm; tình báo cao | +10% giám định vĩnh viễn |
| Vàng / Cổng | Bí Nhân Mua Già | Nhiều heo già | Đổi 2–8 heo già lấy vật phẩm II–III; ÁLSK +5 |
| Vàng / Sương | Chợ Không Đồng Hồ | Điều kiện chợ đêm | Trả bằng vật phẩm/heo/thời gian |
| Vàng / Cổng | Giám Định Vô Danh | Có heo Quý+ chưa định danh | 1 lần giám định 100% đổi vật phẩm |
| Vàng / Cổng | Đổi Một Lấy Một | Có heo non F2 | Thưởng biết trước vs tương lai gen |
| Vàng / Sương | Thư Viện Mực Ấn | Qua Cổng Giữa Game | Đổi nguyên liệu gia cường 3:1; có Đá Cường Hóa |
| Đỏ / Xa trại | Đợt Săn Trong Sương | Chu kỳ 11+ | Phòng thủ cảnh báo ngắn; quái huyền bí |
| Đỏ / Xa trại | Càn Quét UFO | Khối lượng đàn cao | Áp lực trên không |
| Đỏ / Cổng | Giữ Cổng Ngược | Chủ động vào cổng Đỏ | Bảo vệ 1 mục tiêu; bản vẽ Cao cấp/Bạch Vân (rất hiếm) |
| Đỏ / Sương | Ba Ngày Khóa | ÁLSK ≥ 70 | Không xong → kẹt 3 ngày; Ngọc Phá Sương thoát |
| Đỏ / Cổng | Lò Mổ Hư Không | Có heo nhiễm Hư Mạch | Sinh tồn 5 phút; thuốc chữa Hư Mạch |



# PHẦN VI — CÂN BẰNG, TRIỂN KHAI & NGHIỆM THU


## 25. Tổng hợp công thức



| Hệ thống | Công thức / luật |
| --- | --- |
| Sức chứa & mật độ | MIN(Đất×8; Mái trú×6; Bồn×12; Máng×10) − Xác ngoài Khu Xử Lý; Mật độ = Heo sống / Hiệu dụng; hệ số dịch nội suy 0,85 → 2,20 |
| Bầy Đàn | ≥ 100 gắn kết (Kim Thọ = 3) • SC ≥ 50 • Tinh thần TB ≥ 60 (55 có Kim Thọ) • mật độ 71–110% • ≥ 6 heo già Hòa nhập ≥ 65 • không dịch • Cấp trại ≥ 6, ≥ 4 đợt lớn; duy trì 3 ngày |
| Thủ lĩnh / kế vị | Sáng lập 6% + 0,15%×(Hòa nhập − 65) + 2% (×1,5 Kim Thọ, trần 20%) • Kế vị 5%/ngày (Kim Thọ 20%) + Bồi Dưỡng ≤ 30% • Truyền Thừa 25% (Kim Thọ 50%) + Bồi Dưỡng |
| Giá heo | Thịt: kg × 3G × Hạng × Giai đoạn × Giống × Chợ × Chu kỳ • Giống: 315G × Giai đoạn × Giống × Chợ |
| Dịch & lây | clamp(1% × SC × Mật độ × Thời tiết × Xác × Đặc biệt; 0,2%; 35%)/cụm/ngày • lây 10% × … × Thủ lĩnh 0,75, mỗi 6 giờ |
| Stamina | Gốc × (1 + ΣMôi trường + ΣSức khỏe + ΣTải) × (1 − ΣGiảm trừ), ×0,6–×2,0 |
| Sát thương nhận | Gốc × 100/(100 + Phòng thủ) × (1 − RES gốc × 0,5%) |
| Gia cường giấy | +1: 100% • +2: 65% + 6%/đá (≤ 95%) • +3: 40% + 5%/đá (≤ 80%) |
| Công trình | Lần dùng/đợt = floor(Nạp / Tiêu hao) • Thời gian = Gốc / Hiệu suất phòng (40/70/100%) • −1 độ bền mỗi 100 sát thương nhận |
| Sự kiện & Bí nhân | Bất chợt clamp(0,25% + 0,08%×ÁLSK; 8%) • phòng thủ (0/2/12%) × (1 + ÁLSK/50) • Bí nhân 3% × chu kỳ × (1 + ÁLSK/100), ≤ 25% |
| Trao đổi | Đúng món yêu cầu, hoặc cùng nhóm ≥ 110% GTTC; ô vàng 18%, giá GTTC × 1,4; phí 5%; Bí nhân 130–200%, 0% vàng |



## 26. Kế hoạch phân rã công việc (WBS)



| Giai đoạn | Thời lượng | Hạng mục |
| --- | --- | --- |
| P0 Nền tảng | 3 tuần | Đồng hồ 16 phút/ngày, 4 buổi, ngủ, bộ gieo chu kỳ; lưu/tải; khung dữ liệu heo, công trình, chợ, sự kiện, GTTC |
| P1 Trang trại | 6 tuần | Vòng đời, Tinh thần, Hòa nhập, Độ bám đàn; sức chứa sống/xác; mật độ nội suy; ÁLSK; SC 5 bậc; Trạng thái Bầy Đàn 3 bậc, thủ lĩnh, kế vị, Bồi Dưỡng, Truyền Thừa; 10 mối đe dọa sức khỏe; 10 thời tiết + trạng thái |
| P2 Nhân vật | 5 tuần | Chỉ số gốc + thưởng; Stamina 2 lớp, Nộ Khí, Huyết Tế; 6 ô trang bị, 3 cặp mở ô võ kỹ, lối chơi theo hạng cân; 24 kỹ năng; vật phẩm 4 nhóm; Phù Văn, Mệnh Ấn |
| P3 Phòng thủ | 6 tuần | Bản vẽ, Phòng Xây Dựng 3 cấp, hiệu suất NPC trực; 31 công trình tự động + 3 Bạch Vân thủ công; giới hạn số lượng; nạp trước; độ bền do bắn và bị đánh; lưới điện; 14 quái + 3 Boss; đợt lớn/nhỏ; báo cáo |
| P4 Kinh tế & Thông tin | 5 tuần | Chợ ngày vàng; chợ đêm “món muốn nhận”, 18% ô vàng, đấu giá; 8 chủ đề; Bí nhân 2 giờ đêm sát trại; 6 cấp tình báo; 12 dòng gen, dấu “?”, khắc chế sau định danh |
| P5 Chiều sâu | 5 tuần | Cổng Giữa Game; giấy 4 màu, Đá Cường Hóa; đặc tính ẩn; 14 sự kiện; vật phẩm thoát Cổng/Sương mù |
| P6 Cân bằng | 4 tuần | Playtest; đo vàng và kho giá trị mỗi chu kỳ; đo tỉ lệ đạt Bầy Đàn; kiểm thử bất biến |



## 27. Bất biến thiết kế & tiêu chí nghiệm thu



| Bất biến | Kiểm thử phải đạt |
| --- | --- |
| Bầy Đàn không tự có | Không có thủ lĩnh nào khi chưa đạt đủ 7 điều kiện Bầy Đàn; SC < 50 không bao giờ lập được bầy |
| Kế vị không chắc chắn | Tỉ lệ Truyền Thừa < 100% ở mọi trường hợp, kể cả Kim Thọ + bồi dưỡng tối đa |
| Gen xấu luôn có đường khắc chế | Mọi tính chất xấu đều có ít nhất 1 cách hạn chế sau định danh; Hư Thể có thể phong ấn |
| Công trình tự động, Bạch Vân thủ công | Chỉ 3 Bạch Vân cần kích tay; không cài đặt nào làm chúng tự kích |
| Không kháng công trình | Không quái nào có kháng/miễn nhiễm với công trình; giới hạn chỉ là số lượng, nạp, hồi chiêu, hao mòn |
| Không xây khi thiếu bản vẽ | Có vàng nhưng không bản vẽ/Phòng Xây Dựng → nút xây khóa |
| Không nạp, không sửa trong đợt | Khóa ở cả đợt lớn và đợt nhỏ |
| Chỉ số thưởng không mở điều kiện | Học võ, dùng kỹ năng ngoài phái chỉ xét chỉ số gốc |
| Ô võ kỹ cần đủ cặp | Thiếu 1 món trong cặp → ô võ kỹ tương ứng khóa |
| Bí nhân không nhận vàng | 0% giao dịch bằng vàng; tỉ giá luôn ≥ 130% GTTC |
| Vàng luôn có giá trị | Không mua trọn chợ đêm bằng vàng; nhưng vẫn cần vàng cho mọi hoạt động ngày |
| Đặc tính ẩn hoàn toàn | Tên/hiệu ứng không lộ trước khi xong 6 khóa |



## 28. Hạng mục hoãn

Prestige Reset (hoãn — xung đột với trại tích lũy và Truyền Thừa) • Nhiều phe ngoài hành tinh (hoãn) • Thả heo ngoài trang trại (loại) • Thêm công trình Bạch Vân hoặc công trình tấn công (loại — giữ 5 + 3) • Chợ giữa người chơi (hoãn — phá cân bằng trao đổi) • Mệnh Ấn cho NPC (loại theo thiết kế).


# PHỤ LỤC


## Phụ lục A — Thuật ngữ & ghi chú



| Thuật ngữ | Giải thích |
| --- | --- |
| Chu kỳ / Đợt lớn / Đợt nhỏ | 6–12 ngày kết thúc bằng đợt lớn; giữa chu kỳ 0–3 đợt nhỏ |
| ÁLSK | Áp Lực Sự Kiện 0–100 |
| Sinh Cảnh (SC) | Chỉ số môi trường 0–100; quyết định có lập được Bầy Đàn |
| Tinh thần / Hòa nhập / Độ bám đàn | Ổn định cảm xúc / mức gắn kết với đàn / trung bình của hai chỉ số |
| Trạng thái Bầy Đàn | Trạng thái của cả trại (Sơ Khai, Vững, Hưng Thịnh); điều kiện để có thủ lĩnh |
| Neo tinh thần / Truyền Thừa | Hiệu ứng thủ lĩnh / tầng tích lũy khi kế vị thành công (tối đa 3) |
| Bồi Dưỡng Kế Vị | Chuẩn bị ứng viên kế vị để tăng tỉ lệ kế vị và Truyền Thừa |
| Phong ấn (Hư Thể) | Tính chất xấu bị khóa vĩnh viễn nếu heo qua giai đoạn Đang lớn mà không dính trạng thái xấu |
| Ô Thế Công / Thủ / Biến | Ô võ kỹ mở bởi cặp Vũ khí+Giày / Giáp+Quần / Vòng tay+Vòng cổ |
| Nộ Khí / Huyết Tế | Năng lượng từ hạ quái / dùng HP thay Stamina |
| Phù Văn / Mệnh Ấn | Khắc lên trang bị / khắc lên nhân vật người chơi (NPC không được) |
| Bản vẽ / Phòng Xây Dựng | Điều kiện bắt buộc để xây công trình; phòng cũng đảm nhận sửa chữa |
| Bạch Vân | Cửu Tiêu Lôi Pháo, Thiên Nhãn Vọng Đài, Vạn Linh Hồi Nguyên Trận — kích hoạt thủ công |
| GTTC / món muốn nhận | Giá trị tham chiếu / món cụ thể người bán ở chợ đêm yêu cầu |
| Bậc vật phẩm I–IV | Thường, Tốt, Quý, Cực Phẩm |


Ghi chú: mọi con số là baseline v0.1; tinh chỉnh bằng playtest nhưng giữ cấu trúc luật. Ưu tiên khi mâu thuẫn: Chương 13 → Chương 20 → Chương 4.6 → Chương 10 → Chương 5 → các bảng catalog. Hình minh họa mang tính khái niệm; chữ đặt ở chú thích và bảng để luôn đọc rõ.


## Phụ lục B — Nhật ký thay đổi v5.0 → v6.0



| Mục | Thay đổi |
| --- | --- |
| 4.2 chỉ số heo | Khôi phục bảng; thêm Tinh thần, Hòa nhập, Độ bám đàn |
| 4.6 Bầy Đàn | Thêm 7 điều kiện hình thành, 3 bậc bầy, thủ lĩnh chỉ sinh từ bầy, kế vị có tỉ lệ, Bồi Dưỡng Kế Vị, Truyền Thừa |
| 5.2–5.3 Gen | Kim Thọ thành trụ cột bầy; Lôi Mạch, Mộng Nhãn mạnh tương xứng; thay Hắc Dạ bằng Xích Mao (gen xấu thuần hóa được); Hư Thể có cơ chế phong ấn; mọi tính chất xấu có khắc chế |
| 6.1 Sinh Cảnh | Mỗi bậc có tương tác riêng với Bầy Đàn |
| 6.4 Say Nắng | Cứu bằng lùa tới bùn, Xô Nước, Mái Che Tạm, phun sương (bỏ “khiêng vào bóng mát”) |
| 9.6, 12.5 Vật phẩm | Trao đổi theo món người bán yêu cầu; thêm 20 vật phẩm 4 nhóm |
| 11 NPC | Kỹ Sư Xây Dựng đảm nhận cả thi công và sửa chữa |
| 12 Combat | 6 ô trang bị, 3 cặp mở ô võ kỹ, Nộ Khí/Huyết Tế, lối chơi theo hạng cân, Phù Văn, Mệnh Ấn |
| 13–18 Phòng thủ | Bản vẽ + Phòng Xây Dựng; tên Bạch Vân; luật 9 mới (không kháng, giới hạn số lượng); độ bền mất khi bị đánh |
| 19 Quái | Bỏ kháng công trình; áp lực bằng hành vi |
| 20–21 Chợ & Bí nhân | 18% ô nhận vàng; Bí nhân 2 giờ đêm ngẫu nhiên, sát ranh giới trại, 0% vàng, tỉ giá 130–200% |
| Hình | Vẽ lại bìa, 1.1, 4.1, 7.1, 12.1; sơ đồ khối 13.1, 13.4, 19.1, 22.1, 23.1 |



## Phụ lục C — Tra cứu nhanh



| Nhóm | Thông số khóa |
| --- | --- |
| Thời gian | 1 ngày = 16 phút; chu kỳ 6–8 / 7–10 / 8–12 ngày; Boss mỗi 5 chu kỳ |
| Bầy Đàn | ≥ 100 gắn kết • SC ≥ 50 • Tinh thần ≥ 60 • mật độ 71–110% • ≥ 6 heo già • Cấp ≥ 6, ≥ 4 đợt |
| Thủ lĩnh | Neo ≥ 25 (+5/tầng Truyền Thừa) • kế vị 5% (Kim Thọ 20%) + Bồi Dưỡng ≤ 30% |
| Combat | 6 ô • 3 ô võ kỹ • Stamina 180 • Nộ Khí 100 • chỉ số thưởng ≤ +8 |
| Phòng thủ | Tự động • bản vẽ + Phòng Xây Dựng • Bạch Vân thủ công • Nỏ 50/50 • Thiên Nhãn 3/9 lít • Lôi Pháo 20 giây truyền năng, 2 phát |
| Chợ | Ngày: vàng • Đêm: món muốn nhận, 18% ô vàng, phí 5% • Bí nhân: 2 giờ đêm, 0% vàng |
| Sự kiện | Cổng thoát ≥ 50% + Bùa Hồi Hương • Sương mù ≤ 3 ngày hoặc La Bàn/Ngọc Phá Sương |


— Hết tài liệu — Pig Tycoon GDD v6.0 —
