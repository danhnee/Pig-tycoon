# -*- coding: utf-8 -*-
"""
World Entities (18+ Pigs, Pets, NPCs, Specialists, Mercenaries, Monsters, Bosses)
Multi-Clip Animation & 2.5D Map Grounding Builder.

Deliverables:
1. Inspects and upgrades all non-hero character folders in:
   - D:\\Pig-tycoon\\.collab\\claude-2d\\ui_and_map_sync\\from_argon\\Assets\\Art\\PixelMyth\\Characters\\
   and syncs to:
   - D:\\Pig-tycoon\\Assets\\Art\\PixelMyth\\Characters\\
   (Strictly skips an*, khoa*, vfx_*, wpn_*, and Buildings/).

2. Ensures:
   - 18+ Pig Breeds & Stages (pig_*, pig, argon_pig):
     4-directional (down, left, up, right) multi-frame sheets + .json (fps: 16..20) for:
     idle, walk, run, eat, drink, sleep, happy, panic, hit, death (plus play_dead where present).
     Soft grounded contact shadow (alpha <= 52) right under hooves at pivot=[36, 5] and a crisp
     1px warm-umber selective outline (#261912) so pigs never float above the pasture.
   - NPCs, Specialists & Mercenaries (ba_sau, ong_ton, tam_va, cuc, le, man, anh_tu, anh_ca,
     dung, kien, bi_nhan, merc_*, npc_*, etc.):
     4-directional (down, left, up, right) sheets + .json (fps: 16..22) for:
     idle, idle_fidget_1, walk, run, work, talk, interact, attack, hit, death.
     Grounded foot contact shadows (alpha <= 54) at pivot=[36, 5].
   - Monsters, Raiders & Bosses (thief, cho_suong, dau_linh_trom, chua_trom_riu_sat, crawler,
     bay_nhay, bong_huyen, chuot_gam_kho, ke_cat_day, ke_mang_dich, mon_*, thiet_giap_cong_thanh,
     thu_bao, thuc_the_suong, ufo_*):
     4-directional (down, left, up, right) sheets + .json (fps: 18..24, with accurate effect_frame) for:
     idle, walk, move, run, attack, skill, charge, hit, death.
     Grounded contact shadows (alpha <= 52).
   - Pets (pet_co, pet_giam_doc, pet_mop, pet_phieu, pet_rang, pet_tin, pet_tron):
     4-directional (down, left, up, right) sheets + .json (fps: 16..20) for:
     idle, walk, run, happy, work, bark, attack, interact, sleep, hit, death.
     Grounded contact shadows (alpha <= 52).

3. Renders visual verification showcase:
   C:\\Users\\Admin\\.gemini\\antigravity\\brain\\ac0f248a-d876-47b0-9f6a-277b76657366\\world_entities_multi_animation_and_grounding_proof.png
"""

import json
import math
import os
import shutil
from typing import Dict, List, Optional, Tuple

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = r"D:\Pig-tycoon"
FROM_ARGON_CHARS = os.path.join(
    ROOT, r".collab\claude-2d\ui_and_map_sync\from_argon\Assets\Art\PixelMyth\Characters"
)
ASSETS_CHARS = os.path.join(ROOT, r"Assets\Art\PixelMyth\Characters")
MAPS_DIR = os.path.join(
    ROOT, r".collab\claude-2d\ui_and_map_sync\from_argon\Assets\Art\PixelMyth\Maps"
)
PROOF_OUT_BRAIN = r"C:\Users\Admin\.gemini\antigravity\brain\ac0f248a-d876-47b0-9f6a-277b76657366\world_entities_multi_animation_and_grounding_proof.png"
PROOF_OUT_COLLAB = os.path.join(
    ROOT, r".collab\claude-2d\ui_and_map_sync\world_entities_multi_animation_and_grounding_proof.png"
)

ROWS = ["down", "left", "up", "right"]

PIG_FOLDERS = [
    "argon_pig",
    "pig",
    "pig_dang_lon",
    "pig_heo_gia",
    "pig_heo_non",
    "pig_hong_dien",
    "pig_hu_the",
    "pig_kim_tho",
    "pig_lam_khe",
    "pig_leader",
    "pig_loi_mach",
    "pig_moc_cuoc",
    "pig_mong_nhan",
    "pig_phong_mau",
    "pig_thiet_bi",
    "pig_tho_tram",
    "pig_tinh_quang",
    "pig_white_feet",
    "pig_white_foot",
    "pig_xich_mao",
]

NPC_MERC_FOLDERS = [
    "anh_ca",
    "anh_tu",
    "ba_ban_rau",
    "ba_sau",
    "bi_nhan",
    "cuc",
    "dung",
    "kien",
    "le",
    "man",
    "merc_chot",
    "merc_do",
    "merc_mien",
    "merc_tu_troi",
    "merc_van",
    "nguoi_ao_mua",
    "nguoi_ban_dem",
    "nguoi_ban_tin",
    "npc_appraisal",
    "npc_auction",
    "npc_mercenary",
    "npc_post",
    "npc_quarantine",
    "npc_recruit",
    "npc_supply",
    "ong_ton",
    "tam_va",
]

MONSTER_BOSS_FOLDERS = [
    "bay_nhay",
    "bong_huyen",
    "cho_suong",
    "chua_trom_riu_sat",
    "chuot_gam_kho",
    "crawler",
    "dau_linh_trom",
    "ke_cat_day",
    "ke_mang_dich",
    "mon_boss_thien_tai",
    "mon_chim_ung_dem",
    "mon_doi_quy",
    "thief",
    "thiet_giap_cong_thanh",
    "thu_bao",
    "thuc_the_suong",
    "ufo_mothership",
    "ufo_phao_ham",
    "ufo_thu_hoach",
    "ufo_trinh_sat",
]

PET_FOLDERS = [
    "pet_co",
    "pet_giam_doc",
    "pet_mop",
    "pet_phieu",
    "pet_rang",
    "pet_tin",
    "pet_tron",
]

HOVER_ENTITIES = {
    "ufo_mothership",
    "ufo_phao_ham",
    "ufo_thu_hoach",
    "ufo_trinh_sat",
    "mon_chim_ung_dem",
    "mon_doi_quy",
    "bong_huyen",
}


def write_json(path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")


def ensure_kien_seed_folder() -> None:
    """Ensures specialist NPC 'kien' exists using 'anh_tu'/'man' 2.5D villager base with warm bronze/slate tunic tint."""
    kien_dir = os.path.join(FROM_ARGON_CHARS, "kien")
    src_dir = os.path.join(FROM_ARGON_CHARS, "anh_tu")
    if not os.path.exists(src_dir):
        src_dir = os.path.join(FROM_ARGON_CHARS, "man")
    os.makedirs(kien_dir, exist_ok=True)
    for clip in ("idle", "interact", "walk"):
        src_png = os.path.join(src_dir, f"{os.path.basename(src_dir)}_{clip}.png")
        src_js = os.path.join(src_dir, f"{os.path.basename(src_dir)}_{clip}.json")
        dst_png = os.path.join(kien_dir, f"kien_{clip}.png")
        dst_js = os.path.join(kien_dir, f"kien_{clip}.json")
        if os.path.exists(src_png) and not os.path.exists(dst_png):
            im = Image.open(src_png).convert("RGBA")
            arr = np.array(im, dtype=np.int16)
            mask = arr[:, :, 3] >= 180
            # Subtle warm bronze-indigo specialist palette shift on midtones
            r, g, b = arr[:, :, 0], arr[:, :, 1], arr[:, :, 2]
            cloth_mask = mask & (y_grid_for(arr.shape[0], arr.shape[1]) % 84 >= 42) & (y_grid_for(arr.shape[0], arr.shape[1]) % 84 <= 68)
            arr[:, :, 0] = np.where(cloth_mask, np.clip(r * 0.85 + 28, 0, 255), r)
            arr[:, :, 1] = np.where(cloth_mask, np.clip(g * 0.95 + 18, 0, 255), g)
            arr[:, :, 2] = np.where(cloth_mask, np.clip(b * 1.12 + 24, 0, 255), b)
            Image.fromarray(arr.astype(np.uint8), "RGBA").save(dst_png)
        if os.path.exists(src_js) and not os.path.exists(dst_js):
            meta = json.load(open(src_js, encoding="utf-8"))
            meta["source"] = f"Argon 2.5D Specialist Sheet (kien:{clip})"
            write_json(dst_js, meta)


def y_grid_for(h: int, w: int) -> np.ndarray:
    return np.broadcast_to(np.arange(h, dtype=np.int16)[:, None], (h, w))


def extract_clean_body(cell_rgba: np.ndarray) -> np.ndarray:
    """Strips old semi-transparent shadow blobs (alpha < 180) and keeps crisp opaque character pixels."""
    out = cell_rgba.copy()
    low_a = out[:, :, 3] < 180
    out[low_a] = 0
    out[:, :, 3] = np.where(out[:, :, 3] >= 180, 255, 0)
    return out


def align_body_to_ground(
    body_rgba: np.ndarray,
    ground_y: int,
    center_x: int,
    dy_offset: int = 0,
    dx_offset: int = 0,
    is_hover: bool = False,
) -> Tuple[np.ndarray, Tuple[int, int, int, int]]:
    """
    Shifts the opaque body so its bottom feet/hooves sit squarely at `ground_y + dy_offset`
    (or hovers above ground_y for flying entities), centered horizontally near center_x + dx_offset.
    Returns (aligned_rgba, (xmin, ymin, xmax, ymax)).
    """
    ch, cw, _ = body_rgba.shape
    ys, xs = np.where(body_rgba[:, :, 3] > 128)
    if len(ys) == 0:
        return body_rgba.copy(), (center_x - 8, ground_y - 16, center_x + 8, ground_y)

    ymin, ymax = int(ys.min()), int(ys.max())
    xmin, xmax = int(xs.min()), int(xs.max())
    cur_cx = (xmin + xmax) // 2

    target_ymax = (ground_y - 6 if is_hover else ground_y) + dy_offset
    shift_y = target_ymax - ymax
    # Only gently recenter if horizontal drift is large (> 5px) so walk/attack motion is preserved
    drift_x = center_x - cur_cx
    if abs(drift_x) > 6:
        shift_x = int(np.sign(drift_x) * (abs(drift_x) - 4)) + dx_offset
    else:
        shift_x = dx_offset

    out = np.zeros_like(body_rgba)
    src_y0 = max(0, -shift_y)
    src_y1 = min(ch, ch - shift_y)
    dst_y0 = max(0, shift_y)
    dst_y1 = dst_y0 + (src_y1 - src_y0)

    src_x0 = max(0, -shift_x)
    src_x1 = min(cw, cw - shift_x)
    dst_x0 = max(0, shift_x)
    dst_x1 = dst_x0 + (src_x1 - src_x0)

    if dst_y1 > dst_y0 and dst_x1 > dst_x0:
        out[dst_y0:dst_y1, dst_x0:dst_x1] = body_rgba[src_y0:src_y1, src_x0:src_x1]

    ys2, xs2 = np.where(out[:, :, 3] > 128)
    if len(ys2) == 0:
        return out, (center_x - 8, ground_y - 16, center_x + 8, ground_y)
    return out, (int(xs2.min()), int(ys2.min()), int(xs2.max()), int(ys2.max()))


def apply_selective_warm_umber_outline(body_rgba: np.ndarray, is_pig: bool = False) -> np.ndarray:
    """
    Ensures a crisp 1px warm-umber selective outline (#261912, modulated by height) around
    the opaque body without thickening existing dark outlines.
    """
    out = body_rgba.copy()
    alpha = out[:, :, 3] > 128
    if not alpha.any():
        return out

    # Identify boundary pixels of the opaque mask (opaque pixels touching at least one transparent neighbor)
    p = np.pad(alpha, 1, mode="constant", constant_values=False)
    eroded = p[1:-1, 1:-1] & p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]
    boundary = alpha & (~eroded)

    # Darken boundary pixels that are overly bright so every pig/entity has a crisp selective outline
    lum = (0.299 * out[:, :, 0] + 0.587 * out[:, :, 1] + 0.114 * out[:, :, 2])
    if is_pig:
        # On pigs, also check exterior 1px ring where boundary is bright, or darken boundary directly to warm umber
        needs_outline = boundary & (lum > 72)
        out[needs_outline, 0] = np.clip(out[needs_outline, 0] * 0.32 + 26, 28, 62).astype(np.uint8)
        out[needs_outline, 1] = np.clip(out[needs_outline, 1] * 0.28 + 16, 18, 42).astype(np.uint8)
        out[needs_outline, 2] = np.clip(out[needs_outline, 2] * 0.25 + 12, 12, 32).astype(np.uint8)
    else:
        needs_outline = boundary & (lum > 95)
        out[needs_outline, 0] = np.clip(out[needs_outline, 0] * 0.42 + 22, 24, 72).astype(np.uint8)
        out[needs_outline, 1] = np.clip(out[needs_outline, 1] * 0.38 + 15, 16, 52).astype(np.uint8)
        out[needs_outline, 2] = np.clip(out[needs_outline, 2] * 0.35 + 12, 12, 42).astype(np.uint8)
    return out


