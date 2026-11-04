"""Turn supersampled RGBA renders (render_fbx_sprites.py) into a pixel-art sprite sheet.

  python tools/pixelart/pixelize.py --src <render dir> --out <sheet.png> [--palette <family.json>] [--colors 28]

Steps, in order, all deterministic:
  1. integer box downsample with premultiplied alpha (factor = manifest supersample)
  2. hard alpha (>= 50 % coverage becomes opaque) - pixel art has no soft edges
  3. shared palette: k-means over every frame of the family, so colours never flicker between frames
     or between clips of one character; reuse with --palette to keep Walk/Run/Attack identical
  4. selective outline: transparent pixels touching the silhouette get a darkened, warm-shifted copy
     of the neighbour colour (reads better than flat black at phone size)
  5. pack: rows = directions, columns = frames; one cell size and one foot pivot for the whole sheet
Writes <sheet>.png and <sheet>.json (cell size, pivot, rows, fps hint).
"""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

OUTLINE_TINT = np.array([46, 26, 18], dtype=np.float32)   # deep umber, the darkest ink of the gold palette


def load_frames(src):
    man = json.loads((src / "manifest.json").read_text(encoding="utf-8"))
    frames = {}
    for name in man["files"]:
        frames[name] = np.asarray(Image.open(src / name).convert("RGBA"), dtype=np.float32) / 255.0
    return man, frames


def box_down(img, f):
    if f == 1:
        return img
    h, w, _ = img.shape
    h2, w2 = h // f, w // f
    img = img[: h2 * f, : w2 * f]
    a = img[..., 3:4]
    pre = np.concatenate([img[..., :3] * a, a], axis=2)
    pre = pre.reshape(h2, f, w2, f, 4).mean(axis=(1, 3))
    a2 = pre[..., 3:4]
    rgb = np.where(a2 > 1e-6, pre[..., :3] / np.maximum(a2, 1e-6), 0)
    return np.concatenate([rgb, a2], axis=2)


def grade(rgb, contrast, sat):
    """Light pre-quantisation grade: a little contrast and saturation so small sprites keep form."""
    lum = rgb @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    rgb = lum[..., None] + (rgb - lum[..., None]) * sat
    rgb = (rgb - 0.5) * contrast + 0.5
    return np.clip(rgb, 0, 1)


def sharpen(rgb, alpha, amount):
    """Unsharp mask inside the silhouette so facial features and folds survive the palette."""
    if amount <= 0:
        return rgb
    pad = np.pad(rgb, ((1, 1), (1, 1), (0, 0)), mode="edge")
    blur = (pad[:-2, 1:-1] + pad[2:, 1:-1] + pad[1:-1, :-2] + pad[1:-1, 2:] + rgb) / 5.0
    out = np.clip(rgb + (rgb - blur) * amount, 0, 1)
    return np.where(alpha[..., None] > 0.5, out, rgb)


def inner_lines(rgba, pal, threshold):
    """Darken the darker side of strong luminance edges inside the sprite (hair/face, eyes, clothing seams)."""
    if threshold <= 0:
        return rgba
    a = rgba[..., 3] > 0.5
    lum = rgba[..., :3] @ np.array([0.299, 0.587, 0.114])
    out = rgba.copy()
    darker = np.zeros_like(a)
    for dy, dx in ((0, 1), (1, 0)):
        nb = np.roll(np.roll(lum, -dy, 0), -dx, 1)
        na = np.roll(np.roll(a, -dy, 0), -dx, 1)
        edge = a & na & (np.abs(lum - nb) > threshold)
        darker |= edge & (lum < nb)
        darker |= np.roll(np.roll(edge & (nb < lum), dy, 0), dx, 1)
    ink = out[..., :3] * 0.55
    flat = ink.reshape(-1, 3)
    d = ((flat[:, None, :] - pal[None, :, :]) ** 2).sum(-1)
    out[..., :3] = np.where(darker[..., None], pal[d.argmin(1)].reshape(ink.shape), out[..., :3])
    return out


def kmeans(pixels, k, iters=24, seed=7):
    rng = np.random.default_rng(seed)
    k = min(k, len(pixels))
    # k-means++ style seeding on luminance-sorted samples for stable, repeatable palettes
    order = np.argsort(pixels @ np.array([0.299, 0.587, 0.114]))
    cent = pixels[order[np.linspace(0, len(order) - 1, k).astype(int)]].copy()
    for _ in range(iters):
        d = ((pixels[:, None, :] - cent[None, :, :]) ** 2).sum(-1)
        lab = d.argmin(1)
        for j in range(k):
            m = lab == j
            if m.any():
                cent[j] = pixels[m].mean(0)
            else:
                cent[j] = pixels[rng.integers(len(pixels))]
    return cent


def quantize(rgb, pal):
    flat = rgb.reshape(-1, 3)
    d = ((flat[:, None, :] - pal[None, :, :]) ** 2).sum(-1)
    return pal[d.argmin(1)].reshape(rgb.shape)


