"""Gentle candidate-only meadow relief, with paired vegetation ground contact.

This is visual geometry for review, not a change to the gameplay height sampler.
Approved buildings, water and source files are excluded.
"""
import numpy as np
from map_surface_contract import eval_pond_distances,eval_road_mask,eval_river_center_and_width

def height_delta(x,y):
    x=np.asarray(x,dtype=np.float32);y=np.asarray(y,dtype=np.float32)
    hill=(.72*np.exp(-((x+15.2)/4.7)**2-((y+9.0)/3.6)**2)
          +.84*np.exp(-((x-3.0)/5.3)**2-((y+12.8)/3.4)**2)
          +.52*np.exp(-((x+10.0)/3.2)**2-((y+.5)/3.6)**2))
    pond=np.minimum.reduce(eval_pond_distances(x,y))
    mask=np.clip((pond-1.35)/.55,0,1)*(1-np.asarray(eval_road_mask(x,y)))**3
    mask*=np.clip((4-y)/3,0,1)
    river,width=eval_river_center_and_width(y)
    river_distance=np.abs(x-river)-width
    mask*=np.where((y>-9)&(y<13),np.clip((river_distance-.8)/1.8,0,1),1)
    # A smooth untouched join at the original outside edge and near buildings.
    mask*=np.clip((y+19.5)/2,0,1)
    for bx,by in [(-17.8,5.4),(-17,9.2),(-13.4,9.4),(-7.8,6.4),(-1.8,7.6),(8.6,4.6)]:
        mask*=np.clip((np.sqrt((x-bx)**2+(y-by)**2)-4.2)/2,0,1)
    return hill*mask

def apply_meadow_relief(scene):
    prefixes=('World_Low_Curved_Grass','World_Fern_And_Broadleaf','World_Native_Low_Flower',
              'World_Irregular_Bank','World_HeroFlora_','World_RimTransition_')
    changed=[];peak=0.
    for ob in scene.objects:
        if ob.type!='MESH' or ob.hide_render or ob.get('meadow_relief_v1'):continue
        if ob.name.startswith(('WorldAncientRuins_','World_NativeTree_','WorldEvergreen_','PineTree_')):
            dz=float(height_delta(ob.location.x,ob.location.y))
            ob.location.z+=dz
        elif ob.name.startswith('Zone01_') or ob.name.startswith(prefixes):
            mesh=ob.data
            positions=np.empty(len(mesh.vertices)*3,dtype=np.float32)
            mesh.vertices.foreach_get('co',positions);positions=positions.reshape((-1,3))
            dz=height_delta(positions[:,0],positions[:,1])
            positions[:,2]+=dz;mesh.vertices.foreach_set('co',positions.ravel());mesh.update()
            peak=max(peak,float(np.max(dz,initial=0)))
        else:continue
        ob['meadow_relief_v1']=True;changed.append(ob.name)
    return {'visual_candidate_only':True,'gameplay_sampler_changed':False,
            'maximum_added_relief_m':peak,'objects':changed,'approved_buildings_unchanged':True}
