using UnityEngine;

public enum AttackImpact { Light, Heavy }

public static class HitReactionRules
{
    public const float RecoveryProtection = .3f;
    public static float Duration(AttackImpact impact, bool guardBroken = false) => guardBroken ? .8f : impact == AttackImpact.Heavy ? .65f : .3f;
    public static float PushDuration(AttackImpact impact) => impact == AttackImpact.Heavy ? .32f : .16f;
    public static float PushSpeed(AttackImpact impact) => impact == AttackImpact.Heavy ? 7f : 3.5f;
    public static Vector3 Away(Vector3 position, Vector3 source, Vector3 forward)
    {
        Vector3 direction = position - source; direction.y = 0;
        if (direction.sqrMagnitude < .001f) direction = -forward;
        return direction.normalized;
    }
    public static string Direction(Vector3 localSource) => Mathf.Abs(localSource.x) > Mathf.Abs(localSource.z)
        ? localSource.x > 0 ? "Right" : "Left" : localSource.z >= 0 ? "Front" : "Back";
    public static string Family(PlayerStance stance) => stance switch
    {
        PlayerStance.TwoHandSword or PlayerStance.TwoHandAxe => "Heavy",
        PlayerStance.TwoHandBow or PlayerStance.TwoHandCrossbow => "Bow",
        PlayerStance.TwoHandStaff or PlayerStance.TwoHandSpear => "Staff",
        _ => "Sword"
    };
}
