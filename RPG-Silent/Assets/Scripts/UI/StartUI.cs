using RPGSilent.Domain;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class StartUI : UIBase
{
    [Inject]
    private IUIService _uiService;
    [Inject]
    private PlayerSessionService _playerSession;
    [Inject]
    private PlayerProgressService _progress;
    [Inject]
    private IPlayerInputActions _input;
    private bool _starting;
    private CharacterCreationView classSelection;
    private CharacterSelectionView characterSelection;
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
            if (_starting)
                return;
            StartNewGame();
        });
        ConfirmNewGameButton?.onClick.AddListener(StartNewGame);
        CancelNewGameButton?.onClick.AddListener(() => NewGameConfirmation.SetActive(false));
        ContinueButton?.onClick.AddListener(() =>
        {
            if (_starting)
                return;
            OpenCharacters();
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
        if (_starting)
            return;
        if (characterSelection != null)
            characterSelection.gameObject.SetActive(false);
        NewGameConfirmation.SetActive(false);
        if (classSelection != null)
        {
            classSelection.gameObject.SetActive(true);
            return;
        }

        var overlay = UIView.Panel(transform, "ClassSelection");
        classSelection = overlay.gameObject.GetComponent<CharacterCreationView>();
        overlay.gameObject.SetActive(true);
        classSelection.Initialize(_progress.Classes, SaveStatusText.font, SelectCharacter, () => classSelection.gameObject.SetActive(false));
    }

    public void SelectClass(string classId) => SelectCharacter(classId, PlayerGender.Male);
    public void SelectCharacter(string classId, PlayerGender gender)
    {
        if (_starting || !_progress.Classes.Any(c => c.Id == classId && !c.IsAdvanced))
            return;
        if (!_playerSession.TryBeginNewCharacter(classId, gender, out string message))
        {
            SaveStatusText.text = message;
            classSelection?.ShowFailure(message);
            return;
        }

        LoadMain();
    }

    public void OpenCharacters()
    {
        if (_starting)
            return;
        if (classSelection != null)
            classSelection.gameObject.SetActive(false);
        if (characterSelection != null)
        {
            characterSelection.gameObject.SetActive(true);
            characterSelection.Refresh();
            return;
        }

        var overlay = UIView.Panel(transform, "CharacterSelection");
        characterSelection = overlay.gameObject.GetComponent<CharacterSelectionView>();
        overlay.gameObject.SetActive(true);
        characterSelection.Initialize(_progress, SaveStatusText.font, ContinueCharacter, StartNewGame, () => characterSelection.gameObject.SetActive(false));
    }

    public void ContinueCharacter(string key)
    {
        if (_starting)
            return;
        if (!_playerSession.BeginContinueCharacter(key, out string message))
        {
            RefreshSaveState();
            SaveStatusText.text = message;
            characterSelection?.ShowFailure(message);
            return;
        }

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
        var characters = _progress.GetCharacters(out string message);
        ContinueButton.interactable = characters.Length > 0 && !_starting;
        ContinueButton.GetComponentInChildren<TMP_Text>().text = "选择角色";
        StartButton.GetComponentInChildren<TMP_Text>().text = "创建角色";
        SaveStatusText.text = characters.Length > 0 ? $"已有 {characters.Length} 个角色。选择角色继续冒险，创建新角色不会覆盖进度。" : "创建角色，选择职业与外观后开始冒险。";
        if (!string.IsNullOrEmpty(message))
            SaveStatusText.text += "\n" + message;
        if (_progress.HasUnsavedChanges)
            SaveStatusText.text += "\n上一局存在未保存的进度。";
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        _input.PauseTriggered -= OnBackTriggered;
        _input.PauseTriggered += OnBackTriggered;
        _starting = false;
        if (classSelection != null)
            classSelection.gameObject.SetActive(false);
        if (characterSelection != null)
            characterSelection.gameObject.SetActive(false);
        NewGameConfirmation.SetActive(false);
        RefreshSaveState();
        Debug.Log("[StartUI] 已打开。");
    }

    public override void OnClose()
    {
        _input.PauseTriggered -= OnBackTriggered;
        if (classSelection != null)
            classSelection.gameObject.SetActive(false);
        if (characterSelection != null)
            characterSelection.gameObject.SetActive(false);
        base.OnClose();
        Debug.Log("[StartUI] 已关闭。");
    }

    private void OnBackTriggered()
    {
        if (_uiService.IsUIOpen("UI/SettingsUI"))
            UnityEngine.Object.FindFirstObjectByType<SettingsUI>()?.HandleBack();
    }

    private void OnDestroy()
    {
        if (_input != null)
            _input.PauseTriggered -= OnBackTriggered;
    }
}
