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

合并前通过 PR 查看改动和 `Git hygiene` 检查。重要里程碑在功能验收后创建版本标签。CI 不会修改 GitHub 分支保护或自动合并 PR；合并由仓库维护者按任务授权执行。

`main` 的远端保护要求：必须通过 PR 合并，`Git hygiene` 成功且源分支包含最新 `main`，所有审阅讨论已解决。规则同样适用于管理员；禁止强制推送和删除 `main`。当前不强制另一位成员批准，个人维护者可以在检查通过后合并自己的 PR。允许 merge commit，以保留按功能拆分的提交。

## 本地检查

需要 Git、Git LFS 和 Python 3.10 或更新版本，不需要额外 Python 包或 Unity 许可证。

每个新克隆先安装一次本地提交钩子：

```powershell
python RPG-Silent/Tools/Git/install_hooks.py
```

安装只修改当前仓库的 Git 本地配置，记录当前 Python 路径，并把 `core.hooksPath` 指向仓库内的 `RPG-Silent/Tools/Git/hooks`。此后普通 `git commit` 会自动检查实际暂存区，检查失败则中止提交。Python 被移动或卸载后也会中止提交，重新安装钩子即可更新解释器路径。钩子文件使用 LF，可在 Windows Git 和 Linux/macOS 上执行。

安装器用 `git lfs install --local --skip-repo` 配置当前仓库的 LFS 过滤器；跟踪的 `pre-push` 将完整参数和标准输入交给 `git lfs pre-push`，先上传所需对象再推送提交。LFS 未安装时推送明确失败，避免远端只有指针。不要用其他钩子覆盖该文件。真实集成测试通过本地裸仓库推送 LFS 对象，再从全新克隆下载并逐字节比较，确保提交检查和对象上传共存。过滤器和钩子准备就绪不代表已有历史已迁移。

`post-checkout`、`post-commit`、`post-merge` 使用 Git LFS 标准脚本并一同跟踪、设置可执行位。这样 LFS 在检出过程中无需向共享钩子目录自动补写未跟踪文件；集成测试同时要求 LFS 提交后的工作区干净。

安装器遇到已有的钩子目录配置或 `.git/hooks` 中的非示例文件会停止，避免停用已有工具；重复安装本工具安全。需要移除时运行 `python RPG-Silent/Tools/Git/install_hooks.py --uninstall`，仅移除本工具的钩子路径及 Python 配置，LFS 过滤器保留；若继续使用 LFS，需按现有钩子情况配置独立的 LFS pre-push。Git clone 不会自动安装本项目钩子；远端 CI 与本地钩子独立执行，作为合并的必要检查。

```powershell
# 先显式暂存本次功能的文件；也可以在 commit 前手动提前检查
git add -- <本次功能的文件或目录>
python RPG-Silent/Tools/Git/check_repository.py --staged

# 检查当前分支相对 main 的所有新提交
python RPG-Silent/Tools/Git/check_repository.py --base origin/main --head HEAD

# 验证检查器本身，使用独立临时 Git 仓库
python -m unittest discover -s RPG-Silent/Tools/Git -p 'test_*.py' -v
```

脚本读取 Git 暂存区或提交中的实际 blob，未暂存的修复不会掩盖暂存区的问题。提交范围检查逐个检查新提交，能发现「先提交大文件，再在后续提交删除」的情况。

检查范围：

- 禁止提交 `output/`、工程缓存、生成的解决方案、七个根目录生成脚本，以及 Addressables 的 content state、自动 link.xml 和 StreamingAssets/aa 产物。
- 检查新增或修改文件中的合并冲突标记；跳过二进制文件和 Markdown 的普通分隔线。
- 检查本次改动涉及的 `Assets/` 资源、目录及 `.meta` 是否配对；资源删除时也需删除对应 `.meta`。允许 Unity 的空目录元数据，校验 `.meta` 的 GUID 格式，不对全仓库 GUID 唯一性作保证。
- 普通文件上限为 50 MiB。三个大型二进制文件已改为 LFS，两个示例场景已裁剪，Dungeon01 已拆分为场景及两个环境预制体；`policy.json` 不再保留大文件例外。达到 100 MiB 一律失败。新例外应单独说明用途并经过审阅。

