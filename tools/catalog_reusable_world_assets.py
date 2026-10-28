"""Read local world assets and write a linked review catalogue; never copies models.

No acceptance is inferred from a filename, render, mesh count or old validation.
"""
from __future__ import annotations

import hashlib
import html
import json
import struct
from pathlib import Path
from urllib.parse import quote

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '.collab/artifacts/reusable-world-assets-sol-high'
ART = ROOT / '.collab/artifacts'
APPROVED = 'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail/ResearchObservatory.glb'
APPROVED_HASH = 'e943a045445f41cb8c778039f13f3a6821823349e9e381bd3ef28de8ee90c763'
ORIGINAL_DIRS = ('zone02-09-11-20261018', 'zone04-06-20261018', 'zone05-20261018', 'zone07-08-20261018', 'zone10-20261018')
CANDIDATE_DIRS = ('functional-buildings-codex', 'functional-buildings-argon', 'argon-facilities-and-scenes-codex')


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def sha(path: Path) -> str:
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def glb_evidence(path: Path) -> dict:
    with path.open('rb') as stream:
        magic, version, length = struct.unpack('<4sII', stream.read(12))
        size, kind = struct.unpack('<I4s', stream.read(8))
        assert magic == b'glTF' and version == 2 and length == path.stat().st_size
        assert kind == b'JSON'
        doc = json.loads(stream.read(size))
    triangles = vertices = 0
    for mesh in doc.get('meshes', []):
        for primitive in mesh.get('primitives', []):
            count = doc['accessors'][primitive['attributes']['POSITION']]['count']
            vertices += count
            count = doc['accessors'][primitive['indices']]['count'] if 'indices' in primitive else count
            mode = primitive.get('mode', 4)
            triangles += count // 3 if mode == 4 else max(0, count - 2) if mode in (5, 6) else 0
    return {'format': 'GLB2 header/accessors parsed', 'meshes': len(doc.get('meshes', [])),
            'triangles': triangles, 'vertices': vertices, 'materials': len(doc.get('materials', [])),
            'material_names': [m.get('name', '') for m in doc.get('materials', [])],
            'textures': len(doc.get('textures', [])), 'images': len(doc.get('images', [])),
            'embedded_images': sum('bufferView' in i for i in doc.get('images', [])),
            'scope': 'binary structure only; no runtime, collision, door access or visual acceptance claim'}


def meshbin_evidence(path: Path) -> dict:
    with path.open('rb') as stream:
        vertices, indices = struct.unpack('<ii', stream.read(8))
    assert vertices > 0 and indices > 0 and indices % 3 == 0
    assert path.stat().st_size == 8 + vertices * 32 + indices * 4
    return {'format': 'Unity meshbin header and exact buffer length checked', 'vertices': vertices,
            'triangles': indices // 3, 'materials': None, 'textures': None,
            'scope': 'geometry+normals+UV+indices; material resides in original Blender/Unity material binding'}


def file_record(path: Path) -> dict:
    result = {'path': rel(path), 'bytes': path.stat().st_size, 'sha256': sha(path)}
    if path.suffix == '.glb':
        result['evidence'] = glb_evidence(path)
    elif path.suffix == '.meshbin':
        result['evidence'] = meshbin_evidence(path)
    else:
        result['evidence'] = {'format': 'existing Blender file; see independently read blend-index.json'}
    return result


def previews(directory: Path) -> list[str]:
    return [rel(p) for p in sorted(directory.glob('*.png')) if p.name[0].isdigit() or 'contact' in p.name]


