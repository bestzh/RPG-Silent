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
    public void Deal(int damage) { if (damage <= 0) return; DamageDealt += damage; Hits++; }
    public void Receive(int damage) { if (damage > 0) DamageTaken += damage; }
    public void Defend(DefenseOutcome outcome)
    {
        if (outcome == DefenseOutcome.Parried) PerfectParries++;
        if (outcome == DefenseOutcome.PerfectDodge) PerfectDodges++;
        if (outcome == DefenseOutcome.Blocked) Blocks++;
    }
    public void Release(bool skill, int combo) { if (skill) Skills++; else if (combo >= 3) ComboFinishers++; }
    public void Reset() { DamageDealt = DamageTaken = Hits = PerfectParries = PerfectDodges = Blocks = ComboFinishers = Skills = 0; }
}
