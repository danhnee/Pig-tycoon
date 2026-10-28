"""Build a source-backed spatial proposal; never edits or opens Unity scenes.

Run: python Tools/review_world_scene_layout.py
All generated files stay in .collab/artifacts/world-scene-layout-codex.
Coordinates in proposal geometry are local metres, southwest origin, north +Z.
Drawing coordinates are presentation-only and never define world topology.
"""
from __future__ import annotations

import argparse
import hashlib
import html
import json
import math
import re
import subprocess
import textwrap
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / ".collab/artifacts/world-scene-layout-codex"
DEFAULT_SOURCES = Path("D:/CodexHome/visualizations/2026/10/18/01a14cfe-2d81-7d83-a693-615ea95ab2a4/source-documents")
INK, PANEL, MUTED = "#101f2b", "#1b3040", "#a6bcc8"
WHITE, GOLD, GREEN, CYAN, PURPLE = "#f3edda", "#e6bd70", "#83b98e", "#78bacb", "#b1a0cf"


def source(kind: str, *numbers: int) -> list[str]:
    return [f"{kind}:word/document.xml:p{number:05d}" for number in numbers]


def evidence(sources_dir: Path) -> dict:
    result = {}
    source_paths = [sources_dir / f"{kind}.txt" for kind in ("technical", "mechanics")]
    source_paths.append(OUT / "agent-source/agent-full.txt")
    for path in source_paths:
        for line in path.read_text(encoding="utf-8").splitlines():
            match = re.match(r"\[([^\s\]]+)(?:[^\]]*)\] (.*)", line)
            if match:
                result[match[1]] = match[2]
    return result


def pad(name: str, rectangle: list[float], role: str) -> dict:
    return {"name": name, "reservation_rect_m": rectangle, "role": role,
            "status": "proposed_reservation_not_locked_building_footprint"}


