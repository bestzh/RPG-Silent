using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Shared layout primitives for the character and town progression panels.</summary>
public static class ProgressionUIWidgets
{
    public static readonly Color Ink = new(0.075f, 0.10f, 0.14f, 0.98f);
    public static readonly Color Card = new(0.13f, 0.18f, 0.23f, 1f);
    public static readonly Color Accent = new(0.29f, 0.58f, 0.57f, 1f);
    public static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height); return r;
    }
    public static RectTransform Panel(Transform parent, string name, Color color)
    {
        var r = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        r.GetComponent<Image>().color = color; return r;
    }
    public static RectTransform CenterPanel(Transform parent, string name, float width, float height)
    {
        var r = Rect(parent, name, 0, 0, width, height); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.gameObject.AddComponent<Image>().color = Ink; return r;
    }
    public static TMP_Text Text(Transform parent, TMP_FontAsset font, string name, string text, float x, float y, float width, float height, float size = 22)
    {
        var t = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font; t.fontSize = size; t.text = text; t.color = new Color(.91f, .94f, .94f);
        t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; return t;
    }
    public static Button Button(Transform parent, TMP_FontAsset font, string name, string label, float x, float y, float width, float height, UnityAction action)
    {
        var r = Rect(parent, name, x, y, width, height);
        var image = r.gameObject.AddComponent<Image>(); image.color = Accent;
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = image; b.onClick.AddListener(action);
        var colors = b.colors; colors.disabledColor = new Color(.32f, .37f, .40f); b.colors = colors;
        var text = Text(r, font, "Label", label, 8, 2, width - 16, height - 4, 20); text.alignment = TextAlignmentOptions.Center;
        return b;
    }
}
