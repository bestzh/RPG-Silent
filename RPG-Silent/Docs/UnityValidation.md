# Unity 自动化测试

固定版本来自 `ProjectSettings/ProjectVersion.txt`，当前为 Unity 6000.0.66f2。运行时使用 Mono、Windows64，版本号 0.1.0。测试不访问玩家存档，存档测试仅在工程 `Temp/AutomatedSaveTests/<随机目录>` 中创建和清理数据。

## 编辑器内验证

在 Test Runner 的 EditMode 中运行 `RPGSilent.Tests.Editor`。目前共 27 项：玩家经验与等级、奖励事务、生命/能量边界、存档事务与恢复、格式保护、主要场景缺失脚本、Addressables 键和 Resources 入口。

游戏代码仍在 `Assembly-CSharp` 中，测试通过反射调用真实编译类型，避免为引入测试而改变运行时程序集边界。场景检查打开临时 Preview Scene，不保存或覆盖工作场景。通过这些检查不等于完成画面和操作体验验收。

## GitHub Actions

仓库根目录 `.github/workflows/unity-tests.yml` 提供手动 `Unity validation` 工作流。使用完整 SHA 固定 checkout、GameCI 和 artifact action，并固定 GameCI CLI v0.1.72。Unity 版本从工程文件读取；检出包含 LFS 内容；测试仅运行本项目测试程序集，不安装覆盖率包。结果必须包含实际执行且全部通过的第一方测试，日志保存 14 天。

首次启用前，由维护者在 GitHub `Settings > Secrets and variables > Actions` 设置 `UNITY_EMAIL`、`UNITY_PASSWORD`，以及 Personal 的 `UNITY_LICENSE` 或 Pro 的 `UNITY_SERIAL`。不要把这些值提交到仓库或发到聊天中。具体激活方式见 [GameCI 官方文档](https://game.ci/docs/github/activation/)。

2026-10-26，维护者已配置 Personal 所需的三个仓库 secrets，仅核对名称，不读取值。首次运行仍需验证激活是否成功。缺少配置时工作流会明确失败，不会跳过测试后显示成功。首次云端运行通过前，`main` 的必需检查仍为现有 `Git hygiene`。本机测试成功不能替代云端验证结果。

## 验证记录

- 2026-10-26：Unity 6000.0.66f2 EditMode 27/27 通过，0 失败、0 跳过，测试耗时 6.59 秒。
- 云端运行尚在验证，完成后补充实际报告。
