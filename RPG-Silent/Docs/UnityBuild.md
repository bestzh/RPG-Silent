# Windows 构建与验证

固定版本来自 `ProjectSettings/ProjectVersion.txt`，当前为 Unity 6000.0.66f2。构建目标为 Windows64，使用现有 Mono 后端和 PlayerSettings 版本号。

在退出 Play Mode、保存或撤销场景修改后，使用 `Tools > RPG Silent > Build Windows64`。入口先验证版本、平台和场景，再显式构建 Addressables 和 Windows Player。结果写到忽略目录 `Builds/Windows64`，`build-report.json` 记录成功/失败、版本、场景、大小、警告和错误。

## Windows 批处理

先关闭该工程的 Unity 编辑器，然后在工程目录执行：

```powershell
./Tools/Validation/Invoke-UnityValidation.ps1 -UnityEditorPath 'E:/unityEditor/unity6000.0.66/Editor/Unity.exe'
./Tools/Validation/Invoke-UnityValidation.ps1 -UnityEditorPath 'E:/unityEditor/unity6000.0.66/Editor/Unity.exe' -BuildWindows
./Tools/Validation/Invoke-UnityValidation.ps1 -UnityEditorPath 'E:/unityEditor/unity6000.0.66/Editor/Unity.exe' -BuildWindows -SmokeStartup
```

脚本拒绝使用被编辑器占用的工程。测试必须全部通过且非空，才会进入构建；构建状态必须为 Succeeded 且错误数为 0，失败返回非零状态。批处理在 Addressables 构建后明确加载已保存的启动场景，满足 Opsive 预制体验证器恢复场景的要求。日志与 NUnit XML 位于 `Logs/Validation/<UTC时间>/`，独立构建产物位于 `Builds/Validation/<UTC时间>/`，包括每个产物的 SHA-256。`validation.json` 记录提交和未提交文件，避免把工作区验证误认为某个提交的验证。

TMP 会在构建和编辑器退出时清理动态字体缓存。批处理先备份 TMP 字体资产、PlayerSettings、SBP 设置及已有 Addressables state，在 Unity 进程退出后按文件哈希恢复变动；备份保存在本次日志的 `source-backup/`，不使用会被 Unity 删除的 `Temp/`。本次构建的 Addressables state 另存到产物中的 `AddressablesState/<平台>/`，供该版本后续内容更新使用。编辑器菜单构建不包含这个外部恢复步骤，可能产生字体缓存改动；日常交付优先使用批处理。

`-SmokeStartup` 会启动本次独立 Player 30 秒，确认 StartUI 日志、未提前退出和无异常模式，再停止本次进程并保存 `player-startup.json`。该选项必须与 `-BuildWindows` 同用。脚本不会自动发布、打标签或覆盖玩家存档；实际操作、玩法和画面仍需单独验收。

## 验证记录

### 后续总清单 16 的当前预检与预算（2026-10-29）

当前尚未生成包含总清单 01–13 与 V5 UI 迁移的最终 Windows 包；下方历史构建不能代替本次交付。共享编辑器正由 UI 聊天进行页面与分辨率验证，完成后再恢复实际游玩、长期运行和构建。

`Tools/Validation/DeliveryContentPreflight.py` 只读检查保存的地址配置与序列化 GUID 图：15 个必需地址全部存在，地址无重复，排除不导入的包 `Samples~` 后 GUID 无冲突；当前源引用图可达 29 张图标，完整图标包为 8474 张。源图约 1.23 GB，不代表构建体积或运行内存。277 组未解析的“文件／GUID”候选涉及 19 个 GUID，进一步定位在 `DeliveryUnresolvedGuidSites.json`：多数为动画导入器 `maskSource`，另有材质纹理、旧示例 UI 脚本、源角色控制器及多对象网格资产中的作者工具脚本字段。扫描未发现直接的 `m_Shader` 候选，但不涵盖 Unity 的旧程序集脚本重映射、子资产实际使用与运行时控制器替换。需要用 Unity 实际依赖／构建报告判断，不能据此直接删除或替换资源。完整快照在 `Tools/Validation/DeliveryContentPreflight.json`，并行 UI 修改后须重跑。

本机作为首轮参考机器：i7-12700、32 GB 系统内存、RTX 3060 Ti（编辑器报告约 8 GB 显存）、Windows 10 19045／D3D11。硬件来自本机处理器注册表与 Unity 启动日志；这不是已验证的最低配置。暂定独立客户端验收预算如下，实际结果和超预算原因须随同构建记录：

