"""Import Argon's building / prop / gate PNGs into the PixelMyth asset layout.

Input:  D:/Pig-tycoon/.collab/claude-2d/argon-deliverables/<group>/<name>.png   (1x, 32 px = 1 m, #FF00FF background)
Output: Assets/Art/PixelMyth/Buildings/<prefix><name>/<prefix><name>_static.png (+ .json): 4 identical rows
        [down,left,up,right], one frame each, pivot at the bottom centre of the opaque base, like the rendered sheets.
        group 'gate' instead feeds Characters/mystic_gate (idle = *_dormant, active = *_active 4-cell strip).
Tolerant magenta key (AI images drift off #FF00FF), alpha trimmed, no resampling (art is already pixel-exact at 1x;
an 8x master is box-reduced if a *_raw_8x/ copy is the only file).

    python tools/pixelart/import_argon_art.py town village forest_sites gate [--src DIR] [--dry-run]
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

import shutil

ROOT = Path(__file__).resolve().parents[2]
SRC = Path("D:/Pig-tycoon/.collab/claude-2d/argon-deliverables")
BLD = ROOT / "Assets" / "Art" / "PixelMyth" / "Buildings"
CHR = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters"
DEC = ROOT / "Assets" / "Art" / "PixelMyth" / "Decor"
TER = ROOT / "Assets" / "Art" / "PixelMyth" / "Terrain"
PREFIX = {
    "town": "argon_town_", "town_v2": "argon_town_",
    "village": "argon_village_", "village_v2": "argon_village_",
    "forest_sites": "argon_site_", "forest_sites_v2": "argon_site_",
    "village_decor": "argon_vdecor_", "wave2_world_enrichment": "argon_w2_",
    "farm_defense_v2": "", "props_v2": "", "environment_v2": "",
}
UNPREFIXED_OVERRIDES = {
    "town_market_hall", "approved_main_house", "approved_research_lab",
    "bldg_feed_storage", "bldg_quarantine_zone", "bldg_disposal_area",
    "bldg_herd_shelter", "bldg_market_counter", "bldg_night_stall",
    "bldg_observatory", "prop_capacity_board", "prop_care_post",
    "prop_feeder", "prop_water_basin", "fence_gate_arch",
}


def key(img):
    rgb = np.asarray(img.convert("RGB")).astype(int)
    a = np.asarray(img.convert("RGBA"))[..., 3]
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    magenta = (r > 170) & (b > 150) & (g < 110) & (abs(r - b) < 90)
    alpha = np.where(magenta | (a < 128), 0, 255).astype(np.uint8)
    out = np.dstack([rgb.astype(np.uint8), alpha])
    return out


def trim(rgba):
    ys, xs = np.where(rgba[..., 3] > 0)
    if len(xs) == 0:
        return rgba
    return rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def maybe_reduce(rgba, expect_w_px=None):
    """8x masters: box-reduce by the integer factor that brings the width under ~420 px (largest buildings ~12 m)."""
    h, w = rgba.shape[:2]
    f = 1
    for k in (8, 6, 4, 3, 2):
        if w / k >= 24 and w > 420:
            f = k
            break
    if f == 1:
        return rgba
    h2, w2 = h // f, w // f
    rgba = rgba[:h2 * f, :w2 * f]
    blocks = rgba.reshape(h2, f, w2, f, 4)
    a = blocks[..., 3].mean(axis=(1, 3))
    # mode colour per block (keeps hard pixel edges)
    flat = blocks.transpose(0, 2, 1, 3, 4).reshape(h2, w2, f * f, 4)
    out = np.zeros((h2, w2, 4), np.uint8)
    for y in range(h2):
        for x in range(w2):
            px = flat[y, x]
            px = px[px[:, 3] > 0]
            if len(px) and a[y, x] >= 128:
                vals, counts = np.unique(px[:, :3], axis=0, return_counts=True)
                out[y, x, :3] = vals[counts.argmax()]
                out[y, x, 3] = 255
    return out


def sheet_rows(cell, rows=4):
    h, w = cell.shape[:2]
    sheet = np.zeros((h * rows, w, 4), np.uint8)
    for r in range(rows):
        sheet[r * h:(r + 1) * h] = cell
    return sheet


def import_building(png, prefix, dry):
    stem = png.stem[:-7] if png.stem.endswith("_static") else png.stem
    name = stem if (stem in UNPREFIXED_OVERRIDES or stem.startswith("argon_")) else (prefix + stem)
    cell = trim(maybe_reduce(key(Image.open(png))))
    h, w = cell.shape[:2]
    out_dir = BLD / name
    meta = {"source": f"Argon deliverable {png.as_posix()} (sha256 {hashlib.sha256(png.read_bytes()).hexdigest()[:16]})",
            "cell": [w, h], "rows": ["down", "left", "up", "right"], "frames_per_row": 1,
            "pivot_px_from_bottom_left": [w // 2, max(2, h // 20)], "fps": 1.0, "colors": 0}
    if not dry:
        out_dir.mkdir(parents=True, exist_ok=True)
        Image.fromarray(sheet_rows(cell), "RGBA").save(out_dir / f"{name}_static.png")
        (out_dir / f"{name}_static.json").write_text(json.dumps(meta, indent=2))
        if name == "no_xuyen_van":
            alias_dir = BLD / "bldg_noxuyenvan_tower"
            alias_dir.mkdir(parents=True, exist_ok=True)
            shutil.copy2(out_dir / f"{name}_static.png", alias_dir / "bldg_noxuyenvan_tower_static.png")
            shutil.copy2(out_dir / f"{name}_static.json", alias_dir / "bldg_noxuyenvan_tower_static.json")
    return name, w / 32.0, h / 32.0


def import_prebuilt_buildings(src, prefix, dry):
    done = []
    for sub in sorted(src.iterdir()):
        if not sub.is_dir() or sub.name.startswith("_"):
            continue
        static_png = sub / f"{sub.name}_static.png"
        static_json = sub / f"{sub.name}_static.json"
        if not static_png.exists():
            continue
        name = sub.name if (sub.name in UNPREFIXED_OVERRIDES or sub.name.startswith("argon_")) else (prefix + sub.name)
        meta = json.loads(static_json.read_text(encoding="utf-8")) if static_json.exists() else None
        if not dry:
            for target_name in ({name, sub.name} if prefix else {name}):
                out_dir = BLD / target_name
                out_dir.mkdir(parents=True, exist_ok=True)
                shutil.copy2(static_png, out_dir / f"{target_name}_static.png")
                if static_json.exists():
                    shutil.copy2(static_json, out_dir / f"{target_name}_static.json")
        cw, ch = (meta.get("cell", [64, 64]) if meta else [64, 64])
        done.append((name, cw / 32.0, ch / 32.0))
    return done


def import_characters_dir(src, dry):
    done = []
    if not dry:
        for legacy in ("alien_grunt", "alien_elite", "boss_alien_warlord",
                       "argon_alien_grunt", "argon_alien_elite", "argon_boss_alien_warlord"):
            leg_dir = CHR / legacy
            if leg_dir.exists():
                shutil.rmtree(leg_dir, ignore_errors=True)
    for sub in sorted(src.iterdir()):
        if not sub.is_dir() or sub.name.startswith("_"):
            continue
        cid = sub.name
        targets = [cid]
        if not cid.startswith("argon_"):
            targets.append("argon_" + cid)
        if cid == "pig_white_foot":
            targets.extend(["pig_white_feet", "argon_pig_white_feet"])
        if not dry:
            for tid in targets:
                out_dir = CHR / tid
                out_dir.mkdir(parents=True, exist_ok=True)
                for f in sub.iterdir():
                    if f.suffix in (".png", ".json"):
                        new_name = tid + f.name[len(cid):] if f.name.startswith(cid) else f.name
                        shutil.copy2(f, out_dir / new_name)
        done.append((cid, 1.5, 1.5))
    return done


def import_decor_terrain(base_src, dry):
    staged = Path(base_src) / "maps_and_code_v2" / "staged_integration" / "Assets" / "Art" / "PixelMyth"
    done = []
    for folder, dst in (("Decor", DEC), ("Terrain", TER)):
        sdir = staged / folder
        if not sdir.exists():
            continue
        if not dry:
            dst.mkdir(parents=True, exist_ok=True)
            for f in sdir.iterdir():
                if f.is_file() and f.suffix in (".png", ".json"):
                    shutil.copy2(f, dst / f.name)
        done.append((folder, len(list(sdir.glob("*.png"))), 0.0))
    return done


def import_gate(src, dry):
    dormant = sorted(src.glob("*_dormant.png"))
    active = sorted(src.glob("*_active.png"))
    if not active:
        return []
    strip = key(Image.open(active[0]))
    h, w = strip.shape[:2]
    cw = w // 4
    frames = [trim(strip[:, i * cw:(i + 1) * cw]) for i in range(4)]
    fh = max(f.shape[0] for f in frames)
    fw = max(f.shape[1] for f in frames)

    def pad(f):
        out = np.zeros((fh, fw, 4), np.uint8)
        y0, x0 = fh - f.shape[0], (fw - f.shape[1]) // 2
        out[y0:, x0:x0 + f.shape[1]] = f
        return out
    frames = [pad(f) for f in frames]
    idle = [pad(trim(key(Image.open(dormant[0]))))] if dormant else frames[:1]
    if not dry:
        CHR.joinpath("mystic_gate").mkdir(parents=True, exist_ok=True)
        for clip, fr, fps in (("idle", idle, 1.0), ("active", frames, 6.0)):
            sheet = np.zeros((fh * 4, fw * len(fr), 4), np.uint8)
            for r in range(4):
                for i, f in enumerate(fr):
                    sheet[r * fh:(r + 1) * fh, i * fw:(i + 1) * fw] = f[:fh, :fw]
            Image.fromarray(sheet, "RGBA").save(CHR / "mystic_gate" / f"mystic_gate_{clip}.png")
            (CHR / "mystic_gate" / f"mystic_gate_{clip}.json").write_text(json.dumps({
                "source": f"Argon deliverable {active[0].as_posix()}", "cell": [fw, fh],
                "rows": ["down", "left", "up", "right"], "frames_per_row": len(fr),
                "pivot_px_from_bottom_left": [fw // 2, 4], "fps": fps, "colors": 0}, indent=2))
    return [("mystic_gate", fw / 32.0, fh / 32.0)]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("groups", nargs="*", default=["all"])
    ap.add_argument("--src", default=str(SRC))
    ap.add_argument("--dry-run", action="store_true")
    a = ap.parse_args()
    groups = a.groups
    if "all" in groups:
        groups = ["town_v2", "village_v2", "forest_sites_v2", "farm_defense_v2", "props_v2", "environment_v2",
                  "village_decor", "wave2_world_enrichment", "characters_v2", "creatures_vfx_v2", "decor_terrain", "gate"]
    for g in groups:
        src = Path(a.src) / g
        if g == "gate":
            done = import_gate(src, a.dry_run) if src.exists() else []
        elif g in ("characters_v2", "creatures_vfx_v2"):
            done = import_characters_dir(src, a.dry_run) if src.exists() else []
        elif g == "decor_terrain":
            done = import_decor_terrain(a.src, a.dry_run)
        else:
            if not src.exists() and (Path(a.src) / (g + "_v2")).exists():
                src = Path(a.src) / (g + "_v2")
                g = g + "_v2"
            done = import_prebuilt_buildings(src, PREFIX.get(g, ""), a.dry_run) if src.exists() else []
            pngs = [p for p in sorted(src.glob("*.png")) if "contact" not in p.stem and not p.stem.startswith("_")] if src.exists() else []
            done.extend(import_building(p, PREFIX.get(g, ""), a.dry_run) for p in pngs)
        for name, wm, hm in done:
            print(f"{g}: {name}  {wm:.1f} x {hm:.1f} m")


if __name__ == "__main__":
    main()
