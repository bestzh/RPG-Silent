# RPG-Silent 当前功能与项目上下文

<!-- unity-onboarding:generated:start -->

分析日期：2026-10-25（Asia/Shanghai）。项目根目录：`E:/project/RPG-Silent`。
参考提交：`839a3cdf3e74f60511c31600550e00b96700de23`；本次分析以包含未提交修改的工作区为准，而非仅以该提交为准。

## 结论与证据边界

项目是单机第三人称动作 RPG 原型，已建立开始菜单、场景加载、角色控制、战斗、技能、敌人、奖励、HUD 和副本入口的主要代码链路。现已接通副本结算、经验升级和本地进度保存；任务、背包、装备与金币消费仍待实现，尚不是完整 RPG 内容循环。

初次检查了脚本、ScriptableObject 配置、场景与预制体引用，以及已连接的 Unity Editor 状态。下文“已实现/已配置”表示存在代码与配置证据，不代表经过完整游玩验收。随后已修复命中特效、HUD 订阅、全局服务/玩家生命周期、输入统一、连击动画时序、攻击上下文/伤害释放、场景加载失败恢复、敌人战斗动作及玩家死亡动画，并接通副本目标、胜负结算、难度、重试/返回、经验升级和进度存档。针对性 MCP Play Mode 证据与未覆盖项见 `Docs/UnityProjectHealth.md`。

## 环境与框架（已确认）

- Unity `6000.0.66f2`，编辑器当前目标 `StandaloneWindows64`。
- URP `17.0.4`；GraphicsSettings 引用 `Assets/Settings/PC_RPAsset.asset`。
- Input System `1.17.0`；PlayerSettings 的输入配置仍为 Both。第一方玩家攻击、续段、视角和 Ctrl 光标切换现已统一到 InputAction，暂停/传送菜单持有各自的游戏输入阻挡，ESC 单独使用 UI 动作表。
- VContainer `1.16.6`、Addressables `2.8.0`、Cinemachine `3.1.3`、AI Navigation `2.0.9`、uGUI/TMP。
- 2026-10-26 用户安装 Behavior Designer Pro `3.3.3`、Graph Designer `3.3.4`、Opsive Shared `2.3.2` 嵌入包；敌人决策已迁移到 Pro。实际树资产在 `Assets/Combat/Enemies/BehaviorTrees`，不再运行自制 EnemyBrain。
- Test Framework `1.6.0` 已安装；未发现 `Assets/Scripts` 下的第一方测试或 asmdef/asmref。
- Unity MCP 已安装并可读取项目状态、项目路径和 Console。检查时停留在 `StartScene`，未运行、未编译，Console 错误/警告查询返回 0 条；这不构成运行或构建通过的证据。
- 包中存在 Multiplayer Center，但第一方代码未发现联机玩法实现，不能据此认定支持多人。

## 已实现的功能与当前配置

