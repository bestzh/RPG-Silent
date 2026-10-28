using UnityEngine;

public enum DungeonChallenge { Exploration, Timed, BossTrial }
public static class DungeonChallengeRules
{
    public static string Label(DungeonChallenge mode) => mode switch { DungeonChallenge.Timed => "限时挑战", DungeonChallenge.BossTrial => "首领试炼", _ => "普通探索" };
    public static bool Completed(DungeonChallenge mode, bool cleared, float elapsed, float limit) => cleared && (mode != DungeonChallenge.Timed || elapsed <= limit);
    public static int BonusGold(DungeonChallenge mode, bool completed, float rewardScale) => !completed ? 0 : Mathf.RoundToInt((mode == DungeonChallenge.Timed ? 120 : mode == DungeonChallenge.BossTrial ? 180 : 0) * Mathf.Max(0,rewardScale));
    public static string Duration(float seconds) => $"{Mathf.FloorToInt(Mathf.Max(0,seconds) / 60):00}:{Mathf.FloorToInt(Mathf.Max(0,seconds)) % 60:00}";
}