def matching_previews(path: Path) -> list[str]:
    """Associate actual proof frames rather than displaying another building's image."""
    choices = previews(path.parent)
    stem = path.stem
    rules = {'Zone04_': ('01-', '02-'), 'Zone06_': ('03-', '04-'),
             'StorageGranary': ('05-', '06-'), 'Zone07_': ('01-', '02-'),
             'Zone08_': ('03-', '04-'), 'Zone09_': ('01-', '02-'),
             'Zone02_': ('03-', '04-'), 'Zone03_': ('03-', '04-'), 'Zone11_': ('03-', '04-'),
             'Zone05_': ('02-after', '04-after'), 'Zone10_': ('02-after', '04-after'),
             'AlpineMountains': ('02-after', '04-after'), 'ResearchObservatory': ('02-after', '04-after'),
             'warehouse': ('01-warehouse', '02-warehouse'), 'construction_workshop': ('06-construction',),
             'enhancement_atelier': ('07-enhancement',), 'herd-shelter': ('01-shelter', '02-shelter'),
             'main_house': ('01-mainhouse', '02-mainhouse'), 'research_laboratory': ('01-mainhouse', '02-mainhouse'),
             'medical_tent': ('01-mainhouse', '02-mainhouse'), 'information_complex': ('03-information',),
             'power_and_recovery': ('04-generators',), 'market_town': ('05-market', '06-market'),
             'hub_village': ('07-hub', '08-hub'), 'forest_primeval': ('07-hub', '08-hub')}
    for prefix, patterns in rules.items():
        if stem.startswith(prefix):
            return [p for p in choices if Path(p).name.startswith(patterns)]
    return choices


def model_entry(path: Path, status: str, preview: list[str], note: str = '') -> dict:
    return {'id': rel(path), 'title': path.stem, 'kind': 'actual_local_3d', 'readiness': status,
            'function': function_for(path.stem), 'files': [file_record(path)], 'preview_paths': preview,
            'note': note, 'promotion': 'requires user image approval; no import performed'}


def function_for(name: str) -> str:
    tokens = name.lower()
    for keys, function in [(['construction', 'xaydung'], 'Construction / blueprints / repair / ammunition'),
                           (['research', 'observatory', 'nghiencuu', 'genetics'], 'Genetic research; approved observatory exterior retained'),
                           (['storage', 'warehouse', 'granary', 'khocam'], 'Storage / dry feed; variants are art proposals, not a canonical level table'),
                           (['enhancement', 'cuonghoa'], 'Paper reinforcement / engraving'),
                           (['information', 'infobeacon', 'thongtin'], 'Information / observation / weather'),
                           (['medical', 'quany'], 'Medical service; detailed mechanics unresolved'),
                           (['shelter', 'chuong', 'mainbarn'], 'Herd shelter or farmstead; verify specific source component'),
                           (['main_house', 'main-house'], 'Main house for An and Khoa'),
                           (['hub', 'npc', 'cottage'], 'NPC village reference, separate scene'),
                           (['market', 'harbor', 'bridge'], 'Town / bridge / market; old-European town direction only'),
                           (['mountain', 'ridge', 'alpine'], 'Landscape; original historical geometry is not acceptance of old wall layout'),
                           (['ruin', 'flora', 'castle'], 'Distant vista / flora / ruins; season and lore restrictions apply')]:
        if any(key in tokens for key in keys):
            return function
    return 'Reference or source geometry; exact gameplay role not inferred'


def image_related(name: str, entries: list[dict]) -> list[str]:
    token = name.split('_', 1)[0]
    zones = {'01': ['Zone04_'], '02': ['Zone04_'], '03': ['ResearchObservatory'],
             '04': ['Zone07_'], '05': ['StorageGranary', 'Zone04_'], '06': ['Zone06_'],
             '07': ['Zone08_'], '08': ['Zone09_'], '09A': ['Zone11_'], '09B': ['Zone11_']}.get(token, [])
    return [entry['id'] for entry in entries if any(t in Path(entry['id']).name for t in zones)
            and entry['readiness'] != 'unaccepted_candidate']


