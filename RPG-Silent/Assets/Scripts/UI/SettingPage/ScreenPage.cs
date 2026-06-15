using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 设置页面 → 屏幕分页（Screen Tab）。
/// 负责：分辨率、全屏、画质、亮度的 UI 交互与设置应用。
///
/// 此组件挂在 SettingsUI Prefab 内部的 Right/Screen GameObject 上。
/// 当父级 SettingsUI 被 UIManager 加载时，VContainer 会递归注入此组件。
/// </summary>
public class ScreenPage : MonoBehaviour
{
    // ── VContainer 注入（方法注入，比字段注入更可靠）───────────────────────────
    private IScreenSettingsService _screenService;
    private bool _injected;

    [Inject]
    public void Construct(IScreenSettingsService screenService)
    {
        _screenService = screenService;
        _injected      = true;
    }

    // ── UI 控件（在 Inspector 中绑定，或由代码查找）────────────────────────────
    [Header("分辨率")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("全屏")]
    [SerializeField] private Toggle fullscreenToggle;

    [Header("画质")]
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("亮度")]
    [SerializeField] private Slider brightnessSlider;

    [Header("确认按钮")]
    [SerializeField] private Button okButton;
    
    [Header("重置按钮")]
    [SerializeField] private Button ResetButton;


    // 暂存用户本次修改的设置（点 OK 才正式 Apply+Save）
    private ScreenSettings _pendingSettings;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        okButton?.onClick.AddListener(OnOKClicked);
        ResetButton?.onClick.AddListener(OnResetClicked);
    }

    private void OnEnable()
    {
        if (!_injected) return;
        RefreshUI();
    }

    // ── 私有方法 ───────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (_screenService == null) return;

        // 克隆当前设置作为待确认值，用户操作控件只修改 _pendingSettings
        ScreenSettings cur = _screenService.CurrentSettings;
        _pendingSettings = new ScreenSettings
        {
            ResolutionIndex = cur.ResolutionIndex,
            IsFullScreen    = cur.IsFullScreen,
            QualityIndex    = cur.QualityIndex,
            Brightness      = cur.Brightness
        };

        SetupResolutionDropdown();
        SetupQualityDropdown();
        SetupFullscreenToggle();
        SetupBrightnessSlider();
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(
            new System.Collections.Generic.List<string>(_screenService.ResolutionOptions));
        resolutionDropdown.value = _pendingSettings.ResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(index =>
            _pendingSettings.ResolutionIndex = index);
    }

    private void SetupQualityDropdown()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(
            new System.Collections.Generic.List<string>(_screenService.QualityOptions));
        qualityDropdown.value = _pendingSettings.QualityIndex;
        qualityDropdown.RefreshShownValue();

        qualityDropdown.onValueChanged.RemoveAllListeners();
        qualityDropdown.onValueChanged.AddListener(index =>
            _pendingSettings.QualityIndex = index);
    }

    private void SetupFullscreenToggle()
    {
        if (fullscreenToggle == null) return;

        fullscreenToggle.isOn = _pendingSettings.IsFullScreen;

        fullscreenToggle.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.AddListener(value =>
            _pendingSettings.IsFullScreen = value);
    }

    private void SetupBrightnessSlider()
    {
        if (brightnessSlider == null) return;

        brightnessSlider.minValue = 0f;
        brightnessSlider.maxValue = 1f;
        brightnessSlider.value    = _pendingSettings.Brightness;

        brightnessSlider.onValueChanged.RemoveAllListeners();
        brightnessSlider.onValueChanged.AddListener(value =>
            _pendingSettings.Brightness = value);
    }

    private void OnOKClicked()
    {
        if (_screenService == null || _pendingSettings == null) return;

        _screenService.Apply(_pendingSettings);
        _screenService.Save();
        Debug.Log("[ScreenPage] 设置已应用并保存。");
    }

    private void OnResetClicked()
    {
        if (_screenService == null) return;
        _screenService.Reset();
        RefreshUI();
    }

    // ── 工具方法：从子节点路径查找组件 ──────────────────────────────────────────
    private T FindInChildren<T>(string path) where T : Component
    {
        Transform t = transform.Find(path);
        return t != null ? t.GetComponent<T>() : null;
    }
}
