"""Generate the design review index from read-only source/configuration snapshots."""
from pathlib import Path
from datetime import datetime
import json,hashlib,html
P=Path(__file__).resolve().parent
ROOT=P.parents[2]
read=lambda n:json.loads((P/n).read_text(encoding='utf8'))
source=read('source-catalog.json')['records']; cfg=read('configuration-catalog.json'); screens=read('screen-manifest.json')['screens']
game=[s for s in screens if s['id']!='skills-visual-guide']
families=[
 ('启程与角色',['StartUI','CharacterCreationView','CharacterSelectionView','CharacterModelPreview'],['start','creation','characters','save-menu-failed'],'新建只选择基础职业与性别；进阶分支在导师处开放。角色记录独立，删除与无法继续读取各自说明后果。','进入角色页 → 选择记录 → 继续；新建使用当前职业模型。'),
 ('游玩状态与战斗',['MainUI.cs','MainUI.Combat','MainUI.Targeting','MainUI.Adventure','MainUI.Journey','MainUI.EquipmentEffects','SkillSlotUI','WeaponEquipmentUI'],['hud-town','hud-dungeon','world-feedback','hud-expedition','hud-long','training'],'生命、能量、冷却、职业机制、目标与 Boss 阶段分别固定位置；主城禁战，训练不发奖；危险预警与操作反馈不占任务列表。','战斗中保留视野，技能/药品用现有快捷键；需要鼠标时显示光标。'),
 ('拾取与离场',['MainUI.Loot'],['nearby-loot','loot-leave','world-feedback'],'L 附近列表、6 种品质显示筛选、每页 8 项；方向键选择，E/Enter 仅拾取可达物品。满包或保存失败仍留地面，金币经验吸附；离场有未拾取金红物品先确认。','选择掉落 → 拾取；品质筛选不改变掉落。'),
 ('当前穿戴与试穿',['InventoryUI.cs'],['inventory','inventory-tryon','inventory-weapon','inventory-low-level','inventory-save-failed','inventory-empty-slot'],'5 个防具饰品槽与 2 个武器槽持续显示名称、强化、空槽和当前/备用。查看、试穿、实际穿戴分开；低等级可预览，确认需满足等级与角色状态。','防具：选中 → 对比全身变化 → 确认穿戴；武器：选中 → 指定槽 1/2。'),
 ('背包整理与药品',['InventoryUI.Organization','InventoryUI.Consumables'],['inventory-search','inventory-consumable','inventory-overflow','inventory-bulk-sell','inventory-bulk-discard','inventory-bulk-review'],'24 格背包、待领取上限 256 项；7 类直接选择，名称搜索，品质/等级/类型/获得排序双向。新物品与锁定可见；药品设 Q/R/F，共享冷却。输入 B 不关闭，Esc 分层取消。','常用整理留在页面，批量选择后一次核对数量与结果。'),
 ('整套方案、分解与丢弃',['InventoryUI.Management','InventoryUI.Discard'],['inventory-outfit','loadouts','inventory-batch','discard'],'3 个方案保存 5 防具、2 武器与当前槽。多件试穿逐部位组合，确认整套前不变；涉及武器的整套调整回主城。出售/分解保护穿戴、两武器槽、锁定、宝石；分解仅防具饰品。丢弃可回收，离场消失。','复杂整理作为次要入口；单件穿戴不要求先建方案。'),
 ('装备特殊效果',['InventoryUI.Effects'],['equipment-effects'],'仅传说/神话对应固有效果；I/II 两级，同类取最高，当前武器与已穿戴防具饰品才生效。试穿不触发，不靠换装刷新本场冷却。','物品详情就地打开规则，说明触发、效果、冷却和生效来源。'),
 ('任务、章节、收藏',['QuestJournalUI','MainUI.Quests'],['journal','journal-chapters','journal-abandon','achievements','codex'],'当前 48 项任务、7 章主线、10 成就。NPC 接取/交付；手札追踪/取消追踪/放弃。章节门槛含等级、前置、故事、好感；职业考验需同一趟有效正式通关。放弃不撤回历史奖励或故事决定。','任务详情保留目标、进度、奖励、交付对象与追踪操作；成就达成后手动领取。'),
 ('NPC 服务、故事与成长',['TownNpcUI.cs','TownNpcUI.Quests','TownNpcUI.World'],['mentor','skill-assign','advancement','forge-weapon','forge-armor','forge-gems','gem-socket','crafting','reforge','reforge-review','npc-quests','npc-story','story-confirm'],'按 NPC 显示已实现服务。21 项技能：基础职业最高 3、进阶最高 5，学习后首个空槽自动装配。转职 Lv.10/300 金币且不可改分支。武器/防具强化最高 +5；宝石、13 配方、重铸均明确费用。5 段故事保留真实选择和好感。','技能直接选择十个位置；费用与结果同屏；故事先选择再确认。'),
 ('互斥专精',['TownNpcUI.Specialization'],['specialization','specialization-confirm','specialization-reset'],'4 个方向互斥、各 3 级；Lv.6/12/18，150/300/450 金币与 2/4/6 碎片。重置付 100，返已投金币 80%，材料不返。驭灵限御兽师已学召唤。','当前方向、下级变化、投入与重置返还同时可读。'),
 ('仓库存取',['TownNpcUI.Warehouse'],['warehouse-deposit','warehouse-withdraw','warehouse-review'],'每角色 96 格，保留实例、强化、宝石和锁定。锁定可存取，穿戴及方案引用不可存，待领取须先入包。批量数量每页 8 项核对；满容量/保存失败整笔不变。','先选存入/取出方向，再选物品、核对清单和两侧容量。'),
 ('补给与误售回购',['TownNpcUI.Supplies','TownNpcUI.Commerce'],['supplies','shop-purchase','shop-buyback','shop-buyback-review'],'购买 1–99，总价与容量明确；满包新堆叠转待领取。晶核碎片由委托或好感 20 开放，好感 40/70 分别 10%/15% 优惠并向上取整。回购 48 小时/最近 24 项，整条原记录、原实例、原价；需直接放入背包。','商店两页直接选购买/回购；购买数量和回购原记录分别确认。'),
 ('传送、远征与训练',['PortalUI'],['portal-forest','portal-crystal','portal-training','portal-timed','portal-trial','portal-expedition','portal-rush','portal-gauntlet','event-abandon'],'2 张正式地图与独立训练场，地图各自难度。6 种模式：探索、限时、首领试炼、远征、首领连战、千层。远征种子决定阵型/精英/事件/可选节点，重试和读档沿用组合，场景进度重置；组合事件奖仅一次。','地点、难度与模式各自选择，六个模式一眼可点；出发前显示规则。'),
 ('结算与长期挑战',['DungeonUI'],['victory','defeat','reward-save-failed','long-rest','long-pending','long-end-confirm','long-result'],'常规结算与长期挑战分开。首领连战 6 轮、千层最高 1000，每 5 层首领，每轮 10 分钟；提交奖励纪录后恢复四分之一生命能量。待保存阻止继续/离场，失败重开第 1 轮。','成功先看奖励，再拾取/重试/回城；长期挑战休整时直接继续或结束。'),
 ('新手引导',['MainUI.Tutorial'],['tutorial','tutorial-current','tutorial-review'],'14 步来自当前 TutorialTitles，保存角色进度，可跳过、回看、退出续接；按实际动作计数。','仅提示当前一步；完整步骤放入可翻页回看。'),
 ('设置、暂停与加载',['SettingsUI','SettingPage','PauseUI','LoadingUI'],['settings-screen','settings-sound','settings-controls','settings-game','rebind-active','pause','loading','loading-error','save-menu-failed'],'4 页设置：屏幕、声音、14 项改键/控制、6 项游戏选项。新增伤害数字与反馈强度已对齐。应用保存、关闭撤销未应用修改、恢复默认仅预览；重复/保留键拒绝，冲刺/翻滚共享。加载失败可重试或按上下文返回；保存失败不继续离场。','同页操作后应用；Esc 先关展开项或取消改键，返回目的地明确。'),
 ('共用布局与音效',['ProgressionUIWidgets','UiAudioFeedback'],[],'共用按钮、字体、遮罩与可视反馈支撑上述页面；已实现按钮音效与音乐/音效设置联动，没有新增独立音效业务页。','同类状态与按钮保持一致。')
]
ui=[r for r in source if '/UI/' in r['path']]
def source_match(r,t):
 stem=Path(r['path']).stem
 return stem==t.removesuffix('.cs') or stem.startswith(t+'.') or t=='SettingPage' and '/SettingPage/' in r['path']
