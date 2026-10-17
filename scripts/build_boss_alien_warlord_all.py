import os
import math
from PIL import Image, ImageDraw

IMG1_PATH = '/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/.user_uploaded/media_1791879345199.jpg'
IMG2_PATH = '/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/.user_uploaded/media_1791879360800.jpg'
OUT_DIR = 'Assets/Art/Sprites/Characters/Enemies/BossAlienWarlord'
PREVIEW_DIR = 'scratch/warlord_preview'
os.makedirs(OUT_DIR, exist_ok=True)
os.makedirs(PREVIEW_DIR, exist_ok=True)

def flood_clean(crop):
    w, h = crop.size
    data = crop.load()
    visited = set()
    queue = []
    for x in range(w):
        queue.append((x, 0))
        queue.append((x, h - 1))
    for y in range(h):
        queue.append((0, y))
        queue.append((w - 1, y))
        
    def is_bg(r, g, b):
        return r > 218 and g > 218 and b > 218
        
    while queue:
        x, y = queue.pop(0)
        if (x, y) in visited: continue
        visited.add((x, y))
        r, g, b, a = data[x, y]
        if is_bg(r, g, b):
            data[x, y] = (0, 0, 0, 0)
            for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in visited:
                    queue.append((nx, ny))
                    
    for y in range(h):
        for x in range(w):
            r, g, b, a = data[x, y]
            if a > 0 and r > 230 and g > 230 and b > 230:
                data[x, y] = (0, 0, 0, 0)
            elif a > 0 and min(r, g, b) > 210 and (max(r, g, b) - min(r, g, b)) < 15:
                data[x, y] = (0, 0, 0, 0)
                
    bbox = crop.getbbox()
    return crop.crop(bbox) if bbox else crop

img1 = Image.open(IMG1_PATH).convert('RGB')
img2 = Image.open(IMG2_PATH).convert('RGB')

# -------------------------------------------------------------------------
# 1. BASE STATIC SPRITES
# -------------------------------------------------------------------------

# Down / Idle
crop_down = flood_clean(img1.crop((17, 332, 99, 436)).convert('RGBA'))
scale_down = 54.0 / crop_down.height
new_w_down = int(crop_down.width * scale_down)
new_h_down = 54
fg_down = crop_down.resize((new_w_down, new_h_down), Image.Resampling.LANCZOS)
canvas_down = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
paste_x_down = (64 - new_w_down) // 2
canvas_down.paste(fg_down, (paste_x_down, 5), fg_down)

# Hurt
crop_hurt = flood_clean(img1.crop((402, 325, 470, 436)).convert('RGBA'))
s_hurt = 55.0 / crop_hurt.height
nw_hurt = int(crop_hurt.width * s_hurt)
nh_hurt = 55
fg_hurt = crop_hurt.resize((nw_hurt, nh_hurt), Image.Resampling.LANCZOS)
canvas_hurt = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
px_hurt = (64 - nw_hurt) // 2
canvas_hurt.paste(fg_hurt, (px_hurt, 4), fg_hurt)

# Attack base (Frame 3: maximum crescent slash impact)
crop_atk = flood_clean(img1.crop((315, 328, 399, 436)).convert('RGBA'))
s_atk = 54.0 / crop_atk.height
nw_atk = int(crop_atk.width * s_atk)
nh_atk = 54
fg_atk = crop_atk.resize((nw_atk, nh_atk), Image.Resampling.LANCZOS)
canvas_atk = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
px_atk = (64 - nw_atk) // 2
canvas_atk.paste(fg_atk, (px_atk, 5), fg_atk)

# Die base (Frame 4: fallen on ground, completely inside margins)
crop_die = flood_clean(img1.crop((390, 498, 490, 560)).convert('RGBA'))
s_die = min(52.0 / crop_die.width, 32.0 / crop_die.height)
nw_die = int(crop_die.width * s_die)
nh_die = int(crop_die.height * s_die)
fg_die = crop_die.resize((nw_die, nh_die), Image.Resampling.LANCZOS)
canvas_die = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
px_die = (64 - nw_die) // 2
canvas_die.paste(fg_die, (px_die, 58 - nh_die), fg_die)

