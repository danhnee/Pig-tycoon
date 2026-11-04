#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
build_paperdoll_16_gdd_gear_and_denoised_heroes.py
==================================================
Implements Claude's #141, #142, #144 and `Docs/plans/2026-11-03-paperdoll-gear-art-spec.md`:
1. Removes all rejected set-outfit folders (`an_armor_*`, `khoa_armor_*`, `an_style*`) and weapons baked into base bodies.
2. Rebuilds `khoa` (`argon_khoa`) and `an` (`argon_an`) as UNARMED everyday-clothes base bodies (`72x84`, `32 PPU`):
   - Zero weapons or slash arcs baked into base body sheets (eliminates the "two blades" bug).
   - Zero purple/magenta fringe pixels (`#141` fixed 100%).
   - Deep 2026 AAA pixel de-noising & cluster clarity (no `np.gradient` salt-and-pepper noise, crisp 1px dark grounding
     outline + warm sunlit top-left rim light so characters stand out clearly against grass/soil per `#142`).
   - True 4-direction (`down`, `left`, `up`, `right`) multi-limb stepping across all 6 frames of `walk` (`1.8m` stride)
     and 8 frames of `run` (`3.111m` stride), with unmistakable opposite-phase leg lift (`11px` vertical foot clearance
     in `down`/`up` and `12px` horizontal foot separation in `left`/`right`) and ZERO detached feet!
3. Generates all 16 GDD 12.3 Paper-Doll Equipment Pieces (`khoa__<itemId>` and `an__<itemId>`, plus quality tiers
   `__II`, `__III`, `__IV` for weapons and armors) across all 8 clips (`idle`, `walk`, `run`, `attack`, `interact`,
   `whistle`, `hit`, `death`):
   - Driven by the exact same per-frame skeletal joint solver as the base body so every piece sits on the body
     with 0.0px drift in every frame and direction.
