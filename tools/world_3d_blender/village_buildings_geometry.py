"""High-detail, closed-manifold 3D geometry generator for Zone04 (Farm_Core) and Zone06 (Enhancement Sanctum & Elevated Hub_Village).

Strictly implements the architectural and spatial specifications from `Kỹ thuật.docx` (v3.0) and GDD v6.0:
- Zone04 (`Zone04_Building_KhuChuongTrai_Farmstead`):
  1. `Nhà Chính & Đài Điều Khiển Bạch Vân` (`build_main_barn_farmhouse()`) at (-17.8, 5.4):
     Two-story manor homestead with front timber veranda porch, brick feed-fermentation stove (`Lò Ủ Cám xây bằng gạch sát cửa sổ`),
     and rooftop `Bảng Điều Khiển Bạch Vân` (octagonal brass-and-crystal command relay array).
  2. `Nhà Kho` (`Storage Granary`) with 3 Distinct Structural Upgrade Tiers (`Cấp 1 -> Cấp 2 -> Cấp 3`):
     - `build_storage_granary_tier1()`: Raised timber storehouse on 6 stone staddle-pillars with exterior wood X-bracing & side grain bin.
     - `build_storage_granary_tier2()`: Fortified stone-and-timber granary with stone buttresses, 1 cylindrical grain silo, and overhead chute.
     - `build_storage_granary_tier3()`: Grand twin-silo vaulted logistics stronghold (`silo_west` + `silo_east`), overhead covered conveyor bridge,
       central clerestory monitor roof, cantilevered cargo hoist crane, and glowing golden preservation rune seals.
  3. `Phòng Xây Dựng` (`build_construction_guild_hall()`) at (-13.4, 9.4):
     High-bay slate-blue engineering workshop + open-sided timber-truss carpenter/siege bay housing the `Bàn Học Bản Vẽ` (Blueprint Table)
     and turret frame + giant freestanding A-Frame Timber & Iron Gantry Crane (`Cần Trục Chữ A`) + heavy stone forge furnace & twin smokestacks.

- Zone06 (`Zone06_Building_NhaCuongHoa_And_KhuNPC`):
  1. `Xưởng Cường Hóa & Khắc Phù Ấn` (`build_enhancement_rune_sanctum()`) at operational core slot (-1.8, 7.6):
     Mythological octagonal stone-and-bronze sanctum on a 2-tier stepped octagonal plinth with 4 freestanding carved Rune Monolith Pillars,
     2-tiered octagonal slate-and-bronze pagoda roof, levitating Soul-Seal crystal orb with intersecting brass armillary rings, and outdoor
     Arcane Workstations (`Bàn Gia Cường Giấy`, `Bàn Khắc Phù Văn`, `Bàn Khắc Mệnh Ấn`).
  2. `Đoạn Chuyển Map` (`Rear Boundary Wall, Stone Arch Gatehouse & Stepped Mountain Trail`) at y = 9.2..11.2.
  3. `Khu Làng NPC Ngoại Vi` (`Hub_Village`) elevated and pushed far back onto a Raised Stone Highland Terrace Bastion
     (`y in [11.2, 14.6], z_terrace = ground + 1.35m`), featuring:
     - `Nhà Bà Sáu & Quán Trà Đầu Làng` at (-1.3, 12.4)
     - `Hội Quán Làng & Tháp Chuông Quạ Tin` (with tall square stone bell-and-raven tower) at (1.1, 13.5)
     - `Nhà Ông Lẫm` at (3.2, 12.2) + `Giếng Đá Cổ & Bảng Tin Làng` at (0.8, 11.8)

Every component is built with `MapGeometry` from `canonical_map_helpers.py` and verified to be a 100%
water-tight, outward-wound 3D manifold solid (`bad_edges == 0`, `vol6 > 0`).
"""
import math
import numpy as np
from observatory_geometry import frame, unit, meshbin_bytes
from canonical_map_helpers import (
    MapGeometry,
    sample_ground_z,
    eval_road_mask,
    eval_river_center_and_width,
    island_signed_distance,
    validate_closed_parts,
)


# =============================================================================
# LOW-LEVEL CLOSED-MANIFOLD PRIMITIVES
# =============================================================================

def prism(g: MapGeometry, name: str, outline, depth: float, origin, material: str, yaw: float = 0.0):
    """Convex outline in local XZ, facade looking toward negative local Y."""
    c, s = math.cos(yaw), math.sin(yaw)
    verts = []
    for y in (-depth / 2.0, depth / 2.0):
        for x, z in outline:
            verts.append((origin[0] + c * x - s * y, origin[1] + s * x + c * y, origin[2] + z))
    n = len(outline)
    faces = [list(range(n)), list(range(n, 2 * n))]
    faces += [[i, (i + 1) % n, (i + 1) % n + n, i + n] for i in range(n)]
    g.add(name, verts, faces, material, np.mean(verts, axis=0))


def torus_ring(
    g: MapGeometry,
    name: str,
    center,
    normal_axis,
    major_radius: float,
    minor_radius: float,
    material: str,
    major_segments: int = 20,
    minor_segments: int = 8,
):
    """Twist-free closed manifold torus ring around `normal_axis`."""
    center = np.asarray(center, dtype=float)
    basis = frame(normal_axis)
    u_ax, v_ax, n_ax = basis[:, 0], basis[:, 1], basis[:, 2]
    verts = []
    for i in range(major_segments):
        theta = math.tau * i / major_segments
        radial = math.cos(theta) * u_ax + math.sin(theta) * v_ax
        ring_center = center + major_radius * radial
        for j in range(minor_segments):
            phi = math.tau * j / minor_segments
            pt = ring_center + minor_radius * (math.cos(phi) * radial + math.sin(phi) * n_ax)
            verts.append(pt)
    faces = []
    for i in range(major_segments):
        i_next = (i + 1) % major_segments
        for j in range(minor_segments):
            j_next = (j + 1) % minor_segments
            a = i * minor_segments + j
            b = i * minor_segments + j_next
            c = i_next * minor_segments + j_next
            d = i_next * minor_segments + j
            faces.append([a, d, c, b])
    g.add(name, verts, faces, material, convex_center=None)


def closed_ring_solid(
    g: MapGeometry,
    name: str,
    center,
    ring_profiles,
    sides: int,
    material: str,
    yaw: float = 0.0,
    lobe_amp: float = 0.0,
    lobe_freq: int = 3,
    phase: float = 0.0,
    scallop_ring_idx: int = -1,
    scallop_amp: float = 0.0,
    scallop_droop: float = 0.0,
):
    """Builds a 100% closed, outward-wound manifold polyhedron from ascending Z ring profiles."""
    cx, cy, cz = float(center[0]), float(center[1]), float(center[2])
    c_yaw, s_yaw = math.cos(yaw), math.sin(yaw)
    rings = len(ring_profiles)
    verts = []
    for r_idx, (z_off, rx, ry) in enumerate(ring_profiles):
        for i in range(sides):
            theta = math.tau * i / sides
            wob = 1.0 + lobe_amp * math.sin(theta * lobe_freq + phase + r_idx * 0.45)
            dz = 0.0
            if r_idx == scallop_ring_idx:
                wob *= 1.0 + scallop_amp * (1.0 if i % 2 == 0 else -0.65)
                dz = -scallop_droop if i % 2 == 0 else 0.0
            lx = rx * wob * math.cos(theta)
            ly = ry * wob * math.sin(theta)
            wx = cx + c_yaw * lx - s_yaw * ly
            wy = cy + s_yaw * lx + c_yaw * ly
            wz = cz + z_off + dz
            verts.append((wx, wy, wz))

    faces = [
        list(range(sides - 1, -1, -1)),
        list(range((rings - 1) * sides, rings * sides)),
    ]
    for r_idx in range(rings - 1):
        base = r_idx * sides
        for i in range(sides):
            j = (i + 1) % sides
            faces.append([base + i, base + j, base + sides + j, base + sides + i])
    g.add(name, verts, faces, material, convex_center=None)


# =============================================================================
# ARCHITECTURAL COMPONENTS (STONE COURSES, QUOINS, TILED ROOFS, ARCHES, PROPS)
# =============================================================================

def arch_outline(radius: float, spring: float):
    return [(-radius, 0.0), (radius, 0.0)] + [
        (radius * math.cos(t), spring + radius * math.sin(t))
        for t in np.linspace(0.0, math.pi, 13)
    ]