这是增量仓库卫生检查，不替代 Unity 编译、运行测试、构建或完整历史审计。修改检查策略前须运行检查器测试。

Addressables 生成文件的忽略规则使用 `**/Assets/…`，覆盖位于仓库子目录的 Unity 工程。此前已被跟踪的 `Windows/addressables_content_state.bin` 及其 meta 已停止跟踪，本机文件保留。内容更新构建需要与发布版本匹配的 state；Windows 批处理将它归档至对应构建的 `AddressablesState/<平台>/`，与产物和 SHA-256 一同保留，不能拿任意一次本地构建的 state 替代正式版本。

## GitHub Actions

`.github/workflows/repository-checks.yml` 在推送到 `main`、PR 更新及手动触发时运行。PR 比较基线与源分支的共同祖先；手动运行可指定基线，留空检查最后一次提交。

工作流只有仓库读取权限，固定 checkout Action 的完整提交哈希；采用稀疏检出及按需读取 blob，不为运行检查器检出全部美术资源。先运行检查器和真实提交钩子测试，再检查本次提交范围。钩子测试使用临时 Git 仓库，覆盖实际暂存内容、缺失 `.meta`、已有钩子保护、重复安装与卸载、Python 不可用。没有自动部署、Unity 构建或写入仓库的步骤。

## 换行与 Unity 合并

根目录 `.gitattributes` 对脚本、配置、文档和 Unity 文本资源使用 LF；Windows 批处理使用 CRLF。模型、图片、音频等保留二进制字节；`.asset` 使用自动识别，避免把二进制导航网格强制当成文本。引入规则时不批量重新序列化资源。

Windows 开发机可使用匹配工程版本的 Unity 配置本仓库合并工具：

```powershell
./RPG-Silent/Tools/Git/setup_unity_merge.ps1 -UnityEditorPath '实际 Unity Editor 路径/Unity.exe'
git mergetool
```

工具路径写入本机仓库的 `.git/config`，不写死到共享配置。场景和预制体的合并结果仍需在 Unity 中复核。

## 固定 Git 包依赖

`com.coplaydev.unity-mcp` 的 manifest URL 和锁文件均固定到完整提交 `81542a7e86292b5c4c21b1782c5af23b567f45ce`。它与原锁文件及本机已安装的 MCP 10.3.0 完全一致。原锁文件本就能保持日常安装一致；同时固定 manifest 可避免重建锁文件或重新添加同一 URL 时跟随上游 `main` 更新。

