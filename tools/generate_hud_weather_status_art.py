#!/usr/bin/env python3
"""
Generates crisp 2.5D Mythic-Chibi HUD art assets for Pig Tycoon:
- Kit/sundial_base.png (64x64 4-daypart enameled astrological dial: Sang/Trua/Chieu/Dem)
- Kit/sundial_needle.png (64x64 ornate golden sun-pointer needle)
- Icons/weather/{quang_dang,nhieu_may,nang_gat,mua,giong_bao,suong_mu,ret_dam,nom_am,gio_kho,mua_buc_xa}_{16,24}.png
- Icons/status/{cong_tay,kho_rat,lac_huong,bat_tinh,kiet_suc,met_doi,cam_nang,uot_lanh,cam_gio,nhiem_xa,la_do_suong,say_nang,qua_nhiet,loi_tam_spark}_{16,24}.png
- Missing Icons/actions/*_{16,24}.png, Icons/places/*_{16,24}.png, Icons/misc/*_{16,24}.png
- Missing Kit/{herd_pighead_16,morale_face_0..3,bar_rage_flame_0..3,cooldown_wedge_*,ready_pulse_*}.png
Syncs outputs to both:
- D:/Pig-tycoon/Assets/Art/PixelMyth/UI
- D:/Pig-tycoon/.collab/claude-2d/ui_and_map_sync/from_argon/Assets/Art/PixelMyth/UI
"""

import math
import os
import shutil
from pathlib import Path
from PIL import Image, ImageDraw

ROOTS = [
    Path(r"D:\Pig-tycoon\Assets\Art\PixelMyth\UI"),
    Path(r"D:\Pig-tycoon\.collab\claude-2d\ui_and_map_sync\from_argon\Assets\Art\PixelMyth\UI"),
]


def clamp(v, lo=0, hi=255):
    return max(lo, min(hi, int(round(v))))


def rgba(r, g, b, a=255):
    return (clamp(r), clamp(g), clamp(b), clamp(a))


def mix(c1, c2, t):
    return rgba(
        c1[0] + (c2[0] - c1[0]) * t,
        c1[1] + (c2[1] - c1[1]) * t,
        c1[2] + (c2[2] - c1[2]) * t,
        c1[3] + (c2[3] - c1[3]) * t,
    )


def save_both(rel_path: str, img: Image.Image):
    for root in ROOTS:
        out_path = root / rel_path
        out_path.parent.mkdir(parents=True, exist_ok=True)
        img.save(out_path, "PNG")


