import os
import uuid
from PIL import Image

IMG1_PATH = '/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/.user_uploaded/media_1791879345199.jpg'
IMG2_PATH = '/home/danh/.gemini/antigravity/brain/e605a811-d043-4617-bbe7-f0f18bf72b28/.user_uploaded/media_1791879360800.jpg'

def generate_meta(dest_path):
    guid = uuid.uuid4().hex
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
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaIsTransparency: 1
  spriteChildren: []
  androidETC2FallbackOverride: 0
  editorCurves: {{}}
  platformSettings:
  - serializedVersion: 3
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
    forceMaximumCompressionQuality_BC7: 0
"""
    with open(dest_path + '.meta', 'w') as f:
        f.write(meta_content)

def extract_and_clean_sprite(img, crop_box, target_size=(64, 64), target_h=54, feet_y=60, is_vehicle=False):
    crop = img.crop(crop_box).convert('RGBA')
    w, h = crop.size
    data = crop.load()
    
    # 1. Flood fill from 4 borders to remove background
    visited = set()
    queue = []
    for x in range(w):
        queue.append((x, 0))
        queue.append((x, h - 1))
    for y in range(h):
        queue.append((0, y))
        queue.append((w - 1, y))
        
    def is_bg(r, g, b):
        return r > 225 and g > 225 and b > 225
        
    while queue:
        x, y = queue.pop(0)
        if (x, y) in visited:
            continue
        visited.add((x, y))
        r, g, b, a = data[x, y]
        if is_bg(r, g, b):
            data[x, y] = (0, 0, 0, 0)
            for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in visited:
                    queue.append((nx, ny))
                    
    # Clean any remnant pure white halo
    for y in range(h):
        for x in range(w):
            r, g, b, a = data[x, y]
            if a > 0 and r > 235 and g > 235 and b > 235:
                data[x, y] = (0, 0, 0, 0)
            elif a > 0 and min(r, g, b) > 215 and (max(r, g, b) - min(r, g, b)) < 15:
                # Pale grayish artifact from jpeg compression
                data[x, y] = (0, 0, 0, 0)

    bbox = crop.getbbox()
    if not bbox:
        return Image.new('RGBA', target_size, (0, 0, 0, 0))
        
    fg = crop.crop(bbox)
    fg_w, fg_h = fg.size
    
    if is_vehicle:
        # Fit inside target_size maintaining aspect ratio
        scale = min((target_size[0] - 8) / fg_w, (target_size[1] - 8) / fg_h)
        new_w = max(1, int(fg_w * scale))
        new_h = max(1, int(fg_h * scale))
        fg_resized = fg.resize((new_w, new_h), Image.Resampling.LANCZOS)
        canvas = Image.new('RGBA', target_size, (0, 0, 0, 0))
        paste_x = (target_size[0] - new_w) // 2
        paste_y = (target_size[1] - new_h) // 2
        canvas.paste(fg_resized, (paste_x, paste_y), fg_resized)
        return canvas
    else:
        # Character
        scale = min(float(target_h) / fg_h, (target_size[0] - 6) / float(fg_w))
        new_w = max(1, int(fg_w * scale))
        new_h = max(1, int(fg_h * scale))
        fg_resized = fg.resize((new_w, new_h), Image.Resampling.LANCZOS)
        canvas = Image.new('RGBA', target_size, (0, 0, 0, 0))
        paste_x = (target_size[0] - new_w) // 2
        paste_y = max(0, feet_y - new_h)
        canvas.paste(fg_resized, (paste_x, paste_y), fg_resized)
        return canvas

def main():
    img1 = Image.open(IMG1_PATH).convert('RGB')
    img2 = Image.open(IMG2_PATH).convert('RGB')

    # Dirs
    grunt_dir = 'Assets/Art/Sprites/Characters/Enemies/AlienGrunt'
    elite_dir = 'Assets/Art/Sprites/Characters/Enemies/AlienElite'
    boss_dir = 'Assets/Art/Sprites/Characters/Enemies/BossAlienWarlord'
    ufo_dir = 'Assets/Art/Sprites/Vehicles'
    
    for d in [grunt_dir, elite_dir, boss_dir, ufo_dir]:
        os.makedirs(d, exist_ok=True)
        if not os.path.exists(d + '.meta'):
            # Folder meta
            guid = uuid.uuid4().hex
            with open(d + '.meta', 'w') as f:
                f.write(f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

    # 1. ALIEN GRUNT (IMG1 Box 1)
    grunt_specs = {
        'alien_grunt_down.png': (25, 95, 78, 235),
        'alien_grunt_idle.png': (25, 95, 78, 235),
        'alien_grunt_left.png': (90, 95, 145, 235),
        'alien_grunt_right.png': (155, 95, 210, 235),
        'alien_grunt_side.png': (155, 95, 210, 235),
        'alien_grunt_up.png': (220, 95, 275, 235),
        'alien_grunt_attack.png': (280, 95, 355, 235),
        'alien_grunt_hurt.png': (355, 95, 412, 235),
        'alien_grunt_die.png': (430, 135, 502, 235),
    }
    for fname, box in grunt_specs.items():
        p = os.path.join(grunt_dir, fname)
        sp = extract_and_clean_sprite(img1, box, (64, 64), target_h=53, feet_y=60)
        sp.save(p)
        generate_meta(p)
        print(f"Saved: {p}")

    # Grunt Walk (IMG2 Row 1)
    grunt_walk_side = [(48, 65, 122, 210), (165, 65, 238, 210), (288, 65, 360, 210), (404, 65, 476, 210)]
    grunt_walk_up = [(542, 65, 616, 210), (649, 65, 721, 210), (767, 65, 838, 210), (885, 65, 958, 210)]
    for i, box in enumerate(grunt_walk_side, 1):
        p = os.path.join(grunt_dir, f'alien_grunt_side_walk{i}.png')
        sp = extract_and_clean_sprite(img2, box, (64, 64), target_h=53, feet_y=60)
        sp.save(p)
        generate_meta(p)
        print(f"Saved: {p}")
    for i, box in enumerate(grunt_walk_up, 1):
        p = os.path.join(grunt_dir, f'alien_grunt_up_walk{i}.png')
        sp = extract_and_clean_sprite(img2, box, (64, 64), target_h=53, feet_y=60)
        sp.save(p)
        generate_meta(p)
        print(f"Saved: {p}")

    # 2. ALIEN ELITE (IMG1 Box 2)
    elite_specs = {
        'alien_elite_down.png': (530, 90, 595, 225),
        'alien_elite_idle.png': (530, 90, 595, 225),
        'alien_elite_left.png': (605, 90, 660, 225),
        'alien_elite_right.png': (670, 90, 730, 225),
        'alien_elite_side.png': (670, 90, 730, 225),
        'alien_elite_up.png': (740, 90, 795, 225),
        'alien_elite_attack.png': (795, 90, 870, 225),
        'alien_elite_hurt.png': (880, 90, 945, 225),
        'alien_elite_die.png': (945, 135, 1005, 225),
    }
    for fname, box in elite_specs.items():
        p = os.path.join(elite_dir, fname)
        sp = extract_and_clean_sprite(img1, box, (64, 64), target_h=55, feet_y=60)
        sp.save(p)
        generate_meta(p)
        print(f"Saved: {p}")

    # Elite Walk (IMG2 Row 2)
    elite_walk_side = [(33, 275, 123, 415), (158, 275, 246, 415), (282, 275, 363, 415), (399, 275, 484, 415)]
    elite_walk_up = [(535, 275, 620, 415), (644, 275, 734, 415), (761, 275, 848, 415), (878, 275, 965, 415)]
    for i, box in enumerate(elite_walk_side, 1):
        p = os.path.join(elite_dir, f'alien_elite_side_walk{i}.png')
        sp = extract_and_clean_sprite(img2, box, (64, 64), target_h=55, feet_y=60)
        sp.save(p)
        generate_meta(p)
        print(f"Saved: {p}")
    for i, box in enumerate(elite_walk_up, 1):
        p = os.path.join(elite_dir, f'alien_elite_up_walk{i}.png')
        sp = extract_and_clean_sprite(img2, box, (64, 64), target_h=55, feet_y=60)
        sp.save(p)
        generate_meta(p)
        print(f"Saved: {p}")

    # 3. BOSS ALIEN WARLORD (IMG1 Box 3)
    boss_specs = {
        'boss_alien_warlord_down.png': (20, 345, 90, 485),
        'boss_alien_warlord_idle.png': (20, 345, 90, 485),
        'boss_alien_warlord_left.png': (95, 345, 165, 485),
        'boss_alien_warlord_right.png': (170, 345, 238, 485),
        'boss_alien_warlord_side.png': (170, 345, 238, 485),
        'boss_alien_warlord_up.png': (240, 345, 308, 485),
        'boss_alien_warlord_attack.png': (310, 345, 395, 485),
        'boss_alien_warlord_hurt.png': (398, 345, 470, 485),
        'boss_alien_warlord_die.png': (380, 495, 490, 565),
    }
    for fname, box in boss_specs.items():
        p = os.path.join(boss_dir, fname)
        sp = extract_and_clean_sprite(img1, box, (64, 64), target_h=58, feet_y=61)
        sp.save(p)
        generate_meta(p)
        print(f"Saved: {p}")

    # Boss Walk (IMG2 Row 3)
    boss_walk_side = [(39, 495, 138, 635), (189, 495, 292, 635), (351, 495, 457, 635)]
    for i, box in enumerate(boss_walk_side, 1):
        p = os.path.join(boss_dir, f'boss_alien_warlord_side_walk{i}.png')
        sp = extract_and_clean_sprite(img2, box, (64, 64), target_h=58, feet_y=61)
        sp.save(p)
        generate_meta(p)
        print(f"Saved: {p}")

    # 4. TÀU MẸ & UFO (IMG1 Box 4)
    # Mothership (128x128)
    mothership_p = os.path.join(ufo_dir, 'ufo_mothership.png')
    sp_mother = extract_and_clean_sprite(img1, (610, 300, 915, 475), (128, 128), is_vehicle=True)
    sp_mother.save(mothership_p)
    generate_meta(mothership_p)
    print(f"Saved: {mothership_p}")

    # Scout UFOs (64x64)
    ufo_specs = {
        'ufo_scout_down.png': (520, 495, 642, 575),
        'ufo_scout_idle.png': (520, 495, 642, 575),
        'ufo_scout_left.png': (650, 495, 760, 575),
        'ufo_scout_right.png': (765, 495, 875, 575),
        'ufo_scout_up.png': (880, 495, 995, 575),
    }
    for fname, box in ufo_specs.items():
        p = os.path.join(ufo_dir, fname)
        sp = extract_and_clean_sprite(img1, box, (64, 64), is_vehicle=True)
        sp.save(p)
        generate_meta(p)
        print(f"Saved: {p}")

    print("\n[SUCCESS] Extracted and generated all Alien & UFO ecosystem sprites!")

if __name__ == '__main__':
    main()
