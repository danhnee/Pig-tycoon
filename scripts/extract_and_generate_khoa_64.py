import os
import math
from collections import deque
from PIL import Image, ImageDraw

SOURCE_IMAGE_PATH = "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/.user_uploaded/media_1791560585771.jpg"
OUT_DIR = "Assets/Art/Sprites/Characters"
os.makedirs(OUT_DIR, exist_ok=True)

# -------------------------------------------------------------
# 1. FLOOD-FILL BACKGROUND REMOVAL & CLEAN CUTOUT
# -------------------------------------------------------------
def clean_bg(crop_img, bg_thresh=238, edge_fade_thresh=228):
    """
    Flood fills from the 4 outer borders to remove white/off-white background.
    Preserves all internal white/light pixels inside the character.
    Softens anti-aliased compression fringe along the outer boundary.
    """
    img = crop_img.convert('RGBA')
    w, h = img.size
    pix = img.load()
    visited = [[False]*h for _ in range(w)]
    q = deque()

    # Seed from borders
    for x in range(w):
        for y in (0, h-1):
            r, g, b, a = pix[x, y]
            if (r + g + b) / 3 >= bg_thresh:
                visited[x][y] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w-1):
            if not visited[x][y]:
                r, g, b, a = pix[x, y]
                if (r + g + b) / 3 >= bg_thresh:
                    visited[x][y] = True
                    q.append((x, y))

    while q:
        cx, cy = q.popleft()
        pix[cx, cy] = (0, 0, 0, 0)
        for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[nx][ny]:
                r, g, b, a = pix[nx, ny]
                if (r + g + b) / 3 >= bg_thresh:
                    visited[nx][ny] = True
                    q.append((nx, ny))

    # Outer fringe cleanup
    for x in range(w):
        for y in range(h):
            if pix[x, y][3] > 0:
                is_outer = any(
                    0 <= x+dx < w and 0 <= y+dy < h and pix[x+dx, y+dy][3] == 0
                    for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]
                )
                if is_outer:
                    r, g, b, _ = pix[x, y]
                    br = (r + g + b) / 3.0
                    if br >= edge_fade_thresh:
                        pix[x, y] = (0, 0, 0, 0)
                    elif br >= 195:
                        alpha = int(255 * (edge_fade_thresh - br) / (edge_fade_thresh - 195))
                        pix[x, y] = (r, g, b, min(pix[x, y][3], alpha))
    return img

# -------------------------------------------------------------
# 2. FIT SPRITE ONTO 64x64 CANVAS WITH GROUND ANCHORING
# -------------------------------------------------------------
# Ground baseline is placed at feet_y = 60
# Target height is 52 pixels (leaving 8px headroom and 4px footroom)
TARGET_HEIGHT = 52
FEET_Y = 60

def place_on_64(img, raw_y0, raw_y1, band_ground_y, band_top_y, center_offset_x=0):
    """
    Places sprite on a 64x64 RGBA canvas.
    Ground anchoring: the band ground line maps directly to FEET_Y on canvas.
    Width is centered at 32 + center_offset_x.
    """
    w, h = img.size
    # Unified scale factor across the band to preserve relative limb motion
    band_h = band_ground_y - band_top_y
    scale = TARGET_HEIGHT / float(band_h)
    
    scaled_w = max(1, int(w * scale))
    scaled_h = max(1, int(h * scale))
    scaled = img.resize((scaled_w, scaled_h), Image.LANCZOS)
    
    canvas = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    
    # Calculate top_y based on distance of sprite bottom from band ground line
    dist_from_ground = band_ground_y - raw_y1
    bottom_y = FEET_Y - int(dist_from_ground * scale)
    top_y = bottom_y - scaled_h
    
    left_x = (64 - scaled_w) // 2 + center_offset_x
    canvas.paste(scaled, (left_x, top_y), scaled)
    return canvas

