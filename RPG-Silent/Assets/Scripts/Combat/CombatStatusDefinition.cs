using UnityEngine;

public enum CombatStatusKind { Guard, Burning, Chilled }

[CreateAssetMenu(menuName = "Combat/Status")]
public sealed class CombatStatusDefinition : ScriptableObject
{
    public CombatStatusKind Kind;
    public string DisplayName;
    public Sprite Icon;
    public Color Color = Color.white;
    [Range(.1f, 120f)] public float Duration = 6;
    [Range(1, 80)] public int Magnitude = 20;
    public bool IsDebuff => Kind != CombatStatusKind.Guard;
    public string Description => Kind == CombatStatusKind.Guard ? $"额外减伤 {Magnitude}%" : Kind == CombatStatusKind.Chilled ? $"移动减速 {Magnitude}%" : $"每秒 {Magnitude} 点灼烧伤害";
}
