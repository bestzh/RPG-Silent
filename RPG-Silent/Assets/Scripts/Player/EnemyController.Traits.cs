using UnityEngine;

public partial class EnemyController
{
    readonly EnemyTraitState traits = new();
    EnemyController mendTarget;
    float mendElapsed, nextSupportHealAt;
    LineRenderer mendLine;
    GameObject mendWarning;
    public EnemyEliteTrait EliteTrait => traits.Trait;
    public int CurrentShield => traits.Shield;
    public int MaxShield => traits.MaxShield;
    public bool IsEnraged => traits.Enraged;
    public float MendCooldownRemaining => Mathf.Max(0, traits.MendReadyAt - Time.time);
    public EnemyController MendTarget => mendTarget;
    internal bool HasSupportSight(EnemyController target) => target != null && perception.CanSee(target.transform);
    public float MendProgress => mendTarget == null ? 0 : Mathf.Clamp01(mendElapsed / EnemyTraitState.MendWindup);
    public bool CanReceiveSupportHealing => isActiveAndEnabled && HasAggro && !ReturningHome && !combatLocked && !IsDead &&
        !IsBoss && !IsCompanion && !IsTrainingTarget && EliteTrait != EnemyEliteTrait.Mender && CurrentHealth < MaxHealth && Time.time >= nextSupportHealAt;
    public string TraitHint => EliteTrait switch
    {
        EnemyEliteTrait.Shield => traits.Shield > 0 ? $"[盾] 护盾 {traits.Shield}/{traits.MaxShield} · 击破可打断" :
            Time.time < traits.BrokenUntil ? "[盾] 已击破 · 反击机会" : "[盾] 护盾已击破",
        EnemyEliteTrait.Frenzy => traits.Enraged ? "[!] 狂暴 · 伤害 +25% / 移速 +20%" : "[!] 生命 ≤40% 时狂暴",
        EnemyEliteTrait.Mender => mendTarget != null ? $"[+] 治疗蓄力 {Mathf.Max(0, EnemyTraitState.MendWindup - mendElapsed):0.0}s · 可打断" :
            MendCooldownRemaining > 0 ? $"[+] 治疗冷却 {MendCooldownRemaining:0.0}s" : "[+] 支援受伤同伴 · 优先打断",
        _ => string.Empty
    };

    public bool TryConfigureEliteTrait(EnemyEliteTrait trait)
    {
        if (IsDead || combatLocked || IsBoss || IsCompanion || IsTrainingTarget || HasAggro || MaxHealth <= 0 || EliteTrait != EnemyEliteTrait.None ||
            GetComponent<CombatFaction>()?.Team != CombatTeam.Enemy || !System.Enum.IsDefined(typeof(EnemyEliteTrait), trait) || trait == EnemyEliteTrait.None) return false;
        ConfigureElite();
        if (!traits.Configure(trait, MaxHealth)) return false;
        EnsureAllegianceLabel(); Changed?.Invoke(this); return true;
    }
    void EnsureAllegianceLabel()
    {
        if (GetComponent<EnemyAllegianceLabel>() == null) gameObject.AddComponent<EnemyAllegianceLabel>();
    }
    void ClearNonHostileTraits()
    {
        CancelMend(); traits.Clear(); nextSupportHealAt = 0;
        if (IsElite) { IsElite = false; MaxHealth = Mathf.Max(1, MaxHealth / 2); transform.localScale /= 1.15f; }
    }
    bool TryBeginMend()
    {
        if (EliteTrait != EnemyEliteTrait.Mender || IsCompanion || IsTrainingTarget || !HasAggro || ReturningHome || CombatGroup == null ||
            Time.time < traits.MendReadyAt || !CombatGroup.TryFindMendTarget(this, out var target) || !traits.TryStartMend(Time.time)) return false;
        CancelAction();
        mendTarget = target; mendElapsed = 0;
        EnterState(EnemyState.Supporting, true);
        if (CombatWarningMaterial != null)
        {
            mendWarning = new GameObject("Healing warning - interruptible");
            mendLine = mendWarning.AddComponent<LineRenderer>(); mendLine.sharedMaterial = CombatWarningMaterial;
            mendLine.positionCount = 2; mendLine.startWidth = mendLine.endWidth = .12f;
            mendLine.startColor = mendLine.endColor = new Color(.3f, 1, .45f, .9f);
            mendLine.useWorldSpace = true;
            mendLine.SetPosition(0, transform.position + Vector3.up * 1.5f);
            mendLine.SetPosition(1, mendTarget.transform.position + Vector3.up * 1.2f);
        }
        return true;
    }
    void TickMend(float deltaTime)
    {
        if (mendTarget == null || !mendTarget.CanReceiveSupportHealing || mendTarget.CombatGroup != CombatGroup ||
            Vector3.Distance(transform.position, mendTarget.transform.position) > EnemyTraitState.MendRange || !perception.CanSee(mendTarget.transform))
        { CancelMend(); BeginRecovery(.5f); return; }
        mendElapsed += Mathf.Max(0, deltaTime);
        motor.Face(mendTarget.transform.position - transform.position);
        if (mendLine != null)
        { mendLine.SetPosition(0, transform.position + Vector3.up * 1.5f); mendLine.SetPosition(1, mendTarget.transform.position + Vector3.up * 1.2f); }
        if (mendElapsed < EnemyTraitState.MendWindup) return;
        mendTarget.ReceiveSupportHealing(this);
        CancelMend(); BeginRecovery(.65f);
    }
    void ReceiveSupportHealing(EnemyController source)
    {
        if (!CanReceiveSupportHealing || source == null || source == this || source.EliteTrait != EnemyEliteTrait.Mender || source.IsDead ||
            source.combatLocked || source.mendTarget != this || source.State != EnemyState.Supporting || source.CombatGroup != CombatGroup ||
            gameObject.scene != source.gameObject.scene || !source.perception.CanSee(transform)) return;
        CurrentHealth = (int)System.Math.Min(MaxHealth, (long)CurrentHealth + EnemyTraitState.MendAmount(MaxHealth));
        nextSupportHealAt = Time.time + EnemyTraitState.MendTargetLock;
        Changed?.Invoke(this);
    }
    void CancelMend()
    {
        mendTarget = null; mendElapsed = 0; mendLine = null;
        if (mendWarning != null) { mendWarning.SetActive(false); Destroy(mendWarning); }
        mendWarning = null;
    }
    void ResetEncounterHealth()
    {
        CancelAction(true); phases?.ResetEncounter(); CurrentHealth = MaxHealth; staggerDamage = 0;
        Statuses?.State.Clear(); skills.ResetCooldowns(); sequenceReadyAt.Clear(); traits.Reset(MaxHealth); nextSupportHealAt = 0;
        motor.Speed = moveSpeed; HasAggro = false; nextAttack = Time.time + .8f; Changed?.Invoke(this);
    }
}
