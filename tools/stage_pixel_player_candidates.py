"""Compose existing pixel artwork into review-only An/Khoa animator candidates.

No live player asset or scene is overwritten. Sources are read-only; all rectangles,
palette substitutions and nearest-neighbor scaling are recorded in the manifest.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from functools import lru_cache
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets/Art/PixelRework/Players"
EVIDENCE = ROOT / "Docs/art-direction/2d-reuse-player-evidence"
FARMER = Path(r"F:\StardewValley\Content (unpacked)\Characters\Farmer")
KHOA_SOURCE = ROOT.parent / "branches/feat__C__P0.09-khoa-sandbox/Assets/Art/Sprites/Characters/Player"
FACING = {"down": 0, "right": 1, "up": 2, "left": 1}
GROUND_Y = 60  # exclusive bottom of opaque sprite, on a 64x64 canvas


def sha(p: Path) -> str:
    return hashlib.sha256(p.read_bytes()).hexdigest()


@lru_cache(maxsize=48)
def image(path: Path) -> Image.Image:
    return Image.open(path).convert("RGBA")


def recolor(image: Image.Image, palette: list[tuple[int, int, int]]) -> Image.Image:
    result = image.copy()
    values = sorted({(r * 299 + g * 587 + b * 114) // 1000 for r, g, b, a in image.get_flattened_data() if a})
    mapping = {v: palette[min(len(palette) - 1, int(i * len(palette) / max(1, len(values))))] for i, v in enumerate(values)}
    result.putdata([(*mapping[(r * 299 + g * 587 + b * 114) // 1000], a) if a else (r, g, b, 0) for r, g, b, a in image.get_flattened_data()])
    return result


def align_ground(frame: Image.Image) -> tuple[Image.Image, int]:
    bounds = frame.getbbox()
    if not bounds:
        raise ValueError("Empty player frame")
    dy = GROUND_Y - bounds[3]
    if bounds[1] + dy < 0:
        raise ValueError(f"Ground alignment clips the head: {bounds}, shift {dy}")
    output = Image.new("RGBA", (64, 64))
    output.alpha_composite(frame, (0, dy))
    if sum(output.getchannel("A").get_flattened_data()) != sum(frame.getchannel("A").get_flattened_data()):
        raise ValueError("Ground alignment clipped opaque pixels")
    return output, dy


def compose_an(facing: str, column: int, action: bool = False) -> tuple[Image.Image, dict]:
    direction = FACING[facing]
    # Farmer base and pants share16x32 pose positions. Existing carry pose is row4,
    # used only for the animator's single ActionSprite, never labelled a full tool clip.
    base_row = 4 if action else direction
    rect = (column * 16, base_row * 32, column * 16 + 16, base_row * 32 + 32)
    frame = image(FARMER / "farmer_girl_base.png").crop(rect)
    base_alpha = frame.getchannel("A")
    skin = image(FARMER / "skinColors.png")
    skin_map = {skin.getpixel((i, 0))[:3]: skin.getpixel((i, 2))[:3] for i in range(3)}
    frame.putdata([(*skin_map.get((r, g, b), (r, g, b)), a) for r, g, b, a in frame.get_flattened_data()])
    # First pants style, female sprite block, tinted using actual existing blue cloth colors.
    pants_rect = (rect[0], rect[1] + 688, rect[2], rect[3] + 688)
    pants = image(FARMER / "pants.png").crop(pants_rect)
    cloth = image(FARMER / "shirts.png")
    # Palette is taken from Khoa source pixels, avoiding a newly invented garment palette.
    kp = image(KHOA_SOURCE / "player_down_idle.png")
    blues = sorted({(r, g, b) for r, g, b, a in kp.get_flattened_data() if a and b >= r and g >= r and max(r, g, b) < 150}, key=sum)
    if len(blues) < 3:
        raise ValueError("Expected existing Khoa cloth palette unavailable")
    cloth_palette = [blues[len(blues) // 4], blues[len(blues) // 2], blues[3 * len(blues) // 4]]
    frame.alpha_composite(recolor(pants, cloth_palette))
    # Existing green shirt number7:8x8 directional blocks stacked in32px item row.
    shirt_rect = (56, direction * 8, 64, direction * 8 + 8)
    shirt = cloth.crop(shirt_rect)
    frame.alpha_composite(shirt, (4, 16))
    # Existing short hair style0 is gray-tintable; preserve its geometry and direction.
    hair_rect = (0, direction * 32, 16, direction * 32 + 32)
    hair = image(FARMER / "hairstyles.png").crop(hair_rect)
    browns = sorted({(r, g, b) for r, g, b, a in kp.get_flattened_data() if a and r > g >= b and r < 160}, key=sum)
    if len(browns) < 3:
        raise ValueError("Expected existing Khoa brown palette unavailable")
    hair_palette = [browns[len(browns) // 4], browns[len(browns) // 2], browns[3 * len(browns) // 4]]
    frame.alpha_composite(recolor(hair, hair_palette), (0, 5))
    if facing == "left":
        frame = ImageOps.mirror(frame)
    enlarged = frame.resize((32, 64), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (64, 64))
    canvas.alpha_composite(enlarged, (16, 0))
    canvas, dy = align_ground(canvas)
    return canvas, {"base_rect": rect, "pants_rect": pants_rect, "shirt_rect": shirt_rect, "shirt_placement": [4, 16], "hair_rect": hair_rect, "hair_placement": [0, 5], "skin_palette_row": 2, "cloth_palette_rgb_from_khoa": cloth_palette, "hair_palette_rgb_from_khoa": hair_palette, "mirror": facing == "left", "nearest_scale": 2, "ground_shift_px": dy, "base_alpha_pixels": sum(a != 0 for a in base_alpha.get_flattened_data())}


def sprite_meta(path: Path) -> None:
    spec = importlib.util.spec_from_file_location("sheet_staging", ROOT / "tools/stage_pixel_reference_assets.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.texture_meta(path, None, (64, 64), True)
    meta = path.with_suffix(".png.meta")
    # Bottom pivot at the shared ground line: no frame-to-frame vertical jitter.
    meta.write_text(meta.read_text(encoding="utf-8").replace("alignment: 0", "alignment: 9").replace("spritePivot: {x: 0.5, y: 0.5}", "spritePivot: {x: 0.5, y: 0.0625}"), encoding="utf-8")


def preview(frames: list[dict]) -> None:
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    board = Image.new("RGB", (1080, 680), "#242c38")
    draw = ImageDraw.Draw(board)
    for player_index, player in enumerate(["An", "Khoa"]):
        for facing_index, facing in enumerate(FACING):
            x, y = player_index * 540, facing_index * 160
            draw.text((x + 8, y + 4), f"{player} {facing} | 4 stride slots | CANDIDATE", fill="white")
            for step in range(4):
                row = next(r for r in frames if r["player"] == player and r["clip"] == facing + "_walk" and r["slot"] == step)
                im = image(ROOT / row["path"]).resize((128, 128), Image.Resampling.NEAREST)
                draw.line((x + step * 132, y + 140, x + step * 132 + 128, y + 140), fill="#a5c2ab")
                board.paste(im, (x + step * 132, y + 20), im)
    draw.text((8, 650), "Existing art composition only. Ground line is fixed. Not art approved; no live assets replaced.", fill="white")
    board.save(EVIDENCE / "player-walk-contact.png")
    animation = []
    for facing in FACING:
        for step in range(4):
            im = Image.new("RGB", (512, 300), "#242c38")
            d = ImageDraw.Draw(im)
            d.text((8, 8), f"CANDIDATE | {facing} | An source stride, Khoa existing C frames", fill="white")
            for col, player in enumerate(["An", "Khoa"]):
                row = next(r for r in frames if r["player"] == player and r["clip"] == facing + "_walk" and r["slot"] == step)
                spr = image(ROOT / row["path"]).resize((256, 256), Image.Resampling.NEAREST)
                d.line((col * 256, 272, col * 256 + 255, 272), fill="#a5c2ab")
                im.paste(spr, (col * 256, 32), spr)
            animation.append(im)
    animation[0].save(EVIDENCE / "player-walk-preview.gif", save_all=True, append_images=animation[1:], duration=133, loop=0)


def sheets(frames: list[dict]) -> list[dict]:
    entries = []
    spec = importlib.util.spec_from_file_location("sheet_staging", ROOT / "tools/stage_pixel_reference_assets.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    for player in ["An", "Khoa"]:
        sheet = Image.new("RGBA", (320, 320))
        layout = []
        for row, facing in enumerate(FACING):
            for col in range(5):
                clip = facing + ("_idle" if col == 0 else "_walk")
                slot = 0 if col == 0 else col - 1
                frame = next(f for f in frames if f["player"] == player and f["clip"] == clip and f["slot"] == slot)
                sheet.alpha_composite(image(ROOT / frame["path"]), (col * 64, row * 64))
                layout.append({"row": row, "col": col, "frame": frame["path"]})
        action = next(f for f in frames if f["player"] == player and f["clip"] == "action")
        sheet.alpha_composite(image(ROOT / action["path"]), (0, 256))
        layout.append({"row": 4, "col": 0, "frame": action["path"]})
        path = OUTPUT / player / f"{player.lower()}_animator_sheet.png"
        sheet.save(path)
        module.texture_meta(path, (64, 64), (320, 320), True)
        meta = path.with_suffix(".png.meta")
        meta.write_text(meta.read_text(encoding="utf-8").replace("alignment: 0", "alignment: 9").replace("spritePivot: {x: 0.5, y: 0.5}", "spritePivot: {x: 0.5, y: 0.0625}").replace("pivot: {x: 0.5, y: 0.5}", "pivot: {x: 0.5, y: 0.0625}"), encoding="utf-8")
        entries.append({"player": player, "path": path.relative_to(ROOT).as_posix(), "sha256": sha(path), "size": [320, 320], "frame": [64, 64], "occupied_rectangles": layout, "empty_rectangles": [[4, col] for col in range(1, 5)]})
    module.folder_metas()
    return entries


def verify(manifest: dict) -> None:
    for item in manifest["sources"]:
        if sha(Path(item["path"])) != item["sha256"]:
            raise ValueError(f"Source changed: {item['path']}")
    for row in manifest["frames"]:
        p = ROOT / row["path"]
        im = image(p)
        if im.size != (64, 64) or im.getbbox()[3] != GROUND_Y or sha(p) != row["sha256"]:
            raise ValueError(f"Candidate frame/ground/hash failed: {p}")
        if "spritePivot: {x: 0.5, y: 0.0625}" not in p.with_suffix(".png.meta").read_text(encoding="utf-8"):
            raise ValueError(f"Foot pivot missing: {p}")
        if row["player"] == "An" and any(a not in (0, 255) for a in im.getchannel("A").get_flattened_data()):
            raise ValueError(f"Unexpected interpolated alpha: {p}")
        if row["player"] == "Khoa":
            original, _ = align_ground(image(Path(row["source"])))
            if original.tobytes() != im.tobytes():
                raise ValueError(f"Khoa pixels altered beyond grounding: {p}")
    for player in ["An", "Khoa"]:
        for facing in FACING:
            rows = [r for r in manifest["frames"] if r["player"] == player and r["clip"] == facing + "_walk"]
            if len(rows) != 4:
                raise ValueError(f"Missing stride slots {player} {facing}")
    for row in manifest["sheets"]:
        p = ROOT / row["path"]
        if sha(p) != row["sha256"]:
            raise ValueError(f"Sheet hash changed: {p}")
        sheet = image(p)
        for cell in row["occupied_rectangles"]:
            x, y = cell["col"] * 64, cell["row"] * 64
            if sheet.crop((x, y, x + 64, y + 64)).tobytes() != image(ROOT / cell["frame"]).tobytes():
                raise ValueError(f"Sheet frame mismatch: {p}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify", action="store_true")
    args = parser.parse_args()
    manifest_path = OUTPUT / "player-candidate-manifest.json"
    if args.verify:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        verify(manifest)
        print(f"PASS: {len(manifest['frames'])} candidate frames, exact64x64, alpha, ground y60, source hashes and Khoa pixel preservation")
        return
    frames, used_sources = [], set(FARMER / n for n in ["farmer_girl_base.png", "hairstyles.png", "shirts.png", "pants.png", "skinColors.png"])
    for player in ["An", "Khoa"]:
        dest = OUTPUT / player
        dest.mkdir(parents=True, exist_ok=True)
        for facing in FACING:
            for slot, column in enumerate([0, 1, 0, 2]):
                if player == "An":
                    frame, details = compose_an(facing, column)
                    source = str(FARMER / "farmer_girl_base.png")
                else:
                    source = str(KHOA_SOURCE / f"player_{facing}_walk{slot + 1}.png")
                    frame, dy = align_ground(image(Path(source)))
                    details = {"ground_shift_px": dy, "composition": "Existing C player pixels unchanged except canvas translation"}
                p = dest / f"{player.lower()}_{facing}_walk{slot + 1}.png"
                frame.save(p)
                sprite_meta(p)
                used_sources.add(Path(source))
                frames.append({"player": player, "clip": facing + "_walk", "slot": slot, "source": source, "source_pose_column": column if player == "An" else None, "path": p.relative_to(ROOT).as_posix(), "sha256": sha(p), "details": details})
            if player == "An":
                frame, details = compose_an(facing, 0)
            else:
                source = str(KHOA_SOURCE / f"player_{facing}_idle.png")
                frame, dy = align_ground(image(Path(source)))
                used_sources.add(Path(source))
                details = {"ground_shift_px": dy}
            p = dest / f"{player.lower()}_{facing}_idle.png"
            frame.save(p)
            sprite_meta(p)
            frames.append({"player": player, "clip": facing + "_idle", "slot": 0, "source": str(FARMER / "farmer_girl_base.png") if player == "An" else source, "path": p.relative_to(ROOT).as_posix(), "sha256": sha(p), "details": details})
        if player == "An":
            frame, details = compose_an("down", 0, action=True)
            source = str(FARMER / "farmer_girl_base.png")
        else:
            source = str(KHOA_SOURCE / "player_action.png")
            frame, dy = align_ground(image(Path(source)))
            details = {"ground_shift_px": dy}
            used_sources.add(Path(source))
        p = dest / f"{player.lower()}_action.png"
        frame.save(p)
        sprite_meta(p)
        frames.append({"player": player, "clip": "action", "slot": 0, "source": source, "path": p.relative_to(ROOT).as_posix(), "sha256": sha(p), "details": details})
    # Khoa palette source used to tint An existing grayscale clothing/hair.
    used_sources.add(KHOA_SOURCE / "player_down_idle.png")
    manifest = {"status": "CANDIDATES_NOT_APPROVED_NOT_RUNTIME_BOUND", "ppu": 32, "canvas": [64, 64], "ground_bottom_exclusive": GROUND_Y, "sprite_pivot": [0.5, 0.0625], "walk_fps_from_existing_animator": 7.5, "an_stride": "Existing source contact-left-contact-right columns0,1,0,2, not4 invented unique poses", "action_note": "Single existing source pose, not a claim of complete tool/weapon animation", "frames": frames, "sheets": sheets(frames), "sources": [{"path": str(p), "sha256": sha(p)} for p in sorted(used_sources)]}
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    preview(frames)
    verify(manifest)
    print(f"PASS: staged {len(frames)} grounded existing-art player candidate frames; no live sprites replaced")


if __name__ == "__main__":
    main()
