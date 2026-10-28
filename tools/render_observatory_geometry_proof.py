"""Render actual Blender clay and triangle-wire proofs from the saved source asset."""
from pathlib import Path
import hashlib,json,math
import bpy
from mathutils import Vector

root=Path(__file__).resolve().parent.parent
source=root/'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/ResearchObservatory_Editable.blend'
out=root/'.collab/artifacts/zone05-20261018'
bpy.ops.wm.open_mainfile(filepath=str(source))
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=6
scene.render.resolution_x=1200;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
clay=bpy.data.materials.new('Proof_Clay');clay.use_nodes=True
clay.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.42,.44,.48,1)
clay.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.8
scene.view_layers[0].material_override=clay
scene.render.filepath=str(out/'11-blender-clay-front.png');bpy.ops.render.render(write_still=True)
wire=clay.copy();wire.name='Proof_Geometry_Wire'
nodes=wire.node_tree.nodes;links=wire.node_tree.links
edge=nodes.new('ShaderNodeWireframe');edge.use_pixel_size=True;edge.inputs['Size'].default_value=.65
mix=nodes.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(.64,.69,.76,1);mix.inputs[2].default_value=(.012,.018,.028,1)
links.new(edge.outputs['Fac'],mix.inputs[0]);links.new(mix.outputs[0],nodes['Principled BSDF'].inputs['Base Color'])
scene.view_layers[0].material_override=wire
scene.render.filepath=str(out/'12-blender-wireframe-front.png');bpy.ops.render.render(write_still=True)
scene.camera.location=(6,9,6);scene.camera.rotation_euler=(Vector((0,0,1.65))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(out/'13-blender-wireframe-rear.png');bpy.ops.render.render(write_still=True)
report={'blender_version':bpy.app.version_string,'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
        'mesh_objects':sum(o.type=='MESH' for o in scene.objects),'method':'Cycles material override: clay and ShaderNodeWireframe from real triangle geometry; no image postprocessing or viewport screenshot claim'}
(out/'geometry-proof.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('GEOMETRY_PROOFS_RENDERED',json.dumps(report),flush=True)
