using RPGSilent.Domain;
using TMPro;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public sealed partial class InventoryUI
{
    [Inject]
    IPlayerInputActions consumableInput;
    Button[] quickButtons;
    float nextConsumableDetail;
    void RefreshConsumableDetails(ItemDefinition item, bool owned)
    {
        if (quickButtons == null)
        {
            quickButtons = new Button[ConsumableLoadoutData.SlotCount];
            for (int i = 0; i < quickButtons.Length; i++)
            {
                int slot = i;
                quickButtons[i] = UIView.Button(panel, "QuickConsumable:" + i, "", () =>
                {
                    progress.TryAssignConsumable(slot, selected, out string message);
                    status.text = message;
                });
            }
        }

        bool potion = item?.Kind == ItemKind.Consumable;
        for (int i = 0; i < quickButtons.Length; i++)
        {
            quickButtons[i].gameObject.SetActive(potion);
            quickButtons[i].interactable = owned && progress.CanManageInventory;
            quickButtons[i].GetComponentInChildren<TMP_Text>().text = "设为快捷 " + consumableInput.GetConsumableSlotDisplay(i);
        }

        if (!potion)
            return;
        RefreshConsumableCooldown(true);
        setText.text = "药品从副本掉落，需手动拾取。\n主城铁匠的“战斗补给”页也可购买。\n重新打开界面、换药或读档不会重置冷却。";
    }

    void RefreshConsumableCooldown(bool force = false)
    {
        if (progress == null || comparison == null || (!force && UnityEngine.Time.unscaledTime < nextConsumableDetail))
            return;
        nextConsumableDetail = UnityEngine.Time.unscaledTime + .1f;
        var item = progress.FindItem(selected);
        if (item?.Kind != ItemKind.Consumable)
            return;
        comparison.text = $"持有 {progress.ConsumableCount(item.Id)} 瓶 · 全药品共享冷却 {ConsumableLoadoutData.SharedCooldownSeconds} 秒\n关闭背包后使用快捷键，或点击 HUD 药品槽。\n当前冷却 {progress.ConsumableCooldown:0.0} 秒";
    }
}
