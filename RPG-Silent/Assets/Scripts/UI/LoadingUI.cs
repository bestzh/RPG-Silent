using System;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

public class LoadingUI : UIBase
{
    private const string LoadingUiKey = "UI/LoadingUI";
    private const string MenuSceneKey = "Scenes/StartScene";

    [Inject] private IUIService _uiService;
    [Inject] private ISceneLoader _sceneLoader;
    [Inject] private IPlayerInputActions _input;
    [Inject] private ICursorService _cursor;
    [Inject] private PlayerSessionService _playerSession;

    public Slider progressBar;
    public TextMeshProUGUI progressText;
    [SerializeField] private GameObject failurePanel;
    [SerializeField] private TMP_Text failureText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private TMP_Text returnButtonText;

    public bool IsLoading { get; private set; }
    public bool HasFailed { get; private set; }

    private bool _isOpen;
    private int _requestVersion;
    private string _nextScene;
    private string _openUiAfter;
    private Scene _sourceScene;
    private Action _onReturn;
    private string _returnLabel;

    protected override void OnInit()
    {
        retryButton.onClick.AddListener(Retry);
        returnButton.onClick.AddListener(ReturnFromFailure);
        progressBar.interactable = false;
        failurePanel.SetActive(false);
    }

    public override void OnOpen(params object[] args)
    {
        // UIManager 会对已打开界面再次调用 OnOpen；不能覆盖在途请求及其回调。
        if (_isOpen) return;
        base.OnOpen(args);
        _isOpen = true;
        _nextScene = args != null && args.Length > 0 ? args[0] as string : "Scenes/Main";
        _openUiAfter = args != null && args.Length > 1 ? args[1] as string : "UI/MainUI";
        _onReturn = args != null && args.Length > 2 ? args[2] as Action : null;
        _returnLabel = args != null && args.Length > 3 ? args[3] as string : null;
        _sourceScene = SceneManager.GetActiveScene();
        _input.PauseTriggered -= OnBackTriggered;
        _input.PauseTriggered += OnBackTriggered;
        _input.SetGameplayInputBlocked(this, true);
        _cursor.EnterUICursor();
        BeginLoad();
    }

    private void BeginLoad()
    {
        IsLoading = true;
        HasFailed = false;
        failurePanel.SetActive(false);
        progressBar.value = 0f;
        progressText.text = "0%";
        int version = ++_requestVersion;

        _sceneLoader.LoadScene(_nextScene,
            onProgress: progress =>
            {
                if (!IsCurrentRequest(version)) return;
                progressBar.value = progress;
                progressText.text = $"{(int)(progress * 100)}%";
            },
            onComplete: () =>
            {
                if (!IsCurrentRequest(version)) return;
                string openUiAfter = _openUiAfter;
                _onReturn = null;
                if (_nextScene == MenuSceneKey) _playerSession.EndSession();
                _uiService.CloseUI(LoadingUiKey);
                if (!string.IsNullOrEmpty(openUiAfter)) _uiService.OpenUI(openUiAfter);
            },
            onFailed: failure =>
            {
                if (!IsCurrentRequest(version)) return;
                IsLoading = false;
                HasFailed = true;
                progressText.text = "加载失败";
                failureText.text = "场景暂时无法加载，请重试或返回。";
                returnButtonText.text = CanReturnToGame() ? (_returnLabel ?? "返回游戏") : "返回主菜单";
                failurePanel.SetActive(true);
                _cursor.EnterUICursor();
            });
    }

    private bool IsCurrentRequest(int version) => this != null && _isOpen && version == _requestVersion;

    private bool CanReturnToGame() => _sourceScene.IsValid() && _sourceScene.isLoaded &&
        SceneManager.GetActiveScene() == _sourceScene && _playerSession.IsActive &&
        _playerSession.PlayerRig != null && _playerSession.PlayerRig.activeInHierarchy;

    public void Retry()
    {
        if (!_isOpen || !HasFailed || IsLoading || _sceneLoader.IsLoading) return;
        BeginLoad();
    }

    public void ReturnFromFailure()
    {
        if (!_isOpen || !HasFailed || IsLoading || _sceneLoader.IsLoading) return;
        if (CanReturnToGame())
        {
            Action onReturn = _onReturn;
            _uiService.CloseUI(LoadingUiKey);
            _uiService.SetRaycastEnabled("UI/MainUI", true);
            onReturn?.Invoke();
            return;
        }

        _playerSession.EndSession();
        _onReturn = null;
        if (_sourceScene.IsValid() && _sourceScene.isLoaded && _sourceScene.name == "StartScene")
        {
            _uiService.CloseUI(LoadingUiKey);
            _uiService.OpenUI("UI/StartUI");
            return;
        }

        // 原场景已卸载时不能假装恢复游戏，改为加载安全入口；失败仍保留重试出口。
        _nextScene = MenuSceneKey;
        _openUiAfter = string.Empty;
        BeginLoad();
    }

    private void OnBackTriggered()
    {
        if (_isOpen && HasFailed && !IsLoading) ReturnFromFailure();
    }

    public override void OnClose()
    {
        base.OnClose();
        ReleaseInput();
    }

    private void OnDestroy() => ReleaseInput();

    private void ReleaseInput()
    {
        ++_requestVersion;
        _isOpen = false;
        IsLoading = false;
        HasFailed = false;
        _onReturn = null;
        if (_input != null)
        {
            _input.PauseTriggered -= OnBackTriggered;
            _input.SetGameplayInputBlocked(this, false);
        }
        if (_input != null && _input.GameplayInputEnabled) _cursor.EnterGameplayCursor();
        else _cursor?.EnterUICursor();
    }
}
