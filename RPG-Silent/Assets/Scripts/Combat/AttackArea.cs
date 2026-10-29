using System.Collections;
using RPGSilent.Combat;
using System.Collections.Generic;
using RPGSilent.Domain;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackArea : MonoBehaviour
{
    private readonly Dictionary<Collider, IDamageable> targetColliders = new();
    private readonly HashSet<IDamageable> tickTargets = new();
    private AttackContext context;
    private AttackProfile profile => context?.Profile;
    private AttackExecutor owner => context?.Owner;
    private float           tickInterval;
    private TMPro.TextMeshPro dangerLabel;
    private float expiresAt, nextLabel;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Initialize(AttackContext attack)
    {
        context = attack;
        float duration = profile != null ? Mathf.Max(0f,    profile.AreaDuration) : 0f;
        tickInterval   = profile != null ? Mathf.Max(0.05f, profile.TickInterval)  : 1f;
        expiresAt = Time.time + duration;
        if (attack?.EnemyOrigin != null)
        {
            var enemy = owner.GetComponent<EnemyController>();
            var sphere = GetComponent<SphereCollider>();
            if (sphere != null) sphere.radius = Mathf.Max(0, profile.AoeRadius) / Mathf.Max(.001f, Mathf.Abs(transform.lossyScale.x));
            if (enemy != null && enemy.CombatWarningMaterial != null)
            {
                var warning = new GameObject("Persistent attack boundary"); warning.transform.SetParent(transform, false);
                var line = warning.AddComponent<LineRenderer>(); line.sharedMaterial = enemy.CombatWarningMaterial;
                line.useWorldSpace = true; line.positionCount = 49; line.loop = true; line.startWidth = line.endWidth = .1f;
                line.startColor = line.endColor = enemy.IsCompanion ? new Color(.3f, 1, .5f, .9f) : new Color(1, .35f, .15f, .9f);
                for (int i = 0; i < 49; i++) { float angle = i * Mathf.PI * 2 / 48; line.SetPosition(i, transform.position + new Vector3(Mathf.Sin(angle) * profile.AoeRadius, .08f, Mathf.Cos(angle) * profile.AoeRadius)); }
                var text = new GameObject("Persistent attack hint"); text.transform.SetParent(transform, false); text.transform.localPosition = Vector3.up * .8f;
                dangerLabel = text.AddComponent<TMPro.TextMeshPro>(); dangerLabel.font = Resources.Load<LootPresentation>("Loot/Presentation")?.Font;
                dangerLabel.fontSize = 2.4f; dangerLabel.alignment = TMPro.TextAlignmentOptions.Center; dangerLabel.rectTransform.sizeDelta = new Vector2(6, 1.2f);
                dangerLabel.color = enemy.IsCompanion ? new Color(.4f, 1, .6f) : new Color(1, .8f, .3f); dangerLabel.raycastTarget = false;
            }
        }

        StartCoroutine(TickDamage());
        Destroy(gameObject, duration);
    }
    private void Update()
    {
        if (owner == null || !owner.IsContextValid(context)) { gameObject.SetActive(false); Destroy(gameObject); return; }
        if (dangerLabel == null || Time.unscaledTime < nextLabel) return;
        nextLabel = Time.unscaledTime + .1f;
        dangerLabel.text = $"[O] 持续区域 · {Mathf.Max(0, expiresAt - Time.time):0.0}s";
        if (Camera.main != null) dangerLabel.transform.rotation = Camera.main.transform.rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable target = ResolveTarget(other);
        if (target != null) targetColliders[other] = target;
    }

    private void OnTriggerExit(Collider other)
    {
        targetColliders.Remove(other);
    }

    private IEnumerator TickDamage()
    {
        while (profile != null && owner != null && owner.IsContextValid(context))
        {
            tickTargets.Clear();
            foreach (var pair in targetColliders)
            {
                IDamageable target = pair.Value;
                if (pair.Key != null && target is Component targetComp && targetComp != null
                    && tickTargets.Add(target))
                    owner.ApplyDamage(target, context,
                        targetComp.transform.position, allowRepeatedDamage: true);
            }
            yield return new WaitForSeconds(tickInterval);
        }
        Destroy(gameObject);
    }

    private IDamageable ResolveTarget(Collider other)
    {
        if (profile == null || (profile.TargetLayers.value & (1 << other.gameObject.layer)) == 0)
            return null;
        return other.GetComponentInParent<IDamageable>();
    }
}
