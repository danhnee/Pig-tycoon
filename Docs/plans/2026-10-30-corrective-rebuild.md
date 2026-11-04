# Sửa theo phản hồi mới — 2026-10-30

Nguồn quyết định: yêu cầu mới nhất của người dùng và ADR-0011. Thiết kế trước được sửa ngay trong phạm vi đã cho phép, không yêu cầu duyệt lại toàn bộ kế hoạch.

## Hợp đồng phải kiểm chứng

| Lát chơi | Điểm nối thực | Bằng chứng cần có |
|---|---|---|
| Kho và túi | UnifiedInventory giữ kho; SpaceBag mang 3/8 món; cửa kho mở chuyển đồ | Đầy túi không trừ kho, giữ quality, trả món, lưu/tải, save cũ không mất đồ |
| Bốn map | PixelWorldRoutes + WorldLocationGraph + scene portal | Không có map chính thứ năm; direct warp không vượt gate; rừng chỉ qua phiên sự kiện; thoát đúng GDD |
| UI thường trực | Thay hotbar/nhãn giải nghĩa bằng chỉ báo và thao tác theo đối tượng | Touch hit area, trạng thái khóa, cooldown, phản hồi chạm; sổ An/Khoa giữ đúng nội dung |
| Cửa và trạm | Kho, Phòng Xây Dựng, nghiên cứu, gia cường, bảng điều khiển, giường | Vào/ra, đúng trạm, đúng tài nguyên, animation thao tác, save không đổi map sai |
| Xây/nạp/thủ | Blueprint + NPC + phòng + vật liệu + thời gian + footprint; nạp từ kho thật | Không spawn đầy đạn miễn phí; không trừ tiền khi đặt thất bại; thông số riêng Bạch Vân |
| Chợ/Bí Nhân | Một command giao dịch; lịch spawn và vị trí thật | Đúng món/GTTC/phí, không vàng ở Bí Nhân, hết thời gian không mua được |
| Võ/gia cường | Skill definition, học/phái/chỉ số gốc, cooldown, hitframe; công thức và nguyên liệu | Không tự cấp trang bị/ấn; hiệu ứng từng chiêu, thưởng trang bị không mở điều kiện học |
| Art bốn map | Atlas hiện có tái bố cục theo luật; phong hóa, lớp cỏ, nước, mái xanh/viền vàng | Ảnh overview/góc chơi/cận cảnh và người dùng duyệt trước promote |
| Mobile | Flutter mở Unity, input/safe area, save trước đóng | APK/device/emulator thật; không coi Dart test hoặc Windows build là bằng chứng Android |

## Thứ tự thực hiện

1. Ghi nhận đủ tài liệu/ảnh và sai lệch, đóng Editor trước sửa. Hoàn tất nghiên cứu tài liệu trước thay luật.
2. Viết test đỏ rồi sửa túi+kho và graph/gate rừng trên các vùng ghi độc lập.
3. Nối save/load, thay popup túi và cửa kho; bỏ hotbar mà giữ khả năng gọi hành động thật.
4. Loại command debug khỏi luồng overview; nối tác vụ vào trạm/cửa thay cho nút cấp thưởng.
5. Kiểm tra .NET mục tiêu và hồi quy; chỉ sau khi ngừng sửa mới dùng runner Unity6000.6.3f1.
6. Dựng lại đồng cỏ theo bố cục dịch vụ ở rìa, vùng thả heo và phòng thủ lớn; dùng tile/sprite nguồn như vật liệu, không giữ map Stardew nguyên bản.
7. Hoàn thiện tiếp vòng xây/nạp/chiến đấu/giao dịch, action animation và UI; kiểm chứng từng lát với tài nguyên thật.

Không đánh dấu hoàn thành toàn game từ số test. Các điểm phát hiện trong audit vẫn là công việc phải làm, không phải ngoại lệ nghiệm thu.
