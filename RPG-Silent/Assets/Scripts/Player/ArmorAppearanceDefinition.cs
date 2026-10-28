using System;
using UnityEngine;

[Flags]
public enum ArmorBodyRegion { None = 0, Headwear = 1, Hair = 2, Face = 4, Chest = 8, Feet = 16 }

/// <summary>Mesh references and bind-pose bone paths, without a second skeleton or wardrobe prefab.</summary>
[CreateAssetMenu(menuName = "Progression/Armor Appearance")]
public sealed class ArmorAppearanceDefinition : ScriptableObject
{
    [Serializable]
    public sealed class MeshPart
    {
        public Mesh Mesh;
        public Material[] Materials;
        public string[] BonePaths;
        public string RootBonePath;
        public Bounds Bounds;
    }
    [Serializable]
    public sealed class Variant
    {
        public ArmorBodyRegion HiddenRegions;
        public MeshPart[] Parts = Array.Empty<MeshPart>();
    }
    public EquipmentSlot Slot;
    public Color Primary = Color.white, Secondary = Color.gray, Metal = Color.gray;
    public Variant Male = new(), Female = new();
    public Variant ForGender(PlayerGender gender) => gender == PlayerGender.Female ? Female : Male;
}
