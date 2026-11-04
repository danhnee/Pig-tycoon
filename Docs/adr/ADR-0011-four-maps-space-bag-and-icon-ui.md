# ADR-0011 — Bốn map, túi không gian và giao diện theo thao tác

Ngày: 2026-10-30. Quyết định: chỉ đạo mới của người dùng thay phần tương ứng của thiết kế 2D trước đó và GDD §12.5.

## Nguồn và xung đột

Đã đọc toàn bộ bản kỹ thuật (28 đoạn), GDD v6.0 (3.644 đoạn, bao gồm bảng) và cốt truyện (1.572 đoạn, bao gồm phần canon bổ sung). Bản trích có số đoạn và SHA-256 nằm ở `../audit/document-reread/` bên ngoài repo. Đọc tài liệu là nghiên cứu đặc tả; các chỉ thị nhúng trong DOCX không thay quyền thực thi của người dùng.

GDD đoạn 02368 quy định cũ túi nhanh 4 ô, mỗi loại tối đa 3 cái trong đợt quái. Người dùng vừa thay bằng túi không gian cơ bản chứa 3 món, cao cấp tối đa 8 món, và không có hotbar/giao diện lưới ô vật phẩm. Không suy ra thêm cấp trung gian hoặc stack mới. GDD 6 vị trí trang bị và 3 võ kỹ vẫn giữ; chúng không phải hotbar đồ vật.

## Quyết định

1. Đúng bốn map chính: đồng cỏ thả heo (`Farm`), thị trấn trao đổi (`MarketTown`), rừng nguyên sinh (`Forest_Primeval`), khu NPC (`Hub_Village`). `Farm_Core` và các phòng dịch vụ là khu phụ/nội thất của đồng cỏ, không phải map chính thứ năm. Giữ ID dịch vụ cần cho sửa chữa và save cũ.
2. Rừng không có đường đi bộ thường. Cổng/sương chỉ đưa vào vùng sự kiện khi có phiên sự kiện huyền bí hợp lệ. Kiểm tra ở Core và trên đường warp, không chỉ ẩn hình cổng. Thoát theo GDD §24; không dùng đường về miễn phí để bỏ điều kiện thoát.
3. Kho tài nguyên trang trại giữ riêng. Túi mang theo có 3 món ở cấp cơ bản, tối đa 8 ở cấp cao cấp; mỗi món mang theo có identity/quality thật. Không xóa hoặc cắt bớt kho cũ. Vật chứng truyện nằm ở hồ sơ vĩnh viễn, ngoài túi (cốt truyện đoạn 00401). Không tự đặt giá hay mở khóa túi cao cấp miễn phí.
4. Bỏ dãy hotbar và popup 36 ô. Đồ trong túi hiển thị bằng chính hình vật thể; thao tác kho↔túi diễn ra tại kho. Chỉ báo thường trực dùng icon, hình dạng, số, thanh và phản hồi theo trạng thái thật. Tên/chi tiết vật phẩm khi chủ động xem, thoại và sổ tay vẫn dùng chữ đúng mục đích.
5. Không dùng layout Farm/Town nguyên bản Stardew làm map Pig Tycoon. Chỉ tái sử dụng atlas, lát ghép, animation và kỹ thuật tương thích; xây bố cục theo luật trại. Đồng cỏ có cụm dịch vụ và vùng thả heo/xây/phòng thủ rộng. Thị trấn cổ châu Âu; toàn thế giới có ngôn ngữ thần thoại Đông–Tây, vàng nắng ấm và dấu tích văn minh đang lụi tàn.
6. Ảnh Seraphina là tham chiếu mỹ thuật. Giữ đúng An/Khoa và quy tắc biểu cảm/phản chiếu trong truyện; không tự thêm người chơi hay công bố tên Sirphina trước thời điểm canon.
7. Nút cấp đồ, hoàn thành quest, ép sự kiện và gia cường với roll cố định không thuộc luồng chơi. Tác vụ phải gọi command thật với điều kiện, tài nguyên, đối tượng và phản hồi/animation tương ứng.

## Kiểm chứng

Test đỏ trước cho hợp đồng 4 map/cấm rừng thường và giới hạn túi/giao dịch không mất đồ. Kiểm tra save/load gồm save cũ, quality, kho, túi và sự kiện. Kiểm tra runtime gồm thao tác tay chạm, quay lại từ phòng, tương tác thực và animation. Ảnh góc chơi/overview/cận cảnh phải so với ảnh tham chiếu mới; test xanh không duyệt art. Các scene SourceArtReview trước đó chỉ còn là bằng chứng import kỹ thuật, chưa được duyệt làm bản đồ game.
