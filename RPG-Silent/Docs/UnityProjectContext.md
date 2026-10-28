# RPG-Silent 当前功能与项目上下文

<!-- unity-onboarding:generated:start -->

2026-10-28 主城生活更新：传送门移到出生点右侧约 8 米；增加 3 位委托 NPC、10 位沿街活动的居民与巡逻者，主城共 17 位人物。原有任务系统扩展至 19 项（每职业显示 15 项），包括主线、支线、职业和特殊委托；E 接取／提交，J 按类别与状态筛选，头顶／地图显示可接可交标记。对话、宝箱、隐藏支路、精英、限时与首领试炼目标接入真实事件和原子领奖，沿用 v10 存档。367 项 EditMode、132 项运行检查、41 项资源预检通过；仅使用现有资源，主城继续禁战，不接联机，未重新构建 Windows 包。详见 [TownLifeAndQuests.md](TownLifeAndQuests.md)。

2026-10-28 职业与探索更新：剑士破防、游侠弱点、法师元素联动、御兽师兽灵助战已接入实际伤害链。双副本增加宝箱、机关门、隐藏支路与可选精英事件；传送页可选限时挑战 / 首领试炼，结算统计时间、承伤、精准格挡与闪避并发放可重试保存的挑战奖励。独立训练场支持连段 / 技能及防御 / 闪避练习，HUD 增加生命能量数值、冷却秒数、附近目标和职业教学。主城继续禁战，开局仍只有普攻。353 项 EditMode、204 项运行检查、48 项资源预检通过；修复森林地面层判定和兽灵跟随状态抖动。继续使用现有资源、不接联机，未重新生成 Windows 包；范围、验证、警告与恢复记录见 [AdventureSystems.md](AdventureSystems.md)。

分析日期：2026-10-25（Asia/Shanghai）。项目根目录：`E:/project/RPG-Silent`。
参考提交：`839a3cdf3e74f60511c31600550e00b96700de23`；本次分析以包含未提交修改的工作区为准，而非仅以该提交为准。

## 结论与证据边界

2026-10-28 玩家受击与武器差异化更新：四类武器的男女方向受击、重击、击退、硬直恢复保护和方向/镜头反馈已接入。单手剑快连段与闪避追击、重剑按住蓄力、弓蓄力强化箭、法杖三段魔弹及末段命中回能完成；主城禁战规则不变。复用现有资源，328 项 EditMode 通过；运行检查 374 项、双副本 45 项、修正弓子动画后复测 110 项、重复防御和死亡恢复 43 项通过。资源、输入边界、初次失败与未复现记录见 [ImpactWeaponCombat.md](ImpactWeaponCombat.md)。本轮未重新打包 Windows 程序，未加入联机。

2026-10-28 目标锁定与防御更新：中键锁定、滚轮 / 方括号切换目标，锁定时朝向敌人并平滑调整偏肩镜头；遮挡、距离、死亡和主城规则自动解除。右键格挡、精准格挡、翻滚无敌 / 精准闪避与单次反击加成已接入现有能量与伤害系统，四种武器防御 / 受击 / 破防共 12 个现有 Humanoid 动作适配男女。森林密集碰撞体漏选问题改用敌人生命周期集合解决。307 项 EditMode 与 224 项运行检查通过，Windows64 Mono / Direct3D11 最终包 0 错误 / 警告、30 秒独立启动通过，真实存档哈希不变。最终包 `Builds/Validation/20261028-lock-defense-final`；功能、时序、恢复记录及边界见 [CombatFlow.md](CombatFlow.md)。未加入联机或外部资源。

2026-10-28 战斗衔接更新：新增 0.3 秒普攻 / 技能输入缓存，按命中窗口续段，21 个职业技能接入六种现有动作与定时释放；六种敌人加入实际伤害、闪色、音效和局部受击停顿。森林 / 晶窟 Boss 保留半血阶段，增加 25% 终阶段连招、HUD 提示、反击窗口和脱战重置。269 项 EditMode、337 项运行检查通过；新 Windows 验证包 `Builds/Validation/20261028-combat-flow` 构建零错误 / 警告、30 秒独立启动通过。范围、历史验证异常与资源边界见 [CombatFlow.md](CombatFlow.md)。联机继续不做。

