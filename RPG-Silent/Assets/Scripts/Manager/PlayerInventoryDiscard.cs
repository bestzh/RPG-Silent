using System;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class PlayerProgressService
{
    public bool CanDiscardItem(string key, out string reason)
    {
        var entry = inventory.Find(key);
        reason = "请先从背包选择物品；待领取物品需先领取。";
        if (entry == null || entry.legacyReserve || FindItem(entry.itemId) == null) return false;
        if (IsLastEquippedItem(key)) { reason = "请先卸下该装备或将其移出两个武器槽。"; return false; }
        if (entry.isLocked) { reason = "装备已锁定，请先解除锁定。"; return false; }
        reason = string.Empty;
        return true;
    }

    // Bind confirmation to the exact item instance/stack, including its current count.
    public string DiscardQuote(string key) => CanDiscardItem(key, out _) ? JsonUtility.ToJson(inventory.Find(key)) : null;

    public bool TryDiscardItem(string key, int count, string expectedQuote, out string message)
    {
        message = "请在角色站稳、动作结束后丢弃物品。";
        if (!CanManageInventory || !CanDiscardItem(key, out message)) return false;
        var entry = inventory.Find(key);
        if (count < 1 || count > entry.count || entry.IsInstance && count != 1)
        { message = "丢弃数量无效，请重新选择。"; return false; }
        if (string.IsNullOrEmpty(expectedQuote) || expectedQuote != DiscardQuote(key))
        { message = "物品或数量已变化，请重新确认丢弃。"; return false; }

        var scene = SceneManager.GetActiveScene();
        var player = weapons.GetComponent<PlayerController>();
        // Use the feet position: it is reachable even beside a wall or narrow ledge.
        var ground = Physics.RaycastAll(player.transform.position + Vector3.up, Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore)
            .Where(h => h.normal.y > .6f && h.collider.GetComponentInParent<PlayerController>() == null && h.collider.GetComponentInParent<EnemyController>() == null)
            .OrderBy(h => h.distance).ToArray();
        var presentation = Resources.Load<LootPresentation>("Loot/Presentation");
        if (!scene.IsValid() || !scene.isLoaded || ground.Length == 0 || presentation == null)
        { message = "当前位置无法放下物品，请移动到平稳地面后重试。"; return false; }

        var next = inventory.Copy();
        bool removed = entry.IsInstance ? next.RemoveOne(key) : next.TrySpendMaterial(key, count);
        if (!removed) { message = "物品已变化，请重新选择。"; return false; }
        var payload = entry.Copy(); payload.count = count;
        var item = FindItem(entry.itemId);
        var dropObject = new GameObject("Discarded item");
        dropObject.SetActive(false);
        SceneManager.MoveGameObjectToScene(dropObject, scene);
        dropObject.transform.position = ground[0].point + Vector3.up * .04f;
        bool committed = false;
        try
        {
            // Prepare the existing loot presentation before committing. A failed save
            // leaves the bag intact and never publishes a collectible duplicate.
            var drop = dropObject.AddComponent<WorldLootPickup>();
            drop.InitializeRolled(item, payload, 0, 0, presentation);
            if (!CommitInventory(next, 0, 0, $"已丢下 {item.DisplayName} ×{count}。关闭背包后靠近按交互键可拾回；离开场景后消失。", out message)) return false;
            committed = true;
            dropObject.SetActive(true);
            return true;
        }
        finally
        {
            if (!committed) UnityEngine.Object.Destroy(dropObject);
        }
    }
}
