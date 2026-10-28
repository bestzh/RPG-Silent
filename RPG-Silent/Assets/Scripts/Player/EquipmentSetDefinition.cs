using RPGSilent.Domain;
using UnityEngine;

[CreateAssetMenu(menuName = "Progression/Equipment Set")]
public sealed class EquipmentSetDefinition : ScriptableObject
{
    public string Id, DisplayName;
    [Min(0)] public int TwoPieceHealth, FourPieceEnergy, FivePieceAttackPercent;
    public int Bonus(int pieces, ItemAffixStat stat) => stat switch
    {
        ItemAffixStat.Health => pieces >= 2 ? TwoPieceHealth : 0,
        ItemAffixStat.Energy => pieces >= 4 ? FourPieceEnergy : 0,
        ItemAffixStat.AttackPercent => pieces >= 5 ? FivePieceAttackPercent : 0,
        _ => 0
    };
}