coverage=[];entries=[]
for title,patterns,ids,rules,flow in families:
 files=[r for r in ui if any(source_match(r,t) for t in patterns)]
 entries.append({'feature':title,'rules':rules,'flow':flow,'pages':ids,'ui_files':[r['path'] for r in files]})
 for r in files:coverage.append({'path':r['path'],'feature':title,'pages':ids,'sha256':r['sha256'],'lines':r['lines']})
covered={r['path'] for r in coverage}
assert covered=={r['path'] for r in ui},'Unmapped UI files: '+str({r['path'] for r in ui}-covered)
assert {p for e in entries for p in e['pages']}=={s['id'] for s in game}
snapshot={'date':datetime.now().astimezone().isoformat(timespec='seconds'),'runtime_files':len(source),'runtime_lines':sum(r['lines'] for r in source),'ui_files':len(ui),'ui_lines':sum(r['lines'] for r in ui),'pages':len(game),'references':2,'configuration_counts':{k:len(v) for k,v in cfg.items()},'features':entries,'ui_coverage':coverage}
(P/'functional-matrix.json').write_text(json.dumps(snapshot,ensure_ascii=False,indent=2),encoding='utf8')
manifest=read('source-manifest.json');asset_checks=[]
for row in manifest['files']+manifest['derived_assets']:
 f=P/row['design_file'];actual=hashlib.sha256(f.read_bytes()).hexdigest()
 asset_checks.append({'file':row['design_file'],'expected_sha256':row['sha256'],'passed':actual==row['sha256']})
