#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public static class ComfortAudioAuthoring
{
    const string PalettePath = "Assets/Resources/Audio/Presentation.asset";
    [MenuItem("Tools/RPG-Silent/Install Comfort Audio")]
    public static void Install() => CreatePalette();

    public static GameAudioPalette CreatePalette(bool refreshExisting = false)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameAudioPalette>(PalettePath);
        if (existing != null && !refreshExisting) return existing;
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Audio")) AssetDatabase.CreateFolder("Assets/Resources", "Audio");
        var paths = AssetDatabase.FindAssets("t:AudioClip").Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Suntail Village/Audio/Main.mixer");
        if (mixer == null) throw new InvalidOperationException("Existing audio mixer unavailable");
        var palette = existing != null ? existing : ScriptableObject.CreateInstance<GameAudioPalette>();
        palette.Music = mixer.FindMatchingGroups("Music").FirstOrDefault();
        palette.Effects = mixer.FindMatchingGroups("Sounds").FirstOrDefault();
        if (palette.Music == null || palette.Effects == null) throw new InvalidOperationException("Existing mixer groups unavailable");
        AudioClipSetting Load(string name, float targetRms)
        {
            string path = paths.Single(p => System.IO.Path.GetFileNameWithoutExtension(p) == name);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            float gain = .2f;
            if (clip != null && clip.loadType != AudioClipLoadType.Streaming && clip.LoadAudioData())
            {
                int frames = clip.length <= 5 ? clip.samples : Mathf.Min(4096, clip.samples);
                var samples = new float[frames * clip.channels];
                double sum = 0; float peak = 0; int count = 0;
                int slices = frames == clip.samples ? 1 : 12;
                for (int slice = 0; slice < slices; slice++)
                {
                    int offset = (int)((long)Mathf.Max(0, clip.samples - frames) * slice / Mathf.Max(1, slices - 1));
                    if (!clip.GetData(samples, offset)) continue;
                    foreach (float value in samples) { sum += value * value; peak = Mathf.Max(peak, Mathf.Abs(value)); count++; }
                }
                if (count > 0)
                {
                    float rms = (float)Math.Sqrt(sum / count);
                    gain = Mathf.Clamp(targetRms / Mathf.Max(.001f, rms), 0, 1f);
                    gain = Mathf.Min(gain, .45f / Mathf.Max(.001f, peak));
                }
            }
            return new AudioClipSetting { Clip = clip, Gain = gain };
        }
        palette.MenuMusic = Load("Suntail - Mild Flight", .035f);
        palette.ExplorationMusic = Load("Guitar-Gentle", .03f);
        palette.BattleMusic = Load("Suntail - Mild Flight", .045f);
        palette.TownAmbience = Load("Birds_1", .012f);
        palette.ForestAmbience = Load("Wind_2", .012f);
        palette.CaveAmbience = Load("Volcano-bubble", .009f);
        palette.Sounds = new[]
        {
            new GameSoundSetting { Sound = GameSound.Button, Audio = Load("UIClick", .02f) },
            new GameSoundSetting { Sound = GameSound.Attack, Audio = Load("SwooshSlide1b", .015f) },
            new GameSoundSetting { Sound = GameSound.EnemyHit, Audio = Load("Item_Metal_Sword_1", .02f) },
            new GameSoundSetting { Sound = GameSound.PlayerHit, Audio = Load("Item_Metal_Axe_1", .02f) },
            new GameSoundSetting { Sound = GameSound.Guard, Audio = Load("Item_Metal_Shield_1", .02f) },
            new GameSoundSetting { Sound = GameSound.Pickup, Audio = Load("Item_Wood_Small_1", .02f) },
            new GameSoundSetting { Sound = GameSound.Coins, Audio = Load("Coin Pop", .02f) },
            new GameSoundSetting { Sound = GameSound.Quest, Audio = Load("Success5", .02f) }
        };
        if (existing == null) AssetDatabase.CreateAsset(palette, PalettePath);
        else EditorUtility.SetDirty(palette);
        AssetDatabase.SaveAssetIfDirty(palette);
        return palette;
    }
}
#endif
