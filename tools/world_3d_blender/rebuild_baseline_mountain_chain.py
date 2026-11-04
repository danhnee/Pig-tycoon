"""Zone10-only visual candidate. Never writes Assets/ArtSource or runs the old builder.

Run with the isolated bpy runtime using Python, or Blender --background --python.
The old builder is parsed for five pure forest helpers; its module is never imported.
"""
from pathlib import Path
import argparse
import ast
import hashlib
import json
import math
import struct
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.collab/artifacts/mountain-reference-study-codex/runtime'))
sys.path.insert(0, str(ROOT / 'Tools'))
import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from map_surface_contract import sample_ground_z, strip_uv, island_signed_distance

SOURCE = ROOT / 'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/Farm_Main_3D_Zone05_Candidate.blend'
SOURCE_HASH = '4d5969385ab5f5b01326c0b9858223dbec83f00ff3dbfea7f6f8da2de89ad757'
DEFAULT_OUT = ROOT / '.collab/artifacts/landscape-preserve-baseline-argon-v1'
ZONE10 = 'Zone10_Vista_AlpineMountains_And_ForestRidge'


def log(message):
    print('[Zone10] ' + message, flush=True)


def hash_array(data, collection, prop, width, dtype):
    values = np.empty(len(collection) * width, dtype=dtype)
    collection.foreach_get(prop, values)
    data.update(values.tobytes())


def material_signature(material):
    result = {'name': material.name, 'diffuse': list(material.diffuse_color),
              'use_nodes': material.use_nodes}
    if material.use_nodes:
        result['nodes'] = []
        for node in material.node_tree.nodes:
            inputs = {}
            for socket in node.inputs:
                if hasattr(socket, 'default_value'):
                    value = socket.default_value
                    inputs[socket.identifier] = list(value) if hasattr(value, '__len__') else value
            result['nodes'].append({'name': node.name, 'type': node.bl_idname, 'inputs': inputs,
                                    'image': node.image.name if getattr(node, 'image', None) else None})
        result['links'] = sorted((l.from_node.name, l.from_socket.identifier,
                                  l.to_node.name, l.to_socket.identifier)
                                 for l in material.node_tree.links)
    return result


def fingerprints():
    result = {}
    for obj in bpy.data.objects:
        if not obj.name.startswith('Zone') or obj.type != 'MESH':
            continue
        mesh = obj.data
        digest = hashlib.sha256()
        hash_array(digest, mesh.vertices, 'co', 3, np.float32)
        hash_array(digest, mesh.loops, 'vertex_index', 1, np.int32)
        hash_array(digest, mesh.polygons, 'loop_start', 1, np.int32)
        hash_array(digest, mesh.polygons, 'loop_total', 1, np.int32)
        hash_array(digest, mesh.polygons, 'material_index', 1, np.int32)
        hash_array(digest, mesh.polygons, 'use_smooth', 1, np.bool_)
        for layer in mesh.uv_layers:
            digest.update(layer.name.encode())
            hash_array(digest, layer.data, 'uv', 2, np.float32)
        result[obj.name] = {'mesh_uv_bindings_sha256': digest.hexdigest(),
                            'matrix_world': [list(row) for row in obj.matrix_world],
                            'materials': [material_signature(m) for m in mesh.materials],
                            'hide_render': obj.hide_render}
    return result


