"""Independent local-origin, genuine mesh alpine art study; never edits the map.

Run with Blender 5.3 background/factory-startup. All products stay in the study
directory. Reference imagery informs shape/material quality, not world geography.
"""
import json
import math
from pathlib import Path
import random
import subprocess
import sys

import bpy
import bmesh
import numpy as np
from mathutils import Vector

OUT = Path(__file__).resolve().parents[1] / '.collab/artifacts/mountain-reference-study-codex'
OUT.mkdir(parents=True, exist_ok=True)
RNG = random.Random(18062)
XMIN, XMAX, YMIN, YMAX = -25., 25., -17., 22.

# Unequal, branching ridge networks. The middle node is a saddle, not a summit
# duplication. The second massif sits significantly forward of the main divide.
RIDGES = [
    ([(-21, 8, 3), (-16, 9, 8), (-12, 8, 12), (-9, 7, 17),
      (-6, 7.7, 20.4), (-4, 8.6, 16), (-1.6, 10.5, 18),
      (1, 12, 11), (5, 13, 8)], 1.38),
    ([(0, 2, 4), (4, 3, 9), (8, 4, 14.4), (10, 5, 11.0),
      (12, 7, 12.1), (16, 9, 5.8), (21, 11, 2)], 1.33),
    ([(-11, 15, 5), (-5, 16, 10), (1, 17, 12), (4, 16, 9),
      (9, 17, 11), (15, 18, 6)], 1.35),
    ([(-6, 7.7, 20.4), (-8, 3, 12.8), (-12, -1, 7.4),
      (-15, -6, 3.6), (-20, -10, .7)], 1.63),
    ([(-6, 7.7, 20.4), (-3.2, 2.8, 12.1), (-1, -.5, 8.8),
      (-2, -5, 4.8), (1, -11, 1.2)], 1.70),
    ([(-12, 8, 12), (-15, 3, 7.1), (-19, 0, 4.5), (-22, -5, .8)], 1.55),
    ([(-1.6, 10.5, 18), (2.8, 8.2, 10), (4.2, 5, 6)], 1.75),
    ([(8, 4, 14.4), (6, -.4, 8.8), (3.2, -4, 4.8), (6, -10, 1.4)], 1.72),
    ([(8, 4, 14.4), (11, 0, 9), (14, -3, 5.4), (18, -9, 1.1)], 1.82),
    ([(12, 7, 12.1), (16, 4, 6), (21, 1, 1.3)], 1.60),
    # Low wooded buttresses share the slope surface; no foreground rock wall.
    ([(-20, -7, .9), (-16, -6, 3.1), (-12, -9, 4.8), (-8, -12, 1)], 1.2),
    ([(-4, -10, 1.2), (-1, -7, 4.1), (3, -9, 2.4), (7, -13, .8)], 1.3),
    ([(9, -8, 1.7), (12, -6, 4.4), (15, -9, 3.1), (19, -12, .7)], 1.2),
]


def height(x, y, details=True):
    x, y = np.broadcast_arrays(np.asarray(x, dtype=float), np.asarray(y, dtype=float))
    # Domain warp bends erosion channels coherently at every resolution.
    xx = x + .19 * np.sin(y * 1.6 + x * .53) + .11 * np.sin(y * 3.1)
    yy = y + .16 * np.sin(x * 1.33 + y * .7)
    z = .28 + .23 * np.sin(x * .33 + y * .15) * np.sin(y * .45)
    for nodes, slope in RIDGES:
        wooded = max(n[2] for n in nodes) < 6
        for a, b in zip(nodes, nodes[1:]):
            ax, ay, ah = a
            bx, by, bh = b
            dx, dy = bx - ax, by - ay
            t = np.clip(((xx - ax) * dx + (yy - ay) * dy) / (dx * dx + dy * dy), 0, 1)
            distance = np.hypot(xx - ax - t * dx, yy - ay - t * dy)
            ht = t*t*(3-2*t) if wooded else t
            h = ah + (bh - ah) * ht
            shape = (h*np.exp(-(distance/2.3)**2) if wooded else
                     h - slope * distance - .025 * distance ** 2)
            z = np.maximum(z, shape)
    # Continuous tilted rock ribs, rather than independent cone objects.
    if details:
        relief = (.25 * np.sin(xx * 2.65 + yy * 1.14 + z * .65)
                  + .13 * np.sin(xx * 5.1 - yy * 2.9 + z * .9)
                  + .075 * np.sin(xx * 10.1 + yy * 6.6))
        z += relief * np.clip(z / 5, 0, 1)
        z += .11 * np.sin(xx * 1.7 - yy * 2) * np.clip(1 - z / 7, 0, 1)
    # A nonrectangular low skirt; boundary reaches a consistent base elevation.
    edge = np.clip(np.minimum.reduce([(x-XMIN)/3, (XMAX-x)/3,
                                     (y-YMIN)/3, (YMAX-y)/3]), 0, 1)
    return np.maximum(.12, z * edge)


