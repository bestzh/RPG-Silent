using TMPro;
using UnityEngine;
using W = ProgressionUIWidgets;

public partial class MainUI
{
    RectTransform equipmentEffectsPanel;
    TMP_Text equipmentEffectsFeedback, equipmentEffectsTimers;
    float nextEquipmentEffectsHud;
    void BuildEquipmentEffectsHud()
    {
        if (equipmentEffectsPanel != null || classNameText == null)
            return;
        equipmentEffectsPanel = UIView.Rect(UIView.Find(transform, "TopLeftPanel"), "EquipmentEffectsHud");
        equipmentEffectsFeedback = UIView.Text(equipmentEffectsPanel, "EquipmentEffectFeedback", "");
        equipmentEffectsFeedback.color = new Color(1f, .83f, .35f);
        equipmentEffectsTimers = UIView.Text(equipmentEffectsPanel, "EquipmentEffectTimers", "");
    }

    void RefreshEquipmentEffectsHud()
    {
        if (equipmentEffectsPanel == null || Time.unscaledTime < nextEquipmentEffectsHud)
            return;
        nextEquipmentEffectsHud = Time.unscaledTime + .1f;
        var effects = combatPlayer?.EquipmentEffects;
        string text = effects?.StatusText() ?? "";
        bool show = (_gameSettings?.CurrentSettings.ShowHud ?? true) && text.Length > 0;
        equipmentEffectsPanel.gameObject.SetActive(show);
        if (!show)
            return;
        string feedback = effects.Feedback;
        equipmentEffectsFeedback.text = feedback.Length > 0 ? feedback : "装备特效 · 同类取最高等级";
        equipmentEffectsTimers.text = text;
    }
}
