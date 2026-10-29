# V5 UI 预制体与接线

## 入口与页面

`Assets/Scenes/StartScene.unity` 使用 `Assets/Prefabs/UI/CanvasRoot.prefab`。Canvas Scaler 序列化为 Scale With Screen Size、1920×1080、Expand。页面工作区居中，HUD 使用四边和居中锚点；长内容由 ScrollRect、Layout Group、Content Size Fitter、TMP 原生自动字号处理。

下列根预制体仍在 `Assets/Resources_moved/UI/`，保留原 GUID 与 Addressable `UI/<名称>`：

| 预制体 | 包含的页面与状态 |
| --- | --- |
| StartUI | 开始菜单、角色创建、角色列表、存档错误反馈 |
| MainUI | 主城/战斗 HUD、十技能槽、两武器槽、药品、目标、防御、训练、教程、任务追踪、小地图、附近掉落、保存反馈 |
| InventoryUI | 五防具饰品与两武器、单件/整套试穿、物品比较、筛选搜索排序、待领取、药品快捷栏、配装方案、锁定、批量分解/出售/丢弃、确认和失败提示 |
| TownNpcUI | 导师学习与十槽装配、职业进阶、专精、强化、宝石制作镶嵌、商店购买回购、任务、故事、配方重铸、仓库与各确认层 |
| QuestJournalUI | 任务、章节、成就、四类图鉴、放弃确认 |
| PortalUI | 目的地、难度、六种挑战模式、远征预览与记录、训练入口 |
| DungeonUI | 副本目标、首领、胜败结算、继续拾取、离开掉落确认、远征事件、长期挑战战斗/休整/保存待重试/结束确认/结算 |
| SettingsUI | 画面、声音、控制和游戏设置，原服务的预览/应用/撤销及改键 |
| PauseUI | 恢复、保存、设置、主菜单、退出与保存反馈 |
| LoadingUI | 载入进度、重试、返回与错误反馈 |

另有 `Assets/Prefabs/UI/Templates/` 下九个可编辑模板：角色创建、角色选择、四个设置页、目的地行、服务/手札数据行、公共服务确认。共 20 个 UI 预制体，含 CanvasRoot。

## View 与业务边界

继续使用 UIBase → UIManager → Addressables，并由当前场景 VContainer 注入现有服务。视图只接线、填数据、切状态及复用条目；没有新增一套角色、背包、金币、任务或挑战状态。

`UIViewReferences` 在预制体中保存 Scope / Id / Target 直接引用。层级中的显示名称可以调整；保留 Id 和 Target 接线即可。`UIView.Require` 会报告缺失绑定，避免运行时悄悄创建控件补救。静态文案在预制体编辑，业务变化的名称、价格、等级、数量和提示仍由服务刷新。

`UIListView` 复用 `Row_ServiceOrJournal`，`UIListRowView` 包含图标、标题、说明、选择背景与四个操作。行高和列表高度由原生布局计算。`UIEquipmentSlotView` 表现实际穿戴，`UIStatTableView` 接入原有属性计算，`UIConfirmationView` 调用原事务并保留版本核对。

节点按 `Group_`、`Button_`、`Text_`、`Image_`、`RawImage_`、`Input_`、`Dropdown_`、`Slider_`、`Toggle_`、`ScrollView_`、`Content_` 命名，兄弟名称唯一。

## 美术与字体

85 项物品/技能图标引用接入已导入的 PONETI、Bamao、Synty 资源；具体映射见 `artwork-connections.txt` 和 `Tools/UIV5/Resources/resource-map.json`。宝石沿用项目既有蓝晶石图标约束。背景使用项目现有 BG 资源，未改变角色或装备模型。

中文正文使用 `Assets/Fonts/UI/NotoSansSC-UI SDF.asset`，源字体与 OFL 许可同目录。字体预填 1237 字符，并保留原生动态补字能力。

## 修改和复验

可直接在 Prefab Mode 编辑节点、锚点、布局与样式。`Assets/Editor/UIV5` 是编辑器生成/验证工具，不进入玩家构建；重复生成会按 V5 作者源覆盖布局，因此手工微调后应同步对应作者源。该工具连续执行两次已验证绑定数量稳定。

`UIV5Authoring.ValidatePrefabs()` 检查十个根页面的绑定、初始化、字体、原生布局和 Canvas 契约。新增 UI 预制体测试与生命周期回归测试在 `Assets/Tests/Editor/`。最终运行、分辨率和测试证据见验收报告。

3D 角色预览的 RenderTexture/相机宽高比，以及小地图和目标标记的世界坐标投影属于渲染及世界跟随；页面分辨率适配不使用自定义运行时代码。
