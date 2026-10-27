using UnityEngine;

// Legacy preserves the exact appearance of saves created before character creation.
public enum PlayerGender { Legacy = 0, Male = 1, Female = 2 }

[CreateAssetMenu(menuName = "RPG/Player Class")]
public sealed class PlayerClassDefinition : ScriptableObject
{
    public string Id;
    public string DisplayName;
    [TextArea] public string Description;
    public PlayerClassDefinition BaseClass;
    public GameObject ModelPrefab;
    public GameObject MaleModelPrefab;
    public GameObject FemaleModelPrefab;
    public AnimationClip PreviewIdle;
    public WeaponDefinition StartingWeapon;
    public SkillData[] Lessons = new SkillData[0];
    public int RequiredLevel = 10;
    public int AdvancementCost = 300;
    public bool IsAdvanced => BaseClass != null;
    public string FamilyId => IsAdvanced ? BaseClass.Id : Id;
    public float DamageMultiplier => IsAdvanced ? 1.1f : 1f;
    public GameObject GetModel(PlayerGender gender) => gender switch
    {
        PlayerGender.Male => MaleModelPrefab,
        PlayerGender.Female => FemaleModelPrefab,
        PlayerGender.Legacy => ModelPrefab,
        _ => null
    };
}
