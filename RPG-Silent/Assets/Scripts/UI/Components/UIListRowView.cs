using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>A reusable data row; native layout components in its prefab own all geometry.</summary>
public sealed class UIListRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Button selection;
    [SerializeField] private GameObject actionGroup;
    [SerializeField] private Button[] actions;
    [SerializeField] private TMP_Text[] actionLabels;
    public TMP_Text Title => title;
    public TMP_Text Description => description;

    public void Present(string heading, string body = "", Sprite sprite = null)
    {
        title.text = heading;
        description.text = body;
        description.gameObject.SetActive(!string.IsNullOrEmpty(body));
        icon.sprite = sprite;
        icon.gameObject.SetActive(sprite != null);
        background.color = ProgressionUIWidgets.Card;
        title.color = new Color(.88f, .92f, .86f);
        description.color = new Color(.66f, .75f, .71f);
        selection.onClick.RemoveAllListeners();
        selection.interactable = false;
        actionGroup.SetActive(false);
        for (int i = 0; i < actions.Length; i++)
        {
            actions[i].onClick.RemoveAllListeners();
            actions[i].gameObject.SetActive(false);
        }
    }

    public Button Action(int index, string label, UnityAction action, bool enabled = true)
    {
        var button = actions[index];
        actionGroup.SetActive(true);
        button.gameObject.SetActive(true);
        actionLabels[index].text = label;
        button.interactable = enabled;
        button.onClick.RemoveAllListeners();
        if (action != null) button.onClick.AddListener(action);
        return button;
    }

    public void SetSelected(bool selected) => background.color = selected ? ProgressionUIWidgets.Accent : ProgressionUIWidgets.Card;
    public void Select(UnityAction action) { selection.interactable = true; selection.onClick.AddListener(action); }
}
