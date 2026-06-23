using RPGSilent.Domain;
using UnityEngine;

/// <summary>
/// 管理光标显示/锁定。游戏中默认隐藏；Ctrl 可切换；UI 模式（暂停菜单等）强制显示并禁止切换。
/// </summary>
public class CursorService : ICursorService
{
    private bool _uiMode;
    private bool _gameplayCursorVisible;

    public bool IsUICursor => _uiMode;

    public void EnterGameplayCursor(bool resetToHidden = true)
    {
        _uiMode = false;

        if (resetToHidden)
            _gameplayCursorVisible = false;

        ApplyCursor(_gameplayCursorVisible);
    }

    public void EnterUICursor()
    {
        _uiMode = true;
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ToggleGameplayCursor()
    {
        if (_uiMode) return;

        _gameplayCursorVisible = !_gameplayCursorVisible;
        ApplyCursor(_gameplayCursorVisible);
    }

    private static void ApplyCursor(bool visible)
    {
        Cursor.visible   = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
