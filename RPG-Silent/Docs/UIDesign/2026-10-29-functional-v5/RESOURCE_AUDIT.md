# 资源来源与待补内容

三个资源包已在前一轮检查本地文件及 Unity 数据库：PONETI 6000 Fantasy Icons 8474 张 Texture2D；Synty Fantasy Warrior HUD 261 个 Prefab；Synty Fantasy Menus 209 个 Prefab。原始检查为 2026-10-29，见项目内上一版 `2026-10-29-synty-v2/import-verification.json` 与 `RESOURCE_AUDIT.md`。本轮没有重复导入资源包。

V5 沿用这三个包、现有 Fantasy UI、Bamao 物品图与项目职业模型。八个新装备图标选自 PONETI；完整路径、GUID、用途和哈希见 [source-manifest.json](source-manifest.json)。图标选型没有改动游戏资源引用。

角色预览新增四个真实装备代表组合，通过隔离 Unity PreviewRenderUtility 场景渲染：当前、护甲试穿、武器试穿、空防具。没有修改当前场景、Assets 或预制体，前后干净状态见 [渲染验证](model-render-verification.json)。HTML 使用这些预渲染组合；不是完整实时三维预览。

当前仍需补齐：

- 可随游戏分发的中文正文字体及 TMP 字体资产。设计图只使用系统雅黑显示，没有复制系统字体文件。
- 图鉴专用 NPC/敌人缩略图。可从现有模型补渲，无需为此再买模型；当前部分使用职业图占位。

HUD 和部分传送底图为 Synty 示例图片，用于看 UI 层级，不代表实际关卡、角色或 Boss。所有可复制资源保持原字节，122 个复制资源与 10 个派生预览均已做输出哈希核对。
