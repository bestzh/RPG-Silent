using TMPro;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI
{
    bool supplyPage;
    Button supplyTab;
    float AddSupplyRows(float y)
    {
        details.text = $"{progress.CurrentClass.DisplayName} · Lv.{progress.Level} · 金币 {progress.Gold}\n药品可堆叠，所有药品共享 8 秒冷却。生命 / 能量已满时不会浪费药品。";
        foreach (var item in progress.ConsumableItems)
        {
            string id = item.Id;
            var icon = W.Rect(rows, "Icon:" + id, 16, y + 8, 56, 56).gameObject.AddComponent<Image>(); icon.sprite = item.Icon; icon.preserveAspect = true;
            W.Text(rows, Font, "Supply:" + id, $"{item.DisplayName} · 持有 {progress.ConsumableCount(id)}\n{item.Description}", 88, y, 576, 112, 20);
            foreach (int quantity in new[] { 1, 5 })
            {
                int count = quantity;
                var button = W.Button(rows, Font, "BuySupply:" + id + ":" + count, $"买 {count} 瓶 · {item.BuyPrice * count} 金币", 710, y + (count == 1 ? 4 : 62), 240, 44, () =>
                { if (Validate(TownNpcRole.Blacksmith)) { progress.TryBuyConsumable(id, count, interaction.Conversation, out string message); status.text = message; Refresh(); } });
                button.interactable = progress.Gold >= item.BuyPrice * count && progress.CanManageInventory;
            }
            y += 136;
        }
        return y;
    }
}