assert all(r['passed'] for r in asset_checks),'Asset mismatch'
(P/'asset-qa.json').write_text(json.dumps({'passed':True,'files':len(asset_checks),'checks':asset_checks},ensure_ascii=False,indent=2),encoding='utf8')
lines=['# 当前功能与 UI 设计对照 · Functional V5','',f"核对时间：{snapshot['date']}。当前工作区索引 **{len(source)} 个第一方运行时 C# 文件、{len(ui)} 个 UI 脚本**，共 {snapshot['runtime_lines']:,} 行；本稿包含 **{len(game)} 个页面与关键状态 + 2 张参考页**，画布 1920 × 1080。",'',
'本轮从 UI 的入口、事件处理、业务服务、保存边界和 Resources 配置核对玩家操作。全量脚本已建立方法、UI 文案、服务调用与哈希索引；重点阅读 UI 及其业务实现。这不是对第三方包和全部底层算法的逐行技术审计，也没有把规划清单当作已实现功能。','',
'- [打开设计画廊](index.html#inventory) · [关键页面总览](boards/hero-overview.png) · [全部页面总览](boards/all-pages-overview.png)','- [源文件索引](source-catalog.json) · [配置快照](configuration-catalog.json) · [逐功能与逐 UI 文件映射](functional-matrix.json)','',
'## 先解决背包的可理解性','',
'1. 左侧常驻“当前穿戴”：头部、护甲、靴子、戒指、项链，每行有部位、物品名、强化与空槽；其下列出两个武器槽及当前/备用。人物预览只辅助辨认。',
'2. 中间只做找物品：七类、搜索、四种双向排序、24 格物品、新/锁定/已装备标记；批量操作留在次级入口。',
'3. 右侧明确“将替换谁”：防具比较全身装备加成，计入强化、套装、实例词条和宝石；武器明确选槽 1 或 2。',
'4. 单件点选只试穿；当前清单和已生效属性不动，确认后更新。多件试穿累积部位，整套确认；Esc 先取消输入、确认、批量、试穿，再退出。',
'5. 示例用真实配置验算：晶辉护甲 +2 → 守望护甲 +1，全身生命 179 → 195、能量 30 → 15、伤害 9% → 9%、减伤 22% → 25%。护甲改善同时丢失晶辉四件能量 +15，不能只标“战力提升”。','',
'![当前穿戴](screens/08-inventory.png)','','## 当前功能、限制与操作路径','','| 功能 | 当前实现必须保留的规则 | 设计操作 | 页面示例 |','|---|---|---|---|']
by_id={s['id']:s for s in screens}
for e in entries:
 links='、'.join(f"[{by_id[p]['title']}](index.html#{p})" for p in e['pages'][:4]) or '全页共用'
 lines.append('| '+e['feature']+' | '+e['rules']+' | '+e['flow']+' | '+links+' |')
