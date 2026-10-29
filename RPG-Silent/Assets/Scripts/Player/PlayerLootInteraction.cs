using RPGSilent.Domain;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

/// <summary>Manual, nearby, visible pickups share the existing rebindable interaction action.</summary>
public sealed partial class PlayerLootInteraction : MonoBehaviour
{
    static readonly HashSet<PlayerLootInteraction> active = new();
    public static IEnumerable<PlayerLootInteraction> Active => active;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetActive() => active.Clear();
    [Inject] IPlayerInputActions input;
    [Inject] PlayerProgressService progress;
    [Inject] IUIService ui;
    [Inject] ICursorService cursor;
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
        ? $"[{input.GetDisplayString(input.Interact, 0)}] 拾取 {target.ColoredName}  ·  [↑/↓] 切换  ·  [{input.GetDisplayString(input.LootList, 0)}] 附近掉落" : string.Empty;
    bool CanInteract => input != null && player != null && !player.IsDead &&
        (input.GameplayInputEnabled && player.CanChangeEquipment || IsListOpen && !input.IsGameplayBlockedExcept(this) && progress.CanManageInventory);
    void Awake() => player = GetComponent<PlayerController>();
    void OnEnable()
    {
        active.Add(this); SceneManager.activeSceneChanged += SceneChanged;
        if (input == null) return;
        input.InteractTriggered += Interact; input.LootListTriggered += ToggleList;
        input.LootCycleTriggered += CycleTarget; input.LootPickupTriggered += PickupPresented;
    }
    void OnDisable()
    {
        CloseList(); active.Remove(this); SceneManager.activeSceneChanged -= SceneChanged;
        if (input != null) { input.InteractTriggered -= Interact; input.LootListTriggered -= ToggleList;
            input.LootCycleTriggered -= CycleTarget; input.LootPickupTriggered -= PickupPresented; }
        target = null;
    }
    void Update()
    {
        if (IsListOpen && (!CanInteract || OtherModal)) CloseList();
        if (Time.unscaledTime < nextNearbyRefresh) return;
        nextNearbyRefresh = Time.unscaledTime + .1f; RefreshNearby();
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
        if (!IsListOpen) TryCollectTarget(target, out _);
    }
    public bool ReceiveCurrency(WorldLootPickup pickup, out string message)
    {
        message = "金币和经验正在吸附。";
        if (!CanReceiveCurrency || pickup == null || pickup.Item != null || pickup.IsCollected ||
            pickup.gameObject.scene != SceneManager.GetActiveScene() || Vector3.Distance(AttractionPoint, pickup.transform.position) > .6f) return false;
        return progress.TryCollectWorldLoot(pickup.PickupId, System.Array.Empty<ItemStackData>(), pickup.Gold, pickup.Experience, out message);
    }
}