def build_contract() -> dict:
    scenes = [
        {"id": "Farm_Main", "name": "Đồng cỏ / chiến trường", "dimensions_m": [120, 90],
         "dimension_status": "GDD_baseline_initial_bounds", "expanded_dimensions_m": [220, 170],
         "inside_farm_rules": True, "wave_attacks": True, "player": True, "pigs": True,
         "role": "Đàn thả rông, dịch vụ, phòng thủ nhiều hướng, Cổng động và Bí nhân ngoài rào.",
         "sources": source("mechanics", 668, 675, 693, 713, 715),
         "layout": {"pasture_polygon_m": [[18, 18], [95, 18], [102, 37], [100, 70], [76, 78], [42, 78], [18, 62]],
                    "shelter_rect_m": [42, 66, 36, 12],
                    "shelter_entries_m": [[42, 69], [78, 69]],
                    "shelter_entry_width_min_m": 6,
                    "defense_envelope_width_m": 12,
                    "defense_pads_m": [[0, 0, 45, 12], [75, 0, 45, 12], [0, 12, 12, 28],
                                       [108, 12, 12, 28], [0, 55, 12, 20], [108, 55, 12, 20],
                                       [15, 78, 15, 12], [90, 78, 15, 12]],
                    "service_reservations": [pad("Cách ly (người chơi chỉ định)", [0, 55, 10, 20], "Candidate only; overlaps defense candidate pad, choose one use"),
                                             pad("Kỹ thuật điện", [14, 4, 12, 10], "Candidate equipment bay inside defense reservation"),
                                             pad("Xử lý (tự chọn)", [90, 3, 12, 8], "Candidate only; final distance tradeoff stays player-controlled")],
                    "outer_ring_proposed_width_m": 35,
                    "entry_candidates_m": [[20, 125], [100, 125], [155, 20], [155, 70],
                                           [20, -35], [100, -35], [-35, 20], [-35, 70]],
                    "portal_search_pockets_m": [[30, 48], [88, 50], [35, 28], [85, 28]],
                    "portal_pockets_visible_to_player": False,
                    "herd_loop_m": [[30, 38], [45, 24], [80, 25], [90, 45], [77, 64], [47, 64], [30, 38]],
                    "shelter_approach_routes_m": [[[47, 64], [42, 64], [42, 72]], [[77, 64], [78, 64], [78, 72]]],
                    "herd_loop_min_width_m": 6,
                    "service_route_m": [[0, 48], [15, 48], [15, 14], [105, 14], [105, 50]],
                    "gate_positions_m": {"core": [60, 90], "market": [60, 0], "village": [0, 48]},
                    "forest_path_m": [[120, 90], [140, 110], [140, 125]]}},
        {"id": "Farm_Core", "name": "Lõi vận hành / hậu trại", "dimensions_m": [60, 40],
         "dimension_status": "handbook_separate_scene_proposal_requires_boundary_ADR", "inside_farm_rules": True,
         "wave_attacks": True, "player": True, "pigs": False,
         "role": "Bảy khu chức năng; Kho/Nhà chính là mục tiêu quái cuối tuyến; không phải làng NPC.",
         "sources": source("technical", 482, 504, 511, 513, 533),
         "layout": {"entry_m": [30, 0], "paths_m": [[[30, 0], [30, 7], [20, 7], [20, 30], [18, 30]],
                                                      [[20, 23], [40, 23]], [[40, 8], [40, 30], [43, 30]],
                                                      [[30, 23], [30, 27]], [[20, 13], [17, 13]],
                                                      [[40, 13], [43, 13]], [[30, 7], [30, 8]]],
                    "pads": [pad("Nhà chính", [23, 27, 14, 10], "ngủ / lưu / Bạch Vân"),
                             pad("Kho", [3, 25, 15, 12], "chứa hàng / cám / biến thể nâng cấp từ GDD"),
                             pad("Xây dựng", [3, 7, 14, 12], "bản vẽ / sửa / thi công"),
                             pad("Nghiên cứu", [43, 25, 14, 12], "gen / hàng chờ / Nhà Nghiên Cứu"),
                             pad("Cường hóa", [43, 7, 14, 12], "giấy / khắc / Mệnh Ấn"),
                             pad("Quân Y", [24, 8, 10, 8], "công năng chi tiết chưa khóa trong GDD"),
                             pad("Thông tin", [24, 18, 12, 5], "radar / thời tiết / bàn tình báo")]}},
        {"id": "Hub_Village", "name": "Làng NPC / triền sau phía Tây", "dimensions_m": [40, 30],
         "dimension_status": "handbook_proposed_not_GDD_lock", "inside_farm_rules": False,
         "wave_attacks": False, "player": True, "pigs": False,
         "role": "Làng riêng có nhà Bà Sáu, nhà NPC, sân làng, bảng tin; không combat.",
         "sources": source("technical", 482, 509, 515, 703),
         "layout": {"entry_m": [40, 15], "paths_m": [[[40, 15], [10, 15]], [[20, 15], [20, 24], [25, 24]],
                                                      [[10, 10], [10, 19]], [[31, 15], [31, 10]]],
                    "pads": [pad("Nhà Bà Sáu", [3, 19, 12, 8], "cốt truyện / chỗ ở"),
                             pad("Nhà NPC A", [4, 3, 10, 7], "ngoài giờ làm"),
                             pad("Nhà NPC B", [25, 21, 11, 7], "ngoài giờ làm"),
                             pad("Nhà NPC C", [26, 3, 10, 7], "ngoài giờ làm"),
                             pad("Bảng tin / sân làng", [17, 11, 9, 9], "Quạ Tin / tin đồn")],
                    "visual_only": {"vista_bearing": "northwest", "depth": "rear_terrace",
                                    "topology_entry": "west_from_Farm_Main",
                                    "camera": "optional south-to-north composition; no global yaw change",
                                    "rule": "Distant village silhouettes are vista proxies; interactive residents load only in Hub_Village."}}},
        {"id": "Market_Town", "name": "Chợ thị trấn / ngày và đêm", "dimensions_m": [50, 40],
         "dimension_status": "handbook_proposed_not_GDD_lock", "inside_farm_rules": False,
         "wave_attacks": False, "player": True, "pigs": False,
         "role": "Một scene với hai lớp 06–18 / 18–06; đấu giá, giám định, thuê lính.",
         "sources": source("technical", 494, 639, 641, 699),
         "layout": {"entry_m": [25, 40], "paths_m": [[[25, 40], [25, 5]], [[5, 20], [45, 20]],
                                                      [[20, 30], [30, 30]], [[20, 10], [30, 10]]],
                    "pads": [pad("Quầy ngày", [3, 25, 17, 11], "heo / cám / vật liệu / Diesel"),
                             pad("Kiểm dịch / giám định", [30, 25, 17, 11], "Hiếm–Quý; cấp cao tùy đêm"),
                             pad("Sân trao đổi", [3, 4, 17, 11], "lớp đêm; món muốn nhận"),
                             pad("Đấu giá / hội quán", [30, 4, 17, 11], "thuê lính ở đêm")],
                    "layers_same_scene": ["day_06_18", "night_18_06"]}},
        {"id": "Forest_Primeval", "name": "Rừng nguyên sinh / chiều sâu cổ tích", "dimensions_m": [80, 80],
         "dimension_status": "handbook_proposed_not_GDD_lock", "inside_farm_rules": False,
         "wave_attacks": False, "player": True, "pigs": False,
         "role": "Bìa rừng thu thập, rừng sâu, hồ, di tích cổ, Mận dẫn đường; không Cổng cố định.",
         "sources": source("technical", 500, 665, 667, 671, 685),
         "layout": {"entry_m": [40, 0], "paths_m": [[[40, 0], [40, 26], [32, 29], [30, 46], [32, 55], [74, 55], [74, 26], [40, 26]],
                                                      [[40, 55], [40, 58]], [[30, 46], [27, 46]]],
                    "pads": [pad("Bìa rừng / Mận", [15, 7, 40, 18], "gỗ / nấm / dẫn đường"),
                             pad("Hồ cổ", [45, 28, 25, 22], "cảnh quan; đường vòng hai bên"),
                             pad("Di tích", [24, 58, 30, 16], "gợi cốt truyện; không cổng cố định"),
                             pad("Rừng sâu", [2, 32, 25, 23], "tán cây / thu thập")]}},
    ]
    transitions = [
        ("Farm_Main", "Farm_Core", "north", "south", "core", True, source("technical", 511, 695, 697)),
        ("Farm_Main", "Hub_Village", "west", "village_gate", "hub", False, source("technical", 703, 705)),
        ("Farm_Main", "Market_Town", "south", "north", "market", False, source("technical", 699, 701)),
        ("Farm_Main", "Forest_Primeval", "north_outer_path", "south", "forest", False, source("technical", 707, 709)),
    ]
    edges = []
    for a, b, side_a, side_b, key, enemy, refs in transitions:
        dest = "core" if key == "core" else key
        edges.append({"from": a, "to": b, "bidirectional": True, "from_side": side_a, "to_side": side_b,
                      "spawn_return": f"spawn.farm.from_{key}", "spawn_arrival": f"spawn.{dest}.from_farm",
                      "player_allowed": True, "wave_enemy_allowed": enemy, "pig_allowed": False,
                      "sources": refs, "load_mode": "additive", "max_transition_seconds": 1.5})
    return {"schema_version": 1, "status": "reviewable_spatial_proposal_not_Unity_acceptance",
            "coordinate_system": {"units": "metres", "origin": "southwest_per_scene", "north": "+Z", "east": "+X",
                                  "proposal_coordinates": "All placed pads/routes are layout choices, not newly invented balance values."},
            "scope": {"persistent_scene_count": 5, "event_scene_count": 12, "event_templates": 14,
                      "scene_files_modified": False, "Unity_launched": False, "legacy_11_zone_diorama_authoritative": False},
            "runtime_locks": {"Unity": "6000.6.3f1", "existing_sprite_ppu": 32,
                              "players": ["An", "Khoa"], "do_not_change": ["balance", "price_formula", "16_direction_fence", "night_market_gold_ratio", "existing_1m_prototype_snap"]},
            "assurance": {"fourth_document_role": "engineering_evidence_reference_not_new_gameplay_or_authority_over_AGENTS",
                          "observed": ["source paragraphs extracted and read", "five-scene static contract generated", "SVG and PNG files rendered"],
                          "inference": ["rearward northwest village vista can preserve western gameplay gate"],
                          "hypotheses_requiring_integration": ["combined logical farm boundary across Farm_Main and Farm_Core", "defense envelope versus actual pads interpretation"],
                          "not_claimed": ["Unity integration complete", "runtime navigation accepted", "1.5 second load verified", "performance improvement"],
                          "sources": source("agent", 4, 6, 8, 14, 17, 19, 20, 22, 24, 26)},
            "scenes": scenes, "transitions": edges,
            "corridor": {"length_m_approx": 20, "width_m_approx": 8, "gate_min_width_m": 6,
                         "fade_seconds_approx": 0.5, "max_transition_seconds": 1.5,
                         "player_allowed": True, "wave_enemy_allowed": True, "pig_allowed": False,
                         "physical_role": "last defense before warehouse/main house", "sources": source("technical", 511, 533)},
            "world_rules": {"single_logical_farm_boundary": "Farm_Main + corridor + Farm_Core; exact unified boundary mapping requires ADR",
                            "enemy_target_order": ["herd", "warehouse", "infrastructure", "main_house"],
                            "simulation_persists_across_scenes": True, "exterior_residents_during_combat": False,
                            "pasture_min_fraction": 0.35, "defense_building_fraction_range": [0.20, 0.25],
                            "defense_strip_width_range_m": [12, 20], "outer_ring_width_range_m": [30, 40],
                            "cleared_lots_initial": 12, "cleared_lots_available_approx": 40, "lot_size_m": [10, 10],
                            "gate_min_width_main_m": 6, "gate_min_width_secondary_m": 4,
                            "ground_approach_sectors_min": 3, "outer_entry_count_range": [8, 12],
                            "spawn_exclusion_entity_m": 25, "spawn_in_camera_allowed": False,
                            "portal_valid_pockets_min": 4, "portal_distance_to_buildings_min_m": 6,
                            "portal_on_herd_route_allowed": False, "portal_overflow": "outer_perimeter_path",
                            "mysterious_trader": "outer_path_within_30m_of_farm_boundary",
                            "service_radius_m": 30, "cooling_radius_m": 25, "quarantine_lots_min": 2,
                            "sources": source("mechanics", 668, 675, 686, 689, 693, 699, 701, 705, 709, 713, 715, 721, 723, 731) + source("technical", 504, 715, 716)},
            "events": {"entry_scene": "Farm_Main", "fixed_gate_in_forest": False, "hidden_candidate_zones": True,
                       "farm_defense_templates": ["Đợt Săn Trong Sương", "Càn Quét UFO"],
                       "sources": source("technical", 667, 711, 964) + source("mechanics", 713)},
            "conflicts": conflicts()}


