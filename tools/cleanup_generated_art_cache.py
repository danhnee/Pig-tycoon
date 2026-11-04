"""Purge only individually verified rejected outputs and redundant download caches.

--plan writes an immutable, hashed deletion plan. --apply consumes that plan;
--verify checks the final audit, retained evidence and protected-file baseline.
No discovery result outside the three explicit boundaries becomes a delete target.
"""
import argparse
import ctypes
from ctypes import wintypes
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
AUDIT = ROOT / '.collab/artifacts/disk-cleanup-sol-high'
ARCHIVE = ROOT / '.collab/artifacts/rejected/AE-0001-sparse-flat-farm'
PAYLOAD = ARCHIVE / 'payload/document-world-argon'
MOUNTAIN = ROOT / '.collab/artifacts/mountain-reference-study-codex'
PARTS = MOUNTAIN / 'download-parts'
WHEEL = MOUNTAIN / 'bpy-5.2.2-cp313-cp313-win_amd64.whl'
INSTALLER = Path('D:/CodexHome/cache/unity/UnitySetup64-6000.6.3f1.exe')
EDITOR = Path('D:/Unity/Editor/6000.6.3f1/Editor/Unity.exe')
INSTALLER_SHA = '2e2eed1a2482afe272dbc9422825cca5ee303de6a2719679b949ca571abdafb5'
PLAN = AUDIT / 'deletion-plan.json'
RESULT = AUDIT / 'cleanup-result.json'


def now():
    return datetime.now(timezone.utc).isoformat()


def safe(path, boundary):
    """Check lexical boundary, resolved boundary and every reparse ancestor."""
    path, boundary = Path(os.path.abspath(path)), Path(os.path.abspath(boundary))
    if path != boundary and not path.is_relative_to(boundary):
        raise RuntimeError(f'Outside approved boundary: {path}')
    for part in [path, *path.parents]:
        if part.exists() and (part.is_symlink() or part.lstat().st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT):
            raise RuntimeError(f'Refusing reparse point: {part}')
    if not path.resolve().is_relative_to(boundary.resolve()):
        raise RuntimeError(f'Resolved outside boundary: {path}')
    return path


def digest(path):
    value = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(block)
    return value.hexdigest()


def describe(path):
    return {'bytes': path.stat().st_size, 'sha256': digest(path)}


def allocated(path):
    class FileStandardInfo(ctypes.Structure):
        _fields_ = [('allocation_size', ctypes.c_longlong), ('end_of_file', ctypes.c_longlong), ('links', wintypes.DWORD), ('delete_pending', ctypes.c_ubyte), ('directory', ctypes.c_ubyte)]
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    kernel.CreateFileW.argtypes = [wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD, ctypes.c_void_p, wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE]
    kernel.CreateFileW.restype = wintypes.HANDLE
    kernel.GetFileInformationByHandleEx.argtypes = [wintypes.HANDLE, ctypes.c_int, ctypes.c_void_p, wintypes.DWORD]
    kernel.CloseHandle.argtypes = [wintypes.HANDLE]
    handle = kernel.CreateFileW(str(path), 0x80, 7, None, 3, 0, None)
    if handle == ctypes.c_void_p(-1).value:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        info = FileStandardInfo()
        if not kernel.GetFileInformationByHandleEx(handle, 1, ctypes.byref(info), ctypes.sizeof(info)):
            raise ctypes.WinError(ctypes.get_last_error())
        if info.links != 1:
            raise RuntimeError(f'Refusing hard-linked delete target: {path}')
        return info.allocation_size
    finally:
        kernel.CloseHandle(handle)


def write_new(path, value):
    safe(path, AUDIT)
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, ensure_ascii=False, indent=2)
        stream.write('\n')


