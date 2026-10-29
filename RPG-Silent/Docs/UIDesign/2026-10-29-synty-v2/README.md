# RPG-Silent 全界面设计稿 · Synty V2

2026-10-29。40 张页面与关键状态设计图，加 2 张设计规范图；每张 1920 × 1080。

## 浏览与下载

- `index.html`：离线设计画廊，支持目录、前后翻页、键盘方向键及原图链接。
- `screens/`：42 张原尺寸 PNG，其中 `00-style-guide.png` 为视觉规范，`41-skills-visual-guide.png` 为全部 21 个技能的图标建议。
- `boards/hero-overview.png`：6 个代表页面。
- `boards/all-pages-overview.png`：40 个游戏页面与状态总览。
- `boards/board-1.png` 至 `board-5.png`：每组 8 张，方便逐组评审。
- `boards/reference-overview.png`：视觉与技能规范总览。
- `RESOURCE_AUDIT.md`：资源来源、已补齐内容与剩余字体缺口。
- `import-verification.json`：Unity 资源数据库识别结果。
- `source-manifest.json`：素材路径、GUID、SHA-256 与原始九宫格边界。
- `skill-icon-map.json`：项目当前 21 个技能与 PONETI 图标的对照建议。

## 新版方向

深夜蓝面板、月白正文与徽章金焦点，统一开始菜单、角色页、行囊、手札、NPC 服务、传送、设置、加载与结算。Synty Menus 的边框、按钮与徽章建立共用组件；Warrior HUD 的槽位、地图外框、人物状态与导航符号用于战斗信息层；PONETI 提供独立技能图标。

保留项目已有背景、角色模型预览和部分适合的物品图标。新版不需要引入额外的 UI 框架。字体、图标和边框均从现有项目资源中取用；中文正文使用系统雅黑作为设计预览。

## 覆盖范围

覆盖当前项目 10 个 UI 根预制体和 2 个运行时角色页面：

1. StartUI：开始菜单；CharacterCreationView、CharacterSelectionView：创建及选择角色。
2. MainUI：主城、Boss 战、训练场、操作教学与世界反馈。
3. InventoryUI：背包、批量分解、待领取、药品快捷槽、配装方案、丢弃确认。
4. QuestJournalUI：任务、成就、图鉴。
5. TownNpcUI：导师、转职、武器与防具强化、宝石制作与镶嵌、任务、故事选择、补给、配方、重铸。
6. PortalUI：森林、晶窟、训练场；DungeonUI：胜利和死亡结算。
7. PauseUI、SettingsUI：暂停及屏幕、声音、控制、游戏设置。
8. LoadingUI：加载与失败恢复。

`screen-manifest.json` 保留各页面对应的功能与源码依据。

## 检查与范围

三个资源包的目录和资源数据库均已检查。8474 张 PONETI 图标、261 个 HUD 预制体、209 个 Menus 预制体被 Unity 识别；检查时未在控制台读取到错误。

`qa-report.json` 记录全部 42 张画板的图片解码、1920 × 1080 尺寸、页面边界与素材使用情况。`gallery-check.json` 验证目录切换、职业与性别选择、弹窗以及素材副本哈希。

本次仅编写、渲染和打包 `Docs/UIDesign/2026-10-29-synty-v2/` 内的设计文件。设计预览中的购买、强化、学习、保存等按钮仅展示视觉；尚未修改游戏脚本、场景、预制体或游戏中的技能图标引用。示例存档与数值不代表实际角色进度。

HUD 与部分传送卡片的场景底图使用已导入 Synty 包中的干净示例图片，避免旧截图中烘焙的文字影响评审；它们是排版示意，不表示当前实际关卡、人物或 Boss。角色创建与装备预览仍使用此前从项目实际职业模型渲染的图片。图鉴部分角色剪影沿用职业模型，正式内容可用现有 NPC/敌人模型重新渲染。
