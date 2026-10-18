import os
from PIL import Image, ImageDraw

SHEET_PATH = '/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/dau_linh_sheet_1792292540425.jpg'
OUT_DIR = 'Assets/Art/Sprites/Characters/Enemies/DauLinhTrom'
os.makedirs(OUT_DIR, exist_ok=True)

im = Image.open(SHEET_PATH).convert('RGB')
w, h = im.size
data = im.load()

# ---------------------------------------------------------------------------
# 1. EXTRACT CONNECTED COMPONENTS
# ---------------------------------------------------------------------------
visited = set()
components = []

for y in range(h):
    for x in range(w):
        if (x, y) not in visited:
            p = data[x, y]
            if not (p[0] > 230 and p[1] > 230 and p[2] > 230):
                comp = []
                queue = [(x, y)]
                visited.add((x, y))
                while queue:
                    cx, cy = queue.pop(0)
                    comp.append((cx, cy))
                    for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                        nx, ny = cx + dx, cy + dy
                        if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in visited:
                            np = data[nx, ny]
                            if not (np[0] > 230 and np[1] > 230 and np[2] > 230):
                                visited.add((nx, ny))
                                queue.append((nx, ny))
                if len(comp) > 200:
                    components.append(comp)

print(f"Total components found: {len(components)}")

# Helper to turn a pixel component into a crisp RGBA cropped image
def comp_to_image(comp):
    xs = [p[0] for p in comp]
    ys = [p[1] for p in comp]
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    cw = max_x - min_x + 1
    ch = max_y - min_y + 1
    
    img = Image.new('RGBA', (cw, ch), (0, 0, 0, 0))
    pix = img.load()
    for cx, cy in comp:
        pix[cx - min_x, cy - min_y] = data[cx, cy] + (255,)
    return img

target_h = 54
feet_y = 59

def fit_to_64(fg, custom_h=None, custom_feet_y=None, max_w=54):
    th = custom_h or target_h
    fy = custom_feet_y or feet_y
    
    s = min(float(th) / fg.height, float(max_w) / fg.width)
    nw = int(round(fg.width * s))
    nh = int(round(fg.height * s))
    
    resized = fg.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    
    paste_x = (64 - nw) // 2
    paste_y = fy - nh
    canvas.paste(resized, (paste_x, paste_y), resized)
    return canvas

# Sort components by Y centroid first, then group by rows
def get_centroid(c):
    return (sum(p[1] for p in c) / len(c), sum(p[0] for p in c) / len(c))

# Match specific components by their bounds:
# Row 0: y in [9..204]
r0_comps = sorted([c for c in components if min(p[1] for p in c) < 150], key=lambda c: min(p[0] for p in c))
# Row 1: y in [210..410]
r1_comps = sorted([c for c in components if 200 <= min(p[1] for p in c) < 350], key=lambda c: min(p[0] for p in c))
# Row 2: y in [410..615]
r2_comps = sorted([c for c in components if 400 <= min(p[1] for p in c) < 550], key=lambda c: min(p[0] for p in c))
# Row 3: y in [620..830]
r3_comps = sorted([c for c in components if 600 <= min(p[1] for p in c) < 750], key=lambda c: min(p[0] for p in c))
# Row 4: y in [820..1024]
r4_comps = sorted([c for c in components if min(p[1] for p in c) >= 800], key=lambda c: min(p[0] for p in c))

print(f"Row counts: R0={len(r0_comps)}, R1={len(r1_comps)}, R2={len(r2_comps)}, R3={len(r3_comps)}, R4={len(r4_comps)}")

# Merge dropped scimitar with prone body in Row 4 if split
if len(r4_comps) == 5:
    # r4_comps[3] is prone body, r4_comps[4] is dropped scimitar
    r4_merged = r4_comps[3] + r4_comps[4]
    r4_comps = [r4_comps[0], r4_comps[1], r4_comps[2], r4_merged]