# Sau (Back / Up) authentic
crop_sau = flood_clean(img1.crop((245, 328, 316, 436)).convert('RGBA'))
s_sau = 54.0 / crop_sau.height
nw_sau = int(crop_sau.width * s_sau)
nh_sau = 54
fg_sau = crop_sau.resize((nw_sau, nh_sau), Image.Resampling.LANCZOS)
canvas_up = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
px_sau = (64 - nw_sau) // 2
canvas_up.paste(fg_sau, (px_sau, 5), fg_sau)
# Clean any trailing pixel on x > px_sau + nw_sau
pix_up = canvas_up.load()
for y in range(64):
    for x in range(px_sau + nw_sau, 64):
        pix_up[x, y] = (0, 0, 0, 0)

# -------------------------------------------------------------------------
# 2. SIDE WALK ANIMATION (4 FRAMES)
# -------------------------------------------------------------------------
crop_sw1 = flood_clean(img2.crop((35, 490, 140, 630)).convert('RGBA'))
crop_sw2 = flood_clean(img2.crop((185, 490, 295, 630)).convert('RGBA'))
crop_sw4 = flood_clean(img2.crop((345, 490, 460, 630)).convert('RGBA'))

target_h = 54
def make_side_canvas(fg):
    s = target_h / float(fg.height)
    nw = int(fg.width * s)
    nh = target_h
    resized = fg.resize((nw, nh), Image.Resampling.LANCZOS)
    c = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    px = (64 - nw) // 2
    c.paste(resized, (px, 5), resized)
    return c

side_f1 = make_side_canvas(crop_sw1)
side_f2 = make_side_canvas(crop_sw2)
side_f3 = make_side_canvas(crop_sw4)

# Frame 4 for side walk: passing frame with lifted right leg
side_f4 = side_f2.copy()
pix4 = side_f4.load()
pix1 = side_f1.load()
pix3 = side_f3.load()

# Clear right foot lower area at x 13-25
for x in range(13, 26):
    for y in range(52, 60):
        pix4[x, y] = (0, 0, 0, 0)
# Lifted right foot:
for x in range(22, 34):
    for y in range(48, 56):
        src = pix1[x + 8, y + 2]
        if src[3] > 0:
            pix4[x, y] = src
# Planted left foot:
for x in range(34, 45):
    for y in range(46, 59):
        src = pix3[x, y]
        if src[3] > 0:
            pix4[x, y] = src

# Side static
side_static = side_f2.copy()
right_static = side_f2.copy()
left_static = side_f2.transpose(Image.FLIP_LEFT_RIGHT)

# -------------------------------------------------------------------------
# 3. DOWN WALK ANIMATION (4 FRAMES)
# -------------------------------------------------------------------------
dw_base = canvas_down.copy()
dw_w, dw_h = 64, 64

# f1: Left foot step forward, right foot lifted, torso drops 1px
dw_f1 = Image.new('RGBA', (dw_w, dw_h), (0, 0, 0, 0))
upper_dw = dw_base.crop((0, 0, dw_w, 42))
dw_f1.paste(upper_dw, (0, 1), upper_dw)
# Left leg stepped slightly down-forward (plant)
l_leg_dw = dw_base.crop((11, 41, 30, 60))
dw_f1.paste(l_leg_dw, (11, 42), l_leg_dw)
# Center loincloth intact
center_crotch_dw = dw_base.crop((26, 40, 38, 52))
dw_f1.paste(center_crotch_dw, (26, 41), center_crotch_dw)
# Right leg lifted by 2px
r_leg_dw = dw_base.crop((34, 41, 53, 60))
dw_f1.paste(r_leg_dw, (34, 39), r_leg_dw)

# f2: Passing pose (neutral)
dw_f2 = dw_base.copy()

# f3: Right foot step forward, left foot lifted, torso drops 1px
dw_f3 = Image.new('RGBA', (dw_w, dw_h), (0, 0, 0, 0))
dw_f3.paste(upper_dw, (0, 1), upper_dw)
# Right leg stepped down-forward
dw_f3.paste(r_leg_dw, (34, 42), r_leg_dw)
# Center loincloth intact
dw_f3.paste(center_crotch_dw, (26, 41), center_crotch_dw)
# Left leg lifted by 2px
dw_f3.paste(l_leg_dw, (11, 39), l_leg_dw)

