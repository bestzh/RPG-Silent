"""Keep V2 intact and assemble the new personality pass in a separate folder."""
from pathlib import Path
import json

OUT=Path(__file__).resolve().parent
PREVIOUS=OUT.parent/'2026-10-29-synty-v2'
js=(PREVIOUS/'ui-design.js').read_text(encoding='utf-8')
js=js.replace('=>v2Header(name,currency,back);','=>v3Header(name,currency,back);')
js=js.replace("=>v2Heading(title,sub);","=>v3Heading(title,sub);")
(OUT/'ui-design.js').write_text(js,encoding='utf-8')
components=(PREVIOUS/'v2-components.js').read_text(encoding='utf-8')
components=components.replace("showScreen(params.get('screen')||location.hash.slice(1)||'start');",'/* V3 starts rendering after its components are registered. */')
(OUT/'v2-components.js').write_text(components,encoding='utf-8')
html=(PREVIOUS/'index.html').read_text(encoding='utf-8')
html=html.replace('Synty V2','风痕手札 V3').replace('· V2','· V3')
html=html.replace('Synty 界面 × PONETI 技能图标','风痕手札 · 更有冒险者的个性')
html=html.replace('新资源视觉提案','个性化视觉提案')
html=html.replace('<link rel="stylesheet" href="v2-refinements.css">','<link rel="stylesheet" href="v2-refinements.css">\n  <link rel="stylesheet" href="v3-theme.css">')
html=html.replace('<script src="v2-components.js"></script>','<script src="v2-components.js"></script>\n  <script src="v3-components.js"></script>')
(OUT/'index.html').write_text(html,encoding='utf-8')
render=(PREVIOUS/'render-design.cjs').read_text(encoding='utf-8')
render=render.replace('Synty V2','Windworn V3').replace('SYNTY V2','WINDWORN V3')
render=render.replace('深夜蓝 × 徽章金 / 新资源统一视觉提案','苔绿皮革 × 手札纸页 × 旧黄铜 / 个性化视觉提案')
render=render.replace('Synty Fantasy Menus + Warrior HUD + PONETI 6000 Fantasy Icons / 2026.10.29','风痕手札 / 40 个页面与状态 / 保留现有资源与功能 / 2026.10.29')
render=render.replace('统一组件、色彩与 21 个技能的图标对应','物件感、职业差异与 21 个技能的星图式编排')
render=render.replace('UI DESIGN / 02','UI DESIGN / 03').replace('background:#0e1b2a;color:#e1dfd3','background:#172d25;color:#e8dfc6').replace('color:#9aafc1','color:#afba9d').replace('color:#93abbd','color:#a1b394')
(OUT/'render-design.cjs').write_text(render,encoding='utf-8')
manifest=json.loads((PREVIOUS/'source-manifest.json').read_text(encoding='utf-8'))
manifest['version']='Windworn V3'
manifest['art_direction']='Moss leather, parchment notes, worn brass; asymmetric layouts and profession motifs.'
(OUT/'source-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Windworn V3 composed; V2 preserved.')
