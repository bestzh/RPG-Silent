using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 设置页面 → 游戏分页（Game Tab）。
/// 负责：游戏难度、显示 HUD、显示小地图、屏幕震动强度的 UI 交互与设置应用。
///
/// Prefab 节点映射（Right/Game）：
///   Resolution/Dropdown  → 难度
///   FullScreen/Toggle  → 显示 HUD
///   Quality/Dropdown   → 小地图（显示 / 隐藏）
///   Light/Slider       → 屏幕震动
///   OK                 → 应用
/// </summary>
public class GamePage : MonoBehaviour
{
    private IGameSettingsService _gameService;
    private bool _injected;
    [Inject]
    public void Construct(IGameSettingsService gameService)
    {
        _gameService = gameService;
        _injected = true;
        if (isActiveAndEnabled)
        {
            RefreshUI();
            _savedSnapshot = CloneSettings(_gameService.CurrentSettings);
        }
    }

    [Header("游戏难度")]
    [SerializeField]
    private TMP_Dropdown difficultyDropdown;
    [Header("显示 HUD")]
    [SerializeField]
    private Toggle showHudToggle;
    [Header("小地图")]
    [SerializeField]
    private TMP_Dropdown showMiniMapDropdown;
    [Header("屏幕震动")]
    [SerializeField]
    private Slider screenShakeSlider;
    [SerializeField]
    private TMP_Text screenShakeLabel;
    [Header("确认 / 重置按钮")]
    [SerializeField]
    private Button okButton;
    [SerializeField]
    private Button resetButton;
    private static readonly string[] MiniMapOptions =
    {
        "显示",
        "隐藏"
    };
    private GameSettings _pendingSettings;
    private GameSettings _savedSnapshot;
    private Toggle damageNumbersToggle;
    private Slider feedbackSlider;
    private TMP_Text feedbackLabel;
    private void Awake()
    {
        TryAutoBind();
        BuildComfortOptions();
        okButton?.onClick.AddListener(OnOKClicked);
        resetButton?.onClick.AddListener(OnResetClicked);
    }

    private void OnEnable()
    {
        if (!_injected)
            return;
        RefreshUI();
        _savedSnapshot = CloneSettings(_gameService.CurrentSettings);
    }

    private void OnDisable()
    {
        if (_gameService == null || _savedSnapshot == null)
            return;
        _gameService.Apply(_savedSnapshot);
    }

    private void TryAutoBind()
    {
        difficultyDropdown ??= UIView.Find(transform, "Resolution/Dropdown")?.GetComponent<TMP_Dropdown>();
        showHudToggle ??= UIView.Find(transform, "FullScreen/Toggle")?.GetComponent<Toggle>();
        showMiniMapDropdown ??= UIView.Find(transform, "Quality/Dropdown")?.GetComponent<TMP_Dropdown>();
        screenShakeSlider ??= UIView.Find(transform, "Light/Slider")?.GetComponent<Slider>();
        okButton ??= UIView.Find(transform, "OK")?.GetComponent<Button>();
        resetButton ??= UIView.Find(transform, "Reset")?.GetComponent<Button>();
    }

    private void RefreshUI()
    {
        if (_gameService == null)
            return;
        _pendingSettings = CloneSettings(_gameService.CurrentSettings);
        SetupDifficultyDropdown();
        SetupShowHudToggle();
        SetupMiniMapDropdown();
        SetupScreenShakeSlider();
        SetupComfortOptions();
    }

    private Transform Row(Component control)
    {
        if (control == null)
            return null;
        var row = control.transform;
        while (row.parent != transform)
            row = row.parent;
        return row;
    }

