"""Compose real high-detail world geometry for reference comparison, never live Assets.

Keeps the user-approved observatory and existing building coordinates. New output
is an art candidate; the original small prototype is not an accepted final map.
"""
from pathlib import Path
import hashlib
import json
import math
import sys
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'.collab/artifacts/mountain-reference-study-codex/runtime'))
sys.path.insert(0,str(ROOT/'Tools'))
import bpy
from mathutils import Vector

OUT=ROOT/'.collab/artifacts/reference-world-master-sol-high'
SOURCE=ROOT/'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/Farm_Main_3D_Zone05_Candidate.blend'

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def mesh_object(name,vertices,uvs,faces,material,collection):
    vertices=np.asarray(vertices,dtype=np.float32);uvs=np.asarray(uvs,dtype=np.float32);faces=np.asarray(faces,dtype=np.int32)
    assert np.isfinite(vertices).all() and np.isfinite(uvs).all()
    assert faces.min()>=0 and faces.max()<len(vertices)
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices.tolist(),[],faces.tolist());mesh.update()
    layer=mesh.uv_layers.new(name='UVMap')
    layer.data.foreach_set('uv',uvs[faces.ravel()].ravel())
    mesh.materials.append(material)
    ob=bpy.data.objects.new(name,mesh);collection.objects.link(ob)
    return ob

def zone(prefix):
    matches=[ob for ob in bpy.context.scene.objects if ob.type=='MESH' and ob.name.startswith(prefix)]
    assert len(matches)==1,(prefix,[ob.name for ob in matches])
    return matches[0]

def filtered_arrays(geometry,excluded,predicate=None):
    v,uv,f=geometry.arrays();keep=np.ones(len(f),dtype=bool)
    primitive_nature={'flower_gold','flower_magenta','flower_violet','shrub_green',
                      'grass_gold','lotus_gold','pine_green','pine_dark',
                      'oak_gold','oak_amber','oak_orange'}
    for part in geometry.parts:
        if part.get('material') in primitive_nature or any(part['name'].startswith(prefix) for prefix in excluded) or (predicate and predicate(part['name'])):
            start=part['first_triangle'];keep[start:start+part['triangles']]=False
    f=f[keep];used=np.unique(f);remap=np.full(len(v),-1,dtype=np.int32);remap[used]=np.arange(len(used))
    return v[used],uv[used],remap[f]

def point_camera(camera,position,target):
    camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()

def new_sun(name,rotation,color,energy):
    data=bpy.data.lights.new(name,'SUN');data.energy=energy;data.angle=.12;data.color=color
    ob=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(ob);ob.rotation_euler=rotation;return ob

def configure_cuda(scene):
    preferences=bpy.context.preferences.addons['cycles'].preferences
    preferences.compute_device_type='CUDA';preferences.get_devices()
    devices=[device for device in preferences.devices if device.type=='CUDA']
    if not devices:raise RuntimeError('Requested CUDA renderer is unavailable.')
    for device in preferences.devices:device.use=device.type=='CUDA'
    scene.cycles.device='GPU'
    print('CUDA_RENDER_DEVICES',[(d.name,d.type,d.use) for d in preferences.devices],flush=True)

