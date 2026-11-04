#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""
D:\Pig-tycoon\tools\build_an_khoa_150_martial_weapon_clips.py
=============================================================
Builds the complete 150-clip 80px 2.5D Martial, Weapon-Archetype, and Farm Interaction
animation suite for BOTH `an` (and `argon_an`) and `khoa`, plus synchronized 80px weapon
overlay sheets (`{an,khoa}__wpn_{dao_rua,giao_moc,bua_ta,no_tay}*`).

Key Guarantees:
1. Full 150 clips (`*.png` + `*.json` + `*.meta`) for both `an` and `khoa` in:
   - `.collab/claude-2d/ui_and_map_sync/from_argon/Assets/Art/PixelMyth/Characters/{an,khoa,argon_an}/`
   - `Assets/Art/PixelMyth/Characters/{an,khoa,argon_an}/`
2. Native 80px character height (`y=[4..83]`, cell `72x84`, `pivot=[36, 5]`, `32 PPU`) across all 150 clips
   for both `an` (from the approved 1 AM OptionC2_NavySilver 80px sheets) and `khoa` (upgrading all 94
   previously 60px/63px skill sheets to full 80px height).
3. Genuine multi-frame kinematic body articulation across all 4 directions (`down`, `left`, `up`, `right`):
   - Distinct character martial feel (`an`: swift, agile, high-flexibility rotational & aerial grace;
     `khoa`: grounded, wide-stance, forceful shoulder coil & deep lunge).
   - Distinct weapon physics across all 4 weapon archetypes (`slash` Dao Rựa, `thrust` Giáo Móc,
     `smash` Búa Tạ, `ranged` Nỏ Tay).
   - Distinct school & skill visual signatures + kinematic curves across all 23 active skills in
     `MartialSkillCatalog` (Schools A, B, C, D).
   - Distinct per-frame farm interaction animations (`interact_feed`, `interact_water`, `interact_clean`,
     `interact_repair`, `interact_gather`, `interact_carry`, `interact_observe`, `whistle`, `horn`)
     with smooth `18-24` FPS (`24-30` FPS for attacks/skills) and exact `effect_frame = frames // 2`.
4. Synchronized weapon overlay sheets (`{an,khoa}__wpn_{dao_rua,giao_moc,bua_ta,no_tay}{,__II,__III,__IV}`)
   so weapon layers never desync during basic attacks, 3-step combos, or martial skills.
