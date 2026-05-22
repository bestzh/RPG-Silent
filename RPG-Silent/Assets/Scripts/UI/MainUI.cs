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

    private PlayerController player;

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        Debug.Log("MainUI opened.");
        BindPlayer();
    }

    public void UpdateHP(float value)
    {
        if (hpBar != null)
        {
            hpBar.value = value;
        }
    }

    public void UpdateMP(float value)
    {
        if (mpBar != null)
        {
            mpBar.value = value;
        }
    }

    public void UpdateGold(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"{gold}";
        }
    }

    public void UpdateMoney(int money)
    {
        if (moneyText != null)
        {
            moneyText.text = $"{money}";
        }
    }

    public override void OnClose()
    {
        base.OnClose();
        UnbindPlayer();
        Debug.Log("MainUI closed.");
    }

    private void BindPlayer()
    {
        UnbindPlayer();

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogWarning("MainUI did not find a Player object.");
            return;
        }

        player = playerObject.GetComponent<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("MainUI found Player object, but it has no PlayerController.");
            return;
        }

        player.HealthChanged += OnHealthChanged;
        player.GoldChanged += UpdateGold;
        player.ExpChanged += UpdateMoney;

        player.NotifyStatsChanged();
        UpdateMP(1f);
    }

    private void UnbindPlayer()
    {
        if (player == null)
        {
            return;
        }

        player.HealthChanged -= OnHealthChanged;
        player.GoldChanged -= UpdateGold;
        player.ExpChanged -= UpdateMoney;
        player = null;
    }

    private void OnHealthChanged(int current, int max)
    {
        float value = max > 0 ? (float)current / max : 0f;
        UpdateHP(value);
    }
}
