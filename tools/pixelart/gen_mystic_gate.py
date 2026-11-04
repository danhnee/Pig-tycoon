"""Placeholder sprite for the occult-event gate until Argon's art (task A1) lands.

Story/user brief: a gate hastily patched together where the event strikes — two leaning weathered timber posts, a
crooked lintel lashed with rope, rag strips, paper talismans and small bells, stones piled at the feet, faint warm
mist in the opening. 96x112 px (3 m x 3.5 m at 32 PPU). Writes a PixelMyth character-style sheet so the library
builder picks it up as family "mystic_gate": clips idle (1 frame) and active (4-frame mist loop), 4 identical rows.
"""
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Art" / "PixelMyth" / "Characters" / "mystic_gate"
W, H = 96, 112
INK = (43, 27, 18)
WOOD = [(74, 50, 32), (98, 68, 42), (124, 88, 54), (150, 112, 70)]
ROPE = [(150, 118, 70), (196, 160, 100)]
RAG = [(140, 58, 40), (176, 128, 60), (96, 92, 70)]
PAPER = (232, 214, 170)
SEAL = (170, 40, 30)
GOLD = [(176, 128, 40), (232, 190, 80)]
STONE = [(78, 72, 62), (110, 102, 88), (140, 132, 112)]
MOSS = (96, 112, 50)
MIST = [(250, 230, 170), (236, 206, 140), (214, 176, 112)]
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0


