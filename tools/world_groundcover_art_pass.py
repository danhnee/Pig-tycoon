"""Dense low botanical detail on the original 62x47 world contract.

Integration: build_world_groundcover(collection). No scene/source modifications
outside the supplied collection; no building moves; no gameplay colliders.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import math
import re
from pathlib import Path
import sys

import numpy as np

from map_surface_contract import (sample_ground_z, eval_road_mask, eval_pond_distances,
                                  eval_river_center_and_width, island_signed_distance)

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '.collab/artifacts/world-groundcover-sol-high'
CACHE = ROOT / 'Tools/quaternius_nature_cache.npz'
ART_PASS_VERSION = 'v4-segmented-arching-botanical-blades'
PALETTE = [
    (.22, .29, .105, 1), (.30, .37, .13, 1), (.42, .45, .16, 1),
    (.42, .235, .035, 1), (.57, .32, .045, 1), (.69, .44, .085, 1),
    (.36, .42, .22, 1), (.20, .32, .18, 1), (.43, .285, .12, 1),
    (.82, .49, .065, 1), (.90, .69, .22, 1), (.74, .42, .56, 1),
    (.58, .55, .78, 1), (.33, .40, .15, 1), (.37, .235, .10, 1),
]
KEEP_OUTS = [(-17.8, 5.4, 2.3), (-17., 9.2, 3.7), (-13.4, 9.4, 3.2),
            (-7.8, 6.4, 3.6), (-1.8, 7.6, 2.9), (8.6, 4.6, 2.7)]


def should_exclude_primitive_tree(partname):
    """Filter OLD generator parts before adding real textured trees; no building timber filter."""
    tokens = re.split(r'[^a-z0-9]+', str(partname).casefold())
    if any(token.startswith(('barrel', 'cask', 'crate')) for token in tokens):
        return False
    return any(token.startswith(('tree', 'pine', 'oak', 'birch', 'maple', 'conifer',
                                 'canopy', 'foliage', 'trunk', 'bough')) for token in tokens)


def should_exclude_primitive_groundcover(partname):
    name = str(partname).casefold()
    return any(token in name for token in ('meadow_grass', 'grass_batch', 'grass_tuft',
                                           'golden_grass', 'grass_blade', 'agave', 'spike_grass',
                                           '_lotus', 'lotus_', 'rim_top_lip_flora',
                                           'meadow_wildflower_shrub', 'bank_shrub'))


def build_world_tree_clusters(collection, seed=260219, count=72, extra_keepouts=(), height_sampler=sample_ground_z):
    """Place actual textured branched native trees in irregular edge groves.

    No cone/icosphere crown generation. Source templates are removed after linked
    instances are made, preventing hidden origin trees from leaking into exports.
    """
    import bpy
    source = ROOT / 'Tools/farm_landscape_dressing_codex.py'
    spec = importlib.util.spec_from_file_location('world_native_tree_source_owner', source)
    dressing = importlib.util.module_from_spec(spec);spec.loader.exec_module(dressing)
    dressing.OUT = OUT / 'native-tree-source'
    dressing.OUT.mkdir(parents=True, exist_ok=True)
    kit = dressing.DressKit()
    # Keep temporary imports in the caller-owned collection, then remove them.
    if kit.template_collection.name in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.unlink(kit.template_collection)
    collection.children.link(kit.template_collection)
    rng = np.random.default_rng(seed)
    groves = [(-24., 3.5, 1.1, 2.4), (-22., 8.8, 1.6, 2.1), (-18.5, 12.4, 2.0, 1.2),
              (-13., 13.0, 1.9, 1.0), (-7.0, 12.2, 1.5, .8), (-2., 12.8, 1.4, .8),
              (-23.0, -5.8, 1.0, 1.5), (-19.8, -11.0, 1.2, 1.1),
              (11.5, -12.8, 1.1, .7), (14., -10.2, .8, .9),
              (-19., 15., 1.6, .9), (-11., 15.3, 1.8, .8), (-2., 15.1, 1.8, .8),
              (12.5, 14.8, .7, 1.1)]
    species = ['MapleTree_2', 'MapleTree_4', 'BirchTree_2', 'NormalTree_3']
    objects, placements = [], []
    for attempt in range(count * 35):
        if len(placements) >= count:
            break
        gx, gy, sx, sy = groves[int(rng.integers(0, len(groves)))]
        x, y = gx + rng.normal(0, sx), gy + rng.normal(0, sy)
        water = min(float(d) for d in eval_pond_distances(x, y)) < 1.30
        if -8.8 < y < 12.9:
            river_x, river_w = eval_river_center_and_width(y)
            water = water or abs(x-float(river_x)) < float(river_w)*1.20
        keepouts = [*KEEP_OUTS, (7.2, 15.1, 4.6), *extra_keepouts]
        if (float(island_signed_distance(x, y)) > .94 or water
                or any((x-cx)**2+(y-cy)**2 < radius**2 for cx, cy, radius in keepouts)
                or float(eval_road_mask(x, y)) > .18):
            continue
        # Trees have real footprints: preserve open routes, doors and neighbouring crowns.
        if any((x-p['x'])**2 + (y-p['y'])**2 < p['clearance']**2 for p in placements):
            continue
        if y < -8:
            height = float(rng.uniform(1.65, 2.9))
        else:
            height = float(rng.uniform(2.5, 4.6))
        name = str(rng.choice(species, p=[.28, .16, .34, .22]))
        made = kit.place(name, (float(x), float(y)), height_sampler, height, collection,
                         seed=len(placements), yaw=float(rng.uniform(0, math.tau)), sink=.035)
        for obj in made:
            obj.name = 'World_NativeTree_' + obj.name
            obj['source_textured_branch_tree'] = True
            obj['visual_only_nonblocking'] = False
        objects.extend(made)
        placements.append({'x': float(x), 'y': float(y), 'height': height, 'clearance': height * .30, 'source': name})
    for templates, _ in kit.templates.values():
        for template in templates:
            bpy.data.objects.remove(template, do_unlink=True)
    bpy.data.collections.remove(kit.template_collection)
    report = {'art_pass_version': ART_PASS_VERSION, 'tree_count': len(placements), 'mesh_instances': len(objects), 'placements': placements,
              'sources': kit.sources, 'source_policy': 'Real textured Maple/Birch/NormalTree branch and leaf meshes; no primitive crowns or conifer cones.',
              'colliders_added': False, 'geometry_outside_collection_changed': False}
    return {'objects': objects, 'report': report}


class Batch:
    _leaf_topology = {}

    def __init__(self):
        self.vertices, self.faces, self.materials = [], [], []
        self.vertex_count = 0

    def add(self, vertices, faces, material):
        vertices = np.asarray(vertices, dtype=np.float32)
        faces = np.asarray(faces, dtype=np.int32)
        self.vertices.append(vertices)
        self.faces.append(faces + self.vertex_count)
        self.materials.append(np.full(len(faces), material, dtype=np.int16) if np.isscalar(material)
                              else np.asarray(material, dtype=np.int16))
        self.vertex_count += len(vertices)

    def leaf(self, base, angle, length, width, lean, material):
        """Thin curved lamina, 7–10 longitudinal segments with a slight central fold."""
        direction = np.array([math.cos(angle), math.sin(angle), 0.])
        side = np.array([-direction[1], direction[0], 0.])
        p = np.asarray(base)
        segments = int(np.clip(round(7 + (length - .14) * 16), 7, 10))
        arc = .65 + .10 * math.sin(angle * 1.7)
        t = np.arange(1, segments, dtype=float) / segments
        half_width = width * .5 * np.sin(math.pi*t)**.72 * (1-.20*t)
        centre = (p + np.outer(lean*t**1.7, direction)
                  + np.outer(width*.9*np.sin(math.pi*t)*np.sin(angle+t*2.8), side))
        centre[:,2] += length*np.sin(math.pi*arc*t)
        twist = .32*np.sin(angle+t*2.6)
        edge_axis = np.outer(np.cos(twist), side);edge_axis[:,2] += np.sin(twist)
        ridge = centre.copy();ridge[:,2] += width*.13*np.sin(math.pi*t)
        rings = np.stack([centre-edge_axis*half_width[:,None], ridge,
                          centre+edge_axis*half_width[:,None]], axis=1).reshape((-1,3))
        tip = p + direction * lean + [0,0,length * math.sin(math.pi * arc)]
        vertices = np.vstack([p, rings, tip])
        if segments not in self._leaf_topology:
            faces = [[0, 1, 2], [0, 2, 3]]
            for row in range(segments-2):
                a = 1+row*3;b = a+3
                faces.extend([[a,b,b+1],[a,b+1,a+1],[a+1,b+1,b+2],[a+1,b+2,a+2]])
            last = 1+(segments-2)*3;tip_index = len(vertices)-1
            faces.extend([[last,tip_index,last+1],[last+1,tip_index,last+2]])
            self._leaf_topology[segments] = np.asarray(faces, dtype=np.int32)
        faces = self._leaf_topology[segments]
        self.add(vertices, faces, material)

    def arrays(self):
        return {'positions': np.concatenate(self.vertices) if self.vertices else np.empty((0,3),dtype=np.float32),
                'triangles': np.concatenate(self.faces) if self.faces else np.empty((0,3),dtype=np.int32),
                'material_indices': np.concatenate(self.materials) if self.materials else np.empty(0,dtype=np.int16)}


def valid_point(x, y, extra_keepouts=()):
    if float(island_signed_distance(x, y)) > .94 or y > 13.2:
        return False
    if min(float(p) for p in eval_pond_distances(x, y)) < 1.19:
        return False
    if -8.8 < y < 12.9:
        centre, width = eval_river_center_and_width(y)
        if abs(x - float(centre)) < float(width) * 1.16:
            return False
    grass_keepouts = [(cx, cy, r if (cx, cy) == (-7.8, 6.4) else r * .76) for cx, cy, r in KEEP_OUTS]
    return not any((x - cx) ** 2 + (y - cy) ** 2 < r * r for cx, cy, r in [*grass_keepouts, *extra_keepouts])


def patch_field(x, y):
    return np.clip(.47 + .23 * np.sin(x * .39 + np.sin(y * .27))
                   + .19 * np.cos(y * .51 - x * .14) + .10 * np.sin(x * 1.13 + y * .93), .08, .95)


def generate_groundcover_arrays(seed=260119, density=1.0, extra_keepouts=()):
    rng = np.random.default_rng(seed)
    grasses, herbs, flowers = Batch(), Batch(), Batch()
    anchors = []
    # Unstructured stochastic patches: no lattice, rows, equal-height rods or bead rings.
    attempts = int(31000 * density)
    points = rng.uniform((-27., -19.7), (25., 13.1), size=(attempts, 2))
    acceptance = rng.random(attempts)
    field = patch_field(points[:, 0], points[:, 1])
    native = np.load(CACHE, allow_pickle=False)
    counts = {'grass_clumps': 0, 'fern_rosettes': 0, 'native_flower_clumps': 0, 'low_herb_rosettes': 0, 'fine_flower_heads': 0}
    for index, (x, y) in enumerate(points):
        if acceptance[index] > field[index] or not valid_point(float(x), float(y), extra_keepouts):
            continue
        road = float(eval_road_mask(x, y))
        z = float(sample_ground_z(x, y)) - .012
        anchors.append((x, y, z))
        colour = int(rng.choice([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10], p=[.04, .06, .06, .16, .22, .20, .04, .05, .02, .05, .10]))
        height = rng.uniform(.20, .29) * (1 - road * .70)
        for _ in range(int(rng.integers(5, 9))):
            base = (x + rng.normal(0, .055), y + rng.normal(0, .055), z)
            grasses.leaf(base, rng.uniform(0, math.tau), height * rng.uniform(.68, 1.10),
                         rng.uniform(.008, .025), height * rng.uniform(.45, 1.10), colour)
        counts['grass_clumps'] += 1
        # Broad low leaves and fern fronds interrupt narrow blade texture.
        if road < .24 and index % 43 == 0:
            counts['fern_rosettes'] += 1
            for frond in range(7):
                angle = math.tau * frond / 7 + rng.uniform(-.23, .23)
                direction = np.array([math.cos(angle), math.sin(angle), 0.])
                side = np.array([-direction[1], direction[0], 0.])
                reach = rng.uniform(.19, .38)
                for step in range(1, 8):
                    t = step / 8
                    centre = np.array([x, y, z]) + direction * reach * t + [0, 0, .13 * math.sin(t * math.pi * .85)]
                    length = .09 * math.sin(t * math.pi) + .012
                    for sign in [-1, 1]:
                        herbs.leaf(centre, angle + sign*math.pi*.46,
                                   .018 + .015*math.sin(t*math.pi), .012 + .014*math.sin(t*math.pi),
                                   length, int(rng.choice([0, 1, 6, 7, 13])))
        elif road < .36 and index % 9 == 0:
            counts['low_herb_rosettes'] += 1
            for _ in range(9):
                herbs.leaf((x, y, z), rng.uniform(0, math.tau), rng.uniform(.055, .14),
                           rng.uniform(.03, .07), rng.uniform(.10, .24), int(rng.choice([0, 1, 2, 6, 7, 13])))
        if road < .22 and index % 23 == 0:
            name = 'Flower_2_Clump' if index % 2 else 'Flower_3_Clump'
            source = native[name + '_v'].copy()
            faces = native[name + '_f']
            native_height = max(.01, float(source[:, 2].max()))
            source *= rng.uniform(.11, .24) / native_height
            angle = rng.uniform(0, math.tau);c, s = math.cos(angle), math.sin(angle)
            source[:, :2] = source[:, :2] @ np.array([[c, s], [-s, c]])
            face_height = source[faces, 2].mean(axis=1)
            material = np.full(len(faces), int(rng.choice([0, 1, 2, 7])), dtype=np.int16)
            material[face_height > source[:, 2].max() * .66] = int(rng.choice([9, 10, 11, 12], p=[.46, .32, .14, .08]))
            flowers.add(source + [x, y, z], faces, material)
            counts['native_flower_clumps'] += 1
        if road < .30 and index % 3 == 0:
            # Fine five-petal infill, below the grass tips; no giant glowing heads.
            for _ in range(int(rng.integers(2, 5))):
                counts['fine_flower_heads'] += 1
                flower_x, flower_y = x + rng.normal(0, .10), y + rng.normal(0, .10)
                flower_base_z = float(sample_ground_z(flower_x, flower_y)) - .012
                centre = np.array([flower_x, flower_y, flower_base_z + rng.uniform(.055, .12)])
                petal_material = int(rng.choice([9, 10, 11, 12], p=[.35, .40, .18, .07]))
                stem_base = np.array([centre[0], centre[1], flower_base_z])
                stem_side = np.array([math.cos(index), math.sin(index), 0.]) * .0018
                flowers.add([stem_base-stem_side, stem_base+stem_side, centre-stem_side, centre+stem_side],
                            [[0, 1, 2], [1, 3, 2]], 1)
                leaf_centre = stem_base * .52 + centre * .48
                leaf_side = np.array([math.sin(index), -math.cos(index), 0.]) * .015
                flowers.add([leaf_centre, leaf_centre+leaf_side+[0,0,.009], leaf_centre+[.007,.004,.012],
                             leaf_centre-leaf_side+[0,0,.006]], [[0,1,2],[0,2,3]], 2)
                for petal in range(5):
                    angle = petal * math.tau / 5 + index * .17
                    direction = np.array([math.cos(angle), math.sin(angle), 0.])
                    side = np.array([-direction[1], direction[0], 0.])
                    tip = centre + direction * rng.uniform(.022, .038)
                    flowers.add([centre, centre + direction * .015 + side * .012, tip + [0, 0, .006],
                                 centre + direction * .015 - side * .012], [[0, 1, 2], [0, 2, 3]], petal_material)
    native.close()
    groups = {'Low_Curved_Grass_Blades': grasses.arrays(), 'Fern_And_Broadleaf_Mats': herbs.arrays(),
              'Native_Low_Flower_Clumps': flowers.arrays()}
    report = {'art_pass_version': ART_PASS_VERSION, 'generator_sha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
              'seed': seed, 'density': density, 'contract': 'original map_surface_contract 62x47; no terrain transform change',
              'counts': counts, 'anchors': np.asarray(anchors).tolist(), 'path_policy': 'roadmask shortens noncolliding blades; flowers/ferns stay off worn route centres',
              'source_nature_cache': str(CACHE), 'cache_sha256': hashlib.sha256(CACHE.read_bytes()).hexdigest(),
              'source_reuse': 'Verified Z-up Flower_2_Clump/Flower_3_Clump geometry; explicit botanical palette, not an assertion of original UV textures.',
              'triangles': {name: len(arrays['triangles']) for name, arrays in groups.items()}}
    return groups, report


def _material(bpy, name, colour):
    material = bpy.data.materials.new(name)
    material.diffuse_color = colour
    material.use_nodes = True
    shader = material.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = colour
    shader.inputs['Roughness'].default_value = .87
    if name.startswith('World_Bank_'):
        nodes, links = material.node_tree.nodes, material.node_tree.links
        position = nodes.new('ShaderNodeNewGeometry')
        grain = nodes.new('ShaderNodeTexNoise');grain.inputs['Scale'].default_value = 28.;grain.inputs['Detail'].default_value = 3.
        links.new(position.outputs['Position'], grain.inputs['Vector'])
        bump = nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value = .23;bump.inputs['Distance'].default_value = .018
        links.new(grain.outputs['Fac'], bump.inputs['Height']);links.new(bump.outputs['Normal'], shader.inputs['Normal'])
    return material


def apply_meadow_ground_shading(terrain_object):
    """Explicit optional root-owned terrain hook; changes shader only, no transforms/geometry."""
    import bpy
    mesh = terrain_object.data
    positions = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
    mesh.vertices.foreach_get('co', positions)
    positions = positions.reshape((-1, 3))
    matrix = np.asarray(terrain_object.matrix_world)
    world = np.c_[positions, np.ones(len(positions))] @ matrix.T
    x, y = world[:, 0], world[:, 1]
    field = np.clip(.46 + .22 * np.sin(x * .37 + np.sin(y * .41))
                    + .18 * np.cos(y * .63 - x * .27) + .09 * np.sin(x * 2.41 + y * 1.93), 0, 1)
    greens = np.array([.23, .31, .075])
    ochre = np.array([.52, .295, .045])
    warm_weight = np.clip(field * .62 + .40, 0, 1)
    colour = greens[None, :] * (1 - warm_weight[:, None]) + ochre[None, :] * warm_weight[:, None]
    soil = np.clip(.5 + .5 * np.sin(x * .19 - y * .23 + np.sin(y * .51)), 0, 1) ** 5 * .15
    colour = colour * (1 - soil[:, None]) + np.array([.34, .19, .055]) * soil[:, None]
    road = np.asarray(eval_road_mask(x, y))
    colour = colour * (1 - road[:, None] * .86) + np.array([.30, .175, .065]) * road[:, None] * .86
    indices = np.empty(len(mesh.loops), dtype=np.int32)
    mesh.loops.foreach_get('vertex_index', indices)
    corner = np.c_[colour[indices], np.ones(len(indices))].astype(np.float32)
    attribute = mesh.color_attributes.get('World_Meadow_Color') or mesh.color_attributes.new(name='World_Meadow_Color', type='FLOAT_COLOR', domain='CORNER')
    attribute.data.foreach_set('color', corner.reshape(-1))
    material = bpy.data.materials.new('World_Meadow_Ochre_Green_Soil')
    material['art_pass_version'] = ART_PASS_VERSION
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    shader = nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value = .94
    colours = nodes.new('ShaderNodeVertexColor');colours.layer_name = attribute.name
    links.new(colours.outputs['Color'], shader.inputs['Base Color'])
    position = nodes.new('ShaderNodeNewGeometry')
    noise = nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value = 18.;noise.inputs['Detail'].default_value = 2.5
    links.new(position.outputs['Position'], noise.inputs['Vector'])
    bump = nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value = .22;bump.inputs['Distance'].default_value = .025
    links.new(noise.outputs['Fac'], bump.inputs['Height']);links.new(bump.outputs['Normal'], shader.inputs['Normal'])
    mesh.materials.clear();mesh.materials.append(material)
    mesh.polygons.foreach_set('material_index', np.zeros(len(mesh.polygons), dtype=np.int32))
    return material


def build_world_groundcover(collection, seed=260119, density=1.0, include_banks=True, extra_keepouts=()):
    """Root assembler hook. Returns objects/report; writes only into collection."""
    import bpy
    groups, report = generate_groundcover_arrays(seed, density, extra_keepouts)
    materials = [_material(bpy, 'World_Botanical_' + str(i), colour) for i, colour in enumerate(PALETTE)]
    objects = []
    for name, arrays in groups.items():
        if not len(arrays['triangles']):
            continue
        mesh = bpy.data.meshes.new('World_' + name)
        mesh.from_pydata(arrays['positions'].tolist(), [], arrays['triangles'].tolist())
        mesh.update()
        assert not mesh.validate(verbose=True)
        for mat in materials: mesh.materials.append(mat)
        mesh.polygons.foreach_set('material_index', arrays['material_indices'].astype(np.int32))
        mesh.polygons.foreach_set('use_smooth', np.ones(len(mesh.polygons), dtype=np.bool_))
        obj = bpy.data.objects.new(mesh.name, mesh);collection.objects.link(obj)
        obj['visual_only_nonblocking'] = True
        obj['world_coordinates_original_contract'] = True
        objects.append(obj)
    bank_hook = OUT / 'argon-bank-details/bank_geometry.py'
    report['bank_hook_pending'] = include_banks and not bank_hook.exists()
    if include_banks and bank_hook.exists():
        try:
            if str(ROOT) not in sys.path:sys.path.insert(0, str(ROOT))
            spec = importlib.util.spec_from_file_location('world_bank_detail_owner', bank_hook)
            module = importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
            bank = module.build_bank_arrays(seed=seed)
            palette = bank.get('palette', [( .31, .33, .27, 1)])
            mesh = bpy.data.meshes.new('World_Irregular_Bank_Rocks')
            mesh.from_pydata(np.asarray(bank['positions']).tolist(), [], np.asarray(bank['triangles']).tolist());mesh.update()
            assert not mesh.validate(verbose=True)
            for i, colour in enumerate(palette):mesh.materials.append(_material(bpy, 'World_Bank_' + str(i), tuple(colour)))
            mesh.polygons.foreach_set('material_index', np.asarray(bank['material_indices'], dtype=np.int32))
            mesh.polygons.foreach_set('use_smooth', np.asarray(bank['material_indices']) < 5)
            obj = bpy.data.objects.new(mesh.name, mesh);collection.objects.link(obj);obj['visual_only_nonblocking'] = True;objects.append(obj)
            report['bank_triangles'] = len(bank['triangles'])
            report['bank_hook_pending'] = False
        except Exception as error:
            report['bank_hook_pending'] = True
            report['bank_hook_error'] = f'{type(error).__name__}: {error}'
            print('BANK HOOK NOT READY:', report['bank_hook_error'], flush=True)
    report['anchors_count'] = len(report.pop('anchors'))
    return {'objects': objects, 'report': report}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--density', type=float, default=1.0)
    parser.add_argument('--test-blend', action='store_true')
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    groups, report = generate_groundcover_arrays(density=args.density)
    flattened = {name + '__' + field: value for name, arrays in groups.items() for field, value in arrays.items()}
    flattened['anchor_positions'] = np.asarray(report['anchors'], dtype=np.float32)
    flattened['palette'] = np.asarray(PALETTE, dtype=np.float32)
    np.savez_compressed(OUT / 'world-groundcover.npz', **flattened)
    report['anchors_count'] = len(report['anchors']);report.pop('anchors')
    (OUT / 'groundcover-evidence.json').write_text(json.dumps(report, indent=2), encoding='utf8')
    print(json.dumps(report, indent=2), flush=True)
    if args.test_blend:
        sys.path.insert(0, str(ROOT / '.collab/artifacts/mountain-reference-study-codex/runtime'))
        import bpy
        source = ROOT / 'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/Farm_Main_3D_Zone05_Candidate.blend'
        bpy.ops.wm.open_mainfile(filepath=str(source))
        collection = bpy.data.collections.new('World_Groundcover_Component_Test');bpy.context.scene.collection.children.link(collection)
        build_world_groundcover(collection, density=args.density)
        terrain = bpy.context.scene.objects.get('Zone01_Terrain_GoldenMeadow_And_Paths')
        if terrain:apply_meadow_ground_shading(terrain)
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'groundcover-integration-test.blend'))
        scene = bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=4
        scene.render.resolution_x=1440;scene.render.resolution_y=960;scene.render.resolution_percentage=100
        scene.render.filepath=str(OUT / '01-wholemap-component-integration.png');bpy.ops.render.render(write_still=True)


if __name__ == '__main__':
    main()