# -------------------------------------------------------------
# 3. GENERATE 4-FRAME ORGANIC IDLE (BREATHING & BLINKING)
# -------------------------------------------------------------
def generate_idle_frames(base_stand_64, facing='down'):
    """
    Creates 4 frames of living idle animation:
    Frame 1: Base stance
    Frame 2: Inhale (upper body rises 1px, shoulders expand subtly)
    Frame 3: Peak breath & natural eye blink (eyes close 1px for 1 frame)
    Frame 4: Exhale relaxation settling back to ground
    """
    f1 = base_stand_64.copy()
    
    # Frame 2: Inhale
    f2 = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    p_base = base_stand_64.load()
    p_f2 = f2.load()
    waist_y = 38
    for x in range(64):
        for y in range(64):
            if y < waist_y:
                if y + 1 < 64:
                    p_f2[x, y] = p_base[x, y + 1]
            elif y == waist_y:
                p_f2[x, y] = p_base[x, y]
            else:
                p_f2[x, y] = p_base[x, y]
                
    # Frame 3: Peak breath + subtle blink
    f3 = f2.copy()
    p_f3 = f3.load()
    if facing == 'down':
        # Dark eye pupils around x=28..35, y=19..24
        for x in range(27, 37):
            for y in range(19, 25):
                r, g, b, a = p_f3[x, y]
                if a > 150 and r < 75 and g < 65 and b < 55:
                    p_f3[x, y] = (225, 175, 140, a) # skin eyelid
    elif facing == 'side':
        for x in range(26, 38):
            for y in range(19, 25):
                r, g, b, a = p_f3[x, y]
                if a > 150 and r < 75 and g < 65 and b < 55:
                    p_f3[x, y] = (225, 175, 140, a)
                    
    # Frame 4: Exhale (transition back)
    f4 = base_stand_64.copy()
    
    return [f1, f2, f3, f4]

# -------------------------------------------------------------
# 4. EXTRACTION FROM media_1791560585771.jpg
# -------------------------------------------------------------
print("Loading source image:", SOURCE_IMAGE_PATH)
src_img = Image.open(SOURCE_IMAGE_PATH).convert('RGB')
src_pix = src_img.load()

# Exact sprite coordinates from analysis:
# Row 0: Down (y=534..634, baseline=631, top=534)
# Row 1: Up   (y=648..749, baseline=746, top=650)
# Row 2: Side (y=760..863, baseline=860, top=762)
# Row 3: SideAlt (y=874..977, baseline=974, top=876)

bands_data = {
    'down': {
        'top_y': 534, 'ground_y': 631,
        'nobp': [(119, 535, 156, 631), (187, 534, 227, 627), (259, 535, 296, 631)],
        'bp':   [(415, 538, 452, 633), (484, 535, 523, 629), (555, 538, 594, 634)]
    },
    'up': {
        'top_y': 650, 'ground_y': 746,
        'nobp': [(121, 650, 158, 745), (187, 650, 226, 741), (260, 650, 297, 746)],
        'bp':   [(415, 652, 452, 748), (484, 651, 523, 746), (557, 652, 594, 748)]
    },
    'side': {
        'top_y': 762, 'ground_y': 860,
        'nobp_r2': [(115, 762, 164, 860), (188, 762, 224, 857), (256, 764, 304, 860)],
        'nobp_r3': [(114, 878, 162, 974), (194, 876, 230, 971), (254, 876, 304, 974)],
        'bp_r2':   [(411, 764, 461, 862), (484, 764, 528, 860), (552, 765, 600, 862)],
        'bp_r3':   [(410, 879, 459, 976), (482, 878, 527, 974), (550, 878, 601, 976)]
    }
}

def extract_and_fit(coords, top_y, ground_y, offset_x=0):
    x0, y0, x1, y1 = coords
    crop = src_img.crop((x0, y0, x1 + 1, y1 + 1))
    cleaned = clean_bg(crop)
    return place_on_64(cleaned, y0, y1, ground_y, top_y, offset_x)

print("Extracting base character sprites...")

# Down Direction
d_data = bands_data['down']
down_walk_left  = extract_and_fit(d_data['nobp'][0], d_data['top_y'], d_data['ground_y'])
down_stand      = extract_and_fit(d_data['nobp'][1], d_data['top_y'], d_data['ground_y'])
down_walk_right = extract_and_fit(d_data['nobp'][2], d_data['top_y'], d_data['ground_y'])

down_idle_frames = generate_idle_frames(down_stand, 'down')
for i, fr in enumerate(down_idle_frames):
    fr.save(os.path.join(OUT_DIR, f"player_down_idle_{i+1}.png"))
down_stand.save(os.path.join(OUT_DIR, "player_down_idle.png"))

