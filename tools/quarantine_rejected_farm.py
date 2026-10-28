"""Archive the frozen AE-0001 candidate without deleting or rewriting its files.

Run from any directory with --apply once, or --verify to audit retained files
and exact, independently verified purge records for rejected models only.
This deliberately targets only document-world-argon/Farm_Main and its flat
Farm_Main* files. It does not select approved observatory or live Unity assets.
"""

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import shutil
import subprocess


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / '.collab/artifacts/document-world-argon'
ARCHIVE = ROOT / '.collab/artifacts/rejected/AE-0001-sparse-flat-farm'
SCREENSHOT = Path('C:/Users/Admin/AppData/Local/Temp/codex-clipboard-db7b1cec-7c5b-4b4b-8de0-a0a123eaf838.png')
EXTENSIONS = {'.png', '.json', '.blend', '.blend1', '.glb', '.fbx'}
PURGE_AUDIT = ROOT / '.collab/artifacts/disk-cleanup-sol-high'
PURGEABLE_MODELS = {'.blend', '.blend1', '.glb', '.fbx'}


def within_repo(path):
    resolved = path.resolve()
    if resolved == ROOT or not resolved.is_relative_to(ROOT):
        raise RuntimeError(f'Unsafe repository target: {resolved}')
    return resolved


def digest(path):
    value = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(block)
    return value.hexdigest()


def describe(path):
    return {'bytes': path.stat().st_size, 'sha256': digest(path)}


def write_json(path, data):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(data, stream, ensure_ascii=False, indent=2)
        stream.write('\n')


def protected_files():
    paths = []
    reference = ROOT / 'ArtSource/Blender_3D_Map/Zone05_ReferenceDetail'
    if reference.exists():
        paths.extend(path for path in reference.rglob('*') if path.is_file())
    paths.extend((ROOT / 'Tools').glob('observatory*.py'))
    paths.extend(path for path in (ROOT / 'Assets/Scenes').rglob('*') if path.is_file())
    return sorted(set(paths))


def validate_purge_audit(manifest, plan, result, journal, independent):
    """Accept only model tombstones agreeing with every original/audit record."""
    if result.get('status') != 'PASS' or independent.get('status') != 'PASS':
        raise RuntimeError('Purge lacks completed and independent PASS verification')
    expected_count = result.get('delete_count')
    if not isinstance(expected_count, int) or expected_count <= 0:
        raise RuntimeError('Invalid purge deletion count')
    if independent.get('deleted_files_absent') != expected_count:
        raise RuntimeError('Independent purge deletion count differs')

    def indexed(records, label):
        values = {}
        for item in records:
            key = Path(item['path'])
            if not key.is_absolute() or key in values:
                raise RuntimeError(f'Invalid/duplicate path in {label}: {key}')
            values[key] = item
        if len(values) != expected_count:
            raise RuntimeError(f'Incomplete {label}')
        return values

    planned = indexed(plan['items'], 'purge plan')
    deleted = indexed(result['deleted'], 'purge result')
    logged = indexed(journal, 'deletion journal')
    if planned.keys() != deleted.keys() or deleted.keys() != logged.keys():
        raise RuntimeError('Plan/result/journal deletion paths differ')
    for path, item in deleted.items():
        timestamp = logged[path].get('deleted_utc')
        if not isinstance(timestamp, str):
            raise RuntimeError(f'Missing actual deletion timestamp: {path}')
        try:
            stamp = datetime.fromisoformat(timestamp)
            start = datetime.fromisoformat(plan['created_utc'])
            end = datetime.fromisoformat(result['completed_utc'])
            if any(value.tzinfo is None for value in (stamp, start, end)) or not start <= stamp <= end:
                raise ValueError('Deletion timestamp is outside audited purge window')
        except (KeyError, TypeError, ValueError) as error:
            raise RuntimeError(f'Invalid actual deletion timestamp: {path}') from error
        log_item = {key: value for key, value in logged[path].items() if key != 'deleted_utc'}
        if item != planned[path] or item != log_item:
            raise RuntimeError(f'Plan/result/journal record differs: {path}')

    originals = {within_repo(ARCHIVE / item['archived']): item for item in manifest['files']}
    tombstones = {}
    boundary = ARCHIVE / 'payload/document-world-argon'
    for path, item in deleted.items():
        if not path.is_relative_to(ARCHIVE):
            continue  # Installer/download caches never excuse an archive file.
        original = originals.get(path)
        if original is None or path.suffix.lower() not in PURGEABLE_MODELS:
            raise RuntimeError(f'Archive tombstone is not an original rejected model: {path}')
        expected = {key: original[key] for key in ('bytes', 'sha256')}
        if {key: item.get(key) for key in expected} != expected:
            raise RuntimeError(f'Purge hash/size differs from original quarantine: {path}')
        if item.get('original_source') != original['source'] or item.get('boundary') != str(boundary):
            raise RuntimeError(f'Purge source/boundary differs from original quarantine: {path}')
        if not item.get('reason', '').startswith('AE-0001 REJECTED / DO_NOT_REUSE:'):
            raise RuntimeError(f'Purge record lacks rejected-model reason: {path}')
        tombstones[path] = item
    return tombstones