5. Updates `.collab/claude-2d/argon-deliverables/LOCKED/an/sha256.txt` with all 150 `an` clips.
"""

import hashlib
import json
import math
import os
import shutil
import sys
import time
import uuid
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, r"D:\Pig-tycoon\tools\pixelart")
import qa_enforcer

ROOT = Path(r"D:\Pig-tycoon")
FROM_ARGON = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon"
FA_CHARS = FROM_ARGON / "Assets" / "Art" / "PixelMyth" / "Characters"
ROOT_CHARS = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters"
OPT_C2_DIR = ROOT_CHARS / "an_candidates_v13" / "OptionC2_NavySilver"
LOCKED_AN_DIR = ROOT / ".collab" / "claude-2d" / "argon-deliverables" / "LOCKED" / "an"

CW, CH = 72, 84
PIVOT = [36, 5]
ROWS = ["down", "left", "up", "right"]

ACTIVE_SKILLS = [
    # School A: TocBo_A (Speed / Dash / Afterimage)
    "bo_phap_luot",
    "doan_anh_tram",
    "xuyen_bay",
    "cuu_nguy",
    "tiep_vien_toc_hanh",
    "vo_anh_bo",
    # School B: TrongKich_B (Heavy Impact / Seismic / Armor Break)
    "pha_giap",
    "chan_dia",
    "tram_tuyen",
    "phan_chan",
    "neo_kich",
    "thien_chuy",
    # School C: KhongChe_C (Control / Hook / Net / Formation)
    "moc_keo",
    "an_tin_hieu",
    "xoay_bay",
    "troi_luoi",
    "dung_chot",
    "thien_la",
    # School D: SinhTon_D (Fortitude / Shield / Taunt / Bastion)
    "tru_bo",
    "ho_than",
    "khieu_chien",
    "chan_mang",
    "kim_cuong_the",
]

# Detailed per-skill school, kinematic motion profile, FPS, and visual signature
SKILL_PROFILES = {
    # School A (TocBo_A) — Cyan-Silver / Azure Wind & Afterimages
    "bo_phap_luot":      ("A", "dash_blitz",     28.0, (64, 224, 255),  (228, 252, 255)),
    "doan_anh_tram":     ("A", "triple_flurry",  30.0, (52, 212, 255),  (235, 250, 255)),
    "xuyen_bay":         ("A", "pierce_glide",   28.0, (76, 232, 245),  (220, 255, 255)),
    "cuu_nguy":          ("A", "leap_rescue",    28.0, (68, 238, 214),  (232, 255, 248)),
    "tiep_vien_toc_hanh":("A", "warp_flash",     28.0, (92, 198, 255),  (240, 248, 255)),
    "vo_anh_bo":         ("A", "mirage_split",   30.0, (80, 228, 255),  (245, 252, 255)),

    # School B (TrongKich_B) — Molten-Gold / Crimson Seismic Force
    "pha_giap":          ("B", "crush_break",    25.0, (255, 132, 48),  (255, 232, 132)),
    "chan_dia":          ("B", "seismic_stomp",  24.0, (242, 158, 44),  (255, 238, 148)),
    "tram_tuyen":        ("B", "wide_sweep",     25.0, (255, 108, 52),  (255, 224, 128)),
    "phan_chan":         ("B", "parry_riposte",  26.0, (255, 184, 56),  (255, 246, 176)),
    "neo_kich":          ("B", "impale_pin",     25.0, (238, 96, 48),   (255, 214, 118)),
    "thien_chuy":        ("B", "meteor_drop",    24.0, (255, 88, 44),   (255, 242, 156)),

    # School C (KhongChe_C) — Emerald-Jade / Amber Tactical Control
    "moc_keo":           ("C", "hook_yank",      26.0, (52, 218, 148),  (238, 212, 96)),
    "an_tin_hieu":       ("C", "sky_mark",       26.0, (248, 196, 64),  (132, 248, 184)),
    "xoay_bay":          ("C", "vortex_cone",    26.0, (64, 228, 164),  (228, 255, 214)),
    "troi_luoi":         ("C", "net_cast",       26.0, (76, 214, 136),  (246, 218, 108)),
    "dung_chot":         ("C", "stake_drive",    25.0, (186, 142, 68),  (112, 238, 164)),
    "thien_la":          ("C", "grand_array",    26.0, (58, 232, 156),  (255, 232, 124)),

    # School D (SinhTon_D) — Imperial Gold / Jade Bastion Aegis
    "tru_bo":            ("D", "root_stance",    24.0, (232, 188, 68),  (255, 242, 168)),
    "ho_than":           ("D", "dome_aegis",     25.0, (104, 232, 184), (255, 238, 148)),
    "khieu_chien":       ("D", "roar_taunt",     25.0, (255, 164, 64),  (255, 238, 156)),
    "chan_mang":         ("D", "wing_intercept", 26.0, (248, 206, 76),  (164, 248, 208)),
    "kim_cuong_the":     ("D", "diamond_body",   25.0, (255, 224, 92),  (255, 252, 212)),
}

WEAPON_ITEMS = [
    ("wpn_dao_rua", "slash"),
    ("wpn_giao_moc", "thrust"),
    ("wpn_bua_ta", "smash"),
    ("wpn_no_tay", "ranged"),
]


def write_unity_png_meta(png_path: Path, ppu: int = 32) -> None:
    meta_path = Path(str(png_path) + ".meta")
    if meta_path.exists():
        return
    guid = hashlib.md5(f"pigtycoon_char_{png_path.parent.name}_{png_path.name}".encode("utf-8")).hexdigest()[:32]
    sprite_id = uuid.uuid5(uuid.NAMESPACE_DNS, png_path.name).hex[:32]
    content = f"""fileFormatVersion: 2
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
  isReadable: 1
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
  spritePivot: {{x: 0.5, y: 0.05952381}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignoreMasterTextureLimit: 0
  applyToNormalMap: 0
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
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    bindPoses: []
  spritePackingTag: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    meta_path.write_text(content, encoding="utf-8")


def write_unity_json_meta(json_path: Path) -> None:
    meta_path = Path(str(json_path) + ".meta")
    if meta_path.exists():
        return
    guid = hashlib.md5(f"pigtycoon_json_{json_path.parent.name}_{json_path.name}".encode("utf-8")).hexdigest()[:32]
    content = f"""fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    meta_path.write_text(content, encoding="utf-8")


def rotate_patch_nearest(patch: np.ndarray, angle_deg: float, center_xy: tuple) -> np.ndarray:
    if abs(angle_deg) < 0.8:
        return patch.copy()
    pil = Image.fromarray(patch, "RGBA")
    rot = pil.rotate(
        -angle_deg,
        resample=Image.Resampling.NEAREST,
        expand=False,
        center=(int(center_xy[0]), int(center_xy[1])),
    )
    return np.array(rot, dtype=np.uint8)


def alpha_paste_inplace(dst: np.ndarray, src: np.ndarray) -> None:
    m = src[:, :, 3] > 128
    dst[m] = src[m]


def shift_body_keep_shadow(cell: np.ndarray, dx: int, dy: int, foot_keep: int = 2) -> np.ndarray:
    """Shifts the opaque character body (`alpha == 255`) by `(dx, dy)` while keeping the ground shadow
    (`0 < alpha < 255`) anchored at `y=78..83`, preserving full 80px height."""
    if dx == 0 and dy == 0:
        return cell.copy()
    shadow_mask = (cell[:, :, 3] > 0) & (cell[:, :, 3] < 255)
    body_mask = cell[:, :, 3] == 255
    out = np.zeros_like(cell)
    out[shadow_mask] = cell[shadow_mask]

    body_only = np.zeros_like(cell)
    body_only[body_mask] = cell[body_mask]

    ys = np.where(body_mask)[0]
    if len(ys) > 0:
        # Prevent pushing feet off bottom of 84px cell or cutting top head off
        max_dy = max(0, (CH - foot_keep - 1) - int(ys.max()))
        min_dy = min(0, 1 - int(ys.min()))
        dy = max(min_dy, min(dy, max_dy))

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
        out_sub = out[y_dst0:y_dst1, x_dst0:x_dst1]
        out_sub[m] = sub[m]
    return out


def articulate_character_cell(
    cell: np.ndarray,
    row_idx: int,
    dx: int,
    dy: int,
    torso_deg: float,
    arm_dx: int,
    arm_dy: int,
    stride_spread: int = 0,
) -> np.ndarray:
    """Applies multi-part anatomical articulation to an 80px character cell:
    1. Leg stride spread (`stride_spread`) on lower legs (`y=56..79`) for dynamic combat lunges/crouches.
    2. Upper torso rotation (`torso_deg`) around the waist/hip pivot `(cx_b, y_hip)`.
    3. Striking shoulder/arm vertical & horizontal swing (`arm_dx, arm_dy`).
    4. Full-body lunge/recoil translation (`dx, dy`) while keeping the ground shadow anchored."""
    work = cell.copy()
    bm = work[:, :, 3] == 255
    bm[80:, :] = False
    ys, xs = np.where(bm)
    if len(xs) == 0:
        return work

    y_top, y_bot = int(ys.min()), int(ys.max())
    bh = max(24, y_bot - y_top + 1)
    y_hip = y_top + int(bh * 0.56)
    cx_b = int(round(0.5 * (int(xs.min()) + int(xs.max()))))

    # 1. Optional lower-leg stance widening / lunging split
    if stride_spread != 0 and y_hip + 4 < y_bot:
        leg_zone = work[y_hip + 2 : y_bot + 1, :].copy()
        work[y_hip + 2 : y_bot + 1, :] = 0
        lz_h = leg_zone.shape[0]
        left_half = leg_zone[:, :cx_b].copy()
        right_half = leg_zone[:, cx_b:].copy()
        new_legs = np.zeros_like(leg_zone)
        s_px = max(-3, min(3, int(stride_spread)))
        if row_idx in (0, 2):
            # Widen stance horizontally in front/back views
            if s_px > 0:
                new_legs[:, : max(0, cx_b - s_px)] = left_half[:, s_px:]
                new_legs[:, min(CW, cx_b + s_px) :] = right_half[:, : CW - cx_b - s_px]
            else:
                ap = -s_px
                new_legs[:, ap:cx_b] = left_half[:, : cx_b - ap]
                new_legs[:, cx_b : CW - ap] = right_half[:, ap:]
        else:
            # Separate front & rear leg along facing axis in side views
            if s_px > 0:
                new_legs[:, : max(0, cx_b - s_px)] = left_half[:, s_px:]
                new_legs[:, min(CW, cx_b + s_px) :] = right_half[:, : CW - cx_b - s_px]
            else:
                new_legs[:] = leg_zone
        # Keep any ground shadow untouched
        sh_zone = cell[y_hip + 2 : y_bot + 1, :]
        sh_m = (sh_zone[:, :, 3] > 0) & (sh_zone[:, :, 3] < 255)
        work[y_hip + 2 : y_bot + 1, :][sh_m] = sh_zone[sh_m]
        leg_m = new_legs[:, :, 3] == 255
        work[y_hip + 2 : y_bot + 1, :][leg_m] = new_legs[leg_m]

    # 2. Upper torso rotation + striking arm articulation
    if abs(torso_deg) >= 1.0 or arm_dx != 0 or arm_dy != 0:
        upper = np.zeros_like(work)
        upper[: min(CH, y_hip + 2), :] = work[: min(CH, y_hip + 2), :]
        lower_and_sh = work.copy()
        lower_and_sh[:y_hip, :] = 0

        rot_u = rotate_patch_nearest(upper, torso_deg, (cx_b, y_hip))

        if arm_dx != 0 or arm_dy != 0:
            y_a0 = max(0, y_top + int(bh * 0.24))
            y_a1 = min(CH, y_top + int(bh * 0.54))
            ah = y_a1 - y_a0
            if ah > 4:
                if row_idx == 0:
                    # Right-hand side of screen or left-hand side of screen striking arm
                    x_split = max(4, cx_b - 2)
                    sub_a = rot_u[y_a0:y_a1, :x_split].copy()
                    rot_u[y_a0:y_a1, :x_split] = 0
                    yd = max(0, min(CH - ah, y_a0 + arm_dy))
                    xd = max(0, min(CW - x_split, arm_dx))
                    m_a = sub_a[:, :, 3] > 128
                    rot_u[yd : yd + ah, xd : xd + x_split][m_a] = sub_a[m_a]
                elif row_idx == 2:
                    x_split = min(CW - 4, cx_b + 2)
                    sub_a = rot_u[y_a0:y_a1, x_split:].copy()
                    rot_u[y_a0:y_a1, x_split:] = 0
                    aw = CW - x_split
                    yd = max(0, min(CH - ah, y_a0 + arm_dy))
                    xd = max(0, min(CW - aw, x_split + arm_dx))
                    m_a = sub_a[:, :, 3] > 128
                    rot_u[yd : yd + ah, xd : xd + aw][m_a] = sub_a[m_a]
                elif row_idx == 1:
                    x_split = max(6, cx_b + 1)
                    sub_a = rot_u[y_a0:y_a1, :x_split].copy()
                    rot_u[y_a0:y_a1, :x_split] = 0
                    yd = max(0, min(CH - ah, y_a0 + arm_dy))
                    xd = max(0, min(CW - x_split, arm_dx))
                    m_a = sub_a[:, :, 3] > 128
                    rot_u[yd : yd + ah, xd : xd + x_split][m_a] = sub_a[m_a]
                else:
                    x_split = min(CW - 6, cx_b - 1)
                    sub_a = rot_u[y_a0:y_a1, x_split:].copy()
                    rot_u[y_a0:y_a1, x_split:] = 0
                    aw = CW - x_split
                    yd = max(0, min(CH - ah, y_a0 + arm_dy))
                    xd = max(0, min(CW - aw, x_split + arm_dx))
                    m_a = sub_a[:, :, 3] > 128
                    rot_u[yd : yd + ah, xd : xd + aw][m_a] = sub_a[m_a]

        alpha_paste_inplace(lower_and_sh, rot_u)
        work = lower_and_sh

    # 3. Shift full articulated body while keeping ground shadow anchored
    return shift_body_keep_shadow(work, dx, dy, foot_keep=1)


def draw_weapon_archetype_arc(
    dr: ImageDraw.ImageDraw,
    row_idx: int,
    archetype: str,
    step: int,
    phase_idx: int,
    c_main: tuple,
    c_hi: tuple,
    c_out: tuple = (22, 20, 28, 255),
    is_an: bool = False,
) -> None:
    """Draws crisp 32-PPU 2.5D weapon strike arcs, spear thrusts/hooks, hammer slams, or crossbow bolts
    tailored to the weapon archetype (`slash`, `thrust`, `smash`, `ranged`), combo step (`1..3`),
    frame phase (`phase_idx` in `0..2`), and hero (`an` agile twin-edge vs `khoa` heavy broad-arc)."""
    # Offset slightly by phase_idx (0=entry smear, 1=peak impact, 2=follow-through trail)
    p_shift = (phase_idx - 1) * 3

    if archetype == "slash":
        # Nimble curved machete/blade crescent arc
        if row_idx == 0:
            if step == 2:
                pts = [
                    (58, 36 + p_shift),
                    (48, 56 + p_shift),
                    (24, 60 + p_shift),
                    (12, 44 + p_shift),
                    (24, 52 + p_shift),
                    (46, 48 + p_shift),
                ]
            else:
                pts = [
                    (12, 36 + p_shift),
                    (24, 56 + p_shift),
                    (48, 60 + p_shift),
                    (60, 44 + p_shift),
                    (48, 52 + p_shift),
                    (26, 48 + p_shift),
                ]
            dr.polygon(pts, fill=c_main, outline=c_out)
            dr.line(pts[:4], fill=c_hi, width=2 if phase_idx == 1 else 1)
            if step == 3 or is_an:
                # Secondary upper wind-blade crescent for An's swift technique or Step 3 finisher
                pts2 = [(16, 26), (36, 18), (56, 28), (44, 23), (26, 23)]
                dr.polygon(pts2, fill=c_hi, outline=c_out)
        elif row_idx == 2:
            pts = [
                (14, 34 - p_shift),
                (26, 16 - p_shift),
                (48, 16 - p_shift),
                (58, 34 - p_shift),
                (46, 24 - p_shift),
                (26, 24 - p_shift),
            ]
            dr.polygon(pts, fill=c_main, outline=c_out)
            dr.line(pts[:4], fill=c_hi, width=2 if phase_idx == 1 else 1)
        elif row_idx == 1:
            pts = [
                (22, 24 + p_shift),
                (8, 36 + p_shift),
                (6, 54 + p_shift),
                (22, 62 + p_shift),
                (14, 50 + p_shift),
                (16, 36 + p_shift),
            ]
            dr.polygon(pts, fill=c_main, outline=c_out)
            dr.line(pts[:4], fill=c_hi, width=2 if phase_idx == 1 else 1)
        else:
            pts = [
                (50, 24 + p_shift),
                (64, 36 + p_shift),
                (66, 54 + p_shift),
                (50, 62 + p_shift),
                (58, 50 + p_shift),
                (56, 36 + p_shift),
            ]
            dr.polygon(pts, fill=c_main, outline=c_out)
            dr.line(pts[:4], fill=c_hi, width=2 if phase_idx == 1 else 1)

    elif archetype == "thrust":
        # Long-reach piercing spear lance + barbed hook pull-back on phase_idx == 2
        reach = 6 if phase_idx == 1 else (2 if phase_idx == 0 else -3)
        if row_idx == 0:
            tip_y = min(82, 68 + reach)
            dr.polygon([(32, 42), (40, 42), (36, tip_y)], fill=c_main, outline=c_out)
            dr.line([(36, 42), (36, tip_y - 2)], fill=c_hi, width=2)
            # Hooked barb on side of spearhead
            dr.polygon([(38, tip_y - 8), (46, tip_y - 13), (39, tip_y - 3)], fill=c_hi, outline=c_out)
        elif row_idx == 2:
            tip_y = max(4, 14 - reach)
            dr.polygon([(32, 38), (40, 38), (36, tip_y)], fill=c_main, outline=c_out)
            dr.line([(36, 38), (36, tip_y + 2)], fill=c_hi, width=2)
            dr.polygon([(38, tip_y + 8), (45, tip_y + 13), (39, tip_y + 3)], fill=c_hi, outline=c_out)
        elif row_idx == 1:
            tip_x = max(2, 8 - reach)
            dr.polygon([(28, 40), (28, 48), (tip_x, 44)], fill=c_main, outline=c_out)
            dr.line([(28, 44), (tip_x + 2, 44)], fill=c_hi, width=2)
            dr.polygon([(tip_x + 8, 41), (tip_x + 13, 34), (tip_x + 4, 41)], fill=c_hi, outline=c_out)
        else:
            tip_x = min(70, 64 + reach)
            dr.polygon([(44, 40), (44, 48), (tip_x, 44)], fill=c_main, outline=c_out)
            dr.line([(44, 44), (tip_x - 2, 44)], fill=c_hi, width=2)
            dr.polygon([(tip_x - 8, 41), (tip_x - 13, 34), (tip_x - 4, 41)], fill=c_hi, outline=c_out)

    elif archetype == "smash":
        # Heavy overhead sledgehammer cleave + ground shockwave crest
        if row_idx == 0:
            pts = [(12, 26), (10, 48), (20, 62), (52, 62), (56, 54), (24, 54), (18, 42)]
            dr.polygon(pts, fill=c_main, outline=c_out)
            if phase_idx >= 1:
                dr.rectangle([16, 60, 56, 66], fill=c_hi, outline=c_out)
                # Flying seismic rock shards
                dr.rectangle([10, 55, 14, 59], fill=c_main, outline=c_out)
                dr.rectangle([58, 55, 62, 59], fill=c_main, outline=c_out)
        elif row_idx == 2:
            pts = [(12, 38), (10, 20), (22, 12), (52, 12), (54, 19), (26, 19), (18, 28)]
            dr.polygon(pts, fill=c_main, outline=c_out)
            if phase_idx >= 1:
                dr.rectangle([18, 10, 54, 16], fill=c_hi, outline=c_out)
        elif row_idx == 1:
            pts = [(28, 14), (10, 24), (6, 54), (22, 62), (25, 54), (14, 48), (18, 26)]
            dr.polygon(pts, fill=c_main, outline=c_out)
            if phase_idx >= 1:
                dr.rectangle([4, 56, 26, 63], fill=c_hi, outline=c_out)
        else:
            pts = [(44, 14), (62, 24), (66, 54), (50, 62), (47, 54), (58, 48), (54, 26)]
            dr.polygon(pts, fill=c_main, outline=c_out)
            if phase_idx >= 1:
                dr.rectangle([46, 56, 68, 63], fill=c_hi, outline=c_out)

    elif archetype == "ranged":
        # Crossbow bow prod + high-velocity streaking bolt & muzzle flash
        bolt_travel = phase_idx * 6
        if row_idx == 0:
            dr.rectangle([26, 45, 46, 49], fill=c_main, outline=c_out)
            by = min(78, 52 + bolt_travel)
            dr.polygon([(33, by), (39, by), (36, min(83, by + 14))], fill=c_hi, outline=c_out)
        elif row_idx == 2:
            dr.rectangle([26, 30, 46, 34], fill=c_main, outline=c_out)
            by = max(14, 28 - bolt_travel)
            dr.polygon([(33, by), (39, by), (36, max(2, by - 14))], fill=c_hi, outline=c_out)
        elif row_idx == 1:
            dr.rectangle([20, 36, 25, 52], fill=c_main, outline=c_out)
            bx = max(14, 20 - bolt_travel)
            dr.polygon([(bx, 41), (bx, 47), (max(2, bx - 14), 44)], fill=c_hi, outline=c_out)
        else:
            dr.rectangle([47, 36, 52, 52], fill=c_main, outline=c_out)
            bx = min(58, 52 + bolt_travel)
            dr.polygon([(bx, 41), (bx, 47), (min(70, bx + 14), 44)], fill=c_hi, outline=c_out)


def draw_skill_signature_motif(
    cell: np.ndarray,
    row_idx: int,
    skill_key: str,
    archetype: str,
    frame_idx: int,
    n_frames: int,
    is_an: bool,
) -> None:
    """Draws the unmistakable school-colored 2.5D martial skill visual motif AND weapon-archetype
    combination trajectory on active action frames (`frame_idx` in `1..n_frames-2`)."""
    school, motion_type, _, col_main_rgb, col_hi_rgb = SKILL_PROFILES.get(
        skill_key, ("A", "dash_blitz", 26.0, (72, 224, 255), (235, 250, 255))
    )
    # Slightly differentiate An (cooler silver/jade luminance) vs Khoa (warmer gold/bronze punch)
    if is_an:
        c_main = (
            min(255, int(col_main_rgb[0] * 0.92 + 12)),
            min(255, int(col_main_rgb[1] * 1.02 + 8)),
            min(255, int(col_main_rgb[2] * 1.05 + 12)),
            255,
        )
    else:
        c_main = (
            min(255, int(col_main_rgb[0] * 1.05 + 10)),
            min(255, int(col_main_rgb[1] * 0.96 + 4)),
            max(0, int(col_main_rgb[2] * 0.90)),
            255,
        )
    c_hi = (col_hi_rgb[0], col_hi_rgb[1], col_hi_rgb[2], 255)
    c_out = (20, 18, 28, 255)

    pil = Image.fromarray(cell, "RGBA")
    dr = ImageDraw.Draw(pil)

    # 1. Draw the weapon-archetype strike trajectory (tinted with the skill's school colors!)
    if frame_idx in (2, 3, 4, 5):
        eff_arch = archetype if archetype in ("slash", "thrust", "smash", "ranged") else (
            "slash" if school == "A" else ("smash" if school == "B" else ("thrust" if school == "C" else "slash"))
        )
        step_eq = 3 if frame_idx in (4, 5) else (2 if frame_idx == 3 else 1)
        draw_weapon_archetype_arc(
            dr,
            row_idx,
            eff_arch,
            step_eq,
            min(2, max(0, frame_idx - 2)),
            c_main,
            c_hi,
            c_out,
            is_an=is_an,
        )

    # 2. Draw the skill-specific martial signature motif so all 23 skills look unmistakably distinct
    if frame_idx in (2, 3, 4, 5, 6):
        prog = frame_idx - 2  # 0..4
        if motion_type == "dash_blitz":
            # Speed wind streaks behind & around feet
            for k in range(3):
                yy = 34 + k * 14
                x0 = 8 + prog * 2 if row_idx != 1 else 44 - prog * 2
                dr.rectangle([x0, yy, x0 + 14, yy + 3], fill=c_hi if k == 1 else c_main, outline=c_out)
        elif motion_type == "triple_flurry":
            # Triple cascading crescent blades (1-2-3 flurry)
            y_off = 22 + (prog % 3) * 14
            dr.polygon([(12, y_off), (36, y_off - 8), (60, y_off), (46, y_off + 5), (26, y_off + 5)], fill=c_hi, outline=c_out)
        elif motion_type == "pierce_glide":
            # V-shaped wind-cutter wings slicing outward
            dr.polygon([(36, 20), (10, 46 + prog * 2), (18, 50 + prog * 2), (36, 30), (54, 50 + prog * 2), (62, 46 + prog * 2)], fill=c_main, outline=c_out)
        elif motion_type == "leap_rescue":
            # Guardian rescue halo & grappling cord arc
            dr.arc([14, 14, 58, 58], start=180, end=360, fill=c_hi, width=3)
            dr.ellipse([20, 62, 52, 74], outline=c_main, width=2)
        elif motion_type == "warp_flash":
            # Vertical lightning-cyan teleportation pillar & 4-point star crest
            for x_b in (18, 54):
                dr.rectangle([x_b - 2, 10, x_b + 2, 72], fill=c_hi, outline=c_out)
            dr.polygon([(36, 6), (40, 14), (48, 16), (40, 18), (36, 26), (32, 18), (24, 16), (32, 14)], fill=c_hi, outline=c_out)
        elif motion_type == "mirage_split":
            # Twin phantom afterimage chevrons flanking left & right
            for sx in (10, 56):
                dr.polygon([(sx, 22), (sx + 6, 38), (sx, 56), (sx - 4, 38)], fill=c_main, outline=c_out)
                dr.line([(sx, 24), (sx, 54)], fill=c_hi, width=1)
        elif motion_type == "crush_break":
            # Armor-shattering crimson-gold X-fracture impact crest
            cx_i = 20 if row_idx == 1 else (52 if row_idx == 3 else 36)
            cy_i = 52 if row_idx == 0 else (26 if row_idx == 2 else 44)
            dr.line([(cx_i - 10, cy_i - 10), (cx_i + 10, cy_i + 10)], fill=c_hi, width=3)
            dr.line([(cx_i + 10, cy_i - 10), (cx_i - 10, cy_i + 10)], fill=c_main, width=3)
        elif motion_type == "seismic_stomp":
            # Concentric expanding seismic ground rings & upward earth spikes
            rx = 16 + prog * 4
            dr.ellipse([36 - rx, 66 - prog, 36 + rx, 78 + prog], outline=c_hi, width=2)
            for sx in (16, 36, 56):
                dr.polygon([(sx - 3, 72), (sx + 3, 72), (sx, 58 - prog * 2)], fill=c_main, outline=c_out)
        elif motion_type == "wide_sweep":
            # 6m-wide horizontal line-sweep wave bar across cell width
            wy = 56 + prog * 2 if row_idx != 2 else 22 - prog * 2
            dr.rectangle([6, wy - 3, 66, wy + 3], fill=c_main, outline=c_out)
            dr.line([8, wy, 64, wy], fill=c_hi, width=2)
        elif motion_type == "parry_riposte":
            # Hexagonal golden parry spark flash + counter-slash
            dr.polygon([(36, 22), (48, 30), (48, 44), (36, 52), (24, 44), (24, 30)], outline=c_hi, width=2)
            dr.line([(20, 26), (52, 50)], fill=c_main, width=2)
        elif motion_type == "impale_pin":
            # Heavy anchoring spikes pinning down into the earth
            for sx in (22, 36, 50):
                dr.polygon([(sx - 3, 46), (sx + 3, 46), (sx, 72)], fill=c_main, outline=c_out)
                dr.line([(sx, 48), (sx, 68)], fill=c_hi, width=1)
        elif motion_type == "meteor_drop":
            # Ultimate 400% Thiên Chùy: overhead meteor hammer crest + ground crater explosion
            dr.polygon([(36, 6), (48, 20), (42, 34), (30, 34), (24, 20)], fill=c_main, outline=c_out)
            dr.rectangle([12, 64, 60, 72], fill=c_hi, outline=c_out)
        elif motion_type == "hook_yank":
            # Emerald-amber grappling chain & barbed claw pulling inward
            cx_h = 14 if row_idx == 1 else (58 if row_idx == 3 else 36)
            cy_h = 66 if row_idx == 0 else (16 if row_idx == 2 else 44)
            dr.line([(36, 42), (cx_h, cy_h)], fill=c_main, width=3)
            dr.polygon([(cx_h - 5, cy_h), (cx_h + 5, cy_h), (cx_h, cy_h - 6)], fill=c_hi, outline=c_out)
        elif motion_type == "sky_mark":
            # Tactical targeting sigil / diamond flare above & ahead
            dr.polygon([(36, 8), (46, 18), (36, 28), (26, 18)], outline=c_hi, fill=c_main)
            dr.line([(36, 4), (36, 32)], fill=c_hi, width=2)
            dr.line([(22, 18), (50, 18)], fill=c_hi, width=2)
        elif motion_type == "vortex_cone":
            # Swirling 60-degree cone vortex arcs
            dr.arc([12, 24, 60, 68], start=20, end=160, fill=c_main, width=3)
            dr.arc([18, 30, 54, 62], start=200, end=340, fill=c_hi, width=2)
        elif motion_type == "net_cast":
            # Woven emerald-amber capture net lattice
            dr.rectangle([18, 46, 54, 68], outline=c_main, width=2)
            for nx in (26, 36, 46):
                dr.line([(nx, 46), (nx, 68)], fill=c_hi, width=1)
            for ny in (52, 60):
                dr.line([(18, ny), (54, ny)], fill=c_hi, width=1)
        elif motion_type == "stake_drive":
            # 3 upright wooden/jade-runed barricade stakes (`Dựng Chốt`)
            for sx in (18, 36, 54):
                dr.rectangle([sx - 3, 48, sx + 3, 72], fill=c_main, outline=c_out)
                dr.polygon([(sx - 4, 48), (sx + 4, 48), (sx, 40)], fill=c_hi, outline=c_out)
        elif motion_type == "grand_array":
            # Ultimate Thiên La: octagonal emerald-gold ground formation array
            dr.ellipse([10, 58, 62, 78], outline=c_main, width=2)
            dr.polygon([(36, 56), (54, 62), (60, 68), (54, 74), (36, 78), (18, 74), (12, 68), (18, 62)], outline=c_hi)
        elif motion_type == "root_stance":
            # Immovable Trụ Bộ: golden-earth root pillars anchoring feet
            dr.rectangle([16, 62, 24, 76], fill=c_main, outline=c_out)
            dr.rectangle([48, 62, 56, 76], fill=c_main, outline=c_out)
            dr.ellipse([14, 68, 58, 78], outline=c_hi, width=2)
        elif motion_type == "dome_aegis":
            # Hộ Thân: golden-jade protective barrier dome brackets around torso
            dr.arc([12, 16, 60, 68], start=190, end=350, fill=c_hi, width=3)
            dr.arc([14, 20, 58, 66], start=10, end=170, fill=c_main, width=2)
        elif motion_type == "roar_taunt":
            # Khiêu Chiến: expanding sonic war-cry rings radiating outward
            r_s = 18 + prog * 4
            dr.arc([36 - r_s, 36 - r_s // 2, 36 + r_s, 36 + r_s // 2], start=0, end=360, fill=c_hi, width=2)
        elif motion_type == "wing_intercept":
            # Chắn Mạng: wide golden guardian wing-shields flanking both sides
            dr.polygon([(8, 28), (22, 22), (20, 54), (10, 48)], fill=c_main, outline=c_out)
            dr.polygon([(64, 28), (50, 22), (52, 54), (62, 48)], fill=c_main, outline=c_out)
            dr.line([(12, 30), (18, 50)], fill=c_hi, width=2)
            dr.line([(60, 30), (54, 50)], fill=c_hi, width=2)
        elif motion_type == "diamond_body":
            # Ultimate Kim Cương Thể: radiant diamond-body crystal facets around hero
            dr.polygon([(36, 8), (58, 38), (36, 68), (14, 38)], outline=c_hi, width=2)
            dr.polygon([(36, 14), (52, 38), (36, 62), (20, 38)], outline=c_main, width=2)

    cell[:] = np.array(pil, dtype=np.uint8)


def draw_farm_interaction_prop(
    cell: np.ndarray,
    row_idx: int,
    clip_name: str,
    frame_idx: int,
    n_frames: int,
    dy: int,
    is_an: bool,
) -> None:
    """Draws clear 32-PPU handheld farm tools, grain arcs, water streams, hygiene sparkles,
    repair hammer sparks, burlap carry sacks, observation lens/notebook, or herding horn waves."""
    pil = Image.fromarray(cell, "RGBA")
    dr = ImageDraw.Draw(pil)
    c_out = (24, 20, 28, 255)
    hx = 22 if row_idx == 1 else (50 if row_idx == 3 else (44 if row_idx == 0 else 28))
    hy = 46 + dy
    dir_x = -1 if row_idx == 1 else (1 if row_idx == 3 else (1 if row_idx == 0 else -1))

    if clip_name in ("interact_feed", "interact"):
        # Wooden grain scoop bowl + golden feed kernels arc on frames 2..6
        dr.rectangle([hx - 4, hy - 2, hx + 4, hy + 4], fill=(178, 118, 58, 255), outline=c_out)
        dr.rectangle([hx - 3, hy - 4, hx + 3, hy - 1], fill=(252, 214, 88, 255), outline=c_out)
        if 2 <= frame_idx <= 6:
            for k in range(3):
                kx = hx + dir_x * (5 + (frame_idx - 2) * 2 + k * 3)
                ky = hy - 2 + k * 4 + (frame_idx - 3) * 2
                if 4 <= kx < CW - 4 and 8 <= ky < CH - 6:
                    dr.rectangle([kx - 1, ky - 1, kx + 2, ky + 2], fill=(255, 224, 96, 255), outline=c_out)

    elif clip_name == "interact_water":
        # Wooden/azure water bucket + cascading water stream on frames 2..6
        dr.rectangle([hx - 4, hy - 1, hx + 4, hy + 7], fill=(72, 152, 224, 255), outline=c_out)
        dr.line([hx - 3, hy, hx + 3, hy], fill=(212, 246, 255, 255), width=2)
        if 2 <= frame_idx <= 6:
            sx = hx + dir_x * 5
            sy0 = hy + 3
            sy1 = min(76, sy0 + 10 + (frame_idx - 2) * 3)
            dr.rectangle([sx - 2, sy0, sx + 2, sy1], fill=(88, 196, 255, 255), outline=c_out)
            dr.ellipse([sx - 5, sy1 - 2, sx + 5, sy1 + 3], fill=(196, 244, 255, 255), outline=c_out)

    elif clip_name == "interact_clean":
        # Long-handled bamboo broom sweeping side-to-side + bright hygiene bubbles
        sweep = int(round(math.sin(2.0 * math.pi * frame_idx / n_frames) * 5))
        bx = hx + sweep
        dr.line([(hx, hy - 10), (bx, 68)], fill=(168, 118, 58, 255), width=3)
        dr.polygon([(bx - 6, 64), (bx + 6, 64), (bx + 8, 72), (bx - 8, 72)], fill=(228, 192, 108, 255), outline=c_out)
        if 2 <= frame_idx <= 6:
            dr.ellipse([bx + dir_x * 6 - 3, 56, bx + dir_x * 6 + 3, 62], fill=(224, 252, 255, 255), outline=(64, 184, 232, 255))

    elif clip_name == "interact_repair":
        # Iron/wooden repair mallet striking up and down + bright anvil sparks on frames 3..5
        swing_y = -6 if frame_idx in (1, 2) else (4 if frame_idx in (3, 4, 5) else 0)
        mx, my = hx + dir_x * 3, hy + swing_y
        dr.line([(hx, hy + 2), (mx, my - 6)], fill=(156, 104, 54, 255), width=2)
        dr.rectangle([mx - 4, my - 10, mx + 5, my - 4], fill=(184, 196, 212, 255), outline=c_out)
        if frame_idx in (3, 4, 5):
            for sx_o, sy_o in ((6, -8), (10, -2), (5, 4)):
                dr.rectangle([mx + dir_x * sx_o - 1, my + sy_o - 1, mx + dir_x * sx_o + 2, my + sy_o + 2], fill=(255, 232, 96, 255), outline=c_out)

    elif clip_name == "interact_gather":
        # Reaching down to gather glowing golden-emerald farm herbs/materials
        gy = min(74, 58 + dy)
        dr.polygon([(hx - 4, gy), (hx, gy - 7), (hx + 4, gy)], fill=(84, 224, 136, 255), outline=c_out)
        if frame_idx in (3, 4, 5):
            dr.rectangle([hx - 2, gy - 10, hx + 2, gy - 6], fill=(255, 232, 112, 255), outline=c_out)

    elif clip_name == "interact_carry":
        # Tied golden-burlap supply sack hoisted on shoulder/back
        sx = 36 if row_idx in (0, 2) else (42 if row_idx == 1 else 30)
        sy = 30 + dy
        dr.ellipse([sx - 7, sy - 6, sx + 7, sy + 8], fill=(208, 168, 96, 255), outline=c_out)
        dr.rectangle([sx - 3, sy - 9, sx + 3, sy - 5], fill=(238, 204, 132, 255), outline=c_out)

    elif clip_name == "interact_observe":
        # Inspection notebook / magnifying glass + discovery sparkle
        dr.ellipse([hx - 4, hy - 6, hx + 4, hy + 2], fill=(184, 238, 255, 255), outline=(218, 168, 64, 255))
        if frame_idx in (3, 4, 5):
            dr.polygon([(hx + dir_x * 6, hy - 10), (hx + dir_x * 8, hy - 6), (hx + dir_x * 6, hy - 2), (hx + dir_x * 4, hy - 6)], fill=(255, 242, 132, 255), outline=c_out)

    elif clip_name in ("whistle", "horn"):
        # Curved bronze-gold herding horn / whistle at mouth + expanding acoustic wave arcs
        mx = 28 if row_idx == 1 else (44 if row_idx == 3 else 36)
        my = 22 + dy
        if clip_name == "horn":
            dr.polygon([(mx, my), (mx + dir_x * 9, my - 5), (mx + dir_x * 9, my + 3)], fill=(232, 184, 72, 255), outline=c_out)
        if 2 <= frame_idx <= n_frames - 2:
            wx = mx + dir_x * (11 + (frame_idx - 2) * 2)
            dr.arc([wx - 6, my - 8, wx + 6, my + 8], start=-60 if dir_x > 0 else 120, end=60 if dir_x > 0 else 240, fill=(112, 236, 255, 255) if is_an else (255, 224, 108, 255), width=2)

    elif clip_name == "idle_fidget_2":
        # Open ledger notebook (`sổ hồng` for An / `sổ nâu` for Khoa)
        book_col = (242, 152, 178, 255) if is_an else (196, 158, 104, 255)
        dr.rectangle([hx - 5, hy - 4, hx + 4, hy + 4], fill=book_col, outline=c_out)
        dr.line([hx, hy - 3, hx, hy + 3], fill=(250, 246, 232, 255), width=1)

    cell[:] = np.array(pil, dtype=np.uint8)


def resample_sheet_cols(sheet: np.ndarray, target_cols: int) -> np.ndarray:
    src_cols = max(1, sheet.shape[1] // CW)
    if src_cols == target_cols:
        return sheet.copy()
    out = np.zeros((CH * 4, CW * target_cols, 4), dtype=np.uint8)
    for r in range(4):
        for tc in range(target_cols):
            sc = (tc * src_cols) // target_cols
            out[r * CH : (r + 1) * CH, tc * CW : (tc + 1) * CW] = sheet[
                r * CH : (r + 1) * CH, sc * CW : (sc + 1) * CW
            ]
    return out


def build_character_clip_sheet(
    who: str,
    clip_name: str,
    n_frames: int,
    base_clips: dict,
) -> np.ndarray:
    """Builds a complete 4-row (`down`, `left`, `up`, `right`) x `n_frames` 80px sprite sheet (`72x84` cells)
    for `who` (`an` or `khoa`) with genuine multi-frame kinematic body motion and visual signatures."""
    is_an = who.endswith("an")
    b_idle = base_clips["idle"]
    b_walk = base_clips["walk"]
    b_run = base_clips["run"]
    b_atk = base_clips["attack"]
    b_hit = base_clips["hit"]
    b_death = base_clips["death"]
    b_int = base_clips["interact"]
    b_whi = base_clips["whistle"]

    # 1. Core approved 80px locomotion & base hit/death sheets stay faithful to the approved baseline
    if clip_name == "idle":
        return qa_enforcer.finalize_and_verify_sheet(resample_sheet_cols(b_idle, n_frames), CW, CH, n_frames, clip_name, max_colors=47)
    if clip_name == "walk":
        return qa_enforcer.finalize_and_verify_sheet(resample_sheet_cols(b_walk, n_frames), CW, CH, n_frames, clip_name, max_colors=47)
    if clip_name == "run":
        return qa_enforcer.finalize_and_verify_sheet(resample_sheet_cols(b_run, n_frames), CW, CH, n_frames, clip_name, max_colors=47)
    if clip_name in ("hit", "hit_light"):
        s = np.zeros((CH * 4, CW * n_frames, 4), dtype=np.uint8)
        src_n = b_hit.shape[1] // CW
        for r in range(4):
            for c in range(n_frames):
                sc = min(src_n - 1, (c * src_n) // n_frames)
                cell = b_hit[r * CH : (r + 1) * CH, sc * CW : (sc + 1) * CW].copy()
                if clip_name == "hit_light" and c in (1, 2):
                    recoil = 2 if c == 1 else 1
                    dx = recoil if r == 1 else (-recoil if r == 3 else 0)
                    dy = -recoil if r == 0 else (recoil if r == 2 else 0)
                    cell = articulate_character_cell(cell, r, dx, dy, -6.0 * (1 if r != 1 else -1), 0, -1)
                s[r * CH : (r + 1) * CH, c * CW : (c + 1) * CW] = cell
        return qa_enforcer.finalize_and_verify_sheet(s, CW, CH, n_frames, clip_name, max_colors=47)

    if clip_name == "hit_heavy":
        s = np.zeros((CH * 4, CW * n_frames, 4), dtype=np.uint8)
        src_n = b_hit.shape[1] // CW
        recoils = [0, 3, 5, 4, 2, 0]
        for r in range(4):
            for c in range(n_frames):
                sc = min(src_n - 1, (c * src_n) // n_frames)
                cell = b_hit[r * CH : (r + 1) * CH, sc * CW : (sc + 1) * CW].copy()
                rec = recoils[c % len(recoils)]
                dx = rec if r == 1 else (-rec if r == 3 else 0)
                dy = -rec if r == 0 else (rec if r == 2 else 0)
                t_deg = (-12.0 if c in (1, 2, 3) else -5.0) * (-1.0 if r == 1 else 1.0)
                cell = articulate_character_cell(cell, r, dx, dy, t_deg, 0, -2 if c in (1, 2) else 0)
                s[r * CH : (r + 1) * CH, c * CW : (c + 1) * CW] = cell
        return qa_enforcer.finalize_and_verify_sheet(s, CW, CH, n_frames, clip_name, max_colors=47)

    if clip_name in ("death", "faint"):
        s = np.zeros((CH * 4, CW * n_frames, 4), dtype=np.uint8)
        src_n = b_death.shape[1] // CW
        for r in range(4):
            for c in range(n_frames):
                sc = min(src_n - 1, (c * src_n) // n_frames)
                cell = b_death[r * CH : (r + 1) * CH, sc * CW : (sc + 1) * CW].copy()
                if clip_name == "faint" and c >= 2:
                    cell = shift_body_keep_shadow(cell, 1 if r in (0, 3) else -1, min(2, c // 3))
                s[r * CH : (r + 1) * CH, c * CW : (c + 1) * CW] = cell
        return qa_enforcer.finalize_and_verify_sheet(s, CW, CH, n_frames, clip_name, max_colors=47)

    # 2. Dodge & Counter (distinct agile dash vs parry-riposte)
    if clip_name in ("dodge", "counter"):
        s = np.zeros((CH * 4, CW * n_frames, 4), dtype=np.uint8)
        src_sheet = b_run if clip_name == "dodge" else b_atk
        src_n = src_sheet.shape[1] // CW
        # (fwd, vy, torso_deg, arm_dy)
        if clip_name == "dodge":
            k_curve = [
                (-1, 1, -6.0, 1),
                (2, 2, 10.0, 2),
                (5, -2, 14.0, -2),
                (7, -3, 16.0, -3),
                (6, -1, 12.0, -1),
                (4, 1, 8.0, 1),
                (2, 1, 4.0, 0),
                (0, 0, 0.0, 0),
            ]
        else:
            k_curve = [
                (-2, 1, -10.0, -2),
                (-3, 2, -14.0, -3),
                (-1, 0, -4.0, -1),
                (5, -1, 15.0, 3),
                (6, 0, 16.0, 4),
                (4, 0, 10.0, 2),
                (2, 0, 5.0, 1),
                (0, 0, 0.0, 0),
            ]
        for r in range(4):
            dir_sgn = -1.0 if r == 1 else 1.0
            for c in range(n_frames):
                sc = (c * src_n) // n_frames
                cell = src_sheet[r * CH : (r + 1) * CH, sc * CW : (sc + 1) * CW].copy()
                fwd, vy, t_deg, a_dy = k_curve[c % len(k_curve)]
                if is_an:
                    fwd = int(round(fwd * 1.15))
                dx = -fwd if r == 1 else (fwd if r == 3 else (1 if c in (2, 3, 4) and is_an else 0))
                dy = vy + (fwd // 2 if r == 0 else (-fwd // 2 if r == 2 else 0))
                cell = articulate_character_cell(cell, r, dx, dy, t_deg * dir_sgn, 0, a_dy, stride_spread=2 if c in (2, 3, 4) else 0)
                if clip_name == "counter" and c in (3, 4):
                    pil = Image.fromarray(cell, "RGBA")
                    dr = ImageDraw.Draw(pil)
                    draw_weapon_archetype_arc(
                        dr, r, "slash", 2 if c == 3 else 3, c - 2,
                        (96, 228, 255, 255) if is_an else (255, 198, 76, 255),
                        (240, 252, 255, 255) if is_an else (255, 246, 180, 255),
                        is_an=is_an,
                    )
                    cell = np.array(pil, dtype=np.uint8)
                s[r * CH : (r + 1) * CH, c * CW : (c + 1) * CW] = cell
        return qa_enforcer.finalize_and_verify_sheet(s, CW, CH, n_frames, clip_name, max_colors=47)

    # 3. Farm Interaction Clips, Whistle, Horn & Idle Fidgets
    if clip_name.startswith("interact") or clip_name in ("whistle", "horn", "idle_fidget_1", "idle_fidget_2"):
        src_sheet = (
            b_whi if clip_name in ("whistle", "horn")
            else (b_walk if clip_name == "interact_carry" else (b_idle if "fidget" in clip_name else b_int))
        )
        src_n = src_sheet.shape[1] // CW
        s = np.zeros((CH * 4, CW * n_frames, 4), dtype=np.uint8)
        for r in range(4):
            dir_sgn = -1.0 if r == 1 else 1.0
            for c in range(n_frames):
                sc = (c * src_n) // n_frames
                cell = src_sheet[r * CH : (r + 1) * CH, sc * CW : (sc + 1) * CW].copy()
                phase = math.sin(2.0 * math.pi * c / float(n_frames))
                if clip_name in ("interact_gather", "interact_observe"):
                    # Deep crouch / bend down on middle frames (c=2..5)
                    bend = 1.0 if c in (2, 3, 4, 5) else (0.5 if c in (1, 6) else 0.0)
                    dy = int(round(3.0 * bend))
                    dx = int(round((2.0 if r == 3 else (-2.0 if r == 1 else 0.0)) * bend))
                    t_deg = 14.0 * bend * dir_sgn
                    a_dy = int(round(4.0 * bend))
                elif clip_name == "interact_repair":
                    # Hammer raise (c=1..2) -> sharp downward strike (c=3..5)
                    strike = -1.0 if c in (1, 2) else (1.0 if c in (3, 4, 5) else 0.0)
                    dy = 1 if strike > 0 else 0
                    dx = int(round(2.0 * strike * (1 if r == 3 else (-1 if r == 1 else 0))))
                    t_deg = 11.0 * strike * dir_sgn
                    a_dy = int(round(4.0 * strike))
                elif clip_name == "interact_clean":
                    # Side-to-side sweeping motion
                    dx = int(round(2.5 * phase))
                    dy = int(round(1.0 * abs(phase)))
                    t_deg = 9.0 * phase
                    a_dy = int(round(2.0 * phase))
                elif clip_name in ("whistle", "horn"):
                    lift = 1.0 if 2 <= c <= n_frames - 3 else 0.0
                    dx = 0
                    dy = -1 if lift > 0 else 0
                    t_deg = -6.0 * lift * dir_sgn
                    a_dy = int(round(-3.0 * lift))
                else:
                    # feed, water, carry, fidget
                    dx = int(round(1.5 * phase)) if "feed" in clip_name else 0
                    dy = int(round(2.0 * abs(phase)))
                    t_deg = 8.0 * phase * dir_sgn
                    a_dy = int(round(2.5 * phase))

                cell = articulate_character_cell(cell, r, dx, dy, t_deg, 0, a_dy)
                draw_farm_interaction_prop(cell, r, clip_name, c, n_frames, dy, is_an)
                s[r * CH : (r + 1) * CH, c * CW : (c + 1) * CW] = cell
        return qa_enforcer.finalize_and_verify_sheet(s, CW, CH, n_frames, clip_name, max_colors=47)

    # 4. Weapon Basic Attacks (`attack`, `attack_{slash,thrust,smash,ranged}_{1..3}`)
    #    and All 115 Martial Skill Clips (`skill_<key>` and `skill_<key>_{slash,thrust,smash,ranged}`)
    is_skill = clip_name.startswith("skill_")
    archetype = "unarmed"
    for tag in ("slash", "thrust", "smash", "ranged"):
        if f"_{tag}" in clip_name:
            archetype = tag
            break
    if archetype == "unarmed" and clip_name == "attack":
        archetype = "slash"

    step = 3 if clip_name.endswith("_3") else (2 if clip_name.endswith("_2") else 1)

    skill_key = ""
    motion_type = "standard_combo"
    school = "A"
    if is_skill:
        rem_name = clip_name[len("skill_") :]
        for tag in ("_slash", "_thrust", "_smash", "_ranged"):
            if rem_name.endswith(tag):
                rem_name = rem_name[: -len(tag)]
                break
        skill_key = rem_name
        school, motion_type, _, _, _ = SKILL_PROFILES.get(
            skill_key, ("A", "dash_blitz", 26.0, (72, 224, 255), (235, 250, 255))
        )

    # Combine frames from `b_atk`, `b_walk`, and `b_run` so the legs and body change stance across the clip
    w_n = b_walk.shape[1] // CW
    r_n = b_run.shape[1] // CW
    a_n = b_atk.shape[1] // CW

    s = np.zeros((CH * 4, CW * n_frames, 4), dtype=np.uint8)

    for r in range(4):
        dir_sgn = -1.0 if r == 1 else (1.0 if r == 3 else (1.0 if step != 2 else -1.0))
        for c in range(n_frames):
            # Pick dynamic base frame: wind-up uses walk/idle brace, peak strike uses attack/run lunge, recovery uses walk
            if c in (0, 1):
                base_cell = b_walk[r * CH : (r + 1) * CH, ((c + 1) % w_n) * CW : (((c + 1) % w_n) + 1) * CW].copy()
            elif c in (2, 3, 4, 5):
                if c in (2, 3) and a_n >= 4:
                    ai = min(a_n - 1, c + (1 if step == 2 else 0))
                    base_cell = b_atk[r * CH : (r + 1) * CH, ai * CW : (ai + 1) * CW].copy()
                else:
                    ri = (c + step) % r_n
                    base_cell = b_run[r * CH : (r + 1) * CH, ri * CW : (ri + 1) * CW].copy()
            else:
                wi = (c % w_n)
                base_cell = b_walk[r * CH : (r + 1) * CH, wi * CW : (wi + 1) * CW].copy()

            # Compute weapon-archetype + skill-specific kinematic parameters
            is_smash = archetype == "smash" or motion_type in ("crush_break", "seismic_stomp", "meteor_drop", "stake_drive")
            is_thrust = archetype == "thrust" or motion_type in ("pierce_glide", "impale_pin", "hook_yank")
            is_ranged = archetype == "ranged" or motion_type in ("sky_mark", "net_cast")
            is_aerial = motion_type in ("leap_rescue", "meteor_drop", "warp_flash") or (step == 3 and is_an)

            if c in (0, 1):
                # Phase 1: Anticipation / Wind-up
                fwd = (-2 - c) if not is_ranged else -1
                vy = (-2 - c) if is_smash else (1 if not is_aerial else -1)
                t_deg = (-10.0 - 4.0 * c) * dir_sgn if not is_ranged else -5.0 * dir_sgn
                a_dx = -2 * int(dir_sgn)
                a_dy = -3 - c if is_smash else (-2 - c)
                spread = 1
            elif c in (2, 3, 4):
                # Phase 2: Explosive Strike / Lunge / Spin / Cast
                if motion_type == "hook_yank" and c == 4:
                    fwd = -3
                elif is_thrust or motion_type in ("dash_blitz", "pierce_glide", "wing_intercept"):
                    fwd = 6 + step + (c - 2)
                elif is_ranged:
                    fwd = -3 if c in (2, 3) else -1
                elif motion_type in ("root_stance", "dome_aegis", "diamond_body"):
                    fwd = 2
                else:
                    fwd = 4 + step + (c - 2)

                if is_aerial and c in (2, 3):
                    vy = -3
                elif is_smash and c in (3, 4):
                    vy = 2
                elif motion_type in ("root_stance", "seismic_stomp"):
                    vy = 2
                else:
                    vy = 1 if r == 0 else (-1 if r == 2 else 0)

                if motion_type in ("vortex_cone", "triple_flurry", "mirage_split"):
                    t_deg = (15.0 if (c % 2 == 0) else -15.0) * dir_sgn
                elif is_ranged:
                    t_deg = -8.0 * dir_sgn
                else:
                    t_deg = (13.0 + 3.0 * (c - 2)) * dir_sgn

                a_dx = 3 * int(dir_sgn) if not is_ranged else -2 * int(dir_sgn)
                a_dy = 3 + (c - 2) if is_smash else (2 if c >= 3 else -1)
                spread = 2
            elif c in (5, 6):
                # Phase 3: Impact Follow-Through / Hold
                fwd = (-2 if motion_type == "hook_yank" else (3 + step - (c - 5))) if not is_ranged else -1
                vy = 1 if is_smash else 0
                t_deg = (9.0 - 3.0 * (c - 5)) * dir_sgn if not is_ranged else -3.0 * dir_sgn
                a_dx = 1 * int(dir_sgn)
                a_dy = 2 - (c - 5)
                spread = 1
            else:
                # Phase 4: Recovery to combat guard
                rem = max(0.0, float(n_frames - 1 - c) / max(1.0, float(n_frames - 7)))
                fwd = int(round(2.0 * rem))
                vy = 0
                t_deg = 4.0 * rem * dir_sgn
                a_dx = 0
                a_dy = 0
                spread = 0

            # Differentiate An (nimble, higher torso twist & lateral grace) vs Khoa (grounded power)
            if is_an:
                t_deg *= 1.15
                if c in (2, 3, 4) and r in (0, 2):
                    a_dy -= 1
            else:
                if c in (2, 3, 4) and not is_ranged:
                    fwd += 1

            dx = -fwd if r == 1 else (fwd if r == 3 else int(round(dir_sgn * (2 if c in (2, 3, 4) else (-1 if c in (0, 1) else 0)))))
            dy = vy + (fwd // 3 if r == 0 else (-fwd // 3 if r == 2 else 0))

            # Keep Frame 0 strictly near neutral y so Frame 0 height check is always ~80px
            if c == 0:
                dy = max(-1, min(1, dy))

            cell = articulate_character_cell(base_cell, r, dx, dy, t_deg, a_dx, a_dy, stride_spread=spread)

            if is_skill:
                draw_skill_signature_motif(cell, r, skill_key, archetype, c, n_frames, is_an)
            else:
                # Basic attack / weapon combo smear arc on frames 2, 3, 4
                if c in (2, 3, 4):
                    pil = Image.fromarray(cell, "RGBA")
                    dr = ImageDraw.Draw(pil)
                    pal_map = {
                        "slash": ((176, 224, 248, 255), (240, 252, 255, 255)) if is_an else ((192, 208, 224, 255), (245, 250, 255, 255)),
                        "thrust": ((242, 118, 92, 255), (255, 232, 152, 255)) if is_an else ((228, 96, 68, 255), (255, 218, 128, 255)),
                        "smash": ((248, 178, 64, 255), (255, 242, 164, 255)) if is_an else ((238, 148, 48, 255), (255, 228, 136, 255)),
                        "ranged": ((124, 228, 248, 255), (255, 248, 184, 255)) if is_an else ((218, 168, 84, 255), (255, 242, 168, 255)),
                    }
                    c_m, c_h = pal_map.get(archetype, pal_map["slash"])
                    draw_weapon_archetype_arc(dr, r, archetype, step, c - 2, c_m, c_h, is_an=is_an)
                    cell = np.array(pil, dtype=np.uint8)

            s[r * CH : (r + 1) * CH, c * CW : (c + 1) * CW] = cell

    return qa_enforcer.finalize_and_verify_sheet(s, CW, CH, n_frames, clip_name, max_colors=47)


def get_clip_metadata(clip_name: str, n_frames: int) -> dict:
    """Returns the JSON metadata dict for `clip_name` with smooth FPS (`18-24` for interactions,
    `24-30` for attacks/skills) and exact `effect_frame = n_frames // 2` for one-shot actions."""
    loop = clip_name in ("idle", "walk", "run", "interact_carry")
    stride_m = None
    if clip_name == "walk":
        fps = 20.0
        stride_m = 1.8
    elif clip_name == "run":
        fps = 20.0
        stride_m = 3.111
    elif clip_name == "idle":
        fps = 10.0
    elif clip_name in ("idle_fidget_1", "idle_fidget_2"):
        fps = 16.0
    elif clip_name == "interact_observe":
        fps = 18.0
    elif clip_name == "interact_repair":
        fps = 22.0
    elif clip_name.startswith("interact") or clip_name in ("whistle", "horn"):
        fps = 20.0
    elif clip_name in ("dodge", "counter"):
        fps = 24.0
    elif clip_name.startswith("hit"):
        fps = 18.0
    elif clip_name in ("death", "faint"):
        fps = 14.0
    elif clip_name.startswith("attack"):
        if "slash" in clip_name:
            fps = 28.0
        elif "thrust" in clip_name or "ranged" in clip_name:
            fps = 26.0
        elif "smash" in clip_name:
            fps = 24.0
        else:
            fps = 26.0
    elif clip_name.startswith("skill_"):
        rem = clip_name[len("skill_") :]
        for tag in ("_slash", "_thrust", "_smash", "_ranged"):
            if rem.endswith(tag):
                rem = rem[: -len(tag)]
                break
        _, _, sk_fps, _, _ = SKILL_PROFILES.get(rem, ("A", "dash_blitz", 26.0, (0, 0, 0), (0, 0, 0)))
        if clip_name.endswith("_slash"):
            fps = min(30.0, sk_fps + 1.0)
        elif clip_name.endswith("_smash"):
            fps = max(24.0, sk_fps - 1.0)
        else:
            fps = sk_fps
    else:
        fps = 20.0

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
        meta["stride_m_per_cycle"] = float(stride_m)
    if not loop:
        meta["effect_frame"] = int(n_frames // 2)
    return meta


def save_clip_to_dirs(
    dirs: list[Path],
    family: str,
    clip_name: str,
    sheet_rgba: np.ndarray,
    meta: dict,
    write_meta: bool = True,
) -> None:
    pil_img = Image.fromarray(sheet_rgba, "RGBA")
    json_bytes = (json.dumps(meta, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    for d in dirs:
        d.mkdir(parents=True, exist_ok=True)
        png_p = d / f"{family}_{clip_name}.png"
        json_p = d / f"{family}_{clip_name}.json"
        pil_img.save(png_p, format="PNG")
        json_p.write_bytes(json_bytes)
        if write_meta:
            write_unity_png_meta(png_p, ppu=32)
            write_unity_json_meta(json_p)


def load_approved_80px_base_clips(who: str) -> dict:
    """Loads the 8 core 80px sheets (`idle`, `walk`, `run`, `attack`, `hit`, `death`, `interact`, `whistle`)
    for `an` (from `OptionC2_NavySilver`) or `khoa` (from `from_argon/Assets/Art/PixelMyth/Characters/khoa`)."""
    base_clips = {}
    for k in ("idle", "walk", "run", "attack", "hit", "death", "interact", "whistle"):
        if who == "an":
            p = OPT_C2_DIR / f"an_{k}.png"
            if not p.exists():
                p = FA_CHARS / "an" / f"an_{k}.png"
        else:
            p = FA_CHARS / "khoa" / f"khoa_{k}.png"
        im = Image.open(p).convert("RGBA")
        arr = np.array(im, dtype=np.uint8)
        if who == "an":
            # Clean tiny diagonal shadow scratch outside ground shadow ellipse at y=76..83
            cols = arr.shape[1] // CW
            for r in range(4):
                for c in range(cols):
                    ox, oy = c * CW, r * CH
                    for y in range(oy + 76, oy + 84):
                        for x in range(ox, ox + CW):
                            rgba = arr[y, x]
                            if 0 < rgba[3] < 110 and rgba[0] < 90 and rgba[1] < 90 and rgba[2] < 95:
                                dx = (x - (ox + 36)) / 13.5
                                dy = (y - (oy + 79.5)) / 3.2
                                if dx * dx + dy * dy > 1.0:
                                    arr[y, x] = (0, 0, 0, 0)
        base_clips[k] = arr
    return base_clips


def build_weapon_overlays_for_an_and_khoa(all_150_specs: list[tuple[str, int]]) -> int:
    """Generates synchronized 80px weapon overlay sheets for both `an` and `khoa` across all 4 weapons
    (`wpn_dao_rua`, `wpn_giao_moc`, `wpn_bua_ta`, `wpn_no_tay`) and quality tiers (`__II`, `__III`, `__IV`)."""
    weapon_palettes = {
        "wpn_dao_rua": ((176, 196, 216, 255), (238, 248, 255, 255)),
        "wpn_giao_moc": ((168, 184, 204, 255), (242, 212, 132, 255)),
        "wpn_bua_ta": ((156, 146, 138, 255), (242, 198, 116, 255)),
        "wpn_no_tay": ((176, 128, 78, 255), (242, 214, 132, 255)),
    }
    spec_dict = dict(all_150_specs)
    total_written = 0

    for who in ("an", "khoa"):
        is_an = who == "an"
        for item_id, tag in WEAPON_ITEMS:
            fam = f"{who}__{item_id}"
            src_dir = FA_CHARS / fam
            if not (src_dir / f"{fam}_idle.png").exists():
                src_dir = FA_CHARS / f"khoa__{item_id}"

            def load_wpn_base(ck: str, sdir=src_dir, f_prefix=fam) -> np.ndarray:
                p = sdir / f"{f_prefix}_{ck}.png"
                if not p.exists():
                    p = sdir / f"khoa__{item_id}_{ck}.png"
                if not p.exists():
                    p = sdir / f"{f_prefix}_idle.png"
                return np.array(Image.open(p).convert("RGBA"), dtype=np.uint8)

            g_idle = load_wpn_base("idle")
            g_walk = load_wpn_base("walk")
            g_run = load_wpn_base("run")
            g_atk = load_wpn_base("attack")
            c_main, c_hi = weapon_palettes[item_id]

            # Build the 81 clips for this weapon family (35 base/attack/interact + 23 skill_<sk> + 23 skill_<sk>_<tag>)
            wpn_clip_names = [
                c for c, _ in all_150_specs
                if not c.startswith("skill_") or c.endswith(f"_{tag}") or (
                    c.startswith("skill_") and not any(c.endswith(f"_{t}") for t in ("slash", "thrust", "smash", "ranged"))
                )
            ]

            base_dirs = [FA_CHARS / fam, ROOT_CHARS / fam]
            built_sheets = {}

            for clip_name in wpn_clip_names:
                nf = spec_dict.get(clip_name, 8)
                if clip_name == "idle":
                    base_arr = g_idle
                elif clip_name in ("walk", "interact_carry"):
                    base_arr = g_walk
                elif clip_name in ("run", "dodge"):
                    base_arr = g_run
                else:
                    base_arr = g_atk if (clip_name.startswith("attack") or clip_name.startswith("skill_") or clip_name == "counter") else g_idle

                src_n = base_arr.shape[1] // CW
                sheet = np.zeros((CH * 4, CW * nf, 4), dtype=np.uint8)
                is_combat = clip_name.startswith("attack") or clip_name.startswith("skill_") or clip_name == "counter"
                step = 3 if clip_name.endswith("_3") else (2 if clip_name.endswith("_2") else 1)

                # Determine school color if this is a martial skill
                arc_main, arc_hi = c_main, c_hi
                if clip_name.startswith("skill_"):
                    rem = clip_name[len("skill_") :]
                    if rem.endswith(f"_{tag}"):
                        rem = rem[: -len(f"_{tag}")]
                    _, _, _, sm_rgb, sh_rgb = SKILL_PROFILES.get(rem, ("A", "dash_blitz", 26.0, c_main[:3], c_hi[:3]))
                    arc_main = (sm_rgb[0], sm_rgb[1], sm_rgb[2], 255)
                    arc_hi = (sh_rgb[0], sh_rgb[1], sh_rgb[2], 255)

                for r in range(4):
                    for c in range(nf):
                        sc = (c * src_n) // nf
                        cell = base_arr[r * CH : (r + 1) * CH, sc * CW : (sc + 1) * CW].copy()
                        if is_combat:
                            # Articulate weapon position to follow the character's lunge & swing
                            fwd = 3 if c in (2, 3, 4) else (-2 if c in (0, 1) else 0)
                            dx = -fwd if r == 1 else (fwd if r == 3 else 0)
                            dy = -2 if (c in (0, 1) and tag == "smash") else (2 if c in (2, 3, 4) else 0)
                            cell = shift_body_keep_shadow(cell, dx, dy, foot_keep=1)
                            if c in (2, 3, 4):
                                pil = Image.fromarray(cell, "RGBA")
                                dr = ImageDraw.Draw(pil)
                                draw_weapon_archetype_arc(dr, r, tag, step, c - 2, arc_main, arc_hi, is_an=is_an)
                                cell = np.array(pil, dtype=np.uint8)
                        sheet[r * CH : (r + 1) * CH, c * CW : (c + 1) * CW] = cell

                meta = get_clip_metadata(clip_name, nf)
                save_clip_to_dirs(base_dirs, fam, clip_name, sheet, meta, write_meta=False)
                built_sheets[clip_name] = (sheet, meta)
                total_written += 1

            # Sync quality tiers __II, __III, __IV with tier metallic/elemental tint across essential combat & locomotion clips
            tier_clips = [
                "idle", "walk", "run", "attack",
                f"attack_{tag}_1", f"attack_{tag}_2", f"attack_{tag}_3",
                "dodge", "counter", "hit", "hit_light", "hit_heavy", "faint", "death",
                "interact", "interact_feed", "interact_water", "interact_clean", "whistle",
            ]
            for q_tag, tint in (("__II", (8, 22, 14)), ("__III", (10, 18, 28)), ("__IV", (32, 22, -12))):
                q_fam = f"{fam}{q_tag}"
                q_dirs = [FA_CHARS / q_fam, ROOT_CHARS / q_fam]
                for clip_name in tier_clips:
                    if clip_name not in built_sheets:
                        continue
                    base_sheet, meta = built_sheets[clip_name]
                    arr = base_sheet.copy()
                    op = arr[:, :, 3] > 128
                    for ch_i in range(3):
                        arr[:, :, ch_i][op] = np.clip(arr[:, :, ch_i][op].astype(np.int16) + tint[ch_i], 0, 255).astype(np.uint8)
                    save_clip_to_dirs(q_dirs, q_fam, clip_name, arr, meta, write_meta=False)
                    total_written += 1

    return total_written


def update_locked_an_manifest() -> int:
    """Copies all 150 `an_*.png` + `an_*.json` (plus portraits) into `.collab/claude-2d/argon-deliverables/LOCKED/an/`
    and writes `sha256.txt`."""
    LOCKED_AN_DIR.mkdir(parents=True, exist_ok=True)
    src_an = FA_CHARS / "an"
    copied = []
    for f in sorted(src_an.iterdir()):
        if f.is_file() and f.suffix.lower() in (".png", ".json"):
            dst = LOCKED_AN_DIR / f.name
            shutil.copy2(f, dst)
            copied.append(dst)

    src_portraits = FROM_ARGON / "Assets" / "Art" / "PixelMyth" / "UI" / "Portraits"
    if src_portraits.exists():
        p_dst = LOCKED_AN_DIR / "portraits"
        p_dst.mkdir(parents=True, exist_ok=True)
        for f in sorted(src_portraits.iterdir()):
            if f.is_file() and f.name.lower().startswith("an_") and f.suffix.lower() == ".png":
                dst = p_dst / f.name
                shutil.copy2(f, dst)
                copied.append(dst)

    lines = []
    for p in sorted(copied, key=lambda x: str(x.relative_to(LOCKED_AN_DIR))):
        h = hashlib.sha256(p.read_bytes()).hexdigest()
        rel = str(p.relative_to(LOCKED_AN_DIR)).replace("\\", "/")
        lines.append(f"{h}  {rel}")
    (LOCKED_AN_DIR / "sha256.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return len(copied)


def main():
    t0 = time.time()
    khoa_src_dir = FA_CHARS / "khoa"
    all_150_specs = []
    for jp in sorted(khoa_src_dir.glob("khoa_*.json")):
        stem = jp.stem[len("khoa_") :]
        data = json.loads(jp.read_text(encoding="utf-8"))
        nf = int(data.get("frames_per_row", 8))
        all_150_specs.append((stem, nf))

    print(f"[1/4] Loaded {len(all_150_specs)} clip specifications from {khoa_src_dir}")

    for who in ("an", "khoa"):
        base_clips = load_approved_80px_base_clips(who)
        target_dirs = [FA_CHARS / who, ROOT_CHARS / who]
        argon_alias_dirs = []
        if who == "an":
            argon_alias_dirs = [FA_CHARS / "argon_an", ROOT_CHARS / "argon_an"]

        for idx, (clip_name, nf) in enumerate(all_150_specs, 1):
            sheet = build_character_clip_sheet(who, clip_name, nf, base_clips)
            meta = get_clip_metadata(clip_name, nf)
            save_clip_to_dirs(target_dirs, who, clip_name, sheet, meta, write_meta=True)
            if argon_alias_dirs:
                save_clip_to_dirs(argon_alias_dirs, "argon_an", clip_name, sheet, meta, write_meta=True)
            if idx % 50 == 0 or idx == len(all_150_specs):
                print(f"  -> Built {idx}/{len(all_150_specs)} 80px kinematic clips for {who}")

    print("[2/4] Building synchronized 80px weapon overlay sheets for An and Khoa...")
    wpn_count = build_weapon_overlays_for_an_and_khoa(all_150_specs)
    print(f"  -> Generated {wpn_count} weapon overlay sheets across An & Khoa weapon families.")

    print("[3/4] Updating LOCKED/an/ manifest and sha256.txt...")
    locked_cnt = update_locked_an_manifest()
    print(f"  -> Locked {locked_cnt} files in {LOCKED_AN_DIR} (sha256.txt updated).")

    elapsed = time.time() - t0
    print(f"[4/4] Done in {elapsed:.1f}s!")


if __name__ == "__main__":
    main()
