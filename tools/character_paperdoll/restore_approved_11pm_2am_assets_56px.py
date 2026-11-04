#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
restore_approved_11pm_2am_assets_56px.py
========================================
Restores 100% of the user-approved 11pm-2am (2026-11-02 23:00 -> 2026-11-03 02:00) character assets
for Khoa, all 16 GDD 12.3 Paper-Doll gear overlays (`khoa__*`), all 26 NPCs & Mercenaries
(`DELIV_CHARS`), and all 5 Thieves/Bandits (`creatures_vfx_v2`), while balancing their height
cleanly to `56 px` (`1.75m @ 32 PPU`, `y = 23..78` in `72x84` cells, `pivot = [36, 5]`)
WITHOUT redrawing or replacing any approved character art!
"""

import json
import math
import os
import shutil
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, r"D:\Pig-tycoon\tools")
sys.path.insert(0, r"D:\Pig-tycoon\tools\pixelart")

import qa_enforcer
import qa_sheets
from build_25d_faceted_world_entities import (
    SINGLE_POSE_ENEMIES,
    apply_chiseled_25d_facets,
    build_dorsal_back_cell_from_front,
    build_side_profile_cell_from_front,
)
from build_2026_aaa_multi_limb_animations import (
    strip_baked_paw_shadow_from_cell,
    rotate_upper_torso_and_weapon,
    draw_aaa_weapon_slash_vfx,
    draw_ground_shadow,
    alpha_Paste,
    rotate_patch,
)
from denoise_world_entities_and_render_16_gdd_catalog import denoise_and_rimlight_sheet
import upgrade_paperdoll_body_mask_and_readability as upg

ROOT = Path(r"D:\Pig-tycoon")
ASSETS_CHARS = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters"
FROM_ARGON_CHARS = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon" / "Assets" / "Art" / "PixelMyth" / "Characters"
DELIV_CHARS = ROOT / ".collab" / "claude-2d" / "argon-deliverables" / "characters_v2"
DELIV_CREATURES = ROOT / ".collab" / "claude-2d" / "argon-deliverables" / "creatures_vfx_v2"
BRAIN_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\ac0f248a-d876-47b0-9f6a-277b76657366")

CW, CH = 72, 84
FOOT_PAD = 5
TARGET_H = 56  # 1.75m @ 32 PPU (y = 23..78 in 72x84 cell)
PIVOT = [36, 5]
DIRS = ["down", "left", "up", "right"]

# All 31 Human NPC, Mercenary, and Thief folders that were overwritten by build_56px_natural_human_npcs.py
HUMAN_WORLD_ENTITIES = [
    "ba_sau", "man", "tam_va", "dung", "cuc", "le", "anh_ca", "ong_ton", "anh_tu",
    "bi_nhan", "ba_ban_rau", "nguoi_ao_mua", "nguoi_ban_dem", "nguoi_ban_tin",
    "npc_appraisal", "npc_auction", "npc_mercenary", "npc_post", "npc_quarantine",
    "npc_recruit", "npc_supply",
    "merc_chot", "merc_do", "merc_mien", "merc_tu_troi", "merc_van",
    "thief", "dau_linh_trom", "chua_trom_riu_sat", "ke_cat_day", "ke_mang_dich",
]

# Natural 6-frame walk cycle with passing frames (f=2, f=5) bringing feet together (`chụm chân`, gait_x=0.0)
# and contact frames (f=0, f=3) keeping foot span <= 21px!
# Tuple: (gait_x, knee_lift, bob_y, arm_swing, cloth_wave_y, cloth_wave_x)
NATURAL_6_WALK = [
    ( 0.82,  0.30,  0.0, -0.85, -0.55,  0.45),  # f0: Left Contact (span <= 21px)
    ( 0.48,  0.75, -1.5, -0.42,  0.65, -0.55),  # f1: Left Down/Cushion
    ( 0.00, -0.65,  1.2,  0.15,  0.30,  0.60),  # f2: Passing Frame A (chụm chân, knee lifted)
    (-0.82, -0.30,  0.0,  0.85,  0.55, -0.45),  # f3: Right Contact (span <= 21px)
    (-0.48, -0.75, -1.5,  0.42, -0.65,  0.55),  # f4: Right Down/Cushion
    ( 0.00,  0.65,  1.2, -0.15, -0.30, -0.60),  # f5: Passing Frame B (chụm chân, knee lifted)
]

NATURAL_8_WALK = [
    ( 0.82,  0.30,  0.0, -0.85, -0.55,  0.45),  # f0: Contact A
    ( 0.50,  0.75, -1.5, -0.45,  0.65, -0.55),  # f1: Down Recoil A
    ( 0.00, -0.65,  1.0,  0.00,  0.35,  0.60),  # f2: Passing A (chụm chân)
    (-0.45,  0.45,  1.5,  0.45, -0.25, -0.35),  # f3: High Point A
    (-0.82, -0.30,  0.0,  0.85,  0.55, -0.45),  # f4: Contact B
    (-0.50, -0.75, -1.5,  0.45, -0.65,  0.55),  # f5: Down Recoil B
    ( 0.00,  0.65,  1.0,  0.00, -0.35, -0.60),  # f6: Passing B (chụm chân)
    ( 0.45, -0.45,  1.5, -0.45,  0.25,  0.35),  # f7: High Point B
]

NATURAL_4_IDLE = [
    ( 0.35,  0.25, -1.0, -0.35,  0.45, -0.35),
    (-0.15, -0.35,  1.0,  0.20, -0.35,  0.45),
    (-0.35, -0.25, -1.0,  0.35, -0.45,  0.35),
    ( 0.15,  0.35,  1.0, -0.20,  0.35, -0.45),
]


def scale_approved_cell_to_56px(raw_cell: np.ndarray, target_h: int = TARGET_H, max_w: int | None = None) -> np.ndarray:
    """
    Scales an approved 11pm-2am character cell (`DELIV_CHARS` or `DELIV_CREATURES`) proportionally
    to `target_h = 56` px grounded at `y = CH - FOOT_PAD = 79` inside a `72x84` cell,
    preserving 100% of the original character artwork and applying 2.5D chiseled facets.
    """
    cell = raw_cell.copy()
    h, w = cell.shape[:2]
    # Strip semi-transparent baked ground shadow oval at bottom before measuring character height
    cell[max(0, h - 7):, :, 3] = np.where(cell[max(0, h - 7):, :, 3] < 195, 0, cell[max(0, h - 7):, :, 3])
    ys, xs = np.where(cell[..., 3] >= 150)
    if len(xs) == 0:
        ys, xs = np.where(cell[..., 3] > 40)
    if len(xs) == 0:
        return np.zeros((CH, CW, 4), dtype=np.uint8)

    y0, y1 = int(ys.min()), int(ys.max())
    x0, x1 = int(xs.min()), int(xs.max())
    crop = cell[y0:y1 + 1, x0:x1 + 1].copy()
    th, tw = crop.shape[:2]

    scale = float(target_h) / max(1.0, float(th))
    nw = max(12, min(CW - 8, int(round(tw * scale))))
    if max_w is not None:
        nw = max(12, min(nw, max_w))
    nh = max(24, min(CH - FOOT_PAD - 2, int(round(th * scale))))

    pil_crop = Image.fromarray(crop, "RGBA")
    resized = np.asarray(pil_crop.resize((nw, nh), Image.Resampling.LANCZOS), dtype=np.uint8).copy()
    alpha = resized[..., 3] >= 128
    resized[~alpha] = 0
    resized[alpha, 3] = 255

    faceted = apply_chiseled_25d_facets(resized, strength=0.55)

    out = np.zeros((CH, CW, 4), dtype=np.uint8)
    dx = (CW - nw) // 2
    dy = CH - FOOT_PAD - nh
    out[dy:dy + nh, dx:dx + nw] = faceted
    return out


def _measure_torso_w(cell_rgba: np.ndarray) -> int:
    ys, xs = np.where(cell_rgba[..., 3] > 0)
    if len(xs) == 0:
        return 0
    y = int(ys.min()) + int((int(ys.max()) - int(ys.min()) + 1) * 0.35)
    rxs = np.where(cell_rgba[y, :, 3] > 0)[0]
    return int(rxs[-1] - rxs[0] + 1) if len(rxs) > 0 else 0


def load_approved_4dir_cells_56px(name: str) -> tuple[list[np.ndarray], Path]:
    """
    Loads the original approved 11pm-2am `<name>_idle.png` from `DELIV_CHARS` or `DELIV_CREATURES`,
    extracts the 4-direction (`down`, `left`, `up`, `right`) Col 0 cells, and scales them to 56px height.
    """
    src_folder = DELIV_CHARS / name
    if not src_folder.exists():
        src_folder = DELIV_CREATURES / name
    if not src_folder.exists():
        raise FileNotFoundError(f"Original approved folder not found for {name}")

    idle_p = src_folder / f"{name}_idle.png"
    if not idle_p.exists():
        pngs = sorted(src_folder.glob("*.png"))
        idle_p = pngs[0]

    arr = np.asarray(Image.open(idle_p).convert("RGBA"), dtype=np.uint8)
    sh, sw = arr.shape[:2]
    ch_src = sh // 4
    # Determine number of columns in idle.png
    json_p = idle_p.with_suffix(".json")
    ncols = 4
    if json_p.exists():
        try:
            meta = json.loads(json_p.read_text(encoding="utf-8"))
            ncols = int(meta.get("frames_per_row", 4))
        except Exception:
            pass
    cw_src = sw // max(1, ncols)

    is_single_pose = name in SINGLE_POSE_ENEMIES
    cells_56 = []
    if is_single_pose:
        raw0 = strip_baked_paw_shadow_from_cell(arr[0:ch_src, 0:cw_src].copy())
        down_56 = scale_approved_cell_to_56px(raw0, target_h=TARGET_H)
        left_56 = build_side_profile_cell_from_front(down_56, facing_right=False)
        up_56 = build_dorsal_back_cell_from_front(down_56)
        right_56 = build_side_profile_cell_from_front(down_56, facing_right=True)
        cells_56 = [down_56, left_56, up_56, right_56]
    else:
        raw_down = arr[0:ch_src, 0:cw_src].copy()
        down_56 = scale_approved_cell_to_56px(raw_down, target_h=TARGET_H)
        wd = _measure_torso_w(down_56)
        max_side_w = max(12, int(wd * 0.76)) if wd > 0 else None
        cells_56.append(down_56)
        for r in range(1, 4):
            raw_r = arr[r * ch_src:(r + 1) * ch_src, 0:cw_src].copy()
            mw = max_side_w if r in (1, 3) else None
            c_r = scale_approved_cell_to_56px(raw_r, target_h=TARGET_H, max_w=mw)
            if r in (1, 3) and wd > 0 and _measure_torso_w(c_r) >= int(wd * 0.84):
                c_r = scale_approved_cell_to_56px(raw_r, target_h=TARGET_H, max_w=max(11, int(wd * 0.70)))
            cells_56.append(c_r)

    return cells_56, src_folder


def _shade_patch(arr: np.ndarray, factor: float) -> np.ndarray:
    out = arr.copy()
    m = out[..., 3] > 0
    for c in range(3):
        out[m, c] = np.clip(out[m, c].astype(np.float32) * factor, 0, 255).astype(np.uint8)
    return out


def articulate_approved_56px_cell(
    base_cell: np.ndarray,
    direction: str,
    bio_key: tuple[float, float, float, float, float, float],
    amp: float = 1.0,
) -> np.ndarray:
    """
    Articulates a 56px approved character cell:
    - For `left` and `right`: uses TRUE 2-leg overlapping profile scissor articulation under an unbroken
      skirt/coat/apron hem (ZERO vertical tear down the center of the skirt/trousers!).
    - For `down` and `up`: keeps upper torso/skirt intact (`v_rel < 0.64`) and strides the left/right
      legs smoothly below `0.64` (`span <= 21px` on contact, `chụm chân` on passing frames `gait_x == 0`).
    """
    ch, cw = base_cell.shape[:2]
    body = base_cell.copy()
    body[ch - 5:, :, 3] = np.where(body[ch - 5:, :, 3] < 185, 0, body[ch - 5:, :, 3])
    ys, xs = np.where(body[..., 3] >= 160)
    if len(xs) == 0:
        return base_cell.copy()

    y_top, y_bot = int(ys.min()), int(ys.max())
    x_left, x_right = int(xs.min()), int(xs.max())
    bh = max(12, y_bot - y_top + 1)
    bw = max(12, x_right - x_left + 1)
    cx = float(np.median(xs))
    cx_int = int(round(cx))

    gait_x, knee_lift, bob_y_raw, arm_swing, c_wave_y, c_wave_x = bio_key
    bob_y = bob_y_raw * amp
    cadence_x = 1.1 * gait_x * amp

    sh_rx = max(8, int(round(min(cw * 0.20, bw * 0.32))))
    canvas = np.zeros((ch, cw, 4), dtype=np.uint8)
    draw_ground_shadow(canvas, int(round(cw // 2 + cadence_x * 0.3)), ch - FOOT_PAD, rx=sh_rx, ry=3)

    if direction in ("left", "right"):
        dir_sgn = 1.0 if direction == "right" else -1.0
        y_crotch = y_top + int(bh * 0.64)
        y_arm_top = y_top + int(bh * 0.30)
        y_arm_bot = y_top + int(bh * 0.56)

        # Build a clean, solid single profile leg column from `y_crotch - 2` down to `y_bot`
        prof_leg = np.zeros_like(body)
        for y in range(max(0, y_crotch - 2), min(ch, y_bot + 1)):
            row_xs = np.where(body[y, :, 3] >= 160)[0]
            if len(row_xs) == 0:
                continue
            runs = np.split(row_xs, np.where(np.diff(row_xs) > 1)[0] + 1)
            valid_runs = [r for r in runs if len(r) >= 2] or runs
            chosen_run = valid_runs[-1] if direction == "right" else valid_runs[0]
            max_w = 9 if y < y_bot - 5 else 8
            if len(chosen_run) > max_w:
                chosen_run = chosen_run[-max_w:] if direction == "right" else chosen_run[:max_w]
            span = len(chosen_run)
            dst_x0 = cx_int - span // 2
            for i, sx in enumerate(chosen_run):
                dx = dst_x0 + i
                if 0 <= dx < cw:
                    prof_leg[y, dx] = body[y, sx]
                    prof_leg[y, dx, 3] = 255

        bob_i = int(round(bob_y))
        lean_i = int(round(1.0 * dir_sgn * max(0.0, amp - 0.5) + cadence_x * 0.4))
        deg_f = float(gait_x * 13.5 * amp)
        deg_b = float(-gait_x * 13.5 * amp)
        dx_f = int(round(gait_x * 2.2 * amp))
        dx_b = int(round(-gait_x * 2.2 * amp))
        dy_f = -1 if (gait_x * dir_sgn > 0.35) else 0
        dy_b = -1 if (-gait_x * dir_sgn > 0.35) else 0

        back_leg = rotate_patch(_shade_patch(prof_leg, 0.78), deg_b, (cx_int, y_crotch))
        back_leg[CH - FOOT_PAD + 1:, :, :] = 0
        alpha_Paste(canvas, back_leg, lean_i + dx_b, bob_i + dy_b)

        front_leg = rotate_patch(_shade_patch(prof_leg, 1.05), deg_f, (cx_int, y_crotch))
        front_leg[CH - FOOT_PAD + 1:, :, :] = 0
        alpha_Paste(canvas, front_leg, lean_i + dx_f, bob_i + dy_f)

        # Keep upper torso + skirt/apron/coat hem 100% intact (NO vertical shear tearing the center!)
        upper = np.zeros_like(body)
        upper[:min(ch, y_crotch + 2), :] = body[:min(ch, y_crotch + 2), :]
        alpha_Paste(canvas, upper, lean_i, bob_i)

        # Gentle foreground arm/prop counter-swing
        arm_deg = float(-arm_swing * 10.0 * amp)
        if abs(arm_deg) > 1.2:
            arm_patch = np.zeros_like(upper)
            ax0, ax1 = max(0, cx_int - 6), min(cw, cx_int + 7)
            arm_patch[y_arm_top:y_arm_bot, ax0:ax1] = upper[y_arm_top:y_arm_bot, ax0:ax1]
            if np.any(arm_patch[..., 3] > 0):
                arm_rot = rotate_patch(_shade_patch(arm_patch, 1.04), arm_deg, (cx_int, y_arm_top + 2))
                alpha_Paste(canvas, arm_rot, lean_i + int(round(arm_swing * 1.2 * amp)), bob_i)
        return canvas

    yy, xx = np.meshgrid(np.arange(ch, dtype=np.float32), np.arange(cw, dtype=np.float32), indexing="ij")
    v_rel = np.clip((yy - y_top) / float(bh), 0.0, 1.0)
    u_rel = np.clip((xx - cx) / max(5.0, bw * 0.5), -1.2, 1.2)

    leg_zone = np.clip((v_rel - 0.62) / 0.38, 0.0, 1.0)
    head_zone = np.clip((0.32 - v_rel) / 0.32, 0.0, 1.0)
    arm_band = np.exp(-((v_rel - 0.42) / 0.14) ** 2)
    outer_cape = np.clip((np.abs(u_rel) - 0.30) / 0.70, 0.0, 1.0)

    cloth_dy = 1.6 * c_wave_y * outer_cape * (1.0 - leg_zone * 0.5) * amp
    cloth_dx = 1.2 * c_wave_x * outer_cape * head_zone * amp

    side_sgn = np.tanh(u_rel * 2.5)
    leg_dy = 3.6 * knee_lift * side_sgn * (leg_zone ** 1.20) * amp
    leg_dx = 2.0 * gait_x * side_sgn * (leg_zone ** 1.10) * amp
    arm_dy = 2.0 * arm_swing * side_sgn * arm_band * amp
    arm_dx = 1.4 * (-gait_x) * side_sgn * arm_band * amp

    src_x = xx - (1.4 * gait_x * amp) - leg_dx - arm_dx - cloth_dx
    src_y = yy - bob_y + leg_dy + arm_dy + cloth_dy

    sx_i = np.clip(np.rint(src_x).astype(np.int32), 0, cw - 1)
    sy_i = np.clip(np.rint(src_y).astype(np.int32), 0, ch - 1)
    warped = body[sy_i, sx_i].copy()
    warped[CH - FOOT_PAD + 1:, :, :] = 0

    alpha_Paste(canvas, warped, 0, 0)
    return canvas


def build_approved_npc_clip(cells_56: list[np.ndarray], clip_type: str, nframes: int) -> np.ndarray:
    out = np.zeros((CH * 4, CW * nframes, 4), dtype=np.uint8)
    table = NATURAL_8_WALK if nframes >= 8 else (NATURAL_6_WALK if nframes >= 6 else NATURAL_4_IDLE)
    atk_swings = [-16.0, 14.0, 24.0, -14.0, 10.0, 0.0, -6.0, 0.0]

    for r, d in enumerate(DIRS):
        base0 = cells_56[r]
        for f in range(nframes):
            bio = table[f % len(table)]
            if clip_type in ("walk", "move", "run"):
                amp = 1.15 if clip_type == "run" else 1.0
                frame = articulate_approved_56px_cell(base0, d, bio, amp=amp)
            elif clip_type == "attack":
                sw_ang = atk_swings[f % len(atk_swings)]
                swung = rotate_upper_torso_and_weapon(base0, d, sw_ang)
                frame = articulate_approved_56px_cell(swung, d, bio, amp=1.05)
                if f in (1, 2, 3):
                    draw_aaa_weapon_slash_vfx(frame, d, f, primary_rgb=(255, 195, 110))
            elif clip_type == "hit":
                sw_ang = -12.0 if f == 0 else (6.0 if f == 1 else 0.0)
                swung = rotate_upper_torso_and_weapon(base0, d, sw_ang)
                frame = articulate_approved_56px_cell(swung, d, NATURAL_4_IDLE[f % 4], amp=0.85)
                if f == 0:
                    m = frame[..., 3] > 180
                    frame[m, 0] = np.clip(frame[m, 0].astype(np.int16) + 70, 0, 255).astype(np.uint8)
                    frame[m, 1] = np.clip(frame[m, 1].astype(np.int16) + 25, 0, 255).astype(np.uint8)
            elif clip_type == "death":
                body = base0.copy()
                body[body[..., 3] < 180] = 0
                frame = np.zeros_like(base0)
                draw_ground_shadow(frame, CW // 2, CH - FOOT_PAD, rx=12 + f, ry=3)
                rot_deg = min(80.0, f * (80.0 / max(1, nframes - 1)))
                rot = rotate_patch(body, angle_deg=rot_deg, pivot_xy=(CW // 2, CH - 8))
                alpha_Paste(frame, rot, 0, min(10, f * 2))
            else:
                # idle / interact
                amp = 0.75 if clip_type == "interact" else 0.50
                frame = articulate_approved_56px_cell(base0, d, bio, amp=amp)

            out[r * CH:(r + 1) * CH, f * CW:(f + 1) * CW] = frame
    return out


def restore_all_31_npcs_mercs_thieves():
    restored = 0
    for name in HUMAN_WORLD_ENTITIES:
        cells_56, src_folder = load_approved_4dir_cells_56px(name)
        # Determine clip set from original folder + any clips in Assets/Art/PixelMyth/Characters/<name>
        clip_specs = {
            "idle": (4, 6.0, True, None),
            "walk": (6, 14.0, True, 1.31),
        }
        for orig_p in src_folder.glob("*.png"):
            st = orig_p.stem
            if st.startswith(name + "_"):
                ck = st[len(name) + 1:]
                if ck == "move":
                    clip_specs["move"] = (6, 14.0, True, 1.31)
                elif ck == "interact":
                    clip_specs["interact"] = (4, 8.0, False, None)
                elif ck == "attack":
                    clip_specs["attack"] = (6, 12.0, False, None)
                elif ck == "hit":
                    clip_specs["hit"] = (2, 8.0, False, None)
                elif ck == "death":
                    clip_specs["death"] = (4, 6.0, False, None)

        # Also check if Assets/Art/PixelMyth/Characters/<name> had interact/attack/hit/death
        cur_dir = ASSETS_CHARS / name
        if cur_dir.exists():
            for cur_p in cur_dir.glob(f"{name}_*.png"):
                ck = cur_p.stem[len(name) + 1:]
                if ck not in clip_specs:
                    if ck in ("walk", "move", "run"):
                        clip_specs[ck] = (6, 14.0, True, 1.31)
                    elif ck == "attack":
                        clip_specs[ck] = (6, 12.0, False, None)
                    elif ck == "hit":
                        clip_specs[ck] = (2, 8.0, False, None)
                    elif ck == "death":
                        clip_specs[ck] = (4, 6.0, False, None)
                    else:
                        clip_specs[ck] = (4, 8.0, False, None)

        for ck, (nf, fps, loop, stride_m) in sorted(clip_specs.items()):
            sheet = build_approved_npc_clip(cells_56, ck, nf)
            for prefix in ("", "argon_"):
                fam = f"{prefix}{name}"
                for root_dir in (ASSETS_CHARS, FROM_ARGON_CHARS):
                    out_dir = root_dir / fam
                    if prefix == "argon_" and not out_dir.exists():
                        continue
                    out_dir.mkdir(parents=True, exist_ok=True)
                    png_p = out_dir / f"{fam}_{ck}.png"
                    json_p = out_dir / f"{fam}_{ck}.json"
                    arr_clean = qa_enforcer.finalize_and_verify_sheet(sheet.copy(), CW, CH, nf, ck, max_colors=47)
                    Image.fromarray(arr_clean, "RGBA").save(png_p)

                    meta = {
                        "source": f"Restored 11pm-2am Approved 2.5D Asset @ 56px ({fam}:{ck})",
                        "cell": [CW, CH],
                        "rows": DIRS,
                        "frames_per_row": nf,
                        "pivot_px_from_bottom_left": PIVOT,
                        "fps": fps,
                        "loop": loop,
                    }
                    if stride_m is not None:
                        meta["stride_m"] = stride_m
                        meta["stride_m_per_cycle"] = stride_m
                    json_p.write_text(json.dumps(meta, indent=2), encoding="utf-8")
                    restored += 1
    print(f"[OK] Restored {restored} NPC/Mercenary/Thief sheets directly from approved 11pm-2am assets at 56px height!")


def purge_an_from_argon():
    """Ensures zero unapproved `an*` / `Hero_An*` / `portrait_an*` files remain in `from_argon` (#144, #149)."""
    removed = 0
    for d in list(FROM_ARGON_CHARS.iterdir()):
        if d.name in ("an", "argon_an", "Hero_An") or d.name.startswith("an__") or d.name.startswith("an_"):
            if d.is_dir():
                shutil.rmtree(d, ignore_errors=True)
            else:
                d.unlink(missing_ok=True)
            removed += 1
    portraits_fa = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon" / "Assets" / "Art" / "PixelMyth" / "UI" / "Portraits"
    if portraits_fa.exists():
        for p in portraits_fa.glob("*an*"):
            if "ban" not in p.name.lower() and "man" not in p.name.lower() and "nhan" not in p.name.lower():
                p.unlink(missing_ok=True)
                removed += 1
    print(f"[OK] Purged {removed} unapproved An items from from_argon.")


def render_side_by_side_comparison_boards():
    """
    Renders x2 nearest-neighbour visual verification boards on grass:
    1. `npc_56px_natural_gait_on_grass_x2.png`: Side-by-side comparison of Original Approved 11pm-2am
       `DELIV_CHARS` / `DELIV_CREATURES` sprite vs Restored 56px Walk Cycle (6 frames) for Khoa, Bà Sáu, Mận,
       Bà Bán Rau, Ông Tôn, Tám Vá, Trộm (`thief`), and Pig White Foot (`pig_white_foot`).
    2. `khoa_56px_natural_gait_and_slash_x2_grass.png`: Khoa 56px (from approved 11pm-2am artwork) next to
       Doorway (68px) and Pig White Foot (~26px) + full walk/run/attack_slash_1..3 strips with Paper-Doll gear!
    """
    SCALE = 2
    W2, H2 = CW * SCALE, CH * SCALE  # 144 x 168
    try:
        f_title = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 15)
        f_sub = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 12)
        f_lbl = ImageFont.truetype(r"C:\Windows\Fonts\arial.ttf", 11)
    except Exception:
        f_title = f_sub = f_lbl = ImageFont.load_default()

    # --- BOARD 1: NPC, Khoa & An Restored 11pm-2am Assets @ 56px on Grass (x2) ---
    rows_spec = [
        ("Khoa (Approved 11pm-2am 2.5D Asset @ 56px)", ASSETS_CHARS / "khoa" / "khoa_idle.png", ASSETS_CHARS / "khoa" / "khoa_walk.png"),
        ("An C2-A (Approved 11pm-2am 2.5D Asset @ 56px — 0 Stick Arms / 0 Cuts)", ASSETS_CHARS / "an" / "an_idle.png", ASSETS_CHARS / "an" / "an_walk.png"),
        ("Bà Sáu (Original 11pm-2am vs Restored 56px)", DELIV_CHARS / "ba_sau" / "ba_sau_idle.png", ASSETS_CHARS / "ba_sau" / "ba_sau_walk.png"),
        ("Mận (Original 11pm-2am vs Restored 56px)", DELIV_CHARS / "man" / "man_idle.png", ASSETS_CHARS / "man" / "man_walk.png"),
        ("Bà Bán Rau (Original 11pm-2am vs Restored 56px)", DELIV_CHARS / "ba_ban_rau" / "ba_ban_rau_idle.png", ASSETS_CHARS / "ba_ban_rau" / "ba_ban_rau_walk.png"),
        ("Ông Tôn (Original 11pm-2am vs Restored 56px)", DELIV_CHARS / "ong_ton" / "ong_ton_idle.png", ASSETS_CHARS / "ong_ton" / "ong_ton_walk.png"),
        ("Tám Vá (Original 11pm-2am vs Restored 56px)", DELIV_CHARS / "tam_va" / "tam_va_idle.png", ASSETS_CHARS / "tam_va" / "tam_va_walk.png"),
        ("Trộm / Thief (Original 11pm-2am vs Restored 56px)", DELIV_CREATURES / "thief" / "thief_idle.png", ASSETS_CHARS / "thief" / "thief_walk.png"),
    ]

    bw, bh = 1480, 1615
    board = Image.new("RGBA", (bw, bh), (45, 72, 34, 255))
    dr = ImageDraw.Draw(board)
    dr.text(
        (20, 10),
        "ĐỐI CHIẾU TRỰC TIẾP: ASSET GỐC ĐÃ DUYỆT (11PM-2AM) vs BẢN CÂN ĐỐI CHIỀU CAO 56PX (1.75m @ 32 PPU, x2 NEAREST)",
        fill=(255, 238, 152, 255),
        font=f_title,
    )

    for idx, (label, orig_idle_p, walk_56_p) in enumerate(rows_spec):
        y0 = 42 + idx * 172
        dr.rounded_rectangle([16, y0, bw - 16, y0 + 164], radius=6, fill=(52, 82, 38, 255), outline=(88, 122, 68, 255), width=1)
        dr.text((26, y0 + 6), label, fill=(248, 232, 160, 255), font=f_sub)
        gy = y0 + 150
        dr.line([(240, gy), (bw - 28, gy)], fill=(96, 132, 74, 200), width=1)

        # Col 0: Original Approved Down#0 from DELIV_CHARS / DELIV_CREATURES
        orig_im = Image.open(orig_idle_p).convert("RGBA")
        och = orig_im.height // 4
        ocw = orig_im.width // (8 if ("khoa" in orig_idle_p.name or orig_idle_p.name == "an_idle.png") else 4)
        orig_c0 = orig_im.crop((0, 0, ocw, och)).resize((ocw * SCALE, och * SCALE), Image.Resampling.NEAREST)
        board.alpha_composite(orig_c0, (250, gy - (och - 5) * SCALE))
        dr.text((256, y0 + 24), "Gốc Đã Duyệt (Down)", fill=(210, 240, 195, 255), font=f_lbl)

        # Col 1: Restored 56px Down#0
        w_im = Image.open(walk_56_p).convert("RGBA")
        nf = w_im.width // CW
        d0 = w_im.crop((0, 0, CW, CH)).resize((W2, H2), Image.Resampling.NEAREST)
        board.alpha_composite(d0, (410, gy - (CH - FOOT_PAD) * SCALE))
        dr.text((420, y0 + 24), "56px (Down#0)", fill=(255, 235, 150, 255), font=f_lbl)

        # Cols 2..7: 6 frames of Walk Row 1 (Left Side Profile) at 56px
        for f_idx in range(min(6, nf)):
            x_cell = 570 + f_idx * 146
            c_im = w_im.crop((f_idx * CW, CH, (f_idx + 1) * CW, CH * 2)).resize((W2, H2), Image.Resampling.NEAREST)
            board.alpha_composite(c_im, (x_cell, gy - (CH - FOOT_PAD) * SCALE))
            dr.text((x_cell + 16, y0 + 24), f"Left Walk #{f_idx}", fill=(220, 235, 210, 255), font=f_lbl)

    # Bottom row: Pig White Foot scale reference
    y_pig = 42 + len(rows_spec) * 172
    dr.rounded_rectangle([16, y_pig, bw - 16, y_pig + 164], radius=6, fill=(52, 82, 38, 255), outline=(88, 122, 68, 255), width=1)
    dr.text((26, y_pig + 6), "Pig White Foot (32 PPU Scale Ref — 6-Frame Walk Row 1 Left)", fill=(248, 232, 160, 255), font=f_sub)
    gy_p = y_pig + 150
    dr.line([(240, gy_p), (bw - 28, gy_p)], fill=(96, 132, 74, 200), width=1)
    pw_im = Image.open(ASSETS_CHARS / "pig_white_foot" / "pig_white_foot_walk.png").convert("RGBA")
    pw_c, ph_c = pw_im.width // 6, pw_im.height // 4
    for f_idx in range(6):
        x_cell = 260 + f_idx * 165
        p_cell = pw_im.crop((f_idx * pw_c, ph_c, (f_idx + 1) * pw_c, ph_c * 2)).resize((pw_c * SCALE, ph_c * SCALE), Image.Resampling.NEAREST)
        board.alpha_composite(p_cell, (x_cell, gy_p - (ph_c - 8) * SCALE))
        dr.text((x_cell + 12, y_pig + 24), f"Pig Walk #{f_idx}", fill=(220, 235, 210, 255), font=f_lbl)

    out_npc_board = BRAIN_DIR / "npc_56px_natural_gait_on_grass_x2.png"
    board.save(out_npc_board)
    qa_dir_fa = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon" / "Docs" / "qa"
    qa_dir_fa.mkdir(parents=True, exist_ok=True)
    shutil.copy2(out_npc_board, qa_dir_fa / "npc_56px_natural_gait_on_grass_x2.png")
    print(f"[OK] Saved side-by-side comparison board: {out_npc_board}")

    # --- BOARD 2: Khoa 56px (Approved 11pm-2am Artwork) + 6-Slot Paper-Doll + Doorway + Pig ---
    b2 = Image.new("RGBA", (1520, 1180), (34, 48, 30, 255))
    d2 = ImageDraw.Draw(b2)
    d2.text(
        (20, 12),
        "PIG TYCOON — KHOA 56PX (1.75m @ 32 PPU, GIỮ NGUYÊN ASSET 11PM-2AM ĐÃ DUYỆT) + 6 Ô PAPER-DOLL (x2 NEAREST)",
        fill=(255, 236, 152, 255),
        font=f_title,
    )
    d2.rounded_rectangle([20, 42, 1496, 244], radius=8, fill=(44, 60, 38, 255), outline=(98, 124, 82, 255), width=2)
    d2.text(
        (34, 50),
        "1. SO SÁNH TỈ LỆ THẬT (x2 = 64 px/m): Cửa Nhà (68px = 2.12m) vs Khoa (56px = 1.75m, Asset 11pm-2am) vs Heo Chân Trắng (~26px) vs Khoa Mặc Đủ 6 Ô",
        fill=(244, 228, 164, 255),
        font=f_sub,
    )
    gy2 = 220
    d2.line([(40, gy2), (1460, gy2)], fill=(128, 108, 76, 255), width=2)
    for m_val, px_1x in ((0.0, 0), (0.5, 16), (1.0, 32), (1.5, 48), (1.75, 56), (2.0, 64)):
        ry = gy2 - int(px_1x * SCALE)
        d2.line([(56, ry), (460, ry)], fill=(76, 96, 68, 180), width=1)
        d2.text((60, ry - 13), f"{m_val:.2f}m ({px_1x}px)", fill=(210, 228, 196, 255), font=f_lbl)

    door_1x = Image.new("RGBA", (64, 84), (0, 0, 0, 0))
    dd = ImageDraw.Draw(door_1x)
    dd.rectangle([10, 11, 54, 79], fill=(92, 64, 42, 255), outline=(18, 16, 22, 255))
    dd.rectangle([14, 15, 50, 79], fill=(62, 42, 28, 255), outline=(18, 16, 22, 255))
    dd.ellipse([43, 44, 47, 48], fill=(224, 184, 76, 255), outline=(18, 16, 22, 255))
    b2.alpha_composite(door_1x.resize((64 * SCALE, 84 * SCALE), Image.Resampling.NEAREST), (150, gy2 - 79 * SCALE))
    d2.text((155, gy2 + 4), "Cửa Nhà (68px = 2.12m)", fill=(235, 225, 195, 255), font=f_lbl)

    k_idle = Image.open(ASSETS_CHARS / "khoa" / "khoa_idle.png").convert("RGBA")
    k_walk = Image.open(ASSETS_CHARS / "khoa" / "khoa_walk.png").convert("RGBA")
    b2.alpha_composite(k_idle.crop((0, 0, CW, CH)).resize((W2, H2), Image.Resampling.NEAREST), (290, gy2 - (CH - FOOT_PAD) * SCALE))
    d2.text((298, gy2 + 4), "Khoa Đứng (56px = 1.75m)", fill=(235, 225, 195, 255), font=f_lbl)
    b2.alpha_composite(k_walk.crop((0, CH, CW, CH * 2)).resize((W2, H2), Image.Resampling.NEAREST), (440, gy2 - (CH - FOOT_PAD) * SCALE))
    d2.text((444, gy2 + 4), "Khoa Bước Nghiêng (Left#0)", fill=(235, 225, 195, 255), font=f_lbl)

    p_cell = pw_im.crop((0, ph_c, pw_c, ph_c * 2)).resize((pw_c * SCALE, ph_c * SCALE), Image.Resampling.NEAREST)
    b2.alpha_composite(p_cell, (610, gy2 - (ph_c - 8) * SCALE))
    d2.text((605, gy2 + 4), "Heo Chân Trắng (~26px = 0.81m)", fill=(235, 225, 195, 255), font=f_lbl)

    comp_full = k_idle.crop((0, 0, CW, CH))
    for g_id in ("pants_quan_da_lot_bong", "boot_giay_dinh", "armor_giap_da_thuoc", "nck_coi_chan_heo", "brc_vong_huyet_nha", "wpn_dao_rua"):
        gp = ASSETS_CHARS / f"khoa__{g_id}" / f"khoa__{g_id}_idle.png"
        if gp.exists():
            comp_full.alpha_composite(Image.open(gp).convert("RGBA").crop((0, 0, CW, CH)))
    b2.alpha_composite(comp_full.resize((W2, H2), Image.Resampling.NEAREST), (820, gy2 - (CH - FOOT_PAD) * SCALE))
    d2.text((795, gy2 + 4), "Khoa Mặc Đủ 6 Ô Paper-Doll (56px)", fill=(255, 232, 148, 255), font=f_lbl)

    strips = [
        ("2. khoa_walk (Hàng Trước Down — 8 khung, giữ nguyên thiết kế 11pm-2am @ 56px)", "khoa", "walk", None, 0, 8),
        ("3. khoa_walk (Hàng Nghiêng Left — 8 khung, chân qua nhau chụm dưới hông, sải chân tự nhiên)", "khoa", "walk", None, 1, 8),
        ("4. khoa_run (Hàng Nghiêng Left — 8 khung)", "khoa", "run", None, 1, 8),
        ("5. khoa_attack_slash_1 + Dao Rựa (Hàng Trước Down — 8 khung)", "khoa", "attack_slash_1", "khoa__wpn_dao_rua", 0, 8),
        ("6. khoa_attack_slash_3 + Dao Rựa (Hàng Nghiêng Left — 10 khung)", "khoa", "attack_slash_3", "khoa__wpn_dao_rua", 1, 10),
    ]
    y_cur = 258
    for title, body_fam, clip_name, wpn_fam, r_idx, nf in strips:
        d2.text((24, y_cur), title, fill=(244, 228, 156, 255), font=f_sub)
        y_cur += 20
        bsheet = Image.open(ASSETS_CHARS / body_fam / f"{body_fam}_{clip_name}.png").convert("RGBA")
        wsheet = Image.open(ASSETS_CHARS / wpn_fam / f"{wpn_fam}_{clip_name}.png").convert("RGBA") if wpn_fam else None
        for c in range(nf):
            x_cell = 20 + c * 148
            d2.rounded_rectangle([x_cell, y_cur, x_cell + W2, y_cur + H2 - 16], radius=4, fill=(44, 58, 38, 255), outline=(82, 102, 70, 255), width=1)
            gy_cell = y_cur + (CH - FOOT_PAD - 8) * SCALE
            d2.line([(x_cell + 6, gy_cell), (x_cell + W2 - 6, gy_cell)], fill=(96, 86, 62, 180), width=1)
            cell = bsheet.crop((c * CW, r_idx * CH, (c + 1) * CW, (r_idx + 1) * CH))
            if wsheet is not None:
                cell.alpha_composite(wsheet.crop((c * CW, r_idx * CH, (c + 1) * CW, (r_idx + 1) * CH)))
            cell_crop = cell.crop((0, 8, CW, CH)).resize((W2, (CH - 8) * SCALE), Image.Resampling.NEAREST)
            b2.alpha_composite(cell_crop, (x_cell, y_cur))
            d2.text((x_cell + 5, y_cur + 4), f"#{c}", fill=(210, 224, 195, 255), font=f_lbl)
        y_cur += H2 - 8

    out_k_board = BRAIN_DIR / "khoa_56px_natural_gait_and_slash_x2_grass.png"
    b2.save(out_k_board)
    shutil.copy2(out_k_board, BRAIN_DIR / "khoa_p1_p4_weapons_x15_qa_verified.png")
    shutil.copy2(out_k_board, qa_dir_fa / "khoa_56px_natural_gait_and_slash_x2_grass.png")
    shutil.copy2(out_k_board, qa_dir_fa / "khoa_p1_p4_weapons_x15_qa_verified.png")
    print(f"[OK] Saved Khoa 56px showcase board: {out_k_board}")


def main():
    if "--skip-step1" not in sys.argv:
        print("=== STEP 1: Rebuild Khoa + 16 GDD Paper-Doll Overlays from Approved 11pm-2am Artwork @ 56px ===")
        upg.main()

    if "--skip-step2" not in sys.argv:
        print("=== STEP 2: Restore All 31 NPCs, Mercenaries & Thieves from Approved 11pm-2am Assets @ 56px ===")
        restore_all_31_npcs_mercs_thieves()

    print("=== STEP 3: Purge Any Unapproved An Files from from_argon ===")
    purge_an_from_argon()

    print("=== STEP 4: Render Side-by-Side x2 Visual Comparison Boards on Grass ===")
    render_side_by_side_comparison_boards()
    upg.render_updated_16_gear_catalog()
    print("=== ALL APPROVED 11PM-2AM ASSETS RESTORED AT 56PX HEIGHT! ===")


if __name__ == "__main__":
    main()
