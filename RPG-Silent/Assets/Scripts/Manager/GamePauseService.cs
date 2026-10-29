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
    private readonly DungeonRunService _dungeonRun;

    private bool _inPauseMode;
    private bool _settingsFromPause;

    public bool IsPaused => _uiService.IsUIOpen(PauseUiKey);

    public GamePauseService(
        IUIService uiService,
        IPlayerInputActions playerInputActions,
        ICursorService cursorService, DungeonRunService dungeonRun)
    {
        _uiService          = uiService;
        _playerInputActions = playerInputActions;
        _cursorService      = cursorService;
        _dungeonRun = dungeonRun;
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
        if (_dungeonRun.IsLongChallenge && _dungeonRun.LongChallenge != null && _dungeonRun.LongChallenge.Phase != LongChallengePhase.Fighting) return;
        if (_dungeonRun.HasResult) return;
        if (_uiService.IsUIOpen("UI/LoadingUI")) return;
        if (_inPauseMode) Resume();
        else              Pause();
    }

    public void Pause()
    {
        if (_dungeonRun.HasResult) return;
        if (_uiService.IsUIOpen("UI/LoadingUI")) return;
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
        if (_dungeonRun.HasResult) return;
        if (_uiService.IsUIOpen("UI/LoadingUI")) return;
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
        _playerInputActions.SetGameplayInputBlocked(this, true);
        _cursorService.EnterUICursor();
    }

    private void ExitPauseMode()
    {
        if (!_inPauseMode) return;

        _inPauseMode = false;
        _playerInputActions.SetGameplayInputBlocked(this, false);
        if (_playerInputActions.GameplayInputEnabled)
            _cursorService.EnterGameplayCursor(resetToHidden: true);
        else
            _cursorService.EnterUICursor();
    }

    private void OnPauseTriggered()
    {
        foreach (var loot in PlayerLootInteraction.Active)
            if (loot.IsListOpen) { loot.CloseList(); return; }
        if (_dungeonRun.LeaveConfirmationPending) { _dungeonRun.CancelLeave(); return; }
        var dungeonHud = UnityEngine.Object.FindFirstObjectByType<DungeonUI>();
        if (dungeonHud != null && dungeonHud.HandleBack()) return;
        var questInteraction = UnityEngine.Object.FindFirstObjectByType<PlayerQuestInteraction>();
        if (_uiService.IsUIOpen(PlayerQuestInteraction.UiKey) || questInteraction != null && questInteraction.IsOpen)
        {
            var journal = UnityEngine.Object.FindFirstObjectByType<QuestJournalUI>();
            if (journal == null || !journal.HandleBack())
            {
                questInteraction?.Close();
                _uiService.CloseUI(PlayerQuestInteraction.UiKey);
            }
            return;
        }
        var inventoryInteraction = UnityEngine.Object.FindFirstObjectByType<PlayerInventoryInteraction>();
        if (_uiService.IsUIOpen(PlayerInventoryInteraction.UiKey) || inventoryInteraction != null && inventoryInteraction.IsOpen)
        {
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryUI>();
            if (inventory == null || !inventory.HandleBack())
            {
                inventoryInteraction?.Close();
                _uiService.CloseUI(PlayerInventoryInteraction.UiKey);
            }
            return;
        }
        var npcInteraction = UnityEngine.Object.FindFirstObjectByType<PlayerNpcInteraction>();
        if (_uiService.IsUIOpen("UI/TownNpcUI") || npcInteraction != null && npcInteraction.Conversation != null)
        {
            var npcUi = UnityEngine.Object.FindFirstObjectByType<TownNpcUI>();
            if (npcUi == null || !npcUi.HandleServiceBack())
            {
                npcInteraction?.Close();
                _uiService.CloseUI("UI/TownNpcUI");
            }
            return;
        }
        var hud = UnityEngine.Object.FindFirstObjectByType<MainUI>();
        if (hud != null && hud.HandleTutorialBack()) return;
        if (_dungeonRun.Succeeded && !_dungeonRun.ResultVisible) { _dungeonRun.ShowResult(); return; }
        if (_dungeonRun.HasResult) return;
        if (_uiService.IsUIOpen("UI/LoadingUI")) return;
        if (_uiService.IsUIOpen(SettingsUiKey))
        {
            var settings = UnityEngine.Object.FindFirstObjectByType<SettingsUI>();
            if (settings != null)
            {
                settings.HandleBack();
                if (!_uiService.IsUIOpen(SettingsUiKey)) _settingsFromPause = false;
                return;
            }
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
