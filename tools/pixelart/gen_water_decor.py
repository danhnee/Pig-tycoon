"""Water depth / ripple decals for ponds (AE-0002: water must not read as a flat plastic sheet).

Dithered pixel-art blobs, no alpha blur: water_deep_* darken the middle of a pond in 2-3 ordered-dither steps, water_ripple_*
are short light ripple arcs with a warm sun glint. Output: Assets/Art/PixelMyth/Decor/water_*.png (32 px = 1 m)."""
from pathlib import Path
import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parents[2] / "Assets" / "Art" / "PixelMyth" / "Decor"
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0
DEEP = [(0x2a, 0x5f, 0x68), (0x25, 0x55, 0x5f), (0x21, 0x4c, 0x56)]   # one-two steps below the water mid tones


def deep(w_m, h_m, seed):
    w, h = int(w_m * 32), int(h_m * 32)
    rng = np.random.default_rng(seed)
    ys, xs = np.mgrid[0:h, 0:w]
    u, v = (xs + 0.5) / w * 2 - 1, (ys + 0.5) / h * 2 - 1
    wob = 0.10 * np.sin(u * 5 + rng.uniform(0, 6)) + 0.08 * np.sin(v * 7 + rng.uniform(0, 6))
    d = 1 - np.sqrt(u ** 2 + v ** 2) + wob          # 1 centre .. 0 rim
    img = np.zeros((h, w, 4), np.uint8)
    thr = BAYER[ys % 4, xs % 4]
    for k, (lo, col) in enumerate(((0.05, DEEP[0]), (0.30, DEEP[1]), (0.55, DEEP[2]))):
        m = (d - lo) * 2.2 > thr
        img[m, :3] = col
        img[m, 3] = 255
    return Image.fromarray(img, "RGBA")


def ripple(seed):
    rng = np.random.default_rng(seed)
    w, h = 40, 12
    img = np.zeros((h, w, 4), np.uint8)
    light, glint = (0x6f, 0xa9, 0xa3), (0xf2, 0xdc, 0x9a)
    for arc in range(int(rng.integers(2, 4))):
        cx, cy, r = rng.uniform(10, 30), rng.uniform(4, 9), rng.uniform(5, 12)
        for x in range(w):
            dx = (x - cx) / r
            if abs(dx) < 1:
                y = int(round(cy - (1 - dx * dx) * 2.2))
                if 0 <= y < h and rng.random() < 0.85:
                    img[y, x, :3], img[y, x, 3] = light, 255
    gx, gy = int(rng.integers(8, 32)), int(rng.integers(2, 9))
    img[gy, gx, :3], img[gy, gx, 3] = glint, 255
    img[gy, gx + 1, :3], img[gy, gx + 1, 3] = glint, 255
    return Image.fromarray(img, "RGBA")


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    for i, (w, h) in enumerate(((9, 4.5), (6, 3.2), (4, 2.2))):
        deep(w, h, 40 + i).save(OUT / f"water_deep_{'abc'[i]}.png")
    for i in range(3):
        ripple(70 + i).save(OUT / f"water_ripple_{'abc'[i]}.png")
    print("wrote water_deep_a..c, water_ripple_a..c ->", OUT)
