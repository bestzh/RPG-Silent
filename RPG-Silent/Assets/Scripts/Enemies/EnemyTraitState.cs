using System;

public enum EnemyEliteTrait { None, Shield, Frenzy, Mender }

/// <summary>Encounter-local trait values. Never serialized into character progress.</summary>
public sealed class EnemyTraitState
{
    public const float MendWindup = 1.2f, MendCooldown = 12f, MendRange = 10f, MendTargetLock = 8f;
    public EnemyEliteTrait Trait { get; private set; }
    public int MaxShield { get; private set; }
    public int Shield { get; private set; }
    public bool Enraged { get; private set; }
    public float MendReadyAt { get; private set; }
    public float BrokenUntil { get; private set; }
    public float DamageMultiplier => Enraged ? 1.25f : 1f;
    public float MovementMultiplier => Enraged ? 1.2f : 1f;
    public bool Configure(EnemyEliteTrait trait, int health)
    {
        if (Trait != EnemyEliteTrait.None || !Enum.IsDefined(typeof(EnemyEliteTrait), trait) || trait == EnemyEliteTrait.None || health <= 0) return false;
        Trait = trait; Reset(health); return true;
    }
    public void Reset(int health)
    {
        MaxShield = Trait == EnemyEliteTrait.Shield ? Math.Max(1, health / 4) : 0;
        Shield = MaxShield; Enraged = false; MendReadyAt = BrokenUntil = 0;
    }
    public void Clear() { Trait = EnemyEliteTrait.None; Reset(1); }
    public int Absorb(int damage, float now, out int absorbed)
    {
        absorbed = 0;
        if (damage <= 0 || !ValidTime(now)) return 0;
        if (Shield > 0)
        {
            absorbed = Math.Min(damage, Shield); Shield -= absorbed;
            if (Shield == 0) BrokenUntil = now + 1.2f;
        }
        return damage - absorbed;
    }
    public bool UpdateFrenzy(int health, int maximum)
    {
        if (Trait != EnemyEliteTrait.Frenzy || Enraged || health <= 0 || maximum <= 0 || (long)health * 100 > (long)maximum * 40) return false;
        Enraged = true; return true;
    }
    public bool TryStartMend(float now)
    {
        if (Trait != EnemyEliteTrait.Mender || !ValidTime(now) || now < MendReadyAt) return false;
        MendReadyAt = now + MendCooldown; return true;
    }
    public static int MendAmount(int maximum) => maximum <= 0 ? 0 : Math.Max(1, (int)((long)maximum * 15 / 100));
    public static string Name(EnemyEliteTrait trait) => trait switch
    {
        EnemyEliteTrait.Shield => "[盾] 护盾精英",
        EnemyEliteTrait.Frenzy => "[!] 狂暴精英",
        EnemyEliteTrait.Mender => "[+] 治疗精英",
        _ => "精英"
    };
    static bool ValidTime(float value) => value >= 0 && !float.IsNaN(value) && !float.IsInfinity(value);
}