# 4-frame walk cycle: Left -> Stand -> Right -> Stand
down_walk_frames = [down_walk_left, down_stand, down_walk_right, down_stand]
for i, fr in enumerate(down_walk_frames):
    fr.save(os.path.join(OUT_DIR, f"player_down_walk{i+1}.png"))

# Up Direction
u_data = bands_data['up']
up_walk_left  = extract_and_fit(u_data['nobp'][0], u_data['top_y'], u_data['ground_y'])
up_stand      = extract_and_fit(u_data['nobp'][1], u_data['top_y'], u_data['ground_y'])
up_walk_right = extract_and_fit(u_data['nobp'][2], u_data['top_y'], u_data['ground_y'])

up_idle_frames = generate_idle_frames(up_stand, 'up')
for i, fr in enumerate(up_idle_frames):
    fr.save(os.path.join(OUT_DIR, f"player_up_idle_{i+1}.png"))
up_stand.save(os.path.join(OUT_DIR, "player_up_idle.png"))

up_walk_frames = [up_walk_left, up_stand, up_walk_right, up_stand]
for i, fr in enumerate(up_walk_frames):
    fr.save(os.path.join(OUT_DIR, f"player_up_walk{i+1}.png"))

# Side Direction
s_data = bands_data['side']
side_contact_fwd  = extract_and_fit(s_data['nobp_r2'][0], 762, 860)
side_stand        = extract_and_fit(s_data['nobp_r2'][1], 762, 860)
side_contact_back = extract_and_fit(s_data['nobp_r2'][2], 762, 860)
side_stride_pass  = extract_and_fit(s_data['nobp_r3'][0], 874, 974)

side_idle_frames = generate_idle_frames(side_stand, 'side')
for i, fr in enumerate(side_idle_frames):
    fr.save(os.path.join(OUT_DIR, f"player_side_idle_{i+1}.png"))
side_stand.save(os.path.join(OUT_DIR, "player_side_idle.png"))

# 4-frame walk cycle: Contact Forward -> Passing Stand -> Contact Back -> Stride Recovery
side_walk_frames = [side_contact_fwd, side_stand, side_contact_back, side_stride_pass]
for i, fr in enumerate(side_walk_frames):
    fr.save(os.path.join(OUT_DIR, f"player_side_walk{i+1}.png"))

# Also extract Backpack variants
bp_down_walk_left  = extract_and_fit(d_data['bp'][0], d_data['top_y'], d_data['ground_y'])
bp_down_stand      = extract_and_fit(d_data['bp'][1], d_data['top_y'], d_data['ground_y'])
bp_down_walk_right = extract_and_fit(d_data['bp'][2], d_data['top_y'], d_data['ground_y'])
bp_down_idle_frames = generate_idle_frames(bp_down_stand, 'down')
for i, fr in enumerate(bp_down_idle_frames):
    fr.save(os.path.join(OUT_DIR, f"player_backpack_down_idle_{i+1}.png"))
bp_down_walk_frames = [bp_down_walk_left, bp_down_stand, bp_down_walk_right, bp_down_stand]
for i, fr in enumerate(bp_down_walk_frames):
    fr.save(os.path.join(OUT_DIR, f"player_backpack_down_walk{i+1}.png"))

bp_up_walk_left  = extract_and_fit(u_data['bp'][0], u_data['top_y'], u_data['ground_y'])
bp_up_stand      = extract_and_fit(u_data['bp'][1], u_data['top_y'], u_data['ground_y'])
bp_up_walk_right = extract_and_fit(u_data['bp'][2], u_data['top_y'], u_data['ground_y'])
bp_up_idle_frames = generate_idle_frames(bp_up_stand, 'up')
for i, fr in enumerate(bp_up_idle_frames):
    fr.save(os.path.join(OUT_DIR, f"player_backpack_up_idle_{i+1}.png"))
bp_up_walk_frames = [bp_up_walk_left, bp_up_stand, bp_up_walk_right, bp_up_stand]
for i, fr in enumerate(bp_up_walk_frames):
    fr.save(os.path.join(OUT_DIR, f"player_backpack_up_walk{i+1}.png"))

