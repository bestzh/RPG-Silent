using UnityEngine;

/// <summary>Shared weapon content. Runtime equipment and future upgrade levels belong to the player.</summary>
[CreateAssetMenu(fileName = "Weapon", menuName = "Combat/Weapon")]
public sealed class WeaponDefinition : ScriptableObject
{
    public const string StarterSwordId = "training-sword";
    public const string StarterGreatswordId = "training-greatsword";

    public string PersistentId;
    public string DisplayName;
    public PlayerStance Stance = PlayerStance.OneHandSword;
    public AttackProfile[] ComboProfiles;
    public GameObject ModelPrefab;
    public HumanBodyBones HandBone = HumanBodyBones.RightHand;
    public Vector3 HandPosition;
    public Vector3 HandRotation;
    public Vector3 ModelScale = Vector3.one;
    public HumanBodyBones SheathBone = HumanBodyBones.Chest;
    public Vector3 SheathPosition;
    public Vector3 SheathRotation;

    [Header("拔剑与收剑（背部挂点）")]
    public AnimationClip DrawFromRelax;
    public AnimationClip DrawFromUnarmed;
    public AnimationClip SheathToRelax;
    public AnimationClip SheathToUnarmed;

    public AnimationClip GetDrawClip(bool relaxed) => relaxed ? DrawFromRelax : DrawFromUnarmed;
    public AnimationClip GetSheathClip(bool relaxed) => relaxed ? SheathToRelax : SheathToUnarmed;

    public AttackProfile GetProfile(int comboIndex) => ComboProfiles != null && comboIndex > 0
        && comboIndex <= ComboProfiles.Length ? ComboProfiles[comboIndex - 1] : null;
}
