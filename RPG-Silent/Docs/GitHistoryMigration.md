# 2026-10-27 Git 历史迁移

本次仅改写 `main` 的历史，清理已确认无引用的示例内容，并将三个指定二进制资源的历史版本转换为 Git LFS。切换前后当前文件树完全一致，不改变游戏代码、资源、GUID 或导航数据。

| 项目 | 记录 |
| --- | --- |
| 切换前 main | `c5618c67761010e9cbf54fdf9e8d24691adccea0` |
| 切换后 main | `aee9db586b9d7549189429b0f7292eef3e1ddfb8` |
| 相同的当前文件树 | `5bde8c9700db43c638d17b190148e31664e1d9a5` |
| 提交映射 | [GitHistoryCommitMap.csv](GitHistoryCommitMap.csv)，76 条，均映射到新 main 的可达提交 |
| 独立恢复镜像 | `D:/GitBackups/RPG-Silent/20261027-final-history/remote-before.git` |

## 范围与收益

历史中删除的路径是 `PNB_Enchanted_Forest/Scene/Demo.unity`、`Demo_URP.unity` 及对应 meta，以及 `Packages/com.opsive.behaviordesigner/Samples~/`。这批内容此前已在 PR #12 中从当前工程移除并完成依赖、构建和玩法验收。

历史 LFS 范围与当前规则相同：`VampArmsLegs_S.tga`、`VampTorso_S.tga` 和 `Suntail - Mild Flight.wav`。三个资源合计 188,553,860 字节，文件内容与原始 SHA-256 一致。

独立演练副本中，Git pack 从 1,904,958 KiB 降到 1,752,483 KiB，减少 148.90 MiB，约 8%。LFS 对象约 179.82 MiB 另计；不能将此数字解释为总下载量减少 8%。GitHub 的旧 PR 引用可能保留旧对象，本次没有删除 PR 引用，也不承诺服务器物理删除全部旧历史。

## 备份、切换与保护

切换前镜像保存了 14 条远端引用，无 alternates，Git 完整性和三个 LFS 对象哈希验证通过。过滤与 LFS 两阶段的原始提交映射保留在独立备份目录，CSV 为两阶段映射的组合结果。

用户确认维护窗口后，临时允许指定维护账号强推及管理员绕过。推送明确使用旧 SHA 的 `--force-with-lease`，只更新 `refs/heads/main`。推送成功后立即恢复原规则，并重新打开设置逐项比较：PR、Git hygiene、分支保持最新、讨论解决、管理员不得绕过均启用；强推和删除均关闭，临时账号许可已移除。

第一次传输因推送副本不包含旧对象而发送整个历史包，已先恢复保护并终止，远端未改变。随后在推送进程中临时借用独立旧镜像的对象进行差异计算，实际传输约 35 KiB 后完成切换；没有将旧分支重新推回远端，也没有在演练副本留下持久缓存依赖。

本地同步前核对了远端 SHA、干净工作区与完全相同的文件树，再用带明确旧 SHA 的原子引用事务更新本地 main 和 origin/main。保留原 reflog、Codex 快照与独立旧镜像，不清理用户恢复记录。

## 验证

- 演练和独立本地新克隆均通过 `git fsck --full --no-dangling`、`git lfs fsck`、当前完整文件树及三个资源 SHA-256 检查。
- 迁移副本的 24 项 Git/钩子测试与 5 项发布门禁测试通过；新 main 第一父提交至 main 的两个提交检查为 0 问题。
- [Repository checks 40440023431](https://github.com/bestzh/RPG-Silent/actions/runs/40440023431) 在新 main 上成功，比较基线为新历史父提交 `b665e855ee0f848543166b896f38992ccf8b1b0a`。
- 首次 push 事件 [40439403835](https://github.com/bestzh/RPG-Silent/actions/runs/40439403835) 使用旧历史 SHA 作为基线，无公共祖先导致范围检查退出；其两套工具测试成功。保留这次失败记录，没有放宽检查策略。
- 切换后不使用本地对象缓存，从 GitHub 完整下载并检出 33,522 个文件及三个 LFS 对象。`git fsck --full --no-dangling`、`git lfs fsck`、当前完整文件树和三个资源 SHA-256 校验通过，工作区干净，无 shallow 或 alternates。该次 GitHub 克隆的 Git pack 为 1,757,217 KiB；服务器打包方式不同于前述本地演练统计。

详细证据在忽略目录 `Logs/GitManagement/`：`final-history-backup.json`、`final-history-rehearsal.json`、`final-history-preflight.json`、`final-history-remote-verification.json`、`final-history-ci.json`、`main-protection-after-history.json` 和 `final-history-local-sync.json`。完整远端克隆路径为 `D:/GitBackups/RPG-Silent/20261027-final-history/github-after`；另一份使用缓存加速后解除依赖的独立检出也通过完整性验证，其记录另行保留。

## 其他克隆如何继续

最稳妥的方式是保留旧工作目录和未提交文件，在新的空目录重新克隆仓库，确认 Git LFS 检出正常，再用 Unity 6000.0.66f2 打开其中的 `RPG-Silent` 工程目录。后续功能分支从新的 main 创建，通过 PR 和 CI 合并。

有未合并功能的旧克隆应先保存补丁及独立备份，再根据提交映射把必要改动迁移到新分支并验证。不要直接合并旧 main、推送旧备份分支或使用无租约的强推，以免重新引入已清理的历史。原文档中的旧提交号属于迁移前证据，可通过 CSV 查找对应的新提交。

如需回退，由维护者从独立镜像恢复指定引用，检查远端没有后续提交，并重新取得保护调整所需确认。不要移动已发布标签或删除备份。
