# 玩家操控、动画资源与后续扩展

2026-10-27：主城 NPC、职业选择 / 转职、技能学习 / 升级、武器强化已经接入。当前配置和验证以 [TownProgression.md](TownProgression.md) 为准；下文的武器 / 技能资源数量和未来建议保留为前期记录。

日期：2026-10-27。用户确认保留“鼠标转身、WASD 前后左右移动”。

## 本次范围

已完善移动、相机、跳跃、翻滚和动作动画，接入主城安全区规则；随后完成首版武器装备、切换与存档。NPC 交互、技能学习、武器升级和联机组队仍属于后续功能。

## 控制结构

`PlayerInputActionsService → PlayerController / PlayerStateMachine → PlayerMotor → Rigidbody`

- InputAction 保留现有改键和菜单输入阻挡。
- `PlayerMotor` 是普通 C# 对象，只接收世界空间速度意图和物理步长；不读取设备、播放动画或结算伤害。`PlayerController.FixedUpdate` 负责地面查询、朝向与状态物理更新。
- `PlayerMovementSettings` 在玩家组件中序列化：加速 35、地面刹停 50、空中加速 18、空中减速 6；速度仍为步行 2.5 / 跑动 5 / 冲刺 8。冲刺要求前向输入。
- 玩家胶囊使用独立零摩擦、零弹性材质，由 motor 负责刹停，避免默认接触摩擦降低目标速度或在墙边粘住。碰撞几何和重力仍由物理系统处理。
- 脚底球形探测替换原先从角色原点向下 1.1 米的射线，忽略触发器和自身，限制可站立坡度。离地跳跃宽限 0.1 秒、跳跃输入缓存 0.12 秒。
- 二段跳可以在首跳上升时使用，首段最小间隔 0.08 秒；落地缓冲缩短为 0.12 秒。走下悬崖后超过宽限时间会消耗首跳，只保留姿态允许的空中跳跃次数。
- 翻滚方向在开始时固定；普通翻滚 0.8 秒、前扑 1.2 秒，与现有片段长度一致。翻滚结束回到移动/待机；被受击中断时取消自己的动画计时，避免重新进入旧攻击。
- 相机使用高度 1.4 米的独立跟随目标承接 yaw/pitch，角色胶囊保持竖直并完整入镜；显示鼠标光标时停止视角旋转。Cinemachine 启用遮挡回避，忽略 Player 标签。
- 移动动画根据实际水平速度平滑混合；保留原 Animator 状态名和参数。

## 主城与姿态

`PlayerRigSpawner.allowCombat` 是场景配置，Main 配置为 false，副本保持 true。不在控制器中比较场景名称。

- 主城使用放松动画，武器挂在背部，禁止普攻与当前战斗技能；忽略战斗伤害。可以调整装备或用 Tab 选择槽位，但不会拔出武器。
- `PlayerStanceController.CurrentStance` 保留可保存的选择；`EffectiveStance` 是当前区域规则下的实际表现。安全区切换不触发装备选择变更，不会把存档姿态覆盖成 Relax。
- 副本恢复已选姿态；死亡、结算、返回主城继续走现有会话与出生重置流程。
- 当前技能均为战斗技能，因此整体阻挡；未来治疗、社交、生活技能需要独立能力标签及区域策略。
- 玩家预制体的 Tab 已改为切换两个武器槽，由装备决定姿态；空槽使用 Unarmed。场景规则始终优先，不能通过切装备绕过主城禁战。

## 已接入的武器系统

