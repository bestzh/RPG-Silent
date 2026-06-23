using System;
using System.Collections.Generic;
using RPGSilent.Domain;
using UnityEngine;

/// <summary>
/// 游戏设置服务，实现 IGameSettingsService。
/// 持久化难度、HUD、小地图、屏幕震动，由 GameLifetimeScope 注册为全局单例。
/// </summary>
public class GameSettingsService : MonoBehaviour, IGameSettingsService
{
    private const string KeyDifficulty        = "Game_Difficulty";
    private const string KeyShowHud         = "Game_ShowHud";
    private const string KeyShowMiniMap     = "Game_ShowMiniMap";
    private const string KeyScreenShake     = "Game_ScreenShake";

    private const int   DefaultDifficulty   = 1;
    private const bool  DefaultShowHud      = true;
    private const bool  DefaultShowMiniMap  = true;
    private const float DefaultScreenShake  = 1f;

    private static readonly string[] DifficultyLabels = { "简单", "普通", "困难" };
    private static readonly float[]  DamageMultipliers = { 0.75f, 1f, 1.5f };

    public GameSettings CurrentSettings { get; private set; } = new GameSettings();
    public IReadOnlyList<string> DifficultyOptions => DifficultyLabels;

    public event Action<GameSettings> OnSettingsApplied;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void Apply(GameSettings settings)
    {
        CurrentSettings = new GameSettings
        {
            DifficultyIndex      = Mathf.Clamp(settings.DifficultyIndex, 0, DifficultyLabels.Length - 1),
            ShowHud              = settings.ShowHud,
            ShowMiniMap          = settings.ShowMiniMap,
            ScreenShakeIntensity = Mathf.Clamp01(settings.ScreenShakeIntensity)
        };

        ScreenShakeManager.SetIntensityScale(CurrentSettings.ScreenShakeIntensity);
        OnSettingsApplied?.Invoke(CurrentSettings);
    }

    public int ScaleIncomingDamage(int rawDamage)
    {
        if (rawDamage <= 0) return 0;

        int index = Mathf.Clamp(CurrentSettings.DifficultyIndex, 0, DamageMultipliers.Length - 1);
        return Mathf.Max(1, Mathf.RoundToInt(rawDamage * DamageMultipliers[index]));
    }

    public void Save()
    {
        PlayerPrefs.SetInt(KeyDifficulty,    CurrentSettings.DifficultyIndex);
        PlayerPrefs.SetInt(KeyShowHud,       CurrentSettings.ShowHud ? 1 : 0);
        PlayerPrefs.SetInt(KeyShowMiniMap,   CurrentSettings.ShowMiniMap ? 1 : 0);
        PlayerPrefs.SetFloat(KeyScreenShake, CurrentSettings.ScreenShakeIntensity);
        PlayerPrefs.Save();
        Debug.Log("[GameSettings] 已保存。");
    }

    public void Load()
    {
        GameSettings defaults = GetDefaultSettings();
        Apply(new GameSettings
        {
            DifficultyIndex      = PlayerPrefs.GetInt(KeyDifficulty,  defaults.DifficultyIndex),
            ShowHud              = PlayerPrefs.GetInt(KeyShowHud,     defaults.ShowHud ? 1 : 0) == 1,
            ShowMiniMap          = PlayerPrefs.GetInt(KeyShowMiniMap, defaults.ShowMiniMap ? 1 : 0) == 1,
            ScreenShakeIntensity = PlayerPrefs.GetFloat(KeyScreenShake, defaults.ScreenShakeIntensity)
        });
        Debug.Log("[GameSettings] 已加载。");
    }

    public void Reset()
    {
        Apply(GetDefaultSettings());
        Save();
        Debug.Log("[GameSettings] 已恢复默认设置。");
    }

    private static GameSettings GetDefaultSettings() => new GameSettings
    {
        DifficultyIndex      = DefaultDifficulty,
        ShowHud              = DefaultShowHud,
        ShowMiniMap          = DefaultShowMiniMap,
        ScreenShakeIntensity = DefaultScreenShake
    };
}
