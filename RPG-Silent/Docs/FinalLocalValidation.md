# 本地功能最终验证

日期：2026-10-28，Windows，Unity 6000.0.66f2，当前包含未提交修改的工作区。范围以 `LocalCompletionPlan.md` 为准；服务端及联机按用户要求跳过。

## 验收依据

| 检查 | 结果 | 证据 |
| --- | --- | --- |
| 当前 EditMode 全量回归 | 257/257，通过；失败 0、跳过 0，5.997 秒 | `Tools/Validation/FinalLocalEditModeResults.json`，job `73108b2e81464e4bb3b07aab4be109df` |
| 关键预制体及场景入口 | 567 个组件无丢失脚本 / 失效的序列化对象引用；四个场景存在且 Addressables 地址有效 | `Tools/Validation/FinalLocalPreflight.json` |
| 最终连贯玩法流程 | 54 项运行检查通过 | `Tools/Validation/FinalLocalRuntime.cs.txt`、`FinalLocalRuntimeResults.txt` |
| 界面与外观 | 检查 1920×1080 强化背包截图，属性文字溢出检查通过；已有各阶段男女模型检查仍有效 | `Tools/Validation/Captures/final-local-enhanced-armor.png`，各阶段文档 |
| Windows64 Mono / D3D11 | 构建成功，0 错误、0 警告；30 秒独立启动通过 | `Tools/Validation/FinalLocalBuildResults.json`，输出目录内 `player-startup.json` / `.log` |

预制体扫描对象为玩家和开始菜单、主界面、副本结算、NPC、背包、任务手札及暂停界面；不将其描述为整个第三方资源库的全面扫描。Build Settings 保留 StartScene、Main，两个副本沿用 Addressables 加载。

## 最终玩法流程

使用已建立的隔离存档副本 `Temp/FinalLocalRuntime-20261028`，从选择角色进入女性萨满：

1. 确认主城收起武器并禁止攻击，保留已接取的森林任务。
2. 进入森林副本，等待拔武器完成，推进真实敌人波次及首领结算；敌人击杀事件与通关事件将任务推进到可提交。
3. 靠近防具，确认不会自动收入背包；通过临时测试键盘的 E 输入拾取独立实例，再用 B 打开背包，验证试穿不改变实际数据、取消可恢复、确认才装备。
4. 靠近金币 / 经验掉落，确认无须 E 即可吸附获得；回主城恢复收武器姿态并清理状态。
5. 通过职业导师实际任务按钮提交，验证金币 / 经验数量及不可重复领取，再接取水晶洞窟任务。
6. 完成洞窟三波六个敌人，再次验证手动拾取、试穿 / 装备、货币吸附及导师提交。
7. 通过铁匠“防具饰品强化”按钮强化刚拾取的防具，核对独立 +1、金币和材料精确扣除，背包显示强化贡献。
8. 保存，切换到男性狂战士并再切回萨满，逐一比较完整成长记录；另一个角色的存档文件在本轮副本、奖励和强化后保持逐字节不变。

这条流程通过脚本对真实敌人施加致死伤害并补血，旨在验证事件、场景、掉落与持久化联动。它不等同于真人通关、攻击手感或数值平衡测试。药品输入、死亡 / 重试、写盘失败、六种基础职业性别组合及六个转职分支已在对应阶段覆盖。

## 已完成的本地范围

装备实例与随机词条、防具强化、宝石及套装、装备锁定和批量分解、多件试穿和三套配装、10 个主线 / 支线 / 职业任务、药品与快捷使用、状态效果和单机复活、多角色独立存档与选择预览均已接通。资源继续使用本项目已有模型、动画、图标、字体及材质。

各阶段操作和验证细节见 `ArmorUpgrades.md`、`EquipmentInstances.md`、`EquipmentGrowth.md`、`InventoryManagement.md`、`Quests.md`、`CombatSupport.md`、`CharacterSaves.md`。当前数据格式为 v10，仍支持原有单角色记录和旧版本迁移。

## 已知边界

- 尚无服务端，不包含房间、邀请、组队副本同步、服务端校验、云存档或多人救援。客户端的“最近玩家吸附”逻辑不能当作已完成联网同步的证据。
- 验证目标为 Windows64 Mono / Direct3D11；没有验证 IL2CPP、移动端、控制器完整操作、性能预算、不同显卡或正式发行渠道。
- 多角色阶段最早一次回归中，原有备份恢复测试出现一次未定位的写入失败；单独重现及之后三轮 257 项全量测试全部通过，最终集成存档 / 切换检查也通过。历史失败保留，不宣称已定位修复，见 `CharacterSavesInitialRegressionFailure.json`。
- 编辑器保留两个预览场景，其中一个为 SceneView 灯光场景，另一个空场景来源未完全确认；功能结束后专用角色预览相机与渲染纹理均释放。不以关闭未知编辑器场景的方式清理。

## 构建产物及清理

可运行程序：`Builds/Validation/20261028-final-local/RPG-Silent.exe`。包含 Addressables 的全流程耗时 189.425 秒，Player 构建 72.905 秒，BuildReport 总大小 559,024,353 字节。原 `Builds/Windows64` 未覆盖。脚本和运行记录位于 `Tools/Validation/FinalLocalBuild.cs.txt`、`FinalLocalStartup.ps1` 及输出目录。对应的 Addressables 内容状态已归档在产物的 `AddressablesState/Windows`；产物文件校验见 `sha256.json`。

独立程序以隐藏的 batchmode 运行 30 秒，使用真实 Direct3D11 和 RTX 3060 Ti，日志确认 `[StartUI] 已打开`，没有提前退出、异常 / 崩溃 / 加载失败 / Error 模式。只进行启动检查，没有在独立程序里自动创建或游玩角色；完整双副本流程的运行证据来自 Editor Play Mode。启动前后原玩家 Saves 目录文件数量和 SHA256 一致，测试过程未修改真实成长存档。

Play Mode 已退出，StartScene 无未保存修改；存档路径覆盖已清除，后台运行恢复 false，编译和导入均结束。专用角色预览相机 / RenderTexture 数量为零。14 个受验证影响的源文件已与事前备份逐一校验一致；构建改变的 LongCang、LiberationSans fallback 字体及 Addressables 内容状态已恢复，ProjectSettings 与基线一致。相关记录见 `FinalLocalSourceRestore.json`、`FinalLocalEditorState.json`。最终 Console 无错误 / 警告，见 `FinalLocalConsole.json`；未清空 Console。

`git diff --check` 对 Scripts、Tests、Docs 通过；新增功能脚本与测试的 `.meta` 均存在。没有提交 Git 或执行发布。当前本地功能范围完成，平台适配、真人长期游玩、性能和联网仍受上述证据边界限制。