def preserve_zone11_in_glb(path):
    """Keep inherited duplicate/degenerate triangles that validation silently drops.

    The source contains repeated decorative triangles and zero-area faces. glTF
    permits them; append exact loop-expanded position/UV arrays rather than deletion.
    This patches only Zone11's existing GLB primitive and retains its material.
    """
    raw=path.read_bytes()
    json_length,json_type=struct.unpack_from('<II',raw,12)
    if json_type!=0x4E4F534A:
        raise RuntimeError('Unexpected GLB JSON chunk')
    document=json.loads(raw[20:20+json_length])
    binary_offset=20+json_length
    binary_length,binary_type=struct.unpack_from('<II',raw,binary_offset)
    if binary_type!=0x004E4942 or len(document['buffers'])!=1:
        raise RuntimeError('Unexpected GLB binary layout')
    binary=bytearray(raw[binary_offset+8:binary_offset+8+document['buffers'][0]['byteLength']])
    node=next(node for node in document['nodes'] if node.get('name','').startswith('Zone11_'))
    glb_mesh=document['meshes'][node['mesh']]
    material=glb_mesh['primitives'][0]['material']
    mesh=next(obj.data for obj in bpy.data.objects if obj.name.startswith('Zone11_'))
    totals=np.empty(len(mesh.polygons),dtype=np.int32)
    mesh.polygons.foreach_get('loop_total',totals)
    if not np.all(totals==3):
        raise RuntimeError('Zone11 topology no longer all triangles')
    coordinates=np.empty(len(mesh.vertices)*3,dtype=np.float32)
    mesh.vertices.foreach_get('co',coordinates)
    loops=np.empty(len(mesh.loops),dtype=np.int32)
    mesh.loops.foreach_get('vertex_index',loops)
    xyz=coordinates.reshape(-1,3)[loops]
    tri=xyz.reshape(-1,3,3)
    normals=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])
    lengths=np.linalg.norm(normals,axis=1)
    degenerate=lengths<1e-10
    normals/=np.maximum(lengths[:,None],1e-10)
    normals[degenerate]=(0,0,1)
    normals=np.repeat(normals,3,axis=0)
    # Blender Z-up to glTF Y-up; UV V is inverted by Blender's exporter.
    positions=np.ascontiguousarray(np.stack((xyz[:,0],xyz[:,2],-xyz[:,1]),axis=1),dtype=np.float32)
    normals=np.ascontiguousarray(np.stack((normals[:,0],normals[:,2],-normals[:,1]),axis=1),dtype=np.float32)
    uv=np.empty(len(mesh.loops)*2,dtype=np.float32)
    mesh.uv_layers.active.data.foreach_get('uv',uv)
    uv=uv.reshape(-1,2)
    uv[:,1]=1-uv[:,1]
    existing=glb_mesh['primitives'][0]
    def current_array(index,width):
        item=document['accessors'][index]
        view=document['bufferViews'][item['bufferView']]
        if item['componentType']!=5126 or view.get('byteStride'):
            return None
        offset=view.get('byteOffset',0)+item.get('byteOffset',0)
        return np.frombuffer(binary,dtype=np.float32,count=item['count']*width,offset=offset).reshape(item['count'],width)
    current_position=current_array(existing['attributes']['POSITION'],3)
    current_uv=current_array(existing['attributes']['TEXCOORD_0'],2)
    if (len(glb_mesh['primitives'])==1 and document['accessors'][existing['indices']]['count']==len(xyz)
            and current_position is not None and np.array_equal(current_position,positions)
            and current_uv is not None and np.array_equal(current_uv,uv)):
        return {'source_triangles':len(mesh.polygons),'glb_triangles':len(xyz)//3,
                'degenerate_triangles_retained':int(degenerate.sum()),'exact_position_uv_arrays':True,
                'already_exact_no_append':True}
    del current_position,current_uv
    def accessor(values,kind,component_type=5126,target=34962):
        while len(binary)%4:binary.append(0)
        offset=len(binary)
        binary.extend(values.tobytes())
        view=len(document.setdefault('bufferViews',[]))
        document['bufferViews'].append({'buffer':0,'byteOffset':offset,'byteLength':values.nbytes,'target':target})
        data={'bufferView':view,'componentType':component_type,'count':len(values),'type':kind}
        if kind=='VEC3' and values is positions:
            data['min']=values.min(axis=0).tolist();data['max']=values.max(axis=0).tolist()
        index=len(document.setdefault('accessors',[]))
        document['accessors'].append(data)
        return index
    attributes={'POSITION':accessor(positions,'VEC3'),'NORMAL':accessor(normals,'VEC3'),
                'TEXCOORD_0':accessor(uv,'VEC2')}
    indices=accessor(np.arange(len(xyz),dtype=np.uint32),'SCALAR',5125,34963)
    glb_mesh['primitives']=[{'attributes':attributes,'indices':indices,'material':material,'mode':4}]
    document['buffers'][0]['byteLength']=len(binary)
    encoded=json.dumps(document,separators=(',',':')).encode('utf-8')
    encoded+=b' '*((-len(encoded))%4)
    binary+=b'\0'*((-len(binary))%4)
    total=12+8+len(encoded)+8+len(binary)
    path.write_bytes(struct.pack('<4sII',b'glTF',2,total)+struct.pack('<II',len(encoded),0x4E4F534A)+encoded+
                     struct.pack('<II',len(binary),0x004E4942)+binary)
    return {'source_triangles':len(mesh.polygons),'glb_triangles':len(xyz)//3,
            'degenerate_triangles_retained':int(degenerate.sum()),'exact_position_uv_arrays':True}