def add_grounded_contact_shadow(
    cell_rgba: np.ndarray,
    ground_y: int,
    bbox: Tuple[int, int, int, int],
    max_alpha: int = 50,
) -> np.ndarray:
    """
    Renders a soft elliptical 2.5D contact shadow right on `ground_y` under the character's feet/hooves.
    Strictly caps shadow alpha <= max_alpha (e.g. <= 52 for pigs/monsters/pets, <= 54 for NPCs)
    and never overwrites opaque character pixels.
    """
    out = cell_rgba.copy()
    ch, cw, _ = out.shape
    xmin, ymin, xmax, ymax = bbox
    span_w = max(10, xmax - xmin + 1)
    cx = (xmin + xmax) / 2.0
    cy = float(min(ch - 3, max(4, ground_y)))

    rx = max(7.0, min(cw * 0.38, span_w * 0.48))
    ry = max(2.4, min(5.0, ch * 0.045))

    y0 = max(0, int(cy - ry - 2))
    y1 = min(ch, int(cy + ry + 3))
    x0 = max(0, int(cx - rx - 2))
    x1 = min(cw, int(cx + rx + 3))

    yy, xx = np.mgrid[y0:y1, x0:x1]
    dist_sq = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2

    shadow_a = np.zeros_like(dist_sq, dtype=np.uint8)
    shadow_a = np.where(dist_sq <= 1.0, int(max_alpha * 0.38), shadow_a)
    shadow_a = np.where(dist_sq <= 0.62, int(max_alpha * 0.68), shadow_a)
    shadow_a = np.where(dist_sq <= 0.28, int(max_alpha * 0.96), shadow_a)

    # Extra foot-contact spots right beneath opaque pixels at y = ground_y - 2 .. ground_y
    foot_band = out[max(0, ground_y - 2) : min(ch, ground_y + 1), x0:x1, 3] > 180
    if foot_band.any():
        foot_cols = np.any(foot_band, axis=0)
        for fy in range(max(y0, ground_y - 1), min(y1, ground_y + 2)):
            row_idx = fy - y0
            shadow_a[row_idx, foot_cols] = np.maximum(
                shadow_a[row_idx, foot_cols], int(max_alpha)
            )

    shadow_a = np.minimum(shadow_a, max_alpha)

    sub = out[y0:y1, x0:x1]
    empty = sub[:, :, 3] == 0
    sub[:, :, 0] = np.where(empty & (shadow_a > 0), 22, sub[:, :, 0])
    sub[:, :, 1] = np.where(empty & (shadow_a > 0), 16, sub[:, :, 1])
    sub[:, :, 2] = np.where(empty & (shadow_a > 0), 13, sub[:, :, 2])
    sub[:, :, 3] = np.where(empty & (shadow_a > 0), shadow_a, sub[:, :, 3])
    out[y0:y1, x0:x1] = sub

    # Enforce strict alpha cap on any semi-transparent pixels anywhere in the cell
    semi = (out[:, :, 3] > 0) & (out[:, :, 3] < 180)
    out[:, :, 3] = np.where(semi, np.minimum(out[:, :, 3], max_alpha), out[:, :, 3])
    return out


def deform_upper_lower(
    body_rgba: np.ndarray,
    bbox: Tuple[int, int, int, int],
    upper_dx: int = 0,
    upper_dy: int = 0,
    left_leg_dy: int = 0,
    right_leg_dy: int = 0,
    left_leg_dx: int = 0,
    right_leg_dx: int = 0,
    head_dy: int = 0,
    head_dx: int = 0,
) -> np.ndarray:
    """
    Articulates a character frame into Head/Snout, Torso, and Left/Right Legs/Hooves
    so every frame has genuine skeletal pixel motion without blurring.
    """
    ch, cw, _ = body_rgba.shape
    xmin, ymin, xmax, ymax = bbox
    h_span = max(8, ymax - ymin + 1)
    cx = (xmin + xmax) // 2

    head_cut = ymin + int(h_span * 0.38)
    leg_cut = ymin + int(h_span * 0.72)

    out = np.zeros_like(body_rgba)

    def blit_region(mask: np.ndarray, dx: int, dy: int):
        ys, xs = np.where(mask & (body_rgba[:, :, 3] > 128))
        if len(ys) == 0:
            return
        ny = np.clip(ys + dy, 0, ch - 1)
        nx = np.clip(xs + dx, 0, cw - 1)
        out[ny, nx] = body_rgba[ys, xs]

    yy, xx = np.mgrid[0:ch, 0:cw]
    is_legs = yy >= leg_cut
    is_left_leg = is_legs & (xx <= cx)
    is_right_leg = is_legs & (xx > cx)
    is_torso = (yy >= head_cut) & (yy < leg_cut)
    is_head = yy < head_cut

    blit_region(is_left_leg, left_leg_dx, left_leg_dy)
    blit_region(is_right_leg, right_leg_dx, right_leg_dy)
    blit_region(is_torso, upper_dx, upper_dy)
    blit_region(is_head, upper_dx + head_dx, upper_dy + head_dy)
    return out