def make_sundial_base() -> Image.Image:
    """
    64x64 2.5D carved bronze/gold astrological sun-moon dial with 4 distinct enameled quadrants:
    Top-Left / Left-Upper: Sang (Morning Dawn Rose-Gold)
    Top-Right / Upper: Trua (Noon Sun Azure-Gold)
    Bottom-Right / Right-Lower: Chieu (Afternoon Sunset Amber-Crimson)
    Bottom-Left / Lower: Dem (Night Starry Indigo)
    Note: MythHudController rotates needle with z = -(hour/24)*360 + 90:
      06:00 -> z = 0 (points Right / East, or let's make quadrants match clock angles!)
      Wait: in MythHudController:
        00:00 -> z = +90 (Up? No, sundial_needle points UP at z=0! Wait: let's check sundial_needle orientation!)
        In sundial_needle.png, the needle points UP (+Y) at rotation 0.
        When z = -(h/24)*360 + 90:
          h = 6 (06:00 Sang)  -> z = 0 deg (Points UP / North) -> 06:00-12:00 is Top-Left to Left/Bottom!
          Wait: in Unity UI, positive Z rotation is counter-clockwise, negative Z rotation is clockwise!
          Wait: at h=6, z = 0 (Up). At h=12, z = -90 (90 deg clockwise = Right).
          At h=18, z = -180 (Down). At h=24/0, z = -270/+90 (Left).
          So:
            Top-Right quadrant (0 deg Up to -90 deg Right): 06:00 - 12:00 (Sang -> Trua)
            Bottom-Right quadrant (-90 deg Right to -180 deg Down): 12:00 - 18:00 (Trua -> Chieu)
            Bottom-Left quadrant (-180 deg Down to -270 deg Left): 18:00 - 24:00 (Chieu -> Dem)
            Top-Left quadrant (+90 deg Left to 0 deg Up): 00:00 - 06:00 (Dem -> Sang)
    """
    w = h = 64
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = img.load()
    cx, cy = 31.5, 31.5

    # Palette
    OUTLINE = (24, 15, 10, 255)
    BRONZE_DARK = (74, 46, 24, 255)
    BRONZE_MID = (138, 92, 42, 255)
    GOLD_BRIGHT = (236, 188, 86, 255)
    GOLD_HI = (255, 232, 148, 255)
    SLATE_SOCKET = (28, 24, 34, 255)
    SLATE_RIM = (58, 48, 64, 255)

    # Quadrant colors
    Q_SANG_A = (232, 134, 118, 255)   # Top-Right: Morning Dawn Rose-Gold
    Q_SANG_B = (248, 198, 116, 255)
    Q_TRUA_A = (66, 168, 236, 255)    # Bottom-Right: Noon Azure-Gold
    Q_TRUA_B = (245, 208, 96, 255)
    Q_CHIEU_A = (224, 96, 52, 255)    # Bottom-Left: Afternoon Sunset Amber-Crimson
    Q_CHIEU_B = (146, 44, 72, 255)
    Q_DEM_A = (36, 42, 98, 255)       # Top-Left: Night Starry Indigo
    Q_DEM_B = (20, 22, 58, 255)

    for y in range(h):
        for x in range(w):
            dx = x - cx
            dy = y - cy
            r = math.hypot(dx, dy)
            if r > 30.8:
                continue

            # Angle where 0 is Up (12 o'clock), 90 is Right (3 o'clock), 180 is Down (6 o'clock), 270 is Left (9 o'clock)
            ang = (math.degrees(math.atan2(dx, -dy)) + 360.0) % 360.0

            # Outer shadow / outline
            if r > 29.2:
                px[x, y] = OUTLINE
            elif r > 25.5:
                # Carved outer bronze-gold bezel with 2.5D top-left lighting
                light = (-dx - dy) / (2.0 * max(1.0, r))
                base = mix(BRONZE_DARK, GOLD_BRIGHT, clamp((light + 1.0) * 0.5 * 255) / 255.0)
                # Cardinal gem notches at 0, 90, 180, 270
                if min(ang % 90.0, 90.0 - (ang % 90.0)) < 5.5:
                    base = GOLD_HI if r < 28.0 else BRONZE_DARK
                px[x, y] = base
            elif r > 24.2:
                px[x, y] = OUTLINE
            elif r > 13.2:
                # 4 Enameled Daypart Quadrants!
                t_rad = (r - 13.2) / (24.2 - 13.2)
                # Divider ribs between quadrants at 0, 90, 180, 270 deg
                dist_rib = min(ang % 90.0, 90.0 - (ang % 90.0))
                if dist_rib < 2.8:
                    px[x, y] = GOLD_BRIGHT if dist_rib < 1.4 else BRONZE_DARK
                else:
                    if 0.0 <= ang < 90.0:
                        # 06:00 - 12:00: Sang (Dawn Rose-Gold)
                        qcol = mix(Q_SANG_A, Q_SANG_B, t_rad)
                    elif 90.0 <= ang < 180.0:
                        # 12:00 - 18:00: Trua/Chieu (Noon Azure-Gold)
                        qcol = mix(Q_TRUA_B, Q_TRUA_A, t_rad)
                    elif 180.0 <= ang < 270.0:
                        # 18:00 - 22:00: Chieu/Hoang Hon (Sunset Amber-Crimson)
                        qcol = mix(Q_CHIEU_A, Q_CHIEU_B, t_rad)
                    else:
                        # 22:00 - 06:00: Dem (Starry Indigo)
                        qcol = mix(Q_DEM_A, Q_DEM_B, t_rad)

                    # Subtle inner bevel shading
                    if r > 22.8 or r < 14.3:
                        qcol = mix(qcol, OUTLINE, 0.38)
                    px[x, y] = qcol
            elif r > 11.4:
                # Inner gold ring framing the weather icon socket
                light = (-dx - dy) / (2.0 * max(1.0, r))
                px[x, y] = GOLD_HI if light > 0.2 else (GOLD_BRIGHT if light > -0.3 else BRONZE_MID)
            else:
                # Central recessed dark-slate socket for the 24x24 weather icon
                t_in = r / 11.4
                px[x, y] = mix(SLATE_SOCKET, SLATE_RIM, t_in * 0.65)

    # Add tiny celestial pixel details in the 4 quadrants (stars in Night, sun rays in Noon, dawn cloud in Morning, crescent/dusk in Evening)
    stars = [(18, 18), (22, 14), (15, 23), (24, 20)]
    for sx, sy in stars:
        px[sx, sy] = (255, 244, 196, 255)
    # Morning sparkle dots
    for sx, sy in [(43, 16), (47, 21), (39, 14)]:
        px[sx, sy] = (255, 238, 178, 255)
    # Noon sun glint dots
    for sx, sy in [(46, 42), (42, 47), (48, 37)]:
        px[sx, sy] = (255, 250, 210, 255)
    # Sunset warm embers
    for sx, sy in [(18, 44), (23, 47), (15, 39)]:
        px[sx, sy] = (255, 196, 110, 255)

    return img