def make_stone_normal(out):
    """Packed, UV-addressed normal map exports with GLB instead of a Blender-only bump."""
    size=512
    y,x=np.mgrid[0:size,0:size]/size*math.tau
    height=np.zeros((size,size),dtype=np.float32)
    rng=np.random.default_rng(173)
    for frequency,amplitude in [(3,.50),(7,.22),(13,.10),(27,.045)]:
        for _ in range(3):
            a,b=rng.integers(1,frequency+1,size=2)
            height+=amplitude*np.sin(x*a+y*b+rng.uniform(0,math.tau))
    dy,dx=np.gradient(height)
    normal=np.stack([-dx*5,-dy*5,np.ones_like(dx)],axis=-1)
    normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    rgba=np.ones((size,size,4),dtype=np.float32)
    rgba[:,:,:3]=normal*.5+.5
    image=bpy.data.images.new('Z10_Stone_Plane_Normal',width=size,height=size,alpha=True)
    image.colorspace_settings.name='Non-Color'
    image.pixels.foreach_set(rgba.ravel())
    image.filepath_raw=str(out/'stone-plane-normal.png')
    image.file_format='PNG'
    image.save()
    image.pack()
    return image


def make_material(name, color, roughness=0.86, stone_normal=None):
    mat = bpy.data.materials.new('Z10_' + name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = roughness
    if stone_normal is not None:
        # Restrained stone grain supports the large planes; it does not supply shape.
        nodes=mat.node_tree.nodes
        tex=nodes.new('ShaderNodeTexImage')
        tex.image=stone_normal
        normal=nodes.new('ShaderNodeNormalMap')
        normal.inputs['Strength'].default_value=.48
        mat.node_tree.links.new(tex.outputs['Color'],normal.inputs['Color'])
        mat.node_tree.links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
    return mat


def mesh_object(name, verts, faces, materials, collection, uv=None, material_indices=None):
    mesh = bpy.data.meshes.new(name + '_mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    for mat in materials:
        mesh.materials.append(mat)
    if uv is not None:
        layer = mesh.uv_layers.new(name='UVMap')
        for loop in mesh.loops:
            layer.data[loop.index].uv = uv[loop.vertex_index]
    if material_indices is not None:
        mesh.polygons.foreach_set('material_index', material_indices)
    return obj


def retain_forest(old, collection):
    """Exact original trunk/canopy triangles; no old alpine or square crag faces."""
    mesh = old.data
    layer = mesh.uv_layers.active
    verts, uvs, faces = [], [], []
    for polygon in mesh.polygons:
        loop_ids = list(polygon.loop_indices)
        local_uv = [layer.data[i].uv[:] for i in loop_ids]
        atlas_strip = int(np.mean([p[0] for p in local_uv]) * 8)
        if atlas_strip not in (1, 3):
            continue
        first = len(verts)
        for loop_id, uv in zip(loop_ids, local_uv):
            verts.append(mesh.vertices[mesh.loops[loop_id].vertex_index].co[:])
            uvs.append(uv)
        faces.append(tuple(range(first, len(verts))))
    return mesh_object('Z10_Retained_Original_Forest', verts, faces,
                       list(mesh.materials), collection, uvs)


# Each station is an intentional summit, col, shoulder or endcap, not a radial cone.




def geological_field(collection, near, far, north_depth=0.0):
    """A continuous terrain surface shaped by asymmetric divides and branching ribs.

    Crest stations describe a skeleton; broad piecewise slopes create geological
    planes. Diagonal spur paths project into the foreground at different depths.
    There are no independently placed radial/conical mountain primitives.
    """
    main = np.array([(-26.4,12.9,5.0),(-24.9,14.5,7.2),(-23.0,16.5,10.7),
        (-21.8,17.2,12.1),(-20.5,17.5,10.8),(-19.5,18.1,14.8),
        (-18.7,18.4,16.7),(-18.0,18.2,14.9),(-16.9,18.6,13.5),
        (-15.7,18.4,11.2),(-14.0,18.0,8.9),(-12.8,18.2,9.4),
        (-11.7,18.0,13.8),(-10.6,17.8,13.1),(-9.7,17.6,12.9),
        (-8.4,17.9,11.4),(-6.5,18.2,8.5),(-5.2,18.5,9.0),
        (-3.9,18.3,10.3),(-2.7,18.5,12.3),(-1.8,18.3,11.8),
        (-.3,18.0,9.7),(1.7,17.8,8.4),(3.4,17.3,9.4),
        (5.2,16.8,7.8),(7.0,15.6,6.3)])
    distant = np.array([(-25.8,15.5,4.7),(-23.0,18.0,8.6),(-21.0,20.1,11.3),
        (-19.0,21.1,12.5),(-17.4,21.9,13.9),(-16.5,22.0,13.0),
        (-15.4,22.0,15.1),(-14.5,21.9,13.8),(-13.1,21.8,12.8),
        (-11.8,21.8,13.5),(-10.8,21.6,12.1),(-9.5,21.8,14.2),
        (-8.5,21.7,13.2),(-7.3,21.5,11.8),(-5.6,21.2,12.7),
        (-4.3,20.9,11.4),(-2.8,20.8,12.5),(-1.5,20.5,10.9),
        (.3,20.0,11.5),(2.0,19.4,9.8),(4.0,18.7,8.1),(6.8,16.8,6.0)])
    main[:,2]=4.6+(main[:,2]-4.6)*.89
    distant[:,2]=4.6+(distant[:,2]-4.6)*.72
    if north_depth:
        # Optional unwalkable background depth study; gameplay surface stays exact.
        for stations,factor in [(main,1.0),(distant,.96)]:
            taper=np.clip((stations[:,0]+26)/5,0,1)*np.clip((7-stations[:,0])/5,0,1)
            stations[:,1]+=north_depth*factor*taper
            stations[:,2]-=north_depth*(.30 if factor==1.0 else .12)*factor*taper
    xs=np.linspace(-26.4,7.2,140)
    rear_limit=23.3+north_depth*1.24
    ys=np.linspace(11.65,rear_limit,55+int(north_depth*3))
    gx,gy=np.meshgrid(xs,ys)
    # Follow the existing rounded northern silhouette instead of a rectangular sheet.
    back_edge=rear_limit-.011*(gx+8.0)**2
    gy=11.65+(gy-11.65)/(rear_limit-11.65)*(back_edge-11.65)
    # Small domain bends keep the terrain stations from becoming vertical pleats.
    wx=gx+.30*np.sin(gy*.73+gx*.18)
    wy=gy+.23*np.sin(gx*.62+gy*.2)
    floor=sample_ground_z(gx,np.minimum(gy,14.1))-.18
    def divide(stations,front_width,rear_width):
        cy=np.interp(wx,stations[:,0],stations[:,1])
        summit=np.interp(wx,stations[:,0],stations[:,2])
        width=np.where(wy<cy,front_width,rear_width)
        shape=np.maximum(0,1-np.abs(wy-cy)/width)**1.22
        return np.maximum(0,summit-floor)*shape
    forward_width=6.0+north_depth+.6*np.sin(wx*.41)+.5*np.sin(wx*.78)
    hnear=divide(main,forward_width,3.3)
    hfar=divide(distant,2.9+north_depth*.35,1.45+north_depth*.15)
    # Ribs branch diagonally from individual shoulders, each with its own toe.
    spurs=[((-19.1,18.2,13.1),(-22.6,12.0,5.3),2.6),
           ((-17.3,18.4,11.7),(-16.0,12.6,5.6),2.4),
           ((-10.7,17.8,11.8),(-13.4,12.4,5.1),2.7),
           ((-9.5,17.6,10.7),(-7.2,12.1,5.0),2.2),
           ((-2.6,18.4,10.6),(-4.9,12.5,5.4),2.9),
           ((-1.4,18.3,9.9),(1.5,13.0,4.9),2.5)]
    for start,end,width in spurs:
        if north_depth:
            start=(start[0],start[1]+north_depth,start[2]-north_depth*.30)
            width+=north_depth*.14
        a=np.array(start[:2]); b=np.array(end[:2]); segment=b-a
        t=np.clip(((wx-a[0])*segment[0]+(wy-a[1])*segment[1])/np.dot(segment,segment),0,1)
        distance=np.sqrt((wx-(a[0]+t*segment[0]))**2+(wy-(a[1]+t*segment[1]))**2)
        zline=start[2]*(1-t)+end[2]*t
        spur=np.maximum(0,zline-floor)*np.maximum(0,1-distance/(width*(.5+.5*t)))
        hnear=np.maximum(hnear,spur)
    for crest_x,diagonal,width in [(-20.0,-.35,.35),(-17.9,.32,.43),(-11.1,-.48,.40),(-8.8,.51,.36),(-2.4,-.42,.48)]:
        center=crest_x+diagonal*(18.1+north_depth-wy)+.15*np.sin(wy*1.2)
        couloir=np.exp(-((wx-center)/width)**2)
        hnear-=.8*couloir*np.clip(hnear/3,0,1)*np.clip((18.4+north_depth-wy)/2,0,1)
    # Depth study expands only the north background, never the playable contract.
    arc=island_signed_distance(gx,(gy-north_depth*np.clip((gy-14)/10,0,1))*.89)
    edge_taper=np.clip((1.025-arc)/.075,0,1)
    hnear*=edge_taper
    hfar*=edge_taper
    relief=np.maximum(hnear,hfar)
    # Broad stratified breaks, restrained to rock flanks rather than silhouette.
    ribs=.40*np.sin(wx*1.37+wy*.64)+.26*np.sin(wx*.67-wy*1.1)
    relief+=ribs*np.clip(relief/2,0,1)*np.clip((17.0-floor-relief)/3,0,1)
    gz=floor+np.maximum(0,relief)
    if north_depth:
        # Natural rear groundfall reaches the existing studio ground. It is a
        # continuous slope, not a floating tile or an extruded perimeter collar.
        rear_taper=np.clip((back_edge-gy)/3.0,0,1)
        rear_taper=rear_taper*rear_taper*(3-2*rear_taper)
        side_taper=np.clip((1.025-arc)/.12,0,1)
        side_taper=side_taper*side_taper*(3-2*side_taper)
        outside_taper=np.minimum(rear_taper,side_taper)
        outside_taper=np.where(gy<14.3,1,outside_taper)
        gz=-1.03+(gz+1.03)*outside_taper
    verts=np.stack([gx,gy,gz],axis=-1).reshape(-1,3).tolist()
    faces=[];indices=[]; nx=len(xs)
    far_mask=hfar>hnear+.05
    for j in range(len(ys)-1):
        for i in range(nx-1):
            if ((not north_depth and np.max(relief[j:j+2,i:i+2])<.07) or np.min(arc[j:j+2,i:i+2])>1.025):
                continue
            a=j*nx+i
            ts=[(a,a+1,a+nx+1),(a,a+nx+1,a+nx)] if (i+j)%2==0 else [(a,a+1,a+nx),(a+1,a+nx+1,a+nx)]
            for face in ts:
                faces.append(face)
                pts=np.array([verts[k] for k in face]); normal=np.cross(pts[1]-pts[0],pts[2]-pts[0]);normal/=np.linalg.norm(normal)+1e-8
                z=pts[:,2].mean()
                if relief[j,i]<.95 and gy[j,i]<16:mat=5
                elif z>11.8 and normal[2]>.38 and normal[0]<.40:mat=4
                elif normal[0]>.25:mat=1
                elif normal[0]<-.18:mat=2
                else:mat=3 if normal[1]>.3 else 0
                indices.append(mat+(6 if far_mask[j,i] else 0))
    # Close the natural outline down into the diorama rock mass. Front skirts are
    # buried under unchanged meadow; rear skirts prevent a floating terrain sheet.
    edges={}
    for face in faces:
        for a,b in zip(face,face[1:]+face[:1]):
            key=tuple(sorted((a,b)))
            count,_=edges.get(key,(0,(a,b)))
            edges[key]=(count+1,(a,b))
    bottom={}
    shoulder={}
    for count,(a,b) in ([] if north_depth else list(edges.values())):
        if count!=1:
            continue
        for vertex in (a,b):
            if vertex not in bottom:
                x,y,z=verts[vertex]
                shoulder[vertex]=len(verts)
                verts.append((x+.13*math.sin(x*.74+y*.2),y*.994+.12*math.sin(x*.51),
                              z*.42-.25+.20*math.sin(x*.64+y*.31)))
                bottom[vertex]=len(verts)
                verts.append((x*.996,y*.996,-1.05+.08*math.sin(x*.51+y*.37)))
        faces.extend([(a,shoulder[b],b),(a,shoulder[a],shoulder[b]),
                      (shoulder[a],bottom[b],shoulder[b]),(shoulder[a],bottom[a],bottom[b])])
        indices.extend([3,3,3,3])
    obj=mesh_object('Z10_Connected_Geological_Divides',verts,faces,near+far,collection,material_indices=indices)
    uv=obj.data.uv_layers.new(name='Geological_Plane_UV')
    for face in obj.data.polygons:
        axis=int(np.argmax(np.abs(face.normal)))
        for loop_id in face.loop_indices:
            p=obj.data.vertices[obj.data.loops[loop_id].vertex_index].co
            uv.data[loop_id].uv=(p.y*.3,p.z*.3) if axis==0 else ((p.x*.3,p.z*.3) if axis==1 else (p.x*.3,p.y*.3))
    return obj


def make_apron(collection, materials):
    xs = np.linspace(-26.0, 7.0, 56)
    rows = 12
    verts, faces, mi = [], [], []
    for row in range(rows):
        t = row / (rows - 1)
        for x in xs:
            front = 11.45 + .48 * math.sin(x * .42) + .2 * math.sin(x * .88)
            depth = 3.1 + .65 * math.sin(x * .31 + 1)
            taper = max(0, min(1, (x + 26) / 2.2, (7 - x) / 2))
            y = front + depth * t * taper
            base = float(sample_ground_z(x, y))
            hill = (1.2 + .52 * math.sin(x * .43) + .3 * math.sin(x * .91 + .6))
            z = base - .10 + hill * math.sin(t * math.pi) ** 1.1 * taper
            verts.append((float(x), y, z))
    for row in range(rows - 1):
        for i in range(len(xs) - 1):
            a = row * len(xs) + i
            faces.extend([(a, a + 1, a + len(xs) + 1), (a, a + len(xs) + 1, a + len(xs))])
            mi.extend([i % 3 if row > 1 else 0] * 2)
    obj = mesh_object('Z10_Rounded_Wooded_Apron', verts, faces, materials, collection, material_indices=mi)
    for face in obj.data.polygons:
        face.use_smooth = True
    return obj


def load_pure_forest_helpers():
    wanted = {'MeshBuilder', 'make_cylinder_or_cone', 'add_chiseled_broadleaf_tree', 'add_tiered_spruce_pine'}
    source = ROOT / 'Tools/build_true360_glb_masterpiece.py'
    syntax = ast.parse(source.read_text(encoding='utf-8'))
    nodes = [n for n in syntax.body if isinstance(n, (ast.ClassDef, ast.FunctionDef)) and n.name in wanted]
    if {n.name for n in nodes} != wanted:
        raise RuntimeError('Required pure forest helpers unavailable; refuse old-builder import')
    namespace = {'np': np, 'math': math, 'strip_uv': strip_uv,
                 'SUN_DIR': np.array([.68, -.38, .62]) / np.linalg.norm([.68, -.38, .62])}
    exec(compile(ast.Module(body=nodes, type_ignores=[]), str(source), 'exec'), namespace)
    return namespace


def dress_apron(collection, apron, original_material):
    helpers = load_pure_forest_helpers()
    mb = helpers['MeshBuilder']()
    bvh = BVHTree.FromPolygons([v.co for v in apron.data.vertices],
                              [p.vertices[:] for p in apron.data.polygons])
    rng = np.random.default_rng(642)
    accepted = []
    clusters = [(-23.3, 12.8), (-19.5, 13.7), (-16.9, 13.3), (-12.8, 13.0),
                (-9.8, 13.9), (-6.5, 13.2), (-3.4, 13.9), (0.1, 13.2), (3.5, 13.3)]
    for attempt in range(680):
        cx, cy = clusters[attempt % len(clusters)]
        x, y = cx + rng.normal(0, 1.35), cy + rng.normal(0, .65)
        if any((x - px) ** 2 + (y - py) ** 2 < .60 ** 2 for px, py in accepted):
            continue
        hit, normal, _, _ = bvh.ray_cast(Vector((x, y, 30)), Vector((0, 0, -1)))
        if hit is None or normal.z < .55:
            continue
        accepted.append((x, y))
        scale = rng.uniform(.49, .82)
        z = hit.z - .13
        if rng.random() < .59:
            helpers['add_tiered_spruce_pine'](mb, [], x, y, z, scale=scale, tiers=6, seed=attempt*.73)
        else:
            helpers['add_chiseled_broadleaf_tree'](mb, [], x, y, z, scale=scale,
                                                  palette_idx=1 if rng.random() < .65 else 2, seed=attempt*.91)
        if len(accepted) >= 100:
            break
    verts, uvs, faces = mb.build_arrays()
    mesh_object('Z10_Natural_Mixed_Foothill_Woodland', verts, faces, [original_material], collection, uvs)
    return len(accepted)


def aim_camera(name, position, target, lens=50):
    camera = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    bpy.context.scene.collection.objects.link(camera)
    camera.location = position
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.lens = lens
    return camera


def render(out, name, camera, width=1440, height=960):
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(out / name)
    log('Render ' + name)
    bpy.ops.render.render(write_still=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--out', type=Path, default=DEFAULT_OUT)
    parser.add_argument('--skip-before', action='store_true')
    parser.add_argument('--build-only', action='store_true')
    parser.add_argument('--quick', action='store_true', help='Only fixed camera and foothill evidence')
    parser.add_argument('--north-depth', type=float, default=0.0, help='Unwalkable north-only depth study, no playable terrain edits')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:])
    out = args.out.resolve()
    if not out.is_relative_to(ROOT / '.collab/artifacts'):
        raise ValueError('Outputs must stay under .collab/artifacts')
    out.mkdir(parents=True, exist_ok=True)
    if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SOURCE_HASH:
        raise RuntimeError('Protected baseline changed; refuse build')
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE), load_ui=False)
    scene = bpy.context.scene
    camera = bpy.data.objects['Master_Isometric_Camera']
    original_camera = {'matrix': [list(row) for row in camera.matrix_world], 'lens': camera.data.lens}
    before = fingerprints()
    if not args.skip_before and not args.build_only:
        render(out, '01-before-fixed-camera.png', camera)
    collection = bpy.data.collections.new('Landscape_Zone10_Revision')
    scene.collection.children.link(collection)
    old = bpy.data.objects[ZONE10]
    original_material = old.data.materials[0]
    retained = retain_forest(old, collection)
    bpy.data.objects.remove(old, do_unlink=True)
    retained.name = ZONE10
    near_colors = [( .105, .14, .18), (.14, .16, .18), (.078, .115, .16),
                   (.115, .145, .18), (.55, .64, .68), (.18, .195, .085)]
    far_colors = [( .075, .16, .28), (.14, .235, .34), (.05, .12, .23),
                  (.11, .20, .31), (.42, .57, .73), (.13, .22, .18)]
    normal=make_stone_normal(out)
    near = [make_material('Near_' + str(i), color,stone_normal=normal) for i, color in enumerate(near_colors)]
    far = [make_material('Far_' + str(i), color,stone_normal=normal) for i, color in enumerate(far_colors)]
    main_range = geological_field(collection, near, far, args.north_depth)
    apron = make_apron(collection, [make_material('Apron_MossGold', (.26, .245, .075)),
                                   make_material('Apron_Ochre', (.34, .29, .11)),
                                   make_material('Apron_Lichen', (.235, .265, .10))])
    trees = dress_apron(collection, apron, original_material)
    after = fingerprints()
    protected = {name: {'before': data, 'after': after.get(name), 'unchanged': data == after.get(name)}
                 for name, data in before.items() if name != ZONE10}
    assert len(protected) == 10 and all(v['unchanged'] for v in protected.values()), 'Protected zone mutation'
    assert original_camera == {'matrix': [list(row) for row in camera.matrix_world], 'lens': camera.data.lens}
    report = {'source': str(SOURCE), 'source_sha256': SOURCE_HASH, 'runtime': bpy.app.version_string,
              'other_ten_zones_exact': True, 'zones': protected, 'camera_unchanged': True,
              'retained_forest_faces': len(retained.data.polygons), 'new_woodland_trees': trees,
              'main_geological_faces': len(main_range.data.polygons), 'promotion': 'NONE; user visual approval required',
              'unwalkable_north_depth_study':args.north_depth,
              'coordinate_contract': 'map_surface_contract 62x47; no document_farm_surface',
              'runtime_warning': 'Source5.3 opened/saved by bpy5.2.2; inspect compatibility before production integration'}
    (out / 'preservation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    scene.camera = camera
    bpy.ops.wm.save_as_mainfile(filepath=str(out / 'landscape-candidate.blend'))
    log('Saved candidate; protected10 exact. Export GLB')
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type == 'MESH' and (obj.name.startswith('Zone') or obj.name.startswith('Z10_')):
            obj.select_set(True)
    # Blender's exporter validates malformed inherited meshes in place. Export
    # throwaway copies so Zone11 cannot be silently changed by that validation.
    originals=[]
    for obj in list(bpy.context.selected_objects):
        original=obj.data
        duplicate=original.copy()
        originals.append((obj,original,duplicate))
        obj.data=duplicate
    try:
        bpy.ops.export_scene.gltf(filepath=str(out / 'landscape-candidate.glb'), export_format='GLB',
                                  use_selection=True, export_cameras=False, export_lights=False)
    finally:
        for obj,original,duplicate in originals:
            obj.data=original
            bpy.data.meshes.remove(duplicate)
    post_export=fingerprints()
    assert all(post_export.get(name)==data for name,data in before.items() if name!=ZONE10), 'Exporter mutated protected data'
    report['glb_zone11_preservation']=preserve_zone11_in_glb(out/'landscape-candidate.glb')
    (out/'preservation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    if args.build_only:
        return
    render(out, '02-after-fixed-camera.png', camera)
    close = aim_camera('Z10_Review_Foothill', (-17, -4, 13), (-13.6, 14.5, 8), lens=56)
    render(out, '03-foothill-close.png', close)
    if not args.quick:
        ridge_camera = aim_camera('Z10_Review_Ridge', (-20, 0, 17), (-14, 18.4, 12), lens=53)
        render(out, '04-ridge-close.png', ridge_camera)
        rear = aim_camera('Z10_Review_Rear', (-16, 43, 23), (-11, 18, 9), lens=44)
        render(out, '05-rear-geology.png', rear)
        # Clay only changes new Zone10 material slots, then restores them before saving.
        clay = make_material('REVIEW_Clay', (.52, .52, .52))
        stash = []
        for obj in collection.objects:
            stash.append((obj, list(obj.data.materials)))
            for slot in range(len(obj.data.materials)):
                obj.data.materials[slot] = clay
        render(out, '06-clay.png', ridge_camera)
        for obj, materials in stash:
            for slot, material in enumerate(materials):
                obj.data.materials[slot] = material
        top = aim_camera('Z10_Review_Top', (-8, 0, 78), (-8, 0, 0), lens=48)
        render(out, '07-top-layout.png', top)
    scene.camera = camera
    bpy.ops.wm.save_as_mainfile(filepath=str(out / 'landscape-candidate.blend'))
    (out / 'notes.md').write_text(
        '# Zone10 candidate only\n\nConnected asymmetric divide, lower mixed woodland apron; '
        'original trunk/canopy triangles retained. Other ten zones incl approved observatory '
        'are exact in geometry, topology, UV, transforms and material state.\n\n'
        'Compare identical fixed-camera images and close/rear/clay/top renders. '
        'This is a review candidate, not accepted art. No Assets/ArtSource/Unity/F writes. '
        'Runtime5.2.2 against5.3 source requires later compatibility validation.\n', encoding='utf-8')
    log('Evidence complete; no promotion')


if __name__ == '__main__':
    main()