    private void BuildComfortOptions()
    {
        var damageRow = UIView.Find(transform, "DamageNumbers");
        damageNumbersToggle = damageRow.GetComponentInChildren<Toggle>(true);
        var feedbackRow = UIView.Find(transform, "FeedbackIntensity");
        feedbackSlider = feedbackRow.GetComponentInChildren<Slider>(true);
        feedbackLabel = feedbackRow.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => screenShakeLabel != null && t.name == screenShakeLabel.name);
    }

    private void SetupComfortOptions()
    {
        if (damageNumbersToggle != null)
        {
            damageNumbersToggle.SetIsOnWithoutNotify(_pendingSettings.ShowDamageNumbers);
            damageNumbersToggle.onValueChanged.RemoveAllListeners();
            damageNumbersToggle.onValueChanged.AddListener(value =>
            {
                _pendingSettings.ShowDamageNumbers = value;
                PreviewPendingSettings();
            });
        }

        if (feedbackSlider == null)
            return;
        feedbackSlider.minValue = 0;
        feedbackSlider.maxValue = 1;
        feedbackSlider.SetValueWithoutNotify(_pendingSettings.FeedbackIntensity);
        UpdatePercentLabel(feedbackLabel, _pendingSettings.FeedbackIntensity);
        feedbackSlider.onValueChanged.RemoveAllListeners();
        feedbackSlider.onValueChanged.AddListener(value =>
        {
            _pendingSettings.FeedbackIntensity = value;
            UpdatePercentLabel(feedbackLabel, value);
            PreviewPendingSettings();
        });
    }

    private void SetupDifficultyDropdown()
    {
        if (difficultyDropdown == null)
            return;
        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(new List<string>(_gameService.DifficultyOptions));
        difficultyDropdown.SetValueWithoutNotify(_pendingSettings.DifficultyIndex);
        difficultyDropdown.RefreshShownValue();
        difficultyDropdown.onValueChanged.RemoveAllListeners();
        difficultyDropdown.onValueChanged.AddListener(index =>
        {
            _pendingSettings.DifficultyIndex = index;
            PreviewPendingSettings();
        });
    }

    private void SetupShowHudToggle()
    {
        if (showHudToggle == null)
            return;
        showHudToggle.SetIsOnWithoutNotify(_pendingSettings.ShowHud);
        showHudToggle.onValueChanged.RemoveAllListeners();
        showHudToggle.onValueChanged.AddListener(value =>
        {
            _pendingSettings.ShowHud = value;
            PreviewPendingSettings();
        });
    }

    private void SetupMiniMapDropdown()
    {
        if (showMiniMapDropdown == null)
            return;
        showMiniMapDropdown.ClearOptions();
        showMiniMapDropdown.AddOptions(new List<string>(MiniMapOptions));
        showMiniMapDropdown.SetValueWithoutNotify(_pendingSettings.ShowMiniMap ? 0 : 1);
        showMiniMapDropdown.RefreshShownValue();
        showMiniMapDropdown.onValueChanged.RemoveAllListeners();
        showMiniMapDropdown.onValueChanged.AddListener(index =>
        {
            _pendingSettings.ShowMiniMap = index == 0;
            PreviewPendingSettings();
        });
    }

    private void SetupScreenShakeSlider()
    {
        if (screenShakeSlider == null)
            return;
        screenShakeSlider.minValue = 0f;
        screenShakeSlider.maxValue = 1f;
        screenShakeSlider.SetValueWithoutNotify(_pendingSettings.ScreenShakeIntensity);
        UpdatePercentLabel(screenShakeLabel, _pendingSettings.ScreenShakeIntensity);
        screenShakeSlider.onValueChanged.RemoveAllListeners();
        screenShakeSlider.onValueChanged.AddListener(value =>
        {
            _pendingSettings.ScreenShakeIntensity = value;
            UpdatePercentLabel(screenShakeLabel, value);
            PreviewPendingSettings();
        });
    }

    private void PreviewPendingSettings()
    {
        if (_gameService == null || _pendingSettings == null)
            return;
        _gameService.Apply(_pendingSettings);
    }

    private void OnOKClicked()
    {
        if (_gameService == null || _pendingSettings == null)
            return;
        _gameService.Apply(_pendingSettings);
        _gameService.Save();
        _savedSnapshot = CloneSettings(_pendingSettings);
        Debug.Log("[GamePage] 设置已应用并保存。");
    }

    private void OnResetClicked()
    {
        if (_gameService == null)
            return;
        _gameService.Apply(_gameService.Defaults);
        RefreshUI();
    }

    private static GameSettings CloneSettings(GameSettings source) => new GameSettings
    {
        DifficultyIndex = source.DifficultyIndex,
        ShowHud = source.ShowHud,
        ShowMiniMap = source.ShowMiniMap,
        ScreenShakeIntensity = source.ScreenShakeIntensity,
        ShowDamageNumbers = source.ShowDamageNumbers,
        FeedbackIntensity = source.FeedbackIntensity
    };
    private static void UpdatePercentLabel(TMP_Text label, float value)
    {
        if (label == null)
            return;
        label.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