def natural_rim(collection):
    from citadel_diorama_ruins_geometry import _eval_island_rim_xy
    from map_surface_contract import sample_ground_z
    n,rows=560,19;vertices=[];faces=[]
    for row in range(rows):
        t=row/(rows-1)
        for i in range(n):
            a=math.tau*i/n;x,y=_eval_island_rim_xy(a,.967)
            # The cutaway edge must not climb the distant citadel plateau and
            # create a tall rectangular rear parapet in the camera view.
            top=min(float(sample_ground_z(x,y))-.07,3.5)
            bottom=-.48+.06*math.sin(a*7)
            rough=.016*math.sin(a*37+t*3)+.010*math.sin(a*91-t*7)
            # A continuous bank with unequal diagonal joints and softened ledges.
            amount=1-.013*(1-t)+rough*math.sin(math.pi*t)
            z=bottom+(top-bottom)*t+.045*math.sin(a*23+t*12)*math.sin(math.pi*t)
            vertices.append((x*amount,y*amount,z))
    for row in range(rows-1):
        for i in range(n):
            a=row*n+i;b=row*n+(i+1)%n;c=b+n;d=a+n
            faces.extend(((a,b,c),(a,c,d)))
    # Bridge the cutaway rim into the terrain, avoiding a white background seam.
    cap_start=len(vertices)
    for scale in (.87,.992):
        for i in range(n):
            x,y=_eval_island_rim_xy(math.tau*i/n,scale)
            vertices.append((x,y,min(float(sample_ground_z(x,y))+.018,3.52)))
    for i in range(n):
        a=cap_start+i;b=cap_start+(i+1)%n;c=b+n;d=a+n
        faces.extend(((a,b,c),(a,c,d)))
    mesh=bpy.data.meshes.new('Natural_Weathered_Rock_Bank');mesh.from_pydata(vertices,[],faces);mesh.update()
    for poly in mesh.polygons:poly.use_smooth=True
    ob=bpy.data.objects.new('Zone02_NaturalContinuousCutaway',mesh);collection.objects.link(ob)
    mat=bpy.data.materials.new('Natural_bank_stratified_stone');mat.use_nodes=True
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;bsdf=nodes.get('Principled BSDF')
    position=nodes.new('ShaderNodeNewGeometry');noise=nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value=2.4;noise.inputs['Detail'].default_value=4;noise.inputs['Roughness'].default_value=.7
    links.new(position.outputs['Position'],noise.inputs['Vector'])
    ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.2
    ramp.color_ramp.elements[0].color=(.11,.105,.075,1)
    ramp.color_ramp.elements[1].position=.8;ramp.color_ramp.elements[1].color=(.42,.32,.18,1)
    middle=ramp.color_ramp.elements.new(.49);middle.color=(.26,.235,.17,1)
    links.new(noise.outputs['Fac'],ramp.inputs['Fac']);links.new(ramp.outputs['Color'],bsdf.inputs['Base Color'])
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.35;bump.inputs['Distance'].default_value=.065
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])
    bsdf.inputs['Roughness'].default_value=.84;mesh.materials.append(mat)
    cap=bpy.data.materials.new('Natural_Rim_Top_Living_Ground');cap.use_nodes=True
    cap_shader=cap.node_tree.nodes.get('Principled BSDF')
    cap_shader.inputs['Base Color'].default_value=(.38,.26,.055,1)
    cap_shader.inputs['Roughness'].default_value=.93
    mesh.materials.append(cap)
    for polygon in list(mesh.polygons)[2*n*(rows-1):]:polygon.material_index=1
    return ob

