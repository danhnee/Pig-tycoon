import os
import uuid
from PIL import Image, ImageDraw

ARTIFACT_DIR = "/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28"
ENEMIES_DIR = "Assets/Art/Sprites/Characters/Enemies"

THIEF_DIR = os.path.join(ENEMIES_DIR, "Thief")
BREAKER_DIR = os.path.join(ENEMIES_DIR, "Breaker")
PEST_DIR = os.path.join(ENEMIES_DIR, "Pest")

for d in [THIEF_DIR, BREAKER_DIR, PEST_DIR]:
    os.makedirs(d, exist_ok=True)

# ---------------------------------------------------------------------------
# UNITY .META GENERATOR
# ---------------------------------------------------------------------------
def generate_meta_for_file(dest_path):
    meta_path = dest_path + ".meta"
    if os.path.exists(meta_path):
        return
    guid = uuid.uuid4().hex
    meta_content = f"""fileFormatVersion: 2
guid: {guid}
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
"""
    with open(meta_path, "w") as f:
        f.write(meta_content)

def ensure_safe_bounds(img, target_feet_y=59):
    bbox = img.getbbox()
    if bbox is None:
        return img
    cur_w = bbox[2] - bbox[0]
    cur_h = bbox[3] - bbox[1]
    
    max_w = 54
    max_h = 54
    scale = min(1.0, float(max_w) / cur_w, float(max_h) / cur_h)
    
    crop = img.crop(bbox)
    if scale < 1.0:
        nw = max(1, int(round(cur_w * scale)))
        nh = max(1, int(round(cur_h * scale)))
        crop = crop.resize((nw, nh), Image.Resampling.LANCZOS)
    else:
        nw, nh = cur_w, cur_h
        
    canvas = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    paste_x = (64 - nw) // 2
    paste_y = target_feet_y - nh
    if paste_y < 5:
        paste_y = 5
    if paste_y + nh > 59:
        paste_y = 59 - nh
    if paste_x < 5:
        paste_x = 5
    if paste_x + nw > 59:
        paste_x = 59 - nw
        
    canvas.paste(crop, (paste_x, paste_y), crop)
    return canvas

def verify_and_save(img, out_dir, name, legacy_alias=None):
    img = ensure_safe_bounds(img)
    bbox = img.getbbox()
    if bbox is None:
        raise ValueError(f"Empty image for {name}")
    margin_l, margin_t, margin_r, margin_b = bbox[0], bbox[1], 64 - bbox[2], 64 - bbox[3]
    if margin_l < 5 or margin_t < 5 or margin_r < 5 or margin_b < 5:
        print(f"❌ MARGIN WARNING: {name} margins: L={margin_l}, T={margin_t}, R={margin_r}, B={margin_b}")
    else:
        print(f"✔ {name:28}: bbox={bbox}, margins=[L={margin_l}, T={margin_t}, R={margin_r}, B={margin_b}]")
    
    path = os.path.join(out_dir, name)
    img.save(path)
    generate_meta_for_file(path)
    
    if legacy_alias:
        legacy_path = os.path.join(ENEMIES_DIR, legacy_alias)
        img.save(legacy_path)
        generate_meta_for_file(legacy_path)
    return img


def make_gif(frames, out_path, duration=160):
    upscaled = [f.resize((128, 128), Image.NEAREST) for f in frames]
    upscaled[0].save(out_path, save_all=True, append_images=upscaled[1:], duration=duration, loop=0, transparency=0, disposal=2)
    print(f"✔ Saved GIF: {out_path}")

def make_matrix_showcase(matrix_rows, out_path):
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
    matrix_scaled.save(out_path)
    print(f"✔ Saved Matrix Showcase: {out_path}")

