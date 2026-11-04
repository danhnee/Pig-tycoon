# ART SPEC v2: góc nhìn top-down 3/4 + thần thoại sử thi Đông–Tây (bắt buộc)

Ngày 31/10/2026. Bản v1 (đợt Argon A2/A3/A4) bị người dùng bác: vẽ mặt đứng nhìn ngang trong khi game nhìn từ trên xuống, và thiếu chất thần thoại sử thi Đông–Tây.

Nguồn:
- "Bản kỹ thuật" mục 5.1: "Pixel art top-down 3/4"; nguồn sáng trên-trái; viền 1 px màu tối của palette, không đen tuyệt đối; silhouette nhận ra được ở thang xám.
- GDD 3.6, dòng 632: "Góc nhìn trên xuống 3/4".
- Truyện: nền văn minh thần thoại sụp đổ khi khu rừng tràn qua 24 điểm neo của Đại Giới Lưu; Thánh Minh Nhật Giả; Sirphina là tinh linh đồng cỏ vàng; di tích, văn bia cổ.
- Chỉ đạo người dùng (thẩm quyền cao nhất): tổng thể thần thoại sử thi kết hợp Đông–Tây, ở giai đoạn lụi tàn nhìn từ phía con người, vàng nắng ấm, rất cổ xưa. Riêng thị trấn theo châu Âu cổ.

## 1. Phối cảnh (lỗi số 1, không được sai)

**Mốc đã duyệt:** các công trình ở bãi chăn, xem `D:\Pig-tycoon\.collab\claude-2d\refs\ref_projection.png` (nhà chính, kho, đài thiên văn, phòng nghiên cứu).

Camera đặt sau lưng nhân vật, trên cao, chếch xuống khoảng 40–50° (top-down 3/4 kiểu RPG):

| Thành phần | Yêu cầu |
|---|---|
| Mái | Nhìn từ trên xuống, thấy rõ các mặt mái, nóc, đỉnh hồi, ống khói, mái phụ. Chiếm 45–65% chiều cao sprite. |
| Mặt tiền | Chỉ thấy bên dưới mái, bị rút ngắn, cao 25–40% sprite. Có cửa, cửa sổ, biển hiệu hình. |
| Nền / móng | Dải nền phía trước, sâu 6–15% sprite (bậc thềm, sân lát, bệ đá). |

- Tường hồi bên chỉ hơi thấy hoặc không thấy. Không vẽ phối cảnh isometric chéo 45° theo phương ngang.
- **CẤM** vẽ mặt đứng nhìn ngang (elevation) kiểu game đi cảnh, mái chỉ còn một dải mỏng ở trên.
- Cây, tượng, giếng, chum… cũng theo cùng góc: thấy được mặt trên (tán cây, miệng giếng, nắp chum).

**Kích thước:**
- 32 px = 1 m (ADR-0005).
- Bề ngang sprite = bề ngang thật của công trình. Chiều cao sprite = độ sâu mặt đất × ~0,6 + chiều cao công trình × ~0,7.
- Ví dụ: nhà 7 m ngang, 6 m sâu, 6 m cao → sprite khoảng 224 × 250 px.

## 2. Thần thoại sử thi Đông–Tây (lỗi số 2)

Thế giới là con người đang sống trên tàn tích của một nền văn minh thần thoại đã sụp đổ. Mọi khu vực phải để lộ lớp cổ đại ấy:

- Móng và nền là đá cổ khổng lồ đã phong hóa, đè lên rễ cây.
- Phù điêu đĩa mặt trời, vân mây, hoa sen, rồng; kèm cột và vòm La Mã.
- Văn bia hai thứ chữ cổ, chỉ là hoa văn, không đọc được.
- Tượng thần vỡ, đồng xanh rỉ, ngọc, dát vàng đã mờ.

**Đông–Tây phải trộn trong từng công trình,** không tách ra phố Tây riêng, làng Đông riêng:
- Mái ngói cong đầu đao đặt trên khung gỗ kiểu Tây.
- Vòm La Mã trên móng chạm hoa sen.
- Tháp chuông có mái chồng diêm.
- Cửa nguyệt môn trong tường đá Tây.

**Bảng màu:** vàng nắng ấm; ngói đất nung hoặc xanh đá phiến, viền vàng đồng điệu với đài thiên văn đã duyệt; đá cổ màu kem/xám ấm phủ rêu; đồng xanh; ban đêm có đèn lồng tím–cam (thị trấn).

**Giai đoạn lụi tàn:** đồ cổ thì hư hỏng và được con người vá víu, tái dùng (tường xây chèn mảnh phù điêu cổ, mái vá). Không có gì mới tinh, không có hình khối hộp trơn (AE-0002).

## 2b. Nguyên tắc tối thượng (người dùng, 31/10/2026, lần 2)