def lighting(scene,mode):
    for ob in list(scene.objects):
        if ob.type=='LIGHT':bpy.data.objects.remove(ob,do_unlink=True)
    scene.world=bpy.data.worlds.new('Reference_'+mode);scene.world.use_nodes=True
    nodes=scene.world.node_tree.nodes;bg=nodes.get('Background')
    if mode=='day':
        bg.inputs[0].default_value=(.74,.83,1,1);bg.inputs[1].default_value=.6
        new_sun('Daylight',(.54,-.35,-.7),(1,.88,.69),3.8)
        new_sun('Cool_sky_fill',(.9,.4,2.2),(.60,.73,1),.6)
        visible=nodes.new('ShaderNodeBackground');visible.inputs[0].default_value=(1,1,1,1)
        visible.inputs[1].default_value=3
        ray=nodes.new('ShaderNodeLightPath');mix=nodes.new('ShaderNodeMixShader')
        links=scene.world.node_tree.links
        links.new(ray.outputs['Is Camera Ray'],mix.inputs[0]);links.new(bg.outputs[0],mix.inputs[1])
        links.new(visible.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],nodes.get('World Output').inputs['Surface'])
    else:
        sky=nodes.new('ShaderNodeTexSky');sky.sky_type='MULTIPLE_SCATTERING';sky.sun_elevation=.095
        sky.sun_rotation=2.2;sky.altitude=.15;sky.air_density=1.1
        scene.world.node_tree.links.new(sky.outputs['Color'],bg.inputs[0]);bg.inputs[1].default_value=.17
        # An angled diorama camera sees below the physical sky horizon. Give its
        # camera rays a sunset atmosphere instead of Nishita's black ground.
        links=scene.world.node_tree.links
        coord=nodes.new('ShaderNodeTexCoord');split=nodes.new('ShaderNodeSeparateXYZ')
        links.new(coord.outputs['Normal'],split.inputs[0])
        height=nodes.new('ShaderNodeMapRange');height.inputs['From Min'].default_value=-.9
        height.inputs['From Max'].default_value=-.2;links.new(split.outputs['Z'],height.inputs['Value'])
        ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.65,.24,.09,1)
        ramp.color_ramp.elements[1].color=(.16,.13,.29,1)
        middle=ramp.color_ramp.elements.new(.55);middle.color=(.48,.22,.25,1)
        links.new(height.outputs['Result'],ramp.inputs['Fac'])
        visible=nodes.new('ShaderNodeBackground');links.new(ramp.outputs['Color'],visible.inputs[0])
        visible.inputs[1].default_value=.9
        ray=nodes.new('ShaderNodeLightPath');mix=nodes.new('ShaderNodeMixShader')
        links.new(ray.outputs['Is Camera Ray'],mix.inputs[0]);links.new(bg.outputs[0],mix.inputs[1])
        links.new(visible.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],nodes.get('World Output').inputs['Surface'])
        new_sun('Late_golden_key',(.28,-.2,-.72),(1,.72,.42),2.1)
        new_sun('Twilight_fill',(.85,.4,2.3),(.46,.60,1),.65)
    scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
    scene.view_settings.exposure=-.2;scene.view_settings.gamma=1

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    original=digest(SOURCE)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE),load_ui=False,use_scripts=False)
    bpy.context.preferences.filepaths.use_scripts_auto_execute=False
    scene=bpy.context.scene
    for ob in scene.objects:
        if ob.type=='MESH' and not ob.name.startswith('Zone'):ob.hide_render=True
    for col in bpy.data.collections:
        if 'Mode2' in col.name:col.hide_render=True
    art=bpy.data.collections.new('ReferenceWorld_ArtPass');scene.collection.children.link(art)
    from map_materials import create_atlas_material
    textures=ROOT/'Assets/Textures/FarmMain3D'
    material=create_atlas_material('Craft_atlas_reference_pass',textures/'FarmMain_3D_Albedo_4K.png',
                                   textures/'FarmMain_3D_Emission_4K.png',roughness=.74,emission_strength=.08)
    # Small physical surface relief supplements real masonry/tile geometry.
    nodes=material.node_tree.nodes;links=material.node_tree.links;bsdf=nodes.get('Principled BSDF')
    noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=17;noise.inputs['Detail'].default_value=3
    bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.025
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])
    from village_buildings_geometry import build_zone04_farmstead,build_zone06_npc_village
    from harbor_beacon_geometry import build_zone07_beacon,build_zone08_harbor_and_bridge
    from citadel_diorama_ruins_geometry import build_zone09_castle_and_cliff,build_zone02_diorama_rim,build_zone11_ruins_and_flora
    from world_groundcover_art_pass import should_exclude_primitive_tree,should_exclude_primitive_groundcover
    builders={'Zone04':build_zone04_farmstead,'Zone06':build_zone06_npc_village,
              'Zone07':build_zone07_beacon,'Zone08':build_zone08_harbor_and_bridge,
              'Zone09':build_zone09_castle_and_cliff}
    records={}
    for prefix,builder in builders.items():
        old=zone(prefix);old.hide_render=True
        geom=builder()
        excluded=()
        if prefix=='Zone09':
            excluded=('cliff_buttress_strata','cliff_strata_ledge','waterfall_channel_strata',
                      'east_bluff_strata','cliff_rim_capstone')
        if prefix=='Zone11':
            excluded=('meadow_grass_batch_',)
        arrays=filtered_arrays(geom,excluded,lambda name:should_exclude_primitive_tree(name) or should_exclude_primitive_groundcover(name))
        if prefix=='Zone09':
            arrays[0][:,2]=7.4+(arrays[0][:,2]-7.4)*.72
            arrays[0][:,0]=14.4+(arrays[0][:,0]-14.4)*.85
            arrays[0][:,1]=15.6+(arrays[0][:,1]-15.6)*.85
        ob=mesh_object(prefix+'_ReferenceDetail',*arrays,material,art)
        records[prefix]={'vertices':len(ob.data.vertices),'triangles':len(arrays[2]),'source':builder.__module__+'.'+builder.__name__}
        print('SWAPPED',prefix,records[prefix],flush=True)
    zone('Zone02').hide_render=True
    rim=natural_rim(art);records['natural_rim']={'vertices':len(rim.data.vertices)}
    zone('Zone11').hide_render=True
    from world_ancient_ruins_art_pass import build_world_ancient_ruins
    ruins=build_world_ancient_ruins(art)
    records['ancient_fragments']=ruins['report']
    from world_meadow_hero_flora import build_world_meadow_hero_flora
    hero_flora=build_world_meadow_hero_flora(art)
    records['hero_flora']=hero_flora['report']
    for ob in hero_flora['stone_objects']:ob['surface_role']='stone'
    # The approved observatory data, material and transform are untouched.
    obs=zone('Zone05');obs_before=len(obs.data.vertices)
    for prefix in ('Zone01',):
        for mat in zone(prefix).data.materials:
            if mat and mat.use_nodes:
                for node in mat.node_tree.nodes:
                    if node.type=='BSDF_PRINCIPLED':node.inputs['Emission Strength'].default_value=0
    zone('Zone10').hide_render=True
    from world_mountain_frame_refinement import build_world_mountain_frame
    mountain_objects,mountain_report=build_world_mountain_frame(art)
    for ob in mountain_objects:
        if 'Low_Northern_Transition' in ob.name:ob.hide_render=True
        else:
            for polygon in ob.data.polygons:polygon.use_smooth=True
    records['mountains']=mountain_report
    from world_water_cliffs_art_pass import build_world_water_cliffs,should_exclude_legacy_pond_detail
    for ob in list(scene.objects):
        if should_exclude_legacy_pond_detail(ob):ob.hide_render=True
    zone('Zone03').hide_render=True
    water=build_world_water_cliffs(art);records['water_cliffs']=water['metadata']
    from world_citadel_plateau_dressing import build_world_citadel_plateau_dressing
    plateau=build_world_citadel_plateau_dressing(art)
    records['plateau_dressing']=plateau['report']
    from world_groundcover_art_pass import build_world_groundcover,apply_meadow_ground_shading,build_world_tree_clusters
    ground=build_world_groundcover(art,density=2.0)
    apply_meadow_ground_shading(zone('Zone01'))
    records['groundcover']=ground.get('report',{}) if isinstance(ground,dict) else {'hook':'completed'}
    trees=build_world_tree_clusters(art,count=100,extra_keepouts=((14.4,15.6,5.0),))
    records['trees']=trees.get('report',{}) if isinstance(trees,dict) else {'hook':'completed'}
    from world_evergreen_groves import build_world_evergreen_groves
    evergreen=build_world_evergreen_groves(art)
    records['evergreen_groves']=evergreen['report']
    from apply_reference_surface_materials import apply_materials
    records['surface_textures']=apply_materials(art,zone('Zone01'))
    rim.data.materials[1]=zone('Zone01').data.materials[0]
    from world_rim_botanical_transition import build_world_rim_botanical_transition
    records['rim_transition']=build_world_rim_botanical_transition(art)['report']
    from world_meadow_relief import apply_meadow_relief
    records['meadow_relief']=apply_meadow_relief(scene)
    for image in bpy.data.images:
        if image.source=='FILE' and image.has_data:
            try:image.pack()
            except RuntimeError:pass
    data=bpy.data.cameras.new('ReferenceMasterCamera');camera=bpy.data.objects.new('ReferenceMasterCamera',data);art.objects.link(camera)
    scene.camera=camera;camera.data.type='PERSP';camera.data.lens=46;camera.data.clip_end=800
    point_camera(camera,(-2,-65,43),(0,1.5,4))
    scene.render.engine='CYCLES';scene.cycles.samples=12 if '--preview' in sys.argv else 32;scene.cycles.use_denoising=True
    scene.cycles.transparent_max_bounces=64
    if '--gpu' in sys.argv:
        configure_cuda(scene)
    scene.render.threads_mode='FIXED';scene.render.threads=4
    scene.render.resolution_x=1200 if '--preview' in sys.argv else 2000
    scene.render.resolution_y=900 if '--preview' in sys.argv else 1500;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
    bpy.context.preferences.filepaths.save_version=0
    assert len(obs.data.vertices)==obs_before
    for mode in (('day',) if '--preview' in sys.argv else ('day','sunset')):
        lighting(scene,mode)
        suffix='-preview' if '--preview' in sys.argv else ''
        scene.render.filepath=str(OUT/f'world-{mode}{suffix}.png')
        bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'))
        print('RENDER',mode,flush=True);bpy.ops.render.render(write_still=True)
    assert digest(SOURCE)==original
    report={'status':'rendered_art_candidate_not_accepted','source_sha256':original,
            'source_unchanged':True,'approved_observatory_vertices':obs_before,'components':records,
            'blender':bpy.app.version_string,'no_live_assets_changed':True}
    (OUT/'composition-report.json').write_text(json.dumps(report,indent=2,default=str),encoding='utf-8')
    print('WORLD_ART_MASTER_RENDERED',flush=True)

