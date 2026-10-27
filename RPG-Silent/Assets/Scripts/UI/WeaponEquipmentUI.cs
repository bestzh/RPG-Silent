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
        if (weapons != null)
        {
            weapons.EquipmentChanged += Refresh;
            weapons.PresentationChanged += Refresh;
        }
        Refresh();
    }

    public void Unbind()
    {
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
        int current = -1;
        for (int i = 0; i < weapons.AvailableCount; i++)
            if (weapons.GetAvailableWeapon(i) == weapons.GetWeapon(slot)) { current = i; break; }
        var next = weapons.GetAvailableWeapon(current + 1);
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
            : !weapons.CombatAllowed ? "主城 · 武器已收起" : "战斗 · " + NameOf(weapons.ActiveWeapon);
        hintText.text = "Tab 切换武器 · Ctrl 显示鼠标";
        for (int i = 0; i < PlayerWeaponEquipment.SlotCount; i++)
        {
            slotTexts[i].text = (weapons != null && weapons.ActiveSlot == i ? "● " : "○ ") + (i + 1) + "  " + NameOf(weapons?.GetWeapon(i));
            changeButtons[i].interactable = weapons != null && !weapons.IsBusy;
            selectButtons[i].interactable = weapons != null && !weapons.IsBusy && weapons.ActiveSlot != i;
        }
    }

    private static string NameOf(WeaponDefinition weapon) => weapon != null ? weapon.DisplayName : "徒手";
    private void OnDestroy() => Unbind();
}