def conflicts() -> list[dict]:
    rows = [
        ("C01", "open_ADR", "Tách Core và một ranh giới trại", "GDD: một đa giác, lõi liền đồng cỏ. Sổ tay: Core riêng + hành lang, ranh giới gộp. Đề xuất giữ một logical farm_id và liên kết nav; chưa xác nhận hình học ranh giới trên hai scene.", source("mechanics", 674, 678, 772) + source("technical", 504, 511)),
        ("C02", "explicit_visual_interpretation", "Làng phía sau nhưng cổng Tây", "Làng độc lập ở triền hậu cảnh Tây-Bắc; tuyến chơi vẫn thoát cổng Tây. Vị trí card trên atlas và vista xa không sửa topology. Nếu cần làng chính Bắc bằng tọa độ gameplay, phải sửa graph bằng quyết định riêng.", source("technical", 703, 515)),
        ("C03", "migration_required_no_edit", "Code cũ bắt đi chợ qua làng", "WorldMapZoneId thiếu Farm_Core; TransitionPortals chỉ FarmMain↔KhuNPC↔ChoThiTran. Proposal theo tài liệu thêm Core và tuyến Farm_Main↔Market_Town phía Nam; không sửa source trong gói này.", source("technical", 695, 699, 707)),
        ("C04", "resolved_by_GDD", "12 Ô Đất không phải tổng diện tích", "GDD p00668 nói có sẵn 12 lô, khai hoang tới khoảng40;120×90m là toàn bounds. Không có phép chia120×90=12 ô. Giữ số GDD, không đoán thêm sức chứa.", source("mechanics", 662, 668) + source("technical", 488)),
        ("C05", "open_geometry_interpretation", "Dải 12m và tỷ lệ20–25%", "Vành liên tục12m quanh120×90 chiếm41.33%. Sơ đồ giữ envelope12m và các pad xây riêng để đọc rõ xung đột. Diện tích pad không tự chứng minh yêu cầu diện tích dải; cần ADR trước acceptance.", source("mechanics", 668, 686, 776, 848)),
        ("C06", "current_ADR_preserved", "PPU / camera / snap", "Word16PPU không ghi đè32PPU đang chơi. Không sửa camera hay rào16hướng. Sơ đồ có mét thật nhưng không giả là screenshot gameplay hoặc đã nghiệm thu pixel-perfect.", source("technical", 413, 417) + source("mechanics", 660, 664)),
        ("C07", "source_gap", "Ngoại hình kho nâng cấp", "Bố trí dành chỗ riêng cho kho. Số cấp, dung lượng, giá và mức mở phải lấy catalog/GDD; gói này không tự tạo cấp kho.", source("technical", 513, 517)),
        ("C08", "proposal_not_numeric_lock", "Kích thước map ngoại vi", "Làng40×30, chợ50×40, rừng80×80 là đề xuất sổ tay. Dùng làm blockout review có gắn nhãn; không tuyên bố số GDD khóa.", source("technical", 482, 494, 500)),
        ("C09", "migration_required_no_edit", "Giới hạn heo1/3 trong code cũ", "Code cũ MaxAllowedPigRoamingAreaRatio=1/3 và pasture30%; GDD đòi chăn thả≥35% và đi tự do trong ranh giới. Proposal không kế thừa cap1/3; chưa sửa code hay kiểm tra di chuyển thật.", source("mechanics", 532, 668)),
        ("C10", "deferred_runtime_validation", "Hai lần qua scene không thay state", "Scene chỉ hiển thị; đàn, kinh tế, thời tiết, thời gian tiếp tục. Cần integration test lưu state và quái qua hành lang; graph tĩnh chưa chứng minh latency1.5s hoặc AI.", source("technical", 533, 715, 716, 717)),
    ]
    return [{"id": i, "status": s, "title": t, "decision": d, "sources": refs} for i, s, t, d, refs in rows]


def polygon_area(points: list[list[float]]) -> float:
    return abs(sum(x1*y2-x2*y1 for (x1, y1), (x2, y2) in zip(points, points[1:]+points[:1]))) / 2


def crosses_rectangle(a: list[float], b: list[float], rectangle: list[float]) -> bool:
    """Slab intersection for a closed segment and the rectangle's open interior."""
    x, z, w, h = rectangle
    start, end = 0.0, 1.0
    for origin, destination, low, high in zip(a, b, (x, z), (x+w, z+h)):
        delta = destination - origin
        if abs(delta) < 1e-9:
            if not low < origin < high:
                return False
            continue
        lower, upper = sorted(((low-origin)/delta, (high-origin)/delta))
        start, end = max(start, lower), min(end, upper)
        if start >= end:
            return False
    return start < end


