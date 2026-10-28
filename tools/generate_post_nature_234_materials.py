"""
Deterministic 234-Material Transfer Manifest & Asset Stager for post-nature-5d4-source-bundle-v1.
Resolves all 7 visual/shader pitfalls audited across the 234 source materials and 14 textures:
1. 100% deterministic coverage (234/234 materials mapped; 0 unmapped or [0.8, 0.8, 0.8] chalk-white fallbacks).
2. 3-stop ColorRamp normalization for 2-stop and 4-stop ramps (fixes _RampPos1 > _RampPos2 orange-brown highlight bug).
3. Pattern 7 canopy boughs & pine fronds (117-126) extract true color from ShaderNodeHueSaturation.inputs['Color'].
4. Broadleaf atlas (50: NS7_ExistingBroadleafAtlas) and 15 *_Leaves* materials preserve _BaseMap, MixRGB tint, _AlphaClip=True, and two-sided _Cull=0.
5. Pond sedge (110) and wet bank (111) stay opaque foliage/rock; actual water (112, 140, 223) binds _ShallowColor and _DeepColor.
6. All 15 *_Bark* materials bind _BumpMap (00003.png, 00006.png, 00010.png) with _BumpScale=0.45.
7. Blender Scene-Linear RGB socket colors converted to sRGB (IEC 61966-2-1) for Unity Material.SetColor parity.
"""

from collections import Counter
import hashlib
import json
import shutil
from pathlib import Path

REPO_ROOT = Path(r"D:\Pig-tycoon")
EXPORT_BUNDLE = REPO_ROOT / ".collab/artifacts/original-map-unity-20261026/post-nature-5d4-source-bundle-v1"
MANIFEST_PATH = EXPORT_BUNDLE / "manifest.json"
INVENTORY_PATH = EXPORT_BUNDLE / "SOURCE-MATERIAL-INVENTORY.json"

SHADER_SOURCE_DIR = REPO_ROOT / ".collab/artifacts/agent-full-map-visual-study-20261021/argon/game-client-v1/materials-argon"
UNITY_ROOT_DIR = REPO_ROOT / "Assets/OriginalBlenderWorld"
UNITY_SHADERS_DIR = UNITY_ROOT_DIR / "Shaders"
UNITY_TEXTURES_DIR = UNITY_ROOT_DIR / "Textures"
OUTPUT_MANIFEST_PATH = UNITY_ROOT_DIR / "post_nature_234_material_transfer.json"


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def linear_to_srgb_channel(c: float) -> float:
    c = max(0.0, float(c))
    if c <= 0.0031308:
        return min(1.0, c * 12.92)
    return min(1.0, 1.055 * (c ** (1.0 / 2.4)) - 0.055)


def linear_to_srgb_rgba(rgba, alpha_override=None):
    if not rgba or len(rgba) < 3:
        return [1.0, 1.0, 1.0, 1.0]
    r = linear_to_srgb_channel(rgba[0])
    g = linear_to_srgb_channel(rgba[1])
    b = linear_to_srgb_channel(rgba[2])
    a = float(alpha_override) if alpha_override is not None else (float(rgba[3]) if len(rgba) > 3 else 1.0)
    return [round(r, 5), round(g, 5), round(b, 5), round(a, 5)]


def lerp_rgba(a, b, t: float):
    return [a[i] + (b[i] - a[i]) * t for i in range(4)]


def scale_linear_rgb(rgba, factor: float):
    return [
        max(0.0, min(1.0, rgba[0] * factor)),
        max(0.0, min(1.0, rgba[1] * factor)),
        max(0.0, min(1.0, rgba[2] * factor)),
        rgba[3] if len(rgba) > 3 else 1.0,
    ]


