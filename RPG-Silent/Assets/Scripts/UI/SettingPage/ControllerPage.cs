using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 设置页面 → 控制分页（Controller Tab）。
/// 负责：鼠标灵敏度、冲刺持定时间、反转Y轴，以及所有动作按键的运行时改键。
///
/// Inspector 绑定约定：
///   xxxKeyButton  → 对应动作行中可点击区域的 Button 组件
///                   Button 子对象须含一个 TMP_Text 用于显示当前键名
/// </summary>
public class ControllerPage : MonoBehaviour
{
    // ── VContainer 注入 ────────────────────────────────────────────────────────
    private IControllerSettingsService _controllerService;
    private IPlayerInputActions        _playerInputActions;
    private bool _injected;

    [Inject]
    public void Construct(IControllerSettingsService controllerService,
                          IObjectResolver            resolver)
    {
        _controllerService = controllerService;
        _injected          = true;

        // IPlayerInputActions 为可选服务：未注册时改键功能不可用，参数恢复仍正常
        if (!resolver.TryResolve<IPlayerInputActions>(out var playerInputActions))
            Debug.LogWarning("[ControllerPage] IPlayerInputActions 未注册，改键功能不可用。");
        _playerInputActions = playerInputActions;
    }

    // ── 灵敏度 / 冲刺时间 / 反转Y ─────────────────────────────────────────────
    [Header("鼠标灵敏度")]
    [SerializeField] private Slider   mouseSensitivitySlider;
    [SerializeField] private TMP_Text mouseSensitivityLabel;

    [Header("冲刺持定时间")]
    [SerializeField] private Slider   sprintTimeSlider;
    [SerializeField] private TMP_Text sprintTimeLabel;

    [Header("反转Y轴")]
    [SerializeField] private Toggle invertYToggle;

    // ── 按键改键按钮（Button 子对象须有 TMP_Text 显示当前键名）────────────────
    [Header("按键改键按钮")]
    [SerializeField] private Button forwardKeyButton;
    [SerializeField] private Button backwardKeyButton;
    [SerializeField] private Button toLeftKeyButton;
    [SerializeField] private Button toRightKeyButton;
    [SerializeField] private Button attackKeyButton;
    [SerializeField] private Button sprintKeyButton;
    [SerializeField] private Button rollKeyButton;
    [SerializeField] private Button jumpKeyButton;
    [SerializeField] private Button walkKeyButton;

    [Header("确认 / 重置按钮")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button resetButton;

    // ── 内部状态 ──────────────────────────────────────────────────────────────
    private ControllerSettings _pendingSettings;
    private ControllerSettings _savedSnapshot;
    private bool _isRebinding;

    // ── 绑定描述（供 StartRebind 使用）──────────────────────────────────────────
    private struct RebindInfo
    {
        public Button     button;
        public InputAction action;
        public int        bindingIndex;
    }

    // ──────────────────────────────────────────────────────────────────────────

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
        _playerInputActions?.CancelCurrentRebind();

        // 未点「应用」就离开：恢复进入页面前的控制器参数
        if (_controllerService != null && _savedSnapshot != null)
            _controllerService.Apply(_savedSnapshot);

        // 未点「应用」就离开：从 PlayerPrefs 重新加载按键绑定，丢弃本次未保存的改键
        _playerInputActions?.Load();
        RefreshAllKeyLabels();
    }

    // ── UI 初始化 ──────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (_controllerService == null) return;

        _pendingSettings = CloneSettings(_controllerService.CurrentSettings);