2026-10-28 最终本地回归：本聊天范围内的本地功能已完成，详见 [LocalCompletionPlan.md](LocalCompletionPlan.md)。最终 257/257 EditMode、54 项“选角色 → 双副本 → 手动拾取 / 换装 → 导师任务 → 铁匠强化 → 保存切换角色”检查通过；Windows64 Mono / Direct3D11 构建 0 错误 / 0 警告，独立程序 30 秒启动通过。产物在 `Builds/Validation/20261028-final-local`，未覆盖原构建或真实存档。清理与证据边界见 [FinalLocalValidation.md](FinalLocalValidation.md)；下方“剩余 / 下一阶段”描述为各阶段历史。服务端联机按用户要求跳过。

2026-10-28 多角色更新：开始菜单支持创建 / 选择角色，分页列表预览已穿戴防具与武器。新角色在各自 GUID 目录保存完整进度，旧单角色文件保留原位置并独立继续；创建和切换有保存失败保护。仍使用 v10 数据格式，257 项 EditMode、211 项运行检查（含重新进入 Play Mode）通过，详见 [CharacterSaves.md](CharacterSaves.md)。当前仅余最终本地集成回归及发布边界确认，服务端联机继续跳过。

2026-10-28 战斗补充更新：铁匠战斗补给、四种药品手动拾取和 Q/R/F 快捷栏、护体 / 灼烧 / 迟缓及状态 HUD、单机死亡重试 / 回城已接通。v10 保存药品绑定与共享冷却截止时间，兼容旧档；写盘失败不消耗药品、不离开结算。247 项 EditMode 和 282 项运行检查通过，详见 [CombatSupport.md](CombatSupport.md)。当前剩余为多角色存档与最终本地回归，服务端联机继续跳过。

2026-10-28 任务更新：主城导师与铁匠增加任务页，J 打开手札，HUD 追踪当前目标；首批 10 个任务覆盖森林、洞窟、材料交付、技能学习和六个转职分支。奖励需回 NPC 提交，保存失败不扣材料、不重复发奖，满包进入待领取。v9 保存任务与追踪并兼容 v1–v8，235 项 EditMode 和 408 项运行检查通过，详见 [Quests.md](Quests.md)。下一阶段为药品 / 状态 / 单机复活补充、多角色存档与最终本地回归；服务端联机按用户要求跳过。

2026-10-27 背包管理更新：实例可锁定防误出售/分解；背包支持批量分解、五槽与两武器整套试穿、3 个配装方案和一键换装。保存失败或任一部件失效时不部分提交，缺件方案不阻止角色继续游戏。v8 保存锁定与方案，兼容旧档，详见 [InventoryManagement.md](InventoryManagement.md)。

2026-10-27 宝石与套装更新：铁匠“宝石工坊”可用晶核碎片和金币制作四种宝石，五槽防具支持独立镶嵌、替换、返还；原有六套防具按 2/4/5 件激活加成。背包对比含宝石和套装变化。v7 保存实例宝石并兼容旧档，详见 [EquipmentGrowth.md](EquipmentGrowth.md)。本聊天待完成的本地功能范围见 [LocalCompletionPlan.md](LocalCompletionPlan.md)。

2026-10-27 防具强化更新：铁匠新增“防具饰品强化”页，背包内五槽装备均可逐件升到 +5，消耗金币和已有锻造碎片；基础属性每级 +10% 并向上取整，随机词条不变。背包显示等级、强化贡献和总属性对比。沿用 v6 的实例强化字段，详见 [ArmorUpgrades.md](ArmorUpgrades.md)。

2026-10-27 装备实例更新：装备逐件占格并保存独立词条和武器强化，背包试穿/确认/出售按实例操作，备用武器词条不叠加。新掉落白绿蓝紫金红对应 0/1/2/3/4/4 条词条；旧装备保留基础值与强化，超出容量的数量进入待领取。v6 兼容旧档，细节与验证见 [EquipmentInstances.md](EquipmentInstances.md)。下文共享强化、固定品质属性及 v5 为历史记录。

2026-10-27 装备试穿更新：点选背包装备可预览单件替换效果，确认才正式装备；取消、分类切换与关闭背包恢复实际穿戴。男女共用现有换装规则，低等级装备可预览，武器可预览对应姿态，试穿不修改属性和存档。验证见 [InventoryTryOn.md](InventoryTryOn.md)。

2026-10-27 背包预览更新：背包增加独立 3D 角色预览，实时同步已穿戴防具、职业、性别与当前武器姿态，支持左键拖动、滚轮缩放和复位。复用现有选角预览和模块化换装资源，未改变存档格式。详细验证见 [InventoryPreview.md](InventoryPreview.md)。

