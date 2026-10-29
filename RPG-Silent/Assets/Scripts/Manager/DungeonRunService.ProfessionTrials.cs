public sealed partial class DungeonRunService
{
    public string ProfessionTrialSummary => IsLongChallenge ? "" : progress.ProfessionTrialSummary(Dungeon?.Id ?? 0, CombatRecord);
}
