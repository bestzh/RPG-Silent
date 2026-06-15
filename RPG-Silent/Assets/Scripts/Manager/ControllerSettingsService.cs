using RPGSilent.Domain;
using UnityEngine;

/// <summary>
/// 控制器设置服务，实现 IControllerSettingsService。
/// 持久化鼠标灵敏度、冲刺持定时间、反转Y轴，由 GameLifetimeScope 注册为全局单例。
/// </summary>
public class ControllerSettingsService : MonoBehaviour, IControllerSettingsService
{
    private const string KeyMouseSensitivity = "Controller_MouseSensitivity";
    private const string KeySprintHoldTime   = "Controller_SprintHoldTime";
    private const string KeyInvertY          = "Controller_InvertY";

    private const float DefaultMouseSensitivity = 3f;
    private const float DefaultSprintHoldTime   = 0.5f;
    private const bool  DefaultInvertY          = false;

    public ControllerSettings CurrentSettings { get; private set; } = new ControllerSettings();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void Apply(ControllerSettings settings)
    {
        CurrentSettings = new ControllerSettings
        {
            MouseSensitivity = Mathf.Clamp(settings.MouseSensitivity, 0.1f, 10f),
            SprintHoldTime   = Mathf.Clamp(settings.SprintHoldTime,   0.05f, 2f),
            InvertY          = settings.InvertY
        };
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(KeyMouseSensitivity, CurrentSettings.MouseSensitivity);
        PlayerPrefs.SetFloat(KeySprintHoldTime,   CurrentSettings.SprintHoldTime);
        PlayerPrefs.SetInt(KeyInvertY,            CurrentSettings.InvertY ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("[ControllerSettings] 已保存。");
    }

    public void Load()
    {
        ControllerSettings loaded = new ControllerSettings
        {
            MouseSensitivity = PlayerPrefs.GetFloat(KeyMouseSensitivity, DefaultMouseSensitivity),
            SprintHoldTime   = PlayerPrefs.GetFloat(KeySprintHoldTime,   DefaultSprintHoldTime),
            InvertY          = PlayerPrefs.GetInt(KeyInvertY,            DefaultInvertY ? 1 : 0) == 1
        };
        Apply(loaded);
        Debug.Log("[ControllerSettings] 已加载。");
    }

    public void Reset()
    {
        Apply(new ControllerSettings
        {
            MouseSensitivity = DefaultMouseSensitivity,
            SprintHoldTime   = DefaultSprintHoldTime,
            InvertY          = DefaultInvertY
        });
        Save();
        Debug.Log("[ControllerSettings] 已恢复默认设置。");
    }
}
