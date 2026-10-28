# 背包角色预览

更新日期：2026-10-27。全部复用项目现有角色、模块化防具、武器、动画、字体和 UI 组件。

## 操作与范围

- B 打开背包，左侧查看当前人物，中央整理物品，右侧查看属性并穿戴、卸下。
- 在模型区域按住鼠标左键拖动可水平旋转，滚轮可缩放；左右按钮每次转动 30 度，重置视角恢复默认距离与正面斜角。
- 点击背包中的装备先临时试穿，预览标注“试穿预览 · 未装备”。点击“确认穿戴”才正式装备并应用属性；“取消试穿”恢复实际外观，保留查看角度。点选另一件装备会替换当前试穿，不叠加多个临时部位。
- 切换分类、翻页、点选材料、关闭背包或切换场景都会取消试穿。取消后，详情中的“试穿”按钮可以再次进入预览；防具保存失败时保留试穿供重试，实际防具与属性保持原状。
- 未达到等级要求的装备可以试穿，但不能确认；未持有及“待领取”物品不能试穿。饰品显示“无独立外观”，属性仍需确认后应用。出售或分解掉最后一件试穿物品会自动恢复实际外观。
- 预览使用当前职业和性别，九职业男女共 18 套模型均适配。头部、胸甲和靴子可混搭，卸下恢复职业默认部位。戒指和项链沿用属性效果。
- 当前激活武器也在预览中显示：主城收起，副本使用对应武器的战斗待机。试穿武器会临时显示候选武器及对应姿态；点击“确认装入武器槽 1/2”沿用原装备流程，关闭背包完成拔收动画，再次打开可查看新武器。

## 实现

`InventoryUI` 监听 `PlayerProgressService.Changed` 和玩家武器的 `PresentationChanged`，从已提交的五个装备槽构建预览。试穿 ID 仅由背包界面持有，在装备列表副本中覆盖一个槽或替换预览武器，不写入玩家状态与存档。取消始终重新读取最新的实际装备。`PlayerStanceController.GetPreviewIdle` 只读取候选姿态的待机动画，不修改实际姿态或 Animator。

`CharacterModelPreview.ShowLoadout` 复用选角页的独立模型、相机、灯光、RenderTexture 与不触发动画事件的 PlayableGraph。`ModularArmorAppearance` 复用场景角色的网格、骨骼重定向、隐藏部位和品质材质规则；武器使用相同 `CharacterRigProfile` 挂点。普通换装复用模型与骨架，职业或实际模型变化时才重建；选角页原有 `Show` 行为保留。

`PlayerClassDefinition.GetVisualGender` 统一旧存档的兼容性别规则：未穿防具时保留原始模型，穿可见防具后使用职业指定的模块化男女模型。预览与实际玩家使用同一规则。

预览隔离在第 31 层、场景外坐标，无输入控制、攻击或存档组件。关闭背包时解除事件订阅，释放动画图、模型、相机和纹理。背包布局按窗口等比缩放，RenderTexture 按预览矩形的宽高比更新。

## 验证

以下预览初版记录早于试穿功能；最新试穿交互和验证见 [InventoryTryOn.md](InventoryTryOn.md)。旧运行脚本中“选择物品不会试穿”的断言不再适用于当前版本。

- `Tools/Validation/InventoryPreviewEditModeResults.json`：163/163 测试通过，包含新增的 21 项预览测试，覆盖全部职业男女、穿脱恢复、视角保持、旧存档兼容及资源释放。
- `Tools/Validation/InventoryPreviewRuntimeResults.txt`：745 项检查通过，三基础职业男女分别进入主城与 Dungeon01，使用实际背包按钮穿戴/卸下，比较预览和场景角色的网格、颜色、武器挂点与比例；覆盖拖动、滚轮、右键忽略、复位、重开、失败保存以及相机/纹理清理。
- 初次测试失败记录在 `InventoryPreviewEditModeInitial.json`：测试夹具把无网格的女性眉毛节点计入可见部件，并假设 EditMode 会派发运行时 OnDisable。修正过滤与测试清理后通过；运行时关闭清理另有实际 Play Mode 验证。
- 截图在 `Tools/Validation/Captures/inventory-preview-*.png`。验证使用 `Temp/InventoryPreviewRuntime-20261027` 隔离存档，未操作真实玩家存档。
- 实际查看 1920×1080 和 1366×768 背包，模型保持正确比例，1366×768 下全部文本无溢出；EventSystem 射线命中模型区域，拖动入口不被背景遮挡。1280×720 与 1024×768 另做布局尺寸计算检查。
- `InventoryPreviewSupplementalResults.txt` 记录布局、场景切换自动关闭以及选角页六种基础职业男女组合回归。选角最终截图为 `inventory-preview-creation-native.png`。
- 编译无错误，运行验证期间无游戏异常。随后 MCP 同步截图产生 PlayerLoop 递归错误，堆栈明确指向插件 `ScreenshotUtility.cs:197` 内的 `EditorApplication.Step()`；改用 Unity 原生帧末异步截图后正确获取选角界面。无 UI 的两个 `creation-regression` 截图属于该工具失败记录，不作为视觉证据。另有 LongCang 动态字体缓存导入一致性警告；验证后还原缓存。完整日志保留于 `InventoryPreviewConsole.json`。
- 运行时操作由服务调用、UI 按钮与 EventSystem 指针事件驱动；未进行真人完整键鼠游玩、独立 Windows 构建或多人同步验证。