def scale_collapse_pose(
    body_rgba: np.ndarray,
    bbox: Tuple[int, int, int, int],
    ground_y: int,
    sx: float,
    sy: float,
    darken: float = 1.0,
    dissolve_ratio: float = 0.0,
    seed: int = 0,
) -> np.ndarray:
    """Creates a genuine collapsed/sleeping/dying horizontal posture resting squarely on ground_y."""
    ch, cw, _ = body_rgba.shape
    xmin, ymin, xmax, ymax = bbox
    crop = body_rgba[ymin : ymax + 1, xmin : xmax + 1]
    if crop.size == 0:
        return body_rgba.copy()

    nw = max(6, min(cw - 4, int(round(crop.shape[1] * sx))))
    nh = max(4, min(ch - 4, int(round(crop.shape[0] * sy))))
    im_scaled = Image.fromarray(crop, "RGBA").resize((nw, nh), Image.Resampling.NEAREST)
    arr = np.array(im_scaled, dtype=np.uint8)

    if darken != 1.0:
        arr[:, :, :3] = np.clip(arr[:, :, :3].astype(np.float32) * darken, 0, 255).astype(np.uint8)

    if dissolve_ratio > 0.0:
        yy, xx = np.mgrid[0:nh, 0:nw]
        pattern = ((xx * 7 + yy * 13 + seed * 5) % 16) / 16.0
        erode = pattern < dissolve_ratio
        arr[erode] = 0

    out = np.zeros_like(body_rgba)
    cx = (xmin + xmax) // 2
    x0 = max(0, min(cw - nw, cx - nw // 2))
    y0 = max(0, min(ch - nh, ground_y - nh + 1))
    out[y0 : y0 + nh, x0 : x0 + nw] = arr
    return out


def tint_flash(body_rgba: np.ndarray, color: Tuple[int, int, int], strength: float) -> np.ndarray:
    out = body_rgba.copy()
    mask = out[:, :, 3] > 128
    for c in range(3):
        out[:, :, c] = np.where(
            mask,
            np.clip(out[:, :, c] * (1.0 - strength) + color[c] * strength, 0, 255),
            0,
        ).astype(np.uint8)
    return out


def draw_pixel_dots(
    cell_rgba: np.ndarray,
    points: List[Tuple[int, int, Tuple[int, int, int, int]]],
) -> None:
    ch, cw, _ = cell_rgba.shape
    for px, py, rgba in points:
        if 0 <= px < cw and 0 <= py < ch:
            cell_rgba[py, px] = rgba


def draw_slash_arc(
    cell_rgba: np.ndarray,
    row_idx: int,
    bbox: Tuple[int, int, int, int],
    phase: int,
    primary_rgb: Tuple[int, int, int] = (245, 248, 255),
    secondary_rgb: Tuple[int, int, int] = (255, 175, 75),
    heavy: bool = False,
) -> None:
    """Draws a crisp 2D pixel-art weapon/claw/beam smear arc in the facing direction."""
    ch, cw, _ = cell_rgba.shape
    xmin, ymin, xmax, ymax = bbox
    cx = (xmin + xmax) // 2
    cy = (ymin + ymax) // 2
    rx = max(8, (xmax - xmin) // 2 + (5 if heavy else 2))
    ry = max(6, (ymax - ymin) // 3 + (4 if heavy else 1))

    # Facing angle intervals based on row (0: down, 1: left, 2: up, 3: right)
    if row_idx == 0:
        ang_start, ang_end = (15, 135) if phase == 0 else (45, 165)
        ox, oy = cx, min(ch - 6, cy + 5)
    elif row_idx == 1:
        ang_start, ang_end = (115, 235) if phase == 0 else (135, 255)
        ox, oy = max(5, xmin + 2), cy
    elif row_idx == 2:
        ang_start, ang_end = (195, 315) if phase == 0 else (225, 345)
        ox, oy = cx, max(5, ymin + 4)
    else:
        ang_start, ang_end = (-55, 65) if phase == 0 else (-35, 85)
        ox, oy = min(cw - 6, xmax - 2), cy

    thickness = 3 if heavy else 2
    for deg in range(ang_start, ang_end + 1, 4):
        rad = math.radians(deg)
        for t in range(thickness):
            px = int(round(ox + (rx - t) * math.cos(rad)))
            py = int(round(oy + (ry - t) * math.sin(rad)))
            if 0 <= px < cw and 0 <= py < ch:
                col = primary_rgb if t == 0 else secondary_rgb
                cell_rgba[py, px] = (col[0], col[1], col[2], 255)


def load_directional_cells(png_path: str, cw: int, ch: int) -> List[List[np.ndarray]]:
    """Returns 4 rows x N cols of clean body cells (ch, cw, 4)."""
    im = Image.open(png_path).convert("RGBA")
    arr = np.array(im, dtype=np.uint8)
    cols = max(1, im.width // cw)
    rows_out: List[List[np.ndarray]] = []
    for r in range(4):
        r_cells = []
        for c in range(cols):
            cell = arr[r * ch : (r + 1) * ch, c * cw : (c + 1) * cw]
            r_cells.append(extract_clean_body(cell))
        rows_out.append(r_cells)
    return rows_out


def ensure_quadruped_4dir_foreshortening(rows_cells: List[List[np.ndarray]]) -> List[List[np.ndarray]]:
    """
    If Row 0 (Down) and Row 1 (Left) have nearly identical widths on a wide quadruped monster,
    foreshortens Row 0 (Down) and Row 2 (Up) horizontally to 0.82x width so turning Down/Up vs Left/Right
    visibly rotates the 3D volume in 2D space.
    """
    c_down = rows_cells[0][0]
    c_left = rows_cells[1][0]
    ys_d, xs_d = np.where(c_down[:, :, 3] > 128)
    ys_l, xs_l = np.where(c_left[:, :, 3] > 128)
    if len(xs_d) == 0 or len(xs_l) == 0:
        return rows_cells
    w_down = int(xs_d.max() - xs_d.min() + 1)
    w_left = int(xs_l.max() - xs_l.min() + 1)
    h_down = int(ys_d.max() - ys_d.min() + 1)

    if w_left > h_down * 0.95 and abs(w_down - w_left) <= 2:
        ch, cw, _ = c_down.shape
        for r in (0, 2):
            for c_idx, cell in enumerate(rows_cells[r]):
                ys, xs = np.where(cell[:, :, 3] > 128)
                if len(xs) == 0:
                    continue
                xmin, xmax = int(xs.min()), int(xs.max())
                ymin, ymax = int(ys.min()), int(ys.max())
                crop = cell[ymin : ymax + 1, xmin : xmax + 1]
                nw = max(8, int(round(crop.shape[1] * 0.82)))
                nh = crop.shape[0]
                im_s = Image.fromarray(crop, "RGBA").resize((nw, nh), Image.Resampling.NEAREST)
                s_arr = np.array(im_s, dtype=np.uint8)
                if r == 2:
                    # Dorsal/back shading for Up row
                    mask = s_arr[:, :, 3] > 128
                    s_arr[:, :, :3] = np.where(
                        mask[:, :, None],
                        np.clip(s_arr[:, :, :3].astype(np.float32) * 0.90, 0, 255).astype(np.uint8),
                        0,
                    )
                new_cell = np.zeros_like(cell)
                cx = (xmin + xmax) // 2
                x0 = max(0, min(cw - nw, cx - nw // 2))
                new_cell[ymin : ymin + nh, x0 : x0 + nw] = s_arr
                rows_cells[r][c_idx] = new_cell
    return rows_cells


def assemble_sheet(rows_cells: List[List[np.ndarray]], cw: int, ch: int) -> Image.Image:
    cols = len(rows_cells[0])
    out = np.zeros((4 * ch, cols * cw, 4), dtype=np.uint8)
    for r in range(4):
        for c in range(cols):
            out[r * ch : (r + 1) * ch, c * cw : (c + 1) * cw] = rows_cells[r][c]
    return Image.fromarray(out, "RGBA")


def save_clip_both_roots(
    folder: str,
    clip_name: str,
    im: Image.Image,
    meta: dict,
) -> None:
    for root in (FROM_ARGON_CHARS, ASSETS_CHARS):
        ddir = os.path.join(root, folder)
        os.makedirs(ddir, exist_ok=True)
        png_path = os.path.join(ddir, f"{folder}_{clip_name}.png")
        js_path = os.path.join(ddir, f"{folder}_{clip_name}.json")
        im.save(png_path, format="PNG")
        write_json(js_path, meta)


def build_pig_folder(folder: str) -> int:
    """Builds all 10+ rich 4-directional clips for a pig breed/stage folder with grounded contact shadows & warm-umber outline."""
    sdir = os.path.join(FROM_ARGON_CHARS, folder)
    cw, ch = 72, 68
    piv = [36, 5]
    ground_y = ch - piv[1]  # y = 63
    center_x = piv[0]       # x = 36
    max_shadow_a = 50       # strictly <= 52

    idle_src = load_directional_cells(os.path.join(sdir, f"{folder}_idle.png"), cw, ch)
    walk_src = load_directional_cells(os.path.join(sdir, f"{folder}_walk.png"), cw, ch)
    eat_src = load_directional_cells(os.path.join(sdir, f"{folder}_eat.png"), cw, ch)

    def finalize_pig_cell(body: np.ndarray, dy: int = 0, dx: int = 0) -> Tuple[np.ndarray, Tuple[int, int, int, int]]:
        aligned, bbox = align_body_to_ground(body, ground_y, center_x, dy_offset=dy, dx_offset=dx)
        outlined = apply_selective_warm_umber_outline(aligned, is_pig=True)
        return outlined, bbox

    clips_built = 0

    # 1. IDLE (4 frames, fps 16.0, loop=True: breathing + ear twitch + tail flick)
    idle_rows = []
    for r in range(4):
        row_out = []
        for c in range(4):
            base = idle_src[r][c % len(idle_src[r])]
            aligned, bbox = finalize_pig_cell(base)
            breath_dy = [0, -1, -1, 0][c]
            head_dy = [0, 0, -1, 0][c]
            posed = deform_upper_lower(aligned, bbox, upper_dy=breath_dy, head_dy=head_dy)
            posed = apply_selective_warm_umber_outline(posed, is_pig=True)
            row_out.append(add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a))
        idle_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "idle",
        assemble_sheet(idle_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:idle)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": True,
        },
    )
    clips_built += 1

    # 2. WALK (6 frames, fps 18.0, loop=True, stride_m_per_cycle=1.15)
    walk_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = walk_src[r][c % len(walk_src[r])]
            bob = [0, -1, 0, 0, -1, 0][c]
            l_dy = [-1, -2, 0, 0, 0, 0][c]
            r_dy = [0, 0, 0, -1, -2, 0][c]
            aligned, bbox = finalize_pig_cell(base)
            posed = deform_upper_lower(aligned, bbox, upper_dy=bob, left_leg_dy=l_dy, right_leg_dy=r_dy)
            posed = apply_selective_warm_umber_outline(posed, is_pig=True)
            row_out.append(add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a))
        walk_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "walk",
        assemble_sheet(walk_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:walk)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": True,
            "stride_m": 1.15,
            "stride_m_per_cycle": 1.15,
        },
    )
    clips_built += 1

    # 3. RUN (6 frames, fps 20.0, loop=True, stride_m_per_cycle=1.65: bounding gallop + dust puffs)
    run_rows = []
    dir_dx = [0, -2, 0, 2]
    for r in range(4):
        row_out = []
        for c in range(6):
            base = walk_src[r][c % len(walk_src[r])]
            hop_dy = [-1, -2, -1, 0, -2, 0][c]
            l_dx = [-1, -2, -1, 1, 2, 1][c]
            r_dx = [1, 2, 1, -1, -2, -1][c]
            aligned, bbox = finalize_pig_cell(base, dy=hop_dy, dx=dir_dx[r] // 2)
            posed = deform_upper_lower(
                aligned,
                bbox,
                upper_dx=dir_dx[r] // 2,
                upper_dy=hop_dy,
                left_leg_dx=l_dx,
                right_leg_dx=r_dx,
                head_dy=-1 if c in (1, 4) else 0,
            )
            posed = apply_selective_warm_umber_outline(posed, is_pig=True)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            # Kick up warm pasture dust behind hooves on frames 1, 2, 4, 5
            xmin, ymin, xmax, ymax = bbox
            dust_x = xmax + 2 if r == 1 else (xmin - 3 if r == 3 else center_x + (4 if c % 2 == 0 else -4))
            if c in (1, 2, 4, 5):
                draw_pixel_dots(
                    cell_s,
                    [
                        (dust_x, ground_y - (c % 3), (182, 152, 112, 255)),
                        (dust_x + 1, ground_y - 1 - (c % 2), (148, 118, 84, 255)),
                    ],
                )
            row_out.append(cell_s)
        run_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "run",
        assemble_sheet(run_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:run)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 20.0,
            "loop": True,
            "stride_m": 1.65,
            "stride_m_per_cycle": 1.65,
        },
    )
    clips_built += 1

    # 4. EAT (6 frames, fps 16.0, loop=True, effect_frame=2: head dipping into trough + chewing + feed crumbs)
    eat_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = eat_src[r][c % len(eat_src[r])]
            aligned, bbox = finalize_pig_cell(base)
            dip_dy = [1, 2, 3, 2, 3, 1][c]
            chew_dx = [0, -1, 1, -1, 1, 0][c]
            posed = deform_upper_lower(
                aligned,
                bbox,
                upper_dy=1 if c in (1, 2, 3, 4) else 0,
                head_dy=dip_dy,
                head_dx=chew_dx,
            )
            posed = apply_selective_warm_umber_outline(posed, is_pig=True)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            xmin, ymin, xmax, ymax = bbox
            snout_x = xmin - 1 if r == 1 else (xmax + 1 if r == 3 else center_x + chew_dx)
            snout_y = min(ground_y - 1, ymax - 3)
            if c in (1, 2, 3, 4):
                draw_pixel_dots(
                    cell_s,
                    [
                        (snout_x, snout_y, (232, 184, 72, 255)),
                        (snout_x + (1 if c % 2 == 0 else -1), snout_y + 1, (194, 138, 42, 255)),
                        (snout_x, snout_y - 1, (148, 96, 28, 255)),
                    ],
                )
            row_out.append(cell_s)
        eat_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "eat",
        assemble_sheet(eat_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:eat)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": True,
            "effect_frame": 2,
        },
    )
    clips_built += 1

    # 5. DRINK (6 frames, fps 16.0, loop=True, effect_frame=2: lapping water + tongue + water ripples/droplets)
    drink_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = eat_src[r][c % len(eat_src[r])]
            aligned, bbox = finalize_pig_cell(base)
            dip_dy = [1, 3, 3, 2, 3, 1][c]
            posed = deform_upper_lower(
                aligned,
                bbox,
                upper_dy=1 if c in (1, 2, 3, 4) else 0,
                head_dy=dip_dy,
            )
            posed = apply_selective_warm_umber_outline(posed, is_pig=True)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            xmin, ymin, xmax, ymax = bbox
            snout_x = xmin - 1 if r == 1 else (xmax + 1 if r == 3 else center_x)
            snout_y = min(ground_y - 1, ymax - 2)
            pts = []
            if c in (1, 3):
                pts.append((snout_x, snout_y, (238, 122, 138, 255)))  # lapping tongue pixel
            if c in (1, 2, 3, 4, 5):
                spread = (c % 3) + 1
                pts.extend(
                    [
                        (snout_x - spread, snout_y + 1, (104, 198, 236, 255)),
                        (snout_x + spread, snout_y + 1, (168, 228, 255, 255)),
                        (snout_x, snout_y + 2, (62, 142, 192, 255)),
                    ]
                )
            draw_pixel_dots(cell_s, pts)
            row_out.append(cell_s)
        drink_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "drink",
        assemble_sheet(drink_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:drink)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": True,
            "effect_frame": 2,
        },
    )
    clips_built += 1

    # 6. SLEEP (4 frames, fps 16.0, loop=True: curled resting posture on ground + gentle breathing + zZz motes)
    sleep_rows = []
    for r in range(4):
        row_out = []
        for c in range(4):
            base = idle_src[r][c % len(idle_src[r])]
            aligned, bbox = finalize_pig_cell(base)
            sx = [1.08, 1.12, 1.12, 1.08][c]
            sy = [0.76, 0.79, 0.78, 0.75][c]
            curled = scale_collapse_pose(aligned, bbox, ground_y, sx=sx, sy=sy, darken=0.96)
            curled = apply_selective_warm_umber_outline(curled, is_pig=True)
            _, c_bbox = align_body_to_ground(curled, ground_y, center_x)
            cell_s = add_grounded_contact_shadow(curled, ground_y, c_bbox, max_shadow_a)
            # Subtle sleeping zZz pixel motes rising from head
            xmin, ymin, xmax, ymax = c_bbox
            zx = xmin + 3 if r == 1 else (xmax - 3 if r == 3 else center_x + 4)
            zy = max(3, ymin - 2 - c)
            if c in (1, 2, 3):
                draw_pixel_dots(
                    cell_s,
                    [
                        (zx, zy, (188, 220, 248, 255)),
                        (zx + 1, zy, (188, 220, 248, 255)),
                        (zx, zy + 1, (124, 166, 212, 255)),
                        (zx + 1, zy + 1, (188, 220, 248, 255)),
                    ],
                )
            row_out.append(cell_s)
        sleep_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "sleep",
        assemble_sheet(sleep_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:sleep)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": True,
        },
    )
    clips_built += 1

    # 7. HAPPY (6 frames, fps 18.0, loop=False, effect_frame=2: joyful hop + ear flap + warm sparkle/heart motes)
    happy_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = idle_src[r][c % len(idle_src[r])]
            hop_dy = [0, -2, -4, -3, -1, 0][c]
            aligned, bbox = finalize_pig_cell(base, dy=hop_dy)
            posed = deform_upper_lower(
                aligned,
                bbox,
                head_dy=-1 if c in (1, 2, 3) else 0,
                left_leg_dy=1 if c in (2, 3) else 0,
                right_leg_dy=1 if c in (2, 3) else 0,
            )
            posed = apply_selective_warm_umber_outline(posed, is_pig=True)
            # Shadow stays grounded at ground_y=63 even while the pig hops!
            _, g_bbox = finalize_pig_cell(base, dy=0)
            cell_s = add_grounded_contact_shadow(posed, ground_y, g_bbox, max_shadow_a)
            xmin, ymin, xmax, ymax = bbox
            hy = max(3, ymin - 3)
            if c in (1, 2, 3, 4):
                draw_pixel_dots(
                    cell_s,
                    [
                        (center_x - 1, hy, (255, 118, 148, 255)),
                        (center_x + 1, hy, (255, 118, 148, 255)),
                        (center_x, hy + 1, (255, 88, 124, 255)),
                        (xmin + 2, hy + 2, (255, 224, 96, 255)),
                        (xmax - 2, hy + 1, (255, 240, 148, 255)),
                    ],
                )
            row_out.append(cell_s)
        happy_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "happy",
        assemble_sheet(happy_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:happy)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": False,
            "effect_frame": 2,
        },
    )
    clips_built += 1

    # 8. PANIC (6 frames, fps 20.0, loop=True, stride_m_per_cycle=1.85: scrambling sprint + sweat drops + dust)
    panic_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = walk_src[r][c % len(walk_src[r])]
            hop_dy = [-1, 0, -2, 0, -1, 0][c]
            jit_dx = [-1, 1, -1, 1, -1, 1][c]
            l_dy = [-2, 0, -1, -2, 0, -1][c]
            r_dy = [0, -2, 0, -1, -2, 0][c]
            aligned, bbox = finalize_pig_cell(base, dy=hop_dy, dx=jit_dx)
            posed = deform_upper_lower(
                aligned,
                bbox,
                upper_dy=hop_dy,
                left_leg_dy=l_dy,
                right_leg_dy=r_dy,
                head_dy=-1,
            )
            posed = apply_selective_warm_umber_outline(posed, is_pig=True)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            xmin, ymin, xmax, ymax = bbox
            sy = max(3, ymin - 1 + (c % 2))
            draw_pixel_dots(
                cell_s,
                [
                    (xmin - 1, sy, (118, 206, 255, 255)),
                    (xmax + 1, sy + 1, (192, 238, 255, 255)),
                    (center_x + jit_dx * 3, ground_y - 1, (176, 146, 108, 255)),
                ],
            )
            row_out.append(cell_s)
        panic_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "panic",
        assemble_sheet(panic_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:panic)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 20.0,
            "loop": True,
            "stride_m": 1.85,
            "stride_m_per_cycle": 1.85,
        },
    )
    clips_built += 1

    # 9. HIT (4 frames, fps 18.0, loop=False, effect_frame=1: flinch recoil + warm flash)
    hit_rows = []
    recoil_dx_by_row = [0, 2, 0, -2]
    recoil_dy_by_row = [-1, 0, 1, 0]
    for r in range(4):
        row_out = []
        for c in range(4):
            base = idle_src[r][c % len(idle_src[r])]
            scale_recoil = [1, 2, 1, 0][c]
            aligned, bbox = finalize_pig_cell(
                base,
                dy=recoil_dy_by_row[r] * scale_recoil,
                dx=recoil_dx_by_row[r] * scale_recoil,
            )
            if c == 0:
                aligned = tint_flash(aligned, (255, 238, 205), 0.30)
            elif c == 1:
                aligned = tint_flash(aligned, (255, 195, 145), 0.38)
            aligned = apply_selective_warm_umber_outline(aligned, is_pig=True)
            cell_s = add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a)
            if c in (1, 2):
                xmin, ymin, xmax, ymax = bbox
                draw_pixel_dots(
                    cell_s,
                    [
                        (center_x - 3, ymin + 3, (255, 220, 112, 255)),
                        (center_x + 3, ymin + 4, (255, 168, 72, 255)),
                    ],
                )
            row_out.append(cell_s)
        hit_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "hit",
        assemble_sheet(hit_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:hit)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": False,
            "effect_frame": 1,
        },
    )
    clips_built += 1

    # 10. DEATH (4 frames, fps 16.0, loop=False, effect_frame=2: stagger -> collapse onto side)
    death_rows = []
    for r in range(4):
        row_out = []
        for c in range(4):
            base = idle_src[r][0]
            aligned, bbox = finalize_pig_cell(base)
            sx = [1.02, 1.10, 1.22, 1.26][c]
            sy = [0.92, 0.76, 0.58, 0.54][c]
            dk = [0.96, 0.90, 0.84, 0.78][c]
            collapsed = scale_collapse_pose(aligned, bbox, ground_y, sx=sx, sy=sy, darken=dk)
            collapsed = apply_selective_warm_umber_outline(collapsed, is_pig=True)
            _, c_bbox = align_body_to_ground(collapsed, ground_y, center_x)
            row_out.append(add_grounded_contact_shadow(collapsed, ground_y, c_bbox, max_shadow_a))
        death_rows.append(row_out)
    death_im = assemble_sheet(death_rows, cw, ch)
    save_clip_both_roots(
        folder,
        "death",
        death_im,
        {
            "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:death)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": False,
            "effect_frame": 2,
        },
    )
    clips_built += 1

    if folder in ("pig_white_feet", "pig_white_foot"):
        save_clip_both_roots(
            folder,
            "play_dead",
            death_im,
            {
                "source": f"Argon 2.5D Grounded Pig Sheet ({folder}:play_dead)",
                "cell": [cw, ch],
                "rows": ROWS,
                "frames_per_row": 4,
                "pivot_px_from_bottom_left": piv,
                "fps": 16.0,
                "loop": False,
                "effect_frame": 2,
            },
        )
        clips_built += 1

    return clips_built


