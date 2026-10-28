using RPGSilent.Domain;
using UnityEngine;
[CreateAssetMenu(menuName = "RPG/Achievement")]
public sealed class AchievementDefinition : ScriptableObject
{
    public string Id, Title;
    [TextArea] public string Description;
    public CollectionKind Kind;
    public bool Distinct;
    public int Required = 1;
    public int Gold = 100, Experience = 100, ForgeShards = 2;
}
