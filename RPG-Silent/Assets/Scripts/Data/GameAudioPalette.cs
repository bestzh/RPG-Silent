using System;
using UnityEngine;
using UnityEngine.Audio;

public enum GameSound { Button, Attack, EnemyHit, PlayerHit, Guard, Pickup, Coins, Quest }

[Serializable]
public sealed class AudioClipSetting
{
    public AudioClip Clip;
    [Range(0, 1)] public float Gain = .3f;
}

[Serializable]
public sealed class GameSoundSetting
{
    public GameSound Sound;
    public AudioClipSetting Audio = new();
}

[CreateAssetMenu(menuName = "Presentation/Game Audio")]
public sealed class GameAudioPalette : ScriptableObject
{
    public AudioMixerGroup Music, Effects;
    public AudioClipSetting MenuMusic = new(), ExplorationMusic = new(), BattleMusic = new();
    public AudioClipSetting TownAmbience = new(), ForestAmbience = new(), CaveAmbience = new();
    public GameSoundSetting[] Sounds = Array.Empty<GameSoundSetting>();
}