- 新游戏默认配备训练单手剑与训练双手剑。左上方武器面板的“更换”在可用武器与徒手之间循环，“使用”选中该槽。Ctrl 显示鼠标，Tab 切换当前槽。
- `WeaponDefinition` 保存稳定 ID、显示名、姿态、三个普通攻击配置和模型的持握/收起挂点。定义在 `Assets/Resources/Weapons/`，独立攻击配置在 `Assets/Combat/Weapons/`；玩家实例与共享定义分离，后续强化数据不能直接写到定义资产。
- `PlayerWeaponEquipment` 保存两个装备槽与当前槽，提供 `TryEquip`、`TrySelectSlot`、`TrySwitchNext`。只能装备预制体可用列表中的资源；当前两把训练武器均为初始可用内容，尚无拾取、背包或购买流程。
- 换装只在落地且处于 Idle/Move 时允许；攻击、技能、跳跃、翻滚、受击、死亡、菜单输入阻挡期间拒绝切换。`RestoreEquipment` 是会话读档专用入口，先完整验证再应用。
- 单手剑使用 Armed 的八向移动，普攻替换为 Sword-Attack-R1/R2/R3；双手剑使用 2Hand-Sword 的待机、八向移动、跳跃、翻滚和 Attack1/2/3。单手剑允许冲刺，双手剑使用较稳的持剑跑动。
- 模型跟随 Humanoid 右手，主城移至胸骨背部挂点；空槽隐藏模型。单手剑与双手剑已分别配置从 Relax/Unarmed 拔剑、收剑至 Relax/Unarmed 共 8 个导入片段。武器模型碰撞关闭，实际伤害由 AttackExecutor 与 AttackProfile 统一结算。
- 战斗中切换按“旧武器收剑 → 提交装备 → 新武器拔剑”执行；卸下只收剑，徒手装备只拔剑，同一武器不重复播放。主城调整槽位保持收起状态。每段动画以 1.2 倍播放，姿态混合 0.12 秒、挂点混合 0.09 秒；背部位置按两种剑各自的伸手动作校准。
- `WeaponTransitionState` 占用全身动作，期间减速停止，禁止跳跃、翻滚、普通攻击、技能和再次切换。`WeaponSwitch` 动画事件只转移模型挂点；规范化时间提供事件缺失兜底，不能由事件改装备或结算伤害。旧武器收妥后才提交装备与存档；受击打断时保留已提交的选择。死亡、场景重置会清理切换动作。
- 场景禁战立即生效，动画在落地且 LoadingUI 释放输入后开始，避免加载遮罩吞掉动作；菜单阻挡输入时冻结拔收剑，恢复输入后续播。临时 AnimatorOverrideController 在收尾混合完成后释放，防止双手剑末帧突变为单手剑模板姿态。
- 主城从输入、技能释放和 AttackExecutor 上下文有效性三处阻止战斗。进入安全区还会使旧攻击上下文失效，避免残留延迟攻击。打开鼠标光标操作装备界面时，左键不会同时发起普通攻击。
- 进度格式升为 v2，保存两个稳定武器 ID 和当前槽。v1 在读取时迁移：Relax/Unarmed 保留徒手，持械姿态获得训练剑，双手剑姿态选中第二槽；成长和技能保持原值。成功恢复后提交 v2，并保留旧文件备份。未知版本、缺失武器资源和非法装备数据不会静默重置存档。

## 已检查的动画资产

通过 Unity AssetDatabase 读取实际导入片段，排除了 `__preview__`；文件数与片段数不同，一个 FBX 可以包含多个片段。以下数量按名称匹配 Walk/Run/Sprint/Strafe/Jump/Roll/Fall/Land/Idle，不代表每种武器均有完全相同的动作。

主要资源：`Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack/Animations/`。

| 动画组 | 移动相关片段数 | 接入建议/现状 |
| --- | ---: | --- |
| Relax | 40 | 主城、非战斗；已接入 |
| Unarmed | 46 | 徒手；已接入 |
| Armed | 56 | 通用持械、双持；已接入 |
| Armed-Shield | 27 | 单手武器配盾，可作为独立姿态 |
| 2Hand-Axe | 42 | 双手斧 |
| 2Hand-Bow | 41 | 双手弓 |
| 2Hand-Crossbow | 41 | 弩 |
| 2Hand-Shooting | 42 | 双手射击武器 |
| 2Hand-Spear | 42 | 双手矛 |
| 2Hand-Staff | 44 | 法杖 |
| 2Hand-Sword | 42 | 双手剑 |

