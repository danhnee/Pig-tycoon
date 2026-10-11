import os
from PIL import Image, ImageDraw

ENV_DIR = "Assets/Art/Sprites/Environment"
os.makedirs(ENV_DIR, exist_ok=True)

# Palettes
C_POST = (139, 90, 43, 255)
C_POST_HI = (175, 120, 68, 255)
C_POST_SH = (86, 53, 22, 255)
C_POST_TOP = (160, 108, 55, 255)

C_RAIL = (150, 98, 48, 255)
C_RAIL_HI = (185, 130, 75, 255)
C_RAIL_SH = (95, 58, 26, 255)

C_NAIL = (40, 40, 45, 255)
C_NAIL_HI = (190, 195, 200, 255)
C_BAND = (55, 58, 62, 255)

def create_fence(n: bool, e: bool, s: bool, w: bool) -> Image.Image:
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Background Horizontal Rails
    if w:
        # Top rail west
        draw.rectangle([0, 7, 13, 12], fill=C_RAIL)
        draw.line([(0, 7), (13, 7)], fill=C_RAIL_HI)
        draw.line([(0, 12), (13, 12)], fill=C_RAIL_SH)
        # Bottom rail west
        draw.rectangle([0, 18, 13, 23], fill=C_RAIL)
        draw.line([(0, 18), (13, 18)], fill=C_RAIL_HI)
        draw.line([(0, 23), (13, 23)], fill=C_RAIL_SH)

    if e:
        # Top rail east
        draw.rectangle([18, 7, 31, 12], fill=C_RAIL)
        draw.line([(18, 7), (31, 7)], fill=C_RAIL_HI)
        draw.line([(18, 12), (31, 12)], fill=C_RAIL_SH)
        # Bottom rail east
        draw.rectangle([18, 18, 31, 23], fill=C_RAIL)
        draw.line([(18, 18), (31, 18)], fill=C_RAIL_HI)
        draw.line([(18, 23), (31, 23)], fill=C_RAIL_SH)

    # If both E and W connected without vertical, rails cross center post
    if e and w and not n and not s:
        draw.rectangle([13, 7, 18, 12], fill=C_RAIL)
        draw.line([(13, 7), (18, 7)], fill=C_RAIL_HI)
        draw.line([(13, 12), (18, 12)], fill=C_RAIL_SH)
        draw.rectangle([13, 18, 18, 23], fill=C_RAIL)
        draw.line([(13, 18), (18, 18)], fill=C_RAIL_HI)
        draw.line([(13, 23), (18, 23)], fill=C_RAIL_SH)

    # 2. Main Vertical Post (consistent x1=12, x2=19)
    y_start = 0 if n else 4
    y_end = 31 if s else 28

    x1, x2 = 12, 19
    draw.rectangle([x1, y_start, x2, y_end], fill=C_POST)
    draw.line([(x1, y_start), (x1, y_end)], fill=C_POST_HI)
    draw.line([(x2, y_start), (x2, y_end)], fill=C_POST_SH)

    # Pointed wooden cap if not connecting North
    if not n:
        draw.polygon([(x1, 4), (15, 1), (16, 1), (x2, 4)], fill=C_POST_TOP)
        draw.line([(x1, 4), (15, 1)], fill=C_RAIL_HI)
        draw.line([(x2, 4), (16, 1)], fill=C_POST_SH)

    # Bottom ground shading if not connecting South
    if not s:
        draw.line([(x1, 28), (x2, 28)], fill=C_POST_SH)

    # Details: Nails or Iron band
    if e or w:
        if e and w:
            draw.point((15, 9), fill=C_NAIL)
            draw.point((15, 20), fill=C_NAIL)
            draw.point((16, 10), fill=C_NAIL_HI)
            draw.point((16, 21), fill=C_NAIL_HI)
        elif w:
            draw.point((14, 9), fill=C_NAIL)
            draw.point((14, 20), fill=C_NAIL)
            draw.point((15, 10), fill=C_NAIL_HI)
            draw.point((15, 21), fill=C_NAIL_HI)
        elif e:
            draw.point((17, 9), fill=C_NAIL)
            draw.point((17, 20), fill=C_NAIL)
            draw.point((18, 10), fill=C_NAIL_HI)
            draw.point((18, 21), fill=C_NAIL_HI)
    else:
        # Band on vertical or isolated post
        draw.rectangle([x1, 14, x2, 16], fill=C_BAND)
        draw.point((15, 15), fill=C_NAIL_HI)

    return img

SPRITE_NAMES = {
    0:  "fence_post.png",       # isolated
    1:  "fence_end_n.png",      # N
    2:  "fence_end_e.png",      # E
    3:  "fence_corner_ne.png",  # N+E
    4:  "fence_end_s.png",      # S
    5:  "fence_v.png",          # N+S (straight vertical)
    6:  "fence_corner_se.png",  # E+S
    7:  "fence_t_east.png",     # N+E+S
    8:  "fence_end_w.png",      # W
    9:  "fence_corner_nw.png",  # N+W
    10: "fence_h.png",          # E+W (straight horizontal)
    11: "fence_t_north.png",    # N+E+W
    12: "fence_corner_sw.png",  # S+W
    13: "fence_t_west.png",     # N+S+W
    14: "fence_t_south.png",    # E+S+W
    15: "fence_cross.png",      # N+E+S+W
}

for mask, fname in SPRITE_NAMES.items():
    n = bool(mask & 1)
    e = bool(mask & 2)
    s = bool(mask & 4)
    w = bool(mask & 8)
    im = create_fence(n, e, s, w)
    out_path = os.path.join(ENV_DIR, fname)
    im.save(out_path)

print("Regenerated all 16 seamless fence sprites.")
