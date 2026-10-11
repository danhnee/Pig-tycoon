# ADR-0005 — Giữ sprite Prototype ở 32 PPU cho tới lần rescale

## Bối cảnh

Sổ tay mục 5.1 khóa 16 PPU để ô xây 2×2 m bằng 32×32 px. GDD 3.6 khóa 1 m = 1 đơn vị và ô xây 2×2 m. Art và scene trên Prototype được căn ở 32 PPU, lưới tile 1×1 m, tâm ô `(x + 0.5, y + 0.5)`.

## Quyết định

Art đã import giữ `spritePixelsToUnits = 32`. Importer mới không ghi đè PPU. Lần rescale sang 16 PPU phải đi cùng chỉnh scene, collider và tâm ô, rồi mới được coi là khớp sổ tay 5.1.

## Hệ quả

Camera mặc định của scene vẫn là ortho 5.2 (cao khoảng 10,4 m), chưa phải khung GDD 40×22 / 64×36 / 128×72. Ba khung đó có trong `CameraFrames` và nút Dev. Bật chúng trên art 32 PPU làm nhân vật nhỏ hơn layout đang chơi.

Ô xây 2 m chưa snap lên lưới đặt rào/máng. Tâm ô 2 m không trùng tâm ô 1 m hiện tại, nên đổi snap sẽ phá hàng rào 16 hướng vừa khóa ở `a15aa0c`.
