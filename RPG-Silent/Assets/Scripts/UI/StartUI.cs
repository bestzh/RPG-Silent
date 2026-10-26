using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class StartUI : UIBase
{
    [Inject] private IUIService _uiService;
    [Inject] private PlayerSessionService _playerSession;
    [Inject] private PlayerProgressService _progress;
    private bool _starting;

    public Button StartButton;
    public Button ContinueButton;
    public Button SettingsButton;
    public Button QuiteButton;
    public TMP_Text SaveStatusText;
    public GameObject NewGameConfirmation;
    public Button ConfirmNewGameButton;
    public Button CancelNewGameButton;

    private void Awake()
    {
        StartButton?.onClick.AddListener(() =>
        {
            if (_starting) return;
            if (_progress.HasStoredProgress) NewGameConfirmation.SetActive(true);
            else StartNewGame();
        });

        ConfirmNewGameButton?.onClick.AddListener(StartNewGame);
        CancelNewGameButton?.onClick.AddListener(() => NewGameConfirmation.SetActive(false));
        ContinueButton?.onClick.AddListener(() =>
        {
            if (_starting) return;
            if (!_playerSession.BeginContinueGame(out string message))
            {
                RefreshSaveState();
                SaveStatusText.text = message;
                return;
            }
            LoadMain();
        });

        SettingsButton?.onClick.AddListener(() =>
        {
            Debug.Log("[StartUI] 点击设置");
            _uiService.OpenUI("UI/SettingsUI");
        });

        QuiteButton?.onClick.AddListener(() =>
        {
            Debug.Log("[StartUI] 点击退出游戏");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    private void StartNewGame()
    {
        if (_starting) return;
        NewGameConfirmation.SetActive(false);
        _playerSession.BeginNewGame();
        LoadMain();
    }

    private void LoadMain()
    {
        _starting = true;
        _uiService.OpenUI("UI/LoadingUI", "Scenes/Main");
        _uiService.CloseUI("UI/StartUI");
    }

    private void RefreshSaveState()
    {
        var result = _progress.InspectSave();
        ContinueButton.interactable = result.CanContinue && !_starting;
        SaveStatusText.text = result.Message;
        if (_progress.HasUnsavedChanges) SaveStatusText.text += "\n上一局存在未保存的进度。";
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        _starting = false;
        NewGameConfirmation.SetActive(false);
        RefreshSaveState();
        Debug.Log("[StartUI] 已打开。");
    }

    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("[StartUI] 已关闭。");
    }
}
