using RPGSilent.Domain;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

/// <summary>Manual, nearby, visible pickups share the existing rebindable interaction action.</summary>
public sealed class PlayerLootInteraction : MonoBehaviour
{
    static readonly HashSet<PlayerLootInteraction> active = new();
    public static IEnumerable<PlayerLootInteraction> Active => active;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetActive() => active.Clear();
    [Inject] IPlayerInputActions input;
    [Inject] PlayerProgressService progress;
    [Inject] IUIService ui;
    PlayerController player;
    readonly RaycastHit[] hits = new RaycastHit[32];
    WorldLootPickup target;
    string feedback;
    float feedbackUntil;
    public WorldLootPickup Target => target;
    public bool CanReceiveCurrency => isActiveAndEnabled && player != null && !player.IsDead && player.CombatAllowed &&
        ui != null && !ui.IsUIOpen("UI/LoadingUI");
    public Vector3 AttractionPoint => transform.position + Vector3.up * .9f;
    public string Prompt => Time.unscaledTime < feedbackUntil ? feedback : target != null && CanInteract
        ? $"[{input.GetDisplayString(input.Interact, 0)}] 拾取 {target.ColoredName}" : string.Empty;
    bool CanInteract => input != null && input.GameplayInputEnabled && player != null && player.CanChangeEquipment;
    void Awake() => player = GetComponent<PlayerController>();
    void OnEnable() { active.Add(this); if (input != null) input.InteractTriggered += Interact; }
    void OnDisable() { active.Remove(this); if (input != null) input.InteractTriggered -= Interact; target = null; }
    void Update()
    {
        target = null; if (!CanInteract) return;
        float closest = float.PositiveInfinity;
        foreach (var pickup in WorldLootPickup.Active)
        {
            if (pickup.Item == null) continue;
            if (!CanReach(pickup)) continue;
            float distance = (pickup.transform.position - transform.position).sqrMagnitude;
            if (distance < closest) { target = pickup; closest = distance; }
        }
    }
    public bool CanReach(WorldLootPickup pickup)
    {
        if (!CanInteract || pickup == null || pickup.IsCollected || !pickup.isActiveAndEnabled ||
            pickup.gameObject.scene != SceneManager.GetActiveScene() ||
            Vector3.Distance(transform.position, pickup.transform.position) > 2.4f) return false;
        var start = transform.position + Vector3.up * .8f; var delta = pickup.transform.position + Vector3.up * .35f - start;
        int count = Physics.RaycastNonAlloc(start, delta.normalized, hits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
            if (!hits[i].transform.IsChildOf(transform) && !hits[i].transform.IsChildOf(pickup.transform) &&
                hits[i].transform.GetComponentInParent<EnemyController>() == null) return false;
        return true;
    }
    public void Interact()
    {
        if (!CanReach(target)) return;
        var item = target;
        if (item.TryCollect(this, progress, out string message)) { feedback = "已拾取 " + item.ColoredName; target = null; }
        else feedback = message;
        feedbackUntil = Time.unscaledTime + 1.5f;
    }
    public bool ReceiveCurrency(WorldLootPickup pickup, out string message)
    {
        message = "金币和经验正在吸附。";
        if (!CanReceiveCurrency || pickup == null || pickup.Item != null || pickup.IsCollected ||
            pickup.gameObject.scene != SceneManager.GetActiveScene() || Vector3.Distance(AttractionPoint, pickup.transform.position) > .6f) return false;
        return progress.TryCollectWorldLoot(pickup.PickupId, System.Array.Empty<ItemStackData>(), pickup.Gold, pickup.Experience, out message);
    }
}
