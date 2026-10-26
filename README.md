# RPG-Silent

Unity 工程位于 `RPG-Silent/`，请使用 `RPG-Silent/ProjectSettings/ProjectVersion.txt` 指定的编辑器版本打开。

- [项目上下文](RPG-Silent/Docs/UnityProjectContext.md)
- [架构说明](RPG-Silent/ARCHITECTURE.md)
- [Git 管理、提交检查与 CI](RPG-Silent/Docs/GitWorkflow.md)
- [敌人及副本配置](RPG-Silent/Docs/EnemyEncounter.md)

首次克隆后运行 `python RPG-Silent/Tools/Git/install_hooks.py`，此后提交时自动检查暂存区。`main` 通过 PR 合并，必须通过 `Git hygiene` 检查。