def audited_purge_tombstones(manifest):
    result_path = within_repo(PURGE_AUDIT / 'cleanup-result.json')
    if not result_path.exists():
        return {}  # Initial quarantine still requires every archived file.
    plan = json.loads(within_repo(PURGE_AUDIT / 'deletion-plan.json').read_text(encoding='utf-8'))
    result = json.loads(result_path.read_text(encoding='utf-8'))
    journal = [json.loads(line) for line in within_repo(PURGE_AUDIT / 'deleted-files.jsonl').read_text(encoding='utf-8').splitlines()]
    independent = json.loads(within_repo(PURGE_AUDIT / 'verification.json').read_text(encoding='utf-8'))
    for name in ('manifest-before.json', 'manifest-after.json'):
        path = within_repo(ARCHIVE / name)
        if describe(path) != plan['retained'].get(str(path)):
            raise RuntimeError(f'Original quarantine manifest changed since purge: {name}')
    return validate_purge_audit(manifest, plan, result, journal, independent)


def verify():
    manifest = json.loads((ARCHIVE / 'manifest-before.json').read_text(encoding='utf-8'))
    tombstones = audited_purge_tombstones(manifest)
    checked = []
    purged = []
    for entry in manifest['files']:
        original = within_repo(ROOT / entry['source'])
        archived = within_repo(ARCHIVE / entry['archived'])
        if original.exists():
            raise RuntimeError(f'Rejected source reappeared: {original}')
        if archived in tombstones:
            if archived.exists():
                raise RuntimeError(f'Purged rejected output reappeared: {archived}')
            purged.append(entry['archived'])
            continue
        if describe(archived) != {key: entry[key] for key in ('bytes', 'sha256')}:
            raise RuntimeError(f'Archived bytes differ: {archived}')
        checked.append(entry['archived'])
    for entry in manifest['evidence_copies']:
        if describe(ARCHIVE / entry['archived']) != {key: entry[key] for key in ('bytes', 'sha256')}:
            raise RuntimeError(f'Evidence bytes differ: {entry["archived"]}')
    for name, expected in manifest['protected'].items():
        if describe(ROOT / name) != expected:
            raise RuntimeError(f'Protected file changed since quarantine: {name}')
    if (SOURCE / 'Farm_Main').exists():
        raise RuntimeError('Rejected Farm_Main directory reappeared')
    remaining = [path.name for path in SOURCE.iterdir() if path.is_file() and path.name.startswith('Farm_Main')]
    if remaining:
        raise RuntimeError(f'Rejected flat outputs reappeared: {remaining}')
    return {
        'status': 'VERIFIED_REJECTED_DO_NOT_REUSE',
        'verified_utc': datetime.now(timezone.utc).isoformat(),
        'archived_files': len(checked),
        'audited_purged_models': len(purged),
        'total_original_files': len(manifest['files']),
        'evidence_copies': len(manifest['evidence_copies']),
        'protected_files_unchanged': len(manifest['protected']),
        'files': checked,
        'purged_files': purged,
    }


