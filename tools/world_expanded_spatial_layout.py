"""Candidate-only 120x90m art layout; buildings translate, never scale.

GDD6.0 section3.6 specifies approximately120x90m initially, with at least35%
pasture. Coordinates/pond art shapes below are review candidates, not balance.
Pure NumPy contract: no Blender imports, source scene loads or asset writes.
"""
from pathlib import Path
import json
import math
import numpy as np

WORLD_WIDTH=WIDTH=120.0
WORLD_HEIGHT=HEIGHT=90.0
SOURCE_WIDTH=62.0
SOURCE_HEIGHT=47.0
SCALE_X=WORLD_WIDTH/SOURCE_WIDTH
SCALE_Y=WORLD_HEIGHT/SOURCE_HEIGHT
BOUNDS=(-60.0,60.0,-45.0,45.0)
WORLD_BOUNDS=BOUNDS
MIN_PASTURE_FRACTION=.35


def map_source_xy(x,y):
    return np.asarray(x)/SCALE_X,np.asarray(y)/SCALE_Y


def map_world_xy(source_x,source_y):
    return np.asarray(source_x)*SCALE_X,np.asarray(source_y)*SCALE_Y


def base_ground_z(x,y):
    """Remap PRE-water source landscape; old large pond/river cuts are filled."""
    world_x,world_y=np.broadcast_arrays(np.asarray(x),np.asarray(y))
    x,y=map_source_xy(x,y)
    x,y=np.broadcast_arrays(x,y)
    z=(2.32+1.45*np.clip((y+16)/34,0,1)
       +1.15*np.clip((-x+10)/38,0,1)*np.clip((y+5)/24,0,1)
       +.22*np.sin(x*.21+y*.17)*np.cos(x*.14-y*.22))
    # New expanded terrain must not stretch the old straight rectangular castle
    # shelf into a wall. A curved breakline and unequal diagonal spurs form the
    # rear-right geological rise; the existing castle pad stays at full relief.
    break_y=28+2*np.sin(world_x*.15)+1.2*np.cos(world_x*.37)+.08*(world_x-29)
    rise=np.clip((world_y-break_y)/8,0,1)
    rise=rise*rise*(3-2*rise)
    west=np.clip((world_x-8)/14,0,1);east=np.clip((58-world_x)/8,0,1)
    castle=rise*west*east
    alpine=np.clip((y-11.2)/6.5,0,1)*np.clip((7.5-x)/12,0,1)
    z=z+5.40*castle**1.25+3.50*alpine**1.35
    for cx,cy,rx,ry,height in ((20,25,6,9,.85),(39,29,5,10,.70)):
        spur=height*np.exp(-(((world_x-cx)/rx)**2+((world_y-cy)/ry)**2))
        z=z+spur*(1-rise)
    return float(z) if z.ndim==0 else z


# Latest user override: ONE compact organic pond; historical three-pond layouts
# are superseded. Geometry/material reference is the golden flower meadow.
PONDS=({'id':'central_pasture_pond','center':[-6.,-13.],'radii':[4.2,2.5],
        'angle_deg':-24.,'level':base_ground_z(-6.,-13.)-.18,
        'depth':.60,'bank_width':1.5,'seed':260719},)

