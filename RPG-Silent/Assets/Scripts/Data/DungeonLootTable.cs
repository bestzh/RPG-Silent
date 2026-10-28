using System;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

[CreateAssetMenu(menuName = "Progression/Dungeon Loot Table")]
public sealed class DungeonLootTable : ScriptableObject
{
    public int DungeonId;
    public ItemDefinition[] Equipment;
    public ItemDefinition[] Weapons;
    public ItemDefinition Material;
    [Min(1)] public int MaterialCount = 3;
    [Range(0, 1)] public float WeaponChance = .22f;
    [Range(0, 1)] public float EquipmentChance = .25f;
    [Range(0, 1)] public float MaterialChance = .6f;
    public ItemDefinition[] Consumables = Array.Empty<ItemDefinition>();
    [Range(0, 1)] public float ConsumableChance = .3f;
    public ItemStackData[] RollEnemy(DungeonDifficulty difficulty, bool boss, System.Random random)
    {
        int tier = Math.Max(0, (int)difficulty - (int)DungeonDifficulty.Normal);
        if (Material == null) throw new InvalidOperationException("副本掉落表缺少材料。");
        var result = new System.Collections.Generic.List<ItemStackData>();
        if (boss || random.NextDouble() < WeaponChance) AddRandom(Weapons, tier, boss, random, result);
        if (boss || random.NextDouble() < EquipmentChance) AddRandom(Equipment, tier, boss, random, result);
        if (boss || random.NextDouble() < MaterialChance)
            result.Add(new ItemStackData { itemId = Material.Id, count = boss ? MaterialCount + tier : 1 });
        if (Consumables.Length > 0 && (boss || random.NextDouble() < ConsumableChance))
        {
            var potion = Consumables[random.Next(Consumables.Length)];
            if (potion == null || potion.Kind != ItemKind.Consumable) throw new InvalidOperationException("药品掉落表无效。");
            result.Add(new ItemStackData { itemId = potion.Id, count = boss ? 2 : 1 });
        }
        return result.ToArray();
    }
    void AddRandom(ItemDefinition[] pool, int tier, bool boss, System.Random random, System.Collections.Generic.List<ItemStackData> result)
    {
        float bonus = tier * 3 + (DungeonId == 1002 ? 4 : 0) + (boss ? 7 : 0);
        double roll = random.NextDouble() * 100;
        float mythic = .1f + tier * .15f + (DungeonId == 1002 ? .2f : 0) + (boss ? .4f : 0);
        float legendary = mythic + .8f + tier * .6f + (DungeonId == 1002 ? .6f : 0) + (boss ? 1.5f : 0);
        var rarity = (ItemRarity)(roll < mythic ? 5 : roll < legendary ? 4 : roll < legendary + 3 + bonus ? 3 : roll < 18 + bonus ? 2 : roll < 50 + bonus ? 1 : 0);
        var options = pool.Where(x => x != null && x.Rarity == rarity).ToArray();
        if (options.Length == 0) throw new InvalidOperationException("掉落表缺少品质 " + rarity);
        result.Add(EquipmentInstances.Create(options[random.Next(options.Length)], random));
    }
}
