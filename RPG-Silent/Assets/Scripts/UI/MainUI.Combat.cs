using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public partial class MainUI
{
    [Inject]
    IPlayerInputActions combatInput;
    RectTransform consumableBar, statusBar;
    readonly Button[] consumableButtons = new Button[3];
    readonly Image[] consumableIcons = new Image[3];
    readonly TMP_Text[] consumableLabels = new TMP_Text[3];
    readonly TMP_Text[] effectLabels = new TMP_Text[3];
    readonly Image[] effectIcons = new Image[3];
    TMP_Text consumableMessage;
    PlayerController combatPlayer;
    PlayerConsumableActions consumableActions;
    float nextCombatHud;
    void BuildCombatHud()
    {
        combatPlayer = _session?.PlayerRig?.GetComponentInChildren<PlayerController>(true);
        consumableActions = combatPlayer?.GetComponent<PlayerConsumableActions>();
        if (consumableBar != null)
            return;
        var parent = UIView.Find(transform, "TopLeftPanel");
        consumableBar = UIView.Rect(parent, "Consumables");
        UIView.Text(consumableBar, "Heading", "战斗补给 · 共享冷却 8 秒");
        for (int i = 0; i < 3; i++)
        {
            int slot = i;
            var b = UIView.Button(consumableBar, "UseConsumable:" + i, "", () => consumableActions?.UseSlot(slot));
            consumableButtons[i] = b;
            consumableLabels[i] = b.GetComponentInChildren<TMP_Text>();
            consumableIcons[i] = UIView.Rect(b.transform, "Icon").gameObject.GetComponent<Image>();
            consumableIcons[i].raycastTarget = false;
            consumableIcons[i].preserveAspect = true;
        }

        consumableMessage = UIView.Text(consumableBar, "Message", "");
        statusBar = UIView.Rect(parent, "CombatStatuses");
        for (int i = 0; i < 3; i++)
        {
            effectIcons[i] = UIView.Rect(statusBar, "Icon:" + i).gameObject.GetComponent<Image>();
            effectIcons[i].raycastTarget = false;
            effectLabels[i] = UIView.Text(statusBar, "Status:" + i, "");
        }
    }

    void RefreshCombatHud()
    {
        if (consumableBar == null || _progress == null || Time.unscaledTime < nextCombatHud)
            return;
        nextCombatHud = Time.unscaledTime + .1f;
        for (int i = 0; i < 3; i++)
        {
            var item = _progress.QuickConsumable(i);
            int count = item != null ? _progress.ConsumableCount(item.Id) : 0;
            consumableIcons[i].sprite = item?.Icon;
            consumableIcons[i].enabled = item != null;
            string key = combatInput.GetConsumableSlotDisplay(i);
            double cooldown = _progress.ConsumableCooldown;
            consumableLabels[i].text = $"[{key}] ×{count}" + (cooldown > 0 ? $" · {cooldown:0.0}s" : "\n" + (item?.DisplayName ?? "未配置"));
            consumableButtons[i].interactable = count > 0 && cooldown <= 0 && combatPlayer != null && combatPlayer.CanUseConsumables;
        }

        consumableMessage.text = consumableActions != null && Time.unscaledTime < consumableActions.MessageUntil ? consumableActions.Message : "";
        var entries = combatPlayer?.Statuses?.State.Entries;
        for (int i = 0; i < effectLabels.Length; i++)
        {
            bool present = entries != null && i < entries.Count;
            effectLabels[i].gameObject.SetActive(present);
            effectIcons[i].gameObject.SetActive(present);
            if (!present)
                continue;
            var e = entries[i];
            effectLabels[i].text = $"{e.Definition.DisplayName} · {e.Remaining:0.0}s\n{e.Definition.Description}";
            effectLabels[i].color = e.Definition.Color;
            effectIcons[i].sprite = e.Definition.Icon;
            effectIcons[i].color = e.Definition.Color;
        }
    }
}
