using RPGSilent.Domain;
using UnityEngine;

/// <summary>
/// 一局游戏的玩家所有者。游戏场景之间保留 rig，返回菜单时结束，新游戏重置数据。
/// 设置和输入服务仍由全局容器保留。
/// </summary>
public sealed class PlayerSessionService
{
    private readonly PlayerStats _stats;
    private readonly IPlayerInputActions _input;
    private readonly ICursorService _cursor;
    private readonly IUIService _ui;
    private readonly PlayerProgressService _progress;

    public GameObject PlayerRig { get; private set; }
    public bool IsActive { get; private set; }

    public PlayerSessionService(PlayerStats stats, IPlayerInputActions input,
        ICursorService cursor, IUIService ui, PlayerProgressService progress)
    {
        _stats = stats;
        _input = input;
        _cursor = cursor;
        _ui = ui;
        _progress = progress;
    }

    public void BeginNewGame()
        => BeginNewGameAs("swordsman");

    public void BeginNewGameAs(string classId) => BeginNewCharacter(classId, PlayerGender.Male);
    public void BeginNewCharacter(string classId, PlayerGender gender)
    {
        if (!TryBeginNewCharacter(classId, gender, out string message)) throw new System.InvalidOperationException(message);
    }

    public bool TryBeginNewCharacter(string classId, PlayerGender gender, out string message)
    {
        if (IsActive && !_progress.Save()) { message = _progress.StatusMessage; return false; }
        EndSession();
        if (!_progress.TryBeginNewCharacter(classId, gender, out message)) return false;
        IsActive = true;
        _input.SetGameplayInputEnabled(true);
        _cursor.EnterGameplayCursor();
        return true;
    }

    public bool BeginContinueGame(out string message)
    {
        if (IsActive && !_progress.Save()) { message = _progress.StatusMessage; return false; }
        EndSession();
        if (!_progress.BeginContinue(out message)) return false;
        IsActive = true;
        _input.SetGameplayInputEnabled(true);
        _cursor.EnterGameplayCursor();
        return true;
    }

    public bool BeginContinueCharacter(string key, out string message)
    {
        if (IsActive && !_progress.Save()) { message = _progress.StatusMessage; return false; }
        EndSession();
        if (!_progress.BeginContinueCharacter(key, out message)) return false;
        IsActive = true;
        _input.SetGameplayInputEnabled(true);
        _cursor.EnterGameplayCursor();
        return true;
    }

    public void OnPlayerReady() => _progress.Attach(PlayerRig);

    public void RegisterPlayerRig(GameObject rig)
    {
        if (PlayerRig != null && PlayerRig != rig)
            throw new System.InvalidOperationException("当前玩家会话已经拥有一个 rig。");

        PlayerRig = rig;
    }

    public void EndSession()
    {
        _progress.Detach();
        _ui.CloseUI(PlayerQuestInteraction.UiKey);
        _ui.CloseUI(PlayerInventoryInteraction.UiKey);
        _ui.CloseUI("UI/MainUI");
        _ui.CloseUI("UI/TownNpcUI");
        _ui.CloseUI("UI/PortalUI");
        _ui.CloseUI("UI/PauseUI");
        _ui.CloseUI("UI/SettingsUI");

        if (PlayerRig != null)
        {
            // 立即停用玩家、相机和输入订阅，不等帧末销毁。
            PlayerRig.SetActive(false);
            Object.Destroy(PlayerRig);
            PlayerRig = null;
        }

        IsActive = false;
        DungeonLaunchContext.Clear();
        _input.SetGameplayInputEnabled(false);
        _cursor.EnterUICursor();
    }
}