def render_saved_sunset():
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'),load_ui=False,use_scripts=False)
    scene=bpy.context.scene
    lighting(scene,'sunset')
    scene.render.filepath=str(OUT/'world-sunset.png')
    print('RENDER_SAVED_SUNSET',flush=True)
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'))
    print('SAVED_SUNSET_RENDERED',flush=True)

def refine_saved_boundary():
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'),load_ui=False,use_scripts=False)
    scene=bpy.context.scene;configure_cuda(scene)
    art=bpy.data.collections['ReferenceWorld_ArtPass']
    terrain=zone('Zone01');rim=bpy.data.objects['Zone02_NaturalContinuousCutaway']
    rim.data.materials[1]=terrain.data.materials[0]
    from world_rim_botanical_transition import build_world_rim_botanical_transition
    transition=build_world_rim_botanical_transition(art)
    from world_meadow_relief import apply_meadow_relief
    relief=apply_meadow_relief(scene)
    report=json.loads((OUT/'composition-report.json').read_text(encoding='utf-8'))
    report['components']['rim_transition']=transition['report']
    report['components']['meadow_relief']=relief
    bpy.context.preferences.filepaths.save_version=0
    for mode in ('day','sunset'):
        lighting(scene,mode);scene.render.filepath=str(OUT/f'world-{mode}.png')
        print('REFINED_RENDER',mode,flush=True);bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'))
    (OUT/'composition-report.json').write_text(json.dumps(report,indent=2,default=str),encoding='utf-8')
    print('REFINED_BOUNDARY_SAVED',flush=True)