def deletion_target(item):
    """Never trust a plan's boundary as authority to expand deletion scope."""
    path = Path(item['path'])
    if path == INSTALLER:
        boundary = INSTALLER.parent
    elif path == WHEEL:
        boundary = MOUNTAIN
    elif path.parent == PARTS and path.name.isdecimal():
        boundary = PARTS
    else:
        original = json.loads((ARCHIVE / 'manifest-before.json').read_text(encoding='utf-8'))
        frozen_models = {ARCHIVE / entry['archived'] for entry in original['files'] if Path(entry['archived']).suffix.lower() in {'.blend','.blend1','.glb','.fbx'}}
        if path not in frozen_models:
            raise RuntimeError(f'Target is not an authorized rejected file or download cache: {path}')
        boundary = PAYLOAD
    if Path(item['boundary']) != boundary:
        raise RuntimeError(f'Plan boundary changed: {path}')
    return safe(path, boundary)


def editor_proof():
    safe(EDITOR, Path('D:/Unity/Editor/6000.6.3f1'))
    version = ctypes.WinDLL('version', use_last_error=True)
    version.GetFileVersionInfoSizeW.argtypes = [wintypes.LPCWSTR, ctypes.POINTER(wintypes.DWORD)]
    version.GetFileVersionInfoW.argtypes = [wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD, ctypes.c_void_p]
    version.VerQueryValueW.argtypes = [ctypes.c_void_p, wintypes.LPCWSTR, ctypes.POINTER(ctypes.c_void_p), ctypes.POINTER(wintypes.UINT)]
    size = version.GetFileVersionInfoSizeW(str(EDITOR), None)
    if not size:
        raise RuntimeError('Installed Unity has no readable PE version')
    buffer = ctypes.create_string_buffer(size)
    if not version.GetFileVersionInfoW(str(EDITOR), 0, size, buffer):
        raise ctypes.WinError()
    ptr, length = ctypes.c_void_p(), wintypes.UINT()
    if not version.VerQueryValueW(buffer, '\\', ctypes.byref(ptr), ctypes.byref(length)):
        raise ctypes.WinError()
    words = ctypes.cast(ptr, ctypes.POINTER(wintypes.DWORD))
    numeric = [words[2] >> 16, words[2] & 65535, words[3] >> 16, words[3] & 65535]
    if numeric[:3] != [6000, 6, 3]:
        raise RuntimeError(f'Unexpected installed Unity version: {numeric}')
    product_versions = []
    if version.VerQueryValueW(buffer, '\\VarFileInfo\\Translation', ctypes.byref(ptr), ctypes.byref(length)):
        translations = ctypes.cast(ptr, ctypes.POINTER(wintypes.WORD))
        for i in range(length.value // 4):
            query = f'\\StringFileInfo\\{translations[2*i]:04x}{translations[2*i+1]:04x}\\ProductVersion'
            value, value_length = ctypes.c_void_p(), wintypes.UINT()
            if version.VerQueryValueW(buffer, query, ctypes.byref(value), ctypes.byref(value_length)):
                product_versions.append(ctypes.wstring_at(value))
    if not any(v.startswith('6000.6.3f1') for v in product_versions):
        raise RuntimeError(f'Unity product version is not pinned version: {product_versions}')
    return {'path': str(EDITOR), 'numeric_version': numeric, 'product_versions': product_versions, **describe(EDITOR)}


def runtime_proof():
    safe(MOUNTAIN / 'runtime', MOUNTAIN)
    process = subprocess.run([sys.executable, str(AUDIT / 'runtime_probe.py')], capture_output=True, text=True, timeout=45)
    if process.returncode != 0:
        raise RuntimeError(f'bpy runtime probe failed: {process.stdout}\n{process.stderr}')
    line = next(line for line in process.stdout.splitlines() if line.startswith('{'))
    proof = json.loads(line)
    if proof['status'] != 'PASS' or proof['bpy_version'] != '5.2.2 LTS':
        raise RuntimeError(f'Unexpected runtime: {proof}')
    return {'exit_code': process.returncode, 'proof': proof, 'stderr': process.stderr}


def plan():
    if PLAN.exists() or RESULT.exists():
        raise RuntimeError('Audit already exists; refusing overwrite')
    AUDIT.mkdir(parents=True, exist_ok=True)
    original = json.loads((ARCHIVE / 'manifest-before.json').read_text(encoding='utf-8'))
    items, retained = [], {}
    for entry in original['files']:
        path = safe(ARCHIVE / entry['archived'], PAYLOAD)
        if describe(path) != {k: entry[k] for k in ('bytes', 'sha256')}:
            raise RuntimeError(f'Rejected archive changed: {path}')
        if path.suffix.lower() in {'.blend', '.blend1', '.glb', '.fbx'}:
            items.append({'path': str(path), 'boundary': str(PAYLOAD), 'reason': 'AE-0001 REJECTED / DO_NOT_REUSE: reproducible rejected model or duplicate export; original hash retained', 'original_source': entry['source'], **describe(path), 'allocated_bytes': allocated(path)})
        else:
            retained[str(path)] = describe(path)
    for entry in original['evidence_copies']:
        path = safe(ARCHIVE / entry['archived'], ARCHIVE)
        if describe(path) != {k: entry[k] for k in ('bytes', 'sha256')}:
            raise RuntimeError('Original rejection screenshot changed')
        retained[str(path)] = describe(path)
    for name in ['manifest-before.json', 'manifest-after.json', 'REJECTED_DO_NOT_REUSE.txt']:
        retained[str(ARCHIVE/name)] = describe(ARCHIVE/name)
    protected = {str(ROOT/name):describe(safe(ROOT/name, ROOT)) for name in original['protected']}
    drift = [name for name, expected in original['protected'].items() if protected[str(ROOT/name)] != expected]
    editor = editor_proof()
    runtime = runtime_proof()
    if digest(INSTALLER) != INSTALLER_SHA:
        raise RuntimeError('Installer differs from verified installed official download')
    safe(INSTALLER, INSTALLER.parent)
    items.append({'path':str(INSTALLER), 'boundary':str(INSTALLER.parent), 'reason':'Redundant installer cache: pinned Unity 6000.6.3f1 installed and independently PE-version verified; prior installer exit 0 attested by coordinating agent', **describe(INSTALLER), 'allocated_bytes':allocated(INSTALLER)})
    safe(WHEEL, MOUNTAIN)
    wheel_hash = digest(WHEEL)
    joined = hashlib.sha256()
    part_paths = []
    for path in sorted(PARTS.iterdir(), key=lambda p:int(p.name)):
        safe(path, PARTS)
        if not path.is_file() or not path.name.isdecimal():
            raise RuntimeError(f'Unexpected download part: {path}')
        part_paths.append(path)
        with path.open('rb') as stream:
            for block in iter(lambda: stream.read(1024*1024), b''):
                joined.update(block)
    if joined.hexdigest() != wheel_hash:
        raise RuntimeError('Download parts do not reconstruct wheel exactly')
    for path in [WHEEL, *part_paths]:
        items.append({'path':str(path), 'boundary':str(MOUNTAIN if path==WHEEL else PARTS), 'reason':'Redundant bpy download cache; installed 5.2.2 runtime independently usable; concatenated parts SHA-256 matches wheel', **describe(path), 'allocated_bytes':allocated(path)})
    result = {'created_utc':now(), 'disk_before':shutil.disk_usage('D:/')._asdict(), 'items':items, 'retained':retained, 'protected_before':protected, 'pre_existing_protected_drift_from_quarantine':drift, 'unity_installed':editor, 'bpy_runtime':runtime, 'wheel_sha256':wheel_hash, 'delete_count':len(items), 'logical_bytes':sum(x['bytes'] for x in items), 'allocated_bytes':sum(x['allocated_bytes'] for x in items), 'scope':'Only canonical rejected payload model extensions, one installed Unity cached installer, and bpy wheel/download-parts. No live Assets, approved observatory, ArtSource, runtime dependencies, or other projects.'}
    write_new(PLAN,result)
    return {k:v for k,v in result.items() if k not in {'items','retained','protected_before'}}


def apply():
    if RESULT.exists():
        raise RuntimeError('Cleanup already recorded; use --verify')
    data = json.loads(PLAN.read_text(encoding='utf-8'))
    processes = subprocess.run(['tasklist','/FO','CSV','/NH'],capture_output=True,text=True,check=True,timeout=30).stdout
    if any(line.lower().startswith('"unity.exe",') for line in processes.splitlines()):
        raise RuntimeError('Unity Editor must be closed before repository cleanup')
    verify_kept(data)
    editor_proof()
    runtime_proof()
    # Verify every target before any deletion, then recheck before each unlink.
    for item in data['items']:
        path = deletion_target(item)
        if describe(path) != {k:item[k] for k in ('bytes','sha256')}:
            raise RuntimeError(f'Deletion target changed: {path}')
    disk_before = shutil.disk_usage('D:/')._asdict()
    deleted = []
    journal = AUDIT/'deleted-files.jsonl'
    with journal.open('x', encoding='utf-8') as stream:
        for item in data['items']:
            path = deletion_target(item)
            if describe(path) != {k:item[k] for k in ('bytes','sha256')}:
                raise RuntimeError(f'Target changed during cleanup: {path}')
            path.unlink()
            deleted.append(item)
            stream.write(json.dumps({**item, 'deleted_utc':now()})+'\n')
            stream.flush()
            os.fsync(stream.fileno())
    if PARTS.exists():
        safe(PARTS, MOUNTAIN).rmdir()  # Empty only; no recursive deletion.
    verify_kept(data)
    disk_after = shutil.disk_usage('D:/')._asdict()
    result = {'status':'PASS', 'completed_utc':now(), 'delete_count':len(deleted), 'logical_bytes_deleted':sum(x['bytes'] for x in deleted), 'allocated_bytes_deleted':sum(x['allocated_bytes'] for x in deleted), 'disk_before':disk_before, 'disk_after':disk_after, 'observed_free_bytes_delta':disk_after['free']-disk_before['free'], 'measurement_caveat':'Concurrent art/Unity jobs can change drive free bytes; allocated bytes are per-deleted-file Windows measurements.', 'protected_files_unchanged_during_cleanup':len(data['protected_before']), 'pre_existing_protected_drift_from_quarantine':data['pre_existing_protected_drift_from_quarantine'], 'retained_evidence_files':len(data['retained']), 'unity_installed':editor_proof(), 'bpy_runtime':runtime_proof(), 'deleted':deleted}
    write_new(RESULT,result)
    return {k:v for k,v in result.items() if k != 'deleted'}


def verify_kept(data):
    for group in ['retained','protected_before']:
        for name, expected in data[group].items():
            if describe(Path(name)) != expected:
                raise RuntimeError(f'Retained/protected file changed: {name}')


def verify():
    data = json.loads(PLAN.read_text(encoding='utf-8'))
    result = json.loads(RESULT.read_text(encoding='utf-8'))
    verify_kept(data)
    if any(Path(item['path']).exists() for item in data['items']):
        raise RuntimeError('A deleted target has reappeared')
    original = json.loads((ARCHIVE/'manifest-before.json').read_text(encoding='utf-8'))
    if any((ROOT/item['source']).exists() for item in original['files']):
        raise RuntimeError('An original rejected source reappeared')
    editor_proof()
    runtime_proof()
    return {'status':'PASS', 'verified_utc':now(), 'deleted_files_absent':result['delete_count'], 'retained_evidence_files_unchanged':len(data['retained']), 'protected_files_unchanged_during_cleanup':len(data['protected_before']), 'logical_bytes_deleted':result['logical_bytes_deleted'], 'allocated_bytes_deleted':result['allocated_bytes_deleted'], 'observed_free_bytes_delta_at_cleanup':result['observed_free_bytes_delta'], 'current_disk_free':shutil.disk_usage('D:/').free, 'pre_existing_protected_drift_from_quarantine':data['pre_existing_protected_drift_from_quarantine']}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--plan', action='store_true')
    group.add_argument('--apply', action='store_true')
    group.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    print(json.dumps(plan() if args.plan else apply() if args.apply else verify(), ensure_ascii=False, indent=2))
