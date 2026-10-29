using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class InventoryUI
{
    Button equipmentEffectButton;
    RectTransform equipmentEffectShade, equipmentEffectWindow;
    TMP_Text equipmentEffectTitle, equipmentEffectBody, equipmentEffectCurrent;
    string equipmentEffectKey;
    float nextEquipmentEffectRefresh;
    void BuildEquipmentEffectDetails()
    {
        equipmentEffectButton = UIView.Button(panel, "EquipmentEffectDetails", "特殊效果 · 规则", OpenEquipmentEffectDetails);
        equipmentEffectShade = UIView.Panel(transform, "EquipmentEffectShade");
        equipmentEffectWindow = UIView.Panel(equipmentEffectShade, "EquipmentEffectWindow");
        equipmentEffectTitle = UIView.Text(equipmentEffectWindow, "EquipmentEffectTitle", "");
        UIView.Button(equipmentEffectWindow, "CloseEquipmentEffect", "返回 [ESC]", CloseEquipmentEffectDetails);
        equipmentEffectBody = UIView.Text(equipmentEffectWindow, "EquipmentEffectRules", "");
        equipmentEffectCurrent = UIView.Text(equipmentEffectWindow, "EquipmentEffectCurrent", "");
        UIView.Text(equipmentEffectWindow, "EquipmentEffectHelp", "装备固有效果，保留原随机词条、强化和宝石。试穿不提供战斗效果。");
        equipmentEffectShade.gameObject.SetActive(false);
    }

    void RefreshEquipmentEffectDetails(ItemDefinition item)
    {
        if (equipmentEffectButton == null)
            return;
        int level = EquipmentEffectRules.Level(item);
        equipmentEffectButton.gameObject.SetActive(level > 0);
        if (level > 0)
            equipmentEffectButton.GetComponentInChildren<TMP_Text>().text = EquipmentEffectRules.Name(item.SpecialEffect) + (level == 2 ? " II" : " I") + " · 规则";
        if (equipmentEffectShade.gameObject.activeSelf && (level == 0 || equipmentEffectKey != selected))
            CloseEquipmentEffectDetails();
    }

    void OpenEquipmentEffectDetails()
    {
        if (EquipmentEffectRules.Level(progress.FindItem(selected)) == 0)
            return;
        equipmentEffectKey = selected;
        equipmentEffectShade.gameObject.SetActive(true);
        equipmentEffectShade.SetAsLastSibling();
        nextEquipmentEffectRefresh = 0;
        UpdateEquipmentEffectDetails();
    }

    void CloseEquipmentEffectDetails()
    {
        equipmentEffectKey = null;
        if (equipmentEffectShade != null)
            equipmentEffectShade.gameObject.SetActive(false);
    }

    void UpdateEquipmentEffectDetails()
    {
        if (equipmentEffectShade == null || !equipmentEffectShade.gameObject.activeSelf || Time.unscaledTime < nextEquipmentEffectRefresh)
            return;
        nextEquipmentEffectRefresh = Time.unscaledTime + .1f;
        var item = progress.FindItem(equipmentEffectKey);
        int level = EquipmentEffectRules.Level(item);
        if (level == 0 || selected != equipmentEffectKey)
        {
            CloseEquipmentEffectDetails();
            return;
        }

        equipmentEffectTitle.text = item.DisplayName + "\n" + EquipmentEffectRules.Name(item.SpecialEffect) + (level == 2 ? " II · 神话" : " I · 传说");
        equipmentEffectTitle.color = item.QualityColor;
        equipmentEffectBody.text = EquipmentEffectRules.Description(item.SpecialEffect, level);
        int active = progress.EquipmentEffectLevel(item.SpecialEffect);
        float cooldown = player?.EquipmentEffects?.RemainingCooldown(item.SpecialEffect) ?? 0;
        equipmentEffectCurrent.text = progress.EquipmentEffectPosition(equipmentEffectKey) + "\n同类当前取最高：" + (active == 0 ? "未激活" : active == 2 ? "II" : "I") + $" · 冷却 {cooldown:0.0} 秒";
    }
}
