using System.Linq;
using RPGSilent.Domain;

public sealed partial class PlayerProgressService
{
    public const int GemCraftGold = 150, GemCraftCrystals = 3, GemSocketGold = 50;
    public const string GemCraftMaterialId = "crystal-shard";
    public System.Collections.Generic.IEnumerable<ItemDefinition> Gems => Items.Where(x => x.IsGem);
    public int ItemGemBonus(string key, ItemAffixStat stat) => GemBonus(FindInstance(key), stat);
    int GemBonus(ItemStackData entry, ItemAffixStat stat) => entry != null && FindItem(entry.gemId) is { IsGem: true } gem ? EquipmentStats.BaseValue(gem, stat) : 0;
    int SetBonus(InventoryData loadout, ItemAffixStat stat) => loadout.equipped.Select(loadout.Find).Where(x => x != null)
        .Select(x => items[x.itemId]).Where(x => x.Set != null).GroupBy(x => x.Set)
        .Sum(g => g.Key.Bonus(g.Select(x => x.Slot).Distinct().Count(), stat));
    public int SetPieces(EquipmentSetDefinition set) => set == null ? 0 : EquippedItems.Where(x => x.Set == set).Select(x => x.Slot).Distinct().Count();
    public int EquipmentSetBonus(ItemAffixStat stat) => SetBonus(inventory, stat);
    public int ArmorSwapDelta(string key, ItemAffixStat stat)
    {
        var item = FindItem(key);
        if (item == null || item.Kind != ItemKind.Equipment || inventory.Find(key) == null) return 0;
        var next = inventory.Copy(); next.equipped[(int)item.Slot] = key;
        int before = EquippedStat(stat), after = EquippedStat(next, stat);
        if (stat == ItemAffixStat.DefensePercent) { before = System.Math.Min(50, before); after = System.Math.Min(50, after); }
        return after - before;
    }
    public bool TryCraftGem(string gemId, out string message)
    {
        message = "请在主城铁匠处制作宝石。";
        if (!CanManageInventory || weapons.CombatAllowed) return false;
        var gem = FindItem(gemId);
        if (gem == null || !gem.IsGem) { message = "宝石配置不可用。"; return false; }
        if (stats.Gold < GemCraftGold) { message = $"金币不足，需要 {GemCraftGold} 金币。"; return false; }
        var next = Capture();
        if (!next.inventory.TrySpendMaterial(GemCraftMaterialId, GemCraftCrystals)) { message = $"需要背包内晶核碎片 ×{GemCraftCrystals}。"; return false; }
        if (!next.inventory.AddRewards(new[] { new ItemStackData { itemId = gem.Id, count = 1 } })) { message = "宝石存储已满，请先整理。"; return false; }
        return CommitPurchase(next, GemCraftGold, $"已制作 {gem.DisplayName}；背包满时存入待领取。", out message);
    }
    public bool TrySocketGem(string key, string gemId, string expectedGemId, out string message)
    {
        message = "请在主城铁匠处镶嵌或拆卸宝石。";
        if (!CanManageInventory || weapons.CombatAllowed) return false;
        var entry = inventory.Find(key); var item = FindItem(key);
        if (entry == null || !entry.IsInstance || entry.legacyReserve || item?.Kind != ItemKind.Equipment)
        { message = "请选择背包内的具体防具或饰品。"; return false; }
        if (gemId == null || entry.gemId != expectedGemId) { message = "宝石状态已变化，请重新选择。"; return false; }
        if (entry.gemId == gemId) { message = gemId.Length == 0 ? "当前没有可拆卸的宝石。" : "已镶嵌相同宝石。"; return false; }
        var gem = FindItem(gemId);
        if (gemId.Length > 0 && (gem == null || !gem.IsGem)) { message = "请选择可镶嵌的宝石。"; return false; }
        if (stats.Gold < GemSocketGold) { message = $"金币不足，需要 {GemSocketGold} 金币。"; return false; }
        var next = Capture();
        if (gem != null && !next.inventory.TrySpendMaterial(gem.Id, 1)) { message = "背包内没有该宝石，请先制作或领取。"; return false; }
        if (entry.gemId.Length > 0 && !next.inventory.AddRewards(new[] { new ItemStackData { itemId = entry.gemId, count = 1 } }))
        { message = "旧宝石无法存放，请先整理背包及待领取。"; return false; }
        next.inventory.Find(key).gemId = gemId;
        return CommitPurchase(next, GemSocketGold, gem == null ? "宝石已拆下并返还；背包满时存入待领取。" :
            $"已镶嵌 {gem.DisplayName}，旧宝石已返还；背包满时存入待领取。", out message);
    }
}
