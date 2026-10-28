using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "Progression/Loot Presentation")]
public sealed class LootPresentation : ScriptableObject
{
    public GameObject CurrencyModel, ItemModel, GlowPrefab;
    public Material GlowMaterial;
    public TMP_FontAsset Font;
}
