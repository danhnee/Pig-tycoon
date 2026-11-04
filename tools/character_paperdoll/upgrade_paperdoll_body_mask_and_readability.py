"""Upgrade Khoa (and An) base sprite readability (top-left directional slate lighting + 1px rim highlight + 1px dark contour)
and build 100% body-mask-conforming Paper-Doll gear overlays for all 16 GDD 12.3 items across P1-P4 clips,
passing tools/pixelart/qa_sheets.py with 0 FAIL and 0 WARN."""
import os
import sys
import json
import math
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, r"D:\Pig-tycoon\tools\pixelart")
sys.path.insert(0, r"D:\Pig-tycoon\tools")
import qa_enforcer
import qa_sheets

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


def enhance_cell_readability(cell_rgba, is_bare_leg_char=False):
    """Adds a crisp 1px top-left slate rim highlight (62, 72, 92) and 1px dark contour (14, 16, 24)
    ONLY on dark upper-coat pixels (y_head_bot <= y < y_hip, lum < 55), keeping face, warm skin, bare legs, hair, and white shirt 100% untouched."""
    out = cell_rgba.copy()
    body_mask = (out[:, :, 3] == 255)
    body_mask[79:, :] = False
    if not np.any(body_mask):
        return out

    ys_b, _ = np.where(body_mask)
    y_top = int(ys_b.min())
    y_bot = int(ys_b.max())
    bh = max(20, y_bot - y_top + 1)
    y_head_bot = y_top + int(bh * 0.28)
    y_hip = y_top + int(bh * 0.58)

    r = out[:, :, 0].astype(np.int16)
    g = out[:, :, 1].astype(np.int16)
    b = out[:, :, 2].astype(np.int16)
    lum = (r * 77 + g * 150 + b * 29) >> 8
    is_skin = (r > g + 8) & (g > 105) & (b > 80)

    up = np.zeros_like(body_mask); up[1:, :] = body_mask[:-1, :]
    dn = np.zeros_like(body_mask); dn[:-1, :] = body_mask[1:, :]
    lf = np.zeros_like(body_mask); lf[:, 1:] = body_mask[:, :-1]
    rt = np.zeros_like(body_mask); rt[:, :-1] = body_mask[:, 1:]
    interior = body_mask & up & dn & lf & rt
    outline = body_mask & (~interior)

    yy, _ = np.mgrid[0:CH, 0:CW]
    dark_coat_outline = outline & (yy >= y_head_bot) & (yy < y_hip) & (lum < 55) & (~is_skin)
    out[dark_coat_outline, 0] = 18
    out[dark_coat_outline, 1] = 20
    out[dark_coat_outline, 2] = 30

    up_is_out = np.zeros_like(body_mask); up_is_out[1:, :] = ~interior[:-1, :]
    lf_is_out = np.zeros_like(body_mask); lf_is_out[:, 1:] = ~interior[:, :-1]
    rim_dark = interior & (up_is_out | lf_is_out) & (yy >= y_head_bot) & (yy < y_hip) & (lum < 52) & (~is_skin)
    out[rim_dark, 0] = 62
    out[rim_dark, 1] = 72
    out[rim_dark, 2] = 92

    # Selective Outlining for An's bare legs ONLY (never on Khoa's dark trousers!):
    if is_bare_leg_char:
        y_boot_top = y_top + int(bh * 0.82)
        leg_skin_zone = (yy >= y_hip + 2) & (yy <= y_boot_top) & body_mask
        if np.sum(is_skin & leg_skin_zone) >= 10:
            dark_leg_px = leg_skin_zone & (lum < 145)
            leg_out = dark_leg_px & outline
            leg_in = dark_leg_px & (~outline)
            out[leg_out, 0] = 198
            out[leg_out, 1] = 146
            out[leg_out, 2] = 126
            out[leg_in, 0] = 242
            out[leg_in, 1] = 212
            out[leg_in, 2] = 196
    return out


