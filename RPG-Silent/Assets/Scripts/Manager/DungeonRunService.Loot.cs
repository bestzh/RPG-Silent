using System;
using System.Linq;

public sealed partial class DungeonRunService
{
    string pendingLeaveScene, pendingLeaveLoot, confirmedLeaveLoot;
    public bool LeaveConfirmationPending => !string.IsNullOrEmpty(pendingLeaveScene);
    WorldLootPickup[] HighQualityDrops => WorldLootPickup.Active.Where(x => x != null && !x.IsCollected && x.Item != null &&
        x.Item.Rarity >= ItemRarity.Legendary && x.gameObject.scene == runScene).OrderBy(x => x.PickupId, StringComparer.Ordinal).ToArray();
    public int RemainingHighQualityDrops => HighQualityDrops.Length;
    string HighQualityQuote => string.Join("|", HighQualityDrops.Select(x => x.PickupId + ":" + x.Count));
    public string HighQualityDepartureWarning
    {
        get
        {
            var drops = HighQualityDrops;
            return $"地面仍有 {drops.Length} 份传说／神话物品，离开或重试后将消失：\n" +
                string.Join("\n", drops.Take(5).Select(x => x.Item.RarityLabel + " · " + x.DisplayName)) +
                (drops.Length > 5 ? $"\n另有 {drops.Length - 5} 份" : "") + "\n可取消后继续手动拾取；品质筛选不会影响此提醒。";
        }
    }
    bool AcceptLootDeparture(string sceneKey)
    {
        string quote = HighQualityQuote;
        if (quote.Length == 0 || sceneKey == pendingLeaveScene && quote == confirmedLeaveLoot) return true;
        pendingLeaveScene = sceneKey; pendingLeaveLoot = quote; Changed?.Invoke(); return false;
    }
    public void ConfirmLeave()
    {
        if (!LeaveConfirmationPending) return;
        string current = HighQualityQuote;
        if (current != pendingLeaveLoot)
        { pendingLeaveLoot = current; Changed?.Invoke(); return; }
        confirmedLeaveLoot = current;
        try { Leave(pendingLeaveScene); }
        finally { confirmedLeaveLoot = null; }
    }
    void ClearLeaveConfirmation() { pendingLeaveScene = pendingLeaveLoot = null; }
    public void CancelLeave() { ClearLeaveConfirmation(); Changed?.Invoke(); }
}
