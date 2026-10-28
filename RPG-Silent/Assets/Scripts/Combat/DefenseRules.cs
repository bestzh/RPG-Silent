using UnityEngine;

public enum DefenseOutcome { Hit, Dodged, PerfectDodge, Blocked, Parried, GuardBroken }

/// <summary>Shared timing and directional rules; all times use the scaled combat clock.</summary>
public static class DefenseRules
{
    public const float RollCost = 10f;
    public const float CounterDuration = 2f;
    public const float CounterMultiplier = 1.5f;
    public static bool IsInvulnerable(float elapsed) => elapsed >= .06f && elapsed <= .36f;
    public static bool IsPerfectDodge(float elapsed) => elapsed >= .06f && elapsed <= .18f;
    public static bool IsParryWindow(float elapsed) => elapsed >= 0f && elapsed <= .18f;
    public static bool IsFrontal(Vector3 forward, Vector3 sourceOffset)
    {
        forward.y = sourceOffset.y = 0;
        return sourceOffset.sqrMagnitude > .0001f && Vector3.Dot(forward.normalized, sourceOffset.normalized) >= .5f;
    }
    public static bool CanBlock(AttackDeliveryType delivery) => delivery == AttackDeliveryType.MeleeArc
        || delivery == AttackDeliveryType.MeleeHitbox || delivery == AttackDeliveryType.Projectile || delivery == AttackDeliveryType.Raycast;
    public static bool CanParry(AttackDeliveryType delivery) => delivery == AttackDeliveryType.MeleeArc || delivery == AttackDeliveryType.MeleeHitbox;
    public static float BlockCost(int damage) => Mathf.Clamp(8f + Mathf.Max(0, damage) * .4f, 8f, 40f);
    public static int BlockDamage(int damage) => damage <= 0 ? 0 : Mathf.Max(1, Mathf.CeilToInt(damage * .2f));
}
