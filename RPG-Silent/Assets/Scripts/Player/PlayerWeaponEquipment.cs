using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponTransitionPhase { None, Sheathing, Drawing }

/// <summary>Owns durable equipment separately from the weapon currently being animated.</summary>
[DefaultExecutionOrder(10)]
[RequireComponent(typeof(Animator), typeof(PlayerStanceController))]
public sealed class PlayerWeaponEquipment : MonoBehaviour
{
    public const int SlotCount = 2;
    public const string DrawState = "WeaponDraw";
    public const string SheathState = "WeaponSheath";
    public const string DrawPlaceholder = "Relax-Unsheath-R-Back";
    public const string SheathPlaceholder = "Armed-Sheath-R-Back-Relax";

    [SerializeField] private WeaponDefinition[] availableWeapons = Array.Empty<WeaponDefinition>();
    [SerializeField] private WeaponDefinition[] equippedWeapons = new WeaponDefinition[SlotCount];
    [SerializeField, Range(0, SlotCount - 1)] private int activeSlot;
    [SerializeField, Range(0.05f, 0.3f)] private float transitionBlend = 0.12f;
    [SerializeField, Range(0.02f, 0.2f)] private float attachmentBlend = 0.09f;

    private PlayerController player;
    [VContainer.Inject] private PlayerProgressService progress;
    private PlayerStanceController stance;
    private Animator animator;
    private AttackExecutor attacks;
    private PlayerClassAppearance appearance;
    private GameObject model;
    private WeaponDefinition modelDefinition;
    private bool combatAllowed = true;
    private bool modelDrawn;
    private bool started;
    private bool pendingPresentation;
    private bool pendingSelection;
    private int requestedSlot;
    private WeaponDefinition requestedWeapon;
    private string requestedInstanceId;
    private readonly string[] equippedInstanceIds = { "", "" };
    private AnimatorOverrideController transitionController;
    private RuntimeAnimatorController settledController;
    private int settledFrame;
    private Vector3 attachmentStartPosition;
    private Quaternion attachmentStartRotation;
    private float attachmentElapsed;
    private bool blendingAttachment;
    private AnimationClip phaseClip;
    private int phaseHash;
    private float attachTime;
    private float phaseElapsed;
    private bool attachmentApplied;
    private float previousAnimatorSpeed;
    private bool pausedByInput;

    public int ActiveSlot => activeSlot;
    public WeaponDefinition ActiveWeapon => GetWeapon(activeSlot);
    public bool CombatAllowed => combatAllowed;
    public bool IsSheathed => !modelDrawn;
    public bool IsTransitioning => Phase != WeaponTransitionPhase.None;
    public bool IsBusy => IsTransitioning || pendingPresentation;
    public WeaponTransitionPhase Phase { get; private set; }
    public GameObject WeaponModel => model;
    public int AvailableCount => availableWeapons.Length;
    public bool CanChangeEquipment => player != null && player.CanChangeEquipment;
    public event Action EquipmentChanged;
    public event Action PresentationChanged;

    public void PrepareAppearanceChange()
    {
        CancelTransition();
        if (model != null) { model.SetActive(false); Destroy(model); }
        model = null;
        modelDefinition = null;
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        stance = GetComponent<PlayerStanceController>();
        animator = GetComponent<Animator>();
        attacks = GetComponent<AttackExecutor>();
        appearance = GetComponent<PlayerClassAppearance>();
        ApplyStablePresentation();
    }

    private void Start() => started = true;

    private void Update()
    {
        // Keep the outgoing weapon clip through the final blend, then release the
        // temporary controller after the destination pose has taken over.
        if (settledController != null && Time.frameCount > settledFrame && !animator.IsInTransition(0)
            && !animator.GetCurrentAnimatorStateInfo(0).IsTag("WeaponTransition"))
            ReleaseTransitionController();
        // Loading/respawn establishes the rule immediately. Play the visible action only
        // after the loading screen releases input and the player has landed.
        if (pendingPresentation && !IsTransitioning && player.CanStartWeaponTransition)
            BeginSequence();
    }

    private void OnDisable()
    {
        CancelTransition();
        ReleaseTransitionController();
    }

