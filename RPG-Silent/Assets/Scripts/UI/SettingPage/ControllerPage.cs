using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 设置页面 → 控制分页（Controller Tab）。
/// 负责：鼠标灵敏度、冲刺持定时间、反转Y轴的 UI 交互，以及按键说明的只读展示。
/// </summary>
public class ControllerPage : MonoBehaviour
{
    private IControllerSettingsService _controllerService;
    private bool _injected;

    [Inject]
    public void Construct(IControllerSettingsService controllerService)
    {
        _controllerService = controllerService;
        _injected          = true;
    }

    [Header("鼠标灵敏度")]
    [SerializeField] private Slider   mouseSensitivitySlider;
    [SerializeField] private TMP_Text mouseSensitivityLabel;

    [Header("冲刺持定时间")]
    [SerializeField] private Slider   sprintTimeSlider;
    [SerializeField] private TMP_Text sprintTimeLabel;

    [Header("反转Y轴")]
    [SerializeField] private Toggle invertYToggle;

    [Header("按键说明 - 值标签")]
    [SerializeField] private TMP_Text forwardKeyLabel;
    [SerializeField] private TMP_Text backwardKeyLabel;
    [SerializeField] private TMP_Text toLeftKeyLabel;
    [SerializeField] private TMP_Text toRightKeyLabel;
    [SerializeField] private TMP_Text attackKeyLabel;
    [SerializeField] private TMP_Text sprintKeyLabel;
    [SerializeField] private TMP_Text rollKeyLabel;
    [SerializeField] private TMP_Text jumpKeyLabel;
    [SerializeField] private TMP_Text walkKeyLabel;

    [Header("确认按钮")]
    [SerializeField] private Button okButton;

    [Header("重置按钮")]
    [SerializeField] private Button resetButton;

    private ControllerSettings _pendingSettings;
    private ControllerSettings _savedSnapshot;

    private void Awake()
    {
        okButton?.onClick.AddListener(OnOKClicked);
        resetButton?.onClick.AddListener(OnResetClicked);
    }

    private void OnEnable()
    {
        if (!_injected) return;
        RefreshUI();
        _savedSnapshot = CloneSettings(_controllerService.CurrentSettings);
    }

    private void OnDisable()
    {
        // 未点「应用」就离开页面时，恢复进入页面前的设置
        if (_controllerService == null || _savedSnapshot == null) return;
        _controllerService.Apply(_savedSnapshot);
    }

    private void RefreshUI()
    {
        if (_controllerService == null) return;

        _pendingSettings = CloneSettings(_controllerService.CurrentSettings);

        SetupMouseSensitivitySlider();
        SetupSprintTimeSlider();
        SetupInvertYToggle();
        RefreshKeyLabels();
    }

    private void SetupMouseSensitivitySlider()
    {
        if (mouseSensitivitySlider == null) return;

        mouseSensitivitySlider.minValue = 0.1f;
        mouseSensitivitySlider.maxValue = 10f;
        mouseSensitivitySlider.SetValueWithoutNotify(_pendingSettings.MouseSensitivity);
        UpdateFloatLabel(mouseSensitivityLabel, _pendingSettings.MouseSensitivity, "F1");

        mouseSensitivitySlider.onValueChanged.RemoveAllListeners();
        mouseSensitivitySlider.onValueChanged.AddListener(v =>
        {
            _pendingSettings.MouseSensitivity = v;
            UpdateFloatLabel(mouseSensitivityLabel, v, "F1");
            PreviewPendingSettings();
        });
    }

    private void SetupSprintTimeSlider()
    {
        if (sprintTimeSlider == null) return;

        sprintTimeSlider.minValue = 0.05f;
        sprintTimeSlider.maxValue = 2f;
        sprintTimeSlider.SetValueWithoutNotify(_pendingSettings.SprintHoldTime);
        UpdateFloatLabel(sprintTimeLabel, _pendingSettings.SprintHoldTime, "F2");

        sprintTimeSlider.onValueChanged.RemoveAllListeners();
        sprintTimeSlider.onValueChanged.AddListener(v =>
        {
            _pendingSettings.SprintHoldTime = v;
            UpdateFloatLabel(sprintTimeLabel, v, "F2");
            PreviewPendingSettings();
        });
    }

    private void SetupInvertYToggle()
    {
        if (invertYToggle == null) return;

        invertYToggle.SetIsOnWithoutNotify(_pendingSettings.InvertY);

        invertYToggle.onValueChanged.RemoveAllListeners();
        invertYToggle.onValueChanged.AddListener(v =>
        {
            _pendingSettings.InvertY = v;
            PreviewPendingSettings();
        });
    }

    private void PreviewPendingSettings()
    {
        if (_controllerService == null || _pendingSettings == null) return;
        _controllerService.Apply(_pendingSettings);
    }

    /// <summary>
    /// 展示当前硬编码的按键绑定（只读，不支持运行时重绑定）。
    /// 移动键使用 Legacy Input Manager（WASD），动作键使用 New Input System。
    /// </summary>
    private void RefreshKeyLabels()
    {
        SetText(forwardKeyLabel,  "W");
        SetText(backwardKeyLabel, "S");
        SetText(toLeftKeyLabel,   "A");
        SetText(toRightKeyLabel,  "D");
        SetText(attackKeyLabel,   "鼠标左键");
        SetText(sprintKeyLabel,   "Left Shift");
        SetText(rollKeyLabel,     "Left Shift (短按)");
        SetText(jumpKeyLabel,     "Space");
        SetText(walkKeyLabel,     "Left Alt");
    }

    private void OnOKClicked()
    {
        if (_controllerService == null || _pendingSettings == null) return;

        _controllerService.Apply(_pendingSettings);
        _controllerService.Save();
        _savedSnapshot = CloneSettings(_pendingSettings);
        Debug.Log("[ControllerPage] 设置已应用并保存。");
    }

    private void OnResetClicked()
    {
        if (_controllerService == null) return;
        _controllerService.Reset();
        RefreshUI();
        _savedSnapshot = CloneSettings(_controllerService.CurrentSettings);
    }

    private static ControllerSettings CloneSettings(ControllerSettings source)
    {
        return new ControllerSettings
        {
            MouseSensitivity = source.MouseSensitivity,
            SprintHoldTime   = source.SprintHoldTime,
            InvertY          = source.InvertY
        };
    }

    private static void UpdateFloatLabel(TMP_Text label, float value, string format)
    {
        if (label != null) label.text = value.ToString(format);
    }

    private static void SetText(TMP_Text label, string text)
    {
        if (label != null) label.text = text;
    }
}
