using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainUI : UIBase
{
    public Image avatarImage;
    public Slider hpBar;
    public Slider mpBar;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI moneyText;
    public RawImage miniMap;

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        Debug.Log("MainUI 打开");

        // 示例数据填充（后续接入角色系统）
        UpdateHP(0.8f);
        UpdateMP(0.6f);
        UpdateGold(999);
        UpdateMoney(999);
    }

    public void UpdateHP(float value)
    {
        hpBar.value = value;
    }

    public void UpdateMP(float value)
    {
        mpBar.value = value;
    }

    public void UpdateGold(int gold)
    {
        goldText.text = $"{gold}";
    }
    public void UpdateMoney(int money)
    {
        moneyText.text = $"{money}";
    }

    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("MainUI 关闭");
    }
}