    private void LateUpdate()
    {
        if (!blendingAttachment || model == null || !player.GameplayInputEnabled) return;
        attachmentElapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(attachmentElapsed / attachmentBlend));
        var targetPosition = MountPosition(modelDefinition, modelDrawn);
        var targetRotation = MountRotation(modelDefinition, modelDrawn);
        model.transform.localPosition = Vector3.Lerp(attachmentStartPosition, targetPosition, t);
        model.transform.localRotation = Quaternion.Slerp(attachmentStartRotation, targetRotation, t);
        if (t >= 1f) blendingAttachment = false;
    }

    public WeaponDefinition GetWeapon(int slot) => slot >= 0 && slot < SlotCount ? equippedWeapons[slot] : null;
    public string GetInstanceId(int slot) => slot >= 0 && slot < SlotCount ? equippedInstanceIds[slot] : string.Empty;
    public void RestoreInstanceIds(string[] ids)
    {
        if (ids == null || ids.Length != SlotCount) throw new ArgumentException("武器实例记录无效。");
        Array.Copy(ids, equippedInstanceIds, SlotCount);
    }
    public WeaponDefinition GetAvailableWeapon(int index) => index >= 0 && index < availableWeapons.Length ? availableWeapons[index] : null;
    public bool IsAvailable(WeaponDefinition weapon) => weapon == null ||
        (Array.IndexOf(availableWeapons, weapon) >= 0 && (progress == null || progress.CanEquipWeapon(weapon)));

    public bool TryEquip(int slot, WeaponDefinition weapon)
    {
        if (slot < 0 || slot >= SlotCount || !IsAvailable(weapon) || !CanChangeEquipment) return false;
        return TryEquipSelection(slot, weapon, progress?.ResolveWeaponKey(weapon) ?? string.Empty);
    }
    public bool TryEquipInstance(int slot, string key)
    {
        if (progress == null || slot < 0 || slot >= SlotCount || !CanChangeEquipment || !progress.CanEquipWeaponInstance(key)) return false;
        var definition = progress.FindItem(key)?.Weapon;
        if (!IsAvailable(definition)) return false;
        return TryEquipSelection(slot, definition, key ?? string.Empty);
    }
    bool TryEquipSelection(int slot, WeaponDefinition weapon, string key)
    {
        if (equippedWeapons[slot] == weapon && equippedInstanceIds[slot] == key) return true;
        if (slot != activeSlot)
        {
            if (progress != null && !progress.CommitWeaponSelection(slot, key, activeSlot)) return false;
            equippedWeapons[slot] = weapon;
            equippedInstanceIds[slot] = key;
            EquipmentChanged?.Invoke();
            return true;
        }
        return RequestSelection(slot, weapon, key);
    }

    public bool TrySelectSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount || !CanChangeEquipment) return false;
        if (activeSlot == slot) return true;
        return RequestSelection(slot, equippedWeapons[slot], equippedInstanceIds[slot]);
    }

    public bool TrySwitchNext() => TrySelectSlot((activeSlot + 1) % SlotCount);

    private bool RequestSelection(int slot, WeaponDefinition weapon, string key)
    {
        requestedSlot = slot;
        requestedWeapon = weapon;
        requestedInstanceId = key;
        pendingSelection = true;
        if (!combatAllowed || weapon == ActiveWeapon)
        {
            bool committed = CommitSelection();
            ApplyStablePresentation();
            PresentationChanged?.Invoke();
            return committed;
        }
        BeginSequence();
        return true;
    }

    // Validate the entire restored selection before touching live equipment.
    public void RestoreEquipment(WeaponDefinition[] weapons, int selectedSlot)
    {
        if (weapons == null || weapons.Length != SlotCount || selectedSlot < 0 || selectedSlot >= SlotCount)
            throw new ArgumentException("武器装备数据无效。");
        foreach (var weapon in weapons)
            if (!IsAvailable(weapon)) throw new ArgumentException("武器不在当前可装备列表中。");
        CancelTransition();
        ReleaseTransitionController();
        Array.Copy(weapons, equippedWeapons, SlotCount);
        if (progress != null)
            for (int i = 0; i < SlotCount; i++)
                if (progress.FindItem(equippedInstanceIds[i])?.Weapon != weapons[i] || (weapons[i] == null && equippedInstanceIds[i].Length > 0))
                    equippedInstanceIds[i] = progress.ResolveWeaponKey(weapons[i]) ?? string.Empty;
        activeSlot = selectedSlot;
        attacks?.ResetCombat();
        ApplyStablePresentation();
        EquipmentChanged?.Invoke();
    }

    public void SetCombatAllowed(bool allowed)
    {
        if (allowed == combatAllowed) return;
        bool interrupted = IsTransitioning;
        CancelTransition();
        if (interrupted && player.StateMachine.CurrentState is WeaponTransitionState)
            player.ReturnToLocomotion();
        ReleaseTransitionController();
        combatAllowed = allowed;
        // Gameplay policy changes now; the old pose remains available for the blend.
        stance.SetCombatPolicy(allowed, applyAnimator: false);
        if (!started || ActiveWeapon == null)
            ApplyStablePresentation();
        else
            pendingPresentation = true;
        PresentationChanged?.Invoke();
    }

    private void BeginSequence()
    {
        pendingPresentation = false;
        attacks?.ResetCombat();
        player.AnimationController?.CancelAnimation();
        previousAnimatorSpeed = animator.speed;
        CreateTransitionController();
        if (modelDrawn && modelDefinition != null)
            StartPhase(WeaponTransitionPhase.Sheathing, modelDefinition.GetSheathClip(!combatAllowed));
        else
            BeginDraw();
        if (IsTransitioning)
            player.StateMachine.ChangeState(new WeaponTransitionState(player));
    }

    private void CreateTransitionController()
    {
        ReleaseTransitionController();
        var current = animator.runtimeAnimatorController;
        var source = current as AnimatorOverrideController;
        transitionController = new AnimatorOverrideController(source != null ? source.runtimeAnimatorController : current)
        { name = "Runtime Weapon Transition" };
        if (source != null)
        {
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            source.GetOverrides(overrides);
            transitionController.ApplyOverrides(overrides);
        }
        animator.runtimeAnimatorController = transitionController;
    }

    private void StartPhase(WeaponTransitionPhase phase, AnimationClip clip)
    {
        Phase = phase;
        phaseClip = clip;
        phaseHash = Animator.StringToHash(phase == WeaponTransitionPhase.Drawing ? DrawState : SheathState);
        attachmentApplied = false;
        phaseElapsed = 0f;
        attachTime = 0.5f;
        if (clip == null || !animator.HasState(0, phaseHash))
        {
            // Incomplete optional animation content must never lock gameplay.
            AttachForPhase();
            CompletePhase();
            return;
        }
        foreach (var evt in clip.events)
            if (evt.functionName == "WeaponSwitch") { attachTime = Mathf.Clamp01(evt.time / clip.length); break; }
        transitionController[phase == WeaponTransitionPhase.Drawing ? DrawPlaceholder : SheathPlaceholder] = clip;
        animator.CrossFadeInFixedTime(phaseHash, transitionBlend, 0, 0f);
        PresentationChanged?.Invoke();
    }

    private void BeginDraw()
    {
        bool fromRelax = !pendingSelection; // region entry starts from Relax; equipment changes bridge through Unarmed.
        CommitSelection();
        UpdateModel(false);
        if (combatAllowed && ActiveWeapon != null)
            StartPhase(WeaponTransitionPhase.Drawing, ActiveWeapon.GetDrawClip(fromRelax));
        else
            FinishSequence();
    }

    private bool CommitSelection()
    {
        if (!pendingSelection) return true;
        if (progress != null && !progress.CommitWeaponSelection(requestedSlot, requestedInstanceId, requestedSlot))
        { pendingSelection = false; requestedWeapon = null; requestedInstanceId = null; return false; }
        equippedWeapons[requestedSlot] = requestedWeapon;
        equippedInstanceIds[requestedSlot] = requestedInstanceId;
        activeSlot = requestedSlot;
        pendingSelection = false;
        requestedWeapon = null;
        requestedInstanceId = null;
        // The atomic handoff commits progress; presentation callbacks cannot change inventory.
        stance.SetStancePresentation(ActiveWeapon != null ? ActiveWeapon.Stance : PlayerStance.Unarmed, true, applyAnimator: false);
        EquipmentChanged?.Invoke();
        return true;
    }

    public void TickTransition()
    {
        if (!IsTransitioning) return;
        if (!player.GameplayInputEnabled)
        {
            if (!pausedByInput) { animator.speed = 0f; pausedByInput = true; }
            return;
        }
        if (pausedByInput) { animator.speed = previousAnimatorSpeed; pausedByInput = false; }
        if (!animator.isActiveAndEnabled) { FinishSequence(); return; }
        phaseElapsed += Time.deltaTime;
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if (next.shortNameHash == phaseHash) state = next;
        }
        if (state.shortNameHash == phaseHash)
        {
            // Normalized-time fallback also handles disabled/omitted presentation events.
            if (state.normalizedTime >= attachTime) AttachForPhase();
            if (state.normalizedTime >= 0.98f && !animator.IsInTransition(0)) CompletePhase();
        }
        if (IsTransitioning && phaseElapsed > (phaseClip != null ? phaseClip.length * 3f : 1f) + transitionBlend)
            FinishSequence();
    }

    public void NotifyWeaponSwitch(AnimationEvent evt)
    {
        if (!IsTransitioning || evt == null || evt.animatorClipInfo.clip != phaseClip) return;
        // Ignore events from outgoing blends or a cancelled action.
        var state = evt.animatorStateInfo;
        if (state.shortNameHash == phaseHash && state.normalizedTime >= attachTime - 0.02f)
            AttachForPhase();
    }

    private void AttachForPhase()
    {
        if (attachmentApplied) return;
        attachmentApplied = true;
        UpdateModel(Phase == WeaponTransitionPhase.Drawing, blendAttachment: true);
        PresentationChanged?.Invoke();
    }

    private void CompletePhase()
    {
        AttachForPhase();
        if (Phase == WeaponTransitionPhase.Sheathing) BeginDraw();
        else FinishSequence();
    }

    private void FinishSequence()
    {
        // A watchdog/interruption before the handoff retains the old selection.
        CancelTransition();
        if (player.StateMachine.CurrentState is WeaponTransitionState)
        {
            player.ReturnToLocomotion();
            animator.CrossFadeInFixedTime("MoveTree", transitionBlend);
        }
    }

    public void CancelTransition()
    {
        if (!IsBusy) return;
        bool hadTransition = IsTransitioning;
        Phase = WeaponTransitionPhase.None;
        phaseClip = null;
        pendingPresentation = pendingSelection = false;
        requestedWeapon = null;
        if (pausedByInput) { animator.speed = previousAnimatorSpeed; pausedByInput = false; }
        if (stance == null) return;
        if (!hadTransition) ReleaseTransitionController();
        ApplyStablePresentation();
        if (hadTransition && animator.isActiveAndEnabled && transitionController != null)
        {
            settledController = animator.runtimeAnimatorController;
            settledFrame = Time.frameCount;
            var destination = settledController as AnimatorOverrideController;
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            if (destination != null) destination.GetOverrides(overrides);
            else foreach (var clip in settledController.animationClips)
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(clip, clip));
            overrides.RemoveAll(pair => pair.Key.name == DrawPlaceholder || pair.Key.name == SheathPlaceholder);
            transitionController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = transitionController;
            animator.CrossFadeInFixedTime("MoveTree", transitionBlend);
        }
        else ReleaseTransitionController();
        PresentationChanged?.Invoke();
    }

    private void ReleaseTransitionController()
    {
        if (settledController != null && animator != null) animator.runtimeAnimatorController = settledController;
        settledController = null;
        if (transitionController != null) { Destroy(transitionController); transitionController = null; }
    }

    private void ApplyStablePresentation()
    {
        stance.SetCombatPolicy(combatAllowed, applyAnimator: false);
        stance.SetStance(ActiveWeapon != null ? ActiveWeapon.Stance : PlayerStance.Unarmed, true);
        UpdateModel(combatAllowed);
    }

    private void UpdateModel(bool drawn, bool blendAttachment = false)
    {
        blendingAttachment = false;
        var definition = ActiveWeapon;
        if (modelDefinition != definition)
        {
            if (model != null) { model.SetActive(false); Destroy(model); }
            model = null;
            modelDefinition = definition;
            if (definition != null && definition.ModelPrefab != null)
            {
                model = Instantiate(definition.ModelPrefab, transform);
                model.name = "Equipped Weapon - " + definition.DisplayName;
                foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            }
        }
        modelDrawn = drawn && model != null;
        if (model == null) return;
        Transform bone = animator.isHuman ? animator.GetBoneTransform(modelDrawn ? definition.HandBone : definition.SheathBone) : null;
        model.SetActive(bone != null);
        if (bone == null) return;
        model.transform.SetParent(bone, blendAttachment);
        if (blendAttachment)
        {
            attachmentStartPosition = model.transform.localPosition;
            attachmentStartRotation = model.transform.localRotation;
            attachmentElapsed = 0f;
            blendingAttachment = true;
        }
        else
        {
            model.transform.localPosition = MountPosition(definition, modelDrawn);
            model.transform.localRotation = MountRotation(definition, modelDrawn);
        }
        model.transform.localScale = appearance != null && appearance.RigProfile != null ? appearance.RigProfile.Scale(definition.ModelScale) : definition.ModelScale;
    }
    Vector3 MountPosition(WeaponDefinition definition, bool drawn)
    {
        var position = drawn ? definition.HandPosition : definition.SheathPosition;
        var profile = appearance != null ? appearance.RigProfile : null;
        return profile != null ? profile.MountPosition(definition, drawn) : position;
    }
    Quaternion MountRotation(WeaponDefinition definition, bool drawn)
    {
        var rotation = drawn ? definition.HandRotation : definition.SheathRotation;
        var profile = appearance != null ? appearance.RigProfile : null;
        return profile != null ? profile.MountRotation(definition, drawn) : Quaternion.Euler(rotation);
    }
}
