import os
import math
from PIL import Image, ImageDraw

WEAPONS_DIR = "Assets/Art/Sprites/Weapons"
CHARS_DIR = "Assets/Art/Sprites/Characters"
ENV_DIR = "Assets/Art/Sprites/Environment"

for d in [WEAPONS_DIR, CHARS_DIR, ENV_DIR]:
    os.makedirs(d, exist_ok=True)

# ==============================================================================
# 1. 64x64 WEAPONS GENERATION (Authentic High-Detail Pixel Art)
# ==============================================================================

def create_sword_kiem_sat_64():
    """Kiếm Thép (Iron Sword): Blade length ~42px, elegant steel gleam, brass guard & pommel."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Diagonal orientation: Hilt near (16, 48), Tip at (48, 16)
    # 1. Pommel (Đốc kiếm vàng đồng)
    draw.ellipse([13, 49, 18, 54], fill=(212, 160, 23, 255), outline=(133, 95, 10, 255))
    draw.point((15, 51), fill=(254, 230, 110, 255)) # highlight

    # 2. Grip (Chuôi kiếm quấn da nâu)
    for i in range(7):
        gx = 16 + i
        gy = 48 - i
        col = (92, 58, 30, 255) if i % 2 == 0 else (125, 85, 45, 255)
        draw.line([(gx - 1, gy), (gx + 1, gy)], fill=col)

    # 3. Crossguard (Khâu kiếm bảo vệ tay vàng đồng)
    guard_center = (24, 40)
    # Wing 1 (Down-Right)
    draw.polygon([(21, 43), (25, 47), (27, 45), (23, 41)], fill=(212, 160, 23, 255), outline=(133, 95, 10, 255))
    # Wing 2 (Up-Left)
    draw.polygon([(23, 37), (19, 33), (21, 31), (25, 35)], fill=(212, 160, 23, 255), outline=(133, 95, 10, 255))
    # Guard jewel (Ngọc xanh chính giữa)
    draw.rectangle([23, 39, 25, 41], fill=(45, 175, 225, 255), outline=(15, 80, 120, 255))

    # 4. Blade (Lưỡi kiếm thép sắc bén 3 lớp)
    for step in range(32):
        bx = 25 + int(step * 0.72)
        by = 39 - int(step * 0.72)
        
        # Sống kiếm (Center spine)
        draw.point((bx, by), fill=(185, 205, 225, 255))
        # Lưỡi trên (Upper sharp edge - highlighted)
        draw.point((bx - 1, by), fill=(255, 255, 255, 255))
        draw.point((bx - 1, by - 1), fill=(240, 245, 255, 255))
        # Lưỡi dưới (Lower bevel - shadow)
        draw.point((bx + 1, by), fill=(120, 140, 165, 255))
        draw.point((bx, by + 1), fill=(70, 85, 105, 255))

    # Mũi kiếm nhọn sắc (Sword tip)
    tip_x, tip_y = 25 + int(32 * 0.72), 39 - int(32 * 0.72)
    draw.polygon([(tip_x - 1, tip_y), (tip_x + 1, tip_y - 1), (tip_x + 2, tip_y - 2), (tip_x, tip_y + 1)],
                 fill=(255, 255, 255, 255), outline=(140, 165, 195, 255))
    draw.point((tip_x + 2, tip_y - 2), fill=(255, 255, 255, 255))
    return img


def create_hammer_bua_ta_64():
    """Búa Tạ (Heavy Warhammer): Thick oak haft, massive forged steel hammer block with brass ring."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Haft / Handle (Cán búa gỗ sồi quấn dây)
    for i in range(26):
        hx = 16 + i
        hy = 48 - i
        col = (110, 72, 38, 255) if i % 3 != 0 else (68, 42, 20, 255)
        draw.line([(hx - 1, hy), (hx + 1, hy)], fill=col)

    # Đốc chuôi cán búa bọc thép
    draw.rectangle([14, 49, 18, 52], fill=(130, 145, 165, 255), outline=(50, 60, 75, 255))

    # 2. Vòng đai thép cổ búa (Reinforcing collar)
    cx, cy = 16 + 25, 48 - 25
    draw.rectangle([cx - 3, cy - 3, cx + 3, cy + 3], fill=(215, 165, 30, 255), outline=(120, 85, 15, 255))

    # 3. Khối đầu búa tạ (Massive Hammer Head: 18x14 px)
    hx0, hy0 = cx - 4, cy - 14
    hx1, hy1 = cx + 14, cy + 4

    # Thân búa thép nguyên khối
    draw.rectangle([hx0, hy0, hx1, hy1], fill=(95, 108, 125, 255), outline=(40, 48, 60, 255))
    # Mặt gờ vát trên (Highlight)
    draw.rectangle([hx0 + 1, hy0 + 1, hx1 - 1, hy0 + 3], fill=(175, 190, 210, 255))
    # Mặt đập búa (Impact face)
    draw.line([(hx0, hy0 + 4), (hx1, hy0 + 4)], fill=(140, 155, 175, 255))
    # Tán đinh trang trí (Rivets)
    draw.point((hx0 + 3, hy0 + 7), fill=(255, 225, 120, 255))
    draw.point((hx1 - 3, hy0 + 7), fill=(255, 225, 120, 255))
    draw.point((hx0 + 3, hy1 - 3), fill=(255, 225, 120, 255))
    draw.point((hx1 - 3, hy1 - 3), fill=(255, 225, 120, 255))
    # Mũi nhọn đập đá phía sau (Spike on back)
    draw.polygon([(hx0 - 1, cy - 5), (hx0 - 6, cy - 5), (hx0 - 1, cy - 2)], fill=(145, 160, 180, 255), outline=(40, 48, 60, 255))
    return img


