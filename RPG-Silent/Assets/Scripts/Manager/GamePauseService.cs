using System;
using RPGSilent.Domain;
using UnityEngine;

/// <summary>
/// 暂停菜单服务：响应 ESC 输入，开关 PauseUI；暂停期间禁用玩家输入并显示光标。
/// 由 SceneLifetimeScope 以 Scoped 生命周期注册，随游戏场景卸载而销毁。
/// </summary>
public class GamePauseService : IGamePauseService, IDisposable
{
    private const string PauseUiKey    = "UI/PauseUI";
    private const string SettingsUiKey = "UI/SettingsUI";
    private const string MainUiKey     = "UI/MainUI";

    private readonly IUIService          _uiService;
    private readonly IPlayerInputActions _playerInputActions;
    private readonly ICursorService      _cursorService;

    private bool _inPauseMode;
    private bool _settingsFromPause;

    public bool IsPaused => _uiService.IsUIOpen(PauseUiKey);

    public GamePauseService(
        IUIService uiService,
        IPlayerInputActions playerInputActions,
        ICursorService cursorService)
    {
        _uiService          = uiService;
        _playerInputActions = playerInputActions;
        _cursorService      = cursorService;
        _playerInputActions.PauseTriggered += OnPauseTriggered;
    }

    public void Dispose()
    {
        if (_playerInputActions != null)
            _playerInputActions.PauseTriggered -= OnPauseTriggered;

        Resume();
    }

    public void Toggle()
    {
        if (_inPauseMode) Resume();
        else              Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;

        _settingsFromPause = false;
        EnterPauseMode();
        _uiService.SetRaycastEnabled(MainUiKey, false);
        _uiService.OpenUI(PauseUiKey);
        Debug.Log("[GamePauseService] 暂停菜单已打开。");
    }

    public void Resume()
    {
        _settingsFromPause = false;
        _uiService.CloseUI(SettingsUiKey);
        _uiService.CloseUI(PauseUiKey);
        _uiService.SetRaycastEnabled(MainUiKey, true);

        if (!_inPauseMode) return;

        ExitPauseMode();
        Debug.Log("[GamePauseService] 暂停菜单已关闭。");
    }

    public void OpenSettings()
    {
        if (!_inPauseMode) return;

        _settingsFromPause = true;
        _uiService.CloseUI(PauseUiKey);
        _uiService.OpenUI(SettingsUiKey, PauseUiKey);
        Debug.Log("[GamePauseService] 从暂停菜单打开设置。");
    }

    private void EnterPauseMode()
    {
        if (_inPauseMode) return;

        _inPauseMode = true;
        _playerInputActions.SetGameplayInputEnabled(false);
        _cursorService.EnterUICursor();
    }

    private void ExitPauseMode()
    {
        if (!_inPauseMode) return;

        _inPauseMode = false;
        _playerInputActions.SetGameplayInputEnabled(true);
        _cursorService.EnterGameplayCursor(resetToHidden: true);
    }

    private void OnPauseTriggered()
    {
        if (_uiService.IsUIOpen(SettingsUiKey))
        {
            bool returnToPause = _settingsFromPause;
            _uiService.CloseUI(SettingsUiKey);
            _settingsFromPause = false;

            if (returnToPause)
                _uiService.OpenUI(PauseUiKey);

            return;
        }

        Toggle();
    }
}
