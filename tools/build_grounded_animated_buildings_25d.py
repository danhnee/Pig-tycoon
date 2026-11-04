#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
build_grounded_animated_buildings_25d.py
========================================
Subagent 2 Deliverable:
1. Grounds every `*_static.png` in `from_argon/Assets/Art/PixelMyth/Buildings/` onto the map:
   - Fixes cut-off/placeholder static sheets (`approved_power_recovery`, `approved_information_complex`,
     `bldg_observatory`, `prop_water_basin`, `prop_mud_wallow`).
   - Strips harsh 2-3px outer pitch-black halos, leaving a crisp 1px warm-umber selective outline.
   - Adds a grounded 2.5D weathered stone/timber base skirt + soft elliptical ground contact shadow
     (`alpha <= 54`) + subtle mossy earth & olive-grass tufts at the base corners so every structure sits
     firmly IN the `Farm_Main` terrain rather than floating above it.
2. Generates 4 multi-frame animated sprite sheets (`8 frames` each, horizontal strip + `.json` + `.meta`
   at 32 PPU) for all core Defense & Farm structures (`<name>_idle.png`, `<name>_active.png`,
   `<name>_attack.png`, `<name>_damaged.png`) and their `argon_def_*` counterparts.
3. Syncs `from_argon/Assets/Art/PixelMyth/Buildings/`, `Terrain/`, and `Decor/` to
   `D:/Pig-tycoon/Assets/Art/PixelMyth/` so both trees match 100%.
4. Renders the visual proof sheet `grounded_25d_defense_towers_and_buildings_proof.png` showing 12 key
   defense towers & farm buildings placed directly on `Farm_Main` grass in their `Idle`,
   `Charging/Active`, `Firing/Attack`, and `Damaged` states.
