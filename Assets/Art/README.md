# 🎨 HỆ THỐNG QUẢN LÝ TÀI NGUYÊN ART - PIG TYCOON (2D PIXEL ART)

Tài liệu hướng dẫn phân cấp cấu trúc thư mục, quy chuẩn Pixel-Art, PPU (Pixels Per Unit), Điểm neo (Pivot Points), Khung 9-Slice và quy tắc đặt tên cho toàn bộ tài nguyên đồ họa của dự án Pig Tycoon.

---

## 📂 1. Cấu Trúc Thư Mục Chuẩn Hóa (`Assets/Art/`)

```
Assets/Art/
├── Sprites/
│   ├── Characters/
│   │   ├── Player/               # Nhân vật Khoa 64x64 (Idle, Walk, Happy, Action 4 hướng)
│   │   ├── PlayerBackpack/       # Nhân vật Khoa đeo Ba-lô 64x64 (Idle, Walk 4 hướng)
│   │   └── Enemies/              # Quái vật & Kẻ địch (Chuột, Kẻ Cào Phá, Đạo Tặc Bắt Heo)
│   ├── Environment/
│   │   ├── Buildings/            # Công trình (Chuồng, Si-lô, Giếng, Tháp Canh, Máng Ăn/Nước)
│   │   ├── Fences/               # Hàng rào 16 hướng modular, overlay nứt & cọc gãy
│   │   ├── Nature/               # Cảnh quan (Cây thảo nguyên, Tảng đá, Bụi hoa)
│   │   └── Training/             # Thao trường võ học (Bù nhìn Rơm, Sắt, Thép)
│   ├── Pigs/
│   │   ├── hong_dien/            # Heo Hồng Điền (Idle, Walk1-2, Eat, Sleep)
│   │   ├── hu_the/               # Heo Hư Thể (Hắc Ám) (Idle, Walk1-2, Eat, Sleep)
│   │   ├── kim_tho/              # Heo Kim Thọ (Hoàng Kim) (Idle, Walk1-2, Eat, Sleep)
│   │   └── lam_khe/              # Heo Lam Khê (Thủy Lam) (Idle, Walk1-2, Eat, Sleep)
│   ├── VFX/
│   │   ├── Combat/               # Vệt chém (Slash), đâm (Thrust), chấn động búa (Smash)
│   │   └── Telegraphs/           # Cảnh báo Raid: Đuốc lửa đạo tặc, Tractor beam UFO
│   └── Weapons/                  # Vũ khí 24x24 (Dao Gỗ, Kiếm Sắt, Búa Tạ, Giáo Tre)
├── Tiles/
│   ├── Sprites/                  # Sprite nền gạch (Cỏ, Cỏ cao, Cỏ Thảo Nguyên, Đất, Đá, Nước, Bùn)
│   ├── TileAssets/               # Scriptable Tile Assets (.asset) cho Unity Tilemap
│   └── Palettes/                 # Tile Palette Prefabs
└── UI/
    ├── Controls/                 # Cần điều khiển ảo (Joystick base, knob) & Grid Selector
    ├── Frames/                   # Khung giao diện 9-Slice (Gỗ, Giấy da, Slot, Nút bấm)
    ├── Icons/                    # Biểu tượng HUD (Vàng, Máu tim, Sấm năng lượng, Nút đóng)
    ├── Items/                    # Vật phẩm rơi (Loot), Phế liệu UFO, Nông cụ, Thức ăn
    └── Tools/                    # Biểu tượng 10 công cụ làm nông & phòng thủ (32x32)
```

---

## 📐 2. Quy Chuẩn Kỹ Thuật (Asset Specifications)

| Danh Mục | Kích Thước Canvas (px) | PPU (Pixels Per Unit) | Điểm Neo (Pivot) | 9-Slice Border (L, B, R, T) |
|---|---|---|---|---|
| **Player (Khoa)** | $64 \times 64$ | `64` | `(0.5, 0.25)` Chân nhân vật | Không |
| **Enemies (Quái vật)** | $32 \times 32$ | `32` | `(0.5, 0.5)` Tâm | Không |
| **Pigs (Bầy heo 4 hệ gen)** | $16 \times 16$ | `16` | `(0.5, 0.5)` Tâm | Không |
| **Buildings (Công trình)** | $64 \times 64 \rightarrow 128 \times 128$ | `32` | `(0.5, 0.0)` Đáy công trình | Không |
| **Modular Fences (Hàng rào)** | $32 \times 32$ | `32` | `(0.5, 0.5)` Tâm ô lưới | Không |
| **Nature (Cảnh quan)** | $32 \times 32 \rightarrow 64 \times 64$ | `32` | `(0.5, 0.0)` Gốc cây/đá | Không |
| **Weapons (Vũ khí)** | $24 \times 24$ | `64` | `(0.479, 0.146)` Chuôi cầm | Không |
| **Combat VFX** | $48 \times 48 \rightarrow 64 \times 64$ | `64` | `(0.5, 0.5)` Tâm đòn đánh | Không |
| **UI Frames (Khung viền)** | $32 \times 32 \rightarrow 48 \times 48$ | `32` | `(0.5, 0.5)` Tâm | `Vector4(8~16, 8~16, 8~16, 8~16)` |
| **UI Icons / Tools / Items** | $32 \times 32$ | `32` | `(0.5, 0.5)` Tâm | Không |
| **Tiles (Gạch Tilemap)** | $32 \times 32$ | `32` | `(0.5, 0.5)` Tâm | Không |

---

## 🔄 3. Cơ Chế Tự Động Nạp & Tương Thích Ngược (`UISpriteLoader`)

Hệ thống cung cấp lớp [`UISpriteLoader`](file:///home/danh/.gemini/antigravity/scratch/pig-tycoon-core/Assets/Scripts/UI/UISpriteLoader.cs) với các tính năng vượt trội:
1. **Dynamic File Indexing**: Tự động đánh chỉ mục toàn bộ file `.png` trong `Assets/Art/`.
2. **Path Auto-Resolution**: Khi gọi `LoadSprite("tên_file.png")` hoặc đường dẫn cũ, hệ thống tự động ánh xạ tới thư mục con mới.
3. **Point Filtering & Texture Memory Caching**: Toàn bộ sprite pixel art được thiết lập `FilterMode.Point` và lưu cache theo khóa `(path, PPU, pivot)` tránh rò rỉ bộ nhớ.
