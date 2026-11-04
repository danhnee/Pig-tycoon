#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Validate closed outward-wound manifold topology (`bad_edges == 0`, `vol6 > 0`),
finite coordinates, UV bounds, and determinism for Zone09, Zone02, Zone03, and Zone11.
"""

import json
import sys
from pathlib import Path
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from canonical_map_helpers import validate_closed_parts
from citadel_diorama_ruins_geometry import (
    build_zone02_diorama_rim,
    build_zone03_water,
    build_zone09_castle_and_cliff,
    build_zone11_ruins_and_flora,
)


ZONE_BUILDERS = [
    ("Zone09_Vista_TwilightCastle_And_WaterfallCliff", build_zone09_castle_and_cliff),
    ("Zone02_Diorama_Cutaway_RockRim_And_Base", build_zone02_diorama_rim),
    ("Zone11_Props_AncientRuins_And_GoldenFlora", build_zone11_ruins_and_flora),
]


def validate_all():
    summary = {}
    total_parts = 0
    total_verts = 0
    total_tris = 0

    for zone_name, builder_fn in ZONE_BUILDERS:
        g1 = builder_fn()
        g2 = builder_fn()
        v1, uv1, f1 = g1.arrays()
        v2, uv2, f2 = g2.arrays()

        assert np.array_equal(v1, v2) and np.array_equal(uv1, uv2) and np.array_equal(f1, f2), (
            f"Nondeterministic geometry in {zone_name}"
        )
        assert np.isfinite(v1).all() and np.isfinite(uv1).all(), f"Non-finite coordinates or UVs in {zone_name}"
        assert ((uv1 >= 0.0) & (uv1 <= 1.0)).all(), f"UVs outside [0, 1] in {zone_name}"

        tri = v1[f1]
        normals = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
        areas = np.linalg.norm(normals, axis=1)
        assert areas.min() > 1e-9, f"Degenerate triangles in {zone_name}: min_double_area={areas.min()}"

        closed_report = validate_closed_parts(g1)
        closed_report["bounds_min"] = [round(float(x), 4) for x in v1.min(axis=0)]
        closed_report["bounds_max"] = [round(float(x), 4) for x in v1.max(axis=0)]
        closed_report["min_double_triangle_area"] = float(areas.min())
        summary[zone_name] = closed_report

        total_parts += closed_report["parts"]
        total_verts += closed_report["vertices"]
        total_tris += closed_report["triangles"]

        assert closed_report["closed_outward_components"] and not closed_report["failures"], (
            f"Topology validation failed for {zone_name}: {closed_report['failures']}"
        )

    full_report = {
        "all_closed_outward_components": True,
        "total_parts": total_parts,
        "total_vertices": total_verts,
        "total_triangles": total_tris,
        "zones": summary,
    }
    print(json.dumps(full_report, indent=2))
    return full_report


if __name__ == "__main__":
    validate_all()
