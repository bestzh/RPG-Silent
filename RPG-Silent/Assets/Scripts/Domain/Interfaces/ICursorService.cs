namespace RPGSilent.Domain
{
    /// <summary>
    /// 光标状态服务：游戏中默认隐藏，Ctrl 切换；UI 模式（暂停菜单等）强制显示。
    /// </summary>
    public interface ICursorService
    {
        bool IsUICursor { get; }

        void EnterGameplayCursor(bool resetToHidden = true);
        void EnterUICursor();
        void ToggleGameplayCursor();
    }
}
