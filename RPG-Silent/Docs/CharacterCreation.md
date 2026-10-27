# 职业选择与男女外观

2026-10-27。仅使用项目已有的 Polygon Fantasy Hero Characters、武器、动画和 LongCang 字体，没有下载或生成外部美术资源。

## 玩家流程

开始新游戏后打开全屏职业选择页。左侧选择剑士、法师或游侠，中间查看带待机动画和武器的 3D 角色，右侧显示定位、介绍、初始武器和 10 级转职方向。下方选择男 / 女，拖动角色或点击左右旋转按钮查看造型，滚轮缩放。点击“踏入主城”才创建角色；返回或 Escape 取消选择，不修改存档。已有存档的新游戏覆盖确认继续沿用原流程。

角色实际使用与预览相同的模型。性别随职业、成长和装备保存，转职后使用对应分支的同性别外观。开局仍只有普攻，主城收武器并禁止攻击。

## 资源配置

`Assets/Resources/Classes/` 的九个 PlayerClassDefinition 增加 MaleModelPrefab、FemaleModelPrefab、PreviewIdle。原 ModelPrefab 保留给旧存档和已有 NPC。

角色预制体位于 `Assets/Prefabs/PlayerAppearances/`，来源为 `Assets/PolygonFantasyHeroCharacters/Prefabs/Characters_Presets/Chr_FantasyHero_Preset_<编号>.prefab`：

| 职业 | 男预设 | 女预设 |
| --- | --- | --- |
| 剑士 | 13 | 5 |
| 法师 | 49 | 50 |
| 游侠 | 71 | 19 |
| 狂战士 | 38 | 16 |
| 剑圣 | 64 | 36 |
| 萨满 | 18 | 1 |
| 御兽师 | 25 | 98 |
| 刺客 | 4 | 59 |
| 神射手 | 76 | 100 |

每套预制体只保留当前服装和骨骼（约 133～139 个 Transform），避免把整套未启用衣柜带进每个角色实例。男女身体部件已检查一致，18 套外观均为有效 Humanoid。

CharacterRigProfile 负责模型骨骼单位与武器挂点换算。这批骨骼单位为厘米，武器配置为米，因此挂点位置和缩放乘以 100；剑类另有持握校准。预览和 PlayerWeaponEquipment 共用这些配置，拔收过程的挂点插值也使用转换后的目标。旧模型没有该组件，沿用原挂点。

## 生命周期与存档

- StartUI 保留菜单和新游戏确认的所有权，CharacterCreationView 只保存临时选择，确认后调用 PlayerSessionService.BeginNewCharacter。
- PlayerProgressService 保存 CurrentGender，创建、继续游戏及转职均通过 PlayerClassAppearance.ApplySelection 应用模型。
- 存档 v4 增加 gender：1 男、2 女；0 是仅为兼容保留的 Legacy 外观。v1～v3 读取时迁移为 0，精确保留原 ModelPrefab，不根据模型名称猜测性别。新建角色不能选择 Legacy。
- CharacterModelPreview 使用独立 URP 相机、RenderTexture 和无缩放时间的 PlayableGraph，不实例化玩家控制、伤害或存档组件。离开选择页会释放模型、图、相机、灯光和贴图。
- 第 31 层命名为 CharacterPreview，预览舞台位于可玩场景之外；相机只绘制此层。页面按 1600×900 设计区域等比适配，不修改全局 CanvasScaler。

## 验证与重建

资源作者脚本依次为 `Tools/Validation/CharacterAppearanceAuthoring.cs.txt`、`CharacterRigCalibration.cs.txt`。通过 Unity Editor API 保存预制体和职业引用，保留已有 GUID。前者会重建外观，之后须再运行挂点校准脚本。

编辑器测试与运行检查结果保存在 `Tools/Validation/CharacterCreationEditModeResults.json` 和 `CharacterCreationRuntimeResults.txt`。运行验证使用 `Temp/CharacterCreationRuntime-20261027` 隔离存档，不读写真实玩家进度。截图位于 `Tools/Validation/Captures/character-creation-*.png`。

- 最终编辑器测试 109 / 109 通过，覆盖原有回归与男女资源、存档读写和旧版本迁移。
- 选角运行流程 115 项通过：六种基础职业组合实际创建角色，六个转职方向保持性别并经真实菜单“继续游戏”恢复；反复关闭页面后无预览相机或 RenderTexture 残留；另检查 4:3 区域适配和拖动旋转。
- 新骨骼战斗回归 17 项通过，见 `CharacterCombatRuntimeResults.txt`：弓、法杖命中和伤害，拔收切换，Main → Dungeon01 → Main → 菜单 → 继续游戏。额外两项双手剑挂点检查见 `CharacterBladeMountResults.txt`。
- 后台编辑器过滤模拟设备输入，初次脚本没有触发攻击，记录保留在 `CharacterCombatInitialResults.txt`。最终战斗回归直接调用玩家同一个攻击回调，验证动作与实际弹体命中；这不等于本次验证了物理鼠标输入。项目输入设置未修改。
- 最终六种基础外观截图使用包含武器边界的取景；结束时退出自有 Play Mode，恢复后台运行设置和隔离存档路由。

未进行独立 Windows 构建。此页提供职业和性别选择，尚不包括捏脸、换发型或染色。
