using System;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class PlayerProgressService
{
    WarehouseData warehouse = new();
    public WarehouseData Warehouse => warehouse.Copy();
    public ItemStackData[] QueryWarehouse(InventoryCategory category, string search, InventorySort sort, bool descending) =>
        InventoryQuery.Apply(new InventoryData { bag = warehouse.items }, FindItem, category == InventoryCategory.Overflow ? InventoryCategory.All : category, search, sort, descending);
    public bool CanUseTownService(TownNpc npc, TownNpcRole role, out string message)
    {
        message = "请靠近主城服务 NPC 并重新交谈。";
        var player = weapons != null ? weapons.GetComponent<PlayerController>() : null;
        if (!CanManageInventory || player == null || player.CombatAllowed || npc == null || !npc.isActiveAndEnabled || npc.Role != role ||
            npc.gameObject.scene != SceneManager.GetActiveScene() || Vector3.Distance(player.transform.position, npc.transform.position) > npc.InteractionRadius) return false;
        var interaction = player.GetComponent<PlayerNpcInteraction>();
        return interaction != null && interaction.Conversation == npc && interaction.ValidateConversation(role, out message);
    }
    public bool CanWarehouseSelect(string key, bool deposit, out string message)
    {
        message = "请选择当前存储位置中的具体物品。";
        var entry = deposit ? inventory.Find(key) : warehouse.items.FirstOrDefault(x => x.Key == key);
        if (entry == null || entry.legacyReserve || !ValidInventoryEntry(entry)) return false;
        if (deposit && IsLastEquippedItem(key)) { message = "请先卸下装备或将其移出两个武器槽。"; return false; }
        if (deposit && inventory.loadouts.Any(x => x.armorKeys.Contains(key) || x.weaponKeys.Contains(key)))
        { message = "该装备被配装方案引用，请先覆盖或清空相关方案。"; return false; }
        message = string.Empty; return true;
    }
    public string WarehouseQuote(InventorySelectionData[] selection, bool deposit)
    {
        if (selection == null || selection.Length == 0 || selection.Length > (deposit ? InventoryData.Capacity : WarehouseData.Capacity) ||
            selection.Any(x => x == null || !CanWarehouseSelect(x.key, deposit, out _) || x.count < 1 ||
                x.count > (deposit ? inventory.Find(x.key) : warehouse.items.First(i => i.Key == x.key)).count) ||
            selection.Select(x => x.key).Distinct().Count() != selection.Length) return null;
        return profileId + ":" + warehouse.revision + ":" + deposit + ":" + string.Join("|", selection.OrderBy(x => x.key, StringComparer.Ordinal)
            .Select(x => JsonUtility.ToJson(deposit ? inventory.Find(x.key) : warehouse.items.First(i => i.Key == x.key)) + ":" + x.count));
    }
    public bool TryWarehouseTransfer(InventorySelectionData[] selection, bool deposit, string expectedQuote, TownNpc npc, out string message)
    {
        if (!CanUseTownService(npc, TownNpcRole.Steward, out message)) return false;
        string quote = WarehouseQuote(selection, deposit);
        if (quote == null || quote != expectedQuote) { message = "物品或仓库状态已变化，请重新核对；本次未存取任何物品。"; return false; }
        var data = Capture();
        if (!WarehouseData.Transfer(data.inventory, data.warehouse, selection, deposit))
        { message = deposit ? "仓库容量或堆叠上限不足，全部物品已保留在背包。" : "背包容量或堆叠上限不足，全部物品已保留在仓库。"; return false; }
        if (deposit) data.tutorial.Record(TutorialStep.WarehouseDeposit);
        if (!store.TrySave(data, newProfile, out message)) { StatusMessage = message; HasUnsavedChanges = true; Changed?.Invoke(); return false; }
        inventory = data.inventory.Copy(); warehouse = data.warehouse.Copy(); tutorial = data.tutorial.Copy();
        newProfile = false; lastSavedPayload = JsonUtility.ToJson(data); HasUnsavedChanges = false;
        StatusMessage = message = $"已{(deposit ? "存入仓库" : "取回背包")} {selection.Sum(x => x.count)} 件物品，已保存。";
        Changed?.Invoke(); return true;
    }
}