def create_spear_giao_tre_64():
    """Giáo Tre (Bamboo Spear): Long bamboo pike ~50px, segmented nodes, forged spear tip & red tassel."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Thân tre thanh thoát (Long bamboo shaft)
    for i in range(36):
        bx = 12 + i
        by = 52 - i
        # Bamboo green with yellowish nodes
        is_node = (i % 8 == 0)
        col = (205, 185, 75, 255) if is_node else (125, 165, 45, 255)
        draw.line([(bx - 1, by), (bx, by)], fill=col)
        if is_node:
            draw.point((bx + 1, by), fill=(160, 135, 30, 255))
            draw.point((bx - 1, by + 1), fill=(95, 125, 25, 255))

    # 2. Tua chỉ đỏ may mắn (Crimson tassel)
    tx, ty = 12 + 35, 52 - 35
    draw.ellipse([tx - 2, ty - 1, tx + 4, ty + 5], fill=(225, 40, 40, 255))
    draw.point((tx, ty + 1), fill=(255, 110, 110, 255))
    draw.point((tx + 2, ty + 3), fill=(160, 15, 15, 255))

    # 3. Khâu đồng cổ giáo (Brass collar)
    draw.rectangle([tx, ty - 2, tx + 3, ty + 1], fill=(225, 180, 45, 255), outline=(130, 95, 15, 255))

    # 4. Mũi giáo thép lá lúa (Forged spearhead ~16px)
    sx0, sy0 = tx + 2, ty - 1
    for step in range(12):
        px = sx0 + step
        py = sy0 - step
        # Mũi nhọn
        draw.point((px, py), fill=(255, 255, 255, 255))
        draw.point((px - 1, py), fill=(215, 230, 245, 255))
        draw.point((px, py + 1), fill=(135, 155, 180, 255))
    # Đầu mũi giáo nhọn hoắt
    tip_x, tip_y = sx0 + 11, sy0 - 11
    draw.point((tip_x, tip_y), fill=(255, 255, 255, 255))
    draw.point((tip_x + 1, tip_y - 1), fill=(255, 255, 255, 255))
    return img


def create_dagger_dao_go_64():
    """Đao Gỗ (Wooden Dao / Bokken): Curved hardwood blade ~34px, natural woodgrain, indigo wrapped tsuka."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Tsuka (Chuôi đao quấn dây chàm)
    for i in range(8):
        hx = 16 + i
        hy = 48 - i
        col = (45, 65, 95, 255) if i % 2 == 0 else (30, 45, 70, 255)
        draw.line([(hx - 1, hy), (hx + 1, hy)], fill=col)

    # Đốc chuôi đao
    draw.rectangle([14, 49, 17, 52], fill=(160, 95, 35, 255), outline=(90, 50, 15, 255))

    # 2. Tsuba (Khâu chắn tay gỗ tròn)
    kx, ky = 16 + 8, 48 - 8
    draw.ellipse([kx - 3, ky - 3, kx + 3, ky + 3], fill=(130, 75, 25, 255), outline=(75, 40, 10, 255))

    # 3. Lưỡi đao gỗ cong nhẹ (Curved wooden blade)
    for step in range(24):
        bx = kx + int(step * 0.85)
        # Đường cong vát nhẹ
        curve_offset = int(math.sin(step / 24.0 * math.pi * 0.7) * 2.5)
        by = ky - int(step * 0.85) - curve_offset
        
        # Thớ gỗ vàng hổ phách (Oak wood highlights)
        draw.point((bx, by), fill=(225, 150, 50, 255))
        draw.point((bx - 1, by), fill=(250, 195, 110, 255)) # lưỡi sắc
        draw.point((bx + 1, by), fill=(160, 95, 30, 255))   # sống đao dày
        draw.point((bx, by + 1), fill=(110, 60, 18, 255))

    # Mũi đao vát cong đặc trưng
    tx, ty = kx + int(23 * 0.85), ky - int(23 * 0.85) - 2
    draw.polygon([(tx, ty), (tx + 2, ty - 2), (tx + 4, ty - 2), (tx + 1, ty + 1)],
                 fill=(250, 195, 110, 255), outline=(130, 75, 25, 255))
    return img


