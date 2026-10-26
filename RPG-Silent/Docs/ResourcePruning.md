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

裁剪后的导入、测试、Windows 构建与运行结果将在完成后补充。尚未验收时不把该项列为完成。

需要恢复时，从上述备份按原相对路径复制，或从保留的裁剪前 Git 提交恢复目标文件，同时恢复包的 `samples` 条目。不要为已有资源重新生成 GUID。
