"""Read-only source inventory for UI contracts; does not invoke Unity or game code."""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
records = []
for source in sorted((ROOT / 'Assets/Scripts').rglob('*.cs')):
    if 'Editor' in source.parts:
        continue
    raw = source.read_bytes()
    text = raw.decode('utf-8-sig')
    lines = text.splitlines()
    methods = []
    ui = []
    for number, line in enumerate(lines, 1):
        if re.match(r'^\s*(?:(?:public|private|protected|internal|static|override|virtual|async|sealed)\s+)*(?:void|bool|string|int|float|Task|[A-Z]\w*(?:<[^>]+>)?)\s+\w+\s*\(', line):
            methods.append({'line': number, 'signature': line.strip()})
        if 'UI' in source.parts and ('W.Button(' in line or 'W.Text(' in line or re.search(r'\.(?:text|interactable)\s*=', line)):
            ui.append({'line': number, 'code': line.strip()})
    records.append({'path': source.relative_to(ROOT).as_posix(), 'lines': len(lines),
                    'sha256': hashlib.sha256(raw).hexdigest(), 'methods': methods,
                    'ui_evidence': ui,
                    'service_calls': sorted(set(re.findall(r'\b(?:progress|interaction|player|session|dungeon|run|loader|sceneLoader)\.([A-Za-z_]\w*)', text)))})
(OUT / 'source-catalog.json').write_text(json.dumps({'scope': 'First-party runtime C# under Assets/Scripts; vendor and Editor internals excluded', 'records': records}, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'runtime_files': len(records), 'ui_files': sum('/UI/' in r['path'] for r in records), 'runtime_lines': sum(r['lines'] for r in records), 'ui_lines': sum(r['lines'] for r in records if '/UI/' in r['path'])}, ensure_ascii=False))
