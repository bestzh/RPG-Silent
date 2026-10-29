# 风痕手札 V3 · 资源说明

本次使用与 V2 相同的 114 份现有素材副本，通过构图、材质组合、字体、色彩和细节调整增加个性，没有为设计图另购资源。

## 已确认的资源包

沿用 2026-10-29 在项目目录与 Unity AssetDatabase 中核实的结果；原始证据保留在 `import-verification.json`。本次仅调整设计图，没有重新执行导入。

| 包 | 项目目录 | 已核实内容 |
|---|---|---|
| PONETI 6000 Fantasy Icons 1.2 | `Assets/6000FantasyIcons` | 8474 PNG，Unity 识别 8474 Texture2D |
| Synty Fantasy Warrior HUD | `Assets/Synty/InterfaceFantasyWarriorHUD` | 1360 PNG、261 预制体、29 场景；Unity 识别 261 Prefab |
| Synty Fantasy Menus | `Assets/Synty/InterfaceFantasyMenus` | 529 PNG、209 预制体、35 场景；Unity 识别 209 Prefab |

文件的 meta 无缺失，`Assets/Synty/InterfaceCore` 公共依赖存在。数量包含示例和变体。此前数据库检查时编辑器空闲，控制台错误查询为 0 条；该证据不等同于逐个运行示例场景。

## 本版取用

- PONETI：9 个职业的 21 个技能各使用不同图标。名称、解锁条件、能量、冷却和等级上限沿用当前项目技能数据。
- Synty Menus 与 Warrior HUD：纸页、徽章、HUD 槽位、地图与导航装饰、Ortica 拉丁标题字体。
- 原项目：LongCang 中文手写字体、背景、职业模型预览、Bamao 物品图标。
- 构图装饰：曲线、圆形轨道、缎带和封蜡由设计预览绘制，属于视觉组合；并未生成新的 Unity 资源。

所有素材副本的原始字节保持不变；`source-manifest.json` 记录源路径、GUID、SHA-256 和原有九宫格信息。`skill-icon-map.json` 是设计选图建议，游戏实际图标引用未更改。

## 仍需补齐或替换的内容

| 内容 | 状态 |
|---|---|
| 可随游戏分发的中文正文字体 | 仍需补齐。设计正文使用系统 Microsoft YaHei 预览，未复制该字体。标题已使用项目现有 LongCang |
| NPC 与敌人专用头像、图鉴缩略图 | 可用项目已有模型继续渲染；当前部分图鉴使用职业模型占位，无需为此购买新模型 |
| 游戏实际关卡与 Boss 的干净截图 | 当前 HUD 与部分传送页使用 Synty 示例场景图说明排版，正式落地时需替换为游戏画面 |
| 专属品牌印记 | 当前封蜡与标记用于方向评审，可在确定风格后再细化 |

这些缺口不影响本次设计图评审。本次没有新增外部资源，也未改动游戏文件。
