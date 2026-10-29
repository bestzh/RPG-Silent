using UnityEngine;

public enum ItemKind { Equipment, Weapon, Material, Consumable }
public enum ConsumableEffect { None, Heal, Energy, Guard, Cleanse }
public enum EquipmentSlot { Head, Chest, Feet, Ring, Amulet }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

[CreateAssetMenu(menuName = "Progression/Item")]
public sealed class ItemDefinition : ScriptableObject
{
    public string Id, DisplayName;
    [TextArea] public string Description;
    public ItemKind Kind;
    public ItemRarity Rarity;
    public RPGSilent.Domain.EquipmentSpecialEffect SpecialEffect;
    public EquipmentSlot Slot;
    public Sprite Icon;
    public WeaponDefinition Weapon;
    public ArmorAppearanceDefinition Appearance;
    public EquipmentSetDefinition Set;
    public bool IsGem;
    public ConsumableEffect UseEffect;
    [Min(0)] public int UseAmount;
    [Range(1, 10000)] public int BuyPrice = 25;
    public CombatStatusDefinition UseStatus;
    public GameObject UseVfx;
    public bool IsStackable => Kind == ItemKind.Material || Kind == ItemKind.Consumable;
    [Min(1)] public int RequiredLevel = 1;
    [Min(0)] public int Health, Energy, AttackPercent, DefensePercent, SellPrice = 10;
    public string RarityLabel => new[] { "普通", "精良", "稀有", "史诗", "传说", "神话" }[(int)Rarity];
    public string SlotLabel => Kind == ItemKind.Weapon ? "武器" : Kind == ItemKind.Consumable ? "药品" : Kind == ItemKind.Material ? IsGem ? "宝石" : "材料" : SlotName(Slot);
    public static string SlotName(EquipmentSlot slot) => new[] { "头部", "护甲", "靴子", "戒指", "项链" }[(int)slot];
    public Color QualityColor => Rarity switch
    {
        ItemRarity.Uncommon => new Color(.36f, .81f, .62f), ItemRarity.Rare => new Color(.36f, .66f, 1f),
        ItemRarity.Epic => new Color(.76f, .48f, 1f), ItemRarity.Legendary => new Color(1f, .74f, .2f),
        ItemRarity.Mythic => new Color(1f, .2f, .23f), _ => new Color(.88f, .92f, .96f)
    };
}
