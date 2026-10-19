#!/usr/bin/env python3
"""
fix_player_animations.py
Fixes Player (Farmer Khoa) animations:
1. Down Walk & Down Idle: eliminates backpack flashing in no-backpack mode and ensures consistency in backpack mode.
2. Up Walk & Up Idle: fixes 2px foot jitter, aligns spine/head center, and cleans outer compression fringes.
3. Happy Emote: rebuilds 4-frame front-facing joyful celebration with victory fist pump and sparkle stars.
4. Action Sprite: sets clean front-facing pose instead of backwards view.
"""

import os
from collections import deque
from PIL import Image

SRC_SHEET_PATH = "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/.user_uploaded/media_1791560585771.jpg"
SRC_ACTION_PATH = "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/.user_uploaded/media_1791575548208.png"

PLAYER_DIR = "Assets/Art/Sprites/Characters/Player"
BACKPACK_DIR = "Assets/Art/Sprites/Characters/PlayerBackpack"
INSPECT_DIR = "/home/danh/.gemini/antigravity/scratch/inspect"

os.makedirs(PLAYER_DIR, exist_ok=True)
os.makedirs(BACKPACK_DIR, exist_ok=True)
os.makedirs(INSPECT_DIR, exist_ok=True)

# Colors
GOLD = (255, 220, 50, 255)
GOLD_LIGHT = (255, 245, 150, 255)
GOLD_DARK = (210, 160, 20, 255)
WHITE = (255, 255, 255, 255)
JACKET_MAIN = (68, 79, 74, 255)
JACKET_DARK = (48, 56, 52, 255)
JACKET_LIGHT = (85, 98, 92, 255)


def clean_bg(crop_img, thresh=235):
    img = crop_img.convert("RGBA")
    w, h = img.size
    pix = img.load()
    visited = [[False] * h for _ in range(w)]
    q = deque()

    for x in range(w):
        for y in (0, h - 1):
            if (pix[x, y][0] + pix[x, y][1] + pix[x, y][2]) / 3 >= thresh:
                visited[x][y] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if not visited[x][y] and (pix[x, y][0] + pix[x, y][1] + pix[x, y][2]) / 3 >= thresh:
                visited[x][y] = True
                q.append((x, y))

    while q:
        cx, cy = q.popleft()
        pix[cx, cy] = (0, 0, 0, 0)
        for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[nx][ny]:
                if (pix[nx, ny][0] + pix[nx, ny][1] + pix[nx, ny][2]) / 3 >= thresh:
                    visited[nx][ny] = True
                    q.append((nx, ny))

    # Clean outer edge compression fringe
    for x in range(w):
        for y in range(h):
            if pix[x, y][3] > 0:
                is_edge = any(
                    0 <= x + dx < w and 0 <= y + dy < h and pix[x + dx, y + dy][3] == 0
                    for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]
                )
                if is_edge:
                    br = (pix[x, y][0] + pix[x, y][1] + pix[x, y][2]) / 3.0
                    if br >= 225:
                        pix[x, y] = (0, 0, 0, 0)
    return img


def shift_vert(im, dy):
    res = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    p_src = im.load()
    p_res = res.load()
    for x in range(64):
        for y in range(64):
            ny = y + dy
            if 0 <= ny < 64:
                p_res[x, ny] = p_src[x, y]
    return res


def add_sparkle(im, cx, cy, sz=1):
    pix = im.load()
    if 0 <= cx < 64 and 0 <= cy < 64:
        pix[cx, cy] = WHITE
    for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
        if 0 <= cx + dx < 64 and 0 <= cy + dy < 64:
            pix[cx + dx, cy + dy] = GOLD_LIGHT
    if sz >= 2:
        for dx, dy in [(-2, 0), (2, 0), (0, -2), (0, 2)]:
            if 0 <= cx + dx < 64 and 0 <= cy + dy < 64:
                pix[cx + dx, cy + dy] = GOLD
        for dx, dy in [(-1, -1), (1, -1), (-1, 1), (1, 1)]:
            if 0 <= cx + dx < 64 and 0 <= cy + dy < 64:
                pix[cx + dx, cy + dy] = GOLD_DARK


# -------------------------------------------------------------
# 1. PROCESS DOWN DIRECTION
# -------------------------------------------------------------
print("Processing Down Direction Sprites...")
src = Image.open(SRC_SHEET_PATH).convert("RGB")

# Row 0: y=533..632
y0_down, y1_down = 533, 632
cols_down = [
    (119, 157),  # 0: walk left (nobp)
    (187, 228),  # 1: back view (ignored for front)
    (259, 297),  # 2: walk right (nobp)
    (415, 453),  # 3: walk left (bp)
    (484, 524),  # 4: stand front (bp)
    (555, 595),  # 5: walk right (bp)
]