| 条件 | 首轮预算 |
| --- | --- |
| 1920×1080、Medium，暖机后的主城与正常战斗 | 至少 60 FPS；帧间隔 P95 ≤20 ms、P99 ≤33.3 ms |
| 压力场景：大量怪物、特效与掉落同时存在 | 中位数至少 45 FPS、P95 ≤33.3 ms；记录对象数量和实际瓶颈 |
| 普通场景进出 | 暖缓存加载 ≤5 秒；首次加载单列，不混入帧率统计 |
| 稳态进程私有内存 | ≤3 GB；场景往返 20 次并收敛后增长 ≤150 MB |
| 角色预览与 UI 反复开关 | 模型、RenderTexture、UI 实例和资源句柄数量回到稳定范围；不得逐轮累积 |

帧率、加载、内存和对象释放目前均未完成本次独立客户端测量。Editor、MCP 动态程序集和离线源文件大小不用于宣称达到这些预算；压力夹具另列，完整自然操作流程仍要覆盖创建角色、主城服务、双副本、通关回城、退出与继续。

### 独立客户端验证入口暂存（尚未导入或运行）

`BuildStandaloneValidationDriver.py` 从已准备的正常输入脚本生成 `Tools/Validation/StandaloneValidationDriver.cs.txt`。当前离线编译 0 错误、50 条原夹具未使用变量／程序集版本绑定警告（30 条 CS0219、20 条 CS1701）；未启动 Unity 或 Player。编译器验证 release、仅 development、仅 validation 三种不完整条件均不产生验证类型，只有 `DEVELOPMENT_BUILD && RPG_SILENT_VALIDATION` 同时成立才编译此入口。证据为 `StandaloneValidationDriver-Compile.json` 与 `StandaloneValidationDriver-Audit.json`。

入口使用正常 InputSystem 行走、鼠标指针／射线检测、E 交互、拾取、技能与敌人战斗；不直接施加伤害、治疗、发奖或调用按钮回调。当前包含 `native-chest` 与 `native-cave` 两个定向案例，不能合并宣称已经完成新角色连续双副本或退出重启的完整客户端验收。

运行前须等共享编辑器交回，再应用暂存的 `StandaloneValidationBootstrap.patch`，把生成文件导入 `Assets/Scripts/Validation/StandaloneValidationDriver.cs`，生成最新 Addressables 与显式验证构建。`BuildPlayerOptions.extraScriptingDefines` 只为该验证构建加入 `RPG_SILENT_VALIDATION`，同时使用 Development 选项；验证产品 company 必须临时设为 `RPGSilent.Validation.<本次编号>` 以隔离 PlayerPrefs，构建后恢复源设置。普通交付构建不加入此符号。当前补丁只做过 `git apply --check`，实际 `GameLifetimeScope` 尚未修改。

客户端还需显式传入五个独立参数：`--rpg-validation-case`、`--rpg-validation-workspace`、`--rpg-validation-saves`、`--rpg-validation-output` 及各自值。工作区必须为源项目绝对路径，保存与输出目录必须是其 `Builds/Validation` 下不同的子目录；输出目录必须尚不存在。补给箱案例要求空存档，洞窟案例需先在独立目录复制哈希验证过的自然所得检查点。参数重复、相对路径、父目录逃逸、同前缀兄弟目录、真实玩家目录和目录联接均拒绝；18 项原方法 Windows .NET 检查通过，未打开真实玩家文件。检查不替代 Unity 启动、命令行解析整体和实际保存验证。

采集器只在验证进程内记录错误、内存／对象数量和真实墙钟帧间隔，排除首次 10 秒、加载及暂停边界；错误记录限制 64 条但保留总数，诊断写入失败仍以失败退出。开发构建与自动输入的数值单列，不能作为交付构建性能达标证明。Windows computer-use 接口仅完成只读窗口枚举（`DeliveryWindowPreflight.json`），未激活窗口、发出输入或启动客户端；实际可视检查须在最新程序创建后重新选择窗口，不能复用快照中的临时句柄。

- 2026-10-26，运行 `20261026T142107379Z`：27/27 EditMode 测试通过，0 失败、0 跳过；Windows64 构建成功，0 错误、1 警告，产物 517,500,484 字节。Addressables 与 Player 构建共 38.68 秒，完整验证约 78.52 秒（已有本机缓存）。
- 唯一警告为 `LongCang-Regular SDF.asset` 的 NativeFormatImporter 一致性警告；已确认 TMP 构建前自动清理动态字形缓存，批处理结束后源字体文件与运行前一致。该警告仍保留在 `build-report.json`，不将其隐去。
- 实际报告位于 `Logs/Validation/20261026T142107379Z/validation.json`，构建和 SHA-256 清单位于同编号的 `Builds/Validation/`。本次基线为 `bf13d81e` 加当时尚未提交的构建入口与脚本，不能作为仅该提交的构建证据。
- 同一产物独立进程启动 30 秒，日志确认 StartUI 打开，未提前退出，未命中异常/崩溃/加载失败错误；证据为同一日志目录的 `player-startup.json` 和 `.log`。这是启动检查，尚未覆盖完整玩家操作和视觉验收。
