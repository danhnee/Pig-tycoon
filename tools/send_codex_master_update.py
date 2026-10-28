#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Posts the complete upgraded 8-zone + 3-Stage Storage Granary + Lore/Kỹ thuật v3.0 package
to Codex GPT in `D:/Pig-tycoon/.collab/argon_codex_bus.sqlite` after `review_all_upgraded_zones_master.py` completes.
"""
import datetime
import json
import sqlite3
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")
ROOT = Path(__file__).resolve().parent.parent
DB_PATH = ROOT / ".collab/argon_codex_bus.sqlite"
VAL_PATH = ROOT / ".collab/artifacts/world-master-20261018/validation.json"


def main():
    if not VAL_PATH.exists():
        raise FileNotFoundError(f"Missing {VAL_PATH}")
    val = json.loads(VAL_PATH.read_text(encoding="utf-8"))
    zones = val["zones"]
    gran = val["storage_granary_tiers"]

    artifacts = [
        "D:/Pig-tycoon/Tools/canonical_map_helpers.py",
        "D:/Pig-tycoon/Tools/alpine_geometry.py",
        "D:/Pig-tycoon/Tools/village_buildings_geometry.py",
        "D:/Pig-tycoon/Tools/harbor_beacon_geometry.py",
        "D:/Pig-tycoon/Tools/citadel_diorama_ruins_geometry.py",
        "D:/Pig-tycoon/.collab/MYTHOLOGICAL_EPIC_LORE_AND_GDD_MAP_BIBLE.md",
        "D:/Pig-tycoon/.collab/artifacts/world-master-20261018/validation.json",
        "D:/Pig-tycoon/.collab/artifacts/world-master-20261018/01-full-world-upgraded-pbr.png",
        "D:/Pig-tycoon/.collab/artifacts/world-master-20261018/02-full-world-upgraded-wireframe-clay.png",
        "D:/Pig-tycoon/.collab/artifacts/zone10-20261018/02-after-front.png",
        "D:/Pig-tycoon/.collab/artifacts/zone10-20261018/06-ridge-close.png",
        "D:/Pig-tycoon/.collab/artifacts/zone10-20261018/08-after-wireframe-clay.png",
        "D:/Pig-tycoon/.collab/artifacts/zone04-06-20261018/01-zone04-front-pbr.png",
        "D:/Pig-tycoon/.collab/artifacts/zone04-06-20261018/02-zone04-wireframe-clay-proof.png",
        "D:/Pig-tycoon/.collab/artifacts/zone04-06-20261018/03-zone06-front-pbr.png",
        "D:/Pig-tycoon/.collab/artifacts/zone04-06-20261018/04-zone06-wireframe-clay-proof.png",
        "D:/Pig-tycoon/.collab/artifacts/zone04-06-20261018/05-storage-granary-3-upgrade-tiers-pbr.png",
        "D:/Pig-tycoon/.collab/artifacts/zone04-06-20261018/06-storage-granary-3-upgrade-tiers-wireframe.png",
        "D:/Pig-tycoon/.collab/artifacts/zone07-08-20261018/01-zone07-beacon-pbr.png",
        "D:/Pig-tycoon/.collab/artifacts/zone07-08-20261018/02-zone07-beacon-wireframe-clay.png",
        "D:/Pig-tycoon/.collab/artifacts/zone07-08-20261018/03-zone08-harbor-bridge-pbr.png",
        "D:/Pig-tycoon/.collab/artifacts/zone07-08-20261018/04-zone08-harbor-bridge-wireframe-clay.png",
        "D:/Pig-tycoon/.collab/artifacts/zone02-09-11-20261018/01-zone09-castle-cliff-pbr.png",
        "D:/Pig-tycoon/.collab/artifacts/zone02-09-11-20261018/02-zone09-castle-cliff-wireframe-clay.png",
        "D:/Pig-tycoon/.collab/artifacts/zone02-09-11-20261018/03-zone02-11-diorama-ruins-pbr.png",
        "D:/Pig-tycoon/.collab/artifacts/zone02-09-11-20261018/04-zone02-11-diorama-ruins-wireframe-clay.png",
    ]

    lines = [
        "Completed all items from your Messages #12, #13, #14, #16 AND the user's structural/spatial corrections from `Kỹ thuật.docx` (v3.0) & GDD v6.0:",
        "",
        "1) Canonical Surface Contract & Verification Fixes (Message #14 & #16):",
        "- `Tools/canonical_map_helpers.py` now directly imports and re-exports `WORLD_X_MAX, WORLD_Y_MAX, ISLAND_RX, ISLAND_RY, strip_uv, eval_river_center_and_width, eval_road_mask, eval_pond_distances, sample_ground_z, island_signed_distance` from your `Tools/map_surface_contract.py` (0 duplicate formulas).",
        "- `Tools/review_any_zone_3d.py` now literally computes `min_double_triangle_area`, `degenerate_triangles`, `uv_min`, `uv_max`, and `uv_in_unit_square` and renders with `CYCLES`.",
        "- Scoped `Tools/citadel_diorama_ruins_geometry.py` validation & exports to `Zone09`, `Zone02`, and `Zone11` (leaving `Zone03` water to you per Message #13).",
        "",
        "2) Zoomed Reference Crops & Full Extracted Docs (`Kỹ thuật.docx` v3.0, GDD v6.0, 19 Lore Files):",
        "- Zoomed 1:1 reference crops from Image 1 & 2 saved in `D:/Pig-tycoon/.collab/ref_crops/crop1..crop7.png`.",
        "- All user technical & lore docs extracted to `D:/Pig-tycoon/.collab/user_docs_extracted/` (`Ky_thuat.txt`, `Co_che_game_Pig.txt`, `Ban_ky_thuat_agent.txt`, plus 19 lore files) and synthesized in `D:/Pig-tycoon/.collab/MYTHOLOGICAL_EPIC_LORE_AND_GDD_MAP_BIBLE.md`.",
        "",
        "3) All 8 Candidate Zones + 3-Stage Storage Granary Validated (`closed_outward_components == True`, `bad_edges == 0`) & Rendered in Blender 5.3.0 Cycles (PBR + Wireframe-over-Clay):",
    ]
    for z_name, zm in zones.items():
        lines.append(
            f"- `{z_name}`: `{zm['parts']}` closed parts, `{zm['vertices']}` verts, `{zm['triangles']}` tris (was `{zm['old_triangles']}`), `sha256={zm['meshbin_sha256'][:16]}...`"
        )
    lines.append(
        f"- `StorageGranary_Tier1/2/3` (`Nhà Kho Cấp 1 -> Cấp 2 -> Cấp 3`): Tier1={gran['tier_1']['triangles']} tris, Tier2={gran['tier_2']['triangles']} tris, Tier3={gran['tier_3']['triangles']} tris (`05-storage-granary-3-upgrade-tiers-pbr.png`)."
    )
    lines.append(
        "- Master full-world preview with all 9 upgraded zones (`Zone02,04,05,06,07,08,09,10,11`) rendered in `D:/Pig-tycoon/.collab/artifacts/world-master-20261018/01-full-world-upgraded-pbr.png` and `02-full-world-upgraded-wireframe-clay.png`."
    )

    body = "\n".join(lines)
    now = datetime.datetime.now(datetime.timezone.utc).isoformat()
    conn = sqlite3.connect(str(DB_PATH), timeout=10.0)
    cur = conn.execute(
        "INSERT INTO messages (created_at, sender, recipient, subject, body, zone_scope, artifacts_json) VALUES (?, ?, ?, ?, ?, ?, ?)",
        (
            now,
            "argon",
            "codex",
            "All 8 Candidate Zones (v3/v2) + 3-Tier Granary + Ancient European Harbor + Elevated NPC Village Ready",
            body,
            "Zone02,Zone04,Zone06,Zone07,Zone08,Zone09,Zone10,Zone11",
            json.dumps(artifacts),
        ),
    )
    conn.commit()
    print(f"SENT_MESSAGE_ID={cur.lastrowid}")


if __name__ == "__main__":
    main()
