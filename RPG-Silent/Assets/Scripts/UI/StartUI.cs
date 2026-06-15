using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class StartUI : UIBase
{
    [Inject] private IUIService _uiService;

    public Button StartButton;
    public Button SettingsButton;
    public Button QuiteButton;

    private void Awake()
    {
        StartButton?.onClick.AddListener(() =>
        {
            Debug.Log("[StartUI] 点击开始游戏");
            _uiService.OpenUI("UI/LoadingUI", "Scenes/Main");
            _uiService.CloseUI("UI/StartUI");
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

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        Debug.Log("[StartUI] 已打开。");
    }

    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("[StartUI] 已关闭。");
    }
}
