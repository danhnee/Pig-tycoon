"""Render + pixelize every sprite family listed in sprite_jobs.json, one Blender job at a time.

  python tools/pixelart/batch_sprites.py [--only khoa,pig] [--skip-render]

Renders go to .agent_scratch/pixel-pipeline/renders/<family>/<clip>, sheets to
Assets/Art/PixelMyth/Characters/<family>/<family>_<clip>.png (+ .json). All clips of a family share one
palette file, so Walk/Run/Attack never shift colour against each other.
"""
import argparse
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
JOBS = Path(__file__).with_name("sprite_jobs.json")
RENDERS = ROOT / ".agent_scratch" / "pixel-pipeline" / "renders"
SHEETS = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters"
PALETTES = ROOT / "tools" / "pixelart" / "palettes"


def run(cmd):
    print(">", " ".join(str(c) for c in cmd), flush=True)
    r = subprocess.run(cmd, cwd=ROOT)
    if r.returncode != 0:
        raise SystemExit(f"failed ({r.returncode}): {cmd[:4]}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--skip-render", action="store_true")
    a = ap.parse_args()
    spec = json.loads(JOBS.read_text(encoding="utf-8"))
    only = {x for x in a.only.split(",") if x}
    for fam in spec["families"]:
        if only and fam["name"] not in only:
            continue
        srcs = []
        for clip in fam["clips"]:
            rdir = RENDERS / fam["name"] / clip["name"]
            srcs.append((clip, rdir))
            if a.skip_render and (rdir / "manifest.json").exists():
                continue
            fbx_root = Path(fam.get("fbx_root", spec["fbx_root"]))
            cmd = [sys.executable, "tools/headless_runner.py", "blender", "--min-free-mb", "600", "--max-mem-mb", "2048",
                   "--wait-sec", "5400", "--no-cache", "--script", "tools/pixelart/render_fbx_sprites.py", "--",
                   "--fbx", str(fbx_root / clip["fbx"]), "--out", str(rdir),
                   "--dirs", str(fam.get("dirs", 4)), "--frames", str(clip["frames"]),
                   "--px-per-m", str(fam.get("px_per_m", 32)), "--supersample", str(fam.get("supersample", 4)),
                   "--facing", str(fam.get("facing", 0)),
                   "--elev", str(fam.get("elev", 30)), "--head-scale", str(fam.get("head_scale", 1.0))] + (["--no-denoise", "--samples", "48"] if fam.get("denoise") is False else [])
            run(cmd)
        out_dir = SHEETS.parent / fam.get("category", "Characters") / fam["name"]
        pal = PALETTES / f"{fam['name']}.json"
        for clip, rdir in srcs:
            run([sys.executable, "tools/pixelart/pixelize.py", "--src", str(rdir),
                 "--out", str(out_dir / f"{fam['name']}_{clip['name']}.png"), "--palette", str(pal),
                 "--colors", str(fam.get("colors", 28)), "--fps", str(clip.get("fps", 10)),
                 "--sharpen", str(fam.get("sharpen", 0.0)), "--inner-lines", str(fam.get("inner_lines", 0.0))])


if __name__ == "__main__":
    main()