# ==============================================================================
# 2. 64x64 ENEMIES GENERATION (Full Proportional 1.63m Human Scale)
# ==============================================================================

# Palette for Kẻ Bắt Heo (Pig Thief): Dark rogue, masked face, burlap sack
C_THIEF_HOOD = (45, 52, 68, 255)
C_THIEF_HOOD_SH = (28, 33, 44, 255)
C_THIEF_CLOAK = (62, 70, 88, 255)
C_THIEF_CLOAK_SH = (38, 44, 56, 255)
C_THIEF_PANTS = (35, 38, 48, 255)
C_THIEF_BOOTS = (22, 24, 30, 255)
C_THIEF_SACK = (185, 145, 95, 255)
C_THIEF_SACK_SH = (130, 95, 55, 255)
C_THIEF_EYES = (255, 220, 60, 255)
C_SKIN = (245, 195, 160, 255)
C_SKIN_SH = (205, 145, 115, 255)

def draw_thief_down_64(hurt=False):
    """Kẻ Bắt Heo nhìn thẳng: Cao 52px (từ y=8 đến y=60), mũ trùm đầu, mắt vàng lườm, vác bao tải."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    tint_overlay = (255, 70, 70, 180) if hurt else None

    # 1. Burlap Sack behind shoulder (Bao tải đựng heo sau lưng vắt qua vai phải)
    draw.ellipse([36, 16, 52, 38], fill=C_THIEF_SACK, outline=C_THIEF_SACK_SH)
    draw.line([(38, 22), (50, 32)], fill=C_THIEF_SACK_SH)
    # Dây thừng thắt bao tải
    draw.line([(36, 17), (40, 20)], fill=(230, 200, 130, 255), width=2)

    # 2. Đầu & Mũ trùm (Hooded Head: y=8..24)
    # Mũ trùm ngoài
    draw.polygon([(24, 10), (32, 7), (40, 10), (42, 23), (22, 23)], fill=C_THIEF_HOOD, outline=C_THIEF_HOOD_SH)
    # Hốc mặt trong bóng tối
    draw.rectangle([25, 12, 39, 21], fill=(20, 22, 28, 255))
    # Khăn bịt nửa dưới mặt
    draw.rectangle([26, 17, 38, 22], fill=C_THIEF_HOOD_SH)
    # Đôi mắt vàng ranh ma rực sáng
    if not hurt:
        draw.rectangle([27, 14, 30, 15], fill=C_THIEF_EYES)
        draw.rectangle([34, 14, 37, 15], fill=C_THIEF_EYES)
        draw.point((29, 14), fill=(255, 255, 255, 255))
        draw.point((36, 14), fill=(255, 255, 255, 255))
    else: # Mắt nhắm tít đau đớn
        draw.line([(27, 14), (30, 15)], fill=(255, 255, 255, 255))
        draw.line([(34, 15), (37, 14)], fill=(255, 255, 255, 255))

    # 3. Áo choàng & Thân (Torso: y=24..42)
    draw.polygon([(23, 24), (41, 24), (43, 40), (21, 40)], fill=C_THIEF_CLOAK, outline=C_THIEF_CLOAK_SH)
    # Vạt áo rách gấu gió thổi
    draw.polygon([(21, 40), (26, 43), (32, 41), (37, 44), (43, 40)], fill=C_THIEF_CLOAK)
    # Dây đai da chéo vai giữ bao tải
    draw.line([(26, 24), (39, 40)], fill=(95, 60, 30, 255), width=2)
    draw.rectangle([31, 31, 34, 34], fill=(215, 165, 30, 255)) # Khóa đai đồng

    # Hai cánh tay khép nép lén lút
    draw.rectangle([19, 26, 23, 38], fill=C_THIEF_CLOAK_SH)
    draw.rectangle([41, 26, 45, 38], fill=C_THIEF_CLOAK_SH)
    # Bàn tay đeo găng da
    draw.rectangle([19, 38, 23, 41], fill=(85, 55, 30, 255))
    draw.rectangle([41, 38, 45, 41], fill=(85, 55, 30, 255))

    # 4. Quần & Chân (Legs: y=42..56)
    draw.rectangle([25, 42, 30, 54], fill=C_THIEF_PANTS)
    draw.rectangle([34, 42, 39, 54], fill=C_THIEF_PANTS)
    # Bốt da mềm chạm đất (Boots: y=54..60)
    draw.rectangle([24, 54, 30, 60], fill=C_THIEF_BOOTS)
    draw.rectangle([34, 54, 40, 60], fill=C_THIEF_BOOTS)
    draw.line([(24, 60), (30, 60)], fill=(12, 14, 18, 255))
    draw.line([(34, 60), (40, 60)], fill=(12, 14, 18, 255))

    if hurt:
        # Áp ánh đỏ báo thương tích
        red_layer = Image.new("RGBA", (64, 64), (255, 60, 60, 110))
        img = Image.alpha_composite(img, red_layer)
    return img


def draw_thief_side_64():
    """Kẻ Bắt Heo nhìn nghiêng: Dáng rình mò lén lút, bao tải sau lưng, bước khẽ."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Bao tải sau lưng to tướng (Sack on back)
    draw.ellipse([16, 18, 32, 42], fill=C_THIEF_SACK, outline=C_THIEF_SACK_SH)
    draw.line([(18, 24), (28, 36)], fill=C_THIEF_SACK_SH)

    # 2. Đầu nghiêng có mũ trùm (Head: y=8..24, x=26..44)
    draw.polygon([(26, 12), (34, 7), (43, 11), (42, 22), (28, 23)], fill=C_THIEF_HOOD, outline=C_THIEF_HOOD_SH)
    # Khuôn mặt hướng phải
    draw.rectangle([33, 13, 41, 18], fill=(20, 22, 28, 255))
    draw.rectangle([35, 17, 43, 22], fill=C_THIEF_HOOD_SH)
    # Mắt vàng nhìn chằm chằm về phía trước
    draw.rectangle([38, 14, 41, 15], fill=C_THIEF_EYES)
    draw.point((40, 14), fill=(255, 255, 255, 255))

    # 3. Thân người khom nhẹ (Torso: y=23..42, x=28..42)
    draw.polygon([(28, 23), (40, 23), (42, 41), (27, 41)], fill=C_THIEF_CLOAK, outline=C_THIEF_CLOAK_SH)
    # Tay duỗi ra phía trước cảnh giác
    draw.polygon([(36, 27), (45, 34), (47, 33), (38, 25)], fill=C_THIEF_CLOAK_SH)
    draw.rectangle([45, 33, 48, 36], fill=(85, 55, 30, 255)) # Bàn tay găng da

    # 4. Chân bước sải nhẹ (Walking stride: y=42..60)
    # Chân sau
    draw.polygon([(28, 42), (25, 53), (29, 53), (32, 42)], fill=C_THIEF_PANTS)
    draw.rectangle([23, 53, 29, 60], fill=C_THIEF_BOOTS)
    # Chân trước
    draw.polygon([(34, 42), (38, 53), (42, 53), (38, 42)], fill=C_THIEF_PANTS)
    draw.rectangle([37, 53, 44, 60], fill=C_THIEF_BOOTS)
    return img


