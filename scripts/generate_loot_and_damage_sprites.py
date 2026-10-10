import os
from PIL import Image, ImageDraw

def create_sprite(path, size, draw_func):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw_func(draw, size[0], size[1])
    img.save(path)
    print(f"Generated: {path}")

# 1. Bao Cám Ngô (item_corn_feed.png) - 32x32
def draw_corn_feed(d, w, h):
    # Bao tải vải thô màu nâu nhạt
    d.rounded_rectangle([7, 9, 24, 28], radius=3, fill=(195, 155, 105, 255), outline=(120, 85, 50, 255))
    # Túm dây buộc cổ bao
    d.rectangle([11, 7, 20, 10], fill=(225, 190, 140, 255), outline=(120, 85, 50, 255))
    d.line([(10, 8), (21, 8)], fill=(90, 60, 30, 255))
    # Hạt ngô vàng vương vãi
    d.rectangle([13, 14, 18, 19], fill=(255, 215, 0, 255))
    d.point([(14, 15), (17, 16), (15, 18), (14, 23), (18, 22)], fill=(255, 240, 120, 255))
    d.rectangle([12, 13, 19, 20], outline=(180, 130, 0, 255))

# 2. Đinh Rỉ (item_rusty_nail.png) - 32x32
def draw_rusty_nail(d, w, h):
    # Đinh sắt chéo góc, màu nâu rỉ sét
    # Đinh 1
    d.line([(8, 8), (22, 22)], fill=(150, 85, 55, 255), width=2)
    d.line([(6, 10), (10, 6)], fill=(110, 60, 40, 255), width=3) # Mũ đinh
    d.point([(23, 23)], fill=(80, 45, 30, 255)) # Mũi nhọn
    # Đinh 2 bắt chéo
    d.line([(10, 22), (22, 10)], fill=(175, 110, 75, 255), width=2)
    d.line([(8, 20), (12, 24)], fill=(110, 60, 40, 255), width=3) # Mũ đinh

# 3. Cọc Rào Cũ (item_broken_post.png) - 32x32
def draw_broken_post(d, w, h):
    # Cọc gỗ nứt vỡ
    d.rectangle([11, 6, 20, 26], fill=(130, 95, 60, 255), outline=(75, 50, 30, 255))
    # Đầu nhọn cọc
    d.polygon([(11, 6), (15, 2), (20, 6)], fill=(155, 115, 75, 255), outline=(75, 50, 30, 255))
    # Vết nứt đen
    d.line([(13, 11), (17, 16), (14, 22)], fill=(40, 25, 15, 255), width=1)
    d.line([(17, 16), (19, 18)], fill=(40, 25, 15, 255), width=1)

# 4. Đuôi Chuột (item_rat_tail.png) - 32x32
def draw_rat_tail(d, w, h):
    # Đuôi chuột uốn lượn màu hồng xám
    coords = [(8, 8), (14, 11), (20, 10), (24, 16), (20, 23), (12, 24), (8, 20)]
    for i in range(len(coords)-1):
        d.line([coords[i], coords[i+1]], fill=(210, 140, 145, 255), width=2)
    d.point([(7, 7), (8, 7)], fill=(140, 80, 85, 255))

# 5. Phân Bón Vi Sinh (item_fertilizer.png) - 32x32
def draw_fertilizer(d, w, h):
    # Túi phân màu xanh lá sẫm
    d.rounded_rectangle([7, 10, 24, 27], radius=2, fill=(65, 120, 65, 255), outline=(35, 70, 35, 255))
    # Lá mầm vi sinh màu vàng chanh
    d.ellipse([12, 14, 19, 21], fill=(185, 230, 60, 255), outline=(90, 140, 30, 255))
    d.line([(15, 18), (15, 24)], fill=(185, 230, 60, 255), width=1)

# 6. Mảnh Hợp Kim Nhẹ UFO (item_alloy_scrap.png) - 32x32
def draw_alloy_scrap(d, w, h):
    # Mảnh tam giác góc cạnh ánh bạc xanh
    d.polygon([(6, 18), (16, 5), (26, 12), (23, 26), (10, 24)], fill=(180, 215, 245, 255), outline=(90, 130, 175, 255))
    d.polygon([(11, 15), (17, 8), (22, 13)], fill=(225, 245, 255, 255))
    # Rãnh vi mạch phát quang cyan
    d.line([(10, 20), (16, 18), (20, 23)], fill=(50, 235, 255, 255), width=1)

# 7. Cáp Quang Tử (item_quantum_fiber.png) - 32x32
def draw_quantum_fiber(d, w, h):
    # Cuộn dây cáp phát sáng cyan
    d.ellipse([7, 8, 24, 24], outline=(30, 180, 220, 255), width=2)
    d.ellipse([10, 11, 21, 21], outline=(100, 240, 255, 255), width=1)
    d.ellipse([13, 14, 18, 18], fill=(20, 40, 60, 255))
    # Đầu cáp cắm tinh thể
    d.rectangle([21, 6, 26, 11], fill=(160, 245, 255, 255), outline=(30, 120, 160, 255))