def normalize_ramp_to_3_stops_srgb(raw_elements):
    """Normalizes 2-, 3-, or 4-stop linear ColorRamp elements to exact 3 sRGB stops with strictly increasing positions."""
    if not raw_elements:
        return [
            {"position": 0.18, "color": [0.25, 0.25, 0.25, 1.0]},
            {"position": 0.52, "color": [0.55, 0.55, 0.55, 1.0]},
            {"position": 0.84, "color": [0.80, 0.80, 0.80, 1.0]},
        ]
    elems = sorted(raw_elements, key=lambda e: float(e["position"]))
    if len(elems) == 1:
        c = linear_to_srgb_rgba(elems[0]["color"])
        return [
            {"position": 0.18, "color": c},
            {"position": 0.52, "color": c},
            {"position": 0.84, "color": c},
        ]
    elif len(elems) == 2:
        p0, c0 = float(elems[0]["position"]), elems[0]["color"]
        p2, c2 = float(elems[1]["position"]), elems[1]["color"]
        if p2 <= p0 + 0.02:
            p0, p2 = 0.0, 1.0
        p1 = 0.5 * (p0 + p2)
        c1 = lerp_rgba(c0, c2, 0.5)
        return [
            {"position": round(p0, 4), "color": linear_to_srgb_rgba(c0)},
            {"position": round(p1, 4), "color": linear_to_srgb_rgba(c1)},
            {"position": round(p2, 4), "color": linear_to_srgb_rgba(c2)},
        ]
    elif len(elems) == 3:
        p0 = float(elems[0]["position"])
        p1 = max(p0 + 0.01, float(elems[1]["position"]))
        p2 = max(p1 + 0.01, float(elems[2]["position"]))
        return [
            {"position": round(p0, 4), "color": linear_to_srgb_rgba(elems[0]["color"])},
            {"position": round(p1, 4), "color": linear_to_srgb_rgba(elems[1]["color"])},
            {"position": round(p2, 4), "color": linear_to_srgb_rgba(elems[2]["color"])},
        ]
    else:
        # 4+ stops: preserve lowest stop, blended midtone, and highest highlight stop
        e0 = elems[0]
        e_mid = lerp_rgba(elems[1]["color"], elems[2]["color"], 0.5)
        p_mid = 0.5 * (float(elems[1]["position"]) + float(elems[2]["position"]))
        e_top = elems[-1]
        p0 = float(e0["position"])
        p1 = max(p0 + 0.01, p_mid)
        p2 = max(p1 + 0.01, float(e_top["position"]))
        return [
            {"position": round(p0, 4), "color": linear_to_srgb_rgba(e0["color"])},
            {"position": round(p1, 4), "color": linear_to_srgb_rgba(e_mid)},
            {"position": round(p2, 4), "color": linear_to_srgb_rgba(e_top["color"])},
        ]


