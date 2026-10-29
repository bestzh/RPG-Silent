"""Validate the rendered design delivery and package it without development helpers."""
from pathlib import Path
from datetime import datetime, timezone
import json
import zipfile
from PIL import Image

root = Path(__file__).resolve().parent
qa = json.loads((root / 'qa-report.json').read_text(encoding='utf-8'))
gallery = json.loads((root / 'gallery-check.json').read_text(encoding='utf-8'))
assert qa['rendered'] == 42 and not qa['errors'], 'Incomplete render'
assert all(not row[key] for row in qa['audit'] for key in ('broken_images', 'outside_canvas', 'panel_overflow')), 'Layout checks failed'
assert gallery['passed'], 'Gallery checks failed'
screens = sorted((root / 'screens').glob('*.png'))
boards = sorted((root / 'boards').glob('*.png'))
assert len(screens) == 42 and len(boards) == 8
for screen in screens:
    with Image.open(screen) as img:
        assert img.size == (1920, 1080), screen.name
summary = {
    'version': 'Windworn V3',
    'generated_at': datetime.now(timezone.utc).isoformat(),
    'art_direction': '风痕手札：苔绿皮革、手写注记、纸页与旧黄铜，错落构图和职业差异。',
    'game_page_state_count': 40,
    'reference_sheet_count': 2,
    'png_count': len(screens),
    'artboard': [1920, 1080],
    'overview_board_count': len(boards),
    'layout_checks_passed': True,
    'gallery_checks_passed': gallery['passed'],
    'source_asset_copies': 114,
    'previous_version_preserved': True,
    'game_code_modified': False,
    'unity_assets_modified': False,
    'import_evidence': '沿用 2026-10-29 已核实的导入记录；本版未重新导入资源。',
    'limitations': [
        '部分 Synty 示例场景图仅作 UI 排版参照，非实际游戏关卡、人物或 Boss。',
        '部分图鉴缩略图为项目职业模型占位。',
        '中文正文使用系统 Microsoft YaHei 预览，未分发该字体。',
        '按钮功能与数值为设计预览，非 Unity 功能测试。',
    ],
}
(root / 'delivery-summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
archive = root.parent / 'RPG-Silent-UI-Design-v3.zip'
prefix = 'RPG-Silent-UI-Design-v3'
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as package:
    for item in sorted(root.rglob('*')):
        relative = item.relative_to(root)
        if not item.is_file() or item.suffix in ('.py', '.cjs') or item.name == 'qa-smoke.json' or '_review' in relative.parts:
            continue
        package.write(item, f'{prefix}/{relative.as_posix()}')
with zipfile.ZipFile(archive) as package:
    assert package.testzip() is None, 'ZIP integrity failure'
    file_count = len(package.infolist())
print(json.dumps({'archive': str(archive), 'megabytes': round(archive.stat().st_size / 1048576, 1), 'files': file_count, 'screens': len(screens), 'boards': len(boards), 'passed': True}, ensure_ascii=False))
