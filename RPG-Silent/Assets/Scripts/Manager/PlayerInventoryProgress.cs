using System;
using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class PlayerProgressService
{
    readonly Dictionary<string, ItemDefinition> items = new();
    InventoryData inventory = new();
    public InventoryData Inventory => inventory.Copy();
    public IEnumerable<ItemDefinition> Items => items.Values.OrderBy(x => x.Id);
    public float EquipmentDamageMultiplier => 1f + EquipmentAttackPercent / 100f;
    public int EquipmentAttackPercent => EquippedStat(ItemAffixStat.AttackPercent);
    public int EquipmentDefensePercent => Mathf.Min(50, EquippedStat(ItemAffixStat.DefensePercent));
    public int EquipmentHealth => EquippedStat(ItemAffixStat.Health);
    public int EquipmentEnergy => EquippedStat(ItemAffixStat.Energy);
    IEnumerable<ItemDefinition> EquippedItems => inventory.equipped.Where(id => id.Length > 0).Select(FindItem).Where(x => x != null);
    int EquippedStat(ItemAffixStat stat) => EquippedStat(inventory, stat);
    int ArmorStat(InventoryData loadout, ItemAffixStat stat) => loadout.equipped.Select(loadout.Find).Where(x => x != null).Sum(x => EquipmentStats.Total(items[x.itemId], x, stat) + GemBonus(x, stat)) + SetBonus(loadout, stat);
    int EquippedStat(InventoryData loadout, ItemAffixStat stat) => ArmorStat(loadout, stat) +
        (inventory.Find(weapons != null ? weapons.GetInstanceId(weapons.ActiveSlot) : null)?.Bonus(stat) ?? 0);
    public ItemDefinition FindItem(string id)
    {
        if (id == null) return null;
        if (items.TryGetValue(id, out var definition)) return definition;
        var instance = inventory.bag.Concat(inventory.overflow).FirstOrDefault(x => x.Key == id);
        return instance != null && items.TryGetValue(instance.itemId, out definition) ? definition : null;
    }
    public ItemStackData FindInstance(string key) => inventory.bag.Concat(inventory.overflow).FirstOrDefault(x => x.Key == key)?.Copy();
    public int ItemStat(string key, ItemAffixStat stat) => EquipmentStats.Total(FindItem(key), FindInstance(key), stat) + ItemGemBonus(key, stat);
    public int ItemUpgradeBonus(string key, ItemAffixStat stat) => EquipmentStats.UpgradeBonus(FindItem(key), FindInstance(key)?.upgradeLevel ?? 0, stat);
    public string ResolveItemKey(string key)
    {
        if (inventory.Find(key) != null) return key;
        var matches = inventory.bag.Where(x => x.itemId == key).ToArray();
        return matches.Length == 1 ? matches[0].Key : null;
    }
    public string ResolveWeaponKey(WeaponDefinition weapon)
    {
        if (weapon == null) return string.Empty;
        if (FindItem(weapon.PersistentId)?.Weapon != weapon) return null;
        if (weapons != null)
        {
            if (weapons.ActiveWeapon == weapon && inventory.Find(weapons.GetInstanceId(weapons.ActiveSlot)) != null) return weapons.GetInstanceId(weapons.ActiveSlot);
            for (int i = 0; i < PlayerWeaponEquipment.SlotCount; i++)
                if (weapons.GetWeapon(i) == weapon && inventory.Find(weapons.GetInstanceId(i)) != null) return weapons.GetInstanceId(i);
        }
        return inventory.bag.FirstOrDefault(x => x.itemId == weapon.PersistentId)?.Key;
    }
    public int GetInstanceLevel(string key) => inventory.Find(key)?.upgradeLevel ?? 0;
    public string EquippedWeaponKey(int slot) => weapons?.GetInstanceId(slot) ?? string.Empty;
    public bool CanEquipWeaponInstance(string key) => string.IsNullOrEmpty(key) ||
        (inventory.Find(key) is { IsInstance: true, legacyReserve: false } && FindItem(key)?.Kind == ItemKind.Weapon && stats.Level >= FindItem(key).RequiredLevel);
    // Called at the animation's handoff, before the runtime weapon identity changes.
    public bool CommitWeaponSelection(int slot, string key, int activeSlot)
    {
        if (!attached || applying) return true;
        if (slot < 0 || slot >= PlayerWeaponEquipment.SlotCount || activeSlot < 0 || activeSlot >= PlayerWeaponEquipment.SlotCount || !CanEquipWeaponInstance(key)) return false;
        var data = Capture(); data.weaponInstanceIds[slot] = key ?? "";
        data.weaponIds[slot] = FindItem(key)?.Id ?? ""; data.activeWeaponSlot = activeSlot;
        var active = FindItem(data.weaponInstanceIds[activeSlot]); data.stance = (int)(active?.Weapon != null ? active.Weapon.Stance : PlayerStance.Unarmed);
        if (!store.TrySave(data, newProfile, out string message))
        { StatusMessage = message; HasUnsavedChanges = true; Changed?.Invoke(); return false; }
        newProfile = false; lastSavedPayload = JsonUtility.ToJson(data); HasUnsavedChanges = false; StatusMessage = "武器已装备并保存"; return true;
    }
    public bool OwnsWeapon(WeaponDefinition weapon) => weapon == null || inventory.Count(weapon.PersistentId) > 0;
    public bool CanEquipWeapon(WeaponDefinition weapon) => weapon == null || (OwnsWeapon(weapon) &&
        FindItem(weapon.PersistentId) != null && stats.Level >= FindItem(weapon.PersistentId).RequiredLevel);
    public ItemDefinition EquippedItem(EquipmentSlot slot) => FindItem(inventory.equipped[(int)slot]);
    public bool CanManageInventory => attached && !stats.IsDead && weapons != null && !weapons.IsBusy &&
        (weapons.GetComponent<PlayerController>().StateMachine.CurrentState is IdleState ||
         weapons.GetComponent<PlayerController>().StateMachine.CurrentState is MoveState);

    void LoadItemCatalog()
    {
        foreach (var item in Resources.LoadAll<ItemDefinition>("Items"))
        {
            if (string.IsNullOrEmpty(item.Id) || !items.TryAdd(item.Id, item) || item.RequiredLevel < 1 || !Enum.IsDefined(typeof(ItemKind), item.Kind) ||
                item.Health < 0 || item.Energy < 0 || item.AttackPercent < 0 || item.DefensePercent < 0 || item.SellPrice < 0 ||
                (item.IsGem && item.Kind != ItemKind.Material) || (item.Set != null && (item.Kind != ItemKind.Equipment || string.IsNullOrEmpty(item.Set.Id))) ||
                (item.Kind == ItemKind.Consumable && (!Enum.IsDefined(typeof(ConsumableEffect), item.UseEffect) || item.UseEffect == ConsumableEffect.None || item.UseAmount < 0 || item.UseAmount > 10000 ||
                    item.BuyPrice < 1 || item.BuyPrice > 10000 || item.SellPrice > item.BuyPrice ||
                    ((item.UseEffect == ConsumableEffect.Heal || item.UseEffect == ConsumableEffect.Energy) && item.UseAmount == 0) ||
                    (item.UseEffect == ConsumableEffect.Guard && (item.UseStatus == null || item.UseStatus.Kind != CombatStatusKind.Guard)))) ||
                (item.Kind == ItemKind.Weapon && (item.Weapon == null || item.Weapon.PersistentId != item.Id)))
                throw new InvalidOperationException("物品配置无效或重复：" + item.name);
        }
    }
    bool ValidInventoryContent(InventoryData data)
    {
        if (data.bag.Concat(data.overflow).Any(x => !items.TryGetValue(x.itemId, out var item) ||
            (item.IsStackable ? x.IsInstance : !x.IsInstance) ||
            (item.IsStackable && x.upgradeLevel != 0) || x.affixes.Length > EquipmentInstances.AffixCount(item.Rarity) ||
            (x.gemId.Length > 0 && (item.Kind != ItemKind.Equipment || !items.TryGetValue(x.gemId, out var gem) || !gem.IsGem)))) return false;
        for (int i = 0; i < data.equipped.Length; i++)
        {
            var id = data.equipped[i]; if (id.Length == 0) continue;
            var entry = data.Find(id);
            if (entry == null || !items.TryGetValue(entry.itemId, out var item) || item.Kind != ItemKind.Equipment || (int)item.Slot != i) return false;
        }
        return true;
    }
    void ApplyEquipmentBonuses() => stats.SetEquipmentBonuses(EquipmentHealth, EquipmentEnergy, EquipmentDefensePercent);

    public bool TryWearItem(string id, out string message)
    {
        id = ResolveItemKey(id);
        message = "请在角色站稳、动作结束后调整装备。";
        if (!CanManageInventory) return false;
        var item = FindItem(id);
        if (item == null || item.Kind != ItemKind.Equipment || inventory.Count(id) == 0) { message = "未持有该装备。"; return false; }
        if (stats.Level < item.RequiredLevel) { message = $"需要角色等级 {item.RequiredLevel}。"; return false; }
        var next = inventory.Copy(); next.equipped[(int)item.Slot] = id;
        return CommitInventory(next, 0, 0, $"已穿戴 {item.DisplayName}。", out message);
    }
    public bool TryRemoveEquipment(EquipmentSlot slot, out string message)
    {
        message = "当前无法调整装备。";
        if (!CanManageInventory || !Enum.IsDefined(typeof(EquipmentSlot), slot)) return false;
        var next = inventory.Copy(); next.equipped[(int)slot] = string.Empty;
        return CommitInventory(next, 0, 0, "装备已卸下，物品仍在背包。", out message);
    }
    public bool TrySellItem(string id, out string message)
    {
        id = ResolveItemKey(id);
        message = "请回主城出售物品。";
        if (!CanManageInventory || weapons.CombatAllowed) return false;
        var item = FindItem(id);
        if (item == null || inventory.Count(id) == 0) { message = "物品已不在背包中。"; return false; }
        if (IsLastEquippedItem(id)) { message = "请先卸下该装备或武器。"; return false; }
        if (inventory.Find(id).isLocked) { message = "装备已锁定，请先解除锁定。"; return false; }
        if (inventory.Find(id).gemId.Length > 0) { message = "请先在铁匠处拆下宝石，再出售装备。"; return false; }
        if (stats.Gold > int.MaxValue - item.SellPrice) { message = "金币已达上限。"; return false; }
        var next = inventory.Copy(); if (!next.RemoveOne(id)) return false;
        return CommitInventory(next, item.SellPrice, 0, $"已出售 {item.DisplayName}，获得 {item.SellPrice} 金币。", out message);
    }
    public bool TryDismantleItem(string id, out string message)
    {
        id = ResolveItemKey(id);
        var keys = new[] { id };
        return TryDismantleItems(keys, DismantleQuote(keys), out message);
    }
    public bool IsLastEquippedItem(string id)
    {
        id = ResolveItemKey(id);
        return id != null && (inventory.equipped.Contains(id) || Enumerable.Range(0, PlayerWeaponEquipment.SlotCount).Any(i => weapons?.GetInstanceId(i) == id));
    }
    public bool TryCollectOverflow(out string message)
    {
        message = "背包没有足够空间，请先出售或分解物品。";
        if (!CanManageInventory) return false;
        var next = inventory.Copy(); if (!next.CollectOverflow()) return false;
        return CommitInventory(next, 0, 0, "待领取物品已移入背包。", out message);
    }
    public bool TryCollectWorldLoot(string rewardId, ItemStackData[] loot, int gold, int exp, out string message)
    {
        message = "奖励配置不可用，请重试领取。";
        if (!attached || stats.IsDead || !Guid.TryParseExact(rewardId, "N", out _) || loot == null || gold < 0 || exp < 0) return false;
        if (inventory.lastRewardId == rewardId) { message = "本次奖励已领取。"; return true; }
        if (loot.Any(x => x == null || !items.ContainsKey(x.itemId))) return false;
        // Compatibility for explicit grants: create stable base-stat copies keyed by the reward.
        // Dungeon rolls already carry their identities and must never be rolled again here.
        var granted = new List<ItemStackData>();
        for (int i = 0; i < loot.Length; i++)
        {
            var entry = loot[i]; var item = items[entry.itemId];
            if (!InventoryData.ValidItem(entry) || entry.legacyReserve) return false;
            if (!item.IsStackable && !entry.IsInstance)
            {
                if (entry.count > InventoryData.Capacity) { message = "背包空间不足。"; return false; }
                for (int copy = 0; copy < entry.count; copy++)
                {
                    var single = EquipmentInstances.Create(item, null); single.instanceId = EquipmentInstances.StableId(rewardId + ":" + i + ":" + copy); granted.Add(single);
                }
            }
            else granted.Add(entry.Copy());
        }
        var next = inventory.Copy();
        if (!next.AddRewards(granted) || !ValidInventoryContent(next) || next.overflow.Sum(x => x.count) != inventory.overflow.Sum(x => x.count))
        { message = "背包已满，物品仍在地上。请整理背包后再拾取。"; return false; }
        next.lastRewardId = rewardId;
        return CommitInventory(next, gold, exp, "拾取成功，已保存。", out message);
    }
    bool CommitInventory(InventoryData next, int gold, int exp, string success, out string message)
    {
        var data = Capture(); data.inventory = next;
        CollectInventoryDiscoveries(data);
        data.gold = (int)Math.Min(int.MaxValue, (long)stats.Gold + gold);
        data.totalExp = (int)Math.Min(int.MaxValue, (long)stats.Exp + exp);
        if (!store.TrySave(data, newProfile, out message)) { StatusMessage = message; HasUnsavedChanges = true; Changed?.Invoke(); return false; }
        applying = true;
        try
        {
            inventory = next.Copy(); world = data.world.Copy(); ApplyEquipmentBonuses(); stats.AddReward(gold, exp);
            if (appearance != null && appearance.ApplyLoadout(CurrentClass, CurrentGender, EquippedItems))
                weapons.RestoreEquipment(Enumerable.Range(0, PlayerWeaponEquipment.SlotCount).Select(weapons.GetWeapon).ToArray(), weapons.ActiveSlot);
            newProfile = false; lastSavedPayload = JsonUtility.ToJson(data); HasUnsavedChanges = false; StatusMessage = message = success;
        }
        finally { applying = false; }
        Changed?.Invoke(); return true;
    }
}