def put(img, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        img[y, x, :3] = c
        img[y, x, 3] = 255


def post(img, x_base, y_base, height, lean, width, rng):
    """Weathered timber post leaning by `lean` px over its height; outline + grain + knots."""
    for t in range(height):
        y = y_base - t
        xo = x_base + int(round(lean * t / height))
        for k in range(width):
            shade = WOOD[1] if k < width // 3 else (WOOD[2] if k < 2 * width // 3 else WOOD[3] if k == width - 2 else WOOD[2])
            if (t * 7 + k * 3) % 11 == 0:
                shade = WOOD[0]                    # grain
            put(img, xo + k, y, shade)
        put(img, xo - 1, y, INK)
        put(img, xo + width, y, INK)
    for _ in range(3):                             # knots / cracks
        t = int(rng.integers(10, height - 10))
        xo = x_base + int(round(lean * t / height)) + int(rng.integers(1, width - 1))
        put(img, xo, y_base - t, WOOD[0]); put(img, xo, y_base - t - 1, INK)
    top_x = x_base + int(round(lean))
    for k in range(-1, width + 1):                 # broken, splintered top
        put(img, top_x + k, y_base - height - (k % 3 == 0), INK)
    return top_x


def frame(mist_phase, rng_seed=7):
    rng = np.random.default_rng(rng_seed)
    img = np.zeros((H, W, 4), np.uint8)
    base = H - 6
    # mist in the opening first (behind the posts)
    if mist_phase is not None:
        cx, cy = W / 2, base - 40
        ys, xs = np.mgrid[0:H, 0:W]
        a = np.arctan2(ys - cy, (xs - cx) * 1.3)
        r = np.hypot((xs - cx) * 1.3, ys - cy)
        swirl = 0.5 + 0.5 * np.sin(a * 3 + r * 0.28 - mist_phase * math.pi / 2)
        inten = np.clip(1 - r / 34.0, 0, 1) * (0.55 + 0.45 * swirl)
        thr = BAYER[ys % 4, xs % 4]
        inside = (xs > 22) & (xs < W - 22) & (ys > base - 78) & (ys < base - 2)
        for lvl, col in ((0.18, MIST[2]), (0.42, MIST[1]), (0.70, MIST[0])):
            m = inside & (inten - lvl > thr * 0.35)
            img[m, :3] = col
            img[m, 3] = 255
    # stones piled at the feet
    for sx, sy, r in ((14, base, 6), (24, base + 1, 4), (70, base, 6), (82, base + 1, 5), (8, base + 2, 3), (88, base + 2, 3)):
        for dy in range(-r, r + 1):
            for dx in range(-r - 2, r + 3):
                if (dx / (r + 2)) ** 2 + (dy / r) ** 2 <= 1:
                    c = STONE[2] if dy < -r // 3 and dx < 0 else (STONE[1] if dy < r // 3 else STONE[0])
                    put(img, sx + dx, sy + dy // 2, c)
        put(img, sx - 1, sy - r // 2, MOSS); put(img, sx, sy - r // 2, MOSS)
    # two leaning posts
    lt = post(img, 18, base - 2, 88, 5, 8, rng)
    rt = post(img, 70, base - 2, 82, -4, 8, rng)
    # crooked lintel from left top to right top, overhanging
    y0, y1 = base - 2 - 88 + 6, base - 2 - 82 + 10
    for x in range(lt - 8, rt + 16):
        t = (x - (lt - 8)) / max(1, (rt + 16) - (lt - 8))
        y = int(round(y0 + (y1 - y0) * t))
        for k in range(6):
            put(img, x, y + k, WOOD[2] if k < 2 else WOOD[1] if k < 5 else WOOD[0])
        put(img, x, y - 1, INK); put(img, x, y + 6, INK)
    # a second, shorter cross plank nailed askew (the patch)
    for x in range(lt + 2, lt + 30):
        y = base - 70 + (x - lt) // 5
        for k in range(4):
            put(img, x, y + k, WOOD[3] if k == 0 else WOOD[2])
        put(img, x, y - 1, INK); put(img, x, y + 4, INK)
    # rope lashings at both joints
    for jx, jy in ((lt + 3, y0 + 3), (rt + 3, y1 + 3), (lt + 4, base - 70)):
        for k in range(-5, 6):
            put(img, jx + k // 2, jy + k, ROPE[(k + 5) % 2])
            put(img, jx + k // 2 + 1, jy + k, ROPE[1])
    # rag strips, talismans, bells hanging from the lintel
    for i, hx in enumerate(range(lt + 4, rt + 10, 7)):
        t = (hx - (lt - 8)) / max(1, (rt + 16) - (lt - 8))
        hy = int(round(y0 + (y1 - y0) * t)) + 6
        kind = i % 3
        if kind == 0:                               # rag strip
            col = RAG[(i // 3) % 3]
            for k in range(int(rng.integers(10, 18))):
                put(img, hx + (k // 5) % 2, hy + k, col)
                put(img, hx + 1 + (k // 5) % 2, hy + k, col)
        elif kind == 1:                             # paper talisman
            for k in range(2):
                put(img, hx, hy + k, ROPE[0])
            for yy in range(hy + 2, hy + 12):
                for xx in range(hx - 1, hx + 3):
                    put(img, xx, yy, PAPER)
            put(img, hx, hy + 5, SEAL); put(img, hx + 1, hy + 6, SEAL); put(img, hx, hy + 8, SEAL)
        else:                                       # small bell
            for k in range(3):
                put(img, hx, hy + k, ROPE[0])
            for yy, xs_ in ((hy + 3, (0, 1)), (hy + 4, (-1, 0, 1, 2)), (hy + 5, (-1, 0, 1, 2))):
                for dx in xs_:
                    put(img, hx + dx, yy, GOLD[1] if dx <= 0 else GOLD[0])
    return img


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    rows = ["down", "left", "up", "right"]
    idle = frame(None)
    active = [frame(p) for p in range(4)]
    for clip, frames, fps in (("idle", [frame(0)], 1.0), ("active", active, 6.0)):
        sheet = np.zeros((H * 4, W * len(frames), 4), np.uint8)
        for r in range(4):
            for i, f in enumerate(frames):
                sheet[r * H:(r + 1) * H, i * W:(i + 1) * W] = f
        Image.fromarray(sheet, "RGBA").save(OUT / f"mystic_gate_{clip}.png")
        (OUT / f"mystic_gate_{clip}.json").write_text(json.dumps({
            "source": "tools/pixelart/gen_mystic_gate.py (placeholder until Argon task A1)",
            "cell": [W, H], "rows": rows, "frames_per_row": len(frames),
            "pivot_px_from_bottom_left": [W // 2, 5], "fps": fps, "colors": 32}, indent=2))
    Image.fromarray(active[0]).resize((W * 4, H * 4), Image.NEAREST).save(ROOT / ".agent_scratch" / "review5" / "mystic_gate_x4.png")
    print("wrote", OUT)


if __name__ == "__main__":
    main()