Kiến trúc của game sử thi thần thoại phải **mơ mộng, kỳ vĩ, do con người tưởng tượng ra**. Không được chép thực tế, không được bám chữ "làng" hay "thị trấn" rồi vẽ cho có. Mỗi công trình phải trả lời được câu hỏi: "trong một thế giới thần thoại đang lụi tàn, con người sẽ dựng nó kỳ vĩ và đẹp đến mức nào?"

Đặc điểm chung:
- Quy mô lớn hơn đời thường.
- Hình khối táo bạo: mái tầng chồng tầng, vòm và tháp cao vút, cầu và tường thành uốn theo địa hình.
- Vật liệu quý đã phai: đá trắng ngà, đồng, ngọc, dát vàng mờ.
- Ánh sáng ấm như cổ tích.

Thực tế chỉ là gốc rễ, không phải đích đến.

**Mục 3 dưới đây được viết lại theo nguyên tắc này và thay thế bản trước.**

## 3. Theo map

**Thị trấn chợ = thành phố chợ cổ châu Âu kỳ ảo (châu Âu cổ chỉ là gốc):**

Một thị trấn như bước ra từ cổ tích, dựng trên bậc thềm và cầu đá của một kinh đô thần thoại đã sụp.

- **Nhà phố cao vút:** nhiều tầng nhô ra, tháp nhọn và ban công cong, mái xanh đá phiến hoặc đỏ đất nung viền vàng (cùng tinh thần đài thiên văn đã duyệt).
- **Kiến trúc vắt qua phố:** cầu nối giữa các nhà, vòm cổng thành khổng lồ đứng giữa phố.
- **Công trình dịch vụ là những lâu đài nhỏ có cá tính riêng:**
  - Hội quán tuyển người có tháp chuông.
  - Nhà giám định có mái vòm kính hình con mắt.
  - Chuồng lính thuê trong một pháo đài.
  - Sân đấu giá đêm là một nhà hát lộ thiên dưới vòm đá cổ.
  - Bưu điện có tháp chim đưa thư.
  - Trạm kiểm dịch có bể nước thánh.
- **Đông xen vào Tây:** đèn lồng giấy, mái đầu đao trên tháp, cửa nguyệt môn, rồng đồng cuốn cột.
- Ban ngày rực nắng vàng; ban đêm chợ đèn lồng tím–cam huyền bí.

**Làng Bờ Rìa = bản đồ làng bình thường, rộng thoáng như Stardew; chất "thành lũy cổ Việt Nam" nằm ở PHONG CÁCH CÔNG TRÌNH:**

(Đính chính 31/10: KHÔNG làm bản đồ hình tròn, KHÔNG có vòng lũy. Người dùng bác thẳng ý tưởng đó.)

- **Bản đồ:** hình chữ nhật. Đường làng, sân đình, ao sen trước đình, vườn, ruộng rau, rặng tre. Nhà đặt thưa, mỗi nhà có sân và vườn riêng.
- **Công trình:** mang khí chất kiến trúc cổ Việt Nam thần thoại hóa, như cổng thành và hoàng thành cổ: mái ngói cong nhiều tầng đầu đao, nền đá cổ phong hóa, cột son phai, đĩa mặt trời đồng, nghê và rồng đá, gạch đá rêu phong.
- **Cổng làng:** cổng đá cổ kiểu cổng thành có vọng lâu, đứng tự do ở đầu đường vào làng, không gắn với tường thành nào.
- **Địa điểm truyện:**
  - Quán bà Sáu: lầu quán nhiều tầng mái, nồi chặn gạch, bát úp.
  - Đình làng: nhà văn hóa, đặt trên nền đá cổ.
  - Giếng ngọc tạc sen rồng.
  - Bia "BỜ RÌA" trên lưng rùa đá.
  - Đài trống đồng: một công trình đứng riêng.
  - Miếu cổ dưới gốc đa.
  - Bến xe ôm anh Tư với chiếc Cub cũ.
- **Cây:** cổ thụ đa, me, tre, chuối, cau.

**Rừng nguyên sinh:**
- Cổ thụ khổng lồ, rễ nổi, di tích bị rễ siết, sương lạnh, trăng hoặc nắng xuyên tán.
- Các điểm sự kiện là tàn tích thần thoại Đông–Tây phong hóa.

**Bãi chăn (đã ổn):** giữ như hiện tại.

## 4. Kỹ thuật giao file

- PNG 1x (32 px = 1 m), nền #FF00FF đặc, không chạm mép, chân công trình ở đáy.
- Viền ấm 1 px, sáng trên-trái, pixel cứng không nhòe.
- Mỗi nhóm kèm `manifest.json` (file, sha256, footprint m) và contact sheet: đặt cạnh An 70 px và cạnh `ref_projection.png` để chứng minh đúng phối cảnh.
- Tự kiểm trước khi giao: mặt mái chiếm ≥45% chiều cao sprite; nếu không đạt thì vẽ lại.
