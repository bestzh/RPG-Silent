using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD equipment controls; Ctrl releases the cursor, Tab selects the other slot.</summary>
public sealed class WeaponEquipmentUI : MonoBehaviour
{
    public TMP_Text statusText;
    public TMP_Text hintText;
    public TMP_Text[] slotTexts;
    public Button[] changeButtons;
    public Button[] selectButtons;
    private PlayerWeaponEquipment weapons;
    [VContainer.Inject] private PlayerProgressService progress;

    private void Awake()
    {
        for (int i = 0; i < PlayerWeaponEquipment.SlotCount; i++)
        {
            int slot = i;
            changeButtons[i].onClick.AddListener(() => CycleEquipment(slot));
            selectButtons[i].onClick.AddListener(() => SelectSlot(slot));
        }
    }

    public void Bind(PlayerWeaponEquipment source)
    {
        Unbind();
        weapons = source;
        if (progress != null) progress.Changed += Refresh;
        if (weapons != null)
        {
            weapons.EquipmentChanged += Refresh;
            weapons.PresentationChanged += Refresh;
        }
        Refresh();
    }

    public void Unbind()
    {
        if (progress != null) progress.Changed -= Refresh;
        if (weapons != null)
        {
            weapons.EquipmentChanged -= Refresh;
            weapons.PresentationChanged -= Refresh;
        }
        weapons = null;
    }

    public void CycleEquipment(int slot)
    {
        if (weapons == null) return;
        if (progress != null)
        {
            var candidates = progress.Inventory.bag.Where(x => progress.FindItem(x.itemId)?.Kind == ItemKind.Weapon && progress.CanEquipWeaponInstance(x.Key)).Select(x => x.Key).ToArray();
            int index = System.Array.IndexOf(candidates, weapons.GetInstanceId(slot));
            string key = index + 1 < candidates.Length ? candidates[index + 1] : string.Empty;
            if (!weapons.TryEquipInstance(slot, key)) ShowBlocked();
            return;
        }
        int current = -1;
        for (int i = 0; i < weapons.AvailableCount; i++)
            if (weapons.GetAvailableWeapon(i) == weapons.GetWeapon(slot)) { current = i; break; }
        WeaponDefinition next = null;
        for (int i = current + 1; i < weapons.AvailableCount; i++)
            if (weapons.IsAvailable(weapons.GetAvailableWeapon(i))) { next = weapons.GetAvailableWeapon(i); break; }
        if (!weapons.TryEquip(slot, next)) ShowBlocked();
    }

    public void SelectSlot(int slot)
    {
        if (weapons != null && !weapons.TrySelectSlot(slot)) ShowBlocked();
    }

    private void ShowBlocked() => hintText.text = "请在落地、动作结束后更换武器";

    private void Refresh()
    {
        statusText.text = weapons == null ? "武器装备"
            : weapons.Phase == WeaponTransitionPhase.Sheathing ? "收剑中…"
            : weapons.Phase == WeaponTransitionPhase.Drawing ? "拔剑中…"
            : weapons.IsBusy ? "准备切换姿态…"
            : !weapons.CombatAllowed ? "主城 · 武器已收起" : "战斗 · " + NameOf(weapons.ActiveWeapon, weapons.ActiveSlot);
        hintText.text = "Tab 切换武器 · Ctrl 显示鼠标";
        for (int i = 0; i < PlayerWeaponEquipment.SlotCount; i++)
        {
            slotTexts[i].text = (weapons != null && weapons.ActiveSlot == i ? "● " : "○ ") + (i + 1) + "  " + NameOf(weapons?.GetWeapon(i), i);
            changeButtons[i].interactable = weapons != null && !weapons.IsBusy;
            selectButtons[i].interactable = weapons != null && !weapons.IsBusy && weapons.ActiveSlot != i;
        }
    }

    private string NameOf(WeaponDefinition weapon, int slot)
    {
        if (weapon == null) return "徒手";
        int level = progress != null ? progress.GetInstanceLevel(weapons.GetInstanceId(slot)) : 0;
        string name = weapon.DisplayName + (level > 0 ? " +" + level : "");
        var item = progress?.FindItem(weapon.PersistentId);
        return item != null ? $"<color=#{ColorUtility.ToHtmlStringRGB(item.QualityColor)}>{name}</color>" : name;
    }
    private void OnDestroy() => Unbind();
}
