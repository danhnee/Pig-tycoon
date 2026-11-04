#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Validate closed outward-wound manifold topology, finite coordinates, UV bounds, and determinism for Zone07 and Zone08 (`Tools/harbor_beacon_geometry.py`)."""
import json
import sys
from pathlib import Path
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from canonical_map_helpers import validate_closed_parts
from harbor_beacon_geometry import build_zone07_beacon, build_zone08_harbor_and_bridge


def _validate_one(name: str, builder_fn) -> dict:
    geom = builder_fn()
    v, uv, f = geom.arrays()
    v2, uv2, f2 = builder_fn().arrays()
    assert np.array_equal(v, v2) and np.array_equal(uv, uv2) and np.array_equal(f, f2), f"Nondeterministic geometry in {name}"
    assert np.isfinite(v).all() and np.isfinite(uv).all(), f"Non-finite coordinates or UVs in {name}"
    assert ((uv >= 0.0) & (uv <= 1.0)).all(), f"UVs outside [0, 1] in {name}"

    closed_report = validate_closed_parts(geom)
    closed_report["zone"] = name
    closed_report["bounds_min"] = [round(float(x), 4) for x in v.min(axis=0)]
    closed_report["bounds_max"] = [round(float(x), 4) for x in v.max(axis=0)]
    return closed_report


def validate() -> dict:
    r07 = _validate_one("Zone07_Landmark_TramThongTin_InfoBeacon", build_zone07_beacon)
    r08 = _validate_one("Zone08_Vista_StoneBridge_And_HarborTown", build_zone08_harbor_and_bridge)
    report = {
        "zone07": r07,
        "zone08": r08,
        "closed_outward_components": bool(r07["closed_outward_components"] and r08["closed_outward_components"]),
        "failures": r07["failures"] + r08["failures"],
    }
    print(json.dumps(report, indent=2))
    assert report["closed_outward_components"], f"Topology/winding failed: {report['failures']}"
    return report


if __name__ == "__main__":
    validate()