def build_npc_or_mercenary_folder(folder: str) -> int:
    """
    Builds all 10 rich 4-directional clips for an NPC, Specialist, or Mercenary folder:
    idle, idle_fidget_1, walk, run, work, talk, interact, attack, hit, death.
    Ensures grounded foot contact shadows (alpha <= 54) at pivot=[36, 5].
    """
    sdir = os.path.join(FROM_ARGON_CHARS, folder)
    cw, ch = 72, 84
    piv = [36, 5]
    ground_y = ch - piv[1]  # y = 79
    center_x = piv[0]       # x = 36
    max_shadow_a = 52       # strictly <= 54

    idle_src = load_directional_cells(os.path.join(sdir, f"{folder}_idle.png"), cw, ch)
    walk_src = load_directional_cells(os.path.join(sdir, f"{folder}_walk.png"), cw, ch)

    interact_path = os.path.join(sdir, f"{folder}_interact.png")
    attack_path = os.path.join(sdir, f"{folder}_attack.png")
    hit_path = os.path.join(sdir, f"{folder}_hit.png")
    death_path = os.path.join(sdir, f"{folder}_death.png")

    interact_src = load_directional_cells(interact_path, cw, ch) if os.path.exists(interact_path) else idle_src
    attack_src = load_directional_cells(attack_path, cw, ch) if os.path.exists(attack_path) else interact_src
    hit_src = load_directional_cells(hit_path, cw, ch) if os.path.exists(hit_path) else idle_src
    death_src = load_directional_cells(death_path, cw, ch) if os.path.exists(death_path) else idle_src

    def finalize_npc_cell(body: np.ndarray, dy: int = 0, dx: int = 0) -> Tuple[np.ndarray, Tuple[int, int, int, int]]:
        aligned, bbox = align_body_to_ground(body, ground_y, center_x, dy_offset=dy, dx_offset=dx)
        outlined = apply_selective_warm_umber_outline(aligned, is_pig=False)
        return outlined, bbox

    clips_built = 0

    # 1. IDLE (4 frames, fps 16.0, loop=True)
    idle_rows = []
    for r in range(4):
        row_out = []
        for c in range(4):
            base = idle_src[r][c % len(idle_src[r])]
            aligned, bbox = finalize_npc_cell(base)
            posed = deform_upper_lower(aligned, bbox, upper_dy=[0, -1, -1, 0][c])
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            row_out.append(add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a))
        idle_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "idle",
        assemble_sheet(idle_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:idle)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": True,
        },
    )
    clips_built += 1

    # 2. IDLE_FIDGET_1 (6 frames, fps 16.0, loop=False: wiping brow / checking ledger / adjusting tool)
    fidget_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            src_pool = interact_src[r] if c in (1, 2, 3, 4) else idle_src[r]
            base = src_pool[c % len(src_pool)]
            aligned, bbox = finalize_npc_cell(base)
            u_dy = [0, -1, -1, 0, -1, 0][c]
            h_dx = [0, 1, 1, -1, 0, 0][c]
            h_dy = [0, -1, 0, 1, 0, 0][c]
            posed = deform_upper_lower(aligned, bbox, upper_dy=u_dy, head_dx=h_dx, head_dy=h_dy)
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            if c in (2, 3):
                xmin, ymin, xmax, ymax = bbox
                gx = xmin + 2 if r == 1 else (xmax - 2 if r == 3 else center_x + 6)
                gy = ymin + 18
                draw_pixel_dots(
                    cell_s,
                    [
                        (gx, gy, (248, 228, 168, 255)),
                        (gx + 1, gy - 1, (255, 245, 205, 255)),
                    ],
                )
            row_out.append(cell_s)
        fidget_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "idle_fidget_1",
        assemble_sheet(fidget_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:idle_fidget_1)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": False,
        },
    )
    clips_built += 1

    # 3. WALK (6 frames, fps 18.0, loop=True, stride_m_per_cycle=1.31)
    walk_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = walk_src[r][c % len(walk_src[r])]
            aligned, bbox = finalize_npc_cell(base)
            row_out.append(add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a))
        walk_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "walk",
        assemble_sheet(walk_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:walk)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": True,
            "stride_m": 1.31,
            "stride_m_per_cycle": 1.31,
        },
    )
    clips_built += 1

    # 4. RUN (6 frames, fps 20.0, loop=True, stride_m_per_cycle=1.85)
    run_rows = []
    lean_dx = [0, -2, 0, 2]
    for r in range(4):
        row_out = []
        for c in range(6):
            base = walk_src[r][c % len(walk_src[r])]
            hop_dy = [-1, -2, 0, -1, -2, 0][c]
            l_dy = [-2, -1, 0, 0, 0, -1][c]
            r_dy = [0, 0, -1, -2, -1, 0][c]
            aligned, bbox = finalize_npc_cell(base, dy=hop_dy, dx=lean_dx[r] // 2)
            posed = deform_upper_lower(
                aligned,
                bbox,
                upper_dx=lean_dx[r] // 2,
                upper_dy=hop_dy,
                left_leg_dy=l_dy,
                right_leg_dy=r_dy,
            )
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            if c in (1, 4):
                xmin, ymin, xmax, ymax = bbox
                dx_dust = xmax + 2 if r == 1 else (xmin - 2 if r == 3 else center_x)
                draw_pixel_dots(
                    cell_s,
                    [
                        (dx_dust, ground_y - 1, (178, 148, 110, 255)),
                        (dx_dust + 1, ground_y - 2, (142, 114, 82, 255)),
                    ],
                )
            row_out.append(cell_s)
        run_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "run",
        assemble_sheet(run_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:run)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 20.0,
            "loop": True,
            "stride_m": 1.85,
            "stride_m_per_cycle": 1.85,
        },
    )
    clips_built += 1

    # 5. WORK (6 frames, fps 18.0, loop=True, effect_frame=3: forging / cooking / hammering / inspecting)
    work_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            src_pool = interact_src[r] if len(interact_src[r]) > 0 else idle_src[r]
            base = src_pool[c % len(src_pool)]
            aligned, bbox = finalize_npc_cell(base)
            u_dy = [0, -1, -2, 2, 1, 0][c]
            h_dy = [0, -1, -1, 2, 1, 0][c]
            u_dx = [0, 0, -1 if r == 1 else (1 if r == 3 else 0), -2 if r == 1 else (2 if r == 3 else 0), 0, 0][c]
            posed = deform_upper_lower(aligned, bbox, upper_dx=u_dx, upper_dy=u_dy, head_dy=h_dy)
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            if c in (2, 3, 4):
                xmin, ymin, xmax, ymax = bbox
                wx = xmin - 2 if r == 1 else (xmax + 2 if r == 3 else center_x + (3 if c == 3 else -3))
                wy = ymin + int((ymax - ymin) * 0.52)
                draw_pixel_dots(
                    cell_s,
                    [
                        (wx, wy, (255, 212, 92, 255)),
                        (wx + 1, wy - 1, (255, 244, 168, 255)),
                        (wx - 1, wy + 1, (238, 132, 54, 255)),
                    ],
                )
            row_out.append(cell_s)
        work_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "work",
        assemble_sheet(work_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:work)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": True,
            "effect_frame": 3,
        },
    )
    clips_built += 1

    # 6. TALK (6 frames, fps 16.0, loop=True: expressive hand gesture + head nod)
    talk_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            src_pool = interact_src[r] if c in (1, 2, 3, 4) else idle_src[r]
            base = src_pool[c % len(src_pool)]
            aligned, bbox = finalize_npc_cell(base)
            u_dy = [0, -1, 0, -1, 0, 0][c]
            h_dy = [0, 1, 0, -1, 1, 0][c]
            h_dx = [0, 1, 0, -1, 0, 0][c]
            posed = deform_upper_lower(aligned, bbox, upper_dy=u_dy, head_dy=h_dy, head_dx=h_dx)
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            row_out.append(add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a))
        talk_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "talk",
        assemble_sheet(talk_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:talk)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": True,
        },
    )
    clips_built += 1

    # 7. INTERACT (4 frames, fps 16.0, loop=False, effect_frame=2)
    interact_rows = []
    for r in range(4):
        row_out = []
        for c in range(4):
            base = interact_src[r][c % len(interact_src[r])]
            aligned, bbox = finalize_npc_cell(base)
            posed = deform_upper_lower(aligned, bbox, upper_dy=[0, -1, -1, 0][c], head_dy=[0, 1, -1, 0][c])
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            row_out.append(add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a))
        interact_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "interact",
        assemble_sheet(interact_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:interact)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": False,
            "effect_frame": 2,
        },
    )
    clips_built += 1

    # 8. ATTACK (6 frames, fps 20.0, loop=False, effect_frame=2: wind-up -> lunge + arc smear -> recovery)
    attack_rows = []
    lunge_dx = [0, -2, 0, 2]
    lunge_dy = [2, 0, -2, 0]
    for r in range(4):
        row_out = []
        for c in range(6):
            base = attack_src[r][c % len(attack_src[r])]
            step_scale = [-1, -1, 2, 1, 0, 0][c]
            aligned, bbox = finalize_npc_cell(
                base,
                dy=(lunge_dy[r] * step_scale) // 2,
                dx=(lunge_dx[r] * step_scale) // 2,
            )
            posed = deform_upper_lower(
                aligned,
                bbox,
                upper_dx=(lunge_dx[r] * step_scale) // 2,
                upper_dy=-1 if c in (1, 2) else 0,
            )
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            if c in (2, 3):
                draw_slash_arc(
                    cell_s,
                    r,
                    bbox,
                    phase=c - 2,
                    primary_rgb=(240, 248, 255),
                    secondary_rgb=(235, 178, 88),
                    heavy=folder.startswith("merc_"),
                )
            row_out.append(cell_s)
        attack_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "attack",
        assemble_sheet(attack_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:attack)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 20.0,
            "loop": False,
            "effect_frame": 2,
        },
    )
    clips_built += 1

    # 9. HIT (4 frames, fps 18.0, loop=False, effect_frame=1)
    hit_rows = []
    recoil_dx = [0, 2, 0, -2]
    for r in range(4):
        row_out = []
        for c in range(4):
            base = hit_src[r][c % len(hit_src[r])]
            rec = [1, 2, 1, 0][c]
            aligned, bbox = finalize_npc_cell(base, dx=recoil_dx[r] * rec)
            if c == 0:
                aligned = tint_flash(aligned, (255, 240, 210), 0.28)
            elif c == 1:
                aligned = tint_flash(aligned, (255, 185, 135), 0.35)
            aligned = apply_selective_warm_umber_outline(aligned, is_pig=False)
            row_out.append(add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a))
        hit_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "hit",
        assemble_sheet(hit_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:hit)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": False,
            "effect_frame": 1,
        },
    )
    clips_built += 1

    # 10. DEATH (4 frames, fps 16.0, loop=False, effect_frame=2)
    death_rows = []
    for r in range(4):
        row_out = []
        for c in range(4):
            if os.path.exists(death_path) and len(death_src[r]) == 4:
                base = death_src[r][c]
                aligned, bbox = finalize_npc_cell(base)
            else:
                base = idle_src[r][0]
                aligned, bbox = finalize_npc_cell(base)
                sx = [1.02, 1.08, 1.18, 1.24][c]
                sy = [0.92, 0.74, 0.56, 0.50][c]
                aligned = scale_collapse_pose(aligned, bbox, ground_y, sx=sx, sy=sy, darken=0.85)
                aligned = apply_selective_warm_umber_outline(aligned, is_pig=False)
                _, bbox = align_body_to_ground(aligned, ground_y, center_x)
            row_out.append(add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a))
        death_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "death",
        assemble_sheet(death_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded NPC Sheet ({folder}:death)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": False,
            "effect_frame": 2,
        },
    )
    clips_built += 1

    return clips_built


