using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 声音设置服务，实现 ISoundSettingsService。
/// 通过 AudioMixer 暴露参数控制总音量 / 音乐 / 音效，并用 PlayerPrefs 持久化。
/// 由 GameLifetimeScope 注册为全局单例。
/// </summary>
public class SoundSettingsService : MonoBehaviour, ISoundSettingsService
{
    private const string KeyMaster = "Sound_Master";
    private const string KeyMusic  = "Sound_Music";
    private const string KeySFX    = "Sound_SFX";
    private const string KeyMuted  = "Sound_Muted";

    // 兼容旧版 PlayerPrefs 键名
    private const string LegacyKeyMaster = "Audio_Master";
    private const string LegacyKeyMusic  = "Audio_Music";
    private const string LegacyKeySFX    = "Audio_SFX";
    private const string LegacyKeyMuted  = "Audio_Muted";

    private const string ParamMaster = "masterVolume";
    private const string ParamMusic  = "musicVolume";
    private const string ParamSFX    = "soundsVolume";

    private const float DefaultVolume = 1f;
    private const float MinDb         = -80f;

    [SerializeField] private AudioMixer mixer;

    public SoundSettings CurrentSettings { get; private set; } = new SoundSettings();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void Apply(SoundSettings settings)
    {
        CurrentSettings = new SoundSettings
        {
            MasterVolume = ClampVolume(settings.MasterVolume),
            MusicVolume  = ClampVolume(settings.MusicVolume),
            SFXVolume    = ClampVolume(settings.SFXVolume),
            IsMuted      = settings.IsMuted
        };

        if (mixer == null)
        {
            Debug.LogWarning("[SoundSettings] 未绑定 AudioMixer，仅保存设置值。");
            return;
        }

        float masterLinear = CurrentSettings.IsMuted ? 0f : CurrentSettings.MasterVolume;
        SetMixerVolume(ParamMaster, masterLinear);
        SetMixerVolume(ParamMusic,  CurrentSettings.MusicVolume);
        SetMixerVolume(ParamSFX,    CurrentSettings.SFXVolume);

        // 兜底：未接入 Mixer 的 AudioSource 仍受全局音量影响
        AudioListener.volume = masterLinear;
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(KeyMaster, CurrentSettings.MasterVolume);
        PlayerPrefs.SetFloat(KeyMusic,  CurrentSettings.MusicVolume);
        PlayerPrefs.SetFloat(KeySFX,    CurrentSettings.SFXVolume);
        PlayerPrefs.SetInt(KeyMuted,    CurrentSettings.IsMuted ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("[SoundSettings] 已保存。");
    }

    public void Load()
    {
        SoundSettings defaults = GetDefaultSettings();
        SoundSettings loaded = new SoundSettings
        {
            MasterVolume = LoadFloat(KeyMaster, LegacyKeyMaster, defaults.MasterVolume),
            MusicVolume  = LoadFloat(KeyMusic,  LegacyKeyMusic,  defaults.MusicVolume),
            SFXVolume    = LoadFloat(KeySFX,    LegacyKeySFX,    defaults.SFXVolume),
            IsMuted      = LoadMuted(defaults.IsMuted)
        };

        Apply(loaded);
        Debug.Log("[SoundSettings] 已加载。");
    }

    public void Reset()
    {
        Apply(GetDefaultSettings());
        Save();
        Debug.Log("[SoundSettings] 已恢复默认设置。");
    }

    private static SoundSettings GetDefaultSettings()
    {
        return new SoundSettings
        {
            MasterVolume = DefaultVolume,
            MusicVolume  = DefaultVolume,
            SFXVolume    = DefaultVolume,
            IsMuted      = false
        };
    }

    private static float LoadFloat(string key, string legacyKey, float defaultValue)
    {
        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetFloat(key, defaultValue);
        if (PlayerPrefs.HasKey(legacyKey))
            return PlayerPrefs.GetFloat(legacyKey, defaultValue);
        return defaultValue;
    }

    private static bool LoadMuted(bool defaultValue)
    {
        if (PlayerPrefs.HasKey(KeyMuted))
            return PlayerPrefs.GetInt(KeyMuted, defaultValue ? 1 : 0) == 1;
        if (PlayerPrefs.HasKey(LegacyKeyMuted))
            return PlayerPrefs.GetInt(LegacyKeyMuted, defaultValue ? 1 : 0) == 1;
        return defaultValue;
    }

    private static float ClampVolume(float volume) => Mathf.Clamp01(volume);

    private void SetMixerVolume(string paramName, float linearVolume)
    {
        if (!mixer.SetFloat(paramName, LinearToDb(linearVolume)))
            Debug.LogWarning($"[SoundSettings] Mixer 参数 '{paramName}' 未找到，请在 Main.mixer 中 Expose。");
    }

    private static float LinearToDb(float linear)
    {
        if (linear <= 0.0001f) return MinDb;
        return Mathf.Log10(linear) * 20f;
    }
}
