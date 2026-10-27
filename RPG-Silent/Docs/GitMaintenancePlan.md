# Git 与 Unity 管理优化完成清单

本清单承接 2026-10-26 已完成的 Git 忽略规则、中文功能提交、自动提交钩子、PR/CI 强制保护和依赖固定。后续目标保留完整范围，未验证的项目不计作完成。

| 项目 | 完成标准 | 当前状态 |
| --- | --- | --- |
| Unity 验证 | 可重复执行的编译、第一方 EditMode 测试、Windows 构建与运行检查；保留日志与结果，明确 CI 执行方式 | 本机批处理 27/27 通过，Windows 构建 0 错误；云端等待账号认证处理 |
| Git LFS | 明确二进制迁移范围，内容哈希不变，远端对象上传与全新检出成功，CI 正确处理 LFS | 三项当前资源已转为 LFS，上传、独立完整检出、哈希及 fsck 通过；历史迁移另行处理 |
| 资源裁剪 | 核对场景、预制体、Addressables、Resources 和样例导入入口；备份候选资源，裁剪后导入、测试、构建及运行通过 | 已裁剪约 228 MiB；依赖完全一致，27/27 测试、Windows 构建、独立启动及副本往返回归通过，见 ResourcePruning.md |
| 大型场景拆分 | 降低 Dungeon01 单文件体积并保留 GUID、引用、导航和玩法；场景加载与副本回归通过 | 主场景 66.97 → 3.51 MiB；151,667 个对象/组件指纹一致，导航数据与 GUID 不变；测试、构建、独立启动及副本回归通过 |
| 版本发布规范 | 提供版本号/标签/变更记录/构建产物与校验值流程，创建经过验证的版本记录 | 流程、变更记录与打包门禁已实现；正式版本等待最终验收 |
| 分支收尾 | 确认功能分支提交均已合入 main，再清理已合并分支；主分支与远端一致 | 已清理截至 PR #11 的已合并功能分支；当前裁剪分支及后续工作仍待最终收尾 |
| 历史瘦身 | 远端引用和历史备份可恢复，独立副本迁移演练与新克隆验证通过；远端切换后验证保护规则、CI 和对象完整性 | 联合裁剪/LFS 演练及独立完整克隆通过，当前树不变、Git 包约减少 148.9 MiB；远端未改写，最终切换前需更新备份和演练 |

基线：`993b0d0f8f31d1279ff63af5a66167a4a70cbb91`。工程使用 Unity `6000.0.66f2`，Windows64。该基线时仅有 Git 卫生 CI，第一方 Unity 自动化测试尚未建立；后续状态见上表和证据。历史迁移必须保留迁移前引用映射及独立备份，不能以删除当前文件代替历史清理。

## 已有证据

- PR #4 合并了 27 项 EditMode 测试和手动 Unity 云端验证入口；本机测试全部通过，0 跳过。PR #5 修复 GameCI 参数转换问题。云端最终成功状态另行记录，不能把工作流创建当作测试成功。
- PR #6 显式指定 Personal 账号激活，解决旧 `.ulf` 自动优先的问题。运行 `40337511096` 已进入 Personal 激活，但 Unity 返回 401 / Input Error 143.002，等待维护者核对账号与登录验证；尚未执行云端测试。
- `codex/git-hygiene`、`codex/git-reproducibility`、`codex/git-enforcement` 已验证为 main 的祖先，并使用预期 SHA 保护删除远端分支；本地以 `git branch -d` 清理。其提交仍在 main 中。
- 独立镜像备份：`D:/GitBackups/RPG-Silent/20261026-before-maintenance/repository.git`，使用 `--no-hardlinks` 创建，无 alternates；`git fsck --full --no-dangling` 成功，7 条引用与源仓库相同。备份记录在同目录的 `refs-before.txt`。这是迁移准备时的恢复点；正式切换前必须更新到最终远端状态并再次校验。
- 同目录 `lfs-rehearsal` 没有配置远端，仅在副本中转换 48 个 main 历史提交；提交映射为 `lfs-rehearsal-map.csv`。当前树 34,785 个文件的 Git 对象比较只变化 `.gitattributes` 与三个指定二进制文件；三个文件检出后 SHA-256 均与原始版本一致，`git lfs fsck` 成功。
- PR #8 补齐共享 pre-push 钩子的 LFS 上传流程；24 项 Git 测试通过，包括实际推送和全新克隆。PR #9 的三个 LFS 对象共 188,553,860 字节，已上传 GitHub；独立目录 `D:/GitBackups/RPG-Silent/20261026-lfs-remote-check` 完整检出提交 `613581d6`，三个文件大小及 SHA-256 与迁移前相同，`git lfs fsck` 成功且工作区干净。证据在忽略目录 `Logs/GitManagement/lfs-remote-verification.json`。
- 演练不等于远端瘦身：尚未改写任何远端历史。后续按 main 可达历史统计收益，不能将其他本地引用保留的旧 `output/` 对象计入 main 的瘦身收益。资源裁剪与场景拆分的验收分别见 `ResourcePruning.md` 和 `DungeonSceneSplit.md`。
