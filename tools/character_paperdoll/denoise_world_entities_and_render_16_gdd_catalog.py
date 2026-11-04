#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
1. Cleans `np.gradient` salt-and-pepper pixel noise (`bị nhiễu`) and adds a crisp 1px dark grounding
   outline + warm top-left sunlit rim light across all non-hero world entities (`NPCs`, `Mercs`,
   `Pigs`, `Pets`, `Monsters`) in `Assets/Art/PixelMyth/Characters/` and `from_argon/` (#141, #142).
2. Renders a comprehensive x1.5 nearest-neighbour visual verification board of ALL 16 GDD 12.3
   equipment items (`paperdoll_all_16_gdd_items_catalog_x15.png`) + attack swings + quality tiers I-IV.
"""

import shutil
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(r"D:\Pig-tycoon")
BRAIN = Path(r"C:\Users\Admin\.gemini\antigravity\brain\ac0f248a-d876-47b0-9f6a-277b76657366")
ASSETS_CHARS = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters"
FROM_ARGON_CHARS = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon" / "Assets" / "Art" / "PixelMyth" / "Characters"

GDD_16_ITEMS = [
    ("wpn_dao_rua", "weapon", "Dao Rựa"),
    ("wpn_giao_moc", "weapon", "Giáo Móc"),
    ("wpn_bua_ta", "weapon", "Búa Tạ"),
    ("wpn_no_tay", "weapon", "Nỏ Tay"),
    ("boot_giay_dinh", "boots", "Giày Đinh"),
    ("boot_guoc_gio", "boots", "Guốc Gió"),
    ("armor_giap_da_thuoc", "armor", "Giáp Da Thuộc"),
    ("armor_giap_luoi_thep", "armor", "Giáp Lưới Thép"),
    ("armor_ao_mua_dau", "armor", "Áo Mưa Dầu"),
    ("armor_ao_chi", "armor", "Áo Chì"),
    ("pants_quan_da_lot_bong", "pants", "Quần Da Lót Bông"),
    ("pants_quan_giap_xich", "pants", "Quần Giáp Xích"),
    ("brc_vong_cach_dien", "bracelet", "Vòng Cách Điện"),
    ("brc_vong_huyet_nha", "bracelet", "Vòng Huyết Nha"),
    ("nck_coi_chan_heo", "necklace", "Còi Chăn Heo"),
    ("nck_mat_day_den_bao", "necklace", "Mặt Dây Đèn Bão"),
]


def denoise_and_rimlight_sheet(png_path: Path) -> bool:
    im = Image.open(png_path).convert("RGBA")
    arr = np.asarray(im, dtype=np.uint8).copy()
    alpha = arr[..., 3] >= 150
    if not np.any(alpha):
        return False

    r = arr[..., 0].astype(np.int16)
    g = arr[..., 1].astype(np.int16)
    b = arr[..., 2].astype(np.int16)

    # 1. Purge any residual magenta fringe pixels (#141)
    mag_kill = alpha & (r > 140) & (b > 130) & ((r - g) > 35) & ((b - g) > 28)
    alpha[mag_kill] = False
    arr[~alpha] = 0
    arr[alpha, 3] = 255

    # Despill mild purple tint
    r = arr[..., 0].astype(np.int16)
    g = arr[..., 1].astype(np.int16)
    b = arr[..., 2].astype(np.int16)
    mild_mag = alpha & (r > g + 18) & (b > g + 16) & (b > 95)
    if np.any(mild_mag):
        avg_rb = (r[mild_mag] + b[mild_mag]) // 2
        arr[mild_mag, 0] = np.clip((r[mild_mag] * 3 + g[mild_mag] * 2) // 5, 0, 255).astype(np.uint8)
        arr[mild_mag, 1] = np.clip((g[mild_mag] * 3 + avg_rb) // 4, 0, 255).astype(np.uint8)
        arr[mild_mag, 2] = np.clip((b[mild_mag] * 2 + g[mild_mag] * 3) // 5, 0, 255).astype(np.uint8)

    # 2. Edge-preserving 3x3 bilateral de-speckle on interior pixels (eliminates np.gradient salt-and-pepper noise!)
    ap1 = np.pad(alpha, 1, mode="constant")
    interior = ap1[1:-1, 1:-1] & ap1[:-2, 1:-1] & ap1[2:, 1:-1] & ap1[1:-1, :-2] & ap1[1:-1, 2:]
    edge = alpha & (~interior)

    rgb_f = arr[..., :3].astype(np.float32)
    pad_rgb = np.pad(rgb_f, ((1, 1), (1, 1), (0, 0)), mode="edge")
    acc = np.zeros_like(rgb_f)
    w_sum = np.zeros(rgb_f.shape[:2], dtype=np.float32)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            nb = pad_rgb[1 + dy:1 + dy + rgb_f.shape[0], 1 + dx:1 + dx + rgb_f.shape[1]]
            nb_a = ap1[1 + dy:1 + dy + rgb_f.shape[0], 1 + dx:1 + dx + rgb_f.shape[1]]
            diff = np.max(np.abs(nb - rgb_f), axis=-1)
            w = np.where(nb_a & (diff < 36.0), 1.0 if (dx == 0 and dy == 0) else 0.45, 0.0)
            acc += nb * w[..., None]
            w_sum += w
    smooth_rgb = acc / np.maximum(w_sum[..., None], 1e-4)
    # Cluster quantization (step 5) for clean hand-painted pixel clusters
    smooth_rgb = np.round(smooth_rgb / 5.0) * 5.0
    arr[interior, :3] = np.clip(smooth_rgb[interior], 0, 255).astype(np.uint8)

    # 3. Crisp 1px grounding contour + warm top-left sunlit rim (#142 item 2)
    top_left_edge = edge & ((~ap1[:-2, 1:-1]) | (~ap1[1:-1, :-2]))
    bottom_right_edge = edge & (~top_left_edge)

    arr[top_left_edge, 0] = np.clip(arr[top_left_edge, 0].astype(np.int16) * 72 // 100 + 62, 0, 255).astype(np.uint8)
    arr[top_left_edge, 1] = np.clip(arr[top_left_edge, 1].astype(np.int16) * 72 // 100 + 54, 0, 255).astype(np.uint8)
    arr[top_left_edge, 2] = np.clip(arr[top_left_edge, 2].astype(np.int16) * 72 // 100 + 42, 0, 255).astype(np.uint8)

    arr[bottom_right_edge, 0] = np.clip(arr[bottom_right_edge, 0].astype(np.int16) * 38 // 100 + 16, 0, 255).astype(np.uint8)
    arr[bottom_right_edge, 1] = np.clip(arr[bottom_right_edge, 1].astype(np.int16) * 38 // 100 + 12, 0, 255).astype(np.uint8)
    arr[bottom_right_edge, 2] = np.clip(arr[bottom_right_edge, 2].astype(np.int16) * 38 // 100 + 18, 0, 255).astype(np.uint8)

    Image.fromarray(arr, "RGBA").save(png_path)
    return True


def process_all_world_entities():
    count_folders = 0
    count_pngs = 0
    for folder in sorted(ASSETS_CHARS.iterdir()):
        if not folder.is_dir():
            continue
        name = folder.name
        # Skip protagonists and paper-doll gear folders (already built with full 2026 AAA pipeline)
        if name in ("khoa", "an", "argon_khoa", "argon_an") or "__" in name:
            continue
        count_folders += 1
        argon_dst = FROM_ARGON_CHARS / name
        argon_dst.mkdir(parents=True, exist_ok=True)
        for png in folder.glob("*.png"):
            if denoise_and_rimlight_sheet(png):
                count_pngs += 1
                shutil.copy2(png, argon_dst / png.name)
        for js in folder.glob("*.json"):
            shutil.copy2(js, argon_dst / js.name)
    print(f"[OK] De-noised & rim-lit {count_pngs} sprite sheets across {count_folders} world entity folders!")


def render_all_16_gdd_items_catalog():
    """
    Renders a crystal-clear x1.5 nearest-neighbour showcase of ALL 16 GDD 12.3 Paper-Doll items:
    - 4x4 Grid of all 16 items: shows (Unarmed Base + That Single Item Layer) + (Isolated Layer Alone) + (Quality III Variant)
    - Bottom row: 4 GDD Weapons (`Dao Rựa`, `Giáo Móc`, `Búa Tạ`, `Nỏ Tay`) in Attack Strike Frames
    """
    cw, ch = 72, 84
    khoa_idle = np.asarray(Image.open(ASSETS_CHARS / "khoa" / "khoa_idle.png").convert("RGBA"))
    an_idle = np.asarray(Image.open(ASSETS_CHARS / "an" / "an_idle.png").convert("RGBA"))
    khoa_atk = np.asarray(Image.open(ASSETS_CHARS / "khoa" / "khoa_attack.png").convert("RGBA"))

    k_cell0 = khoa_idle[0:ch, 0:cw]
    a_cell0 = an_idle[0:ch, 0:cw]

    card_w, card_h = 232, 104
    cols, rows = 4, 4
    board_w = cols * card_w + 24
    board_h = rows * card_h + 150

    board = Image.new("RGBA", (board_w, board_h), (20, 24, 32, 255))
    dr = ImageDraw.Draw(board)
    try:
        fnt = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 11)
    except Exception:
        fnt = ImageFont.load_default()

    for idx, (item_id, slot_kind, item_vn) in enumerate(GDD_16_ITEMS):
        r_idx = idx // cols
        c_idx = idx % cols
        x0 = 12 + c_idx * card_w
        y0 = 12 + r_idx * card_h

        dr.rounded_rectangle([x0, y0, x0 + card_w - 6, y0 + card_h - 6], radius=6, fill=(30, 35, 46, 255), outline=(92, 78, 54, 255))
        dr.text((x0 + 8, y0 + 4), f"{idx + 1}. {item_vn} ({item_id})", fill=(255, 226, 128, 255), font=fnt)

        # 1. Khoa + Item Q1
        ov_k1 = np.asarray(Image.open(ASSETS_CHARS / f"khoa__{item_id}" / f"khoa__{item_id}_idle.png").convert("RGBA"))[0:ch, 0:cw]
        comp_k = Image.fromarray(k_cell0, "RGBA")
        ov_k1_im = Image.fromarray(ov_k1, "RGBA")
        comp_k.paste(ov_k1_im, (0, 0), ov_k1_im)
        board.paste(comp_k, (x0 + 4, y0 + 18), comp_k)

        # 2. Isolated Item Layer Q1 (shows pure paper-doll transparency)
        board.paste(ov_k1_im, (x0 + 78, y0 + 18), ov_k1_im)

        # 3. An + Item Q3 (`__III`)
        q3_dir = ASSETS_CHARS / f"an__{item_id}__III"
        ov_a3_path = q3_dir / f"an__{item_id}__III_idle.png"
        if not ov_a3_path.exists():
            ov_a3_path = ASSETS_CHARS / f"an__{item_id}" / f"an__{item_id}_idle.png"
        ov_a3 = np.asarray(Image.open(ov_a3_path).convert("RGBA"))[0:ch, 0:cw]
        comp_a = Image.fromarray(a_cell0, "RGBA")
        ov_a3_im = Image.fromarray(ov_a3, "RGBA")
        comp_a.paste(ov_a3_im, (0, 0), ov_a3_im)
        board.paste(comp_a, (x0 + 150, y0 + 18), comp_a)

    # Bottom row: 4 Weapons during Attack Strike (Frame 3 across Down, Left, Up, Right)
    y_atk = 12 + rows * card_h + 6
    for w_idx, (wpn_id, _, wpn_vn) in enumerate(GDD_16_ITEMS[:4]):
        x0 = 12 + w_idx * card_w
        dr.rounded_rectangle([x0, y_atk, x0 + card_w - 6, y_atk + 128], radius=6, fill=(34, 28, 36, 255), outline=(146, 108, 58, 255))
        dr.text((x0 + 8, y_atk + 4), f"Đòn Đánh: {wpn_vn} (Phẩm IV)", fill=(255, 210, 110, 255), font=fnt)
        wpn_atk = np.asarray(Image.open(ASSETS_CHARS / f"khoa__{wpn_id}__IV" / f"khoa__{wpn_id}__IV_attack.png").convert("RGBA"))
        for f_show, x_off in [(1, 6), (2, 78), (3, 150)]:
            b_cell = Image.fromarray(khoa_atk[0:ch, f_show * cw:(f_show + 1) * cw], "RGBA")
            w_cell = Image.fromarray(wpn_atk[0:ch, f_show * cw:(f_show + 1) * cw], "RGBA")
            b_cell.paste(w_cell, (0, 0), w_cell)
            board.paste(b_cell, (x0 + x_off, y_atk + 24), b_cell)

    out_15x = board.resize((int(board.width * 1.5), int(board.height * 1.5)), Image.Resampling.NEAREST)
    out_path = BRAIN / "paperdoll_all_16_gdd_items_catalog_x15.png"
    out_15x.save(out_path)
    (FROM_ARGON_CHARS / "paperdoll_all_16_gdd_items_catalog_x15.png").write_bytes(out_path.read_bytes())
    print(f"[OK] Saved 16 GDD items catalog board to {out_path}")


if __name__ == "__main__":
    process_all_world_entities()
    render_all_16_gdd_items_catalog()
