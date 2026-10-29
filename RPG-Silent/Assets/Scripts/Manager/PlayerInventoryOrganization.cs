using System;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class PlayerProgressService
{
    public ItemStackData[] QueryInventory(InventoryCategory category, string search, InventorySort sort, bool descending) =>
        InventoryQuery.Apply(inventory, FindItem, category, search, sort, descending);
    public bool TryMarkItemSeen(string key, out string message)
    {
        message = "当前无法更新新物品标记。";
        if (!CanManageInventory || inventory.Find(key) is not { isNew: true }) return false;
        var next = inventory.Copy(); next.Find(key).isNew = false;
        return CommitInventory(next, 0, 0, "已查看物品。", out message);
    }
    public bool CanSellItem(string key, out string reason)
    {
        if (!CanDiscardItem(key, out reason)) return false;
        if (inventory.Find(key).gemId.Length > 0) { reason = "请先在铁匠处拆下宝石，再出售装备。"; return false; }
        return true;
    }
    public bool CanBatchSelect(string key, bool discardItems, out string reason) =>
        discardItems ? CanDiscardItem(key, out reason) : CanSellItem(key, out reason);

    public string InventoryBatchQuote(InventorySelectionData[] selection, bool discardItems)
    {
        if (selection == null || selection.Length == 0 || selection.Length > InventoryData.Capacity ||
            selection.Any(x => x == null || !CanBatchSelect(x.key, discardItems, out _) ||
                x.count < 1 || x.count > inventory.Find(x.key).count || inventory.Find(x.key).IsInstance && x.count != 1) ||
            selection.Select(x => x.key).Distinct().Count() != selection.Length) return null;
        return profileId + ":" + (discardItems ? "discard:" : "sell:" + commerce.revision + ":") + string.Join("|", selection.OrderBy(x => x.key, StringComparer.Ordinal)
            .Select(x => JsonUtility.ToJson(inventory.Find(x.key)) + ":" + x.count + ":" + FindItem(x.key).SellPrice));
    }
    public long InventorySaleTotal(InventorySelectionData[] selection) => InventoryBatchQuote(selection, false) == null ? 0 :
        selection.Sum(x => (long)FindItem(x.key).SellPrice * x.count);
    bool RemoveSelection(InventoryData next, InventorySelectionData[] selection)
    {
        foreach (var x in selection)
            if (!(next.Find(x.key).IsInstance ? next.RemoveOne(x.key) : next.TrySpendMaterial(x.key, x.count))) return false;
        return true;
    }
    public bool TrySellItems(InventorySelectionData[] selection, string expectedQuote, out string message)
    {
        message = "请回主城并在角色站稳后出售物品。";
        if (!CanManageInventory || weapons.CombatAllowed) return false;
        var quote = InventoryBatchQuote(selection, false);
        if (quote == null || quote != expectedQuote) { message = "物品状态或数量已变化，请重新确认；本次未出售任何物品。"; return false; }
        long total = InventorySaleTotal(selection);
        if (total > int.MaxValue - stats.Gold) { message = "出售后金币将超出上限，所选物品均已保留。"; return false; }
        var next = inventory.Copy();
        if (!RemoveSelection(next, selection)) { message = "物品已变化，请重新选择。"; return false; }
        return CommitSale(next, selection, (int)total, out message);
    }
}
