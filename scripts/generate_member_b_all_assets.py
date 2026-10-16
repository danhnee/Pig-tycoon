import os
import uuid
from PIL import Image, ImageDraw

# Generates all Member B 32 PPU pixel-art sprite sheets and Unity .meta files:
# 1. 4 Enemies of B (ChuotGamKho, BayNhay, ThuBao, KeMangDich) - 4 directions x 8 frames
# 2. An Full Action Sheet (Idle, Walk, Run, Attack 1-2-3, Dodge, Interact, Hurt, Death x 4 dirs)
# 3. 4 Pig Age Stages (HeoNon, DangLon, TruongThanh, HeoGia) & 3 Gene Packs of B (LamKhe, XichMao, ThietBi)
# 4. 3 NPCs of B (BacSiThuY, NongCong, VeBinhKhien) & 2 Pets of B (ChoChanDan, ChoNgaoCanh)
# 5. 8 Buildings of B Atlas (RaoGo, RaoThep, CongGiaCo, UBaoCat, BanChong, DenPhaCongNghiep, BomLamMat, ChotCanhGac, CoTapKet)

ROOT = r"d:\Pig-tycoon\Assets\Art"

def write_texture_meta(png_path):
    meta_path = png_path + ".meta"
    if os.path.exists(meta_path):
        return
    guid = uuid.uuid4().hex
    content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
  bumpmap:
    convertToNormalMap: 0
  isReadable: 1
  AlphaIsTransparency: 1
  spriteMode: 1
  spritePixelsToUnits: 32
  spritePivot: {{x: 0.5, y: 0.15}}
  spriteGenerateFallbackPhysicsShape: 1
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  textureCompression: 0
"""
    with open(meta_path, "w", encoding="utf-8") as f:
        f.write(content)

def ensure_folder_with_meta(folder_path):
    os.makedirs(folder_path, exist_ok=True)
    meta_path = folder_path.rstrip("\\/") + ".meta"
    if not os.path.exists(meta_path):
        guid = uuid.uuid4().hex
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

def draw_character_or_monster_sheet(out_path, cols, rows, cell_size, body_rgb, accent_rgb, eye_rgb=(250, 240, 210), is_quadruped=False):
    img = Image.new("RGBA", (cols * cell_size, rows * cell_size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    outline = (28, 20, 18, 255)

    for r in range(rows):
        for c in range(cols):
            ox = c * cell_size
            oy = r * cell_size
            bob = (c % 2) * 2
            leap = -4 if (c in (4, 5)) else 0
            cx = ox + cell_size // 2
            cy = oy + cell_size // 2 + bob + leap

            # Ground shadow
            draw.ellipse([cx - 10, oy + cell_size - 10, cx + 10, oy + cell_size - 4], fill=(0, 0, 0, 90))

            if is_quadruped:
                # Quadruped / Pig / Beast silhouette
                w, h = 13, 9
                draw.rounded_rectangle([cx - w, cy - h, cx + w, cy + h], radius=4, fill=(*body_rgb, 255), outline=outline)
                # Legs
                leg_off = (c % 2) * 2 - 1
                draw.rectangle([cx - 9 + leg_off, cy + h - 1, cx - 5 + leg_off, cy + h + 5], fill=outline)
                draw.rectangle([cx + 5 - leg_off, cy + h - 1, cx + 9 - leg_off, cy + h + 5], fill=outline)
                # Snout / Accent crest
                draw.rounded_rectangle([cx - 6, cy - h - 3, cx + 6, cy - h + 3], radius=2, fill=(*accent_rgb, 255), outline=outline)
                draw.point((cx - 4, cy - 2), fill=(*eye_rgb, 255))
                draw.point((cx + 4, cy - 2), fill=(*eye_rgb, 255))
            else:
                # Humanoid / Character / NPC silhouette
                draw.rounded_rectangle([cx - 8, cy - 6, cx + 8, cy + 10], radius=3, fill=(*body_rgb, 255), outline=outline)
                draw.ellipse([cx - 7, cy - 17, cx + 7, cy - 4], fill=(242, 206, 174, 255), outline=outline)
                draw.pieslice([cx - 8, cy - 18, cx + 8, cy - 6], start=180, end=360, fill=(*accent_rgb, 255), outline=outline)
                draw.point((cx - 3, cy - 10), fill=outline)
                draw.point((cx + 3, cy - 10), fill=outline)

    img.save(out_path, "PNG")
    write_texture_meta(out_path)

def draw_buildings_atlas(out_path):
    # 8 buildings of B in a 4x2 grid of 64x64 cells
    cell = 64
    img = Image.new("RGBA", (cell * 4, cell * 2), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    palette = [
        ((138, 92, 52), (186, 138, 86)),   # Hang Rao Go
        ((96, 110, 124), (156, 174, 190)), # Cong Gia Co / Rao Thep
        ((172, 152, 108), (212, 192, 142)),# U Bao Cat
        ((82, 76, 72), (200, 62, 52)),     # Ban Chong
        ((64, 78, 96), (255, 238, 128)),   # Den Pha Cong Nghiep
        ((48, 118, 168), (118, 212, 245)), # Bom Lam Mat
        ((118, 82, 48), (218, 168, 72)),   # Chot Canh Gac
        ((168, 38, 38), (245, 202, 68)),   # Co Tap Ket
    ]
    outline = (28, 20, 18, 255)
    for idx, (c1, c2) in enumerate(palette):
        gx = (idx % 4) * cell
        gy = (idx // 4) * cell
        draw.ellipse([gx + 10, gy + 48, gx + 54, gy + 58], fill=(0, 0, 0, 90))
        draw.rounded_rectangle([gx + 12, gy + 14, gx + 52, gy + 52], radius=5, fill=(*c1, 255), outline=outline)
        draw.rounded_rectangle([gx + 18, gy + 20, gx + 46, gy + 36], radius=3, fill=(*c2, 255), outline=outline)
    img.save(out_path, "PNG")
    write_texture_meta(out_path)

def main():
    enemies_dir = os.path.join(ROOT, "Characters", "Enemies_B")
    an_dir = os.path.join(ROOT, "Characters", "An")
    npcs_dir = os.path.join(ROOT, "Characters", "NPCs_B")
    pets_dir = os.path.join(ROOT, "Characters", "Pets_B")
    pigs_dir = os.path.join(ROOT, "Pigs")
    bld_dir = os.path.join(ROOT, "Buildings")

    for d in [enemies_dir, an_dir, npcs_dir, pets_dir, pigs_dir, bld_dir]:
        ensure_folder_with_meta(d)

    # 1. Member B's 4 Enemies
    draw_character_or_monster_sheet(os.path.join(enemies_dir, "Enemy_ChuotGamKho_Sheet.png"), 8, 4, 48, (112, 96, 88), (215, 118, 124), is_quadruped=True)
    draw_character_or_monster_sheet(os.path.join(enemies_dir, "Enemy_BayNhay_Sheet.png"), 8, 4, 48, (62, 138, 76), (228, 198, 58), is_quadruped=True)
    draw_character_or_monster_sheet(os.path.join(enemies_dir, "Enemy_ThuBao_Sheet.png"), 8, 4, 64, (48, 86, 168), (92, 224, 255), is_quadruped=True)
    draw_character_or_monster_sheet(os.path.join(enemies_dir, "Enemy_KeMangDich_Sheet.png"), 8, 4, 64, (98, 58, 122), (118, 232, 68), is_quadruped=False)

    # 2. An Full Action Sheet (10 states x 4 directions)
    draw_character_or_monster_sheet(os.path.join(an_dir, "An_FullAction_Sheet.png"), 10, 4, 48, (56, 132, 92), (218, 152, 64), is_quadruped=False)

    # 3. 4 Pig Age Stages + 3 Gene Packs of B
    draw_character_or_monster_sheet(os.path.join(pigs_dir, "Pig_Stage_HeoNon_Sheet.png"), 6, 4, 32, (248, 188, 198), (255, 148, 168), is_quadruped=True)
    draw_character_or_monster_sheet(os.path.join(pigs_dir, "Pig_Stage_DangLon_Sheet.png"), 6, 4, 40, (242, 172, 184), (248, 132, 152), is_quadruped=True)
    draw_character_or_monster_sheet(os.path.join(pigs_dir, "Pig_Stage_TruongThanh_Sheet.png"), 6, 4, 48, (236, 158, 172), (240, 118, 140), is_quadruped=True)
    draw_character_or_monster_sheet(os.path.join(pigs_dir, "Pig_Stage_HeoGia_Sheet.png"), 6, 4, 52, (198, 142, 152), (168, 112, 122), is_quadruped=True)

    draw_character_or_monster_sheet(os.path.join(pigs_dir, "Pig_Gene_LamKhe_Sheet.png"), 6, 4, 48, (112, 196, 232), (58, 142, 198), is_quadruped=True)
    draw_character_or_monster_sheet(os.path.join(pigs_dir, "Pig_Gene_XichMao_Sheet.png"), 6, 4, 48, (208, 76, 58), (248, 168, 52), is_quadruped=True)
    draw_character_or_monster_sheet(os.path.join(pigs_dir, "Pig_Gene_ThietBi_Sheet.png"), 6, 4, 48, (118, 128, 142), (182, 196, 212), is_quadruped=True)

    # 4. 3 NPCs & 2 Pets of B
    draw_character_or_monster_sheet(os.path.join(npcs_dir, "NPC_BacSiThuY_Sheet.png"), 6, 4, 48, (228, 238, 245), (58, 168, 138), is_quadruped=False)
    draw_character_or_monster_sheet(os.path.join(npcs_dir, "NPC_NongCong_Sheet.png"), 6, 4, 48, (148, 108, 68), (218, 182, 88), is_quadruped=False)
    draw_character_or_monster_sheet(os.path.join(npcs_dir, "NPC_VeBinhKhien_Sheet.png"), 6, 4, 48, (78, 92, 118), (198, 158, 62), is_quadruped=False)

    draw_character_or_monster_sheet(os.path.join(pets_dir, "Pet_ChoChanDan_Sheet.png"), 6, 4, 40, (198, 138, 72), (242, 228, 198), is_quadruped=True)
    draw_character_or_monster_sheet(os.path.join(pets_dir, "Pet_ChoNgaoCanh_Sheet.png"), 6, 4, 48, (72, 62, 56), (178, 52, 42), is_quadruped=True)

    # 5. 8 Buildings of B Atlas
    draw_buildings_atlas(os.path.join(bld_dir, "Buildings_B_Atlas.png"))
    print("[ART PIPELINE] Generated all Member B 32 PPU sprite sheets and .meta files successfully!")

if __name__ == "__main__":
    main()