# f4: Passing pose (neutral)
dw_f4 = dw_base.copy()

for f in [dw_f1, dw_f2, dw_f3, dw_f4]:
    for x in range(dw_w):
        for y in range(60, dw_h):
            f.putpixel((x, y), (0, 0, 0, 0))

# -------------------------------------------------------------------------
# 4. UP WALK ANIMATION (4 FRAMES)
# -------------------------------------------------------------------------
uw_base = canvas_up.copy()
# f1: Left boot stepping up/forward, right foot back
uw_f1 = Image.new('RGBA', (dw_w, dw_h), (0, 0, 0, 0))
upper_uw = uw_base.crop((0, 0, dw_w, 42))
uw_f1.paste(upper_uw, (0, 1), upper_uw)
l_leg_uw = uw_base.crop((11, 41, 30, 60))
uw_f1.paste(l_leg_uw, (11, 40), l_leg_uw)
center_crotch_uw = uw_base.crop((24, 40, 40, 52))
uw_f1.paste(center_crotch_uw, (24, 41), center_crotch_uw)
r_leg_uw = uw_base.crop((34, 41, 53, 60))
uw_f1.paste(r_leg_uw, (34, 42), r_leg_uw)

uw_f2 = uw_base.copy()

# f3: Right boot stepping up/forward, left foot back
uw_f3 = Image.new('RGBA', (dw_w, dw_h), (0, 0, 0, 0))
uw_f3.paste(upper_uw, (0, 1), upper_uw)
uw_f3.paste(l_leg_uw, (11, 42), l_leg_uw)
uw_f3.paste(center_crotch_uw, (24, 41), center_crotch_uw)
uw_f3.paste(r_leg_uw, (34, 40), r_leg_uw)

uw_f4 = uw_base.copy()

for f in [uw_f1, uw_f2, uw_f3, uw_f4]:
    for x in range(dw_w):
        for y in range(60, dw_h):
            f.putpixel((x, y), (0, 0, 0, 0))

# -------------------------------------------------------------------------
# 5. ATTACK ANIMATION (4 FRAMES)
# -------------------------------------------------------------------------
atk_f3 = canvas_atk.copy()

# Frame 1: Windup (pull arm back, gather cyan/purple plasma energy)
atk_f1 = canvas_down.copy()
draw1 = ImageDraw.Draw(atk_f1)
for ox, oy, r_c, g_c, b_c in [
    (48, 30, 220, 100, 255), (49, 31, 255, 200, 255), (47, 33, 180, 80, 240),
    (51, 32, 0, 220, 255), (46, 35, 200, 120, 255), (50, 36, 255, 255, 255),
    (52, 28, 160, 60, 220), (45, 29, 0, 240, 255)
]:
    draw1.point((ox, oy), fill=(r_c, g_c, b_c, 240))

# Frame 2: Slash begins (lower arc forming)
atk_f2 = atk_f3.copy()
pix_f2 = atk_f2.load()
for y in range(5, 33):
    for x in range(35, 64):
        r, g, b, a = pix_f2[x, y]
        if a > 0 and (r > 140 and b > 160 and g < 150):
            pix_f2[x, y] = (0, 0, 0, 0)

# Frame 4: Recovery (arc dissipates into plasma motes)
atk_f4 = canvas_down.copy()
draw4 = ImageDraw.Draw(atk_f4)
for ox, oy, r_c, g_c, b_c in [
    (38, 44, 200, 100, 255), (41, 46, 255, 180, 255), (45, 42, 160, 60, 220),
    (36, 48, 0, 200, 240), (43, 49, 180, 90, 230), (48, 45, 220, 140, 255),
    (34, 45, 255, 255, 255), (49, 43, 0, 240, 255)
]:
    draw4.point((ox, oy), fill=(r_c, g_c, b_c, 200))

# -------------------------------------------------------------------------
# 6. DIE ANIMATION (4 FRAMES)
# -------------------------------------------------------------------------
die_f4 = canvas_die.copy()

# Frame 1: Recoil / shock
die_f1 = canvas_hurt.copy()

