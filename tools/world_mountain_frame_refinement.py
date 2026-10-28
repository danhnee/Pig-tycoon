"""Distant geological framing for the new whole-world art master.

Import build_world_mountain_frame(collection) -> (objects, report). No source scene
is loaded and no file is written by the hook. Standalone mode previews a copy of
the current world master in its own artifact folder; never modifies live assets.
"""
from pathlib import Path
import argparse
import json
import math
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.collab/artifacts/mountain-reference-study-codex/runtime'))
import bpy
import numpy as np
from mathutils import Vector

OUT = ROOT / '.collab/artifacts/world-mountain-frame-refinement-sol-high'
STABLE = ROOT / '.collab/artifacts/baseline-mountains-sol-high/zone10-distance-frame/zone10-distance-frame.blend'
CONTEXT = ROOT / '.collab/artifacts/reference-world-master-sol-high/reference-world-art-candidate.blend'

# Divides are intentionally unequal: long traverses, off-axis summits, deep cols,
# and branching lower spurs. Elevations are art coordinates, not balance values.
RANGES = (
    dict(name='Near_Sculpted_Massifs', bounds=(-38, 18, 23, 53), base=-.8,
         color=(.19, .225, .27), haze=.012,
         divide=[(-37,32,0),(-31,34,4),(-27,37,8),(-25,39,12.4),
                 (-22,39.5,9.1),(-18.5,42,5),(-16,40,6.3),(-12.5,38.5,10.2),
                 (-10,37,8.3),(-6.5,39,4.2),(-3,42,5.1),(1,43,8.2),
                 (3.5,44,6.6),(7,43,3.6),(12,41,2),(17,40,0)],
         spurs=[([(-25,39,12.4),(-27.5,34,8.3),(-30,29,3.6),(-33,26,0)],1.28),
                ([(-25,39,12.4),(-21.2,34,7),(-17.5,28,2),(-15,25,0)],1.35),
                ([(-12.5,38.5,10.2),(-14,33,6.8),(-18,29,2.5),(-22,25,0)],1.20),
                ([(-12.5,38.5,10.2),(-8,34,5.2),(-4,30,1.7),(0,27,0)],1.40),
                ([(1,43,8.2),(3,37,4.7),(6,33,1.2),(10,30,0)],1.05)], slope=1.02),
    dict(name='Middle_Broken_Divide', bounds=(-46, 28, 34, 67), base=-1.0,
         color=(.205, .29, .39), haze=.045,
         divide=[(-45,47,0),(-40,48,3),(-35,50,6.4),(-32,51,10.0),
                 (-28,53,7.1),(-25,55,4),(-22,56,6.6),(-18,55,8.6),
                 (-14,54,5.5),(-10,56,7.1),(-7,58,10.8),(-4,57,9.4),
                 (0,55,5),(4,54,3.5),(8,54,6.7),(11,55,8.3),
                 (15,56,5),(20,55,3.2),(27,52,0)],
         spurs=[([(-32,51,10),(-35,44,5),(-39,39,0)],1.1),
                ([(-18,55,8.6),(-20,46,4.5),(-24,40,0)],1.15),
                ([(-7,58,10.8),(-3,49,6),(2,42,0)],1.0),
                ([(11,55,8.3),(15,49,4),(18,42,0)],1.2)], slope=1.13),
    dict(name='Far_Atmospheric_Skyline', bounds=(-54, 38, 47, 83), base=-1.2,
         color=(.28, .39, .53), haze=.085,
         divide=[(-53,65,0),(-45,66,3.1),(-41,68,6.8),(-38,69,8.8),
                 (-35,70,7),(-29,73,3.9),(-23,72,5.2),(-19,70,7.9),
                 (-15,71,6.2),(-11,72,4.1),(-6,74,5.7),(-2,75,7.5),
                 (3,74,4.8),(7,72,5.4),(11,71,9.1),(14,70,7.5),
                 (19,69,4.2),(24,68,5.8),(29,69,3.6),(37,66,0)],
         spurs=[([(-38,69,8.8),(-42,59,3),(-47,53,0)],.9),
                ([(-19,70,7.9),(-22,61,3),(-26,53,0)],.95),
                ([(11,71,9.1),(7,62,4),(1,55,0)],.85)], slope=.91),
)


