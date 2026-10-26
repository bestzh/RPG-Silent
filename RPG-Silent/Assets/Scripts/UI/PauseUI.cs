using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using TMPro;

public class PauseUI : UIBase
{
    private IUIService        _uiService;
    private IGamePauseService _gamePauseService;
    [Inject] private PlayerProgressService _progress;

    public Button ResumeButton;
    public Button SettingsButton;
    public Button MainMenuButton;
    public Button QuiteButton;
    public Button SaveButton;
    public TMP_Text SaveStatusText;

    [Inject]
    public void Construct(IUIService uiService, IGamePauseService gamePauseService)
    {
        _uiService        = uiService;
        _gamePauseService = gamePauseService;
    }

    private void Awake()
    {
        ResumeButton?.onClick.AddListener(OnResumeClicked);
        SettingsButton?.onClick.AddListener(OnSettingsClicked);
        MainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        QuiteButton?.onClick.AddListener(OnQuitClicked);
        SaveButton?.onClick.AddListener(() => { _progress.Save(); RefreshSaveStatus(); });
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        _progress.Changed -= RefreshSaveStatus;
        _progress.Changed += RefreshSaveStatus;
        RefreshSaveStatus();
        Debug.Log("[PauseUI] 已打开。");
    }

    public override void OnClose()
    {
        base.OnClose();
        _progress.Changed -= RefreshSaveStatus;
        Debug.Log("[PauseUI] 已关闭。");
    }

    private void RefreshSaveStatus() { if (SaveStatusText != null) SaveStatusText.text = _progress.StatusMessage; }
    private void OnDestroy() { if (_progress != null) _progress.Changed -= RefreshSaveStatus; }

    private void OnResumeClicked()
    {
        Debug.Log("[PauseUI] 点击返回游戏");
        _gamePauseService.Resume();
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[PauseUI] 点击设置");
        _gamePauseService.OpenSettings();
    }

    private void OnMainMenuClicked()
    {
        if (!_progress.Save()) { RefreshSaveStatus(); return; }
        Debug.Log("[PauseUI] 点击返回主菜单");
        _uiService.SetRaycastEnabled("UI/MainUI", true);
        _gamePauseService.Resume();
        // 加载成功后由菜单入口结束会话；失败时保留玩家，以便返回原游戏。
        // StartScene 的 GameStart 是菜单界面的唯一打开入口。
        _uiService.OpenUI("UI/LoadingUI", "Scenes/StartScene", string.Empty);
    }

    private void OnQuitClicked()
    {
        if (!_progress.Save()) { RefreshSaveStatus(); return; }
        Debug.Log("[PauseUI] 点击退出游戏");
        _uiService.SetRaycastEnabled("UI/MainUI", true);
        _gamePauseService.Resume();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