def draw_thief_up_64():
    """Kẻ Bắt Heo nhìn từ phía sau: Mũ trùm sau gáy, thấy trọn bao tải heo buộc thừng."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Thân áo choàng sau lưng
    draw.polygon([(23, 23), (41, 23), (43, 42), (21, 42)], fill=C_THIEF_CLOAK, outline=C_THIEF_CLOAK_SH)

    # 2. Bao tải to đeo sau lưng
    draw.ellipse([24, 20, 44, 41], fill=C_THIEF_SACK, outline=C_THIEF_SACK_SH)
    draw.line([(28, 22), (40, 38)], fill=(230, 200, 130, 255), width=2) # Dây buộc
    draw.line([(40, 22), (28, 38)], fill=(230, 200, 130, 255), width=2)

    # 3. Mũ trùm sau đầu
    draw.polygon([(23, 11), (32, 7), (41, 11), (42, 23), (22, 23)], fill=C_THIEF_HOOD, outline=C_THIEF_HOOD_SH)
    # Nếp gấp sau mũ
    draw.line([(32, 8), (32, 22)], fill=C_THIEF_HOOD_SH)

    # 4. Chân & Bốt sau lưng
    draw.rectangle([25, 42, 30, 54], fill=C_THIEF_PANTS)
    draw.rectangle([34, 42, 39, 54], fill=C_THIEF_PANTS)
    draw.rectangle([24, 54, 30, 60], fill=C_THIEF_BOOTS)
    draw.rectangle([34, 54, 40, 60], fill=C_THIEF_BOOTS)
    return img


# Palette for Kẻ Cào Phá (Farm Breaker / Heavy Brute)
C_BRUTE_HELM = (120, 130, 145, 255)
C_BRUTE_HELM_SH = (70, 78, 90, 255)
C_BRUTE_ARMOR = (145, 95, 60, 255)
C_BRUTE_IRON = (165, 175, 190, 255)
C_BRUTE_PANTS = (55, 45, 35, 255)
C_BRUTE_BOOTS = (38, 42, 50, 255)

def draw_breaker_down_64(hurt=False):
    """Kẻ Cào Phá: Cao 54px (y=6..60), vai u thịt bắp, mũ giáp sắt gai nhọn, cầm xà beng sắt."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Vũ khí đập phá: Xà beng sắt to bản cầm ở tay trái (y=20..52)
    draw.line([(15, 24), (19, 50)], fill=C_BRUTE_IRON, width=3)
    draw.line([(14, 22), (18, 22)], fill=(225, 235, 245, 255), width=2) # Móc cào xà beng

    # 2. Đầu & Mũ giáp sắt bặm trợn (Head: y=6..23)
    draw.polygon([(23, 10), (32, 6), (41, 10), (43, 23), (21, 23)], fill=C_BRUTE_HELM, outline=C_BRUTE_HELM_SH)
    # Khe mắt mũ giáp
    draw.rectangle([25, 13, 39, 17], fill=(25, 28, 35, 255))
    if not hurt:
        draw.rectangle([27, 14, 30, 16], fill=(255, 65, 65, 255)) # Mắt đỏ ngầu giận dữ
        draw.rectangle([34, 14, 37, 16], fill=(255, 65, 65, 255))
    else:
        draw.line([(26, 15), (31, 15)], fill=(255, 255, 255, 255))
        draw.line([(33, 15), (38, 15)], fill=(255, 255, 255, 255))
    # Râu quai nón bặm trợn
    draw.rectangle([24, 19, 40, 23], fill=(45, 35, 28, 255))

    # 3. Thân hình hộ pháp (Torso: y=23..43, vai rộng 26px x=19..45)
    draw.polygon([(19, 23), (45, 23), (43, 42), (21, 42)], fill=C_BRUTE_ARMOR, outline=(75, 45, 25, 255))
    # Đai sắt đinh tán bảo hộ ngực
    draw.rectangle([24, 26, 40, 36], fill=C_BRUTE_IRON, outline=C_BRUTE_HELM_SH)
    draw.point((26, 28), fill=(255, 220, 100, 255))
    draw.point((38, 28), fill=(255, 220, 100, 255))
    draw.point((32, 33), fill=(255, 220, 100, 255))

    # Bắp tay cuồn cuộn
    draw.rectangle([15, 24, 20, 38], fill=C_SKIN_SH)
    draw.rectangle([44, 24, 49, 38], fill=C_SKIN_SH)
    # Găng tay bọc sắt
    draw.rectangle([14, 37, 20, 42], fill=C_BRUTE_HELM_SH)
    draw.rectangle([44, 37, 50, 42], fill=C_BRUTE_HELM_SH)

    # 4. Quần bắp chuối & Bốt sắt nặng trịch (Legs: y=42..60)
    draw.rectangle([23, 42, 30, 53], fill=C_BRUTE_PANTS)
    draw.rectangle([34, 42, 41, 53], fill=C_BRUTE_PANTS)
    draw.rectangle([22, 53, 30, 60], fill=C_BRUTE_BOOTS)
    draw.rectangle([34, 53, 42, 60], fill=C_BRUTE_BOOTS)

    if hurt:
        red_layer = Image.new("RGBA", (64, 64), (255, 60, 60, 110))
        img = Image.alpha_composite(img, red_layer)
    return img


