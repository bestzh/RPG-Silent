namespace RPGSilent.Domain
{
    /// <summary>
    /// 传送门服务：管理 PortalUI 显隐；打开期间禁用玩家输入并显示光标，关闭后恢复。
    /// 由 SceneLifetimeScope 注册，仅在游戏场景内生效。
    /// </summary>
    public interface IPortalService
    {
        bool IsOpen { get; }

        void OpenPortal(int portalId);
        void ClosePortal();
    }
}
