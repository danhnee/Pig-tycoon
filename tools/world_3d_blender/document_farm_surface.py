"""Organic metre-scale Farm_Main surface; standalone art component, not balance.

Blender X=east,Y=north,Z=up. sample_ground_z accepts scalars or numpy arrays.
No live Assets writes. Import build_blender_surface into the scene assembler.
"""
from pathlib import Path
import json
import math
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '.collab/artifacts/farm-surface-codex'
BOUNDS = (-35., 155., -35., 125.)
PONDS = ((42., 49., 6.2, 3.8), (60., 40., 7.8, 4.6), (77., 32., 5.6, 3.4))
# Worn trails are narrower than the traversable grass corridors.
TRAILS = (((60,0),(59,10),(55,19),(46,25),(33,36),(30,48),(36,58),(44,65),(44,72)),
          ((46,25),(64,25),(80,28),(89,40),(89,52),(82,61),(76,65),(76,72)),
          ((44,65),(60,63),(76,65)), ((60,78),(60,90)),
          ((0,48),(12,47),(22,45),(30,48)), ((12,47),(16,30),(17,15),(38,14),(59,10)))

def smoothstep(a, b, x):
    t = np.clip((np.asarray(x)-a)/(b-a), 0, 1)
    return t*t*(3-2*t)

def base_height(x, y):
    x,y=np.broadcast_arrays(np.asarray(x,dtype=float),np.asarray(y,dtype=float))
    z=1.15+.35*np.sin(x*.052+y*.023)+.24*np.cos(y*.083-x*.019)
    z += .13*np.sin(x*.13+y*.08)+.07*np.cos(x*.25-y*.15)
    # Rolling outer landscape rises gradually, leaving raid approaches traversable.
    z += 1.7*smoothstep(87,125,y)*(0.65+.35*np.sin(x*.035)**2)
    z += 1.1*(1-smoothstep(-30,8,x))+.7*smoothstep(112,150,x)
    # One gently levelled shelter site, with broad smooth blending into the meadow.
    dx=np.maximum(np.abs(x-60)-18,0);dy=np.maximum(np.abs(y-72)-6,0)
    pad=1-smoothstep(0,7,np.hypot(dx,dy))
    return z*(1-pad)+1.5*pad

def pond_distance(x,y,pond):
    cx,cy,rx,ry=pond
    dx=(np.asarray(x)-cx)/rx;dy=(np.asarray(y)-cy)/ry
    theta=np.arctan2(dy,dx)
    shape=1+.12*np.sin(3*theta+.8)+.055*np.sin(5*theta+cx*.1)+.025*np.cos(9*theta)
    return np.hypot(dx,dy)/shape

def pond_water_level(pond):
    return float(base_height(pond[0],pond[1]))-.12

def sample_ground_z(x,y):
    x,y=np.broadcast_arrays(np.asarray(x,dtype=float),np.asarray(y,dtype=float))
    z=base_height(x,y)
    for pond in PONDS:
        d=pond_distance(x,y,pond);water=pond_water_level(pond)
        bed=water-.85*np.clip(1-d*d,0,1)**.8
        bank=water+.28*smoothstep(1,1.4,d)
        basin=np.where(d<1,bed,bank)
        blend=1-smoothstep(1.15,1.9,d)
        z=z*(1-blend)+basin*blend
    return float(z) if z.ndim==0 else z