def outline(rgba, pal):
    """Selective outline that stays inside the palette budget: each edge pixel takes the ink of ONE
    neighbour's palette colour (first opaque of up, left, right, down), not an average; codex R2 measured
    100-363 colours per sheet when averaging."""
    a = rgba[..., 3] > 0.5
    h, w = a.shape
    inks = pal * 0.32 + OUTLINE_TINT / 255.0 * 0.68
    out = rgba.copy()
    flat = rgba[..., :3].reshape(-1, 3)
    d = ((flat[:, None, :] - pal[None, :, :]) ** 2).sum(-1)
    idx = d.argmin(1).reshape(h, w)
    chosen = np.full((h, w), -1, dtype=int)
    for dy, dx in ((-1, 0), (0, -1), (0, 1), (1, 0)):
        sa = np.zeros_like(a)
        si = np.full((h, w), -1, dtype=int)
        ys0, ys1 = max(0, -dy), h - max(0, dy)
        xs0, xs1 = max(0, -dx), w - max(0, dx)
        sa[ys0:ys1, xs0:xs1] = a[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx]
        si[ys0:ys1, xs0:xs1] = idx[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx]
        take = (~a) & sa & (chosen < 0)
        chosen[take] = si[take]
    edge = chosen >= 0
    out[edge, :3] = inks[chosen[edge]]
    out[edge, 3] = 1.0
    return out


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--src", required=True, action="append", help="render dir; repeat to build one palette across clips")
    p.add_argument("--out", required=True, help="sheet png path for the FIRST --src; others get <stem>_<n>")
    p.add_argument("--palette", help="family palette json; created if missing, reused if present")
    p.add_argument("--colors", type=int, default=28)
    p.add_argument("--contrast", type=float, default=1.08)
    p.add_argument("--sat", type=float, default=1.12)
    p.add_argument("--fps", type=float, default=10.0)
    p.add_argument("--no-outline", action="store_true")
    p.add_argument("--sharpen", type=float, default=0.0)
    p.add_argument("--inner-lines", type=float, default=0.0, help="luminance step that gets an inner line (0 = off)")
    a = p.parse_args()

    clips = []
    for s in a.src:
        man, frames = load_frames(Path(s))
        f = int(man.get("supersample") or 1)
        small = {n: box_down(img, f) for n, img in frames.items()}
        for n, img in small.items():
            img[..., 3] = (img[..., 3] >= 0.5).astype(np.float32)
            img[..., :3] = grade(img[..., :3], a.contrast, a.sat)
            img[..., :3] = sharpen(img[..., :3], img[..., 3], a.sharpen)
        clips.append((Path(s), man, small))

    pal_path = Path(a.palette) if a.palette else None
    if pal_path and pal_path.exists():
        pal = np.array(json.loads(pal_path.read_text())["rgb"], dtype=np.float32) / 255.0
    else:
        px = np.concatenate([img[img[..., 3] > 0.5][:, :3] for _, _, sm in clips for img in sm.values()])
        if len(px) > 60000:
            px = px[np.random.default_rng(3).choice(len(px), 60000, replace=False)]
        pal = kmeans(px, a.colors)
        if pal_path:
            pal_path.parent.mkdir(parents=True, exist_ok=True)
            pal_path.write_text(json.dumps({"rgb": (pal * 255).round().astype(int).tolist()}))

    for idx, (src, man, small) in enumerate(clips):
        for n, img in small.items():
            m = img[..., 3] > 0.5
            q = quantize(img[..., :3], pal)
            img[..., :3] = np.where(m[..., None], q, 0)
            img = inner_lines(img, pal, a.inner_lines)
            if not a.no_outline:
                small[n] = outline(img, pal)
        # union bbox -> one cell and one foot pivot for every frame of this clip
        alpha_any = np.zeros_like(next(iter(small.values()))[..., 3], dtype=bool)
        for img in small.values():
            alpha_any |= img[..., 3] > 0.5
        ys, xs = np.nonzero(alpha_any)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        cw, ch = x1 - x0, y1 - y0
        cw += cw % 2
        dirs = man["directions"]
        nfr = len(man["files"]) // len(dirs)
        sheet = Image.new("RGBA", (cw * nfr, ch * len(dirs)), (0, 0, 0, 0))
        for r, d in enumerate(dirs):
            for c in range(nfr):
                img = small[f"{d}_{c:02d}.png"]
                tile = (np.clip(img[y0:y1, x0:x0 + cw], 0, 1) * 255).round().astype(np.uint8)
                sheet.paste(Image.fromarray(tile, "RGBA"), (c * cw, r * ch))
        out = Path(a.out) if idx == 0 else Path(a.out).with_name(f"{Path(a.out).stem}_{idx}.png")
        out.parent.mkdir(parents=True, exist_ok=True)
        sheet.save(out)
        # Foot pivot = projected model root (manifest root_uv); fall back to bottom-centre.
        hs, ws = alpha_any.shape
        if "root_uv" in man:
            rx = man["root_uv"][0] * ws
            ry = (1.0 - man["root_uv"][1]) * hs
            pivot = [int(round(rx - x0)), int(round(y1 - ry))]
        else:
            pivot = [int(cw // 2), 1]
        meta = {
            "source": str(src),
            "cell": [int(cw), int(ch)],
            "rows": dirs,
            "frames_per_row": nfr,
            "pivot_px_from_bottom_left": pivot,
            "fps": a.fps,
            "colors": int(len(pal)),
        }
        out.with_suffix(".json").write_text(json.dumps(meta, indent=2))
        print("SHEET", out, meta["cell"], "x", nfr, "frames x", len(dirs), "dirs")


if __name__ == "__main__":
    main()