def build_monster_or_boss_folder(folder: str) -> int:
    """
    Builds all 9 rich 4-directional clips for a Monster, Raider, or Boss folder:
    idle, walk, move, run, attack, skill, charge, hit, death.
    fps: 18..24 with accurate effect_frame and grounded contact shadow (alpha <= 52).
    """
    sdir = os.path.join(FROM_ARGON_CHARS, folder)
    idle_js = json.load(open(os.path.join(sdir, f"{folder}_idle.json"), encoding="utf-8"))
    cw, ch = idle_js["cell"]
    piv = idle_js.get("pivot_px_from_bottom_left", [cw // 2, 6])
    ground_y = ch - piv[1]
    center_x = piv[0]
    is_hover = folder in HOVER_ENTITIES
    is_ufo = folder.startswith("ufo_")
    is_boss = "boss" in folder or cw >= 140
    max_shadow_a = 50  # strictly <= 52

    idle_src = ensure_quadruped_4dir_foreshortening(
        load_directional_cells(os.path.join(sdir, f"{folder}_idle.png"), cw, ch)
    )
    walk_src = ensure_quadruped_4dir_foreshortening(
        load_directional_cells(os.path.join(sdir, f"{folder}_walk.png"), cw, ch)
    )
    attack_src = ensure_quadruped_4dir_foreshortening(
        load_directional_cells(os.path.join(sdir, f"{folder}_attack.png"), cw, ch)
    )

    def finalize_monster_cell(body: np.ndarray, dy: int = 0, dx: int = 0) -> Tuple[np.ndarray, Tuple[int, int, int, int]]:
        aligned, bbox = align_body_to_ground(
            body, ground_y, center_x, dy_offset=dy, dx_offset=dx, is_hover=is_hover
        )
        outlined = apply_selective_warm_umber_outline(aligned, is_pig=False)
        return outlined, bbox

    clips_built = 0

    # 1. IDLE (4 frames, fps 18.0, loop=True)
    idle_rows = []
    for r in range(4):
        row_out = []
        for c in range(4):
            base = idle_src[r][c % len(idle_src[r])]
            bob = [0, -1, -2, -1][c] if is_hover else [0, -1, -1, 0][c]
            aligned, bbox = finalize_monster_cell(base, dy=bob if is_hover else 0)
            if not is_hover:
                aligned = deform_upper_lower(aligned, bbox, upper_dy=bob)
                aligned = apply_selective_warm_umber_outline(aligned, is_pig=False)
            row_out.append(add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a))
        idle_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "idle",
        assemble_sheet(idle_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Monster/Boss Sheet ({folder}:idle)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": True,
        },
    )
    clips_built += 1

    # 2. WALK & MOVE (6 frames, fps 18.0, loop=True, stride_m_per_cycle=1.4)
    walk_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = walk_src[r][c % len(walk_src[r])]
            bob = [0, -1, -2, 0, -1, 0][c] if is_hover else 0
            aligned, bbox = finalize_monster_cell(base, dy=bob)
            row_out.append(add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a))
        walk_rows.append(row_out)
    walk_im = assemble_sheet(walk_rows, cw, ch)
    for cname in ("walk", "move"):
        save_clip_both_roots(
            folder,
            cname,
            walk_im,
            {
                "source": f"Argon 2.5D Grounded Monster/Boss Sheet ({folder}:{cname})",
                "cell": [cw, ch],
                "rows": ROWS,
                "frames_per_row": 6,
                "pivot_px_from_bottom_left": piv,
                "fps": 18.0,
                "loop": True,
                "stride_m": 1.4,
                "stride_m_per_cycle": 1.4,
            },
        )
        clips_built += 1

    # 3. RUN (6 frames, fps 22.0, loop=True, stride_m_per_cycle=2.0)
    run_rows = []
    lean_dx = [0, -3, 0, 3]
    for r in range(4):
        row_out = []
        for c in range(6):
            base = walk_src[r][c % len(walk_src[r])]
            hop_dy = [-1, -2, -1, 0, -2, 0][c]
            aligned, bbox = finalize_monster_cell(base, dy=hop_dy, dx=lean_dx[r] // 2)
            posed = deform_upper_lower(
                aligned,
                bbox,
                upper_dx=lean_dx[r] // 2,
                upper_dy=hop_dy,
                left_leg_dy=[-2, 0, 0, -1, 0, 0][c],
                right_leg_dy=[0, 0, -1, -2, 0, 0][c],
            )
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            if c in (1, 2, 4, 5):
                xmin, ymin, xmax, ymax = bbox
                tx = xmax + 2 if r == 1 else (xmin - 2 if r == 3 else center_x)
                draw_pixel_dots(
                    cell_s,
                    [
                        (tx, ground_y - 1, (164, 138, 108, 255)),
                        (tx + 1, ground_y - 2, (122, 98, 76, 255)),
                    ],
                )
            row_out.append(cell_s)
        run_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "run",
        assemble_sheet(run_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Monster/Boss Sheet ({folder}:run)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 22.0,
            "loop": True,
            "stride_m": 2.0,
            "stride_m_per_cycle": 2.0,
        },
    )
    clips_built += 1

    # 4. ATTACK (6 frames, fps 22.0, loop=False, effect_frame=2: wind-up -> strike lunge + smear -> recovery)
    attack_rows = []
    lunge_dx = [0, -3, 0, 3]
    lunge_dy = [2, 0, -2, 0]
    primary_smear = (120, 238, 255) if is_ufo else ((255, 218, 128) if is_boss else (245, 248, 255))
    secondary_smear = (58, 168, 255) if is_ufo else ((255, 92, 68) if is_boss else (255, 148, 72))
    for r in range(4):
        row_out = []
        for c in range(6):
            base = attack_src[r][c % len(attack_src[r])]
            step_scale = [-1, -1, 2, 1, 0, 0][c]
            aligned, bbox = finalize_monster_cell(
                base,
                dy=(lunge_dy[r] * step_scale) // 2,
                dx=(lunge_dx[r] * step_scale) // 2,
            )
            cell_s = add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a)
            if c in (2, 3):
                draw_slash_arc(
                    cell_s,
                    r,
                    bbox,
                    phase=c - 2,
                    primary_rgb=primary_smear,
                    secondary_rgb=secondary_smear,
                    heavy=is_boss or is_ufo,
                )
            row_out.append(cell_s)
        attack_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "attack",
        assemble_sheet(attack_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Monster/Boss Sheet ({folder}:attack)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 22.0,
            "loop": False,
            "effect_frame": 2,
        },
    )
    clips_built += 1

    # 5. SKILL & CHARGE (6 frames, fps 21.0/22.0, loop=False, effect_frame=3: heavy special attack + shockwave/aura)
    skill_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = attack_src[r][c % len(attack_src[r])]
            surge = [-1, -2, 1, 3, 2, 0][c]
            hop = [1, 2, -2, -1, 0, 0][c]
            aligned, bbox = finalize_monster_cell(
                base,
                dy=hop + (lunge_dy[r] * surge) // 2,
                dx=(lunge_dx[r] * surge) // 2,
            )
            if c in (1, 2):
                aligned = tint_flash(aligned, (255, 210, 120) if not is_ufo else (140, 235, 255), 0.22)
            elif c == 3:
                aligned = tint_flash(aligned, (255, 145, 75) if not is_ufo else (95, 215, 255), 0.30)
            aligned = apply_selective_warm_umber_outline(aligned, is_pig=False)
            cell_s = add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a)
            if c in (2, 3, 4):
                draw_slash_arc(
                    cell_s,
                    r,
                    bbox,
                    phase=(c - 2) % 2,
                    primary_rgb=(255, 238, 145) if not is_ufo else (165, 248, 255),
                    secondary_rgb=(255, 88, 52) if not is_ufo else (68, 178, 255),
                    heavy=True,
                )
                # Ground shockwave ring / energy motes on effect frame 3
                xmin, ymin, xmax, ymax = bbox
                for k in range(-3, 4, 2):
                    sx_pt = max(2, min(cw - 3, center_x + k * 4))
                    sy_pt = max(2, min(ch - 3, ground_y - 2 - abs(k)))
                    draw_pixel_dots(
                        cell_s,
                        [(sx_pt, sy_pt, (255, 200, 92, 255) if not is_ufo else (112, 228, 255, 255))],
                    )
            row_out.append(cell_s)
        skill_rows.append(row_out)
    skill_im = assemble_sheet(skill_rows, cw, ch)
    for cname, fps_val in (("skill", 20.0), ("charge", 22.0)):
        save_clip_both_roots(
            folder,
            cname,
            skill_im,
            {
                "source": f"Argon 2.5D Grounded Monster/Boss Sheet ({folder}:{cname})",
                "cell": [cw, ch],
                "rows": ROWS,
                "frames_per_row": 6,
                "pivot_px_from_bottom_left": piv,
                "fps": fps_val,
                "loop": False,
                "effect_frame": 3,
            },
        )
        clips_built += 1

    # 6. HIT (4 frames, fps 20.0, loop=False, effect_frame=1: genuine recoil stagger + flash, never identical to idle)
    hit_rows = []
    rec_dx = [0, 3, 0, -3]
    rec_dy = [-2, 0, 2, 0]
    for r in range(4):
        row_out = []
        for c in range(4):
            base = idle_src[r][c % len(idle_src[r])]
            step = [1, 2, 1, 0][c]
            aligned, bbox = finalize_monster_cell(
                base,
                dy=(rec_dy[r] * step) // 2 + (1 if c == 1 else 0),
                dx=(rec_dx[r] * step) // 2,
            )
            if c == 0:
                aligned = tint_flash(aligned, (255, 245, 220), 0.36)
            elif c == 1:
                aligned = tint_flash(aligned, (255, 175, 115), 0.42)
            aligned = apply_selective_warm_umber_outline(aligned, is_pig=False)
            cell_s = add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a)
            if c in (0, 1):
                xmin, ymin, xmax, ymax = bbox
                draw_pixel_dots(
                    cell_s,
                    [
                        (center_x - 4, (ymin + ymax) // 2 - 3, (255, 232, 132, 255)),
                        (center_x + 4, (ymin + ymax) // 2 + 2, (255, 152, 68, 255)),
                    ],
                )
            row_out.append(cell_s)
        hit_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "hit",
        assemble_sheet(hit_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Monster/Boss Sheet ({folder}:hit)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 20.0,
            "loop": False,
            "effect_frame": 1,
        },
    )
    clips_built += 1

    # 7. DEATH (6 frames, fps 18.0, loop=False, effect_frame=3: stagger -> collapse onto ground -> dissolve)
    death_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = idle_src[r][0]
            aligned, bbox = finalize_monster_cell(base)
            sx = [1.02, 1.08, 1.16, 1.24, 1.26, 1.28][c]
            sy = [0.94, 0.80, 0.64, 0.50, 0.46, 0.42][c]
            dk = [0.95, 0.86, 0.76, 0.68, 0.60, 0.52][c]
            diss = [0.0, 0.0, 0.05, 0.16, 0.34, 0.52][c]
            collapsed = scale_collapse_pose(
                aligned, bbox, ground_y, sx=sx, sy=sy, darken=dk, dissolve_ratio=diss, seed=r * 7 + c
            )
            collapsed = apply_selective_warm_umber_outline(collapsed, is_pig=False)
            _, c_bbox = align_body_to_ground(collapsed, ground_y, center_x)
            cell_s = add_grounded_contact_shadow(
                collapsed, ground_y, c_bbox, max(18, max_shadow_a - c * 5)
            )
            if c in (2, 3, 4):
                xmin, ymin, xmax, ymax = c_bbox
                draw_pixel_dots(
                    cell_s,
                    [
                        (center_x - 3 + c, max(3, ymin - 2), (168, 142, 198, 255)),
                        (center_x + 3 - c, max(3, ymin - 4), (224, 168, 112, 255)),
                    ],
                )
            row_out.append(cell_s)
        death_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "death",
        assemble_sheet(death_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Monster/Boss Sheet ({folder}:death)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": False,
            "effect_frame": 3,
        },
    )
    clips_built += 1

    return clips_built


