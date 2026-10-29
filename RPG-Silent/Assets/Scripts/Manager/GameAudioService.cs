using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

/// <summary>Owns scene music, ambience and a bounded pool of transient voices.</summary>
public sealed class GameAudioService : MonoBehaviour
{
    public const int VoiceCapacity = 12;
    readonly AudioSource[] voices = new AudioSource[VoiceCapacity];
    readonly float[] started = new float[VoiceCapacity], gains = new float[VoiceCapacity];
    readonly float[] nextCue = new float[8];
    readonly AudioClipSetting[] clips = new AudioClipSetting[8];
    AudioSource musicA, musicB, ambience;
    AudioClipSetting currentMusic, currentAmbience;
    GameAudioPalette palette;
    DungeonDatabase dungeons;
    PlayerSessionService session;
    PlayerProgressService progress;
    RPGSilent.Domain.IPlayerInputActions input;
    PlayerController player;
    GameObject rig;
    bool initialized, fading;
    float fade, fromGain, toGain, ambienceGain, nextContext;
    public int PlayedCount { get; private set; }
    public GameSound LastSound { get; private set; }
    public string Context { get; private set; }
    public int ActiveVoices { get { int count = 0; foreach (var source in voices) if (source != null && source.isPlaying) count++; return count; } }

    [Inject]
    public void Construct(PlayerSessionService playerSession, PlayerProgressService playerProgress, RPGSilent.Domain.IPlayerInputActions playerInput)
    {
        if (initialized) return;
        session = playerSession; progress = playerProgress; input = playerInput;
        palette = Resources.Load<GameAudioPalette>("Audio/Presentation");
        dungeons = Resources.Load<DungeonDatabase>("DungeonDatabase");
        if (palette == null) { Debug.LogWarning("[GameAudio] 音频配置不可用。"); return; }
        foreach (var entry in palette.Sounds) if (entry != null && (int)entry.Sound >= 0 && (int)entry.Sound < clips.Length) clips[(int)entry.Sound] = entry.Audio;
        musicA = CreateSource("Music A", true); musicB = CreateSource("Music B", true);
        ambience = CreateSource("Ambience", false); ambience.loop = true;
        for (int i = 0; i < voices.Length; i++) voices[i] = CreateSource("Effect " + i, false);
        progress.WorldLootCollected += OnLoot;
        progress.QuestCompleted += OnQuest;
        SceneManager.activeSceneChanged += OnSceneChanged;
        initialized = true;
        UpdateContext();
    }

