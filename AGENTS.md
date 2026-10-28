# AGENTS.md

Nguồn vận hành: sổ tay dự án v3.0 và GDD v6.0. Khi hai bên lệch, giữ số trong GDD và ghi ADR. Không đoán số cân bằng.

## Khi sửa repo

- Unity Editor phải tắt. Project khóa `6000.6.3f1`. Không mở bằng bản editor khác.
- Core trong `Assets/Scripts/Core` không `using UnityEngine`.
- Đúng 2 người chơi: An và Khoa. Trang bị chỉ cộng thưởng.
- Art đang chơi giữ 32 PPU. Lý do: `Docs/adr/ADR-0005-prototype-sprite-ppu.md`.
- Chạy Blender/Unity headless qua `python tools/headless_runner.py` (ngầm, 1 tiến trình nặng một lúc, trần RAM, báo cáo đỉnh bộ nhớ). Thiết kế: `Docs/plans/2026-10-23-headless-render-runner.md`.
- Không đổi công thức giá, rào 16 hướng, hoặc tỉ lệ ô vàng chợ đêm nếu chưa có test đỏ trước.

## Art: bài học bắt buộc trước khi promote

- Đọc [AE-0001 — Farm_Main thưa, phẳng](Docs/art-direction/anti-examples/AE-0001-sparse-flat-farm.md). Archive `.collab/artifacts/rejected/AE-0001-sparse-flat-farm/` là `REJECTED / DO_NOT_REUSE`, không phải nguồn art được duyệt.
- Đọc [AE-0002 — Bề mặt nguyên thủy](Docs/art-direction/anti-examples/AE-0002-primitive-surfaces.md): chặn nước như tấm nhựa đục, cỏ gai/confetti, hoa khối và di tích đá hộp chưa phong hóa; archive bằng chứng là `REJECTED / DO_NOT_REUSE`.
- Không tái tạo `.collab/artifacts/document-world-argon/Farm_Main/` hoặc các file `Farm_Main*` trực tiếp trong thư mục cha; candidate mới dùng đường dẫn mới. Không để job cũ import/promote lại bản đã bác bỏ.
- Giữ mốc đã duyệt: đài thiên văn mái xanh/viền vàng (ảnh số 5), `ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/` và `Tools/observatory*.py`. Không coi công trình này là anti-example.
- Chỉ đạo mới nhất của người dùng là nguồn thẩm quyền cao nhất cho art: tổng thể thần thoại sử thi kết hợp Đông–Tây; riêng thị trấn theo châu Âu cổ. Không suy rộng thành toàn nông trại phải châu Âu.
- Trước promote, mở và so ảnh overview, góc chơi/cận cảnh với mốc đã duyệt; ghi bằng chứng thị giác và chờ người dùng duyệt hình ảnh. Test/validation xanh không thay cho kiểm chứng chất lượng thị giác.

## Kiểm tra

```powershell
cd D:\Pig-tycoon
dotnet run --project Tests/PigTycoon.Runner.csproj
```

SDK lấy từ `global.json` (8.0.417).

## Nhánh

Làm trên `feat/<người>/<mã>`. Không đẩy thẳng `main` khi chưa ghép xong phần của người đó. `main` trên GitHub từng là commit khởi tạo, không chung lịch sử với `Prototype`.
