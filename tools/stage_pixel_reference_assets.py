"""Stage existing local raster sheets as review candidates; never modify their sources.

Run from the repository root: python tools/stage_pixel_reference_assets.py [--verify]
Candidate sprites retain source dimensions and alpha, point filtering and 32 PPU.
This is a reusable asset library, not evidence of art approval or runtime binding.
"""
from __future__ import annotations

import argparse
import colorsys
import hashlib
import json
import re
import shutil
import uuid
from collections import Counter
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r"F:\StardewValley\Content (unpacked)")
OUTPUT = ROOT / "Assets/Art/PixelRework"
EVIDENCE = ROOT / "Docs/art-direction/2d-reuse-evidence"
SOURCE_COPIES = ROOT / "Docs/art-direction/2d-reuse-source-sheets"
REFERENCE_MAPS = ROOT / "Docs/art-direction/2d-reuse-source-maps"
NAMESPACE = uuid.UUID("ef42c51b-39f1-4624-b6d2-225e38115888")

# category, source relative path, frame size (None means a sheet kept whole), palette
ASSETS = [
    ("terrain", "Maps/spring_outdoorsTileSheet.png", (16, 16), "verdigris"),
    ("terrain", "Maps/fall_outdoorsTileSheet.png", (16, 16), "amber"),
    ("terrain", "Maps/spring_outdoorsTileSheet2.png", (16, 16), "verdigris"),
    ("terrain", "Maps/spring_outdoorTileSheet_extra.png", (16, 16), "verdigris"),
    ("town", "Maps/spring_town.png", (16, 16), "stone"),
    ("harbor", "Maps/spring_beach.png", (16, 16), "verdigris"),
    ("harbor", "Maps/boatTunnelTiles.png", (16, 16), "stone"),
    ("harbor", "Maps/night_market_tilesheet_objects.png", None, "stone"),
    ("interior", "Maps/townInterior.png", (16, 16), "stone"),
    ("interior", "Maps/farmhouse_tiles.png", (16, 16), "stone"),
    ("interior", "Maps/walls_and_floors.png", (16, 16), "stone"),
    ("terrain", "Maps/paths.png", (16, 16), "stone"),
    ("forest", "Maps/witchSwampTiles.png", (16, 16), "verdigris"),
    ("forest", "Maps/WitchHutTiles.png", (16, 16), "verdigris"),
    ("forest", "Maps/cave.png", (16, 16), "stone"),
    ("forest", "TerrainFeatures/tree1_spring.png", None, "verdigris"),
    ("forest", "TerrainFeatures/tree2_spring.png", None, "verdigris"),
    ("forest", "TerrainFeatures/mystic_tree.png", None, "verdigris"),
    ("terrain", "TerrainFeatures/hoeDirt.png", (16, 16), "stone"),
    ("terrain", "TerrainFeatures/Flooring.png", (16, 16), "stone"),
    ("buildings", "Buildings/Barn.png", None, "stone"),
    ("buildings", "Buildings/Big Barn.png", None, "stone"),
    ("buildings", "Buildings/Deluxe Barn.png", None, "stone"),
    ("buildings", "Buildings/Shed.png", None, "stone"),
    ("buildings", "Buildings/Big Shed.png", None, "stone"),
    ("buildings", "Buildings/Mill.png", None, "stone"),
    ("buildings", "Buildings/Silo.png", None, "stone"),
    ("buildings", "Buildings/Well.png", None, "stone"),
    ("buildings", "Buildings/Stable.png", None, "stone"),
    ("buildings", "Buildings/Earth Obelisk.png", None, "stone"),
    ("buildings", "Buildings/Water Obelisk.png", None, "stone"),
    ("buildings", "Buildings/Gold Clock.png", None, "stone"),
    ("pigs", "Animals/Pig.png", (32, 32), "unchanged"),
    ("pigs", "Animals/BabyPig.png", (32, 32), "unchanged"),
    ("pets", "Animals/dog.png", (32, 32), "unchanged"),
    ("pets", "Animals/cat.png", (32, 32), "unchanged"),
    ("npc", "Characters/Robin.png", (16, 32), "unchanged"),
    ("npc", "Characters/Marlon.png", (16, 32), "unchanged"),
    ("npc", "Characters/Wizard.png", (16, 32), "unchanged"),
    ("npc", "Characters/Dwarf.png", (16, 24), "unchanged"),
    ("monsters", "Characters/Monsters/Skeleton.png", (16, 32), "unchanged"),
    ("monsters", "Characters/Monsters/Mummy.png", (16, 32), "unchanged"),
    ("monsters", "Characters/Monsters/Shadow Brute.png", (16, 32), "unchanged"),
    ("monsters", "Characters/Monsters/Bat.png", (16, 16), "unchanged"),
    ("monsters", "Characters/Monsters/Ghost.png", (16, 24), "unchanged"),
    ("items", "Maps/springobjects.png", (16, 16), "unchanged"),
    ("items", "TileSheets/Objects_2.png", (16, 16), "unchanged"),
    ("equipment", "TileSheets/weapons.png", (16, 16), "unchanged"),
    ("equipment", "TileSheets/tools.png", (16, 16), "unchanged"),
    ("equipment", "Characters/Farmer/hats.png", (20, 20), "unchanged"),
    ("equipment", "Characters/Farmer/shirts.png", None, "unchanged"),
    ("equipment", "Characters/Farmer/pants.png", None, "unchanged"),
    ("craftables", "TileSheets/Craftables.png", (16, 32), "stone"),
    ("furniture", "TileSheets/furniture.png", (16, 16), "stone"),
    ("furniture", "TileSheets/wizard_furniture.png", (16, 16), "stone"),
    ("effects", "TileSheets/Projectiles.png", (16, 16), "unchanged"),
    ("effects", "TileSheets/emotes.png", (16, 16), "unchanged"),
    ("effects", "TileSheets/rain.png", (16, 16), "unchanged"),
]
MAPS = ["Farm", "Forest", "Town", "FarmHouse", "Mountain", "Beach"]


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stable_id(name: str) -> str:
    return uuid.uuid5(NAMESPACE, name).hex


