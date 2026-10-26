using UnityEngine;

public enum CombatTeam { Neutral, Player, Enemy }

/// <summary>阵营与伤害接口独立；未配置的旧对象保持原有中立行为。</summary>
public sealed class CombatFaction : MonoBehaviour
{
    public CombatTeam Team = CombatTeam.Neutral;

    public static CombatTeam Resolve(Component target)
    {
        if (target == null) return CombatTeam.Neutral;
        var faction = target.GetComponentInParent<CombatFaction>();
        if (faction != null) return faction.Team;
        return target.CompareTag("Player") ? CombatTeam.Player : CombatTeam.Neutral;
    }
}