def refresh_saved_components():
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'),load_ui=False,use_scripts=False)
    scene=bpy.context.scene;configure_cuda(scene)
    art=bpy.data.collections['ReferenceWorld_ArtPass']
    for ob in list(scene.objects):
        if ob.name.startswith(('WCF_','WorldMountain_')):bpy.data.objects.remove(ob,do_unlink=True)
    from world_water_cliffs_art_pass import build_world_water_cliffs
    water=build_world_water_cliffs(art)
    from world_mountain_frame_refinement import build_world_mountain_frame
    mountains,mountain_report=build_world_mountain_frame(art)
    for ob in mountains:
        if 'Low_Northern_Transition' in ob.name:ob.hide_render=True
        else:
            for polygon in ob.data.polygons:polygon.use_smooth=True
    from apply_reference_surface_materials import apply_materials
    apply_materials(art,zone('Zone01'))
    # Pines imported under their native names need the same base displacement as
    # the meadow added in the previous pass. Their linked meshes stay untouched.
    from world_meadow_relief import apply_meadow_relief
    apply_meadow_relief(scene)
    removed=[]
    for ob in list(scene.objects):
        if ob.type=='MESH' and ob.hide_render:
            removed.append(ob.name);bpy.data.objects.remove(ob,do_unlink=True)
    for mesh in list(bpy.data.meshes):
        if mesh.users==0:bpy.data.meshes.remove(mesh)
    report=json.loads((OUT/'composition-report.json').read_text(encoding='utf-8'))
    report['components']['water_cliffs']=water['metadata']
    report['components']['mountains']=mountain_report
    report['hidden_superseded_meshes_removed_from_candidate']=removed
    report['generator_sha256']={p.name:digest(p) for p in [Path(__file__),*map(Path,[
        ROOT/'Tools/world_groundcover_art_pass.py',ROOT/'Tools/world_water_cliffs_art_pass.py',
        ROOT/'Tools/world_mountain_frame_refinement.py',ROOT/'Tools/world_meadow_hero_flora.py',
        ROOT/'Tools/world_meadow_relief.py',ROOT/'Tools/apply_reference_surface_materials.py'])]}
    bpy.context.preferences.filepaths.save_version=0
    scene.cycles.transparent_max_bounces=64
    for mode in ('day','sunset'):
        lighting(scene,mode);scene.render.filepath=str(OUT/f'world-{mode}.png')
        print('REFRESHED_RENDER',mode,flush=True);bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'))
    (OUT/'composition-report.json').write_text(json.dumps(report,indent=2,default=str),encoding='utf-8')
    print('REFRESHED_COMPONENTS_SAVED',flush=True)

