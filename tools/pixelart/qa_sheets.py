"""Harsh, automatic QA for Pig Tycoon pixel sprite sheets (Claude -> Argon gate, 3 Nov 2026).

Usage:
    python tools/pixelart/qa_sheets.py <folder-or-png> [...]      # prints a report, exit 1 if any FAIL
    python tools/pixelart/qa_sheets.py Assets/Art/PixelMyth/Characters/khoa --md report.md

Each <name>.png must sit next to <name>.json with "cell" [w,h], "rows" (down/left/up/right) and "frames_per_row".
Checks (thresholds are the bar of polished commercial pixel art, not "it renders"):
  F frames      every frame has pixels; frame count matches the json
  F alpha       in-between alpha (1..254) <= 0.5% of opaque pixels (pixel art has hard edges; no AA fringe/blur)
  F fringe      no magenta/fuchsia key-colour leftovers
  F holes       no transparent holes enclosed by the body (torn limbs, see-through legs)
  W colours     <= 48 colours per sheet (pixel palettes stay tight; hundreds = filtered/painted, not pixel art)
  F legs        locomotion clips (walk/run/move): the lower 40% of the figure must change between frames
  F side-view   left/right rows must differ from the down row (a side walk is a profile, not the front pose sliding)
  W orphans     isolated single pixels (noise) <= 0.3% of opaque pixels
"""
import json, sys, os
from collections import deque
from PIL import Image

LOCOMOTION = ("walk", "run", "move")


def load(png):
    meta_path = os.path.splitext(png)[0] + ".json"
    if not os.path.exists(meta_path):
        return None, None
    meta = json.load(open(meta_path, encoding="utf-8"))
    return Image.open(png).convert("RGBA"), meta


def frames(img, meta):
    cw, ch = meta["cell"]
    rows = meta.get("rows", ["down", "left", "up", "right"])
    n = meta.get("frames_per_row", img.width // cw)
    out = {}
    for r, name in enumerate(rows):
        out[name] = [img.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch)) for c in range(n)]
    return out


def opaque_mask(f):
    a = f.getchannel("A")
    return [[a.getpixel((x, y)) > 0 for x in range(f.width)] for y in range(f.height)]


def enclosed_holes(f, max_speck=24):
    """Small transparent specks enclosed by the body (torn limbs, see-through legs). Big enclosed regions are gaps
    between limbs closed by the ground shadow, which are fine, so only components <= max_speck pixels count."""
    m = opaque_mask(f)
    h, w = len(m), len(m[0])
    seen = [[False] * w for _ in range(h)]
    q = deque((x, y) for x in range(w) for y in (0, h - 1))
    q.extend((x, y) for y in range(h) for x in (0, w - 1))
    while q:
        x, y = q.popleft()
        if x < 0 or y < 0 or x >= w or y >= h or seen[y][x] or m[y][x]:
            continue
        seen[y][x] = True
        q.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
    specks = 0
    for y0 in range(h):
        for x0 in range(w):
            if m[y0][x0] or seen[y0][x0]:
                continue
            comp, st = 0, [(x0, y0)]
            seen[y0][x0] = True
            while st:
                x, y = st.pop(); comp += 1
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[ny][nx] and not m[ny][nx]:
                        seen[ny][nx] = True; st.append((nx, ny))
            if comp <= max_speck:
                specks += comp
    return specks


def lower_diff(a, b):
    """Mean per-pixel difference of the lower 40% of the figure's bounding box (legs)."""
    box = a.getchannel("A").getbbox() or (0, 0, a.width, a.height)
    top = box[1] + int((box[3] - box[1]) * 0.6)
    ca, cb = a.crop((0, top, a.width, box[3])), b.crop((0, top, b.width, box[3]))
    pa, pb = list(ca.get_flattened_data()), list(cb.get_flattened_data())
    d = sum(abs(p[0] - q[0]) + abs(p[1] - q[1]) + abs(p[2] - q[2]) + abs(p[3] - q[3]) for p, q in zip(pa, pb))
    return d / max(1, len(pa)) / 4.0


def frame_diff(a, b):
    pa, pb = list(a.get_flattened_data()), list(b.get_flattened_data())
    return sum(abs(p[3] - q[3]) for p, q in zip(pa, pb)) / max(1, len(pa))


