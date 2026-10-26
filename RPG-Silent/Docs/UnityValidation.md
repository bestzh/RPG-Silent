# Unity 自动化测试

固定版本来自 `ProjectSettings/ProjectVersion.txt`，当前为 Unity 6000.0.66f2。运行时使用 Mono、Windows64，版本号 0.1.0。测试不访问玩家存档，存档测试仅在工程 `Temp/AutomatedSaveTests/<随机目录>` 中创建和清理数据。

## 编辑器内验证

在 Test Runner 的 EditMode 中运行 `RPGSilent.Tests.Editor`。目前共 27 项：玩家经验与等级、奖励事务、生命/能量边界、存档事务与恢复、格式保护、主要场景缺失脚本、Addressables 键和 Resources 入口。

游戏代码仍在 `Assembly-CSharp` 中，测试通过反射调用真实编译类型，避免为引入测试而改变运行时程序集边界。场景检查打开临时 Preview Scene，不保存或覆盖工作场景。通过这些检查不等于完成画面和操作体验验收。

## GitHub Actions

仓库根目录 `.github/workflows/unity-tests.yml` 提供手动 `Unity validation` 工作流。使用完整 SHA 固定 checkout 和 artifact action，直接运行 GameCI CLI v0.1.72，并核对官方发布包的 SHA-256。Unity 版本从工程文件读取；检出包含 LFS 内容；测试仅运行本项目测试程序集，不安装覆盖率包。结果必须包含实际执行且全部通过的第一方测试，日志保存 14 天。

直接调用 CLI 时显式传入 `--coverageEnabled=false`。GameCI Action v4.4.0 会将该选项转换为 CLI 不支持的 `--no-coverageEnabled`；首次云端运行在启动 Unity 前因此失败。当前流程绕过该参数转换层，保留关闭覆盖率的原定配置。

首次启用前，由维护者在 GitHub `Settings > Secrets and variables > Actions` 设置对应凭据。手动运行时默认 `personal`，使用 `UNITY_EMAIL`、`UNITY_PASSWORD` 向 Unity 申请 Personal 席位；`serial` 额外需要 Pro 的 `UNITY_SERIAL`；`file` 使用可用于当前环境的 `UNITY_LICENSE`。不要把这些值提交到仓库或发到聊天中。流程显式传入 `--unityLicensingMethod`，避免已存在的旧 `.ulf` 抢先覆盖账号激活方式。实现依据为 [GameCI CLI v0.1.72 激活脚本](https://github.com/game-ci/cli/blob/v0.1.72/dist/platforms/ubuntu/steps/activate.sh)。

2026-10-26，维护者已配置三个仓库 secrets，仅核对名称，不读取值。运行 `40327272382` 已成功启动 Unity，但自动选择 `.ulf` 激活后报 `TimeStamp validation failed`，尚未运行测试；已改为显式选择 Personal 账号激活，仍需验证其结果。旧 `UNITY_LICENSE` 可以保留，Personal 模式不会使用它激活。缺少配置时工作流会明确失败，不会跳过测试后显示成功。首次云端运行通过前，`main` 的必需检查仍为现有 `Git hygiene`。本机测试成功不能替代云端验证结果。

## 验证记录

- 2026-10-26：Unity 6000.0.66f2 EditMode 27/27 通过，0 失败、0 跳过，测试耗时 6.59 秒。
- 云端运行 `40337511096` 明确使用 Personal 后，Unity 返回 401 Unauthorized / Input Error 143.002，激活未通过、测试未开始。等待维护者核对账号凭据与登录验证，不把这个结果归为游戏测试失败，也不宣称云端验证成功。