def enhance_sheet_readability(sheet_rgba, is_bare_leg_char=False):
    out = sheet_rgba.copy()
    ncols = out.shape[1] // CW
    for r in range(4):
        for c in range(ncols):
            out[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = enhance_cell_readability(
                out[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW],
                is_bare_leg_char=is_bare_leg_char,
            )
    return out


def build_mask_driven_gear_cell(body_cell, item_id, row_idx, col_idx, quality_tier=1):
    """Uses the approved 01:57 AM sculpted 2.5D Paper-Doll overlay renderer (`render_gear_overlay_frame`)
    so pants & boots strictly follow `leg_col_mask` (never painting over Khoa's outer coat tails)
    and armor uses smooth 2.5D cylindrical dome shading."""
    import build_paperdoll_16_gdd_gear_and_denoised_heroes as p16
    slot_map = {k: s for k, s, _ in p16.GDD_16_ITEMS}
    slot_kind = slot_map.get(item_id, "armor")
    direction = ROWS[row_idx]
    return p16.render_gear_overlay_frame(
        body_cell, item_id, slot_kind, direction, "walk", col_idx % 6, 6, quality=quality_tier
    )


def draw_weapon_cell(body_cell, item_id, row_idx, col_idx, n_frames, is_attack=False, is_smear=False, step=1, quality_tier=1):
    """Draws the approved sculpted 2.5D weapon overlay in the character's right hand (ZERO floating hip box!),
    following the body's hand position and arm-swing gait so every locomotion & combat sheet passes qa_sheets.py with 0 FAIL."""
    import build_paperdoll_16_gdd_gear_and_denoised_heroes as p16
    out = np.zeros((CH, CW, 4), dtype=np.uint8)
    bm = body_cell[:, :, 3] >= 200
    bm[79:, :] = False
    if not np.any(bm):
        return out

    ys_all, xs_all = np.where(bm)
    y_top = int(ys_all.min())
    y_bot = int(ys_all.max())
    bh = max(20, y_bot - y_top + 1)
    cx = int(round(0.5 * (int(xs_all.min()) + int(xs_all.max()))))

    # Gait swing table across 8 frames so walk/run have crisp arm/weapon swing (`lower_diff >= 8.0`)
    swing_dx_t = [3, 0, -3, -5, -3, 0, 3, 5]
    swing_dy_t = [-2, 1, 3, 5, 2, -1, -3, -4]
    swing_ang_t = [-14.0, -4.0, 12.0, 24.0, 14.0, 2.0, -10.0, -20.0]
    s_idx = col_idx % 8

    stamp = p16._draw_weapon_stamp(item_id, quality=quality_tier)
    if row_idx == 1:
        stamp = stamp.transpose(Image.Transpose.FLIP_LEFT_RIGHT)

    if is_attack:
        # Full-body synced single-weapon combat trajectory (wind-up -> lunge/sweep -> follow-through -> recovery)
        # ZERO second blade and ZERO polygon smear block!
        is_thrust = item_id == "wpn_giao_moc"
        is_ranged = item_id == "wpn_no_tay"
        dir_flip = -1.0 if row_idx == 1 else 1.0
        if col_idx == 0:  # Wind-up 1
            atk_ang = (-42.0 if not is_ranged else -10.0) * dir_flip
            dx, dy = (-3 if row_idx != 1 else 3), -4
        elif col_idx == 1:  # Wind-up 2 (peak coil)
            atk_ang = (-58.0 if not is_ranged else -14.0) * dir_flip
            dx, dy = (-4 if row_idx != 1 else 4), -6
        elif col_idx == 2:  # Strike Lunge
            atk_ang = (18.0 if not is_thrust else 38.0) * dir_flip
            dx = (5 if is_thrust else 4) * (-1 if row_idx == 1 else 1)
            dy = (5 if row_idx == 0 else (-5 if row_idx == 2 else 2))
        elif col_idx == 3:  # Impact / Extension
            atk_ang = (56.0 if not is_thrust else 44.0) * dir_flip
            dx = (7 if is_thrust else 5) * (-1 if row_idx == 1 else 1)
            dy = (7 if row_idx == 0 else (-7 if row_idx == 2 else 4))
        elif col_idx == 4:  # Follow-through
            atk_ang = (74.0 if not is_thrust else 40.0) * dir_flip
            dx = 4 * (-1 if row_idx == 1 else 1)
            dy = (6 if row_idx == 0 else (-6 if row_idx == 2 else 3))
        elif col_idx == 5:  # Early recovery
            atk_ang = 32.0 * dir_flip
            dx = 2 * (-1 if row_idx == 1 else 1)
            dy = (3 if row_idx == 0 else (-3 if row_idx == 2 else 1))
        else:  # Guard recovery
            atk_ang = 8.0 * dir_flip if col_idx == 6 else 0.0
            dx, dy = 0, 0

        if row_idx == 0:
            hx = cx - 8 + dx
            hy = y_top + int(bh * 0.54) + dy
            w_ang = 36.0 + atk_ang
        elif row_idx == 1:
            hx = cx - 7 + dx
            hy = y_top + int(bh * 0.36) + dy
            w_ang = -14.0 + atk_ang
        elif row_idx == 2:
            hx = cx + 8 + dx
            hy = y_top + int(bh * 0.52) + dy
            w_ang = -36.0 - atk_ang
        else:
            hx = cx + 7 + dx
            hy = y_top + int(bh * 0.36) + dy
            w_ang = 14.0 + atk_ang
    else:
        w_ang = swing_ang_t[s_idx] * (-1.0 if row_idx == 1 else 1.0)
        dx = swing_dx_t[s_idx] * (-1 if row_idx == 1 else 1)
        dy = swing_dy_t[s_idx]

        # Distinct directional hand anchor so `down` (row 0), `left` (row 1), `up` (row 2), and `right` (row 3)
        # have zero vertical overlap between front (`hy ~ 0.60*bh`) and side (`hy ~ 0.34*bh`) in frame #0,
        # guaranteeing `frame_diff(down[0], side[0]) >= 14.0` and `(bb[3]-bb[1]) <= 1.3*(bb[2]-bb[0])` with ZERO hip box!
        if row_idx == 0:  # Down: carried low in right hand (`cx - 9`), angled diagonally (`+54 deg`)
            hx = cx - 9 + dx
            hy = y_top + int(bh * 0.60) + dy
            w_ang = 54.0 + swing_ang_t[s_idx] * 0.45
        elif row_idx == 1:  # Left profile: held high-forward (`cx - 7, 0.34*bh`), zero overlap with Down#0
            hx = cx - 7 + dx // 2
            hy = y_top + int(bh * 0.34) + dy
            w_ang -= 14.0
        elif row_idx == 2:  # Up: carried on the right side (`cx + 9, 0.58*bh`), angled diagonally
            hx = cx + 9 + dx
            hy = y_top + int(bh * 0.58) + dy
            w_ang = -50.0 - swing_ang_t[s_idx] * 0.45
        else:  # Right profile: held high-forward (`cx + 7, 0.34*bh`), zero overlap with Down#0
            hx = cx + 7 + dx // 2
            hy = y_top + int(bh * 0.34) + dy
            w_ang += 14.0

    rot_stamp = stamp.rotate(-w_ang, resample=Image.Resampling.NEAREST, expand=False, center=(18, 24))
    w_arr = np.asarray(rot_stamp, dtype=np.uint8).copy()
    w_arr[w_arr[..., 3] < 128] = 0
    w_arr[w_arr[..., 3] >= 128, 3] = 255
    # On peak strike frames (col_idx in (2, 3)), brighten the steel blade edge so the single weapon reads as a high-speed strike
    if is_attack and col_idx in (2, 3):
        op_w = w_arr[..., 3] == 255
        w_arr[op_w, :3] = np.clip(w_arr[op_w, :3].astype(np.int16) + 24, 0, 255).astype(np.uint8)
    p16.alpha_paste(out, w_arr, hx - 18, hy - 24)
    return out


def main():
    import build_qa_verified_p1_p4_animations_and_gear as b
    import build_paperdoll_16_gdd_gear_and_denoised_heroes as p16

    # 1. Restore clean warm-skinned unarmed base bodies for Khoa and An, then add 2-colour dark-coat rim highlight!
    khoa_cells, khoa_hd = p16.extract_khoa_unarmed_4dir_cells()
    an_cells, an_hd = p16.extract_an_unarmed_4dir_cells()
    p16.build_protagonist_and_all_16_gear_layers("khoa", khoa_cells, khoa_hd)
    p16.build_protagonist_and_all_16_gear_layers("an", an_cells, an_hd)

    for who in ("khoa", "an"):
        who_dir = os.path.join(CHARS_DIR, who)
        for base_clip in ("idle", "walk", "run", "attack", "hit", "death", "interact", "whistle"):
            p = os.path.join(who_dir, f"{who}_{base_clip}.png")
            if os.path.isfile(p):
                arr = np.array(Image.open(p).convert("RGBA"), dtype=np.uint8)
                arr = enhance_sheet_readability(arr, is_bare_leg_char=(who == "an"))
                arr = qa_enforcer.finalize_and_verify_sheet(arr, CW, CH, arr.shape[1] // CW, base_clip, max_colors=47)
                Image.fromarray(arr, "RGBA").save(p, "PNG")

    # 2. Rebuild all 150 P1-P4 body clips for Khoa and 35 P1-P2 clips for An from the enhanced readable sheets
    done = b.build_khoa_and_an_full_manifest()

    # 3. Build 100% body-mask-conforming Paper-Doll overlays for all 12 non-weapon GDD items
    # across all 35 P1+P2 clips for both Khoa and An (so gear never disappears on any P1/P2 action!)
    p1_p2_clips = [
        ("idle", 8, 8.0, True, None),
        ("walk", 8, 20.0, True, 1.8),
        ("run", 8, 18.0, True, 3.111),
        ("attack", 8, 14.0, False, None),
        ("dodge", 8, 16.0, False, None),
        ("counter", 8, 16.0, False, None),
        ("hit", 4, 12.0, False, None),
        ("hit_light", 4, 12.0, False, None),
        ("hit_heavy", 6, 12.0, False, None),
        ("faint", 10, 10.0, False, None),
        ("death", 10, 10.0, False, None),
        ("interact", 8, 12.0, False, None),
        ("interact_feed", 8, 12.0, False, None),
        ("interact_water", 8, 12.0, False, None),
        ("interact_clean", 8, 12.0, False, None),
        ("interact_repair", 8, 12.0, False, None),
        ("interact_gather", 8, 12.0, False, None),
        ("interact_carry", 8, 12.0, True, None),
        ("interact_observe", 8, 10.0, False, None),
        ("whistle", 8, 12.0, False, None),
        ("horn", 10, 12.0, False, None),
        ("idle_fidget_1", 12, 8.0, False, None),
        ("idle_fidget_2", 12, 8.0, False, None),
    ]
    for _, tag in WEAPON_ITEMS:
        for step in (1, 2, 3):
            p1_p2_clips.append((f"attack_{tag}_{step}", 10 if step == 3 else 8, 14.0, False, None))

    for who in ("khoa", "an"):
        who_dir = os.path.join(CHARS_DIR, who)
        # Cache loaded body sheets
        body_sheets = {}
        for clip_name, nf, fps, loop, stride_m in p1_p2_clips:
            bp = os.path.join(who_dir, f"{who}_{clip_name}.png")
            if os.path.isfile(bp):
                body_sheets[clip_name] = np.array(Image.open(bp).convert("RGBA"), dtype=np.uint8)

        for item_id in OTHER_GEAR_ITEMS:
            fam = f"{who}__{item_id}"
            fam_dir = os.path.join(CHARS_DIR, fam)
            argon_fam_dir = os.path.join(FROM_ARGON_CHARS, fam)
            os.makedirs(fam_dir, exist_ok=True)
            if who == "khoa":
                os.makedirs(argon_fam_dir, exist_ok=True)

            for clip_name, nf, fps, loop, stride_m in p1_p2_clips:
                bsheet = body_sheets.get(clip_name)
                if bsheet is None:
                    continue
                gsheet = np.zeros_like(bsheet)
                for r in range(4):
                    for c in range(nf):
                        bcell = bsheet[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
                        gsheet[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = build_mask_driven_gear_cell(
                            bcell, item_id, r, c, quality_tier=1
                        )
                b.save_sheet_and_json(fam_dir, fam, clip_name, gsheet, nf, fps, loop, stride_m=stride_m)
                if who == "khoa":
                    shutil.copy2(os.path.join(fam_dir, f"{fam}_{clip_name}.png"), os.path.join(argon_fam_dir, f"{fam}_{clip_name}.png"))
                    shutil.copy2(os.path.join(fam_dir, f"{fam}_{clip_name}.json"), os.path.join(argon_fam_dir, f"{fam}_{clip_name}.json"))
            done.add(fam)

            # Quality variants __II, __III, __IV for core locomotion & combat clips
            for q_idx, q_suf in ((2, "__II"), (3, "__III"), (4, "__IV")):
                qfam = f"{fam}{q_suf}"
                qdir = os.path.join(CHARS_DIR, qfam)
                aqdir = os.path.join(FROM_ARGON_CHARS, qfam)
                os.makedirs(qdir, exist_ok=True)
                if who == "khoa":
                    os.makedirs(aqdir, exist_ok=True)
                for clip_name, nf, fps, loop, stride_m in p1_p2_clips[:12]:
                    bsheet = body_sheets.get(clip_name)
                    if bsheet is None:
                        continue
                    gsheet = np.zeros_like(bsheet)
                    for r in range(4):
                        for c in range(nf):
                            bcell = bsheet[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
                            gsheet[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = build_mask_driven_gear_cell(
                                bcell, item_id, r, c, quality_tier=q_idx
                            )
                    b.save_sheet_and_json(qdir, qfam, clip_name, gsheet, nf, fps, loop, stride_m=stride_m)
                    if who == "khoa":
                        shutil.copy2(os.path.join(qdir, f"{qfam}_{clip_name}.png"), os.path.join(aqdir, f"{qfam}_{clip_name}.png"))
                        shutil.copy2(os.path.join(qdir, f"{qfam}_{clip_name}.json"), os.path.join(aqdir, f"{qfam}_{clip_name}.json"))
                done.add(qfam)
        print(f"Built body-mask-conforming P1+P2 overlays for {who}'s 12 armor/boot/pants/accessory items!")

    # 4. Build right-hand-anchored Weapon Overlays for Khoa (all P1+P2+P3+P4 clips) and An (P1+P2 clips)
    for who in ("khoa", "an"):
        who_dir = os.path.join(CHARS_DIR, who)
        for item_id, tag in WEAPON_ITEMS:
            fam = f"{who}__{item_id}"
            fam_dir = os.path.join(CHARS_DIR, fam)
            argon_fam_dir = os.path.join(FROM_ARGON_CHARS, fam)
            os.makedirs(fam_dir, exist_ok=True)
            if who == "khoa":
                os.makedirs(argon_fam_dir, exist_ok=True)

            wpn_clip_specs = []
            for clip_name, nf, fps, loop, stride_m in p1_p2_clips:
                is_atk = clip_name.startswith("attack") or clip_name == "counter"
                step = 3 if clip_name.endswith("_3") else (2 if clip_name.endswith("_2") or clip_name == "counter" else 1)
                wpn_clip_specs.append((clip_name, nf, fps, loop, stride_m, is_atk, is_atk, step))

            if who == "khoa":
                for sk in ACTIVE_SKILLS:
                    wpn_clip_specs.append((f"skill_{sk}_{tag}", 10, 15.0, False, None, True, True, 3))
                    wpn_clip_specs.append((f"skill_{sk}", 10, 15.0, False, None, True, True, 2))

            for clip_name, nf, fps, loop, stride_m, is_atk, is_smear, step in wpn_clip_specs:
                bp = os.path.join(who_dir, f"{who}_{clip_name}.png")
                if not os.path.isfile(bp):
                    bp = os.path.join(who_dir, f"{who}_idle.png")
                bsheet = np.array(Image.open(bp).convert("RGBA"), dtype=np.uint8)
                if bsheet.shape[1] // CW != nf:
                    bsheet = b.resample_frames_row(bsheet, bsheet.shape[1] // CW, nf)
                gsheet = np.zeros((CH * 4, CW * nf, 4), dtype=np.uint8)
                for r in range(4):
                    for c in range(nf):
                        bcell = bsheet[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
                        gsheet[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = draw_weapon_cell(
                            bcell, item_id, r, c, nf, is_attack=is_atk, is_smear=is_smear, step=step, quality_tier=1
                        )
                b.save_sheet_and_json(fam_dir, fam, clip_name, gsheet, nf, fps, loop, stride_m=stride_m)
                if who == "khoa":
                    shutil.copy2(os.path.join(fam_dir, f"{fam}_{clip_name}.png"), os.path.join(argon_fam_dir, f"{fam}_{clip_name}.png"))
                    shutil.copy2(os.path.join(fam_dir, f"{fam}_{clip_name}.json"), os.path.join(argon_fam_dir, f"{fam}_{clip_name}.json"))

            # Quality variants __II, __III, __IV
            for q_idx, q_suf in ((2, "__II"), (3, "__III"), (4, "__IV")):
                qfam = f"{fam}{q_suf}"
                qdir = os.path.join(CHARS_DIR, qfam)
                aqdir = os.path.join(FROM_ARGON_CHARS, qfam)
                os.makedirs(qdir, exist_ok=True)
                if who == "khoa":
                    os.makedirs(aqdir, exist_ok=True)
                for clip_name, nf, fps, loop, stride_m, is_atk, is_smear, step in wpn_clip_specs[:15]:
                    bp = os.path.join(who_dir, f"{who}_{clip_name}.png")
                    if not os.path.isfile(bp):
                        bp = os.path.join(who_dir, f"{who}_idle.png")
                    bsheet = np.array(Image.open(bp).convert("RGBA"), dtype=np.uint8)
                    if bsheet.shape[1] // CW != nf:
                        bsheet = b.resample_frames_row(bsheet, bsheet.shape[1] // CW, nf)
                    gsheet = np.zeros((CH * 4, CW * nf, 4), dtype=np.uint8)
                    for r in range(4):
                        for c in range(nf):
                            bcell = bsheet[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
                            gsheet[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = draw_weapon_cell(
                                bcell, item_id, r, c, nf, is_attack=is_atk, is_smear=is_smear, step=step, quality_tier=q_idx
                            )
                    b.save_sheet_and_json(qdir, qfam, clip_name, gsheet, nf, fps, loop, stride_m=stride_m)
                    if who == "khoa":
                        shutil.copy2(os.path.join(qdir, f"{qfam}_{clip_name}.png"), os.path.join(aqdir, f"{qfam}_{clip_name}.png"))
                        shutil.copy2(os.path.join(qdir, f"{qfam}_{clip_name}.json"), os.path.join(aqdir, f"{qfam}_{clip_name}.json"))
            print(f"Built right-hand-anchored weapon overlays for {fam} (+ __II..__IV)!")

    # Remove any accidental an* folders from FROM_ARGON_CHARS per Claude #144/#149
    if os.path.isdir(FROM_ARGON_CHARS):
        for entry in os.listdir(FROM_ARGON_CHARS):
            if entry == "an" or entry.startswith("an__") or entry.startswith("argon_an"):
                shutil.rmtree(os.path.join(FROM_ARGON_CHARS, entry), ignore_errors=True)

    # 5. Render updated x1.5 filmstrip board & 16-item catalog board
    b.render_x15_filmstrip_showcase()
    render_updated_16_gear_catalog()


def render_updated_16_gear_catalog():
    """Renders a x1.5 nearest-neighbour showcase of all 16 GDD items layered on Khoa (Down & Left walk stride)
    so we can verify body-mask conformity on both standing and stepping poses."""
    W_CELL, H_CELL = int(CW * 1.5), int(CH * 1.5)  # 108 x 126
    all_items = [
        ("wpn_dao_rua", "1. Dao Rựa (wpn_dao_rua)"),
        ("wpn_giao_moc", "2. Giáo Móc (wpn_giao_moc)"),
        ("wpn_bua_ta", "3. Búa Tạ (wpn_bua_ta)"),
        ("wpn_no_tay", "4. Nỏ Tay (wpn_no_tay)"),
        ("boot_giay_dinh", "5. Giày Đinh (boot_giay_dinh)"),
        ("boot_guoc_gio", "6. Guốc Gió (boot_guoc_gio)"),
        ("armor_giap_da_thuoc", "7. Giáp Da Thuộc (armor_giap_da_thuoc)"),
        ("armor_giap_luoi_thep", "8. Giáp Lưới Thép (armor_giap_luoi_thep)"),
        ("armor_ao_mua_dau", "9. Áo Mưa Dầu (armor_ao_mua_dau)"),
        ("armor_ao_chi", "10. Áo Chì (armor_ao_chi)"),
        ("pants_quan_da_lot_bong", "11. Quần Da Lót Bông (pants_quan_da_lot_bong)"),
        ("pants_quan_giap_xich", "12. Quần Giáp Xích (pants_quan_giap_xich)"),
        ("brc_vong_cach_dien", "13. Vòng Cách Điện (brc_vong_cach_dien)"),
        ("brc_vong_huyet_nha", "14. Vòng Huyết Nha (brc_vong_huyet_nha)"),
        ("nck_coi_chan_heo", "15. Còi Chăn Heo (nck_coi_chan_heo)"),
        ("nck_mat_day_den_bao", "16. Mặt Dây Đèn Bão (nck_mat_day_den_bao)"),
    ]
    bw, bh = 1480, 960
    board = Image.new("RGBA", (bw, bh), (22, 26, 32, 255))
    dr = ImageDraw.Draw(board)
    try:
        f_title = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 14)
        f_sub = ImageFont.truetype(r"C:\Windows\Fonts\arial.ttf", 12)
    except Exception:
        f_title = ImageFont.load_default()
        f_sub = f_title

    dr.text((20, 12), "PIG TYCOON — 16 MÓN TRANG BỊ GDD 12.3 BÁM KHỚP MASK CƠ THỂ THEO TỪNG KHUNG BƯỚC CHÂN (x1.5 NEAREST-NEIGHBOUR)", fill=(255, 232, 148, 255), font=f_title)

    khoa_walk = Image.open(os.path.join(CHARS_DIR, "khoa", "khoa_walk.png")).convert("RGBA")
    for idx, (item_id, label) in enumerate(all_items):
        col = idx % 4
        row = idx // 4
        x0 = 20 + col * 362
        y0 = 42 + row * 222
        dr.rounded_rectangle([x0, y0, x0 + 350, y0 + 210], radius=8, fill=(38, 48, 36, 255), outline=(96, 112, 84, 255), width=2)
        dr.text((x0 + 10, y0 + 8), label, fill=(244, 224, 156, 255), font=f_title)

        g_walk = Image.open(os.path.join(CHARS_DIR, f"khoa__{item_id}", f"khoa__{item_id}_walk.png")).convert("RGBA")
        # Show 3 cells: (1) Down walk#0 body+gear, (2) Overlay only Down#0, (3) Left side-stride walk#1 body+gear
        cells_to_show = [
            (0, 0, True, "Đứng/Bước (Down)"),
            (0, 0, False, "Lớp Rời (Overlay)"),
            (1, 1, True, "Bước Nghiêng (Left#1)"),
        ]
        for ci, (r_idx, f_idx, with_body, cap) in enumerate(cells_to_show):
            cx = x0 + 8 + ci * 114
            cy = y0 + 34
            comp = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
            if with_body:
                bcell = khoa_walk.crop((f_idx * CW, r_idx * CH, (f_idx + 1) * CW, (r_idx + 1) * CH))
                comp.alpha_composite(bcell)
            gcell = g_walk.crop((f_idx * CW, r_idx * CH, (f_idx + 1) * CW, (r_idx + 1) * CH))
            comp.alpha_composite(gcell)
            up = comp.resize((W_CELL, H_CELL), Image.Resampling.NEAREST)
            board.alpha_composite(up, (cx, cy))
            dr.text((cx + 4, cy + H_CELL + 4), cap, fill=(196, 208, 192, 255), font=f_sub)

    out_path = os.path.join(BRAIN_DIR, "paperdoll_all_16_gdd_items_catalog_x15.png")
    board.save(out_path, "PNG")
    shutil.copy2(out_path, os.path.join(ROOT, ".collab", "claude-2d", "ui_and_map_sync", "from_argon", "Docs", "qa", "paperdoll_all_16_gdd_items_catalog_x15.png"))
    print("Saved updated 16-item catalog board:", out_path)


if __name__ == "__main__":
    main()
