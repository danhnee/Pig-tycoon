#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
build_3_separate_abc_master_boards.py
Builds the 3 Separate Master Showcase Boards for Upgraded Option C (`C1`, `C2`, `C3`):
- `an_master_board_C1.png`: Upgraded C1 (Vòng cổ choker nhung đen mặt hồng ngọc + Áo khoác lửng đỏ rượu viền vàng + Quần short đen cạp cao + Bốt da cao cổ tôn dáng — 0 túi đeo chéo)
- `an_master_board_C2.png`: Upgraded C2 (Vòng cổ bạc 2 tầng mặt lam ngọc đồng bộ Khoa + Áo khoác lửng xanh đêm viền bạc + Quần short đen & đai đùi + Bốt cổ lửng tất mỏng — 0 túi đeo chéo)
- `an_master_board_C3.png`: Upgraded C3 (Vòng cổ choker da mặt hổ phách vàng + Áo khoác lửng kem-đỏ rượu cúc vàng + Quần short xám đen + Bốt da nâu đai vàng chéo — 0 túi đeo chéo)

Also saves aliases `an_master_board_A.png`, `an_master_board_B.png`, `an_master_board_C.png` and extracts
all 6 Anime Visual Novel portraits into `an_candidates_v13/<Option>/portraits/` and `UI/Portraits/`.
"""

from collections import deque
import shutil
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(r"D:\Pig-tycoon")
ASSETS_CHARS = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters"
SYNC_CHARS = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon" / "Assets" / "Art" / "PixelMyth" / "Characters"
ASSETS_PORTRAITS = ROOT / "Assets" / "Art" / "PixelMyth" / "UI" / "Portraits"
SYNC_PORTRAITS = ROOT / ".collab" / "claude-2d" / "ui_and_map_sync" / "from_argon" / "Assets" / "Art" / "PixelMyth" / "UI" / "Portraits"

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\ac0f248a-d876-47b0-9f6a-277b76657366")
SHEET_C1 = ARTIFACT_DIR / "an_option_c1_upgraded_sheet_1793627755537.jpg"
SHEET_C2 = ARTIFACT_DIR / "an_option_c2_upgraded_sheet_1793627795288.jpg"
SHEET_C3 = ARTIFACT_DIR / "an_option_c3_upgraded_sheet_1793627828461.jpg"


def get_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = (
        ["C:/Windows/Fonts/arialbd.ttf", "C:/Windows/Fonts/segoeuib.ttf", "C:/Windows/Fonts/tahomabd.ttf"]
        if bold
        else ["C:/Windows/Fonts/arial.ttf", "C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/tahoma.ttf"]
    )
    for fp in candidates:
        if Path(fp).exists():
            return ImageFont.truetype(fp, size)
    return ImageFont.load_default()


def remove_white_bg_floodfill(crop_rgba: Image.Image, tol: float = 24.0) -> Image.Image:
    arr = np.asarray(crop_rgba.convert("RGBA")).copy()
    h, w, _ = arr.shape
    rgb = arr[:, :, :3].astype(np.float32)
    corners = np.vstack([rgb[0:5, 0:5].reshape(-1, 3), rgb[0:5, w - 5:w].reshape(-1, 3)])
    bg_med = np.median(corners, axis=0)
    dist = np.sqrt(np.sum((rgb - bg_med) ** 2, axis=2))
    is_bg = dist < tol

    visited = np.zeros((h, w), dtype=bool)
    q = deque()
    for x in range(w):
        if is_bg[0, x]:
            visited[0, x] = True
            q.append((0, x))
        if is_bg[h - 1, x]:
            visited[h - 1, x] = True
            q.append((h - 1, x))
    for y in range(h):
        if is_bg[y, 0] and not visited[y, 0]:
            visited[y, 0] = True
            q.append((y, 0))
        if is_bg[y, w - 1] and not visited[y, w - 1]:
            visited[y, w - 1] = True
            q.append((y, w - 1))

    while q:
        cy, cx = q.popleft()
        for ny, nx in ((cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)):
            if 0 <= ny < h and 0 <= nx < w and not visited[ny, nx] and is_bg[ny, nx]:
                visited[ny, nx] = True
                q.append((ny, nx))

    arr[visited, 3] = 0
    return Image.fromarray(arr)


def slice_and_save_anime_portraits(folder_names: list[str], sheet: Image.Image, promote_default: bool = False):
    sw, sh = sheet.size
    boxes = [
        ("an_neutral", 0.496, 0.095, 0.648, 0.415),
        ("an_happy", 0.662, 0.095, 0.812, 0.415),
        ("an_curious", 0.826, 0.095, 0.976, 0.415),
        ("an_too_bright", 0.496, 0.550, 0.648, 0.888),
        ("an_stare", 0.662, 0.550, 0.812, 0.888),
        ("an_silent_tilt", 0.826, 0.550, 0.976, 0.888),
    ]

    extracted = {}
    for name, x0, y0, x1, y1 in boxes:
        raw = sheet.crop((int(sw * x0), int(sh * y0), int(sw * x1), int(sh * y1)))
        clean = remove_white_bg_floodfill(raw, tol=22.0)
        bbox = clean.getbbox()
        if bbox:
            clean = clean.crop(bbox)
        canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        scale = min(484.0 / max(1, clean.width), 496.0 / max(1, clean.height))
        nw = max(1, int(round(clean.width * scale)))
        nh = max(1, int(round(clean.height * scale)))
        resized = clean.resize((nw, nh), Image.Resampling.LANCZOS)
        canvas.paste(resized, ((512 - nw) // 2, 512 - nh), resized)
        extracted[name] = canvas

    for opt_key in folder_names:
        for base in (ASSETS_CHARS, SYNC_CHARS):
            pdir = base / "an_candidates_v13" / opt_key / "portraits"
            pdir.mkdir(parents=True, exist_ok=True)
            for name, img in extracted.items():
                img.save(pdir / f"{name}.png")

    if promote_default:
        alias_map = {
            "an_neutral": ["an_neutral.png", "portrait_an.png", "portrait_an_normal.png", "portrait_Hero_An.png", "portrait_Hero_An_normal.png"],
            "an_happy": ["an_happy.png", "an_laugh.png", "portrait_an_happy.png", "portrait_Hero_An_happy.png"],
            "an_curious": ["an_surprised.png", "an_thinking.png", "portrait_an_surprised.png", "portrait_an_thoughtful.png"],
            "an_too_bright": ["an_too_bright.png", "portrait_an_over_bright_smile.png", "portrait_Hero_An_over_bright_smile.png"],
            "an_stare": ["an_stare.png", "an_angry.png", "portrait_an_long_stare.png", "portrait_Hero_An_long_stare.png", "portrait_an_angry.png"],
            "an_silent_tilt": ["an_silent_tilt.png", "an_reflect_unsmiling.png", "an_sad.png", "an_worried.png", "portrait_an_silent_head_tilt.png", "portrait_an_sad.png"],
        }
        for p_root in (ASSETS_PORTRAITS, SYNC_PORTRAITS):
            p_root.mkdir(parents=True, exist_ok=True)
            for src_key, dst_names in alias_map.items():
                for dname in dst_names:
                    extracted[src_key].save(p_root / dname)


def polish_anime_top_sheet(sheet: Image.Image, an_label: str, outfit_label: str) -> Image.Image:
    im = sheet.copy().convert("RGBA")
    sw, sh = im.size
    d = ImageDraw.Draw(im)

    d.rectangle([int(sw * 0.48), 0, sw, int(sh * 0.086)], fill=(255, 255, 255, 255))
    d.rectangle([int(sw * 0.48), int(sh * 0.485), sw, int(sh * 0.542)], fill=(255, 255, 255, 255))

    f_hdr = get_font(20, bold=True)
    f_sub = get_font(15, bold=True)
    f_tag = get_font(14, bold=False)

    d.text((int(sw * 0.035), 12), an_label, font=f_hdr, fill=(28, 36, 52, 255))
    d.text((int(sw * 0.035), 36), outfit_label, font=f_tag, fill=(82, 96, 118, 255))
    d.text((int(sw * 0.305), 12), "KHOA (Đã duyệt)", font=f_hdr, fill=(28, 36, 52, 255))

    d.text((int(sw * 0.495), 18), "6 BIỂU CẢM VISUAL NOVEL ANIME 2.5D (CÓ VÒNG CỔ, BỎ TÚI ĐEO)", font=f_hdr, fill=(28, 36, 52, 255))
    d.text(
        (int(sw * 0.495), int(sh * 0.498)),
        "3 Biểu cảm 'ẩn đằng sau là một chút điên điên' (Cười quá tươi / Nhìn chằm chằm / Nghiêng đầu im lặng):",
        font=f_sub,
        fill=(148, 42, 72, 255),
    )
    return im


def build_master_board(opt_key: str, badge_title: str, subtitle: str, top_img: Image.Image, out_names: list[str]):
    W, H = 1440, 1120
    board = Image.new("RGBA", (W, H), (22, 26, 32, 255))
    d = ImageDraw.Draw(board)

    f_title = get_font(16, bold=True)
    f_sub = get_font(14, bold=False)
    f_card = get_font(15, bold=True)
    f_lbl = get_font(13, bold=False)

    d.rounded_rectangle([16, 12, W - 16, 66], radius=10, fill=(38, 46, 56, 255), outline=(228, 192, 112, 255), width=2)
    d.text((32, 18), badge_title, font=f_title, fill=(255, 238, 184, 255))
    d.text((32, 41), subtitle, font=f_sub, fill=(196, 226, 204, 255))

    top_resized = top_img.resize((1408, 740), Image.Resampling.LANCZOS)
    board.paste(top_resized, (16, 74))
    d.rounded_rectangle([16, 74, W - 16, 814], radius=10, outline=(228, 192, 112, 255), width=2)

    by0 = 824
    d.rounded_rectangle([16, by0, 740, H - 14], radius=12, fill=(32, 40, 46, 255), outline=(136, 184, 144, 255), width=2)
    d.text((32, by0 + 12), f"BẢN THỂ 32-PPU (72×84) 4 HƯỚNG & SẢI BƯỚC 4.5 m/s — {opt_key}", font=f_card, fill=(255, 232, 156, 255))

    cand_dir = ASSETS_CHARS / "an_candidates_v13" / opt_key
    idle_sh = Image.open(cand_dir / "an_idle.png")
    for dir_i, dir_lbl in enumerate(["Nam (Trước)", "Tây (Trái)", "Bắc (Sau)", "Đông (Phải)"]):
        cell = idle_sh.crop((0, dir_i * 84, 72, (dir_i + 1) * 84)).resize((72, 84), Image.Resampling.NEAREST)
        cx_pos = 30 + dir_i * 86
        cy_pos = by0 + 38
        board.paste(cell, (cx_pos, cy_pos), cell)
        d.text((cx_pos + 2, cy_pos + 86), dir_lbl, font=f_lbl, fill=(190, 210, 200, 255))

    walk_sh = Image.open(cand_dir / "an_walk.png")
    d.text((32, by0 + 150), "Chu kỳ walk 6 frame (15 fps, sải 1.8m = 4.5 m/s, 0 trượt chân):", font=f_lbl, fill=(240, 216, 150, 255))
    for f_i in range(6):
        wcell = walk_sh.crop((f_i * 72, 0, (f_i + 1) * 72, 84)).resize((64, 75), Image.Resampling.NEAREST)
        board.paste(wcell, (30 + f_i * 72, by0 + 174), wcell)

    k_idle = Image.open(ASSETS_CHARS / "khoa" / "khoa_idle.png")
    an_sp = idle_sh.crop((0, 0, 72, 84)).resize((96, 112), Image.Resampling.NEAREST)
    kh_sp = k_idle.crop((0, 0, 72, 84)).resize((96, 112), Image.Resampling.NEAREST)
    board.paste(an_sp, (510, by0 + 82), an_sp)
    board.paste(kh_sp, (612, by0 + 82), kh_sp)
    d.text((512, by0 + 50), "Tỉ lệ 32-PPU cạnh Khoa:", font=f_card, fill=(255, 232, 156, 255))
    d.text((544, by0 + 200), "An", font=f_lbl, fill=(255, 240, 190, 255))
    d.text((640, by0 + 200), "Khoa", font=f_lbl, fill=(200, 225, 255, 255))

    rx0 = 754
    d.rounded_rectangle([rx0, by0, W - 16, H - 14], radius=12, fill=(68, 118, 62, 255), outline=(228, 192, 112, 255), width=2)
    for gy in range(by0 + 8, H - 20, 24):
        shade = (76, 130, 68, 255) if ((gy // 24) % 2 == 0) else (64, 112, 58, 255)
        d.rectangle([rx0 + 6, gy, W - 22, min(H - 20, gy + 24)], fill=shade)
    d.rounded_rectangle([rx0 + 16, by0 + 128, W - 32, by0 + 224], radius=14, fill=(176, 154, 122, 255), outline=(124, 104, 78, 255), width=2)
    for fx in range(rx0 + 24, W - 40, 48):
        d.rectangle([fx, by0 + 82, fx + 8, by0 + 126], fill=(132, 86, 48, 255), outline=(64, 38, 20, 255), width=1)
    d.rectangle([rx0 + 20, by0 + 94, W - 36, by0 + 102], fill=(158, 106, 62, 255), outline=(64, 38, 20, 255), width=1)
    d.rectangle([rx0 + 20, by0 + 110, W - 36, by0 + 118], fill=(146, 96, 54, 255), outline=(64, 38, 20, 255), width=1)

    d.rounded_rectangle([rx0 + 16, by0 + 12, W - 32, by0 + 44], radius=6, fill=(24, 32, 26, 225), outline=(224, 188, 112, 255), width=1)
    d.text((rx0 + 28, by0 + 19), f"PHỐI CẢNH SÂN TRẠI (FARM_MAIN): {opt_key} ĐỨNG CẠNH KHOA & ĐÀN HEO", font=f_card, fill=(255, 240, 190, 255))

    an_farm = idle_sh.crop((0, 0, 72, 84)).resize((126, 147), Image.Resampling.NEAREST)
    kh_farm = k_idle.crop((0, 0, 72, 84)).resize((126, 147), Image.Resampling.NEAREST)
    board.paste(an_farm, (rx0 + 185, by0 + 70), an_farm)
    board.paste(kh_farm, (rx0 + 335, by0 + 70), kh_farm)

    pig1_path = ASSETS_CHARS / "argon_pig_hong_dien" / "argon_pig_hong_dien_idle.png"
    pig2_path = ASSETS_CHARS / "argon_pig_kim_tho" / "argon_pig_kim_tho_idle.png"
    for p_idx, (p_path, px_off) in enumerate([(pig1_path, rx0 + 48), (pig2_path, rx0 + 492)]):
        if p_path.exists():
            pig_sh = Image.open(p_path)
            pw, ph = pig_sh.width // 4, pig_sh.height // 4
            row = 3 if p_idx == 0 else 1
            pig_sp = pig_sh.crop((0, row * ph, pw, (row + 1) * ph)).resize((int(pw * 1.5), int(ph * 1.5)), Image.Resampling.NEAREST)
            board.paste(pig_sp, (px_off, by0 + 126), pig_sp)

    for oname in out_names:
        out_path = ARTIFACT_DIR / oname
        board.save(out_path)
        print(f"[Upgraded-C-Board] Saved {out_path}")


if __name__ == "__main__":
    raw_c1 = Image.open(SHEET_C1).convert("RGBA")
    raw_c2 = Image.open(SHEET_C2).convert("RGBA")
    raw_c3 = Image.open(SHEET_C3).convert("RGBA")

    slice_and_save_anime_portraits(["OptionC1_CrimsonGold", "OptionA_SageCream", "OptionA_AnimeJeans"], raw_c1, promote_default=True)
    slice_and_save_anime_portraits(["OptionC2_NavySilver", "OptionB_SkyIvory", "OptionB_AnimeSlacks"], raw_c2, promote_default=False)
    slice_and_save_anime_portraits(["OptionC3_IvoryGold", "OptionC_ApricotOlive", "OptionC_AnimeShorts"], raw_c3, promote_default=False)

    top_c1 = polish_anime_top_sheet(raw_c1, "AN — BẢN NÂNG CẤP C1", "Choker hồng ngọc + Khoác lửng đỏ rượu viền vàng + Short đen + Bốt da cao cổ (0 túi)")
    top_c2 = polish_anime_top_sheet(raw_c2, "AN — BẢN NÂNG CẤP C2", "Vòng cổ bạc lam ngọc + Khoác lửng xanh đêm viền bạc + Short & đai đùi + Bốt cổ lửng (0 túi)")
    top_c3 = polish_anime_top_sheet(raw_c3, "AN — BẢN NÂNG CẤP C3", "Choker vàng hổ phách + Khoác lửng kem-đỏ rượu + Short xám đen + Bốt đai vàng chéo (0 túi)")

    build_master_board(
        "OptionC1_CrimsonGold",
        "BẢN NÂNG CẤP C1 — CHOKER NHUNG MẶT HỒNG NGỌC + KHOÁC LỬNG ĐỎ RƯỢU VIỀN VÀNG + SHORT ĐEN + BỐT CAO CỔ TÔN DÁNG (ĐÃ BỎ TÚI)",
        "Nâng cấp từ Phương án C: Bỏ túi đeo chéo + Thêm vòng cổ choker mặt ngọc + Áo khoác & bốt cao cổ chuẩn nữ chính Anime hành động cạnh Khoa",
        top_c1,
        ["an_master_board_C1.png", "an_master_board_A.png"],
    )
    build_master_board(
        "OptionC2_NavySilver",
        "BẢN NÂNG CẤP C2 — VÒNG CỔ BẠC 2 TẦNG MẶT LAM NGỌC + KHOÁC LỬNG XANH ĐÊM VIỀN BẠC + SHORT & ĐAI ĐÙI + BỐT CỔ LỬNG (ĐÃ BỎ TÚI)",
        "Nâng cấp từ Phương án C: Bỏ túi đeo chéo + Vòng cổ bạc đồng bộ dây chuyền của Khoa + Trang phục & bốt cổ lửng sắc sảo",
        top_c2,
        ["an_master_board_C2.png", "an_master_board_B.png"],
    )
    build_master_board(
        "OptionC3_IvoryGold",
        "BẢN NÂNG CẤP C3 — CHOKER DA & DÂY VÀNG HỔ PHÁCH + KHOÁC LỬNG KEM-ĐỎ RƯỢU CÚC VÀNG + SHORT XÁM ĐEN + BỐT ĐAI VÀNG CHÉO (ĐÃ BỎ TÚI)",
        "Nâng cấp từ Phương án C: Bỏ túi đeo chéo + Vòng cổ choker dây vàng + Áo khoác kem-đỏ rượu & bốt da đai vàng chéo thanh thoát",
        top_c3,
        ["an_master_board_C3.png", "an_master_board_C.png"],
    )