# ===========================================================================
# 1. ENEMY_THIEF (KẺ BẮT HEO / PIG SNATCHER)
# ===========================================================================
def build_thief():
    print("\n==================== [1/3] BUILDING ENEMY_THIEF ====================")
    sheet_path = os.path.join(ARTIFACT_DIR, "thief_sheet_1792293200357.jpg")
    im = Image.open(sheet_path).convert('RGB')
    w, h = im.size
    data = im.load()

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

    print(f"Thief components extracted: {len(components)}")

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

    def fit_to_64(fg, custom_h=54, custom_feet_y=59, max_w=54):
        s = min(float(custom_h) / fg.height, float(max_w) / fg.width)
        nw = int(round(fg.width * s))
        nh = int(round(fg.height * s))
        resized = fg.resize((nw, nh), Image.Resampling.LANCZOS)
        canvas = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        paste_x = (64 - nw) // 2
        paste_y = custom_feet_y - nh
        canvas.paste(resized, (paste_x, paste_y), resized)
        return canvas

    r0 = sorted([c for c in components if min(p[1] for p in c) < 150], key=lambda c: min(p[0] for p in c))
    r1 = sorted([c for c in components if 200 <= min(p[1] for p in c) < 350], key=lambda c: min(p[0] for p in c))
    r2 = sorted([c for c in components if 400 <= min(p[1] for p in c) < 550], key=lambda c: min(p[0] for p in c))
    r3 = sorted([c for c in components if 600 <= min(p[1] for p in c) < 750], key=lambda c: min(p[0] for p in c))
    r4 = sorted([c for c in components if min(p[1] for p in c) >= 800], key=lambda c: min(p[0] for p in c))

    down_frames = [
        fit_to_64(comp_to_image(r0[0]), custom_h=54, custom_feet_y=59),
        fit_to_64(comp_to_image(r0[1]), custom_h=54, custom_feet_y=59),
        fit_to_64(comp_to_image(r0[2]), custom_h=54, custom_feet_y=59),
        fit_to_64(comp_to_image(r0[3]), custom_h=54, custom_feet_y=59)
    ]
    side_frames = [
        fit_to_64(comp_to_image(r1[0]), custom_h=54, custom_feet_y=59),
        fit_to_64(comp_to_image(r1[1]), custom_h=54, custom_feet_y=59),
        fit_to_64(comp_to_image(r1[2]), custom_h=54, custom_feet_y=59),
        fit_to_64(comp_to_image(r1[3]), custom_h=54, custom_feet_y=59)
    ]
    up_frames = [
        fit_to_64(comp_to_image(r2[0]), custom_h=54, custom_feet_y=59),
        fit_to_64(comp_to_image(r2[1]), custom_h=54, custom_feet_y=59),
        fit_to_64(comp_to_image(r2[2]), custom_h=54, custom_feet_y=59),
        fit_to_64(comp_to_image(r2[3]), custom_h=54, custom_feet_y=59)
    ]
    atk_frames = [
        fit_to_64(comp_to_image(r3[0]), custom_h=54, custom_feet_y=59, max_w=54),
        fit_to_64(comp_to_image(r3[1]), custom_h=54, custom_feet_y=59, max_w=54),
        fit_to_64(comp_to_image(r3[2]), custom_h=54, custom_feet_y=59, max_w=54),
        fit_to_64(comp_to_image(r3[3]), custom_h=54, custom_feet_y=59, max_w=54)
    ]
    die_frames = [
        fit_to_64(comp_to_image(r4[0]), custom_h=52, custom_feet_y=59, max_w=54),
        fit_to_64(comp_to_image(r4[1]), custom_h=46, custom_feet_y=59, max_w=54),
        fit_to_64(comp_to_image(r4[2]), custom_h=32, custom_feet_y=59, max_w=54),
        fit_to_64(comp_to_image(r4[3]), custom_h=22, custom_feet_y=59, max_w=54)
    ]

    # Clean Hurt Frame: recoil pose with clean critical spark
    hurt_frame = die_frames[0].copy()
    d_hurt = ImageDraw.Draw(hurt_frame)
    d_hurt.line([(32, 12), (32, 18)], fill=(255, 220, 60, 255), width=2)
    d_hurt.line([(29, 15), (35, 15)], fill=(255, 220, 60, 255), width=2)
    d_hurt.line([(30, 13), (34, 17)], fill=(255, 255, 255, 255), width=1)
    d_hurt.line([(30, 17), (34, 13)], fill=(255, 255, 255, 255), width=1)

    thief_map = {
        "thief_down_walk1.png": (down_frames[0], None),
        "thief_down_walk2.png": (down_frames[1], None),
        "thief_down_walk3.png": (down_frames[2], None),
        "thief_down_walk4.png": (down_frames[3], None),
        "thief_side_walk1.png": (side_frames[0], None),
        "thief_side_walk2.png": (side_frames[1], None),
        "thief_side_walk3.png": (side_frames[2], None),
        "thief_side_walk4.png": (side_frames[3], None),
        "thief_up_walk1.png": (up_frames[0], None),
        "thief_up_walk2.png": (up_frames[1], None),
        "thief_up_walk3.png": (up_frames[2], None),
        "thief_up_walk4.png": (up_frames[3], None),
        "thief_attack1.png": (atk_frames[0], None),
        "thief_attack2.png": (atk_frames[1], None),
        "thief_attack3.png": (atk_frames[2], None),
        "thief_attack4.png": (atk_frames[3], None),
        "thief_die1.png": (die_frames[0], None),
        "thief_die2.png": (die_frames[1], None),
        "thief_die3.png": (die_frames[2], None),
        "thief_die4.png": (die_frames[3], None),
        "thief_hurt.png": (hurt_frame, "enemy_thief_hurt.png"),
        "thief_down.png": (down_frames[0], "enemy_thief_down.png"),
        "thief_idle.png": (down_frames[0], None),
        "thief_side.png": (side_frames[0], "enemy_thief_side.png"),
        "thief_left.png": (side_frames[0].transpose(Image.FLIP_LEFT_RIGHT), None),
        "thief_right.png": (side_frames[0], None),
        "thief_up.png": (up_frames[0], "enemy_thief_up.png"),
        "thief_attack.png": (atk_frames[1], None),
        "thief_die.png": (die_frames[3], None)
    }

    saved_sprites = {}
    for fname, (img, alias) in thief_map.items():
        saved_sprites[fname] = verify_and_save(img, THIEF_DIR, fname, legacy_alias=alias)

    make_gif([saved_sprites[f"thief_down_walk{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "thief_down_walk.gif"))
    make_gif([saved_sprites[f"thief_side_walk{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "thief_side_walk.gif"))
    make_gif([saved_sprites[f"thief_up_walk{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "thief_up_walk.gif"))
    make_gif([saved_sprites[f"thief_attack{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "thief_attack.gif"), duration=180)
    make_gif([saved_sprites[f"thief_die{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "thief_die.gif"), duration=250)
    
    make_matrix_showcase([
        ("Down Walk", [saved_sprites[f"thief_down_walk{i}.png"] for i in range(1, 5)]),
        ("Side Walk", [saved_sprites[f"thief_side_walk{i}.png"] for i in range(1, 5)]),
        ("Up Walk", [saved_sprites[f"thief_up_walk{i}.png"] for i in range(1, 5)]),
        ("Attack", [saved_sprites[f"thief_attack{i}.png"] for i in range(1, 5)]),
        ("Death", [saved_sprites[f"thief_die{i}.png"] for i in range(1, 5)])
    ], os.path.join(ARTIFACT_DIR, "thief_animation_matrix.png"))



# ===========================================================================
# 2. ENEMY_BREAKER (KẺ CÀO PHÁ / FARM BREAKER)
# ===========================================================================
def build_breaker():
    print("\n==================== [2/3] BUILDING ENEMY_BREAKER ====================")
    C_HELM = (120, 130, 145, 255)
    C_HELM_SH = (65, 72, 85, 255)
    C_HELM_HI = (175, 185, 205, 255)
    C_HORN = (215, 195, 165, 255)
    C_HORN_SH = (145, 125, 95, 255)
    C_SKIN = (215, 150, 110, 255)
    C_SKIN_SH = (155, 95, 65, 255)
    C_ARMOR = (140, 90, 55, 255)
    C_ARMOR_SH = (85, 50, 28, 255)
    C_IRON = (170, 180, 195, 255)
    C_IRON_SH = (90, 100, 115, 255)
    C_PANTS = (58, 48, 40, 255)
    C_BOOTS = (38, 42, 50, 255)
    C_BOOTS_SH = (20, 24, 30, 255)
    C_HAFT = (110, 72, 38, 255)
    C_EYES = (255, 45, 45, 255)

    def draw_hammer(draw, hx0, hy0, hx1, hy1, rot_angle=0):
        # Spiked Sledgehammer / Heavy War Club
        draw.line([(hx0, hy0), (hx1, hy1)], fill=C_HAFT, width=3)
        # Iron head
        draw.rectangle([hx1 - 5, hy1 - 4, hx1 + 5, hy1 + 4], fill=C_IRON, outline=C_IRON_SH)
        draw.point((hx1, hy1), fill=C_HELM_HI)
        # Breaker spike on back
        draw.polygon([(hx1 - 6, hy1 - 1), (hx1 - 10, hy1), (hx1 - 6, hy1 + 1)], fill=C_IRON_SH)

    def draw_breaker_down(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = -1 if (frame_idx % 2 == 1) else 0

        # Leg offsets for 4-frame walk
        left_leg_y = 59 if frame_idx == 0 else (57 if frame_idx == 2 else 58)
        right_leg_y = 59 if frame_idx == 2 else (57 if frame_idx == 0 else 58)
        left_foot_x = 24 if frame_idx == 0 else 23
        right_foot_x = 39 if frame_idx == 2 else 38

        # 1. Weapon in hand (Sledgehammer)
        ham_sway = 2 if frame_idx == 0 else (-2 if frame_idx == 2 else 0)
        d.line([(15 + ham_sway, 24 + bob), (19 + ham_sway, 52 + bob)], fill=C_HAFT, width=3)
        d.rectangle([13 + ham_sway, 20 + bob, 22 + ham_sway, 27 + bob], fill=C_IRON, outline=C_IRON_SH)
        d.polygon([(10 + ham_sway, 23 + bob), (13 + ham_sway, 21 + bob), (13 + ham_sway, 25 + bob)], fill=C_IRON_SH)

        # 2. Head & Horned Helmet (y=7..24)
        d.polygon([(23, 10 + bob), (32, 6 + bob), (41, 10 + bob), (43, 23 + bob), (21, 23 + bob)], fill=C_HELM, outline=C_HELM_SH)
        # Horns
        d.polygon([(21, 12 + bob), (15, 6 + bob), (19, 15 + bob)], fill=C_HORN, outline=C_HORN_SH)
        d.polygon([(43, 12 + bob), (49, 6 + bob), (45, 15 + bob)], fill=C_HORN, outline=C_HORN_SH)
        # Visor slit & glowing eyes
        d.rectangle([25, 13 + bob, 39, 17 + bob], fill=(22, 25, 32, 255))
        d.rectangle([27, 14 + bob, 30, 16 + bob], fill=C_EYES)
        d.rectangle([34, 14 + bob, 37, 16 + bob], fill=C_EYES)
        # Beard / lower jaw
        d.rectangle([24, 19 + bob, 40, 23 + bob], fill=(42, 34, 28, 255))

        # 3. Torso (Muscular, broad shoulders x=19..45, y=23..43)
        d.polygon([(19, 23 + bob), (45, 23 + bob), (43, 42 + bob), (21, 42 + bob)], fill=C_ARMOR, outline=C_ARMOR_SH)
        d.rectangle([24, 26 + bob, 40, 36 + bob], fill=C_IRON, outline=C_HELM_SH)
        # Studs
        d.point((26, 28 + bob), fill=(255, 220, 100, 255))
        d.point((38, 28 + bob), fill=(255, 220, 100, 255))
        d.point((32, 33 + bob), fill=(255, 220, 100, 255))

        # Arms with spiked bracers
        d.rectangle([15, 24 + bob, 20, 38 + bob], fill=C_SKIN_SH)
        d.rectangle([44, 24 + bob, 49, 38 + bob], fill=C_SKIN_SH)
        d.rectangle([14, 36 + bob, 20, 41 + bob], fill=C_HELM_SH)
        d.rectangle([44, 36 + bob, 50, 41 + bob], fill=C_HELM_SH)

        # 4. Legs & Boots (y=42..59)
        d.rectangle([23, 42 + bob, 30, left_leg_y - 6], fill=C_PANTS)
        d.rectangle([34, 42 + bob, 41, right_leg_y - 6], fill=C_PANTS)
        d.rectangle([left_foot_x - 1, left_leg_y - 6, left_foot_x + 7, left_leg_y], fill=C_BOOTS, outline=C_BOOTS_SH)
        d.rectangle([right_foot_x - 4, right_leg_y - 6, right_foot_x + 4, right_leg_y], fill=C_BOOTS, outline=C_BOOTS_SH)
        return img

    def draw_breaker_side(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = -1 if (frame_idx % 2 == 1) else 0

        # Walking stride
        foot_f_x = 42 if frame_idx == 0 else (38 if frame_idx == 2 else 40)
        foot_b_x = 24 if frame_idx == 0 else (28 if frame_idx == 2 else 26)
        foot_f_y = 59 if frame_idx == 0 else 58
        foot_b_y = 57 if frame_idx == 0 else 58

        # 1. Torso profile
        d.polygon([(26, 23 + bob), (44, 23 + bob), (45, 42 + bob), (25, 42 + bob)], fill=C_ARMOR, outline=C_ARMOR_SH)
        d.rectangle([30, 25 + bob, 43, 37 + bob], fill=C_IRON)

        # 2. Head profile & Horn
        d.polygon([(27, 10 + bob), (36, 6 + bob), (45, 11 + bob), (45, 22 + bob), (28, 23 + bob)], fill=C_HELM, outline=C_HELM_SH)
        d.polygon([(36, 8 + bob), (46, 3 + bob), (40, 11 + bob)], fill=C_HORN, outline=C_HORN_SH)
        d.rectangle([36, 13 + bob, 44, 17 + bob], fill=(22, 25, 32, 255))
        d.rectangle([39, 14 + bob, 43, 16 + bob], fill=C_EYES)

        # 3. Arm & Hammer forward
        ham_ang = 4 if frame_idx == 0 else (-4 if frame_idx == 2 else 0)
        d.polygon([(36, 26 + bob), (48, 33 + bob), (50, 31 + bob), (38, 24 + bob)], fill=C_SKIN_SH)
        d.rectangle([48, 30 + bob, 53, 36 + bob], fill=C_HELM_SH)
        # Sledgehammer
        d.line([(51 + ham_ang, 18 + bob), (51 + ham_ang, 48 + bob)], fill=C_HAFT, width=3)
        d.rectangle([47 + ham_ang, 15 + bob, 55 + ham_ang, 22 + bob], fill=C_IRON, outline=C_IRON_SH)

        # 4. Legs
        d.rectangle([25, 42 + bob, 33, foot_b_y - 6], fill=C_PANTS)
        d.rectangle([37, 42 + bob, 45, foot_f_y - 6], fill=C_PANTS)
        d.rectangle([foot_b_x - 2, foot_b_y - 6, foot_b_x + 7, foot_b_y], fill=C_BOOTS, outline=C_BOOTS_SH)
        d.rectangle([foot_f_x - 3, foot_f_y - 6, foot_f_x + 6, foot_f_y], fill=C_BOOTS, outline=C_BOOTS_SH)
        return img

    def draw_breaker_up(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = -1 if (frame_idx % 2 == 1) else 0

        left_leg_y = 59 if frame_idx == 0 else (57 if frame_idx == 2 else 58)
        right_leg_y = 59 if frame_idx == 2 else (57 if frame_idx == 0 else 58)

        # 1. Helmet rear
        d.polygon([(23, 10 + bob), (32, 6 + bob), (41, 10 + bob), (43, 23 + bob), (21, 23 + bob)], fill=C_HELM, outline=C_HELM_SH)
        d.polygon([(21, 12 + bob), (15, 6 + bob), (19, 15 + bob)], fill=C_HORN, outline=C_HORN_SH)
        d.polygon([(43, 12 + bob), (49, 6 + bob), (45, 15 + bob)], fill=C_HORN, outline=C_HORN_SH)

        # 2. Torso rear (Armor mantle)
        d.polygon([(19, 23 + bob), (45, 23 + bob), (43, 42 + bob), (21, 42 + bob)], fill=C_ARMOR, outline=C_ARMOR_SH)

        # 3. Hammer slung at back side
        d.line([(16, 20 + bob), (16, 50 + bob)], fill=C_HAFT, width=3)
        d.rectangle([12, 16 + bob, 20, 23 + bob], fill=C_IRON, outline=C_IRON_SH)

        # 4. Legs rear
        d.rectangle([23, 42 + bob, 30, left_leg_y - 6], fill=C_PANTS)
        d.rectangle([34, 42 + bob, 41, right_leg_y - 6], fill=C_PANTS)
        d.rectangle([22, left_leg_y - 6, 30, left_leg_y], fill=C_BOOTS, outline=C_BOOTS_SH)
        d.rectangle([34, right_leg_y - 6, 42, right_leg_y], fill=C_BOOTS, outline=C_BOOTS_SH)
        return img

    def draw_breaker_attack(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)

        if frame_idx == 0:
            # Windup: raises hammer high overhead, lean back
            d.polygon([(23, 12), (32, 8), (41, 12), (43, 24), (21, 24)], fill=C_HELM, outline=C_HELM_SH)
            d.polygon([(21, 14), (15, 8), (19, 17)], fill=C_HORN, outline=C_HORN_SH)
            d.polygon([(43, 14), (49, 8), (45, 17)], fill=C_HORN, outline=C_HORN_SH)
            d.rectangle([25, 15, 39, 18], fill=(22, 25, 32, 255))
            d.rectangle([27, 16, 30, 18], fill=C_EYES)
            d.rectangle([34, 16, 37, 18], fill=C_EYES)

            d.polygon([(20, 24), (44, 24), (42, 43), (22, 43)], fill=C_ARMOR, outline=C_ARMOR_SH)
            d.rectangle([24, 27, 40, 37], fill=C_IRON)
            # Raised arms
            d.line([(22, 25), (20, 12)], fill=C_SKIN_SH, width=4)
            d.line([(42, 25), (44, 12)], fill=C_SKIN_SH, width=4)
            # Hammer held aloft high at top (y=5..25)
            d.line([(18, 14), (46, 12)], fill=C_HAFT, width=4)
            d.rectangle([44, 5, 54, 15], fill=C_IRON, outline=C_IRON_SH)
            d.point((49, 10), fill=C_HELM_HI)

            d.rectangle([24, 43, 31, 53], fill=C_PANTS)
            d.rectangle([33, 43, 40, 53], fill=C_PANTS)
            d.rectangle([23, 53, 31, 59], fill=C_BOOTS, outline=C_BOOTS_SH)
            d.rectangle([33, 53, 41, 59], fill=C_BOOTS, outline=C_BOOTS_SH)

        elif frame_idx == 1:
            # Swing: downward smash with motion slash arc
            d.polygon([(25, 14), (34, 10), (43, 14), (44, 26), (23, 26)], fill=C_HELM, outline=C_HELM_SH)
            d.polygon([(27, 17), (41, 20)], fill=C_EYES)

            d.polygon([(22, 26), (46, 26), (44, 44), (24, 44)], fill=C_ARMOR, outline=C_ARMOR_SH)
            # Lunging arms bringing hammer down
            d.line([(24, 28), (34, 38)], fill=C_SKIN_SH, width=4)
            d.line([(42, 28), (36, 38)], fill=C_SKIN_SH, width=4)
            # Motion Slash Arc (Vivid cyan/white swing trail)
            d.arc([16, 8, 54, 50], start=180, end=350, fill=(240, 250, 255, 255), width=2)
            d.arc([18, 10, 52, 48], start=200, end=340, fill=(130, 220, 255, 200), width=3)
            # Hammer angled mid-slam
            d.line([(32, 28), (38, 52)], fill=C_HAFT, width=4)
            d.rectangle([34, 46, 44, 54], fill=C_IRON, outline=C_IRON_SH)

            d.rectangle([24, 44, 31, 53], fill=C_PANTS)
            d.rectangle([37, 44, 44, 53], fill=C_PANTS)
            d.rectangle([23, 53, 31, 59], fill=C_BOOTS, outline=C_BOOTS_SH)
            d.rectangle([37, 53, 45, 59], fill=C_BOOTS, outline=C_BOOTS_SH)

        elif frame_idx == 2:
            # Impact: Hammer smashes ground, ground fissures & sparks
            d.polygon([(23, 16), (32, 12), (41, 16), (43, 28), (21, 28)], fill=C_HELM, outline=C_HELM_SH)
            d.rectangle([26, 19, 38, 22], fill=C_EYES)

            d.polygon([(20, 28), (44, 28), (42, 45), (22, 45)], fill=C_ARMOR, outline=C_ARMOR_SH)
            # Slammed arms
            d.line([(22, 30), (32, 48)], fill=C_SKIN_SH, width=4)
            d.line([(42, 30), (34, 48)], fill=C_SKIN_SH, width=4)
            # Hammer flat on ground
            d.line([(32, 38), (32, 54)], fill=C_HAFT, width=4)
            d.rectangle([26, 50, 38, 58], fill=C_IRON, outline=C_IRON_SH)
            # Ground cracks and shockwave sparks
            d.line([(18, 58), (26, 58)], fill=(180, 140, 80, 255), width=2)
            d.line([(38, 58), (46, 58)], fill=(180, 140, 80, 255), width=2)
            d.point([(20, 53), (44, 53), (17, 56), (47, 56)], fill=(255, 230, 80, 255))
            d.point([(23, 50), (41, 50)], fill=(255, 255, 255, 255))

            d.rectangle([21, 45, 28, 53], fill=C_PANTS)
            d.rectangle([36, 45, 43, 53], fill=C_PANTS)
            d.rectangle([20, 53, 28, 59], fill=C_BOOTS, outline=C_BOOTS_SH)
            d.rectangle([36, 53, 44, 59], fill=C_BOOTS, outline=C_BOOTS_SH)

        elif frame_idx == 3:
            # Recovery: lifting hammer from ground
            d.polygon([(23, 12), (32, 8), (41, 12), (43, 25), (21, 25)], fill=C_HELM, outline=C_HELM_SH)
            d.rectangle([26, 15, 38, 18], fill=C_EYES)

            d.polygon([(20, 25), (44, 25), (42, 44), (22, 44)], fill=C_ARMOR, outline=C_ARMOR_SH)
            d.line([(22, 27), (28, 44)], fill=C_SKIN_SH, width=3)
            d.line([(42, 27), (36, 44)], fill=C_SKIN_SH, width=3)
            d.line([(28, 32), (32, 54)], fill=C_HAFT, width=3)
            d.rectangle([28, 48, 36, 56], fill=C_IRON, outline=C_IRON_SH)

            d.rectangle([23, 44, 30, 53], fill=C_PANTS)
            d.rectangle([34, 44, 41, 53], fill=C_PANTS)
            d.rectangle([22, 53, 30, 59], fill=C_BOOTS, outline=C_BOOTS_SH)
            d.rectangle([34, 53, 42, 59], fill=C_BOOTS, outline=C_BOOTS_SH)
        return img

    def draw_breaker_die(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)

        if frame_idx == 0:
            # Stagger recoil
            d.polygon([(25, 12), (34, 8), (43, 12), (44, 24), (23, 24)], fill=C_HELM, outline=C_HELM_SH)
            d.line([(27, 15), (32, 15)], fill=(255, 255, 255, 255))
            d.line([(35, 15), (40, 15)], fill=(255, 255, 255, 255))
            d.polygon([(21, 24), (45, 24), (42, 43), (22, 43)], fill=C_ARMOR, outline=C_ARMOR_SH)
            # Hammer slipping away
            d.line([(44, 30), (52, 50)], fill=C_HAFT, width=3)
            d.rectangle([50, 46, 58, 54], fill=C_IRON, outline=C_IRON_SH)
            d.rectangle([23, 43, 30, 53], fill=C_PANTS)
            d.rectangle([34, 43, 41, 53], fill=C_PANTS)
            d.rectangle([22, 53, 30, 59], fill=C_BOOTS, outline=C_BOOTS_SH)
            d.rectangle([34, 53, 42, 59], fill=C_BOOTS, outline=C_BOOTS_SH)

        elif frame_idx == 1:
            # Knee buckle, collapsing forward
            d.polygon([(25, 20), (34, 16), (43, 20), (44, 31), (23, 31)], fill=C_HELM, outline=C_HELM_SH)
            d.polygon([(22, 31), (44, 31), (42, 47), (23, 47)], fill=C_ARMOR, outline=C_ARMOR_SH)
            # Dropped hammer on ground
            d.line([(42, 56), (56, 56)], fill=C_HAFT, width=3)
            d.rectangle([52, 52, 58, 58], fill=C_IRON, outline=C_IRON_SH)
            # Buckled knees
            d.rectangle([21, 47, 30, 55], fill=C_PANTS)
            d.rectangle([34, 47, 43, 55], fill=C_PANTS)
            d.rectangle([20, 55, 29, 59], fill=C_BOOTS, outline=C_BOOTS_SH)
            d.rectangle([34, 55, 43, 59], fill=C_BOOTS, outline=C_BOOTS_SH)

        elif frame_idx == 2:
            # Hitting ground
            d.ellipse([18, 38, 44, 52], fill=C_ARMOR, outline=C_ARMOR_SH)
            d.polygon([(14, 40), (22, 36), (22, 48), (14, 46)], fill=C_HELM, outline=C_HELM_SH)
            d.line([(40, 55), (56, 55)], fill=C_HAFT, width=3)
            d.rectangle([52, 52, 58, 58], fill=C_IRON, outline=C_IRON_SH)
            d.rectangle([36, 44, 46, 54], fill=C_PANTS)
            d.rectangle([44, 53, 50, 59], fill=C_BOOTS, outline=C_BOOTS_SH)

        elif frame_idx == 3:
            # Flat prone on ground
            d.ellipse([16, 48, 44, 58], fill=C_ARMOR, outline=C_ARMOR_SH)
            d.ellipse([10, 48, 20, 57], fill=C_HELM, outline=C_HELM_SH)
            d.line([(38, 57), (56, 57)], fill=C_HAFT, width=3)
            d.rectangle([52, 53, 58, 59], fill=C_IRON, outline=C_IRON_SH)
            d.rectangle([40, 52, 48, 58], fill=C_PANTS)
            d.rectangle([47, 54, 52, 59], fill=C_BOOTS, outline=C_BOOTS_SH)
        return img

    def draw_breaker_hurt():
        # Clean Hurt Frame: Recoil stance with golden critical damage spark
        base = draw_breaker_down(frame_idx=0)
        d = ImageDraw.Draw(base)
        d.line([(32, 14), (32, 22)], fill=(255, 230, 80, 255), width=2)
        d.line([(28, 18), (36, 18)], fill=(255, 230, 80, 255), width=2)
        d.line([(29, 15), (35, 21)], fill=(255, 255, 255, 255), width=1)
        d.line([(29, 21), (35, 15)], fill=(255, 255, 255, 255), width=1)
        return base

    down_frames = [draw_breaker_down(i) for i in range(4)]
    side_frames = [draw_breaker_side(i) for i in range(4)]
    up_frames = [draw_breaker_up(i) for i in range(4)]
    atk_frames = [draw_breaker_attack(i) for i in range(4)]
    die_frames = [draw_breaker_die(i) for i in range(4)]
    hurt_frame = draw_breaker_hurt()

    breaker_map = {
        "breaker_down_walk1.png": (down_frames[0], None),
        "breaker_down_walk2.png": (down_frames[1], None),
        "breaker_down_walk3.png": (down_frames[2], None),
        "breaker_down_walk4.png": (down_frames[3], None),
        "breaker_side_walk1.png": (side_frames[0], None),
        "breaker_side_walk2.png": (side_frames[1], None),
        "breaker_side_walk3.png": (side_frames[2], None),
        "breaker_side_walk4.png": (side_frames[3], None),
        "breaker_up_walk1.png": (up_frames[0], None),
        "breaker_up_walk2.png": (up_frames[1], None),
        "breaker_up_walk3.png": (up_frames[2], None),
        "breaker_up_walk4.png": (up_frames[3], None),
        "breaker_attack1.png": (atk_frames[0], None),
        "breaker_attack2.png": (atk_frames[1], None),
        "breaker_attack3.png": (atk_frames[2], None),
        "breaker_attack4.png": (atk_frames[3], None),
        "breaker_die1.png": (die_frames[0], None),
        "breaker_die2.png": (die_frames[1], None),
        "breaker_die3.png": (die_frames[2], None),
        "breaker_die4.png": (die_frames[3], None),
        "breaker_hurt.png": (hurt_frame, "enemy_breaker_hurt.png"),
        "breaker_down.png": (down_frames[0], "enemy_breaker_down.png"),
        "breaker_idle.png": (down_frames[0], None),
        "breaker_side.png": (side_frames[0], "enemy_breaker_side.png"),
        "breaker_left.png": (side_frames[0].transpose(Image.FLIP_LEFT_RIGHT), None),
        "breaker_right.png": (side_frames[0], None),
        "breaker_up.png": (up_frames[0], "enemy_breaker_up.png"),
        "breaker_attack.png": (atk_frames[1], None),
        "breaker_die.png": (die_frames[3], None)
    }

    saved_sprites = {}
    for fname, (img, alias) in breaker_map.items():
        saved_sprites[fname] = verify_and_save(img, BREAKER_DIR, fname, legacy_alias=alias)

    make_gif([saved_sprites[f"breaker_down_walk{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "breaker_down_walk.gif"))
    make_gif([saved_sprites[f"breaker_side_walk{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "breaker_side_walk.gif"))
    make_gif([saved_sprites[f"breaker_up_walk{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "breaker_up_walk.gif"))
    make_gif([saved_sprites[f"breaker_attack{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "breaker_attack.gif"), duration=180)
    make_gif([saved_sprites[f"breaker_die{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "breaker_die.gif"), duration=250)

    make_matrix_showcase([
        ("Down Walk", [saved_sprites[f"breaker_down_walk{i}.png"] for i in range(1, 5)]),
        ("Side Walk", [saved_sprites[f"breaker_side_walk{i}.png"] for i in range(1, 5)]),
        ("Up Walk", [saved_sprites[f"breaker_up_walk{i}.png"] for i in range(1, 5)]),
        ("Attack", [saved_sprites[f"breaker_attack{i}.png"] for i in range(1, 5)]),
        ("Death", [saved_sprites[f"breaker_die{i}.png"] for i in range(1, 5)])
    ], os.path.join(ARTIFACT_DIR, "breaker_animation_matrix.png"))



# ===========================================================================
# 3. ENEMY_PEST (CHUỘT GẶM KHO / BARN PEST)
# ===========================================================================
def build_pest():
    print("\n==================== [3/3] BUILDING ENEMY_PEST ====================")
    C_FUR = (130, 100, 75, 255)
    C_FUR_SH = (75, 52, 38, 255)
    C_BELLY = (195, 170, 145, 255)
    C_PINK = (235, 160, 160, 255)
    C_PINK_SH = (185, 115, 115, 255)
    C_EYE = (235, 30, 30, 255)
    C_TEETH = (255, 245, 180, 255)

    def draw_pest_down(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = -1 if (frame_idx % 2 == 1) else 0

        paw_l_y = 59 if frame_idx == 0 else (57 if frame_idx == 2 else 58)
        paw_r_y = 59 if frame_idx == 2 else (57 if frame_idx == 0 else 58)
        tail_ang = -2 if frame_idx == 0 else (2 if frame_idx == 2 else 0)

        # 1. Tail undulating behind
        d.line([(32, 48 + bob), (34 + tail_ang, 42 + bob), (30 + tail_ang * 2, 36 + bob)], fill=C_PINK, width=2)

        # 2. Body (Chubby rat torso)
        d.ellipse([22, 46 + bob, 42, 58 + bob], fill=C_FUR, outline=C_FUR_SH)
        d.ellipse([26, 48 + bob, 38, 56 + bob], fill=C_BELLY)

        # 3. Pink pointed ears
        d.polygon([(23, 42 + bob), (27, 47 + bob), (22, 48 + bob)], fill=C_PINK, outline=C_FUR_SH)
        d.polygon([(41, 42 + bob), (37, 47 + bob), (42, 48 + bob)], fill=C_PINK, outline=C_FUR_SH)

        # 4. Snout, red eyes, and twitching whiskers
        d.polygon([(28, 50 + bob), (32, 56 + bob), (36, 50 + bob)], fill=C_FUR_SH)
        d.point((32, 56 + bob), fill=C_PINK) # Nose
        d.point((27, 49 + bob), fill=C_EYE)
        d.point((37, 49 + bob), fill=C_EYE)
        # Whiskers
        d.line([(24, 52 + bob), (18, 51 + bob)], fill=(220, 220, 220, 200))
        d.line([(40, 52 + bob), (46, 51 + bob)], fill=(220, 220, 220, 200))

        # 5. Paws on ground
        d.rectangle([21, paw_l_y - 3, 25, paw_l_y], fill=C_PINK, outline=C_PINK_SH)
        d.rectangle([39, paw_r_y - 3, 43, paw_r_y], fill=C_PINK, outline=C_PINK_SH)
        return img

    def draw_pest_side(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = -1 if (frame_idx % 2 == 1) else 0

        paw_f_y = 59 if frame_idx == 0 else 58
        paw_b_y = 57 if frame_idx == 0 else (59 if frame_idx == 2 else 58)
        tail_h = 38 if frame_idx == 0 else (42 if frame_idx == 2 else 40)

        # 1. Long curved pink tail
        tail_pts = [(10, tail_h), (14, tail_h + 3), (18, 47 + bob), (24, 52 + bob)]
        d.line(tail_pts, fill=C_PINK, width=2)

        # 2. Horizontal body
        d.ellipse([22, 45 + bob, 46, 57 + bob], fill=C_FUR, outline=C_FUR_SH)
        d.ellipse([26, 48 + bob, 44, 56 + bob], fill=C_BELLY)

        # 3. Head & Snout pointing right
        d.polygon([(40, 47 + bob), (53, 52 + bob), (44, 56 + bob)], fill=C_FUR, outline=C_FUR_SH)
        d.point((53, 52 + bob), fill=C_PINK)
        d.polygon([(41, 42 + bob), (45, 46 + bob), (39, 47 + bob)], fill=C_PINK, outline=C_FUR_SH)
        d.point((46, 49 + bob), fill=C_EYE)
        # Whiskers
        d.line([(48, 53 + bob), (56, 51 + bob)], fill=(220, 220, 220, 200))
        d.line([(48, 54 + bob), (55, 56 + bob)], fill=(220, 220, 220, 200))

        # 4. Paws
        d.rectangle([25, paw_b_y - 3, 29, paw_b_y], fill=C_PINK, outline=C_PINK_SH)
        d.rectangle([39, paw_f_y - 3, 43, paw_f_y], fill=C_PINK, outline=C_PINK_SH)
        return img

    def draw_pest_up(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        bob = -1 if (frame_idx % 2 == 1) else 0

        paw_l_y = 59 if frame_idx == 0 else (57 if frame_idx == 2 else 58)
        paw_r_y = 59 if frame_idx == 2 else (57 if frame_idx == 0 else 58)
        tail_sway = -3 if frame_idx == 0 else (3 if frame_idx == 2 else 0)

        # 1. Tail trailing behind downwards
        d.line([(32, 52 + bob), (32 + tail_sway, 57 + bob), (32 + tail_sway * 2, 59)], fill=C_PINK, width=2)

        # 2. Body from behind
        d.ellipse([22, 46 + bob, 42, 58 + bob], fill=C_FUR, outline=C_FUR_SH)

        # 3. Ears
        d.polygon([(23, 42 + bob), (27, 47 + bob), (22, 48 + bob)], fill=C_FUR_SH)
        d.polygon([(41, 42 + bob), (37, 47 + bob), (42, 48 + bob)], fill=C_FUR_SH)

        # 4. Paws
        d.rectangle([21, paw_l_y - 3, 25, paw_l_y], fill=C_PINK, outline=C_PINK_SH)
        d.rectangle([39, paw_r_y - 3, 43, paw_r_y], fill=C_PINK, outline=C_PINK_SH)
        return img

    def draw_pest_attack(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)

        if frame_idx == 0:
            # Rear up on hind legs, baring sharp yellow incisors
            d.line([(22, 57), (14, 52), (10, 44)], fill=C_PINK, width=2)
            d.ellipse([24, 38, 42, 56], fill=C_FUR, outline=C_FUR_SH)
            d.ellipse([27, 42, 39, 53], fill=C_BELLY)
            # Head raised high
            d.polygon([(26, 32), (32, 26), (38, 32)], fill=C_FUR, outline=C_FUR_SH)
            d.polygon([(23, 28), (27, 33), (22, 34)], fill=C_PINK)
            d.polygon([(41, 28), (37, 33), (42, 34)], fill=C_PINK)
            d.point([(29, 31), (35, 31)], fill=C_EYE)
            # Bared fangs
            d.rectangle([31, 33, 33, 36], fill=C_TEETH)
            # Raised front claws
            d.line([(22, 42), (18, 36)], fill=C_PINK, width=2)
            d.line([(42, 42), (46, 36)], fill=C_PINK, width=2)
            # Hind paws on ground
            d.rectangle([25, 55, 29, 59], fill=C_PINK, outline=C_PINK_SH)
            d.rectangle([37, 55, 41, 59], fill=C_PINK, outline=C_PINK_SH)

        elif frame_idx == 1:
            # Lunge bite snap forward with teeth slash VFX
            d.line([(10, 48), (18, 52), (24, 54)], fill=C_PINK, width=2)
            d.ellipse([24, 46, 48, 58], fill=C_FUR, outline=C_FUR_SH)
            d.polygon([(42, 46), (56, 50), (46, 56)], fill=C_FUR, outline=C_FUR_SH)
            d.point((48, 48), fill=C_EYE)
            # Big open jaws with sharp teeth
            d.polygon([(52, 48), (56, 47), (54, 50)], fill=C_TEETH)
            d.polygon([(52, 53), (56, 54), (54, 51)], fill=C_TEETH)
            # Bite strike VFX (arc teeth gleam)
            d.arc([46, 42, 60, 58], start=280, end=80, fill=(255, 255, 255, 255), width=2)
            # Paws lunging
            d.rectangle([26, 55, 30, 59], fill=C_PINK)
            d.rectangle([42, 55, 46, 59], fill=C_PINK)

        elif frame_idx == 2:
            # Chomp clamping down, impact sparks
            d.line([(10, 46), (18, 51), (24, 54)], fill=C_PINK, width=2)
            d.ellipse([24, 46, 48, 58], fill=C_FUR, outline=C_FUR_SH)
            d.polygon([(42, 48), (54, 52), (46, 56)], fill=C_FUR, outline=C_FUR_SH)
            d.point((47, 49), fill=C_EYE)
            # Clamped teeth
            d.rectangle([50, 51, 53, 53], fill=C_TEETH)
            # Impact sparks
            d.point([(56, 48), (58, 52), (56, 56)], fill=(255, 220, 80, 255))
            d.point((57, 52), fill=(255, 255, 255, 255))
            d.rectangle([27, 55, 31, 59], fill=C_PINK)
            d.rectangle([41, 55, 45, 59], fill=C_PINK)

        elif frame_idx == 3:
            # Recovery landing
            d.line([(12, 44), (18, 48), (24, 53)], fill=C_PINK, width=2)
            d.ellipse([22, 46, 46, 58], fill=C_FUR, outline=C_FUR_SH)
            d.polygon([(40, 48), (52, 53), (44, 57)], fill=C_FUR, outline=C_FUR_SH)
            d.point((46, 50), fill=C_EYE)
            d.rectangle([25, 55, 29, 59], fill=C_PINK)
            d.rectangle([39, 55, 43, 59], fill=C_PINK)
        return img

    def draw_pest_die(frame_idx=0):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)

        if frame_idx == 0:
            # Screech thrash
            d.line([(24, 56), (18, 48), (14, 34)], fill=C_PINK, width=2) # tail stiff upright
            d.ellipse([22, 42, 42, 56], fill=C_FUR, outline=C_FUR_SH)
            d.polygon([(26, 36), (32, 30), (38, 36)], fill=C_FUR, outline=C_FUR_SH)
            d.line([(29, 34), (35, 34)], fill=(255, 255, 255, 255))
            d.rectangle([24, 55, 28, 59], fill=C_PINK)
            d.rectangle([36, 55, 40, 59], fill=C_PINK)

        elif frame_idx == 1:
            # Sideways roll
            d.line([(14, 54), (20, 52), (26, 54)], fill=C_PINK, width=2)
            d.ellipse([22, 46, 46, 57], fill=C_FUR, outline=C_FUR_SH)
            d.polygon([(40, 48), (50, 53), (43, 56)], fill=C_FUR, outline=C_FUR_SH)
            d.line([(45, 50), (48, 50)], fill=(255, 255, 255, 255))
            d.rectangle([28, 55, 32, 59], fill=C_PINK)
            d.rectangle([38, 55, 42, 59], fill=C_PINK)

        elif frame_idx == 2:
            # Rolling onto back, paws curled up in air
            d.line([(12, 57), (18, 56), (24, 57)], fill=C_PINK, width=2)
            d.ellipse([22, 49, 44, 58], fill=C_FUR, outline=C_FUR_SH)
            d.ellipse([25, 50, 41, 57], fill=C_BELLY)
            # Paws sticking up in air
            d.rectangle([26, 45, 28, 49], fill=C_PINK)
            d.rectangle([30, 45, 32, 49], fill=C_PINK)
            d.rectangle([35, 45, 37, 49], fill=C_PINK)
            d.rectangle([39, 45, 41, 49], fill=C_PINK)
            # Head sideways
            d.polygon([(42, 50), (49, 54), (43, 57)], fill=C_FUR, outline=C_FUR_SH)
            d.point((45, 52), fill=(100, 100, 100, 255))

        elif frame_idx == 3:
            # Motionless flat on back
            d.line([(10, 58), (18, 58), (24, 58)], fill=C_PINK_SH, width=2)
            d.ellipse([22, 52, 44, 59], fill=C_FUR, outline=C_FUR_SH)
            d.ellipse([25, 53, 41, 58], fill=C_BELLY)
            d.point([(27, 51), (31, 51), (36, 51), (40, 51)], fill=C_PINK_SH)
            d.polygon([(43, 53), (50, 56), (44, 59)], fill=C_FUR, outline=C_FUR_SH)
        return img

    def draw_pest_hurt():
        base = draw_pest_down(frame_idx=0)
        d = ImageDraw.Draw(base)
        d.line([(32, 44), (32, 50)], fill=(255, 230, 80, 255), width=2)
        d.line([(29, 47), (35, 47)], fill=(255, 230, 80, 255), width=2)
        d.line([(30, 45), (34, 49)], fill=(255, 255, 255, 255), width=1)
        d.line([(30, 49), (34, 45)], fill=(255, 255, 255, 255), width=1)
        return base

    down_frames = [draw_pest_down(i) for i in range(4)]
    side_frames = [draw_pest_side(i) for i in range(4)]
    up_frames = [draw_pest_up(i) for i in range(4)]
    atk_frames = [draw_pest_attack(i) for i in range(4)]
    die_frames = [draw_pest_die(i) for i in range(4)]
    hurt_frame = draw_pest_hurt()

    pest_map = {
        "pest_down_walk1.png": (down_frames[0], None),
        "pest_down_walk2.png": (down_frames[1], None),
        "pest_down_walk3.png": (down_frames[2], None),
        "pest_down_walk4.png": (down_frames[3], None),
        "pest_side_walk1.png": (side_frames[0], None),
        "pest_side_walk2.png": (side_frames[1], None),
        "pest_side_walk3.png": (side_frames[2], None),
        "pest_side_walk4.png": (side_frames[3], None),
        "pest_up_walk1.png": (up_frames[0], None),
        "pest_up_walk2.png": (up_frames[1], None),
        "pest_up_walk3.png": (up_frames[2], None),
        "pest_up_walk4.png": (up_frames[3], None),
        "pest_attack1.png": (atk_frames[0], None),
        "pest_attack2.png": (atk_frames[1], None),
        "pest_attack3.png": (atk_frames[2], None),
        "pest_attack4.png": (atk_frames[3], None),
        "pest_die1.png": (die_frames[0], None),
        "pest_die2.png": (die_frames[1], None),
        "pest_die3.png": (die_frames[2], None),
        "pest_die4.png": (die_frames[3], None),
        "pest_hurt.png": (hurt_frame, "enemy_pest_hurt.png"),
        "pest_down.png": (down_frames[0], "enemy_pest_down.png"),
        "pest_idle.png": (down_frames[0], None),
        "pest_side.png": (side_frames[0], "enemy_pest_side.png"),
        "pest_left.png": (side_frames[0].transpose(Image.FLIP_LEFT_RIGHT), None),
        "pest_right.png": (side_frames[0], None),
        "pest_up.png": (up_frames[0], "enemy_pest_up.png"),
        "pest_attack.png": (atk_frames[1], None),
        "pest_die.png": (die_frames[3], None)
    }

    saved_sprites = {}
    for fname, (img, alias) in pest_map.items():
        saved_sprites[fname] = verify_and_save(img, PEST_DIR, fname, legacy_alias=alias)

    make_gif([saved_sprites[f"pest_down_walk{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "pest_down_walk.gif"))
    make_gif([saved_sprites[f"pest_side_walk{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "pest_side_walk.gif"))
    make_gif([saved_sprites[f"pest_up_walk{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "pest_up_walk.gif"))
    make_gif([saved_sprites[f"pest_attack{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "pest_attack.gif"), duration=180)
    make_gif([saved_sprites[f"pest_die{i}.png"] for i in range(1, 5)], os.path.join(ARTIFACT_DIR, "pest_die.gif"), duration=250)

    make_matrix_showcase([
        ("Down Walk", [saved_sprites[f"pest_down_walk{i}.png"] for i in range(1, 5)]),
        ("Side Walk", [saved_sprites[f"pest_side_walk{i}.png"] for i in range(1, 5)]),
        ("Up Walk", [saved_sprites[f"pest_up_walk{i}.png"] for i in range(1, 5)]),
        ("Attack", [saved_sprites[f"pest_attack{i}.png"] for i in range(1, 5)]),
        ("Death", [saved_sprites[f"pest_die{i}.png"] for i in range(1, 5)])
    ], os.path.join(ARTIFACT_DIR, "pest_animation_matrix.png"))


if __name__ == "__main__":
    print("==================================================================")
    print("  PIG TYCOON: BASELINE ENEMIES 4-FRAME ANIMATION & ZERO-CLIPPING  ")
    print("==================================================================")
    build_thief()
    build_breaker()
    build_pest()
    print("\n==================================================================")
    print("  ALL 3 BASELINE ENEMIES BUILT SUCCESSFULLY!                     ")
    print("==================================================================")
