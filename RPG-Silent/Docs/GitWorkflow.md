# Git 管理与提交检查

仓库根目录是 `E:/project`，Unity 工程位于其下的 `RPG-Silent/`。下面的相对路径和命令均以仓库根目录为起点。Unity 版本见 `RPG-Silent/ProjectSettings/ProjectVersion.txt`。

## 应提交的内容

- 项目源码、使用中的资源与 `.meta`、`Packages/manifest.json`、`Packages/packages-lock.json`、`ProjectSettings/`。
- 有复用价值的编辑器配置、工具、文档与验证记录。
- 同一功能的代码、依赖注入注册、配置、预制体引用与 `.meta` 尽量放在同一提交，确保各提交能独立验证。

根目录 `output/`、Unity 缓存、生成的解决方案和项目文件不提交。项目根目录的七个 CommandBuffer 脚本是 Unity 菜单生成的副本，实际编译使用包中的版本；这七个具体路径被忽略，不会忽略 `Assets/` 中同名的业务代码。

已生成的本地文件保留；停止跟踪仅影响 Git。新克隆可由 Unity/IDE 重新生成解决方案。

## 分支与提交

从最新 `main` 创建短期功能分支，例如 `codex/skill-cooldown`；按功能使用中文提交说明：

```text
feat(战斗): 增加技能冷却提示
fix(副本): 修复失败后重试的输入恢复
chore(仓库): 完善资源忽略规则
```

合并前通过 PR 查看改动和 `Git hygiene` 检查。重要里程碑在功能验收后创建版本标签。此阶段提供 CI，不自动调整 GitHub 分支保护，也不自动合并 PR。

## 本地检查

需要 Git 和 Python 3.10 或更新版本，不需要额外 Python 包或 Unity 许可证。

```powershell
# 先显式暂存本次功能的文件，再检查实际即将提交的内容
git add -- <本次功能的文件或目录>
python RPG-Silent/Tools/Git/check_repository.py --staged

# 检查当前分支相对 main 的所有新提交
python RPG-Silent/Tools/Git/check_repository.py --base origin/main --head HEAD

# 验证检查器本身，使用独立临时 Git 仓库
python -m unittest discover -s RPG-Silent/Tools/Git -p 'test_*.py' -v
```

脚本读取 Git 暂存区或提交中的实际 blob，未暂存的修复不会掩盖暂存区的问题。提交范围检查逐个检查新提交，能发现「先提交大文件，再在后续提交删除」的情况。

检查范围：

- 禁止提交 `output/`、工程缓存、生成的解决方案及七个根目录生成脚本。
- 检查新增或修改文件中的合并冲突标记；跳过二进制文件和 Markdown 的普通分隔线。
- 检查本次改动涉及的 `Assets/` 资源、目录及 `.meta` 是否配对；资源删除时也需删除对应 `.meta`。允许 Unity 的空目录元数据，校验 `.meta` 的 GUID 格式，不对全仓库 GUID 唯一性作保证。
- 普通文件上限为 50 MiB。`policy.json` 对已有六个大文件逐路径记录原因，暂允至 75 MiB；达到 100 MiB 一律失败。新例外应单独说明用途并经过审阅。

这是增量仓库卫生检查，不替代 Unity 编译、运行测试、构建或完整历史审计。修改检查策略前须运行检查器测试。

## GitHub Actions

`.github/workflows/repository-checks.yml` 在推送到 `main`、PR 更新及手动触发时运行。PR 比较基线与源分支的共同祖先；手动运行可指定基线，留空检查最后一次提交。

工作流只有仓库读取权限，固定 checkout Action 的完整提交哈希；采用稀疏检出及按需读取 blob，不为运行检查器检出全部美术资源。先运行检查器测试，再检查本次提交范围。没有自动部署、Unity 构建或写入仓库的步骤。

## 换行与 Unity 合并

根目录 `.gitattributes` 对脚本、配置、文档和 Unity 文本资源使用 LF；Windows 批处理使用 CRLF。模型、图片、音频等保留二进制字节；`.asset` 使用自动识别，避免把二进制导航网格强制当成文本。引入规则时不批量重新序列化资源。

Windows 开发机可使用匹配工程版本的 Unity 配置本仓库合并工具：

```powershell
./RPG-Silent/Tools/Git/setup_unity_merge.ps1 -UnityEditorPath '实际 Unity Editor 路径/Unity.exe'
git mergetool
```

工具路径写入本机仓库的 `.git/config`，不写死到共享配置。场景和预制体的合并结果仍需在 Unity 中复核。

## 第二阶段

大型二进制的 LFS 迁移、历史瘦身、第三方样例裁剪及场景拆分单独评估。仅增加忽略规则不会清理已经提交的历史；历史迁移前需备份并协调其他克隆。本阶段不执行这些操作。

参考：[Git attributes](https://git-scm.com/docs/gitattributes)、[Unity Smart Merge](https://docs.unity3d.com/6000.0/Documentation/Manual/SmartMerge.html)、[GitHub 大文件说明](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github)。