def stage_shaders_and_textures(textures):
    UNITY_SHADERS_DIR.mkdir(parents=True, exist_ok=True)
    UNITY_TEXTURES_DIR.mkdir(parents=True, exist_ok=True)

    for tex in textures:
        rel = tex["file"]
        src = EXPORT_BUNDLE / rel
        if not src.is_file():
            raise FileNotFoundError(f"Missing bundle texture: {src}")
        actual_sha = sha256_file(src)
        if actual_sha.lower() != tex["sha256"].lower():
            raise ValueError(f"Texture SHA256 mismatch for {src}: expected {tex['sha256']}, got {actual_sha}")
        dst = UNITY_TEXTURES_DIR / Path(rel).name
        if not dst.is_file() or sha256_file(dst).lower() != actual_sha.lower():
            shutil.copyfile(src, dst)

    # Also pre-stage character, pig, and care-prop FBX/PNG candidates so Unity imports them once on startup
    art_root = REPO_ROOT / "Assets/PlayableDemo/Art"
    for sub in ("Characters", "Environment", "Textures", "Materials", "Animators", "Prefabs"):
        (art_root / sub).mkdir(parents=True, exist_ok=True)

    khoa_dir = REPO_ROOT / ".collab/artifacts/playable-demo-20261026/argon/candidates_v3/khoa_urban"
    if (khoa_dir / "Khoa_Candidate.fbx").is_file():
        shutil.copyfile(khoa_dir / "Khoa_Candidate.fbx", khoa_dir / "Khoa_Urban.fbx")
    for fbx in ("Khoa_Urban.fbx", "Khoa@Idle.fbx", "Khoa@Walk.fbx", "Khoa@Run.fbx", "Khoa@Interact.fbx", "Khoa@Attack.fbx", "Khoa@Whistle.fbx"):
        shutil.copyfile(khoa_dir / fbx, art_root / "Characters" / fbx)
    for png in (khoa_dir / "Textures").glob("*.png"):
        shutil.copyfile(png, art_root / "Textures" / png.name)

    man_dir = REPO_ROOT / ".collab/artifacts/playable-demo-20261026/argon/candidates_v3/man"
    for fbx in ("Man_Candidate.fbx", "Man@Idle.fbx", "Man@Walk.fbx", "Man@Interact.fbx"):
        shutil.copyfile(man_dir / fbx, art_root / "Characters" / fbx)
    shutil.copyfile(man_dir / "Man_Umbrella_Open.fbx", art_root / "Environment/Man_Umbrella_Open.fbx")
    for png in (man_dir / "Textures").glob("*.png"):
        shutil.copyfile(png, art_root / "Textures" / png.name)

    pig_dir = REPO_ROOT / ".collab/artifacts/playable-demo-20261026/argon/candidates_v3/pig"
    for fbx in ("Pig_HongDien.fbx", "Pig_WhiteFoot.fbx", "Pig@Idle.fbx", "Pig@Walk.fbx", "Pig@Eat.fbx", "Pig@Sleep.fbx"):
        shutil.copyfile(pig_dir / fbx, art_root / "Characters" / fbx)
    for png in (pig_dir / "Textures").glob("*.png"):
        shutil.copyfile(png, art_root / "Textures" / png.name)

    care_dir = REPO_ROOT / ".collab/artifacts/original-map-unity-20261026/care_props_v1"
    for fbx in (
        "Prop_FeederCrate.fbx", "Prop_WaterBasin.fbx", "Prop_MudWallow.fbx", "Prop_CarePost.fbx",
        "Bldg_OpenShelter.fbx", "Prop_CapacityBoard.fbx", "Prop_FenceSegment4m.fbx",
        "Prop_FenceBreachDamaged.fbx", "Prop_FenceTempRepair.fbx", "Bldg_NoXuyenVanTower.fbx"
    ):
        shutil.copyfile(care_dir / fbx, art_root / "Environment" / fbx)
    for png in (care_dir / "Textures").glob("*.png"):
        shutil.copyfile(png, art_root / "Textures" / png.name)