def mesh_object(name, vertices, faces, material=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(vertices, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    if material:
        me.materials.append(material)
    return ob


def mat(name, color, roughness=.9):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = roughness
    return m


def terrain():
    nx, ny = 221, 177
    xs, ys = np.linspace(XMIN, XMAX, nx), np.linspace(YMIN, YMAX, ny)
    xx, yy = np.meshgrid(xs, ys)
    # Jitter interior points to avoid conspicuous regular tessellation.
    rng = np.random.default_rng(82)
    jx, jy = rng.uniform(-.045, .045, (ny, nx)), rng.uniform(-.045, .045, (ny, nx))
    jx[[0, -1], :] = 0; jx[:, [0, -1]] = 0
    jy[[0, -1], :] = 0; jy[:, [0, -1]] = 0
    xx += jx; yy += jy
    zz = height(xx, yy)
    verts = np.stack([xx, yy, zz], axis=-1).reshape(-1, 3).tolist()
    faces = []
    for j in range(ny - 1):
        for i in range(nx - 1):
            a = j * nx + i; b = a + 1; c = a + nx; d = c + 1
            faces.extend([(a, b, d), (a, d, c)] if (i + j) % 2 else [(a, b, c), (b, d, c)])
    # Closed shell with reversed bottom, consistent directed boundary edges.
    boundary = list(range(nx))
    boundary += [j * nx + nx - 1 for j in range(1, ny)]
    boundary += list(range(ny * nx - 2, (ny - 1) * nx - 1, -1))
    boundary += [j * nx for j in range(ny - 2, 0, -1)]
    bottom_ids = []
    for idx in boundary:
        bottom_ids.append(len(verts)); verts.append([verts[idx][0], verts[idx][1], -.45])
    center = len(verts); verts.append([0, 2.5, -.45])
    for k, a in enumerate(boundary):
        b = boundary[(k + 1) % len(boundary)]
        da, db = bottom_ids[k], bottom_ids[(k+1) % len(boundary)]
        faces.extend([(a, da, db), (a, db, b), (center, db, da)])
    ob = mesh_object('Continuous sculpted massif — closed rock and soil', verts, faces)
    uv = ob.data.uv_layers.new(name='Terrain projection')
    for loop in ob.data.loops:
        v = ob.data.vertices[loop.vertex_index].co
        uv.data[loop.index].uv = ((v.x-XMIN)/(XMAX-XMIN), (v.y-YMIN)/(YMAX-YMIN))
    for p in ob.data.polygons:
        p.use_smooth = False
    return ob


def surface_texture(ob):
    w, h = 1600, 1280
    xx, yy = np.meshgrid(np.linspace(XMIN, XMAX, w), np.linspace(YMIN, YMAX, h))
    z = height(xx, yy)
    gy, gx = np.gradient(z, (YMAX-YMIN)/(h-1), (XMAX-XMIN)/(w-1))
    slope = np.hypot(gx, gy)
    noise = np.sin(xx * 3.7 + yy * 1.5) * np.sin(xx * 7.3 - yy * 5.6)
    rock = np.zeros((h, w, 3)); rock[:] = (.37, .44, .52)
    warm = np.clip((gx - gy*.6) * .32 + .23, 0, 1)
    rock = rock * (1-warm[..., None]*.72) + np.array([.69,.52,.38]) * warm[...,None]*.72
    vein = .014*np.sin(z*5.2+xx*.72+yy*.41) + noise*.012
    rock += vein[...,None]
    soil = np.zeros_like(rock); soil[:] = (.30,.34,.15)
    soil += (.045*np.sin(xx*.8+yy*.3)+noise*.022)[...,None]
    grass = np.clip((6.8-z)/3,0,1) * np.clip((2.15-slope)/1.4,0,1)
    color = rock*(1-grass[...,None]) + soil*grass[...,None]
    # Snow follows descending valley paths; it does not cap every peak.
    snow_paths = [
        [(-7.4,5.8),(-8.9,3.7),(-8.2,1.1),(-7.7,-1.8),(-6,-4)],
        [(-4.5,6.6),(-3,4.8),(-1.4,3),(0,1.1),(1,-1)],
        [(-2,9.3),(-.2,7.8),(1.3,6.2),(1.8,4.2)],
        [(-11.3,7),(-12.1,4.7),(-11.5,2.2),(-10.5,.3)],
        [(7.2,3.3),(7,-.1),(8,-1.9),(8.5,-4)],
        [(9.1,4.4),(10.2,2.2),(9.9,-.2)],
        [(11.9,6),(13.7,4.8),(14.8,3.3),(16,1.4)],
        [(-4.8,9),(-6.5,11.5),(-7,13.7)],
        [(0,16.2),(1,14.5),(2,13.7)],
    ]
    snow = np.zeros((h,w))
    for path in snow_paths:
        for k, (a,b) in enumerate(zip(path,path[1:])):
            dx,dy = b[0]-a[0],b[1]-a[1]
            t=np.clip(((xx-a[0])*dx+(yy-a[1])*dy)/(dx*dx+dy*dy),0,1)
            distance=np.hypot(xx-a[0]-t*dx,yy-a[1]-t*dy)
            width=.045+.032*k+.025*np.sin(yy*3+xx*1.3)
            broken=np.clip(.55+1.1*np.sin(yy*2.3+xx*1.4),0,1)
            strip=np.clip((width-distance)*25,0,1)*np.clip((z-6.8)/3,0,1)*broken
            snow=np.maximum(snow,strip)
    color=color*(1-snow[...,None])+np.array([.77,.84,.88])*snow[...,None]
    # Fine packed albedo is exported into GLB and remains valid without Blender nodes.
    rgba=np.ones((h,w,4),dtype=np.float32);rgba[:,:,:3]=np.clip(color,0,1)
    image=bpy.data.images.new('Massif hand-directed stone moss snow albedo',width=w,height=h)
    image.pixels.foreach_set(rgba.ravel()); image.filepath_raw=str(OUT/'massif-albedo.png')
    image.file_format='PNG';image.save();image.pack()
    material=mat('Blue slate / warm rock / moss / narrow snow',(.4,.4,.4))
    nodes=material.node_tree.nodes;links=material.node_tree.links
    tex=nodes.new('ShaderNodeTexImage');tex.image=image
    links.new(tex.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color'])
    ob.data.materials.append(material)


class Batch:
    def __init__(self, name, materials):
        self.name=name;self.materials=materials;self.vertices=[];self.faces=[];self.indices=[]

    def add(self, vertices, faces, idx):
        offset=len(self.vertices);self.vertices.extend(vertices)
        self.faces.extend([tuple(offset+i for i in f) for f in faces]);self.indices.extend([idx]*len(faces))

    def finish(self):
        ob=mesh_object(self.name,self.vertices,self.faces)
        for m in self.materials:ob.data.materials.append(m)
        for p,i in zip(ob.data.polygons,self.indices):p.material_index=i
        return ob


def branch(batch, a, b, r1, r2, idx, sides=7):
    a,b=Vector(a),Vector(b); direction=(b-a).normalized()
    u=direction.cross(Vector((0,0,1)))
    if u.length<.01:u=direction.cross(Vector((0,1,0)))
    u.normalize();v=direction.cross(u)
    verts=[]
    for center,radius in [(a,r1),(b,r2)]:
        for k in range(sides):
            q=center+(u*math.cos(k*math.tau/sides)+v*math.sin(k*math.tau/sides))*radius
            verts.append(tuple(q))
    faces=[]
    for k in range(sides):
        n=(k+1)%sides;faces.extend([(k,n,n+sides),(k,n+sides,k+sides)])
    faces += [tuple(reversed(range(sides))),tuple(range(sides,2*sides))]
    batch.add(verts,faces,idx)


def tuft(batch, center, radii, idx, rotation=0):
    # Sculpted, flattened, lobed leaf masses; closed irregular two-ring solids.
    x,y,z=center;rx,ry,rz=radii;n=7;verts=[]
    for ring,zz,rr in [(0,-.25,.81),(1,.35,1)]:
        for k in range(n):
            a=k*math.tau/n+rotation+ring*.16
            wobble=RNG.uniform(.82,1.12)
            verts.append((x+math.cos(a)*rx*rr*wobble,y+math.sin(a)*ry*rr*wobble,z+zz*rz+RNG.uniform(-.14,.14)*rz))
    verts += [(x-.13*rx,y,z+rz),(x,y,z-.62*rz)]
    faces=[]
    for k in range(n):
        j=(k+1)%n
        faces.extend([(k,j,n+j),(k,n+j,n+k),(n+k,n+j,2*n),(j,k,2*n+1)])
    batch.add(verts,faces,idx)


def plants():
    palettes=[mat('Bark umber',(.16,.09,.045)),mat('Needles shadow',(.055,.13,.10)),
              mat('Needles green',(.11,.24,.15)),mat('Needles lit tips',(.23,.34,.13)),
              mat('Autumn amber',(.67,.29,.035)),mat('Autumn gold',(.88,.48,.045)),
              mat('Autumn pale tips',(.96,.64,.12)),mat('Autumn ochre shade',(.48,.21,.035))]
    conifers=Batch('Irregular wooded slopes — branching fir groves',palettes)
    oaks=Batch('Foothill groves — open branching amber crowns',palettes)
    points=[]
    attempts=0
    # Irregular clusters have variable elevation, depth, and spacing.
    centers=[(-16,-5,3.8),(-11,-10,3),(-4,-5,2.5),(-2,-10,3),(10,-6,3.5),
             (15,-9,3.5),(18,1,2.8),(-17,2,2.4),(2,-1,2.1),(-10,0,2)]
    while len(points)<105 and attempts<4000:
        attempts+=1;cx,cy,spread=RNG.choice(centers)
        x=RNG.gauss(cx,spread*.58);y=RNG.gauss(cy,spread*.55);z=float(height(x,y))
        gx=float(height(x+.12,y)-height(x-.12,y))/.24
        gy=float(height(x,y+.12)-height(x,y-.12))/.24
        if not (.8<z<7.5 and np.hypot(gx,gy)<2.1):continue
        if any((x-a)**2+(y-b)**2<.70 for a,b,c in points):continue
        points.append((x,y,z))
    for i,(x,y,z) in enumerate(points):
        h=RNG.uniform(1.35,2.7)*(1 if z<5 else .77)
        fir=(i%5!=0 and (i%5!=1 or z>4))
        batch=conifers if fir else oaks
        lean=(RNG.uniform(-.12,.12),RNG.uniform(-.12,.12))
        branch(batch,(x,y,z-.15),(x+lean[0],y+lean[1],z+h),h*.037,h*.009,0)
        if fir:
            for level in range(7):
                t=.22+level*.105; radius=h*.30*(1-t)**.8
                angle0=RNG.random()*math.tau
                for arm in range(5):
                    angle=angle0+arm*math.tau/5
                    length=radius*RNG.uniform(.80,1.16)
                    end=(x+math.cos(angle)*length,y+math.sin(angle)*length,z+h*t-.10*h)
                    branch(batch,(x,y,z+h*t),end,h*.014,h*.003,0,5)
                    tuft(batch,(end[0]*.65+x*.35,end[1]*.65+y*.35,end[2]+h*.03),
                         (length*.72,length*.52,h*.10),RNG.choice([1,2,2,3]),angle)
            tuft(batch,(x+lean[0],y+lean[1],z+h*.90),(h*.09,h*.085,h*.18),2)
        else:
            # Visible forked trunk, offset irregular crown, smaller edge leaflets.
            for arm in range(RNG.randint(5,7)):
                angle=arm*2.4+RNG.uniform(-.4,.4)
                length=h*RNG.uniform(.23,.43)
                end=(x+math.cos(angle)*length,y+math.sin(angle)*length,z+h*RNG.uniform(.44,.87))
                branch(batch,(x,y,z+h*.24),end,h*.023,h*.007,0,6)
                for lobe in range(4):
                    c=(end[0]+RNG.uniform(-.19,.19)*h,end[1]+RNG.uniform(-.19,.19)*h,end[2]+RNG.uniform(-.08,.22)*h)
                    tuft(batch,c,(h*.19,h*.18,h*RNG.uniform(.24,.34)),RNG.choice([4,5,5,6,7]),angle)
    return [conifers.finish(),oaks.finish()]


def embedded_rocks():
    materials=[mat('Embedded stone warm',(.36,.32,.28)),mat('Embedded stone blue',(.27,.32,.36)),
               mat('Lichen stone',(.35,.37,.21))]
    batch=Batch('Scattered embedded buttress splinters and scree',materials)
    # Small outcrops emerging from sloping terrain, scattered over depth.
    for i in range(165):
        x=RNG.uniform(-21,21);y=RNG.uniform(-13,9);z=float(height(x,y))
        if not 1<z<7:continue
        size=RNG.uniform(.20,.60)
        tuft(batch,(x,y,z+.10),(size*.75,size*.62,size*RNG.uniform(.7,1.7)),RNG.randrange(3),RNG.random()*6)
    return batch.finish()


def validate(objects):
    records=[];all_vertices=[]
    for ob in objects:
        me=ob.data;me.calc_loop_triangles()
        vertices=np.asarray([tuple(v.co) for v in me.vertices],dtype=float)
        triangles=np.asarray([tuple(t.vertices) for t in me.loop_triangles],dtype=int)
        assert np.isfinite(vertices).all(),ob.name
        assert triangles.min()>=0 and triangles.max()<len(vertices),ob.name
        p=vertices[triangles];normals=np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0])
        area=np.linalg.norm(normals,axis=1)*.5
        assert area.min()>1e-10,(ob.name,area.min())
        bm=bmesh.new();bm.from_mesh(me)
        nonmanifold=sum(not e.is_manifold for e in bm.edges)
        inconsistent=sum(e.is_manifold and not e.is_contiguous for e in bm.edges)
        volume=bm.calc_volume(signed=True);bm.free()
        assert nonmanifold==0,(ob.name,nonmanifold)
        assert inconsistent==0,(ob.name,inconsistent)
        assert volume>0,(ob.name,volume)
        records.append(dict(name=ob.name,vertices=len(vertices),triangles=len(triangles),
                            nonmanifold_edges=nonmanifold,inconsistent_winding_edges=inconsistent,
                            minimum_triangle_area=float(area.min()),signed_volume=volume))
        all_vertices.append(vertices)
    vv=np.concatenate(all_vertices)
    report=dict(status='PASS',coordinate_system='Blender Z up; glTF exports Y up',
                local_origin=True,world_placement=False,objects=records,
                bounds_min=vv.min(axis=0).tolist(),bounds_max=vv.max(axis=0).tolist(),
                vertices=len(vv),triangles=sum(r['triangles'] for r in records),
                note='Closed components may intersect intentionally (tree branches, foliage, embedded rocks). Not a collision or LOD asset.')
    (OUT/'geometry-validation.json').write_text(json.dumps(report,indent=2),encoding='utf8')
    print('GEOMETRY_VALIDATION',json.dumps(report),flush=True)


def setup_scene():
    scene=bpy.context.scene
    if scene.world is None:scene.world=bpy.data.worlds.new('Study sky')
    scene.render.engine='CYCLES';scene.cycles.samples=32
    scene.cycles.use_denoising=True
    # Use CPU consistently when concurrent Blender workers occupy the GPU.
    scene.cycles.device='CPU';scene.render.threads_mode='FIXED';scene.render.threads=8
    scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
    scene.world.color=(.55,.65,.78)
    scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.64,.73,.86,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.55
    scene.view_settings.view_transform='AgX'
    bpy.ops.object.light_add(type='AREA',location=(-24,-18,35))
    key=bpy.context.object;key.name='Warm sky softbox';key.data.energy=4800;key.data.shape='DISK';key.data.size=15
    key.data.color=(1,.80,.58);key.rotation_euler=(Vector((0,3,6))-key.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.light_add(type='SUN',location=(-20,-15,30))
    sun=bpy.context.object;sun.name='Late golden sun';sun.data.energy=3.4;sun.data.angle=.07;sun.data.color=(1,.83,.63)
    sun.rotation_euler=(Vector((0,3,0))-sun.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add()
    cam=bpy.context.object;cam.name='Study camera';cam.data.type='ORTHO';scene.camera=cam
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.52))
    floor=bpy.context.object;floor.name='Studio ground (not exported)';floor.data.materials.append(mat('Studio warm paper',(.72,.75,.77)))
    return scene,cam,floor


def render(scene,cam,name,position,target,scale):
    cam.location=position;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.ortho_scale=scale;scene.render.filepath=str(OUT/name)
    bpy.ops.render.render(write_still=True)
    print('RENDER_READY',name,flush=True)


def main():
    tasklist=subprocess.check_output(['tasklist','/FO','CSV'],text=True,errors='replace')
    assert not any(line.startswith('"Unity.exe"') for line in tasklist.splitlines()),'Close Unity Editor before creating study'
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    mountain=terrain();print('STAGE terrain mesh ready',flush=True)
    surface_texture(mountain);print('STAGE packed albedo ready',flush=True)
    objects=[mountain]+plants()+[embedded_rocks()]
    validate(objects)
    scene,cam,floor=setup_scene()
    # Export only authored local-origin geometry, never lighting/studio backdrop.
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=mountain
    bpy.ops.export_scene.gltf(filepath=str(OUT/'mountain-foothill-study.glb'),export_format='GLB',use_selection=True)
    cam.location=(31,-48,29);cam.rotation_euler=(Vector((0,3,8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=59
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'mountain-foothill-study.blend'))
    render(scene,cam,'01-front-textured.png',(27,-51,28),(0,3,8),57)
    render(scene,cam,'02-rear-textured.png',(-30,45,29),(0,4,8),57)
    render(scene,cam,'03-foothill-close-textured.png',(17,-27,17),(0,-3,5.4),31)
    render(scene,cam,'04-ridge-close-textured.png',(15,-26,22),(-5,6,12),31)
    clay=mat('Neutral clay proof',(.48,.49,.51));scene.view_layers[0].material_override=clay
    render(scene,cam,'05-front-clay.png',(27,-51,28),(0,3,8),57)
    scene.view_layers[0].material_override=None
    wire=mat('Mesh topology wire',(.11,.15,.19));nodes=wire.node_tree.nodes;links=wire.node_tree.links
    w=nodes.new('ShaderNodeWireframe');w.use_pixel_size=True;w.inputs[0].default_value=.6
    mix=nodes.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(.60,.64,.69,1);mix.inputs[2].default_value=(.035,.055,.075,1)
    links.new(w.outputs[0],mix.inputs[0]);links.new(mix.outputs[0],nodes.get('Principled BSDF').inputs['Base Color'])
    scene.view_layers[0].material_override=wire
    render(scene,cam,'06-ridge-wireframe.png',(15,-26,22),(-5,6,12),31)
    scene.view_layers[0].material_override=None
    print('STUDY_COMPLETE',str(OUT),flush=True)


VISTA_RIDGES = [
    ([(-245,142,12),(-204,151,37),(-175,145,61),(-139,151,96),
      (-119,163,74),(-90,176,83),(-48,170,39)],.94),
    ([(-57,132,24),(-17,144,45),(20,159,67),(47,166,53),
      (76,180,85),(108,188,59),(149,192,65),(190,194,33),(235,188,9)],.92),
    ([(-237,222,20),(-185,225,48),(-148,231,67),(-100,243,52),
      (-46,239,72),(-11,242,49),(37,251,63),(88,246,44),(160,249,32),(220,250,12)],1.0),
    ([(-139,151,96),(-157,121,55),(-180,99,27),(-196,69,13)],1.30),
    ([(-139,151,96),(-118,116,50),(-106,87,25),(-80,57,10)],1.32),
    ([(-90,176,83),(-66,139,45),(-43,113,23),(-28,81,11)],1.24),
    ([(20,159,67),(4,122,39),(-8,97,18),(-25,66,8)],1.22),
    ([(76,180,85),(91,144,46),(112,118,23),(137,92,10)],1.22),
    ([(149,192,65),(165,155,35),(185,126,17),(217,93,7)],1.18),
]
VISTA_HILLS=[(-205,57,18,32,23),(-158,71,25,35,27),(-108,52,19,35,22),
             (-54,70,23,31,24),(0,57,18,34,25),(49,79,25,39,27),
             (111,66,20,33,24),(166,79,24,34,25),(210,65,15,32,21)]


def vista_height(x,y):
    """Metre-scale geology with broad shoulders and a rounded wooded apron."""
    x,y=np.broadcast_arrays(np.asarray(x,dtype=float),np.asarray(y,dtype=float))
    z=np.zeros(x.shape)
    for nodes,slope in VISTA_RIDGES:
        for a,b in zip(nodes,nodes[1:]):
            dx,dy=b[0]-a[0],b[1]-a[1]
            t=np.clip(((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy),0,1)
            d=np.hypot(x-a[0]-t*dx,y-a[1]-t*dy)
            h=a[2]+t*(b[2]-a[2])
            # Broad concave side faces; lower spurs round into a talus apron.
            ridge=np.maximum(0,h-slope*d-.0018*d*d)
            low=np.clip((30-h)/22,0,1)
            rounded=h*np.exp(-(d/20)**2)
            z=np.maximum(z,ridge*(1-low)+rounded*low)
    hills=np.zeros(x.shape)
    for cx,cy,h,rx,ry in VISTA_HILLS:
        hills=np.maximum(hills,h*np.exp(-(((x-cx)/rx)**2+((y-cy)/ry)**2)*1.1))
    # Smoothly join the broad hills to the mountain feet; no separate boulder row.
    k=3.8
    blend=np.clip(.5+.5*(z-hills)/k,0,1)
    z=hills*(1-blend)+z*blend+k*blend*(1-blend)
    broad=(1.1*np.sin(x*.082+y*.043)+.7*np.sin(x*.17-y*.067))
    fluting=.50*np.sin(x*.25+y*.085+z*.11)
    z+=(broad+fluting)*np.clip(z/22,0,1)
    # Terrace shoulders occur only in the lower apron, with sloped transitions.
    apron=np.clip((31-z)/16,0,1)*np.clip((y-25)/35,0,1)
    z+=apron*1.1*np.sin(z*.50)
    front=np.clip((y-18)/28,0,1);front=front*front*(3-2*front)
    return np.maximum(0,z*front)


def build_vista():
    """Separate integration export: world anchor is explicit; no display plinth."""
    from PIL import Image
    import importlib.util
    source=Path(__file__).resolve().with_name('document_farm_surface.py')
    spec=importlib.util.spec_from_file_location('approved_farm_surface',source)
    ground=importlib.util.module_from_spec(spec);spec.loader.exec_module(ground)
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    nx,ny=271,151
    xx,yy=np.meshgrid(np.linspace(-270,270,nx),np.linspace(0,285,ny))
    # Exact shared terrain contact along the leading edge; coordinates stay local.
    base=ground.sample_ground_z(xx+60,yy+110)
    zz=base+vista_height(xx,yy)
    edge=np.minimum((270-np.abs(xx))/22,(285-yy)/20)
    edge=np.clip(edge,0,1);edge=edge*edge*(3-2*edge)
    # Outer closure is buried, not a visible rectangular green tile or pedestal.
    bury=np.clip((yy-16)/18,0,1)
    zz=zz*edge+(-14*bury+base*(1-bury))*(1-edge)
    vertices=np.stack([xx,yy,zz],axis=-1).reshape(-1,3).tolist()
    faces=[]
    for j in range(ny-1):
        for i in range(nx-1):
            a=j*nx+i;b=a+1;c=a+nx;d=c+1
            faces.extend([(a,b,c),(b,d,c)] if (i+j)%2==0 else [(a,b,d),(a,d,c)])
    ob=mesh_object('Vista connected geology — buried perimeter, exact meadow seam',vertices,faces)
    uv=ob.data.uv_layers.new(name='Landscape UV')
    for loop in ob.data.loops:
        v=ob.data.vertices[loop.vertex_index].co;uv.data[loop.index].uv=((v.x+270)/540,v.y/285)
    for polygon in ob.data.polygons:polygon.use_smooth=True
    w,h=1800,1000
    tx,ty=np.meshgrid(np.linspace(-270,270,w),np.linspace(0,285,h));tz=vista_height(tx,ty)
    gy,gx=np.gradient(tz,285/(h-1),540/(w-1));slope=np.hypot(gx,gy)
    # Rock colors respond at cliff-face scale; fine noise is deliberately subdued.
    warm=np.clip(.44*gx-.28*gy+.25,0,1)
    rock=np.zeros((h,w,3));rock[:]=(.31,.39,.49)
    rock=rock*(1-warm[...,None]) + np.array([.66,.49,.35])*warm[...,None]
    strata=.018*np.sin(tz*.48+tx*.043)+.011*np.sin(tx*.30+ty*.11)
    rock+=strata[...,None]
    grass=np.clip((32-tz)/14,0,1)*np.clip((1.12-slope)/.72,0,1)
    moss=np.zeros_like(rock);moss[:]=(.36,.42,.19)
    moss+=(.045*np.sin(tx*.075+ty*.06)*np.sin(ty*.1))[...,None]
    color=rock*(1-grass[...,None])+moss*grass[...,None]
    # Curvature identifies hollows between buttresses, giving snow a geological cause.
    gyy=np.gradient(gy,285/(h-1),axis=0);gxx=np.gradient(gx,540/(w-1),axis=1)
    hollow=np.clip((gxx+gyy-.028)*15,0,1)
    snow=hollow*np.clip((tz-39)/22,0,1)*np.clip((1.75-slope)/.55,0,1)
    color=color*(1-snow[...,None]) + np.array([.83,.88,.91])*snow[...,None]
    # Distant divides have a restrained blue aerial perspective, not fog planes.
    haze=np.clip((ty-158)/120,0,.30)
    color=color*(1-haze[...,None])+np.array([.48,.59,.70])*haze[...,None]
    rgba=np.ones((h,w,4),dtype=np.float32);rgba[:,:,:3]=np.clip(color,0,1)
    image=bpy.data.images.new('Vista broad geology packed albedo',width=w,height=h)
    image.pixels.foreach_set(rgba.ravel());image.filepath_raw=str(OUT/'vista-albedo.png');image.file_format='PNG';image.save();image.pack()
    material=mat('Geological planes, wooded terraces, gully snow',(.5,.5,.5))
    tex=material.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
    material.node_tree.links.new(tex.outputs['Color'],material.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
    ob.data.materials.append(material)
    palette=[mat('Vista bark',(.18,.105,.055)),mat('Pine deep',(.08,.16,.12)),
             mat('Pine green',(.16,.29,.17)),mat('Pine tips',(.28,.39,.19)),
             mat('Canopy amber',(.64,.31,.045)),mat('Canopy gold',(.86,.49,.065)),
             mat('Canopy yellow',(.94,.66,.15)),mat('Canopy ochre',(.52,.24,.04))]
    trees=Batch('Wooded apron — terrain-following grove masses',palette)
    points=[];rng=random.Random(170419)
    for i in range(4500):
        if len(points)>=330:break
        cx,cy,hill,rx,ry=rng.choice(VISTA_HILLS)
        x=rng.gauss(cx,rx*.52);y=rng.gauss(cy,ry*.55)
        if not (-242<x<242 and 24<y<112):continue
        z=float(vista_height(x,y));dx=float(vista_height(x+1,y)-vista_height(x-1,y))/2
        dy=float(vista_height(x,y+1)-vista_height(x,y-1))/2
        if not (2<z<31 and np.hypot(dx,dy)<.70):continue
        if any((x-a)**2+(y-b)**2<14 for a,b,c in points):continue
        points.append((x,y,z+float(ground.sample_ground_z(x+60,y+110))))
    for i,(x,y,z) in enumerate(points):
        tall=rng.uniform(4.2,7.8);fir=i%3!=0
        branch(trees,(x,y,z-.3),(x,y,z+tall),tall*.035,tall*.008,0)
        if fir:
            for level in range(6):
                t=.25+level*.12;r=tall*.32*(1-t)**.8
                for arm in range(5):
                    a=arm*math.tau/5+level*.65
                    c=(x+math.cos(a)*r*.55,y+math.sin(a)*r*.55,z+tall*t)
                    tuft(trees,c,(r*.76,r*.62,tall*.105),rng.choice([1,2,2,3]),a)
            tuft(trees,(x,y,z+tall*.91),(tall*.10,tall*.10,tall*.20),2)
        else:
            for arm in range(7):
                a=arm*2.399;r=tall*rng.uniform(.18,.37)
                c=(x+math.cos(a)*r,y+math.sin(a)*r,z+tall*rng.uniform(.46,.88))
                branch(trees,(x,y,z+tall*.24),c,tall*.022,tall*.006,0)
                for lobe in range(3):
                    cc=(c[0]+rng.uniform(-.15,.15)*tall,c[1]+rng.uniform(-.15,.15)*tall,c[2]+rng.uniform(-.05,.17)*tall)
                    tuft(trees,cc,(tall*.20,tall*.20,tall*.26),rng.choice([4,5,5,6,7]),a)
    forest=trees.finish();objects=[ob,forest]
    # Large embedded ledges on low shoulders have an elongated geological axis.
    stone=Batch('Apron embedded ledges', [mat('Shelf slate',(.38,.42,.42)),mat('Shelf sunface',(.49,.43,.34))])
    for cx,cy,hill,rx,ry in VISTA_HILLS:
        for k in range(6):
            x=cx+rng.uniform(-rx*.65,rx*.65);y=cy+rng.uniform(-ry*.55,ry*.55)
            z=float(vista_height(x,y)+ground.sample_ground_z(x+60,y+110))
            tuft(stone,(x,y,z-.7),(rng.uniform(2.8,5.3),rng.uniform(1.6,3),rng.uniform(1.8,3.5)),k%2,rng.uniform(-.3,.3))
    objects.append(stone.finish())
    # Validate the intentional open landscape surface separately from closed trees.
    ob.data.calc_loop_triangles();v=np.array(vertices);f=np.array([tuple(t.vertices) for t in ob.data.loop_triangles])
    cross=np.cross(v[f[:,1]]-v[f[:,0]],v[f[:,2]]-v[f[:,0]])
    assert np.isfinite(v).all() and np.all(cross[:,2]>0)
    seam_error=float(np.abs(zz[0]-ground.sample_ground_z(xx[0]+60,np.full(nx,110))).max())
    assert seam_error<1e-7
    report={'status':'PASS','kind':'vista revision 2; not a gameplay map','vertices':len(v),'terrain_triangles':len(f),
            'tree_count':len(points),'seam_max_error_metres':seam_error,'minimum_triangle_area':float(np.linalg.norm(cross,axis=1).min()/2),
            'bounds_local_min':v.min(axis=0).tolist(),'bounds_local_max':v.max(axis=0).tolist(),
            'terrain_surface':'intentionally open terrain surface with buried side/rear edges; no display slab',
            'anchor_blender_xyz':[60,110,0],'anchor_unity_xyz':[60,0,110],
            'scale':[1,1,1],'shared_seam':'local y=0 matches document_farm_surface.sample_ground_z(world x,110)',
            'integration':'Overlap existing terrain at north fringe; never position or scale using old study bounds. No buildings/NPCs are placed.'}
    (OUT/'vista-integration.json').write_text(json.dumps(report,indent=2),encoding='utf8')
    bpy.ops.object.select_all(action='DESELECT')
    for item in objects:item.select_set(True)
    bpy.context.view_layer.objects.active=ob
    bpy.ops.export_scene.gltf(filepath=str(OUT/'mountain-vista-integration.glb'),export_format='GLB',use_selection=True)
    # Inspection-only meadow continuation. The export above contains no display base.
    cx,cy=np.meshgrid(np.linspace(-330,330,111),np.linspace(-250,0,61))
    cz=ground.base_height(cx+60,cy+110);cv=np.stack([cx,cy,cz],axis=-1).reshape(-1,3)
    cf=[]
    for j in range(60):
        for i in range(110):
            a=j*111+i;cf.extend([(a,a+1,a+112),(a,a+112,a+111)])
    context=mesh_object('INSPECTION ONLY — continuous meadow at shared north seam',cv.tolist(),cf,mat('Context meadow',(.36,.42,.19)))
    for p in context.data.polygons:p.use_smooth=True
    scene,cam,floor=setup_scene();scene.cycles.samples=24
    # All inspection cameras are in the same local coordinate system as the export.
    floor.hide_render=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.66,.77,.9,1)
    scene.render.resolution_x=1920;scene.render.resolution_y=1080
    for obj in bpy.data.objects:
        if obj.type=='LIGHT' and obj.data.type=='SUN':
            obj.rotation_euler=(math.radians(26),math.radians(-22),math.radians(-30))
    cam.location=(20,-310,110);cam.rotation_euler=(Vector((0,140,34))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=550
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'mountain-vista-integration.blend'))
    render(scene,cam,'vista-01-wide-overview.png',(20,-310,110),(0,140,34),550)
    # True perspective view: foreground begins at shared farm seam, not a floating tile.
    cam.data.type='PERSP';cam.data.lens=32
    render(scene,cam,'vista-02-far-perspective.png',(0,-150,15),(0,174,51),550)
    cam.data.type='ORTHO'
    render(scene,cam,'vista-03-wooded-apron.png',(-90,-52,84),(-100,74,20),190)
    render(scene,cam,'vista-04-rear-geology.png',(30,450,190),(0,170,39),570)
    scene.view_layers[0].material_override=mat('Vista neutral clay',(.52,.53,.55))
    render(scene,cam,'vista-05-clay.png',(20,-310,110),(0,140,34),550)
    scene.view_layers[0].material_override=None
    print('VISTA_COMPLETE',json.dumps(report),flush=True)


if __name__=='__main__':
    if '--vista' in sys.argv:build_vista()
    else:main()
