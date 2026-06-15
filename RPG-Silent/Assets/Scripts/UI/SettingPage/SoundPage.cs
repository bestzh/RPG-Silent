using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 设置页面 → 声音分页（Sound Tab）。
/// 负责：总音量、音乐、音效、静音的 UI 交互与设置应用。
/// </summary>
public class SoundPage : MonoBehaviour
{
    private ISoundSettingsService _soundService;
    private bool _injected;

    [Inject]
    public void Construct(ISoundSettingsService soundService)
    {
        _soundService = soundService;
        _injected     = true;
    }

    [Header("总音量")]
    [SerializeField] private Slider    masterVolumeSlider;
    [SerializeField] private TMP_Text  masterVolumeLabel;

    [Header("音乐音量")]
    [SerializeField] private Slider    musicVolumeSlider;
    [SerializeField] private TMP_Text  musicVolumeLabel;

    [Header("音效音量")]
    [SerializeField] private Slider    sfxVolumeSlider;
    [SerializeField] private TMP_Text  sfxVolumeLabel;

    [Header("静音")]
    [SerializeField] private Toggle muteToggle;

    [Header("确认按钮")]
    [SerializeField] private Button okButton;

    [Header("重置按钮")]
    [SerializeField] private Button resetButton;

    private SoundSettings _pendingSettings;
    private SoundSettings _savedSnapshot;

    private void Awake()
    {
        okButton?.onClick.AddListener(OnOKClicked);
        resetButton?.onClick.AddListener(OnResetClicked);
    }

    private void OnEnable()
    {
        if (!_injected) return;
        RefreshUI();
        _savedSnapshot = CloneSettings(_soundService.CurrentSettings);
    }

    private void OnDisable()
    {
        // 未点「应用」就离开页面时，恢复进入页面前的音量
        if (_soundService == null || _savedSnapshot == null) return;
        _soundService.Apply(_savedSnapshot);
    }

    private void RefreshUI()
    {
        if (_soundService == null) return;

        SoundSettings cur = _soundService.CurrentSettings;
        _pendingSettings = new SoundSettings
        {
            MasterVolume = cur.MasterVolume,
            MusicVolume  = cur.MusicVolume,
            SFXVolume    = cur.SFXVolume,
            IsMuted      = cur.IsMuted
        };

        SetupVolumeSlider(masterVolumeSlider, masterVolumeLabel, _pendingSettings.MasterVolume,
            value => _pendingSettings.MasterVolume = value);
        SetupVolumeSlider(musicVolumeSlider, musicVolumeLabel, _pendingSettings.MusicVolume,
            value => _pendingSettings.MusicVolume = value);
        SetupVolumeSlider(sfxVolumeSlider, sfxVolumeLabel, _pendingSettings.SFXVolume,
            value => _pendingSettings.SFXVolume = value);
        SetupMuteToggle();
    }

    private void SetupVolumeSlider(Slider slider, TMP_Text label, float value,
        UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        UpdatePercentLabel(label, value);

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(v =>
        {
            onChanged(v);
            UpdatePercentLabel(label, v);
            PreviewPendingSettings();
        });
    }

    private void SetupMuteToggle()
    {
        if (muteToggle == null) return;

        muteToggle.SetIsOnWithoutNotify(_pendingSettings.IsMuted);

        muteToggle.onValueChanged.RemoveAllListeners();
        muteToggle.onValueChanged.AddListener(value =>
        {
            _pendingSettings.IsMuted = value;
            PreviewPendingSettings();
        });
    }

    private void PreviewPendingSettings()
    {
        if (_soundService == null || _pendingSettings == null) return;
        _soundService.Apply(_pendingSettings);
    }

    private static SoundSettings CloneSettings(SoundSettings source)
    {
        return new SoundSettings
        {
            MasterVolume = source.MasterVolume,
            MusicVolume  = source.MusicVolume,
            SFXVolume    = source.SFXVolume,
            IsMuted      = source.IsMuted
        };
    }

    private static void UpdatePercentLabel(TMP_Text label, float volume)
    {
        if (label == null) return;
        label.text = $"{Mathf.RoundToInt(volume * 100f)}%";
    }

    private void OnOKClicked()
    {
        if (_soundService == null || _pendingSettings == null) return;

        _soundService.Apply(_pendingSettings);
        _soundService.Save();
        _savedSnapshot = CloneSettings(_pendingSettings);
        Debug.Log("[SoundPage] 设置已应用并保存。");
    }

    private void OnResetClicked()
    {
        if (_soundService == null) return;
        _soundService.Reset();
        RefreshUI();
        _savedSnapshot = CloneSettings(_soundService.CurrentSettings);
    }
}
