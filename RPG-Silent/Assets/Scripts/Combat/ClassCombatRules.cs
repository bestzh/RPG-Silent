using UnityEngine;

public enum CombatElement { None, Fire, Frost, Storm, Earth }

/// <summary>Per-target, expiring class interactions. No persistent character data.</summary>
public sealed class ClassCombatRules
{
    int swordHits;
    float swordExpires, brokenUntil, elementExpires, markedUntil;
    CombatElement previousElement;
    public bool IsBroken(float now) => now < brokenUntil;
    public bool IsMarked(float now) => now < markedUntil;
    public string LastEffect { get; private set; }

    public float Hit(string family, bool skill, CombatElement element, bool behind, bool vulnerable, float now)
    {
        LastEffect = "";
        float multiplier = 1f;
        if (family == "swordsman")
        {
            if (now >= swordExpires) swordHits = 0;
            swordExpires = now + 4f;
            if (++swordHits >= 3)
            { swordHits = 0; brokenUntil = now + 4f; LastEffect = "破防 · 4 秒增伤"; }
            if (IsBroken(now)) multiplier = 1.25f;
        }
        else if (family == "ranger")
        {
            if (behind || vulnerable || IsMarked(now))
            { multiplier = 1.5f; markedUntil = 0; LastEffect = "弱点攻击"; }
            if (skill) markedUntil = now + 5f;
        }
        else if (family == "mage" && element != CombatElement.None)
        {
            if (now < elementExpires && previousElement != CombatElement.None && previousElement != element)
            { multiplier = 1.4f; previousElement = CombatElement.None; elementExpires = 0; LastEffect = "元素共鸣 · 回能 5"; }
            else { previousElement = element; elementExpires = now + 4f; }
        }
        return multiplier;
    }

    public static bool Behind(Vector3 forward, Vector3 towardAttacker)
    { forward.y = towardAttacker.y = 0; return towardAttacker.sqrMagnitude > .01f && Vector3.Dot(forward.normalized, towardAttacker.normalized) < -.25f; }
    public static string Description(string family) => family switch
    {
        "swordsman" => "破防：4 秒内连续命中 3 次，目标 4 秒内承伤 +25%，更易硬直。",
        "ranger" => "弱点：背面 / 破绽目标伤害 +50%；技能标记目标 5 秒，下次攻击消耗标记。",
        "mage" => "共鸣：4 秒内命中不同元素技能，后击伤害 +40% 并回复 5 能量。",
        _ => ""
    };
}
