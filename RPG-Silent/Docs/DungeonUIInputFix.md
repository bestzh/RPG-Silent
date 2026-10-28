# 水晶洞窟通关界面无法点击

2026-10-28，经 Unity MCP 直接检查并修复。保留用户当前正在运行的水晶洞窟通关现场，没有停止 Play Mode、重新挑战、回城或修改玩家存档。

## 现场与原因

现场为 `Dungeon02`，`DungeonRunState.Succeeded`，结算界面已显示；再次挑战、继续拾取、返回主城三个按钮均处于可交互状态，鼠标为可见／未锁定。Console 无异常，但 `EventSystem.current == null`，全部已加载对象中 EventSystem 和 BaseInputModule 的数量也都为 0。

项目采用每个场景各自持有 EventSystem 的方式。StartScene、Main、Dungeon01 已配置 `EventSystem + InputSystemUIInputModule`，Dungeon02 与 TrainingGround 没有。离开前一个场景时旧 EventSystem 被卸载，新场景未创建替代者；画面可以正常显示，按钮的鼠标指针、点击、滚轮和 UI 导航却无法派发。

不是结算按钮被禁用，也不是鼠标被锁定或界面被其他面板遮住。补回场景的 EventSystem 后，使用 Input System 鼠标状态事件经过 UI 输入模块派发，实际触发“继续拾取”，确认了失败机制。

## 修复

- 当前运行现场：通过 MCP 添加一个归当前 Dungeon02 场景所有的 EventSystem，绑定现有 Input System 默认 UI actions。离开该场景时正常销毁，不会遗留持久重复实例。
- 持久资产：Dungeon02、TrainingGround 各补入同样的 EventSystem、InputSystemUIInputModule 与既有 UI 输入引用。直接复用 StartScene 中已使用的配置，不新增资源包或改变输入系统。
- 因为用户正在副本内且仍有未拾取战利品，未保存运行时场景。对两份场景文件仅移植已有 EventSystem 的四个序列化对象块及根节点引用，再通过 Unity AssetDatabase 导入；未把临时掉落、玩家或运行中敌人写回场景。
- 没有修改战斗、结算、奖励、存档或 UI 按钮逻辑；没有改动主菜单、主城、森林场景及项目设置。

## 验证

`Tools/Validation/DungeonUIInputRuntime.cs.txt` 在原现场通过 14 项检查：

- 只有一个活动 EventSystem，Point／Click 动作启用。
- 三个结算按钮均为各自屏幕中心最上层的 UI 射线目标，没有遮挡。
- 三轮真实 Input System 鼠标按下／释放 → UI 输入模块 → “继续拾取”成功，游戏光标恢复锁定；键盘 ESC 重开同一结算，光标重新释放。此检查没有调用 `Button.onClick.Invoke` 或直接调用 `ContinueLooting` 作为点击替代。
- 原通关仍处于同一场景，地面 21 份战利品保留。验证中的临时鼠标、键盘和 InputSettings 副本已移除，后台运行及原输入设置恢复。

`python Tools/Validation/DungeonUIInputScenes.py` 检查五个正式 UI 场景：恰好一个 EventSystem 和 InputSystemUIInputModule、同属一个活动物体、组件启用、指针／点击／滚动／移动／确认／取消动作引用完整。修复前明确在 Dungeon02 与 TrainingGround 失败，修复后五个场景全部通过。`--repair` 只用于本次已知缺失配置的移植，修好后不重复运行。

证据：

- `Tools/Validation/DungeonUIBlockedBaseline.txt`
- `Tools/Validation/Captures/dungeon-ui-blocked-baseline.png`
- `Tools/Validation/DungeonUIInputRuntimeResults.txt`
- `Tools/Validation/DungeonUIInputSceneRepair.json`
- `Tools/Validation/DungeonUIInputSceneDiff.patch`

此前内容验证主要直接调用按钮回调，能够检验业务行为，但无法发现缺失 EventSystem 的问题。后续 UI 验收应保留真实指针输入和跨场景输入配置检查，不能仅靠按钮回调证明可点击。

为保留当前地面物品，本轮没有实际点击“再次挑战”或“返回主城”执行离开操作，也没有停止运行重进训练场；这些按钮的射线、可交互状态及场景资源配置已检查。没有重跑需要停止现场的 EditMode 全套测试，也没有重新打包 Windows 客户端。修复后 Console 查询仍为 0 错误、0 警告。