def verify(contract: dict, source_text: dict) -> dict:
    checks = []
    def check(name, condition, detail):
        checks.append({"name": name, "passed": bool(condition), "detail": detail})
    ids = {scene["id"] for scene in contract["scenes"]}
    check("five_named_scenes", ids == {"Farm_Main", "Farm_Core", "Hub_Village", "Market_Town", "Forest_Primeval"}, sorted(ids))
    check("direct_market_edge", any(e["to"] == "Market_Town" and e["from_side"] == "south" for e in contract["transitions"]), "Document topology retained")
    check("separate_village_west_edge", any(e["to"] == "Hub_Village" and e["from_side"] == "west" for e in contract["transitions"]), "Rearward vista does not change cardinal edge")
    check("pigs_do_not_transition", all(not e["pig_allowed"] for e in contract["transitions"]), "No herd transfer out of Farm_Main")
    check("enemies_only_transfer_to_core", all(e["wave_enemy_allowed"] == (e["to"] == "Farm_Core") for e in contract["transitions"]), "No wave attacks in village, market or forest")
    check("seven_core_functional_pads", len(contract["scenes"][1]["layout"]["pads"]) == 7, "Seven different functional reservations")
    main = contract["scenes"][0]
    farm_area = math.prod(main["dimensions_m"])
    pasture_area = polygon_area(main["layout"]["pasture_polygon_m"])
    pad_area = sum(w*h for x, z, w, h in main["layout"]["defense_pads_m"])
    envelope_area = farm_area - (120-24)*(90-24)
    check("pasture_reservation_min_35_percent", pasture_area / farm_area >= .35, {"area_m2": pasture_area, "ratio": pasture_area / farm_area})
    check("defense_pad_reservation_20_to_25_percent", .20 <= pad_area / farm_area <= .25, {"area_m2": pad_area, "ratio": pad_area / farm_area, "not_proof_of_strip_area": True})
    entries = main["layout"]["entry_candidates_m"]
    sectors = {"north": sum(z > 90 for x, z in entries), "south": sum(z < 0 for x, z in entries),
               "east": sum(x > 120 for x, z in entries), "west": sum(x < 0 for x, z in entries)}
    check("eight_entry_candidates_four_sectors", 8 <= len(entries) <= 12 and all(sectors.values()),
          {"candidates_by_sector": sectors, "runtime_eligibility_not_tested": True})
    check("four_hidden_portal_search_pockets", len(main["layout"]["portal_search_pockets_m"]) >= 4 and not main["layout"]["portal_pockets_visible_to_player"], "Debug overlay only; not fixed event plazas")
    forest = next(scene for scene in contract["scenes"] if scene["id"] == "Forest_Primeval")
    lake = next(p["reservation_rect_m"] for p in forest["layout"]["pads"] if p["name"] == "Hồ cổ")
    lake_crossings = [(a, b) for path in forest["layout"]["paths_m"] for a, b in zip(path, path[1:]) if crosses_rectangle(a, b, lake)]
    check("forest_path_avoids_lake_interior", not lake_crossings, {"segments_crossing_lake": lake_crossings})
    script_lines = Path(__file__).read_text(encoding="utf-8").splitlines()
    check("generator_has_no_trailing_whitespace", all(line == line.rstrip() for line in script_lines), "Only this branch's generator checked")
    referenced = set()
    def collect(value):
        if isinstance(value, dict):
            for key, item in value.items():
                if key == "sources":
                    referenced.update(item)
                else:
                    collect(item)
        elif isinstance(value, list):
            for item in value:
                collect(item)
    collect(contract)
    missing = sorted(referenced - source_text.keys())
    check("every_document_anchor_exists", not missing, {"anchors": len(referenced), "missing": missing})
    for scene in contract["scenes"]:
        w, h = scene["dimensions_m"]
        for item in scene["layout"].get("pads", []):
            x, y, rw, rh = item["reservation_rect_m"]
            check(f"pad_inside_{scene['id']}_{item['name']}", 0 <= x < x+rw <= w and 0 <= y < y+rh <= h, item["reservation_rect_m"])
    return {"static_checks_passed": all(c["passed"] for c in checks), "checks": checks,
            "farm_metrics": {"bounds_area_m2": farm_area, "pasture_area_m2": pasture_area, "pasture_fraction": pasture_area/farm_area,
                             "defense_pad_area_m2": pad_area, "defense_pad_fraction": pad_area/farm_area,
                             "continuous_12m_envelope_fraction": envelope_area/farm_area},
            "not_tested": ["Unity import/playmode", "pathfinding", "150-pig gate throughput", "wave distribution", "runtime spawn eligibility", "1.5s scene loading", "pixel-perfect rendering", "runtime collision"],
            "source_evidence": {key: source_text[key] for key in sorted(referenced) if key in source_text}}