def palette_color(rgb: tuple[int, int, int], mode: str) -> tuple[int, int, int]:
    """Material palette study: preserve pixel geometry and value hierarchy.

    Green foliage shifts toward jade; warm building/earth tones lose saturation.
    This does not claim to redesign silhouettes or remove stock signs/technology.
    """
    if mode == "unchanged":
        return rgb
    h, s, v = colorsys.rgb_to_hsv(*(c / 255 for c in rgb))
    if mode == "verdigris" and 0.16 <= h <= 0.47 and s > 0.17:
        h = 0.34 + (h - 0.29) * 0.35
        s *= 0.78
    elif mode == "amber" and 0.025 <= h <= 0.18 and s > 0.15:
        h = 0.08 + (h - 0.08) * 0.8
        s *= 0.68
    elif mode == "stone" and s > 0.13:
        if 0.025 <= h <= 0.17:
            s *= 0.68  # retain warm aged masonry and wood, avoid uniform gray
        elif h >= 0.91 or h <= 0.025:
            h = 0.965 + (h - 0.965) * 0.45 if h >= 0.91 else h * 0.45
            s *= 0.73
        elif 0.18 <= h <= 0.47:
            h = 0.36 + (h - 0.3) * 0.3
            s *= 0.78
    return tuple(round(c * 255) for c in colorsys.hsv_to_rgb(h % 1, s, v))


def adapt(image: Image.Image, mode: str) -> Image.Image:
    result = image.convert("RGBA")
    lut = {rgb: palette_color(rgb, mode) for rgb in set(result.convert("RGB").get_flattened_data())}
    result.putdata([(*lut[(r, g, b)], a) for r, g, b, a in result.get_flattened_data()])
    return result


