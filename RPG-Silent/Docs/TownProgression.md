# 主城职业导师与武器强化

最新选角与外观：职业页已加入男女模型预览，基础职业及转职共 18 套外观，存档更新到 v4；旧模型继续用于兼容旧存档。详见 [CharacterCreation.md](CharacterCreation.md)。下文记录本次成长系统最初接入时的 v3 方案。

更新日期：2026-10-27。所有模型、动画、字体、图标、箭矢与法术特效均复用项目已有资源；没有下载或生成外部美术资源。

## 玩法

- 开始新游戏后先选职业，新角色只有普攻，十个技能槽全部为空。
- 剑士 → 剑圣 / 狂战士；法师 → 萨满 / 御兽师；游侠 → 神射手 / 刺客。共有 3 个基础职业、6 个进阶职业，使用 9 个现有 Polygon Fantasy Kingdom 人形模型。
- 主城出生点附近有剑士导师莱恩、法师导师伊芙、游侠导师林恩及铁匠布隆。面对 NPC、靠近至 2.8 米内按 E 交互；E 可在控制设置中改键。ESC 或关闭按钮结束交谈。离开范围、被遮挡、切换场景或角色死亡会关闭交谈。
- 导师只服务对应基础职业及其进阶分支。每个基础职业有 3 个技能，每个转职分支新增 2 个，共 21 个可学习技能。新技能自动装入第一个空槽；面板可选择 1～0 槽、装配或卸下技能。
- 基础技能学习等级为 1 / 3 / 5 级，费用为 50 / 85 / 120 金币。技能升级每级增加 20% 基础伤害；基础职业上限 3 级，进阶职业上限 5 级。费用、升级所需等级、冷却与能量消耗均在 SkillData 中配置。
- 10 级、300 金币可向原职业导师申请转职。先选择分支，再点击确认；一个角色只能选择一个进阶分支。转职保留已学技能、装备和强化，换模型、开放专属技能、提高全部攻击伤害 10%。
- 铁匠强化当前两个装备槽中的武器，最高 +5；每级提高 10% 普攻基础伤害。升到 +N 花费 100×N 金币，必定成功。同一武器定义共享强化等级，卸装后仍保留。
- 剑士初始持单手剑 / 双手剑，法师持法杖 / 单手剑，游侠持弓 / 单手剑。现有装备界面可继续更换四种训练武器。主城收起武器并禁止攻击和技能，副本恢复持武器动作。

## 内容与边界

- 职业：`Assets/Resources/Classes/*.asset`。PlayerClassDefinition 配置稳定 Id、导师所属基础职业、模型、初始武器及 Lessons。
- 技能：`Assets/Resources/ClassSkills/*.asset`；伤害配置位于 `Assets/Combat/ClassSkills/`。编辑器 GUID 用作技能存档 ID，发布后不要替换 .meta。
- NPC：`Assets/Prefabs/Town/`，Main 场景的 TownServices 下放置四个实例。
- 武器：`Assets/Resources/Weapons/`，新增 TrainingStaff / TrainingBow；使用现有 2Hand-Staff / 2Hand-Bow 动画及拔收动作。弓持于左手。
- 箭矢组合复用现有 SM_Prop_Arrow_01 模型；法术弹体复用项目 EnemyFlameBolt。它们通过 AttackContext 区分攻击者，不受资源名称影响。RangedMuzzle 避免从脚底发射，摄像机瞄准排除玩家自身碰撞体。
- UI：StartUI 内职业选择面板；Addressables `UI/TownNpcUI` 使用现有字体和 uGUI。没有额外 UI 包。
- 御兽师目前是兽灵法术分支，未实现可跟随、寻敌的宠物 AI。若要真实召唤兽，需要导入可用的兽类模型与 Idle / Walk / Run / Attack / Hit / Death 动画；建议先准备狼或豹一套。现有搜索结果中的动物装饰、皮毛和尸体不能代替战斗宠物。
- 联机组队、网络权威校验、角色多存档、背包物品实例与随机词条未纳入本次实现。当前是已有单机进度服务上的闭环。

## 生命周期与保存

PlayerProgressService 是成长记录的唯一所有者，PlayerStats 保留金币和总经验。学习、升级、转职和装配先构造完整候选快照，成功写盘后再扣款和应用。写盘失败不扣钱、不授予技能；旧等级报价防止重复点击连续购买。共享 SkillData / AttackProfile 不会因个人升级而修改。

存档升级到 v3，包含 classId、learnedSkills（技能 ID / 等级）、weaponUpgrades（武器 ID / 等级）。v1 / v2 只在内存中迁移，已有装备技能去重后视为已学 1 级，默认归为剑士；原来的跨职业技能仍可装配使用，新学技能受导师职业限制。继续游戏成功创建玩家后才写入迁移结果；缺失职业、技能或武器资源时保留原文件并阻止继续。

PlayerClassAppearance 复用持久玩家根对象与 Animator，只替换现有模型和 Avatar。输入、碰撞、相机与成长服务保持原来的所有者。换模型前清理武器展示，再恢复当前装备挂点。PlayerNpcInteraction 持有独立输入阻挡，异步 UI 加载失败后有超时释放；每次交易前重新验证 NPC、距离、视线和职业。

## 验证记录

- 最终编辑器测试 **93 / 93 通过**：`Tools/Validation/TownProgressionEditModeResults.json`，覆盖存档迁移、数据拒绝、职业及技能资源、初始空技能槽和可见弹体配置。
- 成长事务、导师限制、转职和恢复：`TownProgressionRuntimeMcp.cs.txt` / `TownProgressionRuntimeResults.txt`，40 项通过。
- 实际弓 / 法杖输入、弹体命中、强化伤害、主城限制、Main → Dungeon01 → Main → 菜单 → 继续游戏：`TownCombatRuntimeMcp.cs.txt` / `TownCombatRuntimeResults.txt`，17 项通过。
- 技能升级后的真实命中、能量、冷却及 NPC 视线遮挡：`TownSkillRuntimeMcp.cs.txt` / `TownSkillRuntimeResults.txt`，11 项通过。运行验证共 **68 项通过**。
- 截图位于 `Tools/Validation/Captures/town-*.png`。
- 内容作者脚本依次为 TownProgressionContentAuthoring、TownProgressionPrefabAuthoring、TownNpcSceneAuthoring（只用于首次添加场景 NPC）、TownPresentationAuthoring。均在 Tools/Validation 下，以 `.cs.txt` 保存，使用 Unity Editor API 创建资源和保持引用。
- 验证使用 `Temp/TownProgressionValidation-20261027` 隔离存档，未读取或替换真实玩家存档。未进行独立 Windows 构建或联机测试。
- 初次场景流程测试误用菜单加载的默认 HUD 参数，并在异步 HUD 尚未出现时断言，产生一次等待技能组件的警告；修正测试调用和等待条件后完整流程通过，初次诊断保存在 TownCombatInitialHarnessResult.txt。过程中还观察到现有动态字体图集的一次导入一致性警告；最终编译无错误，未屏蔽 Console 信息。
- 结束时退出 Play Mode、移除临时输入设备及绑定、恢复后台运行设置并清除隔离存档路由，StartScene 无未保存修改。