def _material(name, color, haze=0):
    mat=bpy.data.materials.new('WorldMountain_'+name)
    mat.diffuse_color=(*color,1);mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=(*color,1)
    shader.inputs['Roughness'].default_value=.92
    shader.inputs['Emission Color'].default_value=(*color,1)
    shader.inputs['Emission Strength'].default_value=haze
    return mat


def _geological_material(name, color, haze):
    """Continuous warm/cool slate response, not a painted triangle palette."""
    mat=_material(name,color,haze)
    nodes=mat.node_tree.nodes;links=mat.node_tree.links
    shader=nodes.get('Principled BSDF')
    geometry=nodes.new('ShaderNodeNewGeometry')
    facing=nodes.new('ShaderNodeVectorMath');facing.operation='DOT_PRODUCT'
    facing.inputs[1].default_value=(-.68,-.4,.61)
    links.new(geometry.outputs['Normal'],facing.inputs[0])
    ramp=nodes.new('ShaderNodeValToRGB')
    c=np.array(color)
    ramp.color_ramp.elements[0].position=.10
    ramp.color_ramp.elements[0].color=(*tuple(c*np.array([.60,.79,1.05])),1)
    ramp.color_ramp.elements[1].position=.85
    ramp.color_ramp.elements[1].color=(*tuple(c*np.array([1.48,1.17,.89])),1)
    links.new(facing.outputs['Value'],ramp.inputs[0])
    # Tiny mineral roughness supports sculpted forms; no displacement/noise shape.
    texture=nodes.new('ShaderNodeTexNoise');texture.inputs['Scale'].default_value=7.5
    texture.inputs['Detail'].default_value=3.2;texture.inputs['Roughness'].default_value=.68
    links.new(geometry.outputs['Position'],texture.inputs['Vector'])
    grain=nodes.new('ShaderNodeValToRGB')
    grain.color_ramp.elements[0].position=.20;grain.color_ramp.elements[0].color=(.48,.51,.55,1)
    grain.color_ramp.elements[1].position=.78;grain.color_ramp.elements[1].color=(1.06,1.03,.98,1)
    links.new(texture.outputs['Fac'],grain.inputs[0])
    mineral=nodes.new('ShaderNodeMixRGB');mineral.blend_type='MULTIPLY';mineral.inputs[0].default_value=.72
    links.new(ramp.outputs['Color'],mineral.inputs[1]);links.new(grain.outputs['Color'],mineral.inputs[2])
    links.new(mineral.outputs['Color'],shader.inputs['Base Color'])
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16
    bump.inputs['Distance'].default_value=.075
    links.new(texture.outputs['Fac'],bump.inputs['Height'])
    links.new(bump.outputs['Normal'],shader.inputs['Normal'])
    return mat