def draw_breaker_side_64():
    """Kẻ Cào Phá nhìn nghiêng: Dáng đồ sộ uy hiếp, vung xà beng."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Thân người to lớn
    draw.polygon([(26, 23), (44, 23), (45, 42), (25, 42)], fill=C_BRUTE_ARMOR, outline=(75, 45, 25, 255))
    draw.rectangle([30, 25, 43, 37], fill=C_BRUTE_IRON)

    # 2. Đầu nghiêng
    draw.polygon([(27, 10), (36, 6), (45, 11), (45, 22), (28, 23)], fill=C_BRUTE_HELM, outline=C_BRUTE_HELM_SH)
    draw.rectangle([36, 13, 44, 17], fill=(25, 28, 35, 255))
    draw.rectangle([39, 14, 43, 16], fill=(255, 65, 65, 255))

    # 3. Tay cầm xà beng hướng tới
    draw.polygon([(36, 26), (48, 33), (50, 31), (38, 24)], fill=C_SKIN_SH)
    draw.rectangle([48, 30, 53, 36], fill=C_BRUTE_HELM_SH)
    draw.line([(51, 18), (51, 48)], fill=C_BRUTE_IRON, width=3) # Xà beng

    # 4. Chân bước hùng hổ
    draw.rectangle([25, 42, 33, 53], fill=C_BRUTE_PANTS)
    draw.rectangle([37, 42, 45, 53], fill=C_BRUTE_PANTS)
    draw.rectangle([23, 53, 33, 60], fill=C_BRUTE_BOOTS)
    draw.rectangle([37, 53, 47, 60], fill=C_BRUTE_BOOTS)
    return img


def draw_breaker_up_64():
    """Kẻ Cào Phá nhìn sau lưng."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw.polygon([(23, 10), (32, 6), (41, 10), (43, 23), (21, 23)], fill=C_BRUTE_HELM, outline=C_BRUTE_HELM_SH)
    draw.polygon([(19, 23), (45, 23), (43, 42), (21, 42)], fill=C_BRUTE_ARMOR)
    draw.rectangle([23, 42, 30, 53], fill=C_BRUTE_PANTS)
    draw.rectangle([34, 42, 41, 53], fill=C_BRUTE_PANTS)
    draw.rectangle([22, 53, 30, 60], fill=C_BRUTE_BOOTS)
    draw.rectangle([34, 53, 42, 60], fill=C_BRUTE_BOOTS)
    return img


