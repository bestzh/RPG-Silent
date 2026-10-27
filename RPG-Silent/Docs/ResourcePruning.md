# 示例资源裁剪记录

2026-10-26 清理下列当前未使用的示例内容；游戏场景、运行时插件、模型、材质、纹理及导航数据保持原样。

| 范围 | 处理 | 依据 |
| --- | --- | --- |
| `Assets/PolygonNatureBiomes/PNB_Enchanted_Forest/Scene/Demo.unity` 及 meta | 删除 | GUID 无外部引用，不属于任何构建、Addressables、Resources 入口或其依赖 |
| 同目录 `Demo_URP.unity` 及 meta | 删除 | 同上 |
| `Packages/com.opsive.behaviordesigner/Samples~/` | 删除未导入样例 | 667 个样例 GUID 在样例目录外无引用；运行时和编辑器插件保留 |
| Opsive `package.json` 的 `samples` 条目 | 移除失效导入入口 | 防止 Package Manager 继续显示已移除的示例 |

这是一项对内嵌 Opsive 3.3.3 包的本地裁剪，未改变版本或运行代码。升级或重新导入插件时应重新审查样例目录及 `samples` 条目。Unity 的 `Samples~` 文件夹默认不导入，只有 Package Manager 的 Import 操作才复制到 Assets；参见 [Unity 6 官方说明](https://docs.unity3d.com/6000.0/Documentation/Manual/cus-samples.html)。

## 依赖与备份证据

- 使用 Unity 6000.0.66f2 的 AssetDatabase，对 Build Settings 全部 3 个场景（包括禁用的 Dungeon01）、全部 10 个 Addressables 入口和 253 个 Resources 资产求递归依赖：263 个不同入口、4,309 项依赖，两演示场景均不在其中。
- 对 Assets、Packages 和 ProjectSettings 的 25,567 个相关文本/序列化文件扫描，667 个样例 GUID 没有目录外引用；额外检查了代码中的演示场景路径及样例导入路径。
- 独立备份位于 `D:/GitBackups/RPG-Silent/20261026-pruned-assets`，包含 1,298 个文件（含原 package.json），共 238,863,741 字节；逐文件 SHA-256 一致，清单为 `manifest.json`。
- 原始证据保留在忽略目录 `Logs/GitManagement/prune-unity-dependencies.json`、`prune-static-references.json`、`samples-external-references.json`。裁剪仅影响当前树；已有历史对象需单独迁移。

## 验收

裁剪后 Unity 导入和编译完成，Console 错误为 0；重新查询同一组入口仍为 4,309 项依赖，新增和缺失均为 0。

2026-10-27 在干净提交 `beab93a6ac89bd2c9862ff913f018fc05347dc5a` 上执行完整验收：

- `Invoke-UnityValidation.ps1 -BuildWindows -SmokeStartup`：27/27 EditMode 测试通过，0 失败、0 跳过；Windows64 构建成功、0 错误，独立 Player 运行 30 秒，开始菜单加载且无错误模式。记录：`Logs/Validation/20261027T012142712Z/validation.json`。
- 保留一条基线已有的 LongCang 动态字体导入一致性警告；构建生成的字体缓存和 Addressables 状态文件由验证器恢复，验证后工作区干净。
- 独立存档运行 StartScene → Main → Hard Dungeon01 → Main → StartScene。既有 `EnemyBehaviorDesignerMcp.cs.txt` 的 26 项检查全部通过，覆盖四处出生点完整导航路径、实际行为树运行及启停、Boss 阶段切换、195 金币/414 经验结算和回城；额外检查返回菜单后玩家数量为 0、副本上下文清空。Console 错误为 0。证据：`Logs/GitManagement/resource-pruning-runtime.txt` 及 `Captures/resource-pruning-main-return.png`。
- 两次失败尝试单独保留：受限环境下本机许可连接超时，未运行测试；正常权限下首次构建遇到 SBP 设置文件 Win32 1224 占用。后者在源码和配置不变的重试中未复现，占用原因尚未定位，不能声称已修复。失败记录分别为 `20261027T011454024Z` 和 `20261027T011858635Z`。

上述为本机验收；GitHub 云端 Unity 认证失败仍单独待处理。

需要恢复时，从上述备份按原相对路径复制，或从保留的裁剪前 Git 提交恢复目标文件，同时恢复包的 `samples` 条目。不要为已有资源重新生成 GUID。
