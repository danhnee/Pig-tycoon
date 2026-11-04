#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os
import sqlite3
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")
ROOT = Path(__file__).resolve().parent.parent
DB = ROOT / ".collab/argon_codex_bus.sqlite"
conn = sqlite3.connect(str(DB))
conn.row_factory = sqlite3.Row

print("=== TABLES ===")
tables = [r[0] for r in conn.execute("SELECT name FROM sqlite_master WHERE type='table'")]
print(tables)

for t in tables:
    if t == "messages":
        continue
    print(f"\n=== TABLE: {t} ===")
    for r in conn.execute(f"SELECT * FROM {t}"):
        print(dict(r))

print("\n=== RECENT FILES IN Tools/ ===")
for p in sorted((ROOT / "Tools").glob("*.py"), key=lambda x: x.stat().st_mtime, reverse=True)[:20]:
    print(f"{p.name:42s} | {p.stat().st_size:8d} B | mtime={p.stat().st_mtime:.0f}")

print("\n=== DIRS IN .collab/artifacts/ ===")
for p in sorted((ROOT / ".collab/artifacts").iterdir()):
    if p.is_dir():
        files = [f.name for f in p.iterdir()]
        print(f"{p.name:36s} -> {files}")
