using RPGSilent.Combat;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public sealed class PlayerDefense : MonoBehaviour
{
    public AudioClip GuardSound;
    public AudioMixerGroup Mixer;
    PlayerController player;
    PlayerSkillController skills;
    AudioSource audioSource;
    bool held, suppressed, perfectDodgeUsed;
    float rollStarted = float.NegativeInfinity;
    float guardStarted = float.NegativeInfinity;
    float nextParryAt;
    float brokenUntil;
    float counterUntil;
    float feedbackUntil;
    float reactionUntil;
    string feedback;
    public bool IsGuarding => player != null && player.StateMachine.CurrentState is GuardState;
    public bool IsInvulnerable => CanDefend && player.IsRolling && DefenseRules.IsInvulnerable(Time.time - rollStarted);
    public bool CounterReady => CanDefend && Time.time < counterUntil;
    public string Feedback => Time.time < feedbackUntil ? feedback : "";
    public DefenseOutcome LastOutcome { get; private set; }
    bool CanDefend => player != null && player.isActiveAndEnabled && player.CanReceiveCombatEffects;
    public bool CanAffordRoll => !player.CombatAllowed || player.CombatStats != null && player.CombatStats.CurrentEnergy >= DefenseRules.RollCost;
    public string GuardMotion => player.CurrentStance switch
    {
        PlayerStance.TwoHandSword or PlayerStance.TwoHandAxe => "GuardHeavy",
        PlayerStance.TwoHandBow or PlayerStance.TwoHandCrossbow => "GuardBow",
        PlayerStance.TwoHandStaff or PlayerStance.TwoHandSpear => "GuardStaff",
        _ => "GuardSword"
    };
    void Awake()
    {
        player = GetComponent<PlayerController>(); skills = GetComponent<PlayerSkillController>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false; audioSource.spatialBlend = .5f;
        audioSource.outputAudioMixerGroup = Mixer;
    }
    public void PressGuard()
    {
        if (held) return;
        held = true; suppressed = false;
        Tick();
    }
    public void ReleaseGuard() { held = false; suppressed = false; StopGuard(); }
    public void Tick()
    {
        if (!CanDefend || !player.GameplayInputEnabled || Cursor.lockState != CursorLockMode.Locked)
        { held = false; suppressed = false; EndRoll(); counterUntil = 0; StopGuard(); return; }
        if (IsGuarding || !held || suppressed || Time.time < brokenUntil || !player.IsGrounded
            || player.IsRolling || player.IsJumping || player.Weapons != null && player.Weapons.IsBusy) return;
        if (player.CurrentStance == PlayerStance.Relax || player.CombatStats == null || player.CombatStats.CurrentEnergy <= 0) return;
        var state = player.StateMachine.CurrentState;
        if (state is not IdleState && state is not MoveState && !(state is AttackState attack && attack.CanLink)) return;
        if (skills != null && !skills.TryFinishForLink()) return;
        player.ClearCombatInput();
        player.StateMachine.ChangeState(new GuardState(player));
    }
    public bool BeginRoll()
    {
        if (player.CombatAllowed && (player.CombatStats == null || !player.CombatStats.TrySpendEnergy(DefenseRules.RollCost))) return false;
        SuppressGuard(); rollStarted = Time.time; perfectDodgeUsed = false; return true;
    }
    public void EndRoll() => rollStarted = float.NegativeInfinity;
    public void SuppressGuard() { suppressed = true; StopGuard(); }
    void StopGuard()
    {
        if (!IsGuarding) return;
        if (player.isActiveAndEnabled && player.animator.isActiveAndEnabled) player.ReturnToLocomotion();
        else player.StateMachine.ChangeState(new IdleState(player));
    }
    public void EnterGuard()
    {
        guardStarted = Time.time >= nextParryAt ? Time.time : float.NegativeInfinity;
        nextParryAt = Time.time + .5f;
        reactionUntil = 0;
        player.AnimationController?.CancelAnimation();
        player.animator.CrossFadeInFixedTime(GuardMotion, .08f, 0, 0);
    }
    public void ExitGuard() { guardStarted = float.NegativeInfinity; }
    public void UpdateGuardAnimation()
    {
        if (reactionUntil <= 0 || Time.time < reactionUntil) return;
        reactionUntil = 0;
        player.animator.CrossFadeInFixedTime(GuardMotion, .08f, 0, 0);
    }
    public DefenseOutcome Resolve(int damage, Vector3 source, AttackContext attack)
    {
        LastOutcome = DefenseOutcome.Hit;
        if (!CanDefend || damage <= 0) return LastOutcome;
        if (IsInvulnerable)
        {
            bool perfect = !perfectDodgeUsed && DefenseRules.IsPerfectDodge(Time.time - rollStarted);
            LastOutcome = perfect ? DefenseOutcome.PerfectDodge : DefenseOutcome.Dodged;
            player.CombatRecord.Defend(LastOutcome);
            if (perfect) { perfectDodgeUsed = true; GrantCounter("精准闪避 · 反击就绪"); }
            return LastOutcome;
        }
        if (!IsGuarding || attack == null || !DefenseRules.CanBlock(attack.Profile.DeliveryType)
            || !DefenseRules.IsFrontal(player.FacingRotation * Vector3.forward, source - transform.position)) return LastOutcome;
        if (DefenseRules.CanParry(attack.Profile.DeliveryType) && DefenseRules.IsParryWindow(Time.time - guardStarted))
        {
            guardStarted = float.NegativeInfinity; // one perfect impact per press
            LastOutcome = DefenseOutcome.Parried;
            player.CombatRecord.Defend(LastOutcome);
            player.CombatStats?.RecoverEnergy(8f);
            attack.Owner?.GetComponent<EnemyController>()?.ReceiveParry();
            GrantCounter("精准格挡 · 反击就绪"); PlayImpact();
            return LastOutcome;
        }
        float cost = DefenseRules.BlockCost(damage);
        if (player.CombatStats == null || !player.CombatStats.TrySpendEnergy(cost))
        {
            if (player.CombatStats != null) player.CombatStats.TrySpendEnergy(player.CombatStats.CurrentEnergy);
            brokenUntil = Time.time + .8f; counterUntil = 0; SuppressGuard();
            player.animator.CrossFadeInFixedTime(GuardMotion + "Break", .04f, 0, 0);
            Show("防御被击破", 1.2f); LastOutcome = DefenseOutcome.GuardBroken;
            return LastOutcome;
        }
        LastOutcome = DefenseOutcome.Blocked; player.CombatRecord.Defend(LastOutcome); Show("格挡", .5f); PlayImpact();
        return LastOutcome;
    }
    void PlayImpact()
    {
        player.animator.CrossFadeInFixedTime(GuardMotion + "Hit", .04f, 0, 0);
        reactionUntil = Time.time + .25f;
        if (GuardSound != null) audioSource.PlayOneShot(GuardSound, .6f);
    }
    void GrantCounter(string message) { counterUntil = Time.time + DefenseRules.CounterDuration; Show(message, 1.1f); }
    void Show(string message, float duration) { feedback = message; feedbackUntil = Time.time + duration; }
    public float ConsumeCounter()
    {
        if (!CounterReady) return 1f;
        counterUntil = 0; Show("反击", .6f); return DefenseRules.CounterMultiplier;
    }
    public void ResetDefense()
    {
        held = suppressed = perfectDodgeUsed = false;
        rollStarted = guardStarted = float.NegativeInfinity;
        counterUntil = feedbackUntil = reactionUntil = brokenUntil = nextParryAt = 0;
        StopGuard();
    }
    void OnDisable() => ResetDefense();
}