scale = 54.0 / 99.0


def extract_down_raw(col_range):
    crop = src.crop((col_range[0], y0_down, col_range[1], y1_down))
    cleaned = clean_bg(crop)
    sw = max(1, int(round(cleaned.width * scale)))
    sh = max(1, int(round(cleaned.height * scale)))
    scaled = cleaned.resize((sw, sh), Image.LANCZOS)
    canvas = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    canvas.paste(scaled, ((64 - sw) // 2, 6), scaled)
    return canvas


down_nobp_walk1 = extract_down_raw(cols_down[0])
down_nobp_walk3 = extract_down_raw(cols_down[2])

down_bp_walk1 = extract_down_raw(cols_down[3])
down_bp_stand = extract_down_raw(cols_down[4])
down_bp_walk3 = extract_down_raw(cols_down[5])

# Create clean stand pose without straps from down_bp_stand:
down_nobp_stand = down_bp_stand.copy()
pix_s = down_bp_stand.load()
pix_clean = down_nobp_stand.load()

for y in range(23, 40):
    for x in range(21, 43):
        r, g, b, a = pix_s[x, y]
        if a > 0:
            is_skin = r > 150 and g > 110 and b > 70 and (r - b) > 30
            is_tshirt = r < 50 and g < 50 and b < 50 and 28 <= x <= 34
            if not is_skin and not is_tshirt:
                if abs(r - g) <= 12 and abs(g - b) <= 12 and r >= 65:
                    if y <= 25:
                        pix_clean[x, y] = JACKET_LIGHT
                    elif y <= 32:
                        pix_clean[x, y] = JACKET_MAIN
                    else:
                        pix_clean[x, y] = JACKET_DARK

for y in range(23, 30):
    for x in [21, 22, 40, 41]:
        r, g, b, a = pix_s[x, y]
        if a > 0 and abs(r - g) <= 10 and abs(g - b) <= 10 and r > 75:
            if x <= 21 or x >= 41:
                pix_clean[x, y] = (0, 0, 0, 0)
            else:
                pix_clean[x, y] = JACKET_MAIN

# Save Down Sprites - Player (No Backpack)
down_nobp_walk1.save(os.path.join(PLAYER_DIR, "player_down_walk1.png"))
down_nobp_stand.save(os.path.join(PLAYER_DIR, "player_down_walk2.png"))
down_nobp_walk3.save(os.path.join(PLAYER_DIR, "player_down_walk3.png"))
down_nobp_stand.save(os.path.join(PLAYER_DIR, "player_down_walk4.png"))

down_nobp_stand.save(os.path.join(PLAYER_DIR, "player_down_idle.png"))
for i in range(1, 5):
    down_nobp_stand.save(os.path.join(PLAYER_DIR, f"player_down_idle_{i}.png"))

# Save Down Sprites - PlayerBackpack
down_bp_walk1.save(os.path.join(BACKPACK_DIR, "player_backpack_down_walk1.png"))
down_bp_stand.save(os.path.join(BACKPACK_DIR, "player_backpack_down_walk2.png"))
down_bp_walk3.save(os.path.join(BACKPACK_DIR, "player_backpack_down_walk3.png"))
down_bp_stand.save(os.path.join(BACKPACK_DIR, "player_backpack_down_walk4.png"))

down_bp_stand.save(os.path.join(BACKPACK_DIR, "player_backpack_down_idle.png"))
for i in range(1, 5):
    down_bp_stand.save(os.path.join(BACKPACK_DIR, f"player_backpack_down_idle_{i}.png"))

print("Down sprites saved successfully.")

# -------------------------------------------------------------
# 2. PROCESS UP DIRECTION (GROUNDED & ALIGNED)
# -------------------------------------------------------------
print("Processing Up Direction Sprites...")
# Row 1: y=645..750
cols_up = [
    (118, 160),  # 0: walk left (nobp)
    (185, 230),  # 1: stand (nobp)
    (258, 300),  # 2: walk right (nobp)
    (412, 455),  # 3: walk left (bp)
    (482, 526),  # 4: stand (bp)
    (554, 597),  # 5: walk right (bp)
]


def extract_up_grounded(col_range, is_stand=False):
    crop = src.crop((col_range[0], 645, col_range[1], 750)).convert("RGBA")
    pix = crop.load()
    w, h = crop.size
    for y in range(h):
        for x in range(w):
            if (pix[x, y][0] + pix[x, y][1] + pix[x, y][2]) / 3 > 232:
                pix[x, y] = (0, 0, 0, 0)

    # Fringe cleanup
    for y in range(h):
        for x in range(w):
            if pix[x, y][3] > 0:
                is_edge = any(
                    0 <= x + dx < w and 0 <= y + dy < h and pix[x + dx, y + dy][3] == 0
                    for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]
                )
                if is_edge and (pix[x, y][0] + pix[x, y][1] + pix[x, y][2]) / 3 > 215:
                    pix[x, y] = (0, 0, 0, 0)

    bbox = crop.getbbox()
    c_img = crop.crop(bbox)

    # Standard scale based on stand height = 52px
    scale_factor = 52.0 / 94.0
    sw = int(round(c_img.width * scale_factor))
    sh = int(round(c_img.height * scale_factor))
    scaled = c_img.resize((sw, sh), Image.LANCZOS)

    canvas = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    bottom_y = 59
    top_y = bottom_y - sh + 1
    left_x = 32 - sw // 2
    canvas.paste(scaled, (left_x, top_y), scaled)

    if is_stand:
        p_canvas = canvas.load()
        # Clean ear fringe & halo artifacts at y=12..13, x=41..42
        for y in [12, 13]:
            p_canvas[41, y] = (0, 0, 0, 0)
            p_canvas[42, y] = (0, 0, 0, 0)
        p_canvas[22, 21] = (0, 0, 0, 0)
        p_canvas[41, 21] = (0, 0, 0, 0)
        p_canvas[22, 22] = (0, 0, 0, 0)

    return canvas


up_nobp_walk1 = extract_up_grounded(cols_up[0])
up_nobp_stand = extract_up_grounded(cols_up[1], is_stand=True)
up_nobp_walk3 = extract_up_grounded(cols_up[2])

up_bp_walk1 = extract_up_grounded(cols_up[3])
up_bp_stand = extract_up_grounded(cols_up[4], is_stand=True)
up_bp_walk3 = extract_up_grounded(cols_up[5])

# Save Up Sprites - Player (No Backpack)
up_nobp_walk1.save(os.path.join(PLAYER_DIR, "player_up_walk1.png"))
up_nobp_stand.save(os.path.join(PLAYER_DIR, "player_up_walk2.png"))
up_nobp_walk3.save(os.path.join(PLAYER_DIR, "player_up_walk3.png"))
up_nobp_stand.save(os.path.join(PLAYER_DIR, "player_up_walk4.png"))

up_nobp_stand.save(os.path.join(PLAYER_DIR, "player_up_idle.png"))
for i in range(1, 5):
    up_nobp_stand.save(os.path.join(PLAYER_DIR, f"player_up_idle_{i}.png"))

# Save Up Sprites - PlayerBackpack
up_bp_walk1.save(os.path.join(BACKPACK_DIR, "player_backpack_up_walk1.png"))
up_bp_stand.save(os.path.join(BACKPACK_DIR, "player_backpack_up_walk2.png"))
up_bp_walk3.save(os.path.join(BACKPACK_DIR, "player_backpack_up_walk3.png"))
up_bp_stand.save(os.path.join(BACKPACK_DIR, "player_backpack_up_walk4.png"))

up_bp_stand.save(os.path.join(BACKPACK_DIR, "player_backpack_up_idle.png"))
for i in range(1, 5):
    up_bp_stand.save(os.path.join(BACKPACK_DIR, f"player_backpack_up_idle_{i}.png"))

print("Up sprites saved successfully.")

# -------------------------------------------------------------
# 3. ACTION SPRITE (FRONT FACING)
# -------------------------------------------------------------
down_nobp_stand.save(os.path.join(PLAYER_DIR, "player_action.png"))
print("Saved player_action.png.")

# -------------------------------------------------------------
# 4. HAPPY EMOTE (FRONT-FACING CELEBRATION WITH SPARKLES)
# -------------------------------------------------------------
print("Building Happy Emote Animation...")
# Load action victory cheer sprite from media_1791575548208.png
src_action = Image.open(SRC_ACTION_PATH).convert("RGBA")
# Crop fig0 (fist pump pose)
c_fig0 = src_action.crop((50, 20, 250, 220))
pix_act = c_fig0.load()
for y in range(c_fig0.height):
    for x in range(c_fig0.width):
        if (pix_act[x, y][0] + pix_act[x, y][1] + pix_act[x, y][2]) / 3 > 235:
            pix_act[x, y] = (0, 0, 0, 0)

b_cheer = c_fig0.crop(c_fig0.getbbox())
scale_cheer = 52.0 / 157.0
cheer_w = int(round(b_cheer.width * scale_cheer))
cheer_h = int(round(b_cheer.height * scale_cheer))
scaled_cheer = b_cheer.resize((cheer_w, cheer_h), Image.LANCZOS)

cheer_base = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
# Feet grounded at y=59
cheer_base.paste(scaled_cheer, (32 - cheer_w // 2 - 2, 59 - cheer_h + 1), scaled_cheer)

# Happy Frame 1: Clean front stand (joyful anticipation)
happy_1 = down_nobp_stand.copy()

# Happy Frame 2: Hop +2px, victory fist pump, golden star sparkle over fist
happy_2 = shift_vert(cheer_base, -2)
add_sparkle(happy_2, 16, 12, 1)

# Happy Frame 3: Peak hop +3px, fist pumped high, glowing sparkles
happy_3 = shift_vert(cheer_base, -3)
add_sparkle(happy_3, 16, 11, 2)
add_sparkle(happy_3, 48, 14, 1)

# Happy Frame 4: Land gently (+1px), fist lowering, subtle sparkle
happy_4 = shift_vert(cheer_base, -1)
add_sparkle(happy_4, 48, 15, 1)

happy_frames = [happy_1, happy_2, happy_3, happy_4]
for i, f in enumerate(happy_frames):
    f.save(os.path.join(PLAYER_DIR, f"player_happy_{i+1}.png"))

print("Happy emote sprites saved successfully.")

# -------------------------------------------------------------
# 5. GENERATE VERIFICATION ANIMATED GIFS & INSPECTION CHECKS
# -------------------------------------------------------------
print("Generating verification animated GIFs...")

down_walk_cycle = [down_nobp_walk1, down_nobp_stand, down_nobp_walk3, down_nobp_stand]
down_walk_cycle[0].save(
    os.path.join(INSPECT_DIR, "verified_down_walk.gif"),
    save_all=True,
    append_images=down_walk_cycle[1:],
    duration=133,
    loop=0,
    disposal=2,
)

up_walk_cycle = [up_nobp_walk1, up_nobp_stand, up_nobp_walk3, up_nobp_stand]
up_walk_cycle[0].save(
    os.path.join(INSPECT_DIR, "verified_up_walk.gif"),
    save_all=True,
    append_images=up_walk_cycle[1:],
    duration=133,
    loop=0,
    disposal=2,
)

bp_down_walk_cycle = [down_bp_walk1, down_bp_stand, down_bp_walk3, down_bp_stand]
bp_down_walk_cycle[0].save(
    os.path.join(INSPECT_DIR, "verified_bp_down_walk.gif"),
    save_all=True,
    append_images=bp_down_walk_cycle[1:],
    duration=133,
    loop=0,
    disposal=2,
)

bp_up_walk_cycle = [up_bp_walk1, up_bp_stand, up_bp_walk3, up_bp_stand]
bp_up_walk_cycle[0].save(
    os.path.join(INSPECT_DIR, "verified_bp_up_walk.gif"),
    save_all=True,
    append_images=bp_up_walk_cycle[1:],
    duration=133,
    loop=0,
    disposal=2,
)

happy_frames[0].save(
    os.path.join(INSPECT_DIR, "verified_happy.gif"),
    save_all=True,
    append_images=happy_frames[1:],
    duration=166,
    loop=0,
    disposal=2,
)

print("--- BOUNDING BOX VERIFICATION ---")
sprites_to_check = [
    ("player_down_walk1", down_nobp_walk1),
    ("player_down_walk2", down_nobp_stand),
    ("player_down_walk3", down_nobp_walk3),
    ("player_up_walk1", up_nobp_walk1),
    ("player_up_walk2", up_nobp_stand),
    ("player_up_walk3", up_nobp_walk3),
    ("player_happy_1", happy_1),
    ("player_happy_2", happy_2),
    ("player_happy_3", happy_3),
    ("player_happy_4", happy_4),
    ("bp_down_walk1", down_bp_walk1),
    ("bp_down_walk2", down_bp_stand),
    ("bp_up_walk1", up_bp_walk1),
    ("bp_up_walk2", up_bp_stand),
]

for name, im in sprites_to_check:
    bbox = im.getbbox()
    min_x, min_y, max_x, max_y = bbox
    pad_l, pad_t = min_x, min_y
    pad_r, pad_b = 64 - max_x, 64 - max_y
    print(f"{name:20}: bbox={bbox}, pads=(L:{pad_l}, T:{pad_t}, R:{pad_r}, B:{pad_b})")

print("All tasks completed successfully!")
