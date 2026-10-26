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

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Initialize(AttackContext attack)
    {
        context = attack;
        float duration = profile != null ? Mathf.Max(0f,    profile.AreaDuration) : 0f;
        tickInterval   = profile != null ? Mathf.Max(0.05f, profile.TickInterval)  : 1f;

        StartCoroutine(TickDamage());
        Destroy(gameObject, duration);
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
