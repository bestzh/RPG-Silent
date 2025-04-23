using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : UIBase
{
    public Slider progressBar;
    public TextMeshProUGUI  progressText;

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        Debug.Log("LoadingUI 打开");

        string nextScene = args.Length > 0 ? args[0] as string : "Scenes/Main";
        SceneLoaderManager.Instance.LoadScene(nextScene, false,
            progress =>
            {
                progressBar.value = progress;
                progressText.text = $"{(int)(progress * 100)}%";
            },
            () =>
            {
                Debug.Log("场景加载完成！");
                UIManager.Instance.CloseUI("UI/LoadingUI");
                UIManager.Instance.OpenUI("UI/MainUI");
            });
    }

    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("LoadingUI 关闭");
    }
}