# ---------------------------------------------------------------------------
# 2. BUILD 4-FRAME ANIMATIONS FOR ALL ACTIONS
# ---------------------------------------------------------------------------
# Down Walk: Left Stride -> Passing -> Right Stride -> Passing
dw_f1 = fit_to_64(comp_to_image(r0_comps[0]), custom_h=54, custom_feet_y=59)
dw_f2 = fit_to_64(comp_to_image(r0_comps[2]), custom_h=54, custom_feet_y=59)
dw_f3 = fit_to_64(comp_to_image(r0_comps[1]), custom_h=54, custom_feet_y=59)
dw_f4 = dw_f2.copy()
# Subtle passing frame eye gleam on frame 4
draw_dw4 = ImageDraw.Draw(dw_f4)
draw_dw4.point([(29, 15), (34, 15)], fill=(255, 230, 80, 255))
down_frames = [dw_f1, dw_f2, dw_f3, dw_f4]

# Side Walk (4 frames)
side_frames = [fit_to_64(comp_to_image(c), custom_h=54, custom_feet_y=59) for c in r1_comps]

# Up Walk (4 frames)
up_frames = [fit_to_64(comp_to_image(c), custom_h=54, custom_feet_y=59) for c in r2_comps]

# Attack (4 frames: Stance -> Forward Slash -> Cross Slash -> Recovery)
# In r3_comps: [c0 (x~29), c1 (x~268, slash), c2 (x~519, cross), c3 (x~793, recovery)]
atk_frames = [
    fit_to_64(comp_to_image(r3_comps[0]), custom_h=54, custom_feet_y=59, max_w=54),
    fit_to_64(comp_to_image(r3_comps[1]), custom_h=54, custom_feet_y=59, max_w=54),
    fit_to_64(comp_to_image(r3_comps[2]), custom_h=54, custom_feet_y=59, max_w=54),
    fit_to_64(comp_to_image(r3_comps[3]), custom_h=54, custom_feet_y=59, max_w=54)
]

# Death (4 frames: Stagger -> Falling -> Collapse -> Prone)
die_frames = [
    fit_to_64(comp_to_image(r4_comps[0]), custom_h=52, custom_feet_y=59, max_w=54),
    fit_to_64(comp_to_image(r4_comps[1]), custom_h=46, custom_feet_y=59, max_w=54),
    fit_to_64(comp_to_image(r4_comps[2]), custom_h=32, custom_feet_y=59, max_w=54),
    fit_to_64(comp_to_image(r4_comps[3]), custom_h=22, custom_feet_y=59, max_w=54)
]

# Hurt Frame: Recoil stance with Stardew 1.6 critical spark
hurt_frame = die_frames[0].copy()
draw_hurt = ImageDraw.Draw(hurt_frame)
draw_hurt.line([(32, 12), (32, 18)], fill=(255, 220, 60, 255), width=2)
draw_hurt.line([(29, 15), (35, 15)], fill=(255, 220, 60, 255), width=2)
draw_hurt.line([(30, 13), (34, 17)], fill=(255, 255, 255, 255), width=1)
draw_hurt.line([(30, 17), (34, 13)], fill=(255, 255, 255, 255), width=1)

# Verify zero clipping
def verify_and_save(img, name):
    bbox = img.getbbox()
    touch_left = bbox[0] == 0
    touch_top = bbox[1] == 0
    touch_right = bbox[2] == 64
    touch_bottom = bbox[3] == 64
    if any([touch_left, touch_top, touch_right, touch_bottom]):
        print(f"❌ ERROR: {name} is clipped at {bbox}")
    else:
        print(f"✔ {name:32}: bbox={bbox}, margin=[T={bbox[1]}, B={64-bbox[3]}, L={bbox[0]}, R={64-bbox[2]}]")
    path = os.path.join(OUT_DIR, name)
    img.save(path)