def build_pet_folder(folder: str) -> int:
    """
    Builds all 11 rich 4-directional clips for a Pet folder:
    idle, walk, run, happy, work, bark, attack, interact, sleep, hit, death.
    fps: 16..20 with grounded contact shadow (alpha <= 52) at pivot=[28, 5].
    """
    sdir = os.path.join(FROM_ARGON_CHARS, folder)
    cw, ch = 56, 56
    piv = [28, 5]
    ground_y = ch - piv[1]  # y = 51
    center_x = piv[0]       # x = 28
    max_shadow_a = 48       # strictly <= 52

    idle_src = load_directional_cells(os.path.join(sdir, f"{folder}_idle.png"), cw, ch)
    walk_src = load_directional_cells(os.path.join(sdir, f"{folder}_walk.png"), cw, ch)
    attack_src = load_directional_cells(os.path.join(sdir, f"{folder}_attack.png"), cw, ch)

    def finalize_pet_cell(body: np.ndarray, dy: int = 0, dx: int = 0) -> Tuple[np.ndarray, Tuple[int, int, int, int]]:
        aligned, bbox = align_body_to_ground(body, ground_y, center_x, dy_offset=dy, dx_offset=dx)
        outlined = apply_selective_warm_umber_outline(aligned, is_pig=False)
        return outlined, bbox

    clips_built = 0

    # 1. IDLE (4 frames, fps 16.0, loop=True)
    idle_rows = []
    for r in range(4):
        row_out = []
        for c in range(4):
            base = idle_src[r][c % len(idle_src[r])]
            aligned, bbox = finalize_pet_cell(base)
            posed = deform_upper_lower(aligned, bbox, upper_dy=[0, -1, -1, 0][c], head_dy=[0, 0, -1, 0][c])
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            row_out.append(add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a))
        idle_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "idle",
        assemble_sheet(idle_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pet Sheet ({folder}:idle)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 16.0,
            "loop": True,
        },
    )
    clips_built += 1

    # 2. WALK (6 frames, fps 18.0, loop=True, stride_m_per_cycle=1.1)
    walk_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = walk_src[r][c % len(walk_src[r])]
            aligned, bbox = finalize_pet_cell(base)
            row_out.append(add_grounded_contact_shadow(aligned, ground_y, bbox, max_shadow_a))
        walk_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "walk",
        assemble_sheet(walk_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pet Sheet ({folder}:walk)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": True,
            "stride_m": 1.1,
            "stride_m_per_cycle": 1.1,
        },
    )
    clips_built += 1

    # 3. RUN (6 frames, fps 20.0, loop=True, stride_m_per_cycle=1.65)
    run_rows = []
    dir_dx = [0, -2, 0, 2]
    for r in range(4):
        row_out = []
        for c in range(6):
            base = walk_src[r][c % len(walk_src[r])]
            hop_dy = [-1, -2, -1, 0, -2, 0][c]
            aligned, bbox = finalize_pet_cell(base, dy=hop_dy, dx=dir_dx[r] // 2)
            posed = deform_upper_lower(
                aligned,
                bbox,
                upper_dx=dir_dx[r] // 2,
                upper_dy=hop_dy,
                left_leg_dy=[-1, -2, 0, 0, 0, 0][c],
                right_leg_dy=[0, 0, 0, -1, -2, 0][c],
            )
            posed = apply_selective_warm_umber_outline(posed, is_pig=False)
            cell_s = add_grounded_contact_shadow(posed, ground_y, bbox, max_shadow_a)
            if c in (1, 4):
                xmin, ymin, xmax, ymax = bbox
                tx = xmax + 1 if r == 1 else (xmin - 1 if r == 3 else center_x)
                draw_pixel_dots(cell_s, [(tx, ground_y - 1, (176, 148, 112, 255))])
            row_out.append(cell_s)
        run_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "run",
        assemble_sheet(run_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pet Sheet ({folder}:run)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 20.0,
            "loop": True,
            "stride_m": 1.65,
            "stride_m_per_cycle": 1.65,
        },
    )
    clips_built += 1

    # 4. HAPPY (6 frames, fps 18.0, loop=False, effect_frame=2: cheerful jump + heart/sparkle motes)
    happy_rows = []
    for r in range(4):
        row_out = []
        for c in range(6):
            base = idle_src[r][c % len(idle_src[r])]
            hop_dy = [0, -2, -4, -3, -1, 0][c]
            aligned, bbox = finalize_pet_cell(base, dy=hop_dy)
            _, g_bbox = finalize_pet_cell(base, dy=0)
            cell_s = add_grounded_contact_shadow(aligned, ground_y, g_bbox, max_shadow_a)
            if c in (1, 2, 3, 4):
                xmin, ymin, xmax, ymax = bbox
                hy = max(2, ymin - 2)
                draw_pixel_dots(
                    cell_s,
                    [
                        (center_x - 1, hy, (255, 118, 148, 255)),
                        (center_x + 1, hy, (255, 118, 148, 255)),
                        (center_x, hy + 1, (255, 88, 124, 255)),
                        (xmin + 2, hy + 1, (255, 228, 102, 255)),
                    ],
                )
            row_out.append(cell_s)
        happy_rows.append(row_out)
    save_clip_both_roots(
        folder,
        "happy",
        assemble_sheet(happy_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pet Sheet ({folder}:happy)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": False,
            "effect_frame": 2,
        },
    )
    clips_built += 1

    # 5. WORK & BARK (6 frames, fps 18.0, effect_frame=2: herding stance / acoustic bark wave rings)
    work_rows = []
    bark_rows = []
    for r in range(4):
        w_out = []
        b_out = []
        for c in range(6):
            base = attack_src[r][c % len(attack_src[r])] if c in (1, 2, 3) else walk_src[r][c % len(walk_src[r])]
            aligned, bbox = finalize_pet_cell(base)
            # Work: herding scan
            w_pose = deform_upper_lower(
                aligned,
                bbox,
                upper_dy=[0, -1, 1, 1, -1, 0][c],
                head_dy=[0, 1, 2, 1, -1, 0][c],
            )
            w_pose = apply_selective_warm_umber_outline(w_pose, is_pig=False)
            w_cell = add_grounded_contact_shadow(w_pose, ground_y, bbox, max_shadow_a)
            w_out.append(w_cell)

            # Bark: chest puff + acoustic soundwave arcs
            b_pose = deform_upper_lower(
                aligned,
                bbox,
                upper_dy=[0, -1, -2, -1, 0, 0][c],
                head_dy=[0, -1, -2, -1, 0, 0][c],
            )
            b_pose = apply_selective_warm_umber_outline(b_pose, is_pig=False)
            b_cell = add_grounded_contact_shadow(b_pose, ground_y, bbox, max_shadow_a)
            if c in (1, 2, 3, 4):
                xmin, ymin, xmax, ymax = bbox
                bx = xmin - 2 - (c % 2) if r == 1 else (xmax + 2 + (c % 2) if r == 3 else center_x)
                by = ymin + 8
                draw_pixel_dots(
                    b_cell,
                    [
                        (bx, by - 1, (255, 228, 128, 255)),
                        (bx, by, (255, 244, 184, 255)),
                        (bx, by + 1, (255, 198, 88, 255)),
                    ],
                )
            b_out.append(b_cell)
        work_rows.append(w_out)
        bark_rows.append(b_out)

    save_clip_both_roots(
        folder,
        "work",
        assemble_sheet(work_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pet Sheet ({folder}:work)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": True,
            "effect_frame": 2,
        },
    )
    save_clip_both_roots(
        folder,
        "bark",
        assemble_sheet(bark_rows, cw, ch),
        {
            "source": f"Argon 2.5D Grounded Pet Sheet ({folder}:bark)",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 6,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0,
            "loop": False,
            "effect_frame": 2,
        },
    )
    clips_built += 2

    # 6. ATTACK, INTERACT, SLEEP, HIT, DEATH
    atk_rows = []
    int_rows = []
    slp_rows = []
    hit_rows = []
    dth_rows = []
    for r in range(4):
        a_out, i_out, s_out, h_out, d_out = [], [], [], [], []
        for c in range(4):
            a_base = attack_src[r][c % len(attack_src[r])]
            a_al, a_bb = finalize_pet_cell(a_base)
            a_cell = add_grounded_contact_shadow(a_al, ground_y, a_bb, max_shadow_a)
            if c in (1, 2):
                draw_slash_arc(a_cell, r, a_bb, phase=c - 1)
            a_out.append(a_cell)

            i_base = idle_src[r][c % len(idle_src[r])]
            i_al, i_bb = finalize_pet_cell(i_base, dy=-1 if c in (1, 2) else 0)
            i_out.append(add_grounded_contact_shadow(i_al, ground_y, i_bb, max_shadow_a))

            # Curled sleep pose
            s_curled = scale_collapse_pose(
                i_al, i_bb, ground_y, sx=[1.08, 1.12, 1.12, 1.08][c], sy=[0.74, 0.78, 0.76, 0.74][c], darken=0.96
            )
            s_curled = apply_selective_warm_umber_outline(s_curled, is_pig=False)
            _, s_bb = align_body_to_ground(s_curled, ground_y, center_x)
            s_cell = add_grounded_contact_shadow(s_curled, ground_y, s_bb, max_shadow_a)
            if c in (1, 2, 3):
                draw_pixel_dots(s_cell, [(center_x + 3, max(2, s_bb[1] - 2 - c), (188, 220, 248, 255))])
            s_out.append(s_cell)

            # Hit recoil
            h_al, h_bb = finalize_pet_cell(i_base, dx=([0, 2, 0, -2][r] * [1, 2, 1, 0][c]) // 2)
            if c in (0, 1):
                h_al = tint_flash(h_al, (255, 225, 185), 0.32)
            h_out.append(add_grounded_contact_shadow(h_al, ground_y, h_bb, max_shadow_a))

            # Death collapse
            d_col = scale_collapse_pose(
                i_al, i_bb, ground_y, sx=[1.02, 1.10, 1.20, 1.24][c], sy=[0.90, 0.72, 0.56, 0.50][c], darken=0.82
            )
            _, d_bb = align_body_to_ground(d_col, ground_y, center_x)
            d_out.append(add_grounded_contact_shadow(d_col, ground_y, d_bb, max_shadow_a))

        atk_rows.append(a_out)
        int_rows.append(i_out)
        slp_rows.append(s_out)
        hit_rows.append(h_out)
        dth_rows.append(d_out)

    for cname, rows_data, loop_flag, eff in (
        ("attack", atk_rows, False, 2),
        ("interact", int_rows, False, 2),
        ("sleep", slp_rows, True, -1),
        ("hit", hit_rows, False, 1),
        ("death", dth_rows, False, 2),
    ):
        meta = {
            "source": f"Argon 2.5D Grounded Pet Sheet ({folder}:{cname})",
            "cell": [cw, ch],
            "rows": ROWS,
            "frames_per_row": 4,
            "pivot_px_from_bottom_left": piv,
            "fps": 18.0 if cname in ("attack", "hit") else 16.0,
            "loop": loop_flag,
        }
        if eff >= 0:
            meta["effect_frame"] = eff
        save_clip_both_roots(folder, cname, assemble_sheet(rows_data, cw, ch), meta)
        clips_built += 1

    return clips_built


def render_verification_proof() -> None:
    """
    Renders a comprehensive high-resolution showcase image proving:
    1. Multi-clip turnaround (Down, Left, Up, Right) & action strips (idle, walk, run, eat, drink, sleep, happy, panic)
       for representative Pig Breeds (including previously floating pig_hu_the, pig_kim_tho, pig_hong_dien, pig_xich_mao).
    2. Multi-clip turnaround & action strips (idle, idle_fidget_1, walk, run, work, talk, attack, hit)
       for NPCs, Specialists & Mercenaries (ba_sau, ong_ton, tam_va, kien, merc_chot, merc_tu_troi).
    3. Multi-clip action strips (idle, walk, run, attack, skill, charge, hit, death)
       for Pets (pet_co, pet_giam_doc) and Monsters/Bosses (cho_suong, dau_linh_trom, mon_boss_thien_tai, ufo_mothership).
    4. Live 2.5D Terrain Grounding Composite on authentic Farm/Village terrain crops proving zero floating (lơ lửng),
       soft contact shadows (alpha <= 52..54), and crisp 1px warm-umber selective outlines.
    """
    W, H = 2240, 1720
    canvas = Image.new("RGBA", (W, H), (24, 20, 18, 255))
    draw = ImageDraw.Draw(canvas)

    try:
        font_title = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 24)
        font_sec = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 17)
        font_lbl = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 13)
        font_sm = ImageFont.truetype("C:/Windows/Fonts/consolab.ttf", 12)
    except Exception:
        font_title = ImageFont.load_default()
        font_sec = font_title
        font_lbl = font_title
        font_sm = font_title

    # Header banner
    draw.rectangle([0, 0, W, 56], fill=(38, 28, 22, 255))
    draw.line([0, 56, W, 56], fill=(212, 168, 84, 255), width=2)
    draw.text(
        (24, 15),
        "PIG TYCOON — WORLD ENTITIES MULTI-CLIP ANIMATION (16-24 FPS) & 2.5D TERRAIN GROUNDING PROOF",
        fill=(248, 228, 172, 255),
        font=font_title,
    )
    draw.text(
        (W - 690, 20),
        "20 Pigs (10 clips) | 27 NPCs/Mercs (10 clips) | 20 Monsters/Bosses (9 clips) | 7 Pets (11 clips)",
        fill=(188, 224, 168, 255),
        font=font_lbl,
    )

    # Helper to load terrain background tile or fallback warm pasture
    farm_bg_path = os.path.join(MAPS_DIR, "farm_preview_crop_x3.png")
    village_bg_path = os.path.join(MAPS_DIR, "village_preview_crop_x3.png")
    farm_bg = Image.open(farm_bg_path).convert("RGBA") if os.path.exists(farm_bg_path) else None
    village_bg = Image.open(village_bg_path).convert("RGBA") if os.path.exists(village_bg_path) else None

    def paste_terrain_patch(x: int, y: int, w: int, h: int, use_village: bool = False):
        src = village_bg if (use_village and village_bg is not None) else farm_bg
        if src is not None:
            cur_dx = 0
            while cur_dx < w:
                chunk_w = min(w - cur_dx, max(64, src.width - 64))
                sx = ((x + cur_dx) * 3) % max(1, src.width - chunk_w)
                sy = (y * 5) % max(1, src.height - h)
                patch = src.crop((sx, sy, sx + chunk_w, sy + h))
                arr = np.array(patch, dtype=np.float32)
                arr[:, :, :3] = arr[:, :, :3] * 0.85
                arr[:, :, 3] = 255
                canvas.paste(Image.fromarray(arr.astype(np.uint8), "RGBA"), (x + cur_dx, y))
                cur_dx += chunk_w
        else:
            draw.rectangle([x, y, x + w, y + h], fill=(92, 118, 64, 255))
        draw.rectangle([x, y, x + w, y + h], outline=(112, 88, 62, 255), width=1)

    def get_frame(folder: str, clip: str, row_idx: int, col_idx: int) -> Tuple[Image.Image, dict]:
        sdir = os.path.join(FROM_ARGON_CHARS, folder)
        js = json.load(open(os.path.join(sdir, f"{folder}_{clip}.json"), encoding="utf-8"))
        im = Image.open(os.path.join(sdir, f"{folder}_{clip}.png")).convert("RGBA")
        cw, ch = js["cell"]
        cols = js["frames_per_row"]
        c = col_idx % cols
        r = row_idx % 4
        return im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch)), js

    # SECTION 1: 18+ PIG BREEDS — 4-DIRECTIONAL TURNAROUND & 10-CLIP ACTION STRIPS ON PASTURE
    y_sec1 = 70
    draw.text(
        (24, y_sec1),
        "1. 18+ PIG BREEDS — 4-Directional Turnaround (Down/Left/Up/Right) & 10-Clip Action Strips (idle, walk, run, eat, drink, sleep, happy, panic, hit, death) @ 16-20 FPS",
        fill=(244, 208, 124, 255),
        font=font_sec,
    )
    pig_samples = ["pig_kim_tho", "pig_hong_dien", "pig_hu_the", "pig_xich_mao", "pig_lam_khe", "pig_tinh_quang"]
    pig_clips_show = [
        ("idle", 0, 1, "idle(D)"),
        ("walk", 1, 1, "walk(L)"),
        ("walk", 2, 2, "walk(U)"),
        ("run", 3, 2, "run(R)"),
        ("eat", 0, 2, "eat"),
        ("drink", 1, 2, "drink"),
        ("sleep", 0, 2, "sleep"),
        ("happy", 0, 2, "happy"),
        ("panic", 1, 2, "panic"),
        ("hit", 0, 1, "hit"),
        ("death", 0, 3, "death"),
    ]
    for p_idx, p_name in enumerate(pig_samples):
        ry = y_sec1 + 28 + p_idx * 76
        paste_terrain_patch(24, ry, W - 48, 70, use_village=False)
        draw.rectangle([24, ry, 188, ry + 70], fill=(32, 24, 18, 220))
        draw.text((32, ry + 14), p_name, fill=(255, 232, 168, 255), font=font_sec)
        draw.text((32, ry + 38), "piv=[36,5] shadow<=50", fill=(172, 224, 156, 255), font=font_sm)

        for c_idx, (cname, r_dir, f_idx, label) in enumerate(pig_clips_show):
            cx = 200 + c_idx * 182
            cell_im, js = get_frame(p_name, cname, r_dir, f_idx)
            # Draw ground baseline indicator tick
            gy = ry + 2 + (js["cell"][1] - js["pivot_px_from_bottom_left"][1])
            draw.line([cx + 14, gy, cx + 58, gy], fill=(255, 220, 120, 95), width=1)
            canvas.paste(cell_im, (cx, ry + 2), cell_im)
            draw.text(
                (cx + 68, ry + 18),
                f"{label}\n{int(js['fps'])}fps\n{js['frames_per_row']}f",
                fill=(242, 236, 220, 255),
                font=font_sm,
            )

    # SECTION 2: NPCs, SPECIALISTS & MERCENARIES — 10-CLIP STRIPS GROUNDED AT PIVOT=[36, 5]
    y_sec2 = y_sec1 + 28 + len(pig_samples) * 76 + 14
    draw.text(
        (24, y_sec2),
        "2. NPCs, SPECIALISTS & MERCENARIES — 4-Directional & 10-Clip Strips (idle, idle_fidget_1, walk, run, work, talk, interact, attack, hit, death) @ 16-20 FPS",
        fill=(244, 208, 124, 255),
        font=font_sec,
    )
    npc_samples = ["ba_sau", "ong_ton", "tam_va", "kien", "merc_chot", "merc_tu_troi"]
    npc_clips_show = [
        ("idle", 0, 1, "idle(D)"),
        ("idle_fidget_1", 0, 2, "fidget"),
        ("walk", 1, 1, "walk(L)"),
        ("walk", 2, 2, "walk(U)"),
        ("run", 3, 1, "run(R)"),
        ("work", 0, 3, "work"),
        ("talk", 0, 2, "talk"),
        ("interact", 1, 2, "interact"),
        ("attack", 3, 2, "attack"),
        ("hit", 0, 1, "hit"),
        ("death", 0, 3, "death"),
    ]
    for n_idx, n_name in enumerate(npc_samples):
        ry = y_sec2 + 28 + n_idx * 90
        paste_terrain_patch(24, ry, W - 48, 84, use_village=True)
        draw.rectangle([24, ry, 188, ry + 84], fill=(32, 24, 18, 220))
        draw.text((32, ry + 18), n_name, fill=(255, 232, 168, 255), font=font_sec)
        draw.text((32, ry + 44), "piv=[36,5] shadow<=52", fill=(172, 224, 156, 255), font=font_sm)

        for c_idx, (cname, r_dir, f_idx, label) in enumerate(npc_clips_show):
            cx = 200 + c_idx * 182
            cell_im, js = get_frame(n_name, cname, r_dir, f_idx)
            gy = ry + (js["cell"][1] - js["pivot_px_from_bottom_left"][1])
            draw.line([cx + 16, gy, cx + 56, gy], fill=(255, 220, 120, 95), width=1)
            canvas.paste(cell_im, (cx, ry), cell_im)
            draw.text(
                (cx + 70, ry + 22),
                f"{label}\n{int(js['fps'])}fps\n{js['frames_per_row']}f",
                fill=(242, 236, 220, 255),
                font=font_sm,
            )

    # SECTION 3: PETS, MONSTERS, RAIDERS & BOSSES — MULTI-CLIP COMBAT & WORK SHOWCASE
    y_sec3 = y_sec2 + 28 + len(npc_samples) * 90 + 14
    draw.text(
        (24, y_sec3),
        "3. PETS, MONSTERS, RAIDERS & BOSSES — Grounded Action Strips (idle, walk, run, happy/work/bark, attack, skill/charge, hit, death) @ 16-22 FPS",
        fill=(244, 208, 124, 255),
        font=font_sec,
    )

    # Row A: Pets & Standard Monsters
    row_a_y = y_sec3 + 28
    paste_terrain_patch(24, row_a_y, W - 48, 96, use_village=False)
    pet_mon_items = [
        ("pet_co", "idle", 0, 1, "pet_co:idle"),
        ("pet_co", "walk", 1, 2, "pet_co:walk"),
        ("pet_co", "happy", 0, 2, "pet_co:happy"),
        ("pet_co", "bark", 3, 2, "pet_co:bark"),
        ("pet_giam_doc", "work", 0, 2, "giam_doc:work"),
        ("pet_giam_doc", "sleep", 0, 2, "giam_doc:sleep"),
        ("cho_suong", "idle", 0, 1, "cho_suong:D"),
        ("cho_suong", "walk", 1, 2, "cho_suong:L"),
        ("cho_suong", "attack", 3, 2, "cho_suong:atk"),
        ("cho_suong", "skill", 3, 3, "cho_suong:skill"),
        ("cho_suong", "hit", 0, 1, "cho_suong:hit"),
        ("cho_suong", "death", 0, 4, "cho_suong:death"),
    ]
    for idx, (ename, cname, r_dir, f_idx, lbl) in enumerate(pet_mon_items):
        cx = 36 + idx * 180
        cell_im, js = get_frame(ename, cname, r_dir, f_idx)
        cw, ch = js["cell"]
        oy = row_a_y + max(4, 72 - ch)
        canvas.paste(cell_im, (cx, oy), cell_im)
        draw.rectangle([cx, row_a_y + 74, cx + 168, row_a_y + 93], fill=(24, 18, 14, 215))
        draw.text((cx + 4, row_a_y + 76), f"{lbl} ({int(js['fps'])}fps)", fill=(244, 228, 184, 255), font=font_sm)

    # Row B: Large Bosses & UFOs (mon_boss_thien_tai, thiet_giap_cong_thanh, ufo_mothership, dau_linh_trom)
    row_b_y = row_a_y + 104
    paste_terrain_patch(24, row_b_y, W - 48, 210, use_village=False)
    boss_items = [
        ("dau_linh_trom", "attack", 3, 2, "dau_linh_trom:attack"),
        ("dau_linh_trom", "skill", 0, 3, "dau_linh_trom:skill"),
        ("thiet_giap_cong_thanh", "walk", 1, 2, "thiet_giap:walk(L)"),
        ("thiet_giap_cong_thanh", "charge", 3, 3, "thiet_giap:charge(R)"),
        ("mon_boss_thien_tai", "idle", 0, 1, "boss_thien_tai:idle"),
        ("mon_boss_thien_tai", "attack", 1, 2, "boss_thien_tai:attack"),
        ("mon_boss_thien_tai", "skill", 0, 3, "boss_thien_tai:skill"),
        ("mon_boss_thien_tai", "death", 0, 4, "boss_thien_tai:death"),
        ("ufo_mothership", "attack", 0, 2, "ufo_mothership:attack"),
        ("ufo_mothership", "skill", 0, 3, "ufo_mothership:skill"),
    ]
    cur_x = 36
    for ename, cname, r_dir, f_idx, lbl in boss_items:
        cell_im, js = get_frame(ename, cname, r_dir, f_idx)
        cw, ch = js["cell"]
        oy = row_b_y + max(4, 182 - ch)
        canvas.paste(cell_im, (cur_x, oy), cell_im)
        box_w = max(cw, 156)
        draw.rectangle([cur_x, row_b_y + 186, cur_x + box_w, row_b_y + 206], fill=(24, 18, 14, 220))
        draw.text(
            (cur_x + 4, row_b_y + 188),
            f"{lbl} [{int(js['fps'])}fps]",
            fill=(255, 224, 152, 255),
            font=font_sm,
        )
        cur_x += box_w + 18

    os.makedirs(os.path.dirname(PROOF_OUT_BRAIN), exist_ok=True)
    os.makedirs(os.path.dirname(PROOF_OUT_COLLAB), exist_ok=True)
    canvas.save(PROOF_OUT_BRAIN, format="PNG")
    canvas.save(PROOF_OUT_COLLAB, format="PNG")
    print(f"Saved visual proof to:\n  - {PROOF_OUT_BRAIN}\n  - {PROOF_OUT_COLLAB}")


