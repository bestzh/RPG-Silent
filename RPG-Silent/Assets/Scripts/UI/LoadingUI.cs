using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class LoadingUI : UIBase
{
    [Inject] private IUIService    _uiService;
    [Inject] private ISceneLoader  _sceneLoader;

    public Slider          progressBar;
    public TextMeshProUGUI progressText;

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        Debug.Log("[LoadingUI] 已打开，开始加载场景。");

        string nextScene = args.Length > 0 ? args[0] as string : "Scenes/Main";

        _sceneLoader.LoadScene(
            nextScene,
            additive: false,
            onProgress: progress =>
            {
                if (progressBar  != null) progressBar.value  = progress;
                if (progressText != null) progressText.text  = $"{(int)(progress * 100)}%";
            },
            onComplete: () =>
            {
                Debug.Log("[LoadingUI] 场景加载完成。");
                _uiService.CloseUI("UI/LoadingUI");
                _uiService.OpenUI("UI/MainUI");
            });
    }

    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("[LoadingUI] 已关闭。");
    }
}