# Frame 2: Knee buckle (torso drops to floor, knees hit ground)
fg_buckle = fg_down.resize((new_w_down + 4, int(new_h_down * 0.75)), Image.Resampling.LANCZOS)
die_f2 = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
die_f2.paste(fg_buckle, ((64 - (new_w_down + 4)) // 2, 58 - int(new_h_down * 0.75)), fg_buckle)

# Frame 3: Pitching sideways / collapse
fg_collapse = fg_die.resize((nw_die, int(nh_die * 1.25)), Image.Resampling.LANCZOS)
die_f3 = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
die_f3.paste(fg_collapse, (px_die, 58 - int(nh_die * 1.25)), fg_collapse)

# -------------------------------------------------------------------------
# SAVE ALL SPRITES TO OUTPUT AND PREVIEW
# -------------------------------------------------------------------------
all_sprites = {
    # Static Directional Sprites
    'boss_alien_warlord_down.png': canvas_down,
    'boss_alien_warlord_idle.png': canvas_down,
    'boss_alien_warlord_up.png': canvas_up,
    'boss_alien_warlord_left.png': left_static,
    'boss_alien_warlord_right.png': right_static,
    'boss_alien_warlord_side.png': side_static,
    'boss_alien_warlord_hurt.png': canvas_hurt,
    'boss_alien_warlord_attack.png': atk_f3,
    'boss_alien_warlord_die.png': die_f4,

    # Down Walk (4 frames)
    'boss_alien_warlord_down_walk1.png': dw_f1,
    'boss_alien_warlord_down_walk2.png': dw_f2,
    'boss_alien_warlord_down_walk3.png': dw_f3,
    'boss_alien_warlord_down_walk4.png': dw_f4,

    # Side Walk (4 frames)
    'boss_alien_warlord_side_walk1.png': side_f1,
    'boss_alien_warlord_side_walk2.png': side_f2,
    'boss_alien_warlord_side_walk3.png': side_f3,
    'boss_alien_warlord_side_walk4.png': side_f4,

    # Up Walk (4 frames)
    'boss_alien_warlord_up_walk1.png': uw_f1,
    'boss_alien_warlord_up_walk2.png': uw_f2,
    'boss_alien_warlord_up_walk3.png': uw_f3,
    'boss_alien_warlord_up_walk4.png': uw_f4,

    # Attack (4 frames)
    'boss_alien_warlord_attack1.png': atk_f1,
    'boss_alien_warlord_attack2.png': atk_f2,
    'boss_alien_warlord_attack3.png': atk_f3,
    'boss_alien_warlord_attack4.png': atk_f4,

    # Die (4 frames)
    'boss_alien_warlord_die1.png': die_f1,
    'boss_alien_warlord_die2.png': die_f2,
    'boss_alien_warlord_die3.png': die_f3,
    'boss_alien_warlord_die4.png': die_f4,
}

print(f"Saving {len(all_sprites)} sprites to {OUT_DIR} and {PREVIEW_DIR}...")
for fname, img in all_sprites.items():
    p_out = os.path.join(OUT_DIR, fname)
    p_prev = os.path.join(PREVIEW_DIR, fname)
    img.save(p_out)
    img.save(p_prev)
    bbox = img.getbbox()
    clipped = (bbox[0] == 0 or bbox[1] == 0 or bbox[2] == 64 or bbox[3] == 64)
    print(f"  {fname:35s}: bbox={str(bbox):18s} {'[CLIPPED!]' if clipped else '[CLEAN]'}")

# Animated GIFs for visual verification
gifs = {
    'warlord_down_walk.gif': ([dw_f1, dw_f2, dw_f3, dw_f4], 160),
    'warlord_side_walk.gif': ([side_f1, side_f2, side_f3, side_f4], 160),
    'warlord_up_walk.gif': ([uw_f1, uw_f2, uw_f3, uw_f4], 160),
    'warlord_attack.gif': ([atk_f1, atk_f2, atk_f3, atk_f4], 150),
    'warlord_die.gif': ([die_f1, die_f2, die_f3, die_f4], 200),
}
for gname, (frames, dur) in gifs.items():
    frames[0].save(os.path.join(PREVIEW_DIR, gname), save_all=True, append_images=frames[1:], duration=dur, loop=0)

print("\n[SUCCESS] Generated all 25 Boss Alien Warlord sprites and 5 animated GIFs!")
