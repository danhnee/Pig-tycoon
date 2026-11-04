"""Preserve source village geometry while replacing nonportable embedded MonoScripts."""
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/Scenes/BoRiaVillage2D.unity'
OUTPUT = ROOT / 'Assets/Scenes/PixelWorldSourceAdapters/BoRiaVillage2D.unity'
RECEIPT = ROOT / 'Docs/plans/2d-village-source-adapter.json'


def adapt():
    source = SOURCE.read_text(encoding='utf-8-sig')
    guid = re.search(r'^guid: (\w+)$', (ROOT / 'Assets/Scripts/Controllers/VillageInteractableTrigger.cs.meta')
                     .read_text(encoding='utf-8-sig'), re.M).group(1)
    documents = re.split(r'(?=^--- !u!)', source, flags=re.M)
    removed, rebound, kept = [], [], []
    for document in documents:
        header = re.match(r'--- !u!(\d+) &(-?\d+)', document)
        if header and header[1] == '115':
            name = re.search(r'^  m_ClassName: (.+)$', document, re.M)
            if name and name[1] in ('VillageInteractableTrigger', 'TransitionTriggerListener'):
                removed.append(header[2])
                if name[1] == 'VillageInteractableTrigger':
                    rebound.append(header[2])
                continue
        if header and header[1] == '114' and '::PigTycoon.Presentation.TransitionTriggerListener' in document:
            removed.append(header[2])
            continue
        kept.append(document)
    result = ''.join(kept)
    for file_id in rebound:
        result = result.replace('m_Script: {fileID: ' + file_id + '}',
                                'm_Script: {fileID: 11500000, guid: ' + guid + ', type: 3}')
    for file_id in removed:
        result = re.sub(r'^  - component: \{fileID: ' + re.escape(file_id) + r'\}\r?\n', '', result, flags=re.M)
        result = result.replace('{fileID: ' + file_id + '}', '{fileID: 0}')
    if re.search(r'm_Script:\s*\{fileID:\s*[1-9]\d*\s*\}', result):
        raise ValueError('Unadapted embedded MonoScript reference remains')
    receipt = {
        'source': SOURCE.relative_to(ROOT).as_posix(),
        'output': OUTPUT.relative_to(ROOT).as_posix(),
        'sourceSha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        'outputSha256': hashlib.sha256(result.encode('utf-8')).hexdigest(),
        'removedLegacyDocumentIds': removed,
        'reboundVillageScriptIds': rebound,
        'scope': 'Original scene unchanged; retain geometry/NPCs/UI. Remove legacy warp listener and replace embedded village script with standalone MonoScript.'
    }
    return result, receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    result, receipt = adapt()
    if args.verify:
        if not OUTPUT.is_file() or OUTPUT.read_bytes() != result.encode('utf-8'):
            raise SystemExit('Village source adapter is missing or stale')
        if json.loads(RECEIPT.read_text(encoding='utf-8')) != receipt:
            raise SystemExit('Village source receipt is stale')
    else:
        OUTPUT.parent.mkdir(parents=True, exist_ok=True)
        OUTPUT.write_bytes(result.encode('utf-8'))
        RECEIPT.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('Village source adapter verified' if args.verify else 'Village source adapter staged; original source preserved')


if __name__ == '__main__':
    main()
