using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个技能槽 UI：图标、热键、冷却遮罩。
/// 未装配时保留槽位外观并降低透明度，方便玩家看出空位。
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    public Image           icon;
    public Image           cooldownMask;
    public Image           background;
    public TextMeshProUGUI hotkeyText;

    [Header("Empty Slot Look")]
    public Color emptyIconColor = new Color(1f, 1f, 1f, 0.2f);
    public Color emptyBackgroundColor = new Color(1f, 1f, 1f, 0.45f);
    public float emptySlotAlpha = 0.7f;

    private CanvasGroup _canvasGroup;
    private Color _defaultIconColor = Color.white;
    private Color _defaultBackgroundColor = Color.white;
    private bool _colorsCached;

    private void Awake()
    {
        CacheRefs();
    }

    private void CacheRefs()
    {
        if (icon == null)
            icon = transform.Find("Icon")?.GetComponent<Image>();

        if (cooldownMask == null)
            cooldownMask = transform.Find("CooldownMask")?.GetComponent<Image>();

        if (background == null)
            background = transform.Find("bg")?.GetComponent<Image>();

        if (hotkeyText == null)
            hotkeyText = transform.Find("HotkeyText")?.GetComponent<TextMeshProUGUI>();

        if (cooldownMask != null)
        {
            cooldownMask.type       = Image.Type.Filled;
            cooldownMask.fillMethod = Image.FillMethod.Radial360;
            cooldownMask.fillAmount = 0f;
        }

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (!_colorsCached)
        {
            if (icon != null) _defaultIconColor = icon.color;
            if (background != null) _defaultBackgroundColor = background.color;
            _colorsCached = true;
        }
    }

    public void Bind(SkillData skill, string hotkey)
    {
        CacheRefs();

        if (hotkeyText != null)
            hotkeyText.text = string.IsNullOrEmpty(hotkey) ? string.Empty : hotkey;

        bool equipped = skill != null;

        if (icon != null)
        {
            if (equipped && skill.Icon != null)
            {
                icon.sprite  = skill.Icon;
                icon.enabled = true;
                icon.color   = _defaultIconColor;
            }
            else if (equipped)
            {
                // 有技能但没配 Icon：保留预制体上的图
                icon.enabled = true;
                icon.color   = _defaultIconColor;
            }
            else
            {
                // 空槽：保留占位图，但明显变淡
                icon.enabled = true;
                icon.color   = emptyIconColor;
            }
        }

        if (background != null)
            background.color = equipped ? _defaultBackgroundColor : emptyBackgroundColor;

        if (_canvasGroup != null)
            _canvasGroup.alpha = equipped ? 1f : emptySlotAlpha;

        if (cooldownMask != null && !equipped)
            cooldownMask.fillAmount = 0f;
    }

    public void SetCooldownNormalized(float normalized)
    {
        CacheRefs();
        if (cooldownMask == null) return;
        cooldownMask.fillAmount = Mathf.Clamp01(normalized);
    }
}
