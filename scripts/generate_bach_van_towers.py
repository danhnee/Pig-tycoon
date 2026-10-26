#!/usr/bin/env python3
"""
Bộ sinh đồ họa Pixel Art 16-bit chuẩn chỉnh cho 5 Thần Khí & Công Trình Bạch Vân:
1. CuuTieuLoiPhao (Cửu Tiêu Lôi Pháo - Tier 4)
2. ThienNhanVongDai (Thiên Nhãn Vọng Đài - Tier 3)
3. VanLinhHoiNguyenTran (Vạn Linh Hồi Nguyên Trận - Tier 3)
4. NoXuyenVan (Nỏ Xuyên Vân - Tier 1)
5. BayChongGo (Bẫy Chông Gỗ - Tier 1)
"""

import os, math, uuid
from PIL import Image, ImageDraw

OUT_DIR = "Assets/Art/Sprites/Environment/Buildings/BachVanTowers"
SHEET_DIR = "Assets/Art/Sprites/Environment/Buildings"
ARTIFACT_DIR = "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28"
os.makedirs(OUT_DIR, exist_ok=True)
os.makedirs(SHEET_DIR, exist_ok=True)
os.makedirs(ARTIFACT_DIR, exist_ok=True)

# Đảm bảo có .meta cho thư mục BachVanTowers
folder_meta = OUT_DIR + ".meta"
if not os.path.exists(folder_meta):
    f_guid = uuid.uuid4().hex
    with open(folder_meta, "w") as f:
        f.write(f"fileFormatVersion: 2\nguid: {f_guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

def make_meta(path, guid=None):
    if guid is None:
        guid = uuid.uuid4().hex
    meta_path = path + ".meta"
    if os.path.exists(meta_path):
        return
    template = """fileFormatVersion: 2
guid: {0}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""".format(guid)
    with open(meta_path, "w") as f:
        f.write(template)

# Helper vẽ elip pixel
def draw_pixel_ellipse(im, cx, cy, rx, ry, fill_color, outline_color=None):
    for y in range(cy - ry, cy + ry + 1):
        for x in range(cx - rx, cx + rx + 1):
            if 0 <= x < im.width and 0 <= y < im.height:
                val = ((x - cx) / max(1, rx))**2 + ((y - cy) / max(1, ry))**2
                if val <= 1.0:
                    im.putpixel((x, y), fill_color)
    if outline_color:
        for y in range(cy - ry - 1, cy + ry + 2):
            for x in range(cx - rx - 1, cx + rx + 2):
                if 0 <= x < im.width and 0 <= y < im.height:
                    val = ((x - cx) / max(1, rx))**2 + ((y - cy) / max(1, ry))**2
                    if 0.75 <= val <= 1.25 and im.getpixel((x, y))[3] == 0:
                        im.putpixel((x, y), outline_color)

# ==============================================================================
# 1. CỬU TIÊU LÔI PHÁO (CuuTieuLoiPhao - Tier 4)
# ==============================================================================
def render_cuu_tieu_loi_phao(f_idx):
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    c_out = (26, 21, 38, 255)
    c_m_white = (248, 250, 252, 255)
    c_m_light = (226, 232, 240, 255)
    c_m_mid = (203, 213, 225, 255)
    c_m_shad = (148, 163, 184, 255)
    c_m_dark = (71, 85, 105, 255)

    c_g_hi = (254, 240, 138, 255)
    c_g_bright = (250, 204, 21, 255)
    c_g_mid = (234, 179, 8, 255)
    c_g_dark = (161, 98, 7, 255)

    c_br_hi = (217, 119, 6, 255)
    c_br_mid = (180, 83, 9, 255)
    c_br_shad = (120, 53, 15, 255)
    c_br_dark = (69, 26, 3, 255)

    c_cyan_hi = (207, 250, 254, 255)
    c_cyan_mid = (34, 211, 238, 255)

    # 1. Bệ đá cẩm thạch trắng mây cuộn (Y: 48..60, X: 12..52)
    # Tầng dưới cùng
    for y in range(54, 60):
        w_y = int(18 - (y - 54) * 0.5)
        for x in range(32 - w_y, 32 + w_y + 1):
            if y == 59 or x in (32 - w_y, 32 + w_y):
                im.putpixel((x, y), c_out)
            elif y == 54:
                im.putpixel((x, y), c_m_white if x < 32 else c_m_light)
            else:
                im.putpixel((x, y), c_m_light if x < 30 else (c_m_mid if x < 36 else c_m_shad))

    # Tầng giữa có chạm phù văn sét vàng
    for y in range(46, 54):
        w_y = int(15 - (y - 46) * 0.3)
        for x in range(32 - w_y, 32 + w_y + 1):
            if y == 53 or x in (32 - w_y, 32 + w_y):
                im.putpixel((x, y), c_out)
            elif y == 46:
                im.putpixel((x, y), c_m_white if x < 31 else c_m_light)
            else:
                col = c_m_light if x < 28 else (c_m_mid if x < 36 else c_m_shad)
                # Phù văn vàng ở giữa
                if y in (48, 50, 52) and 24 <= x <= 40 and (x % 3 == 0 or x % 4 == 0):
                    col = c_g_hi if f_idx in (1, 2) else c_g_bright
                im.putpixel((x, y), col)

    # 4 góc bệ đá có 4 cột trụ đồng nạm ngọc Lôi Thạch
    pillars_x = [18, 45]
    for px in pillars_x:
        for py in range(42, 53):
            for ox in range(-2, 3):
                x = px + ox
                if ox in (-2, 2) or py in (42, 52):
                    im.putpixel((x, py), c_out)
                else:
                    im.putpixel((x, py), c_br_mid if ox < 0 else c_br_shad)
        # Đỉnh cột là viên Lôi Thạch phát sáng
        c_gem = c_g_hi if f_idx in (1, 2) else c_g_bright
        im.putpixel((px, 40), c_out)
        im.putpixel((px - 1, 41), c_gem)
        im.putpixel((px, 41), (255, 255, 255, 255) if f_idx in (1, 2) else c_gem)
        im.putpixel((px + 1, 41), c_g_dark)

    # 2. Giá pháo đồng & Giá nâng Hồ Lô (Y: 34..46)
    for y in range(36, 46):
        w = int(8 - (y - 36) * 0.4)
        for x in range(32 - w, 32 + w + 1):
            if y == 45 or x in (32 - w, 32 + w):
                im.putpixel((x, y), c_out)
            else:
                im.putpixel((x, y), c_br_hi if x < 31 else (c_br_mid if x < 34 else c_br_shad))

    # 3. Lôi Đạo Hồ Lô (Bình Hồ Lô sấm sét)
    # Tùy frame: Frame 0 (idle Y=18), Frame 1 (charge nhấc bổng Y=13), Frame 2 (fire dập nén Y=20), Frame 3 (cooldown Y=18)
    gourd_y_offset = [0, -5, 2, 0][f_idx]
    gy_base = 24 + gourd_y_offset

    # Bầu dưới hồ lô (Rx=9, Ry=8, Center Y=gy_base+5)
    draw_pixel_ellipse(im, 32, gy_base + 5, 9, 8, c_br_mid, c_out)
    # Highlight & Shadow bầu dưới
    for by in range(gy_base - 2, gy_base + 12):
        for bx in range(24, 40):
            if 0 <= bx < 64 and 0 <= by < 64 and im.getpixel((bx, by))[3] == 255 and im.getpixel((bx, by)) != c_out:
                if bx < 30:
                    im.putpixel((bx, by), c_br_hi if by < gy_base + 4 else c_br_mid)
                elif bx > 34:
                    im.putpixel((bx, by), c_br_shad)

    # Vòng thắt lưng mạ vàng giữa 2 bầu
    for bx in range(28, 37):
        im.putpixel((bx, gy_base - 1), c_g_bright)
        im.putpixel((bx, gy_base), c_g_dark)

    # Bầu trên hồ lô (Rx=7, Ry=6, Center Y=gy_base-5)
    draw_pixel_ellipse(im, 32, gy_base - 6, 7, 6, c_br_mid, c_out)
    for by in range(gy_base - 11, gy_base):
        for bx in range(26, 38):
            if 0 <= bx < 64 and 0 <= by < 64 and im.getpixel((bx, by))[3] == 255 and im.getpixel((bx, by)) != c_out:
                if bx < 30:
                    im.putpixel((bx, by), c_br_hi)
                elif bx > 34:
                    im.putpixel((bx, by), c_br_shad)

    # Miệng hồ lô & Họng pháo (Y: gy_base - 13..gy_base - 10)
    for ny in range(gy_base - 14, gy_base - 9):
        for nx in range(29, 36):
            if nx in (29, 35) or ny in (gy_base - 14, gy_base - 10):
                im.putpixel((nx, ny), c_out)
            else:
                im.putpixel((nx, ny), c_g_hi if nx < 32 else c_g_mid)

    # Nắp bình lôi đình
    cap_y = (gy_base - 17) if f_idx in (1, 2) else (gy_base - 15)
    for cx in range(30, 35):
        im.putpixel((cx, cap_y), c_out)
        im.putpixel((cx, cap_y + 1), c_g_bright)

    # 4. Hiệu ứng riêng theo Frame
    if f_idx == 0:
        # Idle: Vài đốm lôi điện nhỏ ở miệng bình
        im.putpixel((32, gy_base - 16), c_g_hi)
        im.putpixel((30, gy_base - 15), c_cyan_hi)
        im.putpixel((34, gy_base - 17), c_g_bright)
    elif f_idx == 1:
        # Charge: Các tia sét từ trên trời tụ vào miệng bình + phù văn sáng chói
        # Tia sét 1 (trái trên)
        pts1 = [(20, 2), (23, 6), (22, 10), (28, 14), (31, gy_base - 14)]
        for i in range(len(pts1)-1):
            d.line([pts1[i], pts1[i+1]], fill=c_g_hi, width=2)
        # Tia sét 2 (phải trên)
        pts2 = [(46, 3), (42, 7), (44, 11), (36, 14), (33, gy_base - 14)]
        for i in range(len(pts2)-1):
            d.line([pts2[i], pts2[i+1]], fill=(255, 255, 255, 255), width=2)
        # Hào quang miệng bình
        for ox in range(-3, 4):
            for oy in range(-2, 3):
                px, py = 32 + ox, (gy_base - 14) + oy
                if (ox**2 + oy**2) <= 8 and 0 <= px < 64 and 0 <= py < 64:
                    im.putpixel((px, py), (255, 255, 255, 230))
    elif f_idx == 2:
        # Fire: Cột sấm sét khổng lồ phóng thẳng lên trời (X: 28..36, Y: 0..gy_base - 12)
        for by in range(0, gy_base - 12):
            for bx in range(27, 38):
                dx = abs(bx - 32)
                if dx <= 2:
                    im.putpixel((bx, by), (255, 255, 255, 255))
                elif dx <= 4:
                    im.putpixel((bx, by), c_g_hi)
                elif dx <= 5:
                    im.putpixel((bx, by), c_g_bright)
        # Vòng sóng xung kích hoàng kim dưới chân tháp (Y: 57..62)
        for sx in range(10, 54):
            dx = abs(sx - 32)
            sy = int(58 + math.sin(dx * 0.15) * 2)
            if 0 <= sy < 64:
                im.putpixel((sx, sy), c_g_hi)
                if sy + 1 < 64:
                    im.putpixel((sx, sy + 1), c_g_bright)
    elif f_idx == 3:
        # Cooldown: Khói mây trắng thanh thoát bốc lên từ vòi bình
        smoke_motes = [(32, gy_base - 16), (33, gy_base - 18), (31, gy_base - 20), (34, gy_base - 22), (32, gy_base - 24)]
        for sx, sy in smoke_motes:
            if 0 <= sx < 64 and 0 <= sy < 64:
                im.putpixel((sx, sy), (241, 245, 249, 210))
                im.putpixel((sx+1, sy), (226, 232, 240, 180))

    return im

# ==============================================================================
# 2. THIÊN NHÃN VỌNG ĐÀI (ThienNhanVongDai - Tier 3)
# ==============================================================================
def render_thien_nhan_vong_dai(f_idx):
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    c_out = (26, 21, 38, 255)
    c_wood_dark = (69, 26, 3, 255)
    c_wood_mid = (120, 53, 15, 255)
    c_wood_light = (180, 83, 9, 255)

    c_jade_dark = (6, 78, 59, 255)
    c_jade_mid = (4, 120, 87, 255)
    c_jade_light = (16, 185, 129, 255)
    c_jade_bright = (52, 211, 153, 255)

    c_gold_hi = (254, 240, 138, 255)
    c_gold = (250, 204, 21, 255)
    c_gold_dark = (161, 98, 7, 255)

    # 1. Chân tháp & 4 trụ gỗ lim (Y: 42..60, X: 16..48)
    for y in range(44, 60):
        # Trụ trái & phải
        for tx in (19, 20, 43, 44):
            im.putpixel((tx, y), c_wood_dark if tx in (19, 43) else c_wood_mid)
            if tx in (19, 44):
                im.putpixel((tx - 1 if tx == 19 else tx + 1, y), c_out)
    # Giằng chéo chữ X
    d.line([(21, 46), (42, 57)], fill=c_wood_mid, width=2)
    d.line([(21, 57), (42, 46)], fill=c_wood_dark, width=2)

    # Bệ sàn vọng đài gỗ (Y: 40..44, X: 16..48)
    for y in range(41, 45):
        for x in range(16, 48):
            if y == 44 or x in (16, 47) or y == 41:
                im.putpixel((x, y), c_out)
            else:
                im.putpixel((x, y), c_wood_light if y == 42 else c_wood_mid)
    # Lan can gỗ đỏ
    for rx in range(17, 47, 4):
        for ry in range(37, 41):
            im.putpixel((rx, ry), c_wood_dark)
            im.putpixel((rx+1, ry), c_wood_light)
    d.line([(16, 37), (47, 37)], fill=c_wood_mid, width=1)

    # 2. Tầng trên & Cột chống mái (Y: 28..37, X: 22..42)
    for y in range(29, 37):
        for tx in (24, 39):
            im.putpixel((tx, y), c_wood_mid)
            im.putpixel((tx+1, y), c_wood_dark)

    # 3. Mái ngói lưu ly cong vút màu ngọc bích (Y: 22..30, X: 12..52)
    for y in range(22, 30):
        spread = int((y - 22) * 2.5)
        lx = 32 - 12 - spread
        rx = 32 + 12 + spread
        for x in range(max(10, lx), min(54, rx + 1)):
            if y == 29 or x in (lx, rx):
                im.putpixel((x, y), c_out)
            elif y == 22:
                im.putpixel((x, y), c_jade_bright if x < 32 else c_jade_light)
            else:
                im.putpixel((x, y), c_jade_light if x < 30 else (c_jade_mid if x < 38 else c_jade_dark))
    # Đầu mái vuốt cong lên (đầu đao mái ngói)
    im.putpixel((10, 26), c_gold)
    im.putpixel((9, 25), c_out)
    im.putpixel((53, 26), c_gold)
    im.putpixel((54, 25), c_out)

    # 4 Chuông gió treo góc mái
    im.putpixel((12, 31), c_gold)
    im.putpixel((12, 32), c_gold_dark)
    im.putpixel((51, 31), c_gold)
    im.putpixel((51, 32), c_gold_dark)

    # 4. Đỉnh chóp mái & Kính Bát Quái Thiên Nhãn (Bagua Linh Kính - Center X=32, Y=13)
    # Chóp hồ lô ngọc đỡ gương
    for py in range(18, 23):
        im.putpixel((31, py), c_gold)
        im.putpixel((32, py), c_gold_dark)

    # Gương Bát Quái
    mirror_angle = [0, 45, 90, 20][f_idx]
    # Vẽ gương bát giác viền vàng
    draw_pixel_ellipse(im, 32, 13, 8, 8, c_jade_mid, c_gold)
    # Mặt gương ngọc bích
    for my in range(8, 19):
        for mx in range(27, 38):
            dx = abs(mx - 32)
            dy = abs(my - 13)
            if (dx**2 + dy**2) <= 30 and 0 <= mx < 64 and 0 <= my < 64:
                if dx + dy <= 3:
                    im.putpixel((mx, my), c_jade_bright if f_idx in (1, 2) else (255, 255, 255, 240))
                else:
                    im.putpixel((mx, my), c_jade_light if mx < 32 else c_jade_mid)

    # Quẻ Bát Quái viền ngoài
    trigram_pts = [(32, 4), (32, 22), (23, 13), (41, 13), (25, 6), (39, 6), (25, 20), (39, 20)]
    for tx, ty in trigram_pts:
        if 0 <= tx < 64 and 0 <= ty < 64:
            im.putpixel((tx, ty), c_gold_hi if f_idx in (1, 2) else c_gold)

    # 5. Hiệu ứng riêng theo Frame
    if f_idx == 0:
        # Idle: Chuông gió lấp lánh nhẹ, tâm gương đọng tia sáng
        im.putpixel((32, 13), (255, 255, 255, 255))
    elif f_idx == 1:
        # Charge: Gương xoay tít, 8 quẻ phát quang hoàng kim
        for tx, ty in trigram_pts:
            d.line([(32, 13), (tx, ty)], fill=c_gold_hi, width=1)
    elif f_idx == 2:
        # Scan/Fire: Chiếu chùm linh quang quét hình nón xuống chiến trường
        # Chùm sáng hình nón từ (32, 16) tỏa xuống (12..52, 60)
        overlay = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        ov_d = ImageDraw.Draw(overlay)
        ov_d.polygon([(32, 16), (6, 62), (58, 62)], fill=(254, 240, 138, 70))
        ov_d.polygon([(32, 16), (18, 62), (46, 62)], fill=(255, 255, 255, 110))
        im = Image.alpha_composite(im, overlay)
        # Hoa văn Bát Quái in bóng ảo ảnh dưới đất
        draw_pixel_ellipse(im, 32, 58, 14, 4, (52, 211, 153, 160), c_gold_hi)
    elif f_idx == 3:
        # Cooldown: Hạt bụi linh quang bay lả tả
        motes = [(30, 24), (35, 27), (28, 33), (36, 35)]
        for mx, my in motes:
            if 0 <= mx < 64 and 0 <= my < 64:
                im.putpixel((mx, my), c_gold_hi)

    return im

# ==============================================================================
# 3. VẠN LINH HỒI NGUYÊN TRẬN (VanLinhHoiNguyenTran - Tier 3)
# ==============================================================================
def render_van_linh_hoi_nguyen_tran(f_idx):
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    c_out = (26, 21, 38, 255)
    c_m_white = (248, 250, 252, 255)
    c_m_light = (226, 232, 240, 255)
    c_m_mid = (203, 213, 225, 255)
    c_m_shad = (148, 163, 184, 255)

    c_moss = (22, 101, 52, 255)
    c_moss_light = (34, 197, 94, 255)

    c_lotus_hi = (167, 243, 208, 255)
    c_lotus_light = (52, 211, 153, 255)
    c_lotus_mid = (16, 185, 129, 255)
    c_lotus_shad = (5, 150, 105, 255)
    c_lotus_dark = (6, 78, 59, 255)

    c_cry_white = (255, 255, 255, 255)
    c_cry_cyan = (103, 232, 249, 255)
    c_cry_deep = (6, 182, 212, 255)

    # 1. Đàn tế đá ngũ giác cẩm thạch (Y: 44..60)
    # Tầng dưới
    draw_pixel_ellipse(im, 32, 54, 22, 8, c_m_light, c_out)
    for y in range(48, 62):
        for x in range(10, 55):
            if 0 <= x < 64 and 0 <= y < 64 and im.getpixel((x, y))[3] == 255 and im.getpixel((x, y)) != c_out:
                if y > 56:
                    im.putpixel((x, y), c_m_shad)
                elif x < 28:
                    im.putpixel((x, y), c_m_white)
                # Đốm rêu xanh ngũ hành
                if (x + y) % 7 == 0 and y > 52:
                    im.putpixel((x, y), c_moss)

    # Tầng trên đàn tế (Y: 42..52, Rx=18, Ry=6)
    draw_pixel_ellipse(im, 32, 48, 18, 6, c_m_mid, c_out)
    for y in range(43, 54):
        for x in range(14, 51):
            if 0 <= x < 64 and 0 <= y < 64 and im.getpixel((x, y))[3] == 255 and im.getpixel((x, y)) != c_out:
                if y == 43:
                    im.putpixel((x, y), c_m_white)
                elif x < 28:
                    im.putpixel((x, y), c_m_light)
                elif x > 38:
                    im.putpixel((x, y), c_m_shad)

    # Đường phù văn linh mạch trên mặt đàn tế
    rune_col = c_lotus_light if f_idx in (1, 2) else c_lotus_shad
    d.arc([18, 44, 46, 52], start=0, end=360, fill=rune_col, width=1)

    # 2. Đóa Sen Ngọc Bích Thần Thoại (Linh Mộc Thanh Liên)
    # Center X=32, Y=38..46
    lotus_spread = [0, 3, 7, 1][f_idx]
    petals = [
        (-4 - lotus_spread, 36, -8 - lotus_spread, 28, -2, 40), # Cánh trái ngoài
        (4 + lotus_spread, 36, 8 + lotus_spread, 28, 2, 40),   # Cánh phải ngoài
        (-2 - lotus_spread//2, 34, -4 - lotus_spread//2, 24, 0, 42), # Cánh trái trong
        (2 + lotus_spread//2, 34, 4 + lotus_spread//2, 24, 0, 42),   # Cánh phải trong
        (0, 32, 0, 22, 0, 42), # Cánh chính giữa
    ]

    # Vẽ các cánh sen ngọc
    for x0, y0, x1, y1, x2, y2 in petals:
        pts = [(32 + x0, y0), (32 + x1, y1), (32 + x2, y2)]
        for px, py in pts:
            draw_pixel_ellipse(im, px, py, 3, 5, c_lotus_mid, c_out)

    # Đài sen trung tâm
    draw_pixel_ellipse(im, 32, 38, 7, 5, c_lotus_light, c_lotus_dark)

    # 3. Tinh Thạch Bạch Vân ở nhụy sen (Center X=32, Y: 28..34)
    cry_y = 31 if f_idx != 1 else 27
    draw_pixel_ellipse(im, 32, cry_y, 4, 6, c_cry_cyan, c_out)
    im.putpixel((32, cry_y - 2), c_cry_white)
    im.putpixel((31, cry_y - 1), c_cry_white)
    im.putpixel((33, cry_y + 2), c_cry_deep)

    # 4. Hiệu ứng riêng theo Frame
    if f_idx == 0:
        # Idle: Hạt sinh khí xanh ngọc bay lơ lửng
        im.putpixel((26, 26), c_lotus_hi)
        im.putpixel((38, 24), c_cry_cyan)
        im.putpixel((32, 20), c_lotus_light)
    elif f_idx == 1:
        # Charge: Tinh thạch nhấc bổng, vòng tròn ma pháp xanh bừng sáng
        d.ellipse([20, 44, 44, 52], outline=(52, 211, 153, 255), width=2)
        # Tia sáng xoay quanh nhụy
        for angle in (30, 90, 150, 210, 270, 330):
            rad = math.radians(angle)
            lx = int(32 + math.cos(rad) * 9)
            ly = int(cry_y + math.sin(rad) * 9)
            if 0 <= lx < 64 and 0 <= ly < 64:
                im.putpixel((lx, ly), c_lotus_hi)
    elif f_idx == 2:
        # Heal Burst/Fire: Vầng hào quang sinh mệnh cực đại tỏa tròn khắp tế đàn
        overlay = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        ov_d = ImageDraw.Draw(overlay)
        ov_d.ellipse([4, 18, 60, 58], outline=(52, 211, 153, 200), width=3)
        ov_d.ellipse([10, 24, 54, 52], outline=(167, 243, 208, 240), width=2)
        # Các cánh sen quang văng bay tỏa ra ngoài
        petal_motes = [(12, 22), (50, 22), (8, 38), (56, 38), (20, 14), (44, 14)]
        for pmx, pmy in petal_motes:
            ov_d.ellipse([pmx, pmy, pmx+3, pmy+5], fill=(167, 243, 208, 255))
        im = Image.alpha_composite(im, overlay)
    elif f_idx == 3:
        # Cooldown: Sương mai lấp lánh đọng trên cánh hoa
        motes = [(32, 24), (28, 32), (36, 32), (32, 42)]
        for mx, my in motes:
            if 0 <= mx < 64 and 0 <= my < 64:
                im.putpixel((mx, my), (207, 250, 254, 220))

    return im

# ==============================================================================
# 4. NỎ XUYÊN VÂN (NoXuyenVan - Tier 1)
# ==============================================================================
def render_no_xuyen_van(f_idx):
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    c_out = (26, 21, 38, 255)
    c_wood_dark = (69, 26, 3, 255)
    c_wood_mid = (113, 63, 18, 255)
    c_wood_light = (161, 98, 7, 255)
    c_wood_hi = (202, 138, 4, 255)

    c_br_dark = (120, 53, 15, 255)
    c_br_light = (217, 119, 6, 255)

    c_iron_dark = (51, 65, 85, 255)
    c_iron_mid = (100, 116, 139, 255)
    c_iron_light = (148, 163, 184, 255)
    c_iron_white = (241, 245, 249, 255)

    # 1. Chân tháp chòi canh 4 cọc gỗ mít (Y: 36..60, X: 16..48)
    for y in range(38, 60):
        for tx in (19, 20, 43, 44):
            im.putpixel((tx, y), c_wood_dark if tx in (19, 43) else c_wood_mid)
            if tx in (19, 44):
                im.putpixel((tx - 1 if tx == 19 else tx + 1, y), c_out)
    # Giằng chéo giằng ngang
    d.line([(21, 44), (42, 54)], fill=c_wood_mid, width=2)
    d.line([(21, 54), (42, 44)], fill=c_wood_dark, width=2)
    d.line([(18, 48), (45, 48)], fill=c_wood_light, width=2)

    # Sàn chòi canh gỗ (Y: 34..38, X: 14..50)
    for y in range(34, 38):
        for x in range(14, 50):
            if y == 37 or x in (14, 49) or y == 34:
                im.putpixel((x, y), c_out)
            else:
                im.putpixel((x, y), c_wood_hi if y == 35 else c_wood_mid)

    # 2. Khung xoay & Trục nỏ máy cơ khí (Y: 24..34, Center X=32)
    for y in range(26, 34):
        for x in range(30, 35):
            if x in (30, 34) or y == 33:
                im.putpixel((x, y), c_out)
            else:
                im.putpixel((x, y), c_br_light if x < 33 else c_br_dark)

    # 3. Cỗ Giàn Nỏ Cánh Cung Kép Phong Lôi
    # Tùy theo Frame:
    # Frame 0 (idle): Cánh cung cong vừa, mũi tên nạp sẵn
    # Frame 1 (charge): Cánh cung uốn cong gập sâu về sau, đầu mũi tên sáng chớp
    # Frame 2 (fire): Cánh cung bật thẳng về trước, giàn nỏ giật lùi (recoil), mũi tên vút bay kèm vệt gió
    # Frame 3 (cooldown): Cánh cung rung lắc nhẹ, đang nạp mũi tên mới
    bow_pull = [0, 4, -3, 1][f_idx]
    by = 22

    # Rãnh trượt mũi tên (Thân nỏ X: 31..33, Y: 12..28)
    recoil_y = 2 if f_idx == 2 else 0
    for ry in range(14 + recoil_y, 28 + recoil_y):
        for rx in (31, 32, 33):
            if rx in (31, 33):
                im.putpixel((rx, ry), c_iron_dark)
            else:
                im.putpixel((rx, ry), c_iron_mid)

    # 2 Cánh cung nỏ kép (Trái & Phải)
    # Cánh cung trái
    left_tip = (14 + bow_pull, by + 2 - bow_pull)
    left_mid = (22 + bow_pull//2, by + 4)
    d.line([(31, by + 6 + recoil_y), left_mid, left_tip], fill=c_wood_dark, width=3)
    d.line([(31, by + 5 + recoil_y), (left_mid[0], left_mid[1]-1), (left_tip[0], left_tip[1]-1)], fill=c_wood_hi, width=1)

    # Cánh cung phải
    right_tip = (50 - bow_pull, by + 2 - bow_pull)
    right_mid = (42 - bow_pull//2, by + 4)
    d.line([(33, by + 6 + recoil_y), right_mid, right_tip], fill=c_wood_dark, width=3)
    d.line([(33, by + 5 + recoil_y), (right_mid[0], right_mid[1]-1), (right_tip[0], right_tip[1]-1)], fill=c_wood_hi, width=1)

    # Dây cung bện gân thú
    string_center = (32, by + 6 + bow_pull + recoil_y)
    d.line([left_tip, string_center], fill=(226, 232, 240, 255), width=1)
    d.line([right_tip, string_center], fill=(226, 232, 240, 255), width=1)

    # 4. Mũi tên sắt Xuyên Vân
    if f_idx in (0, 1):
        # Mũi tên nằm trên rãnh trượt (Y: 10..22)
        arr_y = 10 + bow_pull//2
        # Cán tên gỗ
        d.line([(32, arr_y + 4), (32, arr_y + 14)], fill=c_wood_light, width=1)
        # Đầu tên sắt nhọn
        d.polygon([(32, arr_y), (30, arr_y + 4), (34, arr_y + 4)], fill=c_iron_white)
        # Lông vũ đuôi tên
        im.putpixel((31, arr_y + 14), (241, 245, 249, 255))
        im.putpixel((33, arr_y + 14), (241, 245, 249, 255))
        if f_idx == 1:
            # Lóe sáng đầu mũi tên
            im.putpixel((32, arr_y - 1), (255, 255, 255, 255))
            im.putpixel((31, arr_y), (147, 197, 253, 255))
            im.putpixel((33, arr_y), (147, 197, 253, 255))
    elif f_idx == 2:
        # Fire: Mũi tên vọt đi về phía trước (Y: 2..14) kèm vệt khí động học xé gió
        arr_y = 2
        d.line([(32, arr_y + 4), (32, arr_y + 12)], fill=c_iron_mid, width=1)
        d.polygon([(32, arr_y), (30, arr_y + 4), (34, arr_y + 4)], fill=c_iron_white)
        # Vệt gió trắng xé gió
        d.line([(30, arr_y + 12), (28, arr_y + 20)], fill=(226, 232, 240, 180), width=1)
        d.line([(34, arr_y + 12), (36, arr_y + 20)], fill=(226, 232, 240, 180), width=1)
        # Tia dăm gỗ văng
        im.putpixel((27, by + 8), c_wood_hi)
        im.putpixel((37, by + 8), c_wood_hi)
    elif f_idx == 3:
        # Cooldown: Nạp mũi tên mới nửa chừng
        arr_y = 16
        d.line([(32, arr_y + 4), (32, arr_y + 12)], fill=c_wood_mid, width=1)
        d.polygon([(32, arr_y), (30, arr_y + 4), (34, arr_y + 4)], fill=c_iron_mid)

    return im

# ==============================================================================
# 5. BẪY CHÔNG GỖ (BayChongGo - Tier 1)
# ==============================================================================
def render_bay_chong_go(f_idx):
    im = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    c_out = (26, 21, 38, 255)
    c_grass_dark = (21, 128, 61, 255)
    c_grass_mid = (34, 197, 94, 255)
    c_grass_light = (74, 222, 128, 255)

    c_dirt_dark = (69, 26, 3, 255)
    c_dirt_mid = (113, 63, 18, 255)
    c_dirt_light = (161, 98, 7, 255)

    c_wood_dark = (69, 26, 3, 255)
    c_wood_mid = (120, 53, 15, 255)
    c_wood_light = (180, 83, 9, 255)
    c_wood_hi = (217, 119, 6, 255)
    c_tip_sharp = (254, 240, 138, 255)

    # 1. Thảm cỏ xanh nông trang xung quanh bẫy (Y: 34..58, X: 12..52)
    draw_pixel_ellipse(im, 32, 46, 22, 11, c_grass_mid, c_out)
    for y in range(35, 58):
        for x in range(11, 54):
            if 0 <= x < 64 and 0 <= y < 64 and im.getpixel((x, y))[3] == 255 and im.getpixel((x, y)) != c_out:
                if y > 50:
                    im.putpixel((x, y), c_grass_dark)
                elif y < 40 or x < 24:
                    im.putpixel((x, y), c_grass_light)

    # 2. Khung gỗ sồi của hố bẫy ngầm (X: 18..46, Y: 38..52)
    d.polygon([(18, 45), (32, 38), (46, 45), (32, 52)], fill=c_dirt_dark, outline=c_out)

    # 3. Trạng thái bẫy theo Frame
    if f_idx == 0:
        # Idle: Nắp bẫy phẳng lì ngụy trang cỏ và gỗ
        d.polygon([(20, 45), (32, 40), (44, 45), (32, 50)], fill=c_wood_mid, outline=c_wood_dark)
        # Rãnh then cài
        d.line([(24, 45), (40, 45)], fill=c_wood_dark, width=1)
        # Vài lá cây cỏ phủ ngụy trang
        im.putpixel((28, 44), c_grass_light)
        im.putpixel((29, 44), c_grass_mid)
        im.putpixel((35, 46), c_grass_light)
    elif f_idx == 1:
        # Trigger: Bàn đạp sụt lún 3px, nắp bẫy hé mở lộ đầu cọc chông nhọn
        d.polygon([(21, 46), (32, 42), (43, 46), (32, 51)], fill=c_dirt_dark, outline=c_out)
        # Đầu các cọc chông nhú lên 3px trong bóng tối
        spike_tips = [(25, 44), (29, 42), (32, 41), (35, 42), (39, 44)]
        for sx, sy in spike_tips:
            im.putpixel((sx, sy), c_tip_sharp)
            im.putpixel((sx, sy + 1), c_wood_hi)
    elif f_idx == 2:
        # Sprung/Fire: 8 cọc chông gỗ sồi nhọn hoắt bung vọt ngược lên khỏi mặt đất!
        # Cọc chông cắm ở các góc nghiêng khác nhau
        spikes = [
            (22, 47, 18, 26), # Cọc 1 nghiêng trái
            (26, 45, 23, 20), # Cọc 2
            (29, 48, 28, 16), # Cọc 3
            (32, 44, 32, 12), # Cọc 4 chính giữa vút cao
            (35, 47, 36, 17), # Cọc 5
            (38, 44, 41, 21), # Cọc 6
            (42, 46, 46, 27), # Cọc 7 nghiêng phải
            (32, 49, 33, 24), # Cọc 8 phụ
        ]
        # Vẽ các cọc chông
        for bx, by, tx, ty in spikes:
            d.line([(bx, by), (tx, ty)], fill=c_wood_dark, width=3)
            d.line([(bx, by), (tx, ty)], fill=c_wood_light, width=2)
            d.line([(bx, by-1), (tx, ty-1)], fill=c_wood_hi, width=1)
            # Mũi nhọn vót tẩm nhựa cây vàng
            im.putpixel((tx, ty), c_tip_sharp)
            im.putpixel((tx, ty - 1), (255, 255, 255, 255))
            im.putpixel((tx - 1, ty), c_out)
            im.putpixel((tx + 1, ty), c_out)
        # Đất đá và vụn cỏ văng tung tóe
        dirt_debris = [(15, 36), (20, 30), (45, 32), (48, 38), (30, 28), (36, 26)]
        for dx, dy in dirt_debris:
            if 0 <= dx < 64 and 0 <= dy < 64:
                im.putpixel((dx, dy), c_dirt_mid)
                im.putpixel((dx+1, dy), c_grass_light)
    elif f_idx == 3:
        # Cooldown/Rest: Các cọc chông cắm sừng sững trên đất nứt
        spikes = [
            (22, 47, 19, 30),
            (26, 45, 24, 24),
            (32, 44, 32, 18),
            (38, 44, 40, 25),
            (42, 46, 45, 31),
        ]
        for bx, by, tx, ty in spikes:
            d.line([(bx, by), (tx, ty)], fill=c_wood_dark, width=3)
            d.line([(bx, by), (tx, ty)], fill=c_wood_mid, width=2)
            im.putpixel((tx, ty), c_tip_sharp)
        # Nứt đất
        d.line([(24, 49), (40, 49)], fill=c_dirt_dark, width=1)

    return im

def main():
    towers = [
        ("CuuTieuLoiPhao", render_cuu_tieu_loi_phao, "cuu_tieu_loi_phao"),
        ("ThienNhanVongDai", render_thien_nhan_vong_dai, "thien_nhan_vong_dai"),
        ("VanLinhHoiNguyenTran", render_van_linh_hoi_nguyen_tran, "van_linh_hoi_nguyen_tran"),
        ("NoXuyenVan", render_no_xuyen_van, "no_xuyen_van"),
        ("BayChongGo", render_bay_chong_go, "bay_chong_go"),
    ]

    suffixes = ["idle", "charge", "fire", "cooldown"]

    for bp_id, render_fn, sheet_name in towers:
        frames = []
        # Sheet 4 frames horizontal (256x64)
        sheet = Image.new("RGBA", (256, 64), (0, 0, 0, 0))

        for f_idx in range(4):
            frame = render_fn(f_idx)
            frames.append(frame)

            # Lưu từng frame đơn lẻ vào BachVanTowers
            fname = f"{bp_id}_{suffixes[f_idx]}.png"
            fpath = os.path.join(OUT_DIR, fname)
            frame.save(fpath, "PNG")
            make_meta(fpath)

            # Dán vào sheet
            sheet.paste(frame, (f_idx * 64, 0), frame)

        # Lưu spritesheet ngang vào Buildings
        sheet_path = os.path.join(SHEET_DIR, f"{sheet_name}_sheet.png")
        sheet.save(sheet_path, "PNG")
        make_meta(sheet_path)

        # Tạo ảnh động GIF trong thư mục artifact để báo cáo trực quan cho user
        gif_path = os.path.join(ARTIFACT_DIR, f"{sheet_name}_anim.gif")
        durations = [400, 300, 450, 350]
        frames[0].save(gif_path, save_all=True, append_images=frames[1:], duration=durations, loop=0, disposal=2)

        print(f"✔ Đã hoàn tất xuất xưởng đồ họa cho: {bp_id} (4 frames + Sheet + GIF)")

if __name__ == "__main__":
    main()
