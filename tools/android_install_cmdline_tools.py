"""Install Google's Windows SDK command-line tools with repository checksum verification.

Does not accept SDK licenses or alter Unity installations.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import tempfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

BASE = "https://dl.google.com/android/repository/"


def install(sdk: Path) -> dict:
    destination = sdk.resolve() / "cmdline-tools" / "latest"
    if destination.exists():
        raise FileExistsError(f"Refusing to overwrite existing tools: {destination}")
    with urllib.request.urlopen(BASE + "repository2-1.xml", timeout=60) as response:
        document = ET.fromstring(response.read())
    package = next(p for p in document if p.tag.endswith("remotePackage")
                   and p.attrib.get("path") == "cmdline-tools;latest")
    archive = next(a for a in package.findall("archives/archive")
                   if a.findtext("host-os") == "windows")
    complete = archive.find("complete")
    if complete is None:
        raise ValueError("Google repository has no complete Windows archive")
    url = complete.findtext("url")
    checksum = complete.findtext("checksum")
    expected_size = int(complete.findtext("size", "0"))
    if not url or "/" in url or not checksum or expected_size <= 0:
        raise ValueError("Invalid Google repository archive metadata")
    with tempfile.TemporaryDirectory(prefix="pigtycoon-android-tools-") as directory:
        temp = Path(directory)
        download = temp / url
        with urllib.request.urlopen(BASE + url, timeout=120) as response:
            with download.open("wb") as output:
                shutil.copyfileobj(response, output, 1024 * 1024)
        actual = hashlib.sha1(download.read_bytes()).hexdigest()
        if download.stat().st_size != expected_size or actual != checksum.lower():
            raise ValueError("Android command-line tools size/checksum verification failed")
        extracted = temp / "extracted"
        with zipfile.ZipFile(download) as bundle:
            for member in bundle.infolist():
                target = (extracted / member.filename).resolve()
                if not target.is_relative_to(extracted.resolve()):
                    raise ValueError("Unsafe archive member")
            bundle.extractall(extracted)
        source = extracted / "cmdline-tools"
        if not (source / "bin" / "sdkmanager.bat").is_file():
            raise ValueError("Downloaded archive lacks sdkmanager.bat")
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(source), str(destination))
    return {"url": BASE + url, "sha1": actual, "bytes": expected_size,
            "installed": str(destination), "licenses_accepted": False}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sdk", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    result = install(args.sdk)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2))
