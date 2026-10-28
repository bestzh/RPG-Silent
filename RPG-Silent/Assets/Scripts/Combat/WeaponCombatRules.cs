using UnityEngine;

public static class WeaponCombatRules
{
    public const float ChargeThreshold = .18f;
    public static bool CanCharge(PlayerStance stance) => stance == PlayerStance.TwoHandSword || stance == PlayerStance.TwoHandBow;
    public static float ChargeDuration(PlayerStance stance) => stance == PlayerStance.TwoHandSword ? 1.2f : 1f;
    public static float ChargeRatio(PlayerStance stance, float held) => !CanCharge(stance) || float.IsNaN(held) ? 0
        : Mathf.Clamp01((held - ChargeThreshold) / (ChargeDuration(stance) - ChargeThreshold));
    public static float ChargeDamage(PlayerStance stance, float ratio) => Mathf.Lerp(1, stance == PlayerStance.TwoHandSword ? 2.4f : 2.2f, Mathf.Clamp01(ratio));
    public static float AttackSpeed(PlayerStance stance, int combo) => stance switch
    {
        PlayerStance.OneHandSword => combo == 3 ? 1.12f : 1.28f,
        PlayerStance.TwoHandStaff => combo == 3 ? 1f : 1.2f,
        _ => 1f
    };
}
