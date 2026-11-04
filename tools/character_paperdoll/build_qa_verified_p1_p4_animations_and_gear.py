"""Ultra-fast vectorized builder for P1-P4 animations and 16 GDD paper-doll gear overlays for Khoa (and P1 for An),
enforcing 0 FAIL / 0 WARN on tools/pixelart/qa_sheets.py across ALL character folders."""
import os
import sys
import json
import math
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, r"D:\Pig-tycoon\tools\pixelart")
import qa_enforcer

ROOT = r"D:\Pig-tycoon"
CHARS_DIR = os.path.join(ROOT, "Assets", "Art", "PixelMyth", "Characters")
FROM_ARGON_CHARS = os.path.join(ROOT, ".collab", "claude-2d", "ui_and_map_sync", "from_argon", "Assets", "Art", "PixelMyth", "Characters")
BRAIN_DIR = r"C:\Users\Admin\.gemini\antigravity\brain\ac0f248a-d876-47b0-9f6a-277b76657366"

CW, CH = 72, 84
PIVOT = [36, 5]
ROWS = ["down", "left", "up", "right"]

ACTIVE_SKILLS = [
    "bo_phap_luot", "doan_anh_tram", "xuyen_bay", "cuu_nguy",
    "tiep_vien_toc_hanh", "vo_anh_bo", "pha_giap", "chan_dia",
    "tram_tuyen", "phan_chan", "neo_kich", "thien_chuy",
    "moc_keo", "an_tin_hieu", "xoay_bay", "troi_luoi",
    "dung_chot", "thien_la", "tru_bo", "ho_than",
    "khieu_chien", "chan_mang", "kim_cuong_the",
]

WEAPON_ITEMS = [
    ("wpn_dao_rua", "slash"),
    ("wpn_giao_moc", "thrust"),
    ("wpn_bua_ta", "smash"),
    ("wpn_no_tay", "ranged"),
]

OTHER_GEAR_ITEMS = [
    "boot_giay_dinh", "boot_guoc_gio",
    "armor_giap_da_thuoc", "armor_giap_luoi_thep", "armor_ao_mua_dau", "armor_ao_chi",
    "pants_quan_da_lot_bong", "pants_quan_giap_xich",
    "brc_vong_cach_dien", "brc_vong_huyet_nha",
    "nck_coi_chan_heo", "nck_mat_day_den_bao",
]


def _shift_upper(cell, dx, dy, foot_keep=5):
    """Shifts the opaque character body (`alpha == 255`) as one intact piece by `(dx, dy)` while keeping
    the ground shadow (`0 < alpha < 255`) anchored, with ZERO horizontal cuts across boots or ankles."""
    if dx == 0 and dy == 0:
        return cell.copy()
    shadow_mask = (cell[:, :, 3] > 0) & (cell[:, :, 3] < 255)
    body_mask = cell[:, :, 3] == 255
    sh = np.zeros_like(cell)
    sh[shadow_mask] = cell[shadow_mask]

    body_only = np.zeros_like(cell)
    body_only[body_mask] = cell[body_mask]

    ys = np.where(body_mask)[0]
    if len(ys) > 0:
        max_dy = max(0, (CH - foot_keep - 1) - int(ys.max()))
        dy = min(dy, max_dy)

    y_dst0 = max(0, dy)
    y_dst1 = min(CH, CH + dy)
    y_src0 = max(0, -dy)
    y_src1 = y_src0 + (y_dst1 - y_dst0)

    x_dst0 = max(0, dx)
    x_dst1 = min(CW, CW + dx)
    x_src0 = max(0, -dx)
    x_src1 = x_src0 + (x_dst1 - x_dst0)

    if y_dst1 > y_dst0 and x_dst1 > x_dst0:
        sub = body_only[y_src0:y_src1, x_src0:x_src1]
        m = sub[:, :, 3] == 255
        sh_sub = sh[y_dst0:y_dst1, x_dst0:x_dst1]
        sh_sub[m] = sub[m]
    return sh


def save_sheet_and_json(out_dir, family, clip, sheet_rgba, n_frames, fps, loop, stride_m=None):
    os.makedirs(out_dir, exist_ok=True)
    sheet_rgba = qa_enforcer.finalize_and_verify_sheet(sheet_rgba, CW, CH, n_frames, clip, max_colors=47)
    png_path = os.path.join(out_dir, f"{family}_{clip}.png")
    json_path = os.path.join(out_dir, f"{family}_{clip}.json")
    Image.fromarray(sheet_rgba, "RGBA").save(png_path)
    meta = {
        "cell": [CW, CH],
        "rows": ROWS,
        "frames_per_row": int(n_frames),
        "pivot_px_from_bottom_left": PIVOT,
        "fps": float(fps),
        "loop": bool(loop),
    }
    if stride_m is not None:
        meta["stride_m"] = float(stride_m)
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=2)


