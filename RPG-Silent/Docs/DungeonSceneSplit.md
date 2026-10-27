# Dungeon01 环境拆分

2026-10-27 将 `Dungeon01` 中两个静态环境根对象通过 Unity `SaveAsPrefabAssetAndConnect` 提取为场景专用预制体，保留原层级、名称、变换和嵌套预制体。游戏逻辑、敌人、出生点、灯光和导航继续由原场景管理。

| 文件 | 拆分前 | 拆分后 |
| --- | ---: | ---: |
| `Assets/Scenes/Dungeon01.unity` | 70,219,410 字节（66.97 MiB） | 3,685,415 字节（3.51 MiB） |
| `Assets/Scenes/Dungeon01/Environment/DirtBlobs.prefab` | — | 47,165,163 字节（44.98 MiB） |
| `Assets/Scenes/Dungeon01/Environment/Terrain.prefab` | — | 20,275,364 字节（19.34 MiB） |

主场景文件缩小约 94.75%，三个文本文件均低于 50 MiB，因此删除 Git 大文件例外。内容被分离到预制体，总资产体积并未因此减少；这项改动用于分开维护环境与玩法，并非历史瘦身。

## 引用与内容校验

- 场景 GUID 仍为 `121ef50d0993f3d41a5663b03a24a33a`，场景 meta、现有 NavMesh 资产及 meta 与独立备份逐字节一致。
- 场景的遮挡、RenderSettings、LightmapSettings 和 NavMeshSettings 序列化内容未变化。
- 拆分前及保存后重新打开场景，均记录 151,667 个 GameObject/组件。按层级索引、名称、组件次序建立稳定标识，将 Editor JSON 中临时 instanceID 替换为对象标识或资源 GUID/fileID，再逐对象计算 SHA-256；另记录 Renderer 的光照贴图索引及缩放偏移。
- 重新加载后的对象集合及全部指纹一致：0 新增、0 删除、0 改变；0 丢失脚本、0 未解析引用、0 跨根对象引用。上述指纹验证不替代实际运行或画面检查。
- 复用审计脚本：`Tools/Validation/DungeonSceneAuditMcp.cs.txt`。在已保存的 Dungeon01 Edit Mode 设置 AppDomain 键 `DungeonAudit.label` 为 `before`、`after` 或 `reloaded` 后，通过 MCP 执行；查询 `DungeonAudit.result`。它分批读取对象，证据写入忽略目录 `Logs/GitManagement/dungeon-audit-*.json`，不修改场景。

独立恢复点：`D:/GitBackups/RPG-Silent/20261027-before-scene-split`，含原场景、meta、导航数据、原 Git 规则和逐文件 SHA-256。汇总证据：`Logs/GitManagement/dungeon-split-comparison.json`。

## 验收

- 固定机位的拆分前后截图已检查，环境几何与材质一致；图片在 `Logs/GitManagement/Captures/dungeon-before-entry.png` 和 `dungeon-after-entry.png`。
- 独立存档运行 StartScene → Main → Hard Dungeon01 → Main → StartScene，既有行为树/副本脚本 26 项检查通过，另检查返回菜单后的玩家清理和副本上下文；Console 错误为 0。覆盖全部四个出生点的完整导航路径、两波敌人、Boss 阶段切换、奖励及返回主城。证据：`Logs/GitManagement/dungeon-split-runtime.txt`。
- Editor 内 EditMode 测试 27/27 通过，0 失败、0 跳过，MCP job `202a8378975848fa840c065175375a37`。
- 编辑器内执行现有 `RPGSilent.Editor.ProjectBuild.BuildWindows`：Addressables 与 Windows64 Player 构建成功，0 错误、1 条基线已有的动态字体导入一致性警告，产物 518,483,334 字节。独立 Player 运行 30 秒，开始菜单加载，未提前退出且未发现错误模式。报告：`Builds/Windows64/build-report.json`、`Logs/GitManagement/dungeon-split-editor-build.json`、`dungeon-split-player-startup.json`。
- 构建生成的字体缓存、Addressables 状态及 PlayerSettings 从本次构建前备份恢复，未混入提交；旧 Windows64 构建已保留在 `Builds/Archived/20261026T123557Z-Windows64`。本轮验证的是场景修改工作树，正式发布仍须从最终干净提交重新执行发布验证。

后续修改静态环境时，编辑相应场景专用预制体，避免把大量环境覆盖重新写回主场景。不要重新生成现有场景或导航资源的 GUID。
