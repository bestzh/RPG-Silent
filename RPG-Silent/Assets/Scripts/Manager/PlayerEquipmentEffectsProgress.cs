using System;
using RPGSilent.Domain;

public sealed partial class PlayerProgressService
{
    readonly int[] equipmentEffectLevels = new int[EquipmentEffectRules.EffectCount];
    public int EquipmentEffectLevel(EquipmentSpecialEffect effect) => EquipmentEffectRules.Valid(effect) ? equipmentEffectLevels[(int)effect] : 0;
    void RefreshEquipmentEffectLevels()
    {
        Array.Clear(equipmentEffectLevels, 0, equipmentEffectLevels.Length);
        foreach (string key in inventory.equipped) AddEquipmentEffect(key);
        if (weapons != null) AddEquipmentEffect(weapons.GetInstanceId(weapons.ActiveSlot));
    }
    void AddEquipmentEffect(string key)
    {
        var entry = inventory.Find(key);
        if (entry == null || !items.TryGetValue(entry.itemId, out var definition)) return;
        int level = EquipmentEffectRules.Level(definition);
        if (level > 0) equipmentEffectLevels[(int)definition.SpecialEffect] = Math.Max(equipmentEffectLevels[(int)definition.SpecialEffect], level);
    }
    public string EquipmentEffectPosition(string key)
    {
        if (weapons != null)
        {
            if (weapons.GetInstanceId(weapons.ActiveSlot) == key) return "当前武器 · 战斗中生效";
            for (int i = 0; i < PlayerWeaponEquipment.SlotCount; i++)
                if (weapons.GetInstanceId(i) == key) return "备用武器 · 当前不提供特效";
        }
        foreach (string worn in inventory.equipped) if (worn == key) return "已穿戴 · 战斗中生效";
        return "未穿戴 · 确认装备后才会生效";
    }
}
