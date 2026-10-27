using UnityEngine;

/// <summary>Converts the project's weapon mounts to a model's bind-pose bone axes and units.</summary>
public sealed class CharacterRigProfile : MonoBehaviour
{
    [System.Serializable]
    public sealed class DrawnMount
    {
        public WeaponDefinition Weapon;
        public Vector3 Position;
        public Vector3 Rotation;
    }
    public float UnitsPerMeter = 1;
    public Quaternion RightHandBasis = Quaternion.identity;
    public Quaternion LeftHandBasis = Quaternion.identity;
    public Quaternion ChestBasis = Quaternion.identity;
    public DrawnMount[] DrawnMounts = new DrawnMount[0];
    Quaternion Basis(HumanBodyBones bone) => bone == HumanBodyBones.RightHand ? RightHandBasis :
        bone == HumanBodyBones.LeftHand ? LeftHandBasis : ChestBasis;
    public Vector3 Position(HumanBodyBones bone, Vector3 authored) => Basis(bone) * authored * UnitsPerMeter;
    public Quaternion Rotation(HumanBodyBones bone, Vector3 authored) => Basis(bone) * Quaternion.Euler(authored);
    public Vector3 Scale(Vector3 authored) => authored * UnitsPerMeter;
    public Vector3 MountPosition(WeaponDefinition weapon, bool drawn)
    {
        if (drawn) foreach (var mount in DrawnMounts)
            if (mount != null && mount.Weapon == weapon) return Position(weapon.HandBone, mount.Position);
        return Position(drawn ? weapon.HandBone : weapon.SheathBone, drawn ? weapon.HandPosition : weapon.SheathPosition);
    }
    public Quaternion MountRotation(WeaponDefinition weapon, bool drawn)
    {
        if (drawn) foreach (var mount in DrawnMounts)
            if (mount != null && mount.Weapon == weapon) return Rotation(weapon.HandBone, mount.Rotation);
        return Rotation(drawn ? weapon.HandBone : weapon.SheathBone, drawn ? weapon.HandRotation : weapon.SheathRotation);
    }
}
