# Windows 构建与验证

固定版本来自 `ProjectSettings/ProjectVersion.txt`，当前为 Unity 6000.0.66f2。构建目标为 Windows64，使用现有 Mono 后端和 PlayerSettings 版本号。

在退出 Play Mode、保存或撤销场景修改后，使用 `Tools > RPG Silent > Build Windows64`。入口先验证版本、平台和场景，再显式构建 Addressables 和 Windows Player。结果写到忽略目录 `Builds/Windows64`，`build-report.json` 记录成功/失败、版本、场景、大小、警告和错误。

## Windows 批处理

先关闭该工程的 Unity 编辑器，然后在工程目录执行：

```powershell
./Tools/Validation/Invoke-UnityValidation.ps1 -UnityEditorPath 'E:/unityEditor/unity6000.0.66/Editor/Unity.exe'
./Tools/Validation/Invoke-UnityValidation.ps1 -UnityEditorPath 'E:/unityEditor/unity6000.0.66/Editor/Unity.exe' -BuildWindows
```

脚本拒绝使用被编辑器占用的工程。测试必须全部通过且非空，才会进入构建；构建状态必须为 Succeeded 且错误数为 0，失败返回非零状态。批处理在 Addressables 构建后明确加载已保存的启动场景，满足 Opsive 预制体验证器恢复场景的要求。日志与 NUnit XML 位于 `Logs/Validation/<UTC时间>/`，独立构建产物位于 `Builds/Validation/<UTC时间>/`，包括每个产物的 SHA-256。`validation.json` 记录提交和未提交文件，避免把工作区验证误认为某个提交的验证。

TMP 会在构建和编辑器退出时清理动态字体缓存。批处理先备份 TMP 字体资产、PlayerSettings、SBP 设置及已有 Addressables state，在 Unity 进程退出后按文件哈希恢复变动；备份保存在本次日志的 `source-backup/`，不使用会被 Unity 删除的 `Temp/`。本次构建的 Addressables state 另存到产物中的 `AddressablesState/<平台>/`，供该版本后续内容更新使用。编辑器菜单构建不包含这个外部恢复步骤，可能产生字体缓存改动；日常交付优先使用批处理。

脚本不会自动发布、打标签或覆盖玩家存档。Player 启动和玩法回归需额外验证，不能由构建成功推定。

## 验证记录

- 2026-10-26，运行 `20261026T142107379Z`：27/27 EditMode 测试通过，0 失败、0 跳过；Windows64 构建成功，0 错误、1 警告，产物 517,500,484 字节。Addressables 与 Player 构建共 38.68 秒，完整验证约 78.52 秒（已有本机缓存）。
- 唯一警告为 `LongCang-Regular SDF.asset` 的 NativeFormatImporter 一致性警告；已确认 TMP 构建前自动清理动态字形缓存，批处理结束后源字体文件与运行前一致。该警告仍保留在 `build-report.json`，不将其隐去。
- 实际报告位于 `Logs/Validation/20261026T142107379Z/validation.json`，构建和 SHA-256 清单位于同编号的 `Builds/Validation/`。本次基线为 `bf13d81e` 加当时尚未提交的构建入口与脚本，不能作为仅该提交的构建证据。
- 同一产物独立进程启动 30 秒，日志确认 StartUI 打开，未提前退出，未命中异常/崩溃/加载失败错误；证据为同一日志目录的 `player-startup.json` 和 `.log`。这是启动检查，尚未覆盖完整玩家操作和视觉验收。
