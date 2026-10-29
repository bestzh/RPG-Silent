using System;
using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class PlayerProgressService
{
    CommerceData commerce = new();
    public CommerceData Commerce => commerce.Copy();
    public IEnumerable<ItemDefinition> ShopItems => ConsumableItems.Concat(Items.Where(x => x.Id == "forge-shard" || x.Id == "crystal-shard"));
    public BuybackRecordData[] BuybackItems => commerce.buyback.Where(x => !x.Expired(DateTime.UtcNow.Ticks)).Reverse().Select(x => x.Copy()).ToArray();
    public string CommerceStateKey => profileId + ":" + commerce.revision + ":" + stats.Gold + ":" + ShopDiscountPercent + ":" + JsonUtility.ToJson(inventory);
    public static int ShopDiscountForAffinity(int affinity) => affinity >= 70 ? 15 : affinity >= 40 ? 10 : 0;
    public int ShopDiscountPercent => ShopDiscountForAffinity(NpcAffinity(TownNpcRole.Blacksmith));
    public int ShopUnitPrice(string id)
    {
        int price = id == "forge-shard" ? 25 : id == "crystal-shard" ? 80 : FindItem(id)?.Kind == ItemKind.Consumable ? FindItem(id).BuyPrice : 0;
        return price <= 0 ? 0 : Math.Max(1, (int)(((long)price * (100 - ShopDiscountPercent) + 99) / 100));
    }
    public bool ShopUnlocked(string id) => ShopItems.Any(x => x.Id == id) &&
        (id != "crystal-shard" || NpcAffinity(TownNpcRole.Blacksmith) >= 20 || QuestWasCompleted("side-forge"));
    public string ShopRequirement(string id) => (id == "crystal-shard" ? "完成铁匠的材料委托，或铁匠好感达到 20 后解锁。" : "常备补给") +
        (ShopDiscountPercent > 0 ? $" · 好感优惠 {ShopDiscountPercent}%（单价向上取整）" : " · 铁匠好感 40／70：补给优惠 10%／15%");
    public string ShopCapacityResult(string id, int count)
    {
        if (count < 1 || count > 99 || !ShopItems.Any(x => x.Id == id)) return "数量须为 1–99。";
        var next = inventory.Copy();
        if (!next.AddRewards(new[] { new ItemStackData { itemId = id, count = count } })) return "背包与待领取容量不足。";
        return $"背包 {inventory.bag.Length} → {next.bag.Length}/{InventoryData.Capacity} 格；待领取增加 {next.overflow.Sum(x => x.count) - inventory.overflow.Sum(x => x.count)} 件。";
    }
    public string ShopQuote(string id, int count)
    {
        if (count < 1 || count > 99 || !ShopUnlocked(id) || ShopUnitPrice(id) < 1) return null;
        var next = inventory.Copy(); if (!next.AddRewards(new[] { new ItemStackData { itemId = id, count = count } })) return null;
        return profileId + ":buy:" + commerce.revision + ":" + stats.Gold + ":" + id + ":" + count + ":" + ShopUnitPrice(id) + ":" + JsonUtility.ToJson(inventory);
    }
    public bool TryShopPurchase(string id, int count, string expectedQuote, TownNpc npc, out string message)
    {
        if (!CanUseTownService(npc, TownNpcRole.Blacksmith, out message)) return false;
        if (!ShopItems.Any(x => x.Id == id)) { message = "商品不存在或已下架。"; return false; }
        if (!ShopUnlocked(id)) { message = ShopRequirement(id); return false; }
        string quote = ShopQuote(id, count);
        if (quote == null || quote != expectedQuote) { message = "数量、价格或容量已变化，请重新确认购买。"; return false; }
        long cost = (long)ShopUnitPrice(id) * count;
        if (cost > stats.Gold) { message = $"金币不足，需要 {cost} 金币。"; return false; }
        if (commerce.revision == long.MaxValue) { message = "交易记录已达上限。"; return false; }
        var data = Capture();
        if (!data.inventory.AddRewards(new[] { new ItemStackData { itemId = id, count = count } })) { message = "背包与待领取容量不足，本次未扣款。"; return false; }
        data.commerce.revision++;
        return CommitPurchase(data, (int)cost, $"已购买 {FindItem(id).DisplayName} ×{count}，支付 {cost} 金币。满包部分保存在待领取。", out message);
    }
    public string BuybackQuote(string recordId)
    {
        var record = commerce.buyback.FirstOrDefault(x => x.id == recordId && !x.Expired(DateTime.UtcNow.Ticks));
        return record == null ? null : profileId + ":buyback:" + commerce.revision + ":" + stats.Gold + ":" + JsonUtility.ToJson(record) + ":" + JsonUtility.ToJson(inventory);
    }
    public string BuybackCapacityResult(string recordId)
    {
        var record = commerce.buyback.FirstOrDefault(x => x.id == recordId && !x.Expired(DateTime.UtcNow.Ticks));
        if (record == null) return "记录已到期或已回购。";
        var next = inventory.Copy(); if (!next.AddRewards(new[] { record.item })) return "背包与待领取容量不足。";
        return $"背包 {inventory.bag.Length} → {next.bag.Length}/{InventoryData.Capacity} 格；待领取增加 {next.overflow.Sum(x => x.count) - inventory.overflow.Sum(x => x.count)} 件。";
    }
    public bool BuybackFits(string recordId)
    {
        var record = commerce.buyback.FirstOrDefault(x => x.id == recordId && !x.Expired(DateTime.UtcNow.Ticks));
        return record != null && inventory.Copy().AddRewards(new[] { record.item });
    }
    public bool TryBuyback(string recordId, string expectedQuote, TownNpc npc, out string message)
    {
        if (!CanUseTownService(npc, TownNpcRole.Blacksmith, out message)) return false;
        string quote = BuybackQuote(recordId);
        if (quote == null || quote != expectedQuote) { message = "回购记录、金币或容量已变化，请重新确认；未扣款。"; return false; }
        var record = commerce.buyback.First(x => x.id == recordId);
        if (stats.Gold < record.price) { message = $"金币不足，需要 {record.price} 金币。"; return false; }
        if (commerce.revision == long.MaxValue) { message = "交易记录已达上限。"; return false; }
        var data = Capture();
        if (!data.inventory.AddRewards(new[] { record.item })) { message = "背包与待领取容量不足，回购物品与金币均已保留。"; return false; }
        data.commerce.buyback = data.commerce.buyback.Where(x => x.id != recordId && !x.Expired(DateTime.UtcNow.Ticks)).ToArray();
        data.commerce.revision++;
        return CommitPurchase(data, record.price, $"已原价回购 {FindItem(record.item.itemId).DisplayName} ×{record.item.count}，支付 {record.price} 金币。", out message);
    }
    bool CommitSale(InventoryData next, InventorySelectionData[] selection, int gold, out string message)
    {
        var data = Capture(); data.inventory = next; data.gold = stats.Gold + gold;
        var sold = selection.Select(x => { var item = inventory.Find(x.key).Copy(); item.count = x.count; return item; }).ToArray();
        var prices = selection.Select(x => (int)((long)FindItem(x.key).SellPrice * x.count)).ToArray();
        if (!data.commerce.RecordSale(sold, prices, DateTime.UtcNow.Ticks)) { message = "交易记录不可用，所选物品均已保留。"; return false; }
        if (!store.TrySave(data, newProfile, out message)) { StatusMessage = message; HasUnsavedChanges = true; Changed?.Invoke(); return false; }
        applying = true;
        try
        {
            ApplyRecords(data); stats.AddGold(gold); newProfile = false; lastSavedPayload = JsonUtility.ToJson(data); HasUnsavedChanges = false;
            StatusMessage = message = $"已出售 {selection.Sum(x => x.count)} 件物品，获得 {gold} 金币。48 小时内可向铁匠原价回购，最多保留最近 24 项。";
        }
        finally { applying = false; }
        Changed?.Invoke(); return true;
    }
}