# ---------------------------------------------------------------------------
# 3. SAVE ALL 26 SPRITE ASSETS
# ---------------------------------------------------------------------------
sprites_map = {
    # Down Walk
    "dau_linh_down_walk1.png": down_frames[0],
    "dau_linh_down_walk2.png": down_frames[1],
    "dau_linh_down_walk3.png": down_frames[2],
    "dau_linh_down_walk4.png": down_frames[3],
    # Side Walk
    "dau_linh_side_walk1.png": side_frames[0],
    "dau_linh_side_walk2.png": side_frames[1],
    "dau_linh_side_walk3.png": side_frames[2],
    "dau_linh_side_walk4.png": side_frames[3],
    # Up Walk
    "dau_linh_up_walk1.png": up_frames[0],
    "dau_linh_up_walk2.png": up_frames[1],
    "dau_linh_up_walk3.png": up_frames[2],
    "dau_linh_up_walk4.png": up_frames[3],
    # Attack
    "dau_linh_attack1.png": atk_frames[0],
    "dau_linh_attack2.png": atk_frames[1],
    "dau_linh_attack3.png": atk_frames[2],
    "dau_linh_attack4.png": atk_frames[3],
    # Die
    "dau_linh_die1.png": die_frames[0],
    "dau_linh_die2.png": die_frames[1],
    "dau_linh_die3.png": die_frames[2],
    "dau_linh_die4.png": die_frames[3],
    # Hurt
    "dau_linh_hurt.png": hurt_frame,
    # Directional / Defaults
    "dau_linh_down.png": down_frames[0],
    "dau_linh_idle.png": down_frames[0],
    "dau_linh_side.png": side_frames[0],
    "dau_linh_left.png": side_frames[0].transpose(Image.FLIP_LEFT_RIGHT),
    "dau_linh_right.png": side_frames[0],
    "dau_linh_up.png": up_frames[0],
    "dau_linh_attack.png": atk_frames[1],
    "dau_linh_die.png": die_frames[3]
}

print("=== SAVING & VERIFYING ALL 26 SPRITES ===")
for fname, img in sprites_map.items():
    verify_and_save(img, fname)

# ---------------------------------------------------------------------------
# 4. GENERATE ANIMATED GIFS & MATRIX SHOWCASE
# ---------------------------------------------------------------------------
def make_gif(frames, out_path, duration=160):
    upscaled = [f.resize((128, 128), Image.NEAREST) for f in frames]
    upscaled[0].save(out_path, save_all=True, append_images=upscaled[1:], duration=duration, loop=0, transparency=0, disposal=2)
    print(f"✔ Saved GIF: {out_path}")

make_gif(down_frames, "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/dau_linh_down_walk.gif")
make_gif(side_frames, "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/dau_linh_side_walk.gif")
make_gif(up_frames, "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/dau_linh_up_walk.gif")
make_gif(atk_frames, "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/dau_linh_attack.gif", duration=180)
make_gif(die_frames, "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/dau_linh_die.gif", duration=250)

# Matrix Showcase
matrix_rows = [
    ("Down Walk", down_frames),
    ("Side Walk", side_frames),
    ("Up Walk", up_frames),
    ("Attack", atk_frames),
    ("Death", die_frames)
]

matrix_w = 4 * 64 + 5 * 8
matrix_h = len(matrix_rows) * 64 + (len(matrix_rows) + 1) * 8
matrix = Image.new("RGBA", (matrix_w, matrix_h), (24, 24, 32, 255))
d_mat = ImageDraw.Draw(matrix)

for r_idx, (label, f_list) in enumerate(matrix_rows):
    y_pos = 8 + r_idx * (64 + 8)
    for c_idx, frame in enumerate(f_list):
        x_pos = 8 + c_idx * (64 + 8)
        d_mat.rectangle([x_pos - 1, y_pos - 1, x_pos + 64, y_pos + 64], fill=(36, 38, 50, 255), outline=(60, 64, 80, 255))
        matrix.paste(frame, (x_pos, y_pos), frame)

matrix_scaled = matrix.resize((matrix_w * 2, matrix_h * 2), Image.NEAREST)
matrix_path = "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/dau_linh_trom_animation_matrix.png"
matrix_scaled.save(matrix_path)
print(f"✔ Saved Matrix Showcase: {matrix_path}")

print("==================================================================")
print("   HOÀN TẤT THIẾT KẾ TOÀN BỘ 4-FRAME ANIMATION ĐẦU LĨNH TRỘM!    ")
print("==================================================================")