# Source anchors are measured cluster centres, except the approved observatory's
# established design pivot. The approved object's mesh/materials remain untouched.
BUILDING_ANCHORS={
 'farmstead':{'source_xy':(-12.5014882,7.2001263),'target_xy':(-35.,7.),'half_extents':(7.8015,3.7031),'zone':'Zone04'},
 'observatory':{'source_xy':(-7.8,6.4),'target_xy':(-14.,12.),'half_extents':(1.801,1.654),'zone':'Zone05'},
 'enhancement_sanctum':{'source_xy':(-1.8,7.6),'target_xy':(-2.,11.5),'half_extents':(3.0,2.8),'zone':'Zone06_PART_SANCTUM'},
 'npc_village':{'source_xy':(1.,12.85),'target_xy':(-28.,29.),'half_extents':(4.721,2.65),'zone':'Zone06_PART_NPC'},
 'transition_gate':{'source_xy':(.6,9.15),'target_xy':(-30.,21.5),'half_extents':(3.4,.8),'zone':'Zone06_PART_GATE'},
 'information_beacon':{'source_xy':(8.5655093,4.5700336),'target_xy':(21.,11.),'half_extents':(2.383,2.171),'zone':'Zone07'},
 'harbor_town':{'source_xy':(22.2677383423,1.8206167221),'target_xy':(43.,14.),'half_extents':(5.2392473221,6.7693119049),
                'source_ground_z_override':2.35,'source_water_level':2.04,'zone':'Zone08_PART_TOWN'},
 'castle':{'source_xy':(14.4244123,15.4067106),'target_xy':(29.,39.),'half_extents':(4.618,2.234),'zone':'Zone09'},
 'bridge':{'source_xy':(14.9561538696,6.1999998093),'target_xy':(28.,13.),'half_extents':(5.9546093941,1.5864102840),'zone':'Zone08_PART_ONLY'},
}
ANCHORS=BUILDING_ANCHORS
SOURCE_ZONE06_FULL_CLUSTER_CENTER=(.4193552,10.1745324)
ZONE06_PARTITION={'method':'whole authored geometry components, never cut through meshes at a Y plane',
                 'sanctum_builder':'village_buildings_geometry.build_enhancement_rune_sanctum',
                 'gate_builder':'village_buildings_geometry.build_transition_gatehouse_arch',
                 'npc_parts':'terrace, NPC buildings, signpost/well, stairs; source village terrace pivot(1,12.85)',
                 'full_cluster_legacy_anchor':SOURCE_ZONE06_FULL_CLUSTER_CENTER}

TRAILS=(
 ((-35.,7.),(-27.,2.),(-14.,9.),(8.,10.),(21.,11.),(28.,13.),(43.,14.)),
 ((-35.,7.),(-38.,17.),(-34.,24.),(-28.,29.)),
 ((-28.,29.),(-12.,29.),(4.,28.),(19.,31.),(29.,39.)),
 ((-27.,2.),(-25.,-13.),(-18.,-28.),(0.,-30.),(20.,-19.),(27.,0.),(28.,13.)),
)
MEADOW_ANCHORS=((-32.,-25.),(-12.,-30.),(9.,-25.),(24.,-13.),(-24.,-3.),(-2.,8.))
RUIN_ANCHORS=((-39.,-17.),(-26.,-31.),(-3.,-23.),(16.,-15.),(28.,-4.),(-7.,21.))
LAYOUT={'version':'expanded-art-candidate-v2-one-pond','bounds':list(BOUNDS),'width':WIDTH,'height':HEIGHT,
        'source_scale_xy':[SCALE_X,SCALE_Y],'ponds':list(PONDS),'rivers':[],'cascades':[],
        'building_anchors':BUILDING_ANCHORS,'zone06_partition':ZONE06_PARTITION,'trails':TRAILS,
        'meadow_anchors':MEADOW_ANCHORS,'ruin_anchors':RUIN_ANCHORS,
        'coordinate_system':'Blender X east, Y north, Z up; metres',
        'preserve_building_geometry_and_scale':True,
        'latest_user_art_direction':'Near-reference golden flower carpet; one compact central pond; no dense olive bush carpet',
        'gdd_scope':'120x90 initial envelope and at least35%pasture; no gameplay count/cost changes'}


def as_dict():
    """Shared mutable metadata; water owner may register exact river/cascade data."""
    return LAYOUT


def translation_xy(name):
    record=BUILDING_ANCHORS[name]
    return tuple(record['target_xy'][i]-record['source_xy'][i] for i in range(2))


def building_placement(name):
    record=BUILDING_ANCHORS[name]
    from map_surface_contract import sample_ground_z as source_ground_z
    source_z=float(record.get('source_ground_z_override',source_ground_z(*record['source_xy'])))
    target_z=sample_ground_z(*record['target_xy'])
    return {**record,'translation_xy':translation_xy(name),'asset_scale':(1.,1.,1.),
            'source_ground_z':source_z,'target_ground_z':target_z,
            'foundation_offset_z':target_z-source_z}


