"""Reopen the deliverable and validate its GLB and canonical binary contract."""
from pathlib import Path
import hashlib,json,struct,sys
import bpy
import numpy as np

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'Tools'))
from observatory_geometry import build_observatory,meshbin_bytes

out=ROOT/'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail'
report=json.loads((out/'validation.json').read_text())
geometry=build_observatory()
arrays=geometry.arrays((-7.8,6.4,3.323293447494507),yaw=-.05)
assert hashlib.sha256(meshbin_bytes(*arrays)).hexdigest()==report['meshbin_sha256']
binary=(out/'Zone05_Building_NhaNghienCuu_ResearchObservatory.meshbin').read_bytes()
assert hashlib.sha256(binary).hexdigest()==report['meshbin_sha256']
nv,ni=struct.unpack_from('<ii',binary)
assert len(binary)==8+nv*32+ni*4 and ni//3==report['triangles']
normals=np.frombuffer(binary,dtype='<f4',count=nv*3,offset=8+nv*12).reshape(-1,3)
assert np.allclose(np.linalg.norm(normals,axis=1),1,atol=1e-5)

data=(out/'ResearchObservatory.glb').read_bytes()
magic,version,length=struct.unpack_from('<III',data)
assert magic==0x46546c67 and version==2 and length==len(data)
size,kind=struct.unpack_from('<II',data,12);assert kind==0x4e4f534a
gltf=json.loads(data[20:20+size].decode('utf-8'))
glb_triangles=0
for mesh in gltf['meshes']:
    for primitive in mesh['primitives']:
        assert primitive.get('mode',4)==4 and 'NORMAL' in primitive['attributes']
        assert 'TEXCOORD_0' in primitive['attributes'] and 'material' in primitive
        glb_triangles+=gltf['accessors'][primitive['indices']]['count']//3
assert glb_triangles==report['triangles']
assert len(gltf['images'])==2 and all('bufferView' in image for image in gltf['images'])

bpy.ops.wm.open_mainfile(filepath=str(out/'ResearchObservatory_Editable.blend'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
blend_triangles=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
assert blend_triangles==report['triangles'] and len(meshes)==138
assert all(image.packed_file is not None for image in bpy.data.images if image.source=='FILE')
result={'canonical_binary_matches_candidate':True,'explicit_normals_unit_length':True,
        'glb_triangles':glb_triangles,'glb_textures_embedded':2,'editable_blend_objects':len(meshes),
        'editable_blend_triangles':blend_triangles,'blend_textures_packed':True,
        'unity_runtime_verified':False}
(out/'package-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('OBSERVATORY_PACKAGE_VERIFIED',json.dumps(result),flush=True)
