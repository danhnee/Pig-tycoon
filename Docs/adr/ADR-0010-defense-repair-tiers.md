# ADR-0010 — Phân bậc và phí sửa công trình

Ngày: 30/10/2026. Trạng thái: triển khai theo chỉ đạo trực tiếp của người dùng: “theo chức năng cũng như năng lực công trình lấy công trình bậc cao nhất là bạch vân làm mốc ở cao nhất mà đưa xuống”. Đây là bổ sung cân bằng mới được cho phép, không phải số nguyên văn có sẵn trong GDD.

Giữ các khoảng và vật liệu của GDD mục 18. Chốt mức trong khoảng để playtest:

| Bậc | Vàng / 10 độ bền | Phế liệu | Linh Kiện Tinh | Phút game gốc |
|---|---:|---:|---:|---:|
| Nhẹ | 20 | 2 | 0 | 15 |
| Vừa | 50 | 3 | 0 | 30 |
| Nặng | 80 | 4 | 0 | 45 |
| Bạch Vân | 160 | 7 | 1 | 60 |

Nặng: Tháp Hồ Quang, Skyhook, Đại Thiên Lôi, Máy Chiếu Khiên, Máy Cộng Hưởng Huyền Bí, Đài Dò Dị Tượng. Đây là hỏa lực/thiết bị trường năng lượng hoặc thông tin cấp cao, cần sửa chuyên sâu.

Vừa: Nỏ Xuyên Vân, Radar Uy Hiếp, Bàn Tình Báo, Trạm Tiếp Sóng Chợ, Trạm Khí Tượng, Trạm Hồi Sức, Đèn Pha Công Nghiệp, Máy Phát Điện, Bơm Làm Mát, Cột Thu Lôi, Rào Thép, Cổng Gia Cố, Lưới EMP, Tấm Sốc Điện, Đèn Hiệu Đánh Chặn.

Nhẹ: các công trình cơ khí/thụ động còn lại đang có trong registry. Bạch Vân luôn được nhận diện bằng `IsBachVan`, ưu tiên cao hơn mọi nhóm khác. Bản sao bảng theo ID nằm cạnh ADR; `DefenseRepairs` là nguồn thực thi.

Tính số lô bằng làm tròn lên mức hư hại lớn hơn giữa độ bền vận hành và phần trăm HP đã mất, chia 10; không cộng hai kênh để tránh tính phí hai lần. Công trình hỏng có ít nhất một lô và thêm 10% giá xây, làm tròn lên. Hoàn thành đơn khôi phục HP và độ bền; không sinh lại đạn đã dùng.

Thời gian mỗi lô chia hiệu suất Phòng Xây Dựng có sẵn: 40% tự làm, 70% với Kỹ Sư tại cấp 1, 100% với Kỹ Sư cấp phòng từ 2. Lệnh chỉ nhận ở Farm_Core, ngoài đợt quái, trừ tiền qua sổ cái và vật liệu qua Inventory một lần; lưu hàng đợi, tiếp tục qua save/load và thời gian ngủ. Đợt quái tạm dừng tiến độ.

`item_alloy_scrap` là phế liệu đang có. `item_fine_component` là ID dành cho Linh Kiện Tinh; tuyến nhận vật liệu này qua nội dung gameplay chưa được nối, không coi kiểm tra hàng đợi là bằng chứng đã hoàn thiện toàn bộ tuyến sửa Bạch Vân.

Kiểm chứng: test đỏ trước triển khai ở `.agent_scratch/compatibility/repair-red.log`; kiểm tra vị trí, chặn trong đợt quái, trừ phí/vật liệu một lần, không hoàn tất tức thì, không đặt trùng, giữ hàng đợi qua save/load và thứ bậc Bạch Vân.
