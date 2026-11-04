"""Remove sprite clips that are pixel-identical copies of another clip of the same character (3 Nov 2026).

Argon's deliveries filled the clip list with copies: in khoa/ 150 sheets held only 37 distinct images (30 different
skills shared one sheet). A copy pretends to be a weapon- or skill-specific animation while showing the generic one,
and costs memory and import time. The runtime already falls back from specific to generic clips
(Core/AnimationClips: skill_<id>_<class> → skill_<id> → shape_* → attack_<class>_<n> → attack_<class> → attack),
so deleting the specific copies shows the same or the honest generic clip and the manifest shows what is still missing.

Rule (per character, decided on the BODY sheet folder <char>/):
  - a group of identical skill_* clips that has no generic member is a fake "unique skill animation": every member is
    deleted from the body AND from all of that character's overlay folders (<char>__*), so body and gear layers fall
    back to the same generic clip together (CharacterGearLayers plays the body's ShownClip, then its chain);
  - other identical pairs (death/faint, hit/hit_light, attack/attack_slash_1) are left alone: deleting one of them in
    one folder only could put a gear layer on a different clip than the body.

  python tools/pixelart/dedupe_clips.py Assets/Art/PixelMyth/Characters            # dry run
  python tools/pixelart/dedupe_clips.py Assets/Art/PixelMyth/Characters --apply
"""
import hashlib
import os
import re
import sys
from collections import defaultdict

from PIL import Image


def generic_rank(clip: str) -> tuple:
    """Lower = more generic = kept."""
    if clip.startswith("skill_"):
        return (5, clip.count("_"), clip)
    if clip.startswith("shape_"):
        return (4, clip.count("_"), clip)
    if re.fullmatch(r"attack_[a-z]+_\d", clip):
        return (2, 0, clip)
    if clip.startswith("attack"):
        return (1, clip.count("_"), clip)
    return (0, clip.count("_"), clip)


def scan(folder: str):
    family = os.path.basename(folder.rstrip("/\\"))
    groups = defaultdict(list)
    for name in sorted(os.listdir(folder)):
        if not name.endswith(".png") or not name.startswith(family + "_"):
            continue
        clip = name[len(family) + 1:-4]
        with Image.open(os.path.join(folder, name)) as im:
            digest = hashlib.md5(im.convert("RGBA").tobytes()).hexdigest()
        groups[digest].append(clip)
    return family, [g for g in groups.values() if len(g) > 1]


def main(argv):
    apply = "--apply" in argv
    roots = [a for a in argv if not a.startswith("--")]
    removed = 0
    for root in roots:
        dirs = sorted(d for d in os.listdir(root) if os.path.isdir(os.path.join(root, d)))
        bodies = [d for d in dirs if "__" not in d]
        for body in bodies:
            family, dups = scan(os.path.join(root, body))
            fake = sorted({c for g in dups if all(c.startswith("skill_") for c in g) for c in g})
            if not fake:
                continue
            targets = [body] + [d for d in dirs if d.startswith(body + "__")]
            n = 0
            for folder in targets:
                for clip in fake:
                    for ext in (".png", ".png.meta", ".json", ".json.meta"):
                        path = os.path.join(root, folder, f"{folder}_{clip}{ext}")
                        if os.path.exists(path):
                            n += ext == ".png"
                            if apply:
                                os.remove(path)
            removed += n
            print(f"{body}: {len(fake)} shared skill clips are copies -> {n} sheets in {len(targets)} folders: {', '.join(fake[:8])}{' …' if len(fake) > 8 else ''}")
    print(f"--- {removed} copied skill sheets {'deleted' if apply else 'would be deleted (dry run)'}")


if __name__ == "__main__":
    main(sys.argv[1:])
