using System;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class PlayerProgressService
{
    ConsumableLoadoutData consumables = new();
    public ConsumableLoadoutData ConsumableLoadout => consumables.Copy();
    public double ConsumableCooldown => ConsumableLoadoutData.Remaining(consumables.readyUtcTicks, DateTime.UtcNow.Ticks);
    public System.Collections.Generic.IEnumerable<ItemDefinition> ConsumableItems => Items.Where(x => x.Kind == ItemKind.Consumable);
    public ItemDefinition QuickConsumable(int slot) => slot >= 0 && slot < ConsumableLoadoutData.SlotCount ? FindItem(consumables.itemIds[slot]) : null;
    public int ConsumableCount(string id) => inventory.Count(id);
    bool ValidConsumableContent(ConsumableLoadoutData data) => data.itemIds.All(id => id.Length == 0 || FindItem(id)?.Kind == ItemKind.Consumable);
    public bool TryAssignConsumable(int slot, string id, out string message)
    {
        message = "请在角色动作结束后选择背包中持有的药品。";
        if (!CanManageInventory || slot < 0 || slot >= ConsumableLoadoutData.SlotCount || id == null || (id.Length > 0 && (FindItem(id)?.Kind != ItemKind.Consumable || inventory.Count(id) == 0))) return false;
        var data = Capture();
        if (id.Length > 0) for (int i = 0; i < data.consumables.itemIds.Length; i++) if (data.consumables.itemIds[i] == id) data.consumables.itemIds[i] = "";
        data.consumables.itemIds[slot] = id;
        return CommitConsumables(data, "药品快捷栏已保存，当前冷却保持不变。", out message);
    }
    public bool TryBuyConsumable(string id, int count, TownNpc npc, out string message)
    {
        message = "请靠近主城铁匠，在战斗补给页购买。";
        var item = FindItem(id); var player = weapons != null ? weapons.GetComponent<PlayerController>() : null;
        if (!CanManageInventory || player == null || player.CombatAllowed || npc == null || !npc.isActiveAndEnabled || npc.Role != TownNpcRole.Blacksmith ||
            npc.gameObject.scene != SceneManager.GetActiveScene() || Vector3.Distance(player.transform.position, npc.transform.position) > npc.InteractionRadius ||
            item?.Kind != ItemKind.Consumable || count < 1 || count > 99) return false;
        return TryShopPurchase(id, count, ShopQuote(id, count), npc, out message);
    }
    public bool TryUseConsumableSlot(int slot, out string message)
    {
        message = "当前无法使用药品。";
        var item = QuickConsumable(slot); var player = weapons != null ? weapons.GetComponent<PlayerController>() : null;
        if (!attached || item == null || player == null || !player.CanUseConsumables) return false;
        if (ConsumableCooldown > 0) { message = $"药品共享冷却还剩 {ConsumableCooldown:0.0} 秒。"; return false; }
        if (inventory.Count(item.Id) <= 0) { message = "背包中没有该药品，可从副本拾取或向主城铁匠购买。"; return false; }
        var state = player.Statuses?.State;
        if (item.UseEffect == ConsumableEffect.Heal && stats.CurrentHealth >= stats.MaxHealth) { message = "生命已满，无需使用。"; return false; }
        if (item.UseEffect == ConsumableEffect.Energy && stats.CurrentEnergy >= stats.MaxEnergy) { message = "能量已满，无需使用。"; return false; }
        if (item.UseEffect == ConsumableEffect.Cleanse && (state == null || !state.HasDebuffs)) { message = "当前没有可净化的异常状态。"; return false; }
        if (item.UseEffect == ConsumableEffect.Guard && (!player.CanReceiveCombatEffects || item.UseStatus == null || state == null)) { message = "护体药剂仅能在副本战斗区域使用。"; return false; }
        var data = Capture();
        if (!data.inventory.RemoveOne(item.Id)) return false;
        data.consumables.readyUtcTicks = DateTime.UtcNow.AddSeconds(ConsumableLoadoutData.SharedCooldownSeconds).Ticks;
        if (!CommitConsumables(data, "已使用「" + item.DisplayName + "」。", out message)) return false;
        switch (item.UseEffect)
        {
            case ConsumableEffect.Heal: stats.Heal(item.UseAmount); break;
            case ConsumableEffect.Energy: stats.RecoverEnergy(item.UseAmount); break;
            case ConsumableEffect.Guard: player.Statuses.Apply(item.UseStatus); break;
            case ConsumableEffect.Cleanse: state.Cleanse(); break;
        }
        if (item.UseVfx != null) { var vfx = UnityEngine.Object.Instantiate(item.UseVfx, player.transform.position, Quaternion.identity, player.transform); UnityEngine.Object.Destroy(vfx, 2f); }
        return true;
    }
    bool CommitConsumables(PlayerProgressData data, string success, out string message)
    {
        if (!store.TrySave(data, newProfile, out message)) { StatusMessage = message; HasUnsavedChanges = true; Changed?.Invoke(); return false; }
        inventory = data.inventory.Copy(); consumables = data.consumables.Copy();
        newProfile = false; lastSavedPayload = JsonUtility.ToJson(data); HasUnsavedChanges = false; StatusMessage = message = success;
        Changed?.Invoke(); return true;
    }
}
