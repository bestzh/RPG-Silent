using System.Collections.Generic;
using RPGSilent.Domain;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private string hitboxName;
    [SerializeField] private string hitboxGroup;

    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private Collider        hitboxCollider;
    private AttackExecutor  owner;
    private AttackProfile   profile;
    private bool            isActive;

    public string HitboxName  => string.IsNullOrEmpty(hitboxName) ? name : hitboxName;
    public string HitboxGroup => hitboxGroup;

    private void Awake()
    {
        EnsureCollider();
        DisableHitbox();
    }

    public void EnableHitbox(AttackProfile attackProfile, AttackExecutor attackOwner)
    {
        EnsureCollider();
        profile   = attackProfile;
        owner     = attackOwner;
        isActive  = true;
        hitTargets.Clear();
        hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        EnsureCollider();
        isActive               = false;
        hitboxCollider.enabled = false;
        hitTargets.Clear();
        profile = null;
        owner   = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || profile == null || owner == null) return;

        if ((profile.TargetLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null || hitTargets.Contains(target)) return;

        hitTargets.Add(target);
        owner.ApplyDamage(target, profile, other.ClosestPoint(transform.position));
    }

    private void EnsureCollider()
    {
        if (hitboxCollider != null) return;
        hitboxCollider           = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
    }
}