| 模块 | 当前能力 | 主要证据 |
| --- | --- | --- |
| 启动与菜单 | 新游戏覆盖确认、继续游戏、设置、退出；加载进度、失败重试/返回和输入恢复；继续游戏回到 Main | `GameStart.cs`、`UI/StartUI.cs`、`UI/LoadingUI.cs` |
| 角色移动 | 方向移动、步行、跑动、长按冲刺、短按翻滚、方向翻滚/前扑、跳跃、二段跳、空中移动、落地过渡 | `Player/PlayerController.cs`、`Player/States/` |
| 角色状态与动画 | Idle、Move、Jump、Roll、Attack、Hurt、Dead 状态；AnimatorOverrideController 切换姿态动画 | `Player/PlayerStateMachine.cs`、`Player/PlayerStanceController.cs` |
| 普通攻击 | 动画进度驱动攻击起手/释放/结束；续段输入窗口为 [0.35, 0.8]，有效请求直接触发动画衔接，0.95 开始统一结束过渡；按姿态和连击段读取攻击配置 | `Player/States/AttackState.cs`、`Combat/PlayerAttackLoadout.cs`、`Assets/Animator/PlayerAnim.controller` |
| 通用命中系统 | 近战扇形、Hitbox、射线/球形投射检测、飞行物、延迟范围伤害、自身范围、持续伤害区域；每次攻击独立上下文、一次释放、复合碰撞体去重，排除自身和无效目标 | `Combat/AttackContext.cs`、`AttackExecutor.cs`、`AttackProfile.cs`、`AttackProjectile.cs`、`AttackArea.cs` |
| 技能 | 10 槽装配接口、数字键触发、冷却、能量消耗与恢复、施法特效；吟唱后开始动画，事件/兜底仅结算一次；可中断技能被翻滚/跳跃/受击取消，死亡强制取消 | `Animation/PlayerSkillController.cs`、`SkillData.cs` |
| 敌人 | Behavior Designer Pro 可视化决策 + 动作状态机 + 技能配置；感知/导航/技能执行分离；近战、远程后退射击、两阶段 Boss；主城逐敌奖励，副本按波次统一结算 | `Player/EnemyController.cs`、`Enemies/BehaviorDesigner/`、`Combat/Enemies/BehaviorTrees/`、`World/DungeonEncounter.cs`；详见 `Docs/EnemyEncounter.md` |
| 玩家数据 | HP、能量、金币、累计经验、1–50 级成长；每级需当前等级 × 100 经验，生命上限 +10、能量上限 +5；奖励一次提交 | `Domain/Models/PlayerStats.cs`、`Application/` |
| HUD | 血条、能量条、等级/本级经验条、金币和累计经验；10 个技能图标、快捷键与冷却遮罩；HUD 显隐及存档错误提示 | `UI/MainUI.cs`、`UI/SkillSlotUI.cs`、`Resources_moved/UI/MainUI.prefab` |
| 游戏内菜单 | ESC 菜单、返回游戏、保存进度、设置、返回主菜单、退出；保存失败可重试并阻止退出丢失内存进度；禁用游戏动作输入 | `Manager/GamePauseService.cs`、`UI/PauseUI.cs` |
| 进度存档 | v1 单槽本地 JSON；金币、累计经验、姿态和 10 技能槽；自动/手动保存、临时文件替换、完整性校验、上次备份恢复、覆盖前归档 | `Manager/PlayerProgressService.cs`、`ProgressSaveStore.cs`、`Domain/Models/PlayerProgressData.cs` |
| 设置 | 分辨率、全屏、画质；主音量/音乐/音效/静音；控制参数和运行时改键；全局难度、HUD 显隐；保存/读取/恢复默认 | `UI/SettingPage/`、`Manager/*SettingsService.cs`、`PlayerInputActionsService.cs` |
| 副本入口 | 进入触发器弹出副本列表、介绍/背景、难度选择；记录进入上下文，加载场景，按配置定位玩家 | `World/PortalTrigger.cs`、`UI/PortalUI.cs`、`Data/`、`World/PlayerRigSpawner.cs` |
| 副本运行 | 清除进入时激活的全部敌人即通关；目标进度、一次性奖励、死亡失败、原难度重试/返回主城；加载失败保留原结算 | `Manager/DungeonRunService.cs`、`UI/DungeonUI.cs`、`Resources_moved/UI/DungeonUI.prefab` |
| 玩家会话 | 同一局跨场景保留 rig；返回菜单解绑存档监听并清玩家；新游戏重置，继续游戏创建新 rig、恢复成长/装配、清战斗临时状态；设置独立保留 | `DI/GameLifetimeScope.cs`、`Manager/PlayerSessionService.cs`、`World/PlayerRigSpawner.cs` |
| 编辑器工具 | 姿态动画覆盖匹配预览与写入；出生点物体与副本配置表双向同步 | `Editor/StanceOverrideAutoFiller.cs`、`Editor/DungeonSpawnAuthoringEditor.cs` |

上表中脚本的相对路径均以 `Assets/Scripts/` 为起点；资源路径以 `Assets/` 为起点。

### 当前姿态

`Assets/Animator/PlayerStanceDatabase.asset` 实配三种姿态，而非枚举中所有武器类型：

| 姿态 | 最大连击段数 | 翻滚 | 最大跳跃次数 | 冲刺 |
| --- | --- | --- | --- | --- |
| 放松（默认） | 0 | 否 | 2 | 是 |
| 无武装 | 4 | 是 | 2 | 是 |
| 战斗 | 2 | 是 | 2 | 否 |

普通攻击的多段动画和时序有实现，但当前 Loadout 主要只配置各姿态第 1 段，其他段可使用 fallback 攻击配置，不能理解成每段均有独立伤害配置。枪、弓、法杖已有攻击配置，但未加入当前三姿态循环。

### 当前装备技能

角色预制体前 6 槽已装备，后 4 槽为空。以下是资产配置值，不是实测平衡结果：

| 按键 | 名称 | 实际伤害方式 | 伤害 | 冷却（秒） | 能量消耗 |
| --- | --- | --- | --- | --- | --- |
| 1 | 裂空斩 | 近战扇形 | 35 | 3 | 12 |
| 2 | 火球术 | 飞行物 | 40 | 4 | 18 |
| 3 | 崩裂地火 | 前方范围伤害 | 45 | 6 | 25 |
| 4 | 霜冻新星 | 自身范围伤害 | 30 | 7 | 28 |
| 5 | 雷枪贯穿 | 射线单次命中 | 55 | 5 | 22 |
| 6 | 陨星坠击 | 前方范围伤害，带短延迟 | 70 | 10 | 40 |

