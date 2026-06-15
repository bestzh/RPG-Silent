using System.Collections.Generic;
using RPGSilent.Domain;
using UnityEngine;

/// <summary>
/// 屏幕设置服务，实现 IScreenSettingsService。
/// 封装 Unity 的 Screen、QualitySettings 和 PlayerPrefs API。
/// 由 GameLifetimeScope 注册为全局单例。
/// </summary>
public class ScreenSettingsService : MonoBehaviour, IScreenSettingsService
{
    // PlayerPrefs 键名
    private const string KeyResolution = "Screen_Resolution";
    private const string KeyFullscreen = "Screen_Fullscreen";
    private const string KeyQuality    = "Screen_Quality";
    private const string KeyQualityUi  = "Screen_QualityUi";
    private const string KeyBrightness = "Screen_Brightness";

    // UI 下拉顺序：高 → 中 → 低；对应 Unity Quality 等级 2 → 1 → 0
    private static readonly string[] QualityLabels    = { "高", "中", "低" };
    private static readonly int[]    UiToUnityQuality = { 2, 1, 0 };

    // 出厂默认值
    private const int   DefaultWidth        = 1920;
    private const int   DefaultHeight       = 1080;
    private const int   DefaultQualityUi    = 1;     // 中
    private const bool  DefaultFullScreen   = false;
    private const float DefaultBrightness   = 1f;

    public ScreenSettings CurrentSettings { get; private set; } = new ScreenSettings();

    private readonly List<string> _resolutionOptions  = new();
    private readonly List<int>    _resolutionRawIndex = new();
    private readonly List<string> _qualityOptions     = new();
    private Resolution[]          _resolutions;