def make_sundial_needle() -> Image.Image:
    """64x64 crisp 2.5D golden astrological pointer needle pointing UP (+Y, towards y=2)."""
    w = h = 64
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Pointer tip near top rim (y=2..16) so it doesn't obscure the central 24x24 weather icon!
    # Plus a subtle golden ring collar around the center socket (r=12) and tail counterweight
    OUTLINE = (24, 14, 8, 255)
    GOLD_HI = (255, 238, 156, 255)
    GOLD_MID = (238, 184, 68, 255)
    GOLD_DARK = (156, 98, 28, 255)
    CRIMSON_GEM = (232, 68, 56, 255)

    # Main pointer spearhead at top (x=31..32, y=2..17)
    draw.polygon([(32, 2), (26, 15), (32, 12), (38, 15)], fill=GOLD_MID, outline=OUTLINE)
    draw.polygon([(32, 3), (28, 14), (32, 11)], fill=GOLD_HI)
    draw.polygon([(32, 3), (36, 14), (32, 11)], fill=GOLD_DARK)
    # Glowing crimson-gold sun gem at needle base (y=9..11)
    draw.ellipse([30, 8, 34, 12], fill=CRIMSON_GEM, outline=OUTLINE)
    img.putpixel((31, 9), (255, 210, 190, 255))

    # Small counterweight tail at bottom (y=50..57)
    draw.polygon([(32, 58), (29, 50), (35, 50)], fill=GOLD_DARK, outline=OUTLINE)
    draw.line([(32, 51), (32, 56)], fill=GOLD_HI)
    return img


