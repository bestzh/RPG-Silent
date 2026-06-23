using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class PauseUI : UIBase
{
    private IUIService        _uiService;
    private IGamePauseService _gamePauseService;

    public Button ResumeButton;
    public Button SettingsButton;
    public Button MainMenuButton;
    public Button QuiteButton;

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
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        Debug.Log("[PauseUI] 已打开。");
    }

    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("[PauseUI] 已关闭。");
    }

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
        Debug.Log("[PauseUI] 点击返回主菜单");
        _uiService.SetRaycastEnabled("UI/MainUI", true);
        _gamePauseService.Resume();
        _uiService.CloseUI("UI/MainUI");
        _uiService.OpenUI("UI/LoadingUI", "Scenes/StartScene", "UI/StartUI");
    }

    private void OnQuitClicked()
    {
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