bp_side_contact_fwd  = extract_and_fit(s_data['bp_r2'][0], 762, 860)
bp_side_stand        = extract_and_fit(s_data['bp_r2'][1], 762, 860)
bp_side_contact_back = extract_and_fit(s_data['bp_r2'][2], 762, 860)
bp_side_stride_pass  = extract_and_fit(s_data['bp_r3'][0], 874, 974)
bp_side_idle_frames = generate_idle_frames(bp_side_stand, 'side')
for i, fr in enumerate(bp_side_idle_frames):
    fr.save(os.path.join(OUT_DIR, f"player_backpack_side_idle_{i+1}.png"))
bp_side_walk_frames = [bp_side_contact_fwd, bp_side_stand, bp_side_contact_back, bp_side_stride_pass]
for i, fr in enumerate(bp_side_walk_frames):
    fr.save(os.path.join(OUT_DIR, f"player_backpack_side_walk{i+1}.png"))

print("Idle, Walk, and Backpack variants extracted successfully!")

# -------------------------------------------------------------
# 5. GENERATE ACTION & HAPPY EMOTES (BASED ON KHOA'S DESIGN)
# -------------------------------------------------------------
# Action Sprite: Khoa reaches forward to feed pigs / fix fences
action_img = down_stand.copy()
draw = ImageDraw.Draw(action_img)
# Khoa's right arm extends forward with a small feed bowl/trough
feed_bowl = [(38, 38), (44, 38), (43, 42), (39, 42)]
draw.polygon(feed_bowl, fill=(160, 120, 80, 255), outline=(90, 60, 30, 255))
# Golden grain seeds in bowl
draw.point([(40, 37), (41, 37), (42, 38)], fill=(255, 215, 0, 255))
action_img.save(os.path.join(OUT_DIR, "player_action.png"))

# Happy Emotes: Khoa celebrates with joyful hop and sparkles
for i in range(4):
    h_img = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    p_base = down_stand.load()
    p_h = h_img.load()
    # Hop height: 0, 2, 3, 1 pixels
    hop = [0, 2, 3, 1][i]
    for x in range(64):
        for y in range(64):
            if y - hop >= 0 and y < 64:
                p_h[x, y - hop] = p_base[x, y]
    # Add sparkle stars around head
    draw_h = ImageDraw.Draw(h_img)
    sparkle_color = (255, 235, 59, 255)
    if i in [1, 2]:
        # Left star
        draw_h.point([(18, 14 - hop), (19, 13 - hop), (19, 15 - hop), (20, 14 - hop)], fill=sparkle_color)
        # Right star
        draw_h.point([(44, 12 - hop), (45, 11 - hop), (45, 13 - hop), (46, 12 - hop)], fill=sparkle_color)
    h_img.save(os.path.join(OUT_DIR, f"player_happy_{i+1}.png"))

print("Action and Happy emotes generated successfully!")

# -------------------------------------------------------------
# 6. GENERATE COMBAT ATTACK FRAMES (KHOA WIELDING WEAPONS)
# -------------------------------------------------------------
# Khoa Palette
HAIR_DARK   = (47, 36, 32, 255)
HAIR_MID    = (65, 50, 42, 255)
SKIN_BASE   = (242, 195, 155, 255)
SKIN_SHADE  = (210, 160, 120, 255)
JACKET_BASE = (73, 86, 79, 255)
JACKET_DARK = (52, 62, 57, 255)
JACKET_PATCH= (170, 140, 95, 255)
SHIRT_DARK  = (26, 26, 26, 255)
PANTS_DARK  = (38, 48, 51, 255)
BOOTS_BROWN = (113, 95, 73, 255)

# Weapon Colors
STEEL_LIGHT = (230, 240, 255, 255)
STEEL_MID   = (170, 185, 200, 255)
STEEL_DARK  = (100, 115, 130, 255)
WOOD_LIGHT  = (160, 110, 60, 255)
WOOD_DARK   = (100, 65, 35, 255)
SLASH_FX    = (0, 229, 255, 200)
SMASH_FX    = (255, 145, 0, 220)
THRUST_FX   = (118, 255, 3, 220)

