using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public readonly struct EnemyHit
{
    public readonly int Damage;
    public readonly bool Staggered, Armored, Vulnerable, Killed, Periodic;
    public EnemyHit(int damage, bool staggered, bool armored, bool vulnerable, bool killed, bool periodic)
    { Damage = damage; Staggered = staggered; Armored = armored; Vulnerable = vulnerable; Killed = killed; Periodic = periodic; }
}

/// <summary>Presentation only: one reusable number and AudioSource per enemy, no material instances or global time changes.</summary>
[RequireComponent(typeof(EnemyController))]
public sealed class EnemyHitFeedback : MonoBehaviour
{
    public TMP_FontAsset Font;
    public AudioClip ImpactSound;
    public AudioMixerGroup Mixer;
    EnemyController enemy;
    Animator animator;
    Camera view;
    AudioSource source;
    TextMeshPro number;
    Renderer[] renderers;
    MaterialPropertyBlock[][] originals, flashes;
    float flashRemaining, stopRemaining, numberRemaining, savedSpeed, nextSound;
    Vector3 numberOrigin;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), ColorId = Shader.PropertyToID("_Color");
    public EnemyHit LastHit { get; private set; }
    public int HitCount { get; private set; }

    void Awake()
    {
        enemy = GetComponent<EnemyController>(); animator = GetComponent<Animator>();
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        originals = new MaterialPropertyBlock[renderers.Length][]; flashes = new MaterialPropertyBlock[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            var materials = renderers[i].sharedMaterials;
            originals[i] = new MaterialPropertyBlock[materials.Length]; flashes[i] = new MaterialPropertyBlock[materials.Length];
            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j] == null) continue;
                int property = materials[j].HasProperty(BaseColor) ? BaseColor : ColorId;
                if (!materials[j].HasProperty(property)) continue;
                var original = new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(original, j); originals[i][j] = original;
                var flash = new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(flash, j);
                Color color = original.HasColor(property) ? original.GetColor(property) : materials[j].GetColor(property);
                flash.SetColor(property, Color.Lerp(color, new Color(1f, .86f, .55f, color.a), .65f)); flashes[i][j] = flash;
            }
        }
        source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 1f;
        source.outputAudioMixerGroup = Mixer; source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 2; source.maxDistance = 22;
        var label = new GameObject("Enemy hit feedback"); label.transform.SetParent(transform, false);
        number = label.AddComponent<TextMeshPro>(); if (Font != null) number.font = Font;
        number.fontSize = 4; number.alignment = TextAlignmentOptions.Center; number.rectTransform.sizeDelta = new Vector2(6, 2);
        number.raycastTarget = false; label.SetActive(false);
    }
    void OnEnable() { enemy.Damaged += Show; view = Camera.main; }
    void OnDisable()
    {
        enemy.Damaged -= Show; RestoreFlash(); RestoreSpeed();
        if (number != null) number.gameObject.SetActive(false);
        if (source != null) source.Stop();
        numberRemaining = 0;
    }
    void Show(EnemyHit hit)
    {
        LastHit = hit; HitCount++;
        if (!hit.Periodic)
        {
            flashRemaining = .1f;
            for (int i = 0; i < renderers.Length; i++) for (int j = 0; j < flashes[i].Length; j++)
                if (renderers[i] != null && flashes[i][j] != null) renderers[i].SetPropertyBlock(flashes[i][j], j);
            if (hit.Staggered && !hit.Killed && animator != null && animator.isActiveAndEnabled)
            {
                if (stopRemaining <= 0f) savedSpeed = animator.speed;
                animator.speed = 0f; stopRemaining = .045f;
            }
            if (ImpactSound != null && Time.time >= nextSound && Time.timeScale > 0f)
            { source.PlayOneShot(ImpactSound, hit.Staggered ? .38f : .25f); nextSound = Time.time + .08f; }
        }
        number.text = hit.Damage + (hit.Killed ? "\n击败" : hit.Staggered ? "\n打断" : hit.Armored ? "\n霸体" : hit.Vulnerable ? "\n破绽" : "");
        number.color = hit.Periodic ? new Color(1f, .55f, .25f) : hit.Staggered ? new Color(1f, .8f, .3f) : hit.Armored ? new Color(.65f, .8f, 1f) : Color.white;
        numberOrigin = transform.position + Vector3.up * 2.2f;
        foreach (var renderer in renderers) if (renderer != null && renderer.enabled) numberOrigin.y = Mathf.Max(numberOrigin.y, renderer.bounds.max.y + .25f);
        number.transform.position = numberOrigin;
        number.transform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);
        numberRemaining = .75f; number.gameObject.SetActive(true);
    }
    void LateUpdate()
    {
        if (flashRemaining > 0 && (flashRemaining -= Time.deltaTime) <= 0) RestoreFlash();
        if (stopRemaining > 0 && (stopRemaining -= Time.deltaTime) <= 0) RestoreSpeed();
        if (numberRemaining <= 0) return;
        numberRemaining -= Time.deltaTime;
        number.transform.position = numberOrigin + Vector3.up * ((.75f - numberRemaining) * .6f);
        if (view != null) number.transform.rotation = view.transform.rotation;
        number.alpha = Mathf.Clamp01(numberRemaining / .2f);
        if (numberRemaining <= 0) number.gameObject.SetActive(false);
    }
    void RestoreFlash()
    {
        flashRemaining = 0;
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++) for (int j = 0; j < originals[i].Length; j++)
            if (renderers[i] != null && originals[i][j] != null) renderers[i].SetPropertyBlock(originals[i][j], j);
    }
    void RestoreSpeed()
    {
        if (animator != null && (stopRemaining != 0 || animator.speed == 0f)) animator.speed = savedSpeed;
        stopRemaining = 0;
    }
}
