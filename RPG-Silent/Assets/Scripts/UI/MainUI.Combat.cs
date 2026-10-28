using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public partial class MainUI
{
    [Inject] IPlayerInputActions combatInput;
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
        if (consumableBar != null) return;
        var parent = transform.Find("TopLeftPanel");
        consumableBar = W.Rect(parent, "Consumables", 28, 700, 370, 152);
        W.Text(consumableBar, classNameText.font, "Heading", "战斗补给 · 共享冷却 8 秒", 0, 0, 370, 30, 19);
        for (int i = 0; i < 3; i++)
        {
            int slot = i; var b = W.Button(consumableBar, classNameText.font, "UseConsumable:" + i, "", i * 120, 36, 112, 78, () => consumableActions?.UseSlot(slot)); consumableButtons[i] = b;
            consumableLabels[i] = b.GetComponentInChildren<TMP_Text>(); consumableLabels[i].fontSize = 17;
            consumableLabels[i].rectTransform.anchoredPosition = new Vector2(8, -40); consumableLabels[i].rectTransform.sizeDelta = new Vector2(96, 36);
            consumableIcons[i] = W.Rect(b.transform, "Icon", 38, 3, 36, 36).gameObject.AddComponent<Image>(); consumableIcons[i].raycastTarget = false; consumableIcons[i].preserveAspect = true;
        }
        consumableMessage = W.Text(consumableBar, classNameText.font, "Message", "", 0, 120, 500, 56, 18);
        statusBar = W.Rect(parent, "CombatStatuses", 28, 420, 412, 168);
        for (int i = 0; i < 3; i++)
        {
            effectIcons[i] = W.Rect(statusBar, "Icon:" + i, 0, i * 52, 40, 40).gameObject.AddComponent<Image>(); effectIcons[i].raycastTarget = false;
            effectLabels[i] = W.Text(statusBar, classNameText.font, "Status:" + i, "", 48, i * 52, 362, 48, 18);
        }
    }
    void RefreshCombatHud()
    {
        if (consumableBar == null || _progress == null || Time.unscaledTime < nextCombatHud) return;
        nextCombatHud = Time.unscaledTime + .1f;
        for (int i = 0; i < 3; i++)
        {
            var item = _progress.QuickConsumable(i); int count = item != null ? _progress.ConsumableCount(item.Id) : 0;
            consumableIcons[i].sprite = item?.Icon; consumableIcons[i].enabled = item != null;
            string key = combatInput.GetConsumableSlotDisplay(i); double cooldown = _progress.ConsumableCooldown;
            consumableLabels[i].text = $"[{key}] ×{count}" + (cooldown > 0 ? $" · {cooldown:0.0}s" : "\n" + (item?.DisplayName ?? "未配置"));
            consumableButtons[i].interactable = count > 0 && cooldown <= 0 && combatPlayer != null && combatPlayer.CanUseConsumables;
        }
        consumableMessage.text = consumableActions != null && Time.unscaledTime < consumableActions.MessageUntil ? consumableActions.Message : "";
        var entries = combatPlayer?.Statuses?.State.Entries;
        for (int i = 0; i < effectLabels.Length; i++)
        {
            bool present = entries != null && i < entries.Count;
            effectLabels[i].gameObject.SetActive(present); effectIcons[i].gameObject.SetActive(present);
            if (!present) continue;
            var e = entries[i]; effectLabels[i].text = $"{e.Definition.DisplayName} · {e.Remaining:0.0}s\n{e.Definition.Description}"; effectLabels[i].color = e.Definition.Color;
            effectIcons[i].sprite = e.Definition.Icon; effectIcons[i].color = e.Definition.Color;
        }
    }
}
