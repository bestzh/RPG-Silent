using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StartUI : UIBase
{
     public Button StartButton;
     public Button QuiteButton;

    private void Awake()
    {
        StartButton.onClick.AddListener(() => {
            Debug.Log("点击开始游戏");
            UIManager.Instance.OpenUI("UI/LoadingUI", "Scenes/Main");
            UIManager.Instance.CloseUI("UI/StartUI");
        });
        QuiteButton.onClick.AddListener(() => {
            Debug.Log("点击关闭游戏");
        });
    }
    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        Debug.Log("Start UI 打开");
    }

    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("Start UI 关闭");
    }
}
