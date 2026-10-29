using System;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class InventoryUI
{
    readonly UIEquipmentSlotView[] wornSlots = new UIEquipmentSlotView[7];
    UIStatTableView itemStatTable, outfitStatTable;
    TMPro.TMP_Text replacementSummary;
    UnityEngine.UI.Image selectedItemIcon;
    void BindV5Inventory()
    {
        for (int i = 0; i < 5; i++) wornSlots[i] = UIView.Require<UIEquipmentSlotView>(panel, "Equipment" + i);
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            wornSlots[5 + i] = UIView.Require<UIEquipmentSlotView>(panel, "WeaponSlot" + i);
            UIView.Button(panel, "WeaponSlot" + i, null, () => Select(progress.EquippedWeaponKey(index)));
        }
        itemStatTable = UIView.Require<UIStatTableView>(panel, "ItemStatTable");
        outfitStatTable = UIView.Require<UIStatTableView>(panel, "OutfitStatTable");
        replacementSummary = UIView.Require<TMPro.TMP_Text>(panel, "ReplacementSummary");
        selectedItemIcon = UIView.Require<UnityEngine.UI.Image>(panel, "SelectedItemIcon");
    }
    void RefreshV5Inventory()
    {
        if (wornSlots[0] == null) return;
        var data = progress.Inventory;
        for (int i = 0; i < 7; i++)
        {
            string key = i < 5 ? data.equipped[i] : progress.EquippedWeaponKey(i - 5);
            var item = progress.FindItem(key);
            int rank = progress.GetInstanceLevel(key);
            string state = i < 5 ? "当前生效" : player != null && player.Weapons.ActiveSlot == i - 5 ? "当前使用" : "备用";
            bool replacing = draft != null ? i < 5 ? draft.armorKeys[i] != key : draft.weaponKeys[i - 5] != key :
                TriedItem != null && (i < 5 ? TriedItem.Kind == ItemKind.Equipment && (int)TriedItem.Slot == i : TriedItem.Kind == ItemKind.Weapon && i == 5);
            wornSlots[i].Present(i < 5 ? ItemDefinition.SlotName((EquipmentSlot)i) : "武器槽 " + (i - 4),
                item == null ? "未穿戴" : item.DisplayName + (rank > 0 ? " +" + rank : ""),
                item?.Icon, item != null ? item.QualityColor : Color.gray, item == null ? "空槽" : state, replacing);
        }
        var chosen = progress.FindItem(selected);
        bool gear = chosen != null && !chosen.IsStackable;
        selectedItemIcon.sprite = chosen?.Icon; selectedItemIcon.gameObject.SetActive(chosen?.Icon != null);
        replacementSummary.gameObject.SetActive(gear);
        if (chosen?.Kind == ItemKind.Equipment)
        {
            string current = data.equipped[(int)chosen.Slot]; var item = progress.FindItem(current); int rank = progress.GetInstanceLevel(current);
            replacementSummary.text = current == selected ? "正在查看当前穿戴 · 属性已经生效" : "将替换：" + (item == null ? "空槽" : item.DisplayName + (rank > 0 ? " +" + rank : "")) + "\n试穿尚未生效，确认后才会更换。";
        }
        else if (chosen?.Kind == ItemKind.Weapon) replacementSummary.text = "选择放入武器槽 1 或 2；左侧始终显示实际持有的两把武器。";
        affixText.gameObject.SetActive(!string.IsNullOrEmpty(affixText.text)); gemText.gameObject.SetActive(!string.IsNullOrEmpty(gemText.text));
        bool trying = draft != null || TriedItem != null;
        itemStatTable.gameObject.SetActive(gear && !trying);
        comparison.gameObject.SetActive(!gear);
        var stats = Enum.GetValues(typeof(ItemAffixStat)).Cast<ItemAffixStat>().ToArray();
        if (gear)
        {
            var total = stats.Select(s => progress.ItemStat(selected, s)).ToArray();
            var upgrade = stats.Select(s => progress.ItemUpgradeBonus(selected, s) + progress.ItemGemBonus(selected, s)).ToArray();
            itemStatTable.Present("基础 / 词条", "强化 / 宝石", "合计", total.Zip(upgrade, (a,b) => a-b).ToArray(), upgrade, total, false);
        }
        outfitStatTable.gameObject.SetActive(trying);
        if (trying)
        {
            var before = stats.Select(s => progress.LoadoutStat(progress.CurrentLoadout, s)).ToArray();
            var delta = stats.Select(s => draft != null ? progress.LoadoutStat(draft, s) - progress.LoadoutStat(progress.CurrentLoadout, s) :
                TriedItem.Kind == ItemKind.Equipment ? progress.ArmorSwapDelta(tryOnItemId, s) :
                progress.ItemStat(tryOnItemId, s) - progress.ItemStat(progress.EquippedWeaponKey(player.Weapons.ActiveSlot), s)).ToArray();
            outfitStatTable.Present("当前穿戴", draft == null && TriedItem.Kind == ItemKind.Weapon ? "预览使用时" : "确认后", "变化", before, before.Zip(delta, (a,b) => a+b).ToArray(), delta, true);
        }
    }
}