def draw_badge_bg(size: int, rim_col, fill_dark, fill_light):
    """Creates a crisp 2.5D rounded pixel medallion/plate background for 16x16 or 24x24 icons."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    cx = (size - 1) / 2.0
    cy = (size - 1) / 2.0
    max_r = size * 0.47
    OUTLINE = (20, 14, 12, 255)

    for y in range(size):
        for x in range(size):
            dx = x - cx
            dy = y - cy
            # Squircle distance for crisp pixel silhouette
            r = math.pow(abs(dx) ** 2.4 + abs(dy) ** 2.4, 1.0 / 2.4)
            if r > max_r + 0.6:
                continue
            if r > max_r - 0.5:
                px[x, y] = OUTLINE
            elif r > max_r - 1.6:
                px[x, y] = rim_col
            else:
                t = clamp((dy / max_r + 1.0) * 0.5 * 255) / 255.0
                px[x, y] = mix(fill_light, fill_dark, t)
    return img


def make_weather_icon(stem: str, size: int) -> Image.Image:
    """Generates a 16x16 or 24x24 2.5D weather icon for all 10 WeatherType values."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = size / 24.0

    def sc(v):
        return int(round(v * s))

    OUT = (22, 15, 12, 255)
    SUN_HI = (255, 242, 132, 255)
    SUN_MID = (246, 182, 46, 255)
    SUN_HOT = (242, 92, 38, 255)
    CLOUD_HI = (242, 246, 255, 255)
    CLOUD_MID = (182, 198, 222, 255)
    CLOUD_DARK = (96, 110, 138, 255)
    RAIN_COL = (88, 194, 255, 255)
    BOLT_COL = (255, 232, 76, 255)
    ICE_COL = (132, 234, 255, 255)
    RAD_COL = (164, 244, 74, 255)
    RAD_PURPLE = (168, 86, 232, 255)

    def draw_sun(cx, cy, r_sun, hot=False):
        c_fill = SUN_HOT if hot else SUN_MID
        c_hi = SUN_MID if hot else SUN_HI
        # 8 rays
        for deg in range(0, 360, 45):
            rad = math.radians(deg)
            x1 = int(round(cx + math.cos(rad) * (r_sun + 1)))
            y1 = int(round(cy + math.sin(rad) * (r_sun + 1)))
            x2 = int(round(cx + math.cos(rad) * (r_sun + max(2, sc(3.5)))))
            y2 = int(round(cy + math.sin(rad) * (r_sun + max(2, sc(3.5)))))
            d.line([(x1, y1), (x2, y2)], fill=OUT, width=max(1, sc(2.2)))
            d.line([(x1, y1), (x2, y2)], fill=c_hi, width=1)
        d.ellipse([cx - r_sun - 1, cy - r_sun - 1, cx + r_sun + 1, cy + r_sun + 1], fill=OUT)
        d.ellipse([cx - r_sun, cy - r_sun, cx + r_sun, cy + r_sun], fill=c_fill)
        d.ellipse([cx - r_sun + 1, cy - r_sun + 1, cx, cy], fill=c_hi)

    def draw_cloud(y_off=0, dark=False):
        c_top = CLOUD_MID if dark else CLOUD_HI
        c_bot = CLOUD_DARK if dark else CLOUD_MID
        blobs = [
            (sc(8), sc(13 + y_off), sc(4.5)),
            (sc(13), sc(11 + y_off), sc(5.5)),
            (sc(17), sc(13 + y_off), sc(4.2)),
        ]
        for bx, by, br in blobs:
            d.ellipse([bx - br - 1, by - br - 1, bx + br + 1, by + br + 1], fill=OUT)
        d.rectangle([sc(7), sc(12 + y_off), sc(18), sc(17 + y_off)], fill=OUT)
        for bx, by, br in blobs:
            d.ellipse([bx - br, by - br, bx + br, by + br], fill=c_bot)
            d.ellipse([bx - br + 1, by - br + 1, bx + br - 1, by], fill=c_top)
        d.rectangle([sc(8), sc(13 + y_off), sc(17), sc(16 + y_off)], fill=c_bot)

    if stem == "quang_dang":
        draw_sun(sc(12), sc(12), max(4, sc(5.5)), hot=False)
    elif stem == "nhieu_may":
        draw_sun(sc(9), sc(8), max(3, sc(4.2)), hot=False)
        draw_cloud(y_off=2, dark=False)
    elif stem == "nang_gat":
        draw_sun(sc(12), sc(12), max(4, sc(6.0)), hot=True)
        # Heat waves at bottom
        d.line([(sc(5), sc(20)), (sc(19), sc(20))], fill=(255, 120, 50, 255), width=1)
    elif stem == "mua":
        draw_cloud(y_off=-2, dark=True)
        for rx in [sc(7), sc(12), sc(17)]:
            d.line([(rx, sc(16)), (rx - sc(2), sc(21))], fill=OUT, width=max(1, sc(2.2)))
            d.line([(rx, sc(16)), (rx - sc(2), sc(21))], fill=RAIN_COL, width=1)
    elif stem == "giong_bao":
        draw_cloud(y_off=-3, dark=True)
        bolt = [(sc(13), sc(13)), (sc(10), sc(18)), (sc(13), sc(18)), (sc(11), sc(22))]
        d.line(bolt, fill=OUT, width=max(2, sc(3)))
        d.line(bolt, fill=BOLT_COL, width=max(1, sc(1.5)))
    elif stem == "suong_mu":
        for idx, yy in enumerate([sc(7), sc(11), sc(15), sc(19)]):
            x1 = sc(4 if idx % 2 == 0 else 6)
            x2 = sc(19 if idx % 2 == 0 else 20)
            d.line([(x1, yy), (x2, yy)], fill=OUT, width=max(2, sc(3)))
            d.line([(x1, yy), (x2, yy)], fill=CLOUD_HI if idx < 2 else CLOUD_MID, width=max(1, sc(1.5)))
    elif stem == "ret_dam":
        cx, cy, r = sc(12), sc(12), sc(8)
        for deg in [0, 60, 120]:
            rad = math.radians(deg)
            dx = int(round(math.cos(rad) * r))
            dy = int(round(math.sin(rad) * r))
            d.line([(cx - dx, cy - dy), (cx + dx, cy + dy)], fill=OUT, width=max(2, sc(3)))
            d.line([(cx - dx, cy - dy), (cx + dx, cy + dy)], fill=ICE_COL, width=1)
        d.ellipse([cx - sc(2), cy - sc(2), cx + sc(2), cy + sc(2)], fill=CLOUD_HI, outline=OUT)
    elif stem == "nom_am":
        draw_cloud(y_off=-2, dark=False)
        for dx, dy in [(sc(8), sc(18)), (sc(13), sc(20)), (sc(17), sc(18))]:
            d.ellipse([dx - sc(2), dy - sc(2), dx + sc(2), dy + sc(2)], fill=RAIN_COL, outline=OUT)
    elif stem == "gio_kho":
        for yy, xend in [(sc(8), sc(19)), (sc(13), sc(21)), (sc(18), sc(16))]:
            d.line([(sc(4), yy), (xend, yy)], fill=OUT, width=max(2, sc(3)))
            d.line([(sc(4), yy), (xend, yy)], fill=(238, 198, 124, 255), width=1)
            d.ellipse([xend - sc(2), yy - sc(3), xend + sc(1), yy], fill=(238, 198, 124, 255), outline=OUT)
    elif stem == "mua_buc_xa":
        draw_cloud(y_off=-2, dark=True)
        for rx in [sc(8), sc(13), sc(17)]:
            d.line([(rx, sc(15)), (rx - sc(1), sc(21))], fill=RAD_PURPLE, width=max(2, sc(2.5)))
            d.line([(rx, sc(15)), (rx - sc(1), sc(21))], fill=RAD_COL, width=1)
    return img


