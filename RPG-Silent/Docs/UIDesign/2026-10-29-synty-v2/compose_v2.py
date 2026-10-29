"""Build a file:// compatible review gallery from the previous page coverage."""
from pathlib import Path
import json

OUT=Path(__file__).resolve().parent
OLD=OUT.parent/'2026-10-28'
data=json.loads((OUT/'skill-icon-map.json').read_text(encoding='utf-8'))
data['assets']=json.loads((OUT/'asset-map.json').read_text(encoding='utf-8'))
(OUT/'v2-data.js').write_text('const V2='+json.dumps(data,ensure_ascii=False)+';\n',encoding='utf-8')
js=(OLD/'ui-design.js').read_text(encoding='utf-8')
lines=js.splitlines()
for i,line in enumerate(lines):
    if line.startswith('const header='):
        lines[i]="const header=(name,currency=true,back='hud-town')=>v2Header(name,currency,back);"
    if line.startswith('const heading='):
        lines[i]="const heading=(title,sub='')=>v2Heading(title,sub);"
js='\n'.join(lines)
js=js.replace("${crystal?'world-dungeon.png':training?'bg-portal.png':'bg-castle.png'}", "${crystal?'sy-destination-crystal.png':training?'sy-destination-training.png':'sy-destination-forest.png'}")
js=js.replace("${crystal?'background-size:180%;background-position:center 55%':''}", '')
js=js.replace("+' / 40'", "+' / 41'")
js=js.replace(";showScreen(params.get('screen')||location.hash.slice(1)||'start');",';')
(OUT/'ui-design.js').write_text(js+'\n',encoding='utf-8')
html=(OLD/'index.html').read_text(encoding='utf-8')
html=html.replace('全界面设计提案','全界面设计提案 · Synty V2').replace('2026.10.28','2026.10.29 · V2')
html=html.replace('现有美术资源 × 统一界面系统<br>40 个页面与关键状态 · 1920 × 1080','Synty 界面 × PONETI 技能图标<br>40 个页面与状态 + 2 张设计规范<br>1920 × 1080 · 新资源视觉提案')
html=html.replace('<link rel="stylesheet" href="ui-design.css">','<link rel="stylesheet" href="ui-design.css">\n  <link rel="stylesheet" href="v2-theme.css">\n  <link rel="stylesheet" href="v2-refinements.css">')
html=html.replace('<script src="ui-design.js"></script>','<script src="v2-data.js"></script>\n  <script src="ui-design.js"></script>\n  <script src="v2-components.js"></script>')
(OUT/'index.html').write_text(html,encoding='utf-8')
print('V2 gallery composed: 40 game pages/states, 2 design reference sheets.')