    AudioSource CreateSource(string name, bool music)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>(); source.playOnAwake = false;
        source.outputAudioMixerGroup = music ? palette.Music : palette.Effects;
        source.dopplerLevel = 0; source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2; source.maxDistance = 28; source.volume = 0;
        return source;
    }

    public bool Play(GameSound sound, Vector3 position)
    {
        int cue = (int)sound;
        if (!initialized || cue < 0 || cue >= clips.Length || clips[cue]?.Clip == null || Time.unscaledTime < nextCue[cue]) return false;
        int slot = -1;
        for (int i = 0; i < voices.Length; i++) if (!voices[i].isPlaying) { slot = i; break; }
        if (slot < 0)
        {
            slot = 0;
            for (int i = 1; i < voices.Length; i++) if (started[i] < started[slot]) slot = i;
        }
        var source = voices[slot]; source.Stop(); source.clip = clips[cue].Clip;
        source.transform.position = position;
        source.spatialBlend = sound == GameSound.Attack || sound == GameSound.EnemyHit || sound == GameSound.PlayerHit || sound == GameSound.Guard ? .8f : 0;
        gains[slot] = Mathf.Clamp01(clips[cue].Gain); started[slot] = Time.unscaledTime;
        source.volume = gains[slot] / Mathf.Sqrt(1 + ActiveVoices);
        source.Play();
        nextCue[cue] = Time.unscaledTime + (sound == GameSound.Quest ? .3f : sound == GameSound.Coins ? .12f : .06f);
        PlayedCount++; LastSound = sound;
        return true;
    }

    void OnLoot(bool items) => Play(items ? GameSound.Pickup : GameSound.Coins, player != null ? player.transform.position : Vector3.zero);
    void OnQuest() => Play(GameSound.Quest, Vector3.zero);
    void OnSceneChanged(Scene before, Scene after)
    {
        foreach (var voice in voices) if (voice != null) voice.Stop();
        nextContext = 0;
    }

    void Update()
    {
        if (!initialized) return;
        if (Time.unscaledTime >= nextContext) { nextContext = Time.unscaledTime + .2f; UpdateContext(); }
        float duck = session.IsActive && !input.GameplayInputEnabled ? .55f : 1;
        if (fading)
        {
            fade = Mathf.Min(1, fade + Time.unscaledDeltaTime / 1.5f);
            musicA.volume = fromGain * Mathf.Sqrt(1 - fade) * duck;
            musicB.volume = toGain * Mathf.Sqrt(fade) * duck;
            if (fade >= 1)
            {
                musicA.Stop(); var old = musicA; musicA = musicB; musicB = old;
                fromGain = toGain; fading = false;
            }
        }
        else musicA.volume = fromGain * duck;
        ambience.volume = Mathf.MoveTowards(ambience.volume, ambienceGain * duck * (Context == "Combat" ? .5f : 1), Time.unscaledDeltaTime * .12f);
        float headroom = 1 / Mathf.Sqrt(Mathf.Max(1, ActiveVoices));
        for (int i = 0; i < voices.Length; i++) if (voices[i].isPlaying) voices[i].volume = gains[i] * headroom;
    }

    void UpdateContext()
    {
        if (rig != session.PlayerRig)
        { rig = session.PlayerRig; player = rig != null ? rig.GetComponentInChildren<PlayerController>() : null; }
        string scene = SceneManager.GetActiveScene().name;
        bool menu = !session.IsActive || player == null || scene == "StartScene";
        bool town = !menu && !player.CombatAllowed;
        bool fighting = false;
        if (!menu && !town)
            foreach (var enemy in EnemyController.ActiveEnemies)
                if (enemy != null && !enemy.IsDead && !enemy.IsCompanion && !enemy.IsTrainingTarget && enemy.HasAggro && enemy.gameObject.scene == SceneManager.GetActiveScene())
                { fighting = true; break; }
        Context = menu ? "Menu" : town ? "Town" : fighting ? "Combat" : "Exploration";
        var music = menu || town ? palette.MenuMusic : fighting ? palette.BattleMusic : palette.ExplorationMusic;
        ChangeMusic(music);
        var environment = menu ? null : town ? palette.TownAmbience : dungeons?.GetById(1002)?.MatchesScene(scene) == true ? palette.CaveAmbience : palette.ForestAmbience;
        if (currentAmbience != environment)
        {
            ambience.Stop(); ambience.volume = 0; currentAmbience = environment;
            ambience.clip = environment?.Clip;
            if (ambience.clip != null) ambience.Play();
        }
        ambienceGain = environment != null ? environment.Gain : 0;
    }

    void ChangeMusic(AudioClipSetting setting)
    {
        if (currentMusic == setting) return;
        if (fading)
        {
            // Keep the audible side when another scene transition interrupts a fade.
            if (musicB.volume > musicA.volume) { var old = musicA; musicA = musicB; musicB = old; }
            musicB.Stop(); fromGain = musicA.volume;
        }
        currentMusic = setting;
        musicB.Stop(); musicB.clip = setting?.Clip; musicB.loop = true; musicB.volume = 0;
        toGain = setting?.Clip != null ? Mathf.Clamp01(setting.Gain) : 0;
        if (musicB.clip != null) musicB.Play();
        fade = 0; fading = true;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (progress != null) { progress.WorldLootCollected -= OnLoot; progress.QuestCompleted -= OnQuest; }
    }
}