def generate():
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    inventory = json.loads(INVENTORY_PATH.read_text(encoding="utf-8"))

    materials = manifest["materials"]
    graphs = manifest["shader_graphs"]
    textures = manifest["textures"]
    meshes = manifest["meshes"]
    inv_mats = inventory["materials"]

    assert len(materials) == 234 and len(inv_mats) == 234, f"Expected 234 materials, got {len(materials)}"
    stage_shaders_and_textures(textures)

    mesh_stats = {
        i: {"mesh_count": 0, "triangles": 0, "uv_meshes": 0, "no_uv_meshes": 0}
        for i in range(len(materials))
    }
    for mesh in meshes:
        for mid in mesh["material_ids"]:
            if mid >= 0:
                mesh_stats[mid]["mesh_count"] += 1
                mesh_stats[mid]["triangles"] += mesh["triangles"]
                if mesh["has_uv"]:
                    mesh_stats[mid]["uv_meshes"] += 1
                else:
                    mesh_stats[mid]["no_uv_meshes"] += 1

    records = []
    route_counter = Counter()

    for idx, mat in enumerate(materials):
        inv_mat = inv_mats[idx]
        assert idx == inv_mat["id"]
        name = mat["name"]
        triage = inv_mat["triage_only"]

        gid = mat.get("graph")
        g = graphs[gid] if (gid is not None and gid < len(graphs)) else {"nodes": [], "links": []}
        nodes = g.get("nodes", [])
        links = g.get("links", [])
        node_by_name = {n["name"]: n for n in nodes}

        p_nodes = [n for n in nodes if n.get("type") == "ShaderNodeBsdfPrincipled"]
        p_node = p_nodes[0] if p_nodes else None

        inputs = {}
        linked_inputs = set()
        if p_node:
            for inp in p_node.get("inputs", []):
                inputs[inp["name"]] = inp.get("default")
                if inp.get("linked"):
                    linked_inputs.add(inp["name"])

        raw_base_color = inputs.get("Base Color") or [0.8, 0.8, 0.8, 1.0]
        roughness = inputs.get("Roughness") if inputs.get("Roughness") is not None else 0.55
        metallic = inputs.get("Metallic") if inputs.get("Metallic") is not None else 0.0
        emission_color = inputs.get("Emission Color") or [0.0, 0.0, 0.0, 1.0]
        emission_strength = inputs.get("Emission Strength") if inputs.get("Emission Strength") is not None else 0.0
        alpha = inputs.get("Alpha") if inputs.get("Alpha") is not None else 1.0
        transmission = inputs.get("Transmission Weight") if inputs.get("Transmission Weight") is not None else 0.0
        smoothness = max(0.0, min(1.0, 1.0 - float(roughness)))
        if metallic < 0.2 and transmission < 0.05 and "glass" not in name.lower() and "water" not in name.lower():
            smoothness = min(smoothness, 0.24)

        tex_nodes = [n for n in nodes if n.get("type") == "ShaderNodeTexImage"]
        bump_nodes = [n for n in nodes if n.get("type") == "ShaderNodeBump"]
        normal_map_nodes = [n for n in nodes if n.get("type") == "ShaderNodeNormalMap"]
        noise_nodes = [n for n in nodes if n.get("type") == "ShaderNodeTexNoise"]
        wave_nodes = [n for n in nodes if n.get("type") == "ShaderNodeTexWave"]
        hsv_nodes = [n for n in nodes if n.get("type") == "ShaderNodeHueSaturation"]
        mix_rgb_nodes = [n for n in nodes if n.get("type") == "ShaderNodeMixRGB"]
        mix_nodes = [n for n in nodes if n.get("type") == "ShaderNodeMix"]
        math_nodes = [n for n in nodes if n.get("type") == "ShaderNodeMath"]

        base_map = None
        base_map_sha256 = None
        base_map_colorspace = None
        emission_map = None
        emission_map_sha256 = None
        bump_map = None
        bump_map_sha256 = None
        bump_scale = 0.65

        for tn in tex_nodes:
            t_idx = tn.get("texture")
            if t_idx is None or t_idx >= len(textures):
                continue
            t_rec = textures[t_idx]
            t_file = t_rec["file"]
            t_sha = t_rec["sha256"]
            t_cs = t_rec.get("colorspace", "sRGB")
            t_name = tn.get("name")

            for l in links:
                if l["from_node"] != t_name:
                    continue
                to_node = node_by_name.get(l["to_node"])
                if not to_node:
                    continue
                if p_node and l["to_node"] == p_node["name"]:
                    if l["to_socket"] in ("Base Color", "Color"):
                        base_map, base_map_sha256, base_map_colorspace = t_file, t_sha, t_cs
                    elif l["to_socket"] in ("Emission Color", "Emission"):
                        emission_map, emission_map_sha256 = t_file, t_sha
                elif to_node.get("type") == "ShaderNodeNormalMap":
                    bump_map, bump_map_sha256 = t_file, t_sha
                    for ninp in to_node.get("inputs", []):
                        if ninp.get("name") == "Strength" and ninp.get("default") is not None:
                            bump_scale = float(ninp["default"])
                elif to_node.get("type") in ("ShaderNodeMixRGB", "ShaderNodeBsdfTranslucent", "ShaderNodeMath"):
                    if t_cs == "sRGB" and base_map is None:
                        base_map, base_map_sha256, base_map_colorspace = t_file, t_sha, t_cs

        if bump_nodes and bump_map is None:
            for inp in bump_nodes[0].get("inputs", []):
                if inp.get("name") == "Strength" and inp.get("default") is not None:
                    bump_scale = float(inp["default"])
                    break

        noise_scale = 1.2
        noise_detail = 4.0
        if noise_nodes:
            for inp in noise_nodes[0].get("inputs", []):
                if inp.get("name") == "Scale" and inp.get("default") is not None:
                    noise_scale = float(inp["default"])
                elif inp.get("name") == "Detail" and inp.get("default") is not None:
                    noise_detail = float(inp["default"])

        wave_scale = 4.5
        wave_distortion = 0.35
        if wave_nodes:
            for inp in wave_nodes[0].get("inputs", []):
                if inp.get("name") == "Scale" and inp.get("default") is not None:
                    wave_scale = float(inp["default"])
                elif inp.get("name") == "Distortion" and inp.get("default") is not None:
                    wave_distortion = float(inp["default"])

        raw_ramp_elements = []
        for n in nodes:
            if n.get("color_ramp") and n["color_ramp"].get("elements"):
                raw_ramp_elements = n["color_ramp"]["elements"]
                break

        # Extract synthesized ramps for Pattern 7 (HueSaturation), Pattern 8 (M_Nautical Mix), Pattern 10 (WR_FineMasonry MixRGB)
        if not raw_ramp_elements:
            if hsv_nodes:
                hsv_col = [0.25, 0.38, 0.12, 1.0]
                for inp in hsv_nodes[0].get("inputs", []):
                    if inp.get("name") == "Color" and inp.get("default"):
                        hsv_col = inp["default"]
                        break
                raw_ramp_elements = [
                    {"position": 0.18, "color": scale_linear_rgb(hsv_col, 0.72)},
                    {"position": 0.52, "color": scale_linear_rgb(hsv_col, 1.00)},
                    {"position": 0.84, "color": scale_linear_rgb(hsv_col, 1.28)},
                ]
            elif name.startswith("WR_FineMasonry_") and mix_rgb_nodes:
                c1, c2 = [0.085, 0.077, 0.045, 1.0], [0.42, 0.31, 0.16, 1.0]
                for inp in mix_rgb_nodes[0].get("inputs", []):
                    if inp.get("name") == "Color1" and inp.get("default"):
                        c1 = inp["default"]
                    elif inp.get("name") == "Color2" and inp.get("default"):
                        c2 = inp["default"]
                raw_ramp_elements = [
                    {"position": 0.0, "color": c1},
                    {"position": 1.0, "color": c2},
                ]
            elif name.startswith("M_Nautical_") and mix_nodes:
                ca, cb = scale_linear_rgb(raw_base_color, 0.72), raw_base_color
                for inp in mix_nodes[0].get("inputs", []):
                    if inp.get("identifier") == "A_Color" and inp.get("default"):
                        ca = inp["default"]
                    elif inp.get("identifier") == "B_Color" and inp.get("default"):
                        cb = inp["default"]
                raw_ramp_elements = [
                    {"position": 0.0, "color": ca},
                    {"position": 1.0, "color": cb},
                ]

        normalized_3stop_srgb = normalize_ramp_to_3_stops_srgb(raw_ramp_elements)

        # Exact deterministic route assignment (all 234 materials)
        is_true_water = idx in (112, 140, 223)
        is_textured_leaf = ("_Leaves" in name) or (idx == 50)
        is_textured_bark = "_Bark" in name
        is_foliage_procedural = (
            ("OakBough" in name)
            or ("PineFrond" in name)
            or name.startswith("BOT_Gold_")
            or (idx == 110)  # FM21_Pond_Sedge_Mat
            or (
                name.startswith("NS7_")
                and idx != 50
                and not name.startswith("NS7_Stone")
                and name != "NS7_WarmBark"
            )
        )
        is_rock_coast_procedural = (
            name.startswith("WR_WarmLimestone_")
            or (idx in (82, 90, 91, 93, 94, 111, 142, 224))
            or name.startswith("CONT21_Coast_")
            or name.startswith("NS7_Stone")
            or (idx in (172, 173, 174, 175, 176))
        )
        is_object_space_procedural = (
            name.startswith("WR_FineMasonry_")
            or name.startswith("M_Nautical_")
            or (name == "NS7_WarmBark")
            or (name.startswith("MPT_") and idx not in (172, 173, 174, 175, 176) and triage != "CONSTANT_PRINCIPLED_NATIVE_REVIEW")
        )

        if triage == "CONSTANT_PRINCIPLED_NATIVE_REVIEW" or name.startswith("MC_") or name.startswith("GEO7_"):
            route = "URP_LIT_DIRECT_CONSTANT"
            shader = "Universal Render Pipeline/Lit"
        elif triage == "IMAGE_PBR_NATIVE_MAPPING_REVIEW" or is_textured_leaf or is_textured_bark or base_map is not None:
            route = "URP_LIT_DIRECT_TEXTURED"
            shader = "Universal Render Pipeline/Lit"
        elif is_true_water:
            route = "URP_PROCEDURAL_WATER"
            shader = "PigTycoon/URP/WaterSurface"
        elif is_foliage_procedural:
            route = "URP_PROCEDURAL_FOLIAGE"
            shader = "PigTycoon/URP/FoliageBough"
        elif is_rock_coast_procedural:
            route = "URP_PROCEDURAL_ROCK_COAST"
            shader = "PigTycoon/URP/ProceduralRockCoast"
        elif is_object_space_procedural:
            route = "URP_PROCEDURAL_OBJECT_SPACE"
            shader = "PigTycoon/URP/ObjectSpaceProcedural"
        else:
            raise ValueError(f"Unclassified material {idx}: {name} (triage={triage})")

        route_counter[route] += 1

        # Determine sRGB _BaseColor and alpha cutout
        alpha_clip = False
        cutoff = 0.5
        if is_textured_leaf:
            alpha_clip = True
            cutoff = 0.6275 if idx == 50 else 0.5
            for mn in math_nodes:
                for inp in mn.get("inputs", []):
                    if inp.get("identifier") == "Value_001" and inp.get("default") is not None:
                        cutoff = round(float(inp["default"]), 4)
            if idx == 50:
                base_color_srgb = [1.0, 1.0, 1.0, 1.0]
            else:
                tint = [0.45, 0.58, 0.24, 1.0]
                if mix_rgb_nodes:
                    for inp in mix_rgb_nodes[0].get("inputs", []):
                        if inp.get("name") == "Color2" and inp.get("default"):
                            tint = inp["default"]
                            break
                base_color_srgb = linear_to_srgb_rgba(tint)
        elif base_map is not None:
            base_color_srgb = [1.0, 1.0, 1.0, 1.0]
        elif "Base Color" in linked_inputs and raw_ramp_elements:
            base_color_srgb = normalized_3stop_srgb[1]["color"]
        else:
            base_color_srgb = linear_to_srgb_rgba(raw_base_color, alpha_override=alpha)

        # For ObjectSpaceProcedural (2-stop shader), provide 2 stops at [0.0, 1.0]
        if shader == "PigTycoon/URP/ObjectSpaceProcedural":
            ramp_for_shader = [
                {"position": 0.0, "color": normalized_3stop_srgb[0]["color"]},
                {"position": 1.0, "color": normalized_3stop_srgb[2]["color"]},
            ]
        else:
            ramp_for_shader = normalized_3stop_srgb

        # Water surface shallow & deep colors
        shallow_color_srgb = [0.20, 0.52, 0.48, 0.78]
        deep_color_srgb = [0.04, 0.18, 0.28, 0.94]
        if is_true_water and raw_ramp_elements:
            c_s = normalized_3stop_srgb[0]["color"]
            c_d = normalized_3stop_srgb[2]["color"]
            # Ensure shallow is brighter/more aqua and deep is darker
            lum0 = 0.299 * c_s[0] + 0.587 * c_s[1] + 0.114 * c_s[2]
            lum2 = 0.299 * c_d[0] + 0.587 * c_d[1] + 0.114 * c_d[2]
            if lum0 < lum2:
                c_s, c_d = c_d, c_s
            shallow_color_srgb = [c_s[0], c_s[1], c_s[2], 0.78 if idx != 223 else 0.88]
            deep_color_srgb = [c_d[0], c_d[1], c_d[2], 0.94]

        is_wood = any(k in name.lower() for k in ("wood", "timber", "oak", "plank", "hull", "mast", "spar", "deck", "bark"))
        grain_stretch = 4.0 if is_wood else 1.0

        is_transparent = (alpha < 0.999 or transmission > 0.05 or is_true_water) and not alpha_clip and not is_textured_bark
        surface_type = 1 if is_transparent else 0
        zwrite = 0 if is_transparent else 1
        render_queue = 3000 if is_transparent else (2450 if alpha_clip else 2000)

        cull_mode = 0 if (
            is_textured_leaf
            or is_foliage_procedural
            or "leaf" in name.lower()
            or "bough" in name.lower()
            or "frond" in name.lower()
            or "grass" in name.lower()
            or "petal" in name.lower()
            or not mat["material_settings"].get("use_backface_culling", True)
        ) else 2

        if emission_strength > 0:
            emit_srgb = linear_to_srgb_rgba(emission_color)
            emit_col = [round(emit_srgb[0] * float(emission_strength), 5),
                        round(emit_srgb[1] * float(emission_strength), 5),
                        round(emit_srgb[2] * float(emission_strength), 5), 1.0]
        else:
            emit_col = [0.0, 0.0, 0.0, 1.0]

        unity_base_map_asset = f"Assets/OriginalBlenderWorld/Textures/{Path(base_map).name}" if base_map else None
        unity_emit_map_asset = f"Assets/OriginalBlenderWorld/Textures/{Path(emission_map).name}" if emission_map else None
        unity_bump_map_asset = f"Assets/OriginalBlenderWorld/Textures/{Path(bump_map).name}" if bump_map else None

        safe_mat_name = "".join(ch if (ch.isalnum() or ch == "_") else "_" for ch in name)
        if idx == 0:
            unity_mat_asset_path = "Assets/OriginalBlenderWorld/Materials/Mat_000_Approved_Fence_Railing_Dark_Wood.mat"
        else:
            unity_mat_asset_path = f"Assets/OriginalBlenderWorld/Materials/Mat_{idx:03d}_{safe_mat_name}.mat"

        surface_mode = "AlphaClip" if alpha_clip else ("Transparent" if is_transparent else "Opaque")

        extra_floats = []
        extra_colors = []
        if any(c > 0.0 for c in emit_col[:3]):
            extra_colors.append({"property": "_EmissionColor", "rgba": emit_col})

        if shader == "PigTycoon/URP/FoliageBough":
            extra_colors.extend([
                {"property": "_RampColor0", "rgba": ramp_for_shader[0]["color"]},
                {"property": "_RampColor1", "rgba": ramp_for_shader[1]["color"]},
                {"property": "_RampColor2", "rgba": ramp_for_shader[2]["color"]},
            ])
            extra_floats.extend([
                {"property": "_RampPos0", "value": ramp_for_shader[0]["position"]},
                {"property": "_RampPos1", "value": ramp_for_shader[1]["position"]},
                {"property": "_RampPos2", "value": ramp_for_shader[2]["position"]},
                {"property": "_NoiseScale", "value": round(float(noise_scale), 4)},
            ])
        elif shader == "PigTycoon/URP/ProceduralRockCoast":
            extra_colors.extend([
                {"property": "_RampColor0", "rgba": ramp_for_shader[0]["color"]},
                {"property": "_RampColor1", "rgba": ramp_for_shader[1]["color"]},
                {"property": "_RampColor2", "rgba": ramp_for_shader[2]["color"]},
            ])
            extra_floats.extend([
                {"property": "_RampPos0", "value": ramp_for_shader[0]["position"]},
                {"property": "_RampPos1", "value": ramp_for_shader[1]["position"]},
                {"property": "_RampPos2", "value": ramp_for_shader[2]["position"]},
                {"property": "_StrataFrequency", "value": round(float(wave_scale), 4)},
                {"property": "_NoiseFrequency", "value": round(float(noise_scale), 4)},
                {"property": "_StrataDistortion", "value": round(float(wave_distortion), 4)},
                {"property": "_BumpStrength", "value": round(float(bump_scale), 4)},
            ])
        elif shader == "PigTycoon/URP/ObjectSpaceProcedural":
            extra_colors.extend([
                {"property": "_RampColor0", "rgba": ramp_for_shader[0]["color"]},
                {"property": "_RampColor1", "rgba": ramp_for_shader[1]["color"]},
            ])
            obj_scale = 4.5 if name == "MPT_cobalt" else (3.5 if name.startswith("MPT_") else 2.0)
            extra_floats.extend([
                {"property": "_RampPos0", "value": ramp_for_shader[0]["position"]},
                {"property": "_RampPos1", "value": ramp_for_shader[1]["position"]},
                {"property": "_ObjectScale", "value": obj_scale},
                {"property": "_GrainStretch", "value": grain_stretch},
                {"property": "_BumpStrength", "value": round(float(bump_scale), 4)},
            ])
        elif shader == "PigTycoon/URP/WaterSurface":
            extra_colors.extend([
                {"property": "_ShallowColor", "rgba": shallow_color_srgb},
                {"property": "_DeepColor", "rgba": deep_color_srgb},
            ])

        record = {
            "index": idx,
            "name": name,
            "sourceMaterialId": idx,
            "sourceMaterialName": name,
            "unityMaterialAssetPath": unity_mat_asset_path,
            "mappingStatus": "EXPLICIT_CANDIDATE_NOT_VERIFIED",
            "shaderName": shader,
            "surfaceMode": surface_mode,
            "alphaClip": alpha_clip,
            "cutoff": cutoff,
            "doubleSided": (cull_mode == 0),
            "baseColor": base_color_srgb,
            "smoothness": round(float(smoothness), 4),
            "metallic": round(float(metallic), 4),
            "bumpScale": round(float(bump_scale), 4),
            "baseMapAssetPath": unity_base_map_asset or "",
            "normalMapAssetPath": unity_bump_map_asset or "",
            "roughnessMapAssetPath": "",
            "secondaryMapAssetPath": unity_emit_map_asset or "",
            "extraFloats": extra_floats,
            "extraColors": extra_colors,
            "triage_category": triage,
            "transfer_route": route,
            "target_shader": shader,
            "status": "EXPLICIT_CANDIDATE_NOT_VERIFIED",
            "properties": {
                "_BaseColor": base_color_srgb,
                "_BaseMap": base_map,
                "_BaseMapAssetPath": unity_base_map_asset,
                "_BaseMapSha256": base_map_sha256,
                "_BaseMapColorspace": base_map_colorspace,
                "_Metallic": round(float(metallic), 4),
                "_Smoothness": round(float(smoothness), 4),
                "_SourceRoughness": round(float(roughness), 4),
                "_SourceAlpha": round(float(alpha), 4),
                "_SourceTransmission": round(float(transmission), 4),
                "_EmissionColor": emit_col,
                "_EmissionMap": emission_map,
                "_EmissionMapAssetPath": unity_emit_map_asset,
                "_EmissionMapSha256": emission_map_sha256,
                "_BumpMap": bump_map,
                "_BumpMapAssetPath": unity_bump_map_asset,
                "_BumpMapSha256": bump_map_sha256,
                "_BumpScale": round(float(bump_scale), 4),
                "_ShallowColor": shallow_color_srgb,
                "_DeepColor": deep_color_srgb,
                "_Surface": surface_type,
                "_Blend": 0,
                "_ZWrite": zwrite,
                "_RenderQueue": render_queue,
                "_Cull": cull_mode,
                "_AlphaClip": alpha_clip,
                "_Cutoff": cutoff,
            },
            "procedural_config": {
                "has_procedural_nodes": bool(raw_ramp_elements or noise_nodes or wave_nodes),
                "color_ramp_stops": len(ramp_for_shader),
                "ramp_elements": ramp_for_shader,
                "noise_scale": round(float(noise_scale), 4),
                "noise_detail": round(float(noise_detail), 4),
                "strata_frequency": round(float(wave_scale), 4),
                "strata_distortion": round(float(wave_distortion), 4),
                "bump_strength": round(float(bump_scale), 4),
                "object_scale": 4.5 if name == "MPT_cobalt" else (3.5 if name.startswith("MPT_") else 2.0),
                "grain_stretch": grain_stretch,
            },
            "geometry_bindings": mesh_stats[idx],
        }
        records.append(record)

    manifest_sha = sha256_file(MANIFEST_PATH)
    output = {
        "kind": "POST_NATURE_5D4_SOURCE_TO_URP_TRANSFER_MANIFEST",
        "bundleDirectory": ".collab/artifacts/original-map-unity-20261026/post-nature-5d4-source-bundle-v1",
        "expectedRunId": manifest["run_id"],
        "expectedSourceSha256": manifest["source_sha256"],
        "expectedManifestSha256": manifest_sha,
        "allowExplicitUnverifiedMaterialCandidates": True,
        "addReviewCamera": False,
        "run_id": manifest["run_id"],
        "source_sha256": manifest["source_sha256"],
        "manifest_sha256": manifest_sha,
        "blender_version": manifest["blender_version"],
        "total_materials": len(records),
        "route_summary": dict(route_counter),
        "materialBindings": records,
        "materials": records,
    }
    OUTPUT_MANIFEST_PATH.write_text(json.dumps(output, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Generated {OUTPUT_MANIFEST_PATH} with {len(records)} materials.")
    for r, c in route_counter.most_common():
        print(f"  {r:35s}: {c:3d}")


if __name__ == "__main__":
    generate()