def sample_ground_z(x,y):
    """Authoritative final ground uses the water owner's exact shared carve."""
    from world_expanded_water import carve_water_ground
    z=carve_water_ground(x,y,base_ground_z(x,y),LAYOUT)
    z=np.asarray(z)
    return float(z) if z.ndim==0 else z


def pond_distance(x,y,pond):
    from world_expanded_water import pond_signed_distance
    return pond_signed_distance(x,y,pond)


def pond_outline(pond,count=128):
    from world_expanded_water import pond_outline as outline
    return outline(pond,count)


def pond_surface_height(pond):
    return pond['level']


def nearest_trail(x,y):
    x,y=np.broadcast_arrays(np.asarray(x),np.asarray(y));distance=np.full(x.shape,np.inf)
    for route in TRAILS:
        for a,b in zip(route,route[1:]):
            dx,dy=b[0]-a[0],b[1]-a[1]
            t=np.clip(((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy),0,1)
            distance=np.minimum(distance,np.hypot(x-a[0]-t*dx,y-a[1]-t*dy))
    return float(distance) if distance.ndim==0 else distance


def valid_point(x,y):
    x,y=np.broadcast_arrays(np.asarray(x),np.asarray(y))
    # Rounded natural edge in the full new envelope, no tiny old island guard.
    mask=(np.abs(x/60)**4+np.abs(y/45)**4)<=1
    return bool(mask) if mask.ndim==0 else mask


def is_walkable(x,y):
    """Art-placement guard only; no claim of runtime navigation/collider validity."""
    x,y=np.broadcast_arrays(np.asarray(x),np.asarray(y));mask=np.asarray(valid_point(x,y))
    from world_expanded_water import water_surface_height
    mask=mask & ~np.isfinite(np.asarray(water_surface_height(x,y,LAYOUT)))
    mask=mask & (np.asarray(nearest_trail(x,y))>.9)
    for pond in PONDS:mask=mask & (np.asarray(pond_distance(x,y,pond))>1.2)
    for record in BUILDING_ANCHORS.values():
        cx,cy=record['target_xy'];rx,ry=record['half_extents']
        mask=mask & ~((np.abs(x-cx)<rx+2)&(np.abs(y-cy)<ry+2))
    return bool(mask) if mask.ndim==0 else mask


def contract_report():
    pond_area=sum(math.pi*p['radii'][0]*p['radii'][1] for p in PONDS)
    return {'status':'candidate_layout_contract_not_visual_acceptance','layout':LAYOUT,
            'area_comparison':{'source_envelope_m2':SOURCE_WIDTH*SOURCE_HEIGHT,'new_envelope_m2':WIDTH*HEIGHT,
                               'multiplier':WIDTH*HEIGHT/(SOURCE_WIDTH*SOURCE_HEIGHT)},
            'pond_ellipse_area_estimate_m2':pond_area,'pond_fraction_of_envelope':pond_area/(WIDTH*HEIGHT),
            'placements':{name:building_placement(name) for name in BUILDING_ANCHORS},
            'note':'Bridge is a Zone08 part; do not move the whole town twice when splitting it. Exact water masks/carve owned by world_expanded_water.'}


from world_expanded_water import configure_water_layout
configure_water_layout(LAYOUT,base_ground_z)


if __name__=='__main__':
    out=Path(__file__).resolve().parents[1]/'.collab/artifacts/world-expanded-layout-sol-high'
    out.mkdir(parents=True,exist_ok=True)
    report=contract_report()
    (out/'expanded-layout-contract.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps({'bounds':BOUNDS,'area_multiplier':report['area_comparison']['multiplier'],
                      'pond_fraction':report['pond_fraction_of_envelope'],'api':'sample_ground_z/valid_point/is_walkable/BUILDING_ANCHORS/PONDS/LAYOUT'}))
