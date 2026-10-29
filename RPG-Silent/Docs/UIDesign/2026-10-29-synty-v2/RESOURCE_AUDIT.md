# 资源复核与新版取用说明

检查日期：2026-10-29。项目：`E:/project/RPG-Silent`，Unity 6000.0.66f2。

## 三个资源包

| 包 | 导入目录 | 本地文件检查 | Unity 数据库检查 |
|---|---|---|---|
| PONETI 6000 Fantasy Icons 1.2 | `Assets/6000FantasyIcons` | 8474 PNG、1 个示例场景；文件的 meta 无缺失 | 8474 Texture2D |
| Synty Fantasy Warrior HUD | `Assets/Synty/InterfaceFantasyWarriorHUD` | 1360 PNG、261 个预制体、29 个示例场景；文件的 meta 无缺失 | 261 Prefab |
| Synty Fantasy Menus | `Assets/Synty/InterfaceFantasyMenus` | 529 PNG、209 个预制体、35 个示例场景；文件的 meta 无缺失 | 209 Prefab |

数量按实际目录统计，包含示例与变体，不等同于商店宣传中的组件数量。`Assets/Synty/InterfaceCore` 公共资源也存在。

检查时编辑器没有进行编译或资源刷新，控制台错误查询返回 0 条。上述验证确认资源已导入和可被资源数据库识别，不等同于逐个示例场景的运行验证。

## 设计资源分工

- Fantasy Menus：共用面板边框、主要与次要按钮、金色徽章、分割线、拉丁标题字体和设置/导航图标。
- Fantasy Warrior HUD：技能槽边框、人物状态区、地图外框、首领信息装饰、导航及武器图标。
- PONETI：21 个技能各自对应不同图标，涵盖项目 9 个职业；名称、解锁级别、能量、冷却与等级上限来自当前 `Assets/Resources/ClassSkills`。
- 已有美术：项目的 5 张 UI 背景、Bamao 物品图标和职业模型预览继续复用。HUD 与部分传送卡片使用新包里自带的干净示例图片作场景底图，仅展示 UI 与画面的层级关系，不表示实际关卡、人物或 Boss。

素材按原始字节复制到设计目录；`source-manifest.json` 可追踪源路径、GUID、哈希及九宫格切分参数。技能图标为设计建议，游戏中现有 `Icon` 引用未更改。

## 已补齐与剩余内容

| 内容 | 当前状态 |
|---|---|
| 统一奇幻面板、菜单、弹窗、按钮 | 已由 Synty Menus 提供 |
| 统一 HUD、槽位、地图、状态装饰 | 已由 Synty Warrior HUD 提供 |
| 21 个可区分的职业技能图标 | 已选择并记录独立图标 |
| 可读且可随游戏分发的中文正文字体 | 仍需补齐；设计图使用系统 Microsoft YaHei 预览，未复制该系统字体 |
| NPC/敌人专用头像与图鉴缩略图 | 可由项目现有模型渲染，无须为此购买新模型；当前部分图鉴使用职业模型占位 |
| SILENT 专用品牌徽章 | 当前用已导入的盾徽、羽翼与交叉剑组合，供视觉方向评审 |

正式落地时可选择思源黑体等可分发的中文字体，并制作 TMP 字体资产。该项未阻止本次 PNG 设计图交付。
