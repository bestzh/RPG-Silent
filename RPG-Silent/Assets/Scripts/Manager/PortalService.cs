using RPGSilent.Domain;
using UnityEngine;

/// <summary>
/// 传送门服务：响应传送门触发器，开关 PortalUI；
/// 打开期间禁用玩家输入并显示光标，关闭（含进入副本）时恢复。
/// 由 SceneLifetimeScope 以 Scoped 生命周期注册，随游戏场景卸载而销毁。
/// </summary>
public class PortalService : IPortalService
{
    private const string PortalUiKey = "UI/PortalUI";
    private const string MainUiKey   = "UI/MainUI";

    private readonly IUIService          _uiService;
    private readonly IPlayerInputActions _playerInputActions;
    private readonly ICursorService      _cursorService;

    private bool _inUIMode;

    public bool IsOpen => _uiService.IsUIOpen(PortalUiKey);

    public PortalService(
        IUIService uiService,
        IPlayerInputActions playerInputActions,
        ICursorService cursorService)
    {
        _uiService          = uiService;
        _playerInputActions = playerInputActions;
        _cursorService      = cursorService;
    }

    public void OpenPortal(int portalId)
    {
        if (IsOpen) return;

        EnterUIMode();
        _uiService.SetRaycastEnabled(MainUiKey, false);
        _uiService.OpenUI(PortalUiKey, portalId);
        Debug.Log($"[PortalService] 打开传送门 UI，portalId={portalId}");
    }

    public void ClosePortal()
    {
        if (!IsOpen && !_inUIMode) return;

        _uiService.CloseUI(PortalUiKey);
        _uiService.SetRaycastEnabled(MainUiKey, true);
        ExitUIMode();
        Debug.Log("[PortalService] 关闭传送门 UI");
    }

    private void EnterUIMode()
    {
        if (_inUIMode) return;

        _inUIMode = true;
        _playerInputActions.SetGameplayInputEnabled(false);
        _cursorService.EnterUICursor();
    }

    private void ExitUIMode()
    {
        if (!_inUIMode) return;

        _inUIMode = false;
        _playerInputActions.SetGameplayInputEnabled(true);
        _cursorService.EnterGameplayCursor(resetToHidden: true);
    }
}
