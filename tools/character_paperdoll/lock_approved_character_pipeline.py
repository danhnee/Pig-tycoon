# -*- coding: utf-8 -*-
"""
Locks the approved character sheets into `.collab/claude-2d/argon-deliverables/LOCKED/<char>/`
with `sha256.txt` per Claude Collab #151 & #153, and moves legacy character-overwrite scripts
out of `tools/` into `tools/_retired_legacy_character_scripts/` so no script can ever regress
approved character assets.
"""
import hashlib
import os
import shutil

ROOT = "d:/Pig-tycoon"
CHARS_DIR = os.path.join(ROOT, "Assets/Art/PixelMyth/Characters")
LOCKED_ROOT = os.path.join(ROOT, ".collab/claude-2d/argon-deliverables/LOCKED")
RETIRED_DIR = os.path.join(ROOT, "tools/_retired_legacy_character_scripts")

LEGACY_SCRIPTS_TO_RETIRE = [
    "build_56px_natural_human_khoa_and_gear.py",
    "build_56px_natural_human_npcs.py",
    "build_khoa_anime_anatomy_candidate.py",
    "build_khoa_character_rework.py",
    "build_khoa_motion_rework.py",
]

LOCKED_CHARACTERS = [
    "khoa",
    "an",
    "ba_sau", "man", "tam_va", "dung", "cuc", "le", "anh_ca", "ong_ton", "anh_tu",
    "bi_nhan", "ba_ban_rau", "nguoi_ao_mua", "nguoi_ban_dem", "nguoi_ban_tin",
]


def sha256_file(path: str) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def lock_characters():
    os.makedirs(LOCKED_ROOT, exist_ok=True)
    locked_count = 0
    for char_name in LOCKED_CHARACTERS:
        src_dir = os.path.join(CHARS_DIR, char_name)
        if not os.path.isdir(src_dir):
            continue
        dst_dir = os.path.join(LOCKED_ROOT, char_name)
        os.makedirs(dst_dir, exist_ok=True)
        manifest_lines = []
        for fn in sorted(os.listdir(src_dir)):
            if not (fn.endswith(".png") or fn.endswith(".json")):
                continue
            sp = os.path.join(src_dir, fn)
            dp = os.path.join(dst_dir, fn)
            shutil.copy2(sp, dp)
            if fn.endswith(".png"):
                manifest_lines.append(f"{sha256_file(dp)}  {fn}")
                locked_count += 1
        with open(os.path.join(dst_dir, "sha256.txt"), "w", encoding="utf-8") as f:
            f.write("\n".join(manifest_lines) + "\n")

    os.makedirs(RETIRED_DIR, exist_ok=True)
    retired = []
    for script in LEGACY_SCRIPTS_TO_RETIRE:
        sp = os.path.join(ROOT, "tools", script)
        if os.path.isfile(sp):
            dp = os.path.join(RETIRED_DIR, script)
            shutil.move(sp, dp)
            retired.append(script)

    print(f"Locked {locked_count} PNG sheets across {len(LOCKED_CHARACTERS)} character folders into {LOCKED_ROOT}")
    print(f"Retired {len(retired)} legacy character scripts into {RETIRED_DIR}: {retired}")


if __name__ == "__main__":
    lock_characters()
