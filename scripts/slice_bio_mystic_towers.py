#!/usr/bin/env python3
import os
import math
import uuid
import re
from PIL import Image

def main():
    src_path = "Assets/Art/Sprites/Environment/Buildings/bio_mystic_towers_spritesheet.jpg"
    bldg_dir = "Assets/Art/Sprites/Environment/Buildings"
    out_dir = os.path.join(bldg_dir, "BioTowers")
    hires_dir = os.path.join(out_dir, "hires")
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(hires_dir, exist_ok=True)

    # Folder meta for BioTowers
    folder_meta_path = out_dir + ".meta"
    if not os.path.exists(folder_meta_path):
        folder_guid = uuid.uuid4().hex
        with open(folder_meta_path, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {folder_guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

    # Sheet meta
    sheet_meta_path = src_path + ".meta"
    if not os.path.exists(sheet_meta_path):
        sheet_guid = uuid.uuid4().hex
        with open(sheet_meta_path, "w") as f:
            f.write(f"fileFormatVersion: 2\nguid: {sheet_guid}\nTextureImporter:\n  serializedVersion: 13\n  textureType: 8\n  spritePixelsToUnits: 32\n  filterMode: 0\n")

    img = Image.open(src_path).convert("RGB")
    w, h = img.size

    # Background color estimation (dark charcoal/slate)
    bg_r, bg_g, bg_b = 45.0, 46.0, 57.0

    def extract_alpha(crop_img):
        cw, ch = crop_img.size
        rgba = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
        crop_rgb = crop_img.convert("RGB")
        
        for y in range(ch):
            for x in range(cw):
                r, g, b = crop_rgb.getpixel((x, y))
                # Euclidean distance in RGB
                dist = math.sqrt((r - bg_r)**2 + (g - bg_g)**2 + (b - bg_b)**2)
                
                # Thresholds
                if dist <= 14.0:
                    rgba.putpixel((x, y), (0, 0, 0, 0))
                elif dist < 34.0:
                    # Smooth edge antialiasing
                    t = (dist - 14.0) / 20.0
                    alpha = int(t * 255)
                    # Unmatte background: estimate true foreground color
                    un_r = int(max(0, min(255, (r - (1 - t) * bg_r) / max(0.01, t))))
                    un_g = int(max(0, min(255, (g - (1 - t) * bg_g) / max(0.01, t))))
                    un_b = int(max(0, min(255, (b - (1 - t) * bg_b) / max(0.01, t))))
                    rgba.putpixel((x, y), (un_r, un_g, un_b, alpha))
                else:
                    rgba.putpixel((x, y), (r, g, b, 255))
        return rgba

    # 5 Towers x 4 Frames (0=idle, 1=charge, 2=fire, 3=cooldown)
    # Centers in X in the spritesheet for base alignment across all 4 frames
    towers = [
        ("UGaiDocGamNhom", 
         [(117, 40, 305, 170), (385, 40, 572, 170), (645, 40, 895, 170), (924, 40, 1111, 170)],
         [210.0, 478.0, 750.0, 1018.0]),
        ("TruNamBaoTuMu", 
         [(123, 172, 303, 358), (387, 172, 571, 358), (600, 172, 895, 358), (938, 172, 1099, 358)],
         [217.0, 483.0, 747.0, 1021.0]),
        ("OngBomDichAxit", 
         [(146, 380, 292, 532), (412, 380, 557, 532), (650, 380, 850, 532), (948, 380, 1090, 532)],
         [229.0, 496.0, 740.0, 1033.0]),
        ("CotToHutHon", 
         [(159, 545, 270, 715), (418, 545, 543, 715), (670, 545, 900, 715), (964, 545, 1077, 715)],
         [214.0, 477.0, 742.0, 1017.0]),
        ("CotDiCotSuongHuyen", 
         [(156, 720, 270, 895), (423, 720, 538, 895), (630, 720, 900, 895), (965, 720, 1078, 895)],
         [212.0, 480.0, 764.0, 1021.0])
    ]

    suffixes = ["idle", "charge", "fire", "cooldown"]

    TARGET_W = 160
    TARGET_H = 112
    CANVAS_BASE_X = 80
    CANVAS_BASE_Y = 96

    for bp_id, boxes, centers_x in towers:
        print(f"\nProcessing Tower: {bp_id}")
        
        # 1. Determine canonical scale from Frame 0 (idle)
        raw_idle = img.crop(boxes[0])
        alpha_idle = extract_alpha(raw_idle)
        bbox_idle = alpha_idle.getbbox()
        tight_idle = alpha_idle.crop(bbox_idle) if bbox_idle else alpha_idle
        tw_idle, th_idle = tight_idle.size
        canonical_scale = min(58.0 / tw_idle, 70.0 / th_idle)
        print(f"  Canonical scale: {canonical_scale:.4f} (from idle {tw_idle}x{th_idle})")

        for f_idx, box in enumerate(boxes):
            suffix = suffixes[f_idx]
            raw_crop = img.crop(box)
            alpha_crop = extract_alpha(raw_crop)

            # Auto-crop tight bounding box around non-zero alpha for Hi-Res archive
            bbox = alpha_crop.getbbox()
            tight_crop = alpha_crop.crop(bbox) if bbox else alpha_crop
            hires_name = f"{bp_id}_{suffix}.png"
            tight_crop.save(os.path.join(hires_dir, hires_name))

            # Scale to game size using fixed canonical scale (Phương Án 1)
            new_w = max(1, int(round(alpha_crop.size[0] * canonical_scale)))
            new_h = max(1, int(round(alpha_crop.size[1] * canonical_scale)))
            scaled = alpha_crop.resize((new_w, new_h), Image.Resampling.LANCZOS)

            # Anchor alignment: center base at CANVAS_BASE_X (32) and CANVAS_BASE_Y (74)
            local_center_x = centers_x[f_idx] - box[0]
            scaled_center_x = local_center_x * canonical_scale
            s_bbox = scaled.getbbox()
            scaled_bottom_y = s_bbox[3] if s_bbox else new_h

            paste_x = int(round(CANVAS_BASE_X - scaled_center_x))
            paste_y = CANVAS_BASE_Y - scaled_bottom_y

            # Place onto 64x80 canvas
            canvas = Image.new("RGBA", (TARGET_W, TARGET_H), (0, 0, 0, 0))
            canvas.paste(scaled, (paste_x, paste_y), scaled)

            out_filename = f"{bp_id}_{suffix}.png"
            out_filepath = os.path.join(out_dir, out_filename)
            canvas.save(out_filepath)

            c_bbox = canvas.getbbox()
            cw = c_bbox[2] - c_bbox[0] if c_bbox else 0
            ch = c_bbox[3] - c_bbox[1] if c_bbox else 0
            print(f"  -> Saved {out_filename} (game bbox: {cw}x{ch}, bottom: {c_bbox[3] if c_bbox else 0})")

            # Preserve existing Unity .meta GUID
            meta_filepath = out_filepath + ".meta"
            guid = uuid.uuid4().hex
            if os.path.exists(meta_filepath):
                with open(meta_filepath, "r") as mf_read:
                    m = re.search(r"guid:\s*([a-f0-9]{32})", mf_read.read())
                    if m:
                        guid = m.group(1)

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
  spritePivot: {{x: 0.5, y: 0.142857}}
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
            with open(meta_filepath, "w") as mf:
                mf.write(meta_content)

    print("\n✅ Successfully updated all 20 Bio & Mystic Tower sprites with Phương Án 1 (Fixed Canonical Scale & Base Anchor)!")

if __name__ == "__main__":
    main()
