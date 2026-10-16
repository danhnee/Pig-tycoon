PIG TYCOON
SỔ TAY DỰ ÁN HỢP NHẤT v3.0 — WBS đầy đủ • Phân công 4 người • Prompt chung cho Agent • Quy chuẩn Unity & Pixel Art • Kiểm thử nghiệm thu
| Hạng mục | Nội dung |
Hạng mục
Nội dung
| Cơ sở thiết kế | Pig Tycoon – Game Design Document v6.0 (file “cơ chế game Pig.docx”, lập 28/09/2026). Mọi số liệu là baseline v0.1 theo Design Lock. |
Cơ sở thiết kế
Pig Tycoon – Game Design Document v6.0 (file “cơ chế game Pig.docx”, lập 28/09/2026). Mọi số liệu là baseline v0.1 theo Design Lock.
| Tài liệu kế thừa | PigTycoon_WBS_v2 (bản phân công trước) – bản v3.0 này thay thế và mở rộng. |
Tài liệu kế thừa
PigTycoon_WBS_v2 (bản phân công trước) – bản v3.0 này thay thế và mở rộng.
| Repository | https://github.com/danhnee/Pig-tycoon |
Repository
https://github.com/danhnee/Pig-tycoon
| Engine | Unity 6.3 LTS (6000.3.x) – URP 2D – Pixel Perfect – C# |
Engine
Unity 6.3 LTS (6000.3.x) – URP 2D – Pixel Perfect – C#
| Phong cách | Pixel art top-down 3/4, tham chiếu cảm giác UI/UX của Momodora: Moonlit Farewell (không bắt buộc đạt). |
Phong cách
Pixel art top-down 3/4, tham chiếu cảm giác UI/UX của Momodora: Moonlit Farewell (không bắt buộc đạt).
| Nhóm | A – Tech Lead/Core & QA + Map Khu Lõi Vận Hành (map riêng phía sau trang trại, nối bằng Đoạn Chuyển Map: nhà chính, kho, Phòng Xây Dựng, Phòng Nghiên Cứu, Xưởng Cường Hóa, khu thông tin) + Làng NPC • B – World/Farm/Pig & Art Lead + Đồng Cỏ Heo Thả Rông (đã có map + nhân vật nữ An) • C – Combat/Defense/Enemy + Chợ Thị Trấn (nhân vật nam Khoa) • D – Economy/Info/Event & Data + Rừng Nguyên Sinh và các cảnh sự kiện |
Nhóm
A – Tech Lead/Core & QA + Map Khu Lõi Vận Hành (map riêng phía sau trang trại, nối bằng Đoạn Chuyển Map: nhà chính, kho, Phòng Xây Dựng, Phòng Nghiên Cứu, Xưởng Cường Hóa, khu thông tin) + Làng NPC • B – World/Farm/Pig & Art Lead + Đồng Cỏ Heo Thả Rông (đã có map + nhân vật nữ An) • C – Combat/Defense/Enemy + Chợ Thị Trấn (nhân vật nam Khoa) • D – Economy/Info/Event & Data + Rừng Nguyên Sinh và các cảnh sự kiện
| Ngày lập / bắt đầu | 05/10/2026 (Thứ Hai, tuần W1) – Kế hoạch 34 tuần, phát hành 1.0 dự kiến 30/05/2027 |
Ngày lập / bắt đầu
05/10/2026 (Thứ Hai, tuần W1) – Kế hoạch 34 tuần, phát hành 1.0 dự kiến 30/05/2027
| Trạng thái | Bản chính thức để thực thi. Thay đổi phải qua Issue + ADR, cập nhật số phiên bản tài liệu. |
Trạng thái
Bản chính thức để thực thi. Thay đổi phải qua Issue + ADR, cập nhật số phiên bản tài liệu.
MỤC LỤC
​
​
1. Tổng quan, phạm vi và nguyên tắc6
1.1. Mục tiêu của tài liệu6
1.2. Tóm tắt cơ chế GDD v6.0 cần hiện thực6
1.3. Nguyên tắc không được phá (Design Lock)7
1.4. Hiện trạng đầu vào7
2. Tổ chức nhóm và vai trò8
2.1. Vai trò chính, người dự phòng và trách nhiệm review8
2.2. Ma trận RACI theo hệ thống9
3. Định hướng kiến trúc và cấu trúc repo10
3.1. Lựa chọn: Monorepo + Unity project mô-đun hóa bằng Assembly Definition10
3.2. Cây thư mục chuẩn10
3.3. Các tầng kiến trúc và luật phụ thuộc11
3.4. Quyền sở hữu scene (tránh conflict YAML)11
4. Quy ước đặt tên thống nhất12
4.1. Tên dự án, module, namespace12
4.2. Bảng tiền tố asset12
4.3. Công thức đặt tên cho file mới (bắt buộc, cả người và agent)13
4.4. ID dữ liệu14
4.5. Quy ước code C#14
5. Quy chuẩn Pixel Art và mục tiêu UI/UX14
5.1. Thông số kỹ thuật thống nhất14
5.2. Bộ animation tối thiểu (MUST)15
5.3. Tham chiếu Momodora: Moonlit Farewell – học gì, không làm gì16
5.4. Nguyên tắc UI/UX17
6. Phân công 4 map chính và ma trận thiết kế asset17
6.1. Tổng quan 4 map và cách chia đều17
6.2. Map A – Khu Lõi Vận Hành (Farm_Core, phía sau trang trại) và Làng NPC (Hub_Village)18
6.3. Map B – Đồng Cỏ Heo Thả Rông (Farm_Main) – Owner: B22
6.4. Map C – Chợ Thị Trấn (Market_Town) – Owner: C23
6.5. D – Rừng Nguyên Sinh (Forest_Primeval) và khung cảnh sự kiện24
6.6. Kết nối giữa 4 map và chuẩn chung khi dựng map25
6.7. WBS dựng map (bổ sung vào các giai đoạn P0–P5)26
6.8. Nhân vật người chơi và nhân vật cốt truyện27
6.9. Asset khác28
6.10. Tổng hợp khối lượng theo người28
6.11. WBS bổ sung cho từng khu vực và phân chia 14 sự kiện29
7. WBS Master P0 – P631
7.1. P0 – Nền tảng (W1–W2: 05/10 – 18/10/2026)31
7.2. P1 – Vertical Slice (W3–W8: 19/10 – 29/11/2026)33
7.3. P2 – Alpha: hệ thống lõi (W9–W16: 30/11/2026 – 24/01/2027)34
7.4. P3 – Phòng thủ, quái và Boss đầy đủ (W17–W23: 25/01 – 14/03/2027)36
7.5. P4 – Chiều sâu (W24–W29: 15/03 – 25/04/2027)37
7.6. P5 – Polish, UI/UX, cân bằng (W30–W32: 26/04 – 16/05/2027)38
7.7. P6 – Release Candidate và phát hành 1.0 (W33–W34: 17/05 – 30/05/2027)39
8. Nhiệm vụ từng người: MUST và “Làm hơn mức đó thì tốt”40
8.1. Thành viên A – Tech Lead / Core & QA40
8.2. Thành viên B – World, Farm, Pig & Art Lead (người dùng tài liệu này)40
8.3. Thành viên C – Combat, Defense, Enemy Lead41
8.4. Thành viên D – Economy, Information, Event & Data Lead41
9. Lịch 34 tuần và các mốc nghiệm thu42
10. Các yếu tố kết nối giữa module (Contracts)43
11. Quy tắc làm việc trên GitHub45
11.1. Nhịp cập nhật bắt buộc45
11.2. Branch, commit, PR45
11.3. Mẫu PR bắt buộc (.github/PULL_REQUEST_TEMPLATE.md)46
11.4. Review và CI46
11.5. Cấu hình Git cho Unity46
12. Quy trình dùng Agent kết hợp Unity (Unity tắt khi agent chạy)47
12.1. Nguyên tắc: agent làm việc khi Unity đóng, con người mở Unity để kiểm47
12.2. Quy trình 10 bước cho mỗi work item47
12.3. Lệnh kiểm tra khi Unity tắt48
12.4. Cấu hình từng IDE agent (một nguồn: AGENTS.md)48
12.5. MCP for Unity (chế độ 4, tùy chọn)49
12.6. Mẹo giảm nặng máy49
13. Prompt chung cho Agent (AGENTS.md) – bắt buộc49
13.1. Quy định sử dụng49
13.2. Nội dung AGENTS.md (phiên bản 1.0)49
13.3. Mẫu Task Card gửi kèm prompt51
14. Code tham chiếu cho Agent51
14.1. Asmdef của tầng Core (thuần C#)51
14.2. Đồng hồ game theo GDD 1.451
14.3. Công thức cộng dồn chỉ số (GDD 1.4) và test mẫu52
14.4. Định nghĩa dữ liệu (ScriptableObject) và bản ghi JSON52
14.5. Dự án test chạy ngoài Unity và script verify53
14.6. Regex kiểm tra tên asset (dùng trong Check-Names.ps1)53
15. Thư viện và công cụ được phép dùng54
15.1. Gói chính thức của Unity54
15.2. Thư viện mã nguồn mở bên thứ ba (đã kiểm tra là giấy phép mở)55
15.3. Công cụ ngoài Unity55
16. Nguồn tham khảo code, asset và kiến thức cho Agent56
17. Kiểm thử và nghiệm thu theo giai đoạn57
17.1. Tháp kiểm thử57
17.2. Definition of Ready và Definition of Done58
17.3. Cổng nghiệm thu G0 – G658
17.4. Bất biến thiết kế phải có test tự động59
17.5. Kiểm soát chất lượng khi dùng Agent (chống “pass cho có”)60
17.6. Playtest và đo lường60
18. Quản trị dự án chuyên nghiệp60
18.1. Nhịp làm việc60
18.2. Quản lý rủi ro61
18.3. Phiên bản, tài liệu quyết định, bảo mật, giấy phép61
18.4. Hạng mục hoãn (không làm ở bản 1.0)62
19. Phụ lục62
19.1. Checklist khởi động tuần đầu (W1)62
19.2. Thuật ngữ62
19.3. Lịch sử tài liệu63
​
​
1. Tổng quan, phạm vi và nguyên tắc
1.1. Mục tiêu của tài liệu
Tài liệu này là “nguồn sự thật vận hành” duy nhất của nhóm 4 người khi phát triển Pig Tycoon bằng Unity. Nó trả lời đủ các câu hỏi: ai làm gì (WBS và phân công), làm tới đâu là đạt (MUST) và làm hơn thì tốt (STRETCH), làm bằng công cụ gì (Unity + IDE có agent), đặt file ở đâu và tên gì, cập nhật GitHub thế nào, agent phải tuân thủ prompt nào, và mỗi giai đoạn được nghiệm thu ra sao.
Thứ tự ưu tiên khi có mâu thuẫn: (1) GDD v6.0 → (2) Sổ thông số chuẩn trong GDD mục 1.5 (được chuyển thành file dữ liệu Balance) → (3) Sổ tay này → (4) code hiện có trong repo. Agent và con người đều không được tự giải quyết mâu thuẫn bằng cách đoán; phải mở Issue.
1.2. Tóm tắt cơ chế GDD v6.0 cần hiện thực
Pig Tycoon là game tycoon mô phỏng hệ thống kết hợp thủ thành có nhân vật điều khiển. Triết lý: “Luật tối thiểu — Tác động chéo tối đa”, ngân sách là van khóa sức mạnh. Vòng lặp lõi: chăm đàn → tạo dòng tiền → phân bổ ngân sách → thu thập tình báo → nạp trước cho công trình → đợt quái/sự kiện → sửa chữa, phục hồi → lặp lại. Thang thời gian: 1 ngày game = 16 phút thực, 1 giờ game = 40 giây thực, mọi phép tính “mỗi ngày” chốt lúc 06:00; combat và đợt quái tính bằng giây và dừng đồng hồ ngày.
| Nhóm nội dung (bản 1.0) | Số lượng | Ghi chú theo GDD |
Nhóm nội dung (bản 1.0)
Số lượng
Ghi chú theo GDD
| Nhân vật người chơi | 2 | 1 nam (Khoa – C phụ trách), 1 nữ (An – B đã thiết kế). Người chơi chọn 1 để điều khiển. |
Nhân vật người chơi
2
1 nam (Khoa – C phụ trách), 1 nữ (An – B đã thiết kế). Người chơi chọn 1 để điều khiển.
| NPC trang trại / chuyên môn | 7 | Không tham chiến; đặc tính chỉ hỗ trợ việc hằng ngày. |
NPC trang trại / chuyên môn
7
Không tham chiến; đặc tính chỉ hỗ trợ việc hằng ngày.
| NPC chiến đấu / lính đánh thuê | 5 | Chỉ phòng thủ, thuê ở chợ đêm. |
NPC chiến đấu / lính đánh thuê
5
Chỉ phòng thủ, thuê ở chợ đêm.
| Thú cưng | 7 | 4 hỗ trợ hằng ngày + 3 canh gác. |
Thú cưng
7
4 hỗ trợ hằng ngày + 3 canh gác.
| Dòng gen heo | 12 | 6 bậc độ hiếm, mỗi dòng một cơ chế riêng; Xích Mao là gen “Khá xấu” khắc phục được; Hư Thể có tính xấu bị phong ấn. |
Dòng gen heo
12
6 bậc độ hiếm, mỗi dòng một cơ chế riêng; Xích Mao là gen “Khá xấu” khắc phục được; Hư Thể có tính xấu bị phong ấn.
| Phái võ / kỹ năng | 4 / 24 | 6 kỹ năng mỗi phái, mỗi kỹ năng một “động từ” riêng. |
Phái võ / kỹ năng
4 / 24
6 kỹ năng mỗi phái, mỗi kỹ năng một “động từ” riêng.
| Công trình phòng thủ | 31 | 28 tự động + 3 Bạch Vân kích hoạt thủ công (Cửu Tiêu Lôi Pháo, Thiên Nhãn Vọng Đài, Vạn Linh Hồi Nguyên Trận); cần bản vẽ + Phòng Xây Dựng. |
Công trình phòng thủ
31
28 tự động + 3 Bạch Vân kích hoạt thủ công (Cửu Tiêu Lôi Pháo, Thiên Nhãn Vọng Đài, Vạn Linh Hồi Nguyên Trận); cần bản vẽ + Phòng Xây Dựng.
| Quái / Boss chu kỳ | 14 / 3 | Mặt đất, trên không, huyền bí, môi trường. |
Quái / Boss chu kỳ
14 / 3
Mặt đất, trên không, huyền bí, môi trường.
| Thời tiết / Mối đe dọa sức khỏe | 10 / 10 | Sức khỏe chia 4 nhóm, xử lý bằng 5 “động từ”: cách ly, xử lý môi trường, điều trị cá thể, phòng ngừa đàn, nghiên cứu. |
Thời tiết / Mối đe dọa sức khỏe
10 / 10
Sức khỏe chia 4 nhóm, xử lý bằng 5 “động từ”: cách ly, xử lý môi trường, điều trị cá thể, phòng ngừa đàn, nghiên cứu.
| Vật phẩm phá cooldown ngủ | 5 | 4 bậc; chỉ Quả Mộng không có tác dụng phụ. |
Vật phẩm phá cooldown ngủ
5
4 bậc; chỉ Quả Mộng không có tác dụng phụ.
| Mẫu sự kiện huyền bí | 14 | 3 màu (Đỏ/Vàng/Xanh) × 2 hình thức (Cổng / Sương mù). |
Mẫu sự kiện huyền bí
14
3 màu (Đỏ/Vàng/Xanh) × 2 hình thức (Cổng / Sương mù).
Các hệ thống liên quan còn lại cũng nằm trong phạm vi: không gian trại (1 m = 1 unit, Ô Xây 2×2 m, Ô Đất 10×10 m, Nơi Trú, Khu Cách Ly, Khu Xử Lý, lưới điện), Tinh thần và Hòa nhập 0–100, Trạng thái Bầy Đàn (≥100 con gắn kết, Sinh Cảnh ≥50), thủ lĩnh/kế vị/Bồi Dưỡng Kế Vị, Sinh Cảnh 5 bậc, Stamina 2 lớp, 6 ô trang bị (mỗi cặp mở 1 ô võ kỹ), Phù Văn/Mệnh Ấn, chợ ngày bằng vàng, chợ đêm trao đổi (chỉ khoảng 18% ô nhận vàng), đấu giá, GTTC, Bí nhân (không nhận vàng), tình báo, Giấy Chuyên Biệt, Cổng Giữa Game và các bất biến thiết kế ở GDD mục 27.
1.3. Nguyên tắc không được phá (Design Lock)
Không thay đổi luật cấu trúc hoặc số cân bằng vì “agent gợi ý”. Muốn đổi số: mở Issue loại balance, trích mục GDD, cập nhật Sổ thông số chuẩn trước rồi mới sửa code.
Chỉ đúng 2 nhân vật người chơi (Khoa, An). A và D thể hiện phần “thiết kế nhân vật” qua NPC, thú cưng, quái, Boss và nhân vật cốt truyện, không tạo nhân vật người chơi thứ ba.
Mọi công trình tự động, trừ 3 công trình Bạch Vân kích tay. Trang bị chỉ cho chỉ số thưởng, không tính vào điều kiện học võ.
Mọi số liệu là baseline v0.1, chỉnh bằng dữ liệu playtest, không “cảm tính”.
1.4. Hiện trạng đầu vào
B đã thiết kế map heo thả rông và nhân vật nữ An → được tính là đầu vào của P0, việc còn lại là chuẩn hóa theo Art Bible và đưa vào repo.
C phụ trách thiết kế nhân vật nam Khoa từ đầu.
Repo đang ở mức khởi tạo, nên phải xong giai đoạn P0 (khung dự án, quy chuẩn, CI, catalog dữ liệu) trước khi chia việc code lớn.
2. Tổ chức nhóm và vai trò
2.1. Vai trò chính, người dự phòng và trách nhiệm review
Nhóm chia việc theo “trục sở hữu”: mỗi hệ thống có đúng một người chịu trách nhiệm cuối cùng, còn art/nội dung được chia chéo để cả 4 người đều trực tiếp thiết kế nhân vật, NPC, thú cưng, heo, công trình và quái. Không ai “chỉ code” hay “chỉ vẽ”.
| Người | Vai trò | Sở hữu hệ thống | Dự phòng | Review chính |
Người
Vai trò
Sở hữu hệ thống
Dự phòng
Review chính
| A | Tech Lead / Core Systems & QA | Kiến trúc, thời gian/ngày-đêm, save/load, player core dùng chung, input, inventory, lưới điện và hạ tầng điện, thời tiết (logic), Cấp trại 1–20, NPC framework, debug/telemetry, CI, hiệu năng, build & release. Map Farm_Core + Hub_Village. UI: luồng màn hình chung (menu, chọn nhân vật, save, pause, cài đặt), HUD lõi, kho, ngủ, Cấp trại, nghiên cứu, cường hóa. | D | Review PR của D; duyệt mọi thay đổi kiến trúc/save/schema. |
A
Tech Lead / Core Systems & QA
Kiến trúc, thời gian/ngày-đêm, save/load, player core dùng chung, input, inventory, lưới điện và hạ tầng điện, thời tiết (logic), Cấp trại 1–20, NPC framework, debug/telemetry, CI, hiệu năng, build & release. Map Farm_Core + Hub_Village. UI: luồng màn hình chung (menu, chọn nhân vật, save, pause, cài đặt), HUD lõi, kho, ngủ, Cấp trại, nghiên cứu, cường hóa.
D
Review PR của D; duyệt mọi thay đổi kiến trúc/save/schema.
| B | World – Farm – Pig & Art Consistency Lead | Map Farm_Main, lưới Ô Xây/Ô Đất, hạ tầng trại, Nơi Trú Đàn, đàn heo (AI thả rông, vòng đời, Tinh thần, Hòa nhập, Bầy Đàn, thủ lĩnh), sinh sản và hậu duệ heo già, xác/Khu Xử Lý, Sinh Cảnh, sức khỏe, thú cưng framework, nhân vật An, Art Bible. UI: thẻ heo, panel đàn, sức chứa điểm nghẽn, lớp phủ phục vụ/nguồn mát. | C | Review PR của A; duyệt mọi thay đổi Art Bible. |
B
World – Farm – Pig & Art Consistency Lead
Map Farm_Main, lưới Ô Xây/Ô Đất, hạ tầng trại, Nơi Trú Đàn, đàn heo (AI thả rông, vòng đời, Tinh thần, Hòa nhập, Bầy Đàn, thủ lĩnh), sinh sản và hậu duệ heo già, xác/Khu Xử Lý, Sinh Cảnh, sức khỏe, thú cưng framework, nhân vật An, Art Bible. UI: thẻ heo, panel đàn, sức chứa điểm nghẽn, lớp phủ phục vụ/nguồn mát.
C
Review PR của A; duyệt mọi thay đổi Art Bible.
| C | Combat – Defense – Enemy Lead | Nhân vật Khoa, combat, Stamina 2 lớp, chỉ số/trang bị/võ kỹ, 24 kỹ năng, Phù Văn/Mệnh Ấn (logic), Nộ Khí/Huyết Tế, bản vẽ + Phòng Xây Dựng (logic), 31 công trình, độ bền/sửa, wave/quái/Boss, báo cáo sau đợt, AI lính đánh thuê. Map Market_Town. UI: trang bị 6 ô, võ kỹ, xây dựng, báo cáo đợt. | A | Review PR của B; duyệt cân bằng combat. |
C
Combat – Defense – Enemy Lead
Nhân vật Khoa, combat, Stamina 2 lớp, chỉ số/trang bị/võ kỹ, 24 kỹ năng, Phù Văn/Mệnh Ấn (logic), Nộ Khí/Huyết Tế, bản vẽ + Phòng Xây Dựng (logic), 31 công trình, độ bền/sửa, wave/quái/Boss, báo cáo sau đợt, AI lính đánh thuê. Map Market_Town. UI: trang bị 6 ô, võ kỹ, xây dựng, báo cáo đợt.
A
Review PR của B; duyệt cân bằng combat.
| D | Economy – Information – Event & Data Lead | Data Catalog & Balance, chợ ngày/đêm, hợp đồng giao heo, 8 chủ đề chu kỳ, đấu giá, GTTC, ngân sách, Bí nhân, tình báo, gen (logic định danh/giám định/nghiên cứu), Giấy Chuyên Biệt và gia cường, khung + 14 sự kiện huyền bí, Localization. Map Forest_Primeval. UI: chợ, đấu giá, Bí nhân, tình báo, giấy, giám định, ngân sách, minimap. | B | Review PR của C; duyệt thay đổi dữ liệu/catalog. |
D
Economy – Information – Event & Data Lead
Data Catalog & Balance, chợ ngày/đêm, hợp đồng giao heo, 8 chủ đề chu kỳ, đấu giá, GTTC, ngân sách, Bí nhân, tình báo, gen (logic định danh/giám định/nghiên cứu), Giấy Chuyên Biệt và gia cường, khung + 14 sự kiện huyền bí, Localization. Map Forest_Primeval. UI: chợ, đấu giá, Bí nhân, tình báo, giấy, giám định, ngân sách, minimap.
B
Review PR của C; duyệt thay đổi dữ liệu/catalog.
2.2. Ma trận RACI theo hệ thống
R = thực hiện, A = chịu trách nhiệm cuối cùng, C = được hỏi ý kiến, I = được thông báo.
| Hệ thống | A | B | C | D |
Hệ thống
A
B
C
D
| Kiến trúc, Time, Save, Input, CI | A/R | C | C | C |
Kiến trúc, Time, Save, Input, CI
A/R
C
C
C
| Map, Farm grid, Đàn heo, Sinh Cảnh, Sức khỏe | C | A/R | I | C |
Map, Farm grid, Đàn heo, Sinh Cảnh, Sức khỏe
C
A/R
I
C
| Combat, Kỹ năng, Công trình, Quái/Boss | C | I | A/R | C |
Combat, Kỹ năng, Công trình, Quái/Boss
C
I
A/R
C
| Chợ, Ngân sách, Tình báo, Bí nhân, Sự kiện | C | C | C | A/R |
Chợ, Ngân sách, Tình báo, Bí nhân, Sự kiện
C
C
C
A/R
| Gen (logic + 12 gene pack art) | R (art) | R (rig + art) | R (art) | A/R |
Gen (logic + 12 gene pack art)
R (art)
R (rig + art)
R (art)
A/R
| Art Bible, Palette, Import preset | C | A/R | C | C |
Art Bible, Palette, Import preset
C
A/R
C
C
| UI Kit & UX | R (HUD) | C | R (combat UI) | A/R |
UI Kit & UX
R (HUD)
C
R (combat UI)
A/R
| Hiệu năng, Build, Release | A/R | R | R | R |
Hiệu năng, Build, Release
A/R
R
R
R
| Prompt chung cho agent (AGENTS.md) | A/R | C | C | C |
Prompt chung cho agent (AGENTS.md)
A/R
C
C
C
3. Định hướng kiến trúc và cấu trúc repo
3.1. Lựa chọn: Monorepo + Unity project mô-đun hóa bằng Assembly Definition
Dự án dùng một repo duy nhất (monorepo) chứa Unity project, tài liệu, nguồn art, công cụ và CI. Bên trong Unity, code được chia thành nhiều assembly (asmdef) theo module. Lý do chọn hướng này thay vì tách nhiều repo hoặc dùng submodule:
Unity chỉ mở được một project; tách repo làm vỡ GUID của .meta và tham chiếu prefab. Monorepo giữ một lịch sử thống nhất, một CI, một lần review.
Asmdef giúp mỗi module biên dịch riêng (sửa Herd không bắt biên dịch lại Combat), khóa phụ thuộc một chiều, và cho phép test độc lập.
Tầng Core thuần C# (không dùng UnityEngine) có thể chạy unit test bằng dotnet khi Unity đang tắt – đây là chìa khóa để agent làm việc mà không cần mở Unity.
Hướng phát triển lâu dài: khi một module ổn định có thể tách thành Unity package nội bộ (thư mục Packages/com.pigtycoon.*) mà không đổi repo; mùa 2 chỉ thêm module/Content mới.
3.2. Cây thư mục chuẩn
| Pig-tycoon/ (monorepo) ├─ AGENTS.md ............ prompt chung bắt buộc cho mọi agent ├─ CLAUDE.md / GEMINI.md / .github/copilot-instructions.md / .cursor/rules/ → chỉ 1 dòng trỏ về AGENTS.md ├─ README.md • CHANGELOG.md • THIRD_PARTY_NOTICES.md • .gitignore • .gitattributes • .editorconfig ├─ .github/ ............ workflows/, ISSUE_TEMPLATE/, PULL_REQUEST_TEMPLATE.md, CODEOWNERS ├─ docs/ ............... gdd/ (GDD v6.0 PDF/DOCX), handbook/ (sổ tay này), adr/, art-bible/, agent/ (task cards), playtest/ ├─ art-source/ ......... file gốc .aseprite/.psd theo Characters, Npcs, Pets, Pigs, Buildings, Enemies, Tiles, Ui, Vfx (Git LFS) ├─ tools/ .............. verify.ps1, verify.sh, lint/Check-Names.ps1, DomainTests/ (dotnet test), sim/ (mô phỏng kinh tế) └─ game/PigTycoon/ ..... Unity project (Assets, Packages, ProjectSettings) └─ Assets/_Project/ ├─ Art/ ........ Characters, Npcs, Pets, Pigs, Buildings, Enemies, Tiles, Ui, Vfx, Fonts (PNG/ase đã import) ├─ Audio/ ...... Sfx, Music ├─ Code/ │  ├─ Core/ ...... PigTycoon.Core (thuần C#, noEngineReferences): Time, Stats, Herd, Genetics, Economy, Combat rules… │  ├─ Runtime/ ... PigTycoon.<Module>: Bootstrap, Time, Save, Input, Farm, Herd, Health, Player, Combat, Defense, Enemy, Economy, Intel, Events, Npc, Pet, Ui │  ├─ Editor/ .... công cụ editor, importer preset, validator, CI entry │  └─ Tests/ ..... EditMode/, PlayMode/ ├─ Data/ ....... Balance/ (Sổ thông số chuẩn .json), Catalog/ (ScriptableObject định nghĩa), Localization/ ├─ Prefabs/ .... theo module giống Code/Runtime ├─ Scenes/ ..... Boot, Farm_Main (B), Farm_Core (A, map riêng phía sau trại, nối qua Đoạn Chuyển Map), Hub_Village (A), Market_Town (C), Forest_Primeval (D), Event_<Color>_<Name> (theo bảng 6.11), CombatSandbox, MarketEventSandbox, TestHarness, Sandbox_<Owner>_<Feature> └─ Settings/ ... URP, Input Actions, Pixel Perfect, Presets |
Pig-tycoon/ (monorepo)
├─ AGENTS.md ............ prompt chung bắt buộc cho mọi agent
├─ CLAUDE.md / GEMINI.md / .github/copilot-instructions.md / .cursor/rules/ → chỉ 1 dòng trỏ về AGENTS.md
├─ README.md • CHANGELOG.md • THIRD_PARTY_NOTICES.md • .gitignore • .gitattributes • .editorconfig
├─ .github/ ............ workflows/, ISSUE_TEMPLATE/, PULL_REQUEST_TEMPLATE.md, CODEOWNERS
├─ docs/ ............... gdd/ (GDD v6.0 PDF/DOCX), handbook/ (sổ tay này), adr/, art-bible/, agent/ (task cards), playtest/
├─ art-source/ ......... file gốc .aseprite/.psd theo Characters, Npcs, Pets, Pigs, Buildings, Enemies, Tiles, Ui, Vfx (Git LFS)
├─ tools/ .............. verify.ps1, verify.sh, lint/Check-Names.ps1, DomainTests/ (dotnet test), sim/ (mô phỏng kinh tế)
└─ game/PigTycoon/ ..... Unity project (Assets, Packages, ProjectSettings)
└─ Assets/_Project/
├─ Art/ ........ Characters, Npcs, Pets, Pigs, Buildings, Enemies, Tiles, Ui, Vfx, Fonts (PNG/ase đã import)
├─ Audio/ ...... Sfx, Music
├─ Code/
│  ├─ Core/ ...... PigTycoon.Core (thuần C#, noEngineReferences): Time, Stats, Herd, Genetics, Economy, Combat rules…
│  ├─ Runtime/ ... PigTycoon.<Module>: Bootstrap, Time, Save, Input, Farm, Herd, Health, Player, Combat, Defense, Enemy, Economy, Intel, Events, Npc, Pet, Ui
│  ├─ Editor/ .... công cụ editor, importer preset, validator, CI entry
│  └─ Tests/ ..... EditMode/, PlayMode/
├─ Data/ ....... Balance/ (Sổ thông số chuẩn .json), Catalog/ (ScriptableObject định nghĩa), Localization/
├─ Prefabs/ .... theo module giống Code/Runtime
├─ Scenes/ ..... Boot, Farm_Main (B), Farm_Core (A, map riêng phía sau trại, nối qua Đoạn Chuyển Map), Hub_Village (A), Market_Town (C), Forest_Primeval (D), Event_<Color>_<Name> (theo bảng 6.11), CombatSandbox, MarketEventSandbox, TestHarness, Sandbox_<Owner>_<Feature>
└─ Settings/ ... URP, Input Actions, Pixel Perfect, Presets
3.3. Các tầng kiến trúc và luật phụ thuộc
| Tầng | Chứa gì | Được phép phụ thuộc |
Tầng
Chứa gì
Được phép phụ thuộc
| Core (PigTycoon.Core) | Luật game, công thức, state thuần dữ liệu, không MonoBehaviour. | Không phụ thuộc gì ngoài .NET Standard 2.1. Test bằng dotnet và Unity EditMode. |
Core (PigTycoon.Core)
Luật game, công thức, state thuần dữ liệu, không MonoBehaviour.
Không phụ thuộc gì ngoài .NET Standard 2.1. Test bằng dotnet và Unity EditMode.
| Runtime (PigTycoon.<Module>) | MonoBehaviour, view, adapter đọc Core, service Unity. | Core + module khác chỉ qua interface trong PigTycoon.Contracts. |
Runtime (PigTycoon.<Module>)
MonoBehaviour, view, adapter đọc Core, service Unity.
Core + module khác chỉ qua interface trong PigTycoon.Contracts.
| Contracts (PigTycoon.Contracts) | Interface, struct sự kiện, ID dùng chung. | Chỉ Core. A sở hữu; đổi contract phải có ADR. |
Contracts (PigTycoon.Contracts)
Interface, struct sự kiện, ID dùng chung.
Chỉ Core. A sở hữu; đổi contract phải có ADR.
| Ui (PigTycoon.Ui) | UI Toolkit (UXML/USS), presenter. | Contracts; không gọi trực tiếp logic module. |
Ui (PigTycoon.Ui)
UI Toolkit (UXML/USS), presenter.
Contracts; không gọi trực tiếp logic module.
| Editor / Tests | Công cụ, validator, test. | Mọi tầng (chỉ trong Editor). |
Editor / Tests
Công cụ, validator, test.
Mọi tầng (chỉ trong Editor).
3.4. Quyền sở hữu scene (tránh conflict YAML)
| Scene | Owner | Quy tắc |
Scene
Owner
Quy tắc
| Boot + TestHarness | A | Khởi tạo service, nạp dữ liệu, SceneLoader nạp scene theo kiểu additive. Người khác đưa tính năng vào qua prefab/service. |
Boot + TestHarness
A
Khởi tạo service, nạp dữ liệu, SceneLoader nạp scene theo kiểu additive. Người khác đưa tính năng vào qua prefab/service.
| Farm_Main – Đồng cỏ, dải phòng thủ, vành ngoài | B | Cảnh mô phỏng chính trong ranh giới trại. Mọi đợt quái, Cổng sự kiện và Bí nhân đều xảy ra ở đây (GDD 3.6, 21, 24). |
Farm_Main – Đồng cỏ, dải phòng thủ, vành ngoài
B
Cảnh mô phỏng chính trong ranh giới trại. Mọi đợt quái, Cổng sự kiện và Bí nhân đều xảy ra ở đây (GDD 3.6, 21, 24).
| Farm_Core – Khu Lõi Vận Hành (map riêng phía sau trại) | A | Nhà chính, Kho, Phòng Xây Dựng, Phòng Nghiên Cứu, Xưởng Cường Hóa, Lều Quân Y, khu thông tin. Nằm phía sau (phía Bắc) trang trại, nối với Farm_Main bằng Đoạn Chuyển Map. Về mặt luật, map này vẫn thuộc ranh giới trại nên quái vẫn có thể nhắm tới Kho và Nhà chính. |
Farm_Core – Khu Lõi Vận Hành (map riêng phía sau trại)
A
Nhà chính, Kho, Phòng Xây Dựng, Phòng Nghiên Cứu, Xưởng Cường Hóa, Lều Quân Y, khu thông tin. Nằm phía sau (phía Bắc) trang trại, nối với Farm_Main bằng Đoạn Chuyển Map. Về mặt luật, map này vẫn thuộc ranh giới trại nên quái vẫn có thể nhắm tới Kho và Nhà chính.
| Hub_Village – Làng NPC (ngoại vi) | A | Điểm chuyển cảnh: nhà NPC, nhà Bà Sáu, cốt truyện. Không có combat. |
Hub_Village – Làng NPC (ngoại vi)
A
Điểm chuyển cảnh: nhà NPC, nhà Bà Sáu, cốt truyện. Không có combat.
| Market_Town – Chợ Thị Trấn (ngoại vi) | C | Chợ ngày, chợ đêm, đấu giá, kiểm dịch/giám định, hội quán thuê lính. Không có combat. |
Market_Town – Chợ Thị Trấn (ngoại vi)
C
Chợ ngày, chợ đêm, đấu giá, kiểm dịch/giám định, hội quán thuê lính. Không có combat.
| Forest_Primeval – Rừng Nguyên Sinh (ngoại vi) | D | Khám phá, thu thập vật liệu. Không có đợt quái. |
Forest_Primeval – Rừng Nguyên Sinh (ngoại vi)
D
Khám phá, thu thập vật liệu. Không có đợt quái.
| Event_<Color>_<Name> – 12 cảnh bên trong Cổng/Sương mù | Theo bảng 6.11 | D sở hữu khung chung (vào/thoát, đếm %, vật phẩm thoát). Mỗi người dựng các cảnh sự kiện được giao. |
Event_<Color>_<Name> – 12 cảnh bên trong Cổng/Sương mù
Theo bảng 6.11
D sở hữu khung chung (vào/thoát, đếm %, vật phẩm thoát). Mỗi người dựng các cảnh sự kiện được giao.
| CombatSandbox / MarketEventSandbox | C / D | Scene thử nghiệm, không chứa state thật. |
CombatSandbox / MarketEventSandbox
C / D
Scene thử nghiệm, không chứa state thật.
| Sandbox_<Owner>_<Feature> | Người tạo | Scene cá nhân để thử; xóa sau khi merge. |
Sandbox_<Owner>_<Feature>
Người tạo
Scene cá nhân để thử; xóa sau khi merge.
| Luật chung: chỉ owner được mở và lưu file scene của mình. Người khác giao prefab qua branch, owner tự đặt vào scene. Mọi logic gameplay nằm trong prefab, data hoặc Core, không viết riêng trong scene. |
Luật chung: chỉ owner được mở và lưu file scene của mình. Người khác giao prefab qua branch, owner tự đặt vào scene. Mọi logic gameplay nằm trong prefab, data hoặc Core, không viết riêng trong scene.
4. Quy ước đặt tên thống nhất
4.1. Tên dự án, module, namespace
Tên sản phẩm: Pig Tycoon. Tên kỹ thuật (thư mục, namespace, company): PigTycoon. Repo: Pig-tycoon. Bundle ID: com.pigtycoon.game.
Namespace = PigTycoon.<Module>[.<SubModule>], ví dụ PigTycoon.Herd.Navigation. Tên asmdef trùng namespace gốc của module.
Danh sách module chuẩn (không tự đặt thêm khi chưa có ADR): Bootstrap, Time, Save, Input, Farm, Herd, Genetics, Health, Weather, Player, Combat, Skills, Defense, Enemy, Economy, Market, Intel, Events, Npc, Pet, Items, Ui, Audio, Narrative, Debug.
Tên tiếng Việt trong file/ID viết không dấu, PascalCase cho asset và snake_case cho ID dữ liệu. Ví dụ: Lam Khê → LamKhe / lam_khe; Bạch Vân – Cửu Tiêu Lôi Pháo → BachVanCuuTieuLoiPhao / bach_van_cuu_tieu_loi_phao.
4.2. Bảng tiền tố asset
| Tiền tố | Loại | Ví dụ |
Tiền tố
Loại
Ví dụ
| SPR | Sprite / sprite sheet | SPR_Player_An_Walk_S.png |
SPR
Sprite / sprite sheet
SPR_Player_An_Walk_S.png
| ASE | File nguồn Aseprite | ASE_Pig_LamKhe_Adult.aseprite |
ASE
File nguồn Aseprite
ASE_Pig_LamKhe_Adult.aseprite
| ANIM / AC | Animation clip / Animator controller | ANIM_Npc_BacSiThuY_Idle_E.anim • AC_Pig_Base.controller |
ANIM / AC
Animation clip / Animator controller
ANIM_Npc_BacSiThuY_Idle_E.anim • AC_Pig_Base.controller
| PF | Prefab | PF_Building_TramKhiTuong.prefab |
PF
Prefab
PF_Building_TramKhiTuong.prefab
| SO | ScriptableObject dữ liệu | SO_Gene_XichMao.asset |
SO
ScriptableObject dữ liệu
SO_Gene_XichMao.asset
| TILE / TP | Tile / Tile palette | TILE_Farm_Grass_01 • TP_Farm_Main |
TILE / TP
Tile / Tile palette
TILE_Farm_Grass_01 • TP_Farm_Main
| UI / USS / UXML | Sprite UI / stylesheet / layout | UI_Icon_Gold.png • USS_Hud.uss • UXML_MarketNight.uxml |
UI / USS / UXML
Sprite UI / stylesheet / layout
UI_Icon_Gold.png • USS_Hud.uss • UXML_MarketNight.uxml
| VFX / MAT / SHD | Hiệu ứng / material / shader | VFX_Combat_HitSpark • MAT_Sprite_Lit |
VFX / MAT / SHD
Hiệu ứng / material / shader
VFX_Combat_HitSpark • MAT_Sprite_Lit
| SFX / MUS | Âm thanh / nhạc | SFX_Pig_Squeal_02.wav • MUS_Night_Market.ogg |
SFX / MUS
Âm thanh / nhạc
SFX_Pig_Squeal_02.wav • MUS_Night_Market.ogg
| SCN | Scene (chỉ trong tài liệu; file scene dùng tên trần) | Farm_Main.unity |
SCN
Scene (chỉ trong tài liệu; file scene dùng tên trần)
Farm_Main.unity
| (không tiền tố) | Script C# | HerdFlowField.cs (tên file = tên class) |
(không tiền tố)
Script C#
HerdFlowField.cs (tên file = tên class)
4.3. Công thức đặt tên cho file mới (bắt buộc, cả người và agent)
<Prefix>_<Domain>_<Subject>[_<Variant>][_<State>][_<Dir>][_<NN>].<ext>
Prefix: lấy từ bảng 4.2. Domain: Player, Npc, Pet, Pig, Building, Enemy, Boss, Tile, Ui, Vfx, Item, Skill, Event, Weather. Subject: tên riêng PascalCase không dấu.
Variant: giai đoạn/bậc/màu, ví dụ Piglet, Growing, Adult, Old, Lv2, Red. State: Idle, Walk, Run, Attack, Dodge, Interact, Hurt, Death, Active, Damaged, Telegraph. Dir: N, S, E, W (thêm NE, NW, SE, SW nếu có 8 hướng). NN: số thứ tự 2 chữ số khi có nhiều biến thể.
Chỉ dùng A–Z, a–z, 0–9 và dấu gạch dưới; không dấu cách, không tiếng Việt có dấu, không gạch ngang trong tên asset; tối đa 64 ký tự.
Không đánh số phiên bản trong tên (cấm _final, _v2, _new) – lịch sử do Git quản lý.
File phải nằm đúng thư mục module/domain; nếu không có thư mục phù hợp → mở Issue đề xuất, không tự tạo cây thư mục mới.
Tài liệu Markdown: kebab-case, ví dụ docs/adr/ADR-0007-save-format-v2.md; Task card: docs/agent/tasks/<WBS-ID>-<slug>.md.
Script kiểm tra tự động tools/lint/Check-Names.ps1 chạy trong CI; PR có file sai tên sẽ bị chặn.
4.4. ID dữ liệu
Mọi thực thể dữ liệu có ID bất biến dạng <domain>.<category>.<snake_name>, ví dụ: pig.gene.lam_khe, npc.farm.bac_si_thu_y, npc.combat.ve_binh_khien, pet.daily.cho_chan_dan, bld.attack.bach_van_cuu_tieu_loi_phao, enemy.air.<ten>, boss.<ten>, item.sleep.qua_mong, evt.red.gate.<ten>. ID không bao giờ đổi sau khi merge; đổi tên hiển thị chỉ sửa bảng Localization.
4.5. Quy ước code C#
Class/struct/method/property PascalCase; field private _camelCase; hằng PascalCase; interface tiền tố I; event dạng động từ quá khứ (PigDied, WaveStarted).
Một class công khai mỗi file; tên file = tên class. Không dùng #region để giấu code dài; method dài quá 40 dòng phải tách.
Không số “ma thuật”: mọi số cân bằng đọc từ Balance hoặc ScriptableObject, kèm chú thích mục GDD (ví dụ // GDD 4.3).
Áp dụng .editorconfig chung; bật nullable trong Core; không cảnh báo mới khi biên dịch.
Commit: [WBS-ID][A|B|C|D] mô tả ngắn tiếng Việt hoặc tiếng Anh, thể mệnh lệnh.
5. Quy chuẩn Pixel Art và mục tiêu UI/UX
5.1. Thông số kỹ thuật thống nhất
| Thông số | Giá trị chuẩn |
Thông số
Giá trị chuẩn
| Pixels Per Unit | 16 PPU (1 unit = 1 m theo GDD) → Ô Xây 2×2 m = 32×32 px; công trình Nhỏ/Vừa/Lớn = 32/64/96 px; Ô Đất 10×10 m = 160×160 px. Đã đổi từ 32 PPU để khớp khung camera của GDD 3.6. |
Pixels Per Unit
16 PPU (1 unit = 1 m theo GDD) → Ô Xây 2×2 m = 32×32 px; công trình Nhỏ/Vừa/Lớn = 32/64/96 px; Ô Đất 10×10 m = 160×160 px. Đã đổi từ 32 PPU để khớp khung camera của GDD 3.6.
| Góc nhìn | Top-down 3/4; 4 hướng (N/S/E/W) là MUST, 8 hướng là STRETCH sau khi MUST xong. |
Góc nhìn
Top-down 3/4; 4 hướng (N/S/E/W) là MUST, 8 hướng là STRETCH sau khi MUST xong.
| Độ phân giải tham chiếu | Zoom Cận: 640×360 px = 40×22,5 m (×3 lên 1920×1080, đúng GDD 40×22 m). Zoom Mặc định: 960×540 px = 60×33,75 m (×2). GDD ghi 64×36 m và đánh dấu “⚠ Cần khóa”, nên đề xuất chốt 60×34 m qua ADR để giữ pixel chuẩn. Toàn cảnh 128×72 m chỉ dùng để quản lý, cho phép không pixel-perfect. |
Độ phân giải tham chiếu
Zoom Cận: 640×360 px = 40×22,5 m (×3 lên 1920×1080, đúng GDD 40×22 m). Zoom Mặc định: 960×540 px = 60×33,75 m (×2). GDD ghi 64×36 m và đánh dấu “⚠ Cần khóa”, nên đề xuất chốt 60×34 m qua ADR để giữ pixel chuẩn. Toàn cảnh 128×72 m chỉ dùng để quản lý, cho phép không pixel-perfect.
| Import | Filter Mode = Point, Compression = None, Mip Map tắt, Pivot = Bottom Center cho thực thể, Sprite Mode Multiple; dùng Preset chung PRE_Sprite_Pixel16 (B tạo ở P0.05). |
Import
Filter Mode = Point, Compression = None, Mip Map tắt, Pivot = Bottom Center cho thực thể, Sprite Mode Multiple; dùng Preset chung PRE_Sprite_Pixel16 (B tạo ở P0.05).
| Palette | Một palette chung 48 màu (farm ấm, công nghệ xanh/cyan, huyền bí tím/xanh lạnh, nguy hiểm đỏ/cam) lưu tại docs/art-bible/palette.gpl + .ase. Không ai tự thêm màu ngoài palette khi chưa được B duyệt. |
Palette
Một palette chung 48 màu (farm ấm, công nghệ xanh/cyan, huyền bí tím/xanh lạnh, nguy hiểm đỏ/cam) lưu tại docs/art-bible/palette.gpl + .ase. Không ai tự thêm màu ngoài palette khi chưa được B duyệt.
| Ánh sáng & viền | Nguồn sáng trên-trái; viền 1 px màu tối của palette (không đen tuyệt đối); silhouette phải nhận ra được khi chuyển sang thang xám. |
Ánh sáng & viền
Nguồn sáng trên-trái; viền 1 px màu tối của palette (không đen tuyệt đối); silhouette phải nhận ra được khi chuyển sang thang xám.
| Kích thước canvas | Nhân vật người chơi/NPC: canvas 32×32 (thân cao 26–30 px ≈ 1,7 m, tỉ lệ chibi kiểu Momodora). Heo trưởng thành 24×20, heo non 12×10, heo đang lớn 18×14, heo già 24×20 + dấu hiệu tuổi. Thú cưng 16–24 px. Quái 24–48 px; Boss 64–128 px. Công trình theo footprint 32/64/96 px, được cao hơn footprint. Icon 16×16 và 32×32. Preset import đổi tên thành PRE_Sprite_Pixel16. |
Kích thước canvas
Nhân vật người chơi/NPC: canvas 32×32 (thân cao 26–30 px ≈ 1,7 m, tỉ lệ chibi kiểu Momodora). Heo trưởng thành 24×20, heo non 12×10, heo đang lớn 18×14, heo già 24×20 + dấu hiệu tuổi. Thú cưng 16–24 px. Quái 24–48 px; Boss 64–128 px. Công trình theo footprint 32/64/96 px, được cao hơn footprint. Icon 16×16 và 32×32. Preset import đổi tên thành PRE_Sprite_Pixel16.
| Tốc độ khung hình animation | 8–12 fps cho đi bộ/idle; 12–16 fps cho đòn đánh; dùng thời lượng khung không đều (hold frame) để tạo cảm giác mượt kiểu Momodora. |
Tốc độ khung hình animation
8–12 fps cho đi bộ/idle; 12–16 fps cho đòn đánh; dùng thời lượng khung không đều (hold frame) để tạo cảm giác mượt kiểu Momodora.
| Nguồn file | Vẽ bằng Aseprite (hoặc LibreSprite/Pixelorama). Lưu .aseprite ở art-source/ và để Unity 2D Aseprite Importer nhập trực tiếp; tag animation trong Aseprite đặt đúng State (Idle, Walk…). |
Nguồn file
Vẽ bằng Aseprite (hoặc LibreSprite/Pixelorama). Lưu .aseprite ở art-source/ và để Unity 2D Aseprite Importer nhập trực tiếp; tag animation trong Aseprite đặt đúng State (Idle, Walk…).
5.2. Bộ animation tối thiểu (MUST)
Nhân vật người chơi (An, Khoa): Idle, Walk, Run, Attack (3 đòn combo), Dodge, Interact, Hurt, Death × 4 hướng.
NPC: Idle, Walk, Action (làm nghề) × 4 hướng; NPC chiến đấu thêm Attack, Hurt, Death.
Heo: Idle, Walk, Eat, Flee, Sleep × 4 giai đoạn tuổi; dùng chung rig cơ bản của B, mỗi gen thêm dấu hiệu riêng (màu lông, đốm, sừng nhỏ, phát sáng…).
Thú cưng: Idle, Walk, Action, Alert.
Quái: Move, Telegraph (báo trước đòn), Attack, Hit, Death. Boss thêm mỗi pha một Telegraph riêng.
Công trình: Idle, Active (bắn/hoạt động), Damaged, Destroyed, Building (đang xây).
5.3. Tham chiếu Momodora: Moonlit Farewell – học gì, không làm gì
Momodora: Moonlit Farewell (Bombservice, 2024, làm bằng Unity) được chọn làm mục tiêu cảm giác cho art và UI/UX, không phải mục tiêu bắt buộc. Lưu ý: Momodora là game đi cảnh nhìn ngang, còn Pig Tycoon nhìn từ trên xuống 3/4 – vì vậy chỉ học tinh thần, không sao chép bố cục.
| Yếu tố | Điều nên học | Cách áp dụng vào Pig Tycoon |
Yếu tố
Điều nên học
Cách áp dụng vào Pig Tycoon
| Bảng màu & không khí | Đêm trăng xanh tím lạnh, điểm nhấn ánh đèn lồng ấm; ban ngày dịu, ít bão hòa. | Hai bảng LUT ngày/đêm bằng URP 2D Light; đêm dùng Global Light xanh tím + Point Light ấm ở đèn, máng ăn, công trình. |
Bảng màu & không khí
Đêm trăng xanh tím lạnh, điểm nhấn ánh đèn lồng ấm; ban ngày dịu, ít bão hòa.
Hai bảng LUT ngày/đêm bằng URP 2D Light; đêm dùng Global Light xanh tím + Point Light ấm ở đèn, máng ăn, công trình.
| Nhân vật | Tỉ lệ chibi, silhouette rõ, chuyển động mượt nhiều khung, quần áo có chi tiết lay động. | An/Khoa 32×32 có tóc/áo chuyển động phụ (secondary motion) ở Run và Attack. |
Nhân vật
Tỉ lệ chibi, silhouette rõ, chuyển động mượt nhiều khung, quần áo có chi tiết lay động.
An/Khoa 32×32 có tóc/áo chuyển động phụ (secondary motion) ở Run và Attack.
| Cảm giác đánh | Hit-stop ngắn, nháy trắng khi trúng đòn, telegraph rõ ràng trước đòn quái. | Hit-stop 40–60 ms, flash shader, rung camera nhẹ (Cinemachine Impulse), vòng đỏ báo vùng đòn quái. |
Cảm giác đánh
Hit-stop ngắn, nháy trắng khi trúng đòn, telegraph rõ ràng trước đòn quái.
Hit-stop 40–60 ms, flash shader, rung camera nhẹ (Cinemachine Impulse), vòng đỏ báo vùng đòn quái.
| HUD | Tối giản: thanh máu/năng lượng mảnh ở góc trên trái, tiền tệ nhỏ gọn. | Góc trên trái: HP, Stamina 2 lớp, Nộ Khí; góc trên phải: đồng hồ, buổi, thời tiết, vàng; góc dưới: thanh võ kỹ 3 ô. |
HUD
Tối giản: thanh máu/năng lượng mảnh ở góc trên trái, tiền tệ nhỏ gọn.
Góc trên trái: HP, Stamina 2 lớp, Nộ Khí; góc trên phải: đồng hồ, buổi, thời tiết, vàng; góc dưới: thanh võ kỹ 3 ô.
| Menu | Lưới thẻ/biểu tượng rõ ràng (kiểu màn hình Sigil), khung viền trang trí mảnh, font pixel dễ đọc. | Màn hình trang bị 6 ô, Phù Văn/Mệnh Ấn, Giấy Chuyên Biệt dạng lưới thẻ; chợ đêm dạng lưới ô hàng có biểu tượng “món muốn nhận”. |
Menu
Lưới thẻ/biểu tượng rõ ràng (kiểu màn hình Sigil), khung viền trang trí mảnh, font pixel dễ đọc.
Màn hình trang bị 6 ô, Phù Văn/Mệnh Ấn, Giấy Chuyên Biệt dạng lưới thẻ; chợ đêm dạng lưới ô hàng có biểu tượng “món muốn nhận”.
| Hội thoại | Hộp thoại có chân dung, nhịp chữ chạy, ít chữ mỗi trang. | Chân dung 64×64 cho An, Khoa và NPC chính; tối đa 3 dòng mỗi trang; giọng kể của Khoa theo cốt truyện. |
Hội thoại
Hộp thoại có chân dung, nhịp chữ chạy, ít chữ mỗi trang.
Chân dung 64×64 cho An, Khoa và NPC chính; tối đa 3 dòng mỗi trang; giọng kể của Khoa theo cốt truyện.
| Không làm | Không lấy, tô lại hay chỉnh sửa sprite/âm thanh/font của Momodora (kể cả từ các trang lưu trữ sprite). Không dùng tên, nhân vật, biểu tượng của họ. Tham chiếu chỉ lưu dạng ảnh chụp màn hình trong moodboard nội bộ, không đưa vào build. |
Không làm
Không lấy, tô lại hay chỉnh sửa sprite/âm thanh/font của Momodora (kể cả từ các trang lưu trữ sprite). Không dùng tên, nhân vật, biểu tượng của họ. Tham chiếu chỉ lưu dạng ảnh chụp màn hình trong moodboard nội bộ, không đưa vào build.
5.4. Nguyên tắc UI/UX
Đọc được trong 1 giây: trạng thái đàn (mật độ, Sinh Cảnh, Áp Lực Sự Kiện) luôn có biểu tượng màu + số; mọi cảnh báo có cả màu và hình (hỗ trợ mù màu).
Thông tin là hàng hóa: dự báo đợt quái hiển thị theo cấp tình báo 0–5 (mức càng thấp càng mơ hồ) – không bao giờ lộ thông tin người chơi chưa mua.
Tối đa 2 cú nhấp cho thao tác thường ngày (cho ăn, bán, sửa, nạp trước). Có phím tắt và tay cầm.
Font pixel hỗ trợ đầy đủ dấu tiếng Việt (kiểm tra ở P0); không dùng chữ hard-code, tất cả qua Localization (vi mặc định, en STRETCH).
UI Toolkit cho menu/HUD; uGUI chỉ dùng cho thanh máu gắn vật thể (world-space).
6. Phân công 4 map chính và ma trận thiết kế asset
Việc được chia theo hai trục. Trục Map: mỗi người sở hữu trọn một map chính trong Unity, gồm bố cục, tilemap, ánh sáng, công trình, NPC, thú cưng, gen heo, quái gắn với map đó, scene và việc tích hợp. Trục Hệ thống (mục 2): logic dùng chung viết trong Core/Contracts. Khi map và hệ thống thuộc hai người khác nhau, owner map dựng không gian, art và điểm tương tác, còn owner hệ thống cung cấp logic qua Contracts. Toàn bộ ma trận dưới đây là MUST. Người có tên phải tự vẽ (hoặc chỉ đạo và hoàn thiện) asset, tạo animation, import đúng preset, tạo prefab và chạy thử trong Unity. Agent chỉ hỗ trợ script, controller, data, không vẽ thay. Mọi asset phải qua review style của B.
6.1. Tổng quan 4 map và cách chia đều
| Khu vực | Owner | Scene | Kích thước | Vai trò trong vòng lặp game | Khối lượng nội dung |
Khu vực
Owner
Scene
Kích thước
Vai trò trong vòng lặp game
Khối lượng nội dung
| Khu Lõi Vận Hành (map riêng phía sau trại) + Làng NPC (ngoại vi) | A | Farm_Core + Hub_Village | Lõi 60×40 m (6×4 Ô Đất) phía Bắc trại + Đoạn Chuyển Map ≈ 20×8 m; làng đề xuất 40×30 m | Ngủ/lưu, kho, xây dựng, nghiên cứu, cường hóa, tình báo; NPC và cốt truyện | 7 phòng chức năng, 8 công trình, 3 NPC + Bà Sáu, 2 thú cưng, 3 gen, 3 quái + 1 Boss, 2 cảnh sự kiện |
Khu Lõi Vận Hành (map riêng phía sau trại) + Làng NPC (ngoại vi)
A
Farm_Core + Hub_Village
Lõi 60×40 m (6×4 Ô Đất) phía Bắc trại + Đoạn Chuyển Map ≈ 20×8 m; làng đề xuất 40×30 m
Ngủ/lưu, kho, xây dựng, nghiên cứu, cường hóa, tình báo; NPC và cốt truyện
7 phòng chức năng, 8 công trình, 3 NPC + Bà Sáu, 2 thú cưng, 3 gen, 3 quái + 1 Boss, 2 cảnh sự kiện
| Đồng Cỏ Heo Thả Rông + dải phòng thủ + vành ngoài | B | Farm_Main | Trong rào 120×90 m (12 Ô Đất), mở rộng tới 220×170 m (~40 Ô Đất), cộng vành ngoài 30–40 m (GDD 3.6) | Chăm đàn. Nơi diễn ra mọi đợt quái, Cổng sự kiện và nơi Bí nhân xuất hiện | Hạ tầng trại, 8 công trình, 3 NPC, 2 thú cưng, rig heo + 3 gen, 4 quái, 4 cảnh sự kiện |
Đồng Cỏ Heo Thả Rông + dải phòng thủ + vành ngoài
B
Farm_Main
Trong rào 120×90 m (12 Ô Đất), mở rộng tới 220×170 m (~40 Ô Đất), cộng vành ngoài 30–40 m (GDD 3.6)
Chăm đàn. Nơi diễn ra mọi đợt quái, Cổng sự kiện và nơi Bí nhân xuất hiện
Hạ tầng trại, 8 công trình, 3 NPC, 2 thú cưng, rig heo + 3 gen, 4 quái, 4 cảnh sự kiện
| Chợ Thị Trấn (ngoại vi) | C | Market_Town | Đề xuất 50×40 m × 2 lớp ngày/đêm | Bán/mua, kiểm dịch, giám định, chợ đêm, đấu giá, thuê lính | 8 công trình, 3 NPC + 4 NPC chức năng chợ, 1 thú cưng, 3 gen, 4 quái + 1 Boss, 4 cảnh sự kiện |
Chợ Thị Trấn (ngoại vi)
C
Market_Town
Đề xuất 50×40 m × 2 lớp ngày/đêm
Bán/mua, kiểm dịch, giám định, chợ đêm, đấu giá, thuê lính
8 công trình, 3 NPC + 4 NPC chức năng chợ, 1 thú cưng, 3 gen, 4 quái + 1 Boss, 4 cảnh sự kiện
| Rừng Nguyên Sinh (ngoại vi) + khung sự kiện | D | Forest_Primeval + khung Event_* | Đề xuất 80×80 m | Khám phá, thu thập; khung chung cho Cổng/Sương mù | 7 công trình, 3 NPC + Mận + Bí nhân, 2 thú cưng, 3 gen, 3 quái + 1 Boss, 4 cảnh sự kiện + khung sự kiện |
Rừng Nguyên Sinh (ngoại vi) + khung sự kiện
D
Forest_Primeval + khung Event_*
Đề xuất 80×80 m
Khám phá, thu thập; khung chung cho Cổng/Sương mù
7 công trình, 3 NPC + Mận + Bí nhân, 2 thú cưng, 3 gen, 3 quái + 1 Boss, 4 cảnh sự kiện + khung sự kiện
Nguyên tắc chia map: (1) Khu Lõi Vận Hành của A là một map riêng (Farm_Core) nằm phía sau trang trại, nối với Farm_Main bằng một Đoạn Chuyển Map. GDD 3.6 đặt lõi vận hành trong rào. Để giữ đúng luật, ranh giới trại được tính gộp cả Farm_Main và Farm_Core, nên thứ tự mục tiêu của quái (Đàn → Kho → Hạ tầng → Nhà chính) vẫn còn hiệu lực. Nơi Trú Đàn vẫn ở Farm_Main, sát Đoạn Chuyển Map. Thay đổi này ghi vào ADR-0004. (2) Chợ, làng NPC và rừng là ngoại vi, chỉ để chuyển cảnh, quái không tấn công. Mọi công trình phòng thủ chỉ đặt trong Farm_Main hoặc Farm_Core. “Sở hữu” một công trình hay một con quái nghĩa là người đó vẽ, làm animation và prefab. Cân tải: mỗi người có 3 NPC chính, 3 dòng gen, 7–8 công trình, 3–5 quái và 2–4 cảnh sự kiện. A có thêm 7 phòng chức năng và Đoạn Chuyển Map nên chỉ nhận 2 cảnh sự kiện. C phải làm 2 lớp ngày/đêm nên chỉ nhận 1 thú cưng.
6.2. Map A – Khu Lõi Vận Hành (Farm_Core, phía sau trang trại) và Làng NPC (Hub_Village)
| Hạng mục | Nội dung |
Hạng mục
Nội dung
| Chủ đề và cảm giác | Một map riêng nằm phía sau trang trại: cụm nhà gỗ, xưởng và phòng máy. Ban đêm có đèn lồng và ánh màn hình radar. Đây là “bộ não” của trại. Làng NPC là khu nhỏ bên ngoài, yên bình, dùng cho cốt truyện. |
Chủ đề và cảm giác
Một map riêng nằm phía sau trang trại: cụm nhà gỗ, xưởng và phòng máy. Ban đêm có đèn lồng và ánh màn hình radar. Đây là “bộ não” của trại. Làng NPC là khu nhỏ bên ngoài, yên bình, dùng cho cốt truyện.
| Vị trí và Đoạn Chuyển Map | Farm_Core nằm ở phía Bắc Farm_Main, sát rào sau. Nối với Farm_Main bằng Đoạn Chuyển Map: hành lang khoảng 20×8 m có Cổng Sau của trại (rộng ≥6 m, kiểu Cổng Gia Cố), đèn và một khoảng trống đặt được bẫy. Người chơi đi vào vùng kích hoạt ở cuối hành lang thì màn hình mờ đi khoảng 0,5 giây, nạp map còn lại và đặt nhân vật ở điểm xuất hiện bên kia. Chuyển map không quá 1,5 giây. Trong đợt quái, Đoạn Chuyển Map là tuyến phòng thủ cuối: quái muốn tới Kho/Nhà chính phải đi qua hành lang này. Quái đi tới hành lang thì được đưa sang Farm_Core theo cùng cơ chế. Heo không được đi qua (Nơi Trú Đàn ở Farm_Main). |
Vị trí và Đoạn Chuyển Map
Farm_Core nằm ở phía Bắc Farm_Main, sát rào sau. Nối với Farm_Main bằng Đoạn Chuyển Map: hành lang khoảng 20×8 m có Cổng Sau của trại (rộng ≥6 m, kiểu Cổng Gia Cố), đèn và một khoảng trống đặt được bẫy. Người chơi đi vào vùng kích hoạt ở cuối hành lang thì màn hình mờ đi khoảng 0,5 giây, nạp map còn lại và đặt nhân vật ở điểm xuất hiện bên kia. Chuyển map không quá 1,5 giây. Trong đợt quái, Đoạn Chuyển Map là tuyến phòng thủ cuối: quái muốn tới Kho/Nhà chính phải đi qua hành lang này. Quái đi tới hành lang thì được đưa sang Farm_Core theo cùng cơ chế. Heo không được đi qua (Nơi Trú Đàn ở Farm_Main).
| 7 phòng chức năng (trong Farm_Core) | ① Nhà chính: giường ngủ/lưu, chọn An/Khoa đầu game, Bảng Điều Khiển kích Bạch Vân từ xa (trễ +3 giây, GDD 13.1). ② Kho: sức chứa, chống hỏng cám (Thủ Kho, Mèo Kho). ③ Phòng Xây Dựng cấp 1–3: học bản vẽ, ô thi công, sửa chữa, chế tạo đạn (GDD 13.2, 16). ④ Phòng Nghiên Cứu: mở ở Cấp trại 8–10, cần Nhà Nghiên Cứu Gen. Làm giám định gen Hiếm→Dị Biến (18–48 giờ), nghiên cứu Thuần Hóa Huyết Thống cho Xích Mao, chế tạo Cám Hạ Hỏa, nghiên cứu Hư Thể để lấy Tinh Hoa Dị Biến, nghiên cứu thuốc chữa Hư Mạch (GDD 3.5, 5.2, 5.4, 6.4, 10.7). ⑤ Xưởng Cường Hóa (tên đề xuất, GDD chưa ghi địa điểm): gia cường Giấy Chuyên Biệt +1→+3 bằng Đá Cường Hóa, rã giấy lấy nguyên liệu, Bàn Khắc Phù Văn lên trang bị (bản vẽ Cao cấp + Phòng Xây Dựng cấp 2), khắc Mệnh Ấn lên nhân vật (GDD 10.6–10.7, 12.4). ⑥ Lều Quân Y (GDD 3.5, mở Cấp 3–4; ⚠ GDD chưa mô tả cơ chế, cần chốt). ⑦ Khu thông tin: Bàn Tình Báo, Trạm Khí Tượng, chân Tháp Canh/Radar. |
7 phòng chức năng (trong Farm_Core)
① Nhà chính: giường ngủ/lưu, chọn An/Khoa đầu game, Bảng Điều Khiển kích Bạch Vân từ xa (trễ +3 giây, GDD 13.1). ② Kho: sức chứa, chống hỏng cám (Thủ Kho, Mèo Kho). ③ Phòng Xây Dựng cấp 1–3: học bản vẽ, ô thi công, sửa chữa, chế tạo đạn (GDD 13.2, 16). ④ Phòng Nghiên Cứu: mở ở Cấp trại 8–10, cần Nhà Nghiên Cứu Gen. Làm giám định gen Hiếm→Dị Biến (18–48 giờ), nghiên cứu Thuần Hóa Huyết Thống cho Xích Mao, chế tạo Cám Hạ Hỏa, nghiên cứu Hư Thể để lấy Tinh Hoa Dị Biến, nghiên cứu thuốc chữa Hư Mạch (GDD 3.5, 5.2, 5.4, 6.4, 10.7). ⑤ Xưởng Cường Hóa (tên đề xuất, GDD chưa ghi địa điểm): gia cường Giấy Chuyên Biệt +1→+3 bằng Đá Cường Hóa, rã giấy lấy nguyên liệu, Bàn Khắc Phù Văn lên trang bị (bản vẽ Cao cấp + Phòng Xây Dựng cấp 2), khắc Mệnh Ấn lên nhân vật (GDD 10.6–10.7, 12.4). ⑥ Lều Quân Y (GDD 3.5, mở Cấp 3–4; ⚠ GDD chưa mô tả cơ chế, cần chốt). ⑦ Khu thông tin: Bàn Tình Báo, Trạm Khí Tượng, chân Tháp Canh/Radar.
| Làng NPC (Hub_Village) | Nhà Bà Sáu, nhà ở của NPC ngoài giờ làm, bảng tin làng (hiển thị tin Quạ Tin, tin đồn). Cổng về trại. |
Làng NPC (Hub_Village)
Nhà Bà Sáu, nhà ở của NPC ngoài giờ làm, bảng tin làng (hiển thị tin Quạ Tin, tin đồn). Cổng về trại.
| Công trình thiết kế (8) | Thông tin: Tháp Canh Quan Sát • Radar Uy Hiếp • Trạm Khí Tượng • Bàn Tình Báo • Bạch Vân · Thiên Nhãn Vọng Đài. Hỗ trợ: Máy Phát Điện (3 bậc) • Cột Thu Lôi • Trạm Hồi Sức (3 cấp). |
Công trình thiết kế (8)
Thông tin: Tháp Canh Quan Sát • Radar Uy Hiếp • Trạm Khí Tượng • Bàn Tình Báo • Bạch Vân · Thiên Nhãn Vọng Đài. Hỗ trợ: Máy Phát Điện (3 bậc) • Cột Thu Lôi • Trạm Hồi Sức (3 cấp).
| Hạ tầng phụ (ngoài 31 công trình) | Cột điện, dây nổi/dây ngầm (giá ×3), Tụ Điện (bản vẽ Tinh, GDD 15), Bàn Khắc, Bảng Điều Khiển Bạch Vân. |
Hạ tầng phụ (ngoài 31 công trình)
Cột điện, dây nổi/dây ngầm (giá ×3), Tụ Điện (bản vẽ Tinh, GDD 15), Bàn Khắc, Bảng Điều Khiển Bạch Vân.
| NPC | Quản Lý Ca • Thủ Kho • Kỹ Sư Xây Dựng (trực Phòng Xây Dựng, hiệu suất 40/70/100%) • NPC cốt truyện Bà Sáu |
NPC
Quản Lý Ca • Thủ Kho • Kỹ Sư Xây Dựng (trực Phòng Xây Dựng, hiệu suất 40/70/100%) • NPC cốt truyện Bà Sáu
| Thú cưng (2) | Mèo Kho • Ngỗng Báo Động |
Thú cưng (2)
Mèo Kho • Ngỗng Báo Động
| Gen heo (3) | Hồng Điền • Thổ Trầm • Kim Thọ (vẽ trên rig heo chung của B) |
Gen heo (3)
Hồng Điền • Thổ Trầm • Kim Thọ (vẽ trên rig heo chung của B)
| Quái/Boss (art + animation; xuất hiện ở Farm_Main) | UFO Trinh Sát • UFO Thu Hoạch • UFO Pháo Hạm • Boss Mẫu Hạm UFO (chu kỳ 10, 25, 40…) |
Quái/Boss (art + animation; xuất hiện ở Farm_Main)
UFO Trinh Sát • UFO Thu Hoạch • UFO Pháo Hạm • Boss Mẫu Hạm UFO (chu kỳ 10, 25, 40…)
| Cảnh sự kiện (2) | Kho Lạnh Bỏ Quên (Xanh/Cổng) • Giám Định Vô Danh (Vàng/Cổng) |
Cảnh sự kiện (2)
Kho Lạnh Bỏ Quên (Xanh/Cổng) • Giám Định Vô Danh (Vàng/Cổng)
| Hệ thống gắn với khu | Thời gian/ngủ, Save, lưới điện, kho/inventory, Cấp trại (A tự làm). Bản vẽ/thi công và Phù Văn/Mệnh Ấn: logic của C. Giám định/nghiên cứu, gia cường giấy và tình báo: logic của D. A dựng không gian, tương tác và điểm mở UI. |
Hệ thống gắn với khu
Thời gian/ngủ, Save, lưới điện, kho/inventory, Cấp trại (A tự làm). Bản vẽ/thi công và Phù Văn/Mệnh Ấn: logic của C. Giám định/nghiên cứu, gia cường giấy và tình báo: logic của D. A dựng không gian, tương tác và điểm mở UI.
| Đầu ra MUST | Farm_Core là map riêng, đủ 7 phòng, mỗi phòng mở đúng màn hình chức năng. Đoạn Chuyển Map chạy hai chiều cho người chơi và quái, không cho heo đi qua, chuyển map ≤1,5 giây, state Core không bị reset. Phòng Nghiên Cứu và Xưởng Cường Hóa bị khóa đúng điều kiện mở (bảng 6.2.1). Hub_Village đi lại được. 3 NPC làm theo lịch, 2 thú cưng, 8 công trình có prefab đủ trạng thái. |
Đầu ra MUST
Farm_Core là map riêng, đủ 7 phòng, mỗi phòng mở đúng màn hình chức năng. Đoạn Chuyển Map chạy hai chiều cho người chơi và quái, không cho heo đi qua, chuyển map ≤1,5 giây, state Core không bị reset. Phòng Nghiên Cứu và Xưởng Cường Hóa bị khóa đúng điều kiện mở (bảng 6.2.1). Hub_Village đi lại được. 3 NPC làm theo lịch, 2 thú cưng, 8 công trình có prefab đủ trạng thái.
| STRETCH | Nội thất nhà chính, animation máy móc trong Phòng Nghiên Cứu, hiệu ứng ánh sáng khi cường hóa thành công/thất bại, NPC dân làng. |
STRETCH
Nội thất nhà chính, animation máy móc trong Phòng Nghiên Cứu, hiệu ứng ánh sáng khi cường hóa thành công/thất bại, NPC dân làng.
6.2.1. Phòng Nghiên Cứu và Xưởng Cường Hóa – chức năng, điều kiện mở, luồng dữ liệu
A dựng không gian, NPC trực, điểm tương tác và mở UI. Luật và công thức nằm trong Core, do owner hệ thống ghi trong bảng. “Xưởng Cường Hóa” là tên đặt cho khu gom các thao tác gia cường và khắc mà GDD chưa ghi địa điểm (ghi vào ADR-0002). Mọi con số lấy từ GDD.
| Bàn / chức năng | Đối tượng | Điều kiện mở | Đầu vào → Đầu ra | Luật chính (GDD) | Logic |
Bàn / chức năng
Đối tượng
Điều kiện mở
Đầu vào → Đầu ra
Luật chính (GDD)
Logic
| PHÒNG NGHIÊN CỨU |
PHÒNG NGHIÊN CỨU
| Bàn Giải Mã (giám định gen) | Heo gen “?” (Hiếm → Dị Biến) | Phòng nghiên cứu mở ở Cấp trại 8–10; có Nhà Nghiên Cứu Gen trực; Huyền Thoại/Dị Biến cần đặc tính “Giải Mã Sâu” | Heo + vàng + thuốc thử + Stamina → dòng gen được định danh vĩnh viễn, hoặc thất bại | 95/85/65/50%; 700G+1 → 9.000G+6; 18–48 giờ; khóa 12 giờ nếu thành công, 24 giờ nếu thất bại (5.4) | D |
Bàn Giải Mã (giám định gen)
Heo gen “?” (Hiếm → Dị Biến)
Phòng nghiên cứu mở ở Cấp trại 8–10; có Nhà Nghiên Cứu Gen trực; Huyền Thoại/Dị Biến cần đặc tính “Giải Mã Sâu”
Heo + vàng + thuốc thử + Stamina → dòng gen được định danh vĩnh viễn, hoặc thất bại
95/85/65/50%; 700G+1 → 9.000G+6; 18–48 giờ; khóa 12 giờ nếu thành công, 24 giờ nếu thất bại (5.4)
D
| Thuần Hóa Huyết Thống | Dòng Xích Mao đã định danh | Đã định danh Xích Mao | 1.200G, 24 giờ → mở công thức Cám Hạ Hỏa và cách giảm mặt trái 70% | 5.2 | D (B áp lên đàn) |
Thuần Hóa Huyết Thống
Dòng Xích Mao đã định danh
Đã định danh Xích Mao
1.200G, 24 giờ → mở công thức Cám Hạ Hỏa và cách giảm mặt trái 70%
5.2
D (B áp lên đàn)
| Bàn Chế Tạo | Cám Hạ Hỏa (II) | Đã xong Thuần Hóa | Nguyên liệu → vật phẩm trong kho | 12.5 | D |
Bàn Chế Tạo
Cám Hạ Hỏa (II)
Đã xong Thuần Hóa
Nguyên liệu → vật phẩm trong kho
12.5
D
| Nghiên cứu Hư Thể / Hư Mạch | Heo Hư Thể, heo nhiễm Hư Mạch | Có heo tương ứng | Heo → Tinh Hoa Dị Biến (IV) / thuốc chữa Hư Mạch | 6.4, 10.6 | D (B cập nhật sức khỏe) |
Nghiên cứu Hư Thể / Hư Mạch
Heo Hư Thể, heo nhiễm Hư Mạch
Có heo tương ứng
Heo → Tinh Hoa Dị Biến (IV) / thuốc chữa Hư Mạch
6.4, 10.6
D (B cập nhật sức khỏe)
| XƯỞNG CƯỜNG HÓA |
XƯỞNG CƯỜNG HÓA
| Bàn Gia Cường Giấy | Giấy Chuyên Biệt Lam/Vàng/Tím/Đỏ | Đã qua Cổng Giữa Game và có giấy | Giấy + nguyên liệu + vàng + Đá Cường Hóa → giấy +1/+2/+3 (thất bại thì mất nguyên liệu, giấy giữ nguyên) | +1: 100% • +2: 65% + 6%/đá, tối đa 95% • +3: 40% + 5%/đá, tối đa 80%; thiếu nguyên liệu × (nộp/yêu cầu)^1,5, tối thiểu 10% (10.6–10.7) | D |
Bàn Gia Cường Giấy
Giấy Chuyên Biệt Lam/Vàng/Tím/Đỏ
Đã qua Cổng Giữa Game và có giấy
Giấy + nguyên liệu + vàng + Đá Cường Hóa → giấy +1/+2/+3 (thất bại thì mất nguyên liệu, giấy giữ nguyên)
+1: 100% • +2: 65% + 6%/đá, tối đa 95% • +3: 40% + 5%/đá, tối đa 80%; thiếu nguyên liệu × (nộp/yêu cầu)^1,5, tối thiểu 10% (10.6–10.7)
D
| Bàn Rã Giấy | Giấy Lam/Vàng/Tím | Có giấy | Lam → 2 Tơ Vàng • Vàng → 2 Bột Tử Tinh • Tím → 1 Huyết Triện | 10.7 | D |
Bàn Rã Giấy
Giấy Lam/Vàng/Tím
Có giấy
Lam → 2 Tơ Vàng • Vàng → 2 Bột Tử Tinh • Tím → 1 Huyết Triện
10.7
D
| Bàn Dùng Giấy (mở khóa) | Đặc tính chuyên biệt của nhân vật / chuyên môn của NPC, thú cưng | Có giấy | Giấy → số lượt mở khóa lần lượt 1→6; lộ điều kiện; tên ẩn đến khi xong đủ 6 khóa | 10.4–10.5 | D (A cấp dữ liệu nhân vật) |
Bàn Dùng Giấy (mở khóa)
Đặc tính chuyên biệt của nhân vật / chuyên môn của NPC, thú cưng
Có giấy
Giấy → số lượt mở khóa lần lượt 1→6; lộ điều kiện; tên ẩn đến khi xong đủ 6 khóa
10.4–10.5
D (A cấp dữ liệu nhân vật)
| Bàn Khắc – Phù Văn | Trang bị bậc II trở lên, 1 Phù Văn/món | Qua Cổng Giữa Game + ≥12 đợt lớn + Cấp nhân vật ≥15 + Phòng Xây Dựng cấp 2 + bản vẽ Bàn Khắc (Cao cấp) | Trang bị + Mực Phù + vàng → trang bị có Phù Văn | Tỉ lệ 100/80/60% theo bậc I–III (12.4) | C |
Bàn Khắc – Phù Văn
Trang bị bậc II trở lên, 1 Phù Văn/món
Qua Cổng Giữa Game + ≥12 đợt lớn + Cấp nhân vật ≥15 + Phòng Xây Dựng cấp 2 + bản vẽ Bàn Khắc (Cao cấp)
Trang bị + Mực Phù + vàng → trang bị có Phù Văn
Tỉ lệ 100/80/60% theo bậc I–III (12.4)
C
| Bàn Khắc – Mệnh Ấn | Nhân vật người chơi (không cho NPC) | Đã mở Phù Văn ≥5 chu kỳ + ≥18 đợt lớn + Cấp ≥22 + đã hạ 2 Boss + xong 1 sự kiện Đỏ | Huyết Triện + Tinh Hoa Dị Biến → Mệnh Ấn (tối đa 3); trừ vĩnh viễn 10 HP tối đa mỗi lần | 12.4 | C |
Bàn Khắc – Mệnh Ấn
Nhân vật người chơi (không cho NPC)
Đã mở Phù Văn ≥5 chu kỳ + ≥18 đợt lớn + Cấp ≥22 + đã hạ 2 Boss + xong 1 sự kiện Đỏ
Huyết Triện + Tinh Hoa Dị Biến → Mệnh Ấn (tối đa 3); trừ vĩnh viễn 10 HP tối đa mỗi lần
12.4
C
Luồng dữ liệu (qua Contracts, không gọi chéo trực tiếp): người chơi bấm vào bàn → UI của A gọi IResearchService hoặc IEnhanceService → service đọc điều kiện mở từ IFarmLevel (A), IGateProgress (Cổng Giữa Game, A) và IPlayerStats (C) → trừ vàng qua IEconomyLedger (D) và trừ nguyên liệu qua IInventory (A) → tung tỉ lệ bằng RNG có seed trong Core → ghi kết quả vào Catalog (gen đã định danh), Inventory (giấy, vật phẩm) hoặc PlayerStats (Phù Văn, Mệnh Ấn) → phát sự kiện GeneIdentified / PaperEnhanced / RuneEngraved / SealEngraved lên IEventBus → HUD, Save và tình báo cập nhật. Nghiên cứu có thời gian (18–48 giờ) được lưu thành hàng chờ có giờ kết thúc trong Save, đồng hồ tiếp tục chạy khi người chơi rời map.
6.3. Map B – Đồng Cỏ Heo Thả Rông (Farm_Main) – Owner: B
| Hạng mục | Nội dung |
Hạng mục
Nội dung
| Chủ đề và cảm giác | Đồng cỏ rộng, rào gỗ/thép, đàn heo đi lang thang tự do. Là cảnh mô phỏng chính và là chiến trường duy nhất của đợt quái. |
Chủ đề và cảm giác
Đồng cỏ rộng, rào gỗ/thép, đàn heo đi lang thang tự do. Là cảnh mô phỏng chính và là chiến trường duy nhất của đợt quái.
| Phân vùng theo GDD 3.6 | Ranh giới trại (một đa giác khép kín) • Đồng cỏ ≥35% diện tích, không có ngõ cụt sâu >15 m • Nơi Trú Đàn (≥2 lối vào rộng ≥6 m) • Vành dịch vụ: Khu Cách Ly do người chơi chỉ định (≥2 Ô Đất), Khu Xử Lý 4/8 ô tập kết, khu kỹ thuật điện • Dải phòng thủ rộng 12–20 m dọc phía trong rào (chiếm 20–25% diện tích) • Vành ngoài 30–40 m: lối mòn bao quanh, 8–12 điểm vào chia 4 khu hướng, ≥2 điểm vượt mỗi sông/vách • Chỗ dành sẵn cho Farm_Core (của A). |
Phân vùng theo GDD 3.6
Ranh giới trại (một đa giác khép kín) • Đồng cỏ ≥35% diện tích, không có ngõ cụt sâu >15 m • Nơi Trú Đàn (≥2 lối vào rộng ≥6 m) • Vành dịch vụ: Khu Cách Ly do người chơi chỉ định (≥2 Ô Đất), Khu Xử Lý 4/8 ô tập kết, khu kỹ thuật điện • Dải phòng thủ rộng 12–20 m dọc phía trong rào (chiếm 20–25% diện tích) • Vành ngoài 30–40 m: lối mòn bao quanh, 8–12 điểm vào chia 4 khu hướng, ≥2 điểm vượt mỗi sông/vách • Chỗ dành sẵn cho Farm_Core (của A).
| Tileset và ánh sáng | Map đã có của B, chuẩn hóa về 16 PPU. Rule Tile cho cỏ, bùn, nước. Tán cây mờ đi khi có đơn vị phía sau. Ban đêm dùng ánh trăng và Đèn Pha. |
Tileset và ánh sáng
Map đã có của B, chuẩn hóa về 16 PPU. Rule Tile cho cỏ, bùn, nước. Tán cây mờ đi khi có đơn vị phía sau. Ban đêm dùng ánh trăng và Đèn Pha.
| Hạ tầng trại (ngoài 31 công trình) | Máng ăn • Bồn nước • Mái trú (có sưởi) • Bãi bùn • Hệ thống thoát nước • Rào tạm cách ly • Ô tập kết xác • Mái Che Tạm. Kèm lớp phủ vùng phục vụ 30 m, nguồn mát 25 m và lưới đặt xanh/vàng/đỏ. |
Hạ tầng trại (ngoài 31 công trình)
Máng ăn • Bồn nước • Mái trú (có sưởi) • Bãi bùn • Hệ thống thoát nước • Rào tạm cách ly • Ô tập kết xác • Mái Che Tạm. Kèm lớp phủ vùng phục vụ 30 m, nguồn mát 25 m và lưới đặt xanh/vàng/đỏ.
| Công trình thiết kế (8) | Phòng thủ: Hàng Rào gỗ/thép • Cổng Gia Cố • Ụ Bao Cát. Bẫy: Bàn Chông. Hỗ trợ: Đèn Pha Công Nghiệp • Bơm Làm Mát. Triển khai: Chốt Canh Gác • Cờ Tập Kết. |
Công trình thiết kế (8)
Phòng thủ: Hàng Rào gỗ/thép • Cổng Gia Cố • Ụ Bao Cát. Bẫy: Bàn Chông. Hỗ trợ: Đèn Pha Công Nghiệp • Bơm Làm Mát. Triển khai: Chốt Canh Gác • Cờ Tập Kết.
| NPC | Bác Sĩ Thú Y • Nông Công • Vệ Binh Khiên |
NPC
Bác Sĩ Thú Y • Nông Công • Vệ Binh Khiên
| Thú cưng (2) | Chó Chăn Đàn • Chó Ngao Canh |
Thú cưng (2)
Chó Chăn Đàn • Chó Ngao Canh
| Gen heo | Rig heo dùng chung (MUST, phải xong trước mọi gene pack) • Lam Khê • Xích Mao • Thiết Bì |
Gen heo
Rig heo dùng chung (MUST, phải xong trước mọi gene pack) • Lam Khê • Xích Mao • Thiết Bì
| Quái (4) | Chuột Gặm Kho • Bầy Nhảy • Thú Bão (chỉ khi Giông Bão) • Kẻ Mang Dịch (tinh anh) |
Quái (4)
Chuột Gặm Kho • Bầy Nhảy • Thú Bão (chỉ khi Giông Bão) • Kẻ Mang Dịch (tinh anh)
| Điểm đặt cho hệ khác | Vị trí xuất hiện của Bí nhân (ngoài rào, trong 30 m) • ≥4 vùng ẩn hợp lệ cho Cổng • Vùng không spawn 25 m • Điểm vào của quái theo 4 khu hướng. |
Điểm đặt cho hệ khác
Vị trí xuất hiện của Bí nhân (ngoài rào, trong 30 m) • ≥4 vùng ẩn hợp lệ cho Cổng • Vùng không spawn 25 m • Điểm vào của quái theo 4 khu hướng.
| Cảnh sự kiện (4) | Vườn Mười Hai Quả (Xanh/Cổng) • Đồng Cỏ Không Bóng (Xanh/Sương) • Bí Nhân Mua Già (Vàng/Cổng) • Đổi Một Lấy Một (Vàng/Cổng) |
Cảnh sự kiện (4)
Vườn Mười Hai Quả (Xanh/Cổng) • Đồng Cỏ Không Bóng (Xanh/Sương) • Bí Nhân Mua Già (Vàng/Cổng) • Đổi Một Lấy Một (Vàng/Cổng)
| Đầu ra MUST | Đạt toàn bộ checklist bản đồ GDD 3.10 nhóm A–H. Ví dụ: 150 heo qua cổng 6 m không kẹt quá 5 giây; lệnh “Lùa về Nơi Trú” gom ≥90% đàn trong ≤2 giờ game; một đợt dùng 3 khu hướng thì không vị trí đơn lẻ nào chặn được >70% quái mặt đất. |
Đầu ra MUST
Đạt toàn bộ checklist bản đồ GDD 3.10 nhóm A–H. Ví dụ: 150 heo qua cổng 6 m không kẹt quá 5 giây; lệnh “Lùa về Nơi Trú” gom ≥90% đàn trong ≤2 giờ game; một đợt dùng 3 khu hướng thì không vị trí đơn lẻ nào chặn được >70% quái mặt đất.
| STRETCH | Biến thể theo mùa, bong bóng cảm xúc của heo, bóng mây trôi. |
STRETCH
Biến thể theo mùa, bong bóng cảm xúc của heo, bóng mây trôi.
6.4. Map C – Chợ Thị Trấn (Market_Town) – Owner: C
| Hạng mục | Nội dung |
Hạng mục
Nội dung
| Chủ đề và cảm giác | Ban ngày là thị trấn chợ nhộn nhịp. Ban đêm biến thành chợ đêm đèn lồng tím–cam, nhiều bí ẩn. Đây là map gần không khí Momodora nhất. Là ngoại vi nên không có combat. |
Chủ đề và cảm giác
Ban ngày là thị trấn chợ nhộn nhịp. Ban đêm biến thành chợ đêm đèn lồng tím–cam, nhiều bí ẩn. Đây là map gần không khí Momodora nhất. Là ngoại vi nên không có combat.
| Các khu vực | Ngày (06–18): dãy quầy bán heo/mua cám, vật liệu, Diesel, vật phẩm bậc I • Quầy kiểm dịch (60G) và sàng lọc (120G) • Quầy giám định Hiếm–Quý • Bảng hợp đồng giao heo • Nơi tuyển NPC cơ bản. Đêm (18–06): sân lưới ô hàng “món muốn nhận” • Sàn đấu giá • Quầy “Giám Định Cấp Cao” (12% số đêm) • Hội quán thuê 5 lính đánh thuê • Quầy tuyển NPC/thú cưng. Đường lớn phía Bắc dẫn về trại. Không đặt Bí nhân ở đây, vì GDD 21 quy định hắn xuất hiện ngoài rào trại. |
Các khu vực
Ngày (06–18): dãy quầy bán heo/mua cám, vật liệu, Diesel, vật phẩm bậc I • Quầy kiểm dịch (60G) và sàng lọc (120G) • Quầy giám định Hiếm–Quý • Bảng hợp đồng giao heo • Nơi tuyển NPC cơ bản. Đêm (18–06): sân lưới ô hàng “món muốn nhận” • Sàn đấu giá • Quầy “Giám Định Cấp Cao” (12% số đêm) • Hội quán thuê 5 lính đánh thuê • Quầy tuyển NPC/thú cưng. Đường lớn phía Bắc dẫn về trại. Không đặt Bí nhân ở đây, vì GDD 21 quy định hắn xuất hiện ngoài rào trại.
| Tileset và ánh sáng | TILE_Market_* (đá lát, mái vải, quầy gỗ). Hai bố cục là 2 lớp Tilemap bật/tắt theo giờ trong cùng một scene. |
Tileset và ánh sáng
TILE_Market_* (đá lát, mái vải, quầy gỗ). Hai bố cục là 2 lớp Tilemap bật/tắt theo giờ trong cùng một scene.
| Công trình thiết kế (8) | Tấn công: Nỏ Xuyên Vân (3 nhánh nâng cấp) • Tháp Hồ Quang • Tháp Phòng Không Skyhook • Đài Thiên Lôi • Bạch Vân · Cửu Tiêu Lôi Pháo. Phòng thủ: Máy Chiếu Khiên. Triển khai: Đèn Hiệu Đánh Chặn. Thông tin: Trạm Tiếp Sóng Chợ. |
Công trình thiết kế (8)
Tấn công: Nỏ Xuyên Vân (3 nhánh nâng cấp) • Tháp Hồ Quang • Tháp Phòng Không Skyhook • Đài Thiên Lôi • Bạch Vân · Cửu Tiêu Lôi Pháo. Phòng thủ: Máy Chiếu Khiên. Triển khai: Đèn Hiệu Đánh Chặn. Thông tin: Trạm Tiếp Sóng Chợ.
| NPC | Nhà Buôn • Xạ Thủ Nỏ • Kỹ Sư Chiến Trường. Cộng thêm 4 NPC chức năng chợ (người bán chợ ngày, chủ ô hàng chợ đêm, đấu giá viên, giám định viên). 4 NPC này không có trong danh sách 12 của GDD và không có đặc tính. |
NPC
Nhà Buôn • Xạ Thủ Nỏ • Kỹ Sư Chiến Trường. Cộng thêm 4 NPC chức năng chợ (người bán chợ ngày, chủ ô hàng chợ đêm, đấu giá viên, giám định viên). 4 NPC này không có trong danh sách 12 của GDD và không có đặc tính.
| Thú cưng (1) | Quạ Tin |
Thú cưng (1)
Quạ Tin
| Gen heo (3) | Mộng Nhãn • Phong Mẫu • Tinh Quang |
Gen heo (3)
Mộng Nhãn • Phong Mẫu • Tinh Quang
| Quái/Boss (xuất hiện ở Farm_Main) | Kẻ Bắt Heo • Kẻ Cào Phá • Thiết Giáp Công Thành • Đầu Lĩnh Trộm (tinh anh) • Boss Chúa Trộm Rìu Sắt (chu kỳ 5, 20, 35…) |
Quái/Boss (xuất hiện ở Farm_Main)
Kẻ Bắt Heo • Kẻ Cào Phá • Thiết Giáp Công Thành • Đầu Lĩnh Trộm (tinh anh) • Boss Chúa Trộm Rìu Sắt (chu kỳ 5, 20, 35…)
| Cảnh sự kiện (4) | Giữ Cổng Ngược (Đỏ/Cổng) • Lò Mổ Hư Không (Đỏ/Cổng) • Đợt Săn Trong Sương (Đỏ, đánh ở trại) • Càn Quét UFO (Đỏ, đánh ở trại) |
Cảnh sự kiện (4)
Giữ Cổng Ngược (Đỏ/Cổng) • Lò Mổ Hư Không (Đỏ/Cổng) • Đợt Săn Trong Sương (Đỏ, đánh ở trại) • Càn Quét UFO (Đỏ, đánh ở trại)
| Hệ thống gắn với map | Logic chợ ngày/đêm, đấu giá, GTTC, Ledger, kiểm dịch, giám định chợ là của D. C dựng không gian, quầy, NPC và điểm mở UI. AI lính đánh thuê do C làm. |
Hệ thống gắn với map
Logic chợ ngày/đêm, đấu giá, GTTC, Ledger, kiểm dịch, giám định chợ là của D. C dựng không gian, quầy, NPC và điểm mở UI. AI lính đánh thuê do C làm.
| Đầu ra MUST | Map tự đổi giữa ngày và đêm theo giờ game. Mỗi quầy mở đúng màn hình tương ứng. Quầy Giám Định Cấp Cao chỉ hiện khi sự kiện đó được tung trúng. Hội quán thuê được lính. 3 NPC + 4 NPC chức năng, 1 thú cưng, 8 công trình có prefab đủ trạng thái. |
Đầu ra MUST
Map tự đổi giữa ngày và đêm theo giờ game. Mỗi quầy mở đúng màn hình tương ứng. Quầy Giám Định Cấp Cao chỉ hiện khi sự kiện đó được tung trúng. Hội quán thuê được lính. 3 NPC + 4 NPC chức năng, 1 thú cưng, 8 công trình có prefab đủ trạng thái.
| STRETCH | Dân thị trấn đi lại, đèn lồng bay, nhạc nền đổi theo ngày/đêm. |
STRETCH
Dân thị trấn đi lại, đèn lồng bay, nhạc nền đổi theo ngày/đêm.
6.5. D – Rừng Nguyên Sinh (Forest_Primeval) và khung cảnh sự kiện
| Hạng mục | Nội dung |
Hạng mục
Nội dung
| Chủ đề và cảm giác | Rừng cổ thụ rậm rạp, ánh trăng xuyên tán lá, sương lạnh. Là ngoại vi (GDD 3.6: rừng sâu, mỏ, hồ, di tích), không có đợt quái. |
Chủ đề và cảm giác
Rừng cổ thụ rậm rạp, ánh trăng xuyên tán lá, sương lạnh. Là ngoại vi (GDD 3.6: rừng sâu, mỏ, hồ, di tích), không có đợt quái.
| Các khu vực | Bìa rừng (gom vật liệu: gỗ, nấm) • Rừng sâu • Hồ • Di tích cổ (trang trí, gợi ý cốt truyện) • Lối mòn phía Nam về trại • Nơi Mận dẫn đường. Cổng chỉ xuất hiện trong bản đồ nông trại (GDD 24), nên không đặt Cổng cố định ở rừng. |
Các khu vực
Bìa rừng (gom vật liệu: gỗ, nấm) • Rừng sâu • Hồ • Di tích cổ (trang trí, gợi ý cốt truyện) • Lối mòn phía Nam về trại • Nơi Mận dẫn đường. Cổng chỉ xuất hiện trong bản đồ nông trại (GDD 24), nên không đặt Cổng cố định ở rừng.
| Khung sự kiện (D sở hữu) | Bộ chuyển cảnh vào/ra Cổng và Sương mù; đếm % hoàn thành; thoát bằng Bùa Hồi Hương (Cổng ≥50%), La Bàn Tán Vụ / Ngọc Phá Sương (Sương mù); kẹt tối đa 3 ngày; chia thưởng 30/50/100%. Mẫu scene Event_<Color>_<Name> để 4 người dựng nội dung. |
Khung sự kiện (D sở hữu)
Bộ chuyển cảnh vào/ra Cổng và Sương mù; đếm % hoàn thành; thoát bằng Bùa Hồi Hương (Cổng ≥50%), La Bàn Tán Vụ / Ngọc Phá Sương (Sương mù); kẹt tối đa 3 ngày; chia thưởng 30/50/100%. Mẫu scene Event_<Color>_<Name> để 4 người dựng nội dung.
| Tileset và ánh sáng | TILE_Forest_* nhiều lớp (đất, rễ, tán cây). Tán cây mờ đi khi người chơi đi bên dưới. Có tia trăng, đom đóm, sương bằng particle. Tileset này dùng lại cho các cảnh sự kiện Sương mù. |
Tileset và ánh sáng
TILE_Forest_* nhiều lớp (đất, rễ, tán cây). Tán cây mờ đi khi người chơi đi bên dưới. Có tia trăng, đom đóm, sương bằng particle. Tileset này dùng lại cho các cảnh sự kiện Sương mù.
| Công trình thiết kế (7) | Hỗ trợ: Bạch Vân · Vạn Linh Hồi Nguyên Trận. Thông tin: Đài Dò Dị Tượng • Máy Cộng Hưởng Huyền Bí. Bẫy: Tấm Sốc Điện • Hố Keo • Lưới EMP • Mìn Nổ. |
Công trình thiết kế (7)
Hỗ trợ: Bạch Vân · Vạn Linh Hồi Nguyên Trận. Thông tin: Đài Dò Dị Tượng • Máy Cộng Hưởng Huyền Bí. Bẫy: Tấm Sốc Điện • Hố Keo • Lưới EMP • Mìn Nổ.
| NPC | Nhà Nghiên Cứu Gen (làm việc ở Phòng Nghiên Cứu của A) • Thợ Săn UFO • Thích Khách Đêm • NPC cốt truyện Mận • Bí nhân (thương nhân nửa người nửa quỷ, có đèn lồng xanh) |
NPC
Nhà Nghiên Cứu Gen (làm việc ở Phòng Nghiên Cứu của A) • Thợ Săn UFO • Thích Khách Đêm • NPC cốt truyện Mận • Bí nhân (thương nhân nửa người nửa quỷ, có đèn lồng xanh)
| Thú cưng (2) | Heo Săn Nấm • Chó Săn Chiến |
Thú cưng (2)
Heo Săn Nấm • Chó Săn Chiến
| Gen heo (3) | Mộc Cước • Lôi Mạch • Hư Thể (gồm trạng thái phong ấn và thức tỉnh) |
Gen heo (3)
Mộc Cước • Lôi Mạch • Hư Thể (gồm trạng thái phong ấn và thức tỉnh)
| Quái/Boss (xuất hiện ở Farm_Main) | Bóng Huyền • Chó Sương • Kẻ Cắt Dây • Boss Thực Thể Sương (chu kỳ 15, 30, 45…) |
Quái/Boss (xuất hiện ở Farm_Main)
Bóng Huyền • Chó Sương • Kẻ Cắt Dây • Boss Thực Thể Sương (chu kỳ 15, 30, 45…)
| Cảnh sự kiện (4) | Hạt Giống Trắng (Xanh/Sương) • Chợ Không Đồng Hồ (Vàng/Sương) • Thư Viện Mực Ấn (Vàng/Sương) • Ba Ngày Khóa (Đỏ/Sương) |
Cảnh sự kiện (4)
Hạt Giống Trắng (Xanh/Sương) • Chợ Không Đồng Hồ (Vàng/Sương) • Thư Viện Mực Ấn (Vàng/Sương) • Ba Ngày Khóa (Đỏ/Sương)
| Đầu ra MUST | Rừng đi lại được, tán cây che/mờ đúng, có điểm thu thập. Khung sự kiện chạy được cả 12 cảnh Cổng/Sương mù với luật thoát đúng GDD 24. 3 NPC, Mận, Bí nhân, 2 thú cưng, 7 công trình có prefab đủ trạng thái. |
Đầu ra MUST
Rừng đi lại được, tán cây che/mờ đúng, có điểm thu thập. Khung sự kiện chạy được cả 12 cảnh Cổng/Sương mù với luật thoát đúng GDD 24. 3 NPC, Mận, Bí nhân, 2 thú cưng, 7 công trình có prefab đủ trạng thái.
| STRETCH | Rừng sâu sinh ngẫu nhiên theo seed, động vật hoang trang trí, âm thanh môi trường 3 lớp. |
STRETCH
Rừng sâu sinh ngẫu nhiên theo seed, động vật hoang trang trí, âm thanh môi trường 3 lớp.
6.6. Kết nối giữa 4 map và chuẩn chung khi dựng map
| Tuyến nối | Vị trí cổng | Người đặt | ID điểm xuất hiện (spawn) |
Tuyến nối
Vị trí cổng
Người đặt
ID điểm xuất hiện (spawn)
| Farm_Main ↔ Farm_Core | Đoạn Chuyển Map: Cổng Sau ở rào phía Bắc của trại – hành lang khoảng 20×8 m – lối vào phía Nam của Farm_Core. Người chơi và quái đi qua được, heo thì không. | B (cổng), A (hành lang) | spawn.farm.from_core • spawn.core.from_farm |
Farm_Main ↔ Farm_Core
Đoạn Chuyển Map: Cổng Sau ở rào phía Bắc của trại – hành lang khoảng 20×8 m – lối vào phía Nam của Farm_Core. Người chơi và quái đi qua được, heo thì không.
B (cổng), A (hành lang)
spawn.farm.from_core • spawn.core.from_farm
| Farm_Main ↔ Market_Town | Cổng chính phía Nam của trại – Đường lớn phía Bắc của chợ | B, C | spawn.farm.from_market • spawn.market.from_farm |
Farm_Main ↔ Market_Town
Cổng chính phía Nam của trại – Đường lớn phía Bắc của chợ
B, C
spawn.farm.from_market • spawn.market.from_farm
| Farm_Main ↔ Hub_Village | Cổng phụ phía Tây của trại – Cổng làng | B, A | spawn.farm.from_hub • spawn.hub.from_farm |
Farm_Main ↔ Hub_Village
Cổng phụ phía Tây của trại – Cổng làng
B, A
spawn.farm.from_hub • spawn.hub.from_farm
| Farm_Main ↔ Forest_Primeval | Lối mòn vành ngoài phía Bắc – Lối mòn phía Nam của rừng | B, D | spawn.farm.from_forest • spawn.forest.from_farm |
Farm_Main ↔ Forest_Primeval
Lối mòn vành ngoài phía Bắc – Lối mòn phía Nam của rừng
B, D
spawn.farm.from_forest • spawn.forest.from_farm
| Farm_Main ↔ Event_* | Cổng xuất hiện ngẫu nhiên trong vùng ẩn hợp lệ; Sương mù bao lấy người chơi rồi dịch chuyển đi | D (khung), B (vùng ẩn) | spawn.event.<name>.entry • spawn.farm.from_event |
Farm_Main ↔ Event_*
Cổng xuất hiện ngẫu nhiên trong vùng ẩn hợp lệ; Sương mù bao lấy người chơi rồi dịch chuyển đi
D (khung), B (vùng ẩn)
spawn.event.<name>.entry • spawn.farm.from_event
Cổng dùng chung: A làm prefab PF_Map_Gate, PF_Map_TransitionCorridor (Đoạn Chuyển Map) và SceneLoader. Mỗi map là một scene riêng, nạp theo kiểu additive. Chuyển map không quá 1,5 giây trên máy cấu hình tối thiểu. Khi người chơi ở Farm_Core, Farm_Main vẫn được mô phỏng trong Core.
Logic không phụ thuộc scene: đàn heo, thời gian, kinh tế, thời tiết chạy trong Core, vẫn tiếp diễn khi người chơi đang ở map khác. Scene chỉ là phần hiển thị.
Đợt quái: diễn ra ở Farm_Main và vành ngoài. Quái muốn nhắm Kho/Nhà chính thì phải đi qua Đoạn Chuyển Map để vào Farm_Core. Quái không tấn công ngoại vi (GDD 3.6). Khi có cảnh báo đợt quái mà người chơi đang ở ngoại vi, game hiện lời nhắc quay về. Người phụ trách quái làm art, animation, prefab và cùng C làm AI. B đặt điểm vào và đường đi trong Farm_Main, A đặt đường đi trong Farm_Core.
Kích thước: bội số của Ô Đất (10×10 m = 160×160 px ở 16 PPU), gốc tọa độ (0,0) ở góc Tây Nam của map.
Sorting layer chung: Ground, Decor, Actors, Canopy/Overlay, UI_World. Không tự tạo sorting layer mới.
Camera: mỗi map có một vùng giới hạn Cinemachine Confiner. Ánh sáng ngày/đêm dùng chung preset Light 2D của B.
Checklist nghiệm thu map: collider kín, không có chỗ kẹt; đúng palette và PPU; đủ cổng và spawn đúng tên; ≥60 FPS khi có tải; không lỗi Console; ảnh chụp ngày/đêm đính kèm PR.
6.7. WBS dựng map (bổ sung vào các giai đoạn P0–P5)
Các item dưới đây là một phần chính thức của WBS ở mục 7. Mỗi item được tính là một việc hợp lệ cho quy tắc cập nhật GitHub.
| ID | Work item | Owner | Loại | Đầu ra mong đợi |
ID
Work item
Owner
Loại
Đầu ra mong đợi
| P0.14 | SceneLoader + PF_Map_Gate + quy ước spawn | A | M | Đi qua lại giữa 2 scene thử, state Core không bị reset. |
P0.14
SceneLoader + PF_Map_Gate + quy ước spawn
A
M
Đi qua lại giữa 2 scene thử, state Core không bị reset.
| P0.15 | Blockout Hub_Village | A | M | Hub_Village dạng map xám: đủ khu vực, có collider, có cổng nối về trại, đúng kích thước. |
P0.15
Blockout Hub_Village
A
M
Hub_Village dạng map xám: đủ khu vực, có collider, có cổng nối về trại, đúng kích thước.
| P0.16 | Chuẩn hóa Farm_Main theo chuẩn map chung | B | M | Map đã có được gắn zone, cổng, lối quái, sorting layer chuẩn. |
P0.16
Chuẩn hóa Farm_Main theo chuẩn map chung
B
M
Map đã có được gắn zone, cổng, lối quái, sorting layer chuẩn.
| P0.17 | Blockout Market_Town (ngày + đêm) | C | M | Map xám có 2 lớp bố cục ngày/đêm và cổng nối về trại. |
P0.17
Blockout Market_Town (ngày + đêm)
C
M
Map xám có 2 lớp bố cục ngày/đêm và cổng nối về trại.
| P0.18 | Blockout Forest_Primeval | D | M | Map xám đủ khu vực, có cổng nối về trại; có mẫu scene Event_* để 4 người dùng. |
P0.18
Blockout Forest_Primeval
D
M
Map xám đủ khu vực, có cổng nối về trại; có mẫu scene Event_* để 4 người dùng.
| P1.17–P1.20 | Art pass 1 cho map của mình (A/B/C/D theo thứ tự) | A, B, C, D | M | Tileset chính hoàn chỉnh, ≥60% trang trí, ánh sáng cơ bản, đi được trọn map. |
P1.17–P1.20
Art pass 1 cho map của mình (A/B/C/D theo thứ tự)
A, B, C, D
M
Tileset chính hoàn chỉnh, ≥60% trang trí, ánh sáng cơ bản, đi được trọn map.
| P2.17–P2.20 | Đưa NPC, thú cưng và điểm tương tác vào map của mình | A, B, C, D | M | NPC đi theo lịch trong map, tương tác chính của map hoạt động với logic thật. |
P2.17–P2.20
Đưa NPC, thú cưng và điểm tương tác vào map của mình
A, B, C, D
M
NPC đi theo lịch trong map, tương tác chính của map hoạt động với logic thật.
| P3.13–P3.16 | Tích hợp công trình, điểm spawn và đường đi của quái trong map | A, B, C, D | M | Quái của map xuất hiện đúng; công trình của map có prefab đủ trạng thái. |
P3.13–P3.16
Tích hợp công trình, điểm spawn và đường đi của quái trong map
A, B, C, D
M
Quái của map xuất hiện đúng; công trình của map có prefab đủ trạng thái.
| P5.07–P5.10 | Polish map: ánh sáng ngày/đêm, âm thanh nền, chi tiết, tối ưu | A, B, C, D | M | Đạt checklist 6.6; ảnh trước/sau trong PR. |
P5.07–P5.10
Polish map: ánh sáng ngày/đêm, âm thanh nền, chi tiết, tối ưu
A, B, C, D
M
Đạt checklist 6.6; ảnh trước/sau trong PR.
6.8. Nhân vật người chơi và nhân vật cốt truyện
| Người | MUST | STRETCH (làm hơn thì tốt) |
Người
MUST
STRETCH (làm hơn thì tốt)
| B | An (nữ) – đã thiết kế; chuẩn hóa theo Art Bible, đủ bộ animation 5.2, chân dung hội thoại 3 biểu cảm. | Trang phục thay theo trang bị (layer vũ khí/giáp); 8 hướng. |
B
An (nữ) – đã thiết kế; chuẩn hóa theo Art Bible, đủ bộ animation 5.2, chân dung hội thoại 3 biểu cảm.
Trang phục thay theo trang bị (layer vũ khí/giáp); 8 hướng.
| C | Khoa (nam) – concept sheet, đủ bộ animation 5.2, chân dung 3 biểu cảm. | Hiệu ứng “bản ngã” (ảo ảnh Khoa) theo cốt truyện; 8 hướng. |
C
Khoa (nam) – concept sheet, đủ bộ animation 5.2, chân dung 3 biểu cảm.
Hiệu ứng “bản ngã” (ảo ảnh Khoa) theo cốt truyện; 8 hướng.
| A | Không tạo nhân vật người chơi (Design Lock). Chân dung + sprite NPC cốt truyện Bà Sáu (người giữ trại tạm thời, ở Hub_Village). | Biểu cảm phụ và animation hội thoại cho Bà Sáu. |
A
Không tạo nhân vật người chơi (Design Lock). Chân dung + sprite NPC cốt truyện Bà Sáu (người giữ trại tạm thời, ở Hub_Village).
Biểu cảm phụ và animation hội thoại cho Bà Sáu.
| D | Không tạo nhân vật người chơi (Design Lock). Chân dung + sprite NPC cốt truyện Mận (cô gái dẫn đường, ở Forest_Primeval). | Biểu cảm phụ và animation dẫn đường cho Mận. |
D
Không tạo nhân vật người chơi (Design Lock). Chân dung + sprite NPC cốt truyện Mận (cô gái dẫn đường, ở Forest_Primeval).
Biểu cảm phụ và animation dẫn đường cho Mận.
6.9. Asset khác
| Người | MUST | STRETCH |
Người
MUST
STRETCH
| A | Icon HUD lõi (HP, Stamina, giờ, buổi), icon 10 thời tiết, font pixel tiếng Việt, cursor. | Màn hình loading, logo, hiệu ứng chuyển ngày/đêm. |
A
Icon HUD lõi (HP, Stamina, giờ, buổi), icon 10 thời tiết, font pixel tiếng Việt, cursor.
Màn hình loading, logo, hiệu ứng chuyển ngày/đêm.
| B | Tileset Farm (đã có, chuẩn hóa), VFX 10 thời tiết, icon 10 mối đe dọa sức khỏe, icon Sinh Cảnh 5 bậc. | Biến thể mùa; trang trí map; bóng đổ động. |
B
Tileset Farm (đã có, chuẩn hóa), VFX 10 thời tiết, icon 10 mối đe dọa sức khỏe, icon Sinh Cảnh 5 bậc.
Biến thể mùa; trang trí map; bóng đổ động.
| C | Icon 24 kỹ năng, icon 6 ô trang bị, VFX combat (hit, slash, projectile), icon Phù Văn/Mệnh Ấn. | Hiệu ứng Nộ Khí/Huyết Tế toàn màn hình. |
C
Icon 24 kỹ năng, icon 6 ô trang bị, VFX combat (hit, slash, projectile), icon Phù Văn/Mệnh Ấn.
Hiệu ứng Nộ Khí/Huyết Tế toàn màn hình.
| D | UI Kit (khung, nút, panel 9-slice), icon vật phẩm, icon tiền tệ/GTTC, Giấy Chuyên Biệt 4 màu, VFX Cổng/Sương mù, minimap có viền ranh giới trại (bắt buộc theo GDD 3.10-B1). | Animation mở thẻ Giấy kiểu gacha; minimap có biểu tượng động. |
D
UI Kit (khung, nút, panel 9-slice), icon vật phẩm, icon tiền tệ/GTTC, Giấy Chuyên Biệt 4 màu, VFX Cổng/Sương mù, minimap có viền ranh giới trại (bắt buộc theo GDD 3.10-B1).
Animation mở thẻ Giấy kiểu gacha; minimap có biểu tượng động.
6.10. Tổng hợp khối lượng theo người
| Người | Khu vực | Phòng / hạ tầng | Nhân vật | NPC | Thú cưng | Gen | Công trình | Quái/Boss | Sự kiện |
Người
Khu vực
Phòng / hạ tầng
Nhân vật
NPC
Thú cưng
Gen
Công trình
Quái/Boss
Sự kiện
| A | Farm_Core + Hub_Village | 7 phòng + hạ tầng điện | Bà Sáu | 3 | 2 | 3 | 8 | 3 + 1 | 2 |
A
Farm_Core + Hub_Village
7 phòng + hạ tầng điện
Bà Sáu
3
2
3
8
3 + 1
2
| B | Farm_Main | Hạ tầng trại | An | 3 | 2 | 3 + rig | 8 | 4 | 4 |
B
Farm_Main
Hạ tầng trại
An
3
2
3 + rig
8
4
4
| C | Market_Town (ngày + đêm) | Quầy chợ | Khoa | 3 + 4 chức năng | 1 | 3 | 8 | 4 + 1 | 4 |
C
Market_Town (ngày + đêm)
Quầy chợ
Khoa
3 + 4 chức năng
1
3
8
4 + 1
4
| D | Forest_Primeval + khung sự kiện | Khung Event_* | Mận, Bí nhân | 3 | 2 | 3 | 7 | 3 + 1 | 4 |
D
Forest_Primeval + khung sự kiện
Khung Event_*
Mận, Bí nhân
3
2
3
7
3 + 1
4
| Tổng | 4 khu + 12 cảnh sự kiện |  | 2 người chơi + 3 | 12 (+4) | 7 | 12 | 31 | 14 + 3 | 14 |
Tổng
4 khu + 12 cảnh sự kiện
2 người chơi + 3
12 (+4)
7
12
31
14 + 3
14
6.11. WBS bổ sung cho từng khu vực và phân chia 14 sự kiện
| ID | Work item | Owner | Loại | Đầu vào | Đầu ra mong đợi / tiêu chí nghiệm thu |
ID
Work item
Owner
Loại
Đầu vào
Đầu ra mong đợi / tiêu chí nghiệm thu
| P0.19 | Blockout map Farm_Core (7 phòng + khu thông tin) và Đoạn Chuyển Map | A | M | GDD 3.6; Farm_Main | Farm_Core là scene riêng nằm phía Bắc trại. Đi qua Đoạn Chuyển Map hai chiều ≤1,5 giây, không mất state. Mỗi phòng có collider, cửa và điểm tương tác. |
P0.19
Blockout map Farm_Core (7 phòng + khu thông tin) và Đoạn Chuyển Map
A
M
GDD 3.6; Farm_Main
Farm_Core là scene riêng nằm phía Bắc trại. Đi qua Đoạn Chuyển Map hai chiều ≤1,5 giây, không mất state. Mỗi phòng có collider, cửa và điểm tương tác.
| P1.21 | Luồng màn hình chung: menu chính, chọn An/Khoa, slot save, pause, cài đặt, bất tỉnh | A | M | GDD 8, 19 | Bất tỉnh: Thể trạng −20, mất 10% vàng mang theo, tỉnh lại ở nhà chính; có test. |
P1.21
Luồng màn hình chung: menu chính, chọn An/Khoa, slot save, pause, cài đặt, bất tỉnh
A
M
GDD 8, 19
Bất tỉnh: Thể trạng −20, mất 10% vàng mang theo, tỉnh lại ở nhà chính; có test.
| P2.21 | Cấp trại 1–20, EXP trại, bảng mở khóa | A | M | GDD 3.5 | EXP từ bán heo, đợt quái, hợp đồng, định danh, xây; mở đúng mốc (ví dụ Phòng Nghiên Cứu ở Cấp 8–10, Bạch Vân từ Cấp 12). |
P2.21
Cấp trại 1–20, EXP trại, bảng mở khóa
A
M
GDD 3.5
EXP từ bán heo, đợt quái, hợp đồng, định danh, xây; mở đúng mốc (ví dụ Phòng Nghiên Cứu ở Cấp 8–10, Bạch Vân từ Cấp 12).
| P2.22 | Phòng Nghiên Cứu: không gian, hàng chờ nghiên cứu, NPC trực, khóa mở | A (logic D) | M | GDD 5.4, 5.2, 6.4 | Giám định 95/85/65/50% với phí, Stamina, thời gian đúng bảng 5.4; không có Nhà Nghiên Cứu Gen đã mở “Giải Mã Sâu” thì khóa Huyền Thoại/Dị Biến. |
P2.22
Phòng Nghiên Cứu: không gian, hàng chờ nghiên cứu, NPC trực, khóa mở
A (logic D)
M
GDD 5.4, 5.2, 6.4
Giám định 95/85/65/50% với phí, Stamina, thời gian đúng bảng 5.4; không có Nhà Nghiên Cứu Gen đã mở “Giải Mã Sâu” thì khóa Huyền Thoại/Dị Biến.
| P2.23 | Hạ tầng trại + Nơi Trú Đàn + lệnh Lùa + Chen Chúc + lớp phủ phục vụ/nguồn mát | B | M | GDD 3.2, 3.7 | Sức chứa = MIN(4 nguồn) − xác; Chen Chúc kích/tắt đúng ngưỡng; phí duy trì 4/6/3/5G mỗi ngày. |
P2.23
Hạ tầng trại + Nơi Trú Đàn + lệnh Lùa + Chen Chúc + lớp phủ phục vụ/nguồn mát
B
M
GDD 3.2, 3.7
Sức chứa = MIN(4 nguồn) − xác; Chen Chúc kích/tắt đúng ngưỡng; phí duy trì 4/6/3/5G mỗi ngày.
| P2.24 | Hợp đồng giao heo, 8 chủ đề chu kỳ, phản ứng thế giới, sức mua | D | M | GDD 20.2–20.3 | Mô phỏng 1000 chu kỳ cho tỉ lệ chủ đề khớp bảng (24/12/12/10/8/10/12/12%). |
P2.24
Hợp đồng giao heo, 8 chủ đề chu kỳ, phản ứng thế giới, sức mua
D
M
GDD 20.2–20.3
Mô phỏng 1000 chu kỳ cho tỉ lệ chủ đề khớp bảng (24/12/12/10/8/10/12/12%).
| P2.25 | Minimap với viền ranh giới trại | D | M | GDD 3.10-B1 | Ranh giới hiện rõ; Lạc Hướng (Sương Mù) ẩn mọi thứ ngoài 15 m. |
P2.25
Minimap với viền ranh giới trại
D
M
GDD 3.10-B1
Ranh giới hiện rõ; Lạc Hướng (Sương Mù) ẩn mọi thứ ngoài 15 m.
| P2.26 | Hạ tầng điện vật lý: cột, dây nổi/ngầm, Tụ Điện | A | M | GDD 3.8, 15 | Cắt 1 đoạn dây chỉ tắt nhánh phía sau; dây ngầm giá ×3, không bị cắt. |
P2.26
Hạ tầng điện vật lý: cột, dây nổi/ngầm, Tụ Điện
A
M
GDD 3.8, 15
Cắt 1 đoạn dây chỉ tắt nhánh phía sau; dây ngầm giá ×3, không bị cắt.
| P3.17 | Báo cáo sau đợt + Bảng Điều Khiển Bạch Vân ở nhà chính | C | M | GDD 13.1, 18 | Báo cáo đủ 7 mục theo GDD 18; kích từ xa trễ thêm 3 giây. |
P3.17
Báo cáo sau đợt + Bảng Điều Khiển Bạch Vân ở nhà chính
C
M
GDD 13.1, 18
Báo cáo đủ 7 mục theo GDD 18; kích từ xa trễ thêm 3 giây.
| P3.18 | Điểm Bí nhân, vùng ẩn cho Cổng, 8–12 điểm vào quái | B | M | GDD 3.7–3.8 | Luôn còn ≥4 vùng Cổng hợp lệ kể cả khi trại xây kín; không spawn trong 25 m hoặc trong khung camera. |
P3.18
Điểm Bí nhân, vùng ẩn cho Cổng, 8–12 điểm vào quái
B
M
GDD 3.7–3.8
Luôn còn ≥4 vùng Cổng hợp lệ kể cả khi trại xây kín; không spawn trong 25 m hoặc trong khung camera.
| P4.11 | Xưởng Cường Hóa: gia cường giấy, rã giấy, Bàn Khắc Phù Văn, khắc Mệnh Ấn | A (logic D, C) | M | GDD 10.6–10.7, 12.4; ADR-0002 | Tỉ lệ +2 ≤95%, +3 ≤80%; thiếu nguyên liệu × (nộp/yêu cầu)^1,5, tối thiểu 10%; thất bại không hỏng giấy; Mệnh Ấn trừ 10 HP tối đa. |
P4.11
Xưởng Cường Hóa: gia cường giấy, rã giấy, Bàn Khắc Phù Văn, khắc Mệnh Ấn
A (logic D, C)
M
GDD 10.6–10.7, 12.4; ADR-0002
Tỉ lệ +2 ≤95%, +3 ≤80%; thiếu nguyên liệu × (nộp/yêu cầu)^1,5, tối thiểu 10%; thất bại không hỏng giấy; Mệnh Ấn trừ 10 HP tối đa.
| P4.12 | Khung sự kiện + 12 cảnh Cổng/Sương mù (bảng dưới) | D + ALL | M | GDD 24 | Mỗi cảnh chơi được từ đầu đến cuối; luật thoát đúng; có test cho các mốc 50% và 3 ngày. |
P4.12
Khung sự kiện + 12 cảnh Cổng/Sương mù (bảng dưới)
D + ALL
M
GDD 24
Mỗi cảnh chơi được từ đầu đến cuối; luật thoát đúng; có test cho các mốc 50% và 3 ngày.
| P5.11 | Nghiệm thu Farm_Main theo checklist GDD 3.10 nhóm A–H | B (A hỗ trợ) | M | GDD 3.10 | Toàn bộ ô ☐ chuyển thành ☑, kèm bằng chứng trong PR. |
P5.11
Nghiệm thu Farm_Main theo checklist GDD 3.10 nhóm A–H
B (A hỗ trợ)
M
GDD 3.10
Toàn bộ ô ☐ chuyển thành ☑, kèm bằng chứng trong PR.
| Người | Cảnh sự kiện được giao (màu / hình thức) |
Người
Cảnh sự kiện được giao (màu / hình thức)
| A | Kho Lạnh Bỏ Quên (Xanh/Cổng) • Giám Định Vô Danh (Vàng/Cổng) |
A
Kho Lạnh Bỏ Quên (Xanh/Cổng) • Giám Định Vô Danh (Vàng/Cổng)
| B | Vườn Mười Hai Quả (Xanh/Cổng) • Đồng Cỏ Không Bóng (Xanh/Sương) • Bí Nhân Mua Già (Vàng/Cổng) • Đổi Một Lấy Một (Vàng/Cổng) |
B
Vườn Mười Hai Quả (Xanh/Cổng) • Đồng Cỏ Không Bóng (Xanh/Sương) • Bí Nhân Mua Già (Vàng/Cổng) • Đổi Một Lấy Một (Vàng/Cổng)
| C | Giữ Cổng Ngược (Đỏ/Cổng) • Lò Mổ Hư Không (Đỏ/Cổng) • Đợt Săn Trong Sương và Càn Quét UFO (Đỏ, phòng thủ ngay tại trại, không cần cảnh riêng) |
C
Giữ Cổng Ngược (Đỏ/Cổng) • Lò Mổ Hư Không (Đỏ/Cổng) • Đợt Săn Trong Sương và Càn Quét UFO (Đỏ, phòng thủ ngay tại trại, không cần cảnh riêng)
| D | Hạt Giống Trắng (Xanh/Sương) • Chợ Không Đồng Hồ (Vàng/Sương) • Thư Viện Mực Ấn (Vàng/Sương) • Ba Ngày Khóa (Đỏ/Sương) + khung chung |
D
Hạt Giống Trắng (Xanh/Sương) • Chợ Không Đồng Hồ (Vàng/Sương) • Thư Viện Mực Ấn (Vàng/Sương) • Ba Ngày Khóa (Đỏ/Sương) + khung chung
7. WBS Master P0 – P6
Mỗi work item có ID dạng P<giai đoạn>.<số>. Item ước tính trên 4 ngày phải tách thành checkpoint .a/.b/.c, mỗi checkpoint có đầu ra độc lập và demo được trong Unity – mỗi checkpoint được tính là một work item hợp lệ cho quy tắc cập nhật GitHub. Cột “Loại”: M = MUST (bắt buộc), S = STRETCH (làm hơn mức đó thì tốt). “ALL” nghĩa là mỗi người làm phần của mình.
7.1. P0 – Nền tảng (W1–W2: 05/10 – 18/10/2026)
| ID | Work item | Owner | Loại | Đầu vào | Đầu ra mong đợi |
ID
Work item
Owner
Loại
Đầu vào
Đầu ra mong đợi
| P0.01 | Khởi tạo Unity project, URP 2D, cây thư mục, asmdef | A | M | Unity 6000.3 LTS, mục 3 sổ tay | Project mở được trên máy cả 4 người, cây thư mục đúng 3.2, asmdef Core/Contracts/module biên dịch sạch, ProjectVersion.txt chốt bản patch. |
P0.01
Khởi tạo Unity project, URP 2D, cây thư mục, asmdef
A
M
Unity 6000.3 LTS, mục 3 sổ tay
Project mở được trên máy cả 4 người, cây thư mục đúng 3.2, asmdef Core/Contracts/module biên dịch sạch, ProjectVersion.txt chốt bản patch.
| P0.02 | Git hygiene: .gitignore, .gitattributes, LFS, Force Text, Visible Meta, branch protection | A | M | Mục 11 | Push PNG/.aseprite đi qua LFS; main và develop bị khóa push trực tiếp; merge cần 1 review và CI xanh. |
P0.02
Git hygiene: .gitignore, .gitattributes, LFS, Force Text, Visible Meta, branch protection
A
M
Mục 11
Push PNG/.aseprite đi qua LFS; main và develop bị khóa push trực tiếp; merge cần 1 review và CI xanh.
| P0.03 | AGENTS.md + file rule IDE trỏ về AGENTS.md + thư mục docs/agent | A | M | Mục 13 | Cả 4 người chạy 1 phiên agent thử và agent trích dẫn đúng quy tắc R1–R12. |
P0.03
AGENTS.md + file rule IDE trỏ về AGENTS.md + thư mục docs/agent
A
M
Mục 13
Cả 4 người chạy 1 phiên agent thử và agent trích dẫn đúng quy tắc R1–R12.
| P0.04 | CI GitHub Actions: tên file, tiêu đề PR, dotnet DomainTests | A | M | Mục 11.5, 14 | PR sai tên file hoặc test đỏ bị chặn. (Unity test qua GameCI: S) |
P0.04
CI GitHub Actions: tên file, tiêu đề PR, dotnet DomainTests
A
M
Mục 11.5, 14
PR sai tên file hoặc test đỏ bị chặn. (Unity test qua GameCI: S)
| P0.05 | Art Bible v1, palette 48 màu, preset import, font pixel tiếng Việt | B | M | Mục 5; map và An hiện có | docs/art-bible/ có trang quy chuẩn, palette .gpl/.ase, preset; An và map đã khớp chuẩn. |
P0.05
Art Bible v1, palette 48 màu, preset import, font pixel tiếng Việt
B
M
Mục 5; map và An hiện có
docs/art-bible/ có trang quy chuẩn, palette .gpl/.ase, preset; An và map đã khớp chuẩn.
| P0.06 | Nhập map heo thả rông và An vào repo | B | M | Asset B đã làm | Farm_Main có Tilemap, collider ranh giới, cổng; prefab PF_Player_An đi được 4 hướng. |
P0.06
Nhập map heo thả rông và An vào repo
B
M
Asset B đã làm
Farm_Main có Tilemap, collider ranh giới, cổng; prefab PF_Player_An đi được 4 hướng.
| P0.07 | Data Catalog: schema + điền đủ ID và owner của 12 gen, 12 NPC, 7 thú cưng, 31 công trình, 14 quái + 3 Boss, 24 kỹ năng, 10 thời tiết, 10 mối đe dọa, 14 sự kiện | D | M | GDD 1.3, 5, 11, 13–19, 24 | Catalog không thiếu, không trùng ID; test kiểm đếm số lượng khớp GDD. |
P0.07
Data Catalog: schema + điền đủ ID và owner của 12 gen, 12 NPC, 7 thú cưng, 31 công trình, 14 quái + 3 Boss, 24 kỹ năng, 10 thời tiết, 10 mối đe dọa, 14 sự kiện
D
M
GDD 1.3, 5, 11, 13–19, 24
Catalog không thiếu, không trùng ID; test kiểm đếm số lượng khớp GDD.
| P0.08 | Sổ thông số chuẩn (GDD 1.5) → Balance.json + loader | D | M | GDD 1.4, 1.5 | Mọi hằng số đọc được qua IBalance; test so khớp một số giá trị mẫu với GDD. |
P0.08
Sổ thông số chuẩn (GDD 1.5) → Balance.json + loader
D
M
GDD 1.4, 1.5
Mọi hằng số đọc được qua IBalance; test so khớp một số giá trị mẫu với GDD.
| P0.09 | Khoa: concept sheet, palette, Idle/Walk 4 hướng | C | M | Art Bible | PF_Player_Khoa đi được trong Farm_Main, B duyệt style. |
P0.09
Khoa: concept sheet, palette, Idle/Walk 4 hướng
C
M
Art Bible
PF_Player_Khoa đi được trong Farm_Main, B duyệt style.
| P0.10 | CombatSandbox + dummy địch | C | M | P0.01 | Scene thử combat chạy được, có dummy nhận sát thương. |
P0.10
CombatSandbox + dummy địch
C
M
P0.01
Scene thử combat chạy được, có dummy nhận sát thương.
| P0.11 | GameClock + TimeService (16 phút/ngày, 4 buổi, chốt 06:00, dừng khi combat) | A | M | GDD 1.4, 2, 7 | Unit test bao phủ chuyển giờ, chuyển ngày, tạm dừng; HUD debug hiển thị giờ. |
P0.11
GameClock + TimeService (16 phút/ngày, 4 buổi, chốt 06:00, dừng khi combat)
A
M
GDD 1.4, 2, 7
Unit test bao phủ chuyển giờ, chuyển ngày, tạm dừng; HUD debug hiển thị giờ.
| P0.12 | Issue/PR template, CODEOWNERS, GitHub Projects board | D | M | Mục 11 | Toàn bộ WBS P0–P1 có Issue gắn nhãn owner, phase, M/S. |
P0.12
Issue/PR template, CODEOWNERS, GitHub Projects board
D
M
Mục 11
Toàn bộ WBS P0–P1 có Issue gắn nhãn owner, phase, M/S.
| P0.13 | Cài IDE + agent, chạy tools/verify thành công | ALL | M | Mục 12 | Mỗi người gửi log verify xanh và ảnh chụp phiên agent đầu tiên vào Issue onboarding. |
P0.13
Cài IDE + agent, chạy tools/verify thành công
ALL
M
Mục 12
Mỗi người gửi log verify xanh và ảnh chụp phiên agent đầu tiên vào Issue onboarding.
7.2. P1 – Vertical Slice (W3–W8: 19/10 – 29/11/2026)
Mục tiêu: chơi trọn 1 ngày game: chọn An hoặc Khoa → chăm heo → bán ở chợ ngày → nạp trước 1 công trình → chống 1 đợt quái nhỏ → lưu/tải.
| ID | Work item | Owner | Loại | Đầu vào | Đầu ra mong đợi |
ID
Work item
Owner
Loại
Đầu vào
Đầu ra mong đợi
| P1.01 | Save/Load có phiên bản và migrate | A | M | Contracts ISaveable | Lưu/tải 3 lần liên tiếp cho state giống hệt (so hash); file save có version. |
P1.01
Save/Load có phiên bản và migrate
A
M
Contracts ISaveable
Lưu/tải 3 lần liên tiếp cho state giống hệt (so hash); file save có version.
| P1.02 | Player core dùng chung, chọn nhân vật, Input System, tương tác | A | M | PF An, PF Khoa | Màn chọn An/Khoa; cả hai dùng chung controller, khác sprite và chỉ số gốc theo GDD 8. |
P1.02
Player core dùng chung, chọn nhân vật, Input System, tương tác
A
M
PF An, PF Khoa
Màn chọn An/Khoa; cả hai dùng chung controller, khác sprite và chỉ số gốc theo GDD 8.
| P1.03 | Di chuyển đàn thả rông: flow-field + steering, ranh giới, cổng | B | M | Farm_Main | 100 heo đi lang thang tự nhiên, không xuyên rào, tụ về máng/nơi trú; ≥60 FPS. |
P1.03
Di chuyển đàn thả rông: flow-field + steering, ranh giới, cổng
B
M
Farm_Main
100 heo đi lang thang tự nhiên, không xuyên rào, tụ về máng/nơi trú; ≥60 FPS.
| P1.04 | Vòng đời 4 giai đoạn, cân nặng, giá, ăn uống | B | M | Balance, GDD 4 | Heo lớn theo ngày game, chuyển sprite theo giai đoạn; giá tính đúng công thức. |
P1.04
Vòng đời 4 giai đoạn, cân nặng, giá, ăn uống
B
M
Balance, GDD 4
Heo lớn theo ngày game, chuyển sprite theo giai đoạn; giá tính đúng công thức.
| P1.05 | Lưới Ô Xây/Ô Đất, đặt công trình, sức chứa | B | M | GDD 3 | Đặt/xóa công trình theo lưới 2×2 m; sức chứa đàn cập nhật theo Ô Đất. |
P1.05
Lưới Ô Xây/Ô Đất, đặt công trình, sức chứa
B
M
GDD 3
Đặt/xóa công trình theo lưới 2×2 m; sức chứa đàn cập nhật theo Ô Đất.
| P1.06 | An: bộ animation đầy đủ (5.2) | B | M | Art Bible | Animator đủ trạng thái, chuyển mượt, không trượt chân. |
P1.06
An: bộ animation đầy đủ (5.2)
B
M
Art Bible
Animator đủ trạng thái, chuyển mượt, không trượt chân.
| P1.07 | Khoa: bộ animation đầy đủ (5.2) | C | M | Art Bible | Như trên cho Khoa. |
P1.07
Khoa: bộ animation đầy đủ (5.2)
C
M
Art Bible
Như trên cho Khoa.
| P1.08 | Combat cơ bản + Stamina 2 lớp | C | M | GDD 9, 12 | Combo 3 đòn, né có i-frame, hitbox/hurtbox; Stamina tiêu hao/hồi đúng GDD. |
P1.08
Combat cơ bản + Stamina 2 lớp
C
M
GDD 9, 12
Combo 3 đòn, né có i-frame, hitbox/hurtbox; Stamina tiêu hao/hồi đúng GDD.
| P1.09 | Wave nhỏ: spawner, 2 quái đầu, telegraph | C | M | Catalog | Đợt quái dừng đồng hồ ngày, quái đi theo nhiều hướng, kết thúc trả về ngày. |
P1.09
Wave nhỏ: spawner, 2 quái đầu, telegraph
C
M
Catalog
Đợt quái dừng đồng hồ ngày, quái đi theo nhiều hướng, kết thúc trả về ngày.
| P1.10 | Chợ ngày bằng vàng + sổ cái (Ledger) | D | M | GDD 20, 22 | Mua cám, bán heo; mọi giao dịch ghi Ledger có lý do; vàng làm tròn 1G. |
P1.10
Chợ ngày bằng vàng + sổ cái (Ledger)
D
M
GDD 20, 22
Mua cám, bán heo; mọi giao dịch ghi Ledger có lý do; vàng làm tròn 1G.
| P1.11 | HUD v1 theo UI Kit | D (A hỗ trợ) | M | Mục 5.4 | HUD giờ/buổi/vàng/HP/Stamina hiển thị đúng, không chữ hard-code. |
P1.11
HUD v1 theo UI Kit
D (A hỗ trợ)
M
Mục 5.4
HUD giờ/buổi/vàng/HP/Stamina hiển thị đúng, không chữ hard-code.
| P1.12 | Base pig rig + 4 giai đoạn | B | M | Art Bible | Rig dùng chung, 3 người còn lại thay màu/chi tiết để tạo gene pack. |
P1.12
Base pig rig + 4 giai đoạn
B
M
Art Bible
Rig dùng chung, 3 người còn lại thay màu/chi tiết để tạo gene pack.
| P1.13 | Asset sprint 1: 1 gene pack + 1 NPC mỗi người | ALL | M | Ma trận 6 | 4 gene pack, 4 NPC đã có trong game. |
P1.13
Asset sprint 1: 1 gene pack + 1 NPC mỗi người
ALL
M
Ma trận 6
4 gene pack, 4 NPC đã có trong game.
| P1.14 | Bảng debug/cheat, log telemetry | A | M | — | Tua thời gian, cộng vàng, sinh heo, gọi wave; log CSV các chỉ số kinh tế mỗi ngày. |
P1.14
Bảng debug/cheat, log telemetry
A
M
—
Tua thời gian, cộng vàng, sinh heo, gọi wave; log CSV các chỉ số kinh tế mỗi ngày.
| P1.15 | Tích hợp Vertical Slice + kịch bản test | A | M | P1.01–P1.14 | Bản build Windows chạy 1 ngày game không lỗi; tag v0.1.0-vs. |
P1.15
Tích hợp Vertical Slice + kịch bản test
A
M
P1.01–P1.14
Bản build Windows chạy 1 ngày game không lỗi; tag v0.1.0-vs.
| P1.16 | Ánh sáng ngày/đêm URP 2D | B | S | 5.3 | Chuyển màu trời mượt theo giờ, đèn sáng ban đêm. |
P1.16
Ánh sáng ngày/đêm URP 2D
B
S
5.3
Chuyển màu trời mượt theo giờ, đèn sáng ban đêm.
7.3. P2 – Alpha: hệ thống lõi (W9–W16: 30/11/2026 – 24/01/2027)
| ID | Work item | Owner | Loại | Đầu vào | Đầu ra mong đợi |
ID
Work item
Owner
Loại
Đầu vào
Đầu ra mong đợi
| P2.01 | Tinh thần, Hòa nhập, Độ bám đàn, mật độ, Áp Lực Sự Kiện 0–100 | B | M | GDD 3, 4 | Chỉ số từng con và toàn đàn, nội suy mềm, có panel xem. |
P2.01
Tinh thần, Hòa nhập, Độ bám đàn, mật độ, Áp Lực Sự Kiện 0–100
B
M
GDD 3, 4
Chỉ số từng con và toàn đàn, nội suy mềm, có panel xem.
| P2.02 | Trạng thái Bầy Đàn, thủ lĩnh, kế vị, Bồi Dưỡng Kế Vị, Truyền Thừa | B | M | GDD 4 | Bầy chỉ hình thành khi đủ điều kiện (≥100 con gắn kết, Sinh Cảnh ≥50…); test điều kiện biên. |
P2.02
Trạng thái Bầy Đàn, thủ lĩnh, kế vị, Bồi Dưỡng Kế Vị, Truyền Thừa
B
M
GDD 4
Bầy chỉ hình thành khi đủ điều kiện (≥100 con gắn kết, Sinh Cảnh ≥50…); test điều kiện biên.
| P2.03 | Sinh sản F1/F2, cận huyết, hậu duệ heo già (Khỏe Mạnh/Chết yểu/Thể Trạng Yếu/Gen Xấu, Ưu Tú, thức tỉnh gen), heo già, xác, Khu Xử Lý, Khu Cách Ly | B | M | GDD 3, 4 | Xác chiếm sức chứa xác riêng; xử lý tốn chi phí và thời gian theo Balance. |
P2.03
Sinh sản F1/F2, cận huyết, hậu duệ heo già (Khỏe Mạnh/Chết yểu/Thể Trạng Yếu/Gen Xấu, Ưu Tú, thức tỉnh gen), heo già, xác, Khu Xử Lý, Khu Cách Ly
B
M
GDD 3, 4
Xác chiếm sức chứa xác riêng; xử lý tốn chi phí và thời gian theo Balance.
| P2.04 | Sinh Cảnh 5 bậc + 10 mối đe dọa sức khỏe + 5 động từ xử lý | B (D dữ liệu) | M | GDD 6 | Mỗi bậc có tương tác riêng với Bầy Đàn; bệnh lây/khỏi theo quy tắc. |
P2.04
Sinh Cảnh 5 bậc + 10 mối đe dọa sức khỏe + 5 động từ xử lý
B (D dữ liệu)
M
GDD 6
Mỗi bậc có tương tác riêng với Bầy Đàn; bệnh lây/khỏi theo quy tắc.
| P2.05 | Thời tiết 10 loại + trạng thái lên người và heo | A (B VFX) | M | GDD 7 | Thời tiết đổi theo lịch, áp trạng thái đúng; VFX tương ứng. |
P2.05
Thời tiết 10 loại + trạng thái lên người và heo
A (B VFX)
M
GDD 7
Thời tiết đổi theo lịch, áp trạng thái đúng; VFX tương ứng.
| P2.06 | Gen 12 dòng: cơ chế, tính chất phụ, “?”, giám định | D (ALL art) | M | GDD 5 | Heo chưa định danh hiện “?”; giám định theo kênh và tỉ lệ; Xích Mao khắc phục được sau định danh. |
P2.06
Gen 12 dòng: cơ chế, tính chất phụ, “?”, giám định
D (ALL art)
M
GDD 5
Heo chưa định danh hiện “?”; giám định theo kênh và tỉ lệ; Xích Mao khắc phục được sau định danh.
| P2.07 | Chỉ số gốc/thưởng, cấp độ, 6 ô trang bị, 3 ô võ kỹ | C | M | GDD 8, 12 | Trang bị không tính điều kiện học võ (có test); mỗi cặp món mở 1 ô võ kỹ. |
P2.07
Chỉ số gốc/thưởng, cấp độ, 6 ô trang bị, 3 ô võ kỹ
C
M
GDD 8, 12
Trang bị không tính điều kiện học võ (có test); mỗi cặp món mở 1 ô võ kỹ.
| P2.08 | Kỹ năng đợt 1: 12/24 (3 mỗi phái) | C | M | GDD 12 | Mỗi kỹ năng một “động từ” riêng, có icon và VFX. |
P2.08
Kỹ năng đợt 1: 12/24 (3 mỗi phái)
C
M
GDD 12
Mỗi kỹ năng một “động từ” riêng, có icon và VFX.
| P2.09 | Chợ đêm trao đổi theo “món muốn nhận”, khoảng 18% ô nhận vàng, đấu giá, GTTC | D | M | GDD 20 | Sinh ô hàng theo quy tắc; mô phỏng 1000 lần cho tỉ lệ ô vàng xấp xỉ 18%. |
P2.09
Chợ đêm trao đổi theo “món muốn nhận”, khoảng 18% ô nhận vàng, đấu giá, GTTC
D
M
GDD 20
Sinh ô hàng theo quy tắc; mô phỏng 1000 lần cho tỉ lệ ô vàng xấp xỉ 18%.
| P2.10 | Lưới điện | A | M | GDD 15 | Công trình tiêu thụ/phát điện, quá tải cắt theo ưu tiên. |
P2.10
Lưới điện
A
M
GDD 15
Công trình tiêu thụ/phát điện, quá tải cắt theo ưu tiên.
| P2.11 | Framework 7 NPC trang trại + hành vi | A (owner nội dung) | M | GDD 11 | NPC có lịch làm việc, đặc tính hỗ trợ hằng ngày. |
P2.11
Framework 7 NPC trang trại + hành vi
A (owner nội dung)
M
GDD 11
NPC có lịch làm việc, đặc tính hỗ trợ hằng ngày.
| P2.12 | Framework thú cưng + 4 thú hỗ trợ | B (owner nội dung) | M | GDD 11 | Thú theo chủ, làm việc theo vai trò. |
P2.12
Framework thú cưng + 4 thú hỗ trợ
B (owner nội dung)
M
GDD 11
Thú theo chủ, làm việc theo vai trò.
| P2.13 | Inventory, vật phẩm, kho | A | M | Catalog | Kéo-thả, xếp chồng, lưu/tải. |
P2.13
Inventory, vật phẩm, kho
A
M
Catalog
Kéo-thả, xếp chồng, lưu/tải.
| P2.14 | Localization vi (en là S) | D | M | — | Toàn bộ chữ trong game qua bảng; công cụ phát hiện key thiếu. |
P2.14
Localization vi (en là S)
D
M
—
Toàn bộ chữ trong game qua bảng; công cụ phát hiện key thiếu.
| P2.15 | Asset sprint 2: gene pack còn lại, NPC còn lại, thú cưng | ALL | M | Ma trận 6 | 12/12 gen, 12/12 NPC, 7/7 thú cưng có sprite cơ bản. |
P2.15
Asset sprint 2: gene pack còn lại, NPC còn lại, thú cưng
ALL
M
Ma trận 6
12/12 gen, 12/12 NPC, 7/7 thú cưng có sprite cơ bản.
| P2.16 | Tích hợp Alpha | A | M | P2.* | Tag v0.2.0-alpha; mô phỏng headless 30 ngày không lỗi. |
P2.16
Tích hợp Alpha
A
M
P2.*
Tag v0.2.0-alpha; mô phỏng headless 30 ngày không lỗi.
7.4. P3 – Phòng thủ, quái và Boss đầy đủ (W17–W23: 25/01 – 14/03/2027)
| ID | Work item | Owner | Loại | Đầu vào | Đầu ra mong đợi |
ID
Work item
Owner
Loại
Đầu vào
Đầu ra mong đợi
| P3.01 | Bản vẽ + Phòng Xây Dựng: mở khóa, xây, nâng cấp | C | M | GDD 13 | Không có bản vẽ thì không xây được; tiến độ xây hiển thị. |
P3.01
Bản vẽ + Phòng Xây Dựng: mở khóa, xây, nâng cấp
C
M
GDD 13
Không có bản vẽ thì không xây được; tiến độ xây hiển thị.
| P3.02 | Framework 28 công trình tự động: giới hạn số lượng, nạp trước, hồi chiêu, hao mòn | C | M | GDD 13–17 | Công trình không bị quái “khắc chế”; bị giới hạn bằng chi phí vận hành đúng triết lý. |
P3.02
Framework 28 công trình tự động: giới hạn số lượng, nạp trước, hồi chiêu, hao mòn
C
M
GDD 13–17
Công trình không bị quái “khắc chế”; bị giới hạn bằng chi phí vận hành đúng triết lý.
| P3.03 | 3 công trình Bạch Vân kích hoạt thủ công | C (A, D nội dung) | M | GDD 13 | Chỉ 3 công trình này cần thao tác tay (có test bất biến). |
P3.03
3 công trình Bạch Vân kích hoạt thủ công
C (A, D nội dung)
M
GDD 13
Chỉ 3 công trình này cần thao tác tay (có test bất biến).
| P3.04 | Độ bền, hư hại, sửa chữa | C | M | GDD 18 | Sửa tốn ngân sách qua Ledger; trạng thái Damaged/Destroyed có sprite. |
P3.04
Độ bền, hư hại, sửa chữa
C
M
GDD 18
Sửa tốn ngân sách qua Ledger; trạng thái Damaged/Destroyed có sprite.
| P3.05 | 14 quái + 3 Boss: AI, pattern, telegraph | C (ALL art) | M | GDD 19 | Mỗi quái có hành vi khác biệt; Boss có ≥2 pha. |
P3.05
14 quái + 3 Boss: AI, pattern, telegraph
C (ALL art)
M
GDD 19
Mỗi quái có hành vi khác biệt; Boss có ≥2 pha.
| P3.06 | Lịch đợt lớn/nhỏ theo giai đoạn game | C | M | GDD 1.2 | Đợt lớn mỗi 6–8 ngày (đầu), 7–10 (giữa), 8–12 (cuối); có đợt nhỏ xen giữa. |
P3.06
Lịch đợt lớn/nhỏ theo giai đoạn game
C
M
GDD 1.2
Đợt lớn mỗi 6–8 ngày (đầu), 7–10 (giữa), 8–12 (cuối); có đợt nhỏ xen giữa.
| P3.07 | 5 NPC chiến đấu thuê ở chợ đêm | D (C AI) | M | GDD 11, 20 | Thuê, trả công, tham chiến chỉ khi phòng thủ. |
P3.07
5 NPC chiến đấu thuê ở chợ đêm
D (C AI)
M
GDD 11, 20
Thuê, trả công, tham chiến chỉ khi phòng thủ.
| P3.08 | Tình báo cấp 0–5, dự báo đợt, công trình thông tin | D | M | GDD 14, 23 | Độ chính xác dự báo tăng theo cấp; không lộ thông tin chưa mua. |
P3.08
Tình báo cấp 0–5, dự báo đợt, công trình thông tin
D
M
GDD 14, 23
Độ chính xác dự báo tăng theo cấp; không lộ thông tin chưa mua.
| P3.09 | 3 thú cưng canh gác | C (owner nội dung) | M | GDD 11 | Thú canh gác chỉ có đặc tính phòng thủ. |
P3.09
3 thú cưng canh gác
C (owner nội dung)
M
GDD 11
Thú canh gác chỉ có đặc tính phòng thủ.
| P3.10 | Performance pass | A | M | Profiler | 150 heo + 30 quái + 4 NPC/thú ≥60 FPS trên máy tối thiểu; GC alloc/frame ≈0 khi chơi. |
P3.10
Performance pass
A
M
Profiler
150 heo + 30 quái + 4 NPC/thú ≥60 FPS trên máy tối thiểu; GC alloc/frame ≈0 khi chơi.
| P3.11 | Asset sprint 3: toàn bộ công trình và quái của mình | ALL | M | Ma trận 6 | 31/31 công trình, 17/17 quái và Boss có đủ animation. |
P3.11
Asset sprint 3: toàn bộ công trình và quái của mình
ALL
M
Ma trận 6
31/31 công trình, 17/17 quái và Boss có đủ animation.
| P3.12 | Tích hợp, tag v0.3.0 | A | M | P3.* | Chơi 10 ngày game liên tục có 1 đợt lớn. |
P3.12
Tích hợp, tag v0.3.0
A
M
P3.*
Chơi 10 ngày game liên tục có 1 đợt lớn.
7.5. P4 – Chiều sâu (W24–W29: 15/03 – 25/04/2027)
| ID | Work item | Owner | Loại | Đầu vào | Đầu ra mong đợi |
ID
Work item
Owner
Loại
Đầu vào
Đầu ra mong đợi
| P4.01 | Bí nhân | D | M | GDD 21 | Không bao giờ nhận vàng; luôn đòi giá trị cao hơn món đưa ra (test bất biến). |
P4.01
Bí nhân
D
M
GDD 21
Không bao giờ nhận vàng; luôn đòi giá trị cao hơn món đưa ra (test bất biến).
| P4.02 | 14 sự kiện huyền bí (Đỏ/Vàng/Xanh × Cổng/Sương mù) + vật phẩm thoát | D | M | GDD 24 | Cổng: hoàn thành ≥50% thoát được bằng vật phẩm bậc Thường; Sương mù cần vật phẩm bậc cao. |
P4.02
14 sự kiện huyền bí (Đỏ/Vàng/Xanh × Cổng/Sương mù) + vật phẩm thoát
D
M
GDD 24
Cổng: hoàn thành ≥50% thoát được bằng vật phẩm bậc Thường; Sương mù cần vật phẩm bậc cao.
| P4.03 | Hệ Đặc tính + Giấy Chuyên Biệt 4 màu/6 khóa | D | M | GDD 10 | Đặc tính chuyên biệt ẩn hoàn toàn đến khi đủ 6 khóa. |
P4.03
Hệ Đặc tính + Giấy Chuyên Biệt 4 màu/6 khóa
D
M
GDD 10
Đặc tính chuyên biệt ẩn hoàn toàn đến khi đủ 6 khóa.
| P4.04 | Phù Văn, Mệnh Ấn, Nộ Khí, Huyết Tế | C | M | GDD 12 | Mở từ giữa–cuối game; khắc lên trang bị/nhân vật đúng quy tắc. |
P4.04
Phù Văn, Mệnh Ấn, Nộ Khí, Huyết Tế
C
M
GDD 12
Mở từ giữa–cuối game; khắc lên trang bị/nhân vật đúng quy tắc.
| P4.05 | Kỹ năng đợt 2: 12/24 còn lại | C | M | GDD 12 | Đủ 24 kỹ năng. |
P4.05
Kỹ năng đợt 2: 12/24 còn lại
C
M
GDD 12
Đủ 24 kỹ năng.
| P4.06 | Cổng Giữa Game + gia cường | A | M | GDD 2 | Chuyển giai đoạn chơi rõ ràng. |
P4.06
Cổng Giữa Game + gia cường
A
M
GDD 2
Chuyển giai đoạn chơi rõ ràng.
| P4.07 | 5 vật phẩm phá cooldown ngủ | A | M | GDD 9 | Chỉ Quả Mộng không có tác dụng phụ. |
P4.07
5 vật phẩm phá cooldown ngủ
A
M
GDD 9
Chỉ Quả Mộng không có tác dụng phụ.
| P4.08 | Dị Biến / Hư Thể phong ấn – thức tỉnh | B | M | GDD 5 | Tính xấu của Hư Thể chỉ thức tỉnh khi heo gặp trạng thái xấu. |
P4.08
Dị Biến / Hư Thể phong ấn – thức tỉnh
B
M
GDD 5
Tính xấu của Hư Thể chỉ thức tỉnh khi heo gặp trạng thái xấu.
| P4.09 | Hệ hội thoại + quest theo cốt truyện (ink) | A (nội dung ALL) | S | Tài liệu cốt truyện | Chuỗi mở đầu và quest đầu “Cậu tới nhận trại”. |
P4.09
Hệ hội thoại + quest theo cốt truyện (ink)
A (nội dung ALL)
S
Tài liệu cốt truyện
Chuỗi mở đầu và quest đầu “Cậu tới nhận trại”.
| P4.10 | Content complete, tag v0.4.0-beta | A | M | P4.* | Mọi nội dung trong bảng 1.2 đã có trong game. |
P4.10
Content complete, tag v0.4.0-beta
A
M
P4.*
Mọi nội dung trong bảng 1.2 đã có trong game.
7.6. P5 – Polish, UI/UX, cân bằng (W30–W32: 26/04 – 16/05/2027)
| ID | Work item | Owner | Loại | Đầu vào | Đầu ra mong đợi |
ID
Work item
Owner
Loại
Đầu vào
Đầu ra mong đợi
| P5.01 | UI/UX polish toàn bộ theo mục 5.3–5.4 | D (ALL) | M | Moodboard | Mọi màn hình dùng UI Kit; test 5 người mới chơi hoàn thành ngày đầu không cần hỏi. |
P5.01
UI/UX polish toàn bộ theo mục 5.3–5.4
D (ALL)
M
Moodboard
Mọi màn hình dùng UI Kit; test 5 người mới chơi hoàn thành ngày đầu không cần hỏi.
| P5.02 | Game feel: hit-stop, rung, particle, ánh sáng đêm trăng | C, B | M | 5.3 | So sánh trước/sau bằng video; reviewer đồng ý cảm giác tốt hơn. |
P5.02
Game feel: hit-stop, rung, particle, ánh sáng đêm trăng
C, B
M
5.3
So sánh trước/sau bằng video; reviewer đồng ý cảm giác tốt hơn.
| P5.03 | Công cụ mô phỏng kinh tế headless 30–60 ngày | D | M | Core | Báo cáo CSV: dòng tiền, mật độ, số đợt sống sót; phát hiện chiến lược áp đảo. |
P5.03
Công cụ mô phỏng kinh tế headless 30–60 ngày
D
M
Core
Báo cáo CSV: dòng tiền, mật độ, số đợt sống sót; phát hiện chiến lược áp đảo.
| P5.04 | 3 vòng playtest + khảo sát | A | M | Build | Biên bản playtest trong docs/playtest; Issue cho mọi vấn đề lớn. |
P5.04
3 vòng playtest + khảo sát
A
M
Build
Biên bản playtest trong docs/playtest; Issue cho mọi vấn đề lớn.
| P5.05 | Âm thanh SFX/BGM | ALL | S | Nguồn có license | Mỗi người phụ trách SFX cho asset của mình. |
P5.05
Âm thanh SFX/BGM
ALL
S
Nguồn có license
Mỗi người phụ trách SFX cho asset của mình.
| P5.06 | Accessibility: đổi phím, cỡ chữ, chế độ mù màu | A | M | — | Tùy chọn hoạt động và được lưu. |
P5.06
Accessibility: đổi phím, cỡ chữ, chế độ mù màu
A
M
—
Tùy chọn hoạt động và được lưu.
7.7. P6 – Release Candidate và phát hành 1.0 (W33–W34: 17/05 – 30/05/2027)
| ID | Work item | Owner | Loại | Đầu vào | Đầu ra mong đợi |
ID
Work item
Owner
Loại
Đầu vào
Đầu ra mong đợi
| P6.01 | Bug bash, sửa lỗi | ALL | M | Danh sách lỗi | 0 lỗi Blocker/Critical; ≤5 lỗi Major đã biết và có ghi chú. |
P6.01
Bug bash, sửa lỗi
ALL
M
Danh sách lỗi
0 lỗi Blocker/Critical; ≤5 lỗi Major đã biết và có ghi chú.
| P6.02 | Build phát hành, CHANGELOG, tag v1.0.0 | A | M | RC | Bản Windows chạy trên máy sạch; soak test 2 giờ không crash. |
P6.02
Build phát hành, CHANGELOG, tag v1.0.0
A
M
RC
Bản Windows chạy trên máy sạch; soak test 2 giờ không crash.
| P6.03 | Trailer, GIF, ảnh cửa hàng | B | S | Build | Bộ asset quảng bá. |
P6.03
Trailer, GIF, ảnh cửa hàng
B
S
Build
Bộ asset quảng bá.
| P6.04 | Retrospective + roadmap mùa 2 | D | M | Dữ liệu dự án | Tài liệu bài học và backlog mùa 2. |
P6.04
Retrospective + roadmap mùa 2
D
M
Dữ liệu dự án
Tài liệu bài học và backlog mùa 2.
8. Nhiệm vụ từng người: MUST và “Làm hơn mức đó thì tốt”
MUST là mức tối thiểu để được coi là hoàn thành phần việc; thiếu MUST thì giai đoạn không được nghiệm thu. STRETCH chỉ được làm khi toàn bộ MUST của giai đoạn hiện tại đã đạt DoD; STRETCH được ghi nhận như điểm cộng trong buổi review tuần.
8.1. Thành viên A – Tech Lead / Core & QA
| MUST | Làm hơn mức đó thì tốt (STRETCH) |
MUST
Làm hơn mức đó thì tốt (STRETCH)
| P0.01–P0.04, P0.11; P1.01, P1.02, P1.14, P1.15 P2.05, P2.10, P2.11, P2.13, P2.16; P3.10, P3.12 P4.06, P4.07, P4.10; P5.04, P5.06; P6.02 Farm_Core + Hub_Village (mục 6.2): P0.14, P0.15, P0.19, P1.17, P1.21, P2.17, P2.21, P2.22, P2.26, P3.13, P4.11, P5.07. Asset: 7 phòng chức năng (gồm Phòng Nghiên Cứu, Xưởng Cường Hóa), Bà Sáu, 3 NPC, 2 thú cưng, 3 gen, 8 công trình, 3 UFO + Boss Mẫu Hạm UFO, 2 cảnh sự kiện, icon HUD/thời tiết, font Review PR của D, duyệt mọi thay đổi Contracts/Save | GameCI chạy Unity test trên GitHub Actions Hệ thống replay/ghi input để tái hiện lỗi Hệ hội thoại ink + quest (P4.09) Nội thất nhà chính, mùa vụ đổi theo ngày ở Hub_Village Công cụ Editor “Validate Project” một nút |
P0.01–P0.04, P0.11; P1.01, P1.02, P1.14, P1.15
P2.05, P2.10, P2.11, P2.13, P2.16; P3.10, P3.12
P4.06, P4.07, P4.10; P5.04, P5.06; P6.02
Farm_Core + Hub_Village (mục 6.2): P0.14, P0.15, P0.19, P1.17, P1.21, P2.17, P2.21, P2.22, P2.26, P3.13, P4.11, P5.07. Asset: 7 phòng chức năng (gồm Phòng Nghiên Cứu, Xưởng Cường Hóa), Bà Sáu, 3 NPC, 2 thú cưng, 3 gen, 8 công trình, 3 UFO + Boss Mẫu Hạm UFO, 2 cảnh sự kiện, icon HUD/thời tiết, font
Review PR của D, duyệt mọi thay đổi Contracts/Save
GameCI chạy Unity test trên GitHub Actions
Hệ thống replay/ghi input để tái hiện lỗi
Hệ hội thoại ink + quest (P4.09)
Nội thất nhà chính, mùa vụ đổi theo ngày ở Hub_Village
Công cụ Editor “Validate Project” một nút
8.2. Thành viên B – World, Farm, Pig & Art Lead (người dùng tài liệu này)
| MUST | Làm hơn mức đó thì tốt (STRETCH) |
MUST
Làm hơn mức đó thì tốt (STRETCH)
| P0.05, P0.06 (chuẩn hóa map heo thả rông và An đã có) P1.03–P1.06, P1.12 P2.01–P2.04, P2.12; P4.08; P5.02 (ánh sáng) Farm_Main (mục 6.3): P0.16, P1.18, P2.18, P2.23, P3.14, P3.18, P5.08, P5.11. Asset: An đủ bộ, rig heo, 3 gen, hạ tầng trại, 3 NPC, 2 thú cưng, 8 công trình, 4 quái, 4 cảnh sự kiện, tileset, VFX thời tiết, icon sức khỏe/Sinh Cảnh Review style mọi asset; review PR của A | P1.16 ánh sáng ngày/đêm sớm Đàn heo 8 hướng, biểu cảm cảm xúc (bong bóng icon) Trang phục An thay theo trang bị Trailer/GIF (P6.03) Mô phỏng đàn bằng Burst/Jobs đạt 300 heo |
P0.05, P0.06 (chuẩn hóa map heo thả rông và An đã có)
P1.03–P1.06, P1.12
P2.01–P2.04, P2.12; P4.08; P5.02 (ánh sáng)
Farm_Main (mục 6.3): P0.16, P1.18, P2.18, P2.23, P3.14, P3.18, P5.08, P5.11. Asset: An đủ bộ, rig heo, 3 gen, hạ tầng trại, 3 NPC, 2 thú cưng, 8 công trình, 4 quái, 4 cảnh sự kiện, tileset, VFX thời tiết, icon sức khỏe/Sinh Cảnh
Review style mọi asset; review PR của A
P1.16 ánh sáng ngày/đêm sớm
Đàn heo 8 hướng, biểu cảm cảm xúc (bong bóng icon)
Trang phục An thay theo trang bị
Trailer/GIF (P6.03)
Mô phỏng đàn bằng Burst/Jobs đạt 300 heo
8.3. Thành viên C – Combat, Defense, Enemy Lead
| MUST | Làm hơn mức đó thì tốt (STRETCH) |
MUST
Làm hơn mức đó thì tốt (STRETCH)
| P0.09, P0.10; P1.07–P1.09 P2.07, P2.08; P3.01–P3.06, P3.09 P4.04, P4.05; P5.02 (game feel) Market_Town ngày + đêm (mục 6.4): P0.17, P1.19, P2.19, P3.15, P3.17, P5.09. Asset: Khoa đủ bộ, 3 gen, 3 NPC + 4 NPC chức năng chợ, 1 thú cưng, 8 công trình, 4 quái + Boss Chúa Trộm Rìu Sắt, 2 cảnh sự kiện + 2 đợt phòng thủ, icon 24 kỹ năng, VFX combat Review PR của B; chịu trách nhiệm cân bằng combat | Hiệu ứng “bản ngã” của Khoa Chế độ luyện tập/arena trong CombatSandbox Boss có pha thứ 3 Công cụ Editor vẽ vùng spawn/đường quái |
P0.09, P0.10; P1.07–P1.09
P2.07, P2.08; P3.01–P3.06, P3.09
P4.04, P4.05; P5.02 (game feel)
Market_Town ngày + đêm (mục 6.4): P0.17, P1.19, P2.19, P3.15, P3.17, P5.09. Asset: Khoa đủ bộ, 3 gen, 3 NPC + 4 NPC chức năng chợ, 1 thú cưng, 8 công trình, 4 quái + Boss Chúa Trộm Rìu Sắt, 2 cảnh sự kiện + 2 đợt phòng thủ, icon 24 kỹ năng, VFX combat
Review PR của B; chịu trách nhiệm cân bằng combat
Hiệu ứng “bản ngã” của Khoa
Chế độ luyện tập/arena trong CombatSandbox
Boss có pha thứ 3
Công cụ Editor vẽ vùng spawn/đường quái
8.4. Thành viên D – Economy, Information, Event & Data Lead
| MUST | Làm hơn mức đó thì tốt (STRETCH) |
MUST
Làm hơn mức đó thì tốt (STRETCH)
| P0.07, P0.08, P0.12; P1.10, P1.11 P2.06, P2.09, P2.14; P3.07, P3.08 P4.01–P4.03; P5.01, P5.03; P6.04 Forest_Primeval + khung sự kiện (mục 6.5): P0.18, P1.20, P2.20, P2.24, P2.25, P3.16, P4.12, P5.10. Asset: Mận, Bí nhân, 3 gen, 3 NPC, 2 thú cưng, 7 công trình, 3 quái + Boss Thực Thể Sương, 4 cảnh sự kiện, UI Kit, icon vật phẩm, Giấy Chuyên Biệt, VFX Cổng/Sương mù, minimap Review PR của C; duyệt mọi thay đổi Catalog/Balance | Bản tiếng Anh đầy đủ Animation mở Giấy kiểu gacha, minimap Vùng rừng sâu sinh ngẫu nhiên theo seed ở Forest_Primeval Dashboard kinh tế từ telemetry (biểu đồ dòng tiền) |
P0.07, P0.08, P0.12; P1.10, P1.11
P2.06, P2.09, P2.14; P3.07, P3.08
P4.01–P4.03; P5.01, P5.03; P6.04
Forest_Primeval + khung sự kiện (mục 6.5): P0.18, P1.20, P2.20, P2.24, P2.25, P3.16, P4.12, P5.10. Asset: Mận, Bí nhân, 3 gen, 3 NPC, 2 thú cưng, 7 công trình, 3 quái + Boss Thực Thể Sương, 4 cảnh sự kiện, UI Kit, icon vật phẩm, Giấy Chuyên Biệt, VFX Cổng/Sương mù, minimap
Review PR của C; duyệt mọi thay đổi Catalog/Balance
Bản tiếng Anh đầy đủ
Animation mở Giấy kiểu gacha, minimap
Vùng rừng sâu sinh ngẫu nhiên theo seed ở Forest_Primeval
Dashboard kinh tế từ telemetry (biểu đồ dòng tiền)
9. Lịch 34 tuần và các mốc nghiệm thu
Lịch chia theo sprint 2 tuần, bắt đầu Thứ Hai 05/10/2026. Mỗi sprint kết thúc bằng buổi review Chủ Nhật 20:00. Hai hạn cập nhật GitHub cố định mỗi tuần: Thứ Tư 23:59 và Chủ Nhật 23:59.
| Sprint | Tuần | Thời gian | Phase | Trọng tâm | Mốc |
Sprint
Tuần
Thời gian
Phase
Trọng tâm
Mốc
| S1 | W1–W2 | 05/10 – 18/10/2026 | P0 | Khung project, Git, CI, AGENTS.md, Art Bible, nhập map + An, catalog, Balance, GameClock | G0 – 18/10 |
S1
W1–W2
05/10 – 18/10/2026
P0
Khung project, Git, CI, AGENTS.md, Art Bible, nhập map + An, catalog, Balance, GameClock
G0 – 18/10
| S2 | W3–W4 | 19/10 – 01/11 | P1 | Save/Load, player core, đàn thả rông, animation An/Khoa, combat cơ bản | — |
S2
W3–W4
19/10 – 01/11
P1
Save/Load, player core, đàn thả rông, animation An/Khoa, combat cơ bản
—
| S3 | W5–W6 | 02/11 – 15/11 | P1 | Vòng đời heo, lưới đặt công trình, chợ ngày, wave nhỏ, HUD | — |
S3
W5–W6
02/11 – 15/11
P1
Vòng đời heo, lưới đặt công trình, chợ ngày, wave nhỏ, HUD
—
| S4 | W7–W8 | 16/11 – 29/11 | P1 | Asset sprint 1, debug panel, tích hợp Vertical Slice | G1 – 29/11 |
S4
W7–W8
16/11 – 29/11
P1
Asset sprint 1, debug panel, tích hợp Vertical Slice
G1 – 29/11
| S5 | W9–W10 | 30/11 – 13/12 | P2 | Tinh thần/Hòa nhập, chỉ số/trang bị, chợ đêm, thời tiết | — |
S5
W9–W10
30/11 – 13/12
P2
Tinh thần/Hòa nhập, chỉ số/trang bị, chợ đêm, thời tiết
—
| S6 | W11–W12 | 14/12 – 27/12 | P2 | Bầy Đàn/thủ lĩnh, gen & giám định, kỹ năng đợt 1, lưới điện | — |
S6
W11–W12
14/12 – 27/12
P2
Bầy Đàn/thủ lĩnh, gen & giám định, kỹ năng đợt 1, lưới điện
—
| S7 | W13–W14 | 28/12 – 10/01/2027 | P2 | Sinh sản, xác, Sinh Cảnh, sức khỏe, inventory, NPC/thú cưng framework | — |
S7
W13–W14
28/12 – 10/01/2027
P2
Sinh sản, xác, Sinh Cảnh, sức khỏe, inventory, NPC/thú cưng framework
—
| S8 | W15–W16 | 11/01 – 24/01 | P2 | Localization, asset sprint 2, tích hợp Alpha | G2 – 24/01 |
S8
W15–W16
11/01 – 24/01
P2
Localization, asset sprint 2, tích hợp Alpha
G2 – 24/01
| S9 | W17–W18 | 25/01 – 07/02 | P3 | Bản vẽ + Phòng Xây Dựng, framework công trình (Tết Nguyên đán khoảng 06/02) | — |
S9
W17–W18
25/01 – 07/02
P3
Bản vẽ + Phòng Xây Dựng, framework công trình (Tết Nguyên đán khoảng 06/02)
—
| S10 | W19–W20 | 08/02 – 21/02 | P3 | Bạch Vân, độ bền/sửa, quái thường | — |
S10
W19–W20
08/02 – 21/02
P3
Bạch Vân, độ bền/sửa, quái thường
—
| S11 | W21–W22 | 22/02 – 07/03 | P3 | Boss, lịch đợt, NPC chiến đấu, tình báo, thú canh gác | — |
S11
W21–W22
22/02 – 07/03
P3
Boss, lịch đợt, NPC chiến đấu, tình báo, thú canh gác
—
| S12 | W23 | 08/03 – 14/03 | P3 | Performance pass, asset sprint 3, tích hợp | G3 – 14/03 |
S12
W23
08/03 – 14/03
P3
Performance pass, asset sprint 3, tích hợp
G3 – 14/03
| S13 | W24–W25 | 15/03 – 28/03 | P4 | Bí nhân, Phù Văn/Mệnh Ấn, Đặc tính & Giấy | — |
S13
W24–W25
15/03 – 28/03
P4
Bí nhân, Phù Văn/Mệnh Ấn, Đặc tính & Giấy
—
| S14 | W26–W27 | 29/03 – 11/04 | P4 | 14 sự kiện huyền bí, kỹ năng đợt 2, Cổng Giữa Game | — |
S14
W26–W27
29/03 – 11/04
P4
14 sự kiện huyền bí, kỹ năng đợt 2, Cổng Giữa Game
—
| S15 | W28–W29 | 12/04 – 25/04 | P4 | Hư Thể, vật phẩm ngủ, content complete | G4 – 25/04 |
S15
W28–W29
12/04 – 25/04
P4
Hư Thể, vật phẩm ngủ, content complete
G4 – 25/04
| S16 | W30–W32 | 26/04 – 16/05 | P5 | UI/UX polish, game feel, mô phỏng kinh tế, playtest | G5 – 16/05 |
S16
W30–W32
26/04 – 16/05
P5
UI/UX polish, game feel, mô phỏng kinh tế, playtest
G5 – 16/05
| S17 | W33–W34 | 17/05 – 30/05 | P6 | Bug bash, RC, phát hành 1.0 | G6 – 30/05/2027 |
S17
W33–W34
17/05 – 30/05
P6
Bug bash, RC, phát hành 1.0
G6 – 30/05/2027
Tuần Tết (W18–W19): quy tắc 2 cập nhật/tuần vẫn giữ nguyên theo mặc định. Nếu cả 4 người đồng ý trước ít nhất 2 tuần, nhóm có thể ghi một ADR miễn trừ còn 1 cập nhật/tuần cho đúng 2 tuần này; khi đó mốc G3 giữ nguyên và khối lượng được bù ở S10–S11.
10. Các yếu tố kết nối giữa module (Contracts)
GDD yêu cầu “tác động chéo tối đa”, nên kết nối giữa các hệ là phần dễ vỡ nhất. Mọi giao tiếp giữa module đi qua interface trong PigTycoon.Contracts hoặc sự kiện trên Event Bus. Không module nào gọi thẳng class nội bộ của module khác. Thay đổi một contract cần ADR, sự đồng ý của A và của owner bên tiêu thụ.
| Contract | Cung cấp | Sử dụng | Mô tả đầu vào → đầu ra |
Contract
Cung cấp
Sử dụng
Mô tả đầu vào → đầu ra
| ITimeService | A | Tất cả | Đọc ngày, giờ, buổi; sự kiện HourTicked, DayStarted (06:00); Pause/Resume khi vào/ra đợt quái. |
ITimeService
A
Tất cả
Đọc ngày, giờ, buổi; sự kiện HourTicked, DayStarted (06:00); Pause/Resume khi vào/ra đợt quái.
| ISaveService / ISaveable | A | Tất cả | Mỗi module tự ghi/đọc state của mình theo khóa riêng, có số phiên bản. |
ISaveService / ISaveable
A
Tất cả
Mỗi module tự ghi/đọc state của mình theo khóa riêng, có số phiên bản.
| IDataCatalog / IBalance | D | Tất cả | Tra định nghĩa theo ID; đọc hằng số cân bằng. Không ai đọc file dữ liệu trực tiếp. |
IDataCatalog / IBalance
D
Tất cả
Tra định nghĩa theo ID; đọc hằng số cân bằng. Không ai đọc file dữ liệu trực tiếp.
| IHerdQuery | B | C, D | Số heo, mật độ, Áp Lực Sự Kiện, số heo già, số xác, trạng thái Bầy Đàn → quái chọn mục tiêu (C), sự kiện và giá chợ (D). |
IHerdQuery
B
C, D
Số heo, mật độ, Áp Lực Sự Kiện, số heo già, số xác, trạng thái Bầy Đàn → quái chọn mục tiêu (C), sự kiện và giá chợ (D).
| IHealthQuery | B | D | Tình trạng dịch bệnh, Sinh Cảnh → điều chỉnh giá và kích hoạt sự kiện. |
IHealthQuery
B
D
Tình trạng dịch bệnh, Sinh Cảnh → điều chỉnh giá và kích hoạt sự kiện.
| IEconomyLedger | D | A, B, C | TrySpend/Earn với lý do; mua cám (B), đạn/nạp trước/sửa chữa (C), thuê NPC; ngân sách là van khóa. |
IEconomyLedger
D
A, B, C
TrySpend/Earn với lý do; mua cám (B), đạn/nạp trước/sửa chữa (C), thuê NPC; ngân sách là van khóa.
| IWaveSchedule | C | D, A | Lịch đợt quái thật; Intel (D) trả về bản dự báo đã làm mờ theo cấp tình báo 0–5. |
IWaveSchedule
C
D, A
Lịch đợt quái thật; Intel (D) trả về bản dự báo đã làm mờ theo cấp tình báo 0–5.
| IIntelService | D | Ui, C | Dự báo đợt, thời tiết, giá chợ theo cấp tình báo. |
IIntelService
D
Ui, C
Dự báo đợt, thời tiết, giá chợ theo cấp tình báo.
| IPowerGrid | A | B, C | Đăng ký nguồn/tiêu thụ điện; nhận trạng thái cấp điện. |
IPowerGrid
A
B, C
Đăng ký nguồn/tiêu thụ điện; nhận trạng thái cấp điện.
| IPlayerStats | C | A, D | Chỉ số gốc/thưởng, Stamina, trang bị; vật phẩm (D) tác động qua đây. |
IPlayerStats
C
A, D
Chỉ số gốc/thưởng, Stamina, trang bị; vật phẩm (D) tác động qua đây.
| IEventBus | A | Tất cả | Sự kiện dạng struct: PigBorn, PigDied, PigSold, DayStarted, WeatherChanged, WaveStarted, WaveEnded, BuildingDamaged, BuildingDestroyed, OccultEventTriggered. |
IEventBus
A
Tất cả
Sự kiện dạng struct: PigBorn, PigDied, PigSold, DayStarted, WeatherChanged, WaveStarted, WaveEnded, BuildingDamaged, BuildingDestroyed, OccultEventTriggered.
Mẫu code contract để agent tham chiếu:
| namespace PigTycoon.Contracts { public interface IEventBus { void Publish<T>(in T evt) where T : struct; System.IDisposable Subscribe<T>(System.Action<T> handler) where T : struct; }  public readonly struct PigDied { public readonly int PigId; public readonly string GeneId; public readonly DeathCause Cause; public PigDied(int pigId, string geneId, DeathCause cause) { PigId = pigId; GeneId = geneId; Cause = cause; } }  public interface IEconomyLedger { long Gold { get; } bool TrySpend(long amount, LedgerReason reason, string sourceId); // false nếu không đủ, không trừ âm void Earn(long amount, LedgerReason reason, string sourceId); }  public interface IHerdQuery { int LiveCount { get; } int OldCount { get; } int CarcassCount { get; } float DensityRatio { get; }   // số heo / sức chứa sống int EventPressure { get; }   // 0–100 bool IsHerdState { get; }    // Trạng thái Bầy Đàn } } |
namespace PigTycoon.Contracts
{
public interface IEventBus
{
void Publish<T>(in T evt) where T : struct;
System.IDisposable Subscribe<T>(System.Action<T> handler) where T : struct;
}
public readonly struct PigDied
{
public readonly int PigId; public readonly string GeneId; public readonly DeathCause Cause;
public PigDied(int pigId, string geneId, DeathCause cause) { PigId = pigId; GeneId = geneId; Cause = cause; }
}
public interface IEconomyLedger
{
long Gold { get; }
bool TrySpend(long amount, LedgerReason reason, string sourceId); // false nếu không đủ, không trừ âm
void Earn(long amount, LedgerReason reason, string sourceId);
}
public interface IHerdQuery
{
int LiveCount { get; } int OldCount { get; } int CarcassCount { get; }
float DensityRatio { get; }   // số heo / sức chứa sống
int EventPressure { get; }   // 0–100
bool IsHerdState { get; }    // Trạng thái Bầy Đàn
}
}
11. Quy tắc làm việc trên GitHub
11.1. Nhịp cập nhật bắt buộc
| Quy tắc | Chi tiết |
Quy tắc
Chi tiết
| Tần suất tối thiểu | Mỗi người ít nhất 1 cập nhật hợp lệ sau mỗi 3–4 ngày và ít nhất 2 cập nhật hợp lệ mỗi tuần, đẩy lên branch của mình trong repo https://github.com/danhnee/Pig-tycoon và mở/cập nhật PR vào develop. |
Tần suất tối thiểu
Mỗi người ít nhất 1 cập nhật hợp lệ sau mỗi 3–4 ngày và ít nhất 2 cập nhật hợp lệ mỗi tuần, đẩy lên branch của mình trong repo https://github.com/danhnee/Pig-tycoon và mở/cập nhật PR vào develop.
| Hạn cố định | Thứ Tư 23:59 và Chủ Nhật 23:59 (khoảng cách 3 và 4 ngày). Cập nhật sớm hơn vẫn được tính. |
Hạn cố định
Thứ Tư 23:59 và Chủ Nhật 23:59 (khoảng cách 3 và 4 ngày). Cập nhật sớm hơn vẫn được tính.
| Điều kiện được chấp nhận | Cập nhật chỉ hợp lệ khi hoàn thành ít nhất 1 work item hoặc checkpoint WBS (có ID), demo được trong Unity, CI xanh, có bằng chứng (ảnh/GIF/log test). |
Điều kiện được chấp nhận
Cập nhật chỉ hợp lệ khi hoàn thành ít nhất 1 work item hoặc checkpoint WBS (có ID), demo được trong Unity, CI xanh, có bằng chứng (ảnh/GIF/log test).
| Không được tính | Chỉ đổi tên, format code, thêm TODO, asset chưa import vào Unity, code không biên dịch, commit chia nhỏ để đủ số lần, thay đổi do agent tạo mà chủ PR không giải thích được. |
Không được tính
Chỉ đổi tên, format code, thêm TODO, asset chưa import vào Unity, code không biên dịch, commit chia nhỏ để đủ số lần, thay đổi do agent tạo mà chủ PR không giải thích được.
| Theo dõi | D cập nhật bảng “Update Tracker” trên GitHub Projects mỗi Thứ Năm và Thứ Hai. Thiếu 1 hạn: nhắc nhở; thiếu 2 hạn liên tiếp: họp 1-1 và chia lại việc; thiếu 3 hạn trong 1 giai đoạn: chuyển một phần MUST cho người dự phòng. |
Theo dõi
D cập nhật bảng “Update Tracker” trên GitHub Projects mỗi Thứ Năm và Thứ Hai. Thiếu 1 hạn: nhắc nhở; thiếu 2 hạn liên tiếp: họp 1-1 và chia lại việc; thiếu 3 hạn trong 1 giai đoạn: chuyển một phần MUST cho người dự phòng.
11.2. Branch, commit, PR
main: chỉ chứa bản đã qua cổng nghiệm thu (G0…G6), gắn tag v0.x.y. develop: nhánh tích hợp. Không ai push trực tiếp vào main/develop.
Branch làm việc: feat/<A|B|C|D>/<WBS-ID>-<slug>, ví dụ feat/B/P1.03-herd-flowfield. Sửa lỗi: fix/<người>/<issue>-<slug>. Art: art/<người>/<WBS-ID>-<slug>.
Commit và tiêu đề PR: [WBS-ID][Người] mô tả ngắn, ví dụ [P1.03][B] Flow-field cho đàn thả rông + test cổng. Commit do agent hỗ trợ thêm dòng cuối “Agent-Assisted: <công cụ>”.
Rebase lên develop trước khi xin review; PR không quá 400 dòng code thay đổi (không tính asset/test). Lớn hơn thì tách checkpoint.
Merge kiểu squash vào develop; xóa branch sau merge.
11.3. Mẫu PR bắt buộc (.github/PULL_REQUEST_TEMPLATE.md)
| Mục | Nội dung phải điền |
Mục
Nội dung phải điền
| WBS | ID + checkpoint (.a/.b/.c), loại M/S |
WBS
ID + checkpoint (.a/.b/.c), loại M/S
| Đã làm | Đầu ra hoàn tất, liên hệ với “Đầu ra mong đợi” trong WBS |
Đã làm
Đầu ra hoàn tất, liên hệ với “Đầu ra mong đợi” trong WBS
| Cách test trong Unity | Scene, các bước, kết quả mong đợi và kết quả thật |
Cách test trong Unity
Scene, các bước, kết quả mong đợi và kết quả thật
| Bằng chứng | Ảnh/GIF, log tools/verify, kết quả EditMode/PlayMode test |
Bằng chứng
Ảnh/GIF, log tools/verify, kết quả EditMode/PlayMode test
| Agent-Assisted | Agent đã làm phần nào; chủ PR đã đọc, sửa, hiểu phần nào; báo cáo Self-Review của agent (mục 17.5) |
Agent-Assisted
Agent đã làm phần nào; chủ PR đã đọc, sửa, hiểu phần nào; báo cáo Self-Review của agent (mục 17.5)
| Ảnh hưởng chéo | Contract/Save/Balance/Art Bible có bị đổi không; ADR nếu có |
Ảnh hưởng chéo
Contract/Save/Balance/Art Bible có bị đổi không; ADR nếu có
| Reviewer | Theo vòng: A→B, B→C, C→D, D→A (người bên trái được người bên phải review); thêm A nếu đổi kiến trúc, B nếu đổi Art Bible, D nếu đổi dữ liệu |
Reviewer
Theo vòng: A→B, B→C, C→D, D→A (người bên trái được người bên phải review); thêm A nếu đổi kiến trúc, B nếu đổi Art Bible, D nếu đổi dữ liệu
11.4. Review và CI
Reviewer phải tự kéo branch, mở Unity, chạy scene test, không chỉ đọc diff. Thời hạn review: 24 giờ.
CI (GitHub Actions) chạy trên mỗi PR: kiểm tra tên file và thư mục, tiêu đề PR có WBS-ID, dotnet test của Core, kiểm tra không có file .meta bị thiếu/mồ côi, kích thước file không LFS ≤ 1 MB. Unity EditMode/PlayMode qua GameCI khi nhóm có Unity license dùng cho CI (STRETCH).
CODEOWNERS: thư mục module gắn owner tương ứng để GitHub tự gán reviewer.
11.5. Cấu hình Git cho Unity
Editor Settings: Version Control = Visible Meta Files; Asset Serialization = Force Text.
.gitignore chuẩn Unity (Library, Temp, Obj, Build, Logs, UserSettings, *.csproj, *.sln) + artifacts/ + .vs/ .idea/.
.gitattributes: *.png, *.aseprite, *.psd, *.wav, *.ogg, *.ttf filter=lfs; *.unity, *.prefab, *.asset merge=unityyamlmerge (dùng UnityYAMLMerge làm merge tool).
Không bao giờ xóa hoặc tạo .meta bằng tay; di chuyển file trong Unity hoặc di chuyển cả cặp file + .meta.
Theo dõi dung lượng Git LFS của tài khoản GitHub; nén PNG không mất dữ liệu trước khi commit.
12. Quy trình dùng Agent kết hợp Unity (Unity tắt khi agent chạy)
12.1. Nguyên tắc: agent làm việc khi Unity đóng, con người mở Unity để kiểm
Unity Editor tốn nhiều RAM/CPU, tự import lại mỗi khi agent sửa file, và khóa thư mục Library khiến không chạy được chế độ batch. Vì vậy mặc định của dự án: khi agent đang chạy thì Unity Editor TẮT. Agent chỉ sửa file văn bản (C#, asmdef, JSON, USS/UXML, Markdown) và tự kiểm bằng unit test chạy ngoài Unity. Khi agent xong, người làm mới mở Unity để import, xem Console, chơi thử, gắn prefab/scene bằng tay.
| Chế độ | Unity | Dùng khi nào – được làm gì |
Chế độ
Unity
Dùng khi nào – được làm gì
| 1. Agent-only (mặc định) | Tắt | Viết/sửa logic Core, Runtime script, test, dữ liệu, tài liệu. Kiểm bằng dotnet test (vài giây). Không chạm file scene/prefab/.meta. |
1. Agent-only (mặc định)
Tắt
Viết/sửa logic Core, Runtime script, test, dữ liệu, tài liệu. Kiểm bằng dotnet test (vài giây). Không chạm file scene/prefab/.meta.
| 2. Batch verify | Tắt (chạy headless) | Cuối phiên agent hoặc trước khi commit: Unity chạy ẩn để biên dịch và chạy EditMode test rồi tự thoát. Nặng trong 1–3 phút nhưng không có giao diện. |
2. Batch verify
Tắt (chạy headless)
Cuối phiên agent hoặc trước khi commit: Unity chạy ẩn để biên dịch và chạy EditMode test rồi tự thoát. Nặng trong 1–3 phút nhưng không có giao diện.
| 3. Human check | Mở | Agent đã dừng. Người làm mở Unity, đợi import, kiểm Console = 0 lỗi, chơi thử scene, gắn component/prefab, chụp bằng chứng. |
3. Human check
Mở
Agent đã dừng. Người làm mở Unity, đợi import, kiểm Console = 0 lỗi, chơi thử scene, gắn component/prefab, chụp bằng chứng.
| 4. MCP (tùy chọn) | Mở | Chỉ khi cần agent thao tác trực tiếp scene/prefab qua MCP for Unity. Giới hạn phiên ngắn, bật nhóm công cụ cần thiết, commit kiểm từng bước. |
4. MCP (tùy chọn)
Mở
Chỉ khi cần agent thao tác trực tiếp scene/prefab qua MCP for Unity. Giới hạn phiên ngắn, bật nhóm công cụ cần thiết, commit kiểm từng bước.
12.2. Quy trình 10 bước cho mỗi work item
Kéo develop mới nhất, tạo branch feat/<Người>/<WBS-ID>-<slug>. Đóng Unity Editor và Unity Hub.
Tạo Task Card trong docs/agent/tasks/<WBS-ID>-<slug>.md theo mẫu ở mục 13.3 (đầu vào, đầu ra, file được phép sửa, tiêu chí test).
Mở IDE có agent (Antigravity, Codex, Claude Code, Cursor, VS Code + Copilot…). Agent tự nạp AGENTS.md; nếu IDE không tự nạp thì dán nội dung AGENTS.md làm tin nhắn đầu tiên.
Gửi Task Card. Yêu cầu agent trả lời bằng bản tóm tắt hiểu biết và kế hoạch trước khi sửa code; đọc kỹ và duyệt kế hoạch.
Agent viết test trước (test phải đỏ), rồi viết code cho test xanh.
Agent chạy tools/verify (chế độ 1). Không đạt thì agent tự sửa tối đa 3 vòng; quá 3 vòng thì dừng, báo cáo.
Agent nộp báo cáo Self-Review (mục 17.5). Người làm đọc toàn bộ diff, hỏi lại chỗ chưa hiểu.
Đóng phiên agent. Chạy tools/verify -Unity (chế độ 2) nếu đụng code Runtime.
Mở Unity (chế độ 3): kiểm Console, chơi scene, gắn prefab/scene bằng tay, chụp ảnh/GIF (Unity Recorder).
Commit theo quy ước, push, mở PR theo mẫu, gắn reviewer.
12.3. Lệnh kiểm tra khi Unity tắt
| # 1) Test logic thuần (vài giây, không cần Unity) dotnet test tools/DomainTests/DomainTests.csproj -c Release --nologo  # 2) Unity headless: chạy EditMode test (Editor phải đang ĐÓNG) "%UNITY_EXE%" -batchmode -nographics -projectPath game/PigTycoon -runTests -testPlatform EditMode -testResults artifacts/editmode.xml -logFile artifacts/unity-editmode.log  # 3) Unity headless: chỉ biên dịch + validator dự án rồi thoát "%UNITY_EXE%" -batchmode -nographics -quit -projectPath game/PigTycoon -executeMethod PigTycoon.EditorTools.Ci.ProjectValidator.Run -logFile artifacts/unity-validate.log  # Gộp tất cả: pwsh tools/verify.ps1 [-Unity]   (macOS/Linux: bash tools/verify.sh [--unity]) |
# 1) Test logic thuần (vài giây, không cần Unity)
dotnet test tools/DomainTests/DomainTests.csproj -c Release --nologo
# 2) Unity headless: chạy EditMode test (Editor phải đang ĐÓNG)
"%UNITY_EXE%" -batchmode -nographics -projectPath game/PigTycoon -runTests -testPlatform EditMode -testResults artifacts/editmode.xml -logFile artifacts/unity-editmode.log
# 3) Unity headless: chỉ biên dịch + validator dự án rồi thoát
"%UNITY_EXE%" -batchmode -nographics -quit -projectPath game/PigTycoon -executeMethod PigTycoon.EditorTools.Ci.ProjectValidator.Run -logFile artifacts/unity-validate.log
# Gộp tất cả: pwsh tools/verify.ps1 [-Unity]   (macOS/Linux: bash tools/verify.sh [--unity])
12.4. Cấu hình từng IDE agent (một nguồn: AGENTS.md)
| Công cụ | Cách nạp prompt chung |
Công cụ
Cách nạp prompt chung
| OpenAI Codex (CLI/IDE) | Tự đọc AGENTS.md ở gốc repo. Chạy agent tại thư mục gốc repo. |
OpenAI Codex (CLI/IDE)
Tự đọc AGENTS.md ở gốc repo. Chạy agent tại thư mục gốc repo.
| Google Antigravity | Thêm rule cấp workspace trỏ tới AGENTS.md (hoặc tạo GEMINI.md chỉ chứa dòng tham chiếu AGENTS.md). Khi bắt đầu phiên, yêu cầu agent đọc AGENTS.md trước. |
Google Antigravity
Thêm rule cấp workspace trỏ tới AGENTS.md (hoặc tạo GEMINI.md chỉ chứa dòng tham chiếu AGENTS.md). Khi bắt đầu phiên, yêu cầu agent đọc AGENTS.md trước.
| Claude Code | CLAUDE.md chỉ chứa dòng @AGENTS.md để import. |
Claude Code
CLAUDE.md chỉ chứa dòng @AGENTS.md để import.
| GitHub Copilot (VS Code/Rider) | .github/copilot-instructions.md tham chiếu AGENTS.md; bật chế độ Agent. |
GitHub Copilot (VS Code/Rider)
.github/copilot-instructions.md tham chiếu AGENTS.md; bật chế độ Agent.
| Cursor / Windsurf | .cursor/rules/pigtycoon.mdc (alwaysApply) tham chiếu AGENTS.md. |
Cursor / Windsurf
.cursor/rules/pigtycoon.mdc (alwaysApply) tham chiếu AGENTS.md.
| Chat agent không đọc file | Dán nguyên văn AGENTS.md làm tin nhắn đầu tiên, rồi mới gửi Task Card. |
Chat agent không đọc file
Dán nguyên văn AGENTS.md làm tin nhắn đầu tiên, rồi mới gửi Task Card.
Không ai được tạo rule riêng mâu thuẫn với AGENTS.md. Các file CLAUDE.md, GEMINI.md, copilot-instructions.md, .mdc chỉ được chứa tham chiếu, không chứa nội dung riêng.
12.5. MCP for Unity (chế độ 4, tùy chọn)
Gói đề xuất: MCP for Unity (CoplayDev/unity-mcp, giấy phép MIT) – cài qua Package Manager bằng git URL; cần Python 3.10+ và uv. Hoặc Unity MCP chính thức trong gói AI Assistant (Unity 6, cần gói AI của Unity).
Chỉ dùng cho việc agent cần thấy/sửa scene, prefab, đọc Console. Tắt server MCP khi xong để giải phóng tài nguyên.
Sau mỗi phiên MCP: xem diff Git của file .unity/.prefab; nếu diff YAML lớn bất thường thì hủy và làm tay.
Gói MCP chỉ cài trong máy cá nhân (đặt trong Packages/manifest qua UserSettings hoặc không commit) cho tới khi có ADR thống nhất đưa vào project.
12.6. Mẹo giảm nặng máy
Khi buộc phải mở Unity cùng agent: tắt Preferences → Asset Pipeline → Auto Refresh; nhấn Ctrl+R khi muốn import.
Bật Enter Play Mode Options (tắt Domain Reload) – code phải reset static thủ công (agent phải tuân theo).
Chia asmdef nhỏ để chỉ biên dịch lại module bị sửa; đặt project trên SSD; loại thư mục Library khỏi phần mềm diệt virus quét thời gian thực.
Đóng Unity Hub, trình duyệt nặng khi chạy batch; dùng scene sandbox nhỏ thay vì Farm_Main khi thử nghiệm.
13. Prompt chung cho Agent (AGENTS.md) – bắt buộc
13.1. Quy định sử dụng
Cả 4 thành viên bắt buộc nạp nguyên văn prompt dưới đây (file AGENTS.md ở gốc repo) vào mọi phiên agent làm việc cho dự án. Phiên nào không có prompt này thì kết quả không được merge.
Chỉ thay phần trong ngoặc nhọn {…} bằng thông tin của phiên (người, WBS-ID). Không xóa, không rút gọn quy tắc.
A là owner của AGENTS.md; sửa đổi phải qua PR có 2 người duyệt và tăng số phiên bản ở dòng đầu.
13.2. Nội dung AGENTS.md (phiên bản 1.0)
| # AGENTS.md — PIG TYCOON — Agent Contract v1.0 ## 0. Phiên làm việc Thành viên: {A|B|C|D} • WBS: {P?.??[.a]} • Task Card: docs/agent/tasks/{WBS-ID}-{slug}.md ## 1. Vai trò Bạn là kỹ sư Unity/C# cấp senior và người kiểm soát chất lượng, hỗ trợ một thành viên nhóm 4 người làm game Pig Tycoon (tycoon trang trại heo thả rông + thủ thành có nhân vật điều khiển, pixel art top-down 3/4). Ưu tiên: đúng GDD > chất lượng > tốc độ. ## 2. Nguồn sự thật (theo thứ tự) 1) docs/gdd/ GDD v6.0  2) Assets/_Project/Data/Balance (Sổ thông số chuẩn)  3) docs/handbook (Sổ tay dự án v3.0)  4) code hiện có. Nếu mâu thuẫn hoặc thiếu thông tin: DỪNG và hỏi, liệt kê các phương án. Không đoán luật game. ## 3. Bối cảnh kỹ thuật Unity 6000.3 LTS, URP 2D, Pixel Perfect, 16 PPU, 1 unit = 1 m, Ô Xây 2x2 m, Ô Đất 10x10 m. 1 ngày game = 16 phút thực, 1 giờ = 40 giây, chốt ngày 06:00. Input System, UI Toolkit, Addressables, Localization, Unity Test Framework (NUnit). Monorepo: game/PigTycoon. Tầng: Core (thuần C#, noEngineReferences) → Contracts → Runtime module → Ui. Giao tiếp chéo module CHỈ qua Contracts/EventBus. ## 4. Quy tắc cứng R1  Không đổi luật, số cân bằng, số lượng nội dung của GDD. Số liệu chỉ đọc từ Balance/ScriptableObject, kèm chú thích // GDD <mục>. R2  Unity Editor đang TẮT. Chỉ tạo/sửa: .cs .asmdef .json .uss .uxml .md .ps1 .sh .yml. KHÔNG tạo/sửa .unity .prefab .asset .meta .anim .controller .mat trừ khi Task Card cho phép rõ ràng. Phần cần làm trong Editor: ghi thành checklist “Việc cần làm trong Unity”. R3  Chỉ sửa file trong phạm vi Task Card. Cần sửa ngoài phạm vi → hỏi trước. Không đổi public API/Contracts khi không có ADR. R4  Đặt tên: <Prefix>_<Domain>_<Subject>[_<Variant>][_<State>][_<Dir>][_<NN>].<ext> cho asset; script: TênClass.cs PascalCase; namespace PigTycoon.<Module>; ID dữ liệu <domain>.<category>.<snake_name>; chỉ ký tự ASCII không dấu, không dấu cách, ≤64 ký tự; không _final/_v2/_new. File mới phải nằm đúng thư mục module; không tự tạo module/thư mục gốc mới. R5  Logic game đặt trong Core, không using UnityEngine, có unit test NUnit chạy được bằng dotnet test và Unity EditMode. R6  Test-first: viết test thể hiện yêu cầu GDD (dùng số thật của GDD), xác nhận test đỏ, rồi mới code. Cấm test rỗng, cấm [Ignore], cấm sửa test cho khớp code sai. Mỗi hàm public trong Core có ít nhất 1 test thường + 1 test biên. R7  Hiệu năng: không cấp phát bộ nhớ trong Update/FixedUpdate, không LINQ/foreach trên interface trong vòng lặp nóng, không Find*/GetComponent mỗi frame, dùng pooling (UnityEngine.Pool). Mục tiêu: 150 heo + 30 quái + 4 NPC/thú ≥ 60 FPS. R8  Không chuỗi hiển thị cứng: dùng key Localization. Không đường dẫn tuyệt đối. Reset static thủ công (Domain Reload tắt). R9  Chỉ dùng package/thư viện trong danh sách cho phép (Sổ tay mục 15). Thêm mới → đề xuất kèm license, không tự cài. R10 Không dùng code/asset không rõ license; trích nguồn vào THIRD_PARTY_NOTICES.md. Tuyệt đối không dùng asset của Momodora hay game khác. R11 Không chạy lệnh phá hủy (git reset --hard, git push, xóa thư mục, sửa .git); không đọc/ghi ngoài repo; không lộ khóa/API key. R12 Trung thực: không tuyên bố “đã chạy/đã pass” nếu chưa chạy thật; dán output lệnh. Không chắc thì nói không chắc. ## 5. Quy trình bắt buộc B1 Đọc Task Card + file liên quan → trả lời: (a) hiểu yêu cầu, (b) mục GDD liên quan, (c) kế hoạch ≤10 bước, (d) file sẽ sửa, (e) rủi ro. Chờ duyệt. B2 Viết test → chạy, xác nhận đỏ.  B3 Code tối thiểu cho test xanh, rồi refactor.  B4 Chạy: pwsh tools/verify.ps1 (dán kết quả). B5 Tự sửa tối đa 3 vòng; vẫn lỗi → dừng, mô tả nguyên nhân và đề xuất.  B6 Self-Review theo checklist mục 6.  B7 Báo cáo theo mục 7. ## 6. Self-Review (trả lời từng câu, có bằng chứng) [ ] Mỗi tiêu chí chấp nhận trong Task Card ↔ test nào chứng minh?  [ ] Nếu đổi 1 hằng số/dấu so sánh trong code, test nào sẽ đỏ? [ ] Các trường hợp biên: 0, âm, tối đa, null, danh sách rỗng, lưu/tải giữa chừng, tạm dừng thời gian?  [ ] Có vi phạm bất biến GDD mục 27? [ ] Có cấp phát/GC trong vòng lặp nóng?  [ ] Tên file/thư mục đúng R4?  [ ] Có phần nào chỉ “giả lập cho pass” (hard-code kết quả)? → phải = KHÔNG. [ ] 3 điểm yếu nhất của lời giải này là gì và cách cải thiện? ## 7. Mẫu báo cáo cuối phiên TÓM TẮT • FILE ĐÃ ĐỔI (đường dẫn + lý do) • TEST (tên test + kết quả, dán output verify) • SELF-REVIEW (mục 6) • VIỆC CẦN LÀM TRONG UNITY (checklist cho người: gắn component, tạo prefab, chỉnh scene) • RỦI RO/CÂU HỎI MỞ • ĐỀ XUẤT COMMIT: [WBS-ID][Người] … |
# AGENTS.md — PIG TYCOON — Agent Contract v1.0
## 0. Phiên làm việc
Thành viên: {A|B|C|D} • WBS: {P?.??[.a]} • Task Card: docs/agent/tasks/{WBS-ID}-{slug}.md
## 1. Vai trò
Bạn là kỹ sư Unity/C# cấp senior và người kiểm soát chất lượng, hỗ trợ một thành viên nhóm 4 người làm game Pig Tycoon
(tycoon trang trại heo thả rông + thủ thành có nhân vật điều khiển, pixel art top-down 3/4). Ưu tiên: đúng GDD > chất lượng > tốc độ.
## 2. Nguồn sự thật (theo thứ tự)
1) docs/gdd/ GDD v6.0  2) Assets/_Project/Data/Balance (Sổ thông số chuẩn)  3) docs/handbook (Sổ tay dự án v3.0)  4) code hiện có.
Nếu mâu thuẫn hoặc thiếu thông tin: DỪNG và hỏi, liệt kê các phương án. Không đoán luật game.
## 3. Bối cảnh kỹ thuật
Unity 6000.3 LTS, URP 2D, Pixel Perfect, 16 PPU, 1 unit = 1 m, Ô Xây 2x2 m, Ô Đất 10x10 m. 1 ngày game = 16 phút thực, 1 giờ = 40 giây,
chốt ngày 06:00. Input System, UI Toolkit, Addressables, Localization, Unity Test Framework (NUnit). Monorepo: game/PigTycoon.
Tầng: Core (thuần C#, noEngineReferences) → Contracts → Runtime module → Ui. Giao tiếp chéo module CHỈ qua Contracts/EventBus.
## 4. Quy tắc cứng
R1  Không đổi luật, số cân bằng, số lượng nội dung của GDD. Số liệu chỉ đọc từ Balance/ScriptableObject, kèm chú thích // GDD <mục>.
R2  Unity Editor đang TẮT. Chỉ tạo/sửa: .cs .asmdef .json .uss .uxml .md .ps1 .sh .yml. KHÔNG tạo/sửa .unity .prefab .asset .meta .anim
.controller .mat trừ khi Task Card cho phép rõ ràng. Phần cần làm trong Editor: ghi thành checklist “Việc cần làm trong Unity”.
R3  Chỉ sửa file trong phạm vi Task Card. Cần sửa ngoài phạm vi → hỏi trước. Không đổi public API/Contracts khi không có ADR.
R4  Đặt tên: <Prefix>_<Domain>_<Subject>[_<Variant>][_<State>][_<Dir>][_<NN>].<ext> cho asset; script: TênClass.cs PascalCase;
namespace PigTycoon.<Module>; ID dữ liệu <domain>.<category>.<snake_name>; chỉ ký tự ASCII không dấu, không dấu cách, ≤64 ký tự;
không _final/_v2/_new. File mới phải nằm đúng thư mục module; không tự tạo module/thư mục gốc mới.
R5  Logic game đặt trong Core, không using UnityEngine, có unit test NUnit chạy được bằng dotnet test và Unity EditMode.
R6  Test-first: viết test thể hiện yêu cầu GDD (dùng số thật của GDD), xác nhận test đỏ, rồi mới code. Cấm test rỗng, cấm [Ignore],
cấm sửa test cho khớp code sai. Mỗi hàm public trong Core có ít nhất 1 test thường + 1 test biên.
R7  Hiệu năng: không cấp phát bộ nhớ trong Update/FixedUpdate, không LINQ/foreach trên interface trong vòng lặp nóng, không Find*/GetComponent
mỗi frame, dùng pooling (UnityEngine.Pool). Mục tiêu: 150 heo + 30 quái + 4 NPC/thú ≥ 60 FPS.
R8  Không chuỗi hiển thị cứng: dùng key Localization. Không đường dẫn tuyệt đối. Reset static thủ công (Domain Reload tắt).
R9  Chỉ dùng package/thư viện trong danh sách cho phép (Sổ tay mục 15). Thêm mới → đề xuất kèm license, không tự cài.
R10 Không dùng code/asset không rõ license; trích nguồn vào THIRD_PARTY_NOTICES.md. Tuyệt đối không dùng asset của Momodora hay game khác.
R11 Không chạy lệnh phá hủy (git reset --hard, git push, xóa thư mục, sửa .git); không đọc/ghi ngoài repo; không lộ khóa/API key.
R12 Trung thực: không tuyên bố “đã chạy/đã pass” nếu chưa chạy thật; dán output lệnh. Không chắc thì nói không chắc.
## 5. Quy trình bắt buộc
B1 Đọc Task Card + file liên quan → trả lời: (a) hiểu yêu cầu, (b) mục GDD liên quan, (c) kế hoạch ≤10 bước, (d) file sẽ sửa, (e) rủi ro. Chờ duyệt.
B2 Viết test → chạy, xác nhận đỏ.  B3 Code tối thiểu cho test xanh, rồi refactor.  B4 Chạy: pwsh tools/verify.ps1 (dán kết quả).
B5 Tự sửa tối đa 3 vòng; vẫn lỗi → dừng, mô tả nguyên nhân và đề xuất.  B6 Self-Review theo checklist mục 6.  B7 Báo cáo theo mục 7.
## 6. Self-Review (trả lời từng câu, có bằng chứng)
[ ] Mỗi tiêu chí chấp nhận trong Task Card ↔ test nào chứng minh?  [ ] Nếu đổi 1 hằng số/dấu so sánh trong code, test nào sẽ đỏ?
[ ] Các trường hợp biên: 0, âm, tối đa, null, danh sách rỗng, lưu/tải giữa chừng, tạm dừng thời gian?  [ ] Có vi phạm bất biến GDD mục 27?
[ ] Có cấp phát/GC trong vòng lặp nóng?  [ ] Tên file/thư mục đúng R4?  [ ] Có phần nào chỉ “giả lập cho pass” (hard-code kết quả)? → phải = KHÔNG.
[ ] 3 điểm yếu nhất của lời giải này là gì và cách cải thiện?
## 7. Mẫu báo cáo cuối phiên
TÓM TẮT • FILE ĐÃ ĐỔI (đường dẫn + lý do) • TEST (tên test + kết quả, dán output verify) • SELF-REVIEW (mục 6) •
VIỆC CẦN LÀM TRONG UNITY (checklist cho người: gắn component, tạo prefab, chỉnh scene) • RỦI RO/CÂU HỎI MỞ • ĐỀ XUẤT COMMIT: [WBS-ID][Người] …
13.3. Mẫu Task Card gửi kèm prompt
| # Task Card {WBS-ID} — {Tên work item} Owner: {A|B|C|D} • Loại: {MUST|STRETCH} • Mục GDD: {…} • Phụ thuộc: {WBS-ID khác} Đầu vào (mô tả lời): {dữ liệu, contract, asset có sẵn, số liệu Balance liên quan} Đầu ra mong đợi (mô tả lời): {hành vi người chơi thấy được + state hệ thống thay đổi ra sao} Tiêu chí chấp nhận: 1) … 2) … 3) …   (mỗi tiêu chí kiểm chứng được bằng test hoặc thao tác trong Unity) Được phép sửa: {đường dẫn/thư mục}   •   Cấm sửa: {…} Code tham chiếu: {Sổ tay mục 14.x hoặc file trong repo} Ngoài phạm vi: {những gì KHÔNG làm trong task này} |
# Task Card {WBS-ID} — {Tên work item}
Owner: {A|B|C|D} • Loại: {MUST|STRETCH} • Mục GDD: {…} • Phụ thuộc: {WBS-ID khác}
Đầu vào (mô tả lời): {dữ liệu, contract, asset có sẵn, số liệu Balance liên quan}
Đầu ra mong đợi (mô tả lời): {hành vi người chơi thấy được + state hệ thống thay đổi ra sao}
Tiêu chí chấp nhận: 1) … 2) … 3) …   (mỗi tiêu chí kiểm chứng được bằng test hoặc thao tác trong Unity)
Được phép sửa: {đường dẫn/thư mục}   •   Cấm sửa: {…}
Code tham chiếu: {Sổ tay mục 14.x hoặc file trong repo}
Ngoài phạm vi: {những gì KHÔNG làm trong task này}
14. Code tham chiếu cho Agent
Các đoạn code dưới đây là “mẫu chuẩn” để agent bắt chước phong cách, cấu trúc và cách viết test. Chúng là điểm xuất phát, không phải bản cuối; owner của module có thể mở rộng nhưng không được phá quy ước.
14.1. Asmdef của tầng Core (thuần C#)
| // Assets/_Project/Code/Core/PigTycoon.Core.asmdef { "name": "PigTycoon.Core", "rootNamespace": "PigTycoon.Core", "references": [], "noEngineReferences": true, "autoReferenced": false, "allowUnsafeCode": false } |
// Assets/_Project/Code/Core/PigTycoon.Core.asmdef
{ "name": "PigTycoon.Core", "rootNamespace": "PigTycoon.Core", "references": [],
"noEngineReferences": true, "autoReferenced": false, "allowUnsafeCode": false }
14.2. Đồng hồ game theo GDD 1.4
| namespace PigTycoon.Core.Time { public sealed class GameClock { private readonly float _realSecondsPerGameHour; // GDD 1.4: 40 s (đọc từ Balance) private readonly int _dayRolloverHour;        // GDD 1.4: chốt ngày lúc 06:00 public double TotalGameHours { get; private set; } public bool IsPaused { get; set; }             // dừng khi combat/đợt quái public event System.Action<int> HourTicked;   // giờ 0–23 public event System.Action<int> DayStarted;   // ngày thứ n (bắt đầu từ 1)  public GameClock(float realSecondsPerGameHour, int dayRolloverHour) { _realSecondsPerGameHour = realSecondsPerGameHour; _dayRolloverHour = dayRolloverHour; TotalGameHours = dayRolloverHour;        // ngày 1 bắt đầu lúc 06:00 }  public int Hour => (int)(TotalGameHours % 24.0); public int Day => DayOf((int)System.Math.Floor(TotalGameHours)); private int DayOf(int absoluteHour) => (absoluteHour - _dayRolloverHour) / 24 + 1;  public void Advance(float realDeltaSeconds) { if (IsPaused || realDeltaSeconds <= 0f) return; int before = (int)System.Math.Floor(TotalGameHours); TotalGameHours += realDeltaSeconds / _realSecondsPerGameHour; int after = (int)System.Math.Floor(TotalGameHours); for (int h = before + 1; h <= after; h++)  // không bỏ sót giờ khi lag/tua nhanh { HourTicked?.Invoke(h % 24); if (h % 24 == _dayRolloverHour) DayStarted?.Invoke(DayOf(h)); } } } } |
namespace PigTycoon.Core.Time
{
public sealed class GameClock
{
private readonly float _realSecondsPerGameHour; // GDD 1.4: 40 s (đọc từ Balance)
private readonly int _dayRolloverHour;        // GDD 1.4: chốt ngày lúc 06:00
public double TotalGameHours { get; private set; }
public bool IsPaused { get; set; }             // dừng khi combat/đợt quái
public event System.Action<int> HourTicked;   // giờ 0–23
public event System.Action<int> DayStarted;   // ngày thứ n (bắt đầu từ 1)
public GameClock(float realSecondsPerGameHour, int dayRolloverHour)
{
_realSecondsPerGameHour = realSecondsPerGameHour;
_dayRolloverHour = dayRolloverHour;
TotalGameHours = dayRolloverHour;        // ngày 1 bắt đầu lúc 06:00
}
public int Hour => (int)(TotalGameHours % 24.0);
public int Day => DayOf((int)System.Math.Floor(TotalGameHours));
private int DayOf(int absoluteHour) => (absoluteHour - _dayRolloverHour) / 24 + 1;
public void Advance(float realDeltaSeconds)
{
if (IsPaused || realDeltaSeconds <= 0f) return;
int before = (int)System.Math.Floor(TotalGameHours);
TotalGameHours += realDeltaSeconds / _realSecondsPerGameHour;
int after = (int)System.Math.Floor(TotalGameHours);
for (int h = before + 1; h <= after; h++)  // không bỏ sót giờ khi lag/tua nhanh
{
HourTicked?.Invoke(h % 24);
if (h % 24 == _dayRolloverHour) DayStarted?.Invoke(DayOf(h));
}
}
}
}
14.3. Công thức cộng dồn chỉ số (GDD 1.4) và test mẫu
| namespace PigTycoon.Core.Stats { public static class StatCalc { // Cuối = Gốc × (1 + Σ%cùng nhóm/100) × Π(hệ số × khác nhóm), rồi clamp public static float Final(float baseValue, System.ReadOnlySpan<float> percents, System.ReadOnlySpan<float> multipliers, float min, float max) { float sum = 0f; foreach (var p in percents) sum += p; float v = baseValue * (1f + sum / 100f); foreach (var m in multipliers) v *= m; return System.Math.Clamp(v, min, max); } // “điểm %” (đ%): cộng thẳng vào xác suất, ví dụ 70% + 5 đ% = 75% public static float AddPoints(float probability, float points) => System.Math.Clamp(probability + points, 0f, 100f); } }  // Tests/EditMode/StatCalcTests.cs — dùng đúng ví dụ trong GDD 1.4 [Test] public void Percent_Minus85_On70_Gives10_5()   // “sinh sản −85%” = ×0,15 → 70% × 0,15 = 10,5% => Assert.That(StatCalc.Final(70f, new[] { -85f }, default, 0f, 100f), Is.EqualTo(10.5f).Within(1e-4f)); [Test] public void Points_Add5_On70_Gives75() => Assert.That(StatCalc.AddPoints(70f, 5f), Is.EqualTo(75f)); [Test] public void Final_IsClamped() => Assert.That(StatCalc.Final(90f, new[] { 50f }, default, 0f, 100f), Is.EqualTo(100f)); |
namespace PigTycoon.Core.Stats
{
public static class StatCalc
{
// Cuối = Gốc × (1 + Σ%cùng nhóm/100) × Π(hệ số × khác nhóm), rồi clamp
public static float Final(float baseValue, System.ReadOnlySpan<float> percents,
System.ReadOnlySpan<float> multipliers, float min, float max)
{
float sum = 0f; foreach (var p in percents) sum += p;
float v = baseValue * (1f + sum / 100f);
foreach (var m in multipliers) v *= m;
return System.Math.Clamp(v, min, max);
}
// “điểm %” (đ%): cộng thẳng vào xác suất, ví dụ 70% + 5 đ% = 75%
public static float AddPoints(float probability, float points) => System.Math.Clamp(probability + points, 0f, 100f);
}
}
// Tests/EditMode/StatCalcTests.cs — dùng đúng ví dụ trong GDD 1.4
[Test] public void Percent_Minus85_On70_Gives10_5()   // “sinh sản −85%” = ×0,15 → 70% × 0,15 = 10,5%
=> Assert.That(StatCalc.Final(70f, new[] { -85f }, default, 0f, 100f), Is.EqualTo(10.5f).Within(1e-4f));
[Test] public void Points_Add5_On70_Gives75() => Assert.That(StatCalc.AddPoints(70f, 5f), Is.EqualTo(75f));
[Test] public void Final_IsClamped() => Assert.That(StatCalc.Final(90f, new[] { 50f }, default, 0f, 100f), Is.EqualTo(100f));
14.4. Định nghĩa dữ liệu (ScriptableObject) và bản ghi JSON
| namespace PigTycoon.Genetics { [UnityEngine.CreateAssetMenu(menuName = "PigTycoon/Data/Gene")] public sealed class GeneDef : UnityEngine.ScriptableObject { [UnityEngine.SerializeField] private string _id;        // pig.gene.lam_khe (bất biến) [UnityEngine.SerializeField] private string _nameKey;   // key Localization [UnityEngine.SerializeField] private Rarity _rarity;    // 6 bậc theo GDD 5 [UnityEngine.SerializeField] private string _artOwner;  // A|B|C|D public string Id => _id; public Rarity Rarity => _rarity; } } // Data/Balance/balance.time.json — ví dụ { "schema": 1, "gddRef": "1.4", "realSecondsPerGameHour": 40, "dayRolloverHour": 6, "phasesPerDay": 4 } |
namespace PigTycoon.Genetics
{
[UnityEngine.CreateAssetMenu(menuName = "PigTycoon/Data/Gene")]
public sealed class GeneDef : UnityEngine.ScriptableObject
{
[UnityEngine.SerializeField] private string _id;        // pig.gene.lam_khe (bất biến)
[UnityEngine.SerializeField] private string _nameKey;   // key Localization
[UnityEngine.SerializeField] private Rarity _rarity;    // 6 bậc theo GDD 5
[UnityEngine.SerializeField] private string _artOwner;  // A|B|C|D
public string Id => _id; public Rarity Rarity => _rarity;
}
}
// Data/Balance/balance.time.json — ví dụ
{ "schema": 1, "gddRef": "1.4", "realSecondsPerGameHour": 40, "dayRolloverHour": 6, "phasesPerDay": 4 }
14.5. Dự án test chạy ngoài Unity và script verify
| <!-- tools/DomainTests/DomainTests.csproj : biên dịch lại đúng file Core của Unity để test bằng dotnet --> <Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup> <ItemGroup><Compile Include="../../game/PigTycoon/Assets/_Project/Code/Core/**/*.cs" /> <Compile Include="../../game/PigTycoon/Assets/_Project/Code/Tests/EditMode/Core/**/*.cs" /></ItemGroup> <ItemGroup><PackageReference Include="NUnit" Version="3.*" /><PackageReference Include="NUnit3TestAdapter" Version="4.*" /> <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" /></ItemGroup></Project>  # tools/verify.ps1 param([switch]$Unity) $ErrorActionPreference = "Stop"; New-Item -ItemType Directory -Force artifacts | Out-Null pwsh tools/lint/Check-Names.ps1; if ($LASTEXITCODE) { throw "Sai quy ước đặt tên" } dotnet test tools/DomainTests/DomainTests.csproj -c Release --nologo; if ($LASTEXITCODE) { throw "Core tests FAILED" } if ($Unity) { if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw "Hãy ĐÓNG Unity Editor trước khi chạy batch" } & $env:UNITY_EXE -batchmode -nographics -projectPath game/PigTycoon -runTests -testPlatform EditMode ` -testResults artifacts/editmode.xml -logFile artifacts/unity-editmode.log if ($LASTEXITCODE) { throw "Unity EditMode FAILED – xem artifacts/unity-editmode.log" } } Write-Host "VERIFY OK" |
<!-- tools/DomainTests/DomainTests.csproj : biên dịch lại đúng file Core của Unity để test bằng dotnet -->
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup>
<ItemGroup><Compile Include="../../game/PigTycoon/Assets/_Project/Code/Core/**/*.cs" />
<Compile Include="../../game/PigTycoon/Assets/_Project/Code/Tests/EditMode/Core/**/*.cs" /></ItemGroup>
<ItemGroup><PackageReference Include="NUnit" Version="3.*" /><PackageReference Include="NUnit3TestAdapter" Version="4.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" /></ItemGroup></Project>
# tools/verify.ps1
param([switch]$Unity)
$ErrorActionPreference = "Stop"; New-Item -ItemType Directory -Force artifacts | Out-Null
pwsh tools/lint/Check-Names.ps1; if ($LASTEXITCODE) { throw "Sai quy ước đặt tên" }
dotnet test tools/DomainTests/DomainTests.csproj -c Release --nologo; if ($LASTEXITCODE) { throw "Core tests FAILED" }
if ($Unity) {
if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw "Hãy ĐÓNG Unity Editor trước khi chạy batch" }
& $env:UNITY_EXE -batchmode -nographics -projectPath game/PigTycoon -runTests -testPlatform EditMode `
-testResults artifacts/editmode.xml -logFile artifacts/unity-editmode.log
if ($LASTEXITCODE) { throw "Unity EditMode FAILED – xem artifacts/unity-editmode.log" }
}
Write-Host "VERIFY OK"
14.6. Regex kiểm tra tên asset (dùng trong Check-Names.ps1)
| ^(SPR|ASE|ANIM|AC|PF|SO|TILE|TP|UI|USS|UXML|VFX|MAT|SHD|SFX|MUS|PRE)_[A-Z][A-Za-z]+_[A-Z][A-Za-z0-9]+(_[A-Z0-9][A-Za-z0-9]*){0,4}\.[a-z0-9]+$ # Áp cho Assets/_Project/{Art,Audio,Prefabs,Data/Catalog,Settings}; script .cs kiểm bằng: tên file == tên class công khai. |
^(SPR|ASE|ANIM|AC|PF|SO|TILE|TP|UI|USS|UXML|VFX|MAT|SHD|SFX|MUS|PRE)_[A-Z][A-Za-z]+_[A-Z][A-Za-z0-9]+(_[A-Z0-9][A-Za-z0-9]*){0,4}\.[a-z0-9]+$
# Áp cho Assets/_Project/{Art,Audio,Prefabs,Data/Catalog,Settings}; script .cs kiểm bằng: tên file == tên class công khai.
15. Thư viện và công cụ được phép dùng
Đây là danh sách “được phép” mà agent tham chiếu theo quy tắc R9. Gói chính thức của Unity cài qua Package Manager; thư viện bên thứ ba phải ghi vào THIRD_PARTY_NOTICES.md. Thêm thư viện mới cần Issue + đồng ý của A.
15.1. Gói chính thức của Unity
| Gói | Owner sử dụng chính | Dùng cho |
Gói
Owner sử dụng chính
Dùng cho
| Universal RP (2D Renderer) + 2D Lights | B | Ánh sáng ngày/đêm, đèn lồng, bóng 2D |
Universal RP (2D Renderer) + 2D Lights
B
Ánh sáng ngày/đêm, đèn lồng, bóng 2D
| 2D Pixel Perfect (trong URP) | B | Camera pixel chuẩn 640×360 |
2D Pixel Perfect (trong URP)
B
Camera pixel chuẩn 640×360
| 2D Tilemap Editor + 2D Tilemap Extras | B | Map, Rule Tile, Animated Tile |
2D Tilemap Editor + 2D Tilemap Extras
B
Map, Rule Tile, Animated Tile
| 2D Aseprite Importer (com.unity.2d.aseprite) | Tất cả | Nhập trực tiếp .aseprite kèm tag animation |
2D Aseprite Importer (com.unity.2d.aseprite)
Tất cả
Nhập trực tiếp .aseprite kèm tag animation
| 2D Sprite, 2D Animation (tùy chọn) | Tất cả | Sprite Library để đổi gen/trang phục trên cùng rig |
2D Sprite, 2D Animation (tùy chọn)
Tất cả
Sprite Library để đổi gen/trang phục trên cùng rig
| Input System | A | Bàn phím, chuột, tay cầm, đổi phím |
Input System
A
Bàn phím, chuột, tay cầm, đổi phím
| Cinemachine 3 | A, C | Camera theo nhân vật, rung (Impulse), vùng giới hạn |
Cinemachine 3
A, C
Camera theo nhân vật, rung (Impulse), vùng giới hạn
| UI Toolkit (+ uGUI cho world-space) | D | HUD, menu, chợ, inventory |
UI Toolkit (+ uGUI cho world-space)
D
HUD, menu, chợ, inventory
| Localization | D | Tiếng Việt, tiếng Anh |
Localization
D
Tiếng Việt, tiếng Anh
| Addressables | A | Nạp nội dung theo nhóm, giảm RAM |
Addressables
A
Nạp nội dung theo nhóm, giảm RAM
| Burst, Collections, Mathematics, Jobs | B, A | Mô phỏng đàn 150+ heo, flow-field |
Burst, Collections, Mathematics, Jobs
B, A
Mô phỏng đàn 150+ heo, flow-field
| Unity Test Framework, Code Coverage, Performance Testing | A | EditMode/PlayMode test, đo độ phủ, đo hiệu năng |
Unity Test Framework, Code Coverage, Performance Testing
A
EditMode/PlayMode test, đo độ phủ, đo hiệu năng
| Newtonsoft Json (com.unity.nuget.newtonsoft-json) | A, D | Save và dữ liệu Balance |
Newtonsoft Json (com.unity.nuget.newtonsoft-json)
A, D
Save và dữ liệu Balance
| Recorder | Tất cả | Quay GIF/MP4 làm bằng chứng PR |
Recorder
Tất cả
Quay GIF/MP4 làm bằng chứng PR
| Memory Profiler, Profile Analyzer, Project Auditor | A | Tối ưu hiệu năng, kiểm tra dự án |
Memory Profiler, Profile Analyzer, Project Auditor
A
Tối ưu hiệu năng, kiểm tra dự án
| UnityEngine.Pool (có sẵn) | Tất cả | Pool đạn, quái, VFX, popup |
UnityEngine.Pool (có sẵn)
Tất cả
Pool đạn, quái, VFX, popup
15.2. Thư viện mã nguồn mở bên thứ ba (đã kiểm tra là giấy phép mở)
| Thư viện | Giấy phép | Dùng cho / ghi chú |
Thư viện
Giấy phép
Dùng cho / ghi chú
| UniTask (Cysharp) | MIT | Async/await không cấp phát, thay Coroutine trong luồng phức tạp |
UniTask (Cysharp)
MIT
Async/await không cấp phát, thay Coroutine trong luồng phức tạp
| PrimeTween | MIT | Tween UI và hiệu ứng không cấp phát (thay DOTween) |
PrimeTween
MIT
Tween UI và hiệu ứng không cấp phát (thay DOTween)
| VContainer (tùy chọn, cần ADR) | MIT | Dependency injection cho service; nếu không dùng thì A làm Service Locator đơn giản |
VContainer (tùy chọn, cần ADR)
MIT
Dependency injection cho service; nếu không dùng thì A làm Service Locator đơn giản
| NavMeshPlus (tùy chọn) | MIT | NavMesh 2D cho NPC/quái; đàn heo dùng flow-field tự viết |
NavMeshPlus (tùy chọn)
MIT
NavMesh 2D cho NPC/quái; đàn heo dùng flow-field tự viết
| ink + ink-unity-integration (inkle) | MIT | Hội thoại, quest theo cốt truyện |
ink + ink-unity-integration (inkle)
MIT
Hội thoại, quest theo cốt truyện
| Graphy | MIT | Đồng hồ FPS/RAM trong build test |
Graphy
MIT
Đồng hồ FPS/RAM trong build test
| NaughtyAttributes | MIT | Inspector gọn cho designer |
NaughtyAttributes
MIT
Inspector gọn cho designer
| MCP for Unity (CoplayDev/unity-mcp) | MIT | Cầu nối agent ↔ Unity Editor (chế độ 4, chỉ cài máy cá nhân) |
MCP for Unity (CoplayDev/unity-mcp)
MIT
Cầu nối agent ↔ Unity Editor (chế độ 4, chỉ cài máy cá nhân)
| GameCI (game-ci/unity-test-runner, unity-builder) | MIT | Chạy test/build Unity trên GitHub Actions (cần license Unity cho CI) |
GameCI (game-ci/unity-test-runner, unity-builder)
MIT
Chạy test/build Unity trên GitHub Actions (cần license Unity cho CI)
Ghi chú: giấy phép có thể thay đổi theo phiên bản; người thêm thư viện phải kiểm tra lại file LICENSE của đúng phiên bản trước khi đưa vào repo.
15.3. Công cụ ngoài Unity
Pixel art: Aseprite (khuyến nghị), LibreSprite hoặc Pixelorama (miễn phí). Palette: Lospec Palette List.
IDE: JetBrains Rider hoặc VS Code (C# Dev Kit + Unity extension) hoặc Visual Studio; IDE agent: Antigravity, Codex, Claude Code, Cursor, Copilot.
Git: Git + Git LFS, GitHub Desktop hoặc Fork; merge YAML: UnityYAMLMerge.
Âm thanh: Audacity, sfxr/jsfxr/ChipTone cho SFX 8-bit, Bfxr.
Quản lý: GitHub Projects (bảng), GitHub Issues/Discussions, Discord/Zalo cho trao đổi nhanh.
16. Nguồn tham khảo code, asset và kiến thức cho Agent
Agent được phép đọc các nguồn sau để học cách làm. Code chép vào repo phải tương thích giấy phép và ghi nguồn. Asset bên ngoài chỉ dùng làm placeholder tạm (gắn nhãn TEMP) hoặc nếu giấy phép cho phép dùng thương mại; mọi asset cuối cùng do nhóm tự vẽ.
| Nhóm | Nguồn | Dùng để |
Nhóm
Nguồn
Dùng để
| Tài liệu chính thức | docs.unity3d.com (Manual, Scripting API, tài liệu từng package), Unity Learn | API chuẩn, lệnh dòng lệnh, Pixel Perfect, Tilemap, Test Framework |
Tài liệu chính thức
docs.unity3d.com (Manual, Scripting API, tài liệu từng package), Unity Learn
API chuẩn, lệnh dòng lệnh, Pixel Perfect, Tilemap, Test Framework
| Dự án mẫu của Unity | Happy Harvest (mẫu farming 2D top-down URP), Dragon Crashers (2D URP + UI Toolkit), Open Project “Chop Chop” trên GitHub UnityTechnologies | Cấu trúc farming 2D, ánh sáng 2D, UI Toolkit, kiến trúc ScriptableObject |
Dự án mẫu của Unity
Happy Harvest (mẫu farming 2D top-down URP), Dragon Crashers (2D URP + UI Toolkit), Open Project “Chop Chop” trên GitHub UnityTechnologies
Cấu trúc farming 2D, ánh sáng 2D, UI Toolkit, kiến trúc ScriptableObject
| Thuật toán | Red Blob Games (pathfinding, flow field, lưới), Game Programming Patterns (Robert Nystrom, đọc miễn phí), Sebastian Lague (boids trên GitHub) | Flow-field đàn heo, steering, state machine, event queue |
Thuật toán
Red Blob Games (pathfinding, flow field, lưới), Game Programming Patterns (Robert Nystrom, đọc miễn phí), Sebastian Lague (boids trên GitHub)
Flow-field đàn heo, steering, state machine, event queue
| Kiến trúc Unity | Unity e-book “Create modular game architecture with ScriptableObjects”, “Level up your code with design patterns” | Event channel, service, data-driven |
Kiến trúc Unity
Unity e-book “Create modular game architecture with ScriptableObjects”, “Level up your code with design patterns”
Event channel, service, data-driven
| Kho mã nguồn mở | GitHub (lọc giấy phép MIT/Apache/BSD), OpenUPM (registry package Unity) | Tìm thư viện, xem cách triển khai |
Kho mã nguồn mở
GitHub (lọc giấy phép MIT/Apache/BSD), OpenUPM (registry package Unity)
Tìm thư viện, xem cách triển khai
| Agent ↔ Unity | coplaydev.github.io/unity-mcp (tài liệu MCP for Unity), Unity blog về Unity MCP Server | Cấu hình chế độ 4 |
Agent ↔ Unity
coplaydev.github.io/unity-mcp (tài liệu MCP for Unity), Unity blog về Unity MCP Server
Cấu hình chế độ 4
| Asset tạm và tham khảo phong cách | Kenney.nl (CC0), OpenGameArt (kiểm tra giấy phép từng asset), itch.io game assets (đọc kỹ giấy phép), Lospec | Placeholder, palette, học kỹ thuật pixel |
Asset tạm và tham khảo phong cách
Kenney.nl (CC0), OpenGameArt (kiểm tra giấy phép từng asset), itch.io game assets (đọc kỹ giấy phép), Lospec
Placeholder, palette, học kỹ thuật pixel
| Tham chiếu UI/UX | Ảnh chụp màn hình và trailer chính thức của Momodora: Moonlit Farewell (Steam, PLAYISM), Game UI Database | Moodboard nội bộ – chỉ tham khảo, không đưa vào build |
Tham chiếu UI/UX
Ảnh chụp màn hình và trailer chính thức của Momodora: Moonlit Farewell (Steam, PLAYISM), Game UI Database
Moodboard nội bộ – chỉ tham khảo, không đưa vào build
| Học pixel art | Lospec tutorials, Pedro Medeiros (Saint11) tutorials, Slynyrd pixel art blog | Kỹ thuật animation, ánh sáng, palette |
Học pixel art
Lospec tutorials, Pedro Medeiros (Saint11) tutorials, Slynyrd pixel art blog
Kỹ thuật animation, ánh sáng, palette
| CI/CD | game.ci (GameCI docs), GitHub Actions docs | Workflow test/build |
CI/CD
game.ci (GameCI docs), GitHub Actions docs
Workflow test/build
| Thiết kế nội bộ | docs/gdd (GDD v6.0), tài liệu cốt truyện, PigTycoon_WBS_v2, sổ tay này | Nguồn sự thật |
Thiết kế nội bộ
docs/gdd (GDD v6.0), tài liệu cốt truyện, PigTycoon_WBS_v2, sổ tay này
Nguồn sự thật
Cấm: lấy sprite từ các trang trích xuất sprite game thương mại (ví dụ kho sprite của Momodora), chép code từ dự án không có giấy phép, dùng tài sản AI tạo ra từ tên tác phẩm có bản quyền.
17. Kiểm thử và nghiệm thu theo giai đoạn
17.1. Tháp kiểm thử
| Tầng | Công cụ / nơi chạy | Yêu cầu |
Tầng
Công cụ / nơi chạy
Yêu cầu
| Unit test Core | dotnet test + Unity EditMode | Mọi công thức GDD; độ phủ Core ≥70% từ G2, ≥80% từ G4 |
Unit test Core
dotnet test + Unity EditMode
Mọi công thức GDD; độ phủ Core ≥70% từ G2, ≥80% từ G4
| Integration test | Unity EditMode/PlayMode | Mỗi contract có ít nhất 1 test giữa hai module thật |
Integration test
Unity EditMode/PlayMode
Mỗi contract có ít nhất 1 test giữa hai module thật
| Simulation test | tools/sim (headless) | Chạy 30–60 ngày game với seed cố định, so sánh kết quả giữa các lần chạy (determinism) |
Simulation test
tools/sim (headless)
Chạy 30–60 ngày game với seed cố định, so sánh kết quả giữa các lần chạy (determinism)
| Performance test | Performance Testing + Profiler | 150 heo + 30 quái + 4 NPC/thú: ≥60 FPS, p95 frame time ≤20 ms, GC alloc gameplay ≈0 |
Performance test
Performance Testing + Profiler
150 heo + 30 quái + 4 NPC/thú: ≥60 FPS, p95 frame time ≤20 ms, GC alloc gameplay ≈0
| Manual / Playtest | Build Windows | Kịch bản test trong docs/playtest; người ngoài nhóm từ G1 |
Manual / Playtest
Build Windows
Kịch bản test trong docs/playtest; người ngoài nhóm từ G1
| Art review | B + 1 người | Đúng palette, PPU, viền, silhouette thang xám, đủ animation |
Art review
B + 1 người
Đúng palette, PPU, viền, silhouette thang xám, đủ animation
17.2. Definition of Ready và Definition of Done
| Definition of Ready (được bắt đầu) | Definition of Done (được tính hoàn thành) |
Definition of Ready (được bắt đầu)
Definition of Done (được tính hoàn thành)
| Có Issue với WBS-ID, owner, M/S Task Card có đầu vào, đầu ra, tiêu chí chấp nhận Đã trích mục GDD và số Balance liên quan Phụ thuộc đã merge hoặc có mock Ước lượng ≤4 ngày (nếu không: tách checkpoint) | Mọi tiêu chí chấp nhận có bằng chứng (test hoặc video) CI xanh, Console Unity 0 lỗi, 0 cảnh báo mới Không vi phạm bất biến GDD, không số ma thuật Tên file, thư mục đúng quy ước; asset đúng Art Bible Save/Load hoạt động với state mới Reviewer đã tự chạy trong Unity và duyệt Tài liệu/ADR cập nhật nếu đổi contract |
Có Issue với WBS-ID, owner, M/S
Task Card có đầu vào, đầu ra, tiêu chí chấp nhận
Đã trích mục GDD và số Balance liên quan
Phụ thuộc đã merge hoặc có mock
Ước lượng ≤4 ngày (nếu không: tách checkpoint)
Mọi tiêu chí chấp nhận có bằng chứng (test hoặc video)
CI xanh, Console Unity 0 lỗi, 0 cảnh báo mới
Không vi phạm bất biến GDD, không số ma thuật
Tên file, thư mục đúng quy ước; asset đúng Art Bible
Save/Load hoạt động với state mới
Reviewer đã tự chạy trong Unity và duyệt
Tài liệu/ADR cập nhật nếu đổi contract
17.3. Cổng nghiệm thu G0 – G6
| Cổng | Điều kiện bắt buộc | Kiểm tra chất lượng sâu (không chỉ để pass) |
Cổng
Điều kiện bắt buộc
Kiểm tra chất lượng sâu (không chỉ để pass)
| G0 – 18/10/2026 Nền tảng | Project mở được trên 4 máy; CI chặn PR sai; AGENTS.md có hiệu lực; map + An + Khoa idle/walk trong game; catalog đủ số lượng; GameClock có test. | Mỗi người làm 1 PR thử có lỗi cố ý (sai tên file, test đỏ) và xác nhận CI chặn được. |
G0 – 18/10/2026 Nền tảng
Project mở được trên 4 máy; CI chặn PR sai; AGENTS.md có hiệu lực; map + An + Khoa idle/walk trong game; catalog đủ số lượng; GameClock có test.
Mỗi người làm 1 PR thử có lỗi cố ý (sai tên file, test đỏ) và xác nhận CI chặn được.
| G1 – 29/11 Vertical Slice | Chơi trọn 1 ngày game với An hoặc Khoa; chăm heo, bán, nạp trước, chống 1 đợt nhỏ, lưu/tải; ≥60 FPS với 100 heo. | 3 người ngoài nhóm chơi không cần hướng dẫn qua mục tiêu đầu; lưu/tải giữa đợt quái không mất dữ liệu; so hash state 3 lần lưu/tải. |
G1 – 29/11 Vertical Slice
Chơi trọn 1 ngày game với An hoặc Khoa; chăm heo, bán, nạp trước, chống 1 đợt nhỏ, lưu/tải; ≥60 FPS với 100 heo.
3 người ngoài nhóm chơi không cần hướng dẫn qua mục tiêu đầu; lưu/tải giữa đợt quái không mất dữ liệu; so hash state 3 lần lưu/tải.
| G2 – 24/01/2027 Alpha | Mọi hệ lõi P2 hoạt động; 12 gen, 12 NPC, 7 thú cưng có sprite; độ phủ Core ≥70%; mô phỏng 30 ngày không lỗi. | Reviewer “gây đột biến” thủ công: đổi 1 hằng số/dấu so sánh trong mỗi module → phải có test đỏ. Module nào không có test đỏ thì không qua cổng. |
G2 – 24/01/2027 Alpha
Mọi hệ lõi P2 hoạt động; 12 gen, 12 NPC, 7 thú cưng có sprite; độ phủ Core ≥70%; mô phỏng 30 ngày không lỗi.
Reviewer “gây đột biến” thủ công: đổi 1 hằng số/dấu so sánh trong mỗi module → phải có test đỏ. Module nào không có test đỏ thì không qua cổng.
| G3 – 14/03 Defense complete | 31 công trình, 14 quái + 3 Boss; 3 Bạch Vân kích tay, còn lại tự động; mục tiêu hiệu năng đạt trên máy cấu hình tối thiểu. | Profiler capture đính kèm; thử 3 chiến lược cực đoan (chỉ xây, chỉ đánh tay, không tình báo) – không chiến lược nào thắng áp đảo hoặc thua chắc chắn. |
G3 – 14/03 Defense complete
31 công trình, 14 quái + 3 Boss; 3 Bạch Vân kích tay, còn lại tự động; mục tiêu hiệu năng đạt trên máy cấu hình tối thiểu.
Profiler capture đính kèm; thử 3 chiến lược cực đoan (chỉ xây, chỉ đánh tay, không tình báo) – không chiến lược nào thắng áp đảo hoặc thua chắc chắn.
| G4 – 25/04 Content complete (Beta) | Toàn bộ nội dung bảng 1.2 có trong game; Bí nhân, sự kiện, Giấy, Phù Văn/Mệnh Ấn hoạt động; độ phủ Core ≥80%. | Bộ test bất biến 17.4 xanh 100%; chạy mô phỏng 60 ngày × 20 seed, không exception, không giá trị NaN/âm vô lý. |
G4 – 25/04 Content complete (Beta)
Toàn bộ nội dung bảng 1.2 có trong game; Bí nhân, sự kiện, Giấy, Phù Văn/Mệnh Ấn hoạt động; độ phủ Core ≥80%.
Bộ test bất biến 17.4 xanh 100%; chạy mô phỏng 60 ngày × 20 seed, không exception, không giá trị NaN/âm vô lý.
| G5 – 16/05 Polish | UI theo UI Kit 100% màn hình; game feel; 3 vòng playtest; accessibility. | 5 người mới chơi: ≥4/5 hoàn thành ngày đầu không cần hỏi; khảo sát “dễ hiểu” ≥4/5; không còn chữ hard-code. |
G5 – 16/05 Polish
UI theo UI Kit 100% màn hình; game feel; 3 vòng playtest; accessibility.
5 người mới chơi: ≥4/5 hoàn thành ngày đầu không cần hỏi; khảo sát “dễ hiểu” ≥4/5; không còn chữ hard-code.
| G6 – 30/05 Release 1.0 | 0 Blocker/Critical; ≤5 Major có ghi chú; build chạy trên máy sạch; CHANGELOG, tag v1.0.0. | Soak test 2 giờ: không crash, RAM tăng <5%; cài/gỡ sạch; save cũ từ bản Beta tải được (migrate). |
G6 – 30/05 Release 1.0
0 Blocker/Critical; ≤5 Major có ghi chú; build chạy trên máy sạch; CHANGELOG, tag v1.0.0.
Soak test 2 giờ: không crash, RAM tăng <5%; cài/gỡ sạch; save cũ từ bản Beta tải được (migrate).
17.4. Bất biến thiết kế phải có test tự động
Chỉ có 2 nhân vật người chơi (Khoa, An).
Chỉ 3 công trình Bạch Vân cần kích hoạt thủ công; 28 công trình còn lại tự động.
Trang bị không được tính vào điều kiện học võ kỹ; mỗi cặp trang bị mở đúng 1 ô võ kỹ, tối đa 6 ô trang bị.
Bí nhân không bao giờ nhận vàng và luôn đòi giá trị cao hơn món đưa ra.
Chợ đêm: tỉ lệ ô hàng nhận vàng xấp xỉ 18% (kiểm thống kê trên 1000 lần sinh).
Trạng thái Bầy Đàn không hình thành khi Sinh Cảnh <50 hoặc dưới 100 con gắn kết.
Đặc tính chuyên biệt ẩn hoàn toàn cho tới khi đủ 6 khóa.
Cổng: hoàn thành ≥50% thì thoát được bằng vật phẩm bậc Thường; Sương mù cần vật phẩm bậc cao.
Chỉ Quả Mộng trong 5 vật phẩm phá cooldown ngủ không có tác dụng phụ.
Đồng hồ ngày dừng trong đợt quái; mọi phép tính “mỗi ngày” chốt lúc 06:00.
Vàng làm tròn tới 1G sau phép tính cuối; xác suất luôn trong khoảng 0–100%.
Bổ sung đầy đủ các bất biến còn lại theo GDD mục 27 trong file test InvariantTests (owner: A, nội dung: owner từng hệ).
17.5. Kiểm soát chất lượng khi dùng Agent (chống “pass cho có”)
Test phải đỏ trước: PR có test mới phải kèm log cho thấy test đỏ trước khi có code (agent dán output).
Kiểm tra đột biến thủ công: mỗi PR chạm Core, reviewer đổi 1 hằng số hoặc dấu so sánh và xác nhận có test đỏ.
Không hard-code kết quả: cấm trả hằng số khớp test, cấm bắt ngoại lệ rồi bỏ qua, cấm [Ignore] và cấm nới dung sai so sánh để pass.
Self-Review bắt buộc: agent trả lời checklist mục 6 của AGENTS.md, gồm 3 điểm yếu của lời giải; chủ PR phải phản hồi từng điểm.
Hiểu code: reviewer có quyền hỏi bất kỳ dòng nào; chủ PR không giải thích được thì PR bị trả lại.
Đối chiếu GDD: mỗi tiêu chí chấp nhận phải ghi số mục GDD; reviewer kiểm tra ngẫu nhiên 1 số liệu so với GDD.
Chạy trong Unity thật: unit test xanh chưa đủ; phải có video/ảnh chạy trong scene.
17.6. Playtest và đo lường
Telemetry ghi CSV mỗi ngày game: vàng, chi theo lý do, số heo theo giai đoạn, mật độ, Sinh Cảnh, Áp Lực Sự Kiện, kết quả đợt quái.
Mỗi vòng playtest có biên bản: mục tiêu, người chơi, quan sát, số liệu, Issue tạo ra.
Phân loại lỗi: Blocker (không chơi tiếp được), Critical (mất save/crash), Major (sai luật GDD/hỏng tính năng), Minor (hiển thị), Trivial.
18. Quản trị dự án chuyên nghiệp
18.1. Nhịp làm việc
| Hoạt động | Thời điểm | Nội dung |
Hoạt động
Thời điểm
Nội dung
| Standup bất đồng bộ | Hằng ngày trước 10:00 | 3 câu trên kênh chat: hôm qua làm gì, hôm nay làm gì, đang vướng gì. |
Standup bất đồng bộ
Hằng ngày trước 10:00
3 câu trên kênh chat: hôm qua làm gì, hôm nay làm gì, đang vướng gì.
| Hạn cập nhật GitHub | Thứ Tư và Chủ Nhật 23:59 | Mỗi người ít nhất 1 cập nhật hợp lệ ở mỗi hạn. |
Hạn cập nhật GitHub
Thứ Tư và Chủ Nhật 23:59
Mỗi người ít nhất 1 cập nhật hợp lệ ở mỗi hạn.
| Review tuần + demo | Chủ Nhật 20:00 (45 phút) | Mỗi người demo trong Unity 5 phút; cập nhật bảng tiến độ; ghi nhận STRETCH. |
Review tuần + demo
Chủ Nhật 20:00 (45 phút)
Mỗi người demo trong Unity 5 phút; cập nhật bảng tiến độ; ghi nhận STRETCH.
| Lập kế hoạch sprint | Đầu mỗi sprint 2 tuần | Chốt Issue, kiểm DoR, cân bằng tải. |
Lập kế hoạch sprint
Đầu mỗi sprint 2 tuần
Chốt Issue, kiểm DoR, cân bằng tải.
| Retrospective | Sau mỗi cổng G | Giữ gì, bỏ gì, thử gì; cập nhật sổ tay nếu cần. |
Retrospective
Sau mỗi cổng G
Giữ gì, bỏ gì, thử gì; cập nhật sổ tay nếu cần.
18.2. Quản lý rủi ro
| Rủi ro | Mức | Biện pháp |
Rủi ro
Mức
Biện pháp
| Conflict scene/prefab YAML | Cao | Quyền sở hữu scene (3.4), prefab nhỏ, giao qua branch, UnityYAMLMerge. |
Conflict scene/prefab YAML
Cao
Quyền sở hữu scene (3.4), prefab nhỏ, giao qua branch, UnityYAMLMerge.
| Agent sinh code sai GDD hoặc “pass cho có” | Cao | AGENTS.md R1/R6/R12, test-first, kiểm đột biến, Self-Review. |
Agent sinh code sai GDD hoặc “pass cho có”
Cao
AGENTS.md R1/R6/R12, test-first, kiểm đột biến, Self-Review.
| Art lệch phong cách giữa 4 người | Cao | Art Bible, palette khóa, B duyệt mọi asset, rig heo dùng chung. |
Art lệch phong cách giữa 4 người
Cao
Art Bible, palette khóa, B duyệt mọi asset, rig heo dùng chung.
| Hiệu năng đàn heo | Trung bình | Flow-field + Burst/Jobs, đo từ P1, mục tiêu cố định. |
Hiệu năng đàn heo
Trung bình
Flow-field + Burst/Jobs, đo từ P1, mục tiêu cố định.
| Phạm vi phình to | Trung bình | Design Lock, STRETCH chỉ sau MUST, hạng mục hoãn theo GDD 28. |
Phạm vi phình to
Trung bình
Design Lock, STRETCH chỉ sau MUST, hạng mục hoãn theo GDD 28.
| Thành viên vắng/quá tải | Trung bình | Người dự phòng mỗi trục; tài liệu đầy đủ; quy tắc thiếu hạn ở 11.1. |
Thành viên vắng/quá tải
Trung bình
Người dự phòng mỗi trục; tài liệu đầy đủ; quy tắc thiếu hạn ở 11.1.
| Hết dung lượng Git LFS | Thấp | Theo dõi hằng tháng, nén PNG, không commit file xuất trung gian. |
Hết dung lượng Git LFS
Thấp
Theo dõi hằng tháng, nén PNG, không commit file xuất trung gian.
| Lộ khóa API của agent | Thấp | Biến môi trường, .gitignore, GitHub secret scanning. |
Lộ khóa API của agent
Thấp
Biến môi trường, .gitignore, GitHub secret scanning.
18.3. Phiên bản, tài liệu quyết định, bảo mật, giấy phép
Semantic Versioning: v0.1.0-vs (G1), v0.2.0-alpha (G2), v0.3.0 (G3), v0.4.0-beta (G4), v0.9.0-rc (G5), v1.0.0 (G6). CHANGELOG theo nhóm Added/Changed/Fixed.
ADR (Architecture Decision Record): docs/adr/ADR-NNNN-<slug>.md với Bối cảnh – Quyết định – Hệ quả. Bắt buộc khi đổi Contract, Save, cấu trúc thư mục, thêm thư viện, đổi Art Bible, miễn trừ quy tắc.
Bảo mật: không commit Unity license, khóa API, token; agent chỉ chạy trong thư mục repo.
Giấy phép: mọi thứ bên ngoài ghi vào THIRD_PARTY_NOTICES.md (tên, phiên bản, giấy phép, đường dẫn).
Sao lưu: GitHub là bản chính; mỗi cổng G xuất một bản zip build + tag; file .aseprite được lưu trong LFS.
Quyền sở hữu tài sản: asset do thành viên tạo thuộc về dự án chung; ghi tên tác giả trong credits.
18.4. Hạng mục hoãn (không làm ở bản 1.0)
Theo GDD mục 28: các hạng mục bị hoãn không được đưa vào WBS 1.0. Ý tưởng mới đưa vào Issue nhãn “season-2” và chỉ được xét khi tạo ít nhất hai tương tác có ý nghĩa với hệ hiện có hoặc giải quyết một điểm chết rõ ràng (nguyên tắc chống phình phạm vi).
19. Phụ lục
19.1. Checklist khởi động tuần đầu (W1)
| Ai | Việc | Hạn |
Ai
Việc
Hạn
| ALL | Cài Unity 6000.3 LTS đúng bản patch được chốt, Git + Git LFS, IDE agent, .NET 8 SDK, PowerShell 7; clone repo; chạy tools/verify | Thứ Tư 07/10 |
ALL
Cài Unity 6000.3 LTS đúng bản patch được chốt, Git + Git LFS, IDE agent, .NET 8 SDK, PowerShell 7; clone repo; chạy tools/verify
Thứ Tư 07/10
| A | P0.01–P0.04 khung project, Git, AGENTS.md, CI cơ bản | Chủ Nhật 11/10 |
A
P0.01–P0.04 khung project, Git, AGENTS.md, CI cơ bản
Chủ Nhật 11/10
| B | P0.05 Art Bible + palette; P0.06 đưa map heo thả rông và An vào repo | Chủ Nhật 11/10 (bản 1), 18/10 (hoàn tất) |
B
P0.05 Art Bible + palette; P0.06 đưa map heo thả rông và An vào repo
Chủ Nhật 11/10 (bản 1), 18/10 (hoàn tất)
| C | P0.09 concept Khoa + Idle/Walk; P0.10 CombatSandbox | Chủ Nhật 18/10 |
C
P0.09 concept Khoa + Idle/Walk; P0.10 CombatSandbox
Chủ Nhật 18/10
| D | P0.12 bảng GitHub Projects + Issue toàn bộ P0–P1; P0.07 catalog; P0.08 Balance | Thứ Tư 07/10 (bảng), 18/10 (catalog) |
D
P0.12 bảng GitHub Projects + Issue toàn bộ P0–P1; P0.07 catalog; P0.08 Balance
Thứ Tư 07/10 (bảng), 18/10 (catalog)
| ALL | Buổi review G0, demo trong Unity | Chủ Nhật 18/10, 20:00 |
ALL
Buổi review G0, demo trong Unity
Chủ Nhật 18/10, 20:00
19.2. Thuật ngữ
| Thuật ngữ | Ý nghĩa |
Thuật ngữ
Ý nghĩa
| WBS | Work Breakdown Structure – bảng phân rã công việc |
WBS
Work Breakdown Structure – bảng phân rã công việc
| MUST / STRETCH | Việc bắt buộc / làm hơn mức đó thì tốt |
MUST / STRETCH
Việc bắt buộc / làm hơn mức đó thì tốt
| DoR / DoD | Điều kiện bắt đầu / điều kiện hoàn thành |
DoR / DoD
Điều kiện bắt đầu / điều kiện hoàn thành
| ADR | Ghi chép quyết định kiến trúc |
ADR
Ghi chép quyết định kiến trúc
| Asmdef | Assembly Definition – chia code Unity thành các assembly biên dịch riêng |
Asmdef
Assembly Definition – chia code Unity thành các assembly biên dịch riêng
| Batch mode / headless | Chạy Unity bằng dòng lệnh không có giao diện |
Batch mode / headless
Chạy Unity bằng dòng lệnh không có giao diện
| MCP | Model Context Protocol – chuẩn để agent điều khiển công cụ như Unity Editor |
MCP
Model Context Protocol – chuẩn để agent điều khiển công cụ như Unity Editor
| PPU | Pixels Per Unit – số pixel ứng với 1 đơn vị (1 m) trong Unity |
PPU
Pixels Per Unit – số pixel ứng với 1 đơn vị (1 m) trong Unity
| GTTC | Giá trị tham chiếu bằng vàng dùng cho trao đổi ở chợ đêm |
GTTC
Giá trị tham chiếu bằng vàng dùng cho trao đổi ở chợ đêm
| Flow-field | Trường hướng di chuyển tính sẵn trên lưới, cho phép hàng trăm con heo tìm đường rẻ |
Flow-field
Trường hướng di chuyển tính sẵn trên lưới, cho phép hàng trăm con heo tìm đường rẻ
| Telegraph | Dấu hiệu báo trước đòn đánh của quái |
Telegraph
Dấu hiệu báo trước đòn đánh của quái
| Hit-stop | Khựng hình rất ngắn khi đòn trúng để tăng cảm giác lực |
Hit-stop
Khựng hình rất ngắn khi đòn trúng để tăng cảm giác lực
19.3. Lịch sử tài liệu
| Phiên bản | Ngày | Thay đổi |
Phiên bản
Ngày
Thay đổi
| v2.0 | 04/10/2026 | WBS và phân công 4 người ban đầu (PigTycoon_WBS_v2) |
v2.0
04/10/2026
WBS và phân công 4 người ban đầu (PigTycoon_WBS_v2)
| v3.0 | 05/10/2026 | Hợp nhất: monorepo, quy ước tên và công thức file mới, AGENTS.md bắt buộc, quy trình Unity tắt khi agent chạy, code tham chiếu, thư viện, nguồn tham khảo, tham chiếu UI/UX Momodora, cổng nghiệm thu G0–G6, quản trị dự án |
v3.0
05/10/2026
Hợp nhất: monorepo, quy ước tên và công thức file mới, AGENTS.md bắt buộc, quy trình Unity tắt khi agent chạy, code tham chiếu, thư viện, nguồn tham khảo, tham chiếu UI/UX Momodora, cổng nghiệm thu G0–G6, quản trị dự án