def nearest_trail(x,y):
    x,y=np.broadcast_arrays(np.asarray(x),np.asarray(y))
    distance=np.full(x.shape,np.inf)
    for route in TRAILS:
        for a,b in zip(route,route[1:]):
            dx=b[0]-a[0];dy=b[1]-a[1]
            t=np.clip(((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy),0,1)
            distance=np.minimum(distance,np.hypot(x-a[0]-t*dx,y-a[1]-t*dy))
    return distance

def shoreline(pond,count=96):
    cx,cy,rx,ry=pond;theta=np.linspace(0,2*np.pi,count,endpoint=False)
    shape=1+.12*np.sin(3*theta+.8)+.055*np.sin(5*theta+cx*.1)+.025*np.cos(9*theta)
    return np.column_stack((cx+rx*shape*np.cos(theta),cy+ry*shape*np.sin(theta)))

def sample_height(x,y):
    return sample_ground_z(x,y)

def pond_shoreline_points():
    return [{'center':list(p[:2]),'points':shoreline(p).tolist(),'water_z':pond_water_level(p)} for p in PONDS]

def build_farm_surface(collection):
    objects=build_blender_surface()
    for ob in objects:
        for current in list(ob.users_collection):current.objects.unlink(ob)
        collection.objects.link(ob)
    return objects

def texture(size=2048):
    from PIL import Image
    x,y=np.meshgrid(np.linspace(BOUNDS[0],BOUNDS[1],size),np.linspace(BOUNDS[3],BOUNDS[2],size))
    rng=np.random.default_rng(18102026)
    macro=(np.sin(x*.16+y*.11)+np.sin(y*.27-x*.08)+np.cos(x*.081-y*.16))/6+.5
    fine=rng.normal(0,.025,x.shape)+.02*np.sin(x*19+y*7)*np.sin(y*23)
    green=np.array([.39,.43,.20]);gold=np.array([.67,.57,.27])
    color=green+(gold-green)*macro[...,None]+fine[...,None]
    trail=1-smoothstep(.32,1.05,nearest_trail(x,y)+.12*np.sin(x*2.3+y))
    color=color*(1-trail[...,None]*.85)+np.array([.53,.43,.28])*trail[...,None]*.85
    for pond in PONDS:
        d=pond_distance(x,y,pond)
        shore=(1-smoothstep(1.2,1.8,d))*smoothstep(.7,1.03,d)
        color=color*(1-shore[...,None])+(np.array([.36,.32,.21])+fine[...,None])*shore[...,None]
    OUT.mkdir(parents=True,exist_ok=True)
    target=OUT/'farm-ground-albedo.png'
    Image.fromarray(np.uint8(np.clip(color,0,1)*255)).save(target)
    return target

def build_blender_surface(step=.75):
    import bpy
    from PIL import Image
    OUT.mkdir(parents=True,exist_ok=True)
    path=texture()
    x,y=np.meshgrid(np.linspace(BOUNDS[0],BOUNDS[1],math.ceil((BOUNDS[1]-BOUNDS[0])/step)+1),
                    np.linspace(BOUNDS[2],BOUNDS[3],math.ceil((BOUNDS[3]-BOUNDS[2])/step)+1))
    z=sample_ground_z(x,y);ny,nx=x.shape
    vertices=np.column_stack((x.ravel(),y.ravel(),z.ravel()))
    i=np.arange((ny-1)*nx).reshape(ny-1,nx)[:,:-1].ravel()
    faces=np.vstack((np.column_stack((i,i+1,i+nx)),np.column_stack((i+1,i+nx+1,i+nx))))
    mesh=bpy.data.meshes.new('OrganicFarmSurface');mesh.from_pydata(vertices.tolist(),[],faces.tolist());mesh.update()
    ob=bpy.data.objects.new('Ground_OrganicMeadow',mesh);bpy.context.collection.objects.link(ob)
    uv=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        v=mesh.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=((v.x-BOUNDS[0])/(BOUNDS[1]-BOUNDS[0]),(v.y-BOUNDS[2])/(BOUNDS[3]-BOUNDS[2]))
    mat=bpy.data.materials.new('Meadow_dry_grass_soil');mat.use_nodes=True
    bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.95
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path));tex.image.pack()
    mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);mesh.materials.append(mat)
    for p in mesh.polygons:p.use_smooth=True
    result=[ob]
    for index,pond in enumerate(PONDS):
        cx,cy,rx,ry=pond;outline=shoreline(pond,128);water=pond_water_level(pond)
        v=[(cx,cy,water)]+[(float(px),float(py),water) for px,py in (np.array([cx,cy])+(outline-[cx,cy])*.965)]
        f=[(0,j+1,(j+1)%128+1) for j in range(128)]
        m=bpy.data.meshes.new(f'Pond_{index}_surface');m.from_pydata(v,[],f);m.update()
        w=bpy.data.objects.new(f'Water_OrganicPond_{index}',m);bpy.context.collection.objects.link(w)
        uv=m.uv_layers.new(name='UVMap')
        for loop in m.loops:
            p=m.vertices[loop.vertex_index].co
            uv.data[loop.index].uv=((p.x-cx)/(rx*2.5)+.5,(p.y-cy)/(ry*2.5)+.5)
        tx,ty=np.meshgrid(np.linspace(cx-rx*1.25,cx+rx*1.25,512),np.linspace(cy+ry*1.25,cy-ry*1.25,512))
        depth=np.clip(water-sample_ground_z(tx,ty),0,.85)/.85
        shallow=np.array([.31,.53,.47]);deep=np.array([.12,.37,.40])
        color=shallow+(deep-shallow)*depth[...,None]+.008*np.sin(tx*9+ty*6)[...,None]
        pth=OUT/f'pond-{index}-albedo.png';Image.fromarray(np.uint8(np.clip(color,0,1)*255)).save(pth)
        mat=bpy.data.materials.new(f'Water_depth_{index}');mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.18
        bsdf.inputs['Metallic'].default_value=.08
        tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(pth));tex.image.pack()
        mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);m.materials.append(mat);result.append(w)
    return result

