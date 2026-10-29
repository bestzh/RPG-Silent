using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RPGSilent.Domain
{
    [Serializable]
    public sealed class ItemStackData
    {
        public string itemId;
        public int count;
        public string instanceId = string.Empty;
        public int upgradeLevel;
        public string gemId = string.Empty;
        public bool isLocked;
        // Zero is an unknown acquisition time from an older save; never invent a date.
        public long acquiredUtcTicks;
        public bool isNew;
        public ItemAffixData[] affixes = Array.Empty<ItemAffixData>();
        // Only migrated stacks may remain packed in overflow, until there is bag space.
        public bool legacyReserve;
        public bool IsInstance => !string.IsNullOrEmpty(instanceId);
        public string Key => IsInstance ? instanceId : itemId;
        public int Bonus(ItemAffixStat stat) => affixes?.Where(a => a.stat == stat).Sum(a => a.value) ?? 0;
        public ItemStackData Copy() => new() { itemId = itemId, count = count, instanceId = instanceId,
            upgradeLevel = upgradeLevel, gemId = gemId, isLocked = isLocked, legacyReserve = legacyReserve,
            acquiredUtcTicks = acquiredUtcTicks, isNew = isNew,
            affixes = affixes?.Select(a => new ItemAffixData { stat = a.stat, value = a.value }).ToArray() };
    }

    public enum ItemAffixStat { Health, Energy, AttackPercent, DefensePercent }
    [Serializable] public sealed class ItemAffixData { public ItemAffixStat stat; public int value; }

    [Serializable]
    public sealed class InventoryData
    {
        public const int Capacity = 24;
        public const int SlotCount = 5;
        public const int MaxCount = 9999;
        public const int OverflowCapacity = 256;
        public ItemStackData[] bag = Array.Empty<ItemStackData>();
        public ItemStackData[] overflow = Array.Empty<ItemStackData>();
        public string[] equipped = Enumerable.Repeat(string.Empty, SlotCount).ToArray();
        public string lastRewardId = string.Empty;
        public EquipmentLoadoutData[] loadouts = Array.Empty<EquipmentLoadoutData>();
        public InventoryData Copy() => new()
        {
            bag = bag.Select(x => x.Copy()).ToArray(), overflow = overflow.Select(x => x.Copy()).ToArray(),
            equipped = (string[])equipped.Clone(), lastRewardId = lastRewardId, loadouts = loadouts.Select(x => x.Copy()).ToArray()
        };
        public int Count(string id) => bag.Where(x => x.Key == id || x.itemId == id).Sum(x => x.count);
        public ItemStackData Find(string key) => bag.FirstOrDefault(x => x.Key == key);

        // Rewards overflow into a durable collection; never evict or silently truncate items.
        public bool AddRewards(IEnumerable<ItemStackData> rewards) => AddItems(rewards, true);
        bool AddItems(IEnumerable<ItemStackData> rewards, bool acquired)
        {
            var next = Copy();
            long now = acquired ? DateTime.UtcNow.Ticks : 0;
            foreach (var source in rewards)
            {
                if (!ValidItem(source) || source.legacyReserve) return false;
                var reward = source.Copy();
                if (acquired) { reward.acquiredUtcTicks = now; reward.isNew = true; }
                if (reward.IsInstance && next.bag.Concat(next.overflow).Any(x => x.Key == reward.Key)) return false;
                var list = next.bag.ToList();
                var stack = reward.IsInstance ? null : list.FirstOrDefault(x => !x.IsInstance && x.itemId == reward.itemId);
                int remainder = reward.count;
                if (stack != null)
                {
                    int added = Math.Min(MaxCount - stack.count, remainder); stack.count += added; remainder -= added;
                    if (added > 0) MergeAcquisition(stack, reward);
                }
                else if (list.Count < Capacity) { list.Add(reward.Copy()); remainder = 0; }
                next.bag = list.ToArray();
                if (remainder == 0) continue;
                var pending = next.overflow.ToList();
                var stored = reward.IsInstance ? null : pending.FirstOrDefault(x => !x.IsInstance && x.itemId == reward.itemId);
                if (stored != null) { if (stored.count > MaxCount - remainder) return false; stored.count += remainder; MergeAcquisition(stored, reward); }
                else { if (pending.Count >= OverflowCapacity) return false; var copy = reward.Copy(); copy.count = remainder; pending.Add(copy); }
                next.overflow = pending.ToArray();
            }
            bag = next.bag; overflow = next.overflow; return true;
        }
        static void MergeAcquisition(ItemStackData target, ItemStackData incoming)
        { target.acquiredUtcTicks = Math.Max(target.acquiredUtcTicks, incoming.acquiredUtcTicks); target.isNew |= incoming.isNew; }
        public bool CollectOverflow()
        {
            var next = Copy(); var rewards = next.overflow; next.overflow = Array.Empty<ItemStackData>();
            foreach (var reward in rewards)
            {
                if (!reward.legacyReserve) { if (!next.AddItems(new[] { reward }, false)) return false; continue; }
                while (reward.count > 0 && next.bag.Length < Capacity)
                {
                    var single = reward.Copy(); single.count = 1; single.legacyReserve = false;
                    single.instanceId = EquipmentInstances.StableId(reward.instanceId + ":" + reward.count);
                    next.bag = next.bag.Append(single).ToArray(); reward.count--;
                }
                if (reward.count > 0) next.overflow = next.overflow.Append(reward).ToArray();
            }
            bool changed = next.overflow.Sum(x => x.count) < overflow.Sum(x => x.count);
            if (changed) { bag = next.bag; overflow = next.overflow; }
            return changed;
        }
        public bool RemoveOne(string id)
        {
            var item = Find(id);
            if (item == null || item.isLocked || (item.count == 1 && equipped.Contains(id))) return false;
            if (--item.count == 0) bag = bag.Where(x => x != item).ToArray();
            return true;
        }
        public bool TrySpendMaterial(string id, int count)
        {
            var stack = Find(id);
            if (count <= 0 || stack == null || stack.IsInstance || stack.count < count || equipped.Contains(id)) return false;
            stack.count -= count;
            if (stack.count == 0) bag = bag.Where(x => x != stack).ToArray();
            return true;
        }
        public static InventoryData FromLegacyWeapons(IEnumerable<string> ids) => new()
        {
            bag = ids.Where(id => !string.IsNullOrEmpty(id)).Distinct().Select(id => new ItemStackData { itemId = id, count = 1 }).ToArray()
        };
        public static void Validate(InventoryData data)
        {
            if (data == null || data.bag == null || data.overflow == null || data.bag.Length > Capacity || data.overflow.Length > OverflowCapacity ||
                data.equipped == null || data.equipped.Length != SlotCount || data.lastRewardId == null ||
                (data.lastRewardId.Length > 0 && !Guid.TryParseExact(data.lastRewardId, "N", out _)))
                throw new InvalidDataException("背包记录无效，原存档已保留。");
            var instances = new HashSet<string>();
            foreach (var collection in new[] { data.bag, data.overflow })
            {
                var unique = new HashSet<string>();
                foreach (var item in collection)
                    if (!ValidItem(item) || !unique.Add(item.Key) || (item.IsInstance && !instances.Add(item.instanceId)) ||
                        (item.legacyReserve && ReferenceEquals(collection, data.bag)))
                        throw new InvalidDataException("物品数量或标识无效，原存档已保留。");
            }
            foreach (var id in data.equipped)
                if (id == null || id.Length > 128 || (id.Length > 0 && data.Count(id) == 0))
                    throw new InvalidDataException("穿戴了未持有的装备，原存档已保留。");
            if (data.loadouts == null || data.loadouts.Length > EquipmentLoadoutData.MaxPresets)
                throw new InvalidDataException("配装方案数量无效，原存档已保留。");
            foreach (var loadout in data.loadouts) EquipmentLoadoutData.Validate(loadout);
            if (data.loadouts.Select(x => x.slot).Distinct().Count() != data.loadouts.Length)
                throw new InvalidDataException("配装方案槽重复，原存档已保留。");
        }
        public static bool ValidItem(ItemStackData item)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId) || item.itemId.Length > 128 || item.count < 1 || item.count > MaxCount ||
                item.upgradeLevel < 0 || item.upgradeLevel > 5 || item.affixes == null || item.affixes.Length > 4 || item.gemId == null || item.gemId.Length > 128 ||
                item.acquiredUtcTicks < 0 || item.acquiredUtcTicks > DateTime.MaxValue.Ticks || item.isNew && item.acquiredUtcTicks == 0) return false;
            if (item.IsInstance ? !Guid.TryParseExact(item.instanceId, "N", out _) || (!item.legacyReserve && item.count != 1) :
                item.upgradeLevel != 0 || item.affixes.Length != 0 || item.legacyReserve || item.gemId.Length != 0 || item.isLocked) return false;
            if (item.legacyReserve && (item.gemId.Length != 0 || item.isLocked)) return false;
            var stats = new HashSet<ItemAffixStat>();
            return (!item.legacyReserve || item.affixes.Length == 0) && item.affixes.All(a => a != null &&
                Enum.IsDefined(typeof(ItemAffixStat), a.stat) && stats.Add(a.stat) && a.value > 0 &&
                a.value <= (a.stat == ItemAffixStat.Health ? 120 : a.stat == ItemAffixStat.Energy ? 60 : a.stat == ItemAffixStat.AttackPercent ? 15 : 6));
        }
    }
}
