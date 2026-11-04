"""Atlas-backed Principled materials shared by map generation and proof renders."""
import bpy


def create_atlas_material(name,albedo_path,emission_path,roughness=.72,emission_strength=.9,metallic=0.):
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;bsdf=nodes.get('Principled BSDF')
    bsdf.inputs['Roughness'].default_value=roughness
    bsdf.inputs['Metallic'].default_value=metallic
    bsdf.inputs['Emission Strength'].default_value=emission_strength
    for path,socket in [(albedo_path,'Base Color'),(emission_path,'Emission Color')]:
        texture=nodes.new('ShaderNodeTexImage');texture.image=bpy.data.images.load(str(path),check_existing=True)
        texture.extension='EXTEND';links.new(texture.outputs['Color'],bsdf.inputs[socket])
    return mat
