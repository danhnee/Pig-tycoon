"""Standalone validator for Zone04 (Farmstead) and Zone06 (NPC Village) closed-manifold 3D geometry.

Verifies `closed_outward_components == True`, `failures == []`, finite coordinates,
UV bounds in [0, 1], non-degenerate triangles, and `.meshbin` binary serialization.
"""
import json
import sys
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'Tools'))

from canonical_map_helpers import validate_closed_parts
from observatory_geometry import meshbin_bytes
from village_buildings_geometry import build_zone04_farmstead, build_zone06_npc_village


def validate_zone(name: str, geom) -> dict:
    closed_report = validate_closed_parts(geom)
    v_arr, uv_arr, f_arr = geom.arrays()

    assert np.isfinite(v_arr).all(), f"{name}: non-finite vertices"
    assert np.isfinite(uv_arr).all(), f"{name}: non-finite UVs"
    assert f_arr.min() >= 0 and f_arr.max() < len(v_arr), f"{name}: face index out of bounds"
    assert ((uv_arr >= 0.0) & (uv_arr <= 1.0)).all(), f"{name}: UVs outside [0, 1]"

    tri = v_arr[f_arr]
    normals = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    areas = np.linalg.norm(normals, axis=1)
    min_area = float(areas.min())
    assert min_area > 1e-9, f"{name}: degenerate triangles detected (min_area={min_area})"

    raw_bytes = meshbin_bytes(v_arr, uv_arr, f_arr)
    assert len(raw_bytes) > 8, f"{name}: meshbin serialization failed"

    assert closed_report['closed_outward_components'] is True, (
        f"{name}: failed closed outward manifold check: {closed_report['failures']}"
    )
    assert closed_report['failures'] == [], f"{name}: non-empty failures: {closed_report['failures']}"

    return {
        'zone': name,
        'parts': closed_report['parts'],
        'vertices': closed_report['vertices'],
        'triangles': closed_report['triangles'],
        'closed_outward_components': closed_report['closed_outward_components'],
        'failures': closed_report['failures'],
        'min_double_triangle_area': min_area,
        'bounds_min': v_arr.min(axis=0).tolist(),
        'bounds_max': v_arr.max(axis=0).tolist(),
        'meshbin_bytes': len(raw_bytes),
    }


def main():
    z04 = build_zone04_farmstead()
    r04 = validate_zone('Zone04_Building_KhuChuongTrai_Farmstead', z04)

    z06 = build_zone06_npc_village()
    r06 = validate_zone('Zone06_Building_NhaCuongHoa_And_KhuNPC', z06)

    summary = {'Zone04': r04, 'Zone06': r06}
    print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    main()