上述移动片段均被 Unity 识别为 Humanoid。单手匕首、物品、锤、手枪、矛、剑各自目录有 6/8/6/11/7/14 个片段，但没有匹配上述移动命名的片段，应组合 Armed/Armed-Shield 的移动和武器专属攻击，不能只靠枚举项认定姿态已经接通。

其他可用 Humanoid 资源：

| 目录 | 总片段数 | 移动相关片段数 |
| --- | ---: | ---: |
| `Assets/MovementAnimsetPro` | 194 | 133 |
| `Assets/SwordAnimsetPro` | 124 | 50 |
| `Assets/Blink/Art/Animations` | 90 | 26 |

本次优先使用已经匹配玩家骨架的 RPG Character 动画，其他包保留为后续动作参考，未混入当前控制器。

### 本次修正的动画映射

- MoveTree：补全八向慢走与八向跑动，使用归一化方向坐标；冲刺节点从 `(0, 1.1)` 修正为 `(0, 2)`，与速度参数一致。
- Unarmed/Armed 的慢走替换为直立 Strafe，取消原来的 Crouch-Walk 误匹配；自动匹配编辑器也调整为优先精确 Strafe，防止下次生成时退回蹲走。
- RollTree：增加已有 DiveRoll-Forward1 片段，与向前移动时的 RollY=2 一致。
- JumpTree：普通起跳只使用普通跳跃；二段翻身继续由独立 Unarmed-Jump-Flip 状态控制。
- 保留现有控制器、OverrideController 的 GUID，以及攻击、死亡和动画事件链路。

## 后续实现边界

### 武器与成长

1. 首版 `WeaponDefinition`、装备槽和武器驱动姿态已接通；后续增加武器类型、技能使用限制、独立物品实例 ID、升级等级和词条。
2. 现有稳定武器 ID 用于查找共享内容；实例成长和获得/消费事务应由独立数据保存，继续通过装备组件切换姿态与攻击配置。
3. 单手剑与双手剑的闭环已完成，再按相同接口扩展其他武器、拾取与背包。
4. NPC 共用交互入口、距离检测与 UI 输入阻挡；技能导师和铁匠调用独立学习/升级用例，负责验证前置条件、消耗与存档事务，不让 UI 直接改玩家数值。
5. 技能学习区分“已解锁”与“已装配”；现有 `EquipSkill` 只解决装配。武器升级区分实例成长与公共基础配置。

### 联机组队副本

本次的输入与物理分工方便增加网络驱动，但不是已实现网络预测或同步，也不能保证 Rigidbody 确定性。

现有单机限制需要在联机阶段明确改造：

- `PlayerSessionService` 和 `PlayerRigSpawner` 目前只持有一个 rig；联机需要按玩家身份维护角色集合，只有本地拥有者读取输入、创建相机和驱动 HUD。
- `PlayerStats`、`PlayerSkillAccess`、进度服务目前是全局单例；联机角色属性、装备、技能、冷却必须按玩家/角色分离。
- 队伍副本的入场、敌人、伤害、冷却/能量校验、掉落和奖励应由主机或服务器裁定。客户端动画事件不能成为伤害权威来源。
- 稳定 ID 用于玩家、武器、技能和副本实例；同步移动意图与必要状态、姿态/动作 ID，避免直接同步所有 Animator 参数或客户端任意修改 HP/金币。
- 需要定义房主退出、断线重连、迟加入、队伍失败/复活、跨场景加载和奖励幂等规则。
- 建议在首个武器闭环之后尽早做“两名玩家进入同一副本、击败一个敌人、各自获得一次奖励”的小规模联机验证，再扩展大量 NPC 和成长内容。网络框架选择留待确认目标平台、人数、主机/专服模式后决定。

## 验证

### 拔剑、收剑与过渡（2026-10-27）

