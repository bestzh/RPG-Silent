using TMPro;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI
{
    bool supplyPage;
    Button supplyTab;
    float AddSupplyRows(float y)
    {
        details.text = $"{progress.CurrentClass.DisplayName} · Lv.{progress.Level} · 金币 {progress.Gold}\n药品与材料支持数量购买；满包部分进入待领取。药品共享 8 秒冷却。";
        if (shopBuybackPage)
            return AddBuybackRows(y);
        foreach (var item in progress.ShopItems)
        {
            string id = item.Id;
            var row = AddRow(item.DisplayName, $"持有 {progress.Inventory.Count(id)} · 单价 {progress.ShopUnitPrice(id)} 金币\n{item.Description}\n{progress.ShopRequirement(id)}", item.Icon);
            row.Action(0, progress.ShopUnlocked(id) ? "选择数量 / 核对总价" : "商品尚未解锁", () =>
            {
                if (Validate(TownNpcRole.Blacksmith))
                    OpenPurchase(id, 1);
            }, progress.ShopUnlocked(id) && progress.CanManageInventory);
        }

        return 0;
    }
}