2026-10-27 防具外观更新：头盔、护甲、靴子已接入六档男女模块化换装，三个槽可独立混搭；卸下恢复职业默认部位，转职及继续游戏保留穿戴。九职业男女共 18 套骨骼绑定已验证，戒指和项链仍为属性饰品；v5 存档版本不变。资源、旧存档兼容与验证见 [ArmorAppearance.md](ArmorAppearance.md)。

2026-10-27 背包与掉落更新：B 打开背包，五个防具饰品槽提供属性，两武器槽仅能装备已拥有的武器。副本怪物生成地面战利品，金币/经验吸附至附近最近玩家，装备与材料需要靠近按交互键拾取；六档品质为白、绿、蓝、紫、金、红，地面光环/光柱与名称统一着色。通关后可继续拾取，满包及保存失败保留掉落。v5 保存背包与穿戴，兼容旧存档。范围、56 种物品和验证证据见 [InventoryAndLoot.md](InventoryAndLoot.md)。下文“无背包”、旧版自动结算奖励与 v2–v4 存档说明属于历史记录。

2026-10-27 选角页更新：全屏三栏界面加入动态 3D 职业预览、男女切换和旋转缩放。三种基础职业及六个转职分支共 18 套男女外观复用现有模块化角色资源；v4 保存性别并沿用至转职，旧存档保留原模型。实现与验证见 [CharacterCreation.md](CharacterCreation.md)，下文 v3 与旧版选角布局描述属于此前记录。

2026-10-27 主城成长更新：开始新游戏先选择剑士 / 法师 / 游侠，开局只有普攻；三位职业导师教授 21 个职业及分支技能，铁匠支持武器 +1～+5。技能可升级，10 级开放剑圣 / 狂战士、萨满 / 御兽师、神射手 / 刺客六个分支；使用项目已有的九个人形模型、法杖 / 弓及动画。v3 存档保存职业、已学技能等级和武器强化，兼容旧存档装备技能。具体配置、范围和验证见 [TownProgression.md](TownProgression.md)。下文较早的技能默认装配、无金币消费和 v2 存档描述属于历史记录。

2026-10-27 玩家操控更新：保留鼠标转身与 WASD 平移；物理移动交由 `PlayerMotor` 固定步长驱动，修复停止滑动、脚底检测、上下视角、跳跃输入缓存和翻滚中断。移动动画补全八向并纠正蹲走误映射；Main 的 `PlayerRigSpawner.allowCombat=false` 禁止战斗并呈现 Relax，保留所选战斗姿态供副本恢复。当前操控、动画清单及武器/NPC/联机扩展边界以 [PlayerControlAndExpansion.md](PlayerControlAndExpansion.md) 为准；下文早期验证和主城战斗描述属于历史记录。

项目是单机第三人称动作 RPG 原型，已接通职业选角、角色控制、战斗技能、两个副本、地面掉落、背包装备与预览、铁匠成长、导师技能转职、任务循环和本地保存。当前剩余范围以 `LocalCompletionPlan.md` 为准，下方较早的功能表保留历史背景，版本与完成状态以上述更新为准。

初次检查了脚本、ScriptableObject 配置、场景与预制体引用，以及已连接的 Unity Editor 状态。下文“已实现/已配置”表示存在代码与配置证据，不代表经过完整游玩验收。随后已修复命中特效、HUD 订阅、全局服务/玩家生命周期、输入统一、连击动画时序、攻击上下文/伤害释放、场景加载失败恢复、敌人战斗动作及玩家死亡动画，并接通副本目标、胜负结算、难度、重试/返回、经验升级和进度存档。针对性 MCP Play Mode 证据与未覆盖项见 `Docs/UnityProjectHealth.md`。

## 环境与框架（已确认）

