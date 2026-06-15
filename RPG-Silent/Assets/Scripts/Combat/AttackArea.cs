using System.Collections;
using System.Collections.Generic;
using RPGSilent.Domain;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackArea : MonoBehaviour
{
    private readonly HashSet<IDamageable> targets = new HashSet<IDamageable>();
    private AttackProfile   profile;
    private AttackExecutor  owner;
    private float           tickInterval;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Initialize(AttackProfile attackProfile, AttackExecutor attackOwner)
    {
        profile      = attackProfile;
        owner        = attackOwner;
        float duration = profile != null ? Mathf.Max(0f,    profile.AreaDuration) : 0f;
        tickInterval   = profile != null ? Mathf.Max(0.05f, profile.TickInterval)  : 1f;

        StartCoroutine(TickDamage());
        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable target = ResolveTarget(other);
        if (target != null) targets.Add(target);
    }

    private void OnTriggerExit(Collider other)
    {
        IDamageable target = ResolveTarget(other);
        if (target != null) targets.Remove(target);
    }

    private IEnumerator TickDamage()
    {
        while (profile != null && owner != null)
        {
            foreach (IDamageable target in targets)
            {
                if (target is Component targetComp)
                    owner.ApplyDamage(target, profile,
                        targetComp.transform.position, allowRepeatedDamage: true);
            }
            yield return new WaitForSeconds(tickInterval);
        }
    }

    private IDamageable ResolveTarget(Collider other)
    {
        if (profile == null || (profile.TargetLayers.value & (1 << other.gameObject.layer)) == 0)
            return null;
        return other.GetComponentInParent<IDamageable>();
    }
}