def _skeleton_height(x, y, nodes, slope):
    """Planar ridge flanks around a connected divide and its sloping branches."""
    result=np.zeros_like(x)
    for a,b in zip(nodes,nodes[1:]):
        dx,dy=b[0]-a[0],b[1]-a[1]
        t=np.clip(((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy),0,1)
        distance=np.hypot(x-a[0]-t*dx,y-a[1]-t*dy)
        altitude=a[2]+t*(b[2]-a[2])
        # Cross slopes broaden toward the toe; the upper rock stays angular.
        shoulder=np.maximum(0,altitude-slope*distance-.018*distance*distance)
        result=np.maximum(result,shoulder)
    return result


def _build_range(collection, spec, number):
    xmin,xmax,ymin,ymax=spec['bounds']
    step=.40 if number==0 else .75
    xs=np.linspace(xmin,xmax,math.ceil((xmax-xmin)/step)+1)
    ys=np.linspace(ymin,ymax,math.ceil((ymax-ymin)/step)+1)
    x,y=np.meshgrid(xs,ys)
    rng=np.random.default_rng(750+number)
    # Irregular triangulation avoids repeated rows without adding height noise.
    interior=np.ones_like(x);interior[[0,-1],:]=0;interior[:,[0,-1]]=0
    x+=rng.uniform(-.23,.23,x.shape)*interior
    y+=rng.uniform(-.23,.23,y.shape)*interior
    h=_skeleton_height(x,y,spec['divide'],spec['slope'])
    for branch,slope in spec['spurs']:
        h=np.maximum(h,_skeleton_height(x,y,branch,slope))
    if number==0:
        for branch in [ [(-25,39,12.4),(-24,35,8.1),(-23.5,31,4.4),(-25,27,0)],
                        [(-12.5,38.5,10.2),(-11.4,35,7.2),(-13,30,2.3),(-15,27,0)],
                        [(1,43,8.2),(-.5,39,6),(-1.8,35,2.4),(-4,31,0)] ]:
            h=np.maximum(h,_skeleton_height(x,y,branch,1.55))
    # Long, oblique couloirs lie BETWEEN ribs, never vertical decorative stripes.
    if number==0:
        for top,toe,width,depth in [((-23,38),(-18,27),.85,1.05),
                                   ((-13,37),(-9,27),.60,.72),
                                   ((.1,42),(4,32),.65,.68)]:
            dx,dy=toe[0]-top[0],toe[1]-top[1]
            t=np.clip(((x-top[0])*dx+(y-top[1])*dy)/(dx*dx+dy*dy),0,1)
            d=np.hypot(x-top[0]-t*dx,y-top[1]-t*dy)
            h-=depth*np.clip(1-d/width,0,1)*np.clip(h/4,0,1)*(1-t*.65)
    h=np.maximum(h,0)
    # Drainage tributaries follow long diagonal runs at different widths/depths.
    # They split broad buttresses; their elevation fades naturally into talus.
    if number<2:
        for sx,sy,tx,ty,width,depth in [(-24,37,-19.5,28,.34,.46),
                 (-26.2,36.5,-30.5,28.5,.28,.38),(-21.3,37,-18,31,.22,.30),
                 (-12.4,36.7,-10,29,.27,.36),(-10.1,36,-5.8,29,.34,.34),
                 (-1,40,3,32,.30,.29)]:
            dx,dy=tx-sx,ty-sy
            t=np.clip(((x-sx)*dx+(y-sy)*dy)/(dx*dx+dy*dy),0,1)
            d=np.hypot(x-sx-t*dx,y-sy-t*dy)
            h-=depth*np.exp(-(d/width)**2)*np.clip(h/4,0,1)*(1-.55*t)
    z=spec['base']+h
    # Composition is judged in the world camera: far ridges form a low backdrop,
    # rather than occupying most of the frame merely because they are farther north.
    depth_origin=(23,28,36)[number]
    depth_scale=(.54,.54,.50)[number]
    relief_scale=(.92,.74,.48)[number]
    vertices=np.stack((-10+(x+10)*.65,
                       depth_origin+(y-ymin)*depth_scale,
                       -1.03+h*relief_scale),axis=-1).reshape(-1,3)
    # Tuck the outer western toe behind the island rim instead of projecting
    # a horizontal floating skirt into the empty studio background.
    outer=vertices[:,0]<-27
    vertices[outer,0]=-27+(vertices[outer,0]+27)*.24
    faces=[];nx=len(xs)
    for j in range(len(ys)-1):
        for i in range(nx-1):
            if h[j:j+2,i:i+2].max()<.04:continue
            a=j*nx+i
            if (i+j)%2:
                faces.extend(((a,a+1,a+nx),(a+1,a+nx+1,a+nx)))
            else:faces.extend(((a,a+1,a+nx+1),(a,a+nx+1,a+nx)))
    mesh=bpy.data.meshes.new(spec['name'])
    mesh.from_pydata(vertices.tolist(),[],faces);mesh.update()
    ob=bpy.data.objects.new('WorldMountain_'+spec['name'],mesh);collection.objects.link(ob)
    mesh.materials.append(_geological_material(spec['name']+'_Slate',spec['color'],spec['haze']))
    mesh.materials.append(_material(spec['name']+'_CrevasseSnow',(.27,.34,.42),spec['haze']))
    for face in mesh.polygons:
        n=face.normal;center=face.center
        source_x=-10+(center.x+10)/.65
        source_y=ymin+(center.y-depth_origin)/depth_scale
        source_z=(center.z+1.03)/relief_scale+spec['base']
        material=0
        # Snow remains in narrow elevated crevasses, not a white mountain cap.
        if number==0 and source_z>7.1 and n.z>.20 and n.z<.86:
            d=abs(source_x-(-24.1+.29*(38-source_y)))
            d2=abs(source_x-(-12.6+.24*(37-source_y)))
            # Keep the recessed pale vein subdued; stark full-triangle snow
            # patches were rejected in the independent geology review.
            if min(d,d2)<.10 and source_y>33:material=1
        face.material_index=material
        face.use_smooth=True
    uv=mesh.uv_layers.new(name='Rock_Planes')
    for loop in mesh.loops:
        p=mesh.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(p.x*.13,p.y*.13)
    ob['role']='Unwalkable far vista, no gameplay surface edits'
    ob['depth_layer']=number
    return ob


def _low_connector(collection):
    """Reuse only the validated low transition; erase the old wall silhouette."""
    with bpy.data.libraries.load(str(STABLE),link=False) as (source,target):
        target.objects=['Zone10_Distant_Frame_Ridge']
    obj=target.objects[0];collection.objects.link(obj)
    obj.name='WorldMountain_Low_Northern_Transition'
    obj.data=obj.data.copy()
    for v in obj.data.vertices:
        v.co.z=-1.03+(v.co.z+1.03)*.40
    obj.data.materials.clear()
    obj.data.materials.append(_material('LowTransition_Woodland_Ochre',(.24,.27,.14)))
    for p in obj.data.polygons:p.material_index=0
    obj.data.update()
    obj['role']='Low reused northern transition, old corrugated skyline removed'
    obj.hide_render=True
    return obj


def _connected_foothill_mass(collection, ranges):
    """Continuous low bedrock under all three ranges, beyond the game plane.

    The outline follows uneven foothill toes and rear saddles rather than a
    rectangular display slab. Its front is buried under the forest/citadel.
    """
    vertices=[];faces=[];material_indices=[]
    for layer,range_ob in enumerate(ranges):
        edges={}
        for polygon in range_ob.data.polygons:
            ids=list(polygon.vertices)
            for a,b in zip(ids,ids[1:]+ids[:1]):
                key=tuple(sorted((a,b)))
                edges[key]=edges.get(key,0)+1
        strips={}
        for (a,b),uses in edges.items():
            if uses!=1:continue
            for index in (a,b):
                if index in strips:continue
                p=range_ob.data.vertices[index].co
                strip=[]
                for step in range(5):
                    t=step/4
                    target_y=max(17.55+.16*math.sin(p.x*.43),p.y-12.0)
                    toe_z=-1.22+.30*math.sin(p.x*.27+target_y*.16)
                    x=p.x+.15*math.sin(p.y*.41+p.x*.19)*math.sin(math.pi*t)
                    y=p.y+(target_y-p.y)*t
                    z=p.z*(1-t)+toe_z*t+.30*math.sin(math.pi*t)*math.sin(p.x*.28+p.y*.13)
                    strip.append(len(vertices));vertices.append((x,y,z))
                strips[index]=strip
            for step in range(4):
                aa,ab=strips[a][step:step+2];ba,bb=strips[b][step:step+2]
                for face in ((aa,ba,bb),(aa,bb,ab)):
                    p,q,r=(np.array(vertices[index]) for index in face)
                    if np.cross(q-p,r-p)[2]<0:face=tuple(reversed(face))
                    faces.append(face);material_indices.append(layer)
    mesh=bpy.data.meshes.new('Connected_Northern_Bedrock')
    mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new('WorldMountain_Connected_Foothill_Bedrock',mesh)
    collection.objects.link(obj)
    for layer,color in enumerate(((.22,.30,.40),(.18,.24,.31),(.16,.205,.255))):
        mesh.materials.append(_geological_material('ConnectedFoothill_Slate'+str(layer),color,.025))
    for p,index in zip(mesh.polygons,material_indices):p.use_smooth=True;p.material_index=index
    obj['role']='Continuous low unwalkable bedrock joins near/middle/far feet; front hidden behind forests and citadel'
    obj['minimum_y']=min(v[1] for v in vertices)
    return obj


def build_world_mountain_frame(collection):
    """Build only distant mountain art; return (list[Object], JSON-safe report)."""
    objects=[_low_connector(collection)]
    # Back-to-front creation is convenient for inspection; all are real 3D meshes.
    for number in (2,1,0):objects.append(_build_range(collection,RANGES[number],number))
    objects.append(_connected_foothill_mass(collection,objects[1:]))
    report={'status':'art_candidate_requires_visual_review','hook':'build_world_mountain_frame',
            'gameplay_plane_changed':False,'old_world_imported':False,
            'design':'Three depth-separated divides and a continuous irregular low bedrock mass joining every mountain foot',
            'objects':[{'name':ob.name,'bounds':[[min(v.co[k] for v in ob.data.vertices) for k in range(3)],
                                              [max(v.co[k] for v in ob.data.vertices) for k in range(3)]]}
                       for ob in objects]}
    return objects,report


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--gpu',action='store_true',help='Use CUDA explicitly; OptiX is unsupported in this runtime')
    parser.add_argument('--build-only',action='store_true')
    parser.add_argument('--contact-proof',action='store_true',help='Short native-camera border render of castle/mountain contact only')
    parser.add_argument('--close-only',action='store_true');args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(CONTEXT),load_ui=False,use_scripts=False)
    scene=bpy.context.scene
    for ob in scene.objects:
        if ob.name.startswith(('Zone10_Distant_Frame_Ridge','WorldMountain_')):ob.hide_render=True
    collection=bpy.data.collections.new('WorldMountain_Frame_Refinement');scene.collection.children.link(collection)
    objects,report=build_world_mountain_frame(collection)
    bpy.data.libraries.write(str(OUT/'world-mountain-frame.blend'),{collection},fake_user=True)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.gltf(filepath=str(OUT/'world-mountain-frame.glb'),export_format='GLB',use_selection=True,
                               export_cameras=False,export_lights=False)
    (OUT/'report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    if args.build_only:return
    scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
    scene.cycles.device='CPU'
    scene.render.threads_mode='FIXED';scene.render.threads=4
    if args.gpu:
        prefs=bpy.context.preferences.addons['cycles'].preferences
        prefs.compute_device_type='CUDA';prefs.get_devices()
        for device in prefs.devices:device.use=device.type=='CUDA'
        scene.cycles.device='GPU'
    scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    if args.contact_proof:
        # Preserve the actual native world camera; crop only the render region.
        scene.render.resolution_x=1600;scene.render.resolution_y=1200
        scene.cycles.samples=10
        scene.render.use_border=True;scene.render.use_crop_to_border=True
        scene.render.border_min_x=.39;scene.render.border_max_x=.79
        scene.render.border_min_y=.63;scene.render.border_max_y=.90
        scene.render.filepath=str(OUT/'v3-connected-foot-contact.png')
        bpy.ops.render.render(write_still=True)
        print('MOUNTAIN_FOOT_CONTACT_PROOF_READY',json.dumps(report),flush=True)
        return
    if not args.close_only:
        scene.render.filepath=str(OUT/'v2-majestic-context.png');bpy.ops.render.render(write_still=True)
    data=bpy.data.cameras.new('Mountain_Close_Review');camera=bpy.data.objects.new('Mountain_Close_Review',data)
    collection.objects.link(camera);camera.location=(-18,7,16)
    camera.rotation_euler=(Vector((-15,30,5.5))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.lens=52;scene.camera=camera
    # Component close-up retains native lighting; foreground foliage is omitted
    # only in this diagnostic image, never from the full context or saved world.
    for ob in scene.objects:
        if ob.type=='MESH' and ob not in objects:ob.hide_render=True
    scene.render.resolution_y=700
    scene.render.filepath=str(OUT/'v2-majestic-close.png');bpy.ops.render.render(write_still=True)
    print('WORLD_MOUNTAIN_FRAME_READY',json.dumps(report),flush=True)


if __name__=='__main__':main()