def arch_window(
    g: MapGeometry,
    name: str,
    origin,
    width: float = 0.38,
    height: float = 0.66,
    yaw: float = 0.0,
    door: bool = False,
    double_door: bool = False,
):
    """Arched window or doorway with stone jambs, 11 radial voussoirs, sill, and mullions/door hardware."""
    r = width / 2.0
    spring = max(0.14, height - r)
    c, s = math.cos(yaw), math.sin(yaw)
    rot_mat = np.array([[c, -s, 0.0], [s, c, 0.0], [0.0, 0.0, 1.0]])

    def pt(x, y, z):
        return (origin[0] + c * x - s * y, origin[1] + s * x + c * y, origin[2] + z)

    prism(g, name + '_recess', arch_outline(r + 0.035, spring), 0.045, pt(0.0, 0.0, 0.0), 'iron', yaw)
    prism(g, name + '_inset', arch_outline(r - 0.018, spring), 0.025, pt(0.0, -0.029, 0.015), 'door' if door else 'glass', yaw)

    for side in (-1, 1):
        count = max(2, int(spring / 0.15))
        for row in range(count):
            z = (row + 0.5) * spring / count
            g.box(
                name + '_jamb',
                pt(side * (r + 0.045), -0.008, z),
                (0.10, 0.10, spring / count - 0.008),
                'trim',
                0.01,
                rot_mat,
            )

    for i in range(11):
        lo = i * math.pi / 11.0 + 0.018
        hi = (i + 1) * math.pi / 11.0 - 0.018
        outline = [
            ((r + 0.10) * math.cos(lo), spring + (r + 0.10) * math.sin(lo)),
            ((r + 0.10) * math.cos(hi), spring + (r + 0.10) * math.sin(hi)),
            (r * math.cos(hi), spring + r * math.sin(hi)),
            (r * math.cos(lo), spring + r * math.sin(lo)),
        ]
        prism(g, name + '_voussoir', outline, 0.12, origin, 'trim', yaw)

    g.beam(name + '_sill', pt(-r - 0.10, -0.025, -0.025), pt(r + 0.10, -0.025, -0.025), 0.095, 'trim')

    if door:
        n_planks = 6 if double_door else 5
        for i in range(n_planks + 1):
            x = -r + 0.045 + i * (2.0 * r - 0.090) / n_planks
            top = spring + math.sqrt(max(0.0, (r - 0.025) ** 2 - x * x)) - 0.02
            w_plank = 0.030 if (double_door and i == n_planks // 2) else 0.024
            g.beam(name + '_plank_joint', pt(x, -0.048, 0.04), pt(x, -0.048, top), w_plank, 'wood')
        if double_door:
            g.beam(name + '_cross_batten_lo', pt(-r + 0.04, -0.052, 0.26), pt(r - 0.04, -0.052, 0.26), 0.028, 'wood')
            g.beam(name + '_cross_batten_hi', pt(-r + 0.04, -0.052, 0.56), pt(r - 0.04, -0.052, 0.56), 0.028, 'wood')
        for z in (0.22, 0.57):
            for side in (-1, 1):
                g.beam(name + '_hinge', pt(side * 0.04, -0.068, z), pt(side * (r - 0.03), -0.068, z), 0.028, 'brass')
        for side in (-1, 1):
            ring_center = pt(side * 0.085, -0.088, 0.44)
            ring_normal = (-s, c, 0.0)
            torus_ring(g, name + '_door_ring', ring_center, ring_normal, 0.046, 0.018, 'brass', major_segments=12, minor_segments=6)
    else:
        g.beam(name + '_mullion', pt(0.0, -0.05, 0.03), pt(0.0, -0.05, height - 0.03), 0.028, 'wood')
        for z in (spring * 0.34, spring * 0.72):
            g.beam(name + '_transom', pt(-r + 0.025, -0.05, z), pt(r - 0.025, -0.05, z), 0.028, 'wood')


def masonry_block(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    width: float,
    depth: float,
    eave: float = 1.44,
    stone_rows: int = 6,
    timber_braces: bool = True,
    base_z: float = 0.0,
):
    """Closed masonry & plaster block with individual beveled stone courses, corner quoins, and timber framing."""
    g.box(name + '_mortar', (cx, cy, base_z + eave * 0.43), (width, depth, eave * 1.06), 'mortar', 0.015)
    plaster_h = max(0.38, eave - 0.92)
    g.box(name + '_plaster', (cx, cy, base_z + eave - plaster_h * 0.5), (width + 0.006, depth + 0.006, plaster_h), 'plaster', 0.014)

    course_h = 0.175
    for row in range(stone_rows):
        z = base_z - 0.08 + (row + 0.5) * course_h
        for horizontal, length, offset in ((True, width, depth / 2.0), (False, depth, width / 2.0)):
            count = max(2, math.ceil(length / 0.32))
            edges = np.linspace(-length / 2.0, length / 2.0, count + 1)
            if row % 2 == 1:
                edges = np.unique(np.r_[-length / 2.0, edges[:-1] + length / count / 2.0, length / 2.0])
            for left, right in zip(edges[:-1], edges[1:]):
                span = float(right - left)
                if span < 0.045:
                    continue
                for side in (-1, 1):
                    center = (
                        (cx + (left + right) / 2.0, cy + side * offset, z)
                        if horizontal
                        else (cx + side * offset, cy + (left + right) / 2.0, z)
                    )
                    size = (
                        (span - 0.012, 0.065, course_h - 0.010)
                        if horizontal
                        else (0.065, span - 0.012, course_h - 0.010)
                    )
                    g.box(name + '_stone_course', center, size, 'stone', 0.011)

    for x in (-width / 2.0, width / 2.0):
        for y in (-depth / 2.0, depth / 2.0):
            g.box(name + '_corner_timber', (cx + x, cy + y, base_z + eave - plaster_h * 0.48), (0.10, 0.10, plaster_h), 'wood', 0.010)
            for row in range(4):
                g.box(name + '_quoin', (cx + x, cy + y, base_z + 0.02 + row * 0.24), (0.165, 0.165, 0.225), 'trim', 0.012)

    if timber_braces:
        z_lo = base_z + eave - plaster_h + 0.05
        z_hi = base_z + eave - 0.05
        for side_y in (-depth / 2.0 - 0.022, depth / 2.0 + 0.022):
            for side_x in (-1, 1):
                x0 = cx + side_x * (width * 0.46)
                x1 = cx + side_x * (width * 0.24)
                g.beam(name + '_wall_brace', (x0, cy + side_y, z_lo), (x1, cy + side_y, z_hi), 0.052, 'wood')

    for z in (base_z + eave - plaster_h, base_z + eave - 0.035):
        g.box(name + '_belt', (cx, cy, z), (width + 0.12, depth + 0.12, 0.092), 'wood', 0.010)
    g.box(name + '_plinth', (cx, cy, base_z - 0.14), (width + 0.14, depth + 0.14, 0.28), 'stone', 0.022)


def tiled_roof(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    width: float,
    depth: float,
    eave: float,
    rise: float,
    ridge_x: bool = True,
    roof_mat: str = 'terracotta',
    occluders=(),
):
    """Closed pitched roof with overlapping beveled tiles, bargeboards, fascia, ridge beam, and braced plaster gables."""
    def p(u, v, z):
        return np.array((cx + u, cy + v, z) if ridge_x else (cx + v, cy + u, z), dtype=float)

    length = width if ridge_x else depth
    run = (depth if ridge_x else width) / 2.0

    verts = [p(u, v, z) for u in (-length / 2.0, length / 2.0) for v, z in ((-run, eave), (0.0, eave + rise), (run, eave))]
    g.add(name + '_underlay', verts, [[0, 1, 2], [5, 4, 3], [0, 3, 4, 1], [1, 4, 5, 2], [2, 5, 3, 0]], roof_mat, np.mean(verts, axis=0))

    columns = max(3, math.ceil(length / 0.185))
    rows = max(6, math.ceil(math.hypot(run, rise) / 0.145))
    for side in (-1, 1):
        for row in range(rows):
            t = 1.0 - (row + 0.5) / rows
            z = eave + rise * (1.0 - t) + 0.034 * t * t
            slope = unit(p(0.0, side * run, -rise + 0.07 * t) - p(0.0, 0.0, 0.0))
            along = unit(p(1.0, 0.0, 0.0) - p(0.0, 0.0, 0.0))
            normal = unit(np.cross(along, slope))
            if normal[2] < 0.0:
                normal = -normal
            rotation = np.column_stack((along, slope, normal))
            edges = np.linspace(-length / 2.0, length / 2.0, columns + 1)
            if row % 2 == 1:
                edges = np.unique(np.r_[-length / 2.0, edges[:-1] + length / columns / 2.0, length / 2.0])
            for left, right in zip(edges[:-1], edges[1:]):
                span = float(right - left)
                if span < 0.035:
                    continue
                center = p((left + right) / 2.0, side * run * t, z) + normal * 0.023
                if any(test(center) for test in occluders):
                    continue
                g.box(
                    name + '_roof_tile',
                    center,
                    (span - 0.007, math.hypot(run, rise) / rows * 1.18, 0.038),
                    roof_mat,
                    0.006,
                    rotation,
                    top_bevel_only=True,
                )
        for u in (-length / 2.0, length / 2.0):
            g.beam(name + '_bargeboard', p(u, 0.0, eave + rise + 0.025), p(u, side * run, eave + 0.025), 0.078, 'wood')
        g.beam(name + '_fascia', p(-length / 2.0, side * run, eave), p(length / 2.0, side * run, eave), 0.082, 'wood')

    g.beam(name + '_ridge', p(-length / 2.0 - 0.025, 0.0, eave + rise + 0.035), p(length / 2.0 + 0.025, 0.0, eave + rise + 0.035), 0.088, 'wood')

    for side in (-1, 1):
        u = side * (length / 2.0 + 0.032)
        points = [p(u, -run * 0.87, eave - 0.03), p(u, run * 0.87, eave - 0.03), p(u, 0.0, eave + rise - 0.07)]
        axis = unit(p(1.0, 0.0, 0.0) - p(0.0, 0.0, 0.0)) * 0.024
        vv = [q + axis for q in points] + [q - axis for q in points]
        g.add(name + '_gable', vv, [[0, 1, 2], [5, 4, 3], [0, 3, 4, 1], [1, 4, 5, 2], [2, 5, 3, 0]], 'plaster', np.mean(vv, axis=0))
        for v in (-run * 0.82, run * 0.82):
            g.beam(name + '_gable_brace', p(u + side * 0.042, v, eave + 0.02), p(u + side * 0.042, 0.0, eave + rise * 0.70), 0.066, 'wood')
        g.beam(name + '_gable_kingpost', p(u, 0.0, eave), p(u, 0.0, eave + rise - 0.04), 0.068, 'wood')


def add_roof_dormer(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    base_z: float,
    width: float = 0.54,
    depth: float = 0.48,
    wall_h: float = 0.38,
    rise: float = 0.28,
    roof_mat: str = 'terracotta',
):
    """Front-facing roof dormer with plaster cheeks, corner timbers, tiled roof, and arched glowing window."""
    g.box(name + '_wall', (cx, cy, base_z + wall_h * 0.5), (width, depth, wall_h), 'plaster', 0.012)
    for sx in (-width / 2.0, width / 2.0):
        g.box(name + '_cheek_timber', (cx + sx, cy - depth * 0.46, base_z + wall_h * 0.5), (0.065, 0.065, wall_h), 'wood', 0.008)
    tiled_roof(g, name + '_roof', cx, cy, width + 0.14, depth + 0.14, base_z + wall_h - 0.02, rise, ridge_x=False, roof_mat=roof_mat)
    arch_window(g, name + '_window', (cx, cy - depth / 2.0 - 0.02, base_z + 0.04), width=0.22, height=0.34, yaw=0.0, door=False)


def add_hanging_lanterns(g: MapGeometry, name: str, cx: float, front_y: float, z_bracket: float, span_x: float = 0.46):
    """Pair of wrought-iron wall brackets and brass-framed glowing glass lanterns."""
    for side_idx, side in enumerate((-1, 1)):
        tag = f"{name}_{side_idx}"
        x = cx + side * span_x
        y = front_y - 0.14
        g.beam(tag + '_bracket', (x, front_y + 0.02, z_bracket), (x, y, z_bracket), 0.032, 'iron')
        g.beam(tag + '_brace', (x, front_y + 0.02, z_bracket - 0.09), (x, y + 0.04, z_bracket), 0.028, 'iron')
        g.box(tag + '_glass', (x, y, z_bracket - 0.14), (0.115, 0.115, 0.19), 'glass', 0.010)
        for xx in (-0.056, 0.056):
            for yy in (-0.056, 0.056):
                g.cylinder(tag + '_frame', (x + xx, y + yy, z_bracket - 0.24), (x + xx, y + yy, z_bracket - 0.04), 0.016, 'brass', segments=6)
        for z_cap in (z_bracket - 0.24, z_bracket - 0.04):
            g.box(tag + '_cap', (x, y, z_cap), (0.15, 0.15, 0.042), 'brass', 0.006)


def add_stone_chimney(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    base_z: float,
    courses: int = 6,
    course_mat: str = 'stone',
):
    """Stacked stone/terracotta chimney with trim cornice, soot recess, iron cage spikes, and 2 iron crown rings."""
    for row in range(courses):
        z = base_z + row * 0.135
        mat = 'terracotta' if (course_mat == 'brick' and row % 2 == 0) else ('stone' if course_mat == 'brick' else course_mat)
        g.box(f"{name}_course_{row}", (cx, cy, z), (0.29, 0.29, 0.126), mat, 0.012)
    top_z = base_z + courses * 0.135
    g.box(name + '_cap', (cx, cy, top_z), (0.38, 0.38, 0.08), 'trim', 0.012)
    g.box(name + '_soot', (cx, cy, top_z + 0.050), (0.24, 0.24, 0.036), 'iron', 0.005)
    for idx, angle in enumerate(np.linspace(0.0, math.tau, 6, endpoint=False)):
        p0 = (cx + 0.12 * math.cos(angle), cy + 0.12 * math.sin(angle), top_z + 0.04)
        p1 = (cx + 0.16 * math.cos(angle), cy + 0.16 * math.sin(angle), top_z + 0.31)
        g.cylinder(f"{name}_spike_{idx}", p0, p1, 0.018, 'iron', end_radius=0.014, segments=6)
    for r_i, z_ring in enumerate((top_z + 0.13, top_z + 0.23)):
        torus_ring(g, f"{name}_crown_ring_{r_i}", (cx, cy, z_ring), (0.0, 0.0, 1.0), 0.145, 0.022, 'iron', major_segments=12, minor_segments=6)


def add_entrance_steps(g: MapGeometry, name: str, cx: float, front_y: float, base_z: float = -0.02, width: float = 0.84, steps: int = 3):
    """Tiered beveled stone entrance steps."""
    for step in range(steps):
        g.box(
            f"{name}_step_{step}",
            (cx, front_y - 0.11 - step * 0.105, base_z - 0.08 - step * 0.045),
            (width + step * 0.08, 0.28, 0.12),
            'trim',
            0.015,
        )


def _add_grain_silo_tower(
    g: MapGeometry,
    tag: str,
    silo_cx: float,
    silo_cy: float,
    silo_r: float = 0.54,
    silo_h: float = 2.15,
    roof_mat: str = 'terracotta',
    with_rune_seals: bool = False,
):
    """Reusable high-detail cylindrical stone-and-timber grain silo tower with conical tiled cap."""
    g.cylinder(f'{tag}_plinth', (silo_cx, silo_cy, -0.22), (silo_cx, silo_cy, 0.08), silo_r + 0.08, 'stone', segments=28)
    g.cylinder(f'{tag}_core', (silo_cx, silo_cy, 0.05), (silo_cx, silo_cy, silo_h), silo_r, 'mortar', segments=32)

    for row in range(5):
        z0 = 0.06 + row * 0.165
        z1 = z0 + 0.150
        for i in range(14):
            lo = (i + row * 0.5) * math.tau / 14.0 + 0.012
            hi = lo + math.tau / 14.0 - 0.024
            vv = [
                (silo_cx + rr * math.cos(a), silo_cy + rr * math.sin(a), z)
                for z in (z0, z1)
                for rr, a in ((silo_r - 0.02, lo), (silo_r + 0.028, lo), (silo_r + 0.028, hi), (silo_r - 0.02, hi))
            ]
            g.add(
                f'{tag}_stone_{row}_{i}',
                vv,
                [[3, 2, 1, 0], [4, 5, 6, 7], [0, 1, 5, 4], [1, 2, 6, 5], [2, 3, 7, 6], [3, 0, 4, 7]],
                'stone',
                np.mean(vv, axis=0),
            )

    g.cylinder(f'{tag}_belt_trim', (silo_cx, silo_cy, 0.89), (silo_cx, silo_cy, 0.96), silo_r + 0.055, 'trim', segments=32)

    stave_h = max(0.85, silo_h - 0.98)
    stave_cz = 0.96 + stave_h * 0.5
    for i in range(16):
        ang = i * math.tau / 16.0
        sx = silo_cx + (silo_r + 0.012) * math.cos(ang)
        sy = silo_cy + (silo_r + 0.012) * math.sin(ang)
        rot_stave = np.array([[math.cos(ang), -math.sin(ang), 0.0], [math.sin(ang), math.cos(ang), 0.0], [0.0, 0.0, 1.0]])
        g.box(f'{tag}_stave_{i}', (sx, sy, stave_cz), (0.045, 0.16, stave_h), 'wood', 0.008, rot_stave)

    for h_idx, hz in enumerate(np.linspace(1.08, silo_h - 0.12, 3)):
        torus_ring(
            g,
            f'{tag}_hoop_{h_idx}',
            (silo_cx, silo_cy, float(hz)),
            (0.0, 0.0, 1.0),
            silo_r + 0.038,
            0.018,
            'brass' if (with_rune_seals and h_idx == 1) else 'iron',
            major_segments=24,
            minor_segments=6,
        )

    if with_rune_seals:
        # Glowing golden preservation rune seals inlaid on the 4 cardinal points of the silo drum
        for r_idx, ang in enumerate((0.0, math.pi * 0.5, math.pi, math.pi * 1.5)):
            rx = silo_cx + (silo_r + 0.042) * math.cos(ang)
            ry = silo_cy + (silo_r + 0.042) * math.sin(ang)
            rot_r = np.array([[math.cos(ang), -math.sin(ang), 0.0], [math.sin(ang), math.cos(ang), 0.0], [0.0, 0.0, 1.0]])
            g.box(f'{tag}_rune_frame_{r_idx}', (rx, ry, 1.46), (0.045, 0.22, 0.42), 'brass', 0.006, rot_r)
            g.box(f'{tag}_rune_glow_{r_idx}', (rx + 0.012 * math.cos(ang), ry + 0.012 * math.sin(ang), 1.46), (0.032, 0.14, 0.30), 'glass', 0.005, rot_r)

    cap_base = silo_h - 0.02
    cap_rise = 0.82
    g.cylinder(f'{tag}_cap_underlay', (silo_cx, silo_cy, cap_base), (silo_cx, silo_cy, cap_base + cap_rise), silo_r + 0.10, roof_mat, end_radius=0.05, segments=28)
    for ring in range(5):
        t = ring / 5.0
        r_ring = (silo_r + 0.09) * (1.0 - t * 0.78)
        z_ring = cap_base + cap_rise * t + 0.04
        n_tiles = max(8, int(18 - ring * 2))
        for ti in range(n_tiles):
            ang = (ti + 0.5 * (ring % 2)) * math.tau / n_tiles
            radial = np.array([math.cos(ang), math.sin(ang), 0.0])
            tangent = np.array([-math.sin(ang), math.cos(ang), 0.0])
            slope_vec = unit(np.array([-radial[0] * (silo_r + 0.09), -radial[1] * (silo_r + 0.09), cap_rise]))
            norm_vec = unit(np.cross(tangent, slope_vec))
            if norm_vec[2] < 0.0:
                norm_vec = -norm_vec
            rot_tile = np.column_stack((tangent, slope_vec, norm_vec))
            tc = np.array([silo_cx, silo_cy, z_ring]) + radial * r_ring + norm_vec * 0.02
            w_tile = 2.0 * math.pi * r_ring / n_tiles * 0.92
            g.box(f'{tag}_cap_tile_{ring}_{ti}', tc, (w_tile, 0.19, 0.036), roof_mat, 0.006, rot_tile, top_bevel_only=True)
    g.cylinder(f'{tag}_finial', (silo_cx, silo_cy, cap_base + cap_rise - 0.02), (silo_cx, silo_cy, cap_base + cap_rise + 0.22), 0.060, 'brass', end_radius=0.022, segments=16)


# =============================================================================
# SPECIALIZED BUILDING ASSEMBLIES FOR ZONE 04 (FARM_CORE)
# =============================================================================

def build_main_barn_farmhouse() -> MapGeometry:
    """Công trình 1 at (-17.8, 5.4): `Nhà Chính & Đài Điều Khiển Bạch Vân` (Main Homestead, Brick Feed Stove & Bạch Vân Relay)."""
    g = MapGeometry()
    main_w, main_d, main_eave = 2.48, 1.82, 1.66
    wing_w, wing_d, wing_eave = 1.24, 1.32, 1.54
    shed_w, shed_d, shed_eave = 1.02, 1.26, 1.16

    main_cx, main_cy = 0.0, 0.24
    wing_cx, wing_cy = -0.46, -0.56
    shed_cx, shed_cy = 1.62, 0.20

    masonry_block(g, 'barn_main', main_cx, main_cy, main_w, main_d, eave=main_eave, stone_rows=7)
    masonry_block(g, 'barn_wing', wing_cx, wing_cy, wing_w, wing_d, eave=wing_eave, stone_rows=6)
    masonry_block(g, 'barn_shed', shed_cx, shed_cy, shed_w, shed_d, eave=shed_eave, stone_rows=4)

    def under_wing(p):
        return abs(p[0] - wing_cx) < (wing_w * 0.56) and p[1] < 0.22 and p[2] < wing_eave + 0.94 * (1.0 - abs(p[0] - wing_cx) / (wing_w * 0.62))

    tiled_roof(g, 'barn_main_roof', main_cx, main_cy, main_w + 0.24, main_d + 0.26, main_eave + 0.01, 1.12, ridge_x=True, roof_mat='terracotta', occluders=(under_wing,))
    tiled_roof(g, 'barn_wing_roof', wing_cx, wing_cy, wing_w + 0.22, wing_d + 0.22, wing_eave + 0.01, 0.96, ridge_x=False, roof_mat='terracotta')
    tiled_roof(g, 'barn_shed_roof', shed_cx, shed_cy, shed_w + 0.18, shed_d + 0.20, shed_eave + 0.01, 0.66, ridge_x=False, roof_mat='terracotta')

    # Covered Front Veranda Porch (`Hiên Nhà Gỗ`) on the front-east side of the main house
    porch_cx, porch_cy = 0.64, -0.96
    porch_w, porch_d = 1.18, 0.64
    g.box('barn_porch_deck', (porch_cx, porch_cy, -0.04), (porch_w + 0.08, porch_d + 0.08, 0.22), 'stone', 0.018)
    for px in (porch_cx - porch_w * 0.44, porch_cx + porch_w * 0.44):
        g.box(f'barn_porch_plinth_{px:+.2f}', (px, porch_cy - porch_d * 0.38, 0.16), (0.16, 0.16, 0.22), 'trim', 0.012)
        g.box(f'barn_porch_post_{px:+.2f}', (px, porch_cy - porch_d * 0.38, 0.62), (0.10, 0.10, 0.78), 'wood', 0.010)
    g.beam('barn_porch_plate', (porch_cx - porch_w * 0.48, porch_cy - porch_d * 0.38, 0.98), (porch_cx + porch_w * 0.48, porch_cy - porch_d * 0.38, 0.98), 0.08, 'wood')
    tiled_roof(g, 'barn_porch_roof', porch_cx, porch_cy, porch_w + 0.16, porch_d + 0.16, 1.00, 0.36, ridge_x=True, roof_mat='terracotta')

    # Brick Feed-Fermentation Stove (`Lò Ủ Cám Xây Bằng Gạch Sát Cửa Sổ` per Kỹ thuật.docx Section 6.2)
    stove_cx, stove_cy = 1.66, -0.58
    g.box('feed_stove_plinth', (stove_cx, stove_cy, 0.04), (0.86, 0.72, 0.28), 'stone', 0.016)
    g.box('feed_stove_brick_body', (stove_cx, stove_cy, 0.44), (0.76, 0.62, 0.56), 'terracotta', 0.014)
    g.box('feed_stove_top_slab', (stove_cx, stove_cy, 0.74), (0.82, 0.68, 0.08), 'trim', 0.012)
    # Glowing amber firebox mouth + iron cauldron pot for cooking pig feed
    g.box('feed_stove_firebox_frame', (stove_cx, stove_cy - 0.31, 0.32), (0.36, 0.08, 0.30), 'iron', 0.008)
    g.box('feed_stove_firebox_glow', (stove_cx, stove_cy - 0.33, 0.32), (0.26, 0.06, 0.22), 'glass', 0.006)
    g.cylinder('feed_stove_cauldron', (stove_cx - 0.12, stove_cy - 0.04, 0.76), (stove_cx - 0.12, stove_cy - 0.04, 1.04), 0.22, 'iron', end_radius=0.25, segments=18)
    g.cylinder('feed_stove_cauldron_lid', (stove_cx - 0.12, stove_cy - 0.04, 1.03), (stove_cx - 0.12, stove_cy - 0.04, 1.12), 0.24, 'brass', end_radius=0.06, segments=18)
    add_stone_chimney(g, 'feed_stove_chimney', stove_cx + 0.20, stove_cy + 0.12, 0.78, courses=8, course_mat='brick')

    # Rooftop `Bảng Điều Khiển Bạch Vân` (Bạch Vân Command Relay Console on the main roof ridge)
    bv_cx, bv_cy, bv_z = 0.18, main_cy, main_eave + 1.02
    g.cylinder('bachvan_deck_base', (bv_cx, bv_cy, bv_z - 0.18), (bv_cx, bv_cy, bv_z + 0.08), 0.38, 'stone', segments=8)
    g.cylinder('bachvan_deck_trim', (bv_cx, bv_cy, bv_z + 0.06), (bv_cx, bv_cy, bv_z + 0.14), 0.42, 'brass', segments=8)
    g.cylinder('bachvan_mast', (bv_cx, bv_cy, bv_z + 0.12), (bv_cx, bv_cy, bv_z + 0.78), 0.055, 'brass', end_radius=0.035, segments=12)
    torus_ring(g, 'bachvan_ring_horiz', (bv_cx, bv_cy, bv_z + 0.56), (0.0, 0.0, 1.0), 0.24, 0.022, 'brass', major_segments=20, minor_segments=8)
    torus_ring(g, 'bachvan_ring_diag1', (bv_cx, bv_cy, bv_z + 0.56), (0.7, 0.2, 0.7), 0.21, 0.018, 'brass', major_segments=20, minor_segments=8)
    closed_ring_solid(
        g,
        'bachvan_crystal_core',
        (bv_cx, bv_cy, bv_z + 0.56),
        [(-0.14, 0.02, 0.02), (-0.05, 0.10, 0.10), (0.05, 0.10, 0.10), (0.14, 0.02, 0.02)],
        sides=8,
        material='glass',
    )

    add_roof_dormer(g, 'barn_dormer', 0.56, -0.36, main_eave + 0.34, width=0.52, depth=0.46, wall_h=0.38, rise=0.26, roof_mat='terracotta')

    wing_front_y = wing_cy - wing_d / 2.0 - 0.02
    arch_window(g, 'barn_double_doors', (wing_cx, wing_front_y, 0.04), width=0.62, height=0.98, yaw=0.0, door=True, double_door=True)
    arch_window(g, 'barn_win_wing_gable', (wing_cx, wing_front_y - 0.03, wing_eave + 0.08), width=0.30, height=0.48, yaw=0.0)
    arch_window(g, 'barn_win_front_east', (0.62, main_cy - main_d / 2.0 - 0.02, 0.28), width=0.36, height=0.64, yaw=0.0)
    arch_window(g, 'barn_win_west_1', (main_cx - main_w / 2.0 - 0.02, -0.15, 0.28), width=0.34, height=0.62, yaw=-math.pi / 2.0)
    arch_window(g, 'barn_win_west_2', (main_cx - main_w / 2.0 - 0.02, 0.58, 0.28), width=0.34, height=0.62, yaw=-math.pi / 2.0)
    arch_window(g, 'barn_win_east_shed', (shed_cx + shed_w / 2.0 + 0.02, shed_cy, 0.22), width=0.30, height=0.52, yaw=math.pi / 2.0)
    arch_window(g, 'barn_win_rear_1', (-0.55, main_cy + main_d / 2.0 + 0.02, 0.28), width=0.34, height=0.62, yaw=math.pi)
    arch_window(g, 'barn_win_rear_2', (0.45, main_cy + main_d / 2.0 + 0.02, 0.28), width=0.34, height=0.62, yaw=math.pi)

    add_entrance_steps(g, 'barn_steps', wing_cx, wing_front_y, base_z=-0.01, width=0.86, steps=3)
    add_hanging_lanterns(g, 'barn_lantern', wing_cx, wing_front_y, z_bracket=1.08, span_x=0.44)
    add_stone_chimney(g, 'barn_chimney', -0.82, 0.66, main_eave + 0.58, courses=6, course_mat='stone')
    return g


def build_storage_granary_tier1() -> MapGeometry:
    """`Nhà Kho Cấp 1` (Kho Gỗ Kê Đá Sơ Khai — Upgrade Stage 1):
    Compact timber-framed storehouse raised on 6 carved stone staddle-pillars with wooden plank walls,
    exterior X-bracing, pitched wood/terracotta roof, front loading steps, and a small side wooden grain bin.
    """
    g = MapGeometry()
    w, d = 1.84, 1.38
    floor_z = 0.26
    eave_z = floor_z + 1.12

    # 6 Carved Stone Staddle-Pillars (`Chân Kê Đá Chống Chuột/Ẩm`)
    for ix, sx in enumerate((-0.74, 0.0, 0.74)):
        for iy, sy in enumerate((-0.52, 0.52)):
            g.cylinder(f't1_staddle_col_{ix}_{iy}', (sx, sy, -0.18), (sx, sy, floor_z - 0.06), 0.14, 'stone', end_radius=0.11, segments=10)
            g.cylinder(f't1_staddle_cap_{ix}_{iy}', (sx, sy, floor_z - 0.07), (sx, sy, floor_z + 0.01), 0.20, 'trim', segments=12)

    # Raised Timber Floor Deck & Plank Storehouse Box
    g.box('t1_deck', (0.0, 0.0, floor_z + 0.04), (w + 0.14, d + 0.14, 0.10), 'wood', 0.012)
    g.box('t1_core', (0.0, 0.0, floor_z + 0.58), (w, d, 1.06), 'plaster', 0.014)

    # Exterior Timber Corner Posts, Horizontal Belt Beams & X-Bracing
    for sx in (-w / 2.0, w / 2.0):
        for sy in (-d / 2.0, d / 2.0):
            g.box(f't1_post_{sx:+.2f}_{sy:+.2f}', (sx, sy, floor_z + 0.58), (0.11, 0.11, 1.08), 'wood', 0.010)
    for sy in (-d / 2.0 - 0.02, d / 2.0 + 0.02):
        for side_x in (-1, 1):
            x0, x1 = side_x * 0.84, side_x * 0.34
            g.beam(f't1_xbrace_a_{sy:+.2f}_{side_x}', (x0, sy, floor_z + 0.12), (x1, sy, eave_z - 0.08), 0.052, 'wood')
            g.beam(f't1_xbrace_b_{sy:+.2f}_{side_x}', (x1, sy, floor_z + 0.12), (x0, sy, eave_z - 0.08), 0.052, 'wood')

    tiled_roof(g, 't1_roof', 0.0, 0.0, w + 0.22, d + 0.22, eave_z, 0.78, ridge_x=True, roof_mat='terracotta')

    # Front Door, Small Gable Vent & Wooden Loading Steps
    arch_window(g, 't1_door', (0.0, -d / 2.0 - 0.02, floor_z + 0.04), width=0.52, height=0.86, yaw=0.0, door=True, double_door=False)
    arch_window(g, 't1_win_west', (-w / 2.0 - 0.02, 0.0, floor_z + 0.26), width=0.30, height=0.50, yaw=-math.pi / 2.0)
    arch_window(g, 't1_win_east', (w / 2.0 + 0.02, 0.0, floor_z + 0.26), width=0.30, height=0.50, yaw=math.pi / 2.0)
    add_entrance_steps(g, 't1_steps', 0.0, -d / 2.0 - 0.04, base_z=floor_z - 0.04, width=0.72, steps=4)

    # Small Side Wooden Grain Bin (`Thùng Gỗ Trữ Cám Cấp 1`) on West Side
    bin_cx, bin_cy = -1.24, 0.04
    g.box('t1_bin_base', (bin_cx, bin_cy, 0.04), (0.64, 0.68, 0.24), 'stone', 0.014)
    g.box('t1_bin_body', (bin_cx, bin_cy, 0.44), (0.58, 0.62, 0.60), 'wood', 0.012)
    tiled_roof(g, 't1_bin_lid', bin_cx, bin_cy, 0.68, 0.72, 0.74, 0.26, ridge_x=False, roof_mat='terracotta')
    return g


def build_storage_granary_tier2() -> MapGeometry:
    """`Nhà Kho Cấp 2` (Kho Đá Kiên Cố & Tháp Silo Đơn — Upgrade Stage 2):
    Fortified ashlar stone-and-plaster granary with heavy stone buttresses, rooftop ventilation monitor,
    and 1 cylindrical stone-and-stave grain silo connected by an overhead chute bridge.
    """
    g = MapGeometry()
    main_w, main_d, main_eave = 2.42, 1.80, 1.48
    wing_w, wing_d, wing_eave = 1.22, 1.30, 1.50
    main_cx, main_cy = 0.0, 0.22
    wing_cx, wing_cy = 0.44, -0.56

    masonry_block(g, 't2_main', main_cx, main_cy, main_w, main_d, eave=main_eave, stone_rows=6)
    masonry_block(g, 't2_wing', wing_cx, wing_cy, wing_w, wing_d, eave=wing_eave, stone_rows=6)

    # Stone Counterfort Buttresses along the Warehouse Walls
    for bx in (-0.88, 0.88):
        g.box(f't2_buttress_rear_{bx:+.2f}', (bx, main_cy + main_d / 2.0 + 0.08, 0.56), (0.24, 0.22, 1.18), 'stone', 0.014)

    def under_wing(p):
        return abs(p[0] - wing_cx) < (wing_w * 0.56) and p[1] < 0.22 and p[2] < wing_eave + 0.94 * (1.0 - abs(p[0] - wing_cx) / (wing_w * 0.62))

    tiled_roof(g, 't2_main_roof', main_cx, main_cy, main_w + 0.24, main_d + 0.24, main_eave + 0.01, 1.04, ridge_x=True, roof_mat='terracotta', occluders=(under_wing,))
    tiled_roof(g, 't2_wing_roof', wing_cx, wing_cy, wing_w + 0.22, wing_d + 0.22, wing_eave + 0.01, 0.94, ridge_x=False, roof_mat='terracotta')

    # Single West Cylindrical Grain Silo + Overhead Chute Bridge
    _add_grain_silo_tower(g, 't2_silo_west', -1.58, 0.16, silo_r=0.52, silo_h=1.98, roof_mat='terracotta', with_rune_seals=False)
    g.box('t2_chute_bridge', (-1.18, 0.16, 1.54), (0.58, 0.42, 0.34), 'wood', 0.012)
    tiled_roof(g, 't2_chute_roof', -1.18, 0.16, 0.64, 0.52, 1.70, 0.24, ridge_x=True, roof_mat='terracotta')

    add_roof_dormer(g, 't2_dormer', -0.50, -0.36, main_eave + 0.34, width=0.52, depth=0.46, wall_h=0.36, rise=0.26, roof_mat='terracotta')

    wing_front_y = wing_cy - wing_d / 2.0 - 0.02
    arch_window(g, 't2_door', (wing_cx, wing_front_y, 0.04), width=0.58, height=0.96, yaw=0.0, door=True, double_door=True)
    arch_window(g, 't2_win_wing_gable', (wing_cx, wing_front_y - 0.03, wing_eave + 0.08), width=0.30, height=0.48, yaw=0.0)
    arch_window(g, 't2_win_front_west', (-0.52, main_cy - main_d / 2.0 - 0.02, 0.28), width=0.36, height=0.64, yaw=0.0)
    arch_window(g, 't2_win_east_1', (main_cx + main_w / 2.0 + 0.02, -0.14, 0.28), width=0.34, height=0.62, yaw=math.pi / 2.0)
    arch_window(g, 't2_win_east_2', (main_cx + main_w / 2.0 + 0.02, 0.58, 0.28), width=0.34, height=0.62, yaw=math.pi / 2.0)
    arch_window(g, 't2_win_rear_1', (-0.52, main_cy + main_d / 2.0 + 0.02, 0.28), width=0.34, height=0.62, yaw=math.pi)
    arch_window(g, 't2_win_rear_2', (0.52, main_cy + main_d / 2.0 + 0.02, 0.28), width=0.34, height=0.62, yaw=math.pi)

    add_entrance_steps(g, 't2_steps', wing_cx, wing_front_y, base_z=-0.01, width=0.84, steps=3)
    add_hanging_lanterns(g, 't2_lantern', wing_cx, wing_front_y, z_bracket=1.08, span_x=0.44)
    add_stone_chimney(g, 't2_chimney', 0.80, 0.64, main_eave + 0.56, courses=6, course_mat='brick')
    return g


def build_storage_granary_tier3() -> MapGeometry:
    """`Nhà Kho Cấp 3` (Đại Bảo Khố Linh Cốc & Song Tháp Silo Cấp Đại — Upgrade Stage 3):
    Monumental multi-bay vaulted stone-and-bronze logistics citadel featuring TWIN fortified cylindrical silos
    flanking West and East, linked by an elevated covered overhead conveyor gallery through a central clerestory
    monitor roof, plus a cantilevered cargo crane hoist and glowing golden preservation rune seals.
    """
    g = MapGeometry()
    main_w, main_d, main_eave = 2.66, 1.94, 1.64
    wing_w, wing_d, wing_eave = 1.34, 1.38, 1.66
    main_cx, main_cy = 0.0, 0.24
    wing_cx, wing_cy = 0.0, -0.58

    masonry_block(g, 't3_main', main_cx, main_cy, main_w, main_d, eave=main_eave, stone_rows=7)
    masonry_block(g, 't3_wing', wing_cx, wing_cy, wing_w, wing_d, eave=wing_eave, stone_rows=7)

    # Heavy Fortified Stone Corner & Flank Buttresses with Brass Caps
    for bx in (-1.04, 1.04):
        for by_s in (-1, 1):
            by = main_cy + by_s * (main_d / 2.0 + 0.08)
            g.box(f't3_buttress_{bx:+.2f}_{by_s}', (bx, by, 0.64), (0.26, 0.24, 1.34), 'stone', 0.014)
            g.box(f't3_buttress_cap_{bx:+.2f}_{by_s}', (bx, by, 1.34), (0.30, 0.28, 0.08), 'trim', 0.010)

    def under_wing(p):
        return abs(p[0] - wing_cx) < (wing_w * 0.56) and p[1] < 0.24 and p[2] < wing_eave + 0.98 * (1.0 - abs(p[0] - wing_cx) / (wing_w * 0.62))

    tiled_roof(g, 't3_main_roof', main_cx, main_cy, main_w + 0.26, main_d + 0.26, main_eave + 0.01, 1.12, ridge_x=True, roof_mat='terracotta', occluders=(under_wing,))
    tiled_roof(g, 't3_wing_roof', wing_cx, wing_cy, wing_w + 0.24, wing_d + 0.24, wing_eave + 0.01, 1.02, ridge_x=False, roof_mat='terracotta')

    # Raised Central Clerestory Monitor Turret (`Tầng Mái Thông Gió Bảo Quản Linh Cốc`) on Main Ridge
    mon_w, mon_d, mon_base_z, mon_h = 1.48, 0.82, main_eave + 0.72, 0.46
    g.box('t3_monitor_wall', (main_cx, main_cy, mon_base_z + mon_h * 0.5), (mon_w, mon_d, mon_h), 'plaster', 0.012)
    g.box('t3_monitor_trim', (main_cx, main_cy, mon_base_z + mon_h), (mon_w + 0.10, mon_d + 0.10, 0.07), 'brass', 0.008)
    tiled_roof(g, 't3_monitor_roof', main_cx, main_cy, mon_w + 0.18, mon_d + 0.18, mon_base_z + mon_h + 0.01, 0.42, ridge_x=True, roof_mat='terracotta')

    # TWIN Fortified Grain Silo Towers (`Silo Tây` at -1.76 and `Silo Đông` at +1.76) with Preservation Rune Seals
    _add_grain_silo_tower(g, 't3_silo_west', -1.76, 0.20, silo_r=0.55, silo_h=2.24, roof_mat='terracotta', with_rune_seals=True)
    _add_grain_silo_tower(g, 't3_silo_east',  1.76, 0.20, silo_r=0.55, silo_h=2.24, roof_mat='terracotta', with_rune_seals=True)

    # Elevated Covered Overhead Conveyor Bridges (`Hành Lang Cầu Nối Trên Cao`) linking both Silos to the Main Vault
    for side_tag, cx_b in (('west', -1.32), ('east', 1.32)):
        g.box(f't3_conveyor_{side_tag}', (cx_b, 0.20, 1.76), (0.66, 0.48, 0.38), 'wood', 0.012)
        g.beam(f't3_conveyor_strut_{side_tag}', (cx_b, -0.02, 1.24), (cx_b, -0.02, 1.58), 0.065, 'iron')
        tiled_roof(g, f't3_conveyor_roof_{side_tag}', cx_b, 0.20, 0.72, 0.56, 1.94, 0.26, ridge_x=True, roof_mat='terracotta')

    # Cantilevered Cargo Hoist Crane & Pulley (`Cần Trục Bốc Dỡ Đại Bảo Khố`) on the Front Gable Peak
    wing_front_y = wing_cy - wing_d / 2.0 - 0.02
    hoist_z = wing_eave + 0.88
    hoist_tip_y = wing_front_y - 0.56
    g.beam('t3_hoist_beam', (wing_cx, wing_front_y + 0.16, hoist_z), (wing_cx, hoist_tip_y, hoist_z), 0.092, 'wood')
    g.beam('t3_hoist_brace', (wing_cx, wing_front_y - 0.02, hoist_z - 0.36), (wing_cx, hoist_tip_y + 0.14, hoist_z - 0.02), 0.065, 'iron')
    g.cylinder('t3_hoist_wheel', (wing_cx - 0.04, hoist_tip_y + 0.06, hoist_z - 0.08), (wing_cx + 0.04, hoist_tip_y + 0.06, hoist_z - 0.08), 0.085, 'brass', segments=18)
    g.cylinder('t3_hoist_chain', (wing_cx, hoist_tip_y + 0.06, hoist_z - 0.64), (wing_cx, hoist_tip_y + 0.06, hoist_z - 0.10), 0.018, 'iron', segments=10)
    g.box('t3_hoist_crate', (wing_cx, hoist_tip_y + 0.06, hoist_z - 0.74), (0.28, 0.28, 0.24), 'wood', 0.010)
    g.box('t3_hoist_crate_strap', (wing_cx, hoist_tip_y + 0.06, hoist_z - 0.74), (0.30, 0.30, 0.05), 'brass', 0.006)

    # Glowing Golden Preservation Seal Emblem (`Ấn Bảo Quản Linh Cốc`) on Front Gable
    torus_ring(g, 't3_gable_seal_ring', (wing_cx, wing_front_y - 0.04, wing_eave + 0.36), (0.0, 1.0, 0.0), 0.22, 0.024, 'brass', major_segments=20, minor_segments=8)
    g.cylinder('t3_gable_seal_core', (wing_cx, wing_front_y - 0.06, wing_eave + 0.36), (wing_cx, wing_front_y - 0.01, wing_eave + 0.36), 0.16, 'glass', segments=16)

    # Vault Double Doors & Arched Windows
    arch_window(g, 't3_vault_doors', (wing_cx, wing_front_y, 0.04), width=0.68, height=1.04, yaw=0.0, door=True, double_door=True)
    arch_window(g, 't3_win_front_w', (-0.86, main_cy - main_d / 2.0 - 0.02, 0.30), width=0.36, height=0.64, yaw=0.0)
    arch_window(g, 't3_win_front_e', ( 0.86, main_cy - main_d / 2.0 - 0.02, 0.30), width=0.36, height=0.64, yaw=0.0)
    arch_window(g, 't3_win_silo_w', (-1.76, 0.20 - 0.56, 0.38), width=0.26, height=0.46, yaw=0.0)
    arch_window(g, 't3_win_silo_e', ( 1.76, 0.20 - 0.56, 0.38), width=0.26, height=0.46, yaw=0.0)
    arch_window(g, 't3_win_rear_1', (-0.64, main_cy + main_d / 2.0 + 0.02, 0.30), width=0.36, height=0.64, yaw=math.pi)
    arch_window(g, 't3_win_rear_2', ( 0.64, main_cy + main_d / 2.0 + 0.02, 0.30), width=0.36, height=0.64, yaw=math.pi)

    add_entrance_steps(g, 't3_steps', wing_cx, wing_front_y, base_z=-0.01, width=0.96, steps=3)
    add_hanging_lanterns(g, 't3_lantern', wing_cx, wing_front_y, z_bracket=1.14, span_x=0.48)
    return g


def build_granary_feed_kitchen() -> MapGeometry:
    """Default `Nhà Kho` builder for Zone04 at (-17.0, 9.2): returns the flagship Tier 3 Twin-Silo Logistics Granary."""
    return build_storage_granary_tier3()


def build_construction_guild_hall() -> MapGeometry:
    """Công trình 3 at (-13.4, 9.4): `Phòng Xây Dựng` (Construction Guild, Open Drafting Bay, Giant A-Frame Crane & Twin-Stack Forge).

    Completely distinct Industrial-Medieval Engineering Hall:
    - West High-Bay North-South Slate-Blue Workshop Hall (`ridge_x=False`) with clerestory roof monitor
    - East Open-Sided Timber Truss Carpenter & Siege Assembly Bay housing the `Bàn Học Bản Vẽ` (Blueprint Table) and turret frame
    - Giant Freestanding A-Frame Timber & Iron Gantry Crane (`Cần Trục Chữ A`) straddling the front workyard
    - Heavy Stone Forge Furnace & Twin Iron-Crowned Smokestacks on the west flank with outdoor iron anvil
    """
    g = MapGeometry()

    # 1. West High-Bay North-South Engineering Hall (`ridge_x=False`)
    hall_cx, hall_cy = -0.52, 0.16
    hall_w, hall_d, hall_eave = 1.66, 2.08, 1.62
    masonry_block(g, 'guild_hall', hall_cx, hall_cy, hall_w, hall_d, eave=hall_eave, stone_rows=7)
    tiled_roof(g, 'guild_hall_roof', hall_cx, hall_cy, hall_w + 0.24, hall_d + 0.24, hall_eave + 0.01, 1.16, ridge_x=False, roof_mat='roof')

    # Rooftop Industrial Clerestory Monitor on the High-Bay Roof
    g.box('guild_clerestory_wall', (hall_cx, hall_cy + 0.18, hall_eave + 0.96), (0.68, 1.06, 0.38), 'plaster', 0.012)
    tiled_roof(g, 'guild_clerestory_roof', hall_cx, hall_cy + 0.18, 0.82, 1.18, hall_eave + 1.14, 0.30, ridge_x=False, roof_mat='roof')

    # 2. East Open-Sided Timber-Truss Carpenter & Blueprint Drafting Pavilion (`Xưởng Lắp Ráp Khung Mở & Bàn Học Bản Vẽ`)
    bay_cx, bay_cy = 1.04, 0.12
    bay_w, bay_d, bay_eave = 1.48, 1.84, 1.32
    g.box('guild_bay_plinth', (bay_cx, bay_cy, -0.06), (bay_w + 0.12, bay_d + 0.12, 0.28), 'stone', 0.020)
    g.box('guild_bay_floor', (bay_cx, bay_cy, 0.10), (bay_w + 0.04, bay_d + 0.04, 0.08), 'trim', 0.012)

    # 6 Heavy Timber Columns + Knee Braces + Overhead Tie Beams supporting the Pavilion Roof
    for ix, px in enumerate((bay_cx - bay_w * 0.44, bay_cx + bay_w * 0.44)):
        for iy, py in enumerate((bay_cy - bay_d * 0.44, bay_cy, bay_cy + bay_d * 0.44)):
            g.box(f'guild_bay_ped_{ix}_{iy}', (px, py, 0.22), (0.20, 0.20, 0.20), 'stone', 0.012)
            g.box(f'guild_bay_col_{ix}_{iy}', (px, py, 0.78), (0.13, 0.13, 1.04), 'wood', 0.010)
            g.beam(f'guild_bay_knee_{ix}_{iy}', (px, py, 0.96), (px + (-0.22 if ix == 1 else 0.22), py, bay_eave - 0.04), 0.055, 'wood')
    for py in (bay_cy - bay_d * 0.44, bay_cy, bay_cy + bay_d * 0.44):
        g.beam(f'guild_bay_tie_{py:+.2f}', (bay_cx - bay_w * 0.48, py, bay_eave - 0.04), (bay_cx + bay_w * 0.48, py, bay_eave - 0.04), 0.085, 'wood')
    tiled_roof(g, 'guild_bay_roof', bay_cx, bay_cy, bay_w + 0.22, bay_d + 0.22, bay_eave, 0.76, ridge_x=False, roof_mat='roof')

    # `Bàn Học Bản Vẽ` (Blueprint Drafting Table) inside the Open Pavilion
    bt_x, bt_y = bay_cx - 0.12, bay_cy - 0.28
    g.box('blueprint_table_legs', (bt_x, bt_y, 0.34), (0.68, 0.44, 0.42), 'wood', 0.010)
    g.box('blueprint_table_top', (bt_x, bt_y, 0.58), (0.78, 0.54, 0.07), 'wood', 0.010)
    g.box('blueprint_scroll_sheet', (bt_x, bt_y, 0.625), (0.58, 0.38, 0.032), 'glass', 0.005)
    g.cylinder('blueprint_roll', (bt_x - 0.24, bt_y + 0.14, 0.65), (bt_x + 0.24, bt_y + 0.14, 0.65), 0.042, 'brass', segments=12)

    # Partially Assembled `Khung Tháp Nỏ` (Crossbow Defense Turret Frame) in the Rear Bay
    tf_x, tf_y = bay_cx + 0.16, bay_cy + 0.42
    g.box('turret_frame_base', (tf_x, tf_y, 0.28), (0.52, 0.52, 0.32), 'wood', 0.010)
    g.cylinder('turret_frame_pivot', (tf_x, tf_y, 0.42), (tf_x, tf_y, 0.78), 0.08, 'brass', segments=12)
    g.beam('turret_bow_arm', (tf_x - 0.36, tf_y - 0.12, 0.76), (tf_x + 0.36, tf_y - 0.12, 0.76), 0.055, 'iron')
    g.beam('turret_stock', (tf_x, tf_y - 0.32, 0.76), (tf_x, tf_y + 0.28, 0.76), 0.065, 'wood')

    # 3. Giant Freestanding A-Frame Timber & Iron Gantry Crane (`Cần Trục Chữ A Khổng Lồ`) at Front Apron
    crane_y = -1.08
    crane_x0, crane_x1 = -0.36, 0.88
    crane_top_z = 2.48
    for side_tag, cx_leg in (('w', crane_x0), ('e', crane_x1)):
        g.box(f'gantry_foot_f_{side_tag}', (cx_leg, crane_y - 0.44, 0.02), (0.24, 0.24, 0.24), 'stone', 0.014)
        g.box(f'gantry_foot_b_{side_tag}', (cx_leg, crane_y + 0.44, 0.02), (0.24, 0.24, 0.24), 'stone', 0.014)
        g.beam(f'gantry_leg_f_{side_tag}', (cx_leg, crane_y - 0.42, 0.08), (cx_leg, crane_y, crane_top_z), 0.105, 'wood')
        g.beam(f'gantry_leg_b_{side_tag}', (cx_leg, crane_y + 0.42, 0.08), (cx_leg, crane_y, crane_top_z), 0.105, 'wood')
        g.beam(f'gantry_crossbar_{side_tag}', (cx_leg, crane_y - 0.24, 1.28), (cx_leg, crane_y + 0.24, 1.28), 0.075, 'iron')
    g.beam('gantry_top_beam', (crane_x0 - 0.18, crane_y, crane_top_z), (crane_x1 + 0.18, crane_y, crane_top_z), 0.135, 'wood')
    crane_mid_x = 0.5 * (crane_x0 + crane_x1)
    g.box('gantry_trolley', (crane_mid_x, crane_y, crane_top_z - 0.04), (0.28, 0.24, 0.22), 'iron', 0.010)
    g.cylinder('gantry_gear_wheel', (crane_mid_x, crane_y - 0.15, crane_top_z - 0.04), (crane_mid_x, crane_y + 0.15, crane_top_z - 0.04), 0.16, 'brass', segments=20)
    g.cylinder('gantry_cable', (crane_mid_x, crane_y, 1.18), (crane_mid_x, crane_y, crane_top_z - 0.12), 0.022, 'iron', segments=10)
    g.box('gantry_hook_block', (crane_mid_x, crane_y, 1.14), (0.14, 0.14, 0.14), 'brass', 0.010)
    g.box('gantry_suspended_stone', (crane_mid_x, crane_y, 0.82), (0.44, 0.38, 0.36), 'stone', 0.018)
    g.box('gantry_stone_sling', (crane_mid_x, crane_y, 0.82), (0.47, 0.41, 0.06), 'iron', 0.008)

    # 4. Heavy Stone Forge Furnace & Twin Iron-Crowned Smokestacks (`Lò Rèn Đá & Ống Khói Đôi`) on West Flank
    forge_cx, forge_cy = -1.62, 0.22
    g.box('forge_furnace_base', (forge_cx, forge_cy, 0.42), (0.72, 1.16, 0.96), 'stone', 0.018)
    g.box('forge_furnace_cap', (forge_cx, forge_cy, 0.94), (0.78, 1.22, 0.10), 'trim', 0.012)
    g.box('forge_hearth_frame', (forge_cx - 0.35, forge_cy, 0.38), (0.10, 0.48, 0.42), 'iron', 0.008)
    g.box('forge_hearth_glow', (forge_cx - 0.37, forge_cy, 0.38), (0.08, 0.36, 0.30), 'glass', 0.006)
    add_stone_chimney(g, 'forge_stack_south', forge_cx + 0.04, forge_cy - 0.30, 0.98, courses=9, course_mat='stone')
    add_stone_chimney(g, 'forge_stack_north', forge_cx + 0.04, forge_cy + 0.30, 0.98, courses=10, course_mat='brick')
    g.beam('forge_stack_tie', (forge_cx + 0.04, forge_cy - 0.30, 1.92), (forge_cx + 0.04, forge_cy + 0.30, 1.92), 0.055, 'iron')

    # Outdoor Iron Anvil (`Đe Rèn`) in front of the Forge
    ax, ay = -1.66, -0.68
    g.cylinder('anvil_stump', (ax, ay, -0.08), (ax, ay, 0.28), 0.20, 'wood', segments=12)
    g.box('anvil_base', (ax, ay, 0.34), (0.28, 0.18, 0.14), 'iron', 0.008)
    g.box('anvil_top', (ax, ay, 0.46), (0.44, 0.16, 0.12), 'iron', 0.008)

    # Doors & Windows on the Main High-Bay Hall
    hall_front_y = hall_cy - hall_d / 2.0 - 0.02
    arch_window(g, 'guild_doors', (hall_cx, hall_front_y, 0.04), width=0.66, height=1.02, yaw=0.0, door=True, double_door=True)
    arch_window(g, 'guild_win_gable_hi', (hall_cx, hall_front_y - 0.03, hall_eave + 0.12), width=0.34, height=0.56, yaw=0.0)
    arch_window(g, 'guild_win_rear', (hall_cx, hall_cy + hall_d / 2.0 + 0.02, 0.30), width=0.38, height=0.66, yaw=math.pi)
    add_entrance_steps(g, 'guild_steps', hall_cx, hall_front_y, base_z=-0.01, width=0.90, steps=3)
    add_hanging_lanterns(g, 'guild_lantern', hall_cx, hall_front_y, z_bracket=1.12, span_x=0.46)
    return g


# =============================================================================
# SPECIALIZED BUILDING ASSEMBLIES FOR ZONE 06 (ENHANCEMENT SANCTUM & HUB_VILLAGE)
# =============================================================================

def build_enhancement_rune_sanctum() -> MapGeometry:
    """`Xưởng Cường Hóa & Khắc Phù Ấn` (Arcane Enhancement, Scroll & Rune Sanctum at operational slot (-1.8, 7.6)).

    Completely distinct Mythological Octagonal Stone-and-Bronze Sanctum:
    - 2-tier stepped octagonal carved stone plinth (`r_outer = 1.88m`, `r_inner = 1.56m`) with 8 radial stone buttress spurs
    - Octagonal ashlar stone sanctum drum (`radius = 1.28m, eave = 1.58m`) with arched glowing windows & double doors
    - 4 freestanding Carved Rune Monolith Pillars (`Trụ Đá Khắc Phù Văn`) at the 4 diagonal corners with glowing rune inlays & bronze braziers
    - 2-tiered octagonal royal-slate (`roof`) & bronze (`brass`) pagoda roof with 8 radiating bronze hip ribs
    - Rooftop levitating Soul-Seal Crystal Orb (`Tinh Cầu Mệnh Ấn`) surrounded by 2 intersecting brass armillary rings
    - Outdoor Arcane Workstations (`Bàn Gia Cường Giấy`, `Bàn Khắc Phù Văn`, `Bàn Khắc Mệnh Ấn`) on the front apron
    """
    g = MapGeometry()

    # 1. Stepped Octagonal Stone Plinth & 8 Radial Buttress Spurs
    closed_ring_solid(
        g,
        'sanctum_plinth_tier0',
        (0.0, 0.0, 0.0),
        [(-0.28, 1.94, 1.94), (0.14, 1.88, 1.88)],
        sides=8,
        material='stone',
        yaw=math.pi / 8.0,
    )
    closed_ring_solid(
        g,
        'sanctum_plinth_tier1',
        (0.0, 0.0, 0.0),
        [(0.12, 1.62, 1.62), (0.34, 1.56, 1.56)],
        sides=8,
        material='trim',
        yaw=math.pi / 8.0,
    )
    for i in range(8):
        ang = i * math.tau / 8.0
        ca, sa = math.cos(ang), math.sin(ang)
        rot_b = np.array([[ca, -sa, 0.0], [sa, ca, 0.0], [0.0, 0.0, 1.0]])
        g.box(f'sanctum_spur_{i}', (1.68 * ca, 1.68 * sa, 0.08), (0.38, 0.24, 0.36), 'stone', 0.014, rot_b)

    # 2. Octagonal Ashlar Stone Drum & Plaster Upper Frieze
    drum_z0 = 0.32
    drum_eave = 1.84
    closed_ring_solid(
        g,
        'sanctum_drum_core',
        (0.0, 0.0, 0.0),
        [(drum_z0 - 0.04, 1.28, 1.28), (1.26, 1.26, 1.26), (drum_eave, 1.24, 1.24)],
        sides=8,
        material='mortar',
        yaw=math.pi / 8.0,
    )
    closed_ring_solid(
        g,
        'sanctum_drum_frieze',
        (0.0, 0.0, 0.0),
        [(1.22, 1.28, 1.28), (drum_eave - 0.04, 1.28, 1.28)],
        sides=8,
        material='plaster',
        yaw=math.pi / 8.0,
    )
    closed_ring_solid(
        g,
        'sanctum_drum_cornice',
        (0.0, 0.0, 0.0),
        [(drum_eave - 0.06, 1.36, 1.36), (drum_eave + 0.04, 1.40, 1.40)],
        sides=8,
        material='brass',
        yaw=math.pi / 8.0,
    )

    # 8 Octagonal Corner Stone Pilasters around the Drum
    for i in range(8):
        ang = (i + 0.5) * math.tau / 8.0
        ca, sa = math.cos(ang), math.sin(ang)
        rot_p = np.array([[ca, -sa, 0.0], [sa, ca, 0.0], [0.0, 0.0, 1.0]])
        g.box(f'sanctum_pilaster_{i}', (1.26 * ca, 1.26 * sa, 1.08), (0.16, 0.16, 1.48), 'trim', 0.012, rot_p)

    # 3. Four Freestanding Carved Rune Monolith Pillars (`4 Trụ Đá Khắc Phù Văn`) at the Diagonal Corners
    for idx_m, ang in enumerate((0.25 * math.pi, 0.75 * math.pi, 1.25 * math.pi, 1.75 * math.pi)):
        mx, my = 1.66 * math.cos(ang), 1.66 * math.sin(ang)
        rot_m = np.array([[math.cos(ang), -math.sin(ang), 0.0], [math.sin(ang), math.cos(ang), 0.0], [0.0, 0.0, 1.0]])
        g.box(f'monolith_base_{idx_m}', (mx, my, 0.44), (0.36, 0.36, 0.28), 'stone', 0.016, rot_m)
        g.box(f'monolith_shaft_{idx_m}', (mx, my, 1.28), (0.26, 0.26, 1.44), 'stone', 0.016, rot_m)
        g.box(f'monolith_cap_{idx_m}', (mx, my, 2.04), (0.34, 0.34, 0.12), 'brass', 0.010, rot_m)
        # Glowing vertical rune channel on outer and inner faces of each monolith
        for sign_r in (-1, 1):
            rx = mx + sign_r * 0.125 * math.cos(ang)
            ry = my + sign_r * 0.125 * math.sin(ang)
            g.box(f'monolith_rune_{idx_m}_{sign_r}', (rx, ry, 1.30), (0.04, 0.09, 1.08), 'glass', 0.005, rot_m)
        # Top Bronze Flame Brazier & Glowing Orb on each Monolith
        g.cylinder(f'monolith_brazier_{idx_m}', (mx, my, 2.08), (mx, my, 2.26), 0.12, 'brass', end_radius=0.18, segments=12)
        g.cylinder(f'monolith_flame_{idx_m}', (mx, my, 2.22), (mx, my, 2.48), 0.11, 'glass', end_radius=0.02, segments=10)

    # 4. Two-Tiered Octagonal Pagoda/Dome Roof (`Mái Bát Giác 2 Tầng Đỉnh Đồng`)
    roof1_z0 = drum_eave + 0.02
    roof1_z1 = roof1_z0 + 0.78
    closed_ring_solid(
        g,
        'sanctum_roof_tier1',
        (0.0, 0.0, 0.0),
        [(roof1_z0, 1.56, 1.56), (roof1_z0 + 0.42, 1.12, 1.12), (roof1_z1, 0.74, 0.74)],
        sides=8,
        material='roof',
        yaw=math.pi / 8.0,
    )
    # Octagonal Upper Lantern Drum
    lan_z0 = roof1_z1 - 0.04
    lan_z1 = lan_z0 + 0.52
    closed_ring_solid(
        g,
        'sanctum_lantern_drum',
        (0.0, 0.0, 0.0),
        [(lan_z0, 0.72, 0.72), (lan_z1, 0.68, 0.68)],
        sides=8,
        material='plaster',
        yaw=math.pi / 8.0,
    )
    for i in range(8):
        ang = i * math.tau / 8.0
        ca, sa = math.cos(ang), math.sin(ang)
        rot_w = np.array([[ca, -sa, 0.0], [sa, ca, 0.0], [0.0, 0.0, 1.0]])
        g.box(f'sanctum_lantern_win_{i}', (0.64 * ca, 0.64 * sa, 0.5 * (lan_z0 + lan_z1)), (0.06, 0.18, 0.28), 'glass', 0.005, rot_w)

    # Upper Tier 2 Octagonal Roof + 8 Radiating Bronze Hip Ribs
    roof2_z0 = lan_z1 - 0.02
    roof2_z1 = roof2_z0 + 0.72
    closed_ring_solid(
        g,
        'sanctum_roof_tier2',
        (0.0, 0.0, 0.0),
        [(roof2_z0, 0.88, 0.88), (roof2_z0 + 0.38, 0.46, 0.46), (roof2_z1, 0.08, 0.08)],
        sides=8,
        material='roof',
        yaw=math.pi / 8.0,
    )
    for i in range(8):
        ang = (i + 0.5) * math.tau / 8.0
        ca, sa = math.cos(ang), math.sin(ang)
        g.beam(f'sanctum_rib_t1_{i}', (1.54 * ca, 1.54 * sa, roof1_z0 + 0.02), (0.72 * ca, 0.72 * sa, roof1_z1), 0.065, 'brass')
        g.beam(f'sanctum_rib_t2_{i}', (0.86 * ca, 0.86 * sa, roof2_z0 + 0.02), (0.08 * ca, 0.08 * sa, roof2_z1), 0.055, 'brass')

    # 5. Rooftop Levitating Soul-Seal Crystal Spire & Intersecting Brass Armillary Rings
    spire_z = roof2_z1 + 0.42
    g.cylinder('sanctum_spire_stem', (0.0, 0.0, roof2_z1 - 0.04), (0.0, 0.0, spire_z - 0.18), 0.08, 'brass', end_radius=0.04, segments=12)
    torus_ring(g, 'sanctum_armillary_1', (0.0, 0.0, spire_z), (1.0, 0.2, 0.3), 0.30, 0.024, 'brass', major_segments=24, minor_segments=8)
    torus_ring(g, 'sanctum_armillary_2', (0.0, 0.0, spire_z), (-0.2, 1.0, 0.3), 0.26, 0.022, 'brass', major_segments=24, minor_segments=8)
    closed_ring_solid(
        g,
        'sanctum_soul_seal_orb',
        (0.0, 0.0, spire_z),
        [(-0.18, 0.03, 0.03), (-0.07, 0.14, 0.14), (0.07, 0.14, 0.14), (0.18, 0.03, 0.03)],
        sides=10,
        material='glass',
    )

    # 6. Cardinal Arched Doors, Windows & Front Steps
    front_wall_y = -1.16
    arch_window(g, 'sanctum_door_south', (0.0, front_wall_y, drum_z0 + 0.02), width=0.64, height=1.02, yaw=0.0, door=True, double_door=True)
    arch_window(g, 'sanctum_win_west', (-1.16, 0.0, drum_z0 + 0.24), width=0.38, height=0.68, yaw=-math.pi / 2.0)
    arch_window(g, 'sanctum_win_east', ( 1.16, 0.0, drum_z0 + 0.24), width=0.38, height=0.68, yaw=math.pi / 2.0)
    arch_window(g, 'sanctum_win_north', (0.0, 1.16, drum_z0 + 0.24), width=0.38, height=0.68, yaw=math.pi)
    add_entrance_steps(g, 'sanctum_steps', 0.0, -1.52, base_z=0.22, width=0.96, steps=4)
    add_hanging_lanterns(g, 'sanctum_lantern', 0.0, front_wall_y, z_bracket=1.36, span_x=0.46)

    # 7. Outdoor Arcane Workstations (`Bàn Gia Cường Giấy` on West & `Bàn Khắc Phù Ấn` on East)
    # West: Scroll Enhancement Altar (`Bàn Gia Cường Giấy +1..+3`)
    sx_w, sy_w = -1.28, -1.48
    g.box('altar_scroll_base', (sx_w, sy_w, 0.16), (0.64, 0.46, 0.36), 'stone', 0.014)
    g.box('altar_scroll_top', (sx_w, sy_w, 0.38), (0.72, 0.52, 0.08), 'trim', 0.010)
    g.box('altar_scroll_glow', (sx_w, sy_w, 0.44), (0.48, 0.32, 0.04), 'glass', 0.005)
    # East: Rune & Soul-Seal Carving Anvil (`Bàn Khắc Phù Văn & Mệnh Ấn`)
    sx_e, sy_e = 1.28, -1.48
    g.cylinder('altar_rune_ped', (sx_e, sy_e, 0.02), (sx_e, sy_e, 0.38), 0.30, 'stone', segments=8)
    torus_ring(g, 'altar_rune_ring', (sx_e, sy_e, 0.40), (0.0, 0.0, 1.0), 0.26, 0.024, 'brass', major_segments=16, minor_segments=6)
    g.cylinder('altar_rune_core', (sx_e, sy_e, 0.36), (sx_e, sy_e, 0.46), 0.20, 'glass', segments=12)
    return g


def build_transition_gatehouse_arch() -> MapGeometry:
    """`Cổng Sau Trang Trại & Đoạn Chuyển Map` (Twin-Pillar Stone Arch Gatehouse separating Farm_Core from Hub_Village)."""
    g = MapGeometry()
    span_x = 1.08
    pil_h = 1.86
    for side_idx, sx in enumerate((-span_x, span_x)):
        g.box(f'gate_plinth_{side_idx}', (sx, 0.0, 0.06), (0.52, 0.52, 0.32), 'stone', 0.018)
        g.box(f'gate_pillar_{side_idx}', (sx, 0.0, 0.96), (0.40, 0.40, pil_h), 'stone', 0.016)
        g.box(f'gate_cap_{side_idx}', (sx, 0.0, pil_h + 0.06), (0.50, 0.50, 0.12), 'trim', 0.012)
    # Overhead Timber & Stone Lintel + Pitched Gatehouse Roof
    g.beam('gate_lintel_lo', (-span_x - 0.18, 0.0, pil_h - 0.08), (span_x + 0.18, 0.0, pil_h - 0.08), 0.14, 'wood')
    g.beam('gate_lintel_hi', (-span_x - 0.24, 0.0, pil_h + 0.16), (span_x + 0.24, 0.0, pil_h + 0.16), 0.16, 'wood')
    for side_x in (-1, 1):
        g.beam(f'gate_arch_brace_{side_x}', (side_x * (span_x - 0.16), 0.0, pil_h - 0.52), (side_x * 0.42, 0.0, pil_h - 0.08), 0.09, 'wood')
    tiled_roof(g, 'gate_roof', 0.0, 0.0, 2.0 * span_x + 0.68, 0.84, pil_h + 0.18, 0.52, ridge_x=True, roof_mat='terracotta')
    add_hanging_lanterns(g, 'gate_lantern', 0.0, -0.22, z_bracket=1.42, span_x=span_x - 0.04)
    return g


def build_ba_sau_tea_house() -> MapGeometry:
    """`Nhà Bà Sáu & Quán Trà Đầu Làng` on the Elevated Hub_Village Terrace."""
    g = MapGeometry()
    main_w, main_d, main_eave = 2.16, 1.58, 1.42
    wing_w, wing_d, wing_eave = 1.06, 1.16, 1.44
    main_cx, main_cy = 0.0, 0.18
    wing_cx, wing_cy = -0.40, -0.48

    masonry_block(g, 'basau_main', main_cx, main_cy, main_w, main_d, eave=main_eave)
    masonry_block(g, 'basau_wing', wing_cx, wing_cy, wing_w, wing_d, eave=wing_eave)

    def under_wing(p):
        return abs(p[0] - wing_cx) < (wing_w * 0.56) and p[1] < 0.18 and p[2] < wing_eave + 0.90 * (1.0 - abs(p[0] - wing_cx) / (wing_w * 0.62))

    tiled_roof(g, 'basau_main_roof', main_cx, main_cy, main_w + 0.22, main_d + 0.22, main_eave + 0.01, 1.02, ridge_x=True, roof_mat='terracotta', occluders=(under_wing,))
    tiled_roof(g, 'basau_wing_roof', wing_cx, wing_cy, wing_w + 0.20, wing_d + 0.20, wing_eave + 0.01, 0.90, ridge_x=False, roof_mat='terracotta')

    # Front Tea Veranda Awning & Herb-Drying Rack
    tiled_roof(g, 'basau_tea_awning', 0.48, -0.82, 1.04, 0.54, 0.96, 0.30, ridge_x=True, roof_mat='terracotta')
    for px in (0.04, 0.92):
        g.box(f'basau_awning_post_{px:+.2f}', (px, -0.98, 0.48), (0.09, 0.09, 0.96), 'wood', 0.008)
    g.box('basau_tea_table', (0.48, -0.84, 0.24), (0.52, 0.36, 0.32), 'wood', 0.010)

    wing_front_y = wing_cy - wing_d / 2.0 - 0.02
    arch_window(g, 'basau_door', (wing_cx, wing_front_y, 0.04), width=0.54, height=0.92, yaw=0.0, door=True, double_door=False)
    arch_window(g, 'basau_win_front', (0.48, main_cy - main_d / 2.0 - 0.02, 0.26), width=0.34, height=0.60, yaw=0.0)
    arch_window(g, 'basau_win_west', (main_cx - main_w / 2.0 - 0.02, 0.16, 0.26), width=0.32, height=0.58, yaw=-math.pi / 2.0)
    arch_window(g, 'basau_win_east', (main_cx + main_w / 2.0 + 0.02, 0.16, 0.26), width=0.32, height=0.58, yaw=math.pi / 2.0)
    add_entrance_steps(g, 'basau_steps', wing_cx, wing_front_y, base_z=-0.01, width=0.80, steps=3)
    add_hanging_lanterns(g, 'basau_lantern', wing_cx, wing_front_y, z_bracket=1.04, span_x=0.40)
    add_stone_chimney(g, 'basau_chimney', 0.68, 0.56, main_eave + 0.52, courses=6, course_mat='brick')
    return g


def build_village_guild_and_raven_tower() -> MapGeometry:
    """`Hội Quán Làng & Tháp Chuông Quạ Tin` on the Elevated Hub_Village Terrace.

    Features a two-story stone-and-timber village guildhall paired with a tall 3-story square
    stone Bell & Raven Watchtower (`Tháp Chuông Quạ Tin`, height = 3.55m above the terrace)
    with arched belfry openings, a bronze bell, and a steep pyramidal spire.
    """
    g = MapGeometry()
    main_w, main_d, main_eave = 2.28, 1.66, 1.52
    main_cx, main_cy = -0.34, 0.16
    masonry_block(g, 'vguild_main', main_cx, main_cy, main_w, main_d, eave=main_eave, stone_rows=6)
    tiled_roof(g, 'vguild_main_roof', main_cx, main_cy, main_w + 0.24, main_d + 0.24, main_eave + 0.01, 1.06, ridge_x=True, roof_mat='terracotta')
    add_roof_dormer(g, 'vguild_dormer', -0.42, -0.34, main_eave + 0.32, width=0.52, depth=0.46, wall_h=0.36, rise=0.26, roof_mat='terracotta')

    # Attached 3-Story Square Stone Bell & Raven Tower (`Tháp Chuông Quạ Tin`) on the East Side
    tw_cx, tw_cy = 1.08, -0.18
    tw_w = 1.08
    tw_h = 2.68
    masonry_block(g, 'raven_tower_shaft', tw_cx, tw_cy, tw_w, tw_w, eave=tw_h, stone_rows=10, timber_braces=False)
    g.box('raven_tower_belt', (tw_cx, tw_cy, 1.68), (tw_w + 0.14, tw_w + 0.14, 0.10), 'trim', 0.012)

    # Open Belfry Lantern Stage with 4 Corner Stone Piers & Suspended Bronze Bell
    belfry_z0 = tw_h
    belfry_h = 0.62
    for sx in (-0.40, 0.40):
        for sy in (-0.40, 0.40):
            g.box(f'raven_belfry_pier_{sx:+.2f}_{sy:+.2f}', (tw_cx + sx, tw_cy + sy, belfry_z0 + belfry_h * 0.5), (0.20, 0.20, belfry_h), 'stone', 0.012)
    g.cylinder('raven_bell', (tw_cx, tw_cy, belfry_z0 + 0.16), (tw_cx, tw_cy, belfry_z0 + 0.48), 0.24, 'brass', end_radius=0.10, segments=16)

    # Steep Pyramidal Slate-and-Terracotta Spire + Brass Weathervane Finial
    spire_z0 = belfry_z0 + belfry_h
    spire_h = 1.18
    closed_ring_solid(
        g,
        'raven_tower_spire',
        (tw_cx, tw_cy, 0.0),
        [(spire_z0 - 0.02, 0.88, 0.88), (spire_z0 + spire_h * 0.55, 0.44, 0.44), (spire_z0 + spire_h, 0.05, 0.05)],
        sides=4,
        material='roof',
        yaw=math.pi / 4.0,
    )
    g.cylinder('raven_spire_finial', (tw_cx, tw_cy, spire_z0 + spire_h - 0.02), (tw_cx, tw_cy, spire_z0 + spire_h + 0.32), 0.045, 'brass', end_radius=0.018, segments=12)

    front_y = main_cy - main_d / 2.0 - 0.02
    arch_window(g, 'vguild_door', (-0.38, front_y, 0.04), width=0.58, height=0.96, yaw=0.0, door=True, double_door=True)
    arch_window(g, 'raven_tower_win_lo', (tw_cx, tw_cy - tw_w / 2.0 - 0.02, 0.36), width=0.30, height=0.58, yaw=0.0)
    arch_window(g, 'raven_tower_win_hi', (tw_cx, tw_cy - tw_w / 2.0 - 0.02, 1.82), width=0.32, height=0.62, yaw=0.0)
    add_entrance_steps(g, 'vguild_steps', -0.38, front_y, base_z=-0.01, width=0.86, steps=3)
    add_hanging_lanterns(g, 'vguild_lantern', -0.38, front_y, z_bracket=1.08, span_x=0.44)
    return g


def build_village_cottage(
    tag: str,
    main_w: float,
    main_d: float,
    wing_w: float,
    wing_d: float,
    roof_mat: str = 'terracotta',
    wing_on_right: bool = False,
) -> MapGeometry:
    """High-detail closed-manifold rustic village cottage for Zone06 (`Nhà Ông Lẫm`)."""
    g = MapGeometry()
    main_eave = 1.40
    wing_eave = 1.42
    main_cx, main_cy = 0.0, 0.20
    wing_sign = 1.0 if wing_on_right else -1.0
    wing_cx = wing_sign * (main_w * 0.20)
    wing_cy = -0.50

    masonry_block(g, f'{tag}_main', main_cx, main_cy, main_w, main_d, eave=main_eave)
    masonry_block(g, f'{tag}_wing', wing_cx, wing_cy, wing_w, wing_d, eave=wing_eave)

    def under_wing(p):
        return abs(p[0] - wing_cx) < (wing_w * 0.56) and p[1] < 0.18 and p[2] < wing_eave + 0.90 * (1.0 - abs(p[0] - wing_cx) / (wing_w * 0.62))

    tiled_roof(g, f'{tag}_main_roof', main_cx, main_cy, main_w + 0.22, main_d + 0.22, main_eave + 0.01, 0.98, ridge_x=True, roof_mat=roof_mat, occluders=(under_wing,))
    tiled_roof(g, f'{tag}_wing_roof', wing_cx, wing_cy, wing_w + 0.20, wing_d + 0.20, wing_eave + 0.01, 0.88, ridge_x=False, roof_mat=roof_mat)

    dormer_cx = -wing_sign * (main_w * 0.24)
    add_roof_dormer(g, f'{tag}_dormer', dormer_cx, -0.32, main_eave + 0.30, width=0.50, depth=0.44, wall_h=0.36, rise=0.25, roof_mat=roof_mat)

    wing_front_y = wing_cy - wing_d / 2.0 - 0.02
    arch_window(g, f'{tag}_door', (wing_cx, wing_front_y, 0.04), width=0.54, height=0.92, yaw=0.0, door=True, double_door=False)
    arch_window(g, f'{tag}_win_wing_gable', (wing_cx, wing_front_y - 0.03, wing_eave + 0.06), width=0.28, height=0.46, yaw=0.0)
    arch_window(g, f'{tag}_win_front', (dormer_cx, main_cy - main_d / 2.0 - 0.02, 0.26), width=0.34, height=0.60, yaw=0.0)
    arch_window(g, f'{tag}_win_west_1', (main_cx - main_w / 2.0 - 0.02, -0.12, 0.26), width=0.32, height=0.58, yaw=-math.pi / 2.0)
    arch_window(g, f'{tag}_win_west_2', (main_cx - main_w / 2.0 - 0.02, 0.54, 0.26), width=0.32, height=0.58, yaw=-math.pi / 2.0)
    arch_window(g, f'{tag}_win_east_1', (main_cx + main_w / 2.0 + 0.02, -0.12, 0.26), width=0.32, height=0.58, yaw=math.pi / 2.0)
    arch_window(g, f'{tag}_win_east_2', (main_cx + main_w / 2.0 + 0.02, 0.54, 0.26), width=0.32, height=0.58, yaw=math.pi / 2.0)
    arch_window(g, f'{tag}_win_rear_1', (-0.46, main_cy + main_d / 2.0 + 0.02, 0.26), width=0.32, height=0.58, yaw=math.pi)
    arch_window(g, f'{tag}_win_rear_2', (0.46, main_cy + main_d / 2.0 + 0.02, 0.26), width=0.32, height=0.58, yaw=math.pi)

    add_entrance_steps(g, f'{tag}_steps', wing_cx, wing_front_y, base_z=-0.01, width=0.80, steps=3)
    add_hanging_lanterns(g, f'{tag}_lantern', wing_cx, wing_front_y, z_bracket=1.04, span_x=0.40)
    add_stone_chimney(g, f'{tag}_chimney', -wing_sign * (main_w * 0.32), 0.60, main_eave + 0.50, courses=6, course_mat='stone')
    return g


# =============================================================================
# FENCES, FARMSTEAD PROPS, VILLAGE SIGNPOST & TREES
# =============================================================================

def add_split_rail_fence(
    g: MapGeometry,
    name: str,
    x0: float,
    y0: float,
    x1: float,
    y1: float,
    n_posts: int = 7,
):
    """Split-rail wooden fence with stone post bases, beveled timber posts, caps, and double horizontal rails."""
    yaw = math.atan2(y1 - y0, x1 - x0)
    c, s = math.cos(yaw), math.sin(yaw)
    rot_mat = np.array([[c, -s, 0.0], [s, c, 0.0], [0.0, 0.0, 1.0]])

    pts = []
    for i in range(n_posts):
        t = i / float(max(1, n_posts - 1))
        px = x0 + (x1 - x0) * t
        py = y0 + (y1 - y0) * t
        pz = float(sample_ground_z(px, py))
        pts.append((px, py, pz))

        g.box(f'{name}_post_base_{i}', (px, py, pz - 0.05), (0.20, 0.20, 0.22), 'stone', 0.018, rot_mat)
        g.box(f'{name}_post_trim_{i}', (px, py, pz + 0.07), (0.17, 0.17, 0.06), 'trim', 0.010, rot_mat)
        g.box(f'{name}_post_{i}', (px, py, pz + 0.34), (0.12, 0.12, 0.56), 'wood', 0.012, rot_mat)
        g.box(f'{name}_post_cap_{i}', (px, py, pz + 0.64), (0.15, 0.15, 0.065), 'brass' if i % 2 == 0 else 'trim', 0.010, rot_mat)

    for i in range(n_posts - 1):
        p_a = pts[i]
        p_b = pts[i + 1]
        for r_idx, z_off in enumerate((0.24, 0.46)):
            a = (p_a[0], p_a[1], p_a[2] + z_off)
            b = (p_b[0], p_b[1], p_b[2] + z_off)
            g.beam(f'{name}_rail_{i}_{r_idx}', a, b, 0.065, 'wood', depth=0.052)


def build_feed_trough_and_crate() -> MapGeometry:
    """Closed-solid 3D wooden feed trough and framed grain crate at (-16.0, 4.8)."""
    g = MapGeometry()
    for sx in (-0.34, 0.34):
        g.box(f'trough_leg_{sx:+.2f}', (sx, 0.0, 0.12), (0.09, 0.38, 0.32), 'stone', 0.012)
    g.box('trough_bottom', (0.0, 0.0, 0.22), (0.86, 0.34, 0.065), 'wood', 0.008)
    for sy in (-0.16, 0.16):
        g.box(f'trough_side_{sy:+.2f}', (0.0, sy, 0.32), (0.88, 0.055, 0.20), 'wood', 0.008)
    for sx in (-0.41, 0.41):
        g.box(f'trough_end_{sx:+.2f}', (sx, 0.0, 0.32), (0.06, 0.36, 0.20), 'wood', 0.008)
        g.box(f'trough_iron_strap_{sx:+.2f}', (sx, 0.0, 0.32), (0.075, 0.39, 0.045), 'iron', 0.006)
    g.box('trough_grain_fill', (0.0, 0.0, 0.34), (0.76, 0.26, 0.12), 'grass_gold', 0.025)

    cx, cy = 0.64, 0.18
    g.box('crate_core', (cx, cy, 0.20), (0.42, 0.42, 0.44), 'wood', 0.012)
    for sx in (-0.19, 0.19):
        for sy in (-0.19, 0.19):
            g.box(f'crate_post_{sx:+.2f}_{sy:+.2f}', (cx + sx, cy + sy, 0.21), (0.065, 0.065, 0.46), 'trim', 0.008)
    for z_band in (0.04, 0.38):
        g.box(f'crate_band_{z_band:.2f}', (cx, cy, z_band), (0.45, 0.45, 0.055), 'wood', 0.008)
    g.beam('crate_brace_f', (cx - 0.18, cy - 0.22, 0.06), (cx + 0.18, cy - 0.22, 0.36), 0.045, 'wood')
    g.beam('crate_brace_b', (cx - 0.18, cy + 0.22, 0.06), (cx + 0.18, cy + 0.22, 0.36), 0.045, 'wood')
    return g


def build_bound_oak_barrels() -> MapGeometry:
    """Cluster of 3 bound oak barrels with iron/brass hoop bands at (-16.6, 5.1)."""
    g = MapGeometry()
    barrel_locs = [
        (-0.18, -0.26, 0.0, 1.00),
        ( 0.24, -0.32, 0.0, 0.92),
        ( 0.04,  0.06, 0.0, 0.88),
    ]
    for idx, (bx, by, bz, sc) in enumerate(barrel_locs):
        r_mid = 0.20 * sc
        r_end = 0.165 * sc
        h = 0.48 * sc
        profile = [
            (-0.04 * sc, r_end, r_end),
            (0.10 * sc, r_mid, r_mid),
            (0.24 * sc, r_mid * 1.04, r_mid * 1.04),
            (0.38 * sc, r_mid, r_mid),
            (h, r_end, r_end),
        ]
        closed_ring_solid(g, f'barrel_drum_{idx}', (bx, by, bz), profile, sides=14, material='wood')
        g.cylinder(f'barrel_top_rim_{idx}', (bx, by, bz + h - 0.02 * sc), (bx, by, bz + h + 0.025 * sc), r_end * 0.92, 'trim', segments=14)
        for h_i, z_h in enumerate((0.06 * sc, 0.24 * sc, 0.42 * sc)):
            r_hoop = r_mid * (1.03 if h_i == 1 else 0.96)
            torus_ring(
                g,
                f'barrel_hoop_{idx}_{h_i}',
                (bx, by, bz + z_h),
                (0.0, 0.0, 1.0),
                r_hoop,
                0.018 * sc,
                'brass' if h_i == 1 else 'iron',
                major_segments=14,
                minor_segments=6,
            )
    return g


def build_wooden_hay_cart() -> MapGeometry:
    """Detailed closed-solid 3D wooden hay cart with spoked wheels, rails, shafts, and golden hay bales at (-14.5, 6.2)."""
    g = MapGeometry()
    wheel_r = 0.30
    wheel_z = 0.26
    g.cylinder('cart_axle', (0.0, -0.44, wheel_z), (0.0, 0.44, wheel_z), 0.042, 'iron', segments=14)
    g.box('cart_bolster', (0.0, 0.0, wheel_z + 0.05), (0.28, 0.68, 0.09), 'wood', 0.010)

    for side_idx, wy in enumerate((-0.38, 0.38)):
        torus_ring(g, f'cart_wheel_rim_{side_idx}', (0.0, wy, wheel_z), (0.0, 1.0, 0.0), wheel_r, 0.032, 'iron', major_segments=18, minor_segments=6)
        torus_ring(g, f'cart_wheel_felloe_{side_idx}', (0.0, wy, wheel_z), (0.0, 1.0, 0.0), wheel_r - 0.024, 0.028, 'wood', major_segments=18, minor_segments=6)
        g.cylinder(f'cart_wheel_hub_{side_idx}', (0.0, wy - 0.055, wheel_z), (0.0, wy + 0.055, wheel_z), 0.072, 'brass', segments=14)
        for sp in range(8):
            ang = sp * math.tau / 8.0
            p0 = (0.0, wy, wheel_z)
            p1 = ((wheel_r - 0.025) * math.cos(ang), wy, wheel_z + (wheel_r - 0.025) * math.sin(ang))
            g.beam(f'cart_spoke_{side_idx}_{sp}', p0, p1, 0.030, 'wood')

    bed_z = wheel_z + 0.11
    g.box('cart_bed', (0.0, 0.0, bed_z), (0.98, 0.66, 0.075), 'wood', 0.010)
    for sy in (-0.30, 0.30):
        for sx in (-0.42, -0.14, 0.14, 0.42):
            g.box(f'cart_stake_{sx:+.2f}_{sy:+.2f}', (sx, sy, bed_z + 0.16), (0.052, 0.052, 0.32), 'wood', 0.006)
        g.beam(f'cart_rail_lo_{sy:+.2f}', (-0.46, sy, bed_z + 0.14), (0.46, sy, bed_z + 0.14), 0.048, 'wood')
        g.beam(f'cart_rail_hi_{sy:+.2f}', (-0.46, sy, bed_z + 0.28), (0.46, sy, bed_z + 0.28), 0.048, 'wood')
        g.beam(f'cart_shaft_{sy:+.2f}', (-0.44, sy * 0.78, bed_z - 0.01), (-1.02, sy * 0.62, 0.08), 0.052, 'wood')
    g.beam('cart_shaft_crossbar', (-0.88, -0.22, 0.12), (-0.88, 0.22, 0.12), 0.046, 'wood')
    g.box('cart_shaft_rest_block', (-0.98, 0.0, 0.02), (0.16, 0.46, 0.12), 'stone', 0.012)

    bale_specs = [
        (-0.18, -0.10, bed_z + 0.16, 0.42, 0.28, 0.24, 'grass_gold'),
        ( 0.20,  0.08, bed_z + 0.16, 0.42, 0.30, 0.24, 'grass_gold'),
        ( 0.02, -0.02, bed_z + 0.36, 0.46, 0.32, 0.22, 'grass_gold'),
    ]
    for b_idx, (bx, by, bz, bsx, bsy, bsz, bmat) in enumerate(bale_specs):
        g.box(f'cart_hay_bale_{b_idx}', (bx, by, bz), (bsx, bsy, bsz), bmat, 0.032)
        g.box(f'cart_hay_twine_{b_idx}', (bx, by, bz), (0.035, bsy + 0.015, bsz + 0.015), 'wood', 0.005)
    return g


def build_village_signpost_and_well() -> MapGeometry:
    """Cobblestone village signpost with directional boards, hanging lantern, and ancient stone well."""
    g = MapGeometry()
    g.box('sign_plinth_0', (0.0, 0.0, -0.06), (0.54, 0.54, 0.24), 'stone', 0.022)
    g.box('sign_plinth_1', (0.0, 0.0, 0.12), (0.38, 0.38, 0.18), 'trim', 0.016)
    for i, ang in enumerate(np.linspace(0.0, math.tau, 8, endpoint=False)):
        g.box(f'sign_cobble_{i}', (0.22 * math.cos(ang), 0.22 * math.sin(ang), 0.02), (0.13, 0.13, 0.14), 'stone', 0.018)

    g.box('sign_post', (0.0, 0.0, 0.78), (0.13, 0.13, 1.24), 'wood', 0.014)
    g.box('sign_cap', (0.0, 0.0, 1.42), (0.19, 0.19, 0.065), 'trim', 0.010)
    g.cylinder('sign_finial', (0.0, 0.0, 1.45), (0.0, 0.0, 1.60), 0.050, 'brass', end_radius=0.020, segments=14)

    for idx, (z_b, yaw_b, length_b) in enumerate(((1.12, 0.35, 0.42), (1.26, -2.65, 0.40))):
        c, s = math.cos(yaw_b), math.sin(yaw_b)
        rot_b = np.array([[c, -s, 0.0], [s, c, 0.0], [0.0, 0.0, 1.0]])
        cx_b = c * (length_b * 0.5 + 0.04)
        cy_b = s * (length_b * 0.5 + 0.04)
        g.box(f'sign_board_{idx}', (cx_b, cy_b, z_b), (length_b, 0.048, 0.14), 'wood', 0.008, rot_b)
        g.box(f'sign_strap_{idx}', (c * 0.07, s * 0.07, z_b), (0.085, 0.065, 0.16), 'iron', 0.006, rot_b)

    g.beam('sign_lantern_arm', (0.0, -0.05, 0.98), (0.0, -0.30, 0.98), 0.032, 'iron')
    g.beam('sign_lantern_brace', (0.0, -0.05, 0.85), (0.0, -0.22, 0.98), 0.024, 'iron')
    g.box('sign_lantern_glass', (0.0, -0.28, 0.83), (0.11, 0.11, 0.17), 'glass', 0.010)
    for z_c in (0.73, 0.92):
        g.box(f'sign_lantern_cap_{z_c:.2f}', (0.0, -0.28, z_c), (0.14, 0.14, 0.036), 'brass', 0.006)

    # Ancient Stone Well & Timber Canopy (`Giếng Đá Cổ`) beside the Notice Post
    wx, wy = 0.82, 0.12
    g.cylinder('well_plinth', (wx, wy, -0.06), (wx, wy, 0.12), 0.54, 'stone', segments=16)
    g.cylinder('well_curb', (wx, wy, 0.10), (wx, wy, 0.52), 0.42, 'stone', segments=16)
    g.cylinder('well_rim', (wx, wy, 0.48), (wx, wy, 0.58), 0.46, 'trim', segments=16)
    for sx in (-0.36, 0.36):
        g.box(f'well_post_{sx:+.2f}', (wx + sx, wy, 0.92), (0.09, 0.09, 0.78), 'wood', 0.008)
    tiled_roof(g, 'well_roof', wx, wy, 0.98, 0.82, 1.28, 0.36, ridge_x=True, roof_mat='terracotta')
    return g


# =============================================================================
# CLOSED-MANIFOLD AUTUMN OAK & 6-TIER SCALLOPED PINE TREES
# =============================================================================

_STRIP3_FOLIAGE_UV = {
    'oak_gold':   (3, 0.37, 0.76),
    'oak_amber':  (3, 0.44, 0.58),
    'oak_orange': (3, 0.63, 0.68),
    'pine_green': (3, 0.15, 0.62),
    'pine_dark':  (3, 0.10, 0.36),
}


def _remap_last_part_foliage_uv(g: MapGeometry, mat: str, v_jitter: float = 0.0):
    """Maps foliage materials onto their exact Golden Oak / Amber Maple / Spruce sub-strips in Strip 3."""
    if mat not in _STRIP3_FOLIAGE_UV or not g.parts:
        return
    strip, u, v = _STRIP3_FOLIAGE_UV[mat]
    v = float(np.clip(v + v_jitter, 0.08, 0.90))
    patch = np.array([[u - 0.008, v - 0.012], [u + 0.008, v - 0.012], [u, v + 0.012]], dtype=float)
    patch[:, 0] = (strip + 0.03 + 0.94 * patch[:, 0]) / 8.0
    patch[:, 1] = 0.006 + 0.238 * patch[:, 1]
    last_part = g.parts[-1]
    v_start = last_part['first_triangle'] * 3
    for t_i in range(last_part['triangles']):
        idx = v_start + t_i * 3
        g.uvs[idx] = patch[0]
        g.uvs[idx + 1] = patch[1]
        g.uvs[idx + 2] = patch[2]


def add_autumn_oak_tree(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    scale: float = 1.0,
    palette_idx: int = 0,
    seed: float = 0.0,
):
    """High-detail multi-cluster autumn oak tree (trunk + branches + 12 beveled faceted foliage polyhedra)."""
    pal_cycle = [
        ('oak_gold', 'oak_amber', 'oak_gold'),
        ('oak_amber', 'oak_orange', 'oak_gold'),
        ('oak_orange', 'oak_amber', 'oak_gold'),
    ][palette_idx % 3]

    g.cylinder(f'{name}_root_flare', (cx, cy, z0 - 0.18 * scale), (cx, cy, z0 + 0.24 * scale), 0.27 * scale, 'wood', end_radius=0.18 * scale, segments=10)
    g.cylinder(f'{name}_trunk', (cx, cy, z0 + 0.20 * scale), (cx, cy, z0 + 1.08 * scale), 0.18 * scale, 'wood', end_radius=0.12 * scale, segments=10)

    for b_i in range(4):
        b_ang = seed * 1.3 + b_i * (math.tau / 4.0) + 0.25
        bx1 = cx + 0.46 * scale * math.cos(b_ang)
        by1 = cy + 0.46 * scale * math.sin(b_ang)
        bz1 = z0 + (1.05 + 0.08 * (b_i % 2)) * scale
        g.beam(f'{name}_branch_{b_i}', (cx, cy, z0 + 0.64 * scale), (bx1, by1, bz1), 0.095 * scale, 'wood')

    bough_specs = [
        (-0.52, -0.34, 0.96, 0.68, 0.64, 0.48, 0),
        (-0.44,  0.42, 1.00, 0.66, 0.64, 0.50, 1),
        ( 0.04, -0.54, 1.02, 0.66, 0.62, 0.50, 0),
        ( 0.06,  0.52, 1.06, 0.64, 0.66, 0.52, 1),
        ( 0.50, -0.26, 1.10, 0.68, 0.64, 0.52, 0),
        ( 0.46,  0.32, 1.14, 0.66, 0.64, 0.52, 1),
        (-0.34,  0.02, 1.42, 0.70, 0.66, 0.54, 1),
        ( 0.34, -0.04, 1.46, 0.70, 0.66, 0.56, 2),
        ( 0.00, -0.28, 1.50, 0.64, 0.62, 0.52, 2),
        ( 0.02,  0.28, 1.54, 0.64, 0.62, 0.52, 1),
        (-0.14, -0.08, 1.78, 0.60, 0.58, 0.48, 2),
        ( 0.12,  0.08, 1.88, 0.58, 0.56, 0.46, 2),
    ]
    for i, (dx, dy, dz, rx, ry, rz, mat_slot) in enumerate(bough_specs):
        ang = seed * 1.7 + i * 0.58
        ca, sa = math.cos(ang), math.sin(ang)
        bcx = cx + (dx * ca - dy * sa) * scale
        bcy = cy + (dx * sa + dy * ca) * scale
        bcz = z0 + dz * scale
        rx_s, ry_s, rz_s = rx * scale, ry * scale, rz * scale
        ring_profiles = [
            (-0.88 * rz_s, 0.42 * rx_s, 0.42 * ry_s),
            (-0.38 * rz_s, 0.88 * rx_s, 0.88 * ry_s),
            ( 0.14 * rz_s, 1.00 * rx_s, 1.00 * ry_s),
            ( 0.62 * rz_s, 0.80 * rx_s, 0.80 * ry_s),
            ( 0.94 * rz_s, 0.38 * rx_s, 0.38 * ry_s),
        ]
        mat_name = pal_cycle[mat_slot]
        closed_ring_solid(
            g,
            f'{name}_foliage_{i}',
            (bcx, bcy, bcz),
            ring_profiles,
            sides=9,
            material=mat_name,
            yaw=ang,
            lobe_amp=0.08,
            lobe_freq=3,
            phase=seed + i * 0.7,
        )
        _remap_last_part_foliage_uv(g, mat_name, v_jitter=(i - 5.5) * 0.018)


def add_scalloped_pine_tree(
    g: MapGeometry,
    name: str,
    cx: float,
    cy: float,
    z0: float,
    scale: float = 1.0,
    tiers: int = 6,
    seed: float = 0.0,
):
    """6-tier scalloped Highland spruce/pine tree built from closed manifold scalloped cones."""
    g.cylinder(f'{name}_trunk', (cx, cy, z0 - 0.16 * scale), (cx, cy, z0 + 0.68 * scale), 0.18 * scale, 'wood', end_radius=0.09 * scale, segments=8)
    for t_i in range(tiers):
        tz0 = z0 + (0.22 + t_i * 0.38) * scale
        th = (0.86 - 0.08 * t_i) * scale
        r_bot = (0.98 - 0.125 * t_i) * scale
        r_top = max(0.06 * scale, (0.22 - 0.03 * t_i) * scale)
        mat = 'pine_dark' if t_i == 0 else 'pine_green'
        ring_profiles = [
            (-0.05 * scale, r_bot * 0.44, r_bot * 0.44),
            (0.02 * scale, r_bot, r_bot),
            (th * 0.55, (r_bot + r_top) * 0.48, (r_bot + r_top) * 0.48),
            (th, r_top, r_top),
        ]
        closed_ring_solid(
            g,
            f'{name}_tier_{t_i}',
            (cx, cy, tz0),
            ring_profiles,
            sides=12,
            material=mat,
            yaw=seed + t_i * 0.26,
            scallop_ring_idx=1,
            scallop_amp=0.16,
            scallop_droop=0.05 * scale,
        )
        _remap_last_part_foliage_uv(g, mat, v_jitter=t_i * 0.025)


# =============================================================================
# PUBLIC ZONE BUILDERS
# =============================================================================

def build_zone04_farmstead() -> MapGeometry:
    """Builds `Zone04_Building_KhuChuongTrai_Farmstead` in canonical world coordinates."""
    zone = MapGeometry()

    # 1. Three Distinct Farm_Core Functional Complexes at canonical coordinates
    # (a) `Nhà Chính & Đài Điều Khiển Bạch Vân` (Main Homestead + Brick Feed Stove + Bạch Vân Array)
    hx1, hy1, hyaw1 = -17.8, 5.4, 0.14
    hz1 = float(sample_ground_z(hx1, hy1))
    zone.merge_from(build_main_barn_farmhouse(), origin=(hx1, hy1, hz1), yaw=hyaw1)

    # (b) `Nhà Kho` (Flagship Tier 3 Twin-Silo Logistics Stronghold; Tiers 1/2/3 also exported separately)
    hx2, hy2, hyaw2 = -17.0, 9.2, 0.08
    hz2 = float(sample_ground_z(hx2, hy2))
    zone.merge_from(build_storage_granary_tier3(), origin=(hx2, hy2, hz2), yaw=hyaw2)

    # (c) `Phòng Xây Dựng` (High-Bay Workshop + Open Blueprint Bay + Giant A-Frame Crane + Twin-Stack Forge)
    hx3, hy3, hyaw3 = -13.4, 9.4, -0.06
    hz3 = float(sample_ground_z(hx3, hy3))
    zone.merge_from(build_construction_guild_hall(), origin=(hx3, hy3, hz3), yaw=hyaw3)

    # 2. Split-rail wooden fences along (-20.2, 3.6) -> (-14.2, 3.8) and (-10.8, 4.2) -> (-4.8, 4.2)
    add_split_rail_fence(zone, 'farm_fence_west', -20.2, 3.6, -14.2, 3.8, n_posts=7)
    add_split_rail_fence(zone, 'farm_fence_east', -10.8, 4.2, -4.8, 4.2, n_posts=7)

    # 3. Closed-solid 3D wooden feed troughs, bound oak barrels, grain crates, and wooden hay cart
    p1_x, p1_y = -16.0, 4.8
    zone.merge_from(build_feed_trough_and_crate(), origin=(p1_x, p1_y, float(sample_ground_z(p1_x, p1_y))), yaw=0.45)

    p2_x, p2_y = -16.6, 5.1
    zone.merge_from(build_bound_oak_barrels(), origin=(p2_x, p2_y, float(sample_ground_z(p2_x, p2_y))), yaw=0.15)

    p3_x, p3_y = -14.5, 6.2
    zone.merge_from(build_wooden_hay_cart(), origin=(p3_x, p3_y, float(sample_ground_z(p3_x, p3_y))), yaw=-0.30)

    # 4. ~45 High-detail multi-cluster autumn oak trees & 6-tier scalloped pine trees in y <= 10.2 around Zone04
    building_keepouts = [
        (-17.8, 5.4, 3.15),
        (-17.0, 9.2, 3.55),
        (-13.4, 9.4, 3.35),
        (-7.8, 6.4, 3.25),
        (-1.8, 7.6, 3.10),
        (8.6, 4.6, 2.50),
        (-16.0, 4.8, 1.35),
        (-16.6, 5.1, 1.25),
        (-14.5, 6.2, 1.45),
    ]
    placed_trees = []

    def can_place_farm_tree(tx: float, ty: float, min_d: float = 1.30) -> bool:
        if ty > 10.2:
            return False
        if island_signed_distance(tx, ty) > 0.925:
            return False
        if float(eval_road_mask(tx, ty)) >= 0.25:
            return False
        if -20.6 <= tx <= -13.8 and 3.0 <= ty <= 4.4:
            return False
        if -11.2 <= tx <= -4.4 and 3.6 <= ty <= 4.8:
            return False
        if -19.8 <= tx <= -10.2 and 1.6 <= ty <= 9.2:
            return False
        for bx, by, br in building_keepouts:
            if math.hypot(tx - bx, ty - by) < br:
                return False
        for px, py in placed_trees:
            if math.hypot(tx - px, ty - py) < min_d:
                return False
        return True

    rng = np.random.RandomState(42)
    candidate_coords = [
        (-22.8, 4.8), (-22.2, 6.4), (-21.8, 8.0), (-21.2, 9.5),
        (-20.8, 6.8), (-20.6, 8.5), (-20.1, 10.0), (-11.2, 10.0),
        (-10.2, 9.1), (-10.0, 7.5), (-23.6, 3.2), (-22.6, 2.2),
        (-21.4, 1.8), (-20.2, 2.2), (-24.2, 5.6), (-23.5, 7.4),
        (-22.8, 9.1), (-21.5, 5.2), (-20.2, 0.8), (-21.6, 0.4),
        (-23.0, 0.8), (-24.2, 2.0),
    ]
    for _ in range(360):
        tx = float(rng.uniform(-25.4, -9.2))
        ty = float(rng.uniform(-1.6, 10.2))
        if ty < 3.6 and tx > -19.8:
            continue
        candidate_coords.append((tx, ty))

    for idx_t, (tx, ty) in enumerate(candidate_coords):
        if len(placed_trees) >= 42:
            break
        if not can_place_farm_tree(tx, ty, min_d=1.30):
            continue
        placed_trees.append((tx, ty))
        tz = float(sample_ground_z(tx, ty))
        is_pine = (rng.rand() < 0.28)
        if is_pine:
            sc = float(rng.uniform(0.80, 1.14))
            add_scalloped_pine_tree(zone, f'farm_pine_{len(placed_trees)}', tx, ty, tz, scale=sc, tiers=6, seed=idx_t * 0.73)
        else:
            sc = float(rng.uniform(0.84, 1.16))
            pal_idx = len(placed_trees) % 3
            add_autumn_oak_tree(zone, f'farm_oak_{len(placed_trees)}', tx, ty, tz, scale=sc, palette_idx=pal_idx, seed=idx_t * 1.13)

    return zone


def build_zone06_npc_village() -> MapGeometry:
    """Builds `Zone06_Building_NhaCuongHoa_And_KhuNPC` in canonical world coordinates.

    Strictly separates:
    1. Operational Core `Xưởng Cường Hóa & Khắc Phù Ấn` (Octagonal Arcane Enhancement & Rune Sanctum)
       at (-1.8, 7.6) alongside `Zone05` (`Phòng Nghiên Cứu`).
    2. `Đoạn Chuyển Map` (Rear Farmstead Boundary Wall, Twin-Pillar Stone Arch Gatehouse & Stepped Mountain Trail)
       spanning y = 9.0..11.2.
    3. Distant Elevated `Khu Làng NPC Ngoại Vi` (`Hub_Village`) pushed far back to `y in [11.2, 14.6]`
       on a Raised Stone Highland Terrace Bastion (`z_terrace = ground + 1.35m` -> `z ~ 5.25m`),
       completely separated in both depth (Y) and elevation (Z) from the pig meadow!
    """
    zone = MapGeometry()

    # 1. Operational Core Building: `Xưởng Cường Hóa & Khắc Phù Ấn` at (-1.8, 7.6)
    ex, ey, eyaw = -1.8, 7.6, -0.12
    ez = float(sample_ground_z(ex, ey))
    zone.merge_from(build_enhancement_rune_sanctum(), origin=(ex, ey, ez), yaw=eyaw)

    # 2. Rear Farmstead Boundary Fence + `Cổng Sau Trang Trại` (`Đoạn Chuyển Map` Gatehouse) at (0.6, 9.2)
    add_split_rail_fence(zone, 'rear_boundary_fence_w', -4.2, 9.1, -0.7, 9.15, n_posts=5)
    add_split_rail_fence(zone, 'rear_boundary_fence_e',  1.9, 9.15,  4.8, 9.0,  n_posts=5)
    gx_gate, gy_gate = 0.6, 9.15
    gz_gate = float(sample_ground_z(gx_gate, gy_gate))
    zone.merge_from(build_transition_gatehouse_arch(), origin=(gx_gate, gy_gate, gz_gate), yaw=-0.06)

    # 3. Elevated Rear Highland Terrace Bastion (`Bậc Thềm Đá Cao Nguyên Khu Làng NPC`) at y in [11.1, 14.6]
    terrace_cx, terrace_cy = 1.0, 12.85
    terrace_w, terrace_d = 7.80, 3.60
    base_ground_z = float(sample_ground_z(terrace_cx, terrace_cy))
    terrace_top_z = base_ground_z + 1.35

    # Massive stepped ashlar stone retaining bastion anchoring deep into the northern slope
    zone.box('npc_terrace_subbase', (terrace_cx, terrace_cy, base_ground_z + 0.15), (terrace_w + 0.48, terrace_d + 0.48, 1.40), 'stone', 0.035)
    zone.box('npc_terrace_upper_wall', (terrace_cx, terrace_cy, base_ground_z + 0.88), (terrace_w + 0.16, terrace_d + 0.16, 0.92), 'mortar', 0.025)
    zone.box('npc_terrace_deck', (terrace_cx, terrace_cy, terrace_top_z - 0.04), (terrace_w + 0.28, terrace_d + 0.28, 0.16), 'trim', 0.018)

    # 7 Heavy Stone Counterfort Buttresses along the Front Retaining Wall (`y = 11.0`)
    front_wall_y = terrace_cy - terrace_d * 0.5 - 0.08
    for b_idx, bx in enumerate(np.linspace(terrace_cx - 3.4, terrace_cx + 3.4, 7)):
        if abs(bx - 0.65) < 0.75:
            continue  # Leave central stair bay open
        zone.box(f'npc_terrace_buttress_{b_idx}', (float(bx), front_wall_y, base_ground_z + 0.55), (0.38, 0.34, 1.52), 'stone', 0.018)
        zone.box(f'npc_terrace_buttress_cap_{b_idx}', (float(bx), front_wall_y, terrace_top_z - 0.02), (0.44, 0.40, 0.12), 'trim', 0.012)

    # Front Stone Parapet Balustrade on Left and Right of the Grand Stairway
    for side_tag, x_start, x_end in (('west', terrace_cx - 3.6, -0.25), ('east', 1.55, terrace_cx + 3.6)):
        mid_x = 0.5 * (x_start + x_end)
        span = abs(x_end - x_start)
        zone.box(f'npc_parapet_wall_{side_tag}', (mid_x, front_wall_y + 0.12, terrace_top_z + 0.18), (span, 0.22, 0.32), 'stone', 0.014)
        zone.box(f'npc_parapet_coping_{side_tag}', (mid_x, front_wall_y + 0.12, terrace_top_z + 0.36), (span + 0.06, 0.28, 0.08), 'trim', 0.010)

    # Grand Stepped Stone Mountain Stairway (`Đoạn Chuyển Map — Bậc Thang Đá Lên Làng NPC`) from y = 9.55 to y = 11.15
    stair_x = 0.65
    y_bot, y_top = 9.55, 11.15
    z_bot = float(sample_ground_z(stair_x, y_bot))
    n_steps = 9
    for s_i in range(n_steps):
        t = (s_i + 0.5) / n_steps
        sy = y_bot + (y_top - y_bot) * t
        sz = z_bot + (terrace_top_z - z_bot) * t
        zone.box(f'npc_trail_step_{s_i}', (stair_x, sy, sz - 0.12), (1.36, 0.26, 0.28), 'trim', 0.014)
    for side_s, sx_wall in (('w', stair_x - 0.78), ('e', stair_x + 0.78)):
        zone.beam(f'npc_stair_cheek_{side_s}', (sx_wall, y_bot, z_bot + 0.18), (sx_wall, y_top, terrace_top_z + 0.28), 0.22, 'stone', depth=0.36)

    # 4. Three Distinct `Hub_Village` Buildings + Village Notice Board & Stone Well on the Elevated Terrace
    # (a) `Nhà Bà Sáu & Quán Trà Đầu Làng` at (-1.55, 12.35, terrace_top_z)
    zone.merge_from(build_ba_sau_tea_house(), origin=(-1.55, 12.35, terrace_top_z), yaw=0.14)

    # (b) `Hội Quán Làng & Tháp Chuông Quạ Tin` at (1.05, 13.45, terrace_top_z)
    zone.merge_from(build_village_guild_and_raven_tower(), origin=(1.05, 13.45, terrace_top_z), yaw=-0.06)

    # (c) `Nhà Ông Lẫm` at (3.35, 12.25, terrace_top_z)
    zone.merge_from(
        build_village_cottage('onglam_house', 2.04, 1.52, 1.00, 1.10, roof_mat='terracotta', wing_on_right=True),
        origin=(3.35, 12.25, terrace_top_z),
        yaw=-0.22,
    )

    # (d) `Bảng Tin Làng & Giếng Đá Cổ` on the Elevated Village Plaza at (0.55, 11.75, terrace_top_z)
    zone.merge_from(build_village_signpost_and_well(), origin=(0.55, 11.75, terrace_top_z), yaw=-0.12)

    return zone
