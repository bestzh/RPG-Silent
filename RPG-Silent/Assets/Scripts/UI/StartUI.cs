using RPGSilent.Domain;
using System.Linq;
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
    private CharacterCreationView classSelection;

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
        if (classSelection != null) { classSelection.gameObject.SetActive(true); return; }
        var overlay = ProgressionUIWidgets.Panel(transform, "ClassSelection", new Color(.025f, .04f, .055f, 1));
        classSelection = overlay.gameObject.AddComponent<CharacterCreationView>();
        classSelection.Initialize(_progress.Classes, SaveStatusText.font, SelectCharacter,
            () => classSelection.gameObject.SetActive(false));
    }

    public void SelectClass(string classId) => SelectCharacter(classId, PlayerGender.Male);
    public void SelectCharacter(string classId, PlayerGender gender)
    {
        if (_starting || !_progress.Classes.Any(c => c.Id == classId && !c.IsAdvanced)) return;
        _playerSession.BeginNewCharacter(classId, gender);
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
        if (classSelection != null) classSelection.gameObject.SetActive(false);
        NewGameConfirmation.SetActive(false);
        RefreshSaveState();
        Debug.Log("[StartUI] 已打开。");
    }

    public override void OnClose()
    {
        if (classSelection != null) classSelection.gameObject.SetActive(false);
        base.OnClose();
        Debug.Log("[StartUI] 已关闭。");
    }
}