class Drawing:
    """One primitive stream produces matching SVG and optional PNG previews."""
    def __init__(self, width: int, height: int):
        self.width, self.height = width, height
        self.items = []

    def rect(self, x, y, w, h, fill=PANEL, stroke=None, radius=0, width=1):
        self.items.append(("rect", (x, y, w, h), {"fill": fill, "stroke": stroke, "rx": radius, "stroke-width": width}))

    def line(self, points, color=MUTED, width=2, dash=None):
        self.items.append(("polyline", points, {"fill": "none", "stroke": color, "stroke-width": width, "stroke-dasharray": dash}))

    def poly(self, points, fill, stroke=None, width=1):
        self.items.append(("polygon", points, {"fill": fill, "stroke": stroke, "stroke-width": width}))

    def text(self, x, y, text, size=20, color=WHITE, bold=False):
        self.items.append(("text", (x, y, str(text)), {"fill": color, "font-size": size, "font-weight": "700" if bold else "400"}))

    def circle(self, x, y, r, fill=GOLD, stroke=None):
        self.items.append(("circle", (x, y, r), {"fill": fill, "stroke": stroke}))

    def save(self, name: str):
        root = ET.Element("svg", {"xmlns": "http://www.w3.org/2000/svg", "width": str(self.width), "height": str(self.height), "viewBox": f"0 0 {self.width} {self.height}"})
        ET.SubElement(root, "title").text = name
        for tag, data, attrs in self.items:
            attrs = {k: str(v) for k, v in attrs.items() if v is not None}
            if tag == "rect":
                attrs.update(zip(("x", "y", "width", "height"), map(str, data)))
            elif tag in ("polyline", "polygon"):
                attrs["points"] = " ".join(f"{x},{y}" for x, y in data)
            elif tag == "circle":
                attrs.update(zip(("cx", "cy", "r"), map(str, data)))
            else:
                attrs.update({"x": str(data[0]), "y": str(data[1]), "font-family": "Segoe UI, Arial, sans-serif"})
            element = ET.SubElement(root, tag, attrs)
            if tag == "text":
                element.text = data[2]
        ET.ElementTree(root).write(OUT / f"{name}.svg", encoding="utf-8", xml_declaration=True)
        self.save_png(name)

    def save_png(self, name: str):
        try:
            from PIL import Image, ImageDraw, ImageFont
        except ImportError:
            return
        image = Image.new("RGB", (self.width, self.height), INK)
        draw = ImageDraw.Draw(image)
        font_cache = {}
        for tag, data, attrs in self.items:
            fill = attrs.get("fill")
            stroke = attrs.get("stroke")
            width = int(attrs.get("stroke-width", 1))
            if tag == "rect":
                x, y, w, h = data
                draw.rounded_rectangle([x, y, x+w, y+h], radius=int(attrs.get("rx", 0)), fill=fill, outline=stroke, width=width)
            elif tag in ("polygon", "polyline"):
                if tag == "polygon":
                    draw.polygon(data, fill=fill, outline=stroke, width=width)
                elif attrs.get("stroke-dasharray"):
                    for a, b in zip(data, data[1:]):
                        length = math.dist(a, b)
                        for at in range(0, math.ceil(length), 15):
                            start, stop = at/max(length, 1), min(at+8, length)/max(length, 1)
                            draw.line([(a[0]+(b[0]-a[0])*start, a[1]+(b[1]-a[1])*start),
                                       (a[0]+(b[0]-a[0])*stop, a[1]+(b[1]-a[1])*stop)], fill=stroke, width=width)
                else:
                    draw.line(data, fill=stroke, width=width, joint="curve")
            elif tag == "circle":
                x, y, r = data
                draw.ellipse([x-r, y-r, x+r, y+r], fill=fill, outline=stroke)
            else:
                key = (int(attrs["font-size"]), attrs["font-weight"])
                if key not in font_cache:
                    font_path = Path("C:/Windows/Fonts") / ("segoeuib.ttf" if key[1] == "700" else "segoeui.ttf")
                    font_cache[key] = ImageFont.truetype(str(font_path), key[0]) if font_path.exists() else ImageFont.load_default()
                draw.text((data[0], data[1]), data[2], font=font_cache[key], fill=fill, anchor="ls")
        image.save(OUT / f"{name}.png")


def paragraph(d: Drawing, x, y, text, width=63, size=18, color=MUTED, lineheight=27):
    for line in textwrap.wrap(text, width):
        d.text(x, y, line, size, color)
        y += lineheight
    return y


def overview(contract: dict):
    d = Drawing(1800, 1250)
    d.rect(0, 0, 1800, 1250, INK)
    d.text(55, 70, "PIG TYCOON  /  THẾ GIỚI GỒM NHIỀU SCENE", 35, GOLD, True)
    d.text(55, 108, "Sơ đồ liên kết có nguồn • không phải bản đồ liền mạch • vị trí card không phải tọa độ gameplay", 20, MUTED)
    cards = {"Hub_Village": (55, 180, 470, 265), "Farm_Core": (650, 180, 500, 265),
             "Forest_Primeval": (1275, 180, 470, 265), "Farm_Main": (600, 590, 600, 280),
             "Market_Town": (655, 1010, 490, 180)}
    colors = {"Hub_Village": PURPLE, "Farm_Core": GOLD, "Forest_Primeval": GREEN, "Farm_Main": GREEN, "Market_Town": CYAN}
    d.line([(600, 710), (290, 710), (290, 445)], PURPLE, 4)
    d.text(65, 555, "CỔNG TÂY → LÀNG RIÊNG", 20, PURPLE, True)
    d.text(65, 587, "Vista Tây-Bắc / triền hậu cảnh", 18, MUTED)
    d.line([(900, 590), (900, 445)], GOLD, 8)
    d.text(926, 500, "BẮC • hành lang ~20×8 m", 18, GOLD)
    d.text(926, 531, "người + quái / cấm heo", 18, MUTED)
    d.line([(1200, 710), (1510, 710), (1510, 445)], GREEN, 4)
    d.text(1285, 560, "LỐI VÀNH NGOÀI BẮC", 20, GREEN, True)
    d.text(1285, 591, "Vào rừng từ phía Nam", 18, MUTED)
    d.line([(900, 870), (900, 1010)], CYAN, 4)
    d.text(930, 938, "CỔNG NAM → đường Bắc chợ", 18, CYAN)
    d.text(930, 968, "không bắt đi xuyên làng NPC", 18, MUTED)
    for scene in contract["scenes"]:
        x, y, w, h = cards[scene["id"]]
        color = colors[scene["id"]]
        d.rect(x, y, w, h, PANEL, color, 18, 2)
        d.rect(x+18, y+22, 5, 38, color)
        d.text(x+36, y+48, scene["id"], 27, WHITE, True)
        d.text(x+36, y+82, scene["name"], 20, color)
        dimension = "×".join(map(str, scene["dimensions_m"])) + " m"
        state = "trong luật trại" if scene["inside_farm_rules"] else "ngoại vi / không đợt quái"
        d.text(x+36, y+116, dimension + " • " + state, 17, MUTED)
        if scene["id"] != "Market_Town":
            paragraph(d, x+36, y+157, scene["role"], int(w/10.5), 18)
    d.rect(55, 790, 470, 215, "#293747", PURPLE, 16)
    d.text(80, 831, "12 SCENE SỰ KIỆN", 25, PURPLE, True)
    paragraph(d, 80, 868, "Cổng động / Sương → Event_*. Không có khu sự kiện cố định. Hai mẫu phòng thủ còn lại diễn ra ngay ở trại.", 42, 18)
    d.line([(525, 820), (600, 820)], PURPLE, 3, "8 7")
    d.rect(1275, 790, 470, 215, "#2d3435", GOLD, 16)
    d.text(1300, 831, "VISTA ≠ MAP CHƠI", 25, GOLD, True)
    paragraph(d, 1300, 868, "Nhà làng ở lớp cảnh phía sau chỉ là vista khi đứng tại trại. Điểm tương tác và dân làng thật ở Hub_Village riêng.", 43, 18)
    d.text(55, 1130, "5 scene thường trực", 24, WHITE, True)
    d.text(55, 1167, "4 tuyến hai chiều từ trại", 22, MUTED)
    d.text(55, 1210, "Nguồn: technical p00504, p00511, p00694–p00717; mechanics p00668–p00693. Bố cục trình bày: đề xuất.", 16, MUTED)
    d.save("world-overview")