升级 Git 包应使用独立提交，同步 manifest 和锁文件，并在 Unity 解析完成后检查包哈希和 Console。不要只删除锁文件来尝试修复包问题。完整 SHA 的语法见 [Unity Git 依赖说明](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html#revision)。

## 大文件与资源裁剪评估

当前 LFS 范围仅为下表中的 `VampArmsLegs_S.tga`、`VampTorso_S.tga` 和 `Suntail - Mild Flight.wav`，合计 188,553,860 字节；精确路径规则位于根 `.gitattributes`。暂存指针的 SHA-256 和大小均与转换前工作区字节一致，资源及 `.meta` 内容不变。钩子负责先上传对象，Unity 验证工作流使用 `lfs: true` 检出。现有历史瘦身是独立后续步骤，新增指针提交不会删除旧历史中的完整二进制。

首次克隆后安装 Git LFS 和本仓库钩子，运行 `git lfs pull`，再打开 Unity。遇到缺失资源时先检查 `git lfs ls-files`、`git lfs status` 和 `git lfs fsck`，不要让 Unity 导入指针文本。暂不按扩展名把所有贴图、音频或场景整体转入 LFS。

以下保留 2026-10-26 第一阶段合并提交 `fde403e06be0525e56c61b9cdfbe58a59a692f04` 的历史评估：当时树共 34,782 个文件，未压缩内容约 4,138.10 MiB；这不是克隆下载量。当时 `git lfs ls-files` 为空。六个超过 50 MiB 的文件如下，路径相对于 `RPG-Silent/`：

| 文件 | 大小（MiB） | 首次评估时的证据与结论 |
| --- | ---: | --- |
| `Assets/Scenes/Dungeon01.unity` | 66.97 | 项目副本场景，保留；场景拆分需要独立功能验证。 |
| `Assets/PolygonNatureBiomes/PNB_Enchanted_Forest/Scene/Demo.unity` | 64.89 | 未出现在三个配置场景的依赖集合中，列为裁剪候选。 |
| `Assets/PolygonNatureBiomes/PNB_Enchanted_Forest/Scene/Demo_URP.unity` | 64.89 | 同上，尚不能据此证明整个资源包可删除。 |
| `Assets/LongswordAnimsetPro/Models/Vampire/Textures/VampArmsLegs_S.tga` | 64.00 | 被配置场景依赖，保留；可评估 LFS。 |
| `Assets/LongswordAnimsetPro/Models/Vampire/Textures/VampTorso_S.tga` | 64.00 | 被配置场景依赖，保留；可评估 LFS。 |
| `Assets/Suntail Village/Audio/Suntail - Mild Flight.wav` | 51.82 | 被配置场景依赖，保留；可评估 LFS。 |

依赖证据来自 Unity 6000.0.66f2 的 `AssetDatabase.GetDependencies`，对 `StartScene`、`Main`、`Dungeon01` 三个配置场景递归查询，共 3,884 条依赖；包含 Build Settings 中未勾选的 Dungeon01。项目还通过 Addressables 和 Resources 动态加载资源，场景依赖扫描不是完整的未使用资源证明，因此本轮保留全部美术资产。

`Packages/com.opsive.behaviordesigner/Samples~/` 有 1,293 个跟踪文件，合计 94.73 MiB。其 `package.json` 仍声明此样例入口；直接删除会让样例导入入口失效。裁剪应作为单独的供应商包变更，同时处理样例入口、导入功能及更新方式。

该次评估之后已完成三个文件的当前版本 LFS 转换、示例裁剪及场景拆分，详见 `ResourcePruning.md`、`DungeonSceneSplit.md` 和 `GitMaintenancePlan.md`。远端历史尚未重写；增加忽略规则、删除当前文件或仅从新提交开始使用 LFS，都不会消除旧历史中的完整 blob。

如果后续决定迁移历史，应按以下步骤执行：

1. 明确目标分支、标签和文件清单；优先评估上表三个仍在使用的二进制文件，当前版本合计约 179.82 MiB。场景文本保留差异审阅和 Unity 合并能力。
2. 确认 GitHub LFS 可用额度与其他克隆使用情况，约定暂停推送的时间窗口。
3. 在工作仓库之外保存远端镜像备份、所有分支和标签的旧 SHA；另行备份本地未提交文件和需要保留的编辑器快照。验证备份可读后，在独立副本中演练迁移。
4. 比较迁移前后工作树内容哈希，执行 `git lfs fsck`，检查 LFS 对象上传，并用全新克隆验证资源检出和 Unity 导入。
5. 审阅提交映射和验证结果后，再切换远端引用；通知其他克隆重新克隆或按映射迁移，防止重新推回旧历史。
6. 保留旧镜像及引用清单作为恢复点；出现缺失对象时停止进一步写入，并按保存的旧 SHA 恢复受影响引用。

LFS 迁移流程参考 [GitHub 官方说明](https://docs.github.com/en/repositories/working-with-files/managing-large-files/moving-a-file-in-your-repository-to-git-large-file-storage)。

## 本地对象库维护记录

2026-10-26，使用 `git verify-pack` 校验全部三个 pack 后，执行 `git prune-packed` 清理 4,931 个已存于 pack 的重复松散对象。松散对象占用由 508,774 KiB 降到 2,176 KiB，释放约 494.72 MiB；所有引用保持一致，`git fsck --connectivity-only --no-dangling` 通过。

这项清理只影响本机重复存储，不减少远端历史体积。分支、标签、reflog 和 Codex 快照引用均保留。后续可先用 `git count-objects -vH` 和 `git prune-packed --dry-run` 检查；不要为缩减体积直接删除 `.git/refs`、过期 reflog 或执行即时强制清理。命令含义见 [Git prune-packed](https://git-scm.com/docs/git-prune-packed)。

参考：[Git attributes](https://git-scm.com/docs/gitattributes)、[Unity Smart Merge](https://docs.unity3d.com/6000.0/Documentation/Manual/SmartMerge.html)、[GitHub 大文件说明](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github)。
