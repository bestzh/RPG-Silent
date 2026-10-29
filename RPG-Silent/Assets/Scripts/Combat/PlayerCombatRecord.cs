/// <summary>Actual damage and resolved defenses, owned by the current player/run.</summary>
public sealed class PlayerCombatRecord
{
    public int DamageDealt { get; private set; }
    public int DamageTaken { get; private set; }
    public int Hits { get; private set; }
    public int PerfectParries { get; private set; }
    public int PerfectDodges { get; private set; }
    public int Blocks { get; private set; }
    public int ComboFinishers { get; private set; }
    public int Skills { get; private set; }
    public int SkillHits { get; private set; }
    public int RangedHits { get; private set; }
    public int HeavyHits { get; private set; }
    public int CompanionHits { get; private set; }
    int resolvingHits;
    System.Action afterHit;
    internal void BeginHit() => resolvingHits++;
    internal void EndHit()
    {
        if (resolvingHits <= 0 || --resolvingHits != 0) return;
        var action = afterHit; afterHit = null; action?.Invoke();
    }
    // Enemy death can settle the run inside ReceiveHit; include its killing blow first.
    public void WhenHitResolved(System.Action action) { if (resolvingHits == 0) action(); else afterHit += action; }
    public void CountHit(int damage, bool skill, bool heavy, bool ranged, bool companion, bool equipmentProc)
    {
        if (damage <= 0 || equipmentProc) return;
        if (companion) CompanionHits = System.Math.Min(1000000, CompanionHits + 1);
        else if (skill) SkillHits = System.Math.Min(1000000, SkillHits + 1);
        else if (ranged) RangedHits = System.Math.Min(1000000, RangedHits + 1);
        else if (heavy) HeavyHits = System.Math.Min(1000000, HeavyHits + 1);
    }
    public void Deal(int damage) { if (damage <= 0) return; DamageDealt += damage; Hits++; }
    public void Receive(int damage) { if (damage > 0) DamageTaken += damage; }
    public void Defend(DefenseOutcome outcome)
    {
        if (outcome == DefenseOutcome.Parried) PerfectParries++;
        if (outcome == DefenseOutcome.PerfectDodge) PerfectDodges++;
        if (outcome == DefenseOutcome.Blocked) Blocks++;
    }
    public void Release(bool skill, int combo) { if (skill) Skills++; else if (combo >= 3) ComboFinishers++; }
    public void Reset()
    {
        DamageDealt = DamageTaken = Hits = PerfectParries = PerfectDodges = Blocks = ComboFinishers = Skills = 0;
        SkillHits = RangedHits = HeavyHits = CompanionHits = resolvingHits = 0; afterHit = null;
    }
}
