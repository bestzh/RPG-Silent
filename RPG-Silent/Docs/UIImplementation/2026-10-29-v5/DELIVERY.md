# Functional V5 UI 交付

2026-10-29。设计范围为 `Docs/UIDesign/2026-10-29-functional-v5` 的 84 个游戏页面/关键状态；它们落在现有十个 UI 根及其子页面、弹窗与数据状态中。

## 已落地

- 十个根预制体：StartUI、MainUI、InventoryUI、TownNpcUI、QuestJournalUI、PortalUI、DungeonUI、SettingsUI、PauseUI、LoadingUI，位置保持 `Assets/Resources_moved/UI/`。
- 九个可编辑子页/条目/确认模板，位于 `Assets/Prefabs/UI/Templates/`；CanvasRoot 位于 `Assets/Prefabs/UI/`，已连接 StartScene。合计 20 个预制体。
- 十个页面共 1595 项 Scope / Id / Target 序列化绑定。组件按用途命名；更改显示节点名称不依赖字符串层级重找业务控件。
- 背包常驻五个防具饰品槽和两把武器，显示实际穿戴、强化、空槽和当前/备用；试穿与实际穿戴分开，确认前显示整套属性变化。
- 技能十槽直选、六种挑战模式直选；学习、进阶、专精、强化、宝石、买卖回购、批量操作、配装、仓库、任务、故事、图鉴、训练、远征、长期挑战和错误恢复连接当前业务服务。
- 85 项物品/技能图标使用已导入资源。中文使用 Noto Sans CJK SC，源字体与 OFL 同目录，TMP 预填 1237 字符。

## 框架与适配

保留 UIBase → UIManager → Addressables 和 VContainer 注入，未另建角色、背包、货币、任务或挑战系统。静态控件保存在预制体中；View 负责绑定、展示数据、切状态和复用条目模板。

分辨率适配保存在 Canvas Scaler（1920×1080 / Expand）、RectTransform 锚点/边距、Layout Group、Layout Element、Content Size Fitter 和 TMP 原生自动字号属性。移除了 View 按屏幕尺寸计算页面位置、大小或缩放的代码。3D 预览的 RenderTexture/相机比例、世界标记投影与滚动列表归零继续用于各自原有功能。

十个原根预制体的 GUID 及 UI_MainMenu Addressables 配置与应用前备份逐字节一致，见 `guid-addressable-audit.json`。完整路径、GUID、大小和 SHA-256 见 `prefab-manifest.json`。

## 验证结果

| 检查 | 结果 |
| --- | --- |
| Unity 编译 | 无编译错误 |
| 十个根预制体初始化、绑定、字体、命名、原生列表与 Canvas 契约 | 全部通过 |
| 连续两次重新生成 | 1595 项绑定保持稳定 |
| 最终 EditMode 全量测试 | 638 / 638；失败 0，跳过 0；57.6702566 秒 |
| 新增回归 | 八项预制体呈现约束，一项销毁对象关闭生命周期 |
| 原有页面身份与寻址 | 十个原 GUID 和 Addressables 配置保持一致 |
| 临时验证环境恢复 | 已退出 Play Mode；StartScene 未脏；存档覆盖目录恢复为空；后台运行恢复 false；Game View 选择恢复并移除四个临时尺寸 |

最终测试任务为 `00fd6638a064443c9983405811d2412f`，记录在 `editmode-638-final.json`。早期 636 项回归中的一次 Windows File.Replace 1175 已保留原始失败及独立重试证据，后续完整回归通过；未修改存档业务来迁就测试。

此前已在隔离存档中检查穿戴/试穿、交易、回购、仓库、导师、手札、设置、改键、传送、训练、普通结算及长期挑战保存失败恢复，详见 `RUNTIME_CHECKS.md`。五种 Game View 尺寸的背包边界与射线结果见 `resolution-inventory.json`。其中副本清敌使用现有 Enemy.Die 入口建立验证夹具，不代表手动战斗完成全部挑战。

本轮最终修正包含：目的地列表宽度约束、设置旧背景停用、结算与战斗目标栏层级、首领机制文字间距、引导入口监听。最终预制体检查和 638 项自动测试已覆盖应用后的资源与源码；按用户“不需要实机测试”的要求，取消本轮追加的运行复验与截图，不把之前截图当作最终版本逐页实机验收。未执行设备或玩家构建测试。

加载错误测试使用空白场景地址，产生一次预期的“场景地址不能为空”日志；返回游戏成功后已归档说明并清除该受控日志。保持传送页面打开退出运行，未再出现已销毁 UI 的 MissingReferenceException。

## 编辑入口

详细的页面清单、业务边界、组件命名和编辑说明见 `PREFAB_GUIDE.md`。可直接在 Unity 的 Prefab Mode 编辑；`Assets/Editor/UIV5` 只用于编辑器生成和检查，不进入玩家构建。重复执行作者工具会覆盖其负责的布局，手工微调应同步作者源。

隔离测试存档及已有截图保存在 `Builds/Validation/UIV5/`；正式玩家存档未用于本次测试。应用前 290 文件备份为 `Builds/Validation/UIV5/pre-apply-20261029-143856.zip`。未提交 Git，也未覆盖其他聊天的业务实现。
