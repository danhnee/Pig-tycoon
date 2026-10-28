# AE-0001 — Farm_Main thưa, phẳng: REJECTED / DO_NOT_REUSE

Ngày ghi nhận: 2026-10-18. Đây là bản người dùng đã bác bỏ; giữ ảnh, recipe và mã băm gốc để tránh lặp lại. Các mô hình/export nặng đã được xóa theo chỉ đạo dọn ổ D; xem audit bên dưới. Không dùng làm mốc chất lượng, không import hoặc promote vào scene đang chơi.

## Bằng chứng gốc

- [Ảnh người dùng bác bỏ — bản sao giữ nguyên byte](../../../.collab/artifacts/rejected/AE-0001-sparse-flat-farm/evidence/user-rejected-codex-clipboard-db7b1cec-7c5b-4b4b-8de0-a0a123eaf838.png).
- [Cận cảnh Farm_Main bị bác bỏ](../../../.collab/artifacts/rejected/AE-0001-sparse-flat-farm/payload/document-world-argon/Farm_Main/Farm_Main-near.png).
- [Toàn cảnh Farm_Main bị bác bỏ](../../../.collab/artifacts/rejected/AE-0001-sparse-flat-farm/payload/document-world-argon/Farm_Main/Farm_Main-overview.png).
- [Manifest SHA-256 trước khi chuyển](../../../.collab/artifacts/rejected/AE-0001-sparse-flat-farm/manifest-before.json) và [sau khi chuyển](../../../.collab/artifacts/rejected/AE-0001-sparse-flat-farm/manifest-after.json).

Archive bằng chứng chính thức: `.collab/artifacts/rejected/AE-0001-sparse-flat-farm/`. Lần cách ly ban đầu chuyển các tệp vào archive và ghi SHA-256 trước/sau. Theo chỉ đạo tiếp theo yêu cầu giải phóng dung lượng thực, tám tệp BLEND/BLEND1, GLB và FBX bị bác bỏ trong payload này đã được xóa; toàn bộ PNG, metadata và hai manifest gốc vẫn giữ nguyên byte. Các thư mục nghiên cứu khác không thuộc kết luận này.

[Audit dọn ổ D](../../../.collab/artifacts/disk-cleanup-sol-high/cleanup-result.json) ghi từng đường dẫn đã xóa, dung lượng, SHA-256 gốc và lý do; [kế hoạch trước xóa](../../../.collab/artifacts/disk-cleanup-sol-high/deletion-plan.json) ghi hash bằng chứng được giữ cùng 19 tệp được bảo vệ. [Recipe tham khảo](../../../.collab/artifacts/disk-cleanup-sol-high/reference-only-recipe/recipe.json) dẫn tới placement manifest gốc và bản sao chỉ đọc của hai module được manifest gọi tên. Entry point cũ đã được worker khác sửa cho candidate mới trước khi dọn; không gắn nhãn mã hiện tại là bản generator gốc. Bản sao có đuôi `.DO_NOT_EXECUTE.txt`, không chạy lại hoặc dùng làm mốc art.

## Vì sao thất bại

1. Mặt bằng rộng, gần như phẳng và trống; cây, ao, nhà rải rác không tạo lớp không gian hoặc trải nghiệm khám phá của thế giới thần thoại sử thi.
2. Nhà phổ thông, mái lớn và chi tiết ít đã thay thế bản sắc kiến trúc fantasy giàu chi tiết. Lặp nhà/shelter đơn giản khiến cảnh giống danh mục nhà kho hơn một nơi có chức năng, lịch sử và đời sống.
3. Dãy núi xám dựng quá sát thành một bức tường, lấn át công trình và cắt mất chiều sâu. Thêm nhiều núi hoặc cây không tự sửa được bố cục.
4. Cảm giác đặt vật thể lên một tấm nền/plinth rời, có thể trông như đang nổi, không tạo địa hình liên tục và đáng tin ở góc chơi.
5. Vàng nâu xỉn, xám núi và bóng tối làm tổng thể đục; thiếu điểm sáng, tương phản chất liệu và thứ bậc màu để dẫn mắt.
6. Kiểm tra kỹ thuật, export thành công, collider đúng hoặc validation xanh không chứng minh chất lượng thị giác. Cần xem và so ảnh thật trước khi công bố đạt.

## Mốc đã duyệt vẫn phải giữ

Ảnh số 5 người dùng duyệt là **đài thiên văn mái xanh, viền vàng**, nguồn ảnh `C:/Users/Admin/AppData/Local/Temp/codex-clipboard-0e900d61-5f16-4698-853f-cbf7171c16c1.png`. Đây là mốc kiến trúc tốt: silhouette rõ, mái/ngói có lớp, đá xây, cửa vòm, đèn và dụng cụ thiên văn thể hiện chức năng. **Không gắn nhãn rejected cho ảnh này hoặc công trình này.**

Giữ nguyên `ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/`, `Tools/observatory*.py` và các scene gốc trong `Assets/Scenes/`. Manifest ghi hash trước/sau để kiểm chứng phạm vi dọn dẹp.

Chỉ đạo mới nhất của người dùng có hiệu lực cao nhất: tổng thể là **thần thoại sử thi kết hợp Đông–Tây**; **riêng thị trấn** dùng kiến trúc châu Âu cổ. Bài học này không cấm kết hợp Đông–Tây và không bắt toàn bộ nông trại thành châu Âu.

## Chặn tái diễn

- Không tái tạo các đường dẫn đã cách ly `document-world-argon/Farm_Main/` hoặc các file `Farm_Main*` ở thư mục cha. Bản mới dùng đường dẫn candidate có tên mới, có tham chiếu rõ tới chỉ đạo mới nhất.
- Trước promote, mở ảnh overview và ảnh góc chơi/cận cảnh, so trực tiếp với mốc đã duyệt. Ghi rõ silhouette, mức chi tiết, chất liệu, màu, chiều sâu, công năng và điểm còn thiếu. Không thay việc so ảnh bằng log test hoặc số lượng vật thể.
- Chỉ đưa vào scene đang chơi sau khi người dùng duyệt hình ảnh theo chỉ đạo hiện hành. Không để job cũ tự export/import lại bản rejected.
- Lệnh kiểm tra sau dọn: `C:/Users/Admin/AppData/Local/Python/bin/python.exe Tools/cleanup_generated_art_cache.py --verify`. Hai manifest cách ly vẫn là hồ sơ lịch sử trước khi xóa; các payload nặng không còn là dữ liệu phải giữ.
- Lệnh archive ban đầu `C:/Users/Admin/AppData/Local/Python/bin/python.exe Tools/quarantine_rejected_farm.py --verify` cũng hiểu hồ sơ xóa đã audit: chỉ chấp nhận model có đường dẫn, hash/size, nguồn gốc và biên thư mục khớp manifest gốc, plan, kết quả PASS, journal thời điểm xóa và kiểm chứng độc lập. Không miễn kiểm tra ảnh/metadata hoặc tài sản được bảo vệ; model đã xóa xuất hiện lại vẫn là lỗi.
