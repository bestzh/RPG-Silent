using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

/// <summary>Owns proximity checks and a single modal input blocker, including async UI loading.</summary>
public sealed class PlayerNpcInteraction : MonoBehaviour
{
    [Inject] IPlayerInputActions input;
    [Inject] IUIService ui;
    [Inject] ICursorService cursor;
    [Inject] PlayerProgressService progress;
    PlayerController player;
    readonly RaycastHit[] hits = new RaycastHit[32];
    TownNpc target, conversation;
    float openedAt;
    bool holdingInput;
    public TownNpc Conversation => conversation;
    public TownNpc Target => target;
    public string Prompt => target != null && !holdingInput ? $"[{input.GetDisplayString(input.Interact, 0)}] {target.DisplayName}" : string.Empty;
    void Awake() => player = GetComponent<PlayerController>();
    void OnEnable() { if (input != null) input.InteractTriggered += Interact; SceneManager.activeSceneChanged += OnSceneChanged; }
    void OnDisable() { if (input != null) input.InteractTriggered -= Interact; SceneManager.activeSceneChanged -= OnSceneChanged; Close(); }
    void OnSceneChanged(Scene before, Scene after) => Close();
    bool OtherModal => ui.IsUIOpen("UI/LoadingUI") || ui.IsUIOpen("UI/PortalUI") || ui.IsUIOpen("UI/PauseUI") || ui.IsUIOpen("UI/SettingsUI") || ui.IsUIOpen(PlayerInventoryInteraction.UiKey) || ui.IsUIOpen(PlayerQuestInteraction.UiKey);
    void Update()
    {
        if (player == null || input == null || ui == null) return;
        if (holdingInput)
        {
            if (!InRange(conversation) || OtherModal || (!ui.IsUIOpen("UI/TownNpcUI") && Time.unscaledTime - openedAt > 8f)) Close();
            return;
        }
        target = null;
        if (!player.CanChangeEquipment || player.CombatAllowed || OtherModal) return;
        float closest = float.PositiveInfinity;
        foreach (var npc in TownNpc.Active)
        {
            if (!InRange(npc)) continue;
            var direction = npc.transform.position - transform.position;
            if (Vector3.Dot(transform.forward, direction.normalized) < 0.1f) continue;
            if (direction.sqrMagnitude < closest) { closest = direction.sqrMagnitude; target = npc; }
        }
    }
    bool InRange(TownNpc npc)
    {
        if (npc == null || !npc.isActiveAndEnabled || player.IsDead || player.CombatAllowed ||
            npc.gameObject.scene != SceneManager.GetActiveScene() || Vector3.Distance(transform.position, npc.transform.position) > npc.InteractionRadius) return false;
        var start = transform.position + Vector3.up;
        var delta = npc.transform.position + Vector3.up - start;
        int count = Physics.RaycastNonAlloc(start, delta.normalized, hits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
            if (!hits[i].transform.IsChildOf(transform) && !hits[i].transform.IsChildOf(npc.transform)) return false;
        return true;
    }
    public void Interact()
    {
        if (holdingInput || !input.GameplayInputEnabled || !player.CanChangeEquipment || OtherModal || !InRange(target)) return;
        conversation = target; holdingInput = true; openedAt = Time.unscaledTime;
        input.SetGameplayInputBlocked(this, true); cursor.EnterUICursor();
        ui.SetRaycastEnabled("UI/MainUI", false); ui.OpenUI("UI/TownNpcUI", this);
    }
    public bool ValidateConversation(TownNpcRole role, out string message)
    {
        message = "请靠近主城 NPC 后重新交谈。";
        if (!holdingInput || !InRange(conversation) || OtherModal || conversation.Role != role) return false;
        if (role == TownNpcRole.Mentor && (conversation.Profession == null || conversation.Profession.Id != progress.CurrentClass?.FamilyId))
        { message = $"请向{progress.CurrentClass?.BaseClass?.DisplayName ?? progress.CurrentClass?.DisplayName}导师学习。"; return false; }
        message = string.Empty; return true;
    }
    public void Close()
    {
        if (!holdingInput) return;
        holdingInput = false; conversation = null; target = null;
        ui.CloseUI("UI/TownNpcUI");
        input.SetGameplayInputBlocked(this, false);
        if (!OtherModal) ui.SetRaycastEnabled("UI/MainUI", true);
        if (input.GameplayInputEnabled) cursor.EnterGameplayCursor(); else cursor.EnterUICursor();
    }
}