- Unity `6000.0.66f2`，编辑器当前目标 `StandaloneWindows64`。
- URP `17.0.4`；GraphicsSettings 引用 `Assets/Settings/PC_RPAsset.asset`。
- Input System `1.17.0`；PlayerSettings 的输入配置仍为 Both。第一方玩家攻击、续段、视角和 Ctrl 光标切换现已统一到 InputAction，暂停/传送菜单持有各自的游戏输入阻挡，ESC 单独使用 UI 动作表。
- VContainer `1.16.6`、Addressables `2.8.0`、Cinemachine `3.1.3`、AI Navigation `2.0.9`、uGUI/TMP。
- 2026-10-26 用户安装 Behavior Designer Pro `3.3.3`、Graph Designer `3.3.4`、Opsive Shared `2.3.2` 嵌入包；敌人决策已迁移到 Pro。实际树资产在 `Assets/Combat/Enemies/BehaviorTrees`，不再运行自制 EnemyBrain。
- 2026-10-27 敌人增加平滑转向后起手、锁定攻击方向、贴地渐亮预警和独立恢复时间。Boss 第二阶段通过 Pro 新分支启动可配置三连击，连招结束有 1.8 秒反击窗口、恢复期承伤 ×1.25；距离脱离/间隙硬直会取消后续段落。运行时由动作状态机管理，数据在 `EnemyAttackSequence` 及独立技能/命中配置，详见 `Docs/EnemyEncounter.md`。
- 2026-10-27 当前 Dungeon01 普通怪接入每波独立的 `EnemyCombatGroup`：共享附近警戒、近战包围站位、远程避开队友射线，以及近战/远程各一个攻击名额。`EnemyAttackBudget` 负责排队、公平轮换、错峰起手和短暂间隔；Boss 独立运行。参数保存在 DungeonEncounter.GroupCombat，Pro 近战/远程树增加 CoordinateEnemyPosition 节点。
- 2026-10-27 第二关「水晶洞窟」已保存为独立 `Dungeon02` 场景，副本 1002 通过 `Scenes/Dungeon02` 加载。使用现有石桥、岩壁、水晶、角色与特效资源，三波共六名敌人：窄角突刺枪卫、地面持续领域祭司及两阶段晶核守望者；独立 Pro 树、技能和导航资产位于 `Assets/Combat/CrystalCavern` 与 `Assets/Scenes/Dungeon02`。配置和验证证据见 `Docs/EnemyEncounter.md` 最新章节。
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
| 进度存档 | v2 单槽本地 JSON；金币、累计经验、姿态、10 技能槽、2 武器槽与当前槽；兼容 v1 迁移、自动/手动保存、原子替换、校验、备份恢复、覆盖前归档 | `Manager/PlayerProgressService.cs`、`ProgressSaveStore.cs`、`Domain/Models/PlayerProgressData.cs` |
| 武器装备 | 训练单手剑/双手剑、两个可更换槽、徒手、Tab 切换；自动匹配姿态与三段普攻，主城背剑禁战，副本恢复 | `Player/WeaponDefinition.cs`、`Player/PlayerWeaponEquipment.cs`、`UI/WeaponEquipmentUI.cs` |
| 设置 | 分辨率、全屏、画质；主音量/音乐/音效/静音；控制参数和运行时改键；全局难度、HUD 显隐；保存/读取/恢复默认 | `UI/SettingPage/`、`Manager/*SettingsService.cs`、`PlayerInputActionsService.cs` |
| 副本入口 | 进入触发器弹出副本列表、介绍/背景、难度选择；记录进入上下文，加载场景，按配置定位玩家 | `World/PortalTrigger.cs`、`UI/PortalUI.cs`、`Data/`、`World/PlayerRigSpawner.cs` |
| 副本运行 | 清除进入时激活的全部敌人即通关；目标进度、一次性奖励、死亡失败、原难度重试/返回主城；加载失败保留原结算 | `Manager/DungeonRunService.cs`、`UI/DungeonUI.cs`、`Resources_moved/UI/DungeonUI.prefab` |
| 玩家会话 | 同一局跨场景保留 rig；返回菜单解绑存档监听并清玩家；新游戏重置，继续游戏创建新 rig、恢复成长/装配、清战斗临时状态；设置独立保留 | `DI/GameLifetimeScope.cs`、`Manager/PlayerSessionService.cs`、`World/PlayerRigSpawner.cs` |
| 编辑器工具 | 姿态动画覆盖匹配预览与写入；出生点物体与副本配置表双向同步 | `Editor/StanceOverrideAutoFiller.cs`、`Editor/DungeonSpawnAuthoringEditor.cs` |

上表中脚本的相对路径均以 `Assets/Scripts/` 为起点；资源路径以 `Assets/` 为起点。

### 当前姿态

`Assets/Animator/PlayerStanceDatabase.asset` 在原有三种姿态基础上接入 OneHandSword 与 TwoHandSword（均三段普攻）。武器组件驱动姿态，不再让玩家任意轮换；主城强制 Relax。原型姿态配置如下：

| 姿态 | 最大连击段数 | 翻滚 | 最大跳跃次数 | 冲刺 |
| --- | --- | --- | --- | --- |
| 放松（默认） | 0 | 否 | 2 | 是 |
| 无武装 | 4 | 是 | 2 | 是 |
| 战斗 | 2 | 是 | 2 | 否 |