def make_symbolic_icon(category: str, stem: str, size: int) -> Image.Image:
    """Generates crisp 2.5D symbolic pixel icons for status, actions, places, and misc."""
    # Choose category color scheme
    if category == "status":
        if stem in ("cong_tay", "ret_dam", "uot_lanh", "cam_gio"):
            bg = draw_badge_bg(size, (110, 210, 250, 255), (26, 58, 92, 255), (56, 118, 172, 255))
            fg_hi, fg_mid = (224, 248, 255, 255), (122, 214, 255, 255)
        elif stem in ("kho_rat", "cam_nang", "say_nang", "qua_nhiet"):
            bg = draw_badge_bg(size, (248, 168, 72, 255), (98, 32, 20, 255), (186, 68, 32, 255))
            fg_hi, fg_mid = (255, 238, 148, 255), (255, 146, 56, 255)
        elif stem in ("lac_huong", "nhiem_xa", "bat_tinh"):
            bg = draw_badge_bg(size, (198, 124, 248, 255), (54, 24, 82, 255), (108, 48, 154, 255))
            fg_hi, fg_mid = (232, 255, 148, 255), (186, 120, 255, 255)
        elif stem in ("la_do_suong", "loi_tam_spark"):
            bg = draw_badge_bg(size, (116, 232, 148, 255), (24, 72, 42, 255), (48, 136, 78, 255))
            fg_hi, fg_mid = (236, 255, 198, 255), (118, 238, 152, 255)
        else:
            bg = draw_badge_bg(size, (238, 184, 82, 255), (82, 46, 22, 255), (148, 86, 38, 255))
            fg_hi, fg_mid = (255, 238, 168, 255), (238, 172, 72, 255)
    elif category == "actions":
        bg = draw_badge_bg(size, (238, 192, 92, 255), (56, 38, 26, 255), (112, 74, 44, 255))
        fg_hi, fg_mid = (255, 244, 186, 255), (238, 186, 78, 255)
    else:
        bg = draw_badge_bg(size, (218, 174, 88, 255), (44, 52, 68, 255), (78, 94, 122, 255))
        fg_hi, fg_mid = (255, 242, 182, 255), (186, 214, 248, 255)

    d = ImageDraw.Draw(bg)
    s = size / 24.0
    sc = lambda v: int(round(v * s))
    OUT = (18, 12, 10, 255)

    if stem == "cong_tay":
        # Frozen blue hand / crystal frost motif
        d.polygon([(sc(12), sc(4)), (sc(17), sc(10)), (sc(14), sc(19)), (sc(10), sc(19)), (sc(7), sc(10))], fill=fg_mid, outline=OUT)
        d.line([(sc(12), sc(6)), (sc(12), sc(17))], fill=fg_hi, width=max(1, sc(1.5)))
        d.line([(sc(8), sc(11)), (sc(16), sc(11))], fill=fg_hi, width=1)
    elif stem == "kho_rat":
        # Cracked parched drop / scorched throat flame
        d.polygon([(sc(12), sc(4)), (sc(18), sc(14)), (sc(12), sc(20)), (sc(6), sc(14))], fill=fg_mid, outline=OUT)
        d.line([(sc(12), sc(6)), (sc(10), sc(12)), (sc(14), sc(14)), (sc(11), sc(18))], fill=OUT, width=max(1, sc(1.5)))
        d.ellipse([sc(10), sc(7), sc(13), sc(10)], fill=fg_hi)
    elif stem == "lac_huong":
        # Tilted mystic compass / fog spiral
        d.ellipse([sc(5), sc(5), sc(19), sc(19)], fill=(38, 22, 58, 255), outline=OUT)
        d.polygon([(sc(12), sc(6)), (sc(15), sc(12)), (sc(12), sc(18)), (sc(9), sc(12))], fill=fg_mid, outline=OUT)
        d.polygon([(sc(12), sc(6)), (sc(14), sc(12)), (sc(12), sc(12))], fill=fg_hi)
    elif stem in ("danh", "phan_kich"):
        # Crossed/striking mythic blade
        d.line([(sc(6), sc(18)), (sc(18), sc(6))], fill=OUT, width=max(2, sc(4)))
        d.line([(sc(6), sc(18)), (sc(18), sc(6))], fill=fg_hi, width=max(1, sc(2)))
        d.line([(sc(7), sc(13)), (sc(11), sc(17))], fill=fg_mid, width=max(1, sc(2)))
    elif stem == "huyt_sao":
        # Golden whistle + sound wave arcs
        d.ellipse([sc(6), sc(10), sc(14), sc(18)], fill=fg_mid, outline=OUT)
        d.rectangle([sc(12), sc(9), sc(18), sc(13)], fill=fg_hi, outline=OUT)
        d.arc([sc(14), sc(4), sc(21), sc(12)], 270, 40, fill=fg_hi, width=1)
    elif stem == "giam_dinh":
        # All-seeing golden appraisal eye / magnifier
        d.ellipse([sc(5), sc(7), sc(19), sc(17)], fill=fg_hi, outline=OUT)
        d.ellipse([sc(9), sc(8), sc(15), sc(16)], fill=(42, 118, 188, 255), outline=OUT)
        d.ellipse([sc(11), sc(10), sc(13), sc(13)], fill=OUT)
    elif stem in ("kho", "khu_xu_ly", "vao_cua"):
        # Ornate treasure chest / timber gate
        d.rectangle([sc(5), sc(8), sc(19), sc(18)], fill=fg_mid, outline=OUT)
        d.line([(sc(5), sc(12)), (sc(19), sc(12))], fill=OUT, width=max(1, sc(1.5)))
        d.rectangle([sc(10), sc(10), sc(14), sc(15)], fill=fg_hi, outline=OUT)
    elif stem in ("cho_an", "met_doi"):
        # Golden grain bowl
        d.pieslice([sc(5), sc(8), sc(19), sc(20)], 0, 180, fill=fg_mid, outline=OUT)
        d.ellipse([sc(6), sc(8), sc(18), sc(13)], fill=fg_hi, outline=OUT)
    elif stem in ("cho_uong", "uot_lanh"):
        # Crystal water droplet
        d.polygon([(sc(12), sc(4)), (sc(18), sc(14)), (sc(12), sc(19)), (sc(6), sc(14))], fill=(92, 198, 255, 255), outline=OUT)
        d.ellipse([sc(9), sc(11), sc(12), sc(15)], fill=(224, 248, 255, 255))
    else:
        # Crisp diamond emblem with inner highlight
        d.polygon([(sc(12), sc(5)), (sc(19), sc(12)), (sc(12), sc(19)), (sc(5), sc(12))], fill=fg_mid, outline=OUT)
        d.polygon([(sc(12), sc(7)), (sc(16), sc(12)), (sc(12), sc(15)), (sc(8), sc(12))], fill=fg_hi)

    return bg


