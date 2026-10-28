# 防具外观换装

2026-10-27。复用项目已有 Polygon Fantasy Hero Characters 模块化网格、骨骼、材质和动画，未下载资源或新增插件。

## 玩家行为

在 B 背包中穿戴头盔、护甲或靴子后，角色立刻显示对应部件。三个槽可混搭，不要求整套装备；卸下某个位置只恢复该部位的职业默认外观。戒指、项链仍提供属性，没有独立佩戴模型。

旅人、林卫、晶辉、守望、王权、赤曜六档装备各配置头盔、护甲、靴子，共 18 件可见防具、36 套男/女部位配置。造型采用不同网格并配以对应的衣料/金属颜色；护甲负责躯干、手臂、手套与腰部，靴子负责腿部。现有套件没有把裤腿与鞋独立拆分，所以换靴子也会改变对应腿部造型。封闭头盔隐藏原头部、头发和胡须，卸下后恢复。

三个基础职业和六个转职职业的男女模型均适配。换装保留当前骨骼、Animator 和武器挂点；转职更换职业基础模型后重新绑定已穿戴部件。主城禁战、拔收武器和副本切换沿用现有规则。

外观由 v5 存档已有的装备 ID 和性别重建，不新增第二份外观状态，不改变存档版本。写盘失败时不更换外观。未选择性别的旧存档继续保留原整体模型；首次穿戴可见防具时使用职业配置的兼容模块化模型，卸下全部可见防具后恢复原模型，存档的 Legacy 标记保持不变。兼容模型在 `PlayerClassDefinition.LegacyArmorGender` 显式配置。

## 实现与资源

- `ItemDefinition.Appearance` 指向 `Assets/Resources/ArmorAppearances/` 中的 `ArmorAppearanceDefinition`。配置保存男女网格、共享材质、骨骼相对路径、剔除包围盒、遮挡部位和衣料颜色。
- `CharacterRigProfile.ArmorMeshRoot / ArmorBodyParts` 标注默认服装的部位。18 个玩家外观预制体增加这些数据，未修改第三方原始模型、场景或动画控制器。
- `PlayerClassAppearance.ApplyLoadout` 统一处理职业/性别及穿戴。`ModularArmorAppearance` 只创建当前穿戴的 SkinnedMeshRenderer，把骨骼绑定到当前角色；它不加载完整衣柜、不增加 Animator、不逐帧重建模型。
- 衣料/金属颜色通过现有角色 Shader 的 MaterialPropertyBlock 设置，保持原肤色，不克隆或修改共享材质。重复应用相同装备复用渲染对象；移除或换职业时释放旧部件。
- `PlayerProgressService` 在创建/读档、转职及成功的背包事务后同步外观。属性、耐久存档与外观沿用同一个装备列表。

`Tools/Validation/ArmorAppearanceAuthoring.cs.txt` 是可重复执行的 Unity Editor 配置脚本。如果先重建职业外观，应按 CharacterAppearanceAuthoring → CharacterRigCalibration → ArmorAppearanceAuthoring 的顺序执行，最后一步恢复换装部位绑定。

## 验证

`ArmorAppearanceEditModeResults.json` 记录 142/142 测试通过。新增检查覆盖九职业 × 两性别 × 六档整套外观、骨骼归属、原身体隐藏、混搭、卸装恢复、重复应用不重建以及共享材质不被修改。

运行检查共 576 项通过，使用 `Temp/ArmorAppearanceRuntime-20261027` 隔离存档，详细结果见 `Tools/Validation/ArmorAppearanceRuntimeResults.txt`。覆盖三基础职业 × 两性别的六档穿戴、真实背包按钮、混搭、保存失败不变装、转职、进入副本后攻击蒙皮形变、武器切换、返回主城、真实菜单继续游戏，以及两类 Legacy 模型的换装和恢复。脚本通过现有背包按钮和玩家服务/事件驱动流程，并非真人完整键鼠游玩。未进行独立 Windows 打包或联网验证。

初始测试有两处验证假设需修正：一个合法网格直接绑定 `Root` 骨骼；卸下增加生命上限的头盔时，当前生命可以向下钳制而不能增加。初次记录保留在 `ArmorAppearanceInitialTests.json` 与 `ArmorAppearanceInitialRuntimeResults.txt`，修正测试后重跑通过，没有为这些测试失败更改产品规则。

`Tools/Validation/Captures/armor-six-tiers-final.png` 为现有放松待机动画下的造型对照：上排男、下排女，从左到右为旅人、林卫、晶辉、守望、王权、赤曜。截图用临时预览场景生成，结束后释放模型、动画图、贴图和灯光。