def build() -> dict:
    entries = []
    approved = ROOT / APPROVED
    assert sha(approved) == APPROVED_HASH, 'Approved source drift: stop, do not record a new baseline'
    entries.append(model_entry(approved, 'approved_exterior_source_retained',
                              ['.collab/artifacts/zone05-20261018/02-after-front.png',
                               '.collab/artifacts/zone05-20261018/04-after-rear.png'],
                              'Exact approved screenshot identity is recorded in sol-high-architecture/image5-identity.json.'))
    for directory in ORIGINAL_DIRS:
        base = ART / directory
        for p in sorted(base.glob('*.meshbin')):
            note = 'Original upgraded geometry. Readiness is reusable source pending per-building visual review.'
            if 'StorageGranary_Tier' in p.name:
                note += ' Historical Tier1/2/3 filenames do NOT establish three canonical warehouse levels.'
            entries.append(model_entry(p, 'verified_local_geometry_not_new_acceptance', matching_previews(p), note))
        for p in sorted(base.glob('*.glb')):
            entries.append(model_entry(p, 'historical_export_not_new_acceptance', matching_previews(p),
                                      'Separate historical export. Canonical approved GLB has its own protected hash; do not replace it with this file.'))
    for p in sorted((ROOT / 'ArtSource/Blender_3D_Map').rglob('*.blend')):
        entries.append(model_entry(p, 'editable_source_retained', [],
                                  'Master Zone04/06 may predate detailed meshbin upgrades. Do not claim it equals newer proof. No map builder executed.'))
    for directory in CANDIDATE_DIRS:
        base = ART / directory
        for p in sorted(base.glob('*.glb')):
            entries.append(model_entry(p, 'unaccepted_candidate', matching_previews(p),
                                      'Existing study only; repeated ordinary gables or glowing information discs are not approved direction. Reuse selected craft rather than replacing approved observatory.'))
    annex = ART / 'sol-high-architecture/argon-research-annex/annex-only.glb'
    if annex.exists():
        entries.append(model_entry(annex, 'optional_unaccepted_annex', previews(annex.parent),
                                  'Cabinet-scale specimen bay; approved exterior remains intact. Not a mandatory walk-in lab.'))
    for p in sorted((OUT / 'argon-construction-reuse').glob('*.glb')):
        entries.append(model_entry(p, 'new_reuse_candidate_pending_independent_review', previews(p.parent)))
    for p in sorted((OUT / 'codex-construction-extraction').glob('*.glb')):
        entries.append(model_entry(p, 'verified_original_building_extracted', previews(p.parent),
                                  'Exact original Zone04 construction hall geometry retained. Closed doors / solid hall core; no walk-in interior claim. Open working pavilion is separate.'))
    for p in sorted((OUT / 'codex-entry-refinement').glob('*.glb')):
        proof = previews(p.parent)
        proof.sort(key=lambda x: (0 if '01-front' in x else 1 if '06-entry-lit' in x else 2, x))
        entries.append(model_entry(p, 'unaccepted_narrow_entry_refinement', proof,
                                  'Reuse original workshop; 1009 original parts retain exact positions and UVs within glTF precision. Only blocked hall cores/portal stones/door parts changed. Actual centreline rays and images must be checked; no runtime acceptance.'))
    model_entries = list(entries)
    for base in (ROOT / 'Tripo3D_Map_Inputs', ROOT / 'ArtSource/Tripo3D_Map_Inputs'):
        for p in sorted(base.rglob('*')):
            if p.suffix.lower() not in ('.png', '.jpg', '.jpeg'):
                continue
            related = image_related(p.name, model_entries) if 'ArtSource' in p.parts else []
            entries.append({'id': rel(p), 'title': p.stem, 'kind': 'image_reference',
                            'readiness': 'image_with_related_local_3d' if related else 'image_only_no_matching_3d_verified',
                            'function': function_for(p.stem), 'files': [{'path': rel(p), 'bytes': p.stat().st_size, 'sha256': sha(p)}],
                            'preview_paths': [rel(p)], 'related_geometry': related,
                            'note': 'Related model is not proof of image-to-model identity. Image is not a GLB and is not art acceptance.'})
    return {'schema_version': 1, 'policy': 'reuse existing files; approved observatory unchanged; no model copies; no promotion',
            'world_direction': 'Mythological epic East-West blend; only town explicitly old European',
            'excluded': ['.collab/artifacts/rejected/AE-0001-sparse-flat-farm (REJECTED / DO_NOT_REUSE)',
                         'runtime dependencies, protected duplicate copies, quarantined or unrelated whole-map candidates'],
            'evidence': ['.collab/artifacts/sol-high-architecture/architecture-contract.md',
                         '.collab/artifacts/reusable-world-assets-sol-high/blend-index.json'],
            'entries': entries}


