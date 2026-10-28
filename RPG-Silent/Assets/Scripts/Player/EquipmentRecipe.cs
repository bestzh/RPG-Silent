using UnityEngine;
[CreateAssetMenu(menuName = "RPG/Equipment Recipe")]
public sealed class EquipmentRecipe : ScriptableObject
{
    public string Id;
    public ItemDefinition Result;
    public string UnlockQuestId = string.Empty;
    public int Gold = 200, ForgeShards = 8, CrystalShards = 2;
}
