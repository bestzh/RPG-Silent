using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIEquipmentSlotView : MonoBehaviour
{
    [SerializeField] private TMP_Text slot;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text state;
    [SerializeField] private Image icon;
    [SerializeField] private Image replacement;

    public void Present(string slotName, string equipmentName, Sprite equipmentIcon, Color quality, string stateText, bool willReplace)
    {
        slot.text = slotName;
        itemName.text = equipmentName;
        itemName.color = quality;
        state.text = stateText;
        icon.sprite = equipmentIcon;
        icon.enabled = equipmentIcon != null;
        replacement.enabled = willReplace;
    }
}