def write_gallery(data: dict) -> None:
    def link(path: str) -> str:
        return quote(str(Path('../../../') / path).replace('\\', '/'), safe='/:')
    cards = []
    for entry in data['entries']:
        pictures = ''.join(f'<a href="{link(p)}"><img loading="lazy" src="{link(p)}" alt="{html.escape(Path(p).name)}"></a>' for p in entry['preview_paths'][:2])
        extra = '<details><summary>Additional proof frames</summary>' + ''.join(f'<p><a href="{link(p)}">{html.escape(Path(p).name)}</a></p>' for p in entry['preview_paths'][2:]) + '</details>' if len(entry['preview_paths']) > 2 else ''
        files = ''.join(f'<li><a href="{link(f["path"])}">{html.escape(f["path"])}</a><br><code>{f["sha256"]}</code>'
                        f'<br>{html.escape(json.dumps(f.get("evidence", {}), ensure_ascii=False))}</li>' for f in entry['files'])
        cards.append(f'<article data-search="{html.escape(json.dumps(entry).lower(), quote=True)}"><h2>{html.escape(entry["title"])}</h2>'
                     f'<b>{html.escape(entry["readiness"])}</b><p>{html.escape(entry["function"])}</p><div class="pictures">{pictures}</div>'
                     f'{extra}<p>{html.escape(entry.get("note", ""))}</p><ul>{files}</ul></article>')
    document = '''<!doctype html><html lang="en"><meta charset="utf-8"><title>Reusable world assets — actual file inventory</title>
<style>body{margin:28px;background:#172329;color:#e9e6d9;font:16px system-ui}a{color:#b4d9ff}h1{max-width:1000px}input{padding:14px;width:min(900px,90%);font:inherit;position:sticky;top:10px;z-index:2}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(420px,1fr));gap:20px}article{border:1px solid #65726c;padding:18px;min-width:0}h2{font-size:19px;overflow-wrap:anywhere}.pictures{display:flex;gap:8px}.pictures a{width:50%}img{width:100%;max-height:300px;object-fit:contain;background:#29333b}code,li{font-size:11px;overflow-wrap:anywhere}b{color:#f0cb7b}li{margin:10px 0}</style>
<h1>Reuse real local art; preserve the approved blue-and-gold observatory</h1>
<p>Mythological epic East–West world. Only town is explicitly old European. Geometry verification does not imply image approval or runtime acceptance.</p>
<p>All links use existing source files. No model copies. Historical warehouse tier names are not a canon level table. Rejected AE-0001 is excluded.</p>
<input id="q" aria-label="Filter assets" placeholder="Filter: approved, construction, image_only, meshbin…"><p id="count"></p><main>'''+''.join(cards)+'''</main>
<script>const cards=[...document.querySelectorAll('article')];function filter(){let q=document.querySelector('#q').value.toLowerCase();let n=0;cards.forEach(c=>{c.hidden=!c.dataset.search.includes(q);n+=!c.hidden});document.querySelector('#count').textContent=n+' / '+cards.length+' records'}document.querySelector('#q').addEventListener('input',filter);filter()</script></html>'''
    (OUT / 'index.html').write_text(document, encoding='utf8')


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    data = build()
    (OUT / 'catalog.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf8')
    write_gallery(data)
    counts = {}
    for entry in data['entries']:
        counts[entry['readiness']] = counts.get(entry['readiness'], 0) + 1
    print(json.dumps({'records': len(data['entries']), 'readiness': counts, 'out': str(OUT)}, indent=2))


if __name__ == '__main__':
    main()
