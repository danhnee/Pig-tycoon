"""Blender material contract shared by full-map and isolated Zone05 builds."""
from map_materials import create_atlas_material


def create_observatory_material(atlas_path, emission_path):
    return create_atlas_material('Mat_Observatory_Reference_Atlas',atlas_path,emission_path,
                                 roughness=.64,emission_strength=.9)
