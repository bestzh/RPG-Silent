using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RPGSilent.Domain
{
    /// <summary>Creates immutable drop rolls and migrates old bundles without losing copies.</summary>
    public static class EquipmentInstances
    {
        public static int AffixCount(ItemRarity rarity) => new[] { 0, 1, 2, 3, 4, 4 }[(int)rarity];
        public static ItemStackData Create(ItemDefinition item, Random random)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var result = new ItemStackData { itemId = item.Id, count = 1 };
            if (item.IsStackable) return result;
            result.instanceId = Guid.NewGuid().ToString("N");
            if (random == null) return result; // Starter and migrated gear retain their base stats.
            var pool = new List<ItemAffixStat>((ItemAffixStat[])Enum.GetValues(typeof(ItemAffixStat)));
            var rolls = new List<ItemAffixData>(); int tier = (int)item.Rarity;
            for (int i = 0; i < AffixCount(item.Rarity); i++)
            {
                int index = random.Next(pool.Count); var stat = pool[index]; pool.RemoveAt(index);
                int unit = stat == ItemAffixStat.Health ? 8 : stat == ItemAffixStat.Energy ? 4 : 1;
                int max = stat == ItemAffixStat.DefensePercent ? tier + 1 : tier * 3;
                rolls.Add(new ItemAffixData { stat = stat, value = random.Next(Math.Max(1, tier), max + 1) * unit });
            }
            result.affixes = rolls.ToArray(); return result;
        }
        public static string StableId(string source)
        {
            using var sha = SHA256.Create();
            return new Guid(sha.ComputeHash(Encoding.UTF8.GetBytes(source)).Take(16).ToArray()).ToString("N");
        }
        public static void Migrate(PlayerProgressData data, Func<string, ItemDefinition> lookup)
        {
            if (data.equipmentVersion == 1) return;
            if (data.equipmentVersion != 0) throw new InvalidDataException("装备存档版本不兼容。");
            InventoryData.Validate(data.inventory);
            var old = data.inventory;
            if (old.bag.Concat(old.overflow).Any(x => x.IsInstance)) throw new InvalidDataException("装备格式标记与实例记录不一致。");
            if (old.bag.Concat(old.overflow).Any(x => lookup(x.itemId) == null)) throw new InvalidDataException("旧存档物品资源缺失。");
            var next = new InventoryData { lastRewardId = old.lastRewardId };
            var upgrades = data.weaponUpgrades.ToDictionary(x => x.weaponId, x => x.level);
            // Equipped gear must survive expansion into the fixed-size bag.
            var preferred = new HashSet<string>(old.equipped.Concat(data.weaponIds));
            var bag = new List<ItemStackData>(); var overflow = new List<ItemStackData>();
            int serial = 0;
            foreach (var source in old.bag.OrderByDescending(x => preferred.Contains(x.itemId)).Concat(old.overflow))
            {
                bool fromBag = old.bag.Contains(source);
                if (lookup(source.itemId).IsStackable)
                {
                    (fromBag && bag.Count < InventoryData.Capacity ? bag : overflow).Add(source.Copy()); continue;
                }
                var reserve = new ItemStackData { itemId = source.itemId, count = source.count, legacyReserve = true,
                    instanceId = StableId(data.profileId + ":legacy:" + serial++),
                    upgradeLevel = upgrades.TryGetValue(source.itemId, out int level) ? level : 0 };
                // First allocate one per old bag entry, then expand remaining copies below.
                if (fromBag && bag.Count < InventoryData.Capacity)
                {
                    var single = reserve.Copy(); single.count = 1; single.legacyReserve = false;
                    single.instanceId = StableId(reserve.instanceId + ":" + reserve.count); reserve.count--; bag.Add(single);
                }
                if (reserve.count > 0) overflow.Add(reserve);
            }
            next.bag = bag.ToArray(); next.overflow = overflow.ToArray();
            next.CollectOverflow();
            for (int i = 0; i < old.equipped.Length; i++) next.equipped[i] = next.bag.FirstOrDefault(x => x.itemId == old.equipped[i])?.Key ?? "";
            data.weaponInstanceIds = data.weaponIds.Select(id => next.bag.FirstOrDefault(x => x.itemId == id)?.Key ?? "").ToArray();
            data.inventory = next; data.weaponUpgrades = Array.Empty<WeaponUpgradeData>(); data.equipmentVersion = 1;
        }
    }
}