def render_saved_review():
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'),load_ui=False,use_scripts=False)
    scene=bpy.context.scene
    if '--gpu' in sys.argv:configure_cuda(scene)
    # Earlier integration visited unused palette slots on the rock batch. Those
    # slots are shared with plants; restore their original botanical shader.
    from world_groundcover_art_pass import PALETTE
    botanical=[*PALETTE,(.83,.79,.60,1),(.69,.59,.72,1),(.28,.38,.12,1)]
    for index,colour in enumerate(botanical):
        material=bpy.data.materials.get('World_HeroFlora_'+str(index))
        if material is None:continue
        shader=material.node_tree.nodes.get('Principled BSDF')
        for name in ('Base Color','Normal'):
            for link in list(shader.inputs[name].links):material.node_tree.links.remove(link)
        shader.inputs['Base Color'].default_value=colour
        shader.inputs['Roughness'].default_value=.87
        material['generated_rock_albedo_applied']=False
    for ob in scene.objects:
        if ob.type=='MESH' and ob.name.startswith('WorldMountain_'):
            if 'Low_Northern_Transition' in ob.name:ob.hide_render=True
            else:
                for polygon in ob.data.polygons:polygon.use_smooth=True
    bpy.context.preferences.filepaths.save_version=0
    for mode in ('day','sunset'):
        lighting(scene,mode);scene.render.filepath=str(OUT/f'world-{mode}.png')
        print('REVIEW_RENDER',mode,flush=True);bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'))
    print('REVIEW_RENDERED',flush=True)

def render_saved_closes():
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'reference-world-art-candidate.blend'),load_ui=False,use_scripts=False)
    scene=bpy.context.scene;configure_cuda(scene);lighting(scene,'day')
    scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.cycles.samples=24
    data=bpy.data.cameras.new('MaterialDetailCamera');camera=bpy.data.objects.new('MaterialDetailCamera',data)
    scene.collection.objects.link(camera);scene.camera=camera;camera.data.lens=48
    for name,position,target in [('cliff-detail',(11,-2,10),(14,11.5,4.9)),
                                 ('meadow-detail',(-8,-17,8),(-3.4,-5.2,2.3))]:
        point_camera(camera,position,target);scene.render.filepath=str(OUT/(name+'.png'))
        print('CLOSE_RENDER',name,flush=True);bpy.ops.render.render(write_still=True)
    print('DETAIL_RENDERS_COMPLETE',flush=True)

if __name__=='__main__':
    if '--refresh-components' in sys.argv:refresh_saved_components()
    elif '--refine-boundary' in sys.argv:refine_saved_boundary()
    elif '--render-closes' in sys.argv:render_saved_closes()
    elif '--render-review' in sys.argv:render_saved_review()
    elif '--render-sunset' in sys.argv:render_saved_sunset()
    else:main()