def draw_khoa_head(draw, cx, cy, facing='down'):
    # Head/Hair based directly on Khoa's shape
    # cx, cy is center of head (around y=18)
    if facing == 'down':
        # Hair
        draw.ellipse([cx - 8, cy - 9, cx + 8, cy + 3], fill=HAIR_DARK)
        # Face
        draw.rectangle([cx - 6, cy - 2, cx + 6, cy + 5], fill=SKIN_BASE)
        draw.point([cx - 6, cy + 5], fill=SKIN_SHADE)
        draw.point([cx + 6, cy + 5], fill=SKIN_SHADE)
        # Serious eyes
        draw.point([(cx - 3, cy), (cx + 3, cy)], fill=(30, 20, 15, 255))
        draw.point([(cx - 3, cy - 1), (cx + 3, cy - 1)], fill=HAIR_DARK) # eyebrows
        # Messy hair bangs falling onto forehead
        draw.polygon([(cx - 5, cy - 5), (cx - 3, cy - 1), (cx - 1, cy - 4)], fill=HAIR_MID)
        draw.polygon([(cx, cy - 5), (cx + 2, cy - 1), (cx + 4, cy - 4)], fill=HAIR_MID)
    elif facing == 'side':
        # Side profile head
        draw.ellipse([cx - 7, cy - 9, cx + 7, cy + 3], fill=HAIR_DARK)
        draw.rectangle([cx - 3, cy - 2, cx + 6, cy + 5], fill=SKIN_BASE)
        # Side eye
        draw.point([(cx + 3, cy)], fill=(30, 20, 15, 255))
        # Side messy hair bang
        draw.polygon([(cx - 1, cy - 5), (cx + 3, cy - 2), (cx + 1, cy - 4)], fill=HAIR_MID)
    elif facing == 'up':
        # Back head: Full messy hair
        draw.ellipse([cx - 8, cy - 9, cx + 8, cy + 4], fill=HAIR_DARK)
        # Shading highlights on hair
        draw.point([(cx - 3, cy - 6), (cx + 2, cy - 5), (cx - 1, cy - 3)], fill=HAIR_MID)

def draw_khoa_body(draw, cx, cy, facing='down'):
    # cx, cy is center of chest (around y=28)
    if facing == 'down':
        # Inner shirt
        draw.rectangle([cx - 3, cy - 2, cx + 3, cy + 8], fill=SHIRT_DARK)
        # Slate Jacket
        draw.rectangle([cx - 7, cy - 2, cx - 3, cy + 8], fill=JACKET_BASE)
        draw.rectangle([cx + 3, cy - 2, cx + 7, cy + 8], fill=JACKET_BASE)
        # Leather patch on shoulder
        draw.point([(cx - 6, cy), (cx - 5, cy)], fill=JACKET_PATCH)
        # Pants
        draw.rectangle([cx - 5, cy + 9, cx - 1, cy + 18], fill=PANTS_DARK)
        draw.rectangle([cx + 1, cy + 9, cx + 5, cy + 18], fill=PANTS_DARK)
        # Boots
        draw.rectangle([cx - 6, cy + 19, cx - 1, cy + 22], fill=BOOTS_BROWN)
        draw.rectangle([cx + 1, cy + 19, cx + 6, cy + 22], fill=BOOTS_BROWN)
    elif facing == 'side':
        # Jacket
        draw.rectangle([cx - 5, cy - 2, cx + 5, cy + 8], fill=JACKET_BASE)
        draw.rectangle([cx - 4, cy + 1, cx + 1, cy + 8], fill=JACKET_DARK)
        # Pants
        draw.rectangle([cx - 4, cy + 9, cx + 4, cy + 18], fill=PANTS_DARK)
        # Boots
        draw.rectangle([cx - 5, cy + 19, cx + 4, cy + 22], fill=BOOTS_BROWN)
    elif facing == 'up':
        # Back jacket
        draw.rectangle([cx - 7, cy - 2, cx + 7, cy + 8], fill=JACKET_BASE)
        draw.rectangle([cx - 5, cy + 1, cx + 5, cy + 7], fill=JACKET_DARK)
        # Pants
        draw.rectangle([cx - 5, cy + 9, cx - 1, cy + 18], fill=PANTS_DARK)
        draw.rectangle([cx + 1, cy + 9, cx + 5, cy + 18], fill=PANTS_DARK)
        # Boots
        draw.rectangle([cx - 6, cy + 19, cx - 1, cy + 22], fill=BOOTS_BROWN)
        draw.rectangle([cx + 1, cy + 19, cx + 6, cy + 22], fill=BOOTS_BROWN)

