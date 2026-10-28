using RPGSilent.Domain;

public sealed partial class PlayerProgressService
{
    public const string ArmorUpgradeMaterialId = "forge-shard";
    public int ArmorUpgradeGoldCost(int nextLevel) => nextLevel >= 1 && nextLevel <= EquipmentStats.MaxArmorLevel ? nextLevel * 100 : 0;
    public int ArmorUpgradeMaterialCost(int nextLevel) => nextLevel >= 1 && nextLevel <= EquipmentStats.MaxArmorLevel ? nextLevel * 2 : 0;
    public int ArmorUpgradeMaterials => inventory.Count(ArmorUpgradeMaterialId);

    public bool TryUpgradeArmor(string key, int expectedLevel, out string message)
    {
        message = "请在主城铁匠处强化防具。";
        if (!CanManageInventory || weapons.CombatAllowed) return false;
        var entry = inventory.Find(key);
        var item = FindItem(key);
        if (entry == null || !entry.IsInstance || entry.legacyReserve || item?.Kind != ItemKind.Equipment)
        { message = "请选择背包中的具体防具或饰品。"; return false; }
        if (entry.upgradeLevel != expectedLevel) { message = "强化等级已变化，请重新选择。"; return false; }
        if (entry.upgradeLevel >= EquipmentStats.MaxArmorLevel) { message = "该装备已达到强化上限 +5。"; return false; }
        int nextLevel = entry.upgradeLevel + 1;
        int gold = ArmorUpgradeGoldCost(nextLevel), material = ArmorUpgradeMaterialCost(nextLevel);
        if (stats.Gold < gold) { message = $"金币不足，需要 {gold} 金币。"; return false; }
        var materialDefinition = FindItem(ArmorUpgradeMaterialId);
        if (materialDefinition == null || materialDefinition.Kind != ItemKind.Material)
        { message = "强化材料配置不可用。"; return false; }
        var data = Capture();
        if (!data.inventory.TrySpendMaterial(ArmorUpgradeMaterialId, material))
        { message = $"{materialDefinition.DisplayName}不足，需要 {material} 个（背包持有 {ArmorUpgradeMaterials} 个）。"; return false; }
        data.inventory.Find(key).upgradeLevel = nextLevel;
        // The candidate snapshot contains both the spent material and new level. Nothing
        // authoritative changes until the existing purchase transaction reaches disk.
        return CommitPurchase(data, gold, $"{item.DisplayName} 强化至 +{nextLevel}，消耗 {gold} 金币、{materialDefinition.DisplayName} ×{material}。", out message);
    }
}
