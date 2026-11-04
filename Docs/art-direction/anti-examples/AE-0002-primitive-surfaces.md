# AE-0002 — Bề mặt nước, cỏ, hoa và di tích thô: REJECTED / DO_NOT_REUSE

Ngày ghi nhận: 2026-10-19. Người dùng bác bỏ cách thể hiện bề mặt nguyên thủy trong các ảnh dưới đây. Nhãn này áp dụng cho các lỗi được nêu, không tự động bác bỏ mọi vật thể xuất hiện trong ảnh.

## Bằng chứng gốc

- [Thác nhìn chính diện](../../../.collab/artifacts/rejected/AE-0002-primitive-surfaces/evidence/user-rejected-codex-clipboard-1b5abd9e-ba00-46ab-ba46-b39401215e69.png).
- [Thác nhìn nghiêng](../../../.collab/artifacts/rejected/AE-0002-primitive-surfaces/evidence/user-rejected-codex-clipboard-9738c0f4-559f-4db6-93fb-dc74b7d49d2f.png).
- [Ao, cỏ nhọn và hoa khối](../../../.collab/artifacts/rejected/AE-0002-primitive-surfaces/evidence/user-rejected-codex-clipboard-1a84419b-7697-49b3-b574-e006bb1cb2df.png).
- [Manifest nguồn, kích thước và SHA-256 bản gốc/bản sao](../../../.collab/artifacts/rejected/AE-0002-primitive-surfaces/manifest.json).

Archive `.collab/artifacts/rejected/AE-0002-primitive-surfaces/` chỉ giữ ba PNG nguyên byte và manifest; không nhân bản model, recipe hoặc tài sản đã duyệt. Đây là bằng chứng chống tái diễn, không phải nguồn art để import/promote.

## Lỗi phải chặn

- **Nước như tấm nhựa đục:** thác thành dải xanh bóng, dày và liền khối; gợn sáng kéo dọc không tạo cảm giác dòng nước tách, rơi, va đá và hòa vào mặt hồ. Mặt ao đục, đồng đều cùng vài nét gợn đặt lên trên chưa thể hiện được độ sâu, phản xạ và tiếp xúc bờ.
- **Cỏ gai/confetti:** cụm tam giác nhọn, dựng thẳng và rải đều làm nền đọc thành vụn cứng. Cần cỏ thấp có lá cong, độ dày và hướng mọc theo mảng, tiếp đất rõ, giữ đường đi dễ đọc.
- **Hoa khối và di tích đá hộp:** hoa màu chói như các khối đặt trên đế; đá/di tích còn cạnh hộp sạch, thiếu mẻ vỡ, phong hóa và chuyển tiếp với đất/cây. Sửa tạo hình và vật liệu thực; tăng số lượng hoặc thêm noise không tự sửa được các lỗi này.

## Kiểm chứng trước promote

Mở đúng file DCC ứng viên, kiểm tra hình học, độ dày, normal, shader và nguồn texture thực tế. Với nước, kiểm tra đường chảy và vùng va/chân thác; với thực vật/đá, kiểm tra silhouette và tiếp xúc đất. Dùng overview, góc chơi và cận cảnh với camera/ánh sáng được ghi rõ; so trực tiếp với ảnh đích và ảnh bị bác bỏ. Không dùng góc máy che lỗi, ảnh chỉnh hậu kỳ hoặc hiệu ứng chỉ đúng ở một hướng nhìn để thay cho model/material thực.

Ghi đường dẫn ứng viên, ảnh render thực, điểm đã sửa và điểm còn thiếu; chờ người dùng duyệt hình ảnh trước khi đưa vào scene đang chơi. Hash, số đỉnh/triangle/vật thể, export thành công hay test/validation xanh chỉ chứng minh phần kỹ thuật tương ứng, không phải nghiệm thu chất lượng art.

**Giữ mốc đã duyệt:** đài thiên văn mái xanh/viền vàng (ảnh số 5), `ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/` và `Tools/observatory*.py` không thuộc anti-example này. Tổng thể vẫn là thần thoại sử thi kết hợp Đông–Tây; riêng thị trấn theo châu Âu cổ. Bài học về chất lượng bề mặt không cấm phong cách Đông–Tây hoặc buộc cả nông trại thành châu Âu.