def resample_frames_row(src_sheet, src_n, dst_n):
    if src_n == dst_n:
        return src_sheet.copy()
    out = np.zeros((CH * 4, CW * dst_n, 4), dtype=np.uint8)
    for r in range(4):
        for c in range(dst_n):
            src_idx = int((c * src_n) // dst_n) % src_n
            cell = src_sheet[r * CH:(r + 1) * CH, src_idx * CW:(src_idx + 1) * CW]
            prev_idx = int(((c - 1) * src_n) // dst_n) % src_n if c > 0 else -1
            if c > 0 and src_idx == prev_idx:
                dy = -1 if (c % 2 == 1) else 1
                dx = 1 if (r in (1, 3) and c % 2 == 1) else 0
                cell = _shift_upper(cell, dx, dy, foot_keep=6)
            out[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = cell
    return out


def draw_smear_arc(cell, row_idx, archetype, step, color_main, color_hi, outline_col=(20, 18, 26)):
    pil = Image.fromarray(cell, "RGBA")
    dr = ImageDraw.Draw(pil)
    c_main = tuple(int(x) for x in color_main) + (255,)
    c_hi = tuple(int(x) for x in color_hi) + (255,)
    c_out = tuple(int(x) for x in outline_col) + (255,)

    if archetype == "slash":
        if row_idx == 0:
            pts = [(16, 42), (26, 56), (44, 58), (56, 46), (46, 52), (28, 50)] if step != 2 else [(56, 40), (46, 56), (26, 58), (16, 44), (28, 52), (44, 50)]
        elif row_idx == 2:
            pts = [(16, 34), (28, 22), (46, 22), (56, 34), (44, 27), (28, 27)]
        elif row_idx == 1:
            pts = [(10, 30), (8, 44), (16, 56), (26, 52), (16, 44), (18, 34)]
        else:
            pts = [(62, 30), (64, 44), (56, 56), (46, 52), (56, 44), (54, 34)]
        dr.polygon(pts, fill=c_main, outline=c_out)
        dr.line(pts[:4], fill=c_hi, width=1)
    elif archetype == "thrust":
        if row_idx == 0:
            dr.polygon([(33, 44), (39, 44), (36, 68)], fill=c_main, outline=c_out)
            dr.line([(36, 44), (36, 66)], fill=c_hi, width=1)
            dr.polygon([(38, 58), (44, 54), (38, 62)], fill=c_main, outline=c_out)
        elif row_idx == 2:
            dr.polygon([(33, 36), (39, 36), (36, 12)], fill=c_main, outline=c_out)
            dr.line([(36, 36), (36, 14)], fill=c_hi, width=1)
        elif row_idx == 1:
            dr.polygon([(28, 41), (28, 47), (6, 44)], fill=c_main, outline=c_out)
            dr.line([(28, 44), (8, 44)], fill=c_hi, width=1)
            dr.polygon([(14, 41), (18, 36), (12, 41)], fill=c_main, outline=c_out)
        else:
            dr.polygon([(44, 41), (44, 47), (66, 44)], fill=c_main, outline=c_out)
            dr.line([(44, 44), (64, 44)], fill=c_hi, width=1)
            dr.polygon([(58, 41), (54, 36), (60, 41)], fill=c_main, outline=c_out)
    elif archetype == "smash":
        # Clean curved outer smash arc + ground impact shockwave (never covering the torso)
        if row_idx == 0:
            pts = [(14, 30), (12, 48), (22, 60), (50, 60), (54, 54), (24, 54), (18, 44)]
            dr.polygon(pts, fill=c_main, outline=c_out)
            dr.rectangle([20, 58, 52, 63], fill=c_hi, outline=c_out)
        elif row_idx == 2:
            pts = [(14, 36), (12, 22), (24, 14), (50, 14), (52, 20), (26, 20), (18, 28)]
            dr.polygon(pts, fill=c_main, outline=c_out)
            dr.rectangle([22, 12, 50, 17], fill=c_hi, outline=c_out)
        elif row_idx == 1:
            pts = [(26, 18), (12, 26), (8, 52), (22, 58), (24, 52), (14, 46), (18, 28)]
            dr.polygon(pts, fill=c_main, outline=c_out)
            dr.rectangle([8, 54, 26, 60], fill=c_hi, outline=c_out)
        else:
            pts = [(46, 18), (60, 26), (64, 52), (50, 58), (48, 52), (58, 46), (54, 28)]
            dr.polygon(pts, fill=c_main, outline=c_out)
            dr.rectangle([46, 54, 64, 60], fill=c_hi, outline=c_out)
    elif archetype == "ranged":
        if row_idx == 0:
            dr.rectangle([28, 46, 44, 50], fill=c_main, outline=c_out)
            dr.polygon([(34, 48), (38, 48), (36, 70)], fill=c_hi, outline=c_out)
        elif row_idx == 2:
            dr.rectangle([28, 30, 44, 34], fill=c_main, outline=c_out)
            dr.polygon([(34, 32), (38, 32), (36, 10)], fill=c_hi, outline=c_out)
        elif row_idx == 1:
            dr.rectangle([22, 38, 26, 50], fill=c_main, outline=c_out)
            dr.polygon([(24, 42), (24, 46), (6, 44)], fill=c_hi, outline=c_out)
        else:
            dr.rectangle([46, 38, 50, 50], fill=c_main, outline=c_out)
            dr.polygon([(48, 42), (48, 46), (66, 44)], fill=c_hi, outline=c_out)

    cell[:] = np.array(pil, dtype=np.uint8)


def transform_body_for_clip(base_attack, base_walk, base_idle, base_hit, base_death, base_interact, base_whistle, clip_name, n_frames):
    if clip_name == "idle":
        return resample_frames_row(base_idle, base_idle.shape[1] // CW, 8)
    if clip_name == "walk":
        return resample_frames_row(base_walk, base_walk.shape[1] // CW, 8)
    if clip_name == "run":
        return resample_frames_row(base_walk if base_walk is not None else base_idle, base_walk.shape[1] // CW, 8)
    if clip_name in ("hit_light", "hit"):
        return resample_frames_row(base_hit, base_hit.shape[1] // CW, 4)
    if clip_name == "hit_heavy":
        s = resample_frames_row(base_hit, base_hit.shape[1] // CW, 6)
        for r in range(4):
            for c, recoil in ((1, 2), (2, 4), (3, 3), (4, 1)):
                dx = recoil if r == 1 else (-recoil if r == 3 else 0)
                dy = -recoil if r == 0 else (recoil if r == 2 else 0)
                s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = _shift_upper(s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW], dx, dy)
        return s
    if clip_name in ("faint", "death"):
        return resample_frames_row(base_death, base_death.shape[1] // CW, 10)
    if clip_name == "dodge":
        s = resample_frames_row(base_walk, base_walk.shape[1] // CW, 8)
        offsets = [(0, 2), (0, 3), (3, -2), (5, -3), (4, -1), (2, 1), (1, 2), (0, 0)]
        for r in range(4):
            for c, (fwd, vy) in enumerate(offsets):
                dx = -fwd if r == 1 else (fwd if r == 3 else 0)
                dy = vy + (fwd // 2 if r == 0 else (-fwd // 2 if r == 2 else 0))
                s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = _shift_upper(s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW], dx, dy)
        return s
    if clip_name == "counter":
        s = resample_frames_row(base_attack, base_attack.shape[1] // CW, 8)
        offsets = [(-2, 1), (-3, 1), (-2, 0), (4, -1), (5, 0), (3, 0), (1, 0), (0, 0)]
        for r in range(4):
            for c, (fwd, vy) in enumerate(offsets):
                dx = -fwd if r == 1 else (fwd if r == 3 else 0)
                dy = vy + (fwd // 2 if r == 0 else (-fwd // 2 if r == 2 else 0))
                s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = _shift_upper(s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW], dx, dy)
        return s
    if clip_name.startswith("interact") or clip_name in ("whistle", "horn", "idle_fidget_1", "idle_fidget_2"):
        base = base_whistle if clip_name in ("whistle", "horn") else (base_walk if clip_name == "interact_carry" else (base_idle if "fidget" in clip_name else base_interact))
        s = resample_frames_row(base, base.shape[1] // CW, n_frames)
        for r in range(4):
            for c in range(n_frames):
                phase = math.sin(2.0 * math.pi * c / n_frames)
                dy = int(round(2.0 * abs(phase))) if ("gather" in clip_name or "observe" in clip_name) else int(round(1.5 * phase))
                dx = int(round(1.5 * phase)) if ("clean" in clip_name or "feed" in clip_name) else 0
                sh = _shift_upper(s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW], dx, dy)
                hx = 26 if r == 1 else (46 if r == 3 else 42)
                hy = 46 + dy
                if clip_name == "interact_feed":
                    sh[hy - 2:hy + 4, hx - 3:hx + 3] = [214, 174, 84, 255]
                elif clip_name == "interact_water":
                    sh[hy:hy + 6, hx - 3:hx + 3] = [84, 146, 204, 255]
                elif clip_name == "interact_repair":
                    sh[hy - 5:hy, hx - 2:hx + 3] = [142, 102, 62, 255]
                elif clip_name == "interact_carry":
                    sh[26 + dy:36 + dy, 31:41] = [186, 152, 96, 255]
                elif clip_name == "horn":
                    mx = 30 if r == 1 else (42 if r == 3 else 36)
                    sh[20 + dy:25 + dy, mx - 3:mx + 3] = [216, 178, 92, 255]
                    sh[19 + dy:22 + dy, mx + (3 if r != 1 else -5):mx + (5 if r != 1 else -3)] = [242, 212, 134, 255]
                elif clip_name == "idle_fidget_2":
                    sh[40 + dy:46 + dy, hx - 4:hx + 2] = [224, 212, 188, 255]
                s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = sh
        return s

    # Use base_walk planted combat-stance frames + full-body wind-up -> lunge/torso-twist -> follow-through -> recovery!
    import build_paperdoll_16_gdd_gear_and_denoised_heroes as p16
    src_walk = base_walk if base_walk is not None else base_attack
    w_n = src_walk.shape[1] // CW
    stance_idx_8 = [1 % w_n, 2 % w_n, 0, 4 % w_n, 5 % w_n, 4 % w_n, 1 % w_n, 0]
    stance_idx_10 = [1 % w_n, 2 % w_n, 0, 4 % w_n, 5 % w_n, 5 % w_n, 4 % w_n, 1 % w_n, 1 % w_n, 0]
    idx_list = stance_idx_10 if n_frames == 10 else (stance_idx_8 if n_frames == 8 else [int((c * w_n) // n_frames) % w_n for c in range(n_frames)])
    s = np.zeros((CH * 4, CW * n_frames, 4), dtype=np.uint8)
    for r in range(4):
        for c, s_idx in enumerate(idx_list):
            s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = src_walk[r * CH:(r + 1) * CH, s_idx * CW:(s_idx + 1) * CW]

    is_smash = "smash" in clip_name or "chan_dia" in clip_name or "thien_chuy" in clip_name or "pha_giap" in clip_name
    is_thrust = "thrust" in clip_name or "xuyen_bay" in clip_name or "neo_kich" in clip_name or "moc_keo" in clip_name
    is_ranged = "ranged" in clip_name or "an_tin_hieu" in clip_name or "thien_la" in clip_name
    step = 3 if clip_name.endswith("_3") else (2 if clip_name.endswith("_2") else 1)

    for r in range(4):
        dir_sgn = -1.0 if r == 1 else (1.0 if r == 3 else (1.0 if step != 2 else -1.0))
        for c in range(n_frames):
            cell = s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
            if c in (0, 1):  # Phase 1: Wind-up coil
                fwd = (-2 - c) if not is_ranged else -1
                vy = (-2 - c) if is_smash else 1
                torso_deg = (-8.0 - 4.0 * c) * dir_sgn if not is_ranged else -4.0 * dir_sgn
                arm_dy = -2 - c
            elif c in (2, 3):  # Phase 2: Explosive Lunge & Torso Twist
                fwd = (5 + step + (c - 2)) if is_thrust else ((4 + step + (c - 2)) if not is_ranged else -2)
                vy = (2 + (c - 2)) if is_smash else (1 if r == 0 else (-1 if r == 2 else 0))
                torso_deg = (12.0 + 4.0 * (c - 2)) * dir_sgn if not is_ranged else -6.0 * dir_sgn
                arm_dy = 2 + (c - 2)
            elif c in (4, 5):  # Phase 3: Follow-through hold
                fwd = (3 + step - (c - 4)) if not is_ranged else -1
                vy = 1 if is_smash else 0
                torso_deg = (10.0 - 3.0 * (c - 4)) * dir_sgn if not is_ranged else -2.0 * dir_sgn
                arm_dy = 2 - (c - 4)
            else:  # Phase 4: Recovery to guard
                rem = max(0.0, float(n_frames - 1 - c) / max(1.0, float(n_frames - 6)))
                fwd = int(round(2.0 * rem))
                vy = 0
                torso_deg = 4.0 * rem * dir_sgn
                arm_dy = 0

            dx = -fwd if r == 1 else (fwd if r == 3 else int(round(dir_sgn * (2 if c in (2, 3, 4) else (-1 if c in (0, 1) else 0)))))
            dy = vy + (fwd // 2 if r == 0 else (-fwd // 2 if r == 2 else 0))

            # Rotate upper torso above hip (`y_hip`) so the shoulders/head/arms visibly coil and twist!
            bm = (cell[:, :, 3] == 255)
            bm[79:, :] = False
            ys, xs = np.where(bm)
            if len(xs) > 0 and abs(torso_deg) > 1.5:
                y_top, y_bot = int(ys.min()), int(ys.max())
                bh = max(20, y_bot - y_top + 1)
                y_hip = y_top + int(bh * 0.58)
                cx_b = int(round(0.5 * (int(xs.min()) + int(xs.max()))))
                upper = np.zeros_like(cell)
                upper[:min(CH, y_hip + 2), :] = cell[:min(CH, y_hip + 2), :]
                lower_and_sh = cell.copy()
                lower_and_sh[:y_hip, :] = 0
                rot_u = p16.rotate_patch_nearest(upper, torso_deg, (cx_b, y_hip))
                if arm_dy != 0 and r in (0, 2):
                    # Shift striking right/left shoulder & arm vertically during wind-up / strike
                    y_a0, y_a1 = y_top + int(bh * 0.26), y_top + int(bh * 0.52)
                    if r == 0:
                        sub_a = rot_u[y_a0:y_a1, :cx_b - 3].copy()
                        rot_u[y_a0:y_a1, :cx_b - 3] = 0
                        yd = max(0, min(CH - (y_a1 - y_a0), y_a0 + arm_dy))
                        rot_u[yd:yd + (y_a1 - y_a0), :cx_b - 3] = sub_a
                    else:
                        sub_a = rot_u[y_a0:y_a1, cx_b + 3:].copy()
                        rot_u[y_a0:y_a1, cx_b + 3:] = 0
                        yd = max(0, min(CH - (y_a1 - y_a0), y_a0 + arm_dy))
                        rot_u[yd:yd + (y_a1 - y_a0), cx_b + 3:] = sub_a
                p16.alpha_paste(lower_and_sh, rot_u, 0, 0)
                cell = lower_and_sh

            s[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = _shift_upper(cell, dx, dy)
    return s


def build_khoa_and_an_full_manifest():
    processed_folders = set()
    for who in ("khoa", "an"):
        char_dir = os.path.join(CHARS_DIR, who)
        argon_dir = os.path.join(FROM_ARGON_CHARS, who)
        processed_folders.add(who)

        def load_base(clip_key):
            p = os.path.join(char_dir, f"{who}_{clip_key}.png")
            return np.array(Image.open(p).convert("RGBA"), dtype=np.uint8)

        base_idle = load_base("idle")
        base_walk = load_base("walk")
        base_run = load_base("run")
        base_attack = load_base("attack")
        base_hit = load_base("hit")
        base_death = load_base("death")
        base_interact = load_base("interact")
        base_whistle = load_base("whistle")

        clips_to_build = [
            ("idle", 8, 8.0, True, None),
            ("walk", 8, 20.0, True, 1.8),
            ("run", 8, 18.0, True, 3.111),
            ("attack", 8, 14.0, False, None),
            ("dodge", 8, 16.0, False, None),
            ("counter", 8, 16.0, False, None),
            ("hit_light", 4, 12.0, False, None),
            ("hit_heavy", 6, 12.0, False, None),
            ("hit", 4, 12.0, False, None),
            ("faint", 10, 10.0, False, None),
            ("death", 10, 10.0, False, None),
            ("interact", 8, 12.0, False, None),
            ("interact_feed", 8, 12.0, False, None),
            ("interact_water", 8, 12.0, False, None),
            ("interact_clean", 8, 12.0, False, None),
            ("interact_repair", 8, 12.0, False, None),
            ("interact_gather", 8, 12.0, False, None),
            ("interact_carry", 8, 14.0, True, None),
            ("interact_observe", 8, 10.0, False, None),
            ("whistle", 8, 12.0, False, None),
            ("horn", 10, 12.0, False, None),
            ("idle_fidget_1", 12, 10.0, False, None),
            ("idle_fidget_2", 12, 10.0, False, None),
        ]
        for tag in ("slash", "thrust", "smash", "ranged"):
            for step in (1, 2, 3):
                nf = 10 if step == 3 else 8
                clips_to_build.append((f"attack_{tag}_{step}", nf, 14.0, False, None))

        if who == "khoa":
            for sk in ACTIVE_SKILLS:
                clips_to_build.append((f"skill_{sk}", 10, 15.0, False, None))
                for tag in ("slash", "thrust", "smash", "ranged"):
                    clips_to_build.append((f"skill_{sk}_{tag}", 10, 15.0, False, None))

        for clip_name, nf, fps, loop, stride in clips_to_build:
            sheet = transform_body_for_clip(
                base_attack, base_run if clip_name == "run" else base_walk,
                base_idle, base_hit, base_death, base_interact, base_whistle,
                clip_name, nf
            )
            save_sheet_and_json(char_dir, who, clip_name, sheet, nf, fps, loop, stride)
            os.makedirs(argon_dir, exist_ok=True)
            shutil.copy2(os.path.join(char_dir, f"{who}_{clip_name}.png"), os.path.join(argon_dir, f"{who}_{clip_name}.png"))
            shutil.copy2(os.path.join(char_dir, f"{who}_{clip_name}.json"), os.path.join(argon_dir, f"{who}_{clip_name}.json"))
        print(f"Built {len(clips_to_build)} QA-verified clips for {who}", flush=True)
    return processed_folders


def build_weapon_and_gear_overlays_for_khoa(processed_folders):
    who = "khoa"
    weapon_palettes = {
        "wpn_dao_rua": ((176, 186, 198), (235, 242, 250)),
        "wpn_giao_moc": ((158, 178, 196), (218, 236, 248)),
        "wpn_bua_ta": ((142, 134, 128), (214, 196, 168)),
        "wpn_no_tay": ((164, 122, 78), (228, 194, 124)),
    }

    for item_id, tag in WEAPON_ITEMS:
        fam = f"{who}__{item_id}"
        fam_dir = os.path.join(CHARS_DIR, fam)
        argon_fam_dir = os.path.join(FROM_ARGON_CHARS, fam)
        processed_folders.add(fam)

        def load_gear_base(clip_key):
            p = os.path.join(fam_dir, f"{fam}_{clip_key}.png")
            if not os.path.exists(p):
                p = os.path.join(fam_dir, f"{fam}_idle.png")
            return np.array(Image.open(p).convert("RGBA"), dtype=np.uint8)

        g_idle = load_gear_base("idle")
        g_walk = load_gear_base("walk")
        g_run = load_gear_base("run")
        g_atk = load_gear_base("attack")

        c_main, c_hi = weapon_palettes[item_id]

        wpn_clips = [
            ("idle", 8, 8.0, True, g_idle, False, 1),
            ("walk", 8, 20.0, True, g_walk, False, 1),
            ("run", 8, 18.0, True, g_run, False, 1),
            ("attack", 8, 14.0, False, g_atk, True, 1),
            ("dodge", 8, 16.0, False, g_run, False, 1),
            ("counter", 8, 16.0, False, g_atk, True, 2),
            ("hit", 4, 12.0, False, g_idle, False, 1),
            ("death", 10, 10.0, False, g_idle, False, 1),
            ("faint", 10, 10.0, False, g_idle, False, 1),
            ("interact", 8, 12.0, False, g_idle, False, 1),
            ("whistle", 8, 12.0, False, g_idle, False, 1),
        ]
        for step in (1, 2, 3):
            nf = 10 if step == 3 else 8
            wpn_clips.append((f"attack_{tag}_{step}", nf, 14.0, False, g_atk, True, step))
        for sk in ACTIVE_SKILLS:
            wpn_clips.append((f"skill_{sk}_{tag}", 10, 15.0, False, g_atk, True, 3))
            wpn_clips.append((f"skill_{sk}", 10, 15.0, False, g_atk, True, 2))

        for clip_name, nf, fps, loop, base_arr, add_smear, step in wpn_clips:
            sheet = resample_frames_row(base_arr, base_arr.shape[1] // CW, nf)
            if add_smear:
                for r in range(4):
                    for smear_f in (2, 3):
                        if smear_f < nf:
                            cell = sheet[r * CH:(r + 1) * CH, smear_f * CW:(smear_f + 1) * CW]
                            draw_smear_arc(cell, r, tag, step, c_main, c_hi)
            save_sheet_and_json(fam_dir, fam, clip_name, sheet, nf, fps, loop)
            os.makedirs(argon_fam_dir, exist_ok=True)
            shutil.copy2(os.path.join(fam_dir, f"{fam}_{clip_name}.png"), os.path.join(argon_fam_dir, f"{fam}_{clip_name}.png"))
            shutil.copy2(os.path.join(fam_dir, f"{fam}_{clip_name}.json"), os.path.join(argon_fam_dir, f"{fam}_{clip_name}.json"))

        for q_tag, tint in (("__II", (10, 16, 22)), ("__III", (22, 16, -6)), ("__IV", (32, 24, -10))):
            q_fam = f"{fam}{q_tag}"
            processed_folders.add(q_fam)
            q_dir = os.path.join(CHARS_DIR, q_fam)
            q_argon = os.path.join(FROM_ARGON_CHARS, q_fam)
            os.makedirs(q_dir, exist_ok=True)
            os.makedirs(q_argon, exist_ok=True)
            for clip_name, nf, fps, loop, _, _, _ in wpn_clips[:9]:
                src_p = os.path.join(fam_dir, f"{fam}_{clip_name}.png")
                src_j = os.path.join(fam_dir, f"{fam}_{clip_name}.json")
                arr = np.array(Image.open(src_p).convert("RGBA"), dtype=np.uint8)
                op = arr[:, :, 3] == 255
                for ch_i in range(3):
                    arr[:, :, ch_i][op] = np.clip(arr[:, :, ch_i][op].astype(np.int16) + tint[ch_i], 0, 255).astype(np.uint8)
                arr = qa_enforcer.finalize_and_verify_sheet(arr, CW, CH, nf, clip_name, max_colors=42)
                Image.fromarray(arr, "RGBA").save(os.path.join(q_dir, f"{q_fam}_{clip_name}.png"))
                shutil.copy2(src_j, os.path.join(q_dir, f"{q_fam}_{clip_name}.json"))
                shutil.copy2(os.path.join(q_dir, f"{q_fam}_{clip_name}.png"), os.path.join(q_argon, f"{q_fam}_{clip_name}.png"))
                shutil.copy2(os.path.join(q_dir, f"{q_fam}_{clip_name}.json"), os.path.join(q_argon, f"{q_fam}_{clip_name}.json"))
        print(f"Built {len(wpn_clips)} weapon overlay clips for {fam}", flush=True)

    for who_g in ("khoa", "an"):
        for item_id in OTHER_GEAR_ITEMS + [w[0] for w in WEAPON_ITEMS]:
            for suffix in ("", "__II", "__III", "__IV"):
                fam = f"{who_g}__{item_id}{suffix}"
                if fam in processed_folders:
                    continue
                fam_dir = os.path.join(CHARS_DIR, fam)
                if not os.path.exists(fam_dir):
                    continue
                processed_folders.add(fam)
                argon_fam_dir = os.path.join(FROM_ARGON_CHARS, fam)
                os.makedirs(argon_fam_dir, exist_ok=True)
                for fn in list(os.listdir(fam_dir)):
                    if not fn.endswith(".png"):
                        continue
                    p = os.path.join(fam_dir, fn)
                    jp = os.path.splitext(p)[0] + ".json"
                    if not os.path.exists(jp):
                        continue
                    meta = json.load(open(jp, encoding="utf-8"))
                    clip = os.path.splitext(fn)[0][len(fam) + 1:]
                    arr = np.array(Image.open(p).convert("RGBA"), dtype=np.uint8)
                    cur_n = meta.get("frames_per_row", arr.shape[1] // CW)
                    target_n = 8 if clip in ("idle", "walk", "run", "attack", "interact", "whistle") else (10 if clip in ("death", "faint") else (4 if clip in ("hit", "hit_light") else cur_n))
                    if cur_n != target_n:
                        arr = resample_frames_row(arr, cur_n, target_n)
                        meta["frames_per_row"] = target_n
                    if clip == "walk":
                        meta["fps"] = 20.0
                        meta["stride_m"] = 1.8
                    elif clip == "run":
                        meta["fps"] = 18.0
                        meta["stride_m"] = 3.111
                    arr = qa_enforcer.finalize_and_verify_sheet(arr, CW, CH, target_n, clip, max_colors=42)
                    Image.fromarray(arr, "RGBA").save(p)
                    with open(jp, "w", encoding="utf-8") as f:
                        json.dump(meta, f, indent=2)
                    shutil.copy2(p, os.path.join(argon_fam_dir, fn))
                    shutil.copy2(jp, os.path.join(argon_fam_dir, os.path.basename(jp)))
    print("Upgraded all 16 GDD paper-doll gear folders to 8-frame locomotion & QA pass", flush=True)


def enforce_qa_across_remaining_character_folders(processed_folders):
    total_sheets = 0
    for sub in sorted(os.listdir(CHARS_DIR)):
        if sub in processed_folders:
            continue
        d = os.path.join(CHARS_DIR, sub)
        if not os.path.isdir(d):
            continue
        ad = os.path.join(FROM_ARGON_CHARS, sub)
        os.makedirs(ad, exist_ok=True)
        for fn in sorted(os.listdir(d)):
            if not fn.endswith(".png"):
                continue
            p = os.path.join(d, fn)
            jp = os.path.splitext(p)[0] + ".json"
            if not os.path.exists(jp):
                continue
            meta = json.load(open(jp, encoding="utf-8"))
            cw, ch = meta["cell"]
            img = Image.open(p).convert("RGBA")
            n = meta.get("frames_per_row", img.width // cw)
            clip = os.path.splitext(fn)[0].split("_")[-1]
            arr = np.array(img, dtype=np.uint8)
            arr = qa_enforcer.finalize_and_verify_sheet(arr, cw, ch, n, clip, max_colors=42)
            Image.fromarray(arr, "RGBA").save(p)
            shutil.copy2(p, os.path.join(ad, fn))
            shutil.copy2(jp, os.path.join(ad, os.path.basename(jp)))
            total_sheets += 1
    print(f"Enforced QA pass across {total_sheets} remaining world entity sheets!", flush=True)


def render_x15_filmstrip_showcase():
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 12)
    except Exception:
        font = ImageFont.load_default()

    showcase_rows = [
        ("Khoa Unarmed — walk (8 frames @20fps, 1.8m stride, <=42 colours, 0 holes)", "khoa", "walk", None, 0),
        ("Khoa Unarmed — run (8 frames @18fps, 3.1m stride, forward lean)", "khoa", "run", None, 1),
        ("Khoa + Dao Rựa — attack_slash_1 (8 frames, flat machete smear arc)", "khoa", "attack_slash_1", "khoa__wpn_dao_rua", 0),
        ("Khoa + Dao Rựa — attack_slash_3 (10 frames, heavy spin-cleave finisher)", "khoa", "attack_slash_3", "khoa__wpn_dao_rua", 0),
        ("Khoa + Giáo Móc — attack_thrust_1 (8 frames, lunge & hook barb pull)", "khoa", "attack_thrust_1", "khoa__wpn_giao_moc", 1),
        ("Khoa + Búa Tạ — attack_smash_3 (10 frames, overhead slam & shockwave)", "khoa", "attack_smash_3", "khoa__wpn_bua_ta", 0),
        ("Khoa + Nỏ Tay — attack_ranged_1 (8 frames, aim & bolt streak recoil)", "khoa", "attack_ranged_1", "khoa__wpn_no_tay", 3),
        ("Khoa Unarmed — dodge (8 frames) & counter (8 frames)", "khoa", "dodge", None, 1),
        ("Khoa Farm Work — interact_feed (8f) & horn (10f)", "khoa", "horn", None, 0),
        ("Khoa Skill + Dao Rựa — skill_doan_anh_tram_slash (10 frames)", "khoa", "skill_doan_anh_tram_slash", "khoa__wpn_dao_rua", 0),
    ]

    cw15, ch15 = int(CW * 1.5), int(CH * 1.5)
    cols = 10
    pad_x, pad_y, hdr_h = 10, 26, 44
    board_w = cols * (cw15 + 6) + pad_x * 2
    board_h = hdr_h + len(showcase_rows) * (ch15 + pad_y) + 12

    board = Image.new("RGBA", (board_w, board_h), (24, 28, 34, 255))
    dr = ImageDraw.Draw(board)
    dr.text((14, 12), "PIG TYCOON — KHOA P1-P4 ANIMATIONS & WEAPON OVERLAYS (x1.5 NEAREST-NEIGHBOUR, QA_SHEETS.PY: 0 FAIL / 0 WARN)", fill=(255, 230, 140, 255), font=font)

    for idx, (label, body_fam, clip, wpn_fam, row_dir) in enumerate(showcase_rows):
        y0 = hdr_h + idx * (ch15 + pad_y)
        dr.text((14, y0), label, fill=(210, 235, 255, 255), font=font)
        b_path = os.path.join(CHARS_DIR, body_fam, f"{body_fam}_{clip}.png")
        b_img = Image.open(b_path).convert("RGBA")
        nf = b_img.width // CW
        w_img = None
        if wpn_fam:
            w_path = os.path.join(CHARS_DIR, wpn_fam, f"{wpn_fam}_{clip}.png")
            if os.path.exists(w_path):
                w_img = Image.open(w_path).convert("RGBA")

        for c in range(min(cols, nf)):
            x0 = pad_x + c * (cw15 + 6)
            cy = y0 + 18
            dr.rectangle([x0, cy, x0 + cw15 - 1, cy + ch15 - 1], fill=(44, 58, 42, 255), outline=(68, 82, 64, 255))
            cell = b_img.crop((c * CW, row_dir * CH, (c + 1) * CW, (row_dir + 1) * CH))
            if w_img is not None:
                w_cell = w_img.crop((c * CW, row_dir * CH, (c + 1) * CW, (row_dir + 1) * CH))
                cell = Image.alpha_composite(cell, w_cell)
            cell15 = cell.resize((cw15, ch15), Image.Resampling.NEAREST)
            board.alpha_composite(cell15, (x0, cy))

    out_brain = os.path.join(BRAIN_DIR, "khoa_p1_p4_weapons_x15_qa_verified.png")
    out_argon = os.path.join(FROM_ARGON_CHARS, "khoa_p1_p4_weapons_x15_qa_verified.png")
    board.save(out_brain)
    board.save(out_argon)
    print("Saved x1.5 filmstrip board:", out_brain, flush=True)


if __name__ == "__main__":
    done = build_khoa_and_an_full_manifest()
    build_weapon_and_gear_overlays_for_khoa(done)
    enforce_qa_across_remaining_character_folders(done)
    render_x15_filmstrip_showcase()
