/// <summary>One pending intent, latest input wins. The owner supplies scaled gameplay time.</summary>
public sealed class CombatActionBuffer
{
    public const int BasicAttack = -1;
    public const int Empty = -2;
    public const float Lifetime = .3f;
    int action = Empty;
    float expires;
    public void Request(int value, float now)
    {
        if (value < BasicAttack || value >= PlayerSkillController.MaxSlots || float.IsNaN(now) || float.IsInfinity(now)) return;
        action = value; expires = now + Lifetime;
    }
    public int Peek(float now)
    {
        if (float.IsNaN(now) || float.IsInfinity(now) || now > expires) Clear();
        return action;
    }
    public void Clear() => action = Empty;
}
