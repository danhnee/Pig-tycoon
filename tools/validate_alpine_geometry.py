"""Validate closed outward-wound manifold topology, finite coordinates, UV bounds, and determinism for Zone10 (`Tools/alpine_geometry.py`)."""
import json
import sys
from pathlib import Path
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from alpine_geometry import build_alpine_zone


def validate():
    model = build_alpine_zone()
    v, uv, f = model.arrays()
    v2, uv2, f2 = build_alpine_zone().arrays()
    assert np.array_equal(v, v2) and np.array_equal(uv, uv2) and np.array_equal(f, f2), "Nondeterministic geometry"
    assert np.isfinite(v).all() and np.isfinite(uv).all(), "Non-finite coordinates or UVs"
    assert ((uv >= 0.0) & (uv <= 1.0)).all(), "UVs outside [0, 1]"

    failures = []
    for part in model.parts:
        start = part["first_triangle"]
        end = start + part["triangles"]
        triangles = v[f[start:end]].astype(np.float64)
        center = triangles.reshape(-1, 3).mean(axis=0)
        q = triangles - center
        volume = np.einsum("ij,ij->i", q[:, 0], np.cross(q[:, 1], q[:, 2])).sum() / 6.0
        _, indices = np.unique(np.round(triangles.reshape(-1, 3), 5), axis=0, return_inverse=True)
        ids = indices.reshape(-1, 3)
        edges = np.concatenate([ids[:, [0, 1]], ids[:, [1, 2]], ids[:, [2, 0]]])
        directed = {}
        for a, b in edges:
            key = (int(min(a, b)), int(max(a, b)))
            count, winding = directed.get(key, (0, 0))
            directed[key] = (count + 1, winding + (1 if a < b else -1))
        bad_edges = sum(count != 2 or winding != 0 for count, winding in directed.values())
        if volume <= 1e-10 or bad_edges:
            failures.append({"part": part["name"], "volume": float(volume), "bad_edges": int(bad_edges)})

    report = {
        "parts": len(model.parts),
        "vertices": int(len(v)),
        "triangles": int(len(f)),
        "bounds_min": [round(float(x), 4) for x in v.min(axis=0)],
        "bounds_max": [round(float(x), 4) for x in v.max(axis=0)],
        "closed_outward_components": not failures,
        "failures": failures[:10],
    }
    print(json.dumps(report, indent=2))
    assert not failures, f"Component topology/winding failed on {len(failures)} parts"
    return report


if __name__ == "__main__":
    validate()