"""

import hashlib
import json
import math
import os
import shutil
import uuid
from pathlib import Path
from typing import Dict, List, Tuple

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(r"D:\Pig-tycoon")
FROM_ARGON_PM = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon" / "Assets" / "Art" / "PixelMyth"
ASSETS_PM = ROOT / "Assets" / "Art" / "PixelMyth"
ORIG_CACHE_DIR = ROOT / ".collab" / "claude-2d" / "argon-deliverables" / "_orig_buildings_static_cache"
PARENT_BRAIN_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\ac0f248a-d876-47b0-9f6a-277b76657366")
LOCAL_PROOF_PATH = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon" / "grounded_25d_defense_towers_and_buildings_proof.png"


# ---------------------------------------------------------------------------
# Structure categorization for tailored 8-frame animations
# ---------------------------------------------------------------------------
ATTACK_TOWERS = {
    "no_xuyen_van": "crossbow",
    "bldg_noxuyenvan_tower": "crossbow",
    "thap_phong_khong_skyhook": "skyhook",
    "cuu_tieu_loi_phao": "dragon_cannon",
    "thap_ho_quang": "arc_tower",
    "dai_thien_loi": "thunder_citadel",
    "van_linh_hoi_nguyen_tran": "jade_sanctuary",
    "dai_do_di_tuong": "void_obelisk",
    "ban_chong": "spike_trap",
    "min_no": "landmine",
    "ho_keo": "tar_trap",
    "tam_soc_dien": "shock_plate",
    "luoi_emp": "emp_grid",
    "chot_canh_gac": "guard_post",
    "cong_gia_co": "fortified_gate",
    "u_bao_cat": "sandbag",
    "hang_rao_go": "wood_barricade",
    "hang_rao_thep": "steel_barricade",
}

SUPPORT_AND_FARM_STRUCTURES = {
    "may_phat_dien": "generator",
    "approved_power_recovery": "generator",
    "cot_dien_luoi": "pylon",
    "cot_thu_loi": "lightning_rod",
    "thap_canh_quan_sat": "watchtower",
    "thien_nhan_vong_dai": "observatory",
    "bldg_observatory": "observatory",
    "radar_uy_hiep": "radar_dish",
    "ban_tinh_bao": "intel_pavilion",
    "may_chieu_khien": "shield_dome",
    "may_cong_huong_huyen_bi": "arcane_resonator",
    "tram_hoi_suc": "cauldron_shrine",
    "tram_khi_tuong": "weather_station",
    "bom_lam_mat": "cooling_fountain",
    "den_hieu_danh_chan": "beacon_lamp",
    "den_pha_cong_nghiep": "searchlight",
    "tram_tiep_song_cho": "relay_tower",
    "khu_cach_ly_hut": "quarantine_hut",
    "bldg_quarantine_zone": "quarantine_hut",
    "khu_xu_ly_xac": "incinerator_yard",
    "bldg_disposal_area": "incinerator_yard",
    "co_tap_ket": "rally_banner",
    "prop_feeder": "feeder_trough",
    "hay_trough": "feeder_trough",
    "prop_water_basin": "water_basin",
    "prop_care_post": "care_shrine",
    "prop_capacity_board": "stele_board",
    "prop_mud_wallow": "mud_wallow",
    "approved_main_house": "main_house",
    "approved_research_lab": "research_lab",
    "approved_medical_tent": "medical_tent",
    "approved_information_complex": "info_complex",
    "bldg_construction_hall": "forge_hall",
    "bldg_feed_storage": "market_house",
    "bldg_market_counter": "market_house",
    "bldg_warehouse": "warehouse",
    "bldg_herd_shelter": "thatch_shelter",
    "bldg_open_shelter": "thatch_shelter",
    "bldg_night_stall": "night_wagon",
}


def get_structure_behavior(folder_name: str) -> str:
    base = folder_name
    for prefix in ("argon_def_", "argon_prop_"):
        if base.startswith(prefix):
            base = base[len(prefix) :]
            break
    if base in ATTACK_TOWERS:
        return ATTACK_TOWERS[base]
    if base in SUPPORT_AND_FARM_STRUCTURES:
        return SUPPORT_AND_FARM_STRUCTURES[base]
    return ""


def write_unity_meta(png_path: Path, ppu: int = 32, pivot_x: float = 0.5, pivot_y: float = 0.08) -> None:
    meta_path = Path(str(png_path) + ".meta")
    guid = hashlib.md5(str(png_path.as_posix()).encode("utf-8")).hexdigest()[:32]
    if meta_path.exists():
        try:
            for line in meta_path.read_text(encoding="utf-8", errors="ignore").splitlines():
                if line.strip().startswith("guid:"):
                    guid = line.split(":", 1)[1].strip()
                    break
        except Exception:
            pass
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
  alignment: 9
  spritePivot: {{x: {pivot_x:.4f}, y: {pivot_y:.4f}}}
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


# ---------------------------------------------------------------------------
# Pre-fix broken/cropped/placeholder static sheets before grounding
# ---------------------------------------------------------------------------
def build_carved_water_basin_cell(cw: int = 56, ch: int = 47) -> Image.Image:
    """Crafts a 2.5D carved stone & timber water basin filled with crystalline azure water."""
    cell = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    draw = ImageDraw.Draw(cell)
    cx, cy = cw // 2, ch // 2 + 3

    # Ground contact shadow (alpha <= 54)
    draw.ellipse([4, cy + 5, cw - 5, ch - 2], fill=(24, 30, 18, 48))

    # Outer carved weathered stone basin body (isometric octagonal trough)
    outer_poly = [
        (cx - 22, cy - 2),
        (cx - 12, cy - 11),
        (cx + 12, cy - 11),
        (cx + 22, cy - 2),
        (cx + 20, cy + 11),
        (cx + 10, cy + 17),
        (cx - 10, cy + 17),
        (cx - 20, cy + 11),
    ]
    draw.polygon(outer_poly, fill=(96, 94, 88, 255), outline=(42, 31, 22, 245))

    # Front stone wall faces (shaded 2.5D depth)
    left_face = [(cx - 22, cy - 2), (cx - 10, cy + 6), (cx - 10, cy + 17), (cx - 20, cy + 11)]
    mid_face = [(cx - 10, cy + 6), (cx + 10, cy + 6), (cx + 10, cy + 17), (cx - 10, cy + 17)]
    right_face = [(cx + 10, cy + 6), (cx + 22, cy - 2), (cx + 20, cy + 11), (cx + 10, cy + 17)]
    draw.polygon(left_face, fill=(116, 112, 102, 255), outline=(42, 31, 22, 240))
    draw.polygon(mid_face, fill=(92, 90, 82, 255), outline=(42, 31, 22, 240))
    draw.polygon(right_face, fill=(72, 70, 64, 255), outline=(42, 31, 22, 240))

    # Bronze corner bands & timber rim accents
    draw.line([(cx - 10, cy + 6), (cx - 10, cy + 16)], fill=(154, 112, 58, 255), width=2)
    draw.line([(cx + 10, cy + 6), (cx + 10, cy + 16)], fill=(132, 94, 46, 255), width=2)

    # Top stone bevel rim
    rim_outer = [
        (cx - 22, cy - 2),
        (cx - 11, cy - 11),
        (cx + 11, cy - 11),
        (cx + 22, cy - 2),
        (cx + 10, cy + 6),
        (cx - 10, cy + 6),
    ]
    draw.polygon(rim_outer, fill=(148, 142, 128, 255), outline=(46, 34, 24, 245))

    # Crystalline azure water pool inside the trough
    water_poly = [
        (cx - 18, cy - 2),
        (cx - 9, cy - 8),
        (cx + 9, cy - 8),
        (cx + 18, cy - 2),
        (cx + 9, cy + 4),
        (cx - 9, cy + 4),
    ]
    draw.polygon(water_poly, fill=(48, 152, 214, 255), outline=(28, 82, 126, 255))
    inner_water = [
        (cx - 13, cy - 2),
        (cx - 6, cy - 6),
        (cx + 6, cy - 6),
        (cx + 13, cy - 2),
        (cx + 6, cy + 2),
        (cx - 6, cy + 2),
    ]
    draw.polygon(inner_water, fill=(88, 198, 245, 255))
    # Specular water ripples
    draw.arc([cx - 9, cy - 5, cx + 3, cy + 1], start=190, end=350, fill=(215, 248, 255, 245), width=1)
    draw.arc([cx - 2, cy - 3, cx + 10, cy + 2], start=10, end=170, fill=(195, 242, 255, 235), width=1)

    # Small bamboo/bronze water spout on upper-left rim
    draw.polygon([(cx - 18, cy - 15), (cx - 11, cy - 9), (cx - 13, cy - 6), (cx - 20, cy - 12)], fill=(158, 122, 68, 255), outline=(44, 31, 20, 245))
    draw.line([(cx - 12, cy - 7), (cx - 9, cy - 2)], fill=(185, 240, 255, 245), width=2)
    return cell


def build_rich_mud_wallow_cell(cw: int = 120, ch: int = 60) -> Image.Image:
    """Crafts a rich 2.5D mud wallow pit with wet mud highlights, stepping stones & grass tufts."""
    cell = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    draw = ImageDraw.Draw(cell)
    cx, cy = cw // 2, ch // 2 + 2

    # Soft ground transition shadow
    draw.ellipse([6, cy - 18, cw - 6, cy + 20], fill=(32, 36, 20, 44))
    # Outer earthy bank
    draw.ellipse([10, cy - 16, cw - 10, cy + 17], fill=(92, 68, 42, 245), outline=(48, 35, 22, 230))
    # Mid wet mud basin
    draw.ellipse([16, cy - 13, cw - 16, cy + 14], fill=(74, 52, 31, 255))
    # Deep glossy mud pool
    draw.ellipse([24, cy - 9, cw - 24, cy + 10], fill=(58, 39, 23, 255))
    # Wet specular mud sheen & ripples
    draw.arc([28, cy - 6, cx + 8, cy + 5], start=195, end=345, fill=(128, 98, 66, 220), width=1)
    draw.arc([cx - 6, cy - 4, cw - 30, cy + 7], start=15, end=165, fill=(116, 88, 58, 210), width=1)
    draw.ellipse([cx - 18, cy - 3, cx - 12, cy + 1], fill=(138, 108, 76, 210))
    draw.ellipse([cx + 10, cy - 2, cx + 15, cy + 2], fill=(132, 102, 70, 200))

    # Mossy rim stones & meadow tufts around the mud bank
    stones = [(18, cy - 6, 5, 3), (cw - 22, cy - 4, 6, 4), (26, cy + 10, 6, 3), (cw - 30, cy + 9, 5, 3)]
    for sx, sy, rx, ry in stones:
        draw.ellipse([sx - rx, sy - ry, sx + rx, sy + ry], fill=(112, 110, 98, 255), outline=(42, 32, 22, 235))
        draw.ellipse([sx - rx + 1, sy - ry, sx + rx - 2, sy], fill=(142, 140, 124, 240))
    return cell


def fix_special_static_sheets(buildings_root: Path) -> None:
    """Fixes cropped/placeholder static sheets before the universal grounding pass."""
    # 1. approved_power_recovery: fit full uncropped may_phat_dien into (160, 150) per row
    mpd_path = ORIG_CACHE_DIR / "may_phat_dien_static.png"
    if not mpd_path.exists():
        mpd_path = buildings_root / "may_phat_dien" / "may_phat_dien_static.png"
    apr_dir = buildings_root / "approved_power_recovery"
    if mpd_path.exists() and apr_dir.exists():
        mpd_im = Image.open(mpd_path).convert("RGBA")
        mcw, mch = mpd_im.width, mpd_im.height // 4
        row0 = mpd_im.crop((0, 0, mcw, mch))
        # Fit (180, 204) cleanly into (160, 150) preserving aspect ratio
        tcw, tch = 160, 150
        scale = min((tcw - 8) / float(mcw), (tch - 6) / float(mch))
        nw, nh = max(1, int(round(mcw * scale))), max(1, int(round(mch * scale)))
        resized = row0.resize((nw, nh), Image.Resampling.LANCZOS)
        # Sharpen alpha threshold so pixel edges remain crisp
        arr = np.array(resized)
        arr[:, :, 3] = np.where(arr[:, :, 3] > 90, 255, 0).astype(np.uint8)
        resized = Image.fromarray(arr, "RGBA")
        cell = Image.new("RGBA", (tcw, tch), (0, 0, 0, 0))
        cell.paste(resized, ((tcw - nw) // 2, tch - nh - 3), resized)
        full = Image.new("RGBA", (tcw, tch * 4), (0, 0, 0, 0))
        for r in range(4):
            full.paste(cell, (0, r * tch))
        full.save(apr_dir / "approved_power_recovery_static.png", format="PNG")
        if (ORIG_CACHE_DIR / "approved_power_recovery_static.png").exists():
            full.save(ORIG_CACHE_DIR / "approved_power_recovery_static.png", format="PNG")

    # 2. approved_information_complex: rebuild from full tram_khi_tuong, ban_tinh_bao, tram_tiep_song_cho
    #    on a grounded 2.5D cobbled stone courtyard terrace (removing the flat grey UI rectangle!)
    aic_dir = buildings_root / "approved_information_complex"
    tkt_p = ORIG_CACHE_DIR / "tram_khi_tuong_static.png"
    btb_p = ORIG_CACHE_DIR / "ban_tinh_bao_static.png"
    ttsc_p = ORIG_CACHE_DIR / "tram_tiep_song_cho_static.png"
    if not tkt_p.exists():
        tkt_p = buildings_root / "tram_khi_tuong" / "tram_khi_tuong_static.png"
    if not btb_p.exists():
        btb_p = buildings_root / "ban_tinh_bao" / "ban_tinh_bao_static.png"
    if not ttsc_p.exists():
        ttsc_p = buildings_root / "tram_tiep_song_cho" / "tram_tiep_song_cho_static.png"

    if aic_dir.exists() and tkt_p.exists() and btb_p.exists() and ttsc_p.exists():
        tcw, tch = 560, 190
        cell = Image.new("RGBA", (tcw, tch), (0, 0, 0, 0))
        draw = ImageDraw.Draw(cell)

        # Grounded 2.5D weathered cobblestone courtyard plinth connecting the 3 structures
        draw.ellipse([18, tch - 46, tcw - 18, tch - 2], fill=(24, 30, 16, 48))
        plinth_poly = [
            (28, tch - 28),
            (72, tch - 44),
            (tcw - 72, tch - 44),
            (tcw - 28, tch - 28),
            (tcw - 42, tch - 9),
            (42, tch - 9),
        ]
        draw.polygon(plinth_poly, fill=(122, 116, 102, 255), outline=(44, 32, 22, 245))
        # Front bevelled stone curb of the terrace
        curb_poly = [
            (42, tch - 16),
            (tcw - 42, tch - 16),
            (tcw - 42, tch - 8),
            (42, tch - 8),
        ]
        draw.polygon(curb_poly, fill=(88, 84, 74, 255), outline=(42, 30, 20, 245))
        # Cobblestone joint lines & mossy accents on the terrace
        for gx in range(60, tcw - 60, 22):
            draw.line([(gx, tch - 40), (gx - 10, tch - 16)], fill=(96, 90, 78, 190), width=1)
        for gy in (tch - 34, tch - 25):
            draw.line([(54, gy), (tcw - 54, gy)], fill=(102, 96, 84, 180), width=1)

        def paste_fitted(src_path: Path, box_x: int, target_h: int):
            im = Image.open(src_path).convert("RGBA")
            scw, sch = im.width, im.height // 4
            r0 = im.crop((0, 0, scw, sch))
            sc = target_h / float(sch)
            nw, nh = max(1, int(round(scw * sc))), max(1, int(round(sch * sc)))
            rz = r0.resize((nw, nh), Image.Resampling.LANCZOS)
            a = np.array(rz)
            a[:, :, 3] = np.where(a[:, :, 3] > 90, 255, 0).astype(np.uint8)
            rz = Image.fromarray(a, "RGBA")
            cell.paste(rz, (box_x - nw // 2, tch - nh - 5), rz)

        paste_fitted(tkt_p, 118, 180)
        paste_fitted(btb_p, 280, 156)
        paste_fitted(ttsc_p, 438, 178)

        full = Image.new("RGBA", (tcw, tch * 4), (0, 0, 0, 0))
        for r in range(4):
            full.paste(cell, (0, r * tch))
        full.save(aic_dir / "approved_information_complex_static.png", format="PNG")
        if (ORIG_CACHE_DIR / "approved_information_complex_static.png").exists():
            full.save(ORIG_CACHE_DIR / "approved_information_complex_static.png", format="PNG")

    # 3. bldg_observatory: replace flat MS-Paint cyan semicircle with 2.5D thien_nhan_vong_dai
    obs_dir = buildings_root / "bldg_observatory"
    tnvd_p = ORIG_CACHE_DIR / "thien_nhan_vong_dai_static.png"
    if not tnvd_p.exists():
        tnvd_p = buildings_root / "thien_nhan_vong_dai" / "thien_nhan_vong_dai_static.png"
    if obs_dir.exists() and tnvd_p.exists():
        obs_static = obs_dir / "bldg_observatory_static.png"
        ocw, och = Image.open(obs_static).width, Image.open(obs_static).height // 4
        src_im = Image.open(tnvd_p).convert("RGBA")
        scw, sch = src_im.width, src_im.height // 4
        r0 = src_im.crop((0, 0, scw, sch))
        sc = min((ocw - 4) / float(scw), (och - 4) / float(sch))
        nw, nh = max(1, int(round(scw * sc))), max(1, int(round(sch * sc)))
        rz = r0.resize((nw, nh), Image.Resampling.LANCZOS)
        a = np.array(rz)
        a[:, :, 3] = np.where(a[:, :, 3] > 90, 255, 0).astype(np.uint8)
        rz = Image.fromarray(a, "RGBA")
        cell = Image.new("RGBA", (ocw, och), (0, 0, 0, 0))
        cell.paste(rz, ((ocw - nw) // 2, och - nh - 2), rz)
        full = Image.new("RGBA", (ocw, och * 4), (0, 0, 0, 0))
        for r in range(4):
            full.paste(cell, (0, r * och))
        full.save(obs_static, format="PNG")
        if (ORIG_CACHE_DIR / "bldg_observatory_static.png").exists():
            full.save(ORIG_CACHE_DIR / "bldg_observatory_static.png", format="PNG")

    # 4. prop_water_basin: replace wooden grain barrels with 2.5D carved stone & timber water basin
    pwb_dir = buildings_root / "prop_water_basin"
    if pwb_dir.exists():
        pwb_static = pwb_dir / "prop_water_basin_static.png"
        wcw, wch = Image.open(pwb_static).width, Image.open(pwb_static).height // 4
        cell = build_carved_water_basin_cell(wcw, wch)
        full = Image.new("RGBA", (wcw, wch * 4), (0, 0, 0, 0))
        for r in range(4):
            full.paste(cell, (0, r * wch))
        full.save(pwb_static, format="PNG")
        if (ORIG_CACHE_DIR / "prop_water_basin_static.png").exists():
            full.save(ORIG_CACHE_DIR / "prop_water_basin_static.png", format="PNG")

    # 5. prop_mud_wallow: upgrade flat 13-color brown blob to rich 2.5D mud wallow
    pmw_dir = buildings_root / "prop_mud_wallow"
    if pmw_dir.exists():
        pmw_static = pmw_dir / "prop_mud_wallow_static.png"
        mcw, mch = Image.open(pmw_static).width, Image.open(pmw_static).height // 4
        cell = build_rich_mud_wallow_cell(mcw, mch)
        full = Image.new("RGBA", (mcw, mch * 4), (0, 0, 0, 0))
        for r in range(4):
            full.paste(cell, (0, r * mch))
        full.save(pmw_static, format="PNG")
        if (ORIG_CACHE_DIR / "prop_mud_wallow_static.png").exists():
            full.save(ORIG_CACHE_DIR / "prop_mud_wallow_static.png", format="PNG")


# ---------------------------------------------------------------------------
# Universal 2.5D Grounding Pass (Selective 1px Warm-Umber Outline + Plinth +
# Soft Contact Shadow alpha <= 54 + Mossy Meadow Tufts)
# ---------------------------------------------------------------------------
def ground_single_cell_25d(cell_im: Image.Image, seed_key: str = "") -> Image.Image:
    """
    Grounds a single building/prop cell `(cw, ch)` onto the meadow terrain:
    1. Strips harsh outer 2-3px pitch-black halo, warming the 1px selective outline to warm umber.
    2. Ensures 3-4px bottom margin so the lowest apex never clips at `y = ch - 1`.
    3. Adds a soft elliptical + contour-hugging ground contact shadow (`alpha <= 54`).
    4. Adds a weathered 2.5D stone/earth base skirt along the bottom contact profile.
    5. Places subtle mossy earth & olive-gold grass tufts overlapping the base corners.
    """
    cw, ch = cell_im.size
    arr = np.array(cell_im.convert("RGBA"), dtype=np.uint8)
    alpha = arr[:, :, 3]
    vis_ys, vis_xs = np.where(alpha > 25)
    if len(vis_ys) == 0:
        return cell_im

    ymin, ymax = int(vis_ys.min()), int(vis_ys.max())
    xmin, xmax = int(vis_xs.min()), int(vis_xs.max())

    # Step 1: Ensure at least 3-4px bottom clearance for the plinth & contact shadow
    target_bot_pad = min(4, max(2, ch // 36))
    cur_bot_pad = (ch - 1) - ymax
    if cur_bot_pad < target_bot_pad and ch >= 36:
        need = target_bot_pad - cur_bot_pad
        if ymin >= need:
            shifted = np.zeros_like(arr)
            shifted[0 : ch - need, :, :] = arr[need:ch, :, :]
            arr = shifted
        else:
            # Slightly compress height by `need` pixels using Lanczos, keeping crisp alpha
            new_h = max(8, ch - need)
            pil_tmp = Image.fromarray(arr, "RGBA").resize((cw, new_h), Image.Resampling.LANCZOS)
            tmp_arr = np.array(pil_tmp, dtype=np.uint8)
            tmp_arr[:, :, 3] = np.where(tmp_arr[:, :, 3] > 85, 255, 0).astype(np.uint8)
            shifted = np.zeros_like(arr)
            shifted[0:new_h, :, :] = tmp_arr
            arr = shifted

    alpha = arr[:, :, 3]
    solid = alpha > 140
    if not np.any(solid):
        return Image.fromarray(arr, "RGBA")

    # Step 2: Strip harsh 2-3px outer black halo & re-tint 1px outline to warm umber
    pad_s = np.pad(solid, 1, mode="constant", constant_values=False)
    eroded1 = (
        pad_s[1:-1, 1:-1]
        & pad_s[0:-2, 1:-1]
        & pad_s[2:, 1:-1]
        & pad_s[1:-1, 0:-2]
        & pad_s[1:-1, 2:]
    )
    ring1 = solid & (~eroded1)
    pad_e1 = np.pad(eroded1, 1, mode="constant", constant_values=False)
    eroded2 = (
        pad_e1[1:-1, 1:-1]
        & pad_e1[0:-2, 1:-1]
        & pad_e1[2:, 1:-1]
        & pad_e1[1:-1, 0:-2]
        & pad_e1[1:-1, 2:]
    )
    ring2 = eroded1 & (~eroded2)

    lum = 0.299 * arr[:, :, 0].astype(np.float32) + 0.587 * arr[:, :, 1].astype(np.float32) + 0.114 * arr[:, :, 2].astype(np.float32)
    dark1 = ring1 & (lum < 38.0)
    # Check if ring1 dark pixel is backed by a dark ring2 pixel (2px thick dark halo)
    pad_dark2 = np.pad(ring2 & (lum < 42.0), 1, mode="constant", constant_values=False)
    backed_by_dark2 = (
        pad_dark2[0:-2, 1:-1]
        | pad_dark2[2:, 1:-1]
        | pad_dark2[1:-1, 0:-2]
        | pad_dark2[1:-1, 2:]
    )
    extra_halo = dark1 & backed_by_dark2
    # Strip the redundant outer halo pixel on upper/side edges; keep warm selective 1px outline
    arr[extra_halo, 3] = 0
    solid = arr[:, :, 3] > 140
    pad_s = np.pad(solid, 1, mode="constant", constant_values=False)
    eroded1 = (
        pad_s[1:-1, 1:-1]
        & pad_s[0:-2, 1:-1]
        & pad_s[2:, 1:-1]
        & pad_s[1:-1, 0:-2]
        & pad_s[1:-1, 2:]
    )
    ring1 = solid & (~eroded1)
    dark_outline = ring1 & (lum < 48.0)
    # Warm-umber selective outline (#2E2118 on upper edges, #251B13 on lower edges)
    ys_grid = np.arange(ch)[:, None]
    upper_mask = dark_outline & (ys_grid < (ch * 0.65))
    lower_mask = dark_outline & (ys_grid >= (ch * 0.65))
    arr[upper_mask, 0] = 48
    arr[upper_mask, 1] = 35
    arr[upper_mask, 2] = 25
    arr[upper_mask, 3] = 235
    arr[lower_mask, 0] = 38
    arr[lower_mask, 1] = 28
    arr[lower_mask, 2] = 20
    arr[lower_mask, 3] = 245

    # Step 3: Compute bottom ground profile `y_bot[x]` across the base of the sprite
    vis_ys, vis_xs = np.where(solid)
    ymin, ymax = int(vis_ys.min()), int(vis_ys.max())
    xmin, xmax = int(vis_xs.min()), int(vis_xs.max())
    span_w = max(1, xmax - xmin + 1)
    span_h = max(1, ymax - ymin + 1)

    # For each column x, find the lowest solid pixel y_raw[x]
    y_raw = np.full(cw, -1, dtype=np.int32)
    for x in range(xmin, xmax + 1):
        col_ys = np.where(solid[:, x])[0]
        if len(col_ys) > 0:
            y_raw[x] = int(col_ys.max())

    # Only ground columns whose bottom pixel belongs to the lower base region (below 58% of sprite height)
    base_y_threshold = ymin + int(span_h * 0.56)
    valid_cols = np.where(y_raw >= base_y_threshold)[0]
    if len(valid_cols) == 0:
        valid_cols = np.where(y_raw >= 0)[0]

    bx_min, bx_max = int(valid_cols.min()), int(valid_cols.max())
    # Interpolate across any small gaps between posts/stilts to get a continuous base envelope `y_env[x]`
    y_env = np.interp(np.arange(cw), valid_cols, y_raw[valid_cols]).astype(np.int32)

    # Step 4: Paint soft elliptical + contour-hugging ground contact shadow (`alpha <= 54`)
    under_layer = np.zeros((ch, cw, 4), dtype=np.uint8)
    cx_base = 0.5 * (bx_min + bx_max)
    cy_base = float(np.percentile(y_raw[valid_cols], 72))
    rx_shadow = max(6.0, 0.54 * (bx_max - bx_min + 1))
    ry_shadow = max(4.0, min(18.0, 0.16 * span_h))

    yy, xx = np.mgrid[0:ch, 0:cw]
    # Elliptical base shadow cushion
    dx_n = (xx - cx_base) / rx_shadow
    dy_n = (yy - (cy_base + 1.0)) / ry_shadow
    dist2 = dx_n * dx_n + dy_n * dy_n
    ell_mask = (dist2 < 1.0) & (yy >= base_y_threshold - 2)
    ell_alpha = np.where(ell_mask, (1.0 - dist2) * 46.0, 0.0)

    # Contour-hugging ambient occlusion shadow (`1..5px` below `y_env[x]`)
    sh_depth = max(3, min(7, ch // 28))
    dy_env = yy - y_env[None, :]
    in_x_span = (xx >= max(0, bx_min - 3)) & (xx <= min(cw - 1, bx_max + 3))
    contour_mask = in_x_span & (dy_env >= -1) & (dy_env <= sh_depth)
    contour_alpha = np.where(
        contour_mask,
        np.clip((1.0 - (dy_env.astype(np.float32) + 1.0) / float(sh_depth + 2)) * 52.0, 0.0, 52.0),
        0.0,
    )

    shadow_alpha = np.clip(np.maximum(ell_alpha, contour_alpha), 0.0, 52.0).astype(np.uint8)
    # Warm dark olive-earth shadow tint `(24, 30, 16, alpha <= 52)`
    under_layer[:, :, 0] = 24
    under_layer[:, :, 1] = 30
    under_layer[:, :, 2] = 16
    under_layer[:, :, 3] = shadow_alpha

    # Step 5: Paint grounded 2.5D weathered stone / earth plinth skirt along `y_raw[x]`
    plinth_h = max(2, min(4, ch // 42))
    for x in valid_cols:
        yb = int(y_raw[x])
        if yb < base_y_threshold:
            continue
        # Light angle from upper-left: left side of base is slightly warmer/brighter than right side
        is_left = x < cx_base
        top_bevel = (116, 108, 90, 245) if is_left else (94, 88, 74, 245)
        mid_stone = (82, 80, 62, 242) if is_left else (68, 68, 52, 242)
        bot_earth = (64, 74, 42, 185)

        # Soften harsh black pixel at `(x, yb)` into grounded stone/timber plinth
        if lum[yb, x] < 65.0:
            arr[yb, x] = mid_stone
        if yb - 1 >= 0 and solid[yb - 1, x] and lum[yb - 1, x] < 55.0:
            arr[yb - 1, x] = top_bevel

        # Extend 1-2px grounded stone/moss skirt below `yb` (inside bounds)
        if yb + 1 < ch and arr[yb + 1, x, 3] < 120:
            arr[yb + 1, x] = mid_stone if (x % 3 != 0) else (74, 84, 48, 235)
        if plinth_h >= 3 and yb + 2 < ch and arr[yb + 2, x, 3] < 90:
            arr[yb + 2, x] = bot_earth

    # Composite `arr` over `under_layer` (keeping shadow strictly `alpha <= 54` where `arr` is transparent)
    out = under_layer.copy()
    fg_a = arr[:, :, 3:4].astype(np.float32) / 255.0
    bg_a = out[:, :, 3:4].astype(np.float32) / 255.0
    comb_a = fg_a + bg_a * (1.0 - fg_a)
    safe_a = np.where(comb_a > 1e-5, comb_a, 1.0)
    out[:, :, :3] = np.clip(
        (arr[:, :, :3].astype(np.float32) * fg_a + out[:, :, :3].astype(np.float32) * bg_a * (1.0 - fg_a)) / safe_a,
        0,
        255,
    ).astype(np.uint8)
    out[:, :, 3] = np.clip(comb_a[:, :, 0] * 255.0, 0, 255).astype(np.uint8)

    # Step 6: Add subtle 2.5D mossy earth pebbles & olive-gold meadow grass tufts at base corners & apex
    if len(valid_cols) >= 10 and ch >= 36:
        h_seed = int(hashlib.md5(seed_key.encode("utf-8")).hexdigest()[:8], 16)
        # Pick 5-7 anchor columns along the base (left corner, mid-left, center apex, mid-right, right corner)
        q_indices = [0.06, 0.22, 0.42, 0.52, 0.68, 0.84, 0.94]
        moss_dark = (58, 78, 32, 235)
        moss_mid = (96, 122, 46, 240)
        grass_tip = (138, 162, 66, 230)
        pebble_hi = (124, 114, 96, 235)
        pebble_sh = (78, 70, 56, 235)

        for idx_i, q in enumerate(q_indices):
            ci = int(round(q * (len(valid_cols) - 1)))
            tx = int(valid_cols[max(0, min(len(valid_cols) - 1, ci))])
            ty = int(y_raw[tx])
            if ty < base_y_threshold or ty >= ch - 1:
                continue
            # Small weathered pebble at even indices
            if (idx_i + h_seed) % 2 == 0 and 2 <= tx < cw - 2 and ty + 1 < ch:
                out[ty, tx] = pebble_hi
                out[min(ch - 1, ty + 1), tx] = pebble_sh
                if tx + 1 < cw:
                    out[min(ch - 1, ty + 1), tx + 1] = pebble_sh
            # 2-3px curved meadow grass blades overlapping the base edge
            for b_dx, b_h, col_b in ((-1, 2, moss_mid), (0, 3, grass_tip), (1, 2, moss_dark)):
                gx = tx + b_dx
                if 0 <= gx < cw:
                    for dy_b in range(b_h):
                        gy = ty - dy_b
                        if 0 <= gy < ch:
                            out[gy, gx] = grass_tip if dy_b == b_h - 1 else col_b

    return Image.fromarray(out, "RGBA")


# ---------------------------------------------------------------------------
# Multi-State 8-Frame 2.5D Animation Strip Synthesis
# (`_idle.png`, `_active.png`, `_attack.png`, `_damaged.png`)
# ---------------------------------------------------------------------------
def _draw_glow_dot(draw: ImageDraw.ImageDraw, x: float, y: float, r: int, core: Tuple[int, int, int, int], halo: Tuple[int, int, int, int]) -> None:
    ix, iy = int(round(x)), int(round(y))
    if r >= 2:
        draw.ellipse([ix - r - 1, iy - r - 1, ix + r + 1, iy + r + 1], fill=halo)
    draw.ellipse([ix - r, iy - r, ix + r, iy + r], fill=core)


def _draw_lightning_chain(
    draw: ImageDraw.ImageDraw,
    x0: float,
    y0: float,
    x1: float,
    y1: float,
    frame_idx: int,
    core_col: Tuple[int, int, int, int] = (225, 250, 255, 250),
    glow_col: Tuple[int, int, int, int] = (72, 188, 255, 185),
    steps: int = 5,
) -> None:
    pts = [(int(round(x0)), int(round(y0)))]
    for s in range(1, steps):
        u = s / float(steps)
        jx = math.sin(s * 2.7 + frame_idx * 1.9) * 4.5
        jy = math.cos(s * 3.1 - frame_idx * 1.5) * 3.0
        pts.append((int(round(x0 + (x1 - x0) * u + jx)), int(round(y0 + (y1 - y0) * u + jy))))
    pts.append((int(round(x1)), int(round(y1))))
    draw.line(pts, fill=glow_col, width=3)
    draw.line(pts, fill=core_col, width=1)


def _draw_smoke_column(
    draw: ImageDraw.ImageDraw,
    origin_x: float,
    origin_y: float,
    frame_idx: int,
    dark: bool = False,
    scale: float = 1.0,
) -> None:
    t = frame_idx / 8.0
    for i in range(4):
        p = (i * 0.25 + t) % 1.0
        sx = origin_x + math.sin(p * math.pi * 2.0 + i) * (3.5 * scale) + p * 4.0 * scale
        sy = origin_y - p * (18.0 * scale)
        rad = max(1, int(round((2.0 + p * 3.5) * scale)))
        alpha = int(max(20, min(195, (1.0 - p) * (190 if dark else 155))))
        if dark:
            col = (52, 48, 46, alpha)
            rim = (32, 28, 28, int(alpha * 0.7))
        else:
            col = (228, 224, 214, alpha)
            rim = (168, 162, 152, int(alpha * 0.65))
        draw.ellipse([int(sx - rad - 1), int(sy - rad), int(sx + rad + 1), int(sy + rad)], fill=rim)
        draw.ellipse([int(sx - rad), int(sy - rad), int(sx + rad), int(sy + rad)], fill=col)


def _find_feature_anchors(cell_arr: np.ndarray) -> Dict[str, Tuple[float, float]]:
    """Finds key visual anchors on the grounded cell (spire tip, upper core, muzzle, base center)."""
    ch, cw, _ = cell_arr.shape
    solid = cell_arr[:, :, 3] > 140
    ys, xs = np.where(solid)
    if len(ys) == 0:
        return {"top": (cw / 2, ch * 0.2), "upper": (cw / 2, ch * 0.35), "mid": (cw / 2, ch * 0.5), "base": (cw / 2, ch * 0.82)}
    ymin, ymax = int(ys.min()), int(ys.max())
    xmin, xmax = int(xs.min()), int(xs.max())
    top_xs = xs[ys <= ymin + max(3, (ymax - ymin) // 12)]
    top_x = float(np.median(top_xs)) if len(top_xs) > 0 else cw / 2.0

    # Find highest-saturation / brightest energy pixels (cyan/gold/emerald cores) in upper 65%
    r = cell_arr[:, :, 0].astype(np.float32)
    g = cell_arr[:, :, 1].astype(np.float32)
    b = cell_arr[:, :, 2].astype(np.float32)
    upper_zone = solid & (np.arange(ch)[:, None] < ymin + int((ymax - ymin) * 0.65))
    cyan_score = np.where(upper_zone, (b + g) - r * 1.1, -999.0)
    warm_score = np.where(upper_zone, (r + g * 0.8) - b * 1.1, -999.0)

    if cyan_score.max() > 140:
        cy_idx, cx_idx = np.unravel_index(int(np.argmax(cyan_score)), cyan_score.shape)
        core_pt = (float(cx_idx), float(cy_idx))
    elif warm_score.max() > 160:
        cy_idx, cx_idx = np.unravel_index(int(np.argmax(warm_score)), warm_score.shape)
        core_pt = (float(cx_idx), float(cy_idx))
    else:
        core_pt = (float(cw / 2.0), float(ymin + (ymax - ymin) * 0.34))

    return {
        "top": (top_x, float(ymin + 4)),
        "upper": core_pt,
        "mid": (float((xmin + xmax) * 0.5), float(ymin + (ymax - ymin) * 0.52)),
        "right_muzzle": (float(xmin + (xmax - xmin) * 0.78), float(ymin + (ymax - ymin) * 0.32)),
        "left_muzzle": (float(xmin + (xmax - xmin) * 0.22), float(ymin + (ymax - ymin) * 0.32)),
        "base": (float((xmin + xmax) * 0.5), float(ymin + (ymax - ymin) * 0.82)),
        "bbox": (float(xmin), float(ymin), float(xmax), float(ymax)),
    }


def _apply_upper_turret_motion(base_arr: np.ndarray, dx: int, dy: int, split_ratio: float = 0.58) -> np.ndarray:
    """Applies smooth tapered upper-structure recoil/breathing `(dx, dy)` that tapers to `(0, 0)` at `split_y` (zero seam!)."""
    if dx == 0 and dy == 0:
        return base_arr.copy()
    ch, cw, _ = base_arr.shape
    solid = base_arr[:, :, 3] > 100
    ys = np.where(solid)[0]
    if len(ys) == 0:
        return base_arr.copy()
    ymin, ymax = int(ys.min()), int(ys.max())
    split_y = max(ymin + 6, ymin + int((ymax - ymin) * split_ratio))

    out = base_arr.copy()
    for ty in range(0, split_y):
        # Smoothly taper displacement from 100% in the upper turret down to 0% at split_y
        w = max(0.0, min(1.0, (split_y - ty) / float(max(1, split_y - ymin))))
        sy = int(round(ty - dy * w))
        sx_shift = int(round(dx * w))
        if 0 <= sy < ch:
            if sx_shift == 0:
                out[ty, :, :] = base_arr[sy, :, :]
            elif sx_shift > 0:
                out[ty, :sx_shift, :] = 0
                out[ty, sx_shift:cw, :] = base_arr[sy, 0 : cw - sx_shift, :]
            else:
                sh = -sx_shift
                out[ty, cw - sh :, :] = 0
                out[ty, 0 : cw - sh, :] = base_arr[sy, sh:cw, :]
    return out


def _modulate_glow_channels(arr: np.ndarray, boost_cyan: float = 0.0, boost_warm: float = 0.0, darken: float = 0.0) -> np.ndarray:
    """Pulses existing glowing runes/windows/coils or darkens damaged surfaces."""
    out = arr.astype(np.float32)
    a = out[:, :, 3] > 120
    r, g, b = out[:, :, 0], out[:, :, 1], out[:, :, 2]

    if darken > 0.0:
        out[a, 0] *= (1.0 - darken * 0.85)
        out[a, 1] *= (1.0 - darken)
        out[a, 2] *= (1.0 - darken * 1.05)

    if abs(boost_cyan) > 1e-3:
        cyan_mask = a & ((b > r + 22) | (g > r + 22)) & ((g + b) > 160)
        out[cyan_mask, 0] = np.clip(out[cyan_mask, 0] + boost_cyan * 35.0, 0, 255)
        out[cyan_mask, 1] = np.clip(out[cyan_mask, 1] + boost_cyan * 55.0, 0, 255)
        out[cyan_mask, 2] = np.clip(out[cyan_mask, 2] + boost_cyan * 65.0, 0, 255)

    if abs(boost_warm) > 1e-3:
        warm_mask = a & (r > b + 35) & (r > 135) & (g > 85)
        out[warm_mask, 0] = np.clip(out[warm_mask, 0] + boost_warm * 50.0, 0, 255)
        out[warm_mask, 1] = np.clip(out[warm_mask, 1] + boost_warm * 42.0, 0, 255)
        out[warm_mask, 2] = np.clip(out[warm_mask, 2] + boost_warm * 20.0, 0, 255)

    return np.clip(out, 0, 255).astype(np.uint8)


def build_state_strip_8f(grounded_cell: Image.Image, behavior: str, state: str) -> Image.Image:
    """
    Generates an 8-frame horizontal sprite strip `(cw * 8, ch)` for `state` in
    `("idle", "active", "attack", "damaged")`.
    """
    cw, ch = grounded_cell.size
    base_arr = np.array(grounded_cell, dtype=np.uint8)
    anchors = _find_feature_anchors(base_arr)
    top_x, top_y = anchors["top"]
    up_x, up_y = anchors["upper"]
    mid_x, mid_y = anchors["mid"]
    rm_x, rm_y = anchors["right_muzzle"]
    lm_x, lm_y = anchors["left_muzzle"]
    bx0, by0, bx1, by1 = anchors["bbox"]
    span_w = max(12.0, bx1 - bx0)
    span_h = max(12.0, by1 - by0)

    strip = Image.new("RGBA", (cw * 8, ch), (0, 0, 0, 0))

    for f in range(8):
        phase = (2.0 * math.pi * f) / 8.0
        s_wave = math.sin(phase)
        c_wave = math.cos(phase)

        # 1. Base turret/crystal motion & channel glow modulation per state
        if state == "idle":
            dy = int(round(s_wave * 1.0)) if behavior in ("arc_tower", "thunder_citadel", "jade_sanctuary", "void_obelisk", "arcane_resonator") else 0
            dx = 0
            frame_arr = _apply_upper_turret_motion(base_arr, dx, dy, split_ratio=0.48)
            frame_arr = _modulate_glow_channels(frame_arr, boost_cyan=0.25 * s_wave, boost_warm=0.22 * c_wave)
        elif state == "active":
            dy = int(round(s_wave * 1.4)) if ch >= 70 else 0
            dx = int(round(c_wave * 0.7)) if behavior in ("crossbow", "skyhook", "dragon_cannon", "radar_dish") else 0
            frame_arr = _apply_upper_turret_motion(base_arr, dx, dy, split_ratio=0.50)
            frame_arr = _modulate_glow_channels(frame_arr, boost_cyan=0.45 + 0.35 * s_wave, boost_warm=0.38 + 0.30 * c_wave)
        elif state == "attack":
            # Dynamic recoil on frames 2, 3, 4
            recoil_map = [0, 1, -2, -3, -2, -1, 0, 0]
            rec = recoil_map[f]
            if behavior in ("crossbow", "dragon_cannon"):
                frame_arr = _apply_upper_turret_motion(base_arr, dx=rec, dy=-abs(rec) // 2, split_ratio=0.55)
            elif behavior == "skyhook":
                frame_arr = _apply_upper_turret_motion(base_arr, dx=-rec, dy=abs(rec) // 2, split_ratio=0.52)
            elif behavior in ("spike_trap", "landmine", "tar_trap"):
                lift = [0, 1, 3, 4, 2, 1, 0, 0][f]
                frame_arr = _apply_upper_turret_motion(base_arr, dx=0, dy=-lift, split_ratio=0.68)
            else:
                frame_arr = _apply_upper_turret_motion(base_arr, dx=0, dy=-abs(rec) // 2, split_ratio=0.48)
            burst_boost = 0.85 if f in (2, 3) else (0.45 if f in (1, 4) else 0.15)
            frame_arr = _modulate_glow_channels(frame_arr, boost_cyan=burst_boost, boost_warm=burst_boost)
        else:  # damaged
            jitter_x = 1 if f in (2, 6) else 0
            frame_arr = _apply_upper_turret_motion(base_arr, dx=jitter_x, dy=1, split_ratio=0.52)
            frame_arr = _modulate_glow_channels(frame_arr, boost_cyan=-0.35, boost_warm=-0.25, darken=0.22)

        frame_im = Image.fromarray(frame_arr, "RGBA")
        draw = ImageDraw.Draw(frame_im)

        # 2. State-specific 2.5D animated effects
        if state == "idle":
            if behavior in ("generator", "forge_hall", "incinerator_yard", "main_house"):
                # Rising chimney smoke wisps + warm furnace ember pulse
                chim_x = bx0 + span_w * (0.82 if behavior == "generator" else 0.72)
                chim_y = by0 + span_h * 0.12
                _draw_smoke_column(draw, chim_x, chim_y, f, dark=False, scale=max(0.7, span_h / 160.0))
                if behavior in ("generator", "forge_hall"):
                    _draw_glow_dot(draw, bx0 + span_w * 0.25, by0 + span_h * 0.35, 2, (165, 235, 255, 220), (54, 150, 245, 110))
            elif behavior in ("arc_tower", "thunder_citadel", "jade_sanctuary", "void_obelisk", "arcane_resonator", "lightning_rod", "pylon", "shield_dome", "emp_grid"):
                # Orbiting energy motes around upper crystal/orb
                col_c = (130, 255, 195, 240) if behavior == "jade_sanctuary" else (165, 240, 255, 240)
                col_h = (36, 175, 120, 120) if behavior == "jade_sanctuary" else (52, 155, 245, 120)
                rx_o = max(6.0, span_w * 0.22)
                ry_o = max(3.0, span_h * 0.08)
                for k in range(3):
                    ang = phase + k * (2.0 * math.pi / 3.0)
                    mx = up_x + math.cos(ang) * rx_o
                    my = up_y + math.sin(ang) * ry_o
                    _draw_glow_dot(draw, mx, my, 1, col_c, col_h)
            elif behavior in ("watchtower", "observatory", "radar_dish", "weather_station", "info_complex", "relay_tower", "beacon_lamp", "searchlight", "intel_pavilion"):
                # Rotating wind-vane / astrolabe glint & warm lantern flicker
                rx_v = max(5.0, span_w * 0.14)
                vx = top_x + math.cos(phase) * rx_v
                vy = top_y + 6.0 + math.sin(phase) * 3.0
                _draw_glow_dot(draw, vx, vy, 2, (255, 242, 168, 240), (242, 175, 58, 125))
                if behavior in ("watchtower", "relay_tower", "beacon_lamp"):
                    _draw_smoke_column(draw, bx0 + span_w * 0.26, by0 + span_h * 0.20, f, dark=False, scale=0.65)
            elif behavior == "feeder_trough":
                # Golden grain glints dancing across feed pile
                for k in range(3):
                    gx = mid_x - 10 + ((k * 9 + f * 3) % 20)
                    gy = by0 + span_h * 0.62 + ((k + f) % 3) * 2
                    _draw_glow_dot(draw, gx, gy, 1, (255, 246, 172, 245), (235, 175, 52, 140))
            elif behavior == "water_basin" or behavior == "cooling_fountain":
                # Concentric water surface ripple
                rr = 4 + (f % 4) * 3
                draw.ellipse([int(mid_x - rr), int(mid_y - rr * 0.45), int(mid_x + rr), int(mid_y + rr * 0.45)], outline=(205, 246, 255, 210 - (f % 4) * 35))
            elif behavior == "cauldron_shrine" or behavior == "research_lab":
                # Rising emerald/cyan alchemy bubbles
                for k in range(4):
                    p = (k * 0.25 + f / 8.0) % 1.0
                    bx = mid_x + math.sin(p * 6.0 + k) * 8.0
                    by = mid_y + 8.0 - p * 22.0
                    _draw_glow_dot(draw, bx, by, 1, (165, 255, 210, int((1.0 - p) * 240)), (42, 195, 130, int((1.0 - p) * 130)))
            elif behavior == "medical_tent":
                # Campfire flame dance in lower-right pit
                fx_c, fy_c = bx0 + span_w * 0.78, by0 + span_h * 0.84
                _draw_glow_dot(draw, fx_c, fy_c - (f % 3), 2, (255, 238, 142, 245), (245, 118, 32, 155))
            else:
                # Subtle golden/warm glint on turret ring
                gx = mid_x + math.cos(phase) * (span_w * 0.22)
                gy = mid_y + math.sin(phase) * (span_h * 0.08)
                _draw_glow_dot(draw, gx, gy, 1, (255, 236, 164, 220), (218, 152, 48, 110))

        elif state == "active":
            # Base resonance ring + intensified charging particles/arcs
            rx_b = max(10.0, span_w * 0.40)
            ry_b = max(4.0, span_h * 0.11)
            is_jade = behavior in ("jade_sanctuary", "cauldron_shrine", "research_lab")
            is_warm = behavior in ("crossbow", "generator", "forge_hall", "main_house", "medical_tent", "feeder_trough", "market_house")
            ring_col = (110, 250, 175, int(110 + 45 * s_wave)) if is_jade else ((255, 205, 88, int(110 + 45 * s_wave)) if is_warm else (88, 210, 255, int(115 + 45 * s_wave)))
            core_col = (210, 255, 230, 245) if is_jade else ((255, 248, 185, 245) if is_warm else (215, 248, 255, 245))
            halo_col = (42, 188, 122, 145) if is_jade else ((238, 148, 42, 145) if is_warm else (52, 162, 248, 145))

            draw.arc(
                [int(mid_x - rx_b), int(by1 - ry_b * 2.2), int(mid_x + rx_b), int(by1 - 2)],
                start=(f * 45) % 360,
                end=((f * 45) + 210) % 360,
                fill=ring_col,
                width=2,
            )
            # Spiraling charge particles converging toward upper core/muzzle
            for k in range(5):
                u = ((k / 5.0) + (1.0 - f / 8.0)) % 1.0
                ang = k * 1.25 + f * 0.85
                px = up_x + math.cos(ang) * (span_w * 0.34 * u)
                py = up_y + math.sin(ang) * (span_h * 0.22 * u)
                _draw_glow_dot(draw, px, py, 2 if u < 0.35 else 1, core_col, halo_col)

            if behavior in ("arc_tower", "thunder_citadel", "dragon_cannon", "lightning_rod", "pylon", "generator", "emp_grid", "shock_plate"):
                _draw_lightning_chain(draw, bx0 + span_w * 0.28, mid_y, up_x, up_y, f, core_col, halo_col, steps=4)
                _draw_lightning_chain(draw, bx0 + span_w * 0.72, mid_y, up_x, up_y, f + 3, core_col, halo_col, steps=4)
            if behavior in ("generator", "forge_hall", "incinerator_yard", "main_house"):
                _draw_smoke_column(draw, bx0 + span_w * 0.78, by0 + span_h * 0.12, f, dark=False, scale=max(0.85, span_h / 140.0))

        elif state == "attack":
            # Dynamic 8-frame firing sequence (windup f=0..1, climax burst f=2..4, recovery f=5..7)
            if behavior in ("crossbow", "guard_post", "fortified_gate"):
                # Crossbow bolt launch toward right muzzle with blazing golden-cyan bolt & shockwave rings
                mz_x, mz_y = bx0 + span_w * 0.86, by0 + span_h * 0.30
                if f in (0, 1):
                    _draw_glow_dot(draw, mz_x - f * 3, mz_y, 4 + f, (255, 250, 195, 250), (248, 172, 48, 185))
                elif f in (2, 3, 4, 5):
                    prog = (f - 2) / 3.0
                    bx_end = min(cw - 2, mz_x + 8 + prog * 14)
                    by_end = mz_y + prog * 4
                    # Shockwave ring at bow prodd
                    draw.ellipse([int(mz_x - 10), int(mz_y - 12), int(mz_x + 6), int(mz_y + 12)], outline=(255, 232, 130, 235), width=2)
                    # Blazing bolt shaft + arrowhead
                    draw.line([(int(mz_x - 22), int(mz_y - 5)), (int(bx_end), int(by_end))], fill=(255, 168, 38, 235), width=5)
                    draw.line([(int(mz_x - 20), int(mz_y - 4)), (int(bx_end), int(by_end))], fill=(255, 252, 215, 255), width=2)
                    _draw_glow_dot(draw, mz_x, mz_y, 6 - (f - 2), (255, 255, 225, 255), (255, 152, 36, 210))
                else:
                    _draw_smoke_column(draw, mz_x, mz_y, f, dark=False, scale=0.65)

            elif behavior == "skyhook":
                # Quad AA barrels firing upward-right with twin tracer streaks & flak burst
                m1_x, m1_y = bx0 + span_w * 0.42, by0 + span_h * 0.14
                m2_x, m2_y = bx0 + span_w * 0.82, by0 + span_h * 0.08
                if f in (1, 2, 3, 4):
                    for active_m in ((m1_x, m1_y), (m2_x, m2_y)):
                        _draw_glow_dot(draw, active_m[0], active_m[1], 7 if f in (2, 3) else 5, (255, 252, 215, 255), (255, 128, 32, 220))
                        draw.line(
                            [(int(active_m[0] - 10), int(active_m[1] + 12)), (int(min(cw - 2, active_m[0] + 12)), int(max(1, active_m[1] - 12)))],
                            fill=(255, 185, 58, 235),
                            width=4,
                        )
                        draw.line(
                            [(int(active_m[0] - 8), int(active_m[1] + 10)), (int(min(cw - 2, active_m[0] + 12)), int(max(1, active_m[1] - 12)))],
                            fill=(255, 252, 220, 255),
                            width=2,
                        )
                elif f >= 5:
                    _draw_smoke_column(draw, m1_x, m1_y, f, dark=False, scale=0.65)
                    _draw_smoke_column(draw, m2_x, m2_y, f, dark=False, scale=0.65)

            elif behavior == "dragon_cannon":
                # Dragon maw at upper-left (bx0 + 0.20*w, by0 + 0.24*h) unleashing azure-gold plasma blast
                maw_x, maw_y = bx0 + span_w * 0.20, by0 + span_h * 0.24
                if f in (0, 1):
                    _draw_glow_dot(draw, maw_x, maw_y, 5 + f * 2, (220, 252, 255, 250), (48, 182, 255, 195))
                elif f in (2, 3, 4):
                    rad = 11 - (f - 2) * 2
                    _draw_glow_dot(draw, maw_x - (f - 2) * 4, maw_y + (f - 2) * 2, rad, (240, 255, 255, 255), (42, 192, 255, 225))
                    draw.line([(int(maw_x + 4), int(maw_y - 2)), (int(max(2, maw_x - 26)), int(maw_y + 12))], fill=(58, 198, 255, 230), width=6)
                    draw.line([(int(maw_x + 4), int(maw_y - 2)), (int(max(2, maw_x - 26)), int(maw_y + 12))], fill=(240, 255, 255, 255), width=2)
                    _draw_lightning_chain(draw, maw_x, maw_y, max(2, maw_x - 24), maw_y + 10, f, (255, 248, 190, 255), (64, 198, 255, 215), steps=4)
                else:
                    _draw_smoke_column(draw, maw_x, maw_y, f, dark=False, scale=0.75)

            elif behavior == "jade_sanctuary":
                # Emerald spirit-jade shockwave nova + ascending lotus light pillars
                if f in (1, 2, 3, 4, 5):
                    prog = (f - 1) / 4.0
                    _draw_glow_dot(draw, up_x, up_y, 9 if f in (2, 3) else 6, (225, 255, 238, 255), (42, 215, 138, 215))
                    rx_s = span_w * (0.24 + 0.24 * prog)
                    ry_s = span_h * (0.09 + 0.09 * prog)
                    draw.ellipse([int(mid_x - rx_s), int(mid_y - ry_s), int(mid_x + rx_s), int(mid_y + ry_s)], outline=(148, 255, 202, 240), width=3)
                    _draw_lightning_chain(draw, up_x, up_y, bx0 + 18, by1 - 18, f, (220, 255, 235, 255), (46, 212, 138, 205), steps=5)
                    _draw_lightning_chain(draw, up_x, up_y, bx1 - 18, by1 - 18, f + 2, (220, 255, 235, 255), (46, 212, 138, 205), steps=5)

            elif behavior in ("arc_tower", "thunder_citadel", "lightning_rod", "pylon", "emp_grid", "shock_plate", "void_obelisk", "arcane_resonator", "shield_dome"):
                # Spire lightning discharge + radial shockwave ring
                if f in (1, 2, 3, 4):
                    _draw_glow_dot(draw, up_x, up_y, 6 if f in (2, 3) else 4, (235, 252, 255, 255), (68, 188, 255, 200))
                    _draw_lightning_chain(draw, up_x, up_y, bx0 + 6, by1 - 10, f, steps=5)
                    _draw_lightning_chain(draw, up_x, up_y, bx1 - 6, by1 - 10, f + 2, steps=5)
                    if f in (2, 3, 4):
                        prog = (f - 1) / 4.0
                        rx_s = span_w * (0.25 + 0.23 * prog)
                        ry_s = span_h * (0.08 + 0.08 * prog)
                        draw.ellipse([int(mid_x - rx_s), int(by1 - ry_s * 2.2), int(mid_x + rx_s), int(by1 - 2)], outline=(175, 242, 255, int(240 * (1.0 - prog * 0.5))), width=2)

            elif behavior == "spike_trap":
                # Sharp metallic spike slash glints thrusting upward
                if f in (2, 3, 4):
                    for sx in (mid_x - 14, mid_x, mid_x + 14):
                        draw.line([(int(sx), int(mid_y + 6)), (int(sx), int(mid_y - 14))], fill=(245, 250, 255, 250), width=2)
                        _draw_glow_dot(draw, sx, mid_y - 14, 2, (255, 248, 195, 250), (240, 168, 48, 165))

            elif behavior == "landmine":
                if f in (2, 3, 4, 5):
                    rad = 6 + (f - 2) * 3
                    _draw_glow_dot(draw, mid_x, mid_y - (f - 2) * 3, rad, (255, 242, 168, max(80, 255 - (f - 2) * 45)), (242, 96, 28, max(60, 210 - (f - 2) * 40)))

            else:
                # Universal active pulse wave for support/farm buildings when triggered
                prog = f / 7.0
                rx_s = span_w * (0.18 + 0.28 * prog)
                ry_s = span_h * (0.06 + 0.10 * prog)
                alpha_w = int(max(30, math.sin(prog * math.pi) * 215))
                draw.ellipse([int(mid_x - rx_s), int(mid_y - ry_s), int(mid_x + rx_s), int(mid_y + ry_s)], outline=(255, 232, 148, alpha_w), width=2)
                _draw_glow_dot(draw, up_x, up_y, 3, (255, 248, 195, alpha_w), (242, 172, 54, alpha_w // 2))

        else:  # state == "damaged"
            # Scorch cracks, dark smoke columns & intermittent red/amber warning sparks
            crack_pts = [
                (int(mid_x - span_w * 0.12), int(mid_y - span_h * 0.06)),
                (int(mid_x - span_w * 0.04), int(mid_y + span_h * 0.02)),
                (int(mid_x + span_w * 0.06), int(mid_y + span_h * 0.10)),
            ]
            draw.line(crack_pts, fill=(28, 20, 16, 230), width=2)
            _draw_smoke_column(draw, mid_x - span_w * 0.12, mid_y - span_h * 0.08, f, dark=True, scale=max(0.65, span_h / 150.0))
            if span_w >= 90:
                _draw_smoke_column(draw, mid_x + span_w * 0.16, mid_y + span_h * 0.04, (f + 4) % 8, dark=True, scale=max(0.55, span_h / 170.0))

            # Flickering warning embers & short-circuit sparks on frames 1, 2, 5, 6
            if f in (1, 2, 5, 6):
                sx = mid_x + (span_w * 0.10 if f in (1, 2) else -span_w * 0.10)
                sy = mid_y + (span_h * 0.05 if f % 2 == 0 else -span_h * 0.05)
                _draw_glow_dot(draw, sx, sy, 2, (255, 230, 130, 250), (245, 68, 32, 195))
                for k in range(3):
                    ang = k * 2.1 + f
                    draw.line([(int(sx), int(sy)), (int(sx + math.cos(ang) * 5), int(sy + math.sin(ang) * 5))], fill=(255, 195, 82, 235), width=1)

        strip.paste(frame_im, (f * cw, 0))

    return strip


def write_anim_json(json_path: Path, cw: int, ch: int, pivot: List[int], state: str) -> None:
    fps_map = {"idle": 8.0, "active": 10.0, "attack": 12.0, "damaged": 8.0}
    meta = {
        "source": "build_grounded_animated_buildings_25d.py",
        "state": state,
        "cell": [cw, ch],
        "rows": ["down"],
        "frames_per_row": 8,
        "frames": 8,
        "pivot_px_from_bottom_left": pivot,
        "fps": fps_map.get(state, 8.0),
        "loop": state != "attack",
    }
    if state == "attack":
        meta["effect_frame"] = 3
    json_path.write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")


# ---------------------------------------------------------------------------
# Visual Proof Sheet Generator (12 Key Towers & Farm Buildings on Farm_Main Grass)
# ---------------------------------------------------------------------------
def render_visual_proof_sheet(buildings_root: Path, terrain_root: Path, out_paths: List[Path]) -> None:
    showcase = [
        ("no_xuyen_van", "Nỏ Xuyên Vân (Crossbow)"),
        ("thap_ho_quang", "Tháp Hồ Quang (Arc Tower)"),
        ("thap_phong_khong_skyhook", "Tháp Phòng Không Skyhook"),
        ("dai_thien_loi", "Đài Thiên Lôi (Thunder Citadel)"),
        ("cuu_tieu_loi_phao", "Cửu Tiêu Lôi Pháo (Dragon Cannon)"),
        ("van_linh_hoi_nguyen_tran", "Vạn Linh Hồi Nguyên Trận"),
        ("dai_do_di_tuong", "Đài Dò Dị Tượng (Void Obelisk)"),
        ("may_phat_dien", "Máy Phát Điện (Steam Generator)"),
        ("may_chieu_khien", "Máy Chiếu Khiên (Shield Dome)"),
        ("thien_nhan_vong_dai", "Thiên Nhãn Vọng Đài (Observatory)"),
        ("approved_main_house", "Nhà Chính Nông Trại (Main House)"),
        ("prop_water_basin", "Bồn Nước Chăn Nuôi (Water Basin)"),
    ]
    states = [
        ("idle", "IDLE (Thở máy / Xoay / Khói nhẹ)", 2),
        ("active", "ACTIVE / CHARGING (Tích tụ năng lượng)", 4),
        ("attack", "ATTACK / FIRING (Khai hỏa / Xung kích)", 3),
        ("damaged", "DAMAGED (Mòn / Chập lửa / Bốc khói)", 2),
    ]

    meadow_p = terrain_root / "terrain_meadow.png"
    meadow_tile = Image.open(meadow_p).convert("RGBA") if meadow_p.exists() else Image.new("RGBA", (64, 64), (134, 161, 70, 255))
    # Scale meadow tile 2x with NEAREST so pixel-art grass texture matches 32 PPU buildings crisply
    meadow_tile = meadow_tile.resize((meadow_tile.width * 2, meadow_tile.height * 2), Image.Resampling.NEAREST)

    cols = 4
    rows = len(showcase)
    cell_w = 260
    cell_h = 210
    header_h = 74
    left_w = 240
    sheet_w = left_w + cols * cell_w + 24
    sheet_h = header_h + rows * cell_h + 24

    sheet = Image.new("RGBA", (sheet_w, sheet_h), (28, 34, 22, 255))
    draw = ImageDraw.Draw(sheet)

    try:
        font_title = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 16)
        font_sub = ImageFont.truetype(r"C:\Windows\Fonts\arial.ttf", 13)
        font_card = ImageFont.truetype(r"C:\Windows\Fonts\arialbd.ttf", 13)
        font_small = ImageFont.truetype(r"C:\Windows\Fonts\arial.ttf", 12)
    except Exception:
        font_title = font_sub = font_card = font_small = ImageFont.load_default()

    # Title banner
    draw.rectangle([0, 0, sheet_w, header_h - 6], fill=(36, 28, 22, 255))
    draw.line([(0, header_h - 6), (sheet_w, header_h - 6)], fill=(198, 156, 72, 255), width=2)
    draw.text(
        (20, 10),
        "PIG TYCOON 2.5D — GROUNDED DEFENSE TOWERS & MULTI-STATE BUILDING ANIMATIONS PROOF (32 PPU)",
        fill=(255, 232, 168, 255),
        font=font_title,
    )
    draw.text(
        (20, 32),
        "Every structure has a grounded 2.5D stone/earth plinth + soft AO contact shadow (alpha <= 54) + mossy base tufts + 4x 8-frame strips (_idle, _active, _attack, _damaged)",
        fill=(206, 218, 184, 255),
        font=font_sub,
    )

    for c_idx, (_, st_label, _) in enumerate(states):
        cx0 = left_w + c_idx * cell_w
        draw.text((cx0 + 14, 50), st_label, fill=(255, 218, 128, 255), font=font_card)

    for r_idx, (folder, label) in enumerate(showcase):
        ry0 = header_h + r_idx * cell_h
        # Left row header card
        draw.rectangle([12, ry0 + 6, left_w - 10, ry0 + cell_h - 6], fill=(44, 36, 28, 255), outline=(142, 112, 62, 255), width=1)
        draw.text((20, ry0 + cell_h // 2 - 24), f"#{r_idx + 1:02d} {label}", fill=(255, 238, 190, 255), font=font_card)
        draw.text((20, ry0 + cell_h // 2 + 2), f"Folder: {folder}/", fill=(180, 198, 156, 255), font=font_small)
        draw.text((20, ry0 + cell_h // 2 + 22), "4 strips x 8 frames + static", fill=(142, 212, 245, 255), font=font_small)

        for c_idx, (st_key, _, pick_f) in enumerate(states):
            cx0 = left_w + c_idx * cell_w
            pad_x0, pad_y0 = cx0 + 6, ry0 + 6
            pad_w, pad_h = cell_w - 12, cell_h - 12

            # Tile real Farm_Main grass (`terrain_meadow.png`) inside the viewport
            patch = Image.new("RGBA", (pad_w, pad_h), (134, 161, 70, 255))
            tw, th = meadow_tile.size
            for ty in range(0, pad_h, th):
                for tx in range(0, pad_w, tw):
                    patch.paste(meadow_tile, (tx, ty))

            strip_p = buildings_root / folder / f"{folder}_{st_key}.png"
            if strip_p.exists():
                st_im = Image.open(strip_p).convert("RGBA")
                fcw, fch = st_im.width // 8, st_im.height
                frame_im = st_im.crop((pick_f * fcw, 0, (pick_f + 1) * fcw, fch))
                # Fit into viewport while preserving crisp pixel look
                max_w, max_h = pad_w - 24, pad_h - 28
                if fcw > max_w or fch > max_h:
                    sc = min(max_w / float(fcw), max_h / float(fch))
                    nw, nh = max(1, int(round(fcw * sc))), max(1, int(round(fch * sc)))
                    frame_im = frame_im.resize((nw, nh), Image.Resampling.LANCZOS)
                elif fcw < 80 and fch < 80:
                    frame_im = frame_im.resize((fcw * 2, fch * 2), Image.Resampling.NEAREST)
                px = (pad_w - frame_im.width) // 2
                py = pad_h - frame_im.height - 10
                patch.alpha_composite(frame_im, (px, py))

            sheet.paste(patch, (pad_x0, pad_y0))
            draw.rectangle([pad_x0, pad_y0, pad_x0 + pad_w, pad_y0 + pad_h], outline=(98, 118, 58, 255), width=1)
            draw.text((pad_x0 + 8, pad_y0 + 6), f"{st_key}_f{pick_f} (8f)", fill=(34, 26, 18, 230), font=font_small)
            draw.text((pad_x0 + 7, pad_y0 + 5), f"{st_key}_f{pick_f} (8f)", fill=(255, 248, 210, 255), font=font_small)

    for out_p in out_paths:
        try:
            out_p.parent.mkdir(parents=True, exist_ok=True)
            sheet.save(out_p, format="PNG")
            print(f"[Proof] Saved visual proof sheet -> {out_p}")
        except Exception as exc:
            print(f"[Proof] Warning saving to {out_p}: {exc}")


# ---------------------------------------------------------------------------
# Main Pipeline
# ---------------------------------------------------------------------------
def main() -> None:
    buildings_fa = FROM_ARGON_PM / "Buildings"
    terrain_fa = FROM_ARGON_PM / "Terrain"
    decor_fa = FROM_ARGON_PM / "Decor"

    ORIG_CACHE_DIR.mkdir(parents=True, exist_ok=True)

    folders = sorted([d.name for d in buildings_fa.iterdir() if d.is_dir()])
    print(f"[Step 1] Caching originals & fixing cut-off/placeholder sheets across {len(folders)} folders...")
    for folder in folders:
        src_static = buildings_fa / folder / f"{folder}_static.png"
        cache_static = ORIG_CACHE_DIR / f"{folder}_static.png"
        if src_static.exists() and not cache_static.exists():
            shutil.copy2(src_static, cache_static)

    fix_special_static_sheets(buildings_fa)

    grounded_count = 0
    animated_count = 0

    print("[Step 2] Grounding all *_static.png sheets & generating 4x 8-frame animations for core Defense & Farm structures...")
    for folder in folders:
        bdir = buildings_fa / folder
        static_png = bdir / f"{folder}_static.png"
        static_json = bdir / f"{folder}_static.json"
        cache_static = ORIG_CACHE_DIR / f"{folder}_static.png"
        if not static_png.exists():
            continue

        src_im = Image.open(cache_static if cache_static.exists() else static_png).convert("RGBA")
        cw = src_im.width
        ch = src_im.height // 4 if src_im.height >= 4 else src_im.height

        jdata = {}
        if static_json.exists():
            try:
                jdata = json.loads(static_json.read_text(encoding="utf-8-sig"))
            except Exception:
                jdata = {}
        pivot = jdata.get("pivot_px_from_bottom_left", [cw // 2, max(4, ch // 18)])
        px_norm = max(0.0, min(1.0, pivot[0] / float(max(1, cw))))
        py_norm = max(0.0, min(1.0, pivot[1] / float(max(1, ch))))

        # Ground Row 0 and replicate across the 4 directional rows of *_static.png
        row0 = src_im.crop((0, 0, cw, ch))
        grounded_cell = ground_single_cell_25d(row0, seed_key=folder)
        grounded_full = Image.new("RGBA", (cw, ch * 4), (0, 0, 0, 0))
        for r in range(4):
            grounded_full.paste(grounded_cell, (0, r * ch))
        grounded_full.save(static_png, format="PNG")
        write_unity_meta(static_png, ppu=32, pivot_x=px_norm, pivot_y=py_norm)
        grounded_count += 1

        # Check if this structure gets the 4 multi-state 8-frame animation strips
        behavior = get_structure_behavior(folder)
        if behavior:
            for st in ("idle", "active", "attack", "damaged"):
                strip_im = build_state_strip_8f(grounded_cell, behavior=behavior, state=st)
                st_png = bdir / f"{folder}_{st}.png"
                st_json = bdir / f"{folder}_{st}.json"
                strip_im.save(st_png, format="PNG")
                write_anim_json(st_json, cw=cw, ch=ch, pivot=pivot, state=st)
                write_unity_meta(st_png, ppu=32, pivot_x=px_norm, pivot_y=py_norm)
            animated_count += 1

    print(f"  -> Grounded {grounded_count} *_static.png sheets.")
    print(f"  -> Generated {animated_count * 4} multi-frame 8f animation strips ({animated_count} structures x 4 states).")

    # Step 3: Ensure Terrain/ and Decor/ in from_argon also have .meta files at 32 PPU, then sync to Assets/Art/PixelMyth/
    print("[Step 3] Syncing Buildings/, Terrain/, and Decor/ to D:\\Pig-tycoon\\Assets\\Art\\PixelMyth\\...")
    for sub_name in ("Terrain", "Decor"):
        sdir = FROM_ARGON_PM / sub_name
        if sdir.exists():
            for p in sdir.glob("*.png"):
                m = Path(str(p) + ".meta")
                if not m.exists():
                    write_unity_meta(p, ppu=32, pivot_x=0.5, pivot_y=0.5)

    for sub_name in ("Buildings", "Terrain", "Decor"):
        src_sub = FROM_ARGON_PM / sub_name
        dst_sub = ASSETS_PM / sub_name
        if not src_sub.exists():
            continue
        shutil.copytree(src_sub, dst_sub, dirs_exist_ok=True)
        print(f"  -> Synced {sub_name}/ ({len(list(dst_sub.iterdir()))} entries)")

    # Step 4: Render visual proof sheet
    print("[Step 4] Rendering visual proof sheet on Farm_Main grass...")
    render_visual_proof_sheet(
        buildings_root=buildings_fa,
        terrain_root=terrain_fa,
        out_paths=[
            LOCAL_PROOF_PATH,
            PARENT_BRAIN_DIR / "grounded_25d_defense_towers_and_buildings_proof.png",
        ],
    )
    print("[DONE] All 2.5D grounded buildings & multi-state animations built and synced.")


if __name__ == "__main__":
    main()