def verify():
    x,y=np.meshgrid(np.linspace(0,120,241),np.linspace(0,90,181));z=sample_ground_z(x,y)
    assert np.isfinite(z).all()
    pond_results=[]
    for pond in PONDS:
        edge=shoreline(pond);level=pond_water_level(pond)
        errors=np.abs(sample_ground_z(edge[:,0],edge[:,1])-level)
        assert errors.max()<1e-8,errors.max()
        assert sample_ground_z(pond[0],pond[1])<level-.8
        pond_results.append({'center':list(pond[:2]),'water_level':level,'shore_contact_error':float(errors.max())})
    report={'status':'PASS','meaning':'surface geometry only, not finished map or runtime nav acceptance',
            'bounds':BOUNDS,'ground_height_range':[float(z.min()),float(z.max())],'ponds':pond_results,
            'units':'metres','axes':'Blender X east Y north Z up','trail_width_vs_clearance':'narrow worn tracks inside traversable grass corridors'}
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'surface-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report,indent=2))

def build():
    runtime=ROOT/'.collab/artifacts/mountain-reference-study-codex/runtime'
    sys.path.insert(0,str(runtime))
    import bpy
    from mathutils import Vector
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.use_scripts_auto_execute=False
    objects=build_blender_surface()
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.gltf(filepath=str(OUT/'organic-farm-surface.glb'),use_selection=True,export_format='GLB')
    bpy.ops.export_scene.fbx(filepath=str(OUT/'organic-farm-surface.fbx'),use_selection=True,object_types={'MESH'},
                             axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='COPY',embed_textures=True)
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
    scene.render.threads_mode='FIXED';scene.render.threads=4
    scene.render.resolution_x=1400;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
    scene.world=bpy.data.worlds.new('Sky');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.62,.72,.85,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.55
    bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=2.1;sun.data.angle=.15
    sun.rotation_euler=(math.radians(35),math.radians(-15),math.radians(-25))
    bpy.ops.object.camera_add(location=(100,-45,80));camera=bpy.context.object
    camera.rotation_euler=(Vector((60,43,0))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO';camera.data.ortho_scale=140;scene.camera=camera
    scene.render.filepath=str(OUT/'surface-overview-component.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'organic-farm-surface.blend'))
    bpy.ops.render.render(write_still=True)
    verify()

if __name__=='__main__':
    if '--build' in sys.argv:build()
    else:verify()