两把训练剑均配置三段独立攻击资源，并由 PlayerAttackLoadout 优先读取当前武器。徒手仍沿用原有姿态 Loadout 和 fallback；枪、弓、法杖尚未作为可装备武器接入。

武器切换已接入专属拔收剑动画：WeaponDefinition 各有 Relax/Unarmed 的拔剑与收剑片段，PlayerWeaponEquipment 驱动 WeaponTransitionState，收旧武器完成后提交装备和存档，再拔新武器。动作期间停止移动并阻挡攻击/技能/跳跃/翻滚/重复切换；动画事件只负责挂点展示，规范化时间兜底。主城禁战立即生效，进出场景的可见动作等待加载释放输入后播放；受击、死亡和重置可安全打断。详见 `Docs/PlayerControlAndExpansion.md` 和 `Tools/Validation/WeaponTransition*`。

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

默认操作：WASD 移动，LeftAlt 步行，LeftShift 短按翻滚/长按冲刺，Space 跳跃，鼠标左键攻击，Tab 切换武器槽，1–9/0 技能槽，ESC 菜单，Ctrl 切换鼠标光标。左上方武器面板支持更换与选用槽位。MCP 已验证改键后完整连击、默认鼠标攻击、菜单输入隔离及较晚续按的窗口边界；未验收所有设备与所有改键组合。

## 场景与实际接入

当前流程：`StartScene → StartUI → LoadingUI → Main → 传送门 → PortalUI → LoadingUI → Dungeon01 / Dungeon02 → 通关/失败 → 重新挑战或返回 Main`。

- `StartScene`：全局容器、入口脚本、UI/Input/场景加载及各设置服务；声音服务已绑定 AudioMixer。
- `Main`：SceneLifetimeScope、PlayerRigSpawner、portalId=101 的传送门，以及 1 个 Enemy 预制体实例。
- `Dungeon01`：SceneLifetimeScope、PlayerRigSpawner、出生点编辑组件和 DungeonEncounter；旧 5 个 Enemy 实例已停用，运行时第一波生成 2 近战 + 1 远程，第二波生成 1 两阶段 Boss，总计 4 个目标。
- `Dungeon02`：独立水晶洞窟场景与导航，使用现有岩壁、水晶、石板和石桥资源。三波为 2 枪卫 + 1 地面领域祭司、1 枪卫 + 1 祭司、晶核守望者，共 6 个目标；专属敌人配置、六种技能、三份 Pro 树在 `Assets/Combat/CrystalCavern/`。
- `SampleScene`：存在资产，但未进入当前 Build Settings 或所检查的 Addressables 场景组。
- Build Settings 启用 StartScene、Main，Dungeon01 条目禁用；StartScene、Main、Dungeon01、Dungeon02 均在 Addressables Scene 组登记，副本通过 Addressables 加载，第二关不需要另加启用的 Build Settings 条目。
- Addressables UI 组登记 StartUI、LoadingUI、MainUI、SettingsUI、PauseUI、PortalUI、DungeonUI 七个界面。
- 传送门 101 对应副本 1001“迷影森林”和 1002“水晶洞窟”，分别加载 `Scenes/Dungeon01` 与 `Scenes/Dungeon02`。第二关通过 Addressables Scene 组登记，开放普通/困难/噩梦，出生位置 `(0,0.05,-23)`、朝向正 Z。
- 副本难度配置敌人生命/攻击/奖励倍率：简单 0.75/0.75/0.8，普通 1/1/1，困难 1.5/1.5/1.5，噩梦 2/2/2。全局难度另以 0.75/1/1.5 缩放玩家承伤，传送界面已说明叠加关系。等级字段尚未与成长系统接通。
- 进入副本、原难度重试和离开副本成功激活目的场景时，恢复 HP/能量、清冷却/施法/连击/旧攻击上下文、对齐位置与 yaw；保留 rig、姿态、武器/技能装配和已获得金币/经验。返回主城清 DungeonLaunchContext；失败加载不提前重置。1002 已使用水晶洞窟的独立入口，不再使用森林场景的旧出生点。
- 主流程场景及已检查的玩家/敌人/UI 预制体未发现 CustomQuest 或第三方 SkillSystem 脚本接入，第一方代码也未发现对应任务调用。

## 尚未完成或尚未接通的部分