        SetupMouseSensitivitySlider();
        SetupSprintTimeSlider();
        SetupInvertYToggle();
        SetupRebindButtons();
        RefreshAllKeyLabels();
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
            _controllerService.Apply(_pendingSettings);
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
            _controllerService.Apply(_pendingSettings);
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
            _controllerService.Apply(_pendingSettings);
        });
    }

    // ── 改键按钮配置 ───────────────────────────────────────────────────────────

    private void SetupRebindButtons()
    {
        if (_playerInputActions == null) return;

        // Move 复合体：索引 1=前进 2=后退 3=左移 4=右移
        BindRebindButton(forwardKeyButton,  _playerInputActions.Move,   1);
        BindRebindButton(backwardKeyButton, _playerInputActions.Move,   2);
        BindRebindButton(toLeftKeyButton,   _playerInputActions.Move,   3);
        BindRebindButton(toRightKeyButton,  _playerInputActions.Move,   4);

        // 离散动作（索引 0）
        BindRebindButton(attackKeyButton, _playerInputActions.Attack,       0);
        BindRebindButton(sprintKeyButton, _playerInputActions.Sprint,       0);
        BindRebindButton(rollKeyButton,   _playerInputActions.Roll,         0);
        BindRebindButton(jumpKeyButton,   _playerInputActions.Jump,         0);
        BindRebindButton(walkKeyButton,   _playerInputActions.Walk,         0);
    }

    private void BindRebindButton(Button button, InputAction action, int bindingIndex)
    {
        if (button == null || action == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => StartRebind(button, action, bindingIndex));
    }

    // ── 改键流程 ───────────────────────────────────────────────────────────────

    private void StartRebind(Button button, InputAction action, int bindingIndex)
    {
        if (_isRebinding) return;
        _isRebinding = true;

        SetAllRebindButtonsInteractable(false);

        var label = button.GetComponentInChildren<TMP_Text>();
        string originalText = label != null ? label.text : string.Empty;
        if (label != null) label.text = "...";

        _playerInputActions.StartRebind(
            action,
            bindingIndex,
            onComplete: newDisplay =>
            {
                if (label != null) label.text = newDisplay;

                // Roll 镜像 Sprint：完成后同步刷新 Roll 按键标签
                if (action == _playerInputActions.Sprint)
                    RefreshKeyLabel(rollKeyButton, _playerInputActions.Roll, 0);

                // 不在此处保存，等用户点「应用」后才写入 PlayerPrefs
                _isRebinding = false;
                SetAllRebindButtonsInteractable(true);
            },
            onCancel: () =>
            {
                if (label != null) label.text = originalText;
                _isRebinding = false;
                SetAllRebindButtonsInteractable(true);
            });
    }

    private void SetAllRebindButtonsInteractable(bool interactable)
    {
        Button[] all = {
            forwardKeyButton, backwardKeyButton, toLeftKeyButton, toRightKeyButton,
            attackKeyButton, sprintKeyButton, rollKeyButton, jumpKeyButton, walkKeyButton
        };
        foreach (var b in all)
            if (b != null) b.interactable = interactable;
    }

    // ── 键名标签刷新 ───────────────────────────────────────────────────────────

    private void RefreshAllKeyLabels()
    {
        if (_playerInputActions == null) return;

        RefreshKeyLabel(forwardKeyButton,  _playerInputActions.Move,   1);
        RefreshKeyLabel(backwardKeyButton, _playerInputActions.Move,   2);
        RefreshKeyLabel(toLeftKeyButton,   _playerInputActions.Move,   3);
        RefreshKeyLabel(toRightKeyButton,  _playerInputActions.Move,   4);
        RefreshKeyLabel(attackKeyButton,   _playerInputActions.Attack, 0);
        RefreshKeyLabel(sprintKeyButton,   _playerInputActions.Sprint, 0);
        RefreshKeyLabel(rollKeyButton,     _playerInputActions.Roll,   0);
        RefreshKeyLabel(jumpKeyButton,     _playerInputActions.Jump,   0);
        RefreshKeyLabel(walkKeyButton,     _playerInputActions.Walk,   0);
    }

    private void RefreshKeyLabel(Button button, InputAction action, int bindingIndex)
    {
        if (button == null || action == null) return;
        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = _playerInputActions.GetDisplayString(action, bindingIndex);
    }

    // ── 应用 / 重置 ────────────────────────────────────────────────────────────

    private void OnOKClicked()
    {
        if (_controllerService == null || _pendingSettings == null) return;

        // 保存控制器参数
        _controllerService.Apply(_pendingSettings);
        _controllerService.Save();
        _savedSnapshot = CloneSettings(_pendingSettings);

        // 保存按键绑定
        _playerInputActions?.Save();

        Debug.Log("[ControllerPage] 设置已应用并保存。");
    }

    private void OnResetClicked()
    {
        if (_controllerService == null) return;

        // 重置控制器参数（灵敏度、冲刺时间、反转Y），不立即保存
        _controllerService.Reset();

        // 重置按键绑定到默认，不立即保存（等用户点「应用」）
        _playerInputActions?.ResetBindings();

        RefreshUI();
        _savedSnapshot = CloneSettings(_controllerService.CurrentSettings);
    }

    // ── 工具方法 ───────────────────────────────────────────────────────────────

    private static ControllerSettings CloneSettings(ControllerSettings src) =>
        new ControllerSettings
        {
            MouseSensitivity = src.MouseSensitivity,
            SprintHoldTime   = src.SprintHoldTime,
            InvertY          = src.InvertY
        };

    private static void UpdateFloatLabel(TMP_Text label, float value, string fmt)
    {
        if (label != null) label.text = value.ToString(fmt);
    }
}