# Palette for Chuột Gặm Kho (Pest Rat): Realistic pest scale, running along ground y=44..60
C_RAT_FUR = (135, 105, 80, 255)
C_RAT_FUR_SH = (85, 60, 45, 255)
C_RAT_BELLY = (195, 170, 145, 255)
C_RAT_PINK = (235, 160, 160, 255)
C_RAT_EYE = (225, 30, 30, 255)

def draw_pest_down_64(hurt=False):
    """Chuột Gặm Kho nhìn đối diện: Nhỏ nhắn dưới đất (y=45..60), tai hồng nhọn, mắt đỏ gian xảo."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Thân chuột béo tròn (y=47..58, x=22..42)
    draw.ellipse([22, 47, 42, 59], fill=C_RAT_FUR, outline=C_RAT_FUR_SH)
    draw.ellipse([26, 49, 38, 57], fill=C_RAT_BELLY)

    # 2. Hai tai hồng nhọn
    draw.polygon([(23, 43), (27, 48), (22, 49)], fill=C_RAT_PINK, outline=C_RAT_FUR_SH)
    draw.polygon([(41, 43), (37, 48), (42, 49)], fill=C_RAT_PINK, outline=C_RAT_FUR_SH)

    # 3. Mõm nhọn & Đôi mắt đỏ
    draw.polygon([(28, 51), (32, 57), (36, 51)], fill=C_RAT_FUR_SH)
    draw.point((32, 57), fill=C_RAT_PINK) # Mũi hồng
    if not hurt:
        draw.point((27, 50), fill=C_RAT_EYE)
        draw.point((37, 50), fill=C_RAT_EYE)
    else:
        draw.point((27, 50), fill=(255, 255, 255, 255))
        draw.point((37, 50), fill=(255, 255, 255, 255))

    # 4. Bốn chân chuột bám đất (y=57..60)
    draw.rectangle([21, 57, 25, 60], fill=C_RAT_PINK)
    draw.rectangle([39, 57, 43, 60], fill=C_RAT_PINK)

    if hurt:
        red_layer = Image.new("RGBA", (64, 64), (255, 60, 60, 120))
        img = Image.alpha_composite(img, red_layer)
    return img


def draw_pest_side_64():
    """Chuột Gặm Kho nhìn nghiêng: Dáng chạy sột soạt, đuôi dài uốn lượn sau lưng."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Đuôi dài ngoằn ngoèo uốn lượn (Tail: x=10..22, y=42..54)
    tail_pts = [(10, 44), (13, 42), (16, 46), (20, 51), (24, 53)]
    draw.line(tail_pts, fill=C_RAT_PINK, width=2)

    # 2. Thân chuột nằm ngang (Body: x=22..46, y=46..58)
    draw.ellipse([22, 46, 46, 58], fill=C_RAT_FUR, outline=C_RAT_FUR_SH)
    draw.ellipse([26, 49, 44, 57], fill=C_RAT_BELLY)

    # 3. Đầu & Mõm nhọn chuột hướng phải (Head: x=40..54, y=47..56)
    draw.polygon([(40, 48), (53, 53), (44, 57)], fill=C_RAT_FUR, outline=C_RAT_FUR_SH)
    draw.point((53, 53), fill=C_RAT_PINK) # Mũi
    # Tai dựng đứng
    draw.polygon([(41, 43), (45, 47), (39, 48)], fill=C_RAT_PINK, outline=C_RAT_FUR_SH)
    # Mắt đỏ
    draw.point((46, 50), fill=C_RAT_EYE)

    # 4. Bốn chân chạy lon ton
    draw.rectangle([26, 56, 30, 60], fill=C_RAT_PINK)
    draw.rectangle([38, 56, 42, 60], fill=C_RAT_PINK)
    return img


