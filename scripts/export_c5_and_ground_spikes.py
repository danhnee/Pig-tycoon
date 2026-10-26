#!/usr/bin/env python3
import os, math, uuid, re
from PIL import Image, ImageDraw

def render_bone_spire_frame(f_idx):
    # f_idx: 0=idle, 1=charge, 2=fire/slam, 3=cooldown
    FRAME_W, FRAME_H = 64, 64
    im = Image.new("RGBA", (FRAME_W, FRAME_H), (0, 0, 0, 0))

    # Palettes
    c_out = (26, 21, 38, 255)
    c_earth = (38, 30, 48, 255)
    c_earth_light = (62, 50, 76, 255)
    c_b_white = (248, 245, 238, 255)
    c_b_light = (228, 220, 202, 255)
    c_b_mid = (195, 182, 155, 255)
    c_b_shad = (138, 120, 98, 255)
    c_b_dark = (82, 68, 56, 255)
    
    c_h_light = (165, 148, 130, 255)
    c_h_mid = (115, 98, 85, 255)
    c_h_dark = (65, 52, 45, 255)

    c_cry_hi = (250, 232, 255, 255)
    c_cry_light = (236, 72, 153, 255)
    c_cry_mid = (168, 85, 247, 255)
    c_cry_dark = (126, 34, 206, 255)

    c_dim_hi = (156, 163, 175, 255)
    c_dim_mid = (107, 114, 128, 255)
    c_dim_dark = (55, 65, 81, 255)

    # Base earth mound (Y: 50..58)
    for ex in range(16, 48):
        dx = abs(ex - 32)
        ey_max = 58 - int(dx * 0.18)
        ey_min = 52 + int((dx/16)**2 * 2)
        for ey in range(ey_min, ey_max):
            col = c_earth_light if ey == ey_min else c_earth
            im.putpixel((ex, ey), col)
            if ey == ey_max - 1 or ey == ey_min or ex in (16, 47):
                im.putpixel((ex, ey), c_out)

    # Base ribs anchoring into earth (3 pairs of ribs)
    rib_pairs = [
        (21, 52, 25, 45, 30, 48), # Left rib 1
        (42, 52, 38, 45, 34, 48), # Right rib 1
        (19, 54, 23, 47, 29, 50), # Left rib 2
        (44, 54, 40, 47, 35, 50), # Right rib 2
        (17, 56, 22, 51, 28, 53), # Left rib 3
        (46, 56, 41, 51, 36, 53), # Right rib 3
    ]
    for x0, y0, x1, y1, x2, y2 in rib_pairs:
        for t_step in range(15):
            t = t_step / 14.0
            bx = int((1-t)**2 * x0 + 2*(1-t)*t * x1 + t**2 * x2)
            by = int((1-t)**2 * y0 + 2*(1-t)*t * y1 + t**2 * y2)
            im.putpixel((bx, by), c_b_mid)
            im.putpixel((bx, by-1), c_b_white)
            im.putpixel((bx, by+1), c_out)

    # Spine vertebrae
    spine_top = 28
    spine_bottom = 50
    if f_idx == 1: spine_top = 26
    elif f_idx == 2: spine_top = 33
    elif f_idx == 3: spine_top = 29

    num_vert = 5
    vert_h = (spine_bottom - spine_top) / num_vert
    for v in range(num_vert):
        vy = spine_top + v * vert_h
        vh = max(2, int(vert_h - 1))
        vw = 10 if f_idx != 2 else 12
        vx0 = 32 - vw // 2
        vx1 = 32 + vw // 2
        for y in range(int(vy), int(vy + vh)):
            for x in range(vx0, vx1):
                if x == vx0 or x == vx1 - 1 or y == int(vy) or y == int(vy + vh - 1):
                    im.putpixel((x, y), c_out)
                elif x < 31:
                    im.putpixel((x, y), c_b_white if y == int(vy) + 1 else c_b_light)
                elif x in (31, 32):
                    im.putpixel((x, y), c_b_mid)
                else:
                    im.putpixel((x, y), c_b_shad)
        im.putpixel((vx0 - 1, int(vy + vh//2)), c_b_light)
        im.putpixel((vx0 - 2, int(vy + vh//2)), c_out)
        im.putpixel((vx1, int(vy + vh//2)), c_b_shad)
        im.putpixel((vx1 + 1, int(vy + vh//2)), c_out)

    # Skull position
    skull_y_map = [14, 9, 20, 15]
    sy = skull_y_map[f_idx]

    # Horns
    l_pts = [(26, sy + 6), (22, sy + 3), (19, sy - 1), (18, sy - 5), (20, sy - 8), (23, sy - 7)]
    r_pts = [(37, sy + 6), (41, sy + 3), (44, sy - 1), (45, sy - 5), (43, sy - 8), (40, sy - 7)]
    for pts, is_left in [(l_pts, True), (r_pts, False)]:
        for i in range(len(pts)-1):
            p0, p1 = pts[i], pts[i+1]
            steps = 10
            for s in range(steps):
                t = s / float(steps)
                hx = int(p0[0] * (1-t) + p1[0] * t)
                hy = int(p0[1] * (1-t) + p1[1] * t)
                im.putpixel((hx, hy), c_h_light if is_left else c_h_mid)
                im.putpixel((hx, hy+1), c_h_dark)
                im.putpixel((hx-1, hy), c_out)
                im.putpixel((hx+1, hy), c_out)

    # Cranium
    for y in range(sy, sy + 10):
        dy = y - sy
        w_at_y = 12 if dy > 2 else 8 + dy * 2
        sx0 = 32 - w_at_y // 2
        sx1 = 32 + w_at_y // 2
        for x in range(sx0, sx1):
            if x == sx0 or x == sx1 - 1 or y == sy or y == sy + 9:
                im.putpixel((x, y), c_out)
            elif x < 31:
                im.putpixel((x, y), c_b_white if y < sy + 4 else c_b_light)
            elif x in (31, 32):
                im.putpixel((x, y), c_b_mid)
            else:
                im.putpixel((x, y), c_b_shad)

    # Eye Sockets
    eye_col = (192, 38, 211, 255) if f_idx in (0, 1, 2) else (45, 40, 55, 255)
    eye_glow = (245, 208, 254, 255) if f_idx in (1, 2) else ((147, 51, 234, 255) if f_idx == 0 else (65, 60, 75, 255))
    for ex in (28, 35):
        im.putpixel((ex, sy + 5), c_out)
        im.putpixel((ex, sy + 6), eye_col)
        im.putpixel((ex+1 if ex==28 else ex-1, sy + 6), eye_glow)
        im.putpixel((ex, sy + 7), c_out)

    # Nasal cavity
    im.putpixel((31, sy + 7), c_out); im.putpixel((32, sy + 7), c_out)

    # Jaws & Mystic Crystal
    if f_idx == 1: # Charge
        im.putpixel((29, sy + 10), c_b_white); im.putpixel((29, sy + 11), c_b_white)
        im.putpixel((34, sy + 10), c_b_white); im.putpixel((34, sy + 11), c_b_white)
        for lx in range(28, 36):
            im.putpixel((lx, sy + 15), c_b_mid)
            im.putpixel((lx, sy + 16), c_out)
        im.putpixel((29, sy + 14), c_b_white)
        im.putpixel((34, sy + 14), c_b_white)
        for cy in range(sy + 8, sy + 14):
            cdy = cy - (sy + 8)
            cw = 4 if (1 <= cdy <= 3) else 2
            for cx in range(32 - cw//2, 32 + cw//2):
                im.putpixel((cx, cy), c_cry_hi if cx in (31, 32) and cy == sy+10 else c_cry_light)
        for fx, fy in [(26, sy+10), (37, sy+10), (31, sy+6), (32, sy+6), (31, sy+15), (32, sy+15)]:
            im.putpixel((fx, fy), (245, 208, 254, 220))
        for fx, fy in [(27, sy+10), (36, sy+10), (31, sy+7), (32, sy+7)]:
            im.putpixel((fx, fy), (236, 72, 153, 240))

    elif f_idx == 2: # Fire/Slam
        for lx in range(28, 36):
            im.putpixel((lx, sy + 10), c_b_mid)
            im.putpixel((lx, sy + 11), c_b_shad)
            im.putpixel((lx, sy + 12), c_out)
        im.putpixel((31, sy + 10), c_cry_hi); im.putpixel((32, sy + 10), c_cry_hi)
        im.putpixel((31, sy + 11), c_cry_mid); im.putpixel((32, sy + 11), c_cry_mid)

        # Circular purple ground shockwave (No side spikes clipped!)
        for deg in range(0, 360, 5):
            rad = math.radians(deg)
            rx = int(32 + 20 * math.cos(rad))
            ry = int(55 + 6.5 * math.sin(rad))
            if 2 <= rx < 62 and 40 <= ry < 63:
                im.putpixel((rx, ry), (243, 232, 255, 240) if deg % 30 < 15 else (192, 132, 252, 200))
                rx2 = int(32 + 22 * math.cos(rad))
                ry2 = int(55 + 7.5 * math.sin(rad))
                if 0 <= rx2 < 64 and 0 <= ry2 < 64:
                    im.putpixel((rx2, ry2), (168, 85, 247, 140))
        for cx, cy in [(24, 54), (28, 56), (32, 57), (36, 56), (40, 54)]:
            im.putpixel((cx, cy), (250, 232, 255, 255))
            im.putpixel((cx, cy-1), (217, 70, 239, 220))

    elif f_idx == 0: # Idle
        for lx in range(29, 35):
            im.putpixel((lx, sy + 10), c_b_mid)
            im.putpixel((lx, sy + 11), c_b_shad)
            im.putpixel((lx, sy + 12), c_out)
        im.putpixel((31, sy + 10), c_cry_hi); im.putpixel((32, sy + 10), c_cry_light)
        im.putpixel((31, sy + 11), c_cry_mid); im.putpixel((32, sy + 11), c_cry_dark)
        for mx, my in [(22, 57), (25, 58), (38, 57), (41, 58), (30, 59), (34, 59)]:
            im.putpixel((mx, my), (192, 132, 252, 120))
            im.putpixel((mx, my-1), (147, 51, 234, 80))

    elif f_idx == 3: # Cooldown
        for lx in range(29, 35):
            im.putpixel((lx, sy + 10), c_b_mid)
            im.putpixel((lx, sy + 11), c_b_shad)
            im.putpixel((lx, sy + 12), c_out)
        im.putpixel((31, sy + 10), c_dim_hi); im.putpixel((32, sy + 10), c_dim_mid)
        im.putpixel((31, sy + 11), c_dim_mid); im.putpixel((32, sy + 11), c_dim_dark)
        smoke_pts = [(30, sy - 3), (29, sy - 6), (31, sy - 9), (34, sy - 4), (35, sy - 7)]
        for sm_x, sm_y in smoke_pts:
            if 0 <= sm_x < 64 and 0 <= sm_y < 64:
                im.putpixel((sm_x, sm_y), (147, 51, 234, 130))
                im.putpixel((sm_x+1, sm_y), (192, 132, 252, 90))

    return im

def render_ground_spikes_frame(f_idx):
    FRAME_W, FRAME_H = 64, 64
    im = Image.new("RGBA", (FRAME_W, FRAME_H), (0, 0, 0, 0))

    c_out = (26, 21, 38, 255)
    c_earth = (38, 30, 48, 255)
    c_earth_light = (62, 50, 76, 255)
    c_b_white = (252, 250, 242, 255)
    c_b_light = (235, 226, 208, 255)
    c_b_mid = (200, 186, 158, 255)
    c_b_shad = (142, 124, 102, 255)
    c_b_dark = (86, 72, 60, 255)

    c_v_core = (250, 232, 255, 255)
    c_v_hi = (240, 171, 252, 240)
    c_v_mid = (192, 38, 211, 220)
    c_v_deep = (126, 34, 206, 180)
    c_v_mist = (168, 85, 247, 100)

    if f_idx == 0:
        mist_points = [
            (24, 44), (28, 42), (32, 41), (36, 42), (40, 44),
            (20, 46), (26, 48), (32, 47), (38, 48), (44, 46),
            (22, 50), (32, 51), (42, 50)
        ]
        for mx, my in mist_points:
            for dx in range(-2, 3):
                for dy in range(-1, 2):
                    if 0 <= mx+dx < 64 and 0 <= my+dy < 64:
                        im.putpixel((mx+dx, my+dy), c_v_mist)

        fissures = [
            [(32, 46), (28, 44), (24, 45), (19, 43), (15, 45)],
            [(32, 46), (36, 45), (41, 43), (46, 44), (50, 42)],
            [(32, 46), (33, 49), (31, 52), (29, 54)],
            [(32, 46), (35, 48), (38, 51), (42, 53)],
            [(28, 44), (27, 41), (25, 40)],
            [(41, 43), (43, 40), (45, 39)],
        ]
        for branch in fissures:
            for i in range(len(branch)-1):
                p0, p1 = branch[i], branch[i+1]
                steps = 8
                for s in range(steps):
                    t = s / float(steps)
                    fx = int(p0[0] * (1-t) + p1[0] * t)
                    fy = int(p0[1] * (1-t) + p1[1] * t)
                    im.putpixel((fx, fy), c_v_core)
                    im.putpixel((fx, fy-1), c_v_mid)
                    im.putpixel((fx, fy+1), c_out)

    elif f_idx == 1:
        for cx in range(20, 45):
            im.putpixel((cx, 48), c_earth)
            im.putpixel((cx, 49), c_out)
        dirt = [(22, 43), (25, 41), (39, 42), (43, 44)]
        for dx, dy in dirt:
            im.putpixel((dx, dy), c_earth_light)
            im.putpixel((dx, dy+1), c_out)

        for y in range(31, 47):
            t = (y - 31) / 16.0
            w = max(1, int(t * 7))
            x0, x1 = 32 - w//2, 32 + w//2
            for x in range(x0, x1):
                if x == x0 or x == x1 - 1 or y == 31:
                    im.putpixel((x, y), c_out)
                elif x < 32:
                    im.putpixel((x, y), c_b_white if y < 35 else c_b_light)
                else:
                    im.putpixel((x, y), c_b_mid if x == 32 else c_b_shad)
        for y in range(36, 47):
            t = (y - 36) / 11.0
            w = max(1, int(t * 5))
            offset_x = int(-2 - (1-t)*3)
            x0, x1 = 25 + offset_x - w//2, 25 + offset_x + w//2
            for x in range(x0, x1):
                im.putpixel((x, y), c_b_light if x <= 25+offset_x else c_b_shad)
                if x == x0 or x == x1-1 or y == 36:
                    im.putpixel((x, y), c_out)
        for y in range(35, 47):
            t = (y - 35) / 12.0
            w = max(1, int(t * 5))
            offset_x = int(2 + (1-t)*3)
            x0, x1 = 39 + offset_x - w//2, 39 + offset_x + w//2
            for x in range(x0, x1):
                im.putpixel((x, y), c_b_light if x <= 39+offset_x else c_b_shad)
                if x == x0 or x == x1-1 or y == 35:
                    im.putpixel((x, y), c_out)

        for tx, ty in [(32, 29), (31, 30), (33, 30), (22, 34), (43, 33)]:
            im.putpixel((tx, ty), c_v_hi)

    elif f_idx == 2:
        for ex in range(12, 53):
            im.putpixel((ex, 52), c_earth_light)
            im.putpixel((ex, 53), c_earth)
            im.putpixel((ex, 54), c_out)

        def draw_jagged_spike(tip_x, tip_y, base_x, base_y, max_w, is_left_light=True):
            dy_total = base_y - tip_y
            for y in range(tip_y, base_y):
                progress = (y - tip_y) / float(dy_total)
                serration = int(math.sin(progress * 18.0) * 1.5)
                w = max(1, int(progress * max_w) + serration)
                cur_cx = int(tip_x * (1 - progress) + base_x * progress)
                x0, x1 = cur_cx - w // 2, cur_cx + w // 2
                for x in range(x0, x1):
                    if x == x0 or x == x1 - 1 or y == tip_y:
                        im.putpixel((x, y), c_out)
                    elif is_left_light:
                        if x < cur_cx:
                            im.putpixel((x, y), c_b_white if progress < 0.25 else c_b_light)
                        elif x == cur_cx:
                            im.putpixel((x, y), c_b_mid)
                        else:
                            im.putpixel((x, y), c_b_shad if x < x1 - 2 else c_b_dark)
                    else:
                        if x > cur_cx:
                            im.putpixel((x, y), c_b_light)
                        else:
                            im.putpixel((x, y), c_b_shad)
                if progress < 0.8:
                    aura_x_l = x0 - 1
                    aura_x_r = x1
                    if 0 <= aura_x_l < 64:
                        im.putpixel((aura_x_l, y), c_v_mid if y % 2 == 0 else c_v_hi)
                    if 0 <= aura_x_r < 64:
                        im.putpixel((aura_x_r, y), c_v_deep if y % 2 == 1 else c_v_mid)

        draw_jagged_spike(30, 8, 33, 52, 14, is_left_light=True)
        draw_jagged_spike(15, 18, 24, 52, 10, is_left_light=True)
        draw_jagged_spike(49, 16, 41, 52, 11, is_left_light=False)
        draw_jagged_spike(10, 36, 18, 52, 6, is_left_light=True)
        draw_jagged_spike(55, 34, 46, 52, 6, is_left_light=False)

        for fx, fy in [(30, 6), (29, 7), (31, 7), (15, 16), (49, 14)]:
            if 0 <= fx < 64 and 0 <= fy < 64:
                im.putpixel((fx, fy), c_v_core)

    elif f_idx == 3:
        for ex in range(14, 51):
            im.putpixel((ex, 52), c_earth)
            im.putpixel((ex, 53), c_out)

        for y in range(26, 52):
            prog = (y - 26) / 26.0
            w = max(1, int(prog * 12))
            x0, x1 = 33 - w//2, 33 + w//2
            is_crack = (y in (32, 33, 40, 41))
            for x in range(x0, x1):
                if is_crack:
                    im.putpixel((x, y), c_v_mid if x % 2 == 0 else c_out)
                elif x == x0 or x == x1 - 1 or y == 26:
                    im.putpixel((x, y), c_out)
                else:
                    im.putpixel((x, y), c_b_shad if x > 33 else c_b_mid)

        for y in range(32, 52):
            prog = (y - 32) / 20.0
            w = max(1, int(prog * 8))
            for x in range(23 - w//2, 23 + w//2):
                im.putpixel((x, y), c_b_shad)
            for x in range(42 - w//2, 42 + w//2):
                im.putpixel((x, y), c_b_shad)

        shards = [
            (28, 14), (29, 14), (28, 15),
            (35, 12), (36, 12), (36, 13),
            (17, 22), (18, 23),
            (47, 20), (48, 20), (48, 21),
            (32, 20), (33, 21),
        ]
        for sx, sy in shards:
            if 0 <= sx < 64 and 0 <= sy < 64:
                im.putpixel((sx, sy), c_b_white)
                im.putpixel((sx, sy+1), c_out)

        for ex, ey in [(25, 18), (38, 16), (20, 28), (44, 26), (31, 24)]:
            if 0 <= ex < 64 and 0 <= ey < 64:
                im.putpixel((ex, ey), c_v_hi)

    return im

def main():
    # 1. Generate full sheets
    sheet_spire = Image.new("RGBA", (256, 64), (0, 0, 0, 0))
    sheet_spikes = Image.new("RGBA", (256, 64), (0, 0, 0, 0))

    spire_frames = [render_bone_spire_frame(i) for i in range(4)]
    spikes_frames = [render_ground_spikes_frame(i) for i in range(4)]

    for i in range(4):
        sheet_spire.paste(spire_frames[i], (i * 64, 0), spire_frames[i])
        sheet_spikes.paste(spikes_frames[i], (i * 64, 0), spikes_frames[i])

    sheet_spire_path = "Assets/Art/Sprites/Environment/Buildings/ancient_bone_spire_sheet.png"
    sheet_spikes_path = "Assets/Art/Sprites/VFX/Combat/ground_bone_spikes_sheet.png"
    sheet_spire.save(sheet_spire_path)
    sheet_spikes.save(sheet_spikes_path)
    print(f"Saved {sheet_spire_path}")
    print(f"Saved {sheet_spikes_path}")

    # 2. Slice Ancient Bone Spire into BioTowers/CotDiCotSuongHuyen_* on 160x112 canvas
    # Preserving exact Unity GUIDs
    guids_c5 = {
        "idle": "749ab51187c34afe9eb91ad4713f6de5",
        "charge": "47a3aacac54e418991ef1db15f6ad69f",
        "fire": "119a2fb0b8344eaa8d63c6cf249d382c",
        "cooldown": "d9d8e8a9b5514f95bd904b114cc3a75e"
    }
    suffixes = ["idle", "charge", "fire", "cooldown"]
    out_bio_dir = "Assets/Art/Sprites/Environment/Buildings/BioTowers"

    # Base on 160x112 canvas:
    # 64x64 frame base is at (32, 58)
    # Target canvas base is at (80, 96)
    paste_x = 80 - 32 # 48
    paste_y = 96 - 58 # 38

    for idx, suffix in enumerate(suffixes):
        canvas = Image.new("RGBA", (160, 112), (0, 0, 0, 0))
        frame = spire_frames[idx]
        canvas.paste(frame, (paste_x, paste_y), frame)
        
        png_path = os.path.join(out_bio_dir, f"CotDiCotSuongHuyen_{suffix}.png")
        canvas.save(png_path)
        print(f"  -> Exported {png_path} (bbox: {canvas.getbbox()})")

    # 3. Export 4 individual VFX sprites for Ground Bone Spikes in Assets/Art/Sprites/VFX/Combat/
    vfx_dir = "Assets/Art/Sprites/VFX/Combat"
    for idx in range(4):
        vfx_png = os.path.join(vfx_dir, f"vfx_ground_bone_spike_{idx}.png")
        spikes_frames[idx].save(vfx_png)

        # Generate .meta
        vfx_meta = vfx_png + ".meta"
        guid = uuid.uuid4().hex
        if os.path.exists(vfx_meta):
            with open(vfx_meta) as f:
                m = re.search(r"guid:\s*([a-f0-9]{32})", f.read())
                if m: guid = m.group(1)

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
  spritePivot: {{x: 0.5, y: 0.1875}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
"""
        with open(vfx_meta, "w") as f:
            f.write(meta_content)
        print(f"  -> Exported {vfx_png} + meta")

    print("\n✅ Successfully exported both sprite sheets and updated C5 + Ground Bone Spikes VFX!")

if __name__ == "__main__":
    main()