def main() -> None:
    print("=== Building World Entities Multi-Clip Animations & 2.5D Grounding ===")
    ensure_kien_seed_folder()

    pig_clips = 0
    for pf in PIG_FOLDERS:
        pig_clips += build_pig_folder(pf)
    print(f"  [Pigs] Processed {len(PIG_FOLDERS)} folders -> {pig_clips} multi-directional sheets + JSONs.")

    npc_clips = 0
    for nf in NPC_MERC_FOLDERS:
        npc_clips += build_npc_or_mercenary_folder(nf)
    print(f"  [NPCs/Mercs] Processed {len(NPC_MERC_FOLDERS)} folders -> {npc_clips} multi-directional sheets + JSONs.")

    mon_clips = 0
    for mf in MONSTER_BOSS_FOLDERS:
        mon_clips += build_monster_or_boss_folder(mf)
    print(f"  [Monsters/Bosses] Processed {len(MONSTER_BOSS_FOLDERS)} folders -> {mon_clips} multi-directional sheets + JSONs.")

    pet_clips = 0
    for ptf in PET_FOLDERS:
        pet_clips += build_pet_folder(ptf)
    print(f"  [Pets] Processed {len(PET_FOLDERS)} folders -> {pet_clips} multi-directional sheets + JSONs.")

    total_folders = len(PIG_FOLDERS) + len(NPC_MERC_FOLDERS) + len(MONSTER_BOSS_FOLDERS) + len(PET_FOLDERS)
    total_clips = pig_clips + npc_clips + mon_clips + pet_clips
    print(f"=== Total Upgraded: {total_folders} non-hero entity folders, {total_clips} clips synced to both roots ===")

    render_verification_proof()


if __name__ == "__main__":
    main()