def make_kit_extras():
    """Generates missing Kit sprites so herd ring, morale face, rage flame, and cooldown wedges render cleanly."""
    # 1. herd_pighead_16.png
    pig = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(pig)
    OUT = (28, 14, 14, 255)
    PINK_HI = (255, 192, 204, 255)
    PINK_MID = (238, 136, 158, 255)
    d.polygon([(2, 6), (4, 2), (7, 5)], fill=PINK_MID, outline=OUT)
    d.polygon([(13, 6), (11, 2), (8, 5)], fill=PINK_MID, outline=OUT)
    d.ellipse([2, 3, 13, 13], fill=PINK_MID, outline=OUT)
    d.ellipse([4, 4, 11, 8], fill=PINK_HI)
    d.ellipse([5, 7, 10, 11], fill=(255, 156, 176, 255), outline=OUT)
    pig.putpixel((6, 9), OUT)
    pig.putpixel((9, 9), OUT)
    pig.putpixel((4, 6), OUT)
    pig.putpixel((11, 6), OUT)
    save_both("Kit/herd_pighead_16.png", pig)

    # 2. morale_face_0..3.png (16x16)
    colors = [
        (228, 72, 64, 255),    # 0: danger
        (238, 156, 58, 255),   # 1: tense
        (118, 214, 92, 255),   # 2: stable
        (255, 216, 76, 255),   # 3: radiant
    ]
    for idx, col in enumerate(colors):
        im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        dd = ImageDraw.Draw(im)
        dd.ellipse([1, 1, 14, 14], fill=col, outline=OUT)
        dd.ellipse([3, 2, 11, 7], fill=mix(col, (255, 255, 255, 255), 0.35))
        im.putpixel((5, 6), OUT)
        im.putpixel((10, 6), OUT)
        if idx >= 2:
            dd.arc([4, 6, 11, 12], 10, 170, fill=OUT, width=1)
        elif idx == 1:
            dd.line([(5, 10), (10, 10)], fill=OUT, width=1)
        else:
            dd.arc([4, 8, 11, 13], 190, 350, fill=OUT, width=1)
        save_both(f"Kit/morale_face_{idx}.png", im)

    # 3. bar_rage_flame_0..3.png (16x16)
    for idx in range(4):
        im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        dd = ImageDraw.Draw(im)
        h_top = 4 - (idx % 2)
        dd.polygon([(8, h_top), (13, 9), (11, 14), (5, 14), (3, 9)], fill=(242, 96, 36, 255), outline=OUT)
        dd.polygon([(8, h_top + 3), (11, 10), (8, 13), (5, 10)], fill=(255, 224, 92, 255))
        save_both(f"Kit/bar_rage_flame_{idx}.png", im)

    # 4. cooldown_wedge_{large,small}_0..7.png and ready_pulse_{large,small}_0..1.png
    for kind, sz in [("large", 48), ("small", 32)]:
        cx = cy = (sz - 1) / 2.0
        max_r = sz * 0.45
        for step in range(8):
            im = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
            px = im.load()
            max_ang = (8 - step) / 8.0 * 360.0
            for y in range(sz):
                for x in range(sz):
                    dx, dy = x - cx, y - cy
                    r = math.hypot(dx, dy)
                    if r <= max_r:
                        ang = (math.degrees(math.atan2(dx, -dy)) + 360.0) % 360.0
                        if ang <= max_ang:
                            px[x, y] = (12, 10, 18, 165)
            save_both(f"Kit/cooldown_wedge_{kind}_{step}.png", im)

        for p_idx in range(2):
            im = Image.new("RGBA", (sz, sz), (0, 0, 0, 0))
            px = im.load()
            r_in = max_r - (2.8 if p_idx == 0 else 3.6)
            alpha = 185 if p_idx == 0 else 235
            for y in range(sz):
                for x in range(sz):
                    r = math.hypot(x - cx, y - cy)
                    if r_in <= r <= max_r + 0.5:
                        px[x, y] = (255, 226, 112, alpha)
            save_both(f"Kit/ready_pulse_{kind}_{p_idx}.png", im)


