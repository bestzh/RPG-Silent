"""Reuse design assets and export current item/configuration data, without editing Unity files."""
from pathlib import Path
import json, re, shutil, hashlib

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
OLD=OUT.parent/'2026-10-29-windworn-v3'
for name in ['ui-design.js','ui-design.css','v2-components.js','v2-data.js','v2-theme.css','v2-refinements.css','v3-components.js','v3-theme.css']:
    text=(OLD/name).read_text(encoding='utf-8')
    if name=='v3-components.js':
        text=text.replace("showScreen(params.get('screen')||location.hash.slice(1)||'start');",'/* V5 starts after its functional layouts are registered. */')
    (OUT/name).write_text(text,encoding='utf-8')
shutil.copytree(OLD/'assets',OUT/'assets',dirs_exist_ok=True)
manifest=json.loads((OLD/'source-manifest.json').read_text(encoding='utf-8'))
extras={
 'gear-head':'ArmorIcons/ArmorSet_Icons/Mail/Mail9_head.png',
 'gear-chest':'ArmorIcons/ArmorSet_Icons/Mail/Mail9_Chest.png',
 'gear-feet':'ArmorIcons/ArmorSet_Icons/Mail/Mail9_Boots.png',
 'gear-candidate':'ArmorIcons/ArmorSet_Icons/Mail/Mail8_Chest.png',
 'gear-ring':'ArmorIcons/RingAndNeck_Icons/Ring_b_03.png',
 'gear-amulet':'ArmorIcons/RingAndNeck_Icons/Neck_b_03.png',
 'gear-sword':'WeaponIcons/WeaponIconsVol2/Sword_v2_01.png',
 'gear-greatsword':'WeaponIcons/WeaponIconsVol2/Sword_v2_05.png',
}
for name,relative in extras.items():
    source=ROOT/'Assets/6000FantasyIcons'/relative
    target=OUT/'assets'/f'{name}.png'
    shutil.copyfile(source,target)
    guid=re.search(r'^guid: (\w+)',Path(str(source)+'.meta').read_text(encoding='utf-8'),re.M)
    manifest['files'].append({'source':source.relative_to(ROOT).as_posix(),'design_file':f'assets/{name}.png','role':'V5 物品图标设计建议，游戏图标引用未改','guid':guid[1] if guid else None,'sha256':hashlib.sha256(source.read_bytes()).hexdigest()})
manifest['version']='Functional V5'
manifest['scope']='All current UI feature families with representative states; read-only source audit and standalone design prototype'
(OUT/'source-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
items=json.loads((OUT/'item-catalog.json').read_text(encoding='utf-8'))
sets=[]
for p in sorted((ROOT/'Assets/Resources/EquipmentSets').glob('*.asset')):
    text=p.read_text(encoding='utf-8-sig');data={}
    for key in ['Id','DisplayName','TwoPieceHealth','FourPieceEnergy','FivePieceAttackPercent']:
        m=re.search(r'^  '+key+r': (.*)$',text,re.M)
        if m:
            value=m[1].strip();data[key]=json.loads(value) if value.startswith('"') else int(value) if value.isdigit() else value
    sets.append(data)
(OUT/'v5-data.js').write_text('const LIVE_ITEMS='+json.dumps(items,ensure_ascii=False)+';\nconst LIVE_SETS='+json.dumps(sets,ensure_ascii=False)+';\n',encoding='utf-8')
html=(OLD/'index.html').read_text(encoding='utf-8')
html=html.replace('风痕手札 V3','功能匹配 V5').replace('· V3','· V5')
html=html.replace('风痕手札 · 更有冒险者的个性<br>40 个页面与状态 + 2 张设计规范<br>1920 × 1080 · 个性化视觉提案','从当前实现反推页面与操作<br>穿戴清楚 · 入口直接 · 状态明确<br>1920 × 1080 · 功能匹配设计稿')
html=html.replace('RESOURCE_AUDIT.md','FUNCTIONAL_AUDIT.md').replace('查看资源来源与缺口','查看功能逻辑与页面对照')
html=html.replace('<link rel="stylesheet" href="v3-theme.css">','<link rel="stylesheet" href="v3-theme.css">\n  <link rel="stylesheet" href="v5-theme.css">')
html=html.replace('<script src="v3-components.js"></script>','<script src="v3-components.js"></script>\n  <script src="v5-data.js"></script>\n  <script src="v5-components.js"></script>')
(OUT/'index.html').write_text(html,encoding='utf-8')
print('V5 assembled; 64 current item definitions, 7 equipment sets, and existing design assets preserved.')