# ==============================================================================
# 3. 64x64 TRAINING DUMMY GENERATION (Straw, Iron, Steel)
# ==============================================================================

def create_training_dummy_64(variant="straw"):
    """Bù Nhìn Luyện Công 64x64: Cọc gỗ chữ thập, thân bó rơm / giáp sắt, hồng tâm tập đánh."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Cọc gỗ chữ thập đóng xuống đất (Wooden Cross Post: y=16..60)
    # Cọc dọc
    draw.rectangle([30, 18, 34, 60], fill=(110, 75, 40, 255), outline=(65, 40, 18, 255))
    # Cọc ngang giang tay
    draw.rectangle([14, 28, 50, 32], fill=(110, 75, 40, 255), outline=(65, 40, 18, 255))

    body_col = (235, 195, 75, 255) if variant == "straw" else ((145, 155, 170, 255) if variant == "iron" else (85, 95, 115, 255))
    shadow_col = (180, 140, 40, 255) if variant == "straw" else ((90, 100, 115, 255) if variant == "iron" else (50, 58, 75, 255))

    # 2. Đầu bù nhìn (y=12..25, x=24..40)
    draw.ellipse([24, 12, 40, 26], fill=body_col, outline=shadow_col)
    # Nón lá rơm / Mũ sắt
    if variant == "straw":
        draw.polygon([(18, 16), (32, 7), (46, 16)], fill=(215, 170, 55, 255), outline=(135, 95, 25, 255))
        draw.point((32, 7), fill=(255, 230, 120, 255))
    else:
        draw.polygon([(22, 14), (32, 9), (42, 14)], fill=(185, 195, 210, 255), outline=(70, 80, 95, 255))

    # Mặt vẽ dấu X chữ thập tập đánh
    draw.line([(28, 17), (36, 21)], fill=(75, 50, 25, 255), width=2)
    draw.line([(36, 17), (28, 21)], fill=(75, 50, 25, 255), width=2)

    # 3. Thân bù nhìn (Torso: y=26..48, x=22..42)
    draw.polygon([(22, 27), (42, 27), (40, 48), (24, 48)], fill=body_col, outline=shadow_col)
    # Dây thừng thắt eo
    draw.line([(23, 38), (41, 38)], fill=(120, 80, 35, 255), width=2)
    draw.line([(24, 40), (40, 40)], fill=(120, 80, 35, 255), width=2)

    # Bìa hồng tâm tập chém trên ngực (Red Target Bullseye)
    draw.ellipse([28, 29, 36, 37], fill=(235, 45, 45, 255), outline=(150, 15, 15, 255))
    draw.ellipse([30, 31, 34, 35], fill=(255, 255, 255, 255))
    draw.point((32, 33), fill=(235, 45, 45, 255))

    # Hai cánh tay rơm buộc túm hai đầu cọc ngang
    draw.ellipse([12, 27, 18, 33], fill=body_col, outline=shadow_col)
    draw.ellipse([46, 27, 52, 33], fill=body_col, outline=shadow_col)

    # Chân đế cọc cắm đất
    draw.line([(26, 60), (38, 60)], fill=(65, 40, 18, 255), width=2)
    return img


# ==============================================================================
# MAIN EXPORT ROUTINE
# ==============================================================================
if __name__ == "__main__":
    print(">>> Đang xuất khẩu Vũ khí 64x64...")
    create_sword_kiem_sat_64().save(os.path.join(WEAPONS_DIR, "weapon_kiem_sat.png"))
    create_hammer_bua_ta_64().save(os.path.join(WEAPONS_DIR, "weapon_bua_ta.png"))
    create_spear_giao_tre_64().save(os.path.join(WEAPONS_DIR, "weapon_giao_tre.png"))
    create_dagger_dao_go_64().save(os.path.join(WEAPONS_DIR, "weapon_dao_go.png"))
    print("✔ Đã xuất xong 4 vũ khí 64x64: Kiếm Sắt, Búa Tạ, Giáo Tre, Đao Gỗ.")

    print(">>> Đang xuất khẩu Quái vật 64x64...")
    # Kẻ Bắt Heo (Pig Thief)
    draw_thief_down_64(hurt=False).save(os.path.join(CHARS_DIR, "enemy_thief_down.png"))
    draw_thief_side_64().save(os.path.join(CHARS_DIR, "enemy_thief_side.png"))
    draw_thief_up_64().save(os.path.join(CHARS_DIR, "enemy_thief_up.png"))
    draw_thief_down_64(hurt=True).save(os.path.join(CHARS_DIR, "enemy_thief_hurt.png"))

    # Kẻ Cào Phá (Farm Breaker)
    draw_breaker_down_64(hurt=False).save(os.path.join(CHARS_DIR, "enemy_breaker_down.png"))
    draw_breaker_side_64().save(os.path.join(CHARS_DIR, "enemy_breaker_side.png"))
    draw_breaker_up_64().save(os.path.join(CHARS_DIR, "enemy_breaker_up.png"))
    draw_breaker_down_64(hurt=True).save(os.path.join(CHARS_DIR, "enemy_breaker_hurt.png"))

    # Chuột Gặm Kho (Pest Rat)
    draw_pest_down_64(hurt=False).save(os.path.join(CHARS_DIR, "enemy_pest_down.png"))
    draw_pest_side_64().save(os.path.join(CHARS_DIR, "enemy_pest_side.png"))
    draw_pest_down_64(hurt=False).save(os.path.join(CHARS_DIR, "enemy_pest_up.png"))
    draw_pest_down_64(hurt=True).save(os.path.join(CHARS_DIR, "enemy_pest_hurt.png"))
    print("✔ Đã xuất xong 3 nhóm kẻ địch 64x64: Kẻ Bắt Heo, Kẻ Cào Phá, Chuột Gặm Kho.")

    print(">>> Đang xuất khẩu Bù Nhìn Luyện Công 64x64...")
    create_training_dummy_64("straw").save(os.path.join(ENV_DIR, "training_dummy_straw.png"))
    create_training_dummy_64("iron").save(os.path.join(ENV_DIR, "training_dummy_iron.png"))
    create_training_dummy_64("steel").save(os.path.join(ENV_DIR, "training_dummy_steel.png"))
    print("✔ Đã xuất xong 3 loại Bù Nhìn Luyện Công 64x64: Rơm, Sắt, Thép.")
    print("=== TOÀN BỘ ECOSYSTEM 64x64 ĐÃ ĐƯỢC ĐỒNG BỘ THÀNH CÔNG ===")
