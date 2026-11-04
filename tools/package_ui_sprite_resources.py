"""Package existing production Art PNGs as lazy Resources TextAssets for Android.

No sprites are redrawn, recolored or reimported as candidate player art. Runtime
decodes exact original PNG bytes through UISpriteLoader, preserving its pivots/PPU.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets/Resources/PigTycoonArt"
MANIFEST = ROOT / "Docs/plans/2026-10-30-ui-sprite-package.json"
NS = uuid.UUID("5017d341-d6e2-4596-8d38-561f727446d9")


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path: Path) -> str:
    return uuid.uuid5(NS, path.relative_to(ROOT).as_posix()).hex


def meta(path: Path, folder: bool = False) -> None:
    tail = "folderAsset: yes\nDefaultImporter:" if folder else "TextScriptImporter:"
    path.with_name(path.name + ".meta").write_text(
        f"fileFormatVersion: 2\nguid: {guid(path)}\n{tail}\n  externalObjects: {{}}\n  userData: Original PNG byte package for UISpriteLoader\n  assetBundleName:\n  assetBundleVariant:\n", encoding="utf-8")


def sources() -> list[Path]:
    return sorted(p for p in (ROOT / "Assets/Art").rglob("*.png") if "PixelRework" not in p.relative_to(ROOT / "Assets/Art").parts)


def verify() -> dict:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    catalog = json.loads((OUTPUT / "art-index.json").read_text(encoding="utf-8"))
    if catalog["paths"] != [str(p.relative_to(ROOT / "Assets").as_posix()) for p in sources()]:
        raise ValueError("Resource PNG catalog is stale relative to production Art; rerun packaging")
    if len(manifest["files"]) != len(catalog["paths"]):
        raise ValueError("Manifest/catalog length differs")
    for row in manifest["files"]:
        source, package = ROOT / row["source"], ROOT / row["package"]
        if sha(source) != row["sha256"] or sha(package) != row["sha256"]:
            raise ValueError(f"PNG source/package hash mismatch: {source}")
        if not package.with_suffix(package.suffix + ".meta").exists():
            raise ValueError(f"Missing TextAsset metadata: {package}")
    return manifest


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify", action="store_true")
    args = parser.parse_args()
    if not args.verify:
        OUTPUT.mkdir(parents=True, exist_ok=True)
        previous = json.loads(MANIFEST.read_text(encoding="utf-8"))["files"] if MANIFEST.exists() else []
        entries = []
        for source in sources():
            key = source.relative_to(ROOT / "Assets")
            package = (OUTPUT / key).with_suffix(".bytes")
            package.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, package)
            meta(package)
            entries.append({"source": source.relative_to(ROOT).as_posix(), "package": package.relative_to(ROOT).as_posix(), "sha256": sha(source), "bytes": source.stat().st_size})
        retained = {r["package"] for r in entries}
        for row in previous:
            if row["package"] in retained:
                continue
            package = (ROOT / row["package"]).resolve()
            if not package.is_relative_to(OUTPUT.resolve()) or package.suffix != ".bytes":
                raise ValueError(f"Refuse to remove non-package path: {package}")
            for p in [package, package.with_suffix(".bytes.meta")]:
                if p.exists():
                    p.unlink()
        catalog = OUTPUT / "art-index.json"
        catalog.write_text(json.dumps({"paths": [r["source"][7:] for r in entries]}, ensure_ascii=False, indent=2), encoding="utf-8")
        meta(catalog)
        for folder in [OUTPUT, *sorted(p for p in OUTPUT.rglob("*") if p.is_dir())]:
            meta(folder, folder=True)
        MANIFEST.parent.mkdir(parents=True, exist_ok=True)
        MANIFEST.write_text(json.dumps({"purpose": "Exact PNG bytes lazily loaded from Unity Resources in built players, including Android APK; no developer Assets folder required", "excludes": "PixelRework candidate art, which has not been visually approved", "files": entries}, ensure_ascii=False, indent=2), encoding="utf-8")
    manifest = verify()
    print(f"PASS: {len(manifest['files'])} existing PNG byte Resources verified, {sum(r['bytes'] for r in manifest['files'])} bytes; candidate art excluded")


if __name__ == "__main__":
    main()