    public IReadOnlyList<string> ResolutionOptions => _resolutionOptions;
    public IReadOnlyList<string> QualityOptions    => _qualityOptions;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildOptionLists();
        Load();
    }

    // ── 公开接口实现 ───────────────────────────────────────────────────────────

    public void Apply(ScreenSettings settings)
    {
        CurrentSettings = settings;

        // 分辨率 + 全屏（ResolutionIndex 为下拉选项索引）
        if (TryResolveResolution(settings.ResolutionIndex, out Resolution r))
            Screen.SetResolution(r.width, r.height, settings.IsFullScreen);
        else
            Screen.fullScreen = settings.IsFullScreen;

        // 画质（UI 索引 → Unity Quality 等级）
        int unityQuality = UiIndexToUnityLevel(settings.QualityIndex);
        if (unityQuality >= 0 && unityQuality < QualitySettings.names.Length)
            QualitySettings.SetQualityLevel(unityQuality, applyExpensiveChanges: true);

        // 亮度（Screen.brightness 在 PC 端不生效，此处保存值供外部后处理使用）
        ApplyBrightness(settings.Brightness);
    }

    public void Save()
    {
        PlayerPrefs.SetInt(KeyResolution, CurrentSettings.ResolutionIndex);
        PlayerPrefs.SetInt(KeyFullscreen, CurrentSettings.IsFullScreen ? 1 : 0);
        PlayerPrefs.SetInt(KeyQualityUi,  CurrentSettings.QualityIndex);
        PlayerPrefs.SetInt(KeyQuality,    UiIndexToUnityLevel(CurrentSettings.QualityIndex));
        PlayerPrefs.SetFloat(KeyBrightness, CurrentSettings.Brightness);
        PlayerPrefs.Save();
        Debug.Log("[ScreenSettings] 已保存。");
    }

    public void Load()
    {
        ScreenSettings defaults = GetDefaultSettings();

        ScreenSettings loaded = new ScreenSettings
        {
            ResolutionIndex = ClampResolutionIndex(PlayerPrefs.GetInt(KeyResolution, defaults.ResolutionIndex)),
            IsFullScreen    = PlayerPrefs.GetInt(KeyFullscreen, defaults.IsFullScreen ? 1 : 0) == 1,
            QualityIndex    = LoadQualityUiIndex(defaults.QualityIndex),
            Brightness      = PlayerPrefs.GetFloat(KeyBrightness, defaults.Brightness)
        };

        Apply(loaded);
        Debug.Log("[ScreenSettings] 已加载。");
    }

    public void Reset()
    {
        Apply(GetDefaultSettings());
        Save();
        Debug.Log("[ScreenSettings] 已恢复默认设置。");
    }

    // ── 内部实现 ───────────────────────────────────────────────────────────────

    private void BuildOptionLists()
    {
        // 分辨率列表（去重，按宽×高降序）
        _resolutions = Screen.resolutions;
        _resolutionOptions.Clear();
        _resolutionRawIndex.Clear();

        HashSet<string> seen = new HashSet<string>();
        for (int i = 0; i < _resolutions.Length; i++)
        {
            Resolution r = _resolutions[i];
            string label = $"{r.width}×{r.height}";
            if (seen.Add(label))
            {
                _resolutionOptions.Add(label);
                _resolutionRawIndex.Add(i);
            }
        }

        // 画质：固定三档（高 / 中 / 低）
        _qualityOptions.Clear();
        _qualityOptions.AddRange(QualityLabels);
    }

    private static int UiIndexToUnityLevel(int uiIndex)
    {
        uiIndex = Mathf.Clamp(uiIndex, 0, QualityLabels.Length - 1);
        return UiToUnityQuality[uiIndex];
    }

    private static int UnityLevelToUiIndex(int unityLevel)
    {
        unityLevel = Mathf.Clamp(unityLevel, 0, QualityLabels.Length - 1);
        return QualityLabels.Length - 1 - unityLevel;
    }

    private static int LoadQualityUiIndex(int defaultUiIndex)
    {
        if (PlayerPrefs.HasKey(KeyQualityUi))
            return Mathf.Clamp(PlayerPrefs.GetInt(KeyQualityUi), 0, QualityLabels.Length - 1);

        // 兼容旧版两档（Mobile=0, PC=1）存档
        if (PlayerPrefs.HasKey(KeyQuality))
        {
            int legacy = PlayerPrefs.GetInt(KeyQuality);
            if (legacy <= 1)
                return legacy == 0 ? 2 : 1;
            return UnityLevelToUiIndex(legacy);
        }

        return Mathf.Clamp(defaultUiIndex, 0, QualityLabels.Length - 1);
    }

    private ScreenSettings GetDefaultSettings()
    {
        return new ScreenSettings
        {
            ResolutionIndex = FindResolutionOptionIndex(DefaultWidth, DefaultHeight),
            IsFullScreen    = DefaultFullScreen,
            QualityIndex    = DefaultQualityUi,
            Brightness      = DefaultBrightness
        };
    }

    private int FindResolutionOptionIndex(int width, int height)
    {
        string label = $"{width}×{height}";
        int index = _resolutionOptions.IndexOf(label);
        return index >= 0 ? index : 0;
    }

    private int ClampResolutionIndex(int index)
    {
        if (_resolutionOptions.Count == 0) return 0;
        return Mathf.Clamp(index, 0, _resolutionOptions.Count - 1);
    }

    private bool TryResolveResolution(int optionIndex, out Resolution resolution)
    {
        resolution = default;
        if (_resolutions == null || _resolutionRawIndex.Count == 0) return false;

        optionIndex = ClampResolutionIndex(optionIndex);
        int rawIndex = _resolutionRawIndex[optionIndex];
        if (rawIndex < 0 || rawIndex >= _resolutions.Length) return false;

        resolution = _resolutions[rawIndex];
        return true;
    }

    private static void ApplyBrightness(float brightness)
    {
        // PC 端 Screen.brightness 无效；建议通过后处理 Volume 的 Exposure 控制。
        // 此处仅做数值裁剪，具体实现由项目后处理方案决定。
        brightness = Mathf.Clamp01(brightness);

        // 示例：如果场景有全屏遮罩 CanvasGroup，可在这里控制其 alpha
        // BrightnessOverlay.Instance?.SetAlpha(1f - brightness);
    }
}
