"""Turn AI-generated pixel-art strips (Argon) into clean game sheets.

Input: ArtSource/ArgonSprites/<family>_<dir>_<clip>.png — one horizontal strip per direction/clip on a flat
magenta (#FF00FF) background, frames equally spaced. AI images are "pseudo pixel art": the art grid is
upscaled by a non-obvious factor, edges carry soft fringe and colours drift. This tool:
  1. keys out magenta (and its pink/violet fringe),
  2. finds the art-pixel size by testing integer and fractional scales and keeping the one whose block-mode
     reconstruction differs least from the image,
  3. samples each art pixel by the MODE colour of its block (no averaging -> no mush),
  4. splits frames by transparent column gaps (falls back to equal division by the expected count),
  5. snaps every frame to one baseline and one cell, quantises the family to one shared palette,
  6. writes <family>_<clip>.png/.json in the same schema pixelize.py writes (rows down,left,up,right).

  python tools/pixelart/ai_sprite_cleanup.py --family khoa --frames idle=4,walk=6,run=6,attack=6,interact=4,whistle=4
"""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "ArtSource" / "GptSprites"  # --src to override (ArgonSprites, ...)
OUT = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters"
DIRS = ["down", "left", "up", "right"]


def key_magenta(rgb):
    r, g, b = rgb[..., 0].astype(int), rgb[..., 1].astype(int), rgb[..., 2].astype(int)
    magenta = (r > 150) & (b > 150) & (g < 110) & (np.abs(r - b) < 90)
    return ~magenta


def best_scale(rgb, alpha, lo=2.0, hi=12.0):
    """Art-pixel size: the scale whose nearest-sample reconstruction best matches the image."""
    h, w, _ = rgb.shape
    best, best_err = 1.0, 1e18
    ys, xs = np.nonzero(alpha)
    if len(ys) == 0:
        return 1.0
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    crop = rgb[y0:y1, x0:x1].astype(np.float32)
    ch, cw = crop.shape[:2]
    for s in np.arange(lo, hi + 0.001, 0.25):
        gh, gw = int(ch / s), int(cw / s)
        if gh < 8 or gw < 4:
            continue
        cy = ((np.arange(gh) + 0.5) * s).astype(int).clip(0, ch - 1)
        cx = ((np.arange(gw) + 0.5) * s).astype(int).clip(0, cw - 1)
        small = crop[cy][:, cx]
        uy = (np.arange(ch) / s).astype(int).clip(0, gh - 1)
        ux = (np.arange(cw) / s).astype(int).clip(0, gw - 1)
        recon = small[uy][:, ux]
        err = np.abs(recon - crop).mean() + 0.6 * s  # mild bias to larger art pixels (fewer, cleaner)
        if err < best_err:
            best, best_err = s, err
    return float(best)


