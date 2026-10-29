using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class PlayerLootInteraction
{
    readonly List<WorldLootPickup> nearby = new();
    float nextNearbyRefresh;
    WorldLootPickup presentedTarget;
    public bool IsListOpen { get; private set; }
    public ItemRarity MinimumRarity { get; private set; }
    public IReadOnlyList<WorldLootPickup> Nearby => nearby;
    public int ReachableCount { get; private set; }
    public string Feedback => Time.unscaledTime < feedbackUntil ? feedback : string.Empty;
    public string ListKey => input.GetDisplayString(input.LootList, 0);
    public string PickupKey => input.GetDisplayString(input.Interact, 0);
    bool OtherModal => ui.IsUIOpen("UI/LoadingUI") || ui.IsUIOpen("UI/PauseUI") || ui.IsUIOpen("UI/SettingsUI") ||
        ui.IsUIOpen("UI/TownNpcUI") || ui.IsUIOpen("UI/PortalUI") || ui.IsUIOpen(PlayerInventoryInteraction.UiKey) || ui.IsUIOpen(PlayerQuestInteraction.UiKey);
    void SceneChanged(Scene before, Scene after) { CloseList(); nearby.Clear(); target = presentedTarget = null; }
    public void ToggleList()
    {
        if (IsListOpen) { CloseList(); return; }
        if (!CanInteract || OtherModal) return;
        IsListOpen = true; input.SetGameplayInputBlocked(this, true); cursor.EnterUICursor();
        RefreshNearby(); presentedTarget = null;
    }
    public void CloseList()
    {
        if (!IsListOpen) return;
        IsListOpen = false; presentedTarget = null;
        input.SetGameplayInputBlocked(this, false);
        if (input.GameplayInputEnabled) cursor.EnterGameplayCursor(); else cursor.EnterUICursor();
    }
    public void SetMinimumRarity(ItemRarity rarity)
    {
        if (!Enum.IsDefined(typeof(ItemRarity), rarity)) return;
        MinimumRarity = rarity; RefreshNearby();
    }
    void RefreshNearby()
    {
        nearby.Clear(); ReachableCount = 0;
        if (CanInteract)
            foreach (var drop in WorldLootPickup.Active)
                if (drop != null && drop.Item != null && CanReach(drop))
                { ReachableCount++; if (drop.Item.Rarity >= MinimumRarity) nearby.Add(drop); }
        nearby.Sort((a, b) => {
            int distance = (a.transform.position - transform.position).sqrMagnitude.CompareTo((b.transform.position - transform.position).sqrMagnitude);
            return distance != 0 ? distance : StringComparer.Ordinal.Compare(a.PickupId, b.PickupId);
        });
        if (target == null || !nearby.Contains(target)) target = nearby.FirstOrDefault();
        UpdateWorldLabels();
    }
    void UpdateWorldLabels()
    {
        // One readable target label replaces overlapping names. Beams and items stay intact.
        foreach (var drop in WorldLootPickup.Active)
            if (drop != null && drop.Item != null) drop.SetLabelVisible(drop == target && drop.Item.Rarity >= MinimumRarity && CanInteract);
    }
    public bool SelectTarget(WorldLootPickup pickup)
    {
        if (!CanInteract || pickup == null || pickup.Item == null || pickup.Item.Rarity < MinimumRarity || !CanReach(pickup)) return false;
        target = pickup; UpdateWorldLabels(); return true;
    }
    public void CycleTarget(int direction)
    {
        if (!CanInteract || direction == 0) return;
        RefreshNearby(); if (nearby.Count == 0) return;
        int index = nearby.IndexOf(target);
        SelectTarget(nearby[(Mathf.Max(0, index) + (direction > 0 ? 1 : nearby.Count - 1)) % nearby.Count]);
    }
    public void PresentTarget(WorldLootPickup pickup) => presentedTarget = IsListOpen && pickup == target ? pickup : null;
    void PickupPresented() { if (IsListOpen) TryCollectTarget(presentedTarget, out _); }
    public bool TryCollectTarget(WorldLootPickup expectedTarget, out string message)
    {
        message = "目标已变化，请重新选择附近物品。";
        bool collected = false;
        if (expectedTarget != null && expectedTarget == target && CanReach(expectedTarget))
        {
            collected = expectedTarget.TryCollect(this, progress, out message);
            if (collected) { progress.RecordTutorialAction(RPGSilent.Domain.TutorialStep.ManualPickup); message = "已拾取 " + expectedTarget.ColoredName; target = presentedTarget = null; }
        }
        feedback = message; feedbackUntil = Time.unscaledTime + 3;
        RefreshNearby(); return collected;
    }
}
