using UnityEngine;
using UnityEngine.Audio;

/// <summary>Transient presentation and stagger protection; health remains owned by PlayerStats.</summary>
[DisallowMultipleComponent]
public sealed class PlayerHitFeedback : MonoBehaviour
{
    public AudioClip ImpactSound;
    public AudioMixerGroup Mixer;
    AudioSource source;
    PlayerController player;
    float nextSound, nextReaction, shownAt = float.NegativeInfinity;
    public int Sequence { get; private set; }
    public int Damage { get; private set; }
    public Vector3 SourceDirection { get; private set; }
    public AttackImpact Impact { get; private set; }
    public bool Periodic { get; private set; }
    public float Visibility => Mathf.Clamp01(1f - (Time.time - shownAt) / .65f);
    public bool CanReact => Time.time >= nextReaction;
    void Awake()
    {
        player = GetComponent<PlayerController>();
    }
    public void Show(int damage, Vector3 origin, AttackImpact impact, bool periodic)
    {
        if (damage <= 0) return;
        Sequence++; Damage = damage; Impact = impact; Periodic = periodic; shownAt = Time.time;
        SourceDirection = -HitReactionRules.Away(transform.position, origin, transform.forward);
        if (!periodic && Time.time >= nextSound)
        {
            if (player != null && player.Audio != null) player.Audio.Play(GameSound.PlayerHit, transform.position);
            else if (ImpactSound != null)
            {
                if (source == null) { source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = .4f; source.outputAudioMixerGroup = Mixer; }
                source.clip = ImpactSound; source.volume = impact == AttackImpact.Heavy ? .5f : .3f; source.Play();
            }
            nextSound = Time.time + .1f;
        }
    }
    public void ProtectRecovery(float duration) => nextReaction = Time.time + duration + HitReactionRules.RecoveryProtection;
    public void ResetFeedback()
    { nextReaction = nextSound = 0; shownAt = float.NegativeInfinity; if (source != null) source.Stop(); }
    void OnDisable() => ResetFeedback();
}