def texture_meta(path: Path, frame: tuple[int, int] | None, size: tuple[int, int], sprite: bool) -> None:
    rel = path.relative_to(ROOT).as_posix()
    w, h = size
    mode = 2 if frame and sprite else 1 if sprite else 0
    if frame and (w % frame[0] or h % frame[1]):
        raise ValueError(f"Sheet {rel} size {size} does not fit exact frame {frame}")
    header = f"""fileFormatVersion: 2
guid: {stable_id(rel)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureFormat: 1
  maxTextureSize: 8192
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  compressionQuality: 100
  spriteMode: {mode}
  spriteExtrude: 0
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: {8 if sprite else 0}
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 8192
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    overridden: 0
  spriteSheet:
    serializedVersion: 2
"""
    entries, ids = [], []
    if frame and sprite:
        fw, fh = frame
        for row in range(h // fh):
            for col in range(w // fw):
                name = f"{path.stem.replace(' ', '_')}_r{row:03d}_c{col:03d}"
                sid = stable_id(rel + ":" + name)
                file_id = 21300000 + (row * (w // fw) + col) * 2
                entries.append(f"""    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {col * fw}
        y: {h - (row + 1) * fh}
        width: {fw}
        height: {fh}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {sid}
      internalID: {file_id}
      vertices: []
      indices:
      edges: []
      weights: []
""")
                ids.append(f"      {name}: {file_id}")
    body = "    sprites:\n" + "".join(entries) if entries else "    sprites: []\n"
    tail = "    outline: []\n    physicsShape: []\n    bones: []\n    secondaryTextures: []\n"
    tail += "    nameFileIdTable:\n" + "\n".join(ids) + "\n" if ids else "    nameFileIdTable: {}\n"
    tail += "  userData: PixelRework review candidate; not art approved\n  assetBundleName:\n  assetBundleVariant:\n"
    path.with_suffix(path.suffix + ".meta").write_text(header + body + tail, encoding="utf-8")


def folder_metas() -> None:
    for folder in [OUTPUT, *sorted(p for p in OUTPUT.rglob("*") if p.is_dir())]:
        guid = stable_id(folder.relative_to(ROOT).as_posix())
        folder.with_suffix(folder.suffix + ".meta").write_text(
            f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n", encoding="utf-8")
    manifest = OUTPUT / "reuse-manifest.json"
    manifest.with_suffix(".json.meta").write_text(
        f"fileFormatVersion: 2\nguid: {stable_id(manifest.relative_to(ROOT).as_posix())}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n", encoding="utf-8")


def comparison(entries: list[dict]) -> None:
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    selected = ["Maps/spring_outdoorsTileSheet.png", "Maps/spring_town.png", "Buildings/Well.png", "Animals/Pig.png"]
    board = Image.new("RGB", (1300, 1120), "#1d2530")
    draw = ImageDraw.Draw(board)
    for index, rel in enumerate(selected):
        row = next(x for x in entries if x["source_relative"] == rel)
        y = index * 280
        draw.text((12, y + 5), f"{rel} | source (left), candidate (right) | palette study; NOT APPROVED", fill="white")
        for col, key in enumerate(["source_copy", "candidate"]):
            im = Image.open(ROOT / row[key]).convert("RGBA")
            # Documented crops only for evidence layout, never alter source sheets.
            im = im.crop((0, 0, min(320, im.width), min(125, im.height)))
            im = im.resize((im.width * 2, im.height * 2), Image.Resampling.NEAREST)
            board.paste(im, (col * 650 + 12, y + 28), im)
    board.save(EVIDENCE / "source-candidate-comparison.png")


def verify(manifest: dict) -> None:
    for row in manifest["assets"]:
        source, copy, candidate = SOURCE / row["source_relative"], ROOT / row["source_copy"], ROOT / row["candidate"]
        if sha(source) != row["source_sha256"] or sha(copy) != row["source_sha256"]:
            raise ValueError(f"Source changed: {source}")
        if sha(candidate) != row["candidate_sha256"]:
            raise ValueError(f"Candidate changed: {candidate}")
        with Image.open(source) as original, Image.open(candidate) as result:
            if original.size != result.size or original.convert("RGBA").getchannel("A").tobytes() != result.convert("RGBA").getchannel("A").tobytes():
                raise ValueError(f"Dimensions or alpha changed: {candidate}")
        meta = candidate.with_suffix(candidate.suffix + ".meta").read_text(encoding="utf-8")
        for expected in ["filterMode: 0", "spritePixelsToUnits: 32", "enableMipMap: 0", "textureCompression: 0"]:
            if expected not in meta:
                raise ValueError(f"Importer missing {expected}: {candidate}")
        if row["frame"]:
            names = re.findall(r"^      name: (.+)$", meta, re.MULTILINE)
            if len(names) != row["frame_count"] or len(set(names)) != len(names):
                raise ValueError(f"Sprite grid names/count invalid: {candidate}")
    for row in manifest["reference_maps"]:
        if sha(SOURCE / row["source_relative"]) != row["sha256"] or sha(ROOT / row["copy"]) != row["sha256"]:
            raise ValueError(f"Reference map changed: {row}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify", action="store_true")
    args = parser.parse_args()
    manifest_path = OUTPUT / "reuse-manifest.json"
    if args.verify:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        verify(manifest)
        print(f"PASS: {len(manifest['assets'])} sheets; source hashes, alpha/dimensions, candidate hashes and pixel importer settings verified")
        return
    # Validate every required source/frame before writing any candidate.
    for _, rel, frame, _ in ASSETS:
        with Image.open(SOURCE / rel) as image:
            if frame and (image.width % frame[0] or image.height % frame[1]):
                raise ValueError(f"Source {rel} {image.size} incompatible with {frame}")
    entries, maps = [], []
    for category, rel, frame, palette in ASSETS:
        source = SOURCE / rel
        original_copy, candidate = SOURCE_COPIES / rel, OUTPUT / "Candidates" / rel
        original_copy.parent.mkdir(parents=True, exist_ok=True)
        candidate.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, original_copy)
        with Image.open(source) as image:
            size = image.size
            result = adapt(image, palette)
            result.save(candidate)
        texture_meta(candidate, frame, size, True)
        entries.append({"category": category, "source_relative": rel, "source_sha256": sha(source), "source_copy": original_copy.relative_to(ROOT).as_posix(), "candidate": candidate.relative_to(ROOT).as_posix(), "candidate_sha256": sha(candidate), "size": list(size), "frame": list(frame) if frame else None, "frame_count": (size[0] // frame[0]) * (size[1] // frame[1]) if frame else 1, "frame_order": "top-left row-major; names r000_c000; Unity rect y inverted", "palette": palette, "status": "STAGED_NOT_VISUALLY_APPROVED_NOT_RUNTIME_BOUND"})
    for name in MAPS:
        rel = f"Maps/{name}.tmx"
        dest = REFERENCE_MAPS / f"{name}.tmx.txt"
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(SOURCE / rel, dest)
        maps.append({"source_relative": rel, "copy": dest.relative_to(ROOT).as_posix(), "sha256": sha(dest), "status": "layout reference only, not a Unity scene"})
    manifest = {"source_root": str(SOURCE), "status": "REVIEW_CANDIDATES_NOT_APPROVED", "policy": "Original F drive files are never edited; user requested existing asset reuse. Provenance retained; no release or license conclusion is made by this script.", "ppu": 32, "grid_note": "Native16px tiles occupy0.5 Unity units at32PPU; use explicit scale2 for1m tile placement. Source sheet geometry/alpha preserved.", "assets": entries, "reference_maps": maps, "category_counts": dict(Counter(row["category"] for row in entries))}
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    folder_metas()
    comparison(entries)
    verify(manifest)
    print(f"PASS: staged and verified {len(entries)} existing sheets, {len(maps)} reference maps; {sum(x['frame_count'] for x in entries)} exact grid rectangles")


if __name__ == "__main__":
    main()