lines+=['','## 用当前配置校正的数值','',
'- 64 项物品、6 套装备、48 项任务、10 项成就、5 段故事、13 张配方、21 项技能；名称、目标、奖励与条件见配置快照。角色职业共 9 个，创建仅开放基础职业，进阶走导师。',
'- 技能升级费用按目标等级计算，例如震荡斩 2 → 3 级为 120 金币，等级要求 7；不是固定 40。',
'- 强化最高 +5：下一等级 ×100 金币；防具饰品另需下一等级 ×2 碎片。基础属性强化按总等级向上取整，减伤加成上限 50%。',
'- 宝石合成 150 金币 +3 晶核碎片，镶嵌/替换/拆卸 50 金币；生命晶石 +30 生命、凝神晶石 +15 能量、锋芒晶石 +3% 伤害、守护晶石 +2% 减伤。',
'- 守望护甲为史诗，重铸费用 325 金币 +8 碎片；已穿戴也可选择，锁定/普通/堆叠物品受限，重铸保留强化与宝石。林卫护甲回购示例按原出售所得 40 金币。',
'- 普通难度枚举为 2，远征组合事件额外奖励为 80×2，即 160 金币/160 经验。森林示例种子 284941 按 ExpeditionPlan 得到守卫据点、近战前锋、护盾/治疗、补给箱与精英伏击。','',
'## 阅读层级与呈现建议','',
'采用低多边形奇幻的刻面、薄金线和少量徽记。菜单/传送用场景构图，装备/仓库用便于检索的网格和清单，故事用大段阅读区，战斗 HUD 留出中央视野。不同任务使用不同版式，不给所有页面套同一张大卡片。',
'常用按钮直达；只有涉及消耗、删除、互斥或不可逆决定时才单独核对。规则放在相关按钮旁，常态不展开长篇说明。保持品质文字与颜色并用、空槽文字明确、禁用原因就近可见。','',
'## 资源与预览边界','',
'三个新包沿用已完成的导入复核：[PONETI、Synty HUD、Synty Menus 记录](RESOURCE_AUDIT.md)。本稿取用的原始图片、GUID 与哈希见 [source-manifest.json](source-manifest.json)。设计图标是选型建议，游戏 Icon 引用没有改变。',
'本轮额外按实际装备定义渲染四组剑士预览：当前、护甲试穿、武器试穿、空防具。Unity 使用隔离 PreviewRenderUtility 场景与现有 ModularArmorAppearance，StartScene 前后均为干净且未运行；详见 [渲染记录](model-render-verification.json)。HTML 使用预渲染代表组合，不是完整的实时三维换装系统。',
'部分 HUD 与传送底图为已导入 Synty 示例图，只用于看排版层级，不代表实际关卡或 Boss。图鉴部分沿用职业模型占位，正式内容可用现有敌人/NPC 模型补渲。中文正文字体仍使用系统 Microsoft YaHei 预览，未复制分发；落地需要可分发的中文字体与 TMP 字体资产。','',
'## 验证与工程边界','',
'- `qa-report.json`：逐张导出，检查图片加载、画布越界、面板溢出和底部按钮遮挡。',
'- `interaction-qa.json`：通过浏览器鼠标/键盘检查背包当前穿戴、单件试穿/取消/确认、双武器目标、低等级、待领取、中文搜索、B/Esc 层级、整套确认、批量保护与数量、模式直达、引导翻页等。',
'- `asset-qa.json`：核对 122 个复制资源与 10 个派生预览的输出哈希。',
'- 复杂交易、任务进度、存档故障等页面使用场景样例展示规则；HTML 不调用真实游戏服务，不代表全套业务可在浏览器执行。',
'- 本轮仅写入新设计目录及压缩包。没有修改游戏 C#、Assets、场景、预制体、项目设置或存档，没有运行游戏验收测试。',
'- 共享工作区在本轮期间有其他开发修改，新增设置和改键保护已重新核对；[变动记录](source-drift.json) 不属于本设计任务所做修改。后续实际落地应再次核对当时服务接口。','',
'## 43 个 UI 脚本覆盖','','| UI 源文件 | 对应功能 | 页面数 |','|---|---|---|']
for r in coverage:
 lines.append(f"| [{r['path']}](../../../{r['path']}) | {r['feature']} | {len(r['pages']) if r['pages'] else '共用组件'} |")