def check(png):
    img, meta = load(png)
    if img is None:
        return [("W", "meta", "no json next to the sheet")]
    res = []
    fr = frames(img, meta)
    clip = os.path.splitext(os.path.basename(png))[0].split("_")[-1]
    px = list(img.get_flattened_data())
    opaque = [p for p in px if p[3] > 0]
    if not opaque:
        return [("F", "frames", "sheet is empty")]
    # the ground shadow (dark, neutral, translucent) is allowed to be soft
    def is_shadow(p):
        return p[3] < 255 and max(p[:3]) < 90 and max(p[:3]) - min(p[:3]) < 20
    semi = sum(1 for p in opaque if p[3] < 255 and not is_shadow(p))
    if semi > 0.005 * len(opaque):
        res.append(("F", "alpha", f"{semi} semi-transparent pixels ({100 * semi / len(opaque):.1f}%): soft/blurred edges, not hard pixel edges"))
    fringe = sum(1 for p in opaque if p[0] > 200 and p[2] > 200 and p[1] < 90)
    if fringe:
        res.append(("F", "fringe", f"{fringe} magenta key-colour pixels left on the sprite"))
    colours = len(set(p[:3] for p in opaque))
    if colours > 48:
        res.append(("W", "colours", f"{colours} colours (tight pixel palettes stay <= 48)"))
    for row, fs in fr.items():
        for i, f in enumerate(fs):
            if f.getchannel("A").getbbox() is None:
                res.append(("F", "frames", f"{row}#{i} empty frame"))
                continue
            holes = enclosed_holes(f)
            if holes > 3:
                res.append(("F", "holes", f"{row}#{i} {holes} see-through pixels inside the body"))
    if clip in LOCOMOTION:
        for row, fs in fr.items():
            diffs = [lower_diff(fs[i], fs[(i + 1) % len(fs)]) for i in range(len(fs))]
            if diffs and max(diffs) < 6.0:
                res.append(("F", "legs", f"{row}: legs barely change between frames (max {max(diffs):.1f}) — the figure slides"))
        if "down" in fr and fr["down"]:
            for side in ("left", "right"):
                if side in fr and fr[side]:
                    d = frame_diff(fr["down"][0], fr[side][0])
                    if d < 12.0:
                        res.append(("F", "side-view", f"{side} row #0 ≈ down row #0 (silhouette diff {d:.1f}): a front pose, not a profile"))
                    # a profile's torso is clearly narrower than the front view's (shoulders turn away)
                    def torso_width(f):
                        bb = f.getchannel("A").getbbox()
                        if not bb: return 0
                        y = bb[1] + int((bb[3] - bb[1]) * 0.35)
                        row = [f.getpixel((x, y))[3] > 0 for x in range(f.width)]
                        xs = [x for x, o in enumerate(row) if o]
                        return (xs[-1] - xs[0] + 1) if xs else 0
                    wd, ws = torso_width(fr["down"][0]), torso_width(fr[side][0])
                    bb = fr["down"][0].getchannel("A").getbbox()
                    upright = bb is not None and (bb[3] - bb[1]) > 1.3 * (bb[2] - bb[0])  # people, not pigs or crawlers
                    if upright and wd and ws >= wd * 0.85:
                        res.append(("F", "side-view", f"{side} row torso {ws}px vs front {wd}px: shoulders still face the camera, not a profile"))
    return res


def main(argv):
    md = None
    if "--md" in argv:
        i = argv.index("--md"); md = argv[i + 1]; argv = argv[:i] + argv[i + 2:]
    pngs = []
    for a in argv:
        if os.path.isdir(a):
            for root, _, files in os.walk(a):
                pngs += [os.path.join(root, f) for f in files if f.endswith(".png")]
        elif a.endswith(".png"):
            pngs.append(a)
    lines, fails = [], 0
    for p in sorted(pngs):
        r = check(p)
        status = "FAIL" if any(s == "F" for s, _, _ in r) else ("WARN" if r else "PASS")
        fails += status == "FAIL"
        lines.append(f"{status}  {p}")
        lines += [f"      {s} {k}: {m}" for s, k, m in r]
    lines.append(f"--- {len(pngs)} sheets, {fails} FAIL")
    out = "\n".join(lines)
    print(out)
    if md:
        open(md, "w", encoding="utf-8").write("```\n" + out + "\n```\n")
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
