using System;
using System.IO;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class PlayerProgressService
{
    public EquipmentLoadoutData CurrentLoadout => new()
    {
        armorKeys = (string[])inventory.equipped.Clone(),
        weaponKeys = weapons != null ? Enumerable.Range(0, 2).Select(weapons.GetInstanceId).ToArray() : pending?.weaponInstanceIds?.ToArray() ?? new[] { "", "" },
        activeWeaponSlot = weapons != null ? weapons.ActiveSlot : pending?.activeWeaponSlot ?? 0
    };
    public EquipmentLoadoutData SavedLoadout(int slot) => inventory.loadouts.FirstOrDefault(x => x.slot == slot)?.Copy();
    public bool TrySetItemLocked(string key, bool value, out string message)
    {
        message = "当前无法更改装备锁定。";
        if (!CanManageInventory) return false;
        var entry = inventory.Find(key);
        if (entry == null || !entry.IsInstance || entry.legacyReserve) { message = "只能锁定背包内的具体装备或武器。"; return false; }
        var next = inventory.Copy(); next.Find(key).isLocked = value;
        return CommitInventory(next, 0, 0, value ? "装备已锁定，不能出售或分解。" : "装备已解除锁定。", out message);
    }
    public bool CanDismantle(string key) => inventory.Find(key) is { IsInstance: true, legacyReserve: false, isLocked: false, gemId: "" } &&
        FindItem(key)?.Kind == ItemKind.Equipment && !IsLastEquippedItem(key);
    public string DismantleQuote(string[] keys) => keys == null || keys.Length == 0 || keys.Any(k => !CanDismantle(k)) || keys.Distinct().Count() != keys.Length ? null :
        string.Join("|", keys.OrderBy(k => k).Select(k => JsonUtility.ToJson(inventory.Find(k)) + ":" + (int)FindItem(k).Rarity));
    public int DismantleYield(string[] keys) => DismantleQuote(keys) == null ? 0 : keys.Sum(k => 1 + (int)FindItem(k).Rarity);
    public bool TryDismantleItems(string[] keys, string expectedQuote, out string message)
    {
        message = "请回主城分解装备。";
        if (!CanManageInventory || weapons.CombatAllowed) return false;
        string quote = DismantleQuote(keys);
        if (quote == null || quote != expectedQuote) { message = "选择已变化；已穿戴、锁定或镶嵌宝石的装备不能分解，请重新选择。"; return false; }
        var next = inventory.Copy(); int amount = DismantleYield(keys);
        foreach (string key in keys) if (!next.RemoveOne(key)) { message = "装备已变化，请重新选择。"; return false; }
        if (!next.AddRewards(new[] { new ItemStackData { itemId = "forge-shard", count = amount } })) { message = "材料存储已满，所选装备均已保留。"; return false; }
        return CommitInventory(next, 0, 0, $"已分解 {keys.Length} 件装备，获得锻造碎片 ×{amount}。", out message);
    }
    public bool CheckLoadout(EquipmentLoadoutData outfit, bool requireLevel, out string message)
    {
        message = "配装方案格式无效。";
        try { EquipmentLoadoutData.Validate(outfit); } catch (InvalidDataException) { return false; }
        for (int i = 0; i < outfit.armorKeys.Length + outfit.weaponKeys.Length; i++)
        {
            bool armor = i < outfit.armorKeys.Length; string key = armor ? outfit.armorKeys[i] : outfit.weaponKeys[i - outfit.armorKeys.Length];
            if (key.Length == 0) continue;
            var entry = inventory.Find(key); var item = FindItem(key);
            if (entry == null || !entry.IsInstance || entry.legacyReserve || item == null)
            { message = "配装中的装备已不在背包，请领取缺失装备或重新保存方案。"; return false; }
            if (armor ? item.Kind != ItemKind.Equipment || (int)item.Slot != i : item.Kind != ItemKind.Weapon)
            { message = "配装中的装备槽位不匹配。"; return false; }
            if (requireLevel && stats.Level < item.RequiredLevel) { message = $"{item.DisplayName} 需要等级 {item.RequiredLevel}，整套未更换。"; return false; }
        }
        message = "配装完整。"; return true;
    }
    public int LoadoutStat(EquipmentLoadoutData outfit, ItemAffixStat stat)
    {
        if (!CheckLoadout(outfit, false, out _)) return 0;
        var next = inventory.Copy(); next.equipped = (string[])outfit.armorKeys.Clone();
        int value = ArmorStat(next, stat) + (inventory.Find(outfit.weaponKeys[outfit.activeWeaponSlot])?.Bonus(stat) ?? 0);
        return stat == ItemAffixStat.DefensePercent ? Math.Min(50, value) : value;
    }
    public bool TrySaveLoadout(int slot, out string message)
    {
        message = "当前无法保存配装方案。";
        if (!CanManageInventory || slot < 0 || slot >= EquipmentLoadoutData.MaxPresets) return false;
        var outfit = CurrentLoadout; outfit.slot = slot;
        var next = inventory.Copy(); next.loadouts = next.loadouts.Where(x => x.slot != slot).Append(outfit).OrderBy(x => x.slot).ToArray();
        return CommitInventory(next, 0, 0, $"实际穿戴已保存到方案 {slot + 1}。", out message);
    }
    public bool TryDeleteLoadout(int slot, out string message)
    {
        message = "当前无法清空方案。";
        if (!CanManageInventory || SavedLoadout(slot) == null) return false;
        var next = inventory.Copy(); next.loadouts = next.loadouts.Where(x => x.slot != slot).ToArray();
        return CommitInventory(next, 0, 0, "配装方案已清空，装备仍在背包。", out message);
    }
    public bool TryApplySavedLoadout(int slot, out string message) => TryApplyLoadout(SavedLoadout(slot), out message);
    public bool TryApplyLoadout(EquipmentLoadoutData outfit, out string message)
    {
        message = "请在角色站稳、动作结束后调整装备。";
        if (!CanManageInventory || !CheckLoadout(outfit, true, out message)) return false;
        var current = CurrentLoadout;
        bool weaponChange = !current.weaponKeys.SequenceEqual(outfit.weaponKeys) || current.activeWeaponSlot != outfit.activeWeaponSlot;
        if (weaponChange && weapons.CombatAllowed) { message = "整套武器调整请回主城；副本中可以确认防具试穿。"; return false; }
        var definitions = outfit.weaponKeys.Select(k => FindItem(k)?.Weapon).ToArray();
        if (definitions.Any(w => !weapons.IsAvailable(w))) { message = "方案包含当前无法使用的武器。"; return false; }
        var data = Capture(); data.inventory.equipped = (string[])outfit.armorKeys.Clone(); data.weaponInstanceIds = (string[])outfit.weaponKeys.Clone();
        data.weaponIds = definitions.Select(w => w?.PersistentId ?? "").ToArray(); data.activeWeaponSlot = outfit.activeWeaponSlot;
        data.stance = (int)(definitions[outfit.activeWeaponSlot]?.Stance ?? PlayerStance.Unarmed);
        if (!store.TrySave(data, newProfile, out message)) { StatusMessage = message; HasUnsavedChanges = true; Changed?.Invoke(); return false; }
        applying = true;
        try
        {
            inventory = data.inventory.Copy();
            bool changedModel = appearance != null && appearance.ApplyLoadout(CurrentClass, CurrentGender, EquippedItems);
            if (weaponChange || changedModel) { weapons.RestoreInstanceIds(data.weaponInstanceIds); weapons.RestoreEquipment(definitions, outfit.activeWeaponSlot); }
            ApplyEquipmentBonuses(); newProfile = false; lastSavedPayload = JsonUtility.ToJson(data); HasUnsavedChanges = false;
            StatusMessage = message = "整套装备已应用并保存。";
        }
        finally { applying = false; }
        Changed?.Invoke(); return true;
    }
}