def main():
    # 1. Sundial Base & Needle
    save_both("Kit/sundial_base.png", make_sundial_base())
    save_both("Kit/sundial_needle.png", make_sundial_needle())

    # 2. All 10 Weather Icons (16 & 24)
    weather_stems = [
        "quang_dang", "nhieu_may", "nang_gat", "mua", "giong_bao",
        "suong_mu", "ret_dam", "nom_am", "gio_kho", "mua_buc_xa",
    ]
    for stem in weather_stems:
        for sz in (16, 24):
            save_both(f"Icons/weather/{stem}_{sz}.png", make_weather_icon(stem, sz))

    # 3. Status Icons (16 & 24) - always generate cong_tay, kho_rat, lac_huong, plus any missing status icons
    status_stems = [
        "cong_tay", "kho_rat", "lac_huong",
        "bat_tinh", "kiet_suc", "met_doi", "cam_nang",
        "uot_lanh", "cam_gio", "nhiem_xa",
        "la_do_suong", "say_nang", "qua_nhiet", "loi_tam_spark",
    ]
    for stem in status_stems:
        for sz in (16, 24):
            rel = f"Icons/status/{stem}_{sz}.png"
            if stem in ("cong_tay", "kho_rat", "lac_huong") or not (ROOTS[0] / rel).exists():
                save_both(rel, make_symbolic_icon("status", stem, sz))

    # 4. Missing Action, Place, Misc Icons (16 & 24)
    action_stems = [
        "danh", "do_don", "hoc_vo", "cuong_hoa", "huyt_sao",
        "sua", "vao_cua", "giam_dinh", "cho_uong", "cho_an",
        "nap_dan", "trao_doi", "ngu", "can_cot", "luot_ne", "phan_kich",
    ]
    for stem in action_stems:
        for sz in (16, 24):
            rel = f"Icons/actions/{stem}_{sz}.png"
            if not (ROOTS[0] / rel).exists():
                save_both(rel, make_symbolic_icon("actions", stem, sz))

    for stem in ["kho", "khu_xu_ly"]:
        for sz in (16, 24):
            rel = f"Icons/places/{stem}_{sz}.png"
            if not (ROOTS[0] / rel).exists():
                save_both(rel, make_symbolic_icon("places", stem, sz))

    for stem in ["tinh_bao"]:
        for sz in (16, 24):
            rel = f"Icons/misc/{stem}_{sz}.png"
            if not (ROOTS[0] / rel).exists():
                save_both(rel, make_symbolic_icon("misc", stem, sz))

    # 5. Kit extras
    make_kit_extras()
    print("Generated and synced all HUD weather, status, action, and sundial art assets successfully.")


if __name__ == "__main__":
    main()
