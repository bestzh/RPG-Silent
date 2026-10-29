# UI 关闭生命周期修复

观察：Main 场景打开传送页后退出 Play Mode，PortalService.Dispose → ClosePortal → UIManager.CloseUI 在 `GetComponent<UIBase>()` 处抛出 MissingReferenceException。此时界面 GameObject 已被 Unity 销毁，但 activeUIs 字典仍保留引用。此前普通打开、切换与交易没有此异常。

原因证据：完整堆栈指向场景容器销毁阶段；CloseUI 只检查字典键存在，没有检查 Unity 已销毁对象。UIManager 既有 TryGetUIGameObject 已使用 Unity 空值语义，但 CloseUI 未覆盖此分支。该分支在接管前的 UIManager 源中已存在。

修复：先移除 activeUIs 所有权，再回调 OnClose，避免交谈页面回调关闭服务时重入同一所有权。遇到已销毁对象时清除缓存与初始化标记，并释放仍有效的残留 Addressables 句柄；不访问对象、不吞异常。正常存活页面继续按既有缓存/释放流程关闭。

回归：新增 `UIManagerLifecycleTests.ClosingAnAlreadyDestroyedSceneViewClearsOwnership`，注入一个随后销毁的页面，要求关闭后活动/缓存记录均清除，并且重复关闭安全。该测试在最终 638 项回归中通过。原传送页打开时退出运行的路径也已复验，Console 为零错误。

同轮 635/636 中唯一失败项是原有 `ProgressSaveTests.RepeatedCorruptRecoveryPreservesBackupAndCanWriteAgain` 的 Windows File.Replace 1175，保存在 `editmode-636-first.json`；此项单独重跑通过，见 `save-stress-retry.json`。未修改存档实现以迁就 UI 验证。
