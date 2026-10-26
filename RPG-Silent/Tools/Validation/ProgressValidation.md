# H09 MCP 验证

所有 `.cs.txt` 文件均为 Unity MCP `execute_code` 的 C# 6 方法体，不参与游戏程序集编译。需要先确认 Unity 已编译、无正在运行的其他验证脚本，保留用户场景修改。请勿在真实存档路径执行奖励或装配测试。

## 隔离目录

在 **Edit Mode、StartScene** 中执行，再进入 Play Mode：

```csharp
UnityEditor.SessionState.SetString("RPGSilent.Validation.SaveDirectory",
    System.IO.Path.GetFullPath("Temp/CodexProgressValidation/run-" + System.Guid.NewGuid().ToString("N")));
UnityEngine.Application.runInBackground = true;
```

该覆盖仅 Editor 构建存在，VContainer 创建时读取。运行中改 SessionState 不会改变现有服务的路径；每个独立回归使用新目录。真实存档默认在 `Application.persistentDataPath/Saves/`，设置/改键仍使用原存储，两者独立。

## 执行顺序

1. Edit Mode 执行 `ProgressStorageMcp.cs.txt`：34 项纯成长/真实文件系统检查，自建唯一 Temp 目录。
2. 进入 StartScene Play Mode，用实际 StartUI.StartButton 开始新游戏；到达 Main 后执行 `ProgressFlowMcp.cs.txt`。查询 AppDomain `H09.flowResult`，应为 COMPLETE 25；运行钩子键 `H09.flowTick` 完成后为空。
3. 停止 Play Mode，**保留同一隔离目录**，从 StartScene 再进入 Play Mode。执行 `ProgressRestartMcp.cs.txt`；查询 `H09.restartResult`，应为 COMPLETE 26，`H09.restartTick` 清空。脚本读取前一步生成的 `expected-restart.json`。包含刻意移除 Addressables locator 的一次 Main 加载失败，预期出现 InvalidKeyException 与 SceneLoader 错误；恢复逻辑通过真实失败页返回按钮验证。
4. 独立新目录回归 `DungeonFlowMcp.cs.txt`（StartScene）、`DungeonBoundariesMcp.cs.txt`（Main）和 `SceneLoadingMcp.cs.txt`（独立新 StartScene）。成长后满血/满能量应与 MaxHealth/MaxEnergy 比较；SceneLoading 第二次新游戏会点击覆盖确认。这些脚本已加入 Temp 路径保护。
5. 检查 Game View 的继续游戏、覆盖确认、HUD 的等级/本级经验/满级、保存错误与重试。特殊数字要检查实际显示，不只断言 text 字符串。

运行报告写入 `Temp/CodexProgressValidation/`。测试会临时禁用敌人 AI，并在 finally/完成路径移除 Editor 更新钩子、场景监听、测试输入与文件锁。外部强制打断时先停止 Play Mode，确认无残留文件锁/钩子后重跑。

## 收尾与边界

通过实际暂停菜单返回 StartScene，确认玩家为 0、全局根为 1、输入阻挡与攻击订阅为 0、DungeonLaunchContext 已清，然后停止 Play Mode：

```csharp
UnityEditor.SessionState.EraseString("RPGSilent.Validation.SaveDirectory");
UnityEngine.Application.runInBackground = false;
UnityEngine.Time.timeScale = 1;
```

检查 Console，区分故障注入日志和新问题；不批量保存所有资产，避免持久化 TMP 动态字库缓存。测试不替代 Unity Test Runner、独立 Windows 包进程重开、断电/磁盘满或其他平台验证。发布前还需 H10 的构建与包外冒烟。
