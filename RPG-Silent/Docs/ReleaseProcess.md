# 版本与 Windows 发布

PlayerSettings 的 `bundleVersion` 是产品版本来源，使用 `主版本.次版本.修订版本`；开发中的下一版可用 `-rc.1` 等后缀。正式 Git 标签为 `v<版本号>`，标签必须指向已验证的 main 提交，已发布标签不移动。改变版本、更新变更记录和修复功能使用独立提交，通过 PR 和必需 CI 检查后合并。

## 验证与打包

1. 确认最终 main 与远端一致、工作区干净、包依赖固定且 LFS 对象完整。先完成计划中的历史迁移，再进行正式版本验证，避免迁移后提交号改变。
2. 关闭工程编辑器，运行 `Tools/Validation/Invoke-UnityValidation.ps1 -UnityEditorPath '<固定版本的 Unity.exe>' -BuildWindows -SmokeStartup`。全部测试必须执行且通过；Windows 构建必须成功且错误数为 0。保留并说明警告。
3. 脚本会启动本次独立程序 30 秒并检查日志、退出状态，把证据写入同一验证目录的 `player-startup.json`。随后完成本版本相关的实际操作、玩法和视觉验收，不能仅凭启动日志完成验收。
4. 运行 `python Tools/Release/package_windows.py Logs/Validation/<本次编号>`。工具验证当前提交、干净工作区、Unity/产品版本、测试、构建、启动检查以及所有产物哈希后，生成 `Builds/Releases/v<版本>/`，已有目录不会覆盖。

产物包含 Windows 运行包、独立验证包、`release.json` 和 `SHA256SUMS.txt`。验证包保留该构建的 Addressables content state，供后续内容更新使用；运行包不包含状态文件、测试记录或构建报告。整个验证目录仍需保留，打包成功本身不代替玩法和视觉验收。

打包器的门禁回归可用 `python -m unittest discover -s Tools/Release -v` 执行。测试只在临时 Git 仓库中使用模拟产物，覆盖正常打包、旧提交、脏工作区、产物被修改及未知运行依赖；测试通过不代表游戏版本已验收。

## 标签与发布记录

在最终验收通过后创建带说明的标签并推送，例如 `git tag -a v0.1.0 -m 'v0.1.0：首个经验证的 Windows 原型版本'`、`git push origin v0.1.0`。随后核对远端标签解析后的提交与 `release.json` 相同。GitHub Release 使用同一标签，附上两个 zip、清单和校验值，说明功能范围、操作方式、已知限制和验证结果；发布前可先保存为草稿审阅。

下载后用 SHA-256 验证文件。出现问题时保留已有版本，修复后发布新的修订版本，不覆盖旧标签或静默替换同名产物。回退部署应选择已有验证版本，并检查存档兼容性。

当前仍在完成维护清单，尚未把既有本地构建标记为正式发布版本；正式标签和版本记录在最终验证后填写。