def farm_plan(contract: dict, verification: dict):
    d = Drawing(1650, 1220)
    d.rect(0, 0, 1650, 1220, INK)
    d.text(45, 60, "FARM_MAIN  /  ĐỒNG CỎ, DỊCH VỤ VÀ BỐN HƯỚNG PHÒNG THỦ", 29, GOLD, True)
    d.text(45, 96, "Blockout theo mét thật • tọa độ đề xuất • ô Cổng chỉ hiện trong công cụ review", 19, MUTED)
    ox, oy, scale = 225, 825, 5.0
    def pt(x, z):
        return ox+x*scale, oy-z*scale
    def box(rect, color, stroke=None):
        x, z, w, h = rect
        px, py = pt(x, z+h)
        d.rect(px, py, w*scale, h*scale, color, stroke)
    lay = contract["scenes"][0]["layout"]
    box([-35, -35, 190, 160], "#23373d", "#557077")
    box([0, 0, 120, 90], "#4e6753", GOLD)
    box([12, 12, 96, 66], "#344e40")
    for x in range(0, 121, 10):
        d.line([pt(x, 0), pt(x, 90)], "#476352", 1)
    for z in range(0, 91, 10):
        d.line([pt(0, z), pt(120, z)], "#476352", 1)
    for rectangle in lay["defense_pads_m"]:
        box(rectangle, "#8b7045", "#d2b780")
    d.poly([pt(*p) for p in lay["pasture_polygon_m"]], "#668d62", "#a0c08c", 2)
    box(lay["shelter_rect_m"], "#b5a379", "#efe1b0")
    d.text(*pt(45, 71), "NƠI TRÚ ĐÀN", 19, INK, True)
    for x, z in lay["shelter_entries_m"]:
        a, b = pt(x, z), pt(x, z+6)
        d.line([a, b], WHITE, 6)
    d.line([pt(*p) for p in lay["herd_loop_m"]], "#d5d2a0", 7)
    for approach in lay["shelter_approach_routes_m"]:
        d.line([pt(*p) for p in approach], "#d5d2a0", 7)
    d.line([pt(*p) for p in lay["service_route_m"]], CYAN, 4, "8 7")
    for item in lay["service_reservations"]:
        box(item["reservation_rect_m"], "#665e72", PURPLE)
    for i, (x, z) in enumerate(lay["portal_search_pockets_m"], 1):
        px, py = pt(x, z)
        d.circle(px, py, 15, "#526b60", PURPLE)
        d.text(px-9, py+6, f"{i}", 17, WHITE)
    for i, (x, z) in enumerate(lay["entry_candidates_m"], 1):
        px, py = pt(x, z)
        d.circle(px, py, 9, "#d1906c")
        d.text(px+13, py+6, f"E{i}", 17, "#dfad8b")
    d.line([pt(-22, -22), pt(142, -22), pt(142, 112), pt(-22, 112), pt(-22, -22)], "#839a8c", 2, "8 7")
    d.line([pt(*p) for p in lay["forest_path_m"]], GREEN, 5)
    d.text(*pt(119, 115), "RỪNG ↑", 17, GREEN, True)
    box([56, 90, 8, 20], "#927a4f", GOLD)
    d.text(*pt(38, 116), "CORE ↑", 18, GOLD, True)
    d.line([pt(57, 90), pt(63, 90)], WHITE, 6)
    d.line([pt(57, 0), pt(63, 0)], CYAN, 6)
    d.line([pt(0, 46), pt(0, 50)], PURPLE, 6)
    d.text(*pt(45, -12), "CHỢ ↓", 20, CYAN, True)
    d.text(*pt(-29, 54), "LÀNG ←", 17, PURPLE, True)
    d.text(*pt(36, 43), "ĐỒNG CỎ MỞ", 25, WHITE, True)
    ratio = verification["farm_metrics"]["pasture_fraction"]
    d.text(*pt(35, 36), f"{ratio:.1%} diện tích trong rào", 18, WHITE)
    for x in (0, 60, 120):
        d.text(*pt(x-2, -5), str(x), 15, MUTED)
    d.text(70, 1060, "0", 16, MUTED)
    d.line([(95, 1050), (195, 1050)], WHITE, 4)
    d.text(205, 1060, "20 m", 16, MUTED)
    d.text(70, 1100, "Lưới mảnh: 10 m / Ô Đất kinh tế", 17, MUTED)
    d.text(725, 1095, "BẮC ↑  /  ĐÔNG →", 18, GOLD, True)
    d.line([(1050, 125), (1050, 1120)], "#39505a", 2)
    x, y = 1090, 160
    d.text(x, y, "GIỮ KHÔNG GIAN CHO GAMEPLAY", 22, GOLD, True)
    items = [(GREEN, "Chăn thả ≥35%", "Vòng đàn rộng ≥6 m; nơi trú có hai lối ≥6 m. Không dùng đồi/hồ để khóa đường vòng."),
             (GOLD, "Hai lớp phòng thủ", "Envelope rộng12 m; pad xây màu đất. Vành liên tục chiếm41.33%, chưa khớp20–25%: C05 còn mở."),
             (CYAN, "Dịch vụ do người chơi đặt", "Đường dịch vụ riêng tới cổng Tây. Cách ly/Xử lý là vị trí ứng viên, không khu cố định bắt buộc."),
             (PURPLE, "Bốn túi tìm Cổng ẩn", "Ứng viên debug; phải tìm vị trí cách công trình≥6 m, ngoài lối đàn. Khi kín dùng lối vành ngoài."),
             ("#dfad8b", "8 điểm vào / 4 khu hướng", "Chỉ là điểm ứng viên. Runtime kiểm tra cách đơn vị≥25 m và ngoài camera trước spawn."),
             (MUTED, "Chưa phải nghiệm thu", "Chưa chạy AI, throughput150heo, phân bố3hướng hoặc timing chuyển scene. Không sửa rào hay PPU.")]
    for color, title, body in items:
        y += 45
        d.circle(x+7, y-7, 6, color)
        d.text(x+25, y, title, 21, color, True)
        y = paragraph(d, x+25, y+30, body, 43, 18, MUTED, 26) + 8
    d.text(45, 1180, "Nguồn: mechanics p00658–p00749, p00776–p00861; technical p00511. Ranh giới gộp Core và tỷ lệ dải: xem conflict ledger.", 16, MUTED)
    d.save("farm-main-plan")


