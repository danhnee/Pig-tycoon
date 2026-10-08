# Art Bible v1 — bản 1 (P0.05)

Ngày khóa: 2026-10-08. Owner: B. Nguồn số: sổ tay mục 5 và GDD 3.6. Palette đo từ `Assets/Art` trên nhánh Prototype `a15aa0c`.

## Palette

48 màu tại `palette.gpl` và `palette.ase`. Bốn nhóm: Farm (ấm), Pasture (cỏ/đất), Tech (xanh/cyan), Occult (tím/xanh lạnh), Danger (đỏ/cam và điểm nhấn).

Art đang có trong repo **chưa** nằm trên palette này.

FACT đo ngày 2026-10-08:

- 107 file PNG.
- 657 màu khác nhau trên pixel không trong suốt.
- 82 sprite là canvas 32×32. Các sprite lớn hơn (48, 64, 96, 128) là công trình và UI.

Không lượng tử hóa 107 sprite trong bản này. Ép màu khi chưa xem trong Unity sẽ đổi hình map đang chơi được.

## Import

`Assets/Editor/PixelSpriteImportPostprocessor.cs` áp lên `Assets/Art/**` lúc import:

- Filter Mode = Point
- Mip Map = tắt
- Compression = None
- Alpha is transparency = bật

Không đổi `spritePixelsToUnits`.

## PPU

Sổ tay mục 5.1 khóa 16 PPU. Sprite trong repo đang để 32 (`spritePixelsToUnits: 32` trên `grass_base.png.meta` và sprite nhân vật).

16 PPU trên cùng file 32 px biến ô 1 m thành ô 2 m và kéo mọi collider/scene đang căn theo tâm ô 1 m. Vì vậy bản 1 giữ 32 PPU cho art cũ. Lý do và hệ quả: `Docs/adr/ADR-0005-prototype-sprite-ppu.md`.

Preset `PRE_Sprite_Pixel16` chưa tạo thành asset Unity vì máy này không mở được project bằng editor đúng bản (xem blocker trong handover).

## An

Sprite đang dùng: `player_down_*`, `player_up_*`, `player_side_*`, `player_action.png`. Đông và Tây dùng sprite ngang; Tây lật `flipX`. Đó là 4 hướng nhìn, chưa phải 4 sheet riêng và chưa đủ bộ animation mục 5.2 (Run, Attack, Dodge, Hurt, Death).

Object trong `Farm_Main` tên `PF_Player_An`. Chưa có file prefab `.prefab` tách riêng.

## Map

Scene chơi được đổi tên thành `Assets/Scenes/Farm_Main.unity` (GUID giữ nguyên). Tilemap, rào, cổng nam rộng 6,2 m vẫn là layout Prototype: thảo nguyên 90×64 m, chuồng 40×28 m. Chưa phải 120×90 m của GDD 3.6.

Sorting layer đã khai báo theo thứ tự Ground, Decor, Actors, Canopy, UI_World. Renderer vẫn ở layer Default vì Y-sort hiện dựa trên `sortingOrder` chung; tách layer lúc này làm cây và người chơi hết che nhau.

## Font

Chưa có font pixel đủ dấu tiếng Việt trong repo. Không nhét font giả.