def apply():
    for target in (SOURCE, ARCHIVE):
        within_repo(target)
    if ARCHIVE.exists():
        raise RuntimeError('Archive already exists; refusing overwrite. Use --verify.')
    process = subprocess.run(['tasklist', '/v', '/fo', 'csv'], capture_output=True, text=True, check=True, timeout=45)
    active = [line for line in process.stdout.splitlines() if line.lower().startswith(('"unity.exe",', '"blender.exe",'))]
    if active:
        raise RuntimeError('Close Unity/Blender before quarantine: ' + '\n'.join(active))
    candidates = [SOURCE / 'Farm_Main']
    candidates.extend(sorted(path for path in SOURCE.iterdir() if path.is_file() and path.name.startswith('Farm_Main')))
    if not candidates[0].is_dir():
        raise RuntimeError('Expected rejected Farm_Main directory is missing')
    files = []
    for source in candidates:
        within_repo(source)
        if source.is_symlink():
            raise RuntimeError(f'Refusing link: {source}')
        members = sorted(source.rglob('*')) if source.is_dir() else [source]
        for path in members:
            within_repo(path)
            if path.is_symlink():
                raise RuntimeError(f'Refusing link: {path}')
            if path.is_file():
                if path.suffix.lower() not in EXTENSIONS:
                    raise RuntimeError(f'Unexpected output type: {path}')
                files.append({
                    'source': path.relative_to(ROOT).as_posix(),
                    'archived': (Path('payload/document-world-argon') / path.relative_to(SOURCE)).as_posix(),
                    **describe(path),
                })
    evidence = [{
        'source': str(SCREENSHOT),
        'archived': 'evidence/user-rejected-codex-clipboard-db7b1cec-7c5b-4b4b-8de0-a0a123eaf838.png',
        **describe(SCREENSHOT),
    }]
    manifest = {
        'anti_example': 'AE-0001-sparse-flat-farm',
        'status': 'REJECTED / DO_NOT_REUSE',
        'created_utc': datetime.now(timezone.utc).isoformat(),
        'workspace': str(ROOT),
        'scope': 'Frozen rejected Farm_Main candidate and flat outputs only; no deletion.',
        'process_inspection': 'tasklist /v /fo csv; no Unity.exe or blender.exe; Argon mailbox 69 acknowledges zero writers; message 70 freezes old work.',
        'files': files,
        'evidence_copies': evidence,
        'protected': {path.relative_to(ROOT).as_posix(): describe(path) for path in protected_files()},
    }
    ARCHIVE.mkdir(parents=True, exist_ok=False)
    write_json(ARCHIVE / 'manifest-before.json', manifest)
    (ARCHIVE / 'process-inspection-before.txt').write_text(process.stdout, encoding='utf-8')
    (ARCHIVE / 'REJECTED_DO_NOT_REUSE.txt').write_text(
        'REJECTED / DO_NOT_REUSE\nCanonical anti-example AE-0001.\n'
        'Do not import, promote, regenerate in the original paths, or use as an art benchmark.\n'
        'See Docs/art-direction/anti-examples/AE-0001-sparse-flat-farm.md.\n'
        'Approved blue-roof/gold observatory remains approved and outside this quarantine.\n',
        encoding='utf-8',
    )
    for source in candidates:
        destination = within_repo(ARCHIVE / 'payload/document-world-argon' / source.relative_to(SOURCE))
        destination.parent.mkdir(parents=True, exist_ok=True)
        if destination.exists():
            raise RuntimeError(f'Refusing overwrite: {destination}')
        source.rename(destination)
    for entry in evidence:
        destination = within_repo(ARCHIVE / entry['archived'])
        destination.parent.mkdir(parents=True, exist_ok=True)
        if destination.exists():
            raise RuntimeError(f'Refusing overwrite: {destination}')
        shutil.copy2(entry['source'], destination)
    result = verify()
    write_json(ARCHIVE / 'manifest-after.json', {
        **result,
        'files': [{**entry, 'after': describe(ARCHIVE / entry['archived'])} for entry in files],
        'evidence_copies': evidence,
        'protected': manifest['protected'],
    })
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    actions = parser.add_mutually_exclusive_group(required=True)
    actions.add_argument('--apply', action='store_true')
    actions.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    print(json.dumps(apply() if args.apply else verify(), ensure_ascii=False, indent=2))
