# AGENTS.md

Nguồn vận hành: sổ tay dự án v3.0 và GDD v6.0. Khi hai bên lệch, giữ số trong GDD và ghi ADR. Không đoán số cân bằng.

## Khi sửa repo

- Unity Editor phải tắt. Project khóa `6000.6.3f1`. Không mở bằng bản editor khác.
- Core trong `Assets/Scripts/Core` không `using UnityEngine`.
- Đúng 2 người chơi: An và Khoa. Trang bị chỉ cộng thưởng.
- Art đang chơi giữ 32 PPU. Lý do: `Docs/adr/ADR-0005-prototype-sprite-ppu.md`.
- Không đổi công thức giá, rào 16 hướng, hoặc tỉ lệ ô vàng chợ đêm nếu chưa có test đỏ trước.

## Kiểm tra

```powershell
cd D:\Pig-tycoon
dotnet run --project Tests/PigTycoon.Runner.csproj
```

SDK lấy từ `global.json` (8.0.417).

## Nhánh

Làm trên `feat/<người>/<mã>`. Không đẩy thẳng `main` khi chưa ghép xong phần của người đó. `main` trên GitHub từng là commit khởi tạo, không chung lịch sử với `Prototype`.
