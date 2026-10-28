using System;
using System.IO;
using System.Linq;

namespace RPGSilent.Domain
{
    [Serializable]
    public sealed class EquipmentLoadoutData
    {
        public const int MaxPresets = 3;
        public int slot;
        public string[] armorKeys = Enumerable.Repeat(string.Empty, InventoryData.SlotCount).ToArray();
        public string[] weaponKeys = new[] { string.Empty, string.Empty };
        public int activeWeaponSlot;
        public EquipmentLoadoutData Copy() => new() { slot = slot, armorKeys = (string[])armorKeys.Clone(),
            weaponKeys = (string[])weaponKeys.Clone(), activeWeaponSlot = activeWeaponSlot };
        public static void Validate(EquipmentLoadoutData data)
        {
            if (data == null || data.slot < 0 || data.slot >= MaxPresets || data.armorKeys == null || data.armorKeys.Length != InventoryData.SlotCount ||
                data.weaponKeys == null || data.weaponKeys.Length != 2 || data.activeWeaponSlot < 0 || data.activeWeaponSlot >= 2 ||
                data.armorKeys.Concat(data.weaponKeys).Any(k => k == null || (k.Length > 0 && !Guid.TryParseExact(k, "N", out _))))
                throw new InvalidDataException("配装方案格式无效，原存档已保留。");
        }
    }
}
