using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : UIBase
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string QualityLevelKey = "Settings.QualityLevel";

    public Button BackButton;
    public Button ApplyButton;
    public Button ResetButton;

    public Slider MasterVolumeSlider;
    public TextMeshProUGUI MasterVolumeText;
    public Toggle FullscreenToggle;
    public TMP_Dropdown QualityDropdown;

    private float masterVolume = 1f;
    private bool fullscreen = true;
    private int qualityLevel;

    private void Awake()
    {
        BackButton?.onClick.AddListener(OnBackButtonClicked);
        ApplyButton?.onClick.AddListener(OnApplyButtonClicked);
        ResetButton?.onClick.AddListener(OnResetButtonClicked);

        MasterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
        FullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
        QualityDropdown?.onValueChanged.AddListener(OnQualityChanged);
    }

    protected override void OnInit()
    {
        base.OnInit();
        InitQualityOptions();
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        LoadSettings();
        RefreshControls();
        ApplySettings();
        Debug.Log("SettingsUI opened.");
    }

    public override void OnClose()
    {
        base.OnClose();
        SaveSettings();
        Debug.Log("SettingsUI closed.");
    }

    private void OnBackButtonClicked()
    {
        SaveSettings();
        UIManager.Instance.OpenUI("UI/StartUI");
        UIManager.Instance.CloseUI("UI/SettingsUI");
    }

    private void OnApplyButtonClicked()
    {
        ApplySettings();
        SaveSettings();
    }

    private void OnResetButtonClicked()
    {
        masterVolume = 1f;
        fullscreen = true;
        qualityLevel = QualitySettings.names.Length > 0 ? QualitySettings.names.Length - 1 : 0;

        RefreshControls();
        ApplySettings();
        SaveSettings();
    }

    private void OnMasterVolumeChanged(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyMasterVolume();
        RefreshVolumeText();
    }

    private void OnFullscreenChanged(bool value)
    {
        fullscreen = value;
        Screen.fullScreen = fullscreen;
    }

    private void OnQualityChanged(int value)
    {
        qualityLevel = ClampQualityLevel(value);
        QualitySettings.SetQualityLevel(qualityLevel, true);
    }

    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        qualityLevel = PlayerPrefs.GetInt(QualityLevelKey, QualitySettings.GetQualityLevel());

        masterVolume = Mathf.Clamp01(masterVolume);
        qualityLevel = ClampQualityLevel(qualityLevel);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(QualityLevelKey, qualityLevel);
        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        ApplyMasterVolume();
        Screen.fullScreen = fullscreen;
        QualitySettings.SetQualityLevel(qualityLevel, true);
    }

    private void ApplyMasterVolume()
    {
        AudioListener.volume = masterVolume;
    }

    private void RefreshControls()
    {
        if (MasterVolumeSlider != null)
        {
            MasterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }

        if (FullscreenToggle != null)
        {
            FullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }

        if (QualityDropdown != null)
        {
            InitQualityOptions();
            QualityDropdown.SetValueWithoutNotify(qualityLevel);
            QualityDropdown.RefreshShownValue();
        }

        RefreshVolumeText();
    }

    private void RefreshVolumeText()
    {
        if (MasterVolumeText != null)
        {
            MasterVolumeText.text = $"{Mathf.RoundToInt(masterVolume * 100f)}%";
        }
    }

    private void InitQualityOptions()
    {
        if (QualityDropdown == null)
        {
            return;
        }

        string[] qualityNames = QualitySettings.names;
        if (QualityDropdown.options.Count == qualityNames.Length)
        {
            return;
        }

        QualityDropdown.ClearOptions();
        QualityDropdown.AddOptions(new List<string>(qualityNames));
    }

    private int ClampQualityLevel(int value)
    {
        int maxQualityLevel = QualitySettings.names.Length - 1;
        return maxQualityLevel < 0 ? 0 : Mathf.Clamp(value, 0, maxQualityLevel);
    }
}
