using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Shares E with loot: nearby item pickups have priority over scene interactions.</summary>
public sealed class PlayerExplorationInteraction : MonoBehaviour
{
    PlayerController player;
    IPlayerInputActions input;
    PlayerLootInteraction loot;
    readonly RaycastHit[] hits = new RaycastHit[32];
    public ExplorationNode Target { get; private set; }
    float feedbackUntil;
    string feedback;
    public string Prompt => Time.unscaledTime < feedbackUntil ? feedback : Target != null && input != null
        ? "[" + input.GetDisplayString(input.Interact, 0) + "] " + Target.DisplayName : "";
    public void Initialize(PlayerController owner, IPlayerInputActions actions)
    {
        if (input != null) input.InteractTriggered -= Interact;
        player = owner; input = actions; loot = owner.GetComponent<PlayerLootInteraction>();
        input.InteractTriggered += Interact;
    }
    void OnDisable() { if (input != null) input.InteractTriggered -= Interact; Target = null; }
    void OnEnable() { if (input != null) { input.InteractTriggered -= Interact; input.InteractTriggered += Interact; } }
    void Update()
    {
        Target = null;
        if (player == null || !player.CanChangeEquipment || !player.CombatAllowed || loot?.Target != null) return;
        float closest = 2.8f * 2.8f;
        foreach (var node in ExplorationNode.Active)
        {
            if (node == null || node.gameObject.scene != SceneManager.GetActiveScene()) continue;
            node.Discover(transform.position);
            if (node.Used && !node.Reusable) continue;
            float d = (node.transform.position - transform.position).sqrMagnitude;
            if (d < closest && CanReach(node)) { Target = node; closest = d; }
        }
    }
    bool CanReach(ExplorationNode node)
    {
        Vector3 start = transform.position + Vector3.up;
        Vector3 delta = node.transform.position + Vector3.up * .7f - start;
        int count = Physics.RaycastNonAlloc(start, delta.normalized, hits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
            if (!hits[i].transform.IsChildOf(transform) && !hits[i].transform.IsChildOf(node.transform)) return false;
        return true;
    }
    public void Interact()
    {
        if (Target == null || input == null || !input.GameplayInputEnabled || loot?.Target != null || !CanReach(Target)) return;
        var target = Target;
        if (target.Activate(player)) { feedback = target.Status ?? "已切换训练"; feedbackUntil = Time.unscaledTime + 2f; Target = null; }
    }
}