def site_plans(contract: dict):
    d = Drawing(1800, 1450)
    d.rect(0, 0, 1800, 1450, INK)
    d.text(45, 62, "BỐN MAP RIÊNG  /  MỖI NƠI CÓ ĐƯỜNG ĐI VÀ ĐỜI SỐNG RIÊNG", 32, GOLD, True)
    d.text(45, 101, "Các hình chữ nhật là khoảng dành cho công trình + sân tiếp cận; không phải footprint cân bằng mới.", 20, MUTED)
    positions = [(45, 145), (930, 145), (45, 780), (930, 780)]
    for scene, (cx, cy) in zip(contract["scenes"][1:], positions):
        color = {"Farm_Core": GOLD, "Hub_Village": PURPLE, "Market_Town": CYAN, "Forest_Primeval": GREEN}[scene["id"]]
        d.rect(cx, cy, 825, 590, PANEL, color, 16, 2)
        d.text(cx+25, cy+42, scene["id"], 29, color, True)
        d.text(cx+25, cy+74, scene["name"], 19, MUTED)
        w, h = scene["dimensions_m"]
        scale = min(730/w, 380/h)
        ox, oy = cx+45+(730-w*scale)/2, cy+500
        def pt(x, z):
            return ox+x*scale, oy-z*scale
        d.rect(ox, oy-h*scale, w*scale, h*scale, "#2a433d", "#647c70")
        for path in scene["layout"]["paths_m"]:
            d.line([pt(*p) for p in path], "#d4cba2", 8)
        for item in scene["layout"]["pads"]:
            x, z, rw, rh = item["reservation_rect_m"]
            px, py = pt(x, z+rh)
            d.rect(px, py, rw*scale, rh*scale, "#435461", color, 3)
            lines = textwrap.wrap(item["name"], max(9, int(rw*scale/10)))
            for index, line in enumerate(lines):
                d.text(px+5, py+22+index*22, line, 17, WHITE, True)
        ex, ez = scene["layout"]["entry_m"]
        px, py = pt(ex, ez)
        d.circle(px, py, 9, color)
        d.text(cx+25, cy+540, "Lối vào: " + ("Đông (cổng làng)" if scene["id"] == "Hub_Village" else "Bắc" if scene["id"] == "Market_Town" else "Nam"), 19, color)
        d.text(cx+25, cy+570, f"{w}×{h} m  •  " + ("tách Core: cần ADR ranh giới" if scene["id"] == "Farm_Core" else "kích thước đề xuất của sổ tay"), 18, MUTED)
    d.text(45, 1417, "Nguồn: technical p00482, p00494, p00500, p00513–p00515, p00639–p00641, p00665–p00671. Bố trí pad/path: đề xuất.", 16, MUTED)
    d.save("scene-site-plans")


def repo_observations() -> dict:
    path = ROOT / "Assets/Scripts/Core/CameraAndWorldLayoutSystem.cs"
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    needles = ("enum WorldMapZoneId", "FarmMain =", "KhuNPC =", "ChoThiTran =", "RungNguyenSinh =",
               "PortalId =", "MaxAllowedPigRoamingAreaRatio", "CurrentMapZone == WorldMapZoneId.FarmMain")
    observations = [{"line": i, "text": line.strip()} for i, line in enumerate(lines, 1) if any(n in line for n in needles)]
    return {"file": path.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            "read_only_observations": observations, "not_authority_over_GDD": True}