# 8. Lõi Plasma (item_plasma_core.png) - 32x32
def draw_plasma_core(d, w, h):
    # Quả cầu plasma năng lượng nén tím-cyan
    # Khung kim loại bên ngoài
    d.ellipse([5, 5, 26, 26], fill=(45, 30, 65, 255), outline=(140, 100, 200, 255))
    # Lõi sáng rực rỡ
    d.ellipse([9, 9, 22, 22], fill=(185, 95, 255, 255))
    d.ellipse([12, 12, 19, 19], fill=(240, 200, 255, 255))
    d.point([(14, 14), (15, 14), (14, 15)], fill=(255, 255, 255, 255))
    # 4 ngàm giữ năng lượng
    d.rectangle([14, 3, 17, 6], fill=(180, 180, 200, 255))
    d.rectangle([14, 25, 17, 28], fill=(180, 180, 200, 255))
    d.rectangle([3, 14, 6, 17], fill=(180, 180, 200, 255))
    d.rectangle([25, 14, 28, 17], fill=(180, 180, 200, 255))

# 9. Dây Thừng Trói Heo (item_rope.png) - 32x32
def draw_rope(d, w, h):
    # Cuộn dây thừng bện gai
    d.ellipse([7, 8, 24, 23], outline=(175, 135, 75, 255), width=3)
    d.ellipse([10, 11, 21, 20], outline=(215, 175, 115, 255), width=2)
    d.line([(18, 21), (25, 27)], fill=(175, 135, 75, 255), width=3)
    d.line([(24, 26), (26, 28)], fill=(120, 85, 40, 255), width=2)

# 10. Vết nứt hàng rào overlay (fence_damaged_overlay.png) - 32x32
def draw_fence_damaged_overlay(d, w, h):
    # Các vết nứt sâu màu đen-nâu đỏ phủ lên cọc rào
    d.line([(12, 6), (15, 12), (13, 18), (17, 26)], fill=(45, 15, 5, 240), width=2)
    d.line([(15, 12), (19, 15)], fill=(60, 25, 10, 220), width=1)
    d.line([(14, 20), (11, 23)], fill=(50, 20, 10, 220), width=1)
    # Mảnh dăm gỗ tróc ra
    d.rectangle([9, 14, 11, 16], fill=(180, 90, 40, 230))
    d.rectangle([20, 19, 22, 22], fill=(180, 90, 40, 230))

# 11. Cọc rào gãy đổ (fence_broken.png) - 32x32
def draw_fence_broken(d, w, h):
    # Cọc gỗ bị gãy đôi đổ rạp xuống đất
    d.rectangle([6, 22, 18, 29], fill=(110, 80, 50, 255), outline=(60, 40, 25, 255))
    d.polygon([(17, 22), (26, 17), (28, 24), (18, 29)], fill=(135, 95, 60, 255), outline=(60, 40, 25, 255))
    d.polygon([(11, 14), (16, 8), (19, 17), (14, 23)], fill=(120, 85, 55, 255), outline=(60, 40, 25, 255))
    # Dăm gỗ gãy vụn
    d.point([(8, 19), (21, 14), (24, 27), (14, 11), (27, 20)], fill=(180, 130, 80, 255))

# 12. Tractor Beam UFO (vfx_tractor_beam.png) - 64x128
def draw_tractor_beam(d, w, h):
    # Cột sáng hình nón từ trên trời rọi xuống, tỏa ánh cyan trong suốt
    for y in range(h):
        progress = y / float(h)
        beam_width = 8 + int(progress * 46)
        alpha = int((1.0 - progress * 0.25) * 110)
        left = (w - beam_width) // 2
        right = left + beam_width
        d.line([(left, y), (right, y)], fill=(40, 220, 255, alpha))
        # Vùng lõi sáng trắng ở giữa
        core_w = max(2, beam_width // 3)
        c_left = (w - core_w) // 2
        c_right = c_left + core_w
        c_alpha = int(alpha * 1.5)
        d.line([(c_left, y), (c_right, y)], fill=(220, 250, 255, min(255, c_alpha)))

base_ui = "Assets/Art/UI"
base_vfx = "Assets/Art/Sprites/VFX"
base_env = "Assets/Art/Sprites/Environment"

create_sprite(f"{base_ui}/item_corn_feed.png", (32, 32), draw_corn_feed)
create_sprite(f"{base_ui}/item_rusty_nail.png", (32, 32), draw_rusty_nail)
create_sprite(f"{base_ui}/item_broken_post.png", (32, 32), draw_broken_post)
create_sprite(f"{base_ui}/item_rat_tail.png", (32, 32), draw_rat_tail)
create_sprite(f"{base_ui}/item_fertilizer.png", (32, 32), draw_fertilizer)
create_sprite(f"{base_ui}/item_alloy_scrap.png", (32, 32), draw_alloy_scrap)
create_sprite(f"{base_ui}/item_quantum_fiber.png", (32, 32), draw_quantum_fiber)
create_sprite(f"{base_ui}/item_plasma_core.png", (32, 32), draw_plasma_core)
create_sprite(f"{base_ui}/item_rope.png", (32, 32), draw_rope)

create_sprite(f"{base_env}/fence_damaged_overlay.png", (32, 32), draw_fence_damaged_overlay)
create_sprite(f"{base_env}/fence_broken.png", (32, 32), draw_broken_post) # or draw_fence_broken

create_sprite(f"{base_vfx}/vfx_tractor_beam.png", (64, 128), draw_tractor_beam)
print("ALL LOOT & DAMAGE SPRITES GENERATED SUCCESSFULLY!")