证据：`Assets/Resources/RPG-Character.prefab`、`Assets/Resources/Skills/`、`Assets/Combat/AttackProfiles/Skill_*.asset`。
六个技能均配置为 `CastTime=0`、`TriggerTiming=OnCastStart`，且未绑定技能动画。名称中的“霜冻”“贯穿”不代表已实现冰冻状态或穿透多个目标。技能连锁、技能接续 Combo 目前只保留数据字段。

默认操作：WASD 移动，LeftAlt 步行，LeftShift 短按翻滚/长按冲刺，Space 跳跃，鼠标左键攻击，Tab 切换姿态，1–9/0 技能槽，ESC 菜单，Ctrl 切换鼠标光标。MCP 已验证改键后完整连击、默认鼠标攻击、菜单输入隔离及较晚续按的窗口边界；未验收所有设备与所有改键组合。

## 场景与实际接入

当前流程：`StartScene → StartUI → LoadingUI → Main → 传送门 → PortalUI → LoadingUI → Dungeon01 → 通关/失败 → 重新挑战或返回 Main`。

- `StartScene`：全局容器、入口脚本、UI/Input/场景加载及各设置服务；声音服务已绑定 AudioMixer。
- `Main`：SceneLifetimeScope、PlayerRigSpawner、portalId=101 的传送门，以及 1 个 Enemy 预制体实例。
- `Dungeon01`：SceneLifetimeScope、PlayerRigSpawner、出生点编辑组件和 DungeonEncounter；旧 5 个 Enemy 实例已停用，运行时第一波生成 2 近战 + 1 远程，第二波生成 1 两阶段 Boss，总计 4 个目标。
- `SampleScene`：存在资产，但未进入当前 Build Settings 或所检查的 Addressables 场景组。
- Build Settings 启用 StartScene、Main，Dungeon01 条目禁用；三个主流程场景均在 Addressables Scene 组登记，副本通过 Addressables 加载，不能仅凭 Build Settings 未勾选就断定无法进入。
- Addressables UI 组登记 StartUI、LoadingUI、MainUI、SettingsUI、PauseUI、PortalUI、DungeonUI 七个界面。
- 传送门 101 对应副本 1001“迷影森林”、1002“水晶洞窟”。两条记录均指向 `Scenes/Dungeon01`，出生位置不同；目前不是两个独立副本场景。
- 副本难度配置敌人生命/攻击/奖励倍率：简单 0.75/0.75/0.8，普通 1/1/1，困难 1.5/1.5/1.5，噩梦 2/2/2。全局难度另以 0.75/1/1.5 缩放玩家承伤，传送界面已说明叠加关系。等级字段尚未与成长系统接通。
- 进入副本、原难度重试和离开副本成功激活目的场景时，恢复 HP/能量、清冷却/施法/连击/旧攻击上下文、对齐位置与 yaw；保留 rig、姿态、技能装配和已获得金币/经验。返回主城清 DungeonLaunchContext；失败加载不提前重置。1002 出生 Y 从 144 修正到地面附近 -1.49。
- 主流程场景及已检查的玩家/敌人/UI 预制体未发现 CustomQuest 或第三方 SkillSystem 脚本接入，第一方代码也未发现对应任务调用。

## 尚未完成或尚未接通的部分

1. **成长与内容循环**：等级和升级属性已接通，但未实现背包、装备更换、商店、技能解锁、任务完成与结算。CustomQuest、SkillSystem 和大量美术/动画包的导入不能计作主游戏功能完成。
2. **存档扩展与发布验收**：H09 已保存成长/技能/姿态；尚无物品、任务、多个角色槽、云存档或已发布旧格式迁移。Editor 重启读档和损坏恢复已验证，独立 Windows 包与进程重开仍待 H10。
3. **主城死亡与恢复道具**：副本死亡已有失败重试/返回；主城死亡可返回菜单继续已保存进度。检查点原地重生、药品等尚未接通。
4. **副本地图扩展**：H11 已修复当前 5 个目标的陷地与两个入口不可达问题，100 项导航/实际行走检查通过；全地图任意位置、五敌同时围攻平衡和独立水晶洞窟仍未验收或实现。
5. **小地图**：MainUI 有 RawImage 和显隐控制，但预制体的纹理引用为空，未发现第一方实时地图渲染链路。
6. **亮度与震屏**：亮度设置能保存，但 ApplyBrightness 仅裁剪数值，未应用画面效果；震屏类及强度设置存在，未发现第一方战斗调用震屏。
7. **暂停含义**：当前菜单停用游戏 InputAction 并切换光标，没有冻结 Time.timeScale 或敌人更新；不等同于整个世界暂停。
8. **战斗扩展**：Hitbox 和持续区域等通用分支已有实现，但当前六个技能并未覆盖全部分支；连击、动画事件与改键行为仍需实际操作验证。
9. **敌人复杂度**：已实现近战、远程保持距离、共享攻击系统与半血两阶段 Boss，并接入 Behavior Designer Pro 可视化编辑与调试；仍无巡逻、多目标仇恨或多敌人协作。