def write_documents(contract: dict, verification: dict, observations: dict):
    refs = verification.pop("source_evidence")
    write_json("source-evidence.json", refs)
    write_json("world-scene-contract.json", contract)
    write_json("verification.json", verification)
    write_json("repo-observations.json", observations)
    lines = ["# Pig Tycoon — hợp đồng không gian nhiều scene", "",
             "Bản đề xuất đã tạo sơ đồ và dữ liệu để review; chưa sửa hay chạy scene Unity. Mọi tọa độ pad/path là lựa chọn blockout, không là số cân bằng mới.", "",
             "## Quyết định bố cục", "",
             "Farm_Main là trung tâm gameplay; Farm_Core riêng phía Bắc qua hành lang cuối tuyến. Hub_Village là map làng riêng ở triền hậu cảnh Tây-Bắc, đi qua cổng Tây. Chợ đi trực tiếp từ cổng Nam; rừng đi qua lối vành ngoài Bắc. Không gộp làng vào Core hoặc nhét chợ/rừng vào sân trại.", "",
             "Đặt làng trên lớp cảnh phía sau đáp ứng ý đồ thị giác của yêu cầu mới. Hướng cổng Tây vẫn là topology theo sổ tay. Vista chỉ là proxy cảnh xa; nhà dân tương tác/NPC thực nằm ở scene làng. Đây là diễn giải được ghi rõ, không tuyên bố người dùng đã thay hướng cổng thành Bắc.", "",
             "Nguồn: technical p00504, p00511, p00515, p00694–p00717; mechanics p00668–p00693. Các mã nguồn đầy đủ và trích đoạn nằm trong source-evidence.json.", "",
             "## Scene và vai trò", "", "| Scene | Kích thước | Vai trò | Nguồn |", "|---|---|---|---|"]
    for scene in contract["scenes"]:
        lines.append(f"| {scene['id']} | {'×'.join(map(str, scene['dimensions_m']))} m; {scene['dimension_status']} | {scene['role']} | {', '.join(scene['sources'])} |")
    lines += ["", "## Các luật giữ nguyên", "",
              "- Năm scene thường trực; 12 cảnh sự kiện cho14 mẫu. Hai sự kiện phòng thủ chơi ngay tại trại.",
              "- Người chơi đi hai chiều cả bốn tuyến. Heo không chuyển scene. Quái của đợt chỉ qua hành lang từ Farm_Main sang Farm_Core.",
              "- Core simulation không reset khi đổi scene; thời gian, đàn, kinh tế và thời tiết tiếp tục. ≤1.5s và fade~0.5s là yêu cầu chưa đo trong bản vẽ.",
              "- Bảy phòng Core có chỗ riêng và đường tiếp cận; số cấp kho chưa được script sáng tác. Footprint nhà cuối lấy catalog, pad hiện tại gồm sân và khoảng tiếp cận.",
              "- 32PPU, Unity6000.6.3f1, rào16hướng và hai nhân vật An/Khoa giữ nguyên. Không sửa game balance.", "",
              "## Ràng buộc Farm_Main và giới hạn kiểm tra", ""]
    metrics = verification["farm_metrics"]
    lines += [f"Pasture polygon dự kiến: {metrics['pasture_area_m2']:.0f}m² / {metrics['bounds_area_m2']:.0f}m² = {metrics['pasture_fraction']:.2%}. Đây là diện tích vùng dành chăn thả, không số Ô Đất đã mua.", "",
              f"Pad phòng thủ dự kiến: {metrics['defense_pad_area_m2']:.0f}m² = {metrics['defense_pad_fraction']:.2%}. Envelope liên tục12m chiếm {metrics['continuous_12m_envelope_fraction']:.2%}; không thể gọi tỷ lệ pad là bằng chứng dải đầy đủ đạt20–25%. C05 giữ mở.", "",
              "Cách ly/Xử lý là vùng ứng viên do người chơi chỉ định, có thể thay chỗ/chiếm chỗ pad phòng thủ. Các footprint này không được cộng hai lần để tuyên bố đạt diện tích. Lối vòng đàn tối thiểu6m, hai cửa nơi trú≥6m, bốn túi tìm Cổng ẩn,8 điểm vào chia4 hướng và đường dịch vụ có trong JSON. Chưa chứng minh bằng navmesh rằng toàn bộ đường đi/vùng cấm đã hợp lệ. Portal pocket là miền tìm kiếm; vị trí spawn cuối vẫn phải cách nhà≥6m và ngoài đường đàn.", "",
              "## Conflict ledger", "", "| ID | Trạng thái | Vấn đề / quyết định | Nguồn |", "|---|---|---|---|"]
    for item in contract["conflicts"]:
        lines.append(f"| {item['id']} | {item['status']} | **{item['title']}** — {item['decision']} | {', '.join(item['sources'])} |")
    lines += ["", "## Repo hiện tại đã đọc (không sửa)", "", f"File: `{observations['file']}`; SHA256 `{observations['sha256']}`.", ""]
    for observation in observations["read_only_observations"]:
        lines.append(f"- Dòng {observation['line']}: `{observation['text']}`")
    lines += ["", "ADR-0005 đã đối chiếu: art Prototype giữ32PPU; snap2m chưa được áp lên rào/máng1m. Không dùng sơ đồ mới để rescale asset hoặc sửa rào hiện hành.", "",
              "## Handoff triển khai sau review", "",
              "1. Chốt ADR ranh giới gộp Core và cách tính dải phòng thủ; giữ số GDD.",
              "2. Bổ sung Farm_Core vào enum/scene loader và graph gate bằng task riêng; sửa tuyến chợ theo nguồn đã thống nhất.",
              "3. Dựng từng scene blockout đúng cổng/spawn; đặt bảy kiến trúc chức năng riêng, nhà làng và quầy chợ trong scene tương ứng.",
              "4. Lắp asset từ nhánh art theo pad đặt chỗ, không biến pad thành footprint cân bằng; kho gắn mức nâng cấp thực đã xác minh.",
              "5. Kiểm tra runtime chuyển người/quái/cấm heo, persistence, gate throughput150heo,3 hướng tấn công, spawn, camera và tải; đính kèm screenshot ngày/đêm.", "",
              "## Kiểm tra đã chạy", "", f"Static artifact checks: {sum(c['passed'] for c in verification['checks'])}/{len(verification['checks'])}. Xem verification.json.",
              "Không dùng kết quả static để tuyên bố Unity đạt nghiệm thu. Các kiểm tra chưa chạy được liệt kê trong verification.json.", "",
              "Tài liệu thứ tư `Bản kỹ thuật agent.docx` đã đọc trọn27đoạn; chỉ bổ sung phân loại chứng cứ trong `assurance`, không đổi graph, kích thước, cấp kho hay quyền của AGENTS. Xem `agent-document-reading-report.md`.", "",
              "## Tái tạo", "", "`python Tools/review_world_scene_layout.py`", "",
              "Đầu ra chỉ ở `.collab/artifacts/world-scene-layout-codex/`. Script đọc bản trích tài liệu và một file Core, không sửa scene/code của game."]
    (OUT / "world-scene-layout.md").write_text("\n".join(lines)+"\n", encoding="utf-8")
    cards = "".join(f'<section><h2>{html.escape(title)}</h2><img src="{name}.svg" alt="{html.escape(title)}"></section>' for name, title in
                    [("world-overview", "Đồ thị thế giới: năm scene"), ("farm-main-plan", "Farm_Main: khoảng trống cho gameplay"), ("scene-site-plans", "Bốn scene riêng: bố cục đề xuất")])
    page = '<!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
    page += '<title>Pig Tycoon — World scene layout</title><style>body{background:#101f2b;color:#f3edda;font:18px Segoe UI,Arial;margin:30px auto;max-width:1800px;padding:0 22px}h1{color:#e6bd70}a{color:#78bacb}img{width:100%;height:auto}section{margin:45px 0}p{max-width:1000px;line-height:1.6}</style>'
    page += '<h1>Pig Tycoon — không gian nhiều scene</h1><p>Bản đề xuất có nguồn để review. Làng riêng ở lớp cảnh phía sau, tuyến nối vẫn qua cổng Tây. Sơ đồ không chứng minh nghiệm thu Unity.</p>'
    page += '<p><a href="world-scene-layout.md">Đặc tả + conflict ledger</a> · <a href="world-scene-contract.json">JSON contract</a> · <a href="source-evidence.json">Nguồn đoạn văn</a> · <a href="verification.json">Kiểm tra tĩnh</a> · <a href="agent-document-reading-report.md">Tài liệu kỹ thuật agent mới</a></p>' + cards + '</html>'
    (OUT / "review.html").write_text(page, encoding="utf-8")


def write_json(name: str, value: dict):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-dir", type=Path, default=DEFAULT_SOURCES)
    args = parser.parse_args()
    processes = subprocess.run(["tasklist", "/fo", "csv", "/nh"], capture_output=True, text=True, check=True).stdout
    if re.search(r'^"Unity\.exe"', processes, re.MULTILINE | re.IGNORECASE):
        raise SystemExit("Unity Editor is running; close it before the review generator runs.")
    OUT.mkdir(parents=True, exist_ok=True)
    contract = build_contract()
    verification = verify(contract, evidence(args.source_dir))
    if not verification["static_checks_passed"]:
        write_json("verification.json", verification)
        raise SystemExit("Static contract checks failed; inspect verification.json")
    overview(contract)
    farm_plan(contract, verification)
    site_plans(contract)
    write_documents(contract, verification, repo_observations())
    for path in OUT.glob("*.svg"):
        ET.parse(path)
    print(json.dumps({"output_dir": str(OUT), "checks_passed": len(verification["checks"]),
                      "persistent_scenes": len(contract["scenes"]), "transitions": len(contract["transitions"]),
                      "files": sorted(path.name for path in OUT.iterdir() if path.is_file()),
                      "Unity_launched": False, "scene_files_modified": False}, ensure_ascii=True))


if __name__ == "__main__":
    main()
