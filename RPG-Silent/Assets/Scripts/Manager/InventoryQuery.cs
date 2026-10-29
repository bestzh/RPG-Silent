using System;
using System.Linq;
using RPGSilent.Domain;

public enum InventorySort { Quality, Level, Type, Acquired }
public enum InventoryCategory { All, Equipment, Weapon, Material, Overflow, Consumable, Gem }

/// <summary>A view of exact keys. Sorting never rearranges authoritative inventory data.</summary>
public static class InventoryQuery
{
    public static ItemStackData[] Apply(InventoryData data, Func<string, ItemDefinition> lookup,
        InventoryCategory category, string search, InventorySort sort, bool descending)
    {
        string term = (search ?? string.Empty).Trim();
        var source = category == InventoryCategory.Overflow ? data.overflow : data.bag;
        var matches = source.Where(x => lookup(x.itemId) is { } item && Matches(item, category) &&
            (term.Length == 0 || (item.DisplayName ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
        Func<ItemStackData, long> order = sort switch
        {
            InventorySort.Level => x => lookup(x.itemId).RequiredLevel,
            InventorySort.Type => x => (int)lookup(x.itemId).Kind * 10 + (lookup(x.itemId).IsGem ? 6 : (int)lookup(x.itemId).Slot),
            InventorySort.Acquired => x => x.acquiredUtcTicks,
            _ => x => (int)lookup(x.itemId).Rarity
        };
        // Unknown legacy dates always follow known dates, in both directions.
        var result = matches.OrderBy(x => sort == InventorySort.Acquired && x.acquiredUtcTicks == 0 ? 1 : 0);
        result = descending ? result.ThenByDescending(order) : result.ThenBy(order);
        return result.ThenByDescending(x => lookup(x.itemId).Rarity).ThenBy(x => x.itemId, StringComparer.Ordinal)
            .ThenBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Copy()).ToArray();
    }
    static bool Matches(ItemDefinition item, InventoryCategory category) => category switch
    {
        InventoryCategory.Equipment => item.Kind == ItemKind.Equipment,
        InventoryCategory.Weapon => item.Kind == ItemKind.Weapon,
        InventoryCategory.Material => item.Kind == ItemKind.Material && !item.IsGem,
        InventoryCategory.Consumable => item.Kind == ItemKind.Consumable,
        InventoryCategory.Gem => item.IsGem,
        _ => true
    };
}