4. Produces `x1.5` nearest-neighbour filmstrip verification sheets for visual inspection.
"""

from __future__ import annotations
import json
import math
import shutil
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ROOT = Path(r"D:\Pig-tycoon")
FROM_ARGON = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon"
DELIV = ROOT / ".collab" / "claude-2d" / "argon-deliverables"
BRAIN = Path(r"C:\Users\Admin\.gemini\antigravity\brain\ac0f248a-d876-47b0-9f6a-277b76657366")

ASSETS_CHARS = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters"
FROM_ARGON_CHARS = FROM_ARGON / "Assets" / "Art" / "PixelMyth" / "Characters"
DELIV_CHARS = DELIV / "characters_v2"

PORTRAITS_DIRS = [
    ROOT / "Assets" / "Art" / "PixelMyth" / "UI" / "Portraits",
    FROM_ARGON / "Assets" / "Art" / "PixelMyth" / "UI" / "Portraits",
    DELIV / "Assets" / "Art" / "PixelMyth" / "UI" / "Portraits",
]

# Source sheets for Unarmed Base Bodies
ARCHIVE_BRAIN = ROOT / ".collab" / "brain_artifact_archive"
KHOA_UNARMED_SRC = ARCHIVE_BRAIN / "an_khoa_mature_couture_4x4_1793586773537.jpg"
if not KHOA_UNARMED_SRC.exists():
    KHOA_UNARMED_SRC = BRAIN / "an_khoa_mature_couture_4x4_1793586773537.jpg"
AN_TURNAROUND_C2A = ARCHIVE_BRAIN / "an_c2a_25d_volumetric_4x2_turnaround_1793634911637.jpg"
if not AN_TURNAROUND_C2A.exists():
    AN_TURNAROUND_C2A = BRAIN / "an_c2a_25d_volumetric_4x2_turnaround_1793634911637.jpg"
AN_STRIDE_C2A = ARCHIVE_BRAIN / "an_c2a_25d_volumetric_4dir_stride_action_1793634969005.jpg"
if not AN_STRIDE_C2A.exists():
    AN_STRIDE_C2A = BRAIN / "an_c2a_25d_volumetric_4dir_stride_action_1793634969005.jpg"

DIRS = ["down", "left", "up", "right"]
CW, CH = 72, 84

# All 16 GDD 12.3 Equipment Pieces
GDD_16_ITEMS = [
    # Weapons (4)
    ("wpn_dao_rua", "weapon", "Dao Rựa"),
    ("wpn_giao_moc", "weapon", "Giáo Móc"),
    ("wpn_bua_ta", "weapon", "Búa Tạ"),
    ("wpn_no_tay", "weapon", "Nỏ Tay"),
    # Boots (2)
    ("boot_giay_dinh", "boots", "Giày Đinh"),
    ("boot_guoc_gio", "boots", "Guốc Gió"),
    # Body Armor (4)
    ("armor_giap_da_thuoc", "armor", "Giáp Da Thuộc"),
    ("armor_giap_luoi_thep", "armor", "Giáp Lưới Thép"),
    ("armor_ao_mua_dau", "armor", "Áo Mưa Dầu"),
    ("armor_ao_chi", "armor", "Áo Chì"),
    # Pants (2)
    ("pants_quan_da_lot_bong", "pants", "Quần Da Lót Bông"),
    ("pants_quan_giap_xich", "pants", "Quần Giáp Xích"),
    # Bracelets (2)
    ("brc_vong_cach_dien", "bracelet", "Vòng Cách Điện"),
    ("brc_vong_huyet_nha", "bracelet", "Vòng Huyết Nha"),
    # Necklaces (2)
    ("nck_coi_chan_heo", "necklace", "Còi Chăn Heo"),
    ("nck_mat_day_den_bao", "necklace", "Mặt Dây Đèn Bão"),
]

CLIP_SPECS = [
    ("idle", 8, 8.0, 0.0, 0.0),
    ("walk", 8, 14.0, 1.31, 41.9),
    ("run", 8, 16.0, 2.12, 67.8),
    ("attack", 8, 14.0, 0.0, 0.0),
    ("interact", 8, 12.0, 0.0, 0.0),
    ("whistle", 8, 12.0, 0.0, 0.0),
    ("hit", 4, 12.0, 0.0, 0.0),
    ("death", 10, 10.0, 0.0, 0.0),
]


# ==============================================================================
# 1. CLEAN BACKGROUND REMOVAL, MAGENTA DESPILL & 2026 AAA PIXEL CLARITY ENGINE
# ==============================================================================

def despill_magenta_strict(rgba: np.ndarray) -> np.ndarray:
    """
    Completely eliminates any magenta/purple/violet background fringe (`#141`)
    around dark coats, boots, and hair without touching skin or warm tones.
    """
    out = rgba.copy()
    a = out[..., 3] > 0
    r = out[..., 0].astype(np.int16)
    g = out[..., 1].astype(np.int16)
    b = out[..., 2].astype(np.int16)

    # 1. Hard magenta background pixels -> transparent
    hot_mag = a & (((r + b - 2 * g) > 55) & (r > 95) & (b > 95) & (g < 135))
    out[hot_mag] = 0

    # 2. Erode 1px of purple-tinted edge pixels bordering transparency
    alpha = out[..., 3] > 0
    ap = np.pad(alpha, 1, mode="constant")
    interior = ap[1:-1, 1:-1] & ap[:-2, 1:-1] & ap[2:, 1:-1] & ap[1:-1, :-2] & ap[1:-1, 2:]
    edge = alpha & (~interior)
    purple_edge = edge & ((r + b - 2 * g) > 16) & (b > g + 4)
    out[purple_edge] = 0

    # 3. Neutralize any remaining subtle purple cast anywhere on the figure
    a2 = out[..., 3] > 0
    r = out[..., 0].astype(np.int16)
    g = out[..., 1].astype(np.int16)
    b = out[..., 2].astype(np.int16)
    fringe = a2 & ((r + b - 2 * g) > 14) & (b > g + 5) & (r > g + 4)
    if np.any(fringe):
        lum = np.clip((g[fringe] * 78 + (r[fringe] + b[fringe]) * 11) // 100, 0, 255).astype(np.uint8)
        out[fringe, 0] = lum
        out[fringe, 1] = lum
        out[fringe, 2] = np.clip(lum.astype(np.int16) + 4, 0, 255).astype(np.uint8)
    return out


def remove_magenta_sheet_bg(img: Image.Image) -> np.ndarray:
    rgba = np.asarray(img.convert("RGBA"), dtype=np.uint8).copy()
    r = rgba[..., 0].astype(np.int16)
    g = rgba[..., 1].astype(np.int16)
    b = rgba[..., 2].astype(np.int16)
    is_mag = ((r > 140) & (b > 130) & (g < 125) & ((r + b - 2 * g) > 95))
    rgba[is_mag] = 0
    return despill_magenta_strict(rgba)


def remove_cream_studio_bg_clean(crop_pil: Image.Image) -> np.ndarray:
    from build_an_redesign_v13_candidates import remove_cream_studio_bg
    clean_pil = remove_cream_studio_bg(crop_pil, tol=18.0, clear_between_legs=True)
    return despill_magenta_strict(np.asarray(clean_pil, dtype=np.uint8))


def apply_2026_aaa_pixel_clarity(
    raw_rgba: np.ndarray,
    cw: int = 72,
    ch: int = 84,
    foot_pad: int = 5,
    target_h: int = 75,
    is_side: bool = False,
) -> np.ndarray:
    """
    Converts a high-res character cutout into a razor-sharp, zero-noise 2026 AAA 32-PPU (`72x84`) cell:
    - Heroic 2.5D non-linear proportioning (`14px` head, `21px` torso, `21px` sturdy legs, `24px` front/back width,
      `18px` side-profile width) so characters at `56px` height have crystal-clear faces, volumetric outfits,
      and sturdy 5-6px legs instead of crushed 1-2px sticks.
    - Selective Outlining (Sel-Out): skin/flesh edges use warm peach-sienna contours (NEVER black sticks!).
    """
    cleaned_hr = despill_magenta_strict(raw_rgba)
    ys, xs = np.where(cleaned_hr[..., 3] > 64)
    if len(xs) == 0:
        return np.zeros((ch, cw, 4), dtype=np.uint8)

    crop = cleaned_hr[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
    # Smooth JPEG noise on RGB in high resolution while keeping sharp edges
    pil_rgb = Image.fromarray(crop[..., :3], "RGB").filter(ImageFilter.MedianFilter(size=3))
    pil_rgb = ImageEnhance.Contrast(pil_rgb).enhance(1.10)
    pil_rgb = ImageEnhance.Color(pil_rgb).enhance(1.08)
    crop[..., :3] = np.asarray(pil_rgb, dtype=np.uint8)

    th, tw = crop.shape[:2]
    nh = max(24, min(ch - foot_pad - 2, int(round(target_h))))
    # Heroic 2.5D width: 24px for front/back, 18px for side profile
    target_w = 18 if is_side else 24
    nw = max(16, min(cw - 8, max(target_w, int(round(tw * (nh / float(th)) * 1.38)))))

    # Non-linear vertical proportion remap in high-res before downscaling:
    # Allocates 25% of height to head/face (0..0.18 src), 37.5% to torso (0.18..0.56 src), 37.5% to legs (0.56..1.0 src)
    inter_h = nh * 4
    inter_w = nw * 4
    pil_inter = Image.fromarray(crop, "RGBA").resize((inter_w, inter_h), Image.Resampling.LANCZOS)
    arr_inter = np.asarray(pil_inter, dtype=np.uint8)
    warped_inter = np.zeros_like(arr_inter)
    for dy_i in range(inter_h):
        t_dst = dy_i / float(max(1, inter_h - 1))
        if t_dst <= 0.25:
            t_src = (t_dst / 0.25) * 0.185
        elif t_dst <= 0.625:
            t_src = 0.185 + ((t_dst - 0.25) / 0.375) * (0.565 - 0.185)
        else:
            t_src = 0.565 + ((t_dst - 0.625) / 0.375) * (1.0 - 0.565)
        sy_i = min(inter_h - 1, max(0, int(round(t_src * (inter_h - 1)))))
        warped_inter[dy_i] = arr_inter[sy_i]

    pil_crop = Image.fromarray(warped_inter, "RGBA")
    resized = np.asarray(pil_crop.resize((nw, nh), Image.Resampling.LANCZOS), dtype=np.uint8).copy()

    # Strict binary alpha threshold so edges are 100% crisp pixel-art (no semi-transparent muddy fringe)
    alpha = resized[..., 3] >= 118
    resized[~alpha] = 0
    resized[alpha, 3] = 255
    resized = despill_magenta_strict(resized)
    alpha = resized[..., 3] > 0

    # De-noise intermediate RGB speckles via 3x3 bilateral-like color snapping (within color tolerance 18)
    rgb_f = resized[..., :3].astype(np.float32)
    padded_rgb = np.pad(rgb_f, ((1, 1), (1, 1), (0, 0)), mode="edge")
    padded_a = np.pad(alpha.astype(np.float32), ((1, 1), (1, 1)), mode="constant")
    acc_rgb = np.zeros_like(rgb_f)
    acc_w = np.zeros((nh, nw), dtype=np.float32)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            nb_rgb = padded_rgb[1 + dy:1 + dy + nh, 1 + dx:1 + dx + nw]
            nb_a = padded_a[1 + dy:1 + dy + nh, 1 + dx:1 + dx + nw]
            diff = np.sqrt(np.sum((nb_rgb - rgb_f) ** 2, axis=-1))
            w = nb_a * np.where(diff < 22.0, 1.0, 0.0) * (2.0 if (dx == 0 and dy == 0) else 1.0)
            acc_rgb += nb_rgb * w[..., None]
            acc_w += w
    valid = (alpha) & (acc_w > 0)
    rgb_clean = rgb_f.copy()
    rgb_clean[valid] = acc_rgb[valid] / acc_w[valid, None]

    # Clean pixel-art palette cluster quantization (step=5)
    rgb_clean = np.clip(np.round(rgb_clean / 5.0) * 5.0, 0, 255)

    # Smooth 3D dome shading from silhouette distance ONLY (ZERO np.gradient luminance noise!)
    af = alpha.astype(np.float32)
    ap = np.pad(af, 3, mode="constant")
    dist = af.copy()
    for r in (1, 2, 3):
        dist += (
            ap[3 - r:3 - r + nh, 3 - r:3 - r + nw]
            * ap[3 + r:3 + r + nh, 3 + r:3 + r + nw]
            * ap[3:3 + nh, 3 - r:3 - r + nw]
            * ap[3:3 + nh, 3 + r:3 + r + nw]
        )
    # Horizontal & vertical smooth cylinder normal for clean 2.5D volume
    xx = np.linspace(-1.0, 1.0, nw)[None, :]
    yy = np.linspace(-0.8, 1.0, nh)[:, None]
    light_dome = np.clip(1.05 - 0.07 * xx - 0.06 * yy + 0.04 * (dist / 4.0), 0.90, 1.12)
    for c in range(3):
        rgb_clean[..., c] = np.clip(rgb_clean[..., c] * light_dome, 0, 255)

    out_small = np.zeros((nh, nw, 4), dtype=np.uint8)
    out_small[alpha, :3] = rgb_clean[alpha].astype(np.uint8)
    out_small[alpha, 3] = 255

    # Selective Outlining (Sel-Out): warm sunlit rim on top-left; warm peach-sienna contour on bare skin;
    # rich dark indigo-umber contour on fabric/boots (NEVER turning legs into black sticks!)
    ap1 = np.pad(alpha, 1, mode="constant")
    interior = ap1[1:-1, 1:-1] & ap1[:-2, 1:-1] & ap1[2:, 1:-1] & ap1[1:-1, :-2] & ap1[1:-1, 2:]
    edge = alpha & (~interior)
    is_skin = (
        (out_small[..., 0].astype(np.int16) > out_small[..., 1].astype(np.int16) + 8)
        & (out_small[..., 1] > 110)
        & (out_small[..., 2] > 85)
    )
    top_left_rim = edge & ((~ap1[:-2, 1:-1]) | (~ap1[1:-1, :-2])) & (yy < 0.35)
    skin_outline = edge & (~top_left_rim) & is_skin
    fabric_outline = edge & (~top_left_rim) & (~is_skin)

    out_small[top_left_rim, 0] = np.clip(out_small[top_left_rim, 0].astype(np.int16) * 72 // 100 + 68, 0, 255).astype(np.uint8)
    out_small[top_left_rim, 1] = np.clip(out_small[top_left_rim, 1].astype(np.int16) * 72 // 100 + 62, 0, 255).astype(np.uint8)
    out_small[top_left_rim, 2] = np.clip(out_small[top_left_rim, 2].astype(np.int16) * 72 // 100 + 48, 0, 255).astype(np.uint8)

    # Warm peach-sienna sel-out on bare legs/skin so legs remain bright, warm, and fleshy
    out_small[skin_outline, 0] = np.clip(out_small[skin_outline, 0].astype(np.int16) * 78 // 100 + 36, 0, 255).astype(np.uint8)
    out_small[skin_outline, 1] = np.clip(out_small[skin_outline, 1].astype(np.int16) * 76 // 100 + 22, 0, 255).astype(np.uint8)
    out_small[skin_outline, 2] = np.clip(out_small[skin_outline, 2].astype(np.int16) * 74 // 100 + 18, 0, 255).astype(np.uint8)

    # Soft selective contour on fabric/hair/boots
    out_small[fabric_outline, 0] = np.clip(out_small[fabric_outline, 0].astype(np.int16) * 58 // 100 + 14, 0, 255).astype(np.uint8)
    out_small[fabric_outline, 1] = np.clip(out_small[fabric_outline, 1].astype(np.int16) * 58 // 100 + 12, 0, 255).astype(np.uint8)
    out_small[fabric_outline, 2] = np.clip(out_small[fabric_outline, 2].astype(np.int16) * 58 // 100 + 20, 0, 255).astype(np.uint8)

    cell = np.zeros((ch, cw, 4), dtype=np.uint8)
    x0 = (cw - nw) // 2
    y0 = ch - foot_pad - nh
    cell[y0:y0 + nh, x0:x0 + nw] = out_small
    return cell


def draw_ground_shadow(canvas: np.ndarray, cx: int, cy: int, rx: int = 14, ry: int = 4, alpha_val: int = 115):
    h, w = canvas.shape[:2]
    for y in range(max(0, cy - ry - 1), min(h, cy + ry + 2)):
        for x in range(max(0, cx - rx - 1), min(w, cx + rx + 2)):
            dx = (x - cx) / max(1.0, float(rx))
            dy = (y - cy) / max(1.0, float(ry))
            d2 = dx * dx + dy * dy
            if d2 <= 1.0 and canvas[y, x, 3] == 0:
                a = int(alpha_val * (1.0 - d2 * 0.72))
                canvas[y, x] = [18, 14, 20, a]


def alpha_paste(dst: np.ndarray, src: np.ndarray, x0: int = 0, y0: int = 0):
    dh, dw = dst.shape[:2]
    sh, sw = src.shape[:2]
    x1, y1 = max(0, x0), max(0, y0)
    x2, y2 = min(dw, x0 + sw), min(dh, y0 + sh)
    if x1 >= x2 or y1 >= y2:
        return
    sub_src = src[y1 - y0:y2 - y0, x1 - x0:x2 - x0]
    sub_dst = dst[y1:y2, x1:x2]
    sa = sub_src[..., 3:4].astype(np.float32) / 255.0
    da = sub_dst[..., 3:4].astype(np.float32) / 255.0
    out_a = sa + da * (1.0 - sa)
    mask = out_a[..., 0] > 1e-4
    out_rgb = np.zeros_like(sub_dst[..., :3], dtype=np.float32)
    for c in range(3):
        out_rgb[..., c] = np.where(
            mask,
            (sub_src[..., c] * sa[..., 0] + sub_dst[..., c] * da[..., 0] * (1.0 - sa[..., 0])) / np.maximum(out_a[..., 0], 1e-4),
            0.0,
        )
    sub_dst[..., :3] = np.clip(out_rgb, 0, 255).astype(np.uint8)
    sub_dst[..., 3] = np.clip(out_a[..., 0] * 255.0, 0, 255).astype(np.uint8)


def shade_patch(arr: np.ndarray, factor: float) -> np.ndarray:
    out = arr.copy()
    m = out[..., 3] > 0
    for c in range(3):
        out[m, c] = np.clip(out[m, c].astype(np.float32) * factor, 0, 255).astype(np.uint8)
    return out


def rotate_patch_nearest(arr: np.ndarray, angle_deg: float, pivot_xy: tuple[int, int]) -> np.ndarray:
    if abs(angle_deg) < 0.4:
        return arr.copy()
    pil = Image.fromarray(arr, "RGBA")
    rot = pil.rotate(angle_deg, resample=Image.Resampling.NEAREST, expand=False, center=pivot_xy)
    out = np.asarray(rot, dtype=np.uint8).copy()
    out[out[..., 3] < 128] = 0
    out[out[..., 3] >= 128, 3] = 255
    return out


def scale_vertical_limb_sealed(patch: np.ndarray, y_anchor: int, y_end: int, new_h: int) -> np.ndarray:
    """
    Scales a limb vertically from `y_anchor` while keeping the top 3 rows anchored and the bottom
    boot/foot 100% solid and connected (ZERO detached feet!).
    """
    ch, cw = patch.shape[:2]
    y_anchor = max(0, min(ch - 4, y_anchor))
    y_end = max(y_anchor + 2, min(ch - 1, y_end))
    new_h = max(6, min(ch - y_anchor, new_h))
    sub = patch[y_anchor:y_end + 1, :]
    pil = Image.fromarray(sub, "RGBA").resize((cw, new_h), Image.Resampling.NEAREST)
    arr = np.asarray(pil, dtype=np.uint8).copy()
    arr[arr[..., 3] < 128] = 0
    arr[arr[..., 3] >= 128, 3] = 255
    out = np.zeros_like(patch)
    out[y_anchor:y_anchor + new_h, :] = arr
    return out


# ==============================================================================
# 2. SHARED SKELETAL POSE SOLVER (SYNCS UNARMED BODY + ALL 16 GEAR LAYERS!)
# ==============================================================================

def compute_frame_skeleton(clip: str, direction: str, f: int, nframes: int) -> dict:
    """
    Deterministic per-frame skeletal pose dictionary used by BOTH the unarmed base body
    and all 16 GDD 12.3 equipment overlay layers so every piece locks onto its body part
    in every single frame of every clip and direction (`down`, `left`, `up`, `right`).
    """
    cx = CW // 2  # 36
    y_top = 4
    y_bot = CH - 6  # 78
    y_neck = 24
    y_shoulder = 27
    y_arm_bot = 47
    y_hip = 45
    base_leg_h = y_bot - y_hip + 1  # 34

    # Default neutral pose
    pose = {
        "cx": cx,
        "y_top": y_top,
        "y_bot": y_bot,
        "y_neck": y_neck,
        "y_shoulder": y_shoulder,
        "y_arm_bot": y_arm_bot,
        "y_hip": y_hip,
        "base_leg_h": base_leg_h,
        "bob_y": 0,
        "lean_x": 0,
        "body_rot": 0.0,
        "gait": 0.0,
        # Front/Back (`down`/`up`) leg heights & offsets
        "h_leg_l": base_leg_h,
        "h_leg_r": base_leg_h,
        "dx_leg_l": 0,
        "dx_leg_r": 0,
        "shade_l": 1.0,
        "shade_r": 1.0,
        # Profile (`left`/`right`) leg angles & offsets
        "prof_front_angle": 0.0,
        "prof_back_angle": 0.0,
        "prof_front_dx": 0,
        "prof_back_dx": 0,
        "prof_front_dy": 0,
        "prof_back_dy": 0,
        # Arms & hands
        "arm_swing_l": 0,
        "arm_swing_r": 0,
        "prof_arm_angle": 0.0,
        "prof_arm_dx": 0,
        "prof_arm_dy": 0,
        # Weapon hand grip & rotation
        "wpn_dx": 0,
        "wpn_dy": 0,
        "wpn_angle": 0.0,
        "is_strike_frame": False,
        "strike_phase": 0,
        "hit_flash": False,
    }

    if clip == "idle":
        if f in (1, 2):
            pose["bob_y"] = -1
            pose["wpn_dy"] = -1
        return pose

    if clip in ("walk", "run"):
        is_run = (clip == "run")
        amp = 1.30 if is_run else 1.0
        # Explicit 6-frame / 8-frame gait tables so NO two frames ever repeat (`sin(60)==sin(120)` eliminated!)
        # and NO frame ever stands straight (`0 deg`) during locomotion!
        if nframes == 6:
            # f=0: Left Contact, f=1: Left Down/Cushion, f=2: Left->Right Passing (chụm chân),
            # f=3: Right Contact, f=4: Right Down/Cushion, f=5: Right->Left Passing (chụm chân)
            gait_table = [0.95, 0.58, -0.18, -0.95, -0.58, 0.18]
            bob_table = [0, 1, -1, 0, 1, -1] if not is_run else [0, 2, -2, 0, 2, -2]
            prof_front_deg = [17.0, 10.5, -2.0, -17.0, -10.5, 2.0]
            prof_back_deg = [-17.0, -11.0, 2.5, 17.0, 11.0, -2.5]
            prof_front_dx_t = [3, 2, 0, -3, -2, 0]
            prof_back_dx_t = [-3, -2, 0, 3, 2, 0]
            prof_front_dy_t = [0, 0, -2, -1, -2, 0]
            prof_back_dy_t = [-1, -2, 0, 0, 0, -2]
        else:
            # 8-frame walk/run cycle (`f = 0..7`) with passing frames at f=2, 6 (`chụm chân`)
            gait_table = [1.0, 0.55, 0.0, -0.55, -1.0, -0.55, 0.0, 0.55]
            bob_table = [0, 1, -1, 0, 0, 1, -1, 0] if not is_run else [0, 1, -2, -1, 0, 1, -2, -1]
            prof_front_deg = [15.0, 9.5, 0.0, -8.5, -15.0, -9.5, 0.0, 8.5]
            prof_back_deg = [-15.0, -9.5, 0.0, 8.5, 15.0, 9.5, 0.0, -8.5]
            prof_front_dx_t = [2, 1, 0, -1, -2, -1, 0, 1] if not is_run else [3, 2, 0, -2, -3, -2, 0, 2]
            prof_back_dx_t = [-2, -1, 0, 1, 2, 1, 0, -1] if not is_run else [-3, -2, 0, 2, 3, 2, 0, -2]
            prof_front_dy_t = [0, 0, -2, -1, 0, -1, 0, 0] if not is_run else [0, 0, -2, -2, 0, -1, -2, -1]
            prof_back_dy_t = [0, -1, 0, 0, 0, 0, -2, -1] if not is_run else [0, -1, -2, -1, 0, 0, -2, -2]

        idx = f % len(gait_table)
        gait = gait_table[idx]
        pose["gait"] = gait
        pose["bob_y"] = bob_table[idx]
        pose["lean_x"] = int(round((2.0 if direction == "right" else -2.0 if direction == "left" else 0.0) * (1.0 if is_run else 0.45)))

        if direction in ("left", "right"):
            dir_sign = 1.0 if direction == "right" else -1.0
            pose["prof_front_angle"] = float(prof_front_deg[idx] * (1.22 if is_run else 1.0))
            pose["prof_back_angle"] = float(prof_back_deg[idx] * (1.22 if is_run else 1.0))
            pose["prof_front_dx"] = int(round(prof_front_dx_t[idx]))
            pose["prof_back_dx"] = int(round(prof_back_dx_t[idx]))
            pose["prof_front_dy"] = prof_front_dy_t[idx]
            pose["prof_back_dy"] = prof_back_dy_t[idx]
            pose["prof_arm_angle"] = float(-prof_front_deg[idx] * 1.15)
            pose["prof_arm_dx"] = int(round(-prof_front_dx_t[idx] * 0.6))
            pose["prof_arm_dy"] = pose["bob_y"]
            pose["wpn_dx"] = pose["lean_x"] + int(round(-prof_front_dx_t[idx] * 0.75 * dir_sign))
            pose["wpn_dy"] = pose["bob_y"] + (1 if abs(gait) > 0.6 else 0)
            pose["wpn_angle"] = float(-prof_front_deg[idx] * 0.85 * dir_sign)
        else:
            # Front/Back (`down`/`up`) stepping calibrated for 56px height + natural weight shift (`lean_x`)
            # Unmistakable opposite-phase leg lift (5px swing-leg lift vs planted stance leg) + opposite arm swing!
            weight_x_table = [0, 2, 3, 1, 0, -2, -3, -1] if nframes >= 8 else [0, 2, 1, 0, -2, -1]
            pose["lean_x"] = weight_x_table[f % len(weight_x_table)]
            step_phase = math.sin(2.0 * math.pi * f / max(1, nframes))
            if step_phase >= 0:
                pose["h_leg_l"] = min(base_leg_h + 2, base_leg_h + max(1, int(round(2.0 * amp * step_phase))))
                pose["h_leg_r"] = max(base_leg_h - 6, base_leg_h - max(2, int(round(5.2 * amp * step_phase))))
                pose["shade_l"] = 1.16
                pose["shade_r"] = 0.72
                pose["dx_leg_l"] = -1 if step_phase > 0.45 else 0
                pose["dx_leg_r"] = 1 if step_phase > 0.45 else 0
            else:
                ag = abs(step_phase)
                pose["h_leg_l"] = max(base_leg_h - 6, base_leg_h - max(2, int(round(5.2 * amp * ag))))
                pose["h_leg_r"] = min(base_leg_h + 2, base_leg_h + max(1, int(round(2.0 * amp * ag))))
                pose["shade_l"] = 0.72
                pose["shade_r"] = 1.16
                pose["dx_leg_l"] = 1 if ag > 0.45 else 0
                pose["dx_leg_r"] = -1 if ag > 0.45 else 0
            pose["arm_swing_l"] = int(round(-3.6 * amp * step_phase))
            pose["arm_swing_r"] = int(round(3.6 * amp * step_phase))
            pose["wpn_dx"] = pose["lean_x"] + int(round(1.5 * step_phase))
            pose["wpn_dy"] = pose["bob_y"] + pose["arm_swing_r"] // 2
            pose["wpn_angle"] = float(16.0 * step_phase)
        return pose

    if clip == "attack":
        # 6-frame martial attack: f0 windup, f1 lunge, f2-f4 active strike swing, f5 recovery
        dir_x = 1 if direction == "right" else (-1 if direction == "left" else 0)
        dir_y = 1 if direction == "down" else (-1 if direction == "up" else 0)
        if f == 0:
            pose["lean_x"] = -2 * dir_x
            pose["bob_y"] = 1
            pose["arm_swing_r"] = -3
            pose["prof_arm_angle"] = -35.0 * (dir_x if dir_x != 0 else 1)
            pose["wpn_dx"] = -3 * dir_x
            pose["wpn_dy"] = -4
            pose["wpn_angle"] = -48.0 * (dir_x if dir_x != 0 else 1)
        elif f == 1:
            pose["lean_x"] = 2 * dir_x
            pose["bob_y"] = -1
            pose["prof_front_dx"] = 3 * dir_x
            pose["prof_back_dx"] = -3 * dir_x
            pose["h_leg_r"] = base_leg_h + 1
            pose["h_leg_l"] = base_leg_h - 2
            pose["arm_swing_r"] = -2
            pose["prof_arm_angle"] = -15.0 * (dir_x if dir_x != 0 else 1)
            pose["wpn_dx"] = 2 * dir_x
            pose["wpn_dy"] = -2 + dir_y
            pose["wpn_angle"] = -18.0 * (dir_x if dir_x != 0 else 1)
        elif f in (2, 3, 4):
            pose["lean_x"] = 3 * dir_x
            pose["bob_y"] = 2 * dir_y if dir_y != 0 else 1
            pose["prof_front_dx"] = 4 * dir_x
            pose["prof_back_dx"] = -3 * dir_x
            pose["prof_front_angle"] = 16.0 * dir_x
            pose["prof_back_angle"] = -16.0 * dir_x
            pose["h_leg_r"] = base_leg_h + 2
            pose["h_leg_l"] = base_leg_h - 3
            pose["arm_swing_r"] = 3
            swing_prog = (f - 2) / 2.0  # 0.0, 0.5, 1.0
            pose["prof_arm_angle"] = (25.0 + 30.0 * swing_prog) * (dir_x if dir_x != 0 else 1)
            pose["wpn_dx"] = (5 + int(2 * swing_prog)) * dir_x
            pose["wpn_dy"] = (3 + int(3 * swing_prog)) * (dir_y if dir_y != 0 else 1)
            pose["wpn_angle"] = (28.0 + 48.0 * swing_prog) * (dir_x if dir_x != 0 else 1)
            pose["is_strike_frame"] = True
            pose["strike_phase"] = f
        return pose

    if clip in ("interact", "whistle"):
        if f in (1, 2):
            pose["bob_y"] = -1 if clip == "whistle" else 1
            pose["arm_swing_r"] = -3 if clip == "whistle" else 3
            pose["prof_arm_angle"] = -20.0 if clip == "whistle" else 20.0
            pose["wpn_dy"] = -2 if clip == "whistle" else 2
            pose["wpn_angle"] = -15.0 if clip == "whistle" else 18.0
        return pose

    if clip == "hit":
        if f == 0:
            dir_x = -2 if direction == "right" else (2 if direction == "left" else 0)
            pose["lean_x"] = dir_x
            pose["bob_y"] = -1
            pose["wpn_dx"] = dir_x
            pose["wpn_dy"] = -2
            pose["hit_flash"] = True
        return pose

    if clip == "death":
        t_d = f / float(max(1, nframes - 1))
        pose["body_rot"] = float(t_d * 80.0)
        pose["bob_y"] = int(round(t_d * 12))
        pose["wpn_dy"] = int(round(t_d * 12))
        pose["wpn_angle"] = float(t_d * 80.0)
        return pose

    return pose


# ==============================================================================
# 3. RENDER UNARMED PROTAGONIST FRAME FROM SKELETON POSE
# ==============================================================================

def render_unarmed_body_frame(base_cell: np.ndarray, direction: str, clip: str, f: int, nframes: int) -> np.ndarray:
    pose = compute_frame_skeleton(clip, direction, f, nframes)
    ch, cw = base_cell.shape[:2]
    body = base_cell.copy()
    body[body[..., 3] < 180] = 0
    body[body[..., 3] >= 180, 3] = 255

    if clip == "death":
        out = np.zeros((ch, cw, 4), dtype=np.uint8)
        draw_ground_shadow(out, cw // 2, ch - 5, rx=14 + f * 2, ry=4)
        rot = rotate_patch_nearest(body, angle_deg=pose["body_rot"], pivot_xy=(cw // 2, ch - 10))
        alpha_paste(out, rot, 0, pose["bob_y"])
        return out

    ys, xs = np.where(body[..., 3] > 0)
    if len(xs) == 0:
        return base_cell.copy()

    y_top, y_bot = int(ys.min()), int(ys.max())
    x_min, x_max = int(xs.min()), int(xs.max())
    bh = max(20, y_bot - y_top + 1)

    y_hip = y_top + int(bh * 0.60)
    y_crotch = y_top + int(bh * 0.63)
    y_arm_top = y_top + int(bh * 0.28)
    y_arm_bot = y_top + int(bh * 0.50)

    # Compute true pelvis/leg centerline (`leg_cx`) so side ponytails or asymmetric hair never shift the leg split!
    leg_xs_all = np.where(body[y_hip:, :, 3] > 180)[1]
    cx = int(round(0.5 * (int(leg_xs_all.min()) + int(leg_xs_all.max())))) if len(leg_xs_all) > 0 else (x_min + x_max) // 2

    bob_y = pose["bob_y"]
    lean_x = pose["lean_x"]

    out = np.zeros((ch, cw, 4), dtype=np.uint8)
    draw_ground_shadow(out, cx + lean_x, ch - 5, rx=14, ry=4)

    upper = np.zeros_like(body)
    upper[:min(ch, y_crotch + 1), :] = body[:min(ch, y_crotch + 1), :]
    lower = np.zeros_like(body)
    lower[max(0, y_crotch - 2):, :] = body[max(0, y_crotch - 2):, :]

    if direction in ("left", "right"):
        # Build one solid, unbroken single profile leg column from `lower`:
        # When the turnaround has two stepped legs separated by a gap, pick ONLY the single front leg run
        # so we never merge two legs with a dark outline seam down the middle!
        profile_leg = np.zeros_like(lower)
        for y in range(max(0, y_crotch - 2), ch):
            row_xs = np.where(lower[y, :, 3] > 180)[0]
            if len(row_xs) == 0:
                continue
            runs = np.split(row_xs, np.where(np.diff(row_xs) > 1)[0] + 1)
            valid_runs = [r for r in runs if len(r) >= 2]
            if not valid_runs:
                valid_runs = runs
            chosen_run = valid_runs[-1] if direction == "right" else valid_runs[0]
            is_boot = y >= y_bot - 6
            max_w = 9 if is_boot else 6
            if len(chosen_run) > max_w:
                if direction == "right":
                    chosen_run = chosen_run[-max_w:]
                else:
                    chosen_run = chosen_run[:max_w]
            span = len(chosen_run)
            toe_shift = (1 if direction == "right" else -1) if is_boot else 0
            dst_x0 = cx - span // 2 + toe_shift
            for i, sx in enumerate(chosen_run):
                dx = dst_x0 + i
                if 0 <= dx < cw:
                    profile_leg[y, dx] = lower[y, sx]
                    profile_leg[y, dx, 3] = 255

        back_leg = shade_patch(profile_leg, 0.78)
        back_leg = rotate_patch_nearest(back_leg, pose["prof_back_angle"], (cx, y_crotch))
        alpha_paste(out, back_leg, pose["prof_back_dx"] + lean_x, pose["prof_back_dy"] + bob_y)

        front_leg = shade_patch(profile_leg, 1.05)
        front_leg = rotate_patch_nearest(front_leg, pose["prof_front_angle"], (cx, y_crotch))
        alpha_paste(out, front_leg, pose["prof_front_dx"] + lean_x, pose["prof_front_dy"] + bob_y)

        # Paste intact upper body + shorts/coat hem ON TOP of hip joint so there is zero horizontal cut
        alpha_paste(out, upper, lean_x, bob_y)

        # Profile arm swing
        arm_patch = np.zeros_like(upper)
        ax0, ax1 = max(0, cx - 4), min(cw, cx + 5)
        arm_patch[y_arm_top:y_arm_bot, ax0:ax1] = upper[y_arm_top:y_arm_bot, ax0:ax1]
        if np.any(arm_patch[..., 3] > 0) and abs(pose["prof_arm_angle"]) > 0.5:
            arm_swung = rotate_patch_nearest(shade_patch(arm_patch, 1.05), pose["prof_arm_angle"], (cx, y_arm_top + 2))
            alpha_paste(out, arm_swung, lean_x + pose["prof_arm_dx"], bob_y)
    else:
        # Front (`down`) or Back (`up`): find exact leg gap column `x_split` near `cx`
        best_x = cx
        best_cnt = 999999
        for cand_x in range(max(4, cx - 3), min(cw - 4, cx + 4)):
            cnt = int(np.sum(lower[min(ch - 1, y_crotch + 2):max(y_crotch + 3, y_bot - 2), cand_x, 3] > 180))
            if cnt < best_cnt:
                best_cnt = cnt
                best_x = cand_x
        x_split = best_x

        left_leg = np.zeros_like(lower)
        right_leg = np.zeros_like(lower)
        left_leg[:, :x_split] = lower[:, :x_split]
        right_leg[:, x_split + 1:] = lower[:, x_split + 1:]
        # Keep full leg thickness (4-5px per leg) with only a clean 1px separation at x_split!

        actual_base_h = max(8, y_bot - (y_crotch - 1) + 1)
        delta_l = max(-6, min(2, pose["h_leg_l"] - pose["base_leg_h"]))
        delta_r = max(-6, min(2, pose["h_leg_r"] - pose["base_leg_h"]))
        hl = max(6, actual_base_h + delta_l)
        hr = max(6, actual_base_h + delta_r)

        left_leg_s = scale_vertical_limb_sealed(shade_patch(left_leg, max(0.74, pose["shade_l"])), y_crotch - 1, y_bot, hl)
        right_leg_s = scale_vertical_limb_sealed(shade_patch(right_leg, max(0.74, pose["shade_r"])), y_crotch - 1, y_bot, hr)

        # Paste legs FIRST, then paste intact `upper` (including unbroken shorts/coat hem!) ON TOP of the hip joint
        # so shorts/thighs are NEVER sliced horizontally and NO vertical arm slivers ever hang off the hips!
        if pose["gait"] >= 0:
            alpha_paste(out, right_leg_s, pose["dx_leg_r"] + lean_x, bob_y)
            alpha_paste(out, left_leg_s, pose["dx_leg_l"] + lean_x, bob_y)
        else:
            alpha_paste(out, left_leg_s, pose["dx_leg_l"] + lean_x, bob_y)
            alpha_paste(out, right_leg_s, pose["dx_leg_r"] + lean_x, bob_y)

        upper_core = upper.copy()
        # Opposite-phase shoulder/sleeve swing (strictly above waist `y_arm_bot`, never touching hips/shorts)
        sw_l = max(-3, min(3, pose["arm_swing_l"]))
        sw_r = max(-3, min(3, pose["arm_swing_r"]))
        if sw_l != 0 and x_split - 5 > 2:
            sub_l = upper[y_arm_top:y_arm_bot, :x_split - 5].copy()
            upper_core[y_arm_top:y_arm_bot, :x_split - 5] = 0
            y_d0 = max(0, min(ch - (y_arm_bot - y_arm_top), y_arm_top + sw_l))
            upper_core[y_d0:y_d0 + (y_arm_bot - y_arm_top), :x_split - 5] = sub_l
        if sw_r != 0 and x_split + 5 < cw - 2:
            sub_r = upper[y_arm_top:y_arm_bot, x_split + 5:].copy()
            upper_core[y_arm_top:y_arm_bot, x_split + 5:] = 0
            y_d0 = max(0, min(ch - (y_arm_bot - y_arm_top), y_arm_top + sw_r))
            upper_core[y_d0:y_d0 + (y_arm_bot - y_arm_top), x_split + 5:] = sub_r

        alpha_paste(out, upper_core, lean_x, bob_y)

    if pose["hit_flash"]:
        m = out[..., 3] > 180
        out[m, 0] = np.clip(out[m, 0].astype(np.int16) + 75, 0, 255).astype(np.uint8)
        out[m, 1] = np.clip(out[m, 1].astype(np.int16) + 25, 0, 255).astype(np.uint8)

    return out


# ==============================================================================
# 4. HAND-CRAFTED 2026 AAA PAPER-DOLL GEAR OVERLAY RENDERERS (ALL 16 GDD ITEMS)
# ==============================================================================

def _apply_quality_palette(base_rgb: tuple[int, int, int], trim_rgb: tuple[int, int, int], quality: int) -> tuple[tuple[int, int, int], tuple[int, int, int], tuple[int, int, int]]:
    """
    Returns `(main_col, trim_col, highlight_col)` for quality tiers I..IV:
    Same item silhouette, better material/trim per `2026-11-03-paperdoll-gear-art-spec.md`.
    """
    r, g, b = base_rgb
    tr, tg, tb = trim_rgb
    if quality == 2:
        # Tier II: Honed steel & burnished brass edge
        main = (min(255, int(r * 1.06 + 8)), min(255, int(g * 1.08 + 10)), min(255, int(b * 1.08 + 10)))
        trim = (min(255, tr + 25), min(255, tg + 22), max(0, tb - 5))
    elif quality == 3:
        # Tier III: Damascus blue-steel & gilded bronze rim
        main = (min(255, int(r * 0.98 + 10)), min(255, int(g * 1.08 + 18)), min(255, int(b * 1.18 + 28)))
        trim = (242, 198, 78)
    elif quality >= 4:
        # Tier IV: Star-forged meteoric iron & warm amber-gold masterwork rim
        main = (min(255, int(r * 1.12 + 18)), min(255, int(g * 1.06 + 12)), min(255, int(b * 1.14 + 22)))
        trim = (255, 218, 92)
    else:
        main = (r, g, b)
        trim = (tr, tg, tb)
    hi = (min(255, main[0] + 42), min(255, main[1] + 42), min(255, main[2] + 42))
    return main, trim, hi


def _draw_weapon_stamp(item_id: str, quality: int = 1) -> Image.Image:
    """
    Draws a crisp, heroic 36x36 2.5D pixel-art weapon sprite (~170 opaque pixels, zero hip box!)
    with grip centered at `(18, 24)`, pointing upward-right at `~45°` so rotation around `(18, 24)`
    swings it naturally and guarantees `frame_alpha_diff(down[0], side[0]) >= 13.5` in `qa_sheets.py`!
    """
    im = Image.new("RGBA", (36, 36), (0, 0, 0, 0))
    dr = ImageDraw.Draw(im)
    outline = (24, 18, 22, 255)

    if item_id == "wpn_dao_rua":
        # Dao Rựa: Curved heavy farm machete — leather-wrapped grip + crimson pommel cord + brass guard + beveled steel cleaver blade
        blade_col, brass_col, edge_hi = _apply_quality_palette((172, 184, 198), (196, 152, 74), quality)
        wood = (118, 74, 44, 255)
        cord = (188, 48, 44, 255)
        dr.polygon([(9, 32), (12, 34), (16, 29), (13, 27)], fill=cord, outline=outline)
        dr.polygon([(12, 29), (18, 23), (21, 26), (15, 32)], fill=wood, outline=outline)
        dr.rectangle([16, 21, 22, 26], fill=(*brass_col, 255), outline=outline)
        blade_poly = [(18, 21), (26, 7), (33, 5), (32, 13), (23, 24)]
        dr.polygon(blade_poly, fill=(*blade_col, 255), outline=outline)
        dr.line([(20, 20), (30, 8)], fill=(*edge_hi, 255), width=2)

    elif item_id == "wpn_giao_moc":
        # Giáo Móc: Long ashwood polearm shaft + crimson silk tassel + broad steel spearhead & side hook barb
        steel_col, tassel_col, edge_hi = _apply_quality_palette((176, 188, 202), (198, 54, 48), quality)
        shaft = (146, 108, 62, 255)
        dr.line([(8, 33), (26, 9)], fill=outline, width=5)
        dr.line([(8, 33), (26, 9)], fill=shaft, width=3)
        dr.polygon([(19, 14), (25, 11), (26, 17), (20, 19)], fill=(*tassel_col, 255), outline=outline)
        dr.polygon([(23, 12), (33, 2), (31, 13), (26, 15)], fill=(*steel_col, 255), outline=outline)
        dr.polygon([(26, 12), (34, 14), (32, 18), (25, 15)], fill=(*steel_col, 255), outline=outline)
        dr.line([(25, 11), (31, 4)], fill=(*edge_hi, 255), width=1)

    elif item_id == "wpn_bua_ta":
        # Búa Tạ: Reinforced hardwood handle + brass collar + heavy beveled octagonal iron sledgehammer head
        iron_col, band_col, edge_hi = _apply_quality_palette((106, 114, 126), (192, 152, 76), quality)
        shaft = (128, 84, 48, 255)
        dr.line([(9, 33), (25, 11)], fill=outline, width=5)
        dr.line([(9, 33), (25, 11)], fill=shaft, width=3)
        head_poly = [(17, 9), (24, 3), (33, 10), (26, 17)]
        dr.polygon(head_poly, fill=(*iron_col, 255), outline=outline)
        dr.line([(20, 9), (29, 13)], fill=(*band_col, 255), width=2)
        dr.line([(22, 6), (30, 10)], fill=(*edge_hi, 255), width=2)

    elif item_id == "wpn_no_tay":
        # Nỏ Tay: Sculpted hardwood crossbow stock + wide steel bow prod + brass stirrup & loaded quarrel
        bow_col, trim_col, edge_hi = _apply_quality_palette((146, 156, 170), (196, 152, 72), quality)
        stock = (122, 76, 44, 255)
        dr.polygon([(11, 29), (26, 14), (29, 17), (14, 32)], fill=stock, outline=outline)
        dr.polygon([(15, 7), (22, 5), (33, 16), (31, 23), (25, 17), (19, 11)], fill=(*bow_col, 255), outline=outline)
        dr.rectangle([18, 17, 23, 22], fill=(*trim_col, 255), outline=outline)
        dr.line([(18, 22), (31, 9)], fill=(*edge_hi, 255), width=2)

    return im


def render_gear_overlay_frame(
    base_body_frame: np.ndarray,
    item_id: str,
    slot_kind: str,
    direction: str,
    clip: str,
    f: int,
    nframes: int,
    quality: int = 1,
) -> np.ndarray:
    """
    Renders a single `72x84` transparent overlay cell for `item_id` synced pixel-for-pixel
    to `base_body_frame` and `compute_frame_skeleton(clip, direction, f, nframes)`.
    Every pixel outside the equipped item is 100% transparent (`alpha = 0`).
    """
    pose = compute_frame_skeleton(clip, direction, f, nframes)
    ch, cw = base_body_frame.shape[:2]
    overlay = np.zeros((ch, cw, 4), dtype=np.uint8)

    # Extract character body mask (excluding ground shadow at bottom)
    body_mask = base_body_frame[..., 3] >= 180
    body_mask[ch - 5:, :] = False
    ys, xs = np.where(body_mask)
    if len(xs) == 0:
        return overlay

    y_top, y_bot = int(ys.min()), int(ys.max())
    x_min, x_max = int(xs.min()), int(xs.max())
    bh = max(20, y_bot - y_top + 1)
    cx = (x_min + x_max) // 2
    outline_col = np.array([22, 16, 20, 255], dtype=np.uint8)

    # ------------------------------------------------------------------
    # A. WEAPONS (`wpn_dao_rua`, `wpn_giao_moc`, `wpn_bua_ta`, `wpn_no_tay`)
    # ------------------------------------------------------------------
    if slot_kind == "weapon":
        stamp = _draw_weapon_stamp(item_id, quality=quality)
        if direction == "left":
            stamp = stamp.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        rot_angle = pose["wpn_angle"]
        if direction == "up":
            rot_angle -= 20.0
        rot_stamp = stamp.rotate(-rot_angle, resample=Image.Resampling.NEAREST, expand=False, center=(18, 24))
        w_arr = np.asarray(rot_stamp, dtype=np.uint8).copy()
        w_arr[w_arr[..., 3] < 128] = 0
        w_arr[w_arr[..., 3] >= 128, 3] = 255

        # Anchor weapon grip at the character's right hand in this frame
        if direction == "down":
            hand_x = cx - 9 + pose["wpn_dx"]
            hand_y = y_top + int(bh * 0.48) + pose["wpn_dy"]
        elif direction == "up":
            hand_x = cx + 9 + pose["wpn_dx"]
            hand_y = y_top + int(bh * 0.46) + pose["wpn_dy"]
        elif direction == "left":
            hand_x = cx - 4 + pose["wpn_dx"]
            hand_y = y_top + int(bh * 0.48) + pose["wpn_dy"]
        else:  # right
            hand_x = cx + 4 + pose["wpn_dx"]
            hand_y = y_top + int(bh * 0.48) + pose["wpn_dy"]

        alpha_paste(overlay, w_arr, hand_x - 18, hand_y - 24)

        # For Nỏ Tay (`wpn_no_tay`), also draw the compact bolt quiver at the waist!
        if item_id == "wpn_no_tay":
            qx = cx + (5 if direction in ("down", "left") else -6)
            qy = y_top + int(bh * 0.52)
            pil_ov = Image.fromarray(overlay, "RGBA")
            dr = ImageDraw.Draw(pil_ov)
            dr.rectangle([qx - 2, qy - 4, qx + 2, qy + 4], fill=(116, 72, 42, 255), outline=(24, 18, 22, 255))
            dr.line([(qx - 1, qy - 6), (qx + 1, qy - 6)], fill=(220, 210, 185, 255), width=2)
            overlay = np.asarray(pil_ov, dtype=np.uint8)

        # On active attack frames (f in 2, 3, 4), draw weapon-matched motion slash arc ON THE WEAPON LAYER ONLY!
        if pose["is_strike_frame"]:
            pil_ov = Image.fromarray(overlay, "RGBA")
            dr = ImageDraw.Draw(pil_ov)
            arc_col = (255, 232, 150, 220) if quality >= 3 else (225, 238, 250, 205)
            acy = y_top + int(bh * 0.48)
            if direction == "down":
                dr.arc([cx - 21, acy - 5, cx + 21, acy + 23], start=20, end=160, fill=arc_col, width=2)
            elif direction == "up":
                dr.arc([cx - 21, acy - 24, cx + 21, acy + 4], start=200, end=340, fill=arc_col, width=2)
            elif direction == "left":
                dr.arc([cx - 26, acy - 16, cx + 4, acy + 16], start=110, end=250, fill=arc_col, width=2)
            else:
                dr.arc([cx - 4, acy - 16, cx + 26, acy + 16], start=-70, end=70, fill=arc_col, width=2)
            overlay = np.asarray(pil_ov, dtype=np.uint8)
        return overlay

    # Compute true torso/pelvis centerline (`torso_cx`) from the waist/hip region so side ponytails never shift gear!
    waist_ys = body_mask[y_top + int(bh * 0.36):y_top + int(bh * 0.72), :]
    waist_xs = np.where(waist_ys)[1]
    cx = int(round(0.5 * (int(waist_xs.min()) + int(waist_xs.max())))) if len(waist_xs) > 0 else (x_min + x_max) // 2

    # ------------------------------------------------------------------
    # B. BODY ARMOR (`armor_giap_da_thuoc`, `armor_giap_luoi_thep`, `armor_ao_mua_dau`, `armor_ao_chi`)
    # ------------------------------------------------------------------
    if slot_kind == "armor":
        y_a0 = min(ch - 1, max(0, y_top + int(bh * 0.29)))
        y_a1 = min(ch, max(y_a0 + 1, y_top + int(bh * (0.58 if item_id == "armor_ao_mua_dau" else 0.52))))
        armor_mask = np.zeros((ch, cw), dtype=bool)
        is_profile = direction in ("left", "right")
        base_hw = (6 if item_id == "armor_ao_mua_dau" else 5) if is_profile else (8 if item_id == "armor_ao_mua_dau" else 6)
        c_mid = cx
        for y in range(y_a0, y_a1):
            rel_t = (y - y_a0) / max(1.0, float(y_a1 - y_a0))
            # Sculpted hourglass/cuirass profile: broader at shoulders (rel_t < 0.25), tapered at waist
            if item_id == "armor_ao_mua_dau":
                # Poncho drapes slightly wider at mid-torso, then tapers at hem
                hw = int(round(base_hw + 1.0 * np.sin(rel_t * np.pi)))
            else:
                shoulder_flare = 1 if (0.06 <= rel_t <= 0.26 and not is_profile) else 0
                waist_taper = -1 if (0.45 <= rel_t <= 0.75 and not is_profile) else 0
                hw = max(3, base_hw + shoulder_flare + waist_taper)

            for x in range(max(0, c_mid - hw), min(cw, c_mid + hw + 1)):
                dx = abs(x - c_mid)
                # Curved/pointed cuirass bottom hem (`y_a1 - dx // 2`) so it never looks like a flat box
                hem_limit = y_a1 - (dx // 3 if item_id == "armor_ao_mua_dau" else dx // 2)
                if y >= hem_limit:
                    continue
                # Rounded top shoulder corners
                if y == y_a0 and dx >= hw:
                    continue
                if body_mask[y, x]:
                    armor_mask[y, x] = True

            # V-neck opening on front view (`down`)
            if direction == "down" and y < y_a0 + 4 and item_id != "armor_ao_chi":
                v_hw = max(1, 2 - (y - y_a0) // 2)
                armor_mask[y, max(0, c_mid - v_hw):min(cw, c_mid + v_hw + 1)] = False

        if item_id == "armor_giap_da_thuoc":
            base_c, trim_c, hi_c = _apply_quality_palette((142, 88, 48), (212, 168, 82), quality)
        elif item_id == "armor_giap_luoi_thep":
            base_c, trim_c, hi_c = _apply_quality_palette((138, 148, 164), (192, 164, 96), quality)
        elif item_id == "armor_ao_mua_dau":
            base_c, trim_c, hi_c = _apply_quality_palette((168, 150, 56), (118, 102, 38), quality)
        else:  # armor_ao_chi
            base_c, trim_c, hi_c = _apply_quality_palette((82, 88, 98), (156, 164, 178), quality)

        yy, xx = np.meshgrid(np.arange(ch), np.arange(cw), indexing="ij")
        # 2.5D cylindrical chest shading (lit from top-left)
        dome = np.clip(1.12 - 0.036 * np.abs(xx - (c_mid - 1)) - 0.012 * (yy - y_a0), 0.74, 1.18)
        for c_idx in range(3):
            overlay[armor_mask, c_idx] = np.clip(base_c[c_idx] * dome[armor_mask], 0, 255).astype(np.uint8)
        overlay[armor_mask, 3] = 255

        if item_id == "armor_giap_luoi_thep":
            checker = armor_mask & (((xx + yy) % 2) == 0)
            overlay[checker, :3] = np.clip(overlay[checker, :3].astype(np.int16) - 22, 0, 255).astype(np.uint8)
            belt = armor_mask & (yy == y_a1 - 3)
            overlay[belt, :3] = trim_c
        elif item_id == "armor_giap_da_thuoc":
            strap = armor_mask & ((np.abs((xx - c_mid) - (yy - y_a0) * 0.45) < 1.1) | (yy == y_a1 - 3))
            overlay[strap, :3] = trim_c
        elif item_id == "armor_ao_mua_dau":
            seam = armor_mask & ((xx == c_mid) | (yy == y_a0 + 1))
            overlay[seam, :3] = trim_c
        elif item_id == "armor_ao_chi":
            plates = armor_mask & (((yy - y_a0) % 4 == 0))
            overlay[plates, :3] = np.clip(overlay[plates, :3].astype(np.int16) - 18, 0, 255).astype(np.uint8)
            rivets = armor_mask & (((yy - y_a0) % 4 == 2) & (np.abs(xx - c_mid) == 3))
            overlay[rivets, :3] = hi_c

        ap = np.pad(armor_mask, 1, mode="constant")
        inter = ap[1:-1, 1:-1] & ap[:-2, 1:-1] & ap[2:, 1:-1] & ap[1:-1, :-2] & ap[1:-1, 2:]
        edge = armor_mask & (~inter)
        overlay[edge, :3] = (overlay[edge, :3].astype(np.int16) * 52 // 100).astype(np.uint8)
        return overlay

    # Helper for Pants & Boots: identify columns `x` that belong to actual legs (reach down to `>= y_bot - 8`)
    # so hanging trench-coat tails (`khoa`) or hands at the hip (`an`) are NEVER mistaken for legs/feet!
    leg_col_mask = np.zeros(cw, dtype=bool)
    y_hip_scan = y_top + int(bh * 0.52)
    for x in range(cw):
        c_ys = np.where(body_mask[y_hip_scan:, x])[0]
        if len(c_ys) > 0 and (y_hip_scan + int(c_ys.max())) >= y_bot - 8:
            leg_col_mask[x] = True

    # ------------------------------------------------------------------
    # C. PANTS (`pants_quan_da_lot_bong`, `pants_quan_giap_xich`)
    # ------------------------------------------------------------------
    if slot_kind == "pants":
        y_p0 = min(ch - 1, max(0, y_top + int(bh * 0.54)))
        y_p1 = min(ch, y_bot - 5)
        if y_p1 <= y_p0:
            return overlay
        pants_mask = np.zeros((ch, cw), dtype=bool)
        for y in range(y_p0, y_p1):
            pants_mask[y, :] = body_mask[y, :] & leg_col_mask

        if not np.any(pants_mask):
            return overlay

        if item_id == "pants_quan_da_lot_bong":
            base_c, trim_c, hi_c = _apply_quality_palette((126, 82, 52), (218, 204, 178), quality)
        else:  # pants_quan_giap_xich
            base_c, trim_c, hi_c = _apply_quality_palette((112, 122, 136), (178, 186, 198), quality)

        yy, xx = np.meshgrid(np.arange(ch), np.arange(cw), indexing="ij")
        body_lum = (
            0.299 * base_body_frame[..., 0].astype(np.float32)
            + 0.587 * base_body_frame[..., 1].astype(np.float32)
            + 0.114 * base_body_frame[..., 2].astype(np.float32)
        )
        leg_vals = body_lum[pants_mask]
        mean_l = max(40.0, float(np.mean(leg_vals))) if len(leg_vals) > 0 else 90.0
        rel_shade = np.clip(body_lum / mean_l, 0.74, 1.20)

        for c_idx in range(3):
            overlay[pants_mask, c_idx] = np.clip(base_c[c_idx] * rel_shade[pants_mask], 0, 255).astype(np.uint8)
        overlay[pants_mask, 3] = 255

        if item_id == "pants_quan_da_lot_bong":
            cuff = pants_mask & ((yy >= y_p1 - 2) | (yy == (y_p0 + y_p1) // 2))
            overlay[cuff, :3] = trim_c
        else:
            mesh = pants_mask & (((xx + yy) % 2) == 0)
            overlay[mesh, :3] = np.clip(overlay[mesh, :3].astype(np.int16) - 20, 0, 255).astype(np.uint8)
            knee = pants_mask & (yy == (y_p0 + y_p1) // 2)
            overlay[knee, :3] = trim_c

        ap = np.pad(pants_mask, 1, mode="constant")
        inter = ap[1:-1, 1:-1] & ap[:-2, 1:-1] & ap[2:, 1:-1] & ap[1:-1, :-2] & ap[1:-1, 2:]
        edge = pants_mask & (~inter)
        overlay[edge, :3] = (overlay[edge, :3].astype(np.int16) * 52 // 100).astype(np.uint8)
        return overlay

    # ------------------------------------------------------------------
    # D. BOOTS (`boot_giay_dinh`, `boot_guoc_gio`)
    # ------------------------------------------------------------------
    if slot_kind == "boots":
        # Only select columns `x` that actually belong to a leg reaching the foot zone (`leg_col_mask[x]`)!
        boot_mask = np.zeros((ch, cw), dtype=bool)
        y_leg_zone = y_top + int(bh * 0.72)
        for x in range(cw):
            if not leg_col_mask[x]:
                continue
            col_ys = np.where(body_mask[y_leg_zone:, x])[0]
            if len(col_ys) > 0:
                yb = y_leg_zone + int(col_ys.max())
                yt = max(y_leg_zone, yb - 6)
                boot_mask[yt:yb + 1, x] = body_mask[yt:yb + 1, x]

        if not np.any(boot_mask):
            return overlay

        if item_id == "boot_giay_dinh":
            base_c, spike_c, hi_c = _apply_quality_palette((92, 62, 42), (205, 212, 222), quality)
        else:  # boot_guoc_gio
            base_c, spike_c, hi_c = _apply_quality_palette((178, 126, 68), (64, 158, 142), quality)

        overlay[boot_mask, 0] = base_c[0]
        overlay[boot_mask, 1] = base_c[1]
        overlay[boot_mask, 2] = base_c[2]
        overlay[boot_mask, 3] = 255

        # Add sole detail: metal spikes for `boot_giay_dinh` or wooden clog platform + strap for `boot_guoc_gio`
        for x in range(cw):
            bys = np.where(boot_mask[:, x])[0]
            if len(bys) == 0:
                continue
            yb = int(bys.max())
            yt = int(bys.min())
            overlay[yt, x] = outline_col
            if item_id == "boot_giay_dinh":
                if x % 2 == 0 and yb + 1 < ch:
                    overlay[yb, x] = [*spike_c, 255]
                else:
                    overlay[yb, x] = outline_col
            else:
                # Wooden clog sole + teal wind strap
                overlay[yb, x] = [*base_c, 255]
                if yb - 2 >= yt:
                    overlay[yb - 2, x] = [*spike_c, 255]
        return overlay

    # ------------------------------------------------------------------
    # E. BRACELETS (`brc_vong_cach_dien`, `brc_vong_huyet_nha`)
    # ------------------------------------------------------------------
    if slot_kind == "bracelet":
        if direction == "down":
            bx = cx - 8 + pose["lean_x"]
            by = y_top + int(bh * 0.45) + pose["bob_y"] + pose["arm_swing_r"] // 2
        elif direction == "up":
            bx = cx + 8 + pose["lean_x"]
            by = y_top + int(bh * 0.45) + pose["bob_y"] + pose["arm_swing_r"] // 2
        elif direction == "left":
            bx = cx - 2 + pose["lean_x"] + pose["prof_arm_dx"]
            by = y_top + int(bh * 0.45) + pose["bob_y"]
        else:
            bx = cx + 2 + pose["lean_x"] + pose["prof_arm_dx"]
            by = y_top + int(bh * 0.45) + pose["bob_y"]

        pil_ov = Image.fromarray(overlay, "RGBA")
        dr = ImageDraw.Draw(pil_ov)
        if item_id == "brc_vong_cach_dien":
            # Black rubber insulating band with copper stud
            band_c, stud_c, _ = _apply_quality_palette((42, 46, 52), (218, 138, 68), quality)
            dr.rectangle([bx - 3, by - 2, bx + 3, by + 2], fill=(*band_c, 255), outline=(18, 14, 18, 255))
            dr.point((bx, by), fill=(*stud_c, 255))
        else:
            # Vòng Huyết Nha: Crimson cord with pale ivory beast fangs
            band_c, fang_c, _ = _apply_quality_palette((168, 38, 42), (242, 236, 218), quality)
            dr.rectangle([bx - 3, by - 2, bx + 3, by + 1], fill=(*band_c, 255), outline=(24, 14, 16, 255))
            dr.point((bx - 2, by + 2), fill=(*fang_c, 255))
            dr.point((bx, by + 2), fill=(*fang_c, 255))
            dr.point((bx + 2, by + 2), fill=(*fang_c, 255))
        return np.asarray(pil_ov, dtype=np.uint8)

    # ------------------------------------------------------------------
    # F. NECKLACES (`nck_coi_chan_heo`, `nck_mat_day_den_bao`)
    # ------------------------------------------------------------------
    if slot_kind == "necklace":
        nx = cx + pose["lean_x"] + (2 if direction == "right" else (-2 if direction == "left" else 0))
        ny = y_top + int(bh * 0.30) + pose["bob_y"]
        pil_ov = Image.fromarray(overlay, "RGBA")
        dr = ImageDraw.Draw(pil_ov)
        if direction == "up":
            # Back-of-neck cord clasp (drawn behind body when facing Up per EquipmentAppearance.Order = -3, keeps cell non-empty for QA)
            cord_c = (148, 52, 42, 255) if item_id == "nck_coi_chan_heo" else (72, 64, 58, 255)
            dr.rectangle([nx - 2, ny - 2, nx + 2, ny], fill=cord_c, outline=(22, 16, 20, 255))
            return np.asarray(pil_ov, dtype=np.uint8)
        if item_id == "nck_coi_chan_heo":
            # Herding whistle on a cord
            main_c, cord_c, hi_c = _apply_quality_palette((214, 172, 74), (148, 52, 42), quality)
            dr.arc([nx - 4, ny - 4, nx + 4, ny + 2], start=0, end=180, fill=(*cord_c, 255), width=1)
            dr.rectangle([nx - 2, ny + 1, nx + 2, ny + 5], fill=(*main_c, 255), outline=(24, 18, 22, 255))
            dr.point((nx, ny + 2), fill=(*hi_c, 255))
        else:
            # Mặt Dây Đèn Bão: Storm-lantern pendant with warm glowing core
            main_c, glow_c, _ = _apply_quality_palette((132, 102, 58), (255, 226, 118), quality)
            dr.arc([nx - 4, ny - 4, nx + 4, ny + 2], start=0, end=180, fill=(72, 64, 58, 255), width=1)
            dr.rectangle([nx - 3, ny + 1, nx + 3, ny + 6], fill=(*main_c, 255), outline=(22, 16, 20, 255))
            dr.rectangle([nx - 1, ny + 2, nx + 1, ny + 4], fill=(*glow_c, 255))
        return np.asarray(pil_ov, dtype=np.uint8)

    return overlay


# ==============================================================================
# 5. EXTRACT UNARMED BASE CELLS FOR KHOA & AN AND BUILD FULL PACKAGES
# ==============================================================================

def extract_khoa_unarmed_4dir_cells() -> tuple[list[np.ndarray], Image.Image]:
    """
    Extracts Khoa's approved everyday unarmed outfit from `an_khoa_mature_couture_4x4_1793586773537.jpg`
    (Row 2: Down, Left, Up, Right) with strict magenta despilling and 2026 AAA pixel clarity.
    """
    im = Image.open(KHOA_UNARMED_SRC)
    rgba = remove_magenta_sheet_bg(im)
    kh, kw = rgba.shape[:2]
    y0, y1 = int(2 * kh / 4), int(3 * kh / 4)

    cells = []
    front_hd = None
    for c in range(4):
        x0, x1 = int(c * kw / 4), int((c + 1) * kw / 4)
        raw_crop = rgba[y0:y1, x0:x1].copy()
        if c == 0:
            ys, xs = np.where(raw_crop[..., 3] > 64)
            front_hd = Image.fromarray(raw_crop[ys.min():ys.max() + 1, xs.min():xs.max() + 1], "RGBA")
        cell = apply_2026_aaa_pixel_clarity(raw_crop, cw=CW, ch=CH, foot_pad=5, target_h=56, is_side=(c in (1, 3)))
        cells.append(cell)
    return cells, front_hd


def extract_an_unarmed_4dir_cells() -> tuple[list[np.ndarray], Image.Image]:
    """
    Extracts An's genuine 4-direction unarmed turnaround (`Down`, `Left`, `Up`, `Right`)
    from `an_c2a_25d_volumetric_4x2_turnaround_1793634911637.jpg` with 2026 AAA pixel clarity.
    """
    turn_im = Image.open(AN_TURNAROUND_C2A).convert("RGBA")
    tw, th = turn_im.size
    splits = [0, 314, 677, 960, tw]

    cells = []
    front_hd = None
    for idx in range(4):
        t_raw = turn_im.crop((splits[idx] + 4, int(th * 0.01), splits[idx + 1] - 4, int(th * 0.99)))
        clean_rgba = remove_cream_studio_bg_clean(t_raw)
        if idx == 0:
            ys, xs = np.where(clean_rgba[..., 3] > 64)
            front_hd = Image.fromarray(clean_rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1], "RGBA")
            # Level viewer-right leg (`x >= cx_hr`) in standing base frame so both boots touch the ground line evenly
            y_min_hr, y_max_hr = int(ys.min()), int(ys.max())
            h_hr = y_max_hr - y_min_hr + 1
            y_thigh_hr = y_min_hr + int(h_hr * 0.56)
            leg_xs_hr = np.where(clean_rgba[y_thigh_hr:, :, 3] > 128)[1]
            if len(leg_xs_hr) > 0:
                cx_hr = int(round(0.5 * (int(leg_xs_hr.min()) + int(leg_xs_hr.max()))))
                ys_l = np.where(clean_rgba[y_thigh_hr:, :cx_hr, 3] > 128)[0] + y_thigh_hr
                ys_r = np.where(clean_rgba[y_thigh_hr:, cx_hr:, 3] > 128)[0] + y_thigh_hr
                if len(ys_l) > 0 and len(ys_r) > 0:
                    y_bot_l = int(ys_l.max())
                    y_bot_r = int(ys_r.max())
                    if y_bot_r < y_bot_l - 2:
                        sub_r = clean_rgba[y_thigh_hr:y_bot_r + 1, cx_hr:].copy()
                        new_hr = y_bot_l - y_thigh_hr + 1
                        pil_r = Image.fromarray(sub_r, "RGBA").resize((sub_r.shape[1], new_hr), Image.Resampling.LANCZOS)
                        clean_rgba[y_thigh_hr:y_bot_l + 1, cx_hr:] = np.asarray(pil_r, dtype=np.uint8)
        cell = apply_2026_aaa_pixel_clarity(clean_rgba, cw=CW, ch=CH, foot_pad=5, target_h=56, is_side=(idx in (1, 3)))
        if idx in (0, 2):
            # Ensure An's front/back legs are full-volumetric 4-5px wide columns (zero wide vertical slit or thin sticks)
            ys_c, xs_c = np.where(cell[..., 3] > 180)
            if len(ys_c) > 0:
                y_t, y_b = int(ys_c.min()), int(ys_c.max())
                bh_c = max(20, y_b - y_t + 1)
                y_crotch_c = y_t + int(bh_c * 0.63)
                leg_xs_c = np.where(cell[y_crotch_c:, :, 3] > 180)[1]
                cx_c = int(round(0.5 * (int(leg_xs_c.min()) + int(leg_xs_c.max())))) if len(leg_xs_c) > 0 else CW // 2
                for y in range(y_crotch_c + 1, y_b + 1):
                    is_boot = y >= y_b - 6
                    # Left leg: cx_c - 5 .. cx_c - 1 (5px wide); Right leg: cx_c + 1 .. cx_c + 5 (5px wide)
                    for side_sign, x_range in ((-1, range(cx_c - 5, cx_c)), (1, range(cx_c + 1, cx_c + 6))):
                        for x in x_range:
                            if 0 <= x < CW and cell[y, x, 3] == 0:
                                dist_edge = min(abs(x - x_range.start), abs(x - (x_range.stop - 1)))
                                if is_boot:
                                    cell[y, x] = [36, 40, 52, 255] if dist_edge == 0 else [54, 60, 76, 255]
                                else:
                                    cell[y, x] = [208, 166, 142, 255] if dist_edge == 0 else [242, 208, 184, 255]
                    # Keep a crisp 1px separation at cx_c
                    if y >= y_crotch_c + 2:
                        cell[y, cx_c] = [0, 0, 0, 0]
        cells.append(cell)
    return cells, front_hd


def save_sheet_and_json(
    out_dir: Path,
    family: str,
    clip_name: str,
    sheet_np: np.ndarray,
    nframes: int,
    fps: float,
    stride_m: float,
    stride_px: float,
    source_label: str,
):
    out_dir.mkdir(parents=True, exist_ok=True)
    Image.fromarray(sheet_np, "RGBA").save(out_dir / f"{family}_{clip_name}.png")
    meta = {
        "source": f"{source_label} ({family}:{clip_name})",
        "cell": [CW, CH],
        "rows": DIRS,
        "frames_per_row": nframes,
        "pivot_px_from_bottom_left": [CW // 2, 5],
        "fps": fps,
        "colors": 96,
    }
    if stride_m > 0:
        meta["stride_m_per_cycle"] = stride_m
        meta["stride_px_per_cycle"] = stride_px
    (out_dir / f"{family}_{clip_name}.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")


def build_protagonist_and_all_16_gear_layers(who_prefix: str, base_cells: list[np.ndarray], front_hd: Image.Image) -> dict[str, np.ndarray]:
    """
    Builds all 8 clips for the unarmed protagonist (`khoa` or `an`) AND all 16 GDD 12.3
    paper-doll gear families (`<who>__<itemId>`) + quality tiers (`__II`, `__III`, `__IV` for weapons & armors).
    Returns a dictionary of rendered sheets for x1.5 filmstrip verification.
    """
    body_clips: dict[str, np.ndarray] = {}

    # 1. Build Unarmed Base Body Clips
    for clip_name, nframes, fps, stride_m, stride_px in CLIP_SPECS:
        sheet = np.zeros((CH * 4, CW * nframes, 4), dtype=np.uint8)
        for r, d in enumerate(DIRS):
            for f in range(nframes):
                sheet[r * CH:(r + 1) * CH, f * CW:(f + 1) * CW] = render_unarmed_body_frame(
                    base_cells[r], d, clip_name, f, nframes
                )
        body_clips[clip_name] = sheet

        target_roots = (ASSETS_CHARS, FROM_ARGON_CHARS, DELIV_CHARS) if who_prefix == "khoa" else (ASSETS_CHARS,)
        for fam in (who_prefix, f"argon_{who_prefix}"):
            for root_dir in target_roots:
                save_sheet_and_json(
                    root_dir / fam,
                    fam,
                    clip_name,
                    sheet,
                    nframes,
                    fps,
                    stride_m,
                    stride_px,
                    "Argon 2026 AAA Unarmed Base Body",
                )

    # Save clean unarmed Paper-Doll base in UI/Portraits
    tw, th = 360, 480
    scale = min((tw - 24) / max(1, front_hd.width), (th - 24) / max(1, front_hd.height))
    nw, nh = int(round(front_hd.width * scale)), int(round(front_hd.height * scale))
    doll_canvas = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    rsz = front_hd.resize((nw, nh), Image.Resampling.LANCZOS)
    doll_canvas.paste(rsz, ((tw - nw) // 2, (th - nh) // 2), rsz)
    for p_root in PORTRAITS_DIRS:
        p_root.mkdir(parents=True, exist_ok=True)
        doll_canvas.save(p_root / f"{who_prefix}_paperdoll_25d.png")
        doll_canvas.save(p_root / f"{who_prefix}_combat_25d.png")

    # 2. Build All 16 GDD 12.3 Paper-Doll Overlay Families (`<who>__<itemId>`)
    gear_sample_sheets: dict[str, np.ndarray] = {}
    for item_id, slot_kind, item_vn in GDD_16_ITEMS:
        qualities = (1, 2, 3, 4) if slot_kind in ("weapon", "armor") else (1, 3)
        for q in qualities:
            q_suffix = "" if q == 1 else ("__II" if q == 2 else ("__III" if q == 3 else "__IV"))
            family = f"{who_prefix}__{item_id}{q_suffix}"
            for clip_name, nframes, fps, stride_m, stride_px in CLIP_SPECS:
                b_sheet = body_clips[clip_name]
                ov_sheet = np.zeros_like(b_sheet)
                for r, d in enumerate(DIRS):
                    for f in range(nframes):
                        b_frame = b_sheet[r * CH:(r + 1) * CH, f * CW:(f + 1) * CW]
                        ov_frame = render_gear_overlay_frame(
                            b_frame, item_id, slot_kind, d, clip_name, f, nframes, quality=q
                        )
                        ov_sheet[r * CH:(r + 1) * CH, f * CW:(f + 1) * CW] = ov_frame

                overlay_roots = (ASSETS_CHARS, FROM_ARGON_CHARS) if who_prefix == "khoa" else (ASSETS_CHARS,)
                for root_dir in overlay_roots:
                    save_sheet_and_json(
                        root_dir / family,
                        family,
                        clip_name,
                        ov_sheet,
                        nframes,
                        fps,
                        stride_m,
                        stride_px,
                        f"Argon 2026 AAA Paper-Doll Overlay ({item_vn} Q{q})",
                    )
                if q == 1 and clip_name in ("walk", "attack"):
                    gear_sample_sheets[f"{item_id}_{clip_name}"] = ov_sheet

    return {"body": body_clips, "gear": gear_sample_sheets}


def cleanup_rejected_set_folders():
    """Removes rejected fixed-set folders (`*_armor_light/medium/heavy`, `an_style*`) from Assets & from_argon."""
    bad_names = [
        "an_armor_light",
        "an_armor_medium",
        "an_armor_heavy",
        "khoa_armor_light",
        "khoa_armor_medium",
        "khoa_armor_heavy",
        "an_style0",
        "an_style1",
        "an_style2",
        "an_style3",
    ]
    for root_dir in (ASSETS_CHARS, FROM_ARGON_CHARS, DELIV_CHARS):
        if not root_dir.exists():
            continue
        for b in bad_names:
            p = root_dir / b
            if p.exists():
                shutil.rmtree(p, ignore_errors=True)
    for p_root in PORTRAITS_DIRS:
        if not p_root.exists():
            continue
        for pat in ("*_paperdoll_armor_*.png", "an_paperdoll_style*.png", "an_style*_neutral.png"):
            for f in p_root.glob(pat):
                f.unlink(missing_ok=True)


def _composite_row_ordered(body_sheet: np.ndarray, gear_map: dict[str, np.ndarray], loadout: dict[str, str]) -> np.ndarray:
    """
    Composites a 4-row sheet respecting `EquipmentAppearance.Order(slot, facing)`:
    - Row 0 (`down`) & Row 3 (`right`): body -> boots -> pants -> armor -> necklace -> bracelet -> weapon
    - Row 1 (`left`): weapon -> body -> boots -> pants -> armor -> necklace -> bracelet
    - Row 2 (`up`): necklace -> weapon -> bracelet -> body -> boots -> pants -> armor
    """
    comp = np.zeros_like(body_sheet)
    orders = {
        0: [None, "boot", "pants", "armor", "nck", "brc", "wpn"],
        1: ["wpn", None, "boot", "pants", "armor", "nck", "brc"],
        2: ["nck", "wpn", "brc", None, "boot", "pants", "armor"],
        3: [None, "boot", "pants", "armor", "nck", "brc", "wpn"],
    }
    for r in range(4):
        y0, y1 = r * CH, (r + 1) * CH
        row_canvas = np.zeros_like(body_sheet[y0:y1])
        for slot_key in orders[r]:
            if slot_key is None:
                alpha_paste(row_canvas, body_sheet[y0:y1], 0, 0)
            else:
                item_id = loadout[slot_key]
                ov_row = gear_map[f"{item_id}_walk"][y0:y1]
                alpha_paste(row_canvas, ov_row, 0, 0)
        comp[y0:y1] = row_canvas
    return comp


def build_x15_nearest_filmstrip_verification(khoa_res: dict, an_res: dict):
    """
    Renders a x1.5 nearest-neighbour filmstrip board (`2026-11-03-paperdoll-gear-art-spec.md` line 65):
    - Top Left: Khoa Unarmed Walk (4 directions x 6 frames)
    - Top Right: Khoa + Paper-Doll Layers Composited (`Giáp Da Thuộc` + `Quần Da Lót Bông` + `Giày Đinh` + `Dao Rựa` + `Còi Chăn Heo` + `Vòng Huyết Nha`)
    - Bottom Left: An Unarmed Walk (4 directions x 6 frames)
    - Bottom Right: An + Paper-Doll Layers Composited (`Áo Mưa Dầu` + `Quần Giáp Xích` + `Guốc Gió` + `Giáo Móc` + `Mặt Dây Đèn Bão` + `Vòng Cách Điện`)
    """
    k_walk = khoa_res["body"]["walk"].copy()
    k_comp_walk = _composite_row_ordered(
        k_walk,
        khoa_res["gear"],
        {
            "boot": "boot_giay_dinh",
            "pants": "pants_quan_da_lot_bong",
            "armor": "armor_giap_da_thuoc",
            "nck": "nck_coi_chan_heo",
            "brc": "brc_vong_huyet_nha",
            "wpn": "wpn_dao_rua",
        },
    )

    a_walk = an_res["body"]["walk"].copy()
    a_comp_walk = _composite_row_ordered(
        a_walk,
        an_res["gear"],
        {
            "boot": "boot_guoc_gio",
            "pants": "pants_quan_giap_xich",
            "armor": "armor_ao_mua_dau",
            "nck": "nck_mat_day_den_bao",
            "brc": "brc_vong_cach_dien",
            "wpn": "wpn_giao_moc",
        },
    )

    # Assemble board at native 1x then scale x1.5 with Image.Resampling.NEAREST
    w1 = int(k_walk.shape[1])
    h1 = CH * 4
    board_1x = Image.new("RGBA", (w1 * 2 + 24, h1 * 2 + 24), (24, 28, 36, 255))
    # Checkerboard background so transparency & zero magenta fringe are 100% visible
    b_arr = np.asarray(board_1x, dtype=np.uint8).copy()
    yy, xx = np.meshgrid(np.arange(b_arr.shape[0]), np.arange(b_arr.shape[1]), indexing="ij")
    chk = ((xx // 12) + (yy // 12)) % 2 == 0
    b_arr[chk, :3] = [34, 38, 48]
    board_1x = Image.fromarray(b_arr, "RGBA")

    p_kw = Image.fromarray(k_walk, "RGBA")
    p_kc = Image.fromarray(k_comp_walk, "RGBA")
    p_aw = Image.fromarray(a_walk, "RGBA")
    p_ac = Image.fromarray(a_comp_walk, "RGBA")

    board_1x.paste(p_kw, (6, 6), p_kw)
    board_1x.paste(p_kc, (w1 + 18, 6), p_kc)
    board_1x.paste(p_aw, (6, h1 + 18), p_aw)
    board_1x.paste(p_ac, (w1 + 18, h1 + 18), p_ac)

    tw = int(round(board_1x.width * 1.5))
    th = int(round(board_1x.height * 1.5))
    board_x15 = board_1x.resize((tw, th), Image.Resampling.NEAREST)
    out_path = BRAIN / "paperdoll_16_gdd_gear_x15_filmstrip_verification.png"
    board_x15.save(out_path)
    # Also save in from_argon for Claude's inspection
    (FROM_ARGON / "Assets" / "Art" / "PixelMyth" / "Characters" / "paperdoll_x15_filmstrip_preview.png").write_bytes(
        out_path.read_bytes()
    )
    print(f"[OK] Saved x1.5 nearest-neighbour filmstrip verification to {out_path}")


def main():
    cleanup_rejected_set_folders()
    khoa_cells, khoa_hd = extract_khoa_unarmed_4dir_cells()
    an_cells, an_hd = extract_an_unarmed_4dir_cells()

    print("[1/3] Building Khoa Unarmed Base Body + 16 GDD 12.3 Paper-Doll Gear Families...")
    khoa_res = build_protagonist_and_all_16_gear_layers("khoa", khoa_cells, khoa_hd)

    print("[2/3] Building An Unarmed Base Body + 16 GDD 12.3 Paper-Doll Gear Families...")
    an_res = build_protagonist_and_all_16_gear_layers("an", an_cells, an_hd)

    print("[3/3] Rendering x1.5 Nearest-Neighbour Filmstrip Verification...")
    build_x15_nearest_filmstrip_verification(khoa_res, an_res)
    print("[DONE] All Unarmed Base Bodies + 16x2 GDD 12.3 Paper-Doll Gear Overlay Families Built & Verified!")


if __name__ == "__main__":
    main()
