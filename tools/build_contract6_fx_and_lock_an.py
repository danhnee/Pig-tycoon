#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Deliver Contract #6 (Messages #168, #169, #170, #171):
1. 10 horizontal 8-frame 32-PPU (384x48, 8 frames of 48x48) 2.5D FX sprite strips in
   Assets/Art/PixelMyth/FX/ and from_argon/Assets/Art/PixelMyth/FX/:
   - fx_scoopfeed.png
   - fx_pourfeed.png
   - fx_drawwater.png
   - fx_pourwater.png
   - fx_repair.png
   - fx_dismantle.png
   - fx_place.png
   - fx_petpig.png
   - fx_clean.png
   - fx_pickup.png
2. 3 status/deny icons in Assets/Art/PixelMyth/UI/Icons/status/ (16x16 and 24x24):
   - need_feed_16.png, need_feed_24.png
   - need_water_16.png, need_water_24.png
   - tired_16.png, tired_24.png
3. Lock approved 80px C2-A An into .collab/claude-2d/argon-deliverables/LOCKED/an/ + sha256.txt (#169).
4. Add P0* flashback sepia memory styling in VnStoryDialoguePopup.cs and low-stamina (<20) shake/pulse in MythHudController.cs (#168/#171).
"""

import hashlib
import math
import os
import shutil
import uuid
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(r"D:\Pig-tycoon")
FROM_ARGON = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon"
FROM_CLAUDE = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_claude"
LOCKED_AN_DIR = ROOT / ".collab" / "claude-2d" / "argon-deliverables" / "LOCKED" / "an"


def write_unity_meta(png_path: Path, ppu: int = 32) -> None:
    meta_path = Path(str(png_path) + ".meta")
    guid = hashlib.md5(str(png_path.name).encode("utf-8")).hexdigest()[:32]
    if meta_path.exists():
        try:
            for line in meta_path.read_text(encoding="utf-8", errors="ignore").splitlines():
                if line.strip().startswith("guid:"):
                    guid = line.split(":", 1)[1].strip()
                    break
        except Exception:
            pass
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
  spritePivot: {{x: 0.5, y: 0.5}}
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
   ApplyToerd: 0
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
    spriteID: {uuid.uuid5(uuid.NAMESPACE_DNS, png_path.name).hex[:32]}
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


def put_dot(img: Image.Image, x: int, y: int, color: tuple, radius: int = 1, outline: tuple = None):
    w, h = img.size
    px = img.load()
    if outline is not None and radius >= 1:
        r_out = radius + 1
        for dy in range(-r_out, r_out + 1):
            for dx in range(-r_out, r_out + 1):
                if dx * dx + dy * dy <= r_out * r_out + 1:
                    nx, ny = int(round(x + dx)), int(round(y + dy))
                    if 0 <= nx < w and 0 <= ny < h:
                        if px[nx, ny][3] < outline[3]:
                            px[nx, ny] = outline
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            if dx * dx + dy * dy <= radius * radius:
                nx, ny = int(round(x + dx)), int(round(y + dy))
                if 0 <= nx < w and 0 <= ny < h:
                    px[nx, ny] = color


def draw_sparkle_4pt(img: Image.Image, cx: int, cy: int, arm: int, core_col: tuple, rim_col: tuple):
    w, h = img.size
    px = img.load()
    for d in range(-arm, arm + 1):
        col = core_col if abs(d) <= 1 else rim_col
        if 0 <= cx + d < w and 0 <= cy < h:
            px[cx + d, cy] = col
        if 0 <= cx < w and 0 <= cy + d < h:
            px[cx, cy + d] = col
    for dy in (-1, 1):
        for dx in (-1, 1):
            if 0 <= cx + dx < w and 0 <= cy + dy < h:
                px[cx + dx, cy + dy] = rim_col


def draw_heart(img: Image.Image, cx: int, cy: int, scale: float, fill_col: tuple, hi_col: tuple, out_col: tuple):
    w, h = img.size
    px = img.load()
    pattern = [
        " .xx.xx. ",
        ".xxxxxxx.",
        "xxxxxxxxx",
        "xxxxxxxxx",
        ".xxxxxxx.",
        " .xxxxx. ",
        "  .xxx.  ",
        "   .x.   ",
    ]
    pw, ph = len(pattern[0]), len(pattern)
    for py, row in enumerate(pattern):
        for pxx, ch in enumerate(row):
            if ch == "x":
                gx = int(round(cx + (pxx - pw // 2) * scale))
                gy = int(round(cy + (py - ph // 2) * scale))
                if 0 <= gx < w and 0 <= gy < h:
                    is_edge = (
                        py == 0
                        or py == ph - 1
                        or pxx == 0
                        or pxx == pw - 1
                        or pattern[max(0, py - 1)][pxx] != "x"
                        or pattern[min(ph - 1, py + 1)][pxx] != "x"
                        or pattern[py][max(0, pxx - 1)] != "x"
                        or pattern[py][min(pw - 1, pxx + 1)] != "x"
                    )
                    if is_edge:
                        px[gx, gy] = out_col
                    elif py <= 2 and pxx <= 3:
                        px[gx, gy] = hi_col
                    else:
                        px[gx, gy] = fill_col


def build_fx_strip(fx_name: str) -> Image.Image:
    frames = 8
    size = 48
    strip = Image.new("RGBA", (size * frames, size), (0, 0, 0, 0))

    for f in range(frames):
        t = f / float(frames - 1)
        fade = math.sin(max(0.05, min(0.98, t)) * math.pi)
        alpha_main = int(max(55, min(255, 255 * (1.0 - 0.55 * max(0.0, t - 0.45)))))
        alpha_soft = int(max(35, min(190, 175 * fade)))
        cell = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(cell)

        if fx_name == "fx_scoopfeed":
            # Golden grain arc rising from lower-left bin (12,36) to upper-right bag (36,16)
            for i in range(14):
                u = (i / 13.0) * 0.75 + t * 0.25
                if u > 1.0:
                    u -= 1.0
                x = 11 + u * 26
                y = 36 - u * 20 - math.sin(u * math.pi) * 11
                col = (252, 214, 92, alpha_main) if i % 2 == 0 else (218, 158, 54, alpha_main)
                put_dot(cell, x, y, col, radius=2 if i % 3 == 0 else 1, outline=(88, 52, 20, alpha_soft))
            # Warm bran dust puff
            for k in range(5):
                ang = k * 1.25 + t * 2.5
                dx = 16 + math.cos(ang) * (5 + t * 8)
                dy = 30 + math.sin(ang) * (4 + t * 5)
                put_dot(cell, dx, dy, (245, 222, 162, alpha_soft), radius=2)
            draw_sparkle_4pt(cell, int(24 + t * 10), int(18 - t * 4), 3, (255, 248, 196, alpha_main), (240, 186, 72, alpha_soft))

        elif fx_name == "fx_pourfeed":
            # Golden grain cascading downward into trough + warm dust & bouncing kernels
            for i in range(16):
                p = (i / 15.0 + t * 0.85) % 1.0
                x = 24 + math.sin(i * 1.7 + t * 4.0) * (3 + p * 7)
                y = 8 + p * 28
                col = (255, 220, 104, alpha_main) if i % 2 == 0 else (214, 152, 48, alpha_main)
                put_dot(cell, x, y, col, radius=2 if p > 0.6 else 1, outline=(86, 50, 18, alpha_soft))
            # Trough impact mound & dust cloud at bottom
            spread = 6 + int(t * 11)
            draw.ellipse([24 - spread, 34, 24 + spread, 41], fill=(235, 182, 74, alpha_soft), outline=(102, 62, 22, alpha_soft))
            for k in (-1, 1):
                put_dot(cell, 24 + k * (6 + t * 9), 34 - math.sin(t * math.pi) * 6, (255, 232, 148, alpha_main), radius=2)

        elif fx_name == "fx_drawwater":
            # Crystalline azure water column surging upward from well + concentric ripples
            rw = 8 + int(t * 8)
            draw.ellipse([24 - rw, 35, 24 + rw, 42], outline=(120, 216, 255, alpha_main), fill=(46, 138, 214, alpha_soft))
            for i in range(12):
                u = i / 11.0
                y = 36 - u * (14 + t * 16)
                x = 24 + math.sin(u * 4.5 + t * 5.0) * 4.5
                col = (196, 242, 255, alpha_main) if i % 3 == 0 else (68, 176, 245, alpha_main)
                put_dot(cell, x, y, col, radius=2, outline=(24, 72, 128, alpha_soft))
            for k in range(6):
                ang = -math.pi * 0.85 + k * 0.34
                r = 6 + t * 12
                put_dot(cell, 24 + math.cos(ang) * r, 24 + math.sin(ang) * r, (215, 248, 255, alpha_main), radius=1, outline=(30, 88, 148, alpha_soft))

        elif fx_name == "fx_pourwater":
            # Cascading azure stream downward + splash crown droplets
            for i in range(14):
                p = (i / 13.0 + t) % 1.0
                x = 22 + p * 4 + math.sin(i + t * 6.0) * 2.5
                y = 7 + p * 28
                col = (188, 240, 255, alpha_main) if i % 2 == 0 else (58, 168, 242, alpha_main)
                put_dot(cell, x, y, col, radius=2, outline=(22, 68, 122, alpha_soft))
            rw = 7 + int(t * 10)
            draw.ellipse([24 - rw, 34, 24 + rw, 41], fill=(74, 184, 248, alpha_soft), outline=(210, 246, 255, alpha_main))
            for k in range(7):
                ang = -math.pi * 0.9 + k * (math.pi * 0.8 / 6.0)
                dist = 5 + t * 13
                sx = 24 + math.cos(ang) * dist
                sy = 35 + math.sin(ang) * dist * 0.75
                put_dot(cell, sx, sy, (225, 250, 255, alpha_main), radius=1, outline=(28, 84, 142, alpha_soft))

        elif fx_name == "fx_repair":
            # Bright orange-gold anvil/hammer impact ring + flying sparks & wood chips
            ring_r = 4 + int(t * 14)
            draw.ellipse([24 - ring_r, 24 - ring_r, 24 + ring_r, 24 + ring_r], outline=(255, 214, 92, alpha_soft))
            for k in range(8):
                ang = k * (math.pi / 4.0) + 0.25
                dist = 3 + t * 16
                sx = 24 + math.cos(ang) * dist
                sy = 24 + math.sin(ang) * dist
                col = (255, 244, 168, alpha_main) if k % 2 == 0 else (255, 148, 52, alpha_main)
                put_dot(cell, sx, sy, col, radius=2 if k % 2 == 0 else 1, outline=(96, 42, 14, alpha_soft))
            if f <= 4:
                draw_sparkle_4pt(cell, 24, 24, 5 - f // 2, (255, 255, 220, 255), (255, 182, 68, 220))

        elif fx_name == "fx_dismantle":
            # Separating wooden plank fragments + dusty smoke puffs
            for k in range(6):
                ang = k * (math.pi / 3.0) + 0.4
                dist = 5 + t * 14
                px_c = int(24 + math.cos(ang) * dist)
                py_c = int(24 + math.sin(ang) * dist)
                put_dot(cell, px_c, py_c, (206, 192, 168, alpha_soft), radius=3, outline=(78, 62, 46, alpha_soft))
            for k in range(4):
                ang = k * (math.pi / 2.0) + t * 1.4
                dist = 4 + t * 12
                wx = int(24 + math.cos(ang) * dist)
                wy = int(24 + math.sin(ang) * dist)
                draw.rectangle([wx - 3, wy - 1, wx + 3, wy + 2], fill=(178, 118, 62, alpha_main), outline=(72, 42, 18, alpha_main))

        elif fx_name == "fx_place":
            # Expanding golden-emerald ground placement ring + settling dust puffs
            rx = 7 + int(t * 14)
            ry = 4 + int(t * 7)
            draw.ellipse([24 - rx, 32 - ry, 24 + rx, 32 + ry], outline=(134, 239, 172, alpha_main))
            draw.ellipse([24 - max(2, rx - 3), 32 - max(1, ry - 2), 24 + max(2, rx - 3), 32 + max(1, ry - 2)], outline=(253, 224, 71, alpha_soft))
            for k in range(6):
                ang = k * (math.pi / 3.0)
                dx = 24 + math.cos(ang) * rx
                dy = 32 + math.sin(ang) * ry - t * 4
                put_dot(cell, dx, dy, (226, 208, 168, alpha_soft), radius=2, outline=(82, 66, 44, alpha_soft))
            draw_sparkle_4pt(cell, 24, int(26 - t * 10), 3, (255, 250, 190, alpha_main), (134, 239, 172, alpha_soft))

        elif fx_name == "fx_petpig":
            # Floating coral-rose hearts + golden 4-point sparkles rising above pig
            hy1 = int(32 - t * 18)
            hx1 = int(20 + math.sin(t * math.pi * 2) * 3)
            draw_heart(cell, hx1, hy1, 1.2, (255, 108, 138, alpha_main), (255, 198, 212, alpha_main), (112, 28, 48, alpha_main))
            hy2 = int(36 - max(0.0, t - 0.15) * 18)
            hx2 = int(30 - math.sin(t * math.pi * 2) * 2)
            draw_heart(cell, hx2, hy2, 0.9, (255, 138, 162, alpha_soft), (255, 214, 224, alpha_soft), (112, 28, 48, alpha_soft))
            draw_sparkle_4pt(cell, int(14 + t * 6), int(22 - t * 8), 2, (255, 248, 190, alpha_main), (255, 198, 88, alpha_soft))
            draw_sparkle_4pt(cell, int(34 - t * 4), int(18 - t * 6), 3, (255, 248, 190, alpha_main), (255, 198, 88, alpha_soft))

        elif fx_name == "fx_clean":
            # White lime-wash bubbles + bright cyan-gold hygiene sparkles sweeping across
            for k in range(7):
                u = (k / 6.0 + t * 0.5) % 1.0
                bx = int(10 + k * 4.5 + math.sin(t * 5 + k) * 2)
                by = int(36 - u * 24)
                br = 2 + (k % 2)
                draw.ellipse([bx - br, by - br, bx + br, by + br], fill=(232, 250, 255, alpha_soft), outline=(88, 188, 232, alpha_main))
                put_dot(cell, bx - 1, by - 1, (255, 255, 255, alpha_main), radius=0)
            draw_sparkle_4pt(cell, int(16 + t * 16), int(16 + math.sin(t * math.pi) * 6), 3, (255, 255, 255, alpha_main), (104, 228, 255, alpha_main))
            draw_sparkle_4pt(cell, int(32 - t * 12), int(28 - t * 12), 2, (255, 248, 188, alpha_main), (134, 239, 172, alpha_soft))

        elif fx_name == "fx_pickup":
            # Ascending golden-cyan light trail + star crest
            cy = int(36 - t * 24)
            for trail in range(6):
                ty = cy + trail * 3
                if ty < 44:
                    rad = max(1, 3 - trail // 2)
                    put_dot(cell, 24 + math.sin(t * 6 + trail) * 2, ty, (255, 232, 128, max(40, alpha_main - trail * 32)), radius=rad, outline=(112, 74, 18, max(30, alpha_soft - trail * 25)))
            draw_sparkle_4pt(cell, 24, cy, 4, (255, 255, 230, alpha_main), (255, 208, 76, alpha_main))
            draw_sparkle_4pt(cell, int(17 + t * 4), max(8, cy + 5), 2, (165, 243, 252, alpha_soft), (56, 189, 248, alpha_soft))

        strip.paste(cell, (f * size, 0), cell)

    return strip


def build_status_icon(kind: str, dim: int) -> Image.Image:
    img = Image.new("RGBA", (dim, dim), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cx, cy = dim // 2, dim // 2
    r = dim // 2 - 1

    # Warm dark medallion backing with bronze-red alert rim so icon reads clearly over any terrain
    draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(32, 22, 20, 235), outline=(228, 108, 72, 255))
    if dim >= 20:
        draw.ellipse([cx - r + 1, cy - r + 1, cx + r - 1, cy + r - 1], outline=(92, 42, 32, 220))

    if kind == "need_feed":
        # Golden grain mound + wooden trough bowl
        bw = max(4, dim // 3)
        bh = max(3, dim // 5)
        # Golden grain heap on top
        draw.ellipse([cx - bw, cy - bh - 2, cx + bw, cy + bh - 1], fill=(252, 211, 77, 255), outline=(120, 72, 20, 255))
        # Wooden bowl lower half
        draw.polygon(
            [(cx - bw - 1, cy), (cx + bw + 1, cy), (cx + bw - 1, cy + bh + 2), (cx - bw + 1, cy + bh + 2)],
            fill=(180, 114, 56, 255),
            outline=(78, 44, 18, 255),
        )
        put_dot(img, cx - 1, cy - 2, (255, 248, 200, 255), radius=0)

    elif kind == "need_water":
        # Crystalline azure water droplet
        top_y = max(2, cy - r + 3)
        bot_y = min(dim - 3, cy + r - 3)
        w_drop = max(3, dim // 4)
        draw.polygon(
            [(cx, top_y), (cx + w_drop, cy + 1), (cx, bot_y), (cx - w_drop, cy + 1)],
            fill=(56, 189, 248, 255),
            outline=(20, 78, 128, 255),
        )
        draw.ellipse([cx - w_drop, cy - 1, cx + w_drop, bot_y], fill=(56, 189, 248, 255), outline=(20, 78, 128, 255))
        put_dot(img, cx - 1, cy + 1, (224, 248, 255, 255), radius=1 if dim >= 20 else 0)

    elif kind == "tired":
        # Amber-orange lightning bolt + sweat droplet
        pts = [
            (cx + 1, max(2, cy - r + 3)),
            (cx - 3, cy),
            (cx, cy),
            (cx - 2, min(dim - 3, cy + r - 3)),
            (cx + 3, cy - 1),
            (cx, cy - 1),
        ]
        draw.polygon(pts, fill=(251, 191, 36, 255), outline=(120, 53, 15, 255))
        put_dot(img, cx + max(3, dim // 4), cy - 2, (125, 211, 252, 255), radius=1 if dim >= 20 else 0, outline=(22, 78, 128, 230))

    return img


def lock_approved_an() -> int:
    src_an = FROM_ARGON / "Assets" / "Art" / "PixelMyth" / "Characters" / "an"
    src_portraits = FROM_ARGON / "Assets" / "Art" / "PixelMyth" / "UI" / "Portraits"
    LOCKED_AN_DIR.mkdir(parents=True, exist_ok=True)

    copied_files = []
    if src_an.exists():
        for f in sorted(src_an.iterdir()):
            if f.is_file() and f.suffix.lower() in (".png", ".json"):
                dst = LOCKED_AN_DIR / f.name
                shutil.copy2(f, dst)
                copied_files.append(dst)

    if src_portraits.exists():
        portraits_dst = LOCKED_AN_DIR / "portraits"
        portraits_dst.mkdir(parents=True, exist_ok=True)
        for f in sorted(src_portraits.iterdir()):
            if f.is_file() and f.name.lower().startswith("an_") and f.suffix.lower() == ".png":
                dst = portraits_dst / f.name
                shutil.copy2(f, dst)
                copied_files.append(dst)

    lines = []
    for p in sorted(copied_files, key=lambda x: str(x.relative_to(LOCKED_AN_DIR))):
        h = hashlib.sha256(p.read_bytes()).hexdigest()
        rel = str(p.relative_to(LOCKED_AN_DIR)).replace("\\", "/")
        lines.append(f"{h}  {rel}")
    (LOCKED_AN_DIR / "sha256.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return len(copied_files)


def patch_vn_flashback_and_stamina_shake() -> list:
    patched = []
    # 1. Patch VnStoryDialoguePopup.cs for P0* flashback sepia memory styling (#168/#171)
    vn_paths = [
        FROM_ARGON / "Assets" / "Scripts" / "UI" / "VnStoryDialoguePopup.cs",
        ROOT / "Assets" / "Scripts" / "UI" / "VnStoryDialoguePopup.cs",
    ]
    for vp in vn_paths:
        if not vp.exists():
            continue
        txt = vp.read_text(encoding="utf-8")
        target = "            // 1. Backdrop Layer\n            UpdateBackdropVisual(runner.BackdropId, runner.Scene?.Title);"
        replacement = """            // 1. Backdrop Layer + P0* Flashback Memory Sepia Frame (#168/#171)
            bool isFlashbackMemory = runner.Scene != null && !string.IsNullOrEmpty(runner.Scene.Id) &&
                                     runner.Scene.Id.StartsWith("P0", StringComparison.OrdinalIgnoreCase);
            UpdateBackdropVisual(runner.BackdropId, runner.Scene?.Title);
            if (backdropImage != null && backdropImage.sprite != null)
            {
                backdropImage.color = isFlashbackMemory
                    ? new Color(1.0f, 0.92f, 0.78f, 1.0f)
                    : Color.white;
            }"""
        if target in txt and "isFlashbackMemory" not in txt:
            txt = txt.replace(target, replacement, 1)
            vp.write_text(txt, encoding="utf-8")
            patched.append(str(vp))

    # 2. Find MythHudController.cs across FROM_ARGON and ROOT and add low-stamina (<20) bar pulse/shake (#168/#171)
    hud_candidates = list(FROM_ARGON.rglob("MythHudController.cs")) + list((ROOT / "Assets").rglob("MythHudController.cs"))
    for hp in hud_candidates:
        if not hp.exists():
            continue
        txt = hp.read_text(encoding="utf-8")
        if "lowStaminaShakeOffset" in txt:
            continue
        target = "staminaFill.fillAmount = Mathf.Clamp01(c.Stamina / maxStam);"
        replacement = """staminaFill.fillAmount = Mathf.Clamp01(c.Stamina / maxStam);
                // #168/#171: Low stamina (< 20) bar shake & sweat warning pulse
                if (staminaFill.rectTransform != null && staminaFill.transform.parent is RectTransform stamBarRt)
                {
                    bool lowStaminaShakeOffset = c.Stamina < 20f && !c.IsFainted;
                    if (lowStaminaShakeOffset)
                    {
                        float sx = Mathf.Sin(Time.unscaledTime * 22f) * 2.2f;
                        staminaFill.color = Color.Lerp(new Color(0.95f, 0.55f, 0.18f, 1f), new Color(0.98f, 0.28f, 0.22f, 1f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f));
                        stamBarRt.localRotation = Quaternion.Euler(0f, 0f, sx * 0.45f);
                    }
                    else
                    {
                        stamBarRt.localRotation = Quaternion.identity;
                    }
                }"""
        if target in txt:
            txt = txt.replace(target, replacement, 1)
            hp.write_text(txt, encoding="utf-8")
            patched.append(str(hp))

    return patched


def main():
    fx_names = [
        "fx_scoopfeed",
        "fx_pourfeed",
        "fx_drawwater",
        "fx_pourwater",
        "fx_repair",
        "fx_dismantle",
        "fx_place",
        "fx_petpig",
        "fx_clean",
        "fx_pickup",
    ]
    fx_dirs = [
        FROM_ARGON / "Assets" / "Art" / "PixelMyth" / "FX",
        ROOT / "Assets" / "Art" / "PixelMyth" / "FX",
    ]
    for d in fx_dirs:
        d.mkdir(parents=True, exist_ok=True)

    for name in fx_names:
        strip = build_fx_strip(name)
        for d in fx_dirs:
            out_png = d / f"{name}.png"
            strip.save(out_png, format="PNG")
            write_unity_meta(out_png, ppu=32)

    status_dirs = [
        FROM_ARGON / "Assets" / "Art" / "PixelMyth" / "UI" / "Icons" / "status",
        ROOT / "Assets" / "Art" / "PixelMyth" / "UI" / "Icons" / "status",
    ]
    for d in status_dirs:
        d.mkdir(parents=True, exist_ok=True)

    for kind in ("need_feed", "need_water", "tired"):
        for dim in (16, 24):
            icon = build_status_icon(kind, dim)
            for d in status_dirs:
                out_png = d / f"{kind}_{dim}.png"
                icon.save(out_png, format="PNG")
                write_unity_meta(out_png, ppu=32)

    locked_count = lock_approved_an()
    patched = patch_vn_flashback_and_stamina_shake()
    print(f"[OK] Generated {len(fx_names)} 8-frame FX strips (384x48 @ 32PPU), 6 status/deny icons, locked {locked_count} An files in LOCKED/an/, patched {len(patched)} UI scripts.")


if __name__ == "__main__":
    main()