- 完整 EditMode 测试 **75/75 通过**，其中新增 9 项检查验证 8 个 Humanoid 非循环片段、动作中段挂点事件，以及各姿态共用的两个脚本控制状态。结果：`Tools/Validation/WeaponTransitionEditModeResults.json`。
- 隔离存档下 **49 项运行检查通过**：真实 Tab 输入、旧剑收妥后一次提交装备/存档、逐段实际片段与挂点、事件缺失兜底、菜单冻结续播、技能/攻击/跳跃/翻滚/重复切换限制、卸装与徒手装备、受击前后打断、快速切换安全区、死亡/重置，以及真实 Main → Dungeon01 → Main。末尾等待姿态混合结束并验证临时 Animator 控制器释放。脚本与结果：`Tools/Validation/WeaponTransitionRuntimeMcp.cs.txt`、`WeaponTransitionRuntimeResults.txt`。
- 资产更新脚本：先完成 `WeaponAuthoring.cs.txt` 的基础武器配置，再运行 `WeaponTransitionAuthoring.cs.txt` 绑定片段与校准挂点。动画直接引用原有导入资源，未修改第三方 FBX 或复制整套动画。
- 已查看最终拔收剑中段截图，并在运行中确认双手剑退出拔剑状态时保留对应片段参与混合；本次未构建独立 Windows 包。

### 武器接入（2026-10-27）

- `Assets/Tests/Editor/WeaponSystemTests.cs` 新增 13 项测试；完整编辑器测试 **66/66 通过**，覆盖武器资源/姿态/普攻引用、HUD 配置、空槽保存、非法数据拒绝、v1 迁移、缺失资源保护。结果见 `Tools/Validation/WeaponEditModeResults.json`。
- `Tools/Validation/WeaponRuntimeMcp.cs.txt` 在隔离存档下完成 **46 项运行检查**：真实 Tab/鼠标输入、界面换装/卸装、两种实际攻击动画与单次伤害、切换限制、主城技能/伤害阻挡、Main → 副本 → Main → StartScene → 继续游戏。结果见 `Tools/Validation/WeaponRuntimeResults.txt`。
- `Tools/Validation/WeaponAuthoring.cs.txt` 记录定向资产配置流程。已检查主城背剑、单手剑与双手剑姿态画面，修正 HUD 父节点与位置；截图位于 `Tools/Validation/Captures/weapons-*.png`。
- 验证后退出 Play Mode，清理临时设备和绑定、恢复后台设置并解除隔离存档路由；结束时 StartScene 无未保存改动，Console 无错误/警告。尚未验证独立打包程序或多人同步。

### 移动手感修复的前期记录

- 变更前运行时复现：无输入、已落地的 IdleState 仍保留水平速度 5；状态机没有派发 FixedUpdate。
- `Assets/Tests/Editor/PlayerControlTests.cs`：16 项针对性测试；完整 `RPGSilent.Tests.Editor` **53 项通过、0 失败、0 跳过**，已取得全部 53 项 Passed 记录。最终摘要见 `Tools/Validation/PlayerControlEditModeResults.json`。
- `Tools/Validation/PlayerLocomotionAuthoring.cs.txt`：可重复执行的定向动画资产更新脚本。
- `Tools/Validation/PlayerControlMcp.cs.txt`：最终预制体在隔离存档下完成真实输入、物理、动画与 Main → Dungeon01 → Main 回归，**38 项通过**；结果见 `Tools/Validation/PlayerControlResults.txt`。包含前扑片段与时长、死亡和主城姿态恢复。
- Unity 编译通过。首次全量测试摘要在域重载后丢失，随后切换 Play Mode 导致测试框架场景准备/恢复错误；已记录诊断并在 Edit Mode 独立重跑，最终取得完整的 53 项 Passed 结果。未修改测试框架或屏蔽异常。
- 测试结束已退出 Play Mode，移除临时输入设备、还原临时绑定和后台运行设置、清除隔离存档路由；真实玩家存档未用于这次验证。
- 手感的主观偏好、全地图坡道/台阶、多个帧率的实际渲染体验和多人网络表现仍需专门验收，不能由编辑器单元测试代替。
