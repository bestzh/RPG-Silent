using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class MainUI : UIBase
{
    // 通过 VContainer 注入，不再 FindWithTag
    [Inject] private IPlayerStatsReader _stats;

    public Image          avatarImage;
    public Slider         hpBar;
    public Slider         mpBar;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI moneyText;
    public RawImage        miniMap;

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);

        if (_stats == null)
        {
            Debug.LogWarning("[MainUI] IPlayerStatsReader 未注入，无法绑定数据。");
            return;
        }

        _stats.OnHealthChanged += OnHealthChanged;
        _stats.OnGoldChanged   += OnGoldChanged;
        _stats.OnExpChanged    += OnExpChanged;

        _stats.Refresh();
        UpdateMP(1f);

        Debug.Log("[MainUI] 已打开并绑定玩家数据。");
    }

    public override void OnClose()
    {
        base.OnClose();

        if (_stats != null)
        {
            _stats.OnHealthChanged -= OnHealthChanged;
            _stats.OnGoldChanged   -= OnGoldChanged;
            _stats.OnExpChanged    -= OnExpChanged;
        }

        Debug.Log("[MainUI] 已关闭。");
    }

    private void OnHealthChanged(int current, int max)
    {
        if (hpBar != null)
            hpBar.value = max > 0 ? (float)current / max : 0f;
    }

    private void OnGoldChanged(int gold)
    {
        if (goldText != null) goldText.text = $"{gold}";
    }

    private void OnExpChanged(int exp)
    {
        if (moneyText != null) moneyText.text = $"{exp}";
    }

    private void UpdateMP(float value)
    {
        if (mpBar != null) mpBar.value = value;
    }
}
