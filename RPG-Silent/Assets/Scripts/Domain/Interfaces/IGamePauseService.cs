namespace RPGSilent.Domain
{
    /// <summary>
    /// 暂停菜单服务：管理 PauseUI 显隐；暂停期间禁用玩家输入并显示光标。
    /// 由 SceneLifetimeScope 注册，仅在游戏场景内生效。
    /// </summary>
    public interface IGamePauseService
    {
        bool IsPaused { get; }

        void Toggle();
        void Pause();
        void Resume();
        void OpenSettings();
    }
}