1. **成长与内容循环**：等级、升级属性及训练武器装备切换已接通，但未实现物品背包、武器获取/强化、商店、技能解锁、任务完成与结算。CustomQuest、SkillSystem 和大量美术/动画包的导入不能计作主游戏功能完成。
2. **存档扩展与发布验收**：已保存成长/技能/武器装配，包含 v1 → v2 迁移；尚无物品实例、任务、多个角色槽或云存档。Editor 重启读档和损坏恢复已验证，独立 Windows 包与进程重开仍待 H10。
3. **主城死亡与恢复道具**：副本死亡已有失败重试/返回；主城死亡可返回菜单继续已保存进度。检查点原地重生、药品等尚未接通。
4. **副本地图扩展**：H11 的 100 项导航/实际行走检查属于旧森林配置。现已有独立水晶洞窟及专属三波遭遇；最新关卡验证记录见 `Docs/EnemyEncounter.md`。全地图任意位置的碰撞检查、大量敌人压力测试和人工平衡验收仍未完成。
5. **小地图**：MainUI 有 RawImage 和显隐控制，但预制体的纹理引用为空，未发现第一方实时地图渲染链路。
6. **亮度与震屏**：亮度设置能保存，但 ApplyBrightness 仅裁剪数值，未应用画面效果；震屏类及强度设置存在，未发现第一方战斗调用震屏。
7. **暂停含义**：当前菜单停用游戏 InputAction 并切换光标，没有冻结 Time.timeScale 或敌人更新；不等同于整个世界暂停。
8. **战斗扩展**：Hitbox 和持续区域等通用分支已有实现，但当前六个技能并未覆盖全部分支；连击、动画事件与改键行为仍需实际操作验证。
9. **敌人复杂度**：已实现近战、远程保持距离、共享攻击系统与半血两阶段 Boss，并接入 Behavior Designer Pro 可视化编辑与调试；Boss 有三段连招和反击窗口，普通怪有同波警戒、包围站位与错峰出手。仍无巡逻、多目标仇恨或联机 AI 权限同步，战斗参数仍需人工试玩调整。

## 结构与约定

- Dungeon01 使用独立 Dungeon Humanoid（1479372276，半径 0.5 / 高度 1.9 / 坡度 45° / 台阶 0.4）、PhysicsColliders 与 Height Mesh；主城仍用原 Humanoid。森林 NavMesh 资产 GUID 保留，1001 出生 `(13,1.878,-53)`、yaw 290。Dungeon02 使用相同 agent type 和 PhysicsColliders、独立 NavMesh 资产；1002 出生 `(0,0.05,-23)`、yaw 0。更新地形/碰撞或敌人落点后应重烘焙；森林验证用 `Tools/Validation/DungeonNavigationMcp.cs.txt`，洞窟用 `Dungeon02AssetsMcp.cs.txt` 和 `Dungeon02TraversalMcp.cs.txt`。选择 Surface 时限定目标场景且要求 navMeshData 非空，PlayerRig 还含三个无数据的 Surface。
- 主要第一方代码在 `Assets/Scripts`，按 Domain/Application/Manager/Player/Combat/Animation/World/UI/DI/Editor 分工。
- `Domain` 定义模型/接口；`Application` 编排受伤、治疗、奖励；`Manager` 实现服务；`Player/Combat/Animation/World/UI` 承载运行时行为；`DI` 装配 VContainer。
- 当前 PlayerStats、奖励/受伤/治疗用例、PlayerProgressService 和技能 UI 桥接属于全局 Singleton；PlayerSessionService 持有玩家 rig，协调新游戏重置/继续游戏恢复，返回菜单先解绑保存监听再销毁玩家。场景 Scope 负责暂停、传送门和 DungeonRunService，卸载时清订阅、输入阻挡和副本 UI。GameLifetimeScope 在构建前拒绝重复根；GameStart 每次从存活根注入并打开菜单。旧 `ARCHITECTURE.md` 中“玩家数据属于场景 Scope”的描述已落后于代码。
- 进度保存在 `Application.persistentDataPath/Saves/`，当前格式 v2，兼容 v1 迁移，主文件和备份均为完整事务。经验保留累计数值，等级/最大属性由其推导；临时 HP、能量、冷却、位置和副本中途状态不保存。SkillData.persistentId 使用资产 GUID，武器使用 WeaponDefinition.PersistentId；发布后保持 ID 和 meta 稳定。未知版本、缺失技能/武器资源会拒绝读取并保留文件。新游戏旧文件归档到 archives。武器验证见 `Tools/Validation/WeaponEditModeResults.json` 与 `WeaponRuntimeResults.txt`；早期验证详见 Health 的 H09。
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