def block_mode(rgb, alpha, s):
    """Sample each art pixel by the most common opaque colour in its block."""
    h, w, _ = rgb.shape
    gh, gw = int(h / s), int(w / s)
    out = np.zeros((gh, gw, 4), dtype=np.uint8)
    for gy in range(gh):
        ya, yb = int(gy * s), max(int(gy * s) + 1, int((gy + 1) * s))
        for gx in range(gw):
            xa, xb = int(gx * s), max(int(gx * s) + 1, int((gx + 1) * s))
            a = alpha[ya:yb, xa:xb]
            if a.mean() < 0.5:
                continue
            block = rgb[ya:yb, xa:xb][a]
            q = (block // 8).astype(np.int32)
            keys = q[:, 0] * 1024 + q[:, 1] * 32 + q[:, 2]
            vals, counts = np.unique(keys, return_counts=True)
            k = vals[counts.argmax()]
            sel = block[keys == k].mean(0)
            out[gy, gx, :3] = sel.astype(np.uint8)
            out[gy, gx, 3] = 255
    return out


def split_frames(img, expected):
    a = img[..., 3] > 0
    cols = a.any(0)
    runs, start = [], None
    for x, c in enumerate(list(cols) + [False]):
        if c and start is None:
            start = x
        elif not c and start is not None:
            runs.append((start, x))
            start = None
    # drop specks (< 3 art px wide) that would count as extra frames
    runs = [r for r in runs if r[1] - r[0] >= 3]
    if len(runs) == expected:
        return [img[:, x0:x1] for x0, x1 in runs]
    print(f"  WARN segmentation found {len(runs)} sprites, expected {expected}: equal split used - REVIEW")
    # merge/split by equal division when the AI left touching or detached bits
    w = img.shape[1]
    step = w / expected
    return [img[:, int(i * step):int((i + 1) * step)] for i in range(expected)]


def trim(frame):
    a = frame[..., 3] > 0
    if not a.any():
        return frame[:1, :1], 0, 0
    ys, xs = np.nonzero(a)
    return frame[ys.min():ys.max() + 1, xs.min():xs.max() + 1], xs.min(), ys.max()


def kmeans(px, k, iters=20):
    order = np.argsort(px @ np.array([0.299, 0.587, 0.114]))
    cent = px[order[np.linspace(0, len(order) - 1, k).astype(int)]].astype(np.float32)
    for _ in range(iters):
        lab = ((px[:, None] - cent[None]) ** 2).sum(-1).argmin(1)
        for j in range(k):
            m = lab == j
            if m.any():
                cent[j] = px[m].mean(0)
    return cent


def segment_fullres(rgb, alpha, expected):
    """Split a raw strip into frames by 8-connected 2D components on the FULL-RES alpha (codex R5: frames
    overlap in X projection, so column runs merge them). The `expected` largest components are frames; small
    detached bits (hair strands, sword tips, sparks) join the frame whose bbox (padded) contains their centroid.
    Each frame keeps its OWN mask, so a neighbour's pixels inside an overlapping bbox are not included."""
    lab, n = ndimage.label(alpha, structure=np.ones((3, 3)))
    if n == 0:
        return []
    areas = ndimage.sum(alpha, lab, range(1, n + 1))
    order = np.argsort(-areas)
    main = [int(i) + 1 for i in order[:expected]]
    if len(main) < expected:
        print(f"  WARN only {len(main)} components for {expected} frames - REVIEW")
    boxes = {m: ndimage.find_objects((lab == m).astype(int))[0] for m in main}
    masks = {m: lab == m for m in main}
    for i in order[expected:]:
        c = int(i) + 1
        if areas[i] < 30:
            continue
        cy, cx = ndimage.center_of_mass(lab == c)
        best, bd = None, 1e18
        for m, (sy, sx) in boxes.items():
            pad = 0.15 * (sx.stop - sx.start)
            if sx.start - pad <= cx <= sx.stop + pad:
                d = abs(cx - (sx.start + sx.stop) / 2)
                if d < bd:
                    best, bd = m, d
        if best is not None:
            masks[best] |= lab == c
    frames = []
    for m in sorted(main, key=lambda m: (boxes[m][1].start + boxes[m][1].stop) / 2):
        ys, xs = np.nonzero(masks[m])
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        frames.append((rgb[y0:y1, x0:x1], masks[m][y0:y1, x0:x1]))
    return frames


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--family", required=True)
    ap.add_argument("--src", default=str(SRC))
    ap.add_argument("--frames", required=True, help="clip=count,...")
    ap.add_argument("--colors", type=int, default=40)
    ap.add_argument("--target-height", type=int, default=0, help="optional: rescale art so the tallest frame is N px")
    ap.add_argument("--uniform", action="store_true",
                    help="one scale for the whole family, taken from the down/<first clip> strip (quadrupeds: side views are lower than front views and must not be stretched to the same height)")
    ap.add_argument("--fps", default="idle=6,walk=10,run=14,attack=14,interact=10,whistle=8,move=10,eat=8,sleep=3,hit=12,death=10")
    a = ap.parse_args()
    clips = dict((kv.split("=")[0], int(kv.split("=")[1])) for kv in a.frames.split(","))
    fps = dict((kv.split("=")[0], float(kv.split("=")[1])) for kv in a.fps.split(","))
    frames = {}
    uniform_s = None
    dir_ref_px = {}
    for clip, n in clips.items():
        for d in DIRS:
            f = Path(a.src) / f"{a.family}_{d}_{clip}.png"
            if not f.exists():
                print("MISSING", f.name)
                continue
            rgb = np.asarray(Image.open(f).convert("RGB"))
            alpha = key_magenta(rgb)
            alpha = ndimage.binary_opening(alpha, iterations=1)
            raw_frames = segment_fullres(rgb, alpha, n)
            # one stable scale per strip from its FIRST frame (upright/neutral pose): crouches stay lower and a
            # raised sword does not shrink the body (codex R5 #35/#37)
            ref_h = raw_frames[0][0].shape[0] if raw_frames else 1
            s = ref_h / float(a.target_height) if a.target_height > 0 else best_scale(rgb, alpha)
            if a.uniform:
                # family scale from the first strip (down/<first clip>); every other strip's FIRST (neutral) frame is
                # matched to the same direction's first-clip height, which cancels per-strip zoom drift in the AI
                # strips while keeping side views lower than front views
                if uniform_s is None:
                    uniform_s = s
                if d in dir_ref_px:
                    s = ref_h / dir_ref_px[d]
                else:
                    s = uniform_s
                    dir_ref_px[d] = ref_h / uniform_s
            parts = []
            for fr_rgb, fr_mask in raw_frames:
                small = block_mode(fr_rgb, fr_mask, s)
                parts.append(small)
            frames[(clip, d)] = [trim(p)[0] for p in parts]
            art = None
            print(f"{f.name}: scale {s:.2f}px/art-px, {len(parts)} frames")
    if not frames:
        raise SystemExit("no input strips")
    allpx = np.concatenate([fr[fr[..., 3] > 0][:, :3] for lst in frames.values() for fr in lst]).astype(np.float32)
    if len(allpx) > 80000:
        allpx = allpx[np.random.default_rng(1).choice(len(allpx), 80000, replace=False)]
    pal = kmeans(allpx, a.colors)
    (ROOT / "tools" / "pixelart" / "palettes").mkdir(exist_ok=True)
    (ROOT / "tools" / "pixelart" / "palettes" / f"{a.family}_argon.json").write_text(json.dumps({"rgb": pal.round().astype(int).tolist()}))
    maxw = max(fr.shape[1] for lst in frames.values() for fr in lst)
    maxh = max(fr.shape[0] for lst in frames.values() for fr in lst)
    cw, ch = maxw + 2 + (maxw % 2), maxh + 2
    out_dir = OUT / a.family
    out_dir.mkdir(parents=True, exist_ok=True)
    for clip, n in clips.items():
        sheet = np.zeros((ch * 4, cw * n, 4), dtype=np.uint8)
        for r, d in enumerate(DIRS):
            lst = frames.get((clip, d)) or frames.get((clip, "down"))
            if lst is None:
                continue
            for c in range(n):
                fr = lst[min(c, len(lst) - 1)]
                q = fr.copy()
                m = q[..., 3] > 0
                flat = q[..., :3].reshape(-1, 3).astype(np.float32)
                idx = ((flat[:, None] - pal[None]) ** 2).sum(-1).argmin(1)
                q[..., :3] = pal[idx].reshape(q.shape[:2] + (3,)).round().astype(np.uint8)
                q[~m] = 0
                fh, fw = q.shape[:2]
                # anchor on the feet (bottom 15% of the silhouette), not the bbox centre: an extended sword or
                # arm must not slide the body sideways between frames (codex R5 review)
                foot = m[int(fh * 0.85):, :]
                cols = np.nonzero(foot.any(0))[0]
                fx = int((cols.min() + cols.max()) / 2) if len(cols) else fw // 2
                ox = c * cw + cw // 2 - fx
                ox = max(c * cw, min(ox, c * cw + cw - fw))
                oy = r * ch + (ch - 1 - fh)  # feet on the cell's bottom row
                sheet[oy:oy + fh, ox:ox + fw] = np.where(m[..., None], q, sheet[oy:oy + fh, ox:ox + fw])
        png = out_dir / f"{a.family}_{clip}.png"
        Image.fromarray(sheet, "RGBA").save(png)
        png.with_suffix(".json").write_text(json.dumps({
            "source": f"AI-generated strips ({Path(a.src).name}, GPT built-in image generation for GptSprites) via ai_sprite_cleanup.py", "cell": [cw, ch], "rows": DIRS,
            "frames_per_row": n, "pivot_px_from_bottom_left": [cw // 2, 1], "fps": fps.get(clip, 10.0),
            "colors": a.colors}, indent=2))
        print("SHEET", png, [cw, ch], n)


if __name__ == "__main__":
    main()
