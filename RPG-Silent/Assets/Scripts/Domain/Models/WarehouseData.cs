using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RPGSilent.Domain
{
    [Serializable]
    public sealed class WarehouseData
    {
        public const int Capacity = 96;
        public ItemStackData[] items = Array.Empty<ItemStackData>();
        public long revision;
        public WarehouseData Copy() => new() { items = items.Select(x => x.Copy()).ToArray(), revision = revision };
        public static void Validate(WarehouseData data)
        {
            if (data == null || data.items == null || data.items.Length > Capacity || data.revision < 0 ||
                data.items.Any(x => !InventoryData.ValidItem(x) || x.legacyReserve) || data.items.Select(x => x.Key).Distinct().Count() != data.items.Length)
                throw new InvalidDataException("仓库记录无效，原存档已保留。");
        }
        // Works on candidate snapshots. Neither side changes until every item fits.
        public static bool Transfer(InventoryData bag, WarehouseData warehouse, InventorySelectionData[] selection, bool deposit)
        {
            if (bag == null || warehouse == null || bag.bag == null || warehouse.items == null || selection == null || selection.Length == 0 || selection.Length > (deposit ? InventoryData.Capacity : Capacity) ||
                selection.Any(x => x == null) || selection.Select(x => x.key).Distinct().Count() != selection.Length || warehouse.revision == long.MaxValue) return false;
            var source = (deposit ? bag.bag : warehouse.items).Select(x => x.Copy()).ToList();
            var destination = (deposit ? warehouse.items : bag.bag).Select(x => x.Copy()).ToList();
            int capacity = deposit ? Capacity : InventoryData.Capacity;
            foreach (var choice in selection)
            {
                var entry = source.FirstOrDefault(x => x.Key == choice.key);
                if (entry == null || entry.legacyReserve || choice.count < 1 || choice.count > entry.count || entry.IsInstance && choice.count != 1) return false;
                if (deposit && (bag.equipped.Contains(choice.key) || bag.loadouts.Any(x => x.armorKeys.Contains(choice.key) || x.weaponKeys.Contains(choice.key)))) return false;
                var moved = entry.Copy(); moved.count = choice.count;
                var existing = destination.FirstOrDefault(x => x.Key == moved.Key);
                if (moved.IsInstance && existing != null) return false;
                if (existing != null)
                {
                    if (existing.count > InventoryData.MaxCount - moved.count) return false;
                    existing.count += moved.count; existing.acquiredUtcTicks = Math.Max(existing.acquiredUtcTicks, moved.acquiredUtcTicks); existing.isNew |= moved.isNew;
                }
                else { if (destination.Count >= capacity) return false; destination.Add(moved); }
                entry.count -= moved.count; if (entry.count == 0) source.Remove(entry);
            }
            bag.bag = (deposit ? source : destination).ToArray(); warehouse.items = (deposit ? destination : source).ToArray();
            warehouse.revision++; return true;
        }
    }
}