# Helper to generate 3-frame attack archetype
def generate_attack_set(archetype, facing):
    frames = []
    for step in range(1, 4):
        img = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        cx = 32
        cy = 28 # chest center
        
        # 1. Wind-up (step 1)
        if step == 1:
            draw_khoa_head(d, cx - 1, 16, facing)
            draw_khoa_body(d, cx - 1, cy, facing)
            if archetype == 'slash':
                # Sword raised behind shoulder
                d.line([(cx - 10, cy - 12), (cx - 4, cy - 2)], fill=STEEL_LIGHT, width=2)
                d.point([(cx - 4, cy - 1)], fill=WOOD_DARK)
            elif archetype == 'smash':
                # Hammer raised high overhead with both hands
                d.line([(cx - 4, cy - 16), (cx - 2, cy - 2)], fill=WOOD_LIGHT, width=2)
                d.rectangle([cx - 8, cy - 20, cx, cy - 14], fill=STEEL_MID, outline=STEEL_DARK)
            elif archetype == 'thrust':
                # Spear pulled back close to chest ready to lunge
                d.line([(cx - 14, cy + 2), (cx - 2, cy + 2)], fill=WOOD_LIGHT, width=2)
                d.polygon([(cx - 16, cy + 2), (cx - 19, cy + 2), (cx - 15, cy + 1)], fill=STEEL_LIGHT)

        # 2. Active Strike (step 2)
        elif step == 2:
            draw_khoa_head(d, cx + 2, 17, facing)
            draw_khoa_body(d, cx + 2, cy, facing)
            if archetype == 'slash':
                # Sweeping blade slashing forward downwards with dynamic cyan arc
                d.arc([cx - 8, cy - 10, cx + 26, cy + 24], start=280, end=70, fill=SLASH_FX, width=3)
                d.line([(cx + 4, cy + 2), (cx + 20, cy + 16)], fill=STEEL_LIGHT, width=2)
            elif archetype == 'smash':
                # Hammer slammed down onto ground with orange impact shockwave
                d.line([(cx + 2, cy - 2), (cx + 12, cy + 20)], fill=WOOD_LIGHT, width=2)
                d.rectangle([cx + 8, cy + 16, cx + 18, cy + 24], fill=STEEL_MID, outline=STEEL_DARK)
                # Ground shockwave impact dust
                d.ellipse([cx + 6, cy + 22, cx + 24, cy + 28], outline=SMASH_FX, width=2)
            elif archetype == 'thrust':
                # Spear thrust deep forward with vibrant green piercing streak
                d.line([(cx + 2, cy + 2), (cx + 26, cy + 2)], fill=WOOD_LIGHT, width=2)
                d.polygon([(cx + 26, cy + 2), (cx + 32, cy + 2), (cx + 27, cy)], fill=STEEL_LIGHT)
                d.line([(cx + 12, cy + 2), (cx + 32, cy + 2)], fill=THRUST_FX, width=1)

        # 3. Recovery (step 3)
        elif step == 3:
            draw_khoa_head(d, cx, 16, facing)
            draw_khoa_body(d, cx, cy, facing)
            if archetype == 'slash':
                # Blade follows through in low ready stance
                d.line([(cx + 2, cy + 6), (cx + 14, cy + 18)], fill=STEEL_MID, width=2)
            elif archetype == 'smash':
                # Hammer head resting on ground, Khoa catching balance
                d.line([(cx + 2, cy + 4), (cx + 8, cy + 20)], fill=WOOD_LIGHT, width=2)
                d.rectangle([cx + 6, cy + 18, cx + 14, cy + 24], fill=STEEL_MID)
            elif archetype == 'thrust':
                # Spear pulled back to steady guard
                d.line([(cx - 2, cy + 2), (cx + 16, cy + 2)], fill=WOOD_LIGHT, width=2)
                d.polygon([(cx + 16, cy + 2), (cx + 20, cy + 2), (cx + 17, cy + 1)], fill=STEEL_LIGHT)

        frames.append(img)
    return frames

print("Generating combat animations for Slash, Smash, and Thrust...")
archetypes = ['slash', 'smash', 'thrust']
facings = ['down', 'side', 'up']

for arch in archetypes:
    for face in facings:
        anim_frames = generate_attack_set(arch, face)
        for idx, fr in enumerate(anim_frames):
            fr.save(os.path.join(OUT_DIR, f"player_{arch}_{face}_{idx+1}.png"))

print("All combat attack frames successfully generated!")