lines+=['','## 全部页面与关键状态','','| 序号 | 页面 | PNG |','|---|---|---|']
for s in screens:lines.append(f"| {s['number']:02} | [{s['title']}](index.html#{s['id']}) | [原尺寸](screens/{s['file']}) |")
lines+=['| 00 | [功能与交互规范](index.html#style-guide) | [原尺寸](screens/00-style-guide.png) |','']
(P/'FUNCTIONAL_AUDIT.md').write_text('\n'.join(lines),encoding='utf8')
readme='''# RPG-Silent · 功能匹配 UI 设计 V5

打开 [index.html](index.html#inventory) 查看。浏览器预览无需安装依赖，也不连接游戏与存档。

- [背包默认页](screens/08-inventory.png)：持续看见五个装备部位、空槽与两把武器。
- [关键页面总览](boards/hero-overview.png) / [全部页面总览](boards/all-pages-overview.png)。
- [功能、规则与全部页面对照](FUNCTIONAL_AUDIT.md)。

共 84 个页面与关键状态、2 张参考页，全部 1920 × 1080 PNG；11 张分组总览、1 张关键页总览、1 张全部页总览。

背包支持示例交互：分类、搜索、排序、选择试穿、取消与确认、双武器目标、多件试穿确认、数量核对和 Esc 分层返回。其他服务页主要供设计评审，示例按钮用于切换状态；不写入真实物品或进度。

设计从当前 UI、业务服务和配置核对：246 个第一方运行时源文件已索引，43 个 UI 文件逐项映射。详细证据和边界见功能对照；不是完整游戏技术审计或实机验收。

本轮只生成设计资料。中文正文仍为系统雅黑预览；部分底图为 Synty 示例图片。可分发中文字体、图鉴专用缩略图需要在落地时补齐。角色装备代表组合来自项目实际模型与装备定义。

`qa-report.json`、`interaction-qa.json`、`asset-qa.json` 记录设计预览检查。工程后续变化以当时业务接口为准。
'''
(P/'README.md').write_text(readme,encoding='utf8')
print(json.dumps({'features':len(entries),'ui_mapped':len(covered),'pages':len(game),'asset_hashes_passed':len(asset_checks)},ensure_ascii=False))