## 结构与约定

- Dungeon01 使用独立 Dungeon Humanoid（1479372276，半径 0.5 / 高度 1.9 / 坡度 45° / 台阶 0.4）、PhysicsColliders 与 Height Mesh；主城仍用原 Humanoid。现有 NavMesh 资产 GUID 保留。1001 出生 `(13,1.878,-53)`、yaw 290；1002 出生 `(10,1.869,-50)`、yaw 285，覆盖旧 H08 的临时出生修正。更新地形/碰撞或敌人落点后应重烘焙并通过 `Tools/Validation/DungeonNavigationMcp.cs.txt` 验证。选择 Surface 时限定目标场景且要求 navMeshData 非空，PlayerRig 还含三个无数据的 Surface。
- 主要第一方代码在 `Assets/Scripts`，按 Domain/Application/Manager/Player/Combat/Animation/World/UI/DI/Editor 分工。
- `Domain` 定义模型/接口；`Application` 编排受伤、治疗、奖励；`Manager` 实现服务；`Player/Combat/Animation/World/UI` 承载运行时行为；`DI` 装配 VContainer。
- 当前 PlayerStats、奖励/受伤/治疗用例、PlayerProgressService 和技能 UI 桥接属于全局 Singleton；PlayerSessionService 持有玩家 rig，协调新游戏重置/继续游戏恢复，返回菜单先解绑保存监听再销毁玩家。场景 Scope 负责暂停、传送门和 DungeonRunService，卸载时清订阅、输入阻挡和副本 UI。GameLifetimeScope 在构建前拒绝重复根；GameStart 每次从存活根注入并打开菜单。旧 `ARCHITECTURE.md` 中“玩家数据属于场景 Scope”的描述已落后于代码。
- 进度保存在 `Application.persistentDataPath/Saves/`，格式 v1，主文件和备份均为完整事务。经验保留累计数值，等级/最大属性由其推导；临时 HP、能量、冷却、位置和副本中途状态不保存。SkillData.persistentId 使用资产 GUID，发布后保留 meta，新增存档字段需明确版本迁移；未知版本/缺失技能资源会拒绝读取并保留文件。新游戏旧文件归档到 archives。详见 Health 的 H09 与 `Tools/Validation/ProgressValidation.md`。
- MonoBehaviour + 状态机 + ScriptableObject 数据配置 + C# 事件是已确认模式。并非所有模块均严格隔离，领域技能接口仍涉及技能资产类型。
- 未配置第一方 asmdef；运行时代码主要进入 Assembly-CSharp，Editor 目录代码进入编辑器程序集。目录分层不代表编译边界已经隔离。
- Domain/Application 常用 `RPGSilent.Domain`、`RPGSilent.Application` 命名空间，其余多数类使用全局命名空间；常见 `_camelCase` 私有字段、`[SerializeField]` 配置、构造函数或 `[Inject]` 注入；异步加载主要使用回调/协程。
- 既有路线图、重构方案应作为历史或规划参考，功能判断优先采用当前代码与场景/预制体绑定。

## 验证边界与后续工作约束

- 已读取实际场景文件和预制体引用；场景实例数量是序列化引用数量，不是运行时观测。
- 已做命中特效、HUD 及 3 轮菜单/Main/Dungeon01/Main/菜单回归；未做性能测量、Windows 构建或完整资源依赖验证，不能将本次结果视为完整发布验收。
- 工作区存在用户未提交的代码、技能和资源修改；后续工作应保留这些修改。
- 初次检查仅新增说明文档；后续修复及 MCP 验证范围见 Health 文档。H06 定向修改 LoadingUI 预制体，未升级依赖。

关键来源：`ProjectSettings/ProjectVersion.txt`、`ProjectSettings/GraphicsSettings.asset`、`ProjectSettings/EditorBuildSettings.asset`、`Packages/manifest.json`、`Assets/Scenes/`、`Assets/Resources/`、`Assets/Resources_moved/UI/`、`Assets/Animator/PlayerStanceDatabase.asset`、`Assets/AddressableAssetsData/AssetGroups/`，以及上文列出的第一方脚本。

<!-- unity-onboarding:generated:end -->
